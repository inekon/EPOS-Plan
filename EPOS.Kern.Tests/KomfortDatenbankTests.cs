using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Komfortkennzahlen und feste Last im Lauf</b> (Anlagenkopplung AK2-2b; 5.5, 6.2, 11.1, 11.3) — auf einer
    /// Arbeitskopie der Testdatenbank: 1047 ohne Sperrung schreibt die Spalten des Schritts 186 mit 0 Fahrplanstunden
    /// und gefüllten Komfortspalten (F12, E83), mit
    /// Sperrung der Wärmepumpe und Zeitprogramm 0 an Kessel und BHKW stehen Komfortstunden und Restbedarf
    /// nebeneinander; die Bedarfsauskunft rechnet denselben Fahrplan. 1008 (zwei Gebäude) mit einem Gebäude auf dem
    /// Altweg: Es zehrt als feste Last, ohne Komfortstunden und ohne gerechneten Vorlauf; auf dem VDI-Weg gekoppelt
    /// bekommt es beides.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KomfortDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KomfortDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int WP_1047 = 14946;
        private const int KESSEL_1047 = 14994;
        private const int BHKW_1047 = 14995;
        private const int GEBAEUDE_1008_A = 10576;
        private const int GEBAEUDE_1008_B = 10577;

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static SimulationWaermebedarf Bedarf(int idProjekt)
        {
            SimulationProtokoll.NeuStarten();
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            Assert.True(string.IsNullOrEmpty(sim.Fehlertext), sim.Fehlertext);
            return sim;
        }

        private static void Sperren1047()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Sperrung = 1, Sperrzeit_von = 14, Sperrzeit_bis = 17 WHERE ID = ?",
                new DbParam("@id", WP_1047)));
            string null168 = string.Join(";", Enumerable.Repeat("0", 168));
            foreach (int id in new[] { KESSEL_1047, BHKW_1047 })
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Zeitprogramm = ? WHERE ID = ?",
                    new DbParam("@zp", null168), new DbParam("@id", id)));
        }

        private static ErgebnisEnergiebedarfModel LaufUndLesen(int projekt)
        {
            SimulationProtokoll.NeuStarten();
            int kopf = new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            return new ErgebnisCtrl().Load(projekt).Energiebedarf;
        }

        [Fact]
        public void Ohne_greifende_Schranke_stehen_die_Spalten_am_gekoppelten_Referenzprojekt()
        {
            if (!_db.Vorhanden) return;
            ErgebnisEnergiebedarfModel e = LaufUndLesen(1047);
            Assert.Equal(0, e.FahrplanBegrenztStundenH);
            Assert.NotNull(e.KomfortUnterschreitungsstundenH);
            Assert.NotNull(e.KomfortKelvinstundenKh);
            Assert.NotNull(e.KomfortLaengsteStreckeH);
            Assert.NotNull(e.KomfortUeberschreitungsstundenH);
            Assert.NotNull(e.KomfortKelvinstundenKuehlungKh);
            Assert.NotNull(e.KomfortUndRestbedarf);

            // Ein Projekt ohne Kopplung schreibt die Spalten nicht.
            ErgebnisEnergiebedarfModel o = LaufUndLesen(1017);
            Assert.Null(o.FahrplanBegrenztStundenH);
            Assert.Null(o.KomfortUnterschreitungsstundenH);
            Assert.Null(o.KomfortUndRestbedarf);
        }

        [Fact]
        public void Sperrung_und_Zeitprogramm_0_ergeben_Komfortstunden_mit_dem_Restbedarf_daneben()
        {
            if (!_db.Vorhanden) return;
            Sperren1047();

            SimulationWaermebedarf sim = Bedarf(1047);
            Assert.True(sim.FahrplanBegrenztStunden() > 0);
            ErgebnisGebaeudeModel zeile = sim.GebaeudeKennzahlenListe.Single();
            Assert.Equal(Bedarfsbegriff.Rueckwirkung, zeile.Bedarfsbegriff);
            Assert.True(zeile.KomfortUnterschreitungsstundenH > 0);
            Assert.True(zeile.KomfortLaengsteStreckeH <= zeile.KomfortUnterschreitungsstundenH);
            Assert.True(zeile.KomfortKelvinstundenKh >= zeile.KomfortUnterschreitungsstundenH * GebaeudeFestwerte.KOMFORT_SCHWELLE_K);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK2_PROFILWEG_NAEHERUNG, SimulationProtokoll.Aktuell.Hinweise);
            Assert.Equal(0, sim.FahrplanFesteLastGebaeude);

            // Die Bedarfsauskunft rechnet denselben Fahrplan: dieselbe Jahressumme wie der Lauf.
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1047);
            GebaeudeBedarfErgebnis vorschau = GebaeudeBedarfCtrl.Rechnen(1047, Klimaregion(1047), ctrl.items[0]);
            Assert.True(vorschau.Erfolgreich, vorschau.Befund);
            Assert.Equal(zeile.HeizwaermeMwh, vorschau.HeizwaermeMwh);
            Assert.Equal(zeile.KomfortUnterschreitungsstundenH, vorschau.Ergebniszeile.KomfortUnterschreitungsstundenH);

            ErgebnisEnergiebedarfModel e = LaufUndLesen(1047);
            _aus.WriteLine($"Fahrplan {e.FahrplanBegrenztStundenH} h, Komfort {e.KomfortUnterschreitungsstundenH} h / "
                           + $"{e.KomfortKelvinstundenKh:0.0} Kh / {e.KomfortLaengsteStreckeH} h, Rest {e.Waermerestbedarf:0.000} MWh");
            Assert.True(e.FahrplanBegrenztStundenH > 0);
            Assert.Equal(zeile.KomfortUnterschreitungsstundenH, e.KomfortUnterschreitungsstundenH);
            Assert.NotNull(e.KomfortKelvinstundenKh);
            Assert.NotNull(e.KomfortLaengsteStreckeH);
            var neben = e.KomfortUndRestbedarf;
            Assert.NotNull(neben);
            Assert.Equal(e.Waermerestbedarf, neben.Value.WaermerestbedarfMwh);
        }

        private static void Projekt1008Koppeln(string modellB, bool bGekoppelt)
        {
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Anlagenkopplung = ? WHERE ID_Projekt = ?",
                new DbParam("@s", DbWerte.ANLAGENKOPPLUNG_AK1), new DbParam("@p", 1008)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Heizkurve_Aktiv = 1 WHERE ID = ?",
                new DbParam("@a", DbWerte.UEBERGABE_RADIATOR), new DbParam("@id", GEBAEUDE_1008_A)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Gebaeude_Modell = ?, Heizkreis_Aktiv = ?, Uebergabe_Art = ?, Heizkurve_Aktiv = ? WHERE ID = ?",
                new DbParam("@m", modellB), new DbParam("@k", bGekoppelt ? 1 : 0),
                new DbParam("@u", bGekoppelt ? DbWerte.UEBERGABE_RADIATOR : (object)DBNull.Value),
                new DbParam("@h", bGekoppelt ? 1 : 0), new DbParam("@id", GEBAEUDE_1008_B)));
        }

        private static ErgebnisGebaeudeModel Zeile(SimulationWaermebedarf sim, int idGebaeude)
            => sim.GebaeudeKennzahlenListe.Single(z => z.ID_Gebaeude == idGebaeude);

        [Fact]
        public void Altweg_Gebaeude_geht_als_feste_Last_ein_und_bekommt_auf_dem_VDI_Weg_Komfort_und_Vorlauf()
        {
            if (!_db.Vorhanden) return;
            Projekt1008Koppeln(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, false);
            SimulationWaermebedarf alt = Bedarf(1008);
            Assert.True(alt.FahrplanWirksam);
            Assert.Equal(1, alt.FahrplanFesteLastGebaeude);
            Assert.True(alt.FahrplanMitPass1);
            ErgebnisGebaeudeModel a = Zeile(alt, GEBAEUDE_1008_A), b = Zeile(alt, GEBAEUDE_1008_B);
            Assert.Equal(Bedarfsbegriff.Rueckwirkung, a.Bedarfsbegriff);
            Assert.NotNull(a.KomfortUnterschreitungsstundenH);
            Assert.NotNull(a.VorlaufMittelC);
            Assert.Equal(Bedarfsbegriff.FesteLast, b.Bedarfsbegriff);
            Assert.True(b.HeizwaermeMwh > 0.0, "das Altweg-Gebäude zehrt");
            Assert.Null(b.KomfortUnterschreitungsstundenH);
            Assert.Null(b.KomfortLaengsteStreckeH);
            Assert.Null(b.VorlaufMittelC);
            string hinweis = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                           WindowsFormsApplication1.MyResource.Resource.SIMENG_AK2_FESTE_LAST, 1);
            Assert.Contains(hinweis, SimulationProtokoll.Aktuell.Hinweise);

            // Gegenprobe: dasselbe Gebäude auf dem VDI-Weg, gekoppelt.
            Projekt1008Koppeln(DbWerte.GEBAEUDE_MODELL_VDI6007, true);
            SimulationWaermebedarf vdi = Bedarf(1008);
            Assert.Equal(0, vdi.FahrplanFesteLastGebaeude);
            ErgebnisGebaeudeModel b2 = Zeile(vdi, GEBAEUDE_1008_B);
            Assert.Equal(Bedarfsbegriff.Rueckwirkung, b2.Bedarfsbegriff);
            Assert.NotNull(b2.KomfortUnterschreitungsstundenH);
            Assert.NotNull(b2.VorlaufMittelC);
            Assert.DoesNotContain(SimulationProtokoll.Aktuell.Hinweise, h => h.Contains(hinweis));
        }
    }
}
