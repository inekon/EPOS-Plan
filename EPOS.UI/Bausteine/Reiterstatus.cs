namespace EPOS.UI.Bausteine;

/// <summary>
/// Die Stufe der Statuszeile eines <see cref="Reiterblatt"/>s (Konzept Navigation
/// Berichte &amp; Kosten, Etappe A2).
/// </summary>
public enum Statusstufe
{
    /// <summary>Ein leiser Stand — leise Schrift, kein Zeichen.</summary>
    Normal,

    /// <summary>
    /// Ein Stand, den der Anwender beachten sollte: Warnfarbe (<c>--epos-warn-text</c>) und
    /// davor das Zeichen ▲ — die Farbe allein trägt die Aussage nicht (Hochkontrast).
    /// </summary>
    Warnung
}

/// <summary>
/// Der Kurzstand eines Reiterblatts, wie ihn ein Wirt je Blatt hereinreicht: der Text der
/// Statuszeile, seine Kurzform für schmale Fenster (unter 900 px) und die Stufe.
/// </summary>
/// <param name="Text">Die Statuszeile; leer = keine.</param>
/// <param name="Kurz">Die Kurzform unter 900 px; leer = derselbe Text.</param>
/// <param name="Stufe">Normal oder Warnung.</param>
public sealed record Reiterstatus(string Text, string Kurz = "", Statusstufe Stufe = Statusstufe.Normal);
