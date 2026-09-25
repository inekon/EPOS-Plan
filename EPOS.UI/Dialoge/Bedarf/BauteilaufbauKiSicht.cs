using System.Globalization;
using EPOS.UI.Dienste;
using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Verwaltung „Bauteilaufbauten" für den Hilfe-Assistenten
/// (Gebäudesimulation G3, Welle C).
///
/// <para><b>Der Aufbau ist eine WAHL</b> — die Satzwahl der Verwaltung (Schlüssel = Id); ein
/// Setzen wählt die Zeile wie ein Klick. <b>Kopf und Schichten</b> schreiben in den
/// ARBEITSSTAND (<see cref="BauteilaufbauArbeitsstand"/>), denselben, den Stammblatt und Raster
/// zeigen; geschrieben wird mit „Speichern".</para>
///
/// <para><b>Die Schichten sind ein RASTER</b> (Sammlungsform <c>Schichten[]</c>): je Zeile
/// Baustoff, Dicke in mm, λ, ρ, c_p und der Schalter Luftschicht. Die Wahl des Baustoffs nimmt
/// den Weg der Suchauswahl — die Schicht übernimmt die Stoffwerte als Kopie.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten; die Zeilen entstehen je Zugriff neu über dem Arbeitsstand.</para>
/// </summary>
public sealed class BauteilaufbauKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? AufbauLesen { get; init; }
    public Action<string>? AufbauSetzen { get; init; }

    /// <summary>Liefert die Zeilen der Liste (Schlüssel = Id, Text = Name).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? AufbauEintraege { get; init; }

    /// <summary>Der Arbeitsstand des gewählten Aufbaus; <c>null</c> = keiner gewählt.</summary>
    public Func<BauteilaufbauArbeitsstand?>? StandLesen { get; init; }

    /// <summary>Die Bauteilarten zur Wahl (Persistenzwert, Anzeigetext) — leer = für jede.</summary>
    public Func<IReadOnlyList<(string Wert, string Text)>>? Bauteilarten { get; init; }

    /// <summary>Die Baustoffe zur Wahl einer Schicht.</summary>
    public Func<IReadOnlyList<BaustoffWahl>>? Baustoffe { get; init; }

    /// <summary>Meldet eine Änderung am Arbeitsstand — derselbe Nachzug wie nach einer Eingabe von Hand.</summary>
    public Action? Geaendert { get; init; }

    // =====================================================================
    //  Die Wahllisten (Begleiteigenschaften <Feld>Wahl, KI‑D‑Q6)
    // =====================================================================

    /// <summary>Die Aufbauten der Liste.</summary>
    public IReadOnlyList<KiWahleintrag> AufbauWahl
        => AufbauEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bauteilarten: Schlüssel ist der Persistenzwert, leer = für jede.</summary>
    public IReadOnlyList<KiWahleintrag> BauteilartWahl
        => KiMaskenanmeldung.Eintraege(Bauteilarten?.Invoke(), a => a.Wert, a => a.Text);

    /// <summary>Die Baustoffe einer Schicht: Schlüssel ist die Id, leer = freie Eingabe.</summary>
    public IReadOnlyList<KiWahleintrag> BaustoffWahl
        => KiMaskenanmeldung.Eintraege(Baustoffe?.Invoke(), b => b.Id, b => b.Text);

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der gewählte Aufbau (Schlüssel = Id). Ein Setzen WÄHLT die Zeile.</summary>
    public string Aufbau
    {
        get => AufbauLesen?.Invoke() ?? "";
        set => AufbauSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Name im Arbeitsstand.</summary>
    public string Bezeichner
    {
        get => StandLesen?.Invoke()?.Arbeit.Bezeichner ?? "";
        set => Kopf(a => a.Bezeichner = value ?? "");
    }

    /// <summary>Die Bauteilart als Persistenzwert; leer = für jede Bauteilart.</summary>
    public string Bauteilart
    {
        get => StandLesen?.Invoke()?.Arbeit.Bauteilart ?? "";
        set => Kopf(a => a.Bauteilart = value ?? "");
    }

    /// <summary>Die Beschreibung im Arbeitsstand.</summary>
    public string Beschreibung
    {
        get => StandLesen?.Invoke()?.Arbeit.Beschreibung ?? "";
        set => Kopf(a => a.Beschreibung = value ?? "");
    }

    /// <summary>Die Quelle im Arbeitsstand.</summary>
    public string Quelle
    {
        get => StandLesen?.Invoke()?.Arbeit.Quelle ?? "";
        set => Kopf(a => a.Quelle = value ?? "");
    }

    /// <summary>Die Schichten, innen → außen — je Zugriff neu über dem Arbeitsstand.</summary>
    public IReadOnlyList<BauteilschichtKiZeile> Schichten
    {
        get
        {
            BauteilaufbauArbeitsstand? stand = StandLesen?.Invoke();
            if (stand is null) return Array.Empty<BauteilschichtKiZeile>();
            return Enumerable.Range(0, stand.Schichten.Count)
                             .Select(i => new BauteilschichtKiZeile(stand, i, Baustoffe, Geaendert))
                             .ToList();
        }
    }

    private void Kopf(Action<BauteilaufbauDaten> schritt)
    {
        BauteilaufbauArbeitsstand? stand = StandLesen?.Invoke();
        if (stand is null) return;
        schritt(stand.Arbeit);
        stand.Eingabe();
        Geaendert?.Invoke();
    }
}

