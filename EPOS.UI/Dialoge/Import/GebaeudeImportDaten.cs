using EPOS.UI.Bausteine;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Import;

// =====================================================================================
//  Die DTO des Gebäudeimports (Stufe G4c, Welle 2; Softwarearchitektur 3.4, A3/E27).
//
//  Die Komponente GebaeudeImportDialog kennt keine Fachklasse des Kerns: Sie bekommt fertige
//  Anzeigetexte, Zahlen und sprachneutrale Schlüssel, und sie gibt einen Ergebnis-Record
//  zurück. Was sich je FORMAT unterscheidet — Dateifilter, Größengrenze, Schemaanzeige,
//  Zonierungsregeln, Hilfeschlüssel —, steht als DATEN in GebaeudeImportProfilDaten; ein
//  Formatname kommt in dieser Datei und in der Komponente nicht vor (Wache in den Tests).
// =====================================================================================

/// <summary>
/// Was den Import je Format unterscheidet — als Daten aus dem Profil des Kerns
/// (Softwarearchitektur 1.5, Regel 3). Alle Texte sind fertige Anzeigetexte.
/// </summary>
/// <param name="Formatname">Anzeigename des Formats für den Dialogkopf.</param>
/// <param name="Dateifilter">Filter des Dateiwählers in der Schreibweise von <c>IDateiDienst</c>.</param>
/// <param name="Groessengrenze">Die Größengrenze der Plattform als Anzeigetext („25 MB"); leer = keine Angabe.</param>
/// <param name="Zonierungsregeln">Die wählbaren Zonierungsregeln als Anzeigetexte; die erste gilt.</param>
/// <param name="HilfeSchluessel">Bereichsschlüssel des Infoknopfs.</param>
public sealed record GebaeudeImportProfilDaten(
    string Formatname,
    string Dateifilter,
    string Groessengrenze,
    IReadOnlyList<string> Zonierungsregeln,
    string HilfeSchluessel);

/// <summary>
/// Die Antwort des Dateiwählers der Hülle — Pfad und Größe, oder die BENANNTE Ablehnung vor
/// dem Lesen (Größe über der Grenze des Profils, Softwarearchitektur 1.5 Regel 2). <c>null</c>
/// statt eines Satzes heißt „abgebrochen".
/// </summary>
/// <param name="Pfad">Der gewählte Pfad — nur für den Lesedelegaten, nie zur Anzeige.</param>
/// <param name="Dateiname">Der Dateiname ohne Pfad — für die Anzeige.</param>
/// <param name="Groesse">Größe in Byte.</param>
/// <param name="Ablehnung">Der Text der Ablehnung; <c>null</c> = die Datei darf gelesen werden.</param>
public sealed record GebaeudeDateiwahl(string Pfad, string Dateiname, long Groesse, string? Ablehnung = null);

/// <summary>Ein Fortschrittsschritt des Lesens, schon übersetzt.</summary>
/// <param name="Anteil">0 … 1; <c>null</c> = unbestimmt.</param>
/// <param name="Text">Anzeigetext.</param>
public readonly record struct GebaeudeImportFortschritt(double? Anteil, string Text);

/// <summary>Eine Meldung des Imports, schon übersetzt.</summary>
/// <param name="Stufe">Die Dringlichkeit — Fehler sperren die Übernahme.</param>
/// <param name="Stufentext">Anzeigetext der Stufe.</param>
/// <param name="Text">Anzeigetext der Meldung.</param>
/// <param name="Kennung">Sprachneutraler Meldungsschlüssel des Kerns; <c>null</c> = keiner.</param>
public sealed record GebaeudeImportMeldung(WarnStufe Stufe, string Stufentext, string Text, string? Kennung = null);

/// <summary>Der Kopf des Dialogs nach dem Lesen: Datei, Format, Schema, Größe, Zonenregel — Anzeigetexte.</summary>
public sealed record GebaeudeImportKopf(string Dateiname, string Format, string Schema, string Groesse, string Zonenregel);

/// <summary>
/// Was das Lesen ergeben hat. Nicht gelesen: <see cref="Meldungen"/> nennen den Grund (Lesefehler,
/// kein Gebäude); gelesen: der Kopf und die Gebäude der Datei für die Klappliste (U13: eines je Lauf).
/// </summary>
public sealed record GebaeudeLesestand(
    bool Gelesen,
    GebaeudeImportKopf? Kopf,
    IReadOnlyList<string> Gebaeude,
    IReadOnlyList<GebaeudeImportMeldung> Meldungen);

