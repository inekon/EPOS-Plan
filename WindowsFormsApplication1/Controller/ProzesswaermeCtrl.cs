using System;
using System.Collections.Generic;
using System.Data;

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

            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                item.m_szProzessname = row["Bezeichner"].ToString();

            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                item.m_szTyp = row["Typ"].ToString();

            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                item.m_szBeschreibung = row["Beschreibung"].ToString();

            if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

            if (dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                item.m_bReadOnly = Convert.ToBoolean(row["ReadOnly"]);

            // Monatswerte anhand der Spaltennamen (robust gegen zusaetzliche Spalten wie ID_Projekt)
            for (int i = 0; i < 12; i++)
            {
                string col = "Monat_" + (i + 1);
                if (dt.Columns.Contains(col) && row[col] != DBNull.Value)
                    item.m_Monat[i] = Convert.ToDouble(row[col]);
            }
        }

        /// <summary>
        /// Hilfsmethode, um die Eigenschaften dieser Controller-Instanz selbst zu befüllen (für ReadSingle)
        /// </summary>
        private void MapDataRowToInstance(DataRow row, DataTable dt)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                m_ID = Convert.ToInt32(row["ID"]);

            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                m_szProzessname = row["Bezeichner"].ToString();

            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                m_szTyp = row["Typ"].ToString();

            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                m_szBeschreibung = row["Beschreibung"].ToString();

            if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

            if (dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                m_bReadOnly = Convert.ToBoolean(row["ReadOnly"]);

            for (int i = 0; i < 12; i++)
            {
                string col = "Monat_" + (i + 1);
                if (dt.Columns.Contains(col) && row[col] != DBNull.Value)
                    m_Monat[i] = Convert.ToDouble(row[col]);
            }
        }

        private void ClearInstanceData()
        {
            m_ID = 0;
            m_szProzessname = string.Empty;
            m_szTyp = string.Empty;
            m_szBeschreibung = string.Empty;
            for (int i = 0; i < 12; i++) m_Monat[i] = 0.0;
            m_ID_Projekt = 0;
            m_bReadOnly = false;
        }

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Prozesswaerme ORDER BY Bezeichner";
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

            DbParam paramId = new DbParam("@id", DbParamTyp.Integer);
            paramId.Wert = ID_Prozesswaerme;

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
            string sql = "SELECT * FROM Tab_Prozesswaerme WHERE Bezeichner = ?";

            DbParam paramName = new DbParam("@name", DbParamTyp.VarWChar);
            paramName.Wert = szProzessname ?? (object)DBNull.Value;

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