using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Gebäudemaske für den Hilfe-Assistenten (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>GebaeudeProjektZeile</c>.</b> Die Maske
/// führt zwei Listen und einen Detailblock; ihre eigenen Bedienelemente — Suche und die
/// drei Trichter der Katalogliste — liegen im Filterstand der Komponente, und der
/// Detailblock zeigt je nach Markierung die Werte der Projektzeile ODER des
/// Katalogsatzes. Ein an der Zeile angemeldeter Katalog zeigte dem Modell Zahlen, die
/// auf der Maske nirgends stehen; gepflegt werden sie in <c>Form_GebWohnflaeche</c>.
/// Diese Klasse legt sich statt dessen über die LEBENDEN Felder.</para>
///
/// <para><b>Setzen heißt tippen</b> (Stufe G3, Welle K): Jeder Setzweg schreibt den
/// Ausdruck in den Filterstand der Katalogliste — die Suche über alle Spalten oder den
/// Trichter einer Spalte —, und die Liste filtert beim nächsten Zeichnen neu. Leer nimmt
/// den Filter zurück. Die Markierung hängt am Bezeichner und bleibt stehen, auch wenn der
/// Filter den Satz ausblendet (Hausregel der Katalogliste).</para>
///
/// <para><b>Sie hält keinen Zustand</b> (Muster <c>QuelleErdreichKiSicht</c>): Jede
/// Eigenschaft ruft bei jedem Zugriff ihren Delegaten.</para>
/// </summary>
public sealed class GebaeudeKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? VerwendungLesen { get; init; }
    public Action<string>? VerwendungSetzen { get; init; }

    public Func<string>? FilterGebaeudeartLesen { get; init; }
    public Action<string>? FilterGebaeudeartSetzen { get; init; }

    public Func<string>? FilterBaujahrLesen { get; init; }
    public Action<string>? FilterBaujahrSetzen { get; init; }

    public Func<string>? SucheLesen { get; init; }
    public Action<string>? SucheSetzen { get; init; }

    public Func<string>? NameLesen { get; init; }
    public Func<string>? GebaeudeartLesen { get; init; }
    public Func<string>? BeschreibungLesen { get; init; }
    public Func<string>? WohnflaecheLesen { get; init; }
    public Func<string>? AngabeartLesen { get; init; }

    // =====================================================================
    //  Die Einträge der drei Wahlfelder (KI-D-Q6)
    // =====================================================================

    /// <summary>Liefert die Verwendungen, die in der Spalte „Verwendung" stehen.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? VerwendungEintraege { get; init; }

    /// <summary>Liefert die Gebäudearten, die in der Spalte „Gebäudeart" stehen.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? GebaeudeartEintraege { get; init; }

    /// <summary>Liefert die Baualtersklassen, die in der Spalte „Baujahr" stehen.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BaujahrEintraege { get; init; }

    /// <summary>Wohngebäude oder Gewerbe und Sonstiges — so, wie es in der Spalte steht.</summary>
    public IReadOnlyList<KiWahleintrag> VerwendungWahl
        => VerwendungEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Gebäudearten des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> FilterGebaeudeartWahl
        => GebaeudeartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Baualtersklassen des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> FilterBaujahrWahl
        => BaujahrEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Der Trichter der Spalte „Verwendung": der Ausdruck, nach dem die Katalogliste
    /// filtert (Wohngebäude oder Gewerbe+Sonstige); leer = beide.
    /// </summary>
    public string Verwendung
    {
        get => VerwendungLesen?.Invoke() ?? "";
        set => VerwendungSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Trichter der Spalte „Gebäudeart"; leer = jede Art.</summary>
    public string FilterGebaeudeart
    {
        get => FilterGebaeudeartLesen?.Invoke() ?? "";
        set => FilterGebaeudeartSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Trichter der Spalte „Baujahr" (Baualtersklasse); leer = jede Klasse.</summary>
    public string FilterBaujahr
    {
        get => FilterBaujahrLesen?.Invoke() ?? "";
        set => FilterBaujahrSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Suche über alle Spalten der Katalogliste (<c>*</c> und <c>?</c> erlaubt).</summary>
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
}
