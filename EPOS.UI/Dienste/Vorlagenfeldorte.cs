namespace EPOS.UI.Dienste;

/// <summary>
/// Ein Ort der App, an dem eine Platzhaltermarke steht (Konzept Berichtsvorlagen 9.4 „In der App
/// zeigen", 9.6).
/// </summary>
/// <param name="Ansicht">Der Maskenschlüssel der Ansicht (<c>Seitenschluessel.*</c>), wie ihn
/// <c>Dienste.Navigation.OeffneMaske</c> nimmt.</param>
/// <param name="Reiter">Das Blatt bzw. die Marke der Ansicht als erstes Argument
/// (<c>"WIRTSCHAFT"</c>, <c>"schritt=3;blatt=…"</c>); leer = die Ansicht entscheidet.</param>
/// <param name="Element">Das Element in Worten („Kachel JAZ Wärmepumpe"), für die Katalogzeile.</param>
/// <param name="NurLesend">Ist das Ziel lesend erreichbar (keine Eingabemaske, kein Assistent)?</param>
public sealed record Vorlagenfeldort(string Ansicht, string Reiter, string Element, bool NurLesend);

/// <summary>
/// <b>Die Tabelle Ort ↔ Schlüssel</b> (Konzept 9.6). STUB der Infrastruktur (BV-E6 W1): Die Tabelle
/// selbst samt Wache baut W2; beim Zusammenführen ersetzt ihre Datei diese. Bis dahin kennt sie
/// keinen Ort.
/// </summary>
public static class Vorlagenfeldorte
{
    /// <summary>Die Orte eines Schlüssels; leer = kein Gegenstück in der App.</summary>
    public static IReadOnlyList<Vorlagenfeldort> Finde(string schluessel) => Array.Empty<Vorlagenfeldort>();
}
