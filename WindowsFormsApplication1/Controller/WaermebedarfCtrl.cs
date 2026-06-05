using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class WaermebedarfCtrl : WaermebedarfModel
    {
        private List<WaermebedarfModel> _internalList = new List<WaermebedarfModel>();
        public int rows => _internalList.Count;
        public new List<WaermebedarfModel> items => _internalList;

        public WaermebedarfCtrl()
        {
        }

        public bool Delete(string szName)
        {
            try
            {
                // Parametrisierte Abfrage ohne unsaubere Stringverkettungen
                string sql = "DELETE FROM Tab_Waermebedarf WHERE Bezeichner = ?";
                OleDbParameter[] ps = {
                    new OleDbParameter("@bez", szName ?? (object)DBNull.Value)
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        public bool Insert()
        {
            try
            {
                // Ermittlung der nächsten ID direkt über das Repository
                string sqlCount = "SELECT COUNT(*) FROM Tab_Waermebedarf";
                object countResult = DataRepository.ExecuteScalar(sqlCount, null);
                int count = countResult != null ? Convert.ToInt32(countResult) : 0;

                if (count == 0)
                {
                    m_ID_Ganglinie = 1;
                }
                else
                {
                    // Korrektur: Nutzt einheitlich den Spaltennamen aus der DB (hier ID_GanglinieDaten gemäß Altanwendung)
                    string sqlMax = "SELECT MAX(ID_GanglinieDaten) FROM Tab_Waermebedarf";
                    object maxResult = DataRepository.ExecuteScalar(sqlMax, null);
                    m_ID_Ganglinie = (maxResult != null ? Convert.ToInt32(maxResult) : 0) + 1;
                }

                // Standardkonformes INSERT INTO ... VALUES-Statement
                string sql = "INSERT INTO Tab_Waermebedarf (ID_GanglinieDaten, Bezeichner) VALUES (?, ?)";
                OleDbParameter[] ps = {
                    new OleDbParameter("@id", m_ID_Ganglinie),
                    new OleDbParameter("@bez", m_szBezeichner ?? (object)DBNull.Value)
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Waermebedarf ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WaermebedarfModel item = new WaermebedarfModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("ID_GanglinieDaten") && row["ID_GanglinieDaten"] != DBNull.Value)
                    item.m_ID_Ganglinie = Convert.ToInt32(row["ID_GanglinieDaten"]);
                else if (dt.Columns.Contains("ID_Ganglinie") && row["ID_Ganglinie"] != DBNull.Value)
                    item.m_ID_Ganglinie = Convert.ToInt32(row["ID_Ganglinie"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_Waermebedarf WHERE Bezeichner = ?";
            OleDbParameter[] ps = {
                new OleDbParameter("@bez", szBezeichner ?? (object)DBNull.Value)
            };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Löscht Instanzdaten standardmäßig für den Fall, dass nichts gefunden wird
            ID = 0;
            m_ID_Ganglinie = 0;
            m_szBezeichner = string.Empty;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("ID_GanglinieDaten") && row["ID_GanglinieDaten"] != DBNull.Value)
                    m_ID_Ganglinie = Convert.ToInt32(row["ID_GanglinieDaten"]);
                else if (dt.Columns.Contains("ID_Ganglinie") && row["ID_Ganglinie"] != DBNull.Value)
                    m_ID_Ganglinie = Convert.ToInt32(row["ID_Ganglinie"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();
            }
        }
    }
}