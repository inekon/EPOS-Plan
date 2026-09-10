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
    /// Der Wächter über die ZWEI PROZESSWEITEN Kultur-Setzer
    /// (Befund Agent #166, 10.09.2026, behoben in #167: <c>CultureInfo.DefaultThreadCurrentCulture</c>
    /// wirkt auf JEDEN danach neu gestarteten Thread des Prozesses — ein Konstruktor, der ihn
    /// setzt und nie zurückstellt, lässt einen völlig unbeteiligten, später laufenden Test
    /// plötzlich in de-DE rechnen. Genau so fiel <c>BetriebskostenBaugroesseTests</c> (#166)).
    ///
    /// <para><b>Was er prüft.</b> Jede <c>.cs</c>-Datei in <c>EPOS.Kern.Tests</c>, die
    /// <c>CultureInfo.DefaultThreadCurrentCulture =</c> oder
    /// <c>CultureInfo.DefaultThreadCurrentUICulture =</c> setzt, muss in DERSELBEN Datei auch
    /// eine Zuweisung auf DASSELBE Ziel innerhalb eines <c>Dispose()</c>- oder
    /// <c>finally</c>-Blocks tragen — das Muster aus <c>KatalogfilterZeitreihenTests</c> bzw.
    /// dem try/finally in <c>WechselrichterKatalogTests</c>.</para>
    ///
    /// <para><b>Warum nur <c>EPOS.Kern.Tests</c> und nicht auch <c>EPOS.UI.Tests</c>.</b>
    /// Kern.Tests und UI.Tests laufen als zwei getrennte <c>dotnet test</c>-Prozesse — ein Leck
    /// im einen erreicht den anderen nie. In UI.Tests pinnt praktisch jede der rund 150
    /// bunit-Testklassen ihre eigene Kultur im Konstruktor neu, ohne je zurückzustellen — eine
    /// durchgängige, in sich geschlossene Konvention dieses Projekts (Fund aus Auftrag #167,
    /// siehe Abschlussbericht), keine Handvoll vereinzelter Lecks wie hier. Sie in einem Zug auf
    /// dieses Muster zu heben ist eine eigene, groß angelegte Aufgabe.</para>
    /// </summary>
    public class KulturwaechterTests
    {
        private static readonly string[] Ziele = { "DefaultThreadCurrentCulture", "DefaultThreadCurrentUICulture" };

        private static Regex Zielregex(string ziel) => new Regex(
            @"(?:System\.Globalization\.)?CultureInfo\." + ziel + @"\s*=(?!=)",
            RegexOptions.Compiled);

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        /// <summary>
        /// Jeder <c>DefaultThreadCurrentCulture</c>/<c>DefaultThreadCurrentUICulture</c>-Setzer
        /// hat in DERSELBEN Datei eine Rückstellung auf dasselbe Ziel in einem
        /// <c>Dispose()</c>- oder <c>finally</c>-Block.
        /// </summary>
        [Fact]
        public void Jeder_DefaultThreadCurrentCulture_Setzer_hat_eine_Rueckstellung()
        {
            var funde = new List<string>();

            foreach (string datei in Testdateien())
            {
                string text = File.ReadAllText(datei);
                var regionen = RestoreRegionen(text);

                foreach (string ziel in Ziele)
                {
                    Regex re = Zielregex(ziel);
                    List<int> treffer = re.Matches(text).Select(m => m.Index).ToList();
                    if (treffer.Count == 0) continue;

                    bool hatRueckstellung = treffer.Any(i => InRegion(i, regionen));
                    if (!hatRueckstellung)
                    {
                        funde.Add(Path.GetFileName(datei) + "  (" + ziel + ")");
                    }
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Dateien setzen CultureInfo.DefaultThread(UI)Culture, ohne es in " +
                "derselben Datei in Dispose()/finally zurueckzustellen (Befund #166/#167 - " +
                "der Setzer wirkt prozessweit auf jeden danach gestarteten Thread):\n" +
                string.Join("\n", funde));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> beide Schreibweisen (mit und ohne vollen
        /// Namensraum) treffen, eine reine Lesezeile nicht.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_beide_Schreibweisen()
        {
            Regex re = Zielregex("DefaultThreadCurrentCulture");
            Assert.Matches(re, "CultureInfo.DefaultThreadCurrentCulture = de;");
            Assert.Matches(re, "System.Globalization.CultureInfo.DefaultThreadCurrentCulture = de;");
            Assert.DoesNotMatch(re, "var x = CultureInfo.DefaultThreadCurrentCulture;");
            Assert.DoesNotMatch(re, "CultureInfo.DefaultThreadCurrentUICulture = de;");
        }

        /// <summary>
        /// <b>Gegenprobe zur Regionserkennung:</b> ein Setzer in <c>Dispose()</c> bzw.
        /// <c>finally</c> gilt als Rückstellung, derselbe Setzer allein im Konstruktor nicht.
        /// </summary>
        [Fact]
        public void Ein_Setzer_ausserhalb_von_Dispose_und_finally_zaehlt_nicht()
        {
            const string nurKonstruktor =
                "class X { X() { CultureInfo.DefaultThreadCurrentCulture = de; } }";
            var regionenOhne = RestoreRegionen(nurKonstruktor);
            int stelleOhne = Zielregex("DefaultThreadCurrentCulture").Match(nurKonstruktor).Index;
            Assert.False(InRegion(stelleOhne, regionenOhne));

            const string mitDispose =
                "class X { X() { CultureInfo.DefaultThreadCurrentCulture = de; } " +
                "void Dispose() { CultureInfo.DefaultThreadCurrentCulture = vorher; } }";
            var regionenMit = RestoreRegionen(mitDispose);
            var stellenMit = Zielregex("DefaultThreadCurrentCulture").Matches(mitDispose)
                .Select(m => m.Index).ToArray();
            Assert.Contains(stellenMit, i => InRegion(i, regionenMit));

            const string mitFinally =
                "void M() { try { CultureInfo.DefaultThreadCurrentCulture = de; } " +
                "finally { CultureInfo.DefaultThreadCurrentCulture = vorher; } }";
            var regionenFinally = RestoreRegionen(mitFinally);
            var stellenFinally = Zielregex("DefaultThreadCurrentCulture").Matches(mitFinally)
                .Select(m => m.Index).ToArray();
            Assert.Contains(stellenFinally, i => InRegion(i, regionenFinally));
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene Dateien,
        /// und mindestens eine bekannte Datei mit sauberer Rückstellung steht darin (sonst
        /// liefe ein leerer Bestand grün durch, ohne je etwas geprüft zu haben).
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_Bestand()
        {
            string[] dateien = Testdateien();
            Assert.True(dateien.Length > 100, "Nur " + dateien.Length + " Testdateien gefunden.");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "KatalogfilterZeitreihenTests.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "CecWechselrichterAuslieferungTests.cs");
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>
        /// Für jeden Fund eines Ziel-Setzers: liegt die Fundstelle innerhalb einer der
        /// gemerkten Regionen (Klammer-weit)?
        /// </summary>
        private static bool InRegion(int index, List<(int Start, int Ende)> regionen)
            => regionen.Any(r => index >= r.Start && index <= r.Ende);

        /// <summary>
        /// Alle Klammer-weiten Bereiche im Text, die zu einem <c>Dispose(...)</c>-Rumpf oder
        /// einem <c>finally { ... }</c>-Block gehören — einfache Tiefenzählung der
        /// geschweiften Klammern, reicht für Quelltext ohne Klammern in Zeichenketten/Kommentaren
        /// an dieser Stelle (dieselbe Vereinfachung wie die übrigen Quelltext-Wächter des Projekts).
        /// </summary>
        private static List<(int Start, int Ende)> RestoreRegionen(string text)
        {
            var regionen = new List<(int, int)>();
            foreach (Regex kopf in new[]
                     {
                         new Regex(@"\bDispose\s*\([^)]*\)\s*\{", RegexOptions.Compiled),
                         new Regex(@"\bfinally\s*\{", RegexOptions.Compiled)
                     })
            {
                foreach (Match m in kopf.Matches(text))
                {
                    int offen = m.Index + m.Length - 1;
                    int zu = SchliessendeKlammer(text, offen);
                    if (zu >= 0) regionen.Add((offen, zu));
                }
            }
            return regionen;
        }

        private static int SchliessendeKlammer(string text, int offenIndex)
        {
            int tiefe = 0;
            for (int i = offenIndex; i < text.Length; i++)
            {
                if (text[i] == '{') tiefe++;
                else if (text[i] == '}')
                {
                    tiefe--;
                    if (tiefe == 0) return i;
                }
            }
            return -1;
        }

        /// <summary>Alle <c>.cs</c>-Dateien in <c>EPOS.Kern.Tests</c>, ohne Bauordner.</summary>
        private static string[] Testdateien()
        {
            string wurzel = Arbeitsbaum();
            string ordner = Path.Combine(wurzel, "EPOS.Kern.Tests");
            Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);

            // Die eigene Datei ist ausgenommen: ihre Gegenproben-Textzeilen und die
            // Klassendoku nennen "CultureInfo.DefaultThread(UI)Culture =" absichtlich als
            // reinen Text (Muster fuer den Leser, NICHT als echter Setzer) - ohne die
            // Ausnahme faende sich der Waechter selbst.
            return Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories)
                .Where(OhneBauordner)
                .Where(d => Path.GetFileName(d) != "KulturwaechterTests.cs")
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool OhneBauordner(string pfad)
        {
            char t = Path.DirectorySeparatorChar;
            return pfad.IndexOf(t + "bin" + t, StringComparison.Ordinal) < 0
                && pfad.IndexOf(t + "obj" + t, StringComparison.Ordinal) < 0;
        }

        /// <summary>
        /// Die Wurzel des Arbeitsbaums — derselbe Weg wie in <see cref="DoubleWacheTests"/>.
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
