using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
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
/// <param name="Quellen">
/// Die Einträge der Quellenwahl (je Format, Format mit Projektdatei, nur Projektdatei) mit dem Filter des Dateiwählers;
/// <c>null</c> oder leer = keine Wahl (festes Profil) — dann gilt <see cref="Dateifilter"/>.
/// </param>
public sealed record GebaeudeImportProfilDaten(
    string Formatname,
    string Dateifilter,
    string Groessengrenze,
    IReadOnlyList<string> Zonierungsregeln,
    string HilfeSchluessel,
    IReadOnlyList<GebaeudeImportQuellwahl>? Quellen = null);

/// <summary>
/// Der <b>Weg</b> eines Eintrags der Quellenwahl bzw. des gelesenen Laufs (Datenaustauschkonzept 16.1): die Gebäudedatei
/// allein, die Gebäudedatei mit danach dazugeladener Projektdatei, oder allein die Projektdatei.
/// </summary>
public enum GebaeudeImportWeg
{
    /// <summary>Die Gebäudedatei allein.</summary>
    Datei,

    /// <summary>Die Gebäudedatei lesen, danach die Projektdatei dazuladen.</summary>
    MitProjektdatei,

    /// <summary>Allein die Projektdatei (<c>.sqproj</c>), ohne Gebäudedatei.</summary>
    NurProjektdatei,
}

/// <summary>
/// Ein Eintrag der <b>Quellenwahl</b> des Gebäudeimports: ein stabiler Schlüssel der Datenseite, der Anzeigetext, der
/// Filter des Dateiwählers in der Schreibweise von <c>IDateiDienst</c> und der Weg, den der Dialog daraus macht.
/// </summary>
public sealed record GebaeudeImportQuellwahl(string Schluessel, string Text, string Dateifilter,
                                             GebaeudeImportWeg Weg = GebaeudeImportWeg.Datei);

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

/// <summary>
/// Der Kopf des Dialogs nach dem Lesen: Datei, Format, Schema, Größe, Zonenregel — Anzeigetexte; dazu der Weg des Laufs,
/// an dem der Dialog „nur Projektdatei“ erkennt (<see cref="GebaeudeImportWeg.NurProjektdatei"/>).
/// </summary>
public sealed record GebaeudeImportKopf(string Dateiname, string Format, string Schema, string Groesse, string Zonenregel,
                                        GebaeudeImportWeg Weg = GebaeudeImportWeg.Datei);

/// <summary>
/// Was das Lesen ergeben hat. Nicht gelesen: <see cref="Meldungen"/> nennen den Grund (Lesefehler,
/// kein Gebäude); gelesen: der Kopf und die Gebäude der Datei für die Klappliste (U13: eines je Lauf).
/// </summary>
/// <param name="SchonImportiert">
/// Der leise Hinweis, dass ein Gebäude des Projekts schon aus derselben Datei stammt (Name und
/// Zeitpunkt, fertiger Anzeigetext); leer = keiner. Er sperrt nichts.
/// </param>
public sealed record GebaeudeLesestand(
    bool Gelesen,
    GebaeudeImportKopf? Kopf,
    IReadOnlyList<string> Gebaeude,
    IReadOnlyList<GebaeudeImportMeldung> Meldungen,
    string SchonImportiert = "");

/// <summary>
/// Eine Zuordnung, wie der Dialog sie erfragt: welches Gebäude, welche Baualtersklasse
/// (Index 0 = A … 12 = M, <c>null</c> = keine), welche Räume der Anwender gegen die Datei
/// umgestellt hat (Raumkennung → beheizt) und welche Werte er von Hand eingetragen hat
/// (Zielfeld → Wert). Die Handwerte legt die Datenseite auf den Satz und zieht die Vorgaben
/// nach, die von ihnen abhängen (innere Gewinne von der Nutzfläche, Nachtsollwert vom Tag).
/// </summary>
/// <param name="Baustoffzuordnungen">
/// Die Zuordnungen des Abschnitts „Baustoffe", noch nicht gespeichert: Schlüssel eines
/// Materialnamens (<see cref="GebaeudeMaterialzeileDaten.Schluessel"/>) → Katalogbaustoff;
/// <c>null</c> als Wert = die gemerkte Zuordnung des Projekts entfernen. Die Datenseite legt sie
/// über die gemerkten Zuordnungen und bildet den Vorschlag damit neu.
/// </param>
/// <param name="Zonenregel">
/// Die gewählte Zonenregel als sprachneutraler Schlüssel (<see cref="GebaeudeZonenregelDaten.Schluessel"/>);
/// <c>null</c> = die Vorgabe der Datei (je Geschoss, sonst eine Zone). Eine Regel, die das Gebäude nicht
/// trägt, ersetzt die Datenseite durch die Vorgabe.
/// </param>
/// <param name="Umhaengungen">
/// Die Zuordnungen von Hand in ihrer Reihenfolge (Grundriss oder Zonenliste): Raum → Zone, ohne Zielzone
/// als eigene Zone; <c>null</c> = keine. Die Datenseite legt sie auf den Vorschlag der Regel; eine andere
/// Regel verwirft sie (der Dialog fragt vorher).
/// </param>
/// <param name="RaumtemperaturAlsSollwert">
/// Der Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen" (Schlüssel
/// <see cref="GebaeudeImportSchalter.RAUMTEMPERATUR_ALS_SOLLWERT"/>); Vorgabe aus = die Normtemperatur.
/// </param>
/// <param name="Planschritte">
/// Die Schritte am Zonenplan (Zonenbaum) in ihrer Reihenfolge; <c>null</c> = keine — dann gilt der Regelvorschlag.
/// </param>
/// <param name="Plangrundhaken">
/// Die Haken, mit denen der Plan vor dem ersten Schritt gebildet war — damit die Schlüssel der Zonen stehen bleiben,
/// wenn ein Haken danach wechselt (der Wechsel kommt als Schritt <see cref="GebaeudePlanschrittArt.HAKEN"/>).
/// </param>
public sealed record GebaeudeZuordnungsanfrage(
    int Gebaeudeindex,
    int? Baualtersklasse,
    IReadOnlyDictionary<string, bool> BeheiztUebersteuert,
    IReadOnlyDictionary<string, double?>? Handwerte = null,
    IReadOnlyDictionary<string, int?>? Baustoffzuordnungen = null,
    string? Zonenregel = null,
    IReadOnlyList<GebaeudeRaumumhaengung>? Umhaengungen = null,
    bool RaumtemperaturAlsSollwert = false,
    IReadOnlyList<GebaeudePlanschritt>? Planschritte = null,
    IReadOnlyDictionary<string, bool>? Plangrundhaken = null,
    IReadOnlyDictionary<string, string>? Typwahl = null)
{
    /// <summary>Dieselbe Anfrage mit einem Schritt am Zonenplan hinter den bisherigen (Zonenbaum).</summary>
    /// <param name="schritt">Der Schritt — nur Schlüssel und Kennungen, nie ein Anzeigetext.</param>
    public GebaeudeZuordnungsanfrage MitPlanschritt(GebaeudePlanschritt schritt)
        => this with
        {
            Planschritte = (Planschritte ?? Array.Empty<GebaeudePlanschritt>()).Append(schritt).ToList(),
            Plangrundhaken = Plangrundhaken ?? BeheiztUebersteuert,
        };

    /// <summary>
    /// Dieselbe Anfrage mit einer Zuordnung von Hand hinter den bisherigen — der Weg eines Klicks im
    /// Grundriss: Raumkennung und Zonenschlüssel, nie ein Name.
    /// </summary>
    /// <param name="raum">Die Kennung des Raums.</param>
    /// <param name="zielzone">Der Schlüssel der Zielzone; <c>null</c> = als eigene Zone abtrennen.</param>
    public GebaeudeZuordnungsanfrage MitUmhaengung(string raum, string? zielzone)
        => this with
        {
            Umhaengungen = (Umhaengungen ?? Array.Empty<GebaeudeRaumumhaengung>())
                .Append(new GebaeudeRaumumhaengung(raum, zielzone)).ToList(),
        };
}

/// <summary>
/// <b>Eine Zuordnung von Hand</b>: der Raum <paramref name="Raum"/> geht in die Zone mit dem Schlüssel
/// <paramref name="Zielzone"/> (<see cref="GebaeudeZonenzeileDaten.Schluessel"/>) oder — ohne Zielzone —
/// als eigene Zone ab. Nur Kennung und Schlüssel, nie ein Anzeigetext.
/// </summary>
/// <param name="Raum">Die Raumkennung der Datei.</param>
/// <param name="Zielzone">Der Schlüssel der Zielzone; <c>null</c> = als eigene Zone abtrennen.</param>
public sealed record GebaeudeRaumumhaengung(string Raum, string? Zielzone);

/// <summary>Die sprachneutralen Arten eines Schritts am Zonenplan (<see cref="GebaeudePlanschritt.Art"/>).</summary>
public static class GebaeudePlanschrittArt
{
    /// <summary>Zone anlegen (Name, Nutzung).</summary>
    public const string ANLEGEN = "ANLEGEN";
    /// <summary>Zonen löschen (Zonen); ihre Räume sind danach nicht zugeordnet.</summary>
    public const string LOESCHEN = "LOESCHEN";
    /// <summary>Zone umbenennen (Zone, Name).</summary>
    public const string UMBENENNEN = "UMBENENNEN";
    /// <summary>Nutzung einer Zone setzen (Zone, Nutzung; <c>null</c> = keine).</summary>
    public const string NUTZUNG = "NUTZUNG";
    /// <summary>Zonierung aufheben: alle Räume nicht zugeordnet.</summary>
    public const string AUFHEBEN = "AUFHEBEN";
    /// <summary>Räume einer Zone zuordnen (Räume, Zone).</summary>
    public const string ZUORDNEN = "ZUORDNEN";
    /// <summary>Einen Raum als eigene Zone abtrennen (Räume mit einem Raum).</summary>
    public const string EIGENE = "EIGENE";
    /// <summary>Die Räume eines Geschosses einer Zone zuordnen (Geschoss, Zone).</summary>
    public const string GESCHOSS = "GESCHOSS";
    /// <summary>Die nicht zugeordneten Räume nach einer Regel zuordnen (Regel).</summary>
    public const string REST = "REST";
    /// <summary>Den Haken „beheizt" eines Raums im Plan umstellen (Räume mit einem Raum, Beheizt).</summary>
    public const string HAKEN = "HAKEN";
    /// <summary>
    /// Die Zonen der dazugeladenen Projektdatei übernehmen (Datenaustauschkonzept 16.4): ersetzt die Zonierung des Plans durch
    /// die Zonen der Projektdatei samt Konditionierung je Zone; ohne gelesene Projektdatei lehnt die Datenseite ab.
    /// </summary>
    public const string PROJEKTDATEI = "PROJEKTDATEI";
}

/// <summary>
/// <b>Ein Schritt am Zonenplan</b> (Zonenbaum, Mehrzonenkonzept 6.4): Die Anfrage trägt die Schritte in ihrer
/// Reihenfolge, die Hülle legt sie auf den Regelvorschlag. Nur Schlüssel und Kennungen, nie ein Anzeigetext; ein
/// abgelehnter Schritt lässt den Plan unverändert.
/// </summary>
/// <param name="Art">Die Art (<see cref="GebaeudePlanschrittArt"/>).</param>
/// <param name="Raeume">Die Raumkennungen (Zuordnen, Eigene, Haken).</param>
/// <param name="Zone">Der Zonenschlüssel des Plans (Ziel, Umbenennen, Nutzung).</param>
/// <param name="Zonen">Die Zonenschlüssel (Löschen).</param>
/// <param name="Name">Der Zonenname (Anlegen, Umbenennen).</param>
/// <param name="Nutzung">Der Nutzungsschlüssel; <c>null</c> = keine.</param>
/// <param name="Geschoss">Die Geschosskennung (Geschoss).</param>
/// <param name="Regel">Der Regelschlüssel (Rest nach Regel).</param>
/// <param name="Beheizt">Der Haken „beheizt" (Haken).</param>
/// <param name="Zonierung">Die Zonierung der Projektdatei (<see cref="GebaeudeZonierungSchluessel"/>, Projektdatei); <c>null</c> = die bisherige Wahl.</param>
public sealed record GebaeudePlanschritt(
    string Art,
    IReadOnlyList<string>? Raeume = null,
    string? Zone = null,
    IReadOnlyList<string>? Zonen = null,
    string? Name = null,
    string? Nutzung = null,
    string? Geschoss = null,
    string? Regel = null,
    bool Beheizt = false,
    string? Zonierung = null);

