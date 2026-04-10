using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
//using System.Windows.Media;
using Color = System.Drawing.Color;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;


namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Detail : Form
    {
        public SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
        public SimulationStrombedarf simulation_Strombedarf = new SimulationStrombedarf();
        SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();
        SimulationControl sim = new SimulationControl();
        KonfigurationCtrl ctrl = new KonfigurationCtrl();
        ProjektCtrl projektCtrl = new ProjektCtrl();
        ChartManager[] _chartManager = new ChartManager[10];
        ToolTip tooltip = new ToolTip();

        public int m_ID_Projekt;
        public double m_Waermebedarf_Gesamt;
        public double m_Strombedarf_Gesamt;

        private float[] temp_profil;
        private float[] temp_wp;
        private float[] temp_hs;
        private float[] temp_hk;
        private float[] temp_ges;

        double waerme_spk = 0;
        double waerme_wp = 0;
        double waerme_heizstab = 0;
        double waerme_solar = 0;
        double gesamt_waerme = 0;
        double restwaermebedarf = 0;

        Point prevPosition;

        public Form_Simulation_Detail(int iD_Projekt)
        {
            InitializeComponent();
            m_ID_Projekt = iD_Projekt;

            init_Chart(chart1);
            init_Chart(chart2);
            
            listView_SimSPK.View = View.Details;
            listView_SimSPK.Columns.Add("Heizkessel", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Gas/Biogas/Rapsöl/Holz... [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Öl [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Jahresnutzungsgrad [%]", -2, HorizontalAlignment.Left);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView_SimWP.View = View.Details;
            listView_SimWP.Columns.Add("Modul", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Wärmeprod.[MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Stromverbr. [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Heizstab [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.Columns.Add("Betriebsstunden [h/a]", -2, HorizontalAlignment.Left);
            listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimWP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            colorListViewHeader(ref listView_SimWP, Color.LightBlue, Color.Black);
            colorListViewHeader(ref listView_SimSPK, Color.LightBlue, Color.Black);

            // Initialisiere die Navigation für TabPage 1 (z.B. dein Solar-Tab)
            var navManager = new TabNavigationManager(tabPage1);
        }

        public void SetControls()
        {
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

            tabPage_Ergebnis.Invalidate();
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
            // ********************************************************************************************/
            // Wärmepumpe
            // ********************************************************************************************/
            if (sim.bSimulationWP)
            {
                // Chart Wärmepumpe Wärmerbedarf und Produktion
                _chartManager[3] = new ChartManager(chart3);
                _chartManager[3].YMaxValue = sim.simulation_Waermebedarf.Waermebedarf.Max();
                _chartManager[3].YMinValue = 0;
                _chartManager[3].XAxisAsNumber = false;
                _chartManager[3].XAxisTitle = "Jahresstunden";
                _chartManager[3].YAxisTitle = "Wärmelast";
                _chartManager[3].toolTipUnit = "kW";
                _chartManager[3].ChartTitle = "Wärmelast Jahresganglinie";
                _chartManager[3].MitLegende = true;
                _chartManager[3].Init();
                _chartManager[3].AddSeries("Waermebedarf", Color.Red, sim.simulation_wp.Waermebedarf_stuendlich);
                _chartManager[3].AddSeries("Heizstab", Color.Yellow, sim.simulation_wp.Heizstab_stuendlich);
                _chartManager[3].AddSeries("Wärmeproduktion", Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich);

                // Chart Wärmepumpe Strombedarf und Produktion
                float[] temp = simulation_Strombedarf.AddVectors(sim.simulation_wp.WP_Strombedarf_stuendlich, sim.simulation_wp.Heizstab_stuendlich);
                _chartManager[6] = new ChartManager(chart6);
                _chartManager[6].YMaxValue = temp.Max();
                _chartManager[6].YMinValue = 0;
                _chartManager[6].XAxisAsNumber = false;
                _chartManager[6].XAxisTitle = "Jahresstunden";
                _chartManager[6].YAxisTitle = "Strombedarf";
                _chartManager[6].toolTipUnit = "kW";
                _chartManager[6].ChartTitle = "Strombedarf Jahresganglinie";
                _chartManager[6].Init();
                _chartManager[6].AddSeries("Strombedarf", Color.Red, temp);

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
                    lvitem.SubItems.Add(sim.simulation_wp.wp_model[i].Grenzleistung.ToString("F2"));
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
                    //if (werte_produktion.Contains(sim.simulation_wp.Temperatur[n])) continue;
                    double d = Math.Round(sim.simulation_wp.Temperatur[n], 1);
                    ps_produktion_raw[index].X = (float)d;
                    ps_produktion_raw[index].Y = sim.simulation_wp.WP_Waermeproduktion_stuendlich[n];
                    ps_bedarf_raw[index].X = ps_produktion_raw[index].X;
                    ps_bedarf_raw[index].Y = sim.simulation_wp.Waermebedarf_stuendlich[n];

                    if (sim.simulation_wp.Heizstab_stuendlich[n] > 0)
                        ps_heizstab_raw[index].Y = sim.simulation_wp.WP_Waermeproduktion_stuendlich[n] + sim.simulation_wp.Heizstab_stuendlich[n];
                    else
                        ps_heizstab_raw[index].Y = 0;

                    ps_heizstab_raw[index].X = ps_produktion_raw[index].X;
                    werte_produktion.Add(sim.simulation_wp.Temperatur[n]);
                    werte_bedarf.Add(sim.simulation_wp.Waermebedarf_stuendlich[n]);
                    index++;
                }

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

                // ChartManager instanziieren
                _chartManager[4] = new ChartManager(chart4);
                _chartManager[4].ChartTitle = "Leistung über Außentemperatur";
                _chartManager[4].XAxisTitle = "Temperatur [°C]";
                _chartManager[4].YAxisTitle = "Leistung [kW]";
                _chartManager[4].IsXYChart = true;
                _chartManager[4].AreaLine = true; // Area Chart Effekt
                _chartManager[4].MitLegende = true;
                _chartManager[4].YMaxValue = sim.simulation_wp.Waermebedarf_stuendlich.Max();   
                _chartManager[4].Init();

                // Daten hinzufügen (gefilterte PointF[] Arrays)
                _chartManager[4].AddSeries("Wärmebedarf", Color.FromArgb(120, Color.Red), ps_bedarf, 0);
                _chartManager[4].AddSeries("Heizstab", Color.FromArgb(120, Color.Yellow), ps_heizstab, 0);
                _chartManager[4].AddSeries("Wärmeproduktion", Color.FromArgb(120, Color.Blue), ps_produktion, 0);
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
                textBox_Ueberschuss.Text = (sim.simulation_solarthermie.Ueberschuss_summe / 1000).ToString("F2");

                // Chart Solarthermie Wärmerbedarf und Produktion
                _chartManager[8] = new ChartManager(chart8);
                _chartManager[8].YMaxValue = sim.simulation_solarthermie.Waermebedarf.Max();
                _chartManager[8].YMinValue = 0;
                _chartManager[8].XAxisAsNumber = false;
                _chartManager[8].XAxisTitle = "Jahresstunden";
                _chartManager[8].YAxisTitle = "Wärmelast";
                _chartManager[8].toolTipUnit = "kW";
                _chartManager[8].ChartTitle = "Wärmelast Jahresganglinie";
                _chartManager[8].MitLegende = true;
                _chartManager[8].MitChartBorder = true;
                _chartManager[8].AreaLine = false;
                _chartManager[8].Init();
                _chartManager[8].AddSeries("Waermebedarf", Color.Red, Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermebedarf, x => (float)x));
                _chartManager[8].AddSeries("Wärmeproduktion", Color.Blue, Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermeproduktion, x => (float)x));
            }

            // ********************************************************************************************/
            // PV
            // ********************************************************************************************/
            textBox_PVStrom.Text = (sim.simulation_pv.Stromproduktion.Sum() / 1000.0).ToString("F2");
            textBox_PVUeberschuss.Text = (sim.simulation_pv.Ueberschuss.Sum() / 1000.0).ToString("F2");
            textBox_PVStrombedarfsdeckung.Text = (sim.simulation_pv.Stromproduktion.Sum() * 100 / sim.simulation_pv.Strombedarf_stuendlich.Sum()).ToString("F2");
            textBox_PVStrombedarf.Text = (sim.simulation_pv.Strombedarf.Sum() / 4000.0).ToString("F2");
            textBox_PVReststrombedarf.Text = (sim.simulation_pv.Reststrom_viertelstunde.Sum() / 4000.0).ToString("F2");

            _chartManager[9] = new ChartManager(chart_PV);
            _chartManager[9].YMaxValue = sim.simulation_pv.Strombedarf.Max() * 1.1;
            _chartManager[9].YMinValue = 0;
            _chartManager[9].XAxisAsNumber = false;
            _chartManager[9].XAxisTitle = "Monate";
            _chartManager[9].YAxisTitle = "Leistung";
            _chartManager[9].toolTipUnit = "kW";
            _chartManager[9].ChartTitle = "Strombedarf, Photovoltaik Jahresganglinie";
            _chartManager[9].MitLegende = true;
            _chartManager[9].MaxXVALUE = 8760 * 4;
            _chartManager[9].MitViertelStunde = true;
            _chartManager[9].Init();
            // NUR DER SPEICHER geht auf die rechte Achse (true = Sekundärachse kWh)
            _chartManager[9].AddSeries("Speicherfüllstand", Color.FromArgb(120, 130, 140), sim.simulation_pv.Speicherfuellstand_viertelstunde);
            _chartManager[9].AddSeries("Überschuss", Color.Yellow, sim.simulation_pv.Ueberschuss_viertelstunde);
            _chartManager[9].AddSeries("Strombedarf", Color.Red, sim.simulation_pv.Strombedarf);
            _chartManager[9].AddSeries("Photovoltaik", Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            _chartManager[9]._chart.Series["Überschuss"].Enabled = false;
            _chartManager[9]._chart.Series["Speicherfüllstand"].Enabled = false;
            checkBox_Ueberschuss.Checked = false;
            checkBox_Speicherzustand.Checked = false;
            textBox_MaxPSolar.Text =  sim.simulation_pv.MaxPSolar.ToString("F2");

            // ********************************************************************************************/
            // Ergebnisübersicht
            // ********************************************************************************************/

            // Heizkessel
            waerme_spk = 0;
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {
                waerme_spk += sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];
            }

            // Wärmepumpe
            waerme_wp = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            waerme_heizstab = sim.simulation_wp.Heizstab_gesamt / 1000;

            // Solarthermie
            waerme_solar = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;
            gesamt_waerme = waerme_spk + waerme_wp + waerme_heizstab + waerme_solar;
            restwaermebedarf = sim.simulation_Waermebedarf.Waermebedarf_Gesamt - gesamt_waerme;

            // Chart Strombedarf und Stromverbrauch Übersicht
            temp_profil = sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            temp_wp = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.WP_Strombedarf_stuendlich);
            temp_hs = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.Heizstab_stuendlich);
            temp_hk = sim.Stundenwerte_zu_viertelstunden(sim.simulation_spk.Strombedarf_stuendlich);
            temp_ges = new float[8760 * 4];

            for (int i = 0; i < 8760 * 4; i++) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_profil[i];
            float[] temp_leer = new float[8760 * 4];

            _chartManager[7] = new ChartManager(chart7);
            _chartManager[7].YMaxValue = temp_ges.Max() + 1;
            _chartManager[7].YMinValue = 0;
            _chartManager[7].XAxisAsNumber = false;
            _chartManager[7].XAxisTitle = "Monate";
            _chartManager[7].YAxisTitle = "Leistung";
            _chartManager[7].toolTipUnit = "kW";
            _chartManager[7].ChartTitle = "Strombedarf, Stromverbrauch Jahresganglinie";
            _chartManager[7].MitLegende = true;
            _chartManager[7].MaxXVALUE = 8760 * 4;
            _chartManager[7].MitViertelStunde = true;    
            _chartManager[7].Init();
            _chartManager[7].AddSeries("Gesamt", Color.Green, temp_ges);
            _chartManager[7].AddSeries("Waermepumpe", Color.Orange, temp_wp);
            _chartManager[7].AddSeries("Heizstab", Color.Yellow, temp_hs);
            _chartManager[7].AddSeries("Heizkessel", Color.Blue, temp_hk);
            _chartManager[7].AddSeries("Profil/Lastgang", Color.Brown, temp_profil);
            // _chartManager[7].AddSeries("Rest", Color.Black, sim.Rest_Strombedarf_viertelstuendlich);
            _chartManager[7].AddSeries("PV", Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            // _chartManager[7].AddSeries("Überschuss", Color.Magenta, sim.simulation_pv.Ueberschuss_viertelstunde);
            _chartManager[7]._chart.Series["Waermepumpe"].Enabled = false;
            _chartManager[7]._chart.Series["Heizstab"].Enabled = false;
            _chartManager[7]._chart.Series["Heizkessel"].Enabled = false;
            _chartManager[7]._chart.Series["Profil/Lastgang"].Enabled = false;
            _chartManager[7]._chart.Series["PV"].Enabled = false;

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
            if (sim == null || !sim.bSimulationWP) return;

            // 1. Hilfsarray für Heizstab vorbereiten
            float[] tempHeizstab = new float[8760];
            for (int i = 0; i < 8760; i++)
            {
                tempHeizstab[i] = ctrl.model.m_WP_Heizstab ?
                    (sim.simulation_wp.WP_Waermeproduktion_stuendlich[i] + sim.simulation_wp.Heizstab_stuendlich[i]) : 0;
            }

            // Manager referenzieren für bessere Lesbarkeit
            var manager = _chartManager[3];

            if (checkBox_WP_sortiert.Checked)
            {
                // --- SORTIERTER MODUS (Numerische X-Achse) ---
                float[] sortedWBArray = (float[])sim.simulation_wp.WP_Waermeproduktion_stuendlich.Clone();
                Array.Sort(sortedWBArray);
                Array.Reverse(sortedWBArray);

                float[] sortedBedarf = (float[])sim.simulation_wp.Waermebedarf_stuendlich.Clone();
                Array.Sort(sortedBedarf);
                Array.Reverse(sortedBedarf);

                float[] sortedHeizstab = (float[])tempHeizstab.Clone();
                Array.Sort(sortedHeizstab);
                Array.Reverse(sortedHeizstab);

                manager.XAxisAsNumber = true; // Wichtig für Init()
                manager.HardReset();
                manager.Init();

                manager.AddSeries("Wärmebedarf", Color.Red, sortedBedarf);
                manager.AddSeries("Heizstab", Color.Yellow, sortedHeizstab);
                manager.AddSeries("Wärmeproduktion", Color.Blue, sortedWBArray);
            }
            else
            {
                // --- CHRONOLOGISCHER MODUS (Datum X-Achse) ---
                manager.XAxisAsNumber = false;
                manager.HardReset();
                manager.Init(); // Hier wird FormatXAxisWithDate() aufgerufen

                manager.AddSeries("Wärmebedarf", Color.Red, sim.simulation_wp.Waermebedarf_stuendlich);
                manager.AddSeries("Heizstab", Color.Yellow, tempHeizstab);
                manager.AddSeries("Wärmeproduktion", Color.Blue, sim.simulation_wp.WP_Waermeproduktion_stuendlich);
            }

            // Skalierung erzwingen
            //manager.UpdateYScaleBasedOnVisibleSeries();
            manager._chart.Invalidate();
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

        private void checkBox_Gesamt_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Gesamt.Checked)
            {
                _chartManager[7]._chart.Series["Gesamt"].Enabled = true;
            }
            else
            {
                _chartManager[7]._chart.Series["Gesamt"].Enabled = false;
            }
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_WP.Checked)
            {
                _chartManager[7]._chart.Series["Waermepumpe"].Enabled = true;
            }
            else
            {
                _chartManager[7]._chart.Series["Waermepumpe"].Enabled = false;
            }
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Heizstab.Checked)
            {
                _chartManager[7]._chart.Series["Heizstab"].Enabled = true;
            }
            else
            {
                _chartManager[7]._chart.Series["Heizstab"].Enabled = false;
            }
        }

        private void checkBox_SPK_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SPK.Checked)
            {
                _chartManager[7]._chart.Series["Heizkessel"].Enabled = true;
            }
            else
            {
                _chartManager[7]._chart.Series["Heizkessel"].Enabled = false;
            }
        }

        private void checkBox_Profil_Lastgang_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Profil_Lastgang.Checked)
            {
                _chartManager[7]._chart.Series["Profil/Lastgang"].Enabled = true;
            }
            else
            {
                _chartManager[7]._chart.Series["Profil/Lastgang"].Enabled = false;
            }
        }
                
        private void InitTextBoxen(TabPage page)
        {
            page.Controls.OfType<TextBox>().ToList().ForEach(tb => tb.Text = "");
        }

        private void listView_SimWP_MouseDown(object sender, MouseEventArgs e)
        {
            // Prüfen, ob es ein Doppelklick (2 Klicks) mit der linken Maustaste war
            if (e.Clicks == 2 && e.Button == MouseButtons.Left)
            {
                Form_WPAuswahl frm = new Form_WPAuswahl();
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();
                WPCtrl wpctrl = new WPCtrl();
                int id_type;

                frm.list_werzmodel.Clear();
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);
                id_type = WizardItemClass.WP_TYP;

                WErzeugerModel item = new WErzeugerModel();
                for (int i = 0; i < werzctrl.rows; i++)
                {
                    frm.list_werzmodel.Add(werzctrl.items[i]);
                }

                frm.SetControls(Program.startfrm.m_szProjektname);
                DialogResult result = frm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    WizardCtrl wizctrl = new WizardCtrl();
                    wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                    wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
                }
            }
        }
  
        private void tabPage_Ergebnis_Paint(object sender, PaintEventArgs e)
        {
            // Hintergrund des Formulars (Hellgrau für den Kontrast)
            e.Graphics.Clear(Color.FromArgb(240, 240, 240));


            // Kachel für Strom
            Kacheln.DrawKPICard(e.Graphics, new Rectangle(20, 50, 220, 80),
                        "Reststrombedarf", sim.Reststrom.ToString("F2"), "MWh/a", Color.DodgerBlue);

            // Kachel für Wärme
            Kacheln.DrawKPICard(e.Graphics, new Rectangle(260, 50, 220, 80),
                        "Restwärmebedarf", restwaermebedarf.ToString("F2"), "MWh/a", Color.SeaGreen);

            // Donat Chart Farben (WP, Solar, Heizstab, Kessel, Rest)
            Color[] palette = new Color[] {
                ColorTranslator.FromHtml("#2ECC71"), // WP
                ColorTranslator.FromHtml("#E67E22"), // Solar
                ColorTranslator.FromHtml("#F1C40F"), // Heizstab
                ColorTranslator.FromHtml("#95A5A6"), // Kessel
                ColorTranslator.FromHtml("#3498DB")  // Rest
            };
  
            double[] werteArr_Prozent = new double[] { 0,0,0,0,0 };
            double wb_gesamt = 0;
            double werz_gesamt = 0;

            if (sim.simulation_Waermebedarf != null)
            {
                wb_gesamt = sim.simulation_Waermebedarf.Waermebedarf_Gesamt;
                werteArr_Prozent = new double[] { waerme_wp * 100/ wb_gesamt,
                                            waerme_solar* 100/ wb_gesamt,
                                            waerme_heizstab * 100 / wb_gesamt,
                                            waerme_spk * 100 / wb_gesamt,
                                            restwaermebedarf * 100 / wb_gesamt };
                
                werz_gesamt = waerme_wp + waerme_solar + waerme_heizstab + waerme_spk;
            }

            // Bereich für die Diagramm-Kachel definieren
            // (X=20, Y=150, Breite=220, Höhe=300)
            Rectangle kachelBereich = new Rectangle(20, label_WBDeckung.Top + label_WBDeckung.Height +10, 220, 300);

            // Die weiße Kachel zeichnen (mit der Funktion von vorhin)
            Kacheln.DrawKPICard(e.Graphics, kachelBereich, "Wärmedeckung", "", "", Color.SeaGreen);

            // Den Donut + Dynamische Legende darin aufrufen
            // Der Funktion ein etwas kleineres "Innen-Rechteck" geben, damit Abstände zum Rand bleiben
            Rectangle chartInnenBereich = new Rectangle(kachelBereich.X + 10, kachelBereich.Y + 40,
                                                       kachelBereich.Width - 20, kachelBereich.Height - 50);

            // Die Namen der 5 möglichen Quellen
            string[] quellenNamen = { "Wärmepumpe", "Solarthermie", "Heizstab", "Spitzenkessel", "Restwärme" };

            DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, chartInnenBereich, werteArr_Prozent, werz_gesamt * 100 / wb_gesamt, quellenNamen, palette);
        }

        private void btn_Zusammenfassung_Click(object sender, EventArgs e)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            DashboardForm frm = new DashboardForm();
            int id = 0;
            double speicherKWh = 0; //Standardwert, z.B. 5 kWh

            // alle Sromspeicher zum Projekt durchgehen und Leistung aufsummieren (oder direkt aus sim-Objekt, falls dort schon vorhanden)
            ctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
            for(int i=0; i<ctrl.rows; i++)
            {
                id = ctrl.items[i].ID_SP;
                RecordSet rs = new RecordSet();
                rs.Open("select * from Tab_Stromspeicher where ID=" + id);
                if (rs.Next())
                {
                    speicherKWh += (double)rs.Read("Energie");
                }
                rs.Close();
            }
            if (speicherKWh == 0) frm.speicherKWh = 5; else  frm.speicherKWh = speicherKWh;
            frm.Init();

            // Die theoretische Stromproduktion übergeben, Wirkungsgrad Wechselrichter 5% abgezogen
            frm.pvProd = sim.simulation_pv.pvPotentialGesamt_stuendlich;
            // Stromprofil + weitere Verbräuche 
            frm.stromBedarf = sim.simulation_pv.Strombedarf_stuendlich;

            // 3. Solarthermie (deine temp-Logik war okay, wenn du Erzeugung + Überschuss willst)
            float[] tempST = new float[8760];
            for (int i = 0; i < 8760; i++)
            {
                tempST[i] = (float)(sim.simulation_solarthermie.Waermeproduktion[i] + sim.simulation_solarthermie.Ueberschuss[i]);
            }
            frm.stProd = tempST;

            frm.waermeBedarf = Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermebedarf, x => (float)x);

            // Wichtig: Den Speicherwert aus dem sim-Objekt oder dem Standard-Vorgabewert setzen
            // frm.speicherKWh = ... 

            frm.UpdateSimulationData();
            frm.ShowDialog();
        }

        private void checkBox_PV_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_PV.Checked)
            {
                _chartManager[7]._chart.Series["PV"].Enabled = true;
            }
            else
            {
                _chartManager[7]._chart.Series["PV"].Enabled = false;
            }
        }

        private void checkBox_Ueberschuss_CheckedChanged(object sender, EventArgs e)
        {
            // 1. Serie im Chart suchen und umschalten
            if (chart_PV.Series.IndexOf("Überschuss") != -1)
            {
                chart_PV.Series["Überschuss"].Enabled = checkBox_Ueberschuss.Checked;
            }

            // 2. Skalierung über den Manager korrigieren
           // _chartManager[9].UpdateYScaleBasedOnVisibleSeries();
        }

        private void checkBox_Speicherzustand_CheckedChanged(object sender, EventArgs e)
        {
            double neueMax=0;

            _chartManager[9]._chart.Series["Speicherfüllstand"].Enabled = checkBox_Speicherzustand.Checked;

            if (checkBox_Speicherzustand.Checked)
            {
                neueMax = sim.Stundenwerte_zu_viertelstunden(sim.simulation_pv.Speicherfuellstand).Max() * 1.1;//sim.simulation_pv.Strombedarf.Max() + 1;
                if (neueMax < 10) neueMax = 10; // Minimum setzen, damit die Achse nicht zu klein wird
            }
            else
                neueMax = sim.simulation_pv.Strombedarf.Max() * 1.1;

            // Nur die Achse updaten ohne die Daten zu löschen:
            var ca = _chartManager[9]._chart.ChartAreas[0];

            ca.AxisY.Maximum = neueMax; // Den oben berechneten Wert direkt setzen
            ca.AxisY.Interval = 0;      // Auf Auto stellen

            // 2. Prüfen, ob die Serie existiert
            if (_chartManager[9]._chart.Series.IndexOf("Speicherfüllstand") != -1)
            {
                var s = _chartManager[9]._chart.Series["Speicherfüllstand"];
                bool anzeigen = checkBox_Speicherzustand.Checked;

                s.Enabled = anzeigen;

                if (anzeigen)
                {
                    // --- SPEZIALFALL: Y2-ACHSE AKTIVIEREN ---
                    s.YAxisType = AxisType.Secondary; // Serie nach rechts binden
                    ca.AxisY2.Enabled = AxisEnabled.True;

                    // Optik der rechten Achse
                    ca.AxisY2.Title = "Speicher [kWh]";
                    ca.AxisY2.TitleForeColor = Color.Black;
                    ca.AxisY2.LabelStyle.ForeColor = Color.Black;
                    ca.AxisY2.MajorGrid.Enabled = false; // Gitter nur links lassen

                    // Skalierung berechnen (falls nicht automatisch gewünscht)
                    if (s.Points.Count > 0)
                    {
                        double maxVal = s.Points.Max(p => p.YValues[0]);
                        ca.AxisY2.Maximum = maxVal > 0 ? maxVal * 1.1 : 10;
                    }

                    // Den inneren Bereich schrumpfen, damit rechts Platz für die 2. Achse ist
                    ca.InnerPlotPosition.Auto = false;
                    ca.InnerPlotPosition.X = 10;      // Start links
                    ca.InnerPlotPosition.Width = 75;  // Vorher ca. 85, jetzt weniger für Y2-Platz
                    ca.InnerPlotPosition.Y = 8;
                    ca.InnerPlotPosition.Height = 75;

                    // Sicherstellen, dass die Achse nicht abgeschnitten wird
                    ca.AxisY2.LabelStyle.Enabled = true;

                }
                else
                {
                    // Y2-Achse wieder verstecken, wenn Speicher aus
                    ca.AxisY2.Enabled = AxisEnabled.False;
                }
            }

    


            ca.RecalculateAxesScale();
            _chartManager[9]._chart.Invalidate();
            return;








            // --- Y-ACHSE ---
            _chartManager[9]._chart.ChartAreas[0].AxisY.LabelStyle.Format = "N0";
            _chartManager[9]._chart.ChartAreas[0].AxisY.IsLabelAutoFit = false;
            double range = (_chartManager[9].YMaxValue != 0 ? _chartManager[9].YMaxValue : 100) - _chartManager[9].YMinValue;
            //double interval = Math.Max(1, Math.Ceiling(range / 10));
            // Wenn du maximal 10 Labels willst:
            // Wir teilen die Range durch 10. 
            // Math.Max(1, ...) verhindert ein Intervall von 0 (Endlosschleife/Crash)
            //double interval = range / 10.0;
            double interval = _chartManager[9].CalculateNiceInterval(range, 8);
            //interval = Math.Ceiling(interval / 5.0) * 5.0;
            
            if (interval < 1.0)
            {
                // Zeige 1 oder 2 Nachkommastellen, wenn das Intervall klein ist
                _chartManager[9]._chart.ChartAreas[0].AxisY.LabelStyle.Format = "N1";
            }
            else
            {
                _chartManager[9]._chart.ChartAreas[0].AxisY.LabelStyle.Format = "N0";
            }
            _chartManager[9].RefreshChart();
            
            if (chart_PV.Series.IndexOf("Speicherfüllstand") != -1)
                _chartManager[9].YMaxValue = sim.Stundenwerte_zu_viertelstunden(sim.simulation_pv.Speicherfuellstand).Max() * 1.1;//sim.simulation_pv.Strombedarf.Max() + 1;
            else
                _chartManager[9].YMaxValue = sim.simulation_pv.Strombedarf.Max() * 1.1;

            // NUR DER SPEICHER geht auf die rechte Achse (true = Sekundärachse kWh)
            _chartManager[9].AddSeries("Speicherfüllstand", Color.Gold, sim.simulation_pv.Speicherfuellstand_viertelstunde);
            _chartManager[9].AddSeries("Überschuss", Color.Yellow, sim.simulation_pv.Ueberschuss_viertelstunde);
            _chartManager[9].AddSeries("Strombedarf", Color.Red, sim.simulation_pv.Strombedarf);
            _chartManager[9].AddSeries("Photovoltaik", Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            _chartManager[9]._chart.Series["Speicherfüllstand"].Enabled = checkBox_Speicherzustand.Checked;
            _chartManager[9]._chart.Invalidate();
        }
    }

}
