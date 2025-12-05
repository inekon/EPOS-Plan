using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Kurz : Form
    {
        public int m_ID_Projekt;
        public double m_Waermebedarf_Gesamt;
        public double m_Strombedarf_Gesamt;


        public Form_Simulation_Kurz (int iD_Projekt)
        {
            InitializeComponent();
            m_ID_Projekt = iD_Projekt;

            chart5.Legends[0].LegendStyle = LegendStyle.Table;
            chart5.Legends[0].Docking = Docking.Right;
            chart5.Legends[0].Alignment = StringAlignment.Center;
            chart5.Legends[0].Title = "Wärmebedarfsdeckung";
            chart5.Legends[0].BorderColor = Color.Green;
            chart5.Series[0].IsValueShownAsLabel = false;
            chart5.Series[0]["PieLabelStyle"] = "Outside";
            chart5.Series[0].Points.Clear();
        }

        public void SetControls()
        {   
 
        }   

        private void btn_Konfiguration_Click(object sender, EventArgs e)
        {
            Form_Simulation_Config frm = new Form_Simulation_Config();
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            frm.Konfiguration = ctrl.model;
            frm.SetControls(m_ID_Projekt);
            System.Drawing.Point p1 = btn_Konfiguration.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            frm.ShowDialog();
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
            SimulationStrombedarf simulation_Strombedarf = new SimulationStrombedarf();
            ProjektCtrl projektCtrl = new ProjektCtrl();

            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            if (ctrl.rows == 0)
            {
                MessageBox.Show("Bitte zuerst die Konfiguration festlegen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string[] tool = new string[6];
            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;
            tool[4] = ctrl.model.m_Tool_5;
            tool[5] = ctrl.model.m_Tool_6;

            int netzverluste = (int)ctrl.model.m_Netzverluste;

            projektCtrl.ReadSingle("select * from Tab_Projekt where ID=" + m_ID_Projekt);   
            int nKlimaregion = projektCtrl.m_ID_Klimaregion;
            if (nKlimaregion == 0)
            {
                MessageBox.Show("Klimaregion auswählen!");
                return;
            }
            simulation_Waermebedarf.Netzverluste = netzverluste;
            simulation_Waermebedarf.Netzverluste_Einheit = ctrl.model.m_szNetzverlusteEinheit;
            simulation_Waermebedarf.Waermebedarf_berechnen(m_ID_Projekt, nKlimaregion);
            simulation_Strombedarf.m_ID_Projekt = m_ID_Projekt; 
            simulation_Strombedarf.Berechnung(m_ID_Projekt);

            //Do_Simulation( ctrl, tool, simulation_Waermebedarf, simulation_Strombedarf);

            SimulationHelper sim = new SimulationHelper();
            sim.tool = tool;
            sim.Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            sim.simulation_Waermebedarf = simulation_Waermebedarf;
            sim.simulation_Strombedarf = simulation_Strombedarf;
            sim.ctrl_konfig = ctrl;

            textBox_gesStrombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");
            textBox_gesWaermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            sim.Do_Simulation(m_ID_Projekt);

            textBox_Restwaermebedarf.Text = sim.Restwaerme.ToString("F2");
            textBox_Reststrombedarf.Text = sim.Reststrom.ToString("F2");
            textBox_WPWaermeproduktion.Text = (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
            textBox_WPStromverbrauch.Text = (sim.simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
            textBox_SPKWaermeproduktion.Text = sim.simulation_spk.S_Waerme_spk.ToString("F2");
            textBox_HeizstabStromverbrauch.Text = (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
            textBox_SPKStromverbrauch.Text = sim.simulation_spk.Stromverbrauch_Spk.ToString("F2");

            chart5.Series[0].Points.Clear();
            if (sim.simulation_wp.WP_Waermeproduktion_gesamt > 0)
                chart5.Series[0].Points.AddXY("WP", sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000);
            if (sim.simulation_wp.Heizstab_gesamt > 0)
                chart5.Series[0].Points.AddXY("Heizstab", sim.simulation_wp.Heizstab_gesamt / 1000);
            if (sim.simulation_spk.S_Waerme_spk > 0)
                chart5.Series[0].Points.AddXY("Kessel", sim.simulation_spk.S_Waerme_spk);
            if (sim.Restwaerme > 0)
                chart5.Series[0].Points.AddXY("Rest",sim.Restwaerme);

        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
