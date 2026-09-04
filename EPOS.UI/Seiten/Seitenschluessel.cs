namespace EPOS.UI.Seiten;

/// <summary>
/// Die sprachneutralen Schluessel der Ansichten und Masken - dieselbe
/// Drei-Schichten-Regel wie <c>WindowsFormsApplication1.Masken</c>: ASCII,
/// sprachneutral, nie ein Anzeigetext.
///
/// <para><b>Seit iU9-W16c.0 (K7, Entscheide E-1 und E-2) ist das die EINE
/// Tabelle beider Plattformen.</b> Bis dahin standen zwei nebeneinander: hier
/// die sieben Schluessel der iOS-Wurzel, im Kern die 25 Maskenschluessel von
/// <c>Masken</c>. Der Menueband-Baustein des Hauptfensters (W16c.1) verteilt
/// seine 55 Punkte ueber DIESE Schluessel, und <see cref="AppWurzel"/> ist die
/// gemeinsame Wurzel von Windows und iOS (E-1) - also braucht es genau einen
/// Satz.</para>
///
/// <para><b>Eine Wahrheit, kein zweiter Wert.</b> Die uebernommenen Schluessel
/// sind KEINE Abschriften, sondern Verweise auf <c>Masken</c> bzw.
/// <c>Ansichten</c> im Kern (<c>= WindowsFormsApplication1.Masken.X</c>). Damit
/// bleibt <c>INavigation.OeffneMaske</c> unveraendert gueltig (E-2), die
/// Windows-Fassung <c>WinFormsNavigation</c> schaltet weiter ueber dieselben
/// Zeichenketten, und eine Aenderung im Kern kann hier nicht auseinanderlaufen.
/// Die Richtung ist die einzig moegliche: EPOS.UI kennt EPOS.Kern, nicht
/// umgekehrt.</para>
///
/// <para>Ein unbekannter Schluessel tut nichts und liefert <c>false</c> -
/// derselbe Ausgang, den <c>KeineNavigation</c> im Kern liefert.</para>
/// </summary>
public static class Seitenschluessel
{
    /// <summary>Die Projektliste - der Einstieg.</summary>
    public const string Projektliste = "PROJEKTLISTE";

    /// <summary>Der Dialog „Energieträger anlegen" (<c>EnergietraegerVarianteDialog</c>).</summary>
    public const string Energietraeger = "ENERGIETRAEGER_VARIANTE";

    /// <summary>Der Dialog „BHKW-Wirtschaftlichkeit" (<c>BhkwWirtschaftlichkeitDialog</c>).</summary>
    public const string BhkwWirtschaftlichkeit = "BHKW_WIRTSCHAFTLICHKEIT";

    /// <summary>
    /// Die Simulationskonfiguration (<c>Simulation.SimulationKonfigSeite</c>,
    /// iU9-W10b.1) — die erste FACHSEITE, die iOS ueber <see cref="AppWurzel"/>
    /// erreicht. Unter Windows steht dieselbe Komponente bis W16 in einem modalen
    /// Fenster (Entscheid R-W10b-1); der Schluessel gilt fuer beide Wege.
    /// </summary>
    public const string SimulationKonfiguration = "SIMULATION_KONFIGURATION";

    /// <summary>
    /// Das Simulationsergebnis (<c>Simulation.SimulationErgebnisSeite</c>,
    /// iU9-W11b.13) — die zweite FACHSEITE, die iOS ueber <see cref="AppWurzel"/>
    /// erreicht. Unter Windows steht dieselbe Komponente bis W16 in einem modalen
    /// Fenster (Entscheid R-W11-1); der Schluessel gilt fuer beide Wege.
    /// </summary>
    public const string SimulationErgebnis = "SIMULATION_ERGEBNIS";

