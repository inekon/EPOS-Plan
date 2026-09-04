namespace EPOS.UI.Bausteine;

/// <summary>
/// Der ZUSTAND einer <c>Kachel</c> — der Statuspunkt oben in der Karte
/// (iU9-W16a.2, Befund W16-B7).
///
/// <para><b>Wozu.</b> <c>Kachel.razor</c> kannte bis hierher nur EINEN Status: einen
/// Text; stand er da, erschien ein gruener Punkt, sonst gar keiner. Der
/// Komponentenschritt des Assistenten braucht ZWEI Zustaende — gruen „im Projekt",
/// grau „nicht im Projekt", der Punkt in beiden Faellen sichtbar
/// (<c>Wizard_Komponenten.KachelZeichnen</c>: <c>StatusSichtbar = true</c>,
/// <c>StatusFarbe = an ? KartenStil.KARTE_STATUS : KartenStil.KARTE_RAHMEN</c>).</para>
///
/// <para><b>Befund W16a-B1 — „nur Anzeige" ist KEIN dritter Zustand.</b> Die
/// Vermessung schlug <c>Aus</c>/<c>An</c>/<c>NurAnzeige</c> als Dreiersatz vor. Der
/// Bestand fuehrt aber ZWEI unabhaengige Achsen: die Farbe des Punktes haengt allein
/// am Bestand (<c>an</c>), und die Anklickbarkeit allein daran, ob die Komponente
/// eine Assistentenseite hat (<c>OHNE_SEITE</c>). Brauchwasser und Pufferspeicher
/// sind deshalb „nur Anzeige" UND gruen oder grau — ein Dreiersatz koennte das nicht
/// ausdruecken, ohne die Farbe des Bestands zu aendern. Die zweite Achse traegt
/// darum <c>Kachel.Aktiv</c>, und dieser Satz bleibt bei zwei Werten.</para>
/// </summary>
public enum Kachelstand
{
    /// <summary>Nicht im Projekt — grauer Punkt (<c>KartenStil.KARTE_RAHMEN</c>).</summary>
    Aus,

    /// <summary>Im Projekt — gruener Punkt (<c>KartenStil.KARTE_STATUS</c>).</summary>
    An
}
