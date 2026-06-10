using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class StromspeicherCtrl : StromspeicherModel
    {
        private List<StromspeicherModel> _internalList = new List<StromspeicherModel>();
        public int rows => _internalList.Count;
        public List<StromspeicherModel> items => _internalList;

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Stromspeicher ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                StromspeicherModel item = new StromspeicherModel();

                // Namensbasiertes und typsicheres Auslesen der Spalten
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    item.m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value)
                    item.m_Leistung = Convert.ToDouble(row["Leistung"]);

                if (dt.Columns.Contains("Energie") && row["Energie"] != DBNull.Value)
                    item.m_Energie = Convert.ToDouble(row["Energie"]);

                if (dt.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value)
                    item.m_Degradation = Convert.ToDouble(row["Degradation"]);

                if (dt.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value)
                    item.m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);

                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value)
                    item.m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                _internalList.Add(item);
            }
        }

        public void ReadSingle(int ID)
        {
            string sql = "SELECT * FROM Tab_Stromspeicher WHERE ID = ?";

            OleDbParameter paramId = new OleDbParameter("@id", OleDbType.Integer);
            paramId.Value = ID;
            OleDbParameter[] ps = { paramId };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten vorsorglich zurücksetzen
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szTyp = string.Empty;
            m_Leistung = 0;
            m_Energie = 0;
            m_Degradation = 0;
            m_Ladezustand = 0;
            m_Modulkosten = 0;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value)
                    m_Leistung = Convert.ToDouble(row["Leistung"]);

                if (dt.Columns.Contains("Energie") && row["Energie"] != DBNull.Value)
                    m_Energie = Convert.ToDouble(row["Energie"]);

                if (dt.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value)
                    m_Degradation = Convert.ToDouble(row["Degradation"]);

                if (dt.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value)
                    m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);

                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value)
                    m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                // UI-Kompatibilität wahren
                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_Stromspeicher WHERE Bezeichner = ?";

            OleDbParameter paramBez = new OleDbParameter("@bez", OleDbType.VarWChar);
            paramBez.Value = szBezeichner ?? (object)DBNull.Value;
            OleDbParameter[] ps = { paramBez };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten vorsorglich zurücksetzen
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szTyp = string.Empty;
            m_Leistung = 0;
            m_Energie = 0;
            m_Degradation = 0;
            m_Ladezustand = 0;
            m_Modulkosten = 0;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value)
                    m_Leistung = Convert.ToDouble(row["Leistung"]);

                if (dt.Columns.Contains("Energie") && row["Energie"] != DBNull.Value)
                    m_Energie = Convert.ToDouble(row["Energie"]);

                if (dt.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value)
                    m_Degradation = Convert.ToDouble(row["Degradation"]);

                if (dt.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value)
                    m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);

                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value)
                    m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                // UI-Kompatibilität wahren
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
