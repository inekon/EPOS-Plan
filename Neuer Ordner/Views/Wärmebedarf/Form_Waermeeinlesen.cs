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
    partial class Form_Waermeeinlesen : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        public List<WaermebedarfModel> DateiListe = new List<WaermebedarfModel>();
        private ToolsClass tool = new ToolsClass();
        string filename;
        string filebasename;

        public Form_Waermeeinlesen()
        {
            InitializeComponent();
 
            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            textBox_Ordner.Text = szPath;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;  
            Close();
        }

        public void SetControls()
        {
            WaermebedarfCtrl ctrl = new WaermebedarfCtrl();
            ctrl.ReadAll();

            listBox_Extern.Items.Clear();
            
            for(int i=0; i<ctrl.rows;i++)
            {
                WaermebedarfModel model = new WaermebedarfModel(); 

                model.m_szBezeichner = ctrl.items[i].m_szBezeichner;
                listBox_Extern.Items.Add(model.m_szBezeichner);
                DateiListe.Add(model);
            }

            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
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
            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            szPath = Path.Combine(szPath, dateiname);
        }

        private void btn_Oeffnen_Click(object sender, EventArgs e)
        {
            ToolsClass tool = new ToolsClass();

            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            szAppDataPath = Path.Combine(szAppDataPath, textBox_Name.Text);
            tool.OpenFileWithDefaultApp(szAppDataPath);
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            WaermebedarfCtrl ctrl_ganglinie = new WaermebedarfCtrl();
            Z_ProjektGebGanglinieCtrl ctrl = new Z_ProjektGebGanglinieCtrl();
            ctrl.ReadAll("Select * from Z_ProjektWaermebedarf where Bezeichner ='" + listBox_Extern.Text + "'");
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
            WaermebedarfCtrl ctrl_ganglinie = new WaermebedarfCtrl();
            WaermebedarfDatenCtrl ctrl = new WaermebedarfDatenCtrl();

            // Datei schon eingelesen?
            if (listBox_Extern.FindString(Path.GetFileNameWithoutExtension(filebasename)) != ListBox.NoMatches) return;

            // Datei in Liste einlesen 
            if (!tool.OpenText(textBox_Name.Text)) return;

            this.Cursor = Cursors.WaitCursor;

            // Datensatz in DB Tab_Waermebedarf anlegen
            ctrl_ganglinie.m_szBezeichner = Path.GetFileNameWithoutExtension(filebasename);
            if (!ctrl_ganglinie.Insert()) return;

            // Daten in DB
            ctrl.list_GanglinieDaten.Clear();
            for (int i = 0; i < tool.textList.Count; i++)
            {
                WaermebedarfDatenModel model = new WaermebedarfDatenModel();
                model.m_ID_GanglinieDaten = ctrl_ganglinie.m_ID_Ganglinie;
                model.m_Wert = double.Parse(tool.textList[i]);
                ctrl.list_GanglinieDaten.Add(model);
            }
            ctrl.Insert();

            this.Cursor = Cursors.Default;
            SetControls();
        }

    }
}