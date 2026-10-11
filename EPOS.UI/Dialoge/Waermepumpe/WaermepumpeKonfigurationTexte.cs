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
    /// <summary>WPA_CHK_SPERRZEITEN — das Kästchen über den Fenstern (angehakt = Sperrzeiten vorhanden).</summary>
    public string LabelSperrzeitenVorhanden { get; set; } = T("WPA_CHK_SPERRZEITEN", "Sperrzeiten vorhanden");
    /// <summary>WPA_FRAGE_SPERR_ENTFERNEN — Rückfrage beim Abhaken; {0} = Zahl der Fenster.</summary>
    public string FrageSperrzeitenEntfernen { get; set; } = T("WPA_FRAGE_SPERR_ENTFERNEN",
        "Alle {0} Sperrfenster entfernen?");
    /// <summary>WPA_BTN_SPERR_RUECKFRAGE_JA</summary>
    public string SperrRueckfrageJa { get; set; } = T("WPA_BTN_SPERR_RUECKFRAGE_JA", "Ja, entfernen");
    /// <summary>WPA_BTN_SPERR_RUECKFRAGE_NEIN</summary>
    public string SperrRueckfrageNein { get; set; } = T("WPA_BTN_SPERR_RUECKFRAGE_NEIN", "Nein, behalten");
    /// <summary>WPA_LBL_SPERR_TAGE — Name der Chipreihe der Wochentage (Sprachausgabe).</summary>
    public string LabelSperrTage { get; set; } = T("WPA_LBL_SPERR_TAGE", "Wochentage");
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
    // --- Übergabegrenze und Bivalenz (UB‑E1; Fachkonzept 6.2, Umsetzungskonzept 6.3, 6.5) -----------------

    /// <summary>WPA_LBL_KAELTEMITTEL — Klappliste vor der Schnellwahl.</summary>
    public string LabelKaeltemittel { get; set; } = T("WPA_LBL_KAELTEMITTEL", "Kältemittel");

    /// <summary>WPA_LBL_SCHNELLWAHL_KAELTEMITTEL — Beschriftung der Knopfgruppe (aria-label).</summary>
    public string LabelSchnellwahl { get; set; } = T("WPA_LBL_SCHNELLWAHL_KAELTEMITTEL", "Schnellwahl nach Kältemittel");

    /// <summary>WPA_OPT_KAELTEMITTEL_LEER — leere Wahl.</summary>
    public string KaeltemittelLeer { get; set; } = T("WPA_OPT_KAELTEMITTEL_LEER", "nicht gewählt");

    /// <summary>WPA_BTN_SCHNELLWAHL_R410A_R32 — {0} = Höchstvorlauf der Klasse.</summary>
    public string KnopfSchnellwahlR410a { get; set; } = T("WPA_BTN_SCHNELLWAHL_R410A_R32", "R410A / R32 · {0} °C");

    /// <summary>WPA_BTN_SCHNELLWAHL_R290 — {0} = Höchstvorlauf der Klasse.</summary>
    public string KnopfSchnellwahlR290 { get; set; } = T("WPA_BTN_SCHNELLWAHL_R290", "R290 · {0} °C");

    /// <summary>WPA_BTN_SCHNELLWAHL_R744 — {0} Höchstvorlauf, {1} Rücklaufgrenze, {2} Bezugsrücklauf.</summary>
    public string KnopfSchnellwahlR744 { get; set; } = T("WPA_BTN_SCHNELLWAHL_R744", "R744 · {0} °C, Rücklauf ≤ {1} °C (Abwertung ab {2} °C)");

    /// <summary>WPA_BTN_SCHNELLWAHL_R1234ZE — {0} = Höchstvorlauf der Klasse.</summary>
    public string KnopfSchnellwahlR1234ze { get; set; } = T("WPA_BTN_SCHNELLWAHL_R1234ZE", "R1234ze(E) · {0} °C");

    /// <summary>WPA_HRL_SCHNELLWAHL</summary>
    public string ZeileSchnellwahl { get; set; } = T("WPA_HRL_SCHNELLWAHL", "Die Schnellwahl speichert das Kältemittel und füllt nur leere Felder; Herstellerangaben zur Einsatzgrenze haben Vorrang.");

    /// <summary>WPA_HRL_SCHNELLWAHL_GEFUELLT — {0} = gepflegter Wert.</summary>
    public string ZeileSchnellwahlGefuellt { get; set; } = T("WPA_HRL_SCHNELLWAHL_GEFUELLT", "Der höchste Vorlauf ist gepflegt ({0} °C) — die Schnellwahl lässt ihn stehen.");

    /// <summary>WPA_HERLEITUNG_UEBERGABE — {0} Höchstvorlauf, {1} Übergabegrenze, {2} Heizlast, {3} Anteil, {4} Rücklauf, {5} Spreizung.</summary>
    public string HerleitungUebergabe { get; set; } = T("WPA_HERLEITUNG_UEBERGABE", "Übergabe bei Höchstvorlauf {0} °C: {1} kW von {2} kW Heizlast ({3} %), Rücklauf {4} °C, Spreizung {5} K");

    /// <summary>WPA_HERLEITUNG_BIVALENZPUNKTE — {0}–{2} Punkte mit Einheit, {3} Zusatz Vorwärmbetrieb.</summary>
    public string HerleitungBivalenzpunkte { get; set; } = T("WPA_HERLEITUNG_BIVALENZPUNKTE", "erster Bivalenzpunkt {0} (nach Kennfeld allein {1}) · zweiter Bivalenzpunkt {2}{3}");

    /// <summary>WPA_HERLEITUNG_VORWAERMBETRIEB — Zusatz am zweiten Bivalenzpunkt.</summary>
    public string HerleitungVorwaermbetrieb { get; set; } = T("WPA_HERLEITUNG_VORWAERMBETRIEB", " (Vorwärmbetrieb)");

    /// <summary>WPA_HERLEITUNG_KESSELANTEIL — {0} Anteil, {1} Mindestanteil.</summary>
    public string HerleitungKesselanteil { get; set; } = T("WPA_HERLEITUNG_KESSELANTEIL", "Wärmepumpe bei −7 °C {0} % der Kesselleistung (§ 43 GModG: mindestens {1} %)");

    /// <summary>WPA_HERLEITUNG_KEIN_PUNKT — Bivalenzpunkt außerhalb des Auslegungsbereichs.</summary>
    public string HerleitungKeinPunkt { get; set; } = T("WPA_HERLEITUNG_KEIN_PUNKT", "keiner");

    /// <summary>WPA_HERLEITUNG_OHNE_KOPPLUNG</summary>
    public string HerleitungOhneKopplung { get; set; } = T("WPA_HERLEITUNG_OHNE_KOPPLUNG", "Die Übergabe ist nicht beschrieben (Kopplung aus) — die Wärmepumpe rechnet ohne Übergabegrenze.");

    /// <summary>WPA_HERLEITUNG_NICHT_WIRKSAM — U-1.</summary>
    public string HerleitungNichtWirksam { get; set; } = T("WPA_HERLEITUNG_NICHT_WIRKSAM", "Einbindung nicht gesetzt — Übergabegrenze ruht.");

    /// <summary>WPA_HERLEITUNG_UNVOLLSTAENDIG</summary>
    public string HerleitungUnvollstaendig { get; set; } = T("WPA_HERLEITUNG_UNVOLLSTAENDIG", "Die Übergabegrenze wird ohne Lauf nur für gekoppelte Gebäude ohne Zonen mit Flächenangabe und einer Klimaregion des Projekts hergeleitet — hier erst im Lauf.");

    /// <summary>WPA_HERLEITUNG_OHNE_KENNFELD</summary>
    public string HerleitungOhneKennfeld { get; set; } = T("WPA_HERLEITUNG_OHNE_KENNFELD", "keine Wärmekennlinie der Wärmepumpe — keine Bivalenzpunkte");

    /// <summary>WPA_HERLEITUNG_BEFUND — {0} = Grund des Eingangsbauers.</summary>
    public string HerleitungBefund { get; set; } = T("WPA_HERLEITUNG_BEFUND", "Die Übergabegrenze ist nicht herleitbar: {0}");

    // --- Gruppe „Bivalenz und Übergabe" (Übergabegrenze UB‑E2, Umsetzungskonzept 6.2/6.5) -----------------

    /// <summary>WPA_GRP_BIVALENZ_UEBERGABE — Titel der Gruppe</summary>
    public string GruppeBivalenzUebergabe { get; set; } = T("WPA_GRP_BIVALENZ_UEBERGABE", "Bivalenz und Übergabe");

    /// <summary>WPA_LBL_EINBINDUNG</summary>
    public string LabelEinbindung { get; set; } = T("WPA_LBL_EINBINDUNG", "Einbindung");

    /// <summary>WPA_OPT_EINBINDUNG_DIREKT</summary>
    public string EinbindungDirekt { get; set; } = T("WPA_OPT_EINBINDUNG_DIREKT", "direkt (ohne Puffer)");

    /// <summary>WPA_OPT_EINBINDUNG_PUFFER</summary>
    public string EinbindungPuffer { get; set; } = T("WPA_OPT_EINBINDUNG_PUFFER", "Puffer");

    /// <summary>WPA_OPT_EINBINDUNG_WEICHE</summary>
    public string EinbindungWeiche { get; set; } = T("WPA_OPT_EINBINDUNG_WEICHE", "Weiche");

    /// <summary>WPA_OPT_EINBINDUNG_LEER</summary>
    public string EinbindungLeer { get; set; } = T("WPA_OPT_EINBINDUNG_LEER", "nicht gewählt");

    /// <summary>WPA_HINWEIS_EINBINDUNG_PUFFER</summary>
    public string HinweisEinbindungPuffer { get; set; } = T("WPA_HINWEIS_EINBINDUNG_PUFFER", "Puffer: Die Übergabe rechnet mit der Entladeseite als Näherung.");

    /// <summary>WPA_CHK_VORWAERMBETRIEB</summary>
    public string LabelVorwaermbetrieb { get; set; } = T("WPA_CHK_VORWAERMBETRIEB", "Vorwärmbetrieb (Kessel in Reihe)");

    /// <summary>WPA_HINWEIS_VORWAERMBETRIEB</summary>
    public string HinweisVorwaermbetrieb { get; set; } = T("WPA_HINWEIS_VORWAERMBETRIEB", "Die Wärmepumpe wärmt den Rücklauf bis zum Höchstvorlauf vor, der Kessel hebt auf den Sollvorlauf.");

    /// <summary>WPA_HINWEIS_VORWAERMBETRIEB_ALTERNATIV</summary>
    public string HinweisVorwaermbetriebAlternativ { get; set; } = T("WPA_HINWEIS_VORWAERMBETRIEB_ALTERNATIV", "Vorwärmbetrieb ist bei alternativ nicht wählbar.");

    /// <summary>WPA_LBL_HOECHSTVORLAUF_LESEN</summary>
    public string LabelHoechstvorlaufLesen { get; set; } = T("WPA_LBL_HOECHSTVORLAUF_LESEN", "Höchster Vorlauf");

    /// <summary>WPA_LBL_SPREIZUNG_AUSLEGUNG</summary>
    public string LabelSpreizungAuslegung { get; set; } = T("WPA_LBL_SPREIZUNG_AUSLEGUNG", "Spreizung Auslegung");

    /// <summary>WPA_LBL_SPREIZUNG_MAX</summary>
    public string LabelSpreizungMax { get; set; } = T("WPA_LBL_SPREIZUNG_MAX", "max.");

    /// <summary>WPA_LBL_SPREIZUNG_MIN</summary>
    public string LabelSpreizungMin { get; set; } = T("WPA_LBL_SPREIZUNG_MIN", "min.");

    /// <summary>WPA_LBL_MINDESTVOLUMENSTROM</summary>
    public string LabelMindestvolumenstrom { get; set; } = T("WPA_LBL_MINDESTVOLUMENSTROM", "Mindestvolumenstrom");

    /// <summary>WPA_LBL_RUECKLAUF_MAX</summary>
    public string LabelRuecklaufMax { get; set; } = T("WPA_LBL_RUECKLAUF_MAX", "Höchster Rücklauf");

    /// <summary>WPA_LBL_RUECKLAUF_R744</summary>
    public string LabelRuecklaufR744 { get; set; } = T("WPA_LBL_RUECKLAUF_R744", "Rücklauf R744: Bezug / Abwertung / Grenze");

    /// <summary>WPA_HERKUNFT_KATALOG</summary>
    public string HerkunftKatalog { get; set; } = T("WPA_HERKUNFT_KATALOG", "Katalog");

    /// <summary>WPA_HERKUNFT_VORGABE_KAELTEMITTEL</summary>
    public string HerkunftVorgabeKaeltemittel { get; set; } = T("WPA_HERKUNFT_VORGABE_KAELTEMITTEL", "Vorgabe nach Kältemittel");

    /// <summary>WPA_HERKUNFT_VORGABE</summary>
    public string HerkunftVorgabe { get; set; } = T("WPA_HERKUNFT_VORGABE", "Vorgabe");

    /// <summary>WPA_HERKUNFT_ABGELEITET</summary>
    public string HerkunftAbgeleitet { get; set; } = T("WPA_HERKUNFT_ABGELEITET", "abgeleitet");

    /// <summary>WPA_HERKUNFT_NUR_R744</summary>
    public string HerkunftNurR744 { get; set; } = T("WPA_HERKUNFT_NUR_R744", "nur R744");

    /// <summary>WPA_HERLEITUNG_RUECKLAUF_ABGELEITET — {0} Grenze, {1} Höchstvorlauf, {2} Mindestspreizung</summary>
    public string HerleitungRuecklaufAbgeleitet { get; set; } = T("WPA_HERLEITUNG_RUECKLAUF_ABGELEITET", "abgeleitet: {0} °C = {1} − {2} (Höchstvorlauf − Mindestspreizung) · ein kleinerer Katalogwert gilt vor · darüber liefert die Wärmepumpe in der Stunde nichts");

    /// <summary>WPA_HERLEITUNG_R744 — {0} Bezug, {1} Abwertung, {2} Grenze</summary>
    public string HerleitungR744 { get; set; } = T("WPA_HERLEITUNG_R744", "bei R744: Bezugsrücklauf {0} °C · Abwertung {1} %/K · Grenze {2} °C — Leistung und COP sinken je K über {0} °C um {1} %");

    /// <summary>WPA_HINWEIS_GERAETEGRENZEN</summary>
    public string HinweisGeraetegrenzen { get; set; } = T("WPA_HINWEIS_GERAETEGRENZEN", "Gerätegrenzen werden im Katalog gepflegt; leere Felder rechnen mit der Vorgabe.");

    /// <summary>WPA_HERLEITUNG_ABSCHALTPUNKT — {0}–{2} Punkte mit Einheit</summary>
    public string HerleitungAbschaltpunkt { get; set; } = T("WPA_HERLEITUNG_ABSCHALTPUNKT", "eingegeben {0} · aus der Übergabe berechnet {1} · maßgebend {2}");

    /// <summary>WPA_WARN_SPREIZUNG</summary>
    public string WarnSpreizung { get; set; } = T("WPA_WARN_SPREIZUNG", "Die Mindestspreizung ({0} K) ist nicht kleiner als die Höchstspreizung ({1} K) — die Wärmepumpe findet keinen Betriebspunkt.");

    /// <summary>WPA_WARN_HOECHSTVORLAUF</summary>
    public string WarnHoechstvorlauf { get; set; } = T("WPA_WARN_HOECHSTVORLAUF", "Der höchste Vorlauf ({0} °C) liegt unter dem Auslegungsvorlauf der Flächenheizung ({1} °C).");

    /// <summary>WPA_WARN_RUECKLAUF_NIE</summary>
    public string WarnRuecklaufNie { get; set; } = T("WPA_WARN_RUECKLAUF_NIE", "Höchster Rücklauf {0} °C liegt unter dem Rücklauf an der Übergabegrenze ({1} °C) — die Wärmepumpe liefert bei diesem Rücklauf nie.");

    /// <summary>WPA_WARN_VORWAERM_OHNE_KESSEL</summary>
    public string WarnVorwaermOhneKessel { get; set; } = T("WPA_WARN_VORWAERM_OHNE_KESSEL", "Vorwärmbetrieb ohne Kessel oder Heizstab in der Kaskade — niemand hebt auf den Sollvorlauf.");

    /// <summary>WPA_WARN_KASKADE</summary>
    public string WarnKaskade { get; set; } = T("WPA_WARN_KASKADE", "Die Wärmepumpe steht in der Kaskade hinter dem Kessel (Platz {0} nach Platz {1}) — im Vorwärmbetrieb gehört sie davor.");

    /// <summary>WPA_WARN_GMODG</summary>
    public string WarnGmodg { get; set; } = T("WPA_WARN_GMODG", "Die Wärmepumpe erreicht bei −7 °C {0} % der Kesselleistung; § 43 GModG verlangt mindestens {1} %.");

    /// <summary>WPA_WARN_UEBERGABE_BEGRENZT</summary>
    public string WarnUebergabeBegrenzt { get; set; } = T("WPA_WARN_UEBERGABE_BEGRENZT", "Die Heizflächen begrenzen die Wärmepumpe stärker als ihr Kennfeld: Unter {0} reicht der Höchstvorlauf nicht mehr für die Heizlast.");

    /// <summary>
    /// WPA_OPT_KAELTEMITTEL_&lt;Code&gt; — die Anzeige eines Kältemittelcodes; der Schlüssel trägt den Code in
    /// Großbuchstaben ohne Satzzeichen (<c>R1234ze(E)</c> → <c>R1234ZEE</c>). Ein unbekannter Code zeigt sich selbst.
    /// </summary>
    public string KaeltemittelText(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return KaeltemittelLeer;
        string s = new string(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return T("WPA_OPT_KAELTEMITTEL_" + s, code);
    }

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
