namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die sprachneutralen <b>Sprungziele innerhalb einer Maske</b> — das Argument zu
    /// <see cref="INavigation.OeffneMaske"/> neben dem Maskenschlüssel aus <see cref="Masken"/>
    /// (Konzept Kältebedarf 3.5). Ein unbekanntes Ziel öffnet die Maske wie ohne Ziel.
    /// </summary>
    public static class Sprungziele
    {
        /// <summary>
        /// Die Gruppe „Kühlung“ des Projektgebäudes: Der Gebäudedialog wählt das erste Projektgebäude, öffnet
        /// dessen Editor auf dem Reiter „Gebäude und Hülle“, rollt die Gruppe in Sicht und fokussiert ihr erstes
        /// Feld; ohne Projektgebäude öffnet er normal mit dem Banner „Noch kein Gebäude im Projekt“.
        /// </summary>
        public const string Kuehlung = "KUEHLUNG";
    }
}
