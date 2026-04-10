using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using WindowsFormsApplication1.Classes.Simulation;

namespace WindowsFormsApplication1
{
    public class SimulationSPK
    {
        public const int MAX_SPK = 6;

        public List<string> spk_list = new List<string>();
        public int m_ID_Projekt = 0;
        public double Max_Waermebedarf;
        public float[] Waermebedarf = new float[8760];
        public float[] Restwaerme = new float[8760];
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] Kesselleistung_stuendlich = new float[8760];

        public int Vorgabe_Betriebsbereitschaft = 6000;
        
        public double Waermebedarf_gesamt = 0;
        public double Strombedarf_gesamt = 0;
        public double Maximale_Kesselleistung_Spk = 0;
        public double Stromverbrauch_Spk = 0;
        public double BruttoWaermeSpkErzeugung = 0;
        public double S_Waerme_spk = 0;

        double Verbrauch = 0;
        public double Gasverbrauch_SPK = 0;
        public double Oelverbrauch_SPK = 0;
        public double Rapsoelverbrauch_SPK = 0;
        public double Holzverbrauch_SPK = 0;
        public double Sonstigverbrauch_SPK = 0;
        public double Koks_SPK = 0;
        public double Kohle_SPK = 0;    
        public double Pellets_SPK = 0;
        public double TierischeFette_SPK = 0;


        public double Em_CO2_SPK = 0;
        public double Em_CO_SPK = 0;
        public double Em_SO2_SPK = 0;
        public double Em_NOX_SPK = 0;
        public double Em_Staub_SPK = 0;

        public double Gasspitze_Spk = 0;

        double[] Kessel_waerme_gas_Spk = new double[MAX_SPK];
        public double[] s_waerme_Oel_Spk = new double[MAX_SPK];
        public double[] s_waerme_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Oel_Spk = new double[MAX_SPK];
        double[] Betriebsbereitschaft_Verluste = new double[MAX_SPK];
        double[] Betriebsstunden = new double[MAX_SPK];   
        string [] Kessel_Name = new string[MAX_SPK];
        int [] Brennstoff_Betrieb_Spk = new int[MAX_SPK];
        int [] Brennstoff_Art = new int[MAX_SPK];
        double [] Kessel_Leistung_Spk = new double[MAX_SPK];
        int Bereitschaft = 6000;

