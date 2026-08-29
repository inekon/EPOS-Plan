using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_ErgProzesswaerme : Form
    {
        SimulationWaermebedarf simulation;

        // Statisches Array für die Monatsbeschriftungen auf der X-Achse
        private readonly string[] monate = { "Jan", "Feb", "Mrz", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

        public Form_ErgProzesswaerme()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            ResetAndInitChart();
        }

        public void Init(SimulationWaermebedarf waermebedarf_simulation)
        {
            simulation = waermebedarf_simulation;

            // Textfelder befüllen
            textBox_WB_Gebaeude.Text = simulation.Waermebedarf_Gebaeude_Gesamt.ToString("F2");
            textBox_Brauchwasser.Text = simulation.Waermebedarf_Brauchwasser.ToString("F2");
            textBox_WB_Extern.Text = simulation.Waermebedarf_Extern_Gesamt.ToString("F2");
            textBox_MaxWaermelast.Text = simulation.Waermebedarf_Max.ToString("F2");
            textBox_Netzverluste.Text = simulation.Waermebedarf_Netzverluste.ToString("F2");
            textBox_WB_Gesamt.Text = simulation.Waermebedarf_Gesamt.ToString("F2");
            textBox_WB_Prozess.Text = simulation.Waermebedarf_Prozess.ToString("F2");

            // RadioButtons aktivieren (Das löst die CheckedChanged-Events aus)
            radioBtn_Prozesse.Checked = true;
            radioBtn_GrafikProzesse.Checked = true;

            // Sicherheits-Erstbefüllung der Grafik, falls Events beim Laden blockieren
            ZeigeMonatsGrafik("Prozesswärme", simulation.Waermebedarf_Prozess_Monat, Color.Red);
        }

        /// <summary>
        /// Bereitet das Chart-Control jungfräulich vor und fixiert das 12-Monats-Raster.
        /// </summary>
        private void ResetAndInitChart()
        {
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            chart1.Titles.Clear();

            // Eine neue Standard-Zeichenfläche hinzufügen
            ChartArea area = new ChartArea("MainArea");

            // Erzwingt das starre 12-Monats-Layout auf der X-Achse
            area.AxisX.Interval = 1;           // Zeige jeden Monat als Beschriftung
            area.AxisX.IsMarginVisible = true; // Abstand links und rechts in der Grafik halten
            area.AxisX.Minimum = 1;            // Beginne starr bei Position 1
            area.AxisX.Maximum = 12;           // Ende starr bei Position 12
            area.AxisY.Minimum = 0;            // Y-Achse startet immer bei 0

            chart1.ChartAreas.Add(area);

            // Titel hinzufügen
            chart1.Titles.Add(new Title("Wärmelast Monatsübersicht", Docking.Top, new Font("Arial", 12, FontStyle.Bold), Color.Black));
        }

        /// <summary>
        /// Zeichnet die übergebenen Monatsdaten als saubere 12 Balken.
        /// </summary>
        private void ZeigeMonatsGrafik(string serienName, float[] monatsDaten, Color balkenFarbe)
        {
            // Abbrechen, falls noch keine Daten geladen sind
            if (monatsDaten == null || monatsDaten.Length < 12) return;

            chart1.Series.Clear();

            Series serie = new Series(serienName)
            {
                ChartType = SeriesChartType.Column, // Column = Vertikales Balkendiagramm
                Color = balkenFarbe
            };

            // Die 12 Monate explizit an die fixierten X-Positionen 1 bis 12 binden
            for (int i = 0; i < 12; i++)
            {
                DataPoint p = new DataPoint();
                p.SetValueXY(i + 1, monatsDaten[i]); // X = Position (1-12), Y = Wert (kW)
                p.AxisLabel = monate[i];             // Textbeschriftung ("Jan", "Feb", etc.)
                serie.Points.Add(p);
            }

            chart1.Series.Add(serie);

            // Legende ausblenden
            if (chart1.Legends.Count > 0)
            {
                chart1.Legends[0].Enabled = false;
            }

            // --- VOLLKOMMEN GENERISCHE Y-ACHSEN-SKALIERUNG ---
            double maxWert = monatsDaten.Max();

            if (maxWert > 0)
            {
                double[] schoeneSchritte = { 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 2.5, 5.0, 10.0 };
                double zielSchrittweite = (maxWert * 1.1) / 4.5;
                double groessenordnung = Math.Pow(10, Math.Floor(Math.Log10(zielSchrittweite)));
                double normierteSchrittweite = zielSchrittweite / groessenordnung;

                double gewaehlterSchritt = schoeneSchritte.Last();
                foreach (double schritt in schoeneSchritte)
                {
                    if (normierteSchrittweite <= schritt)
                    {
                        gewaehlterSchritt = schritt;
                        break;
                    }
                }

                double finaleSchrittweite = gewaehlterSchritt * groessenordnung;
                double geglaettetesMaximum = Math.Round(Math.Ceiling((maxWert * 1.05) / finaleSchrittweite) * finaleSchrittweite, 4);

                if (finaleSchrittweite <= 0) { finaleSchrittweite = 0.5; geglaettetesMaximum = 2.0; }

                chart1.ChartAreas[0].AxisY.Interval = finaleSchrittweite;
                chart1.ChartAreas[0].AxisY.Maximum = geglaettetesMaximum;

                // Nachkommastellen dynamisch anpassen
                if (finaleSchrittweite >= 1.0)
                    chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N0";
                else if (finaleSchrittweite >= 0.1)
                    chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N1";
                else
                    chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N2";
            }
            else
            {
                chart1.ChartAreas[0].AxisY.Maximum = 5;
                chart1.ChartAreas[0].AxisY.Interval = 1;
                chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N0";
            }

            chart1.Invalidate(); // Neuzeichnen der Grafik erzwingen
        }

        // --- Event-Handler für die Grafik-RadioButtons ---

        private void radioBtn_GrafikProzesse_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtn_GrafikProzesse.Checked && simulation != null)
            {
                ZeigeMonatsGrafik("Prozesswärme", simulation.Waermebedarf_Prozess_Monat, Color.Red);
            }
        }

        private void radioBtn_GrafikGebäude_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtn_GrafikGebäude.Checked && simulation != null)
            {
                ZeigeMonatsGrafik("Gebäudewärme", simulation.Waermebedarf_Gebaeude_Monat, Color.Blue);
            }
        }

        // --- Event-Handler für die Text-Tabellen-RadioButtons ---

        private void radioBtn_Prozesse_CheckedChanged(object sender, EventArgs e)
        {
            if (simulation?.Waermebedarf_Prozess_Monat == null) return;
            Monat_1.Text = simulation.Waermebedarf_Prozess_Monat[0].ToString("F2");
            Monat_2.Text = simulation.Waermebedarf_Prozess_Monat[1].ToString("F2");
            Monat_3.Text = simulation.Waermebedarf_Prozess_Monat[2].ToString("F2");
            Monat_4.Text = simulation.Waermebedarf_Prozess_Monat[3].ToString("F2");
            Monat_5.Text = simulation.Waermebedarf_Prozess_Monat[4].ToString("F2");
            Monat_6.Text = simulation.Waermebedarf_Prozess_Monat[5].ToString("F2");
            Monat_7.Text = simulation.Waermebedarf_Prozess_Monat[6].ToString("F2");
            Monat_8.Text = simulation.Waermebedarf_Prozess_Monat[7].ToString("F2");
            Monat_9.Text = simulation.Waermebedarf_Prozess_Monat[8].ToString("F2");
            Monat_10.Text = simulation.Waermebedarf_Prozess_Monat[9].ToString("F2");
            Monat_11.Text = simulation.Waermebedarf_Prozess_Monat[10].ToString("F2");
            Monat_12.Text = simulation.Waermebedarf_Prozess_Monat[11].ToString("F2");
        }

        private void radioBtn_Gebäude_CheckedChanged(object sender, EventArgs e)
        {
            if (simulation?.Waermebedarf_Gebaeude_Monat == null) return;
            Monat_1.Text = simulation.Waermebedarf_Gebaeude_Monat[0].ToString("F2");
            Monat_2.Text = simulation.Waermebedarf_Gebaeude_Monat[1].ToString("F2");
            Monat_3.Text = simulation.Waermebedarf_Gebaeude_Monat[2].ToString("F2");
            Monat_4.Text = simulation.Waermebedarf_Gebaeude_Monat[3].ToString("F2");
            Monat_5.Text = simulation.Waermebedarf_Gebaeude_Monat[4].ToString("F2");
            Monat_6.Text = simulation.Waermebedarf_Gebaeude_Monat[5].ToString("F2");
            Monat_7.Text = simulation.Waermebedarf_Gebaeude_Monat[6].ToString("F2");
            Monat_8.Text = simulation.Waermebedarf_Gebaeude_Monat[7].ToString("F2");
            Monat_9.Text = simulation.Waermebedarf_Gebaeude_Monat[8].ToString("F2");
            Monat_10.Text = simulation.Waermebedarf_Gebaeude_Monat[9].ToString("F2");
            Monat_11.Text = simulation.Waermebedarf_Gebaeude_Monat[10].ToString("F2");
            Monat_12.Text = simulation.Waermebedarf_Gebaeude_Monat[11].ToString("F2");
        }

        public void SetPage(int page)
        {
            tabControl1.SelectedIndex = page;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}