using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting; // WICHTIG für Charting-Typen

namespace WindowsFormsApplication1
{
    public partial class DashboardForm : Form
    {
        public float[] pvProd;
        public float[] stProd;
        public float[] stromBedarf;
        public float[] waermeBedarf;
        public double speicherKWh = 0;
        
        private bool isUpdatingUI = false; // Event Sperre
        
        public DashboardForm()
        {
            InitializeComponent();

            SetupChart(); // Ruft die neue Setup-Methode auf
        }

        public void Init()
        {
            isUpdatingUI = true; // Event SPERRE AKTIVIEREN
            if (speicherKWh > 0) numSpeicherKWh.Value = (decimal)speicherKWh;
            isUpdatingUI = false; // Event SPERRE DEAKTIVIEREN  
        }

        private void SetupChart()
        {
            // Alle alten Serien löschen, wir bauen sie neu auf
            chartSolar.Series.Clear();
            chartSolar.Legends.Clear();

            // Legende erstellen
            Legend legend1 = new Legend
            {
                Name = "DefaultLegend",
                Docking = Docking.Top,
                Alignment = StringAlignment.Center,
                Enabled = true
            };
            this.chartSolar.Legends.Add(legend1);

            // Serien für gestapelte Säulen definieren
    
            // Serie 1 (Ganz unten): Der Direktverbrauch (Sonne -> Haus)
            Series serDirekt = new Series("Direktverbrauch")
            {
                ChartType = SeriesChartType.StackedColumn,
                Color = Color.Gold, // Gold/Sonnengelb
                LegendText = "Eigenverbrauch (Direkt)"
            };
            chartSolar.Series.Add(serDirekt);

            // Serie 2 (Mitte): Was aus dem Speicher kommt
            Series serSpeicher = new Series("Speichernutzung")
            {
                ChartType = SeriesChartType.StackedColumn,
                Color = Color.LightGreen, // Grün für Speicherstrom
                LegendText = "Eigenverbrauch (Speicher)"
            };
            chartSolar.Series.Add(serSpeicher);

            // Serie 3 (Ganz oben, der Rest): Die Autarkie-Lücke (Netzbezug)
            Series serNetz = new Series("Lücke (Netzbezug)")
            {
                ChartType = SeriesChartType.StackedColumn,
                Color = Color.Red, // Rot für Kosten/Abhängigkeit
                LegendText = "Autarkie-Lücke (Netz)"
            };
            chartSolar.Series.Add(serNetz);

            // Achsen-Konfiguration
            chartSolar.ChartAreas[0].AxisY.Title = "Energie-Bedarf & Deckung (kWh)";
            chartSolar.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 10, FontStyle.Bold);

            // Y-Achse automatisch skalieren, damit der Winter-Bedarf gut sichtbar ist
            // und nicht durch Sommer-PV-Werte "erdrückt" wird.
            chartSolar.ChartAreas[0].AxisY.IsStartedFromZero = true;

            chartSolar.ChartAreas[0].AxisX.Interval = 1; // Jeden Monat anzeigen
            chartSolar.ChartAreas[0].AxisX.Title = "Monat";
        }

