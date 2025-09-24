using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Wizard_Stromlastgang : Form
    {
        StromganglinieCtrl stromgangctrl = new StromganglinieCtrl();
        private Z_ProjektStromganglinieModel model = new Z_ProjektStromganglinieModel();
        WizardParent wizardparent;
        private int m_ID_Projekt = 0;

        public Wizard_Stromlastgang ()
        {
            InitializeComponent();
        }

        public void SetControls(string projekt)
        {
            wizardparent = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                listBox_Auswahl.Items.Clear();

                for (int n = 0; n < wizardparent.list_stromlastmodel.Count; n++)
                {
                    Z_ProjektStromganglinieCtrl item = new Z_ProjektStromganglinieCtrl();

                    item.m_szStromganglinie = wizardparent.list_stromlastmodel[n].m_szStromganglinie;
                    listBox_Auswahl.Items.Add(item.m_szStromganglinie);
                    m_ID_Projekt = wizardparent.list_stromlastmodel[n].m_ID_Projekt;
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

        private void Wizard_Stromlastgang_Load(object sender, EventArgs e)
        {
            stromgangctrl.ReadAll();
            for (int i = 0; i < stromgangctrl.rows; i++)
            {
                listBox_Extern.Items.Add(stromgangctrl.items[i].m_szBezeichner);
            }
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_Extern.Text == "") return;
            model.m_szStromganglinie = listBox_Extern.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromganglinie where Bezeichner='" + listBox_Extern.Text + "'");
            if (!rs.EOF())
            {
                model.m_ID_Stromganglinie = (int)rs.Read("ID");
                model.m_ID_Projekt = m_ID_Projekt;
            }
            rs.Close();

            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_stromlastmodel.Add(model);
            listBox_Auswahl.Items.Add(listBox_Extern.Text);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = listBox_Auswahl.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.Text == "") return;
            model.m_szStromganglinie = listBox_Auswahl.Text;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_stromlastmodel.Count; i++)
            {
                if (wizardparent.list_stromlastmodel[i].m_szStromganglinie == listBox_Auswahl.Text)
                {
                    wizardparent.list_stromlastmodel.RemoveAt(i);
                    listBox_Auswahl.Items.Remove(listBox_Auswahl.Text);
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
        }

        private void listBox_SP_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Extern.Text;
        }

        private void listBox_SP_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Auswahl.Text;
        }
         
    }
}
