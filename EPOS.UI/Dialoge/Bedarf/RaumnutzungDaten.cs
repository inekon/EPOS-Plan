using System;
using System.Collections.Generic;
using System.Linq;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Art einer Kategorie</b> des Nutzungsprofilkatalogs (Konzept Nutzungsprofile NP-F4). Sie steuert
/// allein Anzeige und Quellenhinweis; der Generator kennt sie nicht. Die Übersetzung in die Kennung des
/// Kerns steht allein in <c>RaumnutzungHuelle</c>.
/// </summary>
public enum RaumnutzungArt
{
    /// <summary>Die ausgelieferten EPOS-Muster.</summary>
    EposMuster = 0,

    /// <summary>DIN V 18599-10 — ausgeliefert ohne Werte.</summary>
    Din18599 = 1,

    /// <summary>SIA 2024 — ausgeliefert leer.</summary>
    Sia2024 = 2,

    /// <summary>VDI 2078 — ausgeliefert leer.</summary>
    Vdi2078 = 3,

    /// <summary>Eine eigene Kategorie des Anwenders.</summary>
    Eigen = 4,
}

/// <summary>
/// <b>Die Art einer Zuordnungszeile</b> (NP-F12): woraus der Schlüssel kommt, mit dem der Import ein Profil
/// findet.
/// </summary>
public enum RaumnutzungZuordnungsart
{
    /// <summary>Die Nummer der Nutzung in der Projektdatei (DIN V 18599-10).</summary>
    DinNummer = 0,

    /// <summary>Die IFC-Klasse eines Raums.</summary>
    IfcKlasse = 1,

    /// <summary>Der Raumtyp einer HottCAD-Datei.</summary>
    HottcadRaumtyp = 2,
}

/// <summary>Die Einheit der Außenluft eines Profils (NP-F10).</summary>
public enum RaumnutzungLuftEinheit
{
    /// <summary>Luftwechsel in 1/h — geht ohne Umrechnung ans Ziel.</summary>
    JeStunde = 0,

    /// <summary>Volumenstrom in m³/(h·m²) — wird mit der lichten Höhe des Ziels umgerechnet.</summary>
    JeFlaeche = 1,
}

/// <summary>Die Tagesart eines Stundenprofils (NP-F9).</summary>
public enum RaumnutzungTagesart
{
    /// <summary>Ein Nutzungstag der Woche.</summary>
    Werktag = 0,

    /// <summary>Ein nutzungsfreier Tag.</summary>
    Frei = 1,
}

/// <summary>
/// <b>Eine Kategorie des Katalogs</b> (Konzept Nutzungsprofile 4.1, NP-F3, NP-F4) — die Zeile von
/// <c>Tab_Raumnutzungskatalog</c> als DTO der Oberfläche, ohne Fachklasse des Kerns.
/// </summary>
/// <param name="Id">Die Id der Kategorie.</param>
/// <param name="Bezeichner">Der Name, eindeutig ohne Unterschied der Schreibung.</param>
/// <param name="Art">Die Art (NP-F4).</param>
/// <param name="Ausgeliefert"><c>ReadOnly = 1</c>: gehört zur Auslieferung, nur duplizierbar (NP-F19).</param>
/// <param name="Beschreibung">Die Beschreibung; leer = ohne.</param>
/// <param name="Quellenhinweis">Der Hinweis auf die Quelle samt Lizenz; leer = ohne.</param>
public sealed record RaumnutzungKategorieDaten(long Id, string Bezeichner, RaumnutzungArt Art, bool Ausgeliefert,
                                               string Beschreibung, string Quellenhinweis)
{
    /// <summary>Trägt die Art eine Norm (dann bleibt sie ausgeliefert ohne Werte)?</summary>
    public bool IstNorm => Art is RaumnutzungArt.Din18599 or RaumnutzungArt.Sia2024 or RaumnutzungArt.Vdi2078;
}

