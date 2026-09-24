namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte der Überlagerung „Auslegung" und ihres Konstruktors (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.3; Stufe Z2, Gruppe 2) — EIN Parameter statt hundert.
/// </summary>
/// <remarks>
/// <para><b>Warum gebündelt.</b> Hausregel „ab etwa zehn Anzeigetexten ein Bündel"
/// (<c>EPOS.UI/CLAUDE.md</c>). Beschriftungen, kein Zustand: Eingaben und Ergebnis stehen in
/// <see cref="ZapfprofilAuslegungStartDaten"/> bzw. <see cref="ZapfprofilAuslegungDaten"/>;
/// Meldungen und die Titel der Warnliste bringen ihren Satz selbst mit.</para>
/// <para>Jede Eigenschaft trägt ihren Ressourcenschlüssel im Kommentar; der Vorgabewert ist der
/// deutsche Rückfall und gleicht dem Wert der neutralen Ressource (Wache
/// <c>ZapfprofilAuslegungDatenTests</c>). Platzhalter <c>{0}</c> … füllt der Dialog. Begriffe nach
/// Glossar § 14 „Trinkwarmwasser und Zapfprofil".</para>
/// </remarks>
public sealed class ZapfprofilAuslegungTexte
{
    // ------------------------------------------------------------ Kopf und Kontext

    /// <summary><c>ZPG_AUS_TITEL</c></summary>
    public string Titel { get; set; } = "Auslegung Brauchwasser";

    /// <summary><c>ZPG_AUS_INFO_BEDIENUNG</c></summary>
    public string InfoBedienung { get; set; } = "Bedienung";

    /// <summary><c>ZPG_AUS_INFO_RECHENWEG</c></summary>
    public string InfoRechenweg { get; set; } = "Rechenweg";

    /// <summary><c>ZPG_AUS_GRUND_NOCH_NICHT</c></summary>
    public string GrundNochNicht { get; set; } = "In dieser Fassung noch nicht verfügbar.";

    // ------------------------------------------------------------ Eingaben der Summenlinie

    /// <summary><c>ZPG_AUS_GRP_EINGABEN</c></summary>
    public string GruppeEingaben { get; set; } = "Eingaben der Summenlinie";

    /// <summary><c>ZPG_AUS_LBL_BEDARFSTAG</c></summary>
    public string LabelBedarfstag { get; set; } = "Bedarfstag";

    /// <summary><c>ZPG_AUS_OPT_VORGABEREGEL</c></summary>
    public string OptionVorgaberegel { get; set; } = "Vorgaberegel (Wohnen: DIN-4708-Profil, sonst Konstruktor)";

    /// <summary><c>ZPG_AUS_OPT_STUNDENPROFIL</c></summary>
    public string OptionStundenprofil { get; set; } = "Stundenprofil der Zonen (Spitzen unterschätzt)";

    /// <summary><c>ZPG_AUS_OPT_DIN4708</c></summary>
    public string OptionDin4708 { get; set; } = "DIN-4708-Profil aus der Wohnungstabelle";

    /// <summary><c>ZPG_AUS_OPT_A100</c></summary>
    public string OptionA100 { get; set; } = "A100-Referenzprofil aus dem Katalog";

    /// <summary><c>ZPG_AUS_GRUND_A100</c></summary>
    public string GrundA100 { get; set; } = "Die Katalogzeilen der Art A100-Referenzprofil folgen mit dem Katalogpaket.";

    /// <summary><c>ZPG_AUS_OPT_ECODESIGN</c></summary>
    public string OptionEcodesign { get; set; } = "Ecodesign-Zapfprofil (nur Einfamilienhaus, zur Plausibilisierung)";

    /// <summary><c>ZPG_AUS_GRUND_ECODESIGN</c></summary>
    public string GrundEcodesign { get; set; } = "Der Katalog führt das Ecodesign-Zapfprofil nicht; es kommt mit der Auslieferungsvorlage oder dem Katalogimport.";

    /// <summary><c>ZPG_AUS_OPT_KATALOGTAG</c></summary>
    public string OptionKatalogtag { get; set; } = "{0} · {1}";

    /// <summary><c>ZPG_AUS_OPT_ENTWURF</c></summary>
    public string OptionEntwurf { get; set; } = "{0} (konstruiert, wird mit dem Projekt gespeichert)";

    /// <summary><c>ZPG_AUS_BTN_KONSTRUIEREN</c></summary>
    public string KnopfKonstruieren { get; set; } = "Bedarfstag konstruieren…";

    /// <summary><c>ZPG_AUS_HINW_KONSTRUKTOR</c></summary>
    public string HinweisKonstruktor { get; set; } = "Konstruktor nach A100 NA.5.2.3; der Tag gilt beim Kaltwasser der Auslegung des Katalogs.";

    /// <summary><c>ZPG_AUS_BANNER_SPITZEN</c></summary>
    public string BannerSpitzen { get; set; } = "Spitzen unterschätzt: Der Bedarfstag stammt aus einem Stundenprofil — Zapfspitzen unter einer Stunde sind darin nicht enthalten.";

    /// <summary><c>ZPG_AUS_LBL_SPEICHERTEMPERATUR</c></summary>
    public string LabelSpeichertemperatur { get; set; } = "Speichertemperatur";

