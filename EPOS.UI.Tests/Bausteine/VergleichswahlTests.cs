using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Vergleichswahl (W5-B-5, 08.09.2026) - eine Zeile Schalter, je Version einer; der
/// Stamm gesetzt und gesperrt. Die Auswahl gehoert dem Aufrufer.
/// </summary>
public class VergleichswahlTests : BunitContext
{
    private static IReadOnlyList<Vergleichswahl.Eintrag> Eintraege() => new[]
    {
        new Vergleichswahl.Eintrag(1030, "Stamm", true),
        new Vergleichswahl.Eintrag(1031, "WP klein"),
        new Vergleichswahl.Eintrag(1032, "Erdwärme")
    };

    [Fact]
    public void Je_Version_ein_Schalter_der_Stamm_gesetzt_und_gesperrt()
    {
        var cut = Render<Vergleichswahl>(p => p
            .Add(x => x.Bezeichnung, "Im Vergleich:")
            .Add(x => x.Eintraege, Eintraege())
            .Add(x => x.Gewaehlt, new[] { 1030, 1031, 1032 }));

        var haken = cut.FindAll("input[type=checkbox]");
        Assert.Equal(3, haken.Count);
        Assert.True(haken[0].HasAttribute("disabled"));
        Assert.True(haken[0].HasAttribute("checked"));
        Assert.False(haken[1].HasAttribute("disabled"));
        Assert.True(haken[2].HasAttribute("checked"));
        Assert.Contains("Im Vergleich:", cut.Find(".epos-vergleichswahl-titel").TextContent);
        Assert.Contains("WP klein", cut.Markup);
    }

    [Fact]
    public void Abwaehlen_meldet_die_neue_Liste_mit_dem_festen_Eintrag()
    {
        IReadOnlyList<int>? gemeldet = null;
        var cut = Render<Vergleichswahl>(p => p
            .Add(x => x.Eintraege, Eintraege())
            .Add(x => x.Gewaehlt, new[] { 1030, 1031, 1032 })
            .Add(x => x.GewaehltChanged, (IReadOnlyList<int> l) => gemeldet = l));

        cut.FindAll("input[type=checkbox]")[1].Change(false);

        Assert.Equal(new[] { 1030, 1032 }, gemeldet);
    }

    [Fact]
    public void Anwaehlen_haengt_die_Version_in_Reihenfolge_der_Eintraege_ein()
    {
        IReadOnlyList<int>? gemeldet = null;
        var cut = Render<Vergleichswahl>(p => p
            .Add(x => x.Eintraege, Eintraege())
            .Add(x => x.Gewaehlt, new[] { 1030, 1032 })
            .Add(x => x.GewaehltChanged, (IReadOnlyList<int> l) => gemeldet = l));

        Assert.False(cut.FindAll("input[type=checkbox]")[1].HasAttribute("checked"));
        cut.FindAll("input[type=checkbox]")[1].Change(true);

        Assert.Equal(new[] { 1030, 1031, 1032 }, gemeldet);
    }

    [Fact]
    public void Der_feste_Eintrag_traegt_den_Werkzeugtipp_und_Aktiv_false_sperrt_alle()
    {
        var cut = Render<Vergleichswahl>(p => p
            .Add(x => x.Eintraege, Eintraege())
            .Add(x => x.FestTipp, "Referenz, immer dabei")
            .Add(x => x.Aktiv, false));

        Assert.Equal("Referenz, immer dabei", cut.FindAll(".epos-vergleichswahl-eintrag")[0].GetAttribute("title"));
        Assert.All(cut.FindAll("input[type=checkbox]"), h => Assert.True(h.HasAttribute("disabled")));
    }
}
