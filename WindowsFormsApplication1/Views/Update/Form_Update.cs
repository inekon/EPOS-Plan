using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Update : Form
    {
        string filename;
        string filebasename;    

        public Form_Update()
        {
            InitializeComponent();
        }

        private void btn_DB_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "";
            openFileDialog.Filter = "(*.accdb)|*.accdb";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);
                textBox_DB.Text = filename;
             
            }
            openFileDialog = null;
        }

        private async void btn_Update_Click(object sender, EventArgs e)
        {
            btn_Update.Enabled = false;
            progressBar1.Visible = true;

            string connString = @"Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=" + textBox_DB.Text;
            string targetConnStr = Program.DBConnection.ConnectionString;

            string[] tabellenNamen = { 
                "Tab_Einstellungen", "Tab_Solar","DB-Heizung","DBGebaeude", "DBTagV","DBTagVDaten","Tab_Klimaregion","Tab_Klimadaten","Tab_Projekt","Tab_Applikation",
                "Tab_BHKW","Tab_Einstellungen","Tab_Typ_Energieanlagen","Tab_WP",
                "Tab_Kenndaten","Tab_Kenndaten_Kuehlung","Tab_Prozesstyp","Tab_Prozesswaerme",
                "Tab_Simulation_Ergebnis","Tab_Solarganglinie","Tab_SolarganglinieDaten","Tab_Solarkollektoren","Tab_Stromganglinie",
                "Tab_StromganglinieDaten","Tab_Stromspeicher","Tab_Stromverbrauchertyp","Tab_Stromverbraucher",
                "Tab_Waermebedarf","Tab_WaermebedarfDaten",
                "Tab_Energieanlagen", "Z_Projekt_Prozesswaerme","Z_Projekt_Stromverbraucher",
                "Z_ProjektGebaeude","Z_ProjektSolarganglinie","Z_ProjektSolarkollektoren","Z_ProjektStromganglinie","Z_ProjektWaermebedarf"
            };

            string[] tabellenNamen_delete = { 
                "Tab_Projekt",
                "Tab_Prozesswaerme",
                "Tab_Prozesstyp",
                "Tab_Klimaregion",
                "Tab_Applikation",
                "Tab_Einstellungen",
                "Tab_Kenndaten",
                "DBTagV",
                "Tab_WP",
                "Tab_BHKW",
                "Tab_Waermebedarf",
                "Tab_Typ_Energieanlagen",
                "Tab_Simulation_Ergebnis",
                "Tab_Solar",
                "Tab_Solarganglinie",
                "Tab_Solarkollektoren",
                "DBGebaeude",
                "DB-Heizung",
                "Tab_Stromganglinie",
                "Tab_Stromspeicher",
                "Tab_Stromverbraucher",
                "Tab_Stromverbrauchertyp"
            };

            int totalSteps = tabellenNamen_delete.Length + tabellenNamen.Length;
            progressBar1.Maximum = totalSteps;
            progressBar1.Value = 0;

            var progress = new Progress<int>(val =>
            {
                // Fortschrittsanzeige - absolute abgeschlossene Schritte
                if (val >= 0 && val <= progressBar1.Maximum)
                    progressBar1.Value = val;
            });

            try
            {
                // Schwere Arbeit außerhalb des UI-Threads ausführen
                await Task.Run(() =>
                {
                    int done = 0;

                    // Löschphase
                    foreach (string tableName in tabellenNamen_delete)
                    {
                        if ((tableName == "Tab_Solar" || 
                             tableName == "DB-Heizung" ||
                             tableName == "DBGebaeude" ||
                             tableName == "Tab_Stromganglinie" ||
                             tableName == "Tab_Waermebedarf"
                             ) && !checkBox_Stammdaten.Checked)
                        {
                            // nichts
                        }
                        else
                        {
                            string sql = $"DELETE FROM [{tableName}]"; // SQL zum Leeren der Tabelle
                            using (OdbcConnection targetConn = new OdbcConnection(targetConnStr))
                            {
                                targetConn.Open();
                                OdbcCommand cmd = new OdbcCommand(sql, targetConn);

                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    // Wiederwerfen zur Behandlung im aufrufenden Kontext
                                    throw new Exception($"Fehler beim Löschen aus '{tableName}': {ex.Message}", ex);
                                }
                            }
                        }
                        done++;
                        (progress as IProgress<int>)?.Report(done);
                    }

                    // Importphase
                    for (int i = 0; i < tabellenNamen.Length; i++)
                    {
                        if ((tabellenNamen[i] == "Tab_Solar" ||
                             tabellenNamen[i] == "DB-Heizung" ||
                             tabellenNamen[i] == "DBGebaeude" ||
                             tabellenNamen[i] == "Tab_Stromganglinie" ||
                             tabellenNamen[i] == "Tab_StromganglinieDaten" ||
                             tabellenNamen[i] == "Tab_Waermebedarf" ||
                             tabellenNamen[i] == "Tab_WaermebedarfDaten"
                             ) && !checkBox_Stammdaten.Checked)
                        {
                            // nichts
                        }
                        else
                        {
                            // Bestehende ImportData verwenden (führt DB-Operationen außerhalb des UI-Threads aus)
                            ImportData(connString, targetConnStr, tabellenNamen[i]);
                        }
                        done++;
                        (progress as IProgress<int>)?.Report(done);
                    }
                });
            }
            catch (Exception ex)
            {
                // Fehler im UI-Thread anzeigen
                MessageBox.Show("Fehler beim Import: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar1.Value = 0;
                progressBar1.Visible = false;
                btn_Update.Enabled = true;
            }
        }

        public static void ImportData(string sourceConnStr, string targetConnStr, string QuellTabelle)
        {
            DataTable dt = new DataTable(QuellTabelle);

            // 1. Daten aus der Quell-Datenbank lesen
            using (OdbcConnection sourceConn = new OdbcConnection(sourceConnStr))
            {
                string selectSql = "SELECT * FROM [" + QuellTabelle + "]";
                OdbcCommand cmd = new OdbcCommand(selectSql, sourceConn);
                sourceConn.Open();
                dt.Load(cmd.ExecuteReader()); // Lädt Daten in die DataTable
            }

            // 2. Daten in die Ziel-Datenbank schreiben
            using (OdbcConnection targetConn = new OdbcConnection(targetConnStr))
            {
                string cols = "";
                string parameters = "";

                targetConn.Open();

                foreach (DataColumn col in dt.Columns)
                {
                    cols += "[" + col.ColumnName + "],";
                    parameters += "?,"; 
                }
                
                if (cols.Length == 0) return;
                
                cols = cols.Substring(0, cols.Length - 1);
                parameters = parameters.Substring(0, parameters.Length - 1);
                string insertSql = "INSERT INTO [" + QuellTabelle + "] (" + cols + ") VALUES (" + parameters + ")";

                using (OdbcCommand insertCmd = new OdbcCommand(insertSql, targetConn))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        // Parameter verwenden, um SQL-Injection zu vermeiden
                        insertCmd.Parameters.Clear();
                        int i = 0;
                        foreach (DataColumn col in dt.Columns)
                        {
                            insertCmd.Parameters.AddWithValue("?", row[i++]);
                        }

                        insertCmd.ExecuteNonQuery();
                    }
                }


            }
        }

    }
}
