using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class SimulationSPK
    {
        public const int MAX_SPK = 10;

        // Listen und Projektdaten
        public List<string> spk_list = new List<string>();
        public int m_ID_Projekt = 0;
        public double Max_Waermebedarf;
        public float[] Waermebedarf = new float[8760];
        public float[] Restwaerme = new float[8760];
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] Kesselleistung_stuendlich = new float[8760];

        // Parameter und globale Ergebnisse
        public int Vorgabe_Betriebsbereitschaft = 6000;
        public double Waermebedarf_gesamt = 0;
        public double Strombedarf_gesamt = 0;
        public double Maximale_Kesselleistung_Spk = 0;
        public double Stromverbrauch_Spk = 0;
        public double BruttoWaermeSpkErzeugung = 0;
        public double S_Waerme_spk = 0;
        public double Gasspitze_Spk = 0;

        // Globale Brennstoffzähler (in MWh)
        public double Gasverbrauch_SPK = 0;
        public double Oelverbrauch_SPK = 0;
        public double Rapsoelverbrauch_SPK = 0;
        public double Holzverbrauch_SPK = 0;
        public double Sonstigverbrauch_SPK = 0;
        public double Koks_SPK = 0;
        public double Kohle_SPK = 0;
        public double Pellets_SPK = 0;
        public double TierischeFette_SPK = 0;

        // Emissionen gesamt in kg
        public double Em_CO2_SPK = 0;
        public double Em_CO_SPK = 0;
        public double Em_SO2_SPK = 0;
        public double Em_NOX_SPK = 0;
        public double Em_Staub_SPK = 0;

        // Emissionen je Kessel
        public double[] CO2_SPK = new double[MAX_SPK];
        public double[] CO_SPK = new double[MAX_SPK];
        public double[] SO2_SPK = new double[MAX_SPK];
        public double[] NOX_SPK = new double[MAX_SPK];
        public double[] Staub_SPK = new double[MAX_SPK];

        // Kesselspezifische Arrays (Nutzwärme und Wirkungsgrade)
        public double[] s_waerme_Oel_Spk = new double[MAX_SPK];
        public double[] s_waerme_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Oel_Spk = new double[MAX_SPK];

        // Speicher für die korrekte Nutzungsgrad-Bilanz
        public double[] Kessel_Jahresnutzungsgrad_Spk = new double[MAX_SPK];
        private double[] Kessel_Verbrauch_MWh_Spk = new double[MAX_SPK];
        private double[] Kessel_Basis_Wirkungsgrad = new double[MAX_SPK];

        // Interne Kesselkonfigurationen
        double[] Betriebsbereitschaft_Verluste = new double[MAX_SPK];
        double[] Betriebsstunden = new double[MAX_SPK];
        string[] Kessel_Name = new string[MAX_SPK];
        int[] Brennstoff_Betrieb_Spk = new int[MAX_SPK];
        int[] Brennstoff_Art = new int[MAX_SPK];
        double[] Kessel_Leistung_Spk = new double[MAX_SPK];
        int Bereitschaft = 6000;

        public bool Berechnung(int ID_Projekt)
        {
            int Anzahl = 0;
            m_ID_Projekt = ID_Projekt;

            Init();

            // 1. Gesamten Wärmebedarf ermitteln (in MWh)
            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000;

            HeizkesselCtrl heizkesselctrl = new HeizkesselCtrl();
            Anzahl = spk_list.Count;
            if (Anzahl == 0) { Restwaerme = Waermebedarf; return true; }

            // 2. Kesseldaten laden und Wirkungsgrade normieren
            for (int i = 0; i < Anzahl; i++)
            {
                heizkesselctrl.ReadAll("Name='" + spk_list[i] + "'");
                Kessel_Name[i] = heizkesselctrl.items[0].Name;
                Kessel_Leistung_Spk[i] = heizkesselctrl.items[0].Ptherm;

                // Emissionen aus Brennstoff Tabelle für Brennstoff
                DataTable dt = DataRepository.GetDataTable("select * from Tab_Brennstoff_Stamm where ID=?", new OleDbParameter("@s1", heizkesselctrl.items[0].Brennstoff));
                DataRow row = dt.Rows[0];
                if (row != null)
                {
                    CO2_SPK[i] = row["CO2"] != DBNull.Value ? Convert.ToDouble(row["CO2"]) : 0;
                    SO2_SPK[i] = row["SO2"] != DBNull.Value ? Convert.ToDouble(row["SO2"]) : 0;
                    NOX_SPK[i] = row["NOX"] != DBNull.Value ? Convert.ToDouble(row["NOX"]) : 0;
                    // nicht enthalten CO_SPK[i] = row["CO"] != DBNull.Value ? Convert.ToDouble(row["CO"]) : 0;
                    Staub_SPK[i] = row["Staub"] != DBNull.Value ? Convert.ToDouble(row["Staub"]) : 0;
                }

                // Wirkungsgrade einlesen
                Kessel_Wirk_Gas_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Gas;
                Kessel_Wirk_Oel_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Oel;

                // Absicherung: Falls die DB "88" statt "0.88" liefert, wandeln wir es in einen Faktor um
                if (Kessel_Wirk_Gas_Spk[i] > 1.0) Kessel_Wirk_Gas_Spk[i] /= 100.0;
                if (Kessel_Wirk_Oel_Spk[i] > 1.0) Kessel_Wirk_Oel_Spk[i] /= 100.0;

                // Basis-Wirkungsgrad für unsere saubere Bilanzierung wegsichern
                Kessel_Basis_Wirkungsgrad[i] = Kessel_Wirk_Gas_Spk[i] > 0 ? Kessel_Wirk_Gas_Spk[i] : Kessel_Wirk_Oel_Spk[i];
                if (Kessel_Basis_Wirkungsgrad[i] <= 0) Kessel_Basis_Wirkungsgrad[i] = 0.90; // Fallback

                Brennstoff_Betrieb_Spk[i] = heizkesselctrl.items[0].Brennstoff;
                Brennstoff_Art[i] = Brennstoff_Betrieb_Spk[i];

                Betriebsbereitschaft_Verluste[i] = heizkesselctrl.items[0].Betriebsbereitschaftverlust;
                if (Betriebsbereitschaft_Verluste[i] > 1.0) Betriebsbereitschaft_Verluste[i] /= 100.0; // Ebenfalls normieren

                Maximale_Kesselleistung_Spk += Kessel_Leistung_Spk[i];
            }

            // 3. Die stündliche Simulation durchführen (Akkumuliert Nutzwärme-Arrays stündlich in kW)
            Heizkessel_Simulation(Waermebedarf, ref Gasspitze_Spk, s_waerme_Gas_Spk, s_waerme_Oel_Spk,
                Max_Waermebedarf, Anzahl, Kessel_Leistung_Spk, Kessel_Wirk_Gas_Spk, Brennstoff_Betrieb_Spk);

            // 4. Verbrauch, Stillstandsverluste und Brennstoffe präzise bilanzieren
            for (int i = 0; i < Anzahl; i++)
            {
                Bereitschaft = Vorgabe_Betriebsbereitschaft;

                // WICHTIG: Wenn es nicht der allerletzte Kessel ist, hält er das ganze Jahr über (8760h) die Bereitschaft!
                if (i < Anzahl - 1) Bereitschaft = 8760;

                // Erzeugte Nutzwärme dieses Kessels aus der Simulation holen (wurde am Ende der Simulation in MWh umgerechnet)
                double Kessel_Nutzkraft_Jahr = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i];
                S_Waerme_spk += Kessel_Nutzkraft_Jahr;

                // Reale Volllaststunden des Kessels berechnen
                // 1.Volllaststunden berechnen
                if (Kessel_Nutzkraft_Jahr > 0.0001 && Kessel_Leistung_Spk[i] > 0)
                {
                    Betriebsstunden[i] = (Kessel_Nutzkraft_Jahr * 1000) / Kessel_Leistung_Spk[i];
                }
                else
                {
                    Betriebsstunden[i] = 0;
                }

                // Absicherung: Ein Kessel kann im Jahr unmöglich mehr als 8760 Volllaststunden haben!
                if (Betriebsstunden[i] > Bereitschaft) Betriebsstunden[i] = Bereitschaft;

                // 2. Freie Stunden (Stillstand) berechnen
                double freieStunden = Bereitschaft - Betriebsstunden[i];
                if (freieStunden < 0) freieStunden = 0;
                if (freieStunden > 8760) freieStunden = 8760;

                // Reiner theoretischer Erzeugungsverbrauch ohne Bereitschaftsverlust (in MWh)
                double reinerVerbrauchMWh = Kessel_Nutzkraft_Jahr / Kessel_Basis_Wirkungsgrad[i];

                // Verlust = (Verlustfaktor * Nennleistung (kW) * Stillstandsstunden) / 1000 => MWh
                double verlustMWh = (Betriebsbereitschaft_Verluste[i] * Kessel_Leistung_Spk[i] * freieStunden) / 1000;

                // ECHTER GESAMTVERBRAUCH DES KESSELS (Erzeugung + Standby-Verlust)
                double Kessel_Gesamtverbrauch_MWh = reinerVerbrauchMWh + verlustMWh;
                Kessel_Verbrauch_MWh_Spk[i] = Kessel_Gesamtverbrauch_MWh;

                // Den Gesamtverbrauch auf die globalen Brennstoffzähler der Software buchen
                if (Brennstoff_Art[i] >= 1 && Brennstoff_Art[i] <= 5) Gasverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if ((Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <= 9) || (Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22)) Oelverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 10) Koks_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 11) Kohle_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 12) Holzverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 17) TierischeFette_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 13)
                {
                    Stromverbrauch_Spk += Kessel_Nutzkraft_Jahr;
                    Strombedarf_stuendlich = AddVectors(Strombedarf_stuendlich, Kesselleistung_stuendlich);
                }
                else if (Brennstoff_Art[i] == 15) Pellets_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 16) Rapsoelverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;

                BruttoWaermeSpkErzeugung += Kessel_Gesamtverbrauch_MWh;

                // Emissionen basierend auf dem echten Gesamtverbrauch hochrechnen
                Em_CO2_SPK += Kessel_Gesamtverbrauch_MWh * CO2_SPK[i];
                Em_SO2_SPK += Kessel_Gesamtverbrauch_MWh * SO2_SPK[i];
                Em_NOX_SPK += Kessel_Gesamtverbrauch_MWh * NOX_SPK[i];
                Em_CO_SPK += Kessel_Gesamtverbrauch_MWh * CO_SPK[i];
                Em_Staub_SPK += Kessel_Gesamtverbrauch_MWh * Staub_SPK[i];
            }

            // Emissionen final herunterskalieren, in kg
            Em_CO2_SPK /= 1000;
            Em_SO2_SPK /= 1000;
            Em_NOX_SPK /= 1000;
            Em_CO_SPK /= 1000;
            Em_Staub_SPK /= 1000;
            if (Gasverbrauch_SPK < 0.1) Gasspitze_Spk = 0;

            // --- 5. JAHRESNUTZUNGSGRAD PRO KESSEL SAUBER ERMITTELN ---
            for (int i = 0; i < Anzahl; i++)
            {
                double erzeugteWaerme = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]; // Nutzwärme (MWh)
                double verbrauchterBrennstoff = Kessel_Verbrauch_MWh_Spk[i];       // Gesamtverbrauch (MWh)

                if (erzeugteWaerme > 0 && verbrauchterBrennstoff > 0)
                {
                    // Erzeugte Nutzwärme geteilt durch verbrauchten Brennstoff mal 100
                    double ngrad = (erzeugteWaerme / verbrauchterBrennstoff) * 100;

                    // Plausibilitätsgrenzen (z.B. Brennwerttechnik max 108% bezogen auf Hu)
                    if (ngrad > 110.0) ngrad = 108.0;
                    if (ngrad < 1.0) ngrad = 1.0;

                    Kessel_Jahresnutzungsgrad_Spk[i] = ngrad;
                }
                else
                {
                    Kessel_Jahresnutzungsgrad_Spk[i] = 0; // Kessel stand das ganze Jahr still
                }
            }

            return true;
        }

        private void Heizkessel_Simulation(float[] Waermebedarf, ref double GasSpitze, double[] s_waerme_gas, double[] s_waerme_oel,
                double Max_Waermebedarf, int Anzahl, double[] Leistung, double[] Wirk_Gas, int[] Brennstoff)
        {
            double KesselLeistung;
            double Gasleistung;
            double[] Gasspitze_Kessel = new double[5];
            double waerme;

            Max_Waermebedarf = 0;
            GasSpitze = 0;
            for (int i = 0; i < 5; i++) { Gasspitze_Kessel[i] = 0; }

            // Stündliche Lastverteilung (Einheit: kW)
            for (int Stunde = 0; Stunde < 8760; Stunde++)
            {
                waerme = Waermebedarf[Stunde];

                if (Max_Waermebedarf < waerme) Max_Waermebedarf = waerme;

                for (int Kessel = 0; Kessel < Anzahl; Kessel++)
                {
                    if (waerme > Leistung[Kessel])
                    {
                        KesselLeistung = Leistung[Kessel];
                        waerme -= Leistung[Kessel];
                    }
                    else
                    {
                        KesselLeistung = waerme;
                        waerme = 0;
                    }

                    // Stündlich erzeugte kW aufaddieren
                    if (Brennstoff[Kessel] >= 6 && Brennstoff[Kessel] <= 9 || Brennstoff[Kessel] >= 18 && Brennstoff[Kessel] <= 22)
                    {
                        s_waerme_oel[Kessel] += KesselLeistung;
                    }
                    else
                    {
                        s_waerme_gas[Kessel] += KesselLeistung;

                        // Gasspitzenberechnung unter Verwendung des normierten Wirkungsgrads
                        double wirk = Wirk_Gas[Kessel] <= 0 ? 0.90 : Wirk_Gas[Kessel];
                        Gasleistung = KesselLeistung / wirk;
                        if (Gasspitze_Kessel[Kessel] < Gasleistung) Gasspitze_Kessel[Kessel] = Gasleistung;
                    }

                    Kesselleistung_stuendlich[Stunde] += (float)KesselLeistung;
                    Restwaerme[Stunde] = (float)waerme;
                }
            }

            // Umrechnung der Jahressummen von kW (stündlich akkumuliert = kWh) in MWh (/ 1000)
            for (int i = 0; i < Anzahl; i++)
            {
                s_waerme_gas[i] /= 1000;
                s_waerme_oel[i] /= 1000;
                GasSpitze += Gasspitze_Kessel[i];
            }
        }

        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays müssen die gleiche Länge aufweisen.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++) { result[i] = array1[i] + array2[i]; }
            return result;
        }

        public void Init()
        {
            Maximale_Kesselleistung_Spk = 0;
            Stromverbrauch_Spk = 0;

            for (int j = 0; j < MAX_SPK; j++)
            {
                s_waerme_Gas_Spk[j] = 0;
                s_waerme_Oel_Spk[j] = 0;
                Kessel_Wirk_Gas_Spk[j] = 0;
                Kessel_Wirk_Oel_Spk[j] = 0;
                Betriebsbereitschaft_Verluste[j] = 0;
                Kessel_Name[j] = "";
                Brennstoff_Betrieb_Spk[j] = 0;
                Kessel_Leistung_Spk[j] = 0;
                Betriebsstunden[j] = 0;

                Kessel_Verbrauch_MWh_Spk[j] = 0;
                Kessel_Basis_Wirkungsgrad[j] = 0;
                Kessel_Jahresnutzungsgrad_Spk[j] = 0;

                CO2_SPK[j] = 0;
                CO_SPK[j] = 0;
                SO2_SPK[j] = 0;
                NOX_SPK[j] = 0;
                Staub_SPK[j] = 0;
            }

            BruttoWaermeSpkErzeugung = 0;
            S_Waerme_spk = 0;
            Gasverbrauch_SPK = 0;
            Oelverbrauch_SPK = 0;
            Rapsoelverbrauch_SPK = 0;
            Holzverbrauch_SPK = 0;
            Sonstigverbrauch_SPK = 0;
            Stromverbrauch_Spk = 0;
            Kohle_SPK = 0;
            Koks_SPK = 0;
            Pellets_SPK = 0;
            TierischeFette_SPK = 0;

            Em_CO2_SPK = 0;
            Em_CO_SPK = 0;
            Em_SO2_SPK = 0;
            Em_NOX_SPK = 0;
            Em_Staub_SPK = 0;

            Gasspitze_Spk = 0;

            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Strombedarf_stuendlich, 0, Strombedarf_stuendlich.Length);
            Array.Clear(Kesselleistung_stuendlich, 0, Kesselleistung_stuendlich.Length);
        }
    }
}