/// <summary>
/// <b>Eine Zeile des Zeilenbilds</b> (NP-F7) — eine Zelle der Vorgabe-Matrix, die ein Profil je Größe
/// wörtlich mitbringt. In dieser Welle nur lesend angezeigt (Bearbeitung in NP4).
/// </summary>
/// <param name="Groesse">Die Größe.</param>
/// <param name="Zeile">Die Zeile der Matrix (Tag, Nacht, Wochenende, Ferien) als Anzeigetext des Kerns.</param>
/// <param name="Wert">Der Wert der Zelle; <c>null</c> = ohne.</param>
/// <param name="Aus">„aus" als Angabe — sie hat Vorrang vor dem Wert.</param>
/// <param name="Von">Stunde 0 … 23 am Anfang des Nachtfensters; <c>null</c> = ohne.</param>
/// <param name="Bis">Das andere Ende des Nachtfensters; <c>null</c> = ohne.</param>
/// <param name="DeltaT">ΔT der Nachtauskühlung [K] an Lüftung/Nacht; <c>null</c> = Vorgabe.</param>
public sealed record RaumnutzungZeilenbildDaten(KonditionierungGroesse Groesse, string Zeile, double? Wert, bool Aus,
                                                int? Von, int? Bis, double? DeltaT);

/// <summary>
/// <b>Ein Stundenprofil</b> (NP-F9) — je Größe und Tagesart vierundzwanzig Werte. In dieser Welle nur
/// lesend angezeigt (Bearbeitung in NP4).
/// </summary>
/// <param name="Groesse">Die Größe.</param>
/// <param name="Tagesart">Werktag oder nutzungsfreier Tag.</param>
/// <param name="Werte">Die vierundzwanzig Werte in Stundenordnung; <c>double.NaN</c> heißt „aus".</param>
public sealed record RaumnutzungStundenDaten(KonditionierungGroesse Groesse, RaumnutzungTagesart Tagesart,
                                             IReadOnlyList<double> Werte);

/// <summary>
/// <b>Ein Nutzungsprofil als Feldsatz der Oberfläche</b> (Konzept Nutzungsprofile 4.1, NP-F1, NP-F6):
/// Kopf, die fünfundzwanzig nullbaren Kennwerte, das Zeilenbild je Größe und die Stundenprofile. Jeder
/// Kennwert darf leer sein und heißt dann „Größe nicht belegt" (NP-F6).
///
/// <para><b>Keine Fachklasse des Kerns</b>: Die Übersetzung nach <c>Raumnutzungsprofil</c> steht allein in
/// <c>RaumnutzungHuelle</c>.</para>
/// </summary>
public sealed class RaumnutzungProfilDaten
{
    /// <summary>Die Id des Profils; 0 = neu.</summary>
    public long Id { get; set; }

    /// <summary>Die Kategorie, in der das Profil steht.</summary>
    public long IdKategorie { get; set; }

    /// <summary>Die Nummer in der Quelle als Text (NP-F5); leer = ohne.</summary>
    public string Nummer { get; set; } = "";

    /// <summary>Der Name, eindeutig je Kategorie ohne Unterschied der Schreibung.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Die Beschreibung; leer = ohne.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Gehört das Profil zur Auslieferung (nur duplizierbar, NP-F19)?</summary>
    public bool Ausgeliefert { get; set; }

    // ---------------------------------------------------------------- Nutzungszeit und Woche

    /// <summary>Nutzungsbeginn, Stunde 0 … 24.</summary>
    public int? NutzungVon { get; set; }

    /// <summary>Nutzungsende, Stunde 0 … 24.</summary>
    public int? NutzungBis { get; set; }

    /// <summary>Betriebsbeginn der Anlage; leer = wie Nutzung.</summary>
    public int? BetriebVon { get; set; }

    /// <summary>Betriebsende der Anlage; leer = wie Nutzung.</summary>
    public int? BetriebBis { get; set; }

    /// <summary>Sieben Ziffern 0/1, Montag bis Sonntag — der Generatoreingang (NP-F8).</summary>
    public string NutzungstageWoche { get; set; } = "";