/// <summary>
/// <b>Die sprachneutralen Schlüssel der Zonierung der Projektdatei</b> (E87, F1): die Wahl (<c>data-zonierung</c>, Schritt
/// <see cref="GebaeudePlanschrittArt.PROJEKTDATEI"/>) und die Herkunft einer Zone im Zonenbaum (<c>data-herkunft</c>).
/// </summary>
public static class GebaeudeZonierungSchluessel
{
    /// <summary>Die DIN-V-18599-Zonen (<c>ZoneType</c> 5) — die Vorgabe.</summary>
    public const string DIN = "din";
    /// <summary>Die Simulationszonen (<c>ZoneType</c> 6).</summary>
    public const string SIMULATION = "sim";
    /// <summary>Herkunft einer Zone: aus der Projektdatei, DIN-V-18599-Zonen.</summary>
    public const string HERKUNFT_DIN = "projektdatei-din";
    /// <summary>Herkunft einer Zone: aus der Projektdatei, Simulationszonen.</summary>
    public const string HERKUNFT_SIMULATION = "projektdatei-sim";
}

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
/// <b>Eine Bauteilzeile des Vorschlags</b> — die Zeile, die als echtes Bauteil in die Zone käme,
/// als fertige Anzeigetexte: Bezeichner, Art, Fläche, U-Wert (oder „aus Schichten"), Azimut,
/// Neigung, Randbedingung und Herkunft.
/// </summary>
/// <param name="Bezeichner">Name des Bauteils.</param>
/// <param name="Art">Bauteilart als Anzeigetext.</param>
/// <param name="Flaeche">Fläche mit Einheit.</param>
/// <param name="UWert">U-Wert mit Einheit, „aus Schichten" oder leer als Strich.</param>
/// <param name="Azimut">Azimut in Grad, leer als Strich.</param>
/// <param name="Neigung">Neigung in Grad, leer als Strich.</param>
/// <param name="Randbedingung">Randbedingung als Anzeigetext.</param>
/// <param name="HerkunftText">Herkunft der Zeile als Anzeigetext.</param>
/// <param name="HerkunftSchluessel">Herkunft als sprachneutraler Schlüssel (Stilklasse).</param>
/// <param name="Kennung">Kennung der Quellentität in der Datei; <c>null</c> = Vorgabezeile.</param>
public sealed record GebaeudeBauteilzeileDaten(
    string Bezeichner, string Art, string Flaeche, string UWert, string Azimut, string Neigung,
    string Randbedingung, string HerkunftText, string HerkunftSchluessel, string? Kennung = null);

/// <summary>
/// <b>Der Bauteilvorschlag eines Gebäudes</b> für den Abschnitt „Bauteile (echte Hülle)": ob er
/// sich bilden lässt (sonst der Grund), die Kopfzeile der Zone, die Bauteilzeilen, die Zeile zur
/// inneren Masse und seine Meldungen — alles fertige Anzeigetexte aus den Gaben.
/// </summary>
public sealed record GebaeudeBauteileDaten
{
    /// <summary>Lässt sich der Vorschlag übernehmen? Sonst ist der Schalter aus und gesperrt.</summary>
    public bool Moeglich { get; init; }

    /// <summary>Der Grund, warum nicht; leer, wenn er sich übernehmen lässt.</summary>
    public string Ablehnung { get; init; } = "";

    /// <summary>Die Kopfzeile der Zone (Name, Nutzfläche, Zahl der Bauteile und Aufbauten); leer = keine Zone.</summary>
    public string Kopftext { get; init; } = "";

    /// <summary>Die Bauteilzeilen in der Reihenfolge des Vorschlags.</summary>
    public IReadOnlyList<GebaeudeBauteilzeileDaten> Zeilen { get; init; } = Array.Empty<GebaeudeBauteilzeileDaten>();

    /// <summary>
    /// Die Spalten der Bauteilliste (Bauteil, Art, Fläche, U-Wert, Azimut, Neigung, Randbedingung,
    /// Herkunft) — die Liste ist die <c>Katalogliste</c> mit Suche, Sortierung und Trichter je Spalte;
    /// <c>null</c> = keine Liste.
    /// </summary>
    public Katalogfilterprofil? Profil { get; init; }

    /// <summary>
    /// Die Bauteilzeilen als Zeilen der <c>Katalogliste</c>, in der Reihenfolge des Vorschlags — dieselben
    /// Bauteile wie <see cref="Zeilen"/>, die Zahlen als Zahlen (Sortierung, Trichter mit Vergleich).
    /// </summary>
    public IReadOnlyList<Katalogfilterzeile> Liste { get; init; } = Array.Empty<Katalogfilterzeile>();

    /// <summary>Die Zeile zur inneren Masse (Innenbauteile, Innenflächenfaktor aus der Datei oder Vorgabe); leer = keine.</summary>
    public string Innenweg { get; init; } = "";

    /// <summary>Die Meldungen des Vorschlags.</summary>
    public IReadOnlyList<GebaeudeImportMeldung> Meldungen { get; init; } = Array.Empty<GebaeudeImportMeldung>();
}

/// <summary>
/// <b>Eine Zeile der Liste „Bauteilaufbauten"</b> (Konzept Bauteilaufbau 5.4 b, BA-3) — je Aufbau, nicht je Bauteil, als
/// fertige Anzeigetexte; Bauteile ohne Aufbau je Art und Stufe in einer Zeile „ohne Aufbau".
/// </summary>
public sealed record GebaeudeAufbaulistenzeileDaten
{
    /// <summary>Der stabile Schlüssel der Zeile (Id des Aufbaus bzw. Art und Stufe).</summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Name des Aufbaus bzw. „ohne Aufbau".</summary>
    public string Aufbau { get; init; } = "";

    /// <summary>Bauteilart als Anzeigetext.</summary>
    public string Art { get; init; } = "";

    /// <summary>Die schwächste Stufe der Bauteile dieses Aufbaus.</summary>
    public EPOS.UI.Dialoge.Bedarf.Aufbaustufe Stufe { get; init; }

    /// <summary>Zahl der Bauteile.</summary>
    public int Bauteile { get; init; }

    /// <summary>Fläche der Bauteile [m²] als Text.</summary>
    public string Flaeche { get; init; } = "";

    /// <summary>U der Datei [W/(m²K)]; leer = keiner.</summary>
    public string UDatei { get; init; } = "";

    /// <summary>U aus den Schichten [W/(m²K)]; leer = keiner.</summary>
    public string USchichten { get; init; } = "";

    /// <summary>C₁,korr je m² [kJ/(m²K)], nur zur Anzeige über die Bauteilreduktion; leer = nicht bestimmbar.</summary>
    public string C1korr { get; init; } = "";

    /// <summary>Was fehlt; leer = nichts.</summary>
    public string Fehlt { get; init; } = "";

    /// <summary>Bei einem Ersatzaufbau Typ und Abgleich; leer sonst.</summary>
    public string Typaufbau { get; init; } = "";

    /// <summary>Die Herkunft des Aufbaus als Schlüssel (<see cref="EPOS.UI.Dialoge.Bedarf.SteckbriefHerkunft"/>), etwa „Projektdatei".</summary>
    public string AufbauHerkunftSchluessel { get; init; } = "";

    /// <summary>Der Rang des Aufbaus nach der Rangfolge mit Projektdatei als Anzeigetext; leer = ohne Projektdatei bzw. ohne Rang.</summary>
    public string Aufbaurang { get; init; } = "";

    /// <summary>Die Schichten (ausgeklappt), innen → außen, weggelassene markiert.</summary>
    public IReadOnlyList<EPOS.UI.Dialoge.Bedarf.BauteilsteckbriefSchicht> Schichten { get; init; } = Array.Empty<EPOS.UI.Dialoge.Bedarf.BauteilsteckbriefSchicht>();

    /// <summary>Der Sprung in den Abschnitt „Baustoffe": Schlüssel des Materialnamens; <c>null</c> = kein Sprung.</summary>
    public string? Materialschluessel { get; init; }

    /// <summary>Der Schlüssel der Typwahl eines Ersatzaufbaus (E95-4); <c>null</c> = keine Typwahl (echter Aufbau, ohne Aufbau).</summary>
    public string? Typschluessel { get; init; }

    /// <summary>Der Code des gewählten Typaufbaus; leer = keiner.</summary>
    public string Typcode { get; init; } = "";

    /// <summary>Die wählbaren Typaufbauten derselben Familie (Code, Name); leer = keine Wahl.</summary>
    public IReadOnlyList<GebaeudeZonenregelDaten> Typen { get; init; } = Array.Empty<GebaeudeZonenregelDaten>();
}

/// <summary>Ein Katalogbaustoff der Klappliste im Abschnitt „Baustoffe".</summary>
/// <param name="Id">Die Id des Katalogbaustoffs — der Wert der Zuordnung.</param>
/// <param name="Text">Der Anzeigetext; eine Herstellerzeile nennt den Hersteller.</param>
public sealed record GebaeudeBaustoffwahl(int Id, string Text);

/// <summary>Eine Gruppe der Klappliste (<c>optgroup</c>) — herstellerneutrale Zeilen zuerst.</summary>
/// <param name="Titel">Die Gruppe des Katalogs als Anzeigetext.</param>
/// <param name="Eintraege">Die Baustoffe der Gruppe in Anzeigereihenfolge.</param>
public sealed record GebaeudeBaustoffgruppe(string Titel, IReadOnlyList<GebaeudeBaustoffwahl> Eintraege);

/// <summary>
/// Die sprachneutralen Schlüssel der Abgleichstufe, die die Komponente selbst liest; alle übrigen
/// reicht die Hülle durch (sie sind zugleich die Stilklassen
/// <c>epos-gebimport-abgleich--&lt;schlüssel&gt;</c>).
/// </summary>
public static class GebaeudeAbgleichSchluessel
{
    /// <summary>Die eigene Zuordnung des Anwenders — nur sie lässt sich entfernen.</summary>
    public const string EigeneZuordnung = "N7";

    /// <summary>Ohne Treffer.</summary>
    public const string Ohne = "OHNE";
}

/// <summary>
/// <b>Ein Materialname der Datei im Abschnitt „Baustoffe"</b> — was der Namensabgleich aus ihm
/// gemacht hat, als fertige Anzeigetexte: Stufe, zugeordneter Baustoff mit seinen Stoffwerten und
/// woher die Werte der Schichten kommen. Unveränderlich.
/// </summary>
public sealed record GebaeudeMaterialzeileDaten
{
    /// <summary>Der Name, wie die Datei ihn schreibt.</summary>
    public string Name { get; init; } = "";

    /// <summary>Der normalisierte Name — der Schlüssel einer Zuordnung (mehrere Namen können ihn teilen).</summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Zahl der Schichten mit diesem Namen.</summary>
    public int Schichten { get; init; }

    /// <summary>Die Stufe des Abgleichs als kurzer Anzeigetext („genauer Name", „eigene Zuordnung" …).</summary>
    public string Abgleich { get; init; } = "";

    /// <summary>Die Stufe als sprachneutraler Schlüssel (<see cref="GebaeudeAbgleichSchluessel"/>; Stilklasse).</summary>
    public string AbgleichSchluessel { get; init; } = GebaeudeAbgleichSchluessel.Ohne;

    /// <summary>Der Beleg des Abgleichs als Anzeigetext (Tooltip der Stufe).</summary>
    public string Beleg { get; init; } = "";

    /// <summary>Der zugeordnete Katalogbaustoff; <c>null</c> = keiner (ohne Treffer, Luftschicht, verworfen).</summary>
    public int? IdBaustoff { get; init; }

    /// <summary>Der zugeordnete Baustoff als Anzeigetext; leer = keiner.</summary>
    public string Baustoff { get; init; } = "";

    /// <summary>λ, ρ und c des zugeordneten Baustoffs als Anzeigetext; leer = keiner.</summary>
    public string Stoffwerte { get; init; } = "";

