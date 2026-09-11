using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

public sealed class SpeicherFlottenNetzbilanzTests
{
    [Fact]
    public void Projektadapter_Bilanziert_Tagesimport_und_Exporte_aus_drei_Quellen_getrennt()
    {
        DateTimeOffset start = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var istwerte = Enumerable.Range(0, 96)
            .Select(i => Zeile(start.AddMinutes(15 * i))).ToList();
        istwerte[0].LastKw = 12;
        istwerte[1].PvKw = 20;
        istwerte[1].BhkwKw = 4;

        var eingang = new FlottenEingang
        {
            KonfigurationId = "netzbilanz-konfiguration",
            DatenId = "netzbilanz-tag",
            Istwerte = istwerte,
            Prognosen = new List<FlottenPrognoseSnapshot>
            {
                new("oracle-tag", start, start, PrognoseArt.Oracle, istwerte)
            }
        };
        FlottenStudieKonfiguration konfiguration = Konfiguration();

        SpeicherFlottenProjektLauf lauf = SpeicherFlottenProjektCtrl.Rechnen(
            eingang, konfiguration, null, new TagesPlaner(start));
        SpeicherFlottenNetzbilanz bilanz = StromspeicherSimCtrl.ProjektNetzbilanz(lauf.Studie);

        Assert.True(lauf.Studie.Variante.Zulaessig);
        Assert.Equal(96, bilanz.NetzbezugKw.Length);
        Assert.Equal(12, bilanz.NetzbezugKw[0], 8);

        // 24 kW PV+BHKW treffen auf 10 kW Exportgrenze: ausschließlich der
        // PV-Überschuss wird um 14 kW abgeregelt. Der verbleibende Direktexport
        // bleibt nach Quelle getrennt und wird nicht nochmals als PV ausgewiesen.
        Assert.Equal(10, bilanz.NetzeinspeisungKw[1], 8);
        Assert.Equal(6, bilanz.PvNetzeinspeisungKw[1], 8);
        Assert.Equal(4, bilanz.BhkwNetzeinspeisungKw[1], 8);
        Assert.Equal(0, bilanz.BatterieNetzeinspeisungKw[1], 8);
        Assert.Equal(14, bilanz.PvAbregelungKw[1], 8);

        // Beide physischen Speicher liefern im Folgeintervall je 2 kW ins Netz.
        Assert.Equal(new[] { 2d, 2d },
            lauf.Studie.Variante.Intervalle[2].IstleistungKwJeSpeicher);
        Assert.Equal(4, bilanz.NetzeinspeisungKw[2], 8);
        Assert.Equal(4, bilanz.BatterieNetzeinspeisungKw[2], 8);

        Assert.Equal(3, bilanz.NetzbezugKwh, 8);
        Assert.Equal(3.5, bilanz.NetzeinspeisungKwh, 8);
        Assert.Equal(1.5, bilanz.PvNetzeinspeisungKwh, 8);
        Assert.Equal(1, bilanz.BhkwNetzeinspeisungKwh, 8);
        Assert.Equal(1, bilanz.BatterieNetzeinspeisungKwh, 8);
        Assert.Equal(3.5, bilanz.PvAbregelungKwh, 8);
        Assert.Equal(bilanz.NetzeinspeisungKwh,
            bilanz.PvNetzeinspeisungKwh + bilanz.BhkwNetzeinspeisungKwh
            + bilanz.BatterieNetzeinspeisungKwh, 8);

        double saldiertesNetzKwh = lauf.Studie.Variante.Intervalle
            .Sum(x => x.NetzleistungKw) * .25;
        Assert.Equal(-.5, saldiertesNetzKwh, 8);
        Assert.NotEqual(saldiertesNetzKwh, bilanz.NetzbezugKwh);
        Assert.Equal(istwerte.Sum(x => x.PvKw) * .25,
            bilanz.PvNetzeinspeisungKwh + bilanz.PvAbregelungKwh, 8);
    }

    private static FlottenStudieKonfiguration Konfiguration() => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            Einheit("A", 20, 4),
            Einheit("B", 30, 6)
        },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PvPlanung,
            PrognoseArt = PrognoseArt.Oracle,
            PlanungshorizontIntervalle = 96,
            NeuplanungAlleIntervalle = 96,
            NetzeinspeisungGrenzeKw = 10,
            BatterieexportErlaubt = true,
            EnergieAusgleichEuroProKWh = 0
        }
    };

    private static FlottenEinheit Einheit(string id, double kapazitaet, double leistung) => new()
    {
        Id = id,
        Name = "Speicher " + id,
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = leistung,
        EntladeleistungKw = leistung,
        Ladewirkungsgrad = 1,
        Entladewirkungsgrad = 1,
        SocMin = 0,
        SocStart = .5,
        SocMax = 1
    };

    private static FlottenNetzintervall Zeile(DateTimeOffset zeit) => new()
    {
        Zeitstempel = zeit,
        BezugspreisEuroProKWh = .30,
        PvVerkaufspreisEuroProKWh = .08,
        BhkwVerkaufspreisEuroProKWh = .12,
        BatterieVerkaufspreisEuroProKWh = .05
    };

    private sealed class TagesPlaner : IFlottenPlaner
    {
        private readonly DateTimeOffset _batterieexport;

        public TagesPlaner(DateTimeOffset start) => _batterieexport = start.AddMinutes(30);

        public FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken)
        {
            var plan = new FlottenPlan
            {
                PlanId = "netzbilanz-plan",
                PrognoseId = anfrage.Prognose.Id,
                Entscheidungszeitpunkt = anfrage.Entscheidungszeitpunkt,
                Status = FlottenPlanStatus.Optimal
            };
            foreach (FlottenNetzintervall zeile in anfrage.Prognose.Intervalle)
            {
                bool export = zeile.Zeitstempel == _batterieexport;
                plan.Intervalle.Add(new FlottenPlanIntervall
                {
                    Zeitstempel = zeile.Zeitstempel,
                    LadeleistungKwJeSpeicher = new List<double> { 0, 0 },
                    EntladeleistungKwJeSpeicher = new List<double>
                        { export ? 2 : 0, export ? 2 : 0 }
                });
            }
            return plan;
        }
    }
}
