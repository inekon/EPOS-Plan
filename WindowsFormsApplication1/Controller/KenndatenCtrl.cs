using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class KenndatenCtrl : KenndatenModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public KenndatenModel model;

        public KenndatenCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new KenndatenModel();
        }
        ~KenndatenCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Delete()
        {
            try
            {
                DBCommand.CommandText = "DELETE WPName FROM Tab_Kenndaten WHERE ID_WP=" + m_ID_WP;
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
                DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Kenndaten";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();
                m_ID = (int)DBReader.GetValue(0) + 1;
                DBReader.Close();
                DBCommand.CommandText = "INSERT INTO Tab_Kenndaten ( ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm ) SELECT " + m_ID + " AS Ausdr1, " + m_ID_WP + " AS Ausdr2, " + m_nVorlauf + " AS Ausdr3, " + m_nTemperatur + " AS Ausdr4, " + m_nCOP + " AS Ausdr5, " + m_nPTherm + " AS Ausdr6";
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
                DBCommand.CommandText = "UPDATE Tab_Kenndaten SET ID_WP=" + m_ID_WP + "', Vorlauf=" + m_nVorlauf + ", Temperatur=" + m_nTemperatur + ", COP=" + m_nCOP + ", Ptherm=" + m_nPTherm;
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
        
        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Kenndaten order by ID_WP";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KenndatenModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                KenndatenModel item = new KenndatenModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_WP = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_nVorlauf = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.m_nTemperatur = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.m_nCOP = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_nPTherm = (int)DBReader.GetValue(5);

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
                if (!DBReader.IsDBNull(4)) m_nCOP = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_nPTherm = (int)DBReader.GetValue(5);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadVorlauf(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new KenndatenModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                KenndatenModel item = new KenndatenModel();

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
