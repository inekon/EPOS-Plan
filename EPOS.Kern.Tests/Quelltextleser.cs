using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Ein kleiner Leser für C#-Quelltext ohne Roslyn, für Quelltext-Wachen der Testprojekte.
    /// Er maskiert Kommentare, Zeichen- und Zeichenkettenliterale (auch wortgetreu, interpoliert
    /// und roh) sowie Präprozessorzeilen, zählt Klammertiefen und Zeilen und liest daraus die
    /// Klassen einer Datei (Name, Verschachtelung, Basisliste, Rumpf) samt ihrer
    /// <c>Kulturvorrichtung</c>-Felder; <see cref="Klassenbestand"/> führt Teilklassen über
    /// Dateien zusammen und verfolgt Basisketten.
    ///
    /// <para><b>Herkunft:</b> gebaut mit Auftrag #531 (CI-Wächter, 26.09.2026) als Abschnitt
    /// „Werkzeug — Quelltextleser“ in <c>KulturwaechterTests</c>; mit Nachlese #534 unverändert
    /// in diese Datei ausgelagert, damit andere Quelltext-Wachen ihn nutzen können. Die Regeln
    /// und Meldungen der Kulturwächter stehen weiter in <see cref="KulturwaechterTests"/>.</para>
    /// </summary>
    internal static class Quelltextleser
    {
        /// <summary>Ein Vorrichtungsfeld einer Klasse: Name und Stelle im Text.</summary>
        internal sealed class Feld
        {
            public string Name;
            public int Index;
        }

        /// <summary>
        /// Eine Klasse (auch <c>struct</c>, <c>record</c>, <c>interface</c>) im maskierten Text:
        /// Name, Pfad der Verschachtelung (<c>Aussen.Innen</c>), eigene Basisliste, Rumpf von
        /// <c>{</c> bis <c>}</c>, umschließende Klasse und die Vorrichtungsfelder ihres Rumpfs.
        /// </summary>
        internal sealed class Klasse
        {
            public string Name;
            public string Pfad;
            public string Basisliste;
            public int KopfIndex;
            public int RumpfStart;
            public int RumpfEnde;
            public Klasse Aussen;
            public readonly List<Feld> Vorrichtungsfelder = new List<Feld>();
        }

        /// <summary>
        /// Eine Quelldatei: Text, maskierter Text (Kommentare, Zeichen- und Zeichenkettenliterale,
        /// Präprozessorzeilen durch Leerzeichen ersetzt — Positionen und Zeilen bleiben),
        /// Klammertiefe je Stelle, Zeilenanfänge und die Klassen samt Vorrichtungsfeldern.
        /// </summary>
        internal sealed class Quelle
        {
            public string Relativ;
            public string Text;
            public string Maske;
            public int[] Tiefe;
            public List<int> Zeilenanfaenge;
            public List<Klasse> Klassen;

            public static Quelle Lies(string datei, string wurzel)
                => Baue(File.ReadAllText(datei), Path.GetRelativePath(wurzel, datei).Replace('\\', '/'));

            public static Quelle AusText(string text) => Baue(text, "Probe.cs");

            private static Quelle Baue(string text, string relativ)
            {
                var q = new Quelle { Relativ = relativ, Text = text, Maske = Maskiere(text) };

                q.Tiefe = new int[text.Length + 1];
                for (int i = 0; i < text.Length; i++)
                {
                    char c = q.Maske[i];
                    q.Tiefe[i + 1] = q.Tiefe[i] + (c == '{' ? 1 : c == '}' ? -1 : 0);
                }

                q.Zeilenanfaenge = new List<int> { 0 };
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n') q.Zeilenanfaenge.Add(i + 1);
                }

                q.Klassen = LiesKlassen(q.Maske);
                foreach (Match m in Vorrichtungsdeklaration.Matches(q.Maske))
                {
                    Klasse k = q.InnersteKlasse(m.Index);
                    if (k == null || q.RelativeTiefe(k, m.Index) != 0) continue;
                    if (!Feldvorlauf.IsMatch(Anweisungsvorlauf(q.Maske, m.Index))) continue;
                    k.Vorrichtungsfelder.Add(new Feld { Name = m.Groups[1].Value, Index = m.Index });
                }
                return q;
            }

            /// <summary>Zeile (ab 1) der Stelle.</summary>
            public int Zeile(int index)
            {
                int z = Zeilenanfaenge.BinarySearch(index);
                return z >= 0 ? z + 1 : ~z;
            }

            /// <summary><c>Datei:Zeile</c> der Stelle.</summary>
            public string Ort(int index) => Relativ + ":" + Zeile(index);

            /// <summary>Steht an der Stelle Code (nicht Kommentar, Zeichenkette, Präprozessor)?</summary>
            public bool IstCode(int index) => index < Maske.Length && Maske[index] == Text[index];

            /// <summary>Die innerste Klasse, deren Rumpf die Stelle enthält, sonst <c>null</c>.</summary>
            public Klasse InnersteKlasse(int index)
            {
                Klasse beste = null;
                foreach (Klasse k in Klassen)
                {
                    if (k.RumpfStart < index && index < k.RumpfEnde
                        && (beste == null || k.RumpfStart > beste.RumpfStart))
                    {
                        beste = k;
                    }
                }
                return beste;
            }

            /// <summary>Klammertiefe der Stelle im Rumpf der Klasse: 0 = Ebene der Member.</summary>
            public int RelativeTiefe(Klasse k, int index) => Tiefe[index] - Tiefe[k.RumpfStart + 1];
        }

        /// <summary>
        /// Alle Klassen eines Projekts, für die zweite Tür des UI-Wächters: Teilklassen über
        /// Dateien hinweg nach ihrem Pfad zusammengeführt, Basisketten über Klassennamen verfolgt.
        /// </summary>
        internal sealed class Klassenbestand
        {
            private readonly Dictionary<string, List<Klasse>> _jePfad =
                new Dictionary<string, List<Klasse>>(StringComparer.Ordinal);

            public Klassenbestand(IEnumerable<Quelle> quellen)
            {
                foreach (Quelle q in quellen)
                {
                    foreach (Klasse k in q.Klassen)
                    {
                        if (!_jePfad.TryGetValue(k.Pfad, out List<Klasse> teile))
                        {
                            teile = new List<Klasse>();
                            _jePfad[k.Pfad] = teile;
                        }
                        teile.Add(k);
                    }
                }
            }

            /// <summary>Nutzt die Klasse — oder eine sie umschließende — die Vorrichtung?</summary>
            public bool NutztVorrichtung(Klasse k)
            {
                for (Klasse c = k; c != null; c = c.Aussen)
                {
                    if (PfadNutzt(c.Pfad, 0)) return true;
                }
                return false;
            }

            private bool PfadNutzt(string pfad, int tiefe)
            {
                if (tiefe > 16 || !_jePfad.TryGetValue(pfad, out List<Klasse> teile)) return false;
                foreach (Klasse teil in teile)
                {
                    if (teil.Vorrichtungsfelder.Count > 0) return true;
                    string basis = ErsteBasis(teil.Basisliste);
                    if (basis == "EposBunitContext") return true;
                    if (basis != null && basis != pfad && PfadNutzt(basis, tiefe + 1)) return true;
                }
                return false;
            }
        }

        /// <summary>Der einfache Name des ersten Eintrags einer Basisliste (die Basisklasse,
        /// falls es eine gibt), ohne Namensraum und Typargumente.</summary>
        private static string ErsteBasis(string basisliste)
        {
            if (string.IsNullOrWhiteSpace(basisliste)) return null;
            int tiefe = 0;
            int ende = basisliste.Length;
            for (int i = 0; i < basisliste.Length; i++)
            {
                char c = basisliste[i];
                if (c == '<' || c == '(') tiefe++;
                else if (c == '>' || c == ')') tiefe--;
                else if (c == ',' && tiefe == 0)
                {
                    ende = i;
                    break;
                }
            }
            string erste = basisliste.Substring(0, ende);
            int klammer = erste.IndexOfAny(new[] { '<', '(' });
            if (klammer >= 0) erste = erste.Substring(0, klammer);
            erste = erste.Trim();
            erste = erste.Substring(Math.Max(erste.LastIndexOf('.'), erste.LastIndexOf(':')) + 1);
            return erste.Length == 0 ? null : erste;
        }

        /// <summary>Kopf einer Klasse; <c>record class</c>/<c>record struct</c> eingeschlossen,
        /// die Einschränkung <c>where T : class</c> nicht.</summary>
        private static readonly Regex Klassenkopf = new Regex(
            @"\b(?:class|struct|interface|record)\s+(?:(?:class|struct)\s+)?(?!where\b)([A-Za-z_]\w*)",
            RegexOptions.Compiled);

        /// <summary>Alle Klassen des maskierten Texts mit Basisliste, Rumpf und Verschachtelung.</summary>
        private static List<Klasse> LiesKlassen(string maske)
        {
            var klassen = new List<Klasse>();
            int n = maske.Length;
            foreach (Match m in Klassenkopf.Matches(maske))
            {
                int i = UeberspringeLeer(maske, m.Index + m.Length);
                if (i < n && maske[i] == '<') i = UeberspringeLeer(maske, Schliessende(maske, i, '<', '>') + 1);
                if (i < n && maske[i] == '(') i = UeberspringeLeer(maske, Schliessende(maske, i, '(', ')') + 1);

                // Nur ein echter Kopf: danach folgt Basisliste, Einschränkung, Rumpf oder Semikolon
                // (sonst war es z. B. eine Variable namens "record").
                if (i >= n || !(maske[i] == ':' || maske[i] == '{' || maske[i] == ';'
                                || string.CompareOrdinal(maske, i, "where", 0, 5) == 0))
                {
                    continue;
                }

                int j = i;
                int rund = 0;
                while (j < n && !((maske[j] == '{' || maske[j] == ';') && rund == 0))
                {
                    if (maske[j] == '(') rund++;
                    else if (maske[j] == ')') rund--;
                    j++;
                }
                if (j >= n || maske[j] == ';') continue;

                string kopf = maske.Substring(i, j - i).Trim();
                string basis = "";
                if (kopf.StartsWith(":", StringComparison.Ordinal))
                {
                    basis = kopf.Substring(1);
                    Match wo = Regex.Match(basis, @"\bwhere\b");
                    if (wo.Success) basis = basis.Substring(0, wo.Index);
                    basis = Regex.Replace(basis.Trim(), @"\s+", " ");
                }

                klassen.Add(new Klasse
                {
                    Name = m.Groups[1].Value,
                    Basisliste = basis,
                    KopfIndex = m.Index,
                    RumpfStart = j,
                    RumpfEnde = Schliessende(maske, j, '{', '}'),
                });
            }

            foreach (Klasse k in klassen)
            {
                foreach (Klasse a in klassen)
                {
                    if (a != k && a.RumpfStart < k.KopfIndex && k.KopfIndex < a.RumpfEnde
                        && (k.Aussen == null || a.RumpfStart > k.Aussen.RumpfStart))
                    {
                        k.Aussen = a;
                    }
                }
            }
            foreach (Klasse k in klassen)
            {
                var namen = new List<string>();
                for (Klasse c = k; c != null; c = c.Aussen) namen.Insert(0, c.Name);
                k.Pfad = string.Join(".", namen);
            }
            return klassen;
        }

        private static int UeberspringeLeer(string s, int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            return i;
        }

        /// <summary>Die zur öffnenden passende schließende Klammer (sonst das Textende).</summary>
        private static int Schliessende(string s, int offen, char auf, char zu)
        {
            int tiefe = 0;
            for (int j = offen; j < s.Length; j++)
            {
                if (s[j] == auf) tiefe++;
                else if (s[j] == zu)
                {
                    tiefe--;
                    if (tiefe == 0) return j;
                }
            }
            return s.Length - 1;
        }

        /// <summary>
        /// Ersetzt Kommentare, Zeichen- und Zeichenkettenliterale (auch wortgetreu, interpoliert
        /// und roh) sowie Präprozessorzeilen durch Leerzeichen; Zeilenumbrüche bleiben, damit
        /// Stellen und Zeilen des maskierten Texts denen des Originals gleichen.
        /// </summary>
        private static string Maskiere(string t)
        {
            var sb = new StringBuilder(t);
            int n = t.Length;
            int i = 0;
            bool zeilenanfang = true;
            while (i < n)
            {
                char c = t[i];
                if (c == '\n')
                {
                    zeilenanfang = true;
                    i++;
                    continue;
                }
                if (zeilenanfang && c == '#')
                {
                    int e = Zeilenende(t, i);
                    Leeren(sb, i, e);
                    i = e;
                    continue;
                }
                if (!char.IsWhiteSpace(c)) zeilenanfang = false;

                if (c == '/' && i + 1 < n && t[i + 1] == '/')
                {
                    int e = Zeilenende(t, i);
                    Leeren(sb, i, e);
                    i = e;
                    continue;
                }
                if (c == '/' && i + 1 < n && t[i + 1] == '*')
                {
                    int e = t.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    e = e < 0 ? n : e + 2;
                    Leeren(sb, i, e);
                    i = e;
                    continue;
                }
                if (c == '\'')
                {
                    int e = ZeichenEnde(t, i);
                    Leeren(sb, i, e);
                    i = e;
                    continue;
                }
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    while (i < n && (char.IsLetterOrDigit(t[i]) || t[i] == '_')) i++;
                    continue;
                }
                if (c == '"' || c == '$' || c == '@')
                {
                    int e = ZeichenketteEnde(t, i);
                    if (e > i)
                    {
                        Leeren(sb, i, e);
                        i = e;
                        continue;
                    }
                }
                i++;
            }
            return sb.ToString();
        }

        private static void Leeren(StringBuilder sb, int von, int bis)
        {
            for (int k = von; k < bis && k < sb.Length; k++)
            {
                if (sb[k] != '\n' && sb[k] != '\r') sb[k] = ' ';
            }
        }

        private static int Zeilenende(string t, int i)
        {
            int e = t.IndexOf('\n', i);
            return e < 0 ? t.Length : e;
        }

        /// <summary>Ende (ausschließlich) eines Zeichenliterals ab dem öffnenden Hochkomma.</summary>
        private static int ZeichenEnde(string t, int i)
        {
            int j = i + 1;
            while (j < t.Length && t[j] != '\'' && t[j] != '\n')
            {
                j += t[j] == '\\' ? 2 : 1;
            }
            return Math.Min(j + 1, t.Length);
        }

        /// <summary>
        /// Ende (ausschließlich) einer Zeichenkette ab der Stelle <paramref name="i"/> samt
        /// Vorsilben <c>$</c>/<c>@</c>; <paramref name="i"/> selbst, wenn dort keine beginnt.
        /// Interpolationslöcher dürfen eigene Zeichenketten und Zeichen enthalten.
        /// </summary>
        private static int ZeichenketteEnde(string t, int i)
        {
            int n = t.Length;
            int p = i;
            bool wortgetreu = false;
            bool interpoliert = false;
            while (p < n && (t[p] == '$' || t[p] == '@'))
            {
                if (t[p] == '@') wortgetreu = true;
                else interpoliert = true;
                p++;
            }
            if (p >= n || t[p] != '"') return i;

            if (p + 2 < n && t[p + 1] == '"' && t[p + 2] == '"')
            {
                int q = p;
                while (q < n && t[q] == '"') q++;
                int anzahl = q - p;
                int ende = t.IndexOf(new string('"', anzahl), q, StringComparison.Ordinal);
                return ende < 0 ? n : ende + anzahl;
            }

            int j = p + 1;
            int loch = 0;
            while (j < n)
            {
                char d = t[j];
                if (loch > 0)
                {
                    if (d == '{') loch++;
                    else if (d == '}') loch--;
                    else if (d == '\'')
                    {
                        j = ZeichenEnde(t, j);
                        continue;
                    }
                    else if (d == '"' || d == '$' || d == '@')
                    {
                        int e = ZeichenketteEnde(t, j);
                        if (e > j)
                        {
                            j = e;
                            continue;
                        }
                    }
                    j++;
                    continue;
                }
                if (interpoliert && d == '{')
                {
                    if (j + 1 < n && t[j + 1] == '{')
                    {
                        j += 2;
                        continue;
                    }
                    loch++;
                    j++;
                    continue;
                }
                if (wortgetreu)
                {
                    if (d == '"')
                    {
                        if (j + 1 < n && t[j + 1] == '"')
                        {
                            j += 2;
                            continue;
                        }
                        return j + 1;
                    }
                }
                else
                {
                    if (d == '\\')
                    {
                        j += 2;
                        continue;
                    }
                    if (d == '"') return j + 1;
                    if (d == '\n') return j;
                }
                j++;
            }
            return n;
        }

        // =====================================================================
        //  Felderkennung — Vorrichtungsfelder und Anweisungsvorlauf
        //  (Wächter A in KulturwaechterTests nutzt dieselben Ausdrücke)
        // =====================================================================

        /// <summary>Eine Deklaration <c>Kulturvorrichtung name =|;|,</c> (Feld oder lokal; nicht
        /// Methode, Eigenschaft, Parameter mit Klammer dahinter oder Ausdruckskörper).</summary>
        internal static readonly Regex Vorrichtungsdeklaration = new Regex(
            @"\bKulturvorrichtung\s*\??\s+([A-Za-z_]\w*)\s*(?=(?:=(?!>)|[;,]))", RegexOptions.Compiled);

        /// <summary>Vorlauf eines Feldes: nur Attribute und Modifizierer.</summary>
        private static readonly Regex Feldvorlauf = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|static|readonly|volatile|new|required)\s+)*$",
            RegexOptions.Compiled);

        /// <summary>Der Text vom Anfang der Anweisung (nach dem letzten <c>;</c>, <c>{</c> oder
        /// <c>}</c> im maskierten Text) bis zur Stelle <paramref name="index"/>.</summary>
        internal static string Anweisungsvorlauf(string maske, int index)
        {
            int j = index - 1;
            while (j >= 0 && maske[j] != ';' && maske[j] != '{' && maske[j] != '}') j--;
            return maske.Substring(j + 1, index - j - 1);
        }
    }
}
