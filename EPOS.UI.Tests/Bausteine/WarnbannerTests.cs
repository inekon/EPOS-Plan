using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>Warnbanner - die Meldung im Dialog statt als MessageBox.</summary>
public class WarnbannerTests : BunitContext
{
    [Theory]
    [InlineData(WarnStufe.Hinweis, "epos-warnbanner--hinweis")]
    [InlineData(WarnStufe.Warnung, "epos-warnbanner--warnung")]
    [InlineData(WarnStufe.Fehler, "epos-warnbanner--fehler")]
    public void Jede_Stufe_hat_ihre_Zustandsklasse(WarnStufe stufe, string klasse)
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Stufe, stufe)
            .Add(x => x.Text, "Bitte einen Variantennamen (Code) eingeben."));

        Assert.Contains(klasse, cut.Find("div").ClassName);
    }

    [Fact]
    public void Der_Text_wird_gezeigt_und_als_alert_gemeldet()
    {
        var cut = Render<Warnbanner>(p => p.Add(x => x.Text, "Bitte einen Variantennamen (Code) eingeben."));

        Assert.Equal("alert", cut.Find("div").GetAttribute("role"));
        Assert.Equal("Bitte einen Variantennamen (Code) eingeben.",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Ohne_Angabe_ist_die_Stufe_Warnung()
    {
        var cut = Render<Warnbanner>(p => p.Add(x => x.Text, "Hinweis"));

        Assert.Contains("epos-warnbanner--warnung", cut.Find("div").ClassName);
    }
}
