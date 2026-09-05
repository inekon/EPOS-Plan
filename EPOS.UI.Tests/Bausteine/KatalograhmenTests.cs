using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <c>Katalograhmen</c> — Anwenderwunsch vom 05.09.2026,
/// „Admin-Menüs sind nicht an Größe Bildschirm angepasst".
///
/// <para><b>Der Befund.</b> „Administration Solarkollektoren" zeigte
/// untereinander: Überschrift, Balken „Auswahl in DB:", eine Liste in ihrem
/// eigenen kleinen Rollrahmen und darunter — nur über den SEITENrollbalken
/// erreichbar — den Balken „Eingabe der Solarkollektoren" mit den Feldern.
/// Alle sechs Verwaltungsmasken des Bestands stellten beides NEBENEINANDER
/// (Form_Heizkessel_Admin 726 × 383, Form_BHKWAdmin 856 × 517,
/// Form_SolarKollektorenAdmin 825 × 494, Form_PufferSp_Admin 721 × 330,
/// Form_AdminPV 607 × 489, Form_AdminStromspeicher 614 × 367).</para>
///
/// <para><b>Zweierlei wird geprüft</b> (Lehre W6‑B‑1): das MARKUP über bunit
/// und die REGEL im Stilblatt — eine bunit-Probe rechnet kein CSS aus. Denselben
/// Weg gehen <c>ListenrahmenTests</c> und <c>ZweispaltenauswahlTests</c>.</para>
///
/// <para>Keine Sprachbindung: geprüft werden Klassennamen. Die Kultur wird
/// trotzdem gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class KatalograhmenTests : BunitContext
{
    public KatalograhmenTests()
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

    private IRenderedComponent<Katalograhmen> Aufbauen(bool gestapelt = false) =>
        Render<Katalograhmen>(p => p
            .Add(x => x.Gestapelt, gestapelt)
            .Add(x => x.Liste, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"liste\">Liste</p>")))
            .Add(x => x.Eingabe, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"eingabe\">Eingabe</p>"))));

    // =====================================================================
    //  Das Markup
    // =====================================================================

    /// <summary>Zwei Spalten, jede mit ihrem Inhalt — und der Rahmen füllt.</summary>
    [Fact]
    public void Der_Rahmen_stellt_Liste_und_Eingabe_nebeneinander()
    {
        var cut = Aufbauen();

        string wurzel = cut.Find("div").ClassName ?? "";
        Assert.Contains("epos-katalog-paar", wurzel);
        Assert.Contains("epos-katalog-fuellend", wurzel);
        Assert.DoesNotContain("epos-katalog-paar--gestapelt", wurzel);

        Assert.Equal("Liste", cut.Find(".epos-katalog-liste #liste").TextContent);
        Assert.Equal("Eingabe", cut.Find(".epos-katalog-eingabe #eingabe").TextContent);
    }

    /// <summary>
    /// <c>Gestapelt</c> ist die Ausnahme für die Masken, deren Vorbild schon
    /// gestapelt war (<c>Form_AdminWaermeeinlesen</c> 676 × 433 — die Liste ging
    /// dort über die volle Breite).
    /// </summary>
    [Fact]
    public void Gestapelt_haengt_die_Zusatzklasse_an()
    {
        Assert.Contains("epos-katalog-paar--gestapelt",
                        Aufbauen(gestapelt: true).Find("div").ClassName ?? "");
    }

    /// <summary>
    /// Die zwei Spalten stehen auch ohne Inhalt da — sonst fiele die
    /// Rasteraufteilung in sich zusammen, sobald ein Wirt nur eine Seite füllt.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_stehen_beide_Spalten()
    {
        var cut = Render<Katalograhmen>();

        Assert.Single(cut.FindAll(".epos-katalog-liste"));
        Assert.Single(cut.FindAll(".epos-katalog-eingabe"));
    }

    // =====================================================================
    //  Die Regeln im Stilblatt
    // =====================================================================

    /// <summary>
    /// Die Wurzel eines Katalogdialogs nimmt <c>.epos-dialog</c> die
    /// Breitenbremse (1160 px) und gibt ihm die volle Höhe des Fensters.
    /// </summary>
    [Fact]
    public void Die_Dialogwurzel_nimmt_Breite_und_Hoehe()
    {
        string block = Stilblock(".epos-katalog-dialog {");

        Assert.Contains("max-width: none", block);
        Assert.Contains("height: 100dvh", block);
        Assert.Contains("overflow: hidden", block);
    }

    /// <summary>
    /// Der füllende Abschnitt braucht <c>min-height: 0</c>. Ohne sie wächst ein
    /// Flex-Kind nie unter seinen Inhalt, und der Rollbalken landet an der Seite
    /// statt in der Liste — genau der Befund.
    /// </summary>
    [Fact]
    public void Der_fuellende_Abschnitt_traegt_min_height_null()
    {
        string block = Stilblock(".epos-katalog-fuellend {");

        Assert.Contains("flex: 1 1 auto", block);
        Assert.Contains("min-height: 0", block);
    }

    /// <summary>
    /// <b>Hier fällt die Höchsthöhe aus W9‑B‑2</b> — und nur hier: Die Liste im
    /// Katalograhmen nimmt die verbleibende Höhe, überall sonst gilt
    /// <c>--epos-listenhoehe</c> weiter (das prüft <c>ListenrahmenTests</c>).
    /// </summary>
    [Fact]
    public void Die_Liste_im_Rahmen_verliert_die_Hoechsthoehe()
    {
        string block = Stilblock(".epos-katalog-liste .epos-raster-huelle {");

        Assert.Contains("flex: 1 1 auto", block);
        Assert.Contains("max-height: none", block);
        Assert.Contains("min-height:", block);
    }

    /// <summary>Der Eingabeblock rollt selbst — nie die Seite.</summary>
    [Fact]
    public void Der_Eingabeblock_rollt_selbst()
    {
        Assert.Contains("overflow-y: auto", Stilblock(".epos-katalog-eingabe {"));
    }

    /// <summary>
    /// <b>Der Umbruch ist eine Medienabfrage bei 900 px</b> — derselbe Wert wie
    /// beim Baustein <c>Zweispaltenauswahl</c> (<c>--epos-zweispalten-umbruch</c>)
    /// und beim Dublettenbaum. Nicht 1100 px: Der Inhalt der WebView rechnet in
    /// CSS-Pixeln, das Fenstermaß in Gerätepixeln — bei 150 % Skalierung sind
    /// 1632 Gerätepixel nur 1088 CSS-Pixel, und der Umbruch träfe genau den
    /// Anwender, der den Befund gemeldet hat.
    /// </summary>
    [Fact]
    public void Der_Umbruch_liegt_bei_derselben_Breite_wie_die_Zweispaltenauswahl()
    {
        string css = Stilblatt();

        int a = css.IndexOf(".epos-katalog-paar {", StringComparison.Ordinal);
        Assert.True(a >= 0);

        // Die Vorgabe sind ZWEI Spalten; die Medienabfrage macht daraus eine.
        Assert.Contains("grid-template-columns: minmax(", Stilblock(".epos-katalog-paar {"));

        int m = css.IndexOf("@media (max-width: 900px) {", a, StringComparison.Ordinal);
        Assert.True(m > a, "hinter .epos-katalog-paar steht keine Medienabfrage bei 900px");

        // Und der Wert steht als Token daneben, damit die zwei nicht auseinanderlaufen.
        Assert.Contains("--epos-zweispalten-umbruch: 900px", Stilblock(":root {"));
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
}