    /// <summary>Woher die Stoffwerte der Schichten im Vorschlag kommen, als Anzeigetext.</summary>
    public string Werte { get; init; } = "";

    /// <summary>Braucht der Name einen Baustoff und trifft keinen? Dann ist die Zeile gelb.</summary>
    public bool OhneTreffer { get; init; }

    /// <summary>Hat das Projekt für den Schlüssel schon eine gemerkte Zuordnung?</summary>
    public bool Gemerkt { get; init; }

    /// <summary>Hat der Anwender im Dialog eine Zuordnung gesetzt oder entfernt, die erst beim Speichern gilt?</summary>
    public bool Vorgemerkt { get; init; }
}

/// <summary>
/// <b>Der Abschnitt „Baustoffe"</b>: die Zusammenfassung, je Materialname der Datei eine Zeile und
/// die Katalogbaustoffe der Klappliste, gruppiert — alles fertige Anzeigetexte aus den Gaben.
/// </summary>
public sealed record GebaeudeBaustoffeDaten
{
    /// <summary>Die Zusammenfassung über dem Abschnitt („16 von 20 zugeordnet, 1 ohne Treffer").</summary>
    public string Zusammenfassung { get; init; } = "";

    /// <summary>Je Materialname eine Zeile, in der Reihenfolge der Datei.</summary>
    public IReadOnlyList<GebaeudeMaterialzeileDaten> Zeilen { get; init; } = Array.Empty<GebaeudeMaterialzeileDaten>();

    /// <summary>Die Katalogbaustoffe der Klappliste je Gruppe.</summary>
    public IReadOnlyList<GebaeudeBaustoffgruppe> Katalog { get; init; } = Array.Empty<GebaeudeBaustoffgruppe>();
}

/// <summary>Eine wählbare Zonenregel: sprachneutraler Schlüssel (der Rückweg) und Anzeigetext.</summary>
/// <param name="Schluessel">Der Schlüssel der Regel — zurück in <see cref="GebaeudeZuordnungsanfrage.Zonenregel"/>.</param>
/// <param name="Text">Der Anzeigetext („Z4 – eine Zone je Geschoss").</param>
public sealed record GebaeudeZonenregelDaten(string Schluessel, string Text);

/// <summary>
/// Eine Gruppe der Nutzungsklappliste des Zonenbaums (<c>&lt;optgroup&gt;</c>): die Profile einer Kategorie; Titel leer =
/// Einträge ohne Kategorie (alte Kennungen ohne Katalog, Texte „(nicht im Katalog)“), sie stehen ohne Gruppe.
/// </summary>
/// <param name="Titel">Der Name der Kategorie; leer = ohne Gruppe.</param>
/// <param name="Eintraege">Die Einträge (Schlüssel, Anzeigetext) in der Ordnung des Katalogs.</param>
public sealed record GebaeudeNutzungsgruppe(string Titel, IReadOnlyList<GebaeudeZonenregelDaten> Eintraege);

/// <summary>
/// <b>Die Herleitungszeile einer Zone des Zonenbaums</b> (Konzept Nutzungsprofile 6.2, NP-F16): Profil und Kategorie, woher
/// die Vorbelegung kommt, welche Größen die Datei liefert, und die Kennwerte des Profils in Kurzform — alles Anzeigetexte der
/// Hülle.
/// </summary>
/// <param name="Profil">Der Name des Profils (bzw. der Text „(nicht im Katalog)“); „keine“, wenn die Quelle auf keins führt.</param>
/// <param name="Kategorie">Der Name der Kategorie; leer ohne Katalog.</param>
/// <param name="Quellart">Die Art der Quelle, sprachneutral (<c>data-quelle</c>): eine Art der Zuordnung (Nutzungsklasse,
/// <c>DIN_NUMMER</c>, Raumtyp), <c>DATEI</c>, <c>GESPEICHERT</c>, <c>HAND</c>; leer = keine.</param>
/// <param name="Quelle">Die Quelle als Text („aus DIN-Nr. 1 der Projektdatei“, „von Hand“); leer = keine.</param>
/// <param name="Dateigroessen">Was die Datei liefert („Heizen und Personen aus der Datei“, Datei vor Profil); leer = nichts.</param>
/// <param name="Kennwerte">Die Kennwerte des Profils in Kurzform („Mo–Fr · 7–18 h · 21 °C“); leer = keine.</param>
public sealed record GebaeudeProfilherleitung(string Profil, string Kategorie, string Quellart, string Quelle, string Dateigroessen,
                                              string Kennwerte)
{
    /// <summary>Die Zeile: „Profil · Kategorie — Quelle; Größen aus der Datei; Kennwerte“, was leer ist, fällt weg.</summary>
    public string Text
    {
        get
        {
            string kopf = Kategorie.Length > 0 ? Profil + " · " + Kategorie : Profil;
            if (Quelle.Length > 0) kopf += " — " + Quelle;
            var teile = new List<string> { kopf };
            if (Dateigroessen.Length > 0) teile.Add(Dateigroessen);
            if (Kennwerte.Length > 0) teile.Add(Kennwerte);
            return string.Join("; ", teile);
        }
    }
}

/// <summary>Ein Raum des Zonenbaums oder der Liste „Nicht zugeordnete Räume".</summary>
/// <param name="Kennung">Raumkennung der Datei — der Schlüssel der Wahl (<c>data-raum</c>).</param>
/// <param name="Name">Anzeigename.</param>
/// <param name="Geschoss">Das Geschoss als Anzeigetext.</param>
/// <param name="Flaeche">Fläche mit Einheit.</param>
/// <param name="Beheizt">Wirksam beheizt (mit den Haken).</param>
/// <param name="BeheiztLautDatei">Was die Datei sagt.</param>
public sealed record GebaeudePlanraumDaten(string Kennung, string Name, string Geschoss, string Flaeche, bool Beheizt, bool BeheiztLautDatei);

/// <summary>Eine Zone des Zonenplans: Schlüssel, Name, Nutzung, Beheizung, Zahl der Räume, Fläche, Sollwert und Räume.</summary>
public sealed record GebaeudePlanzoneDaten
{
    /// <summary>Der Schlüssel der Zone im Plan („Z:n") — das Ziel jedes Planschritts (<c>data-zone</c>).</summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Der Schlüssel, unter dem der Grundriss die Zone führt; leer = die Zone steht nicht im Grundriss.</summary>
    public string Ansichtsschluessel { get; init; } = "";

    /// <summary>Der Name der Zone.</summary>
    public string Name { get; init; } = "";

    /// <summary>Der Nutzungsschlüssel (<c>WOHNEN</c>, <c>BUERO</c>, <c>SCHULE</c>); <c>null</c> = keine.</summary>
    public string? Nutzung { get; init; }

    /// <summary>Beheizt; <c>null</c> = die Zone hat keinen Raum.</summary>
    public bool? Beheizt { get; init; }

    /// <summary>Die Zahl der Räume als Anzeigetext.</summary>
    public string Raeume { get; init; } = "";

    /// <summary>Die Fläche mit Einheit.</summary>
    public string Flaeche { get; init; } = "";

    /// <summary>Der Heizsollwert am Tag mit Einheit; das Leerzeichen der Liste = der Wert des Gebäudes.</summary>
    public string Sollwert { get; init; } = "";

    /// <summary>Die Räume der Zone in Dateireihenfolge.</summary>
    public IReadOnlyList<GebaeudePlanraumDaten> Raumliste { get; init; } = Array.Empty<GebaeudePlanraumDaten>();

    /// <summary>Stammt die Zone aus der Projektdatei (Kennzeichen <c>data-herkunft</c>)?</summary>
    public bool AusProjektdatei { get; init; }

    /// <summary>
    /// Die Herkunft der Zone als Schlüssel (<c>data-herkunft</c>): <see cref="GebaeudeZonierungSchluessel.HERKUNFT_DIN"/>,
    /// <see cref="GebaeudeZonierungSchluessel.HERKUNFT_SIMULATION"/>, sonst <see cref="GebaeudePlanschrittArt.PROJEKTDATEI"/>
    /// (Zonierung unbekannt); leer = nicht aus der Projektdatei.
    /// </summary>
    public string Herkunft { get; init; } = "";

    /// <summary>Die Herkunft als Anzeigetext („aus Projektdatei (DIN-Zonen)“); leer = nicht aus der Projektdatei.</summary>
    public string HerkunftText { get; init; } = "";

    /// <summary>Das Nutzungsprofil der Projektdatei als Tooltip („Nutzungsprofil 1 nach DIN/TS 18599-10“); leer = keines.</summary>
    public string Profiltext { get; init; } = "";

    /// <summary>Die Herleitungszeile unter der Zone (Profil, Quelle, Größen aus der Datei, Kennwerte); <c>null</c> = keine.</summary>
    public GebaeudeProfilherleitung? Herleitung { get; init; }
}

/// <summary>Je Größe der Konditionierung: wie viele übernommene Zonen sie aus der Ganglinie bzw. dem Nutzungsprofil bekommen.</summary>
/// <param name="Schluessel">Das Kennwort der Größe (sprachneutral, <c>data-groesse</c>).</param>
/// <param name="Text">Die Zeile als Anzeigetext.</param>
public sealed record GebaeudeProjektdateiGroesse(string Schluessel, string Text, int Ganglinie, int Nutzungsprofil);

/// <summary>
/// <b>Die dazugeladene Projektdatei</b> (Datenaustauschkonzept 16.4) für den Kopf des Zuordnungsdialogs: Datei, Fassung,
/// Raumabgleich, Zonen, Zeitprofile und Abschnitte, nach der Übernahme die Zonen je Größe; dazu die Meldungen mit der
/// schwersten als Banner. Abgelehnt: <see cref="Ablehnung"/> nennt den Grund, die Daten der Gebäudedatei bleiben.
/// </summary>
public sealed record GebaeudeProjektdateiDaten
{
    /// <summary>Der Dateiname ohne Pfad.</summary>
    public string Dateiname { get; init; } = "";

    /// <summary>Die benannte Ablehnung als Meldung; <c>null</c> = gelesen.</summary>
    public GebaeudeImportMeldung? Ablehnung { get; init; }

    /// <summary>Gelesen (nicht abgelehnt)?</summary>
    public bool Gelesen => Ablehnung is null;

    /// <summary>Die Fassung der Raumtabelle; leer = keine.</summary>
    public string Fassung { get; init; } = "";

    /// <summary>Räume abgeglichen.</summary>
    public int Abgeglichen { get; init; }

    /// <summary>Räume der Projektdatei ohne Raum der Gebäudedatei.</summary>
    public int NichtAbgeglichen { get; init; }

    /// <summary>Räume der Gebäudedatei ohne Gegenstück in der Projektdatei.</summary>
    public int OhneGegenstueck { get; init; }

    /// <summary>Die Zonen der belegten Typen in der Datei.</summary>
    public int Zonen { get; init; }

    /// <summary>Trägt die Datei DIN-V-18599-Zonen mit abgeglichenen Räumen?</summary>
    public bool HatDinZonen { get; init; }

    /// <summary>Trägt die Datei Simulationszonen mit abgeglichenen Räumen?</summary>
    public bool HatSimulationszonen { get; init; }

    /// <summary>Beide Zonierungen vorhanden — nur dann steht das Wahlfeld (<c>data-zonierung</c>)?</summary>
    public bool BeideZonierungen => HatDinZonen && HatSimulationszonen;

    /// <summary>Die gewählte Zonierung (<see cref="GebaeudeZonierungSchluessel"/>).</summary>
    public string Gewaehlt { get; init; } = GebaeudeZonierungSchluessel.DIN;

    /// <summary>Die wirksame Zonierung (<see cref="GebaeudeZonierungSchluessel"/>): die gewählte, sonst die vorhandene.</summary>
    public string Zonierung { get; init; } = GebaeudeZonierungSchluessel.DIN;

    /// <summary>Die wirksame Zonierung als Anzeigetext mit der Zahl ihrer Zonen (Bilanz); leer = keine.</summary>
    public string ZonierungText { get; init; } = "";

    /// <summary>Die übernommenen Zonen; <c>null</c> = noch nicht übernommen.</summary>
    public int? Uebernommen { get; init; }

