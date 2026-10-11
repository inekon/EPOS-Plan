using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der Reiter „Kältebedarf“ der Startseite (Konzept Kältebedarf 3.1 und 3.5, Stufe K4): drei Kacheln an dritter
/// Stelle hinter „Wärmebedarf“, Statuspunkte aus den Bits 8192 und 16384, die Zustandszeile der Gebäudekühlung, der
/// Tippkasten und die Hinweiszeile bei <c>Kuehlbetrieb = 0</c> mit Sprung in die Simulationskonfiguration. Der Fall
/// tauscht <c>Dienste.Navigation</c> und steht deshalb in der seriellen Sammlung.
/// </summary>
[Collection("KiDialogweg")]
public class KaeltebedarfReiterTests : EposBunitContext
{
    private readonly INavigation _vorher;

    public KaeltebedarfReiterTests()
    {
        _vorher = WindowsFormsApplication1.Dienste.Navigation;
        Navigation = new TestNavigation();
        WindowsFormsApplication1.Dienste.Navigation = Navigation;

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private TestNavigation Navigation { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing) WindowsFormsApplication1.Dienste.Navigation = _vorher;
        base.Dispose(disposing);
    }

    private static IReadOnlyList<StartKachel> Kacheln(int bitmaske = 0, Kachelstand gebaeude = Kachelstand.Aus)
    {
        Kachelstand Stand(int bit) => (bitmaske & bit) == bit ? Kachelstand.An : Kachelstand.Aus;
        return new[]
        {
            new StartKachel { Schluessel = Kachelschluessel.KaeltebedarfExtern, Reiter = Reiterschluessel.Kaeltebedarf,
                              Titel = Resource.START_K_KBEXTERN_T, Beschreibung = Resource.START_K_KBEXTERN_B, Zustand = Stand(8192) },
            new StartKachel { Schluessel = Kachelschluessel.Kaeltebedarfsprofile, Reiter = Reiterschluessel.Kaeltebedarf,
                              Titel = Resource.START_K_KBPROFIL_T, Beschreibung = Resource.START_K_KBPROFIL_B, Zustand = Stand(16384) },
            new StartKachel { Schluessel = Kachelschluessel.Gebaeudekuehlung, Reiter = Reiterschluessel.Kaeltebedarf,
                              Titel = Resource.START_K_GEBKUEHL_T, Beschreibung = Resource.START_K_GEBKUEHL_B, Zustand = gebaeude }
        };
    }

    private IRenderedComponent<KaeltebedarfReiter> Reiter(GebaeudekuehlungDaten? stand, int bitmaske = 0,
                                                          Action? konfig = null, List<string>? geklickt = null)
        => Render<KaeltebedarfReiter>(p => p
            .Add(x => x.Kacheln, Kacheln(bitmaske, stand?.Zustand ?? Kachelstand.Aus))
            .Add(x => x.Gebaeudekuehlung, stand is null ? null : () => stand)
            .Add(x => x.KonfigurationOeffnen, () => konfig?.Invoke())
            .Add(x => x.Geklickt, (string s) => geklickt?.Add(s)));

