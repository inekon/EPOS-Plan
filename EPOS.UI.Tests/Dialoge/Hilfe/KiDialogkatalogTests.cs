using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using KiKern;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Der WÄCHTER über den Dialogkatalog (Auftrag #200, Stufe S2).
///
/// <para><b>Warum er hier steht und nicht in <c>EPOS.Kern.Tests</c>.</b> Der Katalog
/// liegt im Kern, die Daten-Objekte liegen in <c>EPOS.UI</c> — und geprüft wird gerade
/// ihr Zusammenpassen. Ein Wächter im Kern könnte die Typen gar nicht sehen.</para>
///
/// <para><b>Was er verhindert.</b> Der zweite Parameter jeder <c>KiDialogFeld</c> ist
/// seit diesem Auftrag der Name einer EIGENSCHAFT
/// (<c>HeizkesselKatalogDaten.Ptherm</c>), aufgelöst per Reflection. Ein Tippfehler
/// darin bricht nichts — das Feld wird still nicht angemeldet und fehlt im Feldblock.
/// Genau deshalb muss ihn ein Zeuge nennen.</para>
///
/// <para><b>ZWEI Wächter, zwei Fragen (15.09.2026).</b>
/// <see cref="Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt"/> fragt, ob
/// sich der Pfad AUFLÖSEN lässt;
/// <see cref="Jeder_Feldpfad_steht_im_Markup_seiner_Maske"/> fragt, ob der Anwender das
/// Feld auch SIEHT. Die zweite Frage kam dazu, weil der Heizkesseleditor seine Kosten-
/// und Emissionsgruppen verlor und der Katalog die neun Felder trotzdem weiterführte:
/// Die Eigenschaften gibt es am DTO noch — die Hülle liest und schreibt die Spalten —,
/// nur zeigt die Maske sie nicht mehr. Der erste Wächter blieb dabei grün.</para>
///
/// <para><b>Die Klasse pinnt ihre Kultur auf <c>de-DE</c>.</b> Sie hält deutsche
/// Maskenbeschriftungen gegen die Anzeigenamen des Katalogs — der Windows-Läufer steht
/// auf <c>en-US</c>, und <c>Resource.*</c> löst über <c>CurrentUICulture</c> auf
/// (Hausregel „Kulturpinnung", <c>EPOS.Kern/CLAUDE.md</c>). Sie zeichnet nichts, führt
/// also die <see cref="Kulturvorrichtung"/> unmittelbar statt über
/// <c>EposBunitContext</c>.</para>
/// </summary>
public class KiDialogkatalogTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

    /// <summary>Stellt die vier Kulturwerte zurück.</summary>
    public void Dispose() => _kultur.Dispose();

    /// <summary>Die Masken und ihre Daten-Objekte — die EINE Zuordnungstabelle.</summary>
    /// <remarks>
    /// <b>Ein Daten-Objekt darf MEHRERE Masken tragen.</b> Die Erzeugermasken des
    /// Projekts (Welle KI‑F1) melden alle dieselbe Sichtklasse an
    /// (<c>ErzeugerProjektKiSicht</c> um die gewählte Zeile ihrer Projektliste); welche
    /// Felder daran hängen, sagt der Katalogeintrag und nicht der Typ.
    /// </remarks>
    public static TheoryData<string, Type> Masken() => new()
    {
        { KiMaskennamen.HEIZKESSEL,              typeof(HeizkesselKatalogDaten) },

        // Welle KI-F7: Form_PV meldet seit dem Anwenderentscheid 21.09.2026 eine
        // SICHTKLASSE an. Sie reicht die gewaehlte ErzeugerZeile durch und traegt
        // zusaetzlich die zwei Auslegungstemperaturen des PROJEKTS - siehe
        // OhneMarkupprobe.
        { KiMaskennamen.PHOTOVOLTAIK,            typeof(PhotovoltaikKiSicht) },

        // Die Ueberlagerung „Anlagenwerte" ist eine EIGENE Maske mit eigenem
        // Arbeitsstand; angemeldet ist genau dieser Stand, und zwar nur, solange das
        // Fenster offen steht.
        { KiMaskennamen.PV_ANLAGENWERTE,         typeof(PvAnlagenwerteKiSicht) },

        { KiMaskennamen.PUFFERSPEICHER,          typeof(PufferSpKatalogDaten) },
        { KiMaskennamen.WAERMEPUMPE,             typeof(WaermepumpeStammDaten) },
        { KiMaskennamen.STROMSPEICHER_AUSLEGUNG, typeof(StromspeicherKiSicht) },

        // Auftrag #221 (KI-D-E-1): die sechste Maske — die Ansicht „Simulation".
        // Voll ausgeschrieben: EPOS.UI.Seiten.Simulation fuehrt eine ZWEITE
        // ErzeugerZeile, und ein using darauf machte die Zeile darueber mehrdeutig.
        { KiMaskennamen.SIMULATION,
          typeof(EPOS.UI.Seiten.Simulation.SimulationKiSicht) },

        // 14.09.2026: die siebte Maske — und die erste mit einem RASTER. Ihre Felder
        // sind zum Teil SPALTEN (KostenKomponenteKiSicht.Zeilen[].Nutzungsdauer); der
        // Waechter unten loest sie ueber KiMaskenanmeldung.Pruefe mit auf.
        // Welle KI-F7: Angemeldet ist seither eine SICHTKLASSE - sie reicht den Stand
        // durch und traegt die drei Groessen, die NICHT an ihm haengen (siehe
        // OhneMarkupprobe).
        { KiMaskennamen.KOSTENVERWALTUNG,
          typeof(EPOS.UI.Dialoge.Kosten.KostenKomponenteKiSicht) },

        // Welle KI-F1: die Erzeugermasken des PROJEKTS. Sie melden die GEWAEHLTE
        // Zeile ihrer Projektliste an - seit Welle #458 (Stufe 2) ueber die
        // Sichtklasse ErzeugerProjektKiSicht, die die Zeile durchreicht und den
        // Aufklapper „Alle Daten" als FELDTAFEL traegt. Die benannten Felder behalten
        // ihre Namen und damit ihre Markup-Probe; die Tafelfelder haelt
        // Die_Projektmasken_fuehren_Alle_Daten_genau_nach_ihrem_Profil.
        { KiMaskennamen.HEIZKESSEL_PROJEKT,     typeof(ErzeugerProjektKiSicht) },
        { KiMaskennamen.BHKW_PROJEKT,           typeof(ErzeugerProjektKiSicht) },
        { KiMaskennamen.PUFFERSPEICHER_PROJEKT, typeof(ErzeugerProjektKiSicht) },
        { KiMaskennamen.STROMSPEICHER_PROJEKT,  typeof(ErzeugerProjektKiSicht) },

        // Die Solarkollektoren melden den ARBEITSSTAND der Kollektorgruppe an und
        // nicht die Zeile: Ihre fuenf Zahlen gehen erst mit „Uebernehmen" dorthin.
        // Dieselbe Bauart mit Feldtafel.
        { KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT,
          typeof(EPOS.UI.Dialoge.Solarthermie.SolarkollektorenKiSicht) },

        // Die Waermepumpen-ANLAGE - ein Feldsatz fuer alle drei Bloecke der Maske.
        // Welle #458: Angemeldet ist seither eine SICHTKLASSE - sie reicht den Feldsatz
        // durch und traegt die Projekteinstellung „Extrapolation" (siehe
        // OhneMarkupprobe).
        { KiMaskennamen.WAERMEPUMPE_ANLAGE, typeof(WaermepumpeAnlageKiSicht) },

        // Welle KI-F2: die Masken der SIMULATIONSKONFIGURATION. Sie melden je eine
        // SICHTKLASSE an - siehe OhneMarkupprobe.
        { KiMaskennamen.PUFFERSPEICHER_VERWALTUNG,
          typeof(EPOS.UI.Dialoge.Simulation.PufferSpProjektKiSicht) },
        { KiMaskennamen.QUELLE_ERDREICH,
          typeof(EPOS.UI.Dialoge.Simulation.QuelleErdreichKiSicht) },
        { KiMaskennamen.QUELLE_PUFFERSPEICHER,
          typeof(EPOS.UI.Dialoge.Simulation.QuellePufferspeicherKiSicht) },
        { KiMaskennamen.QUELLPROFIL,
          typeof(EPOS.UI.Dialoge.Simulation.QuellprofilKiSicht) },
        { KiMaskennamen.WAERMESENKE,
          typeof(EPOS.UI.Dialoge.Simulation.WaermesenkeKiSicht) },
        { KiMaskennamen.KOMPONENTENKONFIGURATION,
          typeof(EPOS.UI.Dialoge.Simulation.KomponentenKonfigurationKiSicht) },

        // Welle KI-F3: die Masken des BEDARFS. Sie melden je eine SICHTKLASSE an -
        // siehe OhneMarkupprobe.
        { KiMaskennamen.GEBAEUDE,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeKiSicht) },
        { KiMaskennamen.GEBAEUDE_WOHNFLAECHE,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeWohnflaecheKiSicht) },
        { KiMaskennamen.GEBAEUDE_KATALOG,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogKiSicht) },

        // Welle #465: die Gebaeudeverwaltung - DIESELBE Sichtklasse wie der Katalogeditor,
        // dazu die Satzwahl (SatzWahl als Begleiteigenschaft).
        { KiMaskennamen.GEBAEUDE_ADMIN,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogKiSicht) },
        { KiMaskennamen.GEBAEUDE_BEDARF,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudeBedarfKiSicht) },
        { KiMaskennamen.GEBAEUDETYP,
          typeof(EPOS.UI.Dialoge.Bedarf.GebaeudetypKiSicht) },
        { KiMaskennamen.BAUSTOFF_KATALOG,
          typeof(EPOS.UI.Dialoge.Bedarf.BaustoffKatalogKiSicht) },
        { KiMaskennamen.BAUTEILAUFBAU,
          typeof(EPOS.UI.Dialoge.Bedarf.BauteilaufbauKiSicht) },

        // Gebaeudesimulation G3, Welle D2: Zone und Bauteil, je eine Sichtklasse - die Bauteile
        // der Zone sind ein Raster zum Lesen, die drei Wahlfelder des Bauteils tragen ihre
        // Begleiteigenschaft <Feld>Wahl.
        { KiMaskennamen.ZONE,
          typeof(EPOS.UI.Dialoge.Bedarf.ZonenKiSicht) },
        { KiMaskennamen.BAUTEIL,
          typeof(EPOS.UI.Dialoge.Bedarf.BauteilKiSicht) },

        // Gebaeudesimulation G6b, Welle W2: der Luftaustausch zwischen den Zonen - ein Raster
        // (Luftstroeme[]), die Zonen zum Lesen, der Volumenstrom setzbar.
        { KiMaskennamen.LUFTAUSTAUSCH,
          typeof(EPOS.UI.Dialoge.Bedarf.LuftaustauschKiSicht) },

        // Gebaeudesimulation G7a, Welle W3: der Gebaeudeexport (gbXML) - die Postleitzahl setzbar,
        // die Bestaetigung der Meldungen nur zu lesen.
        { KiMaskennamen.GEBAEUDE_EXPORT,
          typeof(EPOS.UI.Dialoge.Export.GebaeudeExportKiSicht) },
        { KiMaskennamen.TYPPROFIL,
          typeof(EPOS.UI.Dialoge.Bedarf.TypProfilKiSicht) },

        // Die EINZIGE Maske der Welle KI-F3 ohne Sichtklasse: Ihr Satz ist
        // veraenderlich und bindet unmittelbar ans Markup.
        { KiMaskennamen.TYPSTAMM, typeof(EPOS.UI.Dialoge.Bedarf.TypStammDaten) },

        { KiMaskennamen.BEDARFSPROFILE,
          typeof(EPOS.UI.Dialoge.Bedarf.BedarfsProfileKiSicht) },

        // Welle #458, Stufe 3a: die Ueberlagerungen des Zapfprofils - je eine
        // Sichtklasse auf ihren Arbeitsstand (Zonen als Spalten, Eingaben der Auslegung).
        { KiMaskennamen.ZAPFPROFIL,
          typeof(EPOS.UI.Dialoge.Bedarf.ZapfprofilKiSicht) },
        { KiMaskennamen.ZAPFPROFIL_AUSLEGUNG,
          typeof(EPOS.UI.Dialoge.Bedarf.ZapfprofilAuslegungKiSicht) },
        { KiMaskennamen.BEDARFSTAG_KONSTRUKTOR,
          typeof(EPOS.UI.Dialoge.Bedarf.BedarfstagKonstruktorKiSicht) },
        // Zapfprofil Z4, Gruppe 2b: die Editoren der Stufe Experte (Tagesgang, Zapfkategorien).
        { KiMaskennamen.TAGESGANG_EDITOR,
          typeof(EPOS.UI.Dialoge.Bedarf.TagesgangEditorKiSicht) },
        { KiMaskennamen.ZAPFKATEGORIEN,
          typeof(EPOS.UI.Dialoge.Bedarf.ZapfkategorienEditorKiSicht) },
        // Zapfprofil Z4, Gruppe 3: der Katalogdialog der Nutzungsarten und sein Editor.
        { KiMaskennamen.BRAUCHWASSER_NUTZUNGSARTEN,
          typeof(EPOS.UI.Dialoge.Bedarf.TwwNutzungsartAdminKiSicht) },
        { KiMaskennamen.TWW_NUTZUNGSART_EDITOR,
          typeof(EPOS.UI.Dialoge.Bedarf.TwwNutzungsartEditorKiSicht) },
        // Zapfprofil Z4b, Gruppe 2: der Dialog der eingespielten VDI-4655-Typtage.
        { KiMaskennamen.BRAUCHWASSER_TYPTAGE,
          typeof(EPOS.UI.Dialoge.Bedarf.TwwTyptagImportKiSicht) },
        // Zapfprofil Z5, Gruppe 3: der Dialog der eingespielten Messreihen.
        { KiMaskennamen.BRAUCHWASSER_MESSREIHEN,
          typeof(EPOS.UI.Dialoge.Bedarf.TwwMessreihenKiSicht) },

        // DREI Masken auf EINER Sichtklasse: Prozesswaerme, Stromverbraucher und
        // Brauchwasser sind drei Katalogschluessel derselben Komponente.
        { KiMaskennamen.PROZESSWAERME_ADMIN,
          typeof(EPOS.UI.Dialoge.Bedarf.BedarfAdminKiSicht) },
        { KiMaskennamen.STROMVERBRAUCHER_ADMIN,
          typeof(EPOS.UI.Dialoge.Bedarf.BedarfAdminKiSicht) },
        { KiMaskennamen.BRAUCHWASSER_ADMIN,
          typeof(EPOS.UI.Dialoge.Bedarf.BedarfAdminKiSicht) },

        // Welle KI-F4: Kosten und Wirtschaftlichkeit. Auch sie melden je eine
        // SICHTKLASSE an - siehe OhneMarkupprobe.
        { KiMaskennamen.ENERGIETRAEGER,
          typeof(EPOS.UI.Dialoge.Kosten.EnergietraegerKiSicht) },
        { KiMaskennamen.ENERGIETRAEGER_VARIANTE,
          typeof(EPOS.UI.Dialoge.Kosten.EnergietraegerVarianteKiSicht) },
        { KiMaskennamen.LEISTUNGSPREISREIHE,
          typeof(EPOS.UI.Dialoge.Kosten.LeistungspreisReiheKiSicht) },
        { KiMaskennamen.KOSTENPROFIL,
          typeof(EPOS.UI.Dialoge.Kosten.KostenprofilKiSicht) },
        { KiMaskennamen.KOSTENFAKTOR_KATALOG,
          typeof(EPOS.UI.Dialoge.Kosten.KostenfaktorKatalogKiSicht) },
        { KiMaskennamen.EMISSIONSKATALOG,
          typeof(EPOS.UI.Dialoge.Kosten.EmissionskatalogKiSicht) },
        { KiMaskennamen.NUTZUNGSDAUER,
          typeof(EPOS.UI.Dialoge.Kosten.NutzungsdauerKiSicht) },
        { KiMaskennamen.VORLAGENPOSITION,
          typeof(EPOS.UI.Dialoge.Kosten.VorlagenPositionKiSicht) },
        { KiMaskennamen.CASE_EINGABE,
          typeof(EPOS.UI.Dialoge.Kosten.CaseEingabeKiSicht) },
        { KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER,
          typeof(EPOS.UI.Dialoge.Wirtschaftlichkeit.WirtschaftlichkeitParameterKiSicht) },
        { KiMaskennamen.BHKW_WIRTSCHAFTLICHKEIT,
          typeof(EPOS.UI.Dialoge.Wirtschaftlichkeit.BhkwWirtschaftlichkeitKiSicht) },
        { KiMaskennamen.TARIFSTRUKTUR,
          typeof(EPOS.UI.Dialoge.Wirtschaftlichkeit.TarifstrukturKiSicht) },
        { KiMaskennamen.PV_VERGUETUNG,
          typeof(EPOS.UI.Dialoge.Wirtschaftlichkeit.PhotovoltaikVerguetungKiSicht) },
        { KiMaskennamen.GESETZESKATALOG,
          typeof(EPOS.UI.Dialoge.Wirtschaftlichkeit.GesetzeskatalogKiSicht) },
        { KiMaskennamen.GESETZESKATALOG_ZEILE,
          typeof(EPOS.UI.Dialoge.Wirtschaftlichkeit.GesetzeskatalogZeileKiSicht) },

        // Die zwei REITERBLAETTER der Ansicht „Berichte und Kosten".
        { KiMaskennamen.KOSTENSEITE,
          typeof(EPOS.UI.Seiten.Berichte.KostenSeiteKiSicht) },
        { KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE,
          typeof(EPOS.UI.Seiten.Berichte.WirtschaftlichkeitSeiteKiSicht) },

        { KiMaskennamen.BEDARF_ERGEBNIS,
          typeof(EPOS.UI.Dialoge.Bedarf.BedarfErgebnisKiSicht) },

        // Die zweite Maske der Welle KI-F3 ohne Sichtklasse: Angemeldet ist die
        // GEWAEHLTE Zuordnung - dieselbe Bauart wie bei den Erzeugermasken.
        { KiMaskennamen.WAERMEBEDARF_EXTERN,
          typeof(EPOS.UI.Dialoge.Bedarf.WaermebedarfExternZeile) },

        { KiMaskennamen.SOLARGANGLINIE,
          typeof(EPOS.UI.Dialoge.Solarthermie.SolarganglinieKiSicht) },
        { KiMaskennamen.KLIMADATEN,
          typeof(EPOS.UI.Dialoge.Klimadaten.KlimadatenKiSicht) },

        // Welle KI-F5: die ERZEUGERKATALOGE. Die zwei Katalogeditoren melden ihr
        // DATEN-OBJEKT an - dieselbe Bauart wie Heizkessel und Pufferspeicher, und
        // damit tragen sie auch die Markup-Probe.
        { KiMaskennamen.BHKW,           typeof(BhkwKatalogDaten) },
        { KiMaskennamen.SOLARKOLLEKTOR,
          typeof(EPOS.UI.Dialoge.Solarthermie.SolarkollektorKatalogDaten) },

        // DREI Masken auf EINER Sichtklasse: Der Modulkatalog ist eine Komponente
        // mit drei Auspraegungen, und welche Felder sie fuehrt, sagt das Profil zur
        // Laufzeit - siehe OhneMarkupprobe.
        { KiMaskennamen.PV_MODULKATALOG,        typeof(ModulKatalogKiSicht) },
        { KiMaskennamen.STROMSPEICHER_KATALOG,  typeof(ModulKatalogKiSicht) },
        { KiMaskennamen.WECHSELRICHTER_KATALOG, typeof(ModulKatalogKiSicht) },

        // Welle #456: VIER Masken auf EINER Sichtklasse - die Verwaltungen der
        // Erzeugerkataloge. Ihre Profilfelder loest die Sichtklasse als FELDTAFEL
        // auf; hier greift nur die Typprobe vor dem Punkt, den Feldbestand haelt
        // Die_Erzeugerverwaltung_deklariert_genau_die_Felder_ihres_Profils.
        { KiMaskennamen.HEIZKESSEL_ADMIN,       typeof(KatalogBrowserKiSicht) },
        { KiMaskennamen.BHKW_ADMIN,             typeof(KatalogBrowserKiSicht) },
        { KiMaskennamen.SOLARKOLLEKTOREN_ADMIN, typeof(KatalogBrowserKiSicht) },
        { KiMaskennamen.PUFFERSPEICHER_ADMIN,   typeof(KatalogBrowserKiSicht) },

        // Welle KI-F6, Schritt 1 (STROM): drei Masken, drei Sichtklassen. Alle
        // drei fuehren ihren Stand in privaten Feldern der Komponente - siehe
        // OhneMarkupprobe.
        { KiMaskennamen.PEAK_SHAVING,
          typeof(EPOS.UI.Dialoge.Strom.PeakShavingKiSicht) },
        { KiMaskennamen.SPEICHER_ZEITREIHEN,
          typeof(EPOS.UI.Dialoge.Strom.SpeicherZeitreihenKiSicht) },
        { KiMaskennamen.STROMGANGLINIE_ADMIN,
          typeof(EPOS.UI.Dialoge.Strom.StromganglinieAdminKiSicht) },

        // Welle KI-F6, Schritt 2 (BERICHTE und PROJEKT): zwei Reiterblaetter der
        // Ansicht „Berichte und Kosten" und die zwei Projektmasken.
        { KiMaskennamen.BERICHTE_UEBERSICHT,
          typeof(EPOS.UI.Seiten.Berichte.UebersichtSeiteKiSicht) },
        { KiMaskennamen.BERICHTSEITE,
          typeof(EPOS.UI.Seiten.Berichte.BerichtSeiteKiSicht) },
        { KiMaskennamen.PROJEKT_KOPIE,
          typeof(EPOS.UI.Dialoge.Projekt.ProjektKopieKiSicht) },
        { KiMaskennamen.PROJEKT_VARIANTE,
          typeof(EPOS.UI.Dialoge.Projekt.ProjektVarianteKiSicht) },

        // Welle #458, Stufe 2: die uebrigen Masken mit Einstellwerten. Die
        // Ueberlagerung „Kenndaten" meldet ihren Arbeitsstand ueber eine Sichtklasse
        // an - die Stuetzstellen sind Spalten der gewaehlten Vorlaufstufe.
        { KiMaskennamen.KENNLINIEN, typeof(KennlinienKiSicht) },

        // Der Projektkopf des Assistenten und die Startseite: je eine Sichtklasse
        // ueber die Wege der Seite (Klimaregion samt Name, Name im Bearbeiten-Modus
        // fest; Klimaregion und Weiche der Solarthermiekachel).
        { KiMaskennamen.PROJEKTKOPF,
          typeof(EPOS.UI.Seiten.Assistent.ProjektKopfKiSicht) },
        { KiMaskennamen.STARTSEITE,
          typeof(EPOS.UI.Seiten.Start.StartseiteKiSicht) },

        // Die Programmeinstellungen: sechs benannte Werte und die Diagrammfarben als
        // FELDTAFEL - hier greift fuer die Farben nur die Typprobe vor dem Punkt, den
        // Feldbestand haelt Die_Einstellungen_fuehren_je_Farbrolle_ein_Feld.
        { KiMaskennamen.EINSTELLUNGEN,
          typeof(EPOS.UI.Dialoge.Admin.EinstellungenKiSicht) }
    };

    /// <summary>
    /// Je Maske die WAHLFELDER, deren Einträge der Dialog beim Anmelden ausdrücklich
    /// hereinreicht (KI-F1b, KI-D-Q6).
    /// </summary>
    /// <remarks>
    /// <b>Diese Liste ist die Gegenprobe zum Dialog.</b> Ein Wahlfeld löst seine Quelle
    /// entweder über die Begleiteigenschaft <c>&lt;Eigenschaft&gt;Wahl</c> am Daten-Objekt
    /// auf — das findet der Wächter selbst — oder über einen Lieferanten im Dialog, und
    /// den kann er nicht sehen. Steht ein Feld hier, das der Dialog NICHT liefert, bleibt
    /// es im Betrieb ohne Auswahl; steht eines nicht hier, das er liefert, meldet der
    /// Wächter es als fehlend. Beides fällt auf.
    /// </remarks>
    private static string[] Wahlquellen(string maske) => maske switch
    {
        KiMaskennamen.HEIZKESSEL           => new[] { "energietraeger" },
        KiMaskennamen.PUFFERSPEICHER       => new[] { "speichertyp" },
        KiMaskennamen.WAERMEPUMPE          => new[] { "typ", "leistungsstufen",
                                                      "aufstellung", "baujahr" },
        KiMaskennamen.HEIZKESSEL_PROJEKT   => new[] { "energietraeger" },
        KiMaskennamen.BHKW_PROJEKT         => new[] { "energietraeger" },
        KiMaskennamen.STROMSPEICHER_PROJEKT => new[] { "energietraeger" },
        KiMaskennamen.PHOTOVOLTAIK         => new[] { "energietraeger" },
        KiMaskennamen.WAERMEPUMPE_ANLAGE   => new[] { "energietraeger", "betriebsart", "typ",
                                                      "leistungsstufen", "aufstellung", "baujahr" },

        // Welle KI-F3: Der Kopfsatz eines Bedarfskatalogs meldet SEIN Daten-Objekt an;
        // die Typliste kennt nur der Dialog und reicht sie als Lieferant herein.
        KiMaskennamen.TYPSTAMM             => new[] { "typ" },

        // Die drei Bedarfskanaele kennt ebenfalls nur der Dialog; sie kommen als
        // Parameter der Huelle herein.
        KiMaskennamen.WAERMEBEDARF_EXTERN  => new[] { "kanal" },

        // Welle KI-F4: Die VARIANTE der Kostenverwaltung haengt am Stand, ihre Liste
        // aber auch (KostenKomponenteStand.Varianten) - eine Begleiteigenschaft
        // VarianteIdWahl fuehrte dieselbe Liste ein zweites Mal. Der Dialog meldet
        // sie deshalb als benannte Wahlquelle an.
        KiMaskennamen.KOSTENVERWALTUNG     => new[] { "variante" },

        // Welle KI-F5: Der BHKW-Katalogeditor bekommt seine Brennstoffliste von der
        // Huelle - genau wie der Heizkessel. Die drei MODULKATALOGE stehen hier
        // bewusst NICHT: Ihre einzige Wahl (die Zelltechnologie des PV-Moduls) loest
        // die Bruecke ueber die Begleiteigenschaft TechnologieWahl der Sichtklasse
        // selbst auf.
        KiMaskennamen.BHKW                 => new[] { "energietraeger" },

        // Die sechs Masken der SIMULATIONSKONFIGURATION stehen hier bewusst NICHT:
        // Sie melden je eine Sichtklasse an, und die traegt zu jedem Wahlfeld ihre
        // Begleiteigenschaft <Eigenschaft>Wahl - den Weg findet der Waechter selbst.
        // Wer eines ihrer Felder hier eintruege, naehme ihm genau diese Probe.
        _ => Array.Empty<string>()
    };

    // =====================================================================
    //  Der Wächter
    // =====================================================================

    [Theory]
    [MemberData(nameof(Masken))]
    public void Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt(string maske, Type daten)
    {
        IReadOnlyList<string> fehlt = KiMaskenanmeldung.Pruefe(maske, daten, Wahlquellen(maske));

        Assert.True(fehlt.Count == 0,
                    "Diese Eigenschaftspfade der Maske '" + maske + "' lösen an " +
                    daten.Name + " nicht auf: " + string.Join(", ", fehlt));
    }

    [Fact]
    public void Die_Gegenprobe_ein_falsches_Daten_Objekt_faellt_auf()
    {
        // Ohne die Typprobe VOR dem Punkt fiele ein an die falsche Maske gehängtes
        // Daten-Objekt erst auf, wenn zufällig eine Eigenschaft gleich heisst.
        IReadOnlyList<string> fehlt =
            KiMaskenanmeldung.Pruefe(KiMaskennamen.HEIZKESSEL, typeof(PufferSpKatalogDaten),
                                     Wahlquellen(KiMaskennamen.HEIZKESSEL));

        // ELF seit der Welle KI-F1b: die sechs Zahlen des Katalogeditors und die fuenf
        // uebrigen Eingabefelder (Name, Hersteller, Beschreibung, Energietraeger,
        // Brennwert) - keines davon gibt es an PufferSpKatalogDaten.
        Assert.Equal(11, fehlt.Count);
        Assert.Contains("HeizkesselKatalogDaten.Ptherm", fehlt);
    }

    [Fact]
    public void Eine_unbekannte_Maske_wird_benannt_und_nicht_verschwiegen()
    {
        IReadOnlyList<string> fehlt =
            KiMaskenanmeldung.Pruefe("Form_GibtEsNicht", typeof(HeizkesselKatalogDaten));

        Assert.Single(fehlt);
        Assert.Contains("Form_GibtEsNicht", fehlt[0], StringComparison.Ordinal);
    }

    // =====================================================================
    //  Der Katalog selbst
    // =====================================================================

    [Fact]
    public void Der_Katalog_fuehrt_achtzig_Masken()
    {
        KiDialogKatalog katalog = KiDialoge.Katalog;

        // VIERUNDSECHZIG seit der Welle KI-F7: Die Ueberlagerung „Anlagenwerte" der
        // Photovoltaik hat einen eigenen Schluessel bekommen (Anwenderentscheid
        // 21.09.2026, KI-D-Q7). ACHTUNDSECHZIG seit der Welle #456: die vier
        // Verwaltungen der Erzeugerkataloge (KI-D-Q11). Welle #458, Stufe 2: der
        // Kennlinieneditor, der Projektkopf des Assistenten, die Startseite und die
        // Programmeinstellungen. Welle #458, Stufe 3a: das Zapfprofil, seine Auslegung
        // und deren Bedarfstag-Konstruktor. Welle #465: die Gebaeudeverwaltung. Zapfprofil Z4,
        // Gruppe 2b: die Editoren Tagesgang und Zapfkategorien. Zapfprofil Z4, Gruppe 3: der Katalog
        // der Brauchwasser-Nutzungsarten und sein Editor. Zapfprofil Z4b, Gruppe 2: der Dialog der
        // eingespielten VDI-4655-Typtage. Zapfprofil Z5, Gruppe 3: der Dialog der Messdaten.
        // Gebaeudesimulation G3, Welle C: die Verwaltungen der Baustoffe und der
        // Bauteilaufbauten. Welle D2: Zone und Bauteil des Gebaeudeeditors. Stufe G6b, Welle W2:
        // der Luftaustausch zwischen den Zonen. Stufe G7a, Welle W3: der Gebaeudeexport.
        Assert.Equal(88, katalog.Anzahl);
        foreach (object[] zeile in Masken())
            Assert.True(katalog.Kennt((string)zeile[0]), (string)zeile[0]);
    }

    /// <summary>
    /// <b>Jede Katalogmaske hat ein Öffnungsziel, und die Projektmasken teilen sich
    /// eines</b> — die STARTSEITE, von deren Erzeugerkarte aus sie aufgehen (Welle
    /// KI‑F1).
    /// </summary>
    /// <remarks>
    /// <b>Der Wächter über die zwei Fundstellen.</b> <c>KiMaskenziele.STARTSEITE</c>
    /// steht im Kern als Zeichenkette, weil der Kern die Oberfläche nicht kennt;
    /// <c>Seitenschluessel.Startseite</c> steht in <c>EPOS.UI</c>. Laufen beide
    /// auseinander, führt <c>dialog_oeffnen</c> ins Leere — dasselbe Muster, mit dem
    /// die Stromspeicher-Ansicht zusammengehalten wird.
    /// </remarks>
    [Fact]
    public void Das_Ziel_der_Projektmasken_ist_der_Seitenschluessel_der_Startseite()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Startseite, KiMaskenziele.STARTSEITE);

        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.HEIZKESSEL_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.BHKW_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.STROMSPEICHER_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT));
        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.WAERMEPUMPE_ANLAGE));
    }

    /// <summary>
    /// <b>Die Masken der Simulationskonfiguration führen auf die ANSICHT, aus der sie
    /// aufgehen</b> (Welle KI‑F2).
    /// </summary>
    /// <remarks>
    /// Sie brauchen alle eine gewählte Komponente; kontextfrei öffnen lässt sich keine.
    /// <c>Masken.Simulation</c> kennt die Windows-Navigationstabelle, und dieselbe
    /// Zeichenkette ist der Seitenschlüssel der <c>AppWurzel</c>
    /// (<c>Seitenschluessel.Simulation</c> ist auf die Konstante des Kerns gesetzt) —
    /// hier führt ein Ziel also wirklich irgendwohin, anders als bei der
    /// Kostenverwaltung.
    /// </remarks>
    [Fact]
    public void Das_Ziel_der_Simulationsmasken_ist_die_Ansicht_Simulation()
    {
        // Voll ausgeschrieben: Die Testklasse fuehrt selbst eine Masken()-Methode,
        // und die verdeckt den Typnamen.
        string ziel = WindowsFormsApplication1.Masken.Simulation;

        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER_VERWALTUNG));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.QUELLE_ERDREICH));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.QUELLE_PUFFERSPEICHER));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.QUELLPROFIL));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.WAERMESENKE));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.KOMPONENTENKONFIGURATION));
    }

    /// <summary>
    /// <b>Die Gebäudeverwaltung ist eine eigene Maske</b> (Welle #465): Ihr Katalogschlüssel
    /// IST ihr Navigationsschlüssel (<c>Masken.GebaeudeAdmin</c>), und der Katalogeditor,
    /// der aus ihr aufgeht, führt dorthin.
    /// </summary>
    /// <remarks>
    /// Die Gebäudemaske des PROJEKTS, die Wohn-/Nutzflächenangabe und der Wärmebedarf hängen
    /// an einem offenen Projekt bzw. einer gewählten Projektzeile; ihr Ziel ist deshalb die
    /// Startseite — dieselbe Begründung wie bei den Erzeugermasken des Projekts.
    /// </remarks>
    [Fact]
    public void Das_Ziel_der_Gebaeudemasken_ist_die_Gebaeudeverwaltung()
    {
        string ziel = WindowsFormsApplication1.Masken.GebaeudeAdmin;

        Assert.Equal(ziel, KiMaskennamen.GEBAEUDE_ADMIN);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.GebaeudeAdmin, ziel);
        Assert.NotEqual(KiMaskennamen.GEBAEUDE, ziel);
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE_ADMIN));
        Assert.Equal(ziel, KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE_KATALOG));

        Assert.Equal(KiMaskenziele.STARTSEITE, KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE));
        Assert.Equal(KiMaskenziele.STARTSEITE,
                     KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE_WOHNFLAECHE));
        Assert.Equal(KiMaskenziele.STARTSEITE,
                     KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDE_BEDARF));
    }

    /// <summary>
    /// <b>Die Bedarfsmasken führen auf ihre VERWALTUNG</b> (Welle KI‑F3): die
    /// Gebäudetypen auf ihre eigene Maske, Profil und Kopfsatz eines Bedarfstyps auf
    /// die Stromverbraucher-Verwaltung, aus der sie als Überlagerung aufgehen.
    /// </summary>
    [Fact]
    public void Das_Ziel_der_Typmasken_ist_ihre_Verwaltung()
    {
        Assert.Equal(WindowsFormsApplication1.Masken.GebaeudetypenAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.GEBAEUDETYP));

        string strom = WindowsFormsApplication1.Masken.StromverbraucherAdmin;
        Assert.Equal(strom, KiMaskenziele.Ziel(KiMaskennamen.TYPPROFIL));
        Assert.Equal(strom, KiMaskenziele.Ziel(KiMaskennamen.TYPSTAMM));
    }

    /// <summary>
    /// <b>Die drei Bedarfs-Katalogverwaltungen sind DREI Masken</b> (Welle KI‑F3) —
    /// jede mit eigenem Katalogschlüssel und eigenem Menüweg, obwohl EINE Komponente
    /// sie zeichnet. Hier fallen Katalog- und Navigationsschlüssel zusammen.
    /// </summary>
    [Fact]
    public void Jede_Bedarfsverwaltung_hat_ihr_eigenes_Ziel()
    {
        Assert.Equal(WindowsFormsApplication1.Masken.ProzesswaermeAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.PROZESSWAERME_ADMIN));
        Assert.Equal(WindowsFormsApplication1.Masken.StromverbraucherAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.STROMVERBRAUCHER_ADMIN));
        Assert.Equal(WindowsFormsApplication1.Masken.BrauchwasserAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.BRAUCHWASSER_ADMIN));

        // Die Profile eines Projekts gehen aus den Startkacheln auf, das Ergebnis aus
        // der Ansicht „Simulation".
        Assert.Equal(KiMaskenziele.STARTSEITE,
                     KiMaskenziele.Ziel(KiMaskennamen.BEDARFSPROFILE));
        Assert.Equal(WindowsFormsApplication1.Masken.Simulation,
                     KiMaskenziele.Ziel(KiMaskennamen.BEDARF_ERGEBNIS));
    }

    /// <summary>
    /// <b>Der Wächter über die zwei Fundstellen des Klimadatenschlüssels</b>
    /// (Welle KI‑F3): <c>KiMaskenziele.KLIMADATEN</c> steht im Kern als Zeichenkette,
    /// weil der Kern die Oberfläche nicht kennt; <c>Seitenschluessel.Klimadaten</c>
    /// steht in <c>EPOS.UI</c>. Laufen beide auseinander, führt <c>dialog_oeffnen</c>
    /// ins Leere — dasselbe Muster wie bei der Startseite.
    /// </summary>
    [Fact]
    public void Das_Ziel_der_Klimadaten_ist_ihr_Seitenschluessel()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Klimadaten, KiMaskenziele.KLIMADATEN);
        Assert.Equal(KiMaskenziele.KLIMADATEN, KiMaskenziele.Ziel(KiMaskennamen.KLIMADATEN));

        // Die zwei Ganglinienmasken des Projekts gehen aus den Startkacheln auf.
        Assert.Equal(KiMaskenziele.STARTSEITE,
                     KiMaskenziele.Ziel(KiMaskennamen.WAERMEBEDARF_EXTERN));
        Assert.Equal(KiMaskenziele.STARTSEITE,
                     KiMaskenziele.Ziel(KiMaskennamen.SOLARGANGLINIE));
    }

    [Fact]
    public void Kein_Feld_traegt_mehr_einen_WinForms_Controlnamen()
    {
        // Der Beleg für die Umstellung: Ein Controlname ist EINE Stufe
        // ("tb_th_Leistung") und läuft damit gegen die Zwei-Stufen-Regel des
        // Eigenschaftspfades. Geprüft wird trotzdem am Präfix — er ist das, was ein
        // Rückfall in die alte Schreibweise zuerst wieder mitbrächte.
        var funde = new List<string>();

        foreach (KiDialog d in KiDialoge.Katalog)
            foreach (KiDialogFeld f in d.Felder)
            {
                Assert.True(KiEigenschaftspfad.IstGueltig(f.Eigenschaftspfad), f.Eigenschaftspfad);

                if (f.Eigenschaftspfad.StartsWith("tb_", StringComparison.Ordinal) ||
                    f.Eigenschaftspfad.StartsWith("textBox", StringComparison.Ordinal) ||
                    f.Eigenschaftspfad.StartsWith("gb_", StringComparison.Ordinal))
                    funde.Add(d.Maskenname + "." + f.Name + " → " + f.Eigenschaftspfad);
            }

        Assert.True(funde.Count == 0, string.Join("; ", funde));
    }

    [Fact]
    public void Keine_Maske_traegt_mehr_eine_Knopfposition()
    {
        // Sie waren gemessene Koordinaten im Client-Bereich gefallener WinForms-Masken;
        // den Aufrufknopf zeichnet seit iU9-W15b.5 der Baustein KiKnopf im Dialogkopf.
        foreach (KiDialog d in KiDialoge.Katalog)
            Assert.False(d.HatKnopfposition, d.Maskenname);
    }

    [Fact]
    public void Die_vier_Startmasken_fuehren_11_17_5_und_11_Felder()
    {
        // Der Feldumfang ist mit #200 NICHT gewachsen — sonst liesse sich hinterher
        // nicht sagen, was den Feldblock verändert hat: der Umfang oder der
        // Auflösungsweg (Fachkonzept 11.6).
        //
        // GESCHRUMPFT ist er am 15.09.2026, und zwar allein beim Heizkessel: von 15 auf
        // 6. Mit dem Anwenderentscheid „Der Dialog über Button Bearbeiten soll keine
        // Kosten und Emissionen enthalten" hat die Maske neun Felder verloren; der
        // Katalog führt sie deshalb auch nicht mehr (siehe
        // Die_Heizkesselmaske_fuehrt_nur_noch_die_sechs_sichtbaren_Felder).
        //
        // GEWACHSEN ist er mit der Welle KI-F7 bei der Photovoltaik: von 15 auf 17.
        // Die zwei AUSLEGUNGSTEMPERATUREN des Projekts stehen im Strangabschnitt und
        // fehlten dem Katalog, weil sie nicht an der Anlagenzeile haengen
        // (Anwenderentscheid 21.09.2026, KI-D-Q7).
        //
        // Welle #458, Stufe 2: Dazu kommen bei der Photovoltaik die Felder des
        // Aufklappers „Alle Daten" - so viele, wie das Profil des Modulkatalogs fuehrt
        // (Die_Projektmasken_fuehren_Alle_Daten_genau_nach_ihrem_Profil).
        Assert.Equal(11, KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL)!.Felder.Count);
        Assert.Equal(17, KiDialoge.Katalog.Finde(KiMaskennamen.PHOTOVOLTAIK)!.Felder
                             .Count(f => !IstAlleDaten(f)));
        Assert.Equal(5, KiDialoge.Katalog.Finde(KiMaskennamen.PUFFERSPEICHER)!.Felder.Count);
        Assert.Equal(11, KiDialoge.Katalog.Finde(KiMaskennamen.WAERMEPUMPE)!.Felder.Count);

        // Die Ueberlagerung „Anlagenwerte" fuehrt die VIER Kennwerte des
        // Wechselrichters - Nennleistung und die drei Punkte der Teillastkennlinie.
        Assert.Equal(4, KiDialoge.Katalog.Finde(KiMaskennamen.PV_ANLAGENWERTE)!.Felder.Count);
    }

    /// <summary>
    /// <b>Die Kostenverwaltung führt ZWÖLF Felder</b> — die neun der Wellen KI‑F1 bis
    /// KI‑F4 und die drei Lücken, die die Welle KI‑F7 geschlossen hat
    /// (Anwenderentscheid 21.09.2026, KI‑D‑Q7).
    /// </summary>
    /// <remarks>
    /// <b>Zwei der drei sind nur lesbar.</b> Komponentenwahl und PV‑Vergütung zu
    /// wechseln lädt nach — dieselbe Begründung, die das Wahlfeld <c>variante</c> seit
    /// der Welle KI‑F4 trägt. Das PV‑Projekt wählt allein das Ziel eines Knopfes und
    /// bleibt setzbar.
    /// </remarks>
    [Fact]
    public void Die_Kostenverwaltung_fuehrt_zwoelf_Felder_samt_der_drei_Luecken()
    {
        KiDialog kv = KiDialoge.Katalog.Finde(KiMaskennamen.KOSTENVERWALTUNG)!;

        Assert.Equal(12, kv.Felder.Count);

        foreach (string name in new[] { "komponentenwahl", "pv_verguetung", "pv_projekt" })
        {
            KiDialogFeld feld = kv.FindeFeld(name)!;
            Assert.NotNull(feld);
            Assert.Equal(KiParameterTyp.Wahl, feld.Typ);
            Assert.False(feld.IstSpalte, name);
        }

        Assert.True(kv.FindeFeld("komponentenwahl")!.NurLesen);
        Assert.True(kv.FindeFeld("pv_verguetung")!.NurLesen);
        Assert.False(kv.FindeFeld("pv_projekt")!.NurLesen);

        // Der Bestand laeuft unveraendert weiter - Namen, Arten und das nurLesen des
        // Wahlfeldes variante aus der Welle KI-F4.
        Assert.Equal(KiParameterTyp.Wahl, kv.FindeFeld("variante")!.Typ);
        Assert.True(kv.FindeFeld("variante")!.NurLesen);
        Assert.True(kv.FindeFeld("betrag")!.NurLesen);
        Assert.True(kv.FindeFeld("nutzungsdauer")!.IstSpalte);
    }

    /// <summary>
    /// <b>Die MODULKOSTEN der Wärmepumpenverwaltung sind nur lesbar</b>
    /// (Anwenderentscheid 21.09.2026, KI‑D‑Q7).
    /// </summary>
    /// <remarks>
    /// Die Maske zeigt sie als Lesewert mit Herleitungszeile — gepflegt werden sie in
    /// der Kostenverwaltung. Der Katalog führte sie bis dahin setzbar und bot damit an,
    /// eine Zahl zu ändern, für die es auf der Maske kein Eingabefeld gibt; bei
    /// <c>Form_WP_Anlage</c> war dieselbe Größe schon vorher <c>nurLesen</c>.
    /// </remarks>
    [Fact]
    public void Die_Modulkosten_beider_Waermepumpenmasken_sind_nur_lesbar()
    {
        Assert.True(KiDialoge.Katalog.Finde(KiMaskennamen.WAERMEPUMPE)!
                              .FindeFeld("modulkosten")!.NurLesen);
        Assert.True(KiDialoge.Katalog.Finde(KiMaskennamen.WAERMEPUMPE_ANLAGE)!
                              .FindeFeld("modulkosten")!.NurLesen);

        // Die Nennleistung derselben Maske bleibt setzbar — der Feldsatz ist nicht
        // insgesamt gesperrt, nur diese eine Anzeige.
        Assert.False(KiDialoge.Katalog.Finde(KiMaskennamen.WAERMEPUMPE)!
                               .FindeFeld("nennleistung")!.NurLesen);
    }

    /// <summary>
    /// <b>Die fünf Erzeugerkataloge der Welle KI‑F5</b> — je Maske genau die Felder,
    /// die sie zeigt.
    /// </summary>
    /// <remarks>
    /// <para>Der BHKW-Editor führt DREIZEHN und damit zwei mehr als der Heizkessel: den
    /// Motortyp und die zwei Wirkungsgradanteile samt ihrer Summe, dafür keinen
    /// Brennwertschalter. Der Kollektoreditor führt elf ohne die Investitionskosten —
    /// sie sind am 15.09.2026 aus der Maske gefallen — und ohne Vor- und Rücklauf, die
    /// der Katalog nicht mehr führt.</para>
    /// <para>Die drei MODULKATALOGE zählen genau die Felder ihres Profils
    /// (<c>ModulKatalogProfil.Finde</c>); wächst dem Profil eines zu, fällt es hier
    /// auf und nicht beim Anwender.</para>
    /// </remarks>
    [Fact]
    public void Die_fuenf_Erzeugerkataloge_fuehren_13_11_15_14_und_26_Felder()
    {
        Assert.Equal(13, KiDialoge.Katalog.Finde(KiMaskennamen.BHKW)!.Felder.Count);
        Assert.Equal(11, KiDialoge.Katalog.Finde(KiMaskennamen.SOLARKOLLEKTOR)!.Felder.Count);
        Assert.Equal(15, KiDialoge.Katalog.Finde(KiMaskennamen.PV_MODULKATALOG)!.Felder.Count);
        Assert.Equal(14, KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_KATALOG)!.Felder.Count);
        Assert.Equal(26, KiDialoge.Katalog.Finde(KiMaskennamen.WECHSELRICHTER_KATALOG)!.Felder.Count);
    }

    /// <summary>
    /// <b>Die drei Strommasken der Welle KI‑F6 führen 22, 18 und 2 Felder.</b>
    /// </summary>
    /// <remarks>
    /// <para>Die LASTSPITZENKAPPUNG führt achtzehn Eingaben und vier Anzeigen (Quelle,
    /// Ganglinie und die achtzehn Zahlen und Schalter; dazu Reihenzeile, Herkunft und
    /// das offene Blatt).</para>
    /// <para>Die ZEITREIHEN-Leseregeln führen sechzehn Einstellwerte und zwei
    /// Anzeigen; Datei und Rolle kommen vom Wirt.</para>
    /// <para>Die STROMGANGLINIEN-Verwaltung führt genau EINEN Einstellwert — das
    /// Zeitraster — und daneben die Markierung als Anzeige. Alles Übrige darauf ist
    /// Suche, Auswahl und Ladevorgang (KI‑D‑Q5).</para>
    /// </remarks>
    [Fact]
    public void Die_drei_Strommasken_fuehren_22_18_und_2_Felder()
    {
        KiDialog peak = KiDialoge.Katalog.Finde(KiMaskennamen.PEAK_SHAVING)!;
        KiDialog reihe = KiDialoge.Katalog.Finde(KiMaskennamen.SPEICHER_ZEITREIHEN)!;
        KiDialog admin = KiDialoge.Katalog.Finde(KiMaskennamen.STROMGANGLINIE_ADMIN)!;

        Assert.Equal(22, peak.Felder.Count);
        Assert.Equal(18, reihe.Felder.Count);
        Assert.Equal(2, admin.Felder.Count);

        // KEINE Knöpfe: „Berechnen", „Minimale Schwelle", „Übernehmen", „Einlesen"
        // und „Löschen" sind rechnende bzw. datenbankwirksame Aktionen der Stufen 2
        // und 3 und gehören in das Aktionsregister.
        Assert.Empty(peak.Knoepfe);
        Assert.Empty(reihe.Knoepfe);
        Assert.Empty(admin.Knoepfe);

        // Drei Felder der Lastspitzenkappung sind ABGELEITET, zwei der Leseregeln,
        // eines der Verwaltung.
        Assert.Equal(3, peak.Felder.Count(f => f.NurLesen));
        Assert.Equal(2, reihe.Felder.Count(f => f.NurLesen));
        Assert.Equal(1, admin.Felder.Count(f => f.NurLesen));
    }

    /// <summary>
    /// <b>Die Ziele der Welle KI‑F6, Schritt 1.</b> Lastspitzenkappung und
    /// Stromganglinien-Verwaltung sind eigene Fenster mit einem Weg im Menü — ihr
    /// Katalogschlüssel IST ihr Navigationsschlüssel. Die Leseregeln gehen als
    /// Überlagerung aus Station 2 der Stromspeicher-Auslegung auf und führen deshalb
    /// auf diese Ansicht.
    /// </summary>
    [Fact]
    public void Das_Ziel_der_Strommasken_ist_ihr_eigener_Weg_oder_die_Auslegung()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.PeakShaving,
                     KiMaskenziele.Ziel(KiMaskennamen.PEAK_SHAVING));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.StromganglinieAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.STROMGANGLINIE_ADMIN));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.StromspeicherAuslegung,
                     KiMaskenziele.Ziel(KiMaskennamen.SPEICHER_ZEITREIHEN));
    }

    /// <summary>
    /// <b>Die Ziele der Welle KI‑F6, Schritt 2.</b> Die zwei Reiterblätter führen auf
    /// die Ansicht „Berichte und Kosten" — wie schon Kosten- und
    /// Wirtschaftlichkeitsseite der Welle KI‑F4. „Projekt speichern unter" ist selbst
    /// eine Maske der Navigationstabelle; „Als Variante speichern" hängt am Menüweg,
    /// und <c>KiMaskenziele.PROJEKT_VARIANTE</c> steht im Kern als Zeichenkette gegen
    /// <c>Seitenschluessel.ProjektAlsVariante</c> in <c>EPOS.UI</c>.
    /// </summary>
    [Fact]
    public void Das_Ziel_der_Berichts_und_Projektmasken_steht_fest()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.BerichteKosten,
                     KiMaskenziele.Ziel(KiMaskennamen.BERICHTE_UEBERSICHT));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.BerichteKosten,
                     KiMaskenziele.Ziel(KiMaskennamen.BERICHTSEITE));

        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.ProjektSpeichernUnter,
                     KiMaskenziele.Ziel(KiMaskennamen.PROJEKT_KOPIE));

        // Der Wächter über die ZWEI Fundstellen.
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.ProjektAlsVariante,
                     KiMaskenziele.PROJEKT_VARIANTE);
        Assert.Equal(KiMaskenziele.PROJEKT_VARIANTE,
                     KiMaskenziele.Ziel(KiMaskennamen.PROJEKT_VARIANTE));
    }

    /// <summary>
    /// <b>Der Wächter über die zwei Fundstellen der vier MENÜZIELE</b> (Welle KI‑F8):
    /// Kostenverwaltung, Energieträgerverwaltung, Nutzungsdauern und Gesetzliche
    /// Parameter sind Menüpunkte, keine Maskenschlüssel. Ihr Ziel steht im Kern als
    /// Zeichenkette und in <c>EPOS.UI</c> als <c>Seitenschluessel</c>; laufen beide
    /// auseinander, führt <c>dialog_oeffnen</c> ins Leere — dasselbe Muster wie bei
    /// den Klimadaten.
    /// </summary>
    [Fact]
    public void Die_Menueziele_der_Verwaltungsmasken_sind_ihre_Seitenschluessel()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Kostenverwaltung,
                     KiMaskenziele.KOSTENVERWALTUNG);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.EnergietraegerVerwaltung,
                     KiMaskenziele.ENERGIETRAEGER_VERWALTUNG);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.NutzungsdauerVerwaltung,
                     KiMaskenziele.NUTZUNGSDAUER_VERWALTUNG);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Gesetzeskatalog,
                     KiMaskenziele.GESETZESKATALOG);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Einstellungen,
                     KiMaskenziele.EINSTELLUNGEN);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.BaustoffKatalog,
                     KiMaskenziele.BAUSTOFF_KATALOG);
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.BauteilaufbauKatalog,
                     KiMaskenziele.BAUTEILAUFBAU_KATALOG);
        Assert.Equal(KiMaskenziele.BAUSTOFF_KATALOG, KiMaskenziele.Ziel(KiMaskennamen.BAUSTOFF_KATALOG));
        Assert.Equal(KiMaskenziele.BAUTEILAUFBAU_KATALOG, KiMaskenziele.Ziel(KiMaskennamen.BAUTEILAUFBAU));
    }

    /// <summary>
    /// <b>Die Programmeinstellungen führen je Farbrolle ein Feld</b> (Welle #458,
    /// Stufe 2) — erzeugt aus derselben Rollenliste, aus der die Hülle die Rubrik
    /// „Diagramme" füllt (<c>Diagrammfarben.Gruppen</c>): Name mit Vorsilbe, Pfad der
    /// Feldtafel, Anzeigename der Rolle, setzbar. Er ersetzt für die Farben die
    /// Reflection-Probe, die eine Feldtafel nicht leisten kann.
    /// </summary>
    [Fact]
    public void Die_Einstellungen_fuehren_je_Farbrolle_ein_Feld()
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.EINSTELLUNGEN)!;
        Assert.NotNull(d);

        // Sechs Werte der Anwendung und — Berichtsvorlagen BV-E1 — Firma und Vorlagenordner
        // der Rubrik „Bericht", BV-E2 (Entscheid BV-E2-1) das Logo, dazu je Farbrolle ein Feld.
        var rollen = WindowsFormsApplication1.Zeichnung.Diagrammfarben.Rollen;
        Assert.Equal(9 + rollen.Count, d.Felder.Count);
        Assert.Equal(KiDialoge.EINSTELLUNGEN_SICHT + ".BerichtFirma", d.FindeFeld("bericht_firma")!.Eigenschaftspfad);
        Assert.Equal(KiDialoge.EINSTELLUNGEN_SICHT + ".BerichtVorlagenordner",
                     d.FindeFeld("bericht_vorlagenordner")!.Eigenschaftspfad);
        Assert.Equal(KiDialoge.EINSTELLUNGEN_SICHT + ".BerichtLogo", d.FindeFeld("bericht_logo")!.Eigenschaftspfad);
        Assert.Equal("Logo", d.FindeFeld("bericht_logo")!.Anzeigename);

        foreach (WindowsFormsApplication1.Zeichnung.Farbrolle rolle in rollen)
        {
            KiDialogFeld? f = d.FindeFeld(KiDialoge.FARBFELD_VORSILBE + rolle.Name.ToLowerInvariant());
            Assert.True(f is not null, "Die Farbrolle " + rolle.Name + " fehlt im Katalog.");
            Assert.Equal(KiDialoge.EINSTELLUNGEN_SICHT + "." + rolle.Name, f!.Eigenschaftspfad);
            Assert.Equal(WindowsFormsApplication1.Zeichnung.Diagrammfarben.Anzeigename(rolle), f.Anzeigename);
            Assert.False(f.NurLesen, rolle.Name);
        }

        Assert.Equal(nameof(EPOS.UI.Dialoge.Admin.EinstellungenKiSicht), KiDialoge.EINSTELLUNGEN_SICHT);
    }

    /// <summary>
    /// <b>Der Wächter über die zwei Fundstellen der ARGUMENTE</b> (Anwenderentscheid
    /// KI‑D‑Q8): Der Reiter der Startseite und das Blatt der Ansicht „Berichte und
    /// Kosten" stehen im Kern als Zeichenkette, weil der Kern die Oberfläche nicht
    /// kennt. Läuft eine der sechs auseinander, öffnet der Assistent die richtige
    /// Ansicht am falschen Platz — und das fiele nur am Gerät auf.
    /// </summary>
    [Fact]
    public void Die_Argumente_der_Zieltabelle_sind_Reiter_und_Blaetter_der_Oberflaeche()
    {
        Assert.Equal(EPOS.UI.Seiten.Start.Reiterschluessel.Erzeuger,
                     KiMaskenziele.REITER_ERZEUGER);
        Assert.Equal(EPOS.UI.Seiten.Start.Reiterschluessel.Waermebedarf,
                     KiMaskenziele.REITER_WAERMEBEDARF);

        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_UEBERSICHT,
                     KiMaskenziele.BLATT_UEBERSICHT);
        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_KOSTEN,
                     KiMaskenziele.BLATT_KOSTEN);
        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_WIRTSCHAFT,
                     KiMaskenziele.BLATT_WIRTSCHAFT);
        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_BERICHT,
                     KiMaskenziele.BLATT_BERICHT);

        // Und die Tabelle selbst, an einem Beispiel je Art.
        Assert.Equal(EPOS.UI.Seiten.Start.Reiterschluessel.Erzeuger,
                     KiMaskenziele.Argument(KiMaskennamen.WAERMEPUMPE_ANLAGE));
        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_KOSTEN,
                     KiMaskenziele.Argument(KiMaskennamen.KOSTENSEITE));
    }

    /// <summary>
    /// <b>Die vier Masken der Welle KI‑F6, Schritt 2, führen 6, 7, 5 und 4 Felder.</b>
    /// </summary>
    /// <remarks>
    /// <para>Das Reiterblatt „Übersicht" führt vier Einstellwerte (Stammprojekt,
    /// Filter, markierte Version, Bezeichner) und zwei Anzeigen; „Bericht" vier
    /// Einstellwerte (Ausgabe, Zielordner und — Berichtsvorlagen BV-E1 und BV-E7 — die Word- und
    /// die Excel-Vorlage als Wahl) und drei Anzeigen — die zwei Mengen stehen als Aufstellung.</para>
    /// <para>„Projekt speichern unter" führt fünf Verwaltungsangaben, „Als Variante
    /// speichern" drei Einstellwerte und den gerechneten Zielnamen.</para>
    /// </remarks>
    [Fact]
    public void Die_vier_Berichts_und_Projektmasken_fuehren_6_7_5_und_4_Felder()
    {
        KiDialog ueb = KiDialoge.Katalog.Finde(KiMaskennamen.BERICHTE_UEBERSICHT)!;
        KiDialog ber = KiDialoge.Katalog.Finde(KiMaskennamen.BERICHTSEITE)!;
        KiDialog kop = KiDialoge.Katalog.Finde(KiMaskennamen.PROJEKT_KOPIE)!;
        KiDialog var = KiDialoge.Katalog.Finde(KiMaskennamen.PROJEKT_VARIANTE)!;

        Assert.Equal(6, ueb.Felder.Count);
        Assert.Equal(7, ber.Felder.Count);
        Assert.Equal(KiParameterTyp.Wahl, ber.FindeFeld("vorlage")!.Typ);
        Assert.Equal("BerichtSeiteKiSicht.Vorlage", ber.FindeFeld("vorlage")!.Eigenschaftspfad);
        Assert.Equal(KiParameterTyp.Wahl, ber.FindeFeld("excel_vorlage")!.Typ);
        Assert.Equal("BerichtSeiteKiSicht.ExcelVorlage", ber.FindeFeld("excel_vorlage")!.Eigenschaftspfad);
        Assert.Equal(5, kop.Felder.Count);
        Assert.Equal(4, var.Felder.Count);

        Assert.Equal(2, ueb.Felder.Count(f => f.NurLesen));
        Assert.Equal(3, ber.Felder.Count(f => f.NurLesen));
        Assert.Empty(kop.Felder.Where(f => f.NurLesen));
        Assert.Single(var.Felder.Where(f => f.NurLesen));

        foreach (KiDialog d in new[] { ueb, ber, kop, var }) Assert.Empty(d.Knoepfe);
    }

    /// <summary>
    /// <b>Jedes Feld der drei Modulkataloge trägt den Schlüssel eines Profilfeldes</b>
    /// — und umgekehrt fehlt keines.
    /// </summary>
    /// <remarks>
    /// Das ist die schärfere Probe als eine Zahl: Der Feldsatz des Modulkatalogs ist
    /// DATEN (<c>ModulKatalogProfil</c>), und der Katalogeintrag ist seine benannte
    /// Sicht darauf. Liefe eines von beiden dem anderen davon, böte der Assistent ein
    /// Feld an, das die Maske nicht zeigt — oder er verschwiege eines, das sie zeigt.
    /// Geprüft wird über die ANZEIGENAMEN, denn nur sie verbinden beide Seiten: Der
    /// Katalog nennt seine Felder sprachneutral (<c>u_mpp</c>), das Profil über die
    /// Beschriftung der Maske.
    /// </remarks>
    [Theory]
    [InlineData(ModulKatalogArt.Photovoltaik, KiMaskennamen.PV_MODULKATALOG)]
    [InlineData(ModulKatalogArt.Stromspeicher, KiMaskennamen.STROMSPEICHER_KATALOG)]
    [InlineData(ModulKatalogArt.Wechselrichter, KiMaskennamen.WECHSELRICHTER_KATALOG)]
    public void Der_Modulkatalog_deklariert_genau_die_Felder_seines_Profils(
        ModulKatalogArt art, string maske)
    {
        ModulKatalogProfil profil = ModulKatalogProfil.Finde(art, s => Resource.ResourceManager.GetString(s) ?? s);
        KiDialog dialog = KiDialoge.Katalog.Finde(maske)!;

        Assert.Equal(profil.Felder.Count, dialog.Felder.Count);

        var ausDerMaske = new HashSet<string>(profil.Felder.Select(f => f.Bezeichnung.TrimEnd(' ', ':')),
                                              StringComparer.Ordinal);
        foreach (KiDialogFeld f in dialog.Felder)
            Assert.True(ausDerMaske.Contains(f.Anzeigename.TrimEnd(' ', ':')),
                        "Das Katalogfeld '" + f.Name + "' heißt '" + f.Anzeigename +
                        "' — so steht es an der Maske '" + maske + "' nicht.");

        // Und die GESPERRTEN Felder des Profils sind im Katalog nur lesend.
        foreach (ModulKatalogFeld p in profil.Felder.Where(p => p.Gesperrt))
        {
            KiDialogFeld? k = dialog.Felder.FirstOrDefault(
                f => string.Equals(f.Anzeigename.TrimEnd(' ', ':'), p.Feldname, StringComparison.Ordinal));
            Assert.NotNull(k);
            Assert.True(k!.NurLesen, p.Feldname);
        }
    }

    /// <summary>
    /// <b>Die Verwaltung eines Erzeugerkatalogs deklariert GENAU die Felder ihres
    /// Profils</b> — und dazu das Wahlfeld <c>satz</c> (Welle #456, KI‑D‑Q11).
    /// </summary>
    /// <remarks>
    /// <para>Die Feldkarte ENTSTEHT aus dem <c>KatalogBrowserProfil</c>; dieser Fall hält
    /// fest, dass die Erzeugung jede Eigenschaft eines Profilfeldes trägt: Schlüssel
    /// (klein), Pfad der Feldtafel, Beschriftung ohne Doppelpunkt, Feldtyp, Einheit und
    /// die Sperre eines nicht editierbaren Feldes. Er ersetzt für die vier Masken die
    /// Reflection-Probe, die eine Feldtafel nicht leisten kann.</para>
    /// <para>Dazu die ERLÄUTERUNG: Kein Feld darf den Rückfalltext tragen — sonst wüchse
    /// dem Profil ein Feld zu, für das niemand einen Satz geschrieben hat, und
    /// <c>dialog_parameter_erklaeren</c> wiederholte nur den Namen.</para>
    /// </remarks>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel, KiMaskennamen.HEIZKESSEL_ADMIN)]
    [InlineData(KatalogBrowserArt.Bhkw, KiMaskennamen.BHKW_ADMIN)]
    [InlineData(KatalogBrowserArt.Solarkollektoren, KiMaskennamen.SOLARKOLLEKTOREN_ADMIN)]
    [InlineData(KatalogBrowserArt.Pufferspeicher, KiMaskennamen.PUFFERSPEICHER_ADMIN)]
    public void Die_Erzeugerverwaltung_deklariert_genau_die_Felder_ihres_Profils(
        KatalogBrowserArt art, string maske)
    {
        KatalogBrowserProfil profil =
            KatalogBrowserProfil.Finde(art, s => Resource.ResourceManager.GetString(s) ?? s);
        KiDialog dialog = KiDialoge.Katalog.Finde(maske)!;

        Assert.NotNull(dialog);
        Assert.Equal(maske, KiMaskennamen.KatalogBrowser(art));
        Assert.Equal(profil.Titel, dialog.Anzeigename);
        Assert.Equal(nameof(KatalogBrowserKiSicht), KiDialoge.KATALOGBROWSER_SICHT);

        // Ein Feld je Profilzeile - und der Satz.
        Assert.Equal(profil.Detailfelder.Count + 1, dialog.Felder.Count);

        KiDialogFeld satz = dialog.FindeFeld("satz")!;
        Assert.NotNull(satz);
        Assert.True(satz.IstWahl);
        Assert.True(satz.Satzwahl);
        Assert.False(satz.NurLesen);

        foreach (BrowserDetailfeld p in profil.Detailfelder)
        {
            KiDialogFeld? k = dialog.FindeFeld(p.Schluessel.ToLowerInvariant());
            Assert.True(k is not null, "Das Profilfeld " + p.Schluessel + " fehlt im Katalog.");

            Assert.Equal(nameof(KatalogBrowserKiSicht) + "." + p.Schluessel, k!.Eigenschaftspfad);
            Assert.Equal(p.Feldname, k.Anzeigename);
            Assert.Equal(p.Einheit, k.Einheit);
            Assert.Equal(KiDialoge.Feldtyp(p.Art), k.Typ);
            Assert.Equal(!p.Editierbar, k.NurLesen);
            Assert.False(k.Satzwahl, p.Schluessel);

            string rueckfall = string.Format(CultureInfo.CurrentCulture,
                                             Resource.KI_DLG_KBROW_FELD_ERL, p.Feldname);
            Assert.NotEqual(rueckfall, k.Erlaeuterung);
        }
    }

    /// <summary>
    /// <b>Die Verwaltungen führen dorthin, wo sie aufgehen</b> — ihr Katalogschlüssel ist
    /// ihr Navigationsschlüssel, und die Katalogeditoren darüber führen an dieselbe
    /// Stelle (Welle #456).
    /// </summary>
    [Fact]
    public void Das_Ziel_der_Erzeugerverwaltungen_ist_ihr_eigener_Weg()
    {
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.HeizkesselAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.HEIZKESSEL_ADMIN));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.BhkwAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.BHKW_ADMIN));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.SolarkollektorenAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.SOLARKOLLEKTOREN_ADMIN));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.PufferSpAdmin,
                     KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER_ADMIN));

        // Editor und Verwaltung: EIN Ziel.
        Assert.Equal(KiMaskenziele.Ziel(KiMaskennamen.HEIZKESSEL),
                     KiMaskenziele.Ziel(KiMaskennamen.HEIZKESSEL_ADMIN));
        Assert.Equal(KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER),
                     KiMaskenziele.Ziel(KiMaskennamen.PUFFERSPEICHER_ADMIN));
    }

    /// <summary>
    /// <b>Die Bedarfsverwaltung führt ihre Kenndaten als EINGABEN</b> (Welle #456): Typ
    /// (Wahl aus der Typliste), Beschreibung und die zwölf Monatswerte sind setzbar;
    /// Jahressumme und Bedarfsart bleiben Anzeigen, und der Satz ist Satzwahl.
    /// </summary>
    [Theory]
    [InlineData(KiMaskennamen.PROZESSWAERME_ADMIN)]
    [InlineData(KiMaskennamen.STROMVERBRAUCHER_ADMIN)]
    [InlineData(KiMaskennamen.BRAUCHWASSER_ADMIN)]
    public void Die_Bedarfsverwaltung_fuehrt_ihre_Kenndaten_als_Eingaben(string maske)
    {
        KiDialog d = KiDialoge.Katalog.Finde(maske)!;

        Assert.Equal(17, d.Felder.Count);
        Assert.True(d.FindeFeld("satz")!.Satzwahl);
        Assert.True(d.FindeFeld("typ")!.IstWahl);
        Assert.False(d.FindeFeld("typ")!.NurLesen);
        Assert.False(d.FindeFeld("beschreibung")!.NurLesen);
        Assert.True(d.FindeFeld("jahressumme")!.NurLesen);
        Assert.True(d.FindeFeld("bedarfsart")!.NurLesen);

        Assert.Equal(12, KiDialoge.MONATSFELDER.Count);
        for (int m = 0; m < 12; m++)
        {
            KiDialogFeld monat = d.FindeFeld(KiDialoge.MONATSFELDER[m])!;
            Assert.NotNull(monat);
            Assert.Equal(KiParameterTyp.Zahl, monat.Typ);
            Assert.False(monat.NurLesen);
            Assert.Equal(Resource.ResourceManager.GetString("ALLG_MONAT_" + (m + 1)), monat.Anzeigename);
        }

        Assert.NotNull(d.FindeKnopf("speichern"));
    }

    // ---------------------------------------------------------------------
    //  Die ZAHLENREIHEN (Welle #458 Stufe 3b)
    // ---------------------------------------------------------------------

    /// <summary>
    /// <b>Die Zahlenfolgen der Masken sind Zahlenreihen</b> — je EIN setzbares Feld mit
    /// fester Länge und benannten Stellen; die Feldzahl der Maske steht dabei fest.
    /// </summary>
    [Theory]
    [InlineData(KiMaskennamen.TYPPROFIL, "wochenwerte", 168, 4)]
    [InlineData(KiMaskennamen.TYPSTAMM, "monatswerte", 12, 4)]
    [InlineData(KiMaskennamen.GEBAEUDETYP, "stundenwerte", 24, 4)]
    [InlineData(KiMaskennamen.KOSTENPROFIL, "monatswerte", 12, 5)]
    [InlineData(KiMaskennamen.KOSTENPROFIL, "wochenwerte", 168, 5)]
    [InlineData(KiMaskennamen.LEISTUNGSPREISREIHE, "monatssaetze", 12, 4)]
    [InlineData(KiMaskennamen.QUELLPROFIL, "monatswerte", 12, 5)]
    [InlineData(KiMaskennamen.TAGESGANG_EDITOR, "stunden", 24, 5)]
    [InlineData(KiMaskennamen.TAGESGANG_EDITOR, "wochenfaktoren", 7, 5)]
    [InlineData(KiMaskennamen.TWW_NUTZUNGSART_EDITOR, "monatsfaktoren", 12, 19)]
    public void Die_Zahlenfolgen_der_Masken_sind_Zahlenreihen(string maske, string feld, int laenge, int felder)
    {
        KiDialog d = KiDialoge.Katalog.Finde(maske)!;
        Assert.Equal(felder, d.Felder.Count);

        KiDialogFeld reihe = d.FindeFeld(feld)!;
        Assert.NotNull(reihe);
        Assert.True(reihe.IstReihe);
        Assert.Equal(KiParameterTyp.ZahlListe, reihe.Typ);
        Assert.Equal(laenge, reihe.Reihe!.Laenge);
        Assert.False(reihe.NurLesen);
        Assert.All(reihe.Reihe.Stellen, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>
    /// <b>Die Zählliste der Zahlenreihen:</b> genau diese elf Reihen an neun Masken — der
    /// Auslastungsgang des Zapfprofils (Stufe Z4) ist die achte, Stundenanteile und Wochenfaktoren
    /// des Tagesgang-Editors (Z4, Gruppe 2b) die neunte und zehnte, die Monatsfaktoren des Editors
    /// einer Nutzungsart (Z4, Gruppe 3) die elfte.
    /// Eine neue Reihe erzwingt einen Blick hierher — und in die Tests ihrer Maske.
    /// </summary>
    [Fact]
    public void Genau_elf_Zahlenreihen_stehen_im_Katalog()
    {
        string[] reihen = KiDialoge.Katalog.Alle
            .SelectMany(d => d.Felder.Where(f => f.IstReihe).Select(f => d.Maskenname + "." + f.Name))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[]
        {
            KiMaskennamen.TYPSTAMM + ".monatswerte",
            KiMaskennamen.TYPPROFIL + ".wochenwerte",
            KiMaskennamen.GEBAEUDETYP + ".stundenwerte",
            KiMaskennamen.KOSTENPROFIL + ".monatswerte",
            KiMaskennamen.KOSTENPROFIL + ".wochenwerte",
            KiMaskennamen.LEISTUNGSPREISREIHE + ".monatssaetze",
            KiMaskennamen.QUELLPROFIL + ".monatswerte",
            KiMaskennamen.ZAPFPROFIL + ".auslastungsgang",
            KiMaskennamen.TAGESGANG_EDITOR + ".stunden",
            KiMaskennamen.TAGESGANG_EDITOR + ".wochenfaktoren",
            KiMaskennamen.TWW_NUTZUNGSART_EDITOR + ".monatsfaktoren"
        }.OrderBy(s => s, StringComparer.Ordinal), reihen);
    }

    /// <summary>
    /// <b>Der Gebäudekatalog führt die Randbedingung des Hüll-Rasters und die Ferien als
    /// TABELLE</b> — Spalten mit dem Zeitraum als Zeilenkennzeichen und den Grenzen der
    /// Eingabefelder, keine Zahlenreihe; Kennwert und Größe der Rasterzeilen sind die
    /// Felder der U-Werte und Flächen.
    /// </summary>
    [Fact]
    public void Der_Gebaeudekatalog_fuehrt_Randbedingung_und_Ferientabelle()
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_KATALOG)!;

        // 54 bis Stufe 3b, dazu die Randbedingung und die vier Ferienspalten; mit der Welle 3
        // von AK1 die dreizehn Felder der Wärmeübergabe (Konzept Anlagenkopplung 9.1); mit E37
        // die acht Felder der Kühlübergabe; mit G4a das Baujahr neben der Baualtersklasse; mit E43
        // Beginn und Ende der Nachtabsenkung; mit G6a die vier Spalten der Zonenliste (nur lesbar);
        // mit E47 der Energiestandard (Wahl nach der Verwendung).
        Assert.Equal(88, d.Felder.Count);
        Assert.True(d.FindeFeld("energiestandard")!.IstWahl);
        Assert.DoesNotContain(d.Felder, f => f.IstReihe);
        Assert.True(d.FindeFeld("randbedingung")!.IstWahl);

        foreach (string zone in new[] { "zone_name", "zone_nutzflaeche", "zone_ht", "zone_bauteile" })
        {
            KiDialogFeld spalte = d.FindeFeld(zone)!;
            Assert.True(spalte.IstSpalte, zone);
            Assert.True(spalte.NurLesen, zone);
            Assert.Equal("Nummer", spalte.Zeilenkennzeichen);
        }

        foreach ((string name, double max) in new[]
                 {
                     ("ferien_beginn_tag", 31.0), ("ferien_beginn_monat", 12.0),
                     ("ferien_ende_tag", 31.0), ("ferien_ende_monat", 12.0)
                 })
        {
            KiDialogFeld spalte = d.FindeFeld(name)!;
            Assert.True(spalte.IstSpalte, name);
            Assert.Equal("Zeitraum", spalte.Zeilenkennzeichen);
            Assert.Equal(KiParameterTyp.Ganzzahl, spalte.Typ);
            Assert.Equal(1.0, spalte.Min);
            Assert.Equal(max, spalte.Max);
        }

        foreach (string bauteil in new[]
                 {
                     "u_aussenwand", "flaeche_aussenwand", "u_fenster", "u_dachflaeche", "dachflaeche",
                     "u_grundflaeche", "grundflaeche", "u_sonstiges", "sonstige_flaechen",
                     "wbvk_fenster_wand", "anschluss_fenster_wand", "wbvk_aussenwand_keller",
                     "anschluss_aussenwand_keller", "wbvk_wand_dach", "anschluss_wand_dach"
                 })
            Assert.True(d.KenntFeld(bauteil), bauteil);
    }

    /// <summary>
    /// <b>Die Gebäudeverwaltung führt die Felder des Katalogeditors</b> (Welle #465) — aus
    /// DERSELBEN Liste, an derselben Sichtklasse, mit denselben Namen, Pfaden, Typen und
    /// Grenzen; dazu die Satzwahl, ohne die Betriebsart des Editors, und der Name ist nur
    /// lesbar.
    /// </summary>
    [Fact]
    public void Die_Gebaeudeverwaltung_fuehrt_die_Felder_des_Katalogeditors()
    {
        KiDialog editor = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_KATALOG)!;
        KiDialog verwaltung = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_ADMIN)!;

        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBA_TITEL, verwaltung.Anzeigename);
        // + satz, − betriebsart, − die vier Spalten der Zonenliste (G6a: ein Katalogsatz trägt keine Zonen)
        Assert.Equal(editor.Felder.Count - 4, verwaltung.Felder.Count);
        Assert.DoesNotContain(verwaltung.Felder, f => f.Name.StartsWith("zone_", StringComparison.Ordinal));

        KiDialogFeld satz = verwaltung.FindeFeld("satz")!;
        Assert.True(satz.IstWahl);
        Assert.True(satz.Satzwahl);
        Assert.False(satz.NurLesen);
        Assert.False(verwaltung.KenntFeld("betriebsart"));
        Assert.True(verwaltung.FindeFeld("name")!.NurLesen);
        Assert.False(editor.FindeFeld("name")!.NurLesen);

        foreach (KiDialogFeld e in editor.Felder)
        {
            if (e.Name is "betriebsart" or "name" || e.Name.StartsWith("zone_", StringComparison.Ordinal)) continue;
            KiDialogFeld? v = verwaltung.FindeFeld(e.Name);
            Assert.True(v is not null, "Das Feld " + e.Name + " fehlt in der Verwaltung.");
            Assert.Equal(e.Eigenschaftspfad, v!.Eigenschaftspfad);
            Assert.Equal(e.Anzeigename, v.Anzeigename);
            Assert.Equal(e.Typ, v.Typ);
            Assert.Equal(e.Einheit, v.Einheit);
            Assert.Equal(e.NurLesen, v.NurLesen);
            Assert.Equal(e.Min, v.Min);
            Assert.Equal(e.Max, v.Max);
            Assert.Equal(e.Erlaeuterung, v.Erlaeuterung);
        }

        Assert.Equal(new[] { "speichern", "verwerfen", "beenden" },
                     verwaltung.Knoepfe.Select(k => k.Name).ToArray());
    }

    /// <summary>
    /// <b>Die Photovoltaik führt ELF Felder mehr als die drei der Startmaske</b> (Welle
    /// KI‑F1): die drei Modellfelder samt der Wechselrichterwahl und die SIEBEN Spalten
    /// der Strangliste.
    /// </summary>
    /// <remarks>
    /// Die Strangfelder sind SPALTEN (<c>ErzeugerZeile.Straenge[].Mppt</c>): Je
    /// vorhandener Strangzeile wird daraus ein gewöhnliches Feld, und das
    /// Zeilenkennzeichen ist der Bezeichner des Strangs. Ohne diesen Fall bliebe
    /// unbemerkt, wenn eines der sieben wieder zu einem flachen Feld würde.
    /// </remarks>
    [Fact]
    public void Die_Photovoltaikmaske_fuehrt_die_Modellfelder_und_sieben_Strangspalten()
    {
        KiDialog pv = KiDialoge.Katalog.Finde(KiMaskennamen.PHOTOVOLTAIK)!;

        foreach (string name in new[] { "modell_erweitert", "wr_wirkungsgrad",
                                        "systemverluste", "mit_wechselrichter" })
        {
            KiDialogFeld feld = pv.FindeFeld(name)!;
            Assert.NotNull(feld);
            Assert.False(feld.IstSpalte, name);
        }

        string[] spalten =
        {
            "strang", "strang_geraet", "strang_mppt", "strang_module_reihe",
            "strang_parallel", "strang_neigung", "strang_azimut"
        };

        foreach (string name in spalten)
        {
            KiDialogFeld feld = pv.FindeFeld(name)!;
            Assert.NotNull(feld);
            Assert.True(feld.IstSpalte, name);
            Assert.Equal("Straenge", feld.Sammlung);
            Assert.Equal("Bezeichner", feld.Zeilenkennzeichen);
        }

        // Die Anlagenwerte des Wechselrichters stehen in einer eigenen Ueberlagerung
        // und bleiben deshalb aus DIESER Maske draussen (Fachkonzept 11.6). Seit der
        // Welle KI-F7 haben sie einen EIGENEN Schluessel - der Anwender hat sie am
        // 21.09.2026 freigegeben, und zwar nach der Regel „Baustein in eigenem
        // Fenster bekommt einen Schluessel" (KI-D-Q7).
        KiDialog werte = KiDialoge.Katalog.Finde(KiMaskennamen.PV_ANLAGENWERTE)!;
        Assert.NotNull(werte);

        foreach (string weg in new[] { "wr_nennleistung", "wr_eta10", "wr_eta50", "wr_eta100" })
        {
            Assert.False(pv.KenntFeld(weg), weg);
            Assert.True(werte.KenntFeld(weg), weg);
        }

        // Die zwei AUSLEGUNGSTEMPERATUREN gehoeren dagegen zu Form_PV: Sie stehen im
        // Strangabschnitt derselben Maske, nur nicht an der Anlagenzeile.
        Assert.True(pv.KenntFeld("auslegung_kalt"));
        Assert.True(pv.KenntFeld("auslegung_heiss"));

        // Das Rechenmodell ist seit KI-F7 eine WAHL und kein Wahrheitswert (KI-D-Q6).
        Assert.Equal(KiParameterTyp.Wahl, pv.FindeFeld("modell_erweitert")!.Typ);
    }

    /// <summary>
    /// <b>Was der Heizkesseleditor am 15.09.2026 verloren hat — namentlich.</b>
    ///
    /// <para>Neun Felder: <c>investitionskosten</c>, <c>wartungskosten</c>,
    /// <c>raumbedarf</c>, <c>nutzungsdauer</c>, <c>co2</c>, <c>so2</c>, <c>nox</c>,
    /// <c>co</c>, <c>staub</c>. Der Zählfall oben sagt nur, dass es sechs SIND; dieser
    /// sagt, WELCHE — und dass keines der neun auf einem Umweg zurückkommt.</para>
    /// </summary>
    [Fact]
    public void Die_Heizkesselmaske_fuehrt_genau_ihre_sichtbaren_Felder()
    {
        KiDialog hk = KiDialoge.Katalog.Finde(KiMaskennamen.HEIZKESSEL)!;

        string[] erwartet =
        {
            "th_leistung", "wirkungsgrad_gas", "wirkungsgrad_oel",
            "bereitschaftsverlust", "vorlauf", "ruecklauf",
            // Welle KI-F1b: die uebrigen Eingabefelder derselben Maske.
            "name", "hersteller", "beschreibung", "energietraeger", "brennwert"
        };
        Assert.Equal(erwartet.OrderBy(x => x, StringComparer.Ordinal),
                     hk.Felder.Select(f => f.Name).OrderBy(x => x, StringComparer.Ordinal));

        foreach (string weg in new[]
                 {
                     "investitionskosten", "wartungskosten", "raumbedarf", "nutzungsdauer",
                     "co2", "so2", "nox", "co", "staub"
                 })
            Assert.False(hk.KenntFeld(weg), weg);
    }

    // =====================================================================
    //  Der zweite Wächter: steht das Feld auch WIRKLICH auf der Maske?
    // =====================================================================

    /// <summary>
    /// Maskenname → Razor-Datei(en), repo-relativ. Die EINE Zuordnungstabelle für die
    /// Markup-Probe (Auftrag vom 15.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>Sie ist bewusst getrennt von <see cref="Masken"/>: Dort steht das
    /// DATEN-OBJEKT (was der Dialog anmeldet), hier die DATEI (was der Anwender
    /// sieht). Bei der Simulations- und der Stromspeicher-Ansicht fallen beide
    /// auseinander — siehe <see cref="OhneMarkupprobe"/>.</para>
    ///
    /// <para><b>Eine Maske darf aus MEHREREN Dateien bestehen</b> (mit <c>;</c>
    /// getrennt): Wandert das Feldraster eines Dialogs in einen eigenen Baustein, weil
    /// ein zweiter Wirt dieselben Felder zeigt, steht das Feld weiterhin vor dem
    /// Anwender — nur eben in der Kinddatei. Für <c>Form_WP</c> ist das seit #297 so:
    /// <c>WaermepumpeStammFelder</c> trägt das Raster, und der Anlagendialog der
    /// Wärmepumpe bettet es genauso ein wie die Stammdatenpflege.</para>
    /// </remarks>
    public static TheoryData<string, string> Markupdateien() => new()
    {
        { KiMaskennamen.HEIZKESSEL,       "EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor" },

        // Welle KI-F7: Die Ueberlagerung „Anlagenwerte" bindet ihren Arbeitsstand
        // NAMENTLICH im Markup - vier Zahlenfelder, vier Eigenschaften. Sie traegt
        // deshalb die Markup-Probe, obwohl ihr Daten-Objekt eine Sichtklasse ist:
        // Diese Sichtklasse IST der Stand des Fensters und kein Umweg um ihn herum.
        // Form_PV selbst steht seit derselben Welle in OhneMarkupprobe.
        { KiMaskennamen.PV_ANLAGENWERTE,  "EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor" },

        { KiMaskennamen.PUFFERSPEICHER,   "EPOS.UI/Dialoge/Erzeuger/PufferSpKatalogDialog.razor" },
        { KiMaskennamen.WAERMEPUMPE,      "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammDialog.razor;" +
                                          "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeStammFelder.razor" },

        // Welle KI-F1: die Erzeugermasken des PROJEKTS.
        { KiMaskennamen.HEIZKESSEL_PROJEKT,     "EPOS.UI/Dialoge/Erzeuger/HeizkesselDialog.razor" },
        { KiMaskennamen.BHKW_PROJEKT,           "EPOS.UI/Dialoge/Erzeuger/BhkwDialog.razor" },
        { KiMaskennamen.PUFFERSPEICHER_PROJEKT, "EPOS.UI/Dialoge/Erzeuger/PufferspeicherDialog.razor" },
        { KiMaskennamen.STROMSPEICHER_PROJEKT,  "EPOS.UI/Dialoge/Erzeuger/StromspeicherDialog.razor" },
        { KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT,
          "EPOS.UI/Dialoge/Solarthermie/SolarkollektorenDialog.razor" },

        // Form_WP_Anlage steht seit der Welle #458 in OhneMarkupprobe: Der
        // Extrapolationsschalter gehoert dem PROJEKT und haengt an keiner Bindung des
        // Feldsatzes (Muster Form_PV, KI-F7).

        // Welle KI-F3: der Kopfsatz eines Bedarfskatalogs - die einzige Maske der
        // Welle, die ihr Daten-Objekt anmeldet und damit die Markup-Probe traegt.
        { KiMaskennamen.TYPSTAMM, "EPOS.UI/Dialoge/Bedarf/TypStammDialog.razor" },

        // Die gewaehlte Zuordnung der externen Waermebedarfsganglinien: Kanal und
        // Bezeichner stehen beide im Markup.
        { KiMaskennamen.WAERMEBEDARF_EXTERN,
          "EPOS.UI/Dialoge/Bedarf/WaermebedarfExternDialog.razor" },

        // Welle KI-F5: die zwei KATALOGEDITOREN. Sie binden ihr Daten-Objekt und
        // zeichnen jedes Feld selbst - eine Datei je Maske.
        { KiMaskennamen.BHKW, "EPOS.UI/Dialoge/Erzeuger/BhkwKatalogDialog.razor" },
        { KiMaskennamen.SOLARKOLLEKTOR,
          "EPOS.UI/Dialoge/Solarthermie/SolarkollektorKatalogDialog.razor" }
    };

    /// <summary>
    /// Die Masken OHNE Markup-Probe — mit Grund, je eine Zeile.
    /// </summary>
    /// <remarks>
    /// <b>Beide binden über eine SICHTKLASSE.</b> <c>StromspeicherKiSicht</c> und
    /// <c>SimulationKiSicht</c> sind flache Sichtmodelle, die die Kette zum lebenden
    /// Stand EINMAL an einer benannten Stelle auflösen
    /// (<c>KiEigenschaftspfad</c>-Kommentar: „Wo eine Maske echte Tiefe braucht,
    /// bekommt sie ein flaches SICHTMODELL"). Ihre Eigenschaftsnamen
    /// (<c>KapazitaetGesamtKWh</c>, <c>WaermedeckungProzent</c>) stehen deshalb NICHT
    /// im Markup — die Ansicht zeigt dieselben Zahlen aus ihrem eigenen Stand. Für
    /// beide hält die Sichtklasse selbst den Zeugen (<c>KiDialogkatalogTests</c>
    /// weiter unten, <c>KiSimulationMaskeTests</c>): Dort wird gerechnet, ob die
    /// Sicht die Werte der Ansicht trägt, und das ist die schärfere Probe.
    /// </remarks>
    /// <remarks>
    /// <b>Die Masken der Simulationskonfiguration kommen mit der Welle KI‑F2 dazu —
    /// aus demselben Grund und mit demselben Ersatz.</b> Ihre Dialoge führen ihren
    /// Arbeitsstand nicht als veränderliches DTO, sondern in den Eingabefeldern der
    /// Maske; was sie hereinbekommen und herausgeben, sind unveränderliche Records
    /// (<c>init</c>-Eigenschaften, neu erzeugt per <c>with</c>). Ein daran angemeldeter
    /// Katalog zeigte den Stand von vorhin und setzte ins Leere. Jede dieser Masken
    /// bindet deshalb über eine Sichtklasse auf die LEBENDEN Felder — und jede hält
    /// ihren Zeugen in der Testklasse ihres Dialogs: Dort steht die Maske gezeichnet
    /// an der Brücke, und ein Feld wird gelesen UND gesetzt. Das ist die schärfere
    /// Probe, denn sie misst den ganzen Weg statt einer Zeichenkette im Markup.
    /// </remarks>
    private static readonly Dictionary<string, string> OhneMarkupprobe = new()
    {
        [KiMaskennamen.STROMSPEICHER_AUSLEGUNG] =
            "bindet über die Sichtklasse StromspeicherKiSicht, nicht über das Markup",
        [KiMaskennamen.SIMULATION] =
            "bindet über die Sichtklasse SimulationKiSicht, nicht über das Markup",
        [KiMaskennamen.PUFFERSPEICHER_VERWALTUNG] =
            "bindet über die Sichtklasse PufferSpProjektKiSicht auf die Eingabefelder " +
            "der Maske; Zeuge ist PufferSpProjektDialogTests",
        [KiMaskennamen.QUELLE_ERDREICH] =
            "bindet über die Sichtklasse QuelleErdreichKiSicht auf die Eingabefelder " +
            "der Maske; Zeuge ist QuelleErdreichDialogTests",
        [KiMaskennamen.QUELLE_PUFFERSPEICHER] =
            "bindet über die Sichtklasse QuellePufferspeicherKiSicht auf die " +
            "Eingabefelder der Maske; Zeuge ist QuellePufferspeicherDialogTests",
        [KiMaskennamen.QUELLPROFIL] =
            "bindet über die Sichtklasse QuellprofilKiSicht auf die Kopffelder der " +
            "Maske; Zeuge ist QuellprofilDialogTests",
        [KiMaskennamen.WAERMESENKE] =
            "bindet über die Sichtklasse WaermesenkeKiSicht auf die Bedienelemente " +
            "der gewählten Zeile; Zeuge ist WaermesenkeDialogTests",
        [KiMaskennamen.KOMPONENTENKONFIGURATION] =
            "bindet über die Sichtklasse KomponentenKonfigurationKiSicht auf ZWEI " +
            "Arbeitskopien; Zeuge ist KomponentenKonfigurationDialogTests",
        [KiMaskennamen.GEBAEUDE] =
            "bindet über die Sichtklasse GebaeudeKiSicht auf Suche und Trichter der " +
            "Katalogliste und den Detailblock der Maske; Zeuge ist GebaeudeDialogTests",
        [KiMaskennamen.GEBAEUDE_WOHNFLAECHE] =
            "bindet über die Sichtklasse GebaeudeWohnflaecheKiSicht auf die lebenden " +
            "Eingabefelder; Zeuge ist GebaeudeWohnflaecheDialogTests",
        [KiMaskennamen.GEBAEUDE_KATALOG] =
            "bindet über die Sichtklasse GebaeudeKatalogKiSicht auf BEIDE Reiterblätter; " +
            "Zeuge ist GebaeudeKatalogDialogTests",
        [KiMaskennamen.GEBAEUDE_ADMIN] =
            "bindet über dieselbe Sichtklasse GebaeudeKatalogKiSicht auf den Arbeitsstand des " +
            "Stammblatts (Kenndaten und GebaeudeStammblattFelder) samt Satzwahl; Zeuge ist " +
            "GebaeudeAdminDialogTests",
        [KiMaskennamen.GEBAEUDE_BEDARF] =
            "bindet über die Sichtklasse GebaeudeBedarfKiSicht auf den eingefrorenen " +
            "Satz und die zwei Bedienelemente; Zeuge ist GebaeudeBedarfDialogTests",
        [KiMaskennamen.GEBAEUDETYP] =
            "bindet über die Sichtklasse GebaeudetypKiSicht auf die Listenwahl der " +
            "Maske; Zeuge ist GebaeudetypDialogTests",
        [KiMaskennamen.BAUSTOFF_KATALOG] =
            "bindet über die Sichtklasse BaustoffKatalogKiSicht auf die Satzwahl und den " +
            "Arbeitsstand des Stammblatts; Zeuge ist BaustoffKatalogDialogTests",
        [KiMaskennamen.BAUTEILAUFBAU] =
            "bindet über die Sichtklasse BauteilaufbauKiSicht auf die Satzwahl, den Kopf und das " +
            "Schichtenraster des Arbeitsstands; Zeuge ist BauteilaufbauDialogTests",
        [KiMaskennamen.ZONE] =
            "bindet über die Sichtklasse ZonenKiSicht auf Bezeichnung, Nutzfläche, die Werte der Zone " +
            "und das Bauteilraster zum Lesen; Zeuge ist ZonenDialogTests",
        [KiMaskennamen.BAUTEIL] =
            "bindet über die Sichtklasse BauteilKiSicht auf die Listenplätze von Art, " +
            "Randbedingung, Nachbarzone, Zuordnung und Aufbau und den Arbeitsstand des Bauteils; " +
            "Zeuge ist BauteilDialogTests",
        [KiMaskennamen.LUFTAUSTAUSCH] =
            "bindet über die Sichtklasse LuftaustauschKiSicht auf das Raster der Luftströme " +
            "(Zonen zum Lesen, Volumenstrom setzbar); Zeuge ist LuftaustauschDialogTests",
        [KiMaskennamen.GEBAEUDE_EXPORT] =
            "bindet über die Sichtklasse GebaeudeExportKiSicht auf die Postleitzahl (setzbar) und die " +
            "Bestätigung der Meldungen (nur zu lesen); Zeuge ist GebaeudeExportDialogTests",
        [KiMaskennamen.TYPPROFIL] =
            "bindet über die Sichtklasse TypProfilKiSicht auf die Listenwahl der " +
            "Maske; Zeuge ist TypProfilDialogTests",
        [KiMaskennamen.BEDARFSPROFILE] =
            "bindet über die Sichtklasse BedarfsProfileKiSicht auf Infoblock, " +
            "Verbrauchseingabe und die Optionsgruppe „Rechenweg Brauchwasser“; Zeuge ist " +
            "BedarfsProfileDialogTests",
        [KiMaskennamen.ZAPFPROFIL] =
            "bindet über die Sichtklasse ZapfprofilKiSicht auf den Arbeitsstand der " +
            "Überlagerung: Stufe, Zonenwahl, die Zonen als Spalten (Zeilen mit den Wegen der " +
            "Eingabefelder), Ansicht und Stochastik; Zeuge ist ZapfprofilDialogTests",
        [KiMaskennamen.ZAPFPROFIL_AUSLEGUNG] =
            "bindet über die Sichtklasse ZapfprofilAuslegungKiSicht: Bedarfstag als eine Wahl " +
            "aus Quelle und Katalogtag, drei Aufzählungen, jede Eingabe rechnet neu; Zeuge ist " +
            "ZapfprofilAuslegungDialogTests",
        [KiMaskennamen.BEDARFSTAG_KONSTRUKTOR] =
            "bindet über die Sichtklasse BedarfstagKonstruktorKiSicht: der Name und die Zeilen " +
            "als Spalten, die Zapfregel als Platz der Regelwahl, Anzahl bzw. Volumen und " +
            "Temperatur nur, wo die Zeile sie bedienbar zeigt; Zeuge ist ZapfprofilAuslegungDialogTests",
        [KiMaskennamen.TAGESGANG_EDITOR] =
            "bindet über die Sichtklasse TagesgangEditorKiSicht: Tagtyp als Wahl, die 24 Stundenanteile " +
            "des gezeigten Tagtyps und die Wochenfaktoren als Zahlenreihen, Vorlage und Katalogversion " +
            "nur, wo die Maske sie bedienbar zeigt; Zeuge ist TagesgangEditorTests",
        [KiMaskennamen.ZAPFKATEGORIEN] =
            "bindet über die Sichtklasse ZapfkategorienEditorKiSicht: die Kategorien als Spalten des " +
            "Rasters (Grenzen der Felder, nur bedienbar gesetzt) und die Katalogversion der Kopie; " +
            "Zeuge ist ZapfkategorienEditorTests",
        [KiMaskennamen.BRAUCHWASSER_NUTZUNGSARTEN] =
            "bindet über die Sichtklasse TwwNutzungsartAdminKiSicht: die Wahl der Zeile (Satzwahl) und " +
            "drei Anzeigen des lesenden Stammblatts; Zeuge ist TwwNutzungsartAdminDialogTests",
        [KiMaskennamen.TWW_NUTZUNGSART_EDITOR] =
            "bindet über die Sichtklasse TwwNutzungsartEditorKiSicht: die Zahlenfelder über ihren " +
            "Namen, vier Wahlen und die Monatsfaktoren als Zahlenreihe (Grenzen der Felder); Zeuge " +
            "ist TwwNutzungsartAdminDialogTests",
        [KiMaskennamen.BRAUCHWASSER_MESSREIHEN] =
            "bindet über die Sichtklasse TwwMessreihenKiSicht: zwölf Anzeigen der eingespielten "
            + "Reihen und des Prüfberichts, kein Einstellwert — die Dateiwahl ist ein Dateidialog, "
            + "Einspielen und Löschen bleiben Klicks; Zeuge ist TwwMessreihenDialogTests",
        [KiMaskennamen.BRAUCHWASSER_TYPTAGE] =
            "bindet über die Sichtklasse TwwTyptagImportKiSicht: acht Anzeigen des eingespielten " +
            "Stands und des Prüfberichts, kein Einstellwert — die Paketwahl ist ein Dateidialog, " +
            "Einspielen und Löschen bleiben Klicks; Zeuge ist TwwTyptagImportDialogTests",
        [KiMaskennamen.PROZESSWAERME_ADMIN] =
            "bindet über die Sichtklasse BedarfAdminKiSicht auf Listenwahl und den " +
            "Arbeitsstand des Stammblatts; Zeuge ist BedarfAdminDialogTests",
        [KiMaskennamen.STROMVERBRAUCHER_ADMIN] =
            "bindet über die Sichtklasse BedarfAdminKiSicht auf Listenwahl und den " +
            "Arbeitsstand des Stammblatts; Zeuge ist BedarfAdminDialogTests",
        [KiMaskennamen.BRAUCHWASSER_ADMIN] =
            "bindet über die Sichtklasse BedarfAdminKiSicht auf Listenwahl und den " +
            "Arbeitsstand des Stammblatts; Zeuge ist BedarfAdminDialogTests",
        [KiMaskennamen.BEDARF_ERGEBNIS] =
            "bindet über die Sichtklasse BedarfErgebnisKiSicht auf die vier Schalter " +
            "der Anzeige; Zeuge ist BedarfErgebnisDialogTests",
        [KiMaskennamen.SOLARGANGLINIE] =
            "bindet über die Sichtklasse SolarganglinieKiSicht auf Katalogwahl und " +
            "Detailblock; Zeuge ist SolarganglinieDialogTests",
        [KiMaskennamen.KLIMADATEN] =
            "bindet über die Sichtklasse KlimadatenKiSicht auf die lebenden Felder " +
            "von Quelle und Standort; Zeuge ist KlimadatenDialogTests",

        // Welle KI-F4
        [KiMaskennamen.ENERGIETRAEGER] =
            "bindet über die Sichtklasse EnergietraegerKiSicht auf Listenkopf, " +
            "Trägerkarte und beide Preisblöcke; Zeuge ist EnergietraegerDialogTests",
        [KiMaskennamen.ENERGIETRAEGER_VARIANTE] =
            "bindet über die Sichtklasse EnergietraegerVarianteKiSicht auf die zwei " +
            "lebenden Felder; Zeuge ist EnergietraegerVarianteDialogTests",
        [KiMaskennamen.LEISTUNGSPREISREIHE] =
            "bindet über die Sichtklasse LeistungspreisReiheKiSicht auf das Jahr der " +
            "Reihe; Zeuge ist LeistungspreisReiheDialogTests",
        [KiMaskennamen.KOSTENPROFIL] =
            "bindet über die Sichtklasse KostenprofilKiSicht auf Bezeichner und " +
            "Wochentag; Zeuge ist KostenprofilDialogTests",
        [KiMaskennamen.KOSTENFAKTOR_KATALOG] =
            "bindet über die Sichtklasse KostenfaktorKatalogKiSicht auf Neuzeile und " +
            "Rastermarkierung; Zeuge ist KostenfaktorKatalogDialogTests",
        [KiMaskennamen.EMISSIONSKATALOG] =
            "bindet über die Sichtklasse EmissionskatalogKiSicht auf Methode, beide " +
            "Markierungen und die zwei Editoren; Zeuge ist EmissionskatalogDialogTests",
        [KiMaskennamen.NUTZUNGSDAUER] =
            "bindet über die Sichtklasse NutzungsdauerKiSicht auf Kopffelder und die " +
            "lebende Zeilenliste; Zeuge ist NutzungsdauerDialogTests",
        [KiMaskennamen.VORLAGENPOSITION] =
            "bindet über die Sichtklasse VorlagenPositionKiSicht auf die neun " +
            "lebenden Felder; Zeuge ist VorlagenPositionDialogTests",
        [KiMaskennamen.CASE_EINGABE] =
            "bindet über die Sichtklasse CaseEingabeKiSicht auf die sieben lebenden " +
            "Felder und — ETAPPE E9b — drei Auskunftsfelder des Szenariopaars; Zeugen " +
            "sind CaseEingabeDialogTests und CaseEingabeSzenariopaarTests",
        [KiMaskennamen.WIRTSCHAFTLICHKEIT_PARAMETER] =
            "bindet über die Sichtklasse WirtschaftlichkeitParameterKiSicht auf den " +
            "Parametersatz UND beide Szenariosätze; Zeuge ist " +
            "WirtschaftlichkeitParameterDialogTests",
        [KiMaskennamen.BHKW_WIRTSCHAFTLICHKEIT] =
            "bindet über die Sichtklasse BhkwWirtschaftlichkeitKiSicht auf die zwei " +
            "Arbeitsstände; Zeuge ist BhkwWirtschaftlichkeitDialogTests",
        [KiMaskennamen.TARIFSTRUKTUR] =
            "bindet über die Sichtklasse TarifstrukturKiSicht auf die lebenden " +
            "Eingabefelder; Zeuge ist TarifstrukturDialogTests",
        [KiMaskennamen.PV_VERGUETUNG] =
            "bindet über die Sichtklasse PhotovoltaikVerguetungKiSicht auf das " +
            "Vergütungsmodell, gesetzt über die Wege der Maske; Zeuge ist " +
            "PhotovoltaikVerguetungDialogTests",
        [KiMaskennamen.GESETZESKATALOG] =
            "bindet über die Sichtklasse GesetzeskatalogKiSicht auf Klassenwahl und " +
            "Listenmarkierung; Zeuge ist GesetzeskatalogDialogTests",
        [KiMaskennamen.GESETZESKATALOG_ZEILE] =
            "bindet über die Sichtklasse GesetzeskatalogZeileKiSicht auf die sieben " +
            "lebenden Felder; Zeuge ist GesetzeskatalogDialogTests",
        [KiMaskennamen.KOSTENSEITE] =
            "bindet über die Sichtklasse KostenSeiteKiSicht auf die Zeilenmarkierung " +
            "der Seite; Zeuge ist KostenSeiteTests",
        [KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE] =
            "bindet über die Sichtklasse WirtschaftlichkeitSeiteKiSicht auf die fünf " +
            "Wahlwege, die Liste der nicht monetarisierbaren Wirkungen (WirkungenListe, " +
            "ETAPPE E17), die zwei Anzeigewahlen der Zahlungsreihen und " +
            "Zeitraum und Haken des Verlaufs (KapitalwertVerlaufAbschnitt); Zeugen " +
            "sind WirtschaftlichkeitSeiteTests, WirkungenListeTests und KapitalwertVerlaufAbschnittTests",

        // Welle KI-F5: die drei Ausprägungen des Modulkatalogs. Ihre Felder stehen
        // NICHT im Markup — es zeichnet eine Schleife über den Feldsatz, den das
        // ModulKatalogProfil zur Laufzeit bestimmt.
        [KiMaskennamen.PV_MODULKATALOG] =
            "bindet über die Sichtklasse ModulKatalogKiSicht auf den Feldsatz des " +
            "Profils; das Markup zeichnet eine Schleife, keine benannten Bindungen. " +
            "Zeuge ist ModulKatalogDialogTests",
        [KiMaskennamen.STROMSPEICHER_KATALOG] =
            "bindet über die Sichtklasse ModulKatalogKiSicht auf den Feldsatz des " +
            "Profils; das Markup zeichnet eine Schleife, keine benannten Bindungen. " +
            "Zeuge ist ModulKatalogDialogTests",
        [KiMaskennamen.WECHSELRICHTER_KATALOG] =
            "bindet über die Sichtklasse ModulKatalogKiSicht auf den Feldsatz des " +
            "Profils; das Markup zeichnet eine Schleife, keine benannten Bindungen. " +
            "Zeuge ist ModulKatalogDialogTests",

        // Welle #456: die vier Verwaltungen der Erzeugerkataloge. Ihr Stammblatt
        // zeichnet der Baustein Katalogfelder in einer Schleife über den Feldsatz
        // des KatalogBrowserProfil - benannte Bindungen gibt es nicht.
        [KiMaskennamen.HEIZKESSEL_ADMIN] =
            "bindet über die Sichtklasse KatalogBrowserKiSicht als Feldtafel auf den " +
            "Feldsatz des Profils; Zeugen sind KatalogBrowserDialogTests und der " +
            "Profilwächter",
        [KiMaskennamen.BHKW_ADMIN] =
            "bindet über die Sichtklasse KatalogBrowserKiSicht als Feldtafel auf den " +
            "Feldsatz des Profils; Zeugen sind KatalogBrowserDialogTests und der " +
            "Profilwächter",
        [KiMaskennamen.SOLARKOLLEKTOREN_ADMIN] =
            "bindet über die Sichtklasse KatalogBrowserKiSicht als Feldtafel auf den " +
            "Feldsatz des Profils; Zeugen sind KatalogBrowserDialogTests und der " +
            "Profilwächter",
        [KiMaskennamen.PUFFERSPEICHER_ADMIN] =
            "bindet über die Sichtklasse KatalogBrowserKiSicht als Feldtafel auf den " +
            "Feldsatz des Profils; Zeugen sind KatalogBrowserDialogTests und der " +
            "Profilwächter",

        // Welle KI-F6, Schritt 1: die drei Strommasken
        [KiMaskennamen.PEAK_SHAVING] =
            "bindet über die Sichtklasse PeakShavingKiSicht auf die privaten Felder " +
            "der Maske; PeakShavingEingaben entsteht erst im Rechenweg. Zeuge ist " +
            "PeakShavingDialogTests",
        [KiMaskennamen.SPEICHER_ZEITREIHEN] =
            "bindet über die Sichtklasse SpeicherZeitreihenKiSicht auf den lebenden " +
            "Optionssatz und den Zeitart-Schalter der Maske; Zeuge ist " +
            "SpeicherZeitreihenDialogTests",
        [KiMaskennamen.STROMGANGLINIE_ADMIN] =
            "bindet über die Sichtklasse StromganglinieAdminKiSicht auf Rasterwahl " +
            "und Listenmarkierung; Zeuge ist StromganglinieAdminDialogTests",

        // Welle KI-F6, Schritt 2
        [KiMaskennamen.BERICHTE_UEBERSICHT] =
            "bindet über die Sichtklasse UebersichtSeiteKiSicht auf die vier Wahlwege " +
            "der Seite; sie schreibt nicht in ihren Stand, sondern meldet an die " +
            "Hülle. Zeuge ist UebersichtSeiteTests",
        [KiMaskennamen.BERICHTSEITE] =
            "bindet über die Sichtklasse BerichtSeiteKiSicht auf Ausgabeform, " +
            "Zielordner, die Vorlagenwahl der Gruppe „Vorlage“ (Ids der Hülle) und die zwei " +
            "Aufstellungen; Zeugen sind BerichtSeiteTests und BerichtSeiteVorlagenTests",
        [KiMaskennamen.PROJEKT_KOPIE] =
            "bindet über die Sichtklasse ProjektKopieKiSicht auf die sieben privaten " +
            "Felder der Maske; Zeuge ist ProjektKopieDialogTests",
        [KiMaskennamen.PROJEKT_VARIANTE] =
            "bindet über die Sichtklasse ProjektVarianteKiSicht auf Haken, Listenwahl " +
            "und Bezeichner; Zeuge ist ProjektVarianteDialogTests",

        // Welle KI-F7 (Anwenderentscheid 21.09.2026): Form_PV gibt die Markup-Probe
        // auf, weil zwei seiner Felder gar nicht an der Anlagenzeile haengen.
        [KiMaskennamen.PHOTOVOLTAIK] =
            "bindet über die Sichtklasse PhotovoltaikKiSicht: dreizehn Felder reicht " +
            "sie an die gewählte ErzeugerZeile durch, die zwei Auslegungstemperaturen " +
            "gehören dem PROJEKT (Tab_Einstellungen) und stehen in den lebenden " +
            "Feldern des Strangbausteins; Zeuge ist PhotovoltaikDialogTests",
        [KiMaskennamen.WAERMEPUMPE_ANLAGE] =
            "bindet über die Sichtklasse WaermepumpeAnlageKiSicht: den Feldsatz " +
            "WaermepumpeAnlageDaten reicht sie unverändert durch, der Schalter " +
            "„Extrapolation der WP-Kennlinie erlauben“ gehört dem PROJEKT und schreibt " +
            "über ExtrapolationSchreiben; Zeuge ist WaermepumpeAnlageDialogTests",
        [KiMaskennamen.KOSTENVERWALTUNG] =
            "bindet über die Sichtklasse KostenKomponenteKiSicht: den Arbeitsstand " +
            "samt Raster reicht sie unverändert durch, dazu die Komponentenwahl aus " +
            "einem privaten Feld des Dialogs und die PV-Wahl samt PV-Projekt aus dem " +
            "Baustein ErtragBonus; Zeuge ist KostenKomponenteDialogTests",

        // Welle #458, Stufe 2
        [KiMaskennamen.KENNLINIEN] =
            "bindet über die Sichtklasse KennlinienKiSicht auf den Arbeitsstand der " +
            "Überlagerung: Stufenwahl, die Zeilen der gewählten Stufe als Spalten und " +
            "die Felder der neuen Stützstelle; Zeuge ist KennlinienEditorDialogTests",
        [KiMaskennamen.PROJEKTKOPF] =
            "bindet über die Sichtklasse ProjektKopfKiSicht: drei Texte reicht sie an " +
            "ProjektKopfDaten durch, die Klimaregion geht über den Weg der Seite (Id und " +
            "Name zugleich), der Name steht im Bearbeiten-Modus fest; Zeuge ist " +
            "ProjektKopfSeiteTests",
        [KiMaskennamen.STARTSEITE] =
            "bindet über die Sichtklasse StartseiteKiSicht auf die privaten Felder der " +
            "Seite (Klimaregion des Kopfbandes, Weiche der Solarthermiekachel); Zeuge ist " +
            "StartseiteTests",
        [KiMaskennamen.EINSTELLUNGEN] =
            "bindet über die Sichtklasse EinstellungenKiSicht: der Wertesatz führt Felder " +
            "statt Eigenschaften, die Diagrammfarben stehen als Feldtafel je Farbrolle; " +
            "Zeugen sind EinstellungenDialogTests und der Rollenwächter"
    };

    /// <summary>
    /// <b>Ein Katalogfeld, das auf der Maske nicht steht, ist eine stille Setzung.</b>
    ///
    /// <para><b>Der Befund (15.09.2026).</b> Der Heizkesseleditor verlor die Gruppen
    /// „Kosten", „Emissionen nach BEHG-V" und „Emissionsfaktoren"; der Dialogkatalog
    /// führte die neun Felder weiter. Der bisherige Wächter
    /// (<see cref="Jeder_Eigenschaftsname_des_Katalogs_gibt_es_am_Daten_Objekt"/>) blieb
    /// dabei GRÜN: Die Eigenschaften gibt es am DTO ja noch — die Hülle liest und
    /// schreibt die Spalten weiterhin. Grün heißt hier also nur „auflösbar", nicht
    /// „sichtbar". Der Assistent hätte angeboten, eine Zahl zu setzen, die der Anwender
    /// in der offenen Maske nirgends nachlesen kann.</para>
    ///
    /// <para><b>Die Regel.</b> Der Eigenschaftsname — bei einer Spalte
    /// (<c>Stand.Zeilen[].Nutzungsdauer</c>) der Teil NACH dem <c>[]</c> — muss im
    /// Markup MIT FÜHRENDEM PUNKT vorkommen: <c>Daten.Ptherm</c>,
    /// <c>_projektZeile.Neigung</c>, <c>zeile.Nutzungsdauer</c>. Der Punkt ist das
    /// Entscheidende: Er trennt eine BINDUNG von einer gleichlautenden Zeichenkette —
    /// <c>"Investitionskosten…"</c> als Knopftext oder <c>LabelInvestKurz</c> als
    /// Parametername treffen die Regel nicht. Kommentarzeilen zählen nicht mit
    /// (dieselbe Ausnahme wie bei den zwei <c>git grep</c>-Wächtern des Kerns);
    /// sonst hielte ein „HIER STAND Daten.CO2" die entfernte Deklaration am Leben.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Markupdateien))]
    public void Jeder_Feldpfad_steht_im_Markup_seiner_Maske(string maske, string datei)
    {
        string markup = MarkupOhneKommentare(datei);

        KiDialog dialog = KiDialoge.Katalog.Finde(maske)!;
        Assert.NotNull(dialog);
        Assert.NotEmpty(dialog.Felder);

        var fehlt = new List<string>();
        foreach (KiDialogFeld f in dialog.Felder)
        {
            string eigenschaft = KiEigenschaftspfad.Eigenschaft(f.Eigenschaftspfad);
            Assert.NotEqual("", eigenschaft);

            // Ein Feld der FELDTAFEL hat keine Bindung mit Namen — es steht als Daten
            // eines Profils in einer Schleife (Katalogfelder). Seinen Bestand hält der
            // Profilwächter (Die_Projektmasken_fuehren_Alle_Daten_genau_nach_ihrem_Profil).
            if (IstTafelfeld(maske, f)) continue;

            if (!StehtImMarkup(markup, eigenschaft))
                fehlt.Add(f.Name + " → " + f.Eigenschaftspfad);
        }

        Assert.True(fehlt.Count == 0,
                    "Diese Felder der Maske '" + maske + "' stehen in " + datei +
                    " an keiner Bindung — sie sind für den Anwender unsichtbar und " +
                    "gehören damit nicht in den Dialogkatalog: " + string.Join(", ", fehlt));
    }

    /// <summary>
    /// Jede Maske des Katalogs steht in GENAU EINER der beiden Listen — entweder mit
    /// Razor-Datei oder mit begründeter Ausnahme. Ohne diesen Fall verschwände eine
    /// neue Maske stillschweigend aus der Probe, indem niemand sie einträgt.
    /// </summary>
    [Fact]
    public void Jede_Katalogmaske_hat_entweder_ein_Markup_oder_einen_Ausnahmegrund()
    {
        var mitDatei = new HashSet<string>();
        foreach (object[] zeile in Markupdateien()) mitDatei.Add((string)zeile[0]);

        foreach (KiDialog d in KiDialoge.Katalog.Alle)
        {
            bool markup = mitDatei.Contains(d.Maskenname);
            bool ausnahme = OhneMarkupprobe.ContainsKey(d.Maskenname);

            Assert.True(markup ^ ausnahme,
                        "Die Maske '" + d.Maskenname + "' steht in keiner oder in beiden " +
                        "Listen der Markup-Probe.");
        }

        // Und die Ausnahme bleibt an ihre Begründung gebunden: Eine Maske ohne
        // Markup-Probe MUSS über eine Sichtklasse binden — daran hängt der Grund.
        foreach (object[] zeile in Masken())
        {
            if (!OhneMarkupprobe.ContainsKey((string)zeile[0])) continue;

            Type daten = (Type)zeile[1];
            Assert.EndsWith("KiSicht", daten.Name, StringComparison.Ordinal);
            Assert.NotEmpty(OhneMarkupprobe[(string)zeile[0]]);
        }
    }

    /// <summary>
    /// Die GEGENPROBE: Die Regel darf nicht alles durchlassen. Ein erfundener
    /// Eigenschaftsname und eine Zeichenkette ohne führenden Punkt fallen durch.
    /// </summary>
    [Fact]
    public void Die_Markupprobe_laesst_nicht_alles_durch()
    {
        string markup = MarkupOhneKommentare(
            "EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor");

        // Es gibt sie: die sechs Felder, die die Maske zeigt.
        Assert.True(StehtImMarkup(markup, "Ptherm"));
        Assert.True(StehtImMarkup(markup, "Ruecklauf"));

        // Es gibt sie nicht: die neun, die am 15.09.2026 gefallen sind. Genau darauf
        // hätte der bisherige Wächter nicht angeschlagen.
        foreach (string weg in new[]
                 {
                     "Investitionskosten", "Wartungskosten", "Raumbedarf", "Nutzungsdauer",
                     "CO2", "SO2", "NOx", "CO", "Staub"
                 })
            Assert.False(StehtImMarkup(markup, weg), weg);

        // Und ein Name, den es nie gab.
        Assert.False(StehtImMarkup(markup, "GibtEsNichtImMarkup"));
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Markupprobe_findet_ihre_Dateien_und_prueft_genug_Felder()
    {
        int felder = 0;

        foreach (object[] zeile in Markupdateien())
        {
            foreach (string datei in Dateien((string)zeile[1]))
                Assert.True(File.Exists(Path.Combine(Wurzel(),
                                                     datei.Replace('/', Path.DirectorySeparatorChar))),
                            datei);
            felder += KiDialoge.Katalog.Finde((string)zeile[0])!.Felder.Count;
        }

        // 6 (Heizkesseleditor) + 14 (PV) + 1 (Puffereditor) + 1 (WP-Verwaltung) +
        // 7 (Kostenverwaltung) + 3 (Heizkessel im Projekt) + 4 (BHKW im Projekt) +
        // 1 (Pufferspeicher im Projekt) + 1 (Stromspeicher im Projekt) +
        // 5 (Solarkollektoren) + 21 (Waermepumpen-Anlage) = 64; seit der Welle KI-F5
        // dazu 13 (BHKW-Katalogeditor) und 13 (Kollektoreditor) = 90.
        //
        // Mit der Welle KI-F7 verliert die Probe die Felder von Form_PV und der
        // Kostenverwaltung (beide binden jetzt ueber eine Sichtklasse) und gewinnt die
        // vier der Ueberlagerung „Anlagenwerte". Mit der Welle #458 geht auch die
        // Waermepumpen-Anlage auf eine Sichtklasse ueber. Die Schranke sagt weiterhin
        // nur, dass die Probe nicht ins Leere greift.
        Assert.True(felder >= 65, "Nur " + felder + " Feldpfade geprüft.");
    }

    // ---------------------------------------------------------------------
    //  Die FELDTAFEL „Alle Daten" der Projektmasken (Welle #458, Stufe 2)
    // ---------------------------------------------------------------------

    /// <summary>
    /// <b>Die sechs Erzeugermasken des Projekts führen „Alle Daten" GENAU nach dem
    /// Profil</b>, aus dem die Hülle den Aufklapper füllt — Heizkessel, BHKW,
    /// Pufferspeicher und Solarkollektoren nach dem <c>KatalogBrowserProfil</c>.
    /// </summary>
    /// <remarks>
    /// Er ersetzt für die Tafelfelder die Markup-Probe (eine Schleife hat keine Bindung
    /// mit Namen) und die Reflection-Probe (eine Tafel hat keine Eigenschaft je Feld):
    /// Name und Pfad mit Vorsilbe, Anzeigename „… (Alle Daten)", Feldtyp, Einheit und
    /// die Sperre eines nicht editierbaren Feldes.
    /// </remarks>
    [Theory]
    [InlineData(KiMaskennamen.HEIZKESSEL_PROJEKT, KatalogBrowserArt.Heizkessel)]
    [InlineData(KiMaskennamen.BHKW_PROJEKT, KatalogBrowserArt.Bhkw)]
    [InlineData(KiMaskennamen.PUFFERSPEICHER_PROJEKT, KatalogBrowserArt.Pufferspeicher)]
    [InlineData(KiMaskennamen.SOLARKOLLEKTOREN_PROJEKT, KatalogBrowserArt.Solarkollektoren)]
    public void Die_Projektmasken_fuehren_Alle_Daten_genau_nach_ihrem_Profil(string maske, KatalogBrowserArt art)
    {
        KatalogBrowserProfil profil =
            KatalogBrowserProfil.Finde(art, s => Resource.ResourceManager.GetString(s) ?? s);

        AlleDatenPruefen(maske, profil.Detailfelder
            .Select(p => (p.Schluessel, p.Feldname, p.Einheit, p.Art, !p.Editierbar)).ToList());
    }

    /// <summary>
    /// Dasselbe für Photovoltaik und Stromspeicher nach dem <c>ModulKatalogProfil</c> —
    /// nicht setzbar ist, was die Brücke des Aufklappers nicht zurückschreibt: der
    /// gesperrte Bezeichner und ein Auswahlfeld (<c>ModulFeldwertBruecke</c>).
    /// </summary>
    [Theory]
    [InlineData(KiMaskennamen.PHOTOVOLTAIK, ModulKatalogArt.Photovoltaik)]
    [InlineData(KiMaskennamen.STROMSPEICHER_PROJEKT, ModulKatalogArt.Stromspeicher)]
    public void Die_Modulmasken_fuehren_Alle_Daten_genau_nach_ihrem_Profil(string maske, ModulKatalogArt art)
    {
        ModulKatalogProfil profil =
            ModulKatalogProfil.Finde(art, s => Resource.ResourceManager.GetString(s) ?? s);

        AlleDatenPruefen(maske, profil.Felder
            .Select(p => (p.Schluessel, p.Feldname, p.Einheit, p.Art,
                          p.Gesperrt || p.Art == BrowserFeldArt.Auswahl)).ToList());
    }

    private static void AlleDatenPruefen(
        string maske, List<(string Schluessel, string Feldname, string Einheit, BrowserFeldArt Art, bool NurLesen)> profil)
    {
        KiDialog dialog = KiDialoge.Katalog.Finde(maske)!;
        Assert.NotNull(dialog);

        Type? typ = Datentyp(maske);
        Assert.NotNull(typ);
        Assert.True(typeof(IKiFeldtafel).IsAssignableFrom(typ), typ!.Name + " ist keine Feldtafel.");

        Assert.Equal(profil.Count, dialog.Felder.Count(IstAlleDaten));

        foreach (var p in profil)
        {
            string name = (KiDialoge.KATALOGFELD_VORSILBE + p.Schluessel).ToLowerInvariant();
            KiDialogFeld? k = dialog.FindeFeld(name);
            Assert.True(k is not null, "Das Profilfeld " + p.Schluessel + " fehlt in " + maske + ".");

            Assert.Equal(typ.Name + "." + KiDialoge.KATALOGFELD_VORSILBE + p.Schluessel, k!.Eigenschaftspfad);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resource.KI_DLG_ALLE_DATEN_NAME, p.Feldname),
                         k.Anzeigename);
            Assert.Equal(p.Einheit ?? "", k.Einheit);
            Assert.Equal(KiDialoge.Feldtyp(p.Art), k.Typ);
            Assert.Equal(p.NurLesen, k.NurLesen);
            Assert.True(IstTafelfeld(maske, k), name);
            Assert.False(string.IsNullOrWhiteSpace(k.Erlaeuterung), name);
        }
    }

    /// <summary>Ein Feld des Aufklappers „Alle Daten" — erkannt an der Vorsilbe im Pfad.</summary>
    private static bool IstAlleDaten(KiDialogFeld f)
        => f.Eigenschaft.StartsWith(KiDialoge.KATALOGFELD_VORSILBE, StringComparison.Ordinal);

    /// <summary>Das Daten-Objekt einer Maske aus <see cref="Masken"/>; <c>null</c> = keins.</summary>
    private static Type? Datentyp(string maske)
    {
        foreach (object[] zeile in Masken())
            if ((string)zeile[0] == maske) return (Type)zeile[1];
        return null;
    }

    /// <summary>
    /// Ist dieses Feld ein Feld der FELDTAFEL seiner Maske — das Daten-Objekt ist eine
    /// <see cref="IKiFeldtafel"/> und führt KEINE Eigenschaft dieses Namens?
    /// </summary>
    public static bool IstTafelfeld(string maske, KiDialogFeld feld)
    {
        Type? typ = Datentyp(maske);
        return typ is not null && typeof(IKiFeldtafel).IsAssignableFrom(typ) && !feld.IstSpalte &&
               typ.GetProperty(feld.Eigenschaft) is null;
    }

    /// <summary>
    /// Führt die Maske eine FELDTAFEL — dann ist ihr Baustein <c>Katalogfelder</c> über das
    /// Profil gedeckt und nicht über eine Bindung mit Namen (Eingabebilanz des Wächters).
    /// </summary>
    public static bool FuehrtFeldtafel(string maske)
        => KiDialoge.Katalog.Finde(maske)?.Felder.Any(f => IstTafelfeld(maske, f)) == true;

    // ---------------------------------------------------------------------
    //  Hilfen der Markup-Probe
    // ---------------------------------------------------------------------

    /// <summary>
    /// Kommt <paramref name="eigenschaft"/> im Markup als BINDUNG vor — also mit
    /// führendem Punkt und an einer Wortgrenze endend?
    /// </summary>
    /// <remarks>
    /// Die Wortgrenze am Ende ist nötig, damit <c>CO</c> nicht auf <c>Daten.CO2</c>
    /// trifft; der führende Punkt trennt die Bindung vom gleichlautenden Literaltext.
    /// </remarks>
    private static bool StehtImMarkup(string markup, string eigenschaft)
        => Regex.IsMatch(markup, @"\." + Regex.Escape(eigenschaft) + @"\b");

    /// <summary>
    /// Die einzelnen Dateien einer Maske — eine Zeile der Zuordnungstabelle trägt sie
    /// mit <c>;</c> getrennt (siehe <see cref="Markupdateien"/>).
    /// </summary>
    private static string[] Dateien(string eintrag)
        => eintrag.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Der Inhalt der Razor-Datei(en) einer Maske OHNE Kommentare: <c>@* … *@</c> und
    /// jede Zeile, die (nach Einrückung) mit <c>//</c> oder <c>///</c> beginnt.
    /// </summary>
    private static string MarkupOhneKommentare(string eintrag)
        => string.Join("\n", Dateien(eintrag).Select(EineDateiOhneKommentare));

    private static string EineDateiOhneKommentare(string repopfad)
    {
        string voll = Path.Combine(Wurzel(), repopfad.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(voll), repopfad);

        string text = Regex.Replace(File.ReadAllText(voll), @"@\*.*?\*@", " ",
                                    RegexOptions.Singleline);

        return string.Join("\n", text.Split('\n')
                                     .Where(z => !z.TrimStart().StartsWith("//", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Der Weg zur Repowurzel — dasselbe Verfahren wie
    /// <c>UeberlagerungstitelTests.Wurzel</c> und <c>StilblattTests.Wwwroot</c>.
    /// </summary>
    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    // =====================================================================
    //  Die fünfte Deklaration
    // =====================================================================

    [Fact]
    public void Die_Stromspeicher_Ansicht_fuehrt_fuenfundachtzig_Felder_und_keinen_Knopf()
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;

        // Seit #215 siebzehn („Peak-Ziel adaptiv", kausale Ratsche, Spezifikation
        // 5.1.1); seit AUFTRAG #224 sechsundzwanzig: der SCHRITT der Ansicht, die vier
        // Felder der Station 4 („beste Größe suchen", Feinraster, maximale Kandidaten,
        // Kandidatenzahl des Suchraums) und die vier des Kastens „Bestes Ergebnis".
        // Seit AUFTRAG #247 siebenundzwanzig: die SUCHMETHODE (nur lesend) sagt, WAS
        // der nächste Lauf variiert — Größe oder Stückzahl (SD‑E‑10).
        //
        // Mit der Welle KI-F6 kommen die STATIONEN 1 bis 4 dazu (Statuszeile #420,
        // Punkt b): 23 Spalten je Speichereinheit, 17 Felder der Datenquellen und
        // Kostensätze, 10 der Betriebsführung samt Netz und Prognose und 8 Spalten je
        // Suchachse — 58 neue, zusammen 85.
        Assert.Equal(85, d.Felder.Count);

        // ZWEI Sammlungen: die Einheiten der Flotte und die Achsen des Suchraums.
        // Ihre Zahl steht erst zur Laufzeit fest; deshalb sind sie Spalten und keine
        // Einzelfelder.
        Assert.Equal(23, d.Felder.Count(f => f.Sammlung == "Einheitenzeilen"));
        Assert.Equal(8, d.Felder.Count(f => f.Sammlung == "Suchachsen"));

        // KEINE Knöpfe: „Berechnen", „Peak-Ziel bestimmen…" und „Speichern" sind
        // rechnende bzw. datenbankwirksame Aktionen und gehören in das Aktionsregister
        // mit Bestätigung und Sicherungspunkt (Stufe S3), nicht in eine Knopfliste.
        Assert.Empty(d.Knoepfe);
    }

    [Theory]
    [InlineData("einheiten")]
    [InlineData("einheiten_liste")]
    [InlineData("kapazitaet_gesamt")]
    [InlineData("ladeleistung_gesamt")]
    [InlineData("entladeleistung_gesamt")]
    [InlineData("betriebsziel")]
    [InlineData("peak_ziel")]
    [InlineData("peak_ziel_adaptiv")]
    [InlineData("netzladung")]
    [InlineData("start_soc")]
    [InlineData("peak_reserve")]
    [InlineData("diagnose_arbeitslos")]
    [InlineData("diagnose_gruende")]
    [InlineData("pruefhinweise")]
    [InlineData("ergebnis_bezugsspitze")]
    [InlineData("ergebnis_netzbezug")]
    [InlineData("ergebnis_kapitalwert")]
    [InlineData("schritt")]
    [InlineData("groessen_optimieren")]
    [InlineData("feinraster")]
    [InlineData("maximale_kandidaten")]
    [InlineData("kandidatenzahl")]
    [InlineData("bestes_kapitalwert")]
    [InlineData("bestes_kapazitaet")]
    [InlineData("bestes_ersparnis")]
    [InlineData("bestes_phase")]
    public void Die_Stromspeicher_Ansicht_kennt_dieses_Feld(string name)
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;

        KiDialogFeld feld = d.FindeFeld(name)!;
        Assert.NotNull(feld);
        Assert.NotEmpty(feld.Anzeigename);
        Assert.NotEmpty(feld.Erlaeuterung);
    }

    [Fact]
    public void Nur_die_sechsundzwanzig_eingebbaren_Einzelfelder_sind_SETZBAR()
    {
        // Die übrigen achtundzwanzig FLACHEN Felder sind ABGELEITET: die Summen der
        // Flotte (eine Zahl je Feld, aber viele Einheiten dahinter), der Schritt der
        // Ansicht, die Suchmethode, die Kandidatenzahl des Suchraums, die Diagnose und
        // das Ergebnis des letzten Laufs. Die Stufe S3 muss ein feld_setzen darauf
        // ablehnen können, und das hängt an der Schreibbarkeit der Eigenschaft
        // (KiFeldzugang.Setzbar).
        //
        // MIT AUFTRAG #224 kamen drei setzbare dazu, alle aus Station 4; mit der Welle
        // KI-F6 die siebzehn der Station 2 und die zehn der Station 3. Die SPALTEN
        // bleiben hier außen vor — ihre Schreibbarkeit hängt am ZEILENtyp und nicht an
        // der Sicht; sie prüft der Fall darunter.
        string[] setzbar =
        {
            "betriebsziel", "peak_ziel", "peak_ziel_adaptiv", "netzladung", "start_soc",
            "peak_reserve", "groessen_optimieren", "feinraster", "maximale_kandidaten",

            // Station 2
            "lastquelle", "pv_quelle", "preisquelle", "modelljahr_zuordnen",
            "investitionsquelle", "betriebsquelle", "investition_leistung",
            "investition_kapazitaet", "betrieb_leistung", "betrieb_kapazitaet",
            "betrieb_entladen", "leistungspreis", "energie_ausgleich",
            "kalkulationszins", "jahresprojektion", "projektjahre", "restwert_studie",

            // Station 3
            "verteilung", "erzeuger_prioritaet", "batterieexport", "netzbezug_grenze",
            "netzeinspeisung_grenze", "informationsstand", "planungshorizont",
            "neuplanung", "endbedingung", "prognose_fallback"
        };

        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;
        var gefundenSetzbar = new List<string>();

        foreach (KiDialogFeld f in d.Felder)
        {
            if (f.IstSpalte) continue;

            var eigenschaft = typeof(StromspeicherKiSicht)
                .GetProperty(f.Eigenschaft,
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.Instance);

            Assert.NotNull(eigenschaft);
            if (eigenschaft!.CanWrite) gefundenSetzbar.Add(f.Name);
        }

        Assert.Equal(setzbar.OrderBy(x => x, StringComparer.Ordinal),
                     gefundenSetzbar.OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// <b>Jede SPALTE löst am Zeilentyp auf, und alle einunddreißig sind setzbar.</b>
    /// Eine Spalte ohne Setzer wäre eine Anzeige in einem Raster; die zwei
    /// Zeilenhüllen der Ansicht führen nur Eingabefelder — der Kartenname der
    /// Suchachse ist kein Katalogfeld.
    /// </summary>
    [Theory]
    [InlineData("Einheitenzeilen", typeof(EPOS.UI.Seiten.Strom.FlottenEinheitKiZeile), "Name")]
    [InlineData("Suchachsen", typeof(EPOS.UI.Seiten.Strom.FlottenAchseKiZeile), "Achse")]
    public void Jede_Spalte_der_Stromspeicher_Ansicht_loest_am_Zeilentyp_auf(
        string sammlung, Type zeilentyp, string kennzeichen)
    {
        KiDialog d = KiDialoge.Katalog.Finde(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)!;

        var spalten = d.Felder.Where(f => f.Sammlung == sammlung).ToList();
        Assert.NotEmpty(spalten);

        // Die Sammlung selbst gibt es an der Sicht, und sie liefert diesen Zeilentyp.
        var liste = typeof(StromspeicherKiSicht).GetProperty(sammlung)!;
        Assert.NotNull(liste);
        Assert.Equal(zeilentyp, liste.PropertyType.GetGenericArguments()[0]);

        // Das Zeilenkennzeichen steht am Zeilentyp — sonst hieße die Zeile „2".
        Assert.NotNull(zeilentyp.GetProperty(kennzeichen));

        foreach (KiDialogFeld f in spalten)
        {
            Assert.Equal(kennzeichen, f.Zeilenkennzeichen);

            var spalte = zeilentyp.GetProperty(f.Eigenschaft);
            Assert.True(spalte is not null, sammlung + "[]." + f.Eigenschaft);
            Assert.True(spalte!.CanWrite, f.Name);
            Assert.False(f.NurLesen, f.Name);
        }
    }

    // =====================================================================
    //  Das Sichtmodell rechnet aus den lebenden Ständen
    // =====================================================================

    [Fact]
    public void Die_Sicht_summiert_die_Flotte_und_mittelt_den_Start_SoC()
    {
        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.Equal(2, sicht.Einheitenzahl);
        Assert.Equal(40.0, sicht.KapazitaetGesamtKWh);
        Assert.Equal(16.0, sicht.LadeleistungGesamtKw);
        Assert.Equal(19.0, sicht.EntladeleistungGesamtKw);
        Assert.Equal(45.0, sicht.StartSocProzent);            // (0,40 + 0,50) / 2 · 100
        Assert.Equal(7.0, sicht.PeakReserveKWh);

        Assert.Equal("PeakShaving", sicht.Betriebsziel);
        Assert.Equal(16.0, sicht.PeakZielKw);
        Assert.False(sicht.NetzladungErlaubt);

        Assert.Contains("Eins: 24 kWh, 10 / 12 kW", sicht.EinheitenListe, StringComparison.Ordinal);
        Assert.Contains("Zwei: 16 kWh, 6 / 7 kW", sicht.EinheitenListe, StringComparison.Ordinal);
    }

    [Fact]
    public void Die_Sicht_liest_bei_jedem_Zugriff_neu()
    {
        // Die Ansicht ersetzt ihren Eingabestand bei jeder Änderung durch eine KOPIE;
        // ein festgehaltenes Objekt zeigte dem Assistenten den Stand von vorhin.
        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.Equal(2, sicht.Einheitenzahl);

        eingaben = Eingaben();
        eingaben.Auslegung!.Flotte!.Einheiten.RemoveAt(1);

        Assert.Equal(1, sicht.Einheitenzahl);
        Assert.Equal(24.0, sicht.KapazitaetGesamtKWh);
    }

    [Fact]
    public void Ohne_Flotte_und_ohne_Lauf_bleibt_die_Sicht_leer_statt_zu_werfen()
    {
        var sicht = new StromspeicherKiSicht(() => null, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.Equal(0, sicht.Einheitenzahl);
        Assert.Equal("", sicht.EinheitenListe);
        Assert.Equal(0.0, sicht.KapazitaetGesamtKWh);
        Assert.Equal(0.0, sicht.StartSocProzent);
        Assert.Equal("", sicht.Betriebsziel);
        Assert.Null(sicht.PeakZielKw);
        Assert.False(sicht.Arbeitslos);
        Assert.Equal("", sicht.DiagnoseGruende);
        Assert.Equal("", sicht.Pruefhinweise);
        Assert.Null(sicht.BezugsspitzeKw);
        Assert.Null(sicht.NetzbezugKWh);
        Assert.Null(sicht.KapitalwertEuro);
    }

    [Fact]
    public void Die_Sicht_gibt_Diagnose_und_Ergebnis_des_letzten_Laufs_heraus()
    {
        // „Warum ist die Flotte arbeitslos?" wird damit mit den echten Zählern
        // beantwortet und nicht mit einer Vermutung.
        var ergebnis = new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Studie = new FlottenStudienErgebnis
            {
                Variante = new FlottenSimulationErgebnis
                {
                    NetzbezugKWh = 51_611.0,
                    MaximalerNetzbezugKw = 16.7428,
                    Diagnose = new FlottenDiagnose
                    {
                        IntervalleGesamt = 35_040,
                        Arbeitslos = true,
                        IntervalleLadedeckelNullNetzladeverbot = 12_000
                    }
                },
                Wirtschaftlichkeit = new FlottenWirtschaftlichkeitErgebnis
                {
                    KapitalwertEuro = -1234.5
                }
            },
            Pruefhinweise =
            {
                new FlottenHinweis { Stufe = FlottenHinweisStufe.Warnung, Text = "Kein Peak-Ziel gesetzt." },
                new FlottenHinweis { Stufe = FlottenHinweisStufe.Hinweis, Text = "Netzladung verboten." }
            }
        };

        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => ergebnis,
                                             () => Array.Empty<FlottenHinweis>());

        Assert.True(sicht.Arbeitslos);
        Assert.NotEqual("", sicht.DiagnoseGruende);
        Assert.Equal("Kein Peak-Ziel gesetzt.\nNetzladung verboten.", sicht.Pruefhinweise);
        Assert.Equal(16.7428, sicht.BezugsspitzeKw);
        Assert.Equal(51_611.0, sicht.NetzbezugKWh);
        Assert.Equal(-1234.5, sicht.KapitalwertEuro);
    }

    [Fact]
    public void Ohne_Lauf_traegt_die_Sicht_die_Hinweise_der_VORPRUEFUNG()
    {
        var eingaben = Eingaben();
        var vorpruefung = new[]
        {
            new FlottenHinweis { Stufe = FlottenHinweisStufe.Hinweis, Text = "Kostensätze fehlen." }
        };

        var sicht = new StromspeicherKiSicht(() => eingaben, () => null, () => vorpruefung);

        Assert.Equal("Kostensätze fehlen.", sicht.Pruefhinweise);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Ein Arbeitsstand nach dem Muster des Referenzprojekts 1046 („Prüfprojekt
    /// Speicherflotte"): zwei Einheiten, Ziel <c>PeakShaving</c> gegen 16 kW.
    /// </summary>
    // =====================================================================
    //  Die Stationen 1 bis 4 der Ansicht (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// <b>Die Spalten schreiben in die LEBENDE Einheit</b> — und rechnen die fünf
    /// Prozentwerte um: Die Engine führt sie als Anteil 0…1, die Maske zeigt Prozent.
    /// </summary>
    [Fact]
    public void Die_Einheitenzeilen_schreiben_durch_und_rechnen_Prozent_um()
    {
        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        IReadOnlyList<EPOS.UI.Seiten.Strom.FlottenEinheitKiZeile> zeilen = sicht.Einheitenzeilen;
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Eins", zeilen[0].Name);
        Assert.Equal(24.0, zeilen[0].Kapazitaet);

        // 0,40 in der Engine sind 40 % auf der Maske.
        Assert.Equal(40.0, zeilen[0].SocStart, 6);

        zeilen[0].SocStart = 55.0;
        zeilen[0].Kapazitaet = 30.0;

        FlottenEinheit erste = eingaben.Auslegung!.Flotte!.Einheiten[0];
        Assert.Equal(0.55, erste.SocStart, 6);
        Assert.Equal(30.0, erste.KapazitaetKWh);

        // Die Liste entsteht bei JEDEM Zugriff neu — eine gehaltene zeigte den Stand
        // von vorhin.
        Assert.Equal(30.0, sicht.Einheitenzeilen[0].Kapazitaet);
    }

    /// <summary>
    /// <b>Station 2 und 3 schreiben in den lebenden Stand</b> — der Leistungspreis an
    /// BEIDE Orte, die er auf der Maske hat, und der Zins als Anteil.
    /// </summary>
    [Fact]
    public void Die_Stationen_zwei_und_drei_schreiben_in_den_lebenden_Stand()
    {
        var eingaben = Eingaben();
        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        sicht.Leistungspreis = 137.5;
        Assert.Equal(137.5, eingaben.LeistungspreisEurProKwA);
        Assert.Equal(137.5, eingaben.Auslegung!.Flotte!.Tarif.LeistungspreisEuroProKw);

        sicht.KalkulationszinsProzent = 4.5;
        Assert.Equal(0.045, eingaben.Auslegung.Flotte.Wirtschaftlichkeit.Kalkulationszins, 9);

        sicht.InvestitionProKWh = 350.0;
        Assert.Equal(350.0, eingaben.Auslegung.DirekteKosten.InvestEurProKwh);
        Assert.True(eingaben.Auslegung.DirekteKosten.InvestVorhanden);

        sicht.Verteilung = FlottenVerteilung.Grenzkosten;
        Assert.Equal(FlottenVerteilung.Grenzkosten,
                     eingaben.Auslegung.Flotte.Optionen.Verteilung);

        sicht.NetzbezugGrenzeKw = 250.0;
        Assert.Equal(250.0, eingaben.Auslegung.Flotte.Optionen.NetzbezugGrenzeKw);

        // Die Wahllisten stehen je EINMAL: sie kommen aus den Bausteinen der Maske.
        Assert.Equal(3, sicht.VerteilungWahl.Count);
        Assert.Equal(2, sicht.LastquelleWahl.Count);
        Assert.Equal(3, sicht.PreisquelleWahl.Count);
        Assert.Equal(3, sicht.EndbedingungWahl.Count);
    }

    /// <summary>
    /// <b>Die Suchachsen tragen den Namen ihrer Einheit</b> — so beschriftet der
    /// Optimierungsblock seine Karten — und schreiben durch.
    /// </summary>
    [Fact]
    public void Die_Suchachsen_tragen_den_Einheitennamen_und_schreiben_durch()
    {
        var eingaben = Eingaben();
        eingaben.Auslegung!.Flotte!.Auslegung.Achsen.Add(
            new FlottenAuslegungsAchse { ErsetztEinheitId = "2", KapazitaetVonKWh = 5.0 });

        var sicht = new StromspeicherKiSicht(() => eingaben, () => null,
                                             () => Array.Empty<FlottenHinweis>());

        IReadOnlyList<EPOS.UI.Seiten.Strom.FlottenAchseKiZeile> achsen = sicht.Suchachsen;
        Assert.Single(achsen);
        Assert.Equal("Zwei", achsen[0].Achse);
        Assert.Equal(5.0, achsen[0].KapazitaetVon);

        achsen[0].KapazitaetBis = 40.0;
        achsen[0].Quelle = FlottenKandidatenquelle.Stammdaten;

        Assert.Equal(40.0, eingaben.Auslegung.Flotte.Auslegung.Achsen[0].KapazitaetBisKWh);
        Assert.Equal(FlottenKandidatenquelle.Stammdaten,
                     eingaben.Auslegung.Flotte.Auslegung.Achsen[0].Quelle);

        Assert.Equal(2, sicht.QuelleWahl.Count);
    }

    private static SpeicherOptimierungEingaben Eingaben()
    {
        return new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Flotte = new FlottenStudieKonfiguration
                {
                    Einheiten =
                    {
                        new FlottenEinheit
                        {
                            Id = "1", Name = "Eins",
                            KapazitaetKWh = 24.0, LadeleistungKw = 10.0, EntladeleistungKw = 12.0,
                            SocStart = 0.40, PeakReserveKWh = 3.0
                        },
                        new FlottenEinheit
                        {
                            Id = "2", Name = "Zwei",
                            KapazitaetKWh = 16.0, LadeleistungKw = 6.0, EntladeleistungKw = 7.0,
                            SocStart = 0.50, PeakReserveKWh = 4.0
                        }
                    },
                    Optionen = new FlottenSimulationOptionen
                    {
                        Betriebsziel = FlottenBetriebsziel.PeakShaving,
                        Verteilung = FlottenVerteilung.Kaskade,
                        WirtschaftlicherPeakZielwertKw = 16.0,
                        NetzladungErlaubt = false
                    }
                }
            }
        };
    }
}
