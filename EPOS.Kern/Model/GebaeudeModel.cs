using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class GebaeudeModel
    {
        public int ID;
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

        // ---- Gebaeudespalten-Schritt M3 (Schemaschritt 101, Stufe G1/G2) ----------------
        // NULL-ERHALTEND: NULL heisst "Vorgabe", und diese Vorgabe gehoert dem Leser, nicht
        // dem Modell. Die Felder tragen den Katalogwert unveraendert durch Lesen, Insert,
        // Overwrite und die Katalogkopie (Umsetzungskonzept Gebaeudesimulation 1.6).
        // Kein Rechenweg liest sie in diesem Schritt. Namen: GebaeudeSchema.SPALTE_*.
        public string Gebaeude_Modell;
        public double? Fensterflaeche_Ost;
        public double? Fensterflaeche_West;
        public double? Rahmenanteil;
        public double? Verschattungsfaktor;
        public string Grundflaeche_Randbedingung;
        public double? Kellertemperatur;
        public double? Masseanteil_Aussen;
        public double? Innenflaechenfaktor;
        public double? Heizung_Strahlungsanteil;
        public double? Heizleistung_Max;
        public bool Aussenbauteile_Strahlung;
        public double? Luftwechsel_Infiltration;
        public double? Luftwechsel_Nutzer;
        public bool Sommerlueftung;

        // ---- KU-S1, die Kuehleingaben (Schemaschritt 108, Kuehlkonzept 7.1) --------------
        // NULL-ERHALTEND wie der Block darueber: Kuehl_Sollwert NULL heisst "Kuehlung aus",
        // Kuehlleistung_Max NULL "unbegrenzt", Kuehl_Sollwert_Nacht NULL "wie der Tagwert";
        // der Schalter kennt kein NULL. Der Rechenweg liest die Projektkopie
        // (ProjektGebaeudeModel), nicht dieses Modell.
        // Namen: GebaeudeSchema.SPALTE_KUEHL*.
        public double? Kuehl_Sollwert;
        public double? Kuehlleistung_Max;
        public bool Kuehlung_Aktiv;
        public double? Kuehl_Sollwert_Nacht;

        // ---- AK-S1, die Waermeuebergabe (Schemaschritt 122, Anlagenkopplung 8.1) --------
        // NULL-ERHALTEND wie die Bloecke darueber: Uebergabe_Art NULL heisst "ideal" (Kopplung
        // aus), jede andere NULL-Zahl "Vorgabe" bzw. "hergeleitet" (8.4), Sollwertprofil NULL
        // "die vier Bestandssollwerte"; die zwei Schalter kennen kein NULL. Kein Rechenweg
        // liest die Felder in der ersten Welle von AK1. Namen: GebaeudeSchema.SPALTE_*.
        public bool Heizkreis_Aktiv;
        public string Uebergabe_Art;
        public double? Uebergabe_Exponent;
        public double? Uebergabe_Leistung_Nenn;
        public double? Auslegung_Vorlauf;
        public double? Auslegung_Ruecklauf;
        public double? Auslegung_Raumtemperatur;
        public double? Auslegung_Aussentemperatur;
        public bool Heizkurve_Aktiv;
        public double? Heizkurve_Niveau;
        public double? Heizkurve_Steilheit;
        public double? Regler_Proportionalband;
        public string Sollwertprofil;

        // ---- KAK-S1, die Kuehluebergabe (E37, Anlagenkopplung 8.1) -----------------------
        // NULL-ERHALTEND wie die Bloecke darueber: Kuehl_Uebergabe_Art NULL heisst "ideal"
        // (Kaelteseite nicht gekoppelt), jede andere NULL-Zahl "Vorgabe der Art" bzw.
        // "hergeleitet" (Auslegungstag); der Schalter kennt kein NULL (A1, wie Heizkreis_Aktiv).
        // Namen: GebaeudeSchema.SPALTE_KUEHL*.
        public bool Kuehluebergabe_Aktiv;
        public string Kuehl_Uebergabe_Art;
        public double? Kuehl_Uebergabe_Exponent;
        public double? Kuehl_Uebergabe_Leistung_Nenn;
        public double? Kuehl_Auslegung_Vorlauf;
        public double? Kuehl_Auslegung_Ruecklauf;
        public double? Kuehl_Auslegung_Raumtemperatur;
        public double? Kuehl_Vorlaufgrenze;

        // ---- Das Baujahr (G4a, Umsetzungskonzept 3.4; BaujahrSchema.SCHRITT) ---------------
        // Die Jahreszahl 1500 bis 2100, NULL-ERHALTEND: null heisst "unbekannt". Steht neben der
        // Baualtersklasse und steuert nichts. Name: GebaeudeSchema.SPALTE_BAUJAHR.
        public int? Baujahr;

        // ---- Die Nachtzeit (E43, N1.48; NachtzeitSchema.SCHRITT) -------------------------------
        // Beginn und Ende der Nachtabsenkung als Stunde des Tages 0 bis 23, NULL-ERHALTEND: beide
        // null heisst die Vorgabe 22 bis 6 Uhr (Nachtzeit.Vorgabe). Namen:
        // GebaeudeSchema.SPALTE_NACHTABSENKUNG_BEGINN / SPALTE_NACHTABSENKUNG_ENDE.
        public int? Nachtabsenkung_Beginn;
        public int? Nachtabsenkung_Ende;

        public GebaeudeModel()
        {
            ID = 0;
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
