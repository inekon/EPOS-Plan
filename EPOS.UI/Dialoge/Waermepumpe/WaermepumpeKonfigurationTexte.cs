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
