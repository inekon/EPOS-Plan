using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class Z_ProjGebCtrl : Z_ProjGebModel
    {
        OdbcCommand DBCommand;
        Z_ProjGebModel model;
        public int rows;

        public Z_ProjGebCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjGebModel();
        }

        ~Z_ProjGebCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjGebModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                Z_ProjGebModel item = new Z_ProjGebModel();

                if (!DBReader.IsDBNull(0)) item.ID_Z = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.ID_Gebaeude = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.ID_Projekt = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.Wohnflaeche = (double)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.Einheit = (string)DBReader.GetString(4);
                if (!DBReader.IsDBNull(5)) item.Jahresnutzungsgrad = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.DezentralWarmwasser = (bool)DBReader.GetValue(6);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
