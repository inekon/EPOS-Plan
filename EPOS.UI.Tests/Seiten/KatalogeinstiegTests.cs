using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der KATALOGEINSTIEG der Projektliste (Umsetzungskonzept Zapfprofilgenerator,
/// Kapitel 9 ZU34): Auf einer Plattform ohne Menüband (iOS) öffnet der Knopf
/// „Kataloge…" im Seitenkopf die Katalogverwaltungen, die die Wurzel führt.
///
/// <para>Die Einträge kommen aus der <see cref="Menuetabelle"/> (Kennzeichen
/// <see cref="Menuepunkt.Katalog"/>), gefiltert durch die Positivliste der Wurzel
/// (<see cref="AppWurzel.FuehrtZiel"/>) — keine zweite Liste. Geprüft wird über
/// Klassen und <c>data-ziel</c>, nicht über Klartext (kulturunabhängig).</para>
/// </summary>
public class KatalogeinstiegTests : EposBunitContext
{
    private static readonly ProjektZeile[] ZweiProjekte =
    {
        new ProjektZeile(1030, "B3-Kaskade", "Region 12", "WP+BHKW"),
        new ProjektZeile(1007, "Speichervariante A", "Region 12", "WP+Speicher")
    };

    /// <summary>Die drei Kataloge, die die Wurzel führt — in der Reihenfolge des Menübaums.</summary>
    private static readonly string[] DreiKataloge =
    {
        Seitenschluessel.BaustoffKatalog,
        Seitenschluessel.BauteilaufbauKatalog,
        Seitenschluessel.BrauchwasserNutzungsarten
    };

    public KatalogeinstiegTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    // =====================================================================
    //  Die Datenquelle: Menuetabelle + Positivliste der Wurzel
    // =====================================================================

    [Fact]
    public void Die_Wurzel_gibt_genau_die_drei_Kataloge_frei()
    {
        string[] ziele = Menuetabelle.Kataloge(AppWurzel.FuehrtZiel).Select(p => p.Ziel).ToArray();

        Assert.Equal(DreiKataloge, ziele);
    }

    [Fact]
    public void Die_Menuetabelle_kennzeichnet_die_zwoelf_Katalogverwaltungen()
    {
        IReadOnlyList<Menuepunkt> alle = Menuetabelle.Kataloge(_ => true);

        Assert.Equal(12, alle.Count);
        Assert.All(alle, p => Assert.False(string.IsNullOrEmpty(p.Ziel)));
        Assert.All(alle, p => Assert.False(p.Klappt));
        Assert.Equal(alle.Count, alle.Select(p => p.Ziel).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Jeder_gekennzeichnete_Punkt_hat_eine_Beschriftung_in_beiden_Sprachen()
    {
        foreach (Menuepunkt p in Menuetabelle.Kataloge(_ => true))
        {
            string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager
                .GetString(p.TextSchluessel, new System.Globalization.CultureInfo("de-DE")) ?? "";
            string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager
                .GetString(p.TextSchluessel, new System.Globalization.CultureInfo("en-US")) ?? "";
            Assert.False(string.IsNullOrWhiteSpace(de), p.TextSchluessel);
            Assert.False(string.IsNullOrWhiteSpace(en), p.TextSchluessel);
        }
    }

    // =====================================================================
    //  Die Projektliste allein
    // =====================================================================

    [Fact]
    public void Ohne_Kataloge_steht_kein_Knopf()
    {
        var cut = Render<Projektliste>(p => p.Add(x => x.Zeilen, ZweiProjekte));

        Assert.Empty(cut.FindAll(".epos-katalogeinstieg"));
    }

    [Fact]
    public void Ein_einziger_Katalog_oeffnet_unmittelbar_ohne_Liste()
    {
        string? gemeldet = null;
        var cut = Render<Projektliste>(p => p
            .Add(x => x.Zeilen, ZweiProjekte)
            .Add(x => x.Kataloge, new[] { (Seitenschluessel.BaustoffKatalog, "Baustoffe") })
            .Add(x => x.KatalogOeffnen, (string z) => gemeldet = z));

        var knopf = cut.Find(".epos-katalogeinstieg");
        Assert.Contains("Baustoffe", knopf.TextContent);
        Assert.Null(knopf.GetAttribute("aria-haspopup"));

        knopf.Click();

        Assert.Equal(Seitenschluessel.BaustoffKatalog, gemeldet);
        Assert.Empty(cut.FindAll(".epos-katalogwahl"));
    }

    [Fact]
    public void Ab_zwei_Katalogen_klappt_eine_Liste_auf_und_meldet_die_Wahl()
    {
        string? gemeldet = null;
        var cut = Render<Projektliste>(p => p
            .Add(x => x.Zeilen, ZweiProjekte)
            .Add(x => x.Kataloge, new[]
            {
                (Seitenschluessel.BaustoffKatalog, "A"),
                (Seitenschluessel.BrauchwasserNutzungsarten, "B")
            })
            .Add(x => x.KatalogOeffnen, (string z) => gemeldet = z));

        Assert.Empty(cut.FindAll(".epos-katalogwahl"));
        cut.Find(".epos-katalogeinstieg").Click();

        var eintraege = cut.FindAll(".epos-katalogwahl-eintrag");
        Assert.Equal(2, eintraege.Count);
        Assert.Equal("true", cut.Find(".epos-katalogeinstieg").GetAttribute("aria-expanded"));

        eintraege[1].Click();

        Assert.Equal(Seitenschluessel.BrauchwasserNutzungsarten, gemeldet);
        Assert.Empty(cut.FindAll(".epos-katalogwahl"));
    }

    // =====================================================================
    //  In der Wurzel
    // =====================================================================

    private IRenderedComponent<AppWurzel> Aufbauen(TestProjektquelle quelle)
    {
        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>();
    }

    [Fact]
    public void Ohne_Kopfleiste_listet_der_Einstieg_genau_die_freigegebenen_Kataloge()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.Find(".epos-katalogeinstieg").Click();

        string[] ziele = cut.FindAll(".epos-katalogwahl-eintrag")
                            .Select(e => e.GetAttribute("data-ziel") ?? "").ToArray();
        Assert.Equal(DreiKataloge, ziele);
    }

    [Fact]
    public void Mit_Kopfleiste_steht_kein_Einstieg()
    {
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(ZweiProjekte));
        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Kopfleiste,
                 (RenderFragment)(b => b.AddMarkupContent(0, "<div id=\"schale\">Menue</div>"))));

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-katalogeinstieg"));
    }

    [Fact]
    public void Die_Wahl_oeffnet_den_Katalog_ueber_die_Wurzel()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { NutzungsartKatalog = new Dictionary<string, object>() };
        var cut = Aufbauen(quelle);

        cut.Find(".epos-katalogeinstieg").Click();
        cut.Find($".epos-katalogwahl-eintrag[data-ziel=\"{Seitenschluessel.BrauchwasserNutzungsarten}\"]").Click();
        cut.Render();

        Assert.Equal(1, quelle.NutzungsartKatalogGefragt);
        Assert.Single(cut.FindAll(".epos-tww-katalog"));
        Assert.Empty(cut.FindAll(".epos-seite"));
    }

    [Theory]
    [InlineData(Seitenschluessel.BaustoffKatalog, "epos-baustoff-admin")]
    [InlineData(Seitenschluessel.BauteilaufbauKatalog, "epos-bauteilaufbau-admin")]
    public void Die_Kataloge_der_Gebaeudehuelle_oeffnen_aus_dem_Einstieg(string ziel, string klasse)
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            BaustoffKatalog = new Dictionary<string, object>(),
            BauteilaufbauKatalog = new Dictionary<string, object>()
        };
        var cut = Aufbauen(quelle);

        cut.Find(".epos-katalogeinstieg").Click();
        cut.Find($".epos-katalogwahl-eintrag[data-ziel=\"{ziel}\"]").Click();
        cut.Render();

        Assert.Single(cut.FindAll("." + klasse));
    }

    [Fact]
    public void Ohne_Parametersatz_bleibt_die_Liste_stehen_und_nennt_den_Grund()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.Find(".epos-katalogeinstieg").Click();
        cut.Find($".epos-katalogwahl-eintrag[data-ziel=\"{Seitenschluessel.BrauchwasserNutzungsarten}\"]").Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains(cut.Instance.KeinNutzungsartKatalogText, cut.Find(".epos-warnbanner").TextContent);
    }
}