    private IRenderedComponent<Startseite> Startseite(GebaeudekuehlungDaten? stand, List<string> gemeldet, int bitmaske = 0)
        => Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln(bitmaske, stand?.Zustand ?? Kachelstand.Aus))
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Gebaeudekuehlung, stand is null ? null : () => stand)
            .Add(x => x.Geklickt, sch => gemeldet.Add(sch)));

    [Fact]
    public void Der_Reiter_steht_an_dritter_Stelle_hinter_Waermebedarf()
    {
        Assert.Equal(new[] { "PROJEKT", "WAERMEBEDARF", "KAELTEBEDARF", "STROMBEDARF", "ERZEUGER", "SIMULATION", "BERICHTE_KOSTEN" },
                     Reiterschluessel.Alle);

        var cut = Startseite(new GebaeudekuehlungDaten(true, 0, 1), new List<string>());
        Assert.Equal("Kältebedarf", cut.FindAll("[role='tab']")[2].TextContent.Trim());
    }

    [Fact]
    public void Drei_Kacheln_mit_Statuspunkten_aus_den_Kaeltebits_und_der_Gebaeudekuehlung()
    {
        var cut = Reiter(new GebaeudekuehlungDaten(true, 1, 2), bitmaske: 16384);

        var kacheln = cut.FindAll(".epos-kachel");
        Assert.Equal(3, kacheln.Count);
        Assert.Equal(new[] { "Kältebedarf extern", "Kältebedarfsprofile", "Gebäudekühlung" },
                     cut.FindAll(".epos-kachel-titel").Select(e => e.TextContent.Trim()).ToArray());

        var punkte = cut.FindAll(".epos-kachel-statuspunkt");
        Assert.Contains("epos-kachel-statuspunkt--aus", punkte[0].ClassList);
        Assert.DoesNotContain("epos-kachel-statuspunkt--aus", punkte[1].ClassList);
        Assert.DoesNotContain("epos-kachel-statuspunkt--aus", punkte[2].ClassList);
    }

    [Fact]
    public void Die_Gebaeudekuehlung_nennt_wie_viele_Gebaeude_gekuehlt_werden()
    {
        var cut = Reiter(new GebaeudekuehlungDaten(true, 1, 2));

        var status = cut.FindAll(".epos-kachel-status");
        Assert.Single(status.Where(e => e.TextContent.Contains("1 von 2 Gebäuden gekühlt", StringComparison.Ordinal)));
        Assert.DoesNotContain(status, e => e.TextContent.Contains(Resource.START_K_GEBKUEHL_AUS, StringComparison.Ordinal));
    }

    [Fact]
    public void Ohne_Kuehlbetrieb_stehen_Hinweiszeile_und_Zusatz_an_der_Gebaeudekuehlung()
    {
        bool gesprungen = false;
        var cut = Reiter(new GebaeudekuehlungDaten(false, 0, 1), konfig: () => gesprungen = true);

        var zeile = cut.Find(".epos-kaeltereiter-ohne");
        Assert.Contains(Resource.START_KB_OHNE_KUEHLBETRIEB, zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains("0 von 1 Gebäuden gekühlt · Projekt rechnet keine Kälte",
                        cut.FindAll(".epos-kachel-status")[2].TextContent, StringComparison.Ordinal);

        zeile.QuerySelector("button.epos-startreiter-verweis")!.Click();
        Assert.True(gesprungen);
    }

    [Fact]
    public void Mit_Kuehlbetrieb_oder_ohne_Bestand_steht_keine_Hinweiszeile()
    {
        Assert.Empty(Reiter(new GebaeudekuehlungDaten(true, 0, 0)).FindAll(".epos-kaeltereiter-ohne"));
        Assert.Empty(Reiter(null).FindAll(".epos-kaeltereiter-ohne"));
        Assert.Empty(Reiter(null).FindAll(".epos-kachel-status").Where(e => e.TextContent.Contains("von")));
    }

    [Fact]
    public void Tippkasten_und_Hilfeschluessel_stehen()
    {
        var cut = Reiter(null);

        Assert.Contains(Resource.START_KB_TIPP, cut.Markup, StringComparison.Ordinal);
        Assert.Equal("Form_Start.btn_Help_Kaeltebedarf", cut.Instance.HilfeSchluessel);
    }

    [Fact]
    public void Die_Hinweiszeile_springt_in_die_Simulationskonfiguration()
    {
        var gemeldet = new List<string>();
        var cut = Startseite(new GebaeudekuehlungDaten(false, 0, 1), gemeldet);

        cut.FindAll("[role='tab']")[2].Click();
        cut.Find(".epos-kaeltereiter-ohne button.epos-startreiter-verweis").Click();

        Assert.Equal(new[] { Masken.Simulation }, Navigation.Masken);
        Assert.Equal(SimulationMarke.SCHRITT_KONFIGURATION, Navigation.LetzteArgumente[0]);
        Assert.Empty(gemeldet);
    }

    [Fact]
    public void Ein_Klick_auf_eine_Kaeltekachel_meldet_ihren_Schluessel_an_die_Huelle()
    {
        var gemeldet = new List<string>();
        var cut = Startseite(new GebaeudekuehlungDaten(true, 0, 1), gemeldet);

        cut.FindAll("[role='tab']")[2].Click();
        var kacheln = cut.FindAll(".epos-kachel");
        Assert.Equal(3, kacheln.Count);
        foreach (int i in new[] { 0, 1, 2 }) cut.FindAll(".epos-kachel")[i].Click();

        Assert.Equal(new[] { Kachelschluessel.KaeltebedarfExtern, Kachelschluessel.Kaeltebedarfsprofile,
                             Kachelschluessel.Gebaeudekuehlung }, gemeldet);
        Assert.Empty(Navigation.Masken);
    }
}
