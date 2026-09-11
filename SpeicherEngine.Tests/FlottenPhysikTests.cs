using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

public sealed class FlottenPhysikTests
{
    [Fact]
    public void Kapazitaetsverteilung_VerteiltRestNachBegrenzungNeu()
    {
        var config = Config(Einheit("A", 100, 5), Einheit("B", 100, 100));
        var result = FlottenSimulator.Simuliere(Input(Row(0, load: 40)), config);
        var p = result.Variante.Intervalle[0].IstleistungKwJeSpeicher;

        Assert.Equal(5, p[0], 8);
        Assert.Equal(35, p[1], 8);
        Assert.Equal(0, result.Variante.Intervalle[0].NetzbezugKw, 8);
        Assert.Equal(48.75, result.Variante.Intervalle[0].EnergieEndeKWhJeSpeicher[0], 8);
        Assert.Equal(41.25, result.Variante.Intervalle[0].EnergieEndeKWhJeSpeicher[1], 8);
    }

    [Fact]
    public void AcWirkungsgrad_WirdNurEinmalInEnergieUndVerlustBilanzVerwendet()
    {
        var b = Einheit("A", 100, 10);
        b.Entladewirkungsgrad = 0.8;
        var x = FlottenSimulator.Simuliere(Input(Row(0, load: 4)), Config(b)).Variante.Intervalle[0];

        Assert.Equal(4, x.IstleistungKwJeSpeicher[0], 8);
        Assert.Equal(48.75, x.EnergieEndeKWhJeSpeicher[0], 8);
        Assert.Equal(0.25, x.UmwandlungsverlustKWhJeSpeicher[0], 8);
        Assert.Equal(0, x.NetzbezugKw, 8);
    }

    [Fact]
    public void PeakReserve_WirdBeiEchterSpitzeFreigegeben_RestverletzungBleibtSichtbar()
    {
        var b = Einheit("A", 10, 100);
        b.SocMin = 0.1;
        b.SocStart = 0.3;
        b.PeakReserveKWh = 2;
        var config = Config(b);
        config.Optionen.Betriebsziel = FlottenBetriebsziel.PeakShaving;
        config.Optionen.WirtschaftlicherPeakZielwertKw = 10;
        config.Optionen.NetzbezugGrenzeKw = 11;

        var result = FlottenSimulator.Simuliere(Input(Row(0, load: 20)), config).Variante;

        Assert.Equal(8, result.Intervalle[0].IstleistungKwJeSpeicher[0], 8);
        Assert.Equal(12, result.Intervalle[0].NetzbezugKw, 8);
        Assert.Equal(2, result.Intervalle[0].WirtschaftlichePeakverletzungKw, 8);
        Assert.Equal(1, result.Intervalle[0].TechnischeImportverletzungKw, 8);
        Assert.False(result.Zulaessig);
    }

    [Fact]
    public void Ausfallfaktor_BegrenztEinheitUndVerteiltAufVerfuegbareFlotteNeu()
    {
        var config = Config(Einheit("A", 100, 20), Einheit("B", 100, 20));
        var input = Input(Row(0, load: 20));
        input.VerfuegbarkeitsfaktorNachEinheitId["A"] = new List<double> { 0 };
        var result = FlottenSimulator.Simuliere(input, config).Variante.Intervalle[0];

        Assert.Equal(0, result.IstleistungKwJeSpeicher[0], 8);
        Assert.Equal(20, result.IstleistungKwJeSpeicher[1], 8);
        Assert.Equal(0, result.NetzbezugKw, 8);
    }

    [Fact]
    public void PvWirdNurAlsUeberschussAbgeregelt_UndExportNachQuelleBilanziert()
    {
        var config = Config();
        config.Optionen.NetzeinspeisungGrenzeKw = 6;
        var result = FlottenSimulator.Simuliere(Input(Row(0, load: 5, pv: 20)), config).Variante.Intervalle[0];

        Assert.Equal(9, result.PvAbregelungKw, 8);
        Assert.Equal(6, result.NetzeinspeisungKw, 8);
        Assert.Equal(6, result.PvNetzeinspeisungKw, 8);
        Assert.Equal(0, result.BatterieNetzeinspeisungKw, 8);
    }

