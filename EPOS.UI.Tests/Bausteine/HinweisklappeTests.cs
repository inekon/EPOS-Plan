using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Hinweisklappe - die Klappzeile einer Hinweisliste (Simulation, Bericht,
/// Wirtschaftlichkeit): Anfangsstellung, Aufklappen, Sitzungsgedaechtnis und Dialogweg.
/// </summary>
public class HinweisklappeTests : EposBunitContext
{
    private IRenderedComponent<Hinweisklappe> Zeige(int anzahl, IDictionary<string, bool>? gedaechtnis = null,
                                                    string? schluessel = null, bool einzelnOffen = true,
                                                    EventCallback oeffnen = default)
        => Render<Hinweisklappe>(p => p
            .Add(x => x.Anzahl, anzahl)
            .Add(x => x.TextEiner, "1 Hinweis")
            .Add(x => x.TextMehrere, "{0} Hinweise")
            .Add(x => x.Stufe, WarnStufe.Warnung)
            .Add(x => x.Schluessel, schluessel)
            .Add(x => x.Gedaechtnis, gedaechtnis)
            .Add(x => x.EinzelnOffen, einzelnOffen)
            .Add(x => x.Oeffnen, oeffnen)
            .AddChildContent("<ul class=\"liste\"><li>a</li></ul>"));

    [Fact]
    public void Ohne_Hinweise_zeichnet_sie_nichts()
        => Assert.Equal("", Zeige(0).Markup.Trim());

    [Fact]
    public void Mehrere_Hinweise_stehen_eingeklappt()
    {
        var cut = Zeige(5);
        Assert.False(cut.Instance.IstOffen);
        Assert.Equal("5", cut.Find(".epos-simerg-laufband-zahl").TextContent);
        Assert.Contains("5 Hinweise", cut.Find("button").TextContent);
        Assert.Contains("▾", cut.Find(".epos-simerg-laufband-pfeil").TextContent);
        Assert.Empty(cut.FindAll(".liste"));
    }

    [Fact]
    public void Ein_einzelner_Hinweis_steht_offen_ausser_abgeschaltet()
    {
        Assert.Single(Zeige(1).FindAll(".liste"));
        Assert.Empty(Zeige(1, einzelnOffen: false).FindAll(".liste"));
    }

    [Fact]
    public void Klick_klappt_auf_und_zu()
    {
        var cut = Zeige(3);
        cut.Find("button").Click();
        Assert.Single(cut.FindAll(".liste"));
        Assert.Contains("▴", cut.Find(".epos-simerg-laufband-pfeil").TextContent);
        cut.Find("button").Click();
        Assert.Empty(cut.FindAll(".liste"));
    }

    [Fact]
    public void Die_Stellung_steht_im_Sitzungsgedaechtnis_und_bleibt_beim_Neuzeichnen()
    {
        var gedaechtnis = new Dictionary<string, bool>();
        var cut = Zeige(3, gedaechtnis, "Seite");
        cut.Find("button").Click();
        Assert.True(gedaechtnis["Seite"]);

        cut.Render(p => p.Add(x => x.Anzahl, 4));        // neue Zahl, neues Zeichnen
        Assert.Single(cut.FindAll(".liste"));

        Assert.Single(Zeige(2, gedaechtnis, "Seite").FindAll(".liste"));   // neue Instanz
        Assert.Empty(Zeige(2, gedaechtnis, "Andere").FindAll(".liste"));   // je Seite
    }

    [Fact]
    public void Mit_Oeffnen_ruft_der_Klick_den_Dialogweg()
    {
        int gerufen = 0;
        var cut = Zeige(1, oeffnen: EventCallback.Factory.Create(this, () => gerufen++));
        Assert.Empty(cut.FindAll(".liste"));
        cut.Find("button").Click();
        Assert.Equal(1, gerufen);
        Assert.Empty(cut.FindAll(".liste"));
    }
}
