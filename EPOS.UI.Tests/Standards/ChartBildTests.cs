using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// ChartBild - das im Kern gezeichnete PNG als data:-URL. In der WebView gibt
/// es keinen Webserver, der eine Bilddatei ausliefern koennte.
///
/// <para><b>Seit der Windows-Abnahme 05.09.2026 (Befund A-1) steht das Bild im
/// Baustein <see cref="Diagramm"/>.</b> Das ist die Hausregel, und sie wird hier
/// bewiesen: JEDES Renderer-Bild geht durch diese Komponente und ist damit
/// zoombar. Ein zweiter img-Weg an ChartBild vorbei wäre ein Diagramm ohne Zoom
/// — genau der Zustand, den der Anwender beanstandet hat.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class ChartBildTests : BunitContext
{
    public ChartBildTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;

        // Der Rahmen laedt sein Zoommodul dynamisch; in Loose-Mode beantwortet
        // bunit den import mit dem Standardwert.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Die acht Bytes, an denen jede PNG-Datei erkennbar ist.</summary>
    private static readonly byte[] PngKennung = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    [Fact]
    public void Png_wird_als_data_URL_eingebettet()
    {
        var cut = Render<ChartBild>(p => p
            .Add(x => x.Png, PngKennung)
            .Add(x => x.Alt, "Jahresgang der Waermeleistung")
            .Add(x => x.Breite, 640)
            .Add(x => x.Hoehe, 320));

        var bild = cut.Find("img");
        Assert.Equal("data:image/png;base64,iVBORw0KGgo=", bild.GetAttribute("src"));
        Assert.Equal("Jahresgang der Waermeleistung", bild.GetAttribute("alt"));
        Assert.Equal("640", bild.GetAttribute("width"));
        Assert.Equal("320", bild.GetAttribute("height"));
    }

    [Fact]
    public void Ohne_Bild_erscheint_der_Platzhalter()
    {
        var cut = Render<ChartBild>(p => p.Add(x => x.PlatzhalterText, "Noch nicht gerechnet"));

        Assert.Empty(cut.FindAll("img"));
        Assert.Equal("Noch nicht gerechnet", cut.Find(".epos-chartbild-platzhalter").TextContent);
    }

    [Fact]
    public void Leeres_Feld_zeigt_ebenfalls_den_Platzhalter()
    {
        var cut = Render<ChartBild>(p => p.Add(x => x.Png, Array.Empty<byte>()));

        Assert.Empty(cut.FindAll("img"));
        Assert.Single(cut.FindAll(".epos-chartbild-platzhalter"));
    }

    // ==================================================================
    //  Der Rahmen (Windows-Abnahme 05.09.2026, Befund A-1)
    // ==================================================================

    /// <summary>
    /// Jedes Bild steht im Rahmen — daran hängt der ganze Zoom. Der Fall ist die
    /// Wache über die Hausregel: Wer das <c>img</c> hier je wieder aus dem
    /// <c>Diagramm</c> herauslöst, nimmt allen 32 Renderer-Bildern den Zoom.
    /// </summary>
    [Fact]
    public void Jedes_Bild_steht_im_Baustein_Diagramm()
    {
        var cut = Render<ChartBild>(p => p
            .Add(x => x.Png, PngKennung)
            .Add(x => x.Alt, "Jahresgang"));

        Assert.Single(cut.FindComponents<Diagramm>());
        Assert.Single(cut.FindAll(".epos-diagramm-inhalt img.epos-chartbild"));
        Assert.Equal("Jahresgang", cut.Find(".epos-diagramm-flaeche").GetAttribute("aria-label"));
    }

    /// <summary>Ohne Bild gibt es auch keinen Rahmen — ein Platzhalter zoomt nicht.</summary>
    [Fact]
    public void Der_Platzhalter_bekommt_keinen_Rahmen()
    {
        var cut = Render<ChartBild>(p => p.Add(x => x.PlatzhalterText, "Noch nicht gerechnet"));

        Assert.Empty(cut.FindComponents<Diagramm>());
    }

    /// <summary>
    /// Ohne Datenzoom bleibt es beim Bildzoom: EIN Knopf, kein „Bereich". Das gilt
    /// für die weit überwiegende Zahl der Bilder — Kuchen, Ringe, Kennlinien und
    /// Monatssäulen haben keinen Achsenbereich, den man aufziehen könnte.
    /// </summary>
    [Fact]
    public void Ohne_Datenzoom_traegt_der_Rahmen_nur_den_Knopf_eins_zu_eins()
    {
        var cut = Render<ChartBild>(p => p.Add(x => x.Png, PngKennung));

        Assert.Single(cut.FindAll("button.epos-diagramm-knopf"));
    }

    /// <summary>Mit Rückruf reicht ChartBild den Bereich unverändert weiter.</summary>
    [Fact]
    public void Der_Bereich_wird_durchgereicht()
    {
        Diagrammbereich? gemeldet = null;
        var cut = Render<ChartBild>(p => p
            .Add(x => x.Png, PngKennung)
            .Add(x => x.BereichGewaehlt, (Diagrammbereich b) => gemeldet = b));

        Assert.Equal(2, cut.FindAll("button.epos-diagramm-knopf").Count);

        var rahmen = cut.FindComponent<Diagramm>();
        cut.InvokeAsync(() => rahmen.Instance.BereichGemeldet(0.3, 0.6, 0.2, 0.8));

        Assert.NotNull(gemeldet);
        Assert.Equal(0.3, gemeldet!.XVon);
        Assert.Equal(0.8, gemeldet.YBis);
    }

    /// <summary>Und das Zurücksetzen ebenso — „1:1" räumt auch den Achsenbereich weg.</summary>
    [Fact]
    public void Das_Zuruecksetzen_wird_durchgereicht()
    {
        int gerufen = 0;
        var cut = Render<ChartBild>(p => p
            .Add(x => x.Png, PngKennung)
            .Add(x => x.Zurueckgesetzt, () => gerufen++));

        cut.Find("button.epos-diagramm-knopf").Click();

        Assert.Equal(1, gerufen);
    }

    // =====================================================================
    // Die MASSREGEL der Anzeige (Windows-Abnahme V2 07.09.2026, W11b‑B‑4)
    // =====================================================================

    /// <summary>
    /// <b>Befund W11b‑B‑4</b> — „Simulation Übersicht: Die Charts sind optisch zu
    /// groß." Seit W11b‑B‑2 füllt jedes Bild die Breite seines Rahmens; für eine
    /// Jahresganglinie ist das richtig, für einen Kuchen nicht. Die GESTALT sagt
    /// die Komponente, das MASS steht im Stilblatt — sonst stünde es an dreißig
    /// Fundstellen.
    /// </summary>
    [Fact]
    public void Ein_rundes_Bild_traegt_die_Gestaltklasse()
    {
        var cut = Render<ChartBild>(p => p
            .Add(x => x.Png, PngKennung)
            .Add(x => x.Rund, true));

        Assert.Contains("epos-diagramm--rund", cut.Find("div.epos-diagramm").ClassName ?? "");
    }

    /// <summary>
    /// Und ein BREITES Bild trägt sie nicht — die Ganglinien behalten die volle
    /// Zeilenbreite aus W11b‑B‑2. Ohne diese Gegenprobe wäre die Regel eine
    /// stille Rücknahme des vorigen Befundes.
    /// </summary>
    [Fact]
    public void Ein_breites_Bild_traegt_sie_nicht()
    {
        var cut = Render<ChartBild>(p => p.Add(x => x.Png, PngKennung));

        string klasse = cut.Find("div.epos-diagramm").ClassName ?? "";
        Assert.DoesNotContain("epos-diagramm--rund", klasse);
        Assert.Contains("epos-diagramm", klasse);
    }

    /// <summary>
    /// Eine bunit-Probe sieht eine Stilregel NICHT (Lehre W6‑B‑1) — geprüft wird
    /// deshalb auch die REGEL: beide Maße stehen als Token in <c>:root</c>, und
    /// die runde Grenze ist die kleinere von beiden.
    /// </summary>
    [Fact]
    public void Die_zwei_Masse_stehen_als_Token_im_Stilblatt()
    {
        string wurzel = Stilblock(":root {");

        int rund = Mass(wurzel, "--epos-diagramm-rund");
        int breit = Mass(wurzel, "--epos-diagramm-breit");

        Assert.Equal(560, rund);
        Assert.Equal(1240, breit);
        Assert.True(rund < breit, "Ein rundes Bild darf nie breiter erscheinen als ein breites.");
    }

    /// <summary>
    /// Die zwei Regeln selbst: der Rahmen nimmt die breite Grenze, die
    /// Gestaltklasse die runde — beide über das Token, kein Literal.
    /// </summary>
    [Fact]
    public void Der_Rahmen_begrenzt_seine_Anzeigebreite()
    {
        Assert.Contains("max-width: var(--epos-diagramm-breit)", Stilblock(".epos-diagramm {"));
        Assert.Contains("max-width: var(--epos-diagramm-rund)", Stilblock(".epos-diagramm--rund {"));

        // Und die Mindesthoehe der Linienbilder legt dem Kreis keinen weissen
        // Streifen unter den Rand.
        Assert.Contains("min-height: 0",
                        Stilblock(".epos-diagramm--rund .epos-diagramm-flaeche {"));
    }

    /// <summary>Der Wert eines Tokens in Bildpunkten.</summary>
    private static int Mass(string block, string token)
    {
        System.Text.RegularExpressions.Match m =
            System.Text.RegularExpressions.Regex.Match(block, token + @":\s*(\d+)px;");
        Assert.True(m.Success, $"Token {token} steht nicht in :root");
        return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        System.IO.DirectoryInfo? d = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (d is not null &&
               !System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        string css = System.IO.File.ReadAllText(
            System.IO.Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));

        int a = css.IndexOf(selektor, System.StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
