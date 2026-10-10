using System.Globalization;
using KiKern;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Die Sicht des Assistenten auf die Verwaltung „Kältemaschinen"</b> (KU3-1; Feldkarte
/// <c>KiDialoge.KaeltemaschineKatalog</c>). Die Satzwahl nimmt den Weg eines Klicks, die Kenndaten und
/// die Kennlinie schreiben in den Arbeitsstand des Stammblatts — geschrieben wird erst mit „Speichern".
///
/// <para><b>Die Rückkühlart ist eine Wahl über den Listenplatz</b> (<see cref="RueckkuehlartWahl"/>,
/// Schlüssel „0" bis „3"); ein Anzeigetext ist nie ein Steuerwert. <b>Die Kennlinie ist ein Raster</b>
/// (<see cref="Kennlinie"/>, Kennzeichen die Nummer ab 1); Punkte anlegen und entfernen bleiben Klicks.</para>
/// </summary>
public sealed class KaeltemaschineKatalogKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Liest den Schlüssel (die Id) des gewählten Satzes.</summary>
    public Func<string>? SatzLesen { get; init; }

    /// <summary>Wählt einen Satz wie ein Klick in die Liste.</summary>
    public Action<string>? SatzSetzen { get; init; }

    /// <summary>Die Einträge der Satzwahl.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    /// <summary>Die Anzeigetexte der Rückkühlarten in der Reihenfolge der Listenplätze.</summary>
    public Func<IReadOnlyList<string>>? Rueckkuehlarten { get; init; }

    /// <summary>Die Anzeigetexte der Teillastwege in der Folge der Listenplätze (KM3-E3-a).</summary>
    public Func<IReadOnlyList<string>>? TeillastWege { get; init; }

    /// <summary>Die Anzeigetexte der Verdichterregelungen in der Folge der Listenplätze (KM3-E3-a).</summary>
    public Func<IReadOnlyList<string>>? Verdichterregelungen { get; init; }

    /// <summary>Die Anzeigetexte der Wege am Kennfeldrand in der Folge der Listenplätze (KM3-E3-a).</summary>
    public Func<IReadOnlyList<string>>? Randwege { get; init; }

    /// <summary>Die Anzeigetexte der Gerätearten in der Folge der Listenplätze (K-A).</summary>
    public Func<IReadOnlyList<string>>? Geraetearten { get; init; }

    /// <summary>Die Anzeigetexte der Arten der saisonalen Kennzahl in der Folge der Listenplätze (K-A).</summary>
    public Func<IReadOnlyList<string>>? SaisonArten { get; init; }

    /// <summary>Der Arbeitsstand des Stammblatts; <c>null</c> ohne Satz.</summary>
    public Func<KaeltemaschineDaten?>? ArbeitLesen { get; init; }

    /// <summary>Nach jeder Änderung — der Dialog zieht „geändert" nach.</summary>
    public Action? Geaendert { get; init; }

    // =====================================================================
    //  Satzwahl
    // =====================================================================

    /// <summary>Die Wahl des Katalogsatzes (Schlüssel = Id).</summary>
    public string Kaeltemaschine
    {
        get => SatzLesen?.Invoke() ?? "";
        set => SatzSetzen?.Invoke(value ?? "");
    }

    /// <summary>Begleiteigenschaft der Satzwahl.</summary>
    public IReadOnlyList<KiWahleintrag> KaeltemaschineWahl
        => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Kenndaten
    // =====================================================================

    public string Bezeichner { get => Lies(d => d.Bezeichner) ?? ""; set => Setzen(d => d.Bezeichner = value ?? ""); }

    public string Firma { get => Lies(d => d.Firma) ?? ""; set => Setzen(d => d.Firma = value ?? ""); }

    public string Typ { get => Lies(d => d.Typ) ?? ""; set => Setzen(d => d.Typ = value ?? ""); }

    public string Beschreibung { get => Lies(d => d.Beschreibung) ?? ""; set => Setzen(d => d.Beschreibung = value ?? ""); }

    public double? Nennkaelteleistung { get => ArbeitLesen?.Invoke()?.Nennkaelteleistung; set => Setzen(d => d.Nennkaelteleistung = value); }

    public double? NennEer { get => ArbeitLesen?.Invoke()?.NennEer; set => Setzen(d => d.NennEer = value); }

    public string Kaeltemittel { get => Lies(d => d.Kaeltemittel) ?? ""; set => Setzen(d => d.Kaeltemittel = value ?? ""); }

    /// <summary>Der Listenplatz der Rückkühlart als Schlüssel („0" bis „3"); leer = keine Angabe.</summary>
    public string Rueckkuehlart
    {
        get => ArbeitLesen?.Invoke()?.RueckkuehlartIndex?.ToString(CultureInfo.InvariantCulture) ?? "";
        set
        {
            int anzahl = Rueckkuehlarten?.Invoke()?.Count ?? 0;
            int? platz = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                         && n >= 0 && n < anzahl ? n : null;
            Setzen(d => d.RueckkuehlartIndex = platz);
        }
    }

    /// <summary>Begleiteigenschaft der Rückkühlart: Listenplatz und Anzeigetext.</summary>
    public IReadOnlyList<KiWahleintrag> RueckkuehlartWahl
        => (Rueckkuehlarten?.Invoke() ?? Array.Empty<string>())
           .Select((t, i) => new KiWahleintrag(i.ToString(CultureInfo.InvariantCulture), t))
           .ToList();

    public double? Mindestteillast { get => ArbeitLesen?.Invoke()?.Mindestteillast; set => Setzen(d => d.Mindestteillast = value); }

    public double? Hilfsstrom { get => ArbeitLesen?.Invoke()?.Hilfsstrom; set => Setzen(d => d.Hilfsstrom = value); }

    public double? KaltwasserMin { get => ArbeitLesen?.Invoke()?.KaltwasserMin; set => Setzen(d => d.KaltwasserMin = value); }

    public double? Modulkosten { get => ArbeitLesen?.Invoke()?.Modulkosten; set => Setzen(d => d.Modulkosten = value); }

    // =====================================================================
    //  Katalogfelder (K-A)
    // =====================================================================

    /// <summary>Geräteart als Listenplatz (Text); leer = nach der Rückkühlart.</summary>
    public string Geraeteart { get => PlatzLesen(d => d.GeraeteartIndex); set => Setzen(d => d.GeraeteartIndex = Platz(value, Geraetearten)); }

    /// <summary>Die Einträge der Geräteart.</summary>
    public IReadOnlyList<KiWahleintrag> GeraeteartWahl => Wahl(Geraetearten);

    /// <summary>GWP des Kältemittels.</summary>
    public double? Gwp { get => ArbeitLesen?.Invoke()?.Gwp; set => Setzen(d => d.Gwp = value); }

    /// <summary>Füllmenge des Kältemittels [kg].</summary>
    public double? Fuellmenge { get => ArbeitLesen?.Invoke()?.Fuellmenge; set => Setzen(d => d.Fuellmenge = value); }

    /// <summary>Art der saisonalen Kennzahl als Listenplatz (Text); leer = keine Angabe.</summary>
    public string SaisonArt { get => PlatzLesen(d => d.SaisonArtIndex); set => Setzen(d => d.SaisonArtIndex = Platz(value, SaisonArten)); }

    /// <summary>Die Einträge der Art der saisonalen Kennzahl.</summary>
    public IReadOnlyList<KiWahleintrag> SaisonArtWahl => Wahl(SaisonArten);

    /// <summary>Wert der saisonalen Kennzahl.</summary>
    public double? Saisonkennzahl { get => ArbeitLesen?.Invoke()?.Saisonkennzahl; set => Setzen(d => d.Saisonkennzahl = value); }

    // =====================================================================
    //  Teillast und Takten (KM3-E3-a) — die acht Felder der Gruppe
    // =====================================================================

    /// <summary>Teillastrechnung als Listenplatz (Text); leer = wie bisher.</summary>
    public string TeillastWeg { get => PlatzLesen(d => d.TeillastWegIndex); set => Setzen(d => d.TeillastWegIndex = Platz(value, TeillastWege)); }

    /// <summary>Die Einträge der Teillastrechnung.</summary>
    public IReadOnlyList<KiWahleintrag> TeillastWegWahl => Wahl(TeillastWege);

    /// <summary>Beiwert a der Teillastkurve.</summary>
    public double? KurveA { get => ArbeitLesen?.Invoke()?.KurveA; set => Setzen(d => d.KurveA = value); }

    /// <summary>Beiwert b der Teillastkurve.</summary>
    public double? KurveB { get => ArbeitLesen?.Invoke()?.KurveB; set => Setzen(d => d.KurveB = value); }

    /// <summary>Beiwert c der Teillastkurve.</summary>
    public double? KurveC { get => ArbeitLesen?.Invoke()?.KurveC; set => Setzen(d => d.KurveC = value); }

    /// <summary>Lastgrad, ab dem die Kurve gilt (0 … 1).</summary>
    public double? KurveLastgradMin { get => ArbeitLesen?.Invoke()?.KurveLastgradMin; set => Setzen(d => d.KurveLastgradMin = value); }

    /// <summary>Taktverlustfaktor C_d (0 … 1); leer = 0,9.</summary>
    public double? Cd { get => ArbeitLesen?.Invoke()?.Cd; set => Setzen(d => d.Cd = value); }

    /// <summary>Verdichterregelung als Listenplatz (Text); leer = keine Angabe.</summary>
    public string Verdichterregelung
    {
        get => PlatzLesen(d => d.VerdichterregelungIndex);
        set => Setzen(d => d.VerdichterregelungIndex = Platz(value, Verdichterregelungen));
    }

    /// <summary>Die Einträge der Verdichterregelung.</summary>
    public IReadOnlyList<KiWahleintrag> VerdichterregelungWahl => Wahl(Verdichterregelungen);

    /// <summary>Weg am Kennfeldrand als Listenplatz (Text); leer = Randwert.</summary>
    public string Kennfeldrand { get => PlatzLesen(d => d.RandwegIndex); set => Setzen(d => d.RandwegIndex = Platz(value, Randwege)); }

    /// <summary>Die Einträge des Kennfeldrands.</summary>
    public IReadOnlyList<KiWahleintrag> KennfeldrandWahl => Wahl(Randwege);

    private string PlatzLesen(Func<KaeltemaschineDaten, int?> wert)
        => ArbeitLesen?.Invoke() is KaeltemaschineDaten d ? wert(d)?.ToString(CultureInfo.InvariantCulture) ?? "" : "";

    private static int? Platz(string? text, Func<IReadOnlyList<string>>? liste)
    {
        int anzahl = liste?.Invoke()?.Count ?? 0;
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0 && n < anzahl ? n : null;
    }

    private static IReadOnlyList<KiWahleintrag> Wahl(Func<IReadOnlyList<string>>? liste)
        => (liste?.Invoke() ?? Array.Empty<string>())
           .Select((t, i) => new KiWahleintrag(i.ToString(CultureInfo.InvariantCulture), t))
           .ToList();

    // =====================================================================
    //  Kennlinie
    // =====================================================================

    /// <summary>Die Kennlinienpunkte — je Zugriff neu über dem Arbeitsstand.</summary>
    public IReadOnlyList<KaeltemaschinePunktKiZeile> Kennlinie
    {
        get
        {
            KaeltemaschineDaten? d = ArbeitLesen?.Invoke();
            if (d is null) return Array.Empty<KaeltemaschinePunktKiZeile>();
            return Enumerable.Range(0, d.Kennlinie.Count)
                             .Select(i => new KaeltemaschinePunktKiZeile(d, i, Geaendert))
                             .ToList();
        }
    }

    private string? Lies(Func<KaeltemaschineDaten, string> wert)
        => ArbeitLesen?.Invoke() is KaeltemaschineDaten d ? wert(d) : null;

    private void Setzen(Action<KaeltemaschineDaten> schritt)
    {
        KaeltemaschineDaten? d = ArbeitLesen?.Invoke();
        if (d is null) return;
        schritt(d);
        Geaendert?.Invoke();
    }
}

