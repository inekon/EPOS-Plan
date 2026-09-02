using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektSolarganglinieCtrl : Z_ProjektSolarganglinieModel
    {
        private List<Z_ProjektSolarganglinieModel> _internalList = new List<Z_ProjektSolarganglinieModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektSolarganglinieModel> items => _internalList;

        public Z_ProjektSolarganglinieCtrl()
        {
        }

        public void ReadAll(string sql)
        {
            // Abfrage über das zentrale DataRepository laden
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem erneuten Befüllen leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjektSolarganglinieModel item = new Z_ProjektSolarganglinieModel();

                // Sicheres Auslesen über Spaltennamen statt fehleranfälliger numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Solarganglinie") && row["ID_Solarganglinie"] != DBNull.Value)
                    item.m_ID_Solarganglinie = Convert.ToInt32(row["ID_Solarganglinie"]);

                if (dt.Columns.Contains("Solarganglinie") && row["Solarganglinie"] != DBNull.Value)
                    item.m_szSolarganglinie = row["Solarganglinie"].ToString();

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}