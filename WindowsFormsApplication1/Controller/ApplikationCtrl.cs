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

            // Da es nur einen Datensatz gibt, brauchen wir kein WHERE oder Parameter
            string sql = "SELECT TOP 1 * FROM Tab_Applikation";
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

            OleDbParameter[] parameters = {
                new OleDbParameter("@pname", m_szProjektname ?? ""),
                new OleDbParameter("@pID", m_ID_Projekt),
                new OleDbParameter("@desc", m_szBeschreibung ?? ""),
                new OleDbParameter("@icon", m_icon ?? "")
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
        // Bewusst mit eigener, stiller OleDb-Verbindung: DataRepository zeigt bei
        // Fehlern MessageBoxen - beim Programmstart vor dem ersten Fenster ist das
        // nicht hinnehmbar.
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
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    DataTable dt = new DataTable();
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 * FROM Tab_Applikation", conn))
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (!dt.Columns.Contains(SPALTE_SCHEMAVERSION)) return 0; // Spalte fehlt -> Version 0
                    if (dt.Rows.Count == 0) return 0;                          // Zeile fehlt  -> Version 0

                    object v = dt.Rows[0][SPALTE_SCHEMAVERSION];
                    if (v == null || v == DBNull.Value) return 0;
                    return Convert.ToInt32(v);
                }
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
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(
                        "UPDATE Tab_Applikation SET [" + SPALTE_SCHEMAVERSION + "] = ?", conn))
                    {
                        cmd.Parameters.Add(new OleDbParameter("@v", version));
                        return cmd.ExecuteNonQuery() > 0;
                    }
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