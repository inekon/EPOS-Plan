// EINMAL ERZEUGT — seit W16c DIE QUELLE.
//
// Herkunft: WindowsFormsApplication1/MDIMainForm.Designer.cs (45 ToolStripMenuItem,
// 6 ToolStripSeparator) und MDIMainForm.cs (die neun Punkte der acht Init*-
// Methoden), dazu MDIMainForm.resx / .de-DE.resx / .en-US.resx. Erzeuger war
// das Skript w16c_menue.py der Teilwelle iU9-W16c (Auflage R-W16-8: "Die
// Menuetabelle wird per Skript erzeugt, nicht abgetippt").
//
// WER DAS MENUE AENDERT, AENDERT DIESE DATEI. Der Designer ist mit W16c.3
// geloescht, und das Erzeugerskript w16c_menue.py liegt NICHT im Repository:
// Es gibt keine Vorlage mehr, aus der sich die Tabelle neu ziehen liesse. Wer
// das Skript dennoch je wiederfaende und laufen liesse, bekaeme den Stand vom
// 03.09.2026 zurueck und muesste jeden Anwenderentscheid von Hand nachtragen -
// die folgenden zwei Abschnitte sagen, welche.
//
// ANWENDERENTSCHEID W16c-E-2 (04.09.2026) - der Kopf "Sprache"
// (MENU_SPRACHE). Die zwei Sprachpunkte standen im Bestand als Koepfe der
// obersten Ebene und sind seither seine Untereintraege. Ihre Namen, Bilder und
// Seitenschluessel sind unveraendert; der Kopf hat KEINE Designer-Herkunft und
// kein Ziel.
//
// ANWENDERENTSCHEID W16c-E-6 (06.09.2026) - der Kopf "Administration" ist
// umgeordnet ("Verschiebe BHKW von Energiesystem in 'Waermebedarf & Heizung'
// ..."). Vier Bewegungen und zwei Aufloesungen:
//   1. BHKW und Solarkollektoren wandern von "Energiesysteme" nach
//      "Waermebedarf & Heizung" - dorthin, wo der Anwender einen Waermeerzeuger
//      sucht.
//   2. Pufferspeicher wandert die Gegenrichtung, von "Waermebedarf & Heizung"
//      nach "Energiesysteme".
//   3. Die neue Unterrubrik "Profile & Lastgaenge"
//      (MenuItem_ProfileLastgaenge, MENU_PROFILE_LASTGAENGE) sammelt in
//      "Waermebedarf & Heizung", was Zeitreihe und nicht Geraet ist:
//      Waermebedarf Lastgang, Prozesswaerme und - aus "Energiesysteme" -
//      Solarthermieganglinie, in dieser Reihenfolge. Sie ist wie der Kopf
//      "Sprache" ohne Designer-Herkunft und ohne Ziel.
//   4. Die zwei Untermenues mit EINEM Punkt "Bearbeiten" sind aufgeloest:
//      MenuItem_PV und MenuItem_Solarkollektoren tragen jetzt selbst das Ziel
//      ihres frueheren Kindes (PvAdmin bzw. SolarkollektorenAdmin). Die zwei
//      Kinder MenuItem_PC_Bearbeiten und MenuItem_ST_Bearbeiten fallen damit
//      weg; ihre Textschluessel MENU_PC_BEARBEITEN und MENU_ST_BEARBEITEN
//      bleiben im Katalog stehen, werden vom Menue aber nicht mehr gelesen.
// Namen, Seitenschluessel, Bilder und Kuerzel der verschobenen Punkte sind
// unveraendert - es wandert die Zuordnung, nicht die Kennung.
//
// ANWENDERENTSCHEID W16c-E-7 (07.09.2026) - zwei Zwischenknoten
// "Photovoltaik" ("Mache zwei Untermenues Photovoltaik 1. PV Module
// 2. Wechselrichter"). Das Paar Modul/Wechselrichter steht an ZWEI Stellen des
// Kopfes "Administration", und der Anwender hat beide gemeint - er arbeitete im
// Import und stellte richtig: "der Import steht nicht unter Energiesysteme
// sondern vdi3805 (Wechselrichter)". Also zweimal derselbe Knoten:
//   1. "Energiesysteme" fuehrt statt zweier Geschwister den Knoten
//      MenuItem_PV_Gruppe mit "PV Module" (MenuItem_PV) und "Wechselrichter"
//      (MenuItem_Wechselrichter); "Pufferspeicher" bleibt daneben.
//   2. "Daten & Import" fuehrt an der Stelle der zwei PV-Punkte den Knoten
//      MenuItem_PV_Import_Gruppe mit "PV Module (CEC, PAN)..."
//      (MenuItem_PV_Import_CEC) und "Wechselrichter (CEC, OND)..."
//      (MenuItem_WR_Import_CEC); die vier uebrigen Importpunkte bleiben.
// Beide Knoten tragen DENSELBEN Textschluessel MENU_PHOTOVOLTAIK und sind wie
// der Kopf "Sprache" und die Rubrik "Profile & Lastgaenge" ohne
// Designer-Herkunft, ohne Bild und ohne Ziel. Die Regel aus W16c-E-6 - kein
// Untermenue mit nur EINEM Punkt - bleibt gewahrt: Jeder Knoten fuehrt zwei.
// ZIEL, ARGUMENT UND NAME DER VIER PUNKTE SIND UNVERAENDERT; es wandert ihre
// Lage im Baum und bei zweien die Beschriftung: MenuItem_PV traegt jetzt
// MENU_PV_MODULE ("PV Module"), weil "Photovoltaik" ueber ihm steht und die
// Zeile sonst zweimal dasselbe sagte, und MENU_PV_IMPORT_CEC heisst statt
// "Import Photovoltaik CEC/Pan" nun "PV Module (CEC, PAN)..." - der Zwilling
// von "Wechselrichter (CEC, OND)..." unter demselben Knoten. MENU_PV bleibt
// wie MENU_PC_BEARBEITEN im Katalog stehen und wird vom Menue nicht mehr
// gelesen.
//
// ANWENDERENTSCHEID W16c-O-7 (07.09.2026) - das letzte Ein-Punkt-Untermenue
// ist aufgeloest. Die Regel aus W16c-E-6 ("kein Untermenue mit nur EINEM
// Punkt") war im Kopf "Administration" noch einmal verletzt: Der Knoten
// MenuItem_Klima ("Klimadaten") fuehrte als einziges Kind den Punkt
// MenuItem_Klimadaten ("Klimadaten") - dieselbe Beschriftung zweimal, ein
// Klick zuviel. Der Anwender hat auf Rueckfrage "ja" gesagt; der Punkt steht
// jetzt an der Stelle des Knotens unmittelbar im Kopf und traegt dessen Bild
// Menu4. Sein Name, sein Textschluessel (MENU_KLIMADATEN) und sein Ziel
// (Seitenschluessel.Klimadaten) sind unveraendert - help_mapping.txt und die
// Huelle greifen weiter. MENU_KLIMA bleibt wie MENU_PC_BEARBEITEN und MENU_PV
// im Katalog stehen und wird vom Menue nicht mehr gelesen. Damit fuehrt KEIN
// Untermenue mehr nur einen Punkt, und der Waechter
// Ein_neues_Untermenue_fuehrt_nie_nur_einen_einzigen_Punkt gilt ohne jede
// Ausnahme.
//
// ANWENDERWUNSCH W13-E-2 (07.09.2026) - der STROMSPEICHERIMPORT. Er ist der
// erste NEUE Weg seit W6-E-2 und der Grund, warum die Zahl der handelnden
// Punkte auf 45 steigt: "Es gibt keinen Datenimport fuer Stromspeicher. Dieser
// muss noch hinzugefuegt werden (Administration -> Datenimport)." Der Punkt
// MenuItem_SP_Import (MENU_SP_IMPORT, Seitenschluessel.StromspeicherImport)
// steht in "Daten & Import" HINTER dem Knoten "Photovoltaik" und VOR
// "Import Solarkollektoren"; die Maske ist die fuenfte Auspraegung des
// KatalogImportDialog (Stufe S1 des Konzept_Stromspeicherimport_EPOS-Plan.md,
// Entscheide Q1...Q8 = Empfehlung).
//
// ANWENDERENTSCHEID 19.09.2026 (MN-1) - der Kopf "Administration" ist neu
// GEORDNET und in fuenf Bloecke mit VIER Trennstrichen geteilt ("Generell ist
// die Reihenfolge der Menue-Kategorien und der Unterkategorien nicht optimal
// (u.a. fehlt bei Kosten ein Symbol). ... 'Gesetzliche Parameter' -> kann evtl.
// hier raus? ... Einstellungen ans Ende, Katalogdoubletten an andere Stelle.").
// Die Bloecke lesen sich wie der Gang eines Projekts: ORT (Gebaeude,
// Klimadaten) | ANLAGEN (Waermebedarf & Heizung, Strombedarf & Speicher,
// Energiesysteme) | KOSTEN | DATEN & IMPORT | EINSTELLUNGEN. Kein Name, kein
// Textschluessel und kein Seitenschluessel aendert sich - es wandert die Lage.
//   1. "Gebaeude" wandert von der VORLETZTEN Stelle an die ERSTE, "Klimadaten"
//      aus der Mitte dahinter: Beide beschreiben den Ort, auf dem alles
//      Weitere rechnet.
//   2. In "Waermebedarf & Heizung" steht die Unterrubrik "Profile &
//      Lastgaenge" jetzt VOR den Erzeugern (Kessel, BHKW, Waermepumpen,
//      Solarkollektoren) - erst der Bedarf, dann was ihn deckt. Die Reihe der
//      Erzeuger folgt der Rubrikordnung des Kopfes.
//   3. "Kosten" (MenuItem_KostenVerwaltung) bekommt das NEUE Bild kosten_32
//      und als viertes Kind MenuItem_Gesetzesparameter: Die gesetzlichen
//      Parameter (KWKG, EEG, Steuern, CO2-Preis, Umsatzsteuer,
//      Bilanzkonvention) werden ausschliesslich in der Wirtschaftlichkeit
//      gerechnet, nicht in Simulation oder Referenzlauf. Ihr Bild
//      gesetzliche_parameter_32 faellt dabei weg - unter einer Rubrik traegt
//      kein Blatt ein eigenes.
//   4. "Daten & Import" fuehrt die Importe in der Reihenfolge der KATALOGE
//      darueber (Kessel, Waermepumpe, Solarthermie, Pufferspeicher,
//      Photovoltaik, Stromspeicher) und danach - hinter einem Trennstrich -
//      MenuItem_KatalogDubletten, das von der obersten Ebene hierher wandert:
//      Dubletten entstehen BEIM Einlesen.
//   5. "Einstellungen" steht ans ENDE des Kopfes.
//   6. MenuItem_LizenzVerwaltung ("Lizenz…", Seitenschluessel.LizenzVerwaltung,
//      Bild lizenzen_32) ENTFAELLT ersatzlos ("Es gibt einen Lizenz-Dialog fuer
//      Lizenz aktivieren. Diese ist ... unter Hilfe->Lizenz bereits
//      vorhanden."). Der eine Einstieg ist Hilfe -> Lizenz; dort fuehrt der
//      Dialog seit MN-1 den Reiter "Status & Aktivierung". Der Textschluessel
//      MENU_LIZENZ_VERWALTUNG bleibt wie MENU_PC_BEARBEITEN, MENU_PV und
//      MENU_KLIMA im Katalog stehen und wird vom Menue nicht mehr gelesen.
//
// GEBAEUDESIMULATION G3 (Softwarearchitektur Gebaeudesimulation 3.1) - der Knoten
// "Gebaeude" (MenuItem_Gebaeude) fuehrt hinter "Gebaeudetypen" zwei neue Punkte:
// "Baustoffe" (MenuItem_Baustoffe, MENU_BAUSTOFFE, Seitenschluessel.BaustoffKatalog) und
// "Bauteilaufbauten" (MenuItem_Bauteilaufbauten, MENU_BAUTEILAUFBAUTEN,
// Seitenschluessel.BauteilaufbauKatalog) - gemeinsam, weil ein Aufbau aus den Stoffen
// entsteht. Beide Ziele sind freie Ansichten der AppWurzel.
//
// ZAPFPROFILGENERATOR (Umsetzungskonzept 5.4, Stufe Z4) - der Punkt
// "Brauchwasser" in "Waermebedarf & Heizung" wird ein UNTERMENUE mit zwei
// Punkten: "Brauchwasserprofile" (MenuItem_Brauchwasserprofile,
// MENU_BRAUCHWASSERPROFILE) traegt das bisherige Ziel
// Seitenschluessel.BrauchwasserAdmin, "Brauchwasser-Nutzungsarten"
// (MenuItem_BrauchwasserNutzungsarten, MENU_BRAUCHWASSER_NUTZUNGSARTEN) das NEUE
// Ziel Seitenschluessel.BrauchwasserNutzungsarten, den Katalog des
// Zapfprofilgenerators. Der Knoten behaelt Namen und Textschluessel
// (MenuItem_Brauchwasser, MENU_BRAUCHWASSER) und verliert sein Ziel; die Regel
// "kein Untermenue mit nur EINEM Punkt" bleibt gewahrt.
//
// DER KATALOGEINSTIEG OHNE MENUEBAND (Umsetzungskonzept Zapfprofilgenerator,
// Kapitel 9 ZU34) - zwoelf Punkte tragen das Kennzeichen katalog: true: die zwei
// Kataloge der Gebaeudehuelle, die Brauchwasser-Nutzungsarten und die neun
// Geraete- und Verbraucherkataloge (Kessel, BHKW, Waermepumpen, Solarkollektoren,
// Stromverbraucher, Stromspeicher, PV Module, Wechselrichter, Pufferspeicher). Das
// Menue selbst liest es nicht; es liest Kataloge(), die Quelle des Knopfes
// "Kataloge..." der Projektliste auf dem iPad. Ob ein gekennzeichneter Punkt dort
// erscheint, entscheidet die Wurzel (AppWurzel.FuehrtZiel) - keine zweite Liste.

