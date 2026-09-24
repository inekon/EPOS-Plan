using KiKern;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Das FLACHE Abbild des Wärmepumpen-Anlagendialogs für den Hilfe-Assistenten
/// (Welle #458, Stufe 1 — dasselbe Muster wie <c>PhotovoltaikKiSicht</c>, KI‑F7).
///
/// <para><b>Warum ein Sichtmodell, wo die Maske ein Daten-Objekt HAT.</b> Diese Klasse
/// reicht jede Eigenschaft von <see cref="WaermepumpeAnlageDaten"/> unverändert durch —
/// Anlage, Konfiguration und Stammfelder stehen weiter dort. Eine Größe der Maske steht
/// dort aber NICHT: der Schalter „Extrapolation der WP-Kennlinie erlauben". Er gehört
/// dem PROJEKT und schreibt sofort (<c>ExtrapolationSchreiben</c>); die Brücke kennt je
/// Maske EIN Daten-Objekt, also braucht <c>Form_WP_Anlage</c> eines, das beide Herkünfte
/// trägt.</para>
///
/// <para><b>Sie hält keinen Zustand.</b> Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten; die Setzer schreiben in dasselbe Objekt wie die Eingabefelder der Maske.</para>
/// </summary>
public sealed class WaermepumpeAnlageKiSicht
{
    /// <summary>Der Feldsatz der Anlage — derselbe, an den die Maske bindet.</summary>
    public Func<WaermepumpeAnlageDaten?>? Datenquelle { get; init; }

    /// <summary>Der gezeigte Stand des Extrapolationsschalters.</summary>
    public Func<bool>? ExtrapolationLesen { get; init; }

    /// <summary>
    /// Der Schreibweg des Schalters — DERSELBE wie am Bildschirm
    /// (<c>ExtrapolationGesetzt</c>). Rückgabe: der Grund, warum nichts geschrieben wurde,
    /// sonst <c>null</c>.
    /// </summary>
    public Func<bool, string?>? ExtrapolationSetzen { get; init; }

    /// <summary>
    /// Die Kühlgaben des Dialogs — Stützstellen, Sperrgrund und Stromträger des Projekts
    /// (Stufe KU2 Welle 3). <c>null</c> = der Wirt bietet keinen Kühlbetrieb an; dann lehnt
    /// jede Setzung eines Kühlfeldes benannt ab, statt Werte zu schreiben, die niemand sieht.
    /// </summary>
    public Func<WaermepumpeKuehlGaben?>? Kuehlgaben { get; init; }

    private WaermepumpeAnlageDaten? D => Datenquelle?.Invoke();

    private WaermepumpeKuehlGaben? G => Kuehlgaben?.Invoke();

    // =====================================================================
    //  Die Projekteinstellung
    // =====================================================================

