using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace WindowsFormsApplication1
{
    partial class Wizard_Waermebedarf : Form
    {
        public List<Z_ProjWaermebedarfModel> list_wbmodel = new List<Z_ProjWaermebedarfModel>();
        private Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        private ToolsClass tool = new ToolsClass();
        string filename;
        string filebasename;

        public Wizard_Waermebedarf()
        {
            InitializeComponent();
 
            WaermebedarfCtrl ctrl = new WaermebedarfCtrl();
            ctrl.ReadAll(); 
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Extern.Items.Add(ctrl.items[i].m_szBezeichner);
            }
        }

        public void SetControls(string projekt, bool bWizard=false)
        {
            if (bWizard)
            {
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
            }

            m_szProjekt = projekt;
       
            listBox_Auswahl.Items.Clear();
            for (int n = 0; n < list_wbmodel.Count; n++)
            {
                Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                item.m_szBezeichner = list_wbmodel[n].m_szBezeichner;
                listBox_Auswahl.Items.Add(item.m_szBezeichner);
                m_ID_Projekt = list_wbmodel[n].m_ID_Projekt; 
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_Extern.Text == "") return;
            model.m_szBezeichner = listBox_Extern.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Waermebedarf where Bezeichner='" + listBox_Extern.Text + "'");
            if (!rs.EOF())
            {
                model.m_ID_Ganglinie = (int)rs.Read("ID");
                model.m_ID_Projekt = m_ID_Projekt;
            }
            rs.Close();

            list_wbmodel.Add(model);
            listBox_Auswahl.Items.Add(listBox_Extern.Text);
            if (listBox_Extern.Items.Count > 0) listBox_Extern.SelectedIndex = listBox_Extern.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.Text == "") return;
            model.m_szBezeichner = listBox_Auswahl.Text;
            for (int i = 0; i < list_wbmodel.Count; i++)
            {
                if (list_wbmodel[i].m_szBezeichner == listBox_Auswahl.Text)
                {
                    list_wbmodel.RemoveAt(i);
                    listBox_Auswahl.Items.Remove(listBox_Auswahl.Text);
                    break;
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
        }
        
        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
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
                    if (!File.Exists(szAppDataPath + "\\" + filebasename))
                    {
                        File.Copy(filename, szAppDataPath + "\\" + filebasename, true);
                    }
                    string szFile = szAppDataPath + "\\" + filebasename;

                    Einlesen();

                    WaermebedarfCtrl wbctrl = new WaermebedarfCtrl();
                    listBox_Extern.SelectedItems.Clear();
                    listBox_Extern.Items.Clear();
                    wbctrl.ReadAll();
                    for (int i = 0; i < wbctrl.rows; i++)
                    {
                        listBox_Extern.Items.Add(wbctrl.items[i].m_szBezeichner);
                    }

                }
                catch { }
            }
            openFileDialog = null;

        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;
            Close();
        }

        private void Einlesen()
        {
            WaermebedarfCtrl ctrl_ganglinie = new WaermebedarfCtrl();
            WaermebedarfDatenCtrl ctrl = new WaermebedarfDatenCtrl();

            // Datei schon eingelesen?
            if (listBox_Extern.FindString(Path.GetFileNameWithoutExtension(filebasename)) != ListBox.NoMatches)
            {

                Form_Hinweis frm = new Form_Hinweis("Hinweis", "Datei ist bereits eingelesen!");
                frm.Location = this.PointToScreen(btn_Bearbeiten.Location);
                frm.ShowDialog();
                return;
            }

            // Datei in Liste einlesen 
            if (!tool.OpenText(filename)) return;

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

            listBox_Extern.Items.Clear();
            listBox_Extern.SelectedItems.Clear();
            ctrl_ganglinie.ReadAll();
            for (int i = 0; i < ctrl_ganglinie.rows; i++)
            {
                listBox_Extern.Items.Add(ctrl_ganglinie.items[i].m_szBezeichner);
            }

        }
    }
}