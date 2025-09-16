using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class KlimaregionCtrl : KlimaregionModel
    {
        OdbcCommand DBCommand;
        private List<double> list = new List<double>();
 
        public KlimaregionCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Klimaregion order by Name";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KlimaregionModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                KlimaregionModel item = new KlimaregionModel();

                if (!DBReader.IsDBNull(0)) item.m_ID_Klimaregion = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szName = DBReader.GetString(1);


                items[rows] = item;
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
                if (!DBReader.IsDBNull(0)) m_ID_Klimaregion = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szName = DBReader.GetString(1);


                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Klimaregion where Name= '" + szName + "'";
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;
        }

        public bool Add(string szName)
        {
            try
            {
                DBCommand.CommandText = "SELECT Max(ID_Klimaregion) AS Ausdr1 FROM Tab_Klimaregion";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();
                m_ID_Klimaregion = (int)DBReader.GetValue(0) + 1;
                DBReader.Close();
                DBCommand.CommandText = "INSERT INTO TAB_Klimaregion ( ID_Klimaregion, Name ) SELECT " + m_ID_Klimaregion + " AS Ausdr1, '" + szName + "'";
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;
        }

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }

    }
}
