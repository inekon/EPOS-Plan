namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Worst-/Best-Case einer Kostenposition" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell.</b> Der Dialog hält seinen Stand in sieben
/// privaten Feldern; <c>CaseEingabeErgebnis</c> entsteht erst beim OK und trägt die
/// Beträge dann IMMER in Euro — auch wenn der Anwender sie als Prozentabweichung
/// getippt hat.</para>
///
/// <para><b>Der PROZENTMODUS führt die Maske.</b> Er entscheidet, ob die zwei
/// Kostenfelder einen Betrag oder eine Abweichung vom Erwartungswert tragen, und
/// damit auch ihre Einheit und ihre Grenzen. Ohne gepflegten Erwartungswert ist er
/// gesperrt — dann gibt es nichts, wovon abzuweichen wäre.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
///
/// <para><b>ETAPPE E9b — verallgemeinert.</b> Dieselbe Maske pflegt seither auch das
/// Best/Worst-Paar eines Trägerpreises oder Erlössatzes (Szenariopaar). Drei
/// Auskunftsfelder sagen dem Assistenten, WAS gepflegt wird (<see cref="Groesse"/>),
/// gegen welchen Erwartet-Wert (<see cref="Erwartet"/>) und in welcher Einheit
/// (<see cref="Einheit"/>). Im Szenariopaar führt die Maske weder Nutzungsdauer noch
/// Startjahr noch Zuschuss: Ihre Setzwege bleiben dann leer — gesetzt wird nichts, und
/// gelesen wird 0 bzw. „nein".</para>
/// </summary>
public sealed class CaseEingabeKiSicht
{
    public Func<bool>? ProzentmodusLesen { get; init; }
    public Action<bool>? ProzentmodusSetzen { get; init; }

    public Func<double?>? BestLesen { get; init; }
    public Action<double?>? BestSetzen { get; init; }
    public Func<double?>? WorstLesen { get; init; }
    public Action<double?>? WorstSetzen { get; init; }

    public Func<double>? BestDauerLesen { get; init; }
    public Action<double>? BestDauerSetzen { get; init; }
    public Func<double>? WorstDauerLesen { get; init; }
    public Action<double>? WorstDauerSetzen { get; init; }

    public Func<int>? JahrLesen { get; init; }
    public Action<int>? JahrSetzen { get; init; }

    public Func<bool>? ZuschussLesen { get; init; }
    public Action<bool>? ZuschussSetzen { get; init; }

    /// <summary>
    /// Tragen die zwei Kostenfelder eine PROZENTABWEICHUNG statt eines Betrags?
    /// </summary>
    public bool Prozentmodus
    {
        get => ProzentmodusLesen?.Invoke() ?? false;
        set => ProzentmodusSetzen?.Invoke(value);
    }

    /// <summary>Die Kosten im besten Fall.</summary>
    public double? BestCase
    {
        get => BestLesen?.Invoke();
        set => BestSetzen?.Invoke(value);
    }

    /// <summary>Die Kosten im schlechtesten Fall.</summary>
    public double? WorstCase
    {
        get => WorstLesen?.Invoke();
        set => WorstSetzen?.Invoke(value);
    }

    /// <summary>Die Nutzungsdauer im besten Fall.</summary>
    public double BestNutzungsdauer
    {
        get => BestDauerLesen?.Invoke() ?? 0;
        set => BestDauerSetzen?.Invoke(value);
    }

    /// <summary>Die Nutzungsdauer im schlechtesten Fall.</summary>
    public double WorstNutzungsdauer
    {
        get => WorstDauerLesen?.Invoke() ?? 0;
        set => WorstDauerSetzen?.Invoke(value);
    }

    /// <summary>Das Jahr, in dem die Zahlung beginnt (0 = im ersten Jahr).</summary>
    public int Startjahr
    {
        get => JahrLesen?.Invoke() ?? 0;
        set => JahrSetzen?.Invoke(value);
    }

    /// <summary>Ist die Position ein ZUSCHUSS und keine Ausgabe?</summary>
    public bool IstZuschuss
    {
        get => ZuschussLesen?.Invoke() ?? false;
        set => ZuschussSetzen?.Invoke(value);
    }

    // =====================================================================
    //  ETAPPE E9b — die Auskunft über das gepflegte Paar (nur lesbar)
    // =====================================================================

    public Func<string>? GroesseLesen { get; init; }
    public Func<double?>? ErwartetLesen { get; init; }
    public Func<string>? EinheitLesen { get; init; }

    /// <summary>ETAPPE E9b: Was die Maske pflegt — „Kosten" bei einer Kostenposition,
    /// sonst der Trägerpreis oder Erlössatz („Arbeitspreis Erdgas E").</summary>
    public string Groesse => GroesseLesen?.Invoke() ?? "";

    /// <summary>ETAPPE E9b: der Erwartet-Wert, gegen den das Paar steht; leer = keiner
    /// gepflegt (dann rechnen Günstig und Ungünstig mit ihrem Wert allein).</summary>
    public double? Erwartet => ErwartetLesen?.Invoke();

    /// <summary>ETAPPE E9b: die Einheit der zwei Wertfelder im Absolutmodus.</summary>
    public string Einheit => EinheitLesen?.Invoke() ?? "";
}
