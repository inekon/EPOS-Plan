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
    public partial class Form_ErgBrauchwasserwaerme : Form
    {
        SimulationWaermebedarf simulation;

        // Statisches Array für die Monatsbeschriftungen auf der X-Achse
        private readonly string[] monate = { "Jan", "Feb", "Mrz", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

        // ChartManager für die Jahresansicht: numerische X-Achse + Mausrad-Zoom
        // (die Achsenbeschriftung passt sich beim Zoomen automatisch der Auflösung an)
        private ChartManager _chartManager;

        public Form_ErgBrauchwasserwaerme()
        {
            InitializeComponent();
            ResetAndInitChart();
        }

        public void Init(SimulationWaermebedarf waermebedarf_simulation)
        {
            simulation = waermebedarf_simulation;

            // Textfelder befüllen
            textBox_WB_Gebaeude.Text = simulation.Waermebedarf_Gebaeude_Gesamt.ToString("F2");
            textBox_WB_Brauchwasser.Text = (simulation.Waermebedarf_Brauchwasser / 1000).ToString("F2");
            textBox_WB_Extern.Text = simulation.Waermebedarf_Extern_Gesamt.ToString("F2");
            textBox_MaxWaermelast.Text = simulation.Waermebedarf_Max.ToString("F2");
            textBox_Netzverluste.Text = simulation.Waermebedarf_Netzverluste.ToString("F2");
            textBox_WB_Gesamt.Text = simulation.Waermebedarf_Gesamt.ToString("F2");
            textBox_WB_Prozess.Text = simulation.Waermebedarf_Prozess.ToString("F2");

            // RadioButtons aktivieren (Das löst die CheckedChanged-Events aus)
            radioBtn_Prozesse.Checked = true;
            radioBtn_GrafikProzesse.Checked = true;

            // Sicherheits-Erstbefüllung der Grafik, falls Events beim Laden blockieren
            AktualisiereGrafik();
        }

        /// <summary>
        /// Bereitet das Chart-Control vor und fixiert das 12-Monats-Raster.
        /// </summary>
        private void ResetAndInitChart()
        {
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            chart1.Titles.Clear();

            // Eine neue Standard-Zeichenfläche hinzufügen
            ChartArea area = new ChartArea("MainArea");

            // WICHTIG: Erzwingt das starre 12-Monats-Layout auf der X-Achse
            area.AxisX.Interval = 1;           // Zeige jeden Monat als Beschriftung
            area.AxisX.IsMarginVisible = true; // Abstand links und rechts in der Grafik halten
            area.AxisX.Minimum = 1;            // Beginne starr bei Position 1
            area.AxisX.Maximum = 12;           // Ende starr bei Position 12
            area.AxisY.Minimum = 0;            // Y-Achse startet immer bei 0

            chart1.ChartAreas.Add(area);

            // Titel hinzufügen
            chart1.Titles.Add(new Title("Wärmelast Monatsübersicht", Docking.Top, new System.Drawing.Font("Arial", 12, FontStyle.Bold), Color.Black));
        }

        /// <summary>
        /// Zeichnet die übergebenen Monatsdaten als saubere 12 Balken.
        /// </summary>
        private void ZeigeMonatsGrafik(string serienName, float[] monatsDaten, Color balkenFarbe)
        {
            if (monatsDaten == null || monatsDaten.Length < 12) return;

            ChartManagerAus();   // evtl. aktive Jahres-/Zoom-Ansicht sauber beenden

            chart1.Series.Clear();

            // Monats-Layout auf der X-Achse wiederherstellen (falls zuvor Jahresansicht aktiv war)
            Axis xMonat = chart1.ChartAreas[0].AxisX;
            xMonat.Minimum = 1;
            xMonat.Maximum = 12;
            xMonat.Interval = 1;
            xMonat.IsMarginVisible = true;
            xMonat.Title = "";
            xMonat.LabelStyle.Format = "";
            xMonat.CustomLabels.Clear();   // evtl. Monats-CustomLabels der Jahresansicht entfernen
            chart1.Titles[0].Text = "Monatsübersicht";

            Series serie = new Series(serienName)
            {
                ChartType = SeriesChartType.Column,
                Color = balkenFarbe
            };

            for (int i = 0; i < 12; i++)
            {
                DataPoint p = new DataPoint();
                p.SetValueXY(i + 1, monatsDaten[i]);
                p.AxisLabel = monate[i];
                serie.Points.Add(p);
            }

            chart1.Series.Add(serie);

            // Y-Achse generisch skalieren
            SkaliereYAchse(monatsDaten.Max());

            chart1.Legends[0].Enabled = false;
            chart1.Invalidate();
        }

        // --- Event-Handler für die Grafik-RadioButtons ---

        private void radioBtn_GrafikProzesse_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtn_GrafikProzesse.Checked) AktualisiereGrafik();
        }

        private void radioBtn_GrafikGebäude_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtn_GrafikGebäude.Checked) AktualisiereGrafik();
        }

        private void radioBtn_GrafikBrauchwasser_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBtn_GrafikBrauchwasser.Checked) AktualisiereGrafik();
        }

        /// <summary>
        /// Zeichnet die Grafik neu – je nach Zustand der Checkbox als Jahres- oder Monatsansicht.
        /// </summary>
        private void AktualisiereGrafik()
        {
            checkBox_MonatJahr.Visible = false;
            if (simulation == null) return;

            if (radioBtn_GrafikGebäude.Checked)
                ZeigeMonatsGrafik("Gebäudewärme", simulation.Waermebedarf_Gebaeude_Monat, Color.Blue);
            else if (radioBtn_GrafikBrauchwasser.Checked)
            {
                checkBox_MonatJahr.Visible = true;
                if (checkBox_MonatJahr != null && checkBox_MonatJahr.Checked)
                {
                    ZeigeJahresGrafik("Brauchwasserwärme", simulation.brauchwasserwerte, Color.SteelBlue);
                    return;
                }
                else ZeigeMonatsGrafik("Brauchwasserwärme", simulation.Waermebedarf_Brauchwasser_Monat, Color.Orange);
            }
            else
                ZeigeMonatsGrafik("Prozesswärme", simulation.Waermebedarf_Prozess_Monat, Color.Red);
        }

        /// <summary>
        /// Zeichnet den gesamten Wärmebedarf als Jahres-Lastkurve über alle 8760 Stunden.
        /// </summary>
        private void ZeigeJahresGrafik(string serienName, float[] stundenDaten, Color linienFarbe)
        {
            if (stundenDaten == null || stundenDaten.Length == 0) return;

            // Jahresansicht über den ChartManager: numerische X-Achse + Mausrad-Zoom.
            // Beim Spreizen mit dem Mausrad passt der ChartManager die X-Achsenbeschriftung
            // automatisch an die sichtbare Auflösung an (Tages-/Wochen-/Monatsschritte).
            if (_chartManager == null) _chartManager = new ChartManager(chart1);

            _chartManager.XAxisAsNumber = false;    // numerische X-Achse (Jahresstunden)
            _chartManager.IsXYChart = false;
            _chartManager.AreaLine = false;        // Linie statt Fläche
            _chartManager.WheelZoomed = true;      // Mausrad-Zoom aktiv
            _chartManager.MitLegende = false;
            _chartManager.MitViertelStunde = false;
            _chartManager.MaxXVALUE = stundenDaten.Length;   // generisch (hier 8760) statt fix
            _chartManager.XAxisTitle = "Jahresstunde";
            _chartManager.YAxisTitle = "Wärmebedarf";
            _chartManager.toolTipUnit = "kW";
            _chartManager.ChartTitle = "Jahresübersicht (Mausrad = Zoom)";

            double maxWert = 0;
            for (int i = 0; i < stundenDaten.Length; i++)
                if (stundenDaten[i] > maxWert) maxWert = stundenDaten[i];
            _chartManager.YMinValue = 0;
            _chartManager.YMaxValue = maxWert > 0 ? maxWert : 1;

            _chartManager.Init();   // Achsen/Zoom/Legende/Titel neu aufbauen, Serien leeren
            _chartManager.AddSeries(serienName, linienFarbe, stundenDaten);
            chart1.Invalidate();
        }

        /// <summary>
        /// Beendet eine ggf. aktive ChartManager-/Mausrad-Zoom-Ansicht und stellt das
        /// Grundgerüst (ChartArea, Titel, Legende) für die Monatsdarstellung wieder her.
        /// </summary>
        private void ChartManagerAus()
        {
            if (_chartManager != null)
            {
                _chartManager.HardReset();   // Mausrad-Handler abmelden, Serien/Titel/Legenden leeren
                _chartManager = null;
            }

            // Chart-Grundgerüst (ChartArea + fixes 12-Monats-Raster + Titel) neu aufbauen
            ResetAndInitChart();

            // Die Monatsansicht setzt chart1.Legends[0].Enabled = false -> Legende sicherstellen
            if (chart1.Legends.Count == 0) chart1.Legends.Add(new Legend());
        }

        /// <summary>
        /// Extrahiert aus einem Jahresverlauf (8760 h) genau eine Woche (168 h) in ein neues Array.
        /// wochenNr = 1 liefert die Stunden 0..167, wochenNr = 2 die Stunden 168..335 usw.
        /// Fehlende Stunden am Jahresende werden mit 0 aufgefüllt.
        /// </summary>
        private float[] ExtrahiereWoche(float[] jahresDaten, int wochenNr)
        {
            float[] woche = new float[168];
            if (jahresDaten == null || jahresDaten.Length == 0) return woche;
            if (wochenNr < 1) wochenNr = 1;

            int start = (wochenNr - 1) * 168;
            for (int i = 0; i < 168; i++)
            {
                int idx = start + i;
                woche[i] = (idx < jahresDaten.Length) ? jahresDaten[idx] : 0f;
            }
            return woche;
        }

        /// <summary>
        /// Zeichnet eine Woche (168 h) aus dem Jahresverlauf. Die X-Achse ist dezimal von 1..168.
        /// </summary>
        private void ZeigeWochenGrafik(string serienName, float[] jahresDaten, int wochenNr, Color linienFarbe)
        {
            // Basis-Array für das Chart: 168 Stundenwerte der gewählten Woche
            float[] wochenDaten = ExtrahiereWoche(jahresDaten, wochenNr);

            chart1.Series.Clear();

            Series serie = new Series(serienName)
            {
                ChartType = SeriesChartType.FastLine,
                Color = linienFarbe,
                BorderWidth = 1
            };

            for (int i = 0; i < wochenDaten.Length; i++)
            {
                serie.Points.AddXY(i + 1, wochenDaten[i]);   // X = 1..168
            }

            chart1.Series.Add(serie);

            // X-Achse dezimal 1..168 (evtl. Monats-CustomLabels der Jahresansicht entfernen)
            Axis xWoche = chart1.ChartAreas[0].AxisX;
            xWoche.CustomLabels.Clear();
            xWoche.Minimum = 1;
            xWoche.Maximum = 168;
            xWoche.Interval = 24;                 // eine Beschriftung je Tag (24, 48, ...)
            xWoche.IsMarginVisible = false;
            xWoche.Title = "Stunde der Woche";
            xWoche.LabelStyle.Format = "N0";
          
            chart1.Titles[0].Text = "Wochenübersicht (Woche " + wochenNr + ")";

            // Y-Achse generisch skalieren
            double maxWert = 0;
            for (int i = 0; i < wochenDaten.Length; i++)
                if (wochenDaten[i] > maxWert) maxWert = wochenDaten[i];
            SkaliereYAchse(maxWert);

            chart1.Legends[0].Enabled = false;
            chart1.Invalidate();
        }

        /// <summary>
        /// Vollkommen generische, "schöne" Y-Achsen-Skalierung anhand des Maximalwerts.
        /// </summary>
        private void SkaliereYAchse(double maxWert)
        {
            if (maxWert > 0)
            {
                // 1. Definiere typische, "schöne" Schrittweiten-Stufen für den Menschen
                double[] schoeneSchritte = { 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 2.5, 5.0, 10.0 };

                // 2. Berechne die grobe Ziel-Schrittweite, wenn wir die Achse in ca. 4 bis 5 Teile zerlegen wollen
                double zielSchrittweite = (maxWert * 1.1) / 4.5;

                // 3. Bestimme die logarithmische Größenordnung (Zehnerpotenz, z.B. 0.1, 1, 10, 100)
                double groessenordnung = Math.Pow(10, Math.Floor(Math.Log10(zielSchrittweite)));
                double normierteSchrittweite = zielSchrittweite / groessenordnung;

                // 4. Finde den passenden "schönen" Schritt aus unserem Array
                double gewaehlterSchritt = schoeneSchritte.Last();
                foreach (double schritt in schoeneSchritte)
                {
                    if (normierteSchrittweite <= schritt)
                    {
                        gewaehlterSchritt = schritt;
                        break;
                    }
                }

                // 5. Berechne die endgültige, reale Schrittweite und das Maximum
                double finaleSchrittweite = gewaehlterSchritt * groessenordnung;
                double geglaettetesMaximum = Math.Round(Math.Ceiling((maxWert * 1.05) / finaleSchrittweite) * finaleSchrittweite, 4);

                // Sicherheits-Check gegen Rundungsfehler bei sehr kleinen Werten
                if (finaleSchrittweite <= 0) { finaleSchrittweite = 0.5; geglaettetesMaximum = 2.0; }

                // 6. Zuweisung an das Chart
                chart1.ChartAreas[0].AxisY.Minimum = 0;
                chart1.ChartAreas[0].AxisY.Interval = finaleSchrittweite;
                chart1.ChartAreas[0].AxisY.Maximum = geglaettetesMaximum;
                

                // 7. Nachkommastellen dynamisch anpassen (Ganze Zahlen, wenn möglich, sonst 1-2 Stellen)
                if (finaleSchrittweite >= 1.0)
                    chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N0"; // Ganze Zahlen (z.B. 5, 10, 15)
                else if (finaleSchrittweite >= 0.1)
                    chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N1"; // Eine Nachkommastelle (z.B. 0.5, 1.0)
                else
                    chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N2"; // Zwei Nachkommastellen
            }
            else
            {
                // Fallback, wenn alle Daten 0 sind
                chart1.ChartAreas[0].AxisY.Minimum = 0;
                chart1.ChartAreas[0].AxisY.Maximum = 5;
                chart1.ChartAreas[0].AxisY.Interval = 1;
                chart1.ChartAreas[0].AxisY.LabelStyle.Format = "N0";
            }
        }


        // --- Event-Handler für die Text-Tabellen-RadioButtons (unverändert) ---

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

        private void radioBtn_Brauchwasser_CheckedChanged(object sender, EventArgs e)
        {
            if (simulation?.Waermebedarf_Brauchwasser_Monat == null) return;
            Monat_1.Text = simulation.Waermebedarf_Brauchwasser_Monat[0].ToString("F2");
            Monat_2.Text = simulation.Waermebedarf_Brauchwasser_Monat[1].ToString("F2");
            Monat_3.Text = simulation.Waermebedarf_Brauchwasser_Monat[2].ToString("F2");
            Monat_4.Text = simulation.Waermebedarf_Brauchwasser_Monat[3].ToString("F2");
            Monat_5.Text = simulation.Waermebedarf_Brauchwasser_Monat[4].ToString("F2");
            Monat_6.Text = simulation.Waermebedarf_Brauchwasser_Monat[5].ToString("F2");
            Monat_7.Text = simulation.Waermebedarf_Brauchwasser_Monat[6].ToString("F2");
            Monat_8.Text = simulation.Waermebedarf_Brauchwasser_Monat[7].ToString("F2");
            Monat_9.Text = simulation.Waermebedarf_Brauchwasser_Monat[8].ToString("F2");
            Monat_10.Text = simulation.Waermebedarf_Brauchwasser_Monat[9].ToString("F2");
            Monat_11.Text = simulation.Waermebedarf_Brauchwasser_Monat[10].ToString("F2");
            Monat_12.Text = simulation.Waermebedarf_Brauchwasser_Monat[11].ToString("F2");
        }

        public void SetPage(int page)
        {
            tabControl1.SelectedIndex = page;
            if (page == 0)
            {
                radioBtn_Prozesse.Checked = true;
                radioBtn_GrafikProzesse.Checked = true;
            }
            else if (page == 1)
            {
                radioBtn_Gebäude.Checked = true;
                radioBtn_GrafikGebäude.Checked = true;
            }
            else if (page == 2)
            {
                radioBtn_Brauchwasser.Checked = true;
                radioBtn_GrafikBrauchwasser.Checked = true;
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBox_MonatJahr_CheckedChanged(object sender, EventArgs e)
        {
            AktualisiereGrafik();
        }
    }
}