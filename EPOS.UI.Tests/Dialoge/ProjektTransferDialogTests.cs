using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Projekttransfer (iU9-W15a.5) — Soll ist die von Hand geschriebene Feldkarte von
/// <c>Form_ProjektExportImport</c> (23 Steuerelemente aus <c>BaueUi():46-126</c>;
/// die Maske hatte keinen Designer, Befund W15a-B24).
///
/// <para>Geprueft werden die zwei Blaetter, die Variantenhaken „alle vorbelegt an"
/// (TF2), die drei Konfliktmodi samt Vorbelegung, die Paketvorschau aus dem
/// Manifest, die zwei Rueckfragen mit Vorgabe „Nein" und die Regel „kein Delegat =
/// kein Knopf" fuer die Sicherungskopie (A-10).</para>
/// </summary>
public class ProjektTransferDialogTests : BunitContext
{
    private static readonly ProjektKopfZeile[] ZWEI =
    {
        new ProjektKopfZeile(1019, "Wöhler"),
        new ProjektKopfZeile(1030, "Referenz BHKW")
    };

    public ProjektTransferDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    /// <summary>Der Kern-Ersatz: er merkt sich, womit er gerufen wurde.</summary>
    private sealed class Kern
    {
        public string ExportProjekt = "", ExportZiel = "";
        public IReadOnlyList<string> ExportVarianten = Array.Empty<string>();
        public bool ExportErgebnis = true;

        public string ImportPfad = "", ImportName = "";
        public ProjektExportImportCtrl.BeiVorhandenem ImportModus;
        public int ImportId = 4711;
        public string ImportFehler = "";
        public bool ImportGerufen;

        public bool SicherungGerufen;
        public bool SicherungWirft;
        public string BerichtPfad = "", BerichtText = "";

        public bool Exportieren(string projekt, IReadOnlyList<string> varianten, string ziel,
                                IProgress<string>? melder)
        {
            ExportProjekt = projekt; ExportVarianten = varianten; ExportZiel = ziel;
            melder?.Report("… Tab_Projekt");
            return ExportErgebnis;
        }

        public ImportErgebnis Importieren(string pfad, string name,
                                          ProjektExportImportCtrl.BeiVorhandenem modus,
                                          IProgress<string>? melder)
        {
            ImportGerufen = true;
            ImportPfad = pfad; ImportName = name; ImportModus = modus;
            melder?.Report("… Tab_Projekt");
            return new ImportErgebnis(ImportId, name,
                                      new[] { "Zeile eins", "Zeile zwei" }, ImportFehler);
        }

        public string Sicherung()
        {
            SicherungGerufen = true;
            if (SicherungWirft) throw new InvalidOperationException("Platte voll");
            return "Kenndaten_vor_Import_20260904_120000.sqlite";
        }

        public string? Bericht(string paket, string text)
        {
            BerichtPfad = paket; BerichtText = text;
            return "bericht.txt";
        }
    }

    private static ProjektTransferDaten Daten(Kern kern, bool mitSicherung = true,
                                              PaketVorschau? vorschau = null,
                                              string? paket = "C:\\pakete\\projekt.wpx")
        => new ProjektTransferDaten(
            Projekte: ZWEI,
            Varianten: p => p == "Wöhler" ? new[] { "Wöhler - Test1", "Wöhler - Test2" } : Array.Empty<string>(),
            Exportieren: kern.Exportieren,
            PaketLesen: () => paket,
            PaketSchreiben: v => "C:\\ziel\\" + v,
            Vorschau: _ => vorschau ?? new PaketVorschau("Wöhler", "04.09.2026 12:00", 61,
                                                          new[] { "Test1", "Test2" }, ""),
            Importieren: kern.Importieren,
            SicherungAnlegen: mitSicherung ? kern.Sicherung : null,
            BerichtSchreiben: kern.Bericht);

