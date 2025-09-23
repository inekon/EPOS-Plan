using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class Z_ProjektGebGanglinieCtrl : Z_ProjWaermebedarfModel
    {
        OdbcCommand DBCommand;
        Z_ProjWaermebedarfModel model;
        public int rows;

        public Z_ProjektGebGanglinieCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjWaermebedarfModel();
        }

        ~Z_ProjektGebGanglinieCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjWaermebedarfModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_ID_Ganglinie = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_szBezeichner = (string)DBReader.GetString(3);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
