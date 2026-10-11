using System.Linq;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// UB‑E4 (Fachkonzept Übergabegrenze 7.1): die Kachelzeile „Betriebsbereiche“ des Wärmepumpen-Reiters — nur mit
/// Bereichsdaten, je Bereich Stunden und Wärme, Bivalenzpunkte und Übergabegrenze, die leisen Zeilen der Zähler nur
/// über 0 und die Hinweiszeile „Stundenmodell“. Kultur de-DE über die <see cref="Kulturvorrichtung"/>.
/// </summary>
public class WaermepumpeReiterBivalenzTests : EposBunitContext
{
    public WaermepumpeReiterBivalenzTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static SimulationErgebnisCtrl.WaermepumpeErgebnis Erg(Bereichskennzahlen? bereiche)
    {
        var e = new SimulationErgebnisCtrl.WaermepumpeErgebnis
        {
            DeckungProzent = 62.5, StufeneingangMwh = 480.25, RestwaermeMwh = 180.0, StromverbrauchMwh = 75.0,
            WaermeproduktionMwh = 300.0, Vollbenutzungsstunden = 1856.0, MinSpkLeistungKw = 20.22,
            Bereiche = bereiche,
        };
        e.Module.Add(new SimulationErgebnisCtrl.WpModulZeile("WP 1", 30.0, 300.0, 75.0, 2.5, 1856.0));
        return e;
    }

    private static Bereichskennzahlen Bereiche(int spreizung = 0, int ruecklauf = 0) => new()
    {
        Stunden = new int?[] { 4200, 1300, 250, 90 },
        Mwh = new double?[] { 180.5, 95.25, 12.0, 0.0 },
        Spreizung_Unterschritten_h = spreizung,
        Ruecklauf_Ueberschritten_h = ruecklauf,
        Bivalenzpunkt_1 = -4.2,
        Bivalenzpunkt_2 = -9.8,
        Uebergabe_Max_kW = 41.5,
    };

    private IRenderedComponent<WaermepumpeReiter> Zeichnen(SimulationErgebnisCtrl.WaermepumpeErgebnis erg)
        => Render<WaermepumpeReiter>(p =>
        {
            p.Add(x => x.Daten, erg);
            p.Add(x => x.Modell, _ => null);
        });

    [Fact]
    public void Mit_Bereichsdaten_steht_die_Kachelzeile_mit_allen_Werten()
    {
        using var k = new Kulturvorrichtung("de-DE");
        var c = Zeichnen(Erg(Bereiche()));

        var kachel = c.Find("[data-kachel=betriebsbereiche]");
        string text = kachel.TextContent;
        Assert.Contains(R.SIM_KACHEL_BETRIEBSBEREICHE, text);
        Assert.Contains(R.SIM_BEREICH_WP_ALLEIN, text);
        Assert.Contains(R.SIM_BEREICH_PARALLEL, text);
        Assert.Contains(R.SIM_BEREICH_VORWAERMUNG, text);
        Assert.Contains(R.SIM_BEREICH_NUR_KESSEL, text);
        Assert.Contains("4.200 h · 180,50", text);
        Assert.Contains("1.300 h · 95,25", text);
        Assert.Contains("-4,2", text);
        Assert.Contains("-9,8", text);
        Assert.Contains("41,5", text);
        Assert.Contains(R.BER_HINWEIS_STUNDENMODELL, text);
    }

    [Fact]
    public void Ohne_Bereichsdaten_keine_Kachelzeile()
    {
        using var k = new Kulturvorrichtung("de-DE");
        var c = Zeichnen(Erg(null));

        Assert.Empty(c.FindAll("[data-kachel=betriebsbereiche]"));
        Assert.DoesNotContain(R.SIM_KACHEL_BETRIEBSBEREICHE, c.Markup);
    }

    [Fact]
    public void Leise_Zeilen_nur_ueber_null()
    {
        using var k = new Kulturvorrichtung("de-DE");
        var ohne = Zeichnen(Erg(Bereiche()));
        Assert.Empty(ohne.FindAll("[data-zeile=spreizung]"));
        Assert.Empty(ohne.FindAll("[data-zeile=ruecklauf]"));

        var mit = Zeichnen(Erg(Bereiche(spreizung: 12, ruecklauf: 1234)));
        Assert.Contains("12 h", mit.Find("[data-zeile=spreizung]").TextContent);
        Assert.Contains("1.234 h", mit.Find("[data-zeile=ruecklauf]").TextContent);
    }

    [Fact]
    public void Nur_eine_Zaehlerzeile_wenn_nur_einer_ueber_null()
    {
        using var k = new Kulturvorrichtung("de-DE");
        var c = Zeichnen(Erg(Bereiche(ruecklauf: 5)));
        Assert.Empty(c.FindAll("[data-zeile=spreizung]"));
        Assert.Single(c.FindAll("[data-zeile=ruecklauf]"));
    }
}
