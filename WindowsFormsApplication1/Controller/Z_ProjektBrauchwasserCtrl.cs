using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class Z_ProjektBrauchwasserCtrl : Z_ProjektBrauchwasserModel
    {
        private List<Z_ProjektBrauchwasserModel> _internalList = new List<Z_ProjektBrauchwasserModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjektBrauchwasserModel> items => _internalList;

        public Z_ProjektBrauchwasserCtrl()
        {
        }

        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                // Parametrisierte Query: Typkonvertierungen (z. B. Dezimaltrennzeichen bei Double) werden automatisch korrekt gehandhabt
                string sql = @"UPDATE Z_Projekt_Brauchwasser 
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
                Z_ProjektBrauchwasserModel item = new Z_ProjektBrauchwasserModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Brauchwasser") && row["ID_Brauchwasser"] != DBNull.Value)
                    item.ID_Brauchwasser = Convert.ToInt32(row["ID_Brauchwasser"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.szBezeichner = row["Bezeichner"].ToString();

                // Fallback, falls die Spalte in Access exakt wie die Variable heißt
                else if (dt.Columns.Contains("szBezeichner") && row["szBezeichner"] != DBNull.Value)
                    item.szBezeichner = row["szBezeichner"].ToString();

                if (dt.Columns.Contains("Summe") && row["Summe"] != DBNull.Value)
                    item.Summe = Convert.ToDouble(row["Summe"]);

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }
    }
}