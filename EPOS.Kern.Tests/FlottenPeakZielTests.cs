using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID SD‑Q3 (11.09.2026, „Empfehlung"): Die feste Vorbelegung 50 kW
    /// fällt. Das Peak-Ziel wird aus der Referenz hergeleitet (H₀) und lässt sich per
    /// Bisektion bestimmen.
    ///
    /// <para><b>Die Lage im Befund SP‑O‑10.</b> Grundlast 70…80 kW, Spitze 789 kW,
    /// Vorbelegung 50 kW: Die Anforderung <c>n − H</c> war in jedem Intervall positiv,
    /// der Ladedeckel <c>max(0, H − n)</c> dauerhaft 0 — die Flotte konnte nichts tun.</para>
    ///
    /// <para><b>Was hier geprüft wird.</b> (a) die Formel H₀ an einer kleinen Reihe,
    /// (b) der benannte Rückfall ohne Zeitreihe, (c) die Bisektion — sie konvergiert, sie
    /// bleibt unter zwölf Jahresläufen, sie meldet Fortschritt und sie bricht ab,
    /// (d) planende Ziele werden benannt abgewiesen.</para>
    /// </summary>
    public sealed class FlottenPeakZielTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public FlottenPeakZielTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        // ================================================================= H₀

        [Fact]
        public void H0_NimmtDieSpitzeMinusEntladeleistung_WennSieUeberDenTagesminimaLiegt()
        {
            double[] netto = { 100, 20, 100, 30 };

            FlottenPeakZielVorschlag v = FlottenPeakZiel.Vorschlag(netto, 2, 50);

            Assert.True(v.AusReihe);
            Assert.Equal(100, v.ReferenzspitzeKw, 10);
            Assert.Equal(30, v.TagesminimumMaxKw, 10);
            Assert.Equal(50, v.PeakZielKw, 10);
            Assert.Contains("H₀", v.Herleitung);
        }

        [Fact]
        public void H0_FaelltNieUnterDasMaximumDerTagesminima()
        {
            double[] netto = { 100, 20, 100, 30 };

            FlottenPeakZielVorschlag v = FlottenPeakZiel.Vorschlag(netto, 2, 90);

            Assert.Equal(30, v.PeakZielKw, 10);
        }

        [Fact]
        public void H0_AusDerReihe_RechnetDenHilfsverbrauchDerFlotteInDieNettolast()
        {
            FlottenStudieKonfiguration f = Flotte(entladeleistung: 8, hilfsverbrauch: 1);
            List<FlottenNetzintervall> reihe = Reihe(20, 4, 20, 4);

            FlottenPeakZielVorschlag v = FlottenPeakZiel.Vorschlag(reihe, f);

            Assert.Equal(21, v.ReferenzspitzeKw, 10);
            Assert.Equal(5, v.TagesminimumMaxKw, 10);
            Assert.Equal(13, v.PeakZielKw, 10);
        }

        [Fact]
        public void OhneZeitreihe_AberMitBezugsspitze_GiltDerAnteilsRueckfall()
        {
            FlottenPeakZielVorschlag v = FlottenPeakZiel.Rueckfall(100, 95);

            Assert.False(v.AusReihe);
            Assert.Equal(100 * FlottenPeakZiel.GrundlastAnteilImRueckfall, v.PeakZielKw, 10);
        }

        [Fact]
        public void OhneZeitreihe_UndOhneBezugsspitze_GiltDerBenannteRueckfallwert()
        {
            FlottenPeakZielVorschlag v = FlottenPeakZiel.Rueckfall(0, 10);

            Assert.False(v.AusReihe);
            Assert.Equal(FlottenPeakZiel.RueckfallPeakZielKw, v.PeakZielKw, 10);
        }

        // ================================================================= Bisektion

        [Fact]
        public void Bisektion_FindetDasKleinsteGehalteneZiel_UndKonvergiert()
        {
            // Last 20/4 kW im Wechsel, Entladeleistung 8 kW: Unter 12 kW kommt die Flotte
            // nicht, weil die Spitze 20 kW nur um 8 kW gekappt werden kann.
            var fortschritt = new List<FlottenPeakZielFortschritt>();

            FlottenPeakZielErgebnis e = FlottenPeakZiel.PeakZielBestimmen(
                Eingang(Reihe(20, 4, 20, 4, 20, 4, 20, 4)), Flotte(entladeleistung: 8),
                null, new Fortschrittssammler(fortschritt), CancellationToken.None);

            Assert.True(e.Konvergiert);
            Assert.InRange(e.Laeufe, 1, FlottenPeakZiel.HoechsteLaeufe);
            Assert.Equal(20, e.ReferenzspitzeKw, 10);
            Assert.Equal(4, e.GrundlastKw, 10);
            Assert.Equal(12, e.PeakZielKw, 1);
            Assert.Equal(12, e.VerbleibendeSpitzeKw, 1);
            Assert.Equal(e.Laeufe, fortschritt.Count);
            Assert.Equal(FlottenPeakZiel.HoechsteLaeufe, fortschritt[0].HoechsteLaeufe);
            Assert.Contains("Bisektion", e.Herleitung);
        }

        [Fact]
        public void Bisektion_LaeuftNieUeberDieHoechstzahlDerJahreslaeufe()
        {
            FlottenPeakZielErgebnis e = FlottenPeakZiel.PeakZielBestimmen(
                Eingang(Reihe(1000, 1, 1000, 1)), Flotte(entladeleistung: 3),
                null, null, CancellationToken.None);

            Assert.True(e.Laeufe <= FlottenPeakZiel.HoechsteLaeufe);
        }

        [Fact]
        public void Bisektion_BrichtAufDieAbbruchmarkeHinAb()
        {
            using var quelle = new CancellationTokenSource();
            quelle.Cancel();

            Assert.Throws<OperationCanceledException>(() => FlottenPeakZiel.PeakZielBestimmen(
                Eingang(Reihe(20, 4, 20, 4)), Flotte(entladeleistung: 8), null, null, quelle.Token));
        }

        [Fact]
        public void Bisektion_OhneSpitzeUeberDerGrundlast_RechnetKeinenEinzigenLauf()
        {
            FlottenPeakZielErgebnis e = FlottenPeakZiel.PeakZielBestimmen(
                Eingang(Reihe(10, 10, 10, 10)), Flotte(entladeleistung: 8),
                null, null, CancellationToken.None);

            Assert.Equal(0, e.Laeufe);
            Assert.Equal(10, e.PeakZielKw, 10);
        }

        [Fact]
        public void Bisektion_WeistPlanendeBetriebszieleBenanntAb()
        {
            FlottenStudieKonfiguration f = Flotte(entladeleistung: 8);
            f.Optionen.Betriebsziel = FlottenBetriebsziel.Arbitrage;

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => FlottenPeakZiel.PeakZielBestimmen(Eingang(Reihe(20, 4)), f, null, null,
                    CancellationToken.None));

            Assert.Contains("reaktiven", ex.Message);
            Assert.Contains("Fahrplaner", ex.Message);
        }

        [Fact]
        public void Bisektion_OhneSpeicherUndOhneReihe_MeldetDenFehlendenAusgangspunkt()
        {
            FlottenStudieKonfiguration leer = Flotte(entladeleistung: 8);
            leer.Einheiten.Clear();

            Assert.Contains("physischen Speicher", Assert.Throws<InvalidOperationException>(
                () => FlottenPeakZiel.PeakZielBestimmen(Eingang(Reihe(20, 4)), leer, null, null,
                    CancellationToken.None)).Message);
            Assert.Contains("Standortzeitreihe", Assert.Throws<InvalidOperationException>(
                () => FlottenPeakZiel.PeakZielBestimmen(new FlottenEingang(),
                    Flotte(entladeleistung: 8), null, null, CancellationToken.None)).Message);
        }

        // ================================================================= Prüfstand

        private sealed class Fortschrittssammler : IProgress<FlottenPeakZielFortschritt>
        {
            private readonly List<FlottenPeakZielFortschritt> _ziel;
            public Fortschrittssammler(List<FlottenPeakZielFortschritt> ziel) => _ziel = ziel;
            public void Report(FlottenPeakZielFortschritt wert) => _ziel.Add(wert);
        }

        private static FlottenEingang Eingang(List<FlottenNetzintervall> reihe) => new()
        {
            KonfigurationId = "C",
            DatenId = "D",
            Istwerte = reihe
        };

        private static List<FlottenNetzintervall> Reihe(params double[] lasten) =>
            lasten.Select((last, t) => new FlottenNetzintervall
            {
                Zeitstempel = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * t),
                LastKw = last,
                BezugspreisEuroProKWh = 0.3,
                PvVerkaufspreisEuroProKWh = 0.08,
                BhkwVerkaufspreisEuroProKWh = 0.12,
                BatterieVerkaufspreisEuroProKWh = 0.05
            }).ToList();

        private static FlottenStudieKonfiguration Flotte(double entladeleistung, double hilfsverbrauch = 0) => new()
        {
            Einheiten = new List<FlottenEinheit>
            {
                new FlottenEinheit
                {
                    Id = "A",
                    Name = "Speicher A",
                    KapazitaetKWh = 100,
                    LadeleistungKw = entladeleistung,
                    EntladeleistungKw = entladeleistung,
                    Ladewirkungsgrad = 1,
                    Entladewirkungsgrad = 1,
                    SocMin = 0,
                    SocMax = 1,
                    SocStart = 0.5,
                    HilfsverbrauchKw = hilfsverbrauch
                }
            },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                NetzladungErlaubt = true
            }
        };
    }
}
