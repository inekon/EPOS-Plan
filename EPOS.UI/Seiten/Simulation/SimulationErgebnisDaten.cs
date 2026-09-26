using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Ein waehlbarer Steuerwert samt Anzeigetext (iU9-W11b) — die Drei-Schichten-Regel
/// als Datensatz: Der <see cref="Wert"/> steht in der Datenbank
/// (<c>DbWerte.SP_*</c>), der <see cref="Text"/> auf dem Bildschirm.
/// </summary>
public sealed record Steuerwahl(string Wert, string Text);

/// <summary>
/// Eine Zeile eines Ergebnisrasters — Modul-, Puffer- und Brennstoffzeilen der
/// Reiter teilen sich diese Form, weil sie alle dasselbe sind: ein Name und
/// eine Handvoll bereits formatierter Zahlen.
/// </summary>
/// <param name="Zellen">Die Zellen in Spaltenreihenfolge, fertig formatiert.</param>
/// <param name="Hinweis">Mouseover-Text der Zeile; leer = keiner.</param>
public sealed record Rasterzeile(IReadOnlyList<string> Zellen, string Hinweis = "");

/// <summary>
/// Eine Brennstoffzeile des Heizkessel- bzw. BHKW-Reiters: Beschriftung, Wert und
/// die Praesenzregel „sichtbar, wenn der Jahreswert &gt; 0 ist ODER ein Kessel des
/// Projekts diesen Brennstoff fuehrt" (Vorlaeufer
/// <c>KesselBrennstoffZeilenAnpassen</c> :1134-1195).
/// </summary>
public sealed record Brennstoffzeile(string Bezeichnung, double Wert, bool Sichtbar);

// =========================================================================
//  Die Parameterseite (R1 samt P1, P2, P4, P5) und der Speicherparameterblock
// =========================================================================

/// <summary>
/// Die Felder des Stromspeicher-Parameterblatts (Vorlaeufer
/// <c>InitStromspeicherParameter</c> :5690-5990, 302 Zeilen mit 29 programmatischen
/// Steuerelementen).
///
/// <para><b>Sie stehen seit W11b‑B‑28 im ERGEBNISreiter „Stromspeicher"</b>
/// (<c>SpeicherParameterBlock.razor</c>) und nicht mehr als Blatt P3 im Reiter
/// „Parameter" — Anwenderwunsch 10.09.2026. Der Weg der Daten bleibt derselbe: Die
/// Huelle fuellt sie in <c>ParameterDaten.Speicher</c>, die Seite reicht sie durch.</para>
/// </summary>
public sealed class SpeicherParameterDaten
{
    /// <summary>Gibt es ueberhaupt eine aktive Variante? Ohne sie sind die Felder Attrappen.</summary>
    public bool VarianteVorhanden;

    /// <summary>Fusszeile: welche Variante wird hier bearbeitet (bzw. „keine").</summary>
    public string Variantenstatus = "";

    public double SoCMinProzent;
    public double SoCMaxProzent;

    /// <summary>Das kWh-Aequivalent des SoC-Bandes (Abnahmebefund 1); leer = unbestimmt.</summary>
    public string SoCMinKwh = "";
    public string SoCMaxKwh = "";

    public double Ladeschwellwert;

    /// <summary>Lade-/Entladeleistung [kW] — Gerätedatum aus <c>Tab_Stromspeicher</c>.</summary>
    public double LadeleistungKw;

    /// <summary>Nennkapazität [kWh] — Gerätedatum aus <c>Tab_Stromspeicher</c>.</summary>
    public double KapazitaetKwh;

    /// <summary>
    /// Dürfen <see cref="LadeleistungKw"/> und <see cref="KapazitaetKwh"/> geändert
    /// werden? (W11b‑B‑28)
    ///
    /// <para>Sie gehören der ANLAGE und nicht der Variante: Varianten desselben
    /// Speichers teilen sich EINE Gerätekopie in <c>Tab_Stromspeicher</c>. Deshalb
    /// setzt die Hülle das Feld auf dieselbe Bedingung, unter der auch
    /// <c>StromspeicherSimCtrl.UebernehmeAuslegung</c> schreibt: <b>genau eine</b>
    /// <c>SP_TYP</c>-Anlage im Projekt. Sonst bleiben die Felder gesperrt und tragen
    /// den bisherigen Hinweis <c>SP_PARAM_HINWEIS_LADELEISTUNG</c>.</para>
    /// </summary>
    public bool GeraetegroesseAenderbar;

    public string Betriebsart = "";
    public string Berechnungsart = "";
    public IReadOnlyList<Steuerwahl> Betriebsarten = Array.Empty<Steuerwahl>();
    public IReadOnlyList<Steuerwahl> Berechnungsarten = Array.Empty<Steuerwahl>();

    /// <summary>Der Kompatibilitaetsmodus ist nur bei bestimmten Berechnungsarten waehlbar.</summary>
    public bool Kompatibilitaet;
    public bool KompatibilitaetMoeglich;

    /// <summary>Zielschwelle P_ziel [kW] der Lastspitzenkappung; 0 heisst „nicht gepflegt".</summary>
    public double PeakZiel;

    /// <summary>Die Schwelle zieht sich selbst nach, statt gegen <see cref="PeakZiel"/> zu rechnen.</summary>
    public bool PeakZielAdaptiv;

    /// <summary>
    /// Die zwei Peak-Felder gehoeren zur Berechnungsart „Lastspitzenkappung" und
    /// stehen nur dort — bei jeder anderen Art waeren sie eine Eingabe ohne Wirkung.
    /// </summary>
    public bool PeakZielMoeglich;

    /// <summary>
    /// Die HERLEITUNGSZEILE unter dem Peak-Ziel: die Netzbezugsspitze ohne Speicher des
    /// letzten Laufs. Leer, solange kein Lauf eine liefert — dann steht dort nichts,
    /// statt eine Zahl zu erfinden.
    /// </summary>
    public string PeakHerleitung = "";

    public bool LadenAusPv;
    public bool LadenAusBhkw;
    public bool Netzentladung;

    /// <summary>Ausbaustufe 11 — sichtbar, aber dauerhaft gesperrt.</summary>
    public bool BhkwStromgefuehrt;

    public double Kapitalzins;
    public double Nutzungsdauer;
    public double Leistungspreis;
    public double Netzladeaufschlag;

    public string Preisquelle = "";
    public IReadOnlyList<Steuerwahl> Preisquellen = Array.Empty<Steuerwahl>();

    /// <summary>Beschriftung der Reihenauswahl — „Preisreihe" oder „Kostenprofil".</summary>
    public string PreisreiheLabel = "";
    public int PreisreiheId;
    public IReadOnlyList<(int Id, string Text)> Preisreihen = Array.Empty<(int, string)>();
    public bool PreisreiheMoeglich;
    public bool Aufschlag;

    /// <summary>Die Preisvorschau; <see cref="PreisinfoWarnung"/> faerbt sie.</summary>
    public string Preisinfo = "";
    public bool PreisinfoWarnung;
}

/// <summary>
/// Die Reihenauswahl zu EINER Preisquelle (W11b‑B‑28) — Beschriftung, Wählbarkeit,
/// Liste und vorbelegte Id.
///
/// <para><b>Wozu ein eigener Datensatz.</b> Im Parameterblock ist der Wechsel der
/// Preisquelle keine Schreib-, sondern eine ANZEIGEfrage: Aus „Spotmarkt" wird
/// „Preisreihe" mit den Reihen aus <c>Tab_Preisreihe</c>, aus „Profil" wird
/// „Kostenprofil" mit denen aus <c>Tab_Kostenprofil</c>, und beim Fixpreis ist die
/// Liste leer und gesperrt. Bis W11b‑B‑28 kam diese Umstellung nur mit dem nächsten
/// vollständigen Lesen der Seite — der Block bleibt beim Wechsel der Quelle aber
/// stehen, und eine falsch beschriftete Liste wäre schlimmer als eine unveränderte.</para>
/// </summary>
public sealed class SpeicherPreisreihenDaten
{
    /// <summary>Beschriftung der Reihenauswahl — „Preisreihe" oder „Kostenprofil".</summary>
    public string Label = "";

    /// <summary>Ist die Liste überhaupt wählbar? (Beim Fixpreis nicht.)</summary>
    public bool Moeglich;

