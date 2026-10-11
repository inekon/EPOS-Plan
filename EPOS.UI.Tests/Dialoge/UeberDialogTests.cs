using System.Collections.Generic;
using Bunit;
using EPOS.UI.Dialoge.Hilfe;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Über EPOS-Plan" (Konzept Administration und Support, Abschnitt 5): Kopf,
/// Diagnoseblock mit Kopieren und Protokollordner, Kontakt mit Fernwartung, die Geste des
/// Entwicklermodus über den Anstoß. Ohne Rückruf kein Knopf.
/// </summary>
public class UeberDialogTests : EposBunitContext
{
    private const string DIAGNOSE = "Programmversion: 9.8.7\nEntwicklermodus: Anwender (aus)\n";
    private const string DIAGNOSE_NEU = "Programmversion: 9.8.7\nEntwicklermodus: Entwickler (Einstellung)\n";

    public UeberDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static UeberDaten Daten() =>
        new("EPOS-Plan", "Energie · Planung", "Version 9.8.7 vom 11.10.2026", "INEKON",
            DIAGNOSE, "0711 9959 826 50", "info@inekon.de", "werktags 9 bis 16 Uhr");

    private IRenderedComponent<UeberDialog> Zeige(Action<string>? kopieren = null,
                                                   Action? protokollordner = null,
                                                   Action? fernwartung = null,
                                                   Func<string, bool>? oeffnen = null,
                                                   Func<string?>? schalten = null,
                                                   UeberAnstoss? anstoss = null)
        => Render<UeberDialog>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Kopieren, kopieren)
            .Add(x => x.ProtokollordnerOeffnen, protokollordner)
            .Add(x => x.FernwartungOeffnen, fernwartung)
            .Add(x => x.AdresseOeffnen, oeffnen)
            .Add(x => x.EntwicklermodusSchalten, schalten)
            .Add(x => x.Anstoss, anstoss));

    [Fact]
    public void Kopf_und_Diagnosetext_sind_sichtbar()
    {
        var cut = Zeige();

        Assert.Equal("EPOS-Plan", cut.Find(".epos-ueber-produkt").TextContent.Trim());
        Assert.Equal("Version 9.8.7 vom 11.10.2026", cut.Find(".epos-ueber-version").TextContent.Trim());
        Assert.Equal("INEKON", cut.Find(".epos-ueber-haus").TextContent.Trim());
        Assert.Equal(DIAGNOSE, cut.Find("textarea").TextContent);
        Assert.True(cut.Find("textarea").HasAttribute("readonly"));
    }

    [Fact]
    public void Diagnosedaten_kopieren_ruft_Kopieren_mit_dem_Text()
    {
        string? kopiert = null;
        var cut = Zeige(kopieren: t => kopiert = t);

        cut.Find("button.epos-ueber-kopieren").Click();

        Assert.Equal(DIAGNOSE, kopiert);
        Assert.Contains("Zwischenablage", cut.Markup);
    }

    [Fact]
    public void Ohne_Rueckrufe_gibt_es_keine_Knoepfe_und_keinen_Link()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll("button.epos-ueber-kopieren"));
        Assert.Empty(cut.FindAll("button.epos-ueber-protokollordner"));
        Assert.Empty(cut.FindAll("button.epos-ueber-fernwartung"));
        Assert.Empty(cut.FindAll("a.epos-ueber-mail"));
        Assert.Equal("info@inekon.de", cut.Find("span.epos-ueber-mail").TextContent.Trim());
    }

    [Fact]
    public void Protokollordner_oeffnen_ruft_den_Rueckruf()
    {
        int gerufen = 0;
        var cut = Zeige(protokollordner: () => gerufen++);

        cut.Find("button.epos-ueber-protokollordner").Click();

        Assert.Equal(1, gerufen);
    }

    [Fact]
    public void Fernwartung_ruft_den_Rueckruf()
    {
        int gerufen = 0;
        var cut = Zeige(fernwartung: () => gerufen++);

        cut.Find("button.epos-ueber-fernwartung").Click();

        Assert.Equal(1, gerufen);
    }

    [Fact]
    public void Die_E_Mail_oeffnet_mailto_ueber_den_Rueckruf()
    {
        var geoeffnet = new List<string>();
        var cut = Zeige(oeffnen: a => { geoeffnet.Add(a); return true; });

        var link = cut.Find("a.epos-ueber-mail");
        Assert.Equal("mailto:info@inekon.de", link.GetAttribute("href"));
        link.Click();

        Assert.Equal(new[] { "mailto:info@inekon.de" }, geoeffnet);
    }

    [Fact]
    public void Misslingt_die_E_Mail_meldet_der_Dialog_die_Adresse()
    {
        var cut = Zeige(oeffnen: _ => false);

        cut.Find("a.epos-ueber-mail").Click();

        Assert.Contains("Schreiben Sie an info@inekon.de", cut.Markup);
    }

    [Fact]
    public void Die_Geste_ersetzt_den_Diagnosetext()
    {
        var anstoss = new UeberAnstoss();
        int geschaltet = 0;
        var cut = Zeige(schalten: () => { geschaltet++; return DIAGNOSE_NEU; }, anstoss: anstoss);

        cut.InvokeAsync(anstoss.Ausloesen);

        cut.WaitForAssertion(() => Assert.Equal(DIAGNOSE_NEU, cut.Find("textarea").TextContent));
        Assert.Equal(1, geschaltet);
    }

    [Fact]
    public void Liefert_die_Geste_nichts_bleibt_der_Text()
    {
        var anstoss = new UeberAnstoss();
        var cut = Zeige(schalten: () => null, anstoss: anstoss);

        cut.InvokeAsync(anstoss.Ausloesen);

        Assert.Equal(DIAGNOSE, cut.Find("textarea").TextContent);
    }

    [Fact]
    public void Nach_dem_Schliessen_bleibt_der_Anstoss_folgenlos()
    {
        var anstoss = new UeberAnstoss();
        int geschaltet = 0;
        var cut = Zeige(schalten: () => { geschaltet++; return DIAGNOSE_NEU; }, anstoss: anstoss);

        cut.Instance.Dispose();
        anstoss.Ausloesen();

        Assert.Equal(0, geschaltet);
    }

    [Fact]
    public void Die_Fussleiste_traegt_nur_Schliessen()
    {
        bool? geschlossen = null;
        var cut = Render<UeberDialog>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Geschlossen, (bool b) => geschlossen = b));

        var leiste = cut.Find(".epos-leiste");
        Assert.Single(leiste.QuerySelectorAll("button"));
        Assert.NotNull(leiste.QuerySelector(".epos-leiste-fueller"));
        Assert.Contains("epos-knopf--primaer", leiste.QuerySelector("button")!.ClassList);

        leiste.QuerySelector("button")!.Click();
        Assert.False(geschlossen);
    }
}
