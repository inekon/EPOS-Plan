using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Import;

// =====================================================================================
//  Die DTO des Zuordnungsdialogs „Nutzungsprofile zuordnen…“ (Entwurf Zonenzuordnung IFC,
//  Abschnitt 6; Auftrag ZZ-B). Fertige Anzeigetexte und sprachneutrale Schlüssel — die
//  Komponente kennt weder Raumprofilbefund noch Katalogknoten des Kerns.
// =====================================================================================

/// <summary>Die sprachneutralen Stände eines Bündels (<see cref="GebaeudeProfilbuendelDaten.Stand"/>).</summary>
public static class GebaeudeProfilstand
{
    /// <summary>Genau ein Kandidat ohne eigene Angabe der Datei — automatisch gesetzt.</summary>
    public const string EINDEUTIG = "EINDEUTIG";
    /// <summary>Ein Vorschlag, den der Anwender bestätigt (F2, F5, F4).</summary>
    public const string VORSCHLAG = "VORSCHLAG";
    /// <summary>Mehrere Kandidaten ohne Vorschlag — Rückfrage mit den Kandidaten (F3).</summary>
    public const string OFFEN = "OFFEN";
    /// <summary>Kein Kandidat — Rückfrage mit dem ganzen Katalog.</summary>
    public const string OHNE = "OHNE";
    /// <summary>Von Hand gewählt oder bestätigt.</summary>
    public const string HAND = "HAND";
}

/// <summary>Die Ampel der Profilzuordnung (F6): grün alles gesetzt, gelb unbestätigte Vorschläge, rot Rückfragen offen.</summary>
public static class GebaeudeProfilampel
{
    /// <summary>Jeder Raum trägt ein gesetztes Profil (eindeutig oder von Hand).</summary>
    public const string GRUEN = "GRUEN";
    /// <summary>Unbestätigte Vorschläge — der Abschluss ist erlaubt (F6).</summary>
    public const string GELB = "GELB";
    /// <summary>Räume mit Rückfrage (offen oder ohne Kandidat).</summary>
    public const string ROT = "ROT";
}

/// <summary>
/// <b>Ein Bündel des Zuordnungsdialogs</b>: die Räume gleicher Raumart und gleichen Namensstamms mit ihrem gemeinsamen Befund.
/// </summary>
public sealed record GebaeudeProfilbuendelDaten
{
    /// <summary>Der sprachneutrale Schlüssel des Bündels (Raumart und Stamm) — Schlüssel der Auswahl.</summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Der Anzeigename („Büroraum“).</summary>
    public string Anzeigename { get; init; } = "";

    /// <summary>Die Raumart der Datei als Anzeigetext („Raumart Office“); leer ohne.</summary>
    public string Raumart { get; init; } = "";

    /// <summary>Die Raumkennungen der Datei — das Ziel der Schritte.</summary>
    public IReadOnlyList<string> Raeume { get; init; } = Array.Empty<string>();

    /// <summary>Die Fläche als Spanne kleinster bis größter Raum („12–61“), ohne Einheit.</summary>
    public string Flaeche { get; init; } = "";

    /// <summary>Der Stand (<see cref="GebaeudeProfilstand"/>).</summary>
    public string Stand { get; init; } = "";

    /// <summary>Der Stand als Anzeigetext.</summary>
    public string StandText { get; init; } = "";

    /// <summary>Weichen die Räume voneinander ab (etwa nach einer Wahl an einem Einzelraum)?</summary>
    public bool Gemischt { get; init; }

    /// <summary>Der Profilschlüssel des Vorschlags bzw. des gesetzten Profils; <c>null</c> = keiner.</summary>
    public string? Profil { get; init; }

    /// <summary>Das Profil als Anzeigetext; leer = keins.</summary>
    public string ProfilText { get; init; } = "";

    /// <summary>Die Begründungskette als Text.</summary>
    public string Begruendung { get; init; } = "";