    public IReadOnlyList<(int Id, string Text)> Reihen = Array.Empty<(int, string)>();

    /// <summary>Die zu dieser Quelle gepflegte Id (Preisreihe bzw. Kostenprofil); 0 = keine.</summary>
    public int Id;
}

/// <summary>
/// Die fünf LAUFPARAMETER des Projekts und der Stand des Speicherblocks.
///
/// <para><b>Der Reiter „Parameter" ist mit Auftrag #216 gefallen</b>
/// (Windows-Abnahme 11.09.2026, Punkt 3: „Nimm Parameter heraus — die
/// Netzverluste können an eine andere Stelle"). Seine vier Unterblätter stehen
/// seither dort, wo der Anwender die Sache ohnehin einstellt: die
/// <see cref="Netzverluste"/> im Abschnitt „Wärmebedarf" von Schritt ①, die drei
/// Erzeugerwerte in der jeweiligen Erzeugerkarte daneben. Mit dem Reiter sind
/// <c>Unterblaetter</c> und die Schlüsselklasse <c>ParameterBlatt</c> entfallen —
/// welche Karte welches Feld zeigt, entscheidet ihr <c>DbWert</c>.</para>
///
/// <para><see cref="Speicher"/> gehört seit W11b‑B‑28 in den Ergebnisreiter
/// „Stromspeicher" und wird von der Ergebnisseite dorthin durchgereicht; die
/// Hülle liest beides in einem Zug.</para>
/// </summary>
public sealed class ParameterDaten
{
    // ---- P1: Wärme-/Strombedarf (immer) ----
    public double Netzverluste;
    public string NetzverlusteEinheit = "%";

    // ---- P2: BHKW ----

    /// <summary>0 = wärmegeführt, 1 = stromgeführt, 2 = ohne Einspeisung.</summary>
    public int Betriebsart;
    public int UntersteLeistungsgrenze;

    // ---- Stromspeicher: Ergebnisreiter „Stromspeicher" (W11b-B-28) ----
    public SpeicherParameterDaten Speicher = new SpeicherParameterDaten();

    // ---- P4: Wärmepumpe ----
    //
    // 16.09.2026 (Auftrag #299): Hier stand "public bool Heizstab" - der PROJEKTweite
    // Heizstabschalter aus Tab_Einstellungen.WP_Heizstab. Er gehört seither der
    // WÄRMEPUMPE (Tab_Energieanlagen.Heizstab je Anlage) und reist deshalb mit den
    // Anlagendaten, nicht mit den Laufparametern des Projekts.

    // ---- P5: Heizkessel ----
    public double Bereitschaft;

    // ---- Kühlung (Stufe KU1, Kühlkonzept 8.3; K10, E27) ----

    /// <summary>
    /// Die Projekteinstellung „Kühlung rechnen" (<c>Tab_Einstellungen.Kuehlbetrieb</c>):
    /// <c>true</c> = der Lauf rechnet den Kühlkanal. Ohne Einstellungssatz „aus".
    /// </summary>
    public bool Kuehlbetrieb;

    // ---- Anlagenkopplung (Konzept Anlagenkopplung 9.4, AK1 Welle 3) ----

    /// <summary>
    /// Die Projekteinstellung „Anlagenkopplung" (<c>Tab_Einstellungen.Anlagenkopplung</c>) als
    /// Steuerwert (<c>DbWerte.ANLAGENKOPPLUNG_*</c>); <c>null</c> = aus (NULL in der Spalte).
    /// </summary>
    public string? Anlagenkopplung;

    /// <summary>
    /// Die ARBEITSKOPIE für einen Dialog, der erst im OK-Weg schreiben darf
    /// (Anwenderwunsch 16.09.2026, <c>KomponentenKonfigurationDialog</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="Speicher"/> wird als REFERENZ übernommen: Der Block gehört dem
    /// Ergebnisreiter „Stromspeicher" (W11b‑B‑28) und wird von keinem Dialog
    /// bearbeitet, der eine Kopie bräuchte.
    /// </remarks>
    public ParameterDaten Kopie() => new ParameterDaten
    {
        Netzverluste = Netzverluste,
        NetzverlusteEinheit = NetzverlusteEinheit,
        Betriebsart = Betriebsart,
        UntersteLeistungsgrenze = UntersteLeistungsgrenze,
        Speicher = Speicher,
        Bereitschaft = Bereitschaft,
        Kuehlbetrieb = Kuehlbetrieb,
        Anlagenkopplung = Anlagenkopplung
    };
}

// =========================================================================
//  Die Reiterdaten, die nicht schon als Kern-DTO vorliegen
// =========================================================================

/// <summary>
/// Der Bedarfsreiter (R4). Die vier Zahlen kommen aus
/// <c>SimulationErgebnisCtrl.Bedarf</c>; hier stehen nur die Kanalzeilen und
/// ihre Praesenz.
/// </summary>
public sealed class BedarfDaten
{
    public double WaermelastMaxKw;
    public double WaermebedarfGesamtMwh;
    public double StrombedarfMaxKw;
    public double StrombedarfGesamtMwh;

    /// <summary>Die drei Kanaele (Heizung, Brauchwasser, Prozesswärme) in MWh.</summary>
    public IReadOnlyList<double> KanalMwh = Array.Empty<double>();

    /// <summary>Beschriftung je Kanal.</summary>
    public IReadOnlyList<string> Kanalnamen = Array.Empty<string>();

    /// <summary>Führt der Lauf diesen Kanal? Nur dann steht sein Schalter da.</summary>
    public IReadOnlyList<bool> KanalDa = Array.Empty<bool>();

    /// <summary>
    /// Die Kälteseite (Stufe KU1, Kühlkonzept 8.4); <c>null</c> = nicht erhoben (Projekt
    /// ohne Kühlung) — dann steht weder im Bedarfsreiter noch in der Übersicht eine
    /// Kältegruppe, auch keine Nullen (K18).
    /// </summary>
    public KaelteDaten? Kaelte;
}

/// <summary>
/// Die KÄLTESEITE eines Laufs für Bedarfsreiter und Übersicht (Stufe KU1; Kühlkonzept 8.4;
/// E21, K5, K6). Die Zahlen kommen aus <c>SimulationErgebnisCtrl.Kaelte</c>; die Sätze baut
/// die Hülle fertig, die Komponente kennt keinen Ressourcenschlüssel der Kälteseite.
/// </summary>
public sealed class KaelteDaten
{
    /// <summary>Jahreskälte = Summe des Kühlkanals [MWh/a].</summary>
    public double KaeltebedarfMwh;

    /// <summary>Kältespitze [kW].</summary>
    public double KaeltelastMaxKw;

    /// <summary>Stunden mit Kühlbedarf [h/a], gezählt am Kanalvektor.</summary>
    public int StundenMitKuehlbedarf;

    /// <summary>Vollbenutzungsstunden der Kälte [h/a]; <c>null</c> ohne Spitze.</summary>
    public double? VollbenutzungsstundenH;

    /// <summary>Ungedeckte Kälte [MWh/a] — ohne Kälteerzeuger der ganze Bedarf.</summary>
    public double KaelterestbedarfMwh;

    /// <summary>Die Beschriftung der Kanalzeile „Kühlung" (vierter Kanal).</summary>
    public string Kanalname = "";

    /// <summary>
    /// Die Sätze unter den Zahlen, fertig formuliert: ungedeckt ohne Kälteerzeuger bzw. „kein
    /// Kältebedarf", die Stunden mit Heizen und Kühlen (K6) — ohne die Grenze der Zahl, die
    /// steht in <see cref="GrenzeFeuchte"/>.
    /// </summary>
    public IReadOnlyList<string> Hinweise = Array.Empty<string>();

    /// <summary>Der Satz zur Grenze der Kältezahl (K5) — er steht an JEDER Kältezahl.</summary>
    public string GrenzeFeuchte = "";

    /// <summary>Der Satz der Übersicht zur Deckung (ohne Kälteerzeuger: ungedeckt).</summary>
    public string Deckungshinweis = "";

    // ---- Die Deckung (Stufe KU2 Welle 3; Kühlkonzept 8.4; E21, E34) ------------------
    // Ohne Kälteerzeuger bleiben die Felder leer - dann zeigt der Block allein den Bedarf
    // und den ungedeckten Rest.

