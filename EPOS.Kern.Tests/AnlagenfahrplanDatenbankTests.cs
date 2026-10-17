using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Fahrplan im Lauf</b> (Anlagenkopplung AK2-2a; 6.2, 11.1, 11.3) — auf einer Arbeitskopie der
    /// Testdatenbank: die gekoppelten Referenzprojekte 1047 (ein Gebäude) und 1054 (Zonenschleife, Pass 1 für die
    /// zweite Stufe) rechnen ohne Sperrung und Zeitprogramm byte-gleich wie ohne Fahrplan; mit einer Sperrung der
    /// Wärmepumpe 14–17 Uhr (nur in der Arbeitskopie) kappt der Fahrplan, und die Raumluft sinkt in den Sperrstunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenfahrplanDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public AnlagenfahrplanDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int WP_1047 = 14946;
        private const int KESSEL_1047 = 14994;
        private const int BHKW_1047 = 14995;

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static SimulationWaermebedarf Bedarf(int idProjekt, bool ohneFahrplan)
        {
            SimulationProtokoll.NeuStarten();
            var sim = new SimulationWaermebedarf { FahrplanUnterdrueckt = ohneFahrplan };
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            Assert.True(string.IsNullOrEmpty(sim.Fehlertext), sim.Fehlertext);
            return sim;
        }

        private static void ByteGleich(SimulationWaermebedarf a, SimulationWaermebedarf b)
        {
            for (int h = 0; h < 8760; h++)
                Assert.True(a.Waermebedarf[h].Equals(b.Waermebedarf[h]), "Wärmebedarf, Stunde " + h);
            GebaeudeModellErgebnis ea = a.GebaeudeErgebnisse.Alle.First(), eb = b.GebaeudeErgebnisse.Alle.First();
            for (int h = 0; h < 8760; h++)
                Assert.True(ea.Raumtemperatur[h].Equals(eb.Raumtemperatur[h]), "Raumtemperatur, Stunde " + h);
        }

        [Theory]
        [InlineData(1047, false)]
        [InlineData(1054, true)]
        public void Ohne_Sperrung_und_Zeitprogramm_rechnet_der_Zweipass_byte_gleich(int projekt, bool pass1)
        {
            if (!_db.Vorhanden) return;
            SimulationWaermebedarf mit = Bedarf(projekt, false);
            Assert.NotNull(mit.Fahrplan);
            Assert.Equal(pass1, mit.FahrplanMitPass1);
            Assert.Equal(0, mit.FahrplanBegrenztStunden());
            SimulationWaermebedarf ohne = Bedarf(projekt, true);
            Assert.Null(ohne.Fahrplan);
            ByteGleich(mit, ohne);
        }

        [Fact]
        public void Eine_Sperrung_14_bis_17_Uhr_kappt_und_senkt_die_Raumtemperatur()
        {
            if (!_db.Vorhanden) return;
            SimulationWaermebedarf vorher = Bedarf(1047, false);

            // Nur in der Arbeitskopie: Sperrung der Wärmepumpe, Kessel und BHKW ohne Freigabe - ohne Puffer reicht dann
            // in den Sperrstunden nichts.
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Sperrung = 1, Sperrzeit_von = 14, Sperrzeit_bis = 17 WHERE ID = ?",
                new DbParam("@id", WP_1047)));
            string null168 = string.Join(";", Enumerable.Repeat("0", 168));
            foreach (int id in new[] { KESSEL_1047, BHKW_1047 })
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Zeitprogramm = ? WHERE ID = ?",
                    new DbParam("@zp", null168), new DbParam("@id", id)));

            SimulationWaermebedarf nachher = Bedarf(1047, false);
            int begrenzt = nachher.FahrplanBegrenztStunden();
            _aus.WriteLine("Fahrplan_Begrenzt_Stunden = " + begrenzt);
            Assert.True(begrenzt > 0);
            GebaeudeModellErgebnis e = nachher.GebaeudeErgebnisse.Alle.First();
            Assert.Equal(begrenzt, e.FahrplanBegrenztStunden);
            Assert.Equal(0.0, nachher.Fahrplan.Stunde(24 * 10 + 15).LeistungKw);
            Assert.NotEqual(Verfuegbarkeitsgrund.KeineBegrenzung, nachher.Fahrplan.Stunde(24 * 10 + 15).Grund);

            GebaeudeModellErgebnis a = vorher.GebaeudeErgebnisse.Alle.First();
            double summeVorher = 0.0, summeNachher = 0.0;
            int stunden = 0;
            for (int tag = 0; tag < 31; tag++)
                for (int std = 14; std < 17; std++)
                {
                    int h = tag * 24 + std;
                    summeVorher += a.Raumtemperatur[h];
                    summeNachher += e.Raumtemperatur[h];
                    stunden++;
                }
            _aus.WriteLine("Januar 14-17 Uhr: " + summeVorher / stunden + " -> " + summeNachher / stunden + " °C");
            Assert.True(summeNachher < summeVorher, "Raumtemperatur in Sperrstunden niedriger");

            Assert.Equal(begrenzt, nachher.GebaeudeKennzahlenListe.First().FahrplanBegrenztStundenH);
        }
    }
}
