using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Die <b>weich gesperrten Einträge</b> der Mehrfachauswahl (Auftrag PI-1, BV-E2): sichtbar mit
/// <c>aria-disabled</c> und dem Grund als <c>title</c>, nie <c>disabled</c>; ein Klick schaltet nicht
/// um, sondern meldet sich (<c>GesperrtVersucht</c>); „Alle" und „Keine" lassen sie, wie sie sind.
/// </summary>
public class MehrfachauswahlTests : BunitContext
{
    private static readonly IReadOnlyList<(int Id, string Text)> Drei = new[] { (1, "Eins"), (2, "Zwei"), (3, "Drei") };

    /// <summary>1 gesperrt und an, 2 gesperrt und aus, 3 frei und aus.</summary>
    private static string Grund(int id) => id is 1 or 2 ? "gesperrt, weil" : "";

    [Fact]
    public void Ein_gesperrter_Eintrag_traegt_Grund_und_aria_disabled_aber_kein_disabled()
    {
        var cut = Render<Mehrfachauswahl>(p => p
            .Add(x => x.Eintraege, Drei)
            .Add(x => x.Gewaehlt, new[] { 1 })
            .Add(x => x.Sperrgrund, (Func<int, string>)Grund));

        var kaesten = cut.FindAll("input[type=checkbox]");
        Assert.Equal(3, kaesten.Count);
        Assert.Equal("true", kaesten[0].GetAttribute("aria-disabled"));
        Assert.Equal("true", kaesten[1].GetAttribute("aria-disabled"));
        Assert.Null(kaesten[2].GetAttribute("aria-disabled"));
        Assert.All(kaesten, k => Assert.False(k.HasAttribute("disabled")));
        Assert.Equal("gesperrt, weil", kaesten[1].ParentElement!.GetAttribute("title"));
        Assert.Contains("epos-schalter", kaesten[1].ParentElement!.ClassList);
        Assert.True(kaesten[0].HasAttribute("checked"));
    }

    [Fact]
    public void Der_Klick_auf_einen_gesperrten_Eintrag_meldet_sich_und_schaltet_nicht_um()
    {
        var versucht = new List<int>();
        var gemeldet = new List<IReadOnlyList<int>>();
        var cut = Render<Mehrfachauswahl>(p => p
            .Add(x => x.Eintraege, Drei)
            .Add(x => x.Gewaehlt, new[] { 1 })
            .Add(x => x.Sperrgrund, (Func<int, string>)Grund)
            .Add(x => x.GesperrtVersucht, (int id) => versucht.Add(id))
            .Add(x => x.GewaehltChanged, (IReadOnlyList<int> w) => gemeldet.Add(w)));

        cut.FindAll("input[type=checkbox]")[1].Click();

        Assert.Equal(new[] { 2 }, versucht);
        Assert.Empty(gemeldet);
    }

    [Fact]
    public void Ohne_Rueckruf_laeuft_der_Klick_ins_Leere()
    {
        var gemeldet = new List<IReadOnlyList<int>>();
        var cut = Render<Mehrfachauswahl>(p => p
            .Add(x => x.Eintraege, Drei)
            .Add(x => x.Sperrgrund, (Func<int, string>)Grund)
            .Add(x => x.GewaehltChanged, (IReadOnlyList<int> w) => gemeldet.Add(w)));

        cut.FindAll("input[type=checkbox]")[0].Click();

        Assert.Empty(gemeldet);
    }

    [Fact]
    public void Alle_und_Keine_lassen_gesperrte_Eintraege_wie_sie_sind()
    {
        IReadOnlyList<int>? neu = null;
        var cut = Render<Mehrfachauswahl>(p => p
            .Add(x => x.Eintraege, Drei)
            .Add(x => x.Gewaehlt, new[] { 1 })
            .Add(x => x.Sperrgrund, (Func<int, string>)Grund)
            .Add(x => x.GewaehltChanged, (IReadOnlyList<int> w) => neu = w));

        cut.FindAll(".epos-leiste button")[0].Click();   // Alle
        Assert.Equal(new[] { 1, 3 }, neu);

        cut.FindAll(".epos-leiste button")[1].Click();   // Keine
        Assert.Equal(new[] { 1 }, neu);
    }
}