    /// <summary>„Extrapolation der WP-Kennlinie erlauben" — gilt allen Wärmepumpen des Projekts.</summary>
    /// <remarks>Ein abgelehnter oder gescheiterter Schreibweg kommt als benannte Ausnahme zurück.</remarks>
    public bool Extrapolation
    {
        get => ExtrapolationLesen?.Invoke() ?? false;
        set
        {
            string? grund = ExtrapolationSetzen is null
                ? WindowsFormsApplication1.MyResource.Resource.KI_SIM_KEIN_SCHREIBWEG
                : ExtrapolationSetzen(value);
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    // =====================================================================
    //  Die Anlage — durchgereicht
    // =====================================================================

    /// <summary>Bezeichner der gewählten Wärmepumpe (nur lesbar im Katalog).</summary>
    public string Bezeichner => D?.Bezeichner ?? "";

    public int? Vorlauf { get => D?.Vorlauf; set { if (D is { } d) d.Vorlauf = value; } }
    public int? Ruecklauf { get => D?.Ruecklauf; set { if (D is { } d) d.Ruecklauf = value; } }
    public int? Nutzungszeit { get => D?.Nutzungszeit; set { if (D is { } d) d.Nutzungszeit = value; } }

    // ---- Die Konfiguration ------------------------------------------------------

    public bool Heizstab { get => D?.Heizstab ?? false; set { if (D is { } d) d.Heizstab = value; } }
    public bool Sperrung { get => D?.Sperrung ?? false; set { if (D is { } d) d.Sperrung = value; } }
    public int? SperrzeitVon { get => D?.SperrzeitVon; set { if (D is { } d) d.SperrzeitVon = value; } }
    public int? SperrzeitBis { get => D?.SperrzeitBis; set { if (D is { } d) d.SperrzeitBis = value; } }
    public bool BivalenterBetrieb { get => D?.BivalenterBetrieb ?? false; set { if (D is { } d) d.BivalenterBetrieb = value; } }
    public int CarrierId { get => D?.CarrierId ?? 0; set { if (D is { } d) d.CarrierId = value; } }
    public string Betriebsart { get => D?.Betriebsart ?? ""; set { if (D is { } d) d.Betriebsart = value ?? ""; } }
    public double? Abschaltpunkt { get => D?.Abschaltpunkt; set { if (D is { } d) d.Abschaltpunkt = value; } }

    // ---- Der Kühlbetrieb (Stufe KU2 Welle 3; Kühlkonzept 8.2, E15, E33, E34) -----
    //
    // Die Wege stehen EINMAL, in WaermepumpeKuehlKiWege (und deren Regeln in den statischen
    // Wegen der Konfiguration) - dieselben für die Konfiguration der Simulation
    // (KomponentenKonfigurationKiSicht). Was die Maske weich sperrt, lehnt die Setzung benannt ab.

    /// <summary>„Maschine auch zum Kühlen benutzen" — gesperrt ohne Kühlkennlinie oder mit Quellspeicher.</summary>
    public bool Kuehlbetrieb
    {
        get => D?.Kuehlbetrieb ?? false;
        set => WaermepumpeKuehlKiWege.KuehlbetriebSetzen(D, G, value);
    }

    /// <summary>Der Kühl-Vorlauf [°C] aus den Stützstellen; leer = kleinster Stützwert.</summary>
    public int? KuehlVorlauf
    {
        get => D?.KuehlVorlauf;
        set => WaermepumpeKuehlKiWege.VorlaufSetzen(D, G, value);
    }

    /// <summary>Die Stützstellen der Kühlkennlinie, die die Maske zur Wahl stellt — ohne die gesperrten.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlVorlaufWahl => WaermepumpeKuehlKiWege.VorlaufWahl(D, G);

    /// <summary>Der Hilfsstromanteil in PROZENT, wie die Maske ihn zeigt; gespeichert wird der Anteil.</summary>
    public double? KuehlHilfsstromanteil
    {
        get => WaermepumpeKuehlKiWege.HilfsstromProzent(D);
        set => WaermepumpeKuehlKiWege.HilfsstromSetzen(D, G, value);
    }

    /// <summary>Der Stromträger des Kältestroms; leer = wie Heizbetrieb.</summary>
    public int? KuehlCarrierId
    {
        get => D?.KuehlCarrierId;
        set => WaermepumpeKuehlKiWege.KuehltraegerSetzen(D, G, value);
    }

    /// <summary>Die Stromträger des Projekts, die die Maske für den Kältestrom zur Wahl stellt.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlCarrierIdWahl => WaermepumpeKuehlKiWege.TraegerWahl(G);

    /// <summary>
    /// Die Abrechnungsart des Kältestroms (E34): 0 = anteilig am Netzbezug, 1 = eigener Zähler —
    /// wählbar nur, wenn der Kühlträger vom Stromträger des Projekts abweicht.
    /// </summary>
    public int KuehlAbrechnung
    {
        get => WaermepumpeKuehlKiWege.Abrechnung(D);
        set => WaermepumpeKuehlKiWege.AbrechnungSetzen(D, G, value);
    }

    /// <summary>Die zwei Abrechnungsarten der Maske — dieselben Texte wie ihre Optionsgruppe.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlAbrechnungWahl => WaermepumpeKuehlKiWege.AbrechnungWahl();

    // ---- Die Stammfelder des Geräts ---------------------------------------------

    public string Firma { get => D?.Firma ?? ""; set { if (D is { } d) d.Firma = value ?? ""; } }
    public string Beschreibung { get => D?.Beschreibung ?? ""; set { if (D is { } d) d.Beschreibung = value ?? ""; } }
    public string Typ { get => D?.Typ ?? ""; set { if (D is { } d) d.Typ = value ?? ""; } }
    public string Regelung { get => D?.Regelung ?? ""; set { if (D is { } d) d.Regelung = value ?? ""; } }
    public string Aufstellung { get => D?.Aufstellung ?? ""; set { if (D is { } d) d.Aufstellung = value ?? ""; } }
    public int Baujahr { get => D?.Baujahr ?? 0; set { if (D is { } d) d.Baujahr = value; } }
    public int Nennleistung { get => D?.Nennleistung ?? 0; set { if (D is { } d) d.Nennleistung = value; } }
    public int? HeizstabLeistung { get => D?.HeizstabLeistung; set { if (D is { } d) d.HeizstabLeistung = value; } }
    public double? Kuehlleistung { get => D?.Kuehlleistung; set { if (D is { } d) d.Kuehlleistung = value; } }

    /// <summary>Die Modulkosten — eine Anzeige; gepflegt werden sie in der Kostenverwaltung.</summary>
    public int Modulkosten => D?.Modulkosten ?? 0;
}
