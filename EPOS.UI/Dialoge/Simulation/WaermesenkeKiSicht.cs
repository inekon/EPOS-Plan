using KiKern;

namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Das FLACHE Abbild der Maske „Wärmesenken" für den Hilfe-Assistenten (Welle KI‑F2) —
/// die Werte der GEWÄHLTEN Zeile der Senkenliste.
///
/// <para><b>Warum ein Sichtmodell und nicht <c>SenkenzeileDaten</c>.</b> Eine Zeile der
/// Liste ist ein unveränderlicher Record: Jede Änderung ERSETZT den Listeneintrag über
/// einen <c>with</c>-Ausdruck. Ein an der Zeile angemeldeter Katalog schriebe in ein
/// Objekt, das die Liste im nächsten Zeichenlauf nicht mehr führt. Diese Klasse legt
/// sich statt dessen über die Bedienelemente der Maske; jeder Setzweg nimmt denselben
/// Weg wie die Tastatur und schreibt die Zeile ordentlich zurück.</para>
///
/// <para><b>Die gewählte Zeile wechselt mit jedem Klick in der Liste</b> — deshalb
/// Delegaten und keine festgehaltene Zeile: gelesen wird bei jedem Zugriff neu.</para>
/// </summary>
public sealed class WaermesenkeKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? ZielLesen { get; init; }
    public Action<string>? ZielSetzen { get; init; }

    public Func<string>? BedarfsartLesen { get; init; }
    public Action<string>? BedarfsartSetzen { get; init; }

    public Func<int>? LadeprioritaetLesen { get; init; }
    public Action<int>? LadeprioritaetSetzen { get; init; }

    public Func<int>? LadeprioritaetPvLesen { get; init; }
    public Action<int>? LadeprioritaetPvSetzen { get; init; }

    public Func<bool>? LadegrenzeAktivLesen { get; init; }
    public Action<bool>? LadegrenzeAktivSetzen { get; init; }

    public Func<double?>? LadegrenzeLesen { get; init; }
    public Action<double?>? LadegrenzeSetzen { get; init; }

    public Func<bool>? EinspeisehoeheAktivLesen { get; init; }
    public Action<bool>? EinspeisehoeheAktivSetzen { get; init; }

    public Func<double?>? EinspeisehoeheLesen { get; init; }
    public Action<double?>? EinspeisehoeheSetzen { get; init; }

    public Func<int>? SpeicherLesen { get; init; }
    public Action<int>? SpeicherSetzen { get; init; }

    // =====================================================================
    //  Die Einträge der fünf Wahlfelder (KI-F1b)
    // =====================================================================

    /// <summary>Liefert die Ziele, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? ZielEintraege { get; init; }

    /// <summary>Liefert die Bedarfsarten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BedarfsartEintraege { get; init; }

    /// <summary>Liefert die Ladeprioritäten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? LadeprioritaetEintraege { get; init; }

    /// <summary>Liefert die PV-Sonderprioritäten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? LadeprioritaetPvEintraege { get; init; }

    /// <summary>Liefert die Speicher, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SpeicherEintraege { get; init; }

    /// <summary>Heizkreis und Pufferplätze — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> ZielWahl
        => ZielEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bedarfsarten des Heizkreises — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> BedarfsartWahl
        => BedarfsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>„nach Vorgabe" und die Ränge 1…9 — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> LadeprioritaetWahl
        => LadeprioritaetEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>„unverändert" und die Ränge 1…9 — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> LadeprioritaetPvWahl
        => LadeprioritaetPvEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Die wählbaren Pufferspeicher — die Auswahl der Maske (KI-F1b). Die gesperrten
    /// GRUPPENKÖPFE der Klappliste stehen nicht darin; sie sind Überschriften und
    /// keine Wahl.
    /// </summary>
    public IReadOnlyList<KiWahleintrag> SpeicherWahl
        => SpeicherEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der gewählten Zeile
    // =====================================================================

    /// <summary>
    /// Der Steuerwert des Ziels — Heizkreis oder einer der Pufferplätze; ein
    /// unbekannter Wert wird abgewiesen.
    /// </summary>
    public string Ziel
    {
        get => ZielLesen?.Invoke() ?? "";
        set => ZielSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Der Steuerwert der Bedarfsart — nur beim Heizkreis sichtbar; ein unbekannter
    /// Wert wird abgewiesen.
    /// </summary>
    public string Bedarfsart
    {
        get => BedarfsartLesen?.Invoke() ?? "";
        set => BedarfsartSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Ladepriorität dieser Senke; 0 heißt „nach Vorgabe".</summary>
    public int Ladeprioritaet
    {
        get => LadeprioritaetLesen?.Invoke() ?? 0;
        set => LadeprioritaetSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die PV-Sonderpriorität; 0 heißt „unverändert". Es gibt sie nur auf Rang 1.
    /// </summary>
    public int LadeprioritaetPv
    {
        get => LadeprioritaetPvLesen?.Invoke() ?? 0;
        set => LadeprioritaetPvSetzen?.Invoke(value);
    }

    /// <summary>Führt diese Senke eine eigene Ladeobergrenze?</summary>
    public bool LadegrenzeAktiv
    {
        get => LadegrenzeAktivLesen?.Invoke() ?? false;
        set => LadegrenzeAktivSetzen?.Invoke(value);
    }

    /// <summary>Die eigene Ladeobergrenze [%]; sie gilt nur bei eingeschaltetem Schalter.</summary>
    public double? Ladegrenze
    {
        get => LadegrenzeLesen?.Invoke();
        set => LadegrenzeSetzen?.Invoke(value);
    }

    /// <summary>Führt diese Senke eine eigene Einspeisehöhe?</summary>
    public bool EinspeisehoeheAktiv
    {
        get => EinspeisehoeheAktivLesen?.Invoke() ?? false;
        set => EinspeisehoeheAktivSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die eigene Einspeisehöhe im Speicher, 0 (ganz unten) bis 1 (ganz oben); sie gilt
    /// nur bei eingeschaltetem Schalter.
    /// </summary>
    public double? Einspeisehoehe
    {
        get => EinspeisehoeheLesen?.Invoke();
        set => EinspeisehoeheSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Id des Pufferspeichers dieser Senke; <c>0</c> heißt „keiner". Es gibt ihn
    /// nur, wenn das Ziel ein Pufferplatz ist.
    /// </summary>
    public int Speicher
    {
        get => SpeicherLesen?.Invoke() ?? 0;
        set => SpeicherSetzen?.Invoke(value);
    }
}
