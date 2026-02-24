using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Detail : Form
    {
        public SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
        public SimulationStrombedarf simulation_Strombedarf = new SimulationStrombedarf();
        SimulationControl sim = new SimulationControl();
        SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        KonfigurationCtrl ctrl = new KonfigurationCtrl();
        ProjektCtrl projektCtrl = new ProjektCtrl();

        public int m_ID_Projekt;
        public double m_Waermebedarf_Gesamt;
        public double m_Strombedarf_Gesamt;

        private float[] temp_profil;
        private float[] temp_wp;
        private float[] temp_hs;
        private float[] temp_hk;
        private float[] temp_ges;


        Point prevPosition;
        ToolTip tooltip = new ToolTip();

        public Form_Simulation_Detail()
        {
            InitializeComponent();
            init_Chart(chart1);
            init_Chart(chart2);
            init_Chart(chart6);
            init_Chart(chart8);
            chart8.MouseWheel += Chart8_MouseWheel;
        }

        public Form_Simulation_Detail(int iD_Projekt)
        {
            InitializeComponent();
            m_ID_Projekt = iD_Projekt;

            init_Chart(chart1);
            init_Chart(chart2);
            init_Chart(chart6);
            init_Chart(chart7);
//            init_Chart(chart8);
            chart8.MouseWheel += Chart8_MouseWheel;

            listView_SimSPK.View = View.Details;
            listView_SimSPK.Columns.Add("Heizkessel", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Gas/Biogas/Rapsöl/Holz... [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Öl [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Jahresnutzungsgrad [%]", -2, HorizontalAlignment.Left);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView_SimWP.View = View.Details;
            listView_SimWP.Columns.Add("WP Modul", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Wärmeproduktion [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Stromverbrauch [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Heizstab [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Betriebsstunden [h/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            colorListViewHeader(ref listView_SimWP, Color.LightBlue, Color.Black);
            colorListViewHeader(ref listView_SimSPK, Color.LightBlue, Color.Black);
        }

        public void SetControls()
        {
        }

        private void init_Chart(Chart chart)
        {
            var ca = chart.ChartAreas[0];

            // Enable cursors and selections
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;

            // Allow zooming on both axes
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;

            ca.AxisX.ScaleView.SmallScrollSize = 1;

            chart.ChartAreas[0].CursorX.Interval = 0;
            ca.AxisX.Minimum = 0;
            ca.AxisY.Maximum = 100.2;

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
            // TextBoxe leeren  
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                InitTextBoxen(tabControl1.TabPages[i]);
            }

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

            Endergebniss_Simulation();
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

            // chart Wärmebedarf füllen   
            textBox_MaxWaermelast.Text = simulation_Waermebedarf.Waermebedarf_Max.ToString("F2");
            textBox_Gesamt_Waermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            chart1.Annotations.Clear();
            chart1.Series[0].Points.Clear();
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            if (checkBox_Sortiert.Checked)
                ConfigureXAxisWithHours(chart1, simulation_Waermebedarf.Dauerlinie);
            else
            {
                ConfigureXAxisWithMonths(chart1);
                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart1.Series[0].Points.AddXY(d, simulation_Waermebedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }

            chart1.ChartAreas[0].AxisY.Maximum = 100.2;

            // chart Strombedarf füllen
            textBox_MaxStrombedarf.Text = simulation_Strombedarf.Strombedarf_Max.ToString("F2");
            textBox_Gesamt_Strombedarf.Text = simulation_Strombedarf.Strombedarf_gesamt.ToString("F2");

            chart2.Annotations.Clear();
            chart2.Series[0].Points.Clear();
            chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart2.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            if (checkBox_StromSortiert.Checked)
            {
                ConfigureXAxisWithHours2(chart2, 4);
                for (int j = 0; j < 8760 * 4; j += 4)
                {
                    double d = (double)j * 12 / (8760);
                    chart2.Series[0].Points.AddXY(d, simulation_Strombedarf.Dauerlinie[j]);
                }
            }
            else
            {
                ConfigureXAxisWithMonths(chart2);
                for (int j = 0; j < 8760 * 4; j += 10)
                {
                    double d = (double)j * 12 / (8760);
                    chart2.Series[0].Points.AddXY(d, simulation_Strombedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }

            return true;
        }

        private void checkBox_Sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Sortiert.Checked)
                ConfigureXAxisWithHours(chart1, simulation_Waermebedarf.Dauerlinie);
            else
            {
                ConfigureXAxisWithMonths(chart1);
                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart1.Series[0].Points.AddXY(d, simulation_Waermebedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }
        }

        private void checkBox_StromSortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_StromSortiert.Checked)
                ConfigureXAxisWithHours(chart2, simulation_Strombedarf.Dauerlinie, 4);
            else
            {
                ConfigureXAxisWithMonths(chart2);
                for (int j = 0; j < 8760 * 4; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart2.Series[0].Points.AddXY(d, simulation_Strombedarf.Dauerlinie_nicht_sortiert[j]);
                }
            }
        }

        private void btn_Details_Click(object sender, EventArgs e)
        {
            Form_ErgBrauchwasserwaerme frm = new Form_ErgBrauchwasserwaerme();
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

            ReihenfolgeTabPages();
        }

        private void Endergebniss_Simulation()
        {
            chart3.Series[0].Points.Clear();
            chart3.Series[1].Points.Clear();
            chart3.Series[2].Points.Clear();
            chart4.Series[0].Points.Clear();
            chart6.Series[0].Points.Clear();

            // ********************************************************************************************/
            // Wärmepumpe
            // ********************************************************************************************/
            if (sim.bSimulationWP)
            {
                chart3.Annotations.Clear();
                chart3.ChartAreas[0].AxisX.StripLines.Clear();
                chart3.ChartAreas[0].AxisX.CustomLabels.Clear();
                chart3.Series[0].BorderWidth = 2;

                chart3.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Black;
                chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                chart3.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;

                chart3.ChartAreas[0].CursorX.LineDashStyle = ChartDashStyle.Dot;
                chart3.ChartAreas[0].CursorY.LineDashStyle = ChartDashStyle.Dot;
                chart3.ChartAreas[0].CursorX.LineColor = Color.Red;
                chart3.ChartAreas[0].CursorY.LineColor = Color.Red;

                chart3.ChartAreas[0].AxisX.Enabled = AxisEnabled.True;
                chart3.ChartAreas[0].AxisX.Interval = 0;
                chart3.ChartAreas[0].AxisX.LabelStyle.Font = new System.Drawing.Font("Arial", 8);
                chart3.ChartAreas[0].AxisX.LabelAutoFitStyle = LabelAutoFitStyles.None;
                chart3.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
                chart3.ChartAreas[0].AxisX.LineWidth = 1;

                chart3.Series["Waermebedarf"].Color = Color.Red;
                chart3.Series["Heizstab"].Color = Color.Yellow;
                chart3.Series["Waermeproduktion"].Color = Color.Blue;

                textBox_WB_Deckung.Text = "";
                double a = (double)simulation_Waermebedarf.Waermebedarf_Gesamt;
                double b = (double)sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
                double c = (double)sim.simulation_wp.Heizstab_gesamt / 1000;

                if ((b / a * 100) > 100)
                    textBox_WB_Deckung.Text = "100";
                else
                    textBox_WB_Deckung.Text = ((b + c) / a * 100).ToString("F2");


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

                listView_SimWP.Items.Clear();
                for (int i = 0; i < sim.simulation_wp.wp_list.Count(); i++)
                {
                    ListViewItem lvitem = new ListViewItem();
                    lvitem.Text = sim.simulation_wp.WP_Modul[i];
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_WP_Waermeproduktion[i] / 1000).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_WP_Strombedarf[i] / 1000).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_Heizstab[i] / 1000).ToString("F2"));
                    lvitem.SubItems.Add((sim.simulation_wp.Modul_WP_Laufzeit[i]).ToString("F2"));

                    listView_SimWP.Items.Add(lvitem);
                }

                listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                // charts und Textfelder Wärmepumpe
                checkBox_WP_sortiert.Checked = true;
                checkBox_WP_sortiert.Checked = false;

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

                // Chart Wärmepumpe Strombedarf
                float[] temp = simulation_Strombedarf.AddVectors(sim.simulation_wp.WP_Strombedarf_stuendlich, sim.simulation_wp.Heizstab_stuendlich);
                float WPStrombedarf_Max = sim.simulation_Strombedarf.Maximaler_Strombedarf(temp); // in kWh
                //temp = sim.simulation_Strombedarf.NormVector(temp, WPStrombedarf_Max);
                chart6.ChartAreas[0].AxisY.Maximum = WPStrombedarf_Max + 1;
                chart6.Annotations.Clear();
                chart6.Series[0].Points.Clear();
                chart6.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                chart6.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
                ConfigureXAxisWithMonths(chart6);
                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart6.Series[0].Points.AddXY(d, temp[j]);
                }

                // Chart WP Wärmeproduktion über Temperaturgang
                chart4.Series[0].Points.Clear();
                chart4.Series[1].Points.Clear();
                chart4.Series[2].Points.Clear();
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

                chart4.ChartAreas[0].AxisX.LabelStyle.Format = "0.0";
                chart4.ChartAreas[0].AxisX.Interval = 5;

                chart4.Update();
            }

            // ********************************************************************************************/
            // Heizkessel
            // ********************************************************************************************/
            if (sim.bSimulationKessel)
            {
                // Textfelder Spitzenkessel
                if (simulation_Waermebedarf.Waermebedarf_Gesamt > 0)
                    textBox_SPKWaermebedarfsdeckung.Text = (sim.simulation_spk.S_Waerme_spk * 100 / simulation_Waermebedarf.Waermebedarf_Gesamt).ToString("F2");
                else
                    textBox_SPKWaermebedarfsdeckung.Text = "0";
                textBox_SPKWaermebedarf.Text = sim.simulation_spk.Waermebedarf_gesamt.ToString("F2");
                textBox_SPKRestwermebedarf.Text = (sim.simulation_spk.Waermebedarf_gesamt - sim.simulation_spk.S_Waerme_spk).ToString("F2");
                tb_WaermeprSpk.Text = (sim.simulation_spk.S_Waerme_spk).ToString("F2");

                tb_Gasverbrauch.Text = (sim.simulation_spk.Gasverbrauch_SPK).ToString("F2");
                tb_Oelverbrauch.Text = (sim.simulation_spk.Oelverbrauch_SPK).ToString("F2");
                tb_Koks.Text = (sim.simulation_spk.Koks_SPK).ToString("F2");
                tb_Rapsoelverbrauch.Text = (sim.simulation_spk.Rapsoelverbrauch_SPK).ToString("F2");
                tb_Holzverbrauch.Text = (sim.simulation_spk.Holzverbrauch_SPK).ToString("F2");
                tb_Kohle.Text = (sim.simulation_spk.Kohle_SPK).ToString("F2");
                tb_Stromverbrauch.Text = (sim.simulation_spk.Stromverbrauch_Spk).ToString("F2");
                tb_Sonstigverbrauch.Text = (sim.simulation_spk.Sonstigverbrauch_SPK).ToString("F2");
                tb_Pellets.Text = (sim.simulation_spk.Pellets_SPK).ToString("F2");
                tb_Koks.Text = (sim.simulation_spk.Koks_SPK).ToString("F2");
                tb_TierischeFette.Text = (sim.simulation_spk.TierischeFette_SPK).ToString("F2");

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

            // ********************************************************************************************/
            // Solarthermie
            // ********************************************************************************************/
            if (sim.bSimulationSolarthermie)
            {
                // Textfelder Solarthermie
                if (sim.simulation_solarthermie.Waermebedarf_gesamt > 0)
                    textBox_STWaermebedarfsdeckung.Text = (sim.simulation_solarthermie.Waermeproduktion_gesamt * 100 / sim.simulation_solarthermie.Waermebedarf_gesamt).ToString("F2");
                else
                    textBox_STWaermebedarfsdeckung.Text = "";
                textBox_STWaermebedarf.Text = (sim.simulation_solarthermie.Waermebedarf_gesamt / 1000).ToString("F2");
                textBox_STRestwermebedarf.Text = ((sim.simulation_solarthermie.Waermebedarf_gesamt - sim.simulation_solarthermie.Waermeproduktion_gesamt) / 1000).ToString("F2");
                tb_WaermeprST.Text = (sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000).ToString("F2");
                textBox_Ueberschuss.Text = (sim.simulation_solarthermie.ueberschuss / 1000).ToString("F2");

                float STWaermebedarf_Max = (float)sim.simulation_solarthermie.Max_Waermebedarf; // in kWh
                //temp = sim.simulation_Strombedarf.NormVector(sim.simulation_solarthermie.Waermeproduktion, STWaermebedarf_Max);

                float[] temp = Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermeproduktion, x => (float)x);
                chart8.Series[0].Points.Clear();
                chart8.Series[1].Points.Clear();
                chart8.ChartAreas[0].AxisY.Maximum = STWaermebedarf_Max + 1;
                chart8.Annotations.Clear();
                chart8.Series["Wärmeproduktion"].Color = Color.FromArgb(100, Color.Blue);
                chart8.Series["Wärmebedarf"].Color = Color.FromArgb(140, Color.Red);
                chart8.Series[0].ChartType = SeriesChartType.Line;
                chart8.Series[1].ChartType = SeriesChartType.Line;
                chart8.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                chart8.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
    
              //  ConfigureXAxisWithMonths(chart8);
                chart8.Series[0].XValueType = ChartValueType.DateTime;
                chart8.Series[1].XValueType = ChartValueType.DateTime;
                chart8.ChartAreas[0].AxisX.LabelStyle.Format = "%M";
                // 1. Das Format auf "Monatszahl" setzen


                // 2. Sicherstellen, dass die Abstände in Monaten gerechnet werden
                chart8.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Months;
                // Gitterlinien so verschieben, dass sie zwischen den Monaten liegen
                chart8.ChartAreas[0].AxisX.MajorGrid.IntervalOffset = +0.5;
                //chart8.ChartAreas[0].AxisY.MajorTickMark.IntervalOffset = -0.5;
                chart8.ChartAreas[0].AxisX.Maximum = new DateTime(2026, 12, 1).ToOADate();

                // 3. Alle 1 Monat eine Zahl anzeigen
                chart8.ChartAreas[0].AxisX.Interval = 1;

                chart8.ChartAreas[0].CursorX.IsUserEnabled = true;
                chart8.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;

                // Scrollbar aktivieren
                chart8.ChartAreas[0].AxisX.ScrollBar.Enabled = true;

                chart8.ChartAreas[0].CursorX.IsUserEnabled = true;
                chart8.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
                chart8.ChartAreas[0].AxisX.ScaleView.Zoomable = true;


                DateTime dt = new DateTime(2026, 1, 1);
                for (int j = 0; j < 8760; j++)
                {
                    //double d = (double)j * 12 / (8760);
                    DateTime d=dt.AddHours(j); 
                    chart8.Series[1].Points.AddXY(d, temp[j]);
                    chart8.Series[0].Points.AddXY(d, (float)sim.simulation_solarthermie.Waermebedarf[j]);
                }
            }

            // ********************************************************************************************/
            // Ergebnisübersicht
            // ********************************************************************************************/
            // Kuchendiagramm
            chart5.Series[0].ChartType = SeriesChartType.Pie;
            chart5.ChartAreas[0].Area3DStyle.Enable3D = false;
            chart5.Series[0].IsValueShownAsLabel = false;
            chart5.Series[0]["PieLabelStyle"] = "Disabled";
            chart5.BorderlineColor = Color.Black;
            chart5.Series[0].BorderColor = Color.Black;
            chart5.Series[0].BorderWidth = 1;
            chart5.Series[0].LabelForeColor = Color.Black;
            chart5.Series[0].Font = new Font("Arial", 10, FontStyle.Bold);

            chart5.Series[0].Points.Clear();

            double waerme_spk = 0;
            double waerme_wp = 0;
            double waerme_heizstab = 0;
            double waerme_solar = 0;
            double rest = 0;
            double gesamt_waerme = 0;

            // Heizkessel
            waerme_spk = 0;
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {
                waerme_spk += sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];
            }

            if (waerme_spk > 0)
                chart5.Series[0].Points.AddXY("Heizkessel", waerme_spk);

            // Wärmepumpe
            waerme_wp = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            if (waerme_wp > 0)
                chart5.Series[0].Points.AddXY("Wärmepumpe", waerme_wp);

            waerme_heizstab = sim.simulation_wp.Heizstab_gesamt / 1000;
            if (waerme_heizstab > 0)
                chart5.Series[0].Points.AddXY("Heizstab", waerme_heizstab);

            // Solarthermie
            waerme_solar = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;
            if (waerme_solar > 0)
                chart5.Series[0].Points.AddXY("Solarthermie", waerme_solar);

            gesamt_waerme = waerme_spk + waerme_wp + waerme_heizstab + waerme_solar;


            rest = sim.simulation_Waermebedarf.Waermebedarf_Gesamt - gesamt_waerme;

            if (rest >= 0.1)
            {
                chart5.Series[0].Points.AddXY("Restbedarf", rest);
            }

            textBox_FinalWaermebedarf.Text = rest.ToString("F2");
            textBox_FinalStrombedarf.Text = sim.Reststrom.ToString("F2");

            double a2 = (double)simulation_Waermebedarf.Waermebedarf_Gesamt;
            double b2 = (double)sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            double c2 = (double)sim.simulation_wp.Heizstab_gesamt / 1000;
            double d2 = sim.simulation_spk.S_Waerme_spk;
            double e2 = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;

            textBox_WBDeckung.Text = ((b2 + c2) / a2 * 100).ToString("F2");
            textBox_SPKDeckung.Text = (d2 * 100 / a2).ToString("F2");
            textBox_STDeckung.Text = (e2 * 100 / a2).ToString("F2");

            chart5.Update();

            chart7.Series["Gesamt"].Color = Color.Green;
            chart7.Series["Waermepumpe"].Color = Color.Orange;
            chart7.Series["Heizstab"].Color = Color.Red;
            chart7.Series["Heizkessel"].Color = Color.Blue;
            chart7.Series["Profil/Lastgang"].Color = Color.Brown;

            chart7.Series["Gesamt"].ChartType = SeriesChartType.Line;
            chart7.Series["Waermepumpe"].ChartType = SeriesChartType.Line;
            chart7.Series["Heizstab"].ChartType = SeriesChartType.Line;
            chart7.Series["Heizkessel"].ChartType = SeriesChartType.Line;
            chart7.Series["Profil/Lastgang"].ChartType = SeriesChartType.Line;

            temp_profil = sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            temp_wp = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.WP_Strombedarf_stuendlich);
            temp_hs = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.Heizstab_stuendlich);
            temp_hk = sim.Stundenwerte_zu_viertelstunden(sim.simulation_spk.Strombedarf_stuendlich);
            temp_ges = new float[8760 * 4];

            for (int i = 0; i < 8760 * 4; i++)
            {
                temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_profil[i];
            }
            chart7.ChartAreas[0].AxisY.Maximum = temp_ges.Max();

            ConfigureXAxisWithMonths(chart7);

            checkBox_Gesamt.Checked = true;

        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void ConfigureXAxisWithMonths(Chart chartControl)
        {
            // Define your custom labels in an array
            string[] monthArray = { "1", "2", "3", "4", "5", "6", "7", "8", "8", "10", "11", "12" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            chartControl.Annotations.Clear();
            chartControl.Series[0].Points.Clear();

            chartControl.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chartControl.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = monthArray.Length;
            chartControl.ChartAreas[0].AxisX.Interval = 1;

            for (int i = 0; i < monthArray.Length; i++)
            {
                CustomLabel lblMonth = new CustomLabel();
                lblMonth.FromPosition = i;
                lblMonth.ToPosition = i + 0.8;
                lblMonth.Text = monthArray[i];
                chartControl.ChartAreas[0].AxisX.CustomLabels.Add(lblMonth);
            }

            chartControl.ChartAreas[0].AxisX.IntervalOffsetType = DateTimeIntervalType.Months;
            chartControl.ChartAreas[0].AxisX.Title = "Monat";
            chartControl.ChartAreas[0].AxisX.ScaleView.Size = 12;

            return;
        }

        public void ConfigureXAxisWithHours(Chart chartControl, float[] Dauerlinie_sortiert, int Interval = 1)
        {
            // custom labels in array
            string[] hourArray = { "2000", "4000", "6000", "8000" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            chartControl.Annotations.Clear();
            chartControl.Series[0].Points.Clear();

            chartControl.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chartControl.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = hourArray.Length;
            chartControl.ChartAreas[0].AxisX.Interval = 1;

            // Add custom labels for each data point position
            for (int i = 0; i < hourArray.Length; i++)
            {
                CustomLabel lblMonth = new CustomLabel();
                lblMonth.FromPosition = i;
                lblMonth.ToPosition = i + 0.8;
                lblMonth.Text = hourArray[i];
                chartControl.ChartAreas[0].AxisX.CustomLabels.Add(lblMonth);
            }

            for (int j = 0; j < 8760 * Interval; j++)
            {
                double d = (double)j * 4 / (8760 * Interval);
                chartControl.Series[0].Points.AddXY(d, Dauerlinie_sortiert[j]);
            }
            chartControl.ChartAreas[0].AxisX.IntervalOffsetType = DateTimeIntervalType.Hours;
            chartControl.ChartAreas[0].AxisX.Title = "Jahresstunden";

            return;
        }

        public void ConfigureXAxisWithHours2(Chart chartControl, int Interval = 1)
        {
            // custom labels in array
            string[] hourArray = { "2000", "4000", "6000", "8000" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            chartControl.Annotations.Clear();

            chartControl.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chartControl.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            chartControl.ChartAreas[0].AxisX.Minimum = 0;
            chartControl.ChartAreas[0].AxisX.Maximum = hourArray.Length;
            chartControl.ChartAreas[0].AxisX.Interval = 1;

            // Add custom labels for each data point position
            for (int i = 0; i < hourArray.Length; i++)
            {
                CustomLabel lblMonth = new CustomLabel();
                lblMonth.FromPosition = i;
                lblMonth.ToPosition = i + 0.8;
                lblMonth.Text = hourArray[i];
                chartControl.ChartAreas[0].AxisX.CustomLabels.Add(lblMonth);
            }

            chartControl.ChartAreas[0].AxisX.IntervalOffsetType = DateTimeIntervalType.Hours;
            chartControl.ChartAreas[0].AxisX.Title = "Jahresstunden";

            return;
        }

        private void checkBox_WP_sortiert_CheckedChanged(object sender, EventArgs e)
        {
            float[] temp = new float[8760];

            chart3.ChartAreas[0].AxisX.CustomLabels.Clear();
            chart3.ChartAreas[0].AxisX.StripLines.Clear();

            for (int i = 0; i < 8760; i++)
            {
                if (ctrl.model.m_WP_Heizstab)
                {
                    temp[i] = sim.simulation_wp.WP_Waermeproduktion_stuendlich[i] + sim.simulation_wp.Heizstab_stuendlich[i];
                }
                else temp[i] = 0;
            }

            if (checkBox_WP_sortiert.Checked)
            {
                float[] sortedWBArray = new float[8760];
                Array.Copy(sim.simulation_wp.WP_Waermeproduktion_stuendlich, sortedWBArray, 8760);
                Array.Sort(sortedWBArray);
                Array.Reverse(sortedWBArray);

                float[] sortedArray = new float[8760];
                Array.Copy(sim.simulation_wp.Waermebedarf_stuendlich, sortedArray, 8760);
                Array.Sort(sortedArray);
                Array.Reverse(sortedArray);

                float[] sortedArrayHeizstab = new float[8760];
                Array.Copy(temp, sortedArrayHeizstab, 8760);
                Array.Sort(sortedArrayHeizstab);
                Array.Reverse(sortedArrayHeizstab);


                chart3.Series[0].Points.Clear();
                chart3.Series[1].Points.Clear();
                chart3.Series[2].Points.Clear();
                ConfigureXAxisWithHours2(chart3);

                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart3.Series["Waermebedarf"].Points.AddXY(d, sortedArray[j]);
                    chart3.Series["Waermeproduktion"].Points.AddXY(d, sortedWBArray[j]);
                    chart3.Series["Heizstab"].Points.AddXY(d, sortedArrayHeizstab[j]);
                }

                // Gitterlinien für die X-Achse (vertikale Linien)
                chart3.ChartAreas[0].AxisX.MajorGrid.Enabled = true; // Falls sie mal weg sind
                chart3.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray; // Dezente Farbe
                chart3.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash; // Gestrichelt statt durchgezogen
                chart3.ChartAreas[0].AxisX.MajorGrid.LineWidth = 1;

                // Gitterlinien für die Y-Achse (horizontale Linien)
                chart3.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
                chart3.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot; // Gepunktet für die Y-Ebene
            }
            else
            {
                chart3.Series[0].Points.Clear();
                chart3.Series[1].Points.Clear();
                chart3.Series[2].Points.Clear();
                ConfigureXAxisWithMonths(chart3);

                for (int j = 0; j < 8760; j++)
                {
                    double d = (double)j * 12 / (8760);
                    chart3.Series["Waermeproduktion"].Points.AddXY(d, sim.simulation_wp.WP_Waermeproduktion_stuendlich[j]);
                    chart3.Series["Waermebedarf"].Points.AddXY(d, sim.simulation_wp.Waermebedarf_stuendlich[j]);
                    chart3.Series["Heizstab"].Points.AddXY(d, temp[j]);
                }
                chart3.ChartAreas[0].AxisX.Title = "Monat";
            }
        }

        //List view header formatters
        public static void colorListViewHeader(ref ListView list, Color backColor, Color foreColor)
        {
            list.OwnerDraw = true;
            list.DrawColumnHeader +=
                new DrawListViewColumnHeaderEventHandler
                (
                    (sender, e) => headerDraw(sender, e, backColor, foreColor)
                );
            list.DrawItem += new DrawListViewItemEventHandler(bodyDraw);
        }

        private static void headerDraw(object sender, DrawListViewColumnHeaderEventArgs e, Color backColor, Color foreColor)
        {
            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            using (SolidBrush foreBrush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(e.Header.Text, e.Font, foreBrush, e.Bounds);
            }
        }

        private static void bodyDraw(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void chart2_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (pos == prevPosition || checkBox_StromSortiert.Checked) return;

            prevPosition = pos;

            var results = chart2.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    DateTime startDatum = new DateTime(DateTime.Now.Year, 1, 1); // Start: 1. Januar 

                    // Addiere diesen Wert zum Startdatum.
                    int d = (int)(xVal * 365 * 24 * 4 / 12); // mit (int) erhält man nur vielfache von 1/4 Stunden, 15 Minuten Takt

                    // auf Minuten zurückrechnen
                    d = d * 15;
                    DateTime neuesDatum = startDatum.AddMinutes(d);
                    tooltip.Show(neuesDatum.ToString("dd/MM H:mm [" + (int)yVal).ToString() + "%]", chart2, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart2);
                }
            }
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (pos == prevPosition || checkBox_Sortiert.Checked) return;

            prevPosition = pos;

            var results = chart1.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    DateTime startDatum = new DateTime(DateTime.Now.Year, 1, 1); // Start: 1. Januar 

                    // Addiere diesen Wert zum Startdatum.
                    int d = (int)(xVal * 365 * 24 * 4 / 12); // mit (int) erhält man nur vielfache von 1/4 Stunden, 15 Minuten Takt

                    // auf Minuten zurückrechnen
                    d = d * 15;
                    DateTime neuesDatum = startDatum.AddMinutes(d);

                    tooltip.Show(neuesDatum.ToString("dd/MM H:mm [" + (int)yVal).ToString() + "%]", chart1, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart1);
                }
            }
        }

        private void chart6_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (pos == prevPosition) return;

            prevPosition = pos;

            var results = chart6.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    DateTime startDatum = new DateTime(DateTime.Now.Year, 1, 1); // Start: 1. Januar 

                    // Addiere diesen Wert zum Startdatum.
                    int d = (int)(xVal * 365 * 24 / 12); // mit (int) erhält man nur vielfache von 1 Stunden
                    yVal = Math.Round(yVal, 2);
                    DateTime neuesDatum = startDatum.AddHours(d);
                    tooltip.Show(neuesDatum.ToString("dd/MM H:mm [" + yVal).ToString() + "kW]", chart6, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart6);
                }
            }
        }

        private void checkBox_Gesamt_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Gesamt.Checked)
            {
                for (int i = 0; i < 8760 * 4; i++)
                {
                    temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_profil[i];
                    double d = (double)i * 12 / (8760 * 4);
                    chart7.Series["Gesamt"].Points.AddXY(d, temp_ges[i]);

                }
            }
            else
            {
                chart7.Series["Gesamt"].Points.Clear();
            }
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_WP.Checked)
            {
                float[] temp_wp = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.WP_Strombedarf_stuendlich);

                for (int j = 0; j < 8760 * 4; j++)
                {
                    double d = (double)j * 12 / (8760 * 4);
                    chart7.Series["Waermepumpe"].Points.AddXY(d, temp_wp[j]);
                }
            }
            else
            {
                chart7.Series["Waermepumpe"].Points.Clear();
            }
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Heizstab.Checked)
            {
                float[] temp_hk = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.Heizstab_stuendlich);

                for (int j = 0; j < 8760 * 4; j++)
                {
                    double d = (double)j * 12 / (8760 * 4);
                    chart7.Series["Heizstab"].Points.AddXY(d, temp_hk[j]);
                }
            }
            else
            {
                chart7.Series["Heizstab"].Points.Clear();
            }
        }

        private void checkBox_SPK_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SPK.Checked)
            {
                float[] temp_hk = sim.Stundenwerte_zu_viertelstunden(sim.simulation_spk.Strombedarf_stuendlich);

                for (int j = 0; j < 8760 * 4; j++)
                {
                    double d = (double)j * 12 / (8760 * 4);
                    chart7.Series["Heizkessel"].Points.AddXY(d, temp_hk[j]);
                }
            }
            else
            {
                chart7.Series["Heizkessel"].Points.Clear();
            }
        }

        private void checkBox_Profil_Lastgang_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Profil_Lastgang.Checked)
            {
                float[] temp_profil = sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte;

                for (int j = 0; j < 8760 * 4; j++)
                {
                    double d = (double)j * 12 / (8760 * 4);
                    chart7.Series["Profil/Lastgang"].Points.AddXY(d, temp_profil[j]);
                }
            }
            else
            {
                chart7.Series["Profil/Lastgang"].Points.Clear();
            }
        }

        private void chart7_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (pos == prevPosition) return;

            prevPosition = pos;

            var results = chart7.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    DateTime startDatum = new DateTime(DateTime.Now.Year, 1, 1); // Start: 1. Januar 

                    // Addiere diesen Wert zum Startdatum.
                    int d = (int)(xVal * 365 * 24 * 4 / 12); // mit (int) erhält man nur vielfache von 1 Stunden
                    // auf Minuten zurückrechnen
                    d = d * 15;
                    yVal = Math.Round(yVal, 2);
                    DateTime neuesDatum = startDatum.AddMinutes(d);
                    tooltip.Show(neuesDatum.ToString("dd/MM H:mm [" + yVal).ToString() + "kW]", chart7, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart7);
                }
            }
        }

        private void chart8_MouseMove(object sender, MouseEventArgs e)
        {

            var pos = e.Location;
            if (pos == prevPosition) return;

            prevPosition = pos;

            var results = chart8.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);

            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var yVal = result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    var xVal = result.ChartArea.AxisX.PixelPositionToValue(pos.X);
   
                    yVal = Math.Round(yVal, 2);

                    // Umwandlung in ein DateTime Objekt
                    DateTime dateValue = DateTime.FromOADate(xVal);
                    tooltip.Show(dateValue.ToString("dd/MM H:mm [" + yVal).ToString() + "kW]", chart8, pos.X, pos.Y - 15);
                }
                else
                {
                    tooltip.Hide(chart8);
                }
            }
        }

        private void InitTextBoxen(TabPage page)
        {
            page.Controls.OfType<TextBox>().ToList().ForEach(tb => tb.Text = "");
        }

        private void ReihenfolgeTabPages()
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);

            string[] tool = new string[4];
            tool[0] = ctrl.model.m_Tool_1;
            tool[1] = ctrl.model.m_Tool_2;
            tool[2] = ctrl.model.m_Tool_3;
            tool[3] = ctrl.model.m_Tool_4;

            int index = 1;
            for (int i = 0; i < 4; i++)
            {
                if (tool[i] != "")
                {
                    var tabPage = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(tp => tp.Name == "tabPage_" + tool[i]);
                    if (tabPage != null)
                    {
                        tabControl1.TabPages.Remove(tabPage);
                        tabControl1.TabPages.Insert(index++, tabPage);
                    }
                }
            }

        }

        private void Form_Simulation_Detail_Load(object sender, EventArgs e)
        {
            ReihenfolgeTabPages();
        }

        private void Chart8_MouseWheel(object sender, MouseEventArgs e)
        {
            var xAxis = chart8.ChartAreas[0].AxisX;
            double xMin = xAxis.ScaleView.ViewMinimum;
            double xMax = xAxis.ScaleView.ViewMaximum;

            // Fallback auf Gesamtbereich
            if (double.IsNaN(xMin)) xMin = xAxis.Minimum;
            if (double.IsNaN(xMax)) xMax = xAxis.Maximum;

            double range = xMax - xMin;
            double zoomFactor = 0.3; // 30% Zoom-Stärke

            // Mausposition in X-Wert umrechnen (Zoom auf Cursor)
            double mouseX = xAxis.PixelPositionToValue(e.Location.X);
            double ratio = (mouseX - xMin) / range;

            if (e.Delta > 0) // Zoom IN
            {
                if (range < 0.01) return; // Zoom-Limit
                double newRange = range * (1 - zoomFactor);
                xAxis.ScaleView.Zoom(mouseX - newRange * ratio, mouseX + newRange * (1 - ratio));
            }
            else // Zoom OUT
            {
                double newRange = range * (1 + zoomFactor);
                double left = mouseX - newRange * ratio;
                double right = mouseX + newRange * (1 - ratio);

                if (left <= xAxis.Minimum && right >= xAxis.Maximum)
                    xAxis.ScaleView.ZoomReset();
                else
                    xAxis.ScaleView.Zoom(left, right);
            }
  
        }
    }
}
