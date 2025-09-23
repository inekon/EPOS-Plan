using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class WaermebedarfCtrl : WaermebedarfModel
    {
        OdbcCommand DBCommand;
        OdbcDataReader DBReader;
        public int rows;
 
        public WaermebedarfCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~WaermebedarfCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Waermebedarf where Bezeichner= '" + szName + "'";
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
                DBCommand.CommandText = "SELECT Count(*) FROM Tab_Waermebedarf";
                OdbcDataReader DBReader = DBCommand.ExecuteReader();
                DBReader.Read();  
                int result = (int)DBReader.GetValue(0);
                DBReader.Close();

                if (result == 0) m_ID_Ganglinie = 1;
                else
                {
                    DBCommand.CommandText = "SELECT Max(ID_GanglinieDaten) AS Ausdr1 FROM Tab_Waermebedarf";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    m_ID_Ganglinie = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }
                DBCommand.CommandText = "INSERT INTO Tab_Waermebedarf ( ID_GanglinieDaten, Bezeichner) SELECT " + m_ID_Ganglinie +
                    " AS Ausdr1, '" + m_szBezeichner + "' AS Ausdr2";
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
            DBCommand.CommandText = "select * from Tab_Waermebedarf order by Bezeichner";
            DBReader = DBCommand.ExecuteReader();

            items = new WaermebedarfModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                WaermebedarfModel item = new WaermebedarfModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_ID_Ganglinie = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.m_szBezeichner = (string)DBReader.GetString(2);
    
                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string szBezeichner)
        {
            DBCommand.CommandText = "select * from Tab_Waermebedarf where Bezeichner='" + szBezeichner + "'";
            DBReader = DBCommand.ExecuteReader();

            rows = 0;

            if(DBReader.Read())
            {
                WaermebedarfModel item = new WaermebedarfModel();

                if (!DBReader.IsDBNull(0)) ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_ID_Ganglinie = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) m_szBezeichner = (string)DBReader.GetString(2);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }
    }
}
