using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Die Abnahme der fertigen Vorlage. Sie laeuft IMMER — auch bei <c>--trocken</c> —
    /// und ihr Ergebnis entscheidet ueber den Rueckgabecode.
    ///
    /// <para><b>Sieben Fragen.</b> Schemastand, STRICT-Tabellenzahl,
    /// <c>PRAGMA integrity_check</c>, <c>PRAGMA foreign_key_check</c>, die Projektliste,
    /// der Datenschutzwaechter (samt leerer Importablage, Schritt S-F) und die Beidateien. Die ersten vier sind die
    /// Kontrollabfragen, die <c>sql/tools/Reduziere-Testdatenbank.sql</c> am Ende zum
    /// Kopieren auffuehrt; die drei uebrigen kommen aus dem Zweck dieser Datei — sie geht
    /// an Dritte.</para>
    /// </summary>
    internal sealed class Prueflauf
    {
        private readonly Bericht _bericht;
        private readonly Projektsicht _sicht;

        internal Prueflauf(Bericht bericht, Projektsicht sicht) { _bericht = bericht; _sicht = sicht; }

        /// <summary>Die Eingabepfade des Laufs (Quelle, Katalogpaket, Beispiele) — Posten ZU11.</summary>
        internal IReadOnlyList<string> Eingaben { get; set; } = new List<string>();

        /// <summary>Die Tww-Mitnahmen der Beispielpakete (<see cref="Vorlagenbau.TwwMitnahmen"/>).</summary>
        internal IReadOnlyList<string> TwwMitnahmen { get; set; } = new List<string>();

        /// <summary>
        /// Lief der Lauf mit <c>--kataloge alle</c> (Vorgabe)? Dann bleiben eigene Katalogzeilen
        /// stehen, und eine eigene Konditionierungsvorlage ist kein Befund; mit
        /// <c>--kataloge readonly</c> ist sie einer.
        /// </summary>
        internal bool KatalogeVollstaendig { get; set; } = true;

        /// <summary>
        /// Die sechs Fragen, die eine GEOEFFNETE Datenbank beantwortet. Die siebte —
        /// liegen Beidateien daneben? — kann erst danach gestellt werden und steht
        /// deshalb in <see cref="Dateipruefung"/>: Solange die Zugriffsschicht die Datei
        /// im WAL-Modus offen haelt, MUESSEN <c>-wal</c> und <c>-shm</c> existieren.
        /// </summary>
        internal bool Ausfuehren(int strictInDerQuelle)
        {
            _bericht.Abschnitt("Prüfung");
            bool ok = true;

            // ---- 1. Schemastand ----------------------------------------------------
            object stand = DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation LIMIT 1");
            int gelesen = stand == null ? -1 : Convert.ToInt32(stand);
            bool standOk = gelesen == SchemaStand.Zielversion;
            _bericht.Zeile((standOk ? "ok      " : "FEHLER  ") + "Schemastand " + gelesen +
                           "   (erwartet " + SchemaStand.Zielversion + ")");
            ok &= standOk;

            // ---- 2. STRICT-Tabellen ------------------------------------------------
            int strict = StrictTabellen();
            bool strictOk = strict == strictInDerQuelle;
            _bericht.Zeile((strictOk ? "ok      " : "FEHLER  ") + "STRICT-Tabellen " + strict +
                           "   (Quelle " + strictInDerQuelle + ")");
            ok &= strictOk;

            // ---- 3. integrity_check ------------------------------------------------
            string integritaet = Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check"));
            bool integerOk = string.Equals(integritaet, "ok", StringComparison.OrdinalIgnoreCase);
            _bericht.Zeile((integerOk ? "ok      " : "FEHLER  ") + "PRAGMA integrity_check = " + integritaet);
            ok &= integerOk;

            // ---- 4. foreign_key_check ----------------------------------------------
            // Ueber die tabellenwertige Form: PRAGMA-Ergebnisse haben keine deklarierten
            // Spaltentypen, und der Typ-Rueckweg der Zugriffsschicht stolpert dort ueber
            // NULL-Werte (Befund iU5-O-1). Mit einem SELECT auf pragma_foreign_key_check
            // stehen dagegen zwei gewoehnliche Textspalten da.
            long fkMeldungen = Vorlagenbau.Zaehle2("SELECT COUNT(*) FROM pragma_foreign_key_check");
            bool fkOk = fkMeldungen == 0;
            _bericht.Zeile((fkOk ? "ok      " : "WARNUNG ") + "PRAGMA foreign_key_check: " +
                           fkMeldungen + " Meldung(en)");
            if (!fkOk)
            {
                DataTable fk = DataRepository.GetDataTable(
                    "SELECT \"table\" AS Tabelle, \"parent\" AS Eltern, COUNT(*) AS Anzahl " +
                    "FROM pragma_foreign_key_check GROUP BY 1, 2 ORDER BY 3 DESC LIMIT 20");
                foreach (DataRow r in fk.Rows)
                    _bericht.Zeile("        " + Convert.ToString(r["Tabelle"]) + " -> " +
                                   Convert.ToString(r["Eltern"]) + ": " + Convert.ToString(r["Anzahl"]));
            }
            // Vorbestehende Waisen aus der Access-Zeit sind bekannt (Reduziere-Testdatenbank.sql,
            // GRENZEN 3) und brechen die Abnahme nicht — sie werden gemeldet.

            // ---- 5. Projektliste ---------------------------------------------------
            DataTable projekte = DataRepository.GetDataTable(
                "SELECT ID, Projektname FROM Tab_Projekt ORDER BY ID");
            _bericht.Zeile("        Projekte in der Vorlage: " + projekte.Rows.Count);
            foreach (DataRow r in projekte.Rows)
                _bericht.Zeile("            " + Convert.ToString(r["ID"]) + "  " + Convert.ToString(r["Projektname"]));

            var beispielIds = projekte.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["ID"])).ToHashSet();

            // ---- 6. Datenschutzwaechter --------------------------------------------
            ok &= Datenschutzwaechter(beispielIds);

            // ---- 7. Zapfprofil-Kataloge (Konzept Zapfprofilgenerator 3.2, 6 (b), (c)) --
            ok &= new TwwKataloge(_bericht).Pruefen(Eingaben, TwwMitnahmen);

            // ---- 8. Konditionierung (Konzept Konditionierungsprofile 5.5, 5.7, E54) ----
            ok &= Konditionierung();
            return ok;
        }

        // =================================================================================
        //  Die achte Frage — die Konditionierung (Konzept Konditionierungsprofile 5.5)
        // =================================================================================

        /// <summary>
        /// <b>Was an Konditionierung in der Vorlage steckt</b> — und ob es dorthin gehört.
        ///
        /// <para><b>Gezählt</b> wird zweimal: die Vorlagen je Größe, getrennt nach gesperrt
        /// (<c>ReadOnly = 1</c>, sie gehören zur Auslieferung) und eigen, und Kalender, Vorgaben und
        /// Perioden je <b>Eigentümerart</b> — Gebäude, Zone, Katalogbau, Vorlage. Die vier Arten sind
        /// dieselben, die die Teilindizes des Schemaschritts führen; sie stehen dort als Bedingung,
        /// statt hier abgeschrieben zu werden.</para>
        ///
        /// <para><b>Geprüft</b> wird fünferlei, jeder Befund mit Zahl:</para>
        /// <list type="number">
        /// <item><b>Die ausgelieferten Vorlagen der Saat</b> (KP-S1b, E56) — jede der 14 aus
        /// <see cref="KonditionierungsvorlagenSaattabelle"/> steht unter Größe und Namen gesperrt
        /// (<c>ReadOnly = 1</c>) in der Vorlage; eine fehlende ist ein Befund, weitere gesperrte
        /// Vorlagen sind keiner.</item>
        /// <item><b>Vorlageninhalt in fremder Größe</b> — eine Vorlage gehört genau EINER Größe
        /// (P11); der Controller hält das, hier fällt es auf, wenn es jemand umgangen hat.</item>
        /// <item><b>Vorlagen mit Nennwert oder Saison</b> (E54) — Vorgabezeilen <c>NENNWERT</c> und
        /// <c>SAISON</c>, ein <c>Nennwert</c> am Kalender und Perioden der Arten <c>FERIEN</c> und
        /// <c>BETRIEBSPAUSE</c>; beides gehört dem Objekt und bleibt beim Ziel.</item>
        /// <item><b>Waisen</b> — was <c>foreign_key_check</c> an den drei Tabellen meldet. Erst der
        /// Fremdschlüssel aus Schritt <see cref="KonditionierungVorlagenSchema.SCHRITT"/> macht die
        /// Frage beantwortbar.</item>
        /// <item><b>Eigene Vorlagen</b> nach <c>--kataloge readonly</c> — sie müssten über die
        /// ReadOnly-Regel gefallen sein, samt Kalendern, Perioden und Vorgaben (Kaskade).</item>
        /// </list>
        ///
        /// <para>Steht der Schemaschritt nicht, gibt es nichts zu prüfen — das ist ein
        /// <b>Hinweis</b>, kein Fehler: Eine ältere Quelle kennt die Tabellen nicht.</para>
        /// </summary>
        private bool Konditionierung()
        {
            _bericht.Leer();
            _bericht.Zeile("Konditionierung");

            if (!DataRepository.TabelleVorhanden(KonditionierungVorlagenSchema.TAB_VORLAGE))
            {
                _bericht.Zeile("        Schemaschritt " + KonditionierungVorlagenSchema.SCHRITT +
                               " steht nicht — nichts zu pruefen");
                return true;
            }

            // ---- Die Vorlagen je Groesse ------------------------------------------
            long gesperrt = 0, eigen = 0;
            foreach (string groesse in DbWerte.KOND_GROESSEN)
            {
                long g = Vorlagen(groesse, "\"ReadOnly\" = 1");
                long e = Vorlagen(groesse, "\"ReadOnly\" IS NULL OR \"ReadOnly\" = 0");
                gesperrt += g;
                eigen += e;
                _bericht.Zeile("        Vorlagen " + groesse.PadRight(10) + " gesperrt " + g + ", eigen " + e);
            }
            _bericht.Zeile("        Vorlagen gesamt: gesperrt " + gesperrt + ", eigen " + eigen);

            // ---- Kalender, Vorgaben und Perioden je Eigentuemerart ------------------
            _bericht.Leer();
            foreach (KonditionierungVorlagenSchema.Teilindex i in KonditionierungVorlagenSchema.Teilindizes)
            {
                long zeilen = Vorlagenbau.Zaehle2("SELECT COUNT(*) FROM \"" + i.Tabelle + "\" WHERE " +
                                                  i.Bedingung);
                string was = string.Equals(i.Tabelle, KonditionierungSchema.TAB_KALENDER,
                                           StringComparison.Ordinal)
                    ? "Kalender "
                    : "Vorgaben ";
                string zusatz = "";
                if (string.Equals(i.Tabelle, KonditionierungSchema.TAB_KALENDER, StringComparison.Ordinal))
                    zusatz = ", Perioden " + Perioden(i.Bedingung);
                _bericht.Zeile("        " + was + i.Eigentuemer.PadRight(20) + " " + zeilen + zusatz);
            }

            // ---- Die fuenf Pruefungen ---------------------------------------------
            _bericht.Leer();
            bool ok = true;

            // Die ausgelieferten Vorlagen der Saat (KP-S1b, E56): alle 14 gesperrt unter Groesse und Name.
            int saat = 0;
            var fehlend = new List<string>();
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaattabelle.Alle)
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"Groesse\" = ? AND " +
                    "\"Bezeichner\" = ? AND \"ReadOnly\" = 1",
                    new DbParam("@g", s.Kennwort), new DbParam("@b", s.Bezeichner));
                if (n != null && Convert.ToInt64(n, CultureInfo.InvariantCulture) == 1) saat++;
                else fehlend.Add(s.ToString());
            }
            ok &= Befund(saat == KonditionierungsvorlagenSaattabelle.VORLAGEN,
                         "ausgelieferte Vorlagen der Saat: " + saat + " von " + KonditionierungsvorlagenSaattabelle.VORLAGEN +
                         " gesperrt" + (fehlend.Count == 0 ? "" : "   (es fehlen " + string.Join(", ", fehlend) + ")"));

            long fremd = Vorlagenbau.Zaehle2(SqlFremdeGroesse(KonditionierungSchema.TAB_VORGABE))
                         + Vorlagenbau.Zaehle2(SqlFremdeGroesse(KonditionierungSchema.TAB_KALENDER));
            ok &= Befund(fremd == 0, "Vorlageninhalt in fremder Groesse: " + fremd +
                                     " Zeile(n)   (eine Vorlage gehoert genau EINER Groesse)");

            long nennwertZeilen = Vorlagenbau.Zaehle2(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                "\" WHERE \"ID_Vorlage\" IS NOT NULL AND \"Zeile\" IN ('" +
                DbWerte.KOND_ZEILE_NENNWERT + "', '" + DbWerte.KOND_ZEILE_SAISON + "')");
            long nennwertKalender = Vorlagenbau.Zaehle2(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                "\" WHERE \"ID_Vorlage\" IS NOT NULL AND \"Nennwert\" IS NOT NULL");
            long matrixperioden = Vorlagenbau.Zaehle2(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                "WHERE k.\"ID_Vorlage\" IS NOT NULL AND p.\"Art\" IN ('" +
                DbWerte.KOND_ART_FERIEN + "', '" + DbWerte.KOND_ART_BETRIEBSPAUSE + "')");
            long e54 = nennwertZeilen + nennwertKalender + matrixperioden;
            ok &= Befund(e54 == 0, "Vorlagen mit Nennwert oder Saison (E54): " + e54 +
                                   "   (Zeilen " + nennwertZeilen + ", Kalender " + nennwertKalender +
                                   ", Perioden " + matrixperioden + ")");

            long waisen = Vorlagenbau.Zaehle2(
                "SELECT COUNT(*) FROM pragma_foreign_key_check WHERE \"table\" IN ('" +
                KonditionierungSchema.TAB_KALENDER + "', '" + KonditionierungSchema.TAB_PERIODE +
                "', '" + KonditionierungSchema.TAB_VORGABE + "')");
            ok &= Befund(waisen == 0, "Waisen in den drei Tabellen (foreign_key_check): " + waisen);

            if (KatalogeVollstaendig)
                _bericht.Zeile("        eigene Vorlagen: " + eigen +
                               "   (Modus alle — sie bleiben; mit --kataloge readonly fielen sie)");
            else
                ok &= Befund(eigen == 0, "eigene Vorlagen nach --kataloge readonly: " + eigen +
                                         "   (die ReadOnly-Regel raeumt sie samt Kaskade)");

            return ok;
        }

        /// <summary>Eine Prüfzeile im Muster der übrigen: „ok" oder „FEHLER" vor dem Text.</summary>
        private bool Befund(bool gut, string text)
        {
            _bericht.Zeile((gut ? "ok      " : "FEHLER  ") + text);
            return gut;
        }

        /// <summary>Die Vorlagen einer Größe unter einer Bedingung.</summary>
        private static long Vorlagen(string groesse, string bedingung)
            => Vorlagenbau.Zaehle2("SELECT COUNT(*) FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                                   "\" WHERE \"Groesse\" = '" + groesse + "' AND (" + bedingung + ")");

        /// <summary>Die Perioden der Kalender, die der Eigentümerbedingung genügen.</summary>
        private static long Perioden(string kalenderbedingung)
            => Vorlagenbau.Zaehle2(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE " +
                Auf("k", kalenderbedingung));

        /// <summary>
        /// Dieselbe Eigentümerbedingung, auf einen Tabellenalias bezogen — die Bedingungen der
        /// Teilindizes nennen ihre Spalten ohne Alias, im <c>JOIN</c> braucht es ihn.
        /// </summary>
        private static string Auf(string alias, string bedingung)
            => bedingung.Replace("\"ID_", alias + ".\"ID_", StringComparison.Ordinal);

        /// <summary>
        /// Zeilen, deren Größe nicht die ihrer Vorlage ist. Der Vergleich läuft über die Vorlage,
        /// nicht über eine Liste erlaubter Größen — so trifft er auch eine künftige sechste.
        /// </summary>
        private static string SqlFremdeGroesse(string tabelle)
            => "SELECT COUNT(*) FROM \"" + tabelle + "\" t JOIN \"" +
               KonditionierungVorlagenSchema.TAB_VORLAGE + "\" v ON v.\"ID\" = t.\"ID_Vorlage\" " +
               "WHERE t.\"Groesse\" <> v.\"Groesse\"";

        /// <summary>
        /// Die siebte Frage, gestellt NACH dem Schliessen aller Verbindungen: Eine
        /// Auslieferungsvorlage ist EINE Datei. Bleibt eine <c>-wal</c> daneben liegen,
        /// waere ein Teil des Datenstands ausserhalb der Datei, die ins Setup wandert —
        /// und ginge beim Kopieren verloren.
        /// </summary>
        internal bool Dateipruefung(string datei)
        {
            var beidateien = new[] { datei + "-wal", datei + "-shm" }.Where(File.Exists).ToList();
            bool ok = beidateien.Count == 0;
            _bericht.Zeile((ok ? "ok      " : "FEHLER  ") + "Beidateien neben der Zieldatei: " +
                           (ok ? "keine" : string.Join(", ", beidateien.Select(Path.GetFileName))));
            _bericht.Leer();
            _bericht.Zeile("Groesse der Vorlage: " + Vorlagenbau.Mb(new FileInfo(datei).Length));
            return ok;
        }

        /// <summary>
        /// Der Wächter, der die Datenpanne verhindert: In einer Projekttabelle darf keine
        /// Zeile stehen, die weder Vorgabe (Projektspalte 0/NULL) noch Beispiel ist. Dazu
        /// die Suche nach Lizenz-/KI-Tabellen und nach Pfadangaben im Restbestand.
        /// </summary>
        private bool Datenschutzwaechter(HashSet<long> beispielIds)
        {
            _bericht.Leer();
            _bericht.Zeile("Datenschutzwächter");
            bool ok = true;
            string erlaubt = beispielIds.Count == 0
                ? "(-1)"
                : "(" + string.Join(", ", beispielIds.Select(i => i.ToString(CultureInfo.InvariantCulture))) + ")";

            // (a) Fremde Projektzeilen
            int fremd = 0;
            foreach (Projektsicht.Projekttabelle p in _sicht.Stufe1)
            {
                long n = Vorlagenbau.Zaehle2(
                    "SELECT COUNT(*) FROM \"" + p.Tabelle + "\" WHERE \"" + p.Spalte + "\" IS NOT NULL " +
                    "AND \"" + p.Spalte + "\" <> 0 AND \"" + p.Spalte + "\" NOT IN " + erlaubt);
                if (n <= 0) continue;
                fremd++;
                _bericht.Zeile("FEHLER  " + p.Tabelle + ": " + n + " Zeile(n) fremder Projekte");
            }
            if (fremd == 0)
                _bericht.Zeile("ok      keine Zeile in einer der " + _sicht.Stufe1.Count +
                               " Projekttabellen ausserhalb der Beispiele");
            ok &= fremd == 0;

            // (b) Lizenz-, KI- und Zugangs-Tabellen. Sie gibt es im Zielschema nicht;
            //     der Waechter sucht danach, statt es zu behaupten.
            string[] muster = { "lizenz", "licen", "token", "apikey", "api_key", "schluessel",
                                "zugang", "zustimm", "einwillig", "geraet", "ki_", "_ki" };
            var treffer = _sicht.AlleTabellen
                .Where(t => muster.Any(m => t.ToLowerInvariant().Contains(m)))
                .ToList();
            _bericht.Zeile(treffer.Count == 0
                ? "ok      keine Lizenz-, KI- oder Zugangstabelle im Schema (Token, Zeitanker und " +
                  "KI-Schluessel liegen ueber Dienste.Lizenzablage ausserhalb der Datenbank)"
                : "WARNUNG Tabellen mit Lizenz-/KI-/Zugangsbezug gefunden — von Hand pruefen: " +
                  string.Join(", ", treffer));

            // (c) Pfadangaben im Restbestand (Windows-Benutzername!)
            var pfadTreffer = PfadeSuchen();
            _bericht.Zeile(pfadTreffer.Count == 0
                ? "ok      keine Pfadangabe (\"X:\\…\" oder \"/Users/…\") in einer Textspalte"
                : "WARNUNG Pfadangaben gefunden:");
            foreach (string t in pfadTreffer) _bericht.Zeile("        " + t);

            // (d) Die Herkunftsablage der Gebaeudeimporte (Schritt S-F, Datenaustauschkonzept 7.4)
            ok &= ImportablageLeer();

            return ok;
        }

        /// <summary>
        /// <b>Beide Tabellen der Importherkunft sind leer</b> (Datenaustauschkonzept 7.4,
        /// Softwarearchitektur Gebäudesimulation 2.6). <c>Tab_Importquelle</c> trägt Dateiname und
        /// SHA-256 jeder eingelesenen gbXML- oder IFC-Datei, <c>Tab_Importzuordnung</c> die Kennungen
        /// ihrer Entitäten — in einer ausgelieferten <c>Kenndaten.sqlite</c> wären das Spuren fremder
        /// Importe. Die Regel ist eine PRÜFUNG, kein stilles Leeren: Eine Zeile hier kommt über ein
        /// Beispielpaket, und das gehört vor der Auslieferung bereinigt. Fehlt eine der Tabellen,
        /// ist das ebenso ein Befund — eine Zählung auf einer fehlenden Tabelle ergäbe still 0.
        /// </summary>
        private bool ImportablageLeer()
        {
            var teile = new List<string>();
            bool ok = true;
            foreach (string t in new[] { ImportzuordnungSchema.TAB_QUELLE, ImportzuordnungSchema.TAB_ZUORDNUNG })
            {
                if (!DataRepository.TabelleVorhanden(t))
                {
                    ok = false;
                    teile.Add(t + " fehlt");
                    continue;
                }
                long n = Vorlagenbau.Zaehle2("SELECT COUNT(*) FROM \"" + t + "\"");
                if (n != 0) ok = false;
                teile.Add(t + " " + n.ToString(CultureInfo.InvariantCulture));
            }
            _bericht.Zeile((ok ? "ok      " : "FEHLER  ") + "Importablage leer (" + string.Join(", ", teile) + ")" +
                           (ok ? "" : " — Dateinamen und SHA-256 fremder Importe gehoeren nicht in die Auslieferung"));
            return ok;
        }

        /// <summary>
        /// Sucht in jeder Textspalte nach einer Laufwerks- oder Benutzerpfadangabe. Erst
        /// EINE Abfrage je Tabelle (ein Durchlauf), und nur bei einem Treffer die Spalten
        /// einzeln — sonst waeren es dreihundert Durchlaeufe ueber eine 70-MB-Datei.
        /// </summary>
        private List<string> PfadeSuchen()
        {
            var ergebnis = new List<string>();
            foreach (string t in _sicht.AlleTabellen)
            {
                List<string> text = Textspalten(t);
                if (text.Count == 0) continue;

                string bedingung = string.Join(" OR ", text.Select(
                    s => "\"" + s + "\" LIKE '%:\\%' OR \"" + s + "\" LIKE '%/Users/%'"));
                if (Vorlagenbau.Zaehle2("SELECT COUNT(*) FROM \"" + t + "\" WHERE " + bedingung) <= 0) continue;

                foreach (string s in text)
                {
                    long n = Vorlagenbau.Zaehle2("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"" + s +
                                                 "\" LIKE '%:\\%' OR \"" + s + "\" LIKE '%/Users/%'");
                    if (n > 0) ergebnis.Add(t + "." + s + ": " + n + " Zeile(n)");
                }
            }
            return ergebnis;
        }

        private static List<string> Textspalten(string tabelle)
        {
            var s = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM pragma_table_info(?) WHERE upper(type) LIKE 'TEXT%' ORDER BY cid",
                new DbParam("?", tabelle));
            foreach (DataRow r in dt.Rows) s.Add(Convert.ToString(r["name"]));
            return s;
        }

        /// <summary>
        /// Zahl der Tabellen mit <c>STRICT</c>. Sie steht im <c>CREATE TABLE</c>-Text hinter
        /// der schliessenden Klammer; ein Spaltenname „strict" wuerde eine reine
        /// Textsuche taeuschen, deshalb wird nur der Rest hinter der LETZTEN Klammer
        /// betrachtet.
        /// </summary>
        private static int StrictTabellen()
        {
            int n = 0;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'");
            foreach (DataRow r in dt.Rows)
            {
                string sql = r["sql"] == DBNull.Value ? "" : Convert.ToString(r["sql"]);
                int klammer = sql.LastIndexOf(')');
                if (klammer < 0) continue;
                if (sql.Substring(klammer).IndexOf("STRICT", StringComparison.OrdinalIgnoreCase) >= 0) n++;
            }
            return n;
        }

        /// <summary>
        /// Der Vorher-Wert. Gezaehlt wird auf der ARBEITSKOPIE, bevor an ihr etwas
        /// geaendert ist — nie auf der Quelle: Ein Oeffnen der Quelle durch die
        /// Zugriffsschicht legte <c>-wal</c> und <c>-shm</c> daneben und schriebe damit an
        /// der Datei, die unberuehrt bleiben muss. <c>VACUUM INTO</c> uebernimmt das
        /// Schema unveraendert, die Kopie ist also derselbe Zeuge.
        /// </summary>
        internal static int StrictTabellenDerKopie() => StrictTabellen();
    }
}
