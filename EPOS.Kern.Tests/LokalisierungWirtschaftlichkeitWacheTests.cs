using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE B6 (K11) — der Wächter über die nackten deutschen Anzeigetexte der
    /// Wirtschaftlichkeit.</b>
    ///
    /// <para><b>Warum es ihn gibt.</b> Die Masken der Wirtschaftlichkeit sind mit B5b
    /// nach Razor portiert worden, und dabei sind die Beschriftungen in
    /// <c>MyResource</c> gewandert. Ein Wandern ist aber kein Zustand: Der nächste
    /// Dialogumbau schreibt wieder einen deutschen Satz hin, und niemand merkt es,
    /// bis ein englischer Anwender ihn liest. Dieser Fall hält den erreichten Stand.</para>
    ///
    /// <para><b>Was als lokalisiert gilt.</b> Jeder Text, der über einen
    /// SCHLÜSSEL geht: <c>T("KEY", "…")</c>, <c>Text("KEY", "…")</c>,
    /// <c>Text_("KEY", "…")</c>, <c>BhwTexte.T(…)</c> — und <c>Resource.KEY</c>. Der
    /// deutsche Text im zweiten Argument ist der RÜCKFALL und darf dastehen; er ist
    /// gerade der Beleg, dass ein Schlüssel gemeint ist. Ebenso eine
    /// <c>const</c>-Zeichenkette, die im selben Quelltext als solcher Rückfall
    /// benutzt wird (Muster <c>KapitalwertVerlaufHuelle.TITEL_DIFF</c>).</para>
    ///
    /// <para><b>Was auffällt.</b> Eine deutsche Zeichenkette in einer Zuweisung, einem
    /// <c>string.Format</c>, einem Razor-Attribut — also überall dort, wo kein
    /// Schlüssel danebensteht. Erkannt an deutschen Funktionswörtern oder Umlauten;
    /// sprachneutrale Schlüssel, CSS-Klassen, Zahlenformate und Datumsmuster fallen
    /// nicht darunter.</para>
    ///
    /// <para><b>Der Fall misst die QUELLEN im Repositorium</b>, nicht das Kompilat: Die
    /// Regel gilt dem, was jemand schreibt. Fehlt der Ordner (ein Ausgabeordner ohne
    /// Quellen), wird nicht geprüft.</para>
    /// </summary>
    public class LokalisierungWirtschaftlichkeitWacheTests
    {
        /// <summary>
        /// Die Ordner, für die die Regel gilt: die Dialoge und die Seite der
        /// Wirtschaftlichkeit samt der Windows-Hüllen dazu. <b>Nicht</b> der ganze
        /// Berichtsordner — dessen übrige Seiten stammen aus anderen Wellen und tragen
        /// ihre eigenen Rückstände; sie kommen mit ihrer Welle dazu.
        /// </summary>
        private static readonly string[] Ordner =
        {
            Path.Combine("EPOS.UI", "Dialoge", "Wirtschaftlichkeit"),
            Path.Combine("WindowsFormsApplication1", "Views", "Wirtschaftlichkeit"),
        };

        /// <summary>Einzelne Dateien außerhalb der Ordner, die zur selben Sache
        /// gehören: die Seite der Wirtschaftlichkeit und der Auflöser, dessen
        /// Herleitungsprosa mit dieser Etappe zweisprachig geworden ist.</summary>
        private static readonly string[] Dateien =
        {
            Path.Combine("EPOS.UI", "Seiten", "Berichte", "WirtschaftlichkeitSeite.razor"),
            Path.Combine("EPOS.Kern", "Allgemein", "Wirtschaftlichkeit", "EndenergieAufloeser.cs"),
            Path.Combine("EPOS.Kern", "Allgemein", "Wirtschaftlichkeit", "KohaerenzPruefung.cs"),
        };

        [Fact]
        public void Kein_nackter_deutscher_Anzeigetext_in_der_Wirtschaftlichkeit()
        {
            string wurzel = Wurzel();
            var befunde = new List<string>();

            foreach (string datei in Quellen(wurzel))
                befunde.AddRange(Nackte(datei, wurzel));

            Assert.True(befunde.Count == 0,
                "Die Wirtschaftlichkeit führt " + befunde.Count +
                " deutsche(n) Anzeigetext(e) ohne Ressourcenschlüssel:" +
                Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", befunde) + Environment.NewLine +
                "Jeder Anzeigetext geht über MyResource.Resource — in BEIDEN Sprachen, " +
                "danach \"python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben\". " +
                "Ein deutscher Text als RÜCKFALL im zweiten Argument eines " +
                "T(\"SCHLUESSEL\", \"…\")-Aufrufs ist erlaubt und gemeint.");
        }

        // =================================================================
        // Der Messweg
        // =================================================================

        /// <summary>Alle geprüften Quelltexte; fehlende Pfade werden übergangen.</summary>
        private static IEnumerable<string> Quellen(string wurzel)
        {
            foreach (string o in Ordner)
            {
                string voll = Path.Combine(wurzel, o);
                if (!Directory.Exists(voll)) continue;
                foreach (string f in Directory.GetFiles(voll, "*.cs").OrderBy(x => x)) yield return f;
                foreach (string f in Directory.GetFiles(voll, "*.razor").OrderBy(x => x)) yield return f;
            }
            foreach (string d in Dateien)
            {
                string voll = Path.Combine(wurzel, d);
                if (File.Exists(voll)) yield return voll;
            }
        }

        /// <summary>Deutsche Funktionswörter — der Nachweis, dass eine Zeichenkette
        /// Prosa ist und kein Bezeichner.</summary>
        private static readonly Regex Funktionswort = new Regex(
            @"\b(der|die|das|und|oder|nicht|kein|keine|ist|sind|wird|werden|im|in|am|aus|" +
            @"für|von|mit|zum|zur|bitte|ohne|je|auf|ein|eine|dem|den|des|gilt|nur|noch)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex Umlaut = new Regex(@"[äöüÄÖÜß]", RegexOptions.CultureInvariant);

        /// <summary>
        /// SQL-Bruchstücke. Sie sind keine Anzeigetexte und werden nie übersetzt — nur
        /// stolpert die Prosaerkennung über ihre Spaltennamen („Bezeichner",
        /// „Bezeichnung") und über <c>ON</c>, <c>IN</c>, <c>AS</c>. Erkannt an einem
        /// SQL-Schlüsselwort in GROSSBUCHSTABEN; ein deutscher Satz schreibt „FROM"
        /// nicht groß.
        /// </summary>
        private static readonly Regex SqlWort = new Regex(
            @"\b(SELECT|FROM|WHERE|INSERT|UPDATE|DELETE|JOIN|LEFT|INNER|ORDER BY|GROUP BY|" +
            @"VALUES|CREATE|ALTER|PRAGMA|COUNT\(|SET )\b?|^ON [A-Za-z_]+\.",
            RegexOptions.CultureInvariant);

        /// <summary>Ein Aufruf mit Schlüssel — alles ab dem Komma ist Rückfalltext.</summary>
        private static readonly Regex MitSchluessel = new Regex(
            @"\b(?:BhwTexte\.T|WirtTexte\.T|Text_|Text|Txt|T)\s*\(\s*""[A-Z0-9_]+""\s*,",
            RegexOptions.CultureInvariant);

        /// <summary>Eine Zeichenkettenkonstante — Name und Wert.</summary>
        private static readonly Regex Konstante = new Regex(
            @"const\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*""",
            RegexOptions.CultureInvariant);

        private static List<string> Nackte(string datei, string wurzel)
        {
            string s = File.ReadAllText(datei, Encoding.UTF8);
            char[] rein = s.ToCharArray();

            Leere(rein, new Regex(@"//[^\n]*"), s);
            Leere(rein, new Regex(@"/\*.*?\*/", RegexOptions.Singleline), s);
            Leere(rein, new Regex(@"@\*.*?\*@", RegexOptions.Singleline), s);

            // Rückfalltexte lokalisierter Aufrufe: den ganzen Argumentbereich leeren.
            foreach (Match m in MitSchluessel.Matches(s))
                { int ab = m.Index + m.Length; Leere(rein, ab, EndeDesAufrufs(s, ab)); }

            // Rückfall-Konstanten: eine const-Zeichenkette, deren Name im selben
            // Quelltext als zweites Argument eines Schlüsselaufrufs steht.
            foreach (Match m in Konstante.Matches(s))
            {
                string name = m.Groups[1].Value;
                if (!Regex.IsMatch(s, @"""[A-Z0-9_]+""\s*,\s*" + Regex.Escape(name) + @"\s*\)")) continue;
                int start = m.Index + m.Length - 1;
                Leere(rein, start, EndeDerZeichenkette(s, start));
            }

            string mask = new string(rein);
            var treffer = new List<string>();
            foreach (Match m in Regex.Matches(mask, @"""((?:[^""\\\n]|\\.)*)"""))
            {
                string t = m.Groups[1].Value;
                if (!Deutsch(t)) continue;
                int zeile = mask.Take(m.Index).Count(c => c == '\n') + 1;
                treffer.Add(Path.GetRelativePath(wurzel, datei).Replace('\\', '/') + ":" +
                            zeile.ToString(CultureInfo.InvariantCulture) + "  \"" +
                            (t.Length > 80 ? t.Substring(0, 80) + "…" : t) + "\"");
            }
            return treffer;
        }

        /// <summary>Ist die Zeichenkette deutsche Anzeigeprosa?</summary>
        private static bool Deutsch(string t)
        {
            if (t.Length < 6) return false;
            if (t.Contains("=>", StringComparison.Ordinal)) return false;
            if (t.StartsWith("@", StringComparison.Ordinal)) return false;
            if (t.StartsWith("(", StringComparison.Ordinal)) return false;
            if (t.Contains("://", StringComparison.Ordinal)) return false;
            if (Regex.IsMatch(t, @"^[A-Z0-9_\.\-]+$")) return false;            // Schlüssel
            if (Regex.IsMatch(t, @"^[\sdMyHmsf:\.\-]+$")) return false;         // Datumsmuster
            if (!Regex.IsMatch(t, @"[A-Za-zÄÖÜäöüß]{3}")) return false;
            if (SqlWort.IsMatch(t)) return false;
            if (Funktionswort.IsMatch(t) || Umlaut.IsMatch(t)) return true;
            return Regex.IsMatch(t, @"^[A-ZÄÖÜ]") && t.Contains(' ');
        }

        /// <summary>Ende eines Aufrufs ab dem Komma — Klammern gezählt, Zeichenketten
        /// übersprungen.</summary>
        private static int EndeDesAufrufs(string s, int ab)
        {
            int tiefe = 1, i = ab;
            bool inText = false;
            while (i < s.Length && tiefe > 0)
            {
                char c = s[i];
                if (inText)
                {
                    if (c == '\\') { i += 2; continue; }
                    if (c == '"') inText = false;
                }
                else if (c == '"') inText = true;
                else if (c == '(') tiefe++;
                else if (c == ')') tiefe--;
                i++;
            }
            return i;
        }

        /// <summary>Ende einer Zeichenkette, die bei <paramref name="ab"/> mit dem
        /// Anführungszeichen beginnt.</summary>
        private static int EndeDerZeichenkette(string s, int ab)
        {
            int i = ab + 1;
            while (i < s.Length)
            {
                if (s[i] == '\\') { i += 2; continue; }
                if (s[i] == '"') return i + 1;
                i++;
            }
            return i;
        }

        private static void Leere(char[] ziel, Regex r, string quelle)
        {
            foreach (Match m in r.Matches(quelle)) Leere(ziel, m.Index, m.Index + m.Length);
        }

        private static void Leere(char[] ziel, int von, int bis)
        {
            for (int i = von; i < bis && i < ziel.Length; i++)
                if (ziel[i] != '\r' && ziel[i] != '\n') ziel[i] = ' ';
        }

        /// <summary>Der Aufstieg zur Repowurzel — dasselbe Vorgehen wie in
        /// <c>GesetzkatalogSaatWacheTests.Wurzel</c>.</summary>
        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null &&
                   !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;

            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