    /// <summary>
    /// Der KI-Hilfe-Assistent (<c>Dialoge.Hilfe.KiChatDialog</c>, iU9-W15b.7,
    /// Entscheid E-10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Klassenkopf kuendigte ihn seit iU10-2 an ("der Assistent (iL5) kommt
    /// mit iU10-9 und bringt seine eigenen Schluessel mit"). Er ist der erste
    /// Schluessel, den KEINE Kachel der Projektliste oeffnet: Unter Windows steht
    /// der Chat am Menue "Hilfe -&gt; Hilfe-Assistent (KI)…" und an F1, auf iOS am
    /// Baustein <c>KiKnopf</c> jeder Maske.
    /// </para>
    /// <para>
    /// <b>Er hat bewusst KEINEN <c>Masken.*</c>-Zwilling</b> (Befund W15b-B4): Der
    /// Chat wurde nie ueber die Sprungtabelle geoeffnet, und die Tabelle faellt mit
    /// Welle 16 ohnehin.
    /// </para>
    /// <para>
    /// Der Kern kennt denselben Wert als Zeichenkette in
    /// <c>KiChatKontext.BEREICH_JE_SEITE</c> - dort bildet er auf den Bereich
    /// "Hilfe" ab, damit der Assistent auf iOS weiss, wovon der Anwender spricht
    /// (Entscheid E-9).
    /// </para>
    /// </remarks>
    public const string KiAssistent = "KI_ASSISTENT";

    /// <summary>
    /// Der PROJEKTASSISTENT (<c>Seiten.Assistent.AssistentSeite</c>, iU9-W16a.5) —
    /// die dritte Fachseite, die iOS ueber <see cref="AppWurzel"/> erreicht.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Er hat einen <c>Masken.*</c>-Zwilling: <c>Masken.Assistent</c> ist unter
    /// Windows der Weg vom Menue und von den beiden Startkacheln in die modale
    /// Huelle (<c>AssistentHuelle.Oeffnen</c>) — beide Aufrufer werten aus, ob
    /// gespeichert wurde. Der Schluessel hier gilt fuer den iOS-Weg: Dort wird die
    /// Ansicht ausgetauscht, es gibt kein zweites Fenster.
    /// </para>
    /// <para>
    /// Die drei uebrigen Schluessel der Zusammenlegung (<c>STARTSEITE</c>,
    /// <c>BERICHTE_KOSTEN</c> und die verbleibenden <c>Masken</c>-Werte, K7 der
    /// Vermessung) kommen mit W16c.
    /// </para>
    /// </remarks>
    public const string Assistent = "ASSISTENT";
    // =====================================================================
    //  K7 (iU9-W16c.0) - die drei NEUEN Schluessel des Rahmens
    // =====================================================================

    /// <summary>
    /// Die STARTSEITE (<c>Seiten.Start.Startseite</c>, iU9-W16b.2) - sechs
    /// Reiter, 21 Kacheln, Kopfband mit Projektwahl und Klimaregion.
    /// </summary>
    /// <remarks>
    /// Unter Windows steht sie seit W16b unmittelbar in <c>MDIMainForm</c>; mit
    /// W16c ist sie die Vorgabeansicht von <see cref="AppWurzel"/> und damit auf
    /// beiden Plattformen dieselbe Seite (E-1). Auf iOS speist
    /// <c>IProjektQuelle.Startkacheln(id)</c> ihren Kachelbestand (K6).
    /// </remarks>
    public const string Startseite = "STARTSEITE";

    /// <summary>
    /// Der Reiter „Berichte &amp; Kosten" (<c>Seiten.Berichte.BerichteKostenSeite</c>,
    /// iU9-W5) - die Seite, die es als Razor seit Welle 5 gibt und die in
    /// <see cref="AppWurzel"/> bis W16c bloss nicht verdrahtet war
    /// (Vermessung § 9.2).
    /// </summary>
    /// <remarks>
    /// Der Wert ist der BEREICHSSCHLUESSEL des Kerns
    /// (<c>Ansichten.BerichteKosten</c>): Derselbe Text steht seit iU5 in
    /// <c>INavigation.AnsichtAktualisieren</c>, und zwei Zeichenketten fuer
    /// dieselbe Seite waeren zwei Wahrheiten.
    /// </remarks>
    public const string BerichteKosten = WindowsFormsApplication1.Ansichten.BerichteKosten;