    /// <summary>Deckungsgrad des Kühlkanals [%]; <c>null</c> ohne Kälteerzeuger.</summary>
    public double? DeckungsgradProzent;

    /// <summary>Kältestrom samt Hilfsstrom [MWh/a]; <c>null</c> ohne Kälteerzeuger.</summary>
    public double? KaeltestromMwh;

    /// <summary>Jahresarbeitszahl Kälte (EER-Jahreswert); <c>null</c> ohne Kältestrom.</summary>
    public double? EerJahreswert;

    /// <summary>Der Netzbezug des Kältestroms [MWh/a] (E34); <c>null</c> ohne Kälteerzeuger.</summary>
    public double? KaeltestromNetzbezugMwh;

    /// <summary>Die Kälteerzeugertabelle (#32) — die Wärmeerzeugertabelle bleibt bei drei Kanälen.</summary>
    public IReadOnlyList<KaelteerzeugerAnzeige> Erzeuger = Array.Empty<KaelteerzeugerAnzeige>();

    /// <summary>Die HTML-Legende des Kälterings — dieselbe Segmentliste wie das Bild.</summary>
    public IReadOnlyList<Ringanteil> Legende = Array.Empty<Ringanteil>();

    /// <summary>
    /// BV-E6 (Konzept Berichtsvorlagen 9.5): die Platzhalter der Kältekennzahlen des Dashboards —
    /// beim Stamm <c>stamm.kennzahl.kaelte.*</c>, bei einer Variante <c>stand.kennzahl.kaelte.*</c>;
    /// leer = keine Marke (ohne gültiges Ergebnis). Die Hülle setzt sie.
    /// </summary>
    public string BedarfFeld = "", LastFeld = "", RestFeld = "", DeckungFeld = "", StromFeld = "", JazFeld = "";

    /// <summary>
    /// BV-E6: Deckungsgrad und Jahresarbeitszahl Kälte rechnet der Bericht über den Kennzahlenkatalog, die App aus
    /// dem Lauf — die Marken tragen dann „ähnlich im Bericht“ mit <see cref="KennzahlHinweis"/>.
    /// </summary>
    public Vorlagenfeldstufe DeckungStufe = Vorlagenfeldstufe.Entspricht, JazStufe = Vorlagenfeldstufe.Entspricht;

    /// <summary>BV-E6: der Hinweis der Marken der Stufe „ähnlich“; sonst leer.</summary>
    public string KennzahlHinweis = "";
}

/// <summary>
/// Eine Zeile der Kälteerzeugertabelle der Übersicht (Stufe KU2 Welle 3; Kühlkonzept 8.4) —
/// Mengen in MWh/a, der Stromträger als fertiger Text (E34).
/// </summary>
public sealed record KaelteerzeugerAnzeige(string Anlage, int Vorlauf, double KaelteMwh, double StromMwh,
                                          double? Eer, double NetzbezugMwh, string Stromtraeger);

/// <summary>
/// Der Uebersichtsreiter (R2 + <c>NavigatorUebersicht</c>). Die 13 Zahlen und die
/// sechs Summen stehen im Kern-DTO; hier steht, was die Anzeige darum herum
/// braucht.
/// </summary>
public sealed class UebersichtDaten
{
    /// <summary>Die Praesenzregel je Erzeuger — sie blendet Zeilen und Ringsegmente aus.</summary>
    public bool Waermepumpe, Heizstab, Heizkessel, Solarthermie, Bhkw, Photovoltaik, Stromspeicher;

    /// <summary>
    /// Die Steuerwerte (<c>DbWerte.ERZEUGER_*</c>) der Technologien, die das Projekt
    /// ANGELEGT hat, die aber auf keinem Platz der Simulation stehen (#190).
    ///
    /// <para>Sie sind der Grund, warum eine Zeile dieser Uebersicht 0,00 zeigt, obwohl
    /// das Projekt die Anlage fuehrt: Die Praesenzregel laesst sie stehen (Punkt 4 —
    /// „eine vorhandene Anlage mit 0-kWh-Ergebnis bleibt sichtbar"), gerechnet hat sie
    /// aber nie. Der Zusatz „(nicht in der Kaskade)" sagt das an der Zeile; die
    /// vollstaendige Meldung samt Abhilfe steht in den Hinweisen zum Lauf.</para>
    ///
    /// <para>Die Liste kommt aus derselben Vorpruefung
    /// (<c>SimulationLaufCtrl.ErzeugerOhneKaskadenplatz</c>), die auch das Laufprotokoll
    /// speist — EINE Wahrheit, kein zweiter Vergleich in der Anzeige.</para>
    /// </summary>
    public IReadOnlyCollection<string> OhneKaskadenplatz = Array.Empty<string>();

    /// <summary>
    /// Dieselben Erzeuger als HANDLUNGSFÄHIGE Meldung (Anwenderentscheid vom
    /// 16.09.2026, Punkt c) — je Anlage ein Satz und, wenn ein Platz frei ist, der Weg
    /// in die Simulationskonfiguration. Leer = alles Angelegte rechnet auch.
    ///
    /// <para><b>Sie stammt aus DERSELBEN Vorprüfung</b> wie
    /// <see cref="OhneKaskadenplatz"/> (<c>SimulationLaufCtrl</c>) — der Zusatz an der
    /// Tabellenzeile und die Meldung darüber sagen also dasselbe, nicht zweierlei.</para>
    /// </summary>
    public IReadOnlyList<Platzangebot> OhnePlatzAngebote = Array.Empty<Platzangebot>();

    /// <summary>
    /// BEDARFSKANÄLE OHNE VERSORGER — je Kanal mit Bedarf, den keine Senke einer Anlage
    /// bedient, der fertige Satz („Kanal Prozesswärme mit 50,0 MWh/a Bedarf hat keinen
    /// Versorger …"). Leer = jeder Kanal mit Bedarf hat einen Versorger.
    ///
    /// <para>Derselbe Satz wie im Laufprotokoll — beide kommen aus
    /// <c>Warnkriterien.KanaeleOhneVersorger</c> mit dem Kanalbedarf DIESES Laufs.</para>
    /// </summary>
    public IReadOnlyList<string> KanaeleOhneVersorger = Array.Empty<string>();

    /// <summary>Der Mittelwert der beiden Ringe in Prozent.</summary>
    public double WaermedeckungProzent;
    public double StromdeckungProzent;

    /// <summary>Gibt es überhaupt einen Bedarf? Ohne ihn kein Ring (Befund W11-B36).</summary>
    public bool WaermebedarfVorhanden;
    public bool StrombedarfVorhanden;

    public double ReststromMwh;
    public double RestwaermeMwh;

    /// <summary>
    /// Die Wärmespalte des Dashboards: je Erzeuger die Deckung nach Kanälen
    /// (<c>FillTableWithData</c>), dazu Summen- und Restzeile (#222).
    /// </summary>
    public Erzeugertabelle WaermeTabelle = new Erzeugertabelle();

    /// <summary>Die Stromspalte des Dashboards — ohne Stromerzeuger nur die Restzeile.</summary>
    public Erzeugertabelle StromTabelle = new Erzeugertabelle();

    /// <summary>
    /// Die Legende des WÄRMERINGS als Daten (#222) — je Segment Name, Menge, Anteil
    /// und Farbe, der ungedeckte Rest zuletzt.
    ///
    /// <para>Sie kommt aus DERSELBEN Segmentliste wie das Bild
    /// (<c>SimulationErgebnisHuelle.SegmenteWaerme</c>); zwei Wege wären zwei
    /// Wahrheiten über denselben Kreis.</para>
    /// </summary>
    public IReadOnlyList<Ringanteil> WaermeLegende = Array.Empty<Ringanteil>();

    /// <summary>Dasselbe für den Stromring.</summary>
    public IReadOnlyList<Ringanteil> StromLegende = Array.Empty<Ringanteil>();

    /// <summary>Der Wärmebedarf [MWh/a] — der Nenner des Wärmerings.</summary>
    public double WaermebedarfMwh;

    /// <summary>Der Strombedarf samt Eigenverbrauch [MWh/a] — der Nenner des Stromrings.</summary>
    public double StrombedarfMwh;

