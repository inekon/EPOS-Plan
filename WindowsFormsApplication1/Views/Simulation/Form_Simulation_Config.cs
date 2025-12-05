using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Config : Form
    {
        public KonfigurationModel Konfiguration = new KonfigurationModel();
        int m_ID_Projekt;

        public Form_Simulation_Config()
        {
            InitializeComponent();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedIndex != -1) { checkBox1.Checked = true; }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex != -1) { checkBox2.Checked = true; }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex != -1) { checkBox3.Checked = true; }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex != -1) { checkBox4.Checked = true; }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedIndex != -1) { checkBox5.Checked = true; }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedIndex != -1) { checkBox6.Checked = true; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) { comboBox1.Text = ""; }    
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox2.Checked) { comboBox2.Text = ""; }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox3.Checked) { comboBox3.Text = ""; }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox4.Checked) { comboBox4.Text = ""; }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox5.Checked) { comboBox5.Text = ""; }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox6.Checked) { comboBox6.Text = ""; }
        }

        public void SetControls(int ID_Projekt)
        {
            m_ID_Projekt = ID_Projekt;
            comboBox1.Text = Konfiguration.m_Tool_1;
            comboBox2.Text = Konfiguration.m_Tool_2;
            comboBox3.Text = Konfiguration.m_Tool_3;
            comboBox4.Text = Konfiguration.m_Tool_4;
            comboBox5.Text = Konfiguration.m_Tool_5;
            comboBox6.Text = Konfiguration.m_Tool_6;
            checkBox_Heizstab.Checked = Konfiguration.m_WP_Heizstab;
            textBox_Netzverluste.Text = Konfiguration.m_Netzverluste.ToString();
            comboBox_NetzvEinheit.Text = Konfiguration.m_szNetzverlusteEinheit;    
            textBox_untere_PGrenze.Text = Konfiguration.m_BHKW_Grenzleistung.ToString();
            comboBox_Bereitschaft.Text = Konfiguration.m_Kessel_Betriebsbereitschaft.ToString();
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            Konfiguration.m_Tool_1 = comboBox1.Text;
            Konfiguration.m_Tool_2 = comboBox2.Text;
            Konfiguration.m_Tool_3 = comboBox3.Text;
            Konfiguration.m_Tool_4 = comboBox4.Text;
            Konfiguration.m_Tool_5 = comboBox5.Text;
            Konfiguration.m_Tool_6 = comboBox6.Text;
            Konfiguration.m_WP_Heizstab = checkBox_Heizstab.Checked;
            Konfiguration.m_Netzverluste = double.Parse(textBox_Netzverluste.Text);
            Konfiguration.m_szNetzverlusteEinheit = comboBox_NetzvEinheit.Text;
            Konfiguration.m_BHKW_Grenzleistung = double.Parse(textBox_untere_PGrenze.Text);
            Konfiguration.m_Kessel_Betriebsbereitschaft = Int32.Parse(comboBox_Bereitschaft.Text);

            ctrl.model = Konfiguration; 
            if(!ctrl.Delete(m_ID_Projekt)) return;
            if(ctrl.Insert(m_ID_Projekt)) MessageBox.Show("Datensatz gespeichert");
        }
    }
}
