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
        public List<WaermebedarfExternModel> DateiListe = new List<WaermebedarfExternModel>();
        public bool m_bAdmin = false;

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

        private void Form_Waermeeinlesen_Load(object sender, EventArgs e)
        {

        }

        public void SetControls(string szProjekt)
        {
            WaermebedarfExternCtrl ctrl = new WaermebedarfExternCtrl();
            ctrl.ReadAll(m_ID_Projekt);

            listBox_Auswahl.Items.Clear();
            listBox_Extern.Items.Clear();
            
            for(int i=0; i<ctrl.rows;i++)
            {
                WaermebedarfExternModel model = new WaermebedarfExternModel(); 

                model.m_szDateiname = Path.GetFileName(ctrl.items[i].m_szDateiname);
                listBox_Auswahl.Items.Add(model.m_szDateiname);
                DateiListe.Add(model);
            }

            string[] wbeDateien = Directory.GetFiles(Path.Combine(Program.ApplicationPath_User, "Waermebedarf"), "*.txt");

            foreach (string dateiPfad in wbeDateien)
            {
                string dateiName = Path.GetFileName(dateiPfad);
                listBox_Extern.Items.Add(dateiName);
            }

            if (m_bAdmin)
            {
                listBox_Auswahl.Enabled = false;
                btn_Hinzufuegen.Enabled = false;
                btn_Entfernen.Enabled = false;
            }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_Hinzufuegen_Click(object sender, EventArgs e)
        {
            WaermebedarfExternModel model = new WaermebedarfExternModel();
            
            if(listBox_Extern.SelectedIndex >= 0)
            {
                model.m_szDateiname = listBox_Extern.Text;
                DateiListe.Add(model);   
                listBox_Auswahl.Items.Add(listBox_Extern.Text);
            }
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            WaermebedarfExternModel model = new WaermebedarfExternModel();

            if (listBox_Auswahl.SelectedIndex >= 0)
            {
                for (int i = 0; i < DateiListe.Count; i++)
                {
                    if (DateiListe[i].m_szDateiname == listBox_Auswahl.Text)
                    {
                        DateiListe.RemoveAt(i);
                        listBox_Auswahl.Items.RemoveAt(listBox_Auswahl.SelectedIndex);
                        break;
                    }
                }
            }
        }

        private void listBox_Extern_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetDateiInfo(listBox_Extern.Text);
        }

        private void GetDateiInfo(string dateiname)
        {
            textBox_Name.Text = dateiname;
            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            szPath = Path.Combine(szPath, dateiname);
            DateTime dt = File.GetCreationTime(szPath);
            textBox_Datum.Text = dt.ToShortDateString();
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetDateiInfo(listBox_Auswahl.Text);
        }

        private void btn_Oeffnen_Click(object sender, EventArgs e)
        {
            ToolsClass tool = new ToolsClass();

            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            szPath = Path.Combine(szPath, textBox_Name.Text);
            tool.OpenFileWithDefaultApp(szPath);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            
            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            
            openFileDialog.InitialDirectory = szPath;
            openFileDialog.Filter = "(*.txt)|*.txt";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filename = openFileDialog.FileName;
                string filebasename = System.IO.Path.GetFileName(filename);
    
                try
                {
                    szPath = Path.Combine(textBox_Ordner.Text, filebasename);
                    File.Copy(filename, szPath, true);
                }
                catch {}
                SetControls("");
            }
            openFileDialog = null;
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
            szPath = Path.Combine(szPath, listBox_Extern.Text);
            try
            {
                File.Delete(szPath);
            }
            catch { };
            SetControls("");
        }

    }
}