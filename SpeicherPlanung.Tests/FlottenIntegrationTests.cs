using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;
using Xunit.Abstractions;

namespace SpeicherPlanung.Tests;

public sealed class FlottenIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public FlottenIntegrationTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(FlottenBetriebsziel.PvGreedy)]
    [InlineData(FlottenBetriebsziel.PeakShaving)]
    public void ReaktiveStrategienNutzenHeterogeneFlotteOhnePlanerOderPrognose(
        FlottenBetriebsziel ziel)
    {
        FlottenNetzintervall[] rows = Reihen(8, i => Zeile(i, last: i < 4 ? 24 : 4, pv: i >= 4 ? 12 : 0));
        FlottenStudieKonfiguration config = Konfiguration(ziel);
        config.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
        config.Optionen.WirtschaftlicherPeakZielwertKw = ziel == FlottenBetriebsziel.PeakShaving ? 10 : null;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(Eingang(rows), config);

        Assert.True(result.Variante.Zulaessig);
        Assert.Contains(result.Variante.Intervalle, x => x.IstleistungKwJeSpeicher.Any(p => Math.Abs(p) > 1e-6));
        Assert.All(result.ReferenzOhneSpeicher.Intervalle, x => Assert.Null(x.PlanId));
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact]
    public void PvPlanungPlantUeberschussFuerSpaetereLast()
    {
        FlottenNetzintervall[] rows = Reihen(4, i => i < 2
            ? Zeile(i, pv: 20, bezug: 0.2)
            : Zeile(i, last: 15, bezug: 0.4));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.PvPlanung);
        config.Optionen.PlanungshorizontIntervalle = 4;
        config.Optionen.NeuplanungAlleIntervalle = 4;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(
            EingangMitPrognose(rows, PrognoseArt.VerifiziertBekannt), config, new OrToolsFlottenPlaner());

        Assert.True(result.Variante.Zulaessig);
        Assert.Equal(0, result.Variante.PlanFallbackIntervalle);
        Assert.All(result.Variante.Intervalle, x => Assert.False(string.IsNullOrWhiteSpace(x.PlanId)));
        Assert.Contains(result.Variante.Intervalle.Take(2), x => x.IstleistungKwJeSpeicher.Sum() < -1e-6);
        Assert.Contains(result.Variante.Intervalle.Skip(2), x => x.IstleistungKwJeSpeicher.Sum() > 1e-6);
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact]
    public void ArbitrageReagiertAufNegativeUndHohePreiseOhneRichtungskreislauf()
    {
        FlottenNetzintervall[] rows = Reihen(4, i => i < 2
            ? Zeile(i, last: 2, bezug: -0.2)
            : Zeile(i, last: 14, bezug: 1.0));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
        config.Optionen.NetzladungErlaubt = true;
        config.Optionen.PlanungshorizontIntervalle = 4;
        config.Optionen.NeuplanungAlleIntervalle = 4;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(
            EingangMitPrognose(rows, PrognoseArt.VerifiziertBekannt), config, new OrToolsFlottenPlaner());

        Assert.Contains(result.Variante.Intervalle.Take(2), x => x.IstleistungKwJeSpeicher.Sum() < -1e-6);
        Assert.Contains(result.Variante.Intervalle.Skip(2), x => x.IstleistungKwJeSpeicher.Sum() > 1e-6);
        Assert.True(result.Variantenrechnung.EnergiekostenEuro < result.Referenzrechnung.EnergiekostenEuro);
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact]
    public void MultiUseHaeltPeakPrioritaetAuchGegenTeureWiederaufladung()
    {
        FlottenNetzintervall[] rows = Reihen(4, i => i < 2
            ? Zeile(i, last: 20, bezug: 0)
            : Zeile(i, last: 0, bezug: 100));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.MultiUse);
        config.Optionen.NetzladungErlaubt = true;
        config.Optionen.WirtschaftlicherPeakZielwertKw = 10;
        config.Optionen.PlanungshorizontIntervalle = 4;
        config.Optionen.NeuplanungAlleIntervalle = 4;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(
            EingangMitPrognose(rows, PrognoseArt.VerifiziertBekannt), config, new OrToolsFlottenPlaner());

        Assert.InRange(result.Variante.MaximalerNetzbezugKw, 0, 10 + 1e-5);
        Assert.Contains(result.Variante.Intervalle.Take(2), x => x.IstleistungKwJeSpeicher.Sum() > 1e-6);
        Assert.Contains(result.Variante.Intervalle.Skip(2), x => x.IstleistungKwJeSpeicher.Sum() < -1e-6);
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact]
    public void QuellenspezifischeVerguetungBleibtDurchPlanungUndTarifrechnungGetrennt()
    {
        FlottenNetzintervall[] rows =
        {
            Zeile(0, last: 4, pv: 5, bhkw: 5, bezug: -1, pvPreis: 0.08, bhkwPreis: 0.12)
        };
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.PvPlanung);
        config.Optionen.PlanungshorizontIntervalle = 1;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(
            EingangMitPrognose(rows, PrognoseArt.VerifiziertBekannt), config, new OrToolsFlottenPlaner());
        FlottenIntervallErgebnis x = result.Variante.Intervalle[0];

        Assert.Equal(1, x.PvNetzeinspeisungKw, 6);
        Assert.Equal(5, x.BhkwNetzeinspeisungKw, 6);
        Assert.Equal(0, x.BatterieNetzeinspeisungKw, 6);
        Assert.Equal(-0.25 * (0.08 + 5 * 0.12), result.Variantenrechnung.EnergiekostenEuro, 6);
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact]
    public void EndenergieAusgleichVerhindertEntleerungImOffenenHorizont()
    {
        FlottenNetzintervall[] rows = { Zeile(0, last: 8, bezug: 1) };
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
        config.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
        config.Optionen.EnergieAusgleichEuroProKWh = 2;
        config.Optionen.PlanungshorizontIntervalle = 1;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(
            EingangMitPrognose(rows, PrognoseArt.VerifiziertBekannt), config, new OrToolsFlottenPlaner());

        Assert.All(result.Variante.Intervalle[0].IstleistungKwJeSpeicher, p => Assert.InRange(p, -1e-6, 1e-6));
        Assert.All(result.EndenergieAenderungKWhJeSpeicher, e => Assert.InRange(e, -1e-6, 1e-6));
        Assert.InRange(result.EndenergieAusgleichEuro, -1e-6, 1e-6);
    }

    [Fact]
    public void VerifiziertePrognoseMussZumEntscheidungszeitpunktBekanntSein()
    {
        FlottenNetzintervall[] rows = { Zeile(0, last: 8) };
        var input = Eingang(rows);
        input.Prognosen.Add(new FlottenPrognoseSnapshot(
            "zu-spaet", Start.AddMinutes(15), Start, PrognoseArt.VerifiziertBekannt, rows));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            FlottenSimulator.Simuliere(input, config, new OrToolsFlottenPlaner()));
        Assert.Contains("Kein expliziter Prognose-Snapshot", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OracleModusVerwendetKeinenVerifiziertenSnapshotAlsErsatz()
    {
        FlottenNetzintervall[] rows = { Zeile(0, last: 8) };
        FlottenEingang input = EingangMitPrognose(rows, PrognoseArt.VerifiziertBekannt);
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
        config.Optionen.PrognoseArt = PrognoseArt.Oracle;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            FlottenSimulator.Simuliere(input, config, new OrToolsFlottenPlaner()));
        Assert.Contains("Oracle", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EinOracleSnapshotKannExplizitFuerMehrereNeuplanungenWiederverwendetWerden()
    {
        FlottenNetzintervall[] rows = Reihen(6, i => Zeile(i, last: 8 + i, bezug: 0.2 + 0.01 * i));
        FlottenEingang input = Eingang(rows);
        input.Prognosen.Add(new FlottenPrognoseSnapshot(
            "oracle-jahr", Start.AddYears(1), Start, PrognoseArt.Oracle, rows));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
        config.Optionen.PrognoseArt = PrognoseArt.Oracle;
        config.Optionen.PlanungshorizontIntervalle = 3;
        config.Optionen.NeuplanungAlleIntervalle = 1;

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(input, config, new OrToolsFlottenPlaner());

        Assert.All(result.Variante.Intervalle, x => Assert.Equal("oracle-jahr", x.PrognoseId));
        Assert.Equal(rows.Length, result.Variante.Intervalle.Select(x => x.PlanId).Distinct().Count());
        Assert.Equal(0, result.Variante.PlanFallbackIntervalle);
    }

    [Fact]
    public void ForecastModusOhneSpeicherBrauchtWederSnapshotNochPlanerFuerDieReferenz()
    {
        FlottenNetzintervall[] rows = { Zeile(0, last: 8, pv: 2) };
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.PvPlanung);
        config.Einheiten.Clear();

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(Eingang(rows), config);

        Assert.Equal(6, result.ReferenzOhneSpeicher.Intervalle[0].NetzbezugKw, 6);
        Assert.Equal(6, result.Variante.Intervalle[0].NetzbezugKw, 6);
    }

    [Fact]
    public void ReaktivesViertelstundenjahrDurchlaeuftAlle35040Intervalle()
    {
        FlottenNetzintervall[] rows = Reihen(35040, i =>
        {
            double tagesphase = 2 * Math.PI * (i % 96) / 96;
            return Zeile(i,
                last: 18 + 5 * (1 + Math.Sin(tagesphase)),
                pv: Math.Max(0, 24 * Math.Sin(tagesphase - Math.PI / 2)));
        });
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.PvGreedy);
        config.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
        var watch = Stopwatch.StartNew();

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(Eingang(rows), config);
        watch.Stop();

        _output.WriteLine($"35040 reactive intervals: {watch.Elapsed.TotalSeconds:F3} s");
        Assert.Equal(35040, result.Variante.Intervalle.Count);
        Assert.True(result.Variante.Zulaessig);
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact]
    public void OracleVierWochenPlantMitHorizont192UndNeuplanung96()
    {
        const int intervalle = 28 * 96;
        FlottenNetzintervall[] rows = Reihen(intervalle, i => Zeile(i, last: 5, pv: 5, bezug: 0.2));
        FlottenEingang input = Eingang(rows);
        input.Prognosen.Add(new FlottenPrognoseSnapshot(
            "oracle-smoke", Start.AddYears(1), Start, PrognoseArt.Oracle, rows));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
        config.Optionen.PrognoseArt = PrognoseArt.Oracle;
        config.Optionen.PlanungshorizontIntervalle = 192;
        config.Optionen.NeuplanungAlleIntervalle = 96;
        var watch = Stopwatch.StartNew();

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(input, config, new OrToolsFlottenPlaner());
        watch.Stop();

        _output.WriteLine($"{intervalle} Oracle intervals, horizon 192/replan 96: {watch.Elapsed.TotalSeconds:F3} s");
        Assert.Equal(intervalle, result.Variante.Intervalle.Count);
        Assert.Equal(0, result.Variante.PlanFallbackIntervalle);
        Assert.All(result.Variante.Intervalle, x => Assert.Equal("oracle-smoke", x.PrognoseId));
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    [Fact(Skip = "Manueller Lauf: der echte 35040-Intervall-Benchmark wurde nach dem 60-s-Limit abgebrochen.")]
    [Trait("Category", "LongRunning")]
    public void OracleJahrPlantMitHorizont192UndNeuplanung96()
    {
        FlottenNetzintervall[] rows = Reihen(35040, i => Zeile(i, last: 5, pv: 5, bezug: 0.2));
        FlottenEingang input = Eingang(rows);
        input.Prognosen.Add(new FlottenPrognoseSnapshot(
            "oracle-volljahr", Start.AddYears(1), Start, PrognoseArt.Oracle, rows));
        FlottenStudieKonfiguration config = Konfiguration(FlottenBetriebsziel.Arbitrage);
        config.Optionen.PrognoseArt = PrognoseArt.Oracle;
        config.Optionen.PlanungshorizontIntervalle = 192;
        config.Optionen.NeuplanungAlleIntervalle = 96;
        var watch = Stopwatch.StartNew();

        FlottenStudienErgebnis result = FlottenSimulator.Simuliere(input, config, new OrToolsFlottenPlaner());
        watch.Stop();

        _output.WriteLine($"35040 Oracle intervals, horizon 192/replan 96: {watch.Elapsed.TotalSeconds:F3} s");
        Assert.Equal(35040, result.Variante.Intervalle.Count);
        Assert.Equal(0, result.Variante.PlanFallbackIntervalle);
        Assert.All(result.Variante.Intervalle, x => Assert.Equal("oracle-volljahr", x.PrognoseId));
        PruefeKeineGegenlaeufigeFlotte(result.Variante);
    }

    private static readonly DateTimeOffset Start =
        new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static FlottenStudieKonfiguration Konfiguration(FlottenBetriebsziel ziel)
        => new()
        {
            Einheiten = new List<FlottenEinheit>
            {
                Einheit("klein", 10, 4, 0.4),
                Einheit("gross", 30, 12, 0.6)
            },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = ziel,
                ErzeugerPrioritaet = FlottenErzeugerPrioritaet.PvVorBhkw,
                Endbedingung = FlottenEndbedingung.JeSpeicherWieAnfang,
                EnergieAusgleichEuroProKWh = 0,
                PlanungshorizontIntervalle = 192,
                NeuplanungAlleIntervalle = 96
            }
        };

    private static FlottenEinheit Einheit(string id, double kapazitaet, double leistung, double socStart)
        => new()
        {
            Id = id,
            Name = id,
            KapazitaetKWh = kapazitaet,
            LadeleistungKw = leistung,
            EntladeleistungKw = leistung,
            Ladewirkungsgrad = 1,
            Entladewirkungsgrad = 1,
            SocMin = 0,
            SocMax = 1,
            SocStart = socStart
        };

    private static FlottenEingang Eingang(IReadOnlyList<FlottenNetzintervall> rows)
        => new()
        {
            KonfigurationId = "integration",
            DatenId = "synthetisch",
            Istwerte = rows.ToList()
        };

    private static FlottenEingang EingangMitPrognose(
        IReadOnlyList<FlottenNetzintervall> rows,
        PrognoseArt art)
    {
        FlottenEingang input = Eingang(rows);
        input.Prognosen.Add(new FlottenPrognoseSnapshot(
            "prognose", Start.AddMinutes(-15), Start, art, rows));
        return input;
    }

    private static FlottenNetzintervall[] Reihen(
        int anzahl,
        Func<int, FlottenNetzintervall> factory)
        => Enumerable.Range(0, anzahl).Select(factory).ToArray();

    private static FlottenNetzintervall Zeile(
        int viertelstunde,
        double last = 0,
        double pv = 0,
        double bhkw = 0,
        double bezug = 0.3,
        double pvPreis = 0.08,
        double bhkwPreis = 0.12,
        double batteriePreis = 0.05)
        => new()
        {
            Zeitstempel = Start.AddMinutes(15L * viertelstunde),
            LastKw = last,
            PvKw = pv,
            BhkwKw = bhkw,
            BezugspreisEuroProKWh = bezug,
            PvVerkaufspreisEuroProKWh = pvPreis,
            BhkwVerkaufspreisEuroProKWh = bhkwPreis,
            BatterieVerkaufspreisEuroProKWh = batteriePreis
        };

    private static void PruefeKeineGegenlaeufigeFlotte(FlottenSimulationErgebnis result)
    {
        Assert.All(result.Intervalle, x =>
        {
            bool laden = x.IstleistungKwJeSpeicher.Any(p => p < -1e-7);
            bool entladen = x.IstleistungKwJeSpeicher.Any(p => p > 1e-7);
            Assert.False(laden && entladen);
        });
    }
}
