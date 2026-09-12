namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das Ergebnis von <c>GebaeudeWohnflaecheDialog</c> (iU9-W9.3) — die vier Werte, die
/// der Aufrufer in seine Projektzeile zurückschreibt.
///
/// <para><b>Ein Record statt eines Fachobjekts.</b> Der Vorläufer
/// <c>Form_GebWohnflaeche</c> bekam ein <c>Z_ProjGebModel</c> in die Hand und schrieb
/// hinein; die Komponente kennt die Fachklassen des Kerns nicht (Hausregel iU9).</para>
///
/// <para><b>Befund W9‑B3: <see cref="DezentralWarmwasser"/> ist NEU im Ergebnis.</b>
/// <c>btn_OK_Click</c>:42-49 schrieb Wohnfläche, Jahresnutzungsgrad und Einheit zurück —
/// die Checkbox „Dezentrale Warmwasserbereitung" wurde gezeigt, gelesen und dann
/// verworfen, obwohl <c>Z_ProjektGebaeude.dezWarmwasserbereitung</c> gespeichert wird.
/// Ein Anwender, der den Schalter umlegte und mit OK schloss, sah ihn danach wieder auf
/// dem alten Stand. Das ist behoben: Der Schalter geht mit (Abweichung A‑2 im
/// Protokoll W9).</para>
/// </summary>
/// <param name="Wert">Der eingegebene Verbrauch bzw. die Wohnfläche.</param>
/// <param name="Jahresnutzungsgrad">Jahresnutzungsgrad des Kessels (z. B. 0,85).</param>
/// <param name="Einheit">Die gewählte Bedarfsart samt Einheit, z. B. „Wohnfläche [m²]".</param>
/// <param name="DezentralWarmwasser">Dezentrale Warmwasserbereitung.</param>
public sealed record GebaeudeWohnflaecheErgebnis(
    double Wert,
    double Jahresnutzungsgrad,
    string Einheit,
    bool DezentralWarmwasser);
