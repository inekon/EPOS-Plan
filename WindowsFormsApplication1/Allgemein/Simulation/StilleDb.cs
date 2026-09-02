using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialogfreier Datenbankzugriff für die Bausteine aus Paket 2 (Konfigurations-UI).
    ///
    /// <see cref="DataRepository"/> zeigt im Fehlerfall eine MessageBox und liefert bei
    /// Fehlern leere Ergebnisse statt <c>null</c>. Für alles, was auch aus dem Engine-Pfad
    /// oder aus einem headless laufenden Prüfprogramm heraus aufgerufen werden kann,
    /// verlangt Konzept 13.4 Dialogfreiheit — ein hängender Referenzlauf ist sonst die
    /// Folge.
    ///
    /// Bestehende Klassen bringen denselben Vorlauf jeweils privat mit
    /// (<c>PufferSpCtrl.StillScalar</c>, <c>WaermequelleClass.SkalarStill</c>). Diese
    /// Klasse ist die gemeinsame Fassung für den NEUEN Code.
    ///
    /// STAND NACH ARBEITSPAKET S4b: Die privaten Klone laufen NICHT hierher zusammen —
    /// jeder von ihnen führt einen eigenen Meldungstext, und <c>StillScalar</c> reicht
    /// zusätzlich <see cref="DBNull"/> durch, wo diese Klasse <c>null</c> liefert. Sie
    /// wurden deshalb INNEN auf dieselbe Zugriffsschicht umgestellt und teilen sich mit
    /// dieser Klasse den Verbindungsaufbau (<see cref="OeffneVerbindung"/>) sowie die
    /// Übersetzung in <c>DataRepository.ErzeugeKommando</c>. Eine zweite Fassung der
    /// Übersetzung gibt es nirgends.
    ///
    /// Alle Methoden schlucken Fehler und melden sie nur auf die Konsole.
    ///
    /// PROTOKOLLKANAL-NACHZUG, KATEGORIE (c): Das bleibt so. Die drei Meldungen sind
    /// generische Zugriffsdiagnosen ohne Anwenderaussage („StilleDb.Tabelle
    /// fehlgeschlagen: …"); die fachliche Folge meldet jeweils der AUFRUFER über
    /// <see cref="SimulationProtokoll"/> (z. B. „die Stufe rechnet ohne Module").
    /// Hinzu kommt, dass dieselben Methoden auch aus der Konfigurations-Oberfläche
    /// heraus laufen, also außerhalb jedes Simulationslaufs.
    /// </summary>
    internal static class StilleDb
    {
        // =================================================================================
        // ARBEITSPAKET S4b: Verbindungsaufbau (Konzept 2.1)
        // =================================================================================

        /// <summary>
        /// Oeffnet eine SQLite-Verbindung mit den verbindungsgebundenen PRAGMAs.
        ///
        /// ARBEITSPAKET S4c/S4d - VORMERKUNG EINGELOEST. S4b musste den Verbindungsaufbau
        /// hier noch einmal hinschreiben, weil <c>DataRepository.OeffneVerbindung</c>
        /// privat war; die Vormerkung an dieser Stelle lautete, ihn zusammenzufuehren,
        /// sobald die Zugriffsschicht wieder angefasst wird. Das ist jetzt geschehen:
        /// Die Methode DELEGIERT nur noch. Es gibt genau EINEN Verbindungsaufbau
        /// (Verbindungsstring und PRAGMAs stehen ausschliesslich in
        /// <c>DataRepository.OeffneVerbindung</c>).
        ///
        /// Die Methode selbst BLEIBT stehen: Sie ist der stille Einstieg fuer
        /// <c>RecordSet</c>, <c>GeraeteWaisen</c>, <c>PufferSpCtrl</c>
        /// (StillScalar/StillNonQuery/StillProbe/BetroffeneIds/TemperaturenLesen) und
        /// <c>WaermequelleClass</c> (WertLesenStill/SkalarStill/TabelleStill) - 12
        /// Aufrufstellen, die nichts von <c>DataRepository</c> wissen muessen.
        /// </summary>
        internal static SqliteConnection OeffneVerbindung()
        {
            return DataRepository.OeffneVerbindung();
        }

        /// <summary>Skalare Abfrage; <c>null</c> bei Fehler, fehlender Zeile oder NULL.</summary>
        public static object Scalar(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                {
                    object v = cmd.ExecuteScalar();
                    return (v == DBNull.Value) ? null : v;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("StilleDb.Scalar fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Tabellenabfrage; <c>null</c> bei Fehler (z. B. fehlende Spalte).</summary>
        public static DataTable Tabelle(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                using (SqliteDataReader leser = cmd.ExecuteReader())
                {
                    // Derselbe Typ-Rueckweg (D9) wie in DataRepository.GetDataTable -
                    // es gibt keine zweite Uebersetzungsfassung.
                    return DataRepository.LadeTabelle(leser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("StilleDb.Tabelle fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Schreibende Anweisung; Anzahl betroffener Zeilen, -1 bei Fehler.</summary>
        public static int NonQuery(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("StilleDb.NonQuery fehlgeschlagen: " + ex.Message);
                return -1;
            }
        }

        /// <summary>Ganzzahl aus einem Datenbankwert; <paramref name="vorgabe"/> bei NULL/Unfug.</summary>
        public static int Zahl(object o, int vorgabe = 0)
        {
            if (o == null || o == DBNull.Value) return vorgabe;
            try { return Convert.ToInt32(o); }
            catch { return vorgabe; }
        }

        /// <summary>Kommazahl aus einem Datenbankwert; <paramref name="vorgabe"/> bei NULL/Unfug.</summary>
        public static double Kommazahl(object o, double vorgabe = 0)
        {
            if (o == null || o == DBNull.Value) return vorgabe;
            try { return Convert.ToDouble(o); }
            catch { return vorgabe; }
        }

        /// <summary>Text aus einem Datenbankwert; "" bei NULL.</summary>
        public static string Text(object o)
        {
            if (o == null || o == DBNull.Value) return "";
            return o.ToString();
        }

        /// <summary>Feldwert einer DataRow oder <c>null</c>, wenn die Spalte fehlt.</summary>
        public static object Feld(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return null;
            return r[spalte] == DBNull.Value ? null : r[spalte];
        }

        /// <summary>
        /// Parameter mit ausdrücklichem Typ — nötig überall dort, wo der Wert
        /// <see cref="DBNull"/> sein kann (aus DBNull allein leitet der OLE-DB-Provider
        /// keinen Spaltentyp ab). Gleiche Bauart wie <c>ProjektPuffer.Par</c>.
        /// </summary>
        public static OleDbParameter Par(string name, OleDbType typ, object wert)
        {
            return new OleDbParameter(name, typ) { Value = wert ?? DBNull.Value };
        }


        // =================================================================================
        // ARBEITSPAKET S4b: stille Schema-Auskunft (Konzept 2.7, S4c vorgezogen)
        // =================================================================================
        //
        // WARUM NICHT DataRepository.SpaltenVonTabelle/TabelleVorhanden. Die dortigen
        // Auskuenfte laufen ueber GetDataTable/ExecuteScalar und melden einen Fehler als
        // MessageBox. Genau das verbieten sich die Selbstheilungswege im Bestand
        // ausdruecklich ("Eine Vorsorge ist kein Bedienschritt"): Sie hielten deshalb eine
        // EIGENE OleDbConnection und lasen ihr Schema ueber GetOleDbSchemaTable in einem
        // try/catch. Diese beiden Auskuenfte treten an genau diese Stelle - gleiche
        // Rueckgabe, gleiche Stille.
        //
        // Der Tabellenname geht als PARAMETER an die table-valued Form des PRAGMA
        // (pragma_table_info(?)), nicht in zusammengesetztes SQL - wie in DataRepository.

        /// <summary>
        /// Spaltennamen einer Tabelle (Vergleich ohne Gross-/Kleinschreibung), oder
        /// <c>null</c>, wenn es die Tabelle nicht gibt bzw. das Schema nicht lesbar ist.
        /// Tritt an die Stelle der bisherigen privaten <c>SpaltenNamen(conn, tabelle)</c>-
        /// Helfer ueber <c>GetOleDbSchemaTable(Columns, …)</c>.
        /// </summary>
        public static HashSet<string> SpaltenNamen(string tabelle)
        {
            DataTable cols = Tabelle("SELECT name FROM pragma_table_info(?) ORDER BY cid",
                                     new OleDbParameter("?", tabelle ?? string.Empty));

            // Wie bisher: keine Zeilen = keine Tabelle (eine Tabelle ohne Spalten gibt es
            // nicht). null = Schema nicht lesbar. Beides ergibt null.
            if (cols == null || cols.Rows.Count == 0) return null;

            HashSet<string> namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in cols.Rows) namen.Add(Convert.ToString(r["name"]));
            return namen;
        }

        /// <summary>
        /// Gibt es eine Tabelle (oder Sicht) dieses Namens? Dialogfreier Ersatz fuer
        /// <c>GetOleDbSchemaTable(Tables, …)</c> mit anschliessender Zeilenzahlpruefung.
        /// </summary>
        public static bool TabelleVorhanden(string tabelle)
        {
            object treffer = Scalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('table','view') AND name = ?",
                new OleDbParameter("?", tabelle ?? string.Empty));
            return treffer != null && Convert.ToInt32(treffer) > 0;
        }


        // =================================================================================
        // ARBEITSPAKET S4b: Access-Typdefinition -> SQLite (S4d vorgezogen)
        // =================================================================================

        /// <summary>
        /// Uebersetzt eine Access-Typangabe aus <see cref="SchemaKatalog"/>
        /// (<c>LONG</c>, <c>DOUBLE</c>, <c>YESNO</c>, <c>TEXT(n)</c>, <c>DATETIME</c>, …)
        /// in eine SQLite-Spaltendefinition nach dem Muster von
        /// <c>sql\schema\001_grundschema.sql</c>.
        ///
        /// PFLICHT, NICHT KOSMETIK: Alle Tabellen des Zielschemas sind <c>STRICT</c>.
        /// Dort laesst <c>ALTER TABLE … ADD COLUMN</c> nur INT/INTEGER/REAL/TEXT/BLOB/ANY
        /// zu - ein durchgereichtes "TEXT(20)" oder "YESNO" wuerde abgewiesen.
        ///
        /// <see cref="SchemaKatalog"/> selbst bleibt unangetastet (eingefrorener
        /// Access-Zweig, Arbeitspaket S6) - uebersetzt wird beim Verbrauch.
        /// </summary>
        /// <param name="spalte">Spaltenname - fuer die Laengen- und 0/1-Pruefungen.</param>
        /// <param name="accessTyp">Typangabe des Katalogs.</param>
        public static string SqliteSpaltenTyp(string spalte, string accessTyp)
        {
            string t = (accessTyp ?? string.Empty).Trim().ToUpperInvariant();
            string q = "\"" + (spalte ?? string.Empty).Replace("\"", "\"\"") + "\"";

            if (t.StartsWith("YESNO", StringComparison.Ordinal) ||
                t.StartsWith("BIT", StringComparison.Ordinal) ||
                t.StartsWith("BOOLEAN", StringComparison.Ordinal))
            {
                // Wahrheitswert wie im Grundschema: INTEGER 0/1 mit Vorgabe 0. Die Vorgabe
                // ist bei ADD COLUMN Pflicht, sobald NOT NULL steht.
                return "INTEGER NOT NULL DEFAULT 0 CHECK (" + q + " IN (0,1))";
            }

            if (t.StartsWith("DOUBLE", StringComparison.Ordinal) ||
                t.StartsWith("SINGLE", StringComparison.Ordinal) ||
                t.StartsWith("FLOAT", StringComparison.Ordinal) ||
                t.StartsWith("REAL", StringComparison.Ordinal) ||
                t.StartsWith("CURRENCY", StringComparison.Ordinal) ||
                t.StartsWith("DECIMAL", StringComparison.Ordinal) ||
                t.StartsWith("NUMERIC", StringComparison.Ordinal))
            {
                return "REAL";
            }

            if ((t.StartsWith("LONG", StringComparison.Ordinal) &&
                 !t.StartsWith("LONGTEXT", StringComparison.Ordinal)) ||
                t.StartsWith("INTEGER", StringComparison.Ordinal) ||
                t.StartsWith("INT", StringComparison.Ordinal) ||
                t.StartsWith("BYTE", StringComparison.Ordinal) ||
                t.StartsWith("COUNTER", StringComparison.Ordinal) ||
                t.StartsWith("AUTOINCREMENT", StringComparison.Ordinal))
            {
                return "INTEGER";
            }

            // Datum/Zeit wird - wie vom Migrator geschrieben - als ISO-8601-TEXT gehalten.
            if (t.StartsWith("DATETIME", StringComparison.Ordinal) ||
                t.StartsWith("DATE", StringComparison.Ordinal) ||
                t.StartsWith("TIME", StringComparison.Ordinal))
            {
                return "TEXT";
            }

            // TEXT(n)/VARCHAR(n)/CHAR(n) -> TEXT mit Laengenpruefung wie im Grundschema.
            int auf = t.IndexOf('(');
            int zu = t.IndexOf(')');
            if (auf > 0 && zu > auf)
            {
                string zahl = t.Substring(auf + 1, zu - auf - 1).Trim();
                int laenge;
                if (int.TryParse(zahl, NumberStyles.Integer, CultureInfo.InvariantCulture, out laenge) && laenge > 0)
                    return "TEXT CHECK (length(" + q + ") <= " +
                           laenge.ToString(CultureInfo.InvariantCulture) + ")";
            }

            // MEMO/LONGTEXT/TEXT ohne Laenge und alles Unbekannte: TEXT ohne Pruefung.
            return "TEXT";
        }

        /// <summary>
        /// Vollstaendiges <c>ALTER TABLE … ADD COLUMN</c> fuer eine Katalogspalte -
        /// die eine Schreibweise fuer alle Selbstheilungswege des Bestands.
        /// </summary>
        public static string AlterTableAddColumn(string tabelle, string spalte, string accessTyp)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ALTER TABLE [").Append(tabelle).Append("] ADD COLUMN [")
              .Append(spalte).Append("] ").Append(SqliteSpaltenTyp(spalte, accessTyp));
            return sb.ToString();
        }
    }
}
