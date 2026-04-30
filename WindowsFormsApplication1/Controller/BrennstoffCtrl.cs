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


        public static string[] Brennstoffart = { "",
            "Stadtgas","Erdgas LL",
            "Erdgas E","Flüssiggas (Propan)","Flüssiggas (Butan)",
            "Heizöl S","Heizöl M","Heizöl L",
            "Heizöl EL","Koks","Kohle",
            "Holz","Elektrische Energie","Biogas",
            "Pellets","Rapsöl","Tierische Fette",
            "Heizöl Bio 5","Heizöl Bio 10","Heizöl Bio 15",
            "Heizöl Bio 20","Heizöl EL schwefelarm","Sonstige Energieträger"
         };

        public static string[] Brennstoffart_Gruppe = { 
            "Gas","Öl","Koks","Kohle","Holz","Pellets","Strom","Rapsöl","Tierische Fette","Sonstige"
         };

        public BrennstoffCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new BrennstoffModel();
            int i = 0;
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_BrennstoffKategorien order by ID");
            while (rs.Next()) Brennstoffart_Gruppe[i++] = (string)rs.Read("Gruppe");
            rs.Close();
            i = 0;
            rs.Open("select * from Tab_Brennstoff_Stamm order by ID");
            while (rs.Next()) Brennstoffart[i++] = (string)rs.Read("Name");
            rs.Close();
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
                sql = "select * from [Tab_Heizkessel]";
            }
            else sql = "select * from [Tab_Heizkessel] where " + filter;
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
            }
            DBReader.Close();
            DBReader.Dispose();
            //DBReader.Close();
        }
        
        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM [Tab_Heizkessel] where Name= '" + szName + "'";
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
                FormattableString sql = $@"
                    UPDATE [Tab_Heizkessel] SET 
                        Beschreibung = '{model.Beschreibung}',
                        Firma = '{model.Firma}',
                        Ptherm = {model.Ptherm:0.####},
                        Brennstoff = {model.Brennstoff},
                        Wirkungsgrad_Gas = {model.Wirkungsgrad_Gas:0.####},
                        Wirkungsgrad_Öl = {model.Wirkungsgrad_Oel:0.####},
                        Investitionskosten = {model.Investitionskosten:0.####},
                        Raumbedarf = {model.Raumbedarf:0.####},
                        Wartungskosten = {model.Wartungskosten:0.####},
                        Nutzungsdauer = {model.Nutzungsdauer:0.####},
                        CO2 = {model.CO2:0.####},
                        SO2 = {model.SO2:0.####},
                        NOx = {model.NOx:0.####},
                        CO = {model.CO:0.####},
                        Staub = {model.Staub:0.####},
                        Betriebsbereitschaftverlust = {model.Betriebsbereitschaftverlust:0.####}
                    WHERE Name = '{model.Name}';";

                DBCommand.CommandText = sql.ToString(CultureInfo.InvariantCulture) ;
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
