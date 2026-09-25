using System.Globalization;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild des <see cref="ZonenDialog"/> für den Hilfe-Assistenten (Gebäudesimulation G3,
/// Welle D2): Bezeichnung und Nutzfläche sind setzbar, die Bauteile ein RASTER zum Lesen
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

    public Func<IReadOnlyList<BauteilDaten>>? BauteileLesen { get; init; }

    /// <summary>Der Anzeigetext einer Bauteilart.</summary>
    public Func<string, string>? ArtText { get; init; }

    /// <summary>Der Name der Zone.</summary>
    public string Bezeichnung { get => BezeichnungLesen?.Invoke() ?? ""; set => BezeichnungSetzen?.Invoke(value); }

    /// <summary>Nutzfläche der Zone [m²]; leer = die des Gebäudes.</summary>
    public double? Nutzflaeche { get => NutzflaecheLesen?.Invoke(); set => NutzflaecheSetzen?.Invoke(value); }

    /// <summary>Die Bauteile — je Zugriff neu über dem Arbeitsstand, nur lesbar.</summary>
    public IReadOnlyList<ZonenBauteilKiZeile> Bauteile
    {
        get
        {
            IReadOnlyList<BauteilDaten> liste = BauteileLesen?.Invoke() ?? Array.Empty<BauteilDaten>();
            return liste.Select((b, i) => new ZonenBauteilKiZeile(b, i, ArtText)).ToList();
        }
    }
}

/// <summary>Ein Bauteil im Raster des Assistenten — nur lesbar.</summary>
public sealed class ZonenBauteilKiZeile
{
    private readonly BauteilDaten _b;
    private readonly int _index;
    private readonly Func<string, string>? _artText;

    internal ZonenBauteilKiZeile(BauteilDaten b, int index, Func<string, string>? artText)
    {
        _b = b;
        _index = index;
        _artText = artText;
    }

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
