using System;
using System.Diagnostics;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Sondenfeld mit Entzugsrückwirkung</b> (<see cref="Erdsondenfeld"/>, Konzept Simulationsablauf 23).
    ///
    /// <para><b>Geprüft wird:</b> der Vergleich mit der unendlichen Linienquelle einer Einzelsonde unter
    /// konstantem Entzug; die Richtung der Rückwirkung (mehr Sonden, größere Länge, Regeneration heben die
    /// Soletemperatur); die Mehrjahresdrift über die Vorjahre; die Rechenzeit für 8760 Stunden; im Lauf des
    /// Projekts 1029 der Testdatenbank die Reihe der Quelltemperatur der Sonde.</para>
    /// </summary>
    public class ErdsondenfeldTests
    {
        private readonly ITestOutputHelper _aus;

        public ErdsondenfeldTests(ITestOutputHelper aus) { _aus = aus; }

        // Mergel/Lehm nach VDI 4640 Blatt 1, Tabelle 1
        private const double LAMBDA = 2.4;
        private const double RHOCP = 2.0;
        private const double TU = 12.0;

        /// <summary>Exponentialintegral E₁(x) über die Reihe (x &lt; 1).</summary>
        private static double E1(double x)
        {
            double summe = -0.5772156649015329 - Math.Log(x);
            double term = 1.0;
            for (int k = 1; k < 60; k++)
            {
                term *= -x / k;
                summe -= term / k;
            }
            return summe;
        }

        /// <summary>Synthetischer Heizlastgang [kW]: proportional zu max(0, 15 − T) eines Sinusjahres.</summary>
        private static double[] Heizlast(double spitzeKw, double sommerRueckKw = 0)
        {
            double[] last = new double[8760];
            for (int s = 0; s < 8760; s++)
            {
                double t = 9.5 - 9.0 * Math.Cos(2 * Math.PI * (s - 480) / 8760.0);
                double g = 15.0 - t;
                last[s] = g > 0 ? spitzeKw * g / 15.5 : -sommerRueckKw;
            }
            return last;
        }

        [Fact]
        public void Einzelsonde_unter_konstantem_Entzug_folgt_der_Linienquelle()
        {
            // sehr lange Sonde: in 1000 h reicht die Störung rund 4 m weit, Enden wirken nicht
            var feld = new Erdsondenfeld(2000.0, 1, LAMBDA, RHOCP, TU);
            const double q = 50.0;                     // W/m
            double entzugKw = q * 2000.0 / 1000.0;     // 100 kW
            double[] t = feld.Durchrechnen(Enumerable.Repeat(entzugKw, 8760).ToArray());

            double a = LAMBDA / (RHOCP * 1.0e6);
            foreach (int stunde in new[] { 10, 100, 1000, 3000 })
            {
                double x = Erdsondenfeld.BOHRLOCHRADIUS_M * Erdsondenfeld.BOHRLOCHRADIUS_M / (4.0 * a * stunde * 3600.0);
                double erwartet = TU - q / (4.0 * Math.PI * LAMBDA) * E1(x) - q * Erdsondenfeld.BOHRLOCHWIDERSTAND;
                double absenkung = TU - erwartet;
                _aus.WriteLine($"t = {stunde} h: Modell {t[stunde]:0.0000} °C, Linienquelle {erwartet:0.0000} °C");
                Assert.InRange(t[stunde], erwartet - 0.01 * absenkung, erwartet + 0.01 * absenkung);
            }
        }

        [Fact]
        public void Mehr_Sonden_und_groessere_Laenge_heben_die_Soletemperatur()
        {
            double[] last = Heizlast(20.0);
            double Min(int anzahl, double laenge)
                => new Erdsondenfeld(laenge, anzahl, LAMBDA, RHOCP, TU).Durchrechnen(last).Min();

            double vier90 = Min(4, 90), sechs90 = Min(6, 90), vier120 = Min(4, 120), zwei90 = Min(2, 90);
            _aus.WriteLine($"2×90 {zwei90:0.00}, 4×90 {vier90:0.00}, 6×90 {sechs90:0.00}, 4×120 {vier120:0.00}");
            Assert.True(zwei90 < vier90 && vier90 < sechs90, "mehr Sonden");
            Assert.True(vier90 < vier120, "größere Länge");
            Assert.True(vier90 < TU, "der Entzug senkt die Temperatur");
        }

        [Fact]
        public void Regeneration_hebt_die_Soletemperatur()
        {
            double[] ohne = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU).Durchrechnen(Heizlast(20.0));
            double[] mit = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU).Durchrechnen(Heizlast(20.0, 8.0));
            // Dezember: nach dem Sommer mit Rückspeisung wärmer
            double dezOhne = ohne.Skip(8016).Average(), dezMit = mit.Skip(8016).Average();
            _aus.WriteLine($"Dezember ohne {dezOhne:0.00} °C, mit Rückspeisung {dezMit:0.00} °C");
            Assert.True(dezMit > dezOhne + 0.1);
            // vor dem Sommer ist nichts zurückgespeist: die Reihen stimmen überein
            Assert.Equal(ohne[1000], mit[1000], 9);
        }

        [Fact]
        public void Vorjahre_senken_die_Starttemperatur_mit_abnehmender_Drift()
        {
            double[] monate = { 30, 26, 20, 12, 5, 0, 0, 0, 4, 12, 20, 27 };   // W/m
            double Start(int vorjahre)
            {
                var f = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
                f.VorjahreSetzen(monate, vorjahre);
                return f.Starttemperatur;
            }

            double j1 = Start(0), j2 = Start(1), j5 = Start(4), j10 = Start(9), j20 = Start(19);
            _aus.WriteLine($"Start Jahr 1 {j1:0.000}, 2 {j2:0.000}, 5 {j5:0.000}, 10 {j10:0.000}, 20 {j20:0.000}");
            Assert.Equal(TU, j1, 9);
            Assert.True(j1 > j2 && j2 > j5 && j5 > j10 && j10 > j20, "Drift nach unten");
            // die Drift wird je Jahr kleiner
            Assert.True((j2 - j5) / 3.0 > (j5 - j10) / 5.0);
            Assert.True((j5 - j10) / 5.0 > (j10 - j20) / 10.0);
        }

        [Fact]
        public void Jahresentzug_verteilt_sich_nach_Heizgradstunden()
        {
            var f = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
            double[] aussen = new double[8760];
            for (int s = 0; s < 8760; s++) aussen[s] = 9.5 - 9.0 * Math.Cos(2 * Math.PI * (s - 480) / 8760.0);
            double[] m = f.MonatslastenAusJahresentzug(36000.0, aussen);
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            double summeKwh = 0;
            for (int i = 0; i < 12; i++) summeKwh += m[i] * tage[i] * 24 * f.Sondenmeter / 1000.0;
            Assert.Equal(36000.0, summeKwh, 6);
            Assert.True(m[0] > m[3] && m[6] < m[0]);
        }

        [Fact]
        public void Achtausendsiebenhundertsechzig_Stunden_rechnen_unter_200_ms()
        {
            double[] last = Heizlast(20.0);
            long beste = long.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var uhr = Stopwatch.StartNew();
                var feld = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
                feld.VorjahreSetzen(new double[] { 30, 26, 20, 12, 5, 0, 0, 0, 4, 12, 20, 27 }, 9);
                feld.Durchrechnen(last);
                uhr.Stop();
                beste = Math.Min(beste, uhr.ElapsedMilliseconds);
            }
            _aus.WriteLine($"Aufbau, Vorjahre und 8760 Stunden: {beste} ms");
            Assert.True(beste < 200, $"{beste} ms");
        }

        [Fact]
        public void Die_Sprungantwort_auf_dem_Raster_trifft_das_Integral()
        {
            var feld = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
            foreach (double tau in new[] { 3.0, 77.0, 1234.0, 8000.0, 50000.0 })
            {
                double g = feld.G(tau), d = feld.GDirekt(tau);
                Assert.InRange(g, d * 0.995, d * 1.005);
            }
            Assert.Equal(0.0, feld.G(0));
        }
    }

    /// <summary>Lauf des Projekts 1029 in der seriellen Sammlung der Testdatenbank.</summary>
    [Collection("Testdatenbank")]
    public class ErdsondenfeldLaufTests
    {
        private readonly ITestOutputHelper _aus;

        public ErdsondenfeldLaufTests(ITestOutputHelper aus) { _aus = aus; }

        /// <summary>
        /// Projekt 1029 der Testdatenbank (Sonde 90 m, 4 Sonden, Mergel/Lehm) auf einer Arbeitskopie: Die
        /// Quelltemperatur der Sonde ist keine Konstante mehr, sie liegt unter der ungestörten Temperatur und
        /// ihr Tiefstwert fällt in die Heizzeit.
        /// </summary>
        [Fact]
        public void Projekt_1029_rechnet_die_Sonde_mit_Rueckwirkung()
        {
            var aus = _aus;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(1029, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationWaermepumpe wp = laeufer.sim.simulation_wp;
            Assert.NotEmpty(wp.Sondenfelder);

            for (int i = 0; i < wp.Quelltemperaturen.Count; i++)
            {
                Erdsondenfeld feld = wp.Sondenfeld(i);
                if (feld == null) continue;
                double[] r = wp.Quelltemperaturen[i];
                double min = r.Min(), max = r.Max(), mittel = r.Average();
                int stundeMin = Array.IndexOf(r, min);
                double waerme = wp.Modul_WP_Waermeproduktion[i], strom = wp.Modul_WP_Strombedarf[i];
                aus.WriteLine($"Modul {i}: T_u {feld.TUngestoert:0.00} °C, min {min:0.00} (h {stundeMin}), max {max:0.00}, " +
                              $"Mittel {mittel:0.00} °C, Vorjahre {feld.Vorjahre}, Entzug {feld.EntzugKwh:0} kWh, " +
                              $"JAZ {(strom > 0 ? waerme / strom : 0):0.000}");
                Assert.True(max - min > 0.5, "die Quelltemperatur ist keine Konstante");
                Assert.True(mittel < feld.TUngestoert);
                Assert.True(stundeMin < 2160 || stundeMin > 7296, "Tiefstwert in der Heizzeit");
                Assert.True(feld.EntzugKwh > 0);
            }

            foreach (ErdreichAuswertung.AnlageErgebnis e in ErdreichAuswertung.FuerProjekt(1029))
                aus.WriteLine($"VDI 4640 Anlage {e.ID_Anlage}: Entzug {e.JahresentzugKWh:0} kWh/a, max {e.MaxEntzugW:0} W, " +
                              $"{e.VolllastStunden:0} h/a, Frost {e.FrostStunden}/{e.BetriebsStunden} h, {e.Pruefung.Anzeigetext()}");
            var vp = VDI4640Pruefung.Vorpruefung(ErdreichVorpruefungCtrl.Auslegungswerte(1029, wp.Sondenfelder[0].Key),
                                                 ErdreichAuswertung.KlimazoneDesProjekts(1029));
            aus.WriteLine($"Vorprüfung: {vp.Quelle}, {vp.JahresentzugKWh:0} kWh/a, fehlt: {vp.Fehlt}");
        }
    }
}
