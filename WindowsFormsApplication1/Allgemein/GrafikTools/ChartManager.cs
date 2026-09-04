using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    internal class ChartManager
    {
        // --- Konfigurations-Variablen ---
        public int MaxXVALUE = 8760;
        public double YMaxValue = 0;
        public double YMinValue = 0;
        public bool XAxisAsNumber = false;
        public bool IsXYChart = false; // Für XY-Charts (z.B. Temperatur)
        public bool WheelZoomed = true;
        public string toolTipUnit = "";
        // Leer als Vorgabe: alle 15 Aufrufer setzen den Achsentitel selbst. Bis Paket 9
        // stand hier ein deutsches Literal, das nie sichtbar wurde (die Datumsachse hat es
        // ohnehin ueberschrieben) - der Vorgabetext kommt jetzt bei Bedarf aus dem
        // Ressourcenkatalog (FormatXAxisWithDate).
        public string XAxisTitle = "";
        public string YAxisTitle = "";
        public string ChartTitle = "";
        public bool MitChartBorder = false;
        public bool MitLegende = false;
        // Dicke der farbigen Kennzeichnung in der Legende (gefuelltes Rechteck in Seriennfarbe).
        // 0 = Standard (unveraendert). >0 = dicke farbige Markierung; die Hoehe skaliert mit dem Wert
        // (Faktor 15 in Init, bei Bedarf anpassen). Nur die Legende, nicht die Kurven. Startwert ~8..14.
        public int LegendMarkerBreite = 0;
        public bool MitViertelStunde = false;
        public bool AreaLine = false;
        // Plotflaechen-Hintergrund: neutrales Weiss (bessere Lesbarkeit der Serien).
        public Color BackColor = Color.White;

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
                leg.Docking = Docking.Bottom;
                leg.Alignment = StringAlignment.Center;
                leg.BackColor = Color.Transparent;
                leg.Font = new Font("Arial", 8);

                // Groesse des Farb-Symbols (kurze Linie) steuerbar machen: Ohne vergroesserte
                // Symbolflaeche wird eine dicke Linie auf das Standard-Zellenmass abgeschnitten
                // (deshalb wirkt reines BorderWidth kaum). Dafuer eigene Legenden-Spalten anlegen:
                // eine Symbol-Spalte (Hoehe waechst mit LegendMarkerBreite) und eine Text-Spalte.
                if (LegendMarkerBreite > 0)
                {
                    leg.CellColumns.Clear();

                    LegendCellColumn symbolSpalte = new LegendCellColumn();
                    symbolSpalte.ColumnType = LegendCellColumnType.SeriesSymbol;
                    symbolSpalte.HeaderText = "";
                    // Hoehe der Symbolzelle (relative Einheiten, Standard ~70) waechst mit der Breite,
                    // damit die dicke Linie ueberhaupt Platz hat. Faktor bei Bedarf anpassen.
                    symbolSpalte.SeriesSymbolSize = new Size(200, Math.Max(70, LegendMarkerBreite * 15));
                    leg.CellColumns.Add(symbolSpalte);

                    LegendCellColumn textSpalte = new LegendCellColumn();
                    textSpalte.ColumnType = LegendCellColumnType.Text;
                    textSpalte.Text = "#LEGENDTEXT";   // Platzhalter: zeigt den Serien-/Legendentext
                    textSpalte.Alignment = ContentAlignment.MiddleLeft;
                    textSpalte.HeaderText = "";
                    leg.CellColumns.Add(textSpalte);
                }

                _chart.Legends.Add(leg);
            }

            // Legendensymbol optional dicker: doppelt an-/abmelden vermeidet Mehrfach-Registrierung
            // bei wiederholtem Init()/RefreshChart(). Der Handler wirkt nur, wenn LegendMarkerBreite > 0.
            _chart.CustomizeLegend -= Chart_CustomizeLegend;
            _chart.CustomizeLegend += Chart_CustomizeLegend;

            // --- PLATZ FÜR LEGENDE ---
            // --- DYNAMISCHE POSITIONIERUNG ---
            ca.InnerPlotPosition.Auto = false;

            // 1. X-Position (Abstand links): 
            // Je schmaler das Chart, desto mehr Prozent brauchen wir für die Y-Achsen-Zahlen
            ca.InnerPlotPosition.X = _chart.Width < 300 ? 18 : 12;

            // 2. Y-Position (Abstand oben): 
            ca.InnerPlotPosition.Y = _chart.Height < 300 ? 10 : 7;

            // 3. Breite:
            ca.InnerPlotPosition.Width = _chart.Width < 300 ? 78 : 85;

            // 4. Höhe (Das ist der entscheidende Teil für den Leerraum):
            // Wir berechnen einen Puffer für die Legende und Achse
            float bottomSpace = 15; // Standard-Puffer in Prozent
            //if (MitLegende) bottomSpace += 10; // Mehr Platz wenn Legende da
            if (_chart.Height < 300) bottomSpace += 5; // Extra-Puffer bei sehr kleinen Grafiken

            ca.InnerPlotPosition.Height = 100 - ca.InnerPlotPosition.Y - bottomSpace;

            // --- TITEL ---
            _chart.Titles.Clear();
            Title mainTitle = new Title(ChartTitle, Docking.Top, new Font("Segoe UI", 10), Color.FromArgb(64, 64, 64));
            mainTitle.Alignment = ContentAlignment.TopCenter;
            _chart.Titles.Add(mainTitle);

            // --- HINTERGRUND ---
            ca.BackColor = BackColor;
            ca.BackGradientStyle = GradientStyle.None;   // einfarbig statt Verlauf
            ca.BackSecondaryColor = Color.White;

            // --- RAHMEN ---
            _chart.BorderlineDashStyle = MitChartBorder ? System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid : System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.NotSet;
            _chart.BorderlineWidth = MitChartBorder ? 1 : 0;
            ca.BorderDashStyle = ChartDashStyle.DashDot;
            ca.BorderColor = Color.Black;

            // --- Y-ACHSE ---
            ca.AxisY.Title = YAxisTitle + (string.IsNullOrEmpty(toolTipUnit) ? "" : " [" + toolTipUnit + "]");
            ca.AxisY.IsLabelAutoFit = false;
            ca.AxisY.LabelStyle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            ca.AxisY.LabelStyle.ForeColor = Color.FromArgb(64, 64, 64);

            double range = (YMaxValue != 0 ? YMaxValue : 100) - YMinValue;
            double interval = CalculateNiceInterval(range, 8);
            ca.AxisY.Interval = interval;
            ca.AxisY.IntervalOffset = 0;
            ca.AxisY.LabelStyle.Format = interval < 1.0 ? "N1" : "N0";

            // Minimum auf ein Interval-Vielfaches ab- und Maximum aufrunden:
            //  -> Labels sind runde Zahlen (auch bei negativem/krummem Minimum, z. B. Temperatur)
            //  -> der oberste Wert bekommt ein Label und die Datenspitze klebt nicht am oberen Rand
            //     (macht das frühere "YMaxValue * 1.1" beim Aufrufer überflüssig)
            if (interval > 0)
            {
                double roundedMin = Math.Floor(YMinValue / interval) * interval;
                ca.AxisY.Minimum = roundedMin;
                if (YMaxValue != 0)
                {
                    double roundedMax = Math.Ceiling(YMaxValue / interval) * interval;
                    if (roundedMax <= roundedMin) roundedMax = roundedMin + interval;
                    ca.AxisY.Maximum = roundedMax;
                }
                else
                {
                    ca.AxisY.Maximum = double.NaN;
                }
            }
            else
            {
                ca.AxisY.Minimum = YMinValue;
                ca.AxisY.Maximum = YMaxValue != 0 ? YMaxValue : double.NaN;
            }
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.DashDot;
            ca.AxisY.MajorGrid.LineColor = Color.FromArgb(225, 225, 230);

            // --- X-ACHSE ---
            ca.AxisX.ScaleView.Zoomable = true;
            ca.AxisX.ScrollBar.IsPositionedInside = true;
            ca.AxisX.LabelStyle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            ca.AxisX.LabelStyle.ForeColor = Color.FromArgb(64, 64, 64);
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
            ca.AxisX.MajorGrid.LineColor = Color.FromArgb(225, 225, 230);

            // Cursor aktivieren
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;
            ca.CursorX.SelectionColor = Color.FromArgb(120, 0, 120, 215);
            ca.CursorY.SelectionColor = Color.FromArgb(120, 0, 120, 215);

            // --- FORMATIERUNG NACH MODUS ---
            if (IsXYChart) FormatXAxisAsXY(ca);
            else if (XAxisAsNumber) FormatXAxisWithNumber();
            else FormatXAxisWithDate();

            // --- WHEEL ZOOM ---
            if (WheelZoomed)
            {
                _chartWheelManager = new ChartMouseWheel2(_chart, MitViertelStunde);
                _chartWheelManager.szToolTipUnit = toolTipUnit;
                _chartWheelManager.IsNumericAxis = XAxisAsNumber || IsXYChart;
                _chartWheelManager.IsXYMode = IsXYChart;
            }

            _chart.AxisScrollBarClicked += chart1_AxisScrollBarClicked;

            // Position der gesamten ChartArea (inklusive Achsenbeschriftung) im Control
            ca.Position.Auto = true; // Oder manuell auf ca.Position = new ElementPosition(0, 0, 100, 100);

            _chart.Series.Clear();
            _chart.Invalidate();
        }

        private void chart1_AxisScrollBarClicked(object sender, ScrollBarEventArgs e)
        {
            // Prüfen, ob der Reset-Button (das Kreissymbol) geklickt wurde
            if (e.ButtonType == ScrollBarButtonType.ZoomReset)
            {
                // Deine Logik, wenn der Zoom zurückgesetzt wird
                Console.WriteLine("Zoom wurde über den Scrollbar-Button zurückgesetzt.");

                // Optional: Standardverhalten verhindern, wenn du den Reset selbst steuern willst
                // e.IsHandled = true; 
                // _chart.ChartAreas[0].RecalculateAxesScale();
                // _chart.Invalidate();
             //   _chartWheelManager.UpdateVisuals(_chart.ChartAreas[0].AxisX);
            }
        }

        // Zeichnet die kurze Farb-Linie in der Legende dicker – gesteuert ueber LegendMarkerBreite.
        // 0 => nichts aendern (Standardaussehen). >0 => Symbol als Linie dieser Breite.
        private void Chart_CustomizeLegend(object sender, CustomizeLegendEventArgs e)
        {
            if (LegendMarkerBreite <= 0) return;
            foreach (LegendItem item in e.LegendItems)
            {
                // Gefuelltes Rechteck in Seriennfarbe: die Dicke ergibt sich aus der Symbolhoehe
                // (SeriesSymbolSize unten) und wird NICHT von der duennen Serienlinie ueberschrieben.
                item.ImageStyle = LegendImageStyle.Rectangle;
            }
        }

        public void RefreshChart()
        {
            if (_chart == null || _chart.ChartAreas.Count == 0) return;

            ChartArea ca = _chart.ChartAreas[0];

            // 1. Achsen-Reset (Wichtig gegen FormatException)
            ca.AxisX.ScaleView.ZoomReset();
            ca.AxisX.IntervalType = DateTimeIntervalType.Auto;
            ca.AxisX.Interval = 0;
            ca.AxisX.LabelStyle.Format = "";

            // 2. Initialisierung neu aufrufen (setzt Titel, Farben, Achsen-Grundwerte)
            this.Init();

            // 3. Neuzeichnen erzwingen
            _chart.Invalidate();
        }

        public double CalculateNiceInterval(double range, int targetLabels)
        {
            if (range <= 0) return 1;

            // Grobe Schätzung des Intervalls
            double rawInterval = range / targetLabels;

            // Magnitude bestimmen (Zehnerpotenz)
            // Beispiel: bei 137 ist log10 ca. 2.13 -> floor ist 2 -> 10^2 = 100
            double exponent = Math.Floor(Math.Log10(rawInterval));
            double fraction = rawInterval / Math.Pow(10, exponent);

            double niceFraction;
            if (fraction < 1.5) niceFraction = 1;      // Schritte wie 10, 100, 1000
            else if (fraction < 3) niceFraction = 2;   // Schritte wie 20, 200, 2000
            else if (fraction < 7) niceFraction = 5;   // Schritte wie 50, 500, 5000
            else niceFraction = 10;                    // Nächste Zehnerpotenz

            return niceFraction * Math.Pow(10, exponent);
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
                    DateTime d = MitViertelStunde ? dt.AddMinutes(j * 15) : dt.AddHours(j);
                    series.Points.AddXY(d, arr[j]);
                }
            }
        }

        public void FormatXAxisAsXY(ChartArea ca)
        {
            ca.AxisX.CustomLabels.Clear();
            ca.AxisX.Title = XAxisTitle;
            ca.AxisX.LabelStyle.Format = "0";
            ca.AxisX.Interval = 5;
            ca.AxisX.MajorGrid.Interval = 5;

            // --- NULLGRAD-LINIE HERVORHEBEN ---
            ca.AxisX.StripLines.Clear(); // Alte Linien entfernen
            StripLine zeroLine = new StripLine
            {
                IntervalOffset = 0,               // Position bei 0
                Interval = 0,                     // Nur einmal zeichnen
                BorderColor = Color.FromArgb(180, Color.DimGray), // Dezent aber sichtbar
                BorderDashStyle = ChartDashStyle.Solid,
                BorderWidth = 2,
                Text = "0°C",                     // Kleine Beschriftung an der Linie
                TextAlignment = StringAlignment.Far,
                TextLineAlignment = StringAlignment.Near,
                ForeColor = Color.FromArgb(180, Color.DimGray),
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            ca.AxisX.StripLines.Add(zeroLine);
        }

        private void FormatXAxisWithNumber()
        {
            ChartArea ca = _chart.ChartAreas[0];
            ca.AxisX.CustomLabels.Clear();

            // WICHTIG: Format zurücksetzen, sonst versucht er die Zahl als Datum zu interpretieren
            ca.AxisX.LabelStyle.Format = "0";
            ca.AxisX.IntervalType = DateTimeIntervalType.Number;
            ca.AxisX.IntervalOffsetType = DateTimeIntervalType.Number;

            ca.AxisX.Minimum = 0;
            // Achsen-Endwert (voller Wertebereich)
            double xMax = MitViertelStunde ? MaxXVALUE * 4 : MaxXVALUE;   // generisch statt fix 8760
            ca.AxisX.Maximum = xMax;

            ca.AxisX.Title = XAxisTitle;

            // Intervall so wählen, dass der Endwert ein Label bekommt (6 gleiche Abschnitte, kein unbeschrifteter Rest)
            double mainInterval = xMax / 6.0;
            ca.AxisX.Interval = mainInterval;
            ca.AxisX.MajorGrid.Interval = mainInterval;

            // ScaleView anpassen
            ca.AxisX.ScaleView.Size = double.NaN; // Reset Zoom auf volle Breite
        }

        private void FormatXAxisWithDate()
        {
            ChartArea ca = _chart.ChartAreas[0];
            int currentYear = DateTime.Now.Year;
            ca.AxisX.Minimum = new DateTime(currentYear, 1, 1).ToOADate();
            ca.AxisX.Maximum = new DateTime(currentYear, 12, 31, 23, 59, 59).ToOADate();
            // Der Achsentitel des Aufrufers hat Vorrang. Bis Paket 9 stand hier ein festes
            // Literal, das JEDE von aussen gesetzte (und seit L4/L6 lokalisierte)
            // XAxisTitle-Zuweisung ueberschrieben hat - sechs der acht Simulationsdiagramme
            // zeigten deshalb weiter den deutschen Text. Nur wenn kein Titel gesetzt ist,
            // greift der Vorgabetext aus dem Ressourcenkatalog.
            ca.AxisX.Title = string.IsNullOrEmpty(XAxisTitle)
                ? MyResource.Resource.CHART_ACHSE_MONATE
                : XAxisTitle;
            ca.AxisX.LabelStyle.Format = "MMM";   // Monatsnamen (Jan, Feb, ...) – konsistent mit der Zoom-Ansicht (UpdateVisuals)
            ca.AxisX.IntervalType = DateTimeIntervalType.Months;
            ca.AxisX.IntervalOffsetType = DateTimeIntervalType.Months;
            ca.AxisX.Interval = 1;
        }

        public void HardReset()
        {
            _chart.CustomizeLegend -= Chart_CustomizeLegend;
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

    public class ChartMouseWheel2
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

        public ChartMouseWheel2(Chart chart, bool viertelstunde)
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
            UpdateVisuals(xAxis);
        }

        public void HandleMouseMove(object sender, MouseEventArgs e)
        {
            HitTestResult result = _chart.HitTest(e.X, e.Y);
            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                var point = result.Series.Points[result.PointIndex];
                if (point == _lastPoint) return;
                _lastPoint = point;

                // Die Zahlenformate (N1 / "0" / N2) bleiben im Quelltext, der Katalog fuehrt
                // nur die normalisierten Platzhalter (Lesehinweis des Ressourcenkatalogs).
                string xVal = IsXYMode
                    ? $"X: {point.XValue:N1}"
                    : (IsNumericAxis
                        ? string.Format(MyResource.Resource.CHART_TOOLTIP_EINHEIT, point.XValue.ToString("0"))
                        : DateTime.FromOADate(point.XValue).ToString("dd.MM. HH:mm"));
                _toolTip.SetToolTip(_chart, xVal + "\n" + string.Format(
                    MyResource.Resource.CHART_TOOLTIP_WERT,
                    point.YValues[0].ToString("N2"), szToolTipUnit));
            }
            else { _toolTip.Hide(_chart); _lastPoint = null; }
        }

        public void UpdateVisuals(Axis xAxis)
        {
            double viewSize = xAxis.ScaleView.Size;

            // Falls viewSize NaN oder 0 ist (Zoom Reset), berechnen wir die volle Range
            if (double.IsNaN(viewSize) || viewSize <= 0)
                viewSize = xAxis.Maximum - xAxis.Minimum;

            if (IsXYMode)
            {
                // Bei Temperatur-Charts (XY) behalten wir das feste 5-Grad-Raster bei,
                // außer man zoomt extrem weit rein:
                xAxis.Interval = (viewSize < 10) ? 1 : 5;
                xAxis.MajorGrid.Interval = xAxis.Interval;
                return;
            }

            if (IsNumericAxis)
            {
                // Logik für die 8760 Stunden-Achse
                xAxis.LabelStyle.Format = "0"; // Sicherstellen, dass hier kein Datumstext kommt
                double mainInterval;
                if (viewSize <= 72) mainInterval = 24;          // 3 Tage -> 24h Schritte
                else if (viewSize <= 500) mainInterval = 168;   // ~3 Wochen -> Wochenschnitte
                else if (viewSize <= 2500) mainInterval = 720;  // Monate
                else mainInterval = 1460;                       // Übersicht (teilt 8760 -> Endwert bekommt ein Label)

                xAxis.Interval = mainInterval;
                xAxis.MajorGrid.Interval = mainInterval;
            }
            else
            {
                // --- REAKTIVIERTE DATUMS-LOGIK ---
                // viewSize ist bei Datumswerten in Tagen angegeben (OADate)

                if (viewSize <= 2.0) // Weniger als 2 Tage Sichtbar
                {
                    xAxis.LabelStyle.Format = "HH:mm";
                    xAxis.IntervalType = DateTimeIntervalType.Hours;
                    xAxis.Interval = 6; // Alle 6 Stunden ein Label
                }
                else if (viewSize <= 21.0) // Weniger als 3 Wochen
                {
                    xAxis.LabelStyle.Format = "dd.MM.";
                    xAxis.IntervalType = DateTimeIntervalType.Days;
                    xAxis.Interval = 2; // Alle 2 Tage
                }
                else if (viewSize <= 60.0) // Bis zu 2 Monate
                {
                    xAxis.LabelStyle.Format = "dd.MM.";
                    xAxis.IntervalType = DateTimeIntervalType.Days;
                    xAxis.Interval = 7; // Wochenschnitte
                }
                else // Fernansicht (Standard)
                {
                    xAxis.LabelStyle.Format = "MMM";
                    xAxis.IntervalType = DateTimeIntervalType.Months;
                    xAxis.Interval = 1;
                }

                xAxis.MajorGrid.IntervalType = xAxis.IntervalType;
                xAxis.MajorGrid.Interval = xAxis.Interval;
            }
        }
    }

    // iU9-W11b.13: HIER STANDEN DonutChartDrawer (125 Z.) und Kacheln (57 Z.).
    //
    // Sie zeichneten die zwei GDI-Donuts und die KPI-Karten der NavigatorUebersicht -
    // ihres einzigen Nutzers. Mit der Maske sind sie ohne Aufrufer geblieben. An ihre
    // Stelle treten ChartRenderer.Ring (B5, aus dem Kern) und der Baustein
    // Kennzahlkachel.
    //
    // Der ChartManager selbst BLEIBT: Form_Klimadaten und Form_PeakShaving fuehren
    // weiter interaktive WinForms-Charts (geprueft, Wellenauftrag „kein anderer
    // Nutzer" trifft auf ihn nicht zu).


}