    private IRenderedComponent<ProjektTransferDialog> Aufbauen(
        Kern kern, Action<ComponentParameterCollectionBuilder<ProjektTransferDialog>>? mehr = null)
        => Render<ProjektTransferDialog>(p =>
        {
            p.Add(x => x.Daten, Daten(kern));
            mehr?.Invoke(p);
        });

    private static void ZumImport(IRenderedComponent<ProjektTransferDialog> cut)
        => cut.FindAll(".epos-reiter-knopf")[1].Click();

    /// <summary>
    /// Wartet auf den GEZEICHNETEN Abschluss eines gelungenen Imports — das
    /// Berichtsfeld mit den Zeilen der Attrappe.
    ///
    /// <para><b>W16b-O-2: nicht auf das Kennzeichen der Attrappe warten.</b>
    /// <c>kern.ImportGerufen</c> faellt auf dem Faden des <c>Task.Run</c> im Dialog,
    /// also SCHON BEIM BETRETEN des Imports. In diesem Augenblick steht der Dialog
    /// noch auf „laeuft": <c>_importErfolgreich</c> ist nicht gesetzt, der Bericht
    /// nicht gefuellt, nichts davon gezeichnet — und <c>Schliessen()</c> meldet
    /// waehrend eines laufenden Imports gar nichts. Wer auf das Kennzeichen wartet
    /// und sofort weiterklickt, gewinnt einen Wettlauf oder verliert ihn: Ob die
    /// erste Pruefung von <c>WaitForAssertion</c> das Kennzeichen schon sieht,
    /// entscheidet der Faden, nicht der Dialog. Verloren ging er im Windows-Lauf
    /// 34017401022 („Expected True, Actual False" beim Ergebnis des Schliessens).
    /// Das Berichtsfeld dagegen entsteht erst hinter dem <c>await</c> — es belegt,
    /// dass der Dialog fertig ist, nicht nur die Attrappe.</para>
    /// </summary>
    private static void ImportFertig(IRenderedComponent<ProjektTransferDialog> cut)
        => cut.WaitForAssertion(() =>
               Assert.Contains("Zeile eins", cut.Find(".epos-projekttransfer textarea").TextContent));

    [Fact]
    public void Zwei_Blaetter_Exportieren_und_Importieren()
    {
        var cut = Aufbauen(new Kern());

        var reiter = cut.FindAll(".epos-reiter-knopf");
        Assert.Equal(2, reiter.Count);
        Assert.Contains("Exportieren", reiter[0].TextContent);
        Assert.Contains("Importieren", reiter[1].TextContent);

        // Ein ungewaehltes Blatt wird gar nicht gezeichnet (Baustein Reiter).
        Assert.Empty(cut.FindAll(".epos-transfer-import"));
        Assert.Single(cut.FindAll(".epos-transfer-export"));
    }

    [Fact]
    public void Die_Varianten_des_gewaehlten_Projekts_sind_alle_vorbelegt()
    {
        // TF2: clbVarianten.Items.Add(name, true) des Vorlaeufers.
        var cut = Aufbauen(new Kern());

        Assert.Equal(new[] { "Wöhler - Test1", "Wöhler - Test2" }, cut.Instance.GewaehlteVarianten);
        Assert.Equal(2, cut.FindAll(".epos-mehrfachauswahl input[type=checkbox]:checked").Count);
    }

    [Fact]
    public void Ein_Projektwechsel_laedt_die_Variantenliste_neu()
    {
        var cut = Aufbauen(new Kern());

        cut.Find(".epos-projekttransfer select").Change("1");   // Referenz BHKW

        Assert.Empty(cut.Instance.GewaehlteVarianten);
        Assert.Equal("Referenz BHKW", cut.Instance.Projekt);
    }

