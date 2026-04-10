using System;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class DashboardForm : Form
    {
        public float[] pvProd;
        public float[] stProd;
        public float[] stromBedarf;
        public float[] waermeBedarf;
        public float[] ueberschuss;
        public double speicherKWh;

        public DashboardForm()
        {
            InitializeComponent();

            // Legende erstellen
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            legend1.Name = "DefaultLegend";
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Top; // Position (Top, Bottom, Left, Right)
            legend1.Alignment = System.Drawing.StringAlignment.Center;

            // Legende dem Chart hinzufügen
            this.chartSolar.Legends.Add(legend1);

            // Anzeigenamen für die Legende
            this.chartSolar.Series["Produktion"].LegendText = "PV-Erzeugung (kWh)";
            this.chartSolar.Series["Restbedarf Netz"].LegendText = "Netzbezug (kWh)";
            this.chartSolar.Series["Bedarf"].LegendText = "relativer Strombedarf (kWh)";
            chartSolar.Legends[0].Enabled = true; // Schaltet die erste Legende ein
            chartSolar.Legends[0].IsDockedInsideChartArea = true;
            chartSolar.Legends[0].DockedToChartArea = "MainArea";
        }

        public void UpdateSimulationData()
        {
            // --- 0. Speicher-Parameter ---
            speicherKWh = (double)numSpeicherKWh.Value;
            double aktuellerSpeicherinhalt = 0;

            // --- 1. Berechnungen ---
            double gesStrom = stromBedarf.Sum();
            double gesWaerme = waermeBedarf.Sum();

            double pvPotenzialGesamt = pvProd.Sum();
            double stPotenzialGesamt = stProd.Sum();

            double pvDirekt = 0;
            double pvAusSpeicher = 0;
            double stGenutzt = 0;

            for (int i = 0; i < 8760; i++)
            {
                // PV & Speicher
                double direkt = Math.Min(pvProd[i], stromBedarf[i]);
                pvDirekt += direkt;

                double ueberschuss = pvProd[i] - direkt;
                double bedarfNachDirekt = stromBedarf[i] - direkt;

                // Laden/Entladen
                double ladeMenge = Math.Min(ueberschuss, speicherKWh - aktuellerSpeicherinhalt);
                aktuellerSpeicherinhalt += ladeMenge;
                double entnahme = Math.Min(bedarfNachDirekt, aktuellerSpeicherinhalt);
                aktuellerSpeicherinhalt -= entnahme;
                pvAusSpeicher += entnahme;

                // Solarthermie (Genutzte Wärme)
                stGenutzt += Math.Min(stProd[i], waermeBedarf[i]);
            }

            // --- 2. Kennzahlen ---
            double autarkiePV = gesStrom > 0 ? ((pvDirekt + pvAusSpeicher) / gesStrom) * 100 : 0;
            double deckungST = gesWaerme > 0 ? (stGenutzt / gesWaerme) * 100 : 0;

            // Nutzungsgrad ST: Wie viel der z.B. 4,6 kW Spitze wurden nicht weggeworfen?
            double nutzungsgradST = stPotenzialGesamt > 0 ? (stGenutzt / stPotenzialGesamt) * 100 : 0;

            // CO2 Ersparnis
            double co2Saved = ((pvDirekt + pvAusSpeicher) * 0.42) + (stGenutzt * 0.20);

            // --- 3. UI Updates ---
            lblPVAutarkie.Text = $"{autarkiePV:F1} %";
            pbPV.Value = (int)Math.Min(100, autarkiePV);

            lblSTDeckung.Text = deckungST >0 ? $"{deckungST:F1} %" : "nicht benötigt";
            pbST.Value = (int)Math.Min(100, deckungST);

            // NEU: Anzeige Nutzungsgrad und CO2
            lblNutzungsgradST.Text = $"Therm. Nutzungsgrad: {nutzungsgradST:F1} %";
            lblCO2.Text = $"{co2Saved:N0} kg CO2 / Jahr gespart";


        //    System.Diagnostics.Debug.WriteLine($"PV Check -> Jan: {pvProd.Take(730).Sum():N0} kWh | Jun: {pvProd.Skip(730 * 5).Take(730).Sum():N0} kWh");

            // --- 4. Chart ---
            FillMonthlyChart();

            // Test-Ausgabe in der Konsole oder einem Label
            double gesamtGespeichert = pvAusSpeicher; // Die Variable aus deiner Schleife
            lblTest.Text = $"Speichernutzen: {gesamtGespeichert:N0} kWh/Jahr";

        }

        private void FillMonthlyChart()
        {
            chartSolar.Series["Produktion"].Points.Clear();
            chartSolar.Series["Restbedarf Netz"].Points.Clear();
            chartSolar.Series["Bedarf"].Points.Clear();
            chartSolar.Series["Überschuss"].Points.Clear(); // NEU

            double aktuellerSpeicher = 0;

            for (int m = 0; m < 12; m++)
            {
                double monatsProd = 0;
                double monatsRestBedarf = 0;
                double monatsBedarf = 0;
                double monatsUeberschuss = 0; // NEU

                System.Diagnostics.Debug.WriteLine($"Gesamt PV Erzeugung im Array: {pvProd.Sum()}");

                for (int h = 0; h < 730; h++)
                {
                    int i = (m * 730) + h;
                    if (i >= 8760) break;

                    monatsProd += pvProd[i];
                    monatsBedarf += stromBedarf[i];

                    double direkt = Math.Min(pvProd[i], stromBedarf[i]);
                    double ueberschussStunde = pvProd[i] - direkt;
                    double bedarfNachDirekt = stromBedarf[i] - direkt;

                    // 1. Laden
                    double ladeMenge = Math.Min(ueberschussStunde, speicherKWh - aktuellerSpeicher);
                    aktuellerSpeicher += ladeMenge;

                    // --- NEU: Was jetzt noch übrig ist, ist der echte Überschuss ---
                    monatsUeberschuss += (ueberschussStunde - ladeMenge);

                    // 2. Entladen
                    double entnahme = Math.Min(bedarfNachDirekt, aktuellerSpeicher);
                    aktuellerSpeicher -= entnahme;

                    // 3. Restbedarf
                    monatsRestBedarf += (bedarfNachDirekt - entnahme);
                }

                string monthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m + 1);
                chartSolar.Series["Produktion"].Points.AddXY(monthName, monatsProd);
                chartSolar.Series["Restbedarf Netz"].Points.AddXY(monthName, monatsRestBedarf);
                chartSolar.Series["Bedarf"].Points.AddXY(monthName, monatsBedarf);
                chartSolar.Series["Überschuss"].Points.AddXY(monthName, monatsUeberschuss); // NEU
            }
        }
        private void numSpeicherKWh_ValueChanged(object sender, EventArgs e)
        {
            // Hier die Simulation erneut triggern, um die Auswirkungen sofort zu sehen
            // UpdateSimulationData(letztePVWerte, ...); 
            UpdateSimulationData();
        }
    }
}