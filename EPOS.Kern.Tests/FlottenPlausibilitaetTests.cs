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

        // ====================================== Die SCHNELLE Stufe (Auftrag #254)

        /// <summary>
        /// <b>Ohne Standortreihe entfallen die beiden Peak-Ziel-Prüfungen — und nur
        /// sie.</b> Das ist die schnelle Stufe der Vorprüfung: Sie braucht weder
        /// Datenbank noch Zeitreihen und läuft deshalb bei jedem Tastendruck.
        /// </summary>
        [Fact]
        public void OhneStandortreihe_BleibenKostenUndStartSoC_UndDasPeakZielSchweigt()
        {
            // Ein Peak-Ziel, das MIT Reihe zwei Hinweise erzeugte (unter dem Maximum der
            // Tagesminima), und Kostensätze, die den Betriebsaufwandshinweis auslösen.
            FlottenStudieKonfiguration f = Flotte(peakZiel: 10, netzladung: false);
            f.Einheiten[0].SocStart = f.Einheiten[0].SocMin;
            var kosten = new SpeicherKostensaetze
            {
                InvestEurProKwh = 350,
                InvestEurProKw = 200,
                BetriebEurProKwhJahr = 0.001,
                BetriebEurProKwJahr = 0.001
            };

            List<FlottenHinweis> mitReihe = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, kosten);
            List<FlottenHinweis> ohneReihe = FlottenPlausibilitaet.Pruefe(
                Array.Empty<FlottenNetzintervall>(), f, kosten);

            Assert.Contains(mitReihe, x => x.Kennung == FlottenHinweisKennung.PeakZielUnterTagesminimum);
            Assert.DoesNotContain(ohneReihe, x => x.Kennung == FlottenHinweisKennung.PeakZielUnterTagesminimum);
            Assert.DoesNotContain(ohneReihe, x => x.Kennung == FlottenHinweisKennung.PeakZielUeberReferenzspitze);

            // Kosten- und Start-SoC-Hinweis stehen in BEIDEN Stufen, im selben Wortlaut
            // und in derselben Reihenfolge - die Liste bleibt EINE Liste.
            FlottenHinweisKennung[] ohnePeak = mitReihe
                .Where(x => x.Kennung != FlottenHinweisKennung.PeakZielUnterTagesminimum &&
                            x.Kennung != FlottenHinweisKennung.PeakZielUeberReferenzspitze)
                .Select(x => x.Kennung).ToArray();
            Assert.Equal(ohnePeak, ohneReihe.Select(x => x.Kennung).ToArray());
            Assert.Contains(ohneReihe, x => x.Kennung == FlottenHinweisKennung.BetriebskostenSehrNiedrig);
            Assert.Contains(ohneReihe, x => x.Kennung == FlottenHinweisKennung.StartSoCAufMinimum);
        }

        /// <summary>
        /// Der Weg über den <c>FlottenEingang</c> und der über die Istreihe allein
        /// liefern dasselbe — die Prüfung liest von einem Eingang nur die Istwerte.
        /// </summary>
        [Fact]
        public void DerEingangUndSeineIstreiheLiefernDieselbenHinweise()
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 10, netzladung: false);
            FlottenEingang eingang = Eingang(100, 50, 100, 60);

            List<FlottenHinweis> ueberEingang = FlottenPlausibilitaet.Pruefe(eingang, f, null);
            List<FlottenHinweis> ueberIstwerte = FlottenPlausibilitaet.Pruefe(eingang.Istwerte, f, null);

            Assert.Equal(ueberEingang.Select(x => x.Kennung), ueberIstwerte.Select(x => x.Kennung));
            Assert.Equal(ueberEingang.Select(x => x.Text), ueberIstwerte.Select(x => x.Text));
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

        // ============================================== PS‑Q1 (#215)

        [Theory]
        [InlineData(FlottenBetriebsziel.PeakShaving, true)]
        [InlineData(FlottenBetriebsziel.MultiUse, true)]
        [InlineData(FlottenBetriebsziel.PvGreedy, false)]
        [InlineData(FlottenBetriebsziel.PvPlanung, false)]
        [InlineData(FlottenBetriebsziel.Arbitrage, false)]
        public void VorgabePeakZielAdaptiv_HaengtAmBetriebsziel(FlottenBetriebsziel ziel, bool erwartet)
        {
            Assert.Equal(erwartet, FlottenVorgaben.PeakZielAdaptivFuer(ziel));
        }

        [Fact]
        public void DieSerialisierteVorgabeDerRatsche_IstFalsch()
        {
            // Muster #183, Anwenderentscheid PS‑Q1: Die Ratsche ist die Vorgabe NEUER
            // Stände; ein GESPEICHERTER Stand trägt „fest" und rechnet unverändert.
            Assert.False(new FlottenSimulationOptionen().PeakZielAdaptiv);
        }

        [Fact]
        public void EinGespeicherterStandOhneDieEigenschaft_LiestSichAlsFest()
        {
            // Genau so steht der Stand @Projektflotte des Prüfprojekts 1046 in
            // Tab_SpeicherAuslegung: als JSON, das die Eigenschaft nicht kennt.
            const string alt = "{\"Betriebsziel\":1,\"WirtschaftlicherPeakZielwertKw\":16," +
                               "\"NetzladungErlaubt\":true}";

            FlottenSimulationOptionen gelesen =
                System.Text.Json.JsonSerializer.Deserialize<FlottenSimulationOptionen>(alt)!;

            Assert.False(gelesen.PeakZielAdaptiv);
            Assert.Equal(16, gelesen.WirtschaftlicherPeakZielwertKw);
            Assert.Equal(FlottenBetriebsziel.PeakShaving, gelesen.Betriebsziel);
        }

        [Fact]
        public void DieBisektion_RechnetAusdruecklichOhneRatsche()
        {
            // Spezifikation 5.1.1, Alternative S‑C: Gesucht ist das kleinste FESTE H.
            // Mit der Ratsche fände die Bisektion immer ihre untere Schranke.
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: true);
            f.Optionen.PeakZielAdaptiv = true;

            FlottenPeakZielErgebnis e = FlottenPeakZiel.PeakZielBestimmen(
                Eingang(100, 50, 100, 60), f, null, null, CancellationToken.None);

            // Der übergebene Stand bleibt unangetastet — gerechnet wird auf einer Kopie.
            Assert.True(f.Optionen.PeakZielAdaptiv);
            Assert.Contains("mit Vorausschau erreichbar", e.Herleitung);
        }

        // ====================================== Prognosepflicht planender Ziele (#256)

        /// <summary>
        /// DIE REGEL: planendes Ziel + Informationsstand „Archivierte Prognose-Snapshots"
        /// + kein Snapshot dieser Art = ein Befund der Stufe „Problem".
        /// </summary>
        [Theory]
        [InlineData(FlottenBetriebsziel.PvPlanung)]
        [InlineData(FlottenBetriebsziel.Arbitrage)]
        [InlineData(FlottenBetriebsziel.MultiUse)]
        public void EinPlanendesZielOhneGeladenePrognose_IstEinProblem(FlottenBetriebsziel ziel)
        {
            FlottenStudieKonfiguration f = Planend(ziel);

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            FlottenHinweis h = Assert.Single(hinweise,
                x => x.Kennung == FlottenHinweisKennung.PrognoseFehlt);
            Assert.Equal(FlottenHinweisStufe.Problem, h.Stufe);
            // Der Text nennt das ZIEL, den INFORMATIONSSTAND und BEIDE Auswege.
            Assert.Contains(ziel.ToString(), h.Text, StringComparison.Ordinal);
            Assert.Contains("Archivierte Prognose-Snapshots", h.Text, StringComparison.Ordinal);
            Assert.Contains("Idealwissen", h.Text, StringComparison.Ordinal);
            Assert.Contains("Prognosen", h.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// IDEALWISSEN ist der ausdrücklich gewählte Ausweg — für ihn baut der Kern den
        /// Snapshot aus der Istreihe, und die Regel schweigt.
        /// </summary>
        [Fact]
        public void MitIdealwissen_GreiftDieRegelNicht()
        {
            FlottenStudieKonfiguration f = Planend(FlottenBetriebsziel.Arbitrage);
            f.Optionen.PrognoseArt = PrognoseArt.Oracle;

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.PrognoseFehlt);
            Assert.Null(FlottenPlausibilitaet.Prognosepflicht(f, null));
        }

        /// <summary>Die zwei REAKTIVEN Ziele planen nicht und brauchen keine Prognose.</summary>
        [Theory]
        [InlineData(FlottenBetriebsziel.PeakShaving)]
        [InlineData(FlottenBetriebsziel.PvGreedy)]
        public void EinReaktivesZiel_BrauchtKeinePrognose(FlottenBetriebsziel ziel)
        {
            FlottenStudieKonfiguration f = Planend(ziel);

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            Assert.DoesNotContain(hinweise, x => x.Kennung == FlottenHinweisKennung.PrognoseFehlt);
        }

        /// <summary>
        /// Ein GELADENER Snapshot der verlangten Art hebt den Befund auf. Geprüft wird
        /// allein die ART — ob er jeden Horizont abdeckt, entscheidet der Simulator mit
        /// der Reihe in der Hand.
        /// </summary>
        [Fact]
        public void EinGeladenerSnapshotDerVerlangtenArt_HebtDenBefundAuf()
        {
            FlottenStudieKonfiguration f = Planend(FlottenBetriebsziel.PvPlanung);
            FlottenEingang e = Eingang(100, 50, 100, 60);
            e.Prognosen.Add(new FlottenPrognoseSnapshot("p1",
                e.Istwerte[0].Zeitstempel, e.Istwerte[0].Zeitstempel,
                PrognoseArt.VerifiziertBekannt, e.Istwerte));

            Assert.DoesNotContain(FlottenPlausibilitaet.Pruefe(e, f, null),
                x => x.Kennung == FlottenHinweisKennung.PrognoseFehlt);

            // Ein Snapshot der ANDEREN Art zaehlt nicht: Idealwissen ist kein Nachweis
            // dafuer, dass ein Stand vorher bekannt war.
            var nurOracle = new List<FlottenPrognoseSnapshot>
            {
                new FlottenPrognoseSnapshot("o1", e.Istwerte[0].Zeitstempel,
                    e.Istwerte[0].Zeitstempel, PrognoseArt.Oracle, e.Istwerte)
            };
            Assert.NotNull(FlottenPlausibilitaet.Prognosepflicht(f, nurOracle));
        }

        /// <summary>
        /// Die Regel braucht KEINE Zeitreihe — deshalb trägt sie auch die schnelle Stufe
        /// der Vorprüfung (Auftrag #254).
        /// </summary>
        [Fact]
        public void OhneReihe_StehtDerBefundGenauso()
        {
            FlottenStudieKonfiguration f = Planend(FlottenBetriebsziel.MultiUse);

            List<FlottenHinweis> ohneReihe = FlottenPlausibilitaet.Pruefe(
                Array.Empty<FlottenNetzintervall>(), f, null);

            Assert.Contains(ohneReihe, x => x.Kennung == FlottenHinweisKennung.PrognoseFehlt);
        }

        // ================================= Die Lebensdauerkurve der Einheiten (#257)

        /// <summary>
        /// DIE REGEL: Jeder Grund, an dem die Rainflow-Auswertung abbricht, ist ein
        /// Befund der Stufe „Problem" — und der Text nennt die EINHEIT und die
        /// PUNKTNUMMER, 1-basiert wie im Editor.
        /// </summary>
        [Theory]
        [InlineData(0.0, 0.0, "nicht ausgefüllt")]
        [InlineData(1.5, 1000.0, "Entladetiefe")]
        [InlineData(0.5, 0.0, "Zyklen bis Lebensdauerende")]
        public void EinUnvollstaendigerKurvenpunkt_IstEinProblem(
            double tiefe, double zyklen, string teilsatz)
        {
            FlottenStudieKonfiguration f = MitKurve(
                new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
                new FlottenRainflowPunkt { Entladetiefe = tiefe, ZyklenBisEol = zyklen });

            List<FlottenHinweis> hinweise = FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), f, null);

            FlottenHinweis h = Assert.Single(hinweise,
                x => x.Kennung == FlottenHinweisKennung.LebensdauerkurveUngueltig);
            Assert.Equal(FlottenHinweisStufe.Problem, h.Stufe);
            Assert.Contains("Speicher A", h.Text, StringComparison.Ordinal);
            Assert.Contains("Punkt 2", h.Text, StringComparison.Ordinal);
            Assert.Contains(teilsatz, h.Text, StringComparison.Ordinal);
            // BEIDE AUSWEGE stehen im Satz — ausfüllen oder entfernen.
            Assert.Contains("ausfüllen", h.Text, StringComparison.Ordinal);
            Assert.Contains("Punkt entfernen", h.Text, StringComparison.Ordinal);
        }

        /// <summary>Dieselbe Entladetiefe zweimal: gemeldet wird der ZWEITE Punkt.</summary>
        [Fact]
        public void EineDoppelteEntladetiefe_ZeigtAufDenZweitenPunkt()
        {
            FlottenStudieKonfiguration f = MitKurve(
                new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
                new FlottenRainflowPunkt { Entladetiefe = 0.5, ZyklenBisEol = 4000 },
                new FlottenRainflowPunkt { Entladetiefe = 0.5, ZyklenBisEol = 5000 });

            FlottenHinweis h = FlottenPlausibilitaet.Lebensdauerkurve(f);

            Assert.NotNull(h);
            Assert.Contains("Punkt 3", h.Text, StringComparison.Ordinal);
            Assert.Contains("früheren Punkt", h.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// EINE LEERE KURVE IST ZULÄSSIG — sie ist der Normalfall: Ohne Punkte zählt der
        /// Lauf die Zyklen und weist keinen Rainflow-Schaden aus. Eine vollständige Kurve
        /// ebenso.
        /// </summary>
        [Fact]
        public void EineLeereUndEineVollstaendigeKurve_SindInOrdnung()
        {
            Assert.Null(FlottenPlausibilitaet.Lebensdauerkurve(MitKurve()));
            Assert.Null(FlottenPlausibilitaet.Lebensdauerkurve(MitKurve(
                new FlottenRainflowPunkt { Entladetiefe = 1, ZyklenBisEol = 1000 },
                new FlottenRainflowPunkt { Entladetiefe = 0.5, ZyklenBisEol = 4000 })));
            Assert.DoesNotContain(FlottenPlausibilitaet.Pruefe(Eingang(100, 50, 100, 60), MitKurve(), null),
                x => x.Kennung == FlottenHinweisKennung.LebensdauerkurveUngueltig);
        }

        /// <summary>
        /// Die Regel braucht KEINE Zeitreihe — die schnelle Stufe der Vorprüfung trägt
        /// sie deshalb genauso, und der Befund liefert dem Editor die Punktnummer.
        /// </summary>
        [Fact]
        public void OhneReihe_StehtDerBefundGenauso_undNenntDiePunktnummer()
        {
            FlottenStudieKonfiguration f = MitKurve(new FlottenRainflowPunkt());

            Assert.Contains(FlottenPlausibilitaet.Pruefe(Array.Empty<FlottenNetzintervall>(), f, null),
                x => x.Kennung == FlottenHinweisKennung.LebensdauerkurveUngueltig);

            FlottenKurvenbefund befund = FlottenPlausibilitaet.Kurvenbefund(f.Einheiten[0]);
            Assert.NotNull(befund);
            Assert.Equal(1, befund.Punktnummer);
            Assert.Equal(FlottenPlausibilitaet.Lebensdauerkurve(f).Text, befund.Text);
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

        /// <summary>Dieselbe Flotte, deren einzige Einheit die gegebene Kurve trägt.</summary>
        private static FlottenStudieKonfiguration MitKurve(params FlottenRainflowPunkt[] punkte)
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: true);
            f.Einheiten[0].RainflowKurve = punkte.ToList();
            return f;
        }

        /// <summary>Dieselbe Flotte, aber mit einem anderen Betriebsziel.</summary>
        private static FlottenStudieKonfiguration Planend(FlottenBetriebsziel ziel)
        {
            FlottenStudieKonfiguration f = Flotte(peakZiel: 80, netzladung: true);
            f.Optionen.Betriebsziel = ziel;
            return f;
        }

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
