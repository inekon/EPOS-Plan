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
    public partial class Wizard_Kessel : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private BrennstoffCtrl spctrl = new BrennstoffCtrl();

        public Wizard_Kessel()
        {
            InitializeComponent();
            listBox_Kessel_DB.Items.Clear();
            listBox_Kessel.Items.Clear();
        }

        private void Wizard_Kessel_Load(object sender, EventArgs e)
        {
            spctrl.ReadAll();
            for (int i = 0; i < spctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(spctrl.items[i].Name);
            }
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel_DB.Text == "") return;
            model.Bezeichner = listBox_Kessel_DB.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                model.ID_Kessel = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass.KESSEL_TYP; 
            }
            rs.Close();
 
            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_werzmodel.Add(model);
            listBox_Kessel.Items.Add(listBox_Kessel_DB.Text);
            if (listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = listBox_Kessel.Items.Count-1;
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

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel.Text == "") return;
            model.Bezeichner = listBox_Kessel.Text;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_werzmodel.Count; i++)
            {
                if (wizardparent.list_werzmodel[i].Bezeichner == listBox_Kessel.Text && wizardparent.list_werzmodel[i].ID_Type == WizardItemClass.KESSEL_ITEM)
                {
                    wizardparent.list_werzmodel.RemoveAt(i);
                    listBox_Kessel.Items.Remove(listBox_Kessel.Text);
                }
            }
            if(listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = 0;
        }

        private void listBox_Kessel_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
            }
            rs.Close();
        }

        public void SetControls(string projekt)
        {
            WizardParent wizardparent = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                listBox_Kessel.Items.Clear();

                for (int n = 0; n < wizardparent.list_werzmodel.Count; n++)
                {
                    WErzeugerModel item = new WErzeugerModel();

                    if (wizardparent.list_werzmodel[n].ID_Type == WizardItemClass.KESSEL_TYP)
                    {
                        item.Bezeichner = wizardparent.list_werzmodel[n].Bezeichner;
                        listBox_Kessel.Items.Add(item.Bezeichner);
                    }
                }
                if (listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = 0;
            }
        }

        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
            }
            rs.Close();
        }
    }
}
