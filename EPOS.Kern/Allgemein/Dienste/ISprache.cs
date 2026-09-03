namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Oberflächensprache. Fünf Leser und genau eine Setzstelle
    /// (Vermessung iU5, Abschnitt A.5).
    ///
    /// <para><b>Der Textkatalog ist NICHT betroffen.</b> Die rund 3.000 Zugriffe auf
    /// <c>MyResource.Resource.*</c> laufen über <c>ResourceManager.GetString</c> mit
    /// <c>resourceCulture == null</c> und damit über <c>CurrentUICulture</c> — sie sind
    /// bereits plattformfrei. Diese Schnittstelle setzt nur die Kultur und ersetzt die
    /// fünf Vergleiche auf <c>Program.nLanguage</c>.</para>
    ///
    /// <para><b>Verhältnis zu <see cref="Sprache"/>.</b> <c>Sprache.Nummer</c> bleibt der
    /// gehaltene Wert; <c>Program.nLanguage</c> bleibt die Weiterleitung dorthin für die
    /// Masken. Diese Schnittstelle ist die Sicht des Kerns darauf.</para>
    /// </summary>
    public interface ISprache
    {
        /// <summary>Sprachkürzel der Oberfläche: <c>"de"</c> oder <c>"en"</c>.</summary>
        string Kuerzel { get; }

        /// <summary>
        /// <c>true</c>, sobald eine andere Sprache als Deutsch eingestellt ist. Ersetzt
        /// alle fünf Vergleiche auf <c>Program.nLanguage</c>; bewusst „ungleich Deutsch"
        /// und nicht „gleich Englisch", weil die Leser es unterschiedlich prüften.
        /// </summary>
        bool IstEnglisch { get; }

        /// <summary>
        /// Sprache umstellen: Kennnummer, <c>CurrentUICulture</c> und
        /// <c>DefaultThreadCurrentUICulture</c>. Die Windows-Fassung schreibt zusätzlich
        /// den Registry-Wert, aus dem der nächste Start liest.
        /// </summary>
        /// <param name="kuerzel"><c>"de"</c> oder <c>"en"</c>; alles andere gilt als Deutsch.</param>
        void Setzen(string kuerzel);
    }
}
