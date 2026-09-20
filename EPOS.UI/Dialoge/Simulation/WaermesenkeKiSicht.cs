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
}
