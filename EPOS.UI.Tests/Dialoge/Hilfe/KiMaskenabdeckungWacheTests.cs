using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Der WÄCHTER über die Maskenabdeckung des Hilfe-Assistenten (Welle #458, Stufe 1;
/// Entscheid KI‑D‑Q11: „Alle Masken außer den nicht sinnvoll steuerbaren sollen steuerbar
/// sein").
///
/// <para><b>Die Regel.</b> Jede Razor-Komponente unter <c>EPOS.UI/Dialoge</c> und
/// <c>EPOS.UI/Seiten</c>, die ein Eingabefeld trägt, ist auf GENAU einem von drei Wegen
/// abgedeckt:</para>
/// <list type="number">
///   <item><description>Sie meldet sich selbst an (<c>KiMaskenanmeldung.Fuer</c> in der
///     Datei oder ihrem Code-Behind).</description></item>
///   <item><description>Sie ist ein Baustein, dessen Felder in der Feldkarte ihres
///     WIRTS stehen — Eintrag in <see cref="WIRTE"/>, und der Wirt meldet an.</description></item>
///   <item><description>Sie steht mit Grund in der Ausnahmeliste des Kerns
///     (<see cref="KiDialogAusnahmen.Alle"/>).</description></item>
/// </list>
///
/// <para><b>Was als Eingabefeld zählt:</b> die Eingabebausteine des Hauses
/// (<c>Standards/</c>: <c>Zahlenfeld</c>, <c>Ganzzahlfeld</c>, <c>Textfeld</c> ohne
/// <c>NurLesen="true"</c>, <c>Auswahlfeld</c>, <c>Suchauswahl</c>, <c>Datumsfeld</c>,
/// <c>Farbfeld</c>, <c>Schalter</c>; <c>Bausteine/</c>: <c>Optionsgruppe</c>,
/// <c>EnergietraegerWahl</c>, <c>Katalogfelder</c> ohne <c>NurLesen="true"</c>) und nackte
/// <c>input</c>/<c>select</c>/<c>textarea</c>. <c>Dateiwahl</c> ist ein Dateidialog und
/// kein Einstellwert; Listen und Filter (<c>Katalogliste</c>, <c>Mehrfachauswahl</c>,
/// <c>Spaltenfilter</c>, <c>Vergleichswahl</c>, <c>Zeilenwahl</c>) sind Auswahlen, keine
/// Werte. Kommentare (<c>@* … *@</c>, <c>&lt;!-- --&gt;</c>, <c>//</c>-Zeilen) zählen
/// nicht — dieselbe Regel wie bei der Markup-Probe in <see cref="KiDialogkatalogTests"/>.</para>
///
/// <para><b>Die Gegenrichtung — die EINGABEBILANZ.</b> Eine angemeldete Maske kann eine
/// Eingabe tragen, die der Katalog nicht kennt (der Schalter „Kühlbetrieb" stand so
/// zwei Wellen lang in der Simulationsansicht). Für die Masken mit Markup-Probe
/// (<see cref="KiDialogkatalogTests.Markupdateien"/>) muss deshalb jede gebundene
/// Eigenschaft im Katalog stehen oder mit Grund in <see cref="BewusstDraussen"/>; für
/// alle übrigen angemeldeten und gehosteten Dateien steht die ZAHL ihrer Eingabestellen
/// in <see cref="EINGABESTELLEN"/> fest — jede neue Eingabe erzwingt damit einen
/// Entscheid: Katalog nachziehen oder den Grund eintragen.</para>
///
/// <para><b>Die Grenze der Wache:</b> Sie liest Markup, keinen kompilierten Baum — wie
/// <see cref="KnopfleistenWacheTests"/>. Ob ein Feld in einem Zweig steht, der nie
/// gezeichnet wird, sieht sie nicht; dafür hat jede Maske ihren bunit-Zeugen.</para>
/// </summary>
public sealed class KiMaskenabdeckungWacheTests
{
    // =====================================================================
    //  Die Wirte-Tabelle: Baustein -> Wirt, der anmeldet
    // =====================================================================

    /// <summary>
    /// Ein Baustein ohne eigene Anmeldung, dessen Felder der Wirt anmeldet. Der Wirt
    /// ist die Komponente, die <c>KiMaskenanmeldung.Fuer</c> ruft; dazwischen dürfen
    /// weitere Bausteine liegen (<c>BrennstoffBestandteile</c> steht in
    /// <c>EnergietraegerEinstellungen</c>, das in <c>EnergietraegerDialog</c> steht).
    /// </summary>
    private readonly record struct Wirt(string Kind, string Wirtkomponente, string Maske);

    /// <summary>Die Bausteine und ihre Wirte.</summary>
    private static readonly Wirt[] WIRTE =
    {
        new("PvModellFelder",                 "PhotovoltaikDialog",          KiMaskennamen.PHOTOVOLTAIK),
        new("WaermepumpeStammFelder",         "WaermepumpeStammDialog",      KiMaskennamen.WAERMEPUMPE),
        new("WaermepumpeKonfiguration",       "WaermepumpeAnlageDialog",     KiMaskennamen.WAERMEPUMPE_ANLAGE),
        new("EnergietraegerEinstellungen",    "EnergietraegerDialog",        KiMaskennamen.ENERGIETRAEGER),
        new("BrennstoffBestandteile",         "EnergietraegerDialog",        KiMaskennamen.ENERGIETRAEGER),
        new("StrompreisDetails",              "EnergietraegerDialog",        KiMaskennamen.ENERGIETRAEGER),
        new("ErtragBonus",                    "KostenKomponenteDialog",      KiMaskennamen.KOSTENVERWALTUNG),
        new("VorlagenZeile",                  "KostenKomponenteDialog",      KiMaskennamen.KOSTENVERWALTUNG),
        new("SpeicherAuslegungEditor",        "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("SpeicherFlottenBetriebEditor",   "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("SpeicherFlottenEditor",          "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("SpeicherFlottenNetzBlock",       "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("SpeicherFlottenWirtschaftBlock", "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("LeistungspreisBlock",            "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("OptimierungBlock",               "StromspeicherAuslegungSeite", KiMaskennamen.STROMSPEICHER_AUSLEGUNG),
        new("SimulationKonfigSeite",          "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("SpeicherParameterBlock",         "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("ErgebnisReiter",                 "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("ErzeugerReiter",                 "Startseite",                  KiMaskennamen.STARTSEITE),

        // Welle #458, Stufe 2: die Ergebnisblaetter der Simulation melden ihre
        // Anzeigeschalter beim Register ihrer Seite an (Ergebnisblattwirt); die Maske
        // Simulation fuehrt sie als Spalte „anzeige", das Blatt als Wahlfeld „reiter".
        new("BedarfReiter",                   "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("BhkwReiter",                     "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("HeizkesselReiter",               "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("PhotovoltaikReiter",             "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("SolarthermieReiter",             "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("StromgangReiter",                "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("StromspeicherReiter",            "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("WaermegangReiter",               "SimulationSeite",             KiMaskennamen.SIMULATION),
        new("WaermepumpeReiter",              "SimulationSeite",             KiMaskennamen.SIMULATION),

        // Der Abschnitt „Verlauf" der Wirtschaftlichkeitsseite: Zeitraum und Haken.
        new("KapitalwertVerlaufAbschnitt",    "WirtschaftlichkeitSeite",     KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE),

        // ETAPPE E17 (V-G11): die Liste der nicht monetarisierbaren Wirkungen im Bewertungsblock -
        // Spalten wirkung_* der Maske Wirtschaftlichkeitsseite.
        new("WirkungenListe",                 "WirtschaftlichkeitSeite",     KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE),

        // Welle #465: Hülle, Fenster, Kenngrößen und „Alle Daten" des Gebäude-Stammblatts -
        // die Felder des Katalogeditors; die Verwaltung meldet sie als Form_Gebaeude_Admin an.
        new("GebaeudeStammblattFelder",       "GebaeudeAdminDialog",         KiMaskennamen.GEBAEUDE_ADMIN),

        // Anlagenkopplung AK1 Welle 3 (Konzept 9.1): die Gruppe „Wärmeübergabe" - ein Baustein,
        // zwei Wirte: der Katalogeditor (Form_Gebaeude1) und über GebaeudeStammblattFelder das
        // Stammblatt der Verwaltung (Form_Gebaeude_Admin); beide melden dieselben dreizehn Felder
        // über GebaeudeKatalogKiSicht an. Die Tabelle führt je Kind EINEN Wirt.
        new("GebaeudeWaermeuebergabeFelder",  "GebaeudeKatalogDialog",       KiMaskennamen.GEBAEUDE_KATALOG),

        // Gebäudesimulation G3, Welle C: das Schichtenraster eines Aufbaus - die Verwaltung der
        // Bauteilaufbauten meldet seine Spalten (Schichten[]) über BauteilaufbauKiSicht an.
        new("BauteilschichtenFelder",         "BauteilaufbauDialog",         KiMaskennamen.BAUTEILAUFBAU),
        // E37 (Anlagenkopplung 8.1): der Unterabschnitt „Kühlübergabe" der Gruppe „Kühlung" -
        // derselbe Baustein in beiden Wirten, dieselben acht Felder über GebaeudeKatalogKiSicht.
        new("GebaeudeKuehluebergabeFelder",   "GebaeudeKatalogDialog",       KiMaskennamen.GEBAEUDE_KATALOG),

        // Berichtsvorlagen BV-E1 (Konzept 9.7, 10.2): der Platzhalterkatalog steht als
        // Überlagerung IN der Berichtsseite; seine Suche führt der Wirt (Suche/SucheChanged)
        // und meldet sie über BerichtSeiteKiSicht.Katalogsuche an.
        new("PlatzhalterkatalogDialog",       "BerichtSeite",                KiMaskennamen.BERICHTSEITE)
    };

    // =====================================================================
    //  Die Eingabebilanz der Masken OHNE Markup-Probe: die Zahl je Datei
    // =====================================================================

    /// <summary>
    /// Die Zahl der Eingabestellen einer angemeldeten oder gehosteten Datei, die in keiner
    /// Markup-Probe steht; <paramref name="Vermerk"/> nennt, was davon bewusst draußen ist.
    /// </summary>
    private readonly record struct Eingabestellen(string Komponente, int Zahl, string Vermerk = "");

    /// <summary>
    /// <b>Die Zahlen des Bestands.</b> Ändert sich eine, hat die Datei eine Eingabe
    /// bekommen oder verloren: Katalog und Sichtklasse nachziehen (oder den Grund in den
    /// Vermerk schreiben) und die Zahl anpassen — nie nur die Zahl.
    /// </summary>
    private static readonly Eingabestellen[] EINGABESTELLEN =
    {
        // ---- ETAPPE E9b (vollständige Szenarioabdeckung, Pflege in den Dialogen) ----
        // Ein eigener Block, damit die Nachzüge anderer Wellen an der Tafel konfliktarm
        // bleiben. Die Szenariotafel des Parameterdialogs trägt zwei Zeilen mehr —
        // Betrachtungszeitraum und Mengenänderung je Best/Worst, vier Eingabestellen —, und
        // die Feldkarte (KiDialoge, Maske Form_WirtschaftlichkeitParameter) führt sie mit
        // (best_zeitraum, worst_zeitraum, best_menge, worst_menge): 26 → 30.
        // ETAPPE E15 (V-G7): die Gruppe „Risiko" - Art, Zinszuschlag, R_loss und p_loss,
        // vier Eingabestellen, die Feldkarte führt sie mit (risiko_art,
        // risiko_zinszuschlag, risiko_verlust, risiko_wahrscheinlichkeit): 30 → 34.
        // ETAPPE E19 (Konzept § 6.3 Nr. 33): die Unternehmensart in der Gruppe „Strom",
        // nur ohne BHKW sichtbar - eine Eingabestelle, die Feldkarte führt sie mit
        // (unternehmensart, mit BHKW benannt abgelehnt): 34 → 35.
        new("WirtschaftlichkeitParameterDialog", 35),
        // ---- Ende ETAPPE E9b ----

        // ---- ETAPPE E10 (Nutzungsdauer Stufe S3) ----
        // Die Nutzungsdauern (AfA) zeigen die zwei Saetze Instandsetzung und Wartung je
        // Zeile und in der Neuzeile - vier Eingabestellen mehr. Die Feldkarte (KiDialoge,
        // Maske Form_Nutzungsdauer) fuehrt sie mit: die Kopffelder neue_instandsetzung und
        // neue_wartung, die Spalten instandsetzung und wartung (8 -> 12).
        new("NutzungsdauerDialog", 12),
        // ---- Ende ETAPPE E10 ----

        // ---- ETAPPE E17 (V-G11, nicht monetarisierbare Wirkungen) ----
        // Der Freitext der Seite (ein mehrzeiliges Textfeld) weicht der Liste im Baustein
        // WirkungenListe: je Zeile Kategorie, Beschreibung, Dauer und drei Wirkungsgrade -
        // sechs Eingabestellen im Markup. Die Feldkarte (KiDialoge, Maske
        // Wirtschaftlichkeitsseite) fuehrt sie als Spalten wirkung_kategorie ...
        // wirkung_umwelt, dazu wirkung_anzahl und die Anzeige wirkung_beurteilung
        // (WirtschaftlichkeitSeite 9 -> 8).
        new("WirkungenListe", 6),
        // ---- Ende ETAPPE E17 ----

        // ---- Gebäudesimulation G3, Welle C (Baustoffe, Bauteilaufbauten) ----
        // Die sieben Kenndaten der Baustoffverwaltung stehen EINMAL als Fragment und dienen dem
        // Stammblatt und „Neu…" (Aktion Anlegen); der Schalter „nur herstellerneutral" ist der
        // Trichter der Spalte Hersteller, kein Maskenfeld. Die Aufbauverwaltung führt vier
        // Kopffelder, das Schichtenraster als Baustein sechs (Baustoff, Dicke, λ, ρ, c_p,
        // Luftschicht - die Spalten schicht_* der Feldkarte).
        new("BaustoffKatalogDialog", 8, "der Werkzeugschalter „nur herstellerneutral“ setzt den Filter der Liste"),
        new("BauteilaufbauDialog", 4),
        new("BauteilschichtenFelder", 6),
        // ---- Ende Gebäudesimulation G3, Welle C ----

        // ---- Gebäudesimulation G3, Welle D2 (Zone und Bauteil) ----
        // Der Zonendialog führt Bezeichnung, Nutzfläche und (Stufe G6b) die vierzehn Werte der Zone;
        // seine Bauteile sind ein Raster zum Lesen (Bauteile[] der Feldkarte), angelegt und geöffnet
        // wird mit Klicks. Der Bauteildialog führt vierzehn Maskenfelder (Art, Bezeichnung, Fläche,
        // Azimut, Neigung, Randbedingung, Nachbarzone, Zuordnung, g-Wert, Rahmenanteil, Verschattung,
        // ψ·L, U-Wert, Aufbau im Projekt); die Suchauswahl des Katalogaufbaus ist bewusst draußen.
        new("ZonenDialog", 16),
        new("BauteilDialog", 15, "die Suchauswahl „Aufbau aus dem Katalog“ wählt nur vor; die Kopie ins Projekt ist ein Klick auf „Übernehmen“"),
        // Stufe G6b (W2): der Luftaustausch zwischen den Zonen - ein Raster, je Zeile Zone A, Zone B
        // und V̇; die Zonen liest der Assistent nur.
        new("LuftaustauschDialog", 3, "Zone A und Zone B wählt der Anwender; der Assistent liest sie und setzt den Volumenstrom"),
        // Stufe G7a (W3): der Gebaeudeexport - die Postleitzahl und die Bestaetigung der Meldungen;
        // die Bestaetigung liest der Assistent nur, setzen kann sie allein der Anwender.
        new("GebaeudeExportDialog", 2, "die Bestätigung der Meldungen ist ein Katalogfeld nur zum Lesen"),
        // ---- Ende Gebäudesimulation G3, Welle D2 ----

        new("BedarfAdminDialog", 3),
        new("BedarfErgebnisDialog", 4),
        new("BedarfReiter", 3),
        new("BedarfsProfileDialog", 3),
        new("BedarfstagKonstruktor", 10, "Zapfprofil Z4, Gruppe 2b: Bezugsart und Bezugsmenge des Tags (Felder bezugsart, bezugsmenge)"),
        // Berichtsvorlagen BV-E1 (Konzept 10.2): die Vorlagenwahl der Gruppe „Vorlage" (2 → 3) -
        // das Katalogfeld „vorlage" der Maske Berichtsseite (KiDialoge.BerichtVorlagenfeld) über
        // BerichtSeiteKiSicht.Vorlage samt VorlageWahl.
        new("BerichtSeite", 4, "die Vorlagenwahl ist das Katalogfeld vorlage (BerichtSeiteKiSicht.Vorlage samt VorlageWahl); die Musterwahl von „Neue Vorlage…“ (Standardvorlage oder Kurzbericht) steht in der Überlagerung und wählt nur die Quelle einer Kopie"),
        new("BhkwWirtschaftlichkeitDialog", 39),
        new("BhkwReiter", 5),
        new("BrennstoffBestandteile", 2),
        new("CaseEingabeDialog", 7),
        // Berichtsvorlagen BV-E1 (Konzept 10.3): die Firma der Rubrik „Bericht" (9 → 10) - das
        // Katalogfeld bericht_firma über EinstellungenKiSicht.BerichtFirma; der Vorlagenordner ist
        // eine Dateiwahl und zählt hier nicht, steht aber als bericht_vorlagenordner im Katalog.
        // BV-E2 (Entscheid BV-E2-1): das Logo der Rubrik „Bericht" ist eine Dateiwahl und zählt hier nicht
        // (die Zahl bleibt 10), steht aber als Katalogfeld bericht_logo über EinstellungenKiSicht.BerichtLogo.
        new("EinstellungenDialog", 10, "Datenbankname und KI-Abschalter bleiben draußen (Datenbankwechsel beim nächsten Start; der Assistent schaltet sich nicht selbst ab); die fünf Ordner sind Dateiwahlen ohne Katalogfeld; der Vorlagenordner ist eine Dateiwahl mit Katalogfeld bericht_vorlagenordner, die Firma das Katalogfeld bericht_firma (BV-E1), das Logo eine Dateiwahl mit Katalogfeld bericht_logo (BV-E2)"),
        new("EmissionskatalogDialog", 11),
        new("EnergietraegerDialog", 4),
        new("EnergietraegerEinstellungen", 21),
        new("EnergietraegerVarianteDialog", 2),
        new("ErgebnisReiter", 1),
        new("ErzeugerReiter", 1),
        new("ErtragBonus", 2),
        // Welle #465: die Kenndaten des Stammblatts samt Wohnfläche und Bauart (Katalogfelder
        // gebaeudetyp, gebaeudeart, baualtersklasse, verwendung, wohnflaeche, bauart,
        // beschreibung); die übrigen Felder des Katalogeditors trägt GebaeudeStammblattFelder.
        // G4a Welle 3: dazu das Baujahr neben der Baualtersklasse (Katalogfeld baujahr): 7 → 8.
        // E47: der Energiestandard (Katalogfeld energiestandard): 8 → 9.
        new("GebaeudeAdminDialog", 9),
        // G6b W5: die Diagrammwahl je Zone (Katalogfeld diagramm): 2 → 3.
        new("GebaeudeBedarfDialog", 3),
        // G3 Welle K: die vier Filterfelder sind Suche und Trichter der Katalogliste (Baustein) -
        // die Katalogfelder verwendung, filter_gebaeudeart, filter_baujahr und suche binden über
        // GebaeudeKiSicht auf den Filterstand; eigene Eingabestellen trägt die Maske keine mehr: 4 → 0.
        new("GebaeudeDialog", 0, "Suche und Trichter der Katalogliste (Baustein); Katalogfelder verwendung, " +
            "filter_gebaeudeart, filter_baujahr, suche über den Filterstand"),
        // G4a Welle 3: das Baujahr neben der Baualtersklasse (Katalogfeld baujahr): 45 → 46.
        // E43: Beginn und Ende der Nachtabsenkung (Katalogfelder nacht_beginn, nacht_ende): 46 → 48.
        // E47: der Energiestandard (Katalogfeld energiestandard): 48 → 49.
        new("GebaeudeKatalogDialog", 49),
        new("GebaeudeKuehluebergabeFelder", 8, "die acht Felder der Kühlübergabe (E37) - Katalogfelder kuehluebergabe_aktiv, " +
            "kuehl_uebergabe_art, kuehl_uebergabe_exponent, kuehl_uebergabe_nennleistung, kuehl_auslegung_*, kuehl_vorlaufgrenze"),
        // E43: Beginn und Ende der Nachtabsenkung (Katalogfelder nacht_beginn, nacht_ende): 36 → 38.
        new("GebaeudeStammblattFelder", 38, "die Felder des Katalogeditors (Maske Form_Gebaeude_Admin); die Fensterzeile " +
            "des Hüll-Rasters ist gerechnet, die Ferien sind die Spalten ferien_*"),
        new("GebaeudeWaermeuebergabeFelder", 13, "Schnellwahl und freies Feld des Proportionalbands sind EIN Katalogfeld " +
            "(proportionalband); das Zeitprogramm ist das Feld sollwertprofil und steht im Baustein Wochenraster"),
        new("GebaeudeWohnflaecheDialog", 5, "der Schalter „dezentral“ steht in beiden Zweigen (mit und ohne Zone) - EIN Katalogfeld"),
        new("GebaeudetypDialog", 6, "Name, Beschreibung und Kurvenzahl von „Neu…“ gehören zur Aktion Anlegen (KI‑D‑Q11)"),
        new("GesetzeskatalogDialog", 1),
        new("GesetzeskatalogZeileDialog", 7),
        new("HeizkesselReiter", 4),
        new("KatalogBrowserDialog", 2, "Stammblatt über die Feldtafel des Profils"),
        new("KapitalwertVerlaufAbschnitt", 3),
        new("KennlinienEditorDialog", 7),
        new("KlimadatenDialog", 7),
        new("KomponentenKonfigurationDialog", 3),
        new("KostenKomponenteDialog", 3),
        new("KostenSeite", 0),
        new("KostenfaktorKatalogDialog", 1),
        new("KostenprofilDialog", 4),
        new("LeistungspreisBlock", 2),
        new("LeistungspreisReiheDialog", 2),
        new("ModulKatalogDialog", 4, "der Feldsatz des Profils, gebaut über den RenderTreeBuilder"),
        new("OptimierungBlock", 9),
        new("PeakShavingDialog", 17),
        new("PhotovoltaikDialog", 5, "„Alle Daten“ über die Feldtafel des Modulprofils"),
        new("PhotovoltaikReiter", 5),
        new("PhotovoltaikVerguetungDialog", 16),
        // Berichtsvorlagen BV-E1: die Suche des Platzhalterkatalogs - der Wirt BerichtSeite führt
        // sie und meldet sie als BerichtSeiteKiSicht.Katalogsuche an; das Feld „Schreibweise"
        // ist nur lesbar und zählt nicht.
        new("PlatzhalterkatalogDialog", 1, "die Katalogsuche steht über den Wirt als BerichtSeiteKiSicht.Katalogsuche bereit; Katalogfeld katalogsuche der Maske Berichtsseite im Kern nachzuziehen (BV-E1)"),
        new("ProjektKopfSeite", 5),
        new("ProjektKopieDialog", 4),
        new("ProjektVarianteDialog", 2),
        new("PufferSpProjektDialog", 20),
        new("PvModellFelder", 3),
        new("QuelleErdreichDialog", 9),
        new("QuellePufferspeicherDialog", 8),
        new("QuellprofilDialog", 6, "die Tagwahl des nur lesenden Wochengangs (Altweg) ist ein Anzeigeschalter; die 365 bzw. 8 760 Werte " +
            "der Betriebsarten Tag und Stunde sind Zeitreihen (Dateiweg) und stehen in keinem Eingabefeld"),
        new("SimulationKonfigSeite", 4),
        new("SimulationSeite", 0),
        new("SolarganglinieDialog", 0),
        new("SolarthermieReiter", 3),
        new("SpeicherAuslegungEditor", 16),
        new("SpeicherFlottenBetriebEditor", 7),
        new("SpeicherFlottenEditor", 28),
        new("SpeicherFlottenNetzBlock", 8),
        new("SpeicherFlottenWirtschaftBlock", 6),
        new("SpeicherParameterBlock", 20),
        new("SpeicherZeitreihenDialog", 16),
        new("Startseite", 2, "die Projekt- und Variantenwahl im Kopfband öffnet ein anderes Projekt — Navigation, kein Einstellwert"),
        new("StromganglinieAdminDialog", 1),
        new("StromgangReiter", 1, "die Serienauswahl (Mehrfachauswahl) steht als Anzeigeschalter mit im Katalog"),
        new("StrompreisDetails", 3),
        new("StromspeicherAuslegungSeite", 0),
        new("StromspeicherReiter", 2),
        // Zapfprofil Z4, Gruppe 2b: der Tagesgang-Editor (Tagtyp, Stundenanteile und Wochenfaktoren als
        // Zahlenreihen, Vorlage, Katalogversion der Kopie).
        new("TagesgangEditor", 5),
        new("TarifstrukturDialog", 13),
        // Zapfprofil Z4, Gruppe 3: der Katalog der Nutzungsarten ist lesend (Stammblatt), sein Editor
        // führt Kennung, Bezugsart, Tagesgangsatz, Bedarf samt Bandbreite, Bezugstemperaturen,
        // Bilanzgrenze, Kalender, Ferienfaktor und Monatsfaktoren (Schleifen je einmal gezählt).
        // Zapfprofil ZU32: der Schalter „Nur prüfen, nichts schreiben" der Importüberlagerung ist
        // keine Eingabe des Katalogs — er wählt die Betriebsart EINES Knopfdrucks und wird mit der
        // Überlagerung wieder zurückgesetzt; gespeichert wird er nie.
        new("TwwNutzungsartAdminDialog", 1, "Schalter der Betriebsart eines Knopfdrucks, kein Einstellwert der Maske"),
        new("TwwNutzungsartEditor", 13),
        // Zapfprofil Z4b, Gruppe 2: der Dialog der eingespielten VDI-4655-Typtage zeigt den Stand
        // und den Pruefbericht; die Paketwahl ist ein Dateidialog, Einspielen und Loeschen sind
        // Handlungen - kein Einstellwert.
        new("TwwTyptagImportDialog", 0),
        // Zapfprofil Z5, Gruppe 3: der Messdaten-Dialog. Seine fünf Eingaben BESCHREIBEN die
        // gewählte Datei (Bezeichnung, Quelle, gemessene Größe, Lückenschwelle, Zeitrechnung der
        // Zeitstempel) und sind ohne sie ohne Sinn; die Dateiwahl selbst ist ein Dateidialog,
        // Einspielen und Löschen sind Handlungen — kein Einstellwert der Maske.
        new("TwwMessreihenDialog", 5, "Angaben zur gewählten Datei, kein Einstellwert der Maske "
            + "(Grund je Bindung in BewusstDraussen)"),
        new("TypProfilDialog", 2),
        new("UebersichtSeite", 4),
        // ETAPPE E16 (V-G3): das Ganzzahlfeld „Zahlung alle … Jahre" der Betriebsseite; die
        // Feldkarte (KiDialoge, Maske Form_VorlagenPosition) führt es als wiederholperiode (8 -> 9).
        new("VorlagenPositionDialog", 9),
        new("VorlagenZeile", 4),
        new("WaermegangReiter", 3, "die Erzeuger- und die Speicherauswahl (Mehrfachauswahl) stehen als Anzeigeschalter mit im Katalog"),
        new("WaermepumpeAnlageDialog", 5, "der Schalter „mit Kennlinien übernehmen“ gehört zur Aktion Übernehmen"),
        new("WaermepumpeKonfiguration", 14, "das nackte input ist der weich gesperrte Kühlschalter (Grund im title) — " +
            "derselbe Wert wie „kuehlbetrieb“, kein eigenes Feld"),
        new("WaermepumpeReiter", 8),
        new("WaermesenkeDialog", 9),
        // WirtschaftlichkeitParameterDialog: siehe Block ETAPPE E9b oben.
        new("WirtschaftlichkeitSeite", 8, "der Schalter der Vergleichsgruppe ist eine Menge von Verweisen, kein Feldwert; " +
            "Stand und Szenario der Zahlungsreihen (Block 2, ETAPPE E8a) führt die Feldkarte als Anzeigewahlen"),
        // Zapfprofil Z4, Gruppe 2b: der Kategorien-Editor (Katalogversion und sechs Spalten des Rasters)
        // und die neun Eingaben des Verfahrensvergleichs der Auslegung (lade_modus … fuellstand_bezug).
        new("ZapfkategorienEditor", 7),
        new("ZapfprofilAuslegungDialog", 20),
        new("ZapfprofilDialog", 59, "Stufen Erweitert und Experte (Z4): die Angaben der gewählten Zone und des Gebäudes " +
            "samt Wohnungstabelle in der Feldkarte; das Bundesland ist gesperrt (ohne Kalendertabelle) und zählt nicht. " +
            "Dazu die drei Eingaben der Wahl des Typtagwegs (Z4b): Schalter, Klimazone und Gebäudeart, und die Wahl " +
            "der Messreihe des Vergleichs (Z5): Feld messreihe der Feldkarte")
    };

    /// <summary>
    /// Masken, deren Markup-Datei zugleich Felder einer ANDEREN Maske trägt: Die Bilanz
    /// nimmt deren Katalog mit. <c>PvStraengeFelder</c> zeichnet die Überlagerung
    /// „Anlagenwerte" UND das Strangraster von <c>Form_PV</c> (Sichtklasse
    /// <c>PhotovoltaikKiSicht</c>, deshalb ohne eigene Markup-Probe).
    /// </summary>
    private static readonly Dictionary<string, string[]> MITMASKEN = new()
    {
        [KiMaskennamen.PV_ANLAGENWERTE] = new[] { KiMaskennamen.PHOTOVOLTAIK }
    };

    // =====================================================================
    //  Die Eingabebilanz der Masken MIT Markup-Probe: was bewusst draußen ist
    // =====================================================================

    /// <summary>
    /// Je Maske mit Markup-Probe die gebundenen Ausdrücke, die NICHT im Katalog stehen —
    /// mit Grund. Der Schlüssel ist der Bindungsausdruck, wie er im Markup steht (ohne
    /// führendes <c>@</c>, Leerraum zusammengezogen).
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> BewusstDraussen = new()
    {
        [KiMaskennamen.WAERMEPUMPE] = new()
        {
            ["NurMitKuehlung"] =
                "Filterschalter der Katalogliste (Trichter auf die Kühlleistung) — eine Listenauswahl, kein Einstellwert",
            ["(_kuehlung ? 1 : 0)"] =
                "wählt nur das gezeigte Kennfeld (Wärme oder Kühlung) — ein Bildschalter"
        },
        [KiMaskennamen.PV_ANLAGENWERTE] = new()
        {
            ["_tKalt"] =
                "Auslegungstemperatur des Projekts — Feld auslegung_kalt der Maske Form_PV, deren Sichtklasse das " +
                "lebende Feld liest",
            ["_tHeiss"] =
                "Auslegungstemperatur des Projekts — Feld auslegung_heiss der Maske Form_PV, deren Sichtklasse das " +
                "lebende Feld liest",
            ["_hersteller"] = "Herstellerfilter für „Gerät hinzufügen“ — eine Listenauswahl der Aktion",
            ["_neuesGeraet"] = "Gerätewahl für „Gerät hinzufügen“ — eine Aktion, die Sätze anlegt",
            ["ModulAuswahl(s)"] =
                "Modul je Strang — ein Verweis in den Modulkatalog je Zeile; Mengen von Verweisen bleiben außen vor " +
                "(KI‑D‑Q6)",
            ["GeraetAuswahl(s)"] =
                "Wechselrichter je Strang — ein Verweis in den Gerätekatalog je Zeile; Mengen von Verweisen bleiben " +
                "außen vor (KI‑D‑Q6)"
        }

        // Welle #458 Stufe 3b: Die zwölf Monatswerte des Bedarfskopfsatzes
        // (Daten.Monat[monat]) stehen als Zahlenreihe „monatswerte" im Katalog.
    };

    // =====================================================================
    //  Die Wache
    // =====================================================================

    [Fact]
    public void Jede_Maske_mit_Eingabefeldern_ist_angemeldet_oder_ausgenommen()
    {
        IReadOnlyDictionary<string, Komponente> bestand = Bestand();
        var kinder = new HashSet<string>(WIRTE.Select(w => w.Kind), StringComparer.Ordinal);

        var offen = new List<string>();
        var doppelt = new List<string>();

        foreach (Komponente k in bestand.Values.Where(k => k.ImGeltungsbereich && k.Eingaben.Count > 0))
        {
            int wege = (k.Angemeldet ? 1 : 0)
                       + (kinder.Contains(k.Name) ? 1 : 0)
                       + (KiDialogAusnahmen.Finde(k.Name) is not null ? 1 : 0);

            if (wege == 0) offen.Add(k.Repopfad + " (" + Zusammenfassung(k) + ")");
            if (wege > 1) doppelt.Add(k.Repopfad);
        }

        Assert.True(offen.Count == 0,
            "Diese Masken tragen Eingabefelder, melden sich aber nicht beim Assistenten an, " +
            "haben keinen Wirt und stehen nicht in KiDialogAusnahmen.Alle. Entweder anmelden " +
            "(KiMaskenanmeldung.Fuer, Katalogeintrag in KiDialoge, Ziel in KiMaskenziele), " +
            "den Wirt in WIRTE eintragen oder die Maske mit Grund in die Ausnahmeliste:\n  " +
            string.Join("\n  ", offen));

        Assert.True(doppelt.Count == 0,
            "Diese Masken sind auf mehr als einem Weg abgedeckt (angemeldet, Wirt, Ausnahme) — " +
            "genau einer gilt:\n  " + string.Join("\n  ", doppelt));
    }

    /// <summary>
    /// <b>Eine Ausnahme, die ins Leere zeigt, wird gestrichen</b>: Die Datei gibt es, sie
    /// trägt Eingabefelder, und sie meldet nicht an. Sonst wäre die Liste ein Friedhof, auf
    /// dem eine angemeldete Maske als „nicht steuerbar" weiterlebte.
    /// </summary>
    [Fact]
    public void Jede_Ausnahme_trifft_heute_zu()
    {
        IReadOnlyDictionary<string, Komponente> bestand = Bestand();
        var falsch = new List<string>();

        foreach (KiAusnahme a in KiDialogAusnahmen.Alle)
        {
            if (!bestand.TryGetValue(a.Komponente, out Komponente? k) || !k.ImGeltungsbereich)
                falsch.Add(a.Komponente + ": keine Razor-Datei unter EPOS.UI/Dialoge oder EPOS.UI/Seiten");
            else if (k.Eingaben.Count == 0)
                falsch.Add(a.Komponente + ": trägt keine Eingabefelder — der Eintrag gehört gestrichen");
            else if (k.Angemeldet)
                falsch.Add(a.Komponente + ": meldet sich an — der Eintrag gehört gestrichen");
        }

        Assert.True(falsch.Count == 0, "Diese Ausnahmen treffen nicht mehr zu:\n  " + string.Join("\n  ", falsch));
    }

    /// <summary>
    /// <b>Der Wirt meldet an, nicht das Blatt darin</b> (<c>EPOS.UI/CLAUDE.md</c>, Dialoge):
    /// Jeder Wirt der Tabelle ruft <c>KiMaskenanmeldung.Fuer</c>, zeichnet sein Kind
    /// (unmittelbar oder über weitere Bausteine), und das Kind trägt Eingabefelder, meldet
    /// selbst nicht an und steht nicht auf der Ausnahmeliste.
    /// </summary>
    [Fact]
    public void Jeder_Wirt_meldet_an()
    {
        IReadOnlyDictionary<string, Komponente> bestand = Bestand();
        var falsch = new List<string>();

        foreach (Wirt w in WIRTE)
        {
            if (!bestand.TryGetValue(w.Kind, out Komponente? kind))
            {
                falsch.Add(w.Kind + ": keine Razor-Datei");
                continue;
            }

            if (!bestand.TryGetValue(w.Wirtkomponente, out Komponente? wirt))
            {
                falsch.Add(w.Kind + ": Wirt " + w.Wirtkomponente + " hat keine Razor-Datei");
                continue;
            }

            if (!wirt.Angemeldet) falsch.Add(w.Kind + ": Wirt " + w.Wirtkomponente + " meldet nicht an");
            if (kind.Angemeldet) falsch.Add(w.Kind + ": meldet selbst an — der Eintrag gehört gestrichen");
            if (kind.Eingaben.Count == 0) falsch.Add(w.Kind + ": trägt keine Eingabefelder");
            if (!Erreichbar(bestand, w.Wirtkomponente, w.Kind))
                falsch.Add(w.Kind + ": steht nicht (auch nicht mittelbar) im Markup von " + w.Wirtkomponente);
            if (!KiDialoge.Katalog.Kennt(w.Maske))
                falsch.Add(w.Kind + ": die Maske " + w.Maske + " steht nicht im Katalog");
        }

        Assert.Equal(WIRTE.Length, WIRTE.Select(w => w.Kind).Distinct(StringComparer.Ordinal).Count());
        Assert.True(falsch.Count == 0, "Die Wirte-Tabelle stimmt nicht:\n  " + string.Join("\n  ", falsch));
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Wache_findet_ihren_Bestand()
    {
        IReadOnlyDictionary<string, Komponente> bestand = Bestand();
        List<Komponente> geltung = bestand.Values.Where(k => k.ImGeltungsbereich).ToList();

        Assert.True(geltung.Count > 140, "Nur " + geltung.Count + " Razor-Dateien gefunden.");
        Assert.True(geltung.Count(k => k.Angemeldet) > 60,
                    "Nur " + geltung.Count(k => k.Angemeldet) + " Anmeldungen gefunden.");
        Assert.True(geltung.Count(k => k.Eingaben.Count > 0) > 100,
                    "Nur " + geltung.Count(k => k.Eingaben.Count > 0) + " Dateien mit Eingabefeldern.");
        Assert.True(KiDialogAusnahmen.Alle.Count > 20, "Nur " + KiDialogAusnahmen.Alle.Count + " Ausnahmen.");
    }

    /// <summary>
    /// Die GEGENPROBE (Lehre W6‑B‑1: eine Wache, die nie rot werden kann, prüft nichts):
    /// Der Zähler findet jeden Baustein, übersieht Kommentare, gesperrte Textfelder,
    /// Dateiwahlen und Typnamen im Code — und liest Bindungen mit verschachtelten
    /// Anführungszeichen.
    /// </summary>
    [Fact]
    public void Der_Zaehler_findet_Eingaben_und_uebersieht_das_Uebrige()
    {
        string text =
            "<Zahlenfeld Bezeichnung=\"@A\" Wert=\"@Daten.Ptherm\" WertChanged=\"X\" />\n" +
            "<Textfeld Bezeichnung=\"B\" Wert=\"@Daten.Name\" NurLesen=\"true\" />\n" +
            "<Textfeld Bezeichnung=\"B\" Wert=\"@Daten.Firma\" NurLesen=\"@_gesperrt\" />\n" +
            "<Auswahlfeld Auswahl=\"@(_z is not null ? _z.CarrierId : null)\" Eintraege=\"@(new[] { \"a>b\" })\" />\n" +
            "<Dateiwahl Wert=\"@Pfad\" />\n" +
            "@* <Schalter Wert=\"@Weg\" /> *@\n" +
            "<!-- <Ganzzahlfeld Wert=\"@Weg\" /> -->\n" +
            "    // <Datumsfeld Wert=\"@Weg\" />\n" +
            "<Katalogfelder Felder=\"@_felder\" NurLesen=\"true\" />\n" +
            "<select @bind=\"_wahl\"><option>1</option></select>\n" +
            "@code { private IReadOnlyList<EnergietraegerWahl.Eintrag> _e; List<Schalter> _s; }\n" +
            "<Schalter\n    Bezeichnung=\"S\"\n    Wert=\"@Daten.Brennwert\" />\n" +
            "<Schalter Bezeichnung=\"Gesperrt\" Wert=\"@Daten.Anzeige\" Aktiv=\"false\" />\n";

        List<Eingabe> e = Eingaben(OhneKommentare(text));

        Assert.Equal(new[] { "Zahlenfeld", "Textfeld", "Auswahlfeld", "select", "Schalter" },
                     e.Select(x => x.Baustein).ToArray());
        Assert.Equal(new[] { "Daten.Ptherm", "Daten.Firma", "(_z is not null ? _z.CarrierId : null)",
                             "_wahl", "Daten.Brennwert" },
                     e.Select(x => x.Bindung).ToArray());

        // Die Bindung trifft den Katalog über den Eigenschaftsnamen — auch in einem Ausdruck.
        var katalog = new HashSet<string>(StringComparer.Ordinal) { "Ptherm", "CarrierId" };
        Assert.True(Gedeckt(e[0].Bindung, katalog));
        Assert.False(Gedeckt(e[1].Bindung, katalog));
        Assert.True(Gedeckt(e[2].Bindung, katalog));
        Assert.False(Gedeckt("Daten.PthermAlt", katalog));
        Assert.True(Gedeckt("PthermIndex", katalog));

        // Auch was der Code über den RenderTreeBuilder zeichnet, ist eine Eingabe.
        Assert.Equal(new[] { "Ganzzahlfeld", "input" },
                     Eingaben("builder.OpenComponent<Ganzzahlfeld>(10);\nbuilder.OpenElement(2, \"input\");\n" +
                              "builder.OpenComponent<Herleitungszeile>(3);")
                         .Select(x => x.Baustein).ToArray());
    }

    // =====================================================================
    //  Die Eingabebilanz
    // =====================================================================

    /// <summary>
    /// <b>Jede gebundene Eingabe einer Maske mit Markup-Probe steht im Katalog — oder mit
    /// Grund in <see cref="BewusstDraussen"/>.</b> Die Markup-Probe
    /// (<see cref="KiDialogkatalogTests"/>) fragt Katalog → Markup; das hier ist die
    /// Gegenrichtung Markup → Katalog.
    /// </summary>
    [Theory]
    [MemberData(nameof(KiDialogkatalogTests.Markupdateien), MemberType = typeof(KiDialogkatalogTests))]
    public void Jede_Eingabe_einer_Markupmaske_steht_im_Katalog_oder_bewusst_draussen(string maske, string dateien)
    {
        HashSet<string> katalog = Eigenschaften(maske);
        if (MITMASKEN.TryGetValue(maske, out string[]? mit))
            foreach (string m in mit) katalog.UnionWith(Eigenschaften(m));
        BewusstDraussen.TryGetValue(maske, out Dictionary<string, string>? draussen);

        var fehlt = new List<string>();
        foreach (string datei in Dateien(dateien))
            foreach (Eingabe e in Eingaben(OhneKommentare(Lesen(datei))))
            {
                if (Gedeckt(e.Bindung, katalog)) continue;
                if (draussen is not null && draussen.ContainsKey(e.Bindung)) continue;

                // Der Aufklapper „Alle Daten" führt seine Felder als Daten eines Profils:
                // Gedeckt ist er, wenn die Maske eine FELDTAFEL führt — den Feldbestand
                // hält der Profilwächter (Welle #458, Stufe 2).
                if (e.Baustein == "Katalogfelder" && KiDialogkatalogTests.FuehrtFeldtafel(maske)) continue;
                fehlt.Add(datei + ": " + e.Baustein + " → „" + e.Bindung + "“");
            }

        Assert.True(fehlt.Count == 0,
            "Diese Eingaben der Maske '" + maske + "' stehen nicht im Katalog. Entweder die " +
            "Feldkarte nachziehen oder die Bindung mit Grund in BewusstDraussen eintragen:\n  " +
            string.Join("\n  ", fehlt));
    }

    /// <summary>
    /// Ein Eintrag in <see cref="BewusstDraussen"/> muss heute zutreffen: Die Bindung steht
    /// in einer Datei der Maske, und der Katalog deckt sie NICHT — sonst wird er gestrichen.
    /// </summary>
    [Fact]
    public void Jeder_Eintrag_in_BewusstDraussen_trifft_heute_zu()
    {
        var zuordnung = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (object[] zeile in KiDialogkatalogTests.Markupdateien())
            zuordnung[(string)zeile[0]] = (string)zeile[1];

        var falsch = new List<string>();
        foreach ((string maske, Dictionary<string, string> eintraege) in BewusstDraussen)
        {
            if (!zuordnung.TryGetValue(maske, out string? dateien))
            {
                falsch.Add(maske + ": keine Maske mit Markup-Probe");
                continue;
            }

            HashSet<string> katalog = Eigenschaften(maske);
            if (MITMASKEN.TryGetValue(maske, out string[]? mit))
                foreach (string m in mit) katalog.UnionWith(Eigenschaften(m));

            var bindungen = new HashSet<string>(
                Dateien(dateien).SelectMany(d => Eingaben(OhneKommentare(Lesen(d)))).Select(e => e.Bindung),
                StringComparer.Ordinal);

            foreach ((string bindung, string grund) in eintraege)
            {
                if (string.IsNullOrWhiteSpace(grund)) falsch.Add(maske + " / " + bindung + ": ohne Grund");
                if (!bindungen.Contains(bindung)) falsch.Add(maske + " / " + bindung + ": steht in keiner Datei der Maske");
                else if (Gedeckt(bindung, katalog)) falsch.Add(maske + " / " + bindung + ": steht im Katalog");
            }
        }

        Assert.True(falsch.Count == 0, "Diese Einträge treffen nicht mehr zu:\n  " + string.Join("\n  ", falsch));
    }

    /// <summary>
    /// <b>Die Zahl der Eingabestellen ist festgeschrieben</b> — für jede angemeldete oder
    /// gehostete Datei, die in keiner Markup-Probe steht. Ihre Felder binden über eine
    /// Sichtklasse, deren Eigenschaftsnamen nicht im Markup stehen; eine neue Eingabe fiele
    /// sonst niemandem auf.
    /// </summary>
    [Fact]
    public void Die_Zahl_der_Eingabestellen_ist_festgeschrieben()
    {
        IReadOnlyDictionary<string, Komponente> bestand = Bestand();

        var mitProbe = new HashSet<string>(StringComparer.Ordinal);
        foreach (object[] zeile in KiDialogkatalogTests.Markupdateien())
            foreach (string d in Dateien((string)zeile[1]))
                mitProbe.Add(Path.GetFileNameWithoutExtension(d));

        var kinder = new HashSet<string>(WIRTE.Select(w => w.Kind), StringComparer.Ordinal);
        List<Komponente> soll = bestand.Values
            .Where(k => k.ImGeltungsbereich && (k.Angemeldet || kinder.Contains(k.Name)) && !mitProbe.Contains(k.Name))
            .OrderBy(k => k.Name, StringComparer.Ordinal)
            .ToList();

        var tabelle = EINGABESTELLEN.ToDictionary(e => e.Komponente, StringComparer.Ordinal);
        var falsch = new List<string>();
        var vorschlag = new StringBuilder();

        foreach (Komponente k in soll)
        {
            vorschlag.Append("        new(\"").Append(k.Name).Append("\", ").Append(k.Eingaben.Count)
                     .Append(tabelle.TryGetValue(k.Name, out Eingabestellen alt) && alt.Vermerk.Length > 0
                                 ? ", \"" + alt.Vermerk + "\"" : "")
                     .Append("),\n");

            if (!tabelle.TryGetValue(k.Name, out Eingabestellen e))
                falsch.Add(k.Name + ": fehlt in EINGABESTELLEN (" + k.Eingaben.Count + " Eingabestellen: " +
                           Zusammenfassung(k) + ")");
            else if (e.Zahl != k.Eingaben.Count)
                falsch.Add(k.Name + ": " + k.Eingaben.Count + " Eingabestellen statt " + e.Zahl + " (" +
                           Zusammenfassung(k) + ") — Katalog und Sichtklasse nachziehen oder den Grund in den " +
                           "Vermerk schreiben, dann die Zahl anpassen");
        }

        foreach (Eingabestellen e in EINGABESTELLEN)
            if (!soll.Any(k => k.Name == e.Komponente))
                falsch.Add(e.Komponente + ": steht in EINGABESTELLEN, ist aber weder angemeldet noch gehostet " +
                           "(oder trägt eine Markup-Probe)");

        Assert.True(falsch.Count == 0,
            "Die Eingabebilanz stimmt nicht:\n  " + string.Join("\n  ", falsch) +
            "\n\nStand des Bestands:\n" + vorschlag);
    }

    // =====================================================================
    //  Der Hilfeschlüssel einer Ausnahme — der Weg der benannten Absage
    // =====================================================================

    /// <summary>
    /// Der Hilfeschlüssel einer Ausnahme steht in ihrer Datei (am Info-Knopf oder als
    /// Vorgabe des Parameters <c>HilfeSchluessel</c>) und in KEINER angemeldeten Maske —
    /// sonst sagte die Absage „bewusst nicht steuerbar" über eine steuerbare.
    /// </summary>
    [Fact]
    public void Der_Hilfeschluessel_einer_Ausnahme_steht_in_ihrer_Datei()
    {
        IReadOnlyDictionary<string, Komponente> bestand = Bestand();
        var falsch = new List<string>();

        foreach (KiAusnahme a in KiDialogAusnahmen.Alle)
        {
            if (!bestand.TryGetValue(a.Komponente, out Komponente? k)) continue;

            List<string> eigene = Hilfeschluessel(k.Text);
            if (a.Hilfeschluessel.Length > 0 && !eigene.Contains(a.Hilfeschluessel))
                falsch.Add(a.Komponente + ": der Schlüssel " + a.Hilfeschluessel + " steht nicht in der Datei");

            // Vollstaendig: Wer einen eigenen Hauptschluessel traegt, nennt ihn.
            string? haupt = eigene.FirstOrDefault(s => s.EndsWith(".btn_Help", StringComparison.Ordinal));
            if (a.Hilfeschluessel.Length == 0 && haupt is not null)
                falsch.Add(a.Komponente + ": trägt den Hilfeschlüssel " + haupt + ", der Eintrag nennt ihn nicht");

            if (a.Hilfeschluessel.Length == 0) continue;

            foreach (Komponente angemeldet in bestand.Values.Where(x => x.Angemeldet))
                if (Hilfeschluessel(angemeldet.Text).Contains(a.Hilfeschluessel))
                    falsch.Add(a.Komponente + ": den Schlüssel " + a.Hilfeschluessel + " trägt auch die angemeldete " +
                               angemeldet.Name);
        }

        Assert.True(falsch.Count == 0, "Die Hilfeschlüssel der Ausnahmeliste stimmen nicht:\n  " +
                                       string.Join("\n  ", falsch));
    }

    // =====================================================================
    //  Der Bestand
    // =====================================================================

    /// <summary>Eine Razor-Komponente des Bestands.</summary>
    private sealed record Komponente(string Name, string Repopfad, string Text, bool Angemeldet,
                                     bool ImGeltungsbereich, IReadOnlyList<Eingabe> Eingaben);

    /// <summary>Eine Eingabestelle: der Baustein und der Ausdruck, an den er bindet.</summary>
    private readonly record struct Eingabe(string Baustein, string Bindung);

    private static IReadOnlyDictionary<string, Komponente>? _bestand;

    /// <summary>Alle Razor-Komponenten unter <c>EPOS.UI</c>, je Typname eine.</summary>
    private static IReadOnlyDictionary<string, Komponente> Bestand()
    {
        if (_bestand is not null) return _bestand;

        string wurzel = Wurzel();
        var liste = new Dictionary<string, Komponente>(StringComparer.Ordinal);

        foreach (string voll in Directory.EnumerateFiles(Path.Combine(wurzel, "EPOS.UI"), "*.razor",
                                                         SearchOption.AllDirectories))
        {
            string repopfad = Path.GetRelativePath(wurzel, voll).Replace('\\', '/');
            if (repopfad.Contains("/bin/", StringComparison.Ordinal) ||
                repopfad.Contains("/obj/", StringComparison.Ordinal)) continue;

            string name = Path.GetFileNameWithoutExtension(voll);
            string text = OhneKommentare(File.ReadAllText(voll));

            // Die Anmeldung darf auch im Code-Behind stehen (<Name>.razor.cs).
            string hinten = voll + ".cs";
            string anmeldetext = File.Exists(hinten) ? text + "\n" + OhneKommentare(File.ReadAllText(hinten)) : text;

            bool geltung = repopfad.StartsWith("EPOS.UI/Dialoge/", StringComparison.Ordinal) ||
                           repopfad.StartsWith("EPOS.UI/Seiten/", StringComparison.Ordinal);

            Assert.False(liste.ContainsKey(name), "Zwei Razor-Dateien heißen " + name);
            liste[name] = new Komponente(name, repopfad, text,
                                         anmeldetext.Contains("KiMaskenanmeldung.Fuer(", StringComparison.Ordinal),
                                         geltung, Eingaben(text));
        }

        return _bestand = liste;
    }

    /// <summary>
    /// Steht <paramref name="kind"/> im Markup von <paramref name="wirt"/> — unmittelbar
    /// oder über weitere Komponenten (höchstens vier Stufen)?
    /// </summary>
    private static bool Erreichbar(IReadOnlyDictionary<string, Komponente> bestand, string wirt, string kind)
    {
        var besucht = new HashSet<string>(StringComparer.Ordinal) { wirt };
        var stufe = new List<string> { wirt };

        for (int tiefe = 0; tiefe < 4 && stufe.Count > 0; tiefe++)
        {
            var naechste = new List<string>();
            foreach (string name in stufe)
            {
                string text = bestand[name].Text;
                foreach (string ziel in bestand.Keys)
                {
                    if (besucht.Contains(ziel)) continue;
                    if (!Regex.IsMatch(text, @"<(?:[\w.]+\.)?" + Regex.Escape(ziel) + @"[\s/>]")) continue;
                    if (ziel == kind) return true;
                    besucht.Add(ziel);
                    naechste.Add(ziel);
                }
            }
            stufe = naechste;
        }

        return false;
    }

    // =====================================================================
    //  Der Zähler
    // =====================================================================

    /// <summary>Die Eingabebausteine — Haus, Bausteine und nackte HTML-Felder.</summary>
    private static readonly string[] BAUSTEINE =
    {
        "Zahlenfeld", "Ganzzahlfeld", "Textfeld", "Auswahlfeld", "Suchauswahl", "Datumsfeld",
        "Farbfeld", "Schalter", "Optionsgruppe", "EnergietraegerWahl", "Katalogfelder",
        "input", "select", "textarea"
    };

    /// <summary>Die Attribute, an denen ein Baustein seinen Wert bindet.</summary>
    private static readonly string[] WERTATTRIBUTE =
    {
        "Wert", "@bind-Wert", "Auswahl", "@bind-Auswahl", "Felder", "@bind", "@bind-value", "value", "checked"
    };

    /// <summary>
    /// Der Anfang eines Bausteins: <c>&lt;Name</c>, gefolgt von Leerraum, <c>/</c> oder
    /// <c>&gt;</c>, und nicht Teil eines Typnamens (<c>List&lt;Schalter&gt;</c>,
    /// <c>&lt;EnergietraegerWahl.Eintrag&gt;</c>).
    /// </summary>
    private static readonly Regex TAGANFANG = new(
        @"(?<![\w.])<(?<tag>" + string.Join("|", BAUSTEINE) + @")(?=[\s/>])", RegexOptions.Compiled);

    /// <summary>
    /// Ein Baustein, den der Code über den <c>RenderTreeBuilder</c> zeichnet
    /// (<c>builder.OpenComponent&lt;Zahlenfeld&gt;(0)</c>,
    /// <c>builder.OpenElement(0, "input")</c>) — der Modulkatalog baut so seinen Feldsatz.
    /// </summary>
    private static readonly Regex BAUMANFANG = new(
        @"OpenComponent<(?:[\w.]+\.)?(?<tag>" + string.Join("|", BAUSTEINE.Where(b => char.IsUpper(b[0]))) +
        @")>\s*\(|OpenElement\(\s*\d+\s*,\s*""(?<tag>input|select|textarea)""", RegexOptions.Compiled);

    /// <summary>Die Eingabestellen eines Markups (ohne Kommentare).</summary>
    private static List<Eingabe> Eingaben(string text)
    {
        var liste = new List<Eingabe>();

        foreach (Match m in BAUMANFANG.Matches(text))
            liste.Add(new Eingabe(m.Groups["tag"].Value, ""));

        foreach (Match m in TAGANFANG.Matches(text))
        {
            string tag = m.Groups["tag"].Value;
            Dictionary<string, string> attr = Attribute(text, m.Index + m.Length);

            // Ein Typname mit Generika-Klammer ist kein Tag: <Schalter> ohne Attribute gibt
            // es als Baustein nicht, als Typargument sehr wohl.
            if (attr.Count == 0 && char.IsUpper(tag[0])) continue;

            // Gesperrt auf Dauer ist keine Eingabe - ein Textfeld „nur lesen" so wenig wie
            // ein Schalter, der sichtbar, aber nie bedienbar ist (Aktiv="false").
            if ((tag == "Textfeld" || tag == "Katalogfelder") &&
                attr.TryGetValue("NurLesen", out string? nurLesen) &&
                (nurLesen == "true" || nurLesen == "@true")) continue;
            if (attr.TryGetValue("Aktiv", out string? aktiv) && (aktiv == "false" || aktiv == "@false")) continue;

            string bindung = "";
            foreach (string a in WERTATTRIBUTE)
                if (attr.TryGetValue(a, out string? w))
                {
                    bindung = Normalisieren(w);
                    break;
                }

            liste.Add(new Eingabe(tag, bindung));
        }

        return liste;
    }

    /// <summary>
    /// Liest die Attribute eines Tags ab <paramref name="start"/> bis zu seinem Ende — mit
    /// verschachtelten Anführungszeichen in <c>@( … )</c>.
    /// </summary>
    private static Dictionary<string, string> Attribute(string t, int start)
    {
        var attr = new Dictionary<string, string>(StringComparer.Ordinal);
        int i = start;

        while (i < t.Length)
        {
            while (i < t.Length && char.IsWhiteSpace(t[i])) i++;
            if (i >= t.Length || t[i] == '>') break;
            if (t[i] == '/' && i + 1 < t.Length && t[i + 1] == '>') break;

            int nameAnfang = i;
            while (i < t.Length && !char.IsWhiteSpace(t[i]) && t[i] != '=' && t[i] != '>' &&
                   !(t[i] == '/' && i + 1 < t.Length && t[i + 1] == '>')) i++;
            string name = t.Substring(nameAnfang, i - nameAnfang);
            if (name.Length == 0) { i++; continue; }

            while (i < t.Length && char.IsWhiteSpace(t[i])) i++;
            if (i >= t.Length || t[i] != '=')
            {
                attr[name] = "";
                continue;
            }

            i++;
            while (i < t.Length && char.IsWhiteSpace(t[i])) i++;

            string wert;
            if (i < t.Length && t[i] == '"')
            {
                i++;
                int anfang = i, tiefe = 0;
                while (i < t.Length)
                {
                    char c = t[i];
                    if (c == '(') tiefe++;
                    else if (c == ')' && tiefe > 0) tiefe--;
                    else if (c == '"' && tiefe == 0) break;
                    else if (c == '"')
                    {
                        // Eine C#-Zeichenkette im Ausdruck.
                        i++;
                        while (i < t.Length && t[i] != '"') i += t[i] == '\\' ? 2 : 1;
                    }
                    i++;
                }
                wert = t.Substring(anfang, Math.Min(i, t.Length) - anfang);
                i++;
            }
            else
            {
                int anfang = i;
                while (i < t.Length && !char.IsWhiteSpace(t[i]) && t[i] != '>') i++;
                wert = t.Substring(anfang, i - anfang);
            }

            attr[name] = wert;
        }

        return attr;
    }

    /// <summary>Ohne führendes <c>@</c>, Leerraum zu einem Zeichen zusammengezogen.</summary>
    private static string Normalisieren(string ausdruck)
    {
        string s = Regex.Replace(ausdruck.Trim(), @"\s+", " ");
        return s.StartsWith('@') ? s.Substring(1) : s;
    }

    /// <summary>
    /// Deckt der Katalog die Bindung? Ja, wenn ein Bezeichner des Ausdrucks — nach einem
    /// Punkt oder als ganzer Ausdruck — der Eigenschaftsname eines Katalogfeldes ist, oder
    /// dieser Name mit dem Zusatz <c>Index</c> (die Wahl über einen Listenplatz).
    /// </summary>
    private static bool Gedeckt(string bindung, HashSet<string> eigenschaften)
    {
        if (bindung.Length == 0) return false;
        string ohneText = Regex.Replace(bindung, "\"[^\"]*\"", "\"\"");

        var kandidaten = new List<string>();
        foreach (Match m in Regex.Matches(ohneText, @"\.(?<n>[A-Za-z_]\w*)")) kandidaten.Add(m.Groups["n"].Value);
        if (Regex.IsMatch(ohneText, @"^[A-Za-z_]\w*$")) kandidaten.Add(ohneText);

        foreach (string k in kandidaten)
        {
            if (eigenschaften.Contains(k)) return true;
            if (k.EndsWith("Index", StringComparison.OrdinalIgnoreCase) &&
                eigenschaften.Contains(k.Substring(0, k.Length - "Index".Length))) return true;
        }

        return false;
    }

    /// <summary>Die Eigenschaftsnamen der Katalogfelder einer Maske (bei Spalten der Teil nach <c>[]</c>).</summary>
    private static HashSet<string> Eigenschaften(string maske)
    {
        KiDialog? d = KiDialoge.Katalog.Finde(maske);
        Assert.NotNull(d);
        return new HashSet<string>(d!.Felder.Select(f => KiEigenschaftspfad.Eigenschaft(f.Eigenschaftspfad)),
                                   StringComparer.Ordinal);
    }

    /// <summary>Die Hilfeschlüssel einer Datei: Literale am Info-Knopf und Vorgaben der <c>HilfeSchluessel*</c>-Parameter.</summary>
    private static List<string> Hilfeschluessel(string text)
    {
        var liste = new List<string>();
        foreach (Match m in Regex.Matches(text, @"<InfoKnopf\b[^>]*?\bSchluessel=""(?<s>[^""@]+)""", RegexOptions.Singleline))
            liste.Add(m.Groups["s"].Value.Trim());
        foreach (Match m in Regex.Matches(text, @"HilfeSchluessel\w*\s*\{\s*get;\s*set;\s*\}\s*=\s*""(?<s>[^""]+)"""))
            liste.Add(m.Groups["s"].Value.Trim());
        return liste;
    }

    private static string Zusammenfassung(Komponente k)
        => string.Join(", ", k.Eingaben.GroupBy(e => e.Baustein).Select(g => g.Key + "=" + g.Count()));

    // =====================================================================
    //  Dateien
    // =====================================================================

    private static string[] Dateien(string eintrag)
        => eintrag.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Lesen(string repopfad)
    {
        string voll = Path.Combine(Wurzel(), repopfad.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(voll), repopfad);
        return File.ReadAllText(voll);
    }

    /// <summary>
    /// Ohne <c>@* … *@</c>, <c>&lt;!-- … --&gt;</c> und Zeilen, die mit <c>//</c> beginnen —
    /// dieselbe Regel wie die Markup-Probe.
    /// </summary>
    private static string OhneKommentare(string text)
    {
        string t = Regex.Replace(text, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        t = Regex.Replace(t, @"<!--.*?-->", " ", RegexOptions.Singleline);
        return string.Join("\n", t.Split('\n')
                                  .Where(z => !z.TrimStart().StartsWith("//", StringComparison.Ordinal)));
    }

    /// <summary>Der Weg zur Repowurzel — dasselbe Verfahren wie <c>KiDialogkatalogTests.Wurzel</c>.</summary>
    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }
}
