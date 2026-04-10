using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class Z_ProjektProzesswaermeCtrl : Z_ProjektProzesswaermeModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public Z_ProjektProzesswaermeModel model;

        public Z_ProjektProzesswaermeCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjektProzesswaermeModel();
        }
        ~Z_ProjektProzesswaermeCtrl()
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
                DBCommand.CommandText = "UPDATE Z_Projekt_Prozesswaerme SET Summe=" + dSumme.ToString("F2", formatInfo) +
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

            items = new Z_ProjektProzesswaermeModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();

                if (!DBReader.IsDBNull(0)) item.ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.ID_Prozesswaerme = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.szProzessname = (string)DBReader.GetString(3);
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
