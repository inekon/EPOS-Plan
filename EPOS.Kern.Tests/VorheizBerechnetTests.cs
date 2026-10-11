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
    /// <b>Vorheizen Option 2 „Vorheizzeit berechnen“, Vorausschau und Geltung Gebäude</b> (Entwurf Vorheizrampe Fassung 2,
    /// 2.3, 2.4, 2.5, 2.6, 2.7; Welle V3a): die Vorausschau ist rein, die Bisektion trifft die lineare Suche, „unerreichbar“ bei
    /// P_V &lt; Φ_stat, t_V ist das Maximum des Bedarfs und gilt an jedem Sprung, Option 1 nennt den Bedarf der verfehlten Tage,
    /// Geltung Gebäude teilt den Deckel nach Φ_HL (Zone ohne Φ_HL ungedeckelt), die Hinweistexte (de-DE) und die Messung an
    /// Projekt 1051 (nur mit <c>EPOS_MESSUNG=1</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizBerechnetTests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public VorheizBerechnetTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static Aufheizvorgabe Berechnet(Vorheiztoleranzart art = Vorheiztoleranzart.Prozent, double? wert = null,
                                                double? eps = null, Vorheizgeltung geltung = Vorheizgeltung.Gebaeude)
            => new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Berechnet, null, art, wert, eps, geltung),
            };

        private static Aufheizvorgabe Vorgabe(int tV, Vorheizgeltung geltung = Vorheizgeltung.Gebaeude, double? eps = null)
            => new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, tV, Vorheiztoleranzart.Prozent, null, eps, geltung),
            };

        private static GebaeudeModellEingang Probeeingang()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
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
        //  Vorausschau
        // =====================================================================

        [Fact]
        public void Vorausschau_ist_rein_und_laesst_Eingang_und_Lauf_unberuehrt()
        {
            GebaeudeModellEingang e = Probeeingang();
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, Vorgabe(6), 0, 1);
            double[] soll = e.ThetaSoll, deckel = e.HeizleistungMaxReiheW;
            GebaeudeModellErgebnis vorher = Vdi6007Rechenweg.Laufen(e, 0, 1, plan);

            Vorheizvorausschau vs = plan.Vorheizen.Vorausschau;
            Assert.NotNull(vs);
            Vorheizsprung sp = plan.Vorheizen.Spruenge.First(x => x.FensterMaxH >= 13);
            bool a1 = vs.Ankunft(sp.Sprungstunde, 5, sp.PhiRefW, out double d1);
            for (int t = 1; t <= sp.FensterMaxH; t++) vs.Ankunft(sp.Sprungstunde, t, sp.PhiRefW, out _);
            bool a2 = vs.Ankunft(sp.Sprungstunde, 5, sp.PhiRefW, out double d2);
            Assert.Equal(a1, a2);
            Assert.Equal(Bits(d1), Bits(d2));

            Assert.Same(soll, e.ThetaSoll);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(deckel[h]), Bits(e.HeizleistungMaxReiheW[h]));
            GebaeudeModellErgebnis nachher = Vdi6007Rechenweg.Laufen(e, 0, 1, plan);
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Bits(vorher.HeizlastW[h]), Bits(nachher.HeizlastW[h]));
                Assert.Equal(Bits(vorher.Raumtemperatur[h]), Bits(nachher.Raumtemperatur[h]));
            }
        }

        [Theory]
        [InlineData(20.0)]
        [InlineData(2.0)]
        public void Bisektion_gleich_linearer_Suche(double toleranzProzent)
        {
            GebaeudeModellEingang e = Probeeingang();
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, Berechnet(Vorheiztoleranzart.Prozent, toleranzProzent), 0, 1);
            Vorheizvorausschau vs = plan.Vorheizen.Vorausschau;
            int geprueft = 0;
            foreach (Vorheizsprung sp in plan.Vorheizen.Spruenge.Where(x => x.FensterMaxH >= 1))
            {
                int vorher = vs.Vorausschauen;
                int b = vs.Bedarf(sp.Sprungstunde, sp.FensterMaxH, sp.PhiRefW, out bool ub);
                Assert.True(vs.Vorausschauen - vorher <= (int)Math.Ceiling(Math.Log2(sp.FensterMaxH + 1)));
                int l = vs.BedarfLinear(sp.Sprungstunde, sp.FensterMaxH, sp.PhiRefW, out bool ul);
                Assert.Equal(l, b);
                Assert.Equal(ul, ub);
                Assert.Equal(sp.BedarfH, b);
                Assert.Equal(sp.Unerreichbar, ub);
                geprueft++;
            }
            Assert.True(geprueft > 20);
            if (toleranzProzent < 5.0)
                Assert.True(plan.Vorheizen.Spruenge.Select(x => x.BedarfH).Distinct().Count() > 1, "Der Bedarf streut über die Sprünge.");
        }

        [Fact]
        public void Unerreichbar_wenn_die_Vorheizleistung_unter_der_stationaeren_Last_liegt()
        {
            GebaeudeModellEingang e = Probeeingang();
            e.MassenErfassen = true;
            GebaeudeModellErgebnis vorlauf = Vdi6007Rechenweg.Laufen(e, 0, 1);
            ZonenEingang zone = ZonenEingang.Einzeln(e);
            Aufheizzone az = Aufheizzone.Aus(zone);
            Vorheizanalyse a = Vorheizplanung.Analysieren(az, Berechnet(), vorlauf);
            int i = Enumerable.Range(0, a.Spruenge.Count).OrderByDescending(k => a.PhiRefSprung[k]).First();
            (int hs, int _) = a.Spruenge[i];
            double phiStat = Vorheizplanung.PhiRef(az, e.ThetaSoll[hs], hs - 1);
            var knapp = new Vorheizvorausschau(zone, e.ThetaSoll, vorlauf, null, 0.5 * phiStat, 0.5 * phiStat, 1.0);
            int bedarf = knapp.Bedarf(hs, a.FensterMax[i], a.PhiRefSprung[i], out bool unerreichbar);
            Assert.True(unerreichbar);
            Assert.Equal(a.FensterMax[i], bedarf);

            var reichlich = new Vorheizvorausschau(zone, e.ThetaSoll, vorlauf, null, double.PositiveInfinity,
                                                   double.PositiveInfinity, 1.0);
            reichlich.Bedarf(hs, a.FensterMax[i], a.PhiRefSprung[i], out bool u2);
            Assert.False(u2);
        }

        // =====================================================================
        //  Option 2 und Option 1 mit Bedarf
        // =====================================================================

        [Fact]
        public void Option2_tV_ist_das_Maximum_des_Bedarfs_und_gilt_an_jedem_Sprung()
        {
            using var k = new Kulturvorrichtung("de-DE");
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            GebaeudeModellEingang e = Probeeingang();
            (Aufheizplan plan, GebaeudeModellErgebnis lauf) = Rechnen(e, Berechnet());
            Vorheizplan vp = plan.Vorheizen;
            Assert.Equal(Aufheizverfahren.Berechnet, vp.Verfahren);
            List<Vorheizsprung> mit = vp.Spruenge.Where(x => x.FensterMaxH >= 1).ToList();
            Assert.NotEmpty(mit);
            Assert.All(mit, x => Assert.InRange(x.BedarfH, 1, x.FensterMaxH));
            int max = mit.Max(x => x.BedarfH);
            Assert.Equal(max, vp.VorheizzeitH);
            Assert.Equal(max, vp.BedarfMaxH);
            Assert.All(vp.Spruenge, x => Assert.Equal(Math.Min(vp.VorheizzeitH, x.FensterMaxH), x.FensterH));

            Vorheizgebaeude g = Vorheizplanung.Gebaeudewerte(new[] { plan });
            Assert.True(g.Berechnet);
            Assert.Equal(max, g.VorheizzeitMaxH);
            Assert.InRange(g.BedarfMedianH, 1.0, max);
            Assert.True(g.Vorausschauen <= 6 * mit.Count);
            // Die Vorausschau ist zur sicheren Seite: Mit t_V = max t_nötig kommt der Lauf an jedem Sprung an.
            Assert.Equal(0, g.TageOhneAnkunftAnzahl);
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, g, "Probe");
            Assert.Contains(p.Hinweise, z => z.Contains("Vorheizzeit berechnet: " + max.ToString(CultureInfo.InvariantCulture) + " h",
                                                       StringComparison.Ordinal));
        }

        [Fact]
        public void Option1_nennt_den_Bedarf_der_verfehlten_Tage()
        {
            GebaeudeModellEingang e = Probeeingang();
            (Aufheizplan plan, GebaeudeModellErgebnis _) = Rechnen(e, Vorgabe(1, eps: 0.5));
            Vorheiznachweis n = plan.Vorheizen.Nachweis;
            List<Vorheizpruefung> verfehlt = n.Pruefungen.Where(x => !x.Angekommen).ToList();
            Assert.NotEmpty(verfehlt);
            Assert.All(verfehlt, x => Assert.True(x.BedarfH >= 1));
            Assert.All(n.Pruefungen.Where(x => x.Angekommen), x => Assert.Equal(0, x.BedarfH));
            Assert.Equal(verfehlt.Max(x => x.BedarfH), n.BedarfMaxH);
            Assert.True(n.BedarfMaxH > 1);
            Vorheizgebaeude g = Vorheizplanung.Gebaeudewerte(new[] { plan });
            Assert.False(g.Berechnet);
            Assert.Equal(n.BedarfMaxH, g.BedarfMaxH);
        }

        // =====================================================================
        //  Geltung Gebäude
        // =====================================================================

        [Fact]
        public void Geltung_Gebaeude_teilt_den_Deckel_nach_der_Auslegungsheizlast()
        {
            IReadOnlyList<ZonenEingang> zonen = Dreizonen();
            Aufheizzone[] az = Aufheizzone.AusZonen(zonen);
            Aufheizvorgabe v = Berechnet();
            IReadOnlyList<Aufheizplan> plaene = Vorheizplanung.AnwendenZonen(zonen, v, 0, 1, "Dreizonen");

            // Φ_K,max,Geb unabhängig nachgerechnet.
            var stunden = new SortedSet<int>(plaene.Where(p => p.Vorheizen != null)
                                                   .SelectMany(p => p.Vorheizen.Spruenge).Select(sp => sp.Sprungstunde));
            double phiGeb = 0.0;
            foreach (int h in stunden)
            {
                double summe = 0.0;
                for (int i = 0; i < az.Length; i++)
                    if (az[i].Beheizt && !double.IsNaN(az[i].Soll[h])) summe += Vorheizplanung.PhiRef(az[i], az[i].Soll[h], h);
                phiGeb = Math.Max(phiGeb, summe);
            }
            double hl = az.Where(z => z.Beheizt).Sum(z => z.AuslegungsheizlastW);
            List<(Aufheizzone Zone, Vorheizplan Plan)> mit = az.Zip(plaene, (z, p) => (z, p.Vorheizen)).Where(x => x.Item2 != null).ToList();
            Assert.Equal(2, mit.Count);
            foreach ((Aufheizzone z, Vorheizplan vp) in mit)
            {
                Assert.True(vp.GeltungGebaeude);
                Assert.False(vp.OhneAuslegungsheizlast);
                Assert.Equal(1.2 * phiGeb * z.AuslegungsheizlastW / hl, vp.DeckelW, 6);
                Assert.Equal(Math.Min(vp.DeckelW, vp.VerfuegbarW), vp.VorheizleistungW);
            }
            Assert.Equal(1.2 * phiGeb, mit.Sum(x => x.Plan.DeckelW), 6);
            // t_V,Geb = max_i t_V,i: alle Zonen tragen dieselbe Zeit, das Maximum ihrer Bedarfe.
            int tV = mit.Max(x => x.Plan.BedarfMaxH);
            Assert.All(mit, x => Assert.Equal(tV, x.Plan.VorheizzeitH));

            // Lauf und Gebäudewerte: der Sprung an der Summe.
            GebaeudeStepper s = GebaeudeStepper.Mehrzonen(new Zonenschleife(zonen, "Dreizonen"));
            s.Beginnen();
            s.Jahr();
            GebaeudeModellErgebnis[] e = s.Abschluss(0, 1);
            Vorheizplanung.NachweisenZonen(zonen, e);
            var summeW = new double[STUNDEN];
            foreach (GebaeudeModellErgebnis r in e) for (int h = 0; h < STUNDEN; h++) summeW[h] += r.HeizlastW[h];
            Vorheizgebaeude g = Vorheizplanung.Gebaeudewerte(plaene, summeW);
            double sMax = stunden.Max(h => summeW[h] - summeW[Aufheizoptimierung.Ring(h - 1)]);
            Assert.Equal(Bits(sMax), Bits(g.SprungMaxW));
            Assert.Equal(tV, g.VorheizzeitMaxH);
            Assert.Equal(0, g.ZonenOhneAuslegungsheizlast);
        }

        [Fact]
        public void Geltung_Gebaeude_laesst_eine_Zone_ohne_Auslegungsheizlast_ungedeckelt()
        {
            IReadOnlyList<ZonenEingang> zonen = Dreizonen();
            foreach (ZonenEingang z in zonen) z.MassenErfassen = true;
            GebaeudeStepper s = GebaeudeStepper.Mehrzonen(new Zonenschleife(zonen, "Dreizonen"));
            s.Beginnen();
            s.Jahr();
            GebaeudeModellErgebnis[] vorlauf = s.Abschluss(0, 1);
            Aufheizzone[] az = Aufheizzone.AusZonen(zonen);
            Aufheizvorgabe v = Vorgabe(6);
            var analysen = new Vorheizanalyse[az.Length];
            bool erste = true;
            for (int i = 0; i < az.Length; i++)
            {
                if (!az[i].Beheizt) continue;
                Aufheizzone zone = erste ? az[i] with { AuslegungsheizlastW = double.NaN } : az[i];
                erste = false;
                analysen[i] = Vorheizplanung.Analysieren(zone, v, vorlauf[i]);
            }
            Vorheizplanung.GebaeudedeckelVerteilen(analysen, v.Vorheizen);
            List<Vorheizanalyse> mit = analysen.Where(a => a != null).ToList();
            Assert.True(mit[0].OhneAuslegungsheizlast);
            Assert.True(double.IsPositiveInfinity(mit[0].DeckelW));
            Assert.Equal(mit[0].VerfuegbarW, mit[0].VorheizleistungW);
            Assert.False(mit[1].OhneAuslegungsheizlast);
            // Die einzige Zone mit Φ_HL trägt den ganzen Deckel des Gebäudes.
            Assert.True(mit[1].DeckelW > 1.2 * mit[1].PhiKMaxW - 1e-6);

            using var k = new Kulturvorrichtung("de-DE");
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Aufheizplan[] plaene = analysen.Select((a, i) => a == null ? null : Vorheizplanung.PlanBilden(a, v, 6)).ToArray();
            Vorheizgebaeude g = Vorheizplanung.Gebaeudewerte(plaene);
            Assert.Equal(1, g.ZonenOhneAuslegungsheizlast);
            Assert.True(double.IsFinite(g.DeckelW));
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, g, "Probe");
            Assert.Contains(p.Hinweise, z => z.Contains("1 beheizte Zonen haben keine Auslegungsheizlast", StringComparison.Ordinal));
        }

        private static IReadOnlyList<ZonenEingang> Dreizonen()
        {
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(AufheizMehrzonenTests.Dreizonen(), AufheizMehrzonenTests.Klima());
            foreach (ZonenEingang z in zonen)
                if (z.Eingang.ThetaSoll.Any(x => !double.IsNaN(x))) z.Eingang.HeizsollwertMitRampeSetzen(VorheizVorgabeTests.Kalender());
            return zonen;
        }

        // =====================================================================
        //  Hinweise und Bestand
        // =====================================================================

        [Fact]
        public void Hinweistexte_nennen_Bedarf_Unerreichbar_und_Abweichung()
        {
            using var k = new Kulturvorrichtung("de-DE");
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            var tage = new bool[365];
            for (int t = 0; t < 12; t++) tage[t] = true;
            var v = new Vorheizgebaeude
            {
                VorheizzeitMaxH = 6,
                BedarfMaxH = 9,
                TageOhneAnkunft = tage,
                UnterschreitungMaxK = 1.4,
                TageUnerreichbar = 3,
                NaechteOhneAbsenkung = 3,
                TageAbweichung = 2,
            };
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, v, "Gebäude A");
            Assert.Contains(p.Hinweise, z => z.Contains("An 12 Tagen erreicht die Raumluft zum Nutzungsbeginn den Sollwert nicht (bis 1,4 K darunter); nötig wären bis 9 h Vorheizen statt 6 h.", StringComparison.Ordinal));
            Assert.Contains(p.Hinweise, z => z.Contains("An 3 Tagen reicht die Vorheizleistung selbst ohne Absenkung", StringComparison.Ordinal)
                                             && z.Contains("3 Nächte bleiben ohne Absenkung", StringComparison.Ordinal));
            Assert.Contains(p.Hinweise, z => z.Contains("An 2 Tagen verfehlt der Lauf den Sollwert zum Nutzungsbeginn, den die Vorausschau als erreichbar sah", StringComparison.Ordinal));
            Assert.DoesNotContain(p.Hinweise, z => z.Contains("Vorheizzeit berechnet", StringComparison.Ordinal));

            // Ohne höheren Bedarf bleibt der Text der Tage ohne „nötig wären“.
            SimulationProtokoll q = SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, v with { BedarfMaxH = 6 }, "Gebäude B");
            Assert.Contains(q.Hinweise, z => z.Contains("(bis 1,4 K darunter).", StringComparison.Ordinal));
            Assert.DoesNotContain(q.Hinweise, z => z.Contains("nötig wären", StringComparison.Ordinal));

            SimulationProtokoll r = SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Heizkreis, null, "Gebäude C");
            Assert.Contains(r.Hinweise, z => z.Contains("Gebäude C", StringComparison.Ordinal));
        }

        [Fact]
        public void Berechnet_mit_Rueckfall_rechnet_die_Sollwertrampe_bitgleich()
        {
            var an = new Aufheizvorgabe(true, null, null, null, null);
            Aufheizvorgabe wirksam = Vorheizplanung.Wirksam(Berechnet(), Vorheizrueckfall.Heizkreis);
            Assert.False(Vorheizplanung.Anwendbar(wirksam));
            GebaeudeModellEingang a = Probeeingang(), b = Probeeingang();
            Aufheizplan pa = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(a), an);
            Aufheizplan pb = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(b), wirksam);
            GebaeudeModellErgebnis la = Vdi6007Rechenweg.Laufen(a, 0, 1, pa), lb = Vdi6007Rechenweg.Laufen(b, 0, 1, pb);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(la.HeizlastW[h]), Bits(lb.HeizlastW[h]));
        }

        // =====================================================================
        //  Messung an 1051 (nur mit EPOS_MESSUNG=1)
        // =====================================================================

        /// <summary>
        /// <b>Messung an Projekt 1051</b> (Entwurf 2.8, kein harter Wert): Option 2 mit ε 1 K und 0,5 K, Bedarf je Sprung nach
        /// Werktag / nach Wochenende / nach Ferien, Monotonie gegen die lineare Suche, Mehrwärme gegen den Vorlauf und gegen die
        /// Sollwertrampe des Projekts, Option 1 mit 6 h samt Bedarf, Rechenzeit; dazu Option 2 an 1052 (Mehrzonen).
        /// </summary>
        [Fact]
        public void Messung_an_Projekt_1051()
        {
            if (!_db.Vorhanden || Environment.GetEnvironmentVariable("EPOS_MESSUNG") != "1") return;
            using var k = new Kulturvorrichtung("de-DE");
            CultureInfo c = CultureInfo.InvariantCulture;

            var uhr = Stopwatch.StartNew();
            AufheizLauf.Gebaeudelauf ohne = AufheizLauf.Projekt(1051, Aufheizvorgabe.Aus).Single();
            double tOhne = uhr.Elapsed.TotalMilliseconds;
            Aufheizvorgabe projekt = KonfigurationCtrl.AufheizvorgabeLesen(1051) ?? new Aufheizvorgabe(true, null, null, null, null);
            uhr.Restart();
            AufheizLauf.Gebaeudelauf rampe = AufheizLauf.Projekt(1051, projekt.An ? projekt : projekt.Eingeschaltet()).Single();
            double tRampe = uhr.Elapsed.TotalMilliseconds;
            double qOhne = ohne.Ergebnis.VerbrauchAltKwh, qRampe = rampe.Ergebnis.VerbrauchAltKwh;
            _aus.WriteLine(string.Format(c, "1051: Heizwärme ohne Aufheizen {0:0.0} kWh ({1:0} ms), Sollwertrampe des Projekts {2:0.0} kWh ({3:0} ms, An={4})",
                                         qOhne, tOhne, qRampe, tRampe, projekt.An));

            foreach (double eps in new[] { 2.0, 1.0, 0.5, 0.25, 0.1 })
            {
                uhr.Restart();
                AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1051, Berechnet(eps: eps)).Single();
                double ms = uhr.Elapsed.TotalMilliseconds;
                Vorheizgebaeude v = l.Ergebnis.Vorheizen;
                Vorheizplan vp = l.Plan.Vorheizen;
                Assert.NotNull(v);
                double qLauf = l.Ergebnis.VerbrauchAltKwh;
                _aus.WriteLine(string.Format(c,
                    "Option 2, ε {0} K: t_V {1} h, Median {2:0.#} h, unerreichbar {3} Tage, Nächte ohne Absenkung {4}, Tage ohne Ankunft {5} " +
                    "(bis {6:0.00} K), Abweichungen {7}, Φ_K,max {8:0.00} kW, P_K {9:0.00} kW, P_V {10:0.00} kW, Spitze {11:0.00} kW, " +
                    "S_max,0 {12:0.00} kW, S_max {13:0.00} kW, Heizwärme {14:0.0} kWh, Mehrwärme gegen Vorlauf {15:0.0} kWh ({16:0.0} %), " +
                    "gegen Sollwertrampe {17:0.0} kWh ({18:0.0} %), Vorausschauen {19}, Schritte {20}, Rechenzeit {21:0} ms",
                    eps, v.VorheizzeitMaxH, v.BedarfMedianH, v.TageUnerreichbar, v.NaechteOhneAbsenkung, v.TageOhneAnkunftAnzahl,
                    v.UnterschreitungMaxK, v.TageAbweichung, v.PhiKMaxW / 1000.0, v.DeckelW / 1000.0, v.VorheizleistungW / 1000.0,
                    v.SpitzeW / 1000.0, v.Sprung0MaxW / 1000.0, v.SprungMaxW / 1000.0, qLauf, v.MehrwaermeKwh, v.MehrwaermeProzent,
                    qLauf - qRampe, 100.0 * (qLauf - qRampe) / qRampe, v.Vorausschauen, vp.Vorausschau.Schritte, ms));
                foreach ((string name, Func<Vorheizsprung, bool> wahl) in new (string, Func<Vorheizsprung, bool>)[]
                         {
                             ("Werktag D=13", x => x.AbsenkdauerH == 13),
                             ("nach Wochenende D=61", x => x.AbsenkdauerH == 61),
                             ("nach Ferien/Feiertag", x => x.AbsenkdauerH != 13 && x.AbsenkdauerH != 61),
                         })
                {
                    List<int> b = vp.Spruenge.Where(wahl).Select(x => x.BedarfH).OrderBy(x => x).ToList();
                    if (b.Count == 0) continue;
                    string vert = string.Join(" ", b.GroupBy(x => x).Select(g => g.Key + "h×" + g.Count()));
                    _aus.WriteLine(string.Format(c, "  {0}: n {1}, Median {2} h, Max {3} h, Verteilung {4}", name, b.Count,
                                                 b[b.Count / 2], b[^1], vert));
                }
                // Monotonie: Bisektion gegen lineare Suche an jedem Sprung.
                int ausnahmen = 0;
                foreach (Vorheizsprung sp in vp.Spruenge.Where(x => x.FensterMaxH >= 1))
                {
                    int lin = vp.Vorausschau.BedarfLinear(sp.Sprungstunde, sp.FensterMaxH, sp.PhiRefW, out bool ul);
                    if (lin != sp.BedarfH || ul != sp.Unerreichbar)
                    {
                        ausnahmen++;
                        _aus.WriteLine(string.Format(c, "  Monotonie verletzt: h_s {0}, Bisektion {1} h, linear {2} h", sp.Sprungstunde, sp.BedarfH, lin));
                    }
                }
                _aus.WriteLine("  Monotonie: Ausnahmen " + ausnahmen.ToString(c) + " von " + vp.Spruenge.Count.ToString(c));
            }

            foreach (int tV in new[] { 6, 15 })
            {
                uhr.Restart();
                AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(1051, Vorgabe(tV)).Single();
                double ms = uhr.Elapsed.TotalMilliseconds;
                Vorheizgebaeude v = l.Ergebnis.Vorheizen;
                double qLauf = l.Ergebnis.VerbrauchAltKwh;
                _aus.WriteLine(string.Format(c,
                    "Option 1, t_V {0} h: Tage ohne Ankunft {1}, Bedarf max {2} h, Nächte ohne Absenkung {3}, Spitze {4:0.00} kW, " +
                    "Mehrwärme gegen Vorlauf {5:0.0} kWh ({6:0.0} %), gegen Sollwertrampe {7:0.0} kWh ({8:0.0} %), Rechenzeit {9:0} ms",
                    tV, v.TageOhneAnkunftAnzahl, v.BedarfMaxH, v.NaechteOhneAbsenkung, v.SpitzeW / 1000.0, v.MehrwaermeKwh,
                    v.MehrwaermeProzent, qLauf - qRampe, 100.0 * (qLauf - qRampe) / qRampe, ms));
            }

            foreach ((int projektId, double eps) in new[] { (1052, 1.0), (1054, 1.0), (1054, 2.0) })
            {
                uhr.Restart();
                List<AufheizLauf.Gebaeudelauf> ll = AufheizLauf.Projekt(projektId, Berechnet(eps: eps));
                double ms = uhr.Elapsed.TotalMilliseconds;
                uhr.Restart();
                AufheizLauf.Projekt(projektId, Aufheizvorgabe.Aus);
                double msOhne = uhr.Elapsed.TotalMilliseconds;
                foreach (AufheizLauf.Gebaeudelauf l in ll)
                {
                    Vorheizgebaeude v = l.Ergebnis?.Vorheizen;
                    int zonen = l.Mehrzonen?.Eingaenge.Count ?? 1;
                    _aus.WriteLine(string.Format(c, "{0} Gebäude {1} ({2} Zonen), ε {10} K: Option 2 {3}, t_V {4} h, Vorausschauen {5}, Tage ohne Ankunft {6}, " +
                                                    "Abweichungen {7}, unerreichbar {11} Tage, Unterschreitung bis {12:0.00} K, Nächte ohne Absenkung {13}, " +
                                                    "Median {14:0.#} h, Rechenzeit {8:0} ms gegen {9:0} ms ohne Aufheizen",
                                                 projektId, l.Gebaeude, zonen, v != null ? "gerechnet" : "Rückfall",
                                                 v?.VorheizzeitMaxH ?? 0, v?.Vorausschauen ?? 0, v?.TageOhneAnkunftAnzahl ?? 0,
                                                 v?.TageAbweichung ?? 0, ms, msOhne, eps, v?.TageUnerreichbar ?? 0,
                                                 v?.UnterschreitungMaxK ?? 0.0, v?.NaechteOhneAbsenkung ?? 0, v?.BedarfMedianH ?? double.NaN));
                }
            }
        }
    }
}
