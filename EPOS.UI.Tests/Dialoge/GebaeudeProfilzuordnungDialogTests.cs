using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Import;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Überlagerung „Nutzungsprofile zuordnen…“</b> (ZZ-B; Entwurf Zonenzuordnung IFC 6) an synthetischen Daten: Bündel
/// offene zuerst mit Stand und Vorschlag, Zuordnung mehrerer Bündel per Klick auf ein Profil des Katalogs, Kandidaten-
/// Schnellwahl einer Rückfrage, „Alle Vorschläge übernehmen“ und „Zurücknehmen“, Filter „nur offene“ und die Suche im Katalog.
/// </summary>
public sealed class GebaeudeProfilzuordnungDialogTests : EposBunitContext
{
    private sealed class Probe
    {
        public List<GebaeudeProfilwahl> Zuordnungen { get; } = new();
        public List<IReadOnlyList<string>> Ruecknahmen { get; } = new();
        public int Alle { get; set; }
        public int AlleZurueck { get; set; }
    }

    private static GebaeudeProfilbuendelDaten Buendel(string schluessel, string name, string stand, string? profil, params string[] raeume)
        => new()
        {
            Schluessel = schluessel,
            Anzeigename = name,
            Raumart = "Raumart " + schluessel,
            Raeume = raeume,
            Flaeche = "12–61",
            Stand = stand,
            StandText = stand.ToLowerInvariant(),
            Profil = profil,
            ProfilText = profil is null ? "" : "Profil " + profil,
            Begruendung = "Raumart " + schluessel,
            Kandidaten = stand == GebaeudeProfilstand.OFFEN
                ? new[] { new GebaeudeZonenregelDaten("1", "Einzelbüro"), new GebaeudeZonenregelDaten("2", "Gruppenbüro") }
                : Array.Empty<GebaeudeZonenregelDaten>(),
        };

    private static GebaeudeProfilzuordnungDaten Daten(int vorschlag = 2, int hand = 0) => new()
    {
        Buendel = new[]
        {
            Buendel("Office|büroraum", "Büroraum", GebaeudeProfilstand.OFFEN, null, "R1", "R2", "R3"),
            Buendel("Storage|lager", "Lager", GebaeudeProfilstand.VORSCHLAG, "20", "R4", "R5"),
            Buendel("Corridor|flur", "Flur", GebaeudeProfilstand.EINDEUTIG, "19", "R6"),
        },
        Katalog = new[]
        {
            new GebaeudeProfilknotenDaten("K:/DIN", "DIN/TS 18599-10", new[]
            {
                new GebaeudeProfilknotenDaten("K:/DIN/BUERO", "Büro und Verwaltung", new[]
                {
                    new GebaeudeProfilknotenDaten("P:/DIN/BUERO/1", "1 Einzelbüro", Array.Empty<GebaeudeProfilknotenDaten>(), "1", "21 °C · 7–18 h"),
                    new GebaeudeProfilknotenDaten("P:/DIN/BUERO/2", "2 Gruppenbüro", Array.Empty<GebaeudeProfilknotenDaten>(), "2"),
                }),
                new GebaeudeProfilknotenDaten("K:/DIN/NEBEN", "Nebenflächen", new[]
                {
                    new GebaeudeProfilknotenDaten("P:/DIN/NEBEN/20", "20 Lager", Array.Empty<GebaeudeProfilknotenDaten>(), "20"),
                }),
            }),
        },
        Eindeutig = 1, Vorschlag = vorschlag, Rueckfrage = 3, Hand = hand, OffeneBuendel = 1,
        Zaehlertext = "1 eindeutig · 2 Vorschlag · 3 Rückfrage · 0 ohne · 0 von Hand",
        Ampel = GebaeudeProfilampel.ROT,
    };

