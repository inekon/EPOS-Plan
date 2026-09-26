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
public sealed record GebaeudeZuordnungsanfrage(
    int Gebaeudeindex,
    int? Baualtersklasse,
    IReadOnlyDictionary<string, bool> BeheiztUebersteuert,
    IReadOnlyDictionary<string, double?>? Handwerte = null,
    IReadOnlyDictionary<string, int?>? Baustoffzuordnungen = null,
    string? Zonenregel = null);

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

    /// <summary>Die Zeile zur inneren Masse (Innenbauteile, Innenflächenfaktor aus der Datei oder Vorgabe); leer = keine.</summary>
    public string Innenweg { get; init; } = "";

    /// <summary>Die Meldungen des Vorschlags.</summary>
    public IReadOnlyList<GebaeudeImportMeldung> Meldungen { get; init; } = Array.Empty<GebaeudeImportMeldung>();
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
public sealed record GebaeudeFlaechenzeileDaten(Katalogfilterzeile Zeile, bool Fehler, bool OhneGegenstueck, bool OhneUWert);

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
}

/// <summary>
/// Der Stand einer Zuordnung, wie ihn die Hülle aus dem Kern baut: Kopfzeile, Namensvorschlag,
/// Raumliste, Zeilen, Meldungen, der Bauteilvorschlag samt Abschnitt „Baustoffe" — und der
/// Herkunftstext für eine Handänderung, damit auch „manuell" aus den Gaben kommt.
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

    /// <summary>Die Zonierung; <c>null</c>, wenn die Datei nur eine Zone je Gebäude trägt (Einzonenweg wie gehabt).</summary>
    public GebaeudeZonierungDaten? Zonierung { get; init; }
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
public sealed record GebaeudeImportErgebnis(
    int Gebaeudeindex,
    int? Baualtersklasse,
    string Gebaeudename,
    IReadOnlyDictionary<string, bool> BeheiztUebersteuert,
    IReadOnlyList<GebaeudeFeldzeileDaten> Zeilen,
    bool AlsZone = false,
    IReadOnlyDictionary<string, int?>? Baustoffzuordnungen = null,
    string? Zonenregel = null)
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

    /// <summary>GIMP_DLG_ZONE_AUFKLAPPEN — {0} = Zone.</summary>
    public string ZoneAufklappen { get; set; } = Resource.GIMP_DLG_ZONE_AUFKLAPPEN;

    /// <summary>GIMP_DLG_ZONE_ZUKLAPPEN — {0} = Zone.</summary>
    public string ZoneZuklappen { get; set; } = Resource.GIMP_DLG_ZONE_ZUKLAPPEN;

    /// <summary>GIMP_DLG_ZONE_BEHEIZT — Beschriftung des Hakens je Zone für die Sprachausgabe, {0} = Zone.</summary>
    public string ZoneBeheizt { get; set; } = Resource.GIMP_DLG_ZONE_BEHEIZT;

    /// <summary>GIMP_DLG_ZONEN_HINWEIS</summary>
    public string ZonenHinweis { get; set; } = Resource.GIMP_DLG_ZONEN_HINWEIS;

    /// <summary>GIMP_DLG_GRP_FLAECHEN</summary>
    public string GruppeFlaechen { get; set; } = Resource.GIMP_DLG_GRP_FLAECHEN;

    /// <summary>GIMP_DLG_FILTER_FEHLER</summary>
    public string FilterFehler { get; set; } = Resource.GIMP_DLG_FILTER_FEHLER;

    /// <summary>GIMP_DLG_FILTER_OHNE_GEGENSTUECK</summary>
    public string FilterOhneGegenstueck { get; set; } = Resource.GIMP_DLG_FILTER_OHNE_GEGENSTUECK;

    /// <summary>GIMP_DLG_FILTER_OHNE_UWERT</summary>
    public string FilterOhneUWert { get; set; } = Resource.GIMP_DLG_FILTER_OHNE_UWERT;

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
}
