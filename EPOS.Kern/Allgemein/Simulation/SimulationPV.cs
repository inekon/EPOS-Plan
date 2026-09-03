using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Photovoltaik-Modul der Simulationskette: Erzeugung, Direktverbrauch,
    /// Überschuss und Reststrom im Stundenraster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Reine PV-Rechnung seit AP2b.</b> Bis dahin steckte hier eine zweite,
    /// verlustfreie Batterielogik (Fachkonzept 8.2, Rudiment 2): Sie lud aus dem
    /// PV-Überschuss, entlud gegen die Restlast und schlug die Entnahme dem
    /// PV-Ertrag zu. Der Speicher wird jetzt ausschließlich von der
    /// <c>SpeicherEngine</c> gerechnet (<c>StromspeicherSimCtrl</c>), diese Klasse
    /// kennt ihn nicht mehr.
    /// </para>
    /// <para>
    /// <b>Geänderte Ausweissemantik.</b> <see cref="Stromproduktion"/> ist seither
    /// der Direktverbrauch (die frühere Reihe <c>Stromproduktion_OhneSpeicher</c>),
    /// <see cref="Ueberschuss"/> der volle Erzeugungsüberschuss vor Speicherladung
    /// und <see cref="Reststrom"/> die Residuallast vor Speicherentladung. Der
    /// PV-Ertragsausweis der Oberfläche fällt dadurch um die frühere Speicherentnahme
    /// niedriger aus; die Speicherwirkung wird getrennt ausgewiesen (Umsetzungskonzept
    /// Frage 12).
    /// </para>
    /// </remarks>
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

        // Ergebnis-Arrays (Viertelstündlich für das UI/Chart)
        public float[] Stromproduktion_viertelstunde = new float[8760 * 4];
        public float[] Reststrom_viertelstunde = new float[8760 * 4];
        public float[] Ueberschuss_viertelstunde = new float[8760 * 4];

        /// <summary>
        /// V1 (PV-Konzept § 2.3, Etappe P1): BHKW-Stromüberschuss, der als NEGATIVER
        /// Restbedarf im übergebenen Strombedarf steht (der BHKW-Abzug klemmt bewusst
        /// nicht auf 0, damit die SpeicherEngine ihn laden kann). Er ist KEINE
        /// PV-Erzeugung und gehört nicht in <see cref="Ueberschuss"/> — sonst würde
        /// er als PV-Einspeisung vergütet. Hier getrennt ausgewiesen [kWh je Stunde].
        /// </summary>
        public float[] BhkwUeberschuss = new float[8760];

        /// <summary>Jahressumme von <see cref="BhkwUeberschuss"/> [kWh].</summary>
        public float BhkwUeberschuss_gesamt = 0;

        // Statistiken
        public double Stromproduktion_Max = 0;
        public double MaxPSolar = 0;
        public float Stromproduktion_gesamt = 0;
        public float Stromproduktion_Theoretisch_gesamt = 0;

        public void Init()
        {
            Array.Clear(Stromproduktion, 0, Stromproduktion.Length);
            Array.Clear(Stromproduktion_Theoretisch, 0, Stromproduktion_Theoretisch.Length);
            Array.Clear(Reststrom, 0, Reststrom.Length);
            Array.Clear(Ueberschuss, 0, Ueberschuss.Length);
            Array.Clear(BhkwUeberschuss, 0, BhkwUeberschuss.Length);
            BhkwUeberschuss_gesamt = 0;
            Array.Clear(pvPotentialGesamt_stuendlich, 0, pvPotentialGesamt_stuendlich.Length);
            Modul_Ergebnisse.Clear();
        }

        public float[] Berechnung(int ID_Projekt)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();
            int nID_Klimaregion = 0;
            double Lon = 0, Lat = 0;

            Init();

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

            // SCHRITT: ZEITSCHRITT-SIMULATION (VERBRAUCH)
            for (int i = 0; i < 8760; i++)
            {
                double erzeugung = pvPotentialGesamt_stuendlich[i];
                double bedarfRoh = Strombedarf_stuendlich[i];

                // V1 (PV-Konzept § 2.3, Etappe P1): Ein NEGATIVER Restbedarf ist
                // BHKW-Überschuss — kein Bedarf und keine PV-Größe. Ohne die Klemme
                // wurde Min(erzeugung, bedarf) negativ und der BHKW-Überschuss
                // wanderte über „erzeugung − direktVerbrauch" in die PV-Einspeise-
                // reihe (Projekt 1018: 24.532 negative Viertelstunden). Für Projekte
                // ohne BHKW-Überschuss ist bedarfRoh nie negativ — ihr Ergebnis
                // bleibt identisch (Abnahmekriterium P1).
                double bedarf = Math.Max(0, bedarfRoh);
                BhkwUeberschuss[i] = (float)Math.Max(0, -bedarfRoh);

                Stromproduktion_Theoretisch[i] = (float)erzeugung;

                // Direktverbrauch - seit AP2b der EINZIGE Verrechnungsschritt hier.
                double direktVerbrauch = Math.Min(erzeugung, bedarf);

                // Ergebnisse für diese Stunde festschreiben
                Ueberschuss[i] = (float)(erzeugung - direktVerbrauch);   // Was ins Netz geht
                Reststrom[i] = (float)(bedarf - direktVerbrauch);        // Was vom Netz kommt
                Stromproduktion[i] = (float)direktVerbrauch;             // Genutzte Produktion

                if (erzeugung > Stromproduktion_Max) Stromproduktion_Max = erzeugung;
            }

            // SUMMEN & KONVERTIERUNG
            Stromproduktion_gesamt = Stromproduktion.Sum();
            Stromproduktion_Theoretisch_gesamt = Stromproduktion_Theoretisch.Sum();
            BhkwUeberschuss_gesamt = BhkwUeberschuss.Sum();

            // Für den Chart aufbereiten
            Stromproduktion_viertelstunde = Stundenwerte_zu_viertelstunden(Stromproduktion);
            Reststrom_viertelstunde = Stundenwerte_zu_viertelstunden(Reststrom);
            Ueberschuss_viertelstunde = Stundenwerte_zu_viertelstunden(Ueberschuss);

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

        // Stundenwerte_zu_viertelstunden_Interpoliert ist mit AP2b entfallen: Die
        // lineare Spreizung glättete allein die Treppenstufen des stündlich gerechneten
        // Speicherfüllstands. Die SpeicherEngine liefert den Ladezustand nativ
        // viertelstündlich (SimulationControl.Speicherfuellstand_viertelstuendlich),
        // die Interpolation hat damit keinen Gegenstand mehr.

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