        public bool Berechnung(int ID_Projekt)
        {
            int Anzahl = 0;
            m_ID_Projekt = ID_Projekt;

            Init();

            // Wärmebedarf gesamt berechnen, in MWh
            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000; // in MWh  

            BrennstoffCtrl brennstoffctrl = new BrennstoffCtrl();
            Anzahl = spk_list.Count;
            // wenn keine Kessel definiert simd ist die Restwärme == Wärmebedarf
            if (Anzahl == 0) { Restwaerme = Waermebedarf; return true; }

            // Voreinstellungen
            for (int i = 0; i < Anzahl; i++)
            {
                brennstoffctrl.ReadAll("Name='" + spk_list[i] + "'");
                Kessel_Name[i] = brennstoffctrl.items[0].Name;
                Kessel_Leistung_Spk[i] = brennstoffctrl.items[0].Ptherm;
                Kessel_Wirk_Gas_Spk[i] = brennstoffctrl.items[0].Wirkungsgrad_Gas;
                Kessel_Wirk_Oel_Spk[i] = brennstoffctrl.items[0].Wirkungsgrad_Oel;
                Brennstoff_Betrieb_Spk[i] = brennstoffctrl.items[0].Brennstoff;
                Brennstoff_Art[i] = Brennstoff_Betrieb_Spk[i];
                if (Brennstoff_Betrieb_Spk[i] > 1) Brennstoff_Betrieb_Spk[i] = 1;
                Betriebsbereitschaft_Verluste[i] = brennstoffctrl.items[0].Betriebsbereitschaftverlust;
                Maximale_Kesselleistung_Spk = Maximale_Kesselleistung_Spk + Kessel_Leistung_Spk[i];
            }
            
            // Wärmeproduktion berechnen
            Heizkessel_Simulation(Waermebedarf, ref Gasspitze_Spk, s_waerme_Gas_Spk, s_waerme_Oel_Spk,
                Max_Waermebedarf, Anzahl, Kessel_Leistung_Spk, Kessel_Wirk_Gas_Spk, Brennstoff_Betrieb_Spk);

            // Wirkungsgrad, Betriebsbereitschaft und Verbrauch nach Brennstoffart zuordnen
            for (int i = 0; i < Anzahl; i++)
            {
                Bereitschaft = Vorgabe_Betriebsbereitschaft;
                S_Waerme_spk = S_Waerme_spk + s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i];
                
                if (s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i] > 0.0001)
                {
                    Betriebsstunden[i] = (s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]) * 1000 / Kessel_Leistung_Spk[i];
                }
                
                if (Betriebsstunden[i] < 0.0001) Betriebsstunden[i] = 0.0001;
                
                if (Kessel_Wirk_Gas_Spk[i] > 0)
                {
                    if (i < Anzahl - 1) Bereitschaft = 8760;
                    
                    if (Bereitschaft / Betriebsstunden[i] * Betriebsbereitschaft_Verluste[i] < 1)
                    {
                        if (Kessel_Wirk_Gas_Spk[i] < 1)
                        {
                            Kessel_Wirk_Gas_Spk[i] = (1 - Bereitschaft / Betriebsstunden[i] * Betriebsbereitschaft_Verluste[i]) / (1 - Betriebsbereitschaft_Verluste[i]) * Kessel_Wirk_Gas_Spk[i];
                        }
                        else
                        {
                            Kessel_Wirk_Gas_Spk[i] = Kessel_Wirk_Gas_Spk[i] - 0.02; //Brennwertkessel
                        }

                        if (Kessel_Wirk_Gas_Spk[i] < 0.15) Kessel_Wirk_Gas_Spk[i] = 0.15;
                        Verbrauch = (s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]) / Kessel_Wirk_Gas_Spk[i];
                    }
                    else
                    {
                        Kessel_Wirk_Gas_Spk[i] = 0;
                        Verbrauch = s_waerme_Gas_Spk[i] + Betriebsbereitschaft_Verluste[i] * Kessel_Leistung_Spk[i] * (8760 - Betriebsstunden[i]) / 1000;
                    }

                    // Gas
                    if (Brennstoff_Art[i] >= 1 && Brennstoff_Art[i] <= 5)
                        Gasverbrauch_SPK = Gasverbrauch_SPK + Verbrauch;
                    //Öl
                    else if ((Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <=9) || (Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22))
                        Oelverbrauch_SPK = Oelverbrauch_SPK + Verbrauch;
                    // Koks              
                    else if (Brennstoff_Art[i] == 10)
                        Koks_SPK = Koks_SPK + Verbrauch;
                    // Kohle
                    else if (Brennstoff_Art[i] == 11)
                        Kohle_SPK = Kohle_SPK + Verbrauch;
                    // Holz
                    else if (Brennstoff_Art[i] == 12)
                        Holzverbrauch_SPK = Holzverbrauch_SPK + Verbrauch;
                    // Tierische Fette
                    else if (Brennstoff_Art[i] == 17)
                        TierischeFette_SPK = TierischeFette_SPK + Verbrauch;
                    // Strom
                    else if (Brennstoff_Art[i] == 13)
                    {
                        Stromverbrauch_Spk = Stromverbrauch_Spk + S_Waerme_spk;
                        Strombedarf_stuendlich = AddVectors(Strombedarf_stuendlich, Kesselleistung_stuendlich);
                    }
                    // Pellets
                    else if (Brennstoff_Art[i] == 15)
                        Pellets_SPK = Pellets_SPK + Verbrauch;
                    // Rapsöl
                    else if (Brennstoff_Art[i] == 16)
                        Rapsoelverbrauch_SPK = Rapsoelverbrauch_SPK + Verbrauch;
                    // Sonstige
                    else if (Brennstoff_Art[i] == 5)
                        Sonstigverbrauch_SPK = Sonstigverbrauch_SPK + Verbrauch;
                    
                    BruttoWaermeSpkErzeugung = BruttoWaermeSpkErzeugung + Verbrauch;
                    Em_CO2_SPK = Em_CO2_SPK + Verbrauch * Em_CO2_SPK;
                    Em_SO2_SPK = Em_SO2_SPK + Verbrauch * Em_SO2_SPK;
                    Em_NOX_SPK = Em_NOX_SPK + Verbrauch * Em_NOX_SPK;
                    Em_CO_SPK = Em_CO_SPK + Verbrauch * Em_CO_SPK;
                    Em_Staub_SPK = Em_Staub_SPK + Verbrauch * Em_Staub_SPK;
                }
                else if (Kessel_Wirk_Oel_Spk[i] > 0)
                {
                    if (i < Anzahl - 1) Bereitschaft = 8760;
                    
                    if (Bereitschaft / Betriebsstunden[i] * Betriebsbereitschaft_Verluste[i] < 1)
                    {
                        if (Kessel_Wirk_Oel_Spk[i] < 1)
                        {
                            Kessel_Wirk_Oel_Spk[i] = (1 - Bereitschaft / Betriebsstunden[i] * Betriebsbereitschaft_Verluste[i]) / (1 - Betriebsbereitschaft_Verluste[i]) * Kessel_Wirk_Oel_Spk[i];
                        }
                        else
                        {
                            Kessel_Wirk_Oel_Spk[i] = Kessel_Wirk_Oel_Spk[i] - 0.02;
                        }
                        Verbrauch = s_waerme_Oel_Spk[i] / Kessel_Wirk_Oel_Spk[i];
                    }
                    else
                    {
                        Verbrauch = s_waerme_Oel_Spk[i] / Kessel_Wirk_Oel_Spk[i] + Betriebsbereitschaft_Verluste[i] * Kessel_Leistung_Spk[i] * (8760 - Betriebsstunden[i]) / 1000;
                        Kessel_Wirk_Oel_Spk[i] = 0;
                    }
                    
                    Oelverbrauch_SPK = Oelverbrauch_SPK + Verbrauch;
                    BruttoWaermeSpkErzeugung = BruttoWaermeSpkErzeugung + Verbrauch;
                    Em_CO2_SPK = Em_CO2_SPK + Verbrauch * Em_CO2_SPK;
                    Em_SO2_SPK = Em_SO2_SPK + Verbrauch * Em_SO2_SPK;
                    Em_NOX_SPK = Em_NOX_SPK + Verbrauch * Em_NOX_SPK;
                    Em_CO_SPK = Em_CO_SPK + Verbrauch * Em_CO_SPK;
                    Em_Staub_SPK = Em_Staub_SPK + Verbrauch * Em_Staub_SPK;
                }

                // Emissionen bzgl. MWh
                Em_CO2_SPK = Em_CO2_SPK / 1000;
                Em_SO2_SPK = Em_SO2_SPK / 1000;
                Em_NOX_SPK = Em_NOX_SPK / 1000;
                Em_CO_SPK = Em_CO_SPK / 1000;
                Em_Staub_SPK = Em_Staub_SPK / 1000;

                if (Gasverbrauch_SPK < 0.1) Gasspitze_Spk = 0;
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

            for (int i = 0; i < 5; i++)
            {
                Gasspitze_Kessel[i] = 0;
            }

            for (int Stunde = 0; Stunde < 8760; Stunde++)
            {
                waerme = Waermebedarf[Stunde];
                Gasleistung = 0;

                if (Max_Waermebedarf < waerme) Max_Waermebedarf = waerme;

                for (int Kessel = 0; Kessel < Anzahl; Kessel++)
                {
                    if (waerme > Leistung[Kessel])
                    {
                        KesselLeistung = Leistung[Kessel];
                        Kesselleistung_stuendlich[Stunde] = (float)KesselLeistung;
                        waerme -= Leistung[Kessel];
                    }
                    else
                    {
                        KesselLeistung = waerme;
                        Kesselleistung_stuendlich[Stunde] = (float)KesselLeistung;
                        waerme = 0;
                    }

                    if (Brennstoff[Kessel] == 0)
                    {
                        s_waerme_oel[Kessel] = s_waerme_oel[Kessel] + KesselLeistung;
                    }
                    else
                    {
                        s_waerme_gas[Kessel] = s_waerme_gas[Kessel] + KesselLeistung;
                        Gasleistung = KesselLeistung / Wirk_Gas[Kessel];
                        if (Gasspitze_Kessel[Kessel] < Gasleistung) Gasspitze_Kessel[Kessel] = Gasleistung;
                    }

                    Restwaerme[Stunde] = (float)waerme;
                }
            }
        
            for(int i = 0; i < Anzahl; i++)
            {
                s_waerme_gas[i] = s_waerme_gas[i] / 1000;
                s_waerme_oel[i] = s_waerme_oel[i] / 1000;
                GasSpitze += Gasspitze_Kessel[i];
            }
        }

        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] + array2[i];
            }
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

            Verbrauch = 0;
            Gasspitze_Spk = 0;

            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Strombedarf_stuendlich, 0, Strombedarf_stuendlich.Length);
            Array.Clear(Kesselleistung_stuendlich, 0, Kesselleistung_stuendlich.Length);
        }

    }
}
