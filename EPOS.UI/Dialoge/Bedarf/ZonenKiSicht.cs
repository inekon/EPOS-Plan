using System.Globalization;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild des <see cref="ZonenDialog"/> für den Hilfe-Assistenten (Gebäudesimulation G3,
/// Welle D2; Stufe G6b W2): Bezeichnung, Nutzfläche und die Werte der Zone (Raumhöhe, Volumen,
/// beheizt, Sollwerte, Lüftung, Gewinne, Bewohner, Heizung) sind setzbar, die Bauteile ein RASTER zum Lesen
/// (Sammlungsform <c>Bauteile[]</c>, Kennzeichen die Nummer) — anlegen, öffnen und entfernen bleiben
/// Klicks des Anwenders, die Werte eines Bauteils setzt der Assistent im Bauteildialog. Sie hält
/// keinen Zustand: Jede Eigenschaft ruft bei jedem Zugriff ihren Delegaten.
/// <para><b>Die Zonenmatrix ist eine FELDTAFEL</b> (Stufe KP2, Welle U4; Teilkonzept 3.4, 7.3): Ihre
/// Felder erzeugt der Kern aus dem Profil <c>KiKonditionierungsfelder</c> (Zonenkarte); diese Klasse
/// beantwortet sie über den Schlüssel und die <see cref="Konditionierung"/> des Dialogs — derselbe Weg
/// wie die Zellen der Matrix. Leer heißt „wie Gebäude". Die Bestandszellen (Sollwerte, Lüftung,
/// Gewinne) stehen unter den Feldern der Zone und gehen im Dialog ebenfalls über die Bearbeitung.</para>
/// </summary>
public sealed class ZonenKiSicht : IKiFeldtafel
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

    // KU3-3 (E67/E68): die Kuehlung je Zone; leer = wie Gebaeude.
    /// <summary>Liest den Kühlschalter der Zone (<c>null</c> = wie Gebäude).</summary>
    public Func<bool?>? KuehlungAktivLesen { get; init; }
    /// <summary>Setzt den Kühlschalter der Zone.</summary>
    public Action<bool?>? KuehlungAktivSetzen { get; init; }
    /// <summary>Liest die Kühlleistungsgrenze der Zone.</summary>
    public Func<double?>? KuehlleistungMaxLesen { get; init; }
    /// <summary>Setzt die Kühlleistungsgrenze der Zone.</summary>
    public Action<double?>? KuehlleistungMaxSetzen { get; init; }

    // E63 (AK1z): die Uebergabe je Zone; leer = wie Gebaeude.
    /// <summary>Liest die Übergabeart der Zone (<c>null</c> = wie Gebäude).</summary>
    public Func<string?>? UebergabeArtLesen { get; init; }
    /// <summary>Setzt die Übergabeart; ein unbekannter Wert wirft mit Grund.</summary>
    public Action<string?>? UebergabeArtSetzen { get; init; }
    /// <summary>Liest den Exponenten.</summary>
    public Func<double?>? UebergabeExponentLesen { get; init; }
    /// <summary>Setzt den Exponenten.</summary>
    public Action<double?>? UebergabeExponentSetzen { get; init; }
    /// <summary>Liest die Nennleistung [kW].</summary>
    public Func<double?>? UebergabeNennleistungLesen { get; init; }
    /// <summary>Setzt die Nennleistung [kW].</summary>
    public Action<double?>? UebergabeNennleistungSetzen { get; init; }
    /// <summary>Liest den Auslegungsvorlauf [°C].</summary>
    public Func<double?>? AuslegungVorlaufLesen { get; init; }
    /// <summary>Setzt den Auslegungsvorlauf [°C].</summary>
    public Action<double?>? AuslegungVorlaufSetzen { get; init; }
    /// <summary>Liest den Auslegungsrücklauf [°C].</summary>
    public Func<double?>? AuslegungRuecklaufLesen { get; init; }
    /// <summary>Setzt den Auslegungsrücklauf [°C].</summary>
    public Action<double?>? AuslegungRuecklaufSetzen { get; init; }
    /// <summary>Liest die Auslegungsraumtemperatur [°C].</summary>
    public Func<double?>? AuslegungRaumLesen { get; init; }
    /// <summary>Setzt die Auslegungsraumtemperatur [°C].</summary>
    public Action<double?>? AuslegungRaumSetzen { get; init; }
    /// <summary>Liest das Proportionalband [K].</summary>
    public Func<double?>? ProportionalbandLesen { get; init; }
    /// <summary>Setzt das Proportionalband [K].</summary>
    public Action<double?>? ProportionalbandSetzen { get; init; }
    /// <summary>Liest die wirksame Übergabe mit Herkunft je Wert.</summary>
    public Func<string>? UebergabeWirksamLesen { get; init; }

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

    /// <summary>
    /// Wird die Zone gekühlt — „ja", „nein" oder leer = wie das Gebäude (KU3-3). Ein anderer Text wirft mit Grund.
    /// </summary>
    public string KuehlungAktiv
    {
        get => KuehlungAktivLesen?.Invoke() switch { true => "ja", false => "nein", _ => "" };
        set
        {
            string w = (value ?? "").Trim().ToLowerInvariant();
            bool? schalter = w switch
            {
                "" => null,
                "ja" or "yes" or "1" or "true" => true,
                "nein" or "no" or "0" or "false" => false,
                _ => throw new ArgumentException("Erlaubt sind „ja\", „nein\" oder leer (wie Gebäude).", nameof(value))
            };
            KuehlungAktivSetzen?.Invoke(schalter);
        }
    }

    /// <summary>Leistungsgrenze der Kühlung [kW]; leer = die des Gebäudes (ab zwei Zonen anteilig).</summary>
    public double? KuehlleistungMax { get => KuehlleistungMaxLesen?.Invoke(); set => KuehlleistungMaxSetzen?.Invoke(value); }

    /// <summary>Übergabeart der Zone (IDEAL, RADIATOR, FLAECHE, KONVEKTOR); leer = wie Gebäude.</summary>
    public string UebergabeArt { get => UebergabeArtLesen?.Invoke() ?? ""; set => UebergabeArtSetzen?.Invoke(value); }

    /// <summary>Exponent der Übergabe; leer = wie Gebäude.</summary>
    public double? UebergabeExponent { get => UebergabeExponentLesen?.Invoke(); set => UebergabeExponentSetzen?.Invoke(value); }

    /// <summary>Nennleistung der Übergabe [kW]; leer = Anteil des Gebäudes nach Nutzfläche.</summary>
    public double? UebergabeNennleistung { get => UebergabeNennleistungLesen?.Invoke(); set => UebergabeNennleistungSetzen?.Invoke(value); }

    /// <summary>Auslegungsvorlauf [°C]; leer = wie Gebäude.</summary>
    public double? AuslegungVorlauf { get => AuslegungVorlaufLesen?.Invoke(); set => AuslegungVorlaufSetzen?.Invoke(value); }

    /// <summary>Auslegungsrücklauf [°C]; leer = wie Gebäude.</summary>
    public double? AuslegungRuecklauf { get => AuslegungRuecklaufLesen?.Invoke(); set => AuslegungRuecklaufSetzen?.Invoke(value); }

    /// <summary>Auslegungsraumtemperatur [°C]; leer = wie Gebäude.</summary>
    public double? AuslegungRaumtemperatur { get => AuslegungRaumLesen?.Invoke(); set => AuslegungRaumSetzen?.Invoke(value); }

    /// <summary>Proportionalband des Raumreglers [K]; leer = wie Gebäude.</summary>
    public double? Proportionalband { get => ProportionalbandLesen?.Invoke(); set => ProportionalbandSetzen?.Invoke(value); }

    /// <summary>Die wirksame Übergabe mit Herkunft je Wert (nur lesen).</summary>
    public string UebergabeWirksam => UebergabeWirksamLesen?.Invoke() ?? "";

    /// <summary>Liest das zuletzt übernommene Nutzungsprofil der Zone (Stufe NP3b).</summary>
    public Func<string>? NutzungsprofilLesen { get; init; }

    /// <summary>
    /// Das zuletzt übernommene Nutzungsprofil der Zone (nur lesen; Stufe NP3b, Konzept Nutzungsprofile 6.3) —
    /// <c>Tab_Zone.Nutzungsprofil</c> bzw. die Nutzung ihrer Kalender, leer = keines.
    /// </summary>
    public string Nutzungsprofil => NutzungsprofilLesen?.Invoke() ?? "";

    /// <summary>
    /// Die Bearbeitung der Zonenmatrix (<see cref="KonditionierungBearbeitung"/> im Zonenmodus); ohne sie
    /// stehen die Felder der Matrix nicht zur Verfügung.
    /// </summary>
    public KonditionierungBearbeitung? Konditionierung { get; init; }

    /// <summary>Die Feldtafel der Zonenkarte — derselbe Weg wie am Gebäude (<see cref="KonditionierungKiTafel"/>).</summary>
    private readonly KonditionierungKiTafel _tafel = new(KiKonditionierungsfelder.FindeZone);

    /// <inheritdoc />
    public object? Lesen(string schluessel) => _tafel.Lesen(Konditionierung, schluessel);

    /// <inheritdoc />
    public void Setzen(string schluessel, object? wert) => _tafel.Setzen(Konditionierung, schluessel, wert);

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
