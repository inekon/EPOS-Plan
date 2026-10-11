using System.Collections.Generic;
using Bunit;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Fernwartung" (Konzept Administration und Support, Abschnitt 4.2): vier Zonen,
/// gesperrter Knopf mit Grund, Rueckrufe fuer Browser, E-Mail und Zwischenablage.
/// </summary>
public class FernwartungDialogTests : EposBunitContext
{
    private const string DIAGNOSE = "Programmversion: 9.8.7\nLizenztyp: Firma\n";
    private const string GRUND = "Die Fernwartung gehört zum Wartungsvertrag.";

    public FernwartungDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static FernwartungDaten Daten(bool erlaubt) =>
        new(erlaubt, erlaubt ? "" : GRUND, "0711 9959 826 50", "info@inekon.de", "werktags 9 bis 16 Uhr",
            DIAGNOSE, Fernwartung.SEITE);

    private IRenderedComponent<FernwartungDialog> Zeige(bool erlaubt, Func<string, bool>? oeffnen = null,
                                                        Action<string>? kopieren = null)
        => Render<FernwartungDialog>(p => p
            .Add(x => x.Daten, Daten(erlaubt))
            .Add(x => x.AdresseOeffnen, oeffnen)
            .Add(x => x.Kopieren, kopieren));

    [Fact]
    public void Gesperrt_ist_der_Knopf_disabled_und_der_Grund_sichtbar()
    {
        var geoeffnet = new List<string>();
        var cut = Zeige(false, a => { geoeffnet.Add(a); return true; });

        var knopf = cut.Find("button.epos-fw-seite");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Equal(GRUND, cut.Find(".epos-fw-sperrgrund").TextContent.Trim());

        // Die Zone bleibt sichtbar: Erklaerung und drei Schritte.
        Assert.Equal(3, cut.FindAll("ol.epos-fw-schritte li").Count);
        Assert.Empty(geoeffnet);
    }

    [Fact]
    public void Frei_oeffnet_der_Knopf_die_Fernwartungsseite()
    {
        var geoeffnet = new List<string>();
        var cut = Zeige(true, a => { geoeffnet.Add(a); return true; });

        Assert.Empty(cut.FindAll(".epos-fw-sperrgrund"));
        var knopf = cut.Find("button.epos-fw-seite");
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Contains("epos-knopf--primaer", knopf.ClassList);

        knopf.Click();

        Assert.Equal(new[] { Fernwartung.SEITE }, geoeffnet);
        Assert.Empty(cut.FindAll("[role=alert]"));
    }

    [Fact]
    public void Misslingt_das_Oeffnen_meldet_der_Dialog_es_benannt()
    {
        var cut = Zeige(true, _ => false);

        cut.Find("button.epos-fw-seite").Click();

        Assert.Contains(Fernwartung.SEITE, cut.Markup);
    }

    [Fact]
    public void Kopieren_legt_den_Diagnosetext_ab()
    {
        string? kopiert = null;
        var cut = Zeige(true, _ => true, t => kopiert = t);

        Assert.Equal(DIAGNOSE, cut.Find("textarea").TextContent);
        Assert.True(cut.Find("textarea").HasAttribute("readonly"));

        cut.Find("button.epos-fw-kopieren").Click();

        Assert.Equal(DIAGNOSE, kopiert);
    }

    [Fact]
    public void Die_E_Mail_oeffnet_mailto_ueber_den_Rueckruf()
    {
        var geoeffnet = new List<string>();
        var cut = Zeige(false, a => { geoeffnet.Add(a); return true; });

        var link = cut.Find("a.epos-fw-mail");
        Assert.Equal("mailto:info@inekon.de", link.GetAttribute("href"));
        link.Click();

        // Auch gesperrt: Der Kontakt nuetzt immer.
        Assert.Equal(new[] { "mailto:info@inekon.de" }, geoeffnet);
    }

    [Fact]
    public void Die_Fussleiste_traegt_nur_Schliessen()
    {
        bool? geschlossen = null;
        var cut = Render<FernwartungDialog>(p => p
            .Add(x => x.Daten, Daten(true))
            .Add(x => x.Geschlossen, (bool b) => geschlossen = b));

        var leiste = cut.Find(".epos-leiste");
        Assert.Single(leiste.QuerySelectorAll("button"));
        Assert.NotNull(leiste.QuerySelector(".epos-leiste-fueller"));
        // Ohne Rueckrufe: kein Kopierknopf, Seitenknopf gesperrt.
        Assert.Empty(cut.FindAll("button.epos-fw-kopieren"));
        Assert.True(cut.Find("button.epos-fw-seite").HasAttribute("disabled"));

        leiste.QuerySelector("button")!.Click();
        Assert.False(geschlossen);
    }
}
