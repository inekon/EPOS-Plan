using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER NAMENSABGLEICH DER BAUSTOFFE - Schemaschritt der Stufe G4b (Ergaenzung, Welle 1;
    // Mehrzonenkonzept 3.5 und 6.3, Entscheid E27 zu M9, Datenaustauschkonzept 3.6).
    //
    // WOZU. IFC-Dateien liefern fast nie brauchbare Stoffwerte (Befund P, § 3.4: Nullen oder gar
    // keine Saetze). Der Namensabgleich N1...N7 ordnet die Materialnamen der Datei den Baustoffen des
    // Katalogs zu; dafuer braucht er zwei Tabellen:
    //
    //   Tab_Baustoffsynonym_STAMM - die SYNONYMTABELLE DER AUSLIEFERUNG (N4, E27/M9): ein Materialname,
    //       wie ihn ein Autorensystem schreibt (normalisiert), seine Sprache (de, en), der Baustoff des
    //       Katalogs und die Quelle. Gesaet mit festen Ids und ReadOnly = 1 (BaustoffsynonymSaat.cs);
    //       die Saat legt nur an, was fehlt - wie die Baustoffsaat. Eine Datentabelle, kein Anzeigetext:
    //       sie gehoert nicht in die .resx (Mehrzonenkonzept 3.5).
    //
    //   Tab_Baustoffzuordnung - die GEMERKTE ANWENDERZUORDNUNG (N7) JE PROJEKT: Projekt, normalisierter
    //       Materialname der Datei, Baustoff des Katalogs, Zeitpunkt. "Je Projekt" folgt Befund P,
    //       § 3.6 ("wird projektweit (spaeter katalogweit) behalten") und E27/M9 ("je Projekt gepflegte
    //       Zuordnungen ergaenzen sie"). Sie ist Projektdatenbestand: Kopie und Transfer nehmen sie mit,
    //       die Auslieferungsvorlage leert sie (Projektspalte, Loeschweitergabe).
    //
    // BEIDE VERWEISE ZEIGEN AUF DEN KATALOG (Tab_Baustoff_STAMM), nicht auf die Projektkopie: Der
    // Abgleich waehlt aus dem Katalog; die Projektkopie entsteht erst beim Schreiben des Vorschlags
    // (BaustoffCtrl.CopyFromStamm). Die Spalte heisst wie an Tab_Bauteilschicht_STAMM ID_Baustoff; ihr
    // REFERENCES zeigt auf den Katalog. Loeschweitergabe nach Hausregel: faellt ein Katalogbaustoff,
    // fallen seine Synonyme und die gemerkten Zuordnungen mit - ein Verweis ins Leere haette keine
    // Bedeutung. ON UPDATE CASCADE wie an jedem Projektfremdschluessel (Schritt 96).
    //
    // BAUFORM. STRICT, AUTOINCREMENT, CHECK (length(...)) statt Typlaenge. Je Materialname hoechstens
    // EIN Synonym (eindeutiger Index) und je Projekt und Materialname hoechstens EINE Zuordnung - der
    // Abgleich bleibt so eindeutig; ein neues Merken ersetzt das alte.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Tabellen; der Abgleich laeuft nur im Gebaeudeimport auf
    // Zuruf. Die Einfrierregeln der Referenzbasis nennen keine Baustoffe und keine Synonyme; der
    // Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL und die Saat des Namensabgleichs</b> — die Synonymtabelle der Auslieferung und die
    /// gemerkten Zuordnungen je Projekt (Nummer <see cref="SCHRITT"/>). EINE Quelle für die Migration der
    /// Schale, <c>Werkzeuge/Testdatenbankschema</c>, die Testvorrichtung und den Nachweis. Anlass und
    /// Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class BaustoffabgleichSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration, Werkzeug,
        /// Zielstand und Tests verweisen hierher. Sie folgt auf den Konstruktor des Zapfprofilgenerators (<see cref="TwwSchema.SCHRITT_T5_KONSTRUKTOR"/>);
        /// wird der Schritt beim Zusammenführen umnummeriert, ändert sich nur diese Zahl.
        /// </summary>
        public const int SCHRITT = 146;

        // =================================================================
        //  Namen
        // =================================================================

        /// <summary>Die Synonymtabelle der Auslieferung.</summary>
        public const string TAB_SYNONYM = SchemaKatalog.TAB_BAUSTOFFSYNONYM_STAMM;

        /// <summary>Die gemerkten Zuordnungen je Projekt.</summary>
        public const string TAB_ZUORDNUNG = SchemaKatalog.TAB_BAUSTOFFZUORDNUNG;

        /// <summary>Der normalisierte Materialname (beide Tabellen), NOT NULL, 1 … <see cref="LAENGE_MATERIALNAME"/> Zeichen.</summary>
        public const string SPALTE_MATERIALNAME = "Materialname";

        /// <summary>Die Sprache des Synonyms (<see cref="SPRACHEN"/>).</summary>
        public const string SPALTE_SPRACHE = "Sprache";

        /// <summary>Der Baustoff des Katalogs (beide Tabellen) — Fremdschlüssel auf <c>Tab_Baustoff_STAMM</c>.</summary>
        public const string SPALTE_ID_BAUSTOFF = "ID_Baustoff";

        /// <summary>Woher das Synonym stammt; NULL = nicht angegeben.</summary>
        public const string SPALTE_QUELLE = "Quelle";

        /// <summary>„Gehört zur Auslieferung" — nur an den Synonymen.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Das Projekt der Zuordnung — Fremdschlüssel auf <c>Tab_Projekt</c> mit Löschweitergabe.</summary>
        public const string SPALTE_ID_PROJEKT = "ID_Projekt";

        /// <summary>Wann die Zuordnung gemerkt wurde (ISO 8601).</summary>
        public const string SPALTE_ZEITPUNKT = "Zeitpunkt";

        /// <summary>Höchstlänge des Materialnamens.</summary>
        public const int LAENGE_MATERIALNAME = 120;

        /// <summary>Höchstlänge der Quelle.</summary>
        public const int LAENGE_QUELLE = 120;

        /// <summary>Höchstlänge des Zeitpunkts.</summary>
        public const int LAENGE_ZEITPUNKT = 32;

        /// <summary>Die Sprachen der Synonymtabelle (zweisprachig, Mehrzonenkonzept 6.3).</summary>
        public static readonly IReadOnlyList<string> SPRACHEN = new[] { "de", "en" };

        /// <summary>
        /// <b>Die Ids unterhalb dieser Grenze gehören der Synonymsaat</b> — dieselbe Regel wie beim
        /// Baustoffkatalog (<see cref="BaustoffSchema.SAAT_ID_GRENZE"/>): <see cref="SaatSchreiben"/> hebt die
        /// AUTOINCREMENT-Folge auf <c>SAAT_ID_GRENZE − 1</c>.
        /// </summary>
        public const int SAAT_ID_GRENZE = 10000;

        /// <summary>Eindeutiger Index: je Materialname ein Synonym.</summary>
        public const string INDEX_SYNONYM_NAME = "idx_Baustoffsynonym_Materialname";

        /// <summary>Index auf den Baustoff der Synonyme (Löschweitergabe).</summary>
        public const string INDEX_SYNONYM_BAUSTOFF = "idx_Baustoffsynonym_Baustoff";

        /// <summary>Eindeutiger Index: je Projekt und Materialname eine Zuordnung.</summary>
        public const string INDEX_ZUORDNUNG_NAME = "idx_Baustoffzuordnung_Projekt_Materialname";

        /// <summary>Index auf den Baustoff der Zuordnungen (Löschweitergabe).</summary>
        public const string INDEX_ZUORDNUNG_BAUSTOFF = "idx_Baustoffzuordnung_Baustoff";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Baustoffsynonym_STAMM</c> — sechs Spalten, STRICT.</summary>
        public const string SQL_CREATE_SYNONYM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Baustoffsynonym_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Materialname\" TEXT NOT NULL CHECK (length(\"Materialname\") BETWEEN 1 AND 120),\n" +
            "    \"Sprache\" TEXT NOT NULL CHECK (\"Sprache\" IN ('de','en')),\n" +
            "    \"ID_Baustoff\" INTEGER NOT NULL,\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff_STAMM\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Baustoffzuordnung</c> — fünf Spalten, STRICT, je Projekt.</summary>
        public const string SQL_CREATE_ZUORDNUNG =
            "CREATE TABLE IF NOT EXISTS \"Tab_Baustoffzuordnung\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL,\n" +
            "    \"Materialname\" TEXT NOT NULL CHECK (length(\"Materialname\") BETWEEN 1 AND 120),\n" +
            "    \"ID_Baustoff\" INTEGER NOT NULL,\n" +
            "    \"Zeitpunkt\" TEXT NOT NULL CHECK (length(\"Zeitpunkt\") <= 32),\n" +
            "    FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff_STAMM\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE\n" +
            ") STRICT";

        /// <summary>Der eindeutige Index der Synonyme.</summary>
        public const string SQL_INDEX_SYNONYM_NAME =
            "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_Baustoffsynonym_Materialname\" ON \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\")";

        /// <summary>Der Index auf den Baustoff der Synonyme.</summary>
        public const string SQL_INDEX_SYNONYM_BAUSTOFF =
            "CREATE INDEX IF NOT EXISTS \"idx_Baustoffsynonym_Baustoff\" ON \"Tab_Baustoffsynonym_STAMM\" (\"ID_Baustoff\")";

        /// <summary>Der eindeutige Index der Zuordnungen je Projekt.</summary>
        public const string SQL_INDEX_ZUORDNUNG_NAME =
            "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_Baustoffzuordnung_Projekt_Materialname\" ON \"Tab_Baustoffzuordnung\" (\"ID_Projekt\", \"Materialname\")";

        /// <summary>Der Index auf den Baustoff der Zuordnungen.</summary>
        public const string SQL_INDEX_ZUORDNUNG_BAUSTOFF =
            "CREATE INDEX IF NOT EXISTS \"idx_Baustoffzuordnung_Baustoff\" ON \"Tab_Baustoffzuordnung\" (\"ID_Baustoff\")";

        /// <summary>Die zwei Tabellen in Anlegereihenfolge, je Tabellenname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_SYNONYM, SQL_CREATE_SYNONYM);
                yield return new KeyValuePair<string, string>(TAB_ZUORDNUNG, SQL_CREATE_ZUORDNUNG);
            }
        }

        /// <summary>Die vier Indizes, je Indexname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(INDEX_SYNONYM_NAME, SQL_INDEX_SYNONYM_NAME);
                yield return new KeyValuePair<string, string>(INDEX_SYNONYM_BAUSTOFF, SQL_INDEX_SYNONYM_BAUSTOFF);
                yield return new KeyValuePair<string, string>(INDEX_ZUORDNUNG_NAME, SQL_INDEX_ZUORDNUNG_NAME);
                yield return new KeyValuePair<string, string>(INDEX_ZUORDNUNG_BAUSTOFF, SQL_INDEX_ZUORDNUNG_BAUSTOFF);
            }
        }

        /// <summary>Alle Anweisungen in der festen Handgriffreihenfolge (R2) — Tabellen, dann Indizes.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen => Tabellenanweisungen.Concat(Indexanweisungen);

        // =================================================================
        //  Die Saat (BaustoffsynonymSaat.cs)
        // =================================================================

        /// <summary>Die Synonymsaat der Auslieferung (<see cref="BaustoffsynonymSaattabelle.Alle"/>).</summary>
        public static IReadOnlyList<BaustoffsynonymSaat> Saat => BaustoffsynonymSaattabelle.Alle;

        // =================================================================
        //  Auskunft
        // =================================================================

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Tabellen (0 bis 2).</summary>
            public int TabellenAngelegt;

            /// <summary>Zahl der geschriebenen Saatzeilen.</summary>
            public int Gesaet;

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
                => TabellenAngelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" + TAB_SYNONYM + ", " +
                   TAB_ZUORDNUNG + "), vier Indizes, " + Gesaet.ToString(CultureInfo.InvariantCulture) + " von " +
                   Saat.Count.ToString(CultureInfo.InvariantCulture) + " Synonym(en) gesaet (ReadOnly = 1)";
        }

        /// <summary>Stehen beide Tabellen und die vier Indizes?</summary>
        public static bool TabellenVorhanden()
        {
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
                if (Anzahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key)) == 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Steht der Zielstand — Tabellen und Indizes da, jede Saatzeile unter ihrer festen Id mit
        /// <c>ReadOnly = 1</c>, die Folge über der Saatgrenze?
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!TabellenVorhanden()) return false;
            if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_SYNONYM + "\" WHERE \"ID\" BETWEEN 1 AND ? AND \"ReadOnly\" = 1",
                       new DbParam("@max", SAAT_ID_GRENZE - 1)) < Saat.Count)
                return false;
            object seq = DataRepository.ExecuteScalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", TAB_SYNONYM));
            return seq != null && seq != DBNull.Value && Convert.ToInt64(seq, CultureInfo.InvariantCulture) >= SAAT_ID_GRENZE - 1;
        }

        // =================================================================
        //  Ausführen
        // =================================================================

        /// <summary>
        /// Legt Tabellen und Indizes an (wenn sie fehlen) und schreibt die Saat — für
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration der Schale geht
        /// denselben Weg über ihre Helfer. <b>Wiederholbar.</b> Setzt den Baustoffkatalog voraus
        /// (<see cref="BaustoffSchema"/>). Fehler werfen — der Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
            {
                bool vorher = DataRepository.TabelleVorhanden(a.Key);
                DataRepository.ExecuteNonQuery(a.Value);
                if (!vorher && DataRepository.TabelleVorhanden(a.Key)) b.TabellenAngelegt++;
            }
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            b.Gesaet = SaatSchreiben();
            return b;
        }

        /// <summary>
        /// Schreibt die fehlenden Synonyme und hebt danach die AUTOINCREMENT-Folge auf die Saatgrenze.
        /// <b>Wiederholbar und nie überschreibend:</b> Eine Zeile, deren Id oder Materialname schon
        /// belegt ist, wird übergangen; eine Zeile, deren Baustoff im Katalog fehlt, ebenso (der
        /// Fremdschlüssel verlangte ihn) — <see cref="Vollstaendig"/> meldet das.
        /// </summary>
        /// <returns>Zahl der angelegten Zeilen.</returns>
        public static int SaatSchreiben()
        {
            int angelegt = 0;
            foreach (BaustoffsynonymSaat s in Saat)
            {
                if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_SYNONYM + "\" WHERE \"ID\" = ?", new DbParam("@id", s.Id)) > 0) continue;
                if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_SYNONYM + "\" WHERE \"Materialname\" = ?", new DbParam("@m", s.Materialname)) > 0) continue;
                if (Anzahl("SELECT COUNT(*) FROM \"" + BaustoffSchema.TAB_STAMM + "\" WHERE \"ID\" = ?", new DbParam("@b", s.IdBaustoff)) == 0) continue;

                int n = DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"" + TAB_SYNONYM + "\" (\"ID\", \"Materialname\", \"Sprache\", \"ID_Baustoff\", \"Quelle\", \"ReadOnly\") " +
                    "VALUES (?, ?, ?, ?, ?, 1)",
                    new DbParam("@id", s.Id),
                    new DbParam("@m", s.Materialname),
                    new DbParam("@sp", s.Sprache),
                    new DbParam("@b", s.IdBaustoff),
                    new DbParam("@q", s.Quelle));
                if (n == 1) angelegt++;
            }

            DataRepository.ExecuteNonQuery(
                "UPDATE sqlite_sequence SET seq = ? WHERE name = ? AND seq < ?",
                new DbParam("@s", SAAT_ID_GRENZE - 1), new DbParam("@t", TAB_SYNONYM), new DbParam("@g", SAAT_ID_GRENZE - 1));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO sqlite_sequence (name, seq) SELECT ?, ? WHERE NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name = ?)",
                new DbParam("@t", TAB_SYNONYM), new DbParam("@s", SAAT_ID_GRENZE - 1), new DbParam("@t2", TAB_SYNONYM));
            return angelegt;
        }

        private static long Anzahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
