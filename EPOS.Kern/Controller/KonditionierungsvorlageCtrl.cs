using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Controller der Konditionierungsvorlagen</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 3.5 und 5.7): <b>listen</b> je Größe, <b>als Vorlage speichern</b>
    /// (E54), <b>übernehmen</b> (P12), <b>umbenennen</b>, <b>löschen</b>, <b>duplizieren</b> und
    /// <b>in eine andere Größe kopieren</b> („Kopieren nach …", <see cref="Vorlagenkopierregel"/>) —
    /// dazu die <b>Werkzeuge der Karte</b> als dünne Wrapper um
    /// <see cref="Kalenderwerkzeuge"/>.
    ///
    /// <para><b>Eine Vorlage ist ein vorbefüllter Kalender EINER Größe</b> (P11). Ihr Inhalt steht
    /// mit dem Eigentümer <c>ID_Vorlage</c> in Vorgabe-, Kalender- und Periodentabelle, nur in ihrer
    /// Größe; die Größengleichheit hält dieser Controller (Entwurf KP1b, Festlegung 3), der
    /// Prüfbericht der Auslieferungsvorlage zählt die Verstöße.</para>
    ///
    /// <para><b>Was eine Vorlage NICHT trägt</b> (E54, Konzept 5.7): keine Zeile <c>NENNWERT</c>,
    /// keine Zeile <c>SAISON</c>, keine Periode der Arten <c>FERIEN</c> und <c>BETRIEBSPAUSE</c> und
    /// keinen <c>Nennwert</c> am Kalender. Nennwert und Heiz- bzw. Kühlperiode gehören dem Objekt und
    /// bleiben beim Ziel. Der Vorlagenfilter des Kopierers
    /// (<see cref="Konditionierungskopie.Auswahl.FuerVorlage"/>) setzt das durch.</para>
    ///
    /// <para><b>Jeder Schreibweg läuft in EINEM Vorgang</b> — Kopfsatz, Matrixzellen, Kalender und
    /// Perioden zusammen. Scheitert ein Schritt, bleibt nichts halb Geschriebenes stehen; die
    /// Alt→Neu-Zuordnung der Kalender entsteht über <c>last_insert_rowid()</c> und ist nur in
    /// DERSELBEN Transaktion die Id, die eben entstanden ist (Befund KP1a).</para>
    ///
    /// <para><b>Das Schloss</b> (Konzept 3.4, 5.7): Eine Vorlage mit <c>ReadOnly = 1</c> gehört zur
    /// Auslieferung — sie lässt sich weder umbenennen noch löschen noch in ihrem Inhalt ändern, wohl
    /// aber <b>duplizieren</b>. Die Prüfung ist <see cref="KonditionierungCtrl.Schloss"/>; eine
    /// eigene Spalte dafür gibt es nicht.</para>
    ///
    /// <para><b>Die Oberfläche kommt mit KP2.</b> Hier steht die Datenbankseite; die Regeln ohne
    /// Datenbank — Namensregel, Nutzung, eindeutiger Name, Reihenfolge der Liste — stehen als reine
    /// Funktionen daneben und gelten ebenso für die Ablage ohne Datenbank
    /// (<see cref="Konditionierungsvorlagenablage"/>, Stufe KP2, Welle U2).</para>
    /// </summary>
    public sealed class KonditionierungsvorlageCtrl : IKonditionierungsvorlagen
    {
        /// <summary>
        /// <b>Ein Kopfsatz der Vorlagenliste</b> — was die Auswahlliste der Kalenderkarte zeigt.
        /// </summary>
        /// <param name="Id">Die Id in <c>Tab_Konditionierungsvorlage_STAMM</c>.</param>
        /// <param name="Groesse">Die eine Größe der Vorlage (P11).</param>
        /// <param name="Bezeichner">Der Name, eindeutig je Größe.</param>
        /// <param name="Beschreibung">Die Beschreibung; <c>null</c> heißt „ohne".</param>
        /// <param name="Nutzung">Die Nutzung (<see cref="DbWerte.KOND_NUTZUNGEN"/>); <c>null</c> heißt „ohne".</param>
        /// <param name="Ausgeliefert"><c>ReadOnly = 1</c> — die Vorlage gehört zur Auslieferung.</param>
        public sealed record Vorlage(long Id, Konditionierungsgroesse Groesse, string Bezeichner,
                                     string Beschreibung, string Nutzung, bool Ausgeliefert);

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>
        /// <b>Die Vorlagen einer Größe</b>, sortiert wie die Auswahlliste sie zeigt: die
        /// <b>ausgelieferten zuerst</b>, dann nach Name. Leere Liste, wenn die Vorlagentabelle fehlt
        /// (ein Datenbankstand vor Schritt <see cref="KonditionierungVorlagenSchema.SCHRITT"/>).
        /// </summary>
        public List<Vorlage> Liste(Konditionierungsgroesse groesse)
        {
            var liste = new List<Vorlage>();
            if (!KonditionierungVorlagenSchema.Lesbar()) return liste;

            // Die Sortierung ueber IIF statt ueber eine Access-Schreibweise (BETRIEB_SQLITE 6);
            // NOCASE sortiert "Buero" und "buero" nebeneinander, wie die Liste sie zeigt.
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Groesse\", \"Bezeichner\", \"Beschreibung\", \"Nutzung\", \"ReadOnly\" FROM \"" +
                KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"Groesse\" = ? " +
                "ORDER BY IIF(\"ReadOnly\" = 1, 0, 1), \"Bezeichner\" COLLATE NOCASE, \"ID\"",
                new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)));
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
            {
                Vorlage v = Aus(r);
                if (v != null) liste.Add(v);
            }
            return liste;
        }

        /// <summary>Eine Vorlage über ihre Id; <c>null</c>, wenn es sie nicht gibt.</summary>
        public Vorlage Lesen(long id)
        {
            if (!KonditionierungVorlagenSchema.Lesbar()) return null;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Groesse\", \"Bezeichner\", \"Beschreibung\", \"Nutzung\", \"ReadOnly\" FROM \"" +
                KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"ID\" = ?",
                new DbParam("@id", id));
            return t == null || t.Rows.Count == 0 ? null : Aus(t.Rows[0]);
        }

        /// <summary>
        /// <b>Eine Vorlage samt Inhalt</b> — Kopf und Ebene der Art <see cref="Kalendereigentuemer.Vorlage"/>,
        /// die Eingabe des reinen Schritts „Vorlage übernehmen" (Stufe KP2, Welle U2); <c>null</c> mit
        /// benannter Meldung, wenn es sie nicht gibt oder ihr Inhalt nicht lesbar ist.
        /// </summary>
        public Konditionierungsvorlage Inhalt(long id, out string meldung)
        {
            Vorlage v = Lesen(id);
            if (v == null)
            {
                meldung = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_VORLAGE_FEHLT,
                                        id.ToString(CultureInfo.InvariantCulture));
                return null;
            }
            Konditionierungsstand inhalt = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Vorlage(id), out meldung);
            return meldung != null ? null : new Konditionierungsvorlage(id, v.Bezeichner, v.Groesse, inhalt);
        }

        /// <summary>
        /// <b>Die Reihenfolge der Auswahlliste</b> (Teilkonzept 7.4) als reine Funktion — dieselbe wie
        /// <c>ORDER BY IIF("ReadOnly" = 1, 0, 1), "Bezeichner" COLLATE NOCASE, "ID"</c> in
        /// <see cref="Liste"/>: die ausgelieferten zuerst, dann nach Name ohne Unterschied der
        /// Groß- und Kleinschreibung von A bis Z (NOCASE faltet nur diese), zuletzt nach Id. So stehen
        /// gleiche Namen in jeder Liste an derselben Stelle.
        /// </summary>
        public static int Vergleichen(Vorlage a, Vorlage b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            int r = (a.Ausgeliefert ? 0 : 1).CompareTo(b.Ausgeliefert ? 0 : 1);
            if (r != 0) return r;
            r = string.CompareOrdinal(AsciiKlein(a.Bezeichner), AsciiKlein(b.Bezeichner));
            return r != 0 ? r : a.Id.CompareTo(b.Id);
        }

        /// <summary>Faltet nur A … Z auf a … z — wie <c>COLLATE NOCASE</c> in SQLite.</summary>
        private static string AsciiKlein(string s)
        {
            if (s == null) return "";
            var z = s.ToCharArray();
            for (int i = 0; i < z.Length; i++)
                if (z[i] >= 'A' && z[i] <= 'Z') z[i] = (char)(z[i] + ('a' - 'A'));
            return new string(z);
        }

        private static Vorlage Aus(DataRow r)
        {
            string kennwort = Text(r, "Groesse");
            if (!Konditionierungsgroessen.AusKennwort(kennwort, out Konditionierungsgroesse g)) return null;
            return new Vorlage(Lang(r, "ID") ?? 0, g, Text(r, "Bezeichner"), Text(r, "Beschreibung"),
                               Text(r, "Nutzung"), (Lang(r, "ReadOnly") ?? 0) != 0);
        }

        // =================================================================
        //  Als Vorlage speichern (E54, Konzept 3.5 und 5.7)
        // =================================================================

        /// <summary>
        /// <b>„Als Vorlage speichern"</b>: legt aus der Karte <b>einer</b> Größe eines Gebäudes,
        /// einer Zone oder eines Katalogbaus eine <b>eigene</b> Vorlage an (<c>ReadOnly = 0</c>).
        ///
        /// <para><b>Was mitreist</b> (E54): die Nutzungszeilen der Spalte — Tag, Nacht mit Zeiten und
        /// <c>Bedingt_K</c>, Wochenende, Ferienwert —, dazu, falls die Quelle einen angelegten
        /// Kalender dieser Größe trägt, dessen Standardwoche und eigene Perioden samt
        /// Feiertagsregeln. <b>Weder Nennwert noch Saison</b>, keine datierten Ferien und keine
        /// Saisonperiode; der <c>Nennwert</c> am Kalender bleibt NULL.</para>
        ///
        /// <para><b>Bestandszellen werden zu Vorgabezeilen</b>: Eine Vorlage führt keine
        /// Bestandsspalten (<see cref="Matrixzellenort"/>), also kommt der Zahlenwert aus der
        /// Bestandsspalte der Quelle — <c>Raumsolltemperatur_Tag</c>, <c>Kuehl_Sollwert</c>,
        /// <c>Luftwechsel_Nutzer</c> … — in die Vorgabezeile der Vorlage. Eine Zelle, die in der
        /// Quelle auf „aus" steht, bleibt „aus": „aus" schlägt den Zahlenwert (Konzept 5.6).</para>
        /// </summary>
        /// <param name="quelle">Gebäude, Zone oder Katalogbau, dessen Spalte die Vorlage wird.</param>
        /// <param name="groesse">Die eine Größe der Vorlage.</param>
        /// <param name="bezeichner">Der Name; getrimmt, 1 … 80 Zeichen, je Größe eindeutig.</param>
        /// <param name="beschreibung">Die Beschreibung oder <c>null</c>.</param>
        /// <param name="nutzung">Eine der <see cref="DbWerte.KOND_NUTZUNGEN"/> oder <c>null</c>.</param>
        /// <param name="id">Die Id der neuen Vorlage; 0 im Fehlerfall.</param>
        public KonditionierungCtrl.Ergebnis Speichern(KonditionierungCtrl.Eigner quelle,
                                                      Konditionierungsgroesse groesse, string bezeichner,
                                                      string beschreibung, string nutzung, out long id)
        {
            id = 0;
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);
            if (quelle.Art == Kalendereigentuemer.Vorlage)
                return KonditionierungCtrl.Ergebnis.Fehler(
                    MyResource.Resource.KOND_MSG_VORLAGE_ALS_QUELLE);

            string name = Namenspruefung(groesse, bezeichner, 0, out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            string nutzungswert = Nutzungspruefung(nutzung, out meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);

            long neu = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    neu = KopfAnlegen(v, groesse, name, beschreibung, nutzungswert);
                    if (neu <= 0)
                    {
                        v.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(
                            MyResource.Resource.KOND_MSG_KOPF_NICHT_ANGELEGT);
                    }

                    var ziel = KonditionierungCtrl.Eigner.Vorlage(neu);
                    Konditionierungskopie.Befund b = Konditionierungskopie.Kopieren(
                        v, quelle, ziel, Konditionierungskopie.Auswahl.FuerVorlage(groesse));
                    if (!b.Ok)
                    {
                        v.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(b.Meldung);
                    }

                    string fehler = BestandszellenNachtragen(v, quelle, ziel, groesse);
                    if (fehler != null)
                    {
                        v.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(fehler);
                    }

                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return KonditionierungCtrl.Ergebnis.Fehler(ex.Message);
                }
            }
            id = neu;
            return KonditionierungCtrl.Ergebnis.Gut;
        }

        /// <summary>
        /// <b>„Als Vorlage speichern…" aus dem Arbeitsstand</b> (Stufe KP2, Welle K2; Festlegung 13): Der
        /// Inhalt kommt schon gefiltert aus <see cref="Konditionierungsarbeit.AlsVorlage"/> (E54: ohne
        /// Nennwert und Saison, Bestandszellen als Vorgabezellen) — hier entstehen Kopf und Zeilen in EINEM
        /// Vorgang, mit derselben Namens- und Nutzungsregel wie <see cref="Speichern"/>. Schreibt sofort.
        /// </summary>
        public KonditionierungCtrl.Ergebnis SpeichernAus(Konditionierungsstand inhalt, Konditionierungsgroesse groesse,
                                                         string bezeichner, string beschreibung, string nutzung, out long id)
        {
            id = 0;
            if (inhalt == null) throw new ArgumentNullException(nameof(inhalt));
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);
            string name = Namenspruefung(groesse, bezeichner, 0, out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            string nutzungswert = Nutzungspruefung(nutzung, out meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);

            return KopfUndInhaltAnlegen(inhalt, groesse, name, beschreibung, nutzungswert, out id);
        }

        /// <summary>
        /// <b>Kopfsatz und Inhalt einer neuen eigenen Vorlage in EINEM Vorgang</b> — der gemeinsame
        /// Schreibweg von <see cref="SpeichernAus"/> und <see cref="KopierenNach"/>. Der Inhalt steht nur in
        /// der Größe der Vorlage (<see cref="Groessenregel"/>, Konzept 5.7: der Controller hält die Größe
        /// gleich); Name und Nutzung hat der Aufrufer geprüft.
        /// </summary>
        private static KonditionierungCtrl.Ergebnis KopfUndInhaltAnlegen(Konditionierungsstand inhalt,
                                                                         Konditionierungsgroesse groesse, string name,
                                                                         string beschreibung, string nutzung, out long id)
        {
            id = 0;
            string fremd = Groessenregel(inhalt, groesse);
            if (fremd != null) return KonditionierungCtrl.Ergebnis.Fehler(fremd);

            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                try
                {
                    long neu = KopfAnlegen(v, groesse, name, beschreibung, nutzung);
                    if (neu <= 0)
                    {
                        v.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(MyResource.Resource.KOND_MSG_KOPF_NICHT_ANGELEGT);
                    }
                    KonditionierungCtrl.Ergebnis e = new KonditionierungCtrl().StandSchreiben(
                        v, KonditionierungCtrl.Eigner.Vorlage(neu), inhalt.AlsArt(Kalendereigentuemer.Vorlage),
                        mitBestand: false, out _);
                    if (!e.Ok)
                    {
                        v.Rollback();
                        return e;
                    }
                    v.Commit();
                    id = neu;
                    return KonditionierungCtrl.Ergebnis.Gut;
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return KonditionierungCtrl.Ergebnis.Fehler(ex.Message);
                }
            }
        }

        /// <summary>
        /// Trägt die Zahlenwerte der <b>Bestandsspalten</b> der Quelle in die Vorgabezeilen der
        /// Vorlage nach — und die Nachtzeiten, wo die Quelle sie in
        /// <c>Nachtabsenkung_Beginn/_Ende</c> führt statt in einer Vorgabezeile (E54: Nacht
        /// <b>mit Zeiten</b>). Die Zeilen <c>NENNWERT</c> und <c>SAISON</c> bleiben außen vor.
        /// </summary>
        /// <returns><c>null</c> im guten Fall, sonst die benannte Ablehnung.</returns>
        private static string BestandszellenNachtragen(DbVorgang v, KonditionierungCtrl.Eigner quelle,
                                                       KonditionierungCtrl.Eigner ziel,
                                                       Konditionierungsgroesse groesse)
        {
            string gr = Konditionierungsgroessen.Kennwort(groesse);

            foreach (string zeile in NUTZUNGSZEILEN)
            {
                Matrixzellenort.Ort ort = Matrixzellenort.Fuer(quelle.Art, groesse, zeile);
                if (!ort.IstBestandsspalte) continue;          // der Wert steht schon in der Vorgabezeile

                object roh = v.Skalar("SELECT \"" + ort.Spalte + "\" FROM \"" + ort.Tabelle +
                                      "\" WHERE \"ID\" = ?", new DbParam("@id", quelle.Traegerid));
                if (roh == null || roh == DBNull.Value) continue;     // keine Angabe: die Zelle bleibt leer

                double wert = Convert.ToDouble(roh, CultureInfo.InvariantCulture);
                if (!Konditionierungsgroessen.ImBereich(groesse, wert))
                    return string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_WERT_AUSSERHALB,
                        wert.ToString("G6", CultureInfo.InvariantCulture),
                        Konditionierungsgroessen.Bereichstext(groesse));

                // „aus" schlaegt den Zahlenwert (Konzept 5.6): Traegt die kopierte Zeile Aus = 1,
                // bleibt sie, wie sie ist.
                if (Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                  "\" WHERE " + ziel.Bedingung() + " AND \"Groesse\" = ? AND \"Zeile\" = ? " +
                                  "AND \"Aus\" = 1",
                                  Mit(ziel.Parameter(),
                                      new[] { new DbParam("@gr", gr), new DbParam("@ze", zeile) }))) > 0)
                    continue;

                int zeilen = v.Ausfuehren(
                    "UPDATE \"" + KonditionierungSchema.TAB_VORGABE + "\" SET \"Wert\" = ? WHERE " +
                    ziel.Bedingung() + " AND \"Groesse\" = ? AND \"Zeile\" = ?",
                    Mit(new[] { new DbParam("@we", wert) }, ziel.Parameter(),
                        new[] { new DbParam("@gr", gr), new DbParam("@ze", zeile) }));
                if (zeilen == 0)
                    v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                                 "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Vorlage\", " +
                                 "\"Groesse\", \"Zeile\", \"Wert\", \"Aus\") VALUES (?, ?, ?, ?, ?, ?, ?, 0)",
                                 Mit(ziel.Spaltenwerte("@e"),
                                     new[] { new DbParam("@gr", gr), new DbParam("@ze", zeile),
                                             new DbParam("@we", wert) }));
            }

            return Nachtzeiten(v, quelle, ziel, gr);
        }

        /// <summary>
        /// Die Nachtzeiten der Quelle in die Vorgabezeile <c>NACHT</c> der Vorlage — nur, wo die
        /// Zeile noch keine trägt und die Quelle die zwei Spalten führt
        /// (<see cref="Matrixzellenort.Nachtzeittabelle"/>).
        /// </summary>
        private static string Nachtzeiten(DbVorgang v, KonditionierungCtrl.Eigner quelle,
                                          KonditionierungCtrl.Eigner ziel, string gr)
        {
            string tabelle = Matrixzellenort.Nachtzeittabelle(quelle.Art);
            if (tabelle == null) return null;

            DataTable t = v.Lese("SELECT \"" + Matrixzellenort.SPALTE_NACHT_BEGINN + "\" AS Beginn, \"" +
                                 Matrixzellenort.SPALTE_NACHT_ENDE + "\" AS Ende FROM \"" + tabelle +
                                 "\" WHERE \"ID\" = ?", new DbParam("@id", quelle.Traegerid));
            if (t == null || t.Rows.Count == 0) return null;
            int? beginn = Ganz(t.Rows[0], "Beginn");
            int? ende = Ganz(t.Rows[0], "Ende");
            if (!beginn.HasValue || !ende.HasValue) return null;
            if (Nachtzeit.Pruefen(beginn, ende) != NachtzeitBefund.Gueltig)
                return string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_ZEITFENSTER_UNGUELTIG,
                    beginn.Value.ToString(CultureInfo.InvariantCulture),
                    ende.Value.ToString(CultureInfo.InvariantCulture));

            // Nur, wo die Zeile keine eigenen Zeiten traegt - die Vorgabezeile ist die staerkere
            // Angabe (Konzept 5.6).
            v.Ausfuehren("UPDATE \"" + KonditionierungSchema.TAB_VORGABE +
                         "\" SET \"Von\" = ?, \"Bis\" = ? WHERE " + ziel.Bedingung() +
                         " AND \"Groesse\" = ? AND \"Zeile\" = ? AND \"Von\" IS NULL AND \"Bis\" IS NULL",
                         Mit(new[] { new DbParam("@vo", beginn.Value), new DbParam("@bi", ende.Value) },
                             ziel.Parameter(),
                             new[] { new DbParam("@gr", gr),
                                     new DbParam("@ze", DbWerte.KOND_ZEILE_NACHT) }));
            return null;
        }

        // =================================================================
        //  Übernehmen (Konzept 3.5, P12)
        // =================================================================

        /// <summary>
        /// <b>„Vorlage übernehmen"</b> auf ein Gebäude, eine Zone oder einen Katalogbau — nur auf die
        /// Größe der Vorlage.
        ///
        /// <para><b>Was geschieht:</b> Die Zellen der Vorlage gehen über die Zellenort-Weiche in die
        /// Matrixspalte des Ziels — eine <b>leere</b> Zelle der Vorlage lässt die des Ziels stehen,
        /// also bleiben <b>Nennwert und Saison des Ziels</b> unberührt (E54) —, und der Kalender
        /// dieser Größe wird angelegt: der Generator mit den <b>Ferienzeiträumen des Ziels</b>,
        /// darüber Standardwoche und eigene Perioden der Vorlage.</para>
        ///
        /// <para><b>Trägt das Ziel schon einen angelegten Kalender</b> dieser Größe, gilt P12 wie in
        /// <see cref="KonditionierungCtrl.ErneutAnwenden"/> (N1.61 Nr. 13): Ersetzt wird nur der
        /// <b>Matrixbereich</b> — Grundangabe, Standardwoche und die Perioden der Arten
        /// <c>FERIEN</c> und <c>BETRIEBSPAUSE</c>; eigene Perioden und Ausnahmetage bleiben samt
        /// Rang, die Perioden der Vorlage kommen im Band
        /// <see cref="Standardfahrplan.RANG_EIGEN"/> … <see cref="Standardfahrplan.RANG_EIGEN_LETZTER"/>
        /// dazu, und eine <b>Feiertagsregel kommt nur einmal</b>. Eine Rangkollision wird benannt
        /// abgelehnt.</para>
        ///
        /// <para><b>Übernommen ist kopiert.</b> Die Herkunft steht nur als <b>Text</b> in
        /// <c>Bemerkung</c> („aus Vorlage Büro"), nie als Id am Ziel — sonst nähme „Vorlage löschen"
        /// über die Kaskade Projektkalender mit (Entwurf KP1b, Festlegung 6).</para>
        /// </summary>
        /// <param name="idVorlage">Die Vorlage, die übernommen wird.</param>
        /// <param name="ziel">Gebäude, Zone oder Katalogbau.</param>
        /// <param name="zielmatrix">
        /// Die <b>wirksame</b> Matrix des Ziels (nach der Kaskade, F2) — sie liefert die
        /// Ferienzeiträume und die Zellen, die die Vorlage leer lässt.
        /// </param>
        public KonditionierungCtrl.Ergebnis Uebernehmen(long idVorlage, KonditionierungCtrl.Eigner ziel,
                                                        Vorgabematrix zielmatrix)
        {
            if (ziel == null) throw new ArgumentNullException(nameof(ziel));
            if (zielmatrix == null) throw new ArgumentNullException(nameof(zielmatrix));
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);
            if (ziel.Art == Kalendereigentuemer.Vorlage)
                return KonditionierungCtrl.Ergebnis.Fehler(MyResource.Resource.KOND_MSG_VORLAGE_ALS_ZIEL);

            Vorlage vorlage = Lesen(idVorlage);
            if (vorlage == null)
                return KonditionierungCtrl.Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_FEHLT,
                    idVorlage.ToString(CultureInfo.InvariantCulture)));

            string schloss = KonditionierungCtrl.Schloss(ziel);
            if (schloss != null) return KonditionierungCtrl.Ergebnis.Fehler(schloss);

            // Stufe KP2: eine duenne Huelle - die Vorlage und das Ziel lesen, den reinen Schritt
            // (Konditionierungsarbeit.VorlageEintragen: Weiche, Generator mit den Ferien des Ziels,
            // Zusammenfuehren nach P12, Herkunft) rechnen und nur das Geaenderte schreiben.
            var ctrl = new KonditionierungCtrl();
            Konditionierungsstand inhalt = ctrl.StandLesen(KonditionierungCtrl.Eigner.Vorlage(idVorlage), out string m1);
            if (m1 != null) return KonditionierungCtrl.Ergebnis.Fehler(m1);
            var quelle = new Konditionierungsvorlage(idVorlage, vorlage.Bezeichner, vorlage.Groesse, inhalt);
            return ctrl.Schrittweg(ziel, mitBestand: true, streng: true,
                                   vor => Konditionierungsarbeit.VorlageEintragen(vor, zielmatrix, quelle));
        }

        // =================================================================
        //  Umbenennen, Löschen, Duplizieren (Konzept 5.7)
        // =================================================================

        /// <summary><b>Umbenennen</b> — nur eine eigene Vorlage (<c>ReadOnly = 0</c>).</summary>
        public KonditionierungCtrl.Ergebnis Umbenennen(long id, string bezeichner)
        {
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);

            Vorlage v = Lesen(id);
            if (v == null)
                return KonditionierungCtrl.Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_FEHLT, id.ToString(CultureInfo.InvariantCulture)));
            string schloss = KonditionierungCtrl.Schloss(KonditionierungCtrl.Eigner.Vorlage(id));
            if (schloss != null) return KonditionierungCtrl.Ergebnis.Fehler(schloss);

            string name = Namenspruefung(v.Groesse, bezeichner, id, out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);

            try
            {
                DataRepository.ExecuteNonQuery(
                    "UPDATE \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                    "\" SET \"Bezeichner\" = ? WHERE \"ID\" = ?",
                    new DbParam("@b", name), new DbParam("@id", id));
                return KonditionierungCtrl.Ergebnis.Gut;
            }
            catch (Exception ex)
            {
                return KonditionierungCtrl.Ergebnis.Fehler(ex.Message);
            }
        }

        /// <summary>
        /// <b>Löschen</b> — nur eine eigene Vorlage. Kalender, Perioden und Vorgabezeilen fallen über
        /// die <b>Kaskade</b> des Fremdschlüssels aus Schritt
        /// <see cref="KonditionierungVorlagenSchema.SCHRITT"/>; kein Gebäude wird berührt, denn
        /// übernommen ist kopiert (Konzept 5.5).
        /// </summary>
        public KonditionierungCtrl.Ergebnis Loeschen(long id)
        {
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);

            Vorlage v = Lesen(id);
            if (v == null)
                return KonditionierungCtrl.Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_FEHLT, id.ToString(CultureInfo.InvariantCulture)));
            string schloss = KonditionierungCtrl.Schloss(KonditionierungCtrl.Eigner.Vorlage(id));
            if (schloss != null) return KonditionierungCtrl.Ergebnis.Fehler(schloss);

            try
            {
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"ID\" = ?",
                    new DbParam("@id", id));
                return KonditionierungCtrl.Ergebnis.Gut;
            }
            catch (Exception ex)
            {
                return KonditionierungCtrl.Ergebnis.Fehler(ex.Message);
            }
        }

        /// <summary>
        /// <b>Duplizieren</b> — auch einer <b>ausgelieferten</b> Vorlage: So kommt der Anwender an
        /// einen eigenen, änderbaren Satz (Konzept 5.7). Die Kopie trägt <c>ReadOnly = 0</c>; ohne
        /// eigenen Namen bekommt sie den der Quelle mit Zusatz, eindeutig gemacht.
        /// </summary>
        /// <param name="id">Die Vorlage, die kopiert wird.</param>
        /// <param name="bezeichner">Der Name der Kopie; <c>null</c> oder leer heißt „Name (Kopie)".</param>
        /// <param name="neueId">Die Id der Kopie; 0 im Fehlerfall.</param>
        public KonditionierungCtrl.Ergebnis Duplizieren(long id, string bezeichner, out long neueId)
        {
            neueId = 0;
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);

            Vorlage v = Lesen(id);
            if (v == null)
                return KonditionierungCtrl.Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_FEHLT, id.ToString(CultureInfo.InvariantCulture)));

            string wunsch = (bezeichner ?? "").Trim();
            if (wunsch.Length == 0) wunsch = Kopiename(v.Bezeichner);
            string name = Eindeutig(v.Groesse, wunsch);
            string meldung;
            name = Namenspruefung(v.Groesse, name, 0, out meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);

            long neu = 0;
            using (DbVorgang vg = DataRepository.Vorgang())
            {
                try
                {
                    neu = KopfAnlegen(vg, v.Groesse, name, v.Beschreibung, v.Nutzung);
                    if (neu <= 0)
                    {
                        vg.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(
                            MyResource.Resource.KOND_MSG_KOPF_NICHT_ANGELEGT);
                    }

                    Konditionierungskopie.Befund b = Konditionierungskopie.Kopieren(
                        vg, KonditionierungCtrl.Eigner.Vorlage(id), KonditionierungCtrl.Eigner.Vorlage(neu),
                        Konditionierungskopie.Auswahl.FuerVorlage(v.Groesse));
                    if (!b.Ok)
                    {
                        vg.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(b.Meldung);
                    }
                    vg.Commit();
                }
                catch (Exception ex)
                {
                    vg.Rollback();
                    return KonditionierungCtrl.Ergebnis.Fehler(ex.Message);
                }
            }
            neueId = neu;
            return KonditionierungCtrl.Ergebnis.Gut;
        }

        // =================================================================
        //  Kopieren nach … — in eine andere Größe (Konzept 3.5, 7.4)
        // =================================================================

        /// <summary>
        /// <b>„Kopieren nach …"</b>: legt aus einer Vorlage — auch einer <b>ausgelieferten</b> — eine
        /// <b>eigene</b> Vorlage einer <b>anderen</b> Größe an (<c>ReadOnly = 0</c>); die Quelle bleibt, wie
        /// sie ist. Der Inhalt entsteht nach <see cref="Vorlagenkopierregel.Umsetzen"/>: Geräte ↔ Personen
        /// unverändert, Heizen → Kühlen mit Zeitstruktur und Aus-Zeiten, jede Zelle mit Sollwert in Höhe des
        /// Tagwerts auf dem <paramref name="komfortsollwert"/>, jede Absenkzeit (Heizsollwert unter dem Tagwert,
        /// <see cref="Vorlagenkopierregel.Tagwert"/>) auf dem <paramref name="absenksollwert"/> bzw. „aus"; jede
        /// andere Richtung wird benannt abgelehnt.
        ///
        /// <para><b>Name und Rechte:</b> Der Name gilt in der <b>Zielliste</b> — ein Doppelname wird benannt
        /// abgelehnt wie beim Anlegen, nicht still eindeutig gemacht. Die Beschreibung der Quelle wird um
        /// die Herkunft ergänzt („aus Vorlage ‚Büro‘ (Heizen)", <see cref="Vorlagenkopierregel.Beschreibung"/>),
        /// die Nutzung übernommen. Kopf und Inhalt entstehen in <b>einem</b> Vorgang, der Inhalt nur in der
        /// Zielgröße.</para>
        /// </summary>
        /// <param name="id">Die Vorlage, die kopiert wird.</param>
        /// <param name="ziel">Die Zielgröße — eine der <see cref="Vorlagenkopierregel.Ziele"/> der Quelle.</param>
        /// <param name="bezeichner">Der Name der Kopie in der Zielliste; getrimmt, 1 … 80 Zeichen.</param>
        /// <param name="komfortsollwert">Der Komfortsollwert [°C] bei Heizen → Kühlen; sonst ohne Bedeutung.</param>
        /// <param name="absenksollwert">
        /// Der Absenksollwert [°C] bei Heizen → Kühlen, <see cref="Vorlagenkopierregel.ABSENKSOLLWERT_AUS"/> = „aus";
        /// sonst ohne Bedeutung.
        /// </param>
        /// <param name="neueId">Die Id der Kopie; 0 im Fehlerfall.</param>
        public KonditionierungCtrl.Ergebnis KopierenNach(long id, Konditionierungsgroesse ziel, string bezeichner,
                                                         double? komfortsollwert, double? absenksollwert, out long neueId)
        {
            neueId = 0;
            string bereit = Bereit();
            if (bereit != null) return KonditionierungCtrl.Ergebnis.Fehler(bereit);

            Vorlage v = Lesen(id);
            if (v == null)
                return KonditionierungCtrl.Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_FEHLT, id.ToString(CultureInfo.InvariantCulture)));

            string regel = Vorlagenkopierregel.Pruefen(v.Groesse, ziel, komfortsollwert, absenksollwert);
            if (regel != null) return KonditionierungCtrl.Ergebnis.Fehler(regel);
            string name = Namenspruefung(ziel, bezeichner, 0, out string meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);

            Konditionierungsstand quelle = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Vorlage(id), out meldung);
            if (meldung != null) return KonditionierungCtrl.Ergebnis.Fehler(meldung);
            Ebenenergebnis inhalt = Vorlagenkopierregel.Umsetzen(quelle, v.Groesse, ziel, komfortsollwert, absenksollwert);
            if (!inhalt.Ok) return KonditionierungCtrl.Ergebnis.Fehler(inhalt.Meldung);

            return KopfUndInhaltAnlegen(inhalt.Stand, ziel, name,
                                        Vorlagenkopierregel.Beschreibung(v.Beschreibung, v.Bezeichner, v.Groesse),
                                        v.Nutzung, out neueId);
        }

        // =================================================================
        //  Die Werkzeuge der Karte — dünne Wrapper (Konzept 3.5)
        // =================================================================

        /// <summary>
        /// <b>Zeitfenster „Tage, von, bis, Wert"</b> auf den angelegten Kalender einer Größe:
        /// <see cref="Kalenderwerkzeuge.Zeitfenster"/>, geschrieben in <b>einem</b> Vorgang samt dem
        /// Vermerk in <c>Bemerkung</c>. Ohne angelegten Kalender gibt es nichts zu ändern — das wird
        /// benannt abgelehnt, statt still einen anzulegen.
        /// </summary>
        public KonditionierungCtrl.Ergebnis Zeitfenster(KonditionierungCtrl.Eigner eigner,
                                                        Konditionierungsgroesse groesse,
                                                        IReadOnlyList<int> wochentage, int von, int bis,
                                                        double? wert)
            => Werkzeug(eigner, groesse, k => Kalenderwerkzeuge.Zeitfenster(k, wochentage, von, bis, wert));

        /// <summary>
        /// <b>Die Feiertage als Regel</b> auf den angelegten Kalender einer Größe (F11):
        /// <see cref="Kalenderwerkzeuge.Feiertagsregeln"/>.
        /// </summary>
        public KonditionierungCtrl.Ergebnis Feiertagsregeln(KonditionierungCtrl.Eigner eigner,
                                                            Konditionierungsgroesse groesse,
                                                            int wieWochentag = 7)
            => Werkzeug(eigner, groesse, k => Kalenderwerkzeuge.Feiertagsregeln(k, wieWochentag));

        /// <summary>Der gemeinsame Weg beider Werkzeuge: lesen, rechnen, in einem Vorgang schreiben.</summary>
        private static KonditionierungCtrl.Ergebnis Werkzeug(
            KonditionierungCtrl.Eigner eigner, Konditionierungsgroesse groesse,
            Func<Konditionierungskalender, Kalenderwerkzeuge.Werkzeugbefund> werkzeug)
        {
            if (eigner == null) throw new ArgumentNullException(nameof(eigner));
            if (!KonditionierungSchema.Lesbar())
                return KonditionierungCtrl.Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG, KonditionierungSchema.SCHRITT));
            string schloss = KonditionierungCtrl.Schloss(eigner);
            if (schloss != null) return KonditionierungCtrl.Ergebnis.Fehler(schloss);

            // Stufe KP2: der reine Schritt (Konditionierungsarbeit.Werkzeug) - ohne angelegten Kalender
            // benannt abgelehnt, die Herkunft bleibt, der Vermerk kommt dazu (B8).
            return new KonditionierungCtrl().Schrittweg(eigner, mitBestand: false, streng: true,
                vor => Konditionierungsarbeit.Werkzeug(vor, groesse, werkzeug));
        }

        // =================================================================
        //  Die Namensregel (Konzept 5.7)
        // =================================================================

        /// <summary>
        /// <b>Die Namensregel</b>: getrimmt, 1 … <see cref="KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN"/>
        /// Zeichen und <b>je Größe eindeutig ohne Unterschied von Groß- und Kleinschreibung</b>.
        ///
        /// <para>Verglichen wird mit <see cref="StringComparison.OrdinalIgnoreCase"/>, nicht mit dem
        /// <c>NOCASE</c>-Index: <c>NOCASE</c> faltet nur ASCII und hielte „Büro" und „BÜRO"
        /// auseinander. Der Index bleibt die <b>Rückfallsperre</b>, die Regel prüft der Controller
        /// (Entwurf KP1b, Festlegung 2).</para>
        /// </summary>
        /// <param name="groesse">Die Liste, in der der Name eindeutig sein muss.</param>
        /// <param name="bezeichner">Der gewünschte Name.</param>
        /// <param name="ausser">Die eigene Id beim Umbenennen (0 beim Anlegen).</param>
        /// <param name="meldung">Die benannte Ablehnung oder <c>null</c>.</param>
        /// <returns>Der getrimmte Name.</returns>
        public static string Namenspruefung(Konditionierungsgroesse groesse, string bezeichner, long ausser,
                                            out string meldung)
            => Namensregel(groesse, bezeichner, Namen(groesse, ausser), out meldung);

        /// <inheritdoc/>
        public string NamePruefen(Konditionierungsgroesse groesse, string bezeichner, long ausser)
        {
            Namenspruefung(groesse, bezeichner, ausser, out string meldung);
            return meldung;
        }

        /// <summary>
        /// <b>Die Namensregel ohne Datenbank</b> (Stufe KP2, Welle U2) — dieselbe Regel wie
        /// <see cref="Namenspruefung"/> gegen die Namen <paramref name="vorhandene"/> der Liste (ohne den
        /// eigenen beim Umbenennen). Die Ablage ohne Datenbank und die Datenbankseite prüfen so gleich.
        /// </summary>
        public static string Namensregel(Konditionierungsgroesse groesse, string bezeichner,
                                         IEnumerable<string> vorhandene, out string meldung)
        {
            meldung = null;
            string name = (bezeichner ?? "").Trim();
            if (name.Length < 1 || name.Length > KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN)
            {
                meldung = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOND_MSG_VORLAGE_NAME_UNGUELTIG,
                    KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture));
                return name;
            }

            // Die Liste heisst, wie der Umschalter der Verwaltung sie nennt („Kühlen"), nicht nach ihrem
            // Persistenzwert - die Ablehnung steht am Namensfeld vor dem Anwender.
            foreach (string vorhanden in vorhandene ?? Array.Empty<string>())
                if (string.Equals(vorhanden, name, StringComparison.OrdinalIgnoreCase))
                {
                    meldung = string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.KOND_MSG_VORLAGE_NAME_DOPPELT,
                        name, Konditionierungsarbeit.Groessenname(groesse));
                    return name;
                }
            return name;
        }

        /// <summary>
        /// <b>Die Größenregel</b> (Konzept 5.7): Der Inhalt einer Vorlage steht nur in ihrer Größe — eine
        /// Vorgabezeile oder ein Kalender einer anderen Größe wird benannt abgelehnt. Eine reine Regel,
        /// die auch die Ablage ohne Datenbank nimmt.
        /// </summary>
        /// <returns><c>null</c>, wenn der Inhalt nur in <paramref name="groesse"/> steht, sonst die benannte Ablehnung.</returns>
        public static string Groessenregel(Konditionierungsstand inhalt, Konditionierungsgroesse groesse)
        {
            if (inhalt == null) return null;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                if (g == groesse) continue;
                bool fremd = inhalt.Kalender(g) != null;
                foreach (string zeile in DbWerte.KOND_ZEILEN)
                    fremd |= Konditionierungsstand.Traegt(inhalt.Vorgabe(g, zeile));
                if (fremd)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_VORLAGE_FREMDE_GROESSE,
                                         Konditionierungsarbeit.Groessenname(groesse), Konditionierungsarbeit.Groessenname(g));
            }
            return null;
        }

        /// <summary>Die Namen einer Größe, ohne den der Vorlage <paramref name="ausser"/>.</summary>
        private static List<string> Namen(Konditionierungsgroesse groesse, long ausser)
        {
            var namen = new List<string>();
            if (!KonditionierungVorlagenSchema.Lesbar()) return namen;
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Bezeichner\" FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" WHERE \"Groesse\" = ? AND \"ID\" <> ?",
                new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)),
                new DbParam("@id", ausser));
            if (t == null) return namen;
            foreach (DataRow r in t.Rows)
            {
                string n = Text(r, "Bezeichner");
                if (n != null) namen.Add(n);
            }
            return namen;
        }

        /// <summary>
        /// Macht einen Wunschnamen in seiner Liste eindeutig — „Büro", „Büro (2)", „Büro (3)" …,
        /// gekürzt auf die Höchstlänge.
        /// </summary>
        private static string Eindeutig(Konditionierungsgroesse groesse, string wunsch)
            => EindeutigerName(Namen(groesse, 0), wunsch);

        /// <summary>
        /// Derselbe eindeutige Name ohne Datenbank — gegen die Namen <paramref name="namen"/> der Liste
        /// (Stufe KP2, Welle U2; die Ablage ohne Datenbank).
        /// </summary>
        public static string EindeutigerName(IReadOnlyCollection<string> namen, string wunsch)
        {
            var liste = new List<string>(namen ?? Array.Empty<string>());
            string kandidat = Gekuerzt(wunsch);
            wunsch = wunsch ?? "";
            for (int n = 2; Belegt(liste, kandidat) && n < 1000; n++)
            {
                string zusatz = " (" + n.ToString(CultureInfo.InvariantCulture) + ")";
                int platz = KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN - zusatz.Length;
                string stamm = wunsch.Length > platz ? wunsch.Substring(0, platz) : wunsch;
                kandidat = (stamm + zusatz).Trim();
            }
            return kandidat;
        }

        private static bool Belegt(List<string> namen, string kandidat)
        {
            foreach (string n in namen)
                if (string.Equals(n, kandidat, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Der Name der Kopie ohne eigenen Wunsch: „Name (Kopie)" (<c>KOND_MSG_DUPLIKAT_ZUSATZ</c>), gekürzt
        /// — derselbe Satz für Datenbank und Ablage.
        /// </summary>
        public static string Kopiename(string quelle)
            => Gekuerzt(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KOND_MSG_DUPLIKAT_ZUSATZ, quelle));

        private static string Gekuerzt(string name)
        {
            string n = (name ?? "").Trim();
            return n.Length > KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN
                ? n.Substring(0, KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN).Trim()
                : n;
        }

        // =================================================================
        //  Kleine Helfer
        // =================================================================

        /// <summary>Die vier Nutzungszeilen einer Vorlage (E54) — ohne <c>NENNWERT</c> und <c>SAISON</c>.</summary>
        private static readonly string[] NUTZUNGSZEILEN =
        {
            DbWerte.KOND_ZEILE_TAG, DbWerte.KOND_ZEILE_NACHT,
            DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_FERIEN,
        };

        /// <summary>Steht der Schemaschritt? Sonst die benannte Ablehnung.</summary>
        private static string Bereit()
            => KonditionierungSchema.Lesbar() && KonditionierungVorlagenSchema.Lesbar()
                ? null
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ZONE_MSG_OHNE_KOPPLUNG,
                                KonditionierungVorlagenSchema.SCHRITT);

        /// <summary>Trägt die Zelle irgendetwas — Wert, „aus", Zeiten oder ΔT?</summary>
        private static bool Traegt(Matrixzelle z)
            => z.Belegt || z.Von.HasValue || z.Bis.HasValue || z.BedingtK.HasValue;

        /// <summary>Legt den Kopfsatz an und gibt seine Id zurück (0, wenn es nicht ging).</summary>
        private static long KopfAnlegen(DbVorgang v, Konditionierungsgroesse groesse, string bezeichner,
                                        string beschreibung, string nutzung)
        {
            v.Ausfuehren("INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                         "\" (\"Groesse\", \"Bezeichner\", \"Beschreibung\", \"Nutzung\", \"ReadOnly\") " +
                         "VALUES (?, ?, ?, ?, 0)",
                         new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)),
                         new DbParam("@bz", bezeichner),
                         new DbParam("@be", (object)Leer(beschreibung)),
                         new DbParam("@nu", (object)Leer(nutzung)));
            return Zahl(v.Skalar("SELECT last_insert_rowid()"));
        }

        /// <summary>
        /// Die Nutzung muss eine der vier sein (oder fehlen) — kein stiller Eigenwert; eine reine Regel,
        /// die auch die Ablage ohne Datenbank nimmt.
        /// </summary>
        public static string Nutzungspruefung(string nutzung, out string meldung)
        {
            meldung = null;
            string n = Leer(nutzung);
            if (n == null) return null;
            foreach (string k in DbWerte.KOND_NUTZUNGEN)
                if (string.Equals(n, k, StringComparison.Ordinal)) return n;
            meldung = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KOND_MSG_VORLAGE_NUTZUNG_UNBEKANNT, n);
            return null;
        }

        private static string Leer(string t) => string.IsNullOrWhiteSpace(t) ? null : t.Trim();

        private static long Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);

        private static DbParam[] Mit(DbParam[] erste, params DbParam[][] weitere)
        {
            var alle = new List<DbParam>(erste);
            foreach (DbParam[] w in weitere) alle.AddRange(w);
            return alle.ToArray();
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

        private static string Text(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != null && r[spalte] != DBNull.Value
                ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture)
                : null;
    }
}
