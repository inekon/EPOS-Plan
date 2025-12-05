using Microsoft.Office.Interop.Excel;
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
    public partial class Form_Simulation_Detail : Form
    {
        public SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
        public SimulationStrombedarf simulation_Strombedarf = new SimulationStrombedarf();
        SimulationHelper sim = new SimulationHelper();
        SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        KonfigurationCtrl ctrl = new KonfigurationCtrl();
        ProjektCtrl projektCtrl = new ProjektCtrl();

        public int m_ID_Projekt;
        public double m_Waermebedarf_Gesamt;
        public double m_Strombedarf_Gesamt;

        public Form_Simulation_Detail()
        {
            InitializeComponent();
            init_Chart(chart1);
            init_Chart(chart2); 
        }

        public Form_Simulation_Detail(int iD_Projekt)
        {
            InitializeComponent();
            m_ID_Projekt = iD_Projekt;
  
            init_Chart(chart1);
            init_Chart(chart2);

            chart5.Legends[0].LegendStyle = LegendStyle.Table;
            chart5.Legends[0].Docking = Docking.Right;
            chart5.Legends[0].Alignment = StringAlignment.Center;
            chart5.Legends[0].Title = "Wärmebedarfsdeckung";
            chart5.Legends[0].BorderColor = Color.Green;
            chart5.Series[0].IsValueShownAsLabel = false;
            chart5.Series[0]["PieLabelStyle"] = "Outside";
            chart5.Series[0].Points.Clear();

            listView_SimSPK.View = View.Details;
            listView_SimSPK.Columns.Add("Kessel", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Gas/Biogas/Rapsöl/Holz... [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Öl [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Jahresnutzungsgrad [%]", -2, HorizontalAlignment.Left);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

        }

        public void SetControls()
        {
        }
        
        private void init_Chart(System.Windows.Forms.DataVisualization.Charting.Chart chart)
        {
            var ca = chart.ChartAreas[0];
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;

            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;
            ca.CursorX.AutoScroll = true;
            ca.AxisX.ScrollBar.Enabled = true;

            chart.Series[0].BorderWidth = 2;
            chart.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorX.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorY.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorX.LineColor = Color.Red;
            chart.ChartAreas[0].CursorY.LineColor = Color.Red;
        }
        private void btn_StromDetails_Click(object sender, EventArgs e)
        {
            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation_Strombedarf);
            frm.ShowDialog();
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            m_Waermebedarf_Gesamt = simulation_Waermebedarf.Waermebedarf_Gesamt;
            m_Strombedarf_Gesamt = simulation_Strombedarf.Strombedarf_gesamt;
            textBox_gesStrombedarf.Text = m_Strombedarf_Gesamt.ToString("F2");
            textBox_gesWaermebedarf.Text = m_Waermebedarf_Gesamt.ToString("F2");

            // Konfiguration auslesen
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

            if (!Energiebedarf(ctrl.m_Netzverluste, ctrl.m_szNetzverlusteEinheit)) return;


            // Wärmebedarf und Strombedarf Simulation durchführen
            sim.tool = tool;
            sim.Stundentemperatur = simulation_Waermebedarf.Stundentemperatur;
            sim.simulation_Waermebedarf = simulation_Waermebedarf;
            sim.simulation_Strombedarf = simulation_Strombedarf;
            sim.ctrl_konfig = ctrl;

            textBox_gesStrombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");
            textBox_gesWaermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            // Tool Simulation WP, SPK usw. durchführen
            sim.Do_Simulation(m_ID_Projekt);

            Endergebiss_Simulation();
        }

        private bool Energiebedarf(double Netzverluste, string NetzverlusteEinheit)
        {
            int netzverluste = (int)ctrl.m_Netzverluste;
            if (ctrl.m_szNetzverlusteEinheit == "%" && netzverluste > 100)
            {
                MessageBox.Show("die Netzverluste dürfen nicht größer als 100 % sein!");
                return false;
            }

            projektCtrl.ReadSingle("select * from Tab_Projekt where ID=" + m_ID_Projekt);
            int nKlimaregion = projektCtrl.m_ID_Klimaregion;
            if (nKlimaregion == 0)
            {
                MessageBox.Show("Klimaregion auswählen!");
                return false;
            }

            // Parameter für die Wärmebedarf Simulation durchführen 
            simulation_Waermebedarf.Netzverluste = netzverluste;
            simulation_Waermebedarf.Netzverluste_Einheit = ctrl.m_szNetzverlusteEinheit;

            // Wärmebedarf Simulation
            simulation_Waermebedarf.Waermebedarf_berechnen(m_ID_Projekt, nKlimaregion);
            simulation_Strombedarf.m_ID_Projekt = m_ID_Projekt;
            
            // Strombedarf Simulation
            simulation_Strombedarf.Berechnung(m_ID_Projekt);

            // chart füllen   
            textBox_MaxWaermelast.Text = simulation_Waermebedarf.Waermebedarf_Max.ToString("F2");
            textBox_Gesamt_Waermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            chart1.Annotations.Clear();
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            if (checkBox_Sortiert.Checked)
            {
                chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie);
            }
            else chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie_nicht_sortiert);
            chart1.ChartAreas[0].AxisY.Maximum = 100.2;

            // chart füllen
            textBox_MaxStrombedarf.Text = simulation_Strombedarf.Strombedarf_Max.ToString("F2");
            textBox_Gesamt_Strombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");

            chart2.Annotations.Clear();
            chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart2.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
            if (checkBox_StromSortiert.Checked)
            {
                chart2.Series[0].Points.DataBindY(simulation_Strombedarf.Dauerlinie);
            }
            else chart2.Series[0].Points.DataBindY(simulation_Strombedarf.Dauerlinie_nicht_sortiert);

            return true;
        }

        private void checkBox_Sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Sortiert.Checked)
            {
                chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie);
            }
            else chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie_nicht_sortiert);
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Interval = 1000;
            chart1.ChartAreas[0].AxisY.Maximum = 100.2;
            chart1.Series[0].BorderWidth = 2;
        }

        private void checkBox_StromSortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_StromSortiert.Checked)
            {
                chart2.Series[0].Points.DataBindY(simulation_Strombedarf.Dauerlinie);
            }
            else chart2.Series[0].Points.DataBindY(simulation_Strombedarf.Dauerlinie_nicht_sortiert);
            chart2.ChartAreas[0].AxisX.Minimum = 0;
            chart2.ChartAreas[0].AxisX.Interval = 1000;
            chart2.ChartAreas[0].AxisY.Maximum = 100.2;
            chart2.Series[0].BorderWidth = 2;
        }

        private void btn_Details_Click(object sender, EventArgs e)
        {
            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation_Waermebedarf);
            frm.ShowDialog();
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

        private void Ergebnis_Simulation()
        {
            // Wärmepumpe
            textBox_WB_Deckung.Text = "";
            double a = (double)simulation_Waermebedarf.Waermebedarf_Gesamt;
            double b = (double)sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            double c = (double)sim.simulation_wp.Heizstab_gesamt / 1000;

            if ((b / a * 100) > 100)
                textBox_WB_Deckung.Text = "100";
            else
                textBox_WB_Deckung.Text = (b / a * 100).ToString("F2");

            if (ctrl.m_WP_Heizstab)
                textBox_WB_Deckung.Text = ((b + c) / a * 100).ToString("F2");
            else
                textBox_WB_Deckung.Text = (b / a * 100).ToString("F2");

            textBox_Laufzeit.Text = (sim.simulation_wp.WP_Laufzeit / sim.simulation_wp.wp_list.Count).ToString("F0");
            if (sim.simulation_wp.Bivalenzpunkt != -100)
                textBox_Bivalenzpunkt.Text = sim.simulation_wp.Bivalenzpunkt.ToString("F2");
            else
                textBox_Bivalenzpunkt.Text = "-";

            textBox_WPWaermebedarf.Text = (sim.simulation_wp.Waermebedarf_gesamt / 1000).ToString("F2");
            textBox_WPRestwermebedarf.Text = (sim.simulation_wp.Waermebedarf_gesamt / 1000 - sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000 - sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
            textBox_WPStromverbrauch.Text = (sim.simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
            textBox_HeizstabStromverbrauch.Text = (sim.simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
            textBox_WPWaermeproduktion.Text = (sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
            textBox_Pufferspeicher.Text = (sim.simulation_wp.Volumen_Pufferspeicher * 1.16).ToString();
            textBox_WPVollbenutzungsstunden.Text = (sim.simulation_wp.WP_Laufzeit / sim.simulation_wp.wp_list.Count).ToString("F0");

            double Max_Spk = 0;
            for (int i = 0; i < 8750; i++)
            {
                if (sim.simulation_wp.waermerestbedarf_stuendlich[i] > Max_Spk) Max_Spk = sim.simulation_wp.waermerestbedarf_stuendlich[i];
            }
            textBox_MinSPKLeistung.Text = Max_Spk.ToString("F2");

            for (int i = 1; i <= 5; i++)
            {
                tabPage2.Controls["textBox" + i.ToString() + "_1"].Text = (sim.simulation_wp.Modul_WP_Waermeproduktion[i - 1] / 1000).ToString("F2");
                tabPage2.Controls["textBox" + i.ToString() + "_2"].Text = (sim.simulation_wp.Modul_WP_Strombedarf[i - 1] / 1000).ToString("F2");
                tabPage2.Controls["textBox" + i.ToString() + "_3"].Text = (sim.simulation_wp.Modul_Heizstab[i - 1] / 1000).ToString("F2");
                tabPage2.Controls["textBox" + i.ToString() + "_4"].Text = (sim.simulation_wp.Modul_WP_Laufzeit[i - 1]).ToString("F2");
                tabPage2.Controls["textBox" + i.ToString() + "_Modul"].Text = sim.simulation_wp.WP_Modul[i - 1];
            }

            // charts Wärmepumpe

            chart3.ChartAreas[0].AxisX.Minimum = 0;
            chart3.ChartAreas[0].AxisX.Interval = 1000;

            chart3.Series["Waermebedarf"].Points.DataBindY(sim.simulation_wp.Waermebedarf_stuendlich);
            chart3.Series["Waermebedarf"].BorderWidth = 1;
            chart3.Series["Waermebedarf"].Color = System.Drawing.Color.Red;

            float[] temp = new float[8760];
            for (int i = 0; i < 8760; i++)
            {
                if (ctrl.m_WP_Heizstab)
                {
                    temp[i] = sim.simulation_wp.WP_Waermeproduktion_stuendlich[i] + sim.simulation_wp.Heizstab_stuendlich[i];
                }
                else temp[i] = 0;
            }
            chart3.Series["Heizstab"].Points.DataBindY(temp);
            chart3.Series["Heizstab"].BorderWidth = 1;
            chart3.Series["Heizstab"].Color = System.Drawing.Color.Yellow;

            chart3.Series["Waermeproduktion"].Points.DataBindY(sim.simulation_wp.WP_Waermeproduktion_stuendlich);
            chart3.Series["Waermeproduktion"].BorderWidth = 1;
            chart3.Series["Waermeproduktion"].Color = System.Drawing.Color.Blue;

            // chart Temperatur - Leistung  
            PointF[] ps_produktion_raw = new PointF[8760];
            PointF[] ps_bedarf_raw = new PointF[8760];
            PointF[] ps_heizstab_raw = new PointF[8760];

            List<double> werte_produktion = new List<double>();
            List<double> werte_bedarf = new List<double>();

            // nur 1 Leistungswert Wert pro gleicher Temperatur nehmen
            int index = 0;
            for (int n = 0; n < 8760; n++)
            {
                if (werte_produktion.Contains(sim.simulation_wp.Temperatur[n])) continue;
                double d = Math.Round(sim.simulation_wp.Temperatur[n], 1);
                ps_produktion_raw[index].X = (float)d;
                ps_produktion_raw[index].Y = sim.simulation_wp.WP_Waermeproduktion_stuendlich[n];
                ps_bedarf_raw[index].X = ps_produktion_raw[index].X;
                ps_bedarf_raw[index].Y = sim.simulation_wp.Waermebedarf_stuendlich[n];
                if (simulation_wp.Heizstab_stuendlich[n] > 0)
                    ps_heizstab_raw[index].Y = sim.simulation_wp.WP_Waermeproduktion_stuendlich[n] + sim.simulation_wp.Heizstab_stuendlich[n];
                else
                    ps_heizstab_raw[index].Y = 0;

                ps_heizstab_raw[index].X = ps_produktion_raw[index].X;
                werte_produktion.Add(sim.simulation_wp.Temperatur[n]);
                werte_bedarf.Add(sim.simulation_wp.Waermebedarf_stuendlich[n]);
                index++;
            }
;
            // Points Array nur mit der tatsächlichen Anzahl(mehrfache Werte gleicher Tempeatur filtern) füllen
            PointF[] ps_produktion = new PointF[index];
            PointF[] ps_bedarf = new PointF[index];
            PointF[] ps_heizstab = new PointF[index];

            for (int n = 0; n < index; n++)
            {
                ps_produktion.SetValue(ps_produktion_raw[n], n);
                ps_bedarf.SetValue(ps_bedarf_raw[n], n);
                ps_heizstab.SetValue(ps_heizstab_raw[n], n);
            }

            chart4.Series[0].ChartType = SeriesChartType.Area;
            chart4.Series[1].ChartType = SeriesChartType.Area;
            chart4.Series[2].ChartType = SeriesChartType.Area;
            chart4.Series["Waermeproduktion"].Color = Color.FromArgb(100, Color.Blue);
            chart4.Series["Waermebedarf"].Color = Color.FromArgb(140, Color.Red);
            chart4.Series["Heizstab"].Color = Color.FromArgb(240, Color.Yellow);

            chart4.Series["Heizstab"].Points.DataBindXY(ps_heizstab, "X", ps_heizstab, "Y");
            chart4.Series["Waermeproduktion"].Points.DataBindXY(ps_produktion, "X", ps_produktion, "Y");
            chart4.Series["Waermebedarf"].Points.DataBindXY(ps_bedarf, "X", ps_bedarf, "Y");

            chart4.Series[0].Sort(PointSortOrder.Ascending, "X");
            chart4.Series[1].Sort(PointSortOrder.Ascending, "X");
            chart4.Series[2].Sort(PointSortOrder.Ascending, "X");

            chart4.ChartAreas[0].AxisX.Interval = 4;
            chart4.ChartAreas[0].AxisX.LabelStyle.Format = "0.0";
            chart4.Update();

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++/
            // Spitzenkessel

            //sim.simulation_spk.Vorgabe_Betriebsbereitschaft = ctrl.m_Kessel_Betriebsbereitschaft;
            //sim.simulation_spk.Berechnung(m_ID_Projekt);

            //  tb_gesWaermebedarf.Text = (simulation_Waermebedarf.Waermebedarf_Gesamt).ToString("F2");
            textBox_SPKWaermebedarf.Text = sim.simulation_spk.Waermebedarf_gesamt.ToString("F2");
            textBox_SPKRestwermebedarf.Text = (sim.simulation_spk.Waermebedarf_gesamt - sim.simulation_spk.S_Waerme_spk).ToString("F2");

            tb_WaermeprSpk.Text = (sim.simulation_spk.S_Waerme_spk).ToString("F2");
            tb_Gasverbrauch.Text = (sim.simulation_spk.Gasverbrauch_SPK).ToString("F2");
            tb_Oelverbrauch.Text = (sim.simulation_spk.Oelverbrauch_SPK).ToString("F2");
            tb_Biogasverbrauch.Text = (sim.simulation_spk.Biogasverbrauch_SPK).ToString("F2");
            tb_Rapsoelverbrauch.Text = (sim.simulation_spk.Rapsoelverbrauch_SPK).ToString("F2");
            tb_Holzverbrauch.Text = (sim.simulation_spk.Holzverbrauch_SPK).ToString("F2");
            tb_Fluessiggasverbrauch.Text = (sim.simulation_spk.Fluessiggasverbrauch_SPK).ToString("F2");
            tb_Stromverbrauch.Text = (sim.simulation_spk.Stromverbrauch_Spk).ToString("F2");
            tb_Sonstigverbrauch.Text = (sim.simulation_spk.Sonstigverbrauch_SPK).ToString("F2");


            tb_Max_Kesselleistung.Text = (sim.simulation_spk.Maximale_Kesselleistung_Spk).ToString("F2");
            tb_Gasspitze.Text = sim.simulation_spk.Gasspitze_Spk.ToString("F2");
  
            listView_SimSPK.Items.Clear();
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (i + 1).ToString();
                lvitem.SubItems.Add(sim.simulation_spk.spk_list[i]);
                lvitem.SubItems.Add((sim.simulation_spk.s_waerme_Gas_Spk[i]).ToString("F2"));
                lvitem.SubItems.Add((sim.simulation_spk.s_waerme_Oel_Spk[i]).ToString("F2"));
                lvitem.SubItems.Add((sim.simulation_spk.Kessel_Wirk_Gas_Spk[i] * 100).ToString("F1"));

                listView_SimSPK.Items.Add(lvitem);
            }
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

        }

        private void Endergebiss_Simulation()
        {
            Ergebnis_Simulation();

            chart5.Legends[0].LegendStyle = LegendStyle.Table;
            chart5.Legends[0].Docking = Docking.Right;
            chart5.Legends[0].Alignment = StringAlignment.Center;
            chart5.Legends[0].Title = "Wärmebedarfsdeckung";
            chart5.Legends[0].BorderColor = Color.Green;
            chart5.Series[0].IsValueShownAsLabel = false;
            chart5.Series[0]["PieLabelStyle"] = "Outside";
            chart5.Series[0].Points.Clear();

            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {
                double valkessel = sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];
                if (valkessel > 0)
                    chart5.Series[0].Points.AddXY("Kessel" + (i + 1).ToString(), valkessel);
            }

            if (sim.simulation_wp.WP_Waermeproduktion_gesamt > 0)
                chart5.Series[0].Points.AddXY("WP", sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000);

            if (sim.simulation_wp.Heizstab_gesamt > 0)
                chart5.Series[0].Points.AddXY("Heizstab", sim.simulation_wp.Heizstab_gesamt / (double)1000);


            double rest = 0;
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {
                rest = rest + sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];
            }
            rest = sim.simulation_Waermebedarf.Waermebedarf_Gesamt -
                                                        sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000 -
                                                        sim.simulation_wp.Heizstab_gesamt / 1000 -
                                                        rest;

            if (rest >= 0.1)
            {
                chart5.Series[0].Points.AddXY("Restbedarf", rest);
            }

            textBox_FinalWaermebedarf.Text = rest.ToString("F2");
            textBox_FinalStrombedarf.Text = sim.Reststrom.ToString("F2");   

        }


    }
}
