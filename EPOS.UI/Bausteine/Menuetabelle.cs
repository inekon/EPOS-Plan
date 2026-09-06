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

using System;
using System.Collections.Generic;
using EPOS.UI.Seiten;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Das Menue des Hauptfensters als DATEN (iU9-W16c.1).
///
/// <para><b>54 Punkte</b> - 45 aus dem Designer des Vorlaeufers und
/// 9, die dort programmatisch eingehaengt wurden ("damit Designer und
/// .resx unberuehrt bleiben", MDIMainForm.cs:57, :95, :132, :174, :311, :414,
/// :531). Der Grund dafuer entfaellt mit dem Designer; hier sind es
/// gleichrangige Zeilen. Dazu kommen die zwei Punkte OHNE Designer-Herkunft:
/// der Kopf "Sprache" (W16c-E-2) und die Unterrubrik "Profile & Lastgaenge"
/// (W16c-E-6); dafuer fallen mit W16c-E-6 die zwei Ein-Punkt-Untermenues
/// MenuItem_PC_Bearbeiten und MenuItem_ST_Bearbeiten weg. Also
/// 54 Bestandspunkte + 2 - 2 = 54, dazu 8 Trennstriche.</para>
///
/// <para><b>Vier Koepfe</b> in der obersten Ebene: Projekt, Administration,
/// Hilfe und - ganz rechts, wo bis W16c-E-2 "Deutsch" stand - Sprache. Alle
/// vier klappen nur auf; von den 54 Punkten handeln <b>42</b>, 12 klappen auf.
/// Die Zahl der HANDELNDEN Punkte ist mit W16c-E-6 unveraendert geblieben: Es
/// ist kein Ziel entfallen und keines hinzugekommen, es steht nur an einer
/// anderen Stelle des Baumes.</para>
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
        },
        new Menuepunkt("Administration", "MENU_ADMINISTRATION", "")
        {
            new Menuepunkt("MenuItem_WBundHeizung", "MENU_WBUND_HEIZUNG", "", bild: "Menu1")
            {
                new Menuepunkt("MenuItem_Brauchwasser", "MENU_BRAUCHWASSER", Seitenschluessel.BrauchwasserAdmin),
                new Menuepunkt("MenuItem_Kessel", "MENU_KESSEL", Seitenschluessel.HeizkesselAdmin),
                new Menuepunkt("MenuItem_WP", "MENU_WP", Seitenschluessel.WpAdministration),
                // W16c-E-6: aus "Energiesysteme" hierher.
                new Menuepunkt("MenuItem_BHKW", "MENU_BHKW", Seitenschluessel.BhkwAdmin),
                // W16c-E-6: aus "Energiesysteme" hierher - und dabei aus seinem
                // Untermenue heraus. Es fuehrte nur "Bearbeiten"
                // (MenuItem_ST_Bearbeiten); das Ziel ist unveraendert
                // SolarkollektorenAdmin.
                new Menuepunkt("MenuItem_Solarkollektoren", "MENU_SOLARKOLLEKTOREN", Seitenschluessel.SolarkollektorenAdmin),
                // W16c-E-6: die neue Unterrubrik. Sie ist die zweite Zeile ohne
                // Designer-Herkunft (nach dem Kopf "Sprache") und traegt darum
                // KEIN Bild; ihre drei Punkte kommen unveraendert aus
                // "Waermebedarf & Heizung" und aus "Energiesysteme".
                new Menuepunkt("MenuItem_ProfileLastgaenge", "MENU_PROFILE_LASTGAENGE", "")
                {
                    new Menuepunkt("MenuItem_WaermebedarfExtern", "MENU_WAERMEBEDARF_EXTERN", Seitenschluessel.WaermebedarfExternAdmin),
                    new Menuepunkt("MenuItem_Prozesswaerme", "MENU_PROZESSWAERME", Seitenschluessel.ProzesswaermeAdmin),
                    new Menuepunkt("MenuItem_SolThermGanglinie", "MENU_SOL_THERM_GANGLINIE", Seitenschluessel.SolarganglinieAdmin),
                },
            },
            new Menuepunkt("MenuItem_StromBedarfundSp", "MENU_STROM_BEDARFUND_SP", "", bild: "Menue2")
            {
                new Menuepunkt("MenuItem_Stromverbraucher", "MENU_STROMVERBRAUCHER", Seitenschluessel.StromverbraucherAdmin),
                new Menuepunkt("MenuItem_Stromganglinie", "MENU_STROMGANGLINIE", Seitenschluessel.StromganglinieAdmin),
                new Menuepunkt("MenuItem_Stromspeicher", "MENU_STROMSPEICHER", Seitenschluessel.StromspeicherAdmin),
                new Menuepunkt("MenuItem_PeakShaving", "PEAK_MENUE", Seitenschluessel.PeakShaving),
            },
            new Menuepunkt("MenuItem_Energiesysteme", "MENU_ENERGIESYSTEME", "", bild: "Menu3")
            {
                // W16c-E-6: aus dem Untermenue heraus. Es fuehrte nur
                // "Bearbeiten" (MenuItem_PC_Bearbeiten); das Ziel ist
                // unveraendert PvAdmin.
                new Menuepunkt("MenuItem_PV", "MENU_PV", Seitenschluessel.PvAdmin),
                // ANWENDERENTSCHEID W6-E-2 (06.09.2026), Stufe S1.4 des
                // Konzept_Wechselrichter_EPOS-Plan.md: der Wechselrichterkatalog,
                // NACH "Photovoltaik Module" - er gehoert zur selben Anlage und
                // wird nach dem Modul gepflegt.
                new Menuepunkt("MenuItem_Wechselrichter", "MENU_WECHSELRICHTER", Seitenschluessel.WechselrichterAdmin),
                // W16c-E-6: aus "Waermebedarf & Heizung" hierher.
                new Menuepunkt("MenuItem_PufferSp", "MENU_PUFFER_SP", Seitenschluessel.PufferSpAdmin),
            },
            new Menuepunkt("MenuItem_Klima", "MENU_KLIMA", "", bild: "Menu4")
            {
                new Menuepunkt("MenuItem_Klimadaten", "MENU_KLIMADATEN", Seitenschluessel.Klimadaten),
            },
            new Menuepunkt("MenuItem_DatImport", "MENU_DAT_IMPORT", "", bild: "Menue5")
            {
                new Menuepunkt("MenuItem_Import_Heizkessel", "MENU_IMPORT_HEIZKESSEL", Seitenschluessel.HeizkesselImport),
                new Menuepunkt("MenuItem_PufferSp_VDI3805", "MENU_PUFFER_SP_VDI3805", Seitenschluessel.PufferSpImport),
                new Menuepunkt("MeniItem_VDI3805", "MENU_VDI3805", Seitenschluessel.WpImport),
                new Menuepunkt("MenuItem_PV_Import_CEC", "MENU_PV_IMPORT_CEC", Seitenschluessel.PvImport, argument: "CEC"),
                // W6-E-2/S1.5 und W6-O-1: die CEC-Wechselrichterliste (Netz oder
                // Auslieferungsdatei, W6-O-3) und PVsyst .OND - neben dem
                // Modulimport, mit dem sie sich seit W6-O-1 EINEN Wirt teilt.
                // OHNE Argument: Der eine Menuepunkt macht mit der Vorgabequelle
                // (CEC) auf; die zwei anderen Quellen sind Knoepfe IN der Maske.
                new Menuepunkt("MenuItem_WR_Import_CEC", "MENU_WR_IMPORT_CEC", Seitenschluessel.WechselrichterImport),
                new Menuepunkt("MenuItem_ST_Import", "MENU_ST_IMPORT", Seitenschluessel.SolarkollektorenImport),
            },
            new Menuepunkt("MenuItem_KostenVerwaltung", "MENU_KOSTEN_VERWALTUNG", "")
            {
                new Menuepunkt("MenuItem_Kostenvorlagen", "KDLG_MENUE_VORLAGEN", Seitenschluessel.Kostenverwaltung),
                new Menuepunkt("MenuItem_Energietraeger", "KDLG_MENUE_ENERGIETRAEGER", Seitenschluessel.EnergietraegerVerwaltung),
            },
            new Menuepunkt("MenuItem_Gebaeude", "MENU_GEBAEUDE", "", bild: "Menue6")
            {
                new Menuepunkt("MenuItem_GebBearbeiten", "MENU_GEB_BEARBEITEN", Seitenschluessel.GebaeudeAdmin),
                new Menuepunkt("MenuItem_GebTypen", "MENU_GEB_TYPEN", Seitenschluessel.GebaeudetypenAdmin),
            },
            new Menuepunkt("MenuItem_Einstellungen", "MENU_EINSTELLUNGEN", Seitenschluessel.Einstellungen, bild: "einstellungen_32"),
            new Menuepunkt("MenuItem_Gesetzesparameter", "GESETZ_MENUE", Seitenschluessel.Gesetzeskatalog, bild: "gesetzliche_parameter_32"),
            new Menuepunkt("MenuItem_KatalogDubletten", "ADM_DUBLETTEN_MENUE", Seitenschluessel.KatalogDubletten),
            new Menuepunkt("MenuItem_LizenzVerwaltung", "MENU_LIZENZ_VERWALTUNG", Seitenschluessel.LizenzVerwaltung, bild: "lizenzen_32"),
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
