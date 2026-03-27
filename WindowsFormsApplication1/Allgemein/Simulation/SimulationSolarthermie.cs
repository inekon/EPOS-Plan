using System;
using System.Collections.Generic;
using System.Linq;

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

        public double Lon = 0;
        public double Lat = 0;
        public double ueberschuss_summe = 0;
        public double Waermeproduktion_max = 0;

        public bool Berechnung(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();
            WErzeugerCtrl ctrl = new WErzeugerCtrl();

            m_ID_Projekt = ID_Projekt;

            // 1. ID_Klimaregion ermitteln
            rs.Open("select * from Tab_Projekt where ID=" + m_ID_Projekt);
            if (rs.Next())
            {
                nID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();

            // 2. Geokoordinaten auslesen
            KlimaregionCtrl ctrlklima = new KlimaregionCtrl();
            ctrlklima.ReadSingle("select * from Tab_Klimaregion where ID_Klimaregion=" + nID_Klimaregion);

            if (ctrlklima.rows > 0)
            {
                Lon = ctrlklima.Longitude;
                Lat = ctrlklima.Latitude;
            }

            Init();

            // 3. Wärmebedarf initialisieren
            Waermebedarf_gesamt = Waermebedarf.Sum();
            Max_Waermebedarf = Waermebedarf.Max();

            // 4. Schleife über Solarkollektoren
            ctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);

            for (int n = 0; n < ctrl.rows; n++)
            {
                int nId = ctrl.items[n].ID_Solar;
                int nAzimuth = ctrl.items[n].m_Azimut;
                int nNeigung = ctrl.items[n].m_Neigung;
                long nAnzahl = ctrl.items[n].Kollektormodulanzahl;

                SolarkollektorenCtrl ctrlsol = new SolarkollektorenCtrl();
                ctrlsol.ReadSingle(nId);
                double nFlaeche = ctrlsol.m_Aperturfläche;

                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadAll("select * from Tab_Solar where ID_Klimaregion=" + nID_Klimaregion + " order by ID");

                // Konstanten für das Kollektormodell
                double h0 = ctrlsol.m_h0;
                double k1 = ctrlsol.m_k1;
                double k2 = ctrlsol.m_k2;
                double kdir50 = ctrlsol.m_Kdir;
                double tStorage = 50; // Annahme Speichertemperatur
                double leitungsverluste = 0.92;

                for (int i = 0; i < ctrldat.rows; i++)
                {
                    // CalculateHourly berechnet bereits die effektive Strahlung auf der geneigten Fläche [cite: 52, 69, 71]
                    double gTilted = SolarCalculator.CalculateHourly(
                        Lon, Lat, nNeigung, nAzimuth,
                        ctrldat.items[i].Globalstrahlung,
                        ctrldat.items[i].Direktstrahlung,
                        ctrldat.items[i].Diffusstrahlung,
                        ctrldat.items[i].Außen_Temp,
                        i / 24, i % 24);

                    double ta = ctrldat.items[i].Außen_Temp;

                    // WICHTIG: cosTheta für IAM-Berechnung sauber ermitteln
                    // Wir nutzen hier den internen Wert aus dem Calculator
                    double currentCosTheta = SolarCalculator.lastCosTheta;

                    // Falls lastCosTheta nicht verfügbar, nutzen wir 1.0 als Näherung, 
                    // da gTilted bereits die Hauptwinkelkorrektur enthält.
                    var (prod, rest, ueber) = BerechneSolarthermie(
                        Waermebedarf[i],
                        gTilted,
                        nFlaeche * nAnzahl,
                        h0, k1, k2, kdir50,
                        tStorage, ta, currentCosTheta, leitungsverluste);

                    // Ergebnisse aufsummieren (für mehrere Kollektorfelder)
                    Waermeproduktion[i] += prod;
                    Restwaerme[i] = rest; // Restwärme wird pro Zeitschritt überschrieben
                    Ueberschuss[i] += ueber;
                }
            }

            Waermeproduktion_gesamt = Waermeproduktion.Sum();
            Waermeproduktion_max = Waermeproduktion.Max();
            ueberschuss_summe = Ueberschuss.Sum();

            return true;
        }

        public void Init()
        {
            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Waermeproduktion, 0, Waermeproduktion.Length);
            Array.Clear(Ueberschuss, 0, Ueberschuss.Length);
            Waermeproduktion_gesamt = 0;
            ueberschuss_summe = 0;
        }

        public (double produktion, double restbedarf, double ueberschuss) BerechneSolarthermie(
            double waermebedarf, double strahlung, double flaeche,
            double h0, double k1, double k2, double kdir50,
            double tStorage, double ta, double cosTheta, double leitungsverluste)
        {
            // 1. Spezifische Leistung berechnen (W/m²)
            double leistungProQm = CalculateThermalPower(strahlung, ta, tStorage, cosTheta, h0, k1, k2, kdir50);

            // 2. Gesamtproduktion in kW (Wh -> kWh)
            double potenzielleErzeugung = (leistungProQm * flaeche * leitungsverluste) / 1000.0;

            // 3. Bilanzierung
            double produktion = Math.Min(potenzielleErzeugung, waermebedarf);
            double ueberschuss = Math.Max(0, potenzielleErzeugung - waermebedarf);
            double restbedarf = Math.Max(0, waermebedarf - produktion);

            return (produktion, restbedarf, ueberschuss);
        }

        public double CalculateThermalPower(double gTilted, double tAmb, double tStorage,
                                          double cosTheta, double h0, double a1, double a2, double kDir50)
        {
            if (gTilted <= 0) return 0;

            // IAM (Incident Angle Modifier) Berechnung [cite: 50, 67, 69]
            double thetaRad = Math.Acos(Math.Min(Math.Max(cosTheta, 0), 1));

            // Physikalische b0-Näherung für Flachkollektoren
            double cos50 = Math.Cos(50.0 * Math.PI / 180.0);
            double b0 = (1.0 - kDir50) / (1.0 / cos50 - 1.0);

            // IAM Faktor (Vermeidung von Division durch Null bei 90°)
            double cosThetaClamped = Math.Max(cosTheta, 0.001);
            double iam = 1.0 - b0 * (1.0 / cosThetaClamped - 1.0);
            iam = Math.Max(Math.Min(iam, 1.0), 0.0);

            // Wirkungsgrad-Modell nach EN 12975
            double h0_effektiv = h0 * iam;
            double dT = tStorage - tAmb;

            // Thermischer Wirkungsgrad
            double wirkungsgrad = h0_effektiv - (a1 * dT / gTilted) - (a2 * dT * dT / gTilted);

            double leistung = gTilted * wirkungsgrad;
            return Math.Max(0, leistung);
        }
    }
}