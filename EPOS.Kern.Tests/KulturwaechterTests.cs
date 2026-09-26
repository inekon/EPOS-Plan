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
    /// zweiten Tür: eine Zuweisung, deren Klasse die Vorrichtung NUTZT (von
    /// <c>EposBunitContext</c> erbt oder selbst ein <c>Kulturvorrichtung</c>-Feld hält),
    /// braucht keine EIGENE Rückstellung mehr — die Vorrichtung erledigt das an ihrer statt.
    /// Erlaubte Ausnahme von der ganzen Prüfung: die Vorrichtung selbst
    /// (<c>Kulturvorrichtung.cs</c>), deren eigene Zuweisungen und Rückstellungen der
    /// Gegenstand der Prüfung wären, nicht ihr Ergebnis.</para>
    ///
    /// <para><b>Seit Auftrag #531 (26.09.2026, CI-Wächter; Befunde #515 und #525):</b>
    /// (1) <b>Wächter A</b> — jede <c>Kulturvorrichtung</c> wird entsorgt. #515: sechs
    /// Klassen hielten eine Vorrichtung als Feld, ohne <c>IDisposable</c> zu sein; xUnit rief
    /// nie <c>Dispose()</c>, de-DE blieb prozessweit bis zum Laufende stehen und verdeckte,
    /// dass <c>GebaeudeHochrechnungTests</c> gar nicht pinnte — rot erst, als neue Klassen
    /// die Reihenfolge verschoben. Die Regel gilt je KLASSE (verschachtelte einzeln): in
    /// <c>EmissionsspalteTests</c> trug damals nur die verschachtelte
    /// <c>Sprachumschaltung</c> ein <c>IDisposable</c>, eine Prüfung je Datei hätte das Leck
    /// übersehen. (2) <b>Die zweite Tür des UI-Wächters gilt je Klasse</b> statt je Datei:
    /// Vorher genügte das Wort <c>Kulturvorrichtung</c> irgendwo in der Datei (auch in einem
    /// Kommentar). Der Leser dafür (<see cref="Quelle"/>) maskiert Kommentare,
    /// Zeichenketten und Präprozessorzeilen, bevor er Klassen, Felder und Klammern zählt.
    /// (3) <b>Die Standardkultur en-US:</b> <c>EPOS.Kern.Tests/StandardkulturEnUs.cs</c> und
    /// <c>EPOS.UI.Tests/StandardkulturEnUs.cs</c> setzen beim Laden der Testassembly en-US —
    /// die Kultur des Windows-Läufers. Ein Fall, der deutsche Ressourcentexte erwartet und
    /// nicht pinnt, fällt damit auf jedem Rechner, schon im lokalen Gate, statt erst auf dem
    /// Windows-Läufer nach dem Push (ubuntu läuft invariant und liefert die neutralen,
    /// deutschen Ressourcen). Genau diese zwei Dateien (nach Pfad) sind von der
    /// Rückstell-Pflicht ausgenommen; jeder andere Setzer bleibt verboten, und ein eigener
    /// Fall verlangt, dass beide bestehen und en-US setzen.</para>
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

        /// <summary>Die beiden Testprojekte, in denen Kulturvorrichtungen stehen.</summary>
        private static readonly string[] Testprojekte = { "EPOS.Kern.Tests", "EPOS.UI.Tests" };

        /// <summary>
        /// Die zwei Dateien der Standardkultur en-US (repo-relativ) — die EINZIGEN Setzer, die
        /// nicht zurückstellen dürfen, weil sie den Ausgangszustand jedes Laufs herstellen.
        /// </summary>
        private static readonly string[] StandardkulturDateien =
        {
            "EPOS.Kern.Tests/StandardkulturEnUs.cs",
            "EPOS.UI.Tests/StandardkulturEnUs.cs",
        };

        /// <summary>Die vier Werte, die eine Standardkultur-Datei setzen muss.</summary>
        private static readonly string[] StandardkulturZiele =
        {
            "CultureInfo.DefaultThreadCurrentCulture",
            "CultureInfo.DefaultThreadCurrentUICulture",
            "Thread.CurrentThread.CurrentCulture",
            "Thread.CurrentThread.CurrentUICulture",
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
        //  Der Wächter — EPOS.UI.Tests (seit Auftrag #168, Tür je Klasse seit #531)
        // =====================================================================

        /// <summary>
        /// Jede Zuweisung in <c>EPOS.UI.Tests</c> an eines der sechs <see cref="UiZiele"/>
        /// hat entweder eine Rückstellung in <c>Dispose()</c>/<c>finally</c> derselben Datei
        /// (wie der Kern-Wächter es verlangt) ODER steht in einer Klasse, die die
        /// <c>Kulturvorrichtung</c> nutzt (Erbschaft von <c>EposBunitContext</c>, auch über eine
        /// Zwischenbasis, oder ein eigenes Vorrichtungsfeld; eine umschließende Klasse zählt
        /// mit) — dann erledigt DIE die Rückstellung. Ausgenommen ist die Vorrichtung selbst.
        /// </summary>
        [Fact]
        public void Jede_Kulturzuweisung_in_EPOS_UI_Tests_hat_eine_Rueckstellung_oder_nutzt_die_Vorrichtung()
        {
            string wurzel = Arbeitsbaum();
            List<Quelle> quellen = TestdateienUi().Select(d => Quelle.Lies(d, wurzel)).ToList();
            var bestand = new Klassenbestand(quellen);
            var funde = new List<string>();

            foreach (Quelle q in quellen)
            {
                foreach (string ziel in FehlendeRueckstellungenUi(q, bestand))
                {
                    funde.Add(q.Relativ + "  (" + ziel + ")");
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Dateien in EPOS.UI.Tests setzen eine Kultur (CultureInfo.DefaultThread(UI)Culture, " +
                "Thread.CurrentThread.Current(UI)Culture oder CultureInfo.Current(UI)Culture), ohne sie in " +
                "derselben Datei zurueckzustellen und ohne dass ihre Klasse die Kulturvorrichtung nutzt " +
                "(Auftrag #168, Tuer je Klasse seit #531):\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// Kern der UI-Prüfung, gegenprobenfähig ohne Datei: Je Ziel sind die Zuweisungen
        /// befreit, deren Klasse (oder eine umschließende) die Vorrichtung nutzt. Bleibt eine
        /// unbefreite Zuweisung, gilt die Regel des Kern-Wächters — eine Zuweisung auf dasselbe
        /// Ziel innerhalb einer <c>Dispose()</c>/<c>finally</c>-Region derselben Datei. Nur
        /// Zuweisungen im Code zählen, nicht solche in Kommentaren oder Zeichenketten.
        /// </summary>
        private static List<string> FehlendeRueckstellungenUi(Quelle q, Klassenbestand bestand)
        {
            var fehlend = new List<string>();
            var regionen = RestoreRegionen(q.Maske);

            foreach (string ziel in UiZiele)
            {
                List<int> treffer = ZielregexVoll(ziel).Matches(q.Text).Cast<Match>()
                    .Select(m => m.Index).Where(q.IstCode).ToList();
                if (treffer.Count == 0) continue;
                if (treffer.All(i => bestand.NutztVorrichtung(q.InnersteKlasse(i)))) continue;
                if (treffer.Any(i => InRegion(i, regionen))) continue;
                fehlend.Add(ziel);
            }
            return fehlend;
        }

        // =====================================================================
        //  Wächter A — jede Kulturvorrichtung wird entsorgt (seit Auftrag #531)
        // =====================================================================

        /// <summary>
        /// <b>Wächter A.</b> Jede Klasse in <c>EPOS.Kern.Tests</c> und <c>EPOS.UI.Tests</c>
        /// (verschachtelte einzeln), die ein Feld vom Typ <c>Kulturvorrichtung</c> hält, führt
        /// <c>IDisposable</c> oder <c>IAsyncDisposable</c> in ihrer EIGENEN Basisliste oder erbt
        /// von <c>BunitContext</c>/<c>EposBunitContext</c>, und ihr Rumpf ruft
        /// <c>feld.Dispose()</c> auf. Jedes andere <c>new Kulturvorrichtung(…)</c> und jede
        /// lokale Vorrichtung steht in einem <c>using</c>. Eine Vorrichtung, die nie entsorgt
        /// wird, lässt de-DE prozessweit bis zum Laufende stehen (Befund #515).
        /// </summary>
        [Fact]
        public void Jede_Kulturvorrichtung_wird_entsorgt()
        {
            string wurzel = Arbeitsbaum();
            var funde = new List<string>();

            foreach (string projekt in Testprojekte)
            {
                foreach (string datei in Quelldateien(wurzel, projekt))
                {
                    funde.AddRange(VorrichtungsFunde(Quelle.Lies(datei, wurzel)));
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Kulturvorrichtungen werden nicht sicher entsorgt (Waechter A, Auftrag #531; " +
                "Befund #515: eine nie entsorgte Vorrichtung laesst de-DE prozessweit bis zum Laufende " +
                "stehen und verdeckt fehlende Pinnung anderer Klassen). Regel: ein Vorrichtungsfeld " +
                "verlangt IDisposable/IAsyncDisposable in der eigenen Basisliste (oder BunitContext/" +
                "EposBunitContext) und feld.Dispose() im Rumpf; jede andere Vorrichtung steht in einem using:\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// Die Funde von Wächter A in einer Quelle — gegenprobenfähig ohne Datei. Je Fund:
        /// <c>Datei:Zeile Klasse (Feld …): Grund</c>.
        /// </summary>
        private static List<string> VorrichtungsFunde(Quelle q)
        {
            var funde = new List<string>();

            foreach (Klasse k in q.Klassen)
            {
                string rumpf = q.Maske.Substring(k.RumpfStart, k.RumpfEnde - k.RumpfStart + 1);
                foreach (Feld f in k.Vorrichtungsfelder)
                {
                    string ort = q.Ort(f.Index) + " " + k.Pfad + " (Feld " + f.Name + ")";
                    if (!EntsorgbareBasis.IsMatch(k.Basisliste))
                    {
                        funde.Add(ort + ": ohne IDisposable/IAsyncDisposable in der eigenen Basisliste");
                    }
                    if (!new Regex(@"\b" + Regex.Escape(f.Name) + @"\s*\??\s*\.\s*Dispose\s*\(").IsMatch(rumpf))
                    {
                        funde.Add(ort + ": ruft " + f.Name + ".Dispose() nicht auf");
                    }
                }
            }

            foreach (Match m in NeueVorrichtung.Matches(q.Maske))
            {
                Klasse k = q.InnersteKlasse(m.Index);
                if (k == null) continue;
                string vorlauf = Anweisungsvorlauf(q.Maske, m.Index);
                bool gebunden = q.RelativeTiefe(k, m.Index) == 0
                    ? FeldvorlaufMitTyp.IsMatch(vorlauf)
                    : UsingVorlauf.IsMatch(vorlauf) || IstFeldzuweisung(vorlauf, k);
                if (!gebunden)
                {
                    funde.Add(q.Ort(m.Index) + " " + k.Pfad + ": new Kulturvorrichtung(...) ohne using");
                }
            }

            foreach (Match m in Vorrichtungsdeklaration.Matches(q.Maske))
            {
                Klasse k = q.InnersteKlasse(m.Index);
                if (k == null || q.RelativeTiefe(k, m.Index) == 0) continue;   // Felder: oben
                if (MitNeuerVorrichtung.IsMatch(q.Maske, m.Index + m.Length)) continue;   // meldet schon die Schleife davor
                if (Anweisungsvorlauf(q.Maske, m.Index).Trim().Length == 0)
                {
                    funde.Add(q.Ort(m.Index) + " " + k.Pfad + ": lokale Kulturvorrichtung " +
                              m.Groups[1].Value + " ohne using");
                }
            }

            return funde;
        }

        /// <summary>Basislisten, die eine Entsorgung durch xUnit bzw. bunit zusagen.</summary>
        private static readonly Regex EntsorgbareBasis = new Regex(
            @"\b(?:I(?:Async)?Disposable|BunitContext|EposBunitContext)\b", RegexOptions.Compiled);

        /// <summary><c>new Kulturvorrichtung(</c> im maskierten Text.</summary>
        private static readonly Regex NeueVorrichtung = new Regex(
            @"\bnew\s+Kulturvorrichtung\s*\(", RegexOptions.Compiled);

        /// <summary>Eine Deklaration <c>Kulturvorrichtung name =|;|,</c> (Feld oder lokal; nicht
        /// Methode, Eigenschaft, Parameter mit Klammer dahinter oder Ausdruckskörper).</summary>
        private static readonly Regex Vorrichtungsdeklaration = new Regex(
            @"\bKulturvorrichtung\s*\??\s+([A-Za-z_]\w*)\s*(?=(?:=(?!>)|[;,]))", RegexOptions.Compiled);

        /// <summary>Hinter einer Deklaration: <c>= new Kulturvorrichtung</c>.</summary>
        private static readonly Regex MitNeuerVorrichtung = new Regex(
            @"\G\s*=\s*new\s+Kulturvorrichtung\b", RegexOptions.Compiled);

        /// <summary>Vorlauf eines Feldes: nur Attribute und Modifizierer.</summary>
        private static readonly Regex Feldvorlauf = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|static|readonly|volatile|new|required)\s+)*$",
            RegexOptions.Compiled);

        /// <summary>Vorlauf einer Feldinitialisierung bis zum <c>new</c>.</summary>
        private static readonly Regex FeldvorlaufMitTyp = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|static|readonly|volatile|new|required)\s+)*" +
            @"Kulturvorrichtung\s*\??\s+[A-Za-z_]\w*\s*=\s*$",
            RegexOptions.Compiled);

        /// <summary>Eine Anweisung, die mit <c>using</c> (auch <c>await using</c>) beginnt.</summary>
        private static readonly Regex UsingVorlauf = new Regex(@"^\s*(?:await\s+)?using\b", RegexOptions.Compiled);

        /// <summary>Eine Zuweisung <c>[this.]feld =</c> bis zum <c>new</c>.</summary>
        private static readonly Regex Zuweisungsvorlauf = new Regex(
            @"^\s*(?:this\s*\.\s*)?([A-Za-z_]\w*)\s*=\s*$", RegexOptions.Compiled);

        /// <summary>Ist der Vorlauf eine Zuweisung an ein Vorrichtungsfeld der Klasse (oder
        /// einer umschließenden)? Das ist der Konstruktorweg, z. B. in <c>EposBunitContext</c>.</summary>
        private static bool IstFeldzuweisung(string vorlauf, Klasse k)
        {
            Match m = Zuweisungsvorlauf.Match(vorlauf);
            if (!m.Success) return false;
            for (Klasse c = k; c != null; c = c.Aussen)
            {
                if (c.Vorrichtungsfelder.Any(f => f.Name == m.Groups[1].Value)) return true;
            }
            return false;
        }

        /// <summary>Der Text vom Anfang der Anweisung (nach dem letzten <c>;</c>, <c>{</c> oder
        /// <c>}</c> im maskierten Text) bis zur Stelle <paramref name="index"/>.</summary>
        private static string Anweisungsvorlauf(string maske, int index)
        {
            int j = index - 1;
            while (j >= 0 && maske[j] != ';' && maske[j] != '{' && maske[j] != '}') j--;
            return maske.Substring(j + 1, index - j - 1);
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
            Assert.DoesNotContain(dateien, d => Path.GetFileName(d) == "StandardkulturEnUs.cs");
        }

        // =====================================================================
        //  Gegenproben — EPOS.UI.Tests (seit Auftrag #168, Tür je Klasse seit #531)
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
        /// <b>Gegenprobe zur zweiten Tür:</b> Eine Klasse, die die Vorrichtung nutzt (erbt von
        /// <c>EposBunitContext</c> — auch über eine Zwischenbasis wie <c>KiDialogwegBasis</c> —
        /// oder hält ein <c>Kulturvorrichtung</c>-Feld), braucht KEINE eigene Rückstellung —
        /// auch wenn sie mitten im Fall eine andere Kultur setzt (die Sprachwechsel-Klassen aus
        /// Auftrag #168), und ebenso nicht ihre verschachtelte Hilfsklasse.
        /// </summary>
        [Fact]
        public void Eine_Klasse_die_die_Vorrichtung_nutzt_braucht_keine_eigene_Rueckstellung()
        {
            Assert.Empty(UiFunde(
                "public class X : EposBunitContext { void M() { CultureInfo.CurrentUICulture = en; } }"));

            Assert.Empty(UiFunde(
                "public class X : IDisposable { private readonly Kulturvorrichtung _k = new(); " +
                "void M() { CultureInfo.CurrentUICulture = en; } public void Dispose() => _k.Dispose(); }"));

            Assert.Empty(UiFunde(
                "public abstract class Basis : EposBunitContext { }",
                "public class X : Basis { private sealed class Umschalter { " +
                "Umschalter() { CultureInfo.CurrentUICulture = en; } } }"));
        }

        /// <summary>
        /// <b>Gegenprobe zum Leck:</b> Ohne Vorrichtung UND ohne eigene Rückstellung fällt eine
        /// Datei auf — genau der Zustand, den Befund #167 in 105 von 113 Dateien fand.
        /// </summary>
        [Fact]
        public void Ohne_Vorrichtung_und_ohne_Rueckstellung_faellt_eine_Datei_auf()
        {
            List<string> fehlend = UiFunde(
                "public class X : BunitContext { X() { CultureInfo.CurrentUICulture = de; } }");
            Assert.Contains("CultureInfo.CurrentUICulture", fehlend);
        }

        /// <summary>
        /// <b>Gegenprobe zur Tür je Klasse (#531):</b> Bis #531 befreite schon das Wort
        /// <c>EposBunitContext</c> oder <c>Kulturvorrichtung</c> irgendwo in der Datei — auch in
        /// einem Kommentar — jede Zuweisung der Datei. Jetzt fällt die Zuweisung einer Klasse
        /// ohne Vorrichtung auf, auch neben einer bunit-Klasse in derselben Datei; eine
        /// Zuweisung, die nur in einem Kommentar oder einer Zeichenkette steht, zählt nicht.
        /// </summary>
        [Fact]
        public void Die_Tuer_des_UI_Waechters_gilt_je_Klasse_nicht_je_Datei()
        {
            Assert.Contains("CultureInfo.CurrentUICulture", UiFunde(
                "public class A : EposBunitContext { void M() { CultureInfo.CurrentUICulture = en; } }\n" +
                "public static class B { public static void Setze() { CultureInfo.CurrentUICulture = en; } }"));

            Assert.Contains("CultureInfo.CurrentUICulture", UiFunde(
                "// nutzt die Kulturvorrichtung\n" +
                "public class C { void M() { CultureInfo.CurrentUICulture = en; } }"));

            Assert.Empty(UiFunde(
                "public class D { // CultureInfo.CurrentUICulture = en;\n" +
                "string s = \"CultureInfo.CurrentUICulture = en;\"; }"));
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
            Assert.Contains(dateien, d => Path.GetFileName(d) == "DiagrammSvgTests.cs");
            Assert.DoesNotContain(dateien, d => Path.GetFileName(d) == "Kulturvorrichtung.cs");
            Assert.DoesNotContain(dateien, d => Path.GetFileName(d) == "StandardkulturEnUs.cs");
        }

        /// <summary>Die UI-Prüfung über synthetische Dateien (je Text eine).</summary>
        private static List<string> UiFunde(params string[] texte)
        {
            List<Quelle> quellen = texte.Select(t => Quelle.AusText(t)).ToList();
            var bestand = new Klassenbestand(quellen);
            return quellen.SelectMany(q => FehlendeRueckstellungenUi(q, bestand)).ToList();
        }

        // =====================================================================
        //  Gegenproben — Wächter A (seit Auftrag #531)
        // =====================================================================

        /// <summary><b>Gegenprobe A1:</b> ein Vorrichtungsfeld ohne <c>IDisposable</c> fällt auf —
        /// der Zustand der sechs Klassen vor #515.</summary>
        [Fact]
        public void Ein_Vorrichtungsfeld_ohne_IDisposable_faellt_auf()
        {
            List<string> funde = VorrichtungsFunde(Quelle.AusText(
                "public class X { private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung(); " +
                "public void Dispose() => _kultur.Dispose(); }"));
            Assert.Equal(new[] { "Probe.cs:1 X (Feld _kultur): ohne IDisposable/IAsyncDisposable in der eigenen Basisliste" }, funde);
        }

        /// <summary><b>Gegenprobe A2 — die Falle aus <c>EmissionsspalteTests</c> vor #515:</b>
        /// <c>IDisposable</c> an einer VERSCHACHTELTEN Klasse zählt nicht für die äußere, die
        /// das Feld hält; eine Prüfung je Datei hätte das übersehen.</summary>
        [Fact]
        public void IDisposable_an_einer_verschachtelten_Klasse_zaehlt_nicht_fuer_die_aeussere()
        {
            List<string> funde = VorrichtungsFunde(Quelle.AusText(
                "public class Aussen\n{\n    private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();\n" +
                "    private sealed class Sprachumschaltung : System.IDisposable { public void Dispose() { } }\n}"));
            Assert.Contains("Probe.cs:3 Aussen (Feld _kultur): ohne IDisposable/IAsyncDisposable in der eigenen Basisliste", funde);
            Assert.Contains("Probe.cs:3 Aussen (Feld _kultur): ruft _kultur.Dispose() nicht auf", funde);
            Assert.Equal(2, funde.Count);
        }

        /// <summary><b>Gegenprobe A3:</b> <c>IDisposable</c> ohne den Ruf <c>feld.Dispose()</c>
        /// fällt auf (die Klasse entsorgt nur etwas anderes).</summary>
        [Fact]
        public void Ein_Vorrichtungsfeld_ohne_Dispose_Ruf_faellt_auf()
        {
            List<string> funde = VorrichtungsFunde(Quelle.AusText(
                "public class X : IDisposable { private readonly Kulturvorrichtung _kultur = new(); " +
                "private readonly TestDatenbank _db = new(); public void Dispose() => _db.Dispose(); }"));
            Assert.Equal(new[] { "Probe.cs:1 X (Feld _kultur): ruft _kultur.Dispose() nicht auf" }, funde);
        }

        /// <summary><b>Gegenprobe A4:</b> jede zulässige Form bleibt still — Feld mit
        /// <c>IDisposable</c>, <c>EposBunitContext</c> mit Konstruktorzuweisung und
        /// <c>Dispose(bool)</c>, <c>using var</c>, <c>using (…)</c>, <c>using</c> mit Zieltyp,
        /// und Erwähnungen in Kommentar und Zeichenkette.</summary>
        [Fact]
        public void Richtig_entsorgte_und_gebundene_Vorrichtungen_bleiben_still()
        {
            Assert.Empty(VorrichtungsFunde(Quelle.AusText(
                "public class A : IDisposable { private readonly Kulturvorrichtung _k = new Kulturvorrichtung(); " +
                "public void Dispose() => _k.Dispose(); }\n" +
                "public class B : EposBunitContext { private readonly Kulturvorrichtung _en; " +
                "public B() { _en = new Kulturvorrichtung(\"en-US\"); } " +
                "protected override void Dispose(bool d) { if (d) { _en?.Dispose(); } base.Dispose(d); } }\n" +
                "public class C { void M() { using var k = new Kulturvorrichtung(); } " +
                "void N() { using (new Kulturvorrichtung()) { } } void O() { using Kulturvorrichtung k = new(); } }\n" +
                "public class D { // var k = new Kulturvorrichtung();\n" +
                "string s = \"new Kulturvorrichtung()\"; }")));
        }

        /// <summary><b>Gegenprobe A5:</b> eine Vorrichtung ohne <c>using</c> fällt auf — als
        /// <c>var</c>, als lokale Deklaration mit Zieltyp und als Fabrik mit Ausdruckskörper.</summary>
        [Fact]
        public void Eine_Vorrichtung_ohne_using_faellt_auf()
        {
            List<string> funde = VorrichtungsFunde(Quelle.AusText(
                "public class X\n{\n    void M() { var k = new Kulturvorrichtung(); k.Dispose(); }\n" +
                "    void N() { Kulturvorrichtung k = new(); }\n" +
                "    static Kulturvorrichtung Deutsch() => new Kulturvorrichtung();\n}"));
            Assert.Equal(new[]
            {
                "Probe.cs:3 X: new Kulturvorrichtung(...) ohne using",
                "Probe.cs:5 X: new Kulturvorrichtung(...) ohne using",
                "Probe.cs:4 X: lokale Kulturvorrichtung k ohne using",
            }, funde);
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand von Wächter A:</b> Der Leser findet die Vorrichtungsfelder
        /// wirklich — in beiden Projekten, darunter die Klassen der Befunde #515 und eine
        /// Nicht-bunit-Klasse aus <c>EPOS.UI.Tests</c> (sonst liefe ein blinder Leser grün).
        /// </summary>
        [Fact]
        public void Der_Waechter_A_sieht_die_Vorrichtungsfelder_beider_Projekte()
        {
            string wurzel = Arbeitsbaum();
            var klassen = new List<string>();
            foreach (string projekt in Testprojekte)
            {
                foreach (string datei in Quelldateien(wurzel, projekt))
                {
                    klassen.AddRange(Quelle.Lies(datei, wurzel).Klassen
                        .Where(k => k.Vorrichtungsfelder.Count > 0)
                        .Select(k => projekt + "/" + k.Pfad));
                }
            }

            Assert.True(klassen.Count > 150, "Nur " + klassen.Count + " Klassen mit Vorrichtungsfeld gefunden.");
            Assert.Contains("EPOS.Kern.Tests/EmissionsspalteTests", klassen);
            Assert.Contains("EPOS.Kern.Tests/GebaeudeHochrechnungTests", klassen);
            Assert.Contains("EPOS.UI.Tests/FenstermassTests", klassen);
            Assert.Contains("EPOS.UI.Tests/EposBunitContext", klassen);
        }

        // =====================================================================
        //  Die Standardkultur en-US (seit Auftrag #531)
        // =====================================================================

        /// <summary>
        /// <b>Wächter über die Standardkultur:</b> Beide <see cref="StandardkulturDateien"/>
        /// bestehen, tragen einen <c>[ModuleInitializer]</c> und setzen im Code die vier Werte
        /// (<see cref="StandardkulturZiele"/>) auf en-US — keine andere Kultur. Wer eine davon
        /// entfernt oder umstellt, macht den en-US-Nachweis des lokalen Gates still; das soll
        /// nicht unbemerkt geschehen.
        /// </summary>
        [Fact]
        public void Die_Standardkultur_en_US_steht_in_beiden_Testprojekten()
        {
            string wurzel = Arbeitsbaum();
            foreach (string relativ in StandardkulturDateien)
            {
                string datei = Path.Combine(wurzel, relativ);
                Assert.True(File.Exists(datei), "Die Standardkultur fehlt: " + relativ + " (Auftrag #531).");

                Quelle q = Quelle.Lies(datei, wurzel);
                Assert.True(Regex.IsMatch(q.Maske, @"\[\s*ModuleInitializer\s*\]"),
                    relativ + " traegt keinen [ModuleInitializer].");
                foreach (string ziel in StandardkulturZiele)
                {
                    Assert.True(ZielregexVoll(ziel).Matches(q.Text).Cast<Match>().Any(m => q.IstCode(m.Index)),
                        relativ + " setzt " + ziel + " nicht.");
                }
                Assert.Contains("\"en-US\"", q.Text, StringComparison.Ordinal);
                Assert.DoesNotMatch(new Regex("\"[a-z]{2}-[A-Z]{2}\""), q.Text.Replace("\"en-US\"", ""));
            }
        }

        /// <summary>
        /// <b>Gegenprobe zur Standardkultur zur Laufzeit:</b> Ohne Pinnung gilt en-US, und die
        /// Ressourcen antworten englisch; eine <see cref="Kulturvorrichtung"/> schaltet auf de-DE
        /// und stellt danach wieder auf en-US zurück. Dasselbe prüft
        /// <c>EPOS.UI.Tests/StandardkulturTests</c> für die andere Testassembly.
        /// </summary>
        [Fact]
        public void Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck()
        {
            const string englisch = "Hours of the year [h]";
            const string deutsch = "Jahresstunden [h]";

            Assert.Equal("en-US", System.Globalization.CultureInfo.CurrentUICulture.Name);
            Assert.Equal("en-US", System.Globalization.CultureInfo.CurrentCulture.Name);
            Assert.Equal(englisch, WindowsFormsApplication1.MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN);

            using (new Kulturvorrichtung())
            {
                Assert.Equal("de-DE", System.Globalization.CultureInfo.CurrentUICulture.Name);
                Assert.Equal(deutsch, WindowsFormsApplication1.MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN);
            }

            Assert.Equal("en-US", System.Globalization.CultureInfo.CurrentUICulture.Name);
            Assert.Equal(englisch, WindowsFormsApplication1.MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN);
        }

        // =====================================================================
        //  Werkzeug — Regionen des Kern-Wächters
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
        /// an dieser Stelle (dieselbe Vereinfachung wie die übrigen Quelltext-Wächter des Projekts;
        /// der UI-Wächter reicht seit #531 den maskierten Text herein).
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

        // =====================================================================
        //  Werkzeug — Quelltextleser (seit Auftrag #531)
        // =====================================================================

        /// <summary>Ein Vorrichtungsfeld einer Klasse: Name und Stelle im Text.</summary>
        private sealed class Feld
        {
            public string Name;
            public int Index;
        }

        /// <summary>
        /// Eine Klasse (auch <c>struct</c>, <c>record</c>, <c>interface</c>) im maskierten Text:
        /// Name, Pfad der Verschachtelung (<c>Aussen.Innen</c>), eigene Basisliste, Rumpf von
        /// <c>{</c> bis <c>}</c>, umschließende Klasse und die Vorrichtungsfelder ihres Rumpfs.
        /// </summary>
        private sealed class Klasse
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
        private sealed class Quelle
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
        private sealed class Klassenbestand
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
        //  Werkzeug — Dateien
        // =====================================================================

        /// <summary>Alle <c>.cs</c>-Dateien in <c>EPOS.Kern.Tests</c>, ohne Bauordner.</summary>
        private static string[] Testdateien()
        {
            string wurzel = Arbeitsbaum();

            // Die eigene Datei ist ausgenommen: ihre Gegenproben-Textzeilen und die
            // Klassendoku nennen "CultureInfo.DefaultThread(UI)Culture =" absichtlich als
            // reinen Text (Muster fuer den Leser, NICHT als echter Setzer) - ohne die
            // Ausnahme faende sich der Waechter selbst. Ebenso ausgenommen, nach Pfad: die
            // Standardkultur en-US (Auftrag #531) - sie setzt den Ausgangszustand jedes Laufs.
            return Quelldateien(wurzel, "EPOS.Kern.Tests")
                .Where(d => Path.GetFileName(d) != "KulturwaechterTests.cs")
                .Where(d => !IstStandardkulturDatei(wurzel, d))
                .ToArray();
        }

        /// <summary>
        /// Alle <c>.cs</c>-Dateien in <c>EPOS.UI.Tests</c>, ohne Bauordner und ohne die
        /// Vorrichtung selbst (<c>Kulturvorrichtung.cs</c> — Auftrag #168, erlaubte Ausnahme:
        /// ihre eigenen Zuweisungen und Rückstellungen SIND die Vorrichtung, nicht deren
        /// Umgehung) und ohne die Standardkultur en-US (nach Pfad, Auftrag #531).
        /// </summary>
        private static string[] TestdateienUi()
        {
            string wurzel = Arbeitsbaum();
            return Quelldateien(wurzel, "EPOS.UI.Tests")
                .Where(d => Path.GetFileName(d) != "Kulturvorrichtung.cs")
                .Where(d => !IstStandardkulturDatei(wurzel, d))
                .ToArray();
        }

        /// <summary>Ist die Datei eine der zwei <see cref="StandardkulturDateien"/>?</summary>
        private static bool IstStandardkulturDatei(string wurzel, string datei)
            => StandardkulturDateien.Contains(
                Path.GetRelativePath(wurzel, datei).Replace('\\', '/'), StringComparer.Ordinal);

        /// <summary>Alle <c>.cs</c>-Dateien eines Projektordners, ohne Bauordner, sortiert.</summary>
        private static string[] Quelldateien(string wurzel, string projekt)
        {
            string ordner = Path.Combine(wurzel, projekt);
            Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);

            return Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories)
                .Where(OhneBauordner)
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
