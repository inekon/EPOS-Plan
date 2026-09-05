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
}