/// <summary>
/// Eine Zuordnung, wie der Dialog sie erfragt: welches Gebäude, welche Baualtersklasse
/// (Index 0 = A … 20 = U, <c>null</c> = keine) und welche Räume der Anwender gegen die Datei
/// umgestellt hat (Raumkennung → beheizt).
/// </summary>
public sealed record GebaeudeZuordnungsanfrage(
    int Gebaeudeindex,
    int? Baualtersklasse,
    IReadOnlyDictionary<string, bool> BeheiztUebersteuert);

/// <summary>Ein Raum der Raumliste mit dem Haken „beheizt" und dem Grund der Entscheidung.</summary>
/// <param name="Kennung">Raumkennung der Datei — der Schlüssel der Übersteuerung.</param>
/// <param name="Name">Anzeigename (Name, sonst Kennung).</param>
/// <param name="Flaeche">Fläche als Anzeigetext mit Einheit.</param>
/// <param name="Beheizt">Wirksam beheizt — mit der Übersteuerung.</param>
/// <param name="BeheiztLautDatei">Was die Datei sagt.</param>
/// <param name="Grund">Der Grund als Anzeigetext.</param>
/// <param name="Uebersteuert">Hat der Anwender umgestellt?</param>
public sealed record GebaeudeRaumzeileDaten(
    string Kennung, string Name, string Flaeche, bool Beheizt, bool BeheiztLautDatei, string Grund, bool Uebersteuert);

/// <summary>Die Markierung einer Zeile: gelb = auffällig, rot = Fehler (sperrt mit Haken).</summary>
public enum GebaeudeZeilenmarkierung
{
    /// <summary>Unauffällig.</summary>
    Keine,
    /// <summary>Auffällig — bitte ansehen.</summary>
    Gelb,
    /// <summary>Fehlerhaft — mit Haken sperrt die Zeile die Übernahme.</summary>
    Rot,
}

/// <summary>
/// Die sprachneutralen Herkunftsschlüssel, die die Komponente selbst setzt oder liest; alle
/// übrigen reicht die Hülle durch (sie sind zugleich die Stilklassen
/// <c>epos-gebimport-herkunft--&lt;schlüssel&gt;</c>).
/// </summary>
public static class GebaeudeHerkunftSchluessel
{
    /// <summary>Vom Anwender im Dialog geändert.</summary>
    public const string Manuell = "MANUELL";

    /// <summary>Kein Wert, keine Vorgabe.</summary>
    public const string Leer = "LEER";
}

/// <summary>
/// <b>Eine Zeile der Zuordnung — je Zielfeld eine</b> (Datenaustauschkonzept 2.4): Gruppe,
/// Feld, Wert, Einheit, Beleg, Vorgabe, Herkunft und Haken, dazu die Markierung und was der
/// Anwender an der Zeile ändern darf. Unveränderlich; der Dialog ersetzt die Zeile mit
/// <c>with</c>.
/// </summary>
public sealed record GebaeudeFeldzeileDaten
{
    /// <summary>Sprachneutraler Schlüssel des Zielfelds — die Identität der Zeile.</summary>
    public string Zielfeld { get; init; } = "";

    /// <summary>Anzeigetext der Gruppe.</summary>
    public string Gruppe { get; init; } = "";

    /// <summary>Anzeigetext des Felds.</summary>
    public string Feld { get; init; } = "";

    /// <summary>Der Zahlenwert in der <see cref="Einheit"/>; <c>null</c> = leer.</summary>
    public double? Wert { get; init; }

    /// <summary>Der Steuerwert eines Aufzählungsfelds (Baualtersklasse, Bauart, Randbedingung); <c>null</c> bei Zahlenfeldern.</summary>
    public string? Textwert { get; init; }

    /// <summary>Der Wert als Anzeigetext (für Zeilen, die nicht eingebbar sind).</summary>
    public string WertText { get; init; } = "";

    /// <summary>Einheitenzeichen; leer bei Aufzählungsfeldern.</summary>
    public string Einheit { get; init; } = "";

    /// <summary>Woraus der Wert stammt, als Anzeigetext; leer = kein Beleg.</summary>
    public string Beleg { get; init; } = "";

