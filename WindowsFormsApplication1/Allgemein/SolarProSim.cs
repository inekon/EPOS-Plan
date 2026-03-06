using System;
using System.Linq;

public class SolarProSim
{
    public struct Result
    {
        public double[] Rest;    // Nachheizung (kWh)
        public double[] Solar;   // Solarer Ertrag (kWh)
        public double[] TSpeicherOben; // Temperatur für Heizung (°C)
        public double Deckung;   // Prozentualer Anteil
    }

    public Result Berechne(double[] strahlung, double[] bedarf, double[] tAussen, double kapazitaetKWh, double neigung)
    {
        int n = 8760;
        var res = new Result { 
            Rest = new double[n], Solar = new double[n], TSpeicherOben = new double[n] 
        };
        
        double tRaumSoll = 20.0;
        double aktuellerStandKWh = kapazitaetKWh * 0.4; // Startfüllung

        for (int i = 0; i < n; i++)
        {
            // --- 1. SCHICHTUNG (Zwei-Zonen-Modell) ---
            // Wir schätzen die Temperaturen basierend auf der Energie (kWh)
            // Annahme: 1000L (ca. 60kWh bei 50 Grad Delta)
            double tSchnitt = (aktuellerStandKWh / (kapazitaetKWh / 60.0)) + 20.0;
            double tOben = tSchnitt + 12.0;  // Heiße Zone oben (Heizung/WW)
            double tUnten = Math.Max(tAussen[i], tSchnitt - 12.0); // Kalte Zone unten (Solar)
            res.TSpeicherOben[i] = tOben;

            // --- 2. SOLAR-ERTRAG (nutzt tUnten -> hoher Wirkungsgrad!) ---
            double deltaT_Solar = Math.Max(0, tUnten - tAussen[i]);
            double eta = Math.Max(0, 0.8 - (3.5 * deltaT_Solar / (strahlung[i] + 1)));
            double solarInput = (strahlung[i] * 20.0 * eta) / 1000.0;
            
            // Speicher füllen (begrenzt durch Kapazität)
            aktuellerStandKWh = Math.Min(kapazitaetKWh, aktuellerStandKWh + solarInput);

            // --- 3. HEIZKURVE & NACHHEIZUNG ---
            double tVorlaufSoll = tRaumSoll + neigung * (tRaumSoll - tAussen[i]);
            tVorlaufSoll = Math.Max(tVorlaufSoll, 45.0); // Warmwasser-Minimum

            // Umrechnung: Wie viel Energie muss im Speicher sein, damit tOben >= tVorlaufSoll?
            double minKWh = (tVorlaufSoll - 20.0 - 12.0) * (kapazitaetKWh / 60.0);

            // Bedarf abziehen
            aktuellerStandKWh -= bedarf[i];

            if (aktuellerStandKWh < minKWh)
            {
                res.Rest[i] = minKWh - aktuellerStandKWh;
                aktuellerStandKWh = minKWh;
            }
            
            res.Solar[i] = Math.Max(0, bedarf[i] - res.Rest[i]);
        }

        res.Deckung = (1 - (res.Rest.Sum() / bedarf.Sum())) * 100;
        return res;
    }
}
