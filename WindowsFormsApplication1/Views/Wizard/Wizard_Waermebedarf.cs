using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace WindowsFormsApplication1
{
    partial class Wizard_Waermebedarf : Form
    {
        public List<Z_ProjWaermebedarfModel> DateiListe = new List<Z_ProjWaermebedarfModel>();
        private Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();
        WizardParent wizardparent;
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;

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

        public void SetControls(string projekt)
        {
            wizardparent = (WizardParent)getWizardPage();
            m_szProjekt = projekt;
            m_ID_Projekt = wizardparent.projektID;  

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                listBox_Auswahl.Items.Clear();

                for (int n = 0; n < wizardparent.list_wbmodel.Count; n++)
                {
                    Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                    item.m_szBezeichner = wizardparent.list_wbmodel[n].m_szBezeichner;
                    listBox_Auswahl.Items.Add(item.m_szBezeichner);
                }
                if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
            }
        }

        private Form getWizardPage()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "WizardParent")
                {
                    return form;
                }
            }
            return null;
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

            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_wbmodel.Add(model);
            listBox_Auswahl.Items.Add(listBox_Extern.Text);
            if (listBox_Extern.Items.Count > 0) listBox_Extern.SelectedIndex = listBox_Extern.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.Text == "") return;
            model.m_szBezeichner = listBox_Auswahl.Text;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_wbmodel.Count; i++)
            {
                if (wizardparent.list_wbmodel[i].m_szBezeichner == listBox_Auswahl.Text)
                {
                    wizardparent.list_wbmodel.RemoveAt(i);
                    listBox_Auswahl.Items.Remove(listBox_Auswahl.Text);
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
        }

        private void listBox_Extern_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Extern.Text;
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Auswahl.Text;
        }


        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            WaermebedarfCtrl wbctrl = new WaermebedarfCtrl();

            listBox_Auswahl.SelectedItems.Clear();
            listBox_Extern.SelectedItems.Clear();
            ctrl.WaermebedarfExtern();
            listBox_Extern.Items.Clear();
            wbctrl.ReadAll();
            for (int i = 0; i < wbctrl.rows; i++)
            {
                listBox_Extern.Items.Add(wbctrl.items[i].m_szBezeichner);
            }
        }
    }
}