    private IRenderedComponent<GebaeudeProfilzuordnung> Zeichnen(Probe p, GebaeudeProfilzuordnungDaten? daten = null)
        => Render<GebaeudeProfilzuordnung>(c => c
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Zuordnen, (GebaeudeProfilwahl w) => p.Zuordnungen.Add(w))
            .Add(x => x.Zuruecknehmen, (IReadOnlyList<string> r) => p.Ruecknahmen.Add(r))
            .Add(x => x.AlleUebernehmen, () => p.Alle++)
            .Add(x => x.AlleZuruecknehmen, () => p.AlleZurueck++));

    private static IReadOnlyList<string?> Zeilen(IRenderedComponent<GebaeudeProfilzuordnung> cut)
        => cut.FindAll("tr.epos-gebimport-pz-zeile").Select(z => z.GetAttribute("data-buendel")).ToList();

    private static void Waehlen(IRenderedComponent<GebaeudeProfilzuordnung> cut, string buendel)
        => cut.Find("tr[data-buendel='" + buendel + "'] input.epos-gebimport-pz-buendelwahl").WechselAbgewartet(true);

    private static void Aufklappen(IRenderedComponent<GebaeudeProfilzuordnung> cut, string knoten)
        => cut.Find("li[data-schluessel='" + knoten + "'] > .epos-baum-zeile > button.epos-baum-schalter").KlickAbgewartet();

    [Fact]
    public void Buendel_offene_zuerst_mit_Stand_Vorschlag_und_Zaehler()
    {
        var p = new Probe();
        IRenderedComponent<GebaeudeProfilzuordnung> cut = Zeichnen(p);
        Assert.Equal(new[] { "Office|büroraum", "Storage|lager", "Corridor|flur" }, Zeilen(cut));
        IElement offen = cut.Find("tr[data-buendel='Office|büroraum']");
        Assert.Equal(GebaeudeProfilstand.OFFEN, offen.GetAttribute("data-stand"));
        Assert.Contains("Einzelbüro / Gruppenbüro", offen.TextContent);
        Assert.Contains("3", offen.QuerySelectorAll("td.epos-gebimport-zahl")[0].TextContent);
        Assert.Contains("Profil 20", cut.Find("tr[data-buendel='Storage|lager']").TextContent);
        Assert.Contains("1 eindeutig", cut.Find(".epos-gebimport-pz-zaehler").TextContent);
        Assert.Equal(GebaeudeProfilampel.ROT, cut.Find(".epos-gebimport-pz").GetAttribute("data-ampel"));
        // Nur der Vorschlag trägt „übernehmen“.
        Assert.Single(cut.FindAll("button.epos-gebimport-pz-uebernehmen"));
        cut.Find("button.epos-gebimport-pz-uebernehmen").KlickAbgewartet();
        GebaeudeProfilwahl w = Assert.Single(p.Zuordnungen);
        Assert.Equal(new[] { "R4", "R5" }, w.Raeume);
        Assert.Equal("20", w.Profil);
    }

    [Fact]
    public void Mehrere_Buendel_per_Klick_auf_ein_Profil_zuordnen()
    {
        var p = new Probe();
        IRenderedComponent<GebaeudeProfilzuordnung> cut = Zeichnen(p);
        Assert.Contains("ankreuzen", cut.Find(".epos-gebimport-pz-hinweis").TextContent);
        Waehlen(cut, "Office|büroraum");
        Waehlen(cut, "Storage|lager");
        Assert.Contains("2 Bündel gewählt (5 Räume)", cut.Find(".epos-gebimport-pz-gewaehlt").TextContent);

        // Die Kategorie steht offen, die Gruppe wird aufgeklappt; der Klick auf das Profil ordnet zu.
        Aufklappen(cut, "K:/DIN/BUERO");
        cut.Find("li[data-schluessel='P:/DIN/BUERO/2'] .epos-baum-text").KlickAbgewartet();
        GebaeudeProfilwahl w = Assert.Single(p.Zuordnungen);
        Assert.Equal(new[] { "R1", "R2", "R3", "R4", "R5" }, w.Raeume);
        Assert.Equal("2", w.Profil);
        Assert.Empty(cut.Instance.Auswahl);   // die Wahl ist danach leer

        // Ohne gewählte Bündel markiert ein Klick nur.
        cut.Find("li[data-schluessel='P:/DIN/BUERO/1'] .epos-baum-text").KlickAbgewartet();
        Assert.Single(p.Zuordnungen);

        // „keine Nutzung“ und „Zuordnung aufheben“ wirken auf die Wahl.
        Waehlen(cut, "Corridor|flur");
        cut.Find("button.epos-gebimport-pz-keine").KlickAbgewartet();
        Assert.Equal("", p.Zuordnungen.Last().Profil);
        Assert.Equal(new[] { "R6" }, p.Zuordnungen.Last().Raeume);
        Waehlen(cut, "Corridor|flur");
        cut.Find("button.epos-gebimport-pz-aufheben").KlickAbgewartet();
        Assert.Equal(new[] { "R6" }, Assert.Single(p.Ruecknahmen));
    }

    [Fact]
    public void Kandidaten_einer_Rueckfrage_stehen_oben_als_Schnellwahl()
    {
        var p = new Probe();
        IRenderedComponent<GebaeudeProfilzuordnung> cut = Zeichnen(p);
        Assert.Empty(cut.FindAll("button.epos-gebimport-pz-kandidat"));
        Waehlen(cut, "Storage|lager");   // ein Vorschlag hat keine Schnellwahl
        Assert.Empty(cut.FindAll("button.epos-gebimport-pz-kandidat"));
        Waehlen(cut, "Office|büroraum");
        Assert.Equal(new[] { "1", "2" }, cut.FindAll("button.epos-gebimport-pz-kandidat").Select(b => b.GetAttribute("data-profil")));
        cut.Find("button.epos-gebimport-pz-kandidat[data-profil='1']").KlickAbgewartet();
        GebaeudeProfilwahl w = Assert.Single(p.Zuordnungen);
        Assert.Equal("1", w.Profil);
        Assert.Equal(new[] { "R1", "R2", "R3", "R4", "R5" }, w.Raeume);
    }

    [Fact]
    public void Alle_Vorschlaege_uebernehmen_und_zuruecknehmen()
    {
        var p = new Probe();
        IRenderedComponent<GebaeudeProfilzuordnung> cut = Zeichnen(p);
        Assert.True(cut.Find("button.epos-gebimport-pz-zurueck").HasAttribute("disabled"));   // nichts von Hand
        cut.Find("button.epos-gebimport-pz-alle").KlickAbgewartet();
        Assert.Equal(1, p.Alle);

        cut.Render(c => c.Add(x => x.Daten, Daten(vorschlag: 0, hand: 2)));
        Assert.True(cut.Find("button.epos-gebimport-pz-alle").HasAttribute("disabled"));
        cut.Find("button.epos-gebimport-pz-zurueck").KlickAbgewartet();
        Assert.Equal(1, p.AlleZurueck);
    }

    [Fact]
    public void Filter_nur_offene_und_Suche_im_Katalog()
    {
        var p = new Probe();
        IRenderedComponent<GebaeudeProfilzuordnung> cut = Zeichnen(p);
        cut.Find("input.epos-gebimport-pz-nuroffene").WechselAbgewartet(true);
        Assert.Equal(new[] { "Office|büroraum" }, Zeilen(cut));
        cut.Find("input.epos-gebimport-pz-nuroffene").WechselAbgewartet(false);
        Assert.Equal(3, Zeilen(cut).Count);

        // Die Suche über den Namen öffnet die Äste mit Treffern; über die Nummer findet sie das Profil.
        cut.Find("input.epos-gebimport-pz-suche").Input("lager");
        Assert.Equal(new[] { "P:/DIN/NEBEN/20" }, cut.FindAll("li[data-schluessel^='P:']").Select(l => l.GetAttribute("data-schluessel")));
        cut.Find("input.epos-gebimport-pz-suche").Input("1");
        Assert.Contains("P:/DIN/BUERO/1", cut.FindAll("li[data-schluessel^='P:']").Select(l => l.GetAttribute("data-schluessel")));
        cut.Find("input.epos-gebimport-pz-suche").Input("xyz");
        Assert.NotEmpty(cut.FindAll(".epos-baum-leer"));
    }
}
