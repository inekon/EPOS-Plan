using System;
using System.Collections.Generic;
using EPOS.UI.Bausteine;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// EINE Zeile der Erzeugerspalte: die Kachel und alles, was die Seite ueber sie
/// wissen muss, um ihre Ereignisse zu verteilen (iU9-W10b.1).
///
/// <para>Der Baustein <see cref="ErzeugerKachel"/> bleibt damit allgemein — er kennt
/// weder Kaskadenplaetze noch Anlagen-IDs.</para>
/// </summary>
public sealed class ErzeugerZeile
{
    /// <summary>Der Inhalt der Kachel.</summary>
    public ErzeugerKachelDaten Kachel = new ErzeugerKachelDaten();

    /// <summary>
    /// Der Steuerwert der Technologie (<c>DbWerte.ERZEUGER_*</c>) — der
    /// Kaskadenplatz bzw. der Stromplatz, den ▲▼, × und „+ aufnehmen" bewegen.
    /// </summary>
    public string DbWert = "";

    /// <summary><c>Tab_Energieanlagen.ID</c>; 0 = die Zeile hat keine Anlage.</summary>
    public int IdAnlage;

    /// <summary><c>Tab_Energieanlagen.ID_Type</c> — auch die Gruppenkennung der Stromkarten.</summary>
    public int IdType;

    /// <summary>true = Stromerzeuger bzw. Energiespeicher (<c>Tool_5</c>/<c>Tool_6</c>).</summary>
    public bool IstStrom;

    /// <summary>Der Platz der Stromseite: 5 oder 6; 0 bei den Waermeerzeugern.</summary>
    public int StromPlatz;

    /// <summary>true = nicht aufgenommen (die gestrichelte Karte).</summary>
    public bool Verfuegbar;

    /// <summary>Bezeichner der Anlage — er steht in den Meldungen der Vorpruefungen.</summary>
    public string Bezeichner = "";

    /// <summary>true = Waermepumpe (Modus und WP-Prioritaet sind nur dort wirksam).</summary>
    public bool IstWaermepumpe;

    /// <summary>true = die Erzeugerart kennt eine waehlbare Waermequelle (WP, Kessel).</summary>
    public bool QuellenwahlMoeglich;

    /// <summary>
    /// true = eine Luft-Wasser-Waermepumpe (oder eine mit ungepflegter Bauart): Ihre
    /// Quelle IST die Aussenluft, die Wahl ist gesperrt (PAKET Q1).
    /// </summary>
    public bool BauartGebunden;

    /// <summary>Die Bauart als Anzeigetext — sie steht in der Sperrmeldung.</summary>
    public string WpTypAnzeige = "";

    /// <summary>Die aktuelle WP-Prioritaet (Vorgabe der Zahlenabfrage).</summary>
    public int Prioritaet;
}

/// <summary>Eine Gruppe der Erzeugerspalte: Kopf, Zeilen und der Text fuer „leer".</summary>
public sealed class KachelGruppe
{
    public string Titel = "";
    public IReadOnlyList<ErzeugerZeile> Zeilen = new List<ErzeugerZeile>();

    /// <summary>Text, wenn die Gruppe nichts Aufgenommenes zeigt; leer = kein Hinweis.</summary>
    public string LeerText = "";
}

/// <summary>
/// Der vollstaendige Stand EINER Auffrischung der Simulationskonfiguration —
/// das, was der neunfache <c>AktualisiereErzeugerUebersicht</c> des Vorlaeufers
/// zusammengetragen hat (iU9-W10b.1).
/// </summary>
public sealed class SimulationKonfigDaten
{
    public int IdProjekt;

    /// <summary>
    /// Die Schema-Migration ist nicht durchgekommen (ADR-001): Es wird gemeldet und
    /// alles gesperrt — die Seite bleibt schliessbar.
    /// </summary>
    public bool Gesperrt;

    public string Sperrgrund = "";

