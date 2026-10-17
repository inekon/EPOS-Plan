namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>„Klimaregion wählen…“ im Gebäudedialog</b> (Nachzug N5, Gruppe 4 des Blatts „Modell prüfen“): die Klimaregion des
/// Projekts aus derselben Liste wie die Kopfleiste und der Projektassistent (<c>StartseiteCtrl.KlimaregionAuswahlzeilen</c>,
/// #527). Keine Datenbank: Liste, Vorbelegung und Speicherweg kommen aus der Hülle (<c>GebaeudeHuelle.KlimaregionWeg</c>).
/// </summary>
public sealed class GebaeudeKlimaregionWeg
{
    /// <summary>Die wählbaren Regionen als (Stamm-Id, Anzeigetext) — die Einträge der Kopfleiste.</summary>
    public IReadOnlyList<(int Id, string Text)> Eintraege { get; init; } = Array.Empty<(int, string)>();

    /// <summary>Die Stamm-Id der Region des Projekts; <c>null</c> = das Projekt führt keine.</summary>
    public int? Gewaehlt { get; init; }

    /// <summary>
    /// Schreibt die Region (Stamm-Id) an das Projekt — derselbe Weg wie die Kopfleiste (<c>StartseiteCtrl.KlimaregionSpeichern</c>)
    /// samt Nachziehen des Projektkontexts; Rückgabe <c>null</c> = gespeichert, sonst der benannte Grund.
    /// </summary>
    public Func<int, string?> Speichern { get; init; } = _ => null;

    /// <summary>
    /// Die Herkunftszeile einer Region (Stamm-Id) unter der Liste — derselbe Satz wie unter der Klappliste der Kopfleiste und im
    /// Projektassistenten (<c>KlimaHerkunftGaben.Zeile</c>); leer ohne Auskunft.
    /// </summary>
    public Func<int, string> Herkunftszeile { get; init; } = _ => "";
}
