using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

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
        public double Biogasverbrauch_SPK = 0;
        public double Rapsoelverbrauch_SPK = 0;
        public double Holzverbrauch_SPK = 0;
        public double Sonstigverbrauch_SPK = 0;
        public double Fluessiggasverbrauch_SPK = 0;

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

            // Wärmebedarf gesamt
            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000; // in MWh  

            BrennstoffCtrl brennstoffctrl = new BrennstoffCtrl();
            Anzahl = spk_list.Count;
            
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
            Gasspitze_Spk = 0;
            
            Heizkessel_Simulation_Ref(Waermebedarf, ref Gasspitze_Spk, s_waerme_Gas_Spk, s_waerme_Oel_Spk,
                Max_Waermebedarf, Anzahl, Kessel_Leistung_Spk, Kessel_Wirk_Gas_Spk, Brennstoff_Betrieb_Spk);

            BruttoWaermeSpkErzeugung = 0;
            S_Waerme_spk = 0;
            Gasverbrauch_SPK = 0;
            Oelverbrauch_SPK = 0;
            Biogasverbrauch_SPK = 0;
            Rapsoelverbrauch_SPK = 0;
            Holzverbrauch_SPK = 0;
            Sonstigverbrauch_SPK = 0;
            Fluessiggasverbrauch_SPK = 0;
            Stromverbrauch_Spk = 0;
            Em_CO2_SPK = 0;
            Em_CO_SPK = 0;
            Em_SO2_SPK = 0;
            Em_NOX_SPK = 0;
            Em_Staub_SPK = 0;
            Verbrauch = 0;

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

                    if (Brennstoff_Art[i] == 1)
                        Gasverbrauch_SPK = Gasverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 0)
                        Oelverbrauch_SPK = Oelverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 11)
                        Biogasverbrauch_SPK = Biogasverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 3)
                        Rapsoelverbrauch_SPK = Rapsoelverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 4)
                        Holzverbrauch_SPK = Holzverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 5)
                        Sonstigverbrauch_SPK = Sonstigverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 8)
                        Fluessiggasverbrauch_SPK = Fluessiggasverbrauch_SPK + Verbrauch;
                    else if (Brennstoff_Art[i] == 15)
                        Stromverbrauch_Spk = Stromverbrauch_Spk + Verbrauch;

                    BruttoWaermeSpkErzeugung = BruttoWaermeSpkErzeugung + Verbrauch;
                    Em_CO2_SPK = Em_CO2_SPK + Verbrauch * Em_CO2_SPK;
                    Em_SO2_SPK = Em_SO2_SPK + Verbrauch * Em_SO2_SPK;
                    Em_NOX_SPK = Em_NOX_SPK + Verbrauch * Em_NOX_SPK;
                    Em_CO_SPK = Em_CO_SPK + Verbrauch * Em_CO_SPK;
                    Em_Staub_SPK = Em_Staub_SPK + Verbrauch * Em_Staub_SPK;
                }
                else if (Kessel_Wirk_Oel_Spk[i] > 0)
                {
                    // Bereitschaft = 6000;
                    // aus Excel - nur Heizkessel -> if(projekt.Sheets("SpKessel_Daten").Cells(30, 2) = "Ja" Then Bereitschaft = 8760#
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

                Em_CO2_SPK = Em_CO2_SPK / 1000;
                Em_SO2_SPK = Em_SO2_SPK / 1000;
                Em_NOX_SPK = Em_NOX_SPK / 1000;
                Em_CO_SPK = Em_CO_SPK / 1000;
                Em_Staub_SPK = Em_Staub_SPK / 1000;

                if (Gasverbrauch_SPK < 0.1) Gasspitze_Spk = 0;
            }

            return true;    
        }

        private void Heizkessel_Simulation_Ref(float[] Waermebedarf, ref double GasSpitze, double[] s_waerme_gas, double[] s_waerme_oel,
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
                        waerme -= Leistung[Kessel];
                    }
                    else
                    {
                        KesselLeistung = waerme;
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
    }
}
