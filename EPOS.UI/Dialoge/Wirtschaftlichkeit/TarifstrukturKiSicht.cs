using KiKern;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Das FLACHE Abbild der Maske „Tarifstruktur" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell.</b> Der Dialog liest den <c>TarifParameter</c>
/// des Kerns beim Aufbau EINMAL in private Felder und schreibt ihn erst im OK-Weg
/// zurück. Ein Katalog am Parametersatz zeigte damit den Stand von vorhin und
/// setzte ins Leere — dieselbe Lage wie bei den Masken der
/// Simulationskonfiguration (Welle KI‑F2).</para>
///
/// <para><b>Ein Modell, das Rollenmodell</b> (Entscheid Q11, Anwender 22.09.2026:
/// „kein HT/NT"): je ein Arbeits-, Grund- und Leistungspreis für Bezug und
/// Reststrom, dazu die Einspeisung. Die Felder des entfallenen Zonenmodells —
/// Modellwahl, Hochtarif-Fenster, Zonenpreise, zweistufige Staffel — gibt es auf der
/// Maske nicht mehr, also auch nicht hier; die Staffel pflegt der Stromträger
/// (Maske Energieträger).</para>
///
/// <para><b>Die vier LEISTUNGSSTUFEN je Rolle bleiben draußen.</b> Zwölf Zellen je
/// Rolle (Obergrenze, Sommer- und Winterpreis × vier Stufen) entstehen aus einer
/// Schleife über ihren Index und sind damit eine WERTETAFEL — dieselbe Regel, mit
/// der diese Welle die Monatssätze der Preisreihe und die Stundentafel des
/// Kostenprofils auslässt.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class TarifstrukturKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<bool>? AktivLesen { get; init; }
    public Action<bool>? AktivSetzen { get; init; }

    public Func<IReadOnlyList<KiWahleintrag>>? LeistungsmodellEintraege { get; init; }

    public Func<string>? GueltigAbLesen { get; init; }
    public Action<string>? GueltigAbSetzen { get; init; }

    public Func<int?>? WinterVonLesen { get; init; }
    public Action<int?>? WinterVonSetzen { get; init; }
    public Func<int?>? WinterBisLesen { get; init; }
    public Action<int?>? WinterBisSetzen { get; init; }

    public Func<double?>? BezugArbeitLesen { get; init; }
    public Action<double?>? BezugArbeitSetzen { get; init; }
    public Func<double?>? BezugGrundLesen { get; init; }
    public Action<double?>? BezugGrundSetzen { get; init; }
    public Func<double?>? BezugMonatLesen { get; init; }
    public Action<double?>? BezugMonatSetzen { get; init; }
    public Func<string>? BezugModellLesen { get; init; }
    public Action<string>? BezugModellSetzen { get; init; }

    public Func<double?>? RestArbeitLesen { get; init; }
    public Action<double?>? RestArbeitSetzen { get; init; }
    public Func<double?>? RestGrundLesen { get; init; }
    public Action<double?>? RestGrundSetzen { get; init; }
    public Func<double?>? RestMonatLesen { get; init; }
    public Action<double?>? RestMonatSetzen { get; init; }
    public Func<string>? RestModellLesen { get; init; }
    public Action<string>? RestModellSetzen { get; init; }

    public Func<double?>? EinspArbeitLesen { get; init; }
    public Action<double?>? EinspArbeitSetzen { get; init; }
    public Func<double?>? EinspGrundLesen { get; init; }
    public Action<double?>? EinspGrundSetzen { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Das Leistungsmodell der Bezugsrolle.</summary>
    public IReadOnlyList<KiWahleintrag> BezugLeistungsmodellWahl
        => LeistungsmodellEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Das Leistungsmodell der Reststromrolle.</summary>
    public IReadOnlyList<KiWahleintrag> RestLeistungsmodellWahl
        => LeistungsmodellEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Kopf
    // =====================================================================

    /// <summary>Wird die Tarifstruktur überhaupt angewendet?</summary>
    public bool Aktiv
    {
        get => AktivLesen?.Invoke() ?? false;
        set => AktivSetzen?.Invoke(value);
    }

    /// <summary>Der Preisstand, ab dem die Tarifstruktur gilt (ISO).</summary>
    public string GueltigAb
    {
        get => GueltigAbLesen?.Invoke() ?? "";
        set => GueltigAbSetzen?.Invoke(value ?? "");
    }

    // =====================================================================
    //  Winterspanne (Leistungspreismodell „Staffel" der Bezugsrollen)
    // =====================================================================

    /// <summary>Der Monat, mit dem die Winterspanne beginnt.</summary>
    public int? WinterVon
    {
        get => WinterVonLesen?.Invoke();
        set => WinterVonSetzen?.Invoke(value);
    }

    /// <summary>Der Monat, mit dem die Winterspanne endet.</summary>
    public int? WinterBis
    {
        get => WinterBisLesen?.Invoke();
        set => WinterBisSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Rollenmodell
    // =====================================================================

    /// <summary>Der Arbeitspreis der Bezugsrolle.</summary>
    public double? BezugArbeitspreis
    {
        get => BezugArbeitLesen?.Invoke();
        set => BezugArbeitSetzen?.Invoke(value);
    }

    /// <summary>Der Grundpreis der Bezugsrolle.</summary>
    public double? BezugGrundpreis
    {
        get => BezugGrundLesen?.Invoke();
        set => BezugGrundSetzen?.Invoke(value);
    }

    /// <summary>Der Monatspreis der Bezugsrolle.</summary>
    public double? BezugMonatspreis
    {
        get => BezugMonatLesen?.Invoke();
        set => BezugMonatSetzen?.Invoke(value);
    }

    /// <summary>Das Leistungsmodell der Bezugsrolle.</summary>
    public string BezugLeistungsmodell
    {
        get => BezugModellLesen?.Invoke() ?? "";
        set => BezugModellSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Arbeitspreis der Reststromrolle.</summary>
    public double? RestArbeitspreis
    {
        get => RestArbeitLesen?.Invoke();
        set => RestArbeitSetzen?.Invoke(value);
    }

    /// <summary>Der Grundpreis der Reststromrolle.</summary>
    public double? RestGrundpreis
    {
        get => RestGrundLesen?.Invoke();
        set => RestGrundSetzen?.Invoke(value);
    }

    /// <summary>Der Monatspreis der Reststromrolle.</summary>
    public double? RestMonatspreis
    {
        get => RestMonatLesen?.Invoke();
        set => RestMonatSetzen?.Invoke(value);
    }

    /// <summary>Das Leistungsmodell der Reststromrolle.</summary>
    public string RestLeistungsmodell
    {
        get => RestModellLesen?.Invoke() ?? "";
        set => RestModellSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Einspeisepreis der Einspeiserolle.</summary>
    public double? EinspeisungArbeitspreis
    {
        get => EinspArbeitLesen?.Invoke();
        set => EinspArbeitSetzen?.Invoke(value);
    }

    /// <summary>Der Grundpreis der Einspeiserolle.</summary>
    public double? EinspeisungGrundpreis
    {
        get => EinspGrundLesen?.Invoke();
        set => EinspGrundSetzen?.Invoke(value);
    }
}
