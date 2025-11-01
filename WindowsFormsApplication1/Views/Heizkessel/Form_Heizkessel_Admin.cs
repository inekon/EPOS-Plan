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
    public partial class Form_Heizkessel_Admin : Form
    {
        private BrennstoffCtrl heizkesselctrl = new BrennstoffCtrl();
        public int m_ID_Projekt = 0;
  
        public Form_Heizkessel_Admin()
        {
            InitializeComponent();
            listBox_Kessel_DB.Items.Clear();
        }

        private void Form_Heizkessel_Load(object sender, EventArgs e)
        {
            LoadDBHeizkessel();

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

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesselbeschreibung.Text = rs.GetString("Beschreibung");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
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

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            BrennstoffCtrl ctrl = new BrennstoffCtrl();
            if(listBox_Kessel_DB.Text == "") return;    
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Kessel_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            ctrl.Delete(listBox_Kessel_DB.Text);
            listBox_Kessel_DB.Items.Remove(listBox_Kessel_DB.Text); 
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Bearbeiten frm = new Form_Heizkessel_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_EDIT);
            if(listBox_Kessel_DB.Text == "") return;
            frm.SetControls(listBox_Kessel_DB.Text, textBox_Kesselbeschreibung.Text);
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szKessel;
                LoadDBHeizkessel();
                listBox_Kessel_DB.Text = szKessel;
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Bearbeiten frm = new Form_Heizkessel_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_NEU);
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();

            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {
                RecordSet rs = new RecordSet();
                rs.Open("select Name from [DB-Heizung] where Name='" + frmLabel.m_szName + "'");
                if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); return; }
                rs.Close();
                frm.SetControls(frmLabel.m_szName, "");
                
                DialogResult ret = frm.ShowDialog();
                if (ret == DialogResult.OK)
                { 
                    string szKessel = frm.m_szKessel;
                    LoadDBHeizkessel();
                    listBox_Kessel_DB.Text = szKessel;
                }
            }
        }

        private void textBox_Kesselleistung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void LoadDBHeizkessel()
        {
            listBox_Kessel_DB.Items.Clear();
            heizkesselctrl.ReadAll();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);
            }
        }
    }
}