    /// <summary>
    /// Die vier Wärmeplätze des Laufs als Text, z. B. „Wärmepumpe → Heizkessel"
    /// (<c>SimulationControl.tool</c> 1…4). Leer = kein Platz belegt.
    /// </summary>
    public string Kaskade = "";

    /// <summary>
    /// Führt das Projekt überhaupt einen Stromerzeuger (PV, BHKW, Speicher)? Sonst
    /// trägt die Stromspalte statt einer Erzeugertabelle den Weg in die
    /// Konfiguration (#222, Mockup „Ringvariante A").
    /// </summary>
    public bool StromerzeugerVorhanden;

    /// <summary>
    /// BV-E6 (Konzept Berichtsvorlagen 9.5): die Platzhalter der Dashboardzahlen — beim Stamm
    /// <c>stamm.kennzahl.*</c>, bei einer Variante <c>stand.kennzahl.*</c>; leer = keine Marke
    /// (die App zeigt eine Zahl, die der Katalog nicht führt). Die Hülle setzt sie.
    /// </summary>
    public string WaermebedarfFeld = "", RestwaermeFeld = "", StrombedarfFeld = "", ReststromFeld = "";

    /// <summary>BV-E6: die Platzhalter der beiden Ringe (im Bericht Kuchendiagramme je Stand, „ähnlich“).</summary>
    public string RingWaermeFeld = "", RingStromFeld = "";
}

/// <summary>
/// Ein Legendeneintrag eines Deckungsrings (#222): Name, Menge, Anteil und die
/// Farbe des Segments im Bild.
///
/// <para><b>Warum die Legende aus dem Bild heraus ist.</b> Im PNG war sie eine
/// Rastergrafik: nicht kopierbar, nicht mitwachsend, und am Rand des Rahmens
/// abgeschnitten (Windows-Foto „Heinestr 15", rechte Grafik). Als HTML daneben ist
/// sie Text.</para>
/// </summary>
/// <param name="Name">Der Segmentname, z. B. „Wärmepumpe".</param>
/// <param name="Mwh">Die Menge [MWh/a].</param>
/// <param name="Prozent">Sein Anteil an der Summe aller Segmente.</param>
/// <param name="Farbe">Die Segmentfarbe als CSS-Wert, z. B. <c>#2ECC71</c>.</param>
/// <param name="IstRest">Der ungedeckte Rest — er steht abgesetzt am Fuß der Legende.</param>
public sealed record Ringanteil(string Name, double Mwh, double Prozent, string Farbe,
                                bool IstRest = false);

/// <summary>
/// Ein Spaltenkopf der Erzeugertabelle (#222): Beschriftung und Einheit GETRENNT.
///
/// <para>Vorher stand „Deckung Heizung [MWh/a]" als eine Zeichenkette im Kopf,
/// linksbündig über rechtsbündigen Zahlen — die Zahl stand dreihundert Bildpunkte
/// von ihrem Kopf entfernt. Getrennt kann der Kopf rechtsbündig ÜBER seiner Zahl
/// stehen und die Einheit eine kleine zweite Zeile bilden.</para>
/// </summary>
/// <param name="Text">Die Beschriftung, z. B. „Heizung".</param>
/// <param name="Einheit">Die Einheit, z. B. „MWh/a"; leer = ohne zweite Zeile.</param>
public sealed record Tabellenkopf(string Text, string Einheit = "");

/// <summary>
/// Eine Zeile der Erzeugertabelle (#222).
/// </summary>
/// <param name="Name">Der Erzeuger — die erste, linksbündige Spalte.</param>
/// <param name="Zahlen">Die bereits formatierten Zahlen in Spaltenreihenfolge.</param>
/// <param name="OhneBeitrag">
/// Jede Zahl dieser Zeile ist null — die Zeile steht gedimmt und lässt sich
/// ausblenden. Sie verschwindet NICHT von selbst: Eine angelegte Anlage ohne
/// Beitrag ist eine Aussage (Präsenzregel Punkt 4, #190).
/// </param>
/// <param name="Zusatz">Klammerzusatz am Namen, z. B. „(nicht in der Kaskade)".</param>
public sealed record Erzeugerzeile(string Name, IReadOnlyList<string> Zahlen,
                                   bool OhneBeitrag = false, string Zusatz = "");

/// <summary>
/// Ein angelegter Erzeuger OHNE Platz in der Simulation, samt dem Weg dorthin
/// (Anwenderentscheid vom 16.09.2026, Punkt c).
/// </summary>
/// <param name="IdAnlage">
/// <c>Tab_Energieanlagen.ID</c> — die Karte, die die Simulationskonfiguration
/// hervorhebt, wenn der Anwender dem Angebot folgt.
/// </param>
/// <param name="Kennung">
/// Die sprachneutrale Meldungskennung (<c>KiMeldungskennung</c>) für den Link
/// „erklären lassen" am Banner.
/// </param>
/// <param name="Text">Die Meldung — derselbe Satz, den auch das Laufprotokoll führt.</param>
/// <param name="AufnahmeMoeglich">
/// Ist überhaupt ein Platz frei? Nur dann steht der Knopf da; sonst sagt
/// <paramref name="Sperrgrund"/>, warum nicht.
/// </param>
/// <param name="Sperrgrund">
/// Der Satz statt des Knopfes, wenn kein Platz frei ist; leer, solange einer frei ist.
/// </param>
public sealed record Platzangebot(int IdAnlage, string Kennung, string Text,
                                  bool AufnahmeMoeglich, string Sperrgrund = "");

/// <summary>
/// Die Erzeugertabelle EINER Spalte des Dashboards (#222) — Köpfe, Zeilen, die
/// Summe der Erzeuger und der Rest.
/// </summary>
public sealed class Erzeugertabelle
{
    /// <summary>Die Spaltenköpfe; die erste ist die Namensspalte.</summary>
    public IReadOnlyList<Tabellenkopf> Spalten = Array.Empty<Tabellenkopf>();

    /// <summary>Die Erzeugerzeilen in Kaskadenreihenfolge.</summary>
    public IReadOnlyList<Erzeugerzeile> Zeilen = Array.Empty<Erzeugerzeile>();

    /// <summary>Die Summe der Erzeuger; <c>null</c> = keine (dann steht auch kein Strich).</summary>
    public Erzeugerzeile? Summe;

    /// <summary>Der Rest, der nach allen Erzeugern übrig bleibt.</summary>
    public Erzeugerzeile? Rest;

    /// <summary>Satz statt Zeilen, wenn das Projekt keinen Erzeuger dieser Art führt.</summary>
    public string LeerText = "";

    /// <summary>Wie viele Zeilen ohne Beitrag dastehen (Zähler des Schalters).</summary>
    public int OhneBeitrag
    {
        get
        {
            int n = 0;
            foreach (Erzeugerzeile z in Zeilen) if (z.OhneBeitrag) n++;
            return n;
        }
    }
}

/// <summary>
/// Die Autarkie-Analyse (<c>DashboardForm</c>). Die Zahlen rechnet die Huelle je
/// Kapazitaet neu — die Kapazitaet ist eine Was-wäre-wenn-Groesse und wird
/// NICHT gespeichert (Befund W11-B32).
/// </summary>
public sealed class AutarkieDaten
{
    public bool HatPv = true;
    public bool HatSolarthermie = true;

    public double AutarkiePvProzent;
    public double DeckungStProzent;

    /// <summary>Ohne Wärmebedarf steht hier „nicht benötigt" statt einer Zahl.</summary>
    public bool DeckungStBekannt = true;

    public double NutzungsgradStProzent;
    public double Co2ErsparnisKg;
    public double SpeichernutzenKwh;
    public double SpeicherKwh;

    /// <summary>
    /// Zeigt das Blatt den STROM-Monatsstapel? Mit Photovoltaik immer; ohne sie nur,
    /// wenn auch keine Solarthermie da ist — ein Projekt nur mit Solarthermie bekäme
    /// sonst ein leeres Strombild über seinem Wärmebild.
    /// </summary>
    public bool ZeigtStromMonate => HatPv || !HatSolarthermie;

    /// <summary>Zeigt das Blatt den WÄRME-Monatsstapel „Wärmebedarf &amp; Deckung"?</summary>
    public bool ZeigtWaermeMonate => HatSolarthermie;
}

