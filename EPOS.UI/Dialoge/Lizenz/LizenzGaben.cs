namespace EPOS.UI.Dialoge.Lizenz;

/// <summary>
/// Das Lagebild der Lizenz, wie es die Oberflaeche zeigt (iU9-W15c.5).
///
/// <para><b>Was hier steht und was nicht.</b> Fuenf Anzeigewerte — mehr braucht die
/// Verwaltung nicht. <b>Kein Token, kein RohJson, kein Zeitanker, kein
/// Lizenzschluessel</b> (Sicherheitsregel S-3 der Welle): Was die Komponente bekommt,
/// steht im DOM und laesst sich zur Laufzeit ansehen. Der Zustand ist eine
/// ZEICHENKETTE und nicht der Kern-Aufzaehlungstyp <c>LizenzStatus</c> — dieselbe
/// Linie wie <c>Seitenschluessel</c> gegenueber <c>Masken</c>.</para>
///
/// <para>Gebaut wird der Satz in der Huelle aus <c>LizenzCtrl</c>; die Komponente holt
/// ihn nach jeder Aktion ueber den Delegaten <c>Auffrischen</c> neu — genau wie
/// <c>StatusAnzeigen()</c> im Vorlaeufer nach <c>Aktivieren_Click</c> und
/// <c>Freigeben_Click</c> lief.</para>
/// </summary>
/// <param name="Zustand">
/// Sprachneutraler ASCII-Schluessel: <c>GUELTIG</c>, <c>KULANZ</c>,
/// <c>NACHPRUEFUNG</c>, <c>LESEMODUS</c>, <c>UHRMANIPULIERT</c> oder
/// <c>NICHTAKTIVIERT</c>. Er steuert die Stufe der Statusanzeige, nicht ihren Text.
/// </param>
/// <param name="Statustext">Der Satz zum Zustand (<c>LIZ_ST_*</c>).</param>
/// <param name="Detailtext">Lizenz, Firma, Benutzer und Geraet (<c>LIZ_DETAIL</c>).</param>
/// <param name="HatToken">
/// Liegt ein signaturgeprueftes Token vor? Steuert die Bedienbarkeit von
/// „Geraet loesen" (nur MIT) und „Testversion anfordern" (nur OHNE).
/// </param>
/// <param name="PortalUrl">Adresse des Lizenzportals fuer den Verweis in der Statusgruppe.</param>
public sealed record LizenzGaben(
    string Zustand,
    string Statustext,
    string Detailtext,
    bool HatToken,
    string PortalUrl)
{
    /// <summary>Ein leeres Lagebild — die Vorbelegung, solange die Huelle nichts geliefert hat.</summary>
    public static LizenzGaben Leer { get; } = new("NICHTAKTIVIERT", "", "", false, "");
}
