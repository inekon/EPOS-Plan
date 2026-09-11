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
    /// ANWENDERENTSCHEIDE SD‑Q4 und SD‑Q5 (11.09.2026, „Empfehlung") und Konzept
    /// Stromspeicher-Dialoge 2.4 Punkte 3, 4 und 6: die Vorprüfung einer Flottenstudie.
    ///
    /// <para><b>Was hier geprüft wird.</b> Die drei Hinweise aus 2.4 Punkt 3 je EINZELN —
    /// ein Peak-Ziel unter dem Maximum der Tagesminima bei verbotener Netzladung, ein
    /// Peak-Ziel über der Referenzspitze und ein jährlicher Betriebsaufwand unter einem
    /// Tausendstel der Investition —, dazu der Start-SoC-Hinweis (SD‑Q4), der Befund
    /// einer arbeitslosen Flotte und die Vorgabe „Netzladung" je Betriebsziel (SD‑Q5).</para>
    /// </summary>
    public sealed class FlottenPlausibilitaetTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public FlottenPlausibilitaetTests()
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

        // ============================================== 2.4 Punkt 3, die drei Hinweise

        [Fact]
        public void PeakZielUnterDemTagesminimum_IstEineWarnung_WennNetzladungVerbotenIst()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 10, netzladung: false);

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            FlottenHinweis h = Assert.Single(hinweise,
                x => x.Kennung == FlottenHinweisKennung.PeakZielUnterTagesminimum);
            Assert.Equal(FlottenHinweisStufe.Warnung, h.Stufe);
            // Vier Viertelstunden EINES Tages: das Tagesminimum ist 50 kW.
            Assert.Contains("50 kW", h.Text);
            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.PeakZielUeberReferenzspitze);
            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.BetriebskostenSehrNiedrig);
        }

        [Fact]
        public void MitFreigegebenerNetzladung_BleibtDerTagesminimumHinweisAus()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 10, netzladung: true);

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.PeakZielUnterTagesminimum);
        }

        [Fact]
        public void PeakZielUeberDerReferenzspitze_IstEinHinweisAufEineWirkungsloseKappung()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 500, netzladung: false);

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            FlottenHinweis h = Assert.Single(hinweise,
                x => x.Kennung == FlottenHinweisKennung.PeakZielUeberReferenzspitze);
            Assert.Equal(FlottenHinweisStufe.Hinweis, h.Stufe);
            Assert.Contains("wirkungslos", h.Text);
            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.PeakZielUnterTagesminimum);
        }

        [Fact]
        public void BetriebskostenUnterEinemTausendstelDerInvestition_SindEinHinweis()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: false);
            f.Einheiten[0].EigeneKosten = true;
            f.Einheiten[0].InvestitionEuro = 15000;
            f.Einheiten[0].JaehrlicheFixeOpexEuro = 1;

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            FlottenHinweis h = Assert.Single(hinweise,
                x => x.Kennung == FlottenHinweisKennung.BetriebskostenSehrNiedrig);
            Assert.Equal(FlottenHinweisStufe.Hinweis, h.Stufe);
            Assert.Contains("15000", h.Text);
        }

        [Fact]
        public void BetriebskostenInUeblicherGroesse_ErzeugenKeinenHinweis()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: false);
            f.Einheiten[0].EigeneKosten = true;
            f.Einheiten[0].InvestitionEuro = 15000;
            f.Einheiten[0].JaehrlicheFixeOpexEuro = 150;

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.BetriebskostenSehrNiedrig);
        }

        [Fact]
        public void OhneEigeneKosten_RechnetDiePruefungMitDenAufgeloestenSaetzen()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: false);
            f.Einheiten[0].EigeneKosten = false;
            var kosten = new SpeicherKostensaetze
            {
                InvestEurProKwh = 350,
                InvestEurProKw = 200,
                BetriebEurProKwhJahr = 0.001,
                BetriebEurProKwJahr = 0.001
            };

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, kosten);

            Assert.Contains(hinweise, x => x.Kennung == FlottenHinweisKennung.BetriebskostenSehrNiedrig);
        }

        // ============================================== SD‑Q4 und der Diagnosebefund

        [Fact]
        public void StartSoCAufDemMinimum_ErzeugtBeiLastspitzenkappungDenHinweisAusSdQ4()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: true);
            f.Einheiten[0].SocStart = f.Einheiten[0].SocMin;

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            FlottenHinweis h = Assert.Single(hinweise,
                x => x.Kennung == FlottenHinweisKennung.StartSoCAufMinimum);
            Assert.Contains("1. Januar", h.Text);
        }

        [Fact]
        public void EineArbeitsloseFlotte_WirdMitIhrenGezaehltenGruendenGemeldet()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: true);
            var diagnose = new FlottenDiagnose
            {
                IntervalleGesamt = 8,
                Arbeitslos = true,
                Gruende = new List<FlottenDiagnoseBefund>
                {
                    new FlottenDiagnoseBefund
                    {
                        Grund = FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot,
                        Intervalle = 8,
                        Anteil = 1.0
                    }
                }
            };

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(
                Eingang(100, 50, 100, 60), f, null, diagnose);

            FlottenHinweis h = Assert.Single(hinweise, x => x.Kennung == FlottenHinweisKennung.FlotteArbeitslos);
            Assert.Equal(FlottenHinweisStufe.Warnung, h.Stufe);
            Assert.Contains("weder geladen noch entladen", h.Text);
            Assert.Contains("Netzladeverbot", h.Text);
            // SD‑Q4: Der Start-SoC-Hinweis kommt auch dann, wenn der Start über dem Minimum liegt.
            Assert.Contains(hinweise, x => x.Kennung == FlottenHinweisKennung.StartSoCAufMinimum);
        }

        [Fact]
        public void OhneDiagnose_BleibtDerArbeitslosBefundAus()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: true);

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.FlotteArbeitslos);
        }

        // ============================================== SD‑Q5

        [Theory]
        [InlineData(FlottenBetriebsziel.PeakShaving, true)]
        [InlineData(FlottenBetriebsziel.PvGreedy, false)]
        [InlineData(FlottenBetriebsziel.PvPlanung, false)]
        [InlineData(FlottenBetriebsziel.Arbitrage, false)]
        [InlineData(FlottenBetriebsziel.MultiUse, false)]
        public void VorgabeNetzladung_HaengtAmBetriebsziel(FlottenBetriebsziel ziel, bool erwartet)
        {
            Assert.Equal(erwartet, FlottenVorgaben.NetzladungFuer(ziel));
        }

        [Fact]
        public void DieSerialisierteVorgabe_BleibtUnveraendertFalsch()
        {
            // Sonst läse ein gespeicherter Stand ohne die Eigenschaft — insbesondere
            // @Projektflotte des Prüfprojekts 1046 — plötzlich anders.
            Assert.False(new FlottenSimulationOptionen().NetzladungErlaubt);
        }

        // ================================================================= Prüfstand

        private static FlottenEingang Eingang(params double[] lasten) => new()
        {
            KonfigurationId = "C",
            DatenId = "D",
            Istwerte = lasten.Select((last, t) => new FlottenNetzintervall
            {
                Zeitstempel = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * t),
                LastKw = last,
                BezugspreisEuroProKWh = 0.3,
                PvVerkaufspreisEuroProKWh = 0.08,
                BhkwVerkaufspreisEuroProKWh = 0.12,
                BatterieVerkaufspreisEuroProKWh = 0.05
            }).ToList()
        };

        private static FlottenStudieKonfiguration Flotte(double peakZiel, bool netzladung) => new()
        {
            Einheiten = new List<FlottenEinheit>
            {
                new FlottenEinheit
                {
                    Id = "A",
                    Name = "Speicher A",
                    KapazitaetKWh = 20,
                    LadeleistungKw = 10,
                    EntladeleistungKw = 10,
                    Ladewirkungsgrad = 1,
                    Entladewirkungsgrad = 1,
                    SocMin = 0.1,
                    SocMax = 0.9,
                    SocStart = 0.5
                }
            },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                WirtschaftlicherPeakZielwertKw = peakZiel,
                NetzladungErlaubt = netzladung
            }
        };
    }
}
