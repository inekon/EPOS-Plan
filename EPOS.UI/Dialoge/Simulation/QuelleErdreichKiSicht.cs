using KiKern;

namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Das FLACHE Abbild der Maske „Wärmequelle Erdreich" für den Hilfe-Assistenten
/// (Welle KI‑F2).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>QuelleErdreichDaten</c>.</b> Jener Satz
/// ist ein unveränderlicher Record (<c>init</c>-Eigenschaften): Der Dialog liest ihn
/// beim Aufbau in seine Eingabefelder und erzeugt beim OK über <c>with</c> einen NEUEN
/// Satz — in den hereingereichten schreibt er nie. Ein daran angemeldeter Katalog
/// zeigte dem Assistenten den Stand von vorhin und setzte in ein Objekt, das niemand
/// mehr liest. Diese Klasse legt sich statt dessen über die LEBENDEN Felder der Maske.</para>
///
/// <para><b>Setzen heißt tippen.</b> Jeder Setzweg geht denselben Weg wie die Tastatur —
/// die Vorschau, die Bodenkennwerte und die Auslegungsprüfung ziehen danach nach, genau
/// wie nach einer Eingabe von Hand.</para>
///
/// <para><b>Sie hält keinen Zustand</b> (Muster <c>SimulationKiSicht</c>): Jede
/// Eigenschaft ruft bei jedem Zugriff ihren Delegaten.</para>
/// </summary>
public sealed class QuelleErdreichKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<bool>? ErdsondeLesen { get; init; }
    public Action<bool>? ErdsondeSetzen { get; init; }

    public Func<double?>? VerlegetiefeLesen { get; init; }
    public Action<double?>? VerlegetiefeSetzen { get; init; }

    public Func<double?>? FlaecheLesen { get; init; }
    public Action<double?>? FlaecheSetzen { get; init; }

    public Func<double?>? LaengeSondeLesen { get; init; }
    public Action<double?>? LaengeSondeSetzen { get; init; }

    public Func<int?>? AnzahlSondenLesen { get; init; }
    public Action<int?>? AnzahlSondenSetzen { get; init; }

    public Func<double?>? SpreizungLesen { get; init; }
    public Action<double?>? SpreizungSetzen { get; init; }

    public Func<string>? BodentypLesen { get; init; }
    public Action<string>? BodentypSetzen { get; init; }

    public Func<int?>? KlimazoneLesen { get; init; }
    public Action<int?>? KlimazoneSetzen { get; init; }

    // =====================================================================
    //  Die Einträge der beiden Wahlfelder (KI-F1b)
    // =====================================================================

    /// <summary>Liefert die Bodentypen, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BodentypEintraege { get; init; }

    /// <summary>Liefert die Klimazonen, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KlimazoneEintraege { get; init; }

    /// <summary>Die Bodentypen des Katalogs — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> BodentypWahl
        => BodentypEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Zone 0 und die Zonen 1…15 mit ihren Volllaststunden (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> KlimazoneWahl
        => KlimazoneEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// <c>true</c> = Erdsonde, <c>false</c> = Erdkollektor. Die Wahl SPERRT nur — der
    /// andere Zweig behält seine Werte.
    /// </summary>
    public bool Erdsonde
    {
        get => ErdsondeLesen?.Invoke() ?? false;
        set => ErdsondeSetzen?.Invoke(value);
    }

    /// <summary>Die Verlegetiefe des Erdkollektors [m].</summary>
    public double? Verlegetiefe
    {
        get => VerlegetiefeLesen?.Invoke();
        set => VerlegetiefeSetzen?.Invoke(value);
    }

    /// <summary>Die Fläche des Erdkollektors [m²].</summary>
    public double? Flaeche
    {
        get => FlaecheLesen?.Invoke();
        set => FlaecheSetzen?.Invoke(value);
    }

    /// <summary>Die Länge JE Sonde [m].</summary>
    public double? LaengeSonde
    {
        get => LaengeSondeLesen?.Invoke();
        set => LaengeSondeSetzen?.Invoke(value);
    }

    /// <summary>Die Zahl der Erdsonden; mindestens 1.</summary>
    public int? AnzahlSonden
    {
        get => AnzahlSondenLesen?.Invoke();
        set => AnzahlSondenSetzen?.Invoke(value);
    }

    /// <summary>Die nutzbare Spreizung der Quelle [K]; sie muss größer als 0 sein.</summary>
    public double? Spreizung
    {
        get => SpreizungLesen?.Invoke();
        set => SpreizungSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der KATALOGSCHLÜSSEL des gewählten Bodens (<c>ton_nass</c>, <c>granit</c> …) und
    /// nicht sein Platz in der Anzeigeliste: Wird der Katalog umsortiert, zeigte ein
    /// Listenplatz danach auf den falschen Boden (Abweichung A-3 der Maske).
    /// </summary>
    public string Bodentyp
    {
        get => BodentypLesen?.Invoke() ?? "";
        set => BodentypSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Klimazone des Standorts, 1…15; 0 heißt „nicht zugeordnet".</summary>
    public int? Klimazone
    {
        get => KlimazoneLesen?.Invoke();
        set => KlimazoneSetzen?.Invoke(value);
    }
}
