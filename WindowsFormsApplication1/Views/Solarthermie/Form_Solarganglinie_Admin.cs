using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Solarganglinie_Admin : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        public List<SolarganglinieModel> DateiListe = new List<SolarganglinieModel>();
        private ToolsClass tool = new ToolsClass();
        string filename;
        string filebasename;

        public Form_Solarganglinie_Admin ()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
 
            string szPath = Path.Combine(Program.ApplicationPath_User, "Solarthermie");
            textBox_Ordner.Text = szPath;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;  
            Close();
        }

        public void SetControls()
        {
            SolarganglinieStammCtrl ctrl = new SolarganglinieStammCtrl();
            ctrl.ReadAll();

            listBox_Extern.Items.Clear();
            
            for(int i=0; i<ctrl.rows;i++)
            {
                SolarganglinieModel model = new SolarganglinieModel();

                model.m_szBezeichner = ctrl.items[i].m_szBezeichner;
                listBox_Extern.Items.Add(model.m_szBezeichner);
                DateiListe.Add(model);
            }

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "Solarthermie");
            textBox_Ordner.Text = szAppDataPath;
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void GetDateiInfo(string dateiname)
        {
            textBox_Name.Text = dateiname + ".txt";
            string szPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "Solarthermie");
            szPath = Path.Combine(szPath, dateiname);
        }

        private void btn_Oeffnen_Click(object sender, EventArgs e)
        {
            ToolsClass tool = new ToolsClass();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "Solarthermie");
            szAppDataPath = Path.Combine(szAppDataPath, textBox_Name.Text);
            tool.OpenFileWithDefaultApp(szAppDataPath);
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            SolarganglinieStammCtrl ctrl_ganglinie = new SolarganglinieStammCtrl();
            Z_ProjektSolarganglinieCtrl ctrl = new Z_ProjektSolarganglinieCtrl();
            ctrl.ReadAll("Select * from Z_ProjektSolarganglinie where Bezeichner ='" + listBox_Extern.Text + "'");
            if (ctrl.rows > 0)
            {
                MessageBox.Show("Es existiert eine Projektzuordnung, Löschen nicht möglich!");
                return;
            }

            ctrl_ganglinie.Delete(listBox_Extern.Text);
            SetControls(); 
        }

        private void btn_Datei_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = textBox_Ordner.Text;
            openFileDialog.Filter = "(*.txt)|*.txt";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);

                try
                {
                    string szQuelle = Path.Combine(textBox_Ordner.Text, filebasename);
                    if (!File.Exists(textBox_Ordner.Text + "\\" + filebasename))
                    {
                        File.Copy(filename, textBox_Ordner.Text + "\\" + filebasename, true);
                    }
                    textBox_Name.Text = textBox_Ordner.Text + "\\" + filebasename;
                }
                catch { }
            }
            openFileDialog = null;
        }

        private void btn_Einlesen_Click(object sender, EventArgs e)
        {
            SolarganglinieStammCtrl ctrl_stamm = new SolarganglinieStammCtrl();
            if (filebasename == "" || filebasename == null ) return;
            // Datei schon eingelesen?
            if (listBox_Extern.FindString(Path.GetFileNameWithoutExtension(filebasename)) != ListBox.NoMatches)
            {
                MessageBox.Show("Solarganglinie ist bereits in Datenbank vorhanden!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Datei in Liste einlesen 
            if (!tool.OpenText(textBox_Name.Text)) return;

            this.Cursor = Cursors.WaitCursor;

            // Kopf (Bezeichner + Beschreibung) und Datenwerte fuer den STAMM-Import aufbereiten
            string szBezeichner = Path.GetFileNameWithoutExtension(filebasename);
            string szBeschreibung = tool.textList.Count > 0 ? tool.textList[0] : "";
            List<string> werte = new List<string>();
            for (int i = 1; i < tool.textList.Count; i++) werte.Add(tool.textList[i]);

            bool success = ctrl_stamm.ImportGanglinie(szBezeichner, szBeschreibung, werte);

            this.Cursor = Cursors.Default;

            if (!success)
            {
                MessageBox.Show("Fehler beim Speichern der Ganglinie. Die Daten wurden nicht gespeichert.");
                return;
            }

            SetControls();
        }

    }
}