    /// <summary>Gelesene Zeitprofile.</summary>
    public int Zeitprofile { get; init; }

    /// <summary>Gelesene Abschnitte.</summary>
    public int Abschnitte { get; init; }

    /// <summary>Die Namen der Räume der Projektdatei ohne Raum der Gebäudedatei (Geschoss/Name).</summary>
    public IReadOnlyList<string> RaeumeOhneTreffer { get; init; } = Array.Empty<string>();

    /// <summary>Nach der Übernahme: je Größe die Zonen aus Ganglinie und Nutzungsprofil; vorher leer.</summary>
    public IReadOnlyList<GebaeudeProjektdateiGroesse> Groessen { get; init; } = Array.Empty<GebaeudeProjektdateiGroesse>();

    /// <summary>Alle Meldungen (Leser, Abgleich, Übernahme) — das Übersprungene mit Grund.</summary>
    public IReadOnlyList<GebaeudeImportMeldung> Meldungen { get; init; } = Array.Empty<GebaeudeImportMeldung>();

    /// <summary>Die schwerste Meldung als Banner (Fehler vor Warnung); <c>null</c> = keine Warnung.</summary>
    public GebaeudeImportMeldung? Schwerste { get; init; }

    /// <summary>
    /// Der Vorschlag des Einzonenwegs als Hinweiszeile („Vorschlag aus DIN-Nr. 1: Büro · EPOS-Muster — zuweisbar im
    /// Gebäudeeditor über „Nutzungsprofil übernehmen…““); kein Auswahlfeld, gesetzt wird nichts. Leer = keiner.
    /// </summary>
    public string Einzonenvorschlag { get; init; } = "";

    /// <summary>
    /// Die Standprüfung Gebäudedatei gegen Projektdatei (nur beim Weg „Gebäudedatei + Projektdatei“, Anwenderentscheid vom 08.10.2026);
    /// <c>null</c> = nicht geprüft.
    /// </summary>
    public GebaeudeStandpruefungDaten? Standpruefung { get; init; }

    /// <summary>Die Wahl der Aufbauquelle (<see cref="GebaeudeAufbauquelleSchluessel"/>); ohne angeschlagene Prüfung „datei“.</summary>
    public string Aufbauquelle { get; init; } = GebaeudeAufbauquelleSchluessel.DATEI;

    /// <summary>Ist die Wahl offen (Übernehmen gesperrt, bis gewählt ist)?</summary>
    public bool AufbauquelleOffen => Aufbauquelle == GebaeudeAufbauquelleSchluessel.OFFEN;
}

/// <summary>Die sprachneutralen Schlüssel der Aufbauquelle (Anwenderentscheid vom 08.10.2026).</summary>
public static class GebaeudeAufbauquelleSchluessel
{
    /// <summary>Noch nicht gewählt — Übernehmen gesperrt.</summary>
    public const string OFFEN = "offen";

    /// <summary>Aufbauten und U-Werte der Projektdatei.</summary>
    public const string PROJEKTDATEI = "projektdatei";

    /// <summary>Der Stand der Gebäudedatei (Rangfolge nach U-Abgleich).</summary>
    public const string DATEI = "datei";
}

/// <summary>
/// <b>Die Standprüfung Gebäudedatei gegen Projektdatei</b> für den Dialog der Aufbauquelle: ob sie anschlägt, Zahl und Flächenanteil der
/// abweichenden Bauteile, je Bauteilart die Mediane, bis zu fünf Beispiele und die belegten Anzeichen eines anderen Projektstands.
/// </summary>
public sealed record GebaeudeStandpruefungDaten
{
    /// <summary>Schlägt die Prüfung an (abweichende Fläche über 5 % der Hüllfläche)?</summary>
    public bool Angeschlagen { get; init; }

    /// <summary>Bauteile der Hülle mit U auf beiden Seiten.</summary>
    public int Verglichen { get; init; }

    /// <summary>Davon mit einer Abweichung über 10 %.</summary>
    public int Abweichend { get; init; }

    /// <summary>Die Bruttofläche der abweichenden Bauteile [m²].</summary>
    public double AbweichendM2 { get; init; }

    /// <summary>Die Hüllfläche brutto [m²].</summary>
    public double HuellflaecheM2 { get; init; }

    /// <summary>Der Anteil der abweichenden Fläche an der Hüllfläche [%].</summary>
    public double AnteilProzent { get; init; }

    /// <summary>Je Bauteilart der Median des U auf beiden Seiten.</summary>
    public IReadOnlyList<GebaeudeStandpruefungArtDaten> JeArt { get; init; } = Array.Empty<GebaeudeStandpruefungArtDaten>();

    /// <summary>Bis zu fünf abweichende Bauteile, größte Fläche zuerst.</summary>
    public IReadOnlyList<GebaeudeStandpruefungBeispielDaten> Beispiele { get; init; } = Array.Empty<GebaeudeStandpruefungBeispielDaten>();

    /// <summary>Die belegten Anzeichen eines anderen Projektstands (Kennwort und Satzteil).</summary>
    public IReadOnlyList<GebaeudeStandanzeichenDaten> Anzeichen { get; init; } = Array.Empty<GebaeudeStandanzeichenDaten>();

    /// <summary>Die Warnung des Protokolls als Anzeigetext; leer, wenn die Prüfung nicht anschlägt.</summary>
    public string Meldung { get; init; } = "";
}

/// <summary>Je Bauteilart: Anzeigetext der Art, verglichene und abweichende Bauteile, Median-U der Gebäudedatei und der Projektdatei [W/(m²K)].</summary>
public sealed record GebaeudeStandpruefungArtDaten(string Art, int Verglichen, int Abweichend, double MedianUDatei, double MedianUProjektdatei);

/// <summary>Ein abweichendes Bauteil: Name, Art, Bruttofläche [m²], beide U [W/(m²K)] und beide Aufbaunamen (leer = keiner).</summary>
public sealed record GebaeudeStandpruefungBeispielDaten(string Bauteil, string Art, double FlaecheM2, double UDatei, double UProjektdatei,
                                                       string AufbauDatei, string AufbauProjektdatei);

/// <summary>Ein Anzeichen: sprachneutrales Kennwort (<c>kopie</c>, <c>baujahr</c>, <c>dicke</c>) und der Satzteil mit seinen Belegen.</summary>
public sealed record GebaeudeStandanzeichenDaten(string Schluessel, string Text);

/// <summary>
/// <b>Der Zonenplan für den Zonenbaum</b> (Mehrzonenkonzept 6.4): die Zonen mit ihren Räumen, die nicht zugeordneten
/// Räume, die Geschosse und Nutzungen der Klapplisten und die Meldung des letzten Schritts.
/// </summary>
public sealed record GebaeudeZonenplanDaten
{
    /// <summary>Die Zonen in Planreihenfolge.</summary>
    public IReadOnlyList<GebaeudePlanzoneDaten> Zonen { get; init; } = Array.Empty<GebaeudePlanzoneDaten>();

    /// <summary>Die nicht zugeordneten Räume in Dateireihenfolge — solange einer dasteht, ist OK gesperrt.</summary>
    public IReadOnlyList<GebaeudePlanraumDaten> NichtZugeordnet { get; init; } = Array.Empty<GebaeudePlanraumDaten>();

    /// <summary>Die Zahl der Räume, die die Regel bewusst außerhalb lässt (sie sperren das OK nicht).</summary>
    public int Ausserhalb { get; init; }

    /// <summary>Die Geschosse (Kennung, Anzeigetext) für „Geschoss zur Zone".</summary>
    public IReadOnlyList<GebaeudeZonenregelDaten> Geschosse { get; init; } = Array.Empty<GebaeudeZonenregelDaten>();

    /// <summary>Die Nutzungen (Schlüssel, Anzeigetext) der Klappliste; „keine" ist der Platzhalter.</summary>
    public IReadOnlyList<GebaeudeZonenregelDaten> Nutzungen { get; init; } = Array.Empty<GebaeudeZonenregelDaten>();

    /// <summary>
    /// Dieselben Nutzungen je Kategorie für die Klappliste je Zone (<c>&lt;optgroup&gt;</c>); zuerst die ohne Gruppe. Ohne
    /// eigene Gruppen stehen die <see cref="Nutzungen"/> als eine Gruppe ohne Titel.
    /// </summary>
    public IReadOnlyList<GebaeudeNutzungsgruppe> Nutzungsgruppen
    {
        get => _nutzungsgruppen ?? (Nutzungen.Count == 0 ? Array.Empty<GebaeudeNutzungsgruppe>()
                                                         : new[] { new GebaeudeNutzungsgruppe("", Nutzungen) });
        init => _nutzungsgruppen = value;
    }

    private readonly IReadOnlyList<GebaeudeNutzungsgruppe>? _nutzungsgruppen;

    /// <summary>Ergibt der Plan genau eine Zone (Einzonenweg: Name und Nutzung werden nicht übernommen)?</summary>
    public bool Einzonig { get; init; }

    /// <summary>Der Vorgabename einer neuen Zone („Zone n", im Plan frei).</summary>
    public string NeuerName { get; init; } = "";

    /// <summary>Die Meldung des letzten Schritts (Ablehnung oder Hinweis); <c>null</c> = keine.</summary>
    public GebaeudeImportMeldung? Schrittmeldung { get; init; }

    /// <summary>Hat der Kern den letzten Schritt abgelehnt (der Plan ist der ohne ihn)?</summary>
    public bool LetzterAbgelehnt { get; init; }

    /// <summary>Wie viele frühere Schritte sich nicht mehr auflegen ließen.</summary>
    public int Verworfen { get; init; }
}

/// <summary>Ein Raum einer Zone (aufgeklappt): Name, Geschoss, Fläche, Beheizungsregel und ihr Beleg.</summary>
/// <param name="Kennung">Raumkennung der Datei — der Schlüssel der Übersteuerung „beheizt".</param>
/// <param name="Name">Anzeigename (Name, sonst Kennung).</param>
/// <param name="Geschoss">Das Geschoss als Anzeigetext; Strich ohne.</param>
/// <param name="Flaeche">Fläche mit Einheit.</param>
/// <param name="Beheizungsregel">Die Regel, nach der der Raum beheizt oder unbeheizt gilt; Strich ohne.</param>
/// <param name="Beleg">Woraus die Regel folgt, als Anzeigetext.</param>
/// <param name="BeheiztLautDatei">Was die Datei sagt — gegen sie wird eine Übersteuerung gemerkt.</param>
public sealed record GebaeudeZonenraumDaten(
    string Kennung, string Name, string Geschoss, string Flaeche, string Beheizungsregel, string Beleg, bool BeheiztLautDatei);

/// <summary>
/// <b>Eine Zone des Vorschlags</b>: Name, Regel, Zahl der Räume, Fläche, Volumen, beheizt und ein
/// Hinweis (zugeschlagene Zonen, unter der Mindestgröße, ohne Außenfläche); aufgeklappt die Räume.
/// Alles fertige Anzeigetexte aus den Gaben.
/// </summary>
public sealed record GebaeudeZonenzeileDaten
{
    /// <summary>Der Name der Zone — zugleich der Schlüssel des Aufklappens.</summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Der sprachneutrale Schlüssel der Zone — das Ziel einer Zuordnung von Hand
    /// (<see cref="GebaeudeRaumumhaengung.Zielzone"/>), nie ihr Name.
    /// </summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Hat eine Zuordnung von Hand die Zone gebildet oder verändert?</summary>
    public bool VonHand { get; init; }

    /// <summary>Die Regel, nach der sich die Zone gebildet hat, als Anzeigetext.</summary>
    public string Regel { get; init; } = "";

    /// <summary>Die Zahl der Räume als Anzeigetext.</summary>
    public string Raeume { get; init; } = "";

    /// <summary>Die Fläche mit Einheit.</summary>
    public string Flaeche { get; init; } = "";

    /// <summary>Das Volumen mit Einheit.</summary>
    public string Volumen { get; init; } = "";

    /// <summary>Ist die Zone beheizt?</summary>
    public bool Beheizt { get; init; }

    /// <summary>Der Hinweis zur Zone; leer = keiner.</summary>
    public string Hinweis { get; init; } = "";

