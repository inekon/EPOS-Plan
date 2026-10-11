using EPOS.UI.Bausteine;

namespace EPOS.UI.Seiten.Start;

/// <summary>
/// Der Stand der Kachel „Gebäudekühlung“ im Reiter „Kältebedarf“ der Startseite (Konzept Kältebedarf 3.5) —
/// gebaut von der plattformfreien Hülle <c>GebaeudekuehlungKachelBau</c> aus Kern-Controllern; die Oberfläche
/// liest keine Datenbank.
/// </summary>
/// <param name="Kuehlbetrieb">Die Projekteinstellung „Kühlung rechnen“ (<c>Tab_Einstellungen.Kuehlbetrieb</c>).</param>
/// <param name="Gekuehlt">Projektgebäude mit „Gebäude wird gekühlt“ (<c>Kuehlung_Aktiv = 1</c>).</param>
/// <param name="Gebaeude">Alle Projektgebäude.</param>
public sealed record GebaeudekuehlungDaten(bool Kuehlbetrieb, int Gekuehlt, int Gebaeude)
{
    /// <summary>Grün, sobald ein Projektgebäude gekühlt wird — der Statuspunkt der Kachel.</summary>
    public Kachelstand Zustand => Gekuehlt > 0 ? Kachelstand.An : Kachelstand.Aus;
}
