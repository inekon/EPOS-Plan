using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Katalogpaket der Fassung in der Setup-Kette</b> (Konzept Setup, Abschnitt 6.5.3,
    /// Schritte 1, 4 und 6) — gehalten am Quelltext, weil das Setup nur unter Windows baut:
    ///
    /// <list type="number">
    ///   <item>Das <c>.iss</c> bricht ohne <c>Setup\Vorlage\Katalogpaket.json</c> mit <c>#error</c> ab
    ///   und liefert die Datei nach <c>{app}\Vorlage</c> aus, mit denselben Flags
    ///   (<c>ignoreversion</c>) und derselben Komponente wie die Vorlagendatenbank.</item>
    ///   <item><c>Setup/build-setup.ps1</c> löscht ein altes Paket vor dem Werkzeuglauf (nicht im
    ///   Trockenlauf), reicht die Fassung des Katalogpakets als <c>--katalogfassung</c> durch, prüft
    ///   das Paket nach dem Lauf samt Fassung im Kopf und im Trockenlauf das vorhandene Paket;
    ///   <c>.gitignore</c> hält das Paket aus dem Repository.</item>
    ///   <item>Das Freigaberegister <c>Setup/Katalogfassungen.txt</c> (Entscheide E1, E4) führt
    ///   Fassungen der Form <c>JJJJMMTTnn</c>, die streng wachsen und in den Ganzzahltyp passen; das
    ///   Skript liest es, hält jede Fassung außerhalb eines Probelaufs dagegen und trägt eine Freigabe
    ///   erst nach dem Übersetzen ein.</item>
    ///   <item>Der Job <c>installer</c> in <c>windows.yml</c> läuft als Probe und legt nur den Kopf des
    ///   Pakets als <c>katalogpaket.txt</c> ins Artefakt (Entscheid E5).</item>
    /// </list>
    /// </summary>
    public sealed class KatalogpaketSetupWacheTests
    {
        private static readonly Regex REGISTERZEILE =
            new(@"^(?<fassung>\d{10})\s+(?<datum>\d{4}-\d{2}-\d{2})\s+(?<version>\S+)$");

        [Fact]
        public void Das_Setup_bricht_ohne_Paket_ab_und_liefert_es_wie_die_Vorlage_nach_Vorlage_aus()
        {
            string iss = Lesen("Setup", "EPOS-Plan.iss").Replace("\r\n", "\n");

            Assert.Matches(new Regex(@"^#define Katalogpaket\s+SetupDir \+ ""Vorlage\\Katalogpaket\.json""\s*$", RegexOptions.Multiline), iss);
            Assert.Matches(new Regex(@"^#if !FileExists\(Katalogpaket\)\s*\n\s*#error [^\n]+\n#endif", RegexOptions.Multiline), iss);

            Match paket = Eintrag(iss, "Katalogpaket");
            Match vorlage = Eintrag(iss, "VorlageDb");
            Assert.True(paket.Success, "Kein [Files]-Eintrag fuer {#Katalogpaket}.");
            Assert.True(vorlage.Success, "Kein [Files]-Eintrag fuer {#VorlageDb}.");
            Assert.Equal(@"{app}\Vorlage", paket.Groups["ziel"].Value);
            Assert.Equal(vorlage.Groups["rest"].Value, paket.Groups["rest"].Value);   // dieselben Flags, dieselbe Komponente
            Assert.Contains("Flags: ignoreversion", paket.Groups["rest"].Value);
            Assert.Contains("Components: programm", paket.Groups["rest"].Value);
        }

        [Fact]
        public void Das_Skript_loescht_das_alte_Paket_reicht_die_Fassung_durch_und_prueft_das_Paket()
        {
            string ps = Code(Lesen("Setup", "build-setup.ps1"));

            // Pfad passt zum #define Katalogpaket im .iss.
            Assert.Matches(new Regex(@"^\$Katalogpaket\s*=\s*Join-Path \$SetupDir 'Vorlage\\Katalogpaket\.json'", RegexOptions.Multiline), ps);

            // Parameter samt Umgebungsvariable.
            Assert.Matches(new Regex(@"^\s*\[string\] \$Katalogfassung,", RegexOptions.Multiline), ps);
            Assert.Matches(new Regex(@"^\s*\[switch\] \$Probe,", RegexOptions.Multiline), ps);
            Assert.Contains("$env:EPOS_KATALOGFASSUNG", ps);

            int werkzeuglauf = Stelle(ps, "& dotnet run --project $VorlageWerkzeug");
            int loeschen = Stelle(ps, "if ((-not $VorlageNurPruefen) -and (Test-Path $Katalogpaket)) { Remove-Item $Katalogpaket -Force }");
            int durchreichen = Stelle(ps, "$vorlageArgs += '--katalogfassung'; $vorlageArgs += $KatalogfassungWert");
            Assert.True(loeschen < werkzeuglauf, "Das alte Katalogpaket muss VOR dem Werkzeuglauf geloescht werden.");
            Assert.True(durchreichen < werkzeuglauf, "--katalogfassung muss in die Argumente des Werkzeuglaufs.");

            string danach = ps.Substring(werkzeuglauf);
            Assert.Contains("if (-not (Test-Path $Katalogpaket))", danach);
            Assert.Contains("$paketKopf.Fassung -ne $KatalogfassungWert", danach);
            int trocken = Stelle(danach, "Trockenlauf ohne vorhandenes Katalogpaket");
            Assert.True(danach.IndexOf("KatalogpaketKopf $Katalogpaket", trocken, StringComparison.Ordinal) > trocken,
                        "Der Trockenlauf muss den Kopf des vorhandenen Pakets pruefen.");
        }

        [Fact]
        public void Gitignore_haelt_das_Paket_aus_dem_Repository()
        {
            string[] zeilen = Lesen(".gitignore").Replace("\r\n", "\n").Split('\n');
            Assert.Contains("Setup/Vorlage/Katalogpaket.json", zeilen.Select(z => z.Trim()));
        }

        [Fact]
        public void Das_Freigaberegister_waechst_streng_und_fuehrt_nur_Fassungen_JJJJMMTTnn()
        {
            var fassungen = new List<long>();
            foreach (string roh in Lesen("Setup", "Katalogfassungen.txt").Replace("\r\n", "\n").Split('\n'))
            {
                string z = roh.Trim();
                if (z.Length == 0 || z.StartsWith('#')) continue;
                Match m = REGISTERZEILE.Match(z);
                Assert.True(m.Success, "Unlesbare Zeile im Freigaberegister: " + z);
                long f = long.Parse(m.Groups["fassung"].Value, CultureInfo.InvariantCulture);
                Assert.True(GueltigeFassung(f), "Keine Fassung JJJJMMTTnn im Ganzzahltyp: " + f);
                Assert.True(DateTime.TryParseExact(m.Groups["datum"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                                   DateTimeStyles.None, out _), "Kein Datum: " + z);
                Assert.True(fassungen.Count == 0 || f > fassungen[^1], "Das Register waechst nicht streng: " + z);
                fassungen.Add(f);
            }
        }

        [Fact]
        public void Das_Skript_liest_das_Register_haelt_die_Fassung_dagegen_und_traegt_erst_nach_dem_Uebersetzen_ein()
        {
            string ps = Code(Lesen("Setup", "build-setup.ps1"));

            Assert.Matches(new Regex(@"^\$Freigaberegister\s*=\s*Join-Path \$SetupDir 'Katalogfassungen\.txt'", RegexOptions.Multiline), ps);
            Assert.Contains("$RegisterFassungen = FreigaberegisterLesen $Freigaberegister", ps);
            Assert.Contains("NaechsteKatalogfassung $RegisterFassungen $Heute", ps);

            // Die Regel: groesser als die letzte Freigabe und als die heutige Datumsfassung JJJJMMTT.
            string regel = Funktion(ps, "KatalogfassungGegenRegister");
            Assert.Contains("$Fassung -le $Fassungen[$Fassungen.Count - 1]", regel);
            Assert.Contains("[long] $Fassung -le [long] $Heute", regel);
            string form = Funktion(ps, "KatalogfassungLesen");
            Assert.Contains(@"'^\d{10}$'", form);
            Assert.Contains("[int]::MaxValue", form);
            string register = Funktion(ps, "FreigaberegisterLesen");
            Assert.Contains("$f -le $fassungen[$fassungen.Count - 1]", register);   // streng wachsend auch beim Lesen

            // Gegen das Register gehalten wird nur ausserhalb eines Probe- oder Trockenlaufs.
            Assert.Matches(new Regex(@"if \(\$Probe -or \$VorlageNurPruefen\) \{[^}]*\}\s*else \{\s*KatalogfassungGegenRegister \$KatalogfassungWert \$RegisterFassungen \$Heute",
                                     RegexOptions.Singleline), ps);

            // Eingetragen wird erst nach ISCC und Signieren und nie im Probe- oder Trockenlauf.
            int iscc = Stelle(ps, "& $Iscc @isccArgs");
            int signieren = Stelle(ps, "if ($Sign) {");
            int eintragen = Stelle(ps, "FreigabeEintragen $Freigaberegister $KatalogfassungWert $Version");
            Assert.True(eintragen > iscc && eintragen > signieren, "Die Freigabe darf erst nach Uebersetzen und Signieren eingetragen werden.");
            const string NUR_FREIGABE = "if (-not ($Probe -or $VorlageNurPruefen)) {";
            int bedingung = ps.LastIndexOf(NUR_FREIGABE, eintragen, StringComparison.Ordinal);
            Assert.True(bedingung > signieren, "Der Eintrag muss unter '" + NUR_FREIGABE + "' stehen.");
            Assert.DoesNotContain("\n}", ps.Substring(bedingung, eintragen - bedingung));
            Assert.Equal(1, Regex.Matches(ps, @"^\s*FreigabeEintragen ", RegexOptions.Multiline).Count);
        }

        [Fact]
        public void Fassungen_JJJJMMTTnn_passen_bis_2147_in_den_Ganzzahltyp_von_Werkzeug_und_Kern()
        {
            Assert.True(GueltigeFassung(2026100601));
            Assert.True(GueltigeFassung(2147123199));
            Assert.False(GueltigeFassung(2147133201));              // Monat 13: kein Datum
            Assert.False(GueltigeFassung(2026100600));              // Tageslauf ab 01
            Assert.False(GueltigeFassung(20261006));                // die alte Datumsfassung JJJJMMTT
            Assert.True(2026100601L > 99_991_231L, "Jede Fassung JJJJMMTTnn ist groesser als jede Datumsfassung JJJJMMTT.");
        }

        [Fact]
        public void Der_Installer_Job_laeuft_als_Probe_und_legt_den_Kopf_des_Pakets_ins_Artefakt()
        {
            string yml = Lesen(".github", "workflows", "windows.yml").Replace("\r\n", "\n");
            int job = Stelle(yml, "\n  installer:\n");
            string installer = yml.Substring(job);

            Assert.Matches(new Regex(@"\$aufruf = @\('-File', 'Setup/build-setup\.ps1', '-Quelldatenbank', \$quelle, '-Probe'\)"), installer);

            int schritt = Stelle(installer, "- name: Kopf des Katalogpakets");
            int naechster = installer.IndexOf("\n      - name:", schritt + 1, StringComparison.Ordinal);
            string kopf = installer.Substring(schritt, (naechster < 0 ? installer.Length : naechster) - schritt);
            Assert.Contains("if: always()", kopf);
            Assert.Contains("$paket = 'Setup/Vorlage/Katalogpaket.json'", kopf);
            Assert.Contains("$p.Katalogfassung", kopf);
            Assert.Contains("$t.Saetze", kopf);
            Assert.Contains("artifacts/setup/katalogpaket.txt", kopf);
            Assert.DoesNotContain("Copy-Item $paket", kopf);   // nur der Kopf, nicht das Paket (E5)

            int artefakt = Stelle(installer, "name: setup-protokoll");
            Assert.True(schritt < artefakt, "Der Kopf muss vor dem Hochladen von artifacts/setup entstehen.");
        }

        // ---------------------------------------------------------------------------------

        /// <summary>Eine Fassung <c>JJJJMMTTnn</c>: gültiges Datum, Tageslauf 01 bis 99, im Ganzzahltyp.</summary>
        private static bool GueltigeFassung(long f)
        {
            if (f < 1_000_000_000L || f > int.MaxValue) return false;
            string s = f.ToString(CultureInfo.InvariantCulture);
            return DateTime.TryParseExact(s.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                   && int.Parse(s.Substring(8, 2), CultureInfo.InvariantCulture) >= 1;
        }

        /// <summary>Ein Eintrag der Sektion [Files] zu einem Define: Muster, Ziel und der Rest (Flags, Komponente).</summary>
        private static Match Eintrag(string iss, string define)
            => new Regex(@"^Source: ""\{#" + define + @"\}(?<muster>[^""]*)""; DestDir: ""(?<ziel>[^""]*)"";(?<rest>(?:[^\n]*\\\n)*[^\n]*)$",
                         RegexOptions.Multiline).Match(iss);

        /// <summary>Der PowerShell-Text ohne Kommentarzeilen und ohne den Hilfeblock, mit LF.</summary>
        private static string Code(string ps)
        {
            string text = ps.Replace("\r\n", "\n");
            int hilfeEnde = text.IndexOf("#>", StringComparison.Ordinal);
            if (hilfeEnde >= 0) text = text.Substring(hilfeEnde + 2);
            return string.Join("\n", text.Split('\n').Where(z => !z.TrimStart().StartsWith('#')));
        }

        /// <summary>Der Rumpf einer PowerShell-Funktion bis zur nächsten Funktion.</summary>
        private static string Funktion(string ps, string name)
        {
            int start = Stelle(ps, "function " + name + "(");
            int ende = ps.IndexOf("\nfunction ", start + 1, StringComparison.Ordinal);
            return ps.Substring(start, (ende < 0 ? ps.Length : ende) - start);
        }

        private static int Stelle(string text, string teil)
        {
            int i = text.IndexOf(teil, StringComparison.Ordinal);
            Assert.True(i >= 0, "Nicht gefunden: " + teil);
            return i;
        }

        private static string Lesen(params string[] teile) => File.ReadAllText(Pfad(teile));

        private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
