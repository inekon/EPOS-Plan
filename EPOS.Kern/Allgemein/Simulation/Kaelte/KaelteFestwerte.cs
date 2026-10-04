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
    }
}
