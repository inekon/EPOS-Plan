using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class Z_ProjektStromganglinieCtrl : Z_ProjektStromganglinieModel 
    {
        public int rows;
        OdbcCommand DBCommand;
        public Z_ProjektStromganglinieModel model;

        public Z_ProjektStromganglinieCtrl ()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjektStromganglinieModel();
        }
        
        ~Z_ProjektStromganglinieCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjektStromganglinieModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjektStromganglinieModel item = new Z_ProjektStromganglinieModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_ID_Stromganglinie = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_szStromganglinie = (string)DBReader.GetString(3);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }



    }
}
