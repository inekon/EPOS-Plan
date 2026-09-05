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

        /// <summary>
        /// Die STROMVERBRAUCHER-ZUORDNUNGEN eines Projekts (iU9-W9.0d) — der JOIN aus
        /// <c>Form_Start.pBox_StdLastProfil_Click</c> (:494-509) und
        /// <c>StrombedarfKontextMenuCtrl.ContextMenuItemBearbeiten_Click</c>.
        /// </summary>
        public static List<Z_ProjektStromverbraucherModel> LiesProjekt(int idProjekt)
        {
            var liste = new List<Z_ProjektStromverbraucherModel>();

            const string sql =
                "SELECT Z_Projekt_Stromverbraucher.ID, Z_Projekt_Stromverbraucher.ID_Projekt, " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher, Z_Projekt_Stromverbraucher.Summe, " +
                "Tab_Stromverbraucher.Bezeichner " +
                "FROM Z_Projekt_Stromverbraucher INNER JOIN Tab_Stromverbraucher ON " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher = Tab_Stromverbraucher.ID " +
                "WHERE Z_Projekt_Stromverbraucher.ID_Projekt = ?";

            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idProjekt));
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                var item = new Z_ProjektStromverbraucherModel();
                item.m_ID_Z = Convert.ToInt32(row["ID"]);
                item.m_ID_Projekt = idProjekt;
                item.m_ID_Stromverbraucher = Convert.ToInt32(row["ID_Stromverbraucher"]);
                item.m_szVerbraucher = row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString();
                item.m_Summe = row["Summe"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["Summe"]);
                liste.Add(item);
            }
            return liste;
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