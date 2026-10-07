using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// G5-3b (Abstimmung G5, B1): der Farbmodus „Befund“ der Gebäudeansicht — Knopf nur bei <see cref="GebaeudeAnsichtDaten.BefundWaehlbar"/>,
/// Legende mit Zahl, Fläche und Schalter je Befund, Grundrisskanten und Öffnungen in der Farbe der Tafel, Klick öffnet den
/// Steckbrief; der Steckbrief mit Befund (Farbpunkt, Name, Grund) und Herkunft der Fläche; die Szene des Moduls mit den
/// Befundbytes; der Kontrastmodus des Stilblatts über die Stufenschlüssel.
/// </summary>
public class GebaeudeBefundAnsichtTests : EposBunitContext
{
    private const string ZONE = "A1|EG|B";

    private static IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygon(params (double X, double Y)[] punkte)
        => new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)punkte.Select(p => new GebaeudeAnsichtPunkt(p.X, p.Y)).ToList() };

    private static readonly BauteilsteckbriefDaten SteckbriefWand = new()
    {
        Kennung = "aw-sued", Name = "Außenwand Süd", Art = "Außenwand", Stufe = Aufbaustufe.C, Flaeche = "12,5 m²",
        Befund = Bauteilbefundstufe.OhneEigenschaften, Befundgrund = "Kein U-Wert.",
        FlaechenherkunftSchluessel = "MENGENSATZ", Flaechenherkunft = "Mengensatz",
    };

    private static readonly BauteilsteckbriefDaten SteckbriefBoden = new()
    {
        Kennung = "bo-1", Name = "Bodenplatte", Art = "Bodenplatte", Stufe = Aufbaustufe.A, Flaeche = "48 m²",
    };

    private static readonly BauteilsteckbriefDaten SteckbriefFenster = new()
    {
        Kennung = "f-1", Name = "Fenster Süd", Art = "Fenster", Stufe = Aufbaustufe.Transparent, Transparent = true,
        Befund = Bauteilbefundstufe.KoerperUnlesbar, Befundgrund = "Körper mit offener Schale.",
        FlaechenherkunftSchluessel = "KOERPER", Flaechenherkunft = "Körper",
    };

    /// <summary>Ein Raum mit drei Dreiecken (R1 Außenwand, R3 Boden, R0 innen), eine Kante an der Außenwand, ein Fenster als Marke und Körper.</summary>
    private static GebaeudeAnsichtDaten Daten(bool mitBefund = true)
    {
        var raum = new GebaeudeAnsichtRaum("r-wohnen", "Wohnen", ZONE, true, false, "48 m²", Polygon((0, 0), (6, 0), (6, 8), (0, 8)));
        var datei = new GebaeudeAnsichtDateikoerper(new float[] { 0, 0, 0, 6, 0, 0, 6, 8, 0, 0, 8, 0 }, new[] { 0, 1, 2, 0, 2, 3, 0, 1, 3 },
                                                    new[] { 0, 1, 1, 2, 2, 3, 0, 3 }, 3, "FacetedBrep", Array.Empty<string>())
        { Gruppen = new byte[] { 1, 3, 0 } };
        var fenster = new GebaeudeAnsichtDateikoerper(new float[] { 1.5f, 0, 1, 3, 0, 1, 3, 0, 2 }, new[] { 0, 1, 2 }, new[] { 0, 1, 1, 2 }, 1,
                                                      "FacetedBrep", Array.Empty<string>());
        var d = new GebaeudeAnsichtDaten
        {
            Geschosse = new[] { new GebaeudeAnsichtGeschoss("g-eg", "Erdgeschoss", false, 0, 0, 6, 8, new[] { raum }) },
            Zonen = new[] { new GebaeudeAnsichtZone(ZONE, "Erdgeschoss", 0, true, false, false) },
            Koerperraeume = new[]
            {
                new GebaeudeAnsichtKoerperraum("r-wohnen", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null)
                {
                    Dateikoerper = datei, Herkunft = Koerperherkunft.Datei,
                    Kantengruppen = new[] { (IReadOnlyList<Randgruppe?>)new Randgruppe?[] { Randgruppe.R1, null, null, null } },
                    Kantenmarken = new[] { new GebaeudeAnsichtKantenmarke(0, 0, 0.25, 0.5, "f-1") },
                    Dreiecksbauteile = new string?[] { "aw-sued", "bo-1", null },
                    Kantenbauteile = new[] { (IReadOnlyList<string?>)new string?[] { "aw-sued", null, null, null } },
                },
            },
            Flaechengruppen = Enumerable.Range(0, GebaeudeAnsichtRandgruppen.ZAHL)
                .Select(i => new GebaeudeAnsichtFlaechengruppe((Randgruppe)i, i is 1 or 3 ? 48 : 0, i is 1 or 3 ? 1 : 0)).ToList(),
            Bauteilkoerper = new[] { new GebaeudeAnsichtBauteilkoerper("f-1", Randgruppe.R7, fenster) },
            Steckbriefe = new Dictionary<string, BauteilsteckbriefDaten>
            {
                ["aw-sued"] = SteckbriefWand, ["bo-1"] = SteckbriefBoden, ["f-1"] = SteckbriefFenster,
            },
        };
        if (!mitBefund) return d;
        return d with
        {
            Bauteilbefunde = new Dictionary<string, Bauteilbefundstufe>
            {
                ["aw-sued"] = Bauteilbefundstufe.OhneEigenschaften, ["bo-1"] = Bauteilbefundstufe.Ohne, ["f-1"] = Bauteilbefundstufe.KoerperUnlesbar,
            },
            Befundsummen = new[]
            {
                new GebaeudeAnsichtBefundsumme(Bauteilbefundstufe.Ohne, 1, 48),
                new GebaeudeAnsichtBefundsumme(Bauteilbefundstufe.KoerperUnlesbar, 1, 1.5),
                new GebaeudeAnsichtBefundsumme(Bauteilbefundstufe.OhneEigenschaften, 1, 12.5),
                new GebaeudeAnsichtBefundsumme(Bauteilbefundstufe.OhneBauteil, 0, 0),
            },
        };
    }

    private IRenderedComponent<GebaeudeAnsicht> Zeige(GebaeudeAnsichtDaten daten, List<int>? umhaengungen = null)
        => Render<GebaeudeAnsicht>(c =>
        {
            c.Add(x => x.Daten, daten);
            c.Add(x => x.RaumUmgehaengt, (EPOS.UI.Dialoge.Import.GebaeudeRaumumhaengung _) => umhaengungen?.Add(1));
        });

    private static IElement Knopf(IRenderedComponent<GebaeudeAnsicht> cut)
        => cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='befund']");

    private static IRenderedComponent<GebaeudeAnsicht> ImBefundmodus(IRenderedComponent<GebaeudeAnsicht> cut)
    {
        Knopf(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("befund", cut.Instance.Farbmodus));
        return cut;
    }

    // =====================================================================
    //  Knopf und Sperre
    // =====================================================================

    [Fact]
    public void Der_Knopf_Befund_ist_der_vierte_und_nur_mit_Befunden_waehlbar()
    {
        IRenderedComponent<GebaeudeAnsicht> ohne = Zeige(Daten(mitBefund: false));
        Assert.Equal(new[] { "zonen", "randbedingung", "aufbau", "befund" },
                     ohne.FindAll(".epos-gebansicht-farbmodus button").Select(b => b.GetAttribute("data-farbmodus")));
        IElement gesperrt = Knopf(ohne);
        Assert.True(gesperrt.HasAttribute("disabled"));
        Assert.Equal(new GebaeudeAnsichtTexte().BefundOhne, gesperrt.GetAttribute("title"));
        Assert.Equal("Befund", gesperrt.TextContent.Trim());
        gesperrt.Click();
        Assert.Equal("zonen", ohne.Instance.Farbmodus);

        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        Assert.False(Knopf(cut).HasAttribute("disabled"));
        Assert.Null(Knopf(cut).GetAttribute("title"));
        ImBefundmodus(cut);
        Assert.Equal("true", Knopf(cut).GetAttribute("aria-pressed"));
        Assert.Equal("befund", cut.Find(".epos-gebansicht svg").GetAttribute("data-farbmodus"));
    }

    // =====================================================================
    //  Legende und Grundriss
    // =====================================================================

    [Fact]
    public void Die_Legende_nennt_Zahl_und_Flaeche_je_Befund_und_schaltet()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = ImBefundmodus(Zeige(Daten()));
        IReadOnlyList<IElement> zeilen = cut.FindAll(".epos-gebansicht-befundeintrag").ToList();
        Assert.Equal(new[] { "ohne", "koerper", "eigenschaften" }, zeilen.Select(z => z.GetAttribute("data-befund")));
        Assert.Equal(new[] { "1 · 48,0 m²", "1 · 1,5 m²", "1 · 12,5 m²" },
                     zeilen.Select(z => z.QuerySelector(".epos-gebansicht-befund-zahl")!.TextContent.Trim()));
        Assert.Equal(new[] { "Ohne Befund", "Körper unlesbar", "Ohne Eigenschaften" },
                     zeilen.Select(z => z.QuerySelector(".epos-gebansicht-zonenname")!.TextContent.Trim()));
        // Die Farben kommen allein aus der Tafel; das Farbfeld trägt den Stufenschlüssel für den Kontrastmodus.
        for (int i = 0; i < zeilen.Count; i++)
        {
            IElement feld = zeilen[i].QuerySelector(".epos-gebansicht-farbfeld")!;
            Assert.Equal("background:" + GebaeudeAnsichtBefundstufen.FARBEN[i], feld.GetAttribute("style"));
            Assert.Equal(zeilen[i].GetAttribute("data-befund"), feld.GetAttribute("data-befund"));
        }

        // Der Schalter „ohne Eigenschaften" blendet die Kante der Außenwand aus.
        Assert.Single(cut.FindAll("line.epos-gebansicht-kante[data-bauteil='aw-sued']"));
        cut.Find(".epos-gebansicht-befundschalter[data-befund='eigenschaften']").Click();
        Assert.False(cut.Instance.BefundSichtbar(Bauteilbefundstufe.OhneEigenschaften));
        Assert.True(cut.Instance.BefundSichtbar(Bauteilbefundstufe.KoerperUnlesbar));
        Assert.Equal("false", cut.Find(".epos-gebansicht-befundschalter[data-befund='eigenschaften']").GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll("line.epos-gebansicht-kante[data-bauteil='aw-sued']"));
        Assert.Single(cut.FindAll("line.epos-gebansicht-oeffnung[data-bauteil='f-1']"));
    }

    [Fact]
    public void Der_Grundriss_faerbt_Kanten_und_Oeffnungen_nach_Befund_und_ein_Klick_oeffnet_den_Steckbrief()
    {
        var umhaengungen = new List<int>();
        IRenderedComponent<GebaeudeAnsicht> cut = ImBefundmodus(Zeige(Daten(), umhaengungen));
        IElement kante = cut.Find("line.epos-gebansicht-kante[data-bauteil='aw-sued']");
        Assert.Equal("eigenschaften", kante.GetAttribute("data-befund"));
        Assert.Equal(GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.OhneEigenschaften), kante.GetAttribute("stroke"));
        IElement oeffnung = cut.Find("line.epos-gebansicht-oeffnung[data-bauteil='f-1']");
        Assert.Equal("koerper", oeffnung.GetAttribute("data-befund"));
        Assert.Equal(GebaeudeAnsichtBefundstufen.Farbe(Bauteilbefundstufe.KoerperUnlesbar), oeffnung.GetAttribute("stroke"));
        Assert.Empty(cut.FindAll("line[data-stufe]"));

        kante.Click();
        Assert.Equal("aw-sued", cut.Instance.GezeigterSteckbrief?.Kennung);
        Assert.Empty(umhaengungen);
        IElement befund = cut.Find(".epos-steckbrief .epos-steckbrief-befund");
        Assert.Equal("eigenschaften", befund.GetAttribute("data-befund"));
        Assert.Equal("Ohne Eigenschaften", befund.QuerySelector(".epos-steckbrief-befundname")!.TextContent.Trim());
        Assert.Equal("Kein U-Wert.", befund.QuerySelector(".epos-steckbrief-befundgrund")!.TextContent.Trim());
        Assert.Equal("background:" + GebaeudeAnsichtBefundstufen.FARBEN[2], befund.QuerySelector(".epos-gebansicht-farbfeld")!.GetAttribute("style"));
        IElement herkunft = cut.Find(".epos-steckbrief .epos-steckbrief-flaechenherkunft");
        Assert.Equal("Mengensatz", herkunft.TextContent.Trim());
        Assert.Equal("MENGENSATZ", herkunft.GetAttribute("data-flaechenherkunft"));
    }

    [Fact]
    public async Task Ein_Klick_auf_Bauteilkoerper_und_Raumflaeche_oeffnet_den_Steckbrief()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = ImBefundmodus(Zeige(Daten()));
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(null, null, 1, "f-1", 0));
        cut.WaitForAssertion(() => Assert.Equal("f-1", cut.Instance.GezeigterSteckbrief?.Kennung));
        Assert.Equal((null, 1, "f-1"), cut.Instance.Treffer!.Value);
        Assert.Equal("koerper", cut.Find(".epos-steckbrief-befund").GetAttribute("data-befund"));
        Assert.Equal("Körper", cut.Find(".epos-steckbrief-flaechenherkunft").TextContent.Trim());

        // Das zweite Dreieck des Raums gehört der Bodenplatte: ihr Steckbrief, ohne Befund, ohne Herkunft der Fläche.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "r-wohnen", 0, null, 1));
        cut.WaitForAssertion(() => Assert.Equal("bo-1", cut.Instance.GezeigterSteckbrief?.Kennung));
        Assert.Equal("ohne", cut.Find(".epos-steckbrief-befund").GetAttribute("data-befund"));
        Assert.Empty(cut.FindAll(".epos-steckbrief-befundgrund"));
    }

    // =====================================================================
    //  Steckbrief allein
    // =====================================================================

    [Fact]
    public void Der_Steckbrief_zeigt_Befund_und_Herkunft_leer_bei_Handeingabe()
    {
        IRenderedComponent<Bauteilsteckbrief> cut = Render<Bauteilsteckbrief>(c => c.Add(x => x.Daten, SteckbriefBoden));
        IReadOnlyList<string> begriffe = cut.FindAll(".epos-steckbrief-werte dt").Select(d => d.TextContent.Trim()).ToList();
        Assert.Contains("Befund", begriffe);
        Assert.Contains("Herkunft der Fläche", begriffe);
        // Ohne Namen vom Wirt steht der Schlüssel; ohne Herkunft bleibt die Zeile leer und ohne Wert.
        Assert.Equal("ohne", cut.Find(".epos-steckbrief-befundname").TextContent.Trim());
        IElement herkunft = cut.Find(".epos-steckbrief-flaechenherkunft");
        Assert.Equal("", herkunft.TextContent.Trim());
        Assert.False(herkunft.HasAttribute("data-flaechenherkunft"));

        IRenderedComponent<Bauteilsteckbrief> fenster = Render<Bauteilsteckbrief>(c =>
        {
            c.Add(x => x.Daten, SteckbriefFenster);
            c.Add(x => x.BefundName, "Körper unlesbar");
        });
        Assert.Equal("Körper unlesbar", fenster.Find(".epos-steckbrief-befundname").TextContent.Trim());
        Assert.Equal("Körper mit offener Schale.", fenster.Find(".epos-steckbrief-befundgrund").TextContent.Trim());
        Assert.Equal("koerper", fenster.Find(".epos-steckbrief-befund .epos-gebansicht-farbfeld").GetAttribute("data-befund"));
    }

    // =====================================================================
    //  Szene und Stilblatt
    // =====================================================================

    [Fact]
    public void Die_Szene_traegt_Befundbytes_Farbtafel_und_Schalter()
    {
        GebaeudeAnsichtDaten d = Daten();
        GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
        int ab = f.Verzeichnis[0].BefundAb;
        Assert.True(ab > 0);
        Assert.Equal(new byte[] { 2, 0, GebaeudeAnsichtBefundstufen.INNEN_NEUTRAL }, f.Bytes.Skip(ab).Take(3));

        string json = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.SzeneDatei(d, ZONE, f, "befund", null, true, null,
                                                                                             new[] { true, false, true, true }));
        Assert.Contains("\"befundAb\":" + ab, json);
        Assert.Contains("\"befundfarben\":[\"" + string.Join("\",\"", GebaeudeAnsichtBefundstufen.FARBEN) + "\"]", json);
        Assert.Contains("\"befundsichtbar\":[true,false,true,true]", json);
        Assert.Contains("\"befund\":1}", json);
        Assert.Contains("\"farbmodus\":\"befund\"", json);
        Assert.DoesNotContain("Außenwand", json);

        string prismen = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.Szene(d, ZONE, "befund"));
        Assert.Contains("\"befundsichtbar\":[true,true,true,true]", prismen);
    }

    [Fact]
    public void Der_Kontrastmodus_mustert_den_Befund_ueber_die_Stufenschluessel()
    {
        string css = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"));
        int block = css.IndexOf("Befund ueber den Stufenschluessel", StringComparison.Ordinal);
        Assert.True(block > 0);
        int anfang = css.LastIndexOf("@media (forced-colors: active)", block, StringComparison.Ordinal);
        int ende = css.IndexOf("\n}", block, StringComparison.Ordinal);
        string regeln = css[anfang..ende];
        foreach (Bauteilbefundstufe s in new[] { Bauteilbefundstufe.KoerperUnlesbar, Bauteilbefundstufe.OhneEigenschaften, Bauteilbefundstufe.OhneBauteil })
            Assert.Contains(".epos-gebansicht-farbfeld[data-befund=\"" + GebaeudeAnsichtBefundstufen.Schluessel(s) + "\"]", regeln);
        Assert.Contains(".epos-gebansicht-kante[data-befund=\"koerper\"]", regeln);
        Assert.DoesNotContain("#", regeln);   // im Kontrastmodus nur Systemfarben
        // Die vier Farben der Legende sind verschieden.
        Assert.Equal(GebaeudeAnsichtBefundstufen.ZAHL, GebaeudeAnsichtBefundstufen.FARBEN.Take(GebaeudeAnsichtBefundstufen.ZAHL).Distinct().Count());
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Repowurzel nicht gefunden");
    }
}
