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
    /// <b>Wache des Referenzprojekts 1073 „Referenzprojekt Schnellaufheizung"</b> (SA4, E128, E153, Entwurf Vorheizrampe
    /// Abschnitt 7) — die Kopie des Referenzprojekts 1054 „Referenzprojekt Zonen mit Heizkreis" (Hotel in drei Zonen,
    /// Anlagenkopplung AK1, Heizkurve am Gebäude), deren Gebäude mit berechneter Vorheizzeit vorheizt und im Vorheizfenster
    /// den Raumsollwert der Heizkurve um 3 K anhebt: Verfahren <c>BERECHNET</c>, Schalter an, Überhöhung 3 K, alle übrigen
    /// Vorheizzellen leer (Toleranz 20 %, ε 1 K, Geltung Gebäude, Heizkreis einbezogen). Gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_1073_schnellaufheizung.cs</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht wie im Skript; jede übrige Fachzelle der Einstellungen, des Gebäudes und der
    /// Zonen steht wie in 1054.</item>
    /// <item><b>1073 ist das einzige Projekt mit Schnellaufheizung</b>; 1054 rechnet weiter die Sollwertrampe.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> hebt in Fensterstunden den Vorlauf über die Heizkurve an, schreibt die
    /// Ergebnisgrößen der Überhöhung am Gebäude und an den gekoppelten Zonen und hält die Kennzahlen der Basis.</item>
    /// <item><b>Gegenlauf ohne Schalter</b> (dieselbe Kopie, Schnellaufheizung aus): Die Überhöhung kürzt die Vorheizzeit,
    /// erhält die Nachtabsenkung und spart Heizwärme bei höherer Spitze.</item>
    /// <item><b>Gegen die Basis</b>: die Exportschlüssel <c>Geb[0].Vorheiz*</c> des Laufs stehen wie in der Basis.</item>
    /// </list>
    /// 1073 gehört zur Einfrierregel „gesäte Vorheizdaten eines Referenzprojekts" (samt Schnellaufheizung) und als Kopie von
    /// 1054 zu „gesäte Zonendaten" und „gesäte Auslegungsdaten der Übergabe" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SchnellaufheizungReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public SchnellaufheizungReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1073, VORLAGE = 1054;
        private const string NAME = "Referenzprojekt Schnellaufheizung";
        private const double UEBERHOEHUNG_K = 3.0;

        // Die Kennzahlen der Basis R57 (Referenzlaeufe/2026-10-17_R59_Ergebnisreihen/Projekt_1073/aggregate.csv).
        private const int R57_VORHEIZZEIT_H = 5, R57_BEDARF_MAX_H = 9, R57_UEBERHOEHUNG_H = 302;
        private const double R57_HEIZWAERME_MWH = 50.7924533;
        // Der Gegenlauf ohne Schalter (dieselbe Kopie, Schnellaufheizung aus), gemessen mit R57.
        private const int OHNE_VORHEIZZEIT_H = 9, OHNE_NAECHTE = 365;
        private const double OHNE_HEIZWAERME_MWH = 52.2802;

        /// <summary>Die sieben Vorheizspalten (Schritt 215) und die zwei der Schnellaufheizung (Schritt 217).</summary>
        private static readonly string[] VORHEIZSPALTEN =
        {
            VorheizenSchema.SPALTE_VERFAHREN, VorheizenSchema.SPALTE_VORHEIZZEIT, VorheizenSchema.SPALTE_TOLERANZ_ART,
            VorheizenSchema.SPALTE_TOLERANZ, VorheizenSchema.SPALTE_REGELGENAUIGKEIT, VorheizenSchema.SPALTE_GELTUNG,
            VorheizenSchema.SPALTE_HEIZKREIS, SchnellaufheizungSchema.SPALTE_SCHALTER, SchnellaufheizungSchema.SPALTE_UEBERHOEHUNG,
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

        private static bool Nahe(double ist, double soll)
            => Math.Abs(soll) >= 1 ? Math.Abs(ist - soll) <= 1e-4 * Math.Abs(soll) : Math.Abs(ist - soll) <= 0.01;

        // =====================================================================
        //  Die Saat
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Skript()
        {
            if (!_db.Vorhanden) return;
            DataRow p = Zeile("SELECT Projektname, Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", PROJEKT);
            Assert.Equal(NAME, T(p["Projektname"]));
            Assert.StartsWith("Referenzprojekt Schnellaufheizung: Kopie von Projekt 1054", T(p["Beschreibung"]), StringComparison.Ordinal);
            Assert.Equal(DBNull.Value, p["Kosten_Geaendert"]);

            DataRow e = Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT);
            Assert.Equal(1L, Convert.ToInt64(e["Aufheizoptimierung"], CultureInfo.InvariantCulture));
            Assert.Equal("AK1", T(e["Anlagenkopplung"]));
            Assert.Equal(VorheizenSchema.VERFAHREN_BERECHNET, T(e[VorheizenSchema.SPALTE_VERFAHREN]));
            Assert.Equal(1L, Convert.ToInt64(e[SchnellaufheizungSchema.SPALTE_SCHALTER], CultureInfo.InvariantCulture));
            Assert.Equal(UEBERHOEHUNG_K, Convert.ToDouble(e[SchnellaufheizungSchema.SPALTE_UEBERHOEHUNG], CultureInfo.InvariantCulture));
            foreach (string s in VORHEIZSPALTEN.Skip(1).Take(6)) Assert.True(e[s] == DBNull.Value, s + " = " + T(e[s]) + " statt leer");

            // Der Datenweg: Verfahren berechnet, Schnellaufheizung wirksam mit 3 K, Vorgaben des Kerns für den Rest.
            Vorheizvorgabe v = KonfigurationCtrl.VorheizvorgabeLesen(PROJEKT);
            Assert.Equal(Aufheizverfahren.Berechnet, v.Verfahren);
            Assert.True(v.SchnellaufheizungWirksam, "Schnellaufheizung nicht wirksam: " + v);
            Assert.Equal(UEBERHOEHUNG_K, v.UeberhoehungWirksamK);
            _aus.WriteLine("Vorheizvorgabe 1073: " + v);

            // Gebäude und Zonen ohne gepflegte Vorheizzeit.
            Assert.Equal(DBNull.Value, Zeile("SELECT Aufheizzeit_Manuell_H FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT)[0]);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone z JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude " +
                                  "WHERE g.ID_Projekt = ? AND z.Vorheizzeit_H IS NOT NULL", PROJEKT));
        }

        [Fact]
        public void Alle_uebrigen_Zellen_stehen_wie_in_1054()
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
            // 1054 rechnet die Sollwertrampe: alle Vorheiz- und Schnellaufheizungszellen leer.
            foreach (string s in VORHEIZSPALTEN) Assert.True(eV[s] == DBNull.Value, "1054: " + s + " = " + T(eV[s]));
            Assert.Equal(Vorheizvorgabe.Sollwertrampe, KonfigurationCtrl.VorheizvorgabeLesen(VORLAGE));

            DataRow g = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT);
            DataRow gV = Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE);
            foreach (DataColumn c in g.Table.Columns)
            {
                string s = c.ColumnName;
                if (s.IndexOf("ID", StringComparison.Ordinal) >= 0) continue;
                Assert.True(T(gV[s]) == T(g[s]), "Tab_Gebaeude." + s + ": " + T(g[s]) + " statt " + T(gV[s]));
            }

            // Zonen samt Bauteilen wie in 1054 (jede Spalte ohne Schlüssel).
            List<string> Zonen(int projekt)
            {
                var aus = new List<string>();
                DataTable z = Tabelle("SELECT z.* FROM Tab_Zone z JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude WHERE g.ID_Projekt = ? ORDER BY z.Rang", projekt);
                foreach (DataRow r in z.Rows)
                {
                    aus.Add(string.Join("|", z.Columns.Cast<DataColumn>().Where(c => c.ColumnName.IndexOf("ID", StringComparison.Ordinal) < 0)
                                             .Select(c => c.ColumnName + "=" + T(r[c]))));
                    DataTable b = Tabelle("SELECT * FROM Tab_Bauteil WHERE ID_Zone = ? ORDER BY Rang", r["ID"]);
                    foreach (DataRow x in b.Rows)
                        aus.Add("  " + string.Join("|", b.Columns.Cast<DataColumn>().Where(c => c.ColumnName.IndexOf("ID", StringComparison.Ordinal) < 0)
                                                        .Select(c => c.ColumnName + "=" + T(x[c]))));
                }
                return aus;
            }
            List<string> zonen = Zonen(PROJEKT);
            Assert.Equal(3, zonen.Count(z => !z.StartsWith("  ", StringComparison.Ordinal)));
            Assert.Equal(Zonen(VORLAGE), zonen);
        }

        [Fact]
        public void Das_Projekt_ist_das_einzige_mit_Schnellaufheizung()
        {
            if (!_db.Vorhanden) return;
            string bedingung = SchnellaufheizungSchema.SPALTE_SCHALTER + " IS NOT NULL OR " + SchnellaufheizungSchema.SPALTE_UEBERHOEHUNG + " IS NOT NULL";
            Assert.Equal((long)PROJEKT, Zahl("SELECT ID_Projekt FROM Tab_Einstellungen WHERE " + bedingung));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE " + bedingung));
            // Mit Vorheizverfahren rechnen allein 1065 (ohne Heizkreis) und 1073 (mit Heizkreis).
            Assert.Equal(new[] { 1065L, (long)PROJEKT },
                         Tabelle("SELECT ID_Projekt FROM Tab_Einstellungen WHERE " + VorheizenSchema.SPALTE_VERFAHREN + " IS NOT NULL ORDER BY 1")
                             .Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r[0], CultureInfo.InvariantCulture)).ToArray());
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        [Fact]
        public void Ein_Lauf_hebt_den_Vorlauf_im_Vorheizfenster_an_und_spart_gegen_den_Lauf_ohne_Schalter()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            int kopf = new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf " + PROJEKT + " gescheitert: " + fehler);
            ErgebnisGebaeudeModel g = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Gebaeude);
            Vorheizkennzahlen v = g.Vorheizen;
            Assert.NotNull(v);
            Assert.Equal(VorheizenSchema.VERFAHREN_BERECHNET, v.Verfahren);
            // Die gekoppelten Zonen werden bemessen (Vorheizen), nicht mehr nur gekoppelt wie in 1054.
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, g.AufheizZustand);

            // Die Überhöhung greift: 3 K, in Fensterstunden mit angehobenem Vorlauf über dem Mittel des Kreises.
            Assert.Equal(UEBERHOEHUNG_K, v.UeberhoehungK);
            Assert.Equal(R57_UEBERHOEHUNG_H, v.UeberhoehungH);
            Assert.NotNull(v.UeberhoehungVorlaufMaxC);
            Assert.NotNull(g.VorlaufMittelC);
            Assert.True(v.UeberhoehungVorlaufMaxC > g.VorlaufMittelC, "Der überhöhte Vorlauf liegt nicht über dem Mittel");
            Assert.Equal(0, v.UeberhoehungSperrzeitH ?? 0);
            Assert.Equal(R57_VORHEIZZEIT_H, v.VorheizzeitH);
            Assert.Equal(R57_BEDARF_MAX_H, v.VorheizzeitBedarfMaxH);
            Assert.Equal(0, v.NaechteOhneAbsenkung);
            Assert.True(Nahe(g.HeizwaermeMwh, R57_HEIZWAERME_MWH), "Heizwärme " + g.HeizwaermeMwh + " statt " + R57_HEIZWAERME_MWH);

            // Die Spalten der Überhöhung sind geschrieben - am Gebäude und an den gekoppelten (beheizten) Zonen.
            DataRow z = Zeile("SELECT * FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?", kopf);
            foreach (string s in new[] { SchnellaufheizungSchema.SPALTE_ERG_UEBERHOEHUNG, SchnellaufheizungSchema.SPALTE_ERG_STUNDEN,
                                         SchnellaufheizungSchema.SPALTE_ERG_VORLAUF_MAX })
                Assert.True(z[s] != DBNull.Value, "Tab_ErgebnisGebaeude." + s + " ist leer");
            long zonenMit = Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE ID_ErgebnisGebaeude IN (SELECT ID FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?) " +
                                 "AND " + SchnellaufheizungSchema.SPALTE_ERG_STUNDEN + " > 0", kopf);
            Assert.True(zonenMit > 0, "Keine Zone mit überhöhten Stunden");

            // Gegenlauf: dieselbe Kopie ohne Schalter (Testkopie, danach zurückgesetzt).
            Aufheizvorgabe an = VorheizAk3Tests.ProjektvorgabeAn(PROJEKT);
            SimulationRunner r = VorheizAk3Tests.Projektlauf(PROJEKT, an with { Vorheizen = an.Vorheizen.MitSchnellaufheizung(false) });
            List<GebaeudeModellErgebnis> alle = r.simulation_Waermebedarf.GebaeudeErgebnisse.Alle.Where(x => x != null).ToList();
            Vorheizgebaeude ohne = Assert.Single(alle.Where(x => x.Vorheizen != null)).Vorheizen;
            double qOhne = alle.Sum(x => x.HeizlastW.Sum()) / 1e6;
            Assert.Equal(0, ohne.UeberhoehungStundenH);
            Assert.Equal(OHNE_VORHEIZZEIT_H, ohne.VorheizzeitMaxH);
            Assert.Equal(OHNE_NAECHTE, ohne.NaechteOhneAbsenkung);
            Assert.True(Nahe(qOhne, OHNE_HEIZWAERME_MWH), "Heizwärme ohne Schalter " + qOhne + " statt " + OHNE_HEIZWAERME_MWH);
            // Die Richtung der Wirkung: kürzere Vorheizzeit, Nachtabsenkung erhalten, weniger Heizwärme.
            Assert.True(v.VorheizzeitH < ohne.VorheizzeitMaxH, "Die Überhöhung kürzt die Vorheizzeit nicht");
            Assert.True(v.NaechteOhneAbsenkung < ohne.NaechteOhneAbsenkung, "Die Überhöhung erhält die Nachtabsenkung nicht");
            Assert.True(g.HeizwaermeMwh < qOhne, "Die Überhöhung spart keine Heizwärme");
            // Der Preis: Die überhöhte Heizkurve hebt die Spitze des Gebäudes an.
            Assert.True(g.SpitzeKw > ohne.SpitzeW / 1000.0, "Die Überhöhung hebt die Spitze nicht");

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1073: t_V {0} h (Bedarf max {1} h), Nächte ohne Absenkung {2}, überhöht {3} h (Vorlauf max {4:F2} °C, Vorlauf Mittel {5:F2} °C, " +
                "Vorlaufgrenze {6} h, wirkungslos {7} h), Spitze {8:F3} kW, Heizwärme {9:F4} MWh, Zonen mit Überhöhung {10} | ohne Schalter: " +
                "t_V {11} h (Bedarf max {12} h), Nächte ohne Absenkung {13}, Spitze {14:F3} kW, Heizwärme {15:F4} MWh ({16:+0.00;-0.00} %)",
                v.VorheizzeitH, v.VorheizzeitBedarfMaxH, v.NaechteOhneAbsenkung, v.UeberhoehungH, v.UeberhoehungVorlaufMaxC, g.VorlaufMittelC,
                v.VorlaufgrenzeH, v.UeberhoehungWirkungslosH, g.SpitzeKw, g.HeizwaermeMwh, zonenMit,
                ohne.VorheizzeitMaxH, ohne.BedarfMaxH, ohne.NaechteOhneAbsenkung, ohne.SpitzeW / 1000.0, qOhne, 100.0 * (g.HeizwaermeMwh - qOhne) / qOhne));
        }

        // =====================================================================
        //  Gegen die Basis
        // =====================================================================

        /// <summary>Die <c>aggregate.csv</c> von <c>Projekt_1073</c> der einzigen Basis; <c>null</c> = keine eindeutige Basis oder ohne 1073.</summary>
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

        private static bool NaheText(string a, string b)
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
            string ziel = Path.Combine(Path.GetTempPath(), "epos-sa4-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ziel);
            try
            {
                var log = new Protokoll();
                Assert.True(Ergebnisexport.ProjektAusfuehren(PROJEKT, ziel, log) > 0, "Der Lauf von " + PROJEKT + " scheiterte.");
                Dictionary<string, string> Vorheiz(string datei) => File.ReadAllLines(datei)
                    .Select(l => l.Split(';')).Where(t => t.Length == 2 && t[0].StartsWith("Geb[0].Vorheiz", StringComparison.Ordinal))
                    .ToDictionary(t => t[0], t => t[1], StringComparer.Ordinal);
                Dictionary<string, string> b = Vorheiz(basis), l = Vorheiz(Path.Combine(ziel, "aggregate.csv"));
                Assert.True(b.Keys.Any(k => k.IndexOf("Ueberhoehung", StringComparison.Ordinal) >= 0), "Die Basis führt keinen Schlüssel der Überhöhung.");
                Assert.Equal(b.Keys.OrderBy(k => k, StringComparer.Ordinal), l.Keys.OrderBy(k => k, StringComparer.Ordinal));
                foreach (var kv in b)
                    Assert.True(NaheText(kv.Value, l[kv.Key]), kv.Key + ": Basis " + kv.Value + ", Lauf " + l[kv.Key]);
                Assert.Equal("BERECHNET", b["Geb[0].Vorheizverfahren"]);
            }
            finally
            {
                Directory.Delete(ziel, true);
            }
        }
    }
}
