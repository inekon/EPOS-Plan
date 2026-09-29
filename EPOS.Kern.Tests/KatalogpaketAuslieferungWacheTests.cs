using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Katalogpakete des Zapfprofilgenerators in der Auslieferung</b> (Anwenderentscheid
    /// 29.09.2026, Regel ZU24) — gehalten am Quelltext, weil das Setup nur unter Windows baut:
    ///
    /// <list type="number">
    ///   <item>Das Setup liefert den <b>freien Paketteil</b> als Ordner aus: die CSV-Dateien von
    ///   <c>Referenzlaeufe/Katalogpaket_frei/</c> nach <c>{app}\Vorlage\Katalogpaket_frei</c>, nach dem
    ///   Muster der A100-Vorlage (Define mit <c>RepoDir</c>, <c>#error</c> bei fehlendem Ordner,
    ///   dieselben Flags und dieselbe Komponente) — ohne die <c>LIESMICH.md</c>.</item>
    ///   <item>Die <b>A100-Vorlage</b> (Platzhalter) bleibt, wie sie ist, unter
    ///   <c>{app}\Vorlage\Katalogpaket_A100</c>.</item>
    ///   <item>Nichts aus den <b>lokalen Normdaten</b> (<c>Referenzlaeufe/Normzahlen/</c>, dort liegt auch
    ///   das gefüllte A100-Paket) gerät in das Setup, in die Bauanleitung des Setups oder in die
    ///   eingebetteten Ressourcen des Kerns; die Auslieferungsvorlage verweigert eine Eingabe von
    ///   dort ohnehin (ihre Prüfung, <c>TwwVorlageTests.T7</c>).</item>
    /// </list>
    /// </summary>
    public sealed class KatalogpaketAuslieferungWacheTests
    {
        private const string NORMZAHLEN = "Normzahlen";

        [Fact]
        public void Das_Setup_liefert_den_freien_Paketteil_als_Ordner_nach_dem_Muster_der_A100_Vorlage()
        {
            string iss = Lesen("Setup", "EPOS-Plan.iss");

            Assert.Matches(new Regex(@"^#define KatalogpaketFrei\s+RepoDir \+ ""Referenzlaeufe\\Katalogpaket_frei""\s*$", RegexOptions.Multiline), iss);
            Assert.Matches(new Regex(@"^#if !DirExists\(KatalogpaketFrei\)\s*\n\s*#error [^\n]+\n#endif", RegexOptions.Multiline), iss);
            Assert.Matches(new Regex(@"^#define KatalogVorlageA100\s+RepoDir \+ ""Referenzlaeufe\\Katalogpaket_Vorlage_A100""\s*$", RegexOptions.Multiline), iss);

            Match frei = Eintrag(iss, "KatalogpaketFrei");
            Match a100 = Eintrag(iss, "KatalogVorlageA100");
            Assert.True(frei.Success, "Kein [Files]-Eintrag fuer {#KatalogpaketFrei}.");
            Assert.True(a100.Success, "Kein [Files]-Eintrag fuer {#KatalogVorlageA100}.");
            Assert.Equal(@"\*.csv", frei.Groups["muster"].Value);                 // nur die CSV-Dateien, keine LIESMICH.md
            Assert.Equal(@"{app}\Vorlage\Katalogpaket_frei", frei.Groups["ziel"].Value);
            Assert.Equal(@"\*", a100.Groups["muster"].Value);
            Assert.Equal(@"{app}\Vorlage\Katalogpaket_A100", a100.Groups["ziel"].Value);
            Assert.Equal(a100.Groups["rest"].Value, frei.Groups["rest"].Value);   // dieselben Flags, dieselbe Komponente

            string ordner = Pfad("Referenzlaeufe", "Katalogpaket_frei");
            Assert.True(Directory.GetFiles(ordner, "*.csv").Length >= 7, "Der freie Paketteil fuehrt keine sieben CSV-Dateien.");
            Assert.True(File.Exists(Path.Combine(ordner, "LIESMICH.md")), "Die LIESMICH.md des freien Paketteils fehlt.");
        }

        [Fact]
        public void Nichts_aus_den_lokalen_Normdaten_geraet_in_Setup_oder_Kernressourcen()
        {
            foreach (string[] datei in new[]
                     {
                         new[] { "Setup", "EPOS-Plan.iss" },
                         new[] { "Setup", "build-setup.ps1" },
                         new[] { "EPOS.Kern", "EPOS.Kern.csproj" }
                     })
            {
                string[] zeilen = Lesen(datei).Replace("\r\n", "\n").Split('\n');
                string[] funde = zeilen.Select((z, i) => (z, i))
                                       .Where(x => x.z.Contains(NORMZAHLEN, StringComparison.OrdinalIgnoreCase) && !Kommentar(x.z))
                                       .Select(x => string.Join("/", datei) + ":" + (x.i + 1) + "  " + x.z.Trim()).ToArray();
                Assert.True(funde.Length == 0, "Die lokalen Normdaten gehoeren nie in die Auslieferung (ZU24):\n" + string.Join("\n", funde));
            }
            Assert.DoesNotContain(typeof(WindowsFormsApplication1.TwwPaketteilCtrl).Assembly.GetManifestResourceNames(),
                                  n => n.Contains(NORMZAHLEN, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Ein Eintrag der Sektion [Files] zu einem Define: Muster, Ziel und der Rest (Flags, Komponente).</summary>
        private static Match Eintrag(string iss, string define)
            => new Regex(@"^Source: ""\{#" + define + @"\}(?<muster>[^""]*)""; DestDir: ""(?<ziel>[^""]*)"";(?<rest>(?:[^\n]*\\\n)*[^\n]*)$",
                         RegexOptions.Multiline).Match(iss.Replace("\r\n", "\n"));

        /// <summary>Kommentarzeile in Inno Setup (<c>;</c>), PowerShell (<c>#</c>, aber kein <c>#define</c>/<c>#if</c>) oder XML.</summary>
        private static bool Kommentar(string zeile)
        {
            string z = zeile.TrimStart();
            return z.StartsWith(";", StringComparison.Ordinal)
                   || (z.StartsWith("#", StringComparison.Ordinal) && !z.StartsWith("#define", StringComparison.Ordinal)
                       && !z.StartsWith("#if", StringComparison.Ordinal) && !z.StartsWith("#error", StringComparison.Ordinal))
                   || z.StartsWith("<!--", StringComparison.Ordinal) || z.StartsWith("//", StringComparison.Ordinal);
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
