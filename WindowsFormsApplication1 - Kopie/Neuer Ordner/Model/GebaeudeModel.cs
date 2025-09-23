using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class GebaeudeModel
    {
        public GebaeudeModel[] items;
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
        public double Fensterflaeche_Ost;
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
        public double Wohnflaeche;
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
 
        public GebaeudeModel()
        {
            items = null;
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
            Fensterflaeche_Ost = 0;
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
            Wohnflaeche = 0;
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
