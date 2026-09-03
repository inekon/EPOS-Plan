using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Zeilenwahl (iU9-W3.0) - der runde Wahlknopf einer Rasterzeile. Vorher stand
/// er wortgleich im Markup von BhkwWirtschaftlichkeitDialog und
/// KostenfaktorKatalogDialog.
/// </summary>
public class ZeilenwahlTests : BunitContext
{
    [Fact]
    public void Ungewaehlt_zeigt_der_Knopf_den_leeren_Kreis()
    {
        var cut = Render<Zeilenwahl>();

        Assert.Equal("○", cut.Find("button").TextContent.Trim());
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Contains("epos-anlagenwahl", cut.Find("button").ClassName);
        Assert.DoesNotContain("epos-knopf--primaer", cut.Find("button").ClassName);
    }

    [Fact]
    public void Gewaehlt_zeigt_er_den_vollen_Kreis_und_meldet_es_der_Sprachausgabe()
    {
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Gewaehlt, true));

        Assert.Equal("●", cut.Find("button").TextContent.Trim());
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Contains("epos-knopf--primaer", cut.Find("button").ClassName);
    }

    [Fact]
    public void Der_Klick_wird_gemeldet()
    {
        int mal = 0;
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Gewaehltwerden, () => mal++));

        cut.Find("button").Click();

        Assert.Equal(1, mal);
    }

    [Fact]
    public void Der_Kurztext_steht_am_Knopf()
    {
        var cut = Render<Zeilenwahl>(p => p.Add(x => x.Kurztext, "Emissionsart wählen"));

        Assert.Equal("Emissionsart wählen", cut.Find("button").GetAttribute("title"));
    }

    [Fact]
    public void Gesperrt_bleibt_der_Knopf_sichtbar_und_meldet_nicht()
    {
        int mal = 0;
        var cut = Render<Zeilenwahl>(p => p
            .Add(x => x.Aktiv, false)
            .Add(x => x.Gewaehltwerden, () => mal++));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.Equal(0, mal);
    }
}