    /// <summary>
    /// Die Variantenanzeige des Kopfbands - kein eigener Seitenwechsel, sondern
    /// der zweite Bereichsschluessel von <c>INavigation.AnsichtAktualisieren</c>.
    /// Er steht hier, damit die Tabelle vollstaendig ist.
    /// </summary>
    public const string Varianten = WindowsFormsApplication1.Ansichten.Varianten;

    // =====================================================================
    //  K7 (iU9-W16c.0, Entscheid E-2) - die 25 Maskenschluessel des Kerns
    //
    //  Verweise, keine Abschriften: Der Wert steht in
    //  EPOS.Kern/Allgemein/Dienste/Masken.cs und wird hier nur SICHTBAR
    //  gemacht. Die Windows-Fassung WinFormsNavigation schaltet unveraendert
    //  ueber Masken.*, das Menueband (W16c.1) und AppWurzel ueber die
    //  Schluessel hier - es ist derselbe Text.
    // =====================================================================

    /// <summary>Stammdaten Waermepumpen (Razor seit W7.3).</summary>
    public const string WpAdministration = WindowsFormsApplication1.Masken.WpAdministration;

    /// <summary>Stammdaten Stromspeicher (Razor seit W14a.3).</summary>
    public const string StromspeicherAdmin = WindowsFormsApplication1.Masken.StromspeicherAdmin;

    /// <summary>Lastspitzenkappung; Argument: Projekt-Id (Razor seit W12.6).</summary>
    public const string PeakShaving = WindowsFormsApplication1.Masken.PeakShaving;

    /// <summary>Stammdaten Gebaeude (Razor seit W9.2).</summary>
    public const string GebaeudeAdmin = WindowsFormsApplication1.Masken.GebaeudeAdmin;

    /// <summary>Stammdaten Gebaeudetypen (Razor seit W8.4).</summary>
    public const string GebaeudetypenAdmin = WindowsFormsApplication1.Masken.GebaeudetypenAdmin;

    /// <summary>Stammdaten eingelesener Waermebedarf (Razor seit W13.2).</summary>
    public const string WaermebedarfExternAdmin = WindowsFormsApplication1.Masken.WaermebedarfExternAdmin;

    /// <summary>Stammdaten Prozesswaerme (Razor seit W14b.1).</summary>
    public const string ProzesswaermeAdmin = WindowsFormsApplication1.Masken.ProzesswaermeAdmin;

    /// <summary>Stammdaten Stromverbraucher (Razor seit W14b.1).</summary>
    public const string StromverbraucherAdmin = WindowsFormsApplication1.Masken.StromverbraucherAdmin;

    /// <summary>Stammdaten Stromganglinien (Razor seit W12.4).</summary>
    public const string StromganglinieAdmin = WindowsFormsApplication1.Masken.StromganglinieAdmin;

    /// <summary>Stammdaten Solarganglinien (Razor seit W14b.2).</summary>
    public const string SolarganglinieAdmin = WindowsFormsApplication1.Masken.SolarganglinieAdmin;

    /// <summary>Herstellerdaten Waermepumpen einlesen (Razor seit W13.1).</summary>
    public const string WpImport = WindowsFormsApplication1.Masken.WpImport;

    /// <summary>Stammdaten Heizkessel (Razor seit W14a.1).</summary>
    public const string HeizkesselAdmin = WindowsFormsApplication1.Masken.HeizkesselAdmin;

    /// <summary>Stammdaten BHKW (Razor seit W14a.1).</summary>
    public const string BhkwAdmin = WindowsFormsApplication1.Masken.BhkwAdmin;

    /// <summary>Stammdaten Solarkollektoren (Razor seit W14a.1).</summary>
    public const string SolarkollektorenAdmin = WindowsFormsApplication1.Masken.SolarkollektorenAdmin;

    /// <summary>Stammdaten Photovoltaik (Razor seit W14a.1).</summary>
    public const string PvAdmin = WindowsFormsApplication1.Masken.PvAdmin;

    /// <summary>Herstellerdaten Heizkessel einlesen (Razor seit W13.1).</summary>
    public const string HeizkesselImport = WindowsFormsApplication1.Masken.HeizkesselImport;

