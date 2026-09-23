using KiKern;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Das FLACHE Abbild des Reiterblatts „Wirtschaftlichkeit" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Diese Seite trägt Einstellwerte und ist deshalb drin.</b> Sie
/// entscheidet, WELCHE Stände gegeneinander gerechnet werden (Vergleichssicht,
/// Referenz, Paar A und B), unter WELCHEM Szenario (Erwartet, Best, Worst) — und
/// sie pflegt den Freitext der nicht monetären Wirkungen nach DIN EN 17463. Die
/// Kennzahltabelle, die Herleitungszeilen und der Kapitalwertverlauf darunter sind
/// gerechnete Anzeige und bleiben draußen.</para>
///
/// <para><b>Warum ein Sichtmodell.</b> Jedes dieser Felder ist an der Seite ein
/// WEG und kein Wert: Die Seite holt sich zu jeder Wahl einen NEUEN Stand aus der
/// Hülle (<c>Uebernehmen</c>) und rechnet das Warnband nach. Ein Katalog am Stand
/// schriebe die Zahl hin, und die Seite zeigte die Kennzahlen von vorhin.</para>
///
/// <para><b>Die Anzeigewahlen von Block 2 sind drin</b> (ValERI-Bewertung,
/// „Zahlungsreihen"; KI‑D‑Q11): Stand und Szenario der Jahrestafel wählen nur, welche
/// schon gelieferte Tafel die Seite zeigt — kein neuer Stand, kein Nachrechnen. Gelesen
/// wird die GEZEIGTE Wahl (Vorgabe: die Leitversion im Erwartungsfall).</para>
///
/// <para><b>Die Vergleichsgruppe bleibt draußen</b>: Welche Varianten angehakt
/// sind, ist eine Menge von Verweisen und kein Feldwert.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class WirtschaftlichkeitSeiteKiSicht
{
    public Func<int?>? SzenarioLesen { get; init; }
    public Action<int?>? SzenarioSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SzenarioEintraege { get; init; }

    public Func<int?>? SichtLesen { get; init; }
    public Action<int?>? SichtSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SichtEintraege { get; init; }

    public Func<int?>? ReferenzLesen { get; init; }
    public Action<int?>? ReferenzSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? StandEintraege { get; init; }

    public Func<int?>? ALesen { get; init; }
    public Action<int?>? ASetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AEintraege { get; init; }

    public Func<int?>? BLesen { get; init; }
    public Action<int?>? BSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BEintraege { get; init; }

    public Func<string>? WirkungLesen { get; init; }
    public Action<string>? WirkungSetzen { get; init; }

    public Func<int?>? ZahlungsstandLesen { get; init; }
    public Action<int?>? ZahlungsstandSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ZahlungsstandEintraege { get; init; }

    public Func<int?>? ZahlungsszenarioLesen { get; init; }
    public Action<int?>? ZahlungsszenarioSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ZahlungsszenarioEintraege { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Die Szenarien — Erwartet, Best und Worst.</summary>
    public IReadOnlyList<KiWahleintrag> SzenarioWahl
        => SzenarioEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die zwei Vergleichssichten.</summary>
    public IReadOnlyList<KiWahleintrag> VergleichssichtWahl
        => SichtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Stände der Gruppe — Stamm und Varianten.</summary>
    public IReadOnlyList<KiWahleintrag> ReferenzWahl
        => StandEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die für A wählbaren Stände — ohne den, der auf B steht.</summary>
    public IReadOnlyList<KiWahleintrag> StandAWahl
        => AEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die für B wählbaren Stände — ohne den, der auf A steht.</summary>
    public IReadOnlyList<KiWahleintrag> StandBWahl
        => BEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Stände, für die der Lauf Zahlungsreihen geliefert hat (Block 2).</summary>
    public IReadOnlyList<KiWahleintrag> ZahlungsreihenStandWahl
        => ZahlungsstandEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Szenarien der Zahlungsreihen (Block 2).</summary>
    public IReadOnlyList<KiWahleintrag> ZahlungsreihenSzenarioWahl
        => ZahlungsszenarioEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Seite
    // =====================================================================

    /// <summary>Das Szenario, unter dem die Kennzahlen gerechnet werden.</summary>
    public int? Szenario
    {
        get => SzenarioLesen?.Invoke();
        set => SzenarioSetzen?.Invoke(value);
    }

    /// <summary>Alle angehakten Stände gegen die Referenz — oder zwei Stände A und B.</summary>
    public int? Vergleichssicht
    {
        get => SichtLesen?.Invoke();
        set => SichtSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Referenz der Gruppe — die Unterlassensalternative, gegen die gerechnet
    /// wird.
    /// </summary>
    public int? Referenz
    {
        get => ReferenzLesen?.Invoke();
        set => ReferenzSetzen?.Invoke(value);
    }

    /// <summary>Der Stand A der Paarsicht — ihre Referenz.</summary>
    public int? StandA
    {
        get => ALesen?.Invoke();
        set => ASetzen?.Invoke(value);
    }

    /// <summary>Der Stand B der Paarsicht.</summary>
    public int? StandB
    {
        get => BLesen?.Invoke();
        set => BSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die nicht monetären Wirkungen nach DIN EN 17463 — Freitext. Er wird auf Zuruf
    /// geschrieben, nicht bei jedem Zeichen.
    /// </summary>
    public string NichtMonetaer
    {
        get => WirkungLesen?.Invoke() ?? "";
        set => WirkungSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Der Stand, dessen Zahlungsreihen Block 2 zeigt — eine Anzeigewahl; <c>null</c> =
    /// keine Jahresreihen in dieser Sitzung.
    /// </summary>
    public int? ZahlungsreihenStand
    {
        get => ZahlungsstandLesen?.Invoke();
        set => ZahlungsstandSetzen?.Invoke(value);
    }

    /// <summary>
    /// Das Szenario, dessen Zahlungsreihen Block 2 zeigt — eine Anzeigewahl, unabhängig
    /// vom Szenario der Kennzahlen.
    /// </summary>
    public int? ZahlungsreihenSzenario
    {
        get => ZahlungsszenarioLesen?.Invoke();
        set => ZahlungsszenarioSetzen?.Invoke(value);
    }
}