    /// <summary><c>ZPG_AUS_LBL_ERZEUGERLEISTUNG</c></summary>
    public string LabelErzeugerleistung { get; set; } = "Erzeugerleistung";

    /// <summary><c>ZPG_AUS_LBL_UEBERTRAGERLEISTUNG</c></summary>
    public string LabelUebertragerleistung { get; set; } = "Wärmeübertragerleistung";

    /// <summary><c>ZPG_AUS_LBL_SPEICHERART</c></summary>
    public string LabelSpeicherart { get; set; } = "Speicherart";

    /// <summary><c>ZPG_AUS_SPEICHERART_LADESPEICHER</c></summary>
    public string SpeicherartLadespeicher { get; set; } = "Ladespeicher";

    /// <summary><c>ZPG_AUS_SPEICHERART_GEMISCHT</c></summary>
    public string SpeicherartGemischt { get; set; } = "gemischter Speicher";

    /// <summary><c>ZPG_AUS_LBL_SENSORHOEHE</c></summary>
    public string LabelSensorhoehe { get; set; } = "Sensorhöhe h_sensor/h_sto";

    /// <summary><c>ZPG_AUS_LBL_ERZEUGERART</c></summary>
    public string LabelErzeugerart { get; set; } = "Erzeugerart am Speicher";

    /// <summary><c>ZPG_AUS_LBL_WERKSTOFF</c></summary>
    public string LabelWerkstoff { get; set; } = "Werkstoff des Übertragers";

    /// <summary><c>ZPG_AUS_KEINE_ANGABE</c></summary>
    public string KeineAngabe { get; set; } = "keine Angabe";

    /// <summary><c>ZPG_AUS_ERZEUGER_KESSEL</c></summary>
    public string ErzeugerKessel { get; set; } = "Kessel";

    /// <summary><c>ZPG_AUS_ERZEUGER_WAERMEPUMPE</c></summary>
    public string ErzeugerWaermepumpe { get; set; } = "Wärmepumpe";

    /// <summary><c>ZPG_AUS_WERKSTOFF_STAHL</c></summary>
    public string WerkstoffStahl { get; set; } = "Stahl";

    /// <summary><c>ZPG_AUS_WERKSTOFF_EDELSTAHL</c></summary>
    public string WerkstoffEdelstahl { get; set; } = "Edelstahl";

    /// <summary><c>ZPG_AUS_HERL_SPEICHERTEMPERATUR</c></summary>
    public string HerleitungSpeichertemperatur { get; set; } = "angesetzt {0} °C · {1}";

    /// <summary><c>ZPG_AUS_HERL_ERZEUGER</c></summary>
    public string HerleitungErzeuger { get; set; } = "leer = angesetzte Ladeleistung der Speicherauslegung; Nachweisleistung Φ_N = min(Erzeuger, Wärmeübertrager)";

    /// <summary><c>ZPG_AUS_HERL_UEBERTRAGER</c></summary>
    public string HerleitungUebertrager { get; set; } = "leer = aus U·A, Fläche oder Schätzformel der Erzeugerart";

    /// <summary><c>ZPG_AUS_HERL_SENSORHOEHE</c></summary>
    public string HerleitungSensorhoehe { get; set; } = "leer = Vorgabe des Katalogs";

    // ------------------------------------------------------------ Stochastik (Z3)

    /// <summary><c>ZPG_AUS_LBL_STOCHASTISCH</c></summary>
    public string LabelStochastisch { get; set; } = "Stochastisch rechnen";

    /// <summary><c>ZPG_AUS_HERL_STOCHASTISCH</c></summary>
    public string HerleitungStochastisch { get; set; } = "Zieht je Topologiegruppe das Ensemble des Bedarfstags für Perzentil, Streuband und Gleichzeitigkeit — eine Laufangabe, sie wird nicht gespeichert.";

    /// <summary><c>ZPG_AUS_LBL_PERZENTIL</c></summary>
    public string LabelPerzentil { get; set; } = "Auslegungsperzentil";

    /// <summary><c>ZPG_AUS_HERL_PERZENTIL</c></summary>
    public string HerleitungPerzentil { get; set; } = "P95 oder P99 · Vorgabe P{0}";

    /// <summary><c>ZPG_AUS_LBL_REALISIERUNGEN</c></summary>
    public string LabelRealisierungen { get; set; } = "Realisierungen des Bedarfstags";

    /// <summary><c>ZPG_AUS_EINHEIT_TAGE</c></summary>
    public string EinheitTage { get; set; } = "Tage";

    /// <summary><c>ZPG_AUS_HERL_REALISIERUNGEN</c></summary>
    public string HerleitungRealisierungen { get; set; } = "Ganze Zahl von {0} bis {1} · leer = Vorgabe {2} (Vielfaches der Mindestzahl); unter {3} ist P{4} nicht belastbar.";

    /// <summary><c>ZPG_AUS_HERL_REALISIERUNGEN_OHNE</c></summary>
    public string HerleitungRealisierungenOhne { get; set; } = "Ganze Zahl von {0} bis {1} · leer = Vorgabe, hier nicht bestimmbar: {2}";

    /// <summary><c>ZPG_AUS_MSG_FEHLEINGABE</c></summary>
    public string MeldungFehleingabe { get; set; } = "Bitte die markierten Felder berichtigen: {0}.";

