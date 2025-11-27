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
    public partial class Form_WPEing : Form
    {
        public List<WErzeugerModel> list_wpmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SP_TYP;
        public int m_ID_Projekt = 0;

        public Form_WPEing ()
        {
            InitializeComponent();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void Form_WPEing_Load(object sender, EventArgs e)
        {
            WPCtrl ctrl = new WPCtrl();

            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_WP_DB.Items.Add(ctrl.items[i].WPName); 
            }

        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        public void SetControls(string projekt)
        {
            listBox_WPEing.Items.Clear();
            for (int i = 0; i < list_wpmodel.Count; i++)
            {
                listBox_WPEing.Items.Add(list_wpmodel[i].Bezeichner);
            }
            if (listBox_WPEing.Items.Count > 0) listBox_WPEing.SelectedIndex = 0;
        }

        private void listBox_WP_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_WP where WPName='" + listBox_WP_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_WPName.Text = (string)rs.Read("WPName");
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Leistung_th.Text = rs.Read("Nennleistung").ToString();
                textBox_Leistung_el.Text = rs.Read("Heizung").ToString();
           //     textBox_Betriebsart.Text = (string)rs.Read("Betriebsart");
                textBox_Firma.Text = (string)rs.Read("Firma");
            }
            rs.Close();

        }
    }
}