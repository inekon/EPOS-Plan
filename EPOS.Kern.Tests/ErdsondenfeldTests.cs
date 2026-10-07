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

        /// <summary>
        /// Zweiter Feldlauf (Konzept 23.4): Der zusammengefasste Kern der neun Vorjahre trifft die
        /// Faltung über jedes Vorjahr einzeln, Stunde für Stunde.
        /// </summary>
        [Fact]
        public void Stundenlast_der_Vorjahre_trifft_die_Einzelfaltung()
        {
            double[] last = Heizlast(20.0, 4.0);
            var feld = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
            feld.VorjahreSetzenStuendlich(last, 9);
            Assert.Equal(9, feld.Vorjahre);

            foreach (int t in new[] { 0, 1, 2000, 5000, 8759 })
            {
                double erwartet = 0;
                for (int y = 1; y <= 9; y++)
                    for (int i = 0; i < 8760; i++)
                    {
                        double q = last[i] * 1000.0 / feld.Sondenmeter;
                        double lag = t + y * 8760.0 - i;
                        erwartet += q * (feld.G(lag) - feld.G(lag - 1));
                    }
                Assert.Equal(erwartet, feld.VorjahrAbsenkung(t), 9);
            }
        }

        /// <summary>
        /// Jahr 10 liegt unter Jahr 1, wenn die neun Vorjahre die eigene Last tragen; zwei gleiche Felder
        /// rechnen bitgleich.
        /// </summary>
        [Fact]
        public void Jahr_zehn_mit_eigener_Last_liegt_unter_Jahr_eins_und_ist_deterministisch()
        {
            double[] last = Heizlast(20.0);
            double[] jahr1 = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU).Durchrechnen(last);

            double[] Jahr10()
            {
                var f = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
                f.VorjahreSetzenStuendlich(last, f.Betrachtungsjahr - 1);
                return f.Durchrechnen(last);
            }
            double[] a = Jahr10(), b = Jahr10();
            _aus.WriteLine($"Jahr 1: min {jahr1.Min():0.00}, Mittel {jahr1.Average():0.00} °C; " +
                           $"Jahr 10: min {a.Min():0.00}, Mittel {a.Average():0.00} °C");
            Assert.True(a.Min() < jahr1.Min() && a.Average() < jahr1.Average());
            Assert.Equal(a, b);
        }

        /// <summary>Rückspeisung in den Vorjahren hebt das Rechenjahr (Regeneration über die Stundenlast).</summary>
        [Fact]
        public void Rueckspeisung_der_Vorjahre_hebt_das_Rechenjahr()
        {
            double[] entzug = Heizlast(20.0), netto = Heizlast(20.0, 8.0);
            double Mittel(double[] vorjahre)
            {
                var f = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
                f.VorjahreSetzenStuendlich(vorjahre, 9);
                return f.Durchrechnen(netto).Average();
            }
            Assert.True(Mittel(netto) > Mittel(entzug) + 0.1);
        }

        /// <summary>Die Vorjahre aus der Stundenlast kosten eine Faltung über ein Jahr — unter einer festen Grenze.</summary>
        [Fact]
        public void Stundenlast_der_Vorjahre_rechnet_unter_1500_ms()
        {
            double[] last = Heizlast(20.0, 4.0);
            long beste = long.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var feld = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU);
                var uhr = Stopwatch.StartNew();
                feld.VorjahreSetzenStuendlich(last, 9);
                uhr.Stop();
                beste = Math.Min(beste, uhr.ElapsedMilliseconds);
            }
            _aus.WriteLine($"Vorjahre aus der Stundenlast: {beste} ms");
            Assert.True(beste < 1500, $"{beste} ms");
        }

        /// <summary>Die Geometrie trägt die Normwerte als Vorgabe; unbrauchbare Werte fallen auf die Norm.</summary>
        [Fact]
        public void Sondenfeldgeometrie_hat_die_Norm_als_Vorgabe()
        {
            var n = Sondenfeldgeometrie.Norm;
            Assert.Equal(6.0, n.AbstandM);
            Assert.Equal(0.075, n.BohrlochradiusM);
            Assert.Equal(0.10, n.Bohrlochwiderstand);
            Assert.Equal(2.0, n.KopfueberdeckungM);
            Assert.Equal(10, n.Betrachtungsjahr);
            Assert.Equal(Sondenanordnung.Quadratisch, n.Anordnung);

            var b = new Sondenfeldgeometrie { AbstandM = -1, BohrlochradiusM = double.NaN, Bohrlochwiderstand = -0.1,
                                              KopfueberdeckungM = -2, Betrachtungsjahr = 0 }.Bereinigt();
            Assert.Equal(6.0, b.AbstandM);
            Assert.Equal(0.075, b.BohrlochradiusM);
            Assert.Equal(0.10, b.Bohrlochwiderstand);
            Assert.Equal(2.0, b.KopfueberdeckungM);
            Assert.Equal(10, b.Betrachtungsjahr);

            var feld = new Erdsondenfeld(90, 4, LAMBDA, RHOCP, TU, new Sondenfeldgeometrie { AbstandM = 8, Betrachtungsjahr = 5 });
            Assert.Equal(8.0, feld.AbstandM);
            Assert.Equal(5, feld.Betrachtungsjahr);
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
            var uhr = Stopwatch.StartNew();
            Assert.True(laeufer.Simuliere(1029, out string fehler), "Lauf gescheitert: " + fehler);
            aus.WriteLine($"Rechenzeit Simuliere(1029): {uhr.ElapsedMilliseconds} ms");
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

        /// <summary>Ergebnis eines Laufs mit Sondenfeld (erstes Feld des Laufs).</summary>
        private sealed class Feldlauf
        {
            public double[] Soletemperatur;
            public double Mittel, Min, EntzugKwh, RueckspeisungKwh, VorjahrNettoKwh, Jaz, KaelteWpKwh, KaelteMaschineKwh;
            public int Vorjahre;
            public long Millisekunden;
            public double Jahr1Mittel, Jahr1Min;

            public override string ToString()
            {
                return $"Sole min {Min:0.00}, Mittel {Mittel:0.00} °C (Jahr 1 aus derselben Last: min {Jahr1Min:0.00}, " +
                       $"Mittel {Jahr1Mittel:0.00} °C), Vorjahre {Vorjahre}, Entzug {EntzugKwh:0} kWh, " +
                       $"Rückspeisung {RueckspeisungKwh:0} kWh, Nettolast je Vorjahr {VorjahrNettoKwh:0} kWh, " +
                       $"JAZ {Jaz:0.000}, Kälte der WP {KaelteWpKwh:0} kWh, der Kältemaschine {KaelteMaschineKwh:0} kWh, {Millisekunden} ms";
            }
        }

        private static Feldlauf Rechnen(int projekt, bool regeneration)
        {
            var laeufer = new SimulationRunner();
            laeufer.sim.RegenerationRechnen = regeneration;
            var uhr = Stopwatch.StartNew();
            Assert.True(laeufer.Simuliere(projekt, out string fehler), "Lauf gescheitert: " + fehler);
            uhr.Stop();
            SimulationWaermepumpe wp = laeufer.sim.simulation_wp;
            Assert.NotEmpty(wp.Sondenfelder);
            Assert.True(wp.ZweiterFeldlauf, "der zweite Feldlauf hat gerechnet");

            int idAnlage = wp.Sondenfelder[0].Key;
            Erdsondenfeld feld = wp.Sondenfelder[0].Value;
            int modul = -1;
            for (int i = 0; i < wp.Quelltemperaturen.Count; i++)
                if (ReferenceEquals(wp.Sondenfeld(i), feld)) { modul = i; break; }
            double[] r = (double[])wp.Quelltemperaturen[modul].Clone();
            var v = wp.FeldvorgabeAktuell[idAnlage];

            // Jahr 1 aus derselben Last: ein frisches Feld ohne Vorjahre
            double rhoCpMJ = feld.Lambda / feld.A_m2s / 1.0e6;
            double[] jahr1 = new Erdsondenfeld(feld.LaengeM, feld.Anzahl, feld.Lambda, rhoCpMJ, feld.TUngestoert)
                .Durchrechnen(feld.LastKw());

            double waerme = wp.Modul_WP_Waermeproduktion[modul], strom = wp.Modul_WP_Strombedarf[modul];
            double kaelteWp = 0, kaelteMaschine = 0;
            Kaeltekaskade kaskade = laeufer.sim.simulation_Waermebedarf?.Kaelteseite?.Kaskade;
            if (kaskade != null)
                foreach (Kaelteerzeuger e in kaskade.Erzeuger)
                    if (e.Maschine != null) kaelteMaschine += e.KaelteGesamtKwh; else kaelteWp += e.KaelteGesamtKwh;
            return new Feldlauf
            {
                Soletemperatur = r, Mittel = r.Average(), Min = r.Min(), EntzugKwh = feld.EntzugKwh,
                RueckspeisungKwh = v.RueckspeisungKw.Sum(), VorjahrNettoKwh = v.VorjahrLastKw.Sum(),
                Vorjahre = feld.Vorjahre, Jaz = strom > 0 ? waerme / strom : 0,
                KaelteWpKwh = kaelteWp, KaelteMaschineKwh = kaelteMaschine, Millisekunden = uhr.ElapsedMilliseconds,
                Jahr1Mittel = jahr1.Average(), Jahr1Min = jahr1.Min(),
            };
        }

        private static void SondeEinsetzen(int idAnlage, int anzahl, double laengeM)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET WQ_Typ = ?, WQ_Quellsystem = ?, WQ_Tiefe = ?, WQ_Anzahl = ?, WQ_Bodentyp = ? WHERE ID = ?",
                new DbParam("@t", WaermequelleClass.TYP_ERDREICH), new DbParam("@q", ErdreichTemperatur.QUELLSYSTEM_SONDE),
                new DbParam("@l", laengeM), new DbParam("@n", anzahl), new DbParam("@b", "MERGEL_LEHM"),
                new DbParam("@id", idAnlage));
        }

        /// <summary>
        /// Zweiter Feldlauf an Projekt 1029 (ohne Kühlbetrieb): Das Rechenjahr ist Jahr 10 nach neun
        /// Vorjahren mit der eigenen Stundenlast, es liegt unter Jahr 1 derselben Last, und die
        /// Regeneration ändert ohne Kühlbetrieb nichts — bitgleich.
        /// </summary>
        [Fact]
        public void Projekt_1029_rechnet_Jahr_zehn_mit_eigener_Last_und_ohne_Kuehlung_ohne_Regeneration()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Feldlauf mit = Rechnen(1029, true);
            Feldlauf ohne = Rechnen(1029, false);
            _aus.WriteLine("1029: " + mit);
            Assert.Equal(Erdsondenfeld.BETRACHTUNGSJAHR - 1, mit.Vorjahre);
            Assert.Equal(0.0, mit.RueckspeisungKwh);
            Assert.True(mit.Mittel < mit.Jahr1Mittel && mit.Min < mit.Jahr1Min, "Jahr 10 unter Jahr 1");
            // die Vorjahre tragen die Last des ersten Laufs derselben Anlage, also nahe am Rechenjahr
            Assert.InRange(mit.VorjahrNettoKwh, 0.8 * mit.EntzugKwh, 1.25 * mit.EntzugKwh);
            Assert.Equal(mit.Soletemperatur, ohne.Soletemperatur);
        }

        /// <summary>
        /// Projekt 1029 im Lauf: Der zweite Feldlauf (Jahr 10) liegt unter dem ersten (Jahr 1 ohne Klimazone
        /// und damit ohne Startschätzung), und er kostet weniger als einen zweiten vollen Lauf samt Bedarf.
        /// Gemessen wird je Fassung der beste von zwei warmen Läufen.
        /// </summary>
        [Fact]
        public void Projekt_1029_zweiter_Feldlauf_liegt_unter_Jahr_eins_mit_begrenzter_Rechenzeit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            (double mittel, double min, long ms) Lauf(bool zweiter)
            {
                long beste = long.MaxValue;
                double mittel = 0, min = 0;
                for (int k = 0; k < 2; k++)
                {
                    var laeufer = new SimulationRunner();
                    laeufer.sim.ZweitenFeldlaufRechnen = zweiter;
                    var uhr = Stopwatch.StartNew();
                    Assert.True(laeufer.Simuliere(1029, out string fehler), fehler);
                    uhr.Stop();
                    beste = Math.Min(beste, uhr.ElapsedMilliseconds);
                    SimulationWaermepumpe wp = laeufer.sim.simulation_wp;
                    Assert.Equal(zweiter, wp.ZweiterFeldlauf);
                    for (int i = 0; i < wp.Quelltemperaturen.Count; i++)
                        if (wp.Sondenfeld(i) != null) { mittel = wp.Quelltemperaturen[i].Average(); min = wp.Quelltemperaturen[i].Min(); break; }
                }
                return (mittel, min, beste);
            }

            var eins = Lauf(false);
            var zwei = Lauf(true);
            _aus.WriteLine($"1029 ein Feldlauf (Jahr 1): min {eins.min:0.00}, Mittel {eins.mittel:0.00} °C, {eins.ms} ms; " +
                           $"zwei Feldläufe (Jahr 10): min {zwei.min:0.00}, Mittel {zwei.mittel:0.00} °C, {zwei.ms} ms");
            Assert.True(zwei.mittel < eins.mittel && zwei.min < eins.min, "Jahr 10 unter Jahr 1");
            Assert.True(zwei.ms < 2 * eins.ms + 2000, $"{zwei.ms} ms gegen {eins.ms} ms");
        }

        /// <summary>
        /// Regeneration an einer Kopie von Projekt 1017 (Kälte mit der Wärmepumpe im Kühlbetrieb) mit
        /// eingesetzter Sonde 6 × 100 m: Die Kühlwärme speist zurück und hebt die Soletemperatur.
        /// </summary>
        [Fact]
        public void Projekt_1017_mit_Sonde_hebt_die_Soletemperatur_durch_Rueckspeisung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            SondeEinsetzen(10211, 6, 100.0);

            Feldlauf mit = Rechnen(1017, true);
            Feldlauf ohne = Rechnen(1017, false);
            _aus.WriteLine("1017 mit Regeneration:  " + mit);
            _aus.WriteLine("1017 ohne Regeneration: " + ohne);
            Assert.True(mit.KaelteWpKwh > 0 && mit.RueckspeisungKwh > mit.KaelteWpKwh, "Kühlwärme samt Verdichterarbeit speist zurück");
            Assert.Equal(0.0, ohne.RueckspeisungKwh);
            Assert.True(mit.Mittel > ohne.Mittel, "Rückspeisung hebt die mittlere Soletemperatur");
            Assert.True(mit.Min >= ohne.Min, "und den Tiefstwert nicht ab");
        }

        /// <summary>
        /// Kopie von Projekt 1055 (Kälte mit Kältemaschine und Trocken-Rückkühler, die Wärmepumpe heizt nur)
        /// mit eingesetzter Sonde: Die Kältemaschine speist nicht ins Erdreich — bitgleich mit und ohne
        /// Regeneration.
        /// </summary>
        [Fact]
        public void Projekt_1055_Kaeltemaschine_mit_Trockenkuehler_speist_nicht_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            SondeEinsetzen(23726, 6, 100.0);

            Feldlauf mit = Rechnen(1055, true);
            Feldlauf ohne = Rechnen(1055, false);
            _aus.WriteLine("1055: " + mit);
            Assert.True(mit.KaelteMaschineKwh > 0, "die Kältemaschine rechnet");
            Assert.Equal(0.0, mit.KaelteWpKwh);
            Assert.Equal(0.0, mit.RueckspeisungKwh);
            Assert.Equal(mit.Soletemperatur, ohne.Soletemperatur);
        }

        /// <summary>
        /// Taktverlust und Entzug (Konzept 23.2): Projekt 1039 rechnet seine Wärmepumpe an der Erdsonde. Mit
        /// 12 kW Mindestleistung taktet sie in der Übergangszeit; der Mehrstrom ist elektrische Arbeit beim
        /// Anfahren und geht nicht als Wärme aus dem Erdreich in den Kreis. Der gemeldete Entzug ist deshalb
        /// Wärme − (Strom − Taktstrom), also um den Taktanteil höher als Wärme − Strom, und gleich dem Entzug
        /// ohne Takten — Soletemperatur und Wärme bleiben Stunde für Stunde dieselben.
        /// </summary>
        [Fact]
        public void Projekt_1039_Taktstrom_mindert_den_Entzug_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Mindestleistung_kW = NULL WHERE ID_Projekt = ?", new DbParam("@p", 1039)));

            var ohneLauf = new SimulationRunner();
            Assert.True(ohneLauf.Simuliere(1039, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationWaermepumpe ohne = ohneLauf.sim.simulation_wp;
            Assert.False(ohne.RechnetMitTakt(0));
            Erdsondenfeld feldOhne = ohne.Sondenfeld(0);
            Assert.NotNull(feldOhne);
            Assert.Equal(ohne.Modul_WP_Waermeproduktion[0] - ohne.Modul_WP_Strombedarf[0], feldOhne.EntzugKwh, 6);
            double entzugOhne = feldOhne.EntzugKwh;
            double[] soleOhne = (double[])ohne.Quelltemperaturen[0].Clone();
            double[] waermeOhne = (double[])ohne.WP_Waermeproduktion_stuendlich.Clone();

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Mindestleistung_kW = 12 WHERE ID_Projekt = ?", new DbParam("@p", 1039)));
            var mitLauf = new SimulationRunner();
            Assert.True(mitLauf.Simuliere(1039, out fehler), "Lauf gescheitert: " + fehler);
            SimulationWaermepumpe mit = mitLauf.sim.simulation_wp;
            Assert.True(mit.RechnetMitTakt(0));
            double takt = mit.Taktstrom_KWh_WP[0];
            Assert.True(takt > 0, "die Wärmepumpe taktet");
            Erdsondenfeld feldMit = mit.Sondenfeld(0);
            double waermeMinusStrom = mit.Modul_WP_Waermeproduktion[0] - mit.Modul_WP_Strombedarf[0];
            _aus.WriteLine($"1039: Entzug ohne Takt {entzugOhne:0.000} kWh, mit Takt {feldMit.EntzugKwh:0.000} kWh, " +
                           $"Wärme − Strom {waermeMinusStrom:0.000} kWh, Taktstrom {takt:0.000} kWh");

            Assert.Equal(waermeMinusStrom + takt, feldMit.EntzugKwh, 6);
            Assert.Equal(entzugOhne, feldMit.EntzugKwh, 6);
            // Gleich bis auf die Rundung der Summenfolge (W − (S − T) gegen W − S).
            double soleAbw = 0, waermeAbw = 0;
            for (int h = 0; h < 8760; h++)
            {
                soleAbw = Math.Max(soleAbw, Math.Abs(soleOhne[h] - mit.Quelltemperaturen[0][h]));
                waermeAbw = Math.Max(waermeAbw, Math.Abs(waermeOhne[h] - mit.WP_Waermeproduktion_stuendlich[h]));
            }
            _aus.WriteLine($"größte Abweichung: Sole {soleAbw:E2} K, Wärme {waermeAbw:E2} kWh");
            Assert.True(soleAbw < 1e-9, "Soletemperatur wie ohne Takten");
            Assert.True(waermeAbw < 1e-9, "Wärme wie ohne Takten");
        }

        /// <summary>
        /// Rückspeisung ohne Taktanteil (Konzept 23.5): Kopie von Projekt 1017 mit Sonde und Mindestleistung —
        /// die Wärmepumpe taktet auch im Kühlbetrieb. Die Kühlwärme, die ins Feld zurückgeht, ist Kälte +
        /// Verdichterarbeit ohne Hilfsstromzuschlag und ohne den Mehrstrom aus Taktverlust.
        /// </summary>
        [Fact]
        public void Projekt_1017_Rueckspeisung_rechnet_ohne_Taktstrom()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            SondeEinsetzen(10211, 6, 100.0);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Mindestleistung_kW = 12 WHERE ID_Projekt = ?", new DbParam("@p", 1017)));

            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(1017, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationWaermepumpe wp = laeufer.sim.simulation_wp;
            Assert.True(wp.ZweiterFeldlauf);
            double rueck = wp.FeldvorgabeAktuell[wp.Sondenfelder[0].Key].RueckspeisungKw.Sum();

            double kaelte = 0, verdichter = 0, takt = 0;
            foreach (Kaelteerzeuger e in laeufer.sim.simulation_Waermebedarf.Kaelteseite.Kaskade.Erzeuger)
            {
                if (e.Maschine != null || e.Modulindex < 0) continue;
                kaelte += e.KaelteGesamtKwh;
                verdichter += e.StromGesamtKwh - e.HilfsstromGesamtKwh;
                takt += e.TaktstromKwh;
            }
            _aus.WriteLine($"1017: Rückspeisung {rueck:0.0} kWh, Kälte {kaelte:0.0}, Verdichter {verdichter:0.0}, Takt {takt:0.0} kWh");
            Assert.True(takt > 0, "die Wärmepumpe taktet im Kühlbetrieb");
            Assert.Equal(kaelte + verdichter - takt, rueck, 6);
        }

        /// <summary>
        /// Erdreichprüfung je Anlage (Konzept 23.2) an einer Arbeitskopie von Projekt 1008: Luft-Wasser-
        /// und Sole-Wärmepumpe nebeneinander. Die Prüfung gilt der Anlage mit Erdreichquelle allein — ihr
        /// Jahresentzug ist der Entzug ihres Sondenfelds, die Prüfung ist möglich, die Zeile trägt das Modul
        /// am Sondenfeld, und die Betriebsstunden sind die der Sole-Wärmepumpe (in 1008 die Grundlast: jede
        /// Stunde), nicht die der Kaskade.
        /// </summary>
        [Fact]
        public void Projekt_1008_prueft_die_Sole_Waermepumpe_neben_der_Luft_Waermepumpe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(1008, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationWaermepumpe wp = laeufer.sim.simulation_wp;
            Assert.True(wp.wp_list.Count >= 2, "zwei Wärmepumpen");
            Assert.True(wp.EntzugJeModulGefuehrt);
            int modul = wp.wp_list.IndexOf(10132);
            Assert.True(modul >= 0);
            Erdsondenfeld feld = wp.Sondenfeld(modul);
            Assert.NotNull(feld);

            var liste = ErdreichAuswertung.FuerProjekt(1008);
            ErdreichAuswertung.AnlageErgebnis e = Assert.Single(liste);
            _aus.WriteLine($"1008 Anlage {e.ID_Anlage} ({e.Modul}): Entzug {e.JahresentzugKWh:0} kWh/a (Feld {feld.EntzugKwh:0}), " +
                           $"max {e.MaxEntzugW:0} W, {e.VolllastStunden:0} h/a, Frost {e.FrostStunden}/{e.BetriebsStunden} h, " +
                           $"Prüfung möglich {e.Pruefung.Moeglich}");
            Assert.Equal(10132, e.ID_Anlage);
            Assert.Equal(wp.WP_Modul[modul], e.Modul);
            Assert.False(e.Unwirksam);
            Assert.True(e.MaxEntzugBelastbar);
            Assert.False(e.MaxEntzugGeschaetzt);
            Assert.True(e.JahresentzugKWh > 0);
            Assert.True(e.JahresentzugKWh >= feld.EntzugKwh - 1e-6, "Σ positiver Stunden ≥ Entzug des Feldes");
            Assert.Equal(feld.EntzugKwh, e.JahresentzugKWh, 0);
            Assert.True(e.MaxEntzugW > 0);
            Assert.True(e.Pruefung.Moeglich);
            Assert.Equal(wp.ModulBetriebStuendlich(modul).Count(b => b), e.BetriebsStunden);
            Assert.True(e.FrostStunden <= e.BetriebsStunden);
        }
    }
}