    /// <summary>Waermeerzeuger, Stromerzeuger, Energiespeicher — in dieser Reihenfolge.</summary>
    public IReadOnlyList<KachelGruppe> Gruppen = new List<KachelGruppe>();

    /// <summary>Die Speicherspalte in der Reihenfolge von <c>ProjektPufferListe</c>.</summary>
    public IReadOnlyList<SpeicherKachelDaten> Speicher = new List<SpeicherKachelDaten>();

    /// <summary>Text, wenn kein Speicher da ist (mit bzw. ohne Projekt).</summary>
    public string SpeicherLeerText = "";

    /// <summary>Der Schalter „Extrapolation der WP-Kennlinie erlauben".</summary>
    public bool ExtrapolationErlaubt = true;

    /// <summary>Ohne Projekt ist er gesperrt — die Vorbelegung bleibt trotzdem „an".</summary>
    public bool ExtrapolationMoeglich;

    /// <summary>
    /// Der Booster-Lesepunkt erscheint erst, wenn das Projekt einen gekoppelten
    /// Booster fuehrt (PAKET B2).
    /// </summary>
    public bool BoosterSichtbar;

    /// <summary>true = „Stundenanfang (konservativ)".</summary>
    public bool BoosterDavor = true;

    /// <summary>true = im Projekt ist eine Photovoltaik aufgenommen (PV-Hinweis des Modus).</summary>
    public bool PvGewaehlt;
}

/// <summary>
/// Das Ergebnis eines Schreibwegs, den die Seite anstoesst: gelungen oder nicht,
/// dazu der Text fuer die Statuszeile (iU9-W10b.1).
/// </summary>
public sealed record Rueckmeldung(bool Erfolg, string Text)
{
    /// <summary>Nichts zu melden — der Weg wurde abgebrochen.</summary>
    public static readonly Rueckmeldung Still = new Rueckmeldung(true, "");
}

/// <summary>
/// Die Datenseite der Simulationskonfiguration — die Huelle legt sie ein
/// (iU9-W10b.1). Ohne Delegat geschieht an der Stelle nichts.
/// </summary>
public sealed class SimulationKonfigDienste
{
    /// <summary>Traegt alles zusammen, was die Seite zeigt. Nie <c>null</c>.</summary>
    public Func<int, SimulationKonfigDaten>? Laden;

    /// <summary>
    /// Das ANGEORDNETE Schema — nur gerufen, wenn das Schemablatt vorn steht.
    /// Der Vorlaeufer rechnete es aus demselben Grund nur bei sichtbarer Ansicht.
    /// </summary>
    public Func<int, SchemaBild>? SchemaLaden;

    /// <summary>Verschiebt einen Erzeuger in der Kaskade (−1 vor, +1 zurueck).</summary>
    public Action<string, int>? Verschieben;

    /// <summary>Nimmt einen Waermeerzeuger in die Simulation auf.</summary>
    public Action<string>? Aufnehmen;

    /// <summary>Nimmt einen Waermeerzeuger aus der Simulation.</summary>
    public Action<string>? Entfernen;

    /// <summary>Setzt den Stromplatz (5 oder 6); leerer Wert = nicht aufnehmen.</summary>
    public Action<int, string>? StromAuswahl;

    /// <summary>Schreibt <c>Tool_1..6</c> weg; <c>false</c> = fehlgeschlagen.</summary>
    public Func<bool>? Speichern;

    /// <summary>Schreibt die Extrapolationseinstellung SOFORT; <c>false</c> = fehlgeschlagen.</summary>
    public Func<bool, bool>? ExtrapolationSchreiben;

    /// <summary>Schreibt den Booster-Lesepunkt SOFORT; <c>false</c> = fehlgeschlagen.</summary>
    public Func<bool, bool>? LesepunktSchreiben;

    // =====================================================================
    // Die Editoren — Ebene 1 der Ueberlagerungen
    // =====================================================================

