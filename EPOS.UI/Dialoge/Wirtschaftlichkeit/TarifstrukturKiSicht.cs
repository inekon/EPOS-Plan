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
/// <para><b>Zwei Modelle, eine Maske.</b> Das ZONENMODELL rechnet mit vier
/// Preiszonen (Winter/Sommer × Hoch-/Niedertarif) und einer Leistungsstaffel, das
/// ROLLENMODELL mit je einem Arbeits-, Grund- und Leistungspreis für Bezug und
/// Reststrom. Welches gilt, sagt das Feld <see cref="Modell"/>; die Felder des
/// jeweils anderen stehen gesperrt da. Deklariert sind beide — der Anwender sieht
/// beide.</para>
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

    public Func<string>? ModellLesen { get; init; }
    public Action<string>? ModellSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ModellEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? LeistungsmodellEintraege { get; init; }

    public Func<string>? GueltigAbLesen { get; init; }
    public Action<string>? GueltigAbSetzen { get; init; }

    public Func<int?>? WinterVonLesen { get; init; }
    public Action<int?>? WinterVonSetzen { get; init; }
    public Func<int?>? WinterBisLesen { get; init; }
    public Action<int?>? WinterBisSetzen { get; init; }
    public Func<int?>? HtVonLesen { get; init; }
    public Action<int?>? HtVonSetzen { get; init; }
    public Func<int?>? HtBisLesen { get; init; }
    public Action<int?>? HtBisSetzen { get; init; }

    public Func<double?>? BezugWinterHtLesen { get; init; }
    public Action<double?>? BezugWinterHtSetzen { get; init; }
    public Func<double?>? BezugWinterNtLesen { get; init; }
    public Action<double?>? BezugWinterNtSetzen { get; init; }
    public Func<double?>? BezugSommerHtLesen { get; init; }
    public Action<double?>? BezugSommerHtSetzen { get; init; }
    public Func<double?>? BezugSommerNtLesen { get; init; }
    public Action<double?>? BezugSommerNtSetzen { get; init; }

    public Func<double?>? EinspWinterHtLesen { get; init; }
    public Action<double?>? EinspWinterHtSetzen { get; init; }
    public Func<double?>? EinspWinterNtLesen { get; init; }
    public Action<double?>? EinspWinterNtSetzen { get; init; }
    public Func<double?>? EinspSommerHtLesen { get; init; }
    public Action<double?>? EinspSommerHtSetzen { get; init; }
    public Func<double?>? EinspSommerNtLesen { get; init; }
    public Action<double?>? EinspSommerNtSetzen { get; init; }

    public Func<double?>? StaffelGrenzeLesen { get; init; }
    public Action<double?>? StaffelGrenzeSetzen { get; init; }
    public Func<double?>? StaffelPreis1Lesen { get; init; }
    public Action<double?>? StaffelPreis1Setzen { get; init; }
    public Func<double?>? StaffelPreis2Lesen { get; init; }
    public Action<double?>? StaffelPreis2Setzen { get; init; }

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

    /// <summary>Zonenmodell oder Rollenmodell — Schlüssel ist der Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> ModellWahl
        => ModellEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

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

    /// <summary>Zonenmodell oder Rollenmodell.</summary>
    public string Modell
    {
        get => ModellLesen?.Invoke() ?? "";
        set => ModellSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Preisstand, ab dem die Tarifstruktur gilt (ISO).</summary>
    public string GueltigAb
    {
        get => GueltigAbLesen?.Invoke() ?? "";
        set => GueltigAbSetzen?.Invoke(value ?? "");
    }

    // =====================================================================
    //  Zeitzonen
    // =====================================================================

    /// <summary>Der Monat, mit dem die Winterzone beginnt.</summary>
    public int? WinterVon
    {
        get => WinterVonLesen?.Invoke();
        set => WinterVonSetzen?.Invoke(value);
    }

    /// <summary>Der Monat, mit dem die Winterzone endet.</summary>
    public int? WinterBis
    {
        get => WinterBisLesen?.Invoke();
        set => WinterBisSetzen?.Invoke(value);
    }

    /// <summary>Die Stunde, mit der der Hochtarif beginnt.</summary>
    public int? HochtarifVon
    {
        get => HtVonLesen?.Invoke();
        set => HtVonSetzen?.Invoke(value);
    }

    /// <summary>Die Stunde, mit der der Hochtarif endet.</summary>
    public int? HochtarifBis
    {
        get => HtBisLesen?.Invoke();
        set => HtBisSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Zonenmodell: Bezug, Einspeisung, Staffel
    // =====================================================================

    /// <summary>Bezugspreis Winter im Hochtarif.</summary>
    public double? BezugWinterHoch
    {
        get => BezugWinterHtLesen?.Invoke();
        set => BezugWinterHtSetzen?.Invoke(value);
    }

    /// <summary>Bezugspreis Winter im Niedertarif.</summary>
    public double? BezugWinterNieder
    {
        get => BezugWinterNtLesen?.Invoke();
        set => BezugWinterNtSetzen?.Invoke(value);
    }

    /// <summary>Bezugspreis Sommer im Hochtarif.</summary>
    public double? BezugSommerHoch
    {
        get => BezugSommerHtLesen?.Invoke();
        set => BezugSommerHtSetzen?.Invoke(value);
    }

    /// <summary>Bezugspreis Sommer im Niedertarif.</summary>
    public double? BezugSommerNieder
    {
        get => BezugSommerNtLesen?.Invoke();
        set => BezugSommerNtSetzen?.Invoke(value);
    }

    /// <summary>Einspeisepreis Winter im Hochtarif — er gilt für PV UND KWK.</summary>
    public double? EinspeisungWinterHoch
    {
        get => EinspWinterHtLesen?.Invoke();
        set => EinspWinterHtSetzen?.Invoke(value);
    }

    /// <summary>Einspeisepreis Winter im Niedertarif.</summary>
    public double? EinspeisungWinterNieder
    {
        get => EinspWinterNtLesen?.Invoke();
        set => EinspWinterNtSetzen?.Invoke(value);
    }

    /// <summary>Einspeisepreis Sommer im Hochtarif.</summary>
    public double? EinspeisungSommerHoch
    {
        get => EinspSommerHtLesen?.Invoke();
        set => EinspSommerHtSetzen?.Invoke(value);
    }

    /// <summary>Einspeisepreis Sommer im Niedertarif.</summary>
    public double? EinspeisungSommerNieder
    {
        get => EinspSommerNtLesen?.Invoke();
        set => EinspSommerNtSetzen?.Invoke(value);
    }

    /// <summary>Die Leistungsgrenze, ab der der zweite Staffelpreis gilt.</summary>
    public double? StaffelGrenze
    {
        get => StaffelGrenzeLesen?.Invoke();
        set => StaffelGrenzeSetzen?.Invoke(value);
    }

    /// <summary>Der Leistungspreis unterhalb der Staffelgrenze.</summary>
    public double? StaffelPreisUnten
    {
        get => StaffelPreis1Lesen?.Invoke();
        set => StaffelPreis1Setzen?.Invoke(value);
    }

    /// <summary>Der Leistungspreis oberhalb der Staffelgrenze.</summary>
    public double? StaffelPreisOben
    {
        get => StaffelPreis2Lesen?.Invoke();
        set => StaffelPreis2Setzen?.Invoke(value);
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
