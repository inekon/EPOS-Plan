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