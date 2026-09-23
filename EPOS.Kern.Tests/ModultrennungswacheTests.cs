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
    /// <b>Die Modultrennungswache</b> (Entscheid E20, ADR-006; Umsetzungskonzept
    /// Gebäudesimulation 1.9, Softwarearchitektur 1.7 Regel AR16). Die Gebäudebedarfsrechnung
    /// hat zwei getrennte Module hinter einer Weiche: den Tagesbilanz-Weg in
    /// <c>EPOS.Kern/Allgemein/Simulation/Altweg/</c> und den VDI-6007-Weg in
    /// <c>…/Simulation/Gebaeude/</c>. Ohne Halt wandert beim ersten gemeinsam gebrauchten
    /// Hilfsstück wieder ein Aufruf über die Grenze, und der byte-gleiche Nachweis des
    /// Altwegs wäre nicht mehr zu führen.
    ///
    /// <para><b>Vier Sätze:</b> (1) Keine Datei unter <c>Gebaeude/</c> nennt einen Bezeichner
    /// aus <c>Altweg/</c> oder eine Altweg-Datenquelle. (2) Keine Datei unter <c>Altweg/</c>
    /// nennt einen Bezeichner aus <c>Gebaeude/</c>. (3) Die Kältefassade
    /// <c>SimulationKaeltebedarf</c> nennt <c>Altweg/</c> nicht, der <c>Anlagenfahrplan</c>
    /// keines der beiden Module — beide Dateien entstehen erst mit KU1 bzw. AK2; bis dahin
    /// prüft Satz 4 sie mit. (4) <b>Ausbauprobe, statischer Teil:</b> Außer der Weiche in
    /// <c>SimulationWaermebedarf.cs</c> nennt keine Datei des Kerns (<c>EPOS.Kern/</c>)
    /// <c>Altweg/</c>. Tests, die den Altweg selbst prüfen, liegen außerhalb des Kerns.</para>
    ///
    /// <para><b>„Nennen" heißt:</b> auf einer Zeile, die der Übersetzer sieht (Kommentare
    /// ausgenommen), steht der Namensraum <c>WindowsFormsApplication1.Altweg</c>, ein
    /// qualifizierender Zugriff <c>Altweg.…</c> oder einer der Typnamen, die im jeweils
    /// anderen Ordner deklariert sind. Die Namenslisten entstehen aus den Ordnern selbst; die
    /// Gegenprobe hält sie gegen die Dateien.</para>
    ///
    /// <para>Die Wache lebt bis zur Stufe GA; Satz 4 ist die Vorstufe des Gates von GA, der
    /// vollständigen Ausbauprobe.</para>
    /// </summary>
    public class ModultrennungswacheTests
    {
        private const string ALTWEG_NAMENSRAUM = "WindowsFormsApplication1.Altweg";

        /// <summary>
        /// Satz 1, zweiter Teil: die Datenquellen und Methoden, die allein der Altweg liest
        /// oder trägt (Umsetzungskonzept 1.9). Der Gebäudetyp als Verteilungsschlüssel
        /// erscheint als Zugriff <c>.Typ</c>.
        /// </summary>
        private static readonly string[] AltwegQuellen =
        {
            "Berechnung_Gebaeude_Tageswerte", "DBTagesVeteilung", "FerienmaskeBilden",
            "StdWerte", "SolareGewinneC", "SpezWaermeverlusteC", "TaeglHeizlastWG",
            "Sol_N", "Sol_O", "Sol_S", "Sol_W", "Sol_w", "A_Temp", "TagTyp_W", "TagTyp_NW",
            "Tab_DBTagV", "Abfrage_Tagverteilung", "Fensterflaeche_Ost_West",
            "KlimakalenderAltweg",
        };

        // =====================================================================
        //  Die vier Sätze
        // =====================================================================

        [Fact]
        public void Satz1_Gebaeude_nennt_nichts_aus_dem_Altweg()
        {
            var muster = AltwegMuster()
                .Concat(AltwegQuellen.Select(Wort))
                .Append(new Regex(@"\.Altweg\b"))      // Klimakalender.Altweg
                .Append(new Regex(@"\.Typ\b"))          // Typ als Verteilungsschlüssel
                .ToArray();

            var funde = Funde(Dateien(Gebaeudeordner()), muster);
            Assert.True(funde.Count == 0,
                "Das Modul Gebaeude/ nennt den Altweg (E20):\n" + string.Join("\n", funde));
        }

        [Fact]
        public void Satz2_Altweg_nennt_nichts_aus_Gebaeude()
        {
            var muster = Typnamen(Gebaeudeordner()).Select(Wort).ToArray();

            var funde = Funde(Dateien(Altwegordner()), muster);
            Assert.True(funde.Count == 0,
                "Das Modul Altweg/ nennt das Modul Gebaeude/ (E20):\n" + string.Join("\n", funde));
        }

        [Fact]
        public void Satz3_Kaeltefassade_und_Anlagenfahrplan_nennen_den_Altweg_nicht()
        {
            string[] kern = Dateien(Kernordner());
            var altweg = AltwegMuster();
            var gebaeude = Typnamen(Gebaeudeordner()).Select(Wort).ToArray();

            var funde = new List<string>();
            funde.AddRange(Funde(kern.Where(d => Path.GetFileName(d) == "SimulationKaeltebedarf.cs"), altweg));
            funde.AddRange(Funde(kern.Where(d => Path.GetFileName(d) == "Anlagenfahrplan.cs"),
                                 altweg.Concat(gebaeude).ToArray()));

            Assert.True(funde.Count == 0,
                "Kältefassade oder Anlagenfahrplan nennen ein Modul (E21, AK2):\n" + string.Join("\n", funde));
        }

        [Fact]
        public void Satz4_Ausser_der_Weiche_nennt_keine_Kerndatei_den_Altweg()
        {
            string altweg = Altwegordner() + Path.DirectorySeparatorChar;
            string weiche = Weichendatei();

            IEnumerable<string> dateien = Dateien(Kernordner())
                .Where(d => !d.StartsWith(altweg, StringComparison.OrdinalIgnoreCase))
                .Where(d => !string.Equals(d, weiche, StringComparison.OrdinalIgnoreCase));

            var funde = Funde(dateien, AltwegMuster());
            Assert.True(funde.Count == 0,
                "Außer der Weiche nennt eine Datei des Kerns den Altweg (Ausbauprobe, statischer Teil):\n"
                + string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// Die Wache sieht, was sie bewacht: Der Ordner <c>Altweg/</c> existiert und trägt die
        /// drei Typen des Tagesbilanz-Wegs, <c>Gebaeude/</c> trägt den Löser, und die Weiche
        /// nennt den Altweg wirklich — sonst wäre ihre Ausnahme in Satz 4 hinfällig.
        /// </summary>
        [Fact]
        public void Die_Wache_sieht_beide_Module_und_die_Weiche()
        {
            Assert.True(Directory.Exists(Altwegordner()), "Ordner fehlt: " + Altwegordner());
            Assert.NotEmpty(Dateien(Altwegordner()));
            string[] altweg = Typnamen(Altwegordner());
            Assert.Contains("TagesbilanzRechenweg", altweg);
            Assert.Contains("TagesbilanzPhysik", altweg);
            Assert.Contains("Tagesbilanzzustand", altweg);

            Assert.Contains("Zonenmodell2K", Typnamen(Gebaeudeordner()));

            Assert.True(File.Exists(Weichendatei()), "Weiche fehlt: " + Weichendatei());
            Assert.NotEmpty(Funde(new[] { Weichendatei() }, AltwegMuster()));
        }

        /// <summary>Der Leser erkennt einen Verstoß und schont Kommentare.</summary>
        [Fact]
        public void Der_Leser_erkennt_einen_Verstoss_und_schont_Kommentare()
        {
            Regex[] muster = AltwegMuster();
            Assert.True(Trifft("            var p = new TagesbilanzRechenweg(kalender);", muster));
            Assert.True(Trifft("using WindowsFormsApplication1.Altweg;", muster));
            Assert.True(Trifft("            Altweg.Tagesbilanzzustand z = null;", muster));
            Assert.False(Trifft("            double x = kalender.Altweg.Sol_N[0];", new[] { Wort("TagesbilanzPhysik") }));
            Assert.False(Trifft("        /// <see cref=\"TagesbilanzRechenweg\"/>", muster));
            Assert.False(Trifft("            int n = 0; // wie im TagesbilanzRechenweg", muster));
            Assert.False(Trifft("            string s = \"Der Altweg\";", muster));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        private static Regex Wort(string name) => new Regex(@"\b" + Regex.Escape(name) + @"\b");

        /// <summary>Namensraum, qualifizierender Zugriff und Typnamen des Altwegs.</summary>
        private static Regex[] AltwegMuster()
        {
            return new[]
                {
                    new Regex(Regex.Escape(ALTWEG_NAMENSRAUM) + @"\b"),
                    new Regex(@"(?<![\w.])Altweg\s*\.\s*[A-Za-z_]"),
                }
                .Concat(Typnamen(Altwegordner()).Select(Wort))
                .ToArray();
        }

        private static bool Trifft(string zeile, Regex[] muster)
        {
            string code = Code(zeile);
            return code.Length > 0 && muster.Any(m => m.IsMatch(code));
        }

        private static List<string> Funde(IEnumerable<string> dateien, Regex[] muster)
        {
            var funde = new List<string>();
            foreach (string datei in dateien)
            {
                string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                    if (Trifft(zeilen[i], muster))
                        funde.Add(datei + ":" + (i + 1) + "  " + zeilen[i].Trim());
            }
            return funde;
        }

        /// <summary>
        /// Was der Übersetzer von einer Zeile sieht: ohne Kommentarzeilen (<c>//</c>,
        /// <c>///</c>, <c>*</c>, <c>/*</c>), ohne einen nachgestellten <c>//</c>-Kommentar und
        /// ohne Zeichenkettenliterale. Bewusst einfach, wie in <c>DoubleWacheTests</c>.
        /// </summary>
        private static string Code(string zeile)
        {
            string t = zeile.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("/*", StringComparison.Ordinal)
                || t.StartsWith("*", StringComparison.Ordinal))
                return "";
            string ohneText = Regex.Replace(t, "\"(?:[^\"\\\\]|\\\\.)*\"", "\"\"");
            int k = ohneText.IndexOf("//", StringComparison.Ordinal);
            return k >= 0 ? ohneText.Substring(0, k) : ohneText;
        }

        /// <summary>Die Namen der Typen, die in einem Ordner deklariert sind.</summary>
        private static string[] Typnamen(string ordner)
        {
            var deklaration = new Regex(@"\b(?:class|interface|struct|record|enum)\s+([A-Za-z_]\w*)");
            var namen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string datei in Dateien(ordner))
                foreach (string zeile in File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n'))
                    foreach (Match m in deklaration.Matches(Code(zeile)))
                        namen.Add(m.Groups[1].Value);
            return namen.ToArray();
        }

        private static string[] Dateien(string ordner)
        {
            char t = Path.DirectorySeparatorChar;
            return Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories)
                .Where(p => p.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0
                         && p.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
        }

        private static string Kernordner() => Path.Combine(Arbeitsbaum(), "EPOS.Kern");

        private static string Simulationsordner() => Path.Combine(Kernordner(), "Allgemein", "Simulation");

        private static string Altwegordner() => Path.Combine(Simulationsordner(), "Altweg");

        private static string Gebaeudeordner() => Path.Combine(Simulationsordner(), "Gebaeude");

        private static string Weichendatei() => Path.Combine(Simulationsordner(), "SimulationWaermebedarf.cs");

        /// <summary>
        /// Die Wurzel des Arbeitsbaums — derselbe Weg wie in <see cref="DoubleWacheTests"/>:
        /// erst über den eigenen Quelltextort, sonst vom Ausgabeordner aufwärts.
        /// </summary>
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
