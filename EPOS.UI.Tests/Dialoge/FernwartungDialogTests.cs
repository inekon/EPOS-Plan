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
/// gesperrte Knoepfe mit Grund, Rueckrufe fuer Quick Assist, Browser, E-Mail und Zwischenablage.
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
                                                        Action<string>? kopieren = null,
                                                        Func<bool>? starten = null)
        => Render<FernwartungDialog>(p => p
            .Add(x => x.Daten, Daten(erlaubt))
            .Add(x => x.AdresseOeffnen, oeffnen)
            .Add(x => x.Kopieren, kopieren)
            .Add(x => x.FernwartungStarten, starten));

    [Fact]
    public void Quick_Assist_starten_gibt_es_nur_mit_Rueckruf()
    {
        var ohne = Zeige(true, _ => true);
        Assert.Empty(ohne.FindAll("button.epos-fw-quickassist"));
        // Ohne Startweg nennt ein Satz Windows, die Seite ist der primaere Knopf.
        Assert.Equal(new FernwartungTexte().QuickAssistNurWindows,
                     ohne.Find(".epos-fw-nur-windows").TextContent.Trim());
        Assert.Contains("epos-knopf--primaer", ohne.Find("button.epos-fw-seite").ClassList);

        var mit = Zeige(true, _ => true, starten: () => true);
        var knopf = mit.Find("button.epos-fw-quickassist");
        Assert.Contains("epos-knopf--primaer", knopf.ClassList);
        Assert.Equal(new FernwartungTexte().QuickAssistStarten, knopf.TextContent.Trim());
        Assert.Empty(mit.FindAll(".epos-fw-nur-windows"));
        // Die Seite bleibt als zweiter, nicht primaerer Weg.
        Assert.DoesNotContain("epos-knopf--primaer", mit.Find("button.epos-fw-seite").ClassList);
    }

    [Fact]
    public void Der_Klick_startet_Quick_Assist_ueber_den_Rueckruf()
    {
        int aufrufe = 0;
        var geoeffnet = new List<string>();
        var cut = Zeige(true, a => { geoeffnet.Add(a); return true; }, starten: () => { aufrufe++; return true; });

        cut.Find("button.epos-fw-quickassist").Click();

        Assert.Equal(1, aufrufe);
        Assert.Empty(geoeffnet);
        Assert.DoesNotContain(new FernwartungTexte().QuickAssistFehler, cut.Markup);
    }

    [Fact]
    public void Misslingt_der_Start_zeigt_der_Dialog_den_Hinweis()
    {
        var cut = Zeige(true, _ => true, starten: () => false);

        cut.Find("button.epos-fw-quickassist").Click();

        Assert.Contains(new FernwartungTexte().QuickAssistFehler, cut.Markup);
    }

    [Fact]
    public void Wirft_der_Start_gilt_er_als_misslungen()
    {
        var cut = Zeige(true, _ => true, starten: () => throw new InvalidOperationException("kaputt"));

        cut.Find("button.epos-fw-quickassist").Click();

        Assert.Contains(new FernwartungTexte().QuickAssistFehler, cut.Markup);
    }

    [Fact]
    public void Gesperrt_ist_Quick_Assist_starten_disabled_mit_Grund()
    {
        int aufrufe = 0;
        var cut = Zeige(false, _ => true, starten: () => { aufrufe++; return true; });

        var knopf = cut.Find("button.epos-fw-quickassist");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.True(cut.Find("button.epos-fw-seite").HasAttribute("disabled"));
        Assert.Equal(GRUND, cut.Find(".epos-fw-sperrgrund").TextContent.Trim());

        knopf.Click();
        Assert.Equal(0, aufrufe);
    }

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
        int gestartet = 0;
        var cut = Zeige(true, a => { geoeffnet.Add(a); return true; }, starten: () => { gestartet++; return true; });

        Assert.Empty(cut.FindAll(".epos-fw-sperrgrund"));
        var knopf = cut.Find("button.epos-fw-seite");
        Assert.False(knopf.HasAttribute("disabled"));

        knopf.Click();

        Assert.Equal(new[] { Fernwartung.SEITE }, geoeffnet);
        Assert.Equal(0, gestartet);
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
        // Ohne Rueckrufe: kein Kopierknopf, kein Quick-Assist-Knopf, Seitenknopf gesperrt.
        Assert.Empty(cut.FindAll("button.epos-fw-kopieren"));
        Assert.Empty(cut.FindAll("button.epos-fw-quickassist"));
        Assert.True(cut.Find("button.epos-fw-seite").HasAttribute("disabled"));

        leiste.QuerySelector("button")!.Click();
        Assert.False(geschlossen);
    }
}
