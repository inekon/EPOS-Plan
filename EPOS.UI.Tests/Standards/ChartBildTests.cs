using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// ChartBild - das im Kern gezeichnete PNG als data:-URL. In der WebView gibt
/// es keinen Webserver, der eine Bilddatei ausliefern koennte.
/// </summary>
public class ChartBildTests : BunitContext
{
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
}
