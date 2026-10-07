using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Probe 42 — gleicher Umfang wie IFC</b> (Datenaustauschkonzept 17.5 und 17.8), Ansicht: Ein Raum mit einem aus Flächen
/// gebildeten Körper (Projektdatei, gbXML) trägt das Kennzeichen „aus Flächen gebildet“ in Kennzeichenzeile und Raumliste, die
/// Farbmodi Randbedingung, Aufbau und Befund sind wählbar, ein Klick auf ein Dreieck öffnet den Steckbrief des Bauteils seiner
/// Quellfläche, und der Steckbrief trägt die Zeile „Körper“ mit Weg und Vermerken.
/// </summary>
public class GebaeudeAnsichtAbgeleitetTests : EposBunitContext
{
    private const string ZONE = "A1|EG|B";

    private static readonly BauteilsteckbriefDaten SteckbriefWand = new()
    {
        Kennung = "AW", Name = "Außenwand Süd", Art = "Außenwand", Stufe = Aufbaustufe.A, Flaeche = "10 m²",
    };

    private static readonly BauteilsteckbriefDaten SteckbriefBoden = new()
    {
        Kennung = "BP", Name = "Bodenplatte", Art = "Bodenplatte", Stufe = Aufbaustufe.A, Flaeche = "20 m²",
    };

