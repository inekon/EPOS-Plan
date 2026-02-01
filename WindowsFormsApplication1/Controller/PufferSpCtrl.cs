using System;
using System.Data.Odbc;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class PufferSpCtrl : PufferSpModel 
    {
        public int rows;
        public OdbcCommand DBCommand;
        OdbcDataReader DBReader;
        public PufferSpModel model;
        
        public PufferSpCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new PufferSpModel();
        }

        ~PufferSpCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string filter = "")
        {
            string sql;

            if (filter == "")
            {
                sql = "select * from Tab_Pufferspeicher";
            }
            else sql = "select * from Tab_Pufferspeicher where " + filter;
            DBCommand.CommandText = sql;
            DBReader = DBCommand.ExecuteReader();

            items = new PufferSpModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                PufferSpModel item = new PufferSpModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.Name = (string)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Firma = (string)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) item.Speichertyp = (string)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.Betriebsbereitschaftverlust = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Gesamtvolumen = (int)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.Investitionskosten = (double)DBReader.GetValue(6);

                items[rows] = item;
                rows += 1;
            }
            DBReader.Close();
            DBReader.Dispose();
            //DBReader.Close();
        }
        
        public bool Delete(string szName)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Pufferspeicher where Bezeichner='" + szName + "'";
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
                string sql = "UPDATE Tab_Pufferspeicher SET " +
                    "Hersteller = '" + model.Firma + "'" +
                    ", Speichertyp='" + model.Speichertyp + "'" +
                    ", Bereitschaftsverluste= " + model.Betriebsbereitschaftverlust.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Investitionskosten= " + model.Investitionskosten.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Gesamtvolumen= " + model.Gesamtvolumen.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    " WHERE Bezeichner='" + model.Name + "'";
                
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
    }
}