    // ------------------------------------------------------------ Gruppen und Karten

    /// <summary><c>ZPG_AUS_GRP_TOPOLOGIE</c></summary>
    public string GruppeTopologie { get; set; } = "{0} · Zonen: {1}";

    /// <summary><c>ZPG_AUS_BEDARFSTAG_GRUPPE</c></summary>
    public string BedarfstagGruppe { get; set; } = "Bedarfstag: {0}";

    /// <summary><c>ZPG_AUS_KARTE_SUMMENLINIE</c></summary>
    public string KarteSummenlinie { get; set; } = "(a) Summenlinie";

    /// <summary><c>ZPG_AUS_KARTE_SUMMENLINIE_UNTER</c></summary>
    public string KarteSummenlinieUnter { get; set; } = "Verfahren nach DIN EN 12831-3 mit den Rechenregeln aus A100/A1, Minutentakt";

    /// <summary><c>ZPG_AUS_KARTE_MINUTENSPITZE</c></summary>
    public string KarteMinutenspitze { get; set; } = "(a) Minutenspitze";

    /// <summary><c>ZPG_AUS_KARTE_MINUTENSPITZE_UNTER</c></summary>
    public string KarteMinutenspitzeUnter { get; set; } = "Größte Minutenleistung des Bedarfstags (größte Stundenleistung nachrichtlich)";

    /// <summary><c>ZPG_AUS_KARTE_PERZENTIL</c></summary>
    public string KartePerzentil { get; set; } = "(b) Perzentil";

    /// <summary><c>ZPG_AUS_KARTE_PERZENTIL_UNTER</c></summary>
    public string KartePerzentilUnter { get; set; } = "Stochastisch superponierte Minutenlast";

    /// <summary><c>ZPG_AUS_PERZENTIL_OFFEN</c></summary>
    public string PerzentilOffen { get; set; } = "noch nicht gerechnet — „Stochastisch rechnen“ in den Eingaben zieht das Ensemble; die Empfehlung stützt sich auf (a) und (c).";

    /// <summary><c>ZPG_AUS_PERZENTIL_LAEUFT</c></summary>
    public string PerzentilLaeuft { get; set; } = "rechnet … — das Ensemble des Bedarfstags wird gezogen.";

    /// <summary><c>ZPG_AUS_STATUS_LAEUFT</c></summary>
    public string StatusEnsembleLaeuft { get; set; } = "Auslegung rechnet … · Ensemble des Bedarfstags";

    /// <summary><c>ZPG_AUS_HINW_ENSEMBLE_ABGEBROCHEN</c></summary>
    public string HinweisEnsembleAbgebrochen { get; set; } = "Das Ensemble ist abgebrochen — „Stochastisch rechnen“ ist wieder aus.";

    /// <summary><c>ZPG_AUS_ENTFAELLT</c></summary>
    public string Entfaellt { get; set; } = "entfällt";

    /// <summary><c>ZPG_AUS_PERZENTIL_ENTFAELLT</c></summary>
    public string PerzentilEntfaellt { get; set; } = "Perzentilwert und Gleichzeitigkeit entfallen: Beim Φ_N {0} kW findet die Summenlinie für {1} von {2} Realisierungen keinen Nachweis.";

    /// <summary><c>ZPG_AUS_PERZENTIL_LAUF</c></summary>
    public string PerzentilLauf { get; set; } = "nach „Stochastisch rechnen“: Seed {0} · {1} Tage gezogen · maßgebender Tag {2}";

    /// <summary><c>ZPG_AUS_PERZENTIL_VOLUMEN</c></summary>
    public string PerzentilVolumen { get; set; } = "Volumen P{0}";

    /// <summary><c>ZPG_AUS_PERZENTIL_VOLUMEN_WERT</c></summary>
    public string PerzentilVolumenWert { get; set; } = "{0} l bei {1} kW";

    /// <summary><c>ZPG_AUS_PERZENTIL_MINUTENSPITZE</c></summary>
    public string PerzentilMinutenspitze { get; set; } = "Minutenspitze P{0}";

    /// <summary><c>ZPG_AUS_PERZENTIL_NACHRICHTLICH</c></summary>
    public string PerzentilNachrichtlich { get; set; } = "nachrichtlich: Minutenspitze P{0} {1} kW · größte Stundenleistung P{0} {2} kW";

    /// <summary><c>ZPG_AUS_NICHT_BELASTBAR</c></summary>
    public string NichtBelastbar { get; set; } = "nicht belastbar";

    /// <summary><c>ZPG_AUS_GRUND_NICHT_BELASTBAR</c></summary>
    public string GrundNichtBelastbar { get; set; } = "{0} Realisierungen; ein empirisches P{1} braucht mindestens {2} = 1/(1 − p).";

    /// <summary><c>ZPG_AUS_STREUBAND</c></summary>
    public string Streuband { get; set; } = "Streuband über {0} Realisierungen";

    /// <summary><c>ZPG_AUS_SP_PERZENTIL</c></summary>
    public string SpaltePerzentil { get; set; } = "Perzentil";

    /// <summary><c>ZPG_AUS_SP_WERT</c></summary>
    public string SpalteWert { get; set; } = "Wert";

