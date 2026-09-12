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
    /// Der Wächter über die PROZESSWEITEN Kultur-Setzer
    /// (Befund Agent #166, 10.09.2026, behoben in #167: <c>CultureInfo.DefaultThreadCurrentCulture</c>
    /// wirkt auf JEDEN danach neu gestarteten Thread des Prozesses — ein Konstruktor, der ihn
    /// setzt und nie zurückstellt, lässt einen völlig unbeteiligten, später laufenden Test
    /// plötzlich in de-DE rechnen. Genau so fiel <c>BetriebskostenBaugroesseTests</c> (#166)).
    ///
    /// <para><b>Was er in <c>EPOS.Kern.Tests</c> prüft.</b> Jede <c>.cs</c>-Datei, die
    /// <c>CultureInfo.DefaultThreadCurrentCulture =</c> oder
    /// <c>CultureInfo.DefaultThreadCurrentUICulture =</c> setzt, muss in DERSELBEN Datei auch
    /// eine Zuweisung auf DASSELBE Ziel innerhalb eines <c>Dispose()</c>- oder
    /// <c>finally</c>-Blocks tragen — das Muster aus <c>KatalogfilterZeitreihenTests</c> bzw.
    /// dem try/finally in <c>WechselrichterKatalogTests</c>.</para>
    ///
    /// <para><b>Seit Auftrag #168 (11.09.2026) prüft derselbe Wächter auch
    /// <c>EPOS.UI.Tests</c>.</b> Befund #167 hatte das bewusst ausgenommen: 63 Dateien
    /// pinnten dort <c>DefaultThreadCurrentCulture</c> prozessweit, 107
    /// <c>CurrentCulture</c>/<c>Thread.CurrentThread.CurrentCulture</c> threadgebunden — fast
    /// jede der rund 150 bunit-Testklassen mit einer eigenen privaten Pinn-Methode, kaum eine
    /// mit Rückstellung. Der Anwenderentscheid vom 11.09.2026 („Empfehlung umsetzen") hat das
    /// auf EINE gemeinsame Vorrichtung gehoben: <c>Kulturvorrichtung</c> (merkt vier Werte,
    /// stellt sie in <c>Dispose</c> zurück) und <c>EposBunitContext</c> (legt sie im
    /// Konstruktor jeder bunit-Klasse an). Seither gilt für <c>EPOS.UI.Tests</c> dieselbe
    /// Regel wie für <c>EPOS.Kern.Tests</c>, nur über SECHS statt zwei Ziele (dazu
    /// <c>CultureInfo.CurrentCulture</c>/<c>…CurrentUICulture</c> und
    /// <c>Thread.CurrentThread.CurrentCulture</c>/<c>…CurrentUICulture</c>) und mit einer
    /// zweiten Tür: eine Datei, die die Vorrichtung NUTZT (von <c>EposBunitContext</c> erbt
    /// oder selbst ein <c>Kulturvorrichtung</c>-Feld hält), braucht keine EIGENE
    /// Rückstellung mehr — die Vorrichtung erledigt das an ihrer statt. Erlaubte Ausnahme von
    /// der ganzen Prüfung: die Vorrichtung selbst (<c>Kulturvorrichtung.cs</c>), deren eigene
    /// Zuweisungen und Rückstellungen der Gegenstand der Prüfung wären, nicht ihr Ergebnis.</para>
    /// </summary>
    public class KulturwaechterTests
    {
        private static readonly string[] Ziele = { "DefaultThreadCurrentCulture", "DefaultThreadCurrentUICulture" };

        /// <summary>
        /// Die SECHS Ziele der Kulturvorrichtung in <c>EPOS.UI.Tests</c> — als volle
        /// Ausdrücke, weil sie (anders als in <c>EPOS.Kern.Tests</c>) zwei verschiedene
        /// Vorsilben tragen: <c>CultureInfo.</c> und <c>Thread.CurrentThread.</c>.
        /// </summary>
        private static readonly string[] UiZiele =
        {
            "CultureInfo.DefaultThreadCurrentCulture",
            "CultureInfo.DefaultThreadCurrentUICulture",
            "Thread.CurrentThread.CurrentCulture",
            "Thread.CurrentThread.CurrentUICulture",
            "CultureInfo.CurrentCulture",
            "CultureInfo.CurrentUICulture",
        };

        private static Regex Zielregex(string ziel) => new Regex(
            @"(?:System\.Globalization\.)?CultureInfo\." + ziel + @"\s*=(?!=)",
            RegexOptions.Compiled);

        /// <summary>Derselbe Leser wie <see cref="Zielregex"/>, aber für einen vollen
        /// Ausdruck (<see cref="UiZiele"/>) statt nur einen <c>CultureInfo.</c>-Suffix.</summary>
        private static Regex ZielregexVoll(string zielAusdruck) => new Regex(
            Regex.Escape(zielAusdruck) + @"\s*=(?!=)",
            RegexOptions.Compiled);

        // =====================================================================
        //  Der Wächter — EPOS.Kern.Tests
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
        //  Der Wächter — EPOS.UI.Tests (seit Auftrag #168)
        // =====================================================================

        /// <summary>
        /// Jede Datei in <c>EPOS.UI.Tests</c>, die eines der sechs <see cref="UiZiele"/>
        /// setzt, hat entweder eine eigene Rückstellung in <c>Dispose()</c>/<c>finally</c>
        /// (wie der Kern-Wächter es verlangt) ODER nutzt die <c>Kulturvorrichtung</c>
        /// (Erbschaft von <c>EposBunitContext</c> oder ein eigenes Vorrichtungsfeld) — dann
        /// erledigt DIE die Rückstellung. Ausgenommen ist die Vorrichtung selbst.
        /// </summary>
        [Fact]
        public void Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung()
        {
            var funde = new List<string>();

            foreach (string datei in TestdateienUi())
            {
                string text = File.ReadAllText(datei);
                List<string> fehlend = FehlendeRueckstellungen(text, UiZiele, ZielregexVoll, NutztVorrichtung(text));
                foreach (string ziel in fehlend)
                {
                    funde.Add(Path.GetFileName(datei) + "  (" + ziel + ")");
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Dateien in EPOS.UI.Tests setzen eine Kultur (CultureInfo.DefaultThread(UI)Culture, " +
                "Thread.CurrentThread.Current(UI)Culture oder CultureInfo.Current(UI)Culture), ohne sie in " +
                "derselben Datei zurueckzustellen und ohne die Kulturvorrichtung zu nutzen (Auftrag #168):\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// Eine Datei „nutzt die Vorrichtung", wenn ihre Klasse von
        /// <c>EposBunitContext</c> erbt oder sie selbst ein <c>Kulturvorrichtung</c>-Feld
        /// hält (die Nicht-bunit-Klassen, z. B. <c>FenstermassTests</c>,
        /// <c>LizenzTexteTests</c>). In beiden Fällen liegt die eigentliche Rückstellung
        /// nicht mehr in der Datei selbst, sondern in der Vorrichtung.
        /// </summary>
        private static bool NutztVorrichtung(string text)
            => text.Contains("EposBunitContext") || text.Contains("Kulturvorrichtung");

        /// <summary>
        /// Kern der UI-Prüfung, gegenprobenfähig ohne Datei: Ist <paramref name="vorrichtungBefreit"/>
        /// gesetzt, braucht die Datei keine eigene Rückstellung. Sonst gilt dieselbe Regel wie im
        /// Kern-Wächter — je Ziel eine Zuweisung auf dasselbe Ziel innerhalb einer
        /// <c>Dispose()</c>/<c>finally</c>-Region derselben Datei.
        /// </summary>
        private static List<string> FehlendeRueckstellungen(
            string text, IEnumerable<string> ziele, Func<string, Regex> zielLeser, bool vorrichtungBefreit)
        {
            var fehlend = new List<string>();
            if (vorrichtungBefreit) return fehlend;

            var regionen = RestoreRegionen(text);
            foreach (string ziel in ziele)
            {
                Regex re = zielLeser(ziel);
                List<int> treffer = re.Matches(text).Select(m => m.Index).ToList();
                if (treffer.Count == 0) continue;

                bool hatRueckstellung = treffer.Any(i => InRegion(i, regionen));
                if (!hatRueckstellung) fehlend.Add(ziel);
            }
            return fehlend;
        }

        // =====================================================================
        //  Gegenproben — EPOS.Kern.Tests
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
        //  Gegenproben — EPOS.UI.Tests (seit Auftrag #168)
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum UI-Leser:</b> alle sechs vollen Ausdrücke treffen, eine reine
        /// Lesezeile nicht.
        /// </summary>
        [Fact]
        public void Der_UI_Leser_erkennt_alle_sechs_Ziele()
        {
            foreach (string ziel in UiZiele)
            {
                Regex re = ZielregexVoll(ziel);
                Assert.Matches(re, ziel + " = de;");
                Assert.DoesNotMatch(re, "var x = " + ziel + ";");
            }
        }

        /// <summary>
        /// <b>Gegenprobe zur zweiten Tür:</b> Eine Datei, die die Vorrichtung nutzt (erbt von
        /// <c>EposBunitContext</c> oder hält ein <c>Kulturvorrichtung</c>-Feld), braucht KEINE
        /// eigene Rückstellung — auch wenn sie mitten im Fall eine andere Kultur setzt (die
        /// vier Sprachwechsel-Klassen aus Auftrag #168).
        /// </summary>
        [Fact]
        public void Eine_Datei_die_die_Vorrichtung_nutzt_braucht_keine_eigene_Rueckstellung()
        {
            const string mitErbschaft =
                "public class X : EposBunitContext { void M() { CultureInfo.CurrentUICulture = en; } }";
            Assert.Empty(FehlendeRueckstellungen(mitErbschaft, UiZiele, ZielregexVoll, NutztVorrichtung(mitErbschaft)));

            const string mitFeld =
                "public class X : IDisposable { private readonly Kulturvorrichtung _k = new(); " +
                "void M() { CultureInfo.CurrentUICulture = en; } }";
            Assert.Empty(FehlendeRueckstellungen(mitFeld, UiZiele, ZielregexVoll, NutztVorrichtung(mitFeld)));
        }

        /// <summary>
        /// <b>Gegenprobe zum Leck:</b> Ohne Vorrichtung UND ohne eigene Rückstellung fällt eine
        /// Datei auf — genau der Zustand, den Befund #167 in 105 von 113 Dateien fand.
        /// </summary>
        [Fact]
        public void Ohne_Vorrichtung_und_ohne_Rueckstellung_faellt_eine_Datei_auf()
        {
            const string leck =
                "public class X : BunitContext { X() { CultureInfo.CurrentUICulture = de; } }";
            List<string> fehlend = FehlendeRueckstellungen(leck, UiZiele, ZielregexVoll, NutztVorrichtung(leck));
            Assert.Contains("CultureInfo.CurrentUICulture", fehlend);
        }

        /// <summary>
        /// <b>Gegenprobe zum UI-Bestand:</b> Der Wächter sieht wirklich <c>EPOS.UI.Tests</c>,
        /// die Vorrichtung selbst steht NICHT im geprüften Bestand (sie wäre sonst ihr eigener
        /// Gegenstand), und eine bekannte umgestellte Klasse steht darin.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_auch_den_UI_Bestand()
        {
            string[] dateien = TestdateienUi();
            Assert.True(dateien.Length > 100, "Nur " + dateien.Length + " EPOS.UI.Tests-Dateien gefunden.");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "MenuebandTests.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "DiagrammTests.cs");
            Assert.DoesNotContain(dateien, d => Path.GetFileName(d) == "Kulturvorrichtung.cs");
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

        /// <summary>
        /// Alle <c>.cs</c>-Dateien in <c>EPOS.UI.Tests</c>, ohne Bauordner und ohne die
        /// Vorrichtung selbst (<c>Kulturvorrichtung.cs</c> — Auftrag #168, erlaubte Ausnahme:
        /// ihre eigenen Zuweisungen und Rückstellungen SIND die Vorrichtung, nicht deren
        /// Umgehung).
        /// </summary>
        private static string[] TestdateienUi()
        {
            string wurzel = Arbeitsbaum();
            string ordner = Path.Combine(wurzel, "EPOS.UI.Tests");
            Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);

            return Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories)
                .Where(OhneBauordner)
                .Where(d => Path.GetFileName(d) != "Kulturvorrichtung.cs")
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
