using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    partial class Form_Stromganglinie_Admin : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        public List<StromganglinieModel> DateiListe = new List<StromganglinieModel>();
        private ToolsClass tool = new ToolsClass();
        string filename = "";
        string filebasename = "";
        string szAppDataPath = "";

        public Form_Stromganglinie_Admin ()
        {
            InitializeComponent();

            szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Strom");
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;  
            Close();
        }

        public void SetControls()
        {
            StromganglinieCtrl ctrl = new StromganglinieCtrl();
            ctrl.ReadAll();

            listBox_Extern.Items.Clear();
            
            for(int i=0; i<ctrl.rows;i++)
            {
                StromganglinieModel model = new StromganglinieModel(); 

                model.m_szBezeichner = ctrl.items[i].m_szBezeichner;
                listBox_Extern.Items.Add(model.m_szBezeichner);
                DateiListe.Add(model);
            }

            szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Strom");
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            StromganglinieCtrl ctrl_ganglinie = new StromganglinieCtrl();
            Z_ProjektStromganglinieCtrl ctrl = new Z_ProjektStromganglinieCtrl();
            
            ctrl.ReadAll("Select * from Z_ProjektStromganglinie where Bezeichner ='" + listBox_Extern.Text + "'");
            if (ctrl.rows > 0)
            {
                MessageBox.Show("Es existiert eine Projektzuordnung, Löschen nicht möglich!");
                return;
            }

            ctrl_ganglinie.Delete(listBox_Extern.Text);
            SetControls(); 
        }

        private void btn_Einlesen_Click(object sender, EventArgs e)
        {
            StromganglinieCtrl ctrl_ganglinie = new StromganglinieCtrl();
            StromganglinieDatenCtrl ctrl = new StromganglinieDatenCtrl();
            int Zeitinterval = 0;

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.txt)|*.txt";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);

                try
                {
                    string szQuelle = Path.Combine(szAppDataPath, filebasename);
                    if (!File.Exists(szQuelle))
                    {
                        File.Copy(filename, szQuelle, true);
                    }
                }
                catch { }
            }
            openFileDialog = null;


            if (filebasename == "" || filebasename == null ) return;
            
            // Datei schon eingelesen?
            if (listBox_Extern.FindString(Path.GetFileNameWithoutExtension(filebasename)) != ListBox.NoMatches)
            {
                MessageBox.Show("Stromganglinie ist bereits in Datenbank vorhanden!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Datei in Liste einlesen 
            if (!tool.OpenText(szAppDataPath + "\\" + filebasename)) return;

            // Anzahl Daten prüfen 
            if (comboBox_Zeitinterval.Text == "Stundenwerte") Zeitinterval = 1;
            else if (comboBox_Zeitinterval.Text == "1/4 Stundenwerte") Zeitinterval = 4;
            else if (comboBox_Zeitinterval.Text == "Minutenwerte") Zeitinterval = 60;
      
            if (comboBox_Zeitinterval.Text == "Stundenwerte" && tool.textList.Count != 8760)
            {
                MessageBox.Show("Anzahl der Werte stimmt nicht mit dem Zeitinterval überin!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (comboBox_Zeitinterval.Text == "1/4 Stundenwerte" && tool.textList.Count != 8760*4)
            {
                MessageBox.Show("Anzahl der Werte stimmt nicht mit dem Zeitinterval überin!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (comboBox_Zeitinterval.Text == "Minutenwerte" && tool.textList.Count != 8760*60)
            {
                MessageBox.Show("Anzahl der Werte stimmt nicht mit dem Zeitinterval überin!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Datensatz in DB Tab_Stromganglinie anlegen
            ctrl_ganglinie.m_szBezeichner = Path.GetFileNameWithoutExtension(filebasename);
            ctrl_ganglinie.m_Zeitinterval = Zeitinterval; // 1=Stundenwerte, 4=1/4 Stundenwerte, 60=Minutenwerte  

            // Daten in DB schreiben
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                // Wir übergeben das Kopf-Controller-Objekt (für den ersten Insert) 
                // und die rohe Textliste (für das performante Parsen und Einfügen)
                bool success = ctrl.InsertKompletteGanglinie(ctrl_ganglinie, tool.textList);

                if (!success)
                {
                    MessageBox.Show("Fehler beim Speichern der Ganglinie. Die Daten wurden nicht gespeichert.");
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            SetControls();
        }

    }
}