    /// <summary>Die neun bundeseinheitlichen Feiertage „wie Sonntag".</summary>
    public bool? FeiertageWieSonntag { get; set; }

    // ---------------------------------------------------------------- Sollwerte

    /// <summary>Heizsollwert im Betriebsfenster [°C].</summary>
    public double? HeizSoll { get; set; }

    /// <summary>Heizsollwert außerhalb [°C].</summary>
    public double? HeizSollAusserhalb { get; set; }

    /// <summary>Heizen außerhalb „aus".</summary>
    public bool? HeizAusAusserhalb { get; set; }

    /// <summary>Kühlsollwert im Betriebsfenster [°C].</summary>
    public double? KuehlSoll { get; set; }

    /// <summary>Kühlsollwert außerhalb [°C].</summary>
    public double? KuehlSollAusserhalb { get; set; }

    /// <summary>Kühlen außerhalb „aus".</summary>
    public bool? KuehlAusAusserhalb { get; set; }

    // ---------------------------------------------------------------- Außenluft

    /// <summary>Außenluft im Betriebsfenster in <see cref="LuftEinheit"/>.</summary>
    public double? Aussenluft { get; set; }

    /// <summary>Die Einheit der Außenluft (NP-F10).</summary>
    public RaumnutzungLuftEinheit LuftEinheit { get; set; } = RaumnutzungLuftEinheit.JeStunde;

    /// <summary>Außenluft außerhalb in <see cref="LuftEinheit"/>.</summary>
    public double? AussenluftAusserhalb { get; set; }

    // ---------------------------------------------------------------- Lasten

    /// <summary>Fläche je Person [m²].</summary>
    public double? PersonenFlaeche { get; set; }

    /// <summary>Wärmeabgabe je Person [W]; leer = Vorgabe des Kerns.</summary>
    public double? PersonenWaerme { get; set; }

    /// <summary>Anteil der Personen im Nutzungsfenster 0 … 1; leer = 1.</summary>
    public double? PersonenAnteil { get; set; }

    /// <summary>Anteil der Personen außerhalb 0 … 1; leer = 0.</summary>
    public double? PersonenAnteilAusserhalb { get; set; }

    /// <summary>Gerätelast [W/m²].</summary>
    public double? GeraeteLeistung { get; set; }

    /// <summary>Anteil der Geräte im Nutzungsfenster 0 … 1; leer = 1.</summary>
    public double? GeraeteAnteil { get; set; }

    /// <summary>Anteil der Geräte außerhalb 0 … 1; leer = wie im Fenster.</summary>
    public double? GeraeteAnteilAusserhalb { get; set; }

    /// <summary>Beleuchtung [W/m²] — Anteil der Gerätelast.</summary>
    public double? BeleuchtungLeistung { get; set; }

    /// <summary>Gleichzeitigkeit der Beleuchtung 0 … 1; leer = 1.</summary>
    public double? BeleuchtungAnteil { get; set; }

    // ---------------------------------------------------------------- Zeilenbild und Stunden

    /// <summary>Das Zeilenbild je Größe (NP-F7); leer = ohne.</summary>
    public IReadOnlyList<RaumnutzungZeilenbildDaten> Zeilenbild { get; set; }
        = Array.Empty<RaumnutzungZeilenbildDaten>();

    /// <summary>Die Stundenprofile (NP-F9); leer = ohne.</summary>
    public IReadOnlyList<RaumnutzungStundenDaten> Stunden { get; set; }
        = Array.Empty<RaumnutzungStundenDaten>();

    /// <summary>Die fünfundzwanzig Kennwerte — <c>null</c> heißt „Größe nicht belegt" (NP-F6).</summary>
    public IReadOnlyList<object?> Kennwerte() => new object?[]
    {
        NutzungVon, NutzungBis, BetriebVon, BetriebBis,
        string.IsNullOrEmpty(NutzungstageWoche) ? null : NutzungstageWoche, null /* Nutzungstage im Jahr: abgeleitet, E93 */,
        FeiertageWieSonntag,
        HeizSoll, HeizSollAusserhalb, HeizAusAusserhalb, KuehlSoll, KuehlSollAusserhalb, KuehlAusAusserhalb,
        Aussenluft, AussenluftAusserhalb,
        PersonenFlaeche, PersonenWaerme, PersonenAnteil, PersonenAnteilAusserhalb,
        GeraeteLeistung, GeraeteAnteil, GeraeteAnteilAusserhalb, BeleuchtungLeistung, BeleuchtungAnteil,
    };

