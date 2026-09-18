using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Vorschlagszeile — die Grundlage im Klartext und der Knopf, der sie übernimmt
/// (Etappe BK1, Anwenderwunsch 17.09.2026). Sie rechnet nichts und entscheidet nichts;
/// geprüft wird deshalb, was sie ZEIGT und wann sie meldet.
/// </summary>
public class VorschlagszeileTests : BunitContext
{
    [Fact]
    public void Grundlage_und_Knopf_stehen_in_einer_Zeile()
    {
        var cut = Render<Vorschlagszeile>(p => p
            .Add(x => x.Text, "Einspeisung 5,57 ct/kWh")
            .Add(x => x.Knopftext, "Vorschlag uebernehmen"));

        Assert.Equal("Einspeisung 5,57 ct/kWh", cut.Find(".epos-herleitung-text").TextContent);
        Assert.Equal("Vorschlag uebernehmen", cut.Find("button.epos-vorschlag").TextContent.Trim());

        // Sie traegt denselben Rahmen wie eine Herleitungszeile - im Formularraster
        // spannt der ueber die volle Breite und stellt sie damit unter IHR Feld.
        Assert.Contains("epos-herleitung", cut.Find("p").ClassName);
    }

    [Fact]
    public void Der_Knopf_meldet_die_Uebernahme()
    {
        int gerufen = 0;
        var cut = Render<Vorschlagszeile>(p => p
            .Add(x => x.Knopftext, "uebernehmen")
            .Add(x => x.Uebernommen, EventCallback.Factory.Create(this, () => gerufen++)));

        cut.Find("button.epos-vorschlag").Click();
        Assert.Equal(1, gerufen);
    }

    /// <summary>
    /// GESPERRT HEISST BEGRUENDET, UND ZWAR WEICH: <c>aria-disabled</c> statt
    /// <c>disabled</c> (Hausregel EPOS.UI — ein <c>disabled</c>-Knopf zeigt seinen
    /// <c>title</c> nie). Der Knopf bleibt anklickbar und tut nichts.
    /// </summary>
    [Fact]
    public void Ein_gesperrter_Knopf_nennt_seinen_Grund_und_meldet_nicht()
    {
        int gerufen = 0;
        var cut = Render<Vorschlagszeile>(p => p
            .Add(x => x.Knopftext, "uebernehmen")
            .Add(x => x.Erlaubt, false)
            .Add(x => x.Sperrgrund, "Keine elektrische Nennleistung erfasst.")
            .Add(x => x.Uebernommen, EventCallback.Factory.Create(this, () => gerufen++)));

        var knopf = cut.Find("button.epos-vorschlag");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal("Keine elektrische Nennleistung erfasst.", knopf.GetAttribute("title"));

        knopf.Click();
        Assert.Equal(0, gerufen);
    }

    [Fact]
    public void Ein_erlaubter_Knopf_traegt_keinen_Sperrgrund()
    {
        var cut = Render<Vorschlagszeile>(p => p
            .Add(x => x.Knopftext, "uebernehmen")
            .Add(x => x.Sperrgrund, "wird nicht gezeigt"));

        var knopf = cut.Find("button.epos-vorschlag");
        Assert.Equal("false", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("title"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_sie_leer_und_bricht_nicht()
    {
        var cut = Render<Vorschlagszeile>();

        Assert.Equal("", cut.Find(".epos-herleitung-text").TextContent);
        Assert.NotNull(cut.Find("button.epos-vorschlag"));
    }
}
