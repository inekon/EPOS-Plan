using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1055 „Kältemaschine mit Kältespeicher"</b> (KU3-4b, E68) — die Kopie des
    /// Kühlreferenzprojekts 1017, in der eine Kältemaschine mit Trockenkühler und eigenem Zähler samt
    /// Kaltwasserspeicher die Kälte deckt; die Wärmepumpe heizt nur. Gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_1055_kaeltemaschine.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht wie im Skript: Anlagenzeile der Kältemaschine (Anzahl, Träger,
    /// Abrechnungsart), Projektkopie samt sechs Kennlinienpunkten, Kaltwasserspeicher samt Anlagenzeile,
    /// Kaskade, Kühleingaben des Gebäudes, Wärmepumpe ohne Kühlbetrieb.</item>
    /// <item><b>1055 ist eine Kopie von 1017</b>: dasselbe Gebäude mit denselben Kühleingaben, dieselben
    /// übrigen Anlagen; 1017 selbst führt weder Kältemaschine noch Puffer.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> deckt die Kälte allein über die Kältemaschine, über den eigenen
    /// Zähler mit dem Träger 58, und der Kältespeicher lädt und entlädt.</item>
    /// </list>
    /// 1055 gehört zur Einfrierregel „gesäte Kältemaschinendaten" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaeltemaschineReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1055, VORLAGE = 1017;
        private const string NAME = "Kältemaschine mit Kältespeicher";
        private const int TYP_KAELTEMASCHINE = 13, TYP_PUFFER = 12, KUEHLTRAEGER = 58, STAMM = 2;

        private static object Skalar(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static long Zahl(string sql, params object[] w) => Convert.ToInt64(Skalar(sql, w), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, params object[] w)
        {
            DataTable t = DataRepository.GetDataTable(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static double D(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);
        private static long L(object o) => Convert.ToInt64(o, CultureInfo.InvariantCulture);

        // =====================================================================
        //  Jede gesäte Zelle
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Skript()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, Convert.ToString(Skalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", PROJEKT), CultureInfo.InvariantCulture));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));

            // Kaskade und Projektschalter: Wärmeseite wie 1017, Kühlbetrieb an.
            DataRow e = Zeile("SELECT Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6, Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(new[] { "BHKW", "Heizkessel", "Wärmepumpe", "", "", "Stromspeicher" },
                         Enumerable.Range(0, 6).Select(i => Convert.ToString(e[i], CultureInfo.InvariantCulture)).ToArray());
            Assert.Equal(1L, L(e["Kuehlbetrieb"]));

            // Die Wärmepumpe heizt nur.
            DataRow wp = Zeile("SELECT Kuehlbetrieb, Kuehl_Vorlauf FROM Tab_WP WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(0L, L(wp["Kuehlbetrieb"]));
            Assert.Equal(18L, L(wp["Kuehl_Vorlauf"]));   // reist mit der Kopie, ohne Kühlbetrieb wirkungslos

            // Kühleingaben des Gebäudes wie 1017.
            DataRow g = Zeile("SELECT Kuehlung_Aktiv, Kuehl_Sollwert, Kuehlleistung_Max FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(1L, L(g["Kuehlung_Aktiv"]));
            Assert.Equal(24.0, D(g["Kuehl_Sollwert"]));
            Assert.Equal(15.0, D(g["Kuehlleistung_Max"]));

            // Anlagenzeile der Kältemaschine.
            DataRow a = Zeile("SELECT Bezeichner, ID_Kaeltemaschine, Kaeltemaschine_Anzahl, Kuehl_ID_Carrier, Kuehl_EigenerZaehler " +
                              "FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", PROJEKT, TYP_KAELTEMASCHINE);
            Assert.Equal("Kältemaschine 20 kW", a["Bezeichner"]);
            Assert.Equal(1L, L(a["Kaeltemaschine_Anzahl"]));
            Assert.Equal((long)KUEHLTRAEGER, L(a["Kuehl_ID_Carrier"]));
            Assert.Equal(1L, L(a["Kuehl_EigenerZaehler"]));
            long km = L(a["ID_Kaeltemaschine"]);

            // Der Kühlträger ist ein Stromträger des Projekts und weicht vom Netzbezugsträger ab.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ? AND \"ID_Energieträger\" = ?", PROJEKT, KUEHLTRAEGER));

            // Projektkopie des Katalogsatzes, auf ein Zehntel skaliert.
            DataRow m = Zeile("SELECT ID_Projekt, ID_Stamm, Bezeichner, Nennkaelteleistung_kW, Nenn_EER, Rueckkuehlart, Mindestteillast_Prozent, " +
                              "Hilfsstrom_Rueckkuehlung_kW, Kaltwasser_Vorlauf_Min, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil FROM Tab_Kaeltemaschine WHERE ID = ?", km);
            Assert.Equal((long)PROJEKT, L(m["ID_Projekt"]));
            Assert.Equal((long)STAMM, L(m["ID_Stamm"]));
            Assert.Equal("Kältemaschine 20 kW mit Trockenkühler", m["Bezeichner"]);
            Assert.Equal(20.0, D(m["Nennkaelteleistung_kW"]));
            Assert.Equal(4.0, D(m["Nenn_EER"]));
            Assert.Equal("TROCKENKUEHLER", m["Rueckkuehlart"]);
            Assert.Equal(20.0, D(m["Mindestteillast_Prozent"]));
            Assert.Equal(0.6, D(m["Hilfsstrom_Rueckkuehlung_kW"]), 9);
            Assert.Equal(5.0, D(m["Kaltwasser_Vorlauf_Min"]));
            Assert.True(m["Kuehl_Vorlauf"] == DBNull.Value, "Kuehl_Vorlauf ist nicht leer");
            Assert.Equal(0.05, D(m["Kuehl_Hilfsstromanteil"]), 9);

            // Sechs Kennlinienpunkte: Rückkühlung 25/35/45 °C × Kaltwasser 6/12 °C.
            DataTable k = DataRepository.GetDataTable(
                "SELECT Rueckkuehltemperatur, Kaltwassertemperatur, EER, Kaelteleistung_kW FROM Tab_Kenndaten_Kaeltemaschine " +
                "WHERE ID_Kaeltemaschine = ? AND ID_Projekt = ? ORDER BY Kaltwassertemperatur, Rueckkuehltemperatur",
                new DbParam("@p0", km), new DbParam("@p1", PROJEKT));
            var soll = new (double Rk, double Kw, double Eer, double Q)[]
            {
                (25, 6, 5.0, 21.5), (35, 6, 4.0, 20.0), (45, 6, 3.1, 18.0),
                (25, 12, 5.8, 24.0), (35, 12, 4.7, 22.5), (45, 12, 3.6, 20.5)
            };
            Assert.Equal(soll.Length, k.Rows.Count);
            for (int i = 0; i < soll.Length; i++)
            {
                DataRow r = k.Rows[i];
                Assert.Equal(soll[i].Rk, D(r[0]));
                Assert.Equal(soll[i].Kw, D(r[1]));
                Assert.Equal(soll[i].Eer, D(r[2]), 9);
                Assert.Equal(soll[i].Q, D(r[3]), 9);
            }
            // Die Spitze von 1017 (15 kW) deckt die Maschine auch an der heißesten Stützstelle.
            Assert.True(soll.Min(x => x.Q) >= D(g["Kuehlleistung_Max"]));

            // Der Kaltwasserspeicher samt Anlagenzeile.
            DataRow p = Zeile("SELECT ID, Bezeichner, Speichertyp, Gesamtvolumen, Verwendung, Vorlauf, Ruecklauf, Bereitschaftsverluste, " +
                              "Schwelle_Ein, Schwelle_Aus, Schwelle_Aus_Nachrang, Schwelle_Reserve, Nutzung_Heizung, Nutzung_Brauchwasser, " +
                              "Nutzung_Prozess, Schichten_Anzahl FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal("Kaltwasserspeicher", p["Bezeichner"]);
            Assert.Equal(DbWerte.PSP_SPEICHERTYP_PUFFER, p["Speichertyp"]);
            Assert.Equal(DbWerte.PSP_VERWENDUNG_KAELTE, p["Verwendung"]);
            Assert.Equal(2000.0, D(p["Gesamtvolumen"]));
            Assert.Equal(6.0, D(p["Vorlauf"]));
            Assert.Equal(12.0, D(p["Ruecklauf"]));
            Assert.Equal(1.0, D(p["Bereitschaftsverluste"]));
            Assert.Equal(10.0, D(p["Schwelle_Ein"]));
            Assert.Equal(95.0, D(p["Schwelle_Aus"]));
            Assert.True(p["Schwelle_Aus_Nachrang"] == DBNull.Value, "Schwelle_Aus_Nachrang ist nicht leer");
            Assert.Equal(10.0, D(p["Schwelle_Reserve"]));
            foreach (string s in new[] { "Nutzung_Heizung", "Nutzung_Brauchwasser", "Nutzung_Prozess" })
                Assert.Equal(0L, L(p[s]));
            Assert.Equal(1L, L(p["Schichten_Anzahl"]));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? AND ID_PUFFER = ?",
                                  PROJEKT, TYP_PUFFER, L(p["ID"])));

            // Sonst nichts: eine Kältemaschine und ein Puffer.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", PROJEKT));
        }

        [Fact]
        public void Das_Projekt_ist_eine_Kopie_von_1017_und_die_Vorlage_bleibt_unberuehrt()
        {
            if (!_db.Vorhanden) return;
            // Dieselben übrigen Anlagen (ohne Kältemaschine und Puffer), dasselbe Gebäude.
            string anlagen = "SELECT Bezeichner || '|' || ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type NOT IN (12, 13) ORDER BY Bezeichner";
            List<string> Liste(int projekt) => DataRepository.GetDataTable(anlagen, new DbParam("@p0", projekt)).Rows.Cast<DataRow>()
                .Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToList();
            Assert.Equal(Liste(VORLAGE), Liste(PROJEKT));
            Assert.Equal(4, Liste(PROJEKT).Count);

            string geb = "SELECT Gebaeudename || '|' || Kuehlung_Aktiv || '|' || Kuehl_Sollwert || '|' || Kuehlleistung_Max FROM Tab_Gebaeude WHERE ID_Projekt = ?";
            Assert.Equal(Convert.ToString(Skalar(geb, VORLAGE), CultureInfo.InvariantCulture),
                         Convert.ToString(Skalar(geb, PROJEKT), CultureInfo.InvariantCulture));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", PROJEKT));

            // Die Vorlage kühlt weiter mit der Wärmepumpe, ohne Kältemaschine und Puffer.
            Assert.Equal(1L, Zahl("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = 1017033"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", VORLAGE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", VORLAGE));
            // 1055, seine Übernahme in 1059 (AK3-K-K5a) und seine Kopie 1063 (KM3) sind die einzigen Projekte der Testdatenbank mit einer Kältemaschine.
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Type = ?", TYP_KAELTEMASCHINE));
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        [Fact]
        public void Ein_Lauf_deckt_die_Kaelte_ueber_die_Kaeltemaschine_mit_eigenem_Zaehler_und_Kaeltespeicher()
        {
            if (!_db.Vorhanden) return;
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));

            Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf.Kaskade;
            Assert.NotNull(kaskade);
            Kaelteerzeuger e = Assert.Single(kaskade.Erzeuger);   // die Wärmepumpe kühlt nicht
            Assert.NotNull(e.Maschine);
            Assert.Equal(0, e.IdWp);
            Assert.Equal(KUEHLTRAEGER, e.Kuehltraeger);
            Assert.True(e.EigenerZaehler);
            Assert.True(e.KaelteGesamtKwh > 0.0, "keine Kälte");
            Assert.True(e.StromGesamtKwh > 0.0, "kein Kältestrom");
            Assert.True(e.NetzbezugKwh > 0.0, "kein Netzbezug des Kältestroms");
            Assert.True(e.StundenFreieKuehlung >= 0);

            Assert.Single(kaskade.Speicher);
            Assert.True(kaskade.SpeicherladungKwh > 0.0, "der Kältespeicher lädt nicht");
            Assert.True(kaskade.SpeicherentladungKwh > 0.0, "der Kältespeicher entlädt nicht");

            Assert.True(kaskade.BedarfGesamtKwh > 0.0);
            Assert.True(kaskade.RestGesamtKwh <= 1e-3 * kaskade.BedarfGesamtKwh,
                        "Unterdeckung " + kaskade.RestGesamtKwh.ToString("F1", CultureInfo.InvariantCulture) + " kWh");

            _aus.WriteLine("1055: Bedarf {0:F1} kWh, Deckung {1:F1} kWh, Rest {2:F3} kWh, Kälte Maschine {3:F1} kWh, Strom {4:F1} kWh, Netzbezug {5:F1} kWh, " +
                           "freie Kühlung {6} h, Takt {7} h, Speicher Ladung {8:F1} kWh, Entladung {9:F1} kWh",
                           kaskade.BedarfGesamtKwh, kaskade.DeckungGesamtKwh, kaskade.RestGesamtKwh, e.KaelteGesamtKwh, e.StromGesamtKwh,
                           e.NetzbezugKwh, e.StundenFreieKuehlung, e.StundenTakt, kaskade.SpeicherladungKwh, kaskade.SpeicherentladungKwh);
        }
    }
}
