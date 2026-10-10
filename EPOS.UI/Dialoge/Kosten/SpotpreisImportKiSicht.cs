namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild des <see cref="SpotpreisImportDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Importdialoge, Teil A): Bezeichnung und Ablage der Preisreihe.
/// </summary>
/// <remarks>
/// Beide Werte sind private Felder des Dialogs. Die Sicht hält keinen Zustand: Jede Eigenschaft
/// ruft ihren Delegaten, und der Setzer geht denselben Weg wie das Bedienelement. Datei wählen und
/// „Übernehmen" (schreibt die Reihe in die Datenbank) bleiben Handlungen des Anwenders.
/// </remarks>
public sealed class SpotpreisImportKiSicht
{
    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }
    public Func<bool>? StammLesen { get; init; }
    public Action<bool>? StammSetzen { get; init; }

    /// <summary>Der Name, unter dem die Preisreihe abgelegt wird.</summary>
    public string Bezeichnung
    {
        get => BezeichnungLesen?.Invoke() ?? "";
        set => (BezeichnungSetzen ?? throw new InvalidOperationException(nameof(Bezeichnung)))(value ?? "");
    }

    /// <summary>Steht die Preisreihe allen Projekten zur Verfügung?</summary>
    public bool FuerAlleProjekte
    {
        get => StammLesen?.Invoke() ?? false;
        set => (StammSetzen ?? throw new InvalidOperationException(nameof(FuerAlleProjekte)))(value);
    }
}