/// <summary>
/// Eine waehlbare Reihe der beiden Ganglinien-Reiter: sprachneutraler Schluessel,
/// Anzeigetext und ob der Lauf sie ueberhaupt fuehrt.
/// </summary>
/// <param name="Schluessel">Der Serienschluessel — nie ein Anzeigetext.</param>
/// <param name="Text">Die Beschriftung des Schalters.</param>
/// <param name="Vorhanden">
/// Fuehrt der Lauf die Reihe? Fehlende werden AUSGEBLENDET und abgewaehlt — sonst
/// naehme der CSV-Export eine unsichtbare Spalte mit
/// (<c>NavigatorStrom.CheckboxenAnordnen</c> :266).
/// </param>
public sealed record Ganglinienreihe(string Schluessel, string Text, bool Vorhanden);

/// <summary>Der Waermegang-Reiter (<c>NavigatorWaerme</c>).</summary>
public sealed class WaermegangDaten
{
    /// <summary>
    /// Die Summenlinie „GESAMT" und dahinter die bis zu fuenf Erzeugerreihen in
    /// Kaskadenreihenfolge. Die Summe steht UEBER ihren Summanden und ist
    /// abschaltbar wie jeder einzelne Erzeuger.
    /// </summary>
    public IReadOnlyList<Ganglinienreihe> Erzeuger = Array.Empty<Ganglinienreihe>();

    /// <summary>Die Speicherfuellstaende; leer = das Projekt fuehrt keinen Speicher.</summary>
    public IReadOnlyList<Ganglinienreihe> Speicher = Array.Empty<Ganglinienreihe>();

    /// <summary>
    /// „Gesamt" und die Bedarfsarten mit Jahressumme &gt; 0; der erste Eintrag traegt
    /// den Steuerwert <c>-1</c> (Produktion), die uebrigen den Kanalindex.
    /// </summary>
    public IReadOnlyList<(int Kanal, string Text)> Bedarfsarten = Array.Empty<(int, string)>();
}

/// <summary>Der Stromgang-Reiter (<c>NavigatorStrom</c>).</summary>
public sealed class StromgangDaten
{
    /// <summary>
    /// Verbrauchsstapel, Erzeugungslinien und die Kontrolllinie
    /// „Summe Stromverbrauch" (Schluessel „GESAMT").
    /// </summary>
    public IReadOnlyList<Ganglinienreihe> Reihen = Array.Empty<Ganglinienreihe>();
}

/// <summary>
/// Der Stromspeicher-Reiter (R10). Kopfzeile, zwoelf Kacheln, 39 Kennzahlzeilen
/// (die kommen als <c>SpeicherKennzahlenBlock.Zeile</c> aus dem Kern), Ampel und
/// Warnzeile.
/// </summary>
public sealed class SpeicherErgebnisDaten
{
    /// <summary>Übernommene Projektflotte und tatsächlicher letzter Flottenlauf.</summary>
    public bool FlotteImProjektAktiv;
    public SpeicherEngine.FlottenStudieKonfiguration? AktiveFlotte;
    public bool FlottenAenderungOhneNeuenLauf;
    public SpeicherFlottenErgebnis? Flottenergebnis;
    /// <summary>Gab es ueberhaupt einen Speicherlauf?</summary>
    public bool LaufVorhanden;

    /// <summary>Kopfzeile: Variante, Betriebsart, Berechnungsart — bzw. „noch kein Lauf".</summary>
    public string Kopf = "";

    /// <summary>Die zwoelf Kacheln in ihrer Reihenfolge (Titel, Wert).</summary>
    public IReadOnlyList<(string Titel, string Wert)> Kacheln = Array.Empty<(string, string)>();

    /// <summary>Die 39 Kennzahlzeilen aus <c>SpeicherKennzahlenBlock.Zeilen</c>.</summary>
    public IReadOnlyList<WindowsFormsApplication1.SpeicherKennzahlenBlock.Zeile> Kennzahlen
        = Array.Empty<WindowsFormsApplication1.SpeicherKennzahlenBlock.Zeile>();

    /// <summary>Gibt es einen Vergleichslauf? Ohne ihn bleibt die Vergleichsspalte weg.</summary>
    public bool MitVergleich;

    /// <summary>Die Zyklenampel; <see cref="AmpelWarnung"/> faerbt sie.</summary>
    public string Ampel = "";
    public bool AmpelWarnung;

    /// <summary>Warnzeile „Lauf ohne jede Erzeugung"; leer = keine.</summary>
    public string Erzeugungshinweis = "";

    /// <summary>Vergleichen laesst sich erst ab zwei Varianten (Fachkonzept 7.3).</summary>
    public bool MehrereVarianten;
}

/// <summary>
/// Eine Zeile des Variantenvergleichs (<c>Form_SpeicherVariantenVergleich</c>).
/// Die Zahlen sind bereits formatiert — die Rechnung steht in der Huelle.
/// </summary>
public sealed class Vergleichszeile
{
    public int IdEnergieanlage;
    public bool Aktiv;
    public bool Gerechnet;

    public string Bezeichnung = "";
    public string Betriebsart = "";
    public string Berechnungsart = "";

    public string Kapazitaet = "";
    public string Leistung = "";
    public string Investition = "";
    public string Ertrag = "";
    public string DeltaJ = "";
    public string Amortisation = "";
    public string Kapitalwert = "";
    public string Vollzyklen = "";

    /// <summary>Grund einer Fehlerzeile — Mouseover-Text.</summary>
    public string Hinweis = "";
}

/// <summary>Das Ergebnis eines Variantenvergleichslaufs.</summary>
public sealed class VergleichDaten
{
    public IReadOnlyList<Vergleichszeile> Zeilen = Array.Empty<Vergleichszeile>();

    /// <summary>Index der besten Zeile nach ΔJ; −1 = keine.</summary>
    public int BesteZeile = -1;

    /// <summary>Statuszeile; <see cref="StatusWarnung"/> faerbt sie.</summary>
    public string Status = "";
    public bool StatusWarnung;

    /// <summary>Es gibt Varianten, aber keine ist aktiv — der Langtext dazu.</summary>
    public bool HinweisKeineAktive;

    /// <summary>Das Protokoll der Laeufe (mehrzeilig).</summary>
    public string Protokoll = "";
}

// =========================================================================
//  Der Gesamtstand einer Auffrischung
// =========================================================================

/// <summary>
/// In welchem Zustand das ANGEZEIGTE Ergebnis ist (Auftrag <b>#236</b>,
/// Anwenderrueckmeldung 12.09.2026).
///
/// <para><b>Warum ein Zustand und kein Schalter.</b> Bis #236 trug der Stand nur die
/// Marke <c>ErgebnisGueltig</c>. Sie sagte, ob gespeichert werden darf — nicht, WARUM
/// nicht. Die Uebersicht zeichnete deshalb in JEDEM dieser Faelle ein vorbelegtes DTO
/// wie ein Ergebnis: „Strombedarf 0,00 MWh/a", „Deckung 0,0 %" und die Marke „kein
/// Stromerzeuger im Projekt", obwohl die Zusammenfassung daneben 2 850,20 MWh/a
/// nannte. Wer nicht gerechnet hat, hat kein Ergebnis; wer es veraltet hat, hat ein
/// altes — beides ist eine Aussage, und beides ist nicht „alles null".</para>
/// </summary>
public enum ErgebnisZustand
{
    /// <summary>Es ist noch nie gerechnet worden (oder ein Projektwechsel hat den Lauf verworfen).</summary>
    NichtGerechnet = 0,

    /// <summary>Ein vollstaendiger Lauf steht; nur hier darf gespeichert werden.</summary>
    Gueltig = 1,

    /// <summary>
    /// Es ist gerechnet worden, aber seither wurde etwas geaendert, das den Lauf
    /// ueberholt — jeder Besuch der Stromspeicher-Auslegung, der speichert oder
    /// rechnet. <c>LaufGerechnet</c> bleibt dabei wahr.
    /// </summary>
    Veraltet = 2,

    /// <summary>Der Lauf ist nicht durchgegangen: Vorpruefung, Fehler oder Abbruch.</summary>
    Abgebrochen = 3
}