    [Fact]
    public void NichtAbregelbaresBhkw_UeberschreitetExportgrenzeOhneVerstecktenEingriff()
    {
        var config = Config();
        config.Optionen.NetzeinspeisungGrenzeKw = 2;
        var result = FlottenSimulator.Simuliere(Input(Row(0, bhkw: 10)), config).Variante;

        Assert.Equal(10, result.Intervalle[0].BhkwNetzeinspeisungKw, 8);
        Assert.Equal(8, result.Intervalle[0].TechnischeExportverletzungKw, 8);
        Assert.False(result.Zulaessig);
    }

    [Fact]
    public void PvFirst_RegeltKeineLastdeckendePvWegenBhkwExportAb()
    {
        var config = Config();
        config.Optionen.NetzeinspeisungGrenzeKw = 2;
        var result = FlottenSimulator.Simuliere(Input(Row(0, load: 5, pv: 5, bhkw: 10)), config)
            .Variante.Intervalle[0];

        Assert.Equal(0, result.PvAbregelungKw, 8);
        Assert.Equal(5, result.PvVerfuegbarKw, 8);
        Assert.Equal(10, result.BhkwNetzeinspeisungKw, 8);
        Assert.Equal(8, result.TechnischeExportverletzungKw, 8);
    }

    [Fact]
    public void Erzeugerprioritaet_IstDeterministischUndTarifeBleibenGetrennt()
    {
        var config = Config();
        config.Optionen.ErzeugerPrioritaet = FlottenErzeugerPrioritaet.PvVorBhkw;
        var input = Input(Row(0, load: 4, pv: 5, bhkw: 5));
        var study = FlottenSimulator.Simuliere(input, config);
        var x = study.Variante.Intervalle[0];

        Assert.Equal(1, x.PvNetzeinspeisungKw, 8);
        Assert.Equal(5, x.BhkwNetzeinspeisungKw, 8);
        Assert.Equal(-0.25 * (1 * 0.08 + 5 * 0.12), study.Variantenrechnung.EnergiekostenEuro, 8);
    }

    [Fact]
    public void PrognoseSnapshot_IstGegenAenderungDerUrsprungslisteGeschuetzt()
    {
        var source = new List<FlottenNetzintervall> { Row(0, load: 3) };
        var snapshot = new FlottenPrognoseSnapshot("P", T(0), T(0), PrognoseArt.VerifiziertBekannt, source);
        source[0].LastKw = 99;
        source.Add(Row(1));

        Assert.Single(snapshot.Intervalle);
        Assert.Equal(3, snapshot.Intervalle[0].LastKw);
    }

    [Fact]
    public void PrognoseSnapshot_IstMitSystemTextJsonRundlaufFaehig()
    {
        var snapshot = new FlottenPrognoseSnapshot("P", T(0), T(0), PrognoseArt.VerifiziertBekannt,
            new[] { Row(0, load: 3) });
        var json = JsonSerializer.Serialize(snapshot);
        var copy = JsonSerializer.Deserialize<FlottenPrognoseSnapshot>(json);

        Assert.NotNull(copy);
        Assert.Equal("P", copy.Id);
        Assert.Equal(3, copy.Intervalle[0].LastKw);
    }

    [Fact]
    public void Standardkonfiguration_EnthaeltKeineNichtJsonFaehigeUnendlichkeit()
    {
        var json = JsonSerializer.Serialize(new FlottenStudieKonfiguration());
        var copy = JsonSerializer.Deserialize<FlottenStudieKonfiguration>(json);

        Assert.NotNull(copy);
        Assert.Null(copy.Optionen.NetzbezugGrenzeKw);
        Assert.Null(copy.Optionen.ArbitrageEntladepreisSchwelle);
    }

    [Fact]
    public void PlanungOhneExplizitenSnapshot_IstKeinStillerGreedyLauf()
    {
        var config = Config(Einheit("A", 10, 5));
        config.Optionen.Betriebsziel = FlottenBetriebsziel.PvPlanung;
        config.Optionen.PrognoseFallbackErlaubt = false;

        Assert.Throws<InvalidOperationException>(() =>
            FlottenSimulator.Simuliere(Input(Row(0, load: 2)), config, new NullPlaner()));
    }

