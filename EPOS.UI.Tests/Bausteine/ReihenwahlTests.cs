using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <c>Reihenwahl</c> (Auftrag KD-8 Teil A): Chips in Gruppen, Schalten, weiche
/// Sperre mit Grund, die letzte Reihe bleibt, „Vorgabe“, kein <c>datalist</c>.
/// </summary>
public class ReihenwahlTests : EposBunitContext
{
    private static IReadOnlyList<Reihenwahl.Reihenchip> Chips(string gesperrt = "", bool letzte = false) => new[]
    {
        new Reihenwahl.Reihenchip("G", "Global", "kWh/m²", "Strahlung")
            { Gewaehlt = true, Titel = "Reihe abwählen", Sperrgrund = letzte ? "Mindestens eine Reihe bleibt gewählt." : "" },
        new Reihenwahl.Reihenchip("D", "Diffus", "kWh/m²", "Strahlung") { Titel = "Reihe zuschalten" },
        new Reihenwahl.Reihenchip("F", "Feuchte", "%", "Temperatur und Feuchte") { Sperrgrund = gesperrt, Farbe = "rgb(1,2,3)" }
    };

    private IRenderedComponent<Reihenwahl> Zeige(IReadOnlyList<Reihenwahl.Reihenchip> chips, List<string> gemeldet,
                                                 Action? vorgabe = null)
        => Render<Reihenwahl>(p =>
        {
            p.Add(x => x.Chips, chips)
             .Add(x => x.Kopf, "Reihen im Bild")
             .Add(x => x.Leise, "leise")
             .Add(x => x.Umschalten, s => gemeldet.Add(s));
            if (vorgabe is not null) p.Add(x => x.Vorgabe, vorgabe);
        });

    [Fact]
    public void Chips_stehen_in_ihren_Gruppen_mit_Einheit_und_aria_pressed()
    {
        var cut = Zeige(Chips(), new List<string>());

        var gruppen = cut.FindAll(".epos-reihenwahl-gruppe");
        Assert.Equal(2, gruppen.Count);
        Assert.Equal("Strahlung", gruppen[0].QuerySelector(".epos-reihenwahl-gruppenname")!.TextContent);
        var chips = cut.FindAll("button.epos-reihenwahl-chip");
        Assert.Equal(3, chips.Count);
        Assert.Equal("true", chips[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", chips[1].GetAttribute("aria-pressed"));
        Assert.Contains("kWh/m²", chips[1].TextContent);
        Assert.Contains("--epos-reihenwahl-farbe:rgb(1,2,3)", chips[2].GetAttribute("style"));
        Assert.Empty(cut.FindAll("datalist"));
        Assert.Empty(cut.FindAll(".epos-reihenwahl-vorgabe"));   // kein Delegat, kein Knopf
    }

    [Fact]
    public void Klick_meldet_den_Schluessel()
    {
        var gemeldet = new List<string>();
        var cut = Zeige(Chips(), gemeldet);

        cut.FindAll("button.epos-reihenwahl-chip")[1].Click();

        Assert.Equal(new[] { "D" }, gemeldet);
    }

    [Fact]
    public void Weich_gesperrter_Chip_nennt_den_Grund_und_schaltet_nicht()
    {
        var gemeldet = new List<string>();
        const string grund = "Höchstens zwei Einheiten zugleich (kWh/m², °C); erst eine Reihe dieser Einheiten abwählen.";
        var cut = Zeige(Chips(grund), gemeldet);

        var chip = cut.FindAll("button.epos-reihenwahl-chip")[2];
        Assert.Equal("true", chip.GetAttribute("aria-disabled"));
        Assert.Null(chip.GetAttribute("disabled"));
        Assert.Equal(grund, chip.GetAttribute("title"));
        Assert.Equal("leise", cut.Find(".epos-reihenwahl-leise").TextContent);

        chip.Click();

        Assert.Empty(gemeldet);
        Assert.Equal(grund, cut.Find(".epos-reihenwahl-leise").TextContent);
        Assert.Contains("epos-reihenwahl-leise--warn", cut.Find(".epos-reihenwahl-leise").ClassName);
    }

    [Fact]
    public void Die_letzte_Reihe_bleibt_gedrueckt()
    {
        var gemeldet = new List<string>();
        var cut = Zeige(Chips(letzte: true), gemeldet);

        var chip = cut.FindAll("button.epos-reihenwahl-chip")[0];
        Assert.Null(chip.GetAttribute("aria-disabled"));
        chip.Click();

        Assert.Empty(gemeldet);
        Assert.Equal("Mindestens eine Reihe bleibt gewählt.", cut.Find(".epos-reihenwahl-leise").TextContent);
    }

    [Fact]
    public void Vorgabe_meldet_und_loescht_den_Hinweis()
    {
        int vorgabe = 0;
        var cut = Zeige(Chips("Grund"), new List<string>(), () => vorgabe++);
        cut.FindAll("button.epos-reihenwahl-chip")[2].Click();

        cut.Find(".epos-reihenwahl-vorgabe").Click();

        Assert.Equal(1, vorgabe);
        Assert.Equal("leise", cut.Find(".epos-reihenwahl-leise").TextContent);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_er_leer()
    {
        var cut = Render<Reihenwahl>();
        Assert.Empty(cut.FindAll("button.epos-reihenwahl-chip"));
        Assert.Single(cut.FindAll(".epos-reihenwahl"));
    }
}
