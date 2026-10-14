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
    /// <b>Wache des Referenzprojekts 1063 „Referenzprojekt Kältemaschine Teillast"</b> (KM3, Fachkonzept Teillast und
    /// Takten der Kältemaschine, Abschnitt 8.2) — die Kopie des Referenzprojekts 1055, an deren Kältemaschine
    /// Teillastkurve, Mindestteillast 30 %, Takten mit dem Vorgabe-Taktverlustfaktor und die Gütegrad-Extrapolation
    /// wirken. Gesät von <c>Referenzlaeufe/Skripte/referenzprojekt_1063_kaeltemaschine_teillast.cs</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> der Projektkopie der Kältemaschine steht wie im Skript; jede übrige Zelle,
    /// die sechs Kennlinienpunkte, der Kältespeicher, die Anlagenzeilen und die Kaskade wie in 1055.</item>
    /// <item><b>1063 ist eine Kopie von 1055</b>, und 1055 rechnet weiter ohne Teillastweg (Bestand der Basis).</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> taktet (Taktstunden, Starts, Taktstrom), rechnet Teillaststunden und
    /// einen mittleren Lastgrad, extrapoliert mindestens eine Stunde mit dem Gütegrad, schreibt die Kennzahlen in
    /// <c>Tab_ErgebnisKaeltemaschine</c> und kommt auf einen anderen Jahres-EER als 1055.</item>
    /// </list>
    /// 1063 gehört zur Einfrierregel „gesäte Teillastdaten einer Kältemaschine eines Referenzprojekts" und als
    /// Kopie von 1055 zu „gesäte Kältemaschinendaten" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineTeillastReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaeltemaschineTeillastReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1063, VORLAGE = 1055;
        private const string NAME = "Referenzprojekt Kältemaschine Teillast";
        private const int TYP_KAELTEMASCHINE = 13, TYP_PUFFER = 12;

        /// <summary>Die gesäten Zellen der Projektkopie (null = leer).</summary>
        private static readonly (string Spalte, object Wert)[] GESAET =
        {
            ("Teillast_Weg", "KURVE"),
            ("Teillastkurve_a", 0.10),
            ("Teillastkurve_b", 0.60),
            ("Teillastkurve_c", 0.30),
            ("Teillastkurve_Lastgrad_Min", 0.2),
            ("Taktverlustfaktor_Cd", null),
            ("Mindestteillast_Prozent", 30.0),
            ("Verdichterregelung", "STUFEN"),
            ("Kennfeld_Randweg", "GUETEGRAD"),
        };

        private static object Skalar(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static long Zahl(string sql, params object[] w) => Convert.ToInt64(Skalar(sql, w), CultureInfo.InvariantCulture);

        private static DataTable Tabelle(string sql, params object[] w)
            => DataRepository.GetDataTable(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static DataRow Zeile(string sql, params object[] w)
        {
            DataTable t = Tabelle(sql, w);
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static string T(object o) => o == DBNull.Value ? "<leer>" : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>Spalten ohne Kennungen: alles außer ID, ID_Projekt und den Verweisen auf kopierte Zeilen.</summary>
        private static IEnumerable<string> Fachspalten(DataTable t, params string[] ohne)
            => t.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                .Where(n => !n.Equals("ID", StringComparison.OrdinalIgnoreCase)
                            && !n.Equals("ID_Projekt", StringComparison.OrdinalIgnoreCase)
                            && !ohne.Contains(n, StringComparer.OrdinalIgnoreCase));

        // =====================================================================
        //  Jede gesäte Zelle
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Skript()
        {
            if (!_db.Vorhanden) return;
            DataRow p = Zeile("SELECT Projektname, Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", PROJEKT);
            Assert.Equal(NAME, T(p["Projektname"]));
            Assert.StartsWith("Referenzprojekt Kältemaschine Teillast: Kopie von Projekt 1055", T(p["Beschreibung"]), StringComparison.Ordinal);
            Assert.Equal(DBNull.Value, p["Kosten_Geaendert"]);

            DataRow km = Zeile("SELECT * FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", PROJEKT);
            foreach (var (spalte, soll) in GESAET)
            {
                object ist = km[spalte];
                if (soll == null) Assert.True(ist == DBNull.Value, spalte + " ist " + T(ist) + " statt leer");
                else if (soll is double d) Assert.Equal(d, Convert.ToDouble(ist, CultureInfo.InvariantCulture), 12);
                else Assert.Equal(soll, T(ist));
            }

            // Die Kurve ist die des Zahlenbeispiels 3.2: EIRFPLR(1) = 1, E(0,5) = 0,475, g(0,5) = 1,05.
            double a = 0.10, b = 0.60, c = 0.30;
            Assert.Equal(1.0, a + b + c, 12);
            Assert.Equal(0.475, a + b * 0.5 + c * 0.25, 12);
        }

        [Fact]
        public void Alle_uebrigen_Zellen_der_Kaelteseite_stehen_wie_in_1055()
        {
            if (!_db.Vorhanden) return;
            string[] gesaet = GESAET.Select(g => g.Spalte).ToArray();
            DataTable km = Tabelle("SELECT * FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", PROJEKT);
            DataTable kmV = Tabelle("SELECT * FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", VORLAGE);
            Assert.Equal(1, km.Rows.Count);
            Assert.Equal(1, kmV.Rows.Count);
            foreach (string s in Fachspalten(km, gesaet))
                Assert.True(T(kmV.Rows[0][s]) == T(km.Rows[0][s]), "Tab_Kaeltemaschine." + s + ": " + T(km.Rows[0][s]) + " statt " + T(kmV.Rows[0][s]));
            // 1055 rechnet ohne Teillastweg und mit 20 % Mindestteillast.
            Assert.Equal(DBNull.Value, kmV.Rows[0]["Teillast_Weg"]);
            Assert.Equal(DBNull.Value, kmV.Rows[0]["Kennfeld_Randweg"]);
            Assert.Equal(20.0, Convert.ToDouble(kmV.Rows[0]["Mindestteillast_Prozent"], CultureInfo.InvariantCulture), 12);

            string punkte = "SELECT Rueckkuehltemperatur || '|' || Kaltwassertemperatur || '|' || EER || '|' || Kaelteleistung_kW " +
                            "FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ? ORDER BY ID";
            List<string> Punkte(object id) => Tabelle(punkte, id).Rows.Cast<DataRow>().Select(r => T(r[0])).ToList();
            Assert.Equal(6, Punkte(km.Rows[0]["ID"]).Count);
            Assert.Equal(Punkte(kmV.Rows[0]["ID"]), Punkte(km.Rows[0]["ID"]));

            // Anlagenzeilen von Kältemaschine und Kältespeicher (Anzahl, Träger, Abrechnungsart, Bezeichner).
            string anlage = "SELECT Bezeichner || '|' || ID_Type || '|' || IFNULL(Kaeltemaschine_Anzahl, '') || '|' || IFNULL(Kuehl_ID_Carrier, '') " +
                            "|| '|' || IFNULL(Kuehl_EigenerZaehler, '') FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type IN (?, ?) ORDER BY ID_Type";
            List<string> Anlagen(int projekt) => Tabelle(anlage, projekt, TYP_PUFFER, TYP_KAELTEMASCHINE).Rows.Cast<DataRow>().Select(r => T(r[0])).ToList();
            Assert.Equal(2, Anlagen(PROJEKT).Count);
            Assert.Equal(Anlagen(VORLAGE), Anlagen(PROJEKT));

            // Kältespeicher: jede Zelle wie in 1055.
            DataTable sp = Tabelle("SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", PROJEKT);
            DataTable spV = Tabelle("SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", VORLAGE);
            Assert.Equal(1, sp.Rows.Count);
            Assert.Equal(1, spV.Rows.Count);
            Assert.Equal("Kaelte", T(sp.Rows[0]["Verwendung"]));
            foreach (string s in Fachspalten(sp))
                Assert.True(T(spV.Rows[0][s]) == T(sp.Rows[0][s]), "Tab_Pufferspeicher." + s + ": " + T(sp.Rows[0][s]) + " statt " + T(spV.Rows[0][s]));

            // Kaskade und Kühlbetrieb wie in 1055.
            string kaskade = "SELECT Tool_1 || '|' || Tool_2 || '|' || Tool_3 || '|' || Tool_4 || '|' || Tool_5 || '|' || Tool_6 || '|' || Kuehlbetrieb " +
                             "FROM Tab_Einstellungen WHERE ID_Projekt = ?";
            Assert.Equal(T(Skalar(kaskade, VORLAGE)), T(Skalar(kaskade, PROJEKT)));
        }

        [Fact]
        public void Das_Projekt_ist_eine_Kopie_von_1055()
        {
            if (!_db.Vorhanden) return;
            string anlagen = "SELECT Bezeichner || '|' || ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY Bezeichner, ID_Type";
            List<string> Liste(int projekt) => Tabelle(anlagen, projekt).Rows.Cast<DataRow>().Select(r => T(r[0])).ToList();
            Assert.Equal(Liste(VORLAGE), Liste(PROJEKT));

            string geb = "SELECT Gebaeudename || '|' || Kuehlung_Aktiv || '|' || Kuehl_Sollwert || '|' || Kuehlleistung_Max FROM Tab_Gebaeude WHERE ID_Projekt = ?";
            Assert.Equal(T(Skalar(geb, VORLAGE)), T(Skalar(geb, PROJEKT)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", PROJEKT));
            // Die Wärmepumpe heizt nur, wie in 1055.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = ? AND Kuehlbetrieb = 1", PROJEKT));
            // 1063 ist das einzige Projekt der Testdatenbank mit einem Teillastweg an einer Projektkältemaschine.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt IS NOT NULL AND Teillast_Weg IS NOT NULL"));
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        [Fact]
        public void Ein_Lauf_taktet_rechnet_Teillast_und_extrapoliert_mit_anderem_Jahres_EER_als_1055()
        {
            if (!_db.Vorhanden) return;
            (Kaelteerzeuger e, int kopf) Lauf(int projekt)
            {
                var lauf = new SimulationRunner();
                int k = lauf.SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(k > 0, "Lauf " + projekt + " gescheitert: " + fehler);
                Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));
                Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf.Kaskade;
                Assert.NotNull(kaskade);
                Assert.True(kaskade.RestGesamtKwh <= 1e-3 * kaskade.BedarfGesamtKwh, "Unterdeckung in " + projekt);
                return (Assert.Single(kaskade.Erzeuger), k);
            }

            var (e, kopf) = Lauf(PROJEKT);
            Assert.NotNull(e.Maschine);
            Assert.True(e.Maschine.TeillastWirksam, "Teillastweg nicht wirksam");
            Assert.True(e.Maschine.GuetegradWirksam, "Gütegradweg nicht wirksam");
            Assert.True(e.Taktstunden > 0, "keine Taktstunden");
            Assert.True(e.Starts > 0, "keine Starts");
            Assert.True(e.Starts >= e.Taktstunden, "weniger Starts als Taktstunden");
            Assert.True(e.TaktstromKwh > 0.0, "kein Taktstrom");
            Assert.True(e.StundenTeillast > 0, "keine Teillaststunden");
            Assert.InRange(e.LastgradMittel, 0.2, 1.0);
            Assert.True(e.StundenExtrapoliert > 0, "keine extrapolierte Stunde");

            DataRow r = Zeile("SELECT Taktstrom_MWh, Starts, Teillaststunden, Lastgrad_Mittel, Stunden_Extrapoliert, Taktstunden " +
                              "FROM Tab_ErgebnisKaeltemaschine WHERE ID_Ergebnis = ?", kopf);
            Assert.True(Convert.ToDouble(r["Taktstrom_MWh"], CultureInfo.InvariantCulture) > 0.0, "Taktstrom_MWh leer");
            Assert.True(Convert.ToInt64(r["Starts"], CultureInfo.InvariantCulture) > 0);
            Assert.True(Convert.ToInt64(r["Teillaststunden"], CultureInfo.InvariantCulture) > 0);
            Assert.True(Convert.ToInt64(r["Stunden_Extrapoliert"], CultureInfo.InvariantCulture) > 0);
            Assert.True(Convert.ToInt64(r["Taktstunden"], CultureInfo.InvariantCulture) > 0);

            var (eV, _) = Lauf(VORLAGE);
            Assert.False(eV.Maschine.TeillastWirksam);
            double eer = e.KaelteGesamtKwh / e.StromGesamtKwh, eerV = eV.KaelteGesamtKwh / eV.StromGesamtKwh;
            Assert.True(Math.Abs(eer - eerV) > 1e-4, "Jahres-EER gleich 1055");

            _aus.WriteLine("1063: Kälte {0:F1} kWh, Strom {1:F1} kWh, EER {2:F4}, Takt {3} h, Starts {4}, Taktstrom {5:F2} kWh, Teillast {6} h, " +
                           "Lastgrad {7:F3}, extrapoliert {8} h; 1055: Kälte {9:F1} kWh, Strom {10:F1} kWh, EER {11:F4}, Takt {12} h",
                           e.KaelteGesamtKwh, e.StromGesamtKwh, eer, e.Taktstunden, e.Starts, e.TaktstromKwh, e.StundenTeillast,
                           e.LastgradMittel, e.StundenExtrapoliert, eV.KaelteGesamtKwh, eV.StromGesamtKwh, eerV, eV.Taktstunden);
        }
    }
}
