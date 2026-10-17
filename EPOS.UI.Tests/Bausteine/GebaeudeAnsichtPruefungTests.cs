using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using Microsoft.JSInterop;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// MA-5 „Ansicht als Werkzeug" am Baustein <see cref="GebaeudeAnsicht"/>: Hervorhebung der Befunde einer Prüfgruppe im Grundriss
/// und in der Szene, die Prüfzeile, „Übernehmen" und „Verwerfen" je Legendenzeile und der Farbmodus „Nutzung".
///
/// <para><b>Was hier NICHT bewiesen wird:</b> das Bild selbst — bunit hat kein WebGL; die Szene trägt <c>hervorhebung</c> und je
/// Körper <c>hervor</c>, das Modul dimmt danach (Browserprobe im Protokoll).</para>
/// </summary>
public class GebaeudeAnsichtPruefungTests : EposBunitContext
{
    private const string Z1 = "A1|EG|1";
    private const string Z2 = "A1|EG|2";
    private const string MODUL = GebaeudeAnsicht.MODUL_KOERPER;

    private static IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygon(params (double X, double Y)[] punkte)
        => new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)punkte.Select(p => new GebaeudeAnsichtPunkt(p.X, p.Y)).ToList() };

    /// <summary>
    /// Zwei Räume: A (Zone 1) mit Dateikörper aus Außenwand (Stufe A, Bauteil 41) und Bodenplatte (Stufe C, Bauteil 42), B (Zone 2)
    /// als Prisma; ein Fensterkörper (Bauteil 43). Zone 1 trägt das Profil „Büro", Zone 2 keines.
    /// </summary>
    private static GebaeudeAnsichtDaten Daten(bool mitNutzung = true)
    {
        var datei = new GebaeudeAnsichtDateikoerper(
            new float[] { 0, 0, 0, 6, 0, 0, 6, 8, 0, 0, 8, 0 }, new[] { 0, 1, 2, 0, 2, 3 }, new[] { 0, 1, 1, 2, 2, 3, 0, 3 },
            2, "FacetedBrep", Array.Empty<string>()) { Gruppen = new byte[] { 1, 3 } };
        var fenster = new GebaeudeAnsichtDateikoerper(
            new float[] { 1.5f, 0, 1, 3, 0, 1, 3, 0, 2 }, new[] { 0, 1, 2 }, new[] { 0, 1, 1, 2 }, 1, "FacetedBrep", Array.Empty<string>());
        var d = new GebaeudeAnsichtDaten
        {
            Geschosse = new[]
            {
                new GebaeudeAnsichtGeschoss("g-eg", "Erdgeschoss", false, 0, 0, 10, 8, new[]
                {
                    new GebaeudeAnsichtRaum("r-a", "Raum A", Z1, true, false, "48 m²", Polygon((0, 0), (6, 0), (6, 8), (0, 8))),
                    new GebaeudeAnsichtRaum("r-b", "Raum B", Z2, true, false, "32 m²", Polygon((6, 0), (10, 0), (10, 8), (6, 8))),
                }),
            },
            Zonen = new[]
            {
                new GebaeudeAnsichtZone(Z1, "Zone 1", 0, true, false, false),
                new GebaeudeAnsichtZone(Z2, "Zone 2", 1, true, false, false),
            },
            Koerperraeume = new[]
            {
                new GebaeudeAnsichtKoerperraum("r-a", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null)
                {
                    Dateikoerper = datei, Herkunft = Koerperherkunft.Datei,
                    Kantengruppen = new[] { (IReadOnlyList<Randgruppe?>)new Randgruppe?[] { Randgruppe.R1, null, null, null } },
                    Dreiecksbauteile = new string?[] { "aw-1", "bp-1" },
                    Kantenbauteile = new[] { (IReadOnlyList<string?>)new string?[] { "aw-1", null, null, null } },
                },
                new GebaeudeAnsichtKoerperraum("r-b", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null),
            },
            Flaechengruppen = Enumerable.Range(0, GebaeudeAnsichtRandgruppen.ZAHL)
                .Select(i => new GebaeudeAnsichtFlaechengruppe((Randgruppe)i, i is 1 or 3 ? 48 : 0, i is 1 or 3 ? 1 : 0)).ToList(),
            Bauteilkoerper = new[] { new GebaeudeAnsichtBauteilkoerper("f-1", Randgruppe.R7, fenster) },
            Bauteilstufen = new Dictionary<string, Aufbaustufe>
            {
                ["aw-1"] = Aufbaustufe.A, ["bp-1"] = Aufbaustufe.C, ["f-1"] = Aufbaustufe.Transparent,
            },
            Aufbausummen = new[]
            {
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.A, 1, 12.5, 0, 0),
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.C, 1, 48, 0, 0),
                new GebaeudeAnsichtAufbausumme(Aufbaustufe.Transparent, 1, 1.5, 0, 0),
            },
            BauteilIds = new Dictionary<string, int>(StringComparer.Ordinal) { ["aw-1"] = 41, ["bp-1"] = 42, ["f-1"] = 43 },
            ZonenIds = new Dictionary<string, int>(StringComparer.Ordinal) { [Z1] = 501, [Z2] = 502 },
        };
        return mitNutzung
            ? d with
            {
                Nutzungen = new Dictionary<string, GebaeudeAnsichtNutzung>(StringComparer.Ordinal)
                {
                    [Z1] = new GebaeudeAnsichtNutzung("Büro", "Nichtwohnen"), [Z2] = new GebaeudeAnsichtNutzung("", ""),
                }
            }
            : d;
    }

    /// <summary>Gruppe 2: die Bodenplatte (42) mit Vorschlag, die Außenwand (41) ohne.</summary>
    private static GebaeudeAnsichtPruefung Aufbaupruefung() => new(Pruefgruppe.AufbautenStoffwerte, "Aufbauten und Stoffwerte", new[]
    {
        new ModellpruefungBefundzeile("Nur U-Wert", Pruefampel.Gelb, "Zone 1 — Boden", "Aufbau Bodenplatte", 501, 42),
        new ModellpruefungBefundzeile("Kapazität geschätzt", Pruefampel.Gelb, "Zone 1 — Wand", "", 501, 41),
    });

    /// <summary>Gruppe 3: Zone 2 ohne Profil, mit Vorschlag.</summary>
    private static GebaeudeAnsichtPruefung Nutzungspruefung() => new(Pruefgruppe.NutzungKonditionierung, "Nutzung", new[]
    {
        new ModellpruefungBefundzeile("Zone ohne Profil", Pruefampel.Gelb, "Zone 2", "Profil Wohnen", 502, null),
    });

    private IRenderedComponent<GebaeudeAnsicht> Zeige(GebaeudeAnsichtDaten daten, GebaeudeAnsichtPruefung? pruefung, string? farbmodus,
                                                      List<ModellpruefungAuswahl>? uebernommen = null,
                                                      List<ModellpruefungAuswahl>? verworfen = null)
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        BunitJSModuleInterop modul = JSInterop.SetupModule(MODUL);
        modul.Setup<bool>("erzeugen", _ => true).SetResult(true);
        modul.SetupVoid("aktualisieren", _ => true).SetVoidResult();
        modul.SetupVoid("entsorgen").SetVoidResult();
        _modul = modul;
        return Render<GebaeudeAnsicht>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Pruefung, pruefung);
            p.Add(x => x.FarbmodusVorgabe, farbmodus);
            if (uebernommen is not null) p.Add(x => x.PruefungUebernehmen, (ModellpruefungAuswahl a) => uebernommen.Add(a));
            if (verworfen is not null) p.Add(x => x.PruefungVerwerfen, (ModellpruefungAuswahl a) => verworfen.Add(a));
        });
    }

    private BunitJSModuleInterop? _modul;

    private BunitJSModuleInterop Koerper(IRenderedComponent<GebaeudeAnsicht> cut)
    {
        cut.FindAll("button[role=tab]").First(k => k.TextContent.Trim().StartsWith("Körper", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.KoerperGezeigt));
        return _modul!;
    }

    private static string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);

    [Fact]
    public void MA5_Die_Befunde_der_Gruppe_heben_Raum_und_Kante_hervor_und_die_Pruefzeile_nennt_sie()
    {
        var cut = Zeige(Daten(), Aufbaupruefung(), "aufbau");
        Assert.Equal("aufbau", cut.Instance.Farbmodus);
        IElement zeile = cut.Find(".epos-gebansicht-pruefzeile");
        Assert.Equal(("2", "2", "0"), (zeile.GetAttribute("data-gruppe"), zeile.GetAttribute("data-bauteile"), zeile.GetAttribute("data-zonen")));
        Assert.Equal(string.Format(R.GANS_PRUEF_HERVOR, "Aufbauten und Stoffwerte", 2, 0), zeile.QuerySelector(".epos-gebansicht-prueftext")!.TextContent);

        // Raum A trägt beide Bauteile: hervorgehoben; Raum B tritt zurück.
        Assert.Contains("epos-gebansicht-raum--geprueft", cut.Find("g[data-raum='r-a']").ClassName);
        Assert.Contains("epos-gebansicht-raum--gedimmt", cut.Find("g[data-raum='r-b']").ClassName);
        // Die Kante der Außenwand trägt den Rand der Hervorhebung.
        Assert.Contains("epos-gebansicht-kante--geprueft", cut.Find("line.epos-gebansicht-kante[data-bauteil='aw-1']").ClassName);
    }

    [Fact]
    public void MA5_Die_Szene_traegt_die_Hervorhebung_je_Koerper()
    {
        var cut = Zeige(Daten(), Aufbaupruefung(), "aufbau");
        BunitJSModuleInterop modul = Koerper(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(modul.Invocations["erzeugen"]));
        string szene = Json(modul.Invocations["erzeugen"].Last().Arguments[1]!);
        Assert.Contains("\"hervorhebung\":true", szene);
        Assert.Matches("\"raum\":\"r-a\",\"zone\":\"" + Z1 + "\",\"stelle\":0,[^}]*\"hervor\":true", szene);
        Assert.Matches("\"raum\":\"r-b\",\"zone\":\"" + Z2 + "\",\"stelle\":1,[^}]*\"hervor\":false", szene);
        Assert.Contains("\"bauteil\":\"f-1\"", szene);
        Assert.Contains("\"hervorBauteile\":[\"aw-1\",\"bp-1\"]", szene);   // der Fensterkörper ist nicht getroffen
    }

    [Fact]
    public void MA5_Gegenprobe_ohne_Pruefung_keine_Hervorhebung_in_der_Szene()
    {
        var ohne = Zeige(Daten(), null, "aufbau");
        BunitJSModuleInterop m2 = Koerper(ohne);
        ohne.WaitForAssertion(() => Assert.NotEmpty(m2.Invocations["erzeugen"]));
        string s2 = Json(m2.Invocations["erzeugen"].Last().Arguments[1]!);
        Assert.Contains("\"hervorhebung\":false", s2);
        Assert.DoesNotContain("\"hervor\":true", s2);
        Assert.Empty(ohne.FindAll(".epos-gebansicht-pruefzeile"));
    }

    [Fact]
    public void MA5_Die_Aufbaulegende_nennt_je_Stufe_die_Vorschlaege_und_reicht_Uebernehmen_und_Verwerfen_weiter()
    {
        var uebernommen = new List<ModellpruefungAuswahl>();
        var verworfen = new List<ModellpruefungAuswahl>();
        var cut = Zeige(Daten(), Aufbaupruefung(), "aufbau", uebernommen, verworfen);
        Assert.Equal(R.GANS_PRUEF_VORSCHLAEGE, cut.Find(".epos-gebansicht-aufbaulegende .epos-gebansicht-vorschlagskopf").TextContent);
        IElement stufeC = cut.Find(".epos-gebansicht-aufbaueintrag[data-stufe='" + GebaeudeAnsichtAufbaustufen.Schluessel(Aufbaustufe.C) + "']");
        IElement stufeA = cut.Find(".epos-gebansicht-aufbaueintrag[data-stufe='" + GebaeudeAnsichtAufbaustufen.Schluessel(Aufbaustufe.A) + "']");
        Assert.Equal("1", stufeC.QuerySelector(".epos-gebansicht-vorschlag")!.GetAttribute("data-vorschlaege"));
        Assert.Equal("0", stufeA.QuerySelector(".epos-gebansicht-vorschlag")!.GetAttribute("data-vorschlaege"));
        Assert.Null(stufeA.QuerySelector(".epos-gebansicht-uebernehmen"));
        Assert.Equal(string.Format(R.GANS_PRUEF_ZAHL, 1), stufeC.QuerySelector(".epos-gebansicht-vorschlagszahl")!.TextContent);

        stufeC.QuerySelector(".epos-gebansicht-uebernehmen")!.Click();
        ModellpruefungAuswahl a = Assert.Single(uebernommen);
        Assert.Equal(Pruefgruppe.AufbautenStoffwerte, a.Gruppe);
        Assert.Equal(new[] { 42 }, a.Bauteile);
        Assert.Empty(a.Zonen);

        cut.Find(".epos-gebansicht-aufbaueintrag[data-stufe='" + GebaeudeAnsichtAufbaustufen.Schluessel(Aufbaustufe.C) + "'] .epos-gebansicht-verwerfen").Click();
        Assert.Equal(new[] { 42 }, Assert.Single(verworfen).Bauteile);

        // Die Prüfzeile bietet dasselbe für alle Vorschläge der Gruppe in der Ansicht.
        cut.Find(".epos-gebansicht-pruefzeile .epos-gebansicht-uebernehmen").Click();
        Assert.Equal(new[] { 42 }, uebernommen.Last().Bauteile);
    }

    [Fact]
    public void MA5_Ohne_Delegat_des_Wirts_keine_Knoepfe_ohne_Vorschlag_keine_Spalte()
    {
        var cut = Zeige(Daten(), Aufbaupruefung(), "aufbau");
        Assert.NotEmpty(cut.FindAll(".epos-gebansicht-vorschlagszahl"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-uebernehmen, .epos-gebansicht-verwerfen"));

        var ohneVorschlag = new GebaeudeAnsichtPruefung(Pruefgruppe.AufbautenStoffwerte, "Aufbauten", new[]
        {
            new ModellpruefungBefundzeile("Kapazität geschätzt", Pruefampel.Gelb, "", "", 501, 41),
        });
        var cut2 = Zeige(Daten(), ohneVorschlag, "aufbau", new List<ModellpruefungAuswahl>(), new List<ModellpruefungAuswahl>());
        Assert.Empty(cut2.FindAll(".epos-gebansicht-vorschlagskopf"));
        Assert.Single(cut2.FindAll(".epos-gebansicht-pruefzeile"));
    }

    [Fact]
    public void MA5_Ohne_Treffer_nennt_die_Pruefzeile_den_Grund_und_dimmt_nichts()
    {
        var fremd = new GebaeudeAnsichtPruefung(Pruefgruppe.GeometrieBauteile, "Geometrie", new[]
        {
            new ModellpruefungBefundzeile("Fläche fehlt", Pruefampel.Rot, "", "", 999, 999),
        });
        var cut = Zeige(Daten(), fremd, "randbedingung");
        Assert.Equal(string.Format(R.GANS_PRUEF_OHNE, "Geometrie"), cut.Find(".epos-gebansicht-prueftext").TextContent);
        Assert.Empty(cut.FindAll(".epos-gebansicht-raum--gedimmt"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-raum--geprueft"));
    }

    [Fact]
    public void MA5_Farbmodus_Nutzung_faerbt_nach_Profil_und_fuehrt_die_Legende_mit_Zahl_und_fehlt()
    {
        var cut = Zeige(Daten(), null, null);
        IElement knopf = cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='nutzung']");
        Assert.Equal(R.GANS_FARBMODUS_NUTZUNG, knopf.TextContent.Trim());
        knopf.Click();
        Assert.Equal("nutzung", cut.Instance.Farbmodus);
        Assert.False(cut.Instance.OrdnetZu);
        Assert.Contains("epos-gebansicht-zone--0", cut.Find("g[data-raum='r-a']").ClassName);
        Assert.Contains("epos-gebansicht-zone--ohne", cut.Find("g[data-raum='r-b']").ClassName);
        var zeilen = cut.FindAll(".epos-gebansicht-nutzungslegende tbody tr");
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Büro", zeilen[0].QuerySelector(".epos-gebansicht-zonenname")!.TextContent);
        Assert.Equal("Nichtwohnen", zeilen[0].QuerySelector(".epos-gebansicht-marke")!.TextContent);
        Assert.Equal(string.Format(R.GANS_NUTZUNG_ZONEN, 1), zeilen[0].QuerySelector(".epos-gebansicht-nutzung-zahl")!.TextContent);
        Assert.Equal(R.GANS_NUTZUNG_FEHLT, zeilen[1].QuerySelector(".epos-gebansicht-zonenname")!.TextContent);
        Assert.Contains("epos-gebansicht-zone--ohne", zeilen[1].QuerySelector(".epos-gebansicht-farbfeld")!.ClassName);

        // In der Szene: die Farbstelle nach dem Profil, −1 = fehlt.
        BunitJSModuleInterop modul = Koerper(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(modul.Invocations["erzeugen"]));
        string szene = Json(modul.Invocations["erzeugen"].Last().Arguments[1]!);
        Assert.Contains("\"farbmodus\":\"nutzung\"", szene);
        Assert.Contains("\"raum\":\"r-a\",\"zone\":\"" + Z1 + "\",\"stelle\":0", szene);
        Assert.Contains("\"raum\":\"r-b\",\"zone\":\"" + Z2 + "\",\"stelle\":-1", szene);
    }

    [Fact]
    public void MA5_Nutzung_ist_ohne_gelesene_Zonen_nicht_im_Umschalter_und_faellt_auf_Zonen_zurueck()
    {
        var cut = Zeige(Daten(mitNutzung: false), Nutzungspruefung(), "nutzung");
        Assert.Empty(cut.FindAll(".epos-gebansicht-farbmodus button[data-farbmodus='nutzung']"));
        Assert.Equal("zonen", cut.Instance.Farbmodus);
        // Die Hervorhebung der Zone gilt auch im Farbmodus „Zonen".
        Assert.Contains("epos-gebansicht-raum--geprueft", cut.Find("g[data-raum='r-b']").ClassName);
    }

    [Fact]
    public void MA5_Nutzungslegende_bietet_die_Vorschlaege_der_Zonen_ohne_Profil()
    {
        var uebernommen = new List<ModellpruefungAuswahl>();
        var verworfen = new List<ModellpruefungAuswahl>();
        var cut = Zeige(Daten(), Nutzungspruefung(), "nutzung", uebernommen, verworfen);
        Assert.Equal("nutzung", cut.Instance.Farbmodus);
        Assert.Contains("epos-gebansicht-raum--geprueft", cut.Find("g[data-raum='r-b']").ClassName);
        Assert.Contains("epos-gebansicht-raum--gedimmt", cut.Find("g[data-raum='r-a']").ClassName);
        IElement fehlt = cut.Find(".epos-gebansicht-nutzungseintrag--fehlt");
        Assert.Equal("1", fehlt.QuerySelector(".epos-gebansicht-vorschlag")!.GetAttribute("data-vorschlaege"));
        Assert.Equal("0", cut.Find(".epos-gebansicht-nutzungseintrag[data-stelle='0'] .epos-gebansicht-vorschlag").GetAttribute("data-vorschlaege"));
        fehlt.QuerySelector(".epos-gebansicht-uebernehmen")!.Click();
        ModellpruefungAuswahl a = Assert.Single(uebernommen);
        Assert.Equal(Pruefgruppe.NutzungKonditionierung, a.Gruppe);
        Assert.Empty(a.Bauteile);
        Assert.Equal(new[] { 502 }, a.Zonen);
        cut.Find(".epos-gebansicht-nutzungseintrag--fehlt .epos-gebansicht-verwerfen").Click();
        Assert.Equal(new[] { 502 }, Assert.Single(verworfen).Zonen);
    }

    [Fact]
    public void MA5_Die_Auswahl_trennt_Bauteil_und_Zone()
    {
        var a = new ModellpruefungAuswahl(Pruefgruppe.AufbautenStoffwerte, new[] { 42 }, new[] { 502 });
        Assert.True(a.Enthaelt(501, 42));
        Assert.False(a.Enthaelt(502, 41));    // ein Befund mit Bauteil zählt über das Bauteil, nicht über die Zone
        Assert.True(a.Enthaelt(502, null));
        Assert.False(a.Enthaelt(null, null));
    }
}
