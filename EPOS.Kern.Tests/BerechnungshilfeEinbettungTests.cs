using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die WACHE über die EINBETTUNG der Hilferubrik „Berechnung" — Paket
    /// <b>H13</b> (Anwenderwunsch vom 06.09.2026).
    ///
    /// <para><b>Worum es geht.</b> Die Seiten der Rubrik liegen als
    /// MediaWiki-Markup in <c>Allgemein/Hilfe/Berechnung/*.wiki</c> und kommen
    /// über ein GLOB in die Assembly (<c>EPOS.Kern.csproj</c>, LogicalName
    /// <c>EPOS.Kern.Hilfe.Berechnung.&lt;Datei&gt;</c>). Ein Glob ist bequem und
    /// still: Verrutscht der Ordner, ändert jemand den LogicalName oder legt eine
    /// Seite mit falscher Endung an, fällt das beim Übersetzen NICHT auf — die
    /// Ressource fehlt dann einfach, und jeder Leser bekommt <c>null</c>.</para>
    ///
    /// <para><b>Was hier gehalten wird.</b> Zu jeder Datei auf dem Datenträger gibt
    /// es eine Ressource mit genau diesem Namen, und ihr Inhalt ist Zeichen für
    /// Zeichen derselbe. Der Fall setzt KEINEN Lader voraus — er fragt die
    /// Assembly unmittelbar; ein Lader kann darauf aufbauen, muss es aber nicht.</para>
    ///
    /// <para>Die INHALTLICHE Prüfung der Seiten (Kopfblock, die sieben Abschnitte
    /// der Bauform, die Notation, die Zuordnungen in <c>help_mapping.txt</c>) steht
    /// in <c>EPOS.UI.Tests/BerechnungshilfeTests</c> — dort, wo auch die Razor-Wirte
    /// der Infoknöpfe liegen.</para>
    ///
    /// <para><b>Fassung 2 (06.09.2026).</b> Die Seiten setzen ihre Formeln in
    /// UNICODE-Notation (η, ϑ, ·, Σ, √, ≤) — das Wiki führt keine
    /// Math-Erweiterung. Diese Zeichen müssen die Einbettung überstehen: Eine
    /// Ressource, die als ASCII oder in einer Codepage herauskäme, wäre für den
    /// KI-Assistenten Zeichensalat. Der letzte Fall hält genau das.</para>
    ///
    /// <para>Keine Datenbank, keine <c>Dienste.*</c> — deshalb ohne Sammlung.</para>
    /// </summary>
    public class BerechnungshilfeEinbettungTests
    {
        /// <summary>Der Namensraum, unter dem das Glob die Seiten einbettet.</summary>
        private const string PRAEFIX = "EPOS.Kern.Hilfe.Berechnung.";

        /// <summary>
        /// Kleinste Zahl von Seiten, unter der der Leser als kaputt gilt. Teil B
        /// der Rubrik liefert sieben (Heizkessel, BHKW, Wärmepumpe,
        /// Pufferspeicher, Solarthermie, Photovoltaik, Stromspeicher).
        /// </summary>
        private const int MINDESTSEITEN = 7;

        [Fact]
        public void Der_Ordner_der_Rubrik_traegt_Seiten()
        {
            string[] dateien = Seitendateien();

            Assert.True(dateien.Length >= MINDESTSEITEN,
                        "Nur " + dateien.Length + " Seiten in " + Seitenordner() +
                        " gefunden (erwartet: mindestens " + MINDESTSEITEN + ").");
        }

        /// <summary>
        /// Jede Datei des Ordners steht als eingebettete Ressource in der Assembly —
        /// unter genau dem Namen, den das Glob verspricht.
        /// </summary>
        [Fact]
        public void Jede_Seite_steht_als_eingebettete_Ressource()
        {
            Assembly kern = typeof(WindowsFormsApplication1.SimulationPV).Assembly;
            string[] namen = kern.GetManifestResourceNames();

            var fehlend = Seitendateien()
                .Select(Path.GetFileName)
                .Where(d => !namen.Contains(PRAEFIX + d))
                .ToArray();

            Assert.True(fehlend.Length == 0,
                        "Diese Seiten sind nicht eingebettet:\n  " +
                        string.Join("\n  ", fehlend) +
                        "\nVorhanden sind:\n  " +
                        string.Join("\n  ", namen.Where(n => n.StartsWith(PRAEFIX, StringComparison.Ordinal))));
        }

        /// <summary>
        /// Der eingebettete Inhalt ist derselbe wie auf dem Datenträger. Sonst
        /// liefe ein Leser gegen einen Stand, den niemand mehr sieht.
        /// </summary>
        [Fact]
        public void Der_eingebettete_Inhalt_ist_der_Dateiinhalt()
        {
            Assembly kern = typeof(WindowsFormsApplication1.SimulationPV).Assembly;

            foreach (string datei in Seitendateien())
            {
                string name = PRAEFIX + Path.GetFileName(datei);

                using Stream strom = kern.GetManifestResourceStream(name);
                Assert.True(strom != null, "Die Ressource " + name + " gibt es nicht.");

                using var leser = new StreamReader(strom, new UTF8Encoding(false), true);
                string eingebettet = leser.ReadToEnd().Replace("\r\n", "\n");
                string aufPlatte = File.ReadAllText(datei).Replace("\r\n", "\n");

                Assert.Equal(aufPlatte, eingebettet);
            }
        }

        /// <summary>
        /// Die Formelzeichen überstehen die Einbettung. Gelesen wird aus der
        /// ASSEMBLY, nicht von der Platte: Was der KI-Assistent und der Hilfeleser
        /// sehen, ist die Ressource, nicht die Datei.
        ///
        /// <para>Geprüft werden zwei Zeichen, die auf JEDER der sieben Seiten
        /// vorkommen — der Malpunkt (keine Formel dieser Rubrik kommt ohne
        /// Multiplikation aus) und das typografische Minus —, dazu MINDESTENS EIN
        /// griechischer oder mathematischer Buchstabe aus der Schreibweise der
        /// Rubrik. Ein bestimmter griechischer Buchstabe taugt dafür nicht: Die
        /// Wärmepumpe rechnet mit COP statt mit η, der Pufferspeicher mit λ.
        /// Kommt eines dieser Zeichen nicht an, ist die Kodierung unterwegs
        /// verlorengegangen.</para>
        ///
        /// <para>Gleichzeitig hält der Fall die zwei Verbote der Fassung 2 auf dem
        /// Weg, auf dem die Seiten wirklich ausgeliefert werden: keine
        /// <c>&lt;math&gt;</c>-Auszeichnung, kein LaTeX-Befehl.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(SeitenDerRubrik))]
        public void Die_Formelzeichen_ueberstehen_die_Einbettung(string seite)
        {
            Assembly kern = typeof(WindowsFormsApplication1.SimulationPV).Assembly;
            string name = PRAEFIX + seite + ".wiki";

            using Stream strom = kern.GetManifestResourceStream(name);
            Assert.True(strom != null, "Die Ressource " + name + " gibt es nicht.");

            using var leser = new StreamReader(strom, new UTF8Encoding(false), true);
            string inhalt = leser.ReadToEnd();

            foreach (string zeichen in ZEICHEN_DER_NOTATION)
                Assert.True(inhalt.Contains(zeichen, StringComparison.Ordinal),
                            "Die eingebettete Seite " + seite + " führt das Zeichen '" +
                            zeichen + "' nicht — entweder fehlt die Notation, oder die " +
                            "Kodierung ist unterwegs verlorengegangen.");

            Assert.True(GRIECHISCH.Any(z => inhalt.Contains(z, StringComparison.Ordinal)),
                        "Die eingebettete Seite " + seite + " führt keinen einzigen " +
                        "griechischen oder mathematischen Buchstaben (" +
                        string.Join(" ", GRIECHISCH) + ") — entweder fehlt die Notation, " +
                        "oder die Kodierung ist unterwegs verlorengegangen.");

            Assert.DoesNotContain("<math", inhalt, StringComparison.Ordinal);
            Assert.DoesNotContain("\\frac", inhalt, StringComparison.Ordinal);
            Assert.DoesNotContain("\\sum", inhalt, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Lesen
        // =====================================================================

        /// <summary>
        /// Alle dreizehn Seiten der Rubrik — seit der Zusammenführung von Teil A
        /// und Teil B (06.09.2026) hält dieser Wächter die Notation jeder Seite. Sie
        /// stehen AUSDRÜCKLICH da und nicht als Verzeichnisinhalt.
        /// </summary>
        public static TheoryData<string> SeitenDerRubrik
        {
            get
            {
                var daten = new TheoryData<string>();
                foreach (string s in new[]
                         {
                             "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme",
                             "Strombedarf", "Wärmequelle Erdreich", "Heizkessel", "BHKW", "Wärmepumpe",
                             "Pufferspeicher", "Solarthermie", "Photovoltaik", "Stromspeicher"
                         })
                    daten.Add(s);
                return daten;
            }
        }

        /// <summary>
        /// Das Zeichen, das auf JEDER Seite steht: der Malpunkt (U+00B7). Das
        /// typografische Minus (U+2212) steht nur, wo die Seite subtrahiert —
        /// Prozesswärme und Strombedarf tun das nicht (Zusammenführung 06.09.2026).
        /// </summary>
        private static readonly string[] ZEICHEN_DER_NOTATION = { "·" };

        /// <summary>
        /// Davon steht mindestens EINES auf jeder Seite — welches, hängt vom Fach ab:
        /// η beim Erzeuger mit Wirkungsgrad, ϑ bei jeder Temperatur, λ beim
        /// Schichtmodell, Σ bei jeder Summe.
        /// </summary>
        private static readonly string[] GRIECHISCH =
            { "η", "ϑ", "λ", "β", "γ", "ρ", "κ", "θ", "χ", "Δ", "Σ", "√" };

        /// <summary>
        /// Die Seiten des Ordners. Dateien mit führendem <c>_</c> sind KEINE
        /// Wikiseiten (Bauform: <c>_Index.wiki</c>, <c>_Bezuege.wiki</c>) und
        /// bleiben hier trotzdem drin — eingebettet müssen sie ebenso sein.
        /// </summary>
        private static string[] Seitendateien()
        {
            string ordner = Seitenordner();
            if (!Directory.Exists(ordner)) return Array.Empty<string>();

            return Directory.GetFiles(ordner, "*.wiki", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => p, StringComparer.Ordinal)
                            .ToArray();
        }

        private static string Seitenordner()
            => Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung");

        /// <summary>
        /// Der Aufstieg zur Repowurzel — dasselbe Vorgehen wie in
        /// <c>DiensteSammlungTests.OrdnerAusDemArbeitsbaum</c>: vom Ausgabeordner
        /// aufwärts, bis eine bekannte Datei auftaucht.
        /// </summary>
        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null &&
                   !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;

            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
