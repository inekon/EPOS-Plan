using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Teillast, Takten und Randweg der Kältemaschine im Lauf</b> (KM3-E2-b; Fachkonzept Teillast und Takten 5.3, 8.1):
    /// Arbeitskopie von 1055. Ohne Weg schreibt der Runner die fünf Ergebnisspalten NULL und das Protokoll bleibt ohne
    /// neue Meldung; mit Weg sind sie gefüllt, der Mehrstrom steckt im Kältestrom, und die Meldungen zu verworfener Kurve
    /// und extrapolierten Stunden erscheinen einmal je Maschine.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineTeillastLaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1055;

        private static SimulationRunner Rechnen()
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);
            return lauf;
        }

        private static int Maschine() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_PROJEKT + " WHERE ID_Projekt = ? ORDER BY ID", new DbParam("?", PROJEKT)),
            CultureInfo.InvariantCulture);

        private static DataRow Ergebniszeile() => DataRepository.GetDataTable(
            "SELECT k.* FROM " + KaeltemaschineAnlageSchema.TAB_ERGEBNIS + " k JOIN Tab_Ergebnis e ON e.ID = k.ID_Ergebnis " +
            "WHERE e.ID_Projekt = ? ORDER BY k.ID DESC LIMIT 1", new DbParam("?", PROJEKT)).Rows[0];

        private static bool KmMeldung(SimulationRunner lauf, string vorlage)
        {
            string kopf = vorlage.Substring(0, vorlage.IndexOf("{0}", StringComparison.Ordinal));
            string schluss = vorlage.Substring(vorlage.LastIndexOf('}') + 1);
            return lauf.Protokoll.Hinweise.Any(t => t.StartsWith(kopf, StringComparison.Ordinal) && t.EndsWith(schluss, StringComparison.Ordinal));
        }

        [Fact]
        public void Ohne_Weg_bleiben_die_Spalten_NULL_und_das_Protokoll_ohne_neue_Meldung()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner lauf = Rechnen();
            Kaelteerzeuger e = lauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(x => x.Maschine != null);
            Assert.False(e.Maschine.MitWeg);
            DataRow r = Ergebniszeile();
            foreach (var s in KaeltemaschineTeillastSchema.ERGEBNIS_SPALTEN)
                Assert.True(r[s.Spalte] == DBNull.Value, s.Spalte);
            ErgebnisKaeltemaschineModel m = new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen.Single();
            Assert.Null(m.Taktstrom_MWh);
            Assert.Null(m.Starts);
            Assert.Null(m.Teillaststunden);
            Assert.Null(m.Lastgrad_Mittel);
            Assert.Null(m.Stunden_Extrapoliert);
            Assert.False(KmMeldung(lauf, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_KM_KURVE_VERWORFEN));
            Assert.False(KmMeldung(lauf, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_KM_EXTRAPOLIERT));
            Assert.DoesNotContain(KaelteErgebnisexport.Skalare(lauf.simulation_Kaeltebedarf), s => s.Key.EndsWith("TaktstromMwh", StringComparison.Ordinal));
        }

        [Fact]
        public void Mit_Weg_sind_die_Spalten_gefuellt_und_der_Mehrstrom_steckt_im_Kaeltestrom()
        {
            if (!_db.Vorhanden) return;
            int km = Maschine();
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_PROJEKT + " SET Teillast_Weg = ?, Teillastkurve_a = ?, " +
                "Teillastkurve_b = ?, Teillastkurve_c = ?, Teillastkurve_Lastgrad_Min = ?, Mindestteillast_Prozent = ?, Kennfeld_Randweg = ? WHERE ID = ?",
                new DbParam("?", "KURVE"), new DbParam("?", 0.10), new DbParam("?", 0.60), new DbParam("?", 0.30), new DbParam("?", 0.2),
                new DbParam("?", 30.0), new DbParam("?", "GUETEGRAD"), new DbParam("?", km));
            SimulationRunner lauf = Rechnen();
            Kaelteerzeuger e = lauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(x => x.Maschine != null);
            Assert.True(e.Maschine.TeillastWirksam);
            Assert.True(e.Maschine.GuetegradWirksam);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Kurve, e.Maschine.Teillast.Herkunft);
            Assert.True(e.StundenMitKaelte > 0);
            Assert.True(e.Starts > 0);
            Assert.Equal(e.TaktstromKwh, e.Taktstrom_stuendlich.Sum(), 6);

            ErgebnisKaeltemaschineModel m = new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen.Single();
            Assert.NotNull(m.Taktstrom_MWh);
            Assert.InRange(m.Taktstrom_MWh.Value, e.TaktstromKwh / 1000.0 - 0.006, e.TaktstromKwh / 1000.0 + 0.006);
            Assert.True(m.Stromverbrauch_MWh >= m.Taktstrom_MWh.Value);
            Assert.Equal(e.Starts, m.Starts);
            // Ein Begriff, ein Zähler: die Ergebnisspalte liest denselben Zähler wie der alte Taktweg.
            Assert.True(e.Taktstunden > 0);
            Assert.Equal(e.Taktstunden, m.Taktstunden);
            Assert.Equal(e.StundenTeillast, m.Teillaststunden);
            Assert.NotNull(m.Lastgrad_Mittel);
            Assert.InRange(m.Lastgrad_Mittel.Value, 0.0, 1.0);
            Assert.Equal(e.StundenExtrapoliert, m.Stunden_Extrapoliert);
            // Der Kältestrom des Laufs ist die Summe der Stunden - der Mehrstrom ist darin (Abrechnung liest ihn mit).
            Assert.Equal(e.StromGesamtKwh, e.Strom_stuendlich.Sum(), 6);
            Assert.Equal(e.StundenExtrapoliert > 0, KmMeldung(lauf, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_KM_EXTRAPOLIERT));
            Assert.False(KmMeldung(lauf, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_KM_KURVE_VERWORFEN));
            Assert.Contains(KaelteErgebnisexport.Skalare(lauf.simulation_Kaeltebedarf), s => s.Key.EndsWith("TaktstromMwh", StringComparison.Ordinal));
        }

        [Fact]
        public void Eine_verworfene_Kurve_rechnet_linear_und_wird_gemeldet()
        {
            if (!_db.Vorhanden) return;
            int km = Maschine();
            // Nur zwei Beiwerte gepflegt: die Kurve ist unvollständig.
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_PROJEKT + " SET Teillast_Weg = ?, Teillastkurve_a = ?, " +
                "Teillastkurve_b = ? WHERE ID = ?",
                new DbParam("?", "KURVE"), new DbParam("?", 0.10), new DbParam("?", 0.60), new DbParam("?", km));
            SimulationRunner lauf = Rechnen();
            Kaelteerzeuger e = lauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(x => x.Maschine != null);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Verworfen, e.Maschine.Teillast.Herkunft);
            Assert.True(e.Maschine.TeillastWirksam);
            Assert.Equal(1, lauf.Protokoll.Hinweise.Count(t => t.Contains(e.Maschine.Bezeichner, StringComparison.Ordinal) &&
                t == string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_KM_KURVE_VERWORFEN, e.Maschine.Bezeichner)));
            Assert.NotNull(new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen.Single().Starts);
            Assert.Null(new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen.Single().Stunden_Extrapoliert);
        }
    }
}
