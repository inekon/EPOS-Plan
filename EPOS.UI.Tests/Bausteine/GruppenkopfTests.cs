using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>Gruppenkopf - Abschnittsbalken nach dem Vorbild SectionPanel.cs.</summary>
public class GruppenkopfTests : BunitContext
{
    [Fact]
    public void Titel_Symbol_und_Summe_stehen_im_Balken()
    {
        var cut = Render<Gruppenkopf>(p => p
            .Add(x => x.Titel, "Energietraeger")
            .Add(x => x.Symbol, "E")
            .Add(x => x.Summe, "1.234 EUR"));

        var balken = cut.Find(".epos-gruppenkopf-balken");
        Assert.Equal("Energietraeger", cut.Find(".epos-gruppenkopf-titel").TextContent);
        Assert.Equal("E", cut.Find(".epos-gruppenkopf-symbol").TextContent);
        Assert.Equal("1.234 EUR", cut.Find(".epos-gruppenkopf-summe").TextContent);
        Assert.Contains("Energietraeger", balken.TextContent);
    }

    [Fact]
    public void Ohne_Summe_und_Symbol_bleiben_die_Felder_weg()
    {
        var cut = Render<Gruppenkopf>(p => p.Add(x => x.Titel, "Kosten"));

        Assert.Empty(cut.FindAll(".epos-gruppenkopf-summe"));
        Assert.Empty(cut.FindAll(".epos-gruppenkopf-symbol"));
    }

    [Fact]
    public void Inhalt_steht_unter_dem_Balken()
    {
        var cut = Render<Gruppenkopf>(p => p
            .Add(x => x.Titel, "Kosten")
            .Add(x => x.KindInhalt, (RenderFragment)(bau => bau.AddMarkupContent(0, "<p>Inhalt</p>"))));

        Assert.Equal("Inhalt", cut.Find(".epos-gruppenkopf-koerper p").TextContent);
    }
}
