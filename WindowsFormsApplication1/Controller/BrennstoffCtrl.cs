using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class BrennstoffCtrl : BrennstoffModel 
    {
        public int rows;
        public OdbcCommand DBCommand;
        OdbcDataReader DBReader;
        public BrennstoffModel model;


        public static string[] Brennstoffart = { "Öl", "Gas", "", "Rapsöl", "Holz/Pellet", "Sonstiges", "", "", "Flüssiggas", "", "", "Biogas", "", "","Fernwärme","Strom" };

        public BrennstoffCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new BrennstoffModel();
        }

        ~BrennstoffCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string filter = "")
        {
            string sql;

            if (filter == "")
            {
                sql = "select * from [DB-Heizung]";
            }
            else sql = "select * from [DB-Heizung] where " + filter;
            DBCommand.CommandText = sql;
            DBReader = DBCommand.ExecuteReader();

            items = new BrennstoffModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                BrennstoffModel item = new BrennstoffModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.Name = (string)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Firma = (string)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.Beschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Brennstoff = (int)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.Wirkungsgrad_Gas = (double)DBReader.GetValue(6);    
                if (!DBReader.IsDBNull(7)) item.Wirkungsgrad_Oel = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.Investitionskosten = (double)DBReader.GetValue(8);  
                if (!DBReader.IsDBNull(9)) item.Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.Wartungskosten = (double)DBReader.GetValue(10);     
                if (!DBReader.IsDBNull(11)) item.Nutzungsdauer = (double)DBReader.GetValue(11);      
                if (!DBReader.IsDBNull(12)) item.CO2 = (double)DBReader.GetValue(12);    
                if (!DBReader.IsDBNull(13)) item.SO2 = (double)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.NOx = (double)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) item.CO = (double)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) item.Staub = (double)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) item.Betriebsbereitschaftverlust = (double)DBReader.GetValue(17);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Close();
            DBReader.Dispose();
            //DBReader.Close();
        }
        
        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM [DB-Heizung] where Name= '" + szName + "'";
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

        public bool Update()
        {
            try
            {
                string sql = "UPDATE [DB-Heizung] SET Beschreibung='" + model.Beschreibung + "'" +
                    ", Firma = '" + model.Firma + "'" +
                    ", Ptherm=" + model.Ptherm.ToString(CultureInfo.CreateSpecificCulture("en-US"))  + ", Brennstoff=" + model.Brennstoff + 
                    ", Wirkungsgrad_Gas=" + model.Wirkungsgrad_Gas.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Wirkungsgrad_Öl= " + model.Wirkungsgrad_Oel.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Investitionskosten= " + model.Investitionskosten.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Raumbedarf= " + model.Raumbedarf.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Wartungskosten= " + model.Wartungskosten.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Nutzungsdauer= " + model.Nutzungsdauer.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", CO2= " + model.CO2.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", SO2= " + model.SO2.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", NOx= " + model.NOx.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", CO= " + model.CO.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Staub= " + model.Staub.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Betriebsbereitschaftverlust= " + model.Betriebsbereitschaftverlust.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    " WHERE Name='" + model.Name + "'";
                
                DBCommand.CommandText = sql;
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
    }
}
