using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Verwaltung „Betriebskalender" für den Hilfe-Assistenten
/// (Entscheidungsvorlage Modellgrenzen PW2, BW2).
///
/// <para><b>Ein Satz, kein Raster.</b> Die Maske zeigt EINEN Kalender zur Zeit; gewählt wird er über
/// <see cref="Kalender"/>. Die vier Ferienzeiträume stehen als Jahrestage 1 … 365 im Gemeinjahr da
/// (<c>Ferien1Von</c> … <c>Ferien4Bis</c>), die weiteren Betriebsferien als Tabelle <see cref="WeitereFerien"/>
/// (Name, von, bis je Zeile; Zeilen fügt der Anwender an), der Ferienfaktor in Prozent. Geschrieben wird erst mit
/// „Speichern" oder OK.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class BetriebskalenderKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? KalenderLesen { get; init; }
    public Action<int?>? KalenderSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KalenderEintraege { get; init; }

    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }

    public Func<string>? BundeslandLesen { get; init; }
    public Action<string>? BundeslandSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BundeslandEintraege { get; init; }

    public Func<bool>? FeiertagLesen { get; init; }
    public Action<bool>? FeiertagSetzen { get; init; }
    public Func<bool>? FerienSonntagLesen { get; init; }
    public Action<bool>? FerienSonntagSetzen { get; init; }

    public Func<int, int?>? VonLesen { get; init; }
    public Action<int, int?>? VonSetzen { get; init; }
    public Func<int, int?>? BisLesen { get; init; }
    public Action<int, int?>? BisSetzen { get; init; }
    public Func<IReadOnlyList<BetriebsferienKiZeile>>? ListeLesen { get; init; }

    public Func<double?>? FaktorLesen { get; init; }
    public Action<double?>? FaktorSetzen { get; init; }

    public Func<bool>? KuerzenLesen { get; init; }
    public Action<bool>? KuerzenSetzen { get; init; }

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der gezeigte Kalender (ID); leer = ein neuer, noch nicht gespeicherter.</summary>
    public int? Kalender
    {
        get => KalenderLesen?.Invoke();
        set => KalenderSetzen?.Invoke(value);
    }

    /// <summary>Die Kalender zur Wahl.</summary>
    public IReadOnlyList<KiWahleintrag> KalenderWahl => KalenderEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bezeichnung des Kalenders.</summary>
    public string Bezeichnung
    {
        get => BezeichnungLesen?.Invoke() ?? "";
        set => BezeichnungSetzen?.Invoke(value);
    }

    /// <summary>Die Länderkennung (BW … TH); leer = nur die bundeseinheitlichen Feiertage.</summary>
    public string Bundesland
    {
        get => BundeslandLesen?.Invoke() ?? "";
        set => BundeslandSetzen?.Invoke(value);
    }

    /// <summary>Die Länder zur Wahl.</summary>
    public IReadOnlyList<KiWahleintrag> BundeslandWahl => BundeslandEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Feiertage rechnen wie ein Sonntag des Wochenprofils.</summary>
    public bool FeiertagWieSonntag
    {
        get => FeiertagLesen?.Invoke() ?? true;
        set => FeiertagSetzen?.Invoke(value);
    }

    public int? Ferien1Von { get => VonLesen?.Invoke(0); set => VonSetzen?.Invoke(0, value); }
    public int? Ferien1Bis { get => BisLesen?.Invoke(0); set => BisSetzen?.Invoke(0, value); }
    public int? Ferien2Von { get => VonLesen?.Invoke(1); set => VonSetzen?.Invoke(1, value); }
    public int? Ferien2Bis { get => BisLesen?.Invoke(1); set => BisSetzen?.Invoke(1, value); }
    public int? Ferien3Von { get => VonLesen?.Invoke(2); set => VonSetzen?.Invoke(2, value); }
    public int? Ferien3Bis { get => BisLesen?.Invoke(2); set => BisSetzen?.Invoke(2, value); }
    public int? Ferien4Von { get => VonLesen?.Invoke(3); set => VonSetzen?.Invoke(3, value); }
    public int? Ferien4Bis { get => BisLesen?.Invoke(3); set => BisSetzen?.Invoke(3, value); }

    /// <summary>
    /// Die weiteren Betriebsferien (Liste „Weitere Betriebsferien“) als TABELLE — Zeilen mit der Nummer als Kennzeichen,
    /// Spalten Name, von und bis (Jahrestag). Die Zellen sind dieselben Felder wie die Eingaben der Maske.
    /// </summary>
    public IReadOnlyList<BetriebsferienKiZeile> WeitereFerien
        => ListeLesen?.Invoke() ?? Array.Empty<BetriebsferienKiZeile>();

    /// <summary>Der Ferienfaktor in Prozent des Tagesmittels (0 … 100).</summary>
    public double? Ferienfaktor
    {
        get => FaktorLesen?.Invoke();
        set => FaktorSetzen?.Invoke(value);
    }

    /// <summary>Ferientage rechnen wie ein Sonntag des Wochenprofils (der Ferienfaktor wirkt dann nicht).</summary>
    public bool FerienWieSonntag
    {
        get => FerienSonntagLesen?.Invoke() ?? false;
        set => FerienSonntagSetzen?.Invoke(value);
    }

    /// <summary>Kürzen die Ferien die Monatsmenge?</summary>
    public bool FerienKuerzen
    {
        get => KuerzenLesen?.Invoke() ?? false;
        set => KuerzenSetzen?.Invoke(value);
    }
}

/// <summary>
/// EINE Zeile der Liste „Weitere Betriebsferien“ für den Hilfe-Assistenten — eine Zeile der Tabelle
/// <see cref="BetriebskalenderKiSicht.WeitereFerien"/>. Sie hält keinen Zustand: Jede Zelle liest und schreibt über ihren
/// Delegaten den Arbeitsstand der Maske.
/// </summary>
public sealed class BetriebsferienKiZeile
{
    public Func<string>? NummerLesen { get; init; }
    public Func<string>? NameLesen { get; init; }
    public Action<string>? NameSetzen { get; init; }
    public Func<int?>? VonLesen { get; init; }
    public Action<int?>? VonSetzen { get; init; }
    public Func<int?>? BisLesen { get; init; }
    public Action<int?>? BisSetzen { get; init; }

    /// <summary>Die Nummer der Zeile (1 …) — das Zeilenkennzeichen.</summary>
    public string Nummer => NummerLesen?.Invoke() ?? "";

    /// <summary>Der wahlfreie Name des Zeitraums.</summary>
    public string Name
    {
        get => NameLesen?.Invoke() ?? "";
        set => NameSetzen?.Invoke(value);
    }

    /// <summary>Beginn als Tag im Jahr (1 bis 365); leer = nicht eingetragen.</summary>
    public int? Von
    {
        get => VonLesen?.Invoke();
        set => VonSetzen?.Invoke(value);
    }

    /// <summary>Ende als Tag im Jahr (1 bis 365); leer = nicht eingetragen.</summary>
    public int? Bis
    {
        get => BisLesen?.Invoke();
        set => BisSetzen?.Invoke(value);
    }
}
