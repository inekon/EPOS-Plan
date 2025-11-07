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
    public partial class Wizard_Prozess : Form
    {
        ProzesswaermeCtrl prozctrl = new ProzesswaermeCtrl();
        private Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();
        WizardParent wizardparent;
        private int m_ID_Projekt = 0;

        public Wizard_Prozess ()
        {
            InitializeComponent();
        }

        public void SetControls(string projekt)
        {
            wizardparent  = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                listBox_SP.Items.Clear();

                for (int n = 0; n < wizardparent.list_prozmodel.Count; n++)
                {
                    Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();

                    item.szProzessname = wizardparent.list_prozmodel[n].szProzessname;
                    listBox_SP.Items.Add(item.szProzessname);
                    m_ID_Projekt = wizardparent.list_prozmodel[n].ID_Projekt;
                }
                if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;
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

        private void Wizard_Prozess_Load(object sender, EventArgs e)
        {
          prozctrl.ReadAll();
            for (int i = 0; i < prozctrl.rows; i++)
            {
                listBox_SP_DB.Items.Add(prozctrl.items[i].m_szProzessname);
            }
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_SP_DB.Text == "") return;
            model.szProzessname = listBox_SP_DB.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Prozesswaerme where Prozessname='" + listBox_SP_DB.Text + "'");
            if (!rs.EOF())
            {
                model.ID_Prozesswaerme = (int)rs.Read("ID");
                model.ID_Projekt = m_ID_Projekt;
            }
            rs.Close();

            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_prozmodel.Add(model);
            listBox_SP.Items.Add(listBox_SP_DB.Text);
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = listBox_SP.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_SP.Text == "") return;
            model.szProzessname = listBox_SP.Text;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_prozmodel.Count; i++)
            {
                if (wizardparent.list_prozmodel[i].szProzessname == listBox_SP.Text)
                {
                    wizardparent.list_prozmodel.RemoveAt(i);
                    listBox_SP.Items.Remove(listBox_SP.Text);
                }
            }
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;
        }

        private void listBox_SP_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_SP_DB.Text;
            SetBeschreibung(listBox_SP_DB.Text); 
        }

        private void listBox_SP_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_SP.Text;
            SetBeschreibung(listBox_SP.Text); 
        }

        private void SetBeschreibung(string Prozessname)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Prozesswaerme where Prozessname='" + Prozessname + "'");
            if (rs.Next())
            {
                textBox_Beschreibung.Text = "";
                textBox_Typ.Text = ""; 
                try
                {
                    textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                    textBox_Typ.Text = (string)rs.Read("Typ");
                }
                catch (SystemException ex) { Console.Write(ex.Message); }
            }
            rs.Close();
        }
    }
}
