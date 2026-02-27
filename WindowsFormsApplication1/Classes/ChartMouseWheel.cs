using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Windows.Forms.DataVisualization.Charting;

    public class ChartMouseWheel
    {
        public string szToolTipUnit = "";
        private readonly Chart _chart;
        private readonly ToolTip _toolTip = new ToolTip();
        private DataPoint _lastPoint = null;
        public bool UseYearlyHourAxis { get; set; } = true;

        public ChartMouseWheel(Chart chart)
        {
            _chart = chart;
            InitializeSettings();
            AttachEvents();
        }

        private void InitializeSettings()
        {
            var area = _chart.ChartAreas[0];
            area.AxisX.ScrollBar.IsPositionedInside = false;
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;
            area.AxisX.ScaleView.Zoomable = true;

            // Performance-Booster
            _chart.Series[0].ToolTip = "";
        }

        private void AttachEvents()
        {
            _chart.MouseWheel += HandleMouseWheel;
            _chart.MouseMove += HandleMouseMove;
            _chart.MouseEnter += (s, e) => _chart.Focus();
        }

        private void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            var xAxis = _chart.ChartAreas[0].AxisX;
            double xMin = xAxis.ScaleView.ViewMinimum;
            double xMax = xAxis.ScaleView.ViewMaximum;

            // Fallback auf Gesamtbereich
            if (double.IsNaN(xMin)) xMin = xAxis.Minimum;
            if (double.IsNaN(xMax)) xMax = xAxis.Maximum;

            double range = xMax - xMin;
            double zoomFactor = 0.30; // 30% Zoom-Stärke

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

            // Nach dem Zoom prüfen wir die neue Größe des Sichtfeldes
            double currentSize = xAxis.ScaleView.Size;

            // Wenn die Größe NaN ist, sehen wir das ganze Jahr (12 Monate)
            if (double.IsNaN(currentSize)) currentSize = xAxis.Maximum - xAxis.Minimum;

            // 30 Tage als Schwellenwert
            if (currentSize <= 31)
            {
                // Zoom ist tiefer als 1 Monat -> Tage anzeigen
                xAxis.LabelStyle.Format = "dd.MM.";
                xAxis.IntervalType = DateTimeIntervalType.Days;
                xAxis.Interval = currentSize < 7 ? 1 : 2; // Bei sehr tiefem Zoom jeden Tag, sonst alle 2 Tage
                _chart.ChartAreas[0].AxisX.Title = "Tage";
            }
            else
            {
                // Zoom ist weit draußen -> Monatszahlen anzeigen (wie gewünscht)
                xAxis.LabelStyle.Format = "%M";
                xAxis.IntervalType = DateTimeIntervalType.Months;
                xAxis.Interval = 1;
                _chart.ChartAreas[0].AxisX.Title = "Monate";
            }
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            HitTestResult result = _chart.HitTest(e.X, e.Y);

            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                var point = result.Series.Points[result.PointIndex];
                if (point == _lastPoint) return;

                _lastPoint = point;
                DateTime xDate = DateTime.FromOADate(point.XValue);
                double yVal = point.YValues[0] > 0.01 ? point.YValues[0] : 0;

                _toolTip.SetToolTip(_chart, $"{xDate:dd/MM H:mm}\n[{yVal:N2}{szToolTipUnit}]");
            }
            else
            {
                _toolTip.Hide(_chart);
                _lastPoint = null;
            }
        }
    }
}
