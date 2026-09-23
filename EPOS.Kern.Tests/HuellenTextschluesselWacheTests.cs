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
    /// <b>Die Wache über die Textschlüssel der Hüllen</b>: Jeder Schlüssel, den eine Hülle als
    /// LITERAL an <c>Text_("…", …)</c> gibt, steht in BEIDEN Ressourcendateien mit einem Text.
    ///
    /// <para><b>Warum.</b> <c>Text_</c> fällt still auf den deutschen Rückfall im Quelltext
    /// zurück, wenn der Schlüssel fehlt — die Oberfläche bleibt dann auch in Englisch deutsch, und
    /// kein Test merkt es. Die Bündelwachen (<c>ZapfprofilDatenTests</c>,
    /// <c>ZapfprofilAuslegungDatenTests</c>) sehen nur Schlüssel eines Textbündels, nicht die
    /// Sätze, die eine Hülle selbst bildet (Sperrgründe, Herleitungen, Meldungen).</para>
    ///
    /// <para><b>Geltungsbereich</b> sind die plattformfreien Hüllen unter <c>EPOS.UI.Daten/</c>
    /// und die Hüllen der Windows-Schale unter <c>WindowsFormsApplication1/</c>. Ein
    /// zusammengesetzter Schlüssel (<c>Text_("ZPG_AUS_TOPOLOGIE_" + …)</c>) ist kein Literal; ihn
    /// prüfen die Wachen je Aufzählung (<c>ZapfprofilAuslegungHuelleTests</c>). Gelesen wird
    /// Quelltext, die Ressourcen wie in <c>ZapfprofilHuelleTests</c> mit einem regulären Ausdruck.</para>
    /// </summary>
    public sealed class HuellenTextschluesselWacheTests
    {
        private static readonly Regex LiteralSchluessel = new(@"\bText_\(\s*""(?<k>[^""]+)""\s*[,)]", RegexOptions.Compiled);

        [Fact]
        public void Jeder_Literalschluessel_der_Huellen_steht_in_beiden_Sprachen()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");

            var fundorte = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            int aufrufe = 0;
            foreach (string datei in Quelldateien())
                foreach (Match m in LiteralSchluessel.Matches(File.ReadAllText(datei)))
                {
                    aufrufe++;
                    string k = m.Groups["k"].Value;
                    if (!fundorte.TryGetValue(k, out SortedSet<string> orte)) fundorte[k] = orte = new SortedSet<string>(StringComparer.Ordinal);
                    orte.Add(Path.GetFileName(datei));
                }

            // Gegenprobe: Die Wache sieht die Hüllen überhaupt — auch die der Auslegung.
            Assert.True(aufrufe >= 1000, "Nur " + aufrufe + " Literalaufrufe gefunden.");
            Assert.Contains("ZPG_AUS_GRUND_NORMTAG", fundorte.Keys);
            Assert.Contains("ZPG_AUS_TITEL", fundorte.Keys);

            var funde = new List<string>();
            foreach (KeyValuePair<string, SortedSet<string>> f in fundorte)
            {
                string wo = " (" + string.Join(", ", f.Value) + ")";
                if (!de.TryGetValue(f.Key, out string d) || d.Trim().Length == 0) funde.Add(f.Key + ": fehlt in Resource.resx" + wo);
                if (!en.TryGetValue(f.Key, out string e) || e.Trim().Length == 0) funde.Add(f.Key + ": fehlt in Resource.en-US.resx" + wo);
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        /// <summary>Die Quelltexte der Hüllen: <c>EPOS.UI.Daten</c> und <c>WindowsFormsApplication1</c>, ohne <c>bin</c>/<c>obj</c>.</summary>
        private static IEnumerable<string> Quelldateien()
        {
            foreach (string projekt in new[] { "EPOS.UI.Daten", "WindowsFormsApplication1" })
                foreach (string datei in Directory.GetFiles(Path.Combine(Wurzel(), projekt), "*.cs", SearchOption.AllDirectories))
                {
                    string[] teile = datei.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (teile.Contains("bin") || teile.Contains("obj")) continue;
                    yield return datei;
                }
        }

        private static Dictionary<string, string> Resx(string datei)
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", datei));
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
