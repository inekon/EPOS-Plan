namespace EPOS.UI.Bausteine;

/// <summary>
/// Wie genau eine Platzhaltermarke das Element trifft, an dem sie steht (Konzept
/// Berichtsvorlagen 9.4, 9.5): Der Schlüssel erzeugt genau den angezeigten Wert — oder im
/// Bericht etwas Ähnliches (Kuchen statt Ring, Speicherverlauf statt Speicherbetrieb).
/// </summary>
public enum Vorlagenfeldstufe
{
    /// <summary>Der Schlüssel erzeugt im Bericht genau das, was hier steht.</summary>
    Entspricht,

    /// <summary>Im Bericht erscheint Ähnliches; der Hinweis der Marke nennt den Unterschied.</summary>
    Aehnlich,
}
