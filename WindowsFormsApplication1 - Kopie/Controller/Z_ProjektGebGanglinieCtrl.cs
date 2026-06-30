using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektGebGanglinieCtrl : Z_ProjWaermebedarfModel
    {
        private List<Z_ProjWaermebedarfModel> _internalList = new List<Z_ProjWaermebedarfModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjWaermebedarfModel> items => _internalList;

        public Z_ProjektGebGanglinieCtrl()
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
                Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                // Sicheres Auslesen über Spaltennamen statt fehleranfälliger numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Ganglinie") && row["ID_Ganglinie"] != DBNull.Value)
                    item.m_ID_Ganglinie = Convert.ToInt32(row["ID_Ganglinie"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                // Fallback, falls die Spalte in Access exakt wie die Variable heißt
                else if (dt.Columns.Contains("m_szBezeichner") && row["m_szBezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["m_szBezeichner"].ToString();

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}