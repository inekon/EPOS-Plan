using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die Entsorgung der Arbeitskopien (<see cref="TestDatenbank"/>).
    ///
    /// <para><b>Was schiefging.</b> Am 23.09.2026 lief beim vollen Test-Gate Laufwerk C:
    /// voll, 199 Fälle fielen mit Schreibfehlern. Unter <c>%TEMP%</c> lagen 1 170 Ordner
    /// <c>epos-kerntest-*</c> mit je einer Kopie der Testdatenbank, zusammen 77 GB. Ursache:
    /// Vier Testklassen hielten die Vorrichtung als Feld
    /// (<c>private readonly TestDatenbank _db = new TestDatenbank();</c>), trugen aber kein
    /// <see cref="IDisposable"/>. xunit legt je Testfall eine neue Instanz der Klasse an und
    /// entsorgt sie nur, wenn sie <c>IDisposable</c> ist — so blieb je Testfall eine Kopie
    /// liegen, 40 je Lauf. Nebenbei stand <c>DataRepository.PfadUeberschreibung</c> danach
    /// für den Rest des Laufs auf der letzten dieser Kopien.</para>
    ///
    /// <para><b>Die Regel.</b> Jedes <c>new TestDatenbank()</c> steht in einer
    /// <c>using</c>-Anweisung oder wird einem Namen zugewiesen, dessen <c>Dispose()</c> in
    /// derselben Datei gerufen wird. Jede Klasse, die eine Arbeitskopie im Feld hält, trägt
    /// <c>IDisposable</c> (oder <c>IAsyncLifetime</c>) — es sei denn, xunit reicht sie ihr als
    /// Klassenvorrichtung (<c>IClassFixture&lt;TestDatenbank&gt;</c>) und entsorgt sie
    /// selbst.</para>
    ///
    /// <para>Wie <see cref="DiensteSammlungTests"/> liest der Wächter die Quelldateien, dazu
    /// die Typen der Assembly. Ein vergessenes Dispose soll auffallen, BEVOR die Platte voll
    /// ist — und nicht erst an einem Fall, der Wochen später mit „disk full" fällt.</para>
    /// </summary>
    public class TestDatenbankEntsorgungWacheTests
    {
        /// <summary>Eine neue Arbeitskopie.</summary>
        private static readonly Regex Neu =
            new Regex(@"\bnew\s+TestDatenbank\s*\(", RegexOptions.Compiled);

        /// <summary>
        /// … in einer <c>using</c>-Deklaration oder -Anweisung:
        /// <c>using var db = new TestDatenbank();</c>, <c>using (TestDatenbank db = new TestDatenbank())</c>.
        /// </summary>
        private static readonly Regex MitUsing =
            new Regex(@"\busing\s*(\(\s*)?(var|TestDatenbank)\s+\w+\s*=\s*new\s+TestDatenbank\s*\(",
                      RegexOptions.Compiled);

        /// <summary>… einem Namen zugewiesen, einem Feld oder einer lokalen Variablen.</summary>
        private static readonly Regex Zuweisung =
            new Regex(@"\b(?<name>\w+)\s*=\s*new\s+TestDatenbank\s*\(", RegexOptions.Compiled);

        // =====================================================================
        //  Die Wächter
        // =====================================================================

        [Fact]
        public void Jede_Arbeitskopie_wird_entsorgt()
        {
            var funde = new List<string>();
            foreach (string datei in Quelldateien())
            {
                if (Path.GetFileName(datei) == nameof(TestDatenbankEntsorgungWacheTests) + ".cs") continue;
                funde.AddRange(Unentsorgte(Path.GetFileName(datei), File.ReadAllText(datei)));
            }

            Assert.True(funde.Count == 0,
                "Diese Stellen legen eine Arbeitskopie der Testdatenbank an, die nie entsorgt wird — " +
                "weder in einer using-Anweisung noch über ein Dispose() ihres Namens in derselben Datei. " +
                "Jede solche Stelle lässt je Testfall einen Ordner epos-kerntest-* von rund 65 MB unter " +
                "%TEMP% liegen (Befund 23.09.2026: 77 GB, Platte voll).\n" +
                string.Join("\n", funde));
        }

        [Fact]
        public void Jede_Klasse_mit_einer_Arbeitskopie_im_Feld_ist_entsorgbar()
        {
            List<string> funde = OhneEntsorgung(Testtypen().Where(t => t.DeclaringType != typeof(TestDatenbankEntsorgungWacheTests)))
                                     .Select(t => t.FullName)
                                     .OrderBy(n => n, StringComparer.Ordinal)
                                     .ToList();

            Assert.True(funde.Count == 0,
                "Diese Klassen halten eine TestDatenbank im Feld, tragen aber weder IDisposable noch " +
                "IAsyncLifetime — xunit ruft ihr Dispose dann nie, und je Testfall bleibt eine Kopie " +
                "liegen. Abhilfe: \": IDisposable\" und \"public void Dispose() => _db.Dispose();\".\n" +
                string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Er erkennt die entsorgten Formen des Bestands — und
        /// schlägt bei den drei unentsorgten an, auch beim Feld der vier Klassen vom 23.09.2026.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_entsorgte_und_unentsorgte_Kopien()
        {
            Assert.Empty(Unentsorgte("a.cs", "            using var db = new TestDatenbank();"));
            Assert.Empty(Unentsorgte("a.cs", "            using (var db = new TestDatenbank())"));
            Assert.Empty(Unentsorgte("a.cs", "            using (TestDatenbank db = new TestDatenbank())"));
            Assert.Empty(Unentsorgte("a.cs", "        private readonly TestDatenbank _db = new TestDatenbank();\n" +
                                             "        public void Dispose() => _db.Dispose();"));
            Assert.Empty(Unentsorgte("a.cs", "            var db = new TestDatenbank();\n" +
                                             "            try { } finally { db?.Dispose(); }"));

            Assert.Single(Unentsorgte("a.cs", "        private readonly TestDatenbank _db = new TestDatenbank();\n" +
                                              "        [Fact] public void Fall() { if (!_db.Vorhanden) return; }"));
            Assert.Single(Unentsorgte("a.cs", "            var db = new TestDatenbank();"));
            Assert.Single(Unentsorgte("a.cs", "            return new TestDatenbank();"));
        }

        /// <summary>
        /// <b>Gegenprobe zur Kommentarschonung:</b> Die Klassenköpfe zitieren das Muster —
        /// ein Kommentar ist keine Kopie, und ein kommentiertes Dispose entsorgt nichts.
        /// </summary>
        [Fact]
        public void Ein_Kommentar_zaehlt_weder_als_Kopie_noch_als_Entsorgung()
        {
            Assert.Empty(Unentsorgte("a.cs", "    /// (<c>private readonly TestDatenbank _db = new TestDatenbank();</c>)"));
            Assert.Empty(Unentsorgte("a.cs", "        // var db = new TestDatenbank();"));
            Assert.Single(Unentsorgte("a.cs", "        private readonly TestDatenbank _db = new TestDatenbank();\n" +
                                              "        // public void Dispose() => _db.Dispose();"));
        }

        /// <summary>
        /// <b>Gegenprobe zur Typprüfung:</b> Sie findet die Klasse mit Feld und ohne
        /// Entsorgung — und lässt die entsorgbare und die Klassenvorrichtung in Ruhe.
        /// </summary>
        [Fact]
        public void Die_Typpruefung_erkennt_eine_Klasse_ohne_Entsorgung()
        {
            Type[] proben = { typeof(FeldOhneEntsorgung), typeof(FeldMitEntsorgung), typeof(FeldAusKlassenvorrichtung) };
            Assert.Equal(new[] { typeof(FeldOhneEntsorgung) }, OhneEntsorgung(proben));
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene Dateien
        /// und Typen und findet dort Kopien und Feldträger — ein leerer Bestand liefe sonst
        /// grün durch, ohne je etwas geprüft zu haben.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_Bestand()
        {
            string[] dateien = Quelldateien();
            Assert.True(dateien.Length > 50, "Nur " + dateien.Length + " Quelldateien gefunden.");

            int kopien = dateien.Sum(d => File.ReadAllText(d).Replace("\r\n", "\n").Split('\n')
                                              .Count(z => Neu.IsMatch(z) && !IstKommentar(z)));
            Assert.True(kopien >= 500, "Nur " + kopien + " Stellen mit new TestDatenbank() gefunden (erwartet: mindestens 500).");

            int feldtraeger = Testtypen().Count(t => t.DeclaringType != typeof(TestDatenbankEntsorgungWacheTests)
                                                     && Feldtraeger(t));
            Assert.True(feldtraeger >= 10, "Nur " + feldtraeger + " Klassen mit einer TestDatenbank im Feld gefunden (erwartet: mindestens 10).");
        }

        // =====================================================================
        //  Die Proben der Typprüfung
        // =====================================================================

        private sealed class FeldOhneEntsorgung
        {
            internal TestDatenbank Db { get; set; }
        }

        private sealed class FeldMitEntsorgung : IDisposable
        {
            internal TestDatenbank Db { get; set; }

            public void Dispose() => Db?.Dispose();
        }

        private sealed class FeldAusKlassenvorrichtung : IClassFixture<TestDatenbank>
        {
            internal TestDatenbank Db { get; set; }
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Die Stellen eines Quelltexts, die eine Arbeitskopie anlegen und nicht entsorgen —
        /// je Fund „Datei:Zeile  Text".
        /// </summary>
        private static List<string> Unentsorgte(string datei, string quelltext)
        {
            string[] zeilen = quelltext.Replace("\r\n", "\n").Split('\n');
            var funde = new List<string>();
            for (int i = 0; i < zeilen.Length; i++)
            {
                string zeile = zeilen[i];
                if (!Neu.IsMatch(zeile) || IstKommentar(zeile) || MitUsing.IsMatch(zeile)) continue;

                Match zuweisung = Zuweisung.Match(zeile);
                if (zuweisung.Success && WirdEntsorgt(zeilen, zuweisung.Groups["name"].Value)) continue;

                funde.Add(datei + ":" + (i + 1) + "  " + zeile.Trim());
            }
            return funde;
        }

        /// <summary>Ruft eine Zeile außerhalb eines Kommentars <c>name.Dispose()</c> oder <c>name?.Dispose()</c>?</summary>
        private static bool WirdEntsorgt(string[] zeilen, string name)
        {
            var aufruf = new Regex(@"\b" + Regex.Escape(name) + @"\s*\??\.\s*Dispose\s*\(\s*\)");
            return zeilen.Any(z => !IstKommentar(z) && aufruf.IsMatch(z));
        }

        /// <summary>
        /// Klassen mit einer Arbeitskopie im Feld, die xunit nicht entsorgen kann: weder
        /// <c>IDisposable</c> noch <c>IAsyncLifetime</c>, und die Kopie ist keine
        /// Klassenvorrichtung, die xunit selbst entsorgt.
        /// </summary>
        private static List<Type> OhneEntsorgung(IEnumerable<Type> typen)
        {
            return typen.Where(Feldtraeger)
                        .Where(t => !typeof(IDisposable).IsAssignableFrom(t)
                                 && !typeof(IAsyncLifetime).IsAssignableFrom(t)
                                 && !typeof(IClassFixture<TestDatenbank>).IsAssignableFrom(t))
                        .ToList();
        }

        /// <summary>
        /// Hält der Typ eine <see cref="TestDatenbank"/> in einem eigenen Instanzfeld? Vom
        /// Übersetzer erzeugte Typen (Zustandsautomaten, Abschlussklassen) zählen nicht: Ihr
        /// Feld ist die lokale Variable einer <c>using</c>-Anweisung.
        /// </summary>
        private static bool Feldtraeger(Type t)
        {
            return t != typeof(TestDatenbank)
                && !t.IsDefined(typeof(CompilerGeneratedAttribute), false)
                && t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Any(f => f.FieldType == typeof(TestDatenbank));
        }

        private static IEnumerable<Type> Testtypen()
        {
            try
            {
                return typeof(TestDatenbank).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Steht die Zeile in einem Kommentar? Die Klassenköpfe nennen das Muster
        /// absichtlich; geprüft wird nur, was der Übersetzer sieht.
        /// </summary>
        private static bool IstKommentar(string zeile)
        {
            string s = zeile.TrimStart();
            return s.StartsWith("//", StringComparison.Ordinal)
                || s.StartsWith("*", StringComparison.Ordinal)
                || s.StartsWith("/*", StringComparison.Ordinal);
        }

        /// <summary>
        /// Alle <c>.cs</c>-Dateien DIESES Testprojekts — derselbe Weg wie in
        /// <see cref="DiensteSammlungTests"/>: über <see cref="CallerFilePathAttribute"/>,
        /// sonst vom Ausgabeordner aufwärts, bis <c>TestDatenbank.cs</c> auftaucht.
        /// </summary>
        private static string[] Quelldateien()
        {
            string ordner = EigenerOrdner();
            if (!Directory.Exists(ordner)) ordner = OrdnerAusDemArbeitsbaum();

            Assert.True(ordner != null && Directory.Exists(ordner),
                        "Die Quelldateien von EPOS.Kern.Tests sind nicht zu finden.");

            return Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories)
                            .Where(p => p.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
                                                  StringComparison.Ordinal) < 0
                                     && p.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
                                                  StringComparison.Ordinal) < 0)
                            .OrderBy(p => p, StringComparer.Ordinal)
                            .ToArray();
        }

        private static string EigenerOrdner([CallerFilePath] string eigeneDatei = null)
        {
            return string.IsNullOrEmpty(eigeneDatei) ? null : Path.GetDirectoryName(eigeneDatei);
        }

        private static string OrdnerAusDemArbeitsbaum()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests");
                if (File.Exists(Path.Combine(kandidat, "TestDatenbank.cs"))) return kandidat;
                d = d.Parent;
            }
            return null;
        }
    }
}
