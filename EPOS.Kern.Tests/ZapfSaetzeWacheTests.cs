using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache der Sätze des Zapfprofilgenerators</b> (Umsetzungskonzept Zapfprofilgenerator,
    /// N11 (k); Stufe Z4): Jede Ablehnung, jeder Hinweis und jede Warnung des Kerns ist ein
    /// <see cref="ZapfSatz"/> — Kennung und Werte, kein fertiger Satz. Gelesen aus dem QUELLTEXT
    /// des Kerns (<c>ZapfSatz.Neu(…)</c>: Literale, benannte Konstanten, die drei Begriffsfamilien
    /// Tagtyp, Füllstand und Bezugsart), damit eine neue Kennung auffällt:
    /// <list type="bullet">
    /// <item>jede Kennung hat ihr Muster <c>ZPG_SATZ_…</c> in BEIDEN Sprachen mit denselben
    /// Platzhaltern samt Zahlformat;</item>
    /// <item>jedes Muster <c>ZPG_SATZ_…</c> gehört zu einer Kennung des Kerns — kein toter Schlüssel;</item>
    /// <item>im Zapfprofil-Ordner steht kein fertiger Satz mehr („Nicht rechenbar —" als Literal).</item>
    /// </list>
    /// </summary>
    public sealed class ZapfSaetzeWacheTests
    {
        private static readonly Regex Aufruf = new(@"ZapfSatz\.Neu\(\s*(?<a>[^,;)]*)", RegexOptions.Compiled);
        private static readonly Regex Literal = new(@"""(?<k>[A-Z][A-Z0-9_]*)""(?<plus>\s*\+)?", RegexOptions.Compiled);
        private static readonly Regex Konstante = new(@"const\s+string\s+(?<n>[A-Z][A-Z0-9_]*)\s*=\s*""(?<w>[^""]*)""", RegexOptions.Compiled);
        private static readonly Regex Bezeichner = new(@"^(?:[A-Za-z0-9_]+\.)*(?<n>[A-Z][A-Z0-9_]*)$", RegexOptions.Compiled);

        /// <summary>Die Begriffsfamilien mit berechneter Kennung — je Präfix die Kennungen aller Werte.</summary>
        private static readonly Dictionary<string, string[]> Familien = new()
        {
            ["BEGRIFF_TAGTYP_"] = Enum.GetValues(typeof(ZapfTagtyp)).Cast<ZapfTagtyp>().Select(t => Formvektor.Tagtyp(t).Kennung).ToArray(),
            ["BEGRIFF_FUELLSTAND_"] = Enum.GetValues(typeof(ZapfFuellstandbezug)).Cast<ZapfFuellstandbezug>()
                                         .Select(b => TwwSpeicherauslegung.Fuellstandbegriff(b).Kennung).ToArray(),
            ["BEGRIFF_BEZUGSART_"] = Enum.GetValues(typeof(ZapfBezugsart)).Cast<ZapfBezugsart>()
                                        .Select(b => ZapfprofilAuslegung.Bezugsartbegriff(b).Kennung).ToArray()
        };

        [Fact]
        public void Jede_Kennung_des_Kerns_hat_ihr_Muster_in_beiden_Sprachen()
        {
            SortedSet<string> kennungen = Kennungen(out List<string> unaufgeloest);
            Assert.True(unaufgeloest.Count == 0, "Nicht auflösbare Kennungen in ZapfSatz.Neu: " + string.Join(", ", unaufgeloest));
            Assert.True(kennungen.Count >= 300, "Nur " + kennungen.Count + " Kennungen gefunden.");
            Assert.Contains("EINGABE_NUTZUNGSART_FEHLT", kennungen);
            Assert.Contains("BEGRIFF_TAGTYP_1", kennungen);
            Assert.Contains("BEGRIFF_BEZUGSART_7", kennungen);

            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            var funde = new List<string>();
            foreach (string k in kennungen)
            {
                string schluessel = ZapfSatz.PRAEFIX + k;
                if (!de.TryGetValue(schluessel, out string d) || d.Trim().Length == 0) { funde.Add(schluessel + ": fehlt deutsch"); continue; }
                if (!en.TryGetValue(schluessel, out string e) || e.Trim().Length == 0) { funde.Add(schluessel + ": fehlt englisch"); continue; }
                if (!Platzhalter(d).SequenceEqual(Platzhalter(e))) funde.Add(schluessel + ": Platzhalter weichen ab");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        [Fact]
        public void Jedes_Muster_gehoert_zu_einer_Kennung_des_Kerns()
        {
            SortedSet<string> kennungen = Kennungen(out _);
            string[] tot = Resx("Resource.resx").Keys
                .Where(k => k.StartsWith(ZapfSatz.PRAEFIX, StringComparison.Ordinal))
                .Where(k => !kennungen.Contains(k.Substring(ZapfSatz.PRAEFIX.Length)))
                .OrderBy(k => k, StringComparer.Ordinal).ToArray();
            Assert.True(tot.Length == 0, "Muster ohne Kennung des Kerns: " + string.Join(", ", tot));
        }

        [Fact]
        public void Im_Zapfprofil_Ordner_steht_kein_fertiger_Satz()
        {
            var funde = new List<string>();
            foreach (string datei in Quellen().Where(d => Path.GetDirectoryName(d).EndsWith("Zapfprofil", StringComparison.Ordinal)
                                                          || Path.GetFileName(d).StartsWith("Zapfprofil", StringComparison.Ordinal)
                                                          || Path.GetFileName(d).StartsWith("Tww", StringComparison.Ordinal)))
            {
                string[] zeilen = File.ReadAllLines(datei);
                for (int i = 0; i < zeilen.Length; i++)
                {
                    string z = zeilen[i].TrimStart();
                    if (z.StartsWith("//", StringComparison.Ordinal)) continue;
                    if (z.Contains("\"Nicht rechenbar", StringComparison.Ordinal) || z.Contains("\"Hinweis:", StringComparison.Ordinal))
                        funde.Add(Path.GetFileName(datei) + ":" + (i + 1));
                }
            }
            Assert.True(funde.Count == 0, "Fertige Sätze statt ZapfSatz: " + string.Join(", ", funde));
        }

        /// <summary>Der Klartext bleibt deutsch und invariant; die Oberfläche setzt ihre Kultur ein.</summary>
        [Fact]
        public void Klartext_ist_deutsch_und_invariant_die_Oberflaeche_formatiert_in_ihrer_Kultur()
        {
            ZapfSatz s = ZapfSatz.Neu("HINWEIS_SUMME_NORMIERT", Formvektor.Tagtyp(ZapfTagtyp.Werktag), "Wohnen", 1.25);
            Assert.StartsWith("Werktag", s.Klartext, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1.25", s.Klartext);
            string en = s.Text(k => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(k, CultureInfo.GetCultureInfo("en-US")),
                               CultureInfo.GetCultureInfo("de-DE"));
            Assert.Contains("1,25", en);
            Assert.DoesNotContain("Zone „", en);
            Assert.Equal(s, ZapfSatz.Neu("HINWEIS_SUMME_NORMIERT", Formvektor.Tagtyp(ZapfTagtyp.Werktag), "Wohnen", 1.25));
            Assert.Equal("UNBEKANNT (1; x)", ZapfSatz.Neu("UNBEKANNT", 1, "x").Klartext);
        }

        // =================================================================================

        private static SortedSet<string> Kennungen(out List<string> unaufgeloest)
        {
            var konstanten = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] texte = Quellen().Select(File.ReadAllText).ToArray();
            foreach (string t in texte)
                foreach (Match m in Konstante.Matches(t))
                    konstanten[m.Groups["n"].Value] = m.Groups["w"].Value;

            var kennungen = new SortedSet<string>(StringComparer.Ordinal);
            unaufgeloest = new List<string>();
            foreach (string t in texte)
                foreach (Match m in Aufruf.Matches(t))
                {
                    string a = m.Groups["a"].Value.Trim();
                    MatchCollection literale = Literal.Matches(a);
                    if (literale.Count > 0)
                    {
                        foreach (Match l in literale)
                        {
                            string k = l.Groups["k"].Value;
                            if (!l.Groups["plus"].Success) { kennungen.Add(k); continue; }
                            if (Familien.TryGetValue(k, out string[] familie)) kennungen.UnionWith(familie);
                            else unaufgeloest.Add(a);
                        }
                        continue;
                    }
                    Match b = Bezeichner.Match(a);
                    if (b.Success && konstanten.TryGetValue(b.Groups["n"].Value, out string wert)) kennungen.Add(wert);
                    else unaufgeloest.Add(a);
                }
            return kennungen;
        }

        /// <summary>Die Quellen des Kerns, die Sätze bauen: Zapfprofil-Ordner und Controller.</summary>
        private static IEnumerable<string> Quellen()
            => new[] { Pfad("EPOS.Kern", "Allgemein", "Zapfprofil"), Pfad("EPOS.Kern", "Controller") }
               .SelectMany(o => Directory.GetFiles(o, "*.cs"));

        private static string[] Platzhalter(string text)
            => Regex.Matches(text ?? "", @"\{\d+(:[^}]*)?\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

        private static Dictionary<string, string> Resx(string datei)
        {
            string text = File.ReadAllText(Pfad("EPOS.Kern", "MyResource", datei));
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

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
