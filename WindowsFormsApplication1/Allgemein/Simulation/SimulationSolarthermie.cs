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
        public double[] Ueberschuss = new double[8760];
        public double[] tempRestwaerme = new double[8760];
        public double[] tempWaermeproduktion = new double[8760];
        public double[] tempUeberschuss = new double[8760];
        public double[] strahlung = new double[8760];
        public double[] sonnen_azimut = new double[8760]; 
        public double[] wirk = new double[8760];

        public double Lon = 0;
        public double Lat = 0;
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

            // Arrays und Vaiablen initialisieren
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

                // Schleife Solardaten auslesen und Orts- und Tageszeit und neigungsabhängige Strahlungsleistung bestimmen
                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadAll("select * from Tab_Solar where ID_Klimaregion=" + nID_Klimaregion + " order by ID");

                for (int i = 0; i < ctrldat.rows; i++)
                {
                    strahlung[i] = SolarCalculator.CalculateHourly(Lon, Lat, nNeigung, nAzimuth, ctrldat.items[i].Globalstrahlung,
                    ctrldat.items[i].Direktstrahlung, ctrldat.items[i].Diffusstrahlung, ctrldat.items[i].Außen_Temp, i / 24, i % 24);
                    sonnen_azimut[i] = SolarCalculator.sonnen_azimut;
                }

                double k1 = ctrlsol.m_k1;
                double k2 = ctrlsol.m_k2;
                double Leitungsverluste = 0.92;
                double kdir50 = ctrlsol.m_Kdir;
                // kann bzgl. Modulneigung genauer betrachtet werden, siehe KI
                
                double tStorage = 50;
                // wenn man das detailierter will muss mam die Temperatur zum und von einem Puffer im Zusammenhang mit der
                // Umgebungstemperatur und ggf. mit dem Kollektortyp kalkulieren. Siehe KI
                
                double ta = 0;
                double Neigung = nNeigung;
                double h0 = ctrlsol.m_h0;

                for (int i = 0; i < ctrldat.rows; i++)
                {
                    ta = ctrldat.items[i].Außen_Temp;
                    // cosTheta ist der Winkel zwischen senkrechte (90 Grad) und Sonnenwinkel über Horizont
                    double cosTheta = GetProjectionFactor(ctrldat.items[i].Sonnenwinkel, sonnen_azimut[i], nNeigung, nAzimuth);
                    cosTheta = cosTheta * 180 / Math.PI; // Bogenmaß
                    (Waermeproduktion[i], Restwaerme[i], Ueberschuss[i]) = BerechneSolarthermie(Waermebedarf[i], strahlung[i], nFlaeche * nAnzahl, h0, k1, k2, kdir50, tStorage, ta, Neigung, cosTheta, Leitungsverluste);
                }
            }

            // Gesamtproduktion berechnen   
            Array.ForEach(Waermeproduktion, value => Waermeproduktion_gesamt += value);
            Array.ForEach(Ueberschuss, value => ueberschuss += value);
            
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

        public (double produktion, double restbedarf, double ueberschuss) BerechneSolarthermie(double waermebedarf, double strahlung, double flaeche, double h0, double k1, double k2, double kdir50, double tStorage, double ta, double Neigung, double cosTheta, double Leitungsverluste)
        {
            double produktion;
            double restbedarf;
            double ueberschuss;

            // Berechnung der potenziellen Produktion in Watt (bzw. Wh pro Stunde)
            double wirkungsgrad = CalculateThermalPower(strahlung, ta, tStorage, cosTheta, h0, k1, k2, kdir50);
            double potenzielleErzeugung = flaeche * wirkungsgrad; 
   
            potenzielleErzeugung /= 1000; // kW
 
            // Wir können nicht mehr produzieren, als aktuell benötigt wird 
            // (vorausgesetzt es gibt keinen Speicher in dieser Basissimulation)
            produktion = Math.Min(potenzielleErzeugung, waermebedarf);
            ueberschuss = Math.Max(0, potenzielleErzeugung - waermebedarf);

            // Der verbleibende Bedarf
            restbedarf = Math.Max(0, waermebedarf - produktion);
            return (produktion, restbedarf, ueberschuss);
        }

        // Add this helper method to the SolarCalculator class or as a private static method in the same file
        public double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public double CalculateThermalPower(double gTilted, double tAmb, double tStorage,
                                    double cosTheta, double h0, double a1, double a2, double kDir50)
        {
            if (gTilted <= 0) return 0;

            // 1. Berechnung des IAM (Incident Angle Modifier) aus Kdir(50)
            // b0 ist der physikalische Parameter für die Glascharakteristik
            double thetaRad = Math.Acos(Clamp(cosTheta, 0, 1));
            double b0 = (1.0 - kDir50) / (1.0 / Math.Cos(50.0 * (Math.PI / 180.0)) - 1.0);

            // IAM Faktor: Korrekturwert zwischen 0.0 und 1.0
            double iam = 1.0 - b0 * (1.0 / Math.Cos(thetaRad) - 1.0);
            iam = Clamp(iam, 0.0, 1.0);

            // 2. Wirkungsgrad-Formel (angepasst um den Winkel-Korrekturfaktor IAM)
            // h0 wird durch IAM reduziert, da weniger Licht den Absorber erreicht.
            double h0_effektiv = h0 * iam;
            double dT = tStorage - tAmb;

            // Deine Formel: h0_eff - (a1 * dT / G) - (a2 * dT² / G)
            double aktuellerWirkungsgrad = h0_effektiv - (a1 * dT / gTilted) - (a2 * dT * dT / gTilted);
    
            // 3. Umrechnung in Leistung (Watt pro m²)
            double leistungProQuadratmeter = gTilted * aktuellerWirkungsgrad;

            // 4. Physikalische Plausibilität: Wenn Verluste > Gewinn, dann keine Leistung
            if (leistungProQuadratmeter < 0) leistungProQuadratmeter = 0;

            return leistungProQuadratmeter;
        }

        public double GetProjectionFactor(double sunAlphaDeg, double sunAzimuthDeg,
                                         double moduleTiltDeg, double moduleAzimuthDeg)
        {
            double a = sunAlphaDeg * Math.PI / 180.0;
            double b = moduleTiltDeg * Math.PI / 180.0;
            double gs = sunAzimuthDeg * Math.PI / 180.0;
            double gm = moduleAzimuthDeg * Math.PI / 180.0;
            gs = sunAzimuthDeg;

            // Der Kosinus des Einfallswinkels (Theta)
            double cosTheta = Math.Sin(a) * Math.Cos(b) +
                              Math.Cos(a) * Math.Sin(b) * Math.Cos(gs - gm);

            // Wenn cosTheta < 0, steht die Sonne hinter dem Modul
            return Math.Max(0, cosTheta);
        }
    }

}
