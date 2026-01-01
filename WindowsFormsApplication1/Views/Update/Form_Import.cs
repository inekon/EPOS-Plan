using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Import : Form
    {
        string filename;
        string filebasename;
        string dbPath;
        string connString;
        string targetConnStr = Program.DBConnection.ConnectionString;

        public Form_Import()
        {
            InitializeComponent();
            object p = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\ODBC\\ODBC.ini\\TEST", "DBQ", "");
            if(p != null)
            {
                dbPath = p.ToString();
            }
            else dbPath = "";
            filename = "";
            filebasename = "";
            connString = "";
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
            DbClass dbClass = new DbClass();
            
            connString = @"Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=" + textBox_DB.Text;

            string[] tabellenNamen_import = {
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

            if(textBox_DB.Text == "")
            {
                MessageBox.Show("Bitte wählen Sie eine Datenbankdatei aus!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }   

            btn_Update.Enabled = false;
            progressBar1.Visible = true;
            progressBar1.Step = 1;
            connString = @"Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=" + textBox_DB.Text;

            dbClass.tabellenNamen_import = tabellenNamen_import;
            dbClass.tabellenNamen_delete = tabellenNamen_delete;

            bool updateSucceeded = await dbClass.UpdateDatabaseAsync(connString, targetConnStr, progressBar1);
            progressBar1.Visible = false;

            if (updateSucceeded)
            {
                MessageBox.Show("Datenbank Update erfolgreich abgeschlossen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Datenbank Update fehlgeschlagen.\n" + dbClass.szError, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btn_Update.Enabled = true;
        }
        
        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
