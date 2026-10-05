using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1056 „Referenz Kopplung mit Fahrplan"</b> (Anlagenkopplung AK2-4; F11, F12,
    /// E83) — die Kopie des gekoppelten Referenzprojekts 1047, in der der Anlagenfahrplan greift. Die gesäten Zellen
    /// schreibt <c>Referenzlaeufe/Skripte/referenzprojekt_1056_fahrplan.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht: Kopplungsstufe AK1, Heizkreis mit Radiator, Sperrung der Wärmepumpe
    /// 0 bis 6 Uhr samt <c>Vorlauf_Max</c> 50 °C, das Zeitprogramm (0 von 0 bis 6 Uhr, jeden Tag) an Kessel und
    /// BHKW, kein Pufferspeicher, keine Sperrfenster.</item>
    /// <item><b>1056 ist eine Kopie von 1047</b>: dieselbe Kaskade, dieselben Anlagentypen; 1047 selbst führt
    /// weder Sperrung noch Zeitprogramm noch Vorlaufgrenze.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> kappt den Fahrplan in mehr als 0 Stunden, schreibt Komfortstunden,
    /// Kelvinstunden und längste Strecke, den Restbedarf daneben, und meldet die Näherung des Profilwegs.</item>
    /// </list>
    /// 1056 gehört zur Einfrierregel „gesäte Auslegungsdaten der Übergabe" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class FahrplanReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1056;
        private const int VORLAGE = 1047;
        private const string NAME = "Referenz Kopplung mit Fahrplan";
        private static readonly string ZEITPROGRAMM = string.Join(";", Enumerable.Range(0, 168).Select(h => h % 24 < 6 ? "0" : "1"));

        private static long Zahl(string sql, params object[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray()),
                               CultureInfo.InvariantCulture);

        private static DataRow Anlage(int projekt, int typ)
        {
            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                                                      new DbParam("@p", projekt), new DbParam("@t", typ));
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_geplant()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.Equal("AK1", KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Heizkreis_Aktiv = 1 AND Uebergabe_Art = 'RADIATOR'", PROJEKT));

            DataRow wp = Anlage(PROJEKT, 1);
            Assert.Equal(1L, Convert.ToInt64(wp["Sperrung"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(wp["Sperrzeit_von"], CultureInfo.InvariantCulture));
            Assert.Equal(6L, Convert.ToInt64(wp["Sperrzeit_bis"], CultureInfo.InvariantCulture));
            Assert.Equal(50.0, Convert.ToDouble(wp["Vorlauf_Max"], CultureInfo.InvariantCulture));
            Assert.Equal(55L, Convert.ToInt64(wp["Vorlauf"], CultureInfo.InvariantCulture));
            Assert.True(wp["Zeitprogramm"] == DBNull.Value, "Wärmepumpe trägt ein Zeitprogramm");
            foreach (int typ in new[] { 10, 11 })
            {
                DataRow a = Anlage(PROJEKT, typ);
                Assert.Equal(ZEITPROGRAMM, Convert.ToString(a["Zeitprogramm"], CultureInfo.InvariantCulture));
                Assert.Equal(0L, Convert.ToInt64(a["Sperrung"], CultureInfo.InvariantCulture));
                Assert.True(a["Vorlauf_Max"] == DBNull.Value, "Typ " + typ + ": Vorlauf_Max ist nicht leer");
            }
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Sperrfenster WHERE ID_Energieanlage IN " +
                                  "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)", PROJEKT));
        }

        [Fact]
        public void Projekt_1056_ist_eine_Kopie_von_1047_und_1047_bleibt_ohne_Fahrplan()
        {
            if (!_db.Vorhanden) return;
            DataRow v = DataRepository.GetDataTable("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("@p", VORLAGE)).Rows[0];
            DataRow k = DataRepository.GetDataTable("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)).Rows[0];
            foreach (string s in new[] { "Tool_1", "Tool_2", "Tool_3", "Tool_4", "Tool_5", "Tool_6", "Kuehlbetrieb", "Anlagenkopplung" })
                Assert.Equal(Convert.ToString(v[s], CultureInfo.InvariantCulture), Convert.ToString(k[s], CultureInfo.InvariantCulture));
            string typen = "SELECT GROUP_CONCAT(ID_Type, ',') FROM (SELECT ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID_Type)";
            Assert.Equal(Convert.ToString(DataRepository.ExecuteScalar(typen, new DbParam("@p", VORLAGE)), CultureInfo.InvariantCulture),
                         Convert.ToString(DataRepository.ExecuteScalar(typen, new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND " +
                                  "(Sperrung = 1 OR Zeitprogramm IS NOT NULL OR Vorlauf_Max IS NOT NULL)", VORLAGE));
        }

        [Fact]
        public void Ein_Lauf_kappt_den_Fahrplan_und_schreibt_Komfortstunden_mit_dem_Restbedarf_daneben()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            int kopf = new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK2_PROFILWEG_NAEHERUNG, SimulationProtokoll.Aktuell.Hinweise);

            ErgebnisEnergiebedarfModel e = new ErgebnisCtrl().Load(PROJEKT).Energiebedarf;
            Assert.True(e.FahrplanBegrenztStundenH > 0, "Fahrplan_Begrenzt_Stunden = " + e.FahrplanBegrenztStundenH);
            Assert.True(e.KomfortUnterschreitungsstundenH > 0, "Komfortstunden = " + e.KomfortUnterschreitungsstundenH);
            Assert.True(e.KomfortKelvinstundenKh >= e.KomfortUnterschreitungsstundenH * GebaeudeFestwerte.KOMFORT_SCHWELLE_K);
            Assert.True(e.KomfortLaengsteStreckeH > 0 && e.KomfortLaengsteStreckeH <= e.KomfortUnterschreitungsstundenH);
            Assert.NotNull(e.KomfortUndRestbedarf);
            Assert.Equal(e.Waermerestbedarf, e.KomfortUndRestbedarf.Value.WaermerestbedarfMwh);
        }
    }
}
