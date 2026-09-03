using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class Z_ProjektStromverbraucherCtrl : Z_ProjektStromverbraucherModel
    {
        private List<Z_ProjektStromverbraucherModel> _internalList = new List<Z_ProjektStromverbraucherModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektStromverbraucherModel> items => _internalList;
        public Z_ProjektStromverbraucherModel model;

        public Z_ProjektStromverbraucherCtrl()
        {
            model = new Z_ProjektStromverbraucherModel();
        }

        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query gegen SQL-Injections und Formatierungsprobleme bei Nachkommastellen (Double)
                string sql = @"UPDATE Z_Projekt_Stromverbraucher 
                               SET Summe = ? 
                               WHERE Bezeichner = ? 
                                 AND ID_Projekt = ?";

                DbParam[] ps = {
                    new DbParam("@summe", dSumme),
                    new DbParam("@bez", szBezeichner ?? (object)DBNull.Value),
                    new DbParam("@idProj", IDProjekt)
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei UpdateSumme: " + ex.Message);
                return false;
            }
        }

        public void ReadAll(string sql)
        {
            // Daten abrufen über DataRepository
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem Befüllen bereinigen
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();

                // Spaltenweises, sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Stromverbraucher") && row["ID_Stromverbraucher"] != DBNull.Value)
                    item.m_ID_Stromverbraucher = Convert.ToInt32(row["ID_Stromverbraucher"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szVerbraucher = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Summe") && row["Summe"] != DBNull.Value)
                    item.m_Summe = Convert.ToDouble(row["Summe"]);

                // Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}