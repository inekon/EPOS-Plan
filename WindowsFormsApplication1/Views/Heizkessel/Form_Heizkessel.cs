using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
            comboBox_Brennstoffart.Items.Add("Alle");
            comboBox_Brennstoffart.Items.Add("Gas");
            comboBox_Brennstoffart.Items.Add("Öl");
            comboBox_Brennstoffart.Items.Add("Gas Brennwert");
            comboBox_Brennstoffart.Items.Add("Öl Brennwert");
            comboBox_Brennstoffart.Items.Add("Biogas");
            comboBox_Brennstoffart.Items.Add("Rapsöl");
            comboBox_Brennstoffart.Items.Add("Holz");
            comboBox_Brennstoffart.Items.Add("Flüssiggas");
            comboBox_Brennstoffart.Items.Add("Sonstiges");
            comboBox_Brennstoffart.Items.Add("Fernwärme");
            comboBox_Brennstoffart.Items.Add("Strom");

            comboBox_Leistung.Items.Add("Alle");
            comboBox_Leistung.Items.Add("bis 50 kW");
            comboBox_Leistung.Items.Add(">50 bis 200 kW");
            comboBox_Leistung.Items.Add(">200 bis 500 kW");
            comboBox_Leistung.Items.Add(">500 bis 1.000 kW");
            comboBox_Leistung.Items.Add("über 1.000 kW");
            comboBox_Leistung.Text = "Alle";
            comboBox_Brennstoffart.Text = "Alle";   
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

        private void comboBox_Brennstoffart_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

            szFilterLeistung = "";
            if (comboBox_Leistung.Text == "Alle") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "bis 50 kW") szFilterLeistung = "Ptherm <50";
            else if (comboBox_Leistung.Text == ">50 bis 200 kW") szFilterLeistung = "Ptherm >=50 and Ptherm <200";
            else if (comboBox_Leistung.Text == ">200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == ">500 bis 1.000 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <1000";
            else if (comboBox_Leistung.Text == "über 1.000 kW") szFilterLeistung = "Ptherm >=1000";

            if (comboBox_Brennstoffart.Text == "Gas") szFilter = "Brennstoff=1 and Wirkungsgrad_Gas<1";
            else if (comboBox_Brennstoffart.Text == "Gas Brennwert") szFilter = "Brennstoff=1 and Wirkungsgrad_Gas>=1";
            else if (comboBox_Brennstoffart.Text == "Öl") szFilter = "Brennstoff=0 and Wirkungsgrad_Öl<1";
            else if (comboBox_Brennstoffart.Text == "Öl Brennwert") szFilter = "Brennstoff=0 and Wirkungsgrad_Öl>=1";
            else if (comboBox_Brennstoffart.Text == "Biogas") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoffart.Text == "Rapsöl") szFilter = "Brennstoff=3";
            else if (comboBox_Brennstoffart.Text == "Holz") szFilter = "Brennstoff=4";
            else if (comboBox_Brennstoffart.Text == "Flüssiggas") szFilter = "Brennstoff=8";
            else if (comboBox_Brennstoffart.Text == "Sonstiges") szFilter = "Brennstoff=5";
            else if (comboBox_Brennstoffart.Text == "Fernwärme") szFilter = "Brennstoff=14";
            else if (comboBox_Brennstoffart.Text == "Strom") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoffart.Text == "Alle") szFilter = "Brennstoff Like '%'";

            listBox_Kessel_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from [DB-Heizung] where " + szFilterLeistung + " order by Name";
            else
                sql = "select * from [DB-Heizung] where " + szFilter + " and " + szFilterLeistung + " order by Name";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_Kessel_DB.Items.Add((string)rs.Read("Name"));
            }
            rs.Close();
        }
    }
}