    /// <summary>Ein Raum mit einem gebildeten Körper aus drei Dreiecken (Außenwand, Boden, Ersatzkennung ohne Bauteil).</summary>
    private static GebaeudeAnsichtDaten Daten()
    {
        var punkte = new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)new[] { new GebaeudeAnsichtPunkt(0, 0), new GebaeudeAnsichtPunkt(4, 0),
                                                                           new GebaeudeAnsichtPunkt(4, 5), new GebaeudeAnsichtPunkt(0, 5) } };
        var raum = new GebaeudeAnsichtRaum("R1", "Raum Eins", ZONE, true, false, "20 m²", punkte);
        var koerper = new GebaeudeAnsichtDateikoerper(new float[] { 0, 0, 0, 4, 0, 0, 4, 5, 0, 0, 5, 0 }, new[] { 0, 1, 2, 0, 2, 3, 0, 1, 3 },
                                                      new[] { 0, 1, 1, 2, 2, 3, 0, 3 }, 3, "Raumpolygon", new[] { "Vorgabedicke" })
        { Gruppen = new byte[] { 1, 3, 1 } };
        return new GebaeudeAnsichtDaten
        {
            Geschosse = new[] { new GebaeudeAnsichtGeschoss("FE", "Erdgeschoss", false, 0, 0, 4, 5, new[] { raum }) },
            Zonen = new[] { new GebaeudeAnsichtZone(ZONE, "Erdgeschoss", 0, true, false, false) },
            Koerperraeume = new[]
            {
                new GebaeudeAnsichtKoerperraum("R1", 2.5, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null)
                {
                    Dateikoerper = koerper, Herkunft = Koerperherkunft.Abgeleitet,
                    Kantengruppen = new[] { (IReadOnlyList<Randgruppe?>)new Randgruppe?[] { Randgruppe.R1, null, null, null } },
                    Dreiecksbauteile = new string?[] { "AW", "BP", null },
                    Kantenbauteile = new[] { (IReadOnlyList<string?>)new string?[] { "AW", null, null, null } },
                },
            },
            Flaechengruppen = Enumerable.Range(0, GebaeudeAnsichtRandgruppen.ZAHL)
                .Select(i => new GebaeudeAnsichtFlaechengruppe((Randgruppe)i, i is 1 or 3 ? 10 : 0, i is 1 or 3 ? 1 : 0)).ToList(),
            Steckbriefe = new Dictionary<string, BauteilsteckbriefDaten> { ["AW"] = SteckbriefWand, ["BP"] = SteckbriefBoden },
            Bauteilstufen = new Dictionary<string, Aufbaustufe> { ["AW"] = Aufbaustufe.A, ["BP"] = Aufbaustufe.A },
            Bauteilbefunde = new Dictionary<string, Bauteilbefundstufe> { ["AW"] = Bauteilbefundstufe.Ohne, ["BP"] = Bauteilbefundstufe.Ohne },
            Koerperwege = new Dictionary<string, GebaeudeAnsichtKoerperweg>
            {
                ["AW"] = new GebaeudeAnsichtKoerperweg(true, "Flaechenextrusion", new[] { "Vorgabedicke" }),
                ["BP"] = new GebaeudeAnsichtKoerperweg(true, "Raumpolygon", Array.Empty<string>()),
            },
        };
    }

    private IRenderedComponent<GebaeudeAnsicht> Zeige(GebaeudeAnsichtDaten daten)
        => Render<GebaeudeAnsicht>(c => c.Add(x => x.Daten, daten));

    [Fact]
    public void Kennzeichen_aus_Flaechen_gebildet_in_Zeile_und_Raumliste()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        cut.FindAll("button[role=tab]").First(k => k.TextContent.Trim().StartsWith("Körper", StringComparison.Ordinal)).Click();
        Assert.Equal("dateikoerper", cut.Instance.KoerperModus);
        IElement zeile = cut.Find(".epos-gebansicht-kennzeichen");
        Assert.Equal(("0", "1"), (zeile.GetAttribute("data-datei"), zeile.GetAttribute("data-abgeleitet")));
        Assert.Equal("1 Räume aus Flächen der Datei gebildet, 0 aus Umriss, 0 schematisch", zeile.TextContent);
        IElement r1 = cut.Find(".epos-gebansicht-raumherkunft li[data-raum='R1']");
        Assert.Equal("abgeleitet", r1.GetAttribute("data-herkunft"));
        Assert.Contains("aus Flächen gebildet", r1.GetAttribute("title"));
        Assert.Equal("Vorgabedicke", cut.Find(".epos-gebansicht-vereinfacht").GetAttribute("data-vermerke"));
    }

    [Fact]
    public void Die_Farbmodi_sind_wie_bei_IFC_waehlbar()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        foreach (string modus in new[] { "randbedingung", "aufbau", "befund" })
            Assert.False(cut.Find($".epos-gebansicht-farbmodus button[data-farbmodus='{modus}']").HasAttribute("disabled"), modus);
    }

    [Fact]
    public async Task Klick_auf_ein_Dreieck_oeffnet_den_Steckbrief_der_Quellflaeche_mit_Zeile_Koerper()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='befund']").Click();
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "R1", 1, null, 0));
        cut.WaitForAssertion(() => Assert.Equal("AW", cut.Instance.GezeigterSteckbrief?.Kennung));
        IElement koerper = cut.Find(".epos-steckbrief-koerper");
        Assert.Equal("abgeleitet", koerper.GetAttribute("data-koerper"));
        Assert.Equal("aus Flächen der Datei gebildet (Extrusion der Fläche) — vereinfacht: " + new GebaeudeAnsichtTexte().Vermerk("Vorgabedicke"),
                     koerper.TextContent.Trim());

        // Das zweite Dreieck gehört der Bodenplatte (Raumpolygon, ohne Vermerk); das dritte trägt kein Bauteil.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "R1", 3, null, 1));
        cut.WaitForAssertion(() => Assert.Equal("BP", cut.Instance.GezeigterSteckbrief?.Kennung));
        Assert.Equal("aus Flächen der Datei gebildet (Raumpolygon)", cut.Find(".epos-steckbrief-koerper").TextContent.Trim());
    }

    [Fact]
    public void Der_Steckbrief_ohne_Koerper_hat_keine_Zeile_Koerper()
    {
        IRenderedComponent<Bauteilsteckbrief> cut = Render<Bauteilsteckbrief>(c => c.Add(x => x.Daten, SteckbriefWand));
        Assert.Empty(cut.FindAll(".epos-steckbrief-koerper"));
        IRenderedComponent<Bauteilsteckbrief> mit = Render<Bauteilsteckbrief>(c =>
        {
            c.Add(x => x.Daten, SteckbriefWand);
            c.Add(x => x.Koerper, "aus der Datei (FacetedBrep)");
            c.Add(x => x.KoerperAbgeleitet, false);
        });
        IElement zeile = mit.Find(".epos-steckbrief-koerper");
        Assert.Equal("datei", zeile.GetAttribute("data-koerper"));
        Assert.Equal("Körper", zeile.PreviousElementSibling!.TextContent.Trim());
    }
}