/// <summary>
/// Eine Schicht im Raster des Assistenten — eine Sicht auf die Zeile des Arbeitsstands. Die
/// Wahl des Baustoffs nimmt den Weg der Suchauswahl (<see cref="BauteilaufbauArbeitsstand.StoffWaehlen"/>).
/// </summary>
public sealed class BauteilschichtKiZeile
{
    private readonly BauteilaufbauArbeitsstand _stand;
    private readonly int _index;
    private readonly Func<IReadOnlyList<BaustoffWahl>>? _baustoffe;
    private readonly Action? _geaendert;

    internal BauteilschichtKiZeile(BauteilaufbauArbeitsstand stand, int index,
                                   Func<IReadOnlyList<BaustoffWahl>>? baustoffe, Action? geaendert)
    {
        _stand = stand;
        _index = index;
        _baustoffe = baustoffe;
        _geaendert = geaendert;
    }

    private BauteilschichtDaten Zeile => _stand.Schichten[_index];

    /// <summary>Die Nummer der Schicht, innen = 1 — das Kennzeichen der Zeile.</summary>
    public string Nummer => (_index + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>Der Baustoff (Schlüssel = Id; leer = freie Eingabe). Ein Setzen übernimmt die Stoffwerte.</summary>
    public string Baustoff
    {
        get => Zeile.IdBaustoff?.ToString(CultureInfo.InvariantCulture) ?? "";
        set
        {
            int? id = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : null;
            _stand.StoffWaehlen(_index, id, _baustoffe?.Invoke());
            _geaendert?.Invoke();
        }
    }

    /// <summary>Schichtdicke [mm].</summary>
    public double? DickeMm { get => Zeile.DickeMm; set => Setzen(() => Zeile.DickeMm = value); }

    /// <summary>Wärmeleitfähigkeit λ [W/(mK)].</summary>
    public double? Lambda { get => Zeile.Lambda; set => Setzen(() => Zeile.Lambda = value); }

    /// <summary>Rohdichte ρ [kg/m³].</summary>
    public double? Rho { get => Zeile.Rho; set => Setzen(() => Zeile.Rho = value); }

    /// <summary>Spezifische Wärmekapazität c_p [J/(kgK)].</summary>
    public double? Cp { get => Zeile.Cp; set => Setzen(() => Zeile.Cp = value); }

    /// <summary>Luftschicht (ohne λ: ruhende Luftschicht nach DIN EN ISO 6946).</summary>
    public bool IstLuftschicht { get => Zeile.IstLuftschicht; set => Setzen(() => Zeile.IstLuftschicht = value); }

    private void Setzen(Action schritt)
    {
        schritt();
        _stand.Eingabe();
        _geaendert?.Invoke();
    }
}