    /// <summary><c>ZPG_AUS_SPANNWEITE</c></summary>
    public string Spannweite { get; set; } = "Spannweite (min – max)";

    /// <summary><c>ZPG_AUS_OHNE_NACHWEIS</c></summary>
    public string OhneNachweis { get; set; } = "{0} von {1} Realisierungen ohne Nachweis beim Φ_N (Volumen ∞)";

    /// <summary><c>ZPG_AUS_GLF_V</c></summary>
    public string GlfV { get; set; } = "Gleichzeitigkeit GLF_V";

    /// <summary><c>ZPG_AUS_GLF_V_BEZUG</c></summary>
    public string GlfVBezug { get; set; } = "Ergebnis, kein Eingabefaktor: P{0} des Volumens der Gruppe ÷ Σ P{0} der Volumina je Einheit (Σ n_E = {1})";

    /// <summary><c>ZPG_AUS_GLF_P</c></summary>
    public string GlfP { get; set; } = "Gleichzeitigkeit GLF_P";

    /// <summary><c>ZPG_AUS_GLF_P_BEZUG</c></summary>
    public string GlfPBezug { get; set; } = "Ergebnis, kein Eingabefaktor: P{0} der Minutenspitze der Gruppe ÷ Σ P{0} der Minutenspitze je Einheit (Σ n_E = {1})";

    /// <summary><c>ZPG_AUS_SPITZE_JE_EINHEIT</c></summary>
    public string SpitzeJeEinheit { get; set; } = "Minutenspitze P{0} je Wohnungsstation";

    /// <summary><c>ZPG_AUS_SPITZE_JE_EINHEIT_ZONE</c></summary>
    public string SpitzeJeEinheitZone { get; set; } = "Auslegungsgröße jeder Wohnungsstation; maßgebend ist die Zone „{0}“.";

    /// <summary><c>ZPG_AUS_WURZEL_N</c></summary>
    public string WurzelN { get; set; } = "Einzelstatistik μ + z·σ/√N: {0} kW (Hinweis)";

    /// <summary><c>ZPG_AUS_KARTE_NORM</c></summary>
    public string KarteNorm { get; set; } = "(c) Normvergleich";

    /// <summary><c>ZPG_AUS_KARTE_NORM_UNTER</c></summary>
    public string KarteNormUnter { get; set; } = "DIN 4708 nur für Wohnen mit Speicher, mit Gültigkeitsprüfung";

    /// <summary><c>ZPG_AUS_GEWAEHLTER_PUNKT</c></summary>
    public string GewaehlterPunkt { get; set; } = "Gewählter Punkt";

    /// <summary><c>ZPG_AUS_LADEZEIT</c></summary>
    public string Ladezeit { get; set; } = "Ladezeit je Tag (Σ t_power,on)";

    /// <summary><c>ZPG_AUS_ZEITKONSTANTE</c></summary>
    public string Zeitkonstante { get; set; } = "Zeitkonstante τ (informativ)";

    /// <summary><c>ZPG_AUS_WERTEPAARE_OHNE</c></summary>
    public string WertepaareOhne { get; set; } = "Die Wertepaarkurve entfällt (weniger als zwei Wertepaare).";

    /// <summary><c>ZPG_AUS_BEDARFSKENNZAHL</c></summary>
    public string Bedarfskennzahl { get; set; } = "Bedarfskennzahl N";

    /// <summary><c>ZPG_AUS_VERGLEICHSPUNKT</c></summary>
    public string Vergleichspunkt { get; set; } = "Vergleichspunkt V_DIN";

    /// <summary><c>ZPG_AUS_ZONEN_AUSSERHALB</c></summary>
    public string ZonenAusserhalb { get; set; } = "Außerhalb des Gültigkeitsbereichs: {0}";

    /// <summary><c>ZPG_AUS_HINW_WAERMEPUMPE</c></summary>
    public string HinweisWaermepumpe { get; set; } = "Die Kennzahl N ist für die Vorlauftemperaturen einer Wärmepumpe kaum aussagefähig.";

    /// <summary><c>ZPG_AUS_ROHRNETZ</c></summary>
    public string Rohrnetz { get; set; } = "Rohrnetz-Spitzendurchfluss (nachrichtlich, Verfahren nach DIN 1988-300): noch nicht gerechnet";

    /// <summary><c>ZPG_AUS_GRUND_ROHRNETZ</c></summary>
    public string GrundRohrnetz { get; set; } = "Die Summe der Entnahmearmaturen fehlt im Datenmodell — der Rohrnetz-Spitzendurchfluss kommt mit einer späteren Fassung.";

    /// <summary><c>ZPG_AUS_STAND_NICHT_RECHENBAR</c></summary>
    public string StandNichtRechenbar { get; set; } = "nicht rechenbar";

    /// <summary><c>ZPG_AUS_STAND_AUSSERHALB</c></summary>
    public string StandAusserhalb { get; set; } = "außerhalb des Gültigkeitsbereichs";

    // ------------------------------------------------------------ Empfehlung

    /// <summary><c>ZPG_AUS_EMPFEHLUNG</c></summary>
    public string Empfehlung { get; set; } = "Empfohlener Auslegungspunkt: {0}";

