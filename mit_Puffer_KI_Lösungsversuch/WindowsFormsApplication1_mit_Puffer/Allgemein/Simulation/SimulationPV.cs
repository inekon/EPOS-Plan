using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    public class SimulationPV
    {
        // --- Datenstrukturen ---
        public List<int> photovoltaik_list = new List<int>();
        // Ergebnis je PV-Modul(feld) fuer die Auflistung in der Ergebnismaske.
        public List<PVModulErgebnis> Modul_Ergebnisse = new List<PVModulErgebnis>();
        public int m_ID_Projekt = 0;

        // Input-Arrays (15-Minuten-Werte vom Lastprofil)
        public float[] Strombedarf = new float[8760 * 4];

        // Interne Stunden-Arrays für die Simulation
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] pvPotentialGesamt_stuendlich = new float[8760];

        // Ergebnis-Arrays (Stündlich)
        public float[] Stromproduktion_Theoretisch = new float[8760];
        public float[] Stromproduktion = new float[8760];
        public float[] Reststrom = new float[8760];
        public float[] Ueberschuss = new float[8760];
        public float[] Speicherfuellstand = new float[8760];
        public float[] Stromproduktion_OhneSpeicher = new float[8760];

        // Ergebnis-Arrays (Viertelstündlich für das UI/Chart)
        public float[] Stromproduktion_viertelstunde = new float[8760 * 4];
        public float[] Stromproduktion_OhneSpeicher_viertelstunde = new float[8760 * 4];
        public float[] Reststrom_viertelstunde = new float[8760 * 4];
        public float[] Ueberschuss_viertelstunde = new float[8760 * 4];
        public float[] Speicherfuellstand_viertelstunde = new float[8760 * 4];

        // Statistiken
        public double Stromproduktion_Max = 0;
        public double MaxPSolar = 0;
        public float Stromproduktion_gesamt = 0;
        public float Stromproduktion_Theoretisch_gesamt = 0;

        // Speicher-Parameter
        public double SpeicherKapazitaetKWh = 0;
        public double MaxLadeLeistungKW = 0;

        public void Init()
        {
            Array.Clear(Stromproduktion, 0, Stromproduktion.Length);
            Array.Clear(Stromproduktion_Theoretisch, 0, Stromproduktion_Theoretisch.Length);
            Array.Clear(Reststrom, 0, Reststrom.Length);
            Array.Clear(Ueberschuss, 0, Ueberschuss.Length);
            Array.Clear(Speicherfuellstand, 0, Speicherfuellstand.Length);
            Array.Clear(pvPotentialGesamt_stuendlich, 0, pvPotentialGesamt_stuendlich.Length);
            Array.Clear(Speicherfuellstand_viertelstunde, 0, Speicherfuellstand_viertelstunde.Length);
            Array.Clear(Speicherfuellstand, 0, Speicherfuellstand.Length);
            Array.Clear(Stromproduktion_OhneSpeicher_viertelstunde, 0, Stromproduktion_OhneSpeicher_viertelstunde.Length);
            Array.Clear(Stromproduktion_OhneSpeicher, 0, Stromproduktion_OhneSpeicher.Length);
            SpeicherKapazitaetKWh = 0;
            Modul_Ergebnisse.Clear();
        }

        public float[] Berechnung(int ID_Projekt)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();
            int nID_Klimaregion = 0;
            double Lon = 0, Lat = 0;
            int id = 0;

            Init();

            // alle Sromspeicher zum Projekt durchgehen und Leistung aufsummieren (oder direkt aus sim-Objekt, falls dort schon vorhanden)
            ctrl.ReadAllFilter("ID_Projekt=" + ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
            
            for (int i = 0; i < ctrl.rows; i++)
            {
                id = ctrl.items[i].ID_SP;
                rs.Open("select * from Tab_Stromspeicher where ID=" + id);
                if (rs.Next())
                {
                    SpeicherKapazitaetKWh += (double)rs.Read("Energie");
                }
                rs.Close();
                MaxLadeLeistungKW = SpeicherKapazitaetKWh; 
            }
     
            // Bedarf von 15-Min auf 1-Std mitteln
            Strombedarf_stuendlich = Viertelstunden_zu_stunden(Strombedarf);

            // Geodaten laden
            rs.Open("select * from Tab_Projekt where ID=" + ID_Projekt);
            if (rs.Next()) nID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            rs.Close();

            KlimaregionCtrl ctrlklima = new KlimaregionCtrl();
            ctrlklima.ReadSingle("select * from Tab_Klimaregion where ID=" + nID_Klimaregion);
            if (ctrlklima.rows > 0) { Lon = ctrlklima.Longitude; Lat = ctrlklima.Latitude; }

            // PV-POTENTIAL ALLER MODULE SAMMELN
            ctrl.ReadAllFilter("ID_Projekt=" + ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);
            
            for (int n = 0; n < ctrl.rows; n++)
            {
                PhotovoltaikCtrl ctrlsol = new PhotovoltaikCtrl();
                ctrlsol.ReadSingle(ctrl.items[n].ID_PV);
                double nFlaecheGesamt = ctrlsol.m_Breite * ctrlsol.m_Laenge * (long)ctrl.items[n].PV_Leistung;
                double nennWirk = ctrlsol.m_Wirkungsgrad / 100.0;
                double tempKoeff = ctrlsol.m_Temp_Coeff_Pmax / 100.0;

                SolardatenCtrl ctrldat = new SolardatenCtrl();
                ctrldat.ReadAll("select * from Tab_Solar where ID_Klimaregion=" + nID_Klimaregion + " order by ID");

                double prodSummeMod = 0;

                for (int i = 0; i < ctrldat.rows; i++)
                {
                    double effStr = SolarCalculator.CalculateHourly(Lon, Lat, ctrl.items[n].m_Neigung, ctrl.items[n].m_Azimut,
                                    ctrldat.items[i].Globalstrahlung, ctrldat.items[i].Direktstrahlung,
                                    ctrldat.items[i].Diffusstrahlung, ctrldat.items[i].Außen_Temp, i / 24, i % 24);

                    if (effStr > MaxPSolar) MaxPSolar = effStr;

                    // Theoretische Erzeugung dieses Moduls berechnen
                    var erg = BerechnePV(Strombedarf_stuendlich[i], effStr, nFlaecheGesamt, nennWirk, tempKoeff, ctrldat.items[i].Außen_Temp, 1.0);

                    // Aufsummieren auf das Stunden-Array (nach Wechselrichter 95%)
                    pvPotentialGesamt_stuendlich[i] += (float)(erg.potenzielleErzeugung * 0.95);

                    prodSummeMod += erg.potenzielleErzeugung * 0.95;
                }

                Modul_Ergebnisse.Add(new PVModulErgebnis
                {
                    Name = ctrl.items[n].Bezeichner,
                    Flaeche = nFlaecheGesamt,
                    Anzahl = (long)ctrl.items[n].PV_Leistung,
                    Stromproduktion = prodSummeMod
                });
            }

            // SCHRITT: ZEITSCHRITT-SIMULATION (BATTERIE & VERBRAUCH)
            double aktuellerSOC = 0; // Aktueller Speicherinhalt in kWh

            for (int i = 0; i < 8760; i++)
            {
                double erzeugung = pvPotentialGesamt_stuendlich[i];
                double bedarf = Strombedarf_stuendlich[i];

                Stromproduktion_Theoretisch[i] = (float)erzeugung;

                // Priorität 1: Direktverbrauch
                double direktVerbrauch = Math.Min(erzeugung, bedarf);

                double ueberschussNachDirekt = erzeugung - direktVerbrauch;
                double restbedarfNachPV = bedarf - direktVerbrauch;

                double ladeEnergie = 0;
                double entnahmeEnergie = 0;

                // Priorität 2: Speicher laden (wenn Überschuss da ist)
                if (ueberschussNachDirekt > 0)
                {
                    ladeEnergie = Math.Min(ueberschussNachDirekt, SpeicherKapazitaetKWh - aktuellerSOC);
                    ladeEnergie = Math.Min(ladeEnergie, MaxLadeLeistungKW);
                    aktuellerSOC += ladeEnergie;
                }

                // Priorität 3: Speicher entladen (wenn noch Bedarf offen ist)
                if (restbedarfNachPV > 0)
                {
                    entnahmeEnergie = Math.Min(restbedarfNachPV, aktuellerSOC);
                    entnahmeEnergie = Math.Min(entnahmeEnergie, MaxLadeLeistungKW);
                    aktuellerSOC -= entnahmeEnergie;
                }

                // Ergebnisse für diese Stunde festschreiben
                Ueberschuss[i] = (float)(ueberschussNachDirekt - ladeEnergie); // Was ins Netz geht
                Reststrom[i] = (float)(restbedarfNachPV - entnahmeEnergie);   // Was vom Netz kommt
                Speicherfuellstand[i] = (float)aktuellerSOC;                  // Aktueller SOC

                // Genutzte Produktion = Direkt verbraucht + in den Speicher geladen
                Stromproduktion[i] = (float)(direktVerbrauch + entnahmeEnergie);
                Stromproduktion_OhneSpeicher[i] = (float)direktVerbrauch;   

                if (erzeugung > Stromproduktion_Max) Stromproduktion_Max = erzeugung;
            }

            // SUMMEN & KONVERTIERUNG
            Stromproduktion_gesamt = Stromproduktion.Sum();
            Stromproduktion_Theoretisch_gesamt = Stromproduktion_Theoretisch.Sum();

            // Für den Chart aufbereiten
            Stromproduktion_viertelstunde = Stundenwerte_zu_viertelstunden(Stromproduktion);
            Reststrom_viertelstunde = Stundenwerte_zu_viertelstunden(Reststrom);
            Ueberschuss_viertelstunde = Stundenwerte_zu_viertelstunden(Ueberschuss);

            // Wichtig für die Nachtkurve: Interpolation des Speicherstands
            Speicherfuellstand_viertelstunde = Stundenwerte_zu_viertelstunden_Interpoliert(Speicherfuellstand);

            return Stromproduktion_viertelstunde;
        }

        // --- Hilfsmethoden ---

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {
            float[] v = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                v[i * 4] = v[i * 4 + 1] = v[i * 4 + 2] = v[i * 4 + 3] = stundenwerte[i];
            }
            return v;
        }

        public float[] Stundenwerte_zu_viertelstunden_Interpoliert(float[] stundenwerte)
        {
            float[] v = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                float curr = stundenwerte[i];
                float next = (i < stundenwerte.Length - 1) ? stundenwerte[i + 1] : curr;
                float diff = (next - curr) / 4.0f;

                v[i * 4] = curr;
                v[i * 4 + 1] = curr + diff;
                v[i * 4 + 2] = curr + (diff * 2);
                v[i * 4 + 3] = curr + (diff * 3);
            }
            return v;
        }

        public float[] Viertelstunden_zu_stunden(float[] v)
        {
            float[] s = new float[v.Length / 4];
            for (int i = 0; i < s.Length; i++)
            {
                s[i] = (v[i * 4] + v[i * 4 + 1] + v[i * 4 + 2] + v[i * 4 + 3]) / 4.0f;
            }
            return s;
        }

        public (double produktion, double restbedarf, double ueberschuss, double potenzielleErzeugung) BerechnePV(
                double bedarf, double strahlung, double flaeche, double nennWirk, double tempKoeff, double tAmb, double cosTheta)
        {
            double tCell = tAmb + (strahlung / 800.0) * 25.0;
            double wirk = nennWirk * (1 + tempKoeff * (tCell - 25.0));
            double potErzeugung = (strahlung * cosTheta * flaeche * wirk) / 1000.0;

            double prod = Math.Min(potErzeugung, bedarf);
            double rest = Math.Max(0, bedarf - prod);
            double ueb = Math.Max(0, potErzeugung - bedarf);

            return (prod, rest, ueb, potErzeugung);
        }
    }

    // Ergebnis eines einzelnen PV-Modul(felds) fuer die Ergebnis-Auflistung.
    public class PVModulErgebnis
    {
        public string Name = "";
        public double Flaeche;          // m^2 gesamt
        public long Anzahl;             // Modulanzahl
        public double Stromproduktion;  // kWh/a (theoretisch, nach Wechselrichter)
    }
}