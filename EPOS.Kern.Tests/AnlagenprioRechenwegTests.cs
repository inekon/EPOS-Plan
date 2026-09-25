using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Rechenweg-Leser ordnen die Anlagen nach derselben Regel wie Hydraulikbild und
    /// Erzeugerkarten: <see cref="Ladeordnung.SqlAnlagenprio"/> — gepflegte Priorität
    /// zuerst, eine Anlage ohne Priorität (NULL oder 0) hinten, bei Gleichstand die ID
    /// (Konzept Wirtschaftlichkeit § 6.3 Nr. 18, Regel „99" des Hydraulikbilds HB1).
    ///
    /// <para><b>Warum eine Quelltext-Wache.</b> Ein <c>ORDER BY Prioritaet</c> stellt in
    /// SQLite NULL VOR die 1; die ungepflegte Anlage wäre dann Modul 1 im Ergebnis,
    /// während Karte und Bild die gepflegte vorn zeigen. Kein Referenzprojekt rechnet
    /// daran anders — nur der Index der Module wechselt —, der Referenzlauf bemerkt einen
    /// Rückfall also allenfalls als vertauschte Modulnummer. Die Wache nennt die Stelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenprioRechenwegTests
    {
        /// <summary>Die acht Rechenweg-Leser: Datei (repo-relativ) und Methodenkopf.</summary>
        private static readonly (string Datei, string Kopf)[] Leser =
        {
            ("EPOS.Kern/Allgemein/Simulation/SimulationControl.cs", "private void WP_Liste_Laden()"),
            ("EPOS.Kern/Allgemein/Simulation/SimulationControl.cs", "private void SPK_Liste_Laden()"),
            ("EPOS.Kern/Allgemein/Simulation/SimulationControl.cs", "private void Solar_Liste_Laden()"),
            ("EPOS.Kern/Allgemein/Simulation/SimulationControl.cs", "private void BHKW_Liste_Laden()"),
            ("EPOS.Kern/Allgemein/Simulation/SimulationControl.cs", "private void QuellbezuegeAufbauen("),
            ("EPOS.Kern/Allgemein/Simulation/SimulationControl.cs", "private List<int> SenkenPufferDerAnlagen()"),
            ("EPOS.Kern/Allgemein/Simulation/WaermesenkeClass.cs", "public static List<Senkenzuordnung> SenkenLaden(int idProjekt)"),
            ("EPOS.Kern/Allgemein/Simulation/WaermesenkeClass.cs", "public static List<Senkenliste> SenkenlistenLaden(int idProjekt)"),
        };

        [Fact]
        public void Die_acht_Rechenweg_Leser_ordnen_nach_Ladeordnung_SqlAnlagenprio()
        {
            string wurzel = Arbeitsbaum();
            var fehler = new List<string>();

            foreach ((string datei, string kopf) in Leser)
            {
                string sql = AbfrageDesLesers(Path.Combine(wurzel, datei), kopf, out string grund);
                if (sql == null) { fehler.Add(datei + " · " + kopf + ": " + grund); continue; }

                if (!sql.Contains("\"ORDER BY \" + Ladeordnung.SqlAnlagenprio(null) + \", ID\""))
                    fehler.Add(datei + " · " + kopf + ": die Abfrage ordnet nicht nach " +
                               "Ladeordnung.SqlAnlagenprio(null), ID");
                if (sql.Contains("ORDER BY Prioritaet"))
                    fehler.Add(datei + " · " + kopf + ": ORDER BY Prioritaet stellt NULL vor die 1");
            }

            Assert.True(fehler.Count == 0,
                "Ein Rechenweg-Leser weicht von der Anlagenordnung des Hydraulikbilds ab:\n" +
                string.Join("\n", fehler));
        }

        /// <summary>
        /// Gegenprobe der Wache: Eine Abfrage mit <c>ORDER BY Prioritaet, ID</c> oder ganz
        /// ohne Ordnung fällt auf, die gültige Schreibweise nicht.
        /// </summary>
        [Fact]
        public void Die_Wache_erkennt_eine_Abfrage_ohne_die_Regel()
        {
            string alt =
                "        private void Probe()\r\n        {\r\n" +
                "            DataTable dt = StilleDb.Tabelle(\r\n" +
                "                \"SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? \" +\r\n" +
                "                \"ORDER BY Prioritaet, ID\",\r\n" +
                "                StilleDb.Par(\"@proj\", DbParamTyp.Integer, 1));\r\n        }\r\n";
            string neu = alt.Replace("\"ORDER BY Prioritaet, ID\"",
                                     "\"ORDER BY \" + Ladeordnung.SqlAnlagenprio(null) + \", ID\"");

            string datei = Path.Combine(Path.GetTempPath(), "anlagenprio-probe-" + Guid.NewGuid().ToString("N") + ".cs");
            try
            {
                File.WriteAllText(datei, alt);
                string sqlAlt = AbfrageDesLesers(datei, "private void Probe()", out _);
                File.WriteAllText(datei, neu);
                string sqlNeu = AbfrageDesLesers(datei, "private void Probe()", out _);

                Assert.Contains("ORDER BY Prioritaet", sqlAlt);
                Assert.DoesNotContain("Ladeordnung.SqlAnlagenprio", sqlAlt);
                Assert.Contains("\"ORDER BY \" + Ladeordnung.SqlAnlagenprio(null) + \", ID\"", sqlNeu);
            }
            finally
            {
                File.Delete(datei);
            }
        }

        /// <summary>
        /// Projekt 1042: Wärmepumpe 14817 trägt Priorität 1, Wärmepumpe 14818 keine. Der
        /// Rechenweg lädt 14817 zuerst — mit <c>ORDER BY Prioritaet</c> stand 14818 vorn.
        /// </summary>
        [Fact]
        public void Eine_Anlage_ohne_Prioritaet_steht_hinter_der_mit_Prioritaet_1()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> senken = WaermesenkeClass.SenkenLaden(1042).Select(s => s.AnlagenID).ToList();
            List<int> listen = WaermesenkeClass.SenkenlistenLaden(1042).Select(s => s.AnlagenID).ToList();

            foreach (List<int> reihe in new[] { senken, listen })
            {
                Assert.Equal(14817, reihe[0]);
                Assert.True(reihe.IndexOf(14818) > reihe.IndexOf(14817),
                    "Die ungepflegte Wärmepumpe 14818 steht vor 14817: " + string.Join(", ", reihe));
            }
            Assert.Equal(senken, listen);
        }

        /// <summary>
        /// Synthetisch auf der Kopie: Priorität 0 gilt wie NULL als ungepflegt, und eine
        /// gepflegte Priorität schlägt die kleinere ID — hier der Kessel 14854 mit
        /// Priorität 1 vor beiden Wärmepumpen, deren eine 0 trägt und deren andere keine.
        /// </summary>
        [Fact]
        public void Prioritaet_0_gilt_als_ungepflegt_und_die_gepflegte_Anlage_steht_vorn()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Prioritaet = 0 WHERE ID = ?",
                                      new DbParam("@id", 14817));
            DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Prioritaet = 1 WHERE ID = ?",
                                      new DbParam("@id", 14854));

            List<int> reihe = WaermesenkeClass.SenkenLaden(1042).Select(s => s.AnlagenID).ToList();

            Assert.Equal(new[] { 14854, 14817, 14818 }, reihe);
        }

        /// <summary>
        /// Der SQL-Teil des ersten <c>StilleDb.Tabelle(</c>-Aufrufs nach dem Methodenkopf —
        /// vom Aufruf bis zum ersten <c>StilleDb.Par(</c>. <c>null</c> mit Grund, wenn
        /// Kopf oder Aufruf fehlen.
        /// </summary>
        private static string AbfrageDesLesers(string pfad, string kopf, out string grund)
        {
            grund = null;
            if (!File.Exists(pfad)) { grund = "Datei fehlt"; return null; }

            string text = File.ReadAllText(pfad);
            int k = text.IndexOf(kopf, StringComparison.Ordinal);
            if (k < 0) { grund = "Methodenkopf nicht gefunden"; return null; }

            int a = text.IndexOf("StilleDb.Tabelle(", k, StringComparison.Ordinal);
            if (a < 0) { grund = "kein StilleDb.Tabelle-Aufruf"; return null; }

            int e = text.IndexOf("StilleDb.Par(", a, StringComparison.Ordinal);
            if (e < 0) { grund = "kein StilleDb.Par nach der Abfrage"; return null; }

            return text.Substring(a, e - a);
        }

        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
