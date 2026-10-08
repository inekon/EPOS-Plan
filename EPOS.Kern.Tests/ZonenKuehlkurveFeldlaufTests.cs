using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KZ2 — die Kühlkurve im Mehrzonenweg am Referenzprojekt 1058</b> (Entwurf KK 2.8, 2.9, Abschnitt 4): 1058 rechnet mit
    /// eingeschaltetem Kernschalter, das Gebäude über die Probenaht <see cref="KuehlkurveKernschalter.Probewerte"/> in zwei
    /// Hälften geteilt (<see cref="GebaeudeZonenuebernahme.AlsEineZone(ProjektGebaeudeModel, double)"/> mit Faktor 0,5; Zone 2
    /// kühlt 1 K wärmer mit Gebläsekonvektor) — der Vorgriff auf RP-KKZ, in der Arbeitskopie der Testdatenbank, ohne sie zu
    /// ändern. Die Probe hält den Lauf auf AK3 im Mehrzonenweg; die Messreihe der Rechenzeit läuft nur mit <c>KZ2_MESSUNG</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenKuehlkurveFeldlaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public ZonenKuehlkurveFeldlaufTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        /// <summary>Die zwei Hälften des Gebäudes (Faktor 0,5); Zone 2 kühlt 1 K wärmer mit Gebläsekonvektor.</summary>
        private static GebaeudeZonensatz[] Teilen(ProjektGebaeudeModel g)
        {
            GebaeudeZonensatz a = GebaeudeZonenuebernahme.AlsEineZone(g, 0.5), b = GebaeudeZonenuebernahme.AlsEineZone(g, 0.5);
            return new[]
            {
                new GebaeudeZonensatz(1, "Hälfte 1", a.Bauteile, a.Nutzflaeche_M2, new Zoneneingaben(Nutzflaeche: a.Nutzflaeche_M2), 1),
                new GebaeudeZonensatz(2, "Hälfte 2", b.Bauteile, b.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: b.Nutzflaeche_M2, KuehlSollwert: (g.Kuehl_Sollwert ?? 26.0) + 1.0,
                                                        KuehlUebergabeArt: DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR), 2),
            };
        }

        /// <summary>Ein Lauf; <paramref name="schalter"/> false = der heutige Weg (keine Probewerte, eine Zone).</summary>
        private static (SimulationControl sim, double sekunden) Rechnen(int projekt, bool schalter, bool zwei, bool kurve, double? kK)
        {
            using (KuehlkurveKernschalter.Schalten(schalter))
            {
                KuehlkurveKernschalter.Probewerte = g =>
                {
                    g.Kuehlkurve_Aktiv = kurve;
                    g.Kuehlkurve_Raumeinfluss = kK;
                    if (zwei && (g.Zonen == null || g.Zonen.Count < 2)) g.Zonen = Teilen(g);
                };
                try
                {
                    SimulationProtokoll.NeuStarten();
                    var r = new SimulationRunner();
                    var uhr = Stopwatch.StartNew();
                    bool ok = r.Simuliere(projekt, out string fehler);
                    double s = uhr.Elapsed.TotalSeconds;
                    Assert.True(ok, "Lauf " + projekt + " (zwei " + zwei + ", Kurve " + kurve + ", k_K " + kK + ") gescheitert: " + fehler);
                    return (r.sim, s);
                }
                finally { KuehlkurveKernschalter.Probewerte = null; }
            }
        }

        /// <summary>
        /// 1058 zweizonig mit Kühlkurve und k_K 3: Der Kreis rechnet das Mehrzonengebäude, baut den Raumeinfluss über den
        /// Kühlkreis, die Kurve liegt nie unter der Vorlaufgrenze; Durchläufe unter der Höchstzahl.
        /// </summary>
        [Fact]
        public void Projekt_1058_zweizonig_rechnet_mit_Kuehlkurve_im_Kreis()
        {
            if (!_db.Vorhanden) return;
            (SimulationControl sim, double s) = Rechnen(1058, true, true, true, 3.0);
            Anlagenkopplung kreis = sim.simulation_Waermebedarf.Ak3?.Kreis;
            Assert.NotNull(kreis);
            Assert.Contains(sim.simulation_Waermebedarf.Ak3.Gebaeude, e => e.Stepper.Zonenzahl == 2);
            KuehlRaumeinfluss k2 = kreis.KuehlRaumeinfluss;
            Assert.NotNull(k2);
            Assert.True(k2.Kuehlstunden > 100, "Kühlstunden " + k2.Kuehlstunden);
            Assert.True(kreis.DurchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            _aus.WriteLine("1058 zweizonig k_K 3: {0:0.00} s, Kühlstunden {1}, Vorlauf Mittel {2:0.00} / min {3:0.00} °C, an der Grenze {4} h, " +
                           "abgesenkt {5} h ({6:0.0} Kh), festgehalten {7}, Durchläufe Mittel {8:0.000} / max {9}",
                           s, k2.Kuehlstunden, k2.KuehlVorlaufMittelC, k2.KuehlVorlaufMinC, k2.StundenAnVorlaufgrenze, k2.StundenAbgesenkt,
                           k2.AbsenkungSummeKh, k2.StundenFestgehalten, kreis.DurchlaeufeMittel, kreis.DurchlaeufeMax);
        }

        /// <summary>
        /// <b>Messreihe der Rechenzeit</b> (Entwurf KK 2.8, 2.9; nur mit <c>KZ2_MESSUNG</c> = Zieldatei): 1058 einzonig ohne
        /// Schalter und mit Kurve, zweizonig mit festem Kühlvorlauf (KZ1), mit Kurve ohne und mit Raumeinfluss — Laufzeit
        /// (bester von zwei Läufen nach einem Aufwärmlauf), Feldläufe, Durchläufe des Kreises, Kühlstunden.
        /// </summary>
        [Fact]
        public void Messreihe_der_Rechenzeit_im_Mehrzonenweg()
        {
            string ziel = Environment.GetEnvironmentVariable("KZ2_MESSUNG");
            if (!_db.Vorhanden || string.IsNullOrEmpty(ziel)) return;
            CultureInfo c = CultureInfo.InvariantCulture;
            void Messen(string name, bool schalter, bool zwei, bool kurve, double? kK)
            {
                (SimulationControl sim, double s) a = Rechnen(1058, schalter, zwei, kurve, kK);
                double s2 = Rechnen(1058, schalter, zwei, kurve, kK).sekunden;
                Ak3Weg weg = a.sim.simulation_Waermebedarf.Ak3;
                Anlagenkopplung kreis = weg.Kreis;
                var kreise = weg.FruehereKreise.Concat(new[] { kreis }).ToList();
                KuehlRaumeinfluss k2 = kreis.KuehlRaumeinfluss;
                File.AppendAllText(ziel, string.Format(c,
                    "{0}|Laufzeit {1:0.00} s|Feldläufe {2}|Durchläufe Mittel {3:0.000} max {4}|Kühlstunden {5}|Vorlauf {6:0.00}/{7:0.00} °C|" +
                    "abgesenkt {8} h {9:0.0} Kh|festgehalten {10}\n",
                    name, Math.Min(a.s, s2), kreise.Count, kreis.DurchlaeufeMittel, kreise.Max(k => k.DurchlaeufeMax),
                    k2?.Kuehlstunden ?? 0, k2?.KuehlVorlaufMittelC ?? double.NaN, k2?.KuehlVorlaufMinC ?? double.NaN,
                    k2?.StundenAbgesenkt ?? 0, k2?.AbsenkungSummeKh ?? 0.0, k2?.StundenFestgehalten ?? 0));
            }
            Rechnen(1058, false, false, false, null);   // Aufwärmen, nicht gezählt
            Messen("einzonig aus", false, false, false, null);
            Messen("einzonig Kurve k_K 3", true, false, true, 3.0);
            Messen("zweizonig fest (KZ1)", true, true, false, null);
            Messen("zweizonig Kurve k_K 0", true, true, true, null);
            Messen("zweizonig Kurve k_K 3", true, true, true, 3.0);
        }
    }
}
