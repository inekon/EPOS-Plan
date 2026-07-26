using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class Z_ProjGebCtrl : Z_ProjGebModel
    {
        // Das neue dynamische Listen-Schema
        private List<Z_ProjGebModel> _internalList = new List<Z_ProjGebModel>();

        public int rows => _internalList.Count;
        public new List<Z_ProjGebModel> items => _internalList;

        private Z_ProjGebModel model;

        public Z_ProjGebCtrl()
        {
            model = new Z_ProjGebModel();
        }

        public void ReadAll(string sql)
        {
            // Abfrage über das zentrale DataRepository holen
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Liste vor dem Befüllen leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjGebModel item = new Z_ProjGebModel();

                // Sicheres Mapping über die Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Gebaeude") && row["ID_Gebaeude"] != DBNull.Value)
                    item.ID_Gebaeude = Convert.ToInt32(row["ID_Gebaeude"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("Wohnflaeche") && row["Wohnflaeche"] != DBNull.Value)
                    item.Wohnflaeche = Convert.ToDouble(row["Wohnflaeche"]);

                if (dt.Columns.Contains("Einheit") && row["Einheit"] != DBNull.Value)
                    item.Einheit = row["Einheit"].ToString();

                if (dt.Columns.Contains("Jahresnutzungsgrad") && row["Jahresnutzungsgrad"] != DBNull.Value)
                    item.Jahresnutzungsgrad = Convert.ToDouble(row["Jahresnutzungsgrad"]);

                if (dt.Columns.Contains("DezentralWarmwasser") && row["DezentralWarmwasser"] != DBNull.Value)
                    item.DezentralWarmwasser = Convert.ToBoolean(row["DezentralWarmwasser"]);

                // Element dynamisch zur internen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}
