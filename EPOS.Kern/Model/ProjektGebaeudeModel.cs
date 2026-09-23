using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class ProjektGebaeudeModel
    {
        public ProjektGebaeudeModel[] items;
        public int ID_Projekt;
        public int ID_Gebaeude;
        public double Z_AuswahlWohnflaeche;
        public string Einheit;
        public double Jahresnutzungsgrad;
        public bool DezentralWarmwasser;
        public string Gebaeudename;
        public string Typ;
        public string Beschreibung;
        public double Wohnflaeche_gesamt;
        public double Bewohner;
        public double Flaeche_Nutzer;
        public double Interne_Waermegewinne;
        public double Bauweise;
        public double Fensterflaeche_Sued;
        public double Fensterflaeche_OstWest;
        public double Fensterflaeche_Nord;
        public double Fensterdurchlassgrad;
        public double Raumsolltemperatur_Nachtabsenkung;
        public double Raumsolltemperatur_Tag;
        public double Raumsolltemperatur_Wochenende;
        public double Raumsolltemperatur_Ferien;
        public double Maximaleraumtemperatur;
        public double k_Wert_Außenwand;
        public double k_Wert_Fenster;
        public double k_Wert_Dachflaeche;
        public double k_Wert_Grundflaeche;
        public double k_Wert_Sonstiges;
        public double Flaeche_Außenwand;
        public double gesamte_Fensterflaeche;
        public double Dachflaeche;
        public double Grundflaeche;
        public double Sonstige_Flaechen;
        public double Nutzflaeche;
        public double Raumhoehe;
        public double Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand;
        public double Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach;
        public double Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke;
        public double Abmessung_Anschluß_Fenster_Wand;
        public double Abmessung_Anschluß_Wand_Dach;
        public double Abmessung_Anschluß_Außenwand_Kellerdecke;
        public double Luftwechselrate;
        public double Wochenende;
        public double Ferien;
        public double Ferienbeginn_1;
        public double Ferienende_1;
        public double Ferienbeginn_2;
        public double Ferienende_2;
        public double Ferienbeginn_3;
        public double Ferienende_3;
        public double Ferienbeginn_4;
        public double Ferienende_4;
        public double WW_Bedarf;
        public double spez_Waermeverbrauch;
        public double Waermebedarf;
        public string Baualtersklasse;
        public string Gebaeudeart;
        public string Wohngebaeude_Nicht_Wohngebaeude;

        /// <summary>
        /// Rechenweg des Gebäudes (<c>Gebaeude_Modell</c>); <c>null</c> = VDI 6007 (E1).
        /// Gelesen von der Weiche <c>SimulationWaermebedarf.RechenwegWaehlen</c>; fällt mit
        /// der Stufe GA.
        /// </summary>
        public string Gebaeude_Modell;

        // =====================================================================
        //  Die übrigen vierzehn Gebäudespalten des Schritts M3 (Schemaschritt 101,
        //  Umsetzungskonzept 1.6/1.7) — NULL-ERHALTEND: null heißt „Vorgabe des
        //  Eingangsbauers" (Rechenschritte 1.1), nie 0. Gelesen allein vom VDI-Weg
        //  (GebaeudeModellEingang); der Tagesbilanz-Weg kennt sie nicht.
        // =====================================================================

        /// <summary>Fensterfläche Ost [m²]; null = ½ <see cref="Fensterflaeche_OstWest"/> (Vorbereitungsschritt).</summary>
        public double? Fensterflaeche_Ost;
        /// <summary>Fensterfläche West [m²]; null = ½ <see cref="Fensterflaeche_OstWest"/> (Vorbereitungsschritt).</summary>
        public double? Fensterflaeche_West;
        /// <summary>Rahmenanteil 1 − F_F [–]; null = Vorgabe.</summary>
        public double? Rahmenanteil;
        /// <summary>Verschattungsfaktor F_S [–]; null = Vorgabe.</summary>
        public double? Verschattungsfaktor;
        /// <summary>Randbedingung der Grundfläche (<c>DbWerte.GRUND_*</c>); null = Erdreich.</summary>
        public string Grundflaeche_Randbedingung;
        /// <summary>Kellertemperatur [°C] bei Randbedingung Keller; null = Vorgabe.</summary>
        public double? Kellertemperatur;
        /// <summary>Masseanteil der Außenbauteile a_AW [–]; null = Vorgabe.</summary>
        public double? Masseanteil_Aussen;
        /// <summary>Innenflächenfaktor f_IW [–]; null = Vorgabe.</summary>
        public double? Innenflaechenfaktor;
        /// <summary>Strahlungsanteil der Heizübergabe [–]; null = Vorgabe.</summary>
        public double? Heizung_Strahlungsanteil;
        /// <summary>Heizleistungsgrenze [kW]; null = unbegrenzt.</summary>
        public double? Heizleistung_Max;
        /// <summary>Strahlung auf Außenbauteile (Schalter, NOT NULL DEFAULT 0).</summary>
        public bool Aussenbauteile_Strahlung;
        /// <summary>Infiltration [1/h] (Stufe G2); null = Vorgabe.</summary>
        public double? Luftwechsel_Infiltration;
        /// <summary>Nutzerlüftung [1/h] (Stufe G2); null = Vorgabe.</summary>
        public double? Luftwechsel_Nutzer;
        /// <summary>Sommerlüftung (Schalter, Stufe G2, NOT NULL DEFAULT 0).</summary>
        public bool Sommerlueftung;

        // =====================================================================
        //  Die vier Kühleingaben aus KU-S1 (Schemaschritt 108, Kühlkonzept 7.1) —
        //  NULL-ERHALTEND wie der Block darüber. Gelesen aus der Sicht, von keinem
        //  Rechenweg benutzt, bis der Kühlkanal steht (zweite Welle von KU1).
        // =====================================================================

        /// <summary>Kühlsollwert [°C]; null = Kühlung aus (kein stiller Rückfall auf die Maximaltemperatur).</summary>
        public double? Kuehl_Sollwert;
        /// <summary>Kühlleistungsgrenze [kW]; null = unbegrenzt.</summary>
        public double? Kuehlleistung_Max;
        /// <summary>„Dieses Gebäude wird gekühlt" (Schalter, NOT NULL DEFAULT 0).</summary>
        public bool Kuehlung_Aktiv;
        /// <summary>Kühlsollwert der Nacht [°C]; null = wie <see cref="Kuehl_Sollwert"/>. Gelesen erst ab KU3.</summary>
        public double? Kuehl_Sollwert_Nacht;

        public ProjektGebaeudeModel()
        {
            items = null;
            ID_Projekt = 0;
            ID_Gebaeude = 0;
            Z_AuswahlWohnflaeche = 0.0;
            Einheit = "";
            Jahresnutzungsgrad = 0.0;
            DezentralWarmwasser = false;
            Gebaeudename = "";
            Typ = "Wohngebaeude  VDI 2067";
            Beschreibung = "";
            Wohnflaeche_gesamt = 0;
            Bewohner = 0;
            Flaeche_Nutzer = 0;
            Interne_Waermegewinne = 0;
            Bauweise = 10000;
            Fensterflaeche_Sued = 0;
            Fensterflaeche_OstWest = 0;
            Fensterflaeche_Nord = 0;
            Fensterdurchlassgrad = 0;
            Raumsolltemperatur_Nachtabsenkung = 0;
            Raumsolltemperatur_Tag = 0;
            Raumsolltemperatur_Wochenende = 0;
            Raumsolltemperatur_Ferien = 0;
            Maximaleraumtemperatur = 0;
            k_Wert_Außenwand = 0;
            k_Wert_Fenster = 0;
            k_Wert_Dachflaeche = 0;
            k_Wert_Grundflaeche = 0;
            k_Wert_Sonstiges = 0;
            Flaeche_Außenwand = 0;
            gesamte_Fensterflaeche = 0;
            Dachflaeche = 0;
            Grundflaeche = 0;
            Sonstige_Flaechen = 0;
            Nutzflaeche = 0;
            Raumhoehe = 0;
            Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = 0;
            Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = 0;
            Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = 0;
            Abmessung_Anschluß_Fenster_Wand = 0;
            Abmessung_Anschluß_Wand_Dach = 0;
            Abmessung_Anschluß_Außenwand_Kellerdecke = 0;
            Luftwechselrate = 0;
            Wochenende = 0;
            Ferien = 0;
            Ferienbeginn_1 = 0;
            Ferienende_1 = 0;
            Ferienbeginn_2 = 0;
            Ferienende_2 = 0;
            Ferienbeginn_3 = 0;
            Ferienende_3 = 0;
            Ferienbeginn_4 = 0;
            Ferienende_4 = 0;
            WW_Bedarf = 0;
            spez_Waermeverbrauch = 0;
            Waermebedarf = 0;
            Baualtersklasse = "S";
            Gebaeudeart = "Einfamilienhaus";
            Wohngebaeude_Nicht_Wohngebaeude = "Wohngebaeude";
        }
    }
}