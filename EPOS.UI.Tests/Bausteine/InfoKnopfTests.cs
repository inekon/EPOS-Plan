using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>InfoKnopf - 28x28, help_icon.png, Klick geht an den Hilfedienst.</summary>
public class InfoKnopfTests : BunitContext
{
    [Fact]
    public void Der_Knopf_zeigt_das_Hilfesymbol_in_28x28()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        var cut = Render<InfoKnopf>(p => p.Add(x => x.Schluessel, "Form_Kosten_Auswahl.btn_Help"));

        var bild = cut.Find("img");
        Assert.Equal("_content/EPOS.UI/help_icon.png", bild.GetAttribute("src"));
        Assert.Equal("28", bild.GetAttribute("width"));
        Assert.Equal("28", bild.GetAttribute("height"));
        Assert.Contains("epos-infoknopf", cut.Find("button").ClassName);
    }

    [Fact]
    public void Der_Klick_oeffnet_die_Hilfe_zum_Schluessel()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Render<InfoKnopf>(p => p.Add(x => x.Schluessel, "Form_Kosten_Auswahl.btn_Help"));
        cut.Find("button").Click();

        Assert.Equal(new[] { "Form_Kosten_Auswahl.btn_Help" }, hilfe.Geoeffnet);
    }

    [Fact]
    public void Der_Kurztext_kommt_aus_dem_Katalog()
    {
        Services.AddSingleton<IHilfeDienst>(
            new TestHilfe(new HilfeEintrag("Kosten der Energietraeger", "Lange Beschreibung", null)));

        var cut = Render<InfoKnopf>(p => p.Add(x => x.Schluessel, "Form_Kosten_Auswahl.btn_Help"));

        Assert.Equal("Kosten der Energietraeger", cut.Find("button").GetAttribute("title"));
        Assert.Equal("Kosten der Energietraeger", cut.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void Ohne_Katalogeintrag_bleibt_der_Standardkurztext()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        var cut = Render<InfoKnopf>(p => p
            .Add(x => x.Schluessel, "Unbekannt.btn_Help")
            .Add(x => x.StandardKurztext, "Hilfe zu diesem Fenster"));

        Assert.Equal("Hilfe zu diesem Fenster", cut.Find("button").GetAttribute("title"));
        Assert.Null(cut.Instance.Eintrag);
    }

    [Fact]
    public void Ohne_Schluessel_geschieht_beim_Klick_nichts()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Render<InfoKnopf>();
        cut.Find("button").Click();

        Assert.Empty(hilfe.Geoeffnet);
    }
}
