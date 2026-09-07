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
    /// Der Wächter über die TYPREGEL des Rechenkerns
    /// (Anwenderentscheid <b>W8‑O‑5d</b> vom 07.09.2026: „alles in double, ist kein
    /// Nachteil und systematisch. Summenfunktionen aus Original BHKW-Plan ebenfalls
    /// double.").
    ///
    /// <para><b>Die Regel, die er hält.</b> Im Rechenweg — den sieben Simulationsklassen
    /// samt ihren Nachbarn in <c>EPOS.Kern/Allgemein/Simulation/</c> und den
    /// Summenfunktionen des BHKW-Plan-Ports (<c>BhkwPlan.cs</c>) — steht kein
    /// <c>float</c> mehr: nicht als Feldtyp, nicht als Parameter, nicht als Wandlung
    /// (<c>Convert.ToSingle</c>, <c>(float)</c>, <c>float.Parse</c>), nicht als Literal
    /// (<c>0f</c>). Zeitreihen, Akkumulatoren und Zwischenwerte haben dieselbe Breite
    /// wie die Zellen der Datenbank (SQLite <c>REAL</c> ist <c>double</c>).</para>
    ///
    /// <para><b>Warum es den Wächter braucht.</b> Der Bestand rechnete an vielen Stellen
    /// in <c>double</c> und SPEICHERTE in <c>float</c> — das war die bewusste Nachbildung
    /// des FPU-Verhaltens der alten <c>BHKWPLAN.DLL</c>. Wer eine Reihe neu anlegt, greift
    /// leicht wieder zum alten Muster; der Wächter meldet es beim Übersetzen der Tests
    /// statt erst beim nächsten Referenzlauf.</para>
    ///
    /// <para><b>Was NICHT geprüft wird</b> und warum (die drei Grenzen aus W8‑O‑5d):
    /// die Bildpunkte des <c>ChartRenderer</c> (SkiaSharp rechnet in <c>float</c>; die
    /// DATENREIHEN, die er annimmt, sind <c>double</c>), die Einbettungsvektoren des
    /// KI-Wissens (<c>SemantikIndex</c>/<c>SemantikModell</c> — kein Rechenweg) und die
    /// Typprüfungen auf boxed Datenbankwerte (<c>v is float</c>, <c>typeof(float)</c>),
    /// die als Absicherung stehen bleiben. Keine dieser Stellen liegt in den Ordnern,
    /// über die der Wächter läuft.</para>
    /// </summary>
    public class DoubleWacheTests
    {
        /// <summary>
        /// Jede Schreibweise, mit der <c>float</c> in den Quelltext kommt: der Typname
        /// als eigenes Wort (deckt <c>float x</c>, <c>float[]</c>, <c>(float)</c>,
        /// <c>float.Parse</c>, <c>float.Epsilon</c> ab), die zwei Wandlungshelfer und
        /// das Zahlenliteral mit <c>f</c>-Suffix.
        ///
        /// <para>Der Typname wird mit einem Blick nach links abgesichert: <c>ZuFloat</c>
        /// und <c>SingleToInt32Bits</c> sollen NICHT treffen, <c>(float)</c> schon.</para>
        /// </summary>
        private static readonly Regex Floatstelle = new Regex(
            @"(?<![A-Za-z0-9_.])float(?![A-Za-z0-9_])"
            + @"|(?<![A-Za-z0-9_])Convert\.ToSingle(?![A-Za-z0-9_])"
            + @"|(?<![A-Za-z0-9_.])MathF\."
            + @"|(?<![A-Za-z0-9_.])(?:\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)[fF](?![A-Za-z0-9_.])",
            RegexOptions.Compiled);

        /// <summary>
        /// Belegte Ausnahmen. <b>Leer</b> — der Rechenweg trägt seit W8‑O‑5d keine
        /// einzige <c>float</c>-Stelle mehr. Wer hier etwas einträgt, schreibt den Grund
        /// dazu; die Gegenprobe unten hält fest, dass jede eingetragene Stelle wirklich
        /// existiert.
        /// </summary>
        private static readonly (string Datei, string Ausdruck, string Grund)[] Ausnahmen =
            Array.Empty<(string, string, string)>();

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// Im Rechenweg steht kein <c>float</c>. Meldet Datei und Zeile jeder Fundstelle.
        /// </summary>
        [Fact]
        public void Im_Rechenweg_steht_kein_float_mehr()
        {
            var funde = new List<string>();

            foreach (string datei in Rechenwegdateien())
            {
                string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                {
                    string zeile = zeilen[i];
                    if (IstKommentar(zeile)) continue;
                    if (!Floatstelle.IsMatch(zeile)) continue;
                    if (IstAusgenommen(datei, zeile)) continue;

                    funde.Add(Path.GetFileName(datei) + ":" + (i + 1) + "  " + zeile.Trim());
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Stellen des Rechenwegs fuehren wieder float (Typregel W8-O-5d, " +
                "07.09.2026: der Kern rechnet und speichert in double). Bildpunkte des " +
                "ChartRenderer, KI-Einbettungen und Typpruefungen auf Datenbankwerte sind " +
                "die drei belegten Grenzen - sie liegen ausserhalb dieser Ordner.\n" +
                string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Jede Schreibweise trifft, und die Bezeichner, die
        /// das Wort nur ENTHALTEN, treffen nicht. Ohne diesen Fall wäre der Wächter oben
        /// stumm, sobald jemand den Typ anders hinschreibt.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_jede_Schreibweise()
        {
            Assert.Matches(Floatstelle, "        public float[] Waermebedarf = new float[8760];");
            Assert.Matches(Floatstelle, "            float summe = 0;");
            Assert.Matches(Floatstelle, "            x = (float)wert;");
            Assert.Matches(Floatstelle, "            float w = float.Parse(text);");
            Assert.Matches(Floatstelle, "            if (x > float.Epsilon) return;");
            Assert.Matches(Floatstelle, "            double w = Convert.ToSingle(r[0]);");
            Assert.Matches(Floatstelle, "            double a = MathF.Abs(b);");
            Assert.Matches(Floatstelle, "            acc = 0f;");
            Assert.Matches(Floatstelle, "            double q = 0.5f * x;");

            // Keine Fundstelle: der Typ double, ein Bezeichner, der das Wort enthaelt,
            // eine Zahl ohne f-Suffix und ein Zeichenkettenteil eines Namens.
            Assert.DoesNotMatch(Floatstelle, "        public double[] Waermebedarf = new double[8760];");
            Assert.DoesNotMatch(Floatstelle, "            var x = RasterAdapter.ZuFloat(reihe);");
            Assert.DoesNotMatch(Floatstelle, "            BitConverter.SingleToInt32Bits(x);");
            Assert.DoesNotMatch(Floatstelle, "            double q = 0.5 * x;");
            Assert.DoesNotMatch(Floatstelle, "            int n = 0x1f;");
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene
        /// Dateien — und über genug davon. Ein leerer Bestand liefe sonst grün durch,
        /// ohne je etwas geprüft zu haben. Dazu: jede Ausnahme steht wirklich dort.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert()
        {
            string[] dateien = Rechenwegdateien();
            Assert.True(dateien.Length > 25,
                        "Nur " + dateien.Length + " Rechenwegdateien gefunden.");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "BhkwPlan.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "SimulationControl.cs");

            foreach (var a in Ausnahmen)
            {
                bool da = dateien.Any(d => string.Equals(Path.GetFileName(d), a.Datei, StringComparison.Ordinal)
                                        && File.ReadAllText(d).Contains(a.Ausdruck, StringComparison.Ordinal));
                Assert.True(da, "Die Ausnahme '" + a.Ausdruck + "' steht nicht mehr in " + a.Datei +
                                " - sie gehoert aus der Liste entfernt.");
            }
        }

        /// <summary>
        /// <b>Gegenprobe zur Kommentarschonung:</b> Die Klassenköpfe NENNEN <c>float</c>
        /// absichtlich — sie erzählen, was der Port früher tat. Geprüft wird nur, was der
        /// Übersetzer sieht.
        /// </summary>
        [Fact]
        public void Ein_Kommentar_zaehlt_nicht_als_Fundstelle()
        {
            Assert.True(IstKommentar("        /// Die DLL akkumulierte in einer float-Speicherzelle."));
            Assert.True(IstKommentar("        // Datentyp der Vektoren war float."));
            Assert.True(IstKommentar("            /* float[] alt = ...; */"));
            Assert.False(IstKommentar("            float acc = 0f;"));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>
        /// Eine Zeile, die der Übersetzer nicht als Anweisung sieht: <c>//</c>,
        /// <c>///</c>, <c>*</c> einer Blockdoku oder <c>/*</c>. Dieselbe Regel wie in
        /// <see cref="EinheitenWacheTests"/>, bewusst konservativ.
        /// </summary>
        private static bool IstKommentar(string zeile)
        {
            string t = zeile.TrimStart();
            return t.StartsWith("//", StringComparison.Ordinal)
                || t.StartsWith("/*", StringComparison.Ordinal)
                || t.StartsWith("*", StringComparison.Ordinal);
        }

        private static bool IstAusgenommen(string datei, string zeile)
        {
            string name = Path.GetFileName(datei);
            return Ausnahmen.Any(a => string.Equals(a.Datei, name, StringComparison.Ordinal)
                                   && zeile.Contains(a.Ausdruck, StringComparison.Ordinal));
        }

        /// <summary>
        /// Der Rechenweg: alle <c>.cs</c> in <c>EPOS.Kern/Allgemein/Simulation/</c> und
        /// dazu <c>EPOS.Kern/Allgemein/BhkwPlan.cs</c> — die Summenfunktionen aus dem
        /// Original-BHKW-Plan, die der Anwenderentscheid ausdrücklich mit einschließt.
        /// </summary>
        private static string[] Rechenwegdateien()
        {
            string wurzel = Arbeitsbaum();
            string ordner = Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Simulation");
            Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);

            string bhkwPlan = Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "BhkwPlan.cs");
            Assert.True(File.Exists(bhkwPlan), "Datei nicht gefunden: " + bhkwPlan);

            var dateien = new List<string>(Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories));
            dateien.Add(bhkwPlan);

            return dateien.Where(OhneBauordner).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }

        private static bool OhneBauordner(string pfad)
        {
            char t = Path.DirectorySeparatorChar;
            return pfad.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0
                && pfad.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0;
        }

        /// <summary>
        /// Die Wurzel des Arbeitsbaums — derselbe Weg wie in
        /// <see cref="EinheitenWacheTests"/>: erst über den eigenen Quelltextort, sonst
        /// vom Ausgabeordner aufwärts.
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
