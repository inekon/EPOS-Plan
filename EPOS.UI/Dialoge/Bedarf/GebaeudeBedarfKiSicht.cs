using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Maske „Wärmebedarf eines Gebäudes" für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>GebaeudeBedarfDaten</c>.</b> Jener Satz
/// trägt das eingefrorene Ergebnis und nur dieses; die ZWEI Bedienelemente der Maske —
/// die Anzeigeeinheit und der Schalter „sortiert" — liegen in ihren lebenden Feldern.
/// Ein am Satz angemeldeter Katalog hätte genau die zwei Werte nicht geführt, die der
/// Anwender hier einstellen kann.</para>
///
/// <para><b>Die Kennzahlen stehen in MWh und kW</b>, so wie der Rechenkern sie liefert.
/// Welche Einheit die Maske gerade ZEIGT, sagt das Wahlfeld daneben.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class GebaeudeBedarfKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? EinheitLesen { get; init; }
    public Action<int?>? EinheitSetzen { get; init; }

    public Func<bool>? SortiertLesen { get; init; }
    public Action<bool>? SortiertSetzen { get; init; }

    public Func<GebaeudeBedarfDaten?>? SatzLesen { get; init; }

    public Func<int?>? DiagrammLesen { get; init; }
    public Action<int?>? DiagrammSetzen { get; init; }

    /// <summary>Liefert die Einträge der Diagrammwahl (Stufe G6b): das Gebäude und seine Zonen.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? DiagrammEintraege { get; init; }

    /// <summary>Die Einträge der Diagrammwahl; leer bei einem Gebäude mit höchstens einer Zone.</summary>
    public IReadOnlyList<KiWahleintrag> DiagrammWahl
        => DiagrammEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Liefert die Energieeinheiten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? EinheitEintraege { get; init; }

    /// <summary>Die Energieeinheiten der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl
        => EinheitEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    private GebaeudeBedarfDaten? Satz => SatzLesen?.Invoke();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die Anzeigeeinheit der Energiemengen als Platz in der Klappliste; sie wirkt auf
    /// die Kennzahlen, die Monatsübersicht und das Bild zugleich.
    /// </summary>
    public int? Einheit
    {
        get => EinheitLesen?.Invoke();
        set => EinheitSetzen?.Invoke(value);
    }

    /// <summary>
    /// Zeigt das Bild die DAUERLINIE statt der Jahresganglinie? Sortiert heißt: die
    /// 8 760 Stundenwerte der Größe nach.
    /// </summary>
    public bool Sortiert
    {
        get => SortiertLesen?.Invoke() ?? false;
        set => SortiertSetzen?.Invoke(value);
    }

    /// <summary>
    /// Für wen die Bilder Wärmelast und Raumtemperatur gelten (Stufe G6b): 0 = das ganze Gebäude,
    /// 1 … = die Zone dieses Rangs; leer bei einem Gebäude mit höchstens einer Zone.
    /// </summary>
    public int? Diagramm
    {
        get => DiagrammLesen?.Invoke();
        set => DiagrammSetzen?.Invoke(value);
    }

    /// <summary>Der Name des Gebäudes, dessen Bedarf die Maske zeigt.</summary>
    public string Gebaeude => Satz?.Name ?? "";

    /// <summary>Die Jahressumme der Heizwärme [MWh].</summary>
    public double? HeizwaermeMwh => Satz?.HeizwaermeMwh;

    /// <summary>Die höchste Stundenlast [kW].</summary>
    public double? MaxLastKw => Satz?.MaxLastKw;

    /// <summary>
    /// Die Vollbenutzungsstunden [h/a]; leer, wenn es sie nicht gibt (Höchstlast 0).
    /// </summary>
    public double? VollbenutzungsstundenH => Satz?.VollbenutzungsstundenH;
}
