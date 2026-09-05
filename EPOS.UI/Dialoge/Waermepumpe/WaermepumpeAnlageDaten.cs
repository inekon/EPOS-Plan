namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Der Feldsatz der Wärmepumpen-ANLAGE — das plattformfreie Abbild von
/// <c>WErzeugerModel</c>, soweit die Detailansicht es zeigt (iU9-W7.4).
///
/// <para><b>Warum ein eigener Typ.</b> <c>WErzeugerModel</c> ist die Fachklasse des
/// Kerns für JEDE Anlagenart und trägt über dreißig Felder; die Maske fasst
/// vierzehn an. Eine Razor-Komponente kennt die Fachklassen des Kerns nicht.</para>
///
/// <para><b>Der Dialog bearbeitet eine KOPIE.</b> Der Vorläufer schrieb erst im
/// OK-Knopf in <c>item</c> — die Referenz in die Projektliste der Verwaltung.
/// Hier bekommt der Dialog eine Kopie, und die Hülle überträgt sie beim OK zurück;
/// Abbrechen verwirft sie. Ergebnisgleich, aber ohne die Möglichkeit, das
/// Listenobjekt auf halbem Weg zu verändern.</para>
///
/// <para><b>Vier Werte laufen VERBORGEN mit</b> — <see cref="Volumen"/>,
/// <see cref="Solaranteil"/>, <see cref="RendeMix"/> und <see cref="Modulkosten"/>.
/// Die Pufferspeichergruppe ist seit Ä19 nicht mehr gezeichnet (gepflegt wird sie in
/// der Simulation-Konfiguration), die Modulkosten laufen über die Kostenverwaltung.
/// Ihre Werte gehen dabei NICHT verloren: Sie kommen aus dem Datensatz herein und
/// werden unverändert zurückgeschrieben.</para>
/// </summary>
public sealed class WaermepumpeAnlageDaten
{
    // --- Die Wahl der Wärmepumpe -----------------------------------------------

    /// <summary>Bezeichner der gewählten Wärmepumpe (<c>listBox_WP.Text</c>).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>
    /// Die Geräte-Id. <b>Sie hat zwei Bedeutungen</b>, und das ist die Fachlage
    /// (Ä21): Bei einer Zeile aus der Projektliste ist es die PROJEKT-Geräte-Id
    /// (<c>Tab_WP.ID</c>), bei einer frisch gewählten Wärmepumpe die
    /// STAMMKATALOG-Id (<c>Tab_WP_STAMM.ID</c>) — der Speicherweg materialisiert
    /// die Stammwahl später. Nur eine ECHTE Nutzerwahl darf sie wechseln.
    /// </summary>
    public int IdWp { get; set; }

    // --- Auslegung für Verteilung ----------------------------------------------

    /// <summary>Vorlauftemperatur [°C] — die Stufen kommen aus den Kennlinien.</summary>
    public int? Vorlauf { get; set; }

    /// <summary>Rücklauftemperatur [°C] — frei eingebbar, die Liste ist ein Vorschlag.</summary>
    public int? Ruecklauf { get; set; }

    // --- Spitzenlast und Betrieb -----------------------------------------------

    /// <summary>Wärmeerzeuger Spitzenlast (Heizstab) vorhanden?</summary>
    public bool Heizstab { get; set; }

    /// <summary>Leistung des Heizstabs [kW] — Pflichtangabe.</summary>
    public int? HeizstabLeistung { get; set; }

    /// <summary>Wärmepumpenleistung / maximale Betriebszeit begrenzt?</summary>
    public bool Sperrung { get; set; }

    /// <summary>Sperrzeit von [h] — Pflichtangabe.</summary>
    public int? SperrzeitVon { get; set; }

    /// <summary>Sperrzeit bis [h] — Pflichtangabe.</summary>
    public int? SperrzeitBis { get; set; }

    /// <summary>Nutzungsdauer [h/Tag] — Pflichtangabe.</summary>
    public int? Nutzungszeit { get; set; }

    /// <summary>Bivalenter Betrieb.</summary>
    public bool BivalenterBetrieb { get; set; }

    /// <summary>
    /// Betriebsart — ein STEUERWERT aus <c>DbWerte.WP_BETRIEBSART_*</c>, kein
    /// Anzeigetext: Er steht so in <c>Tab_Energieanlagen.Betriebsart</c>.
    /// </summary>
    public string Betriebsart { get; set; } = "";

    /// <summary>
    /// Bivalenztemperatur [°C] (<c>Abschaltpunkt</c>). Sie ist nur bei
    /// Teilparallel- und Alternativbetrieb rechenwirksam und dann sichtbar; ein
    /// leeres Feld lässt den bisherigen Wert stehen.
    /// </summary>
    public double? Abschaltpunkt { get; set; }

    // --- Anzeigefelder aus dem Stammsatz (nur lesen) ---------------------------

    /// <summary>Beschreibung der gewählten Wärmepumpe.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Baujahr der gewählten Wärmepumpe.</summary>
    public int Baujahr { get; set; }

    /// <summary>Leistungsstufen der gewählten Wärmepumpe.</summary>
    public string Regelung { get; set; } = "";

    /// <summary>Typ der gewählten Wärmepumpe.</summary>
    public string Typ { get; set; } = "";

    /// <summary>Hersteller der gewählten Wärmepumpe.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Nennleistung [kW] der gewählten Wärmepumpe.</summary>
    public int Nennleistung { get; set; }

    // --- Verborgen mitlaufend --------------------------------------------------

    /// <summary>Modulkosten [€] — Ä19, nicht gezeichnet.</summary>
    public int Modulkosten { get; set; }

    /// <summary>Puffervolumen [m³] — Ä19, nicht gezeichnet.</summary>
    public double Volumen { get; set; }

    /// <summary>Anteil Speicher für Solaranlage [%] — Ä19, nicht gezeichnet.</summary>
    public int Solaranteil { get; set; }

    /// <summary>Pufferspeicher mit optimiertem Ladesystem — Ä19, nicht gezeichnet.</summary>
    public bool RendeMix { get; set; }

    /// <summary>Eine wortgleiche Kopie — der Dialog bearbeitet nie das Original der Hülle.</summary>
    public WaermepumpeAnlageDaten Kopie() => new()
    {
        Bezeichner = Bezeichner,
        IdWp = IdWp,
        Vorlauf = Vorlauf,
        Ruecklauf = Ruecklauf,
        Heizstab = Heizstab,
        HeizstabLeistung = HeizstabLeistung,
        Sperrung = Sperrung,
        SperrzeitVon = SperrzeitVon,
        SperrzeitBis = SperrzeitBis,
        Nutzungszeit = Nutzungszeit,
        BivalenterBetrieb = BivalenterBetrieb,
        Betriebsart = Betriebsart,
        Abschaltpunkt = Abschaltpunkt,
        Beschreibung = Beschreibung,
        Baujahr = Baujahr,
        Regelung = Regelung,
        Typ = Typ,
        Firma = Firma,
        Nennleistung = Nennleistung,
        Modulkosten = Modulkosten,
        Volumen = Volumen,
        Solaranteil = Solaranteil,
        RendeMix = RendeMix
    };
}
