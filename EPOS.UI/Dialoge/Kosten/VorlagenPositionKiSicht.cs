using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild des Zeileneditors „Kostenposition" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell.</b> Der Editor hält seinen Stand in sechs
/// privaten Feldern; <c>VorlagenPositionErgebnis</c> entsteht erst beim OK und ist
/// unveränderlich.</para>
///
/// <para><b>Zwei WAHLFELDER mit verschiedenen Schlüsseln</b> (KI‑D‑Q6): Die
/// KOSTENART trägt den Listenplatz der VDI‑2067-Liste, die POSITIONSART die Id des
/// Nutzungsdauersatzes. Beide setzt der Assistent über ihren Anzeigetext, nie über
/// eine rohe Zahl.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class VorlagenPositionKiSicht
{
    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }

    public Func<int?>? KostenartLesen { get; init; }
    public Action<int?>? KostenartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KostenartEintraege { get; init; }

    public Func<bool>? ErloesLesen { get; init; }
    public Action<bool>? ErloesSetzen { get; init; }

    public Func<int?>? PositionsartLesen { get; init; }
    public Action<int?>? PositionsartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? PositionsartEintraege { get; init; }

    public Func<double?>? VonLesen { get; init; }
    public Action<double?>? VonSetzen { get; init; }
    public Func<double?>? BisLesen { get; init; }
    public Action<double?>? BisSetzen { get; init; }

    /// <summary>Die Kostenarten nach VDI 2067 — Schlüssel ist ihr Listenplatz.</summary>
    public IReadOnlyList<KiWahleintrag> KostenartWahl
        => KostenartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Positionsarten der Technik — Schlüssel ist ihre Id.</summary>
    public IReadOnlyList<KiWahleintrag> PositionsartWahl
        => PositionsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bezeichnung der Position.</summary>
    public string Bezeichnung
    {
        get => BezeichnungLesen?.Invoke() ?? "";
        set => BezeichnungSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Kostenart nach VDI 2067.</summary>
    public int? Kostenart
    {
        get => KostenartLesen?.Invoke();
        set => KostenartSetzen?.Invoke(value);
    }

    /// <summary>Ist die Position ein ERLÖS und keine Kosten?</summary>
    public bool IstErloes
    {
        get => ErloesLesen?.Invoke() ?? false;
        set => ErloesSetzen?.Invoke(value);
    }

    /// <summary>Die Positionsart, aus der die Nutzungsdauer kommt.</summary>
    public int? Positionsart
    {
        get => PositionsartLesen?.Invoke();
        set => PositionsartSetzen?.Invoke(value);
    }

    /// <summary>Die untere Grenze des Empfehlungsbereichs.</summary>
    public double? EmpfehlungVon
    {
        get => VonLesen?.Invoke();
        set => VonSetzen?.Invoke(value);
    }

    /// <summary>Die obere Grenze des Empfehlungsbereichs.</summary>
    public double? EmpfehlungBis
    {
        get => BisLesen?.Invoke();
        set => BisSetzen?.Invoke(value);
    }
}
