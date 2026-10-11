using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1065 „Referenzprojekt Vorheizen"</b> (AH-V7, Entwurf Vorheizrampe Abschnitt 4, F9) —
    /// die Kopie des Referenzprojekts 1051 „Referenzprojekt Konditionierung", deren Gebäude vor dem Kalendersprung mit
    /// berechneter Vorheizzeit vorheizt: Verfahren <c>BERECHNET</c>, alle übrigen Vorheizzellen leer (Toleranz 20 %,
    /// ε 1 K, Geltung Gebäude, Heizkreis einbezogen — die Vorgaben des Kerns). Gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_1065_vorheizen.cs</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht wie im Skript; jede übrige Fachzelle der Einstellungen und des Gebäudes steht
    /// wie in 1051.</item>
    /// <item><b>1065 ist das einzige Projekt mit Vorheizen</b>; 1051 rechnet weiter die Sollwertrampe (Bestand, F9).</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> bemisst t_V = 2 h als 95-%-Quantil, den Deckel aus Φ_K,max mit 20 %
    /// Toleranz, hält die Spitze am Deckel, schreibt die Spalten <c>Vorheiz*</c> und braucht rund 0,45 MWh mehr Heizwärme
    /// als 1051; 1051 schreibt keine Vorheizgröße.</item>
    /// <item><b>Gegen die Basis</b>: die Exportschlüssel <c>Geb[0].Vorheiz*</c> des Laufs stehen wie in der Basis.</item>
    /// </list>
    /// 1065 gehört zur Einfrierregel „gesäte Vorheizdaten eines Referenzprojekts" und als Kopie von 1051 zu „gesäte
    /// Konditionierungsdaten" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class VorheizReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public VorheizReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1065, VORLAGE = 1051;
        private const string NAME = "Referenzprojekt Vorheizen";

        // Die Kennzahlen der Basis R52 (Referenzlaeufe/2026-10-11_R52_Vorheizen/Projekt_1065/aggregate.csv).
        private const int R52_VORHEIZZEIT_H = 2, R52_BEDARF_MAX_H = 5, R52_TAGE_UEBER = 5, R52_TAGE_VERFEHLT = 5;
        private const double R52_BEZUG_KW = 23.6166077, R52_DECKEL_KW = 28.3399293;
        private const double R52_HEIZWAERME_1065_MWH = 24.6731709, R52_HEIZWAERME_1051_MWH = 24.223504;

        /// <summary>Die sieben Vorheizspalten an <c>Tab_Einstellungen</c> (Schritt 215).</summary>
        private static readonly string[] VORHEIZSPALTEN =
        {
            VorheizenSchema.SPALTE_VERFAHREN, VorheizenSchema.SPALTE_VORHEIZZEIT, VorheizenSchema.SPALTE_TOLERANZ_ART,
            VorheizenSchema.SPALTE_TOLERANZ, VorheizenSchema.SPALTE_REGELGENAUIGKEIT, VorheizenSchema.SPALTE_GELTUNG,
            VorheizenSchema.SPALTE_HEIZKREIS,
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

        private static string T(object o) => o == DBNull.Value ? "<leer>" : Convert.ToString(o, CultureInfo.InvariantCulture);

        // =====================================================================
        //  Die Saat
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Skript()
        {
            if (!_db.Vorhanden) return;
            DataRow p = Zeile("SELECT Projektname, Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", PROJEKT);
            Assert.Equal(NAME, T(p["Projektname"]));
            Assert.StartsWith("Referenzprojekt Vorheizen: Kopie von Projekt 1051", T(p["Beschreibung"]), StringComparison.Ordinal);
            Assert.Equal(DBNull.Value, p["Kosten_Geaendert"]);

            DataRow e = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(1L, Convert.ToInt64(e["Aufheizoptimierung"], CultureInfo.InvariantCulture));
            Assert.Equal(VorheizenSchema.VERFAHREN_BERECHNET, T(e[VorheizenSchema.SPALTE_VERFAHREN]));
            foreach (string s in VORHEIZSPALTEN.Skip(1)) Assert.True(e[s] == DBNull.Value, s + " = " + T(e[s]) + " statt leer");

            // Die Vorgaben des Kerns greifen: Toleranz 20 %, ε 1 K, Geltung Gebäude, Heizkreis einbezogen.
            Vorheizvorgabe v = KonfigurationCtrl.VorheizvorgabeLesen(PROJEKT);
            Assert.NotEqual(Vorheizvorgabe.Sollwertrampe, v);
            _aus.WriteLine("Vorheizvorgabe 1065: " + v);

            // Gebäude und Zonen ohne gepflegte Vorheizzeit.
            Assert.Equal(DBNull.Value, Zeile("SELECT Aufheizzeit_Manuell_H FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT)[0]);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone z JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude " +
                                  "WHERE g.ID_Projekt = ? AND z.Vorheizzeit_H IS NOT NULL", PROJEKT));
        }

        [Fact]
        public void Alle_uebrigen_Zellen_stehen_wie_in_1051()
        {
            if (!_db.Vorhanden) return;
            DataRow e = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT);
            DataRow eV = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE);
            foreach (DataColumn c in e.Table.Columns)
            {
                string s = c.ColumnName;
                if (s.IndexOf("ID", StringComparison.Ordinal) >= 0 || VORHEIZSPALTEN.Contains(s, StringComparer.OrdinalIgnoreCase)) continue;
                Assert.True(T(eV[s]) == T(e[s]), "Tab_Einstellungen." + s + ": " + T(e[s]) + " statt " + T(eV[s]));
            }
            // 1051 rechnet die Sollwertrampe: alle sieben Vorheizzellen leer.
            foreach (string s in VORHEIZSPALTEN) Assert.True(eV[s] == DBNull.Value, "1051: " + s + " = " + T(eV[s]));
            Assert.Equal(Vorheizvorgabe.Sollwertrampe, KonfigurationCtrl.VorheizvorgabeLesen(VORLAGE));

            DataRow g = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT);
            DataRow gV = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE);
            foreach (DataColumn c in g.Table.Columns)
            {
                string s = c.ColumnName;
                if (s.IndexOf("ID", StringComparison.Ordinal) >= 0) continue;
                Assert.True(T(gV[s]) == T(g[s]), "Tab_Gebaeude." + s + ": " + T(g[s]) + " statt " + T(gV[s]));
            }
            // Kalender, Vorgaben und Perioden am Gebäude wie in 1051.
            string kal = "SELECT k.Groesse || '|' || IFNULL(k.Wert, '') || '|' || IFNULL(k.Aus, '') || '|' || IFNULL(k.Woche, '') || '|' || " +
                         "(SELECT COUNT(*) FROM Tab_Konditionierungsperiode p WHERE p.ID_Kalender = k.ID) " +
                         "FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude WHERE g.ID_Projekt = ? ORDER BY k.Groesse, k.ID";
            List<string> Kalender(int projekt) => DataRepository.GetDataTable(kal, new DbParam("@p0", projekt)).Rows.Cast<DataRow>()
                                                               .Select(r => T(r[0])).ToList();
            Assert.NotEmpty(Kalender(PROJEKT));
            Assert.Equal(Kalender(VORLAGE), Kalender(PROJEKT));
        }

        [Fact]
        public void Das_Projekt_ist_das_einzige_mit_Vorheizen()
        {
            if (!_db.Vorhanden) return;
            string bedingung = string.Join(" OR ", VORHEIZSPALTEN.Select(s => s + " IS NOT NULL"));
            Assert.Equal((long)PROJEKT, Zahl("SELECT ID_Projekt FROM Tab_Einstellungen WHERE " + bedingung));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE " + bedingung));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE Vorheizzeit_H IS NOT NULL"));
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        [Fact]
        public void Ein_Lauf_bemisst_die_Vorheizzeit_haelt_den_Deckel_und_schreibt_die_Vorheizgroessen()
        {
            if (!_db.Vorhanden) return;
            (ErgebnisGebaeudeModel g, int kopf) Lauf(int projekt)
            {
                SimulationProtokoll.NeuStarten();
                var lauf = new SimulationRunner();
                int k = lauf.SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(k > 0, "Lauf " + projekt + " gescheitert: " + fehler);
                ErgebnisModel erg = new ErgebnisCtrl().Load(projekt);
                return (Assert.Single(erg.Gebaeude), k);
            }

            var (g, kopf) = Lauf(PROJEKT);
            Vorheizkennzahlen v = g.Vorheizen;
            Assert.NotNull(v);
            Assert.Equal(VorheizenSchema.VERFAHREN_BERECHNET, v.Verfahren);
            // t_V ist ganzzahlig und das 95-%-Quantil; der größte Bedarf liegt darüber (F19) - exakt.
            Assert.Equal(R52_VORHEIZZEIT_H, v.VorheizzeitH);
            Assert.Equal(R52_BEDARF_MAX_H, v.VorheizzeitBedarfMaxH);
            Assert.Equal(R52_TAGE_UEBER, v.TageUeberVorheizzeit);
            Assert.Equal(0, v.NaechteOhneAbsenkung);
            Assert.Equal(0, v.UnerreichbarTage);
            Assert.Equal(0, v.SperrzeitTage);
            // Φ_K,max und Deckel mit der Toleranz des Referenzvergleichs (relativ 1e-4).
            Assert.NotNull(v.BezugKw);
            Assert.NotNull(v.DeckelKw);
            Assert.InRange(v.BezugKw.Value, R52_BEZUG_KW * (1 - 1e-4), R52_BEZUG_KW * (1 + 1e-4));
            Assert.InRange(v.DeckelKw.Value, R52_DECKEL_KW * (1 - 1e-4), R52_DECKEL_KW * (1 + 1e-4));
            // Der Deckel ist Φ_K,max mit der Vorgabe-Toleranz 20 % - die Regel selbst, nicht nur die Zahl.
            Assert.Equal(v.BezugKw.Value * 1.2, v.DeckelKw.Value, 6);
            // Die Spitze des Gebäudes steht am Deckel (P_V = P_K ohne gepflegte Leistungsgrenze) und unter der von 1051.
            Assert.InRange(g.SpitzeKw, v.DeckelKw.Value - 1e-6, v.DeckelKw.Value * (1 + 1e-4));
            Assert.NotNull(v.LeistungKw);
            Assert.InRange(v.LeistungKw.Value, 0.0, v.DeckelKw.Value + 1e-9);
            Assert.True(v.DeckelstundenH > 0, "der Deckel greift nie");
            Assert.Equal(R52_TAGE_VERFEHLT, v.TageVerfehlt);
            Assert.NotNull(v.MehrwaermeMwh);
            Assert.True(v.MehrwaermeMwh > 0, "Vorheizen ohne Mehrwärme");

            // Die Spalten Vorheiz* sind geschrieben (Tab_ErgebnisGebaeude des Laufs).
            DataRow z = Zeile("SELECT * FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?", kopf);
            foreach (string s in new[]
                     {
                         VorheizenSchema.SPALTE_ERG_VERFAHREN, VorheizenSchema.SPALTE_ERG_VORHEIZZEIT, VorheizenSchema.SPALTE_ERG_BEDARF_MAX,
                         VorheizenSchema.SPALTE_ERG_BEZUG, VorheizenSchema.SPALTE_ERG_DECKEL, VorheizenSchema.SPALTE_ERG_LEISTUNG,
                         VorheizenSchema.SPALTE_ERG_TAGE_UEBER, VorheizenSchema.SPALTE_ERG_NAECHTE, VorheizenSchema.SPALTE_ERG_MEHRWAERME,
                     })
                Assert.True(z[s] != DBNull.Value, "Tab_ErgebnisGebaeude." + s + " ist leer");

            // 1051: Sollwertrampe, keine Vorheizgröße, höhere Spitze, weniger Heizwärme.
            var (gV, kopfV) = Lauf(VORLAGE);
            Assert.Null(gV.Vorheizen);
            DataRow zV = Zeile("SELECT * FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?", kopfV);
            Assert.Equal(DBNull.Value, zV[VorheizenSchema.SPALTE_ERG_VERFAHREN]);
            Assert.Equal(DBNull.Value, zV[VorheizenSchema.SPALTE_ERG_DECKEL]);
            Assert.True(g.SpitzeKw < gV.SpitzeKw, "Vorheizen senkt die Spitze nicht");
            Assert.InRange(g.HeizwaermeMwh, R52_HEIZWAERME_1065_MWH * (1 - 1e-4), R52_HEIZWAERME_1065_MWH * (1 + 1e-4));
            Assert.InRange(gV.HeizwaermeMwh, R52_HEIZWAERME_1051_MWH * (1 - 1e-4), R52_HEIZWAERME_1051_MWH * (1 + 1e-4));
            // Mehrwärme gegen 1051: rund 0,45 MWh (+1,9 %); Band ±0,01 MWh = die absolute Toleranz des Referenzvergleichs.
            double mehr = g.HeizwaermeMwh - gV.HeizwaermeMwh;
            Assert.InRange(mehr, (R52_HEIZWAERME_1065_MWH - R52_HEIZWAERME_1051_MWH) - 0.01, (R52_HEIZWAERME_1065_MWH - R52_HEIZWAERME_1051_MWH) + 0.01);

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1065: t_V {0} h (Bedarf max {1} h, Tage über t_V {2}), Φ_K,max {3:F3} kW, Deckel {4:F3} kW, Spitze {5:F3} kW " +
                "(1051: {6:F3} kW), Deckelstunden {7}, Nächte ohne Absenkung {8}, Heizwärme {9:F4} MWh (1051: {10:F4} MWh, +{11:F4} MWh, " +
                "+{12:F2} %), Mehrwärme gegen den Vorlauf {13:F4} MWh",
                v.VorheizzeitH, v.VorheizzeitBedarfMaxH, v.TageUeberVorheizzeit, v.BezugKw, v.DeckelKw, g.SpitzeKw, gV.SpitzeKw,
                v.DeckelstundenH, v.NaechteOhneAbsenkung, g.HeizwaermeMwh, gV.HeizwaermeMwh, mehr, 100.0 * mehr / gV.HeizwaermeMwh,
                v.MehrwaermeMwh));
        }

        // =====================================================================
        //  Gegen die Basis
        // =====================================================================

        /// <summary>Die <c>aggregate.csv</c> von <c>Projekt_1065</c> der einzigen Basis; <c>null</c> = keine eindeutige Basis oder ohne 1065.</summary>
        private static string BasisAggregat()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Referenzlaeufe"))) d = d.Parent;
            if (d == null) return null;
            string[] basen = Directory.GetDirectories(Path.Combine(d.FullName, "Referenzlaeufe"))
                .Where(o => Regex.IsMatch(Path.GetFileName(o), @"^\d{4}-\d{2}-\d{2}_R\d+_")).ToArray();
            if (basen.Length != 1) return null;
            string p = Path.Combine(basen[0], "Projekt_" + PROJEKT, "aggregate.csv");
            return File.Exists(p) ? p : null;
        }

        private static bool Nahe(string a, string b)
        {
            if (a == b) return true;
            if (!double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
                !double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out double y)) return false;
            double m = Math.Max(Math.Abs(x), Math.Abs(y));
            return m >= 1 ? Math.Abs(x - y) <= 1e-4 * m : Math.Abs(x - y) <= 0.01;
        }

        [Fact]
        public void Die_Vorheizschluessel_des_Laufs_stehen_wie_die_Basis()
        {
            if (!_db.Vorhanden) return;
            string basis = BasisAggregat();
            Assert.True(basis != null, "Die Basis führt Projekt_" + PROJEKT + " nicht.");
            string ziel = Path.Combine(Path.GetTempPath(), "epos-ahv7-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ziel);
            try
            {
                var log = new Protokoll();
                Assert.True(Ergebnisexport.ProjektAusfuehren(PROJEKT, ziel, log) > 0, "Der Lauf von " + PROJEKT + " scheiterte.");
                Dictionary<string, string> Vorheiz(string datei) => File.ReadAllLines(datei)
                    .Select(l => l.Split(';')).Where(t => t.Length == 2 && t[0].StartsWith("Geb[0].Vorheiz", StringComparison.Ordinal))
                    .ToDictionary(t => t[0], t => t[1], StringComparer.Ordinal);
                Dictionary<string, string> b = Vorheiz(basis), l = Vorheiz(Path.Combine(ziel, "aggregate.csv"));
                Assert.Equal(18, b.Count);
                Assert.Equal(b.Keys.OrderBy(k => k, StringComparer.Ordinal), l.Keys.OrderBy(k => k, StringComparer.Ordinal));
                foreach (var kv in b)
                    Assert.True(Nahe(kv.Value, l[kv.Key]), kv.Key + ": Basis " + kv.Value + ", Lauf " + l[kv.Key]);
                Assert.Equal("BERECHNET", b["Geb[0].Vorheizverfahren"]);
            }
            finally
            {
                Directory.Delete(ziel, true);
            }
        }
    }
}
