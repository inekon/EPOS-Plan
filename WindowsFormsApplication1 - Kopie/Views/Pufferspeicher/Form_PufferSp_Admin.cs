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
    public partial class Form_PufferSp_Admin : Form
    {
        private PufferSpCtrl ctrl = new PufferSpCtrl();
        public int m_ID_Projekt = 0;
        public bool m_bReadOnly = false;

        public Form_PufferSp_Admin ()
        {
            InitializeComponent();
            listBox_PufferSp_DB.Items.Clear();
        }

        private void Form_PufferSp_Admin_Load(object sender, EventArgs e)
        {
            LoadDBPufferSp();

            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                if (comboBox_Hersteller.FindStringExact(ctrl.items[i].Firma) == -1) comboBox_Hersteller.Items.Add(ctrl.items[i].Firma);
            }

            comboBox_Volumen.Items.Add("Alle");
            comboBox_Volumen.Items.Add("bis 100 l");
            comboBox_Volumen.Items.Add(">100 bis 200 l");
            comboBox_Volumen.Items.Add(">200 bis 500 l");
            comboBox_Volumen.Items.Add(">500 bis 1.000 l");
            comboBox_Volumen.Items.Add("über 1.000 l");
            comboBox_Volumen.Text = "Alle";
            comboBox_Hersteller.Text = "Alle";   

            if(m_bReadOnly)
            {
                btn_Neu.Enabled = false;
                btn_Bearbeiten.Enabled = false;
                btn_Loeschen.Enabled = false;
            }   
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterVolumen = "";
            string sql = "";

            szFilterVolumen = "";
            if (comboBox_Volumen.Text == "Alle" || comboBox_Volumen.Text == "") szFilterVolumen = "Gesamtvolumen Like '%'";
            else if (comboBox_Volumen.Text == "bis 100 l") szFilterVolumen = "Gesamtvolumen <100";
            else if (comboBox_Volumen.Text == ">100 bis 200 l") szFilterVolumen = "Gesamtvolumen >=100 and Gesamtvolumen <200";
            else if (comboBox_Volumen.Text == ">200 bis 500 l") szFilterVolumen = "Gesamtvolumen >=200 and Gesamtvolumen <500";
            else if (comboBox_Volumen.Text == ">500 bis 1.000 l") szFilterVolumen = "Gesamtvolumen >=500 and Gesamtvolumen <1000";
            else if (comboBox_Volumen.Text == "über 1.000 l") szFilterVolumen = "Gesamtvolumen >=1000";

            if (comboBox_Hersteller.Text == "Alle" || comboBox_Hersteller.Text == "") szFilter = "Hersteller Like '%'";
            else szFilter = "Hersteller='" + comboBox_Hersteller.Text + "'";

            listBox_PufferSp_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from Tab_Pufferspeicher where " + szFilterVolumen + " order by Name";
            else
                sql = "select * from Tab_Pufferspeicher where " + szFilter + " and " + szFilterVolumen + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_PufferSp_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            PufferSpCtrl ctrl = new PufferSpCtrl();
            if(listBox_PufferSp_DB.Text == "") return;    
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_PufferSp_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            ctrl.Delete(listBox_PufferSp_DB.Text);
            listBox_PufferSp_DB.Items.Remove(listBox_PufferSp_DB.Text); 
        }

        private void LoadDBPufferSp()
        {
            listBox_PufferSp_DB.Items.Clear();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_PufferSp_DB.Items.Add(ctrl.items[i].Name);
            }
        }

        private void comboBox_Hersteller_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Volumen_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void listBox_PufferSp_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Pufferspeicher where Bezeichner='" + listBox_PufferSp_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Hersteller.Text = rs.GetString("Hersteller");
                textBox_Typ.Text = (string)rs.Read("Speichertyp");
                textBox_Versluste.Text = rs.Read("Bereitschaftsverluste").ToString();
                textBox_Volumen.Text = rs.Read("Gesamtvolumen").ToString();
                textBox_Investitionskosten.Text = rs.Read("Investitionskosten").ToString();
            }
            rs.Close();
        }

        private void textBox_Versluste_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Bearbeiten frm = new Form_PufferSp_Bearbeiten(Form_PufferSp_Bearbeiten.MODE_EDIT);
            if (listBox_PufferSp_DB.Text == "") return;
            frm.SetControls(listBox_PufferSp_DB.Text);
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szPufferSp;
                LoadDBPufferSp();
                listBox_PufferSp_DB.Text = szKessel;
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Bearbeiten frm = new Form_PufferSp_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_NEU);
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();

            Point p1 = btn_Neu.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;

            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {
                RecordSet rs = new RecordSet();
                rs.Open("select Bezeichner from Tab_Pufferspeicher where Bezeichner='" + frmLabel.m_szName + "'");
                bool bExist = !rs.EOF();
                rs.Close();

                if (bExist)
                {
                    MessageBox.Show("Name existiert bereits!");
                }
                else
                {
                    frm.SetControls(frmLabel.m_szName);

                    DialogResult ret = frm.ShowDialog();
                    if (ret == DialogResult.OK)
                    {
                        string szKessel = frm.m_szPufferSp;
                        LoadDBPufferSp();
                        listBox_PufferSp_DB.Text = szKessel;
                    }
                }
            }
        }
    }
}
