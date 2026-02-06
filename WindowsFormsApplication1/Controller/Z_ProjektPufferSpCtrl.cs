using System;
using System.Data.Odbc;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class Z_ProjektPufferSpCtrl : Z_ProjektPufferSpModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public Z_ProjektPufferSpModel model;

        public Z_ProjektPufferSpCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new Z_ProjektPufferSpModel();
        }
        
        ~Z_ProjektPufferSpCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Delete()
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Z_ProjektPufferSp WHERE ID_Projekt=" + ID_Projekt;
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
                DBCommand.CommandText = "INSERT INTO Z_ProjektPufferSp (ID_Projekt, Erzeuger, Pufferspeicher, " +
                        "Vorlauf, Ruecklauf, Prioritaet ) SELECT " + ID_Projekt + " AS Ausdr2, '" + 
                        Erzeuger + "' AS Ausdr3, '" + PufferSp + "' AS Ausdr4, " +
                        Vorlauf + " AS Ausdr5, " + Ruecklauf  + " AS Ausdr6, " + Prioritaet + " AS Ausdr7";
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
        
        public void ReadAll(string szFilter)
        {
            if(szFilter == "")
                DBCommand.CommandText = "select * from Z_ProjektPufferSp order by Prioritaet";
            else
                DBCommand.CommandText = "select * from Z_ProjektPufferSp where " + szFilter + " order by Prioritaet";
            
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new Z_ProjektPufferSpModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                Z_ProjektPufferSpModel item = new Z_ProjektPufferSpModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Erzeuger = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.PufferSp = DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.Vorlauf = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Ruecklauf = (int)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.Prioritaet = (int)DBReader.GetValue(6);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

     }
}
