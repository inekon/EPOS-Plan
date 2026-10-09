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
    }
}
