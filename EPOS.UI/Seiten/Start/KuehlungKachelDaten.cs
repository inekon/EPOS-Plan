namespace EPOS.UI.Seiten.Start;

/// <summary>Eine Kältemaschine des Projekts auf der Kachel „Kühlung“: Name der Anlage und Anzahl gleicher Maschinen.</summary>
public sealed record KuehlKaeltemaschine(string Name, int Anzahl);

/// <summary>
/// Eine Wärmepumpe des Projekts mit Kühlfunktion auf der Kachel „Kühlung“: die Projektkopie
/// (<c>Tab_WP.ID</c>), ihr Name, ob sie im Kühlbetrieb mitläuft, und warum sich der Kühlbetrieb
/// nicht einschalten lässt (<c>null</c> = frei).
/// </summary>
public sealed record KuehlWaermepumpe(int IdWp, string Name, bool Kuehlbetrieb, string? Sperrgrund);

/// <summary>
/// Der Bestand der Kachel „Kühlung“ im Reiter Energieerzeuger der Startseite — gebaut von der Hülle
/// aus den Kern-Controllern (<c>KuehlungKachelBau</c>), die Oberfläche liest keine Datenbank.
/// </summary>
public sealed class KuehlungKachelDaten
{
    /// <summary>Die Projekteinstellung „Kühlung rechnen“ (Simulation › Konfiguration).</summary>
    public bool KuehlungRechnen { get; init; }

    /// <summary>Die Kältemaschinen des Projekts in Anzeigereihenfolge.</summary>
    public IReadOnlyList<KuehlKaeltemaschine> Kaeltemaschinen { get; init; } = Array.Empty<KuehlKaeltemaschine>();

    /// <summary>Die Wärmepumpen des Projekts mit Kühlfunktion — Geräte ohne Kühlkennlinie fehlen.</summary>
    public IReadOnlyList<KuehlWaermepumpe> Waermepumpen { get; init; } = Array.Empty<KuehlWaermepumpe>();

    /// <summary>
    /// KM4-E3-b (Konzept Kältemittel als Stammdatum 5.2, 7.5): Zahl der Kälteerzeuger mit Kältemittelwarnung (ab einem
    /// Stichtag nur Restbestand oder Ausnahme). Nur mit gesetztem Inbetriebnahmejahr größer 0.
    /// </summary>
    public int KaeltemittelWarnungen { get; init; }

    /// <summary>KM4-E3-b: Zahl der Kälteerzeuger, deren Kältemittel „nicht prüfbar“ ist; nur mit gesetztem Jahr größer 0.</summary>
    public int KaeltemittelNichtPruefbar { get; init; }

    /// <summary>Ist die Kachel leer — keine Kältemaschine und keine kühlfähige Wärmepumpe?</summary>
    public bool Leer => Kaeltemaschinen.Count == 0 && Waermepumpen.Count == 0;
}
