using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;
using System.Data;

namespace WindowsFormsApplication1
{
    class WErzeugerCtrl : WErzeugerModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public WErzeugerModel model;

        public const string selectCommandText = "SELECT * FROM Tab_Energieanlagen";
        public const string insertCommandText = "INSERT INTO Tab_Energieanlagen (ID, ID_Projekt,Bezeichner,ID_Type,ID_WP,Betriebsart,Sperrung,Sperrzeit_von,Sperrzeit_bis,Vorlauf,Ruecklauf,Bivalenter_Betrieb,Abschaltpunkt,Nutzungszeit,ID_SP,ID_PV,ID_Solar) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
        public const string updateCommandText = "UPDATE Tab_Energieanlagen SET Name = ? WHERE ID = ?";
        public const string deleteCommandText = "DELETE FROM Tab_Energieanlagen WHERE ID = ?";

        public WErzeugerCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new WErzeugerModel();
        }
        ~WErzeugerCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public bool Update()
        {
            try
            {
                DBCommand.CommandText =
                "UPDATE Tab_Energieanlagen SET ID_Projekt=" + ID_Projekt + "', Bezeichner='" + Bezeichner + "'" +
                " ID_Type=" + ID_Type + ", ID_WP=" + ID_WP + "' Betriebsart='" + Betriebsart + "'" +
                " Sperrung=" + Sperrung + ", Sperrzeit_von=" + Sperrzeit_von + " Sperrzeit_bis=" + Sperrzeit_bis +
                " Vorlauf=" + Vorlauf + ", Ruecklauf=" + Ruecklauf + " Bivalenter_Betrieb=" + Bivalenter_Betrieb +
                " Abschaltpunkt=" + Abschaltpunkt + ", Nutzungszeit=" + Nutzungszeit + ", ID_SP=" + ID_SP +
                " ID_PV=" + ID_PV + ", ID_Solar=" + ID_Solar;
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
                DBCommand.CommandText = "DELETE ID_Projekt FROM Tab_Energieanlagen WHERE ID_Projekt=" + ID_Projekt;
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

        public int GetMaxID()
        {
            int maxID = 0;

            DBCommand.CommandText = "SELECT Count(*) FROM Tab_Energieanlagen";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            DBReader.Read();  
            int result = (int)DBReader.GetValue(0);
            DBReader.Close();

            if (result == 0) maxID = 0;
            else
            {
                DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Energieanlagen";
                DBReader = DBCommand.ExecuteReader();
                DBReader.Read();
                maxID = (int)DBReader.GetValue(0);
            }
            return maxID; 
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
                    DBCommand.CommandText = "SELECT Max(ID) AS Ausdr1 FROM Tab_Energieanlagen";
                    DBReader = DBCommand.ExecuteReader();
                    DBReader.Read();
                    ID = (int)DBReader.GetValue(0) + 1;
                    DBReader.Close();
                }

                DBCommand.CommandText =
                    "INSERT INTO TAB_WP ( ID,ID_Projekt,Bezeichner,ID_Type,ID_WP,Betriebsart,Sperrung," +
                    "Sperrzeit_von,Sperrzeit_bis,Vorlauf,Ruecklauf,Bivalenter_Betrieb,Abschaltpunkt,Nutzungszeit," +
                    "ID_SP,ID_WP,ID_Solar) SELECT " + ID + " AS Ausdr1, " + ID_Projekt + " AS Ausdr2, '" +
                    Bezeichner + "' AS Ausdr3, " + ID_Type + " AS Ausdr4 , " + ID_WP + " AS Ausdr5, '" + Betriebsart + "' AS Ausdr6, " +
                    Sperrung + " AS Ausdr7, " + Sperrzeit_von + " AS Ausdr8, " + Sperrzeit_bis + " AS Ausdr9, "
                    + Vorlauf + " AS Ausdr10, " + Ruecklauf + " AS Ausdr11, " + Bivalenter_Betrieb + " AS Ausdr12," +
                    + Abschaltpunkt + " AS Ausdr13, " + Nutzungszeit + " AS Ausdr14, " + ID_SP + " AS Ausdr15," +
                    + ID_PV + " AS Ausdr16," + ID_Solar + " AS Ausdr17";
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

        public void ReadAllFilter(string filter="")
        {
            string sql;

            if (filter == "")
            {
                sql = "select * from Tab_Energieanlagen order by Bezeichner";
            }
            else sql = "select * from Tab_Energieanlagen where " + filter;
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new WErzeugerModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                WErzeugerModel item = new WErzeugerModel();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Bezeichner = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.ID_Type = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.ID_WP = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Betriebsart = (string)DBReader.GetString(5);
                if (!DBReader.IsDBNull(6)) item.Sperrung = (bool)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.Sperrzeit_von = (int)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.Sperrzeit_bis = (int)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.Vorlauf = (int)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.Ruecklauf = (int)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.Bivalenter_Betrieb = (bool)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) item.Abschaltpunkt = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) item.Nutzungszeit = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.ID_SP = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) item.ID_PV = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) item.ID_Solar = (int)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) item.Heizstab = (bool)DBReader.GetValue(17);
                if (!DBReader.IsDBNull(18)) item.Volumen = (double)DBReader.GetValue(18);
                if (!DBReader.IsDBNull(19)) item.rendeMix = (bool)DBReader.GetValue(19);
                if (!DBReader.IsDBNull(20)) item.Solaranteil = (int)DBReader.GetValue(20);
                if (!DBReader.IsDBNull(21)) item.ID_Kessel = (int)DBReader.GetValue(21);
                if (!DBReader.IsDBNull(22)) item.ID_BHKW = (int)DBReader.GetValue(22);
                if (!DBReader.IsDBNull(23)) item.Grenzleistung = (double)DBReader.GetValue(23);

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
                if (!DBReader.IsDBNull(0)) ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) Bezeichner = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) ID_Type = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) ID_WP = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) Betriebsart = (string)DBReader.GetString(5);
                if (!DBReader.IsDBNull(6)) Sperrung = (bool)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) Sperrzeit_von = (int)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) Sperrzeit_bis = (int)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) Vorlauf = (int)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) Ruecklauf = (int)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) Bivalenter_Betrieb = (bool)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) Abschaltpunkt = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) Nutzungszeit = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) ID_SP = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) ID_PV = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) ID_Solar = (int)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) Heizstab = (bool)DBReader.GetValue(17);
                if (!DBReader.IsDBNull(18)) Volumen = (double)DBReader.GetValue(18);
                if (!DBReader.IsDBNull(19)) rendeMix = (bool)DBReader.GetValue(19);
                if (!DBReader.IsDBNull(20)) Solaranteil = (int)DBReader.GetValue(20);
                if (!DBReader.IsDBNull(21)) ID_Kessel = (int)DBReader.GetValue(21);
                if (!DBReader.IsDBNull(22)) ID_BHKW = (int)DBReader.GetValue(22);
                if (!DBReader.IsDBNull(23)) Grenzleistung = (double)DBReader.GetValue(23);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public DataSet GetDataSetFromAdapter(DataSet dataSet, string queryString)
        {
            //using (OdbcConnection connection = Program.DBConnection)
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter(queryString, Program.DBConnection);
              //  connection.Open();
                // Open the connection and fill the DataSet.
                try
                {
                   // connection.Open();
                    adapter.Fill(dataSet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                // The connection is automatically closed when the
                // code exits the using block.
            }
            return dataSet;
        }

    }
}
