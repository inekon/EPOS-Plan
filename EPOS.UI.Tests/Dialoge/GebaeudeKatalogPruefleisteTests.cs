using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// N1 (Nachzug MA-1b/MA-5): die Ampelleiste der Modellprüfung im Reiter „Zonen“ des Gebäudeeditors — fünf Gruppen mit Ampel und
/// Zahl samt Rechenstatus, ein Klick meldet die Gruppe an den Wirt, mit ungespeicherten Zonen gesperrt, ohne Weg keine Leiste;
/// eine neue Prüffassung liest Leiste und Zonen neu. Dazu die Startzone (Sprung „Zone…“) und das Kurzbild der Woche.
/// </summary>
public sealed class GebaeudeKatalogPruefleisteTests : EposBunitContext
{
    public GebaeudeKatalogPruefleisteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ZoneDaten Zone(int id, string name)
        => new() { Id = id, Bezeichner = name, Nutzflaeche = 50 };

    private int _neugelesen;

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(ModellpruefungProbeweg probe, List<Pruefgruppe> geklickt,
                                                               bool mitWeg = true, int? startZone = null,
                                                               GebaeudeKatalogDaten? daten = null,
                                                               Func<GebaeudeKatalogDaten?>? gebaeudeNeulesen = null,
                                                               List<GebaeudeKatalogDaten>? geschrieben = null)
    {
        var zonenweg = new GebaeudeZonenweg
        {
            Zonen = new[] { Zone(1, "Erdgeschoss"), Zone(2, "Obergeschoss") },
            Speichern = _ => ZonenSchreibergebnis.Gut,
            Neulesen = () => { _neugelesen++; return new ZonenNeulesung(new[] { Zone(1, "EG neu"), Zone(2, "Obergeschoss") }, Array.Empty<ZonenluftstromDaten>()); },
        };
        ModellpruefungWeg weg = probe.Weg();
        return Render<GebaeudeKatalogDialog>(p =>
        {
            p.Add(x => x.Daten, daten ?? new GebaeudeKatalogDaten { Name = "Haus A", Gebaeudeart = "Hotel", WohnflaecheGesamt = 100 })
             .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
             .Add(x => x.GebaeudeNeulesen, gebaeudeNeulesen)
             .Add(x => x.Geschlossen, _ => { })
             .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
             .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
             .Add(x => x.Zonen, zonenweg)
             .Add(x => x.StartReiter, GebaeudeKatalogDialog.REITER_ZONEN)
             .Add(x => x.StartZone, startZone);
            if (geschrieben is not null)
                p.Add(x => x.Speichern, (d, istNeu, _) =>
                {
                    if (!istNeu) geschrieben.Add(d.Kopie());
                    return new GebaeudeKatalogErgebnis(true, "");
                });
            if (mitWeg)
                p.Add(x => x.ModellpruefungLesen, () => weg.Pruefen())
                 .Add(x => x.PruefgruppeOeffnen, g => geklickt.Add(g));
        });
    }

    [Fact]
    public void Leiste_zeigt_Status_und_fuenf_Gruppen_und_meldet_den_Klick()
    {
        var geklickt = new List<Pruefgruppe>();
        var cut = Aufbauen(new ModellpruefungProbeweg(), geklickt);
        IElement leiste = cut.Find(".epos-pruefleiste");
        Assert.Equal("rechnet mit Vorgaben", leiste.QuerySelector(".epos-pruefleiste-status")!.TextContent.Trim());
        Assert.Contains("epos-ampel-gelb", leiste.QuerySelector(".epos-pruefleiste-status")!.ClassName);
        IReadOnlyList<IElement> gruppen = cut.FindAll(".epos-pruefleiste-gruppe");
        Assert.Equal(5, gruppen.Count);
        IElement zwei = cut.Find(".epos-pruefleiste-gruppe[data-gruppe='2']");
        Assert.Equal("2", zwei.QuerySelector(".epos-pruefleiste-zahl")!.TextContent);
        Assert.NotNull(zwei.QuerySelector(".epos-ampel.epos-ampel-gelb"));
        Assert.Contains("Gruppe 2", zwei.GetAttribute("title"));
        zwei.Click();
        Assert.Equal(new[] { Pruefgruppe.AufbautenStoffwerte }, geklickt);
    }

