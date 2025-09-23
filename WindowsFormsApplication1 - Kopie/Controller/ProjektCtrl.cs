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

        public bool Update()
        {
            try
            {
                DBCommand.CommandText = "UPDATE Tab_Projekt SET Bearbeiter='" + m_szBearbeiter + "', Beschreibung='" + m_szBeschreibung + "', Kunde='" + m_szKunde + "', Aenderungsdatum='" + m_Aenderungsdatum + "', ID_Klimaregion=" + m_ID_Klimaregion + ", Erstelldatum='" + m_Erstelldatum + "' WHERE Projektname='" + m_szProjektname + "'";
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
            
        public bool Delete(string szProjekt)
        {
            try
            {
                DBCommand.CommandText = "DELETE Projektname FROM Tab_Projekt WHERE Projektname='" + szProjekt + "'";
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
                DBCommand.CommandText = "SELECT Count(*) FROM TAB_Projekt";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();  
                int result = (int)DBReader.GetValue(0);
                DBReader.Close();

                if (result == 0) m_ID = 1;
                else
                {
                    DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Projekt";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    m_ID = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }

                DBCommand.CommandText = "INSERT INTO TAB_Projekt ( ID, Projektname, Bearbeiter, Beschreibung, Kunde, " +
                    "Aenderungsdatum, ID_Klimaregion, ID_Gebaeude, Erstelldatum ) SELECT " + m_ID + " AS Ausdr1, '" +
                    m_szProjektname + "' AS Ausdr2, '" + m_szBearbeiter + "' AS Ausdr3, '" + m_szKunde + "' AS Ausdr4, '" +
                    m_szBeschreibung + "' AS Ausdr5, '" + m_Aenderungsdatum + "' AS Ausdr6, " + m_ID_Klimaregion + " AS Ausdr7, " +
                    m_ID_Gebaeude + " AS Ausdr8, '" + m_Erstelldatum + "' AS Ausdr7, " + m_ID_Klimaregion + " AS Ausdr7";
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
            return true;        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Projekt order by Projektname";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            items = new ProjektModel[100];
            rows = 0;
            
            while (DBReader.Read())
            {
                ProjektModel item = new ProjektModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szProjektname = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szBearbeiter = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_szBeschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.m_szKunde = (string)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Aenderungsdatum = (DateTime)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_ID_Klimaregion = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_ID_Gebaeude = (int)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_Erstelldatum = (DateTime)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_nNetzverluste = (int)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.m_szEinheit = (string)DBReader.GetString(10);

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
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szProjektname = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szBearbeiter = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) m_szKunde = (string)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Aenderungsdatum = (DateTime)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_ID_Klimaregion = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_ID_Gebaeude = (int)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_Erstelldatum = (DateTime)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_nNetzverluste = (int)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_szEinheit = (string)DBReader.GetString(10);
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
                ctrl.Items.Add(items[i].m_szProjektname);
            }
        }
     
        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szProjektname);
            }
        }

        public int GetMaxID()
        {
            int maxID = 0;

            DBCommand.CommandText = "SELECT Count(*) FROM Tab_Projekt";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            DBReader.Read();  
            int result = (int)DBReader.GetValue(0);
            DBReader.Close();

            if (result == 0) maxID = 1;
            else
            {
                DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Projekt";
                DBReader = DBCommand.ExecuteReader();
                DBReader.Read();
                maxID = (int)DBReader.GetValue(0);
                DBReader.Close();
            }
            return maxID;
        }
    }
}
