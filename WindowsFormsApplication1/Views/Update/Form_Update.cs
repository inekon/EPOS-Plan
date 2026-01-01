using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Update : Form
    {
        public bool updateSucceeded = false;    
        private DbClass dbClass = new DbClass();

        public Form_Update()
        {
            InitializeComponent();
        }

        public async void doUpdateDB()
        {
            textBox1.Text = "Datenbank Update...";
            textBox1.Refresh();
            string result = await UpdateDB();
   
            if (updateSucceeded)
            {
                textBox1.Text = result;
                textBox1.Refresh();  
                Thread.Sleep(5000);
                Close();
            }
        }

        public async Task<string> UpdateDB()
        {
            // duch das uodate wird die DB ins user appdata verzeichnis kopiert und in 'update.accdb' umbenannt
            // duch das uodate wird die installierte DB mit der neuen überschrieben
            // die Daten aus der gesicherten DB werden in die installierte DB importiert    

            string sourceConnString = @"Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=" + Program.ApplicationPath_User + "\\update.accdb";
            string result = "";
            string targetConnStr = Program.DBConnection.ConnectionString;

            string szIniFile = dbClass.GetIniFilePath();
            if (szIniFile == "")
            {
                //MessageBox.Show("Die Datenbank Update Datei wurde nicht gefunden!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = "Die gesicherte Datenbank wurde nicht gefunden!";
                textBox1.Text = result;
                return result;
            }

            IniFileParser ini = dbClass.ParseIniFile(szIniFile);
            // Tabellenstruktur ggf. in der gesicherten Datenbank aktualisieren
            // die zu aktualisierenden Tabellen aus der ini Datei ermitteln
            dbClass.GetUpdateTables(ini);

            if (!dbClass.UpdateTablesStructure(ini, sourceConnString))
            {
                //MessageBox.Show("Fehler beim Ausführen des Scripts:\n" + szError, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = "Fehler beim Ausführen des Scripts:\n" + dbClass.szError;
                textBox1.Text = result;
                return result;
            }

            updateSucceeded = await dbClass.UpdateDatabaseAsync(sourceConnString, targetConnStr,progressBar1);

            if (updateSucceeded)
            {
                //MessageBox.Show("Datenbank Update erfolgreich abgeschlossen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                result = "Datenbank Update erfolgreich abgeschlossen.";
                textBox1.Text = result;
            }
            else
            {
                //MessageBox.Show("Datenbank Update fehlgeschlagen.\n" + szError, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = "Datenimport nicht vollständig.\nDialog schließen damit die Anwendung gestartet wird!\n\n" + dbClass.szError;
                textBox1.Text = result;
            }
            File.Move(szIniFile, Program.ApplicationPath_User + "\\UpdateDB_" + DateTime.Now.ToString("yyyy-MM-dd") + ".ini");
            return result;
        }

        private void FormUpdate_Load(object sender, EventArgs e)
        {
            doUpdateDB();
        }
    }
}
