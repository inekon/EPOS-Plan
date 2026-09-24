using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER BAUSTOFFKATALOG - Schemaschritt S-A der Gebaeudesimulation, Stufe G3
    // (Softwarearchitektur Gebaeudesimulation 2.2 und 2.4; Mehrzonenkonzept 3.5 und 4.4).
    //
    // WOZU. Ein Bauteilaufbau besteht aus Schichten, und eine Schicht braucht die Stoffwerte
    // Lambda, Rho und cp. Bis hierher gab es keinen Ort dafuer. Der Schritt legt den Katalog
    // (Tab_Baustoff_STAMM) und seine Projektkopie (Tab_Baustoff) an und saet den Katalog mit
    // Norm- und Richtwerten nach DIN 4108-4 und DIN EN ISO 10456, je Zeile mit Quelle.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei NutzungsdauerSchema
    // (75) und ProjektWirkungSchema (127): VIER Leser - der Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema, die Testvorrichtung EPOS.Kern.Tests/TestDatenbank und der
    // Kopierweg BaustoffCtrl.CopyFromStamm (Fachspalten).
    //
    // SPALTENGLEICH (Regel WechselrichterSchema): Katalog und Projektkopie tragen dieselben
    // Fachspalten in derselben Reihenfolge; nur ReadOnly (Katalog) und ID_Projekt (Kopie)
    // unterscheiden sie. Eine Spalte nur auf einer Seite waere beim CopyFromStamm sofort ein
    // Datenverlust.
    //
    // HERSTELLER (Anwenderwunsch 24.09.2026): eine Spalte ueber 2.2 hinaus. NULL heisst
    // herstellerneutral - ein Norm- oder Richtwert. Die Kataloge der wichtigsten Hersteller
    // werden in einem spaeteren Schritt gesaet; die Spalte entsteht schon jetzt, damit dieser
    // Schritt kein DDL braucht.
    //
    // DER PROJEKTFREMDSCHLUESSEL (Hausregel seit Schemaschritt 96, BETRIEB_SQLITE.md 2a).
    // ID_Projekt der Projektkopie traegt FOREIGN KEY auf Tab_Projekt mit ON DELETE CASCADE
    // ON UPDATE CASCADE - wie ihre Vorbilder Tab_Wechselrichter und Tab_PV heute: Ein
    // geloeschtes Projekt nimmt seine Baustoffe mit, eine Waise kann nicht entstehen, und der
    // Waechter ProjektFremdschluesselTests haelt jede Tabelle mit Projektspalte darauf.
    // Softwarearchitektur 2.2 ("ohne Fremdschluessel, wie der Bestand") beschreibt den Stand
    // vor Schritt 96 und ist an dieser Stelle vom Bestand ueberholt. Die Projektkopie reist
    // ueber ihre Spalte ID_Projekt mit (ProjektDuplizierenCtrl, Projekttransfer).
    //
    // DIE SAAT (BaustoffSaat.cs). 132 Zeilen mit fester Id: 65 herstellerneutrale Normzeilen
    // (1 bis 65) und 67 Herstellerzeilen (1001 bis 1067), ReadOnly = 1, Herkunft = VORGABE und
    // die Quelle je Zeile; Bemerkungen und Belege der Recherche stehen NICHT in der Datenbank.
    // Die Ids unter SAAT_ID_GRENZE sind der Auslieferungssaat vorbehalten: SaatSchreiben hebt
    // die AUTOINCREMENT-Folge des Katalogs auf die Grenze, damit eine vom Anwender angelegte
    // Zeile nie eine Id bekommt, die eine Saat fest vergibt.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Tabellen; der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL und die Saat des Baustoffkatalogs</b> — Schemaschritt S-A (Nummer
    /// <see cref="SCHRITT"/>). Anlass, Bauform und Ergebnisneutralität stehen im Kopf der Datei.
    /// </summary>
    public static class BaustoffSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: Migration,
        /// Werkzeug und Nachweis lesen sie hier. Vergeben am 24.09.2026 für die Welle G3-B;
        /// 129 ist einem parallelen Schritt zugesagt.
        /// </summary>
        public const int SCHRITT = 130;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Der Katalog (Auslieferung).</summary>
        public const string TAB_STAMM = SchemaKatalog.TAB_BAUSTOFF_STAMM;

        /// <summary>Die Projektkopie.</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_BAUSTOFF;

        /// <summary>Name des Stoffes, NOT NULL, höchstens <see cref="LAENGE_BEZEICHNER"/> Zeichen (W3).</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Ordnungsgruppe; NULL = ohne Gruppe.</summary>
        public const string SPALTE_GRUPPE = "Gruppe";

        /// <summary>
        /// Hersteller des Produkts; <b>NULL = herstellerneutral</b> (Norm- oder Richtwert).
        /// Anwenderwunsch vom 24.09.2026: Herstellerkataloge kommen in einem späteren Schritt.
        /// </summary>
        public const string SPALTE_HERSTELLER = "Hersteller";

        /// <summary>Wärmeleitfähigkeit [W/(m·K)]; NULL = nicht angegeben.</summary>
        public const string SPALTE_LAMBDA = "Lambda";

        /// <summary>Rohdichte [kg/m³]; NULL = nicht angegeben.</summary>
        public const string SPALTE_RHO = "Rho";

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)]; NULL = nicht angegeben.</summary>
        public const string SPALTE_CP = "cp";

        /// <summary>Regelwerk oder Dateiname des Imports; NULL = nicht angegeben.</summary>
        public const string SPALTE_QUELLE = "Quelle";

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); NULL = nicht angegeben (W9).</summary>
        public const string SPALTE_HERKUNFT = "Herkunft";

        /// <summary>Kennung der Quellentität eines Imports (IFC-GUID, gbXML-id); NULL = keine (W10).</summary>
        public const string SPALTE_QUELLKENNUNG = "Quellkennung";

        /// <summary>„Gehört zur Auslieferung" — nur im Katalog.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Das Projekt der Kopie — nur in der Projektkopie; Fremdschlüssel auf <c>Tab_Projekt</c> mit Löschweitergabe (Schritt 96).</summary>
        public const string SPALTE_ID_PROJEKT = "ID_Projekt";

        /// <summary>Höchstlänge des Bezeichners.</summary>
        public const int LAENGE_BEZEICHNER = 80;

        /// <summary>Höchstlänge der Gruppe.</summary>
        public const int LAENGE_GRUPPE = 40;

        /// <summary>Höchstlänge des Herstellers.</summary>
        public const int LAENGE_HERSTELLER = 80;

        /// <summary>Höchstlänge der Quelle.</summary>
        public const int LAENGE_QUELLE = 120;

        /// <summary>Höchstlänge der Quellkennung (W10) — gilt an allen vier Tabellenfamilien.</summary>
        public const int LAENGE_QUELLKENNUNG = 64;

        /// <summary>
        /// Die Wertliste der Herkunft als SQL-Literal — die EINE Quelle aller vier
        /// <c>CHECK</c>-Klauseln (Baustoff, Aufbau, Zone, Bauteil). Konstant zusammengesetzt aus
        /// <see cref="DbWerte"/>; der Nachweis hält sie gegen <see cref="DbWerte.HERKUENFTE"/>.
        /// </summary>
        public const string WERTE_HERKUNFT =
            "'" + DbWerte.HERKUNFT_MANUELL + "','" + DbWerte.HERKUNFT_KATALOG + "','" +
            DbWerte.HERKUNFT_IFC + "','" + DbWerte.HERKUNFT_GBXML + "','" + DbWerte.HERKUNFT_VORGABE + "'";

        /// <summary>
        /// <b>Die Ids unterhalb dieser Grenze gehören der Auslieferungssaat</b> — die Normsaat
        /// belegt 1 bis 65 (Raum bis 999), die Herstellersaat 1001 bis 1067 (Raum bis 9 999).
        /// <see cref="SaatSchreiben"/> hebt die AUTOINCREMENT-Folge des Katalogs auf
        /// <c>SAAT_ID_GRENZE − 1</c>; eine vom Anwender angelegte Zeile beginnt damit bei
        /// dieser Grenze und kann keine feste Saat-Id besetzen.
        /// </summary>
        public const int SAAT_ID_GRENZE = 10000;

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Baustoff_STAMM</c> — elf Spalten, <b>STRICT</b>.
        /// </summary>
        public const string SQL_CREATE_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Baustoff_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Gruppe\" TEXT CHECK (length(\"Gruppe\") <= 40),\n" +
            "    \"Hersteller\" TEXT CHECK (length(\"Hersteller\") <= 80),\n" +
            "    \"Lambda\" REAL,\n" +
            "    \"Rho\" REAL,\n" +
            "    \"cp\" REAL,\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Baustoff</c> — die Projektkopie, spaltengleich,
        /// zusätzlich <c>ID_Projekt</c> (Fremdschlüssel auf <c>Tab_Projekt</c>, Löschweitergabe wie
        /// jede Projekttabelle seit Schritt 96), ohne <c>ReadOnly</c>.
        /// </summary>
        public const string SQL_CREATE_PROJEKT =
            "CREATE TABLE IF NOT EXISTS \"Tab_Baustoff\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Gruppe\" TEXT CHECK (length(\"Gruppe\") <= 40),\n" +
            "    \"Hersteller\" TEXT CHECK (length(\"Hersteller\") <= 80),\n" +
            "    \"Lambda\" REAL,\n" +
            "    \"Rho\" REAL,\n" +
            "    \"cp\" REAL,\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE\n" +
            ") STRICT";

        /// <summary>
        /// Beide Anweisungen in Anlegereihenfolge, je Tabellenname — so, wie Migration,
        /// Werkzeug und Testvorrichtung sie abarbeiten.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_STAMM, SQL_CREATE_STAMM);
                yield return new KeyValuePair<string, string>(TAB_PROJEKT, SQL_CREATE_PROJEKT);
            }
        }

        /// <summary>
        /// Die FACHSPALTEN beider Tabellen in Schemareihenfolge — ohne <c>ID</c>,
        /// <c>ID_Projekt</c>, <c>Bezeichner</c> und <c>ReadOnly</c>. Die EINE Liste, an der
        /// <c>BaustoffCtrl.CopyFromStamm</c>, die Schreibwege und der Nachweis hängen.
        /// </summary>
        public static readonly IReadOnlyList<string> Fachspalten = new[]
        {
            SPALTE_GRUPPE, SPALTE_HERSTELLER, SPALTE_LAMBDA, SPALTE_RHO, SPALTE_CP,
            SPALTE_QUELLE, SPALTE_HERKUNFT, SPALTE_QUELLKENNUNG
        };

        // =================================================================
        //  Die Saat (BaustoffSaat.cs)
        // =================================================================

        /// <summary>
        /// <b>Die ganze Auslieferungssaat</b> — die 65 Normzeilen (Ids 1 bis 65) und die 67
        /// Herstellerzeilen (Ids 1001 bis 1067) aus <see cref="BaustoffSaattabelle"/>, in dieser
        /// Reihenfolge. <see cref="SaatSchreiben"/> schreibt sie in EINEM Zug.
        /// </summary>
        public static readonly IReadOnlyList<BaustoffSaat> Saat =
            BaustoffSaattabelle.Norm.Concat(BaustoffSaattabelle.Hersteller).ToArray();

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
            {
                return TabellenAngelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" +
                       TAB_STAMM + ", " + TAB_PROJEKT + "), " + Gesaet.ToString(CultureInfo.InvariantCulture) +
                       " von " + Saat.Count.ToString(CultureInfo.InvariantCulture) +
                       " Saatzeile(n) geschrieben (ReadOnly = 1, Herkunft " + DbWerte.HERKUNFT_VORGABE + ")";
            }
        }

        /// <summary>Stehen beide Tabellen?</summary>
        public static bool TabellenVorhanden()
            => DataRepository.TabelleVorhanden(TAB_STAMM) && DataRepository.TabelleVorhanden(TAB_PROJEKT);

        /// <summary>
        /// Steht der Zielstand — beide Tabellen da, jede Saatzeile unter ihrer festen Id, die
        /// Folge über der Saatgrenze?
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!TabellenVorhanden()) return false;
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + TAB_STAMM + "\" WHERE \"ID\" BETWEEN 1 AND ? AND \"ReadOnly\" = 1",
                new DbParam("@max", SAAT_ID_GRENZE - 1));
            if (n == null || n == DBNull.Value || Convert.ToInt64(n, CultureInfo.InvariantCulture) < Saat.Count)
                return false;
            object seq = DataRepository.ExecuteScalar(
                "SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", TAB_STAMM));
            return seq != null && seq != DBNull.Value &&
                   Convert.ToInt64(seq, CultureInfo.InvariantCulture) >= SAAT_ID_GRENZE - 1;
        }

        // =================================================================
        //  Ausführen
        // =================================================================

        /// <summary>
        /// Legt die Tabellen an (wenn sie fehlen) und schreibt die Saat — für
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration der
        /// Schale geht denselben Weg über ihre eigenen Helfer. <b>Wiederholbar.</b> Fehler
        /// werfen — der Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            foreach (KeyValuePair<string, string> a in Anweisungen)
            {
                bool vorher = DataRepository.TabelleVorhanden(a.Key);
                DataRepository.ExecuteNonQuery(a.Value);
                if (!vorher && DataRepository.TabelleVorhanden(a.Key)) b.TabellenAngelegt++;
            }
            b.Gesaet = SaatSchreiben();
            return b;
        }

        /// <summary>
        /// Schreibt die fehlenden Saatzeilen und hebt danach die AUTOINCREMENT-Folge des
        /// Katalogs auf die Saatgrenze. <b>Wiederholbar und nie überschreibend:</b> Eine Zeile,
        /// deren Id schon belegt ist ODER deren Bezeichner schon beim selben Hersteller (bzw.
        /// herstellerneutral) dasteht, wird übergangen — ein zweiter Lauf ändert nichts, und eine vom Anwender
        /// geänderte Saatzeile bleibt, wie sie ist.
        /// </summary>
        /// <returns>Zahl der angelegten Zeilen.</returns>
        public static int SaatSchreiben()
        {
            int angelegt = 0;
            foreach (BaustoffSaat s in Saat)
            {
                if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_STAMM + "\" WHERE \"ID\" = ?",
                           new DbParam("@id", s.Id)) > 0) continue;
                if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_STAMM + "\" WHERE \"Bezeichner\" = ? AND \"Hersteller\" IS ?",
                           new DbParam("@bez", s.Bezeichner),
                           new DbParam("@her", DbParamTyp.VarWChar) { Wert = (object)s.Hersteller ?? DBNull.Value }) > 0) continue;

                int n = DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"" + TAB_STAMM + "\" (\"ID\", \"Bezeichner\", \"Gruppe\", \"Hersteller\", " +
                    "\"Lambda\", \"Rho\", \"cp\", \"Quelle\", \"Herkunft\", \"Quellkennung\", \"ReadOnly\") " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 1)",
                    new DbParam("@id", s.Id),
                    new DbParam("@bez", s.Bezeichner),
                    new DbParam("@gr", s.Gruppe),
                    new DbParam("@her", DbParamTyp.VarWChar) { Wert = (object)s.Hersteller ?? DBNull.Value },
                    new DbParam("@l", DbParamTyp.Double) { Wert = s.Lambda },
                    new DbParam("@r", DbParamTyp.Double) { Wert = s.Rho },
                    new DbParam("@c", DbParamTyp.Double) { Wert = s.Cp },
                    new DbParam("@q", s.Quelle),
                    new DbParam("@h", DbWerte.HERKUNFT_VORGABE));
                if (n == 1) angelegt++;
            }

            FolgeReservieren();
            return angelegt;
        }

        /// <summary>
        /// Hebt die AUTOINCREMENT-Folge des Katalogs auf <c>SAAT_ID_GRENZE − 1</c> — nie nach
        /// unten. Nach dem ersten <c>INSERT</c> steht die Zeile in <c>sqlite_sequence</c>
        /// bereits; das zweite Statement legt sie nur für einen Katalog an, in den noch nie
        /// geschrieben wurde.
        /// </summary>
        private static void FolgeReservieren()
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE sqlite_sequence SET seq = ? WHERE name = ? AND seq < ?",
                new DbParam("@s", SAAT_ID_GRENZE - 1),
                new DbParam("@t", TAB_STAMM),
                new DbParam("@g", SAAT_ID_GRENZE - 1));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO sqlite_sequence (name, seq) SELECT ?, ? " +
                "WHERE NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name = ?)",
                new DbParam("@t", TAB_STAMM),
                new DbParam("@s", SAAT_ID_GRENZE - 1),
                new DbParam("@t2", TAB_STAMM));
        }

        /// <summary>Die Saatzeile zu einer Id; <c>null</c>, wenn die Id keine Saatzeile ist.</summary>
        public static BaustoffSaat SaatZu(int id)
        {
            foreach (BaustoffSaat s in Saat) if (s.Id == id) return s;
            return null;
        }

        private static long Anzahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
