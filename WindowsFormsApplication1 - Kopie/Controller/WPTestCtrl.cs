using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class WPTestCtrl : WPModel
    {
        public int rows;
        OdbcCommand DBCommand;
        WPModel wpmodel;

        public WPTestCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            wpmodel = new WPModel();
        }
        ~WPTestCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void Update()
        {
            DBCommand.CommandText = "UPDATE Tab_WP SET Firma='" + Firma + "', Beschreibung='" + Beschreibung + "'";
            DBCommand.ExecuteNonQuery();
        }
            
        public void Delete()
        {
            DBCommand.CommandText = "DELETE WPName FROM Tab_WP WHERE WPName='" + WPName + "'";
            DBCommand.ExecuteNonQuery();
        }

        public void Add()
        {
            DBCommand.CommandText = "SELECT Max(ID_WP) AS Ausdr1 FROM Tab_WP";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            DBReader.Read();
            ID = (int)DBReader.GetValue(0) + 1;
            DBReader.Close();
            DBCommand.CommandText = "INSERT INTO TAB_WP ( ID_WP, WPName, Beschreibung ) SELECT " + ID + " AS Ausdr1, '" + WPName + "' AS Ausdr2, '" + Beschreibung + "' AS Ausdr3";
            DBCommand.ExecuteNonQuery();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_WP order by ID_WP";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            int fCount = DBReader.FieldCount;
            wpmodel.items = new WPModel[fCount];
            string fName = DBReader.GetName(fCount - 1);
            rows = 0;
            while (DBReader.Read())
            {
                WPModel item = new WPModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.WPName = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.Firma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.Beschreibung = (string)DBReader.GetValue(3);
                
                wpmodel.items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;
            DBReader.Read();
            if (!DBReader.IsDBNull(0)) ID = (int)DBReader.GetValue(0);
            if (!DBReader.IsDBNull(1)) WPName = DBReader.GetString(1);
            if (!DBReader.IsDBNull(2)) Firma = DBReader.GetString(2);
            if (!DBReader.IsDBNull(3)) Beschreibung = (string)DBReader.GetValue(3);
               
            rows = 1;
            DBReader.Dispose();
            DBReader.Close();
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(wpmodel.items[i].WPName);
            }
         }

    }
}
