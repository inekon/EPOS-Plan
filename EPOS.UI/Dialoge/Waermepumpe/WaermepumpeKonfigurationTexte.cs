using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Die Beschriftungen des Bausteins <see cref="WaermepumpeKonfiguration"/> — des Blocks,
/// der bis zum 16.09.2026 im Anlagendialog unter der Überschrift „Wärmeerzeuger
/// Spitzenlast:" stand.
///
/// <para><b>Warum der Block einen eigenen Namen bekam.</b> Die alte Überschrift benannte
/// nur den ERSTEN Schalter (die elektrische Nachheizung); darunter standen aber Sperrzeit,
/// bivalenter Betrieb, Bivalenztemperatur und der Energieträger — alles, was den Rechenweg
/// der Wärmepumpe parametriert. Seit dem Anwenderentscheid vom 16.09.2026 heißt der Block
/// deshalb „Konfiguration" und ist von zwei Stellen aus erreichbar: aus der Detailansicht
/// über den Knopf „Konfiguration…" und aus Simulation › Konfiguration.</para>
///
/// <para>Ein BÜNDEL nach der Bauart <c>PvModellTexte</c> (Hausregel EPOS.UI, „ab etwa zehn
/// Anzeigetexten ein Bündel"): Es füllt sich SELBST aus <c>MyResource</c> in der
/// Oberflächensprache; ein fehlender Schlüssel fällt auf den deutschen Wortlaut zurück. Die
/// Hülle muss nichts beisteuern — der Baustein zeichnet auch ohne jede Gabe. Jede
/// Eigenschaft nennt ihren Ressourcenschlüssel.</para>
///
/// <para>Die Eigenschaften sind SETZBAR: Ein Wirt, der seine Texte aus einer Hülle bezieht
/// (Prüfstand, Fremdsprache aus einem anderen Katalog), überschreibt einzelne davon.</para>
/// </summary>
public sealed class WaermepumpeKonfigurationTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    // --- Heizstab ---------------------------------------------------------------
    //
    // DER SCHALTER HEISST SEIT DEM 16.09.2026 "Heizstab mitrechnen" (Schemaschritt 79).
    // Vorher stand da "Elektrische Nachheizung aktivieren (falls vorhanden)" - ein Text
    // aus der Zeit, als dieser Schalter gar nicht rechnete: Er entschied allein ueber die
    // Energietraegerwahl, gerechnet wurde der projektweite Tab_Einstellungen.WP_Heizstab.
    // Seit Schritt 79 gibt es nur noch DIESEN, und der Lauf liest ihn je Waermepumpe
    // (SimulationWaermepumpe.ModuleAufbauen). "Mitrechnen" sagt genau das; das
    // "(falls vorhanden)" ist entfallen, weil die Herleitungszeile darunter den Fall
    // ohne hinterlegte Heizstableistung ausdruecklich benennt.

    /// <summary>WPA_CHK_HEIZSTAB — der Text des Heizstab-Häkchens.</summary>
    public string LabelHeizstab { get; set; } = T("WPA_CHK_HEIZSTAB", "Heizstab mitrechnen");

    /// <summary>WPA_HRL_HEIZSTAB — die Herleitungszeile unter dem Häkchen.</summary>
    public string HinweisHeizstab { get; set; } = T("WPA_HRL_HEIZSTAB",
        "Der Heizstab dieser Wärmepumpe wird bei Unterdeckung zugeschaltet.");

    /// <summary>
    /// WPA_HINWEIS_HEIZSTAB_LEER — die zweite, leise Zeile, wenn die Projektkopie
    /// keine Heizstableistung führt (<c>Tab_WP.Heizung</c> = 0 oder leer).
    ///
    /// <para>Der Schalter bleibt dabei BEDIENBAR: Ob ein Heizstab mitgerechnet werden
    /// soll, ist eine Entscheidung; ob eine Leistung dafür hinterlegt ist, eine
    /// Tatsache. Ein gesperrter Schalter verschwiege, welche der beiden fehlt.</para>
    /// </summary>
    public string HinweisOhneHeizstableistung { get; set; } = T("WPA_HINWEIS_HEIZSTAB_LEER",
        "Für diese Wärmepumpe ist keine Heizstableistung hinterlegt (Feld „Heizstab kW“ im Block Stammdaten).");

    // --- Sperrzeit --------------------------------------------------------------

    /// <summary>WPA_LBL_SPERRZEIT — Titel der Formulargruppe (<c>label19</c> des Vorbilds).</summary>
    public string GruppeSperrzeit { get; set; } = T("WPA_LBL_SPERRZEIT",
        "Wärmepumpenleistung / maximale Betriebszeit:");

    /// <summary>WPA_CHK_SPERRZEIT — der Text des Sperrzeit-Häkchens.</summary>
    public string LabelSperrzeitSchalter { get; set; } = T("WPA_CHK_SPERRZEIT",
        "Sperrzeit durch Energieversorger");

    // --- Sperrzeiten (Welle V14) -------------------------------------------------

    public string GruppeSperrzeiten { get; set; } = T("WPA_GRP_SPERRZEITEN", "Sperrzeiten");
    public string LabelSperrVorlage { get; set; } = T("WPA_LBL_SPERR_VORLAGE", "Vorlage:");
    public string SperrVorlageKeine { get; set; } = T("WPA_BTN_SPERR_KEINE", "keine");
    public string SperrVorlage2x2 { get; set; } = T("WPA_BTN_SPERR_2X2", "2 × 2 h");
    public string SperrVorlage3x2 { get; set; } = T("WPA_BTN_SPERR_3X2", "3 × 2 h");
    public string LabelSperrVon { get; set; } = T("WPA_LBL_SPERR_VON", "Beginn");
    public string LabelSperrDauer { get; set; } = T("WPA_LBL_SPERR_DAUER", "Dauer");
    public string LabelSperrHeizstab { get; set; } = T("WPA_LBL_SPERR_HEIZSTAB", "Heizstab mitgesperrt");
    public string SperrHinzufuegen { get; set; } = T("WPA_BTN_SPERR_HINZU", "Fenster hinzufügen");
    public string SperrEntfernen { get; set; } = T("WPA_BTN_SPERR_ENTFERNEN", "Entfernen");
    public string HinweisSperrLeer { get; set; } = T("WPA_HINWEIS_SPERR_LEER", "Keine Sperrzeiten — die Wärmepumpe darf jederzeit laufen.");

    // --- Betriebszeiten (Anlagenkopplung 9.3) ----------------------------------------

    /// <summary>WPA_GRP_BETRIEBSZEITEN — die Gruppe mit Zeitprogramm und höchstem Vorlauf.</summary>
    public string GruppeBetriebszeiten { get; set; } = T("WPA_GRP_BETRIEBSZEITEN", "Betriebszeiten");

    /// <summary>WPA_LBL_ZEITPROGRAMM</summary>
    public string LabelZeitprogramm { get; set; } = T("WPA_LBL_ZEITPROGRAMM", "Zeitprogramm");

    /// <summary>WPA_BTN_ZEITPROGRAMM — öffnet das Wochenraster.</summary>
    public string KnopfZeitprogramm { get; set; } = T("WPA_BTN_ZEITPROGRAMM", "Wochenraster bearbeiten");

    /// <summary>WPA_BTN_ZEITPROGRAMM_SCHLIESSEN — klappt das Wochenraster wieder zu.</summary>
    public string KnopfZeitprogrammSchliessen { get; set; } = T("WPA_BTN_ZEITPROGRAMM_SCHLIESSEN", "Wochenraster schließen");

    /// <summary>WPA_HRL_ZEITPROGRAMM_LEER</summary>
    public string ZeileZeitprogrammLeer { get; set; } = T("WPA_HRL_ZEITPROGRAMM_LEER", "Nicht gepflegt — die Anlage ist immer verfügbar.");

    /// <summary>WPA_HRL_ZEITPROGRAMM_GEPFLEGT — {0} volle, {1} gesperrte Wochenstunden.</summary>
    public string ZeileZeitprogrammGepflegt { get; set; } = T("WPA_HRL_ZEITPROGRAMM_GEPFLEGT",
        "Gepflegt: {0} von 168 Wochenstunden mit voller Verfügbarkeit, {1} gesperrt.");

    /// <summary>WPA_HRL_ZEITPROGRAMM_FAKTOREN</summary>
    public string ZeileZeitprogrammFaktoren { get; set; } = T("WPA_HRL_ZEITPROGRAMM_FAKTOREN",
        "Faktor je Wochenstunde von 0 (gesperrt) bis 1 (volle Leistung), Montag 0 Uhr bis Sonntag 23 Uhr.");

    /// <summary>WPA_LBL_VORLAUF_MAX</summary>
    public string LabelVorlaufMax { get; set; } = T("WPA_LBL_VORLAUF_MAX", "Höchster Vorlauf");

    /// <summary>WPA_HRL_VORLAUF_MAX — {0} = projektierter Vorlauf.</summary>
    public string ZeileVorlaufMax { get; set; } = T("WPA_HRL_VORLAUF_MAX", "Vorgabe: projektierter Vorlauf {0} °C; leer = Vorgabe.");

    /// <summary>WPA_HRL_VORLAUF_MAX_OHNE — ohne projektierten Vorlauf.</summary>
    public string ZeileVorlaufMaxOhne { get; set; } = T("WPA_HRL_VORLAUF_MAX_OHNE", "Leer = projektierter Vorlauf der Anlage.");

    /// <summary>WPA_HINWEIS_SPERRZEIT_VORRANG</summary>
    public string HinweisSperrzeitVorrang { get; set; } = T("WPA_HINWEIS_SPERRZEIT_VORRANG",
        "Sperrzeit und Zeitprogramm gelten zusammen; die Sperrzeit geht vor.");

    /// <summary>WPA_HRL_BETRIEBSZEITEN_WIRKUNG</summary>
    public string ZeileBetriebszeitenWirkung { get; set; } = T("WPA_HRL_BETRIEBSZEITEN_WIRKUNG",
        "Zeitprogramm und höchster Vorlauf wirken mit der Anlagenkopplung auf die gekoppelten Gebäude des Projekts.");

    /// <summary>WPA_MSG_VORLAUF_MAX_BEREICH — {0} … {1} °C.</summary>
    public string MeldungVorlaufMaxBereich { get; set; } = T("WPA_MSG_VORLAUF_MAX_BEREICH",
        "Der höchste Vorlauf muss zwischen {0} und {1} °C liegen.");

    /// <summary>WPA_INFO_SPERRZEIT_ZEITPROGRAMM — {0} überschnittene Wochenstunden.</summary>
    public string InfoSperrzeitZeitprogramm { get; set; } = T("WPA_INFO_SPERRZEIT_ZEITPROGRAMM",
        "Sperrzeit und Zeitprogramm überschneiden sich in {0} Wochenstunden mit Faktor über 0; dort gilt die Sperrzeit.");
    public string HinweisSperrUebertrag { get; set; } = T("WPA_HINWEIS_SPERR_UEBERTRAG",
        "Ein Fenster über Mitternacht läuft in den Folgetag; gesperrt ist jede Stunde, deren Beginn im Fenster liegt.");
    public string EinheitStunden { get; set; } = T("WPA_EINHEIT_STUNDEN", "h");

    /// <summary>Die Kurznamen der Wochentage, Montag zuerst.</summary>
    public IReadOnlyList<string> Wochentage { get; set; } = new[]
    {
        T("WPA_WT_MO", "Mo"), T("WPA_WT_DI", "Di"), T("WPA_WT_MI", "Mi"), T("WPA_WT_DO", "Do"),
        T("WPA_WT_FR", "Fr"), T("WPA_WT_SA", "Sa"), T("WPA_WT_SO", "So")
    };

    /// <summary>WPA_LBL_VON</summary>
    public string LabelVon { get; set; } = T("WPA_LBL_VON", "Sperrzeit von");

    /// <summary>WPA_LBL_BIS</summary>
    public string LabelBis { get; set; } = T("WPA_LBL_BIS", "Sperrzeit bis");

    /// <summary>
    /// Einheit der Sperrzeit — <c>label35</c> des Vorbilds. Sie steht in beiden Sprachen
    /// gleich und braucht deshalb keinen Ressourcenschlüssel.
    /// </summary>
    public string EinheitStundeTag { get; set; } = "h/Tag";

    // --- Außentemperaturgesteuerter Betrieb -------------------------------------

    /// <summary>WPA_HINWEIS_BETRIEB — Titel der Formulargruppe.</summary>
    public string GruppeBetrieb { get; set; } = T("WPA_HINWEIS_BETRIEB",
        "Außentemperaturgesteuerter Betrieb:");

    /// <summary>WPA_LBL_BIVALENT</summary>
    public string LabelBivalent { get; set; } = T("WPA_LBL_BIVALENT", "Bivalenter Betrieb");

    /// <summary>WPA_LBL_BETRIEBSART</summary>
    public string LabelBetriebsart { get; set; } = T("WPA_LBL_BETRIEBSART", "Betriebsart");

    /// <summary>WPA_LBL_ABSCHALTTEMP</summary>
    public string LabelAbschalttemp { get; set; } = T("WPA_LBL_ABSCHALTTEMP", "Bivalenztemperatur");

    /// <summary>WPA_LBL_ABSCHALTTEMP — derselbe Schlüssel als FELDNAME der Meldung.</summary>
    public string LabelAbschalttempKurz { get; set; } = T("WPA_LBL_ABSCHALTTEMP", "Bivalenztemperatur");

    /// <summary>Einheit der Bivalenztemperatur; in beiden Sprachen gleich.</summary>
    public string EinheitGrad { get; set; } = "°C";

    // --- Energieträger (ET-5) ----------------------------------------------------

    /// <summary>ETW_GRP_TITEL — Titel der Formulargruppe.</summary>
    public string GruppeEnergietraeger { get; set; } = T("ETW_GRP_TITEL", "Energieträger");

    /// <summary>ETW_LBL_GRUPPE</summary>
    public string LabelTraegerGruppe { get; set; } = T("ETW_LBL_GRUPPE", "Energieträger:");

    /// <summary>ETW_LBL_ART</summary>
    public string LabelTraegerArt { get; set; } = T("ETW_LBL_ART", "Art:");

    // --- Kühlbetrieb (Stufe KU2 Welle 3; Kühlkonzept 8.2, E15, E33, E34) -------

    /// <summary>WPK_GRP_KUEHLBETRIEB — Titel der Formulargruppe.</summary>
    public string GruppeKuehlbetrieb { get; set; } = T("WPK_GRP_KUEHLBETRIEB", "Kühlbetrieb");

    /// <summary>WPK_CHK_KUEHLBETRIEB — der Schalter.</summary>
    public string LabelKuehlbetrieb { get; set; } = T("WPK_CHK_KUEHLBETRIEB", "Maschine auch zum Kühlen benutzen");

    /// <summary>WPK_HRL_NENNKUEHL_OHNE_KENNLINIE — die Warnung am gesperrten Schalter (8.2).</summary>
    public string WarnungNennkuehlleistungOhneKennlinie { get; set; } = T("WPK_HRL_NENNKUEHL_OHNE_KENNLINIE",
        "Nennkühlleistung ohne Kühlkennlinie — diese Maschine rechnet nur Wärme.");

    /// <summary>WPK_LBL_KUEHL_VORLAUF — die Auswahl des Kühl-Vorlaufs aus den Stützstellen (K21).</summary>
    public string LabelKuehlVorlauf { get; set; } = T("WPK_LBL_KUEHL_VORLAUF", "Kühl-Vorlauf");

    /// <summary>WPK_PH_KUEHL_VORLAUF — Vorgabe-Anzeige; {0} = kleinster Stützwert.</summary>
    public string PlatzhalterKuehlVorlauf { get; set; } = T("WPK_PH_KUEHL_VORLAUF",
        "Vorgabe: kleinster Stützwert ({0} °C)");

    /// <summary>WPK_HRL_UMSCHALTUNG — die feste Umschaltregel (5.2), keine Auswahl.</summary>
    public string HinweisUmschaltung { get; set; } = T("WPK_HRL_UMSCHALTUNG",
        "Umschaltung je Tag: Übersteigt der Kältebedarf eines Tages seinen Heizbedarf, kühlt die Maschine an diesem Tag; Brauchwasser bleibt bedienbar.");

    /// <summary>WPK_LBL_HILFSSTROM — der Hilfsstromanteil (K23), gezeigt in Prozent.</summary>
    public string LabelHilfsstrom { get; set; } = T("WPK_LBL_HILFSSTROM", "Hilfsstromanteil");

    /// <summary>WPK_PH_HILFSSTROM — Vorgabe-Anzeige.</summary>
    public string PlatzhalterHilfsstrom { get; set; } = T("WPK_PH_HILFSSTROM", "Vorgabe: kein Zuschlag");

    /// <summary>WPK_HRL_HILFSSTROM — was der Anteil bedeutet.</summary>
    public string HinweisHilfsstrom { get; set; } = T("WPK_HRL_HILFSSTROM",
        "Pumpen und Ventilatoren des Kältekreises als Anteil an der Verdichterarbeit.");

    /// <summary>WPK_MSG_HILFSSTROM_BEREICH — die Prüfregel (0 ≤ x &lt; 100 %).</summary>
    public string MeldungHilfsstromBereich { get; set; } = T("WPK_MSG_HILFSSTROM_BEREICH",
        "Der Hilfsstromanteil muss mindestens 0 % und weniger als 100 % betragen.");

    // --- Freie Kühlung über die Wärmequelle (KU3-6, F2, F6) ----------------------

    /// <summary>WPK_CHK_KUEHL_FREI — der Schalter.</summary>
    public string LabelKuehlFrei { get; set; } = T("WPK_CHK_KUEHL_FREI", "Freie Kühlung über die Wärmequelle");

    /// <summary>WPK_HRL_KUEHL_FREI — was die freie Kühlung tut.</summary>
    public string HinweisKuehlFrei { get; set; } = T("WPK_HRL_KUEHL_FREI",
        "Liegt die Quellentemperatur plus Grädigkeit unter dem Kühl-Vorlauf, deckt die Wärmequelle die Kälte der Stunde direkt — vor dem Verdichter.");

    /// <summary>WPK_LBL_KUEHL_FREI_GRAEDIGKEIT — die Grädigkeit des Wärmetauschers [K].</summary>
    public string LabelKuehlFreiGraedigkeit { get; set; } = T("WPK_LBL_KUEHL_FREI_GRAEDIGKEIT", "Grädigkeit des Wärmetauschers");

    /// <summary>WPK_PH_KUEHL_FREI_GRAEDIGKEIT — Vorgabe-Anzeige.</summary>
    public string PlatzhalterKuehlFreiGraedigkeit { get; set; } = T("WPK_PH_KUEHL_FREI_GRAEDIGKEIT", "Vorgabe: 3,0 K");

    /// <summary>WPK_HRL_KUEHL_FREI_GRAEDIGKEIT — die Herleitung der Vorgabe.</summary>
    public string HinweisKuehlFreiGraedigkeit { get; set; } = T("WPK_HRL_KUEHL_FREI_GRAEDIGKEIT",
        "Leer = 3,0 K. Temperaturabstand zwischen Quelle und Kaltwasser am Wärmetauscher, 0 bis 20 K.");

    /// <summary>WPK_LBL_KUEHL_FREI_LEISTUNG — die Leistungsgrenze der freien Kühlung [kW].</summary>
    public string LabelKuehlFreiLeistung { get; set; } = T("WPK_LBL_KUEHL_FREI_LEISTUNG", "Leistungsgrenze");

    /// <summary>WPK_PH_KUEHL_FREI_LEISTUNG — Vorgabe-Anzeige.</summary>
    public string PlatzhalterKuehlFreiLeistung { get; set; } = T("WPK_PH_KUEHL_FREI_LEISTUNG", "Vorgabe: Kälteleistung der Kennlinie");

    /// <summary>WPK_MSG_KUEHL_FREI_GRAEDIGKEIT — die Prüfregel (0 bis 20 K).</summary>
    public string MeldungKuehlFreiGraedigkeit { get; set; } = T("WPK_MSG_KUEHL_FREI_GRAEDIGKEIT",
        "Die Grädigkeit der freien Kühlung muss zwischen 0 und 20 K liegen.");

    /// <summary>WPK_MSG_KUEHL_FREI_LEISTUNG — die Prüfregel (&gt; 0 kW).</summary>
    public string MeldungKuehlFreiLeistung { get; set; } = T("WPK_MSG_KUEHL_FREI_LEISTUNG",
        "Die Leistungsgrenze der freien Kühlung muss größer als 0 kW sein.");

    /// <summary>WPK_FREI_SPERR_BAUART — Sperrgrund: keine Sole-/Wasser-Wasser-Maschine.</summary>
    public string SperrgrundFreiBauart { get; set; } = T("WPK_FREI_SPERR_BAUART",
        "Freie Kühlung nur an einer Sole-Wasser- oder Wasser-Wasser-Wärmepumpe — diese Maschine nutzt die Außenluft.");

    /// <summary>WPK_FREI_SPERR_QUELLE — Sperrgrund: Wärmequelle nicht gepflegt (Außenluft, leer, Pufferspeicher).</summary>
    public string SperrgrundFreiQuelle { get; set; } = T("WPK_FREI_SPERR_QUELLE",
        "Freie Kühlung braucht eine gepflegte Wärmequelle (Erdreich, Konstant, Profil oder CSV) — Außenluft, keine Angabe und Pufferspeicher wirken nicht.");

    /// <summary>WPK_LBL_KUEHLTRAEGER — der Stromträger des Kältestroms (K9).</summary>
    public string LabelKuehltraeger { get; set; } = T("WPK_LBL_KUEHLTRAEGER", "Stromträger des Kältestroms");

    /// <summary>WPK_PH_KUEHLTRAEGER — Vorgabe-Anzeige (NULL).</summary>
    public string PlatzhalterKuehltraeger { get; set; } = T("WPK_PH_KUEHLTRAEGER", "wie Heizbetrieb");

    /// <summary>WPK_HRL_KUEHLTRAEGER_WEITERE — wenn das Projekt keinen weiteren Stromträger führt.</summary>
    public string HinweisKuehltraegerWeitere { get; set; } = T("WPK_HRL_KUEHLTRAEGER_WEITERE",
        "Einen weiteren Stromträger ordnen Sie dem Projekt unter „Berichte & Kosten › Energieträger“ zu.");

    /// <summary>WPK_LBL_ABRECHNUNG — die Abrechnungsart bei abweichendem Kühlträger (E34).</summary>
    public string LabelAbrechnung { get; set; } = T("WPK_LBL_ABRECHNUNG", "Abrechnung des Kältestroms");

    /// <summary>WPK_OPT_ANTEILIG — Wahl 1, die Vorgabe.</summary>
    public string OptionAnteilig { get; set; } = T("WPK_OPT_ANTEILIG", "anteilig am Netzbezug (Vorgabe)");

    /// <summary>WPK_OPT_ZAEHLER — Wahl 2.</summary>
    public string OptionZaehler { get; set; } = T("WPK_OPT_ZAEHLER", "eigener Zähler");

    /// <summary>WPK_HRL_ANTEILIG — was Wahl 1 rechnet.</summary>
    public string HinweisAnteilig { get; set; } = T("WPK_HRL_ANTEILIG",
        "Ein Netzanschluss: Der Netzbezug jeder Viertelstunde wird nach dem Anteil des Kältestroms geteilt; dieser Anteil trägt Arbeitspreis und Emissionsfaktor des gewählten Stromträgers. Eigenstrom aus Photovoltaik und Stromspeicher bleibt gemeinsam, der Leistungspreis beim Stromträger des Projekts.");

    /// <summary>WPK_HRL_ZAEHLER — was Wahl 2 rechnet.</summary>
    public string HinweisZaehler { get; set; } = T("WPK_HRL_ZAEHLER",
        "Der ganze Kältestrom trägt Arbeitspreis und Emissionsfaktor des gewählten Stromträgers; er wird nicht aus Photovoltaik oder Stromspeicher gedeckt.");

    /// <summary>WPK_HRL_WIE_PROJEKT — wenn der gewählte Träger der des Projekts ist (die Wahl wirkt nicht).</summary>
    public string HinweisWieProjekt { get; set; } = T("WPK_HRL_WIE_PROJEKT",
        "Der Kältestrom trägt Tarif und Emissionsfaktor des Stromträgers des Projekts.");

    // --- Die drei Erklärkästen ---------------------------------------------------

    /// <summary>WPA_ERL_ALTERNATIV — der grüne Kasten (<c>label21</c> des Vorbilds).</summary>
    public string ErlaeuterungAlternativ { get; set; } = T("WPA_ERL_ALTERNATIV",
        "Bei der bivalent-alternativen Betriebsweise wird der Wärmebedarf bis zum Erreichen des "
        + "Bivalenzpunktes allein von der Wärmepumpe getragen. Der zweite Wärmeerzeuger springt bei "
        + "der Unterschreitung des Bivalenzpunktes ein und übernimmt den alleinigen Heizbetrieb.");

    /// <summary>WPA_ERL_PARALLEL — der gelbe Kasten (<c>label22</c>).</summary>
    public string ErlaeuterungParallel { get; set; } = T("WPA_ERL_PARALLEL",
        "Bei der bivalent-parallelen Betriebsweise wird der Wärmebedarf bis zum Erreichen des "
        + "Bivalenzpunktes allein von der Wärmepumpe getragen. Bei der Unterschreitung des "
        + "Bivalenzpunktes unterstützt der zweite Wärmeerzeuger den Heizbetrieb der Wärmepumpe.");

    /// <summary>WPA_ERL_TEILPARALLEL — der türkise Kasten (<c>label23</c>).</summary>
    public string ErlaeuterungTeilparallel { get; set; } = T("WPA_ERL_TEILPARALLEL",
        "Der bivalent-teilparallele Betrieb ist eine Mischung aus bivalent-paralleler und "
        + "bivalent-alternativer Betriebsweise. Die Wärmepumpe arbeitet bis zum Bivalenzpunkt allein "
        + "und wird anschließend vom zweiten Wärmeerzeuger unterstützt. Bei Erreichen einer weiteren "
        + "festgelegten Temperatur (z. B. -2 °C) schaltet sich die Wärmepumpe ab.");
}
