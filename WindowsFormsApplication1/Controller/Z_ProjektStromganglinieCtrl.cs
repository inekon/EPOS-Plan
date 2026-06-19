using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektStromganglinieCtrl : Z_ProjektStromganglinieModel
    {
        private List<Z_ProjektStromganglinieModel> _internalList = new List<Z_ProjektStromganglinieModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektStromganglinieModel> items => _internalList;
        public Z_ProjektStromganglinieModel model;

        public Z_ProjektStromganglinieCtrl()
        {
            model = new Z_ProjektStromganglinieModel();
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
                Z_ProjektStromganglinieModel item = new Z_ProjektStromganglinieModel();

                // Sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Ganglinie") && row["ID_Ganglinie"] != DBNull.Value)
                    item.m_ID_Stromganglinie = Convert.ToInt32(row["ID_Ganglinie"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szStromganglinie = row["Bezeichner"].ToString();

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}