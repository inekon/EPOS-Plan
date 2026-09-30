namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte des Reiters „Konditionierung“ des Gebäude-Katalogeditors (Teilkonzept
/// Konditionierungsprofile 7, Stufe KP2) — EIN Parameter statt hundertvierzig: die fünf Größen
/// und die Zeilen der Vorgabe-Matrix, die Zustände und Knöpfe der Kalenderkarten, die Kalenderkarte
/// selbst, die Vorlagen und die Hinweise.
/// </summary>
/// <remarks>
/// <para><b>Warum gebündelt.</b> Hausregel „ab etwa zehn Anzeigetexten ein Bündel“
/// (<c>EPOS.UI/CLAUDE.md</c>). Beschriftungen, kein Zustand — was die Komponente rechnet oder
/// hält, steht in ihren Gaben.</para>
/// <para><b>Schlüssel.</b> Jede Eigenschaft trägt ihren Ressourcenschlüssel im Kommentar; der
/// Vorgabewert ist der deutsche Rückfall. <c>KOND_LBL_*</c> sind Beschriftungen, <c>KOND_BTN_*</c>
/// Knöpfe, <c>KOND_TXT_*</c> Hinweise, Zustände und Platzhalter mit oder ohne <c>{n}</c>. Nicht hier
/// stehen die Meldungen des Kerns (<c>KOND_MSG_*</c>, <c>SIMENG_KOND_*</c>), die Wochentage und
/// Feiertagsnamen (<c>KOND_TEXT_WOCHENTAGE</c>, <c>KOND_TEXT_FEIERTAGE</c>) und die Rückfragen der
/// Dialoge (<c>KOND_FRAGE_*</c>, Stufen U1 bis U4).</para>
/// <para><b>Begriffe</b> nach Glossar § 13, Abschnitt „Konditionierungsprofile“
/// (<c>Dokumentation/aktuell/Glossar_Lokalisierung.md</c>). Die Namen der Vorlagen („Wohnen“,
/// „Büro“, „Schule“) sind Daten und bleiben deutsch (Glossar § 10); übersetzt wird allein ihre
/// <b>Nutzung</b> als Anzeigewert.</para>
/// <para><b>Einzahl.</b> Der Kartenzustand „angelegt, {0} eigene Perioden“ gilt für jede Zahl außer 1;
/// für genau eine Periode gilt <see cref="ZustandAngelegtEine"/>.</para>
/// <para>Gefüllt wird das Bündel von <c>KonditionierungTexteHuelle.Texte()</c> in
/// <c>EPOS.UI.Daten</c>; <c>KonditionierungTexteTests</c> hält Bündel, Füllung und beide
/// Ressourcendateien zusammen.</para>
/// </remarks>
public sealed class KonditionierungTexte
{
    // ------------------------------------------------------------ Reiter, Matrix und die fünf Größen

    /// <summary><c>KOND_LBL_REITER</c> — der Reiter des Katalogeditors</summary>
    public string Reiter { get; set; } = "Konditionierung";

    /// <summary><c>KOND_LBL_MATRIX</c> — die Überschrift der Matrix</summary>
    public string Matrix { get; set; } = "Vorgabe-Matrix";

    /// <summary>
    /// <c>KOND_LBL_GROESSE_HEIZEN</c> — Größe Heizsollwert; Name für Karte und Reiter der schmalen
    /// Anordnung
    /// </summary>
    public string GroesseHeizen { get; set; } = "Heizen";

    /// <summary><c>KOND_LBL_GROESSE_KUEHLEN</c> — Größe Kühlsollwert</summary>
    public string GroesseKuehlen { get; set; } = "Kühlen";

    /// <summary><c>KOND_LBL_GROESSE_LUEFTUNG</c> — Größe Nutzerlüftung</summary>
    public string GroesseLueftung { get; set; } = "Lüftung";

    /// <summary>
    /// <c>KOND_LBL_GROESSE_GERAETE</c> — Größe Geräte und Anlage (einschließlich Beleuchtung)
    /// </summary>
    public string GroesseGeraete { get; set; } = "Geräte";

    /// <summary><c>KOND_LBL_GROESSE_PERSONEN</c> — Größe Personen (Anwesenheit)</summary>
    public string GroessePersonen { get; set; } = "Personen";

    /// <summary><c>KOND_LBL_SPALTE_HEIZEN</c> — Spaltenkopf mit Einheit</summary>
    public string SpalteHeizen { get; set; } = "Heizen °C";

    /// <summary><c>KOND_LBL_SPALTE_KUEHLEN</c> — Spaltenkopf mit Einheit</summary>
    public string SpalteKuehlen { get; set; } = "Kühlen °C";

    /// <summary><c>KOND_LBL_SPALTE_LUEFTUNG</c> — Spaltenkopf mit Einheit</summary>
    public string SpalteLueftung { get; set; } = "Lüftung 1/h";

    /// <summary>
    /// <c>KOND_LBL_SPALTE_GERAETE</c> — Spaltenkopf mit Einheit; der Nennwert in W, die übrigen Zeilen in
    /// %
    /// </summary>
    public string SpalteGeraete { get; set; } = "Geräte W bzw. %";

    /// <summary>
    /// <c>KOND_LBL_SPALTE_PERSONEN</c> — Spaltenkopf mit Einheit; der Nennwert in W, die übrigen Zeilen
    /// in %
    /// </summary>
    public string SpaltePersonen { get; set; } = "Personen W bzw. %";

    // ------------------------------------------------------------ Zeilen der Matrix

    /// <summary><c>KOND_LBL_ZEILE_VORLAGE</c> — die zuletzt übernommene Vorlage je Spalte</summary>
    public string ZeileVorlage { get; set; } = "Vorlage";

    /// <summary>
    /// <c>KOND_LBL_ZEILE_NENNWERT</c> — Bezugswert der Anteile; bei Lüftung die Infiltration
    /// </summary>
    public string ZeileNennwert { get; set; } = "Nennwert";

    /// <summary><c>KOND_LBL_ZEILE_TAG</c></summary>
    public string ZeileTag { get; set; } = "Tag";

    /// <summary><c>KOND_LBL_ZEILE_NACHT</c></summary>
    public string ZeileNacht { get; set; } = "Nacht";

    /// <summary><c>KOND_LBL_ZEILE_WOCHENENDE</c></summary>
    public string ZeileWochenende { get; set; } = "Wochenende";

    /// <summary><c>KOND_LBL_ZEILE_FERIEN</c></summary>
    public string ZeileFerien { get; set; } = "Ferien";

    /// <summary><c>KOND_LBL_ZEILE_SAISON</c> — Heiz- bzw. Kühlperiode mit Start und Ende</summary>
    public string ZeileSaison { get; set; } = "Saison";

    /// <summary>
    /// <c>KOND_LBL_FELD_HEIZEN_WOCHENENDE</c> — der Feldname der Heizzelle Wochenende (Bedienhilfe,
    /// Fehlermeldung, Wiki)
    /// </summary>
    public string FeldHeizenWochenende { get; set; } = "Soll am Wochenende (ganztägig)";

    /// <summary><c>KOND_LBL_FELD_HEIZEN_FERIEN</c> — der Feldname der Heizzelle Ferien</summary>
    public string FeldHeizenFerien { get; set; } = "Soll in Ferien (ganztägig)";

    /// <summary><c>KOND_LBL_NACHTFENSTER</c> — die Nachtzeit einer Spalte</summary>
    public string LabelNachtfenster { get; set; } = "Nachtfenster";

    /// <summary>
    /// <c>KOND_LBL_NACHTFENSTER_VON</c> — Beschriftung des Stundenfelds (volle Stunde 0 … 23)
    /// </summary>
    public string LabelNachtfensterVon { get; set; } = "Nachtfenster von";

    /// <summary>
    /// <c>KOND_LBL_NACHTFENSTER_BIS</c> — Beschriftung des Stundenfelds (volle Stunde 0 … 23)
    /// </summary>
    public string LabelNachtfensterBis { get; set; } = "Nachtfenster bis";

    /// <summary><c>KOND_LBL_SAISON_START</c> — Datumsfeld im Gemeinjahr</summary>
    public string LabelSaisonStart { get; set; } = "Start der Saison";

    /// <summary><c>KOND_LBL_SAISON_ENDE</c> — Datumsfeld im Gemeinjahr</summary>
    public string LabelSaisonEnde { get; set; } = "Ende der Saison";

    // ------------------------------------------------------------ Zusatzzeilen, Lüftung und Herleitungen

    /// <summary>
    /// <c>KOND_LBL_FERIENZEITRAEUME</c> — die datierten Zeiträume des Gebäudes (gelten für alle Spalten)
    /// </summary>
    public string LabelFerienzeitraeume { get; set; } = "Ferienzeiträume";

    /// <summary>
    /// <c>KOND_LBL_MAXRAUMTEMPERATUR</c> — die Grenze der Überhitzungsstunden (Feld
    /// <c>Maximaleraumtemperatur</c>)
    /// </summary>
    public string LabelMaxRaumtemperatur { get; set; } = "Maximalraumtemperatur";

    /// <summary><c>KOND_LBL_SOMMERLUEFTUNG</c> — der Schalter</summary>
    public string LabelSommerlueftung { get; set; } = "Sommerlüftung";

    /// <summary><c>KOND_LBL_INFILTRATION</c> — die feste Zeile der Lüftungsspalte (Nennwert)</summary>
    public string LabelInfiltration { get; set; } = "Infiltration";

    /// <summary><c>KOND_LBL_NUTZERLUEFTUNG</c> — der Tagwert der Lüftungsspalte</summary>
    public string LabelNutzerlueftung { get; set; } = "Nutzerlüftung";

    /// <summary><c>KOND_LBL_NACHTAUSKUEHLUNG</c> — der Nachtwert der Lüftungsspalte</summary>
    public string LabelNachtauskuehlung { get; set; } = "Nachtauskühlung";

    /// <summary>
    /// <c>KOND_LBL_AUSSENABSTAND</c> — der Mindestabstand der Außenluft zur Raumluft in K (Vorgabe 2 K)
    /// </summary>
    public string LabelAussenabstand { get; set; } = "ΔT Außenluft";

    /// <summary>
    /// <c>KOND_TXT_JAHRESMITTEL</c> — die Zeile unter der Matrix; „{0}“ Geräte [W], „{1}“ Personen [W],
    /// „{2}“ Summe [W], „{3}“ Summe [W/m²], „{4}“ die Gesamtangabe <c>Interne_Waermegewinne</c> [W]
    /// </summary>
    public string TextJahresmittel { get; set; }
        = "Jahresmittel der Gewinne: Geräte {0} W, Personen {1} W, zusammen {2} W = {3} W/m²; Interne "
        + "Wärmegewinne (Gesamtangabe): {4} W.";

    /// <summary>
    /// <c>KOND_TXT_HERLEITUNG_PERSONEN</c> — Herleitung des Nennwerts der Personen; „{0}“ Personenzahl,
    /// „{1}“ Wärme je Person [W], „{2}“ Nennwert [W]
    /// </summary>
    public string TextHerleitungPersonen { get; set; } = "Personen: {0} × {1} W = {2} W (sensible Wärme)";

    /// <summary>
    /// <c>KOND_TXT_HERLEITUNG_GERAETE</c> — Herleitung des Nennwerts der Geräte; „{0}“ Nennwert [W],
    /// „{1}“ Interne Wärmegewinne (Gesamtangabe) [W], „{2}“ Personenmittel [W]
    /// </summary>
    public string TextHerleitungGeraete { get; set; }
        = "Geräte: {0} W = Interne Wärmegewinne {1} W − Jahresmittel der Personenwärme {2} W";

    /// <summary>
    /// <c>KOND_TXT_KEINE</c> — Platzhalter einer leeren, unwirksamen Zelle (Wochenendwert bis 5 °C,
    /// Ferienwert unter 1 °C)
    /// </summary>
    public string PlatzhalterKeine { get; set; } = "keine";

    /// <summary><c>KOND_TXT_LEER</c> — Platzhalter einer leeren Zelle (wie Tag bzw. wie Werktag)</summary>
    public string PlatzhalterLeer { get; set; } = "—";

    /// <summary>
    /// <c>KOND_TXT_VORGABE</c> — Platzhalter einer geerbten Zonenzelle; „{0}“ ist der Wert des Gebäudes
    /// </summary>
    public string PlatzhalterVorgabe { get; set; } = "Vorgabe {0}";

    /// <summary><c>KOND_TXT_GANZJAEHRIG</c> — Platzhalter einer leeren Saison</summary>
    public string PlatzhalterGanzjaehrig { get; set; } = "ganzjährig";

    /// <summary><c>KOND_LBL_AUS</c> — der Zellzustand „aus“ (Heizen und Kühlen; Wochenraster)</summary>
    public string ZelleAus { get; set; } = "aus";

    /// <summary><c>KOND_LBL_AUS_SCHALTER</c> — Beschriftung des Schalters je Zelle</summary>
    public string AusSchalter { get; set; } = "Zelle auf „aus“ setzen";

    // ------------------------------------------------------------ Zustand einer Kalenderkarte

    /// <summary><c>KOND_TXT_ZUSTAND_MATRIX</c> — der Kalender ist abgeleitet</summary>
    public string ZustandMatrix { get; set; } = "aus der Matrix";

    /// <summary>
    /// <c>KOND_TXT_ZUSTAND_VORLAGE</c> — „{0}“ ist der Name der Vorlage (ein Datenwert, nicht übersetzt)
    /// </summary>
    public string ZustandVorlage { get; set; } = "aus Vorlage {0}";

    /// <summary>
    /// <c>KOND_TXT_ZUSTAND_ANGELEGT</c> — „{0}“ ist die Zahl der eigenen Perioden (nicht 1)
    /// </summary>
    public string ZustandAngelegt { get; set; } = "angelegt, {0} eigene Perioden";

    /// <summary><c>KOND_TXT_ZUSTAND_ANGELEGT_EINE</c> — genau eine eigene Periode</summary>
    public string ZustandAngelegtEine { get; set; } = "angelegt, 1 eigene Periode";

    /// <summary><c>KOND_TXT_ZUSTAND_GEBAEUDE</c> — die Zone folgt dem Kalender des Gebäudes</summary>
    public string ZustandGebaeude { get; set; } = "vom Gebäude";

    /// <summary><c>KOND_TXT_ZUSTAND_EIGEN</c> — die Zone führt einen eigenen Kalender</summary>
    public string ZustandEigen { get; set; } = "eigener Kalender";

    // ------------------------------------------------------------ Knöpfe

    /// <summary><c>KOND_BTN_KALENDER_ANLEGEN</c> — schreibt die Kalender aus der Matrix</summary>
    public string KnopfKalenderAnlegen { get; set; } = "Kalender anlegen";

    /// <summary><c>KOND_BTN_VERWERFEN</c> — löscht die angelegten Zeilen, zurück zur Matrix</summary>
    public string KnopfVerwerfen { get; set; } = "Verwerfen";

    /// <summary><c>KOND_BTN_MATRIX_ERNEUT</c> — ersetzt nur den Matrixbereich; mit Rückfrage</summary>
    public string KnopfMatrixErneut { get; set; } = "Matrix erneut anwenden…";

    /// <summary><c>KOND_BTN_ZURUECKNEHMEN</c> — nimmt den letzten Schritt des Arbeitsstands zurück</summary>
    public string KnopfZuruecknehmen { get; set; } = "Zurücknehmen";

    /// <summary><c>KOND_BTN_UEBERNEHMEN</c> — übernimmt die gewählte Vorlage</summary>
    public string KnopfUebernehmen { get; set; } = "Übernehmen";

    /// <summary><c>KOND_BTN_ALS_VORLAGE</c> — fragt Name, Beschreibung und Nutzung</summary>
    public string KnopfAlsVorlage { get; set; } = "Als Vorlage speichern…";

    /// <summary>
    /// <c>KOND_BTN_VORLAGEN_VERWALTEN</c> — öffnet die Verwaltung als Blatt im Katalogeditor
    /// </summary>
    public string KnopfVorlagenVerwalten { get; set; } = "Vorlagen verwalten";

    /// <summary><c>KOND_BTN_KATALOG_ERNEUT</c> — im Reiterkopf, nur im Projekt</summary>
    public string KnopfKatalogErneut { get; set; } = "Aus dem Katalog erneut übernehmen…";

    /// <summary>
    /// <c>KOND_BTN_UEBERNEHMEN_ANPASSEN</c> — Zone: legt eine eigene Kopie des Gebäudekalenders an
    /// </summary>
    public string KnopfUebernehmenAnpassen { get; set; } = "Vom Gebäude übernehmen und anpassen";

    /// <summary><c>KOND_BTN_ERBEN</c> — Zone: leert die Zelle, sie gilt wieder vom Gebäude</summary>
    public string KnopfErben { get; set; } = "Erben";

    /// <summary><c>KOND_BTN_DUPLIZIEREN</c> — Vorlagenverwaltung</summary>
    public string KnopfDuplizieren { get; set; } = "Duplizieren";

    /// <summary><c>KOND_BTN_UMBENENNEN</c> — Vorlagenverwaltung</summary>
    public string KnopfUmbenennen { get; set; } = "Umbenennen";

    /// <summary><c>KOND_BTN_LOESCHEN</c> — Vorlagenverwaltung</summary>
    public string KnopfLoeschen { get; set; } = "Löschen";

    /// <summary><c>KOND_BTN_SPEICHERN</c> — Abfrage von „Als Vorlage speichern…“</summary>
    public string KnopfSpeichern { get; set; } = "Speichern";

    /// <summary><c>KOND_BTN_ABBRECHEN</c> — Abfrage von „Als Vorlage speichern…“</summary>
    public string KnopfAbbrechen { get; set; } = "Abbrechen";

    /// <summary><c>KOND_BTN_SCHLIESSEN</c> — Vorlagenverwaltung</summary>
    public string KnopfSchliessen { get; set; } = "Schließen";

    /// <summary><c>KOND_BTN_ZEITFENSTER</c> — trägt das Zeitfenster in die Standardwoche ein</summary>
    public string KnopfZeitfenster { get; set; } = "Zeitfenster eintragen";

    /// <summary><c>KOND_BTN_FEIERTAGE</c> — Werkzeug Feiertage</summary>
    public string KnopfFeiertage { get; set; } = "Feiertage als Regel anlegen";

    /// <summary><c>KOND_BTN_ZEITSTRUKTUR</c> — Werkzeug Zeitstruktur</summary>
    public string KnopfZeitstruktur { get; set; } = "Zeitstruktur übernehmen";

    /// <summary><c>KOND_BTN_PERIODE_NEU</c> — Periodenliste</summary>
    public string KnopfPeriodeNeu { get; set; } = "Periode hinzufügen";

    /// <summary><c>KOND_BTN_PERIODE_BEARBEITEN</c> — Periodenliste (Zeilenknopf ✎)</summary>
    public string KnopfPeriodeBearbeiten { get; set; } = "Periode bearbeiten";

    /// <summary><c>KOND_BTN_PERIODE_LOESCHEN</c> — Periodenliste</summary>
    public string KnopfPeriodeLoeschen { get; set; } = "Periode löschen";

    /// <summary><c>KOND_BTN_RANG_HOEHER</c> — Periodenliste (▲)</summary>
    public string KnopfRangHoeher { get; set; } = "Rang erhöhen";

    /// <summary><c>KOND_BTN_RANG_NIEDRIGER</c> — Periodenliste (▼)</summary>
    public string KnopfRangNiedriger { get; set; } = "Rang senken";

    // ------------------------------------------------------------ Kalenderkarte

    /// <summary><c>KOND_LBL_GRUNDANGABE</c> — Ebene 1: ein Wert oder „aus“</summary>
    public string LabelGrundangabe { get; set; } = "Grundangabe";

    /// <summary>
    /// <c>KOND_LBL_STANDARDWOCHE</c> — Ebene 2: 168 Zellen von Montag 00:00 bis Sonntag 23:00
    /// </summary>
    public string LabelStandardwoche { get; set; } = "Standardwoche";

    /// <summary><c>KOND_LBL_WOCHENRASTER</c> — der Baustein zur Standardwoche</summary>
    public string LabelWochenraster { get; set; } = "Wochenraster";

    /// <summary><c>KOND_LBL_ZEITFENSTER</c> — Überschrift des Werkzeugs</summary>
    public string LabelZeitfenster { get; set; } = "Zeitfenster";

    /// <summary><c>KOND_LBL_ZEITFENSTER_TAGE</c> — die sieben Tagesknöpfe</summary>
    public string LabelZeitfensterTage { get; set; } = "Tage";

    /// <summary><c>KOND_LBL_ZEITFENSTER_VON</c> — Stunde 0 … 24</summary>
    public string LabelZeitfensterVon { get; set; } = "Von";

    /// <summary><c>KOND_LBL_ZEITFENSTER_BIS</c> — Stunde 0 … 24</summary>
    public string LabelZeitfensterBis { get; set; } = "Bis";

    /// <summary><c>KOND_LBL_ZEITFENSTER_WERT</c> — Zahl oder „aus“</summary>
    public string LabelZeitfensterWert { get; set; } = "Wert";

    /// <summary><c>KOND_LBL_PERIODEN</c> — Überschrift der Periodenliste</summary>
    public string LabelPerioden { get; set; } = "Perioden";

    /// <summary><c>KOND_LBL_SPALTE_ART</c> — Periodenliste</summary>
    public string SpalteArt { get; set; } = "Art";

    /// <summary><c>KOND_LBL_SPALTE_NAME</c> — Periodenliste</summary>
    public string SpalteName { get; set; } = "Name";

    /// <summary><c>KOND_LBL_SPALTE_VON</c> — Periodenliste: Beginn als Datum im Gemeinjahr</summary>
    public string SpalteVon { get; set; } = "Von";

    /// <summary><c>KOND_LBL_SPALTE_BIS</c> — Periodenliste: Ende als Datum im Gemeinjahr</summary>
    public string SpalteBis { get; set; } = "Bis";

    /// <summary><c>KOND_LBL_SPALTE_WERT</c> — Periodenliste</summary>
    public string SpalteWert { get; set; } = "Wert";

    /// <summary><c>KOND_LBL_SPALTE_RANG</c> — Periodenliste</summary>
    public string SpalteRang { get; set; } = "Rang";

    /// <summary>
    /// <c>KOND_LBL_ART_ZEITRAUM</c> — Anzeigewert der Art <c>ZEITRAUM</c> (der Persistenzwert bleibt
    /// deutsch)
    /// </summary>
    public string ArtZeitraum { get; set; } = "Zeitraum";

    /// <summary><c>KOND_LBL_ART_FERIEN</c> — Anzeigewert der Art <c>FERIEN</c></summary>
    public string ArtFerien { get; set; } = "Ferien";

    /// <summary><c>KOND_LBL_ART_FEIERTAG</c> — Anzeigewert der Art <c>FEIERTAG</c></summary>
    public string ArtFeiertag { get; set; } = "Feiertag";

    /// <summary><c>KOND_LBL_ART_BETRIEBSPAUSE</c> — Anzeigewert der Art <c>BETRIEBSPAUSE</c></summary>
    public string ArtBetriebspause { get; set; } = "Betriebspause";

    /// <summary>
    /// <c>KOND_TXT_WERT_EIGENE_WOCHE</c> — Angabe einer Periode: sie trägt eine eigene Woche
    /// </summary>
    public string WertEigeneWoche { get; set; } = "eigene Woche";

    /// <summary>
    /// <c>KOND_TXT_WERT_WIE_WOCHENTAG</c> — Angabe einer Periode; „{0}“ ist der Wochentag, z. B. Sonntag
    /// bei den Feiertagen
    /// </summary>
    public string WertWieWochentag { get; set; } = "wie {0}";

    /// <summary><c>KOND_LBL_WERKZEUG_FEIERTAGE</c> — Überschrift des Werkzeugs</summary>
    public string LabelWerkzeugFeiertage { get; set; } = "Feiertage";

    /// <summary><c>KOND_LBL_WERKZEUG_ZEITSTRUKTUR</c> — Überschrift des Werkzeugs</summary>
    public string LabelWerkzeugZeitstruktur { get; set; } = "Zeitstruktur";

    /// <summary><c>KOND_LBL_WIE_HEIZUNG</c> — Auswahl der Zeitstruktur</summary>
    public string LabelWieHeizung { get; set; } = "wie Heizung";

    /// <summary><c>KOND_LBL_WIE_ANWESENHEIT</c> — Auswahl der Zeitstruktur</summary>
    public string LabelWieAnwesenheit { get; set; } = "wie Anwesenheit";

    /// <summary><c>KOND_LBL_VORSCHAU_WOCHE</c> — Überschrift des Wochenbilds</summary>
    public string LabelVorschauWoche { get; set; } = "Vorschau: Woche";

    /// <summary><c>KOND_LBL_TEPPICHBILD</c> — Überschrift des Jahresbilds</summary>
    public string LabelTeppichbild { get; set; } = "Teppichbild";

    /// <summary><c>KOND_TXT_TEPPICHBILD</c> — Bildunterschrift; „{0}“ ist das Bezugsjahr</summary>
    public string TextTeppichbild { get; set; }
        = "Bezugsjahr {0}: Tage × Stunden, Farbe = Wert, „aus“ als eigene Fläche. Berühren einer Stunde "
        + "nennt ihre Quelle.";

    /// <summary>
    /// <c>KOND_TXT_QUELLE</c> — die Quelle einer Stunde; „{0}“ ist die Periode, die Standardwoche oder
    /// die Grundangabe
    /// </summary>
    public string TextQuelle { get; set; } = "Quelle: {0}";

    /// <summary><c>KOND_TXT_HINWEIS_PERIODEN</c> — unter der Periodenliste</summary>
    public string HinweisPerioden { get; set; }
        = "Der Matrixbereich ist nur lesbar; neue Perioden sind Zeiträume oder Feiertage. Bei "
        + "Überschneidung gilt die Periode mit dem höheren Rang.";

    /// <summary><c>KOND_TXT_HINWEIS_FEIERTAGE</c> — Werkzeug Feiertage</summary>
    public string HinweisFeiertage { get; set; }
        = "Legt die neun bundeseinheitlichen Feiertage als Regel an, jeweils wie Sonntag. Länderfeiertage "
        + "legen Sie als eigene Perioden an.";

    /// <summary><c>KOND_TXT_HINWEIS_ZEITSTRUKTUR</c> — Werkzeug Zeitstruktur</summary>
    public string HinweisZeitstruktur { get; set; }
        = "„wie Heizung“ nimmt die Stunden, in denen mit dem Tagwert oder höher geheizt wird, „wie "
        + "Anwesenheit“ die Stunden mit Anwesenheit. Diese Stunden bekommen den Tagwert der Größe, alle "
        + "übrigen ihren Nachtwert; ersetzt wird nur die Standardwoche.";

    // ------------------------------------------------------------ Vorlagen

    /// <summary><c>KOND_LBL_VORLAGE_AUSWAHL</c> — die Auswahlliste im Kopf der Kalenderkarte</summary>
    public string LabelVorlageAuswahl { get; set; } = "Vorlage";

    /// <summary><c>KOND_TXT_VORLAGE_KEINE</c> — leerer Eintrag der Auswahlliste</summary>
    public string TextVorlageKeine { get; set; } = "keine Vorlage gewählt";

    /// <summary>
    /// <c>KOND_LBL_VORLAGE_NAME</c> — „Als Vorlage speichern…“ und Verwaltung; der Name ist ein Datenwert
    /// </summary>
    public string LabelVorlageName { get; set; } = "Name";

    /// <summary><c>KOND_LBL_VORLAGE_BESCHREIBUNG</c> — Vorlage</summary>
    public string LabelVorlageBeschreibung { get; set; } = "Beschreibung";

    /// <summary><c>KOND_LBL_VORLAGE_NUTZUNG</c> — Vorlage</summary>
    public string LabelVorlageNutzung { get; set; } = "Nutzung";

    /// <summary>
    /// <c>KOND_LBL_NUTZUNG_WOHNEN</c> — Anzeigewert der Nutzung <c>WOHNEN</c> (der Persistenzwert bleibt
    /// deutsch)
    /// </summary>
    public string NutzungWohnen { get; set; } = "Wohnen";

    /// <summary><c>KOND_LBL_NUTZUNG_BUERO</c> — Anzeigewert der Nutzung <c>BUERO</c></summary>
    public string NutzungBuero { get; set; } = "Büro";

    /// <summary><c>KOND_LBL_NUTZUNG_SCHULE</c> — Anzeigewert der Nutzung <c>SCHULE</c></summary>
    public string NutzungSchule { get; set; } = "Schule";

    /// <summary><c>KOND_LBL_NUTZUNG_SONSTIGE</c> — Anzeigewert der Nutzung <c>SONSTIGE</c></summary>
    public string NutzungSonstige { get; set; } = "Sonstige";

    /// <summary><c>KOND_LBL_NUTZUNG_KEINE</c> — die Nutzung ist leer</summary>
    public string NutzungKeine { get; set; } = "ohne Angabe";

    /// <summary>
    /// <c>KOND_LBL_VORLAGE_AUSGELIEFERT</c> — Kennzeichnung einer Vorlage der Auslieferung (Schloss)
    /// </summary>
    public string LabelVorlageAusgeliefert { get; set; } = "ausgeliefert";

    /// <summary><c>KOND_LBL_VORLAGE_EIGEN</c> — Kennzeichnung einer eigenen Vorlage</summary>
    public string LabelVorlageEigen { get; set; } = "eigen";

    /// <summary><c>KOND_LBL_VERWALTUNG</c> — Titel der Vorlagenverwaltung</summary>
    public string LabelVerwaltung { get; set; } = "Vorlagen der Konditionierung";

    /// <summary><c>KOND_LBL_GROESSE</c> — Umschalter der Größe in der Vorlagenverwaltung</summary>
    public string LabelGroesse { get; set; } = "Größe";

    /// <summary>
    /// <c>KOND_TXT_VORLAGE_SOFORT</c> — unter „Als Vorlage speichern…“ und in der Verwaltung
    /// </summary>
    public string HinweisVorlageSofort { get; set; }
        = "Die Vorlage wird sofort gespeichert, nicht erst mit dem OK des Editors.";

    /// <summary>
    /// <c>KOND_TXT_VORLAGE_GESPERRT</c> — der Grund des Schlosses einer ausgelieferten Vorlage
    /// </summary>
    public string GrundVorlageGesperrt { get; set; }
        = "Ausgelieferte Vorlage — nur lesbar; Duplizieren legt eine bearbeitbare Kopie an.";

    /// <summary>
    /// <c>KOND_TXT_VORLAGE_LOESCHEN</c> — die Rückfrage vor dem Löschen; „{0}“ ist der Name der Vorlage
    /// </summary>
    public string HinweisVorlageLoeschen { get; set; }
        = "Beim Löschen der Vorlage „{0}“ wird kein Gebäude, keine Zone und kein Katalogsatz berührt; "
        + "schon übernommene Werte bleiben.";

    /// <summary><c>KOND_TXT_VORLAGE_UEBERNEHMEN</c> — unter der Auswahlliste</summary>
    public string HinweisVorlageUebernehmen { get; set; }
        = "Übernehmen wirkt nur auf diese Größe. Nennwert und Saison des Ziels bleiben; leere Zellen der "
        + "Vorlage lassen die Zellen des Ziels unverändert.";

    /// <summary><c>KOND_TXT_VORLAGE_SPEICHERN</c> — unter „Als Vorlage speichern…“</summary>
    public string HinweisVorlageSpeichern { get; set; }
        = "Die Vorlage nimmt die Matrixspalte und, falls angelegt, den Kalender dieser Größe mit — ohne "
        + "Nennwert und Saison.";

    // ------------------------------------------------------------ Hinweise und Gründe

    /// <summary><c>KOND_TXT_HINWEIS_VDI6007</c> — unter der Matrix und in der Kalenderkarte</summary>
    public string HinweisVdi6007 { get; set; }
        = "Unter VDI 6007 ist die Matrix Vorgabe; angelegte Kalender gehen vor.";

    /// <summary><c>KOND_TXT_HINWEIS_ALTFELDER</c> — die Zeile im Reiter „Gebäude und Hülle“</summary>
    public string HinweisAltfelder { get; set; }
        = "Sollwerte, Wärmegewinne, Infiltration, Nutzerlüftung, Sommerlüftung und Maximalraumtemperatur "
        + "stehen im Reiter „Konditionierung“.";

    /// <summary><c>KOND_TXT_KUEHLEN_GESPERRT</c> — der Grund der weich gesperrten Kühlspalte</summary>
    public string GrundKuehlenGesperrt { get; set; }
        = "Die Kühlspalte wirkt nur mit Kühlbetrieb im Projekt („Kühlung rechnen“ in der "
        + "Simulationskonfiguration) und bei „Gebäude wird gekühlt“.";

    /// <summary><c>KOND_TXT_KUEHLEN_ZONE</c> — der Grund der gesperrten Kühlspalte einer Zone</summary>
    public string HinweisKuehlenZone { get; set; }
        = "Die Kühlwerte gelten für das ganze Gebäude; ein eigener Kühlkalender je Zone ist noch nicht "
        + "möglich.";

    /// <summary><c>KOND_TXT_HINWEIS_TAGESBILANZ</c> — für Gebäude auf dem Tagesbilanz-Weg</summary>
    public string HinweisTagesbilanz { get; set; }
        = "Dieses Gebäude rechnet auf dem Rechenweg „Tagesbilanz (Bestandsweg)“: Die Matrix zeigt nur die "
        + "Felder, die dieser Weg liest; Kalender und weitere Zellen wirken erst unter VDI 6007.";

    /// <summary>
    /// <c>KOND_TXT_HINWEIS_LESEMODUS</c> — der Grund des Lesemodus eines gesperrten Katalogsatzes
    /// </summary>
    public string HinweisLesemodus { get; set; }
        = "Dieser Katalogsatz gehört zur Auslieferung und ist nur lesbar. „Speichern unter“ legt eine "
        + "bearbeitbare Kopie an.";

    /// <summary><c>KOND_TXT_GEMEINJAHR</c> — Meldung des Felds für ein Datum im Gemeinjahr</summary>
    public string MeldungGemeinjahr { get; set; }
        = "Ungültiges Datum: TT.MM. im Gemeinjahr; den 29.02. gibt es nicht.";

    /// <summary><c>KOND_TXT_HINWEIS_AUS</c> — Bedienhinweis zum Zellzustand „aus“ im Wochenraster</summary>
    public string HinweisAus { get; set; }
        = "„aus“: Die Größe ist in dieser Stunde abgeschaltet (Heizen und Kühlen ohne Betrieb, "
        + "Nutzerlüftung 0 1/h, Anteile 0 %). Ein neuer Wert in der Zelle hebt „aus“ wieder auf.";

    /// <summary><c>KOND_TXT_HINWEIS_NACHTAUSKUEHLUNG</c> — an der Nachtzeile der Lüftungsspalte</summary>
    public string HinweisNachtauskuehlung { get; set; }
        = "Wirkt nur bedingt: Die Raumluft liegt über der Schwelle und die Außenluft ist mindestens ΔT "
        + "kühler (Vorgabe 2 K). Einen Schalter gibt es nicht.";

    /// <summary><c>KOND_TXT_HINWEIS_NACHTFENSTER</c> — Platzhalter eines leeren Nachtfensters</summary>
    public string HinweisNachtfensterLeer { get; set; } = "leer = Nachtfenster der Heizspalte";

    /// <summary><c>KOND_TXT_HINWEIS_SAISON</c> — an der Zeile Saison</summary>
    public string HinweisSaison { get; set; }
        = "Innerhalb der Saison wird geheizt bzw. gekühlt, außerhalb steht die Größe auf „aus“; leer "
        + "heißt ganzjährig. Start und Ende gibt es nur zusammen.";

    /// <summary>
    /// <c>KOND_TXT_GRUND_VOM_GEBAEUDE</c> — der Grund der gesperrten Anzeige „vom Gebäude“ in der Zone
    /// </summary>
    public string GrundVomGebaeude { get; set; }
        = "Die Zone folgt dem Kalender des Gebäudes. „Vom Gebäude übernehmen und anpassen“ legt eine "
        + "eigene Kopie an.";

    /// <summary><c>KOND_TXT_HINWEIS_UNBEHEIZT</c> — an der Matrix einer unbeheizten Zone</summary>
    public string HinweisUnbeheizt { get; set; }
        = "Eine unbeheizte Zone hat weder Heiz- noch Kühlwerte; Lüftung, Geräte und Personen gelten.";

    /// <summary><c>KOND_TXT_GRUND_OHNE_TABELLEN</c> — der Grund des gesperrten Reiters</summary>
    public string GrundOhneTabellen { get; set; }
        = "Die Konditionierung steht nicht zur Verfügung: Diese Datenbank trägt die Tabellen der "
        + "Konditionierung nicht.";

    // ------------------------------------------------------------ Katalogauswahl

    /// <summary>
    /// <c>KOND_LBL_KATALOG_KALENDER</c> — Spalte der Katalogauswahl: angelegte Kalender je Satz
    /// </summary>
    public string SpalteKatalogKalender { get; set; } = "Kalender";

    /// <summary>
    /// <c>KOND_TXT_KATALOG_KALENDER</c> — Zelle der Spalte; „{0}“ angelegte Kalender, „{1}“ Zahl der
    /// Größen (5)
    /// </summary>
    public string TextKatalogKalender { get; set; } = "{0} von {1}";

    // ------------------------------------------------------------ Reiter und Rückfragen (Welle U1)

    /// <summary>
    /// <c>KOND_TXT_HINWEIS_ZURUECKNEHMEN</c> — der Kurztext von „Zurücknehmen“: eine Stufe, geschrieben
    /// wird erst mit OK (Entwurf KP2, Festlegung 1)
    /// </summary>
    public string HinweisZuruecknehmen { get; set; }
        = "„Zurücknehmen“ nimmt den letzten Schritt dieses Reiters zurück; geschrieben wird erst mit OK.";

    /// <summary><c>KOND_TXT_GRUND_NICHTS_ZURUECK</c> — der Grund des weich gesperrten „Zurücknehmen“</summary>
    public string GrundNichtsZurueck { get; set; } = "Es gibt keinen Schritt, den „Zurücknehmen“ zurücknehmen könnte.";

    /// <summary><c>KOND_TXT_NICHTS</c> — ein leerer Teil einer Rückfrage („es bleibt: nichts“)</summary>
    public string TextNichts { get; set; } = "nichts";

    /// <summary><c>KOND_TXT_POSTEN_MATRIXZELLEN</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenMatrixzellen { get; set; } = "{0} Zellen der Matrix";

    /// <summary><c>KOND_TXT_POSTEN_KALENDER</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenKalender { get; set; } = "{0} Kalender";

    /// <summary><c>KOND_TXT_POSTEN_STANDARDWOCHE</c> — Posten einer Rückfrage</summary>
    public string TextPostenStandardwoche { get; set; } = "die Standardwoche";

    /// <summary><c>KOND_TXT_POSTEN_FERIENPERIODEN</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenFerienperioden { get; set; } = "{0} Ferienperioden";

    /// <summary><c>KOND_TXT_POSTEN_SAISON</c> — Posten einer Rückfrage</summary>
    public string TextPostenSaison { get; set; } = "die Saison";

    /// <summary><c>KOND_TXT_POSTEN_EIGENE_PERIODEN</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenEigenePerioden { get; set; } = "{0} eigene Perioden";

    /// <summary><c>KOND_TXT_POSTEN_FEIERTAGE</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenFeiertage { get; set; } = "{0} Feiertagsregeln";

    /// <summary><c>KOND_TXT_POSTEN_NACHTZEITEN</c> — Posten einer Rückfrage</summary>
    public string TextPostenNachtzeiten { get; set; } = "das Nachtfenster";

    /// <summary><c>KOND_TXT_POSTEN_FERIENZEITRAEUME</c> — Posten einer Rückfrage</summary>
    public string TextPostenFerienzeitraeume { get; set; } = "die Ferienzeiträume";

    /// <summary><c>KOND_TXT_POSTEN_LUFTWECHSEL</c> — Posten einer Rückfrage</summary>
    public string TextPostenLuftwechsel { get; set; } = "die Gesamtangabe des Luftwechsels";

    /// <summary><c>KOND_TXT_POSTEN_ZONENKALENDER</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenZonenkalender { get; set; } = "{0} Kalender und Zellen von Zonen";

    /// <summary><c>KOND_TXT_POSTEN_BAUTEILE</c> — Posten einer Rückfrage; „{0}“ die Zahl</summary>
    public string TextPostenBauteile { get; set; } = "{0} Bauteile";

    // ------------------------------------------------------------ Zonenmatrix (Welle U4)

    /// <summary><c>KOND_TXT_PLATZHALTER_WIE_GEBAEUDE</c> — Platzhalter einer leeren Zonenzelle ohne Wert des Gebäudes</summary>
    public string PlatzhalterWieGebaeude { get; set; } = "wie Gebäude";

    /// <summary>
    /// <c>KOND_TXT_ZONE_AUFTEILEN</c> — an einer Zone, deren Gebäude die Lüftung als Gesamtangabe führt:
    /// aufgeteilt wird am Gebäude
    /// </summary>
    public string HinweisZoneAufteilen { get; set; }
        = "Die Lüftung des Gebäudes steht als Gesamtangabe „Luftwechselrate“. Teilen Sie sie im Reiter „Konditionierung“ des "
          + "Gebäudes auf; danach trägt die Zone eigene Lüftungswerte.";

    /// <summary><c>KOND_TXT_ZONE_OHNE_WIRKUNG</c> — Kurztext der Zellen einer Größe, die dem Gebäudekalender folgt</summary>
    public string HinweisZoneOhneWirkung { get; set; } = "Ohne Wirkung, solange die Zone dem Kalender des Gebäudes folgt.";

    /// <summary><c>KOND_LBL_ZONE_KALENDER</c> — Überschrift der Zustandszeilen je Größe in der Zone</summary>
    public string LabelZoneKalender { get; set; } = "Kalender der Zone";

    /// <summary><c>KOND_TXT_ZONE_MATRIX</c> — die leise Zeile unter der Zonenmatrix: leer erbt</summary>
    public string HinweisZoneMatrix { get; set; }
        = "Eine leere Zelle gilt wie im Gebäude; der Platzhalter nennt den Wert. Eine eigene Zelle überschreibt ihn, eine "
          + "geleerte erbt wieder.";

    // ------------------------------------------------------------ Gebäudeverwaltung (Welle U4)

    /// <summary><c>KOND_BTN_KONDITIONIERUNG</c> — Kopf der Stammblattgruppe: öffnet das Blatt „Konditionierung"</summary>
    public string KnopfKonditionierung { get; set; } = "Konditionierung…";

    /// <summary>
    /// <c>KOND_TXT_VERWALTUNG_SPEICHERN</c> — die leise Zeile im Blatt der Verwaltung: geschrieben wird mit
    /// „Speichern" der Fußleiste
    /// </summary>
    public string HinweisVerwaltungSpeichern { get; set; }
        = "Die Änderungen gehören zum gewählten Satz: „Speichern“ der Verwaltung schreibt sie, „Verwerfen“ nimmt sie zurück.";

    /// <summary>
    /// <c>KOND_TXT_VERWALTUNG_ALTFELDER</c> — die Herleitungszeile in „Alle Daten": wohin die Felder der
    /// Konditionierung gewandert sind (E56 F3 (a))
    /// </summary>
    public string HinweisVerwaltungAltfelder { get; set; }
        = "Sollwerte, Nachtzeit, Ferienzeiträume, innere Wärmegewinne, Infiltration, Nutzerlüftung, Sommerlüftung, "
          + "Kühlsollwert und Maximalraumtemperatur stehen in der Gruppe „Konditionierung“.";

    // ------------------------------------------------------------ Vorlagen je Karte (Welle U2)

    /// <summary>
    /// <c>KOND_TXT_VORSCHAU_VORLAGE</c> — über der Vorschau, solange eine Vorlage gewählt, aber nicht
    /// übernommen ist; „{0}“ ist der Name der Vorlage (ein Datenwert)
    /// </summary>
    public string TextVorschauVorlage { get; set; } = "Vorschau mit der Vorlage „{0}“ – übernommen wird erst mit „Übernehmen“.";

    /// <summary><c>KOND_TXT_GRUND_KEINE_VORLAGE</c> — der Grund des weich gesperrten „Übernehmen“</summary>
    public string GrundKeineVorlage { get; set; } = "Erst eine Vorlage aus der Liste wählen.";

    /// <summary>
    /// <c>KOND_TXT_VORLAGE_GESPEICHERT</c> — die leise Zeile nach „Als Vorlage speichern…“; „{0}“ der Name
    /// </summary>
    public string TextVorlageGespeichert { get; set; } = "Vorlage „{0}“ gespeichert – sie steht jetzt in der Liste dieser Größe.";

    /// <summary><c>KOND_TXT_VORLAGE_UMBENANNT</c> — die Zeile der Verwaltung; „{0}“ der neue Name</summary>
    public string TextVorlageUmbenannt { get; set; } = "Vorlage umbenannt in „{0}“.";

    /// <summary><c>KOND_TXT_VORLAGE_GELOESCHT</c> — die Zeile der Verwaltung; „{0}“ der Name</summary>
    public string TextVorlageGeloescht { get; set; } = "Vorlage „{0}“ gelöscht.";

    /// <summary>
    /// <c>KOND_TXT_VORLAGE_DUPLIZIERT</c> — die Zeile der Verwaltung; „{0}“ der Name der Kopie, „{1}“ der
    /// Name der Quelle
    /// </summary>
    public string TextVorlageDupliziert { get; set; } = "Vorlage „{0}“ als Kopie von „{1}“ angelegt.";

    /// <summary><c>KOND_TXT_VORLAGEN_LEER</c> — die leere Liste einer Größe</summary>
    public string TextVorlagenLeer { get; set; } = "Diese Größe hat noch keine Vorlage.";

    /// <summary><c>KOND_LBL_SPALTE_AKTIONEN</c> — die Aktionsspalte der Vorlagenverwaltung</summary>
    public string SpalteAktionen { get; set; } = "Aktionen";

    // ------------------------------------------------------------ Die Karte im Einzelnen (Welle U3)

    /// <summary><c>KOND_BTN_EINZELHEITEN</c> — klappt die Karte auf: Grundangabe, Woche, Perioden, Werkzeuge, Teppichbild</summary>
    public string KnopfEinzelheiten { get; set; } = "Kalender im Einzelnen";

    /// <summary><c>KOND_TXT_GRUND_NICHT_ANGELEGT</c> — der Grund der weich gesperrten Handlungen der aufgeklappten Karte</summary>
    public string GrundNichtAngelegt { get; set; } = "Erst „Kalender anlegen“ – bis dahin folgt der Kalender der Matrix.";

    /// <summary><c>KOND_TXT_HINWEIS_GRUNDANGABE</c> — unter der Grundangabe</summary>
    public string HinweisGrundangabe { get; set; }
        = "Die Grundangabe gilt in jeder Stunde ohne Standardwoche und ohne Periode; eine Standardwoche tritt an ihre Stelle.";

    /// <summary><c>KOND_TXT_GRUNDANGABE_WOCHE</c> — an Stelle der Grundangabe, solange eine Standardwoche gilt</summary>
    public string TextGrundangabeWoche { get; set; }
        = "Die Standardwoche tritt an die Stelle der Grundangabe; „Standardwoche verwerfen“ kehrt zu ihr zurück.";

    /// <summary><c>KOND_BTN_WOCHE_ANLEGEN</c> — macht aus der Grundangabe eine Standardwoche</summary>
    public string KnopfWocheAnlegen { get; set; } = "Standardwoche anlegen";

    /// <summary><c>KOND_BTN_WOCHE_VERWERFEN</c> — zurück zur Grundangabe (häufigster Wert der Woche)</summary>
    public string KnopfWocheVerwerfen { get; set; } = "Standardwoche verwerfen";

    /// <summary><c>KOND_TXT_WOCHE_VORGABE</c> — über dem Wochenraster ohne Standardwoche</summary>
    public string TextWocheVorgabe { get; set; } = "Noch keine Standardwoche – das Raster zeigt die Grundangabe.";

    /// <summary><c>KOND_TXT_VERMERK</c> — der letzte Werkzeugvermerk in der aufgeklappten Karte; „{0}“ der Vermerk</summary>
    public string TextVermerk { get; set; } = "Zuletzt angewandt: {0}";

    /// <summary><c>KOND_TXT_HINWEIS_ZEITFENSTER</c> — unter dem Werkzeug Zeitfenster</summary>
    public string HinweisZeitfenster { get; set; }
        = "Setzt den Wert in die gewählten Stunden der Standardwoche; alle übrigen Stunden bleiben. „Bis“ vor „Von“ geht über Mitternacht.";

    /// <summary><c>KOND_TXT_GRUND_ZEITFENSTER_TAGE</c> — der Grund des weich gesperrten „Zeitfenster eintragen“</summary>
    public string GrundZeitfensterTage { get; set; } = "Erst mindestens einen Tag wählen.";

    /// <summary><c>KOND_TXT_GRUND_ZEITFENSTER_ZEITEN</c> — der Grund des weich gesperrten „Zeitfenster eintragen“</summary>
    public string GrundZeitfensterZeiten { get; set; } = "Erst „Von“ und „Bis“ angeben (volle Stunden, „Bis“ 1 … 24).";

    /// <summary><c>KOND_TXT_GRUND_ZEITFENSTER_WERT</c> — der Grund des weich gesperrten „Zeitfenster eintragen“</summary>
    public string GrundZeitfensterWert { get; set; } = "Erst einen Wert oder „aus“ angeben.";

    /// <summary><c>KOND_LBL_ANGABE</c> — die Angabe einer Periode im Formular der Periodenliste</summary>
    public string LabelAngabe { get; set; } = "Angabe";

    /// <summary><c>KOND_LBL_ANGABE_WOCHENTAG</c> — die Angabe „wie Wochentag“ einer Periode</summary>
    public string LabelAngabeWochentag { get; set; } = "wie Wochentag";

    /// <summary><c>KOND_LBL_WOCHENTAG</c> — der Wochentag der Angabe „wie Wochentag“</summary>
    public string LabelWochentag { get; set; } = "Wochentag";

    /// <summary><c>KOND_TXT_AUS_MATRIX</c> — die Aktionsspalte einer Periode des Matrixbereichs</summary>
    public string TextAusMatrix { get; set; } = "aus der Matrix – nur lesbar";

    /// <summary><c>KOND_TXT_PERIODEN_LEER</c> — die leere Periodenliste</summary>
    public string TextPeriodenLeer { get; set; } = "Keine Perioden – es gelten Standardwoche bzw. Grundangabe.";

    /// <summary><c>KOND_TXT_GRUND_PERIODE</c> — der Grund des weich gesperrten „Übernehmen“ im Formular</summary>
    public string GrundPeriodeUnvollstaendig { get; set; } = "Erst Name, Tage bzw. Feiertag und die Angabe angeben.";

    /// <summary><c>KOND_TXT_GRUND_RANG_OBEN</c> — der Grund des weich gesperrten ▲</summary>
    public string GrundRangOben { get; set; } = "Die Periode hat schon den höchsten Rang der eigenen Perioden.";

    /// <summary><c>KOND_TXT_GRUND_RANG_UNTEN</c> — der Grund des weich gesperrten ▼</summary>
    public string GrundRangUnten { get; set; } = "Die Periode hat schon den niedrigsten Rang der eigenen Perioden.";

    /// <summary><c>KOND_TXT_GRUND_RANG_BAND</c> — der Grund der weich gesperrten ▲▼ außerhalb des Eigenbands</summary>
    public string GrundRangBand { get; set; }
        = "Nur Perioden im Band der eigenen Perioden wechseln ihren Rang; Feiertagsregeln stehen unter den Ferien.";

    /// <summary><c>KOND_TXT_TEPPICH_LEER</c> — an Stelle des Teppichbilds, wenn die Größe keinen Kalender ergibt</summary>
    public string TextTeppichLeer { get; set; }
        = "Kein Jahresbild – die Matrix ergibt für diese Größe keinen Kalender (etwa ohne Anteile).";
}
