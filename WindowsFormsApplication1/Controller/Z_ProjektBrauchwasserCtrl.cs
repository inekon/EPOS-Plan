using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class Z_ProjektBrauchwasserCtrl : Z_ProjektBrauchwasserModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public Z_ProjektBrauchwasserModel model;

        public Z_ProjektBrauchwasserCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjektBrauchwasserModel();
        }
        ~Z_ProjektBrauchwasserCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool UpdateSumme(double dSumme, string szBezeichner, int IDProjekt)
        {
            try
            {
                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = "."; // Komma als Dezimaltrennzeichen
                DBCommand.CommandText = "UPDATE Z_Projekt_Brauchwasser SET Summe=" + dSumme.ToString("F2", formatInfo) +
                    " WHERE Bezeichner='" + szBezeichner + "' and ID_Projekt=" + IDProjekt;
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjektBrauchwasserModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjektBrauchwasserModel item = new Z_ProjektBrauchwasserModel();

                if (!DBReader.IsDBNull(0)) item.ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.ID_Brauchwasser = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.szBezeichner = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.Summe = (double)DBReader.GetValue(4);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }



    }
}
