using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Kostenprofil" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Was die Maske führt.</b> Ihre Einstellwerte sind der BEZEICHNER des
/// Profils, der Wochentag, dessen Stundenkurve gerade dasteht, und die zwei
/// Wertetafeln — als ZAHLENREIHEN (Welle #458 Stufe 3b): die zwölf Monatsniveaus
/// (<see cref="Monatswerte"/>) und die 7 × 24 Abweichungen je Wochentag und Stunde
/// (<see cref="Wochenwerte"/>, Montag Stunde 1 zuerst). Sie stehen nicht einzeln in
/// der Feldkarte, sondern je als EIN Feld mit einer Liste; die Griffe der Maske
/// („Jan.-Wert in alle Monate", „Tag kopieren", „Für alle Tage") sind für den
/// Assistenten ein Aufruf mit der passenden Liste.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class KostenprofilKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? BezeichnerLesen { get; init; }
    public Action<string>? BezeichnerSetzen { get; init; }

    public Func<int?>? TagLesen { get; init; }
    public Action<int?>? TagSetzen { get; init; }

    /// <summary>Liefert die sieben Wochentage der Klappliste.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TagEintraege { get; init; }

    public Func<string>? EinheitLesen { get; init; }

    /// <summary>Liest die zwölf Monatsniveaus.</summary>
    public Func<double?[]?>? MonatswerteLesen { get; init; }

    /// <summary>Schreibt die zwölf Monatsniveaus in die Tafel.</summary>
    public Action<double?[]>? MonatswerteSetzen { get; init; }

    /// <summary>Liest die 168 Abweichungen, Montag Stunde 1 zuerst.</summary>
    public Func<double?[]?>? WochenwerteLesen { get; init; }

    /// <summary>Schreibt die 168 Abweichungen in die Tafel.</summary>
    public Action<double?[]>? WochenwerteSetzen { get; init; }

    /// <summary>Die Wochentage der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> WochentagWahl
        => TagEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der Name des Profils.</summary>
    public string Bezeichner
    {
        get => BezeichnerLesen?.Invoke() ?? "";
        set => BezeichnerSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Der Wochentag, dessen 24 Stundenwerte das Reiterblatt „Woche" zeigt.
    /// </summary>
    public int? Wochentag
    {
        get => TagLesen?.Invoke();
        set => TagSetzen?.Invoke(value);
    }

    /// <summary>Die Einheit, in der die Tafel rechnet — Anzeige, nicht Eingabe.</summary>
    public string Einheit => EinheitLesen?.Invoke() ?? "";

    /// <summary>Die Zahlenreihe der zwölf Monatsniveaus (Welle #458 Stufe 3b).</summary>
    public double?[]? Monatswerte
    {
        get => MonatswerteLesen?.Invoke();
        set { if (value is not null) MonatswerteSetzen?.Invoke(value); }
    }

    /// <summary>
    /// Die Zahlenreihe der 168 Abweichungen je Wochentag und Stunde (Welle #458
    /// Stufe 3b) — die Stelle (Tag − 1) · 24 + Stunde.
    /// </summary>
    public double?[]? Wochenwerte
    {
        get => WochenwerteLesen?.Invoke();
        set { if (value is not null) WochenwerteSetzen?.Invoke(value); }
    }
}