/// <summary>
/// Was die Ergebnisseite nach EINEM Lauf zeigt (iU9-W11b.1). Die Huelle traegt es
/// zusammen; die Seite und ihre Reiter rechnen nichts nach.
/// </summary>
public sealed class SimulationErgebnisDaten
{
    public int IdProjekt;

    /// <summary>Die Schema-Migration ist nicht durchgekommen (ADR-001): alles gesperrt.</summary>
    public bool Gesperrt;
    public string Sperrgrund = "";

    /// <summary>
    /// Der Zustand des angezeigten Ergebnisses (Auftrag #236). Er ist die WAHRHEIT;
    /// <see cref="ErgebnisGueltig"/> leitet sich daraus ab.
    /// </summary>
    public ErgebnisZustand Zustand = ErgebnisZustand.NichtGerechnet;

    /// <summary>
    /// Der ANLASS des Zustands, in Anwendersprache und ohne Satzzeichen am Ende — bei
    /// <see cref="ErgebnisZustand.Veraltet"/> die Aenderung, die den Lauf ueberholt hat,
    /// bei <see cref="ErgebnisZustand.Abgebrochen"/> der Abbruchgrund. Leer = keiner
    /// bekannt; die Seite setzt dann ihren allgemeinen Satz.
    /// </summary>
    /// <remarks>
    /// <b>Nur der Anlass, nicht der ganze Satz.</b> Wie der Satz lautet, entscheidet die
    /// ANZEIGE (<c>SimulationErgebnisSeite.Zustandstext</c>): Sie weiss als Einzige, ob
    /// derselbe Grund schon als Warnbanner ueber dem Reiterstapel steht.
    /// </remarks>
    public string Zustandsgrund = "";

    /// <summary>
    /// Liegt ein vollstaendiger Lauf vor? Nur dann darf gespeichert werden.
    /// </summary>
    /// <remarks>
    /// <b>Seit #236 eine ABLEITUNG</b> aus <see cref="Zustand"/>. Der Setzer bleibt als
    /// Kurzform fuer Pruefstaende und alten Programmtext: <c>true</c> setzt
    /// <see cref="ErgebnisZustand.Gueltig"/>, <c>false</c>
    /// <see cref="ErgebnisZustand.NichtGerechnet"/>. Wer „veraltet" oder „abgebrochen"
    /// meint, setzt den Zustand selbst — sonst geht der Grund verloren.
    /// </remarks>
    public bool ErgebnisGueltig
    {
        get => Zustand == ErgebnisZustand.Gueltig;
        set => Zustand = value ? ErgebnisZustand.Gueltig : ErgebnisZustand.NichtGerechnet;
    }

    /// <summary>Die Parameterseite.</summary>
    public ParameterDaten Parameter = new ParameterDaten();

    // ---- Welche Fachreiter zeigt die Leiste? (BefuelleQuellenListe :2876-2970) ----
    public bool ReiterWaermepumpe;
    public bool ReiterHeizkessel;
    public bool ReiterBhkw;
    public bool ReiterSolarthermie;
    public bool ReiterPhotovoltaik;
    public bool ReiterStromspeicher;

    // ---- Die Zahlen je Reiter; null = der Lauf fuehrt die Komponente nicht ----
    public WindowsFormsApplication1.SimulationErgebnisCtrl.UebersichtKennzahlen? Kennzahlen;

    /// <summary>
    /// Das Dashboard der Uebersicht; <c>null</c> = es gibt kein gerechnetes Ergebnis
    /// (<see cref="Zustand"/> sagt, warum).
    /// </summary>
    /// <remarks>
    /// <b>BEFUND #236.</b> Hier stand bis zum 12.09.2026 ein <c>= new UebersichtDaten()</c>.
    /// Die Huelle stieg bei ungueltigem Ergebnis aus, bevor sie das Feld fuellte — und
    /// der Reiter zeichnete die Vorbelegung wie ein Ergebnis: lauter Nullen, dazu die
    /// Marke „kein Stromerzeuger im Projekt". Ein Reiter zeichnet nie ein vorbelegtes
    /// DTO als Ergebnis; ohne Lauf steht hier <c>null</c>, und die Anzeige zeigt ihren
    /// Leerzustand mit Grund.
    /// </remarks>
    public UebersichtDaten? Uebersicht;

    /// <summary>
    /// Die Zahlen der BEDARFSRECHNUNG. Sie stehen in JEDEM Zustand da (#236) — sie
    /// haengen nicht am Lauf, sondern am Projekt, und es sind dieselben Zahlen, die die
    /// Projektzusammenfassung des Startreiters nennt.
    /// </summary>
    public BedarfDaten Bedarf = new BedarfDaten();
    public WindowsFormsApplication1.SimulationErgebnisCtrl.WaermepumpeErgebnis? Waermepumpe;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.HeizkesselErgebnis? Heizkessel;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.SolarthermieErgebnis? Solarthermie;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.BhkwErgebnis? Bhkw;
    public WindowsFormsApplication1.SimulationErgebnisCtrl.PhotovoltaikErgebnis? Photovoltaik;
    public SpeicherErgebnisDaten Speicher = new SpeicherErgebnisDaten();
    public AutarkieDaten Autarkie = new AutarkieDaten();
    public WaermegangDaten Waermegang = new WaermegangDaten();
    public StromgangDaten Stromgang = new StromgangDaten();

    /// <summary>Die zehn Brennstoffzeilen des Heizkesselreiters samt Praesenz.</summary>
    public IReadOnlyList<Brennstoffzeile> KesselBrennstoffe = Array.Empty<Brennstoffzeile>();

    /// <summary>Die Brennstoffzeilen des BHKW — nur die mit Verbrauch &gt; 0.</summary>
    public IReadOnlyList<Brennstoffzeile> BhkwBrennstoffe = Array.Empty<Brennstoffzeile>();

    /// <summary>
    /// Fuehrt mindestens ein Heizkessel des Projekts einen gepflegten Brennstoff
    /// (Auftrag BH-1)? Entscheidet den Text des leeren Brennstoffblocks.
    /// </summary>
    public bool KesselBrennstoffDefiniert;

    /// <summary>
    /// Fuehrt mindestens ein BHKW des Projekts einen gepflegten Brennstoff
    /// (Auftrag BH-1)? Entscheidet den Text des leeren Brennstoffblocks: „nicht
    /// gelaufen" statt „kein Brennstoff definiert".
    /// </summary>
    public bool BhkwBrennstoffDefiniert;

    /// <summary>Die Erdreich-Kurztexte der VDI-4640-Pruefung; leer = keine.</summary>
    public IReadOnlyList<string> ErdreichHinweise = Array.Empty<string>();
    public bool ErdreichWarnung;

    /// <summary>Fuehrt der Lauf Speichertemperaturen? Nur dann steht das Unterblatt da.</summary>
    public bool Speichertemperaturen;

    /// <summary>
    /// BV-E6: der Platzhalter des Speichertemperaturbilds — nur beim Stammprojekt
    /// (<c>stamm.bild.speichertemperaturen</c>, „ähnlich“); leer = keine Marke.
    /// </summary>
    public string SpeichertemperaturFeld = "";

    /// <summary>Die Meldungen des Laufs (Warnungen und Hinweise); leer = keine.</summary>
    public string Laufmeldungen = "";
    public int LaufmeldungenAnzahl;
}

// =========================================================================
//  Die Datenseite — die Huelle legt sie ein
// =========================================================================

