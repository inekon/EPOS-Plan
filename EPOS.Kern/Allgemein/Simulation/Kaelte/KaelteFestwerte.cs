namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Festwerte des Rechenwegs der Kältemaschine</b> (KU3-2; Kühlkonzept 5.3, 5.4, 6.1).
    ///
    /// <para>Jeder Wert ist benannt und steht genau hier — kein Schema, keine Eingabe. Kommt eine
    /// Eingabe dafür, ersetzt sie den Festwert an genau einer Stelle.</para>
    /// </summary>
    public static class KaelteFestwerte
    {
        /// <summary>Grädigkeit des luftgekühlten Verflüssigers [K]: Rückkühltemperatur = Außentemperatur + 5 K.</summary>
        public const double GRAEDIGKEIT_LUFT_K = 5.0;

        /// <summary>Grädigkeit des Trockenkühlers [K]: Rückkühltemperatur = Außentemperatur + 10 K.</summary>
        public const double GRAEDIGKEIT_TROCKENKUEHLER_K = 10.0;

        /// <summary>Rückkühltemperatur der wassergekühlten Maschine mit fremdem Kühlwasser [°C] — ohne Quelle fest.</summary>
        public const double RUECKKUEHLTEMPERATUR_WASSER_C = 25.0;

        /// <summary>Grädigkeit des Nasskühlers [K] über der Feuchtkugeltemperatur.</summary>
        public const double GRAEDIGKEIT_NASSKUEHLER_K = 5.0;

        /// <summary>
        /// Nasskühler ohne Luftfeuchte im Klima [K]: Rückkühltemperatur = Außentemperatur − 3 K
        /// (mit Hinweis im Protokoll).
        /// </summary>
        public const double NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K = 3.0;

        /// <summary>
        /// Freie Kühlung [K]: Der Rückkühler deckt die Kälte direkt, wenn die Rückkühltemperatur
        /// mindestens so weit unter der Kaltwassertemperatur liegt.
        /// </summary>
        public const double FREIE_KUEHLUNG_ABSTAND_K = 3.0;

        /// <summary>EER-Ersatz der freien Kühlung [—]: Pumpen und Ventilatoren ohne Verdichter.</summary>
        public const double FREIE_KUEHLUNG_EER = 15.0;

        /// <summary>
        /// K-F3 (Entwurf Split/VRF/Rückkühlwerk 5.5): Auslegungsspreizung des Kaltwassers [K] für die Teil-Freikühlung in Reihe,
        /// wenn kein Gebäude des Projekts eine Kühlübergabe pflegt — ohne
        /// gerechneten Rücklauf Bezug des Rücklaufs (Vorlauf + Δθ); der Kaltwasserstrom folgt der Last (ṁ·c_p = Last / Δθ). Quelle: Normbedingung „Kaltwasser 12/7 °C“ der DIN EN 14511-2
        /// (Flüssigkeitskühlsätze, Anwendung Gebläsekonvektor) — Quelle nicht am Wortlaut geprüft (vorläufig).
        /// </summary>
        public const double REIHE_SPREIZUNG_K = 5.0;

        /// <summary>
        /// Freie Kühlung über die Wärmequelle der Wärmepumpe [K] (KU3-6, F1/F3): die Grädigkeit des
        /// Wärmetauschers zwischen Sole bzw. Grundwasser und Kaltwasser, wenn die Anlagenzeile keine
        /// pflegt (<c>Kuehl_Frei_Graedigkeit_K</c> NULL). Frei gekühlt wird, solange die Quellentemperatur
        /// plus Grädigkeit den Kaltwasser-Vorlauf nicht übersteigt.
        /// </summary>
        public const double FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K = 3.0;

        // ---- Teillast und Takten (KM3, Fachkonzept Teillast und Takten 3.5 und 4.4) ----

        /// <summary>
        /// Kontrollwert Nenn-EER [—, relativ]: Weicht der gepflegte Nenn-EER um mehr als diesen Anteil vom EER des
        /// Kennfelds am Eurovent-Nennpunkt ab, meldet <see cref="KaeltemaschineStammCtrl.NennEerHinweis"/> einen Hinweis
        /// (kein Fehler).
        /// </summary>
        public const double NENN_EER_ABWEICHUNG = 0.10;

        // Vorgabekurven je Verdichterregelung (KM3-Q3): abgeleitet aus den Typkennfeldern (PNNL Copper, BSD-2). Je
        // Regelung die auf EIRFPLR(1) = 1 normierte Kurve des Typkennfelds, dessen EER-Verhaeltnis g(0,5) der untere
        // Median der Gruppe ist (nur Saetze mit plausibler, nicht linearer Kurve, also Teillast_Weg KURVE; Gruppen: EIN_AUS Hubkolben und Scroll, STUFEN Schraube und
        // Turbo mit fester Drehzahl, DREHZAHL drehzahlgeregelt). Gehalten von KaeltemaschineTeillastkurveTests.

        /// <summary>Vorgabekurve EIN_AUS, Beiwert a — Copper-Datensatz 315 (g(0,5) = 1,037).</summary>
        public const double VORGABEKURVE_EIN_AUS_A = -0.011227;

        /// <summary>Vorgabekurve EIN_AUS, Beiwert b.</summary>
        public const double VORGABEKURVE_EIN_AUS_B = 0.961893;

        /// <summary>Vorgabekurve EIN_AUS, Beiwert c.</summary>
        public const double VORGABEKURVE_EIN_AUS_C = 0.049334;

        /// <summary>Vorgabekurve STUFEN, Beiwert a — Copper-Datensatz 146 (g(0,5) = 0,985).</summary>
        public const double VORGABEKURVE_STUFEN_A = 0.251864;

        /// <summary>Vorgabekurve STUFEN, Beiwert b.</summary>
        public const double VORGABEKURVE_STUFEN_B = 0.275640;

        /// <summary>Vorgabekurve STUFEN, Beiwert c.</summary>
        public const double VORGABEKURVE_STUFEN_C = 0.472495;

        /// <summary>Vorgabekurve DREHZAHL, Beiwert a — Copper-Datensatz 49 (g(0,5) = 1,306).</summary>
        public const double VORGABEKURVE_DREHZAHL_A = 0.087861;

        /// <summary>Vorgabekurve DREHZAHL, Beiwert b.</summary>
        public const double VORGABEKURVE_DREHZAHL_B = 0.267810;

        /// <summary>Vorgabekurve DREHZAHL, Beiwert c.</summary>
        public const double VORGABEKURVE_DREHZAHL_C = 0.644329;

        /// <summary>
        /// Obere Grenze der Teillaststunden (KM3, Fachkonzept Teillast und Takten 5.3, Vorgabe): eine Verdichterstunde mit
        /// PLR_min ≤ PLR &lt; 0,95 zählt als Teillaststunde.
        /// </summary>
        public const double TEILLASTSTUNDEN_LASTGRAD_GRENZE = 0.95;

        // ---- Rückkühlwerk (K-F2, Entwurf Split/VRF/Rückkühlwerk 5.3): die Vorgaben leerer Katalogfelder ----
        // Alle Quellenangaben dieses Abschnitts sind aus der Fachliteratur zitiert und nicht am Wortlaut geprüft
        // (vorläufig); eine „Annahme“ ist eine eigene Festlegung mit fachlicher Begründung.

        /// <summary>
        /// Vorgabe <c>Befeuchtung_Wirkungsgrad</c> [—]: Sättigungswirkungsgrad der adiabaten Vorkühlung, die Lufteintritts-
        /// temperatur sinkt um diesen Anteil des psychrometrischen Abstands (Trockenkugel − Feuchtkugel). Quelle: ASHRAE
        /// Handbook — HVAC Systems and Equipment, Kapitel „Evaporative Air-Cooling Equipment“ (direkte Verdunstungskühler mit
        /// Benetzungsmatten etwa 70 bis 95 %); 0,8 liegt im unteren Drittel des Bands. Quelle nicht am Wortlaut geprüft (vorläufig).
        /// </summary>
        public const double RKW_BEFEUCHTUNG_WIRKUNGSGRAD = 0.8;

        /// <summary>
        /// Vorgabe <c>Befeuchtung_Ab_C</c> [°C]: Außentemperatur, ab der adiabat befeuchtet (bzw. ein Hybridkühler nass
        /// fährt). Annahme (eigene Festlegung): Mit der trockenen Grädigkeit <see cref="GRAEDIGKEIT_TROCKENKUEHLER_K"/> hält
        /// ein Trockenkühler bis 25 °C Außenluft einen Kühlwasservorlauf von etwa 35 °C, wie ihn wassergekühlte Verflüssiger
        /// üblich erwarten; darüber lohnt die Befeuchtung.
        /// </summary>
        public const double RKW_BEFEUCHTUNG_AB_C = 25.0;

        /// <summary>
        /// Vorgabe <c>Verdunstung_Faktor</c> [—] des Kühlturms: Anteil der Wärme, der als Verdampfung abgeht. Quelle: ASHRAE
        /// Handbook — HVAC Systems and Equipment, Kapitel „Cooling Towers“ (Verdunstung ≈ 0,00085 · Umlauf · Spreizung in °F,
        /// das sind rund 90 % der abgeführten Wärme latent). Quelle nicht am Wortlaut geprüft (vorläufig).
        /// </summary>
        public const double RKW_VERDUNSTUNG_FAKTOR = 0.9;

        /// <summary>
        /// Vorgabe <c>Verdunstung_Faktor</c> [—] der adiabaten und hybriden Befeuchtung: Die Vorkühlung der Luft ist reine
        /// Verdunstung (isenthalp), das verdunstete Wasser trägt die ganze Vorkühlwärme.
        /// </summary>
        public const double RKW_VERDUNSTUNG_FAKTOR_BEFEUCHTUNG = 1.0;

        /// <summary>
        /// Vorgabe <c>Eindickung</c> [—]: Eindickungszahl des Umlauf- bzw. Befeuchtungswassers. Annahme, übliche
        /// Größenordnung nach Fachliteratur (etwa 2 bis 6, z. B. ASHRAE Handbook — HVAC Applications, „Water Treatment“);
        /// 3 ist der vorsichtige Wert ohne Wasseraufbereitung. Quelle nicht am Wortlaut geprüft (vorläufig).
        /// </summary>
        public const double RKW_EINDICKUNG = 3.0;

        /// <summary>
        /// Vorgabe <c>Drift_Anteil</c> [—] des Kühlturms, bezogen auf das Umlaufwasser: 0,01 % — die Anforderung an
        /// Tropfenabscheider nach VDI 2047 Blatt 2 (Verdunstungskühlanlagen; Quelle nicht am Wortlaut geprüft (vorläufig)). Adiabate und hybride Befeuchtung: Vorgabe 0
        /// (Benetzungsmatten und Besprühung ohne nennenswerten Tropfenauswurf), ein gepflegter Wert gilt dort bezogen auf
        /// das verdunstete Wasser.
        /// </summary>
        public const double RKW_DRIFT_ANTEIL = 0.0001;

        /// <summary>
        /// Vorgabe <c>Ventilator_Drehzahl_Min</c> [—]: kleinster Luftanteil des drehzahlgeregelten Ventilators. Annahme,
        /// übliche Größenordnung nach Fachliteratur (Stellbereich drehzahlgeregelter Ventilatoren bis etwa 20 % der
        /// Nenndrehzahl, z. B. Recknagel, Taschenbuch für Heizung und Klimatechnik). Quelle nicht am Wortlaut geprüft (vorläufig).
        /// </summary>
        public const double RKW_VENTILATOR_DREHZAHL_MIN = 0.2;

        /// <summary>Vorgabe <c>Ventilator_Stufen</c> bei Regelung <c>STUFEN</c>: zwei Stufen (das Schema verlangt mindestens 2).</summary>
        public const int RKW_VENTILATOR_STUFEN = 2;

        /// <summary>Verdampfungsenthalpie des Wassers bei rund 20 °C [kJ/kg] (Stoffwert, z. B. VDI-Wärmeatlas: etwa 2 450 kJ/kg; Quelle nicht am Wortlaut geprüft (vorläufig)).</summary>
        public const double RKW_VERDAMPFUNGSENTHALPIE_KJ_KG = 2450.0;

        /// <summary>Spezifische Wärmekapazität des Wassers [kJ/(kg·K)] — Umlaufwasser des Kühlturms.</summary>
        public const double RKW_WASSER_CP_KJ_KGK = 4.19;

        /// <summary>Dichte des Wassers [kg/m³].</summary>
        public const double RKW_WASSER_DICHTE_KG_M3 = 1000.0;

        /// <summary>
        /// Spreizung des Umlaufwassers am Kühlturm [K] — Bezugsgröße der Drift: Umlauf = Wärme / (c_p · Spreizung).
        /// Annahme: übliche Kühlwasserspreizung wassergekühlter Verflüssiger (etwa 30/35 °C).
        /// </summary>
        public const double RKW_UMLAUF_SPREIZUNG_K = 5.0;

        /// <summary>
        /// Lufterwärmung über den Wärmeübertrager des adiabaten bzw. hybriden Kühlers [K] — Bezugsgröße der Befeuchtung:
        /// Luftstrom · c_p = abgeführte Wärme / Lufterwärmung, der Luftstrom folgt der Last. Annahme: Im üblichen Auslegungspunkt
        /// eines Trockenkühlers (Lufteintritt um 25 °C, Fluid um 40/35 °C) erwärmt sich die Luft um rund 10 K.
        /// </summary>
        public const double RKW_LUFTERWAERMUNG_K = 10.0;
    }
}
