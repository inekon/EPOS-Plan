namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Saisonale Leistungspreis-Sätze" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell.</b> Der Dialog hält seinen Arbeitsstand in
/// privaten Feldern (<c>_jahr</c>, <c>_werte</c>); die Parameter <c>Jahr</c> und
/// <c>Werte</c> sind nur der Anfangsstand, und einen Ergebnis-Record gibt es
/// nicht — geschrieben wird über den Delegaten <c>Uebernehmen</c>.</para>
///
/// <para><b>Die zwölf Monatssätze sind EINE Zahlenreihe</b> (Welle #458 Stufe 3b,
/// <see cref="Monatssaetze"/>): Januar bis Dezember, mit den Grenzen der zwölf
/// Eingabefelder. Das JAHR ist der Einstellwert, der die ganze Reihe trägt: Er sagt,
/// für welches Jahr die Sätze gelten.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class LeistungspreisReiheKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? JahrLesen { get; init; }
    public Action<int?>? JahrSetzen { get; init; }

    public Func<string>? EinheitLesen { get; init; }
    public Func<string>? KontextLesen { get; init; }

    /// <summary>Liest die zwölf Monatssätze.</summary>
    public Func<double?[]?>? MonatssaetzeLesen { get; init; }

    /// <summary>Schreibt die zwölf Monatssätze in die Felder der Maske.</summary>
    public Action<double?[]>? MonatssaetzeSetzen { get; init; }

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Das Jahr, für das die zwölf Monatssätze gelten.</summary>
    public int? Jahr
    {
        get => JahrLesen?.Invoke();
        set => JahrSetzen?.Invoke(value);
    }

    /// <summary>Die Einheit der zwölf Sätze — Anzeige, nicht Eingabe.</summary>
    public string Einheit => EinheitLesen?.Invoke() ?? "";

    /// <summary>Zu welchem Energieträger die Reihe gehört — Anzeige.</summary>
    public string Kontext => KontextLesen?.Invoke() ?? "";

    /// <summary>
    /// Die Zahlenreihe der zwölf Monatssätze (Welle #458 Stufe 3b), Januar bis Dezember;
    /// die Grenzen (0 bis 100 000) sind die der zwölf Eingabefelder.
    /// </summary>
    public double?[]? Monatssaetze
    {
        get => MonatssaetzeLesen?.Invoke();
        set { if (value is not null) MonatssaetzeSetzen?.Invoke(value); }
    }
}
