namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Berichtskapitel (Konzept Kap. 8.3). Bausteine schreiben ihren Inhalt über
    /// den WordKontext (Style-basiert) und kennen keine OpenXML-Details des Rahmens.
    /// SchreibeExcel folgt in Phase 4 (eigene Schnittstelle, gleiche Registrierung).
    /// </summary>
    public interface IBerichtsBaustein
    {
        /// <summary>Stabiler Schlüssel (BerichtsKonfiguration.B_*).</summary>
        string Schluessel { get; }

        /// <summary>Kapiteltitel (Berichtssprache; Phase 5: aus Ressourcen).</summary>
        string Titel { get; }

        void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig);
    }
}
