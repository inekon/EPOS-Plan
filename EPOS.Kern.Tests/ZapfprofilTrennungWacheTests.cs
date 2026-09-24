using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache „Auslegung ohne Bilanzreihe"</b> (Umsetzungskonzept Zapfprofilgenerator 2.4,
    /// 0 Punkt 3): Bilanz und Auslegung sind zwei Produkte — auch im Quelltext. Keine
    /// Auslegungsklasse nimmt eine Bilanzreihe oder ein Jahresfeld an.
    ///
    /// <para><b>Drei Sätze:</b> (1) <b>Quelltext:</b> Keine Zeile der Auslegungsdateien, die der
    /// Übersetzer sieht, nennt <c>Bilanzreihe</c>, <c>ZapfprofilErgebnis</c>,
    /// <c>ZapfprofilRechner</c>, <c>ZonenErgebnis</c> oder <c>Formvektor.Stundenreihe</c>.
    /// (2) <b>Signaturen (Reflection):</b> Kein nicht privates Glied eines Auslegungstyps trägt
    /// <c>Bilanzreihe</c>, <c>ZapfprofilErgebnis</c> oder <c>ZonenErgebnis</c> — weder als
    /// Parameter noch als Rückgabe, Eigenschaft oder Feld, auch nicht als Typargument; weder eine
    /// Methode noch ein nicht privater Konstruktor nimmt ein Zahlenfeld an (<c>double[]</c>, <c>double[,]</c>,
    /// <c>IReadOnlyList&lt;double&gt;</c>, <c>IList</c>, <c>List</c>, <c>ICollection</c>,
    /// <c>IEnumerable</c> von <c>double</c>), und kein Glied gibt eines heraus — weder als
    /// Rückgabe noch als Eigenschaft oder Feld, außer den benannten Gliedern in
    /// <see cref="Ausnahmen"/>. Minutenwerte kommen nur über <see cref="Bedarfstag"/> (und die
    /// <see cref="Minutenstatistik"/> der Einheiten in derselben Datei),
    /// Stundenwerte nur über <see cref="Wochenreihe"/> (168 h) — deren Dateien sind vom
    /// Zahlenfeldsatz ausgenommen, nicht vom Bilanzsatz. Als Kommentar zählt eine Zeile, die
    /// mit <c>//</c> oder <c>/*</c> beginnt, und eine Zeile innerhalb eines Blocks
    /// <c>/* … */</c> — eine Zeile, die mit <c>*</c> beginnt, nur dort (sonst ist sie Code, etwa
    /// die Fortsetzung eines Produkts). (3) <b>Vollständigkeit:</b> Jeder Typ, den eine Auslegungsdatei auf
    /// Namensraumebene deklariert, steht in der Liste dieser Wache — ein neuer Typ entgeht ihr
    /// nicht.</para>
    ///
    /// <para><b>Gegenproben:</b> Die Bilanzfassade <see cref="ZapfprofilRechner"/> und
    /// <see cref="Formvektor"/> verletzen die Regel und müssen erkannt werden.</para>
    ///
    /// <para><b>Stufe Z3 — zwei Ensembles:</b> Das Ensemble der Jahresreihe
    /// (<see cref="Jahresensemble"/>, <see cref="Jahreszone"/>, <see cref="Jahreskonsistenz"/>) ist
    /// Bilanz und zählt zu den Bilanzbezeichnern und -typen; das Auslegungsensemble
    /// (<see cref="Zapfensemble"/>) steht mit Zufall, Kategorien und Ereignisgenerator unter der
    /// strengen Regel. So reicht kein Ensemble eine Bilanzreihe in die Auslegung (2.4).</para>
    /// </summary>
    public sealed class ZapfprofilTrennungWacheTests
    {
        /// <summary>Die Dateien der Auslegung unter <c>EPOS.Kern/Allgemein/Zapfprofil/</c>.</summary>
        private static readonly string[] Dateien =
        {
            "Auslegungsparameter.cs", "Bedarfstag.cs", "Wochenreihe.cs", "Summenlinie.cs", "Din4708Kennzahl.cs",
            "TwwSpeicherauslegung.cs", "Grossanlage.cs", "Auslegungsergebnis.cs", "ZapfprofilAuslegung.cs",
            // Stufe Z3: Zufall, Kategorien und Ereignisgenerator dienen beiden Produkten und halten
            // die strengere Regel — keine Bilanzreihe, keine Zahlenliste in einer Signatur.
            "ZapfZufall.cs", "Zapfkategorie.cs", "Zapfereignisgenerator.cs",
            // Das Auslegungsensemble (Bedarfstag, Perzentile, Gleichzeitigkeit) — die Zapfensemble-Auswertung der Invariante 2.4.
            "Zapfensemble.cs",
            // Stufe Z4: die Sätze des Kerns und die Schätzhilfen dienen beiden Produkten und halten die strengere Regel.
            "ZapfSatz.cs", "Schaetzhilfe.cs"
        };

        /// <summary>Die Dateien der Bilanz (Stufe Z1) — jede Datei des Ordners gehört zu genau einer Liste.</summary>
        private static readonly string[] Bilanzdateien =
        {
            "Bilanzreihe.cs", "Formvektor.cs", "Herkunftsprotokoll.cs", "Kaltwassergang.cs", "Mengengeruest.cs",
            "Nutzungsart.cs", "Parametersatz.cs", "Provenienz.cs", "Zapfauswertung.cs", "Zapfkalender.cs",
            "ZapfprofilErgebnis.cs", "ZapfprofilRechner.cs", "ZapfprofilStand.cs", "Zapfprofileingang.cs",
            "Zirkulationskanal.cs",
            // Stufe Z3: das Ensemble der Jahresreihe („stochastisch") ist Bilanz.
            "Jahresensemble.cs",
            // Stufe Z4b: die eingespielten Typtage tragen den JAHRESGANG der Bilanz; die Auslegung
            // (Wochenreihe, Bedarfstag, Summenlinie) bleibt unberührt (N14 (j)).
            "Typtagsatz.cs", "Typtagzuordnung.cs"
        };

        /// <summary>Die Dateien, deren Typen Minuten- bzw. Stundenwerte tragen dürfen.</summary>
        private static readonly string[] ZahlenfeldDateien = { "Bedarfstag.cs", "Wochenreihe.cs" };

        /// <summary>
        /// Die begründeten Ausnahmen vom Zahlenfeldsatz außerhalb der beiden Dateien — je Typ
        /// und Glied mit Grund. Jede andere Zahlenliste eines Auslegungstyps ist ein Verstoß.
        /// </summary>
        private static readonly (string Typ, string Glied, string Grund)[] Ausnahmen =
        {
            ("Summenliniennachweis", "InhaltKwh",
             "Speicherinhalt je Minute des Bedarfstags (1441 Werte) — Minutenwerte der Auslegung, keine Jahresreihe."),
            ("Speicherauslegungsergebnis", "DefizitKwh",
             "Defizit D(t) der doppelten Wochenreihe (336 h) — Stundenwerte der Auslegung, keine Jahresreihe."),
            ("Nenninhaltsliste", "WerteL", "Liste der Speicher-Nenninhalte (Einstellung), keine Zeitreihe."),
            ("Nenninhaltsliste", "Aus", "Bildet die Liste der Nenninhalte aus Werten (Einstellung), keine Zeitreihe."),
            ("Perzentilwerte", "Aus",
             "Perzentile einer Stichprobe je Realisierung des Auslegungsensembles (R Werte), keine Zeitreihe."),
        };

        /// <summary>Gegenprobe des Zahlenfeldsatzes: eine Zahlenliste als Konstruktorparameter, Rückgabe, Eigenschaft und Feld.</summary>
        private sealed class Zahlenlistenprobe
        {
            internal Zahlenlistenprobe(double[] werteKwh) { }
            internal IReadOnlyList<double> Reihe() => null;
            internal IReadOnlyList<double> ReiheKwh { get; } = null;
            internal List<double> FeldKwh = null;
        }

        /// <summary>Die Bezeichner der Bilanz, die keine Auslegungsdatei nennt.</summary>
        private static readonly Regex Bilanzbezug = new Regex(
            @"\bBilanzreihe\b|\bZapfprofilErgebnis\b|\bZapfprofilRechner\b|\bZonenErgebnis\b|\bStundenreihe\s*\("
            + @"|\bJahresensemble\b|\bJahreszone\b|\bJahreskonsistenz\b",
            RegexOptions.Compiled);

        /// <summary>Eine Typdeklaration auf Namensraumebene (vier Leerzeichen Einzug).</summary>
        private static readonly Regex Deklaration = new Regex(
            @"^    internal\s+(?:(?:sealed|static|readonly|abstract)\s+)*(?:record\s+struct|record|class|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        private static readonly Type[] Bilanztypen =
        {
            typeof(Bilanzreihe), typeof(ZapfprofilErgebnis), typeof(ZonenErgebnis),
            typeof(Jahresensemble), typeof(Jahreszone), typeof(Jahreskonsistenz)
        };

        // =====================================================================
        //  Satz 1 — Quelltext
        // =====================================================================

        [Fact]
        public void Keine_Auslegungsdatei_nennt_die_Bilanzreihe()
        {
            var funde = new List<string>();
            foreach (string datei in Dateien)
                funde.AddRange(Bilanzfunde(datei, Lesen(datei)));
            Assert.True(funde.Count == 0, "Die Auslegung nennt die Bilanz:\n" + string.Join("\n", funde));

            // Gegenproben zum Leser.
            Assert.Matches(Bilanzbezug, "Bilanzreihe b = e.Zapfung;");
            Assert.Matches(Bilanzbezug, "double[] s = Formvektor.Stundenreihe(t, s, k);");
            Assert.Matches(Bilanzbezug, "var e = ZapfprofilRechner.Rechnen(x, k);");
            Assert.Matches(Bilanzbezug, "Jahresensemble j = Jahresensemble.Ziehen(z, 1, 10);");
            Assert.DoesNotMatch(Bilanzbezug, "Bedarfstagensemble b = Zapfensemble.Ziehen(z, 1, 100, 99);");
            Assert.DoesNotMatch(Bilanzbezug, "Wochenreihe w = Wochenreihe.Bilden(z, 0, r); // ohne Stundenreihe");
            Assert.True(Kommentarzeilen(new[] { "        /// ohne Stundenreihe(…) der Bilanz" })[0]);
            Assert.NotEmpty(Bilanzfunde("ZapfprofilRechner.cs", Lesen("ZapfprofilRechner.cs")));

            // Gegenprobe zum Stern: Innerhalb /* … */ ist eine Zeile mit „*" Kommentar, außerhalb
            // Code — etwa die Fortsetzung eines Produkts, die die Bilanz nennt.
            string[] block = { "        /*", "         * Formvektor.Stundenreihe(t, s, k) der Bilanz", "         */",
                               "        double x = a" , "            * Formvektor.Stundenreihe(t, s, k).Length;" };
            Assert.Equal(new[] { true, true, true, false, false }, Kommentarzeilen(block));
            Assert.Equal(new[] { "Probe:5" }, Bilanzfunde("Probe", block).Select(f => f.Substring(0, f.IndexOf(' '))).ToArray());
        }

        // =====================================================================
        //  Satz 2 — Signaturen
        // =====================================================================

        [Fact]
        public void Keine_Auslegungsklasse_nimmt_eine_Bilanzreihe_oder_ein_Jahresfeld_an()
        {
            var funde = new List<string>();
            int geprueft = 0;
            foreach (string datei in Dateien)
            {
                bool zahlenfeld = ZahlenfeldDateien.Contains(datei);
                foreach (string name in Deklarationen(datei))
                {
                    Type t = Typ(name);
                    funde.AddRange(Verstoesse(t, !zahlenfeld, Ausnahmen));
                    geprueft++;
                }
            }
            Assert.True(geprueft >= 40, "Nur " + geprueft + " Auslegungstypen geprüft — die Wache sieht die Dateien nicht.");
            Assert.True(funde.Count == 0, "Die Auslegung nimmt die Bilanz oder ein Zahlenfeld an:\n" + string.Join("\n", funde));

            // Gegenproben: die Bilanzfassade und der Formvektor verletzen die Regel.
            Assert.Contains(Verstoesse(typeof(ZapfprofilRechner), true), f => f.Contains("Bilanzreihe"));
            Assert.Contains(Verstoesse(typeof(Formvektor), true), f => f.Contains("Stundenreihe") && f.Contains("Double[]"));
            Assert.Contains(Verstoesse(typeof(ZapfprofilErgebnis), false), f => f.Contains("Bilanzreihe"));
            Assert.Contains(Verstoesse(typeof(Jahresensemble), false), f => f.Contains("Bilanzreihe"));
            Assert.Contains(Verstoesse(typeof(ZonenErgebnis), false), f => f.Contains("Jahreskonsistenz"));
            // Die Minuten- und Stundentypen tragen ihre Werte, aber keine Bilanz.
            Assert.Empty(Verstoesse(typeof(Bedarfstag), false));
            Assert.Empty(Verstoesse(typeof(Wochenreihe), false));
            Assert.NotEmpty(Verstoesse(typeof(Wochenreihe), true));

            // Gegenproben zu den Zahlenlisten: Rückgabe, Eigenschaft und Feld werden erkannt …
            string[] probe = Verstoesse(typeof(Zahlenlistenprobe), true).ToArray();
            Assert.Contains(probe, f => f.StartsWith("Zahlenlistenprobe.Reihe gibt IReadOnlyList", StringComparison.Ordinal));
            Assert.Contains(probe, f => f.StartsWith("Zahlenlistenprobe.ReiheKwh ist IReadOnlyList", StringComparison.Ordinal));
            Assert.Contains(probe, f => f.StartsWith("Zahlenlistenprobe.FeldKwh ist List", StringComparison.Ordinal));
            Assert.Contains(probe, f => f.StartsWith("Zahlenlistenprobe.ctor nimmt Double[] (werteKwh)", StringComparison.Ordinal));
            // … und jede benannte Ausnahme wäre ohne ihren Eintrag ein Verstoß (keine Ausnahme auf Vorrat).
            foreach (var (typ, glied, grund) in Ausnahmen)
            {
                Assert.False(string.IsNullOrWhiteSpace(grund));
                Assert.Contains(Verstoesse(Typ(typ), true), f => f.StartsWith(typ + "." + glied + " ", StringComparison.Ordinal));
                Assert.DoesNotContain(Verstoesse(Typ(typ), true, Ausnahmen), f => f.StartsWith(typ + "." + glied + " ", StringComparison.Ordinal));
            }
        }

        // =====================================================================
        //  Stufe Z3 — das Ensemble reicht keine Bilanzreihe in die Auslegung
        // =====================================================================

        [Fact]
        public void Das_Ensemble_reicht_keine_Bilanzreihe_in_die_Auslegung()
        {
            // Das Auslegungsensemble steht unter der strengen Regel, das der Jahresreihe ist Bilanz.
            Assert.Contains("Zapfensemble.cs", Dateien);
            Assert.Contains("Jahresensemble.cs", Bilanzdateien);
            foreach (string typ in new[] { "Zapfensemble", "Bedarfstagensemble", "Ensemblezone", "Ensemblezonenstatistik",
                                           "Perzentilwerte", "Speicherensemble", "Realisierungskennzahl", "Vertretertag",
                                           "Volumenauftrag" })
            {
                Assert.Contains(typ, Deklarationen("Zapfensemble.cs"));
                Assert.Empty(Verstoesse(Typ(typ), true, Ausnahmen));
            }
            Assert.Empty(Verstoesse(typeof(Perzentilergebnis), true, Ausnahmen));
            // Die Minutenstatistik der Einheiten ist ein Minutentyp neben dem Bedarfstag (Zahlenfelddatei);
            // die Zonenstatistik des Ensembles nimmt sie als Typ, nie als Zahlenfeld.
            Assert.Contains("Minutenstatistik", Deklarationen("Bedarfstag.cs"));
            Assert.Contains(typeof(Ensemblezonenstatistik).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                .SelectMany(c => c.GetParameters()), p => p.ParameterType == typeof(Minutenstatistik));
            Assert.NotEmpty(Verstoesse(typeof(Minutenstatistik), true));

            // Die Fassade der Auslegung ruft das Auslegungsensemble — und nennt das der Jahresreihe nicht.
            string[] auslegung = Lesen("ZapfprofilAuslegung.cs");
            bool[] kommentar = Kommentarzeilen(auslegung);
            Assert.Contains(auslegung.Where((z, i) => !kommentar[i]), z => z.Contains("Zapfensemble.Ziehen("));
            Assert.Empty(Bilanzfunde("ZapfprofilAuslegung.cs", auslegung));
            // Gegenprobe: der Bilanzrechenweg ruft das Jahresensemble und wird erkannt.
            Assert.Contains(Bilanzfunde("ZapfprofilRechner.cs", Lesen("ZapfprofilRechner.cs")), f => f.Contains("Jahresensemble"));
            // Minutenwerte des Ensembles nur als Bedarfstag: die aufbewahrten Vertretertage und jeder
            // nachgezogene Tag sind Bedarfstage.
            const BindingFlags alle = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Assert.Equal(typeof(IReadOnlyList<Vertretertag>), typeof(Bedarfstagensemble).GetProperty("VertreterMinutenspitze", alle).PropertyType);
            Assert.Equal(typeof(Bedarfstag), typeof(Vertretertag).GetProperty("Tag", alle).PropertyType);
            Assert.Equal(typeof(Bedarfstag), typeof(Bedarfstagensemble).GetMethod("Tag", alle).ReturnType);
        }

        // =====================================================================
        //  Satz 3 — Vollständigkeit
        // =====================================================================

        [Fact]
        public void Jeder_Auslegungstyp_steht_in_der_Wache()
        {
            var fehlend = new List<string>();
            foreach (string datei in Dateien)
                foreach (string name in Deklarationen(datei))
                    if (typeof(Summenlinie).Assembly.GetType("WindowsFormsApplication1." + name) == null)
                        fehlend.Add(datei + ": " + name);
            Assert.True(fehlend.Count == 0, "Typen ohne Entsprechung in der Baugruppe:\n" + string.Join("\n", fehlend));

            // Jede Datei des Ordners ist entweder Bilanz oder Auslegung — eine neue Datei muss sich einordnen.
            string[] ordner = Directory.GetFiles(Ordner(), "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            string[] bekannt = Dateien.Concat(Bilanzdateien).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.True(ordner.SequenceEqual(bekannt), "Dateien des Zapfprofil-Ordners ohne Zuordnung zu Bilanz oder Auslegung: "
                + string.Join(", ", ordner.Except(bekannt).Concat(bekannt.Except(ordner).Select(x => "fehlt: " + x))));
            Assert.Empty(Dateien.Intersect(Bilanzdateien));
            foreach (var (typ, _, _) in Ausnahmen)
                Assert.Contains(Dateien, d => Deklarationen(d).Contains(typ));
            Assert.Contains("ZapfprofilAuslegung", Deklarationen("ZapfprofilAuslegung.cs"));
            Assert.DoesNotContain("Zonenarbeit", Deklarationen("ZapfprofilAuslegung.cs"));   // geschachtelt, privat
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Die Verstöße eines Typs: Bilanztypen in jedem nicht privaten Glied; mit
        /// <paramref name="zahlenfeldsatz"/> zusätzlich Zahlenfelder (<see cref="IstZahlenfeld"/>)
        /// als Methoden- oder Konstruktorparameter, Rückgabe, Eigenschaft oder Feld — außer den Gliedern in
        /// <paramref name="ausnahmen"/>.
        /// </summary>
        private static IEnumerable<string> Verstoesse(Type t, bool zahlenfeldsatz,
                                                      IEnumerable<(string Typ, string Glied, string Grund)> ausnahmen = null)
        {
            const BindingFlags alle = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic
                                      | BindingFlags.Instance | BindingFlags.Static;
            string n = t.Name + ".";
            var frei = new HashSet<string>((ausnahmen ?? Enumerable.Empty<(string, string, string)>())
                                           .Where(a => a.Item1 == t.Name).Select(a => a.Item2), StringComparer.Ordinal);
            // Eigenschaftszugriffe und Operatoren prüft die Eigenschaftsschleife bzw. sie tragen den Typ
            // selbst; vom Übersetzer erzeugte Glieder (lokale Funktionen, Klonen) heißen mit „<".
            foreach (MethodInfo m in t.GetMethods(alle).Where(m => !m.IsPrivate && !m.IsSpecialName
                                                                   && !m.Name.StartsWith("<", StringComparison.Ordinal)))
            {
                bool liste = zahlenfeldsatz && !frei.Contains(m.Name);
                if (TraegtBilanz(m.ReturnType)) yield return n + m.Name + " gibt " + m.ReturnType.Name + " heraus";
                if (liste && IstZahlenfeld(m.ReturnType)) yield return n + m.Name + " gibt " + Anzeige(m.ReturnType) + " heraus";
                foreach (ParameterInfo p in m.GetParameters())
                {
                    Type pt = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                    if (TraegtBilanz(pt)) yield return n + m.Name + " nimmt " + pt.Name + " (" + p.Name + ")";
                    if (liste && !p.IsOut && IstZahlenfeld(pt))
                        yield return n + m.Name + " nimmt " + Anzeige(pt) + " (" + p.Name + ")";
                }
            }
            // Konstruktoren: Bilanztypen immer; Zahlenfelder unter dem Zahlenfeldsatz — ausgenommen der
            // Parameter eines positionalen Datensatzes, dessen gleichnamiges Glied benannt ausgenommen ist.
            foreach (ConstructorInfo c in t.GetConstructors(alle).Where(c => !c.IsPrivate))
                foreach (ParameterInfo p in c.GetParameters())
                {
                    if (TraegtBilanz(p.ParameterType)) yield return n + "ctor nimmt " + p.ParameterType.Name;
                    if (zahlenfeldsatz && !frei.Contains(p.Name ?? "") && IstZahlenfeld(p.ParameterType))
                        yield return n + "ctor nimmt " + Anzeige(p.ParameterType) + " (" + p.Name + ")";
                }
            foreach (PropertyInfo p in t.GetProperties(alle))
            {
                MethodInfo g = p.GetGetMethod(true);
                if (g == null || g.IsPrivate) continue;
                if (TraegtBilanz(p.PropertyType)) yield return n + p.Name + " ist " + p.PropertyType.Name;
                if (zahlenfeldsatz && !frei.Contains(p.Name) && IstZahlenfeld(p.PropertyType))
                    yield return n + p.Name + " ist " + Anzeige(p.PropertyType);
            }
            foreach (FieldInfo f in t.GetFields(alle).Where(f => !f.IsPrivate))
            {
                if (TraegtBilanz(f.FieldType)) yield return n + f.Name + " ist " + f.FieldType.Name;
                if (zahlenfeldsatz && !frei.Contains(f.Name) && IstZahlenfeld(f.FieldType))
                    yield return n + f.Name + " ist " + Anzeige(f.FieldType);
            }
        }

        private static bool TraegtBilanz(Type t)
        {
            if (t == null) return false;
            if (t.IsArray || t.IsByRef) return TraegtBilanz(t.GetElementType());
            if (Bilanztypen.Contains(t)) return true;
            return t.IsGenericType && t.GetGenericArguments().Any(TraegtBilanz);
        }

        private static bool IstZahlenfeld(Type t)
        {
            if (t == typeof(double[]) || t == typeof(double[,])) return true;
            if (!t.IsGenericType) return false;
            Type d = t.GetGenericTypeDefinition();
            return t.GetGenericArguments()[0] == typeof(double)
                   && (d == typeof(IReadOnlyList<>) || d == typeof(IList<>) || d == typeof(List<>)
                       || d == typeof(ICollection<>) || d == typeof(IEnumerable<>) || d == typeof(IReadOnlyCollection<>));
        }

        private static string Anzeige(Type t) => t.IsGenericType ? t.Name + "<Double>" : t.Name;

        private static Type Typ(string name)
        {
            Type t = typeof(Summenlinie).Assembly.GetType("WindowsFormsApplication1." + name);
            Assert.True(t != null, "Typ nicht gefunden: " + name);
            return t;
        }

        private static IReadOnlyList<string> Deklarationen(string datei)
            => Lesen(datei).Select(z => Deklaration.Match(z)).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();

        private static string[] Lesen(string datei)
            => File.ReadAllText(Path.Combine(Ordner(), datei)).Replace("\r\n", "\n").Split('\n');

        /// <summary>Die Zeilen, die die Bilanz nennen und kein Kommentar sind, als „Datei:Zeile  Text".</summary>
        private static List<string> Bilanzfunde(string datei, string[] zeilen)
        {
            bool[] kommentar = Kommentarzeilen(zeilen);
            var funde = new List<string>();
            for (int i = 0; i < zeilen.Length; i++)
                if (!kommentar[i] && Bilanzbezug.IsMatch(zeilen[i]))
                    funde.Add(datei + ":" + (i + 1) + "  " + zeilen[i].Trim());
            return funde;
        }

        /// <summary>
        /// Welche Zeilen Kommentar sind: eine Zeile, die mit <c>//</c> oder <c>/*</c> beginnt, und
        /// jede Zeile, solange ein Block <c>/* … */</c> offen ist. Eine Zeile, die mit <c>*</c>
        /// beginnt, ist nur innerhalb des Blocks Kommentar — außerhalb ist sie Code.
        /// </summary>
        private static bool[] Kommentarzeilen(string[] zeilen)
        {
            var k = new bool[zeilen.Length];
            bool imBlock = false;
            for (int i = 0; i < zeilen.Length; i++)
            {
                string s = zeilen[i].TrimStart();
                if (imBlock)
                {
                    k[i] = true;
                    if (s.Contains("*/")) imBlock = false;
                }
                else if (s.StartsWith("//", StringComparison.Ordinal))
                {
                    k[i] = true;
                }
                else if (s.StartsWith("/*", StringComparison.Ordinal))
                {
                    k[i] = true;
                    imBlock = !s.Substring(2).Contains("*/");
                }
            }
            return k;
        }

        private static string Ordner()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern", "Allgemein", "Zapfprofil");
                if (Directory.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Der Ordner EPOS.Kern/Allgemein/Zapfprofil wurde nicht gefunden.");
            return null;
        }
    }
}
