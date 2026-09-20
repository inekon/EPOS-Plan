using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die <b>Parameter-Falle der konstanten Null</b> (Befund aus
    /// Schemaschritt 99, <c>BhkwWirkungsgradAnteile.NULLGRENZE</c>).
    ///
    /// <para><b>Die Falle.</b> <see cref="DbParam"/> hat zwei Konstruktoren:
    /// <c>DbParam(string, object)</c> — Name und WERT — und <c>DbParam(string,
    /// DbParamTyp)</c> — Name und TYP, mit dem Wert <c>DBNull</c>. Ein KONSTANTER
    /// AUSDRUCK mit dem Wert null geht in C# implizit in jeden Aufzählungstyp über, und
    /// zwar aus JEDEM numerischen Typ: <c>0</c>, <c>0.0</c>, <c>0d</c>, <c>0f</c>,
    /// <c>0m</c>, <c>0L</c>, <c>0x0</c> und <c>-0</c> sind alle eine gültige
    /// <c>DbParamTyp</c>-Umwandlung. Roslyn wählt deshalb die Überladung mit
    /// <c>DbParamTyp</c> — sie ist die spezifischere —, und der Parameter bindet
    /// <c>DBNull</c> statt der Zahl.</para>
    ///
    /// <para><b>Warum das so teuer ist.</b> Die Abfrage meldet nichts. In SQLite ist
    /// jeder Vergleich mit <c>NULL</c> selbst <c>NULL</c> — weder wahr noch falsch:
    /// <c>Wirkungsgrad &gt; NULL</c> trifft keine Zeile, <c>COALESCE(…, 0) = NULL</c>
    /// ebenso wenig. Der Aufrufer sieht eine leere Menge und hält sie für ein Ergebnis.
    /// Genau so zählte der Datenteil von Schritt 99 zunächst <b>0</b> aufzuteilende
    /// Zeilen, obwohl dieselbe Abfrage außerhalb der Anwendung 78 lieferte — ohne
    /// Ausnahme, ohne Warnung, ohne Spur im Protokoll.</para>
    ///
    /// <para><b>Die Regel, die dieser Wächter hält.</b> Das zweite Argument eines
    /// <c>new DbParam(…)</c> ist nie eine konstante Null — weder als Literal noch als
    /// <c>const</c>-Bezeichner mit dem Wert 0. Wer eine Null binden will, nimmt einen
    /// <c>static readonly</c> (kein konstanter Ausdruck, also gewinnt die Überladung mit
    /// <c>object</c>), eine Variable oder eine ausdrückliche Wandlung
    /// (<c>(object)0</c>).</para>
    ///
    /// <para><b>Gelesen wird der ganze Bestand</b> außer den Tests: <c>EPOS.Kern</c>,
    /// <c>EPOS.UI.Daten</c>, <c>WindowsFormsApplication1</c>, <c>Werkzeuge</c>,
    /// <c>EPOS.Referenzlauf</c> und <c>KiKern</c>, ohne <c>bin</c> und <c>obj</c>.</para>
    /// </summary>
    public class DbParamNullkonstanteWacheTests
    {
        /// <summary>
        /// Die Ordner, über die der Wächter läuft — jeder Quelltext, der
        /// <see cref="DbParam"/> baut, außer den Testprojekten.
        /// </summary>
        private static readonly string[] Ordner =
        {
            "EPOS.Kern", "EPOS.UI.Daten", "WindowsFormsApplication1",
            "Werkzeuge", "EPOS.Referenzlauf", "KiKern"
        };

        /// <summary>
        /// Ein Aufruf <c>new DbParam(erstes, zweites)</c> — <b>auch über mehrere
        /// Zeilen</b>, weil die Zeichenklassen den Zeilenumbruch einschließen.
        ///
        /// <para><b>Bewusst eng:</b> Beide Argumente dürfen weder Klammer noch Komma
        /// tragen. Ein Aufruf wie <c>new DbParam("@id", Convert.ToInt32(v))</c> trifft
        /// deshalb gar nicht — und muss auch nicht: Ein Ausdruck mit Klammern ist weder
        /// ein Literal noch ein <c>const</c>-Bezeichner und damit nie die Falle. Die
        /// dreistellige Überladung (Name, Typ, Größe) fällt aus demselben Grund heraus,
        /// denn sie trägt zwei Kommata.</para>
        /// </summary>
        private static readonly Regex Aufruf = new Regex(
            @"new\s+DbParam\s*\(\s*(?<a>[^(),]*?)\s*,\s*(?<b>[^(),]*?)\s*\)",
            RegexOptions.Compiled);

        /// <summary>
        /// Ein Zahlenliteral mit dem Wert null — in jeder Schreibweise, die C# als
        /// konstanten Ausdruck ansieht: <c>0</c>, <c>0.0</c>, <c>0d</c>, <c>0f</c>,
        /// <c>0m</c>, <c>0L</c>, <c>0x0</c>, <c>-0</c> und ihre Vielfachen an Nullen.
        /// </summary>
        private static readonly Regex Nullliteral = new Regex(
            @"^[+-]?\s*(?:0+(?:\.0+)?[dDfFmMlLuU]{0,2}|0[xX]0+[uUlL]{0,2}|0[bB]0+[uUlL]{0,2})$",
            RegexOptions.Compiled);

        /// <summary>
        /// Eine <c>const</c>-Deklaration eines numerischen Typs mit dem Wert null — die
        /// zweite Gestalt derselben Falle, und die gefährlichere: Am Aufruf steht dann
        /// ein NAME, dem man die Null nicht ansieht.
        /// </summary>
        private static readonly Regex Nullkonstante = new Regex(
            @"\bconst\s+(?:int|long|double|decimal|float|short|byte|uint|ulong)\s+"
            + @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*"
            + @"(?:[+-]?\s*(?:0+(?:\.0+)?[dDfFmMlLuU]{0,2}|0[xX]0+[uUlL]{0,2}))\s*;",
            RegexOptions.Compiled);

        /// <summary>Ein einfacher Bezeichner, ggf. qualifiziert (<c>Klasse.NAME</c>).</summary>
        private static readonly Regex Bezeichner = new Regex(
            @"^(?:[A-Za-z_][A-Za-z0-9_]*\.)*(?<letzt>[A-Za-z_][A-Za-z0-9_]*)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Belegte Ausnahmen. <b>Leer</b> — der Bestand trägt keine einzige Stelle.
        /// Wer hier etwas einträgt, schreibt den Grund dazu.
        /// </summary>
        private static readonly (string Datei, string Ausdruck, string Grund)[] Ausnahmen =
            Array.Empty<(string, string, string)>();

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// Kein <c>new DbParam(…)</c> des Bestands bindet eine konstante Null. Meldet
        /// Datei, Zeile und den Aufruf jeder Fundstelle.
        /// </summary>
        [Fact]
        public void Kein_DbParam_bindet_eine_konstante_Null()
        {
            string[] dateien = Quelldateien();
            HashSet<string> nullkonstanten = Nullkonstanten(dateien);

            var funde = new List<string>();

            foreach (string datei in dateien)
            {
                foreach (var fund in Fundstellen(File.ReadAllText(datei), nullkonstanten))
                {
                    if (IstAusgenommen(datei, fund.Ausdruck)) continue;
                    funde.Add(Path.GetFileName(datei) + ":" + fund.Zeile + "  " + fund.Ausdruck);
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Aufrufe binden eine KONSTANTE NULL und damit DBNull statt einer Zahl: " +
                "Roslyn waehlt die Ueberladung DbParam(string, DbParamTyp), weil ein konstanter " +
                "Ausdruck mit dem Wert null jedes numerischen Typs implizit in jeden " +
                "Aufzaehlungstyp uebergeht. Die SQL-Bedingung ist dann fuer jede Zeile NULL und " +
                "meldet nichts - ohne sich zu beklagen (Befund Schemaschritt 99, " +
                "BhkwWirkungsgradAnteile.NULLGRENZE). Abhilfe: static readonly, eine Variable " +
                "oder (object)0.\n" +
                string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Der Wächter erkennt eine synthetische Verletzung
        /// — jedes Nullliteral, den mehrzeiligen Aufruf und den <c>const</c>-Bezeichner
        /// — und lässt alles durch, was keine konstante Null ist. Ohne diesen Fall wäre
        /// er stumm, sobald jemand die Null anders hinschreibt.
        /// </summary>
        [Fact]
        public void Der_Waechter_erkennt_eine_synthetische_Verletzung()
        {
            var konstanten = new HashSet<string>(StringComparer.Ordinal) { "NULLGRENZE", "SOC_MIN" };

            const string schlecht = @"
class Probe
{
    void Tun()
    {
        var a = new DbParam(""@a"", 0);
        var b = new DbParam(""@b"", 0.0);
        var c = new DbParam(""@c"", 0d);
        var d = new DbParam(""@d"", 0f);
        var e = new DbParam(""@e"", 0m);
        var f = new DbParam(""@f"", 0L);
        var g = new DbParam(""@g"", 0x0);
        var h = new DbParam(""@h"", -0);
        var i = new DbParam(""@i"", NULLGRENZE);
        var j = new DbParam(
            ""@j"",
            SpeicherParameterPruefung.SOC_MIN);
    }
}";

            var funde = Fundstellen(schlecht, konstanten).ToList();
            Assert.Equal(10, funde.Count);
            Assert.Contains(funde, f => f.Ausdruck.Contains("@g", StringComparison.Ordinal));
            Assert.Contains(funde, f => f.Ausdruck.Contains("SOC_MIN", StringComparison.Ordinal));

            // Die mehrzeilige Fundstelle wird mit der Zeile ihres "new DbParam" gemeldet.
            var mehrzeilig = funde.Single(f => f.Ausdruck.Contains("SOC_MIN", StringComparison.Ordinal));
            Assert.Equal(15, mehrzeilig.Zeile);

            const string gut = @"
class Probe
{
    const double NULLGRENZE_RICHTIG = 0.0;
    static readonly double Grenze = 0.0;

    void Tun(int id)
    {
        var a = new DbParam(""@a"", Grenze);
        var b = new DbParam(""@b"", id);
        var c = new DbParam(""@c"", (object)0);
        var d = new DbParam(""@d"", 0.5);
        var e = new DbParam(""@e"", 1);
        var f = new DbParam(""@f"", DbParamTyp.Integer);
        var g = new DbParam(""@g"", Convert.ToInt32(id));
        // var h = new DbParam(""@h"", 0);
    }
}";

            Assert.Empty(Fundstellen(gut, konstanten));
        }

        /// <summary>
        /// <b>Gegenprobe zur Sammlung der <c>const</c>-Nullen:</b> Sie findet die
        /// bekannten Stellen des Bestands wieder. Fände sie nichts, prüfte der Wächter
        /// oben nur noch die Literale — und die gefährlichere Hälfte der Falle, der
        /// NAME, an dem man die Null nicht sieht, bliebe ungeprüft.
        /// </summary>
        [Fact]
        public void Die_Sammlung_findet_die_bekannten_const_Nullen()
        {
            HashSet<string> namen = Nullkonstanten(Quelldateien());

            Assert.True(namen.Count > 15, "Nur " + namen.Count + " const-Nullen gefunden.");

            // Je eine aus den drei Gestalten: double, int und ein Bezeichner, der
            // ausserhalb seiner Klasse gelesen wird.
            Assert.Contains("SOC_MIN", namen);                          // SpeicherParameterPruefung
            Assert.Contains("ALLE", namen);                             // Vergleichssicht
            Assert.Contains("ANTEIL_VON", namen);                       // BhkwWirkungsgrad
            Assert.Contains("SYSTEMVERLUSTE_VORGABE", namen);           // SimulationPV

            // NULLGRENZE steht seit dem Befund als static readonly da - genau deshalb
            // gehoert sie NICHT in diese Sammlung.
            Assert.DoesNotContain("NULLGRENZE", namen);
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene
        /// Dateien — und über genug davon. Dazu: jede Ausnahme steht wirklich dort.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert()
        {
            string[] dateien = Quelldateien();

            Assert.True(dateien.Length > 500, "Nur " + dateien.Length + " Quelldateien gefunden.");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "DbParam.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "BhkwWirkungsgradAnteile.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "BHKWStammCtrl.cs");

            // UND ER LIEST SIE AUCH: Ein Leser, der gar nichts mehr trifft - ein
            // verunglueckter Ausdruck, ein umbenannter Typ - liefe still gruen
            // durch. Der Bestand baut seine Parameter an rund 2 500 Stellen.
            int aufrufe = dateien.Sum(d => Aufruf.Matches(File.ReadAllText(d)).Count);
            Assert.True(aufrufe > 1500, "Der Leser findet nur " + aufrufe + " DbParam-Aufrufe.");

            foreach (var a in Ausnahmen)
            {
                bool da = dateien.Any(d => string.Equals(Path.GetFileName(d), a.Datei, StringComparison.Ordinal)
                                        && File.ReadAllText(d).Contains(a.Ausdruck, StringComparison.Ordinal));
                Assert.True(da, "Die Ausnahme '" + a.Ausdruck + "' steht nicht mehr in " + a.Datei +
                                " - sie gehoert aus der Liste entfernt.");
            }
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>Eine Fundstelle: Zeilennummer und der Aufruf im Wortlaut.</summary>
        private sealed record Fund(int Zeile, string Ausdruck);

        /// <summary>
        /// Jede Fundstelle eines Quelltextes. Kommentarzeilen fallen VOR dem Suchen
        /// heraus — der Übersetzer sieht sie nicht, und die Klassenköpfe nennen die
        /// Falle absichtlich im Wortlaut.
        /// </summary>
        private static IEnumerable<Fund> Fundstellen(string quelltext, HashSet<string> nullkonstanten)
        {
            string[] zeilen = quelltext.Replace("\r\n", "\n").Split('\n');

            // Der Text OHNE Kommentarzeilen, dazu je Zeichenposition die urspruengliche
            // Zeilennummer - so bleibt eine mehrzeilige Fundstelle auffindbar UND
            // meldbar.
            var text = new StringBuilder();
            var anfang = new List<int>();
            var nummer = new List<int>();

            for (int i = 0; i < zeilen.Length; i++)
            {
                if (IstKommentar(zeilen[i])) continue;
                anfang.Add(text.Length);
                nummer.Add(i + 1);
                text.Append(zeilen[i]).Append('\n');
            }

            string gelesen = text.ToString();

            foreach (Match m in Aufruf.Matches(gelesen))
            {
                string zweites = m.Groups["b"].Value.Trim();
                if (!IstKonstanteNull(zweites, nullkonstanten)) continue;

                yield return new Fund(ZeileZu(m.Index, anfang, nummer),
                                      Regex.Replace(m.Value, @"\s+", " ").Trim());
            }
        }

        /// <summary>
        /// Ist dieser Ausdruck eine konstante Null — als Literal oder als Name einer
        /// <c>const</c>-Deklaration mit dem Wert 0?
        /// </summary>
        private static bool IstKonstanteNull(string ausdruck, HashSet<string> nullkonstanten)
        {
            if (ausdruck.Length == 0) return false;
            if (Nullliteral.IsMatch(ausdruck)) return true;

            Match b = Bezeichner.Match(ausdruck);
            return b.Success && nullkonstanten.Contains(b.Groups["letzt"].Value);
        }

        /// <summary>Alle <c>const</c>-Bezeichner des Bestands, deren Wert 0 ist.</summary>
        private static HashSet<string> Nullkonstanten(IEnumerable<string> dateien)
        {
            var namen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string datei in dateien)
            {
                foreach (Match m in Nullkonstante.Matches(File.ReadAllText(datei)))
                    namen.Add(m.Groups["name"].Value);
            }

            return namen;
        }

        /// <summary>Die Zeilennummer zu einer Zeichenposition im gelesenen Text.</summary>
        private static int ZeileZu(int index, List<int> anfang, List<int> nummer)
        {
            int i = anfang.BinarySearch(index);
            if (i < 0) i = ~i - 1;
            if (i < 0) i = 0;
            return nummer[i];
        }

        /// <summary>
        /// Eine Zeile, die der Übersetzer nicht als Anweisung sieht — dieselbe Regel wie
        /// in <see cref="DoubleWacheTests"/> und <c>EinheitenWacheTests</c>, bewusst
        /// konservativ.
        /// </summary>
        private static bool IstKommentar(string zeile)
        {
            string t = zeile.TrimStart();
            return t.StartsWith("//", StringComparison.Ordinal)
                || t.StartsWith("/*", StringComparison.Ordinal)
                || t.StartsWith("*", StringComparison.Ordinal);
        }

        private static bool IstAusgenommen(string datei, string ausdruck)
        {
            string name = Path.GetFileName(datei);
            return Ausnahmen.Any(a => string.Equals(a.Datei, name, StringComparison.Ordinal)
                                   && ausdruck.Contains(a.Ausdruck, StringComparison.Ordinal));
        }

        /// <summary>Jede <c>.cs</c>-Datei der sechs Ordner, ohne <c>bin</c> und <c>obj</c>.</summary>
        private static string[] Quelldateien()
        {
            string wurzel = Arbeitsbaum();
            var dateien = new List<string>();

            foreach (string o in Ordner)
            {
                string pfad = Path.Combine(wurzel, o);
                Assert.True(Directory.Exists(pfad), "Ordner nicht gefunden: " + pfad);
                dateien.AddRange(Directory.GetFiles(pfad, "*.cs", SearchOption.AllDirectories));
            }

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
        /// <see cref="DoubleWacheTests"/>: erst über den eigenen Quelltextort, sonst vom
        /// Ausgabeordner aufwärts.
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
