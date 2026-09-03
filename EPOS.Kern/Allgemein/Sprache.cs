namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die eingestellte Oberflächensprache als Kernwert (Umsetzungskonzept iU4,
    /// Schritt 1).
    ///
    /// <para><b>Warum eine eigene Klasse.</b> Die Berichtstexte entscheiden über
    /// <c>Program.nLanguage</c>, ob sie deutsch oder englisch ausgeben. Damit hing
    /// eine reine Rechen-/Textentscheidung an <c>Program</c> — der Klasse mit dem
    /// WinForms-Einstiegspunkt. Hier steht derselbe Wert ohne Oberfläche.</para>
    ///
    /// <para><b>Wer setzt.</b> Genau eine Stelle: <c>Program.Main</c> liest die
    /// Registry (<c>Software\wp-plan</c>, Wert <c>Language</c>) und schreibt das
    /// Ergebnis über die Weiterleitung <c>Program.nLanguage</c> hierher. Ohne
    /// Oberfläche bleibt die Vorbelegung 0 = deutsch stehen.</para>
    /// </summary>
    public static class Sprache
    {
        /// <summary>Sprachnummer der Oberfläche: 0 = deutsch, 1 = englisch.</summary>
        public static int Nummer = 0;

        /// <summary>
        /// <c>true</c>, sobald eine andere Sprache als Deutsch eingestellt ist.
        /// Bewusst „ungleich 0" und nicht „gleich 1", weil die vorhandenen Leser es
        /// unterschiedlich prüften; die strengere Fassung stünde sonst gegen die
        /// mildere.
        /// </summary>
        public static bool Englisch
        {
            get { return Nummer != 0; }
        }
    }
}
