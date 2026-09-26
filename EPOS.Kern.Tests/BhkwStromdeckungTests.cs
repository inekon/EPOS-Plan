using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E30/3 (#542, Befund N10 aus E29, Entscheid E30‑Q7 a) — <b>der Stromdeckungsgrad
    /// des BHKW ist sein Eigenverbrauch am Bedarf aller Verbraucher</b>
    /// (<see cref="SimulationErgebnisCtrl.BhkwStromdeckungProzent"/>).
    ///
    /// <para>Bis hierher zählten Lauf (<c>Tab_ErgebnisBHKW.Strombedarfsdeckung</c>, Word-Torte),
    /// BHKW-Reiter und Übersicht (Ring, Stromtabelle) die ganze Erzeugung samt Einspeisung am
    /// Projekt-Strombedarf. Jetzt: <c>(Erzeugung − KWK-Einspeisung) ÷ Σ Strombedarf der
    /// Verbraucher</c> — Projektlast, Wärmepumpe, Heizstab, Elektrokessel, Kälte —, geklemmt
    /// auf 0…100. Referenzbasis <c>2026-09-26_R21_BhkwDeckung</c>: es wandern 1017 (5,48 → 5,31),
    /// 1024 (26,22 → 20,94) und 1047 (5,34 → 5,30); 1030 bleibt gerundet 9,02, 1018 (ohne Bedarf) 0.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BhkwStromdeckungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public BhkwStromdeckungTests(TestDatenbank db) { _db = db; }

        private static SimulationRunner Lauf(int idProjekt)
        {
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);
            return r;
        }

        [Theory]
        [InlineData(1017, 5.31)]
        [InlineData(1018, 0.0)]
        [InlineData(1024, 20.94)]
        [InlineData(1030, 9.02)]
        [InlineData(1047, 5.30)]
        public void Die_Stromdeckung_ist_der_Eigenverbrauch_am_Gesamtbedarf(int projekt, double gerundet)
        {
            if (!_db.Vorhanden) return;

            SimulationRunner l = Lauf(projekt);
            SimulationControl sim = l.sim;

            double eigen = SimulationErgebnisCtrl.BhkwEigenverbrauchMwh(sim);
            double bedarf = SimulationErgebnisCtrl.StrombedarfVerbraucherMwh(sim);
            Assert.Equal(sim.simulation_bhkw.Stromproduktion_BHKW_MWh
                         - SimulationErgebnisCtrl.BhkwEinspeisungMwh(sim), eigen, 9);

            double deckung = SimulationErgebnisCtrl.BhkwStromdeckungProzent(sim);
            Assert.Equal(bedarf > 0 ? eigen * 100.0 / bedarf : 0.0, deckung, 9);
            Assert.Equal(gerundet, Math.Round(deckung, 2), 9);

            // Dieselbe Zahl in Lauf und BHKW-Reiter.
            ErgebnisModel m = SimulationRunner.BaueErgebnis(projekt, l.simulation_Waermebedarf,
                                                            l.simulation_Strombedarf, sim);
            Assert.Equal(deckung, m.BHKW.Strombedarfsdeckung, 9);
            SimulationErgebnisCtrl.BhkwErgebnis reiter =
                SimulationErgebnisCtrl.Bhkw(sim, l.simulation_Waermebedarf, l.simulation_Strombedarf);
            Assert.Equal(deckung, reiter.StromdeckungProzent, 9);

            // Der Nenner ist der Bedarf der Übersicht (E29/N6 samt Kältestrom), und
            // die gedeckte Summe bleibt darunter.
            SimulationErgebnisCtrl.UebersichtKennzahlen u =
                SimulationErgebnisCtrl.Uebersicht(sim, l.simulation_Waermebedarf, l.simulation_Strombedarf);
            Assert.Equal(u.StrombedarfMitEigenverbrauchMwh, bedarf, 6);
            Assert.Equal(eigen, u.BhkwStromEigenverbrauchMwh, 9);
            Assert.True(u.StromGesamtMwh <= u.StrombedarfMitEigenverbrauchMwh + 1e-9,
                        "Die Übersicht deckt mehr als den Bedarf.");
        }

        /// <summary>1018 speist seinen ganzen BHKW-Strom ein (kein Strombedarf): kein
        /// Eigenverbrauch, keine Deckung — nicht mehr über 100 %.</summary>
        [Fact]
        public void Reine_Einspeisung_deckt_nichts()
        {
            if (!_db.Vorhanden) return;

            SimulationControl sim = Lauf(1018).sim;
            Assert.True(sim.simulation_bhkw.Stromproduktion_BHKW_MWh > 27);
            Assert.Equal(0.0, SimulationErgebnisCtrl.BhkwEigenverbrauchMwh(sim), 6);
            Assert.Equal(0.0, SimulationErgebnisCtrl.BhkwStromdeckungProzent(sim), 9);
        }
    }
}
