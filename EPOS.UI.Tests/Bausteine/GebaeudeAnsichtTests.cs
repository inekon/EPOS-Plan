using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="GebaeudeAnsicht"/> — der Grundriss eines Gebäudes als SVG (Stufe G6c, Welle D2;
/// Entscheid E11, Mehrzonenkonzept 6.7).
///
/// <para><b>Was hier bewiesen wird</b> (Abnahme 6.7): Polygone je Geschoss in Metern mit gespiegeltem y
/// (Nord oben), die Zonenfarbe nach der Stelle und grau ohne Zone, die Kennzeichnung „schematisch" je Geschoss,
/// je Raum und in der Legende, die Herkunft je Zone, der Geschosswechsel, der Klick (und die Tastatur) meldet
/// Raumkennung und gewählte Zone — ohne Zone „als eigene Zone" —, ohne Umhängbarkeit keine Meldung, die
/// Körperansicht (Probe 26: Umschalter, Canvas, „schematisch", Klick meldet die Zone, ohne WebGL benannt), der Fall ohne Gaben, und DETERMINISMUS: dieselben Daten geben zeichengleich
/// dasselbe Markup, auch unter einer anderen Kultur.</para>
///
/// <para><b>Was hier NICHT bewiesen wird:</b> Farbe, Strichbreite und Lage am Schirm — bunit hat kein Layout.
/// Die Regeln dazu hält <c>StilblattTests</c>; die Sichtprüfung gehört zur Windows-Abnahme.</para>
///
/// <para>Die Klasse pinnt die Sprache (de-DE über <see cref="EposBunitContext"/>): Sie prüft deutsche Texte.</para>
/// </summary>
public class GebaeudeAnsichtTests : EposBunitContext
{
    private const string ZONE_KG = "A1|KG|U";
    private const string ZONE_EG = "A1|EG|B";

    // =====================================================================
    //  Die Daten der Fälle
    // =====================================================================

    private static IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygon(params (double X, double Y)[] punkte)
        => new[] { (IReadOnlyList<GebaeudeAnsichtPunkt>)punkte.Select(p => new GebaeudeAnsichtPunkt(p.X, p.Y)).ToList() };

    private static readonly IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> KeinPolygon =
        Array.Empty<IReadOnlyList<GebaeudeAnsichtPunkt>>();

    /// <summary>
    /// Zwei Geschosse in der Reihenfolge der Höhenlage: das Kellergeschoss schematisch mit dem unbeheizten Lager,
    /// das Erdgeschoss aus Raumgrenzen mit Wohnen und Küche (Zone „Erdgeschoss", von Hand verändert), einem Flur
    /// ohne Zone und einem Schacht ohne Umriss.
    /// </summary>
    private static GebaeudeAnsichtDaten Daten(bool umhaengbar = true) => new()
    {
        Geschosse = new[]
        {
            new GebaeudeAnsichtGeschoss("g-kg", "Kellergeschoss", true, 0, 0, 8, 5, new[]
            {
                new GebaeudeAnsichtRaum("r-lager", "Lager", ZONE_KG, false, true, "40 m²", Polygon((0, 0), (8, 0), (8, 5), (0, 5))),
            }),
            new GebaeudeAnsichtGeschoss("g-eg", "Erdgeschoss", false, 0, 0, 10, 9, new[]
            {
                new GebaeudeAnsichtRaum("r-wohnen", "Wohnen", ZONE_EG, true, false, "48 m²", Polygon((0, 0), (6, 0), (6, 8), (0, 8))),
                new GebaeudeAnsichtRaum("r-kueche", "Küche", ZONE_EG, true, false, "32 m²", Polygon((6, 0), (10, 0), (10, 8), (6, 8))),
                new GebaeudeAnsichtRaum("r-flur", "Flur", null, true, false, "10 m²", Polygon((0, 8), (10, 8), (10, 9), (0, 9))),
                new GebaeudeAnsichtRaum("r-schacht", "Schacht", ZONE_EG, true, false, "1 m²", KeinPolygon),
            }),
        },
        Zonen = new[]
        {
            new GebaeudeAnsichtZone(ZONE_KG, "Kellergeschoss", 0, false, true, false),
            new GebaeudeAnsichtZone(ZONE_EG, "Erdgeschoss", 1, true, false, true),
        },
        Schematisch = true,
        Umhaengbar = umhaengbar,
        Hinweise = new[] { "1 Räume ohne Umriss aus Raumgrenzen, schematisch: Lager" },
    };

    private IRenderedComponent<GebaeudeAnsicht> Zeige(
        GebaeudeAnsichtDaten? daten, string? zone = ZONE_KG, List<GebaeudeRaumumhaengung>? gemeldet = null,
        List<string>? geschosse = null, bool aktiv = true, GebaeudeAnsichtTexte? texte = null)
        => Render<GebaeudeAnsicht>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.GewaehlteZone, zone);
            p.Add(x => x.Aktiv, aktiv);
            if (gemeldet is not null) p.Add(x => x.RaumUmgehaengt, (GebaeudeRaumumhaengung u) => gemeldet.Add(u));
            if (geschosse is not null) p.Add(x => x.GeschossChanged, (string g) => geschosse.Add(g));
            if (texte is not null) p.Add(x => x.Texte, texte);
        });

    private static IElement Raum(IRenderedComponent<GebaeudeAnsicht> cut, string kennung)
        => cut.Find("g.epos-gebansicht-raum[data-raum='" + kennung + "']");

    private static IElement Reiterknopf(IRenderedComponent<GebaeudeAnsicht> cut, string text)
        => cut.FindAll("button[role=tab]").First(k => k.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

    private static void Erdgeschoss(IRenderedComponent<GebaeudeAnsicht> cut)
    {
        Reiterknopf(cut, "Erdgeschoss").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("g[data-raum='r-wohnen']")));
    }

    /// <summary>Das Markup ohne die Kennungen der Ereignisbindung, die der Zeichner je Lauf neu vergibt.</summary>
    private static string OhneBindungen(string markup) => Regex.Replace(markup, @"\s+blazor:[\w:]+=""[^""]*""", "");

    // =====================================================================
    //  Bild: Polygone, Spiegelung, Farbe, Beschriftung
    // =====================================================================

    [Fact]
    public void GA1_Das_unterste_Geschoss_mit_Umriss_steht_vorn_und_zeichnet_seine_Raeume()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());

        Assert.Equal("g-kg", cut.Instance.GezeigtesGeschoss!.Kennung);
        Assert.Equal("g-kg", cut.Find(".epos-gebansicht-geschoss").GetAttribute("data-geschoss"));
        IElement lager = Raum(cut, "r-lager");
        Assert.Equal("0.000,0.000 8.000,0.000 8.000,-5.000 0.000,-5.000", lager.QuerySelector("polygon")!.GetAttribute("points"));
        Assert.Single(cut.FindAll("g.epos-gebansicht-raum"));
        Assert.Equal(new[] { "Grundriss", "Körper", "Kellergeschoss (schematisch)", "Erdgeschoss" },
                     cut.FindAll("button[role=tab]").Select(k => k.TextContent.Trim()));
    }

    [Fact]
    public void GA1_Die_Polygone_stehen_in_Metern_mit_gespiegeltem_y_und_der_Ausschnitt_hat_Rand()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        Erdgeschoss(cut);

        // Nord oben: y wird −y; x bleibt. Kein „-0.000".
        Assert.Equal("0.000,0.000 6.000,0.000 6.000,-8.000 0.000,-8.000",
                     Raum(cut, "r-wohnen").QuerySelector("polygon")!.GetAttribute("points"));
        Assert.Equal("0.000,-8.000 10.000,-8.000 10.000,-9.000 0.000,-9.000",
                     Raum(cut, "r-flur").QuerySelector("polygon")!.GetAttribute("points"));

        // Ausschnitt: 10 × 9 m, Rand 4 % der größeren Seite (0,4 m), der nördlichste Punkt oben.
        IElement bild = cut.Find("svg.epos-gebansicht-bild");
        Assert.Equal("-0.400 -9.400 10.800 9.800", bild.GetAttribute("viewBox"));
        Assert.Equal("Grundriss Erdgeschoss", bild.GetAttribute("aria-label"));

        // Der Schacht hat keinen Umriss und steht nicht im Bild.
        Assert.Empty(cut.FindAll("g[data-raum='r-schacht']"));
        Assert.Equal(3, cut.FindAll("g.epos-gebansicht-raum").Count);
    }

    [Fact]
    public void GA1_Jede_Zone_traegt_die_Farbe_ihrer_Stelle_ohne_Zone_grau()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        Assert.Contains("epos-gebansicht-zone--0", Raum(cut, "r-lager").ClassName);
        Assert.Equal(ZONE_KG, Raum(cut, "r-lager").GetAttribute("data-zone"));

        Erdgeschoss(cut);
        Assert.Contains("epos-gebansicht-zone--1", Raum(cut, "r-wohnen").ClassName);
        Assert.Contains("epos-gebansicht-zone--1", Raum(cut, "r-kueche").ClassName);
        IElement flur = Raum(cut, "r-flur");
        Assert.Contains("epos-gebansicht-zone--ohne", flur.ClassName);
        Assert.False(flur.HasAttribute("data-zone"));

        // Die Palette läuft modulo zehn: Stelle 13 nimmt die Farbe 3.
        GebaeudeAnsichtDaten weit = Daten() with
        {
            Zonen = new[] { new GebaeudeAnsichtZone(ZONE_KG, "Kellergeschoss", 13, false, true, false) },
        };
        Assert.Contains("epos-gebansicht-zone--3", Raum(Zeige(weit), "r-lager").ClassName);
    }

    [Fact]
    public void GA1_Name_und_Flaeche_stehen_im_Raum_wenn_sie_hineinpassen()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        Erdgeschoss(cut);

        IReadOnlyList<IElement> texte = Raum(cut, "r-wohnen").QuerySelectorAll("text").ToList();
        Assert.Equal(new[] { "Wohnen", "48 m²" }, texte.Select(t => t.TextContent));
        Assert.Equal("3.000", texte[0].GetAttribute("x"));                 // Schwerpunkt des Rechtecks
        Assert.Equal("-4.056", texte[0].GetAttribute("y"));                // Name über dem Schwerpunkt
        Assert.Equal("-3.734", texte[1].GetAttribute("y"));                // Fläche darunter
        Assert.Equal("0.280", texte[0].GetAttribute("font-size"));         // 2,8 % der größeren Seite
        Assert.Equal("middle", texte[0].GetAttribute("text-anchor"));

        // Ein Raum, dessen Beschriftung nicht hineinpasst, trägt seinen Namen nur im Kurztext.
        GebaeudeAnsichtDaten eng = Daten() with
        {
            Geschosse = new[]
            {
                new GebaeudeAnsichtGeschoss("g-kg", "Kellergeschoss", false, 0, 0, 20, 20, new[]
                {
                    new GebaeudeAnsichtRaum("r-klein", "Hausanschlussraum", ZONE_KG, false, false, "1 m²",
                                            Polygon((0, 0), (1, 0), (1, 1), (0, 1))),
                    new GebaeudeAnsichtRaum("r-gross", "Halle", ZONE_KG, false, false, "300 m²",
                                            Polygon((2, 2), (20, 2), (20, 20), (2, 20))),
                }),
            },
        };
        IRenderedComponent<GebaeudeAnsicht> zweit = Zeige(eng);
        Assert.Empty(Raum(zweit, "r-klein").QuerySelectorAll("text"));
        Assert.Contains("Hausanschlussraum", Raum(zweit, "r-klein").QuerySelector("title")!.TextContent);
        Assert.Equal(2, Raum(zweit, "r-gross").QuerySelectorAll("text").Length);
    }

    // =====================================================================
    //  Kennzeichnung, Legende, Hinweise
    // =====================================================================

    [Fact]
    public void GA2_Schematisch_steht_am_Geschoss_am_Raum_und_in_der_Legende()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());

        // Geschoss: im Reiter und als Zeile über dem Bild.
        Assert.Equal("Kellergeschoss (schematisch)", Reiterknopf(cut, "Kellergeschoss").TextContent.Trim());
        IElement zeile = cut.Find(".epos-gebansicht-schematisch");
        Assert.Contains("schematisch", zeile.TextContent);
        Assert.Contains("Form und Lage sind erfunden", zeile.TextContent);

        // Raum: gestrichelt (Klasse) und das Wort in Beschreibung und Kurztext.
        IElement lager = Raum(cut, "r-lager");
        Assert.Contains("epos-gebansicht-raum--schematisch", lager.ClassName);
        Assert.Contains(", schematisch", lager.GetAttribute("aria-label"));
        Assert.Contains(", schematisch", lager.QuerySelector("title")!.TextContent);

        // Legende: die Zone mit Herkunft „schematisch" und „unbeheizt", dazu der gestrichelte Eintrag.
        IElement zone = cut.Find(".epos-gebansicht-legendeneintrag[data-zone='" + ZONE_KG + "']");
        Assert.Contains("Kellergeschoss", zone.TextContent);
        Assert.Equal("schematisch", zone.QuerySelector(".epos-gebansicht-herkunft")!.TextContent);
        Assert.Contains("unbeheizt", zone.TextContent);
        Assert.Single(cut.FindAll(".epos-gebansicht-legendeneintrag--schematisch"));

        // Das Erdgeschoss ist aus Raumgrenzen: keine Zeile, kein gestrichelter Raum.
        Erdgeschoss(cut);
        Assert.Empty(cut.FindAll(".epos-gebansicht-schematisch"));
        Assert.DoesNotContain("epos-gebansicht-raum--schematisch", Raum(cut, "r-wohnen").ClassName);
        Assert.Empty(cut.FindAll(".epos-gebansicht-legendeneintrag--schematisch"));
    }

    [Fact]
    public void GA2_Die_Legende_nennt_Zonen_des_Geschosses_mit_Herkunft_von_Hand_und_ohne_Zone()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), zone: ZONE_EG);
        Erdgeschoss(cut);

        IReadOnlyList<IElement> eintraege = cut.FindAll(".epos-gebansicht-legendeneintrag");
        Assert.Equal(2, eintraege.Count);                                   // Erdgeschoss und „ohne Zone"
        IElement eg = eintraege[0];
        Assert.Equal(ZONE_EG, eg.GetAttribute("data-zone"));
        Assert.Contains("epos-gebansicht-legendeneintrag--ziel", eg.ClassName);   // die gewählte Zielzone
        Assert.Equal("Erdgeschoss", eg.QuerySelector(".epos-gebansicht-zonenname")!.TextContent);
        Assert.Equal("aus Raumgrenzen", eg.QuerySelector(".epos-gebansicht-herkunft")!.TextContent);
        Assert.Contains("von Hand", eg.TextContent);
        Assert.DoesNotContain("unbeheizt", eg.TextContent);
        Assert.Contains("epos-gebansicht-zone--1", eg.QuerySelector(".epos-gebansicht-farbfeld")!.ClassName);
        Assert.Equal("ohne Zone", eintraege[1].TextContent.Trim());
        Assert.Contains("epos-gebansicht-zone--ohne", eintraege[1].QuerySelector(".epos-gebansicht-farbfeld")!.ClassName);

        // Die Räume der Zielzone tragen den kräftigeren Rand.
        Assert.Contains("epos-gebansicht-raum--ziel", Raum(cut, "r-wohnen").ClassName);
        Assert.DoesNotContain("epos-gebansicht-raum--ziel", Raum(cut, "r-flur").ClassName);

        // Die Hinweise der Geometrie stehen als Liste.
        Assert.Equal(new[] { "1 Räume ohne Umriss aus Raumgrenzen, schematisch: Lager" },
                     cut.FindAll(".epos-gebansicht-hinweise li").Select(l => l.TextContent));
    }

    [Fact]
    public void GA2_Ohne_Umriss_im_Geschoss_steht_ein_Hinweis_statt_eines_leeren_Bilds()
    {
        GebaeudeAnsichtDaten daten = Daten() with
        {
            Geschosse = new[]
            {
                new GebaeudeAnsichtGeschoss("", "", false, 0, 0, 0, 0, new[]
                {
                    new GebaeudeAnsichtRaum("r-x", "Raum X", ZONE_EG, true, false, "—", KeinPolygon),
                }),
            },
        };
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(daten);
        Assert.Empty(cut.FindAll("svg"));
        Assert.Equal("In diesem Geschoss trägt kein Raum einen Umriss.", cut.Find(".epos-gebansicht-ohneumriss").TextContent);
        Assert.Equal("ohne Geschoss", cut.Find(".epos-gebansicht-geschossname").TextContent);   // ein Geschoss: kein Reiter
    }

    // =====================================================================
    //  Geschosswahl und Umschalter
    // =====================================================================

    [Fact]
    public void GA3_Der_Geschosswechsel_zeichnet_das_andere_Geschoss_und_meldet_seine_Kennung()
    {
        var geschosse = new List<string>();
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), geschosse: geschosse);
        Assert.Equal("true", Reiterknopf(cut, "Kellergeschoss").GetAttribute("aria-selected"));

        Erdgeschoss(cut);
        Assert.Equal(new[] { "g-eg" }, geschosse);
        Assert.Equal("true", Reiterknopf(cut, "Erdgeschoss").GetAttribute("aria-selected"));
        Assert.Empty(cut.FindAll("g[data-raum='r-lager']"));

        // Ein gebundenes Geschoss vom Wirt gilt.
        cut.Render(p => p.Add(x => x.Geschoss, "g-kg"));
        Assert.NotEmpty(cut.FindAll("g[data-raum='r-lager']"));
        Assert.Equal("g-kg", cut.Instance.GezeigtesGeschoss!.Kennung);
    }

    // =====================================================================
    //  Körper (G7b, E66; Probe 26 des Datenaustauschkonzepts)
    // =====================================================================

    private const string MODUL = GebaeudeAnsicht.MODUL_KOERPER;

    /// <summary>Die Fälle mit Körperangaben: Wohnen 2,75 m hoch mit Außenwand an der Südkante, Boden und Decke.</summary>
    private static GebaeudeAnsichtDaten KoerperDaten(double? lageEg = null) => Daten() with
    {
        Koerperraeume = new[]
        {
            new GebaeudeAnsichtKoerperraum("r-wohnen", 2.75,
                new[] { (IReadOnlyList<string?>)new string?[] { "Aussenwand", null, null, null } }, "Bodenplatte", "Decke"),
            new GebaeudeAnsichtKoerperraum("r-kueche", 2.75, new[] { (IReadOnlyList<string?>)new string?[4] }, null, null),
        },
        Geschosslagen = new[] { new GebaeudeAnsichtGeschosslage("g-kg", null), new GebaeudeAnsichtGeschosslage("g-eg", lageEg) },
    };

    private IRenderedComponent<GebaeudeAnsicht> ZeigeKoerper(GebaeudeAnsichtDaten daten, List<string>? zonen = null, bool aktiv = true)
        => Render<GebaeudeAnsicht>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Aktiv, aktiv);
            if (zonen is not null) p.Add(x => x.ZoneGewaehlt, (string z) => zonen.Add(z));
        });

    [Fact]
    public void GA3_Der_Umschalter_wechselt_auf_Koerper_mit_Canvas_und_Kennzeichnung_und_zurueck()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var erzeugen = modul.Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);
        var entsorgen = modul.SetupVoid("entsorgen", _ => true);

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(KoerperDaten());
        IElement koerper = Reiterknopf(cut, "Körper");
        Assert.False(koerper.HasAttribute("aria-disabled"));
        Assert.Empty(cut.FindAll("canvas"));
        Assert.Empty(JSInterop.Invocations.Where(a => a.Identifier == "import"));   // erst beim ersten Wechsel

        koerper.Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));
        Assert.True(cut.Instance.KoerperGezeigt);
        Assert.Equal("true", Reiterknopf(cut, "Körper").GetAttribute("aria-selected"));
        Assert.Empty(cut.FindAll("svg.epos-gebansicht-bild"));
        IElement canvas = cut.Find(".epos-gebansicht-koerperansicht canvas.epos-gebansicht-canvas");
        Assert.Equal("img", canvas.GetAttribute("role"));
        Assert.Equal("schematisch", cut.Find(".epos-gebansicht-koerperansicht .epos-gebansicht-schematisch .epos-gebansicht-marke").TextContent);
        Assert.Equal(3, erzeugen.Invocations.Single().Arguments.Count);
        Assert.IsType<DotNetObjectReference<GebaeudeAnsicht>>(erzeugen.Invocations.Single().Arguments[2]);

        // Herkunft je Zone in der Legende, dazu die Höhe als Vorgabe beim Keller (kein Körperraum).
        IElement kg = cut.Find(".epos-gebansicht-koerperansicht li[data-zone='" + ZONE_KG + "']");
        Assert.Equal("schematisch", kg.QuerySelector(".epos-gebansicht-herkunft")!.TextContent);
        Assert.NotNull(kg.QuerySelector(".epos-gebansicht-marke--hoehe"));
        IElement eg = cut.Find(".epos-gebansicht-koerperansicht li[data-zone='" + ZONE_EG + "']");
        Assert.Equal("aus Raumgrenzen", eg.QuerySelector(".epos-gebansicht-herkunft")!.TextContent);
        Assert.Null(eg.QuerySelector(".epos-gebansicht-marke--hoehe"));

        // Zurück: die Szene wird entsorgt, der Grundriss steht wieder.
        Reiterknopf(cut, "Grundriss").Click();
        cut.WaitForAssertion(() => Assert.Single(entsorgen.Invocations));
        Assert.False(cut.Instance.KoerperGezeigt);
        Assert.Empty(cut.FindAll("canvas"));
        Assert.NotEmpty(cut.FindAll("svg.epos-gebansicht-bild"));

        // Wieder hin: das Modul ist geladen, die Szene entsteht neu.
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, erzeugen.Invocations.Count));
        Assert.Single(JSInterop.Invocations.Where(a => a.Identifier == "import"));
    }

    [Fact]
    public void GA3_Neue_Daten_gehen_ueber_aktualisieren_hinein()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.Setup<bool>("erzeugen", _ => true).SetResult(true);
        var aktualisieren = modul.SetupVoid("aktualisieren", _ => true);

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(KoerperDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.KoerperGezeigt));
        Assert.Empty(aktualisieren.Invocations);

        cut.Render(p => p.Add(x => x.GewaehlteZone, ZONE_EG));
        cut.WaitForAssertion(() => Assert.Single(aktualisieren.Invocations));
    }

    [Fact]
    public void GA3_Ohne_WebGL_steht_die_Hinweiszeile_statt_des_Canvas()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        JSInterop.SetupModule(MODUL).Setup<bool>("erzeugen", _ => true).SetResult(false);

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(KoerperDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebansicht-ohnewebgl")));
        Assert.True(cut.Instance.KoerperNichtVerfuegbar);
        Assert.Empty(cut.FindAll("canvas"));
        Assert.StartsWith("Körper: Diese Umgebung kann keine 3D-Grafik zeichnen", cut.Find(".epos-gebansicht-ohnewebgl").TextContent);
        Assert.Equal("status", cut.Find(".epos-gebansicht-ohnewebgl").GetAttribute("role"));
    }

    [Fact]
    public void GA3_Laedt_das_Modul_nicht_steht_die_Hinweiszeile()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;   // kein Modul eingerichtet: der Import wirft

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(KoerperDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebansicht-ohnewebgl")));
        Assert.Empty(cut.FindAll("canvas"));
    }

    [Fact]
    public async Task GA3_Der_Klick_auf_einen_Koerper_meldet_die_Zone_an_den_Wirt()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(MODUL).Setup<bool>("erzeugen", _ => true).SetResult(true);
        var zonen = new List<string>();
        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(KoerperDaten(), zonen);
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.KoerperGezeigt));

        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE_EG, "r-wohnen"));
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(null, "r-flur"));          // ohne Zone: nichts
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick("Erdgeschoss", "r-wohnen")); // ein NAME ist kein Schlüssel
        Assert.Equal(new[] { ZONE_EG }, zonen);

        // Gesperrt meldet der Klick nichts.
        cut.Render(p => p.Add(x => x.Aktiv, false));
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE_KG, "r-lager"));
        Assert.Single(zonen);
    }

    [Fact]
    public void GA3_Die_Koerper_stapeln_Geschosse_ohne_Lage_und_nehmen_die_Vorgabehoehe()
    {
        IReadOnlyList<GebaeudeAnsichtKoerper> koerper = KoerperDaten().Koerper();
        Assert.Equal(new[] { "r-lager", "r-wohnen", "r-kueche", "r-flur" }, koerper.Select(k => k.Raum.Kennung));  // Schacht ohne Umriss fehlt

        GebaeudeAnsichtKoerper lager = koerper[0], wohnen = koerper[1], flur = koerper[3];
        Assert.Equal((0.0, 3.0, true, true), (lager.UnterkanteM, lager.HoeheM, lager.HoeheVorgabe, lager.Schematisch));
        Assert.Equal((3.0, 2.75, false, false), (wohnen.UnterkanteM, wohnen.HoeheM, wohnen.HoeheVorgabe, wohnen.Schematisch));
        Assert.Equal((3.0, 3.0, true, true), (flur.UnterkanteM, flur.HoeheM, flur.HoeheVorgabe, flur.Schematisch));
        Assert.Equal("Aussenwand", wohnen.Angaben!.Kanten[0][0]);
        Assert.Null(flur.Angaben);

        // Eine Höhenlage der Datei gilt vor dem Stapeln.
        Assert.Equal(2.5, KoerperDaten(lageEg: 2.5).Koerper()[1].UnterkanteM);
        // Ohne Körperangaben stehen alle Körper auf der Vorgabe.
        Assert.All(Daten().Koerper(), k => Assert.True(k.HoeheVorgabe));
    }

    [Fact]
    public void GA3_Die_Szene_traegt_Kennungen_und_Zahlen_keinen_Anzeigetext()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.Szene(KoerperDaten(), ZONE_EG));
        Assert.Contains("\"raum\":\"r-wohnen\"", json);
        Assert.Contains("\"stelle\":1", json);
        Assert.Contains("\"hoehe\":2.75", json);
        Assert.Contains("\"Aussenwand\"", json);
        foreach (string text in new[] { "Wohnen", "Küche", "Erdgeschoss", "Kellergeschoss", "m²" })
            Assert.DoesNotContain(text, json);
        // Deterministisch: zweimal dieselbe Szene.
        Assert.Equal(json, System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.Szene(KoerperDaten(), ZONE_EG)));
    }

    // =====================================================================
    //  Klick und Tastatur
    // =====================================================================

    [Fact]
    public void GA4_Der_Klick_meldet_Raum_und_gewaehlte_Zone()
    {
        var gemeldet = new List<GebaeudeRaumumhaengung>();
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), zone: ZONE_KG, gemeldet: gemeldet);
        Erdgeschoss(cut);

        IElement kueche = Raum(cut, "r-kueche");
        Assert.Equal("0", kueche.GetAttribute("tabindex"));
        Assert.Equal("button", kueche.GetAttribute("role"));
        Assert.Equal("Küche, 32 m², Zone „Erdgeschoss“ — Antippen ordnet der Zone „Kellergeschoss“ zu",
                     kueche.GetAttribute("aria-label"));
        Assert.Contains("epos-gebansicht-bild--bedienbar", cut.Find("svg").ClassName);

        kueche.Click();
        Assert.Equal(new[] { new GebaeudeRaumumhaengung("r-kueche", ZONE_KG) }, gemeldet);
        Raum(cut, "r-flur").Click();
        Assert.Equal(new GebaeudeRaumumhaengung("r-flur", ZONE_KG), gemeldet[1]);
    }

    [Fact]
    public void GA4_Als_eigene_Zone_meldet_der_Klick_keine_Zielzone()
    {
        var gemeldet = new List<GebaeudeRaumumhaengung>();
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), zone: null, gemeldet: gemeldet);
        Erdgeschoss(cut);

        IElement kueche = Raum(cut, "r-kueche");
        Assert.EndsWith("— Antippen trennt als eigene Zone ab", kueche.GetAttribute("aria-label"));
        Assert.DoesNotContain("epos-gebansicht-raum--ziel", kueche.ClassName);
        kueche.Click();
        GebaeudeRaumumhaengung u = Assert.Single(gemeldet);
        Assert.Equal("r-kueche", u.Raum);
        Assert.Null(u.Zielzone);
    }

    [Fact]
    public void GA4_Enter_und_Leertaste_am_fokussierten_Raum_ordnen_zu_andere_Tasten_nicht()
    {
        var gemeldet = new List<GebaeudeRaumumhaengung>();
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), zone: ZONE_EG, gemeldet: gemeldet);

        Raum(cut, "r-lager").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Raum(cut, "r-lager").KeyDown(new KeyboardEventArgs { Key = " " });
        Raum(cut, "r-lager").KeyDown(new KeyboardEventArgs { Key = "a" });
        Raum(cut, "r-lager").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.Equal(new[] { new GebaeudeRaumumhaengung("r-lager", ZONE_EG), new GebaeudeRaumumhaengung("r-lager", ZONE_EG) },
                     gemeldet);
    }

    [Fact]
    public void GA4_Ohne_Umhaengbarkeit_ist_die_Ansicht_reine_Anzeige_mit_Hinweis()
    {
        var gemeldet = new List<GebaeudeRaumumhaengung>();
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(umhaengbar: false), gemeldet: gemeldet);

        IElement lager = Raum(cut, "r-lager");
        Assert.False(lager.HasAttribute("tabindex"));
        Assert.Equal("img", lager.GetAttribute("role"));
        Assert.DoesNotContain("Antippen", lager.GetAttribute("aria-label"));
        Assert.DoesNotContain("epos-gebansicht-bild--bedienbar", cut.Find("svg").ClassName);
        Assert.Equal("Nur Anzeige: Räume lassen sich unter einer Zonenregel mit mehreren Zonen zuordnen.",
                     cut.Find(".epos-gebansicht-nuranzeige").TextContent);

        lager.Click();
        lager.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Empty(gemeldet);
        Assert.False(cut.Instance.Bedienbar);
    }

    [Fact]
    public void GA4_Gesperrt_oder_ohne_Rueckruf_meldet_der_Klick_nichts()
    {
        var gemeldet = new List<GebaeudeRaumumhaengung>();
        IRenderedComponent<GebaeudeAnsicht> gesperrt = Zeige(Daten(), gemeldet: gemeldet, aktiv: false);
        Raum(gesperrt, "r-lager").Click();
        Assert.Empty(gemeldet);
        Assert.False(gesperrt.Instance.Bedienbar);
        Assert.Empty(gesperrt.FindAll(".epos-gebansicht-nuranzeige"));   // gesperrt ist nicht „nur Anzeige"

        IRenderedComponent<GebaeudeAnsicht> ohne = Zeige(Daten());        // kein Delegat, kein Knopf
        Assert.False(ohne.Instance.Bedienbar);
        Assert.False(Raum(ohne, "r-lager").HasAttribute("tabindex"));
    }

    [Fact]
    public void GA5_Ohne_Gaben_steht_der_Leerhinweis()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Render<GebaeudeAnsicht>();
        Assert.Equal("Kein Grundriss vorhanden.", cut.Find(".epos-gebansicht-leer").TextContent);
        Assert.Empty(cut.FindAll("svg"));
        Assert.Empty(cut.FindAll("button"));

        IRenderedComponent<GebaeudeAnsicht> leer = Zeige(new GebaeudeAnsichtDaten());
        Assert.Equal("Kein Grundriss vorhanden.", leer.Find(".epos-gebansicht-leer").TextContent);
    }

    // =====================================================================
    //  Determinismus und Kultur
    // =====================================================================

    [Fact]
    public void GA6_Dieselben_Daten_geben_zeichengleich_dasselbe_Markup()
    {
        var texte = new GebaeudeAnsichtTexte();
        string erstes = OhneBindungen(Zeige(Daten(), texte: texte).Markup);
        string zweites = OhneBindungen(Zeige(Daten(), texte: texte).Markup);
        Assert.Equal(erstes, zweites);
        Assert.Contains("points=\"0.000,0.000 8.000,0.000 8.000,-5.000 0.000,-5.000\"", erstes);

        // Auch nach einem Umweg über das andere Geschoss steht dasselbe Bild wieder da.
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), texte: texte);
        Erdgeschoss(cut);
        Reiterknopf(cut, "Kellergeschoss").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("g[data-raum='r-lager']")));
        Assert.Equal(erstes, OhneBindungen(cut.Markup));
    }

    [Fact]
    public void GA6_Unter_en_US_stehen_dieselben_Koordinaten_wie_unter_de_DE()
    {
        var texte = new GebaeudeAnsichtTexte();
        IRenderedComponent<GebaeudeAnsicht> deutsch = Zeige(Daten(), texte: texte);
        Erdgeschoss(deutsch);
        string markupDeutsch = OhneBindungen(deutsch.Markup);

        string markupEnglisch;
        List<string?> punkteEnglisch;
        using (new Kulturvorrichtung("en-US"))
        {
            Assert.Equal("1.5", 1.5.ToString(CultureInfo.CurrentCulture));
            IRenderedComponent<GebaeudeAnsicht> englisch = Zeige(Daten(), texte: texte);
            Erdgeschoss(englisch);
            markupEnglisch = OhneBindungen(englisch.Markup);
            punkteEnglisch = englisch.FindAll("polygon").Select(p => p.GetAttribute("points")).ToList();
        }

        Assert.Equal("1,5", 1.5.ToString(CultureInfo.CurrentCulture));
        Assert.Equal(deutsch.FindAll("polygon").Select(p => p.GetAttribute("points")), punkteEnglisch);
        Assert.Equal(markupDeutsch, markupEnglisch);
        Assert.DoesNotContain("0,000", markupDeutsch);
    }

    [Fact]
    public void GA6_Die_Texte_stehen_auch_englisch()
    {
        GebaeudeAnsichtTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeAnsichtTexte();
        Assert.Equal("Floor plan", englisch.Grundriss);
        Assert.Equal("Solids", englisch.Koerper);
        Assert.Equal("height defaulted", englisch.HoeheVorgabe);
        Assert.StartsWith("Solids: this environment cannot draw 3D graphics", englisch.KoerperOhneWebgl);
        Assert.Equal("schematic", englisch.Schematisch);
        Assert.Equal("from space boundaries", englisch.Raumgrenzen);
        Assert.Equal("no zone", englisch.OhneZone);

        var gemeldet = new List<GebaeudeRaumumhaengung>();
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten(), zone: null, gemeldet: gemeldet, texte: englisch);
        Assert.Equal("Kellergeschoss (schematic)", Reiterknopf(cut, "Kellergeschoss").TextContent.Trim());
        Assert.Equal("Lager, 40 m², zone “Kellergeschoss”, schematic — Tapping splits off as a zone of its own",
                     Raum(cut, "r-lager").GetAttribute("aria-label"));
        Assert.Contains("Floor plan", cut.Markup);
    }

    // =====================================================================
    //  Dateikörper (G7f-2; Datenaustauschkonzept 15.4, Probe 26 erweitert und Probe 30)
    // =====================================================================

    /// <summary>
    /// Die Körperdaten mit einem Dateikörper für Wohnen: zwei Dreiecke (ein offenes Netz), vier Randkanten, die
    /// Vermerke Bogen und Offen. Küche bleibt Prisma aus dem Umriss, Lager und Flur schematisch.
    /// </summary>
    private static GebaeudeAnsichtDaten DateiDaten(int? grenze = null)
    {
        GebaeudeAnsichtDaten d = KoerperDaten();
        var datei = new GebaeudeAnsichtDateikoerper(
            new float[] { 0, 0, 0, 6, 0, 0, 6, 8, 0, 0, 8, 0 }, new[] { 0, 1, 2, 0, 2, 3 }, new[] { 0, 1, 1, 2, 2, 3, 0, 3 },
            2, "FacetedBrep", new[] { "Bogen", "Offen" });
        return d with
        {
            Koerperraeume = new[] { d.Koerperraeume[0] with { Dateikoerper = datei, Herkunft = Koerperherkunft.Datei }, d.Koerperraeume[1] },
            Bezugspunkt = new[] { 1_000_000.0, 2_000_000.0, 0.0 },
            Dreiecksgrenze = grenze ?? GebaeudeAnsichtDaten.DREIECKSGRENZE,
        };
    }

    private static IElement Modusknopf(IRenderedComponent<GebaeudeAnsicht> cut, string schluessel)
        => cut.Find(".epos-gebansicht-modus button[data-modus='" + schluessel + "']");

    [Fact]
    public void GA6_Probe30_Mit_Dateikoerper_Umschalter_Vorgabe_Dateikoerper_Wechsel_und_zurueck()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var erzeugen = modul.Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);
        var aktualisieren = modul.SetupVoid("aktualisieren", _ => true);
        aktualisieren.SetVoidResult();

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(DateiDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));

        // Vorgabe Dateikörper: Szene, Rückweg und das Bytefeld (zwölf Floats, zehn Indizes, acht Kantenindizes).
        Assert.Equal("dateikoerper", cut.Instance.KoerperModus);
        Assert.Equal("dateikoerper", cut.Find(".epos-gebansicht-koerperansicht").GetAttribute("data-modus"));
        Assert.Equal("true", Modusknopf(cut, "dateikoerper").GetAttribute("aria-pressed"));
        Assert.Equal("false", Modusknopf(cut, "exportmodell").GetAttribute("aria-pressed"));
        Assert.Equal("Dateikörper", Modusknopf(cut, "dateikoerper").TextContent);
        Assert.Equal("Exportmodell", Modusknopf(cut, "exportmodell").TextContent);
        IReadOnlyList<object?> argumente = erzeugen.Invocations.Single().Arguments;
        Assert.Equal(4, argumente.Count);
        Assert.Equal(4 * (12 + 6 + 8), Assert.IsType<byte[]>(argumente[3]).Length);

        // Die Kennzeichenzeile zählt: Wohnen aus Datei, Küche aus Umriss, Lager und Flur schematisch.
        IElement zeile = cut.Find(".epos-gebansicht-kennzeichen");
        Assert.Equal(("1", "1", "2"), (zeile.GetAttribute("data-datei"), zeile.GetAttribute("data-umriss"), zeile.GetAttribute("data-schematisch")));
        Assert.Equal("1 Räume aus Datei, 1 aus Umriss, 2 schematisch", zeile.TextContent);
        IElement vereinfacht = cut.Find(".epos-gebansicht-vereinfacht");
        Assert.Equal("Bogen,Offen", vereinfacht.GetAttribute("data-vermerke"));
        Assert.Equal("vereinfacht: Bogen als Sehnenzug, offene Schale", vereinfacht.TextContent);
        IElement wohnen = cut.Find(".epos-gebansicht-raumherkunft li[data-raum='r-wohnen']");
        Assert.Equal("datei", wohnen.GetAttribute("data-herkunft"));
        Assert.StartsWith("Wohnen — Körper aus Datei", wohnen.GetAttribute("title"));
        Assert.Equal("umriss", cut.Find(".epos-gebansicht-raumherkunft li[data-raum='r-kueche']").GetAttribute("data-herkunft"));
        Assert.Equal("schematisch", cut.Find(".epos-gebansicht-raumherkunft li[data-raum='r-flur']").GetAttribute("data-herkunft"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-zugross"));

        // Auf Exportmodell: dieselbe Szene am Modul, aktualisiert ohne Bytefeld; keine Kennzeichenzeile.
        Modusknopf(cut, "exportmodell").Click();
        cut.WaitForAssertion(() => Assert.Single(aktualisieren.Invocations));
        Assert.Equal("exportmodell", cut.Instance.KoerperModus);
        Assert.Single(aktualisieren.Invocations.First().Arguments);
        Assert.Equal("true", Modusknopf(cut, "exportmodell").GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-kennzeichen"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-raumherkunft"));

        // Und zurück: wieder mit Bytefeld; das Modul ist nur einmal geladen, die Szene nur einmal erzeugt.
        Modusknopf(cut, "dateikoerper").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, aktualisieren.Invocations.Count));
        Assert.IsType<byte[]>(aktualisieren.Invocations.Last().Arguments[1]);
        Assert.Single(erzeugen.Invocations);
        Assert.Single(JSInterop.Invocations.Where(a => a.Identifier == "import"));
    }

    [Fact]
    public void GA6_Ohne_Dateikoerper_kein_Umschalter_und_das_Exportmodell()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var erzeugen = JSInterop.SetupModule(MODUL).Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(KoerperDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));
        Assert.Equal("exportmodell", cut.Instance.KoerperModus);
        Assert.Empty(cut.FindAll(".epos-gebansicht-modus"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-kennzeichen"));
        Assert.Equal(3, erzeugen.Invocations.Single().Arguments.Count);
    }

    [Fact]
    public void GA6_Ueber_der_Grenze_der_Hinweis_alle_Raeume_als_Prisma_und_kein_Bytefeld()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var erzeugen = JSInterop.SetupModule(MODUL).Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(DateiDaten(grenze: 1));
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));

        Assert.Equal("dateikoerper", cut.Instance.KoerperModus);
        IElement hinweis = cut.Find(".epos-gebansicht-zugross");
        Assert.Equal(("2", "1"), (hinweis.GetAttribute("data-dreiecke"), hinweis.GetAttribute("data-grenze")));
        Assert.Equal("Dateikörper zu groß (2 Dreiecke, Grenze 1) — alle Räume als Prisma aus dem Umriss.", hinweis.TextContent.Trim());
        Assert.Equal("0", cut.Find(".epos-gebansicht-kennzeichen").GetAttribute("data-datei"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-vereinfacht"));
        // Kein Dateikörper-Aufruf: drei Argumente, keine Netze in der Szene.
        Assert.Equal(3, erzeugen.Invocations.Single().Arguments.Count);
        Assert.DoesNotContain("\"dateikoerper\":[{", System.Text.Json.JsonSerializer.Serialize(erzeugen.Invocations.Single().Arguments[1]));
    }

    [Fact]
    public void GA6_Die_Dateiszene_traegt_Kennungen_und_Zahlen_keinen_Anzeigetext()
    {
        GebaeudeAnsichtDaten d = DateiDaten();
        string json = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.SzeneDatei(d, ZONE_EG, d.Koerperfeld()));
        Assert.Contains("\"modus\":\"dateikoerper\"", json);
        Assert.Contains("\"bezugspunkt\":[1000000,2000000,0]", json);
        Assert.Contains("\"raum\":\"r-wohnen\",\"zone\":\"" + ZONE_EG + "\",\"stelle\":1,\"punkteAb\":0,\"punktZahl\":4,\"dreieckeAb\":48,\"dreieckZahl\":2,\"kantenAb\":72,\"kantenZahl\":4", json);
        Assert.Contains("\"raum\":\"r-kueche\"", json);
        foreach (string text in new[] { "Wohnen", "Küche", "Erdgeschoss", "Kellergeschoss", "m²", "Bogen", "Dateikörper" })
            Assert.DoesNotContain(text, json);
        Assert.Equal(json, System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.SzeneDatei(DateiDaten(), ZONE_EG, DateiDaten().Koerperfeld())));
    }

    [Fact]
    public void GA6_Die_Texte_der_Dateikoerper_stehen_englisch_unter_en_US()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        using var englisch = new Kulturvorrichtung("en-US");
        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(DateiDaten());
        cut.FindAll("button[role=tab]")[1].Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebansicht-kennzeichen")));
        Assert.Equal("File solids", Modusknopf(cut, "dateikoerper").TextContent);
        Assert.Equal("Export model", Modusknopf(cut, "exportmodell").TextContent);
        Assert.Equal("1 rooms from file, 1 from outline, 2 schematic", cut.Find(".epos-gebansicht-kennzeichen").TextContent);
        Assert.Equal("simplified: arc as chords, open shell", cut.Find(".epos-gebansicht-vereinfacht").TextContent);
        Assert.StartsWith("Wohnen — solid from file", cut.Find(".epos-gebansicht-raumherkunft li[data-raum='r-wohnen']").GetAttribute("title"));
    }

    // =====================================================================
    //  Farbmodus „Randbedingung" (HC-2 Teil A; HottCAD-Verbund 4.3)
    // =====================================================================

    /// <summary>
    /// Die Dateikörperdaten mit Gruppen: Wohnen trägt je Dreieck R1 und R3, an den Kanten R1, R2, R0, R1, ein Fenster als
    /// Marke auf der Südkante (ein Viertel bis zur Hälfte) und einen Boden auf dem Erdreich (R3); die Küche einen neutralen
    /// Boden (R0); dazu die Gruppenliste und ein Fensterkörper.
    /// </summary>
    private static GebaeudeAnsichtDaten RandDaten(int? grenze = null)
    {
        GebaeudeAnsichtDaten d = DateiDaten(grenze);
        GebaeudeAnsichtKoerperraum wohnen = d.Koerperraeume[0];
        var fenster = new GebaeudeAnsichtDateikoerper(
            new float[] { 1.5f, 0, 1, 3, 0, 1, 3, 0, 2 }, new[] { 0, 1, 2 }, new[] { 0, 1, 1, 2 }, 1, "FacetedBrep", Array.Empty<string>());
        return d with
        {
            Koerperraeume = new[]
            {
                wohnen with
                {
                    Dateikoerper = wohnen.Dateikoerper! with { Gruppen = new byte[] { 1, 3 } },
                    Kantengruppen = new[] { (IReadOnlyList<Randgruppe?>)new Randgruppe?[] { Randgruppe.R1, Randgruppe.R2, Randgruppe.R0, Randgruppe.R1 } },
                    Kantenmarken = new[] { new GebaeudeAnsichtKantenmarke(0, 0, 0.25, 0.5, "f-1") },
                    Bodengruppe = Randgruppe.R3,
                },
                d.Koerperraeume[1] with { Bodengruppe = Randgruppe.R0 },
            },
            Flaechengruppen = new[]
            {
                new GebaeudeAnsichtFlaechengruppe(Randgruppe.R0, 12, 2), new GebaeudeAnsichtFlaechengruppe(Randgruppe.R1, 78, 12),
                new GebaeudeAnsichtFlaechengruppe(Randgruppe.R2, 12, 2), new GebaeudeAnsichtFlaechengruppe(Randgruppe.R3, 36, 4),
                new GebaeudeAnsichtFlaechengruppe(Randgruppe.R4, 0, 0), new GebaeudeAnsichtFlaechengruppe(Randgruppe.R5, 36, 4),
                new GebaeudeAnsichtFlaechengruppe(Randgruppe.R6, 0, 0), new GebaeudeAnsichtFlaechengruppe(Randgruppe.R7, 1.8, 0),
            },
            Bauteilkoerper = new[] { new GebaeudeAnsichtBauteilkoerper("f-1", Randgruppe.R7, fenster) },
        };
    }

    private static IElement Farbmodusknopf(IRenderedComponent<GebaeudeAnsicht> cut, string schluessel)
        => cut.Find(".epos-gebansicht-farbmodus button[data-farbmodus='" + schluessel + "']");

    private static IElement Randschalter(IRenderedComponent<GebaeudeAnsicht> cut, string gruppe)
        => cut.Find(".epos-gebansicht-randlegende button[data-gruppe='" + gruppe + "']");

    private IRenderedComponent<GebaeudeAnsicht> ImRandmodus(GebaeudeAnsichtDaten daten)
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(daten);
        Erdgeschoss(cut);
        Farbmodusknopf(cut, "randbedingung").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        return cut;
    }

    [Fact]
    public void HC2_Der_Umschalter_steht_in_der_Leiste_vorgewaehlt_Zonen_und_wechselt_auf_Randbedingung()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(RandDaten());
        Erdgeschoss(cut);
        Assert.Equal("zonen", cut.Instance.Farbmodus);
        Assert.Equal("Zonen", Farbmodusknopf(cut, "zonen").TextContent);
        Assert.Equal("Randbedingung", Farbmodusknopf(cut, "randbedingung").TextContent);
        Assert.Equal("Farbmodus", cut.Find(".epos-gebansicht-farbmodus").GetAttribute("aria-label"));
        Assert.Equal("true", Farbmodusknopf(cut, "zonen").GetAttribute("aria-pressed"));
        Assert.False(Farbmodusknopf(cut, "randbedingung").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-farbmodus-hinweis"));
        Assert.Empty(cut.FindAll("line.epos-gebansicht-kante"));
        Assert.Empty(cut.FindAll(".epos-gebansicht-randlegende"));

        Farbmodusknopf(cut, "randbedingung").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        Assert.Equal("true", Farbmodusknopf(cut, "randbedingung").GetAttribute("aria-pressed"));
        Assert.Equal("randbedingung", cut.Find("svg.epos-gebansicht-bild").GetAttribute("data-farbmodus"));
        Assert.NotEmpty(cut.FindAll("line.epos-gebansicht-kante"));
        // Die Zonenlegende weicht der Gruppenlegende; zurück auf Zonen ist das Bild wie vorher.
        Assert.Empty(cut.FindAll(".epos-gebansicht-legendeneintrag[data-zone]"));
        Farbmodusknopf(cut, "zonen").Click();
        cut.WaitForAssertion(() => Assert.Equal("zonen", cut.Instance.Farbmodus));
        Assert.Empty(cut.FindAll("line.epos-gebansicht-kante"));
        Assert.NotEmpty(cut.FindAll(".epos-gebansicht-legendeneintrag[data-zone]"));
    }

    [Fact]
    public void HC2_Die_Legende_nennt_Gruppen_mit_Farbe_Flaeche_und_Schalter_Decken_nur_in_der_Legende()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = ImRandmodus(RandDaten());
        IReadOnlyList<IElement> eintraege = cut.FindAll(".epos-gebansicht-randlegende li");
        // R4 und R6 ohne Fläche und Dreiecke stehen nicht da.
        Assert.Equal(new[] { "R0", "R1", "R2", "R3", "R5", "R7" }, eintraege.Select(e => e.GetAttribute("data-gruppe")));
        Assert.Equal("Flächen nach Randbedingung", cut.Find(".epos-gebansicht-randlegende").GetAttribute("aria-label"));
        IElement r1 = Randschalter(cut, "R1");
        Assert.Equal("R1 Wand gegen außen", r1.QuerySelector(".epos-gebansicht-zonenname")!.TextContent);
        Assert.Equal("78,00 m²", r1.QuerySelector(".epos-gebansicht-randflaeche")!.TextContent);
        Assert.Equal("background:" + GebaeudeAnsichtRandgruppen.FARBEN[1], r1.QuerySelector(".epos-gebansicht-farbfeld")!.GetAttribute("style"));
        Assert.Equal("R1 Wand gegen außen anzeigen", r1.GetAttribute("aria-label"));
        Assert.Equal("true", r1.GetAttribute("aria-pressed"));
        Assert.Equal("nur Legende und Bilanz", Randschalter(cut, "R5").QuerySelector(".epos-gebansicht-marke")!.TextContent);
        Assert.Null(r1.QuerySelector(".epos-gebansicht-marke"));

        // Der Schalter blendet R1 aus: Kanten weg, Schalter aus; noch einmal und sie sind wieder da.
        Assert.Equal(2, cut.FindAll("line.epos-gebansicht-kante[data-gruppe='R1']").Count);
        r1.Click();
        cut.WaitForAssertion(() => Assert.False(cut.Instance.GruppeSichtbar(Randgruppe.R1)));
        Assert.Equal("false", Randschalter(cut, "R1").GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll("line.epos-gebansicht-kante[data-gruppe='R1']"));
        Assert.Single(cut.FindAll("line.epos-gebansicht-kante[data-gruppe='R2']"));
        Randschalter(cut, "R1").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("line.epos-gebansicht-kante[data-gruppe='R1']").Count));
    }

    [Fact]
    public void HC2_Kanten_in_der_Gruppenfarbe_Fenster_als_Marke_Boden_schraffiert_oder_weiss()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = ImRandmodus(RandDaten());
        IElement wohnen = Raum(cut, "r-wohnen");
        IReadOnlyList<IElement> kanten = wohnen.QuerySelectorAll("line.epos-gebansicht-kante").ToList();
        Assert.Equal(new[] { "R1", "R2", "R0", "R1" }, kanten.Select(k => k.GetAttribute("data-gruppe")));
        Assert.Equal(new[] { "0:0", "0:1", "0:2", "0:3" }, kanten.Select(k => k.GetAttribute("data-kante")));
        Assert.Equal(new[] { GebaeudeAnsichtRandgruppen.FARBEN[1], GebaeudeAnsichtRandgruppen.FARBEN[2], GebaeudeAnsichtRandgruppen.FARBEN[0],
                             GebaeudeAnsichtRandgruppen.FARBEN[1] }, kanten.Select(k => k.GetAttribute("stroke")));
        // Die erste Kante (0|0) → (6|0) in Metern, y gespiegelt; die letzte schließt (0|8) → (0|0).
        Assert.Equal(("0.000", "0.000", "6.000", "0.000"),
                     (kanten[0].GetAttribute("x1"), kanten[0].GetAttribute("y1"), kanten[0].GetAttribute("x2"), kanten[0].GetAttribute("y2")));
        Assert.Equal(("0.000", "-8.000", "0.000", "0.000"),
                     (kanten[3].GetAttribute("x1"), kanten[3].GetAttribute("y1"), kanten[3].GetAttribute("x2"), kanten[3].GetAttribute("y2")));

        // Das Fenster: ein Viertel bis zur Hälfte der Südkante, in R7.
        IElement fenster = wohnen.QuerySelector("line.epos-gebansicht-oeffnung")!;
        Assert.Equal(("R7", "f-1", GebaeudeAnsichtRandgruppen.FARBEN[7]), (fenster.GetAttribute("data-gruppe"), fenster.GetAttribute("data-bauteil"), fenster.GetAttribute("stroke")));
        Assert.Equal(("1.500", "3.000"), (fenster.GetAttribute("x1"), fenster.GetAttribute("x2")));

        // Der Boden: Wohnen schraffiert nach R3, die Küche weiß (R0); die Schraffuren stehen als Muster im Bild.
        Assert.Equal("R3", wohnen.GetAttribute("data-boden"));
        Assert.Equal("fill:url(#epos-gebansicht-schraffur-3)", wohnen.QuerySelector("polygon")!.GetAttribute("style"));
        Assert.Equal("fill:#ffffff", Raum(cut, "r-kueche").QuerySelector("polygon")!.GetAttribute("style"));
        Assert.Equal(new[] { "epos-gebansicht-schraffur-3", "epos-gebansicht-schraffur-4" },
                     cut.FindAll("pattern.epos-gebansicht-schraffur").Select(p => p.Id));
        Assert.Equal(GebaeudeAnsichtRandgruppen.FARBEN[3], cut.Find("#epos-gebansicht-schraffur-3 line").GetAttribute("stroke"));

        // R7 und R3 ausgeblendet: keine Marke, der Boden weiß.
        Randschalter(cut, "R7").Click();
        Randschalter(cut, "R3").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("line.epos-gebansicht-oeffnung")));
        Assert.Equal("fill:#ffffff", Raum(cut, "r-wohnen").QuerySelector("polygon")!.GetAttribute("style"));

        // Dieselben Daten geben zeichengleich dasselbe Markup.
        Assert.Equal(OhneBindungen(ImRandmodus(RandDaten()).Markup), OhneBindungen(ImRandmodus(RandDaten()).Markup));
    }

    [Fact]
    public void HC2_Ohne_Klassifikation_ist_Randbedingung_benannt_gesperrt()
    {
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(Daten());
        Erdgeschoss(cut);
        IElement knopf = Farbmodusknopf(cut, "randbedingung");
        string grund = "Randbedingung nicht wählbar: Für dieses Gebäude liegen keine klassifizierten Körper aus der Datei vor.";
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Equal(grund, knopf.GetAttribute("title"));
        Assert.Equal(grund, cut.Find(".epos-gebansicht-farbmodus-hinweis").TextContent);
        knopf.Click();
        Assert.Equal("zonen", cut.Instance.Farbmodus);
        Assert.Empty(cut.FindAll("line.epos-gebansicht-kante"));

        // Über der Dreiecksgrenze: Prismen ohne Gruppen, der Grund ist die Grenze.
        IRenderedComponent<GebaeudeAnsicht> gross = Zeige(RandDaten(grenze: 1));
        Assert.True(Farbmodusknopf(gross, "randbedingung").HasAttribute("disabled"));
        Assert.Equal("Randbedingung nicht wählbar: Die Körper überschreiten die Dreiecksgrenze, die Räume stehen als Prismen ohne Gruppen da.",
                     gross.Find(".epos-gebansicht-farbmodus-hinweis").TextContent);
        IRenderedComponent<GebaeudeAnsicht> verworfen = Zeige(Daten() with { RandgruppenZuGross = true });
        Assert.StartsWith("Randbedingung nicht wählbar: Die Körper überschreiten", verworfen.Find(".epos-gebansicht-farbmodus-hinweis").TextContent);
    }

    [Fact]
    public void HC2_Die_Szene_traegt_Farbmodus_Farbtafel_Schalter_Gruppenbytes_und_Bauteilkoerper()
    {
        GebaeudeAnsichtDaten d = RandDaten();
        GebaeudeAnsichtKoerperfeld feld = d.Koerperfeld();
        // Teil 1 wie bisher (Wohnen: 12 + 6 + 8 Werte), Teil 2 das Fenster (9 + 3 + 4), Teil 3 zwei Gruppenbytes, aufgefüllt.
        Assert.Equal(4 * (12 + 6 + 8) + 4 * (9 + 3 + 4) + 4, feld.Bytes.Length);
        GebaeudeAnsichtKoerperfeldEintrag fenster = Assert.Single(feld.Bauteile);
        Assert.Equal(("f-1", 7, 4 * (12 + 6 + 8)), (fenster.Bauteil, fenster.Gruppe, fenster.PunkteAb));
        int ab = feld.Verzeichnis.Single().GruppenAb;
        Assert.Equal(4 * (12 + 6 + 8) + 4 * (9 + 3 + 4), ab);
        Assert.Equal(new byte[] { 1, 3 }, feld.Bytes.Skip(ab).Take(2));

        bool[] schalter = { true, false, true, true, true, true, true, true };
        string json = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.SzeneDatei(d, ZONE_EG, feld, "randbedingung", schalter));
        Assert.Contains("\"kantenAb\":72,\"kantenZahl\":4,\"gruppenAb\":" + ab, json);
        Assert.Contains("\"farbmodus\":\"randbedingung\"", json);
        Assert.Contains("\"randfarben\":[\"" + string.Join("\",\"", GebaeudeAnsichtRandgruppen.FARBEN) + "\"]", json);
        Assert.Contains("\"randsichtbar\":[true,false,true,true,true,true,true,true]", json);
        Assert.Contains("\"bauteilesichtbar\":true", json);
        Assert.Contains("\"bauteilesichtbar\":false", System.Text.Json.JsonSerializer.Serialize(
            GebaeudeAnsicht.SzeneDatei(d, ZONE_EG, feld, "randbedingung", schalter, bauteilesichtbar: false)));
        Assert.Contains("\"bauteile\":[{\"bauteil\":\"f-1\",\"gruppe\":7,\"punkteAb\":104,\"punktZahl\":3,\"dreieckeAb\":140,\"dreieckZahl\":1,\"kantenAb\":152,\"kantenZahl\":2,\"stufe\":-1}]", json);
        foreach (string text in new[] { "Wohnen", "Wand", "m²", "Randbedingung" })
            Assert.DoesNotContain(text, json);
        // Das Exportmodell trägt Farbmodus, Farbtafel und Schalter ebenso; ohne Angabe der Modus Zonen, alles sichtbar.
        string export = System.Text.Json.JsonSerializer.Serialize(GebaeudeAnsicht.Szene(d, ZONE_EG));
        Assert.Contains("\"farbmodus\":\"zonen\"", export);
        Assert.Contains("\"randsichtbar\":[true,true,true,true,true,true,true,true]", export);
        Assert.Contains("\"bauteilesichtbar\":true", export);
    }

    [Fact]
    public void HC2_Der_Bauteilschalter_steht_nur_unter_den_Koerpern_und_geht_als_bauteilesichtbar_an_das_Modul()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var erzeugen = modul.Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);
        var aktualisieren = modul.SetupVoid("aktualisieren", _ => true);
        aktualisieren.SetVoidResult();

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(RandDaten());
        Erdgeschoss(cut);
        Farbmodusknopf(cut, "randbedingung").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        // Im Grundriss gibt es keine Bauteilkörper, also keinen Schalter.
        Assert.Empty(cut.FindAll(".epos-gebansicht-bauteilschalter"));

        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));
        Assert.Contains("\"bauteilesichtbar\":true", System.Text.Json.JsonSerializer.Serialize(erzeugen.Invocations.Single().Arguments[1]));
        IElement schalter = cut.Find(".epos-gebansicht-randlegende .epos-gebansicht-bauteilschalter");
        Assert.Equal("true", schalter.GetAttribute("aria-pressed"));
        Assert.Equal("Bauteilkörper anzeigen", schalter.GetAttribute("aria-label"));
        Assert.Equal("Bauteilkörper", schalter.QuerySelector(".epos-gebansicht-zonenname")!.TextContent);
        Assert.Equal("1", schalter.QuerySelector(".epos-gebansicht-randflaeche")!.TextContent);

        schalter.Click();
        cut.WaitForAssertion(() => Assert.Single(aktualisieren.Invocations));
        Assert.False(cut.Instance.BauteileSichtbar);
        Assert.Equal("false", cut.Find(".epos-gebansicht-bauteilschalter").GetAttribute("aria-pressed"));
        string szene = System.Text.Json.JsonSerializer.Serialize(aktualisieren.Invocations.Single().Arguments[0]);
        Assert.Contains("\"bauteilesichtbar\":false", szene);
        Assert.Contains("\"farbmodus\":\"randbedingung\"", szene);
        Assert.Single(erzeugen.Invocations);

        // Zurück auf Zonen: die Zonenlegende, kein Schalter; die Szene meldet den Modus Zonen.
        Farbmodusknopf(cut, "zonen").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, aktualisieren.Invocations.Count));
        Assert.Empty(cut.FindAll(".epos-gebansicht-bauteilschalter"));
        Assert.Contains("\"farbmodus\":\"zonen\"", System.Text.Json.JsonSerializer.Serialize(aktualisieren.Invocations.Last().Arguments[0]));
    }

    [Fact]
    public async Task HC2_Der_Klick_meldet_Gruppe_und_Bauteil_in_der_Infozeile_im_Modus_Zonen_wie_bisher()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(MODUL).Setup<bool>("erzeugen", _ => true).SetResult(true);
        var zonen = new List<string>();
        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(RandDaten(), zonen);
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.KoerperGezeigt));

        // Modus Zonen: der Rückruf trägt Gruppe und Bauteil als null - die Zone wird gemeldet, keine Infozeile.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE_EG, "r-wohnen", null, null));
        Assert.Equal(new[] { ZONE_EG }, zonen);
        Assert.Null(cut.Instance.Treffer);
        Assert.Empty(cut.FindAll(".epos-gebansicht-treffer"));

        Farbmodusknopf(cut, "randbedingung").Click();
        cut.WaitForAssertion(() => Assert.Equal("randbedingung", cut.Instance.Farbmodus));
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE_EG, "r-wohnen", 1, null));
        cut.WaitForAssertion(() => Assert.Equal("Gewählt: Raum Wohnen — R1 Wand gegen außen", cut.Find(".epos-gebansicht-treffer").TextContent));
        IElement zeile = cut.Find(".epos-gebansicht-treffer");
        Assert.Equal(("r-wohnen", "1"), (zeile.GetAttribute("data-raum"), zeile.GetAttribute("data-gruppe")));
        Assert.Equal(new[] { ZONE_EG, ZONE_EG }, zonen);

        // Ein Bauteilkörper hat keinen Raum und keine Zone: nur die Infozeile.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(null, null, 7, "f-1"));
        cut.WaitForAssertion(() => Assert.Equal("Gewählt: Bauteil f-1 — R7 Fenster und Türen", cut.Find(".epos-gebansicht-treffer").TextContent));
        Assert.Equal("f-1", cut.Find(".epos-gebansicht-treffer").GetAttribute("data-bauteil"));
        Assert.Equal(2, zonen.Count);

        // Ein entartetes Dreieck (255) ist ohne Gruppe.
        await cut.InvokeAsync(() => cut.Instance.BeiKoerperklick(ZONE_EG, "r-wohnen", 255, null));
        cut.WaitForAssertion(() => Assert.Equal("Gewählt: Raum Wohnen — ohne Gruppe", cut.Find(".epos-gebansicht-treffer").TextContent));

        // Zurück auf Zonen: die Infozeile fällt weg.
        Farbmodusknopf(cut, "zonen").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-gebansicht-treffer")));
        Assert.Null(cut.Instance.Treffer);
    }

    [Fact]
    public void HC2_Farbmodus_und_Schalter_gehen_ueber_aktualisieren_an_die_Koerper()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var erzeugen = modul.Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);
        var aktualisieren = modul.SetupVoid("aktualisieren", _ => true);
        aktualisieren.SetVoidResult();

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(RandDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));
        Assert.Contains("\"farbmodus\":\"zonen\"", System.Text.Json.JsonSerializer.Serialize(erzeugen.Invocations.Single().Arguments[1]));

        Farbmodusknopf(cut, "randbedingung").Click();
        cut.WaitForAssertion(() => Assert.Single(aktualisieren.Invocations));
        string szene = System.Text.Json.JsonSerializer.Serialize(aktualisieren.Invocations.Single().Arguments[0]);
        Assert.Contains("\"farbmodus\":\"randbedingung\"", szene);
        Assert.IsType<byte[]>(aktualisieren.Invocations.Single().Arguments[1]);
        // Die Legende steht auch unter den Körpern; ihr Schalter geht an das Modul.
        Randschalter(cut, "R0").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, aktualisieren.Invocations.Count));
        Assert.Contains("\"randsichtbar\":[false,true,true,true,true,true,true,true]",
                        System.Text.Json.JsonSerializer.Serialize(aktualisieren.Invocations.Last().Arguments[0]));
        Assert.Single(erzeugen.Invocations);
    }

    [Fact]
    public void HC2_Die_Texte_des_Farbmodus_stehen_englisch_unter_en_US()
    {
        using var englisch = new Kulturvorrichtung("en-US");
        IRenderedComponent<GebaeudeAnsicht> cut = Zeige(RandDaten());
        Assert.Equal("Zones", Farbmodusknopf(cut, "zonen").TextContent);
        Assert.Equal("Boundary condition", Farbmodusknopf(cut, "randbedingung").TextContent);
        Farbmodusknopf(cut, "randbedingung").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebansicht-randlegende")));
        Assert.Equal("R1 wall to outside", Randschalter(cut, "R1").QuerySelector(".epos-gebansicht-zonenname")!.TextContent);
        Assert.Equal("78.00 m²", Randschalter(cut, "R1").QuerySelector(".epos-gebansicht-randflaeche")!.TextContent);
        Assert.Equal("legend and balance only", Randschalter(cut, "R5").QuerySelector(".epos-gebansicht-marke")!.TextContent);
        var texte = new GebaeudeAnsichtTexte();
        Assert.Equal(("Component bodies", "no group"), (texte.RandBauteilkoerper, texte.RandKeine));
        Assert.Equal("Selected: component {0} — {1}", texte.RandTrefferBauteil);
        Assert.Equal("Selected: room {0} — {1}", texte.RandTrefferRaum);
        Assert.Equal("Boundary condition not available: this building has no classified solids from the file.",
                     Zeige(Daten()).Find(".epos-gebansicht-farbmodus-hinweis").TextContent);
    }

    // =====================================================================
    //  HC-5: Prismen aus dem Grundriss des Dateikörpers im Exportmodell
    // =====================================================================

    private static GebaeudeAnsichtDaten GrundrissDaten()
    {
        GebaeudeAnsichtDaten basis = KoerperDaten();
        return basis with
        {
            Koerperraeume = new[]
            {
                basis.Koerperraeume[0] with
                {
                    Herkunft = Koerperherkunft.Grundriss,
                    Prismen = new[] { new GebaeudeAnsichtPrismenlage(1.5, 4.2) },
                    Grundrissvermerke = new[] { "Stufen", "Dachschraege" },
                },
                basis.Koerperraeume[1],
            },
            Zonen = basis.Zonen.Select(z => z.Schluessel == ZONE_EG ? z with { AusDateikoerper = true } : z).ToList(),
        };
    }

    [Fact]
    public void HC5_Das_Prisma_aus_dem_Grundriss_steht_auf_seinem_Boden_mit_seiner_Hoehe()
    {
        GebaeudeAnsichtKoerper k = GrundrissDaten().Koerper().Single(x => x.Raum.Kennung == "r-wohnen");
        Assert.Equal(1.5, k.UnterkanteM);
        Assert.Equal(4.2, k.HoeheM);
        Assert.False(k.HoeheVorgabe);
        Assert.Equal(Koerperherkunft.Grundriss, k.Herkunft);
        Assert.Equal(Koerperherkunft.Umriss, GrundrissDaten().Koerper().Single(x => x.Raum.Kennung == "r-kueche").Herkunft);
        Assert.Equal("aus Dateikörper (Grundriss)", new GebaeudeAnsichtTexte().Herkunft(Koerperherkunft.Grundriss));
    }

    [Fact]
    public void HC5_Exportmodell_nennt_aus_Dateikoerper_in_Kennzeichen_Legende_und_Raumliste()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var erzeugen = JSInterop.SetupModule(MODUL).Setup<bool>("erzeugen", _ => true);
        erzeugen.SetResult(true);

        IRenderedComponent<GebaeudeAnsicht> cut = ZeigeKoerper(GrundrissDaten());
        Reiterknopf(cut, "Körper").Click();
        cut.WaitForAssertion(() => Assert.Single(erzeugen.Invocations));
        Assert.Equal("exportmodell", cut.Instance.KoerperModus);

        IElement zeile = cut.Find(".epos-gebansicht-kennzeichen");
        Assert.Equal("1", zeile.GetAttribute("data-grundriss"));
        Assert.Equal("1", zeile.GetAttribute("data-umriss"));
        Assert.Contains("1 Räume aus Dateikörper", zeile.TextContent);
        Assert.Contains(cut.FindAll(".epos-gebansicht-legende .epos-gebansicht-herkunft"), e => e.TextContent == "aus Dateikörper (Grundriss)");

        IElement wohnen = cut.Find(".epos-gebansicht-raumherkunft li[data-raum='r-wohnen']");
        Assert.Equal("grundriss", wohnen.GetAttribute("data-herkunft"));
        Assert.Contains("aus Dateikörper (Grundriss)", wohnen.TextContent);
        Assert.Contains("Stufen im Boden", wohnen.TextContent);
        Assert.Contains("Dachschräge", wohnen.TextContent);
        Assert.Equal("umriss", cut.Find(".epos-gebansicht-raumherkunft li[data-raum='r-kueche']").GetAttribute("data-herkunft"));
    }
}
