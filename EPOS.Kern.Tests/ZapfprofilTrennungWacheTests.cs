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
    /// Parameter noch als Rückgabe, Eigenschaft oder Feld, auch nicht als Typargument; keine
    /// Methode nimmt ein Zahlenfeld an (<c>double[]</c>, <c>double[,]</c>,
    /// <c>IReadOnlyList&lt;double&gt;</c>, <c>IList</c>, <c>List</c>, <c>ICollection</c>,
    /// <c>IEnumerable</c> von <c>double</c>), und kein Glied gibt ein <c>double[]</c> heraus.
    /// Minutenwerte kommen nur über <see cref="Bedarfstag"/>, Stundenwerte nur über
    /// <see cref="Wochenreihe"/> (168 h) — deren Dateien sind vom Zahlenfeldsatz ausgenommen,
    /// nicht vom Bilanzsatz. (3) <b>Vollständigkeit:</b> Jeder Typ, den eine Auslegungsdatei auf
    /// Namensraumebene deklariert, steht in der Liste dieser Wache — ein neuer Typ entgeht ihr
    /// nicht.</para>
    ///
    /// <para><b>Gegenproben:</b> Die Bilanzfassade <see cref="ZapfprofilRechner"/> und
    /// <see cref="Formvektor"/> verletzen die Regel und müssen erkannt werden.</para>
    /// </summary>
    public sealed class ZapfprofilTrennungWacheTests
    {
        /// <summary>Die Dateien der Auslegung unter <c>EPOS.Kern/Allgemein/Zapfprofil/</c>.</summary>
        private static readonly string[] Dateien =
        {
            "Auslegungsparameter.cs", "Bedarfstag.cs", "Wochenreihe.cs", "Summenlinie.cs", "Din4708Kennzahl.cs",
            "TwwSpeicherauslegung.cs", "Grossanlage.cs", "Auslegungsergebnis.cs", "ZapfprofilAuslegung.cs"
        };

        /// <summary>Die Dateien der Bilanz (Stufe Z1) — jede Datei des Ordners gehört zu genau einer Liste.</summary>
        private static readonly string[] Bilanzdateien =
        {
            "Bilanzreihe.cs", "Formvektor.cs", "Herkunftsprotokoll.cs", "Kaltwassergang.cs", "Mengengeruest.cs",
            "Nutzungsart.cs", "Parametersatz.cs", "Provenienz.cs", "Zapfauswertung.cs", "Zapfkalender.cs",
            "ZapfprofilErgebnis.cs", "ZapfprofilRechner.cs", "ZapfprofilStand.cs", "Zapfprofileingang.cs",
            "Zirkulationskanal.cs"
        };

        /// <summary>Die Dateien, deren Typen Minuten- bzw. Stundenwerte tragen dürfen.</summary>
        private static readonly string[] ZahlenfeldDateien = { "Bedarfstag.cs", "Wochenreihe.cs" };

        /// <summary>
        /// Die begründeten Ausnahmen vom Zahlenfeldsatz außerhalb der beiden Dateien — je Typ mit
        /// Grund.
        /// </summary>
        private static readonly (string Typ, string Grund)[] Ausnahmen =
        {
            ("Nenninhaltsliste", "Liste der Speicher-Nenninhalte (Einstellung), keine Zeitreihe."),
        };

        /// <summary>Die Bezeichner der Bilanz, die keine Auslegungsdatei nennt.</summary>
        private static readonly Regex Bilanzbezug = new Regex(
            @"\bBilanzreihe\b|\bZapfprofilErgebnis\b|\bZapfprofilRechner\b|\bZonenErgebnis\b|\bStundenreihe\s*\(",
            RegexOptions.Compiled);

        /// <summary>Eine Typdeklaration auf Namensraumebene (vier Leerzeichen Einzug).</summary>
        private static readonly Regex Deklaration = new Regex(
            @"^    internal\s+(?:(?:sealed|static|readonly|abstract)\s+)*(?:record\s+struct|record|class|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        private static readonly Type[] Bilanztypen = { typeof(Bilanzreihe), typeof(ZapfprofilErgebnis), typeof(ZonenErgebnis) };

        // =====================================================================
        //  Satz 1 — Quelltext
        // =====================================================================

        [Fact]
        public void Keine_Auslegungsdatei_nennt_die_Bilanzreihe()
        {
            var funde = new List<string>();
            foreach (string datei in Dateien)
            {
                string[] zeilen = Lesen(datei);
                for (int i = 0; i < zeilen.Length; i++)
                    if (!IstKommentar(zeilen[i]) && Bilanzbezug.IsMatch(zeilen[i]))
                        funde.Add(datei + ":" + (i + 1) + "  " + zeilen[i].Trim());
            }
            Assert.True(funde.Count == 0, "Die Auslegung nennt die Bilanz:\n" + string.Join("\n", funde));

            // Gegenproben zum Leser.
            Assert.Matches(Bilanzbezug, "Bilanzreihe b = e.Zapfung;");
            Assert.Matches(Bilanzbezug, "double[] s = Formvektor.Stundenreihe(t, s, k);");
            Assert.Matches(Bilanzbezug, "var e = ZapfprofilRechner.Rechnen(x, k);");
            Assert.DoesNotMatch(Bilanzbezug, "Wochenreihe w = Wochenreihe.Bilden(z, 0, r); // ohne Stundenreihe");
            Assert.True(IstKommentar("        /// ohne Stundenreihe(…) der Bilanz"));
            Assert.Contains(Lesen("ZapfprofilRechner.cs"), z => !IstKommentar(z) && Bilanzbezug.IsMatch(z));
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
                    bool ausnahme = zahlenfeld || Ausnahmen.Any(a => a.Typ == name);
                    funde.AddRange(Verstoesse(t, !ausnahme));
                    geprueft++;
                }
            }
            Assert.True(geprueft >= 40, "Nur " + geprueft + " Auslegungstypen geprüft — die Wache sieht die Dateien nicht.");
            Assert.True(funde.Count == 0, "Die Auslegung nimmt die Bilanz oder ein Zahlenfeld an:\n" + string.Join("\n", funde));

            // Gegenproben: die Bilanzfassade und der Formvektor verletzen die Regel.
            Assert.Contains(Verstoesse(typeof(ZapfprofilRechner), true), f => f.Contains("Bilanzreihe"));
            Assert.Contains(Verstoesse(typeof(Formvektor), true), f => f.Contains("Stundenreihe") && f.Contains("Double[]"));
            Assert.Contains(Verstoesse(typeof(ZapfprofilErgebnis), false), f => f.Contains("Bilanzreihe"));
            // Die Minuten- und Stundentypen tragen ihre Werte, aber keine Bilanz.
            Assert.Empty(Verstoesse(typeof(Bedarfstag), false));
            Assert.Empty(Verstoesse(typeof(Wochenreihe), false));
            Assert.NotEmpty(Verstoesse(typeof(Wochenreihe), true));
            Assert.All(Ausnahmen, a => Assert.False(string.IsNullOrWhiteSpace(a.Grund)));
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
            foreach (var (typ, _) in Ausnahmen)
                Assert.Contains(Dateien, d => Deklarationen(d).Contains(typ));
            Assert.Contains("ZapfprofilAuslegung", Deklarationen("ZapfprofilAuslegung.cs"));
            Assert.DoesNotContain("Zonenarbeit", Deklarationen("ZapfprofilAuslegung.cs"));   // geschachtelt, privat
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Die Verstöße eines Typs: Bilanztypen in jedem nicht privaten Glied; mit
        /// <paramref name="zahlenfeldsatz"/> zusätzlich Zahlenfelder als Methodenparameter und
        /// <c>double[]</c> als Rückgabe, Eigenschaft oder Feld.
        /// </summary>
        private static IEnumerable<string> Verstoesse(Type t, bool zahlenfeldsatz)
        {
            const BindingFlags alle = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic
                                      | BindingFlags.Instance | BindingFlags.Static;
            string n = t.Name + ".";
            // Eigenschaftszugriffe und Operatoren prüft die Eigenschaftsschleife bzw. sie tragen den Typ
            // selbst; vom Übersetzer erzeugte Glieder (lokale Funktionen, Klonen) heißen mit „<".
            foreach (MethodInfo m in t.GetMethods(alle).Where(m => !m.IsPrivate && !m.IsSpecialName
                                                                   && !m.Name.StartsWith("<", StringComparison.Ordinal)))
            {
                if (TraegtBilanz(m.ReturnType)) yield return n + m.Name + " gibt " + m.ReturnType.Name + " heraus";
                if (zahlenfeldsatz && m.ReturnType == typeof(double[])) yield return n + m.Name + " gibt Double[] heraus";
                foreach (ParameterInfo p in m.GetParameters())
                {
                    Type pt = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                    if (TraegtBilanz(pt)) yield return n + m.Name + " nimmt " + pt.Name + " (" + p.Name + ")";
                    if (zahlenfeldsatz && !p.IsOut && IstZahlenfeld(pt))
                        yield return n + m.Name + " nimmt " + Anzeige(pt) + " (" + p.Name + ")";
                }
            }
            foreach (ConstructorInfo c in t.GetConstructors(alle).Where(c => !c.IsPrivate))
                foreach (ParameterInfo p in c.GetParameters())
                    if (TraegtBilanz(p.ParameterType)) yield return n + "ctor nimmt " + p.ParameterType.Name;
            foreach (PropertyInfo p in t.GetProperties(alle))
            {
                MethodInfo g = p.GetGetMethod(true);
                if (g == null || g.IsPrivate) continue;
                if (TraegtBilanz(p.PropertyType)) yield return n + p.Name + " ist " + p.PropertyType.Name;
                if (zahlenfeldsatz && p.PropertyType == typeof(double[])) yield return n + p.Name + " ist Double[]";
            }
            foreach (FieldInfo f in t.GetFields(alle).Where(f => !f.IsPrivate))
            {
                if (TraegtBilanz(f.FieldType)) yield return n + f.Name + " ist " + f.FieldType.Name;
                if (zahlenfeldsatz && f.FieldType == typeof(double[])) yield return n + f.Name + " ist Double[]";
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

        private static bool IstKommentar(string zeile)
        {
            string s = zeile.TrimStart();
            return s.StartsWith("//", StringComparison.Ordinal) || s.StartsWith("*", StringComparison.Ordinal)
                   || s.StartsWith("/*", StringComparison.Ordinal);
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
