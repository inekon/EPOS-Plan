using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using Microsoft.JSInterop;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Ein- und Ausblenden in der Körperansicht des Bausteins <see cref="GebaeudeAnsicht"/> (Auftrag AB4).
///
/// <para><b>Was hier bewiesen wird:</b> die Werkzeugwahl „Zuordnen | Ein-/Ausblenden" an der Legende (Vorgabe Zuordnen), der
/// Klick im Werkzeug Ein-/Ausblenden schaltet die Sichtbarkeit der Zone und ordnet nichts zu, „Alle zeigen" setzt zurück, die
/// Kästchenliste der Bauteilgruppen zeigt nur vorkommende Gruppen mit Zahl, die Sichtbarkeit eines Körpers ist Legende UND
/// Gruppe, ein Wechsel des Farbmodus setzt die Legende zurück und lässt die Gruppen, und die Szene trägt nur Sichtbares
/// (ausgeblendete Körper nehmen am Klick nicht teil, weil das Modul sie nicht baut).</para>
///
/// <para><b>Was hier NICHT bewiesen wird:</b> das Bild selbst — bunit hat kein WebGL; das Modul hält die Dreiecke über die
/// Bauteilgruppenbytes, die hier als Bytefeld geprüft werden.</para>
/// </summary>
public class GebaeudeAnsichtAusblendenTests : EposBunitContext
{
    private const string Z1 = "A1|EG|1";
    private const string Z2 = "A1|EG|2";
    private const string MODUL = GebaeudeAnsicht.MODUL_KOERPER;

