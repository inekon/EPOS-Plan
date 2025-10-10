using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class BrennstoffCtrl : BrennstoffModel 
    {
        public int rows;
        OdbcCommand DBCommand;
        OdbcDataReader DBReader;
        public BrennstoffModel model;


        public static string[] Brennstoffart = { "Öl", "Gas", "Holz", "", "Pellet", "", "", "", "Gas Brennwert", "", "", "", "", "" };

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
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
