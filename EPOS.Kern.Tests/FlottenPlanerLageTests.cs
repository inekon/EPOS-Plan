using System;
using System.Collections.Generic;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Die Auskunft „Planer verfügbar?" (Auftrag #170c). Sie ist der VORDERE Riegel vor den zwei
/// Ausnahmen in <see cref="SpeicherFlottenProjektCtrl"/> und <c>FlottenSimulator</c>: Ohne
/// registrierte <see cref="SpeicherFlottenProjektCtrl.PlanerFactory"/> sollen die drei planenden
/// Betriebsziele in der Oberfläche gar nicht erst begehbar sein.
///
/// <para>Pinnt die Kultur (Auftrag #230, Befund „Windows-CI rot seit Lauf 262"): Die
/// Vorprüfung <see cref="SpeicherFlottenProjektCtrl.Pruefe"/> meldet auf Deutsch
/// („Fahrplan-Löser"), die Ressourcen folgen <c>CurrentUICulture</c> — auf dem
/// Windows-Läufer (en-US) sonst englisch.</para>
/// </summary>
public sealed class FlottenPlanerLageTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new();

    public void Dispose() => _kultur.Dispose();

    [Fact]
    public void OhneFabrik_IstKeinPlanerDa_undDerGrundStehtInDenRessourcen()
    {
        MitFabrik(null, () =>
        {
            Assert.False(FlottenPlanerLage.Verfuegbar);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.FLOTTE_PLANER_FEHLT,
                FlottenPlanerLage.Grund);
            Assert.NotEmpty(FlottenPlanerLage.Grund);
        });
    }

    [Fact]
    public void MitFabrikDieEinenPlanerLiefert_IstErVerfuegbar_undDerGrundIstLeer()
    {
        MitFabrik(() => new StillerPlaner(), () =>
        {
            Assert.True(FlottenPlanerLage.Verfuegbar);
            Assert.Equal("", FlottenPlanerLage.Grund);
        });
    }

    [Fact]
    public void FabrikDieNullLiefert_ZaehltWieKeineFabrik()
    {
        MitFabrik(() => null, () => Assert.False(FlottenPlanerLage.Verfuegbar));
    }

    [Fact]
    public void FabrikDieWirft_ZaehltWieKeineFabrik()
    {
        MitFabrik(() => throw new DllNotFoundException("libscip"),
            () => Assert.False(FlottenPlanerLage.Verfuegbar));
    }

    [Theory]
    [InlineData(FlottenBetriebsziel.PvGreedy, false)]
    [InlineData(FlottenBetriebsziel.PeakShaving, false)]
    [InlineData(FlottenBetriebsziel.PvPlanung, true)]
    [InlineData(FlottenBetriebsziel.Arbitrage, true)]
    [InlineData(FlottenBetriebsziel.MultiUse, true)]
    public void IstPlanend_KenntGenauDieDreiPlanendenZiele(FlottenBetriebsziel ziel, bool planend)
        => Assert.Equal(planend, FlottenPlanerLage.IstPlanend(ziel));

    [Fact]
    public void ZielMoeglich_SperrtOhnePlanerNurDiePlanendenZiele()
    {
        MitFabrik(null, () =>
        {
            Assert.True(FlottenPlanerLage.ZielMoeglich(FlottenBetriebsziel.PvGreedy));
            Assert.True(FlottenPlanerLage.ZielMoeglich(FlottenBetriebsziel.PeakShaving));
            Assert.False(FlottenPlanerLage.ZielMoeglich(FlottenBetriebsziel.PvPlanung));
            Assert.False(FlottenPlanerLage.ZielMoeglich(FlottenBetriebsziel.Arbitrage));
            Assert.False(FlottenPlanerLage.ZielMoeglich(FlottenBetriebsziel.MultiUse));
        });

        MitFabrik(() => new StillerPlaner(), () =>
        {
            foreach (FlottenBetriebsziel ziel in Enum.GetValues<FlottenBetriebsziel>())
                Assert.True(FlottenPlanerLage.ZielMoeglich(ziel));
        });
    }

    [Fact]
    public void EinFabrikwechsel_WirdBemerkt()
    {
        MitFabrik(null, () =>
        {
            Assert.False(FlottenPlanerLage.Verfuegbar);
            SpeicherFlottenProjektCtrl.PlanerFactory = () => new StillerPlaner();
            Assert.True(FlottenPlanerLage.Verfuegbar);
            SpeicherFlottenProjektCtrl.PlanerFactory = null;
            Assert.False(FlottenPlanerLage.Verfuegbar);
        });
    }

    /// <summary>
    /// Die zwei REAKTIVEN Ziele rechnen ohne Planer weiter — das ist der Grund, warum die
    /// Oberfläche nur die drei planenden sperrt und nicht die ganze Flotte.
    /// </summary>
    [Theory]
    [InlineData(FlottenBetriebsziel.PvGreedy)]
    [InlineData(FlottenBetriebsziel.PeakShaving)]
    public void ReaktiveZiele_RechnenOhnePlaner(FlottenBetriebsziel ziel)
    {
        MitFabrik(null, () =>
        {
            FlottenStudieKonfiguration config = Konfiguration(ziel);
            SpeicherFlottenProjektLauf lauf = SpeicherFlottenProjektCtrl.Rechnen(
                Eingang(
                    new FlottenNetzintervall { LastKw = 0, PvKw = 8, BezugspreisEuroProKWh = .2 },
                    new FlottenNetzintervall { LastKw = 8, PvKw = 0, BezugspreisEuroProKWh = .2 }),
                config, null, planer: null);

            Assert.True(lauf.Studie.Variante.Zulaessig);
            Assert.Equal(2, lauf.NetzleistungKw.Length);
        });
    }

    /// <summary>
    /// Die letzte Schranke bleibt: Ohne Fabrik wirft der Weg zum Planer weiterhin — die Auskunft
    /// ERSETZT die Ausnahme nicht, sie führt nur daran vorbei.
    /// </summary>
    [Fact]
    public void PlanendesZiel_OhneFabrik_WirftWeiterhinImLauf()
    {
        MitFabrik(null, () =>
        {
            FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                FlottenSimulator.Simuliere(
                    Eingang(new FlottenNetzintervall { LastKw = 1, BezugspreisEuroProKWh = .2 }),
                    config, null, CancellationToken.None));
            Assert.Contains("IFlottenPlaner", ex.Message);
        });
    }

    /// <summary>
    /// BEFUND #185: Die neue Vorprüfung einer Projektflotte nennt ein planendes
    /// Betriebsziel ohne Fahrplan-Löser als benanntes PROBLEM — der Lauf meldete es
    /// vorher erst tief in der Engine. Der Fall steht hier und nicht bei den
    /// Kostenprüffällen, weil die Fabrik prozessweiter Zustand ist und jeder Tausch
    /// in diese eine Klasse gehört.
    /// </summary>
    [Fact]
    public void Vorpruefung_NenntDasPlanendeZielOhnePlaner()
    {
        MitFabrik(null, () =>
        {
            SpeicherOptimierungEingaben eingaben = FlottenEingaben(FlottenBetriebsziel.Arbitrage);

            FlottenProjektPruefung pruefung =
                SpeicherFlottenProjektCtrl.Pruefe(eingaben, 0, false);

            Assert.False(pruefung.Rechenbar);
            Assert.Contains(pruefung.Probleme, x => x.Contains("Fahrplan-Löser"));
            Assert.Contains("Ausweg", pruefung.Meldung);
        });

        MitFabrik(() => new StillerPlaner(), () =>
        {
            SpeicherOptimierungEingaben eingaben = FlottenEingaben(FlottenBetriebsziel.Arbitrage);

            Assert.True(SpeicherFlottenProjektCtrl.Pruefe(eingaben, 0, false).Rechenbar);
        });
    }

    private static SpeicherOptimierungEingaben FlottenEingaben(FlottenBetriebsziel ziel)
        => new()
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Lastquelle = SpeicherAuslegungQuelle.Epos,
                PvQuelle = SpeicherAuslegungQuelle.Keine,
                Preisquelle = SpeicherAuslegungQuelle.Epos,
                Flotte = Konfiguration(ziel)
            }
        };

    /// <summary>
    /// Setzt die Fabrik, führt die Prüfung aus und stellt den vorherigen Stand wieder her. Der
    /// Zwischenspeicher der Auskunft wird vorher und nachher verworfen, damit kein Prüffall den
    /// nächsten färbt.
    /// </summary>
    private static void MitFabrik(Func<IFlottenPlaner> fabrik, Action pruefung)
    {
        Func<IFlottenPlaner> vorher = SpeicherFlottenProjektCtrl.PlanerFactory;
        try
        {
            SpeicherFlottenProjektCtrl.PlanerFactory = fabrik;
            FlottenPlanerLage.Vergessen();
            pruefung();
        }
        finally
        {
            SpeicherFlottenProjektCtrl.PlanerFactory = vorher;
            FlottenPlanerLage.Vergessen();
        }
    }

    private static FlottenEingang Eingang(params FlottenNetzintervall[] intervalle)
    {
        DateTimeOffset start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < intervalle.Length; i++)
            intervalle[i].Zeitstempel = start.AddMinutes(i * 15);
        return new FlottenEingang
        {
            KonfigurationId = "config", DatenId = "data",
            Istwerte = new List<FlottenNetzintervall>(intervalle)
        };
    }

    private static FlottenStudieKonfiguration Konfiguration(FlottenBetriebsziel ziel) => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            new()
            {
                Id = "s1", Name = "Speicher 1", KapazitaetKWh = 10,
                LadeleistungKw = 4, EntladeleistungKw = 4,
                Ladewirkungsgrad = 1, Entladewirkungsgrad = 1,
                SocMin = 0, SocStart = 0, SocMax = 1
            }
        },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = ziel,
            EnergieAusgleichEuroProKWh = 0,
            PrognoseFallbackErlaubt = false,
            WirtschaftlicherPeakZielwertKw = 2
        },
        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            { ProjektjahreBeiWiederholung = 1 }
    };

    /// <summary>Ein Planer, der nur beweist, dass die Fabrik etwas liefert.</summary>
    private sealed class StillerPlaner : IFlottenPlaner
    {
        public FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Dieser Prüfplaner rechnet nicht.");
    }
}
