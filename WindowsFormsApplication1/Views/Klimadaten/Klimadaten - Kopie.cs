//using MathNet.Numerics;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{


    public partial class Form_Klimadaten : Form
    {
        private string m_szExcelBasName;
        double ChartSelBegin;
        double ChartSelEnd;

        public Form_Klimadaten()
        {
            InitializeComponent();
            m_szExcelBasName = "";
        }

        private void Form_Klimadaten_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            KlimaregionCtrl krclass = new KlimaregionCtrl();
            krclass.ReadAll();
            krclass.FillListBox(listBoxKlimreg);
            krclass = null;
            
            chart1.Series.Clear();
            var ca = chart1.ChartAreas[0];
            ca.CursorX.IsUserEnabled = false;
            ca.CursorX.IsUserSelectionEnabled = false;
            ca.CursorY.IsUserEnabled = false;
            ca.CursorY.IsUserSelectionEnabled = false;
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;
            ca.CursorX.AutoScroll = true;
            ca.CursorX.IsUserSelectionEnabled = true;
        }

        private void butt_WP_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl krclass = new KlimaregionCtrl();
            krclass.ReadAll();
            krclass.FillListBox(listBoxKlimreg);
            krclass = null;
        }

        private void listBoxWP_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_szExcelBasName = listBoxKlimreg.Text;
            CreateChart();
        }

        private void CreateChart()
        {
            List<int> xAxis = new List<int>();
            List<Double> yAxis = new List<Double>();
            KlimaregionCtrl ctrlregion = new KlimaregionCtrl();
            KlimadatenCtrl ctrl = new KlimadatenCtrl();

            ctrlregion.ReadSingle("Select * from Tab_Klimaregion where Name = '" + m_szExcelBasName + "'");
            int ID_Klimaregion = ctrlregion.m_ID_Klimaregion;

            ctrl.ReadAll(ID_Klimaregion);

            var series = new Series("Jahrestemperatur");
            chart1.Series.Clear();
            
            yAxis = ctrl.list_Temperatur;
            xAxis = ctrl.list_Tag;  
            chart1.Series.Add(series);
            series.ChartType = SeriesChartType.Line;
   
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
                    rs.Close();
                    MessageBox.Show("Löschen nicht möglich!\nDiese Klimaregion ist dem Projekt " + rs.Read("Projektname")+ " zugeordnet!", "Hinweis");
                    return;
                }
                rs.Close();

                KlimaregionCtrl krclass = new KlimaregionCtrl();
                krclass.Delete(listBoxKlimreg.Text);
                krclass.ReadAll();
                krclass.FillListBox(listBoxKlimreg);
                krclass = null;
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

        private void SelectionRangeBegin(object sender, CursorEventArgs e)
        {
            ChartSelBegin = chart1.ChartAreas[0].CursorX.SelectionStart;
            ChartSelEnd = chart1.ChartAreas[0].CursorX.SelectionEnd;
        }

        private void SelectionRangeEnd(object sender, CursorEventArgs e)
        {
            if (!Program.HasValue(ChartSelBegin)) return;
            
            DateTime date = new DateTime(DateTime.Now.Year, 1, 1);
            date = date.AddDays(ChartSelBegin - 1);

            DateTime date2 = new DateTime(DateTime.Now.Year, 1, 1);
            date2 = date2.AddDays(ChartSelEnd - 1);
        
            TextAnnotation ta2 = new TextAnnotation();
            ta2.Text = date.ToString("dd.MMMM") + " bis " + date2.ToString("dd.MMMM");
            ta2.AnchorX = 18 ;  // % of chart width
            ta2.AnchorY = 98;  // % of chart height, from top
            if (chart1.Annotations.Count == 0)
            {
                chart1.Annotations.Add(ta2);
            }
            else
            {
                chart1.Annotations[0] = ta2;
            }
        }

        private void AxisScrollBarClicked(object sender, ScrollBarEventArgs e)
        {
            if (e.ButtonType == ScrollBarButtonType.ZoomReset)
            {
                chart1.Annotations.Clear();
            }
            else
            {
                ChartSelBegin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                ChartSelEnd = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
                DateTime date = new DateTime(2025, 1, 1);
                date = date.AddDays(ChartSelBegin - 1);

                DateTime date2 = new DateTime(2025, 1, 1);
                date2 = date2.AddDays(ChartSelEnd - 1);

                TextAnnotation ta2 = new TextAnnotation();
                ta2.Text = date.ToString("dd.MMMM") + " bis " + date2.ToString("dd.MMMM");
                ta2.AnchorX = 18;  // % of chart width
                ta2.AnchorY = 98;  // % of chart height, from top
                if (chart1.Annotations.Count == 0)
                {
                    chart1.Annotations.Add(ta2);
                }
                else
                {
                    chart1.Annotations[0] = ta2;
                }
            }
        }

        public void ConfigureXAxisWithMonths(Chart chartControl)
        {
            // Define your custom labels in an array
            string[] monthArray = { "1", "2", "3", "4", "5", "6", "7", "8", "8", "10", "11", "12" };

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

        private void button1_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrlklimareg = new KlimaregionCtrl();
            pBar_Import.Maximum = 3;
            pBar_Import.Value = 1;
            pBar_Import.Visible = true;

            PVGIS_EPW_Downloader dl = new PVGIS_EPW_Downloader();
            string ort = comboBox_Ort.Text;
            dl.GetData(ort);

            textBox_Display.Text = PVGIS_EPW_Downloader.displayName;
            return;

            dl.GetData(ort).ContinueWith(task =>
            {
                // Prüfen, ob der Task abgebrochen wurde, bevor UI-Logik läuft
                if (task.IsCanceled) return;

                if (task.Status == TaskStatus.RanToCompletion)
                {
                    this.Invoke((Action)(() => {
                        textBox_Display.Text = PVGIS_EPW_Downloader.displayName;
                        textBox_Display.Update();
                        textBox_Longitude.Text = PVGIS_EPW_Downloader.longitude.ToString();
                        textBox_Latitude.Text = PVGIS_EPW_Downloader.latitude.ToString();
                        pBar_Import.Value += 1;
                    }));


                    List<TmyHourlyData> tmyHourlyList = task.Result;
                    PvgisOutputs ret = new PvgisOutputs { TmyHourly = tmyHourlyList }; // Warten auf den Abschluss der asynchronen Methode   

                    string db = "";
                    string userPath = $@"SOFTWARE\ODBC\ODBC.INI\TEST";
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(userPath))
                    {
                        if (key != null)
                        {
                            db = key.GetValue("DBQ")?.ToString() ?? key.GetValue("Database")?.ToString();
                        }
                    }

                    AccessRepository repo = new AccessRepository(db);
                    repo.SaveTmyData(tmyHourlyList, ort);
                    this.Invoke((Action)(() => pBar_Import.Value += 1));
                    Thread.Sleep(1000);
                    this.Invoke((Action)(() => pBar_Import.Visible = false));
                    ctrlklimareg.ReadAll();
                    this.Invoke((Action)(() => ctrlklimareg.FillListBox(listBoxKlimreg)));
                    this.Invoke((Action)(() => listBoxKlimreg.SelectedIndex = listBoxKlimreg.FindString(ort)));
                    
                }
                else if (task.IsFaulted)
                {
                    // Fehlerbehandlung
                    MessageBox.Show("Fehler beim Abrufen der Daten: " + task.Exception?.Message);
                    this.Invoke((Action)(() => pBar_Import.Visible = false));
                }
            });
    
  
    
        }

        private void comboBox_Ort_Click(object sender, EventArgs e)
        {
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
        }

        private void btn_Koordinaten_Click(object sender, EventArgs e)
        {
            if (comboBox_Ort.Text == "") return;
            btn_Import.PerformClick();
        }
    }

}
