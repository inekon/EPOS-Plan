using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <c>Formularraster</c> samt <c>Formulargruppe</c> — Anwenderwunsch
/// <b>iU8‑E‑2 / W14a‑E‑7</b> vom 05.09.2026: „Verbessere die Darstellung der
/// Dialoge, insbesondere der Parameter auf der rechten Seite: kompakter,
/// übersichtlicher, …".
///
/// <para><b>Der Befund</b> (Bildschirmfoto „Administration Photovoltaik
/// Module"): Im <c>Katalograhmen</c> stand rechts der Block „Moduldaten" —
/// Bezeichner, Hersteller, Beschreibung, Nennleistung, Wirkungsgrad, Spannung
/// im MPP, Leerlaufspannung … —, und JEDES Feld nahm die volle Breite, die
/// Beschriftung stand DARÜBER, die Zahlenfelder waren so breit wie die
/// Textfelder, und die Einheit stand am rechten Rand des Blocks statt hinter
/// dem Wert. Der Block war doppelt so hoch wie der Dialog und rollte.</para>
///
/// <para><b>Die Vorbilder, gemessen</b> (<c>Form_AdminPV.resx</c>, 607 × 489):
/// Beschriftung bei x = 253, Feld bei x = 431 — also eine Beschriftungsspalte
/// von 178 px; Zahlenfeld 62 px breit, die Einheit bei x = 497, mithin 4 px
/// dahinter; Textfeld 250 px, Beschreibung 250 × 48. Dieselbe Anordnung in
/// <c>Form_Heizkessel_Admin</c> (726 × 383), <c>Form_BHKWAdmin</c> (856 × 517)
/// und <c>Form_AdminStromspeicher</c> (614 × 367).</para>
///
/// <para><b>Zweierlei wird geprüft</b> (Lehre W6‑B‑1): das MARKUP über bunit
/// und die REGEL im Stilblatt — eine bunit-Probe rechnet kein CSS aus. Denselben
/// Weg gehen <c>KatalograhmenTests</c>, <c>ListenrahmenTests</c> und
/// <c>ZweispaltenauswahlTests</c>.</para>
///
/// <para>Keine Sprachbindung: geprüft werden Klassennamen. Die Kultur wird
/// trotzdem gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class FormularrasterTests : BunitContext
{
    public FormularrasterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    // =====================================================================
    //  Das Markup
    // =====================================================================

    /// <summary>Der Raster ist ein Kasten mit der Hausklasse — und sonst nichts.</summary>
    [Fact]
    public void Der_Raster_traegt_seine_Klasse_und_zeigt_seinen_Inhalt()
    {
        var cut = Render<Formularraster>(p => p
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"drin\">Feld</p>"))));

        var raster = cut.Find(".epos-formularraster");

        Assert.NotNull(raster.QuerySelector("#drin"));
        Assert.DoesNotContain("epos-formularraster--einspaltig", raster.ClassList);
    }

    /// <summary>Einspaltig ist eine Zusatzklasse, kein anderer Baustein.</summary>
    [Fact]
    public void Einspaltig_haengt_nur_eine_Klasse_an()
    {
        var cut = Render<Formularraster>(p => p
            .Add(x => x.Einspaltig, true)
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Feld</p>"))));

        Assert.Contains("epos-formularraster--einspaltig",
                        cut.Find(".epos-formularraster").ClassList);
    }

    /// <summary>
    /// Eine Gruppe zeichnet ihre leise Zwischenüberschrift — und die Felder
    /// bleiben ihre Geschwister im Markup, nicht Kinder eines Unterkastens.
    /// </summary>
    [Fact]
    public void Eine_Gruppe_zeichnet_ihren_Titel_ueber_ihren_Feldern()
    {
        var cut = Render<Formulargruppe>(p => p
            .Add(x => x.Titel, "Elektrik")
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"f\">Feld</p>"))));

        var gruppe = cut.Find(".epos-formulargruppe");

        Assert.Equal("Elektrik", gruppe.QuerySelector(".epos-formulargruppe-titel")!.TextContent);
        Assert.NotNull(gruppe.QuerySelector("#f"));
    }

    /// <summary>
    /// Ohne Titel zeichnet die Gruppe NICHTS Eigenes — die erste Gruppe eines
    /// Blocks steht üblicherweise so, ihr Name hängt schon im Gruppenkopf.
    /// </summary>
    [Fact]
    public void Eine_Gruppe_ohne_Titel_zeichnet_keine_Ueberschrift()
    {
        var cut = Render<Formulargruppe>(p => p
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Feld</p>"))));

        Assert.Empty(cut.FindAll(".epos-formulargruppe-titel"));
    }

    // =====================================================================
    //  Die Felder melden ihre Länge selbst
    // =====================================================================

    /// <summary>
    /// Ein Zahlenfeld trägt <c>epos-feld--kurz</c>; im Raster wird daraus die
    /// kurze Breite mit der Einheit unmittelbar dahinter (Vorbild: 62 px, die
    /// Einheit 4 px danach).
    /// </summary>
    [Fact]
    public void Ein_Zahlenfeld_meldet_sich_als_kurzes_Feld()
    {
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Bezeichnung, "Nennleistung (Pmax)")
            .Add(x => x.Einheit, "W"));

        var feld = cut.Find("label.epos-feld");

        Assert.Contains("epos-feld--kurz", feld.ClassList);

        // Die Einheit steht IN der Feldzeile, also direkt hinter dem Eingabefeld -
        // nicht als eigener Kasten am Rand des Blocks.
        var zeile = feld.QuerySelector(".epos-feld-zeile")!;
        Assert.Equal("W", zeile.QuerySelector(".epos-einheit")!.TextContent);
    }

    /// <summary>Dasselbe für das Ganzzahlfeld.</summary>
    [Fact]
    public void Ein_Ganzzahlfeld_meldet_sich_als_kurzes_Feld()
    {
        var cut = Render<Ganzzahlfeld>(p => p.Add(x => x.Bezeichnung, "Volumen"));

        Assert.Contains("epos-feld--kurz", cut.Find("label.epos-feld").ClassList);
    }

    /// <summary>
    /// Ein DATUM ist NICHT kurz: <c>&lt;input type="date"&gt;</c> zeichnet
    /// TT.MM.JJJJ und das Kalendersymbol des Browsers, und unter rund 130 px
    /// schneidet Chromium das Symbol ab.
    /// </summary>
    [Fact]
    public void Ein_Datumsfeld_ist_nicht_kurz()
    {
        var cut = Render<Datumsfeld>(p => p.Add(x => x.Bezeichnung, "Stichtag"));

        Assert.DoesNotContain("epos-feld--kurz", cut.Find("label.epos-feld").ClassList);
    }

    /// <summary>
    /// Ein MEHRZEILIGES Textfeld meldet sich als lang und spannt im Raster über
    /// beide Spalten — im Vorbild war die Beschreibung 250 × 48 px.
    /// </summary>
    [Fact]
    public void Ein_mehrzeiliges_Textfeld_meldet_sich_als_breites_Feld()
    {
        var cut = Render<Textfeld>(p => p
            .Add(x => x.Bezeichnung, "Beschreibung")
            .Add(x => x.Mehrzeilig, true)
            .Add(x => x.Zeilen, 2));

        Assert.Contains("epos-feld--breit", cut.Find("label.epos-feld").ClassList);
    }

    /// <summary>Ein EINZEILIGES Textfeld nimmt die Feldspalte, mehr nicht.</summary>
    [Fact]
    public void Ein_einzeiliges_Textfeld_ist_weder_kurz_noch_breit()
    {
        var cut = Render<Textfeld>(p => p.Add(x => x.Bezeichnung, "Bezeichner"));

        var feld = cut.Find("label.epos-feld");

        Assert.DoesNotContain("epos-feld--kurz", feld.ClassList);
        Assert.DoesNotContain("epos-feld--breit", feld.ClassList);
    }

    // =====================================================================
    //  Das Stilblatt — eine bunit-Probe sieht eine Regel nicht (W6-B-1)
    // =====================================================================

    /// <summary>
    /// Die BESCHRIFTUNGSSPALTE: Im Raster ist ein Feld ein zweispaltiges Raster
    /// aus fester Beschriftungsspalte und Feldspalte — die Beschriftung steht
    /// NEBEN dem Feld, nicht darüber. Das war der Kern des Befunds.
    /// </summary>
    [Fact]
    public void Im_Raster_steht_die_Beschriftung_neben_dem_Feld()
    {
        string block = Stilblock(".epos-formularraster .epos-feld {");

        Assert.Contains("display: grid", block, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: var(--epos-beschriftung-breite) minmax(0, 1fr)",
                        block, StringComparison.Ordinal);

        // Und das Token steht als Token in :root, nicht als Rueckfall in der Regel.
        Assert.Contains("--epos-beschriftung-breite: 12rem", Stilblock(":root {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Das KURZE FELD: Ein Zahlenfeld ist so breit, wie sein Wertebereich
    /// braucht — im Vorbild 62 px —, damit die Einheit unmittelbar dahinter
    /// steht statt am rechten Rand des Blocks.
    /// </summary>
    [Fact]
    public void Ein_kurzes_Feld_bekommt_im_Raster_die_kurze_Breite()
    {
        string block = Stilblock(".epos-formularraster .epos-feld--kurz .epos-eingabe {");

        Assert.Contains("width: var(--epos-kurzfeld-breite)", block, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 auto", block, StringComparison.Ordinal);

        Assert.Contains("--epos-kurzfeld-breite: 8em", Stilblock(":root {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// ZWEI FELDPAARE JE ZEILE über <c>auto-fill</c>/<c>minmax</c> — kein
    /// gerechneter Prozentwert, dieselbe Bauart wie beim Kachelraster. Die
    /// Spaltenzahl hängt damit an der Breite des RASTERS und nicht an der des
    /// Fensters: genau das braucht die rechte Spalte eines Katalograhmens.
    /// </summary>
    [Fact]
    public void Der_Raster_ordnet_ueber_auto_fill_und_nicht_ueber_Prozente()
    {
        string block = Stilblock(".epos-formularraster {");

        Assert.Contains("repeat(auto-fill, minmax(var(--epos-formularspalte), 1fr))",
                        block, StringComparison.Ordinal);
        Assert.DoesNotContain("%", block, StringComparison.Ordinal);

        // Einspaltig ist die benannte Ausnahme.
        Assert.Contains("grid-template-columns: minmax(0, 1fr)",
                        Stilblock(".epos-formularraster--einspaltig {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Die GRUPPE ist durchsichtig: Ihre Felder bleiben direkte Rasterkinder.
    /// Ein zwischengeschobener Kasten hätte alle Felder einer Gruppe in EINE
    /// Rasterzelle gesetzt und die Beschriftungsspalten zweier Gruppen
    /// auseinanderlaufen lassen.
    /// </summary>
    [Fact]
    public void Eine_Gruppe_ist_im_Stilblatt_durchsichtig()
    {
        Assert.Contains("display: contents", Stilblock(".epos-formulargruppe {"), StringComparison.Ordinal);

        Assert.Contains("grid-column: 1 / -1",
                        Stilblock(".epos-formulargruppe-titel {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Der UMBRUCH ist eine Medienabfrage bei 900 px — dieselbe Breite wie bei
    /// <c>Zweispaltenauswahl</c> und <c>Katalograhmen</c>
    /// (<c>--epos-zweispalten-umbruch</c>). Darunter fällt die Beschriftung
    /// wieder über das Feld und der Raster auf eine Spalte.
    /// </summary>
    [Fact]
    public void Unter_900_Pixeln_faellt_die_Beschriftung_wieder_ueber_das_Feld()
    {
        string css = Stilblatt();

        int a = css.IndexOf("/* FORMULARRASTER (Anwenderwunsch iU8-E-2", StringComparison.Ordinal);
        Assert.True(a >= 0, "Der Block \"Formularraster\" steht nicht im Stilblatt");

        string block = css.Substring(a);

        int m = block.IndexOf("@media (max-width: 900px)", StringComparison.Ordinal);
        Assert.True(m >= 0, "Der Umbruch des Formularrasters ist keine Medienabfrage bei 900 px");

        string eng = block.Substring(m);

        // Eine Spalte, und die Beschriftung wieder ueber dem Feld.
        Assert.Contains(".epos-formularraster .epos-feld {", eng, StringComparison.Ordinal);
        Assert.Contains("grid-column: 1;", eng, StringComparison.Ordinal);

        // Dieselbe Breite wie die Hausregel - sonst braeche die eine Spalte
        // frueher als die andere.
        Assert.Contains("--epos-zweispalten-umbruch: 900px", Stilblock(":root {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Regel greift NUR im Raster. Ein Feld irgendwo sonst im Haus behält
    /// seine Form (Beschriftung darüber) — sonst hätte die Umstellung eines
    /// Bausteins sechzig Dialoge verschoben.
    /// </summary>
    [Fact]
    public void Ausserhalb_des_Rasters_bleibt_ein_Feld_unveraendert()
    {
        Assert.Contains("flex-direction: column", Stilblock(".epos-feld {"), StringComparison.Ordinal);

        string css = Stilblatt();

        int a = css.IndexOf("/* FORMULARRASTER (Anwenderwunsch iU8-E-2", StringComparison.Ordinal);
        Assert.True(a >= 0);

        // Jede Selektorzeile des Blocks nennt .epos-formularraster oder gehoert
        // zum Raster selbst (.epos-formulargruppe*). Nichts darin trifft ein
        // freistehendes Feld.
        foreach (string zeile in css.Substring(a).Split('\n'))
        {
            string z = zeile.Trim();
            if (!z.EndsWith("{", StringComparison.Ordinal)) continue;
            if (z.StartsWith("@", StringComparison.Ordinal)) continue;
            if (z.StartsWith(".epos-formulargruppe", StringComparison.Ordinal)) continue;

            Assert.Contains(".epos-formularraster", z, StringComparison.Ordinal);
        }
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static string Stilblatt()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        string css = Stilblatt();

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }

    // =====================================================================
    //  Das kurze ANZEIGEfeld — Paket P1 (Anwenderfoto „Verwaltung BHKW")
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1</b> (Anwenderfoto „Verwaltung BHKW", 05.09.2026):
    /// „Stelle diesen Dialog kompakter dar, insbesondere Daten zum BHKW-Modul
    /// unten."
    ///
    /// <para>Der Komponentenblock der Erzeuger-Projektmasken zeigt seine Werte
    /// als NUR LESBARE Textfelder — „290" hinter „thermische Leistung
    /// [kWth]:". Das ist so kurz wie ein Zahlenfeld, und deshalb kann ein
    /// Textfeld sich seit P1 ebenfalls als kurz melden.</para>
    /// </summary>
    [Fact]
    public void Ein_Textfeld_mit_Kurz_meldet_sich_als_kurzes_Feld()
    {
        var cut = Render<Textfeld>(p => p
            .Add(x => x.Bezeichnung, "thermische Leistung [kWth]:")
            .Add(x => x.Wert, "290")
            .Add(x => x.NurLesen, true)
            .Add(x => x.Kurz, true));

        Assert.Contains("epos-feld--kurz", cut.Find("label.epos-feld").ClassList);
    }

    /// <summary>
    /// Gegenprobe: OHNE <c>Kurz</c> bleibt das Textfeld, was es war — sonst
    /// verschöbe der neue Parameter die 92 Dateien mit Feldern auf einmal.
    /// </summary>
    [Fact]
    public void Ein_Textfeld_ohne_Kurz_bleibt_unveraendert()
    {
        var cut = Render<Textfeld>(p => p.Add(x => x.Bezeichnung, "Bezeichner"));

        Assert.DoesNotContain("epos-feld--kurz", cut.Find("label.epos-feld").ClassList);
    }

    /// <summary>
    /// Ein MEHRZEILIGES Textfeld bleibt LANG, auch wenn jemand <c>Kurz</c>
    /// setzt: Die Beschreibung war im Vorbild 250 × 48 px und spannt im Raster
    /// über beide Spalten. Zwei einander widersprechende Meldungen darf es
    /// nicht geben — die Länge gewinnt, weil sie am Bauteil hängt und nicht am
    /// Aufrufer.
    /// </summary>
    [Fact]
    public void Ein_mehrzeiliges_Textfeld_bleibt_lang()
    {
        var cut = Render<Textfeld>(p => p
            .Add(x => x.Mehrzeilig, true)
            .Add(x => x.Kurz, true));

        var feld = cut.Find("label.epos-feld");

        Assert.Contains("epos-feld--breit", feld.ClassList);
        Assert.DoesNotContain("epos-feld--kurz", feld.ClassList);
    }

    /// <summary>
    /// Welches Anzeigefeld kurz ist, entscheidet <c>ErzeugerDetail.IstZahl</c>
    /// — an EINER Stelle für alle sechs Erzeuger-Projektmasken. Die Probe hängt
    /// am WERT und nicht an der Beschriftung: Die Feldnamen kommen je
    /// Erzeugerart anders herein, eine Zahl bleibt eine Zahl.
    /// </summary>
    [Theory]
    [InlineData("290", true)]
    [InlineData("0,9", true)]
    [InlineData("0.9", true)]
    [InlineData("-12", true)]
    [InlineData("2 G Energietechnik GmbH", false)]
    [InlineData("Stadtgas", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Ein_Anzeigewert_ist_genau_dann_kurz_wenn_er_eine_Zahl_ist(string wert, bool kurz)
    {
        Assert.Equal(kurz, ErzeugerDetail.IstZahl(wert));
    }
}
