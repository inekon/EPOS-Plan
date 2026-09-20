using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Maske „Wohn-/Nutzfläche des Gebäudes" für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>GebaeudeWohnflaecheErgebnis</c>.</b> Jener
/// Satz ist ein unveränderlicher Record und entsteht erst beim OK; bis dahin stehen die
/// vier Eingaben in den lebenden Feldern der Maske, und die fünf Kopfangaben kommen als
/// einzelne Parameter herein. Ein am Ergebnis angemeldeter Katalog hätte in ein Objekt
/// geschrieben, das es noch gar nicht gibt.</para>
///
/// <para><b>Die Bedarfsart ist ein WAHLFELD</b> (KI‑D‑Q6): Ihre Einträge sind die sechs
/// festen Texte der Maske; sie sind zugleich Steuerwerte und entscheiden, ob mit der
/// Wohnfläche gerechnet oder die Fläche aus einem Verbrauch zurückgerechnet wird.</para>
///
/// <para><b>Sie hält keinen Zustand</b> (Muster <c>QuelleErdreichKiSicht</c>): Jede
/// Eigenschaft ruft bei jedem Zugriff ihren Delegaten.</para>
/// </summary>
public sealed class GebaeudeWohnflaecheKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? BedarfsartLesen { get; init; }
    public Action<int?>? BedarfsartSetzen { get; init; }

    public Func<double?>? WertLesen { get; init; }
    public Action<double?>? WertSetzen { get; init; }

    public Func<double?>? NutzungsgradLesen { get; init; }
    public Action<double?>? NutzungsgradSetzen { get; init; }

    public Func<bool>? DezentralLesen { get; init; }
    public Action<bool>? DezentralSetzen { get; init; }

    public Func<string>? GebaeudenameLesen { get; init; }
    public Func<string>? GebaeudeartLesen { get; init; }
    public Func<string>? BeschreibungLesen { get; init; }
    public Func<string>? BaujahrLesen { get; init; }
    public Func<string>? AngabeartLesen { get; init; }

    /// <summary>Liefert die sechs Bedarfsarten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BedarfsartEintraege { get; init; }

    /// <summary>Die sechs Bedarfsarten samt Einheit (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> BedarfsartWahl
        => BedarfsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die Art der Angabe als Platz in der Klappliste; sie bestimmt Einheit und
    /// Rechenweg. Ein Bestandswert, den die Liste nicht führt, steht unter −1.
    /// </summary>
    public int? Bedarfsart
    {
        get => BedarfsartLesen?.Invoke();
        set => BedarfsartSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der Wärmebedarf bzw. die Wohnfläche — die Zahl in der Einheit, die
    /// <see cref="Bedarfsart"/> nennt.
    /// </summary>
    public double? Wert
    {
        get => WertLesen?.Invoke();
        set => WertSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der Jahresnutzungsgrad des Kessels als Anteil (0,85 heißt 85 %). Er wird nur
    /// bei einer Brennstoffangabe gebraucht.
    /// </summary>
    public double? Jahresnutzungsgrad
    {
        get => NutzungsgradLesen?.Invoke();
        set => NutzungsgradSetzen?.Invoke(value);
    }

    /// <summary>Dezentrale Warmwasserbereitung im Gebäude.</summary>
    public bool DezentralWarmwasser
    {
        get => DezentralLesen?.Invoke() ?? false;
        set => DezentralSetzen?.Invoke(value);
    }

    /// <summary>Der Name des Gebäudes, dessen Angaben die Maske führt.</summary>
    public string Gebaeudename => GebaeudenameLesen?.Invoke() ?? "";

    /// <summary>Die Gebäudeart aus dem Katalogsatz.</summary>
    public string Gebaeudeart => GebaeudeartLesen?.Invoke() ?? "";

    /// <summary>Die Beschreibung aus dem Katalogsatz.</summary>
    public string Beschreibung => BeschreibungLesen?.Invoke() ?? "";

    /// <summary>Die Baualtersklasse im Klartext.</summary>
    public string Baujahr => BaujahrLesen?.Invoke() ?? "";

    /// <summary>
    /// Die Art der Angabe, wie sie im Kopf steht — der Bestandstext der Zuordnung,
    /// auch wenn ihn die Klappliste nicht führt.
    /// </summary>
    public string Angabeart => AngabeartLesen?.Invoke() ?? "";
}
