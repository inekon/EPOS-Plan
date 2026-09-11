using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherPlanung.Tests;

public sealed class OrToolsFlottenPlanerTests
{
    private const double Genauigkeit = 1e-5;

    [Fact]
    public void PlantZweiSpeicherMitJeweiligenLeistungsgrenzenUndEnergiebilanz()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[]
            {
                Zeile(last: 0, bezugspreis: 0),
                Zeile(last: 10, bezugspreis: 1)
            },
            Batterie("klein", 2),
            Batterie("gross", 8));
        anfrage.EnergieKWh.AddRange(new[] { 5d, 5d });
        anfrage.Optionen.NetzladungErlaubt = true;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].LadeleistungKwJeSpeicher[0], 2 - Genauigkeit, 2 + Genauigkeit);
        Assert.InRange(plan.Intervalle[0].LadeleistungKwJeSpeicher[1], 8 - Genauigkeit, 8 + Genauigkeit);
        Assert.InRange(plan.Intervalle[1].EntladeleistungKwJeSpeicher[0], 2 - Genauigkeit, 2 + Genauigkeit);
        Assert.InRange(plan.Intervalle[1].EntladeleistungKwJeSpeicher[1], 8 - Genauigkeit, 8 + Genauigkeit);
    }

    [Fact]
    public void RichtungsvariablenVerhindernNegativpreisKreislauf()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 5, pv: 5, bezugspreis: -1, pvPreis: 1, batteriepreis: 1) },
            Batterie("B", 10));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.NetzladungErlaubt = true;
        anfrage.Optionen.BatterieexportErlaubt = true;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.All(plan.Intervalle[0].LadeleistungKwJeSpeicher, x => Assert.InRange(x, 0, Genauigkeit));
        Assert.All(plan.Intervalle[0].EntladeleistungKwJeSpeicher, x => Assert.InRange(x, 0, Genauigkeit));
        Assert.InRange(plan.ZielfunktionswertEuro, -Genauigkeit, Genauigkeit);
    }

    [Fact]
    public void PvPlanungLaedtBeiGleichemNutzenSoSpaetWieMoeglich()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.PvPlanung,
            new[]
            {
                Zeile(last: 0, pv: 4),
                Zeile(last: 0, pv: 4),
                Zeile(last: 1, pv: 0)
            },
            Batterie("B", 4, kapazitaet: 4));
        anfrage.EnergieKWh.Add(0);

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].LadeleistungKwJeSpeicher[0], 0, Genauigkeit);
        Assert.InRange(plan.Intervalle[1].LadeleistungKwJeSpeicher[0], 1 - Genauigkeit, 1 + Genauigkeit);
        Assert.InRange(plan.Intervalle[2].EntladeleistungKwJeSpeicher[0], 1 - Genauigkeit, 1 + Genauigkeit);
        Assert.All(plan.Intervalle, x => Assert.InRange(x.PvAbregelungKw, 0, Genauigkeit));
    }

    [Fact]
    public void MultiUseMinimiertPeakVorEnergiekosten()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.MultiUse,
            new[]
            {
                Zeile(last: 10, bezugspreis: 0),
                Zeile(last: 0, bezugspreis: 100)
            },
            Batterie("B", 5));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.NetzladungErlaubt = true;
        anfrage.Optionen.WirtschaftlicherPeakZielwertKw = 5;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].EntladeleistungKwJeSpeicher[0], 5 - Genauigkeit, 5 + Genauigkeit);
        Assert.InRange(plan.Intervalle[1].LadeleistungKwJeSpeicher[0], 5 - Genauigkeit, 5 + Genauigkeit);
    }

    [Fact]
    public void VerfuegbarkeitBegrenztNurDasBetroffeneIntervall()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[]
            {
                Zeile(last: 4, bezugspreis: 1),
                Zeile(last: 0, bezugspreis: 0)
            },
            Batterie("B", 4));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.NetzladungErlaubt = true;
        anfrage.VerfuegbarkeitsfaktorJeSpeicher.Add(new List<double> { 0, 1 });

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].EntladeleistungKwJeSpeicher[0], 0, Genauigkeit);
        Assert.InRange(plan.Intervalle[0].LadeleistungKwJeSpeicher[0], 0, Genauigkeit);
    }

    [Fact]
    public void HarteNetzgrenzeWirdAlsUnzulaessigGemeldet()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 10) },
            Batterie("B", 0));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.NetzbezugGrenzeKw = 5;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Unzulaessig, plan.Status);
        Assert.Empty(plan.Intervalle);
        Assert.Contains("Anschlussgrenzen", plan.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void HarteExportgrenzeLaesstKeineVerdeckteBhkwAbregelungZu()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(bhkw: 10) },
            Batterie("B", 0));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.NetzeinspeisungGrenzeKw = 5;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Unzulaessig, plan.Status);
        Assert.Empty(plan.Intervalle);
    }

    [Fact]
    public void EntladewirkungsgradBegrenztAcAbgabeOhneEnergieZuErzeugen()
    {
        FlottenEinheit batterie = Batterie("B", 10, kapazitaet: 2);
        batterie.Entladewirkungsgrad = 0.9;
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 3.6, bezugspreis: 1) },
            batterie);
        anfrage.EnergieKWh.Add(1);
        anfrage.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].EntladeleistungKwJeSpeicher[0], 3.6 - Genauigkeit, 3.6 + Genauigkeit);
    }

    [Fact]
    public void PrioritaetVerhindertTarifgetriebeneUmdeklarationVonPvExport()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 5, pv: 5, bhkw: 5, pvPreis: 10, bhkwPreis: 0) },
            Batterie("B", 0));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.ErzeugerPrioritaet = FlottenErzeugerPrioritaet.PvVorBhkw;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.ZielfunktionswertEuro, -Genauigkeit, Genauigkeit);
    }

    [Fact]
    public void EndenergieAusgleichVerhindertKostenloseHorizontentleerung()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 4, bezugspreis: 1) },
            Batterie("B", 4));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
        anfrage.EndenergieAusgleichEuroProKWh = 2;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].EntladeleistungKwJeSpeicher[0], 0, Genauigkeit);
    }

    [Fact]
    public void BatterieentladungDeklariertPvNichtFaelschlichZumHoeherVerguetetenExport()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 5, pv: 5, pvPreis: 10, batteriepreis: 0) },
            Batterie("B", 5));
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
        anfrage.Optionen.BatterieexportErlaubt = true;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.ZielfunktionswertEuro, -Genauigkeit, Genauigkeit);
    }

    [Fact]
    public void PlanungSchuetztPeakReserveFuerDieAusfuehrungsregel()
    {
        FlottenEinheit batterie = Batterie("B", 5, kapazitaet: 10);
        batterie.PeakReserveKWh = 5;
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.MultiUse,
            new[] { Zeile(last: 5, bezugspreis: 1) },
            batterie);
        anfrage.EnergieKWh.Add(5);
        anfrage.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
        anfrage.Optionen.WirtschaftlicherPeakZielwertKw = 0;

        FlottenPlan plan = new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None);

        Assert.Equal(FlottenPlanStatus.Optimal, plan.Status);
        Assert.InRange(plan.Intervalle[0].EntladeleistungKwJeSpeicher[0], 0, Genauigkeit);
    }

    [Fact]
    public void NichtPositivesZeitlimitWirdVorDemSolverAbgewiesen()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 1) },
            Batterie("B", 1));
        anfrage.EnergieKWh.Add(5);
        anfrage.Zeitlimit = TimeSpan.Zero;

        Assert.Throws<ArgumentException>(() =>
            new OrToolsFlottenPlaner().Plane(anfrage, CancellationToken.None));
    }

    [Fact]
    public void VorabAbgebrochenePlanungWirftOperationCanceled()
    {
        FlottenPlanAnfrage anfrage = Anfrage(
            FlottenPlanerModus.Arbitrage,
            new[] { Zeile(last: 1) },
            Batterie("B", 1));
        anfrage.EnergieKWh.Add(5);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new OrToolsFlottenPlaner().Plane(anfrage, cancellation.Token));
    }

    private static FlottenPlanAnfrage Anfrage(
        FlottenPlanerModus modus,
        IReadOnlyList<FlottenNetzintervall> zeilen,
        params FlottenEinheit[] batterien)
    {
        DateTimeOffset entscheidung = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var datiert = zeilen.Select((x, i) => new FlottenNetzintervall
        {
            Zeitstempel = entscheidung.AddMinutes(15 * i),
            LastKw = x.LastKw,
            PvKw = x.PvKw,
            BhkwKw = x.BhkwKw,
            BezugspreisEuroProKWh = x.BezugspreisEuroProKWh,
            PvVerkaufspreisEuroProKWh = x.PvVerkaufspreisEuroProKWh,
            BhkwVerkaufspreisEuroProKWh = x.BhkwVerkaufspreisEuroProKWh,
            BatterieVerkaufspreisEuroProKWh = x.BatterieVerkaufspreisEuroProKWh
        }).ToArray();
        return new FlottenPlanAnfrage
        {
            Entscheidungszeitpunkt = entscheidung,
            Modus = modus,
            Prognose = new FlottenPrognoseSnapshot(
                "prognose-1", entscheidung.AddMinutes(-5), entscheidung,
                PrognoseArt.VerifiziertBekannt, datiert),
            Einheiten = batterien.ToList(),
            Optionen = new FlottenSimulationOptionen
            {
                Endbedingung = FlottenEndbedingung.JeSpeicherWieAnfang
            },
            Zeitlimit = TimeSpan.FromSeconds(10)
        };
    }

    private static FlottenEinheit Batterie(string id, double leistung, double kapazitaet = 20)
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
            SocMax = 1
        };

    private static FlottenNetzintervall Zeile(
        double last = 0,
        double pv = 0,
        double bhkw = 0,
        double bezugspreis = 0,
        double pvPreis = 0,
        double bhkwPreis = 0,
        double batteriepreis = 0)
        => new()
        {
            LastKw = last,
            PvKw = pv,
            BhkwKw = bhkw,
            BezugspreisEuroProKWh = bezugspreis,
            PvVerkaufspreisEuroProKWh = pvPreis,
            BhkwVerkaufspreisEuroProKWh = bhkwPreis,
            BatterieVerkaufspreisEuroProKWh = batteriepreis
        };
}
