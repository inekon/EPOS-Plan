using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache der gesäten Erdreichquellen der Referenzprojekte</b> (Konzept Simulationsablauf 23.7) und des
    /// Referenzprojekts 1057 „Referenz Erdsonde (Kopie 1029)". Die Zellen schreibt
    /// <c>Referenzlaeufe/Skripte/erdreichquellen_referenzprojekte.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Quelle</b> steht: an der Wärmepumpen-Anlage jedes Projekts der Tabelle Erdreich, Sonde,
    /// Anzahl und Tiefe, Bodentyp Mergel/Lehm, Fläche und Spreizung leer (Spreizung = Vorgabe 5 K); an der eigenen
    /// Klimaregion des Projekts Klimazone 6.</item>
    /// <item><b>1057 ist ein eigenständiges Projekt</b>: Kopie von 1029 mit denselben Anlagentypen, eigener
    /// Klimaregion, ohne Variantenzeile, mit Sonde 4 × 90 m; 1029 selbst bleibt, wie es war.</item>
    /// <item><b>Ein Lauf von 1057</b> rechnet die Sole aus dem Sondenfeld: Das Jahresmittel liegt unter 5 °C, und
    /// der Winter liegt unter dem Sommer — die Soletemperatur fällt mit dem Entzug.</item>
    /// </list>
    /// Die Zellen gehören zur Einfrierregel „gesäte Erdreichquellen der Referenzprojekte" (<c>CLAUDE.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErdsondeReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public ErdsondeReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1057, VORLAGE = 1029, TYP_WP = 1, KLIMAZONE = 6;
        private const string NAME = "Referenz Erdsonde (Kopie 1029)";

        /// <summary>Projekt, Wärmepumpen-Anlage, Anzahl Sonden, Tiefe je Sonde [m].</summary>
        public static readonly TheoryData<int, int, int, double> Quellen = new TheoryData<int, int, int, double>
        {
            { 1008, 10132, 6, 110.0 },
            { 1023, 11204, 3, 100.0 },
            { 1050, 16960, 3, 100.0 },
            { 1019, 14923, 3, 100.0 },
            { 1039, 14720, 16, 105.0 },
            { 1017, 10211, 5, 120.0 },
            { 1047, 14946, 5, 120.0 },
            { 1055, 23726, 5, 120.0 },
            { 1056, 23781, 5, 120.0 },
            { 1027, 11272, 8, 100.0 },
            { 1057, 23850, 4, 90.0 },
        };

        private static object Skalar(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static long Zahl(string sql, params object[] w) => Convert.ToInt64(Skalar(sql, w), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, params object[] w)
        {
            DataTable t = DataRepository.GetDataTable(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static string S(object o) => Convert.ToString(o, CultureInfo.InvariantCulture);

        private static long Region(int projekt) => Zahl("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", projekt);

        [Theory]
        [MemberData(nameof(Quellen))]
        public void Jede_gesaete_Erdreichquelle_steht_samt_Klimazone(int projekt, int anlage, int anzahl, double tiefe)
        {
            if (!_db.Vorhanden) return;
            DataRow a = Zeile("SELECT * FROM Tab_Energieanlagen WHERE ID = ?", anlage);
            Assert.Equal(projekt, Convert.ToInt32(a["ID_Projekt"], CultureInfo.InvariantCulture));
            Assert.Equal(TYP_WP, Convert.ToInt32(a["ID_Type"], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.WQ_TYP_ERDREICH, S(a["WQ_Typ"]));
            Assert.Equal(DbWerte.WQ_QUELLSYSTEM_SONDE, S(a["WQ_Quellsystem"]));
            Assert.Equal(anzahl, Convert.ToInt32(a["WQ_Anzahl"], CultureInfo.InvariantCulture));
            Assert.Equal(tiefe, Convert.ToDouble(a["WQ_Tiefe"], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.BODENTYP_MERGEL_LEHM, S(a["WQ_Bodentyp"]));
            Assert.True(a["WQ_Flaeche"] == DBNull.Value, "Anlage " + anlage + ": WQ_Flaeche ist nicht leer");
            Assert.True(a["WQ_Spreizung"] == DBNull.Value, "Anlage " + anlage + ": WQ_Spreizung ist nicht leer");

            long region = Region(projekt);
            DataRow r = Zeile("SELECT ID_Projekt, Klimazone_DIN4710 FROM Tab_Klimaregion WHERE ID = ?", region);
            Assert.Equal(projekt, Convert.ToInt32(r["ID_Projekt"], CultureInfo.InvariantCulture));
            Assert.Equal(KLIMAZONE, Convert.ToInt32(r["Klimazone_DIN4710"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID_Klimaregion = ?", region));
        }

        [Fact]
        public void Projekt_1057_ist_eine_eigenstaendige_Kopie_von_1029_mit_Sonde_und_1029_bleibt()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, S(Skalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", PROJEKT)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = ?", PROJEKT));
            Assert.NotEqual(Region(VORLAGE), Region(PROJEKT));

            string typen = "SELECT GROUP_CONCAT(ID_Type, ',') FROM (SELECT ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID_Type)";
            Assert.Equal(S(Skalar(typen, VORLAGE)), S(Skalar(typen, PROJEKT)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", PROJEKT, TYP_WP));
            foreach (string t in new[] { "Tab_WP", "Tab_Gebaeude", "Tab_Einstellungen", "Tab_Klimaregion", "Tab_Pufferspeicher" })
                Assert.Equal(Zahl("SELECT COUNT(*) FROM " + t + " WHERE ID_Projekt = ?", VORLAGE),
                             Zahl("SELECT COUNT(*) FROM " + t + " WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen a WHERE a.ID_Projekt = ? AND a.ID_WP IN " +
                                  "(SELECT ID FROM Tab_WP WHERE ID_Projekt = ?)", PROJEKT, VORLAGE));

            // Die Vorlage bleibt Zelle für Zelle, wie sie war: Variante von 1026, Sonde 4 × 90 m mit Spreizung 5 K.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = ?", VORLAGE));
            DataRow v = Zeile("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", VORLAGE, TYP_WP);
            Assert.Equal(DbWerte.WQ_QUELLSYSTEM_SONDE, S(v["WQ_Quellsystem"]));
            Assert.Equal(4, Convert.ToInt32(v["WQ_Anzahl"], CultureInfo.InvariantCulture));
            Assert.Equal(90.0, Convert.ToDouble(v["WQ_Tiefe"], CultureInfo.InvariantCulture));
            Assert.Equal(5.0, Convert.ToDouble(v["WQ_Spreizung"], CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Ein_Lauf_von_1057_rechnet_die_Sole_aus_dem_Sondenfeld_mit_fallender_Temperatur()
        {
            if (!_db.Vorhanden) return;
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            int anlage = Convert.ToInt32(Skalar("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", PROJEKT, TYP_WP),
                                         CultureInfo.InvariantCulture);
            SimulationWaermepumpe wp = lauf.sim.simulation_wp;
            int i = wp.wp_list.IndexOf(anlage);
            Assert.True(i >= 0 && i < wp.Quelltemperaturen.Count, "Anlage " + anlage + " fehlt im Lauf");
            double[] sole = wp.Quelltemperaturen[i];
            Assert.NotNull(sole);
            Assert.Equal(8760, sole.Length);

            double mittel = sole.Average();
            double januar = sole.Take(744).Average();
            double juli = sole.Skip(4344).Take(744).Average();
            _aus.WriteLine("1057: Sole Mittel {0:F2} °C, Januar {1:F2} °C, Juli {2:F2} °C, Minimum {3:F2} °C",
                           mittel, januar, juli, sole.Min());
            Assert.True(mittel < 5.0, "Sole Mittel " + mittel.ToString("F2", CultureInfo.InvariantCulture) + " °C");
            Assert.True(januar < juli, "die Sole fällt im Winter nicht unter den Sommer");
        }
    }
}
