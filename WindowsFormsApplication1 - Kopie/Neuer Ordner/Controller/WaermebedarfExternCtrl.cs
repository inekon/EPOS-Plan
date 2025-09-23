using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class WaermebedarfExternCtrl : WaermebedarfExternModel
    {
        OdbcCommand DBCommand;
        OdbcDataReader DBReader;
        public int rows;
 
        public WaermebedarfExternCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~WaermebedarfExternCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(int ID_Projekt)
        {
            DBCommand.CommandText = "select * from Tab_WaermebedarfExtern where ID_Projekt=" + ID_Projekt;
            DBReader = DBCommand.ExecuteReader();

            items = new WaermebedarfExternModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                WaermebedarfExternModel item = new WaermebedarfExternModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_szDateiname = (string)DBReader.GetString(2);
    
                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }
    }
}