    /// <summary>Herstellerdaten Pufferspeicher einlesen (Razor seit W13.1).</summary>
    public const string PufferSpImport = WindowsFormsApplication1.Masken.PufferSpImport;

    /// <summary>Stammdaten Pufferspeicher (Razor seit W14a.1).</summary>
    public const string PufferSpAdmin = WindowsFormsApplication1.Masken.PufferSpAdmin;

    /// <summary>Stammdaten Brauchwasser (Razor seit W14b.1).</summary>
    public const string BrauchwasserAdmin = WindowsFormsApplication1.Masken.BrauchwasserAdmin;

    /// <summary>Herstellerdaten Solarkollektoren einlesen (Razor seit W13.1).</summary>
    public const string SolarkollektorenImport = WindowsFormsApplication1.Masken.SolarkollektorenImport;

    /// <summary>
    /// Herstellerdaten PV-Module einlesen; Argument <c>"CEC"</c> oder
    /// <c>"PAN"</c> (Razor seit W13.3).
    /// </summary>
    public const string PvImport = WindowsFormsApplication1.Masken.PvImport;

    /// <summary>„Speichern unter…" - dupliziert ein Projekt (Razor seit W15a.4).</summary>
    public const string ProjektSpeichernUnter = WindowsFormsApplication1.Masken.ProjektSpeichernUnter;

    /// <summary>Projektauswahl zum Oeffnen (Razor seit W15a.3).</summary>
    public const string ProjektAuswahl = WindowsFormsApplication1.Masken.ProjektAuswahl;

    /// <summary>Projektauswahl zum Loeschen (Razor seit W15a.3).</summary>
    public const string ProjektDelete = WindowsFormsApplication1.Masken.ProjektDelete;

    // Masken.Assistent traegt denselben Wert wie der Schluessel Assistent
    // weiter oben ("ASSISTENT"); er steht deshalb nur EINMAL in dieser Klasse.

    // =====================================================================
    //  Die WEGE des Hauptfensters (iU9-W16c.1)
    //
    //  Neunzehn Menuepunkte fuehren nicht auf eine Maske aus "Masken", sondern
    //  auf einen zusammengesetzten Ablauf oder auf eine Windows-Eigenheit
    //  (Sprachwechsel, Browser, Versionsmeldung). Im Bestand war jeder von
    //  ihnen ein eigener Ereignishandler in MDIMainForm; hier ist er ein
    //  Schluessel wie jeder andere, und Hauptfenster.Springe verteilt ihn -
    //  entweder selbst oder ueber den Weg-Delegaten der Huelle.
    //
    //  Sie stehen bewusst in DIESER Klasse und nicht in einer zweiten: Das
    //  Menueband kennt genau eine Schluesselart, und N4 prueft sie an einem
    //  Ort.
    // =====================================================================

    /// <summary>Menue „Projekt -> Neu…" - der Assistent in Betriebsart NEU.</summary>
    public const string ProjektNeu = "PROJEKT_NEU";

    /// <summary>Menue „Projekt -> Öffnen…" - Projektauswahl, dann aktiv setzen.</summary>
    public const string ProjektOeffnen = "PROJEKT_OEFFNEN";

    /// <summary>Menue „Projekt -> Bearbeiten…" - der Assistent in Betriebsart BEARBEITEN.</summary>
    public const string ProjektBearbeiten = "PROJEKT_BEARBEITEN";

    /// <summary>Menue „Projekt -> zuletzt geöffnet" - ohne Dialog aktiv setzen.</summary>
    public const string ProjektZuletzt = "PROJEKT_ZULETZT";

    /// <summary>Menue „Projekt -> Löschen…" - Auswahl, Rueckfrage, die drei Loeschschritte.</summary>
    public const string ProjektLoeschen = "PROJEKT_LOESCHEN";

    /// <summary>Menue „Projekt -> Export/Import" (<c>ProjektTransferDialog</c>, W15a.5).</summary>
    public const string ProjektTransfer = "PROJEKT_TRANSFER";

    /// <summary>Menue „Projekt -> Als Variante speichern…" (<c>NamensDialog</c>, W2.1).</summary>
    public const string ProjektAlsVariante = "PROJEKT_ALS_VARIANTE";