    /// <summary>Trägt das Profil weder Kennwert noch Zeilenbild noch Stundenprofil (NP-F13)?</summary>
    public bool IstLeer => Kennwerte().All(w => w is null) && Zeilenbild.Count == 0 && Stunden.Count == 0;

    /// <summary>Eine Kopie — der Editor arbeitet auf ihr und gibt sie erst beim Speichern weiter.</summary>
    public RaumnutzungProfilDaten Kopie()
    {
        var k = (RaumnutzungProfilDaten)MemberwiseClone();
        k.Zeilenbild = Zeilenbild.ToList();
        k.Stunden = Stunden.ToList();
        return k;
    }

    /// <summary>Sprachunabhängige Kurzfassung — Nummer und Name wie im Baum.</summary>
    public override string ToString()
        => (string.IsNullOrEmpty(Nummer) ? "" : Nummer + " ") + Bezeichner;
}

/// <summary>
/// <b>Eine Zuordnungszeile</b> (NP-F12) — Art, Schlüssel und das Profil, auf das sie zeigt; <c>null</c>
/// heißt „keine" und lässt den Import ohne Profil.
/// </summary>
/// <param name="Id">Die Id der Zeile; 0 = neu.</param>
/// <param name="Art">Woraus der Schlüssel kommt.</param>
/// <param name="Schluessel">Der Schlüssel (ganzer Vergleich, getrimmt, ohne Unterschied der Schreibung).</param>
/// <param name="IdProfil">Das Profil oder <c>null</c> für „keine".</param>
/// <param name="Ausgeliefert">Gehört die Zeile zur Auslieferung? Sie bleibt umstellbar (NP-F19).</param>
public sealed record RaumnutzungZuordnungDaten(long Id, RaumnutzungZuordnungsart Art, string Schluessel,
                                               long? IdProfil, bool Ausgeliefert);

/// <summary>
/// <b>Eine Zeile der Vorschau</b> — eine Vorgabezeile der Vorlage, die der Generator aus dem Profil
/// erzeugt, in Anzeigeform.
/// </summary>
/// <param name="Zeile">Die Zeile (Tag, Nacht, Wochenende, Ferien, Standardwoche).</param>
/// <param name="Wert">Der Wert samt Einheit oder „aus".</param>
/// <param name="Fenster">Das Zeitfenster („22 – 6 Uhr"); leer = ohne.</param>
/// <param name="Wochentage">Die Wochentage, für die die Zeile gilt; leer = alle.</param>
public sealed record RaumnutzungVorschauzeile(string Zeile, string Wert, string Fenster, string Wochentage);

/// <summary>
/// <b>Die Vorschau einer Größe</b> — was der Generator aus dem Profil machen würde, ohne etwas zu
/// schreiben (Konzept Nutzungsprofile 6.1 Punkt 2).
/// </summary>
/// <param name="Groesse">Die Größe.</param>
/// <param name="Belegt"><c>false</c> = die Größe ist nicht belegt, das Ziel behält seinen Kalender (NP-F6).</param>
/// <param name="Weg">Woher die Zeilen kommen (Zeilenbild, Stundenprofil, Kennwerte) als Anzeigetext.</param>
/// <param name="Zeilen">Die erzeugten Zeilen.</param>
/// <param name="Nennwert">Der Nennwert samt Herleitung (NP-F18); leer = ohne.</param>
/// <param name="Hinweis">Was benannt wird, ohne abzulehnen; leer = nichts.</param>
public sealed record RaumnutzungVorschauGroesse(KonditionierungGroesse Groesse, bool Belegt, string Weg,
                                                IReadOnlyList<RaumnutzungVorschauzeile> Zeilen,
                                                string Nennwert, string Hinweis);