/// <summary>
/// Ein Bildauftrag der Ergebnisseite: welcher Reiter, in welcher Schalterstellung.
/// Die Seite bildet daraus ihren Zwischenspeicherschluessel — sechzehn Zeichenmodelle
/// je Lauf im Voraus zu bauen waere zu teuer (Risiko der Vermessung § 11.5).
/// </summary>
/// <param name="Bild">Sprachneutraler Bildschluessel (<c>BEDARF_WAERME</c>, …).</param>
/// <param name="Sortiert">Dauerlinie statt Ganglinie.</param>
/// <param name="Kanal">Bedarfsart des Wärmegangs; −1 = Produktion.</param>
/// <param name="Reihen">Die gewaehlten Serienschluessel; leer = alle.</param>
/// <param name="Zahl">Freier Zahlenparameter (Was-wäre-wenn-Kapazitaet der Autarkie).</param>
/// <remarks>
/// <b>DER RUNDLAUF-DATENZOOM IST ENTFALLEN</b> (Entscheid DG-E3-9, Konzept Diagramme
/// § 6). Bis zur Etappe DG-E3 trug der Auftrag einen sechsten Wert
/// <c>Diagrammbereich Bereich</c>: das Rechteck, das der Anwender im PNG aufgezogen
/// hatte, in Bildanteilen — die Hülle liess den Kern daraus einen Achsenbereich machen
/// und rechnete das Bild ein zweites Mal. Seit jedes Bild im Baustein
/// <c>DiagrammSvg</c> steht, liegt der Zoom in der <c>viewBox</c> der Zeichenflaeche:
/// eine Attributaenderung, kein zweiter Renderlauf. Die vier Bilder ohne Zeitachse
/// kannten nie einen Bereich.
/// </remarks>
public sealed record Bildauftrag(string Bild, bool Sortiert = false, int Kanal = -1,
                                 IReadOnlyList<string>? Reihen = null, double Zahl = 0.0)
{
    /// <summary>Der Zwischenspeicherschluessel — er trennt zwei Schalterstellungen.</summary>
    public string Schluessel =>
        Bild + "|" + (Sortiert ? "1" : "0") + "|" + Kanal + "|" + Zahl.ToString("R") + "|" +
        (Reihen is null ? "" : string.Join(",", Reihen));
}

/// <summary>Die sprachneutralen Bildschluessel der Ergebnisseite.</summary>
public static class Bilder
{
    public const string BedarfWaerme = "BEDARF_WAERME";
    public const string BedarfStrom = "BEDARF_STROM";

    /// <summary>Die Kältelast des Projekts — ein EIGENES Bild, nicht im Wärmebild (Stufe KU1).</summary>
    public const string BedarfKaelte = "BEDARF_KAELTE";
    public const string RingWaerme = "RING_WAERME";
    public const string RingStrom = "RING_STROM";

    /// <summary>Der Ring „Kältedeckung" (Stufe KU2 Welle 3) — nur mit Kälteerzeuger.</summary>
    public const string RingKaelte = "RING_KAELTE";
    public const string WpProduktion = "WP_PRODUKTION";
    public const string WpStromverbrauch = "WP_STROMVERBRAUCH";
    public const string WpLeistungTemperatur = "WP_LEISTUNG_TEMPERATUR";
    public const string Speichertemperaturen = "SPEICHERTEMPERATUREN";
    public const string Heizkessel = "HEIZKESSEL";
    public const string Solarthermie = "SOLARTHERMIE";
    public const string Bhkw = "BHKW";
    public const string Photovoltaik = "PHOTOVOLTAIK";
    public const string SpeicherBetrieb = "SPEICHER_BETRIEB";
    public const string AutarkieMonate = "AUTARKIE_MONATE";

    /// <summary>Der Wärme-Monatsstapel der Autarkie-Analyse — nur mit Solarthermie.</summary>
    public const string WaermeAutarkieMonate = "WAERME_AUTARKIE_MONATE";
    public const string Waermegang = "WAERMEGANG";
    public const string Stromgang = "STROMGANG";
}

// Das Ergebnis eines Schreib- oder Rechenwegs meldet der Datensatz
// EPOS.UI.Seiten.Simulation.Rueckmeldung (SimulationKonfigDaten.cs, iU9-W10b.1) —
// dieselbe Frage, dieselbe Antwortform, deshalb kein zweiter Typ.

/// <summary>
/// Die Datenseite der Ergebnisseite — die Windows-Huelle legt sie ein
/// (iU9-W11b.1). Ohne Delegat geschieht an der Stelle nichts, und ein Knopf ohne
/// Delegat erscheint gar nicht erst.
/// </summary>
public sealed class SimulationErgebnisDienste
{
    /// <summary>Traegt alles zusammen, was die Seite zeigt. Nie <c>null</c>.</summary>
    public Func<int, SimulationErgebnisDaten>? Laden;

    /// <summary>
    /// Startet den Lauf. Die Huelle faehrt ihn in <c>Task.Run</c>, meldet den
    /// Fortschritt ueber <paramref name="melder"/> und nimmt den Abbruch entgegen;
    /// die Rueckmeldung traegt bei einem Abbruch dessen Grund.
    /// </summary>
    public Func<Action<double?, string>, Task<Rueckmeldung>>? Laufen;

    /// <summary>Bricht den laufenden Lauf ab.</summary>
    public Action? Abbrechen;

    /// <summary>Speichert das Ergebnis nach <c>Tab_Ergebnis*</c>.</summary>
    public Func<Rueckmeldung>? Speichern;

    /// <summary>
    /// Das ZEICHENMODELL EINES Bildes (Etappe DG-E3) — erst beim Betreten des
    /// Reiters, dann zwischengespeichert. Die Seite zeigt es im Baustein
    /// <c>DiagrammSvg</c>.
    ///
    /// <para><b>Einen zweiten Weg für Pixelbilder gibt es nicht mehr.</b> Bis zur
    /// Gruppe (c) stand daneben ein <c>Bild</c>-Delegat für die vier Bilder ohne
    /// Zeitachse — die Streuwolke, die zwei Ringe und die Monatssäulen der Autarkie.
    /// Auch sie sind jetzt Modelle: Drei zeigen den Wert des Elements unter dem
    /// Zeiger, die Streuwolke lässt sich zudem spreizen.</para>
    /// </summary>
    public Func<Bildauftrag, Zeichenmodell?>? Modell;

    /// <summary>
    /// Die Farbe einer Reihe anwendungsweit setzen (<c>Diagrammfarben.Setze</c>) —
    /// der Klick auf das Farbfeld eines Legendeneintrags. Ohne Delegat bietet kein
    /// Diagramm den Waehler an.
    /// </summary>
    public Func<Farbrolle, Farbe, Task>? FarbeSetzen;

    /// <summary>„Hausfarbe": die Rolle wieder auf die Vorgabe.</summary>
    public Func<Farbrolle, Task>? FarbeZuruecksetzen;

    // ---- Die FUENF Laufparameter stehen seit #216 in Schritt ① ----
    //
    // Netzverluste, BHKW-Betriebsart, untere Leistungsgrenze, Heizstab und
    // Betriebsbereitschaft haben diese Seite mit dem Reiter „Parameter" verlassen
    // (Windows-Abnahme 11.09.2026, Punkt 3). Ihre Delegaten stehen unveraendert in
    // SimulationParameterDienste und kommen aus DERSELBEN Huelle - ein zweiter
    // Satz hier waere eine zweite Wahrheit.

    /// <summary>
    /// Ein Feld der Speichervariante bzw. der Speicheranlage — der Schluessel benennt
    /// das Feld (<see cref="SpeicherFeld"/>), der Wert steht als Zeichenkette darin
    /// (Zahlen invariant, Schalter "1"/"0"). Die Rueckmeldung traegt den Grund einer
    /// Abweisung (Pruefung) oder eines Fehlschlags — sonst die Bestaetigung.
    /// </summary>
    /// <remarks>
    /// <b>Der EINZIGE Schreibweg der Speicherparameter</b> (W11b‑B‑29,
    /// Anwenderentscheid 1 vom 10.09.2026: "Sofort schreiben, wie ueberall sonst im
    /// Programm."). Ihn nehmen der Parameterblock des Ergebnisreiters
    /// (<c>SpeicherParameterBlock</c>, jedes Feld einzeln und sofort) UND die
    /// Auslegungsoptimierung fuer ihren Leistungspreis L_P
    /// (<c>OptimierungLeistungspreis</c>, W11b‑E‑3). Der gepufferte Satzweg aus
    /// W11b‑B‑28 ist ersatzlos entfallen: Sein Puffer starb beim Reiterwechsel, und
    /// der Anwender fand den Knopf am Blockende nicht.
    /// <para><b>Geprueft wird VOR dem Schreiben</b>, je Feld die Regel, die zu ihm
    /// gehoert (<c>SpeicherParameterPruefung</c> im Kern). Ein Verstoss weist ab, und
    /// es geht nichts in die Datenbank; der Wert bleibt im Feld stehen, damit der
    /// Anwender zu Ende tippen kann.</para>
    /// </remarks>
    public Func<string, string, Rueckmeldung>? SpeicherfeldSchreiben;

