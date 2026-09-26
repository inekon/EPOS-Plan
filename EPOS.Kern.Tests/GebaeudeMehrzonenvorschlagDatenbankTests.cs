using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle A — der Schreibweg mehrerer Zonen und der Durchgang bis zur Rechnung</b> über
    /// eine Arbeitskopie der Testdatenbank: Der Vorschlag des Probenhauses nach Geschossen (Z4) wird samt
    /// Herkunft in EINEM Vorgang geschrieben — drei Zonen, Trenndecken mit endgültiger Nachbarzone, die
    /// Räume auf ihre Zone gepaart —, und die Mehrzonenrechnung aus G6b rechnet das Gebäude ohne Fehler.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeMehrzonenvorschlagDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private const string PROBE = "ifc4_haus.ifc";

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static ProjektGebaeudeModel Zeile(int idProjekt)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[0];
            item.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            return item;
        }

        private static long Zahl(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        [Fact]
        public void Drei_Zonen_werden_samt_Herkunft_in_einem_Vorgang_geschrieben_und_gerechnet()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(ablauf, 0, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            long zonenVorher = Zahl(ZonenSchema.TAB_ZONE), bauteileVorher = Zahl(ZonenSchema.TAB_BAUTEIL);
            long paarungenVorher = Zahl(ImportzuordnungSchema.TAB_ZUORDNUNG);

            GebaeudeZonenCtrl.Vorschlagsergebnis e;
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(g.ID_Gebaeude, ablauf.Quelle, e.Zuordnungen, vorgang);
                Assert.True(h.Ok, h.Meldung);
                vorgang.Commit();
            }
            Assert.Equal(zonenVorher + 3, Zahl(ZonenSchema.TAB_ZONE));
            Assert.Equal(bauteileVorher + v.Zeilen.Count, Zahl(ZonenSchema.TAB_BAUTEIL));
            Assert.Equal(paarungenVorher + e.Zuordnungen.Count, Zahl(ImportzuordnungSchema.TAB_ZUORDNUNG));

            // Der Vorschlag selbst bleibt, wie er war.
            Assert.Equal(new[] { -1, -2, -3 }, v.Zonen.Select(z => z.ID));

            // Zurücklesen: drei Zonen in Rangfolge, die Trenndecken zeigen auf die endgültigen Zonen.
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).ToList();
            Assert.Equal(e.Zonen.Select(z => z.ID), zonen.Select(z => z.ID));
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, zonen.Select(z => z.Bezeichner));
            Assert.Equal(new[] { false, true, true }, zonen.Select(z => z.IstBeheizt));
            for (int i = 0; i < 3; i++) Assert.Equal(v.Zeilen.Count(z => z.Zone == i), zonen[i].Bauteile.Count);
            BauteilModel kd = Assert.Single(zonen[1].Bauteile, b => b.Bezeichner == "Kellerdecke");
            Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, kd.Randbedingung);
            Assert.Equal(zonen[0].ID, kd.ID_Nachbarzone);
            BauteilModel gd = Assert.Single(zonen[1].Bauteile, b => b.Bezeichner == "Geschossdecke");
            Assert.Equal(zonen[2].ID, gd.ID_Nachbarzone);

            // Die Paarungen: die Räume auf ihre Zone, jede Zeile auf ihr Bauteil.
            List<GebaeudeQuellzuordnung> raeume = e.Zuordnungen.Where(z => z.Ziel == ImportZiel.Zone).ToList();
            Assert.Equal(5, raeume.Count);
            Assert.Equal(new[] { 1, 2, 2 }, zonen.Select(z => raeume.Count(r => r.ZielId == z.ID)));
            Assert.Equal(zonen.SelectMany(z => z.Bauteile).Where((b, i) => true).Select(b => (int?)b.ID).OrderBy(x => x),
                         e.Zuordnungen.Where(z => z.Ziel == ImportZiel.Bauteil).Select(z => z.ZielId).OrderBy(x => x));
            Assert.Null(GebaeudeImportCtrl.Pruefen(ablauf.Quelle, e.Zuordnungen));

            // Der Lauf: die Mehrzonenrechnung (G6b) rechnet das Gebäude ohne Fehler.
            ProjektGebaeudeModel mit = Zeile(PROJEKT);
            Assert.Equal(3, mit.Zonen.Count);
            Assert.All(mit.Zonen, z => Assert.Null(z.Lesefehler));
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            var werte = new double[8760];
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(mit, 0, werte), string.Join(" | ", p.Hinweise));
            Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
            Assert.True(sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh > 0.0);
            Assert.True(werte.Sum() > 0.0);
        }

        [Fact]
        public void Ein_Gebaeude_mit_Zone_nimmt_keinen_Mehrzonenvorschlag()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(PROBE);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(ablauf, 0, null);
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.VorschlagSchreiben(g.ID_Gebaeude, v).Ok);
            long zonen = Zahl(ZonenSchema.TAB_ZONE);
            GebaeudeZonenCtrl.Vorschlagsergebnis zweiter = ctrl.VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.False(zweiter.Ok);
            Assert.Equal(GebaeudeBauteilvorschlag.ZONE_VORHANDEN, zweiter.Befund.Schluessel);
            Assert.Equal(zonen, Zahl(ZonenSchema.TAB_ZONE));
        }
    }
}
