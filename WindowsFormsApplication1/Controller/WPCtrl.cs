using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class WPCtrl : WPModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public WPModel wpmodel;

        public WPCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            wpmodel = new WPModel();
        }
        
        ~WPCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Update()
        {
            try
            {
                string sql = "UPDATE Tab_WP SET Firma='" + Firma + "', Beschreibung='" + Beschreibung + "', " +
                "Typ='" + Typ + "', Baujahr=" + Baujahr.ToString() + ", Aufstellung='" + Aufstellung + "', " +
                "Nennleistung=" + Nennleistung.ToString() + ", maxPTherm=" + maxPTherm.ToString() + ", " +
                "Heizung=" + Heizung.ToString() + ", Regelung='" + Regelung + "', Modulkosten=" + Modulkosten.ToString() + 
                " WHERE WPName='" + WPName + "'";
            
                DBCommand.CommandText = sql;
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
            
        public bool Delete()
        {
            try
            {
                DBCommand.CommandText = "DELETE WPName FROM Tab_WP WHERE WPName='" + WPName + "'";
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

        public bool Insert()
        {
            try
            {
                DBCommand.CommandText = "SELECT Count(*) FROM TAB_WP";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();  
                int result = (int)DBReader.GetValue(0);
                DBReader.Close();

                if (result == 0) ID = 1;
                else
                {
                    DBCommand.CommandText = "SELECT Max(ID_WP) AS Ausdr1 FROM Tab_WP";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    ID = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }

                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = "."; // Komma als Dezimaltrennzeichen

                DBCommand.CommandText = "INSERT INTO TAB_WP ( ID_WP, WPName, Firma, Beschreibung, Typ, Baujahr, Aufstellung," +
                "Nennleistung, maxPTherm, Heizung, Regelung, Modulkosten, Kuehlleistung )" +
                "SELECT " + ID.ToString() + " AS Ausdr1, '" + WPName + "' AS Ausdr2,'" + Firma  + "' AS Ausdr12, '" + Beschreibung + "' AS Ausdr3, '" +
                Typ + "' AS Ausdr4, " + Baujahr.ToString() + " AS Ausdr5, '" + Aufstellung + "' AS Ausdr6, " +
                Nennleistung.ToString() + " AS Ausdr7, " + maxPTherm.ToString() + " AS Ausdr8, " + Heizung + " AS Ausdr9, '" +
                Regelung + "' AS Ausdr10, " + Modulkosten.ToString() + " AS Ausdr11, " + Kuehlleistung.ToString("N2", formatInfo) + " AS Ausdr13";
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

        public void ReadAll(string filter="")
        {
            string sql;

            if (filter == "")
            {
                sql = "select * from Tab_WP order by WPName";
            }
            else sql = "select * from Tab_WP where " + filter;
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new WPModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                WPModel item = new WPModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.WPName = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.Firma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.Beschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.Typ = (string)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Baujahr = (int)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.Aufstellung = (string)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.Nennleistung = (int)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.maxPTherm = (int)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.Heizung = (int)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.Regelung = (string)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.Modulkosten = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(17)) item.Kuehlleistung = (double)DBReader.GetValue(17);

                items[rows] = item;
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
            if (DBReader.Read())
            {
                if (!DBReader.IsDBNull(0)) ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) WPName = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) Firma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) Beschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) Typ = (string)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) Baujahr = (int)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) Aufstellung = (string)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) Nennleistung = (int)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(9)) Heizung = (int)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) Regelung = (string)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) Modulkosten = (int)DBReader.GetValue(11);

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
                ctrl.Items.Add(items[i].WPName);
            }
         }

    }
}
