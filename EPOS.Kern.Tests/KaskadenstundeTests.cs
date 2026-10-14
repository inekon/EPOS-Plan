using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kaskadenstunde und Bedarfsnaht</b> (AK3-W2, Entwurf AK3 2.1 Schritt 4, Festlegungen 13 und 14):
    /// (a) die Naht <see cref="IStundenbedarf"/> rechnet bitgleich zur Vektorform und wird je Stunde genau
    /// einmal, in Stundenfolge, gerufen; (b) die Kennlinienwahl zählt eine Stunde nur einmal, auch wenn sie
    /// mehrfach abgefragt wird; (c) Vektorstufen vor der Speicherstufe werden im (nicht wählbaren) AK3-Weg
    /// Schleifenmitglieder.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaskadenstundeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Eine Naht, die den Jahresvektor liest wie die Vorgabe und ihre Aufrufe mitschreibt.</summary>
        private sealed class ZaehlendeNaht : IStundenbedarf
        {
            internal int Aufrufe;
            internal int LetzteStunde = -1;
            internal bool InFolge = true;

            public void BedarfDerStunde(int stunde, Kanalsatz kanaele, double[] rest)
            {
                if (stunde != LetzteStunde + 1) InFolge = false;
                LetzteStunde = stunde;
                Aufrufe++;
                foreach (int k in Kanal.KANAELE_WAERME) rest[k] = kanaele.Bedarf[k][stunde];
            }
        }

        private static SimulationRunner Lauf(int projekt, Action<SimulationControl> vorbereiten = null)
        {
            var runner = new SimulationRunner();
            vorbereiten?.Invoke(runner.sim);
            Assert.True(runner.Simuliere(projekt, out string fehler), "Lauf gescheitert: " + fehler);
            return runner;
        }

        // =============================================================================
        //  (a) Naht bitgleich zur Vektorform
        // =============================================================================

        [Fact]
        public void Vektornaht_schreibt_nur_die_Waermekanaele()
        {
            var kanaele = new Kanalsatz();
            foreach (int k in Kanal.KANAELE_WAERME) kanaele.Bedarf[k][17] = 10.0 + k;
            kanaele.Bedarf[Kanal.KUEHLUNG][17] = 99.0;

            double[] rest = new double[Kanal.ANZAHL];
            rest[Kanal.KUEHLUNG] = -1.0;
            VektorStundenbedarf.Instanz.BedarfDerStunde(17, kanaele, rest);

            foreach (int k in Kanal.KANAELE_WAERME) Assert.Equal(10.0 + k, rest[k]);
            Assert.Equal(-1.0, rest[Kanal.KUEHLUNG]);
        }

        /// <summary>
        /// Projekt 1047 (gekoppelt, Wärmepumpe mit Kennlinienwahl am Vorlauf): ein Lauf über eine eigene Naht
        /// trifft den Lauf mit der Vorgabe Bit für Bit — Restwärme, Wärme und Strom der Wärmepumpe, Reststrom
        /// und der Ausweis der Vorlaufwahl.
        /// </summary>
        [Fact]
        public void Naht_ist_bitgleich_zur_Vektorform_und_einmal_je_Stunde_gerufen()
        {
            if (!_db.Vorhanden) return;

            // 1047 rechnet seit R40 ein Erdsondenfeld; sein zweiter Feldlauf ruft die Kaskade ein zweites Jahr
            // (Konzept Simulationsablauf 23.4). Die Probe hält die Naht je Kaskadenjahr, deshalb ohne ihn.
            SimulationRunner vorgabe = Lauf(1047, sim => sim.ZweitenFeldlaufRechnen = false);
            var naht = new ZaehlendeNaht();
            SimulationRunner mitNaht = Lauf(1047, sim => { sim.ZweitenFeldlaufRechnen = false; sim.Stundenbedarf = naht; });

            Assert.Equal(8760, naht.Aufrufe);
            Assert.True(naht.InFolge);
            Assert.Equal(8759, naht.LetzteStunde);

            Assert.Equal(vorgabe.sim.Rest_Waermebedarf_stuendlich, mitNaht.sim.Rest_Waermebedarf_stuendlich);
            Assert.Equal(vorgabe.sim.simulation_wp.WP_Waermeproduktion_stuendlich,
                         mitNaht.sim.simulation_wp.WP_Waermeproduktion_stuendlich);
            Assert.Equal(vorgabe.sim.simulation_wp.WP_Strombedarf_stuendlich,
                         mitNaht.sim.simulation_wp.WP_Strombedarf_stuendlich);
            Assert.Equal(vorgabe.sim.ReststromMwh, mitNaht.sim.ReststromMwh);

            var ausweis = vorgabe.sim.simulation_wp.VorlaufwahlDesModuls(0);
            Assert.NotNull(ausweis);
            Assert.Equal(ausweis, mitNaht.sim.simulation_wp.VorlaufwahlDesModuls(0));
        }

        // =============================================================================
        //  (b) Zählung einmal je Stunde
        // =============================================================================

        [Fact]
        public void Mehrfach_abgefragte_Stunde_zaehlt_einmal_mit_der_letzten_Wahl()
        {
            var wahl = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(35, 45, 55);

            // Stunde 1: dreimal abgefragt (darunter, 45, darüber) - gezählt wird einmal, die letzte Wahl.
            wahl.Abfragen(30.0, true, out _);
            wahl.Abfragen(44.0, true, out _);
            Assert.Equal(SimulationWaermepumpe.Vorlauflage.Darueber, wahl.Abfragen(60.0, true, out int stelle));
            Assert.Equal(2, stelle);
            Assert.Equal(0, wahl.Stunden.Sum());

            wahl.Festschreiben();
            Assert.Equal(new[] { 0, 0, 1 }, wahl.Stunden);
            Assert.Equal(1, wahl.Darueber);
            Assert.Equal(0, wahl.Darunter);
            Assert.Equal(60.0, wahl.DarueberMax);

            // Ein zweites Festschreiben ohne neue Abfrage zählt nichts.
            wahl.Festschreiben();
            Assert.Equal(1, wahl.Stunden.Sum());

            // Stunde 2: eine verbotene Abfrage verwirft die Vormerkung - nichts zu zählen.
            wahl.Abfragen(40.0, true, out _);
            Assert.Equal(SimulationWaermepumpe.Vorlauflage.Verboten, wahl.Abfragen(60.0, false, out _));
            wahl.Festschreiben();
            Assert.Equal(1, wahl.Stunden.Sum());
        }

        [Fact]
        public void Zaehlen_ist_Abfragen_und_Festschreiben()
        {
            double[] reihe = { 30.0, 36.0, 40.0, 41.0, 47.0, 52.0, 55.0, 60.0 };
            var a = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(35, 45, 55);
            var b = SimulationWaermepumpe.Kennlinienwahl.FuerVorlaeufe(35, 45, 55);
            foreach (double v in reihe)
            {
                a.Zaehlen(v, true, out _);
                b.Abfragen(v, true, out _);
                b.Abfragen(v, true, out _);
                b.Festschreiben();
            }
            Assert.Equal(a.Stunden, b.Stunden);
            Assert.Equal(a.Darueber, b.Darueber);
            Assert.Equal(a.Darunter, b.Darunter);
            Assert.Equal(a.Ausweis(), b.Ausweis());
        }

        // =============================================================================
        //  (c) Vektorstufe als Schleifenmitglied (Festlegung 13)
        // =============================================================================

        private const string WP = DbWerte.ERZEUGER_WAERMEPUMPE;
        private const string KESSEL = DbWerte.ERZEUGER_HEIZKESSEL;
        private const string SOLAR = DbWerte.ERZEUGER_SOLARTHERMIE;
        private const string BHKW = DbWerte.ERZEUGER_BHKW;

        [Fact]
        public void Aufnahme_nimmt_nur_Vektorstufen_vor_dem_ersten_Mitglied()
        {
            // Kessel vor der Wärmepumpe: aufgenommen; die Solarthermie dahinter bleibt Vektorstufe.
            Assert.Equal(new[] { 0 }, SimulationControl.Ak3Aufnahmepositionen(
                new[] { KESSEL, WP, SOLAR, "" }, new[] { false, true, false, false }));
            // BHKW und Kessel vor einem Kessel-Mitglied an Platz 3.
            Assert.Equal(new[] { 0, 1 }, SimulationControl.Ak3Aufnahmepositionen(
                new[] { BHKW, SOLAR, KESSEL, "" }, new[] { false, false, true, false }));
            // Wärmepumpe vorn: nichts davor.
            Assert.Empty(SimulationControl.Ak3Aufnahmepositionen(
                new[] { WP, KESSEL, "", "" }, new[] { true, false, false, false }));
            // Ohne Speicherstufe: alle Heizerzeuger, leere Plätze nicht.
            Assert.Equal(new[] { 0, 1 }, SimulationControl.Ak3Aufnahmepositionen(
                new[] { BHKW, KESSEL, "", "" }, new[] { false, false, false, false }));
            Assert.Empty(SimulationControl.Ak3Aufnahmepositionen(null, null));
        }

        /// <summary>
        /// Projekt 1017 (BHKW und Kessel vor der Wärmepumpe, ohne Puffersenke): im heutigen Weg Vektorstufe, mit dem
        /// AK3-Schalter Schleifenmitglieder (beide). Ohne Speicher an diesen Stufen und in derselben Reihenfolge
        /// deckt die Stunde in der Schleife, was die Vektorstufe deckte: Kessel- und BHKW-Wärme und Restwärme
        /// stimmen auf 1e-9 des Bedarfs (gemessen: gleich). Eine Abweichung entsteht erst, wo ein Speicher der
        /// Stufe hinter ihnen anders lädt — die ist benannt (Festlegung 13) und gehört W3.
        /// </summary>
        [Fact]
        public void Vektorstufe_vor_der_Speicherstufe_wird_im_AK3_Weg_Schleifenmitglied()
        {
            if (!_db.Vorhanden) return;

            SimulationRunner heute = Lauf(1017);
            Assert.False(heute.sim.KesselInSchleife);
            Assert.False(heute.sim.BhkwInSchleife);

            SimulationRunner ak3 = Lauf(1017, sim => sim.Ak3VektorstufenInSchleife = true);
            Assert.True(ak3.sim.KesselInSchleife);
            Assert.True(ak3.sim.BhkwInSchleife);

            double bedarf = ak3.sim.simulation_Waermebedarf.Waermebedarf_Gesamt;
            Assert.True(bedarf > 0);
            double rand = 1e-9 * bedarf;

            double kesselAk3 = ak3.sim.simulation_spk.BruttoWaermeSpkErzeugungMwh;
            Assert.True(kesselAk3 > 0, "Der Kessel als Schleifenmitglied deckt nichts.");
            Assert.InRange(kesselAk3 - heute.sim.simulation_spk.BruttoWaermeSpkErzeugungMwh, -rand, rand);
            Assert.InRange(ak3.sim.simulation_bhkw.Waermeproduktion_BHKW_MWh -
                           heute.sim.simulation_bhkw.Waermeproduktion_BHKW_MWh, -rand, rand);
            Assert.InRange(ak3.sim.RestwaermeMwh - heute.sim.RestwaermeMwh, -rand, rand);
        }
    }
}
