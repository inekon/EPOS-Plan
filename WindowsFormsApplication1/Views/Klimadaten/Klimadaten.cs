using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
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
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;
            ca.CursorX.AutoScroll = true;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisX.ScaleView.MinSize = 0;
            ca.CursorX.Interval = 0;
            ca.CursorX.SelectionColor = Color.FromArgb(100, 100, 0, 0);
            ca.CursorY.SelectionColor = Color.FromArgb(100, 100, 0, 0);
        }

        private void listBoxWP_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_szOrtName = listBoxKlimreg.Text;
            CreateChart();
        }

        private void CreateChart()
        {
            List<Double> yAxis = new List<Double>();
            KlimaregionCtrl ctrlregion = new KlimaregionCtrl();
            SolardatenCtrl ctrl = new SolardatenCtrl();

            ctrlregion.ReadSingle("Select * from Tab_Klimaregion where Name = '" + m_szOrtName + "'");
            textBox_Display.Text = ctrlregion.Details.ToString();
            textBox_Bezeichnung.Text = ctrlregion.m_szName.ToString();   
            textBox_Latitude.Text = ctrlregion.Latitude.ToString();
            textBox_Longitude.Text = ctrlregion.Longitude.ToString();
            int ID_Klimaregion = ctrlregion.m_ID_Klimaregion;

            ctrl.ReadAll(ID_Klimaregion);

            // Chart Temperaturverlauf
            yAxis = ctrl.list_Temperatur;

            ChartManager _chartManager = new ChartManager(chart1);
            _chartManager.YMaxValue = yAxis.ToArray().Max();
            _chartManager.YMinValue = yAxis.ToArray().Min();
            _chartManager.XAxisAsNumber = false;
            _chartManager.XAxisTitle = "Jahresstunden";
            _chartManager.YAxisTitle = "Temperatur";
            _chartManager.toolTipUnit = "°C";
            _chartManager.ChartTitle = "Jahrestemperatur Verlauf";
            _chartManager.MitLegende = false;
            _chartManager.BackColor = Color.FromArgb(245, 247, 249);
            _chartManager.Init();
            _chartManager.AddSeries("Temperatur", Color.Blue, Array.ConvertAll<double, float>(yAxis.ToArray(), x => (float)x));

            // Chart Sonnenwinkel
            yAxis = ctrl.list_Sonnenwinkel;

            ChartManager _chartManager2 = new ChartManager(chart2);
            _chartManager2.YMaxValue = yAxis.ToArray().Max();
            _chartManager2.YMinValue = 0;
            _chartManager2.XAxisAsNumber = false;
            _chartManager2.XAxisTitle = "Jahresstunden";
            _chartManager2.YAxisTitle = "Sonnenwinkel";
            _chartManager2.toolTipUnit = "°";
            _chartManager2.ChartTitle = "Sonnenwinkel Verlauf";
            _chartManager2.MitLegende = false;
            _chartManager2.BackColor = Color.FromArgb(245, 247, 249);
            _chartManager2.Init();
            _chartManager2.AddSeries("Sonnenwinkel", Color.Orange, Array.ConvertAll<double, float>(yAxis.ToArray(), x => (float)x));
 
        }

        private void btn_Delete_Click(object sender, EventArgs e)
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

        private async void btn_Import_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrlklimareg = new KlimaregionCtrl();
            double ghi; double dni; double dhi; double t2m;
            List<double> sonnenwinkel = new List<double>();

            // PVGIS nutzt Punkt als Dezimaltrenner, daher muss die InvariantCulture verwendet werden
            var culture = CultureInfo.InvariantCulture;

            pBar_Import.Maximum = 7;
            pBar_Import.Value = 1;
            textBox_Display.Text = "";
            bool Success; double Lat; double Lon; string DisplayName; string Listbezeichner;

            if (comboBox_Ort.Text != "")
            {
                // Koordinaten für den Ort ermitteln
                // wenn in DB schon vorhanden, dann nicht importieren
                if (listBoxKlimreg.FindString(comboBox_Ort.Text) != -1) return;
                pBar_Import.Visible = true;
                (Success, Lat, Lon, DisplayName) = await PVGIS_EPW_Downloader.GetCoordinatesAsync(comboBox_Ort.Text);
                if (!Success) { pBar_Import.Visible = false; MessageBox.Show("Der Ort '" + comboBox_Ort.Text + "'konnte nicht ermittelt werden..."); return; }
                Listbezeichner = comboBox_Ort.Text; 
            }
            else
            {
                // Eingabe überprüfen
                if (textBox_Latitude.Text == "" || textBox_Longitude.Text == "" || textBox_Bezeichnung.Text == "") { MessageBox.Show("Eingaben überprüfen!"); textBox_Bezeichnung.Focus(); return; }
                pBar_Import.Visible = true;
                textBox_Longitude.Text = textBox_Longitude.Text.Replace('.', ','); 
                Lon = Convert.ToDouble(textBox_Longitude.Text);
                textBox_Latitude.Text = textBox_Latitude.Text.Replace('.', ',');
                Lat = Convert.ToDouble(textBox_Latitude.Text);
                DisplayName = "";
                Listbezeichner = textBox_Bezeichnung.Text;
            }

            pBar_Import.Value += 1;
            textBox_Display.Text = DisplayName;
            textBox_Latitude.Text = Lat.ToString();
            textBox_Longitude.Text = Lon.ToString();


            // TMY Daten von PVGIS herunterladen, berechnen nach Ost, Süd, West, Nord und in Listen speichern
            List<TmyHourlyData> tmyHourlyList = await PVGIS_EPW_Downloader.GetTMY(Lon, Lat, 0);
            if (tmyHourlyList == null) { pBar_Import.Visible = false; return; } 
            
            List<TmyHourlyData> tmyHourlyList_ost = new List<TmyHourlyData>();
            List<TmyHourlyData> tmyHourlyList_sued = new List<TmyHourlyData>();
            List<TmyHourlyData> tmyHourlyList_west = new List<TmyHourlyData>();
            List<TmyHourlyData> tmyHourlyList_nord = new List<TmyHourlyData>();

            // Anzeige der Datenquelle in der GUI
            textBox_Display.Text = PVGIS_EPW_Downloader.meteoDb + ": " + textBox_Display.Text;
            DisplayName = textBox_Display.Text; 

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
            if (!ctrlklimareg.Add(Listbezeichner, Lon, Lat, DisplayName)) return;
            ctrlklimareg.ReadSingle("SELECT * FROM Tab_Klimaregion where Name = '" + Listbezeichner + "'");
            int id = ctrlklimareg.m_ID_Klimaregion;
            if (id == 0) return;

            pBar_Import.Value += 1; pBar_Import.Update();

            // Tabelle Solar (Stundenwerte) schreiben
            AccessRepository repo = new AccessRepository(db);
            repo.SaveTmyData(tmyHourlyList, Listbezeichner, "Tab_Solar", id);

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
          
        private void textBox_Longitude_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Clear();
        }

        private void textBox_Latitude_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Clear();
        }

        private void panel_KlimaGraph_Paint(object sender, PaintEventArgs e)
        {
            Control p = (Control)sender;
            // Zeichne nur den farbigen Akzentbalken oben (z.B. Blau für Klima)
            using (SolidBrush b = new SolidBrush(Color.DodgerBlue))
            {
                e.Graphics.FillRectangle(b, 0, 0, p.Width, 5);
            }

            // Optional: Ein ganz feiner Rahmen, falls Schatten zu schwer ist
            using (Pen pen = new Pen(Color.FromArgb(220, 220, 220)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            }
        }
    }

}
