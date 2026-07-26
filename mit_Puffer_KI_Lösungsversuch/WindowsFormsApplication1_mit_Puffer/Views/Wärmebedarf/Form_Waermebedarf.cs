using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Waermebedarf : Form
    {
        public List<Z_ProjWaermebedarfModel> list_wbmodel = new List<Z_ProjWaermebedarfModel>();
        private Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        private ToolsClass tool = new ToolsClass();
        string filename = "";
        string filebasename = "";

        public Form_Waermebedarf()
        {
            InitializeComponent();
 
            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
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

            rs.Open("select * from Tab_Waermebedarf_STAMM where Bezeichner='" + listBox_Extern.Text + "'");
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
            Form_AdminWaermeeinlesen frm = new Form_AdminWaermeeinlesen();
            frm.SetControls();
            frm.ShowDialog();
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

            // Import in die STAMM-Tabellen (Kopf + Daten)
            WaermebedarfStammCtrl ctrl_stamm = new WaermebedarfStammCtrl();
            ctrl_stamm.ImportGanglinie(Path.GetFileNameWithoutExtension(filebasename), tool.textList);

            this.Cursor = Cursors.Default;
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            WaermebedarfStammCtrl ctrl_ganglinie = new WaermebedarfStammCtrl();
            Z_ProjektGebGanglinieCtrl ctrl = new Z_ProjektGebGanglinieCtrl();
            ctrl.ReadAll("Select * from Z_ProjektWaermebedarf where Bezeichner ='" + listBox_Extern.Text + "'");
            if (ctrl.rows > 0)
            {
                MessageBox.Show("Es existiert eine Projektzuordnung, Löschen nicht möglich!");
                return;
            }

            // Delete prueft selbst auf ReadOnly und meldet ggf.
            if (!ctrl_ganglinie.Delete(listBox_Extern.Text)) return;

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