/// <summary>Eine Zeile des Kennlinienrasters in der Sicht des Assistenten.</summary>
public sealed class KaeltemaschinePunktKiZeile
{
    private readonly KaeltemaschineDaten _stand;
    private readonly int _index;
    private readonly Action? _geaendert;

    internal KaeltemaschinePunktKiZeile(KaeltemaschineDaten stand, int index, Action? geaendert)
    {
        _stand = stand;
        _index = index;
        _geaendert = geaendert;
    }

    private KaeltemaschinePunktDaten Punkt => _stand.Kennlinie[_index];

    /// <summary>Das Zeilenkennzeichen: die Nummer ab 1.</summary>
    public string Nummer => (_index + 1).ToString(CultureInfo.InvariantCulture);

    public double? Rueckkuehltemperatur { get => Punkt.Rueckkuehltemperatur; set => Setzen(() => Punkt.Rueckkuehltemperatur = value); }

    public double? Kaltwassertemperatur { get => Punkt.Kaltwassertemperatur; set => Setzen(() => Punkt.Kaltwassertemperatur = value); }

    public double? Eer { get => Punkt.Eer; set => Setzen(() => Punkt.Eer = value); }

    public double? Kaelteleistung { get => Punkt.Kaelteleistung; set => Setzen(() => Punkt.Kaelteleistung = value); }

    private void Setzen(Action schritt)
    {
        schritt();
        _geaendert?.Invoke();
    }
}
