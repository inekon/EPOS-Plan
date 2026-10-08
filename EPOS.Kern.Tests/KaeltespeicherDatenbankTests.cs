using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kältespeicher im Lauf</b> (KU3-5, E68) an einer Kopie der Testdatenbank: Projekt 1017 rechnet
    /// Kälte mit der Wärmepumpe im Kühlbetrieb; dazu eine knapp bemessene Kältemaschine (Muster
    /// <see cref="KaeltemaschineAnlageDatenbankTests"/>) und ein Kaltwasserspeicher. Der Speicher entlädt,
    /// die Unterdeckung sinkt, und die Ergebniszeile trägt Verwendung und Kühlkanalentladung.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltespeicherDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        private static int Stamm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", bezeichner)),
            CultureInfo.InvariantCulture);

        private static SimulationRunner Rechnen(int projekt = PROJEKT)
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(projekt, out string fehler);
            Assert.True(kopf > 0, fehler);
            return lauf;
        }

        private static int KaeltespeicherAnlegen(int projekt, int volumen) =>
            PufferSpCtrl.ProjektPufferAnlegen(projekt, "Kaltwasserspeicher Test", "", "", volumen, 2.0, 0,
                                              DbWerte.PSP_VERWENDUNG_KAELTE, 6, 12, 10, 95, null, 0);

        [Fact]
        public void Der_Kaeltespeicher_entlaedt_senkt_die_Unterdeckung_und_wird_gespeichert()
        {
            if (!_db.Vorhanden) return;
            // Fallbildung: Mit der Zonensperre deckt die Wärmepumpe von 1017 die Kälte ganz (Basis R43); die
            // Unterdeckung kommt aus ihrer geminderten Leistungsgrenze.
            KaelteUnterdeckung.WaermepumpeMindern(PROJEKT);
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, Stamm(LUFTGEKUEHLT), "KM Halle");
            KaeltemaschineAnlageModel a = KaeltemaschineAnlageCtrl.Laden(anlage);
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_KENNDATEN + " SET " +
                KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " = " + KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " / 20 WHERE " +
                KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", a.IdKaeltemaschine.Value));
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_PROJEKT + " SET " +
                KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG + " = 2.5 WHERE ID = ?", new DbParam("?", a.IdKaeltemaschine.Value));

            Kaeltekaskade ohne = Rechnen().simulation_Kaeltebedarf.Kaskade;
            Assert.True(ohne.RestGesamtKwh > 0, "Der Fall braucht eine Unterdeckung.");
            Assert.Empty(ohne.Speicher);

            int id = KaeltespeicherAnlegen(PROJEKT, 20000);
            Assert.True(id > 0);
            Assert.True(PufferSpCtrl.KlassenSetLesen(id).Kaelte);
            Assert.Empty(Warnkriterien.PruefeProjekt(PROJEKT)
                .Where(b => b.Kriterium.StartsWith("KAELTESPEICHER", StringComparison.Ordinal)));

            SimulationRunner lauf = Rechnen();
            Kaeltekaskade mit = lauf.simulation_Kaeltebedarf.Kaskade;
            SimulationPufferspeicher sp = Assert.Single(mit.Speicher);
            Assert.Equal(id, sp.ID_Pufferspeicher);
            Assert.Equal(20000 * 1.16 * 6 / 1000.0, sp.Q_max, 9);
            Assert.True(sp.Entladung_gesamt > 0);
            Assert.True(sp.Ladung_gesamt >= sp.Entladung_gesamt);
            Assert.True(sp.Vollzyklen > 0);
            Assert.True(mit.RestGesamtKwh < ohne.RestGesamtKwh,
                $"Unterdeckung mit Speicher {mit.RestGesamtKwh} gegen ohne {ohne.RestGesamtKwh}");
            Assert.True(Kaeltekaskade.Deckungsprobe(mit, null, mit.Bedarf_stuendlich, null, 0).Ok);
            // Der Kältespeicher steht nicht in der Wärmeordnung.
            Assert.DoesNotContain(lauf.sim.AlleSpeicher(), s => s.ID_Pufferspeicher == id);
            Assert.Contains(lauf.sim.Kaeltespeicher(), s => s.ID_Pufferspeicher == id);
            Assert.Contains(lauf.Protokoll.Hinweise, t => t.Contains("Kaltwasserspeicher Test", StringComparison.Ordinal));
            Assert.Contains(SimulationKaeltebedarfSkalare(lauf), k => k == "Kaeltespeicher[0].EntladungMwh");

            ErgebnisPufferspeicherModel z = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Pufferspeicher,
                                                          p => p.ID_Pufferspeicher == id);
            Assert.Equal(DbWerte.PSP_VERWENDUNG_KAELTE, z.Verwendung);
            Assert.InRange(z.Entladung_Kanal[Kanal.KUEHLUNG], sp.Entladung_gesamt - 0.01, sp.Entladung_gesamt + 0.01);
            Assert.Equal(0.0, z.Entladung_Kanal[Kanal.HEIZUNG]);
            Assert.Null(z.T_oben_Mittel);
        }

        private static string[] SimulationKaeltebedarfSkalare(SimulationRunner lauf) =>
            KaelteErgebnisexport.Skalare(lauf.simulation_Kaeltebedarf).Select(k => k.Key).ToArray();

        [Fact]
        public void Warnkriterien_ohne_Kuehlung_und_ohne_Kaelteerzeuger()
        {
            if (!_db.Vorhanden) return;
            int id = KaeltespeicherAnlegen(PROJEKT, 5000);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, false));
            Assert.Contains(Warnkriterien.PruefeProjekt(PROJEKT),
                b => b.Kriterium == Warnkriterien.KAELTESPEICHER_OHNE_KUEHLUNG && b.ID_Puffer == id && !b.Hart);

            // Ohne Kühlung rechnet er nicht - benannt im Laufprotokoll.
            SimulationRunner lauf = Rechnen();
            Assert.Empty(lauf.sim.Kaeltespeicher());
            Assert.Contains(lauf.Protokoll.Warnungen, t => t.Contains("Kaltwasserspeicher Test", StringComparison.Ordinal));

            // Projekt 1030 hat keinen Kälteerzeuger.
            int id2 = KaeltespeicherAnlegen(1030, 5000);
            Assert.Contains(Warnkriterien.PruefeProjekt(1030),
                b => b.Kriterium == Warnkriterien.KAELTESPEICHER_OHNE_ERZEUGER && b.ID_Puffer == id2);
        }
    }
}
