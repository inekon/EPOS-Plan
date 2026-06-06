using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class Z_ProjektProzesswaermeCtrl : Z_ProjektProzesswaermeModel
    {
        private List<Z_ProjektProzesswaermeModel> _internalList = new List<Z_ProjektProzesswaermeModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektProzesswaermeModel> items => _internalList;

        public Z_ProjektProzesswaermeCtrl()
        {
        }

        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query: Verhindert SQL-Injections und regelt Nachkommastellen (Double) automatisch fehlerfrei
                string sql = @"UPDATE Z_Projekt_Prozesswaerme 
                               SET Summe = ? 
                               WHERE Bezeichner = ? 
                                 AND ID_Projekt = ?";

                OleDbParameter[] ps = {
                    new OleDbParameter("@summe", dSumme),
                    new OleDbParameter("@bez", szBezeichner ?? (object)DBNull.Value),
                    new OleDbParameter("@idProj", IDProjekt)
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
            // Daten abrufen über das zentrale DataRepository
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem erneuten Laden leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Prozesswaerme") && row["ID_Prozesswaerme"] != DBNull.Value)
                    item.ID_Prozesswaerme = Convert.ToInt32(row["ID_Prozesswaerme"]);

                if (dt.Columns.Contains("szProzessname") && row["szProzessname"] != DBNull.Value)
                    item.szProzessname = row["szProzessname"].ToString();

                // Falls die Spalte in der Access-Tabelle stattdessen "Prozessname" heißt, hier anpassen:
                else if (dt.Columns.Contains("Prozessname") && row["Prozessname"] != DBNull.Value)
                    item.szProzessname = row["Prozessname"].ToString();

                if (dt.Columns.Contains("Summe") && row["Summe"] != DBNull.Value)
                    item.Summe = Convert.ToDouble(row["Summe"]);

                // Das fertige Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}
