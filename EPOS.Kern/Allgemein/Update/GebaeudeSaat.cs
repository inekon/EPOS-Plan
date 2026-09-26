using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE KATALOGSAETZE DER BAUALTERSKLASSEN M UND A - Entscheid E51 (Konzept Gebaeudesimulation
    // N1.58, Konzept Baualtersklassen 4), gesaet mit dem Schemaschritt GebaeudeSaatSchema.SCHRITT.
    //
    // WOZU. Nach E47 hatten die Klassen M (ab 2021) und A (bis 1859) keinen Satz im Gebaeudekatalog
    // und damit keine Vorgabe fuer U-Werte, g-Wert und psi. Die sechs Saetze hier schliessen die
    // Luecke: drei fuer M (Referenzgebaeude des GEG, Effizienzhaus 55, kleines Mehrfamilienhaus nach
    // dem Typgebaeude 2021 bis 2025), drei fuer A (Urzustand EFH und kleines MFH, anteilig
    // modernisiertes EFH). Die Mediane in GebaeudeVorgaben rechnet GebaeudeVorgabenTests aus der
    // Testdatenbank nach, die die Saetze ueber denselben Schritt traegt.
    //
    // QUELLEN. U-Werte, g-Wert und Waermebrueckenzuschlag von M1 aus dem GEG (Referenzgebaeude
    // Wohngebaeude, Anlage 1), M2 davon x 0,70 (Effizienzhaus 55: H'T hoechstens 70 % des
    // Referenzgebaeudes, KfW "Das Effizienzhaus", Abruf 26.09.2026; die gleichmaessige Skalierung
    // aller Bauteile ist eine benannte EPOS-Annahme). Alle Flaechen, Wohnflaechen und die Werte von
    // M3 und A1 bis A3 aus Stein, B.; Loga, T. (2025): Das Typgebaeude-Modell zur energetischen
    // Bewertung des Wohngebaeudebestands - Beschreibung, Nutzungshinweise und Tabellenwerte, IWU im
    // Auftrag des BBSR, Zenodo, https://doi.org/10.5281/zenodo.15488271, Lizenz CC BY 4.0
    // (Namensnennung; die Zusammenfassung und Herleitung unten ist eine Aenderung im Sinne der
    // Lizenz). Genutzt sind allein Anhang A, Tab. 26 (S. 66), 27 (S. 67), 28 (S. 68), 33 (S. 73),
    // 34 (S. 74) und 39 (S. 79), differenziertes Modell, Referenzjahr 2025 - nicht Tab. 60 (Werte
    // der Typologie 2015, nicht frei). Die Quelle fasst alles vor 1918 in EINER Klasse "bis 1918"
    // zusammen; die Klassen A und B beruhen damit auf derselben Quellklasse.
    //
    // REGELN FUER ALLE SECHS SAETZE (Vorschlag vom Anwender bestaetigt am 26.09.2026):
    //   Aussenwand = Aussenwand netto; Dach = Dach + oberste Geschossdecke, U flaechengewichtet;
    //   Grund = Kellerdecke + Boden + Wand gegen Keller + Wand gegen Erdreich, U flaechengewichtet;
    //   Sonstige = Aussentueren.
    //   l_FW = Umfang des Standardfensters 1,23 m x 1,48 m (Stein/Loga Tab. 62) je m2 Fenster
    //   = 2,98 m/m2 x Fensterflaeche; l_WD = l_AK = Umfang einer quadratischen Grundflaeche aus
    //   Kellerdecke + Boden gegen Erdreich.
    //   psi im Verhaeltnis 0,05 : 0,16 : 0,24, skaliert so, dass Summe psi x l = dU_WB x Huellflaeche.
    //   Nutzflaeche = Wohnflaeche; Flaeche je Nutzer 35 m2; innere Gewinne 5 W/m2 x Wohnflaeche (E43);
    //   Bauweise 50 Wh/(m2K) x Wohnflaeche (schwere Bauart; Stein/Loga Tab. 39: 49,5 Wh/(m2K) fuer
    //   EZFH); Sollwerte 20 / 18 / 24 degC; Warmwasserbedarf 700 und Ferienfahrplan "keine Ferien"
    //   nach dem Muster der Wohngebaeude im Katalog; Fenster gleich auf die vier Himmelsrichtungen
    //   verteilt; Luftwechsel 0,6 1/h (M) bzw. 0,7 1/h (A) nach dem Muster; Baujahr, die Spalten
    //   der Stufen G1/G2 und der Uebergabe leer; ReadOnly = 1.
    //
    // KENNZAHL IM NAMEN. Die Namen des Bestands tragen am Ende die Katalogkennzahl
    // spez_Waermeverbrauch ("EFH-A-U-338"). Sie stammt aus dem Vorlaeufer und laesst sich mit keinem
    // heutigen Rechenweg nachvollziehen (Befund D der Gebaeudesimulation: das Tagesmodell erreicht
    // 48 bis 88 %, der VDI-6007-Weg 86 bis 100 % davon). Die sechs Saetze tragen deshalb KEINE
    // Kennzahl; spez_Waermeverbrauch und Waermebedarf bleiben leer.
    //
    // SCHLUESSEL IST DER BEZEICHNER (eindeutiger Index Tab_Gebaeude_STAMM_Gebaeudename), keine feste
    // Id: GebaeudeSaatSchema.SaatSchreiben legt nur an, was unter seinem Namen fehlt, und
    // ueberschreibt nie.
    // ====================================================================================

    /// <summary>
    /// Ein Katalogsatz der Saat (Entscheid E51) — die Werte, wie sie in <c>Tab_Gebaeude_STAMM</c>
    /// stehen; abgeleitete Spalten (Bewohner, innere Gewinne, Bauweise, Fenster je Richtung) rechnet
    /// die Klasse nach den Regeln im Kopf der Datei.
    /// </summary>
    public sealed class GebaeudeSaat
    {
        public GebaeudeSaat(string bezeichner, string gebaeudeart, string klasse, string energiestandard,
                            double wohnflaeche, double raumhoehe, double luftwechsel,
                            double flaecheAussenwand, double flaecheFenster, double flaecheDach, double flaecheGrund,
                            double flaecheSonstige,
                            double uAussenwand, double uFenster, double uDach, double uGrund, double uSonstige, double gWert,
                            double deltaUWb, double psiFensterWand, double psiWandDach, double psiAussenwandKeller,
                            double laengeFensterWand, double laengeWandDach, double laengeAussenwandKeller,
                            string beschreibung)
        {
            Bezeichner = bezeichner;
            Gebaeudeart = gebaeudeart;
            Klasse = klasse;
            Energiestandard = energiestandard;
            Wohnflaeche = wohnflaeche;
            Raumhoehe = raumhoehe;
            Luftwechsel = luftwechsel;
            FlaecheAussenwand = flaecheAussenwand;
            FlaecheFenster = flaecheFenster;
            FlaecheDach = flaecheDach;
            FlaecheGrund = flaecheGrund;
            FlaecheSonstige = flaecheSonstige;
            UAussenwand = uAussenwand;
            UFenster = uFenster;
            UDach = uDach;
            UGrund = uGrund;
            USonstige = uSonstige;
            GWert = gWert;
            DeltaUWb = deltaUWb;
            PsiFensterWand = psiFensterWand;
            PsiWandDach = psiWandDach;
            PsiAussenwandKeller = psiAussenwandKeller;
            LaengeFensterWand = laengeFensterWand;
            LaengeWandDach = laengeWandDach;
            LaengeAussenwandKeller = laengeAussenwandKeller;
            Beschreibung = beschreibung;
        }

        /// <summary>Der Name — zugleich der Schlüssel der Saat (eindeutiger Index).</summary>
        public string Bezeichner { get; }

        /// <summary>Die Gebäudeart des Katalogs („Einfamilienhaus", „kleines Mehrfamilienhaus").</summary>
        public string Gebaeudeart { get; }

        /// <summary>Die Baualtersklasse A…M (E47).</summary>
        public string Klasse { get; }

        /// <summary>Der Code des Energiestandards (<see cref="WindowsFormsApplication1.Energiestandard"/>); <c>null</c> = keiner.</summary>
        public string Energiestandard { get; }

        /// <summary>Wohnfläche = Nutzfläche [m²].</summary>
        public double Wohnflaeche { get; }

        /// <summary>Raumhöhe [m].</summary>
        public double Raumhoehe { get; }

        /// <summary>Luftwechselrate [1/h].</summary>
        public double Luftwechsel { get; }

        /// <summary>Außenwand netto [m²].</summary>
        public double FlaecheAussenwand { get; }
        /// <summary>Fenster gesamt [m²].</summary>
        public double FlaecheFenster { get; }
        /// <summary>Dach + oberste Geschossdecke [m²].</summary>
        public double FlaecheDach { get; }
        /// <summary>Kellerdecke + Boden + Wand gegen Keller + Wand gegen Erdreich [m²].</summary>
        public double FlaecheGrund { get; }
        /// <summary>Außentüren [m²].</summary>
        public double FlaecheSonstige { get; }

        /// <summary>U Außenwand [W/(m²K)].</summary>
        public double UAussenwand { get; }
        /// <summary>U Fenster [W/(m²K)].</summary>
        public double UFenster { get; }
        /// <summary>U Dach, flächengewichtet [W/(m²K)].</summary>
        public double UDach { get; }
        /// <summary>U Grund, flächengewichtet [W/(m²K)].</summary>
        public double UGrund { get; }
        /// <summary>U Außentüren [W/(m²K)].</summary>
        public double USonstige { get; }
        /// <summary>Gesamtenergiedurchlassgrad g [–].</summary>
        public double GWert { get; }

        /// <summary>Wärmebrückenzuschlag ΔU_WB der Quelle [W/(m²K)] — Grundlage der ψ, nicht gespeichert.</summary>
        public double DeltaUWb { get; }

        /// <summary>ψ Fenster–Wand [W/(mK)].</summary>
        public double PsiFensterWand { get; }
        /// <summary>ψ Wand–Dach [W/(mK)].</summary>
        public double PsiWandDach { get; }
        /// <summary>ψ Außenwand–Kellerdecke [W/(mK)].</summary>
        public double PsiAussenwandKeller { get; }

        /// <summary>Länge Anschluss Fenster–Wand [m].</summary>
        public double LaengeFensterWand { get; }
        /// <summary>Länge Anschluss Wand–Dach [m].</summary>
        public double LaengeWandDach { get; }
        /// <summary>Länge Anschluss Außenwand–Kellerdecke [m].</summary>
        public double LaengeAussenwandKeller { get; }

        /// <summary>Die Beschreibung des Katalogs samt Quellenangabe.</summary>
        public string Beschreibung { get; }

        /// <summary>Bewohner = Wohnfläche ÷ Fläche je Nutzer (<see cref="GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE"/>).</summary>
        public double Bewohner => Wohnflaeche / GebaeudeStammCtrl.FLAECHE_JE_NUTZER_VORGABE;

        /// <summary>
        /// Innere Gewinne [W] = <see cref="GebaeudeStammCtrl.INNERE_GEWINNE_JE_M2_VORGABE"/> × Wohnfläche (E43),
        /// auf sechs Nachkommastellen gerundet (ohne Gleitkommarest).
        /// </summary>
        public double InnereGewinne => Math.Round(GebaeudeStammCtrl.INNERE_GEWINNE_JE_M2_VORGABE * Wohnflaeche, 6);

        /// <summary>
        /// Bauweise [Wh/K] = 50 × Wohnfläche (schwere Bauart, <see cref="Gebaeudebauweise.SCHWER"/>), auf sechs
        /// Nachkommastellen gerundet (303,4 × 50 ergäbe sonst 15 169,999…).
        /// </summary>
        public double Bauweise => Math.Round(Gebaeudebauweise.BauweiseAusBauart(Gebaeudebauweise.SCHWER, Wohnflaeche), 6);

        /// <summary>Fenster Süd = Nord = ein Viertel der Fensterfläche.</summary>
        public double FensterSued => FlaecheFenster / 4.0;

        /// <summary>Fenster Ost + West zusammen = die Hälfte der Fensterfläche.</summary>
        public double FensterOstWest => FlaecheFenster / 2.0;

        /// <summary>Fenster Nord = ein Viertel der Fensterfläche.</summary>
        public double FensterNord => FlaecheFenster / 4.0;

        /// <summary>Die Hüllfläche [m²] — Summe der fünf Flächen.</summary>
        public double Huellflaeche => FlaecheAussenwand + FlaecheFenster + FlaecheDach + FlaecheGrund + FlaecheSonstige;
    }

    /// <summary>
    /// <b>Die sechs Katalogsätze der Klassen M und A</b> (Entscheid E51). Gelesen über
    /// <see cref="GebaeudeSaatSchema.Saat"/>; Quellen und Regeln im Kopf der Datei.
    /// </summary>
    public static class GebaeudeSaattabelle
    {
        /// <summary>Typ der Wohngebäude des Katalogs (zwei Leerzeichen wie im Bestand).</summary>
        public const string TYP_WOHNGEBAEUDE = "Wohngebaeude  VDI 2067";

        /// <summary>Gebäudeart Einfamilienhaus.</summary>
        public const string EFH = "Einfamilienhaus";

        /// <summary>Gebäudeart kleines Mehrfamilienhaus.</summary>
        public const string KMH = "kleines Mehrfamilienhaus";

        /// <summary>Wert der Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>.</summary>
        public const string WOHNGEBAEUDE = "Wohngebaeude";

        /// <summary>Warmwasserbedarf nach dem Muster der Wohngebäude im Katalog (häufigster Wert).</summary>
        public const double WW_BEDARF = 700.0;

        /// <summary>Höchste Raumtemperatur [°C] nach dem Muster.</summary>
        public const double RAUMTEMPERATUR_MAX = 24.0;

        private const string QUELLE = "Stein, B.; Loga, T. (2025): Das Typgebäude-Modell zur energetischen Bewertung " +
                                      "des Wohngebäudebestands, IWU im Auftrag des BBSR, Zenodo, doi:10.5281/zenodo.15488271, CC BY 4.0";

        /// <summary>Die sechs Sätze in der Reihenfolge M1, M2, M3, A1, A2, A3.</summary>
        public static readonly IReadOnlyList<GebaeudeSaat> Alle = new[]
        {
            // M1 - Referenzgebaeude nach GEG vom 08.08.2020 (BGBl. I S. 1728), Anlage 1 (zu § 15 Abs. 1),
            //      Fundstelle der Anlage BGBl. I 2020 S. 1767-1768, am Normtext geprueft 26.09.2026
            //      (gesetze-im-internet.de; das Gesetz heisst dort Gebaeudemodernisierungsgesetz - GModG,
            //      zuletzt geaendert durch Art. 4 G v. 23.07.2026, BGBl. 2026 I Nr. 226; Anlage 1
            //      unveraendert): Nr. 1.1 Aussenwand 0,28; Nr. 1.2 Bodenplatte, Waende und Decken zu
            //      unbeheizten Raeumen 0,35; Nr. 1.3 Dach und oberste Geschossdecke 0,20; Nr. 1.4 Fenster
            //      Uw 1,3, g 0,60; Nr. 1.7 Aussentueren 1,8; Nr. 2 dU_WB 0,05 W/(m2K).
            //      Flaechen: Stein/Loga Tab. 27 (S. 67), EZFH freistehend 2021-2025: Dach 113,9 + oGD 35,8;
            //      AW 193,3; Grund 2,7 + 17,7 + 15,1 + 100,1; Fenster 30,6; Tueren 3,4. Wohnflaeche
            //      Tab. 26 (S. 66): 171,8 m2. Raumhoehe 2,5 m nach Muster.
            new GebaeudeSaat("EFH-GEG-Ref", GebaeudeSaattabelle.EFH, "M", null, 171.8, 2.5, 0.6,
                             193.3, 30.6, 149.7, 135.6, 3.4,
                             0.28, 1.30, 0.20, 0.35, 1.80, 0.60,
                             0.05, 0.059, 0.189, 0.283,
                             91.1, 42.9, 42.9,
                             "Einfamilienhaus, Neubau nach dem Referenzgebäude für Wohngebäude des GEG vom 08.08.2020 " +
                             "(BGBl. I S. 1728), Anlage 1 Nr. 1.1 bis 1.7 und 2 (U-Werte, g-Wert, Wärmebrückenzuschlag " +
                             "0,05 W/(m²K)). Flächen und Wohnfläche: Typgebäude EZFH freistehend 2021 bis 2025 nach " +
                             QUELLE + ", Tab. 26 und 27; Anschlusslängen und ψ in EPOS-Plan hergeleitet."),

            // M2 - Effizienzhaus 55: H'T hoechstens 70 % des Referenzgebaeudes (Bundesfoerderung fuer
            //      effiziente Gebaeude, KfW "Das Effizienzhaus", Abruf 26.09.2026). Jeder U-Wert und
            //      dU_WB von M1 x 0,70 - die gleichmaessige Skalierung ist eine benannte EPOS-Annahme;
            //      g wie M1. Flaechen, Wohnflaeche und Laengen wie M1.
            new GebaeudeSaat("EFH-GEG-EH55", GebaeudeSaattabelle.EFH, "M", WindowsFormsApplication1.Energiestandard.EH55,
                             171.8, 2.5, 0.6,
                             193.3, 30.6, 149.7, 135.6, 3.4,
                             0.196, 0.910, 0.140, 0.245, 1.260, 0.60,
                             0.035, 0.041, 0.132, 0.198,
                             91.1, 42.9, 42.9,
                             "Einfamilienhaus, Neubau als Effizienzhaus 55: U-Werte und Wärmebrückenzuschlag des " +
                             "Referenzgebäudes nach GEG Anlage 1 je × 0,70 (Transmissionswärmeverlust H'T höchstens " +
                             "70 % des Referenzgebäudes, Bundesförderung für effiziente Gebäude; die gleichmäßige " +
                             "Skalierung aller Bauteile ist eine Annahme von EPOS-Plan). Flächen und Wohnfläche: " +
                             "Typgebäude EZFH freistehend 2021 bis 2025 nach " + QUELLE + ", Tab. 26 und 27."),

            // M3 - Stein/Loga MFH mit 3 bis 6 Wohnungen, 2021-2025. Tab. 27 (S. 67): Dach 194,1 + oGD 27,7;
            //      AW 326,4; Grund 4,1 + 16,1 + 61,9 + 104,7; Fenster 67,1; Tueren 5,5. Tab. 28 (S. 68):
            //      Dach 0,132, oGD 0,132, AW 0,158, Wand g. Keller/Erdreich, KD und Boden je 0,158,
            //      Fenster 0,951, g 0,550, Tueren 0,951. Tab. 34 (S. 74): dU_WB 0,031. Tab. 26 (S. 66):
            //      Wohnflaeche 366,6 m2. Raumhoehe 2,7 m nach Muster der kleinen MFH.
            new GebaeudeSaat("KMH-GEG-typ", GebaeudeSaattabelle.KMH, "M", null, 366.6, 2.7, 0.6,
                             326.4, 67.1, 221.8, 186.8, 5.5,
                             0.158, 0.951, 0.132, 0.158, 0.951, 0.55,
                             0.031, 0.041, 0.131, 0.196,
                             199.8, 51.6, 51.6,
                             "Kleines Mehrfamilienhaus (3 bis 6 Wohnungen), Baujahr 2021 bis 2025: Typgebäude nach " +
                             QUELLE + ", Tab. 26 bis 28 und 34 (Referenzjahr 2025). U-Werte flächengewichtet, " +
                             "Anschlusslängen und ψ aus dem Wärmebrückenzuschlag in EPOS-Plan hergeleitet."),

            // A1 - Stein/Loga EZFH freistehend bis 1918, Urzustand. Tab. 27 (S. 67): Dach 69,9 + oGD 55,0;
            //      AW 227,8; Grund 1,9 + 11,5 + 54,9 + 48,5; Fenster 27,0; Tueren 3,2. Tab. 28 (S. 68):
            //      Dach 1,318, oGD 1,100, AW 1,372, Wand g. Keller 1,372, Wand g. Erdreich 1,372, KD 1,015,
            //      Boden 1,015, Fenster 3,492, g 0,594, Tueren 3,492. Tab. 34 (S. 74): dU_WB nicht
            //      modernisiert 0,016. Tab. 26 (S. 66): Wohnflaeche 152,5 m2. Quellklasse "bis 1918"
            //      gilt auch fuer Klasse B.
            new GebaeudeSaat("EFH-bis1859-U", GebaeudeSaattabelle.EFH, "A", null, 152.5, 2.5, 0.7,
                             227.8, 27.0, 124.9, 116.8, 3.2,
                             1.372, 3.492, 1.222, 1.056, 3.492, 0.594,
                             0.016, 0.020, 0.063, 0.095,
                             80.4, 40.7, 40.7,
                             "Einfamilienhaus, Baujahr bis 1859, Urzustand: Typgebäude EZFH freistehend bis 1918 nach " +
                             QUELLE + ", Tab. 26 bis 28 und 34 (Referenzjahr 2025, nicht modernisierter Zustand); die " +
                             "Quelle unterscheidet vor 1918 nicht weiter (dieselbe Quellklasse wie Klasse B). U-Werte " +
                             "flächengewichtet, Anschlusslängen und ψ in EPOS-Plan hergeleitet."),

            // A2 - Stein/Loga MFH mit 3 bis 6 Wohnungen bis 1918, Urzustand. Tab. 27 (S. 67): Dach 130,9 +
            //      oGD 52,5; AW 310,0; Grund 2,6 + 15,0 + 88,1 + 48,1; Fenster 55,2; Tueren 4,8. Tab. 28
            //      (S. 68): wie A1, aber Fenster und Tueren 3,545. Tab. 34 (S. 74): dU_WB 0,016.
            //      Tab. 26 (S. 66): Wohnflaeche 303,4 m2. Raumhoehe 2,65 m nach Muster der kleinen MFH.
            new GebaeudeSaat("KMH-bis1859-U", GebaeudeSaattabelle.KMH, "A", null, 303.4, 2.65, 0.7,
                             310.0, 55.2, 183.4, 153.8, 4.8,
                             1.372, 3.545, 1.256, 1.056, 3.545, 0.594,
                             0.016, 0.021, 0.067, 0.101,
                             164.4, 46.7, 46.7,
                             "Kleines Mehrfamilienhaus (3 bis 6 Wohnungen), Baujahr bis 1859, Urzustand: Typgebäude " +
                             "bis 1918 nach " + QUELLE + ", Tab. 26 bis 28 und 34 (Referenzjahr 2025, nicht " +
                             "modernisierter Zustand); dieselbe Quellklasse wie Klasse B. U-Werte flächengewichtet, " +
                             "Anschlusslängen und ψ in EPOS-Plan hergeleitet."),

            // A3 - Stein/Loga EZFH freistehend bis 1918, anteilig modernisierter Bestand 2025 OHNE
            //      Energiestandard. Flaechen wie A1. Tab. 33 (S. 73): Dach 0,708, oGD 0,470, AW 1,082,
            //      Wand g. Keller 0,790, Wand g. Erdreich 1,082, KD 0,704, Boden 0,908, Fenster 2,075,
            //      g 0,594, Tueren 2,075. Tab. 34 (S. 74): dU_WB gesamt 0,040.
            new GebaeudeSaat("EFH-bis1859-TS", GebaeudeSaattabelle.EFH, "A", null, 152.5, 2.5, 0.7,
                             227.8, 27.0, 124.9, 116.8, 3.2,
                             1.082, 2.075, 0.603, 0.827, 2.075, 0.594,
                             0.040, 0.049, 0.158, 0.236,
                             80.4, 40.7, 40.7,
                             "Einfamilienhaus, Baujahr bis 1859, anteilig modernisiert (Bestand 2025): Typgebäude EZFH " +
                             "freistehend bis 1918 nach " + QUELLE + ", Tab. 26, 27, 33 und 34 (flächengewichtete " +
                             "U-Werte aus nicht modernisiertem und modernisiertem Anteil); dieselbe Quellklasse wie " +
                             "Klasse B. Anschlusslängen und ψ in EPOS-Plan hergeleitet."),
        };
    }
}