    [Fact]
    public void Ohne_Weg_keine_Leiste()
    {
        var cut = Aufbauen(new ModellpruefungProbeweg(), new List<Pruefgruppe>(), mitWeg: false);
        Assert.Empty(cut.FindAll(".epos-pruefleiste"));
    }

    [Fact]
    public void Neue_Prueffassung_liest_Leiste_und_Zonen_neu()
    {
        var probe = new ModellpruefungProbeweg();
        var cut = Aufbauen(probe, new List<Pruefgruppe>());
        int vorher = probe.Geprueft;
        Assert.Equal(0, _neugelesen);
        probe.Weg().AlleUebernehmen();
        cut.Render(p => p.Add(x => x.PruefFassung, 1));
        Assert.True(probe.Geprueft > vorher);
        Assert.Equal(1, _neugelesen);
        Assert.Contains("epos-ampel-gruen", cut.Find(".epos-pruefleiste-status").ClassName);
        Assert.Contains(cut.FindAll("td, th, input"), e => (e.GetAttribute("value") ?? e.TextContent).Contains("EG neu", StringComparison.Ordinal));
    }

    // =============================================================================
    //  N3: Gebäudefelder einer Übernahme (Feiertagsland, Konditionierung) neu lesen, sonst Leiste sperren
    // =============================================================================

