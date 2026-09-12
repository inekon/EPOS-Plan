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
    /// Der Wächter über die ORDNUNG der Dokumentation — Auftrag #241 (Anwender,
    /// 12.09.2026: „Verschiebe alle .MD Dateien in ein Verzeichnis und strukturiere nach
    /// aktuell und überholt. Sie sollen nach wie vor für Claude genutzt werden.").
    ///
    /// <para><b>Die Regel.</b> Alle Markdown-Papiere liegen unter <c>Dokumentation/</c> —
    /// <c>aktuell/</c> für die gültigen Arbeitsgrundlagen, <c>ueberholt/</c> für
    /// Abgeschlossenes und Ersetztes, darunter <c>ueberholt/Protokolle/</c> für die
    /// Etappenprotokolle. In der Wurzel bleiben nur <c>CLAUDE.md</c> und
    /// <c>README.md</c>; die vier weiteren <c>CLAUDE.md</c> bleiben an ihrem Projektordner,
    /// weil Claude Code sie DORT lädt.</para>
    ///
    /// <para><b>Warum ein Wächter.</b> Eine Ordnung, die niemand prüft, zerfällt beim
    /// nächsten Umzug: 265 Dateien sind bewegt und 311 relative Verweise nachgezogen
    /// worden. Ein einziger übersehener Verweis zeigt danach ins Leere, ohne dass es
    /// jemandem auffällt — Markdown meldet keinen Fehler. Ebenso still wäre ein Papier,
    /// das niemand im Index findet, oder eine neue <c>.md</c>, die wieder in der Wurzel
    /// landet.</para>
    ///
    /// <para><b>Was der Wächter NICHT prüft.</b> Absolute Adressen (<c>http</c>,
    /// <c>mailto</c>), reine Ankerziele (<c>#abschnitt</c>) und die Papiere, die
    /// ausdrücklich am Ort bleiben (Werkzeug-Liesmich, Referenzpaket
    /// <c>Projekte/Speichersimulation/</c>) — der Index nennt sie unter „Bleibt am
    /// Ort".</para>
    /// </summary>
    public sealed class DokumentationLinkWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Der Index, den Fall 2 gegen den Bestand hält.</summary>
        private const string Index = "Dokumentation/LIESMICH.md";

        /// <summary>
        /// Die Markdown-Dateien AUSSERHALB von <c>Dokumentation/</c>, deren Verweise
        /// ebenfalls stimmen müssen: die fünf <c>CLAUDE.md</c> — die Arbeitsregeln, die
        /// Claude Code lädt — und die Startseite des Repositoriums.
        /// </summary>
        private static readonly string[] WeitereWegweiser =
        {
            "CLAUDE.md",
            "README.md",
            "EPOS.Kern/CLAUDE.md",
            "EPOS.UI/CLAUDE.md",
            "EPOS.iOS/CLAUDE.md",
            "WindowsFormsApplication1/CLAUDE.md",
        };

        /// <summary>
        /// <b>Die vorbestehenden Lücken</b> — siebzehn Fundstellen in sechzehn Paarungen
        /// (in <c>K3_BivalenzTemperatur_Protokoll.md</c> steht dasselbe Ziel zweimal) —
        /// Verweise, die schon vor dem Umzug ins
        /// Leere zeigten, weil ihr Ziel aus dem Arbeitsbaum verschwunden ist: die
        /// Referenzbasen vor R4 (seit AUF‑Q1 vom 12.09.2026 auch nicht mehr in der
        /// Git-Geschichte; ihre Protokolle liegen unter
        /// <c>Dokumentation/ueberholt/Referenzbasen/</c>) und vier WinForms-Masken, die mit
        /// den Wellen iU9‑W13 und W14a gefallen sind. Sie sind mit #241 NICHT
        /// stillschweigend entlinkt worden — ein Protokoll darf nennen, wogegen es gemessen
        /// hat, auch wenn die Messung heute nicht mehr im Baum liegt. Wer eine dieser
        /// Stellen tilgt, streicht hier die Zeile mit.
        ///
        /// <para><b>Die fünf letzten Paarungen kamen mit #244 dazu</b> (Anwenderentscheid
        /// AUF‑Q1, 12.09.2026): Vor dem Umschreiben der Git-Geschichte sind die Protokolle
        /// der 24 entfernten Referenzbasen byte-gleich nach
        /// <c>Dokumentation/ueberholt/Referenzbasen/</c> gesichert worden. Fünf von ihnen
        /// nennen den Ablageort ihrer Etappenprotokolle von damals
        /// (<c>WindowsFormsApplication1/Allgemein/Simulation/…</c>, zwei Konzepte in der
        /// Repository-Wurzel) — beide Orte gibt es seit #241 nicht mehr. Byte-gleich
        /// gesichert heißt: unverändert, auch in den Verweisen.</para>
        /// </summary>
        private static readonly (string Datei, string Ziel)[] VorbestehendeLuecken =
        {
            ("Dokumentation/ueberholt/Protokolle/Reporting/W4_E8_Abnahme_Protokoll.md",
             "../../../../Referenzlaeufe/2026-08-19_B6/lauf_protokoll.md"),
            ("Dokumentation/ueberholt/Protokolle/Reporting/W4_Umsetzungsstand.md",
             "../../../../Referenzlaeufe/2026-08-19_B6/lauf_protokoll.md"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Pruefung_Pufferspeicher_Ergebnisse_Protokoll.md",
             "../../../../Referenzlaeufe/2026-08-19_B6/lauf_protokoll.md"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Pruefung_Pufferspeicher_Ergebnisse_Protokoll.md",
             "../../../../Referenzlaeufe/2026-08-19_B5/lauf_protokoll.md"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/K3_BivalenzTemperatur_Protokoll.md",
             "../../../../Referenzlaeufe/2026-08-15_B3/"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/K3_BivalenzTemperatur_Protokoll.md",
             "../../../../Referenzlaeufe/2026-08-15_B3/lauf_protokoll.md"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Paket7_Ergebnis_Anzeigen_Protokoll.md",
             "../../../../Referenzlaeufe/2026-08-14_Paket7/vergleich_protokoll.md"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Befund_convertTxt2Double_Dezimaltrennzeichen.md",
             "../../../../WindowsFormsApplication1/Views/Wärmepumpe/Form_WP_einlesen.cs"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Befund_convertTxt2Double_Dezimaltrennzeichen.md",
             "../../../../WindowsFormsApplication1/Views/Heizkessel/Form_Heizkessel_einlesen.cs"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Befund_convertTxt2Double_Dezimaltrennzeichen.md",
             "../../../../WindowsFormsApplication1/Views/Pufferspeicher/Form_PufferSp_einlesen.cs"),
            ("Dokumentation/ueberholt/Protokolle/Simulation/Befund_convertTxt2Double_Dezimaltrennzeichen.md",
             "../../../../WindowsFormsApplication1/Views/Photovoltaik/Form_AdminPV.cs"),

            // #244 — die byte-gleich gesicherten Protokolle der entfernten Referenzbasen.
            ("Dokumentation/ueberholt/Referenzbasen/2026-08-28_B2/lauf_protokoll.md",
             "../../WindowsFormsApplication1/Allgemein/Simulation/B2_KesselTemperaturmodus_Protokoll.md"),
            ("Dokumentation/ueberholt/Referenzbasen/2026-08-29_Booster/lauf_protokoll.md",
             "../../WindowsFormsApplication1/Allgemein/Simulation/B3_QuelleUnbegrenzt_Protokoll.md"),
            ("Dokumentation/ueberholt/Referenzbasen/2026-08-29_E1E2/lauf_protokoll.md",
             "../../Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md"),
            ("Dokumentation/ueberholt/Referenzbasen/2026-09-02_PA1_nach-PaketA/lauf_protokoll.md",
             "../../WindowsFormsApplication1/Allgemein/Simulation/PaketA_Zeitbasis_E1_Protokoll.md"),
            ("Dokumentation/ueberholt/Referenzbasen/2026-09-03_PB1_nach-PaketB/lauf_protokoll.md",
             "../../WindowsFormsApplication1/Allgemein/Simulation/PaketB_E2_Modellwahl_Protokoll.md"),
        };

        /// <summary>Ein Markdown-Verweis <c>[Text](Ziel)</c>.</summary>
        private static readonly Regex Verweis =
            new Regex(@"\[[^\]\r\n]*\]\(([^)\r\n]*)\)", RegexOptions.Compiled);

        // =====================================================================
        //  Fall 1 — kein Verweis zeigt ins Leere
        // =====================================================================

        /// <summary>
        /// Jeder relative Verweis der Dokumentation und der sechs Wegweiser trifft eine
        /// vorhandene Datei oder einen vorhandenen Ordner.
        /// </summary>
        [Fact]
        public void Kein_relativer_Verweis_zeigt_ins_Leere()
        {
            string wurzel = Arbeitsbaum();
            var funde = new List<string>();

            foreach (string datei in GeprueftePapiere())
            {
                string text = File.ReadAllText(Path.Combine(wurzel, datei));
                string[] zeilen = text.Replace("\r\n", "\n").Split('\n');

                for (int i = 0; i < zeilen.Length; i++)
                {
                    foreach (string ziel in Ziele(zeilen[i]))
                    {
                        if (VorbestehendeLuecken.Any(l => l.Datei == datei && l.Ziel == ziel))
                            continue;
                        if (Trifft(wurzel, datei, ziel)) continue;
                        funde.Add(datei + ":" + (i + 1) + "  -> " + ziel);
                    }
                }
            }

            Assert.True(funde.Count == 0,
                "Diese relativen Verweise zeigen ins Leere (Auftrag #241). Wer ein Papier " +
                "bewegt, zieht seine Verweise im selben Schritt nach - und die Verweise, " +
                "die auf es zeigen:\n" + string.Join("\n", funde));
        }

        // =====================================================================
        //  Fall 2 — kein Papier ohne Indexzeile
        // =====================================================================

        /// <summary>
        /// <c>Dokumentation/LIESMICH.md</c> nennt jede Datei unter <c>aktuell/</c> und
        /// <c>ueberholt/</c>; für die Protokolle genügt die Zeile ihres Unterordners.
        /// </summary>
        [Fact]
        public void Der_Index_nennt_jedes_Papier()
        {
            string wurzel = Arbeitsbaum();
            string index = File.ReadAllText(Path.Combine(wurzel, Index));
            var fehlend = new List<string>();

            foreach (string papier in Dokumentationspapiere())
            {
                if (papier == Index) continue;
                string rel = papier.Substring("Dokumentation/".Length);

                string gesucht = rel.StartsWith("ueberholt/Protokolle/", StringComparison.Ordinal)
                    ? string.Join("/", rel.Split('/').Take(3)) + "/"   // ueberholt/Protokolle/<Ordner>/
                    : rel;

                if (index.IndexOf(gesucht, StringComparison.Ordinal) < 0) fehlend.Add(rel);
            }

            Assert.True(fehlend.Count == 0,
                "Diese Papiere stehen in keiner Zeile von " + Index + " - ein Waisenkind " +
                "findet niemand wieder (Auftrag #241):\n" + string.Join("\n", fehlend.Distinct()));
        }

        // =====================================================================
        //  Fall 3 — die Ordnung selbst
        // =====================================================================

        /// <summary>
        /// In der Wurzel liegen außer <c>CLAUDE.md</c> und <c>README.md</c> keine
        /// Markdown-Dateien mehr, und unter <c>WindowsFormsApplication1/Allgemein/</c>
        /// kein <c>*_Protokoll.md</c>.
        /// </summary>
        [Fact]
        public void Weder_die_Wurzel_noch_der_Quellbaum_fuehren_wieder_Papiere()
        {
            string wurzel = Arbeitsbaum();

            string[] inDerWurzel = Directory
                .GetFiles(wurzel, "*.md", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(n => n != "CLAUDE.md" && n != "README.md")
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            Assert.True(inDerWurzel.Length == 0,
                "Diese Markdown-Dateien liegen wieder in der Wurzel; sie gehoeren nach " +
                "Dokumentation/aktuell oder Dokumentation/ueberholt (Auftrag #241):\n" +
                string.Join("\n", inDerWurzel));

            string allgemein = Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein");
            string[] protokolle = Directory.Exists(allgemein)
                ? Directory.GetFiles(allgemein, "*_Protokoll.md", SearchOption.AllDirectories)
                    .Select(d => Kurzname(wurzel, d)).OrderBy(n => n, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();

            Assert.True(protokolle.Length == 0,
                "Diese Protokolle liegen wieder zwischen den Quelldateien; sie gehoeren nach " +
                "Dokumentation/ueberholt/Protokolle (Auftrag #241):\n" +
                string.Join("\n", protokolle));
        }

        // =====================================================================
        //  Gegenproben
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene
        /// Papiere in beiden Ordnern — ein leerer Bestand liefe sonst grün durch, ohne je
        /// etwas geprüft zu haben.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_beide_Ordner_und_die_Protokolle()
        {
            string[] papiere = Dokumentationspapiere();

            Assert.True(papiere.Length > 250,
                "Nur " + papiere.Length + " Papiere unter Dokumentation/ gefunden.");
            Assert.Contains(papiere, p => p.StartsWith("Dokumentation/aktuell/", StringComparison.Ordinal));
            Assert.Contains(papiere, p => p.StartsWith("Dokumentation/ueberholt/", StringComparison.Ordinal));
            Assert.Contains(papiere, p => p.StartsWith("Dokumentation/ueberholt/Protokolle/", StringComparison.Ordinal));

            // Drei benannte Papiere - sie belegen, dass die Einteilung wirklich steht.
            Assert.Contains("Dokumentation/aktuell/BETRIEB_SQLITE.md", papiere);
            Assert.Contains("Dokumentation/aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md", papiere);
            Assert.Contains("Dokumentation/ueberholt/Konzept_iOS-Portierung_EPOS-Plan.md", papiere);

            // Und die sechs Wegweiser gibt es alle.
            string wurzel = Arbeitsbaum();
            foreach (string w in WeitereWegweiser)
                Assert.True(File.Exists(Path.Combine(wurzel, w.Replace('/', Path.DirectorySeparatorChar))),
                            "Wegweiser nicht gefunden: " + w);
        }

        /// <summary>
        /// <b>Gegenprobe zum Leser:</b> Die Schreibweisen des Bestands werden erkannt —
        /// spitze Klammern, Anker, Titel, <c>%20</c> —, und was kein relativer Verweis
        /// ist, bleibt außen vor. Ohne diesen Fall wäre der Wächter stumm, sobald jemand
        /// anders schreibt.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_die_Schreibweisen_und_laesst_das_Fremde_liegen()
        {
            Assert.Equal(new[] { "aktuell/BETRIEB_SQLITE.md" },
                         Ziele("siehe [`BETRIEB_SQLITE.md`](aktuell/BETRIEB_SQLITE.md) dort"));
            Assert.Equal(new[] { "../EPOS.Kern/CLAUDE.md" },
                         Ziele("[Kern](<../EPOS.Kern/CLAUDE.md>)"));
            Assert.Equal(new[] { "aktuell/Konzept_Einheiten_EPOS-Plan.md" },
                         Ziele("[K](aktuell/Konzept_Einheiten_EPOS-Plan.md#kapitel-9)"));
            Assert.Equal(new[] { "aktuell/Doku PV.md" },
                         Ziele("[K](aktuell/Doku%20PV.md \"Titel\")"));
            Assert.Equal(new[] { "ueberholt/Protokolle/" },
                         Ziele("[Ordner](ueberholt/Protokolle/)"));

            // Kein relativer Verweis: absolute Adressen, reine Anker, Bildverweise ohne Ziel.
            Assert.Empty(Ziele("[Wiki](https://wiki.epos-plan.de/Seite)"));
            Assert.Empty(Ziele("[Post](mailto:info@inekon.de)"));
            Assert.Empty(Ziele("[Kapitel 3](#kapitel-3)"));
            Assert.Empty(Ziele("[leer]()"));
            Assert.Empty(Ziele("kein Verweis, nur `aktuell/BETRIEB_SQLITE.md` als Text"));
        }

        /// <summary>
        /// <b>Gegenprobe zum Wächter selbst:</b> Ein erfundenes Ziel fällt auf, ein
        /// vorhandenes nicht — und eine Ausnahme wirkt nur für IHRE Datei.
        /// </summary>
        [Fact]
        public void Ein_erfundenes_Ziel_faellt_auf()
        {
            string wurzel = Arbeitsbaum();

            Assert.True(Trifft(wurzel, Index, "aktuell/BETRIEB_SQLITE.md"));
            Assert.True(Trifft(wurzel, Index, "../CLAUDE.md"));
            Assert.True(Trifft(wurzel, Index, "ueberholt/Protokolle/Simulation/"));
            Assert.False(Trifft(wurzel, Index, "aktuell/Gibt_Es_Nicht.md"));
            Assert.False(Trifft(wurzel, "CLAUDE.md", "Dokumentation/aktuell/Gibt_Es_Nicht.md"));

            // Die Ausnahmeliste greift nur bei der Paarung Datei + Ziel.
            (string Datei, string Ziel) luecke = VorbestehendeLuecken[0];
            Assert.False(Trifft(wurzel, luecke.Datei, luecke.Ziel));
            Assert.Contains(VorbestehendeLuecken, l => l.Datei == luecke.Datei && l.Ziel == luecke.Ziel);
            Assert.DoesNotContain(VorbestehendeLuecken, l => l.Datei == Index && l.Ziel == luecke.Ziel);
        }

        /// <summary>
        /// <b>Gegenprobe zum Index:</b> Er nennt die Regel, beide Ordner und den Abschnitt
        /// „Bleibt am Ort" — ein Index ohne sie wäre eine Liste, keine Ordnung.
        /// </summary>
        [Fact]
        public void Der_Index_traegt_Regel_Pflegeregel_und_den_Abschnitt_Bleibt_am_Ort()
        {
            string index = File.ReadAllText(Path.Combine(Arbeitsbaum(), Index));

            Assert.Contains("Die Regel", index, StringComparison.Ordinal);
            Assert.Contains("Die Pflegeregel", index, StringComparison.Ordinal);
            Assert.Contains("Bleibt am Ort", index, StringComparison.Ordinal);
            Assert.Contains("aktuell/", index, StringComparison.Ordinal);
            Assert.Contains("ueberholt/", index, StringComparison.Ordinal);
            Assert.Contains("CLAUDE.md", index, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>
        /// Die relativen Verweisziele einer Zeile — ohne absolute Adressen, ohne reine
        /// Anker, mit abgeschnittenem Anker und Titel und aufgelöstem <c>%20</c>.
        /// </summary>
        private static string[] Ziele(string zeile)
        {
            var ziele = new List<string>();

            foreach (Match m in Verweis.Matches(zeile))
            {
                string z = m.Groups[1].Value.Trim();
                if (z.StartsWith("<", StringComparison.Ordinal) && z.EndsWith(">", StringComparison.Ordinal))
                    z = z.Substring(1, z.Length - 2).Trim();

                int titel = z.IndexOf(" \"", StringComparison.Ordinal);
                if (titel >= 0) z = z.Substring(0, titel).Trim();

                int anker = z.IndexOf('#');
                if (anker >= 0) z = z.Substring(0, anker);

                if (z.Length == 0) continue;
                if (z.StartsWith("#", StringComparison.Ordinal)) continue;
                if (z.StartsWith("/", StringComparison.Ordinal)) continue;
                if (Regex.IsMatch(z, @"^[a-zA-Z][a-zA-Z0-9+.-]*:")) continue;   // http:, https:, mailto:, …

                ziele.Add(z.Replace("%20", " "));
            }

            return ziele.ToArray();
        }

        /// <summary>Trifft das Ziel, von <paramref name="datei"/> aus gelesen, etwas Vorhandenes?</summary>
        private static bool Trifft(string wurzel, string datei, string ziel)
        {
            string ordner = Path.GetDirectoryName(Path.Combine(wurzel, datei.Replace('/', Path.DirectorySeparatorChar)));
            string voll = Path.GetFullPath(Path.Combine(ordner, ziel.Replace('/', Path.DirectorySeparatorChar)));
            return File.Exists(voll) || Directory.Exists(voll);
        }

        /// <summary>Alle Markdown-Papiere unter <c>Dokumentation/</c>, repo-relativ.</summary>
        private static string[] Dokumentationspapiere()
        {
            string wurzel = Arbeitsbaum();
            string ordner = Path.Combine(wurzel, "Dokumentation");
            Assert.True(Directory.Exists(ordner), "Ordner nicht gefunden: " + ordner);

            return Directory.GetFiles(ordner, "*.md", SearchOption.AllDirectories)
                .Select(d => Kurzname(wurzel, d))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>Die Dokumentation und die sechs Wegweiser daneben.</summary>
        private static string[] GeprueftePapiere()
            => Dokumentationspapiere().Concat(WeitereWegweiser)
                .OrderBy(p => p, StringComparer.Ordinal).ToArray();

        private static string Kurzname(string wurzel, string datei)
            => datei.Substring(wurzel.Length).TrimStart(Path.DirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/');

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>ParallelitaetWacheTests</c>.</summary>
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
