using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class ProjektCtrl : ProjektModel
    {
        public int rows;
        OdbcCommand DBCommand;
        ProjektModel projektmodel;

        public ProjektCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            projektmodel = new ProjektModel();
        }
        ~ProjektCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void Update()
        {
            DBCommand.CommandText = "UPDATE Tab_Projekt SET Bearbeiter='" + Bearbeiter + "', Beschreibung='" + Beschreibung + "', Kunde='" + Kunde + "', Aenderungsdatum='" + Aenderung + "' WHERE Projektname='" + Projektname + "'";
            DBCommand.ExecuteNonQuery();
        }
            
        public void Delete()
        {
            DBCommand.CommandText = "DELETE Projektname FROM Tab_Projekt WHERE Projektname='" + Projektname + "'";
            DBCommand.ExecuteNonQuery();
        }

        public void Add()
        {
            DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Projekt";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            DBReader.Read();
            ID = (int)DBReader.GetValue(0) + 1;
            DBReader.Close();
            DBCommand.CommandText = "INSERT INTO TAB_Projekt ( ID, Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum ) SELECT " + ID + " AS Ausdr1, '" + Projektname + "' AS Ausdr2, '" + Bearbeiter + "' AS Ausdr3, '" + Kunde + "' AS Ausdr4, '" + Beschreibung + "' AS Ausdr5, '" + Aenderung + "' AS Ausdr6";
            DBCommand.ExecuteNonQuery();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Projekt order by Projektname";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            int fCount = DBReader.FieldCount;
            projektmodel.items = new ProjektModel[fCount];
            string fName = DBReader.GetName(fCount - 1);
            rows = 0;
            while (DBReader.Read())
            {
                ProjektModel item = new ProjektModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.Projektname = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.Bearbeiter = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.Beschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.Kunde = (string)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Aenderung = (DateTime)DBReader.GetValue(5);



                projektmodel.items[rows] = item;
                item = null;
                rows += 1;
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
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) Projektname = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) Bearbeiter = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) Beschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) Kunde = (string)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) Aenderung = (DateTime)DBReader.GetValue(5);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(projektmodel.items[i].Projektname);
            }
         }
        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(projektmodel.items[i].Projektname);
            }
        }
    }
}
