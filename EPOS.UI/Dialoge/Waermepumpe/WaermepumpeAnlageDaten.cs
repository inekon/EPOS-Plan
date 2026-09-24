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
    /// ET‑5 (08.09.2026): der Energieträger der Anlage (<c>Tab_Energieanlagen.ID_Carrier</c>),
    /// in der Gliederung des Katalogs Gruppe › Art gewählt; 0 = noch keiner (die Hülle setzt den
    /// Stromträger des Projekts als Vorgabe).
    /// </summary>
    public int CarrierId { get; set; }

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

    /// <summary>
    /// Aufstellungsart (<c>Tab_WP.Aufstellung</c>) — seit dem Anwenderentscheid vom
    /// 16.09.2026 im Feldsatz: Die Stammfelder des Anlagendialogs schreiben in die
    /// PROJEKTKOPIE, und Aufstellung gehört dazu. Vorher lief sie am Dialog vorbei.
    /// </summary>
    public string Aufstellung { get; set; } = "";

    /// <summary>Hersteller der gewählten Wärmepumpe.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Nennleistung [kW] der gewählten Wärmepumpe.</summary>
    public int Nennleistung { get; set; }

    /// <summary>
    /// Kühlleistung [kW] der PROJEKTKOPIE (<c>Tab_WP.Kuehlleistung</c>) — seit dem
    /// Anwenderentscheid vom 16.09.2026 im Feldsatz und im Stammfeldblock BEARBEITBAR,
    /// wie die Nennleistung daneben.
    ///
    /// <para><b>Eine Kommazahl, keine ganze:</b> Die Spalte ist <c>REAL</c>; 5,5 kW sind
    /// ein gültiger Wert. <c>null</c> = die Hülle führt sie nicht (Prüfstand, ältere
    /// Aufrufer) und der Schreibweg lässt die Spalte dann stehen.</para>
    /// </summary>
    public double? Kuehlleistung { get; set; }

    // --- Kühlbetrieb (Stufe KU2 Welle 3; Kühlkonzept 8.2; E15, E33, E34) -------
    //
    // Drei Felder der PROJEKTKOPIE (Tab_WP) und zwei der ANLAGENZEILE
    // (Tab_Energieanlagen). Die Gruppe „Kühlbetrieb" des Bausteins
    // WaermepumpeKonfiguration zeichnet sie nur, wenn der Wirt die Kühlgaben reicht
    // (WaermepumpeKuehlGaben); sonst laufen sie unverändert mit.

    /// <summary>„Maschine auch zum Kühlen benutzen" (<c>Tab_WP.Kuehlbetrieb</c>).</summary>
    public bool Kuehlbetrieb { get; set; }

    /// <summary>Kühl-Vorlauf [°C] aus den Stützstellen der Kühlkennlinie (<c>Tab_WP.Kuehl_Vorlauf</c>, K21); <c>null</c> = kleinster Stützwert.</summary>
    public int? KuehlVorlauf { get; set; }

    /// <summary>Hilfsstromanteil [—], 0 ≤ x &lt; 1 (<c>Tab_WP.Kuehl_Hilfsstromanteil</c>, K23); <c>null</c> = kein Zuschlag. Der Baustein zeigt ihn in Prozent.</summary>
    public double? KuehlHilfsstromanteil { get; set; }

    /// <summary>Stromträger des Kältestroms (<c>Tab_Energieanlagen.Kuehl_ID_Carrier</c>, K9); <c>null</c> = wie Heizbetrieb.</summary>
    public int? KuehlCarrierId { get; set; }

    /// <summary>Abrechnung des Kältestroms bei abweichendem Träger (<c>Kuehl_EigenerZaehler</c>, E34): <c>true</c> = eigener Zähler, sonst anteilig am Netzbezug.</summary>
    public bool? KuehlEigenerZaehler { get; set; }

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
        CarrierId = CarrierId,
        Betriebsart = Betriebsart,
        Abschaltpunkt = Abschaltpunkt,
        Beschreibung = Beschreibung,
        Baujahr = Baujahr,
        Regelung = Regelung,
        Typ = Typ,
        Aufstellung = Aufstellung,
        Firma = Firma,
        Nennleistung = Nennleistung,
        Kuehlleistung = Kuehlleistung,
        Kuehlbetrieb = Kuehlbetrieb,
        KuehlVorlauf = KuehlVorlauf,
        KuehlHilfsstromanteil = KuehlHilfsstromanteil,
        KuehlCarrierId = KuehlCarrierId,
        KuehlEigenerZaehler = KuehlEigenerZaehler,
        Modulkosten = Modulkosten,
        Volumen = Volumen,
        Solaranteil = Solaranteil,
        RendeMix = RendeMix
    };

    /// <summary>Schreibt die fünf Kühlfelder eines anderen Satzes zurück — der Abbrechen-Weg der Konfiguration.</summary>
    public void KuehlfelderAus(WaermepumpeAnlageDaten quelle)
    {
        if (quelle is null) return;
        Kuehlbetrieb = quelle.Kuehlbetrieb;
        KuehlVorlauf = quelle.KuehlVorlauf;
        KuehlHilfsstromanteil = quelle.KuehlHilfsstromanteil;
        KuehlCarrierId = quelle.KuehlCarrierId;
        KuehlEigenerZaehler = quelle.KuehlEigenerZaehler;
    }
}

/// <summary>
/// Eine Vorlauf-Stützstelle der Kühlkennlinie für die Auswahl „Kühl-Vorlauf" (K21) —
/// mit dem Grund, aus dem der Lauf sie ablehnt (Heizlage, vertauschte Achsen, K22); leer =
/// wählbar.
/// </summary>
public sealed record KuehlVorlaufEintrag(int Vorlauf, string Sperrgrund = "");

/// <summary>
/// <b>Die Kühlgaben der Wärmepumpen-Konfiguration</b> (Stufe KU2 Welle 3; Kühlkonzept 8.2) — was
/// der Baustein <c>WaermepumpeKonfiguration</c> für die Gruppe „Kühlbetrieb" aus der Datenbank
/// braucht, gebaut in der Hülle aus Kern-Controllern. <c>null</c> am Baustein = keine Gruppe.
/// </summary>
public sealed class WaermepumpeKuehlGaben
{
    /// <summary>Die Vorlauf-Stützstellen der Kühlkennlinie eines Geräts (Projektkopie vor Katalog); leer = keine Kennlinie.</summary>
    public Func<int, IReadOnlyList<KuehlVorlaufEintrag>>? Vorlaeufe { get; init; }

    /// <summary>Der Sperrgrund des Kühlbetriebs eines Geräts (keine Kennlinie, Quellspeicher); <c>null</c> = frei.</summary>
    public Func<int, string?>? Sperrgrund { get; init; }

    /// <summary>Die Stromträger des Projekts für die Wahl des Kühlträgers — Id und Name.</summary>
    public IReadOnlyList<(int Id, string Text)> Stromtraeger { get; init; } = Array.Empty<(int, string)>();

    /// <summary>
    /// Der Stromträger, der den Netzbezug des Projekts bepreist (<c>Kaeltestromabrechnung.Projekttraeger</c>);
    /// weicht der Kühlträger davon ab, bietet der Baustein die Abrechnungsart an (E34).
    /// </summary>
    public int ProjektStromtraeger { get; init; }
}
