using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Maske „Wochen-Stundenprofil eines Typs" für den
/// Hilfe-Assistenten (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>TypProfilDaten</c>.</b> Der TYP ist eine
/// WAHL und kein Feld: Die Liste lädt einen anderen Satz samt seinen 168 Werten; ein
/// Setzen von <c>Daten.Typ</c> benannte bloß den Satz im Speicher um. Der gewählte
/// WOCHENTAG gehört ohnehin der Komponente — er entscheidet, welche 24 Felder gerade
/// dastehen.</para>
///
/// <para><b>Die 7 × 24 ÜBERNOMMENEN Wochenwerte sind EINE Zahlenreihe</b> (Welle #458
/// Stufe 3b, <see cref="Wochenwerte"/>): Montag Stunde 1 bis Sonntag Stunde 24 — der
/// Stand, den „Speichern in DB" schreibt. Setzen heißt dasselbe wie Tippen und
/// „Änderungen Übernehmen"; die 24 Felder des gezeigten Tages ziehen nach.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class TypProfilKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? TypLesen { get; init; }
    public Action<string>? TypSetzen { get; init; }

    public Func<int?>? WochentagLesen { get; init; }
    public Action<int?>? WochentagSetzen { get; init; }

    public Func<string>? BeschreibungLesen { get; init; }
    public Action<string>? BeschreibungSetzen { get; init; }

    /// <summary>Liest die 168 übernommenen Wochenwerte, Montag Stunde 1 zuerst.</summary>
    public Func<double?[]?>? WochenwerteLesen { get; init; }

    /// <summary>Legt die 168 Wochenwerte als übernommenen Stand ab.</summary>
    public Action<double?[]>? WochenwerteSetzen { get; init; }

    /// <summary>Liefert die Typen, die die Liste der Maske führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TypEintraege { get; init; }

    /// <summary>Liefert die sieben Wochentage in der Reihenfolge der Maske.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? WochentagEintraege { get; init; }

    /// <summary>Die Typen des Katalogs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> TypWahl
        => TypEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Montag bis Sonntag (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> WochentagWahl
        => WochentagEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Der geladene Typ. Ein Setzen LÄDT sein Profil — denselben Weg nimmt ein Klick
    /// in die Liste.
    /// </summary>
    public string Typ
    {
        get => TypLesen?.Invoke() ?? "";
        set => TypSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Der Wochentag, dessen 24 Stundenwerte die Maske zeigt; der Schlüssel ist sein
    /// Listenplatz, 0 ist Montag.
    /// </summary>
    public int? Wochentag
    {
        get => WochentagLesen?.Invoke();
        set => WochentagSetzen?.Invoke(value);
    }

    /// <summary>Der Freitext des geladenen Typs; er geht mit dem Speichern mit.</summary>
    public string Beschreibung
    {
        get => BeschreibungLesen?.Invoke() ?? "";
        set => BeschreibungSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Die Zahlenreihe der 168 übernommenen Wochenwerte (Welle #458 Stufe 3b) — die
    /// Stelle (Tag − 1) · 24 + Stunde, Montag Stunde 1 ist die erste.
    /// </summary>
    public double?[]? Wochenwerte
    {
        get => WochenwerteLesen?.Invoke();
        set { if (value is not null) WochenwerteSetzen?.Invoke(value); }
    }
}
