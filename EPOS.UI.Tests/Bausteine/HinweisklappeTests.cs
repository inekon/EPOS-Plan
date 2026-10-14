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

    // =====================================================================
    //  Die Klapp-Bauform des Hauses: Hinweise als Liste, Rollbereich, Klang
    // =====================================================================

    private static readonly string[] DreiHinweise = { "Erster", "Zweiter", "Dritter" };

    /// <summary>
    /// Mit Hinweisen zaehlt die Liste: aufgeklappt ein Eintrag je Hinweis im Rollbereich,
    /// aria-controls zeigt auf ihn.
    /// </summary>
    [Fact]
    public void Hinweise_als_Liste_stehen_im_Rollbereich()
    {
        var cut = Render<Hinweisklappe>(p => p
            .Add(x => x.Hinweise, DreiHinweise)
            .Add(x => x.TextEiner, "1 Hinweis")
            .Add(x => x.TextMehrere, "{0} Hinweise"));

        Assert.Equal("3", cut.Find(".epos-simerg-laufband-zahl").TextContent);
        Assert.Equal("3 Hinweise", cut.Find(".epos-simerg-laufband-text").TextContent);
        Assert.Empty(cut.FindAll(".epos-hinweisklappe-inhalt"));

        cut.Find("button").Click();

        var knopf = cut.Find("button");
        var liste = cut.Find("ul.epos-hinweisklappe-inhalt.epos-hinweisklappe-liste");
        Assert.Equal("true", knopf.GetAttribute("aria-expanded"));
        Assert.Equal(liste.Id, knopf.GetAttribute("aria-controls"));
        Assert.Equal(3, liste.QuerySelectorAll("li").Length);
    }

    /// <summary>Beliebiger Inhalt steht ebenso im Rollbereich.</summary>
    [Fact]
    public void Beliebiger_Inhalt_steht_im_Rollbereich()
    {
        var cut = Zeige(1);
        Assert.Single(cut.FindAll(".epos-hinweisklappe-inhalt > ul.liste"));
    }

    /// <summary>Offen setzt die Anfangsstellung vor der Zahlregel.</summary>
    [Fact]
    public void Offen_setzt_die_Anfangsstellung()
    {
        var cut = Render<Hinweisklappe>(p => p
            .Add(x => x.Hinweise, DreiHinweise)
            .Add(x => x.Offen, true));
        Assert.True(cut.Instance.IstOffen);
        Assert.Single(cut.FindAll("ul.epos-hinweisklappe-liste"));
    }

    /// <summary>
    /// Frei stehend traegt das Band den Farbklang seiner Stufe (ohne Stufe der der
    /// Warnung); eingebettet keinen Klang und kein Zeichen - es erbt den des Banners.
    /// </summary>
    [Theory]
    [InlineData(WarnStufe.Hinweis, "epos-hinweisklappe--hinweis")]
    [InlineData(WarnStufe.Warnung, "epos-hinweisklappe--warnung")]
    [InlineData(WarnStufe.Fehler, "epos-hinweisklappe--fehler")]
    [InlineData(WarnStufe.Erfolg, "epos-hinweisklappe--erfolg")]
    public void Frei_stehend_traegt_das_Band_den_Klang_der_Stufe(WarnStufe stufe, string klasse)
    {
        var frei = Render<Hinweisklappe>(p => p.Add(x => x.Hinweise, DreiHinweise).Add(x => x.Stufe, stufe));
        Assert.Contains(klasse, frei.Find("button").ClassList);
        Assert.Single(frei.FindAll(".epos-hinweisklappe-symbol"));

        var eingebettet = Render<Hinweisklappe>(p => p.Add(x => x.Hinweise, DreiHinweise)
                                                    .Add(x => x.Stufe, stufe).Add(x => x.Eingebettet, true));
        Assert.Contains("epos-hinweisklappe--eingebettet", eingebettet.Find("button").ClassList);
        Assert.DoesNotContain(klasse, eingebettet.Find("button").ClassList);
        Assert.Empty(eingebettet.FindAll(".epos-hinweisklappe-symbol"));
    }

    /// <summary>
    /// Die Stilregeln der Bauform (bunit misst keine Hoehe - geprueft wird die Regel): der
    /// Rollbereich ist begrenzt und rollt innen; die Klaenge stehen in denselben Regeln wie
    /// die des Warnbanners, auch im Kontrastmodus; die Zeile setzt selbst keine Farbe.
    /// </summary>
    [Fact]
    public void Die_Stilregeln_der_Bauform_stehen_einmal()
    {
        string css = StilblattLesen();
        string inhalt = Regel(css, "\n.epos-hinweisklappe-inhalt {");
        Assert.Contains("max-height: 40vh", inhalt);
        Assert.Contains("overflow-y: auto", inhalt);

        foreach (string stufe in new[] { "hinweis", "warnung", "fehler", "erfolg" })
        {
            string paar = ".epos-warnbanner--" + stufe + ",\n";
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(
                css, System.Text.RegularExpressions.Regex.Escape(paar) + @"\s*\.epos-hinweisklappe--" + stufe + " {").Count);
        }

        string band = Regel(css, ".epos-simerg-laufband {");
        Assert.DoesNotContain("background", band);
        Assert.DoesNotContain("color:", band);

        // Keine Doppelregeln der alten Klappe des Banners.
        Assert.DoesNotContain(".epos-warnbanner-klapper {", css);
        Assert.DoesNotContain(".epos-warnbanner-liste {", css);
        Assert.DoesNotContain(".epos-warnbanner-zahl {", css);
    }

    private static string Regel(string css, string kopf)
    {
        int i = css.IndexOf(kopf, System.StringComparison.Ordinal);
        Assert.True(i >= 0, "Regel " + kopf + " fehlt");
        return css.Substring(i, css.IndexOf('}', i) - i);
    }

    private static string StilblattLesen()
    {
        string? ordner = System.AppContext.BaseDirectory;
        while (ordner != null && !System.IO.File.Exists(System.IO.Path.Combine(ordner, "EPOS.UI", "wwwroot", "epos-ui.css")))
            ordner = System.IO.Path.GetDirectoryName(ordner);
        Assert.NotNull(ordner);
        return System.IO.File.ReadAllText(System.IO.Path.Combine(ordner!, "EPOS.UI", "wwwroot", "epos-ui.css"))
                             .Replace("\r\n", "\n");
    }
}
