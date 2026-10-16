namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lädt das Energieflussbild einer Version (Konzept Energieflussbild, Abschnitt 2.3): dieselben Daten wie der
    /// Energievergleich der Übersicht (<see cref="EnergievergleichCtrl.Lade"/> — gespeicherter Ergebnisbaum,
    /// Brennstoffmengen, Trägernachweise), ohne zu simulieren; den Flussbau trifft <see cref="Energiefluss.Aus(VariantenDaten, bool, string)"/>.
    /// </summary>
    public static class EnergieflussCtrl
    {
        /// <summary>
        /// Das Flussmodell der Version <paramref name="idProjekt"/>; ohne gespeicherten Lauf ein Modell ohne Knoten mit
        /// benanntem <see cref="EnergieflussModell.Grund"/>. <paramref name="versionsname"/> leer = Name aus den Daten.
        /// </summary>
        public static EnergieflussModell Lade(int idProjekt, bool istStamm, bool englisch, string versionsname = null)
        {
            VariantenDaten v = EnergievergleichCtrl.Lade(idProjekt, istStamm);
            return Energiefluss.Aus(v, englisch, string.IsNullOrWhiteSpace(versionsname) ? null : versionsname);
        }
    }
}
