using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BAUTEILAUFBAU UND SCHICHTEN - Schemaschritt S-B der Gebaeudesimulation, Stufe G3
    // (Softwarearchitektur Gebaeudesimulation 2.2, 2.4, W5, W11, L1; Mehrzonenkonzept 4.2).
    //
    // WOZU. Der Bauteilaufbau ist das Wiederverwendbare am Bauteil: eine Wand „Kalksandstein
    // mit WDVS" gilt fuer viele Bauteile vieler Zonen. Er besteht aus geordneten Schichten
    // (innen -> aussen, lueckenlos ab 1), und jede Schicht traegt ihre Stoffwerte als KOPIE
    // zum Zeitpunkt der Zuordnung - eine spaetere Katalogaenderung verschiebt kein
    // gerechnetes Ergebnis rueckwirkend.
    //
    // VIER TABELLEN, ZWEI SEITEN. Katalog (Tab_Bauteilaufbau_STAMM, Tab_Bauteilschicht_STAMM)
    // und Projektkopie (Tab_Bauteilaufbau, Tab_Bauteilschicht). Die Spalte ID_Baustoff heisst
    // auf beiden Seiten gleich, ihr REFERENCES zeigt aber je Seite auf die EIGENE Ablage
    // (W11): der Katalog auf Tab_Baustoff_STAMM, die Kopie auf Tab_Baustoff. Zeigte die
    // Stammseite auf Projektdaten, truege die Auslieferungsdatenbank Verweise auf geloeschte
    // Projektzeilen.
    //
    // DIE SCHICHT (L1, W5). Eltern ist der Aufbau, ID_Aufbau NOT NULL mit ON DELETE CASCADE -
    // die Kaskade zeigt NUR zum Eltern; ID_Baustoff kaskadiert nicht, NULL heisst freie
    // Eingabe. Die Schicht fuehrt KEIN ReadOnly und KEINE Herkunft: Sie gehoert zur Auslieferung,
    // wenn ihr Aufbau dazugehoert, und sie erbt dessen Herkunft. Werkzeuge/Auslieferungsvorlage
    // fuehrt Tab_Bauteilschicht_STAMM deshalb in ihrer namentlichen Liste der Kindkataloge ohne
    // ReadOnly. Kein ID_Projekt an der Schicht (W16): Sie haengt ueber ihren Aufbau am Projekt.
    //
    // KEINE SAAT. Beide Kataloge entstehen leer; ergebnisneutral, der Referenzlauf bleibt
    // byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL der Bauteilaufbauten und ihrer Schichten</b> — Schemaschritt S-B (Nummer
    /// <see cref="SCHRITT"/>). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class BauteilaufbauSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht. Er folgt auf
        /// S-A (<see cref="BaustoffSchema.SCHRITT"/>), dessen Tabellen die Schichten ansprechen.
        /// </summary>
        public const int SCHRITT = BaustoffSchema.SCHRITT + 1;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Aufbaukatalog (Auslieferung).</summary>
        public const string TAB_AUFBAU_STAMM = SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM;

        /// <summary>Projektkopie der Aufbauten.</summary>
        public const string TAB_AUFBAU = SchemaKatalog.TAB_BAUTEILAUFBAU;

        /// <summary>Schichten der Katalogaufbauten — Kindkatalog ohne <c>ReadOnly</c> (L1).</summary>
        public const string TAB_SCHICHT_STAMM = SchemaKatalog.TAB_BAUTEILSCHICHT_STAMM;

        /// <summary>Schichten der Projektaufbauten.</summary>
        public const string TAB_SCHICHT = SchemaKatalog.TAB_BAUTEILSCHICHT;

        /// <summary>Index der Katalogschichten über (<c>ID_Aufbau</c>, <c>Reihenfolge</c>).</summary>
        public const string INDEX_SCHICHT_STAMM = "idx_Bauteilschicht_STAMM_Aufbau";

        /// <summary>Index der Projektschichten über (<c>ID_Aufbau</c>, <c>Reihenfolge</c>) — der Leseweg je Projekt (2.9).</summary>
        public const string INDEX_SCHICHT = "idx_Bauteilschicht_Aufbau";

        /// <summary>Name des Aufbaus, NOT NULL, höchstens 80 Zeichen.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Beschreibung, höchstens <see cref="LAENGE_BESCHREIBUNG"/> Zeichen; NULL = keine.</summary>
        public const string SPALTE_BESCHREIBUNG = "Beschreibung";

        /// <summary>
        /// Die Bauteilart, für die der Aufbau gedacht ist (<see cref="DbWerte.BAUTEILARTEN"/>);
        /// <b>NULL = für jede Bauteilart</b>.
        /// </summary>
        public const string SPALTE_BAUTEILART = "Bauteilart";

        /// <summary>Regelwerk oder <b>Dateiname</b> des Imports (nie ein Pfad); NULL = nicht angegeben.</summary>
        public const string SPALTE_QUELLE = "Quelle";

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); NULL = nicht angegeben.</summary>
        public const string SPALTE_HERKUNFT = "Herkunft";

        /// <summary>Kennung der Quellentität eines Imports; NULL = keine (W10).</summary>
        public const string SPALTE_QUELLKENNUNG = "Quellkennung";

        /// <summary>„Gehört zur Auslieferung" — nur am Katalogaufbau, nie an der Schicht (L1).</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Projekt der Kopie — nur am Projektaufbau, ohne Fremdschlüssel (W16).</summary>
        public const string SPALTE_ID_PROJEKT = "ID_Projekt";

        /// <summary>Eltern der Schicht, NOT NULL, <c>ON DELETE CASCADE</c> (W5).</summary>
        public const string SPALTE_ID_AUFBAU = "ID_Aufbau";

        /// <summary>Lage der Schicht, innen → außen, lückenlos ab 1.</summary>
        public const string SPALTE_REIHENFOLGE = "Reihenfolge";

        /// <summary>Stoff der Schicht, ohne Kaskade, je Seite auf die eigene Ablage (W11); <b>NULL = freie Eingabe</b>.</summary>
        public const string SPALTE_ID_BAUSTOFF = "ID_Baustoff";

        /// <summary>Schichtdicke [m], NOT NULL.</summary>
        public const string SPALTE_DICKE = "Dicke";

        /// <summary>Schalter (0/1, NOT NULL DEFAULT 0): ruhende Luftschicht — Widerstand nach DIN EN ISO 6946.</summary>
        public const string SPALTE_IST_LUFTSCHICHT = "IstLuftschicht";

        /// <summary>Wärmeleitfähigkeit [W/(m·K)] — Kopie zum Zeitpunkt der Zuordnung; NULL = nicht angegeben.</summary>
        public const string SPALTE_LAMBDA = "Lambda";

        /// <summary>Rohdichte [kg/m³] — Kopie; NULL = nicht angegeben.</summary>
        public const string SPALTE_RHO = "Rho";

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)] — Kopie; NULL = nicht angegeben.</summary>
        public const string SPALTE_CP = "cp";

        /// <summary>Höchstlänge der Beschreibung.</summary>
        public const int LAENGE_BESCHREIBUNG = 250;

        /// <summary>
        /// Die Wertliste der Bauteilart als SQL-Literal — die EINE Quelle der zwei
        /// <c>CHECK</c>-Klauseln am Aufbau und der am Bauteil (<see cref="ZonenSchema"/>).
        /// Konstant zusammengesetzt aus <see cref="DbWerte"/>; der Nachweis hält sie gegen
        /// <see cref="DbWerte.BAUTEILARTEN"/>.
        /// </summary>
        public const string WERTE_BAUTEILART =
            "'" + DbWerte.BAUTEILART_AUSSENWAND + "','" + DbWerte.BAUTEILART_DACH + "','" +
            DbWerte.BAUTEILART_BODENPLATTE + "','" + DbWerte.BAUTEILART_FENSTER + "','" +
            DbWerte.BAUTEILART_TUER + "','" + DbWerte.BAUTEILART_INNENWAND + "','" +
            DbWerte.BAUTEILART_DECKE + "','" + DbWerte.BAUTEILART_VORHANGFASSADE + "','" +
            DbWerte.BAUTEILART_SONSTIGES + "'";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Bauteilaufbau_STAMM</c> — acht Spalten, STRICT.</summary>
        public const string SQL_CREATE_AUFBAU_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Bauteilaufbau_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Beschreibung\" TEXT CHECK (length(\"Beschreibung\") <= 250),\n" +
            "    \"Bauteilart\" TEXT CHECK (\"Bauteilart\" IN (" + WERTE_BAUTEILART + ")),\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + BaustoffSchema.WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Bauteilaufbau</c> — die Projektkopie, spaltengleich,
        /// zusätzlich <c>ID_Projekt</c> (ohne Fremdschlüssel), ohne <c>ReadOnly</c>.
        /// </summary>
        public const string SQL_CREATE_AUFBAU =
            "CREATE TABLE IF NOT EXISTS \"Tab_Bauteilaufbau\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Beschreibung\" TEXT CHECK (length(\"Beschreibung\") <= 250),\n" +
            "    \"Bauteilart\" TEXT CHECK (\"Bauteilart\" IN (" + WERTE_BAUTEILART + ")),\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + BaustoffSchema.WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64)\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Bauteilschicht_STAMM</c> — die Schichten der
        /// Katalogaufbauten; Kaskade nur zum Aufbau, <c>ID_Baustoff</c> auf den Katalog (W11).
        /// </summary>
        public const string SQL_CREATE_SCHICHT_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Bauteilschicht_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Aufbau\" INTEGER NOT NULL,\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL,\n" +
            "    \"ID_Baustoff\" INTEGER,\n" +
            "    \"Dicke\" REAL NOT NULL,\n" +
            "    \"IstLuftschicht\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstLuftschicht\" IN (0,1)),\n" +
            "    \"Lambda\" REAL,\n" +
            "    \"Rho\" REAL,\n" +
            "    \"cp\" REAL,\n" +
            "    FOREIGN KEY (\"ID_Aufbau\") REFERENCES \"Tab_Bauteilaufbau_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff_STAMM\" (\"ID\")\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Bauteilschicht</c> — die Schichten der
        /// Projektaufbauten; Kaskade nur zum Aufbau, <c>ID_Baustoff</c> auf die Projektkopie (W11).
        /// </summary>
        public const string SQL_CREATE_SCHICHT =
            "CREATE TABLE IF NOT EXISTS \"Tab_Bauteilschicht\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Aufbau\" INTEGER NOT NULL,\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL,\n" +
            "    \"ID_Baustoff\" INTEGER,\n" +
            "    \"Dicke\" REAL NOT NULL,\n" +
            "    \"IstLuftschicht\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstLuftschicht\" IN (0,1)),\n" +
            "    \"Lambda\" REAL,\n" +
            "    \"Rho\" REAL,\n" +
            "    \"cp\" REAL,\n" +
            "    FOREIGN KEY (\"ID_Aufbau\") REFERENCES \"Tab_Bauteilaufbau\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff\" (\"ID\")\n" +
            ") STRICT";

        /// <summary>Der Index der Katalogschichten.</summary>
        public const string SQL_INDEX_SCHICHT_STAMM =
            "CREATE INDEX IF NOT EXISTS \"idx_Bauteilschicht_STAMM_Aufbau\" ON \"Tab_Bauteilschicht_STAMM\" " +
            "(\"ID_Aufbau\", \"Reihenfolge\")";

        /// <summary>Der Index der Projektschichten.</summary>
        public const string SQL_INDEX_SCHICHT =
            "CREATE INDEX IF NOT EXISTS \"idx_Bauteilschicht_Aufbau\" ON \"Tab_Bauteilschicht\" " +
            "(\"ID_Aufbau\", \"Reihenfolge\")";

        /// <summary>Die vier Tabellen in Anlegereihenfolge (Eltern vor Kind), je Tabellenname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_AUFBAU_STAMM, SQL_CREATE_AUFBAU_STAMM);
                yield return new KeyValuePair<string, string>(TAB_AUFBAU, SQL_CREATE_AUFBAU);
                yield return new KeyValuePair<string, string>(TAB_SCHICHT_STAMM, SQL_CREATE_SCHICHT_STAMM);
                yield return new KeyValuePair<string, string>(TAB_SCHICHT, SQL_CREATE_SCHICHT);
            }
        }

        /// <summary>Die zwei Indizes, je Indexname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(INDEX_SCHICHT_STAMM, SQL_INDEX_SCHICHT_STAMM);
                yield return new KeyValuePair<string, string>(INDEX_SCHICHT, SQL_INDEX_SCHICHT);
            }
        }

        /// <summary>
        /// Alle sechs Anweisungen in der festen Handgriffreihenfolge (R2): Tabellen, dann
        /// Indizes — so, wie Migration, Werkzeug und Testvorrichtung sie abarbeiten.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
            => Tabellenanweisungen.Concat(Indexanweisungen);

        /// <summary>
        /// Die FACHSPALTEN beider Aufbautabellen in Schemareihenfolge — ohne <c>ID</c>,
        /// <c>ID_Projekt</c>, <c>Bezeichner</c> und <c>ReadOnly</c>; die EINE Liste des
        /// Kopierwegs <c>BauteilaufbauCtrl.CopyFromStamm</c>.
        /// </summary>
        public static readonly IReadOnlyList<string> Fachspalten = new[]
        {
            SPALTE_BESCHREIBUNG, SPALTE_BAUTEILART, SPALTE_QUELLE, SPALTE_HERKUNFT, SPALTE_QUELLKENNUNG
        };

        /// <summary>
        /// Die Spalten beider Schichttabellen in Schemareihenfolge, ohne <c>ID</c> — die EINE
        /// Liste, an der Lesen, Schreiben und Kopieren der Schichten hängen.
        /// </summary>
        public static readonly IReadOnlyList<string> Schichtspalten = new[]
        {
            SPALTE_ID_AUFBAU, SPALTE_REIHENFOLGE, SPALTE_ID_BAUSTOFF, SPALTE_DICKE,
            SPALTE_IST_LUFTSCHICHT, SPALTE_LAMBDA, SPALTE_RHO, SPALTE_CP
        };

        /// <summary>Stehen alle vier Tabellen und beide Indizes?</summary>
        public static bool Vollstaendig()
        {
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key));
                if (n == null || System.Convert.ToInt64(n, System.Globalization.CultureInfo.InvariantCulture) == 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Zug aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b> (<c>IF NOT EXISTS</c>), <b>kein DML</b>.
        /// </summary>
        /// <returns>Zahl der in diesem Lauf angelegten Tabellen (0 bis 4).</returns>
        public static int Ausfuehren()
        {
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
            {
                bool vorher = DataRepository.TabelleVorhanden(a.Key);
                DataRepository.ExecuteNonQuery(a.Value);
                if (!vorher && DataRepository.TabelleVorhanden(a.Key)) angelegt++;
            }
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            return angelegt;
        }
    }
}
