using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class ApplikationCtrl : ApplikationModel
    {
        private List<ApplikationModel> _internalList = new List<ApplikationModel>();
        private bool _hasData = false;

        // Kompatibilitätsschicht
        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasData ? 1 : 0);
        public List<ApplikationModel> items => _internalList;

        public ApplikationCtrl() { }

        /// <summary>
        /// Liest den einzigen existierenden Datensatz aus der Tabelle.
        /// </summary>
        public void ReadSingle()
        {
            _internalList.Clear();
            _hasData = false;

            // Da es nur einen Datensatz gibt, brauchen wir kein WHERE oder Parameter.
            // TOP 1 -> LIMIT 1 (ARBEITSPAKET S5 hier VORGEZOGEN): Diese Methode
            // laeuft beim Programmstart ueber DataRepository; SQLite kennt TOP nicht,
            // sie waere sonst der erste Laufzeitbruch ueberhaupt. Gleiche Bauform wie
            // GetSchemaVersion weiter unten.
            string sql = "SELECT * FROM Tab_Applikation LIMIT 1";
            DataTable dt = DataRepository.GetDataTable(sql);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                // Controller-Felder füllen (this)
                FillModelFromRow(this, row);

                // Liste füllen (items[0])
                ApplikationModel m = new ApplikationModel();
                FillModelFromRow(m, row);
                _internalList.Add(m);

                _hasData = true;
            }
        }

        public bool Update()
        {
            // Update erfolgt immer auf ID=1, da es nur diesen einen Datensatz gibt
            string sql = @"UPDATE Tab_Applikation SET 
                            Projektname = ?, 
                            ID_Projekt = ?, 
                            Beschreibung = ?, 
                            Icon = ? 
                           WHERE ID = 1;";

            DbParam[] parameters = {
                new DbParam("@pname", m_szProjektname ?? ""),
                new DbParam("@pID", m_ID_Projekt),
                new DbParam("@desc", m_szBeschreibung ?? ""),
                new DbParam("@icon", m_icon ?? "")
            };

            return DataRepository.ExecuteSQL(sql, parameters);
        }

        // =========================================================================
        // Schemamarker (ADR-001, Aufgabe 2)
        //
        // Tab_Applikation ist die anwendungsweite Einzelzeilen-Statustabelle und damit
        // der natuerliche Ort fuer den Schemastand. Die Spalte selbst legt die
        // SchemaMigration als Bootstrap an - die beiden Methoden hier sind bewusst
        // TOLERANT: fehlt die Spalte (oder die Zeile, oder die Tabelle), gilt
        // Version 0, ohne Dialog und ohne Ausnahme.
        //
        // Bewusst STILL: DataRepository zeigt bei Fehlern MessageBoxen - beim
        // Programmstart vor dem ersten Fenster ist das nicht hinnehmbar. Der Weg
        // dorthin ist seit ARBEITSPAKET S4b nicht mehr eine eigene OleDb-Verbindung,
        // sondern StilleDb (dieselbe Zusage, aber auf der Zugriffsschicht).
        //
        // VORMERKUNG S6/S8 - ERSTMIGRATIONS-HEBUNG: Die beiden Methoden hier lesen und
        // schreiben ab sofort den Schemastand der SQLITE-Datei. Fuer die einmalige
        // Hebung eines Altbestands ("Kenndaten.accdb" ist da, die .sqlite noch nicht)
        // braucht es einen EIGENEN, ausdruecklich benannten OleDb-Leser auf die
        // Alt-.accdb - er gehoert zur Hebung (S6/S8), nicht hierher. Diese Methoden
        // duerfen dafuer NICHT umgebogen werden: Sie beantworten die Frage "welchen
        // Stand hat die Datenbank, mit der das Programm gerade arbeitet".
        //
        // EINGELOEST MIT ARBEITSPAKET S6: Der angekuendigte OleDb-Leser (und der dazu
        // gehoerende Schreiber) stehen weiter unten als GetSchemaVersionOleDb /
        // SetSchemaVersionOleDb. Sie bekommen die Verbindung HEREINGEREICHT und ziehen
        // sich ausdruecklich KEINEN Verbindungsstring aus DataRepository - der liefert
        // seit S4a den SQLite-String. Benutzt werden sie ausschliesslich vom
        // eingefrorenen Access-Zweig SchemaMigration.HebeAltbestand.
        // =========================================================================

        /// <summary>Name der Markerspalte in Tab_Applikation.</summary>
        public const string SPALTE_SCHEMAVERSION = "SchemaVersion";

        /// <summary>
        /// Liefert den gespeicherten Schemastand. 0 bedeutet "noch nichts migriert" -
        /// das ist auch die Antwort, wenn Spalte, Zeile oder Tabelle fehlen.
        /// </summary>
        public static int GetSchemaVersion()
        {
            try
            {
                // TOP 1 -> LIMIT 1 (S5 vorgezogen, weil diese Abfrage hier
                // ohnehin umgebaut wird).
                DataTable dt = StilleDb.Tabelle("SELECT * FROM Tab_Applikation LIMIT 1");

                if (dt == null) return 0;                                  // Tabelle fehlt -> Version 0
                if (!dt.Columns.Contains(SPALTE_SCHEMAVERSION)) return 0;  // Spalte fehlt  -> Version 0
                if (dt.Rows.Count == 0) return 0;                          // Zeile fehlt   -> Version 0

                object v = dt.Rows[0][SPALTE_SCHEMAVERSION];
                if (v == null || v == DBNull.Value) return 0;
                return Convert.ToInt32(v);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Schreibt den Schemastand. Rueckgabe false, wenn nichts geschrieben werden
        /// konnte (fehlende Spalte, leere Tabelle, schreibgeschuetzte Datenbank) - die
        /// SchemaMigration wertet das als Fehlschlag des Schritts.
        /// </summary>
        public static bool SetSchemaVersion(int version)
        {
            try
            {
                return StilleDb.NonQuery(
                    "UPDATE Tab_Applikation SET [" + SPALTE_SCHEMAVERSION + "] = ?",
                    new DbParam("@v", version)) > 0;
            }
            catch
            {
                return false;
            }
        }

        // =========================================================================
        // Schemamarker im ALTBESTAND (ARBEITSPAKET S6, eingefrorener Access-Zweig)
        //
        // Formgleich mit den beiden Methoden darueber - nur eben ueber OleDb auf einer
        // HEREINGEREICHTEN Verbindung. Drei Unterschiede, alle drei Absicht:
        //
        //   1. KEIN DataRepository. Weder Verbindungsstring noch Zugriffsmethode: Beides
        //      zeigt seit S4a auf die SQLite-Datei. Die Verbindung kommt vom Aufrufer
        //      (SchemaMigration.HebeAltbestand baut sie aus einem ausdruecklichen
        //      ACE-Verbindungsstring auf den .accdb-Pfad).
        //   2. SELECT TOP 1 statt LIMIT 1. Die Zugriffsschicht ist auf SQLite umgestellt,
        //      der Altbestand aber nicht: ACE kennt kein LIMIT. Der Dialektwechsel aus S5
        //      gilt hier ausdruecklich NICHT.
        //   3. Ebenso TOLERANT wie die SQLite-Fassung: fehlende Spalte, fehlende Zeile,
        //      fehlende Tabelle -> Version 0, ohne Dialog und ohne Ausnahme. Genau das
        //      braucht der Bootstrap der Migration, der die Markerspalte erst anlegt.
        // =========================================================================

        /// <summary>
        /// Schemastand des ALTBESTANDS ueber die hereingereichte OleDb-Verbindung.
        /// 0 bedeutet "noch nichts migriert" - auch dann, wenn Spalte, Zeile oder
        /// Tabelle fehlen.
        /// </summary>
        internal static int GetSchemaVersionOleDb(OleDbConnection verbindung)
        {
            if (verbindung == null) return 0;
            try
            {
                DataTable dt = new DataTable();
                using (OleDbCommand cmd =
                           new OleDbCommand("SELECT TOP 1 * FROM Tab_Applikation", verbindung))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }

                if (!dt.Columns.Contains(SPALTE_SCHEMAVERSION)) return 0;  // Spalte fehlt -> Version 0
                if (dt.Rows.Count == 0) return 0;                          // Zeile fehlt  -> Version 0

                object v = dt.Rows[0][SPALTE_SCHEMAVERSION];
                if (v == null || v == DBNull.Value) return 0;
                return Convert.ToInt32(v);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Schreibt den Schemastand in den ALTBESTAND. Rueckgabe false, wenn nichts
        /// geschrieben werden konnte - die Migration wertet das als Fehlschlag des
        /// Schritts (gleiche Zusage wie <see cref="SetSchemaVersion"/>).
        /// </summary>
        internal static bool SetSchemaVersionOleDb(OleDbConnection verbindung, int version)
        {
            if (verbindung == null) return false;
            try
            {
                using (OleDbCommand cmd = new OleDbCommand(
                           "UPDATE Tab_Applikation SET [" + SPALTE_SCHEMAVERSION + "] = ?", verbindung))
                {
                    // iU6: KEIN DbParam - hier wird eine echte OleDbCommand-Sammlung
                    // auf einer Access-Verbindung gefuellt (Altbestand-Zweig).
                    cmd.Parameters.Add(new OleDbParameter("@v", version));
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void FillModelFromRow(ApplikationModel target, DataRow row)
        {
            target.m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            target.m_szProjektname = row["Projektname"]?.ToString() ?? "";
            target.m_ID_Projekt = row["ID_Projekt"] != DBNull.Value ? Convert.ToInt32(row["ID_Projekt"]) : 0;
            target.m_szBeschreibung = row["Beschreibung"]?.ToString() ?? "";
            target.m_icon = row["Icon"]?.ToString() ?? "";
        }
    }
}