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
    /// der Datenschutzwaechter und die Beidateien. Die ersten vier sind die
    /// Kontrollabfragen, die <c>sql/tools/Reduziere-Testdatenbank.sql</c> am Ende zum
    /// Kopieren auffuehrt; die drei uebrigen kommen aus dem Zweck dieser Datei — sie geht
    /// an Dritte.</para>
    /// </summary>
    internal sealed class Prueflauf
    {
        private readonly Bericht _bericht;
        private readonly Projektsicht _sicht;

        internal Prueflauf(Bericht bericht, Projektsicht sicht) { _bericht = bericht; _sicht = sicht; }

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
            return ok;
        }

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
