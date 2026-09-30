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

            /// <summary>
            /// <b>Die Id des Satzes, der die Bestandsspalten dieses Eigentümers trägt</b> — das
            /// Gebäude, die Zone, der Katalogbau oder die Vorlage (die keine führt,
            /// <see cref="Matrixzellenort"/>). EINE Stelle: Schreibweg und Vorlagen-Controller
            /// fragen dieselbe.
            /// </summary>
            internal long Traegerid
            {
                get
                {
                    switch (Art)
                    {
                        case Kalendereigentuemer.Gebaeude: return IdGebaeude;
                        case Kalendereigentuemer.Zone: return IdZone.Value;
                        case Kalendereigentuemer.Katalogbau: return IdStamm;
                        default: return IdVorlage;
                    }
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
        //  Der Stand einer Ebene — Lesen und Schreiben (Stufe KP2, Welle K2)
        // =================================================================

        /// <summary>Die Bestandsspalten der Zone (Konzept 5.6): die neun Bestandszellen samt Bewohner und Maximaltemperatur.</summary>
        private static readonly string[] SPALTEN_ZONE =
        {
            "Raumsolltemperatur_Tag", "Raumsolltemperatur_Nachtabsenkung", "Raumsolltemperatur_Wochenende",
            "Raumsolltemperatur_Ferien", "Kuehl_Sollwert", "Kuehl_Sollwert_Nacht", "Luftwechsel_Infiltration",
            "Luftwechsel_Nutzer", "Interne_Waermegewinne", "Bewohner", "Maximaleraumtemperatur",
        };

        /// <summary>
        /// <b>Die Ebene eines Eigentümers aus der Datenbank</b> — Bestandsfelder aus seiner Zeile
        /// (<see cref="BestandLesen"/>), Vorgabezeilen, angelegte Kalender und ihre Herkunft
        /// (<c>Bemerkung</c>). Eine ungültige Kalenderzeile fehlt und steht benannt in
        /// <paramref name="meldung"/>. Läuft unter einer <see cref="Vorgangsklammer"/> im Vorgang.
        /// </summary>
        public Konditionierungsstand StandLesen(Eigner eigner, out string meldung)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            Matrixeingang bestand = BestandLesen(eigner);
            List<Vorgabezeile> vorgaben = Vorgaben(eigner);
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender = Kalender(eigner, out meldung);
            return Konditionierungsstand.Aus(eigner.Art, bestand, vorgaben, kalender, Bemerkungen(eigner));
        }

        /// <summary>Die Spalte <c>Bemerkung</c> je Größe — Herkunft und Vermerk (B8).</summary>
        private static Dictionary<Konditionierungsgroesse, string> Bemerkungen(Eigner eigner)
        {
            var d = new Dictionary<Konditionierungsgroesse, string>();
            if (!KonditionierungSchema.Lesbar()) return d;
            DataTable t = DataRepository.GetDataTable(
                "SELECT Groesse, Bemerkung FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                eigner.Bedingung() + " ORDER BY Groesse, ID", eigner.Parameter());
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
                if (Konditionierungsgroessen.AusKennwort(Text(r, "Groesse"), out Konditionierungsgroesse g) && !d.ContainsKey(g))
                    d[g] = Text(r, "Bemerkung");
            return d;
        }

        /// <summary>
        /// <b>Die Bestandsfelder aus der Zeile des Eigentümers</b> — dieselbe Abbildung wie
        /// <see cref="Konditionierungseingang.Bestand"/>: die neun Bestandszellen, Bewohner und
        /// Maximaltemperatur; an Gebäude und Katalogbau dazu Nachtzeiten, Merker, Ferienzeiträume,
        /// <c>Luftwechselrate</c> und <c>Sollwertprofil</c>. Die Schalter des Laufs (Kopplung, Kühlung)
        /// bleiben aus. Eine Vorlage hat keine Zeile — leer.
        /// </summary>
        internal static Matrixeingang BestandLesen(Eigner eigner)
        {
            var b = new Matrixeingang();
            string tabelle = Matrixzellenort.Tabelle(eigner.Art);
            if (tabelle == null) return b;
            DataTable t = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" WHERE \"ID\" = ?",
                                                      new DbParam("@id", eigner.Traegerid));
            if (t == null || t.Rows.Count == 0) return b;
            DataRow r = t.Rows[0];
            b.SollTag = Endlich(Zahl(r, "Raumsolltemperatur_Tag"));
            b.SollNacht = Endlich(Zahl(r, "Raumsolltemperatur_Nachtabsenkung"));
            b.SollWochenende = Endlich(Zahl(r, "Raumsolltemperatur_Wochenende"));
            b.SollFerien = Endlich(Zahl(r, "Raumsolltemperatur_Ferien"));
            b.KuehlSollwert = Zahl(r, "Kuehl_Sollwert");
            b.KuehlSollwertNacht = Zahl(r, "Kuehl_Sollwert_Nacht");
            b.LuftwechselInfiltration = Zahl(r, "Luftwechsel_Infiltration");
            b.LuftwechselNutzer = Zahl(r, "Luftwechsel_Nutzer");
            b.InterneWaermegewinne = Endlich(Zahl(r, "Interne_Waermegewinne"));
            b.Bewohner = Zahl(r, "Bewohner");
            b.Maximaleraumtemperatur = Endlich(Zahl(r, "Maximaleraumtemperatur"));
            if (Matrixzellenort.Nachtzeittabelle(eigner.Art) != null)
            {
                b.NachtBeginn = Ganz(r, "Nachtabsenkung_Beginn");
                b.NachtEnde = Ganz(r, "Nachtabsenkung_Ende");
                b.Ferienmerker = Zahl(r, "Ferien") ?? 0.0;
                b.Wochenendmerker = Zahl(r, "Wochenende") ?? 0.0;
                b.Luftwechselrate = Endlich(Zahl(r, "Luftwechselrate"));
                b.Sollwertprofil = Text(r, "Sollwertprofil");
                for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
                {
                    string k = (i + 1).ToString(CultureInfo.InvariantCulture);
                    b.Ferienbeginn[i] = Zahl(r, "Ferienbeginn_" + k) ?? 0.0;
                    b.Ferienende[i] = Zahl(r, "Ferienende_" + k) ?? 0.0;
                }
            }
            Konditionierungsarbeit.HerkunftDesLuftwechsels(b);
            return b;
        }

        /// <summary>
        /// <b>Schreibt eine Ebene im laufenden Vorgang</b> — nur, was sich gegen die Datenbank
        /// geändert hat: je Vorgabezelle, je Kalender samt Herkunft und mit
        /// <paramref name="mitBestand"/> je Bestandsspalte. Gleicht der Stand der Datenbank, wird nichts
        /// geschrieben (<paramref name="geschrieben"/> <c>false</c>) — ein zweites OK schreibt nichts
        /// doppelt. Die Klammer (<see cref="Vorgangsklammer"/>) und das Schloss hält der Aufrufer; die
        /// Zelle einer Bestandsspalte trägt in der Vorgabezeile nie einen Wert (Konzept 5.6).
        /// </summary>
        internal Ergebnis StandSchreiben(DbVorgang v, Eigner eigner, Konditionierungsstand neu, bool mitBestand,
                                         out bool geschrieben)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (neu == null) throw new ArgumentNullException(nameof(neu));
            geschrieben = false;
            if (!KonditionierungSchema.Lesbar())
                return neu.TabellenLeer && !mitBestand
                    ? Ergebnis.Gut
                    : Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG, KonditionierungSchema.SCHRITT));

            Konditionierungsstand alt = StandLesen(eigner, out _);

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                foreach (string zeile in DbWerte.KOND_ZEILEN)
                {
                    Matrixzelle n = neu.Vorgabe(g, zeile);
                    if (Kalendervergleich.ZelleGleich(alt.Vorgabe(g, zeile), n)) continue;
                    Ergebnis e = VorgabeSchreiben(v, eigner, g, zeile, n);
                    if (!e.Ok) return e;
                    geschrieben = true;
                }

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                Konditionierungskalender ka = alt.Kalender(g), kn = neu.Kalender(g);
                if (Kalendervergleich.KalenderGleich(ka, kn) && (kn == null || alt.Herkunft(g).Equals(neu.Herkunft(g))))
                    continue;
                if (kn == null)
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE " +
                                 eigner.Bedingung() + " AND \"Groesse\" = ?",
                                 Mit(eigner.Parameter(), new DbParam("@gr", Konditionierungsgroessen.Kennwort(g))));
                else
                {
                    Ergebnis e = KalenderSchreiben(v, eigner, kn, neu.Herkunft(g).Bemerkung());
                    if (!e.Ok) return e;
                }
                geschrieben = true;
            }

            if (mitBestand)
            {
                Ergebnis e = BestandSchreiben(v, eigner, alt.Bestand, neu.Bestand, out bool bestand);
                if (!e.Ok) return e;
                geschrieben |= bestand;
            }
            return Ergebnis.Gut;
        }

        /// <summary>Eine Vorgabezeile ersetzen — ohne Wert, wo die Zelle eine Bestandsspalte hat; eine leere Zelle hat keine Zeile.</summary>
        private static Ergebnis VorgabeSchreiben(DbVorgang v, Eigner eigner, Konditionierungsgroesse groesse, string zeile,
                                                 Matrixzelle zelle)
        {
            string gr = Konditionierungsgroessen.Kennwort(groesse);
            v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE " +
                         eigner.Bedingung() + " AND \"Groesse\" = ? AND \"Zeile\" = ?",
                         Mit(eigner.Parameter(), new DbParam("@gr", gr), new DbParam("@ze", zeile)));
            if (!Konditionierungsstand.Traegt(zelle)) return Ergebnis.Gut;
            bool bestandsspalte = Matrixzellenort.HatBestandsspalte(eigner.Art, groesse, zeile);
            object wert = zelle.Belegt && !zelle.Aus && !bestandsspalte ? (object)zelle.Wert : null;
            if (wert == null && !zelle.Aus && !zelle.Von.HasValue && !zelle.Bis.HasValue && !zelle.BedingtK.HasValue)
                return Ergebnis.Gut;
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
            return Ergebnis.Gut;
        }

        /// <summary>
        /// <b>Die geänderten Bestandsspalten schreiben</b> — die neun Bestandszellen; an Gebäude und
        /// Katalogbau dazu Nachtzeiten, Merker, Ferienzeiträume und <c>Luftwechselrate</c>. Je Spalte
        /// eine Anweisung mit dem Spaltennamen aus dieser Klasse und dem Wert als <c>?</c>.
        /// </summary>
        internal static Ergebnis BestandSchreiben(DbVorgang v, Eigner eigner, Matrixeingang alt, Matrixeingang neu,
                                                  out bool geschrieben)
        {
            geschrieben = false;
            string tabelle = Matrixzellenort.Tabelle(eigner.Art);
            if (tabelle == null) return Ergebnis.Gut;
            var aenderungen = new List<KeyValuePair<string, object>>();
            void Zahlfeld(string spalte, double? a, double? n)
            {
                if (!Kalendervergleich.Gleich(a, n)) aenderungen.Add(new KeyValuePair<string, object>(spalte, n));
            }
            Zahlfeld("Raumsolltemperatur_Tag", alt.SollTag, neu.SollTag);
            Zahlfeld("Raumsolltemperatur_Nachtabsenkung", alt.SollNacht, neu.SollNacht);
            Zahlfeld("Raumsolltemperatur_Wochenende", alt.SollWochenende, neu.SollWochenende);
            Zahlfeld("Raumsolltemperatur_Ferien", alt.SollFerien, neu.SollFerien);
            Zahlfeld("Kuehl_Sollwert", alt.KuehlSollwert, neu.KuehlSollwert);
            Zahlfeld("Kuehl_Sollwert_Nacht", alt.KuehlSollwertNacht, neu.KuehlSollwertNacht);
            Zahlfeld("Luftwechsel_Infiltration", alt.LuftwechselInfiltration, neu.LuftwechselInfiltration);
            Zahlfeld("Luftwechsel_Nutzer", alt.LuftwechselNutzer, neu.LuftwechselNutzer);
            Zahlfeld("Interne_Waermegewinne", alt.InterneWaermegewinne, neu.InterneWaermegewinne);
            if (Matrixzellenort.Nachtzeittabelle(eigner.Art) != null)
            {
                if (alt.NachtBeginn != neu.NachtBeginn)
                    aenderungen.Add(new KeyValuePair<string, object>(Matrixzellenort.SPALTE_NACHT_BEGINN, neu.NachtBeginn));
                if (alt.NachtEnde != neu.NachtEnde)
                    aenderungen.Add(new KeyValuePair<string, object>(Matrixzellenort.SPALTE_NACHT_ENDE, neu.NachtEnde));
                Zahlfeld("Ferien", alt.Ferienmerker, neu.Ferienmerker);
                Zahlfeld("Wochenende", alt.Wochenendmerker, neu.Wochenendmerker);
                Zahlfeld("Luftwechselrate", alt.Luftwechselrate, neu.Luftwechselrate);
                for (int i = 0; i < Matrixeingang.FERIENZEITRAEUME; i++)
                {
                    string k = (i + 1).ToString(CultureInfo.InvariantCulture);
                    Zahlfeld("Ferienbeginn_" + k, alt.Ferienbeginn[i], neu.Ferienbeginn[i]);
                    Zahlfeld("Ferienende_" + k, alt.Ferienende[i], neu.Ferienende[i]);
                }
            }

            foreach (KeyValuePair<string, object> a in aenderungen)
            {
                int zeilen = v.Ausfuehren("UPDATE \"" + tabelle + "\" SET \"" + a.Key + "\" = ? WHERE \"ID\" = ?",
                                          new DbParam("@w", a.Value), new DbParam("@id", eigner.Traegerid));
                if (zeilen == 0)
                    return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_EIGNER_FEHLT, tabelle,
                        eigner.Traegerid.ToString(CultureInfo.InvariantCulture)));
                geschrieben = true;
            }
            return Ergebnis.Gut;
        }

        /// <summary>
        /// <b>Der Arbeitsstand eines Projektgebäudes aus der Datenbank</b> — seine Ebene und die seiner
        /// Zonen in Listenfolge (Rang, Id) mit Name, Nutzfläche und „beheizt"; Nutzfläche des Gebäudes
        /// für den Flächenschlüssel. Für die Rückfragen und die Paritätsprobe.
        /// </summary>
        public Konditionierungsarbeitsstand ArbeitsstandLesen(long idGebaeude, int? referenzjahr, out string meldung)
        {
            Eigner gebaeude = Eigner.Gebaeude(idGebaeude);
            Konditionierungsstand g = StandLesen(gebaeude, out meldung);
            DataTable kopf = DataRepository.GetDataTable(
                "SELECT * FROM \"" + Matrixzellenort.TAB_GEBAEUDE + "\" WHERE \"ID\" = ?", new DbParam("@id", idGebaeude));
            double? nutzflaeche = kopf != null && kopf.Rows.Count > 0 ? Zahl(kopf.Rows[0], "Nutzflaeche") : null;

            var zonen = new List<Konditionierungszone>();
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Bezeichner\", \"Nutzflaeche\", \"IstBeheizt\" FROM \"" + Matrixzellenort.TAB_ZONE +
                "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\", \"ID\"", new DbParam("@g", idGebaeude));
            if (t != null)
                foreach (DataRow r in t.Rows)
                {
                    long id = Lang(r, "ID") ?? 0;
                    Konditionierungsstand z = StandLesen(Eigner.Zone(idGebaeude, id), out string m);
                    meldung ??= m;
                    zonen.Add(new Konditionierungszone(id, Text(r, "Bezeichner") ?? "", Zahl(r, "Nutzflaeche"),
                                                       (Lang(r, "IstBeheizt") ?? 1) != 0, z));
                }
            return new Konditionierungsarbeitsstand(g, zonen, nutzflaeche, referenzjahr);
        }

        private static double? Endlich(double? w) => w.HasValue && double.IsFinite(w.Value) ? w : null;

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

            // Stufe KP2: eine duenne Huelle ueber den reinen Schritt (Konditionierungsarbeit.KalenderAnlegen).
            return Schrittweg(eigner, mitBestand: false, streng: false,
                              vor => Konditionierungsarbeit.KalenderAnlegen(vor, matrix, groesse));
        }

        /// <summary>
        /// <b>Schreibt einen Kalender</b> samt Perioden in <b>einer</b> Transaktion und ersetzt einen
        /// vorhandenen derselben Größe (die Perioden fallen über die Kaskade). Der Rundlauf wird noch
        /// einmal geprüft — auch ein von Hand gebauter Kalender darf keine Reihe verschieben.
        /// </summary>
        public Ergebnis Schreiben(Eigner eigner, Konditionierungskalender kalender)
            => Schreiben(eigner, kalender, null);

        /// <summary>
        /// Dieselbe Fassung mit einem <b>Vermerk</b> für die Spalte <c>Bemerkung</c> — die Herkunft
        /// einer übernommenen Vorlage („aus Vorlage Büro") und der Vermerk eines Werkzeugs stehen
        /// dort als <b>Text</b>, nie als Id am Ziel (Konzept 3.5, Entwurf KP1b Nr. 11).
        /// </summary>
        public Ergebnis Schreiben(Eigner eigner, Konditionierungskalender kalender, string bemerkung)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    Ergebnis e = KalenderSchreiben(v, eigner, kalender, bemerkung);
                    if (!e.Ok)
                    {
                        v.Rollback();
                        return e;
                    }
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
        /// <b>Derselbe Schreibweg im laufenden Vorgang</b> — die Klammer hält der Aufrufer. Der
        /// Vorlagen-Controller braucht sie: „Vorlage übernehmen" schreibt Matrixzellen <b>und</b>
        /// Kalender, und beides gehört in <b>einen</b> Vorgang (Konzept 5.5). Das Schloss prüft der
        /// Aufrufer <b>vor</b> dem Vorgang — es steht in der Datenbank, nicht im Arbeitsstand.
        /// </summary>
        internal static Ergebnis KalenderSchreiben(DbVorgang v, Eigner eigner,
                                                   Konditionierungskalender kalender, string bemerkung)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (kalender == null) throw new ArgumentNullException(nameof(kalender));
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
            if (bemerkung != null) zeile.Bemerkung = bemerkung;

            string groesse = Konditionierungsgroessen.Kennwort(kalender.Groesse);

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
            return Schrittweg(eigner, mitBestand: false, streng: false,
                              vor => Ebenenergebnis.Gut(Konditionierungsarbeit.KalenderVerwerfen(vor, groesse)));
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
            if (!KonditionierungSchema.Lesbar())
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG, KonditionierungSchema.SCHRITT));
            string schloss = Schloss(eigner);
            if (schloss != null) return Ergebnis.Fehler(schloss);

            // Ohne angelegten Kalender ist es „Anlegen" (Konditionierungsarbeit.MatrixbereichErsetzen).
            return Schrittweg(eigner, mitBestand: false, streng: true,
                              vor => Konditionierungsarbeit.MatrixbereichErsetzen(vor, matrix, groesse));
        }

        /// <summary>
        /// Gehört die Periode zum <b>Matrixbereich</b> (P12)? Genau die Arten, die der Generator
        /// vergibt: <c>FERIEN</c> (die Ferienzeiträume) und <c>BETRIEBSPAUSE</c> (die Saison).
        /// <c>ZEITRAUM</c> und <c>FEIERTAG</c> gehören dem Anwender und bleiben.
        /// </summary>
        public static bool IstMatrixbereich(Kalenderregel r) => Konditionierungsarbeit.IstMatrixbereich(r);

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
            string pruefung = ZellePruefen(groesse, zeile, zelle);
            if (pruefung != null) return Ergebnis.Fehler(pruefung);

            // Stufe KP2: der reine Schritt (Weiche, Nachtzeiten B6, Merker B7), dann nur Geaendertes.
            // Eine unbelegte Zelle laesst die Bestandsspalte hier stehen, auch an der Zone (KP1).
            return Schrittweg(eigner, mitBestand: true, streng: false, vor =>
            {
                Matrixzelle z = zelle;
                if (!z.Belegt && Matrixzellenort.HatBestandsspalte(vor.Art, groesse, zeile))
                {
                    double? w = Konditionierungsarbeit.Bestandswert(vor.Bestand, groesse, zeile);
                    if (w.HasValue) z = Matrixzelle.AusWert(w.Value, z.Von, z.Bis, z.BedingtK);
                }
                return Konditionierungsarbeit.Eintragen(vor, groesse, zeile, z);
            });
        }

        /// <summary>
        /// <b>Der duenne Schreibweg eines Knopfs</b> (Stufe KP2, Welle K2): die Ebene lesen, den reinen
        /// Schritt der <see cref="Konditionierungsarbeit"/> rechnen und nur das Geaenderte schreiben
        /// (<see cref="StandSchreiben"/>) — alles in EINEM Vorgang unter der
        /// <see cref="Vorgangsklammer"/>. <paramref name="streng"/>: eine ungueltige Kalenderzeile
        /// wird benannt abgelehnt, statt still ueberschrieben. Schloss und Pruefung haelt der Aufrufer.
        /// </summary>
        internal Ergebnis Schrittweg(Eigner eigner, bool mitBestand, bool streng,
                                     Func<Konditionierungsstand, Ebenenergebnis> schritt)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (schritt == null) throw new ArgumentNullException(nameof(schritt));
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                try
                {
                    Konditionierungsstand vor = StandLesen(eigner, out string meldung);
                    if (streng && meldung != null)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(meldung);
                    }
                    Ebenenergebnis r = schritt(vor);
                    if (!r.Ok)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(r.Meldung);
                    }
                    Ergebnis e = StandSchreiben(v, eigner, r.Stand, mitBestand, out _);
                    if (!e.Ok)
                    {
                        v.Rollback();
                        return e;
                    }
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
        /// <b>Was an einer Zelle auch ohne Datenbank prüfbar ist</b>: das Zeilenkennwort und die
        /// Grenzen der Größe. Die Grenzen gelten den <b>Wertzeilen</b> (Tag, Nacht, Wochenende,
        /// Ferien); die Zeile <c>NENNWERT</c> trägt bei den Lasten einen Wattwert — nicht den Anteil
        /// 0 … 1 —, bei der Lüftung die Infiltration in 1/h, und die Zeile <c>SAISON</c> trägt keinen
        /// Wert, nur Tage (E53, N1.61 Nr. 14).
        /// </summary>
        /// <returns><c>null</c>, wenn die Zelle passt, sonst die benannte Ablehnung.</returns>
        internal static string ZellePruefen(Konditionierungsgroesse groesse, string zeile, Matrixzelle zelle)
            => Konditionierungsarbeit.Zellenpruefung(groesse, zeile, zelle);

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
            => Konditionierungsarbeit.PersonenNennwertVorschlag(bewohner, nutzflaecheM2, flaecheJeNutzer);

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
            => Konditionierungsarbeit.GeraeteNennwertNachPersonen(interneWaermegewinneW, personen, w0, referenzjahr);

        /// <summary>Das Jahresmittel der Personenwärme [W] — Anteil × Nennwert über 8 760 Stunden.</summary>
        public static double PersonenJahresmittelW(Konditionierungskalender personen, int w0, int referenzjahr)
            => Konditionierungsarbeit.PersonenJahresmittelW(personen, w0, referenzjahr);

        // =================================================================
        //  Kleine Helfer
        // =================================================================

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
