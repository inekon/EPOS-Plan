using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class Z_ProjektSolarganglinieCtrl : Z_ProjektSolarganglinieModel 
    {
        public int rows;
        OdbcCommand DBCommand;
        public Z_ProjektSolarganglinieModel model;

        public Z_ProjektSolarganglinieCtrl ()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjektSolarganglinieModel();
        }
        
        ~Z_ProjektSolarganglinieCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjektSolarganglinieModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjektSolarganglinieModel item = new Z_ProjektSolarganglinieModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_ID_Solarganglinie = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_szSolarganglinie = (string)DBReader.GetString(3);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }



    }
}
