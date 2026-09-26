using System.Globalization;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild des <see cref="ZonenDialog"/> für den Hilfe-Assistenten (Gebäudesimulation G3,
/// Welle D2; Stufe G6b W2): Bezeichnung, Nutzfläche und die Werte der Zone (Raumhöhe, Volumen,
/// beheizt, Sollwerte, Lüftung, Gewinne, Bewohner, Heizung) sind setzbar, die Bauteile ein RASTER zum Lesen
/// (Sammlungsform <c>Bauteile[]</c>, Kennzeichen die Nummer) — anlegen, öffnen und entfernen bleiben
/// Klicks des Anwenders, die Werte eines Bauteils setzt der Assistent im Bauteildialog. Sie hält
/// keinen Zustand: Jede Eigenschaft ruft bei jedem Zugriff ihren Delegaten.
/// </summary>
public sealed class ZonenKiSicht
{
    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }

    public Func<double?>? NutzflaecheLesen { get; init; }
    public Action<double?>? NutzflaecheSetzen { get; init; }

    // Stufe G6b (W2): die Werte der Zone; leer = der Wert des Gebaeudes (Vorgabenkaskade).
    public Func<double?>? RaumhoeheLesen { get; init; }
    public Action<double?>? RaumhoeheSetzen { get; init; }
    public Func<double?>? VolumenLesen { get; init; }
    public Action<double?>? VolumenSetzen { get; init; }
    public Func<bool>? BeheiztLesen { get; init; }
    public Action<bool>? BeheiztSetzen { get; init; }
    public Func<double?>? SollTagLesen { get; init; }
    public Action<double?>? SollTagSetzen { get; init; }
    public Func<double?>? SollNachtLesen { get; init; }
    public Action<double?>? SollNachtSetzen { get; init; }
    public Func<double?>? SollWochenendeLesen { get; init; }
    public Action<double?>? SollWochenendeSetzen { get; init; }
    public Func<double?>? SollFerienLesen { get; init; }
    public Action<double?>? SollFerienSetzen { get; init; }
    public Func<double?>? MaxTemperaturLesen { get; init; }
    public Action<double?>? MaxTemperaturSetzen { get; init; }
    public Func<double?>? InfiltrationLesen { get; init; }
    public Action<double?>? InfiltrationSetzen { get; init; }
    public Func<double?>? NutzerlueftungLesen { get; init; }
    public Action<double?>? NutzerlueftungSetzen { get; init; }
    public Func<double?>? GewinneLesen { get; init; }
    public Action<double?>? GewinneSetzen { get; init; }
    public Func<double?>? BewohnerLesen { get; init; }
    public Action<double?>? BewohnerSetzen { get; init; }
    public Func<double?>? StrahlungsanteilLesen { get; init; }
    public Action<double?>? StrahlungsanteilSetzen { get; init; }
    public Func<double?>? HeizleistungMaxLesen { get; init; }
    public Action<double?>? HeizleistungMaxSetzen { get; init; }

    public Func<IReadOnlyList<BauteilDaten>>? BauteileLesen { get; init; }

    /// <summary>Der Anzeigetext einer Bauteilart.</summary>
    public Func<string, string>? ArtText { get; init; }

    /// <summary>Der Anzeigetext der Randbedingung eines Bauteils (Stufe G6b).</summary>
    public Func<BauteilDaten, string>? RandText { get; init; }

    /// <summary>Der Name der Nachbarzone eines Bauteils; „—" ohne (Stufe G6b).</summary>
    public Func<BauteilDaten, string>? NachbarText { get; init; }

    /// <summary>Der Name der Zone.</summary>
    public string Bezeichnung { get => BezeichnungLesen?.Invoke() ?? ""; set => BezeichnungSetzen?.Invoke(value); }

    /// <summary>Nutzfläche der Zone [m²]; leer = die des Gebäudes.</summary>
    public double? Nutzflaeche { get => NutzflaecheLesen?.Invoke(); set => NutzflaecheSetzen?.Invoke(value); }

    /// <summary>Raumhöhe [m]; leer = die des Gebäudes.</summary>
    public double? Raumhoehe { get => RaumhoeheLesen?.Invoke(); set => RaumhoeheSetzen?.Invoke(value); }

    /// <summary>Luftvolumen [m³]; leer = Nutzfläche × Raumhöhe.</summary>
    public double? Volumen { get => VolumenLesen?.Invoke(); set => VolumenSetzen?.Invoke(value); }

    /// <summary>Wird die Zone beheizt? Nein = sie schwingt frei.</summary>
    public bool Beheizt { get => BeheiztLesen?.Invoke() ?? true; set => BeheiztSetzen?.Invoke(value); }

    /// <summary>Raumsolltemperatur am Tag [°C]; leer = die des Gebäudes.</summary>
    public double? SollTag { get => SollTagLesen?.Invoke(); set => SollTagSetzen?.Invoke(value); }

    /// <summary>Nachtabsenkung auf [°C]; leer = die des Gebäudes.</summary>
    public double? SollNacht { get => SollNachtLesen?.Invoke(); set => SollNachtSetzen?.Invoke(value); }

    /// <summary>Wochenendabsenkung [°C]; leer = die des Gebäudes.</summary>
    public double? SollWochenende { get => SollWochenendeLesen?.Invoke(); set => SollWochenendeSetzen?.Invoke(value); }

    /// <summary>Sollwert in den Ferien [°C]; leer = der des Gebäudes.</summary>
    public double? SollFerien { get => SollFerienLesen?.Invoke(); set => SollFerienSetzen?.Invoke(value); }

    /// <summary>Maximalraumtemperatur [°C]; leer = die des Gebäudes.</summary>
    public double? MaxTemperatur { get => MaxTemperaturLesen?.Invoke(); set => MaxTemperaturSetzen?.Invoke(value); }

    /// <summary>Infiltration [1/h]; leer = die des Gebäudes.</summary>
    public double? Infiltration { get => InfiltrationLesen?.Invoke(); set => InfiltrationSetzen?.Invoke(value); }

    /// <summary>Nutzerlüftung [1/h]; leer = die des Gebäudes.</summary>
    public double? Nutzerlueftung { get => NutzerlueftungLesen?.Invoke(); set => NutzerlueftungSetzen?.Invoke(value); }

    /// <summary>Innere Wärmegewinne [W]; leer = die des Gebäudes nach dem Flächenschlüssel.</summary>
    public double? Gewinne { get => GewinneLesen?.Invoke(); set => GewinneSetzen?.Invoke(value); }

    /// <summary>Bewohner; leer = die des Gebäudes nach dem Flächenschlüssel.</summary>
    public double? Bewohner { get => BewohnerLesen?.Invoke(); set => BewohnerSetzen?.Invoke(value); }

    /// <summary>Strahlungsanteil der Heizung [–]; leer = der des Gebäudes.</summary>
    public double? Strahlungsanteil { get => StrahlungsanteilLesen?.Invoke(); set => StrahlungsanteilSetzen?.Invoke(value); }

    /// <summary>Leistungsgrenze der Heizung [kW]; leer = die des Gebäudes (ab zwei Zonen anteilig).</summary>
    public double? HeizleistungMax { get => HeizleistungMaxLesen?.Invoke(); set => HeizleistungMaxSetzen?.Invoke(value); }

    /// <summary>Die Bauteile — je Zugriff neu über dem Arbeitsstand, nur lesbar.</summary>
    public IReadOnlyList<ZonenBauteilKiZeile> Bauteile
    {
        get
        {
            IReadOnlyList<BauteilDaten> liste = BauteileLesen?.Invoke() ?? Array.Empty<BauteilDaten>();
            return liste.Select((b, i) => new ZonenBauteilKiZeile(b, i, ArtText, RandText, NachbarText)).ToList();
        }
    }
}

