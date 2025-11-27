using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class Z_ProjektStromverbraucherCtrl : Z_ProjektStromverbraucherModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public Z_ProjektStromverbraucherModel model;

        public Z_ProjektStromverbraucherCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjektStromverbraucherModel();
        }
        ~Z_ProjektStromverbraucherCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjektStromverbraucherModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_ID_Stromverbraucher = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_szVerbraucher = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.m_Summe = (double)DBReader.GetValue(4);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }



    }
}