    /// <summary>Die Kandidaten (Profilschlüssel, Anzeigetext) in Normreihenfolge — die Schnellwahl einer Rückfrage.</summary>
    public IReadOnlyList<GebaeudeZonenregelDaten> Kandidaten { get; init; } = Array.Empty<GebaeudeZonenregelDaten>();

    /// <summary>Braucht das Bündel eine Rückfrage (offen oder ohne Kandidat)?</summary>
    public bool Offen => Stand is GebaeudeProfilstand.OFFEN or GebaeudeProfilstand.OHNE;
}

/// <summary>Ein Knoten des Katalogbaums: Kategorie → Gruppe der Norm → Profil; ein Profil trägt seinen Profilschlüssel.</summary>
/// <param name="Schluessel">Eindeutig im ganzen Baum.</param>
/// <param name="Text">Der Anzeigetext.</param>
/// <param name="Kinder">Leer = Profil.</param>
/// <param name="Profilschluessel">Der Profilschlüssel eines Profils; <c>null</c> an Kategorie und Gruppe.</param>
/// <param name="Kennwerte">Kennwerte des Profils als Kurztext (Sollwert, Nutzungszeit); leer = keine.</param>
public sealed record GebaeudeProfilknotenDaten(string Schluessel, string Text, IReadOnlyList<GebaeudeProfilknotenDaten> Kinder,
                                               string? Profilschluessel = null, string Kennwerte = "");

/// <summary>Die Wahl eines Profils für Räume: Raumkennungen und Profilschlüssel (leer = „keine Nutzung“).</summary>
public sealed record GebaeudeProfilwahl(IReadOnlyList<string> Raeume, string Profil);

/// <summary>
/// <b>Die Profilzuordnung eines Zonenplans</b> (ZZ-B): Bündel (offene zuerst), Katalogbaum, Zähler je Stand in Räumen und
/// die Ampel (F6). <c>null</c> am Plan, wenn die Datei keine Raumprofile trägt.
/// </summary>
public sealed record GebaeudeProfilzuordnungDaten
{
    /// <summary>Die Bündel, offene zuerst.</summary>
    public IReadOnlyList<GebaeudeProfilbuendelDaten> Buendel { get; init; } = Array.Empty<GebaeudeProfilbuendelDaten>();

    /// <summary>Der Katalog als Baum.</summary>
    public IReadOnlyList<GebaeudeProfilknotenDaten> Katalog { get; init; } = Array.Empty<GebaeudeProfilknotenDaten>();

    /// <summary>Räume mit eindeutigem Profil.</summary>
    public int Eindeutig { get; init; }

    /// <summary>Räume mit unbestätigtem Vorschlag.</summary>
    public int Vorschlag { get; init; }

    /// <summary>Räume mit Rückfrage (mehrere Kandidaten).</summary>
    public int Rueckfrage { get; init; }

    /// <summary>Räume ohne Kandidat.</summary>
    public int Ohne { get; init; }

    /// <summary>Räume mit Wahl von Hand.</summary>
    public int Hand { get; init; }

    /// <summary>Die Zahl der Bündel mit Rückfrage — der Zähler am Knopf.</summary>
    public int OffeneBuendel { get; init; }

    /// <summary>Die Zähler als Anzeigetext.</summary>
    public string Zaehlertext { get; init; } = "";

    /// <summary>Die Ampel (<see cref="GebaeudeProfilampel"/>).</summary>
    public string Ampel { get; init; } = GebaeudeProfilampel.GRUEN;

    /// <summary>Der Hinweis zur Ampel.</summary>
    public string Ampeltext { get; init; } = "";
}