    [Fact]
    public void Der_Export_reicht_Projekt_Varianten_und_Zielpfad_durch()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);

        cut.Find(".epos-transfer-export").Click();

        // Der Lauf liegt in Task.Run - auf sein Ende wird gewartet, nicht geraten.
        cut.WaitForAssertion(() =>
            Assert.Contains("Export abgeschlossen.", cut.Find(".epos-warnbanner").TextContent));
        Assert.Equal("Wöhler", kern.ExportProjekt);
        Assert.Equal(new[] { "Wöhler - Test1", "Wöhler - Test2" }, kern.ExportVarianten);
        Assert.Equal("C:\\ziel\\Wöhler.wpx", kern.ExportZiel);
    }

    [Fact]
    public void Ein_gescheiterter_Export_meldet()
    {
        var kern = new Kern { ExportErgebnis = false };
        var cut = Aufbauen(kern);

        cut.Find(".epos-transfer-export").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Export fehlgeschlagen.", cut.Find(".epos-warnbanner").TextContent));
    }

    [Fact]
    public void Die_Paketvorschau_liest_Quellprojekt_Datum_Schemastand_und_Varianten()
    {
        var cut = Aufbauen(new Kern());
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();

        string text = cut.Find(".epos-warnbanner").TextContent;
        Assert.Contains("Wöhler", text);
        Assert.Contains("61", text);
        Assert.Contains("Test1, Test2", text);

        // Der Zielname wird mit dem Quellprojekt vorbelegt, solange er leer ist.
        Assert.Equal("Wöhler", cut.FindAll(".epos-projekttransfer input[type=text]")
                                  .Last().GetAttribute("value"));
    }

    [Fact]
    public void Ein_unlesbares_Paket_meldet_statt_zu_importieren()
    {
        var kern = new Kern();
        var cut = Render<ProjektTransferDialog>(p => p.Add(x => x.Daten,
            Daten(kern, vorschau: new PaketVorschau("", "", 0, Array.Empty<string>(),
                                                    "Kein gültiges Paket (manifest.json fehlt)."))));
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();

        Assert.Contains("Kein gültiges Paket", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Die_drei_Konfliktmodi_stehen_mit_neuem_Namen_als_Vorbelegung()
    {
        var cut = Aufbauen(new Kern());
        ZumImport(cut);

        var optionen = cut.FindAll(".epos-optionsgruppe input[type=radio]");
        Assert.Equal(3, optionen.Count);
        Assert.True(((AngleSharp.Html.Dom.IHtmlInputElement)optionen[0]).IsChecked);
    }

    [Fact]
    public void Der_Import_legt_erst_die_Sicherung_an_und_laeuft_dann()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();
        cut.Find(".epos-transfer-import").Click();

        ImportFertig(cut);
        Assert.True(kern.SicherungGerufen);
        Assert.True(kern.ImportGerufen);
        Assert.Equal("C:\\pakete\\projekt.wpx", kern.ImportPfad);
        Assert.Equal(ProjektExportImportCtrl.BeiVorhandenem.NeuerName, kern.ImportModus);
        Assert.Equal("C:\\pakete\\projekt.wpx", kern.BerichtPfad);
    }

    [Fact]
    public void Ueberschreiben_fragt_zuerst_und_Nein_bricht_ab()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();
        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);   // Überschreiben
        cut.Find(".epos-transfer-import").Click();

        var frage = cut.Find(".epos-rueckfrage-text");
        Assert.Contains("unwiderruflich überschrieben", frage.TextContent);
        Assert.False(kern.ImportGerufen);

        // Vorgabe „Nein": der zweite Knopf ist der hervorgehobene.
        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassList);

        knoepfe[1].Click();                                                     // Nein
        Assert.False(kern.ImportGerufen);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Ueberschreiben_mit_Ja_importiert()
    {
        var kern = new Kern();
        var cut = Aufbauen(kern);
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();
        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);
        cut.Find(".epos-transfer-import").Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();          // Ja

        ImportFertig(cut);
        Assert.True(kern.ImportGerufen);
        Assert.Equal(ProjektExportImportCtrl.BeiVorhandenem.Ueberschreiben, kern.ImportModus);
    }

    [Fact]
    public void Eine_gescheiterte_Sicherung_fragt_Trotzdem_importieren()
    {
        var kern = new Kern { SicherungWirft = true };
        var cut = Aufbauen(kern);
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();
        cut.Find(".epos-transfer-import").Click();

        Assert.Contains("Platte voll", cut.Find(".epos-rueckfrage-text").TextContent);
        Assert.False(kern.ImportGerufen);

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();          // Ja
        ImportFertig(cut);
        Assert.True(kern.ImportGerufen);
    }

    [Fact]
    public void Ohne_Sicherungsdelegat_erscheint_kein_Schalter()
    {
        // Hausregel A-18 / A-10: auf iOS gibt es die 77-MB-Kopie nicht.
        var kern = new Kern();
        var cut = Render<ProjektTransferDialog>(p => p.Add(x => x.Daten, Daten(kern, mitSicherung: false)));
        ZumImport(cut);

        Assert.Empty(cut.FindAll(".epos-schalter"));

        cut.Find(".epos-dateiwahl button").Click();
        cut.Find(".epos-transfer-import").Click();

        ImportFertig(cut);
        Assert.True(kern.ImportGerufen);
        Assert.False(kern.SicherungGerufen);
    }

    [Fact]
    public void Ein_gescheiterter_Import_meldet_den_Grund()
    {
        var kern = new Kern { ImportId = -1, ImportFehler = "Schemastand 60" };
        var cut = Aufbauen(kern);
        ZumImport(cut);

        cut.Find(".epos-dateiwahl button").Click();
        cut.Find(".epos-transfer-import").Click();

        // Die STATUSZEILE ist der Warnbanner unmittelbar unter dem Reiterwerk; der
        // erste .epos-warnbanner der Maske ist die Paketvorschau IM Reiterblatt.
        cut.WaitForAssertion(() =>
            Assert.Contains("Schemastand 60",
                            cut.Find(".epos-dialog > .epos-warnbanner").TextContent));
        Assert.Empty(cut.FindAll(".epos-projekttransfer textarea"));
    }

    [Fact]
    public void Schliessen_meldet_ob_ein_Import_gelungen_ist()
    {
        var kern = new Kern();
        bool? ergebnis = null;
        var cut = Aufbauen(kern, p => p.Add(x => x.Geschlossen, (bool b) => ergebnis = b));

        // W16b-O-2, zweiter Wettlauf: bunits SYNCHRONES Click() gibt das Ereignis
        // nur beim Zeichner ab, es wartet nicht auf den Ereignisbehandler (nur
        // ClickAsync tut das). Der Rueckruf kann deshalb erst nach der Rueckkehr
        // aus Click() fallen — gemessen in 5 von 12 Laeufen. Also auch hier
        // warten statt raten.
        cut.FindAll(".epos-dialog > .epos-leiste .epos-knopf--primaer")[0].Click();
        cut.WaitForAssertion(() => Assert.False(ergebnis));

        cut = Aufbauen(kern, p => p.Add(x => x.Geschlossen, (bool b) => ergebnis = b));
        ZumImport(cut);
        cut.Find(".epos-dateiwahl button").Click();
        cut.Find(".epos-transfer-import").Click();

        // W16b-O-2, erster Wettlauf: erst der GEZEICHNETE Abschluss, dann der
        // Klick. Auf das Kennzeichen der Attrappe zu warten hiesse, waehrend des
        // laufenden Imports zu schliessen — und ein laufender Import meldet gar
        // nichts (ProjektTransferDialog.Schliessen: „if (_laeuft) return").
        ImportFertig(cut);
        cut.FindAll(".epos-dialog > .epos-leiste .epos-knopf--primaer")[0].Click();
        cut.WaitForAssertion(() => Assert.True(ergebnis));
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Export- und Importreiter stellen ihre Felder in den Formularraster, einspaltig: Der Export traegt eine Reihenfolge, der Import ein Pfadfeld.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Die_Felder_stehen_im_einspaltigen_Formularraster()
    {
        var cut = Aufbauen(new Kern());

        Assert.NotEmpty(cut.FindAll(".epos-formularraster--einspaltig"));
        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld"));

        ZumImport(cut);

        Assert.Equal(2, cut.FindAll(".epos-formularraster--einspaltig").Count);
    }
}