    /// <summary>Menue „Administration -> Klimadaten" (<c>KlimadatenDialog</c>, W14c.7).</summary>
    public const string Klimadaten = "KLIMADATEN";

    /// <summary>Menue „Administration -> Kosten -> Kostenverwaltung…" (<c>KostenKomponenteDialog</c>, W4.2).</summary>
    public const string Kostenverwaltung = "KOSTENVERWALTUNG";

    /// <summary>Menue „Administration -> Kosten -> Energieträgerverwaltung…" (<c>EnergietraegerDialog</c>, W4.4).</summary>
    public const string EnergietraegerVerwaltung = "ENERGIETRAEGER_VERWALTUNG";

    /// <summary>Menue „Administration -> Einstellungen" (<c>EinstellungenDialog</c>, W14c.6).</summary>
    public const string Einstellungen = "EINSTELLUNGEN";

    /// <summary>Menue „Administration -> Gesetzliche Parameter" (<c>GesetzeskatalogDialog</c>, W14c.2).</summary>
    public const string Gesetzeskatalog = "GESETZESKATALOG";

    /// <summary>Menue „Administration -> Katalog-Dubletten" (<c>KatalogDublettenDialog</c>, W14c.5).</summary>
    public const string KatalogDubletten = "KATALOG_DUBLETTEN";

    /// <summary>Menue „Administration -> Lizenz…" (<c>LizenzVerwaltungDialog</c>, W15c.5).</summary>
    public const string LizenzVerwaltung = "LIZENZ_VERWALTUNG";

    /// <summary>Menue „Hilfe -> Lizenz" - der VERTRAGSTEXT (<c>LizenzDialog</c>, W15c.11).</summary>
    public const string Lizenztext = "LIZENZTEXT";

    /// <summary>Menue „Hilfe -> Version" - die Meldung „Über EPOS-Plan".</summary>
    public const string Version = "VERSION";

    /// <summary>Menue „Hilfe -> Dokumentation" - das Wiki im Browser.</summary>
    public const string Dokumentation = "DOKUMENTATION";

    /// <summary>Menue „Deutsch" - Oberflaechensprache umstellen und neu starten.</summary>
    public const string SpracheDeutsch = "SPRACHE_DEUTSCH";

    /// <summary>Menue „Englisch" - Oberflaechensprache umstellen und neu starten.</summary>
    public const string SpracheEnglisch = "SPRACHE_ENGLISCH";

    /// <summary>
    /// Alle Schluessel dieser Klasse - fuer den Nachweis, dass sie ASCII,
    /// sprachneutral und untereinander verschieden sind (N4, W16c.1).
    /// </summary>
    public static readonly IReadOnlyList<string> Alle = new[]
    {
        Projektliste, Energietraeger, BhkwWirtschaftlichkeit,
        SimulationKonfiguration, SimulationErgebnis, KiAssistent, Assistent,
        Startseite, BerichteKosten, Varianten,
        WpAdministration, StromspeicherAdmin, PeakShaving, GebaeudeAdmin,
        GebaeudetypenAdmin, WaermebedarfExternAdmin, ProzesswaermeAdmin,
        StromverbraucherAdmin, StromganglinieAdmin, SolarganglinieAdmin,
        WpImport, HeizkesselAdmin, BhkwAdmin, SolarkollektorenAdmin, PvAdmin,
        HeizkesselImport, PufferSpImport, PufferSpAdmin, BrauchwasserAdmin,
        SolarkollektorenImport, PvImport, ProjektSpeichernUnter,
        ProjektAuswahl, ProjektDelete,
        ProjektNeu, ProjektOeffnen, ProjektBearbeiten, ProjektZuletzt,
        ProjektLoeschen, ProjektTransfer, ProjektAlsVariante, Klimadaten,
        Kostenverwaltung, EnergietraegerVerwaltung, Einstellungen,
        Gesetzeskatalog, KatalogDubletten, LizenzVerwaltung, Lizenztext,
        Version, Dokumentation, SpracheDeutsch, SpracheEnglisch
    };
}
