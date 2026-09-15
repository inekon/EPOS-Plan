using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die KODIERUNG des C#-Quelltexts — Auftrag #290.
    ///
    /// <para><b>Die Regel</b> steht in der <c>.editorconfig</c>: <c>[*.cs] charset =
    /// utf-8-bom</c>. Verbindlich ist UTF-8 <b>mit</b> BOM, und zwar nicht aus Geschmack.
    /// Ein Teil des Bestands lag historisch als Windows-1252 ohne BOM vor; ein Werkzeug
    /// ohne ausdrückliche Kodierungsangabe liest eine BOM-lose Datei je nach Umgebung als
    /// UTF-8 oder als Codepage und schreibt sie so zurück — dabei zerfallen die Umlaute in
    /// den deutschen Bezeichnern und Kommentaren. Die Signatur macht die Kodierung für
    /// Visual Studio, MSBuild, Git und jedes Kommandozeilenwerkzeug eindeutig.</para>
    ///
    /// <para><b>Warum ein Wächter.</b> Ohne ihn kommt der Fund zurück. Die
    /// <c>.editorconfig</c> ist eine Bitte an den Editor, keine Prüfung: Wer eine Datei mit
    /// einem Skript, einem Generator oder über die Zwischenablage anlegt, legt sie ohne BOM
    /// an, und niemand sieht es. Auftrag #290 hat 332 von 1 311 versionierten <c>.cs</c> so
    /// vorgefunden — ein knappes Viertel des Bestands, über zwölf Projekte verteilt.</para>
    ///
    /// <para><b>Der Wächter prüft, was VERSIONIERT ist — nicht, was im Ordner liegt.</b>
    /// Die Dateiliste kommt aus <c>git ls-files -z</c>: Ein nicht versioniertes Fundstück
    /// im Arbeitsbaum (ein Wegwerf-Prüfstand, ein Agenten-Arbeitsbaum unter
    /// <c>.claude/worktrees/…</c>) geht keinen Merge etwas an. Lässt sich <c>git</c> nicht
    /// starten (eine Umgebung ohne Git, ein entpacktes Archiv), fällt er auf den
    /// Dateisystemlauf zurück und nimmt dort <c>bin</c>, <c>obj</c>, <c>.git</c>,
    /// <c>node_modules</c>, <c>.claude</c> und <c>TestResults</c> aus — er besteht NIE
    /// still.</para>
    ///
    /// <para><b>Was der Wächter NICHT prüft.</b> Zeilenenden. Die <c>.gitattributes</c>
    /// führt <c>* text=auto</c>, Git legt im Verzeichnis LF ab und checkt auf Windows CRLF
    /// aus — im Arbeitsbaum eines Linux-Läufers steht also LF, obwohl die
    /// <c>.editorconfig</c> CRLF verlangt. Beides ist richtig; eine Prüfung darauf wäre auf
    /// der einen Plattform immer rot. Ebenso wenig prüft er <c>.razor</c> (die
    /// <c>.editorconfig</c> kennt für sie keine Kodierungsregel), <c>.csproj</c> und
    /// <c>.resx</c> — dieser Wächter ist der über <c>.cs</c>.</para>
    /// </summary>
    public sealed class QuelltextKodierungWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die drei Bytes der UTF-8-Signatur: <c>EF BB BF</c>.</summary>
        private static readonly byte[] Signatur = { 0xEF, 0xBB, 0xBF };

        /// <summary>
        /// Versionierte <c>.cs</c>, die AUSNAHMSWEISE ohne BOM stehen dürfen — namentlich
        /// und begründet, repo-relativ mit '/'.
        ///
        /// <para><b>Die Liste ist leer, und das ist ein Messergebnis.</b> Auftrag #290 hat
        /// alle 1 311 versionierten <c>.cs</c> Byte für Byte geprüft: Jede einzelne ist
        /// gültiges UTF-8. Die Windows-1252-Dateien, vor denen die Wurzel-<c>CLAUDE.md</c>
        /// warnt, sind unter den <c>.cs</c> nicht mehr vorhanden — ihnen ein BOM
        /// voranzustellen hätte Bytesalat ergeben, deshalb wurde vor dem Ändern gemessen
        /// und nicht geraten. Wer hier je einen Eintrag ergänzt, schreibt den Grund
        /// daneben: „ohne BOM" ist kein Grund, „diese Datei ist Windows-1252 und wird von
        /// <c>X</c> so gelesen" ist einer.</para>
        /// </summary>
        private static readonly string[] AusnahmenOhneBom = Array.Empty<string>();

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>Jede versionierte <c>.cs</c> beginnt mit der UTF-8-Signatur.</summary>
        [Fact]
        public void Jede_versionierte_cs_traegt_das_BOM()
        {
            List<string> funde = FundeOhneBom(CsBestand(), Arbeitsbaum(), AusnahmenOhneBom);

            Assert.True(funde.Count == 0,
                "Diese " + funde.Count + " C#-Datei(en) liegen ohne UTF-8-BOM im Repository. " +
                "Die .editorconfig verlangt fuer *.cs 'charset = utf-8-bom' (Auftrag #290); " +
                "ohne die Signatur zerfallen die Umlaute, sobald ein Werkzeug die Datei " +
                "unter einer anderen Kodierungsannahme zurueckschreibt. Zu beheben ist das " +
                "durch VORANSTELLEN der drei Bytes EF BB BF - und durch nichts sonst: keine " +
                "Zeilenenden, keine Formatierung. Schreibt ein Erzeuger die Datei, wird " +
                "ZUERST der Erzeuger umgestellt, sonst ist das BOM beim naechsten Lauf " +
                "wieder fort:\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// Jede versionierte <c>.cs</c> ist gültiges UTF-8. Der Fall fängt den Rückweg ab,
        /// den der BOM-Fall allein nicht sieht: eine Datei, der jemand die Signatur
        /// voranstellt, deren Rumpf aber Windows-1252 geblieben ist. Sie trägt dann ein BOM
        /// und ist trotzdem kaputt — der Compiler liest sie wegen der Signatur als UTF-8
        /// und macht aus jedem Umlaut ein Ersatzzeichen.
        /// </summary>
        [Fact]
        public void Jede_versionierte_cs_ist_gueltiges_UTF8()
        {
            List<string> funde = FundeKeinUtf8(CsBestand(), Arbeitsbaum());

            Assert.True(funde.Count == 0,
                "Diese " + funde.Count + " C#-Datei(en) sind kein gueltiges UTF-8 - " +
                "vermutlich Windows-1252 (Auftrag #290). EIN BOM DAVORZUSETZEN MACHT ES " +
                "SCHLIMMER: Die Datei wird dann als UTF-8 gelesen und jeder Umlaut zum " +
                "Ersatzzeichen. Sie muss umkodiert werden (Windows-1252 lesen, UTF-8 mit " +
                "BOM schreiben) - oder namentlich in AusnahmenOhneBom stehen:\n" +
                string.Join("\n", funde));
        }

        // =====================================================================
        //  Die Regel als Funktion — dieselbe für den Bestand und die Gegenproben
        // =====================================================================

        /// <summary>Alle Dateien einer Liste, die nicht mit der Signatur beginnen (ohne die Ausnahmen).</summary>
        private static List<string> FundeOhneBom(IEnumerable<string> dateien, string wurzel, IEnumerable<string> ausnahmen)
        {
            var frei = new HashSet<string>(ausnahmen, StringComparer.Ordinal);
            var funde = new List<string>();
            foreach (string datei in dateien)
            {
                if (frei.Contains(datei)) continue;
                byte[] kopf = Dateikopf(wurzel, datei, Signatur.Length);
                if (kopf == null) continue;                       // im Arbeitsbaum nicht vorhanden
                if (!kopf.SequenceEqual(Signatur)) funde.Add(datei);
            }
            return funde;
        }

        /// <summary>Alle Dateien einer Liste, die sich nicht streng als UTF-8 lesen lassen.</summary>
        private static List<string> FundeKeinUtf8(IEnumerable<string> dateien, string wurzel)
        {
            var streng = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var funde = new List<string>();
            foreach (string datei in dateien)
            {
                string pfad = Vollpfad(wurzel, datei);
                if (!File.Exists(pfad)) continue;
                byte[] roh = File.ReadAllBytes(pfad);
                try
                {
                    streng.GetString(roh);
                }
                catch (DecoderFallbackException e)
                {
                    funde.Add(datei + "  (Byte " + e.Index + ": 0x" + e.BytesUnknown[0].ToString("X2") + ")");
                }
            }
            return funde;
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über den ganzen
        /// <c>.cs</c>-Bestand — über einer leeren oder halben Liste liefe er grün durch,
        /// ohne je etwas geprüft zu haben. Vier bekannte Dateien aus vier Projekten belegen,
        /// dass er wirklich den Bestand sieht und nicht nur seinen eigenen Ordner.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_ganzen_cs_Bestand()
        {
            string[] bestand = CsBestand();

            Assert.True(bestand.Length > 1000, "Nur " + bestand.Length + " .cs-Dateien gefunden.");
            Assert.Contains("EPOS.Kern/Allgemein/SqliteDatenzugriff.cs", bestand);
            Assert.Contains("EPOS.Kern.Tests/RepositoryOrdnungWacheTests.cs", bestand);
            Assert.Contains("SpeicherEngine/FlottenOptimierer.cs", bestand);
            Assert.Contains("sql/schema/SchemaTypKatalog.g.cs", bestand);
            Assert.DoesNotContain(bestand, p => !p.EndsWith(".cs", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Gegenprobe zum Wächter selbst:</b> In einem eigens angelegten, synthetischen
        /// Baum fällt die BOM-lose Datei auf, die mit BOM nicht, und die Windows-1252-Datei
        /// fällt dem UTF-8-Fall zur Last — nicht dem BOM-Fall. Ohne diesen Fall wäre offen,
        /// ob die Regeln wirklich anschlagen oder nur zufällig auf den heutigen Bestand
        /// passen.
        /// </summary>
        [Fact]
        public void Ein_eingeschmuggelter_Fund_faellt_in_einem_synthetischen_Baum_auf()
        {
            string wurzel = Path.Combine(Path.GetTempPath(), "QuelltextKodierungWache_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(wurzel, "Projekt"));

                // Umlaute im Inhalt - genau daran haengt der ganze Fall.
                const string inhalt = "// Waermepumpe, Brauchwasser, Groesse\r\nclass Gebaeude { }\r\n";
                string mitUmlaut = inhalt.Replace("ae", "ä").Replace("oe", "ö");

                Schreiben(wurzel, "Projekt/MitBom.cs", Signatur.Concat(Encoding.UTF8.GetBytes(mitUmlaut)).ToArray());
                Schreiben(wurzel, "Projekt/OhneBom.cs", Encoding.UTF8.GetBytes(mitUmlaut));
                Schreiben(wurzel, "Projekt/Cp1252.cs", Encoding.Latin1.GetBytes(mitUmlaut));

                string[] dateien = { "Projekt/MitBom.cs", "Projekt/OhneBom.cs", "Projekt/Cp1252.cs" };

                List<string> ohneBom = FundeOhneBom(dateien, wurzel, Array.Empty<string>());
                Assert.Contains("Projekt/OhneBom.cs", ohneBom);
                Assert.Contains("Projekt/Cp1252.cs", ohneBom);
                Assert.DoesNotContain("Projekt/MitBom.cs", ohneBom);

                List<string> keinUtf8 = FundeKeinUtf8(dateien, wurzel);
                Assert.Single(keinUtf8);
                Assert.StartsWith("Projekt/Cp1252.cs", keinUtf8[0], StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(wurzel, recursive: true);
            }
        }

        /// <summary>
        /// <b>Gegenprobe zur Ausnahmeliste:</b> Sie nimmt genau ihren Eintrag heraus und
        /// sonst nichts. Heute ist sie leer; der Fall hält das Werkzeug trotzdem
        /// nachweislich funktionsfähig, damit ein künftiger Eintrag nicht ungeprüft
        /// wirkt — oder, schlimmer, still gar nicht wirkt.
        /// </summary>
        [Fact]
        public void Die_Ausnahmeliste_nimmt_genau_ihren_Eintrag_heraus()
        {
            string wurzel = Path.Combine(Path.GetTempPath(), "QuelltextKodierungWache_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(wurzel, "Projekt"));
                Schreiben(wurzel, "Projekt/Eins.cs", Encoding.UTF8.GetBytes("class Eins { }\r\n"));
                Schreiben(wurzel, "Projekt/Zwei.cs", Encoding.UTF8.GetBytes("class Zwei { }\r\n"));

                string[] dateien = { "Projekt/Eins.cs", "Projekt/Zwei.cs" };

                Assert.Equal(2, FundeOhneBom(dateien, wurzel, Array.Empty<string>()).Count);

                List<string> mitAusnahme = FundeOhneBom(dateien, wurzel, new[] { "Projekt/Eins.cs" });
                Assert.Equal(new[] { "Projekt/Zwei.cs" }, mitAusnahme);

                // Die Liste von heute ist leer - sonst waere der Bestand ungeprueft.
                Assert.Empty(AusnahmenOhneBom);
            }
            finally
            {
                Directory.Delete(wurzel, recursive: true);
            }
        }

        /// <summary>
        /// <b>Gegenprobe zur Liste aus Git:</b> <c>git ls-files -z</c> liefert NUL-getrennte,
        /// unverkürzte Pfade — ohne <c>-z</c> verkürzt Git jeden Pfad mit Sonderzeichen zu
        /// <c>"…\303\244…"</c> in Anführungszeichen, und genau daran vorbei läuft jede
        /// Prüfung, die die Datei danach öffnen will. Neun der versionierten <c>.cs</c>
        /// liegen unter einem Ordner mit Umlaut (<c>Views/Gebäude</c>,
        /// <c>Views/Wärmepumpe</c>). Geht Git in dieser Umgebung nicht, greift der
        /// Rückfallweg — dann bleibt dieser Fall still, der Wächter selbst aber nicht.
        /// </summary>
        [Fact]
        public void Die_Liste_aus_Git_traegt_Pfade_mit_Umlaut_unverkuerzt()
        {
            string[] ausGit = VersionierteDateien(Arbeitsbaum());
            if (ausGit == null) return;   // keine Git-Umgebung - der Rueckfallweg ist geprueft

            string[] cs = NurCs(ausGit);
            Assert.DoesNotContain(cs, p => p.StartsWith("\"", StringComparison.Ordinal));
            Assert.Contains(cs, p => p.Any(z => z > 127));
            Assert.All(cs, p => Assert.True(File.Exists(Vollpfad(Arbeitsbaum(), p)), p + " ist nicht zu oeffnen."));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>Eine Datei im synthetischen Baum anlegen, Ordner inbegriffen.</summary>
        private static void Schreiben(string wurzel, string relativ, byte[] inhalt)
        {
            string pfad = Vollpfad(wurzel, relativ);
            Directory.CreateDirectory(Path.GetDirectoryName(pfad));
            File.WriteAllBytes(pfad, inhalt);
        }

        /// <summary>Repo-relativer Pfad mit '/' zu einem Pfad des laufenden Systems.</summary>
        private static string Vollpfad(string wurzel, string relativ)
            => Path.Combine(wurzel, relativ.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>
        /// Die ersten <paramref name="anzahl"/> Bytes einer Datei; <c>null</c>, wenn sie im
        /// Arbeitsbaum nicht liegt. Eine kürzere Datei liefert entsprechend weniger Bytes —
        /// eine leere <c>.cs</c> trägt kein BOM und ist damit ein Fund.
        /// </summary>
        private static byte[] Dateikopf(string wurzel, string relativ, int anzahl)
        {
            string pfad = Vollpfad(wurzel, relativ);
            if (!File.Exists(pfad)) return null;

            var kopf = new byte[anzahl];
            using FileStream strom = File.OpenRead(pfad);
            int gelesen = strom.Read(kopf, 0, anzahl);
            return gelesen == anzahl ? kopf : kopf.Take(gelesen).ToArray();
        }

        /// <summary>Aus einer Pfadliste nur die <c>.cs</c>, in stabiler Ordnung.</summary>
        private static string[] NurCs(IEnumerable<string> pfade)
            => pfade.Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .ToArray();

        /// <summary>
        /// Der zu prüfende Bestand: alle versionierten <c>.cs</c>, repo-relativ mit '/'.
        /// Erste Wahl ist <c>git ls-files -z</c>; erst wenn sich Git nicht starten lässt,
        /// läuft der Wächter über das Dateisystem.
        /// </summary>
        private static string[] CsBestand()
        {
            string wurzel = Arbeitsbaum();
            string[] ausGit = VersionierteDateien(wurzel);
            return NurCs(ausGit ?? Dateisystembestand(wurzel));
        }

        /// <summary>Ordnernamen, die der Wächter auf dem Rückfallweg nie betritt.</summary>
        private static readonly string[] AusgenommeneOrdnernamen =
            { ".git", "bin", "obj", "node_modules", ".claude", "TestResults" };

        /// <summary>
        /// Die versionierten Dateien aus <c>git ls-files -z</c>, repo-relativ mit '/'.
        /// Liefert <c>null</c>, wenn Git nicht startet oder mit einem Fehler endet.
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

        /// <summary>
        /// Rückfallweg: alle Dateien unter <paramref name="wurzel"/>, rekursiv, repo-relativ
        /// mit '/', ohne die Bauordner und die nicht versionierten Ablagen.
        /// </summary>
        private static string[] Dateisystembestand(string wurzel)
        {
            var dateien = new List<string>();

            string Relativ(string voll)
                => voll.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar, '/')
                       .Replace(Path.DirectorySeparatorChar, '/');

            void Rekursiv(string aktuell)
            {
                foreach (string datei in Directory.EnumerateFiles(aktuell))
                    dateien.Add(Relativ(datei));

                foreach (string unter in Directory.EnumerateDirectories(aktuell))
                {
                    string name = Path.GetFileName(unter);
                    if (AusgenommeneOrdnernamen.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                    Rekursiv(unter);
                }
            }

            Rekursiv(wurzel);
            return dateien.ToArray();
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>RepositoryOrdnungWacheTests</c>.</summary>
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
