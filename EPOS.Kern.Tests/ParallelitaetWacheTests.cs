using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die PARALLELITÄT der fünf plattformfreien Projekte
    /// (Auftrag #232, Anwenderentscheid „#231: Empfehlung umsetzen" vom 12.09.2026;
    /// <c>EPOS.UI</c> kam mit Auftrag <b>#240</b> dazu).
    ///
    /// <para><b>Die Regel.</b> In <c>EPOS.Kern</c>, <c>SpeicherEngine</c>,
    /// <c>KiKern</c>, <c>EPOS.UI.Daten</c> und <c>EPOS.UI</c> entsteht ein Arbeitsfaden
    /// ausschließlich über <c>SpeicherEngine.Kulturweitergabe</c>. Ein nacktes
    /// <c>Parallel.For</c>, <c>Parallel.ForEach</c>, <c>Parallel.Invoke</c>,
    /// <c>Task.Run</c>, <c>new Thread</c>, <c>ThreadPool.QueueUserWorkItem</c> oder
    /// <c>AsParallel()</c> fällt hier auf — in <c>.cs</c> UND in <c>.razor</c>,
    /// denn in <c>EPOS.UI</c> steht der Programmtext im <c>@code</c>-Block.</para>
    ///
    /// <para><b>Warum.</b> Ein Faden ohne eigene Kultur liest bei JEDEM Zugriff den
    /// prozessweiten <c>CultureInfo.DefaultThreadCurrentCulture</c> beziehungsweise
    /// <c>…UICulture</c> — und der ist veränderlich. Im Produkt schaltet ihn der
    /// Sprachwechsel um, im Testprozess über 50 Testklassen bei jedem Konstruktor- und
    /// <c>Dispose</c>-Aufruf. Fällt eine solche Umschaltung mitten in eine laufende
    /// Rechnung, wechselt ein Arbeitsfaden mitten im Lauf die Sprache — Etikett und
    /// Zahlenbild passen dann nicht mehr zusammen (Befund #231, Windows-Lauf 319).
    /// Die Vorrichtung erfasst die Kultur des Aufrufers EINMAL am Einstieg und legt
    /// sie je Arbeitspaket auf den Arbeitsfaden; der Beleg dafür steht in
    /// <see cref="KulturweitergabeTests"/>.</para>
    ///
    /// <para><b>Warum <c>EPOS.UI</c> mit #240 dazukam.</b> #232 hatte es ausgenommen,
    /// mit der Begründung „dort läuft alles am Bedienfaden der WebView" — die zwei
    /// <c>Task.Run</c> des <c>ProjektTransferDialog</c> widerlegten sie: Export und
    /// Import lesen und schreiben die ganze Datenbank und laufen minutenlang im
    /// Hintergrund, also genau auf einem Faden ohne eigene Kultur. Sie gehen seither
    /// über <c>Kulturweitergabe.Starten</c>, und die Ausnahme ist gefallen; die
    /// Trefferliste ist leer.</para>
    ///
    /// <para><b>Was der Wächter NICHT prüft.</b> Die Testprojekte — die Gegenprobe in
    /// <see cref="KulturweitergabeTests"/> BRAUCHT ein nacktes <c>Parallel.For</c>,
    /// sonst bewiese sie nichts.</para>
    /// </summary>
    public class ParallelitaetWacheTests
    {
        /// <summary>Die fünf Projekte, über die der Wächter läuft.</summary>
        private static readonly string[] Projekte =
        {
            "EPOS.Kern", "SpeicherEngine", "KiKern", "EPOS.UI.Daten", "EPOS.UI",
        };

        /// <summary>
        /// Die Dateiarten mit Programmtext. <c>.razor</c> gehört dazu, seit
        /// <c>EPOS.UI</c> im Bestand steht (Auftrag #240): Dort steht der
        /// Programmtext im <c>@code</c>-Block, und genau darin standen die zwei
        /// <c>Task.Run</c> des Projekttransfers.
        /// </summary>
        private static readonly string[] Endungen = { "*.cs", "*.razor" };

        /// <summary>
        /// Die einzige erlaubte Ausnahme: die Vorrichtung selbst. Ihre nackten Aufrufe
        /// SIND die Vorrichtung, nicht deren Umgehung — dieselbe Ausnahme, die
        /// <c>KulturwaechterTests</c> der <c>Kulturvorrichtung</c> gewährt.
        /// </summary>
        private const string Vorrichtung = "Kulturweitergabe.cs";

        /// <summary>
        /// Die sieben Wege, auf denen im .NET-Bestand ein Arbeitsfaden entsteht. Je
        /// Muster ein Name für die Meldung — wer einen Fund sieht, soll lesen können,
        /// was er gefunden hat.
        /// </summary>
        private static readonly (string Name, Regex Muster)[] Wege =
        {
            ("Parallel.For",                 new Regex(@"\bParallel\s*\.\s*For\s*\(", RegexOptions.Compiled)),
            ("Parallel.ForEach",             new Regex(@"\bParallel\s*\.\s*ForEach\s*\(", RegexOptions.Compiled)),
            ("Parallel.Invoke",              new Regex(@"\bParallel\s*\.\s*Invoke\s*\(", RegexOptions.Compiled)),
            ("Task.Run",                     new Regex(@"\bTask\s*\.\s*Run\s*\(", RegexOptions.Compiled)),
            ("new Thread",                   new Regex(@"\bnew\s+Thread\s*\(", RegexOptions.Compiled)),
            ("ThreadPool.QueueUserWorkItem", new Regex(@"\bThreadPool\s*\.\s*QueueUserWorkItem\s*\(", RegexOptions.Compiled)),
            ("AsParallel()",                 new Regex(@"\.\s*AsParallel\s*\(\s*\)", RegexOptions.Compiled)),
        };

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// Kein nackter Fadenstart in den vier plattformfreien Projekten — die
        /// Vorrichtung ausgenommen.
        /// </summary>
        [Fact]
        public void Kein_nackter_Fadenstart_ausserhalb_der_Kulturweitergabe()
        {
            var funde = new List<string>();

            foreach (string datei in Quelldateien())
            {
                string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                {
                    if (IstKommentar(zeilen[i])) continue;

                    foreach (var weg in Wege)
                    {
                        if (!weg.Muster.IsMatch(zeilen[i])) continue;
                        funde.Add(Kurzname(datei) + ":" + (i + 1) + "  (" + weg.Name + ")  " + zeilen[i].Trim());
                    }
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Stellen starten einen Arbeitsfaden an der Kulturweitergabe vorbei " +
                "(Auftrag #232). Ein Faden ohne eigene Kultur liest den VERAENDERLICHEN " +
                "prozessweiten Vorgabewert und kann mitten in der Rechnung die Sprache " +
                "wechseln - zu nehmen ist SpeicherEngine.Kulturweitergabe (For, ForEach, " +
                "Starten, StartenAsync):\n" + string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Alle sieben Wege treffen — in den Schreibweisen
        /// des Bestands —, und die Stellen, die NICHT gemeint sind, treffen nicht.
        /// Ohne diesen Fall wäre der Wächter stumm, sobald jemand anders schreibt.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_alle_sieben_Wege()
        {
            Assert.Matches(Muster("Parallel.For"), "            Parallel.For(0, n, po, index =>");
            Assert.Matches(Muster("Parallel.ForEach"), "Parallel.ForEach(quelle, arbeit);");
            Assert.Matches(Muster("Parallel.Invoke"), "System.Threading.Tasks.Parallel.Invoke(a, b);");
            Assert.Matches(Muster("Task.Run"), "            return Task.Run(arbeit);");
            Assert.Matches(Muster("new Thread"), "var t = new Thread(Vorbereiten);");
            Assert.Matches(Muster("ThreadPool.QueueUserWorkItem"), "ThreadPool.QueueUserWorkItem(_ => Tu());");
            Assert.Matches(Muster("AsParallel()"), "var x = liste.AsParallel().Select(F);");

            // Die Huellen selbst sind KEIN Fund.
            Assert.DoesNotMatch(Muster("Parallel.For"), "Kulturweitergabe.For(0, n, po, index =>");
            Assert.DoesNotMatch(Muster("Parallel.ForEach"), "Kulturweitergabe.ForEach(quelle, po, arbeit);");
            Assert.DoesNotMatch(Muster("Task.Run"), "return Kulturweitergabe.Starten(arbeit);");

            // Parallel.ForEach ist kein Parallel.For, und ein Lesezugriff kein Start.
            Assert.DoesNotMatch(Muster("Parallel.For"), "Parallel.ForEach(quelle, arbeit);");
            Assert.DoesNotMatch(Muster("new Thread"), "Thread faden = Thread.CurrentThread;");
            Assert.DoesNotMatch(Muster("ThreadPool.QueueUserWorkItem"), "ThreadPool.SetMinThreads(4, 4);");
            Assert.DoesNotMatch(Muster("AsParallel()"), "var x = liste.AsParallel(grad);");
        }

        /// <summary>
        /// <b>Gegenprobe zur Kommentarschonung:</b> Die Klassenköpfe nennen
        /// <c>Task.Run(…)</c> und <c>Parallel.For(…)</c> absichtlich; geprüft wird nur,
        /// was der Übersetzer sieht.
        /// </summary>
        [Fact]
        public void Ein_Kommentar_zaehlt_nicht_als_Fadenstart()
        {
            Assert.True(IstKommentar("        // return Task.Run(arbeit);"));
            Assert.True(IstKommentar("        /// <see cref=\"Parallel.For(int,int,ParallelOptions,Action{int})\"/>"));
            Assert.True(IstKommentar("     * Task.Run(arbeit)"));
            Assert.True(IstKommentar("        /* Parallel.Invoke(a, b); */"));
            Assert.False(IstKommentar("            return Task.Run(arbeit);"));
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene
        /// Dateien aus allen VIER Projekten — ein leerer oder halber Bestand liefe
        /// sonst grün durch, ohne je etwas geprüft zu haben.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_Bestand_aller_vier_Projekte()
        {
            string[] dateien = Quelldateien();
            Assert.True(dateien.Length > 400, "Nur " + dateien.Length + " Quelldateien gefunden.");

            foreach (string projekt in Projekte)
            {
                string marke = Path.DirectorySeparatorChar + projekt + Path.DirectorySeparatorChar;
                Assert.True(dateien.Any(d => d.Contains(marke, StringComparison.Ordinal)),
                            "Aus " + projekt + " steht keine Datei im geprueften Bestand.");
            }

            // Vier bekannte umgestellte Stellen - sie belegen, dass der Bestand die
            // wirklich betroffenen Dateien enthaelt. Die vierte ist eine RAZOR-Datei
            // (Auftrag #240): Ohne sie liefe der Waechter ueber EPOS.UI, ohne den
            // Programmtext dieses Projekts je zu sehen.
            Assert.Contains(dateien, d => Path.GetFileName(d) == "SpeicherOptimierer.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "KiAusfuehrung.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "StromspeicherAuslegungHuelle.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "ProjektTransferDialog.razor");

            // Und der Bestand traegt wirklich Razor-Dateien in nennenswerter Zahl.
            Assert.True(dateien.Count(d => d.EndsWith(".razor", StringComparison.Ordinal)) > 100,
                        "Der gepruefte Bestand fuehrt kaum Razor-Dateien - dann prueft der " +
                        "Waechter in EPOS.UI praktisch nichts (Auftrag #240).");
        }

        /// <summary>
        /// <b>Gegenprobe zur Ausnahme:</b> Die Vorrichtung steht NICHT im geprüften
        /// Bestand, es gibt sie aber, und sie trägt wirklich die nackten Aufrufe. Eine
        /// Ausnahme, die ins Leere zeigt, wäre eine stille Lücke — und ohne die nackten
        /// Aufrufe darin hätte der Wächter nichts mehr auszunehmen.
        /// </summary>
        [Fact]
        public void Die_Vorrichtung_ist_ausgenommen_und_traegt_die_nackten_Aufrufe()
        {
            Assert.DoesNotContain(Quelldateien(), d => Path.GetFileName(d) == Vorrichtung);

            string pfad = Path.Combine(Arbeitsbaum(), "SpeicherEngine", Vorrichtung);
            Assert.True(File.Exists(pfad), "Die Vorrichtung gibt es nicht mehr: " + pfad);

            string text = File.ReadAllText(pfad);
            Assert.Matches(Muster("Parallel.For"), text);
            Assert.Matches(Muster("Parallel.ForEach"), text);
            Assert.Matches(Muster("Task.Run"), text);
        }

        /// <summary>
        /// <b>Gegenprobe zum Wächter selbst:</b> Ein eingeschmuggelter nackter Aufruf
        /// wird gefunden. Geprüft wird die Kernschleife an einem Text statt an einer
        /// Datei — ohne diesen Fall bliebe offen, ob der Wächter überhaupt anschlägt.
        /// </summary>
        [Fact]
        public void Ein_eingeschmuggelter_nackter_Aufruf_faellt_auf()
        {
            const string sauber =
                "class X { void M() { Kulturweitergabe.For(0, 10, null, i => Tu(i)); } }";
            Assert.Empty(Funde(sauber));

            const string leck =
                "class X { void M() { Parallel.For(0, 10, i => Tu(i)); } }";
            List<string> funde = Funde(leck);
            Assert.Single(funde);
            Assert.Equal("Parallel.For", funde[0]);
        }

        /// <summary>Die Kernschleife ohne Datei — gegenprobenfähig.</summary>
        private static List<string> Funde(string text)
        {
            var funde = new List<string>();
            foreach (string zeile in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (IstKommentar(zeile)) continue;
                foreach (var weg in Wege)
                    if (weg.Muster.IsMatch(zeile)) funde.Add(weg.Name);
            }
            return funde;
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        private static Regex Muster(string name)
            => Wege.Single(w => w.Name == name).Muster;

        /// <summary>
        /// Steht die Zeile in einem Kommentar? Dieselbe Vereinfachung wie in den
        /// übrigen Quelltext-Wächtern des Projekts (<c>EinheitenWacheTests</c>).
        /// </summary>
        private static bool IstKommentar(string zeile)
        {
            string s = zeile.TrimStart();
            return s.StartsWith("//", StringComparison.Ordinal)
                || s.StartsWith("*", StringComparison.Ordinal)
                || s.StartsWith("/*", StringComparison.Ordinal)
                || s.StartsWith("@*", StringComparison.Ordinal);
        }

        /// <summary>
        /// Alle <c>.cs</c>- und <c>.razor</c>-Dateien der fünf Projekte, ohne Bauordner
        /// und ohne die Vorrichtung.
        /// </summary>
        private static string[] Quelldateien()
        {
            string wurzel = Arbeitsbaum();
            var dateien = new List<string>();

            foreach (string projekt in Projekte)
            {
                string ordner = Path.Combine(wurzel, projekt);
                Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);
                foreach (string endung in Endungen)
                    dateien.AddRange(Directory.GetFiles(ordner, endung, SearchOption.AllDirectories));
            }

            return dateien
                .Where(OhneBauordner)
                .Where(d => Path.GetFileName(d) != Vorrichtung)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool OhneBauordner(string pfad)
        {
            char t = Path.DirectorySeparatorChar;
            return pfad.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0
                && pfad.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0;
        }

        /// <summary>Der Pfad ab der Wurzel des Arbeitsbaums — das nennt die Meldung.</summary>
        private static string Kurzname(string datei)
        {
            string wurzel = Arbeitsbaum();
            return datei.StartsWith(wurzel, StringComparison.Ordinal)
                 ? datei.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar)
                       .Replace(Path.DirectorySeparatorChar, '/')
                 : datei;
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>KulturwaechterTests</c>.</summary>
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
