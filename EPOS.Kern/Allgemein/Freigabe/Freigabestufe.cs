namespace WindowsFormsApplication1
{
    /// <summary>
    /// Sichtbarkeitsstufe der Oberfläche (Konzept Administration und Support, Abschnitt 3.1).
    /// Kein Rechtemodell: Die Stufe entscheidet nur, welche Bereiche gezeigt werden. Die
    /// gültige Stufe des Laufs liefert <see cref="Entwicklermodus.Stufe"/>.
    /// </summary>
    public enum Freigabestufe
    {
        /// <summary>Was jeder Anwender sieht.</summary>
        Anwender = 0,

        /// <summary>Dazu die Entwicklerbereiche aus <see cref="Entwicklerbereiche.Alle"/>.</summary>
        Entwickler = 1,
    }
}
