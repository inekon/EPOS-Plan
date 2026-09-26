using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;
using static EPOS.Kern.Tests.Quelltextleser;

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
    /// Kommentar). Der Leser dafür (<see cref="Quelltextleser.Quelle"/>) maskiert Kommentare,
    /// Zeichenketten und Präprozessorzeilen, bevor er Klassen, Felder und Klammern zählt.
    /// (3) <b>Die Standardkultur en-US:</b> <c>EPOS.Kern.Tests/StandardkulturEnUs.cs</c> und
    /// <c>EPOS.UI.Tests/StandardkulturEnUs.cs</c> setzen beim Laden der Testassembly en-US —
    /// die Kultur des Windows-Läufers. Ein Fall, der deutsche Ressourcentexte erwartet und
    /// nicht pinnt, fällt damit auf jedem Rechner, schon im lokalen Gate, statt erst auf dem
    /// Windows-Läufer nach dem Push (ubuntu läuft invariant und liefert die neutralen,
    /// deutschen Ressourcen). Genau diese zwei Dateien (nach Pfad) sind von der
    /// Rückstell-Pflicht ausgenommen; jeder andere Setzer bleibt verboten, und ein eigener
    /// Fall verlangt, dass beide bestehen und en-US setzen.</para>
    ///
    /// <para><b>Seit Nachlese #534 (26.09.2026)</b> steht der Quelltextleser in
    /// <c>EPOS.Kern.Tests/Quelltextleser.cs</c> (<see cref="Quelltextleser"/>); diese Klasse behält
    /// die Regeln, Meldungen, Selbsttests, Bestandsproben, den Standardkultur-Wächter und die
    /// Regionen- und Dateihilfen. Mit derselben Nachlese (Anwenderentscheid 26.09.2026) tragen
    /// auch <c>KiKern.Tests</c>, <c>SpeicherEngine.Tests</c> und <c>SpeicherPlanung.Tests</c> je
    /// eine <c>StandardkulturEnUs.cs</c>: Die Ausnahme gilt für alle fünf Dateien, der
    /// Standardkultur-Wächter verlangt alle fünf, und der Setzer-Wächter
    /// (<c>DefaultThreadCurrent(UI)Culture</c> nur mit Rückstellung) sucht außer
    /// <c>EPOS.Kern.Tests</c> auch diese drei Projekte ab.</para>
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
        /// Die drei kleinen Testprojekte ohne Kulturvorrichtung (Nachlese #534): Der
        /// Setzer-Wächter sucht sie zusätzlich zu <c>EPOS.Kern.Tests</c> ab.
        /// </summary>
        private static readonly string[] KleineTestprojekte =
            { "KiKern.Tests", "SpeicherEngine.Tests", "SpeicherPlanung.Tests" };

        /// <summary>
        /// Die fünf Dateien der Standardkultur en-US (repo-relativ, eine je Testprojekt) — die
        /// EINZIGEN Setzer, die nicht zurückstellen dürfen, weil sie den Ausgangszustand jedes
        /// Laufs herstellen.
        /// </summary>
        private static readonly string[] StandardkulturDateien =
        {
            "EPOS.Kern.Tests/StandardkulturEnUs.cs",
            "EPOS.UI.Tests/StandardkulturEnUs.cs",
            "KiKern.Tests/StandardkulturEnUs.cs",
            "SpeicherEngine.Tests/StandardkulturEnUs.cs",
            "SpeicherPlanung.Tests/StandardkulturEnUs.cs",
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
        //  Der Wächter — EPOS.Kern.Tests und die drei kleinen Testprojekte (seit #534)
        // =====================================================================

        /// <summary>
        /// Jeder <c>DefaultThreadCurrentCulture</c>/<c>DefaultThreadCurrentUICulture</c>-Setzer
        /// hat in DERSELBEN Datei eine Rückstellung auf dasselbe Ziel in einem
        /// <c>Dispose()</c>- oder <c>finally</c>-Block. Abgesucht werden <c>EPOS.Kern.Tests</c>
        /// und die <see cref="KleineTestprojekte"/>.
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

        /// <summary>Hinter einer Deklaration: <c>= new Kulturvorrichtung</c>.</summary>
        private static readonly Regex MitNeuerVorrichtung = new Regex(
            @"\G\s*=\s*new\s+Kulturvorrichtung\b", RegexOptions.Compiled);

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
            Assert.Contains(dateien, d => Path.GetFileName(d) == "KiAbsichtTests.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "FlottenDiagnoseTests.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "OrToolsFlottenPlanerTests.cs");
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
        /// <b>Wächter über die Standardkultur:</b> Alle fünf <see cref="StandardkulturDateien"/>
        /// (eine je Testprojekt, seit #534 auch in den drei kleinen) bestehen, tragen einen <c>[ModuleInitializer]</c> und setzen im Code die vier Werte
        /// (<see cref="StandardkulturZiele"/>) auf en-US — keine andere Kultur. Wer eine davon
        /// entfernt oder umstellt, macht den en-US-Nachweis des lokalen Gates still; das soll
        /// nicht unbemerkt geschehen.
        /// </summary>
        [Fact]
        public void Die_Standardkultur_en_US_steht_in_allen_Testprojekten()
        {
            string wurzel = Arbeitsbaum();
            foreach (string relativ in StandardkulturDateien)
            {
                string datei = Path.Combine(wurzel, relativ);
                Assert.True(File.Exists(datei), "Die Standardkultur fehlt: " + relativ + " (Auftrag #531/#534).");

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
        //  Werkzeug — Dateien
        // =====================================================================

        /// <summary>Alle <c>.cs</c>-Dateien in <c>EPOS.Kern.Tests</c> und den
        /// <see cref="KleineTestprojekte"/> (seit #534), ohne Bauordner.</summary>
        private static string[] Testdateien()
        {
            string wurzel = Arbeitsbaum();

            // Die eigene Datei ist ausgenommen: ihre Gegenproben-Textzeilen und die
            // Klassendoku nennen "CultureInfo.DefaultThread(UI)Culture =" absichtlich als
            // reinen Text (Muster fuer den Leser, NICHT als echter Setzer) - ohne die
            // Ausnahme faende sich der Waechter selbst. Ebenso ausgenommen, nach Pfad: die
            // Standardkultur en-US (Auftrag #531/#534) - sie setzt den Ausgangszustand jedes Laufs.
            return new[] { "EPOS.Kern.Tests" }.Concat(KleineTestprojekte)
                .SelectMany(projekt => Quelldateien(wurzel, projekt))
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

        /// <summary>Ist die Datei eine der fünf <see cref="StandardkulturDateien"/>?</summary>
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
