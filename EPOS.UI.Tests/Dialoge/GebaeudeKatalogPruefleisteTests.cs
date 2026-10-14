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
                                                               bool mitWeg = true, int? startZone = null)
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
            p.Add(x => x.Daten, new GebaeudeKatalogDaten { Name = "Haus A", Gebaeudeart = "Hotel", WohnflaecheGesamt = 100 })
             .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
             .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
             .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
             .Add(x => x.Zonen, zonenweg)
             .Add(x => x.StartReiter, GebaeudeKatalogDialog.REITER_ZONEN)
             .Add(x => x.StartZone, startZone);
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
