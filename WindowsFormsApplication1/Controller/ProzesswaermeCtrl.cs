using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class ProzesswaermeCtrl : ProzesswaermeModel
    {
        // Das dynamische Listen-Schema zur Aufhebung des 1000er-Limits
        private List<ProzesswaermeModel> _internalList = new List<ProzesswaermeModel>();
        public int rows => _internalList.Count;
        public new List<ProzesswaermeModel> items => _internalList;

        // Unused/Instanz-Model aus dem Altcode zur Kompatibilität beibehalten
        public ProzesswaermeModel model { get; set; }

        public ProzesswaermeCtrl()
        {
            model = new ProzesswaermeModel();
        }

        /// <summary>
        /// Hilfsmethode, um eine DataRow sicher in ein ProzesswaermeModel zu mappen (inkl. der 12 Monate)
        /// </summary>
        private void MapDataRowToModel(DataRow row, ProzesswaermeModel item, DataTable dt)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                item.m_ID = Convert.ToInt32(row["ID"]);

            if (dt.Columns.Contains("Prozessname") && row["Prozessname"] != DBNull.Value)
                item.m_szProzessname = row["Prozessname"].ToString();

            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                item.m_szTyp = row["Typ"].ToString();

            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                item.m_szBeschreibung = row["Beschreibung"].ToString();

            // Auslesen der 12 Monats-Werte (Spalten 4 bis 15 im Altcode)
            for (int i = 0; i < 12; i++)
            {
                int columnIndex = i + 4;
                if (dt.Columns.Count > columnIndex && row[columnIndex] != DBNull.Value)
                {
                    item.m_Monat[i] = Convert.ToDouble(row[columnIndex]);
                }
            }
        }

        /// <summary>
        /// Hilfsmethode, um die Eigenschaften dieser Controller-Instanz selbst zu befüllen (für ReadSingle)
        /// </summary>
        private void MapDataRowToInstance(DataRow row, DataTable dt)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                m_ID = Convert.ToInt32(row["ID"]);

            if (dt.Columns.Contains("Prozessname") && row["Prozessname"] != DBNull.Value)
                m_szProzessname = row["Prozessname"].ToString();

            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                m_szTyp = row["Typ"].ToString();

            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                m_szBeschreibung = row["Beschreibung"].ToString();

            for (int i = 0; i < 12; i++)
            {
                int columnIndex = i + 4;
                if (dt.Columns.Count > columnIndex && row[columnIndex] != DBNull.Value)
                {
                    m_Monat[i] = Convert.ToDouble(row[columnIndex]);
                }
            }
        }

        private void ClearInstanceData()
        {
            m_ID = 0;
            m_szProzessname = string.Empty;
            m_szTyp = string.Empty;
            m_szBeschreibung = string.Empty;
            for (int i = 0; i < 12; i++) m_Monat[i] = 0.0;
        }

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Prozesswaerme ORDER BY Prozessname";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                ProzesswaermeModel item = new ProzesswaermeModel();
                MapDataRowToModel(row, item, dt);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(int ID_Prozesswaerme)
        {
            string sql = "SELECT * FROM Tab_Prozesswaerme WHERE ID = ?";

            OleDbParameter paramId = new OleDbParameter("@id", OleDbType.Integer);
            paramId.Value = ID_Prozesswaerme;

            DataTable dt = DataRepository.GetDataTable(sql, new[] { paramId });

            ClearInstanceData();

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                MapDataRowToInstance(row, dt);

                // Listensynchronisation zur Erhaltung der UI-Kompatibilität (rows = 1)
                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }

        public void ReadSingle(string szProzessname)
        {
            string sql = "SELECT * FROM Tab_Prozesswaerme WHERE Prozessname = ?";

            OleDbParameter paramName = new OleDbParameter("@name", OleDbType.VarWChar);
            paramName.Value = szProzessname ?? (object)DBNull.Value;

            DataTable dt = DataRepository.GetDataTable(sql, new[] { paramName });

            ClearInstanceData();

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                MapDataRowToInstance(row, dt);

                // Listensynchronisation zur Erhaltung der UI-Kompatibilität (rows = 1)
                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }
    }
}