    /// <summary>
    /// Die Reihenauswahl zu einer Preisquelle — sie LIEST nur (kein Schreibweg).
    /// Ohne Delegat bleibt die Liste so stehen, wie sie geliefert wurde.
    /// </summary>
    public Func<string, SpeicherPreisreihenDaten>? SpeicherPreisreihen;

    // ---- Was die Seite oeffnet ----

    // Der Parametersatz der KONFIGURATIONSSEITE stand hier (W10b) - die Seite
    // oeffnete sie als zweite Ueberlagerung in ihrer eigenen. Seit Auftrag #207
    // ist die Konfiguration Schritt ① DERSELBEN Ansicht (SIM-Q1), und ihren
    // Parametersatz legt SimulationHuelle unmittelbar an die Ansicht.

    /// <summary>Parametersatz des Bedarfsergebnis-Dialogs (W8); true = Wärme, false = Strom.</summary>
    public Func<bool, IReadOnlyDictionary<string, object>>? BedarfGaben;

    /// <summary>Parametersatz des Wärmepumpendialogs (W7).</summary>
    public Func<IReadOnlyDictionary<string, object>>? WaermepumpenGaben;

    /// <summary>Nimmt das Ergebnis des Wärmepumpendialogs entgegen (true = übernommen).</summary>
    public Action<bool>? WaermepumpenFertig;

    /// <summary>Rechnet den Variantenvergleich; <paramref name="melder"/> zaehlt n von m.</summary>
    public Func<Action<double?, string>, Task<VergleichDaten>>? VergleichRechnen;

    /// <summary>Setzt eine Variante aktiv; die Rueckmeldung traegt den Fehlertext.</summary>
    public Func<int, Rueckmeldung>? VarianteAktivSetzen;

    /// <summary>Schreibt die Vergleichstabelle als CSV.</summary>
    public Func<Task<Rueckmeldung>>? VergleichCsv;

    // ---- Die Auslegung des Stromspeichers (W11b-B-5, seit P3/#192 eine ANSICHT) ----
    //
    // Bis zur Windows-Abnahme V2 (07.09.2026) stand hier EIN Delegat: die
    // Sprungbruecke in Form_SpeicherOptimierung. Danach waren es zwoelf - die
    // Datenseite der zwei Ueberlagerungen SpeicherFlottenDialog und
    // SpeicherOptimierungDialog. BEIDE Dialoge sind mit Paket P3 (#192) gefallen;
    // ihre Delegaten stehen jetzt an der Ansicht STROMSPEICHER_AUSLEGUNG und
    // fuehren in EPOS.Kern/Controller/StromspeicherAuslegungCtrl.
    //
    // SEIT AUFTRAG #274 sind es ZWEI, und der Grund dafuer ist der Anwenderwunsch
    // vom 14.09.2026: Der Einstieg in die Auslegung steht in Schritt ① neben der
    // Pufferverwaltung (SimulationKonfigDienste.AuslegungOeffnen), nicht mehr hier.
    // Damit fallen Vorbelegung, Flottenprobe und der Schreibweg der Betriebsoptionen
    // aus dieser Datenseite: Der Reiter „Stromspeicher" ist reines ERGEBNIS. Was er
    // zeigt, steht fertig in SpeicherErgebnisDaten (AktiveFlotte, Flottenergebnis).

    /// <summary>Schreibt den uebergebenen CSV-Text in eine Datei; ohne Delegat kein Knopf.</summary>
    public Func<string, Task<Rueckmeldung>>? OptimierungCsv;

    /// <summary>
    /// Wechselt auf Schritt ① der Ansicht „Simulation" (Auftrag <b>#274</b>) — der
    /// Verweis „Konfiguration ändern → ①" der Herkunftszeile im Reiter „Stromspeicher".
    /// </summary>
    /// <remarks>
    /// Er gilt fuer BEIDE Wirte der Ergebniskomponente: in der Ansicht SIMULATION
    /// wechselt er den Schritt, im Startseiten-Reiter „Simulation" oeffnet er die
    /// Ansicht in ①. Beides ueber denselben Maskenschluessel
    /// <c>SIMULATION_KONFIGURATION</c>. OHNE Delegat gibt es den Verweis nicht.
    /// </remarks>
    public Action? KonfigurationOeffnen;

    // ---- Die vier CSV-Exporte ----

    public Action? CsvBedarf;

    /// <summary>
    /// Der CSV-Export der Kälteseite (Stufe KU1, Kühlkonzept 9.2) — die Kältelast je Stunde mit
    /// der Grenze der Zahl in der Kopfzeile; ohne Delegat kein Knopf.
    /// </summary>
    public Action? CsvKaelte;
    public Action? CsvWaermepumpe;
    public Action? CsvHeizkessel;
    public Action? CsvSpeicher;

    /// <summary>CSV des Wärmegangs — nur die angehakten Reihen, immer chronologisch.</summary>
    public Action<int, IReadOnlyList<string>, IReadOnlyList<string>>? CsvWaermegang;

    /// <summary>CSV des Stromgangs — nur die angehakten Reihen.</summary>
    public Action<IReadOnlyList<string>>? CsvStromgang;

    /// <summary>Rechnet die Autarkie-Kacheln zu einer Was-wäre-wenn-Kapazitaet neu.</summary>
    public Func<double, AutarkieDaten>? AutarkieRechnen;
}

/// <summary>
/// Die Feldschluessel der Speicherparameter — sprachneutral und ASCII
/// (Drei-Schichten-Regel). Sie benennen das Feld, das
/// <see cref="SimulationErgebnisDienste.SpeicherfeldSchreiben"/> setzt.
///
/// <para><b>Zwei Ziele.</b> Die meisten Schluessel benennen ein Feld der aktiven
/// VARIANTE (<c>Tab_StromspeicherVariante</c>); <see cref="Kapazitaet"/> und
/// <see cref="Leistung"/> dagegen die Groesse der ANLAGE
/// (<c>Tab_Stromspeicher</c>, W11b‑B‑29). Der Unterschied ist keine Feinheit: Die
/// Geraetegroesse gehoert allen Varianten desselben Speichers gemeinsam, und die
/// Huelle schreibt sie deshalb ueber denselben Weg wie die Auslegungsoptimierung
/// (<c>StromspeicherSimCtrl.UebernehmeAuslegung</c>) samt seiner Wache "genau eine
/// SP-Anlage im Projekt".</para>
/// </summary>
public static class SpeicherFeld
{
    public const string SoCMin = "SOC_MIN";
    public const string SoCMax = "SOC_MAX";
    public const string Ladeschwelle = "LADESCHWELLE";

    /// <summary>Nennkapazitaet [kWh] — Geraetedatum der ANLAGE, nicht der Variante.</summary>
    public const string Kapazitaet = "KAPAZITAET";

    /// <summary>Lade-/Entladeleistung [kW] — Geraetedatum der ANLAGE, nicht der Variante.</summary>
    public const string Leistung = "LEISTUNG";

    public const string Betriebsart = "BETRIEBSART";
    public const string Berechnungsart = "BERECHNUNGSART";

    /// <summary>Zielschwelle P_ziel [kW] der Lastspitzenkappung (Schemaschritt 86).</summary>
    public const string PeakZiel = "PEAKZIEL";

    /// <summary>Adaptive Schwellensuche der Lastspitzenkappung (Schemaschritt 86).</summary>
    public const string PeakZielAdaptiv = "PEAKZIEL_ADAPTIV";
    public const string Kompatibilitaet = "KOMPATIBILITAET";
    public const string LadenPv = "LADEN_PV";
    public const string LadenBhkw = "LADEN_BHKW";
    public const string Netzentladung = "NETZENTLADUNG";
    public const string Kapitalzins = "KAPITALZINS";
    public const string Nutzungsdauer = "NUTZUNGSDAUER";
    public const string Leistungspreis = "LEISTUNGSPREIS";
    public const string Netzladeaufschlag = "NETZLADEAUFSCHLAG";
    public const string Preisquelle = "PREISQUELLE";
    public const string Preisreihe = "PREISREIHE";
    public const string Aufschlag = "AUFSCHLAG";
}
