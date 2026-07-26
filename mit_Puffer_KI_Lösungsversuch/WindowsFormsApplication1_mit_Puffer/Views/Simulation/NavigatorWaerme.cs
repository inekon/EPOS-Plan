using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Humanizer.In;

namespace WindowsFormsApplication1
{
    public partial class NavigatorWaerme : UserControl, INavigatableContent
    {
        ChartManager _chartManager;
        SimulationControl sim;

        private float[] temp_profil;
        private float[] temp_wp;
        private float[] temp_hs;
        private float[] temp_hk;
        private float[] temp_st;
        private float[] temp_bhkw;
        private float[] temp_ges;

        public NavigatorWaerme(SimulationControl simctrl)
        {
            InitializeComponent();
            SetControl(sim = simctrl);
        }

        public void RefreshContent()
        {
            SetControl(this.sim);
            ApplyCheckboxStates();
        }

        public void SetControl(SimulationControl sim)
        {
            if (sim.simulation_Waermebedarf == null) return; // Sicherheitshalber prüfen 

            // Chart Strombedarf und Stromverbrauch Übersicht
            temp_profil = sim.simulation_Waermebedarf.Waermebedarf;
            temp_wp = sim.simulation_wp.WP_Waermeproduktion_stuendlich;
            temp_hs = sim.simulation_wp.Heizstab_stuendlich;
            temp_hk = sim.simulation_spk.Kesselleistung_stuendlich;
            temp_st = Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermeproduktion, x => (float)x);
            temp_bhkw = sim.simulation_bhkw.waermeproduktion;
            temp_ges = new float[8760];

            for (int i = 0; i < 8760; i++) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_st[i] + temp_bhkw[i];

            _chartManager = new ChartManager(chart_Waerme);
            _chartManager.BackColor = Color.White;
            _chartManager._chart.BackColor = Color.LightGray;
            _chartManager.YMaxValue = temp_ges.Max() + 1;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisAsNumber = false;
            _chartManager.XAxisTitle = "Monate";
            _chartManager.YAxisTitle = "Leistung";
            _chartManager.toolTipUnit = "kW";
            _chartManager.ChartTitle = "Wärmeproduktion Jahresganglinie";
            _chartManager.MitLegende = true;
            _chartManager.MaxXVALUE = 8760;
            _chartManager.MitViertelStunde = false;
            _chartManager.Init();
            _chartManager.AddSeries("Gesamt", Color.Green, temp_ges);
            _chartManager.AddSeries("Waermepumpe", Color.Orange, temp_wp);
            _chartManager.AddSeries("Heizstab", Color.Yellow, temp_hs);
            _chartManager.AddSeries("Heizkessel", Color.Blue, temp_hk);
            _chartManager.AddSeries("Solarthermie", Color.Brown, temp_st);
            _chartManager.AddSeries("BHKW", Color.Red, temp_bhkw);
            _chartManager._chart.Series["Waermepumpe"].Enabled = false;
            _chartManager._chart.Series["Heizstab"].Enabled = false;
            _chartManager._chart.Series["Heizkessel"].Enabled = false;
            _chartManager._chart.Series["Solarthermie"].Enabled = false;
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
                _chartManager._chart.Series["Solarthermie"].Enabled = checkBox_ST.Checked;
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
        }

        private void checkBox_ST_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_ST.Checked)
            {
                _chartManager._chart.Series["Solarthermie"].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series["Solarthermie"].Enabled = false;
            }
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
        }
    }
}
