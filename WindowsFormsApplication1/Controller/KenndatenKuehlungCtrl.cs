using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class KenndatenKuehlungCtrl: KenndatenKuehlungModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public KenndatenKuehlungModel model;

        public KenndatenKuehlungCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new KenndatenKuehlungModel();
        }
        ~KenndatenKuehlungCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Delete()
        {
            try
            {
                DBCommand.CommandText = "DELETE WPName FROM Tab_Kenndaten_Kuehlung WHERE ID_WP=" + m_ID_WP;
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
                DBCommand.CommandText = "SELECT Count(*) FROM Tab_Kenndaten_Kuehlung";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();  
                int result = (int)DBReader.GetValue(0);
                DBReader.Close();

                if (result == 0) m_ID = 1;
                else
                {
                    DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Kenndaten_Kuehlung";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    m_ID = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }

                NumberFormatInfo formatInfo = new NumberFormatInfo();
                formatInfo.NumberDecimalSeparator = "."; // Komma als Dezimaltrennzeichen

                DBCommand.CommandText = "INSERT INTO Tab_Kenndaten_Kuehlung ( ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last ) SELECT " + m_ID + " AS Ausdr1, " + m_ID_WP + " AS Ausdr2, " + m_nVorlauf + " AS Ausdr3, " + m_nTemperatur + " AS Ausdr4, " + m_nCOP.ToString("N2", formatInfo) + " AS Ausdr5, " + m_nPkuehl.ToString("N2", formatInfo) + " AS Ausdr6, " + m_nLast.ToString() + " AS Ausdr7";
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

        public bool Update()
        {
            try
            {
                DBCommand.CommandText = "UPDATE Tab_Kenndaten_Kuehlung SET ID_WP=" + m_ID_WP + "', Vorlauf=" + m_nVorlauf + ", Temperatur=" + m_nTemperatur + ", COP=" + m_nCOP + ", Ptherm=" + m_nPkuehl;
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
        
        public void ReadAll(int ID_WP=0)
        {
            if(ID_WP >0)
                DBCommand.CommandText = "select * from Tab_Kenndaten_Kuehlung where ID_WP=" + ID_WP + " order by ID_WP";
            else
                DBCommand.CommandText = "select * from Tab_Kenndaten_Kuehlung order by ID_WP";
            
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KenndatenKuehlungModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                KenndatenKuehlungModel item = new KenndatenKuehlungModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_WP = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_nVorlauf = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_nTemperatur = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.m_nCOP = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_nPkuehl = (double)DBReader.GetValue(5);

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
            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_ID_WP = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) m_nVorlauf = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) m_nTemperatur = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) m_nCOP = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_nPkuehl = (double)DBReader.GetValue(5);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadVorlauf(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KenndatenKuehlungModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                KenndatenKuehlungModel item = new KenndatenKuehlungModel();

                if (!DBReader.IsDBNull(0)) item.m_nVorlauf = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_WP = (int)DBReader.GetValue(1);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }
     }
}
