using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Kostenprofil" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Was die Maske führt und was nicht.</b> Ihre Einstellwerte sind der
/// BEZEICHNER des Profils und der Wochentag, dessen Stundenkurve gerade dasteht.
/// Die 36 Zahlenfelder darunter — zwölf Monatswerte und 7 × 24 Wochenstunden —
/// sind eine WERTETAFEL: Sie entstehen aus einer Schleife über ihren Index, und
/// die Maske pflegt sie mit eigenen Griffen („Jan.-Wert in alle Monate",
/// „Tag kopieren", „Tag einfügen"). Der Dateikopf des Dialogs weist sie
/// ausdrücklich als „nicht einzeln in die Feldkarte" aus; der Katalog folgt dem.</para>
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
}
