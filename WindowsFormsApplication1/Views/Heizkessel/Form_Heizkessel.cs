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
    public partial class Form_Heizkessel : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private BrennstoffCtrl heizkesselctrl = new BrennstoffCtrl();
        public List<WErzeugerModel> list_heizkesselmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SP_TYP;
        public int m_ID_Projekt = 0;
        int startindex = 100000;

        public Form_Heizkessel()
        {
            InitializeComponent();
            listBox_Kessel_DB.Items.Clear();
            listBox_Kessel.Items.Clear();
        }

        public void SetControls(string projekt)
        {
            listBox_Kessel.Items.Clear();
            for (int i = 0; i < list_heizkesselmodel.Count; i++)
            {
                listBox_Kessel.Items.Add(list_heizkesselmodel[i].Bezeichner);
            }
            if (listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = 0;
        }

        private void Form_Heizkessel_Load(object sender, EventArgs e)
        {
            heizkesselctrl.ReadAll();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);
            }
        }

        private void listBox_Kessel_SelectedIndexChanged(object sender, EventArgs e)
        {

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

        private void btn_Kessel_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
           
            if (listBox_Kessel_DB.Text == "") return;

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");
            if (rs.Next())
            {
                WErzeugerModel model = new WErzeugerModel();
                model.ID = startindex++;
                model.ID_Projekt = m_ID_Projekt;
                model.ID_Kessel = (int)rs.Read("ID");
                model.ID_Type = m_nType;
                model.Bezeichner = listBox_Kessel_DB.Text;

                list_heizkesselmodel.Add(model);
            }
            rs.Close();

            listBox_Kessel.Items.Add(listBox_Kessel_DB.Text);
            //if (listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = listBox_Kessel.Items.Count - 1;
        }

        private void btn_Kessel_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel.SelectedIndex == -1) return;
            list_heizkesselmodel.RemoveAt(listBox_Kessel.SelectedIndex);
            listBox_Kessel.Items.RemoveAt(listBox_Kessel.SelectedIndex); 
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBox_Kessel_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
            }
            rs.Close();
        }

        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
            }
            rs.Close();
        }
    }
}
