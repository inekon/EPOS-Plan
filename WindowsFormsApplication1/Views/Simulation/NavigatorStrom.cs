using System;
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
                if (series.Enabled)
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

            // 2. Skalierung anwenden (mit kleinem Puffer, z.B. 5-10%)
            if (anySeriesVisible && maxVisibleValue > 0)
            {
                chart.ChartAreas[0].AxisY.Maximum = maxVisibleValue * 1.1; // 10% Puffer oben
                chart.ChartAreas[0].AxisY.Minimum = 0;
            }
            else
            {
                // Fallback, wenn nichts ausgewählt ist
                chart.ChartAreas[0].AxisY.Maximum = Double.NaN; // Automatisch
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