    /// <summary><c>ZPG_AUS_PUNKT_SPEICHER</c></summary>
    public string PunktSpeicher { get; set; } = "Speicher {0} l · Leistung {1} kW";

    /// <summary><c>ZPG_AUS_PUNKT_NENNINHALT</c></summary>
    public string PunktNenninhalt { get; set; } = "nächster Nenninhalt {0} l";

    /// <summary><c>ZPG_AUS_PUNKT_MINUTENSPITZE</c></summary>
    public string PunktMinutenspitze { get; set; } = "Minutenspitze {0} kW";

    /// <summary><c>ZPG_AUS_EMPFEHLUNG_KEINE</c></summary>
    public string EmpfehlungKeine { get; set; } = "Kein Auslegungspunkt — {0}";

    /// <summary><c>ZPG_AUS_SCHNELLAUSLEGUNG</c></summary>
    public string Schnellauslegung { get; set; } = "Schnellauslegung";

    /// <summary><c>ZPG_AUS_ENTWURFSSTAND</c></summary>
    public string Entwurfsstand { get; set; } = "Entwurfsstand — A100/A1 sind Entwürfe, Anwendung besonders zu vereinbaren.";

    /// <summary><c>ZPG_AUS_NACHRICHTLICH</c></summary>
    public string Nachrichtlich { get; set; } = "nachrichtlich";

    /// <summary><c>ZPG_AUS_PUNKT_BLEIBT</c></summary>
    public string PunktBleibt { get; set; } = "Der Auslegungspunkt des Zapfprofils bleibt der Punkt der Summenlinie; der Verfahrensvergleich ist Vergleich, OK übernimmt nur den Punkt.";

    // ------------------------------------------------------------ Verfahrensvergleich

    /// <summary><c>ZPG_AUS_VERGLEICH</c></summary>
    public string Vergleich { get; set; } = "Speicherauslegung — Verfahrensvergleich (Regel: größtes Volumen)";

    /// <summary><c>ZPG_AUS_VERGLEICH_MARKE</c></summary>
    public string VergleichMarke { get; set; } = "Vorschlag";

    /// <summary><c>ZPG_AUS_VERGLEICH_UNTER</c></summary>
    public string VergleichUnter { get; set; } = "Vorschlag an die Speicherauslegung, getrennt von der Dreiergruppe darüber; alle Verfahren lesen dasselbe Mengengerüst, nie die Jahresreihe.";

    /// <summary><c>ZPG_AUS_VERGLEICH_EINGABEN</c></summary>
    public string VergleichEingaben { get; set; } = "Ladeleistung {0} kW ({1}) · Personen {2} · Kennzahl N {3} · nutzbarer Anteil {4} · Zuschlag {5}";

    /// <summary><c>ZPG_AUS_AUTO</c></summary>
    public string Auto { get; set; } = "auto";

    /// <summary><c>ZPG_AUS_MANUELL</c></summary>
    public string Manuell { get; set; } = "manuell";

    /// <summary><c>ZPG_AUS_SP_VERFAHREN</c></summary>
    public string SpalteVerfahren { get; set; } = "Verfahren";

    /// <summary><c>ZPG_AUS_SP_VOLUMEN</c></summary>
    public string SpalteVolumen { get; set; } = "Volumen";

    /// <summary><c>ZPG_AUS_SP_KENNWERT</c></summary>
    public string SpalteKennwert { get; set; } = "Kennwert";

    /// <summary><c>ZPG_AUS_SP_RECHENWEG</c></summary>
    public string SpalteRechenweg { get; set; } = "Rechenweg";

    /// <summary><c>ZPG_AUS_GROESSTER_WERT</c></summary>
    public string GroessterWert { get; set; } = "größter Wert";

    /// <summary><c>ZPG_AUS_NUR_NACHRICHTLICH</c></summary>
    public string NurNachrichtlich { get; set; } = "nur nachrichtlich";

    /// <summary><c>ZPG_AUS_KACHEL_GROESSTER</c></summary>
    public string KachelGroesster { get; set; } = "Größter Wert";

    /// <summary><c>ZPG_AUS_KACHEL_LISTE</c></summary>
    public string KachelListe { get; set; } = "Listengröße (Vorschlag)";

    /// <summary><c>ZPG_AUS_KACHEL_KRITERIUM</c></summary>
    public string KachelKriterium { get; set; } = "Kriterium";

    /// <summary><c>ZPG_AUS_KACHEL_FUELLSTAND</c></summary>
    public string KachelFuellstand { get; set; } = "Füllstand";

    /// <summary><c>ZPG_AUS_NENNINHALT</c></summary>
    public string Nenninhalt { get; set; } = "Nenninhalt {0} l";

    /// <summary><c>ZPG_AUS_MEHRSPEICHER</c></summary>
    public string Mehrspeicher { get; set; } = "Mehrspeicheranlage prüfen";

    /// <summary><c>ZPG_AUS_OHNE_LISTE</c></summary>
    public string OhneListe { get; set; } = "ohne Nenninhaltsliste nicht gerundet";

    /// <summary><c>ZPG_AUS_KRITERIUM_NL</c></summary>
    public string KriteriumNl { get; set; } = "Speicher mit N_L ≥ {0}";

    /// <summary><c>ZPG_AUS_KRITERIUM_OHNE</c></summary>
    public string KriteriumOhne { get; set; } = "ohne Kennzahl N kein Kriterium";

