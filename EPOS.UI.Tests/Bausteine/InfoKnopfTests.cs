using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>InfoKnopf - die Hilfepille (Auftrag #218): links das "i" zur Hilfeseite als
/// Inline-SVG, Klick geht an den Hilfedienst. <c>help_icon.png</c> bleibt fuer die
/// WinForms-Hilfe (Form_HelpPopup), hier zeichnet der Baustein kein Bild mehr.</summary>
public class InfoKnopfTests : BunitContext
{
    [Fact]
    public void Der_Knopf_zeigt_das_Hilfesymbol_als_Inline_SVG()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        var cut = Render<InfoKnopf>(p => p.Add(x => x.Schluessel, "Form_Kosten_Auswahl.btn_Help"));

        Assert.Empty(cut.FindAll("img"));
        var knopf = cut.Find("button.epos-infoknopf");
        Assert.NotEmpty(knopf.QuerySelectorAll("svg"));
    }

    /// <summary>Seit Auftrag #218 steht der Knopf im linken Feld der Hilfepille — auch
    /// dann, wenn ohne Assistenten kein zweites Feld daneben steht.</summary>
    [Fact]
    public void Der_Knopf_steht_im_linken_Feld_der_Hilfepille()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        var cut = Render<InfoKnopf>(p => p.Add(x => x.Schluessel, "Form_Kosten_Auswahl.btn_Help"));

        var pille = cut.Find(".epos-hilfepille");
        var knopf = pille.QuerySelector("button.epos-infoknopf");
        Assert.NotNull(knopf);
        Assert.Contains("epos-hilfepille__feld", knopf!.ClassName);
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