    private static IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygon(params (double X, double Y)[] punkte)
        => new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)punkte.Select(p => new GebaeudeAnsichtPunkt(p.X, p.Y)).ToList() };

    /// <summary>
    /// Ein Geschoss mit drei Räumen: A (Zone 1) mit Dateikörper aus zwei Dreiecken — Außenwand und Bodenplatte —, B (Zone 2) als
    /// Prisma mit Außen- und Innenwand an den Kanten, Bodenplatte und Decke, C ohne Zone; dazu ein Fensterkörper (R7).
    /// </summary>
    private static GebaeudeAnsichtDaten Daten(bool mitArten = true)
    {
        var datei = new GebaeudeAnsichtDateikoerper(
            new float[] { 0, 0, 0, 6, 0, 0, 6, 8, 0, 0, 8, 0 }, new[] { 0, 1, 2, 0, 2, 3 }, new[] { 0, 1, 1, 2, 2, 3, 0, 3 },
            2, "FacetedBrep", Array.Empty<string>()) { Gruppen = new byte[] { 1, 3 } };
        var fenster = new GebaeudeAnsichtDateikoerper(
            new float[] { 1.5f, 0, 1, 3, 0, 1, 3, 0, 2 }, new[] { 0, 1, 2 }, new[] { 0, 1, 1, 2 }, 1, "FacetedBrep", Array.Empty<string>());
        return new GebaeudeAnsichtDaten
        {
            Geschosse = new[]
            {
                new GebaeudeAnsichtGeschoss("g-eg", "Erdgeschoss", false, 0, 0, 14, 9, new[]
                {
                    new GebaeudeAnsichtRaum("r-a", "Raum A", Z1, true, false, "48 m²", Polygon((0, 0), (6, 0), (6, 8), (0, 8))),
                    new GebaeudeAnsichtRaum("r-b", "Raum B", Z2, true, false, "32 m²", Polygon((6, 0), (10, 0), (10, 8), (6, 8))),
                    new GebaeudeAnsichtRaum("r-c", "Raum C", null, true, false, "10 m²", Polygon((10, 0), (14, 0), (14, 8), (10, 8))),
                }),
            },
            Zonen = new[]
            {
                new GebaeudeAnsichtZone(Z1, "Zone 1", 0, true, false, false),
                new GebaeudeAnsichtZone(Z2, "Zone 2", 1, true, false, false),
            },
            Umhaengbar = true,
            Koerperraeume = new[]
            {
                new GebaeudeAnsichtKoerperraum("r-a", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null)
                {
                    Dateikoerper = datei, Herkunft = Koerperherkunft.Datei, Dreiecksbauteile = new[] { "aw-1", "bp-1" },
                },
                new GebaeudeAnsichtKoerperraum("r-b", 2.75,
                    new[] { (IReadOnlyList<string?>)new string?[] { "Aussenwand", "Innenwand", null, null } }, "Bodenplatte", "Decke"),
                new GebaeudeAnsichtKoerperraum("r-c", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null),
            },
            Flaechengruppen = new[]
            {
                new GebaeudeAnsichtFlaechengruppe(Randgruppe.R1, 48, 1), new GebaeudeAnsichtFlaechengruppe(Randgruppe.R3, 48, 1),
                new GebaeudeAnsichtFlaechengruppe(Randgruppe.R7, 1.5, 0),
            },
            Bauteilkoerper = new[] { new GebaeudeAnsichtBauteilkoerper("f-1", Randgruppe.R7, fenster) },
            Bauteilarten = mitArten
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["aw-1"] = "Aussenwand", ["bp-1"] = "Bodenplatte", ["f-1"] = "Fenster", ["iw-1"] = "Innenwand",
                }
                : new Dictionary<string, string>(StringComparer.Ordinal),
        };
    }

    private static readonly IReadOnlyList<GebaeudeAnsichtZiel> Ziele = new[]
    {
        new GebaeudeAnsichtZiel("P-1", "Zone 1", Z1),
        new GebaeudeAnsichtZiel("P-2", "Zone 2", Z2),
    };

    /// <summary>Die Körperansicht mit Zielwahl (AB2); Modul nachgestellt, Meldungen gesammelt.</summary>
    private (IRenderedComponent<GebaeudeAnsicht> Cut, BunitJSModuleInterop Modul) ZeigeKoerper(
        List<GebaeudeRaumumhaengung> gemeldet, List<string?> wahl, GebaeudeAnsichtDaten? daten = null, string? gewaehlt = null)
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        BunitJSModuleInterop modul = JSInterop.SetupModule(MODUL);
        modul.Setup<bool>("erzeugen", _ => true).SetResult(true);
        modul.SetupVoid("aktualisieren", _ => true).SetVoidResult();
        modul.SetupVoid("entsorgen").SetVoidResult();
        IRenderedComponent<GebaeudeAnsicht> cut = Render<GebaeudeAnsicht>(p =>
        {
            p.Add(x => x.Daten, daten ?? Daten());
            p.Add(x => x.Zuordnungsziele, Ziele);
            p.Add(x => x.GewaehltesZiel, gewaehlt);
            p.Add(x => x.GewaehlteZone, Ziele.FirstOrDefault(z => z.Schluessel == gewaehlt)?.Ansichtsschluessel);
            p.Add(x => x.RaumUmgehaengt, (GebaeudeRaumumhaengung u) => gemeldet.Add(u));
            p.Add(x => x.ZielChanged, (string? z) => wahl.Add(z));
        });
        cut.FindAll("button[role=tab]").First(k => k.TextContent.Trim().StartsWith("Körper", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.KoerperGezeigt));
        return (cut, modul);
    }

    private static IElement Werkzeugknopf(IRenderedComponent<GebaeudeAnsicht> cut, string schluessel)
        => cut.Find(".epos-gebansicht-werkzeug button[data-werkzeug='" + schluessel + "']");

    private static IElement Sichtknopf(IRenderedComponent<GebaeudeAnsicht> cut, string zone)
        => cut.Find(".epos-gebansicht-sichtlegende li[data-zone='" + zone + "'] button");

    private static IElement Gruppenkaestchen(IRenderedComponent<GebaeudeAnsicht> cut, string gruppe)
        => cut.Find(".epos-gebansicht-bauteilgruppe[data-gruppe='" + gruppe + "'] input");

    private static string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);

    [Fact]
    public void AB4_Die_Werkzeugwahl_steht_an_der_Legende_Vorgabe_Zuordnen()
    {
        var (cut, _) = ZeigeKoerper(new(), new());
        Assert.Equal("zuordnen", cut.Instance.Werkzeug);
        Assert.Equal("Werkzeug der Legende", cut.Find(".epos-gebansicht-werkzeug").GetAttribute("aria-label"));
        IElement zuordnen = Werkzeugknopf(cut, "zuordnen");
        IElement ausblenden = Werkzeugknopf(cut, "ausblenden");
        Assert.Equal(("Zuordnen", "true"), (zuordnen.TextContent.Trim(), zuordnen.GetAttribute("aria-pressed")));
        Assert.Equal(("Ein-/Ausblenden", "false"), (ausblenden.TextContent.Trim(), ausblenden.GetAttribute("aria-pressed")));
        Assert.Equal("Alle zeigen", cut.Find(".epos-gebansicht-allezeigen").TextContent.Trim());
        // Im Werkzeug Zuordnen bleibt AB2: die Zielknöpfe, keine Sichtlegende.
        Assert.NotEmpty(cut.FindAll(".epos-gebansicht-legende--zielwahl .epos-gebansicht-zielknopf"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-sichtlegende"));

        ausblenden.Click();
        Assert.Equal("ausblenden", cut.Instance.Werkzeug);
        Assert.Equal("true", Werkzeugknopf(cut, "ausblenden").GetAttribute("aria-pressed"));
        Assert.Equal(new[] { Z1, Z2, "" }, cut.FindAll(".epos-gebansicht-sichtlegende li").Select(e => e.GetAttribute("data-zone")));
        Assert.All(cut.FindAll(".epos-gebansicht-sichtknopf"), k => Assert.Equal("true", k.GetAttribute("aria-pressed")));

        // In den Gruppenmodi schalten die Legenden ohnehin: dort nur „Alle zeigen".
        cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='randbedingung']").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        Assert.Empty(cut.FindAll(".epos-gebansicht-werkzeugknopf"));
        Assert.Single(cut.FindAll(".epos-gebansicht-allezeigen"));
    }

    [Fact]
    public async Task AB4_Der_Klick_im_Werkzeug_Ausblenden_schaltet_die_Zone_und_ordnet_nichts_zu()
    {
        var gemeldet = new List<GebaeudeRaumumhaengung>();
        var wahl = new List<string?>();
        var (cut, modul) = ZeigeKoerper(gemeldet, wahl, gewaehlt: "P-2");
        Assert.True(cut.Instance.OrdnetZu);
        Werkzeugknopf(cut, "ausblenden").Click();
        Assert.False(cut.Instance.OrdnetZu);

        Sichtknopf(cut, Z1).Click();
        Assert.False(cut.Instance.ZoneSichtbar(Z1));
        Assert.True(cut.Instance.ZoneSichtbar(Z2));
        Assert.False(cut.Instance.RaumSichtbar("r-a"));
        IElement knopf = Sichtknopf(cut, Z1);
        Assert.Equal("false", knopf.GetAttribute("aria-pressed"));
        Assert.Equal("Zone 1 einblenden", knopf.GetAttribute("title"));
        Assert.Contains("epos-gebansicht-legendeneintrag--aus", knopf.ParentElement!.ClassName);
        Assert.Equal("Zone 2 ausblenden", Sichtknopf(cut, Z2).GetAttribute("title"));
        // Keine Zielwahl, keine Zuordnung — auch ein Klick auf einen Körper ordnet im Werkzeug Ausblenden nicht zu.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(Z2, "r-b"));
        Assert.Empty(wahl);
        Assert.Empty(gemeldet);

        // Die Szene trägt den Raum der ausgeblendeten Zone nicht mehr: das Modul baut ihn nicht, der Klick trifft ihn nicht.
        cut.WaitForAssertion(() => Assert.NotEmpty(modul.Invocations["aktualisieren"]));
        string szene = Json(modul.Invocations["aktualisieren"].Last().Arguments[0]!);
        Assert.DoesNotContain("\"raum\":\"r-a\"", szene);
        Assert.Contains("\"raum\":\"r-b\"", szene);

        // Räume ohne Zone schalten über „nicht zugeordnet".
        Sichtknopf(cut, "").Click();
        Assert.False(cut.Instance.ZoneSichtbar(null));
        Assert.False(cut.Instance.RaumSichtbar("r-c"));

        // Zurück im Werkzeug Zuordnen: die Sichtbarkeit bleibt, der Klick ordnet wieder zu.
        Werkzeugknopf(cut, "zuordnen").Click();
        Assert.False(cut.Instance.ZoneSichtbar(Z1));
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(Z2, "r-b"));
        Assert.Equal(new[] { new GebaeudeRaumumhaengung("r-b", Z2) }, gemeldet);
    }

    [Fact]
    public void AB4_Alle_zeigen_setzt_Legende_und_Gruppen_zurueck()
    {
        var (cut, _) = ZeigeKoerper(new(), new());
        Werkzeugknopf(cut, "ausblenden").Click();
        Sichtknopf(cut, Z2).Click();
        Gruppenkaestchen(cut, "fenster").Change(false);
        Assert.False(cut.Instance.ZoneSichtbar(Z2));
        Assert.False(cut.Instance.BauteilgruppeSichtbar(Bauteilartgruppe.Fenster));
        Assert.Equal("true", cut.Find(".epos-gebansicht-allezeigen").GetAttribute("data-ausgeblendet"));

        cut.Find(".epos-gebansicht-allezeigen").Click();
        Assert.True(cut.Instance.ZoneSichtbar(Z2));
        Assert.True(cut.Instance.BauteilgruppeSichtbar(Bauteilartgruppe.Fenster));
        Assert.Equal("false", cut.Find(".epos-gebansicht-allezeigen").GetAttribute("data-ausgeblendet"));
        Assert.All(cut.FindAll(".epos-gebansicht-sichtknopf"), k => Assert.Equal("true", k.GetAttribute("aria-pressed")));
        Assert.True(Gruppenkaestchen(cut, "fenster").HasAttribute("checked"));
    }

    [Fact]
    public void AB4_Die_Gruppenliste_zeigt_nur_vorkommende_Gruppen_mit_Zahl()
    {
        var (cut, _) = ZeigeKoerper(new(), new());
        Assert.Equal("Bauteilgruppen", cut.Find(".epos-gebansicht-bauteilgruppen > legend").TextContent.Trim());
        IReadOnlyList<IElement> eintraege = cut.FindAll(".epos-gebansicht-bauteilgruppe").ToList();
        Assert.Equal(new[] { "aussenwand", "innenwand", "boden", "fenster" }, eintraege.Select(e => e.GetAttribute("data-gruppe")));
        Assert.Equal(new[] { "Außenwand (1)", "Innenwand (1)", "Bodenplatte (1)", "Fenster (1)" },
                     eintraege.Select(e => e.TextContent.Trim()));
        Assert.All(eintraege, e => Assert.True(e.QuerySelector("input")!.HasAttribute("checked")));

        // Ohne Bauteilarten zählen die Platten des Exportmodells (Raum B: Außen- und Innenwand, Bodenplatte, Decke).
        cut.Render(p => p.Add(x => x.Daten, Daten(mitArten: false)));
        Assert.Equal(new[] { "aussenwand", "innenwand", "decke", "boden" },
                     cut.FindAll(".epos-gebansicht-bauteilgruppe").Select(e => e.GetAttribute("data-gruppe")));
    }

    [Fact]
    public void AB4_Sichtbarkeit_ist_Legende_und_Gruppe()
    {
        var (cut, modul) = ZeigeKoerper(new(), new());
        cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='randbedingung']").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        Assert.True(cut.Instance.BauteilSichtbar("f-1"));

        // Gruppe aus, Legende an: unsichtbar, und der Fensterkörper fällt aus der Szene.
        Gruppenkaestchen(cut, "fenster").Change(false);
        Assert.True(cut.Instance.GruppeSichtbar(Randgruppe.R7));
        Assert.False(cut.Instance.BauteilSichtbar("f-1"));
        cut.WaitForAssertion(() => Assert.Contains("\"bauteile\":[]", Json(modul.Invocations["aktualisieren"].Last().Arguments[0]!)));

        // Gruppe an, Legende aus: ebenso unsichtbar.
        Gruppenkaestchen(cut, "fenster").Change(true);
        Assert.True(cut.Instance.BauteilSichtbar("f-1"));
        cut.Find(".epos-gebansicht-randlegende button[data-gruppe='R7']").Click();
        Assert.False(cut.Instance.BauteilSichtbar("f-1"));

        // Beides an: sichtbar.
        cut.Find(".epos-gebansicht-randlegende button[data-gruppe='R7']").Click();
        Assert.True(cut.Instance.BauteilSichtbar("f-1"));

        // Die Szene: die Schalter der Gruppen, im Exportmodell fehlen die Platten einer abgewählten Gruppe.
        bool[] gruppen = Enumerable.Repeat(true, GebaeudeAnsichtBauteilartgruppen.ZAHL).ToArray();
        gruppen[(int)Bauteilartgruppe.Aussenwand] = false;
        string export = Json(GebaeudeAnsicht.Szene(Daten(), null, bauteilgruppensichtbar: gruppen));
        Assert.Contains("\"kanten\":[[null,\"Innenwand\",null,null]],\"boden\":\"Bodenplatte\",\"decke\":\"Decke\"", export);
        Assert.Contains("\"bauteilgruppensichtbar\":[false,true,true,true,true,true,true,true,true]", export);
        Assert.Contains("\"kanten\":[[\"Aussenwand\",\"Innenwand\",null,null]]", Json(GebaeudeAnsicht.Szene(Daten(), null)));
    }

    [Fact]
    public void AB4_Ein_Wechsel_des_Farbmodus_setzt_die_Legende_zurueck_die_Gruppen_bleiben()
    {
        var (cut, _) = ZeigeKoerper(new(), new());
        Werkzeugknopf(cut, "ausblenden").Click();
        Sichtknopf(cut, Z1).Click();
        Gruppenkaestchen(cut, "aussenwand").Change(false);

        cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='randbedingung']").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        Assert.True(cut.Instance.ZoneSichtbar(Z1));
        Assert.False(cut.Instance.BauteilgruppeSichtbar(Bauteilartgruppe.Aussenwand));
        cut.Find(".epos-gebansicht-randlegende button[data-gruppe='R1']").Click();
        Assert.False(cut.Instance.GruppeSichtbar(Randgruppe.R1));

        cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='zonen']").Click();
        cut.WaitForAssertion(() => Assert.Equal("zonen", cut.Instance.Farbmodus));
        Assert.True(cut.Instance.GruppeSichtbar(Randgruppe.R1));
        Assert.False(cut.Instance.BauteilgruppeSichtbar(Bauteilartgruppe.Aussenwand));
        Assert.False(Gruppenkaestchen(cut, "aussenwand").HasAttribute("checked"));
    }

    [Fact]
    public void AB4_Das_Bytefeld_traegt_je_Raumdreieck_die_Bauteilgruppe_nur_mit_Bauteilarten()
    {
        GebaeudeAnsichtKoerperfeld mit = Daten().Koerperfeld();
        GebaeudeAnsichtKoerperfeld ohne = Daten(mitArten: false).Koerperfeld();
        int ab = mit.Verzeichnis.Single().BauteilgruppenAb;
        Assert.True(ab > 0);
        Assert.Equal(new[] { (byte)Bauteilartgruppe.Aussenwand, (byte)Bauteilartgruppe.Boden }, mit.Bytes.Skip(ab).Take(2));
        Assert.Equal(-1, ohne.Verzeichnis.Single().BauteilgruppenAb);
        // Ohne Bauteilarten ist das Feld byteweise das bisherige; mit ihnen hängt Teil 6 hinten an.
        Assert.Equal(ohne.Bytes.Take(ab), mit.Bytes.Take(ab));

        string json = Json(GebaeudeAnsicht.SzeneDatei(Daten(), null, mit));
        Assert.Contains("\"bauteilgruppenAb\":" + ab, json);
        bool[] gruppen = Enumerable.Repeat(true, GebaeudeAnsichtBauteilartgruppen.ZAHL).ToArray();
        gruppen[(int)Bauteilartgruppe.Fenster] = false;
        Assert.Contains("\"bauteile\":[]", Json(GebaeudeAnsicht.SzeneDatei(Daten(), null, mit, "randbedingung", bauteilgruppensichtbar: gruppen)));
        Assert.DoesNotContain("\"raum\":\"r-a\"", Json(GebaeudeAnsicht.SzeneDatei(Daten(), null, mit, zonenAus: new[] { Z1 })));
    }
}