    /// <summary>Der Vorgabewert der Baualtersklasse als Anzeigetext; leer = keiner.</summary>
    public string Vorgabe { get; init; } = "";

    /// <summary>Woraus die Vorgabe stammt (Klasse, Katalogsätze), als Anzeigetext.</summary>
    public string VorgabeBeleg { get; init; } = "";

    /// <summary>Die Herkunft als Anzeigetext — aus den Gaben, nie aus der Komponente.</summary>
    public string HerkunftText { get; init; } = "";

    /// <summary>Die Herkunft als sprachneutraler Schlüssel (Stilklasse, Rückweg).</summary>
    public string HerkunftSchluessel { get; init; } = GebaeudeHerkunftSchluessel.Leer;

    /// <summary>Wird die Zeile übernommen?</summary>
    public bool Haken { get; init; }

    /// <summary>Gelb oder rot markiert?</summary>
    public GebaeudeZeilenmarkierung Markierung { get; init; }

    /// <summary>Darf der Anwender den Wert ändern?</summary>
    public bool Eingebbar { get; init; }

    /// <summary>Darf der Anwender den Haken setzen?</summary>
    public bool HakenSetzbar { get; init; }
}

/// <summary>
/// Der Stand einer Zuordnung, wie ihn die Hülle aus dem Kern baut: Kopfzeile, Namensvorschlag,
/// Raumliste, Zeilen, Meldungen — und der Herkunftstext für eine Handänderung, damit auch
/// „manuell" aus den Gaben kommt.
/// </summary>
public sealed record GebaeudeImportStand
{
    /// <summary>Die Bilanzzeile des Kopfs (Datei, Gebäude, Klasse, Zahl der Werte je Herkunft).</summary>
    public string Kopftext { get; init; } = "";

    /// <summary>Der Name des neuen Gebäudes aus der Datei.</summary>
    public string Vorschlagsname { get; init; } = "";

    /// <summary>Die Räume des Gebäudes in Dateireihenfolge.</summary>
    public IReadOnlyList<GebaeudeRaumzeileDaten> Raeume { get; init; } = Array.Empty<GebaeudeRaumzeileDaten>();

    /// <summary>Je Zielfeld eine Zeile.</summary>
    public IReadOnlyList<GebaeudeFeldzeileDaten> Zeilen { get; init; } = Array.Empty<GebaeudeFeldzeileDaten>();

    /// <summary>Lese- und Zuordnungsmeldungen.</summary>
    public IReadOnlyList<GebaeudeImportMeldung> Meldungen { get; init; } = Array.Empty<GebaeudeImportMeldung>();

    /// <summary>Der Herkunftstext einer Handänderung („Manuell").</summary>
    public string ManuellHerkunftText { get; init; } = "";
}

/// <summary>
/// <b>Das Ergebnis des Dialogs</b> — ALLE Zeilen, auch die unveränderten
/// (Datenaustauschkonzept 2.4), mit Wert, Herkunftsschlüssel und Haken; dazu Gebäude,
/// Baualtersklasse, Name des neuen Gebäudes und die umgestellten Räume. Abbrechen liefert
/// <c>null</c>.
/// </summary>
/// <param name="Gebaeudeindex">Das gewählte Gebäude der Datei.</param>
/// <param name="Baualtersklasse">Index 0 = A … 20 = U; <c>null</c> = keine.</param>
/// <param name="Gebaeudename">Der Name, unter dem das Gebäude angelegt würde.</param>
/// <param name="BeheiztUebersteuert">Raumkennung → beheizt, nur die Abweichungen von der Datei.</param>
/// <param name="Zeilen">Alle Zeilen der Zuordnung.</param>
public sealed record GebaeudeImportErgebnis(
    int Gebaeudeindex,
    int? Baualtersklasse,
    string Gebaeudename,
    IReadOnlyDictionary<string, bool> BeheiztUebersteuert,
    IReadOnlyList<GebaeudeFeldzeileDaten> Zeilen)
{
    /// <summary>Die Zeile zu einem Zielfeld; <c>null</c>, wenn es sie nicht gibt.</summary>
    public GebaeudeFeldzeileDaten? Zeile(string zielfeld)
        => Zeilen.FirstOrDefault(z => string.Equals(z.Zielfeld, zielfeld, StringComparison.Ordinal));
}

