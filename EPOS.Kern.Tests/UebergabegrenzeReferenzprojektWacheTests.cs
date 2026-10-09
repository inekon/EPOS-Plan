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
    /// <b>Wache des Referenzprojekts 1060 „Referenzprojekt Übergabegrenze"</b> (Umsetzungskonzept Übergabegrenze und
    /// Bivalenz 4.4 und 8.2, Fachkonzept 5.3) — die Kopie des Referenzprojekts 1056, in der allein die Übergabegrenze
    /// der Wärmepumpe wirkt. Die gesäten Zellen schreibt <c>Referenzlaeufe/Skripte/referenzprojekt_1060_uebergabegrenze.cs</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht: Heizkörper 75/60 °C mit Exponent 1,3 am Gebäude, Wärmepumpe mit
    /// <c>Vorlauf_Max</c> 55 °C, Einbindung <c>DIREKT</c>, Vorwärmbetrieb, Parallelbetrieb ohne Abschaltpunkt, keine
    /// Sperrzeit und kein Zeitprogramm, die acht Gerätespalten der Wärmepumpe und <c>Ruecklauf_Max</c> am BHKW leer
    /// (Vorgabe), der Kessel ein Brennwertkessel mit Brennwertkennlinie, die Kaskade Wärmepumpe vor Kessel.</item>
    /// <item><b>1060 ist eine Kopie von 1056</b>: dieselbe Kopplungsstufe, dieselben Anlagentypen; 1056 selbst
    /// führt keine Einbindung.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> rechnet die Bereiche B1 und B3, keine Stunde in B4, beide
    /// Bivalenzpunkte, die größte Übergabe und die Rücklaufstufe „Vorwärmer" des Kessels — die Sollwerte des
    /// ersten Laufs mit der Toleranz des Referenzlaufs.</item>
    /// </list>
    /// 1060 gehört zur Einfrierregel „gesäte Übergabegrenzdaten" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class UebergabegrenzeReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public UebergabegrenzeReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1060;
        private const int VORLAGE = 1056;
        private const string NAME = "Referenzprojekt Übergabegrenze";
        private const int TYP_WP = 1, TYP_KESSEL = 10, TYP_BHKW = 11;

        /// <summary>Die acht Gerätespalten der Wärmepumpe (leer = Vorgabe).</summary>
        private static readonly string[] GERAETESPALTEN =
        {
            "Spreizung_Auslegung_K", "Spreizung_Max_K", "Spreizung_Min_K", "Mindestvolumenstrom_Prozent",
            "Ruecklauf_Max", "Ruecklauf_Bezug", "Ruecklauf_Abwertung_ProzentJeK", "Kaeltemittel",
        };

        // Sollwerte des ersten Laufs (Basis R45); NurKessel zählt B0 und B4 gemeinsam - ohne Sperrzeit, Zeitprogramm und
        // Abschaltpunkt gibt es kein B0, 0 Stunden heißt damit auch B4 = 0.
        private const int SOLL_B1_H = 3987, SOLL_B2_H = 0, SOLL_B3_H = 1308, SOLL_NURKESSEL_H = 0;
        private const double SOLL_BIVALENZPUNKT_1 = 1.825241, SOLL_BIVALENZPUNKT_2 = -3.549464, SOLL_UEBERGABE_MAX_KW = 21.99587;
        private const int SOLL_VORWAERMER_H = 952;

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

        private static DataRow Zeile(string sql, object wert)
        {
            DataTable t = DataRepository.GetDataTable(sql, new DbParam("@p", wert));
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static double D(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);

        /// <summary>Toleranz des Referenzlaufs: ab Betrag 1 relativ 1e-4, sonst absolut 0,01.</summary>
        private static void Nahe(double soll, double ist, string was)
        {
            double tol = Math.Abs(soll) >= 1.0 ? Math.Abs(soll) * 1e-4 : 0.01;
            Assert.True(Math.Abs(soll - ist) <= tol, was + ": soll " + soll.ToString("R", CultureInfo.InvariantCulture) +
                                                      ", ist " + ist.ToString("R", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_geplant()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.Equal("AK1", KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));

            DataRow g = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(1L, Convert.ToInt64(g["Heizkreis_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal("RADIATOR", Convert.ToString(g["Uebergabe_Art"], CultureInfo.InvariantCulture));
            Assert.Equal(75.0, D(g["Auslegung_Vorlauf"]));
            Assert.Equal(60.0, D(g["Auslegung_Ruecklauf"]));
            Assert.Equal(1.3, D(g["Uebergabe_Exponent"]));
            DataRow gv = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE);
            foreach (string s in new[] { "Auslegung_Raumtemperatur", "Auslegung_Aussentemperatur", "Uebergabe_Leistung_Nenn", "Heizkurve_Aktiv" })
                Assert.Equal(Convert.ToString(gv[s], CultureInfo.InvariantCulture), Convert.ToString(g[s], CultureInfo.InvariantCulture));

            DataRow wp = Anlage(PROJEKT, TYP_WP);
            Assert.Equal(55.0, D(wp["Vorlauf_Max"]));
            Assert.Equal("DIREKT", Convert.ToString(wp["Einbindung"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(wp["Vorwaermbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(wp["Bivalenter_Betrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.WP_BETRIEBSART_PARALLEL, Convert.ToString(wp["Betriebsart"], CultureInfo.InvariantCulture));
            Assert.True(wp["Abschaltpunkt"] == DBNull.Value, "Abschaltpunkt gesetzt");
            Assert.Equal(0L, Convert.ToInt64(wp["Sperrung"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(wp["Sperrzeit_von"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(wp["Sperrzeit_bis"], CultureInfo.InvariantCulture));
            Assert.True(wp["Zeitprogramm"] == DBNull.Value, "Wärmepumpe trägt ein Zeitprogramm");
            DataRow geraet = Zeile("SELECT * FROM Tab_WP WHERE ID = ?", wp["ID_WP"]);
            Assert.Equal(PROJEKT, Convert.ToInt32(geraet["ID_Projekt"], CultureInfo.InvariantCulture));
            foreach (string s in GERAETESPALTEN)
                Assert.True(geraet[s] == DBNull.Value, "Tab_WP." + s + " ist nicht leer");

            foreach (int typ in new[] { TYP_KESSEL, TYP_BHKW })
            {
                DataRow a = Anlage(PROJEKT, typ);
                Assert.True(a["Zeitprogramm"] == DBNull.Value, "Typ " + typ + ": Zeitprogramm gesetzt");
                Assert.Equal(0L, Convert.ToInt64(a["Sperrung"], CultureInfo.InvariantCulture));
                Assert.True(a["Vorlauf_Max"] == DBNull.Value, "Typ " + typ + ": Vorlauf_Max ist nicht leer");
                Assert.True(a["Einbindung"] == DBNull.Value, "Typ " + typ + ": Einbindung gesetzt");
            }
            DataRow bhkw = Zeile("SELECT * FROM Tab_BHKW WHERE ID = ?", Anlage(PROJEKT, TYP_BHKW)["ID_BHKW"]);
            Assert.True(bhkw["Ruecklauf_Max"] == DBNull.Value, "Tab_BHKW.Ruecklauf_Max ist nicht leer");
            DataRow kessel = Zeile("SELECT * FROM Tab_Heizkessel WHERE ID = ?", Anlage(PROJEKT, TYP_KESSEL)["ID_Kessel"]);
            Assert.Equal(1L, Convert.ToInt64(kessel["Brennwert"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(kessel["Kennlinie_Brennwert"], CultureInfo.InvariantCulture));
            Assert.Equal(3L, Convert.ToInt64(kessel["Brennstoff"], CultureInfo.InvariantCulture));
            Assert.True(kessel["Firma"] == DBNull.Value, "Kessel trägt eine Firma");

            DataRow e = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal("Wärmepumpe", Convert.ToString(e["Tool_1"], CultureInfo.InvariantCulture));
            Assert.Equal("Heizkessel", Convert.ToString(e["Tool_2"], CultureInfo.InvariantCulture));
            Assert.Equal("BHKW", Convert.ToString(e["Tool_3"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Sperrfenster WHERE ID_Energieanlage IN " +
                                  "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)", PROJEKT));
        }

        [Fact]
        public void Projekt_1060_ist_eine_Kopie_von_1056_und_1056_bleibt_ohne_Einbindung()
        {
            if (!_db.Vorhanden) return;
            DataRow v = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE);
            DataRow k = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT);
            foreach (string s in new[] { "Tool_4", "Tool_5", "Tool_6", "Kuehlbetrieb", "Anlagenkopplung" })
                Assert.Equal(Convert.ToString(v[s], CultureInfo.InvariantCulture), Convert.ToString(k[s], CultureInfo.InvariantCulture));
            string typen = "SELECT GROUP_CONCAT(ID_Type, ',') FROM (SELECT ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID_Type)";
            Assert.Equal(Convert.ToString(DataRepository.ExecuteScalar(typen, new DbParam("@p", VORLAGE)), CultureInfo.InvariantCulture),
                         Convert.ToString(DataRepository.ExecuteScalar(typen, new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND " +
                                  "(Einbindung IS NOT NULL OR Vorwaermbetrieb = 1)", VORLAGE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 1 AND Sperrung = 1", VORLAGE));
        }

        [Fact]
        public void Ein_Lauf_rechnet_die_Bereiche_die_Bivalenzpunkte_und_die_Vorwaermer_Stufe()
        {
            if (!_db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            int kopf = r.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
            int vorwaermer = r.sim.simulation_spk.RuecklaufStufenstunden(0, Ruecklaufstufe.Vorwaermer);

            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Assert.NotNull(m.Waermepumpe);
            Bereichskennzahlen b = m.Waermepumpe.Bereiche;
            Assert.NotNull(b);
            Assert.Single(m.Waermepumpe.Module);
            Bereichskennzahlen bm = m.Waermepumpe.Module[0].Bereiche;
            Assert.NotNull(bm);
            _aus.WriteLine("1060: Bereiche h " + string.Join("/", b.Stunden.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))) +
                           ", MWh " + string.Join("/", b.Mwh.Select(x => x.HasValue ? x.Value.ToString("R", CultureInfo.InvariantCulture) : "-")) +
                           "; Bivalenzpunkte " + Convert.ToString(bm.Bivalenzpunkt_1, CultureInfo.InvariantCulture) + " / " +
                           Convert.ToString(bm.Bivalenzpunkt_2, CultureInfo.InvariantCulture) + " °C; Übergabe max " +
                           Convert.ToString(bm.Uebergabe_Max_kW, CultureInfo.InvariantCulture) + " kW; Vorwärmer " + vorwaermer +
                           " h; WP " + m.Waermepumpe.Waermeproduktion_WP.ToString("R", CultureInfo.InvariantCulture) +
                           " MWh, Kessel " + m.Heizkessel?.Waermeproduktion.ToString("R", CultureInfo.InvariantCulture) +
                           " MWh; Komfort " + m.Energiebedarf.KomfortUnterschreitungsstundenH + " h");

            Assert.True(b.Stunden[0] > 0, "B1 ohne Stunden");
            Assert.True(b.Stunden[2] > 0, "B3 ohne Stunden");
            Assert.Equal(SOLL_B1_H, b.Stunden[0]);
            Assert.Equal(SOLL_B2_H, b.Stunden[1]);
            Assert.Equal(SOLL_B3_H, b.Stunden[2]);
            Assert.Equal(SOLL_NURKESSEL_H, b.Stunden[3]);
            for (int i = 0; i < 4; i++) Assert.Equal(b.Stunden[i], bm.Stunden[i]);
            Assert.True(bm.Bivalenzpunkt_1.HasValue && bm.Bivalenzpunkt_2.HasValue, "Bivalenzpunkte nicht gesetzt");
            Nahe(SOLL_BIVALENZPUNKT_1, bm.Bivalenzpunkt_1.Value, "Bivalenzpunkt_1");
            Nahe(SOLL_BIVALENZPUNKT_2, bm.Bivalenzpunkt_2.Value, "Bivalenzpunkt_2");
            Assert.True(bm.Uebergabe_Max_kW > 0, "Uebergabe_Max_kW nicht gesetzt");
            Nahe(SOLL_UEBERGABE_MAX_KW, bm.Uebergabe_Max_kW.Value, "Uebergabe_Max_kW");
            Assert.True(vorwaermer > 0, "Stufe Vorwärmer ohne Stunden");
            Assert.Equal(SOLL_VORWAERMER_H, vorwaermer);
        }
    }
}