    /// <summary>
    /// Der Heizsollwert am Tag der Zone mit Einheit, wenn sie einen eigenen trägt (Raumtemperatur der Datei);
    /// sonst das Leerzeichen der Liste (GIMP_WERT_LEER) — der Wert des Gebäudes.
    /// </summary>
    public string Sollwert { get; init; } = "";

    /// <summary>Die Räume der Zone in Dateireihenfolge.</summary>
    public IReadOnlyList<GebaeudeZonenraumDaten> Raumliste { get; init; } = Array.Empty<GebaeudeZonenraumDaten>();
}

/// <summary>
/// <b>Eine Fläche einer Zone</b>: die Zeile der Liste „Flächen je Zone" (fertige Anzeigetexte je
/// Spalte des Profils) und die drei Befunde, nach denen der Dialog filtert.
/// </summary>
/// <param name="Zeile">Die Zeile der Liste.</param>
/// <param name="Fehler">Hat die Fläche einen Befund (ohne Gegenstück, ohne U-Wert, Fläche geschätzt)?</param>
/// <param name="OhneGegenstueck">Eine innere Grenze ohne Gegenstück — gerechnet gegen unbeheizt.</param>
/// <param name="OhneUWert">Weder ein U-Wert noch ein Aufbau.</param>
/// <param name="MitBauteilbefund">Steht das Bauteil im Farbmodus „Befund" orange oder rot (G5-3: Körper unlesbar, ohne Eigenschaften)?</param>
public sealed record GebaeudeFlaechenzeileDaten(Katalogfilterzeile Zeile, bool Fehler, bool OhneGegenstueck, bool OhneUWert,
                                                bool MitBauteilbefund = false);

/// <summary>Die Bilanz der Zonierung als Anzeigetexte mit Einheit.</summary>
public sealed record GebaeudeZonenbilanzDaten(string Zonen, string BeheizteFlaeche, string Volumen, string Aussenflaeche, string Trennflaeche);

/// <summary>
/// <b>Die Zonierung eines Gebäudes</b> für den Dialog — nur, wenn die Datei mehr als eine Regel
/// trägt (sonst <c>null</c>, und der Dialog zeigt den Einzonenweg wie gehabt): die wählbaren
/// Regeln, die gebildete, die Bilanz, die schwerste Meldung, die Obergrenze mit dem Vorschlag einer
/// gröberen Regel, die Zonen und die Flächen je Zone samt dem Profil ihrer Liste.
/// </summary>
public sealed record GebaeudeZonierungDaten
{
    /// <summary>Die wählbaren Regeln in Rangfolge.</summary>
    public IReadOnlyList<GebaeudeZonenregelDaten> Regeln { get; init; } = Array.Empty<GebaeudeZonenregelDaten>();

    /// <summary>Der Schlüssel der Regel, nach der gebildet ist.</summary>
    public string Regel { get; init; } = "";

    /// <summary>Die gebildete Regel als Anzeigetext.</summary>
    public string RegelText { get; init; } = "";

    /// <summary>Eine Zone — der Einzonenweg: keine Abschnitte „Zonen" und „Flächen je Zone".</summary>
    public bool Einzonig { get; init; }

    /// <summary>Die Bilanz.</summary>
    public GebaeudeZonenbilanzDaten Bilanz { get; init; } = new("", "", "", "", "");

    /// <summary>Die schwerste Meldung der Zonierung ab Stufe Warnung; <c>null</c> = keine.</summary>
    public GebaeudeImportMeldung? Schwerste { get; init; }

    /// <summary>Mehr Zonen, als gerechnet werden (Obergrenze)?</summary>
    public bool ZuViele { get; init; }

    /// <summary>Der Schlüssel der vorgeschlagenen gröberen Regel; <c>null</c> = keine.</summary>
    public string? Vorschlagsregel { get; init; }

    /// <summary>Die vorgeschlagene Regel als Anzeigetext.</summary>
    public string VorschlagsregelText { get; init; } = "";

    /// <summary>Die Zonen in Rangfolge.</summary>
    public IReadOnlyList<GebaeudeZonenzeileDaten> Zonen { get; init; } = Array.Empty<GebaeudeZonenzeileDaten>();

    /// <summary>Die Spalten der Liste „Flächen je Zone"; <c>null</c> = keine Liste.</summary>
    public Katalogfilterprofil? Flaechenprofil { get; init; }

    /// <summary>Die Flächen aller Zonen in der Reihenfolge des Vorschlags.</summary>
    public IReadOnlyList<GebaeudeFlaechenzeileDaten> Flaechen { get; init; } = Array.Empty<GebaeudeFlaechenzeileDaten>();

    /// <summary>
    /// Die <b>abgelehnten Zuordnungen von Hand</b> als Meldungen des Kerns, in der Reihenfolge der Zuordnungen
    /// (Raum unbekannt, Zielzone unbekannt, ungleich beheizt, eine Zone je Gebäude) — die letzte gehört zur
    /// letzten abgelehnten Zuordnung. Leer = keine abgelehnt.
    /// </summary>
    public IReadOnlyList<GebaeudeImportMeldung> Ablehnungen { get; init; } = Array.Empty<GebaeudeImportMeldung>();

    /// <summary>Der Zonenplan für den Zonenbaum; <c>null</c> = keiner (die Regel ist abgelehnt).</summary>
    public GebaeudeZonenplanDaten? Plan { get; init; }
}

/// <summary>
/// Der Stand einer Zuordnung, wie ihn die Hülle aus dem Kern baut: Kopfzeile, Namensvorschlag,
/// Raumliste, Zeilen, Meldungen, der Bauteilvorschlag samt Abschnitt „Baustoffe" — und der
/// Herkunftstext für eine Handänderung, damit auch „manuell" aus den Gaben kommt.
/// </summary>
public sealed record GebaeudeImportStand
{
    /// <summary>
    /// Lässt sich die Raumtemperatur der Datei als Heizsollwert übernehmen? Mindestens ein beheizter Raum trägt eine
    /// CAD-Raumtemperatur und keiner einen Norm-Sollwert — nur dann zeigt der Dialog den Schalter.
    /// </summary>
    public bool CadSollwertMoeglich { get; init; }

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

    /// <summary>
    /// Die Klasse, die der Import aus dem Baujahr der Datei zog (Index 0 = A … 12 = M); <c>null</c>
    /// ohne Baujahr oder bei eigener Wahl. Die Klappliste zeigt sie, solange keine eigene Wahl besteht.
    /// </summary>
    public int? KlasseDerDatei { get; init; }

    /// <summary>Der Hinweis unter der Klappliste: Herkunft der Klasse und wie viele Werte sie füllt; leer = der allgemeine.</summary>
    public string KlassenHinweis { get; init; } = "";

    /// <summary>Der Bauteilvorschlag; <c>null</c> = keiner (dann steht der Abschnitt nicht).</summary>
    public GebaeudeBauteileDaten? Bauteile { get; init; }

    /// <summary>Die Materialnamen der Datei mit ihrem Abgleich; <c>null</c> = keine (dann steht der Abschnitt nicht).</summary>
    public GebaeudeBaustoffeDaten? Baustoffe { get; init; }

    /// <summary>Die Liste „Bauteilaufbauten" (BA-3) — je Aufbau eine Zeile; leer = keine.</summary>
    public IReadOnlyList<GebaeudeAufbaulistenzeileDaten> Aufbauten { get; init; } = Array.Empty<GebaeudeAufbaulistenzeileDaten>();

    /// <summary>Die Zonierung; <c>null</c>, wenn die Datei nur eine Zone je Gebäude trägt (Einzonenweg wie gehabt).</summary>
    public GebaeudeZonierungDaten? Zonierung { get; init; }

    /// <summary>
    /// Der Grundriss je Geschoss aus dem Zonengeometrie-Modell des Kerns (Entscheid E11) — auch im
    /// Einzonenweg; <c>null</c> ohne gelesenes Gebäude.
    /// </summary>
    public EPOS.UI.Dialoge.Bedarf.GebaeudeAnsichtDaten? Ansicht { get; init; }

    /// <summary>
    /// Lässt sich zum gewählten Gebäude eine Projektdatei dazuladen (Gebäudedatei aus dem passenden CAD-Programm)? Sonst steht der Knopf ausgegraut mit
    /// Grund (Datenaustauschkonzept 16.4).
    /// </summary>
    public bool ProjektdateiMoeglich { get; init; }

    /// <summary>Die dazugeladene Projektdatei; <c>null</c> = keine.</summary>
    public GebaeudeProjektdateiDaten? Projektdatei { get; init; }
}

