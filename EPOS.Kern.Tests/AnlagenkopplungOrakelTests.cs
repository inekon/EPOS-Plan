using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Prüforakel des Kreises AK3</b> (AK3-W3c; Entwurf AK3 2.4, 2.6, 2.7, 3; E102 ohne O3) — ohne Datenbank, die
    /// Erzeuger als Testdoubles hinter <see cref="IErzeugerkapazitaet"/>.
    /// <list type="bullet">
    /// <item><b>O1</b>: stetige, linear über θ_V fallende Kapazität, ein Gebäude, eine Zone, kein Speicher, keine Sperre.
    /// <b>H2 ist ein Testdouble hinter der Naht <see cref="Anlagenkopplung.Vorlaufkorrektur"/></b> (die echte H2 kommt
    /// mit W4): θ_V = V₀ + k_R · (θ_ref − θ_i), θ_ref die Raumluft des unbegrenzten Probeschritts derselben Stunde. Der
    /// Kreis trifft je Stunde die Bisektionslösung von g(θ_V) = θ_V − V₀ − k_R · (θ_ref − θ_i(Φ(θ_V))) = 0.</item>
    /// <item><b>O1p</b>: dasselbe mit der echten Kennlinie (drei Vorläufe 35/45/55 °C, Interpolation an) gegen die
    /// Handrechnung der Kapazität; zwei Läufe byte-gleich.</item>
    /// <item><b>O2</b>: konstante Kapazität ohne H2 — der Probeschritt und ein begrenzter Durchlauf, Bit für Bit wie
    /// „AK2 mit Schranke = P“; mit H2-Testdouble die Bisektionslösung.</item>
    /// <item><b>θ_R-Abhängigkeit (UB‑E2, Umsetzungskonzept 9):</b> Eine Wärmepumpe mit Bivalenzobjekt im Vorwärmbetrieb
    /// (B3) bietet über ihrem Höchstvorlauf min(W_H·(θ_WP,max − θ_R), Φ_KF(θ_WP,max)) an; θ_R ist der Kreisrücklauf des
    /// vorigen Durchlaufs (<see cref="Anlagenkopplung.Kreisruecklauf"/>). Die Kapazität fällt monoton mit θ_R, die
    /// Abhängigkeit läuft über die Abbruchschwellen des Kreises (Schranke 0,1 W, Vorlauf); geprüft in
    /// <c>BetriebsbereichTests.Angebot_im_Kreis_rechnet_B3_ueber_dem_Hoechstvorlauf</c>. Ohne Bivalenzobjekt bleibt der Kreis
    /// Bit für Bit wie O1/O1p/O2.</item>
    /// </list>
    /// Toleranzen nach Entwurf: Vorlauf 0,05 K, Raumluft 0,01 K, Leistung 0,1 W.
    /// </summary>
    public class AnlagenkopplungOrakelTests
    {
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungOrakelTests(ITestOutputHelper aus) { _aus = aus; }

        private const double TOL_VORLAUF_K = 0.05, TOL_THETA_K = 0.01, TOL_PHI_W = 0.1;

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        /// <summary>Das gekoppelte Probegebäude (wie <c>AnlagenkopplungKreisTests</c>).</summary>
        private static GebaeudeModellEingang Gekoppelt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Nachtabsenkung = g.Raumsolltemperatur_Tag;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            return GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
        }

        private static GebaeudeStepper Stepper()
        {
            GebaeudeStepper s = GebaeudeStepper.Einzone(ZonenEingang.Einzeln(Gekoppelt()));
            s.Beginnen();
            return s;
        }

        private static double SpitzeW() => Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1).HeizlastW.Max();

        // ------------------------------------------------------------------------------------------
        //  Testdoubles
        // ------------------------------------------------------------------------------------------

        /// <summary>Kapazität als Funktion des Vorlaufs, zustandsfrei; ohne Vorlauf (NaN) am Bezugsvorlauf.</summary>
        private sealed class Kennkapazitaet : IErzeugerkapazitaet
        {
            private readonly Func<double, double> _kw;
            private readonly double _bezugC;
            internal Kennkapazitaet(Func<double, double> kw, double bezugC) { _kw = kw; _bezugC = bezugC; }
            public string Bezeichner => "Probeerzeuger";
            public Erzeugerangebot Abfragen(int stunde, double vorlaufC)
            {
                double p = _kw(double.IsNaN(vorlaufC) ? _bezugC : vorlaufC);
                return new Erzeugerangebot(p, p, Verfuegbarkeitsgrund.KeineBegrenzung, true, double.NaN, false);
            }
        }

        /// <summary>Erzwingt einen Fallwechsel: die erste Abfrage je Stunde knapp, jede weitere reichlich.</summary>
        private sealed class ErstKnapp : IErzeugerkapazitaet
        {
            private readonly double _knapp, _reichlich;
            private int _stunde = -1;
            internal ErstKnapp(double knapp, double reichlich) { _knapp = knapp; _reichlich = reichlich; }
            public string Bezeichner => "Wechselerzeuger";
            public Erzeugerangebot Abfragen(int stunde, double vorlaufC)
            {
                double p = stunde == _stunde ? _reichlich : _knapp;
                _stunde = stunde;
                return new Erzeugerangebot(p, p, Verfuegbarkeitsgrund.KeineBegrenzung, true, double.NaN, false);
            }
        }

        /// <summary>
        /// Das H2-Testdouble (W4 baut die echte Naht): θ_V = V₀ + k_R · (θ_ref − θ_i), ohne Lösung V₀. θ_ref setzt die
        /// Probe je Stunde (die Raumluft des unbegrenzten Schritts derselben Stunde) oder lässt die Vorgabe 20 °C.
        /// </summary>
        private sealed class H2Double
        {
            private readonly double _v0, _kr;
            internal double Referenz { get; set; } = 20.0;
            internal H2Double(double v0, double kr) { _v0 = v0; _kr = kr; }
            internal double Naht(int h, double v, IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung)
                => loesung == null ? _v0 : _v0 + _kr * (Referenz - loesung[0][0].ThetaAirMittel);
        }

        /// <summary>Eine Stunde des Kreises mit H2-Testdouble: θ_ref aus dem unbegrenzten Schritt, dann der Kreis.</summary>
        private static Kopplungsstunde MitReferenz(Anlagenkopplung kreis, GebaeudeStepper s, H2Double h2, int h)
        {
            h2.Referenz = s.Schritt(h)[0].ThetaAirMittel;
            return kreis.Stunde(h, double.NaN, default);
        }

        // ------------------------------------------------------------------------------------------
        //  Bisektion (das Orakel)
        // ------------------------------------------------------------------------------------------

        private sealed class Orakelbefund
        {
            internal int Stunden, Begrenzt, DurchlaeufeMax;
            internal double DvMax, DthetaMax, DphiMax; internal string Schlimmste = "";
        }

        /// <summary>
        /// Das Jahr im Kreis mit H2-Testdouble; je Stunde zuerst die Bisektion am selben Stepper (Schritte werden
        /// zurückgesetzt), dann der Kreis, dann der Vergleich.
        /// </summary>
        private Orakelbefund Orakel(Func<double, double> kapazitaetKw, IErzeugerkapazitaet erzeuger, double v0, double kr,
                                    double faktor, Func<double, int> stuetzstelle, out Anlagenkopplung kreis,
                                    out GebaeudeModellErgebnis ergebnis)
        {
            GebaeudeStepper s = Stepper();
            var h2 = new H2Double(v0, kr);
            kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, faktor) }, new[] { erzeuger }, null)
            {
                Vorlaufkorrektur = h2.Naht,
                Stuetzstelle = stuetzstelle,
                StuetzstelleHalten = false,
            };
            var befund = new Orakelbefund();
            for (int h = 0; h < 8760; h++)
            {
                double thetaRef = s.Schritt(h)[0].ThetaAirMittel;
                h2.Referenz = thetaRef;
                Stundenergebnis Bei(double v)
                {
                    double pW = kapazitaetKw(v) * 1000.0 / faktor;
                    return s.Schritt(h, (int z, int st, in Stundenrand r) =>
                        r.MitVerfuegbarkeit(pW, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN))[0];
                }
                double G(double v) => v - v0 - kr * (thetaRef - Bei(v).ThetaAirMittel);
                double lo = v0 - 40.0, hi = v0 + 60.0;
                Assert.True(G(lo) < 0.0 && G(hi) > 0.0, "Stunde " + h + ": keine Klammer");
                for (int i = 0; i < 60; i++)
                {
                    double m = 0.5 * (lo + hi);
                    if (G(m) > 0.0) hi = m; else lo = m;
                }
                double vStern = 0.5 * (lo + hi);
                Stundenergebnis soll = Bei(vStern);

                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Stundenergebnis ist = k.Loesung[0][0];
                kreis.Festschreiben(h);

                befund.Stunden++;
                if (k.SchrankeGreift) befund.Begrenzt++;
                befund.DurchlaeufeMax = Math.Max(befund.DurchlaeufeMax, k.Durchlaeufe);
                befund.DvMax = Math.Max(befund.DvMax, Math.Abs(k.VorlaufKreisC - vStern));
                if (Math.Abs(ist.ThetaAirMittel - soll.ThetaAirMittel) > befund.DthetaMax) befund.Schlimmste = string.Format("h {0} k {1} V {2} V* {3} Φ {4} Φ* {5} S {6} probe {7} θ {8} θ* {9} θref {10}", h, k.Durchlaeufe, k.VorlaufKreisC, vStern, ist.HeizleistungW, soll.HeizleistungW, k.Angebot.LeistungKw, thetaRef, ist.ThetaAirMittel, soll.ThetaAirMittel, thetaRef);
                befund.DthetaMax = Math.Max(befund.DthetaMax, Math.Abs(ist.ThetaAirMittel - soll.ThetaAirMittel));
                befund.DphiMax = Math.Max(befund.DphiMax, Math.Abs(ist.HeizleistungW - soll.HeizleistungW) * faktor);
                Assert.Equal(Bits(k.HeizlastW[0]), Bits(ist.HeizleistungW * faktor));
            }
            ergebnis = s.Abschluss(0, 1)[0];
            return befund;
        }

        private void Melden(string name, Orakelbefund b, Anlagenkopplung k)
            => _aus.WriteLine("{0}: {1} h, begrenzt {2} h, Durchläufe max {3} (Mittel {4:0.000}), |ΔV| {5:0.000000} K, " +
                              "|Δθ| {6:0.000000} K, |ΔΦ| {7:0.000000} W, Stützstellenwechsel {8}, Fallwechsel {9}",
                              name, b.Stunden, b.Begrenzt, b.DurchlaeufeMax, k.DurchlaeufeMittel, b.DvMax, b.DthetaMax, b.DphiMax,
                              k.StuetzstellenWechsel, k.FallWechsel + " | " + b.Schlimmste);

        private static void ImToleranzband(Orakelbefund b)
        {
            Assert.True(b.Begrenzt > 100, "zu wenige begrenzte Stunden: " + b.Begrenzt);
            Assert.True(b.DvMax <= TOL_VORLAUF_K, "Vorlauf " + b.DvMax);
            Assert.True(b.DthetaMax <= TOL_THETA_K, "Raumluft " + b.DthetaMax);
            Assert.True(b.DphiMax <= TOL_PHI_W, "Leistung " + b.DphiMax);
            Assert.True(b.DurchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
        }

        // ------------------------------------------------------------------------------------------
        //  O1, O1p, O2
        // ------------------------------------------------------------------------------------------

        [Fact]
        public void O1_lineare_Kapazitaet_trifft_die_Bisektionsloesung()
        {
            double spitzeKw = SpitzeW() / 1000.0;
            // Kapazität 70 % der Spitze bei 40 °C, je Kelvin 2 % der Spitze weniger; k_R = 2 K/K.
            Func<double, double> p = v => Math.Max(0.7 * spitzeKw - 0.02 * spitzeKw * (v - 40.0), 0.0);
            Orakelbefund b = Orakel(p, new Kennkapazitaet(p, 40.0), 40.0, 2.0, 1.0, null, out Anlagenkopplung k, out _);
            Melden("O1", b, k);
            ImToleranzband(b);
            Assert.True(k.DurchlaeufeMax > 2, "H2 wirkt nicht zurück");
        }

        /// <summary>Die Handrechnung der Kapazität (drei Kennlinien 35/45/55 °C bei 0 °C Quelle: 10/9/8 kW, I-1, I-2).</summary>
        private static double HandKw(double v) => v <= 35.0 ? 10.0 : v >= 55.0 ? 8.0 : 10.0 - 0.1 * (v - 35.0);

        private static SimulationWaermepumpe._Kenndaten Kurve(int vorlauf, params (double t, double cop, double p)[] punkte)
            => new SimulationWaermepumpe._Kenndaten
            {
                Vorlauf = vorlauf,
                anz = punkte.Length,
                dat = punkte.Select(x => new SimulationWaermepumpe._DAT { Temperatur = x.t, COP = x.cop, Leistung = x.p }).ToArray(),
            };

        private static WaermepumpeKapazitaet Wp() => new WaermepumpeKapazitaet(new Fahrplanerzeuger { Bezeichner = "WP" },
            new[]
            {
                Kurve(35, (10, 5.0, 12.0), (0, 4.0, 10.0), (-10, 3.0, 8.0)),
                Kurve(45, (10, 4.0, 11.0), (0, 3.2, 9.0), (-10, 2.4, 7.0)),
                Kurve(55, (10, 3.0, 10.0), (0, 2.5, 8.0), (-10, 2.0, 6.0)),
            }, null, new Quellprofil(Enumerable.Repeat(0.0, 8760).ToArray()), true, 1, true);

        [Fact]
        public void O1p_Produktweg_mit_echter_Kennlinie_gegen_Handrechnung_und_byte_gleich()
        {
            WaermepumpeKapazitaet wp = Wp();
            foreach (double v in new[] { 30.0, 35.0, 40.0, 44.0, 45.0, 47.5, 55.0, 60.0 })
                Assert.True(Math.Abs(wp.Abfragen(0, v).KapazitaetKw - HandKw(v)) <= 1e-12, "Kapazität am Vorlauf " + v);

            // Das Gebäude im Maßstab, in dem die WP in den kalten Stunden um rund 30 % zu klein ist.
            double faktor = 1.3 * 9000.0 / SpitzeW();
            Orakelbefund b = Orakel(HandKw, wp, 45.0, 2.0, faktor, wp.Stuetzstelle, out Anlagenkopplung k1, out GebaeudeModellErgebnis e1);
            Melden("O1p", b, k1);
            ImToleranzband(b);
            Assert.Equal(0, k1.StundenFestgehalten);

            // Kapazität am Vorlauf (Konzept 11.1): das Angebot jeder Stunde ist die Handrechnung am Vorlauf des Kreises.
            Orakel(HandKw, Wp(), 45.0, 2.0, faktor, null, out Anlagenkopplung k2, out GebaeudeModellErgebnis e2);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(Bits(e1.HeizlastW[h]), Bits(e2.HeizlastW[h]));
                Assert.Equal(Bits(e1.Raumtemperatur[h]), Bits(e2.Raumtemperatur[h]));
            }
            Assert.Equal(k1.DurchlaeufeVerteilung, k2.DurchlaeufeVerteilung);
        }

        [Fact]
        public void Kapazitaet_am_Vorlauf_das_Angebot_folgt_der_Kennlinie_am_Vorlauf_des_Kreises()
        {
            WaermepumpeKapazitaet wp = Wp();
            double faktor = 1.3 * 9000.0 / SpitzeW();
            var h2 = new H2Double(45.0, 2.0);
            GebaeudeStepper s = Stepper();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, faktor) }, new IErzeugerkapazitaet[] { wp }, null)
            { Vorlaufkorrektur = h2.Naht };
            int geprueft = 0;
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                kreis.Festschreiben(h);
                // Das Angebot der Lösung ist die Handrechnung am Vorlauf des Kreises — bis auf das Abbruchmaß der Schranke.
                Assert.True(Math.Abs(k.Angebot.ErzeugerKw - HandKw(k.VorlaufKreisC)) <= Anlagenkopplung.ABBRUCH_SCHRANKE_W / 1000.0 + 1e-12,
                            "Stunde " + h);
                if (k.VorlaufKreisC != 45.0) geprueft++;
            }
            Assert.True(geprueft > 100, "Vorlauf bewegt sich nicht: " + geprueft);
        }

        [Fact]
        public void O2_konstante_Kapazitaet_ohne_H2_ist_bitgleich_zu_AK2_mit_Schranke_P()
        {
            double pKw = 0.6 * SpitzeW() / 1000.0;
            GebaeudeStepper s = Stepper();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                            new IErzeugerkapazitaet[] { new Kennkapazitaet(_ => pKw, 40.0) }, null);
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                kreis.Festschreiben(h);
                Assert.True(k.Durchlaeufe == 1, "Stunde " + h + ": " + k.Durchlaeufe + " Durchläufe");
            }
            GebaeudeModellErgebnis ist = s.Abschluss(0, 1)[0];

            // AK2 mit Schranke = P je Stunde des Jahres; eingeschwungen unbegrenzt wie jeder Stepper des AK3-Wegs
            // (Festlegung 16) — der Jahreslauf mit Fahrplanreihe schwänge mit der Schranke ein.
            GebaeudeStepper ak2 = Stepper();
            for (int h = 0; h < 8760; h++)
            {
                ak2.Schritt(h, (int z, int st, in Stundenrand r) =>
                    r.MitVerfuegbarkeit(pKw * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN));
                ak2.Festschreiben(h);
            }
            GebaeudeModellErgebnis soll = ak2.Abschluss(0, 1)[0];
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(Bits(soll.HeizlastW[h]) == Bits(ist.HeizlastW[h]), "Heizlast, Stunde " + h);
                Assert.True(Bits(soll.Raumtemperatur[h]) == Bits(ist.Raumtemperatur[h]), "Raumluft, Stunde " + h);
            }
            Assert.True(kreis.StundenAnDerSchranke > 100, "Schranke greift zu selten: " + kreis.StundenAnDerSchranke);
            Assert.Equal(8760, kreis.DurchlaeufeVerteilung[1]);
            _aus.WriteLine("O2: 8760 h mit einem Durchlauf, Schranke greift in {0} h", kreis.StundenAnDerSchranke);
        }

        /// <summary>
        /// Warum auch der erste Durchlauf begrenzt rechnet: Eine Schranke ohne Grenze (1 GW) rechnet Bit für Bit wie ohne
        /// Schranke; eine Schranke über dem Stundenmittel des unbegrenzten Schritts greift dagegen in manchen Stunden doch
        /// (der Löser begrenzt innerhalb der Stunde) — das Stundenmittel ist kein Kriterium für „passt“.
        /// </summary>
        [Fact]
        public void Schranke_ohne_Grenze_ist_bitgleich_das_Stundenmittel_ist_kein_Kriterium()
        {
            double pKw = 0.6 * SpitzeW() / 1000.0;
            GebaeudeStepper s = Stepper();
            int ueberMittel = 0;
            for (int h = 0; h < 8760; h++)
            {
                Stundenergebnis u = s.Schritt(h)[0];
                Stundenergebnis b = s.Schritt(h, (int z, int st, in Stundenrand r) =>
                    r.MitVerfuegbarkeit(pKw * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN))[0];
                if (u.HeizleistungW <= pKw * 1000.0 && Bits(u.HeizleistungW) != Bits(b.HeizleistungW)) ueberMittel++;
                Stundenergebnis g = s.Schritt(h, (int z, int st, in Stundenrand r) =>
                    r.MitVerfuegbarkeit(1.0e9, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN))[0];
                Assert.True(Bits(u.HeizleistungW) == Bits(g.HeizleistungW) && Bits(u.ThetaAirMittel) == Bits(g.ThetaAirMittel),
                            "1 GW, Stunde " + h);
                s.Schritt(h);
                s.Festschreiben(h);
            }
            Assert.True(ueberMittel > 0, "Das Stundenmittel wäre doch ein Kriterium");
            _aus.WriteLine("Schranke über dem Stundenmittel und dennoch wirksam: {0} h", ueberMittel);
        }

        [Fact]
        public void O2_konstante_Kapazitaet_mit_H2_Testdouble_trifft_die_Bisektionsloesung()
        {
            double pKw = 0.6 * SpitzeW() / 1000.0;
            Orakelbefund b = Orakel(_ => pKw, new Kennkapazitaet(_ => pKw, 40.0), 40.0, 2.0, 1.0, null, out Anlagenkopplung k, out _);
            Melden("O2 mit H2", b, k);
            ImToleranzband(b);
        }

        // ------------------------------------------------------------------------------------------
        //  Fallwechsel, benannter Fehler, Determinismus
        // ------------------------------------------------------------------------------------------

        /// <summary>Stufenkapazität mit zwei Stützstellen (unter 45 °C knapp, darüber reichlich) und kräftigem H2 — sie pendelt.</summary>
        private static Anlagenkopplung Pendelkreis(bool halten, out GebaeudeStepper s, out H2Double h2)
        {
            double spitzeKw = SpitzeW() / 1000.0;
            Func<double, double> p = v => v < 45.0 ? 0.3 * spitzeKw : 2.0 * spitzeKw;
            h2 = new H2Double(44.0, 20.0);
            s = Stepper();
            return new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 7, "Pendelhaus", s, 1.0) },
                                       new IErzeugerkapazitaet[] { new Kennkapazitaet(p, 44.0) }, null)
            {
                Vorlaufkorrektur = h2.Naht,
                Stuetzstelle = v => v < 45.0 ? 0 : 1,
                StuetzstelleHalten = halten,
            };
        }

        [Fact]
        public void Pendelnde_Stuetzstelle_wird_aus_Durchlauf_1_festgehalten_und_gezaehlt()
        {
            Anlagenkopplung kreis = Pendelkreis(true, out GebaeudeStepper s, out H2Double h2);
            int festgehalten = 0;
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = MitReferenz(kreis, s, h2, h);
                kreis.Festschreiben(h);
                if (!k.Festgehalten) continue;
                festgehalten++;
                Assert.Equal(44.0, k.VorlaufKreisC);
                Assert.True(k.Durchlaeufe <= 4, "Stunde " + h + ": " + k.Durchlaeufe);
            }
            Assert.True(festgehalten > 0, "kein Pendeln erzwungen");
            Assert.Equal(festgehalten, kreis.StundenFestgehalten);
            Assert.True(kreis.StuetzstellenWechsel >= 2 * festgehalten);
            _aus.WriteLine("Pendeln: {0} h festgehalten, {1} Stützstellenwechsel, Durchläufe max {2}",
                           festgehalten, kreis.StuetzstellenWechsel, kreis.DurchlaeufeMax);
        }

        [Fact]
        public void Erzwungener_Wechsel_des_Betriebsfalls_wird_gezaehlt()
        {
            double spitzeKw = SpitzeW() / 1000.0;
            GebaeudeStepper s = Stepper();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                            new IErzeugerkapazitaet[] { new ErstKnapp(0.2 * spitzeKw, 2.0 * spitzeKw) }, null);
            int mitWechsel = 0;
            for (int h = 0; h < 8760; h++)
            {
                int vorher = kreis.FallWechsel;
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                kreis.Festschreiben(h);
                if (kreis.FallWechsel > vorher) mitWechsel++;
                Assert.True(k.Durchlaeufe <= 4, "Stunde " + h + ": " + k.Durchlaeufe);
            }
            Assert.True(mitWechsel > 0, "kein Fallwechsel erzwungen");
            _aus.WriteLine("Betriebsfall: {0} Wechsel in {1} h", kreis.FallWechsel, mitWechsel);
        }

        [Fact]
        public void Ohne_Festhalten_endet_das_Pendeln_im_benannten_Fehler()
        {
            Anlagenkopplung kreis = Pendelkreis(false, out GebaeudeStepper s, out H2Double h2);
            AnlagenkopplungException f = null;
            for (int h = 0; h < 8760 && f == null; h++)
            {
                try
                {
                    MitReferenz(kreis, s, h2, h);
                    kreis.Festschreiben(h);
                }
                catch (AnlagenkopplungException ex) { f = ex; }
            }
            Assert.NotNull(f);
            Assert.Contains("Pendelhaus (7)", f.Gebaeude);
            Assert.Contains("Probeerzeuger", f.Beteiligte);
            Assert.Contains(Anlagenkopplung.HOECHSTZAHL + " Durchläufe", f.LetzterStand);
            // W4b: die Meldung kommt aus den Ressourcen (Sprache des Läufers) - sie nennt Stunde und Gebäude.
            Assert.Contains(" " + f.Stunde.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ", f.Message);
            Assert.Contains(f.Gebaeude, f.Message);
            Assert.DoesNotContain("Produkt", f.Abweichung);
            Assert.DoesNotContain("θ 0 K, Φ 0 W", f.Abweichung);
            _aus.WriteLine(f.Message);
        }

        [Fact]
        public void Schutzgrenze_Hoechstzahl_20_und_Produkt_120()
        {
            Assert.False(Anlagenkopplung.GrenzeErreicht(1, 20));
            Assert.True(Anlagenkopplung.GrenzeErreicht(1, 21));
            Assert.False(Anlagenkopplung.GrenzeErreicht(2, 20));
            Assert.True(Anlagenkopplung.GrenzeErreicht(2, 21));
            Assert.False(Anlagenkopplung.GrenzeErreicht(7, 17));
            Assert.True(Anlagenkopplung.GrenzeErreicht(7, 18));
            Assert.False(Anlagenkopplung.GrenzeErreicht(60, 2));
            Assert.True(Anlagenkopplung.GrenzeErreicht(61, 2));
        }

        [Fact]
        public void Determinismus_zwei_Laeufe_mit_Pendeln_sind_byte_gleich()
        {
            GebaeudeModellErgebnis Lauf(out Anlagenkopplung k)
            {
                k = Pendelkreis(true, out GebaeudeStepper s, out H2Double h2);
                for (int h = 0; h < 8760; h++)
                {
                    MitReferenz(k, s, h2, h);
                    k.Festschreiben(h);
                }
                return s.Abschluss(0, 7)[0];
            }
            GebaeudeModellErgebnis a = Lauf(out Anlagenkopplung ka), b = Lauf(out Anlagenkopplung kb);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(Bits(a.HeizlastW[h]), Bits(b.HeizlastW[h]));
                Assert.Equal(Bits(a.Raumtemperatur[h]), Bits(b.Raumtemperatur[h]));
            }
            Assert.Equal(ka.DurchlaeufeVerteilung, kb.DurchlaeufeVerteilung);
            Assert.Equal(ka.StuetzstellenWechsel, kb.StuetzstellenWechsel);
            Assert.Equal(ka.StundenFestgehalten, kb.StundenFestgehalten);
        }

        // ------------------------------------------------------------------------------------------
        //  Konzept 11.1: konvergiert, Altweg als feste Last
        // ------------------------------------------------------------------------------------------

        [Fact]
        public void Altweg_als_feste_Last_mindert_das_Angebot_des_Kreises()
        {
            double spitzeKw = SpitzeW() / 1000.0;
            double pKw = 1.2 * spitzeKw, altKw = 0.5 * spitzeKw;
            GebaeudeStepper s = Stepper();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                            new IErzeugerkapazitaet[] { new Kennkapazitaet(_ => pKw, 40.0) }, null);
            var vorrang = new Stundenvorrang(0.0, 0.0, 0.0, altKw);
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, vorrang);
                kreis.Festschreiben(h);
                Assert.Equal(pKw - altKw, k.Angebot.LeistungKw, 12);
                Assert.True(k.HeizlastW[0] <= (pKw - altKw) * 1000.0 + 1e-6, "Stunde " + h);
            }
            Assert.True(kreis.StundenAnDerSchranke > 0, "Die feste Last mindert nichts");
        }

        // ------------------------------------------------------------------------------------------
        //  Laufzeitprobe (E102 Q-AK3-5)
        // ------------------------------------------------------------------------------------------

        [Fact]
        public void Laufzeit_AK3_hoechstens_Faktor_3_und_100_ms_je_Gebaeude_und_Jahr()
        {
            double schrankeKw = 0.8 * SpitzeW() / 1000.0;
            double Ohne()
            {
                var uhr = Stopwatch.StartNew();
                GebaeudeStepper s = Stepper();
                s.Jahr();
                s.Abschluss(0, 1);
                return uhr.Elapsed.TotalMilliseconds;
            }
            double Mit(out Anlagenkopplung kreis)
            {
                var uhr = Stopwatch.StartNew();
                GebaeudeStepper s = Stepper();
                kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                            new IErzeugerkapazitaet[] { new Kennkapazitaet(_ => schrankeKw, 40.0) }, null);
                for (int h = 0; h < 8760; h++)
                {
                    kreis.Stunde(h, double.NaN, default);
                    kreis.Festschreiben(h);
                }
                s.Abschluss(0, 1);
                return uhr.Elapsed.TotalMilliseconds;
            }
            // Aufwärmen (JIT), dann das Beste aus drei Läufen.
            Ohne();
            Mit(out _);
            double ohne = double.MaxValue, mit = double.MaxValue;
            Anlagenkopplung k = null;
            for (int i = 0; i < 3; i++)
            {
                ohne = Math.Min(ohne, Ohne());
                mit = Math.Min(mit, Mit(out k));
            }
            double faktor = mit / ohne;
            _aus.WriteLine("Laufzeit Einzone: ohne Kreis {0:0.0} ms, AK3 {1:0.0} ms, Faktor {2:0.00} (Grenze 3, Abbruch 15); " +
                           "Durchläufe Mittel {3:0.000}, Schranke {4} h", ohne, mit, faktor, k.DurchlaeufeMittel, k.StundenAnDerSchranke);
            Assert.True(k.StundenAnDerSchranke > 0);
            // Die Probe scheitert erst beim Fünffachen der Grenze (Rauschen auf Läufern).
            Assert.True(faktor <= 5 * 3.0, "Faktor " + faktor);
            Assert.True(mit <= 5 * 100.0, "AK3 " + mit + " ms");
        }

        // ------------------------------------------------------------------------------------------
        //  Mehrzonen im Kreis (Entwurf 2.6: Anlage außen, Zonen innen)
        // ------------------------------------------------------------------------------------------

        /// <summary>Zwei gleiche Hälften des Probegebäudes, gekoppelt (Radiator 55/45 °C ohne Heizkurve).</summary>
        private static ProjektGebaeudeModel Zweizonen()
        {
            ProjektGebaeudeModel g = ZonenschleifeTests.Haelften(Trennflaechenzuordnung.Innen);
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = false;
            g.Auslegung_Vorlauf = 55.0;
            g.Auslegung_Ruecklauf = 45.0;
            g.Heizung_Strahlungsanteil = 0.3;
            return g;
        }

        private static GebaeudeModellErgebnis ZweizonenImKreis(double kw, out Anlagenkopplung kreis)
        {
            ProjektGebaeudeModel g = Zweizonen();
            var s = GebaeudeStepper.Mehrzonen(new Zonenschleife(
                ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes(), false, DbWerte.ANLAGENKOPPLUNG_AK1), "Zweizonen"));
            s.Beginnen();
            kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, g.ID_Gebaeude, "Zweizonen", s, 1.0) },
                new IErzeugerkapazitaet[] { new FesteKapazitaet(new Fahrplanerzeuger { Bezeichner = "Kessel", NennleistungKw = kw }) }, null);
            for (int h = 0; h < 8760; h++)
            {
                kreis.Stunde(h, double.NaN, default);
                kreis.Festschreiben(h);
            }
            return Zonenrechnung.Abschluss(s, 0, g.ID_Gebaeude);
        }

        [Fact]
        public void Zweizonen_ohne_Grenzen_ist_der_Kreis_bitgleich_zu_AK1z()
        {
            ProjektGebaeudeModel g = Zweizonen();
            Mehrzonenergebnis soll = Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), false, DbWerte.ANLAGENKOPPLUNG_AK1, 0, g.ID_Gebaeude);
            GebaeudeModellErgebnis ist = ZweizonenImKreis(1.0e6, out Anlagenkopplung kreis);
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(Bits(soll.Gebaeude.HeizlastW[h]) == Bits(ist.HeizlastW[h]), "Heizlast, Stunde " + h);
                Assert.True(Bits(soll.Gebaeude.Raumtemperatur[h]) == Bits(ist.Raumtemperatur[h]), "Raumluft, Stunde " + h);
                for (int z = 0; z < soll.Zonen.Count; z++)
                    Assert.True(Bits(soll.Zonen[z].HeizlastW[h]) == Bits(ist.Zonen[z].Ergebnis.HeizlastW[h]), "Zone " + z + ", Stunde " + h);
            }
            Assert.Equal(Bits(soll.Gebaeude.JahresheizwaermeMwh), Bits(ist.JahresheizwaermeMwh));
            Assert.Equal(8760, kreis.DurchlaeufeVerteilung[1]);
            Assert.Equal(0, kreis.StundenAnDerSchranke);
        }

        [Fact]
        public void Zweizonen_mit_Schranke_haelt_die_Summe_der_Zonen()
        {
            ProjektGebaeudeModel g = Zweizonen();
            double spitzeKw = Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), false, DbWerte.ANLAGENKOPPLUNG_AK1, 0,
                                                    g.ID_Gebaeude).Gebaeude.HeizlastW.Max() / 1000.0;
            double schrankeKw = 0.6 * spitzeKw;
            GebaeudeModellErgebnis ist = ZweizonenImKreis(schrankeKw, out Anlagenkopplung kreis);
            for (int h = 0; h < 8760; h++)
                Assert.True(ist.HeizlastW[h] <= schrankeKw * 1000.0 + 1e-6, "Stunde " + h + ": " + ist.HeizlastW[h] + " W");
            Assert.True(kreis.StundenAnDerSchranke > 100, "Schranke greift zu selten: " + kreis.StundenAnDerSchranke);
            Assert.True(!Anlagenkopplung.GrenzeErreicht(2, kreis.DurchlaeufeMax));
            _aus.WriteLine("Zweizonen mit Schranke: {0} h an der Schranke, Durchläufe max {1} (Mittel {2:0.000})",
                           kreis.StundenAnDerSchranke, kreis.DurchlaeufeMax, kreis.DurchlaeufeMittel);
        }
        // ------------------------------------------------------------------------------------------
        //  O1k, O2k — die Kälteseite im Kreis (AK3-K K3; Entwurf AK3-K 4.1, 4.4, Festlegungen 14, 15)
        // ------------------------------------------------------------------------------------------

        private const double KUEHL_SOLL_C = 23.0, KUEHL_VORLAUF_C = 16.0, WAERME_OHNE_GRENZE_KW = 1.0e6;
        private const double TOL_THETA_KAELTE_K = 0.0001;

        /// <summary>Das Probegebäude mit wirksamer Kühlung (Kühlsollwert 23 °C, ideale Kühlung) im Kreis.</summary>
        private static GebaeudeStepper KuehlStepper()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Nachtabsenkung = g.Raumsolltemperatur_Tag;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = KUEHL_SOLL_C;
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, true, DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            GebaeudeStepper s = GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e));
            s.Beginnen();
            return s;
        }

        /// <summary>Die Kühlspitze des unbegrenzten Jahres [W].</summary>
        private static double KuehlspitzeW()
        {
            GebaeudeStepper s = KuehlStepper();
            double spitze = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                spitze = Math.Max(spitze, s.Schritt(h)[0].KuehlleistungW);
                s.Festschreiben(h);
            }
            return spitze;
        }

        /// <summary>Ein Kälteerzeuger konstanter Kapazität (zustandsfrei, am festen Kühlvorlauf).</summary>
        private sealed class Kaeltekonstante : IKaelteerzeugerkapazitaet
        {
            private readonly double _kw;
            internal Kaeltekonstante(double kw) { _kw = kw; }
            public string Bezeichner => "Probekälte";
            public Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC)
                => new Erzeugerangebot(_kw, _kw, Verfuegbarkeitsgrund.KeineBegrenzung, true, kuehlVorlaufC, false);
        }

        private static Kaeltestundenangebot KaelteBei(double pW)
            => new Kaeltestundenangebot(pW / 1000.0, 0.0, 0.0, KUEHL_VORLAUF_C, Verfuegbarkeitsgrund.KeineBegrenzung);

        /// <summary>Der Kreis mit unbegrenzter Wärmeseite und der Kälteschranke <paramref name="pKw"/>.</summary>
        private static Anlagenkopplung Kaeltekreis(GebaeudeStepper s, double pKw)
            => new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                   new IErzeugerkapazitaet[] { new Kennkapazitaet(_ => WAERME_OHNE_GRENZE_KW, 40.0) }, null)
            {
                Kaelteschranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { new Kaeltekonstante(pKw) }, null, KUEHL_VORLAUF_C),
            };

        /// <summary>Der Schritt der Stunde an der Kälteschranke <paramref name="pW"/> — wie der Kreis ihn stellt.</summary>
        private static Stundenergebnis KaelteSchritt(GebaeudeStepper s, int h, double pW)
            => s.Schritt(h, (int z, int st, in Stundenrand r) =>
                   r.MitVerfuegbarkeit(WAERME_OHNE_GRENZE_KW * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN)
                    .MitKaelteverfuegbarkeit(pW, Verfuegbarkeitsgrund.KeineBegrenzung, KUEHL_VORLAUF_C))[0];

        /// <summary>
        /// <b>O1k</b>: die Kälteschranke als stetige Testkennlinie über die Naht <see cref="Anlagenkopplung.Kaeltekorrektur"/>
        /// — P(θ_i) = P₀ + a · (θ_i − θ_kühl), die Kapazität steigt mit der Raumluft des letzten Durchlaufs (wärmerer
        /// Rücklauf). Die Kälteschranke greift; der Kreis trifft je Stunde die Bisektionslösung von
        /// g(P) = P − P₀ − a · (θ_i(P) − θ_kühl) = 0 auf 0,0001 K und 0,1 W.
        /// </summary>
        [Fact]
        public void O1k_stetige_Kaeltekennlinie_ueber_die_Naht_trifft_die_Bisektionsloesung()
        {
            double spitzeW = KuehlspitzeW();
            double p0W = 0.5 * spitzeW, aWK = 0.05 * spitzeW;
            Func<double, double> pVon = theta => Math.Max(p0W + aWK * (theta - KUEHL_SOLL_C), 0.0);
            GebaeudeStepper s = KuehlStepper();
            Anlagenkopplung kreis = Kaeltekreis(s, p0W / 1000.0);
            kreis.Kaeltekorrektur = (h, a, l) => l == null ? a : KaelteBei(pVon(l[0][0].ThetaAirMittel));

            int gegriffen = 0, durchlaeufeMax = 0;
            double dThetaMax = 0.0, dPhiMax = 0.0;
            string schlimmste = "";
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Stundenergebnis ist = k.Loesung[0][0];
                if (k.KaelteschrankeGreift)
                {
                    // Die Bisektion am selben Stepper (die Schritte werden zurückgesetzt), danach der offene Schritt neu.
                    double G(double pW) => pW - pVon(KaelteSchritt(s, h, pW).ThetaAirMittel);
                    double lo = 0.0, hi = 4.0 * spitzeW;
                    Assert.True(G(lo) < 0.0 && G(hi) > 0.0, "Stunde " + h + ": keine Klammer");
                    for (int i = 0; i < 80; i++)
                    {
                        double m = 0.5 * (lo + hi);
                        if (G(m) > 0.0) hi = m; else lo = m;
                    }
                    Stundenergebnis soll = KaelteSchritt(s, h, 0.5 * (lo + hi));
                    double dt = Math.Abs(ist.ThetaAirMittel - soll.ThetaAirMittel);
                    double dp = Math.Abs(ist.KuehlleistungW - soll.KuehlleistungW);
                    if (dt > dThetaMax) schlimmste = string.Format("h {0} k {1} θ {2} θ* {3} Φc {4} Φc* {5}", h, k.Durchlaeufe,
                                                                   ist.ThetaAirMittel, soll.ThetaAirMittel, ist.KuehlleistungW, soll.KuehlleistungW);
                    dThetaMax = Math.Max(dThetaMax, dt);
                    dPhiMax = Math.Max(dPhiMax, dp);
                    gegriffen++;
                    // Der offene Schritt ist wieder die Lösung des Kreises (letzter Durchlauf).
                    Stundenergebnis wieder = KaelteSchritt(s, h, k.Kaelteangebot.Value.LeistungKw * 1000.0);
                    Assert.Equal(Bits(ist.ThetaAirMittel), Bits(wieder.ThetaAirMittel));
                }
                durchlaeufeMax = Math.Max(durchlaeufeMax, k.Durchlaeufe);
                kreis.Festschreiben(h);
            }
            _aus.WriteLine("O1k: Kälteschranke gegriffen {0} h (Kreis {1}), Durchläufe max {2} (Mittel {3:0.000}), |Δθ| {4:0.0000000} K, " +
                           "|ΔΦc| {5:0.000000} W, Fallwechsel {6} | {7}", gegriffen, kreis.StundenAnDerKaelteschranke, durchlaeufeMax,
                           kreis.DurchlaeufeMittel, dThetaMax, dPhiMax, kreis.FallWechsel, schlimmste);
            Assert.True(gegriffen > 100, "Kälteschranke greift zu selten: " + gegriffen);
            Assert.True(durchlaeufeMax > 2, "die Naht wirkt nicht zurück");
            Assert.True(durchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            Assert.True(dThetaMax <= TOL_THETA_KAELTE_K, "Raumluft " + dThetaMax);
            Assert.True(dPhiMax <= TOL_PHI_W, "Kühlleistung " + dPhiMax);
        }

        /// <summary>
        /// <b>O1kk</b> (KK2; Entwurf KK 2.4, 2.5, Festlegung 11): die Kälteschranke am STUNDENvorlauf mit der echten Kette —
        /// Kühlkennlinie als Schar über alle Vorläufe, Wärmepumpe als Kapazität am Vorlauf (<c>AbfragenAmVorlauf</c>),
        /// Kälteschranke mit Vorlauf-Argument. Der Vorlauf folgt der Raumluft des letzten Durchlaufs,
        /// V(θ_i) = clamp(11 °C + 2 K/K · (θ_i − θ_kühl), 7 °C, 18 °C), und kreuzt die Stützstelle 12 °C; die Kälteleistung ist
        /// stetig über den Vorlauf und steigt mit ihm, also mit der Raumluft (Muster O1k) — g(P) = P − P_kap(V(θ_i(P))) ist streng
        /// monoton, der Fixpunkt eindeutig. Der Kreis trifft je Stunde die Bisektionslösung auf 0,0001 K und 0,1 W.
        /// </summary>
        [Fact]
        public void O1kk_Kaelteschranke_am_Stundenvorlauf_trifft_die_Bisektionsloesung()
        {
            double spitzeW = KuehlspitzeW();
            const double V0 = 12.0, VB = 11.0, K_VORLAUF = 2.0, V_MIN = 7.0, V_MAX = 18.0, T_QUELLE = 27.5;
            Func<double, double> vorlaufVon = theta => Math.Min(V_MAX, Math.Max(V_MIN, VB + K_VORLAUF * (theta - KUEHL_SOLL_C)));
            // P(12 °C, 27,5 °C) = skala · 1,15 · 0,925 = halbe Kühlspitze.
            List<KuehlkennlinienZeile> zeilen = KaelteStundenvorlaufTests.Zeilen(0.5 * spitzeW / 1000.0 / (1.15 * 0.925));
            var wp = new Kaelteerzeuger
            {
                Bezeichner = "WP", Modulindex = 0, Kennlinie = Kuehlkennlinie.Bilden(zeilen, (int)V0, true),
                Schar = new KuehlkennlinienSchar(zeilen), KuehlVorlaufC = V0,
                Quelltemperatur = Enumerable.Repeat(T_QUELLE, 8760).ToArray(), Zeitanteil = Enumerable.Repeat(1.0, 8760).ToArray(),
            };
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { new WaermepumpeKaeltekapazitaet(wp, null, null, true) },
                                              null, V0);
            Func<int, double, double> pVon = (h, theta) => schranke.Angebot(h, default, vorlaufVon(theta)).LeistungKw * 1000.0;
            GebaeudeStepper s = KuehlStepper();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                            new IErzeugerkapazitaet[] { new Kennkapazitaet(_ => WAERME_OHNE_GRENZE_KW, 40.0) }, null)
            {
                Kaelteschranke = schranke,
                Kaeltekorrektur = (h, a, l) => l == null ? a : schranke.Angebot(h, default, vorlaufVon(l[0][0].ThetaAirMittel)),
            };

            int gegriffen = 0, durchlaeufeMax = 0, unter = 0, ueber = 0;
            double dThetaMax = 0.0, dPhiMax = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Stundenergebnis ist = k.Loesung[0][0];
                if (k.KaelteschrankeGreift)
                {
                    double G(double pW) => pW - pVon(h, KaelteSchritt(s, h, pW).ThetaAirMittel);
                    double lo = 0.0, hi = 4.0 * spitzeW;
                    Assert.True(G(lo) < 0.0 && G(hi) > 0.0, "Stunde " + h + ": keine Klammer");
                    for (int i = 0; i < 80; i++)
                    {
                        double m = 0.5 * (lo + hi);
                        if (G(m) > 0.0) hi = m; else lo = m;
                    }
                    Stundenergebnis soll = KaelteSchritt(s, h, 0.5 * (lo + hi));
                    dThetaMax = Math.Max(dThetaMax, Math.Abs(ist.ThetaAirMittel - soll.ThetaAirMittel));
                    dPhiMax = Math.Max(dPhiMax, Math.Abs(ist.KuehlleistungW - soll.KuehlleistungW));
                    double v = vorlaufVon(soll.ThetaAirMittel);
                    if (v < V0) unter++;
                    else if (v > V0) ueber++;
                    gegriffen++;
                    Stundenergebnis wieder = KaelteSchritt(s, h, k.Kaelteangebot.Value.LeistungKw * 1000.0);
                    Assert.Equal(Bits(ist.ThetaAirMittel), Bits(wieder.ThetaAirMittel));
                }
                durchlaeufeMax = Math.Max(durchlaeufeMax, k.Durchlaeufe);
                kreis.Festschreiben(h);
            }
            _aus.WriteLine("O1kk: Kälteschranke gegriffen {0} h (Vorlauf unter / über 12 °C {1} / {2}), Durchläufe max {3} (Mittel {4:0.000}), " +
                           "|Δθ| {5:0.0000000} K, |ΔΦc| {6:0.000000} W, Fallwechsel {7}", gegriffen, unter, ueber, durchlaeufeMax,
                           kreis.DurchlaeufeMittel, dThetaMax, dPhiMax, kreis.FallWechsel);
            Assert.True(gegriffen > 100, "Kälteschranke greift zu selten: " + gegriffen);
            Assert.True(unter > 0 && ueber > 0, "der Vorlauf kreuzt die Stützstelle nicht");
            Assert.True(durchlaeufeMax > 2, "der Vorlauf wirkt nicht zurück");
            Assert.True(durchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            Assert.True(dThetaMax <= TOL_THETA_KAELTE_K, "Raumluft " + dThetaMax);
            Assert.True(dPhiMax <= TOL_PHI_W, "Kühlleistung " + dPhiMax);
        }

        /// <summary>
        /// <b>O2k</b>: ein Kälteerzeuger konstanter Leistung — der Kreis rechnet je Stunde einen Durchlauf, Bit für Bit wie
        /// „AK2 mit Kälteschranke = P“; die Kältestunde danach (Kältekaskade mit einer Wärmepumpe im Kühlbetrieb derselben
        /// konstanten Leistung) lässt keinen Kälte-Restbedarf: „Kälte-Restbedarf 0“ im einfachen Fall.
        /// </summary>
        [Fact]
        public void O2k_konstante_Kaelteleistung_bitgleich_und_Kaelte_Restbedarf_null()
        {
            double pKw = 0.6 * KuehlspitzeW() / 1000.0;
            GebaeudeStepper s = KuehlStepper();
            Anlagenkopplung kreis = Kaeltekreis(s, pKw);

            // Die Kältestunde: eine Wärmepumpe im Kühlbetrieb mit flacher Kühlkennlinie P = pKw, jeder Tag ein Kühltag.
            var zeilen = new List<KuehlkennlinienZeile>();
            int id = 1;
            foreach (int t in new[] { -20, 0, 20, 40 }) zeilen.Add(new KuehlkennlinienZeile(id++, (int)KUEHL_VORLAUF_C, t, 4.0, pKw, 100));
            var wp = new Kaelteerzeuger
            {
                Bezeichner = "WP", Modulindex = 0, Kennlinie = Kuehlkennlinie.Bilden(zeilen, (int)KUEHL_VORLAUF_C),
                Quelltemperatur = Enumerable.Repeat(10.0, 8760).ToArray(), KuehlVorlaufC = KUEHL_VORLAUF_C,
                Zeitanteil = Enumerable.Repeat(1.0, 8760).ToArray(),
            };
            var kaskade = new Kaeltekaskade
            {
                Erzeuger = new List<Kaelteerzeuger> { wp }, Kuehltage = Enumerable.Repeat(true, 365).ToArray(), ImKreis = true,
            };
            kaskade.Beginnen(false);

            int gekuehlt = 0;
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Assert.True(k.Durchlaeufe == 1, "Stunde " + h + ": " + k.Durchlaeufe + " Durchläufe");
                double kuehlW = k.Loesung[0][0].KuehlleistungW;
                Assert.True(kuehlW <= pKw * 1000.0 + 1e-6, "Kühlleistung über der Schranke in Stunde " + h);
                kreis.Festschreiben(h);
                kaskade.StundeRechnen(h, kuehlW / 1000.0);
                kreis.KaelteRestZaehlen(kaskade.Rest_stuendlich[h]);
                if (kuehlW > 0.0) gekuehlt++;
            }
            GebaeudeModellErgebnis ist = s.Abschluss(0, 1)[0];

            GebaeudeStepper ak2 = KuehlStepper();
            for (int h = 0; h < 8760; h++)
            {
                KaelteSchritt(ak2, h, pKw * 1000.0);
                ak2.Festschreiben(h);
            }
            GebaeudeModellErgebnis soll = ak2.Abschluss(0, 1)[0];
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(Bits(soll.KuehlbedarfKwh[h]) == Bits(ist.KuehlbedarfKwh[h]), "Kühlbedarf, Stunde " + h);
                Assert.True(Bits(soll.Raumtemperatur[h]) == Bits(ist.Raumtemperatur[h]), "Raumluft, Stunde " + h);
            }
            _aus.WriteLine("O2k: {0} h gekühlt, Kälteschranke gegriffen {1} h, Kälte-Restbedarf {2} h / {3:0.######} kWh",
                           gekuehlt, kreis.StundenAnDerKaelteschranke, kreis.KaelteRestStunden, kreis.KaelteRestKwh);
            Assert.True(kreis.StundenAnDerKaelteschranke > 50, "Kälteschranke greift zu selten: " + kreis.StundenAnDerKaelteschranke);
            Assert.Equal(8760, kreis.DurchlaeufeVerteilung[1]);
            Assert.Equal(0, kreis.KaelteRestStunden);
            Assert.Equal(0.0, kreis.KaelteRestKwh);
            Assert.Equal(0.0, kaskade.RestGesamtKwh);
        }
        /// <summary>
        /// <b>Pendelregel und benannter Fehler auf der Kälteseite</b> (4.4, Festlegung 15): eine Kälteschranke, die zwischen
        /// zwei Werten springt, wird aus Durchlauf 1 festgehalten und als Fallwechsel gezählt; ohne Festhalten endet das
        /// Pendeln bei der Höchstzahl im benannten Fehler, der die Kälteschranke nennt.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Pendelnde_Kaelteschranke_wird_festgehalten_sonst_benannter_Fehler(bool halten)
        {
            double spitzeW = KuehlspitzeW();
            double p1W = 0.3 * spitzeW, p2W = 0.5 * spitzeW;
            GebaeudeStepper s = KuehlStepper();
            Anlagenkopplung kreis = Kaeltekreis(s, p1W / 1000.0);
            kreis.StuetzstelleHalten = halten;
            bool zweiter = false;
            kreis.Kaeltekorrektur = (h, a, l) =>
            {
                if (l == null) { zweiter = false; return KaelteBei(p1W); }
                zweiter = !zweiter;
                return KaelteBei(zweiter ? p2W : p1W);
            };
            if (!halten)
            {
                AnlagenkopplungException ex = Assert.Throws<AnlagenkopplungException>(() => kreis.Stunde(0, double.NaN, default));
                Assert.Contains("Kälteschranke", ex.Abweichung);
                Assert.Contains("Kälteschranke", ex.LetzterStand);
                return;
            }
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                Assert.True(k.Festgehalten, "Stunde " + h);
                Assert.Equal(3, k.Durchlaeufe);
                Assert.Equal(Bits(p1W / 1000.0), Bits(k.Kaelteangebot.Value.LeistungKw));
                kreis.Festschreiben(h);
            }
            Assert.Equal(8760, kreis.StundenFestgehalten);
            Assert.True(kreis.FallWechsel >= 8760, "Fallwechsel " + kreis.FallWechsel);
        }
    }
}
