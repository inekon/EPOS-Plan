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

    private WaermepumpeAnlageDaten? D => Datenquelle?.Invoke();

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