/// <summary>
/// <b>Das Textbündel des Gebäudeimports</b> (Hausregel ab etwa zehn Texten). Beschriftungen,
/// kein Zustand; je Eigenschaft der Ressourcenschlüssel im Kommentar, der deutsche Rückfall
/// ist der Ressourcentext selbst.
/// </summary>
public sealed class GebaeudeImportTexte
{
    /// <summary>GIMP_DLG_TITEL</summary>
    public string Titel { get; set; } = Resource.GIMP_DLG_TITEL;

    /// <summary>GIMP_FELD_BAUALTERSKLASSE</summary>
    public string Baualtersklasse { get; set; } = Resource.GIMP_FELD_BAUALTERSKLASSE;

    /// <summary>GIMP_DLG_KLASSE_KEINE</summary>
    public string KlasseKeine { get; set; } = Resource.GIMP_DLG_KLASSE_KEINE;

    /// <summary>GIMP_DLG_KLASSE_HINWEIS</summary>
    public string KlasseHinweis { get; set; } = Resource.GIMP_DLG_KLASSE_HINWEIS;

    /// <summary>GIMP_DLG_DATEI</summary>
    public string Datei { get; set; } = Resource.GIMP_DLG_DATEI;

    /// <summary>GIMP_DLG_DATEI_KNOPF</summary>
    public string DateiKnopf { get; set; } = Resource.GIMP_DLG_DATEI_KNOPF;

    /// <summary>GIMP_DLG_GRENZE — Platzhalter {0} = Größengrenze des Profils.</summary>
    public string Grenze { get; set; } = Resource.GIMP_DLG_GRENZE;

    /// <summary>IMPORT_BTN_ABBRECHEN — bricht den laufenden Lesegang ab.</summary>
    public string LaufAbbrechen { get; set; } = Resource.IMPORT_BTN_ABBRECHEN;

    /// <summary>GIMP_DLG_NICHT_GELESEN</summary>
    public string NichtGelesen { get; set; } = Resource.GIMP_DLG_NICHT_GELESEN;

    /// <summary>GIMP_DLG_LEER</summary>
    public string Leer { get; set; } = Resource.GIMP_DLG_LEER;

    /// <summary>GIMP_DLG_GRP_QUELLE</summary>
    public string GruppeQuelle { get; set; } = Resource.GIMP_DLG_GRP_QUELLE;

    /// <summary>GIMP_DLG_KOPF_DATEI</summary>
    public string KopfDatei { get; set; } = Resource.GIMP_DLG_KOPF_DATEI;

    /// <summary>GIMP_DLG_KOPF_FORMAT</summary>
    public string KopfFormat { get; set; } = Resource.GIMP_DLG_KOPF_FORMAT;

    /// <summary>GIMP_DLG_KOPF_SCHEMA</summary>
    public string KopfSchema { get; set; } = Resource.GIMP_DLG_KOPF_SCHEMA;

    /// <summary>GIMP_DLG_KOPF_GROESSE</summary>
    public string KopfGroesse { get; set; } = Resource.GIMP_DLG_KOPF_GROESSE;

    /// <summary>GIMP_DLG_KOPF_ZONENREGEL</summary>
    public string KopfZonenregel { get; set; } = Resource.GIMP_DLG_KOPF_ZONENREGEL;

    /// <summary>GIMP_DLG_GEBAEUDE</summary>
    public string Gebaeude { get; set; } = Resource.GIMP_DLG_GEBAEUDE;

    /// <summary>GIMP_DLG_NAME</summary>
    public string Name { get; set; } = Resource.GIMP_DLG_NAME;

    /// <summary>GIMP_DLG_GRP_RAEUME</summary>
    public string GruppeRaeume { get; set; } = Resource.GIMP_DLG_GRP_RAEUME;

    /// <summary>GIMP_DLG_SP_RAUM</summary>
    public string SpalteRaum { get; set; } = Resource.GIMP_DLG_SP_RAUM;

    /// <summary>GIMP_DLG_SP_FLAECHE</summary>
    public string SpalteFlaeche { get; set; } = Resource.GIMP_DLG_SP_FLAECHE;

    /// <summary>GIMP_DLG_SP_BEHEIZT</summary>
    public string SpalteBeheizt { get; set; } = Resource.GIMP_DLG_SP_BEHEIZT;