/// <summary>
/// <b>Das Ergebnis des Dialogs</b> — ALLE Zeilen, auch die unveränderten
/// (Datenaustauschkonzept 2.4), mit Wert, Herkunftsschlüssel und Haken; dazu Gebäude,
/// Baualtersklasse, Name des neuen Gebäudes, die umgestellten Räume, die Wahl, das Gebäude
/// als Zone mit Bauteilen zu übernehmen, und die Zuordnungen der Baustoffe. Abbrechen liefert
/// <c>null</c>.
/// </summary>
/// <param name="Gebaeudeindex">Das gewählte Gebäude der Datei.</param>
/// <param name="Baualtersklasse">Index 0 = A … 12 = M; <c>null</c> = keine.</param>
/// <param name="Gebaeudename">Der Name, unter dem das Gebäude angelegt würde.</param>
/// <param name="BeheiztUebersteuert">Raumkennung → beheizt, nur die Abweichungen von der Datei.</param>
/// <param name="Zeilen">Alle Zeilen der Zuordnung.</param>
/// <param name="AlsZone">Als Zone mit Bauteilen übernehmen (Schalter des Abschnitts „Bauteile")?</param>
/// <param name="Baustoffzuordnungen">
/// Die Zuordnungen des Abschnitts „Baustoffe" (Schlüssel → Katalogbaustoff, <c>null</c> = entfernen);
/// gemerkt werden sie für das Projekt erst mit dem Speichern der Gebäudeliste.
/// </param>
/// <param name="Zonenregel">Die Zonenregel, nach der gebildet ist; <c>null</c> = die Vorgabe der Datei.</param>
/// <param name="Umhaengungen">
/// Die Zuordnungen von Hand in ihrer Reihenfolge; <c>null</c> = keine. Gespeichert werden sie mit den
/// Zonen und den Raumpaarungen erst mit der Gebäudeliste.
/// </param>
/// <param name="RaumtemperaturAlsSollwert">Der Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen".</param>
/// <param name="Planschritte">Die Schritte am Zonenplan in ihrer Reihenfolge; <c>null</c> = keine (der Regelvorschlag).</param>
/// <param name="Plangrundhaken">Die Haken, mit denen der Plan vor dem ersten Schritt gebildet war; <c>null</c> = die heutigen.</param>
public sealed record GebaeudeImportErgebnis(
    int Gebaeudeindex,
    int? Baualtersklasse,
    string Gebaeudename,
    IReadOnlyDictionary<string, bool> BeheiztUebersteuert,
    IReadOnlyList<GebaeudeFeldzeileDaten> Zeilen,
    bool AlsZone = false,
    IReadOnlyDictionary<string, int?>? Baustoffzuordnungen = null,
    string? Zonenregel = null,
    IReadOnlyList<GebaeudeRaumumhaengung>? Umhaengungen = null,
    bool RaumtemperaturAlsSollwert = false,
    IReadOnlyList<GebaeudePlanschritt>? Planschritte = null,
    IReadOnlyDictionary<string, bool>? Plangrundhaken = null,
    IReadOnlyDictionary<string, string>? Typwahl = null)
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

    /// <summary>GIMP_DLG_QUELLWAHL — Beschriftung der Quellenwahl (je Format, mit Projektdatei, nur Projektdatei).</summary>
    public string Quellwahl { get; set; } = Resource.GIMP_DLG_QUELLWAHL;

    /// <summary>GIMP_DLG_QUELLWAHL_ALLE — der Platzhalter der Quellenwahl: jede Datei, die Quelle folgt der Endung.</summary>
    public string QuellwahlAlle { get; set; } = Resource.GIMP_DLG_QUELLWAHL_ALLE;

    /// <summary>GIMP_DLG_QUELLZEILE — Platzhalter {0} = die Quelle des gelesenen Laufs („Quelle: Projektdatei“).</summary>
    public string Quellzeile { get; set; } = Resource.GIMP_DLG_QUELLZEILE;

    /// <summary>GIMP_DLG_QUELLE_MIT_SQPROJ — die Quelle „Gebäudedatei + Projektdatei“, sobald die Projektdatei dazugeladen ist.</summary>
    public string QuelleMitProjektdatei { get; set; } = Resource.GIMP_DLG_QUELLE_MIT_SQPROJ;

    /// <summary>GIMP_DLG_SQNUR_UNBEHEIZT — leiser Hinweis im Weg „nur Projektdatei“: unbeheizte Räume in einer Zone.</summary>
    public string SqNurUnbeheizt { get; set; } = Resource.GIMP_DLG_SQNUR_UNBEHEIZT;

    /// <summary>GEB_AUSRICHTUNG_NORDRICHTUNG — der Abschnitt der Nordrichtung (G5-N).</summary>
    public string GruppeNordrichtung { get; set; } = Resource.GEB_AUSRICHTUNG_NORDRICHTUNG;

    /// <summary>GEB_AUSRICHTUNG_LAEUFT — der Fortschrittstext des Neulesens mit neuer Nordrichtung (G5-N).</summary>
    public string NordrichtungLaeuft { get; set; } = Resource.GEB_AUSRICHTUNG_LAEUFT;

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

    /// <summary>GIMP_DLG_GRP_BAUTEILE — Kopf des Abschnitts mit dem Bauteilvorschlag.</summary>
    public string GruppeBauteile { get; set; } = Resource.GIMP_DLG_GRP_BAUTEILE;

    /// <summary>GIMP_DLG_ALS_ZONE — der Schalter „Als Zone mit Bauteilen übernehmen".</summary>
    public string AlsZone { get; set; } = Resource.GIMP_DLG_ALS_ZONE;

    /// <summary>GIMP_DLG_ALS_ZONE_HINWEIS</summary>
    public string AlsZoneHinweis { get; set; } = Resource.GIMP_DLG_ALS_ZONE_HINWEIS;

    /// <summary>GIMP_DLG_ALS_ZONE_NICHT — Platzhalter {0} = der Grund, warum der Vorschlag sich nicht übernehmen lässt.</summary>
    public string AlsZoneNicht { get; set; } = Resource.GIMP_DLG_ALS_ZONE_NICHT;

    /// <summary>GIMP_DLG_ALS_ZONEN — der Schalter bei mehreren Zonen „Als Zonen mit Bauteilen übernehmen".</summary>
    public string AlsZonen { get; set; } = Resource.GIMP_DLG_ALS_ZONEN;

    /// <summary>GIMP_DLG_ALS_ZONEN_HINWEIS</summary>
    public string AlsZonenHinweis { get; set; } = Resource.GIMP_DLG_ALS_ZONEN_HINWEIS;

    /// <summary>GIMP_DLG_BILANZ_ZONEN</summary>
    public string BilanzZonen { get; set; } = Resource.GIMP_DLG_BILANZ_ZONEN;

    /// <summary>GIMP_DLG_BILANZ_FLAECHE</summary>
    public string BilanzFlaeche { get; set; } = Resource.GIMP_DLG_BILANZ_FLAECHE;

    /// <summary>GIMP_DLG_BILANZ_VOLUMEN</summary>
    public string BilanzVolumen { get; set; } = Resource.GIMP_DLG_BILANZ_VOLUMEN;

    /// <summary>GIMP_DLG_BILANZ_AUSSEN</summary>
    public string BilanzAussen { get; set; } = Resource.GIMP_DLG_BILANZ_AUSSEN;

    /// <summary>GIMP_DLG_BILANZ_TRENN</summary>
    public string BilanzTrenn { get; set; } = Resource.GIMP_DLG_BILANZ_TRENN;

    /// <summary>GIMP_DLG_GROEBERE_REGEL — Knopf bei zu vielen Zonen, {0} = die vorgeschlagene Regel.</summary>
    public string GroebereRegel { get; set; } = Resource.GIMP_DLG_GROEBERE_REGEL;

    /// <summary>GIMP_DLG_GRP_ZONEN</summary>
    public string GruppeZonen { get; set; } = Resource.GIMP_DLG_GRP_ZONEN;

    /// <summary>GIMP_DLG_SP_ZONE</summary>
    public string SpalteZone { get; set; } = Resource.GIMP_DLG_SP_ZONE;

    /// <summary>GIMP_DLG_SP_REGEL</summary>
    public string SpalteRegel { get; set; } = Resource.GIMP_DLG_SP_REGEL;

    /// <summary>GIMP_DLG_SP_RAEUME</summary>
    public string SpalteRaeume { get; set; } = Resource.GIMP_DLG_SP_RAEUME;

    /// <summary>GIMP_DLG_SP_VOLUMEN</summary>
    public string SpalteVolumen { get; set; } = Resource.GIMP_DLG_SP_VOLUMEN;

    /// <summary>GIMP_DLG_SP_GESCHOSS</summary>
    public string SpalteGeschoss { get; set; } = Resource.GIMP_DLG_SP_GESCHOSS;

    /// <summary>GIMP_DLG_SP_BEHEIZUNGSREGEL</summary>
    public string SpalteBeheizungsregel { get; set; } = Resource.GIMP_DLG_SP_BEHEIZUNGSREGEL;

    /// <summary>GIMP_DLG_SP_HINWEIS</summary>
    public string SpalteHinweis { get; set; } = Resource.GIMP_DLG_SP_HINWEIS;

    /// <summary>GIMP_DLG_SP_SOLLWERT — Spalte „Heizsollwert Tag" der Zonenliste.</summary>
    public string SpalteSollwert { get; set; } = Resource.GIMP_DLG_SP_SOLLWERT;

    /// <summary>GIMP_DLG_CAD_SOLLWERT — der Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen".</summary>
    public string CadSollwert { get; set; } = Resource.GIMP_DLG_CAD_SOLLWERT;

    /// <summary>GIMP_DLG_CAD_SOLLWERT_HINWEIS — der Hinweis am Schalter (Vorgabe Normtemperatur, Wert änderbar).</summary>
    public string CadSollwertHinweis { get; set; } = Resource.GIMP_DLG_CAD_SOLLWERT_HINWEIS;

    /// <summary>GIMP_DLG_ZONE_AUFKLAPPEN — {0} = Zone.</summary>
    public string ZoneAufklappen { get; set; } = Resource.GIMP_DLG_ZONE_AUFKLAPPEN;

    /// <summary>GIMP_DLG_ZONE_ZUKLAPPEN — {0} = Zone.</summary>
    public string ZoneZuklappen { get; set; } = Resource.GIMP_DLG_ZONE_ZUKLAPPEN;

    /// <summary>GIMP_DLG_ZONE_BEHEIZT — Beschriftung des Hakens je Zone für die Sprachausgabe, {0} = Zone.</summary>
    public string ZoneBeheizt { get; set; } = Resource.GIMP_DLG_ZONE_BEHEIZT;

    /// <summary>GIMP_DLG_SP_NUTZUNG</summary>
    public string SpalteNutzung { get; set; } = Resource.GIMP_DLG_SP_NUTZUNG;

    /// <summary>GIMP_DLG_PLAN_WURZEL</summary>
    public string PlanWurzel { get; set; } = Resource.GIMP_DLG_PLAN_WURZEL;

    /// <summary>GIMP_DLG_PLAN_ZONE_WAEHLEN</summary>
    public string ZoneWaehlen { get; set; } = Resource.GIMP_DLG_PLAN_ZONE_WAEHLEN;

    /// <summary>GIMP_DLG_PLAN_ZONE_NAME</summary>
    public string ZoneName { get; set; } = Resource.GIMP_DLG_PLAN_ZONE_NAME;

    /// <summary>GIMP_DLG_PLAN_ZONE_NUTZUNG</summary>
    public string ZoneNutzung { get; set; } = Resource.GIMP_DLG_PLAN_ZONE_NUTZUNG;

    /// <summary>GIMP_DLG_PLAN_NUTZUNG_KEINE</summary>
    public string NutzungKeine { get; set; } = Resource.GIMP_DLG_PLAN_NUTZUNG_KEINE;

    /// <summary>GIMP_DLG_PLAN_ZONE_LEER</summary>
    public string ZoneLeer { get; set; } = Resource.GIMP_DLG_PLAN_ZONE_LEER;

    /// <summary>GIMP_DLG_PLAN_RAUM_WAEHLEN</summary>
    public string RaumWaehlen { get; set; } = Resource.GIMP_DLG_PLAN_RAUM_WAEHLEN;

    /// <summary>GIMP_DLG_PLAN_NAME</summary>
    public string PlanName { get; set; } = Resource.GIMP_DLG_PLAN_NAME;

    /// <summary>GIMP_DLG_PLAN_NUTZUNG</summary>
    public string PlanNutzung { get; set; } = Resource.GIMP_DLG_PLAN_NUTZUNG;

    /// <summary>GIMP_DLG_PLAN_ANLEGEN_OK</summary>
    public string PlanAnlegenOk { get; set; } = Resource.GIMP_DLG_PLAN_ANLEGEN_OK;

    /// <summary>GIMP_DLG_PLAN_ANLEGEN_VERWERFEN</summary>
    public string PlanAnlegenVerwerfen { get; set; } = Resource.GIMP_DLG_PLAN_ANLEGEN_VERWERFEN;

    /// <summary>GIMP_DLG_PLAN_ANLEGEN</summary>
    public string PlanAnlegen { get; set; } = Resource.GIMP_DLG_PLAN_ANLEGEN;

    /// <summary>GIMP_DLG_PLAN_LOESCHEN</summary>
    public string PlanLoeschen { get; set; } = Resource.GIMP_DLG_PLAN_LOESCHEN;

    /// <summary>GIMP_DLG_PLAN_AUFHEBEN</summary>
    public string PlanAufheben { get; set; } = Resource.GIMP_DLG_PLAN_AUFHEBEN;

    /// <summary>GIMP_DLG_PLAN_ZUORDNEN</summary>
    public string PlanZuordnen { get; set; } = Resource.GIMP_DLG_PLAN_ZUORDNEN;

    /// <summary>GIMP_DLG_PLAN_ZUORDNEN_GRUND</summary>
    public string PlanZuordnenGrund { get; set; } = Resource.GIMP_DLG_PLAN_ZUORDNEN_GRUND;

    /// <summary>GIMP_DLG_PLAN_GESCHOSS</summary>
    public string PlanGeschoss { get; set; } = Resource.GIMP_DLG_PLAN_GESCHOSS;

    /// <summary>GIMP_DLG_PLAN_GESCHOSS_KNOPF</summary>
    public string PlanGeschossKnopf { get; set; } = Resource.GIMP_DLG_PLAN_GESCHOSS_KNOPF;

    /// <summary>GIMP_DLG_PLAN_REST_REGEL</summary>
    public string PlanRestRegel { get; set; } = Resource.GIMP_DLG_PLAN_REST_REGEL;

    /// <summary>GIMP_DLG_PLAN_REST</summary>
    public string PlanRest { get; set; } = Resource.GIMP_DLG_PLAN_REST;

    /// <summary>GIMP_DLG_PLAN_HINWEIS</summary>
    public string PlanHinweis { get; set; } = Resource.GIMP_DLG_PLAN_HINWEIS;

    /// <summary>GIMP_DLG_PLAN_EINZONIG</summary>
    public string PlanEinzonig { get; set; } = Resource.GIMP_DLG_PLAN_EINZONIG;

    /// <summary>GIMP_DLG_ALS_ZONE_PLAN_HINWEIS</summary>
    public string AlsZonePlanHinweis { get; set; } = Resource.GIMP_DLG_ALS_ZONE_PLAN_HINWEIS;

    /// <summary>GIMP_DLG_PLAN_OFFEN_TITEL</summary>
    public string PlanOffenTitel { get; set; } = Resource.GIMP_DLG_PLAN_OFFEN_TITEL;

    /// <summary>GIMP_DLG_PLAN_OFFEN_LEER</summary>
    public string PlanOffenLeer { get; set; } = Resource.GIMP_DLG_PLAN_OFFEN_LEER;

    /// <summary>GIMP_DLG_PLAN_AUSSERHALB</summary>
    public string PlanAusserhalb { get; set; } = Resource.GIMP_DLG_PLAN_AUSSERHALB;

    /// <summary>GIMP_DLG_PLAN_AUFHEBEN_TITEL</summary>
    public string PlanAufhebenTitel { get; set; } = Resource.GIMP_DLG_PLAN_AUFHEBEN_TITEL;

    /// <summary>GIMP_DLG_PLAN_AUFHEBEN_FRAGE</summary>
    public string PlanAufhebenFrage { get; set; } = Resource.GIMP_DLG_PLAN_AUFHEBEN_FRAGE;

    /// <summary>GIMP_DLG_PLAN_OK_GESPERRT</summary>
    public string PlanOkGesperrt { get; set; } = Resource.GIMP_DLG_PLAN_OK_GESPERRT;

    /// <summary>GIMP_DLG_ZONEN_HINWEIS</summary>
    public string ZonenHinweis { get; set; } = Resource.GIMP_DLG_ZONEN_HINWEIS;

    /// <summary>GIMP_DLG_GRP_GRUNDRISS — Kopf des Grundrisses, wenn er nicht neben der Zonenliste steht.</summary>
    public string GruppeGrundriss { get; set; } = Resource.GIMP_DLG_GRP_GRUNDRISS;

    /// <summary>GIMP_DLG_ZUORDNEN_ZU — die Zielzone eines Klicks im Grundriss und des Knopfs „Umhängen".</summary>
    public string ZuordnenZu { get; set; } = Resource.GIMP_DLG_ZUORDNEN_ZU;

    /// <summary>GIMP_DLG_EIGENE_ZONE — der Eintrag „als eigene Zone" der Zielzone.</summary>
    public string EigeneZone { get; set; } = Resource.GIMP_DLG_EIGENE_ZONE;

    /// <summary>GIMP_DLG_RAUMWAHL — die Raumwahl des Wegs ohne Grundriss.</summary>
    public string Raumwahl { get; set; } = Resource.GIMP_DLG_RAUMWAHL;

    /// <summary>GIMP_DLG_OHNE_UMRISS — Zusatz eines Raums ohne Umriss in der Raumwahl.</summary>
    public string OhneUmriss { get; set; } = Resource.GIMP_DLG_OHNE_UMRISS;

    /// <summary>GIMP_DLG_UMHAENGEN — der Knopf des Wegs ohne Grundriss.</summary>
    public string Umhaengen { get; set; } = Resource.GIMP_DLG_UMHAENGEN;

    /// <summary>GIMP_DLG_UMHAENGEN_HINWEIS</summary>
    public string UmhaengenHinweis { get; set; } = Resource.GIMP_DLG_UMHAENGEN_HINWEIS;

    /// <summary>GIMP_DLG_SCHON_DORT — {0} = Raum, {1} = Zone.</summary>
    public string SchonDort { get; set; } = Resource.GIMP_DLG_SCHON_DORT;

    /// <summary>GIMP_DLG_SCHON_EIGENE — {0} = Raum.</summary>
    public string SchonEigene { get; set; } = Resource.GIMP_DLG_SCHON_EIGENE;

    /// <summary>GIMP_DLG_AUSWEG — der Ausweg bei ungleicher Beheizung, {0} = Raum, {1} = Zielzone.</summary>
    public string Ausweg { get; set; } = Resource.GIMP_DLG_AUSWEG;

    /// <summary>GIMP_DLG_REGEL_FRAGE_TITEL</summary>
    public string RegelFrageTitel { get; set; } = Resource.GIMP_DLG_REGEL_FRAGE_TITEL;

    /// <summary>GIMP_DLG_REGEL_FRAGE — Rückfrage vor dem Regelwechsel, {0} = Zahl der Zuordnungen von Hand.</summary>
    public string RegelFrage { get; set; } = Resource.GIMP_DLG_REGEL_FRAGE;

    /// <summary>ALLG_BTN_JA</summary>
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;

    /// <summary>ALLG_BTN_NEIN</summary>
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;

    /// <summary>Die Texte der Grundrissansicht — in derselben Sprache angelegt wie dieses Bündel.</summary>
    public EPOS.UI.Dialoge.Bedarf.GebaeudeAnsichtTexte Ansicht { get; set; } = new();

    /// <summary>GIMP_DLG_GRP_FLAECHEN</summary>
    public string GruppeFlaechen { get; set; } = Resource.GIMP_DLG_GRP_FLAECHEN;

    /// <summary>GIMP_DLG_FILTER_FEHLER</summary>
    public string FilterFehler { get; set; } = Resource.GIMP_DLG_FILTER_FEHLER;

    /// <summary>GIMP_DLG_FILTER_OHNE_GEGENSTUECK</summary>
    public string FilterOhneGegenstueck { get; set; } = Resource.GIMP_DLG_FILTER_OHNE_GEGENSTUECK;

    /// <summary>GIMP_DLG_FILTER_OHNE_UWERT</summary>
    public string FilterOhneUWert { get; set; } = Resource.GIMP_DLG_FILTER_OHNE_UWERT;

    /// <summary>GIMP_DLG_FILTER_BAUTEILBEFUND</summary>
    public string FilterBauteilbefund { get; set; } = Resource.GIMP_DLG_FILTER_BAUTEILBEFUND;

    /// <summary>GIMP_DLG_FILTER_BAUTEILBEFUND_HINWEIS</summary>
    public string FilterBauteilbefundHinweis { get; set; } = Resource.GIMP_DLG_FILTER_BAUTEILBEFUND_HINWEIS;

    /// <summary>GIMP_FL_FLAECHENHERKUNFT_HINWEIS</summary>
    public string FlaechenherkunftHinweis { get; set; } = Resource.GIMP_FL_FLAECHENHERKUNFT_HINWEIS;

    /// <summary>GIMP_DLG_FILTER_ERLAEUTERUNG</summary>
    public string FilterErlaeuterung { get; set; } = Resource.GIMP_DLG_FILTER_ERLAEUTERUNG;

    /// <summary>GIMP_DLG_FILTER_FEHLER_HINWEIS</summary>
    public string FilterFehlerHinweis { get; set; } = Resource.GIMP_DLG_FILTER_FEHLER_HINWEIS;

    /// <summary>GIMP_DLG_FILTER_OHNE_GEGENSTUECK_HINWEIS</summary>
    public string FilterOhneGegenstueckHinweis { get; set; } = Resource.GIMP_DLG_FILTER_OHNE_GEGENSTUECK_HINWEIS;

    /// <summary>GIMP_DLG_FILTER_OHNE_UWERT_HINWEIS</summary>
    public string FilterOhneUWertHinweis { get; set; } = Resource.GIMP_DLG_FILTER_OHNE_UWERT_HINWEIS;

    /// <summary>GIMP_DLG_FLAECHEN_HINWEIS</summary>
    public string FlaechenHinweis { get; set; } = Resource.GIMP_DLG_FLAECHEN_HINWEIS;

    /// <summary>GIMP_DLG_SP_BAUTEIL</summary>
    public string SpalteBauteil { get; set; } = Resource.GIMP_DLG_SP_BAUTEIL;

    /// <summary>GIMP_DLG_SP_ART</summary>
    public string SpalteArt { get; set; } = Resource.GIMP_DLG_SP_ART;

    /// <summary>GIMP_DLG_SP_UWERT</summary>
    public string SpalteUWert { get; set; } = Resource.GIMP_DLG_SP_UWERT;

    /// <summary>GIMP_DLG_SP_AZIMUT</summary>
    public string SpalteAzimut { get; set; } = Resource.GIMP_DLG_SP_AZIMUT;

    /// <summary>GIMP_DLG_SP_NEIGUNG</summary>
    public string SpalteNeigung { get; set; } = Resource.GIMP_DLG_SP_NEIGUNG;

    /// <summary>GIMP_DLG_SP_RAND</summary>
    public string SpalteRand { get; set; } = Resource.GIMP_DLG_SP_RAND;

    /// <summary>GIMP_DLG_KEINE_BAUTEILE</summary>
    public string KeineBauteile { get; set; } = Resource.GIMP_DLG_KEINE_BAUTEILE;

    /// <summary>GIMP_DLG_GRP_BAUSTOFFE — Kopf des Abschnitts mit den Materialnamen der Datei.</summary>
    public string GruppeBaustoffe { get; set; } = Resource.GIMP_DLG_GRP_BAUSTOFFE;

    /// <summary>GIMP_DLG_BAUSTOFFE_HINWEIS</summary>
    public string BaustoffeHinweis { get; set; } = Resource.GIMP_DLG_BAUSTOFFE_HINWEIS;

    /// <summary>GIMP_GRP_AUFBAUTEN — Kopf der Liste „Bauteilaufbauten" (BA-3).</summary>
    public string GruppeAufbauten { get; set; } = Resource.GIMP_GRP_AUFBAUTEN;

    /// <summary>GIMP_AB_SP_AUFBAU</summary>
    public string AbSpalteAufbau { get; set; } = Resource.GIMP_AB_SP_AUFBAU;

    /// <summary>GIMP_AB_SP_ART</summary>
    public string AbSpalteArt { get; set; } = Resource.GIMP_AB_SP_ART;

    /// <summary>GIMP_AB_SP_BAUTEILE</summary>
    public string AbSpalteBauteile { get; set; } = Resource.GIMP_AB_SP_BAUTEILE;

    /// <summary>GIMP_AB_SP_FLAECHE</summary>
    public string AbSpalteFlaeche { get; set; } = Resource.GIMP_AB_SP_FLAECHE;

    /// <summary>GIMP_AB_SP_UDATEI</summary>
    public string AbSpalteUDatei { get; set; } = Resource.GIMP_AB_SP_UDATEI;

    /// <summary>GIMP_AB_SP_USCHICHTEN</summary>
    public string AbSpalteUSchichten { get; set; } = Resource.GIMP_AB_SP_USCHICHTEN;

    /// <summary>GIMP_AB_SP_C1</summary>
    public string AbSpalteC1 { get; set; } = Resource.GIMP_AB_SP_C1;

    /// <summary>GIMP_AB_SP_STUFE</summary>
    public string AbSpalteStufe { get; set; } = Resource.GIMP_AB_SP_STUFE;

    /// <summary>GIMP_AB_SP_FEHLT</summary>
    public string AbSpalteFehlt { get; set; } = Resource.GIMP_AB_SP_FEHLT;

    /// <summary>GIMP_AB_SP_HANDLUNG</summary>
    public string AbSpalteHandlung { get; set; } = Resource.GIMP_AB_SP_HANDLUNG;

    /// <summary>GIMP_AB_NUR_BC — Filter „nur B und C".</summary>
    public string AbNurBC { get; set; } = Resource.GIMP_AB_NUR_BC;

    /// <summary>GIMP_AB_ZUR_BAUSTOFF — Sprung in den Abschnitt „Baustoffe".</summary>
    public string AbZurBaustoff { get; set; } = Resource.GIMP_AB_ZUR_BAUSTOFF;

    /// <summary>GIMP_AB_SCHICHTEN — klappt die Schichten einer Zeile auf.</summary>
    public string AbSchichten { get; set; } = Resource.GIMP_AB_SCHICHTEN;

    /// <summary>GIMP_AB_TYPWAHL — Beschriftung der Typwahl je Ersatzaufbau.</summary>
    public string AbTyp { get; set; } = Resource.GIMP_AB_TYPWAHL;

    /// <summary>GIMP_AB_LEER</summary>
    public string AbLeer { get; set; } = Resource.GIMP_AB_LEER;

    /// <summary>GIMP_AB_HINWEIS</summary>
    public string AbHinweis { get; set; } = Resource.GIMP_AB_HINWEIS;

    /// <summary>GIMP_DLG_SP_MATERIALNAME</summary>
    public string SpalteMaterialname { get; set; } = Resource.GIMP_DLG_SP_MATERIALNAME;

    /// <summary>GIMP_DLG_SP_SCHICHTEN</summary>
    public string SpalteSchichten { get; set; } = Resource.GIMP_DLG_SP_SCHICHTEN;

    /// <summary>GIMP_DLG_SP_ABGLEICH</summary>
    public string SpalteAbgleich { get; set; } = Resource.GIMP_DLG_SP_ABGLEICH;

    /// <summary>GIMP_DLG_SP_BAUSTOFF</summary>
    public string SpalteBaustoff { get; set; } = Resource.GIMP_DLG_SP_BAUSTOFF;

    /// <summary>GIMP_DLG_SP_STOFFWERTE</summary>
    public string SpalteStoffwerte { get; set; } = Resource.GIMP_DLG_SP_STOFFWERTE;

    /// <summary>GIMP_DLG_SP_WERTE_AUS</summary>
    public string SpalteWerteAus { get; set; } = Resource.GIMP_DLG_SP_WERTE_AUS;

    /// <summary>GIMP_DLG_BAUSTOFF_WAEHLEN — Beschriftung der Klappliste je Zeile für die Sprachausgabe, {0} = Materialname.</summary>
    public string BaustoffWaehlen { get; set; } = Resource.GIMP_DLG_BAUSTOFF_WAEHLEN;

    /// <summary>GIMP_DLG_BAUSTOFF_PLATZHALTER — leere Zeile der Klappliste, solange kein Baustoff zugeordnet ist.</summary>
    public string BaustoffPlatzhalter { get; set; } = Resource.GIMP_DLG_BAUSTOFF_PLATZHALTER;

    /// <summary>GIMP_DLG_ZUORDNUNG_ENTFERNEN</summary>
    public string ZuordnungEntfernen { get; set; } = Resource.GIMP_DLG_ZUORDNUNG_ENTFERNEN;

    /// <summary>GIMP_DLG_ZUORDNUNG_ENTFERNEN_TITEL — {0} = Materialname.</summary>
    public string ZuordnungEntfernenTitel { get; set; } = Resource.GIMP_DLG_ZUORDNUNG_ENTFERNEN_TITEL;

    /// <summary>GIMP_DLG_BAUSTOFF_VORGEMERKT — Hinweis an einer Zeile, deren Zuordnung erst beim Speichern gilt.</summary>
    public string BaustoffVorgemerkt { get; set; } = Resource.GIMP_DLG_BAUSTOFF_VORGEMERKT;

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

    // ---- Projektdatei dazuladen (Datenaustauschkonzept 16.4) ----

    /// <summary>GIMP_DLG_SQ_KNOPF</summary>
    public string SqKnopf { get; set; } = Resource.GIMP_DLG_SQ_KNOPF;

    /// <summary>GIMP_DLG_SQ_KNOPF_GRUND</summary>
    public string SqKnopfGrund { get; set; } = Resource.GIMP_DLG_SQ_KNOPF_GRUND;

    /// <summary>GIMP_DLG_SQ_ENTFERNEN</summary>
    public string SqEntfernen { get; set; } = Resource.GIMP_DLG_SQ_ENTFERNEN;

    /// <summary>GIMP_DLG_SQ_UEBERNEHMEN</summary>
    public string SqUebernehmen { get; set; } = Resource.GIMP_DLG_SQ_UEBERNEHMEN;

    /// <summary>GIMP_DLG_SQ_GRUPPE</summary>
    public string SqGruppe { get; set; } = Resource.GIMP_DLG_SQ_GRUPPE;

    /// <summary>GIMP_DLG_SQ_FASSUNG</summary>
    public string SqFassung { get; set; } = Resource.GIMP_DLG_SQ_FASSUNG;

    /// <summary>GIMP_DLG_SQ_RAEUME</summary>
    public string SqRaeume { get; set; } = Resource.GIMP_DLG_SQ_RAEUME;

    /// <summary>GIMP_DLG_SQ_RAEUME_WERT</summary>
    public string SqRaeumeWert { get; set; } = Resource.GIMP_DLG_SQ_RAEUME_WERT;

    /// <summary>GIMP_DLG_SQ_ZONEN</summary>
    public string SqZonen { get; set; } = Resource.GIMP_DLG_SQ_ZONEN;

    /// <summary>GIMP_DLG_SQ_ZONEN_WERT</summary>
    public string SqZonenWert { get; set; } = Resource.GIMP_DLG_SQ_ZONEN_WERT;

    /// <summary>GIMP_DLG_SQ_ZONEN_OFFEN</summary>
    public string SqZonenOffen { get; set; } = Resource.GIMP_DLG_SQ_ZONEN_OFFEN;

    /// <summary>GIMP_DLG_SQ_PROFILE</summary>
    public string SqProfile { get; set; } = Resource.GIMP_DLG_SQ_PROFILE;

    /// <summary>GIMP_DLG_SQ_PROFILE_WERT</summary>
    public string SqProfileWert { get; set; } = Resource.GIMP_DLG_SQ_PROFILE_WERT;

    /// <summary>GIMP_DLG_SQ_JE_GROESSE</summary>
    public string SqJeGroesse { get; set; } = Resource.GIMP_DLG_SQ_JE_GROESSE;

    /// <summary>GIMP_DLG_SQ_UEBERSPRUNGEN</summary>
    public string SqUebersprungen { get; set; } = Resource.GIMP_DLG_SQ_UEBERSPRUNGEN;

    /// <summary>GIMP_DLG_SQ_OHNE_TREFFER</summary>
    public string SqOhneTreffer { get; set; } = Resource.GIMP_DLG_SQ_OHNE_TREFFER;

    /// <summary>GIMP_DLG_SQ_HERKUNFT</summary>
    public string SqHerkunft { get; set; } = Resource.GIMP_DLG_SQ_HERKUNFT;

    /// <summary>GIMP_DLG_SQ_ZONIERUNG</summary>
    public string SqZonierung { get; set; } = Resource.GIMP_DLG_SQ_ZONIERUNG;

    /// <summary>IMP_SQ_ZONIERUNG_DIN</summary>
    public string SqZonierungDin { get; set; } = Resource.IMP_SQ_ZONIERUNG_DIN;

    /// <summary>IMP_SQ_ZONIERUNG_SIM</summary>
    public string SqZonierungSim { get; set; } = Resource.IMP_SQ_ZONIERUNG_SIM;

    /// <summary>GIMP_DLG_SQ_ZONIERUNG_FRAGE</summary>
    public string SqZonierungFrage { get; set; } = Resource.GIMP_DLG_SQ_ZONIERUNG_FRAGE;

    /// <summary>GIMP_DLG_SQ_FRAGE_TITEL</summary>
    public string SqFrageTitel { get; set; } = Resource.GIMP_DLG_SQ_FRAGE_TITEL;

    /// <summary>GIMP_DLG_SQ_FRAGE</summary>
    public string SqFrage { get; set; } = Resource.GIMP_DLG_SQ_FRAGE;

    /// <summary>GIMP_DLG_SQ_KEIN_PLAN</summary>
    public string SqKeinPlan { get; set; } = Resource.GIMP_DLG_SQ_KEIN_PLAN;

    /// <summary>GIMP_DLG_SQ_HINWEIS</summary>
    public string SqHinweis { get; set; } = Resource.GIMP_DLG_SQ_HINWEIS;

    // Die Wahl der Aufbauquelle bei abweichendem Projektstand (Anwenderentscheid vom 08.10.2026)

    /// <summary>GIMP_DLG_AQ_TITEL</summary>
    public string AqTitel { get; set; } = Resource.GIMP_DLG_AQ_TITEL;

    /// <summary>GIMP_DLG_AQ_SATZ</summary>
    public string AqSatz { get; set; } = Resource.GIMP_DLG_AQ_SATZ;

    /// <summary>GIMP_DLG_AQ_ANZEICHEN</summary>
    public string AqAnzeichen { get; set; } = Resource.GIMP_DLG_AQ_ANZEICHEN;

    /// <summary>GIMP_DLG_AQ_SPALTE_ART</summary>
    public string AqSpalteArt { get; set; } = Resource.GIMP_DLG_AQ_SPALTE_ART;

    /// <summary>GIMP_DLG_AQ_SPALTE_ZAHL</summary>
    public string AqSpalteZahl { get; set; } = Resource.GIMP_DLG_AQ_SPALTE_ZAHL;

    /// <summary>GIMP_DLG_AQ_SPALTE_DATEI</summary>
    public string AqSpalteDatei { get; set; } = Resource.GIMP_DLG_AQ_SPALTE_DATEI;

    /// <summary>GIMP_DLG_AQ_SPALTE_PROJEKTDATEI</summary>
    public string AqSpalteProjektdatei { get; set; } = Resource.GIMP_DLG_AQ_SPALTE_PROJEKTDATEI;

    /// <summary>GIMP_DLG_AQ_TABELLE</summary>
    public string AqTabelle { get; set; } = Resource.GIMP_DLG_AQ_TABELLE;

    /// <summary>GIMP_DLG_AQ_ZAHL_WERT</summary>
    public string AqZahlWert { get; set; } = Resource.GIMP_DLG_AQ_ZAHL_WERT;

    /// <summary>GIMP_DLG_AQ_BEISPIELE</summary>
    public string AqBeispiele { get; set; } = Resource.GIMP_DLG_AQ_BEISPIELE;

    /// <summary>GIMP_DLG_AQ_SPALTE_BAUTEIL</summary>
    public string AqSpalteBauteil { get; set; } = Resource.GIMP_DLG_AQ_SPALTE_BAUTEIL;

    /// <summary>GIMP_DLG_AQ_SPALTE_FLAECHE</summary>
    public string AqSpalteFlaeche { get; set; } = Resource.GIMP_DLG_AQ_SPALTE_FLAECHE;

    /// <summary>GIMP_DLG_AQ_U_AUFBAU</summary>
    public string AqUAufbau { get; set; } = Resource.GIMP_DLG_AQ_U_AUFBAU;

    /// <summary>GIMP_DLG_AQ_PROJEKTDATEI</summary>
    public string AqProjektdatei { get; set; } = Resource.GIMP_DLG_AQ_PROJEKTDATEI;

    /// <summary>GIMP_DLG_AQ_DATEI</summary>
    public string AqDatei { get; set; } = Resource.GIMP_DLG_AQ_DATEI;

    /// <summary>GIMP_DLG_AQ_NEU_EXPORT</summary>
    public string AqNeuExport { get; set; } = Resource.GIMP_DLG_AQ_NEU_EXPORT;

    /// <summary>GIMP_DLG_AQ_GEWAEHLT_PROJEKTDATEI</summary>
    public string AqGewaehltProjektdatei { get; set; } = Resource.GIMP_DLG_AQ_GEWAEHLT_PROJEKTDATEI;

    /// <summary>GIMP_DLG_AQ_GEWAEHLT_DATEI</summary>
    public string AqGewaehltDatei { get; set; } = Resource.GIMP_DLG_AQ_GEWAEHLT_DATEI;

    /// <summary>GIMP_DLG_AQ_NEU_EXPORT_TITEL</summary>
    public string AqNeuExportTitel { get; set; } = Resource.GIMP_DLG_AQ_NEU_EXPORT_TITEL;

    /// <summary>GIMP_DLG_AQ_NEU_EXPORT_FRAGE</summary>
    public string AqNeuExportFrage { get; set; } = Resource.GIMP_DLG_AQ_NEU_EXPORT_FRAGE;

    /// <summary>GIMP_DLG_AQ_NEU_EXPORT_JA</summary>
    public string AqNeuExportJa { get; set; } = Resource.GIMP_DLG_AQ_NEU_EXPORT_JA;

    /// <summary>GIMP_DLG_AQ_NEU_EXPORT_NEIN</summary>
    public string AqNeuExportNein { get; set; } = Resource.GIMP_DLG_AQ_NEU_EXPORT_NEIN;

    /// <summary>IMP_SQ_PROT_AUFBAUQUELLE_OFFEN — der Sperrgrund am OK, derselbe Text wie die Prüfung; {0} = Anteil in %.</summary>
    public string AqSperre { get; set; } = Resource.IMP_SQ_PROT_AUFBAUQUELLE_OFFEN;
}

/// <summary>Die sprachneutralen Schlüssel der Schalter des Zuordnungsdialogs (als <c>data-schluessel</c>).</summary>
public static class GebaeudeImportSchalter
{
    /// <summary>Der Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen".</summary>
    public const string RAUMTEMPERATUR_ALS_SOLLWERT = "RaumtemperaturAlsSollwert";
}
