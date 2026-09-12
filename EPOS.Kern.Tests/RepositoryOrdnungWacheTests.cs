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
    /// Der Wächter über die ORDNUNG des Repositorys — Auftrag #242 (Anwender, 12.09.2026:
    /// „Räume alte nicht mehr genutzte Läufe/Verzeichnisse auf", „Lösche .work").
    ///
    /// <para><b>Die Regel</b> (<c>Dokumentation/aktuell/Konzept_Repository_Aufraeumen_EPOS-Plan.md</c>
    /// § 1). Drei Dinge gehören NIE ins Repository: Sicherungskopien von Quelltexten
    /// (<c>*.bak</c>, <c>*.orig</c>, <c>*.original-*</c>), Kopien oder Sicherungen von
    /// Datenbanken (<c>*.accdb</c>, <c>*.laccdb</c>, jedes <c>*.sqlite</c> außer der
    /// Testdatenbank) und Arbeitsordner (<c>.work/</c>) beziehungsweise der frühere
    /// Access-Sicherungsordner (<c>DB-Backup/</c>). Auftrag #242 hat 71 MB genau davon
    /// entfernt — <c>.work/</c> mit einer 70-MB-Kopie der Produktivdatenbank, 16
    /// Git-LFS-Zeiger unter <c>DB-Backup/</c>, vier <c>.bak</c>, vier
    /// Lizenzserver-Originale. Ohne einen dauerhaften Wächter kommt beim nächsten Sync
    /// oder Merge genau das zurück, was #242 entfernt hat — die <c>.gitignore</c> schützt
    /// nur vor einem NEUEN <c>git add</c>, nicht vor einem bereits versionierten Fund, der
    /// über einen anderen Zweig zurückgemischt wird.</para>
    ///
    /// <para><b>Was der Wächter NICHT prüft.</b> Er betritt <c>.git</c>, <c>bin</c>,
    /// <c>obj</c> und <c>node_modules</c> nicht — dort liegen Bauartefakte, keine
    /// versionierten Dateien. Die eine erlaubte Ausnahme von der <c>*.sqlite</c>-Regel ist
    /// die Testdatenbank <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> (Setup- und
    /// Vorlagendatenbanken liegen unter <c>Setup/Vorlage/</c> bzw. sind ohnehin
    /// gitignored und stehen damit gar nicht im Arbeitsbaum eines sauberen Checkouts).</para>
    /// </summary>
    public sealed class RepositoryOrdnungWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Ordnernamen, die es im Arbeitsbaum an KEINER Stelle geben darf.</summary>
        private static readonly string[] VerboteneOrdnernamen = { ".work", "DB-Backup" };

        /// <summary>
        /// Ordnernamen, die der Wächter nie betritt — Bauordner und Fremdwerkzeuge, keine
        /// versionierten Dateien.
        /// </summary>
        private static readonly string[] AusgenommeneOrdnernamen = { ".git", "bin", "obj", "node_modules" };

        /// <summary>
        /// Die verbotenen Dateimuster — Sicherungskopien von Quelltexten und
        /// Datenbankdateien. Je Muster ein Name für die Meldung.
        /// </summary>
        private static readonly (string Name, Regex Muster)[] VerboteneDateimuster =
        {
            ("*.bak",        new Regex(@"\.bak$",           RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("*.orig",       new Regex(@"\.orig$",          RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("*.original-*", new Regex(@"\.original-[^.]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("*.accdb",      new Regex(@"\.accdb$",         RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("*.laccdb",     new Regex(@"\.laccdb$",        RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        };

        /// <summary>Die einzige erlaubte <c>*.sqlite</c>-Datei im ganzen Arbeitsbaum, repo-relativ.</summary>
        private const string WeisslisteSqlite = "Referenzlaeufe/Kenndaten_Test.sqlite";

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>Keine Sicherungskopie von Quelltext oder Datenbank im Arbeitsbaum.</summary>
        [Fact]
        public void Keine_Sicherungskopien_und_keine_Datenbankdateien()
        {
            var funde = new List<string>();
            foreach (string datei in Erfasse(Arbeitsbaum()).Dateien)
            {
                string name = Path.GetFileName(datei);
                foreach (var muster in VerboteneDateimuster)
                    if (muster.Muster.IsMatch(name))
                        funde.Add(Kurzname(datei) + "  (" + muster.Name + ")");
            }

            Assert.True(funde.Count == 0,
                "Diese Dateien gehoeren nicht ins Repository (Auftrag #242, Konzept " +
                "Repository_Aufraeumen). Sicherungskopien und Datenbankdateien werden per " +
                "'git rm' entfernt, nie versioniert:\n" + string.Join("\n", funde));
        }

        /// <summary>Kein Arbeitsordner <c>.work/</c> und kein Sicherungsordner <c>DB-Backup/</c>.</summary>
        [Fact]
        public void Kein_Arbeitsordner_und_kein_Datenbank_Sicherungsordner()
        {
            var funde = new List<string>();
            foreach (string ordner in Erfasse(Arbeitsbaum()).Ordner)
            {
                string name = Path.GetFileName(ordner);
                if (VerboteneOrdnernamen.Contains(name, StringComparer.Ordinal))
                    funde.Add(Kurzname(ordner) + "/");
            }

            Assert.True(funde.Count == 0,
                "Diese Ordner gehoeren nicht ins Repository (Auftrag #242): Arbeitsordner " +
                "der Windows-Seite (.work) und der ehemalige Access-Sicherungsordner " +
                "(DB-Backup) sind beide mit #242 entfernt worden - kommt einer zurueck, " +
                "hat ihn ein Merge oder Sync wieder hereingezogen:\n" +
                string.Join("\n", funde));
        }

        /// <summary><c>*.sqlite</c> nur auf der Weißliste — die Testdatenbank.</summary>
        [Fact]
        public void Sqlite_Dateien_nur_auf_der_Weissliste()
        {
            string wurzel = Arbeitsbaum();
            var funde = new List<string>();
            foreach (string datei in Erfasse(wurzel).Dateien)
            {
                if (!datei.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)) continue;
                string kurz = Kurzname(datei);
                if (kurz == WeisslisteSqlite) continue;
                funde.Add(kurz);
            }

            Assert.True(funde.Count == 0,
                "Diese *.sqlite-Dateien stehen nicht auf der Weissliste - einzig erlaubt " +
                "ist " + WeisslisteSqlite + " (Auftrag #242):\n" + string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Alle fünf Dateimuster treffen — in den
        /// Schreibweisen des Bestands (die vier entfernten Lizenzserver-Originale, die vier
        /// entfernten <c>.bak</c>) —, und ähnlich aussehende, aber unbedenkliche Namen
        /// treffen nicht.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_alle_fuenf_Dateimuster()
        {
            Assert.Matches(Muster("*.bak"), "BHKWCtrl.cs.bak");
            Assert.Matches(Muster("*.bak"), "ProjektDuplizierenCtrl.bak");
            Assert.Matches(Muster("*.orig"), "Foo.cs.orig");
            Assert.Matches(Muster("*.original-*"), "epos-lizenz.php.original-2026-08-19");
            Assert.Matches(Muster("*.accdb"), "Kenndaten.accdb");
            Assert.Matches(Muster("*.laccdb"), "Kenndaten.laccdb");

            // Aehnlich, aber KEIN Fund.
            Assert.DoesNotMatch(Muster("*.bak"), "Backup.cs");
            Assert.DoesNotMatch(Muster("*.bak"), "bakery.txt");
            Assert.DoesNotMatch(Muster("*.orig"), "Original.cs");
            Assert.DoesNotMatch(Muster("*.accdb"), "Kenndaten.sqlite");
            Assert.DoesNotMatch(Muster("*.laccdb"), "Kenndaten.accdb");
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über einen wirklich großen,
        /// versionierten Bestand — ein leerer oder halber Bestand liefe sonst grün durch,
        /// ohne je etwas geprüft zu haben. Zwei bekannte Dateien belegen, dass er die
        /// Wurzel UND einen Unterordner mehrerer Ebenen wirklich sieht.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_einen_grossen_Bestand_samt_Unterordnern()
        {
            var (dateien, _) = Erfasse(Arbeitsbaum());
            Assert.True(dateien.Length > 2000, "Nur " + dateien.Length + " Dateien gefunden.");

            Assert.Contains(dateien, d => Kurzname(d) == "CLAUDE.md");
            Assert.Contains(dateien, d => Kurzname(d) == WeisslisteSqlite);
        }

        /// <summary>
        /// <b>Gegenprobe zu den Bauordnern:</b> Eine Datei unter <c>bin/</c> oder <c>obj/</c>
        /// darf niemals im erfassten Bestand auftauchen — sonst würde jeder lokale Build
        /// den Wächter mit Fundstellen fluten, die kein Merge je zurückholt.
        /// </summary>
        [Fact]
        public void Bauordner_und_git_Ordner_bleiben_aussen_vor()
        {
            var (dateien, ordner) = Erfasse(Arbeitsbaum());

            bool InBauordner(string pfad) =>
                pfad.Split(Path.DirectorySeparatorChar)
                    .Any(teil => AusgenommeneOrdnernamen.Contains(teil, StringComparer.OrdinalIgnoreCase));

            Assert.DoesNotContain(dateien, InBauordner);
            Assert.DoesNotContain(ordner, InBauordner);
        }

        /// <summary>
        /// <b>Gegenprobe zum Wächter selbst:</b> Ein eingeschmuggelter Fund — je eine
        /// verbotene Datei, ein verbotener Ordner und eine <c>*.sqlite</c> außerhalb der
        /// Weißliste — fällt in einem eigens angelegten, synthetischen Arbeitsbaum auf; ein
        /// unauffälliger Bestand bleibt sauber. Ohne diesen Fall wäre offen, ob die drei
        /// Regeln wirklich anschlagen, statt nur zufällig auf den heutigen Bestand zu
        /// passen.
        /// </summary>
        [Fact]
        public void Ein_eingeschmuggelter_Fund_faellt_in_einem_synthetischen_Baum_auf()
        {
            string wurzel = Path.Combine(Path.GetTempPath(), "RepositoryOrdnungWache_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(wurzel, "EPOS.Kern"));
                Directory.CreateDirectory(Path.Combine(wurzel, "bin", "Debug"));
                Directory.CreateDirectory(Path.Combine(wurzel, ".work", "RealFleetHarness"));
                Directory.CreateDirectory(Path.Combine(wurzel, "DB-Backup"));
                Directory.CreateDirectory(Path.Combine(wurzel, "Referenzlaeufe"));

                File.WriteAllText(Path.Combine(wurzel, "EPOS.Kern", "Foo.cs"), "class Foo {}");
                File.WriteAllText(Path.Combine(wurzel, "EPOS.Kern", "Foo.cs.bak"), "class Foo {}");
                File.WriteAllText(Path.Combine(wurzel, "bin", "Debug", "Geheim.bak"), "x");
                File.WriteAllText(Path.Combine(wurzel, ".work", "RealFleetHarness", "Program.cs"), "x");
                File.WriteAllText(Path.Combine(wurzel, "DB-Backup", "Kenndaten-alt.accdb"), "x");
                File.WriteAllText(Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite"), "x");
                File.WriteAllText(Path.Combine(wurzel, "Referenzlaeufe", "Fremd.sqlite"), "x");

                var (dateien, ordner) = Erfasse(wurzel);

                // Die eingeschmuggelte Sicherungskopie im Quellordner faellt auf ...
                Assert.Contains(dateien, d => Path.GetFileName(d) == "Foo.cs.bak");
                // ... die im Bauordner NICHT, weil bin/ nie betreten wird.
                Assert.DoesNotContain(dateien, d => Path.GetFileName(d) == "Geheim.bak");
                // Die zwei verbotenen Ordner stehen im erfassten Ordnerbestand.
                Assert.Contains(ordner, o => Path.GetFileName(o) == ".work");
                Assert.Contains(ordner, o => Path.GetFileName(o) == "DB-Backup");
                // Weissliste greift nur fuer IHREN Pfad.
                Assert.Contains(dateien, d => Path.GetFileName(d) == "Fremd.sqlite");
                Assert.Contains(dateien, d => Path.GetFileName(d) == "Kenndaten_Test.sqlite");
            }
            finally
            {
                Directory.Delete(wurzel, recursive: true);
            }
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        private static Regex Muster(string name)
            => VerboteneDateimuster.Single(w => w.Name == name).Muster;

        /// <summary>
        /// Alle Dateien und alle Ordner unter <paramref name="wurzel"/>, rekursiv, ohne
        /// <see cref="AusgenommeneOrdnernamen"/>.
        /// </summary>
        private static (string[] Dateien, string[] Ordner) Erfasse(string wurzel)
        {
            var dateien = new List<string>();
            var ordner = new List<string>();

            void Rekursiv(string aktuell)
            {
                foreach (string datei in Directory.EnumerateFiles(aktuell))
                {
                    // .git ist in einem Worktree eine DATEI (Verweis auf das gemeinsame
                    // Git-Verzeichnis), kein Ordner - ohne diesen Filter entginge sie der
                    // Ordnerausnahme und stuende faelschlich im erfassten Bestand.
                    string dateiname = Path.GetFileName(datei);
                    if (AusgenommeneOrdnernamen.Contains(dateiname, StringComparer.OrdinalIgnoreCase)) continue;
                    dateien.Add(datei);
                }
                foreach (string unter in Directory.EnumerateDirectories(aktuell))
                {
                    string name = Path.GetFileName(unter);
                    if (AusgenommeneOrdnernamen.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    ordner.Add(unter);
                    Rekursiv(unter);
                }
            }

            Rekursiv(wurzel);
            return (dateien.ToArray(), ordner.ToArray());
        }

        /// <summary>Der Pfad ab der übergebenen Wurzel — das nennt die Meldung.</summary>
        private static string Kurzname(string pfadUnterWurzel)
        {
            string wurzel = Arbeitsbaum();
            return pfadUnterWurzel.StartsWith(wurzel, StringComparison.Ordinal)
                 ? pfadUnterWurzel.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar)
                       .Replace(Path.DirectorySeparatorChar, '/')
                 : pfadUnterWurzel;
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>ParallelitaetWacheTests</c>.</summary>
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
