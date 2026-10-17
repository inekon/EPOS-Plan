using KiKern;

namespace EPOS.UI.Dialoge.Projekt;

/// <summary>
/// Das FLACHE Abbild des Importblatts im <see cref="ProjektTransferDialog"/> für den
/// Hilfe-Assistenten (Freigabe der Masken, Teil C): Zielname, Umgang mit einem vorhandenen
/// Namen und die Sicherung vor dem Import.
/// </summary>
/// <remarks>
/// Die drei Werte sind private Felder des Dialogs. Die Sicht hält keinen Zustand: Jede
/// Eigenschaft ruft ihren Delegaten, und der Setzer geht denselben Weg wie das Bedienelement.
/// Dateiwahl, Projektliste des Exports, „Exportieren…" und „Importieren…" (schreibt Projekte in
/// die Datenbank, beim Überschreiben erst nach der Rückfrage) bleiben Handlungen des Anwenders.
/// </remarks>
public sealed class ProjektTransferKiSicht
{
    public Func<string>? ZielnameLesen { get; init; }
    public Action<string>? ZielnameSetzen { get; init; }
    public Func<int>? KonfliktLesen { get; init; }
    public Action<int>? KonfliktSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KonfliktEintraege { get; init; }
    public Func<bool>? SicherungLesen { get; init; }
    public Action<bool>? SicherungSetzen { get; init; }

    /// <summary>Der Name, unter dem das Paket eingespielt wird; leer = der Name aus der Datei.</summary>
    public string Zielname
    {
        get => ZielnameLesen?.Invoke() ?? "";
        set => (ZielnameSetzen ?? throw new InvalidOperationException(nameof(Zielname)))(value ?? "");
    }

    /// <summary>Die Wahl des Feldes <c>Konflikt</c> — dieselben drei Modi wie die Optionsgruppe.</summary>
    public IReadOnlyList<KiWahleintrag> KonfliktWahl
        => KonfliktEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Was geschieht, wenn der Name schon vergeben ist (neuer Name, überschreiben, abbrechen).</summary>
    public int Konflikt
    {
        get => KonfliktLesen?.Invoke() ?? 0;
        set => (KonfliktSetzen ?? throw new InvalidOperationException(nameof(Konflikt)))(value);
    }

    /// <summary>Wird vor dem Import eine Sicherungskopie der Datenbank angelegt?</summary>
    public bool Sicherung
    {
        get => SicherungLesen?.Invoke() ?? false;
        set => (SicherungSetzen ?? throw new InvalidOperationException(nameof(Sicherung)))(value);
    }
}