    /// <summary>Ein vollständiger Satz mit runden Phantasiewerten, der die Prüfregeln des OK-Wegs hält.</summary>
    private static GebaeudeKatalogDaten Satz(string? land) => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Gebaeudeart = "Hotel", Verwendung = "Wohngebaeude",
        Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35, Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5, WwBedarf = 700, Feiertagsland = land,
    };

    private int _gebaeudeGelesen;

    /// <summary>Die Projektkopie nach der Übernahme der Gruppe 4: Feiertagsland „BW“.</summary>
    private GebaeudeKatalogDaten? NachUebernahme()
    {
        _gebaeudeGelesen++;
        return Satz("BW");
    }

    [Fact]
    public void N3_Uebernahme_liest_das_Feiertagsland_neu_und_OK_schreibt_es_nicht_zurueck()
    {
        var probe = new ModellpruefungProbeweg();
        var geschrieben = new List<GebaeudeKatalogDaten>();
        var cut = Aufbauen(probe, new List<Pruefgruppe>(), daten: Satz("BY"), gebaeudeNeulesen: NachUebernahme,
                           geschrieben: geschrieben);
        Assert.Equal("BY", cut.Instance.Arbeitsstand.Feiertagsland);
        Assert.Equal(0, _gebaeudeGelesen);
        probe.Weg().Uebernehmen!(Pruefgruppe.OrtLage);
        cut.Render(p => p.Add(x => x.PruefFassung, 1));
        Assert.Equal(1, _gebaeudeGelesen);
        Assert.Equal("BW", cut.Instance.Arbeitsstand.Feiertagsland);
        Assert.False(cut.Instance.PrueffelderGeaendert);
        cut.Find(".epos-leiste button.epos-knopf--primaer").Click();
        GebaeudeKatalogDaten d = Assert.Single(geschrieben);
        Assert.Equal("BW", d.Feiertagsland);
    }

    [Fact]
    public void N3_Gegenprobe_ungespeichertes_Feiertagsland_sperrt_die_Leiste_und_bleibt_stehen()
    {
        var probe = new ModellpruefungProbeweg();
        var geklickt = new List<Pruefgruppe>();
        var cut = Aufbauen(probe, geklickt, daten: Satz("BY"), gebaeudeNeulesen: NachUebernahme);
        cut.Instance.Arbeitsstand.Feiertagsland = "HE";
        cut.Render();
        Assert.True(cut.Instance.PrueffelderGeaendert);
        IElement vier = cut.Find(".epos-pruefleiste-gruppe[data-gruppe='4']");
        Assert.Equal("true", vier.GetAttribute("aria-disabled"));
        Assert.Equal(R.GEB_SPERRE_PRUEFFELDER, vier.GetAttribute("title"));
        vier.Click();
        Assert.Empty(geklickt);
        Assert.Equal(R.GEB_SPERRE_PRUEFFELDER, cut.Instance.Meldung);
        // Eine Prüffassung von außen liest die geänderten Felder nicht neu — der Anwenderwert bleibt.
        cut.Render(p => p.Add(x => x.PruefFassung, 1));
        Assert.Equal(0, _gebaeudeGelesen);
        Assert.Equal("HE", cut.Instance.Arbeitsstand.Feiertagsland);
    }

    [Fact]
    public void N3_Gegenprobe_geaenderte_Konditionierung_sperrt_die_Leiste()
    {
        var cut = Aufbauen(new ModellpruefungProbeweg(), new List<Pruefgruppe>(), daten: Satz("BY"),
                           gebaeudeNeulesen: NachUebernahme);
        cut.Instance.Arbeitsstand.Konditionierung = new KonditionierungDaten { Fassung = 1 };
        cut.Render();
        Assert.Equal("true", cut.Find(".epos-pruefleiste-gruppe[data-gruppe='3']").GetAttribute("aria-disabled"));
        cut.Render(p => p.Add(x => x.PruefFassung, 1));
        Assert.Equal(0, _gebaeudeGelesen);
    }

    [Fact]
    public void N3_unveraenderter_Dialog_laesst_die_Leiste_frei()
    {
        var geklickt = new List<Pruefgruppe>();
        var cut = Aufbauen(new ModellpruefungProbeweg(), geklickt, daten: Satz("BY"), gebaeudeNeulesen: NachUebernahme);
        Assert.False(cut.Instance.PrueffelderGeaendert);
        Assert.All(cut.FindAll(".epos-pruefleiste-gruppe"), g => Assert.Null(g.GetAttribute("aria-disabled")));
        cut.Find(".epos-pruefleiste-gruppe[data-gruppe='4']").Click();
        Assert.Equal(new[] { Pruefgruppe.OrtLage }, geklickt);
    }

    [Fact]
    public void Startzone_oeffnet_die_Zone_im_Zonendialog()
    {
        var cut = Aufbauen(new ModellpruefungProbeweg(), new List<Pruefgruppe>(), startZone: 2);
        Assert.NotEmpty(cut.FindAll(".epos-blatt--breit"));
        var ohne = Aufbauen(new ModellpruefungProbeweg(), new List<Pruefgruppe>());
        Assert.Empty(ohne.FindAll(".epos-blatt--breit"));
    }

    [Fact]
    public void Kurzbild_der_Woche_fasst_gleiche_Stunden_zusammen_und_nennt_Tag_Stunde_Wert()
    {
        double[] w = Enumerable.Range(0, 168).Select(i => i % 24 >= 6 && i % 24 < 22 ? 20.0 : 16.0).ToArray();
        w[5 * 24 + 3] = double.NaN;
        var cut = Render<Wochenkurzbild>(p => p
            .Add(x => x.Spuren, new[] { new Wochenkurzbild.Wochenspur("HEIZSOLL", "Heizen", "°C", w) })
            .Add(x => x.Beschriftung, "Woche"));
        Assert.Equal(7, cut.FindAll("rect.epos-wochenbild-tag").Count);
        // Je Tag drei Läufe (16, 20, 16); am Samstag zerfällt der Morgen um die Lücke „aus“ (16, aus, 16, 20, 16 → vier Balken).
        Assert.Equal(6 * 3 + 4, cut.FindAll("rect.epos-wochenbild-balken").Count);
        string[] titel = cut.FindAll("rect.epos-wochenbild-balken title").Select(t => t.TextContent).ToArray();
        Assert.Contains(titel, t => t.Contains("06–22", StringComparison.Ordinal) && t.Contains("20 °C", StringComparison.Ordinal));
        Assert.DoesNotContain(cut.Markup, "#");
        Assert.Empty(Render<Wochenkurzbild>().FindAll("svg"));
    }
}
