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
        private float[] temp_puffer;   // Pufferspeicher-Füllstand [kWh]

        // Checkbox für den Speicherfüllstand (programmatisch, kein Designer nötig)
        private CheckBox checkBox_Puffer;

        public NavigatorWaerme(SimulationControl simctrl)
        {
            InitializeComponent();
            InitPufferCheckBox();
            SetControl(sim = simctrl);
            InitCsvExportButton();
        }

        /// <summary>
        /// Legt die Checkbox "Speicherfüllstand" neben den übrigen Serien-Checkboxen an.
        /// </summary>
        private void InitPufferCheckBox()
        {
            checkBox_Puffer = new CheckBox();
            checkBox_Puffer.Name = "checkBox_Puffer";
            checkBox_Puffer.Text = "Speicherfüllstand";
            checkBox_Puffer.AutoSize = true;
            checkBox_Puffer.Location = new Point(checkBox_BHKW.Right + 15, checkBox_BHKW.Top);
            checkBox_Puffer.CheckedChanged += checkBox_Puffer_CheckedChanged;
            this.Controls.Add(checkBox_Puffer);
            checkBox_Puffer.BringToFront();
        }

        /// <summary>
        /// Legt den CSV-Export-Button rechts neben den Checkboxen an (programmatisch, kein Designer nötig).
        /// </summary>
        private void InitCsvExportButton()
        {
            Button btnExport = new Button();
            btnExport.Name = "btn_CsvExport";
            btnExport.Text = "CSV Export";
            btnExport.Size = new Size(110, 28);
            // Oberhalb des Diagramms rechtsbündig (rechte Kante = Diagrammkante),
            // damit die Checkbox-Zeile darunter frei bleibt.
            btnExport.Location = new Point(chart_Waerme.Right - btnExport.Width, 20);
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnExport.Click += btn_CsvExport_Click;
            this.Controls.Add(btnExport);
            btnExport.BringToFront();
        }

        /// <summary>
        /// Exportiert die aktuell per Checkbox selektierten Serien des Wärme-Charts als CSV
        /// (Zeitstempel, Außentemperatur, Werte — Stundenwerte).
        /// </summary>
        private void btn_CsvExport_Click(object sender, EventArgs e)
        {
            if (sim == null || sim.simulation_Waermebedarf == null || temp_ges == null)
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
            if (checkBox_ST.Checked) spalten.Add(new CsvSpalte("Solarthermie [kW]", temp_st));
            if (checkBox_BHKW.Checked) spalten.Add(new CsvSpalte("BHKW [kW]", temp_bhkw));
            if (checkBox_Puffer != null && checkBox_Puffer.Checked)
                spalten.Add(new CsvSpalte("Speicherfüllstand [kWh]", temp_puffer));

            CsvExportClass.Export("Waermeproduktion.csv",
                sim.simulation_Waermebedarf.Stundentemperatur, spalten, false);
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

            // Pufferspeicher-Füllstand (Energieinhalt) der Wärmepumpe
            temp_puffer = (sim.puffer_wp != null && sim.puffer_wp.SOC_stuendlich != null)
                ? sim.puffer_wp.SOC_stuendlich
                : new float[8760];

            for (int i = 0; i < 8760; i++) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_st[i] + temp_bhkw[i];

            _chartManager = new ChartManager(chart_Waerme);
            _chartManager.BackColor = Color.White;
            _chartManager._chart.BackColor = Color.LightGray;
            // Skalierung so wählen, dass auch der Speicherfüllstand vollständig sichtbar ist
            _chartManager.YMaxValue = Math.Max(temp_ges.Max(), temp_puffer.Max()) + 1;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisAsNumber = false;
            _chartManager.XAxisTitle = "Monate";
            _chartManager.YAxisTitle = "Leistung [kW] / Speicherinhalt [kWh]";
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
            _chartManager.AddSeries("Speicherfüllstand", Color.MediumVioletRed, temp_puffer);
            _chartManager._chart.Series["Waermepumpe"].Enabled = false;
            _chartManager._chart.Series["Heizstab"].Enabled = false;
            _chartManager._chart.Series["Heizkessel"].Enabled = false;
            _chartManager._chart.Series["Solarthermie"].Enabled = false;
            _chartManager._chart.Series["BHKW"].Enabled = false;
            _chartManager._chart.Series["Speicherfüllstand"].Enabled = false;
            checkBox_Gesamt.Checked = true;

            // Checkbox nur anbieten, wenn dem Projekt ein Pufferspeicher zugeordnet ist
            if (checkBox_Puffer != null) checkBox_Puffer.Enabled = (sim.puffer_wp != null);
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
                if (checkBox_Puffer != null && _chartManager._chart.Series.IndexOf("Speicherfüllstand") >= 0)
                    _chartManager._chart.Series["Speicherfüllstand"].Enabled = checkBox_Puffer.Checked;
            }
        }

        private void checkBox_Puffer_CheckedChanged(object sender, EventArgs e)
        {
            if (_chartManager == null || _chartManager._chart.Series.IndexOf("Speicherfüllstand") < 0) return;
            _chartManager._chart.Series["Speicherfüllstand"].Enabled = checkBox_Puffer.Checked;
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
