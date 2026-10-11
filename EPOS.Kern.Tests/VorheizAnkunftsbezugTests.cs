using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ankunftsbezug und Maximum der Option 2 als interne Wahl</b> (Welle V3b, für einen offenen Anwenderentscheid): (a) gegen
    /// θ_T − ε (Vorgabe), (b) gegen min(θ_T, θ_stat) − ε an einer Zone mit Heizkreis — θ_stat ist der Fixpunkt der Stundenabbildung
    /// des Zonenmodells unter dem Rand von h_s; unerreichbare Sprünge im Maximum t_V (Vorgabe) oder nur gezählt. Dazu die Messung
    /// an 1054, 1056, 1047, 1058 und 1051 (nur mit <c>EPOS_MESSUNG=1</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizAnkunftsbezugTests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public VorheizAnkunftsbezugTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static Aufheizvorgabe Berechnet(Vorheizankunftsbezug bezug = Vorheizvorgabe.ANKUNFTSBEZUG_VORGABE,
                                                bool imMaximum = Vorheizvorgabe.UNERREICHBARE_IM_MAXIMUM_VORGABE,
                                                Aufheizvorgabe basis = null)
            => (basis ?? new Aufheizvorgabe(true, null, null, null, null)) with
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Berechnet)
                {
                    Ankunftsbezug = bezug,
                    UnerreichbareImMaximum = imMaximum,
                },
            };

        /// <summary>Das Probegebäude AK1 (Radiator, Heizkurve) mit knapper Übergabe: Nennleistung = <paramref name="anteil"/> · Φ_HL.</summary>
        private static GebaeudeModellEingang Gekoppelt(double anteil)
        {
            static ProjektGebaeudeModel G(double? nennKw)
            {
                ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
                g.Heizkreis_Aktiv = true;
                g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
                g.Heizkurve_Aktiv = true;
                g.Uebergabe_Leistung_Nenn = nennKw;
                return g;
            }
            static GebaeudeModellEingang Bauen(ProjektGebaeudeModel g)
                => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                               GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1);
            double phiHl = Bauen(G(null)).AuslegungsheizlastW;
            GebaeudeModellEingang e = Bauen(G(anteil * phiHl / 1000.0));
            Assert.True(e.KopplungWirksam);
            e.HeizsollwertMitRampeSetzen(VorheizVorgabeTests.Kalender());
            return e;
        }

        private static GebaeudeModellEingang Ideal()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Klima);
            e.HeizsollwertMitRampeSetzen(VorheizVorgabeTests.Kalender());
            return e;
        }

        private static (Aufheizplan Plan, GebaeudeModellErgebnis Lauf) Rechnen(GebaeudeModellEingang e, Aufheizvorgabe v)
        {
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, v, 0, 1);
            GebaeudeModellErgebnis lauf = Vdi6007Rechenweg.Laufen(e, 0, 1, plan);
            Vorheizplanung.NachweisenEinzone(e, plan, lauf);
            return (plan, lauf);
        }

        // =====================================================================
        //  θ_stat
        // =====================================================================

        [Fact]
        public void Die_stationaere_Luft_ist_der_Fixpunkt_der_Stundenabbildung_und_liegt_bei_knapper_Uebergabe_unter_dem_Sollwert()
        {
            GebaeudeModellEingang e = Gekoppelt(0.4);
            GebaeudeModellErgebnis vorlauf = Vdi6007Rechenweg.Laufen(Massen(e), 0, 1);
            ZonenEingang zone = ZonenEingang.Einzeln(e);
            double[] s = e.ThetaSoll;
            int unter = 0, geprueft = 0;
            for (int hs = 1; hs < STUNDEN; hs++)
            {
                if (!(s[hs] > s[hs - 1] + 1.0) || double.IsNaN(s[hs - 1])) continue;
                Stundenrand r = zone.Rand(hs, false, ReadOnlySpan<double>.Empty, false).MitVorheizen(s[hs], e.HeizleistungMaxW);
                var m = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
                double stat = Vorheizankunft.StationaereLuft(m, in r, vorlauf.MassenEndeAw[hs - 1], vorlauf.MassenEndeIw[hs - 1]);
                double aw = m.ThetaMAw, iw = m.ThetaMIw;
                // Fixpunkt: Eine Stunde mit demselben Rand lässt die Massen stehen.
                m.Schritt(in r);
                Assert.True(Math.Abs(m.ThetaMAw - aw) < 1e-6 && Math.Abs(m.ThetaMIw - iw) < 1e-6, "kein Fixpunkt an h_s " + hs);
                double bezug = Vorheizankunft.Bezug(Vorheizankunftsbezug.Uebergabe, e, new Zonenmodell2K(e.Parameter, e.Bezeichnung), in r,
                                                    s[hs], vorlauf.MassenEndeAw[hs - 1], vorlauf.MassenEndeIw[hs - 1]);
                Assert.Equal(Math.Min(s[hs], stat), bezug, 12);
                Assert.Equal(Bits(s[hs]), Bits(Vorheizankunft.Bezug(Vorheizankunftsbezug.Sollwert, e, m, in r, s[hs], aw, iw)));
                if (stat < s[hs] - 0.1) unter++;
                if (geprueft++ < 3)
                {
                    // Gegenprobe: die einfache Iteration über viele Stunden mit festgehaltenem Rand endet am selben Punkt.
                    var p = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
                    p.Zuruecksetzen(vorlauf.MassenEndeAw[hs - 1], vorlauf.MassenEndeIw[hs - 1]);
                    for (int k = 0; k < 40_000; k++) p.Schritt(in r);
                    Assert.Equal(stat, p.LuftAmBeginn(in r), 4);
                    _aus.WriteLine(FormattableString.Invariant($"h_s {hs}: θ_T {s[hs]:0.0} °C, θ_stat {stat:0.000} °C, Iteration {p.LuftAmBeginn(in r):0.000} °C"));
                }
            }
            Assert.True(geprueft > 0);
            Assert.True(unter > 0, "Die knappe Übergabe hält an keinem Sprung weniger als θ_T.");
        }

        private static GebaeudeModellEingang Massen(GebaeudeModellEingang e)
        {
            e.MassenErfassen = true;
            return e;
        }

        // =====================================================================
        //  Wirkung im Plan und im Nachweis
        // =====================================================================

        [Fact]
        public void Bezug_b_mindert_die_unerreichbaren_Spruenge_an_knapper_Uebergabe()
        {
            (Aufheizplan pa, _) = Rechnen(Gekoppelt(0.4), Berechnet(Vorheizankunftsbezug.Sollwert));
            (Aufheizplan pb, _) = Rechnen(Gekoppelt(0.4), Berechnet(Vorheizankunftsbezug.Uebergabe));
            Assert.Equal(Vorheizankunftsbezug.Uebergabe, pb.Vorheizen.Ankunftsbezug);
            int ua = pa.Vorheizen.SpruengeUnerreichbar, ub = pb.Vorheizen.SpruengeUnerreichbar;
            _aus.WriteLine($"(a): t_V {pa.Vorheizen.VorheizzeitH} h, unerreichbar {ua}, Tage ohne Ankunft {pa.Vorheizen.Nachweis.TageOhneAnkunftAnzahl}");
            _aus.WriteLine($"(b): t_V {pb.Vorheizen.VorheizzeitH} h, unerreichbar {ub}, Tage ohne Ankunft {pb.Vorheizen.Nachweis.TageOhneAnkunftAnzahl}");
            Assert.True(ua > 0, "Die Probe ist nicht knapp genug.");
            Assert.True(ub < ua);
            Assert.True(pb.Vorheizen.Nachweis.TageOhneAnkunftAnzahl <= pa.Vorheizen.Nachweis.TageOhneAnkunftAnzahl);
        }

        [Fact]
        public void Bezug_b_an_einer_Zone_ohne_Heizkreis_rechnet_bitgleich_zu_a()
        {
            (Aufheizplan pa, GebaeudeModellErgebnis la) = Rechnen(Ideal(), Berechnet(Vorheizankunftsbezug.Sollwert));
            (Aufheizplan pb, GebaeudeModellErgebnis lb) = Rechnen(Ideal(), Berechnet(Vorheizankunftsbezug.Uebergabe));
            Assert.Equal(pa.Vorheizen.VorheizzeitH, pb.Vorheizen.VorheizzeitH);
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Bits(pa.Reihe[h]), Bits(pb.Reihe[h]));
                Assert.Equal(Bits(la.HeizlastW[h]), Bits(lb.HeizlastW[h]));
            }
            Assert.Equal(pa.Vorheizen.Nachweis.TageOhneAnkunftAnzahl, pb.Vorheizen.Nachweis.TageOhneAnkunftAnzahl);
        }

        [Fact]
        public void Ohne_unerreichbare_Spruenge_im_Maximum_ist_tV_das_Maximum_der_erreichbaren()
        {
            (Aufheizplan mit, _) = Rechnen(Gekoppelt(0.4), Berechnet(imMaximum: true));
            (Aufheizplan ohne, _) = Rechnen(Gekoppelt(0.4), Berechnet(imMaximum: false));
            Assert.True(ohne.Vorheizen.SpruengeUnerreichbar > 0);
            int erreichbar = ohne.Vorheizen.Spruenge.Where(x => !x.Unerreichbar).Select(x => x.BedarfH).DefaultIfEmpty(0).Max();
            Assert.Equal(Math.Max(1, erreichbar), ohne.Vorheizen.VorheizzeitH);
            Assert.Equal(Math.Max(1, mit.Vorheizen.Spruenge.Max(x => x.BedarfH)), mit.Vorheizen.VorheizzeitH);
            Assert.True(ohne.Vorheizen.VorheizzeitH < mit.Vorheizen.VorheizzeitH);
            Assert.Equal(mit.Vorheizen.SpruengeUnerreichbar, ohne.Vorheizen.SpruengeUnerreichbar);
        }

        [Fact]
        public void Die_Vorgaben_sind_das_Verhalten_vor_V3b()
        {
            var v = new Vorheizvorgabe(Aufheizverfahren.Berechnet);
            Assert.Equal(Vorheizankunftsbezug.Sollwert, v.Ankunftsbezug);
            Assert.True(v.UnerreichbareImMaximum);
            Assert.Equal(v, v with { Ankunftsbezug = Vorheizankunftsbezug.Sollwert, UnerreichbareImMaximum = true });
            // Ausdrücklich gesetzte Vorgaben rechnen bitgleich zur ungesetzten Vorgabe — auch an der knappen Übergabe.
            (Aufheizplan pa, GebaeudeModellErgebnis la) = Rechnen(Gekoppelt(0.4), new Aufheizvorgabe(true, null, null, null, null) { Vorheizen = v });
            (Aufheizplan pb, GebaeudeModellErgebnis lb) = Rechnen(Gekoppelt(0.4), Berechnet());
            Assert.Equal(pa.Vorheizen.VorheizzeitH, pb.Vorheizen.VorheizzeitH);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(la.HeizlastW[h]), Bits(lb.HeizlastW[h]));
        }

        // =====================================================================
        //  Messung (nur mit EPOS_MESSUNG=1)
        // =====================================================================

        /// <summary>
        /// <b>Die Entscheidungsgrundlage</b>: je Projekt Option 2 (ε 1 K, Geltung Gebäude, die übrigen Felder der Projektvorgabe) für
        /// Bezug (a)/(b) × unerreichbare Sprünge im Maximum ja/nein über den ganzen Projektlauf (1058: Nachweis am Kreis); Mehrwärme
        /// gegen den Lauf mit der gespeicherten Vorgabe des Projekts (= Basis R51).
        /// </summary>
        [Fact]
        public void Messung_Ankunftsbezug_und_Maximum()
        {
            if (!_db.Vorhanden || Environment.GetEnvironmentVariable("EPOS_MESSUNG") != "1") return;
            using var k = new Kulturvorrichtung("de-DE");
            CultureInfo c = CultureInfo.InvariantCulture;
            foreach (int projekt in new[] { 1054, 1056, 1047, 1058, 1051 })
            {
                SimulationRunner basis = VorheizAk3Tests.Projektlauf(projekt, null);
                double qBasis = Heizwaerme(basis);
                Aufheizvorgabe pv = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
                _aus.WriteLine(string.Format(c, "{0}: Heizwärme Basis {1:0} kWh (Aufheizoptimierung des Projekts {2})", projekt, qBasis,
                                             pv != null && pv.An ? "an" : "aus"));
                foreach ((Vorheizankunftsbezug bezug, bool imMax) in new[]
                         {
                             (Vorheizankunftsbezug.Sollwert, true), (Vorheizankunftsbezug.Sollwert, false),
                             (Vorheizankunftsbezug.Uebergabe, true), (Vorheizankunftsbezug.Uebergabe, false),
                         })
                {
                    var uhr = System.Diagnostics.Stopwatch.StartNew();
                    SimulationRunner r = VorheizAk3Tests.Projektlauf(projekt, Berechnet(bezug, imMax, VorheizAk3Tests.ProjektvorgabeAn(projekt)));
                    double ms = uhr.Elapsed.TotalMilliseconds;
                    double q = Heizwaerme(r);
                    foreach (GebaeudeModellErgebnis e in Ergebnisse(r).Where(x => x.Vorheizen != null))
                    {
                        Vorheizgebaeude v = e.Vorheizen;
                        _aus.WriteLine(string.Format(c,
                            "| {0} | {1} | {2} | {3} | {4:0.#} / {5} | {6} | {7} | {8:0.00} | {9} | {10:0.0} | {11:+0;−0} ({12:+0.0;−0.0} %) | {13:0} ms |",
                            projekt, bezug == Vorheizankunftsbezug.Sollwert ? "(a)" : "(b)", imMax ? "mit" : "ohne", v.VorheizzeitMaxH,
                            v.BedarfMedianH, v.BedarfMaxH, v.TageUnerreichbar, v.TageOhneAnkunftAnzahl, v.UnterschreitungMaxK,
                            v.NaechteOhneAbsenkung, v.SpitzeW / 1000.0, q - qBasis, 100.0 * (q - qBasis) / qBasis, ms));
                    }
                }
            }
        }

        private static IEnumerable<GebaeudeModellErgebnis> Ergebnisse(SimulationRunner r)
            => r.simulation_Waermebedarf.GebaeudeErgebnisse.Alle.Where(e => e != null);

        private static double Heizwaerme(SimulationRunner r) => Ergebnisse(r).Sum(e => e.HeizlastW.Sum()) / 1000.0;
    }
}
