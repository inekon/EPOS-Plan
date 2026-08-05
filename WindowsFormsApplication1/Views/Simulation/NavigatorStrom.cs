using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class NavigatorStrom : UserControl, INavigatableContent
    {
        private float[] temp_profil;
        private float[] temp_wp;
        private float[] temp_hs;
        private float[] temp_hk;
        private float[] temp_bhkw;
        private float[] temp_ges;

        ChartManager _chartManager;
        SimulationControl sim;

        public NavigatorStrom(SimulationControl simctrl)
        {
            InitializeComponent();
            sim = simctrl;
            InitCsvExportButton();
        }

        /// <summary>
        /// Legt den CSV-Export-Button rechts neben den Checkboxen an (programmatisch, kein Designer nötig).
        /// </summary>
        private void InitCsvExportButton()
        {
            Button btnExport = new Button();
            btnExport.Name = "btn_CsvExport";
            btnExport.Text = "CSV Export";
            btnExport.Size = new Size(105, 28);
            btnExport.Location = new Point(875, 516); // rechts neben checkBox_BHKW (y=520)
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnExport.Click += btn_CsvExport_Click;
            this.Controls.Add(btnExport);
            btnExport.BringToFront();
        }

        /// <summary>
        /// Exportiert die aktuell per Checkbox selektierten Serien des Strom-Charts als CSV
        /// (Zeitstempel, Außentemperatur, Werte — Viertelstundenwerte).
        /// </summary>
        private void btn_CsvExport_Click(object sender, EventArgs e)
        {
            if (sim == null || sim.simulation_Strombedarf == null || temp_ges == null)
            {
                MessageBox.Show("Keine Simulationsdaten vorhanden!\nBitte zuerst die Simulation durchführen.",
                    "CSV Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // nur die aktuell selektierten (angezeigten) Serien exportieren
            List<CsvSpalte> spalten = new List<CsvSpalte>();
            if (checkBox_Gesamt.Checked) spalten.Add(new CsvSpalte("Gesamt [kW]", temp_ges));
            if (checkBox_WP.Checked) spalten.Add(new CsvSpalte("Wärmepumpe [kW]", temp_wp));
            if (checkBox_Heizstab.Checked) spalten.Add(new CsvSpalte("Heizstab [kW]", temp_hs));
            if (checkBox_SPK.Checked) spalten.Add(new CsvSpalte("Heizkessel [kW]", temp_hk));
            if (checkBox_Profil_Lastgang.Checked) spalten.Add(new CsvSpalte("Profil/Lastgang [kW]", temp_profil));
            if (checkBox_PV.Checked) spalten.Add(new CsvSpalte("PV [kW]", sim.simulation_pv != null ? sim.simulation_pv.Stromproduktion_viertelstunde : null));
            if (checkBox_BHKW.Checked) spalten.Add(new CsvSpalte("BHKW [kW]", temp_bhkw));

            float[] temperatur = (sim.simulation_Waermebedarf != null)
                ? sim.simulation_Waermebedarf.Stundentemperatur
                : null;

            CsvExportClass.Export("Strombedarf.csv", temperatur, spalten, true);
        }

        public void RefreshContent()
        {
            // Hier die Logik, um die Charts mit neuen Daten aus simctrl zu füttern
            SetControl(this.sim);
            ApplyCheckboxStates();
            this.Invalidate();
        }

        public void UpdateSimulationData()
        {
            SetControl(this.sim);
        }

        public void SetControl(SimulationControl sim)
        {
            if (sim == null) return;
            if (sim.simulation_Strombedarf == null) return; // Sicherheitshalber prüfen 
            _chartManager = new ChartManager(chart7);
            _chartManager.BackColor = Color.White;
            _chartManager._chart.BackColor = Color.LightGray;

            // Chart Strombedarf und Stromverbrauch Übersicht
            temp_profil = sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            temp_wp = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.WP_Strombedarf_stuendlich);
            temp_hs = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.Heizstab_stuendlich);
            temp_hk = sim.Stundenwerte_zu_viertelstunden(sim.simulation_spk.Strombedarf_stuendlich);
            temp_bhkw = sim.Stundenwerte_zu_viertelstunden(sim.simulation_bhkw.stromproduktion);
            temp_ges = new float[8760 * 4];

            for (int i = 0; i < 8760 * 4; i++) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_profil[i];

            _chartManager.YMaxValue = temp_ges.Max() + 1;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisAsNumber = false;
            _chartManager.XAxisTitle = "Monate";
            _chartManager.YAxisTitle = "Leistung";
            _chartManager.toolTipUnit = "kW";
            _chartManager.ChartTitle = "Strombedarf, Stromverbrauch Jahresganglinie";
            _chartManager.MitLegende = true;
            _chartManager.MaxXVALUE = 8760 * 4;
            _chartManager.MitViertelStunde = true;
            _chartManager.LegendMarkerBreite = 5;

            _chartManager.Init();
            _chartManager.AddSeries("Gesamt", Color.Green, temp_ges);
            _chartManager.AddSeries("Waermepumpe", Color.Orange, temp_wp);
            _chartManager.AddSeries("Heizstab", Color.Yellow, temp_hs);
            _chartManager.AddSeries("Heizkessel", Color.Blue, temp_hk);
            _chartManager.AddSeries("Profil/Lastgang", Color.Brown, temp_profil);
            _chartManager.AddSeries("BHKW", Color.Brown, temp_bhkw);


            // _chartManager[7].AddSeries("Rest", Color.Black, sim.Rest_Strombedarf_viertelstuendlich);
            _chartManager.AddSeries("PV", Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            // _chartManager[7].AddSeries("Überschuss", Color.Magenta, sim.simulation_pv.Ueberschuss_viertelstunde);
            _chartManager._chart.Series["Waermepumpe"].Enabled = false;
            _chartManager._chart.Series["Heizstab"].Enabled = false;
            _chartManager._chart.Series["Heizkessel"].Enabled = false;
            _chartManager._chart.Series["Profil/Lastgang"].Enabled = false;
            _chartManager._chart.Series["PV"].Enabled = false;
            _chartManager._chart.Series["BHKW"].Enabled = false;
            checkBox_Gesamt.Checked = true;
        }

        private void ApplyCheckboxStates()
        {
            // Hier erzwingst du, dass das Chart genau das anzeigt, was die Checkbox sagt
            if (_chartManager != null && _chartManager._chart.Series.Count > 0)
            {
                _chartManager._chart.Series["Gesamt"].Enabled = checkBox_Gesamt.Checked;
                _chartManager._chart.Series["Waermepumpe"].Enabled = checkBox_WP.Checked;
                _chartManager._chart.Series["Heizstab"].Enabled = checkBox_Heizstab.Checked;
                _chartManager._chart.Series["Heizkessel"].Enabled = checkBox_SPK.Checked;
                _chartManager._chart.Series["Profil/Lastgang"].Enabled = checkBox_Profil_Lastgang.Checked;
                _chartManager._chart.Series["PV"].Enabled = checkBox_PV.Checked;
                _chartManager._chart.Series["BHKW"].Enabled = checkBox_BHKW.Checked;
            }
        }

        private void checkBox_Gesamt_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Gesamt.Checked)
            {
                _chartManager._chart.Series["Gesamt"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["Gesamt"].Enabled = false;
            }
            OptimizeYAxisScale();
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_WP.Checked)
            {
                _chartManager._chart.Series["Waermepumpe"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["Waermepumpe"].Enabled = false;
            }
            OptimizeYAxisScale();
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Heizstab.Checked)
            {
                _chartManager._chart.Series["Heizstab"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["Heizstab"].Enabled = false;
            }
            OptimizeYAxisScale();
        }

        private void checkBox_SPK_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SPK.Checked)
            {
                _chartManager._chart.Series["Heizkessel"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["Heizkessel"].Enabled = false;
            }
            OptimizeYAxisScale();
        }

        private void checkBox_Profil_Lastgang_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Profil_Lastgang.Checked)
            {
                _chartManager._chart.Series["Profil/Lastgang"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["Profil/Lastgang"].Enabled = false;
            }
            OptimizeYAxisScale();
        }


        private void checkBox_PV_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_PV.Checked)
            {
                _chartManager._chart.Series["PV"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["PV"].Enabled = false;
            }
            OptimizeYAxisScale();
        }

        private void OptimizeYAxisScale()
        {
            if (_chartManager == null || _chartManager._chart == null) return;

            var chart = _chartManager._chart;
            float maxVisibleValue = 0;
            bool anySeriesVisible = false;

            // 1. Finde den höchsten Wert aller sichtbaren Serien
            foreach (var series in chart.Series)
            {
                if (series.Enabled && series.Points.Count > 0)
                {
                    anySeriesVisible = true;
                    // Ermittle das Maximum der Punkte dieser Serie
                    double seriesMax = series.Points.Max(p => p.YValues[0]);
                    if (seriesMax > maxVisibleValue)
                    {
                        maxVisibleValue = (float)seriesMax;
                    }
                }
            }

            // 2. Skalierung + passendes Interval anwenden.
            //    WICHTIG: Bisher wurde nur das Maximum gesetzt. Das in Init() berechnete
            //    Interval passte danach nicht mehr zum neuen Maximum -> die Y-Achse hatte
            //    keine (oder unpassende) Labels. Deshalb hier das Interval passend zum
            //    neuen Maximum neu berechnen (gleiche Logik wie in ChartManager.Init()).
            var axisY = chart.ChartAreas[0].AxisY;
            if (anySeriesVisible && maxVisibleValue > 0)
            {
                double interval = _chartManager.CalculateNiceInterval(maxVisibleValue, 8);
                if (interval <= 0) interval = 1;
                double roundedMax = Math.Ceiling((maxVisibleValue * 1.1) / interval) * interval;

                axisY.Minimum = 0;
                axisY.Maximum = roundedMax;
                axisY.Interval = interval;
                axisY.IntervalOffset = 0;
                axisY.LabelStyle.Format = interval < 1.0 ? "N1" : "N0";
            }
            else
            {
                // Fallback, wenn nichts ausgewählt ist: alles automatisch
                axisY.Minimum = 0;
                axisY.Maximum = Double.NaN;
                axisY.Interval = 0; // 0 = automatische Intervallberechnung
            }

            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        private void checkBox_BHKW_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_BHKW.Checked)
            {
                _chartManager._chart.Series["BHKW"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["BHKW"].Enabled = false;
            }
            OptimizeYAxisScale();
        }
    }
}
