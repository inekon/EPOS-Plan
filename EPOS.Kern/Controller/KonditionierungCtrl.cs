using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Controller der Konditionierung</b> (Stufe KP1, Konzept Konditionierungsprofile 6):
    /// Matrix und Kalender eines Gebäudes, einer Zone oder eines Katalogbaus lesen, <b>anlegen</b>,
    /// <b>verwerfen</b> und <b>erneut anwenden</b> — jeder Schreibweg in <b>einer</b> Transaktion.
    ///
    /// <para><b>Die Oberfläche kommt mit KP2.</b> Hier steht die Datenbankseite: Die Razor-Karten und
    /// die Hüllen in <c>EPOS.UI.Daten/Bedarf/</c> rufen diesen Controller, damit in der Oberfläche
    /// kein SQL steht.</para>
    ///
    /// <para><b>Drei Regeln halten „Anlegen ändert keine Reihe"</b> (Konzept 3.3):</para>
    /// <list type="number">
    /// <item><b>Rundlauf</b> — jeder Wert muss Wert → Text → Wert bitgleich zurückkommen, sonst wird
    /// das Anlegen <b>benannt abgelehnt</b> statt eine Reihe um eine Rundung zu verschieben
    /// (<see cref="Kalenderleser.Rundlaeuft"/>).</item>
    /// <item><b>Zonenwerte bleiben</b> (F2) — legt ein Gebäude seinen Kalender an, während Zonen
    /// eigene Zellen tragen, legt <see cref="Anlegen"/> deren abgeleitete Kalender <b>mit</b> an.</item>
    /// <item><b>Energie bleibt</b> (P1) — der Geräte-Nennwert wird beim Anlegen eines
    /// Personenkalenders um das Jahresmittel der Personenwärme gesenkt
    /// (<see cref="PersonenNennwertVorschlag"/> und <see cref="GeraeteNennwertNachPersonen"/>).</item>
    /// </list>
    ///
    /// <para><b>Die Eindeutigkeit halten die acht Teilindizes</b> des Schemaschritts
    /// <see cref="KonditionierungVorlagenSchema.SCHRITT"/> (Konzept 5.1): ein Kalender je
    /// Eigentümer und Größe, eine Vorgabezeile je Eigentümer, Größe und Zeile. Der Controller
    /// ersetzt vorher, was er überschreibt — so läuft er nicht in den Datenbankfall, und ein
    /// Datenbankstand vor dem Schritt bleibt ebenso eindeutig.</para>
    ///
    /// <para><b>Das Schloss</b> (Konzept 3.4, 5.7): Ein Katalogbau oder eine Vorlage mit
    /// <c>ReadOnly = 1</c> gehört zur Auslieferung; jeder Schreibweg auf ihn wird <b>benannt
    /// abgelehnt</b>, auch der auf Matrix und Kalender — eine eigene Spalte dafür gibt es nicht.</para>
    /// </summary>
    public sealed class KonditionierungCtrl
    {
        /// <summary>Was ein Schreibversuch ergeben hat — dieselbe Form wie <c>GebaeudeZonenCtrl.Ergebnis</c>.</summary>
        public sealed record Ergebnis(bool Ok, string Meldung)
        {
            /// <summary>Der gute Fall.</summary>
            public static readonly Ergebnis Gut = new Ergebnis(true, "");

            /// <summary>Der benannte Fehlschlag.</summary>
            public static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "");
        }

        /// <summary>
        /// <b>Der Eigentümer eines Schreibwegs</b> — genau einer der vier (Konzept 5.1). Ein
        /// Wertträger, kein Zustand: Jeder Aufruf nennt seinen Eigentümer selbst.
        /// </summary>
        public sealed record Eigner(Kalendereigentuemer Art, long IdGebaeude, long? IdZone, long IdStamm,
                                    long IdVorlage)
        {
            /// <summary>Ein Projektgebäude ohne Zone.</summary>
            public static Eigner Gebaeude(long id) => new Eigner(Kalendereigentuemer.Gebaeude, id, null, 0, 0);

            /// <summary>Eine Zone; <paramref name="idGebaeude"/> ist das Gebäude der Zone (Konsistenzregel).</summary>
            public static Eigner Zone(long idGebaeude, long idZone)
                => new Eigner(Kalendereigentuemer.Zone, idGebaeude, idZone, 0, 0);

            /// <summary>Ein Katalogbau (P3 (b)).</summary>
            public static Eigner Katalogbau(long id) => new Eigner(Kalendereigentuemer.Katalogbau, 0, null, id, 0);

            /// <summary>
            /// Eine Vorlage (P11, Konzept 5.7) — ihr Inhalt steht in <b>einer</b> Größe; der
            /// Vorlagen-Controller kommt mit D3.
            /// </summary>
            public static Eigner Vorlage(long id) => new Eigner(Kalendereigentuemer.Vorlage, 0, null, 0, id);

            /// <summary>Die Spalten- und Wertpaare des Eigentümers für <c>WHERE</c> und <c>INSERT</c>.</summary>
            internal string Bedingung()
            {
                switch (Art)
                {
                    case Kalendereigentuemer.Gebaeude:
                        return "\"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL";
                    case Kalendereigentuemer.Zone:
                        return "\"ID_Gebaeude\" = ? AND \"ID_Zone\" = ?";
                    case Kalendereigentuemer.Katalogbau:
                        return "\"ID_Gebaeude_Stamm\" = ?";
                    default:
                        return "\"ID_Vorlage\" = ?";
                }
            }

            /// <summary>Die Parameter zu <see cref="Bedingung"/>, in derselben Reihenfolge.</summary>
            internal DbParam[] Parameter()
            {
                switch (Art)
                {
                    case Kalendereigentuemer.Gebaeude:
                        return new[] { new DbParam("@g", IdGebaeude) };
                    case Kalendereigentuemer.Zone:
                        return new[] { new DbParam("@g", IdGebaeude), new DbParam("@z", IdZone.Value) };
                    case Kalendereigentuemer.Katalogbau:
                        return new[] { new DbParam("@s", IdStamm) };
                    default:
                        return new[] { new DbParam("@v", IdVorlage) };
                }
            }

            /// <summary>Die vier Eigentümerspalten als Werte für ein <c>INSERT</c> (NULL, wo sie nicht gilt).</summary>
            internal DbParam[] Spaltenwerte(string praefix)
                => new[]
                {
                    new DbParam(praefix + "g", Art == Kalendereigentuemer.Gebaeude || Art == Kalendereigentuemer.Zone
                        ? (object)IdGebaeude : null),
                    new DbParam(praefix + "z", Art == Kalendereigentuemer.Zone ? (object)IdZone.Value : null),
                    new DbParam(praefix + "s", Art == Kalendereigentuemer.Katalogbau ? (object)IdStamm : null),
                    new DbParam(praefix + "v", Art == Kalendereigentuemer.Vorlage ? (object)IdVorlage : null),
                };
        }

        // =================================================================
        //  Das Schloss (Konzept 3.4, 5.7)
        // =================================================================

        /// <summary>
        /// <b>Darf auf diesen Eigentümer geschrieben werden?</b> Ein Katalogbau
        /// (<c>Tab_Gebaeude_STAMM.ReadOnly = 1</c>) und eine Vorlage
        /// (<c>Tab_Konditionierungsvorlage_STAMM.ReadOnly = 1</c>) gehören zur Auslieferung; ihr
        /// Schloss gilt auch für Matrix und Kalender. Ein Projektgebäude und eine Zone tragen
        /// keins.
        ///
        /// <para>Gelesen wird ohne Vorgang — das Schloss steht in der Datenbank, nicht im
        /// Arbeitsstand; die Prüfung läuft VOR dem Schreibvorgang.</para>
        /// </summary>
        /// <returns><c>null</c>, wenn geschrieben werden darf, sonst die benannte Ablehnung.</returns>
        public static string Schloss(Eigner eigner)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            switch (eigner.Art)
            {
                case Kalendereigentuemer.Katalogbau:
                    return Gesperrt(Matrixzellenort.TAB_KATALOGBAU, eigner.IdStamm)
                        ? string.Format(CultureInfo.CurrentCulture,
                                        MyResource.Resource.KOND_MSG_KATALOGBAU_GESPERRT,
                                        eigner.IdStamm.ToString(CultureInfo.InvariantCulture))
                        : null;

                case Kalendereigentuemer.Vorlage:
                    if (!KonditionierungVorlagenSchema.Lesbar())
                        return string.Format(CultureInfo.CurrentCulture,
                                             MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG,
                                             KonditionierungVorlagenSchema.SCHRITT);
                    return Gesperrt(KonditionierungVorlagenSchema.TAB_VORLAGE, eigner.IdVorlage)
                        ? string.Format(CultureInfo.CurrentCulture,
                                        MyResource.Resource.KOND_MSG_VORLAGE_GESPERRT,
                                        eigner.IdVorlage.ToString(CultureInfo.InvariantCulture))
                        : null;

                default:
                    return null;
            }
        }

        /// <summary>Trägt der Satz <paramref name="id"/> der Tabelle das Auslieferungskennzeichen?</summary>
        private static bool Gesperrt(string tabelle, long id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"ID\" = ? AND \"ReadOnly\" = 1",
                new DbParam("@id", id));
            return o != null && o != DBNull.Value && Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>
        /// <b>Die Vorgabezeilen eines Eigentümers.</b> Leere Liste, wenn die Tabellen fehlen (ein
        /// Datenbankstand vor Schritt <see cref="KonditionierungSchema.SCHRITT"/>).
        /// </summary>
        public List<Vorgabezeile> Vorgaben(Eigner eigner)
        {
            var liste = new List<Vorgabezeile>();
            if (!KonditionierungSchema.Lesbar()) return liste;
            DataTable t = DataRepository.GetDataTable(
                "SELECT ID, ID_Gebaeude, ID_Zone, ID_Gebaeude_Stamm, ID_Vorlage, Groesse, Zeile, Wert, Aus, " +
                "Von, Bis, Bedingt_K FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE " +
                eigner.Bedingung() + " ORDER BY Groesse, Zeile, ID", eigner.Parameter());
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(new Vorgabezeile
                {
                    Id = Lang(r, "ID") ?? 0,
                    IdGebaeude = Lang(r, "ID_Gebaeude"),
                    IdZone = Lang(r, "ID_Zone"),
                    IdGebaeudeStamm = Lang(r, "ID_Gebaeude_Stamm"),
                    IdVorlage = Lang(r, "ID_Vorlage"),
                    Groesse = Text(r, "Groesse"),
                    Zeile = Text(r, "Zeile"),
                    Wert = Zahl(r, "Wert"),
                    Aus = (Lang(r, "Aus") ?? 0) != 0,
                    Von = Ganz(r, "Von"),
                    Bis = Ganz(r, "Bis"),
                    BedingtK = Zahl(r, "Bedingt_K"),
                });
            return liste;
        }

        /// <summary>
        /// <b>Die angelegten Kalender eines Eigentümers je Größe</b>, gelesen mit dem strengen Leser;
        /// eine ungültige Zeile ist ein <b>benannter</b> Befund in <paramref name="meldung"/> und
        /// fehlt im Ergebnis, statt still umgedeutet zu werden.
        /// </summary>
        public Dictionary<Konditionierungsgroesse, Konditionierungskalender> Kalender(
            Eigner eigner, out string meldung)
        {
            meldung = null;
            var ziel = new Dictionary<Konditionierungsgroesse, Konditionierungskalender>();
            if (!KonditionierungSchema.Lesbar()) return ziel;

            DataTable t = DataRepository.GetDataTable(
                "SELECT ID, ID_Gebaeude, ID_Zone, ID_Gebaeude_Stamm, ID_Vorlage, Groesse, Wert, Aus, Woche, " +
                "Nennwert, Bemerkung FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                eigner.Bedingung() + " ORDER BY Groesse, ID", eigner.Parameter());
            if (t == null || t.Rows.Count == 0) return ziel;

            var zeilen = new List<Kalenderzeile>();
            foreach (DataRow r in t.Rows)
                zeilen.Add(new Kalenderzeile
                {
                    Id = Lang(r, "ID") ?? 0,
                    IdGebaeude = Lang(r, "ID_Gebaeude"),
                    IdZone = Lang(r, "ID_Zone"),
                    IdGebaeudeStamm = Lang(r, "ID_Gebaeude_Stamm"),
                    IdVorlage = Lang(r, "ID_Vorlage"),
                    Groesse = Text(r, "Groesse"),
                    Wert = Zahl(r, "Wert"),
                    Aus = (Lang(r, "Aus") ?? 0) != 0,
                    Woche = Text(r, "Woche"),
                    Nennwert = Zahl(r, "Nennwert"),
                    Bemerkung = Text(r, "Bemerkung"),
                });

            List<Periodenzeile> perioden = Perioden(zeilen);
            foreach (Kalenderzeile z in zeilen)
            {
                Kalenderlesung l = Kalenderleser.Lesen(z, perioden);
                if (l.Befund != Kalenderbefund.Gelesen)
                {
                    meldung = string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KOND_KALENDER_UNGUELTIG, l.Befund.ToString(), l.Fundstelle());
                    continue;
                }
                ziel[l.Kalender.Groesse] = l.Kalender;
            }
            return ziel;
        }

        /// <summary>Die Perioden der übergebenen Kalenderzeilen — eine Abfrage je Kalender.</summary>
        private static List<Periodenzeile> Perioden(IEnumerable<Kalenderzeile> zeilen)
        {
            var liste = new List<Periodenzeile>();
            foreach (Kalenderzeile z in zeilen)
            {
                DataTable t = DataRepository.GetDataTable(
                    "SELECT ID, ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Feiertagsregel, Wert, Aus, " +
                    "Woche, WieWochentag FROM \"" + KonditionierungSchema.TAB_PERIODE +
                    "\" WHERE ID_Kalender = ? ORDER BY Rang DESC", new DbParam("@k", z.Id));
                if (t == null) continue;
                foreach (DataRow r in t.Rows)
                    liste.Add(new Periodenzeile
                    {
                        Id = Lang(r, "ID") ?? 0,
                        IdKalender = Lang(r, "ID_Kalender") ?? 0,
                        Rang = (int)(Lang(r, "Rang") ?? 0),
                        Art = Text(r, "Art"),
                        Bezeichner = Text(r, "Bezeichner"),
                        Beginn = Ganz(r, "Beginn"),
                        Ende = Ganz(r, "Ende"),
                        Feiertagsregel = Text(r, "Feiertagsregel"),
                        Wert = Zahl(r, "Wert"),
                        Aus = (Lang(r, "Aus") ?? 0) != 0,
                        Woche = Text(r, "Woche"),
                        WieWochentag = Ganz(r, "WieWochentag"),
                    });
            }
            return liste;
        }

        // =================================================================
        //  Anlegen, Verwerfen, erneut Anwenden
        // =================================================================

        /// <summary>
        /// <b>„Kalender anlegen"</b> (Konzept 3.3): Der Generator schreibt die Größe
        /// <paramref name="groesse"/> in Zeilen. Der <b>Rundlauf</b> wird vorher geprüft; scheitert er,
        /// wird benannt abgelehnt und <b>nichts</b> geschrieben. Ein vorhandener Kalender derselben
        /// Größe wird ersetzt — der Aufrufer hat die Rückfrage geführt.
        /// </summary>
        /// <param name="eigner">Wem der Kalender gehört.</param>
        /// <param name="matrix">Die wirksame Matrix des Eigentümers (nach der Kaskade, F2).</param>
        /// <param name="groesse">Die Größe, deren Kalender entsteht.</param>
        public Ergebnis Anlegen(Eigner eigner, Vorgabematrix matrix, Konditionierungsgroesse groesse)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            if (!KonditionierungSchema.Lesbar())
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG, KonditionierungSchema.SCHRITT));
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);

            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, groesse, rundlaufPruefen: true);
            if (l.Befund == Fahrplanbefund.KeineAngabe)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT, l.Befund.ToString(), l.Fundstelle()));
            if (l.Befund != Fahrplanbefund.Erzeugt)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT, l.Befund.ToString(), l.Fundstelle()));

            return Schreiben(eigner, l.Kalender);
        }

        /// <summary>
        /// <b>Schreibt einen Kalender</b> samt Perioden in <b>einer</b> Transaktion und ersetzt einen
        /// vorhandenen derselben Größe (die Perioden fallen über die Kaskade). Der Rundlauf wird noch
        /// einmal geprüft — auch ein von Hand gebauter Kalender darf keine Reihe verschieben.
        /// </summary>
        public Ergebnis Schreiben(Eigner eigner, Konditionierungskalender kalender)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);
            if (!Kalenderleser.Rundlaeuft(kalender, out int rang, out int stelle))
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                    Fahrplanbefund.RundlaufVerletzt.ToString(),
                    "Rang " + rang.ToString(CultureInfo.InvariantCulture) + ", Stelle " +
                    stelle.ToString(CultureInfo.InvariantCulture)));

            var zeile = new Kalenderzeile();
            var perioden = new List<Periodenzeile>();
            try
            {
                Kalenderleser.Schreiben(kalender, zeile, perioden);
            }
            catch (ArgumentException ex)
            {
                return Ergebnis.Fehler(ex.Message);
            }

            string groesse = Konditionierungsgroessen.Kennwort(kalender.Groesse);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // Der vorhandene Kalender dieser Groesse faellt samt Perioden (Kaskade); so laeuft
                    // das Ersetzen nicht in den Teilindex der Eindeutigkeit (Konzept 5.1).
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                                 eigner.Bedingung() + " AND \"Groesse\" = ?",
                                 Mit(eigner.Parameter(), new DbParam("@gr", groesse)));

                    v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER +
                                 "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Vorlage\", " +
                                 "\"Groesse\", \"Wert\", \"Aus\", \"Woche\", \"Nennwert\", \"Bemerkung\") " +
                                 "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                 Mit(eigner.Spaltenwerte("@e"),
                                     new DbParam("@gr", groesse),
                                     new DbParam("@w", (object)zeile.Wert),
                                     new DbParam("@a", zeile.Aus ? 1 : 0),
                                     new DbParam("@wo", (object)zeile.Woche),
                                     new DbParam("@nw", (object)zeile.Nennwert),
                                     new DbParam("@bm", (object)zeile.Bemerkung)));

                    object neu = v.Skalar("SELECT last_insert_rowid()");
                    long idKalender = Convert.ToInt64(neu, CultureInfo.InvariantCulture);

                    foreach (Periodenzeile p in perioden)
                        v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE +
                                     "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", \"Ende\", " +
                                     "\"Feiertagsregel\", \"Wert\", \"Aus\", \"Woche\", \"WieWochentag\") " +
                                     "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                     new DbParam("@k", idKalender),
                                     new DbParam("@r", p.Rang),
                                     new DbParam("@ar", p.Art),
                                     new DbParam("@bz", p.Bezeichner),
                                     new DbParam("@vo", (object)p.Beginn),
                                     new DbParam("@bi", (object)p.Ende),
                                     new DbParam("@ft", (object)p.Feiertagsregel),
                                     new DbParam("@we", (object)p.Wert),
                                     new DbParam("@au", p.Aus ? 1 : 0),
                                     new DbParam("@wo", (object)p.Woche),
                                     new DbParam("@ww", (object)p.WieWochentag));
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return Ergebnis.Fehler(ex.Message);
                }
            }
            return Ergebnis.Gut;
        }

        /// <summary>
        /// <b>„Verwerfen"</b> (Konzept 3.3): Der angelegte Kalender einer Größe fällt samt Perioden —
        /// danach rechnet der Lauf wieder <b>abgeleitet</b> aus der Matrix. Die Vorgabezeilen bleiben;
        /// sie sind die Matrix, nicht der Kalender.
        /// </summary>
        public Ergebnis Verwerfen(Eigner eigner, Konditionierungsgroesse groesse)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (!KonditionierungSchema.Lesbar()) return Ergebnis.Gut;
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);
            try
            {
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " + eigner.Bedingung() +
                    " AND \"Groesse\" = ?",
                    Mit(eigner.Parameter(), new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse))));
                return Ergebnis.Gut;
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(ex.Message);
            }
        }

        /// <summary>
        /// <b>„Matrix erneut anwenden"</b> (P12 (a)): Auf einen angelegten, geänderten Kalender wird
        /// <b>nur der Matrixbereich</b> ersetzt — Standardwoche, Ferien- und Saisonperioden; <b>eigene
        /// Perioden und Ausnahmetage bleiben</b>. Die Rückfrage führt der Aufrufer; sie nennt, was
        /// ersetzt wird und was bleibt.
        ///
        /// <para>Der Matrixbereich ist genau, was der Generator schreibt: die Grundangabe samt
        /// Standardwoche und die Perioden der Art <c>FERIEN</c> und <c>BETRIEBSPAUSE</c>, die er
        /// vergibt. Jede andere Periode — <c>ZEITRAUM</c>, <c>FEIERTAG</c> — gehört dem Anwender.</para>
        /// </summary>
        public Ergebnis ErneutAnwenden(Eigner eigner, Vorgabematrix matrix, Konditionierungsgroesse groesse)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);

            Dictionary<Konditionierungsgroesse, Konditionierungskalender> vorhanden = Kalender(eigner, out string m);
            if (m != null) return Ergebnis.Fehler(m);
            if (!vorhanden.TryGetValue(groesse, out Konditionierungskalender alt))
                return Anlegen(eigner, matrix, groesse);      // nichts angelegt: der gewöhnliche Weg

            Fahrplanlesung l = Standardfahrplan.Erzeugen(matrix, groesse, rundlaufPruefen: true);
            if (l.Befund != Fahrplanbefund.Erzeugt)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT, l.Befund.ToString(), l.Fundstelle()));

            // Die eigenen Perioden des Bestands behalten, die des Matrixbereichs ersetzen.
            var perioden = new List<Kalenderregel>();
            foreach (Kalenderregel r in alt.Perioden)
                if (!IstMatrixbereich(r)) perioden.Add(r);
            foreach (Kalenderregel r in l.Kalender.Perioden)
                perioden.Add(r);

            // Ein Rang darf nicht zweimal vorkommen; die eigenen Perioden behalten ihren, eine
            // Kollision wird benannt abgelehnt statt still verschoben.
            var raenge = new HashSet<int>();
            foreach (Kalenderregel r in perioden)
                if (!raenge.Add(r.Rang))
                    return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KOND_FAHRPLAN_ABGELEHNT,
                        Fahrplanbefund.RundlaufVerletzt.ToString(),
                        "Rang " + r.Rang.ToString(CultureInfo.InvariantCulture) + " doppelt"));

            var neu = new Konditionierungskalender(groesse, l.Kalender.Grundangabe, l.Kalender.Nennwert, perioden);
            return Schreiben(eigner, neu);
        }

        /// <summary>
        /// Gehört die Periode zum <b>Matrixbereich</b> (P12)? Genau die Arten, die der Generator
        /// vergibt: <c>FERIEN</c> (die Ferienzeiträume) und <c>BETRIEBSPAUSE</c> (die Saison).
        /// <c>ZEITRAUM</c> und <c>FEIERTAG</c> gehören dem Anwender und bleiben.
        /// </summary>
        public static bool IstMatrixbereich(Kalenderregel r)
            => r != null
               && (string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal)
                   || string.Equals(r.Art, DbWerte.KOND_ART_BETRIEBSPAUSE, StringComparison.Ordinal));

        // =================================================================
        //  Vorgabezeilen schreiben
        // =================================================================

        /// <summary>
        /// <b>Schreibt eine Zelle der Vorgabe-Matrix</b> (Konzept 5.6). Eine Zeile je Eigentümer,
        /// Größe und Zeile: Die vorhandene wird ersetzt. Ist die Zelle in jedem Feld leer, fällt die
        /// Zeile — leer heißt „wie die Ebene darüber", und eine leere Zeile wäre eine Leerstelle mit
        /// Id.
        ///
        /// <para><b>Ein Ort je Zelle</b> (Konzept 5.6, Weiche <see cref="Matrixzellenort"/>): Hat
        /// die Zelle am Eigentümer eine <b>Bestandsspalte</b>, geht ihr Zahlenwert dorthin — in
        /// DERSELBEN Transaktion —, und die Vorgabezeile trägt nur „aus", die Zeiten und
        /// <c>Bedingt_K</c>. Eine solche Zelle kann nicht „leer" sein: Die Bestandsspalte führt
        /// immer einen Wert; eine unbelegte Zelle lässt sie deshalb unberührt. Hat die Zelle keine
        /// Bestandsspalte — an einer Vorlage nirgends (Konzept 5.7) —, steht ihr Wert wie bisher in
        /// der Vorgabezeile.</para>
        /// </summary>
        public Ergebnis Vorgabe(Eigner eigner, Konditionierungsgroesse groesse, string zeile,
                                Matrixzelle zelle)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (zelle == null) throw new ArgumentNullException(nameof(zelle));
            if (!KonditionierungSchema.Lesbar())
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG, KonditionierungSchema.SCHRITT));
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);
            bool bekannt = false;
            foreach (string z in DbWerte.KOND_ZEILEN)
                if (string.Equals(zeile, z, StringComparison.Ordinal)) { bekannt = true; break; }
            if (!bekannt) return Ergebnis.Fehler("Die Zeile „" + (zeile ?? "leer") + "“ ist keine der sechs.");

            // Die Grenzen der Groesse gelten den WERTZEILEN (Tag, Nacht, Wochenende, Ferien). Die
            // Zeile NENNWERT traegt bei den Lasten einen Wattwert - nicht den Anteil 0 … 1 -, bei der
            // Lueftung die Infiltration in 1/h; die Zeile SAISON traegt keinen Wert, nur Tage (E53).
            if (zelle.Belegt && !zelle.Aus)
            {
                if (string.Equals(zeile, DbWerte.KOND_ZEILE_NENNWERT, StringComparison.Ordinal))
                {
                    if (Konditionierungsgroessen.HatNennwert(groesse))
                    {
                        if (!(zelle.Wert >= 0.0) || double.IsInfinity(zelle.Wert))
                            return Ergebnis.Fehler("Der Nennwert " + Zahltext(zelle.Wert) +
                                                   " W ist negativ oder nicht endlich.");
                    }
                    else if (!Konditionierungsgroessen.ImBereich(groesse, zelle.Wert))
                        return Ergebnis.Fehler("Der Wert liegt außerhalb der Grenzen " +
                                               Konditionierungsgroessen.Bereichstext(groesse) + ".");
                }
                else if (!string.Equals(zeile, DbWerte.KOND_ZEILE_SAISON, StringComparison.Ordinal)
                         && !Konditionierungsgroessen.ImBereich(groesse, zelle.Wert))
                    return Ergebnis.Fehler("Der Wert liegt außerhalb der Grenzen " +
                                           Konditionierungsgroessen.Bereichstext(groesse) + ".");
            }

            string gr = Konditionierungsgroessen.Kennwort(groesse);

            // DIE WEICHE (Konzept 5.6): Wo es eine Bestandsspalte gibt, gehoert der Zahlenwert
            // dorthin - die Vorgabezeile traegt dann nur "aus", Zeiten und Bedingt_K.
            Matrixzellenort.Ort ort = Matrixzellenort.Fuer(eigner.Art, groesse, zeile);
            bool inBestandsspalte = ort.IstBestandsspalte;
            object wert = zelle.Belegt && !zelle.Aus && !inBestandsspalte ? (object)zelle.Wert : null;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    if (inBestandsspalte && zelle.Belegt && !zelle.Aus)
                    {
                        int zeilen = v.Ausfuehren(
                            "UPDATE \"" + ort.Tabelle + "\" SET \"" + ort.Spalte + "\" = ? WHERE \"ID\" = ?",
                            new DbParam("@w", zelle.Wert),
                            new DbParam("@id", Traegerid(eigner)));
                        if (zeilen == 0)
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.KOND_MSG_EIGNER_FEHLT, ort.Tabelle,
                                Traegerid(eigner).ToString(CultureInfo.InvariantCulture)));
                        }
                    }

                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE " +
                                 eigner.Bedingung() + " AND \"Groesse\" = ? AND \"Zeile\" = ?",
                                 Mit(eigner.Parameter(), new DbParam("@gr", gr), new DbParam("@ze", zeile)));

                    bool leer = wert == null && !zelle.Aus && !zelle.Von.HasValue && !zelle.Bis.HasValue
                                && !zelle.BedingtK.HasValue;
                    if (!leer)
                        v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                                     "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Vorlage\", " +
                                     "\"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\") " +
                                     "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                     Mit(eigner.Spaltenwerte("@e"),
                                         new DbParam("@gr", gr),
                                         new DbParam("@ze", zeile),
                                         new DbParam("@we", wert),
                                         new DbParam("@au", zelle.Aus ? 1 : 0),
                                         new DbParam("@vo", (object)zelle.Von),
                                         new DbParam("@bi", (object)zelle.Bis),
                                         new DbParam("@bk", (object)zelle.BedingtK)));
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return Ergebnis.Fehler(ex.Message);
                }
            }
            return Ergebnis.Gut;
        }

        /// <summary>Die Id des Satzes, der die Bestandsspalten des Eigentümers trägt.</summary>
        private static long Traegerid(Eigner eigner)
        {
            switch (eigner.Art)
            {
                case Kalendereigentuemer.Gebaeude: return eigner.IdGebaeude;
                case Kalendereigentuemer.Zone: return eigner.IdZone.Value;
                case Kalendereigentuemer.Katalogbau: return eigner.IdStamm;
                default: return eigner.IdVorlage;
            }
        }

        // =================================================================
        //  Energie bleibt (P1)
        // =================================================================

        /// <summary>
        /// <b>Der Vorschlag für den Personen-Nennwert</b> [W] (Konzept 3.1): Personenzahl ×
        /// <see cref="Matrixeingang.PERSON_W"/>. Die Zahl kommt aus <c>Bewohner</c>, sonst aus
        /// Nutzfläche ÷ <c>Flaeche_Nutzer</c>; ohne beides 0.
        /// </summary>
        public static double PersonenNennwertVorschlag(double? bewohner, double nutzflaecheM2,
                                                       double? flaecheJeNutzer)
        {
            double zahl = bewohner.HasValue && bewohner.Value > 0.0
                ? bewohner.Value
                : flaecheJeNutzer.HasValue && flaecheJeNutzer.Value > 0.0 && nutzflaecheM2 > 0.0
                    ? nutzflaecheM2 / flaecheJeNutzer.Value
                    : 0.0;
            return zahl * Matrixeingang.PERSON_W;
        }

        /// <summary>
        /// <b>Der Geräte-Nennwert nach dem Anlegen eines Personenkalenders</b> (P1 (b),
        /// energieerhaltend): <c>Interne_Waermegewinne</c> − <b>Jahresmittel</b> der Personenwärme.
        /// Nie unter null; die Karte zeigt die Rechnung und beide Jahresmittel.
        /// </summary>
        /// <param name="interneWaermegewinneW">Der heutige Dauerwert [W].</param>
        /// <param name="personen">Der Personenkalender (Anteile × Nennwert).</param>
        /// <param name="w0">w₀ des Laufs.</param>
        /// <param name="referenzjahr">Das Referenzjahr.</param>
        public static double GeraeteNennwertNachPersonen(double interneWaermegewinneW,
                                                        Konditionierungskalender personen,
                                                        int w0, int referenzjahr)
        {
            if (personen == null) return interneWaermegewinneW;
            double mittel = PersonenJahresmittelW(personen, w0, referenzjahr);
            double rest = interneWaermegewinneW - mittel;
            return rest > 0.0 ? rest : 0.0;
        }

        /// <summary>Das Jahresmittel der Personenwärme [W] — Anteil × Nennwert über 8 760 Stunden.</summary>
        public static double PersonenJahresmittelW(Konditionierungskalender personen, int w0, int referenzjahr)
        {
            if (personen == null) return 0.0;
            double nennwert = personen.Nennwert ?? 0.0;
            double[] anteil = personen.Auswerten(w0, referenzjahr);
            double summe = 0.0;
            foreach (double a in anteil) summe += a;
            return summe * nennwert / anteil.Length;
        }

        // =================================================================
        //  Kleine Helfer
        // =================================================================

        private static string Zahltext(double w) => w.ToString("G6", CultureInfo.InvariantCulture);

        private static DbParam[] Mit(DbParam[] erste, params DbParam[] weitere)
        {
            var alle = new DbParam[erste.Length + weitere.Length];
            Array.Copy(erste, alle, erste.Length);
            Array.Copy(weitere, 0, alle, erste.Length, weitere.Length);
            return alle;
        }

        private static long? Lang(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture)
                : (long?)null;

        private static int? Ganz(DataRow r, string spalte)
        {
            long? l = Lang(r, spalte);
            return l.HasValue ? (int)l.Value : (int?)null;
        }

        private static double? Zahl(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture)
                : (double?)null;

        private static string Text(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture)
                : null;
    }
}
