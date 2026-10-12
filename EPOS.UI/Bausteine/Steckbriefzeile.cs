namespace EPOS.UI.Bausteine;

/// <summary>
/// Eine Zeile des <see cref="Geraetesteckbrief"/>: Beschriftung und Wert, wahlweise mit Einheit und einer leisen
/// Herleitungszeile unter dem Wert (WP1a, Anwenderentscheid 12.10.2026: „auf der linken Seite Daten zur ausgewählten
/// Anlage — aus dem Datensatz die wichtigsten Daten").
/// </summary>
/// <param name="Beschriftung">Die Beschriftung (Ressourcentext des Wirts).</param>
/// <param name="Wert">Der fertig formatierte Wert; <c>null</c> oder leer = „—", dann ohne Einheit.</param>
/// <param name="Einheit">Die Einheit hinter dem Wert, etwa „kW"; leer = keine.</param>
/// <param name="Herleitung">Eine leise Zeile unter dem Wert; leer = keine.</param>
/// <param name="Kennung">Ein sprachneutraler Prüfanker (<c>data-zeile</c>); leer = keiner.</param>
public sealed record Steckbriefzeile(string Beschriftung, string? Wert, string Einheit = "", string Herleitung = "",
                                     string Kennung = "")
{
    /// <summary>Der Platzhalter eines fehlenden Werts.</summary>
    public const string FEHLT = "—";

    /// <summary>Ist ein Wert da?</summary>
    public bool HatWert => !string.IsNullOrWhiteSpace(Wert);

    /// <summary>Der Anzeigetext: Wert mit Einheit, ohne Wert „—".</summary>
    public string Anzeige => !HatWert ? FEHLT : Einheit.Length > 0 ? Wert!.Trim() + " " + Einheit : Wert!.Trim();
}