    /// <summary>GIMP_DLG_SP_GRUND</summary>
    public string SpalteGrund { get; set; } = Resource.GIMP_DLG_SP_GRUND;

    /// <summary>GIMP_DLG_BEHEIZT — Beschriftung des Hakens je Raum für die Sprachausgabe, {0} = Raum.</summary>
    public string BeheiztTitel { get; set; } = Resource.GIMP_DLG_BEHEIZT;

    /// <summary>GIMP_DLG_RAEUME_HINWEIS</summary>
    public string RaeumeHinweis { get; set; } = Resource.GIMP_DLG_RAEUME_HINWEIS;

    /// <summary>GIMP_DLG_GRP_ZEILEN</summary>
    public string GruppeZeilen { get; set; } = Resource.GIMP_DLG_GRP_ZEILEN;

    /// <summary>GIMP_DLG_SP_GRUPPE</summary>
    public string SpalteGruppe { get; set; } = Resource.GIMP_DLG_SP_GRUPPE;

    /// <summary>GIMP_DLG_SP_FELD</summary>
    public string SpalteFeld { get; set; } = Resource.GIMP_DLG_SP_FELD;

    /// <summary>GIMP_DLG_SP_WERT</summary>
    public string SpalteWert { get; set; } = Resource.GIMP_DLG_SP_WERT;

    /// <summary>GIMP_DLG_SP_EINHEIT</summary>
    public string SpalteEinheit { get; set; } = Resource.GIMP_DLG_SP_EINHEIT;

    /// <summary>GIMP_DLG_SP_BELEG</summary>
    public string SpalteBeleg { get; set; } = Resource.GIMP_DLG_SP_BELEG;

    /// <summary>GIMP_DLG_SP_VORGABE</summary>
    public string SpalteVorgabe { get; set; } = Resource.GIMP_DLG_SP_VORGABE;

    /// <summary>GIMP_DLG_SP_HERKUNFT</summary>
    public string SpalteHerkunft { get; set; } = Resource.GIMP_DLG_SP_HERKUNFT;

    /// <summary>GIMP_DLG_SP_UEBERNEHMEN</summary>
    public string SpalteUebernehmen { get; set; } = Resource.GIMP_DLG_SP_UEBERNEHMEN;

    /// <summary>GIMP_DLG_HAKEN — Beschriftung des Hakens je Zeile für die Sprachausgabe, {0} = Feld.</summary>
    public string HakenTitel { get; set; } = Resource.GIMP_DLG_HAKEN;

    /// <summary>GIMP_DLG_ZEILEN_HINWEIS</summary>
    public string ZeilenHinweis { get; set; } = Resource.GIMP_DLG_ZEILEN_HINWEIS;

    /// <summary>GIMP_DLG_GRP_MELDUNGEN</summary>
    public string GruppeMeldungen { get; set; } = Resource.GIMP_DLG_GRP_MELDUNGEN;

    /// <summary>GIMP_DLG_SP_STUFE</summary>
    public string SpalteStufe { get; set; } = Resource.GIMP_DLG_SP_STUFE;

    /// <summary>GIMP_DLG_SP_MELDUNG</summary>
    public string SpalteMeldung { get; set; } = Resource.GIMP_DLG_SP_MELDUNG;

    /// <summary>GIMP_DLG_KEINE_MELDUNGEN</summary>
    public string KeineMeldungen { get; set; } = Resource.GIMP_DLG_KEINE_MELDUNGEN;

    /// <summary>ALLG_BTN_OK</summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary>ALLG_BTN_ABBRECHEN</summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;

    /// <summary>GIMP_DLG_OK_OHNE_DATEI — Grund der weichen Sperre vor dem Lesen.</summary>
    public string OkOhneDatei { get; set; } = Resource.GIMP_DLG_OK_OHNE_DATEI;

    /// <summary>GIMP_DLG_OK_OHNE_WEG — Grund der weichen Sperre ohne Schreibdelegat.</summary>
    public string OkOhneWeg { get; set; } = Resource.GIMP_DLG_OK_OHNE_WEG;

    /// <summary>GIMP_DLG_GESPERRT — Einleitung der Fehler, die die Übernahme sperren.</summary>
    public string Gesperrt { get; set; } = Resource.GIMP_DLG_GESPERRT;
}