    [Fact]
    public void RollierendePlanung_PlantAusDemFortgeschriebenenIstzustandNeu()
    {
        var config = Config(Einheit("A", 10, 100));
        config.Optionen.Betriebsziel = FlottenBetriebsziel.Arbitrage;
        config.Optionen.NeuplanungAlleIntervalle = 1;
        config.Optionen.PlanungshorizontIntervalle = 2;
        var input = Input(Row(0, load: 100), Row(1, load: 100));
        input.Prognosen.Add(new FlottenPrognoseSnapshot("P", T(0), T(0),
            PrognoseArt.VerifiziertBekannt, input.Istwerte));
        var planner = new RecordingPlaner();

        var result = FlottenSimulator.Simuliere(input, config, planner).Variante;

        Assert.Equal(2, planner.Energien.Count);
        Assert.Equal(5, planner.Energien[0], 8);
        Assert.Equal(0, planner.Energien[1], 8);
        Assert.Equal(20, result.Intervalle[0].IstleistungKwJeSpeicher[0], 8);
        Assert.Equal(0, result.Intervalle[1].IstleistungKwJeSpeicher[0], 8);
    }

    [Fact]
    public void UngleicheEndenergie_BenoetigtExplizitenAusgleichswert()
    {
        var config = Config(Einheit("A", 10, 5));
        config.Optionen.EnergieAusgleichEuroProKWh = null;

        Assert.Throws<ArgumentException>(() => FlottenSimulator.Simuliere(Input(Row(0, load: 2)), config));
    }

    private sealed class NullPlaner : IFlottenPlaner
    {
        public FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken) => new();
    }

    private sealed class RecordingPlaner : IFlottenPlaner
    {
        public List<double> Energien { get; } = new();

        public FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken)
        {
            Energien.Add(anfrage.EnergieKWh[0]);
            var plan = new FlottenPlan
            {
                PlanId = "Plan-" + Energien.Count,
                PrognoseId = anfrage.Prognose.Id,
                Entscheidungszeitpunkt = anfrage.Entscheidungszeitpunkt,
                Status = FlottenPlanStatus.Optimal
            };
            foreach (var row in anfrage.Prognose.Intervalle)
                plan.Intervalle.Add(new FlottenPlanIntervall
                {
                    Zeitstempel = row.Zeitstempel,
                    LadeleistungKwJeSpeicher = new List<double> { 0 },
                    EntladeleistungKwJeSpeicher = new List<double> { 100 }
                });
            return plan;
        }
    }

    private static FlottenStudieKonfiguration Config(params FlottenEinheit[] units) => new()
    {
        Einheiten = new List<FlottenEinheit>(units),
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PvGreedy,
            EnergieAusgleichEuroProKWh = 0
        }
    };

    private static FlottenEinheit Einheit(string id, double capacity, double power) => new()
    {
        Id = id,
        Name = id,
        KapazitaetKWh = capacity,
        LadeleistungKw = power,
        EntladeleistungKw = power,
        Ladewirkungsgrad = 1,
        Entladewirkungsgrad = 1,
        SocMin = 0,
        SocMax = 1,
        SocStart = 0.5
    };

    private static FlottenEingang Input(params FlottenNetzintervall[] rows) => new()
    {
        KonfigurationId = "C",
        DatenId = "D",
        Istwerte = new List<FlottenNetzintervall>(rows)
    };

    private static FlottenNetzintervall Row(int quarter, double load = 0, double pv = 0, double bhkw = 0) => new()
    {
        Zeitstempel = T(quarter),
        LastKw = load,
        PvKw = pv,
        BhkwKw = bhkw,
        BezugspreisEuroProKWh = 0.3,
        PvVerkaufspreisEuroProKWh = 0.08,
        BhkwVerkaufspreisEuroProKWh = 0.12,
        BatterieVerkaufspreisEuroProKWh = 0.05
    };

    private static DateTimeOffset T(int quarter) =>
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * quarter);
}