/// <summary>
/// <b>Die Vorschau eines Profils</b> über alle fünf Größen samt der Zeile „erzeugt: … Nutzungstage,
/// Quelle: …" (NP-F8).
/// </summary>
/// <param name="Profilname">Der Name, den die Übernahme ans Ziel schreibt (NP-F14).</param>
/// <param name="Groessen">Je Größe das Ergebnis des Generators.</param>
/// <param name="Nutzungstage">Die Zeile des Tagesvergleichs; leer = ohne.</param>
/// <param name="Hinweise">Die Hinweise über alle Größen, jeder einmal.</param>
public sealed record RaumnutzungVorschau(string Profilname, IReadOnlyList<RaumnutzungVorschauGroesse> Groessen,
                                         string Nutzungstage, IReadOnlyList<string> Hinweise)
{
    /// <summary>Die leere Vorschau.</summary>
    public static RaumnutzungVorschau Keine { get; } = new("", Array.Empty<RaumnutzungVorschauGroesse>(), "",
                                                           Array.Empty<string>());
}

/// <summary>
/// <b>Das Ergebnis einer Handlung</b> des Katalogs — Ablehnungen kommen benannt als Text (NP-F11, NP-F19,
/// NP-F20), nie still.
/// </summary>
/// <param name="Ok">Hat die Handlung geschrieben?</param>
/// <param name="Meldung">Der Grund einer Ablehnung; leer = keiner.</param>
/// <param name="Id">Die Id des angelegten Satzes; 0 = keiner.</param>
public sealed record RaumnutzungErgebnis(bool Ok, string Meldung, long Id = 0)
{
    /// <summary>Die Handlung hat geschrieben.</summary>
    public static RaumnutzungErgebnis Gut { get; } = new(true, "");

    /// <summary>Die Handlung hat geschrieben und einen Satz angelegt.</summary>
    public static RaumnutzungErgebnis MitId(long id) => new(true, "", id);

    /// <summary>Die Handlung ist benannt abgelehnt.</summary>
    public static RaumnutzungErgebnis Fehler(string meldung) => new(false, meldung ?? "", 0);
}

/// <summary>
/// <b>Der Weg der Nutzungsprofile</b> — alles, was das Blatt „Nutzungsprofile" vom Kern braucht, als EIN
/// Parameter (Muster <see cref="KonditionierungWeg"/>).
///
/// <para><b>Kein Delegat, kein Knopf.</b> Je Handlung ein Delegat; fehlt er, bietet das Blatt den Knopf
/// nicht an. Das leere Bündel bietet keinen — so steht das Blatt auch ohne Katalogtabellen da und nennt
/// seinen Grund.</para>
/// <para><b>Der Katalog schreibt SOFORT</b> (NP-F3): Er steht projektübergreifend einmal in der Datenbank
/// und reist nicht im Feldsatz eines Gebäudes mit; jede Handlung des Blatts schreibt für sich, eine leise
/// Zeile sagt es. Die Datenbankseite steht in <c>RaumnutzungCtrl</c>, die Übersetzung in
/// <c>RaumnutzungHuelle</c>; die Vorschau rechnet der Generator ohne zu schreiben.</para>
/// </summary>
public sealed class RaumnutzungWeg
{
    /// <summary>Das leere Bündel: Es bietet keinen Knopf an.</summary>
    public static RaumnutzungWeg Keiner { get; } = new();

    /// <summary>Der Grund, warum der Katalog nicht zu haben ist (fehlende Tabellen); <c>null</c> = er ist da.</summary>
    public string? Sperrgrund { get; init; }

    /// <summary>Profile aus einer Projektdatei in eine eigene Kategorie (NP4b, Q46); <c>null</c> = kein Knopf.</summary>
    public ProjektdateiProfileWeg? Projektdatei { get; init; }