/// <summary>Die Beschriftungen des Zuordnungsdialogs (Präfix <c>GIMP_PZ_</c>).</summary>
public sealed class GebaeudeProfilzuordnungTexte
{
    /// <summary>GIMP_PZ_TITEL</summary>
    public string Titel { get; set; } = Resource.GIMP_PZ_TITEL;
    /// <summary>GIMP_PZ_KNOPF</summary>
    public string Knopf { get; set; } = Resource.GIMP_PZ_KNOPF;
    /// <summary>GIMP_PZ_KNOPF_OFFEN</summary>
    public string KnopfOffen { get; set; } = Resource.GIMP_PZ_KNOPF_OFFEN;
    /// <summary>GIMP_PZ_KATALOG</summary>
    public string Katalog { get; set; } = Resource.GIMP_PZ_KATALOG;
    /// <summary>GIMP_PZ_SUCHE</summary>
    public string Suche { get; set; } = Resource.GIMP_PZ_SUCHE;
    /// <summary>GIMP_PZ_KATALOG_LEER</summary>
    public string KatalogLeer { get; set; } = Resource.GIMP_PZ_KATALOG_LEER;
    /// <summary>GIMP_PZ_BUENDEL_TITEL</summary>
    public string BuendelTitel { get; set; } = Resource.GIMP_PZ_BUENDEL_TITEL;
    /// <summary>GIMP_PZ_SP_BUENDEL</summary>
    public string SpalteBuendel { get; set; } = Resource.GIMP_PZ_SP_BUENDEL;
    /// <summary>GIMP_PZ_SP_RAEUME</summary>
    public string SpalteRaeume { get; set; } = Resource.GIMP_PZ_SP_RAEUME;
    /// <summary>GIMP_PZ_SP_FLAECHE</summary>
    public string SpalteFlaeche { get; set; } = Resource.GIMP_PZ_SP_FLAECHE;
    /// <summary>GIMP_PZ_SP_STAND</summary>
    public string SpalteStand { get; set; } = Resource.GIMP_PZ_SP_STAND;
    /// <summary>GIMP_PZ_SP_VORSCHLAG</summary>
    public string SpalteVorschlag { get; set; } = Resource.GIMP_PZ_SP_VORSCHLAG;
    /// <summary>GIMP_PZ_GEMISCHT</summary>
    public string Gemischt { get; set; } = Resource.GIMP_PZ_GEMISCHT;
    /// <summary>GIMP_PZ_KEINE</summary>
    public string Keine { get; set; } = Resource.GIMP_PZ_KEINE;
    /// <summary>GIMP_PZ_NUR_OFFENE</summary>
    public string NurOffene { get; set; } = Resource.GIMP_PZ_NUR_OFFENE;
    /// <summary>GIMP_PZ_ALLE_UEBERNEHMEN</summary>
    public string AlleUebernehmen { get; set; } = Resource.GIMP_PZ_ALLE_UEBERNEHMEN;
    /// <summary>GIMP_PZ_ALLE_ZURUECK</summary>
    public string AlleZuruecknehmen { get; set; } = Resource.GIMP_PZ_ALLE_ZURUECK;
    /// <summary>GIMP_PZ_UEBERNEHMEN</summary>
    public string Uebernehmen { get; set; } = Resource.GIMP_PZ_UEBERNEHMEN;
    /// <summary>GIMP_PZ_AUFHEBEN</summary>
    public string Aufheben { get; set; } = Resource.GIMP_PZ_AUFHEBEN;
    /// <summary>GIMP_PZ_SCHNELLWAHL</summary>
    public string Schnellwahl { get; set; } = Resource.GIMP_PZ_SCHNELLWAHL;
    /// <summary>GIMP_PZ_HINWEIS_WAHL</summary>
    public string HinweisWahl { get; set; } = Resource.GIMP_PZ_HINWEIS_WAHL;
    /// <summary>GIMP_PZ_GEWAEHLT</summary>
    public string Gewaehlt { get; set; } = Resource.GIMP_PZ_GEWAEHLT;
    /// <summary>GIMP_PZ_KEIN_BUENDEL</summary>
    public string KeinBuendel { get; set; } = Resource.GIMP_PZ_KEIN_BUENDEL;
}
