using Bunit;
using EPOS.UI.Dialoge.Kosten;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Kostenleiste (iU9-W6.0f). Soll ist <c>Views/Kosten/KostenKnoepfe.Leiste</c>:
/// drei Knoepfe und ein optionaler roter Kurzhinweis mit Vollsatz als Tooltip.
/// Der Vorlaeufer legte das Label nur an, wenn ein Hinweistext kam - hier ist
/// dieselbe Regel auf alle vier Teile ausgeweitet: kein Delegat, kein Knopf.
/// </summary>
public class KostenKnoepfeLeisteTests : BunitContext
{
    [Fact]
    public void Mit_beiden_Delegaten_stehen_die_drei_Knoepfe()
    {
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.KostenOeffnen, _ => Task.CompletedTask)
            .Add(x => x.EnergiekostenOeffnen, () => Task.CompletedTask));

        var knoepfe = cut.FindAll("button.epos-knopf");
        Assert.Equal(3, knoepfe.Count);
        Assert.Equal("Investitionskosten…", knoepfe[0].TextContent);
        Assert.Equal("Betriebskosten…", knoepfe[1].TextContent);
        Assert.Equal("Energiekosten…", knoepfe[2].TextContent);
    }

    [Fact]
    public void Ohne_Delegat_bleibt_der_Knopf_weg()
    {
        // Muster ErtragBonus.Sprung und Dateiwahl: Ein Knopf, der nichts tut,
        // waere eine Behauptung, die nicht stimmt.
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.EnergiekostenOeffnen, () => Task.CompletedTask));

        var knoepfe = cut.FindAll("button.epos-knopf");
        Assert.Single(knoepfe);
        Assert.Equal("Energiekosten…", knoepfe[0].TextContent);
    }

    [Fact]
    public void Ganz_ohne_Delegaten_bleibt_die_Leiste_leer()
    {
        var cut = Render<KostenKnoepfeLeiste>();

        Assert.Empty(cut.FindAll("button.epos-knopf"));
        Assert.Empty(cut.FindAll(".epos-kostenleiste-hinweis"));
    }

    [Fact]
    public void Invest_und_Betrieb_unterscheiden_sich_nur_im_Schalter()
    {
        // Im Vorlaeufer waren es zwei Click-Handler auf dieselbe Methode
        // OeffneKosten(..., betrieb: false/true).
        bool? gemeldet = null;
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.KostenOeffnen, b => { gemeldet = b; return Task.CompletedTask; }));

        cut.FindAll("button.epos-knopf")[0].Click();
        Assert.False(gemeldet);

        cut.FindAll("button.epos-knopf")[1].Click();
        Assert.True(gemeldet);
    }

    [Fact]
    public void Der_Energiekostenknopf_ruft_seinen_Delegaten()
    {
        int gerufen = 0;
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.EnergiekostenOeffnen, () => { gerufen++; return Task.CompletedTask; }));

        cut.Find("button.epos-knopf").Click();
        Assert.Equal(1, gerufen);
    }

    [Fact]
    public void Der_FK8_Hinweis_traegt_den_Vollsatz_als_Kurztext()
    {
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.KostenOeffnen, _ => Task.CompletedTask)
            .Add(x => x.Fk8Kurz, "Gepflegt wird im Kostendialog.")
            .Add(x => x.Fk8Hinweis, "Gepflegt wird im Kostendialog — Felder schreibgeschützt."));

        var hinweis = cut.Find(".epos-kostenleiste-hinweis");
        Assert.Equal("Gepflegt wird im Kostendialog.", hinweis.TextContent);
        Assert.Equal("Gepflegt wird im Kostendialog — Felder schreibgeschützt.",
                     hinweis.GetAttribute("title"));
    }

    [Fact]
    public void Ohne_Kurztext_gibt_es_keinen_Hinweis()
    {
        // Der Vorlaeufer legte das Label nur bei gesetztem fk8Hinweis an.
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.KostenOeffnen, _ => Task.CompletedTask));

        Assert.Empty(cut.FindAll(".epos-kostenleiste-hinweis"));
    }

    [Fact]
    public void Die_drei_Beschriftungen_lassen_sich_setzen()
    {
        // Die Huelle legt die Ressourcenschluessel KDLG_KNOPF_* ein.
        var cut = Render<KostenKnoepfeLeiste>(p => p
            .Add(x => x.KostenOeffnen, _ => Task.CompletedTask)
            .Add(x => x.EnergiekostenOeffnen, () => Task.CompletedTask)
            .Add(x => x.InvestText, "Investment costs…")
            .Add(x => x.BetriebText, "Operating costs…")
            .Add(x => x.EnergieText, "Energy costs…"));

        var knoepfe = cut.FindAll("button.epos-knopf");
        Assert.Equal("Investment costs…", knoepfe[0].TextContent);
        Assert.Equal("Operating costs…", knoepfe[1].TextContent);
        Assert.Equal("Energy costs…", knoepfe[2].TextContent);
    }
}