using System;
using System.Collections.Generic;
using EPOS.UI.Seiten;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Das Menue des Hauptfensters als DATEN (iU9-W16c.1).
///
/// <para><b>63 Punkte</b> - 45 aus dem Designer des Vorlaeufers und
/// 9, die dort programmatisch eingehaengt wurden ("damit Designer und
/// .resx unberuehrt bleiben", MDIMainForm.cs:57, :95, :132, :174, :311, :414,
/// :531). Der Grund dafuer entfaellt mit dem Designer; hier sind es
/// gleichrangige Zeilen. Dazu kommen die Zeilen OHNE Designer-Herkunft:
/// der Kopf "Sprache" (W16c-E-2), die Unterrubrik "Profile & Lastgaenge"
/// (W16c-E-6) und die zwei Knoten "Photovoltaik" (W16c-E-7); dafuer fallen
/// mit W16c-E-6 die zwei Ein-Punkt-Untermenues MenuItem_PC_Bearbeiten und
/// MenuItem_ST_Bearbeiten weg, und mit W6-E-2 kommen die zwei
/// Wechselrichterpunkte hinzu; mit W16c-O-7 faellt das dritte und letzte
/// Ein-Punkt-Untermenue MenuItem_Klima, mit W13-E-2 kommt der
/// Stromspeicherimport hinzu, mit SIM-Q3 der Punkt „Simulation…", mit
/// ND-Q3 der Punkt „Nutzungsdauern (AfA)…", mit MN-1 faellt
/// MenuItem_LizenzVerwaltung, und mit dem Zapfprofilgenerator (5.4) kommen
/// die zwei Punkte unter „Brauchwasser" hinzu, mit der Gebaeudesimulation G3 die zwei
/// Kataloge unter „Gebaeude". Also
/// 54 Bestandspunkte
/// + 2 - 2 + 2 + 2 - 1 + 1 + 1 + 1 - 1 + 2 + 2 = 63, dazu 13 Trennstriche
/// (8 aus dem Bestand und die 5 aus MN-1: vier in der obersten Ebene des
/// Kopfes "Administration", einer in "Daten &amp; Import").</para>
///
/// <para><b>Vier Koepfe</b> in der obersten Ebene: Projekt, Administration,
/// Hilfe und - ganz rechts, wo bis W16c-E-2 "Deutsch" stand - Sprache. Alle
/// vier klappen nur auf; von den 63 Punkten handeln <b>49</b>, 14 klappen auf.
/// Die Zahl der HANDELNDEN Punkte ist mit W16c-E-6, mit W16c-E-7 und mit
/// W16c-O-7 unveraendert geblieben: Es ist kein Ziel entfallen und keines
/// hinzugekommen, es steht nur an einer anderen Stelle des Baumes. Gewachsen
/// ist sie dreimal, und jedes Mal um einen ECHTEN neuen Weg: mit W6-E-2 um
/// den Wechselrichterkatalog und seinen Import (42 -> 44), mit W13-E-2 um den
/// Stromspeicherimport (44 -> 45) und mit SIM-Q3 (Auftrag #207) um die Ansicht
/// „Simulation" (45 -> 46); GESUNKEN ist sie genau einmal - mit MN-1
/// (19.09.2026) faellt der zweite Weg zur Lizenzverwaltung (47 -> 46), weil
/// derselbe Dialog unter Hilfe -> Lizenz steht. Mit dem Zapfprofilgenerator
/// (5.4) waechst sie um den Katalog der Brauchwasser-Nutzungsarten (46 -> 47), mit der
/// Gebaeudesimulation G3 um die Kataloge der Baustoffe und der Bauteilaufbauten (47 -> 49).</para>
///
/// <para><b>Jeder Klick ist ein <see cref="Seitenschluessel"/>.</b> Der Vorlaeufer
/// fuehrte 34 Ereignishandler mit je einer Wirkzeile, dazu neun Lambdas in den
/// Init*-Methoden; hier steht das ZIEL in der Zeile, und
/// <c>Hauptfenster.Springe</c> ist der einzige Handler.</para>
///
/// <para><b>Die Beschriftung steht nicht hier</b>, sondern als Schluessel: Der
/// Punkt traegt <see cref="Menuepunkt.TextSchluessel"/>, der Text kommt zur
/// Laufzeit aus <c>MyResource</c> und ist damit zweisprachig. Vier der
/// 45 Designer-Punkte hatten in <c>MDIMainForm.en-US.resx</c> gar keinen
/// Eintrag und drei nur einen neutralen (Befund W16-B26 und die drei
/// nachgemessenen) - sie sind beim Umzug ergaenzt worden.</para>
/// </summary>
public static class Menuetabelle
{
    /// <summary>Die Punkte der obersten Ebene, in der Reihenfolge des Bestands.</summary>
    public static readonly IReadOnlyList<Menuepunkt> Eintraege = new Menuepunkt[]
    {
        new Menuepunkt("Projekte", "MENU_PROJEKTE", "")
        {
            new Menuepunkt("MenuItem_ProjektNeu", "MENU_PROJEKT_NEU", Seitenschluessel.ProjektNeu),
            Menuepunkt.Trennstrich("toolStripSeparator1"),
            new Menuepunkt("MenuItem_ProjektOeffnen", "MENU_PROJEKT_OEFFNEN", Seitenschluessel.ProjektOeffnen),
            Menuepunkt.Trennstrich("toolStripSeparator2"),
            new Menuepunkt("MenuItem_ProjektBearbeiten", "MENU_PROJEKT_BEARBEITEN", Seitenschluessel.ProjektBearbeiten),
            Menuepunkt.Trennstrich("toolStripSeparator3"),
            new Menuepunkt("MenuItem_zuletztGeöffnet", "MENU_ZULETZT_GEOEFFNET", Seitenschluessel.ProjektZuletzt),
            Menuepunkt.Trennstrich("toolStripSeparator4"),
            new Menuepunkt("MenuItem_ProjektLöschen", "MENU_PROJEKT_LOESCHEN", Seitenschluessel.ProjektLoeschen),
            Menuepunkt.Trennstrich("toolStripSeparator5"),
            new Menuepunkt("MenuItem_ExportImport", "MENU_EXPORT_IMPORT", Seitenschluessel.ProjektTransfer),
            Menuepunkt.Trennstrich("MenuItem_TrennerVarianten"),
            new Menuepunkt("MenuItem_AlsVariante", "MENU_VARIANTE_SPEICHERN", Seitenschluessel.ProjektAlsVariante),
            new Menuepunkt("MenuItem_VariantenBericht", "MENU_VARIANTEN_BERICHT", Seitenschluessel.BerichteKosten),
            // ANWENDERENTSCHEID SIM-Q3 (11.09.2026, Auftrag #207): der einzige
            // NEUE Punkt des Kopfs „Projekt" seit W16c. Bis dahin war die
            // Startseite der EINZIGE Weg in die Simulation - das Menue fuehrte
            // keinen Punkt dorthin (Konzept „Simulationsablauf" 1.1). Er steht
            // unmittelbar hinter „Varianten und Bericht…" und traegt KEIN
            // Untermenue (Regel W16c-E-6); sein Ziel ist die freie Ansicht
            // SIMULATION, die ohne Marke bei Schritt ① aufmacht.
            new Menuepunkt("MenuItem_Simulation", "MENU_SIMULATION", Seitenschluessel.Simulation),
        },
        new Menuepunkt("Administration", "MENU_ADMINISTRATION", "")
        {
            // MN-1 (19.09.2026): Das Gebaeude steht ZUERST - es ist der erste
            // Schritt jedes Projekts, und was danach kommt, rechnet auf ihm.
            new Menuepunkt("MenuItem_Gebaeude", "MENU_GEBAEUDE", "", bild: "Menue6")
            {
                new Menuepunkt("MenuItem_GebBearbeiten", "MENU_GEB_BEARBEITEN", Seitenschluessel.GebaeudeAdmin),
                new Menuepunkt("MenuItem_GebTypen", "MENU_GEB_TYPEN", Seitenschluessel.GebaeudetypenAdmin),
                // Gebaeudesimulation G3 (Softwarearchitektur 3.1): die zwei Kataloge der
                // Gebaeudehuelle, GEMEINSAM eingehaengt - Baustoffe und die Aufbauten aus
                // ihnen. Beide sind freie Ansichten der Wurzel, auf beiden Plattformen.
                new Menuepunkt("MenuItem_Baustoffe", "MENU_BAUSTOFFE", Seitenschluessel.BaustoffKatalog, katalog: true),
                new Menuepunkt("MenuItem_Bauteilaufbauten", "MENU_BAUTEILAUFBAUTEN", Seitenschluessel.BauteilaufbauKatalog, katalog: true),
            },
            // W16c-O-7: der Punkt stand bis zum 07.09.2026 als EINZIGES Kind
            // im Untermenue MenuItem_Klima ("Klimadaten" ueber "Klimadaten").
            // Er traegt jetzt an dessen Stelle das Bild Menu4 des gefallenen
            // Knotens; Name, Textschluessel und Ziel sind unveraendert.
            // MN-1 (19.09.2026): Er steht mit dem Gebaeude im ersten Block -
            // Gebaeude und Klima sind der ORT, alles Weitere die Anlage.
            new Menuepunkt("MenuItem_Klimadaten", "MENU_KLIMADATEN", Seitenschluessel.Klimadaten, bild: "Menu4"),
            // MN-1: Ort | Anlagen
            Menuepunkt.Trennstrich("MenuItem_TrennerAdminOrt"),
            new Menuepunkt("MenuItem_WBundHeizung", "MENU_WBUND_HEIZUNG", "", bild: "Menu1")
            {
                // Zapfprofilgenerator 5.4: aus dem Punkt wird ein Untermenue mit
                // den Profilen (bisheriges Ziel) und dem Katalog der Nutzungsarten.
                new Menuepunkt("MenuItem_Brauchwasser", "MENU_BRAUCHWASSER", "")
                {
                    new Menuepunkt("MenuItem_Brauchwasserprofile", "MENU_BRAUCHWASSERPROFILE", Seitenschluessel.BrauchwasserAdmin),
                    new Menuepunkt("MenuItem_BrauchwasserNutzungsarten", "MENU_BRAUCHWASSER_NUTZUNGSARTEN",
                                   Seitenschluessel.BrauchwasserNutzungsarten, katalog: true),
                },
                // W16c-E-6: die neue Unterrubrik. Sie ist die zweite Zeile ohne
                // Designer-Herkunft (nach dem Kopf "Sprache") und traegt darum
                // KEIN Bild; ihre drei Punkte kommen unveraendert aus
                // "Waermebedarf & Heizung" und aus "Energiesysteme".
                // MN-1 (19.09.2026): Sie steht jetzt VOR den Erzeugern - erst
                // der Bedarf, dann was ihn deckt.
                new Menuepunkt("MenuItem_ProfileLastgaenge", "MENU_PROFILE_LASTGAENGE", "")
                {
                    new Menuepunkt("MenuItem_WaermebedarfExtern", "MENU_WAERMEBEDARF_EXTERN", Seitenschluessel.WaermebedarfExternAdmin),
                    new Menuepunkt("MenuItem_Prozesswaerme", "MENU_PROZESSWAERME", Seitenschluessel.ProzesswaermeAdmin),
                    new Menuepunkt("MenuItem_SolThermGanglinie", "MENU_SOL_THERM_GANGLINIE", Seitenschluessel.SolarganglinieAdmin),
                },
                new Menuepunkt("MenuItem_Kessel", "MENU_KESSEL", Seitenschluessel.HeizkesselAdmin, katalog: true),
                // W16c-E-6: aus "Energiesysteme" hierher.
                new Menuepunkt("MenuItem_BHKW", "MENU_BHKW", Seitenschluessel.BhkwAdmin, katalog: true),
                new Menuepunkt("MenuItem_WP", "MENU_WP", Seitenschluessel.WpAdministration, katalog: true),
                // W16c-E-6: aus "Energiesysteme" hierher - und dabei aus seinem
                // Untermenue heraus. Es fuehrte nur "Bearbeiten"
                // (MenuItem_ST_Bearbeiten); das Ziel ist unveraendert
                // SolarkollektorenAdmin.
                new Menuepunkt("MenuItem_Solarkollektoren", "MENU_SOLARKOLLEKTOREN", Seitenschluessel.SolarkollektorenAdmin, katalog: true),
            },
            new Menuepunkt("MenuItem_StromBedarfundSp", "MENU_STROM_BEDARFUND_SP", "", bild: "Menue2")
            {
                new Menuepunkt("MenuItem_Stromverbraucher", "MENU_STROMVERBRAUCHER", Seitenschluessel.StromverbraucherAdmin, katalog: true),
                new Menuepunkt("MenuItem_Stromganglinie", "MENU_STROMGANGLINIE", Seitenschluessel.StromganglinieAdmin),
                new Menuepunkt("MenuItem_Stromspeicher", "MENU_STROMSPEICHER", Seitenschluessel.StromspeicherAdmin, katalog: true),
                new Menuepunkt("MenuItem_PeakShaving", "PEAK_MENUE", Seitenschluessel.PeakShaving),
            },
            new Menuepunkt("MenuItem_Energiesysteme", "MENU_ENERGIESYSTEME", "", bild: "Menu3")
            {
                // W16c-E-7: der Zwischenknoten. Er fasst die zwei Kataloge
                // EINER Anlage zusammen - das Modul und das Geraet dahinter -
                // und traegt wie die Rubrik "Profile & Lastgaenge" kein Bild
                // und kein Ziel.
                new Menuepunkt("MenuItem_PV_Gruppe", "MENU_PHOTOVOLTAIK", "")
                {
                    // W16c-E-6: aus dem Untermenue heraus. Es fuehrte nur
                    // "Bearbeiten" (MenuItem_PC_Bearbeiten); das Ziel ist
                    // unveraendert PvAdmin. W16c-E-7: die Beschriftung heisst
                    // "PV Module" (MENU_PV_MODULE) - "Photovoltaik" steht
                    // jetzt darueber.
                    new Menuepunkt("MenuItem_PV", "MENU_PV_MODULE", Seitenschluessel.PvAdmin, katalog: true),
                    // ANWENDERENTSCHEID W6-E-2 (06.09.2026), Stufe S1.4 des
                    // Konzept_Wechselrichter_EPOS-Plan.md: der Wechselrichterkatalog,
                    // NACH "PV Module" - er gehoert zur selben Anlage und
                    // wird nach dem Modul gepflegt.
                    new Menuepunkt("MenuItem_Wechselrichter", "MENU_WECHSELRICHTER", Seitenschluessel.WechselrichterAdmin, katalog: true),
                },
                // W16c-E-6: aus "Waermebedarf & Heizung" hierher.
                new Menuepunkt("MenuItem_PufferSp", "MENU_PUFFER_SP", Seitenschluessel.PufferSpAdmin, katalog: true),
            },
            // MN-1: Anlagen | Kosten
            Menuepunkt.Trennstrich("MenuItem_TrennerAdminAnlagen"),
            // MN-1 (19.09.2026): Die Rubrik "Kosten" bekommt ein Bild
            // (kosten_32) - sie war die EINZIGE Rubrik des Kopfes ohne eines.
            new Menuepunkt("MenuItem_KostenVerwaltung", "MENU_KOSTEN_VERWALTUNG", "", bild: "kosten_32")
            {
                new Menuepunkt("MenuItem_Kostenvorlagen", "KDLG_MENUE_VORLAGEN", Seitenschluessel.Kostenverwaltung),
                new Menuepunkt("MenuItem_Energietraeger", "KDLG_MENUE_ENERGIETRAEGER", Seitenschluessel.EnergietraegerVerwaltung),
                // ANWENDERENTSCHEID ND-Q3 (14.09.2026), Stufe S1 des Konzepts
                // "Nutzungsdauer je Technik und Positionsart": die
                // Nutzungsdauertabelle als DRITTER Eintrag der Rubrik, neben
                // Kostenvorlagen und Energietraegern. Sie gehoert hierher, weil
                // sie die Kostenpositionen vorbelegt - und nirgends sonst hin.
                new Menuepunkt("MenuItem_Nutzungsdauer", "ND_MENUE", Seitenschluessel.NutzungsdauerVerwaltung),
                // MN-1 (19.09.2026): aus der obersten Ebene des Kopfes hierher.
                // Die gesetzlichen Parameter (KWKG, EEG, Steuern, CO2-Preis,
                // Umsatzsteuer, Bilanzkonvention) werden AUSSCHLIESSLICH in der
                // Wirtschaftlichkeit gerechnet - sie sind Kostengroessen und
                // stehen deshalb bei den Kosten. Ihr Bild faellt dabei weg:
                // unter einer Rubrik traegt kein Blatt ein eigenes.
                new Menuepunkt("MenuItem_Gesetzesparameter", "GESETZ_MENUE", Seitenschluessel.Gesetzeskatalog),
            },
            // MN-1: Kosten | Daten & Import
            Menuepunkt.Trennstrich("MenuItem_TrennerAdminKosten"),
            new Menuepunkt("MenuItem_DatImport", "MENU_DAT_IMPORT", "", bild: "Menue5")
            {
                // MN-1 (19.09.2026): Die Importe stehen in der Reihenfolge der
                // KATALOGE darueber - Kessel, Waermepumpe, Solarthermie,
                // Pufferspeicher, Photovoltaik, Stromspeicher.
                new Menuepunkt("MenuItem_Import_Heizkessel", "MENU_IMPORT_HEIZKESSEL", Seitenschluessel.HeizkesselImport),
                new Menuepunkt("MeniItem_VDI3805", "MENU_VDI3805", Seitenschluessel.WpImport),
                new Menuepunkt("MenuItem_ST_Import", "MENU_ST_IMPORT", Seitenschluessel.SolarkollektorenImport),
                new Menuepunkt("MenuItem_PufferSp_VDI3805", "MENU_PUFFER_SP_VDI3805", Seitenschluessel.PufferSpImport),
                // W16c-E-7: derselbe Zwischenknoten wie unter
                // "Energiesysteme", an der Stelle der zwei PV-Importpunkte -
                // der Anwender hat ihn ausdruecklich HIER gemeint ("der Import
                // steht nicht unter Energiesysteme sondern vdi3805").
                new Menuepunkt("MenuItem_PV_Import_Gruppe", "MENU_PHOTOVOLTAIK", "")
                {
                    new Menuepunkt("MenuItem_PV_Import_CEC", "MENU_PV_IMPORT_CEC", Seitenschluessel.PvImport, argument: "CEC"),
                    // W6-E-2/S1.5 und W6-O-1: die CEC-Wechselrichterliste (Netz oder
                    // Auslieferungsdatei, W6-O-3) und PVsyst .OND - neben dem
                    // Modulimport, mit dem sie sich seit W6-O-1 EINEN Wirt teilt.
                    // OHNE Argument: Der eine Menuepunkt macht mit der Vorgabequelle
                    // (CEC) auf; die zwei anderen Quellen sind Knoepfe IN der Maske.
                    new Menuepunkt("MenuItem_WR_Import_CEC", "MENU_WR_IMPORT_CEC", Seitenschluessel.WechselrichterImport),
                },
                // ANWENDERWUNSCH W13-E-2 (07.09.2026), Stufe S1: der
                // Stromspeicherimport. Er stand als EINZIGER Katalog ohne
                // Einlesepunkt da; die Rubrik fuehrte sechs Importe und keinen
                // fuer Tab_Stromspeicher_STAMM. Seine Stelle ist HINTER dem
                // Knoten "Photovoltaik" - Stromspeicher und PV gehoeren zur
                // selben Anlage - und VOR den Solarkollektoren.
                new Menuepunkt("MenuItem_SP_Import", "MENU_SP_IMPORT", Seitenschluessel.StromspeicherImport),
                // MN-1: Importe | Pflege der eingelesenen Kataloge
                Menuepunkt.Trennstrich("MenuItem_TrennerImportDubletten"),
                // MN-1 (19.09.2026): aus der obersten Ebene des Kopfes hierher.
                // Dubletten entstehen BEIM Einlesen; die Pruefung gehoert ans
                // Ende derselben Rubrik. Ihr Bild hatte sie nie.
                new Menuepunkt("MenuItem_KatalogDubletten", "ADM_DUBLETTEN_MENUE", Seitenschluessel.KatalogDubletten),
            },
            // MN-1: Daten & Import | Einstellungen
            Menuepunkt.Trennstrich("MenuItem_TrennerAdminImport"),
            // MN-1 (19.09.2026): Die Einstellungen stehen ans ENDE - sie sind
            // kein Katalog, sondern das Programm selbst.
            new Menuepunkt("MenuItem_Einstellungen", "MENU_EINSTELLUNGEN", Seitenschluessel.Einstellungen, bild: "einstellungen_32"),
        },
        new Menuepunkt("Help", "MENU_HELP", "")
        {
            new Menuepunkt("MenuItem_Version", "MENU_VERSION", Seitenschluessel.Version),
            Menuepunkt.Trennstrich("toolStripSeparator7"),
            new Menuepunkt("MenuItem_Lizenz", "MENU_LIZENZ", Seitenschluessel.Lizenztext),
            new Menuepunkt("MenuItem_Dokumentation", "MENU_DOKUMENTATION", Seitenschluessel.Dokumentation),
            Menuepunkt.Trennstrich("MenuItem_TrennerKiHilfe"),
            new Menuepunkt("MenuItem_KiAssistent", "KI_MENUE_ASSISTENT", Seitenschluessel.KiAssistent, kuerzel: "F1"),
        },
        // ANWENDERENTSCHEID W16c-E-2 (04.09.2026): Die zwei Sprachpunkte standen
        // im Bestand als eigene Koepfe der obersten Ebene (menuToolbar.Items =
        // Projekt, Administration, Hilfe, Deutsch, Englisch). Sie sind jetzt die
        // Untereintraege EINES Kopfes "Sprache" an derselben Stelle - ganz
        // rechts, nach "Hilfe". Der Kopf klappt nur auf und traegt deshalb kein
        // Ziel; Namen, Bilder und Seitenschluessel der zwei Punkte sind
        // unveraendert, damit help_mapping.txt und HauptfensterHuelle.Weg
        // weiterhin greifen.
        // ANWENDERWUNSCH 05.09.2026 (W16c-E-4): Der Kopf steht GANZ RECHTS in
        // der Leiste - so wie im Bestand die zwei Sprachpunkte, die als letzte
        // Eintraege des MenuStrip rechtsbuendig am Rand sassen. Verschoben wird
        // nur die Optik (margin-left: auto im Band), nicht die Reihenfolge im
        // Markup: Tastaturweg und Nachweis N4 bleiben unveraendert.
        new Menuepunkt("Sprache", "MENU_SPRACHE", "", rechtsBuendig: true)
        {
            new Menuepunkt("Deutsch", "MENU_DEUTSCH", Seitenschluessel.SpracheDeutsch, bild: "germany"),
            new Menuepunkt("Englisch", "MENU_ENGLISCH", Seitenschluessel.SpracheEnglisch, bild: "usa"),
        },
    };

