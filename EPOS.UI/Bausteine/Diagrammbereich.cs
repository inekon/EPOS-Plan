namespace EPOS.UI.Bausteine;

/// <summary>
/// Der Ausschnitt, den der Anwender in einem Diagramm aufgezogen hat — als
/// ANTEILE DES BILDES, 0 links/oben bis 1 rechts/unten (Windows-Abnahme
/// 05.09.2026, Befund A-1).
///
/// <para><b>Warum Anteile und nicht Stunden.</b> Die Oberfläche kennt weder
/// Jahresstunden noch Kilowatt — sie sieht ein PNG. Wo im BILD das Rechteck
/// liegt, kann sie messen; was dort steht, weiß nur der Renderer, der es
/// gezeichnet hat. Deshalb reicht der Baustein Anteile nach oben, und die
/// Hülle lässt den Kern daraus einen Achsenbereich machen
/// (<c>ChartRenderer.FensterAusBild</c>).</para>
/// </summary>
/// <param name="XVon">Linke Kante, Anteil der Bildbreite.</param>
/// <param name="XBis">Rechte Kante, Anteil der Bildbreite.</param>
/// <param name="YVon">Obere Kante, Anteil der Bildhöhe.</param>
/// <param name="YBis">Untere Kante, Anteil der Bildhöhe.</param>
public sealed record Diagrammbereich(double XVon, double XBis, double YVon, double YBis)
{
    /// <summary>
    /// Der Zwischenspeicherschlüssel des Bereichs — zwei verschiedene
    /// Ausschnitte müssen zwei verschiedene Bilder ergeben. Rundzahlig auf vier
    /// Nachkommastellen: Ein Unterschied von einem Zehntausendstel der Bildbreite
    /// ist kein anderer Ausschnitt, sondern derselbe Zug mit zitternder Hand.
    /// </summary>
    public override string ToString()
        => XVon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + ";" +
           XBis.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + ";" +
           YVon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + ";" +
           YBis.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
}
