using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class WizardCtrl
    {
        public bool speichern;
        public string Projektname;
        public string Klimazone;
        public WizardParent parentform;

        public WizardCtrl()
        {
            speichern = false;
            Projektname = "";
            Klimazone = "";
        }

        public bool Add_WP_Waermeerzeuger(int projektID, List<WErzeugerModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Energieanlagen", Program.DBConnection);
                DataSet dataSet = new DataSet();
                WErzeugerCtrl ctrl = new WErzeugerCtrl();

                adapter.Fill(dataSet, "Tab_Energieanlagen");
                int maxID = ctrl.GetMaxID() + 1;

                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Tab_Energieanlagen"].NewRow();
                    newRow["ID"] = maxID++;
                    newRow["ID_Projekt"] = projektID;
                    newRow["Bezeichner"] = list[i].Bezeichner;
                    newRow["Betriebsart"] = list[i].Betriebsart;
                    newRow["Sperrung"] = list[i].Sperrung;
                    newRow["Sperrzeit_von"] = list[i].Sperrzeit_von;
                    newRow["Sperrzeit_bis"] = list[i].Sperrzeit_bis;
                    newRow["Vorlauf"] = list[i].Vorlauf;
                    newRow["Rücklauf"] = list[i].Ruecklauf;
                    newRow["Bivalenter_Betrieb"] = list[i].Bivalenter_Betrieb;
                    newRow["Abschaltpunkt"] = list[i].Abschaltpunkt;
                    newRow["Nutzungszeit"] = list[i].Nutzungszeit;

                    newRow["ID_WP"] = DBNull.Value;
                    newRow["ID_SP"] = DBNull.Value;
                    newRow["ID_PV"] = DBNull.Value;
                    newRow["ID_KESSEL"] = DBNull.Value;
                    newRow["ID_Type"] = list[i].ID_Type;
                    
                    if (list[i].ID_Type == WizardItemClass.WP_TYP || list[i].ID_Type == WizardItemClass.REF_WP_TYP)
                    {
                        newRow["ID_WP"] = list[i].ID_WP;
                    }
                    else if (list[i].ID_Type == WizardItemClass.SOLAR_TYP || list[i].ID_Type == WizardItemClass.REF_SOLAR_TYP)
                    {
                        newRow["ID_Solar"] = list[i].ID_Solar;
                    }
                    else if (list[i].ID_Type == WizardItemClass.PV_TYP || list[i].ID_Type == WizardItemClass.REF_PV_TYP)
                    {
                        newRow["ID_PV"] = list[i].ID_PV;
                    }
                    else if (list[i].ID_Type == WizardItemClass.SP_TYP || list[i].ID_Type == WizardItemClass.REF_SP_TYP)
                    {
                        newRow["ID_SP"] = list[i].ID_SP;
                    }
                    else if (list[i].ID_Type == WizardItemClass.KESSEL_TYP || list[i].ID_Type == WizardItemClass.REF_KESSEL_TYP)
                    {
                        newRow["ID_KESSEL"] = list[i].ID_Kessel;
                    }

                    newRow["Heizstab"] = list[i].Heizstab;
                    newRow["Volumen"] = list[i].Volumen;
                    newRow["rendeMix"] = list[i].rendeMix;
                    newRow["Solaranteil"] = list[i].Solaranteil;

                    dataSet.Tables["Tab_Energieanlagen"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Tab_Energieanlagen");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Del_Projekt_Waermeerzeuger(int projektID)
        {
            OdbcTransaction transaction = null;

            ExecuteTransaction(Program.DBConnection.ConnectionString, projektID );
            return true;
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Energieanlagen where ID_Projekt=" + projektID, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Tab_Energieanlagen");

                transaction = Program.DBConnection.BeginTransaction(); // Transaktion starten
                adapter.SelectCommand.Transaction = transaction;

                for (int i = 0; i < dataSet.Tables["Tab_Energieanlagen"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Tab_Energieanlagen"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Tab_Energieanlagen");
                
                transaction.Commit();  
                
                return true;
            }
            catch (Exception ex)
            {
                if(transaction != null)
                    transaction.Rollback();
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public static void ExecuteTransaction(string connectionString, int projektID)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                OdbcCommand command = new OdbcCommand();
                OdbcTransaction transaction = null;

                // Set the Connection to the new OdbcConnection.
                command.Connection = connection;

                // Open the connection and execute the transaction.
                try
                {
                    connection.Open();

                    OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Energieanlagen where ID_Projekt=" + projektID, Program.DBConnection);
                    adapter.SelectCommand.Transaction = transaction;
                    
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, "Tab_Energieanlagen");

                    // Start a local transaction
                    transaction = connection.BeginTransaction();

                    // Assign transaction object for a pending local transaction.
                    //command.Connection = connection;
                    command.Transaction = transaction;

                    for (int i = 0; i < dataSet.Tables["Tab_Energieanlagen"].Rows.Count; i++)
                    {
                        DataRow row = dataSet.Tables["Tab_Energieanlagen"].Rows[i];
                        row.Delete();
                    }
                    OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                    adapter.Update(dataSet, "Tab_Energieanlagen");

                    // Commit the transaction.
                    transaction.Commit();
                    Console.WriteLine("Both records are written to database.");
                    connection.Close(); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    try
                    {
                        // Attempt to roll back the transaction.
                        transaction.Rollback();
                    }
                    catch
                    {
                        // Do nothing here; transaction is not active.
                    }
                }
                // The connection is automatically closed when the
                // code exits the using block.
            }
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektGebaeude where ID_Projekt=" + projektID, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Z_ProjektGebaeude");

                for (int i = 0; i < dataSet.Tables["Z_ProjektGebaeude"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Z_ProjektGebaeude"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektGebaeude");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Del_Projekt_ZuordungGebäude(int projektID, int ID)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektGebaeude where ID_Projekt=" + projektID + " and ID=" + ID, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Z_ProjektGebaeude");

                for (int i = 0; i < dataSet.Tables["Z_ProjektGebaeude"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Z_ProjektGebaeude"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektGebaeude");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Add_Projekt_ZuordungGebäude(int projektID, List<Z_ProjGebModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektGebaeude", Program.DBConnection);
                DataSet dataSet = new DataSet();

                adapter.Fill(dataSet, "Z_ProjektGebaeude");
 
                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Z_ProjektGebaeude"].NewRow();
                    newRow["ID"] = 1;
                    newRow["ID_Projekt"] = projektID;
                    newRow["ID_Gebaeude"] = list[i].ID_Gebaeude;
                    newRow["Wohnflaeche_Waermebedarf"] = list[i].Wohnflaeche;
                    newRow["Einheit_Waermebedarf_Wohnflaeche"] = list[i].Einheit;
                    newRow["Jahresnutzungsgrad"] = list[i].Jahresnutzungsgrad;
                    newRow["dezWarmwasserbereitung"] = list[i].DezentralWarmwasser;

                    dataSet.Tables["Z_ProjektGebaeude"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektGebaeude");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Del_Projekt_ID_Waermeerzeuger(int projektID, int ID_Waermeerzeuger)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Energieanlagen where ID_Projekt=" + projektID + " and ID=" + ID_Waermeerzeuger, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Tab_Energieanlagen");

                for (int i = 0; i < dataSet.Tables["Tab_Energieanlagen"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Tab_Energieanlagen"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Tab_Energieanlagen");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Del_Projekt_Waermeerzeuger(int projektID, int nType)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Energieanlagen where ID_Projekt=" + projektID + " and ID_Type=" + nType, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Tab_Energieanlagen");

                for (int i = 0; i < dataSet.Tables["Tab_Energieanlagen"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Tab_Energieanlagen"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Tab_Energieanlagen");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Add_SP(int projektID, List<StromspeicherModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Energieanlagen", Program.DBConnection);
                DataSet dataSet = new DataSet();
                WErzeugerCtrl ctrl = new WErzeugerCtrl();

                adapter.Fill(dataSet, "Tab_Energieanlagen");
                int maxID = ctrl.GetMaxID() + 1;

                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Tab_Energieanlagen"].NewRow();
                    newRow["ID"] = maxID++;
                    newRow["ID_Projekt"] = projektID;
                    newRow["Bezeichner"] = list[i].m_szBezeichner;
                    newRow["ID_Type"] = 4;
                    newRow["ID_WP"] = DBNull.Value;
                    newRow["Betriebsart"] = DBNull.Value;
                    newRow["Sperrung"] = DBNull.Value;
                    newRow["Sperrzeit_von"] = DBNull.Value;
                    newRow["Sperrzeit_bis"] = DBNull.Value;
                    newRow["Vorlauf"] = DBNull.Value;
                    newRow["Rücklauf"] = DBNull.Value;
                    newRow["Bivalenter_Betrieb"] = DBNull.Value;
                    newRow["Abschaltpunkt"] = DBNull.Value;
                    newRow["Nutzungszeit"] = DBNull.Value;
                    newRow["ID_SP"] = list[i].m_ID;
                    newRow["ID_PV"] = DBNull.Value;
                    newRow["ID_Solar"] = DBNull.Value;
                    newRow["Heizstab"] = DBNull.Value;
                    newRow["Volumen"] = DBNull.Value;
                    newRow["rendeMix"] = DBNull.Value;
                    newRow["Solaranteil"] = DBNull.Value;

                    dataSet.Tables["Tab_Energieanlagen"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Tab_Energieanlagen");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Add_Projekt(int projektID, ProjektModel model)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Projekt", Program.DBConnection);
                DataSet dataSet = new DataSet();
                ProjektCtrl ctrl = new ProjektCtrl();

                adapter.Fill(dataSet, "Tab_Projekt");

                DataRow newRow = dataSet.Tables["Tab_Projekt"].NewRow();

                newRow["ID"] = projektID;
                newRow["Projektname"] = model.m_szProjektname;
                newRow["Bearbeiter"] = model.m_szBearbeiter;
                newRow["Beschreibung"] = model.m_szBeschreibung;
                newRow["Kunde"] = model.m_szKunde;
                newRow["Aenderungsdatum"] = model.m_Aenderungsdatum;
                newRow["ID_Klimaregion"] = model.m_ID_Klimaregion;

                dataSet.Tables["Tab_Projekt"].Rows.Add(newRow);

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Tab_Projekt");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Update_Projekt(int projektID, ProjektModel model)
        {
            try
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle("select * from Tab_Projekt where ID=" + projektID);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.m_ID_Klimaregion = model.m_ID_Klimaregion;
                projctrl.m_szBearbeiter = model.m_szBearbeiter;
                projctrl.m_szKunde = model.m_szKunde;
                projctrl.m_szBeschreibung = model.m_szBeschreibung;
                projctrl.Update();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Add_WaermebedarfExtern(int projektID, List<Z_ProjWaermebedarfModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektWaermebedarf", Program.DBConnection);
                DataSet dataSet = new DataSet();

                adapter.Fill(dataSet, "Z_ProjektWaermebedarf");

                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Z_ProjektWaermebedarf"].NewRow();
                    newRow["ID_Z"] = 1;
                    newRow["ID_Projekt"] = projektID;
                    newRow["ID_Ganglinie"] = list[i].m_ID_Ganglinie;
                    newRow["Bezeichner"] = list[i].m_szBezeichner;  

                    dataSet.Tables["Z_ProjektWaermebedarf"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektWaermebedarf");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Del_WaermebedarfExtern(int projektID)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektWaermebedarf where ID_Projekt=" + projektID, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Z_ProjektWaermebedarf");

                for (int i = 0; i < dataSet.Tables["Z_ProjektWaermebedarf"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Z_ProjektWaermebedarf"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektWaermebedarf");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Del_Projekt_Prozess(int projektID, int ID=0)
        {
            try
            {
                string sql;
                if (ID > 0)
                {
                    sql = "select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + projektID + " and ID=" + ID;
                }
                else sql = "select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + projektID;

                OdbcDataAdapter adapter = new OdbcDataAdapter(sql, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Z_Projekt_Prozesswaerme");

                for (int i = 0; i < dataSet.Tables["Z_Projekt_Prozesswaerme"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Z_Projekt_Prozesswaerme"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_Projekt_Prozesswaerme");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Add_Projekt_Prozess(int projektID, List<Z_ProjektProzesswaermeModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_Projekt_Prozesswaerme", Program.DBConnection);
                DataSet dataSet = new DataSet();

                adapter.Fill(dataSet, "Z_Projekt_Prozesswaerme");

                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Z_Projekt_Prozesswaerme"].NewRow();
                    newRow["ID"] = list[i].ID_Z;
                    newRow["ID_Projekt"] = projektID;
                    newRow["ID_Prozesswaerme"] = list[i].ID_Prozesswaerme;
                    newRow["Bezeichner"] = list[i].szProzessname;  
                    dataSet.Tables["Z_Projekt_Prozesswaerme"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_Projekt_Prozesswaerme");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Del_Projekt_Stromverbraucher(int projektID, int ID = 0)
        {
            try
            {
                string sql;
                if (ID > 0)
                {
                    sql = "select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + projektID + " and ID=" + ID;
                }
                else sql = "select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + projektID;

                OdbcDataAdapter adapter = new OdbcDataAdapter(sql, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Z_Projekt_Stromverbraucher");

                for (int i = 0; i < dataSet.Tables["Z_Projekt_Stromverbraucher"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Z_Projekt_Stromverbraucher"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_Projekt_Stromverbraucher");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Add_Projekt_Stromverbraucher(int projektID, List<Z_ProjektStromverbraucherModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_Projekt_Stromverbraucher", Program.DBConnection);
                DataSet dataSet = new DataSet();

                adapter.Fill(dataSet, "Z_Projekt_Stromverbraucher");

                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Z_Projekt_Stromverbraucher"].NewRow();
                    newRow["ID"] = list[i].m_ID_Z;
                    newRow["ID_Projekt"] = projektID;
                    newRow["ID_Stromverbraucher"] = list[i].m_ID_Stromverbraucher;
                    newRow["Bezeichner"] = list[i].m_szVerbraucher; 

                    dataSet.Tables["Z_Projekt_Stromverbraucher"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_Projekt_Stromverbraucher");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }

        }

        public bool Add_Stromganglinie(int projektID, List<Z_ProjektStromganglinieModel> list)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektStromganglinie", Program.DBConnection);
                DataSet dataSet = new DataSet();

                adapter.Fill(dataSet, "Z_ProjektStromganglinie");

                for (int i = 0; i < list.Count; i++)
                {
                    DataRow newRow = dataSet.Tables["Z_ProjektStromganglinie"].NewRow();
                    newRow["ID_Z"] = 1;
                    newRow["ID_Projekt"] = projektID;
                    newRow["ID_Ganglinie"] = list[i].m_ID_Stromganglinie;
                    newRow["Bezeichner"] = list[i].m_szStromganglinie;

                    dataSet.Tables["Z_ProjektStromganglinie"].Rows.Add(newRow);
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektStromganglinie");

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        public bool Del_Stromganglinie(int projektID)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Z_ProjektStromganglinie where ID_Projekt=" + projektID, Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "Z_ProjektStromganglinie");

                for (int i = 0; i < dataSet.Tables["Z_ProjektStromganglinie"].Rows.Count; i++)
                {
                    DataRow row = dataSet.Tables["Z_ProjektStromganglinie"].Rows[i];
                    row.Delete();
                }
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "Z_ProjektStromganglinie");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }
    }
}
