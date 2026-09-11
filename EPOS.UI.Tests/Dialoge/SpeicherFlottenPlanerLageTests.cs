using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Flottenziele OHNE registrierten Fahrplan-Löser (Auftrag #170c). Auf dem iPad und in
/// den Linux-Prüfständen setzt niemand <c>SpeicherFlottenProjektCtrl.PlanerFactory</c> — die
/// drei planenden Ziele <c>PvPlanung</c>, <c>Arbitrage</c> und <c>MultiUse</c> sind dort nicht
/// rechenbar. Bis hierher endete ihre Wahl erst IM LAUF mit einer Ausnahme; jetzt stehen sie
/// gesperrt in der Liste und nennen den Grund, ein gespeichertes planendes Profil erklärt sich
/// mit einem Banner, und der Rechenknopf des Dialogs bleibt zu.
/// </summary>
public sealed class SpeicherFlottenPlanerLageTests : EposBunitContext
{
    /// <summary>Die Ids der Liste im Baustein — sie folgen der Reihenfolge des Aufzählungstyps.</summary>
    private const string Reaktiv = "0";      // PvGreedy
    private const string PeakShaving = "1";
    private const string PvPlanung = "2";
    private const string Arbitrage = "3";
    private const string MultiUse = "4";

    [Fact]
    public void Ohne_Planer_sind_genau_die_drei_planenden_Ziele_gesperrt()
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen())
            .Add(x => x.PlanerVerfuegbar, false));

        Assert.Equal(new[] { PvPlanung, Arbitrage, MultiUse },
            Optionen(cut).Where(x => x.HasAttribute("disabled"))
                         .Select(x => x.GetAttribute("value")).ToArray());
        Assert.All(Optionen(cut).Where(x => x.HasAttribute("disabled")),
            x => Assert.Equal(Resource.FLOTTE_PLANER_ZIEL_GESPERRT, x.GetAttribute("title")));
        Assert.All(Optionen(cut).Where(x => !x.HasAttribute("disabled")),
            x => Assert.Null(x.GetAttribute("title")));
    }

    [Fact]
    public void Mit_Planer_ist_kein_Ziel_gesperrt_und_kein_Banner_zu_sehen()
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen())
            .Add(x => x.PlanerVerfuegbar, true));

        Assert.DoesNotContain(Optionen(cut), x => x.HasAttribute("disabled"));
        Assert.DoesNotContain(Resource.FLOTTE_PLANER_FEHLT, cut.Markup);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Ohne_Planer_nennt_ein_Hinweisbanner_den_Grund()
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen())
            .Add(x => x.PlanerVerfuegbar, false));

        Assert.Contains(Resource.FLOTTE_PLANER_FEHLT, cut.Markup);
        Assert.Single(cut.FindAll(".epos-warnbanner--hinweis"));
    }

    /// <summary>
    /// Der Kernfall: ein GESPEICHERTES Profil mit planendem Ziel. Es wird gezeichnet und
    /// erklärt — nicht geworfen.
    /// </summary>
    [Theory]
    [InlineData(FlottenBetriebsziel.PvPlanung, "PV-Prognoseplanung")]
    [InlineData(FlottenBetriebsziel.Arbitrage, "Preisoptimierung / Arbitrage")]
    [InlineData(FlottenBetriebsziel.MultiUse, "Multi Use mit Peak-Priorität")]
    public void Gespeichertes_planendes_Profil_erklaert_sich_mit_einem_Banner(
        FlottenBetriebsziel ziel, string name)
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen { Betriebsziel = ziel })
            .Add(x => x.PlanerVerfuegbar, false));

        Assert.Contains(string.Format(Resource.FLOTTE_PLANER_PROFIL, name), cut.Markup);
        Assert.Single(cut.FindAll(".epos-warnbanner--warnung"));
        // Das gespeicherte Ziel bleibt in der Liste gewaehlt - es wird erklaert, nicht ersetzt.
        Assert.Equal(((int)ziel).ToString(),
            Optionen(cut).Single(x => x.HasAttribute("selected")).GetAttribute("value"));
    }

    [Fact]
    public void Ohne_Planer_wird_ein_planendes_Ziel_nicht_uebernommen()
    {
        FlottenSimulationOptionen? gemeldet = null;
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen())
            .Add(x => x.PlanerVerfuegbar, false)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Auswahl(cut).Change(MultiUse);

        Assert.Null(gemeldet);
        Assert.Equal(Reaktiv, Optionen(cut).Single(x => x.HasAttribute("selected")).GetAttribute("value"));
    }

    /// <summary>Die zwei REAKTIVEN Ziele bleiben ohne Planer voll bedienbar.</summary>
    [Fact]
    public void Ohne_Planer_bleiben_PvGreedy_und_PeakShaving_waehlbar()
    {
        FlottenSimulationOptionen? gemeldet = null;
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen())
            .Add(x => x.PlanerVerfuegbar, false)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Auswahl(cut).Change(PeakShaving);
        Assert.Equal(FlottenBetriebsziel.PeakShaving, gemeldet!.Betriebsziel);

        Auswahl(cut).Change(Reaktiv);
        Assert.Equal(FlottenBetriebsziel.PvGreedy, gemeldet.Betriebsziel);
    }

    [Fact]
    public void Dialog_sperrt_den_Rechenknopf_fuer_ein_planendes_Ziel_ohne_Planer()
    {
        var cut = Dialog(FlottenBetriebsziel.Arbitrage, planer: false);

        Assert.True(Rechenknopf(cut).HasAttribute("disabled"));
        Assert.Contains(Resource.FLOTTE_PLANER_LAUF_GESPERRT, cut.Markup);
    }

    [Fact]
    public void Dialog_laesst_ein_reaktives_Ziel_ohne_Planer_rechnen()
    {
        var cut = Dialog(FlottenBetriebsziel.PeakShaving, planer: false);

        Assert.False(Rechenknopf(cut).HasAttribute("disabled"));
        Assert.DoesNotContain(Resource.FLOTTE_PLANER_LAUF_GESPERRT, cut.Markup);
    }

    [Fact]
    public void Dialog_laesst_ein_planendes_Ziel_mit_Planer_rechnen()
    {
        var cut = Dialog(FlottenBetriebsziel.Arbitrage, planer: true);

        Assert.False(Rechenknopf(cut).HasAttribute("disabled"));
        Assert.DoesNotContain(Resource.FLOTTE_PLANER_LAUF_GESPERRT, cut.Markup);
    }

    /// <summary>
    /// Auch die ZIELLISTE der Auslegung zaehlt: Sie variiert ueber Betriebsziele und braeuchte
    /// den Planer genauso wie das eingestellte Ziel.
    /// </summary>
    [Fact]
    public void Dialog_sperrt_auch_ein_planendes_Ziel_im_Auslegungsraster()
    {
        FlottenStudieKonfiguration flotte = Flotte(FlottenBetriebsziel.PvGreedy);
        flotte.Auslegung.Betriebsziele.Add(FlottenBetriebsziel.MultiUse);

        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.PlanerVerfuegbar, false)
            .Add(x => x.Vorgaben, () => new SpeicherOptimierungVorgaben
            {
                Eingaben = new SpeicherOptimierungEingaben
                    { Auslegung = new SpeicherAuslegungKonfiguration { Flotte = flotte } }
            })
            .Add(x => x.Rechnen, (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())));

        Assert.True(Rechenknopf(cut).HasAttribute("disabled"));
        Assert.Contains(Resource.FLOTTE_PLANER_LAUF_GESPERRT, cut.Markup);
    }

    private IRenderedComponent<SpeicherFlottenDialog> Dialog(FlottenBetriebsziel ziel, bool planer) =>
        Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.PlanerVerfuegbar, planer)
            .Add(x => x.Vorgaben, () => new SpeicherOptimierungVorgaben
            {
                Eingaben = new SpeicherOptimierungEingaben
                    { Auslegung = new SpeicherAuslegungKonfiguration { Flotte = Flotte(ziel) } }
            })
            .Add(x => x.Rechnen, (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())));

    private static FlottenStudieKonfiguration Flotte(FlottenBetriebsziel ziel) => new()
    {
        Einheiten = new()
        {
            new() { Id = "a", Name = "A", KapazitaetKWh = 20, LadeleistungKw = 5,
                    EntladeleistungKw = 5, SocMin = .1, SocStart = .5, SocMax = .9 }
        },
        Optionen = new FlottenSimulationOptionen
            { Betriebsziel = ziel, EnergieAusgleichEuroProKWh = .2 },
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            { ProjektjahreBeiWiederholung = 1 }
    };

    private static AngleSharp.Dom.IElement Rechenknopf(IRenderedComponent<SpeicherFlottenDialog> cut) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Speichervergleich berechnen");

    private static AngleSharp.Dom.IElement Auswahl(IRenderedComponent<SpeicherFlottenBetriebEditor> cut) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains("Betriebsziel:")).QuerySelector("select")!;

    private static AngleSharp.Dom.IElement[] Optionen(
        IRenderedComponent<SpeicherFlottenBetriebEditor> cut) =>
        Auswahl(cut).QuerySelectorAll("option").ToArray();
}