    /// <summary>Die Kategorien in der Reihenfolge des Kerns — die ausgelieferten zuerst.</summary>
    public Func<IReadOnlyList<RaumnutzungKategorieDaten>>? Kategorien { get; init; }

    /// <summary>Die Profile einer Kategorie, sortiert nach Nummer, dann Name.</summary>
    public Func<long, IReadOnlyList<RaumnutzungProfilDaten>>? Profile { get; init; }

    /// <summary>Die Zuordnungszeilen über alle Arten.</summary>
    public Func<IReadOnlyList<RaumnutzungZuordnungDaten>>? Zuordnungen { get; init; }

    /// <summary>
    /// Die Vorschau eines Profils (Konzept Nutzungsprofile 6.1): der Generator über dem Feldsatz des
    /// Editors, ohne Datenbank und ohne zu schreiben. Zweiter Parameter ist die Fläche eines gedachten
    /// Ziels für die Nennwertzeile, dritter die lichte Höhe für die Außenluft in m³/(h·m²).
    /// </summary>
    public Func<RaumnutzungProfilDaten, double?, double?, RaumnutzungVorschau>? Vorschau { get; init; }

    /// <summary>
    /// Die Nutzungstage im Jahr des Feldsatzes, abgeleitet aus Wochenmuster und Feiertagen (E93) — keine Eingabe; der
    /// Kern zählt sie im Raster von 365 Tagen. <c>null</c> = ohne Feldsatz.
    /// </summary>
    public Func<RaumnutzungProfilDaten, int?>? Nutzungstage { get; init; }

    /// <summary>Eine eigene Kategorie anlegen (NP-F19).</summary>
    public Func<string, string, string, RaumnutzungErgebnis>? KategorieAnlegen { get; init; }

    /// <summary>Eine eigene Kategorie umbenennen (NP-F19).</summary>
    public Func<long, string, string, string, RaumnutzungErgebnis>? KategorieAendern { get; init; }

    /// <summary>Eine Kategorie samt Profilen in eine eigene Kategorie duplizieren (NP-F19).</summary>
    public Func<long, string, RaumnutzungErgebnis>? KategorieDuplizieren { get; init; }

    /// <summary>Eine eigene Kategorie löschen (NP-F19).</summary>
    public Func<long, RaumnutzungErgebnis>? KategorieLoeschen { get; init; }

    /// <summary>Ein eigenes Profil anlegen.</summary>
    public Func<RaumnutzungProfilDaten, RaumnutzungErgebnis>? ProfilAnlegen { get; init; }

    /// <summary>Ein eigenes Profil ändern — Grenzen werden benannt abgelehnt (NP-F11).</summary>
    public Func<RaumnutzungProfilDaten, RaumnutzungErgebnis>? ProfilAendern { get; init; }

    /// <summary>Ein Profil in eine Kategorie duplizieren — der Weg, ein Normprofil zu befüllen (NP-F19).</summary>
    public Func<long, long?, string, RaumnutzungErgebnis>? ProfilDuplizieren { get; init; }

    /// <summary>Ein eigenes Profil löschen; Zuordnungen darauf fallen auf „keine" (NP-F19).</summary>
    public Func<long, RaumnutzungErgebnis>? ProfilLoeschen { get; init; }

    /// <summary>Wie viele Zuordnungszeilen auf ein Profil zeigen — die Rückfrage vor dem Löschen.</summary>
    public Func<long, int>? ZuordnungenAuf { get; init; }

    /// <summary>Eine Zuordnungszeile setzen oder anlegen; <c>null</c> als Profil heißt „keine".</summary>
    public Func<RaumnutzungZuordnungsart, string, long?, RaumnutzungErgebnis>? ZuordnungSetzen { get; init; }

    /// <summary>Eine eigene Zuordnungszeile löschen.</summary>
    public Func<long, RaumnutzungErgebnis>? ZuordnungLoeschen { get; init; }

    /// <summary>Bietet das Bündel die Verwaltung des Katalogs an (Lesen genügt nicht)?</summary>
    public bool MitKatalog => Kategorien is not null && Profile is not null;
}
