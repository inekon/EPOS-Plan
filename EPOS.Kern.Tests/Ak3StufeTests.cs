using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Stufe AK3 wählbar</b> (AK3-W4a; Entwurf AK3, Festlegungen 3, 20, 22; E102 Q-AK3-1) an der Testdatenbank:
    /// Ein Projekt mit gespeicherter Stufe AK3 rechnet im Projektlauf den Kreis und schreibt die Kennzahlen; die
    /// Auskunft des Bedarfsdialogs rechnet auf dem Profilweg (Bit für Bit wie AK2) und nennt die Rückstufe; jede andere
    /// Stufe schreibt die Kennzahlen NULL. Dazu die Messung 1056 mit AK3 und k_R = 1 K/K (nur berichtet).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3StufeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public Ak3StufeTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private static void StufeSetzen(int projekt, string stufe)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Anlagenkopplung = ? WHERE ID_Projekt = ?",
                                                     new DbParam("@s", stufe), new DbParam("@p", projekt)));

        private static SimulationRunner Rechnen(int projekt, out double sekunden)
        {
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            var uhr = Stopwatch.StartNew();
            bool ok = r.SimuliereUndSpeichere(projekt, out string fehler) > 0;
            uhr.Stop();
            sekunden = uhr.Elapsed.TotalSeconds;
            Assert.True(ok, "Lauf " + projekt + " gescheitert: " + fehler);
            return r;
        }

        private static object Kennzahl(int projekt, string spalte)
            => DataRepository.ExecuteScalar("SELECT e." + spalte + " FROM Tab_ErgebnisEnergiebedarf e JOIN Tab_Ergebnis k ON k.ID = e.ID_Ergebnis " +
                                            "WHERE k.ID_Projekt = ? ORDER BY e.ID DESC LIMIT 1", new DbParam("?", projekt));

        [Fact]
        public void Gespeicherte_Stufe_AK3_rechnet_den_Kreis_und_schreibt_die_Kennzahlen()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(Ak3Kernmodus.GespeicherteStufe, Ak3Kernstufe.Modus);

            // AK1 gespeichert: kein Kreis, alle Kennzahlen NULL.
            SimulationRunner ak1 = Rechnen(1047, out _);
            Assert.Null(ak1.simulation_Waermebedarf.Ak3);
            foreach (string s in Ak3Schema.SPALTEN_ERGEBNIS)
                Assert.True(Kennzahl(1047, s) is null or DBNull, s + " ist bei AK1 nicht NULL");

            // AK3 gespeichert: der Kreis rechnet das ganze Jahr, die Kennzahlen stehen in der Projektzeile.
            StufeSetzen(1047, DbWerte.ANLAGENKOPPLUNG_AK3);
            SimulationRunner ak3 = Rechnen(1047, out _);
            Ak3Weg weg = ak3.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Assert.DoesNotContain(SimulationProtokoll.Aktuell.Hinweise, h => h.Contains(Ak3Kernstufe.Rueckstufetext, StringComparison.Ordinal));
            double mittel = Convert.ToDouble(Kennzahl(1047, Ak3Schema.SPALTE_DURCHLAEUFE_MITTEL), CultureInfo.InvariantCulture);
            Assert.Equal(weg.Kreis.DurchlaeufeMittel, mittel, 12);
            Assert.Equal((long)weg.Kreis.DurchlaeufeMax, Convert.ToInt64(Kennzahl(1047, Ak3Schema.SPALTE_DURCHLAEUFE_MAX), CultureInfo.InvariantCulture));
            Assert.Equal((long)(weg.Kreis.StuetzstellenWechsel + weg.Kreis.FallWechsel),
                         Convert.ToInt64(Kennzahl(1047, Ak3Schema.SPALTE_FALLWECHSEL), CultureInfo.InvariantCulture));
            Assert.Equal((long)weg.Kreis.StundenAnDerSchranke,
                         Convert.ToInt64(Kennzahl(1047, Ak3Schema.SPALTE_SCHRANKE_STUNDEN), CultureInfo.InvariantCulture));
            Assert.Equal((long)weg.Kreis.StundenSpeicherLeer,
                         Convert.ToInt64(Kennzahl(1047, Ak3Schema.SPALTE_SPEICHER_LEER_STUNDEN), CultureInfo.InvariantCulture));
            long rest = Convert.ToInt64(Kennzahl(1047, Ak3Schema.SPALTE_RESTBEDARF_STUNDEN), CultureInfo.InvariantCulture);
            Assert.Equal((long)ak3.sim.Rest_Waermebedarf_stuendlich.Count(x => x > SimulationRunner.AK3_RESTBEDARF_SCHWELLE_KWH), rest);
            _aus.WriteLine("1047 AK3: Durchläufe Mittel {0:0.000}, max {1}, Fallwechsel {2}, Schranke {3} h, Speicher leer {4} h, Restbedarf {5} h",
                           mittel, weg.Kreis.DurchlaeufeMax, weg.Kreis.StuetzstellenWechsel + weg.Kreis.FallWechsel,
                           weg.Kreis.StundenAnDerSchranke, weg.Kreis.StundenSpeicherLeer, rest);

            // Ohne Kreis bleibt das Modell NULL (Festlegung 22).
            var e = new ErgebnisEnergiebedarfModel();
            SimulationRunner.Ak3SpaltenSetzen(e, null, ak3.sim.Rest_Waermebedarf_stuendlich);
            Assert.Null(e.Ak3DurchlaeufeMittel);
            Assert.Null(e.Ak3RestbedarfStundenH);
        }

        /// <summary>
        /// <b>Auskunft-Rückstufe</b> (Festlegung 20): Mit Stufe AK3 rechnet der Bedarfsdialog auf dem Profilweg — Bit für
        /// Bit wie mit AK2 — und nennt die Rückstufe; mit AK1 nennt er keine.
        /// </summary>
        [Fact]
        public void Auskunft_rechnet_mit_AK3_auf_dem_Profilweg_und_nennt_die_Rueckstufe()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1047;
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];

            SimulationProtokoll.NeuStarten();
            GebaeudeBedarfErgebnis ak1 = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, g);
            Assert.Null(ak1.Rueckstufe);

            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK2);
            GebaeudeBedarfErgebnis ak2 = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, g);
            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK3);
            SimulationProtokoll.NeuStarten();
            GebaeudeBedarfErgebnis ak3 = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, g);
            Assert.Equal(Ak3Kernstufe.Rueckstufetext, ak3.Rueckstufe);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, h => h.Contains(Ak3Kernstufe.Rueckstufetext, StringComparison.Ordinal));
            Assert.Equal(ak2.Stundenwerte.Length, ak3.Stundenwerte.Length);
            for (int h = 0; h < ak2.Stundenwerte.Length; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(ak2.Stundenwerte[h]), BitConverter.DoubleToInt64Bits(ak3.Stundenwerte[h]));

            // Der Testschalter „aus" kennt keine Rückstufe.
            using (Ak3Kernstufe.Schalten(Ak3Kernmodus.Aus))
                Assert.Null(GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, g).Rueckstufe);
        }

        /// <summary>
        /// <b>Messung 1056 mit Stufe AK3 und k_R = 1 K/K</b> (nur berichtet): Durchläufe, Laufzeit-Faktor gegen den Lauf ohne
        /// Kreis, Heizwärme und Vorlauf im Mittel — ohne Kreis, AK3 ohne H2, AK3 mit H2.
        /// </summary>
        [Fact]
        public void Messung_1056_mit_AK3_und_Raumeinfluss()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1056;
            long heizkurve = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Heizkurve_Aktiv = 1", new DbParam("?", PROJEKT)), CultureInfo.InvariantCulture);

            SimulationRunner ohne = Rechnen(PROJEKT, out double tOhne);
            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK3);
            SimulationRunner ak3 = Rechnen(PROJEKT, out double tAk3);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Heizkurve_Raumeinfluss = 1 WHERE ID_Projekt = ?", new DbParam("?", PROJEKT)));
            SimulationRunner h2 = Rechnen(PROJEKT, out double tH2);

            void Zeile(string name, SimulationRunner r, double t)
            {
                Anlagenkopplung k = r.simulation_Waermebedarf.Ak3?.Kreis;
                HeizkreisProjekt hk = r.simulation_Waermebedarf.Heizkreis;
                _aus.WriteLine("1056 {0}: Laufzeit {1:0.00} s (Faktor {2:0.00}), Heizwärme Gebäude {3:0.000} MWh, Vorlauf Mittel {4:0.00} °C, " +
                               "Durchläufe Mittel {5}, max {6}, Schranke {7} h, angehoben {8} h (Mittel {9} K)",
                               name, t, t / tOhne, r.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt,
                               hk != null ? hk.VorlaufMittelC : double.NaN,
                               k != null ? k.DurchlaeufeMittel.ToString("0.000", CultureInfo.InvariantCulture) : "—",
                               k != null ? k.DurchlaeufeMax.ToString(CultureInfo.InvariantCulture) : "—",
                               k != null ? k.StundenAnDerSchranke.ToString(CultureInfo.InvariantCulture) : "—",
                               k?.Raumeinfluss != null ? k.Raumeinfluss.StundenAngehoben.ToString(CultureInfo.InvariantCulture) : "—",
                               k?.Raumeinfluss != null && k.Raumeinfluss.StundenAngehoben > 0
                                   ? (k.Raumeinfluss.AnhebungSummeKh / k.Raumeinfluss.StundenAngehoben).ToString("0.000", CultureInfo.InvariantCulture)
                                   : "—");
            }
            _aus.WriteLine("1056: Gebäude mit Heizkurve_Aktiv = 1: " + heizkurve);
            Zeile("ohne Kreis (AK1)", ohne, tOhne);
            Zeile("AK3 ohne H2", ak3, tAk3);
            Zeile("AK3 mit k_R = 1", h2, tH2);
            Assert.NotNull(h2.simulation_Waermebedarf.Ak3?.Kreis);
            if (heizkurve > 0) Assert.NotNull(h2.simulation_Waermebedarf.Ak3.Kreis.Raumeinfluss);
        }
    }
}
