using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der AK3-Weg mit zweitem Feldlauf der Erdsonde</b> (AK3-W3d; Konzept Simulationsablauf 23.4, Entwurf AK3 2.1):
    /// Der zweite Feldlauf wiederholt den ganzen Durchgang an derselben Instanz. Je Feldlauf rechnet der Kreis ein
    /// volles Jahr aus Pass 1 mit frischen Steppern; Zähler laufen je Kreis, die Abweichung wird einmal nachgeführt,
    /// das Ergebnis ist das des letzten Feldlaufs.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3FeldlaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        internal static SimulationControl Rechnen(int projekt, Ak3Kernmodus modus, bool zweiterFeldlauf)
        {
            using (Ak3Kernstufe.Schalten(modus))
            {
                SimulationProtokoll.NeuStarten();
                var r = new SimulationRunner();
                r.sim.ZweitenFeldlaufRechnen = zweiterFeldlauf;
                bool ok = r.Simuliere(projekt, out string fehler);
                Assert.True(ok, "Lauf " + projekt + " (" + modus + ", zweiter Feldlauf " + zweiterFeldlauf + ") gescheitert: " + fehler);
                return r.sim;
            }
        }

        private static void Bitgleich(double[] soll, double[] ist, string was)
        {
            Assert.Equal(soll.Length, ist.Length);
            for (int h = 0; h < soll.Length; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(soll[h]) == BitConverter.DoubleToInt64Bits(ist[h]),
                            was + ", Stunde " + h + ": " + soll[h] + " gegen " + ist[h]);
        }

        /// <summary>
        /// Gate „ohne Grenzen bitgleich“ je Feldlauf: 1047 (Sondenfeld, keine Sperre) mit Stufe AK3 rechnet mit und ohne
        /// zweiten Feldlauf Bit für Bit wie der heutige Weg; jeder Kreis zählt genau ein Jahr mit einem Durchlauf je Stunde.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Projekt_1047_rechnet_je_Feldlauf_ein_volles_Jahr_bitgleich_zum_heutigen_Weg(bool zweiter)
        {
            if (!_db.Vorhanden) return;
            SimulationControl ak1 = Rechnen(1047, Ak3Kernmodus.Aus, zweiter);
            SimulationControl ak3 = Rechnen(1047, Ak3Kernmodus.AlleGekoppelten, zweiter);
            Assert.True(ak3.simulation_wp.Sondenfelder.Count > 0, "1047 ohne Sondenfeld");
            Assert.Equal(zweiter, ak3.simulation_wp.ZweiterFeldlauf);

            Ak3Weg weg = ak3.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.Equal(zweiter ? 2 : 1, weg.Feldlauf);
            Assert.Equal(zweiter ? 1 : 0, weg.FruehereKreise.Count);
            foreach (Anlagenkopplung k in weg.FruehereKreise.Concat(new[] { weg.Kreis }))
            {
                Assert.Equal(8760, k.Stunden);
                Assert.Equal(8760, k.DurchlaeufeVerteilung[1]);
                Assert.Equal(0, k.StundenAnDerSchranke);
            }
            Assert.All(weg.Gebaeude, g => Assert.Equal(8760, g.Stepper.NaechsteStunde));
            Assert.All(weg.DeltaKw, d => Assert.Equal(0.0, d));

            // Quellzustand (W3a-Naht): Die Wärmepumpe des Kreises liest am Sondenfeld den Feldzustand, nie das Profil.
            WaermepumpeKapazitaet wp = weg.Kreis.Erzeuger.OfType<WaermepumpeKapazitaet>().FirstOrDefault();
            Assert.NotNull(wp);
            Sondenquelle quelle = Assert.IsType<Sondenquelle>(wp.Quelle);
            Assert.True(quelle.AbfragenAusFeld > 0, "Die Angebotsfunktion hat die Quelle nicht gelesen.");
            Assert.Equal(0, quelle.AbfragenAusProfil);

            Bitgleich(ak1.simulation_Waermebedarf.Waermebedarf, ak3.simulation_Waermebedarf.Waermebedarf, "Wärmebedarf");
            GebaeudeModellErgebnis g1 = ak1.simulation_Waermebedarf.GebaeudeErgebnisse.Alle.First(e => e != null);
            GebaeudeModellErgebnis g3 = ak3.simulation_Waermebedarf.GebaeudeErgebnisse.Alle.First(e => e != null);
            Bitgleich(g1.HeizlastW, g3.HeizlastW, "Heizlast");
            Bitgleich(g1.Raumtemperatur, g3.Raumtemperatur, "Raumtemperatur");
            for (int i = 0; i < ak1.simulation_wp.Quelltemperaturen.Count; i++)
                Bitgleich(ak1.simulation_wp.Quelltemperaturen[i], ak3.simulation_wp.Quelltemperaturen[i], "Quelle " + i);
            if (ak1.KesselInSchleife == ak3.KesselInSchleife && ak1.BhkwInSchleife == ak3.BhkwInSchleife)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(ak1.simulation_wp.WpStrombedarfGesamtKwh),
                             BitConverter.DoubleToInt64Bits(ak3.simulation_wp.WpStrombedarfGesamtKwh));
                Assert.Equal(BitConverter.DoubleToInt64Bits(ak1.RestwaermeMwh), BitConverter.DoubleToInt64Bits(ak3.RestwaermeMwh));
            }
        }

        /// <summary>
        /// <b>Quellzustand der Sonde ohne Datenbank</b>: In der Stunde, deren Beginn das Feld kennt, liefert die
        /// <see cref="Sondenquelle"/> Zeichen für Zeichen den Wert, den die Quellreihe des Laufs dort trägt (Vorbelegung in
        /// Stunde 0, danach die Rückgabe der Vorstunde) — und nicht die ungestörte Vorbelegung (Gegenprobe). Für eine
        /// Stunde außer der Reihe bleibt sie beim Jahresprofil und zählt das.
        /// </summary>
        [Fact]
        public void Sondenquelle_liest_den_Feldzustand_am_Stundenbeginn_sonst_das_Profil()
        {
            var feld = new Erdsondenfeld(100.0, 2, 2.0, 2.4, 10.0);
            double[] reihe = feld.Vorbelegung();
            double[] ungestoert = (double[])reihe.Clone();
            var quelle = new Sondenquelle(feld, reihe);
            bool abweichung = false;
            for (int h = 0; h < 72; h++)
            {
                Assert.True(quelle.FeldKennt(h));
                double t = quelle.TemperaturAmStundenbeginn(h);
                Assert.Equal(BitConverter.DoubleToInt64Bits(reihe[h]), BitConverter.DoubleToInt64Bits(t));
                if (t != ungestoert[h]) abweichung = true;
                double naechste = feld.StundeMelden(8.0);
                reihe[h + 1] = naechste;
            }
            Assert.True(abweichung, "Gegenprobe: Der Entzug senkt die Soletemperatur nicht.");
            Assert.Equal(72, quelle.AbfragenAusFeld);
            Assert.Equal(0, quelle.AbfragenAusProfil);

            Assert.False(quelle.FeldKennt(100));
            Assert.Equal(ungestoert[100], quelle.TemperaturAmStundenbeginn(100));
            Assert.Equal(1, quelle.AbfragenAusProfil);
        }

        /// <summary>
        /// <b>Messung</b> (nur mit <c>AK3_MESSUNG</c>): 1047, 1054, 1056, 1057 mit Stufe AK3 gegen ohne Kreis — Feldläufe,
        /// Durchläufe je Feldlauf, Laufzeit (bester von zwei Läufen), Heizwärme der Gebäude, WP-Strom.
        /// </summary>
        [Theory]
        [InlineData(1047)]
        [InlineData(1054)]
        [InlineData(1056)]
        [InlineData(1057)]
        public void Messung_mit_Kernstufe_AK3(int projekt)
        {
            string ziel = Environment.GetEnvironmentVariable("AK3_MESSUNG");
            if (!_db.Vorhanden || string.IsNullOrEmpty(ziel)) return;
            (SimulationControl sim, double s) Bester(Ak3Kernmodus modus)
            {
                SimulationControl letzte = null;
                double beste = double.MaxValue;
                for (int k = 0; k < 2; k++)
                {
                    var uhr = Stopwatch.StartNew();
                    letzte = Rechnen(projekt, modus, true);
                    beste = Math.Min(beste, uhr.Elapsed.TotalSeconds);
                }
                return (letzte, beste);
            }
            var ak1 = Bester(Ak3Kernmodus.Aus);
            var ak3 = Bester(Ak3Kernmodus.AlleGekoppelten);
            Ak3Weg weg = ak3.sim.simulation_Waermebedarf.Ak3;
            CultureInfo c = CultureInfo.InvariantCulture;
            string kreise = weg == null ? "kein gekoppeltes Gebäude" : string.Join("; ",
                weg.FruehereKreise.Concat(new[] { weg.Kreis }).Select((k, i) => string.Format(c,
                    "FL{0}: {1} h, Mittel {2:0.000}, max {3}, Schranke {4} h, Fallwechsel {5}",
                    i + 1, k.Stunden, k.DurchlaeufeMittel, k.DurchlaeufeMax, k.StundenAnDerSchranke, k.FallWechsel)));
            File.AppendAllText(ziel, string.Format(c,
                "{0}|Sonde {1}|Feldläufe {2}|{3}|AK1 {4:0.000} s|AK3 {5:0.000} s|Faktor {6:0.00}|Heizwärme {7:0.000}/{8:0.000} MWh|" +
                "WP-Strom {9:0.000}/{10:0.000} MWh|Abweichungsstunden {11}\n",
                projekt, ak3.sim.simulation_wp.Sondenfelder.Count, ak3.sim.simulation_wp.ZweiterFeldlauf ? 2 : 1, kreise,
                ak1.s, ak3.s, ak3.s / ak1.s,
                ak1.sim.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt, ak3.sim.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt,
                ak1.sim.simulation_wp.WpStrombedarfGesamtKwh / 1000.0, ak3.sim.simulation_wp.WpStrombedarfGesamtKwh / 1000.0,
                weg == null ? 0 : weg.DeltaKw.Count(d => d != 0.0)));
        }

        /// <summary>
        /// 1056 (Nachtsperre, Sondenfeld) mit Stufe AK3 und zweitem Feldlauf: Der Kreis weicht von Pass 1 ab, und der
        /// Wärmebedarf nach dem Lauf ist Pass 1 plus die Abweichung des LETZTEN Feldlaufs — einmal nachgeführt, nicht
        /// zweimal. Heizwärme der Gebäude mit und ohne zweiten Feldlauf liegen dicht beieinander (kein doppeltes Jahr).
        /// </summary>
        [Fact]
        public void Projekt_1056_fuehrt_die_Abweichung_des_letzten_Feldlaufs_einmal_nach()
        {
            if (!_db.Vorhanden) return;
            SimulationControl eins = Rechnen(1056, Ak3Kernmodus.AlleGekoppelten, false);
            SimulationControl zwei = Rechnen(1056, Ak3Kernmodus.AlleGekoppelten, true);
            Ak3Weg weg = zwei.simulation_Waermebedarf.Ak3;
            Assert.Equal(2, weg.Feldlauf);
            Assert.NotNull(weg.Pass1Stand);
            Assert.Contains(weg.DeltaKw, d => d != 0.0);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Assert.Equal(8760, weg.FruehereKreise[0].Stunden);

            double[] w = zwei.simulation_Waermebedarf.Waermebedarf, p1 = weg.Pass1Stand.Waermebedarf;
            for (int h = 0; h < 8760; h++)
            {
                double soll = weg.DeltaKw[h] == 0.0 ? p1[h] : p1[h] + weg.DeltaKw[h];
                Assert.True(BitConverter.DoubleToInt64Bits(soll) == BitConverter.DoubleToInt64Bits(w[h]), "Stunde " + h);
            }

            double qEins = eins.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt;
            double qZwei = zwei.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt;
            double qPass1 = weg.Pass1Stand.GebaeudeGesamt;
            Assert.True(Math.Abs(qZwei - qEins) <= 0.02 * qPass1, "Heizwärme " + qZwei + " gegen " + qEins + " MWh");
            Assert.Equal(qPass1 + weg.DeltaKw.Sum() / 1000.0, qZwei, 6);
        }
    }
}
