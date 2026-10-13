using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Das Jahresraster zum Ansehen</b> (Karte „Kalender" des Zapfprofils, ZB5): 12 Monate × 31 Tage, je Tag die Farbe
/// seiner Tagesart aus den Gaben des Wirts, Legende mit der Zahl der Tage, keine Bedienung; ohne Gaben nichts.
/// </summary>
public class JahresrasterTests : EposBunitContext
{
    private static readonly (int, string)[] LEGENDE = { (0, "Werktag"), (1, "Samstag"), (4, "Ferien") };

    private static int[] Tage()
    {
        var tage = new int[365];
        for (int d = 1; d <= 365; d++) tage[d - 1] = (d - 1) % 7 == 5 ? 1 : 0;
        for (int d = 182; d <= 226; d++) tage[d - 1] = 4;      // 01.07. bis 14.08.
        return tage;
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_das_Raster_nichts()
    {
        var cut = Render<Jahresraster>();
        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void Das_Raster_zeigt_365_Tage_in_zwoelf_Monaten_mit_Farbe_Titel_und_Legende()
    {
        var cut = Render<Jahresraster>(p => p
            .Add(x => x.Bezeichnung, "Jahresraster der Zone")
            .Add(x => x.Tage, Tage())
            .Add(x => x.Legende, LEGENDE)
            .Add(x => x.Monatsnamen, new[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" }));

        IElement[] tage = cut.FindAll(".epos-jahresraster-tag").ToArray();
        Assert.Equal(365, tage.Length);
        Assert.Equal(12, cut.FindAll(".epos-kalb-monat").Count);
        Assert.Equal("Feb", cut.FindAll(".epos-kalb-monat")[1].TextContent);
        // 12 Monate × 31 Plätze: die Tage, die es im Gemeinjahr nicht gibt, sind leer.
        Assert.Equal(12 * 31 - 365, cut.FindAll(".epos-kalb-tagleer").Count);
        Assert.Equal("Jahresraster der Zone", cut.Find(".epos-kalb-jahr").GetAttribute("aria-label"));

        Assert.Contains("epos-kalb-art-1", tage[5].ClassName);
        Assert.Contains("epos-kalb-art-4", tage[181].ClassName);
        Assert.Equal("182", tage[181].GetAttribute("data-tag"));
        Assert.Equal("01.07. · Ferien", tage[181].GetAttribute("title"));
        Assert.Equal("1", tage[181].TextContent);

        string[] legende = cut.FindAll(".epos-kalb-legende li").Select(l => l.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Werktag · 274", "Samstag · 46", "Ferien · 45" }, legende);
        // Die Tage sind keine Knöpfe: Das Raster wählt nichts.
        Assert.Empty(cut.FindAll(".epos-jahresraster button"));
    }
}