    /// <summary><c>ZPG_AUS_FUELLSTAND</c></summary>
    public string Fuellstand { get; set; } = "min. {0} von {1} kWh, {2} Restreserve ({3})";

    /// <summary><c>ZPG_AUS_ZEITPUNKT</c></summary>
    public string Zeitpunkt { get; set; } = "Maßgebender Zeitpunkt der Stundenbilanz: {0}, Tag {1} von 14 (in Woche 2 gezählt), {2}–{3} Uhr.";

    /// <summary><c>ZPG_AUS_DMAX_NULL</c></summary>
    public string DmaxNull { get; set; } = "D_max = 0: Die Ladeleistung deckt jede Stundenlast; „profilbasiert“ zeigt „–“ statt 0 l — den größten Wert liefert dann DIN 4708.";

    /// <summary><c>ZPG_AUS_WOCHENBILD</c></summary>
    public string Wochenbild { get; set; } = "Maßgebende Woche = Woche mit dem größten Tagesbedarf des Jahres; Woche 2 der Zweiwochenbilanz aus Mengengerüst, Tagesgang und Wochenfaktoren — nicht aus der Jahresreihe.";

    /// <summary><c>ZPG_AUS_BTN_UEBERGEBEN</c></summary>
    public string KnopfUebergeben { get; set; } = "An Speicherauslegung übergeben…";

    /// <summary><c>ZPG_AUS_GRUND_UEBERGEBEN</c></summary>
    public string GrundUebergeben { get; set; } = "Die Übergabe an die Speicherauslegung kommt mit einer späteren Fassung.";

    // ------------------------------------------------------------ Warnliste

    /// <summary><c>ZPG_AUS_WARNLISTE</c></summary>
    public string Warnliste { get; set; } = "Hinweise und Warnungen der Auslegung";

    /// <summary><c>ZPG_AUS_WARNLISTE_UNTER</c></summary>
    public string WarnlisteUnter { get; set; } = "Klartext mit eingesetzten Zahlen; nichts davon blockiert OK.";

    /// <summary><c>ZPG_AUS_WARNLISTE_LEER</c></summary>
    public string WarnlisteLeer { get; set; } = "Keine Hinweise.";

    /// <summary><c>ZPG_AUS_KONSISTENZ</c></summary>
    public string Konsistenz { get; set; } = "Konsistenzhinweis (stochastische Spitze gegen die Leistung des Summenlinienpunkts): noch nicht geprüft";

    /// <summary><c>ZPG_AUS_GRUND_KONSISTENZ</c></summary>
    public string GrundKonsistenz { get; set; } = "Der Konsistenzhinweis braucht das Perzentil — „Stochastisch rechnen“ einschalten.";

    /// <summary><c>ZPG_AUS_KONSISTENZ_OK</c></summary>
    public string KonsistenzOk { get; set; } = "Konsistenzhinweis geprüft: Die größte Stundenleistung P{0} ({1} kW) liegt nicht über dem {2}-Fachen der Leistung Φ_N des Summenlinienpunkts ({3} kW).";

    /// <summary><c>ZPG_AUS_KONSISTENZ_AUFFAELLIG</c></summary>
    public string KonsistenzAuffaellig { get; set; } = "Konsistenzhinweis: Die größte Stundenleistung P{0} ({1} kW) liegt über dem {2}-Fachen der Leistung Φ_N des Summenlinienpunkts ({3} kW) — Bedarfstag und Summenlinie prüfen.";

    /// <summary><c>ZPG_AUS_KONSISTENZ_LAEUFT</c></summary>
    public string KonsistenzLaeuft { get; set; } = "Konsistenzhinweis: wird mit dem Ensemble geprüft — rechnet …";

    /// <summary><c>ZPG_AUS_KONSISTENZ_OHNE_PERZENTIL</c></summary>
    public string KonsistenzOhnePerzentil { get; set; } = "Konsistenzhinweis entfällt — das Perzentil ist nicht rechenbar (Grund in Karte (b)).";

    /// <summary><c>ZPG_AUS_KONSISTENZ_OHNE_SCHWELLE</c></summary>
    public string KonsistenzOhneSchwelle { get; set; } = "Konsistenzhinweis entfällt — die Schwelle fehlt im Parametersatz.";

    /// <summary><c>ZPG_AUS_STUFE_HINWEIS</c></summary>
    public string StufeHinweis { get; set; } = "Hinweis";

    /// <summary><c>ZPG_AUS_STUFE_WARNUNG</c></summary>
    public string StufeWarnung { get; set; } = "Warnung";

    // ------------------------------------------------------------ Fuß und Rückfrage

    /// <summary><c>ZPG_AUS_STATUS_PUNKT</c></summary>
    public string StatusPunkt { get; set; } = "Punkt gewählt: {0}";

    /// <summary><c>ZPG_AUS_STATUS_OHNE_PUNKT</c></summary>
    public string StatusOhnePunkt { get; set; } = "Kein Auslegungspunkt — OK übernimmt nur die Eingaben.";

    /// <summary><c>ZPG_AUS_STATUS_OHNE</c></summary>
    public string StatusOhne { get; set; } = "Keine Auslegung — {0}";

