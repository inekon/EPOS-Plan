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
    public partial class Wizard_Sp : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private StromspeicherCtrl spctrl = new StromspeicherCtrl();

        public Wizard_Sp()
        {
            InitializeComponent();
            listBox_SP_DB.Items.Clear();
            listBox_SP.Items.Clear();
        }

        private void Wizard_Sp_Load(object sender, EventArgs e)
        {
            spctrl.ReadAll();
            for (int i = 0; i < spctrl.rows; i++)
            {
                listBox_SP_DB.Items.Add(spctrl.items[i].m_szBezeichner);
            }
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_SP_DB.Text == "") return;
            model.Bezeichner = listBox_SP_DB.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + listBox_SP_DB.Text + "'");
            if (!rs.EOF())
            {
                model.ID_SP = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass.SP_TYP; 
            }
            rs.Close();
 
            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_werzmodel.Add(model);
            listBox_SP.Items.Add(listBox_SP_DB.Text);
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = listBox_SP.Items.Count-1;
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
            if (listBox_SP.Text == "") return;
            model.Bezeichner = listBox_SP.Text;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_werzmodel.Count; i++)
            {
                if (wizardparent.list_werzmodel[i].Bezeichner == listBox_SP.Text)
                {
                    wizardparent.list_werzmodel.RemoveAt(i);
                    listBox_SP.Items.Remove(listBox_SP.Text);
                }
            }
            if(listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;
        }

        private void listBox_SP_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + listBox_SP.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Typ.Text = (string)rs.Read("Typ");
                textBox_Leistung.Text = rs.Read("Leistung").ToString();
                textBox_Energie.Text = rs.Read("Energie").ToString();
                textBox_Degradation.Text = rs.Read("Degradation").ToString(); ;
                textBox_Ladezustand.Text = rs.Read("Ladezustand").ToString(); ;
            }
            rs.Close();
        }

        public void SetControls(string projekt)
        {
            WizardParent wizardparent = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                listBox_SP.Items.Clear();

                for (int n = 0; n < wizardparent.list_werzmodel.Count; n++)
                {
                    WErzeugerModel item = new WErzeugerModel();

                    if (wizardparent.list_werzmodel[n].ID_Type == WizardItemClass.SP_TYP)
                    {
                        item.Bezeichner = wizardparent.list_werzmodel[n].Bezeichner;
                        listBox_SP.Items.Add(item.Bezeichner);
                    }
                }
                if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;
            }
        }

        private void textBox_Leistung_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Leistung, textBox_Leistung.Text)) { textBox_Leistung.Undo(); }
        }

        private void textBox_Energie_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Energie, textBox_Energie.Text)) { textBox_Energie.Undo(); }
        }

        private void textBox_Degradation_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Degradation, textBox_Degradation.Text)) { textBox_Degradation.Undo(); }
        }

        private void textBox_Ladezustand_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Ladezustand, textBox_Ladezustand.Text)) { textBox_Ladezustand.Undo(); }
        }
    }
}