/// <summary>Ein Bauteil im Raster des Assistenten — nur lesbar.</summary>
public sealed class ZonenBauteilKiZeile
{
    private readonly BauteilDaten _b;
    private readonly int _index;
    private readonly Func<string, string>? _artText;
    private readonly Func<BauteilDaten, string>? _randText;
    private readonly Func<BauteilDaten, string>? _nachbarText;

    internal ZonenBauteilKiZeile(BauteilDaten b, int index, Func<string, string>? artText,
                                 Func<BauteilDaten, string>? randText = null, Func<BauteilDaten, string>? nachbarText = null)
    {
        _b = b;
        _index = index;
        _artText = artText;
        _randText = randText;
        _nachbarText = nachbarText;
    }

    /// <summary>Die Randbedingung als Anzeigetext (Stufe G6b).</summary>
    public string Rand => _randText?.Invoke(_b) ?? _b.Randbedingung ?? "";

    /// <summary>Die Nachbarzone einer Trennfläche; „—" ohne (Stufe G6b).</summary>
    public string Nachbar => _nachbarText?.Invoke(_b) ?? "";

    /// <summary>Die Nummer der Zeile, ab 1 — das Kennzeichen.</summary>
    public string Nummer => (_index + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>Die Bauteilart als Anzeigetext.</summary>
    public string Art => _artText?.Invoke(_b.Bauteilart) ?? _b.Bauteilart;

    /// <summary>Der Name des Bauteils.</summary>
    public string Bezeichnung => _b.Bezeichner;

    /// <summary>Fläche [m²].</summary>
    public double? Flaeche => _b.Flaeche;

    /// <summary>Der wirksame U-Wert [W/(m²K)]: eingetragen, sonst der des Aufbaus.</summary>
    public double? UWert => _b.UWirksam;

    /// <summary>Azimut [°].</summary>
    public double? Azimut => _b.Azimut;

    /// <summary>Der Aufbau (Name); leer = keiner.</summary>
    public string Aufbau => _b.AufbauText;
}
