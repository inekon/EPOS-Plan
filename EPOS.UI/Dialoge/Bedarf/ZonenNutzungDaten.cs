using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Der Weg des Blatts „Nutzung und Kalender“</b> (MA-2, Entwurf Geführter Modellaufbau 2 und 4): Lesen des Stands aller
/// Zonen, die Profile des Katalogs und die Schreibwege für n gewählte Zonen samt Rücknahme. Gefüllt von der Hülle
/// (<c>ZonenNutzungHuelle</c>); keine Datenbank in der Oberfläche.
/// </summary>
public sealed class ZonenNutzungWeg
{
    /// <summary>Der Name des Gebäudes für den Kopf.</summary>
    public string Gebaeudename { get; init; } = "";

    /// <summary>Liest den Stand; <c>null</c>, wenn das Gebäude nicht zu lesen ist.</summary>
    public Func<ZonenNutzungsstand?> Lesen { get; init; } = () => null;

    /// <summary>Die Profile des Katalogs für „Profil zuweisen…“.</summary>
    public Func<IReadOnlyList<ZonenNutzungsprofil>> Profile { get; init; } = () => Array.Empty<ZonenNutzungsprofil>();

    /// <summary>„Profil zuweisen…“ (Profil-Id) bzw. „Vorschlag übernehmen“ (<c>null</c>) für die gewählten Zonen.</summary>
    public Func<IReadOnlyCollection<int>, long?, ZonenNutzungsergebnis> ProfilZuweisen { get; init; } = (_, _) => Leer;

    /// <summary>„Projektvorgabe Ferien/Heizperiode übernehmen“ für die gewählten Zonen.</summary>
    public Func<IReadOnlyCollection<int>, ZonenNutzungsergebnis> ProjektvorgabeUebernehmen { get; init; } = _ => Leer;

    /// <summary>„Kalender kopieren von…“: Quellzone, gewählte Zonen.</summary>
    public Func<int, IReadOnlyCollection<int>, ZonenNutzungsergebnis> KalenderKopieren { get; init; } = (_, _) => Leer;

    /// <summary>„Sollwerte setzen…“: gewählte Zonen, Tag, Nacht (<c>null</c> = unverändert).</summary>
    public Func<IReadOnlyCollection<int>, double?, double?, ZonenNutzungsergebnis> SollwerteSetzen { get; init; } = (_, _, _) => Leer;

    /// <summary>„Zurücknehmen“: die letzte Mehrfachänderung.</summary>
    public Func<ZonenNutzungsergebnis> Zuruecknehmen { get; init; } = () => Leer;

    private static readonly ZonenNutzungsergebnis Leer = new(false, "", 0, null);
}