    /// <summary><c>ZPG_AUS_RUECKFRAGE_TITEL</c></summary>
    public string RueckfrageTitel { get; set; } = "Bedarfstag aus dem Stundenprofil";

    /// <summary><c>ZPG_AUS_RUECKFRAGE_STUNDENPROFIL</c></summary>
    public string RueckfrageStundenprofil { get; set; } = "Der Bedarfstag stammt aus dem Stundenprofil — Spitzen unter einer Stunde sind unterschätzt. Den Punkt trotzdem übernehmen?";

    /// <summary><c>ZPG_AUS_JA</c></summary>
    public string Ja { get; set; } = "Übernehmen";

    /// <summary><c>ZPG_AUS_NEIN</c></summary>
    public string Nein { get; set; } = "Zurück";

    // ------------------------------------------------------------ Konstruktor

    /// <summary><c>ZPG_AUS_KON_TITEL</c></summary>
    public string KonstruktorTitel { get; set; } = "Bedarfstag konstruieren";

    /// <summary><c>ZPG_AUS_KON_LBL_NAME</c></summary>
    public string KonstruktorName { get; set; } = "Name des Bedarfstags";

    /// <summary><c>ZPG_AUS_KON_HINWEIS</c></summary>
    public string KonstruktorHinweis { get; set; } = "Zeilen nach dem Verfahren der A100 (NA.5.2.3): Zeitfenster in Stunden, Volumen je Zapfregel und Anzahl oder direkt mit Zapftemperatur. Die Energie gilt beim Kaltwasser der Auslegung des Katalogs.";

    /// <summary><c>ZPG_AUS_KON_SP_BEGINN</c></summary>
    public string KonstruktorBeginn { get; set; } = "Beginn [h]";

    /// <summary><c>ZPG_AUS_KON_SP_ENDE</c></summary>
    public string KonstruktorEnde { get; set; } = "Ende [h]";

    /// <summary><c>ZPG_AUS_KON_SP_REGEL</c></summary>
    public string KonstruktorRegel { get; set; } = "Zapfregel";

    /// <summary><c>ZPG_AUS_KON_SP_ANZAHL</c></summary>
    public string KonstruktorAnzahl { get; set; } = "Anzahl";

    /// <summary><c>ZPG_AUS_KON_SP_VOLUMEN</c></summary>
    public string KonstruktorVolumen { get; set; } = "Volumen [l]";

    /// <summary><c>ZPG_AUS_KON_SP_TEMPERATUR</c></summary>
    public string KonstruktorTemperatur { get; set; } = "Zapftemperatur [°C]";

    /// <summary><c>ZPG_AUS_KON_SP_VERBRAUCHER</c></summary>
    public string KonstruktorVerbraucher { get; set; } = "Verbraucher";

    /// <summary><c>ZPG_AUS_KON_REGEL_FREI</c></summary>
    public string KonstruktorRegelFrei { get; set; } = "Volumen direkt";

    /// <summary><c>ZPG_AUS_KON_REGEL</c></summary>
    public string KonstruktorRegelText { get; set; } = "{0} · {1} l je Vorgang bei {2} °C";

    /// <summary><c>ZPG_AUS_KON_BTN_ZEILE_NEU</c></summary>
    public string KonstruktorZeileNeu { get; set; } = "Zeile hinzufügen";

    /// <summary><c>ZPG_AUS_KON_BTN_ZEILE_ENTFERNEN</c></summary>
    public string KonstruktorZeileEntfernen { get; set; } = "Entfernen";

    /// <summary><c>ZPG_AUS_KON_REGELN_OHNE</c></summary>
    public string KonstruktorRegelnOhne { get; set; } = "Der Katalog trägt keine Zapfregel — nur Zeilen mit direktem Volumen.";

    /// <summary><c>ZPG_AUS_KON_SUMME</c></summary>
    public string KonstruktorSumme { get; set; } = "{0} Zeilen";

    /// <summary><c>ZPG_AUS_KON_FELD</c></summary>
    public string KonstruktorFeld { get; set; } = "{0}, Zeile {1}";

    /// <summary><c>ZPG_AUS_KON_FEHLEINGABE</c></summary>
    public string KonstruktorFehleingabe { get; set; } = "Keine gültige Zahl: {0}";

    /// <summary><c>ZPG_AUS_KON_GRUND_LETZTE_ZEILE</c></summary>
    public string KonstruktorGrundLetzteZeile { get; set; } = "Die letzte Zeile bleibt — ein Bedarfstag braucht mindestens eine Zeile.";

    // ------------------------------------------------------------ Eingaben des Verfahrensvergleichs, Laufangaben, Konstruktor (Z4, Gruppe 2b)

    /// <summary><c>ZPG_AUS_GRP_VERGLEICH_EINGABEN</c></summary>
    public string GruppeVergleichEingaben { get; set; } = "Eingaben des Verfahrensvergleichs";

    /// <summary><c>ZPG_AUS_HINW_VERGLEICH_EINGABEN</c></summary>
    public string HinweisVergleichEingaben { get; set; } = "Sie wirken nur auf den Verfahrensvergleich (nachrichtlich), nicht auf den empfohlenen Punkt, und werden mit dem Projekt gespeichert.";

