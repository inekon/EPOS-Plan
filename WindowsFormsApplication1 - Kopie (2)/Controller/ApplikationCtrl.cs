using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class ApplikationCtrl : ApplikationModel
    {
        OdbcCommand DBCommand;
        ApplikationModel applikationmodel;
        public int rows;
 
        public ApplikationCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            applikationmodel = new ApplikationModel();
        }
        
        ~ApplikationCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Update()
        {
            try
            {
                DBCommand.CommandText = "UPDATE Tab_Applikation SET Projektname='" + m_szProjektname + "', ID_Projekt=" + m_ID_Projekt + ", Beschreibung='" + m_szBeschreibung + "', Icon='" + m_icon + "' WHERE ID=" + 1;
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

        public void ReadSingle(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;
            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szProjektname = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_ID_Projekt = (int)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_icon = (string)DBReader.GetValue(4);
                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }
    }
}
