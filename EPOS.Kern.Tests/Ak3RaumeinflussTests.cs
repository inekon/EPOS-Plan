using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>H2 im Kreis und die Stufe AK3</b> (AK3-W4a; Entwurf AK3, Festlegungen 20, 22, 23; E102 Q-AK3-1, Q-AK3-2) —
    /// ohne Datenbank: „H2 aus bitgleich zu AK3 ohne H2“, Orakel O1 mit der echten H2 (Bisektion des Fixpunkts
    /// Δ = k_R · max(0, θ_soll − θ_air(Δ)) je Stunde) und die Vorstunden-Variante als Vergleich.
    /// </summary>
    public class Ak3RaumeinflussTests
    {
        private readonly ITestOutputHelper _aus;

        public Ak3RaumeinflussTests(ITestOutputHelper aus) { _aus = aus; }

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

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

        /// <summary>Kapazität als Funktion des Vorlaufs, zustandsfrei (wie im Orakel des Kreises).</summary>
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

        private static Anlagenkopplung Kreis(GebaeudeStepper s, IErzeugerkapazitaet e, Raumeinfluss h2)
            => new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) }, new[] { e }, null)
            {
                Raumeinfluss = h2,
                StuetzstelleHalten = false,
            };

        // ------------------------------------------------------------------------------------------
        //  Bitgleichheit ohne wirksamen Raumeinfluss
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>„H2 aus bitgleich zu AK3 ohne H2“</b>: ein Raumeinfluss mit k_R = 0 rechnet das ganze Jahr Bit für Bit wie
        /// der Kreis ohne Raumeinfluss — Heizlast, Vorlauf, Rücklauf, Raumluft und Durchläufe.
        /// </summary>
        [Fact]
        public void H2_aus_rechnet_bitgleich_zu_AK3_ohne_H2()
        {
            double spitzeKw = Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1).HeizlastW.Max() / 1000.0;
            Func<double, double> p = v => Math.Max(0.7 * spitzeKw - 0.02 * spitzeKw * (v - 40.0), 0.0);
            GebaeudeStepper sA = Stepper(), sB = Stepper();
            Anlagenkopplung ohne = Kreis(sA, new Kennkapazitaet(p, 40.0), null);
            Anlagenkopplung aus = Kreis(sB, new Kennkapazitaet(p, 40.0), new Raumeinfluss(new[] { 0.0 }));
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde a = ohne.Stunde(h, double.NaN, default);
                Kopplungsstunde b = aus.Stunde(h, double.NaN, default);
                ohne.Festschreiben(h);
                aus.Festschreiben(h);
                Assert.Equal(a.Durchlaeufe, b.Durchlaeufe);
                Assert.Equal(Bits(a.HeizlastW[0]), Bits(b.HeizlastW[0]));
                Assert.Equal(Bits(a.VorlaufC), Bits(b.VorlaufC));
                Assert.Equal(Bits(a.RuecklaufC), Bits(b.RuecklaufC));
                Assert.Equal(Bits(a.Loesung[0][0].ThetaAirMittel), Bits(b.Loesung[0][0].ThetaAirMittel));
                Assert.Equal(0.0, b.AnhebungK);
            }
            Assert.Equal(ohne.DurchlaeufeMittel, aus.DurchlaeufeMittel);
            Assert.Null(Raumeinfluss.AusGebaeuden(new[] { new ProjektGebaeudeModel { Heizkurve_Aktiv = true, Heizkurve_Raumeinfluss = 0.0 } }));
            Assert.Null(Raumeinfluss.AusGebaeuden(new[] { new ProjektGebaeudeModel { Heizkurve_Aktiv = false, Heizkurve_Raumeinfluss = 1.0 } }));
            Assert.Null(Raumeinfluss.AusGebaeuden(new[] { new ProjektGebaeudeModel { Heizkurve_Aktiv = true } }));
            Assert.NotNull(Raumeinfluss.AusGebaeuden(new[] { new ProjektGebaeudeModel { Heizkurve_Aktiv = true, Heizkurve_Raumeinfluss = 1.0 } }));
        }

        /// <summary>Die Anhebung im Rand: oben gekappt, nie unter dem Vorlauf der Heizkurve, ohne Übergabe derselbe Rand.</summary>
        [Fact]
        public void Die_Anhebung_im_Rand_kappt_oben_und_senkt_nie()
        {
            var u = new Uebergabekennwerte(5000.0, 1.3, 55.0, 45.0, 20.0);
            var r = new Stundenrand(0, 0, 20, double.NaN, 0, 0, 0, uebergabe: u, vorlaufC: 40.0, reglerbandK: 1.0);
            Assert.Equal(42.5, r.MitAnhebung(2.5, 55.0).VorlaufC);
            Assert.Equal(55.0, r.MitAnhebung(30.0, 55.0).VorlaufC);
            Assert.Equal(40.0, r.MitAnhebung(5.0, 38.0).VorlaufC);
            Assert.Equal(40.0, r.MitAnhebung(0.0, 55.0).VorlaufC);
            Assert.Equal(40.0, r.MitAnhebung(-1.0, 55.0).VorlaufC);
            var ohneUebergabe = new Stundenrand(0, 0, 20, double.NaN, 0, 0, 0);
            Assert.True(double.IsNaN(ohneUebergabe.MitAnhebung(3.0, 55.0).VorlaufC));
        }

        // ------------------------------------------------------------------------------------------
        //  O1 mit echter H2 und die Vorstunden-Variante
        // ------------------------------------------------------------------------------------------

        private sealed class Befund
        {
            internal int Stunden, Angehoben, DurchlaeufeMax;
            internal double DeltaMax, ResiduumMax, AnhebungSumme;
        }

        /// <summary>
        /// Das Jahr im Kreis mit echter H2; je Stunde zuerst die Bisektion des Fixpunkts am selben Stepper (Schritte werden
        /// zurückgesetzt): G(Δ) = Δ − k_R · max(0, θ_soll − θ_air(Δ)), der Rand wie im Kreis (Anhebung gekappt an θ_V,N, dann
        /// die Kapazität am Vorlauf V₀ + Δ). Danach der Kreis und der Vergleich der Anhebung und des Residuums.
        /// </summary>
        private Befund Lauf(double kr, bool vorstunde, out Anlagenkopplung kreis)
        {
            double spitzeKw = Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1).HeizlastW.Max() / 1000.0;
            // Reichliche, stetig über θ_V fallende Kapazität: die Schranke greift selten, H2 wirkt über die Übergabe.
            Func<double, double> p = v => Math.Max(1.5 * spitzeKw - 0.01 * spitzeKw * (v - 40.0), 0.0);
            GebaeudeStepper s = Stepper();
            kreis = Kreis(s, new Kennkapazitaet(p, 40.0), new Raumeinfluss(new[] { kr }) { Vorstunde = vorstunde });
            var b = new Befund();
            for (int h = 0; h < 8760; h++)
            {
                double soll = double.NaN, hk = double.NaN;
                Stundenergebnis Bei(double delta)
                {
                    return s.Schritt(h, (int z, int st, in Stundenrand r) =>
                    {
                        soll = r.ThetaSoll;
                        hk = r.VorlaufC;
                        Stundenrand a = r.MitAnhebung(delta, r.MitUebergabe ? r.Uebergabe.AuslegungVorlaufC : double.NaN);
                        double v = double.IsNaN(a.VorlaufC) ? double.NaN : a.VorlaufC;
                        return a.MitVerfuegbarkeit(p(double.IsNaN(v) ? 40.0 : v) * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN);
                    })[0];
                }
                double G(double d)
                {
                    Stundenergebnis e = Bei(d);
                    return d - (double.IsNaN(soll) ? 0.0 : kr * Math.Max(0.0, soll - e.ThetaAirMittel));
                }
                double lo = 0.0, hi = kr * 15.0;
                double dStern;
                if (G(lo) >= 0.0) dStern = 0.0;
                else
                {
                    for (int i = 0; i < 50; i++)
                    {
                        double m = 0.5 * (lo + hi);
                        if (G(m) > 0.0) hi = m; else lo = m;
                    }
                    dStern = 0.5 * (lo + hi);
                }

                Kopplungsstunde k = kreis.Stunde(h, hk, default);
                // Residuum des Kreises: die Anhebung, mit der die Lösung gerechnet wurde, gegen die, die aus ihr folgt.
                double gefolgert = double.IsNaN(soll) ? 0.0 : kr * Math.Max(0.0, soll - k.Loesung[0][0].ThetaAirMittel);
                double residuum = Math.Abs(gefolgert - k.AnhebungK);
                kreis.Festschreiben(h);

                b.Stunden++;
                if (k.AnhebungK > 0.0) b.Angehoben++;
                b.AnhebungSumme += k.AnhebungK;
                b.DurchlaeufeMax = Math.Max(b.DurchlaeufeMax, k.Durchlaeufe);
                b.DeltaMax = Math.Max(b.DeltaMax, Math.Abs(k.AnhebungK - dStern));
                b.ResiduumMax = Math.Max(b.ResiduumMax, residuum);
            }
            return b;
        }

        private void Melden(string name, Befund b, Anlagenkopplung k)
            => _aus.WriteLine("{0}: {1} h, angehoben {2} h (Mittel {3:0.000} K), Durchläufe max {4} (Mittel {5:0.000}), " +
                              "|Δ−Δ*| {6:0.0000} K, Residuum max {7:0.0000} K",
                              name, b.Stunden, b.Angehoben, b.Angehoben > 0 ? b.AnhebungSumme / b.Angehoben : 0.0,
                              b.DurchlaeufeMax, k.DurchlaeufeMittel, b.DeltaMax, b.ResiduumMax);

        /// <summary>
        /// <b>O1 mit echter H2</b>: Die Anhebung des Kreises trifft je Stunde den Fixpunkt der Bisektion bis auf das
        /// Abbruchmaß des Vorlaufs; H2 wirkt zurück (mehr als ein Durchlauf). Daneben die <b>Vorstunden-Variante</b>
        /// (Raumtemperatur der Vorstunde, nicht iteriert) als Vergleich: Sie verfehlt den Fixpunkt derselben Stunde
        /// deutlicher.
        /// </summary>
        [Fact]
        public void O1_mit_echter_H2_trifft_den_Fixpunkt_und_die_Vorstunde_als_Vergleich()
        {
            var uhr = Stopwatch.StartNew();
            Befund gleich = Lauf(1.0, false, out Anlagenkopplung kGleich);
            uhr.Stop();
            Melden("O1-H2 gleiche Stunde (" + uhr.ElapsedMilliseconds + " ms mit Bisektion)", gleich, kGleich);
            Befund vor = Lauf(1.0, true, out Anlagenkopplung kVor);
            Melden("O1-H2 Vorstunde", vor, kVor);

            Assert.True(gleich.Angehoben > 1000, "H2 hebt zu selten an: " + gleich.Angehoben);
            Assert.True(gleich.DurchlaeufeMax > 1, "H2 wirkt nicht zurück");
            Assert.True(gleich.DurchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            Assert.True(gleich.ResiduumMax <= Anlagenkopplung.ABBRUCH_VORLAUF_K, "Residuum " + gleich.ResiduumMax);
            Assert.True(gleich.DeltaMax <= Anlagenkopplung.ABBRUCH_VORLAUF_K, "Anhebung " + gleich.DeltaMax);
            Assert.True(vor.ResiduumMax > gleich.ResiduumMax, "Die Vorstunde trifft den Fixpunkt derselben Stunde nicht schlechter.");
        }

        /// <summary>Zwei Läufe mit H2 sind Bit für Bit gleich (Determinismus).</summary>
        [Fact]
        public void H2_rechnet_deterministisch()
        {
            double spitzeKw = Vdi6007Rechenweg.Laufen(Gekoppelt(), 0, 1).HeizlastW.Max() / 1000.0;
            Func<double, double> p = v => Math.Max(1.2 * spitzeKw - 0.01 * spitzeKw * (v - 40.0), 0.0);
            Anlagenkopplung a =Kreis(Stepper(), new Kennkapazitaet(p, 40.0), new Raumeinfluss(new[] { 2.0 }));
            Anlagenkopplung b = Kreis(Stepper(), new Kennkapazitaet(p, 40.0), new Raumeinfluss(new[] { 2.0 }));
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde x = a.Stunde(h, double.NaN, default);
                Kopplungsstunde y = b.Stunde(h, double.NaN, default);
                a.Festschreiben(h);
                b.Festschreiben(h);
                Assert.Equal(Bits(x.HeizlastW[0]), Bits(y.HeizlastW[0]));
                Assert.Equal(Bits(x.AnhebungK), Bits(y.AnhebungK));
            }
            Assert.True(a.Raumeinfluss.StundenAngehoben > 0);
        }
    }
}
