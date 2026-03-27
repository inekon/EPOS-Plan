using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    public class SimulationPV
    {
        public List<int> photovoltaik_list = new List<int>();
        public int m_ID_Projekt = 0;
        public float[] Strombedarf = new float[8760*4];
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] Stromproduktion = new float[8760];
        public float[] Reststrom = new float[8760];
        public float[] Ueberschuss = new float[8760];
        public float[] Stromproduktion_viertelstunde = new float[8760];
        public float[] Reststrom_viertelstunde = new float[8760];
        public float[] Ueberschuss_viertelstunde = new float[8760];
        public double Stromproduktion_Max = 0;

        public float Stromproduktion_gesamt = 0;


        public void Init()
        {
            Array.Clear(Stromproduktion, 0, Stromproduktion.Length);
        }

        public float[] Berechnung(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            int nID_Klimaregion = 0;
            double Lon = 0;
            double Lat = 0;
            double[] strahlung = new double[8760];
            double[] sonnen_azimut = new double[8760];

            double m_ID_Projekt = ID_Projekt;

            Strombedarf_stuendlich = Viertelstunden_zu_stunden(Strombedarf);
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

            // Schleife PV-Module zum Projekt
            ctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);

            for (int n = 0; n < ctrl.rows; n++)
            {
                int nId = ctrl.items[n].ID_PV;
                int nAzimuth = ctrl.items[n].m_Azimut;
                int nNeigung = ctrl.items[n].m_Neigung;
                long nAnzahl = (long)ctrl.items[n].PV_Leistung; //.Kollektormodulanzahl;

                // Moduldaten auslesen (Wirkungsgrad, Temp-Koeffizient)
                PhotovoltaikCtrl ctrlsol = new PhotovoltaikCtrl();
                ctrlsol.ReadSingle(nId);
                double nFlaeche = ctrlsol.m_Breite * ctrlsol.m_Laenge;
           
                // PV-spezifische Werte (Diese sollten idealerweise in deiner DB stehen)
                double nennWirkungsgrad = ctrlsol.m_Wirkungsgrad / 100; // Oft wird h0 für den Grundwirkungsgrad missbraucht

                double tempKoeffizient = ctrlsol.m_Temp_Coeff_Pmax / 100; // - 0.004; Standardwert -0.4%/K, falls nicht in DB vorhanden
                double wechselrichterWirkungsgrad = 0.95; // Verluste ca. 5%

                // Solardaten für die Region holen
                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadAll("select * from Tab_Solar where ID_Klimaregion=" + nID_Klimaregion + " order by ID");

                // In SimulationPV.cs
                for (int i = 0; i < ctrldat.rows; i++)
                {
                    // Diese Methode macht INTERN schon alles mit Azimut und Neigung!
                    double effektiveStrahlung = SolarCalculator.CalculateHourly(Lon, Lat, nNeigung, nAzimuth,
                                                ctrldat.items[i].Globalstrahlung,
                                                ctrldat.items[i].Direktstrahlung,
                                                ctrldat.items[i].Diffusstrahlung,
                                                ctrldat.items[i].Außen_Temp, i / 24, i % 24);

                    // Da CalculateHourly bereits die winkelkorrigierte Strahlung liefert,
                    // wird in BerechnePV cosTheta auf 1.0 gesetzt.
                    var ergebnis = BerechnePV(
                        Strombedarf_stuendlich[i],
                        effektiveStrahlung, // Das ist schon der fertige W/m² Wert auf dem Modul
                        nFlaeche * nAnzahl,
                        nennWirkungsgrad,
                        tempKoeffizient,
                        ctrldat.items[i].Außen_Temp,
                        1.0 // cosTheta hier auf 1.0 setzen, da in effektiveStrahlung schon drin!
                    );
 
                    // Werte zuweisen und Wechselrichterverluste einrechnen
                    Stromproduktion[i] = (float)(ergebnis.produktion * wechselrichterWirkungsgrad);
                    Reststrom[i] = (float)ergebnis.restbedarf;
                    Ueberschuss[i] = (float)ergebnis.ueberschuss;
                    if (ergebnis.potenzielleErzeugung > Stromproduktion_Max) Stromproduktion_Max = ergebnis.potenzielleErzeugung; 

                }
            }
            Stromproduktion_gesamt = Stromproduktion.Sum();

            Stromproduktion_viertelstunde = Stundenwerte_zu_viertelstunden(Stromproduktion);
            Reststrom_viertelstunde = Stundenwerte_zu_viertelstunden(Reststrom);
            Ueberschuss_viertelstunde = Stundenwerte_zu_viertelstunden(Ueberschuss);
     
            return Stromproduktion_viertelstunde;
        }

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {
            float[] viertelstundenwerte = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                viertelstundenwerte[i * 4] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 1] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 2] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 3] = stundenwerte[i];
            }
            return viertelstundenwerte;
        }
        public float[] Viertelstunden_zu_stunden(float[] viertelstundenwerte)
        {
            // Das Ergebnis-Array ist genau 4-mal kleiner
            float[] stundenwerte = new float[viertelstundenwerte.Length / 4];

            for (int i = 0; i < stundenwerte.Length; i++)
            {
                // Wir nehmen die 4 Viertelstunden-Blöcke und bilden den Mittelwert
                float summe = viertelstundenwerte[i * 4] +
                             viertelstundenwerte[i * 4 + 1] +
                             viertelstundenwerte[i * 4 + 2] +
                             viertelstundenwerte[i * 4 + 3];

                stundenwerte[i] = summe / 4.0f;
            }

            return stundenwerte;
        }

        public (double produktion, double restbedarf, double ueberschuss, double potenzielleErzeugung) BerechnePV(
                double strombedarf, double strahlung, double flaeche,
                double nennWirkungsgrad, double tempKoeffizient, double tAmb, double cosTheta)
        {
            // 1. Zelltemperatur schätzen (einfaches Modell: Ta + Strahlungseinfluss)
            // PV-Module sind ca. 20-30 Grad wärmer als die Luft bei voller Sonne
            double tCell = tAmb + (strahlung / 800.0) * 25.0;

            // 2. Wirkungsgradkorrektur durch Temperatur (Standardtestbedingung STC ist 25°C)
            // tempKoeffizient ist meist ca. -0.004 (also -0.4% pro Grad Erwärmung)
            double aktuellerWirkungsgrad = nennWirkungsgrad * (1 + tempKoeffizient * (tCell - 25.0));

            // 3. Potenzielle Erzeugung (kW)
            // strahlung * cosTheta ist die effektiv auftreffende Energie
            double potenzielleErzeugung = (strahlung * cosTheta * flaeche * aktuellerWirkungsgrad) / 1000.0;

            // 4. Logik für Produktion/Überschuss 
            double produktion = Math.Min(potenzielleErzeugung, strombedarf);
            double ueberschuss = Math.Max(0, potenzielleErzeugung - strombedarf);
            double restbedarf = Math.Max(0, strombedarf - produktion);

            return (produktion, restbedarf, ueberschuss, potenzielleErzeugung);
        }
    }
}
