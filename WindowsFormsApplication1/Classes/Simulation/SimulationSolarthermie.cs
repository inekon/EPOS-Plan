using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace WindowsFormsApplication1
{
    public class SimulationSolarthermie
    {
        public List<int> solarthermie_list = new List<int>();
        public int m_ID_Projekt = 0;
        private long nID_Klimaregion;
        
        public double Waermeproduktion_gesamt = 0;
        public double Waermebedarf_gesamt = 0;
        public double Max_Waermebedarf;
        
        public double[] Waermebedarf = new double[8760];
        public double[] Restwaerme = new double[8760];
        public double[] Waermeproduktion = new double[8760];
        public double[] tempRestwaerme = new double[8760];
        public double[] tempWaermeproduktion = new double[8760];
        public double[] tempUeberschuss = new double[8760];
        public double[] strahlung = new double[8760];

        public double Lon = 0;
        public double Lat = 0;
        public double wirkungsgrad = 0.8;
        public double ueberschuss = 0;

        public bool Berechnung(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            
            m_ID_Projekt = ID_Projekt;
            
            // ID_Klimaregion aus Projekt ermitteln
            rs.Open("select * from Tab_Projekt where ID=" + m_ID_Projekt);
            if (rs.Next())
            {
                nID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close(); 
            
            // geo Koordinaten auslesen
            KlimaregionCtrl ctrlklima = new KlimaregionCtrl();
            ctrlklima.ReadSingle("select * from Tab_Klimaregion where ID_Klimaregion=" + nID_Klimaregion);
            if (ctrlklima.rows > 0)
            {
                Lon = ctrlklima.Longitude;
                Lat = ctrlklima.Latitude;
            }

            Init();

            // Wärmebedarf gesamt berechnen
            Waermebedarf_gesamt = 0;
            Max_Waermebedarf = Maximaler_Waermebedarf(Waermebedarf); // kW
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);

            // Schleife Sollarkollektoren zum Projekt
            ctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
            
            for (int n = 0; n < ctrl.rows ; n++)
            {
                int nId = ctrl.items[n].ID_Solar;
                int nAzimuth = ctrl.items[n].m_Azimut;
                int nNeigung = ctrl.items[n].m_Neigung;
                long nAnzahl = ctrl.items[n].Kollektormodulanzahl;
                
                // Aperturfläche auslesen
                SolarkollektorenCtrl ctrlsol = new SolarkollektorenCtrl();
                ctrlsol.ReadSingle(nId);
                double nFlaeche = ctrlsol.m_Aperturfläche;

                // Schleife Solardaten auslesen und Orts- und Tageszeit und Neigungsabhängige Strahlungsleistung bestimmen
                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadAll("select * from Tab_Solar where ID_Klimaregion=" + nID_Klimaregion + " order by ID");
                
                for (int i = 0; i < ctrldat.rows; i++)
                {
                   strahlung[i] = SolarCalculator.CalculateHourly(Lon, Lat, nNeigung, nAzimuth, ctrldat.items[i].Globalstrahlung,
                   ctrldat.items[i].Direktstrahlung, ctrldat.items[i].Diffusstrahlung, ctrldat.items[i].Außen_Temp, i / 24, i % 24);
                }

                // Simulation Solarthermie durchführen
                (tempWaermeproduktion, tempRestwaerme, tempUeberschuss) = BerechneSolarthermie(Waermebedarf, strahlung, nFlaeche * nAnzahl, wirkungsgrad);
                for (int j = 0; j < tempWaermeproduktion.Length; j++)
                {
                    Waermeproduktion[j] += tempWaermeproduktion[j];
                    Restwaerme[j] += tempRestwaerme[j];
                    ueberschuss += tempUeberschuss[j];
                }
            }

            // Gesamtproduktion berechnen   
            Array.ForEach(Waermeproduktion, value => Waermeproduktion_gesamt += value);

            return true;
        }

        public double Maximaler_Waermebedarf(double[] waermebedarf)
        {
            return waermebedarf.Max();
        }

        public void Init()
        {
            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Waermeproduktion, 0, Waermeproduktion.Length);
            Max_Waermebedarf = 0;
            Waermeproduktion_gesamt = 0;
        }

        public (double[] produktion, double[] restbedarf, double[] ueberschuss) BerechneSolarthermie(
                double[] waermebedarf, double[] strahlung, double flaeche, double wirkungsgrad)
        {
            int stunden = 8760;
            double[] produktion = new double[stunden];
            double[] restbedarf = new double[stunden];
            double[] ueberschuss = new double[stunden];

            for (int i = 0; i < stunden; i++)
            {
                // Berechnung der potenziellen Produktion in Watt (bzw. Wh pro Stunde)
                 
                double potenzielleErzeugung = flaeche * wirkungsgrad * strahlung[i] / 1000; //kW

                // Wir können nicht mehr produzieren, als aktuell benötigt wird 
                // (vorausgesetzt es gibt keinen Speicher in dieser Basissimulation)
                produktion[i] = Math.Min(potenzielleErzeugung, waermebedarf[i]);
                ueberschuss[i] = Math.Max(0, potenzielleErzeugung - waermebedarf[i]);

                // Der verbleibende Bedarf
                restbedarf[i] = Math.Max(0, waermebedarf[i] - produktion[i]);
            }

            return (produktion, restbedarf, ueberschuss);
        }
    }

}
