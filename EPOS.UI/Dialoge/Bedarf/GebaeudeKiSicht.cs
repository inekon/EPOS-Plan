using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Gebäudemaske für den Hilfe-Assistenten (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>GebaeudeProjektZeile</c>.</b> Die Maske
/// führt zwei Listen und einen Detailblock; ihre eigenen Bedienelemente — die drei
/// Filterfelder und das Suchfeld — liegen in privaten Feldern der Komponente, und der
/// Detailblock zeigt je nach Markierung die Werte der Projektzeile ODER des
/// Katalogsatzes. Ein an der Zeile angemeldeter Katalog zeigte dem Modell Zahlen, die
/// auf der Maske nirgends stehen; gepflegt werden sie in <c>Form_GebWohnflaeche</c>.
/// Diese Klasse legt sich statt dessen über die LEBENDEN Felder.</para>
///
/// <para><b>Setzen heißt tippen.</b> Jeder Setzweg geht denselben Weg wie die Hand: Der
/// Katalog wird danach neu gefiltert, und eine Markierung, die aus dem Filter fällt,
/// fällt auch aus der Maske.</para>
///
/// <para><b>Sie hält keinen Zustand</b> (Muster <c>QuelleErdreichKiSicht</c>): Jede
/// Eigenschaft ruft bei jedem Zugriff ihren Delegaten.</para>
/// </summary>
public sealed class GebaeudeKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? VerwendungLesen { get; init; }
    public Action<int?>? VerwendungSetzen { get; init; }

    public Func<int?>? FilterGebaeudeartLesen { get; init; }
    public Action<int?>? FilterGebaeudeartSetzen { get; init; }

    public Func<int?>? FilterBaujahrLesen { get; init; }
    public Action<int?>? FilterBaujahrSetzen { get; init; }

    public Func<string>? SucheLesen { get; init; }
    public Action<string>? SucheSetzen { get; init; }

    public Func<string>? NameLesen { get; init; }
    public Func<string>? GebaeudeartLesen { get; init; }
    public Func<string>? BeschreibungLesen { get; init; }
    public Func<string>? WohnflaecheLesen { get; init; }
    public Func<string>? AngabeartLesen { get; init; }
    public Func<bool>? VerwaltungLesen { get; init; }

    // =====================================================================
    //  Die Einträge der drei Wahlfelder (KI-D-Q6)
    // =====================================================================

    /// <summary>Liefert die zwei Verwendungen, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? VerwendungEintraege { get; init; }

    /// <summary>Liefert die Gebäudearten der Klappliste samt dem Eintrag „Alle".</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? GebaeudeartEintraege { get; init; }

    /// <summary>Liefert die Baualtersklassen der Klappliste samt dem Eintrag „Alle".</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BaujahrEintraege { get; init; }

    /// <summary>Wohngebäude oder Gewerbe und Sonstiges.</summary>
    public IReadOnlyList<KiWahleintrag> VerwendungWahl
        => VerwendungEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Gebäudearten, die zur gewählten Verwendung gehören.</summary>
    public IReadOnlyList<KiWahleintrag> FilterGebaeudeartWahl
        => GebaeudeartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Baualtersklassen A…U samt dem Eintrag „Alle".</summary>
    public IReadOnlyList<KiWahleintrag> FilterBaujahrWahl
        => BaujahrEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die Verwendung, nach der die Katalogliste gefiltert wird: 0 = Wohngebäude,
    /// 1 = Gewerbe und Sonstiges. Sie wechselt zugleich die Gebäudearten.
    /// </summary>
    public int? Verwendung
    {
        get => VerwendungLesen?.Invoke();
        set => VerwendungSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Gebäudeart des Filters; der Platz in der Klappliste, −1 heißt „Alle".
    /// </summary>
    public int? FilterGebaeudeart
    {
        get => FilterGebaeudeartLesen?.Invoke();
        set => FilterGebaeudeartSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Baualtersklasse des Filters; der Platz in der Klappliste, −1 heißt „Alle".
    /// </summary>
    public int? FilterBaujahr
    {
        get => FilterBaujahrLesen?.Invoke();
        set => FilterBaujahrSetzen?.Invoke(value);
    }

    /// <summary>Das Suchmuster über die Namen der Katalogliste (<c>*</c> erlaubt).</summary>
    public string Suche
    {
        get => SucheLesen?.Invoke() ?? "";
        set => SucheSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Name des markierten Satzes — Projektzeile oder Katalogsatz.</summary>
    public string Name => NameLesen?.Invoke() ?? "";

    /// <summary>Die Gebäudeart des markierten Satzes.</summary>
    public string Gebaeudeart => GebaeudeartLesen?.Invoke() ?? "";

    /// <summary>Die Beschreibung des markierten Satzes.</summary>
    public string Beschreibung => BeschreibungLesen?.Invoke() ?? "";

    /// <summary>Die Wohn- oder Nutzfläche des markierten Satzes, wie sie dasteht.</summary>
    public string Wohnflaeche => WohnflaecheLesen?.Invoke() ?? "";

    /// <summary>Die Art der Angabe (Wohnfläche, Öl-, Gas- oder Brennstoffverbrauch).</summary>
    public string Angabeart => AngabeartLesen?.Invoke() ?? "";

    /// <summary>
    /// Steht die Maske in der KATALOGVERWALTUNG? Dann führt sie keine Projektliste,
    /// und „Ändern…" sowie „Simulation…" gibt es nicht.
    /// </summary>
    public bool Verwaltung => VerwaltungLesen?.Invoke() ?? false;
}
