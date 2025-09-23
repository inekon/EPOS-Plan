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
                if (!DBReader.IsDBNull(3)) item.Firma = (string)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(4)) item.Beschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(5)) item.Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(6)) item.Brennstoff = (int)DBReader.GetValue(5);
                
                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
