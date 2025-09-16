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
    public partial class Form_ErgProzesswaerme : Form
    {
        SimulationGebaeude gebaeude_simulation;

        public Form_ErgProzesswaerme()
        {
            InitializeComponent();
        }

        public void Init(SimulationGebaeude simulation)
        {
            gebaeude_simulation = simulation;

            textBox_WB_Gebaeude.Text = simulation.Waermebedarf_Gebaeude_Gesamt.ToString("F2");
            textBox_WB_Extern.Text = simulation.Waermebedarf_Extern_Gesamt.ToString("F2");
            textBox_MaxWaermelast.Text = simulation.Waermebedarf_Max.ToString("F2");
            textBox_Netzverluste.Text = simulation.Waermebedarf_Netzverluste.ToString("F2");
            textBox_WB_Gesamt.Text = simulation.Waermebedarf_Gesamt.ToString("F2");
            textBox_WB_Prozess.Text = simulation.Waermebedarf_Prozess.ToString("F2");
            Monat_1.Text = simulation.Waermebedarf_monat[0].ToString("F2");
            Monat_2.Text = simulation.Waermebedarf_monat[1].ToString("F2");
            Monat_3.Text = simulation.Waermebedarf_monat[2].ToString("F2");
            Monat_4.Text = simulation.Waermebedarf_monat[3].ToString("F2");
            Monat_5.Text = simulation.Waermebedarf_monat[4].ToString("F2");
            Monat_6.Text = simulation.Waermebedarf_monat[5].ToString("F2");
            Monat_7.Text = simulation.Waermebedarf_monat[6].ToString("F2");
            Monat_8.Text = simulation.Waermebedarf_monat[7].ToString("F2");
            Monat_9.Text = simulation.Waermebedarf_monat[8].ToString("F2");
            Monat_10.Text = simulation.Waermebedarf_monat[9].ToString("F2");
            Monat_11.Text = simulation.Waermebedarf_monat[10].ToString("F2");
            Monat_12.Text = simulation.Waermebedarf_monat[11].ToString("F2");

            chart1.Series[0].Points.DataBindY(simulation.Waermebedarf_monat);   
        }

        public void SetPage(int page)
        {
            tabControl1.SelectedIndex = page; 
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}