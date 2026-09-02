using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1 // Bitte an deinen Namespace anpassen
{
    internal class ChartManagerNeu
    {
        // --- Konfigurations-Variablen ---
        public double YMaxValue = 0;
        public double YMinValue = 0;
        public bool XAxisAsNumber = false;
        public bool IsXYChart = false; // Neu: Für XY-Charts (z.B. Temperatur)
        public bool WheelZoomed = true;
        public string toolTipUnit = "";
        public string XAxisTitle = "Zeitverlauf (Jahresstunden)";
        public string YAxisTitle = "";
        public string ChartTitle = "";
        public bool MitChartBorder = false;
        public bool MitLegende = false;
        public bool IsQuarterHourly = false;
        public int MaxXVALUE = 8760;
        public bool AreaLine = false;

        public Chart _chart = null;
        public ChartMouseWheel2Neu _chartWheelManager;

        public ChartManagerNeu(Chart chart)
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
                leg.Docking = Docking.Bottom;
                leg.Alignment = StringAlignment.Center;
                leg.BackColor = Color.Transparent;
                leg.Font = new Font("Arial", 8);
                _chart.Legends.Add(leg);
            }

            // --- PLATZ FÜR LEGENDE ---
            ca.InnerPlotPosition.Auto = false;
            ca.InnerPlotPosition.X = 12;
            ca.InnerPlotPosition.Y = 8;
            ca.InnerPlotPosition.Width = 85;
            ca.InnerPlotPosition.Height = MitLegende ? 70 : 80;

            // --- TITEL ---
            _chart.Titles.Clear();
            Title mainTitle = new Title(ChartTitle, Docking.Top, new Font("Segoe UI", 10), Color.FromArgb(64, 64, 64));
            mainTitle.Alignment = ContentAlignment.TopCenter;
            _chart.Titles.Add(mainTitle);

            // --- HINTERGRUND ---
            ca.BackColor = Color.FromArgb(122, 255, 222);
            ca.BackGradientStyle = GradientStyle.DiagonalLeft;
            ca.BackSecondaryColor = Color.White;

            // --- RAHMEN ---
//            _chart.BorderlineDashStyle = MitChartBorder ? ChartDashStyle.Solid : ChartDashStyle.None;
            // Die korrekte Zuweisung für das Chart-Control
            _chart.BorderlineDashStyle = MitChartBorder ? System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid : System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.NotSet;
            _chart.BorderlineWidth = MitChartBorder ? 1 : 0;
            ca.BorderDashStyle = ChartDashStyle.DashDot;
            ca.BorderColor = Color.Black;

            // --- Y-ACHSE ---
            ca.AxisY.Title = YAxisTitle + (string.IsNullOrEmpty(toolTipUnit) ? "" : " [" + toolTipUnit + "]");
            ca.AxisY.LabelStyle.Format = "N0";
            ca.AxisY.IsLabelAutoFit = false;
            double range = (YMaxValue != 0 ? YMaxValue : 100) - YMinValue;
            double interval = Math.Max(1, Math.Ceiling(range / 10));
            ca.AxisY.Interval = interval;
            ca.AxisY.Maximum = YMaxValue != 0 ? YMaxValue : double.NaN;
            ca.AxisY.Minimum = YMinValue;
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.DashDot;
            ca.AxisY.MajorGrid.LineColor = Color.FromArgb(150, 150, 150);

            // --- X-ACHSE ---
            ca.AxisX.ScaleView.Zoomable = true;
            ca.AxisX.ScrollBar.IsPositionedInside = true;
            ca.AxisX.LabelStyle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            ca.AxisX.LabelStyle.ForeColor = Color.FromArgb(64, 64, 64);
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
            ca.AxisX.MajorGrid.LineColor = Color.FromArgb(140, 140, 140);
            
            // Cursor aktivieren
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;
            ca.CursorX.SelectionColor = Color.FromArgb(120, 0, 120, 215);
            ca.CursorY.SelectionColor = Color.FromArgb(120, 0, 120, 215);

            // --- FORMATIERUNG NACH MODUS ---
            if (IsXYChart) FormatXAxisAsXY();
            else if (XAxisAsNumber) FormatXAxisWithNumber();
            else FormatXAxisWithDate();

            // --- WHEEL ZOOM ---
            if (WheelZoomed)
            {
                _chartWheelManager = new ChartMouseWheel2Neu(_chart, IsQuarterHourly);
                _chartWheelManager.szToolTipUnit = toolTipUnit;
                _chartWheelManager.IsNumericAxis = XAxisAsNumber || IsXYChart;
                _chartWheelManager.IsXYMode = IsXYChart;
            }

            _chart.Series.Clear();
            _chart.Invalidate();
        }

        // --- Hinzufügen von Serien (XY-Modus für Temperatur) ---
        public void AddSeries(string seriesName, Color color, PointF[] points, int borderWidth = 0)
        {
            Series series = _chart.Series.Add(seriesName);
            series.ChartType = AreaLine ? SeriesChartType.Area : SeriesChartType.FastLine;
            series.XValueType = ChartValueType.Double;
            series.BorderWidth = borderWidth;
            series.Color = color;
            if (borderWidth > 0) series.BorderColor = Color.FromArgb(255, color);

            series.Points.DataBindXY(points, "X", points, "Y");
            series.Sort(PointSortOrder.Ascending, "X");
        }

        // --- Hinzufügen von Serien (Zeitreihen-Modus) ---
        public void AddSeries(string seriesName, Color color, float[] arr)
        {
            Series series = _chart.Series.Add(seriesName);
            series.ChartType = AreaLine ? SeriesChartType.Area : SeriesChartType.FastLine;
            series.XValueType = XAxisAsNumber ? ChartValueType.Double : ChartValueType.DateTime;
            series.BorderWidth = 2;
            series.Color = color;

            if (XAxisAsNumber)
            {
                for (int i = 0; i < Math.Min(arr.Length, MaxXVALUE); i++) series.Points.AddXY(i, arr[i]);
            }
            else
            {
                DateTime dt = new DateTime(DateTime.Now.Year, 1, 1);
                for (int j = 0; j < Math.Min(arr.Length, MaxXVALUE); j++)
                {
                    DateTime d = IsQuarterHourly ? dt.AddMinutes(j * 15) : dt.AddHours(j);
                    series.Points.AddXY(d, arr[j]);
                }
            }
        }

        private void FormatXAxisAsXY()
        {
            ChartArea ca = _chart.ChartAreas[0];
            ca.AxisX.Title = XAxisTitle;
            ca.AxisX.LabelStyle.Format = "0";
            ca.AxisX.Interval = 5;
            ca.AxisX.MajorGrid.Interval = 5;
        }

        private void FormatXAxisWithNumber()
        {
            ChartArea ca = _chart.ChartAreas[0];
            ca.AxisX.CustomLabels.Clear();
            ca.AxisX.Minimum = 0;
            ca.AxisX.Maximum = IsQuarterHourly ? (8760 * 4) - 1 : 8759;
            ca.AxisX.IntervalType = DateTimeIntervalType.Number;
            ca.AxisX.Title = XAxisTitle;
            ca.AxisX.Interval = IsQuarterHourly ? 8000 : 2000;
            ca.AxisX.ScaleView.Size = MaxXVALUE;
        }

        private void FormatXAxisWithDate()
        {
            ChartArea ca = _chart.ChartAreas[0];
            int currentYear = DateTime.Now.Year;
            ca.AxisX.Minimum = new DateTime(currentYear, 1, 1).ToOADate();
            ca.AxisX.Maximum = new DateTime(currentYear, 12, 31, 23, 59, 59).ToOADate();
            ca.AxisX.Title = "Zeitverlauf (Monate)";
            ca.AxisX.LabelStyle.Format = "%M";
            ca.AxisX.IntervalType = DateTimeIntervalType.Months;
            ca.AxisX.Interval = 1;
        }

        public void HardReset()
        {
            if (_chartWheelManager != null)
            {
                _chart.MouseWheel -= _chartWheelManager.HandleMouseWheel;
                _chart.MouseMove -= _chartWheelManager.HandleMouseMove;
                _chartWheelManager = null;
            }
            _chart.Series.Clear();
            _chart.Titles.Clear();
            _chart.Legends.Clear();
            if (_chart.ChartAreas.Count > 0)
            {
                _chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                _chart.ChartAreas[0].AxisY.ScaleView.ZoomReset();
                _chart.ChartAreas[0].InnerPlotPosition.Auto = true;
            }
            _chart.Invalidate();
        }
    }

    public class ChartMouseWheel2Neu
    {
        public string szToolTipUnit = "";
        public bool IsNumericAxis { get; set; } = false;
        public bool IsXYMode { get; set; } = false; // Neu
        private const double MAX_ZOOM_IN_NUMERIC = 10.0;
        private const double MAX_ZOOM_IN_DATE = 0.4166;

        private readonly Chart _chart;
        private readonly ToolTip _toolTip = new ToolTip();
        private DataPoint _lastPoint = null;
        public bool IsQuarterHourly = false;

        public ChartMouseWheel2Neu(Chart chart, bool viertelstunde)
        {
            _chart = chart;
            IsQuarterHourly = viertelstunde;
            _chart.MouseWheel += HandleMouseWheel;
            _chart.MouseMove += HandleMouseMove;
        }

        public void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            var area = _chart.ChartAreas[0];
            var xAxis = area.AxisX;
            double range = xAxis.ScaleView.ViewMaximum - xAxis.ScaleView.ViewMinimum;
            if (double.IsNaN(range)) range = xAxis.Maximum - xAxis.Minimum;

            double mouseX = xAxis.PixelPositionToValue(e.Location.X);
            double relativePos = (mouseX - xAxis.ScaleView.ViewMinimum) / range;
            double zoomFactor = (e.Delta > 0) ? 0.7 : 1.4;
            double newRange = range * zoomFactor;

            double limit = IsNumericAxis ? MAX_ZOOM_IN_NUMERIC : MAX_ZOOM_IN_DATE;
            if (e.Delta > 0 && newRange < limit) newRange = limit;

            double newMin = mouseX - (newRange * relativePos);
            double newMax = newMin + newRange;

            if (newMin < xAxis.Minimum) newMin = xAxis.Minimum;
            if (newMax > xAxis.Maximum) newMax = xAxis.Maximum;

            if (e.Delta < 0 && newRange >= (xAxis.Maximum - xAxis.Minimum)) xAxis.ScaleView.ZoomReset();
            else xAxis.ScaleView.Zoom(newMin, newMax);
        }

        public void HandleMouseMove(object sender, MouseEventArgs e)
        {
            HitTestResult result = _chart.HitTest(e.X, e.Y);
            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                var point = result.Series.Points[result.PointIndex];
                if (point == _lastPoint) return;
                _lastPoint = point;

                string xVal = IsXYMode ? $"X: {point.XValue:N1}" : (IsNumericAxis ? $"Einheit: {point.XValue:0}" : DateTime.FromOADate(point.XValue).ToString("dd.MM. HH:mm"));
                _toolTip.SetToolTip(_chart, $"{xVal}\nWert: {point.YValues[0]:N2} {szToolTipUnit}");
            }
            else { _toolTip.Hide(_chart); _lastPoint = null; }
        }
    }
}