        public void UpdateSimulationData()
        {
            isUpdatingUI = true; // Event SPERRE AKTIVIEREN

            // Speicher-Parameter
 
            speicherKWh = (double)numSpeicherKWh.Value;

            double aktuellerSpeicherinhalt = 0;
   
            // Berechnungen
            double gesStromBedarfBrutto = stromBedarf.Sum(); // Brutto-Bedarf
            double gesWaerme = waermeBedarf.Sum();

            double stPotenzialGesamt = stProd.Sum();

            double pvDirektSumme = 0;
            double pvAusSpeicherSumme = 0;
            double stGenutztSumme = 0;

            double StromberdarfBrutto = stromBedarf.Sum();  

            // Jahressimulation
            for (int i = 0; i < 8760; i++)
            {
                // PV & Speicher (auf Brutto-Bedarf gerechnet)
                double direkt = Math.Min(pvProd[i] * 0.95, stromBedarf[i]);
                pvDirektSumme += direkt;

                double ueberschuss = pvProd[i] - direkt;
                double bedarfNachDirekt = stromBedarf[i] - direkt;

                // Laden - Entladen
                double ladeMenge = Math.Min(ueberschuss, speicherKWh - aktuellerSpeicherinhalt);
                aktuellerSpeicherinhalt += ladeMenge;
                double entnahme = Math.Min(bedarfNachDirekt, aktuellerSpeicherinhalt);
                aktuellerSpeicherinhalt -= entnahme;
                pvAusSpeicherSumme += entnahme;

                // Solarthermie (Genutzte Wärme)
                stGenutztSumme += Math.Min(stProd[i], waermeBedarf[i]);
            }

            // Kennzahlen
            // Autarkie: (Direktverbrauch + Speicherentnahme) / Gesamtbedarf    wechselrichterWirkungsgrad = 0.95 Verluste ca. 5%
            double autarkiePV = gesStromBedarfBrutto > 0 ? ((pvDirektSumme + pvAusSpeicherSumme) / gesStromBedarfBrutto) * 100 : 0;
            double deckungST = gesWaerme > 0 ? (stGenutztSumme / gesWaerme) * 100 : 0;

            double nutzungsgradST = stPotenzialGesamt > 0 ? (stGenutztSumme / stPotenzialGesamt) * 100 : 0;

            // CO2 Ersparnis (PV-Deckung + ST-Nutzung)
            double co2Saved = ((pvDirektSumme + pvAusSpeicherSumme) * 0.42) + (stGenutztSumme * 0.20);

            // UI Updates
            lblPVAutarkie.Text = $"{autarkiePV:F1} %";  

            pbPV.Value = (int)Math.Min(100, autarkiePV);

            lblSTDeckung.Text = deckungST > 0 ? $"{deckungST:F1} %" : "nicht benötigt";
            pbST.Value = (int)Math.Min(100, deckungST);

            lblNutzungsgradST.Text = $"Therm. Nutzungsgrad: {nutzungsgradST:F1} %";
            lblCO2.Text = $"{co2Saved:N0} kg CO2 / Jahr gespart";

            // Chart
            FillMonthlyChart();

            // Speichernutzen
            lblTest.Text = $"Speichernutzen: {pvAusSpeicherSumme:N0} kWh/Jahr";

            isUpdatingUI = false; // Event SPERRE DEAKTIVIEREN
        }

        private void FillMonthlyChart()
        {
            // Alle Datenpunkte leeren
            foreach (var series in chartSolar.Series)
            {
                series.Points.Clear();
            }

            // Speichersimulation monatsübergreifend
            double aktuellerSpeicher = 0;

            for (int m = 0; m < 12; m++)
            {
                // Berechnung der 3 Kategorien für den gestapelten Balken
                double monatsDirekt = 0;   // Sonne -> Haus
                double monatsSpeicher = 0; // Speicher -> Haus
                double monatsLuecke = 0;    // Netz -> Haus (Die Lücke)

                // Indizierung basierend auf der 8760/12 Annahme (730 Stunden/Monat)
                for (int h = 0; h < 730; h++)
                {
                    int i = (m * 730) + h;
                    if (i >= 8760) break;

                    // Der Direktverbrauch (Sonne deckt Bedarf direkt)
                    double direkt = Math.Min(pvProd[i], stromBedarf[i]);
                    monatsDirekt += direkt;

                    // Was übrig ist (Überschuss) und was noch fehlt
                    double ueberschuss = pvProd[i] - direkt;
                    double bedarfNachDirekt = stromBedarf[i] - direkt;

                    // Speicher-Logik stündlich
                    // Laden
                    double ladeMenge = Math.Min(ueberschuss, speicherKWh - aktuellerSpeicher);
                    aktuellerSpeicher += ladeMenge;

                    // Entladen (Speicher deckt Restbedarf)
                    double entnahme = Math.Min(bedarfNachDirekt, aktuellerSpeicher);
                    aktuellerSpeicher -= entnahme;
                    monatsSpeicher += entnahme; // WICHTIG: Speicherstrom ist genutzte Energie!

                    // Die Lücke (was Netz noch liefern muss)
                    double restbedarfNachSpeicher = bedarfNachDirekt - entnahme;
                    monatsLuecke += restbedarfNachSpeicher;
                }

                // Monatspunkt hinzufügen (gestapelt übereinander)
                string monthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m + 1);

                // Reihenfolge von unten nach oben hinzufügen
                chartSolar.Series["Direktverbrauch"].Points.AddXY(monthName, monatsDirekt); // Gold unten
                chartSolar.Series["Speichernutzung"].Points.AddXY(monthName, monatsSpeicher); // Grün mitte
                chartSolar.Series["Lücke (Netzbezug)"].Points.AddXY(monthName, monatsLuecke); // Rot oben
            }
        }

        private void numSpeicherKWh_ValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI) return;

            UpdateSimulationData();
        }
    }
}