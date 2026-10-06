using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// BA-3 (Konzept Bauteilaufbau beim Import 5.4): Farbmodus „Aufbau" der Gebäudeansicht (Umschalter, Legende außen/innen,
/// Sperre, Körperfeld und Szene), der Bauteilsteckbrief (Klick auf Bauteilkörper, Raumfläche und Kante; Schichtliste mit
/// weggelassener Schicht, Ersatzaufbau, Fenster, Herkunft „Projektdatei"; keine Schreibaufrufe) und die Spalte „Aufbau" samt
/// Filter im Zonendialog.
/// </summary>
public class GebaeudeAufbauTests : EposBunitContext
{
    private const string ZONE = "A1|EG|B";

    private static IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygon(params (double X, double Y)[] punkte)
        => new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)punkte.Select(p => new GebaeudeAnsichtPunkt(p.X, p.Y)).ToList() };

    private static readonly BauteilsteckbriefDaten SteckbriefWand = new()
    {
        Kennung = "aw-sued", Name = "Außenwand Süd", Art = "Außenwand", Stufe = Aufbaustufe.A, Flaeche = "12,5 m²", Azimut = "180°",
        Neigung = "90°", Randbedingung = "Außenluft", UWert = "0,28 W/(m²K)", UHerkunft = "aus Datei", UHerkunftSchluessel = SteckbriefHerkunft.Datei,
        UHinweis = "U der Datei weicht ab.", R1 = "0,0012 K/W", C1 = "620,5 kJ/K", Kapazitaet = "504,2 kJ/(m²K)",
        Aufbau = "AW massiv", AufbauHerkunft = "aus Projektdatei", AufbauHerkunftSchluessel = SteckbriefHerkunft.Projektdatei,
        Schichten = new[]
        {
            new BauteilsteckbriefSchicht("Gipsputz", "15 mm", "0,51 W/(mK)", "1200 kg/m³", "1000 J/(kgK)", "aus Projektdatei", SteckbriefHerkunft.Projektdatei)
                { Materialschluessel = "gipsputz" },
            new BauteilsteckbriefSchicht("Beton", "200 mm", "2 W/(mK)", "2400 kg/m³", "1000 J/(kgK)", "aus Katalog", SteckbriefHerkunft.Katalog),
            new BauteilsteckbriefSchicht("PE-Folie", "0,2 mm", "–", "–", "–", "aus Datei", SteckbriefHerkunft.Datei) { Weggelassen = true, Grund = "dünn" },
        },
        IdBauteil = 41,
    };

    private static readonly BauteilsteckbriefDaten SteckbriefBoden = new()
    {
        Kennung = "bo-1", Name = "Bodenplatte", Art = "Bodenplatte", Stufe = Aufbaustufe.C, UWert = "0,35 W/(m²K)", UHerkunft = "Vorgabe",
        UHerkunftSchluessel = SteckbriefHerkunft.Vorgabe, Aufbau = "Boden (Ersatz)", AufbauHerkunftSchluessel = SteckbriefHerkunft.Vorgabe,
        Ersatz = "Boden Dämmung oben, Dämmdicke 8 cm", Fehlt = "keine Schichten",
        Schichten = new[] { new BauteilsteckbriefSchicht("Estrich", "50 mm", "1,4 W/(mK)", "2000 kg/m³", "1000 J/(kgK)", "Vorgabe", SteckbriefHerkunft.Vorgabe) },
    };

    private static readonly BauteilsteckbriefDaten SteckbriefFenster = new()
    {
        Kennung = "f-1", Name = "Fenster Süd", Art = "Fenster", Stufe = Aufbaustufe.Transparent, Transparent = true,
        UWert = "1,1 W/(m²K)", GWert = "0,6", Rahmenanteil = "30 %",
    };

    private static GebaeudeAnsichtDaten Daten(bool mitStufen = true)
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
        };
        if (!mitStufen) return d;
        return d with
        {
            Bauteilstufen = new Dictionary<string, Aufbaustufe>
            {
                ["aw-sued"] = Aufbaustufe.A, ["bo-1"] = Aufbaustufe.C, ["f-1"] = Aufbaustufe.Transparent,
            },
            Aufbausummen = new[]
            {
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.A, 1, 12.5, 2, 30),
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.B, 0, 0, 0, 0),
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.C, 1, 48, 0, 0),
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.Transparent, 1, 1.5, 0, 0),
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.OhneBauteil, 0, 0, 0, 0),
            },
            Steckbriefe = new Dictionary<string, BauteilsteckbriefDaten>
            {
                ["aw-sued"] = SteckbriefWand, ["bo-1"] = SteckbriefBoden, ["f-1"] = SteckbriefFenster,
            },
        };
    }

    private sealed class Protokoll
    {
        public readonly List<string> Baustoffe = new();
        public readonly List<int> Bauteile = new();
        public int Umhaengungen;
    }

    private IRenderedComponent<GebaeudeAnsicht> Zeige(GebaeudeAnsichtDaten daten, Protokoll? p = null, bool mitBauteilsprung = false)
        => Render<GebaeudeAnsicht>(c =>
        {
            c.Add(x => x.Daten, daten);
            c.Add(x => x.RaumUmgehaengt, (EPOS.UI.Dialoge.Import.GebaeudeRaumumhaengung _) => { if (p is not null) p.Umhaengungen++; });
            c.Add(x => x.ZurBaustoffzuordnung, (string s) => p?.Baustoffe.Add(s));
            if (mitBauteilsprung) c.Add(x => x.ZumBauteil, (int id) => p?.Bauteile.Add(id));
        });

    private static IElement Farbmodusknopf(IRenderedComponent<GebaeudeAnsicht> cut, string schluessel)
        => cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='" + schluessel + "']");

    private static IRenderedComponent<GebaeudeAnsicht> ImAufbaumodus(IRenderedComponent<GebaeudeAnsicht> cut)
    {
        Farbmodusknopf(cut, "aufbau").Click();
        cut.WaitForAssertion(() => Assert.Equal("aufbau", cut.Instance.Farbmodus));
        return cut;
    }

    // =====================================================================
    //  Farbmodus „Aufbau"
    // =====================================================================

    [Fact]
    public void BA3_Der_Umschalter_traegt_Aufbau_und_der_Grundriss_faerbt_Kanten_und_Oeffnungen_nach_Stufe()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        IElement knopf = Farbmodusknopf(cut, "aufbau");
        Assert.Equal("Aufbau", knopf.TextContent.Trim());
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-aufbau-hinweis"));
        ImAufbaumodus(cut);
        Assert.Equal("true", Farbmodusknopf(cut, "aufbau").GetAttribute("aria-pressed"));
        Assert.Equal("false", Farbmodusknopf(cut, "zonen").GetAttribute("aria-pressed"));

        IElement kante = cut.Find("line.epos-gebansicht-kante[data-bauteil='aw-sued']");
        Assert.Equal(("a", GebaeudeAnsichtAufbaustufen.FARBEN[0]), (kante.GetAttribute("data-stufe"), kante.GetAttribute("stroke")));
        IElement oeffnung = cut.Find("line.epos-gebansicht-oeffnung[data-bauteil='f-1']");
        Assert.Equal(("transparent", GebaeudeAnsichtAufbaustufen.FARBEN[3]), (oeffnung.GetAttribute("data-stufe"), oeffnung.GetAttribute("stroke")));
        Assert.Contains("fill:#ffffff", cut.Find("g[data-raum='r-wohnen'] polygon").GetAttribute("style"));
    }

    [Fact]
    public void BA3_Die_Legende_nennt_je_Stufe_Zahl_und_Flaeche_aussen_und_innen_und_schaltet()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = ImAufbaumodus(Zeige(Daten()));
        IReadOnlyList<IElement> eintraege = cut.FindAll(".epos-gebansicht-aufbaueintrag").ToList();
        Assert.Equal(new[] { "a", "c", "transparent" }, eintraege.Select(e => e.GetAttribute("data-stufe")));
        Assert.Equal(new[] { "Zuordnungsstufen des Aufbaus", "Außen", "Innen" },
                     cut.FindAll(".epos-gebansicht-aufbaulegende th").Select(t => t.TextContent.Trim()));
        IElement a = eintraege[0];
        Assert.Equal("A – vollständiger Aufbau", a.QuerySelector(".epos-gebansicht-zonenname")!.TextContent.Trim());
        Assert.Equal("1 · 12,5 m²", a.QuerySelector(".epos-gebansicht-aufbau-aussen")!.TextContent.Trim());
        Assert.Equal("2 · 30,0 m²", a.QuerySelector(".epos-gebansicht-aufbau-innen")!.TextContent.Trim());

        cut.Find(".epos-gebansicht-stufenschalter[data-stufe='a']").Click();
        Assert.False(cut.Instance.StufeSichtbar(Aufbaustufe.A));
        Assert.Equal("false", cut.Find(".epos-gebansicht-stufenschalter[data-stufe='a']").GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll("line.epos-gebansicht-kante[data-bauteil='aw-sued']"));
    }

    [Fact]
    public void BA3_Ohne_Stufen_ist_Aufbau_benannt_gesperrt()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(mitStufen: false));
        IElement knopf = Farbmodusknopf(cut, "aufbau");
        Assert.True(knopf.HasAttribute("disabled"));
        string grund = new GebaeudeAnsichtTexte().AufbauOhneStufen;
        Assert.Equal(grund, knopf.GetAttribute("title"));
        Assert.Equal(grund, cut.Find(".epos-gebansicht-aufbau-hinweis").TextContent.Trim());
        Assert.Equal("zonen", cut.Instance.Farbmodus);
        // „Randbedingung" bleibt wählbar.
        Assert.False(Farbmodusknopf(cut, "randbedingung").HasAttribute("disabled"));
    }

    [Fact]
    public void BA3_Koerperfeld_und_Szene_tragen_die_Stufenbytes()
    {
        GebaeudeAnsichtDaten ohne = Daten(mitStufen: false);
        GebaeudeAnsichtKoerperfeld f0 = ohne.Koerperfeld();
        Assert.Equal(-1, f0.Verzeichnis[0].StufenAb);
        Assert.Equal(-1, f0.Bauteile[0].Stufe);

        GebaeudeAnsichtDaten d = Daten();
        GebaeudeAnsichtKoerperfeld f = d.Koerperfeld();
        Assert.Equal(f0.Bytes.Length + 4, f.Bytes.Length);   // drei Stufenbytes, auf vier Byte aufgefüllt
        int ab = f.Verzeichnis[0].StufenAb;
        Assert.True(ab > 0);
        // R1 mit A-Bauteil → 0, R3 mit C-Bauteil → 2, R0 → neutrale Innenfläche 5.
        Assert.Equal(new byte[] { 0, 2, GebaeudeAnsichtAufbaustufen.INNEN_NEUTRAL }, f.Bytes.Skip(ab).Take(3));
        Assert.Equal((int)Aufbaustufe.Transparent, f.Bauteile[0].Stufe);

        string json = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.SzeneDatei(d, ZONE, f, "aufbau", null, true,
                                                                                             new[] { true, false, true, true, true }));
        Assert.Contains("\"stufenAb\":" + ab, json);
        Assert.Contains("\"aufbaufarben\":[\"" + string.Join("\",\"", GebaeudeAnsichtAufbaustufen.FARBEN) + "\"]", json);
        Assert.Contains("\"aufbausichtbar\":[true,false,true,true,true]", json);
        Assert.Contains("\"stufe\":3}", json);
        Assert.DoesNotContain("Außenwand", json);
    }

    // =====================================================================
    //  Bauteilsteckbrief
    // =====================================================================

    [Fact]
    public async Task BA3_Ein_Klick_auf_den_Bauteilkoerper_oeffnet_den_Steckbrief_des_Fensters()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = ImAufbaumodus(Zeige(Daten()));
        Assert.Null(cut.Instance.GezeigterSteckbrief);
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(null, null, 3, "f-1", 0));
        cut.WaitForAssertion(() => Assert.Equal("f-1", cut.Instance.GezeigterSteckbrief?.Kennung));
        IElement sb = cut.Find(".epos-steckbrief[data-kennung='f-1']");
        Assert.Equal("transparent", sb.GetAttribute("data-stufe"));
        Assert.Equal("0,6", sb.QuerySelector(".epos-steckbrief-g")!.TextContent.Trim());
        Assert.Equal("30 %", sb.QuerySelector(".epos-steckbrief-rahmen")!.TextContent.Trim());
        Assert.Null(sb.QuerySelector(".epos-steckbrief-schichten"));
        Assert.Null(sb.QuerySelector(".epos-steckbrief-r1"));
    }

    [Fact]
    public async Task BA3_Ein_Klick_auf_eine_Raumflaeche_findet_das_Bauteil_ueber_das_Dreieck()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeAnsicht> cut = ImAufbaumodus(Zeige(Daten(), p));
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "r-wohnen", 0, null, 0));
        cut.WaitForAssertion(() => Assert.Equal("aw-sued", cut.Instance.GezeigterSteckbrief?.Kennung));
        Assert.Equal(("r-wohnen", 0, "aw-sued"), cut.Instance.Treffer!.Value);

        IElement sb = cut.Find(".epos-steckbrief");
        Assert.Equal("a", sb.GetAttribute("data-stufe"));
        Assert.Equal("Außenwand Süd", sb.QuerySelector(".epos-steckbrief-name")!.TextContent.Trim());
        Assert.Equal("datei", sb.QuerySelector(".epos-steckbrief-u")!.GetAttribute("data-herkunft"));
        Assert.Equal("U der Datei weicht ab.", sb.QuerySelector(".epos-steckbrief-uhinweis")!.TextContent.Trim());
        Assert.Equal("0,0012 K/W", sb.QuerySelector(".epos-steckbrief-r1")!.TextContent.Trim());
        Assert.Equal("projektdatei", sb.QuerySelector(".epos-steckbrief-aufbau")!.GetAttribute("data-herkunft"));
        IReadOnlyList<IElement> schichten = sb.QuerySelectorAll(".epos-steckbrief-schicht").ToList();
        Assert.Equal(3, schichten.Count);
        Assert.Equal(new[] { "projektdatei", "katalog", "datei" }, schichten.Select(s => s.GetAttribute("data-herkunft")));
        Assert.Equal("true", schichten[2].GetAttribute("data-weggelassen"));
        Assert.Contains("weggelassen: dünn", schichten[2].TextContent);

        // Sprung zur Baustoffzuordnung; ohne Delegat kein Bauteilsprung; nichts umgehängt (keine Schreibaufrufe).
        Assert.Empty(sb.QuerySelectorAll(".epos-steckbrief-bauteil"));
        cut.Find(".epos-steckbrief-baustoff").Click();
        Assert.Equal(new[] { "gipsputz" }, p.Baustoffe);
        Assert.Equal(0, p.Umhaengungen);

        // Das zweite Dreieck gehört der Bodenplatte (Ersatzaufbau), das dritte keinem Bauteil: der Steckbrief bleibt.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "r-wohnen", 2, null, 1));
        cut.WaitForAssertion(() => Assert.Equal("bo-1", cut.Instance.GezeigterSteckbrief?.Kennung));
        Assert.Equal("Ersatzaufbau: Boden Dämmung oben, Dämmdicke 8 cm", cut.Find(".epos-steckbrief-ersatz").TextContent.Trim());
        Assert.Equal("Es fehlt: keine Schichten", cut.Find(".epos-steckbrief-fehlt").TextContent.Trim());
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "r-wohnen", 5, null, 2));
        Assert.Equal("bo-1", cut.Instance.GezeigterSteckbrief?.Kennung);

        cut.Find(".epos-steckbrief-schliessen").Click();
        Assert.Null(cut.Instance.GezeigterSteckbrief);
        Assert.Empty(cut.FindAll(".epos-steckbrief"));
    }

    [Fact]
    public void BA3_Ein_Klick_auf_eine_Grundrisskante_oeffnet_den_Steckbrief_ohne_umzuhaengen()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeAnsicht> cut = ImAufbaumodus(Zeige(Daten(), p, mitBauteilsprung: true));
        cut.Find("line.epos-gebansicht-kante[data-bauteil='aw-sued']").Click();
        Assert.Equal("aw-sued", cut.Instance.GezeigterSteckbrief?.Kennung);
        Assert.Equal(0, p.Umhaengungen);
        cut.Find(".epos-steckbrief-bauteil").Click();
        Assert.Equal(new[] { 41 }, p.Bauteile);
    }

    [Fact]
    public async Task BA3_Im_Modus_Zonen_oeffnet_kein_Steckbrief()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE, "r-wohnen", null, null, null));
        Assert.Null(cut.Instance.GezeigterSteckbrief);
        Assert.Empty(cut.FindAll(".epos-steckbrief"));
    }

    [Fact]
    public void BA3_Der_Steckbrief_zeichnet_allein_ohne_Delegaten_ohne_Knoepfe()
    {
        IRenderedComponent<Bauteilsteckbrief> cut = Render<Bauteilsteckbrief>(c => c.Add(x => x.Daten, SteckbriefWand));
        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("Bauteilsteckbrief", cut.Find("aside").GetAttribute("aria-label"));
        Assert.Equal("a", cut.Find(".epos-steckbrief-stufe").TextContent.Trim());
        Assert.Contains("Der Steckbrief zeigt nur an", cut.Find(".epos-steckbrief-nuranzeige").TextContent);
    }

    // =====================================================================
    //  Zonendialog: Spalte „Aufbau" und Filter
    // =====================================================================

    private IRenderedComponent<ZonenDialog> Zonendialog()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        var zone = new ZoneDaten
        {
            Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150,
            Bauteile =
            {
                new BauteilDaten { Id = 1, Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 100, IdAufbau = 5,
                                   AufbauText = "AW massiv", Herkunft = "IFC" },
                new BauteilDaten { Id = 2, Bezeichner = "Wand Ersatz", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = 40, IdAufbau = 6,
                                   UWert = 0.3, Herkunft = "IFC", AufbauText = "Ersatz" },
                new BauteilDaten { Id = 3, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH, Flaeche = 80, UWert = 0.2,
                                   Herkunft = DbWerte.HERKUNFT_VORGABE },
                new BauteilDaten { Id = 4, Bezeichner = "Fenster", Bauteilart = DbWerte.BAUTEILART_FENSTER, Flaeche = 20, UWert = 1.1 },
            },
        };
        return Render<ZonenDialog>(p => p
            .Add(x => x.Zone, zone)
            .Add(x => x.NutzflaecheGebaeude, 150)
            .Add(x => x.Projektaufbauten, new[]
            {
                new AufbauWahl(5, false, "AW massiv", DbWerte.BAUTEILART_AUSSENWAND, 0.28),
                new AufbauWahl(6, false, "Ersatz", DbWerte.BAUTEILART_AUSSENWAND, 0.3, Ersatz: true),
            }));
    }

    [Fact]
    public void BA3_Die_Spalte_Aufbau_traegt_die_Stufe_und_der_Filter_zeigt_nur_B_und_C()
    {
        IRenderedComponent<ZonenDialog> cut = Zonendialog();
        Assert.Equal(new[] { "a", "b", "c", "transparent" }, cut.FindAll(".epos-zonenbauteil").Select(z => z.GetAttribute("data-stufe")));
        IElement stufe = cut.Find(".epos-zonenbauteil[data-stufe='a'] .epos-zonen-aufbaustufe");
        Assert.Equal("Zuordnungsstufe A: A – vollständiger Aufbau", stufe.GetAttribute("title"));
        Assert.Contains("AW massiv", stufe.TextContent);

        IElement filter = cut.Find(".epos-zonen-aufbaufilter input");
        Assert.Equal("Ohne vollständige Zuordnung", cut.Find(".epos-zonen-aufbaufilter").TextContent.Trim());
        filter.Change(true);
        Assert.Equal(new[] { "b", "c" }, cut.FindAll(".epos-zonenbauteil").Select(z => z.GetAttribute("data-stufe")));

        // Ein Klick auf die Stufe öffnet den vorhandenen Bauteildialog.
        cut.Find(".epos-zonenbauteil[data-stufe='c'] .epos-zonen-aufbaustufe").Click();
        Assert.True(cut.Instance.BauteilOffen);
    }
}
