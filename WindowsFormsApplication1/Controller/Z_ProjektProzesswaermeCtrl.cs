using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

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
