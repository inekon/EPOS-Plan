using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KK3 — der Raumeinfluss der Kühlkurve im Kreis</b> (Entwurf KK 2.2, 2.3, 2.5; Festlegungen 5, 6, 8, 9; Wellenzeile
    /// KK3) — ohne Datenbank, am Probegebäude aus <see cref="Vdi6007Probe"/> mit Kühldecke (16/19 °C, Grenze 16 °C):
    /// Vorzeichen und Grenzen der Absenkung im Rand, Pendelregel, „Raumeinfluss 0 = ohne“, „Schalter aus = keiner“ und das
    /// Orakel <b>O2kk</b> (Absenkung mit Kappung, eindeutiger Fixpunkt, Konvergenz).
    /// </summary>
    public class KuehlRaumeinflussTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KuehlRaumeinflussTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private const double KUEHL_SOLL_C = 24.0, ANLAGE_C = 14.0, ERZEUGER_MIN_C = 12.0, GROSS_KW = 1.0e6;

        // ------------------------------------------------------------------------------------------ Bausteine

        private static ProjektGebaeudeModel Gebaeude(double? kK)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gekuehlt(KUEHL_SOLL_C);
            g.Kuehluebergabe_Aktiv = true;
            g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            g.Kuehlkurve_Aktiv = true;
            g.Kuehlkurve_Raumeinfluss = kK;
            return g;
        }

        private static GebaeudeModellEingang Eingang(double? kK, bool schalter = true, string stufe = DbWerte.ANLAGENKOPPLUNG_AK3)
        {
            using (KuehlkurveKernschalter.Schalten(schalter))
            {
                GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(Gebaeude(kK), Klima, Vdi6007Probe.Wochenende(),
                    Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, true, stufe, double.NaN, 1.0, ANLAGE_C);
                e.KuehlkurveUntergrenzeSetzen(ERZEUGER_MIN_C);
                return e;
            }
        }

        private static GebaeudeStepper Stepper(GebaeudeModellEingang e)
        {
            GebaeudeStepper s = GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e));
            s.Beginnen();
            return s;
        }

        /// <summary>Eine Wärmeseite ohne Grenze (zustandsfrei).</summary>
        private sealed class Waermegross : IErzeugerkapazitaet
        {
            public string Bezeichner => "Probewärme";
            public Erzeugerangebot Abfragen(int stunde, double vorlaufC)
                => new Erzeugerangebot(GROSS_KW, GROSS_KW, Verfuegbarkeitsgrund.KeineBegrenzung, true, double.NaN, false);
        }

        /// <summary>Ein Kälteerzeuger konstanter Kapazität, unabhängig vom Vorlauf.</summary>
        private sealed class Kaeltekonstante : IKaelteerzeugerkapazitaet
        {
            private readonly double _kw;
            internal Kaeltekonstante(double kw) { _kw = kw; }
            public string Bezeichner => "Probekälte";
            public Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC)
                => new Erzeugerangebot(_kw, _kw, Verfuegbarkeitsgrund.KeineBegrenzung, true, kuehlVorlaufC, false);
        }

        private static Anlagenkopplung Kreis(GebaeudeStepper s, KuehlRaumeinfluss k2, double kaelteKw = GROSS_KW)
            => new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                   new IErzeugerkapazitaet[] { new Waermegross() }, null)
            {
                Kaelteschranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { new Kaeltekonstante(kaelteKw) }, null, ANLAGE_C),
                KuehlRaumeinfluss = k2,
            };

        /// <summary>Ein Rand mit Kühldecke (Vorgaben) am Kühlvorlauf <paramref name="vorlaufC"/>, θ_max = 24 °C.</summary>
        private static Stundenrand Rand(double vorlaufC, bool mitUebergabe = true)
        {
            string art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            Uebergabekennwerte k = mitUebergabe
                ? Kuehluebergabe.Gespiegelt(1000.0, Kuehluebergabe.VorgabeExponent(art), Kuehluebergabe.VorgabeVorlaufC(art),
                                            Kuehluebergabe.VorgabeRuecklaufC(art), 26.0)
                : null;
            return new Stundenrand(30.0, 30.0, 20.0, KUEHL_SOLL_C, 0.0, 0.0, 0.0,
                                   kuehlUebergabeGespiegelt: k, kuehlVorlaufC: vorlaufC);
        }

        private static IReadOnlyList<IReadOnlyList<Stundenergebnis>> Loesung(double thetaAir, double kuehlW = 500.0)
            => new[] { new[] { new Stundenergebnis(0.0, kuehlW, thetaAir, thetaAir, thetaAir, thetaAir, thetaAir, thetaAir,
                                                   thetaAir, thetaAir, 1) } };

        // ------------------------------------------------------------------------------------------ Rand

        /// <summary>
        /// <b>Vorzeichen und Grenzen</b> (2.2, Festlegung 5): Durchlauf 1 ohne Absenkung; eine Überschreitung senkt den Vorlauf
        /// um k_K · ü, eine Unterschreitung hebt ihn nie; unten am Erzeuger gekappt, an der Vorlaufgrenze mit Befund; nie über
        /// dem Vorlauf der Kurve; ohne Kühlübergabe derselbe Rand.
        /// </summary>
        [Fact]
        public void Absenkung_im_Rand_Vorzeichen_und_Grenzen()
        {
            var k2 = new KuehlRaumeinfluss(new[] { 2.0 }, new[] { ERZEUGER_MIN_C }, new[] { double.NaN });
            k2.StundeBeginnen();
            Stundenrand r = Rand(18.0);
            Assert.Equal(18.0, k2.Absenken(0, 0, r).KuehlVorlaufC);
            Assert.Equal(18.0, k2.VerlangterC);

            Assert.Equal(2.0, k2.Nachfuehren(Loesung(25.0)), 12);          // ü = 1 K
            k2.DurchlaufBeginnen();
            Assert.Equal(16.0, k2.Absenken(0, 0, r).KuehlVorlaufC, 12);
            Assert.Equal(16.0, k2.VerlangterC, 12);

            Assert.Equal(2.0, k2.Nachfuehren(Loesung(23.0)), 12);          // Unterschreitung: zurück auf 0, nie Anhebung
            Assert.Equal(0.0, k2.Absenkung(0));
            Assert.Equal(18.0, k2.Absenken(0, 0, r).KuehlVorlaufC);

            k2.Nachfuehren(Loesung(30.0));                                  // 12 K: am Erzeuger gekappt, kein Grenzbefund
            Stundenrand amErzeuger = k2.Absenken(0, 0, r);
            Assert.Equal(ERZEUGER_MIN_C, amErzeuger.KuehlVorlaufC);
            Assert.False(amErzeuger.KuehlVorlaufGekappt);

            var mitGrenze = new KuehlRaumeinfluss(new[] { 2.0 }, new[] { ERZEUGER_MIN_C }, new[] { 16.0 });
            mitGrenze.StundeBeginnen();
            mitGrenze.Absenken(0, 0, r);
            mitGrenze.Nachfuehren(Loesung(30.0));
            Stundenrand anGrenze = mitGrenze.Absenken(0, 0, r);
            Assert.Equal(16.0, anGrenze.KuehlVorlaufC);
            Assert.True(anGrenze.KuehlVorlaufGekappt, "Befund der Vorlaufgrenze");

            Stundenrand ohne = Rand(18.0, mitUebergabe: false);
            Stundenrand ohneAus = mitGrenze.Absenken(0, 0, ohne);
            Assert.True(double.IsNaN(ohneAus.KuehlVorlaufC) || ohneAus.KuehlVorlaufC.Equals(ohne.KuehlVorlaufC));

            // Festschreiben: Kennzahlen aus dem letzten Durchlauf (16 °C an der Grenze, Kühlstunde).
            mitGrenze.Festschreiben(Loesung(24.5));
            Assert.Equal(1, mitGrenze.Kuehlstunden);
            Assert.Equal(1, mitGrenze.StundenAnVorlaufgrenze);
            Assert.Equal(1, mitGrenze.StundenAbgesenkt);
            Assert.Equal(16.0, mitGrenze.KuehlVorlaufMittelC, 12);
        }

        /// <summary>Der Vorgabewert beim Einschalten (Q-KK-5 (b)) liegt im Bereich der Spalte (Festlegung 7).</summary>
        [Fact]
        public void Vorgabewert_der_Staerke_liegt_im_Bereich()
        {
            Assert.Equal(3.0, GebaeudeFestwerte.KUEHLKURVE_RAUMEINFLUSS_VORGABE);
            Assert.InRange(GebaeudeFestwerte.KUEHLKURVE_RAUMEINFLUSS_VORGABE,
                           GebaeudeFestwerte.KUEHLKURVE_RAUMEINFLUSS_MIN, GebaeudeFestwerte.KUEHLKURVE_RAUMEINFLUSS_MAX);
        }

        /// <summary>Pendelregel (Festlegung 8): Festhalten setzt die Absenkung auf die des ersten Durchlaufs (0) und zählt.</summary>
        [Fact]
        public void Pendelregel_haelt_die_Absenkung_des_ersten_Durchlaufs()
        {
            var k2 = new KuehlRaumeinfluss(new[] { 3.0 }, new[] { double.NaN }, new[] { double.NaN });
            k2.StundeBeginnen();
            k2.Absenken(0, 0, Rand(18.0));
            k2.Nachfuehren(Loesung(25.0));
            Assert.Equal(3.0, k2.AbsenkungMax(), 12);
            k2.Festhalten();
            Assert.Equal(0.0, k2.AbsenkungMax());
            Assert.Equal(1, k2.StundenFestgehalten);
        }

        // ------------------------------------------------------------------------------------------ Schalter

        /// <summary>
        /// <b>Schalter aus</b> (Festlegungen 1, 6, 13): Ohne Kernschalter, auf AK1 oder ohne Kälteseite baut der Kreis keinen
        /// Raumeinfluss — der Kreis rechnet am festen Vorlauf wie zuvor; Stunden tragen keinen Erzeugervorlauf.
        /// </summary>
        [Fact]
        public void Schalter_aus_baut_keinen_Raumeinfluss()
        {
            Assert.Null(KuehlRaumeinfluss.AusEingaengen(new[] { Eingang(2.0, schalter: false) }));
            Assert.Null(KuehlRaumeinfluss.AusEingaengen(new[] { Eingang(2.0, stufe: DbWerte.ANLAGENKOPPLUNG_AK1) }));
            Assert.Null(KuehlRaumeinfluss.AusEingaengen(new GebaeudeModellEingang[] { null }));
            Assert.NotNull(KuehlRaumeinfluss.AusEingaengen(new[] { Eingang(null) }));

            // Ohne Schalter: die Untergrenze am Erzeuger wirkt nicht, die Reihe bleibt der feste Vorlauf.
            GebaeudeModellEingang aus = Eingang(2.0, schalter: false);
            Assert.False(aus.KuehlkurveWirksam);
            Assert.True(double.IsNaN(aus.KuehlkurveErzeugerC));

            GebaeudeStepper s = Stepper(aus);
            Anlagenkopplung kreis = Kreis(s, null);
            for (int h = 0; h < 48; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Assert.True(double.IsNaN(k.KuehlVorlaufC));
                kreis.Festschreiben(h);
            }
        }

        /// <summary>
        /// <b>Raumeinfluss 0 = ohne</b> (Muster „H2 aus bitgleich“): Ein Raumeinfluss mit k_K = 0 an einer Kälteschranke, die
        /// nicht vom Vorlauf abhängt, rechnet das Jahr Bit für Bit wie der Kreis ohne ihn — Raumluft, Kühlleistung, Durchläufe.
        /// </summary>
        [Fact]
        public void Raumeinfluss_null_rechnet_bitgleich_wie_ohne()
        {
            GebaeudeModellEingang e = Eingang(null);
            Anlagenkopplung ohne = Kreis(Stepper(e), null);
            Anlagenkopplung mit = Kreis(Stepper(e), KuehlRaumeinfluss.AusEingaengen(new[] { e }));
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde a = ohne.Stunde(h, double.NaN, default);
                Kopplungsstunde b = mit.Stunde(h, double.NaN, default);
                Assert.Equal(a.Durchlaeufe, b.Durchlaeufe);
                Assert.True(a.Loesung[0][0].ThetaAirMittel.Equals(b.Loesung[0][0].ThetaAirMittel), "Raumluft, Stunde " + h);
                Assert.True(a.Loesung[0][0].KuehlleistungW.Equals(b.Loesung[0][0].KuehlleistungW), "Kühlleistung, Stunde " + h);
                Assert.Equal(0.0, b.KuehlAbsenkungK);
                ohne.Festschreiben(h);
                mit.Festschreiben(h);
            }
            Assert.Equal(ohne.DurchlaeufeMittel, mit.DurchlaeufeMittel);
            Assert.Equal(0, mit.KuehlRaumeinfluss.StundenAbgesenkt);
            Assert.True(mit.KuehlRaumeinfluss.Kuehlstunden > 100);
        }

        // ------------------------------------------------------------------------------------------ O2kk

        /// <summary>
        /// <b>O2kk</b> (Entwurf KK 2.2, Festlegung 8): der Raumeinfluss der Kühlkurve im Kreis gegen die Bisektion des
        /// Fixpunkts je Stunde. F(a) = a − k_K · max(0, θ_air(a) − θ_max) mit dem Rand wie im Kreis (Vorlauf der Kurve minus a,
        /// unten am Erzeuger, an der Vorlaufgrenze 16 °C gekappt): θ_air fällt mit a, F steigt streng — der Fixpunkt ist
        /// eindeutig, auch wo die Kappung greift. Der Kreis trifft ihn auf das Abbruchmaß; jede Stunde rechnet erneut am
        /// Kreiswert Bit für Bit wie der Kreis (Determinismus).
        /// </summary>
        [Theory]
        [InlineData(2.0)]
        [InlineData(5.0)]
        [InlineData(10.0)]
        public void O2kk_Absenkung_mit_Kappung_trifft_den_Fixpunkt(double kK)
        {
            GebaeudeModellEingang e = Eingang(kK);
            double grenze = e.Kuehlkurve.VorlaufgrenzeC;
            GebaeudeStepper s = Stepper(e);
            KuehlRaumeinfluss k2 = KuehlRaumeinfluss.AusEingaengen(new[] { e });
            Anlagenkopplung kreis = Kreis(s, k2);

            int bisektiert = 0, gekappt = 0, durchlaeufeMax = 0;
            double dAMax = 0.0, dThetaMax = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Stundenergebnis ist = k.Loesung[0][0];
                Anlagenverfuegbarkeit kv = k.Kaelteangebot.Value.AlsVerfuegbarkeit();
                double thetaMax = double.NaN;
                double vKurve = double.NaN;
                Stundenergebnis Schritt(double a)
                    => s.Schritt(h, (int z, int st, in Stundenrand r) =>
                    {
                        thetaMax = r.MitKuehlung ? r.ThetaMax : double.NaN;
                        vKurve = r.KuehlVorlaufC;
                        Stundenrand x = r;
                        if (a > 0.0 && r.MitKuehluebergabe && !double.IsNaN(r.KuehlVorlaufC))
                        {
                            double ziel = Math.Max(r.KuehlVorlaufC - a, ERZEUGER_MIN_C);
                            bool an = ziel < grenze;
                            if (an) ziel = grenze;
                            if (ziel < r.KuehlVorlaufC) x = r.MitKuehlvorlauf(ziel, an || r.KuehlVorlaufGekappt);
                        }
                        return x.MitVerfuegbarkeit(GROSS_KW * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN)
                                .MitKaelteverfuegbarkeit(kv.LeistungKw * 1000.0, kv.Grund, kv.VorlaufC);
                    })[0];
                Stundenergebnis null0 = Schritt(0.0);
                if (!double.IsNaN(thetaMax) && null0.ThetaAirMittel > thetaMax)
                {
                    double F(double a) => a - kK * Math.Max(0.0, Schritt(a).ThetaAirMittel - thetaMax);
                    double lo = 0.0, hi = kK * (null0.ThetaAirMittel - thetaMax) + 1.0;
                    Assert.True(F(lo) < 0.0 && F(hi) > 0.0, "Stunde " + h + ": keine Klammer");
                    for (int i = 0; i < 60; i++)
                    {
                        double m = 0.5 * (lo + hi);
                        if (F(m) > 0.0) hi = m; else lo = m;
                    }
                    double soll = 0.5 * (lo + hi);
                    Stundenergebnis loes = Schritt(soll);
                    dAMax = Math.Max(dAMax, Math.Abs(k.KuehlAbsenkungK - soll));
                    dThetaMax = Math.Max(dThetaMax, Math.Abs(ist.ThetaAirMittel - loes.ThetaAirMittel));
                    if (vKurve - soll < grenze) gekappt++;
                    bisektiert++;
                }
                else Assert.Equal(0.0, k.KuehlAbsenkungK);
                Stundenergebnis wieder = Schritt(k.KuehlAbsenkungK);
                Assert.True(wieder.ThetaAirMittel.Equals(ist.ThetaAirMittel), "Determinismus, Stunde " + h);
                durchlaeufeMax = Math.Max(durchlaeufeMax, k.Durchlaeufe);
                kreis.Festschreiben(h);
            }
            _aus.WriteLine("O2kk k_K {0}: Überschreitungsstunden {1} (davon an der Grenze {2}), |Δa| max {3:0.0000} K, |Δθ| max {4:0.00000} K, " +
                           "Durchläufe max {5} (Mittel {6:0.000}), festgehalten {7}; Kühlstunden {8}, Vorlauf Mittel {9:0.00} / min {10:0.00} °C, " +
                           "an der Grenze {11} h, abgesenkt {12} h, Absenkung {13:0.0} Kh",
                           kK, bisektiert, gekappt, dAMax, dThetaMax, durchlaeufeMax, kreis.DurchlaeufeMittel, k2.StundenFestgehalten,
                           k2.Kuehlstunden, k2.KuehlVorlaufMittelC, k2.KuehlVorlaufMinC, k2.StundenAnVorlaufgrenze, k2.StundenAbgesenkt,
                           k2.AbsenkungSummeKh);
            Assert.True(bisektiert > 100, "zu wenige Überschreitungsstunden: " + bisektiert);
            if (kK >= 5.0) Assert.True(gekappt > 0, "die Kappung greift nie");
            Assert.True(durchlaeufeMax > 1, "der Raumeinfluss wirkt nicht zurück");
            Assert.True(durchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            Assert.True(dAMax <= TOL_ABSENKUNG_K, "Absenkung " + dAMax);
            Assert.True(dThetaMax <= TOL_THETA_K, "Raumluft " + dThetaMax);
            Assert.True(k2.StundenAbgesenkt > 0 && k2.Kuehlstunden > 0);
            Assert.True(k2.KuehlVorlaufMinC >= grenze - 1e-12, "nie unter der Vorlaufgrenze");
        }

        private const double TOL_ABSENKUNG_K = 0.1, TOL_THETA_K = 0.05;
    }
}
