using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class ApplikationCtrl : ApplikationModel
    {
        OdbcCommand DBCommand;
        ApplikationModel applikationmodel;
        public int rows;

        public ApplikationCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            applikationmodel = new ApplikationModel();
        }
        ~ApplikationCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void Update()
        {
            DBCommand.CommandText = "UPDATE Tab_Applikation SET Projektname='" + Projektname + "', ID_Projekt=" + ID_Projekt + ", Beschreibung='" + Beschreibung + "' WHERE ID=" + 1;
            DBCommand.ExecuteNonQuery();
        }

        public void ReadSingle(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;
            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) Projektname = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) ID_Projekt = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) Beschreibung = (string)DBReader.GetString(3);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }
    }
}