    /// <summary>Parametersatz des Betriebsmodus-Dialogs zu einer Anlage.</summary>
    public Func<int, IReadOnlyDictionary<string, object>>? BetriebsmodusGaben;

    /// <summary>Schreibt den gewaehlten Betriebsmodus.</summary>
    public Action<int, string>? BetriebsmodusSchreiben;

    /// <summary>Schreibt die WP-Prioritaet (der Aufrufer laesst nur Werte &gt; 0 durch).</summary>
    public Action<int, int>? PrioritaetSchreiben;

    /// <summary>Die waehlbaren Quellentypen einer Anlage: Steuerwert und Anzeigetext.</summary>
    public Func<int, IReadOnlyList<Quellentyp>>? Quellentypen;

    /// <summary>Der aktuell gespeicherte Quellentyp einer Anlage (Vorauswahl).</summary>
    public Func<int, string>? QuelleTyp;

    /// <summary>Die aktuelle konstante Quelltemperatur (Vorgabe der Zahlenabfrage).</summary>
    public Func<int, double>? QuelleTemperatur;

    /// <summary>
    /// Ein Quellenzweig OHNE eigenen Unterdialog: „Systemruecklauf", „Aussenluft",
    /// „konstant" (dort traegt der dritte Parameter die Zahl).
    /// </summary>
    public Action<int, string, double>? QuelleEinfachSchreiben;

    /// <summary>Parametersatz des Quellendialogs „Pufferspeicher".</summary>
    public Func<int, IReadOnlyDictionary<string, object>>? QuellePufferGaben;

    /// <summary>
    /// Prueft (Kurzschluss, Kaskadenzyklus) und schreibt den Puffer-Zweig; die
    /// Rueckmeldung traegt bei einer Abweisung deren Wortlaut.
    /// </summary>
    public Func<int, EPOS.UI.Dialoge.Simulation.QuellePufferspeicherDaten, Rueckmeldung>? QuellePufferSchreiben;

    /// <summary>Parametersatz des Quellprofil-Dialogs.</summary>
    public Func<int, IReadOnlyDictionary<string, object>>? QuellprofilGaben;

    /// <summary>Schreibt den Fremdschluessel des Quellprofils.</summary>
    public Action<int, int>? QuellprofilSchreiben;

    /// <summary>Parametersatz des Erdreich-Dialogs.</summary>
    public Func<int, IReadOnlyDictionary<string, object>>? QuelleErdreichGaben;

    /// <summary>Schreibt Klimazone und die sieben Erdreichfelder.</summary>
    public Action<int, EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten>? QuelleErdreichSchreiben;

    /// <summary>
    /// Der CSV-Zweig: Datei waehlen, das Profil pruefen, den Pfad schreiben. Die
    /// Rueckmeldung traegt bei einem unlesbaren Profil dessen Meldung.
    /// </summary>
    public Func<int, System.Threading.Tasks.Task<Rueckmeldung>>? QuelleCsvWaehlen;

    /// <summary>Parametersatz des Senkendialogs.</summary>
    public Func<int, IReadOnlyDictionary<string, object>>? WaermesenkeGaben;

    /// <summary>
    /// Nimmt das Ergebnis des Senkendialogs entgegen (er SPEICHERT selbst) und
    /// liefert den Text der Statuszeile.
    /// </summary>
    public Func<int, EPOS.UI.Dialoge.Simulation.WaermesenkeErgebnis?, Rueckmeldung>? WaermesenkeFertig;

    /// <summary>Parametersatz der Pufferverwaltung (Puffer-ID; 0 = ohne Vorwahl).</summary>
    public Func<int, IReadOnlyDictionary<string, object>>? PufferVerwaltungGaben;
}

/// <summary>Ein waehlbarer Quellentyp: Steuerwert und Anzeigetext.</summary>
public sealed record Quellentyp(string Wert, string Text);
