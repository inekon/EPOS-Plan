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
    /// <b>Wache des Referenzprojekts 1061 „Referenzprojekt KK"</b> (RP-KK; Entwurf KK Abschnitt 4, E106 Q-KK-6 (a)) — die
    /// Kopie des Referenzprojekts 1058 (Stufe AK3, Heizungspuffer, Raumeinfluss, Fahrplan, reversible Wärmepumpe) mit
    /// Kühlkurve am Gebäude (Raumeinfluss 3 K/K, Auslegungsweg „tagesmittel“) und dem Kühlvorlauf der Wärmepumpe auf
    /// 12 °C, damit der Erzeuger mit der Kurve an milden Tagen wärmer gleitet. Die gesäten Zellen schreibt
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_1061_kk.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht; <b>1061 ist eine Kopie von 1058</b>, <b>1058 bleibt unverändert</b>.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> schreibt die drei Kennzahlen der Kühlkurve: Die Kurve gleitet (mittlerer
    /// Vorlauf über der Vorlaufgrenze und weit über dem Anlagenvorlauf), der Raumeinfluss senkt (Absenkung &gt; 0), die
    /// Vorlaufgrenze führt in einem Teil der Kühlstunden; das Gebäudeergebnis weist den gerechneten Vorlauf aus.</item>
    /// </list>
    /// 1061 gehört zur Einfrierregel „gesäte Auslegungsdaten der Übergabe“ (Kühlkurve am Gebäude).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlkurveReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public KuehlkurveReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        internal const int PROJEKT = 1061;
        private const int VORLAGE = 1058;
        private const string NAME = "Referenzprojekt KK";
        private const int TYP_WP = 1, TYP_KESSEL = 10, TYP_BHKW = 11;

        internal static long Zahl(string sql, params object[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray()),
                               CultureInfo.InvariantCulture);

        internal static DataRow Zeile(string sql, params object[] werte)
        {
            DataTable t = DataRepository.GetDataTable(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static string Text(object o) => Convert.ToString(o, CultureInfo.InvariantCulture);
        private static double Wert(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);

        /// <summary>Alle Zellen zweier Zeilen gleich, außer Schlüsseln (<c>ID</c>, <c>ID_*</c>) und den genannten.</summary>
        internal static void ZellenGleich(DataRow vorlage, DataRow kopie, string wo, params string[] ausser)
        {
            var aus = new HashSet<string>(ausser, StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn c in vorlage.Table.Columns)
            {
                string n = c.ColumnName;
                if (n.Equals("ID", StringComparison.OrdinalIgnoreCase) || n.StartsWith("ID_", StringComparison.OrdinalIgnoreCase)
                    || aus.Contains(n)) continue;
                Assert.True(Text(vorlage[n]) == Text(kopie[n]), wo + "." + n + ": " + Text(vorlage[n]) + " ≠ " + Text(kopie[n]));
            }
        }

        internal static object Kennzahl(int projekt, string spalte)
            => DataRepository.ExecuteScalar("SELECT e." + spalte + " FROM Tab_ErgebnisEnergiebedarf e JOIN Tab_Ergebnis k ON k.ID = e.ID_Ergebnis " +
                                            "WHERE k.ID_Projekt = ? ORDER BY e.ID DESC LIMIT 1", new DbParam("?", projekt));

        internal static object Gebaeudekennzahl(int projekt, string spalte)
            => DataRepository.ExecuteScalar("SELECT g." + spalte + " FROM Tab_ErgebnisGebaeude g JOIN Tab_Ergebnis k ON k.ID = g.ID_Ergebnis " +
                                            "WHERE k.ID_Projekt = ? ORDER BY g.ID DESC LIMIT 1", new DbParam("?", projekt));

        internal static readonly string[] KURVENSPALTEN =
        {
            GebaeudeSchema.SPALTE_KUEHLKURVE_AKTIV, GebaeudeSchema.SPALTE_KUEHLKURVE_FUSSPUNKT, GebaeudeSchema.SPALTE_KUEHLKURVE_RAUMEINFLUSS,
            GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_WEG, GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_AUSSEN,
        };

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_geplant()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, Text(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT))));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK3, KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));

            DataRow g = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(1L, Convert.ToInt64(g[GebaeudeSchema.SPALTE_KUEHLKURVE_AKTIV], CultureInfo.InvariantCulture));
            Assert.Equal(GebaeudeFestwerte.KUEHLKURVE_RAUMEINFLUSS_VORGABE, Wert(g[GebaeudeSchema.SPALTE_KUEHLKURVE_RAUMEINFLUSS]));
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL, Text(g[GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_WEG]));
            Assert.True(g[GebaeudeSchema.SPALTE_KUEHLKURVE_FUSSPUNKT] == DBNull.Value, "Fußpunkt ist nicht leer");
            Assert.True(g[GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_AUSSEN] == DBNull.Value, "Auslegungs-Außentemperatur ist nicht leer");
            Assert.Equal("KUEHLDECKE", Text(g["Kuehl_Uebergabe_Art"]));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = ?", g["ID"]));

            DataRow wp = Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(1L, Convert.ToInt64(wp["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(12.0, Wert(wp["Kuehl_Vorlauf"]));
        }

        [Fact]
        public void Projekt_1061_ist_eine_Kopie_von_1058_und_1058_bleibt_unveraendert()
        {
            if (!_db.Vorhanden) return;
            ZellenGleich(Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT), "Tab_Einstellungen");
            ZellenGleich(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT), "Tab_Gebaeude", KURVENSPALTEN);
            ZellenGleich(Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", PROJEKT), "Tab_WP", "Kuehl_Vorlauf");
            foreach (int typ in new[] { TYP_WP, TYP_KESSEL, TYP_BHKW })
                ZellenGleich(Zeile("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", VORLAGE, typ),
                             Zeile("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", PROJEKT, typ),
                             "Tab_Energieanlagen[" + typ + "]", "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer");
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", VORLAGE),
                         Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung k JOIN Tab_WP w ON w.ID = k.ID_WP WHERE w.ID_Projekt = ?", VORLAGE),
                         Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung k JOIN Tab_WP w ON w.ID = k.ID_WP WHERE w.ID_Projekt = ?", PROJEKT));

            // 1058 unverändert: fester Kühlvorlauf 18 °C, keine Kühlkurve.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = ? AND Kuehl_Vorlauf = 18", VORLAGE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Kuehlkurve_Aktiv IS NULL " +
                                  "AND Kuehlkurve_Raumeinfluss IS NULL AND Kuehlkurve_Auslegung_Weg IS NULL", VORLAGE));
        }

        [Fact]
        public void Ein_Lauf_schreibt_die_Kennzahlen_die_Kurve_gleitet_und_der_Raumeinfluss_senkt()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            Assert.True(r.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, "Lauf " + PROJEKT + " gescheitert: " + fehler);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);

            Ak3Weg weg = r.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Assert.True(weg.Kreis.DurchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            KuehlRaumeinfluss k2 = weg.Kreis.KuehlRaumeinfluss;
            Assert.NotNull(k2);

            foreach (string s in KuehlkurveSchema.SPALTEN_ERGEBNIS)
                Assert.False(Kennzahl(PROJEKT, s) is null or DBNull, s + " ist leer");
            double mittel = Wert(Kennzahl(PROJEKT, KuehlkurveSchema.SPALTE_VORLAUF_MITTEL));
            double absenkung = Wert(Kennzahl(PROJEKT, KuehlkurveSchema.SPALTE_ABSENKUNG_KH));
            long grenze = Convert.ToInt64(Kennzahl(PROJEKT, KuehlkurveSchema.SPALTE_VORLAUFGRENZE_STUNDEN), CultureInfo.InvariantCulture);
            _aus.WriteLine("1061: Vorlauf Mittel {0:0.00} °C, Absenkung {1:0.0} Kh, an der Grenze {2} h, Kühlstunden {3}, Durchläufe max {4}",
                           mittel, absenkung, grenze, k2.Kuehlstunden, weg.Kreis.DurchlaeufeMax);
            Assert.Equal(k2.KuehlVorlaufMittelC, mittel, 2);
            Assert.Equal(k2.AbsenkungSummeKh, absenkung, 1);
            Assert.Equal(k2.StundenAnVorlaufgrenze, grenze);

            // Die Kurve gleitet: zwischen Vorlaufgrenze (16 °C) und Fußpunkt (19 °C), weit über dem Anlagenvorlauf 12 °C.
            Assert.InRange(mittel, 16.5, 19.0);
            Assert.True(k2.KuehlVorlaufMinC >= 16.0 - 1e-9, "Vorlauf unter der Grenze: " + k2.KuehlVorlaufMinC);
            // Der Raumeinfluss senkt, die Grenze führt nur in einem Teil der Kühlstunden.
            Assert.True(absenkung > 0.0, "Der Raumeinfluss senkt nicht.");
            Assert.True(k2.StundenAbgesenkt > 0);
            Assert.True(grenze > 0 && grenze < k2.Kuehlstunden, "Stunden an der Grenze " + grenze + " von " + k2.Kuehlstunden);

            // Das Gebäudeergebnis weist den gerechneten Vorlauf aus (keinen festen 16 °C).
            double gebaeude = Wert(Gebaeudekennzahl(PROJEKT, KuehluebergabeSchema.SPALTE_KUEHL_VORLAUF_MITTEL_C));
            _aus.WriteLine("1061: KuehlVorlaufMittel_C des Gebäudes {0:0.00} °C", gebaeude);
            Assert.InRange(gebaeude, 16.0, 19.0);
            Assert.NotEqual(16.0, gebaeude);
        }
    }
}
