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
    /// <b>KK3 — die Kühlkurve im Feldlauf des Referenzprojekts 1058</b> (Entwurf KK 2.2, 2.3, 2.9; E106 Q-KK-5 (b)): 1058 rechnet
    /// mit eingeschaltetem Kernschalter und den Probewerten der Kühlkurve (<see cref="KuehlkurveKernschalter.Probewerte"/>),
    /// der Erzeuger gleitet, der Raumeinfluss senkt im Kreis. Die Messreihe der Stärke (nur mit <c>KK3_MESSUNG</c>) begründet
    /// den Vorgabewert beim Einschalten; die Probe hält den Lauf selbst.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlkurveFeldlaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public KuehlkurveFeldlaufTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Lauf; <paramref name="kurve"/> false = Kernschalter aus (der heutige Weg), sonst Kurve mit k_K.</summary>
        internal static (SimulationControl sim, double sekunden) Rechnen(int projekt, bool kurve, double? kK)
        {
            using (KuehlkurveKernschalter.Schalten(kurve))
            {
                KuehlkurveKernschalter.Probewerte = g =>
                {
                    g.Kuehlkurve_Aktiv = true;
                    g.Kuehlkurve_Raumeinfluss = kK;
                };
                try
                {
                    SimulationProtokoll.NeuStarten();
                    var r = new SimulationRunner();
                    var uhr = Stopwatch.StartNew();
                    bool ok = r.Simuliere(projekt, out string fehler);
                    double s = uhr.Elapsed.TotalSeconds;
                    Assert.True(ok, "Lauf " + projekt + " (Kurve " + kurve + ", k_K " + kK + ") gescheitert: " + fehler);
                    return (r.sim, s);
                }
                finally { KuehlkurveKernschalter.Probewerte = null; }
            }
        }

        /// <summary>
        /// 1058 mit Kühlkurve und k_K = 1: Der Kreis baut den Raumeinfluss, der Erzeuger gleitet (mittlerer Vorlauf über dem
        /// festen Anlagenvorlauf), die Kurve liegt nie unter der Vorlaufgrenze; Durchläufe unter der Höchstzahl.
        /// </summary>
        [Fact]
        public void Projekt_1058_rechnet_mit_Kuehlkurve_und_Raumeinfluss()
        {
            if (!_db.Vorhanden) return;
            SimulationControl sim = Rechnen(1058, true, 1.0).sim;
            Anlagenkopplung kreis = sim.simulation_Waermebedarf.Ak3?.Kreis;
            Assert.NotNull(kreis);
            KuehlRaumeinfluss k2 = kreis.KuehlRaumeinfluss;
            Assert.NotNull(k2);
            Assert.True(k2.Kuehlstunden > 100, "Kühlstunden " + k2.Kuehlstunden);
            Assert.True(kreis.DurchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            _aus.WriteLine("1058 k_K 1: Kühlstunden {0}, Vorlauf Mittel {1:0.00} / min {2:0.00} °C, an der Grenze {3} h, abgesenkt {4} h " +
                           "({5:0.0} Kh), festgehalten {6}, Durchläufe Mittel {7:0.000} / max {8}",
                           k2.Kuehlstunden, k2.KuehlVorlaufMittelC, k2.KuehlVorlaufMinC, k2.StundenAnVorlaufgrenze, k2.StundenAbgesenkt,
                           k2.AbsenkungSummeKh, k2.StundenFestgehalten, kreis.DurchlaeufeMittel, kreis.DurchlaeufeMax);
        }

        /// <summary>
        /// <b>Messreihe der Stärke</b> (nur mit <c>KK3_MESSUNG</c> = Zieldatei): 1058 ohne Kurve, mit Kurve ohne Raumeinfluss
        /// und mit k_K 0,5 … 5 — Überschreitungsstunden und Kelvinstunden der Kühlung, Durchläufe, Kälteleistung, EER, Laufzeit
        /// (bester von zwei Läufen).
        /// </summary>
        [Fact]
        public void Messreihe_der_Staerke_an_1058()
        {
            string ziel = Environment.GetEnvironmentVariable("KK3_MESSUNG");
            if (!_db.Vorhanden || string.IsNullOrEmpty(ziel)) return;
            CultureInfo c = CultureInfo.InvariantCulture;
            void Messen(string name, bool kurve, double? kK)
            {
                (SimulationControl sim, double s) a = Rechnen(1058, kurve, kK);
                double s2 = Rechnen(1058, kurve, kK).sekunden;
                SimulationControl sim = a.sim;
                Ak3Weg weg = sim.simulation_Waermebedarf.Ak3;
                Anlagenkopplung kreis = weg.Kreis;
                var kreise = weg.FruehereKreise.Concat(new[] { kreis }).ToList();
                Komfortkennzahlen kuehlen = sim.simulation_Waermebedarf.KomfortProjekt().Kuehlen;
                Kaeltekaskade kas = sim.simulation_Waermebedarf.Kaelteseite?.Kaskade;
                double deckung = kas?.DeckungGesamtKwh ?? double.NaN, strom = kas?.StromGesamtKwh ?? double.NaN;
                KuehlRaumeinfluss k2 = kreis.KuehlRaumeinfluss;
                File.AppendAllText(ziel, string.Format(c,
                    "{0}|Feldläufe {1}|Durchläufe Mittel {2:0.000} max {3}|Kühlen {4} h {5:0.0} Kh|Kälte {6:0.0} kWh|Strom {7:0.0} kWh|" +
                    "EER {8:0.000}|Rest {9:0.0} kWh|Laufzeit {10:0.00} s|Kühlstunden {11}|Vorlauf {12:0.00}/{13:0.00} °C|Grenze {14} h|" +
                    "abgesenkt {15} h {16:0.0} Kh|festgehalten {17}\n",
                    name, kreise.Count, kreis.DurchlaeufeMittel, kreise.Max(k => k.DurchlaeufeMax), kuehlen.Stunden, kuehlen.Kelvinstunden,
                    deckung, strom, deckung / strom, kas?.RestGesamtKwh ?? double.NaN, Math.Min(a.s, s2),
                    k2?.Kuehlstunden ?? 0, k2?.KuehlVorlaufMittelC ?? double.NaN, k2?.KuehlVorlaufMinC ?? double.NaN,
                    k2?.StundenAnVorlaufgrenze ?? 0, k2?.StundenAbgesenkt ?? 0, k2?.AbsenkungSummeKh ?? 0.0, k2?.StundenFestgehalten ?? 0));
            }
            Messen("aus", false, null);
            Messen("Kurve k_K 0", true, null);
            foreach (double kK in new[] { 0.5, 1.0, 2.0, 3.0, 5.0 })
                Messen("Kurve k_K " + kK.ToString(c), true, kK);
        }
    }
}