    /// <summary>
    /// Die KATALOGVERWALTUNGEN, die eine Plattform öffnen kann — in der Reihenfolge des
    /// Menübaums (Umsetzungskonzept Zapfprofilgenerator, Kapitel 9 ZU34).
    /// </summary>
    /// <param name="freigegeben">
    /// Beantwortet, ob die Plattform das Ziel eines Punktes öffnet. Die Wurzel reicht
    /// hier ihre Positivliste herein (<c>AppWurzel.FuehrtZiel</c>); die Tabelle kennt
    /// keine Plattform.
    /// </param>
    /// <returns>Die Punkte mit <see cref="Menuepunkt.Katalog"/>, deren Ziel freigegeben
    /// ist; ein Ziel, das an zwei Stellen steht, erscheint einmal.</returns>
    public static IReadOnlyList<Menuepunkt> Kataloge(Func<string, bool> freigegeben)
    {
        ArgumentNullException.ThrowIfNull(freigegeben);

        var liste = new List<Menuepunkt>();
        var gesehen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Menuepunkt p in Alle)
        {
            if (!p.Katalog || string.IsNullOrEmpty(p.Ziel)) continue;
            if (!freigegeben(p.Ziel) || !gesehen.Add(p.Ziel)) continue;
            liste.Add(p);
        }
        return liste;
    }

    /// <summary>Alle Punkte des Baums, Trennstriche eingeschlossen.</summary>
    public static IEnumerable<Menuepunkt> Alle => Flach(Eintraege);

    private static IEnumerable<Menuepunkt> Flach(IEnumerable<Menuepunkt> punkte)
    {
        foreach (Menuepunkt p in punkte)
        {
            yield return p;
            foreach (Menuepunkt k in Flach(p.Untereintraege)) yield return k;
        }
    }
}
