// ERZEUGT — nicht von Hand bearbeiten.
//
// Quelle: WindowsFormsApplication1/MDIMainForm.Designer.cs (45 ToolStripMenuItem,
// 6 ToolStripSeparator) und MDIMainForm.cs (die neun Punkte der acht Init*-
// Methoden), dazu MDIMainForm.resx / .de-DE.resx / .en-US.resx.
// Erzeuger: das Skript w16c_menue.py der Teilwelle iU9-W16c (Auflage R-W16-8:
// "Die Menuetabelle wird per Skript erzeugt, nicht abgetippt").
//
// Wer das Menue aendert, aendert die TABELLE - der Designer ist mit W16c.3
// geloescht.
//
// EINE ZEILE STAMMT NICHT VOM ERZEUGER: der Kopf "Sprache" (MENU_SPRACHE). Er
// ist der ANWENDERENTSCHEID W16c-E-2 vom 04.09.2026 - die zwei Sprachpunkte
// standen im Bestand als Koepfe der obersten Ebene und sind seither seine
// Untereintraege. Ihre Namen, Bilder und Seitenschluessel sind unveraendert;
// der Kopf hat KEINE Designer-Herkunft und kein Ziel. Das Erzeugerskript
// w16c_menue.py liegt nicht im Repository - wer es je neu laufen laesst, traegt
// diesen Kopf von Hand nach.

using System;
using System.Collections.Generic;
using EPOS.UI.Seiten;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Das Menue des Hauptfensters als DATEN (iU9-W16c.1).
///
/// <para><b>55 Punkte</b> - 45 aus dem Designer des Vorlaeufers und
/// 9, die dort programmatisch eingehaengt wurden ("damit Designer und
/// .resx unberuehrt bleiben", MDIMainForm.cs:57, :95, :132, :174, :311, :414,
/// :531). Der Grund dafuer entfaellt mit dem Designer; hier sind es
/// gleichrangige Zeilen. Dazu kommt der Kopf "Sprache" aus dem
/// Anwenderentscheid W16c-E-2 (04.09.2026), unter dem die zwei Sprachpunkte
/// haengen: zusammen 55 Punkte und 8 Trennstriche.</para>
///
/// <para><b>Vier Koepfe</b> in der obersten Ebene: Projekt, Administration,
/// Hilfe und - ganz rechts, wo bis W16c-E-2 "Deutsch" stand - Sprache. Alle
/// vier klappen nur auf; von den 55 Punkten handeln 42, 13 klappen auf.</para>
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
                new Menuepunkt("MenuItem_Prozesswaerme", "MENU_PROZESSWAERME", Seitenschluessel.ProzesswaermeAdmin),
                new Menuepunkt("MenuItem_PufferSp", "MENU_PUFFER_SP", Seitenschluessel.PufferSpAdmin),
                new Menuepunkt("MenuItem_WaermebedarfExtern", "MENU_WAERMEBEDARF_EXTERN", Seitenschluessel.WaermebedarfExternAdmin),
                new Menuepunkt("MenuItem_WP", "MENU_WP", Seitenschluessel.WpAdministration),
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
                new Menuepunkt("MenuItem_PV", "MENU_PV", "")
                {
                    new Menuepunkt("MenuItem_PC_Bearbeiten", "MENU_PC_BEARBEITEN", Seitenschluessel.PvAdmin),
                },
                new Menuepunkt("MenuItem_Solarkollektoren", "MENU_SOLARKOLLEKTOREN", "")
                {
                    new Menuepunkt("MenuItem_ST_Bearbeiten", "MENU_ST_BEARBEITEN", Seitenschluessel.SolarkollektorenAdmin),
                },
                new Menuepunkt("MenuItem_SolThermGanglinie", "MENU_SOL_THERM_GANGLINIE", Seitenschluessel.SolarganglinieAdmin),
                new Menuepunkt("MenuItem_BHKW", "MENU_BHKW", Seitenschluessel.BhkwAdmin),
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
