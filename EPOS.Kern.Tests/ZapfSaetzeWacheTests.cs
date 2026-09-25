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
    /// des Kerns (<c>ZapfSatz.Neu(…)</c>: Literale, benannte Konstanten, die fünf Begriffsfamilien
    /// Tagtyp, Füllstand, Bezugsart, Einheit und Tabelle), damit eine neue Kennung auffällt:
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
                                        .Select(b => ZapfprofilAuslegung.Bezugsartbegriff(b).Kennung).ToArray(),
            ["BEGRIFF_EINHEIT_"] = Enum.GetValues(typeof(ZapfBezugsart)).Cast<ZapfBezugsart>()
                                      .Select(b => Schaetzhilfe.Einheitbegriff(b).Kennung).ToArray(),
            ["BEGRIFF_TABELLE_"] = TwwSchema.AlleAnweisungen.Select(a => ((ZapfSatz)ZapfSatz.Tabelle(a.Key)).Kennung).ToArray(),
            ["BEGRIFF_TYPTAG_JAHRESZEIT_"] = Enum.GetValues(typeof(Typtagjahreszeit)).Cast<Typtagjahreszeit>()
                                                 .Select(j => Typtagzuordnung.Jahreszeitbegriff(j).Kennung).ToArray(),
            ["BEGRIFF_TYPTAG_TAGART_"] = Enum.GetValues(typeof(Typtagart)).Cast<Typtagart>()
                                             .Select(t => Typtagzuordnung.Tagartbegriff(t).Kennung).ToArray(),
            ["BEGRIFF_TYPTAG_BEWOELKUNG_"] = Enum.GetValues(typeof(Typtagbewoelkung)).Cast<Typtagbewoelkung>()
                                                 .Select(b => Typtagzuordnung.Bewoelkungsbegriff(b).Kennung).ToArray()
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

        /// <summary>
        /// <b>Je Aufruf so viele Werte wie Platzhalter</b>: Jedes <c>ZapfSatz.Neu(Kennung, w0, …)</c> im
        /// Quelltext des Kerns trägt genau die Werte, deren Platzhalter <c>{0}</c> … <c>{n-1}</c> das
        /// Muster in BEIDEN Sprachen benutzt — keiner fehlt, keiner bleibt übrig. Gezählt werden die
        /// Argumente auf oberster Klammerebene (Zeichenketten und Kommentare ausgenommen); eine
        /// Begriffsfamilie gilt für jedes ihrer Glieder.
        /// </summary>
        [Fact]
        public void Jeder_Aufruf_traegt_so_viele_Werte_wie_sein_Muster_Platzhalter()
        {
            var konstanten = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] texte = Quellen().Select(File.ReadAllText).ToArray();
            foreach (string t in texte)
                foreach (Match m in Konstante.Matches(t))
                    konstanten[m.Groups["n"].Value] = m.Groups["w"].Value;
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");

            var funde = new List<string>();
            int aufrufe = 0;
            const string AUFRUF = "ZapfSatz.Neu(";
            foreach (string datei in Quellen())
            {
                string text = OhneKommentare(File.ReadAllText(datei));
                for (int i = text.IndexOf(AUFRUF, StringComparison.Ordinal); i >= 0; i = text.IndexOf(AUFRUF, i + 1, StringComparison.Ordinal))
                {
                    List<string> args = Argumente(text, i + AUFRUF.Length);
                    Assert.True(args != null && args.Count > 0, Path.GetFileName(datei) + ": ZapfSatz.Neu ohne schließende Klammer");
                    int werte = args.Count - 1;
                    aufrufe++;
                    foreach (string k in Aufgeloest(args[0].Trim(), konstanten))
                        foreach ((string sprache, Dictionary<string, string> d) in new[] { ("de", de), ("en", en) })
                        {
                            if (!d.TryGetValue(ZapfSatz.PRAEFIX + k, out string muster)) continue;   // Sache der ersten Wache
                            int[] indizes = Regex.Matches(muster, @"\{(?<i>\d+)(?::[^}]*)?\}").Select(m => int.Parse(m.Groups["i"].Value))
                                                 .Distinct().OrderBy(x => x).ToArray();
                            if (!indizes.SequenceEqual(Enumerable.Range(0, werte)))
                                funde.Add(Path.GetFileName(datei) + ": " + k + " (" + sprache + ") trägt " + werte + " Wert(e), das Muster {"
                                          + string.Join(",", indizes) + "}");
                        }
                }
            }
            Assert.True(aufrufe >= 300, "Nur " + aufrufe + " Aufrufe gefunden.");
            // Gegenprobe des Zählers: Kommas in Klammern, Zeichenketten und Kommentaren trennen nicht.
            Assert.Equal(4, Argumente(OhneKommentare("\"K\", f(a, b), \"x, y\" /* c, d */, g[1, 2]) // e, f"), 0).Count);
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        /// <summary>Ein Muster, das nicht zu seinen Werten passt, wirft benannt — kein stilles Muster.</summary>
        [Fact]
        public void Ein_unpassendes_Muster_wirft_mit_seinem_Schluessel()
        {
            FormatException ex = Assert.Throws<FormatException>(() => ZapfSatz.Neu("HINWEIS_ZIRKULATION_GROSS", 1.0).Klartext);
            Assert.Contains("ZPG_SATZ_HINWEIS_ZIRKULATION_GROSS", ex.Message);
            Assert.Throws<FormatException>(() => ZapfSatz.Neu("EINGABE_ZONE_OHNE_ANGABEN").Text(k => "{0} und {1}", CultureInfo.InvariantCulture));
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

        /// <summary>Die Kennungen des ersten Arguments: ein Literal, eine Familie (Literal mit „+"), eine Konstante.</summary>
        private static IEnumerable<string> Aufgeloest(string a, Dictionary<string, string> konstanten)
        {
            Match l = Literal.Match(a);
            if (l.Success)
                return l.Groups["plus"].Success && Familien.TryGetValue(l.Groups["k"].Value, out string[] familie)
                    ? familie : new[] { l.Groups["k"].Value };
            Match b = Bezeichner.Match(a);
            return b.Success && konstanten.TryGetValue(b.Groups["n"].Value, out string wert) ? new[] { wert } : new string[0];
        }

        /// <summary>Der Quelltext ohne Zeilen- und Blockkommentare (Zeichenketten bleiben).</summary>
        private static string OhneKommentare(string t)
        {
            var s = new System.Text.StringBuilder(t.Length);
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '"' || c == '\'')
                {
                    int ende = Zeichenkette(t, i);
                    s.Append(t, i, ende - i + 1);
                    i = ende;
                }
                else if (c == '/' && i + 1 < t.Length && t[i + 1] == '/')
                {
                    while (i < t.Length && t[i] != '\n') i++;
                    s.Append('\n');
                }
                else if (c == '/' && i + 1 < t.Length && t[i + 1] == '*')
                {
                    int ende = t.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = ende < 0 ? t.Length : ende + 1;
                }
                else s.Append(c);
            }
            return s.ToString();
        }

        /// <summary>Das Ende einer Zeichenkette oder eines Zeichens ab <paramref name="i"/> (auch @"…" und $"…").</summary>
        private static int Zeichenkette(string t, int i)
        {
            char q = t[i];
            bool wort = q == '"' && i > 0 && (t[i - 1] == '@' || (t[i - 1] == '$' && i > 1 && t[i - 2] == '@'));
            for (int j = i + 1; j < t.Length; j++)
            {
                if (!wort && t[j] == '\\') { j++; continue; }
                if (t[j] == q)
                {
                    if (wort && j + 1 < t.Length && t[j + 1] == '"') { j++; continue; }
                    return j;
                }
            }
            return t.Length - 1;
        }

        /// <summary>Die Argumente eines Aufrufs ab der öffnenden Klammer (oberste Ebene, durch Kommas getrennt); <c>null</c> ohne Schluss.</summary>
        private static List<string> Argumente(string t, int start)
        {
            var args = new List<string>();
            int tiefe = 0, anfang = start;
            for (int i = start; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '"' || c == '\'') { i = Zeichenkette(t, i); continue; }
                if (c == '(' || c == '[' || c == '{') tiefe++;
                else if ((c == ')' || c == ']' || c == '}') && tiefe > 0) tiefe--;
                else if (c == ')')
                {
                    string letztes = t.Substring(anfang, i - anfang);
                    if (letztes.Trim().Length > 0 || args.Count > 0) args.Add(letztes);
                    return args;
                }
                else if (c == ',' && tiefe == 0)
                {
                    args.Add(t.Substring(anfang, i - anfang));
                    anfang = i + 1;
                }
            }
            return null;
        }

        /// <summary>Die Quellen des Kerns, die Sätze bauen: Zapfprofil-Ordner und Controller.</summary>
        private static IEnumerable<string> Quellen()
            => new[] { Pfad("EPOS.Kern", "Allgemein", "Zapfprofil"), Pfad("EPOS.Kern", "Controller") }
               .SelectMany(o => Directory.GetFiles(o, "*.cs"))
               // Der Leser der eingespielten Typtage (Stufe Z4b) liegt beim Import, nennt aber
               // seine Ablehnungen als ZapfSatz - also gehoert er in den Quellenkreis.
               .Concat(new[] { Pfad("EPOS.Kern", "Allgemein", "Import", "Normformvektorleser.cs"),
                               // Der Leser der gemessenen Reihen (Stufe Z5) liegt ebenfalls beim
                               // Import, nennt aber seine Ablehnungen als ZapfSatz.
                               Pfad("EPOS.Kern", "Allgemein", "Import", "Messreihenleser.cs"),
                               // Die Huelle des Vergleichs (Stufe Z5) nennt EINEN eigenen Satz: die
                               // Spreizung mehrerer Zonen. Der Kern nimmt EINE Spreizung; welche das
                               // ist, entscheidet die Huelle - also gehoert sie in den Quellenkreis.
                               Pfad("EPOS.UI.Daten", "Bedarf", "ZapfprofilHuelle.Messvergleich.cs") });

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