    /// <summary><c>ZPG_AUS_LBL_LADELEISTUNG</c></summary>
    public string LabelLadeleistung { get; set; } = "Ladeleistung";

    /// <summary><c>ZPG_AUS_LBL_LADE_MANUELL</c></summary>
    public string LabelLadeManuell { get; set; } = "Ladeleistung manuell";

    /// <summary><c>ZPG_AUS_LBL_LADEFENSTER</c></summary>
    public string LabelLadefenster { get; set; } = "Ladezeitfenster";

    /// <summary><c>ZPG_AUS_LBL_LADEFENSTER_BEGINN</c></summary>
    public string LabelLadefensterBeginn { get; set; } = "Beginn des Ladezeitfensters";

    /// <summary><c>ZPG_AUS_LBL_NUTZANTEIL</c></summary>
    public string LabelNutzanteil { get; set; } = "Nutzbarer Anteil";

    /// <summary><c>ZPG_AUS_LBL_ZUSCHLAG</c></summary>
    public string LabelZuschlag { get; set; } = "Zuschlag";

    /// <summary><c>ZPG_AUS_LBL_PERSONEN</c></summary>
    public string LabelPersonen { get; set; } = "Personen";

    /// <summary><c>ZPG_AUS_LBL_PERSONEN_MANUELL</c></summary>
    public string LabelPersonenManuell { get; set; } = "Personen manuell";

    /// <summary><c>ZPG_AUS_LBL_FUELLSTAND_BEZUG</c></summary>
    public string LabelFuellstandBezug { get; set; } = "Bezug des Füllstands";

    /// <summary><c>ZPG_AUS_FUELLSTAND_VORGABE</c></summary>
    public string FuellstandVorgabe { get; set; } = "Vorgabe: Nenninhalt des Punkts, sonst der Punkt";

    /// <summary><c>ZPG_AUS_BTN_VORSCHLAG</c></summary>
    public string KnopfVorschlag { get; set; } = "Als manuellen Wert übernehmen";

    /// <summary><c>ZPG_AUS_LBL_VORSCHLAG</c></summary>
    public string LabelVorschlag { get; set; } = "Vorschlag: {0} {1}";

    /// <summary><c>ZPG_AUS_HERL_LADE</c></summary>
    public string HerleitungLade { get; set; } = "Angesetzt: {0} kW ({1}) · {2}";

    /// <summary><c>ZPG_AUS_HERL_LADE_OHNE</c></summary>
    public string HerleitungLadeOhne { get; set; } = "Einen Vorschlag gibt es erst mit einer gerechneten Speichergruppe.";

    /// <summary><c>ZPG_AUS_HERL_LADEFENSTER</c></summary>
    public string HerleitungLadefenster { get; set; } = "leer = Vorgabe des Katalogs; dieselben Werte wie „Ladezeitfenster“ im Zapfprofil";

    /// <summary><c>ZPG_AUS_HERL_PERSONEN</c></summary>
    public string HerleitungPersonen { get; set; } = "auto = aus dem Mengengerüst der Zonen: {0}; angesetzt: {1}";

    /// <summary><c>ZPG_AUS_HERL_ANGESETZT</c></summary>
    public string HerleitungAngesetzt { get; set; } = "leer = Vorgabe des Katalogs; angesetzt: {0}";

    /// <summary><c>ZPG_AUS_HERL_FUELLSTAND</c></summary>
    public string HerleitungFuellstand { get; set; } = "Auf dieses Volumen bezieht sich der Füllstand der Kachel; angesetzt: {0}";

    /// <summary><c>ZPG_AUS_HERL_GESPEICHERT</c></summary>
    public string HerleitungGespeichert { get; set; } = "wird mit dem Projekt gespeichert; {0}";

    /// <summary><c>ZPG_AUS_HERL_WERKSTOFF</c></summary>
    public string HerleitungWerkstoff { get; set; } = "kein Vorschlag aus dem Projekt; ohne Angabe braucht die Summenlinie U·A, die Fläche oder die Übertragerleistung";

    /// <summary><c>ZPG_AUS_BTN_VORSCHLAG_WAEHLEN</c></summary>
    public string KnopfErzeugerVorschlag { get; set; } = "Vorschlag wählen";

    /// <summary><c>ZPG_AUS_ERZEUGERART_VORSCHLAG</c></summary>
    public string LabelErzeugerVorschlag { get; set; } = "Vorschlag aus dem Projekt: {0}";

    /// <summary><c>ZPG_AUS_KON_LBL_BEZUGSART</c></summary>
    public string KonstruktorLabelBezugsart { get; set; } = "Bezugsart";

    /// <summary><c>ZPG_AUS_KON_OHNE_BEZUG</c></summary>
    public string KonstruktorOhneBezug { get; set; } = "ohne Bezug — der Tag gilt, wie er ist";

    /// <summary><c>ZPG_AUS_KON_LBL_BEZUGSMENGE</c></summary>
    public string KonstruktorLabelBezugsmenge { get; set; } = "Bezugsmenge";

    /// <summary><c>ZPG_AUS_KON_HINW_BEZUG</c></summary>
    public string KonstruktorHinweisBezug { get; set; } = "Mit Bezug skaliert die Auslegung den Tag auf die Bezugsmenge einer Gruppe derselben Bezugsart; beides wird mit dem Tag gespeichert.";
}
