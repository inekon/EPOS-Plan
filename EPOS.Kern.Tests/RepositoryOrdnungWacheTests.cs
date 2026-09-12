using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
    /// <para><b>Der Wächter prüft, was VERSIONIERT ist — nicht, was im Ordner liegt.</b>
    /// Bis zum ersten Gate nach #242 lief er über das Dateisystem und schlug dabei bei
    /// jedem Entwickler an, der vorher einen Referenzlauf gefahren hatte: Der legt
    /// <c>Referenzlaeufe/Arbeitskopie/Kenndaten.sqlite</c> an (<c>.gitignore</c>), und die
    /// Agenten-Arbeitsbäume unter <c>.claude/worktrees/…</c> tragen je eine vollständige
    /// zweite Kopie des Bestands. Beides ist ausdrücklich nicht versioniert und geht den
    /// Wächter nichts an — sein Gegenstand ist der Merge, nicht der Arbeitsplatz. Die
    /// Dateiliste kommt deshalb aus <c>git ls-files -z</c>; Git-LFS-Zeiger stehen darin
    /// wie jede andere Datei. Lässt sich <c>git</c> nicht starten (eine Umgebung ohne Git,
    /// ein entpacktes Archiv), fällt der Wächter auf den Dateisystemlauf zurück, nimmt
    /// dann aber <c>.claude</c>, <c>TestResults</c> und <c>Referenzlaeufe/Arbeitskopie</c>
    /// zusätzlich aus — er besteht NIE still.</para>
    ///
    /// <para><b>Was der Wächter NICHT prüft.</b> <c>.git</c>, <c>bin</c>, <c>obj</c> und
    /// <c>node_modules</c> — dort liegen Bauartefakte, keine versionierten Dateien. Die
    /// eine erlaubte Ausnahme von der <c>*.sqlite</c>-Regel ist die Testdatenbank
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> (Setup- und Vorlagendatenbanken liegen
    /// unter <c>Setup/Vorlage/</c> bzw. sind ohnehin gitignored und stehen damit gar nicht
    /// im versionierten Bestand).</para>
    /// </summary>
    public sealed class RepositoryOrdnungWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Ordnernamen, die es im versionierten Bestand an KEINER Stelle geben darf.</summary>
        private static readonly string[] VerboteneOrdnernamen = { ".work", "DB-Backup" };

        /// <summary>
        /// Ordnernamen, die der Wächter nie betritt — Bauordner und Fremdwerkzeuge, keine
        /// versionierten Dateien. <c>git ls-files</c> führt sie ohnehin nicht; für den
        /// Rückfallweg über das Dateisystem sind sie die erste Ausnahmeliste.
        /// </summary>
        private static readonly string[] AusgenommeneOrdnernamen = { ".git", "bin", "obj", "node_modules" };

        /// <summary>
        /// NUR für den Rückfallweg über das Dateisystem: Ordner, die zwar im Arbeitsbaum
        /// liegen, aber nicht versioniert sind — die Agenten-Arbeitsbäume
        /// (<c>.claude/worktrees/…</c>) und die Testergebnisse. Über <c>git ls-files</c>
        /// entstehen sie gar nicht erst.
        /// </summary>
        private static readonly string[] NurDateisystemAusgenommeneOrdnernamen = { ".claude", "TestResults" };

        /// <summary>
        /// NUR für den Rückfallweg: der Ablageort der Referenzlauf-Arbeitskopie, repo-relativ.
        /// <c>EPOS.Referenzlauf</c> legt dort bei JEDEM Lauf eine <c>Kenndaten.sqlite</c> an
        /// (<c>.gitignore</c>) — sie ist ein Laufartefakt, kein Repositoryinhalt.
        /// </summary>
        private const string NurDateisystemAusgenommenerPfad = "Referenzlaeufe/Arbeitskopie";

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

        /// <summary>Die einzige erlaubte <c>*.sqlite</c>-Datei im Bestand, repo-relativ.</summary>
        private const string WeisslisteSqlite = "Referenzlaeufe/Kenndaten_Test.sqlite";

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>Keine Sicherungskopie von Quelltext oder Datenbank im Bestand.</summary>
        [Fact]
        public void Keine_Sicherungskopien_und_keine_Datenbankdateien()
        {
            List<string> funde = FundeSicherungskopien(Bestand().Dateien);

            Assert.True(funde.Count == 0,
                "Diese Dateien gehoeren nicht ins Repository (Auftrag #242, Konzept " +
                "Repository_Aufraeumen). Sicherungskopien und Datenbankdateien werden per " +
                "'git rm' entfernt, nie versioniert:\n" + string.Join("\n", funde));
        }

        /// <summary>Kein Arbeitsordner <c>.work/</c> und kein Sicherungsordner <c>DB-Backup/</c>.</summary>
        [Fact]
        public void Kein_Arbeitsordner_und_kein_Datenbank_Sicherungsordner()
        {
            List<string> funde = FundeVerboteneOrdner(Bestand().Ordner);

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
            List<string> funde = FundeFremdeSqlite(Bestand().Dateien);

            Assert.True(funde.Count == 0,
                "Diese *.sqlite-Dateien stehen VERSIONIERT im Repository - einzig erlaubt " +
                "ist " + WeisslisteSqlite + " (Auftrag #242). Eine nicht versionierte Kopie " +
                "im Arbeitsbaum (z. B. Referenzlaeufe/Arbeitskopie) ist hier NICHT gemeint:\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Auftrag #243 (Anwenderentscheid AUF‑Q2, 12.09.2026).</b> Die
        /// <c>.gitattributes</c> trägt die vier LFS-Regeln — Testdatenbank und die drei
        /// Archivmuster unter <c>VDI-3805-Daten/</c>. Fällt eine davon bei einem Merge
        /// heraus, landet die nächste Änderung an der Datenbank wieder als 68-MB-Blob in
        /// der Geschichte, und das ist nicht mehr rückgängig zu machen (AUF‑Q1: keine
        /// Geschichtsumschreibung).
        /// </summary>
        [Fact]
        public void Die_gitattributes_traegt_die_vier_LFS_Regeln()
        {
            string wurzel = Arbeitsbaum();
            Assert.Contains(".gitattributes", Bestand().Dateien);

            string text = File.ReadAllText(Path.Combine(wurzel, ".gitattributes"));
            foreach (string muster in LfsMuster)
                Assert.True(text.Contains(muster + " filter=lfs diff=lfs merge=lfs -text", StringComparison.Ordinal),
                    "In .gitattributes fehlt die LFS-Regel fuer \"" + muster + "\" (Auftrag #243). " +
                    "Ohne sie wandert die naechste Fassung dieser Dateien als voller Blob in die " +
                    "Geschichte - und dort bleibt sie.");
        }

        /// <summary>
        /// <b>Auftrag #243.</b> Die Testdatenbank im Arbeitsbaum ist die Datenbank und
        /// nicht ihr LFS-Zeiger. Ohne diesen Fall faellt ein Klon ohne aktiven LFS-Filter
        /// erst tief in den Datenbankfaellen als „file is not a database" auf.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_ist_kein_LFS_Zeiger()
        {
            string pfad = Path.Combine(Arbeitsbaum(), WeisslisteSqlite.Replace('/', Path.DirectorySeparatorChar));
            Assert.Contains(WeisslisteSqlite, Bestand().Dateien);
            if (!File.Exists(pfad)) return;   // Umgebung ohne die Datei - andere Faelle ueberspringen ebenso

            Assert.False(LfsZeigerProbe.IstZeiger(pfad), LfsZeigerProbe.Meldung(pfad));
        }

        // =====================================================================
        //  Die Regel als Funktion — dieselbe für den Bestand und die Gegenproben
        // =====================================================================

        /// <summary>Die vier Muster, die seit #243 in Git LFS liegen.</summary>
        private static readonly string[] LfsMuster =
        {
            "Referenzlaeufe/Kenndaten_Test.sqlite",
            "VDI-3805-Daten/**/*.zip",
            "VDI-3805-Daten/**/*.vdi",
            "VDI-3805-Daten/**/*.VDI",
        };

        /// <summary>Alle Sicherungskopien in einer übergebenen Pfadliste (repo-relativ).</summary>
        private static List<string> FundeSicherungskopien(IEnumerable<string> dateien)
        {
            var funde = new List<string>();
            foreach (string datei in dateien)
            {
                string name = Dateiname(datei);
                foreach (var muster in VerboteneDateimuster)
                    if (muster.Muster.IsMatch(name))
                        funde.Add(datei + "  (" + muster.Name + ")");
            }
            return funde;
        }

        /// <summary>Alle verbotenen Ordner in einer übergebenen Ordnerliste (repo-relativ).</summary>
        private static List<string> FundeVerboteneOrdner(IEnumerable<string> ordner)
        {
            var funde = new List<string>();
            foreach (string o in ordner)
                if (VerboteneOrdnernamen.Contains(Dateiname(o), StringComparer.Ordinal))
                    funde.Add(o + "/");
            return funde;
        }

        /// <summary>Alle <c>*.sqlite</c> ausserhalb der Weissliste (repo-relativ).</summary>
        private static List<string> FundeFremdeSqlite(IEnumerable<string> dateien)
        {
            var funde = new List<string>();
            foreach (string datei in dateien)
            {
                if (!datei.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)) continue;
                if (datei == WeisslisteSqlite) continue;
                funde.Add(datei);
            }
            return funde;
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
            var (dateien, ordner) = Bestand();
            Assert.True(dateien.Length > 2000, "Nur " + dateien.Length + " Dateien gefunden.");

            Assert.Contains("CLAUDE.md", dateien);
            Assert.Contains(WeisslisteSqlite, dateien);
            Assert.Contains("EPOS.Kern/Allgemein/Simulation", ordner);
        }

        /// <summary>
        /// <b>Gegenprobe zur Quelle der Liste:</b> Der Bestand enthält keine Datei aus
        /// <c>bin/</c>, <c>obj/</c>, <c>.git/</c> oder <c>node_modules/</c> — und seit dem
        /// Befund im Gate nach #242 auch nichts aus <c>.claude/worktrees/…</c>,
        /// <c>TestResults/</c> oder <c>Referenzlaeufe/Arbeitskopie/</c>. Das sind
        /// Laufartefakte; sie fluteten den Wächter mit Fundstellen, die kein Merge je
        /// zurückholt. Über <c>git ls-files</c> entstehen sie gar nicht, auf dem
        /// Rückfallweg nimmt die Ausnahmeliste sie heraus — der Fall prüft beides.
        /// </summary>
        [Fact]
        public void Bauordner_Worktrees_und_Laufartefakte_bleiben_aussen_vor()
        {
            var (dateien, ordner) = Bestand();

            string[] tabu = AusgenommeneOrdnernamen
                            .Concat(NurDateisystemAusgenommeneOrdnernamen)
                            .ToArray();

            bool Verboten(string pfad) =>
                pfad.Split('/').Any(teil => tabu.Contains(teil, StringComparer.OrdinalIgnoreCase))
                || pfad == NurDateisystemAusgenommenerPfad
                || pfad.StartsWith(NurDateisystemAusgenommenerPfad + "/", StringComparison.Ordinal);

            Assert.DoesNotContain(dateien, Verboten);
            Assert.DoesNotContain(ordner, Verboten);
        }

        /// <summary>
        /// <b>Gegenprobe zum Wächter selbst:</b> Ein eingeschmuggelter Fund — je eine
        /// verbotene Datei, ein verbotener Ordner und eine <c>*.sqlite</c> außerhalb der
        /// Weißliste — fällt in einem eigens angelegten, synthetischen Arbeitsbaum auf; ein
        /// unauffälliger Bestand bleibt sauber. Ohne diesen Fall wäre offen, ob die drei
        /// Regeln wirklich anschlagen, statt nur zufällig auf den heutigen Bestand zu
        /// passen. Geprüft werden hier die REGELN — woher die Pfadliste kommt (Git oder
        /// Dateisystem), ist dafür gleichgültig.
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
                Directory.CreateDirectory(Path.Combine(wurzel, "Referenzlaeufe", "Arbeitskopie"));

                File.WriteAllText(Path.Combine(wurzel, "EPOS.Kern", "Foo.cs"), "class Foo {}");
                File.WriteAllText(Path.Combine(wurzel, "EPOS.Kern", "Foo.cs.bak"), "class Foo {}");
                File.WriteAllText(Path.Combine(wurzel, "bin", "Debug", "Geheim.bak"), "x");
                File.WriteAllText(Path.Combine(wurzel, ".work", "RealFleetHarness", "Program.cs"), "x");
                File.WriteAllText(Path.Combine(wurzel, "DB-Backup", "Kenndaten-alt.accdb"), "x");
                File.WriteAllText(Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite"), "x");
                File.WriteAllText(Path.Combine(wurzel, "Referenzlaeufe", "Fremd.sqlite"), "x");
                File.WriteAllText(Path.Combine(wurzel, "Referenzlaeufe", "Arbeitskopie", "Kenndaten.sqlite"), "x");

                var (dateien, ordner) = Dateisystembestand(wurzel);

                // Die eingeschmuggelte Sicherungskopie im Quellordner faellt auf ...
                Assert.Contains(FundeSicherungskopien(dateien), f => f.StartsWith("EPOS.Kern/Foo.cs.bak", StringComparison.Ordinal));
                // ... die im Bauordner NICHT, weil bin/ nie betreten wird.
                Assert.DoesNotContain(dateien, d => d.EndsWith("Geheim.bak", StringComparison.Ordinal));
                // ... und die Arbeitskopie des Referenzlaufs ebenso wenig (Befund Gate #242).
                Assert.DoesNotContain(dateien, d => d.StartsWith("Referenzlaeufe/Arbeitskopie", StringComparison.Ordinal));

                // Die zwei verbotenen Ordner faellt die Ordnerregel an.
                List<string> ordnerfunde = FundeVerboteneOrdner(ordner);
                Assert.Contains(".work/", ordnerfunde);
                Assert.Contains("DB-Backup/", ordnerfunde);

                // Weissliste greift nur fuer IHREN Pfad.
                List<string> sqlitefunde = FundeFremdeSqlite(dateien);
                Assert.Contains("Referenzlaeufe/Fremd.sqlite", sqlitefunde);
                Assert.DoesNotContain(WeisslisteSqlite, sqlitefunde);
            }
            finally
            {
                Directory.Delete(wurzel, recursive: true);
            }
        }

        /// <summary>
        /// <b>Gegenprobe zur Liste aus Git:</b> <c>git ls-files -z</c> läuft im
        /// Arbeitsbaum, liefert NUL-getrennte, unverkürzte Pfade (auch mit Umlaut) und
        /// führt die Testdatenbank, obwohl sie seit #243 ein LFS-Zeiger im Index ist.
        /// Geht Git in dieser Umgebung nicht, greift der Rückfallweg — dann bleibt dieser
        /// Fall still, der Wächter selbst aber nicht (er prüft dann das Dateisystem).
        /// </summary>
        [Fact]
        public void Die_Liste_aus_Git_traegt_die_Testdatenbank_und_Pfade_mit_Umlaut()
        {
            string[] ausGit = VersionierteDateien(Arbeitsbaum());
            if (ausGit == null) return;   // keine Git-Umgebung - der Rueckfallweg ist geprueft

            Assert.Contains(WeisslisteSqlite, ausGit);
            Assert.DoesNotContain(ausGit, p => p.StartsWith("\"", StringComparison.Ordinal));
            Assert.Contains(ausGit, p => p.Any(z => z > 127));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        private static Regex Muster(string name)
            => VerboteneDateimuster.Single(w => w.Name == name).Muster;

        /// <summary>Der letzte Pfadteil einer repo-relativen Angabe.</summary>
        private static string Dateiname(string pfad)
        {
            int i = pfad.LastIndexOf('/');
            return i < 0 ? pfad : pfad.Substring(i + 1);
        }

        /// <summary>
        /// Der zu prüfende Bestand: Dateien und Ordner, repo-relativ mit '/'.
        ///
        /// <para>Erste Wahl ist <c>git ls-files -z</c> — was nicht versioniert ist, geht
        /// den Wächter nichts an. Erst wenn sich Git nicht starten lässt, läuft er über das
        /// Dateisystem; still bestehen tut er nie.</para>
        /// </summary>
        private static (string[] Dateien, string[] Ordner) Bestand()
        {
            string wurzel = Arbeitsbaum();
            string[] ausGit = VersionierteDateien(wurzel);
            if (ausGit != null) return (ausGit, OrdnerAusDateien(ausGit));
            return Dateisystembestand(wurzel);
        }

        /// <summary>
        /// Die versionierten Dateien aus <c>git ls-files -z</c>, repo-relativ mit '/'.
        /// Liefert <c>null</c>, wenn Git nicht startet oder mit einem Fehler endet.
        ///
        /// <para><c>-z</c> ist wesentlich: Ohne den Schalter verkürzt Git Pfade mit
        /// Sonderzeichen zu <c>"…\303\244…"</c> in Anführungszeichen — eine der
        /// VDI-3805-Dateien trägt ein „ä" im Namen. Mit <c>-z</c> kommen die Bytes roh und
        /// NUL-getrennt, deshalb wird die Ausgabe ausdrücklich als UTF-8 gelesen.</para>
        /// </summary>
        private static string[] VersionierteDateien(string wurzel)
        {
            try
            {
                var start = new ProcessStartInfo("git", "ls-files -z")
                {
                    WorkingDirectory = wurzel,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = new UTF8Encoding(false),
                    StandardErrorEncoding = new UTF8Encoding(false),
                };

                using Process p = Process.Start(start);
                if (p == null) return null;

                string ausgabe = p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                if (!p.WaitForExit(120_000)) return null;
                if (p.ExitCode != 0) return null;

                string[] pfade = ausgabe.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                return pfade.Length == 0 ? null : pfade;
            }
            catch (Exception)
            {
                // Kein Git in dieser Umgebung (entpacktes Archiv, schmales Abbild) -
                // der Aufrufer nimmt den Dateisystemweg.
                return null;
            }
        }

        /// <summary>Alle Ordnerpfade, die in einer Dateiliste vorkommen — repo-relativ, ohne Dubletten.</summary>
        private static string[] OrdnerAusDateien(string[] dateien)
        {
            var ordner = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string datei in dateien)
            {
                int i = datei.IndexOf('/');
                while (i >= 0)
                {
                    ordner.Add(datei.Substring(0, i));
                    i = datei.IndexOf('/', i + 1);
                }
            }
            return ordner.ToArray();
        }

        /// <summary>
        /// Rückfallweg: alle Dateien und Ordner unter <paramref name="wurzel"/>, rekursiv,
        /// repo-relativ mit '/'. Ausgenommen sind die Bauordner
        /// (<see cref="AusgenommeneOrdnernamen"/>) UND die drei nicht versionierten
        /// Laufablagen (<see cref="NurDateisystemAusgenommeneOrdnernamen"/>,
        /// <see cref="NurDateisystemAusgenommenerPfad"/>).
        /// </summary>
        private static (string[] Dateien, string[] Ordner) Dateisystembestand(string wurzel)
        {
            var dateien = new List<string>();
            var ordner = new List<string>();

            string Relativ(string voll)
                => voll.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar, '/')
                       .Replace(Path.DirectorySeparatorChar, '/');

            void Rekursiv(string aktuell)
            {
                foreach (string datei in Directory.EnumerateFiles(aktuell))
                {
                    // .git ist in einem Worktree eine DATEI (Verweis auf das gemeinsame
                    // Git-Verzeichnis), kein Ordner - ohne diesen Filter entginge sie der
                    // Ordnerausnahme und stuende faelschlich im erfassten Bestand.
                    string dateiname = Path.GetFileName(datei);
                    if (AusgenommeneOrdnernamen.Contains(dateiname, StringComparer.OrdinalIgnoreCase)) continue;
                    dateien.Add(Relativ(datei));
                }
                foreach (string unter in Directory.EnumerateDirectories(aktuell))
                {
                    string name = Path.GetFileName(unter);
                    if (AusgenommeneOrdnernamen.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    if (NurDateisystemAusgenommeneOrdnernamen.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    string relativ = Relativ(unter);
                    if (relativ == NurDateisystemAusgenommenerPfad) continue;
                    ordner.Add(relativ);
                    Rekursiv(unter);
                }
            }

            Rekursiv(wurzel);
            return (dateien.ToArray(), ordner.ToArray());
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
