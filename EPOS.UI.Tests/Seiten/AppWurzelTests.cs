using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// AppWurzel - die Zustandsmaschine der iOS-Huelle (iU10-2).
///
/// Geprueft wird das, was die Huelle von ihr erwartet: Sie zeigt beim Start die
/// Liste, schaltet auf Knopfdruck in einen Dialog, kommt von dort zurueck und
/// laedt dabei nach. Der Kern kommt in diesen Tests nicht vor - die Daten
/// liefert <see cref="TestProjektquelle"/>.
///
/// UI-Kultur auf de-DE gepinnt wie in SpeichernLeisteTests: Die Beschriftungen
/// der Dialoge stammen aus dem Ressourcenkatalog des Kerns, und die CI-Laeufer
/// auf macOS und Windows laufen englisch.
/// </summary>
public class AppWurzelTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    private static readonly ProjektZeile[] ZweiProjekte =
    {
        new ProjektZeile(1030, "B3-Kaskade", "Region 12", "WP+BHKW"),
        new ProjektZeile(1007, "Speichervariante A", "Region 12", "WP+Speicher")
    };

    public AppWurzelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    private IRenderedComponent<AppWurzel> Aufbauen(TestProjektquelle quelle)
    {
        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>();
    }

    [Fact]
    public void Beim_Start_steht_die_Projektliste()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Empty(cut.FindAll(".epos-dialog"));
        Assert.Equal(2, cut.Instance.ProjektAnzahl);
    }

    [Fact]
    public void Der_Klick_schaltet_von_der_Liste_in_den_Dialog()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.FindAll(".epos-projekt-energie")[0].Click();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Equal("Energieträger Variante", cut.Find(".epos-dialog-titel").TextContent);
    }

    [Fact]
    public void Abbrechen_fuehrt_zurueck_zur_Liste_und_laedt_neu()
    {
        var quelle = new TestProjektquelle(ZweiProjekte);
        var cut = Aufbauen(quelle);
        int vorher = quelle.Geladen;

        cut.FindAll(".epos-projekt-energie")[0].Click();
        // Im Dialog ist Abbrechen der erste Knopf der Speichernleiste.
        cut.FindAll(".epos-dialog button").Last(k => !k.ClassList.Contains("epos-knopf--primaer")).Click();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Equal(vorher + 1, quelle.Geladen);
        Assert.Null(quelle.Uebernommen);
    }

    [Fact]
    public void Ein_OK_reicht_das_Ergebnis_an_die_Huelle_weiter()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { Antwort = "Erdgas Sondertarif" };
        var cut = Aufbauen(quelle);

        cut.FindAll(".epos-projekt-energie")[1].Click();
        cut.Find(".epos-dialog input[type=text]").Input("Erdgas Sondertarif");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(quelle.Uebernommen);
        Assert.Equal("Erdgas Sondertarif", quelle.Uebernommen!.VariantenName);
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("Erdgas Sondertarif", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ohne_BHKW_Daten_bleibt_die_Liste_stehen_und_sagt_warum()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.FindAll(".epos-projekt-bhkw")[0].Click();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("keine BHKW-Vergleichsgruppe", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Mit_BHKW_Daten_geht_der_zweite_Dialog_auf()
    {
        var daten = new BhkwDialogDaten(
            IdStamm: 1030,
            StammName: "B3-Kaskade",
            Anlagen: new List<KwkgAnlagenAngabe>(),
            Parameter: new WirtschaftlichkeitParameter(),
            HatHeizkessel: false,
            Doppelpflege: Array.Empty<KohaerenzHinweis>(),
            Katalog: null,
            ErgebnisseLaden: null,
            Speichern: null);

        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte, bhkw: daten));

        cut.FindAll(".epos-projekt-bhkw")[0].Click();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Contains("B3-Kaskade", cut.Find(".epos-dialog-titel").TextContent);
    }

    [Fact]
    public void Die_Wurzel_meldet_sich_als_Navigationsziel_an()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.Same(cut.Instance, Navigationsziel.Aktuell);
        Assert.True(Navigationsziel.Aktuell!.OeffneMaske(Seitenschluessel.Energietraeger));
        Assert.False(Navigationsziel.Aktuell.OeffneMaske("GIBT_ES_NICHT"));
    }

    // =====================================================================
    //  K7 (iU9-W16c.2) - die drei Ansichten des Rahmens
    // =====================================================================

    [Fact]
    public void Die_Wurzel_traegt_die_Schale_ueber_jeder_Ansicht()
    {
        // Entscheid E-1: eine Wurzel, zwei Schalen. Unter Windows ist die
        // Kopfleiste das Menueband; auf iOS gibt es keine.
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(ZweiProjekte));
        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Kopfleiste,
                 (RenderFragment)(b => b.AddMarkupContent(0, "<div id=\"schale\">Menue</div>"))));

        Assert.NotNull(cut.Find("#schale"));
        Assert.Single(cut.FindAll(".epos-seite"));

        cut.Instance.OeffneMaske(Seitenschluessel.Energietraeger);
        cut.Render();

        // Auch ueber dem Dialog steht die Schale - ein Menue verschwindet nicht.
        Assert.NotNull(cut.Find("#schale"));
    }

    [Fact]
    public void Ohne_Startseitengaben_bleibt_die_Liste_stehen_und_sagt_warum()
    {
        // Der Zustand der iOS-Huelle vor iU11 - derselbe Umgang wie beim
        // Assistenten und beim KI-Chat.
        var quelle = new TestProjektquelle(ZweiProjekte);
        var cut = Aufbauen(quelle);

        cut.Instance.OeffneMaske(Seitenschluessel.Startseite);
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-startseite"));
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("Startseite", cut.Find(".epos-warnbanner").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Mit_Startseitengaben_loest_die_Startseite_die_Liste_ab()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            }
        };
        var cut = Aufbauen(quelle);

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.Startseite));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    [Fact]
    public void Die_Startansicht_ist_zugleich_das_Ziel_des_Rueckwegs()
    {
        // Unter Windows fuehrt "Schliessen" eines Dialogs zurueck auf die
        // STARTSEITE, auf iOS auf die Projektliste. Es ist derselbe Weg.
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            }
        };
        Services.AddSingleton<IProjektQuelle>(quelle);
        var cut = Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));

        Assert.Single(cut.FindAll(".epos-startseite"));

        cut.Instance.OeffneMaske(Seitenschluessel.Energietraeger);
        cut.Render();
        Assert.Empty(cut.FindAll(".epos-startseite"));

        // Der Dialog meldet sein Ende - danach steht wieder die Startseite,
        // nicht die Liste. (Abbrechen ist der letzte nicht-primaere Knopf der
        // Speichernleiste - derselbe Griff wie in den Faellen oben.)
        cut.FindAll(".epos-dialog button").Last(k => !k.ClassList.Contains("epos-knopf--primaer")).Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
    }
}
