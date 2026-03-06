using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    internal class ChartManager
    {
        public double YMaxValue = 0;
        public double YMinValue = 0;
        public bool XAxisAsNumber = false;
        public bool WheelZoomed = true;
        public string toolTipUnit = "";
        public string XAxisTitle = "Zeitverlauf (Jahresstunden)";
        public string YAxisTitle = "";
        public string ChartTitle = "";
        public bool MitChartBorder = false;
        public bool MitLegende = false;
        public bool IsQuarterHourly = false;
        public int MaxXVALUE = 8760;

        public Chart _chart = null;
        public ChartMouseWheel2 _chartWheelManager;

        public ChartManager(Chart chart)
        {
            _chart = chart;
        }

        public void Init()
        {
            if (_chart.ChartAreas.Count == 0) return;
            ChartArea ca = _chart.ChartAreas[0];

            // --- LEGENDE KONFIGURIEREN ---
            _chart.Legends.Clear();
            if (MitLegende)
            {
                Legend leg = new Legend("MainLegend");
                leg.Docking = Docking.Bottom;      // Unten platzieren
                leg.Alignment = StringAlignment.Center;
                leg.BackColor = Color.Transparent; // Passt sich dem Mint-Grün an
                leg.Font = new Font("Arial", 8);
                _chart.Legends.Add(leg);
            }

            // --- WICHTIG: PLATZ FÜR LEGENDE LASSEN ---
            // Wenn die Legende unten ist, müssen wir die Höhe der Grafik (Height) 
            // etwas verringern, damit die Legende nicht über die X-Achse gezeichnet wird.
            ca.InnerPlotPosition.Auto = false;
            ca.InnerPlotPosition.X = 12;
            ca.InnerPlotPosition.Y = 8;
            ca.InnerPlotPosition.Width = 85;
            ca.InnerPlotPosition.Height = 70; // Vorher 78 -> jetzt 70 für Platz unten

            _chart.Titles.Clear(); // Alte Titel entfernen
            Title mainTitle = new Title();
            mainTitle.Text = ChartTitle;
            mainTitle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            mainTitle.ForeColor = Color.FromArgb(64, 64, 64); // Dunkelgrau für gute Lesbarkeit
            mainTitle.Alignment = ContentAlignment.TopCenter;

            // Optional: Schatten oder Hintergrund für den Titel
            // mainTitle.ShadowOffset = 1;

            _chart.Titles.Add(mainTitle);

            // --- Hintergrundfarbe & Gradient (122; 255; 222) ---
            ca.BackColor = Color.FromArgb(122, 255, 222);
            ca.BackGradientStyle = GradientStyle.DiagonalLeft;
            ca.BackSecondaryColor = Color.White; // Erzeugt den Verlaufseffekt

            // --- Rahmen ---
            _chart.BorderlineDashStyle = ChartDashStyle.Dot;
            _chart.BorderlineWidth = 0;
            if(MitChartBorder) _chart.BorderlineWidth = 1;
            ca.BorderDashStyle = ChartDashStyle.DashDot;
            ca.BorderColor = Color.Black;

            // --- Y-Achsen Setup & Beschriftung ---
            ca.AxisY.Title = YAxisTitle + " [" + toolTipUnit + "]";
            // --- Y-ACHSE (Gerade Zahlen & Formatierung) ---
            ca.AxisY.LabelStyle.Format = "N0"; // "N0" entfernt die Nachkommastellen (zeigt nur Ganzzahlen)
            ca.AxisY.IsLabelAutoFit = false;

            // Berechnung eines sauberen, ganzzahligen Intervalls
            double range = (YMaxValue != 0 ? YMaxValue : 100) - YMinValue;
            // Wählt ein Intervall, das durch 2, 5 oder 10 teilbar ist, um "gerade" Ansichten zu erzeugen
            double interval = Math.Ceiling(range / 10);
            if (interval < 1) interval = 1; // Sicherstellen, dass es nicht 0 wird

            ca.AxisY.Interval = interval;
            ca.AxisY.MajorGrid.Interval = interval;

            ca.AxisY.Maximum = YMaxValue != 0 ? YMaxValue : double.NaN;
            ca.AxisY.Minimum = YMinValue;
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.DashDot;
            ca.AxisY.MajorGrid.LineColor = Color.FromArgb(150, 150, 150);

            // --- X-Achsen Setup ---
            ca.AxisX.ScaleView.Zoomable = true;
            ca.AxisX.ScrollBar.IsPositionedInside = true;
            ca.AxisX.LabelStyle.Font = new Font("Segoe UI;", 8, FontStyle.Regular);
            ca.AxisX.LabelStyle.ForeColor = Color.FromArgb(64, 64, 64);

            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
            ca.AxisX.MajorGrid.LineColor = Color.FromArgb(140, 140, 140);
            ca.AxisX.MinorGrid.LineDashStyle = ChartDashStyle.DashDot;
            ca.AxisX.MinorGrid.LineColor = Color.FromArgb(180, 180, 180);

            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorX.Interval = 0;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;
            ca.CursorY.Interval = 0;

            // Mittlere Tiefe (gut sichtbar, 120 von 255)
            ca.CursorX.SelectionColor = Color.FromArgb(120, 0, 120, 215);
            ca.CursorY.SelectionColor = Color.FromArgb(120, 0, 120, 215);

            if (XAxisAsNumber)
                FormatXAxisWithNumber();
            else
                FormatXAxisWithDate();

            if (WheelZoomed)
            {
                _chartWheelManager = new ChartMouseWheel2(_chart, IsQuarterHourly);
                _chartWheelManager.szToolTipUnit = toolTipUnit;
                _chartWheelManager.IsNumericAxis = XAxisAsNumber;
            }

            _chart.Series.Clear();
            _chart.Invalidate();
        }

        /// <summary>
        /// Setzt das Chart-Control komplett zurück, entfernt Events und 
        /// macht es bereit für eine erneute Init()-Ausführung.
        /// </summary>
        public void HardReset()
        {
            // 1. MouseWheel Event vom Manager entkoppeln (Wichtig gegen Memory Leaks)
            if (_chartWheelManager != null)
            {
                _chart.MouseWheel -= _chartWheelManager.HandleMouseWheel;
                _chart.MouseMove -= _chartWheelManager.HandleMouseMove;
                _chartWheelManager = null;
            }

            // 2. Alle grafischen Elemente entfernen
            _chart.Series.Clear();
            _chart.Titles.Clear();
            _chart.Legends.Clear();

            // 3. ChartAreas zurücksetzen (Falls du die Area selbst auch neu konfigurieren willst)
            if (_chart.ChartAreas.Count > 0)
            {
                ChartArea ca = _chart.ChartAreas[0];
                ca.AxisX.ScaleView.ZoomReset();
                ca.AxisY.ScaleView.ZoomReset();

                // Custom Labels und Fix-Positionen löschen
                ca.AxisX.CustomLabels.Clear();
                ca.InnerPlotPosition.Auto = true; // Wieder auf Automatik für den nächsten Start
            }

            // 4. Farben auf Default setzen (Optional)
            _chart.BackColor = Color.White;
            _chart.BackGradientStyle = GradientStyle.None;

            _chart.Invalidate(); // Neu zeichnen (leer)
        }

        public void AddSeries(string seriesName, Color color, float[] arr=null)
        {
            Series series = _chart.Series.Add(seriesName);
            series.ChartType = SeriesChartType.FastLine;
            series.XValueType = XAxisAsNumber ? ChartValueType.Double : ChartValueType.DateTime;
            series.BorderWidth = 2;
            series.ShadowOffset = 0; // Leichter Schatten für bessere Sichtbarkeit, 0 für keinen Schatten
            series.Color = color;
            
            if (XAxisAsNumber)
            {
                for (int i = 0; i < MaxXVALUE; i++) series.Points.AddXY(i, arr[i]);
            }
            else
            {
                DateTime dt = new DateTime(DateTime.Now.Year, 1, 1);
                for (int j = 0; j < MaxXVALUE; j++)
                {
                    DateTime d;
                    if (IsQuarterHourly)
                        d = dt.AddMinutes(j*15);
                    else
                        d = dt.AddHours(j);
                    series.Points.AddXY(d, arr[j]);
                }
            }
        }

        private void FormatXAxisWithNumber()
        {
            ChartArea ca = _chart.ChartAreas[0];
            ca.AxisX.CustomLabels.Clear();

            ca.AxisX.Minimum = 0;
            ca.AxisX.Maximum = IsQuarterHourly ? (8760 * 4) - 1 : 8759;
            ca.AxisX.IntervalType = DateTimeIntervalType.Number;

            // Beschriftung Properties
            ca.AxisX.Title = XAxisTitle;
            ca.AxisX.TitleFont = new Font("Segoe UI;", 8, FontStyle.Regular);
            ca.AxisX.LabelStyle.Format = "0";

            // Zoom Out Intervalle
            // Intervalle für die grobe Übersicht
            double interval = IsQuarterHourly ? 8000 : 2000;
            ca.AxisX.Interval = interval;
            ca.AxisX.MajorGrid.Interval = interval;
            ca.AxisX.ScaleView.Size = MaxXVALUE;
        }

        private void FormatXAxisWithDate()
        {
            ChartArea ca = _chart.ChartAreas[0];
            ca.AxisX.CustomLabels.Clear();

            // Hartes Limit auf das aktuelle Jahr
            int currentYear = DateTime.Now.Year;
            ca.AxisX.Minimum = new DateTime(currentYear, 1, 1).ToOADate();
            ca.AxisX.Maximum = new DateTime(currentYear, 12, 31, 23, 59, 59).ToOADate();

            ca.AxisX.Title = "Zeitverlauf (Monate)";
            ca.AxisX.TitleFont = new Font("Segoe UI", 8, FontStyle.Regular);
            ca.AxisX.LabelStyle.Format = "%M";
            ca.AxisX.IntervalType = DateTimeIntervalType.Months;
            ca.AxisX.Interval = 1;
            ca.AxisX.MajorGrid.Interval = 1;
        }
    }

    public class ChartMouseWheel2
    {
        public string szToolTipUnit = "";
        public bool IsNumericAxis { get; set; } = false;
        // Neues Zoom-Limit auf 10 Stunden
        private const double MAX_ZOOM_IN_NUMERIC = 10.0; 
        private const double MAX_ZOOM_IN_DATE = 0.4166; // 10 Stunden in OADate Einheiten (10/24)

        private readonly Chart _chart;
        private readonly ToolTip _toolTip = new ToolTip();
        private DataPoint _lastPoint = null;
        public bool IsQuarterHourly = false;

        public ChartMouseWheel2(Chart chart, bool viertelstunde)
        {
            _chart = chart;
            IsQuarterHourly = viertelstunde;

            _chart.MouseWheel += HandleMouseWheel;
            _chart.MouseMove += HandleMouseMove;
            _chart.MouseEnter += (s, e) => _chart.Focus();
        }

        public void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            var area = _chart.ChartAreas[0];
            var xAxis = area.AxisX;

            double xMin = xAxis.ScaleView.ViewMinimum;
            double xMax = xAxis.ScaleView.ViewMaximum;
            if (double.IsNaN(xMin)) xMin = xAxis.Minimum;
            if (double.IsNaN(xMax)) xMax = xAxis.Maximum;

            double range = xMax - xMin;
            double mouseX;
            try { mouseX = xAxis.PixelPositionToValue(e.Location.X); } catch { return; }

            double relativePos = (mouseX - xMin) / range;
            double zoomFactor = (e.Delta > 0) ? 0.7 : 1.4;
            double newRange = range * zoomFactor;

            // Zoom-Limit Check
            double limit = IsNumericAxis ? MAX_ZOOM_IN_NUMERIC : MAX_ZOOM_IN_DATE;
            if (e.Delta > 0 && newRange < limit) 
            {
                if (range <= limit) return;
                newRange = limit;
            }

            double newMin = mouseX - (newRange * relativePos);
            double newMax = mouseX + (newRange * (1 - relativePos));

            if (newMin < xAxis.Minimum) { newMin = xAxis.Minimum; newMax = newMin + newRange; }
            if (newMax > xAxis.Maximum) { newMax = xAxis.Maximum; newMin = newMax - newRange; }

            if (e.Delta < 0 && newRange >= (xAxis.Maximum - xAxis.Minimum))
                xAxis.ScaleView.ZoomReset();
            else
                xAxis.ScaleView.Zoom(newMin, newMax);

            UpdateVisuals(xAxis);
        }
       
        private void UpdateVisuals(Axis xAxis)
        {
            double viewSize = xAxis.ScaleView.Size;
            if (double.IsNaN(viewSize) || viewSize == 0) viewSize = xAxis.Maximum - xAxis.Minimum;

            if (IsNumericAxis)
            {
                double mainInterval;
                if (viewSize <= 72) mainInterval = 24;
                else if (viewSize <= 500) mainInterval = 168;
                else if (viewSize <= 2500) mainInterval = 720;
                else mainInterval = 2000;

                xAxis.Interval = mainInterval;
                xAxis.MajorGrid.Interval = mainInterval;
                xAxis.MinorGrid.Enabled = (viewSize <= 120);
                xAxis.MinorGrid.Interval = 6;
            }
            else
            {
                // --- Optimierte Datums-Logik ---
                /*
                if (viewSize <= 3.0) // Fokus auf wenige Tage: 6h-Intervalle
                {
                    xAxis.LabelStyle.Format = "HH:mm";
                    xAxis.IntervalType = DateTimeIntervalType.Hours;
                    xAxis.Interval = 6;
                    xAxis.MajorGrid.Interval = 6;
                }*/
                if (viewSize <= 14.0) // 1 bis 2 Wochen: Alle 2 Tage beschriften
                {
                    xAxis.LabelStyle.Format = "dd.MM.";
                    xAxis.IntervalType = DateTimeIntervalType.Days;
                    xAxis.Interval = 2;
                    xAxis.MajorGrid.Interval = 2;
                }
                else if (viewSize <= 45.0) // Bis zu 1.5 Monate: Wochenschritte (Alle 7 Tage)
                {
                    xAxis.LabelStyle.Format = "dd.MM.";
                    xAxis.IntervalType = DateTimeIntervalType.Days;
                    xAxis.Interval = 7;
                    xAxis.MajorGrid.Interval = 7;
                }
                else // Fernansicht: Monate
                {
                    xAxis.LabelStyle.Format = "MMM";
                    xAxis.IntervalType = DateTimeIntervalType.Months;
                    xAxis.Interval = 1;
                    xAxis.MajorGrid.Interval = 1;
                }
                xAxis.MinorGrid.Enabled = false;
            }
        }
        /*
        public void HandleMouseMove(object sender, MouseEventArgs e)
        {
            HitTestResult result = _chart.HitTest(e.X, e.Y);
            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                var point = result.Series.Points[result.PointIndex];
                if (point == _lastPoint) return;
                _lastPoint = point;
                string xVal = IsNumericAxis ? $"Std: {point.XValue:0}" : $"{DateTime.FromOADate(point.XValue):dd.MM. HH:mm}";
                _toolTip.SetToolTip(_chart, $"{xVal}\nWert: {point.YValues[0]:N2} {szToolTipUnit}");
            }
            else { _toolTip.Hide(_chart); _lastPoint = null; }
        }*/
        public void HandleMouseMove(object sender, MouseEventArgs e)
        {
            HitTestResult result = _chart.HitTest(e.X, e.Y);
            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                var point = result.Series.Points[result.PointIndex];
                if (point == _lastPoint) return;
                _lastPoint = point;

                string xVal;
                if (IsNumericAxis)
                {
                    if (IsQuarterHourly)
                    {
                        // Umrechnung: Index -> Stunden und Minuten
                        double totalHours = point.XValue / 4.0;
                        int hours = (int)Math.Floor(totalHours);
                        int minutes = (int)((totalHours - hours) * 60);
                        xVal = $"Einheit: {point.XValue:0} ({hours:00}:{minutes:00}h)";
                    }
                    else
                    {
                        xVal = $"Stunde: {point.XValue:0}";
                    }
                }
                else
                {
                    DateTime dt = DateTime.FromOADate(point.XValue);
                    if (IsQuarterHourly)
                    {
                        xVal = dt.ToString("dd.MM.yyyy HH:mm"); // Zeigt z.B. 12.04.2026 14:15
                    }
                    else
                    {
                        xVal = dt.ToString("dd.MM. HH:mm");
                    }
            
                }

                _toolTip.SetToolTip(_chart, $"{xVal}\nWert: {point.YValues[0]:N2} {szToolTipUnit}");
            }
            else { _toolTip.Hide(_chart); _lastPoint = null; }
        }
    }
}