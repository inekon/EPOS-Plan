using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{

    public partial class Form_Klimadaten : Form
    {
        private string m_szOrtName;

        public Form_Klimadaten()
        {
            InitializeComponent();
            m_szOrtName = "";
        }

        private void Form_Klimadaten_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            KlimaregionCtrl krclass = new KlimaregionCtrl();
            krclass.ReadAll();
            krclass.FillListBox(listBoxKlimreg);

            comboBox_Ort.Items.Clear();

            string szPath = Path.Combine(Program.ApplicationPath_User, "Ortsliste");

            // Windows-1252 ist der Standard für deutsche Textdateien, 
            // die oft als "ASCII" bezeichnet werden
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding enc = Encoding.GetEncoding("Windows-1252");

            string[] lines = File.ReadAllLines(szPath + "\\Ortsnamen.txt", enc);
            foreach (string line in lines)
            {
                Console.WriteLine(line);
                comboBox_Ort.Items.Add(line);
            }

            initChart(chart1);
            initChart(chart2);
            chart2.ChartAreas[0].AxisY.MajorGrid.Interval = 10;
        }

        private void initChart(Chart chart)
        {
            chart.Series.Clear();
            var ca = chart.ChartAreas[0];
            ca.CursorX.IsUserEnabled = false;
            ca.CursorX.IsUserSelectionEnabled = false;
            ca.CursorY.IsUserEnabled = false;
            ca.CursorY.IsUserSelectionEnabled = false;
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;
            ca.CursorX.AutoScroll = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
        }
        
        private void listBoxWP_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_szOrtName = listBoxKlimreg.Text;
            CreateChart();
        }

        private void CreateChart()
        {
            List<int> xAxis = new List<int>();
            List<Double> yAxis = new List<Double>();
            KlimaregionCtrl ctrlregion = new KlimaregionCtrl();
            SolardatenCtrl ctrl = new SolardatenCtrl();
            Series series;

            ctrlregion.ReadSingle("Select * from Tab_Klimaregion where Name = '" + m_szOrtName + "'");
            int ID_Klimaregion = ctrlregion.m_ID_Klimaregion;

            ctrl.ReadAll(ID_Klimaregion);

            if (chart1.Series.Count == 0)
            {
                series = new Series("Jahrestemperatur");
                chart1.Series.Add(series);
            }
            else
            {
                series = chart1.Series[0];
                chart1.Series[0].Points.Clear();
            }
            series.ChartType = SeriesChartType.Line;
            yAxis = ctrl.list_Temperatur;
            xAxis = ctrl.list_Tag;  
            ConfigureXAxisWithMonths(chart1);

            for (int j = 0; j < 8760; j++)
            {
                double d = (double)j * 12 / (8760);
                chart1.Series[0].Points.AddXY(d, yAxis[j]);
            }
            chart1.Series[0].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
            chart1.Series[0].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
            chart1.Series[0].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
            chart1.Series[0].IsValueShownAsLabel = false;
            chart1.Series[0].BorderWidth = 2;
            chart1.Update();

            if (chart2.Series.Count == 0)
            {
                series = new Series("Sonnenwinkel");
                chart2.Series.Add(series);
            }
            else
            {
                series = chart2.Series[0];
                chart2.Series[0].Points.Clear();
            }
            series.ChartType = SeriesChartType.Line;
            yAxis = ctrl.list_Sonnenwinkel;
            xAxis = ctrl.list_Tag;
            ConfigureXAxisWithMonths(chart2);

            for (int j = 0; j < 8760; j++)
            {
                double d = (double)j * 12 / (8760);
                chart2.Series[0].Points.AddXY(d, yAxis[j]);
            }
            chart2.Series[0].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
            chart2.Series[0].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
            chart2.Series[0].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
            chart2.Series[0].IsValueShownAsLabel = false;
            chart2.Series[0].BorderWidth = 2;
            chart2.Update();

        }

        private void butt_Delete_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            
            if (listBoxKlimreg.SelectedIndex != -1)
            {
                //ist diese Klimaregion einem Projekt zugeordnet?
                string sql = "SELECT Tab_Projekt.Projektname, Tab_Klimaregion.Name " +
                            "FROM Tab_Klimaregion INNER JOIN Tab_Projekt ON " +
                            "Tab_Klimaregion.ID_Klimaregion = Tab_Projekt.ID_Klimaregion where " +
                            "Tab_Klimaregion.Name ='" + listBoxKlimreg.Text + "'";

                rs.Open(sql);
                if (rs.Next())
                {
                    MessageBox.Show("Löschen nicht möglich!\nDiese Klimaregion ist dem Projekt " + rs.Read("Projektname")+ " zugeordnet!", "Hinweis");
                    rs.Close();
                    return;
                }
                rs.Close();

                KlimaregionCtrl krclass = new KlimaregionCtrl();
                krclass.Delete(listBoxKlimreg.Text);
                krclass.ReadAll();
                krclass.FillListBox(listBoxKlimreg);
                if(listBoxKlimreg.Items.Count > 0) listBoxKlimreg.SelectedIndex = 0; else chart1.Series.Clear();
            }
            else
            {
                MessageBox.Show("Klimaregion auswählen", "Hinweis");
            }
        }

        public void IncrPBar()
        {
            pBar_Import.Value += 1;
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AxisScrollBarClicked(object sender, ScrollBarEventArgs e)
        {
            if (e.ButtonType == ScrollBarButtonType.ZoomReset)
            {
                while (chart1.Annotations.Count > 0)
                {
                    chart1.Annotations.RemoveAt(0);
                }
                chart1.Update();
            }
        }

        public void ConfigureXAxisWithMonths(Chart chartControl)
        {
            // Define your custom labels in an array
            string[] monthArray = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };

            chartControl.ChartAreas[0].AxisX.CustomLabels.Clear();
            //chartControl.Annotations.Clear();
            //chartControl.Series[0].Points.Clear();

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

        private void comboBox_Ort_Click(object sender, EventArgs e)
        {
        }

        private async void btn_Import_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrlklimareg = new KlimaregionCtrl();
            double ghi; double dni; double dhi; double t2m;
            List<double> sonnenwinkel = new List<double>();

            // PVGIS nutzt Punkt als Dezimaltrenner, daher muss die InvariantCulture verwendet werden
            var culture = CultureInfo.InvariantCulture;

            // wenn in DB schon vorhanden, dann nicht importieren
            if (listBoxKlimreg.FindString(comboBox_Ort.Text) != -1) return;

            pBar_Import.Maximum = 7;
            pBar_Import.Value = 1;
            pBar_Import.Visible = true;

            // Koordinaten für den Ort ermitteln
            var (Success, Lat, Lon, DisplayName) = await PVGIS_EPW_Downloader.GetCoordinatesAsync(comboBox_Ort.Text);
            if (!Success) { pBar_Import.Visible = false; MessageBox.Show("Der Ort '" + comboBox_Ort.Text + "'konnte nicht ermittelt werden..."); return; }  

            pBar_Import.Value += 1;
            textBox_Display.Text = DisplayName;
            textBox_Latitude.Text = Lat.ToString();
            textBox_Longitude.Text = Lon.ToString();

            // TMY Daten von PVGIS herunterladen, berechnen nach Ost, Süd, West, Nord und in Listen speichern
            List<TmyHourlyData> tmyHourlyList = await PVGIS_EPW_Downloader.GetTMY(Lon, Lat, 0);
            List<TmyHourlyData> tmyHourlyList_ost = new List<TmyHourlyData>();
            List<TmyHourlyData> tmyHourlyList_sued = new List<TmyHourlyData>();
            List<TmyHourlyData> tmyHourlyList_west = new List<TmyHourlyData>();
            List<TmyHourlyData> tmyHourlyList_nord = new List<TmyHourlyData>();

            // Anzeige der Datenquelle in der GUI
            textBox_Display.Text = PVGIS_EPW_Downloader.meteoDb + ": " + textBox_Display.Text;

            // PVGIS Parameter für Solarberechnung:
            // <param name="dni">Gb(n) aus TMY</param>
            // <param name="dhi">Gd(h) aus TMY</param>
            // <param name="ghi">G(h) aus TMY</param>
 
            for (int i=0; i < tmyHourlyList.Count; i++)
            {
                ghi = tmyHourlyList[i].GlobalIrradiance;
                dni = tmyHourlyList[i].DirectIrradiance;
                dhi = tmyHourlyList[i].DiffuseIrradiance;
                t2m = tmyHourlyList[i].Temperature;

                DateTime dt = DateTime.ParseExact(tmyHourlyList[i].TimeString, "yyyyMMdd:HHmm", culture);
                tmyHourlyList[i].Sol_sued = SolarCalculator.CalculateHourly(Lon, Lat, 90, 0, ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);
                tmyHourlyList[i].Sol_ost = SolarCalculator.CalculateHourly(Lon, Lat, 90, -90, ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);
                tmyHourlyList[i].Sol_nord = SolarCalculator.CalculateHourly(Lon, Lat, 90, 180, ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);
                tmyHourlyList[i].Sol_west = SolarCalculator.CalculateHourly(Lon, Lat, 90, 90, ghi, dni, dhi, t2m, dt.DayOfYear, dt.Hour);
                sonnenwinkel.Add(SolarCalculator.sonnenwinkel);
                tmyHourlyList[i].Sonnenwinkel = SolarCalculator.sonnenwinkel;
            }

            //File.WriteAllLines("c:\\temp\\sonnenwinkel.txt", sonnenwinkel.Select(d => d.ToString()));
            pBar_Import.Value += 1; pBar_Import.Update();

            // Datenbankpfad fpr OleDb ermitteln aus DSN ODBC Info
            // Klimadaten werden mit OleDb Transactions geschrieben,
            // da es sich um viele Datensätze handelt und damit die Performance deutlich besser ist als mit ODBC
            string db = "";
            string userPath = $@"SOFTWARE\ODBC\ODBC.INI\TEST";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(userPath))
            {
                if (key != null)
                {
                    db = key.GetValue("DBQ")?.ToString() ?? key.GetValue("Database")?.ToString();
                }
            }

            pBar_Import.Value += 1; pBar_Import.Update();

            // Tabelle Klimaregion schreiben
            if (!ctrlklimareg.Add(comboBox_Ort.Text, Lon, Lat)) return;
            ctrlklimareg.ReadSingle("SELECT * FROM Tab_Klimaregion where Name = '" + comboBox_Ort.Text + "'");
            int id = ctrlklimareg.m_ID_Klimaregion;
            if (id == 0) return;

            pBar_Import.Value += 1; pBar_Import.Update();

            // Tabelle Solar (Stundenwerte) schreiben
            AccessRepository repo = new AccessRepository(db);
            repo.SaveTmyData(tmyHourlyList, comboBox_Ort.Text,"Tab_Solar", id);

            // Tageswerte für Tabelle_Klimadaten
            var daylist = SolarCalculator.GetDailyAverages(tmyHourlyList);
   
            for (int i = 0; i < daylist.Count; i++)
            {
                // Tagverteilungstyp für Nicht Wohngebäude bestimmen: 1-4 für Saison, 5-8 für Saison+WE, 1/5=Winter, 3/7=Sommer, 2/6=Frühling/Herbst
                DateTime date = DateTime.ParseExact(daylist[i].TimeString, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                daylist[i].TagTyp_NW = GetSeasonalValue(date);
                
                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                daylist[i].WE = isWeekend;
                
                // Tagverteilungstyp für Wohngebäude bestimmen:
                // wenn Diffusstrahlung mehr als 50% der Globalstrahlung, dann wolkig, sonst sonnig
                if (daylist[i].DiffuseIrradiance > (0.5 * daylist[i].GlobalIrradiance)) daylist[i].TagTyp_W = 2; else daylist[i].TagTyp_W = 1;
            }
            
            // Tabelle Tab_Klimadaten (täglich) schreiben
            repo.SaveTmyData(daylist, comboBox_Ort.Text, "Tab_Klimadaten", id);
            pBar_Import.Value += 1; pBar_Import.Update();

            // GUI aktualisieren
            ctrlklimareg.ReadAll();
            ctrlklimareg.FillListBox(listBoxKlimreg);
            listBoxKlimreg.SelectedIndex = listBoxKlimreg.FindString(comboBox_Ort.Text);

            pBar_Import.Value = pBar_Import.Maximum; pBar_Import.Update();
            pBar_Import.Visible = false;
        }

        private int GetSeasonalValue(DateTime date)
        {
            // Bestimmen, ob es ein Wochenende ist (Samstag oder Sonntag)
            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            // Das Quartal ermitteln (1 bis 4)
            int quarter = (date.Month - 1) / 3 + 1;

            switch(quarter)
            {
                case 1: if (isWeekend) return 2; else return 1;
                case 2: if (isWeekend) return 4; else return 3;
                case 3: if (isWeekend) return 6; else return 5;
                case 4: if (isWeekend) return 8; else return 7;
            };
            return 0;
        }

        private void chart1_AxisViewChanged(object sender, ViewEventArgs e)
        {
            // Wir interessieren uns nur für die X-Achse
            if (e.Axis.AxisName == AxisName.X)
            {
                double startValue = e.Axis.ScaleView.ViewMinimum;
                double endValue = e.Axis.ScaleView.ViewMaximum;

                TextAnnotation ta ;
    
                if (chart1.Annotations.Count == 0)
                {
                    ta = new TextAnnotation();
                    chart1.Annotations.Add(ta);
                }
                else
                {
                    ta= (TextAnnotation)chart1.Annotations[0];
                }
                ta.AnchorX = 18;  // % der chart width, von links
                ta.AnchorY = 98;  // % der chart height, von oben

                // Prüfen, ob wir im "Total-View" sind oder gezoomt haben
                if (startValue == 0 && endValue ==12)
                {
                    ta.Text = "Anzeige: Ganzes Jahr";
                }
                else
                {
                    // Umrechnung Index -> Datum
                    int year = DateTime.Now.Year;
                    DateTime startDate = new DateTime(year, 1, 1).AddHours((int)((startValue/12)*8760));
                    DateTime endDate = new DateTime(year, 1, 1).AddHours((int)Math.Min(8759, (endValue/12)*8760));
                    ta.Text = $"Bereich: {startDate:dd.MM.} bis {endDate:dd.MM.yyyy}";
                }
            }
        }

    }

}
