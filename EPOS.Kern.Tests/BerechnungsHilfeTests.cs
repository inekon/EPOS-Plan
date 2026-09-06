using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Hilferubrik „Berechnung" (H13, Anwenderwunsch vom 06.09.2026).
    ///
    /// <para><b>Was hier geprüft wird — und warum.</b> Die Rechenwegseiten sind Texte, und
    /// Texte altern still. Der Prüfstand hält deshalb genau die Zusagen fest, die die
    /// Bauform des Pakets macht: Jede Seite trägt einen vollständigen KOPFBLOCK mit
    /// Seitenname, Stand und den Kerndateien, gegen die sie belegt ist, und jede Seite hat
    /// dieselben SECHS Abschnitte. Wer eine Seite ergänzt und den Kopf vergisst, sieht es
    /// hier und nicht erst im Wiki.</para>
    ///
    /// <para>Dazu die zwei Anschlüsse: Dateien mit führendem Unterstrich sind KEINE
    /// Wikiseiten (<c>_Index.wiki</c> ist die Rubrik-Startseite, <c>_Bezuege.wiki</c> die
    /// Arbeitsvorlage für den Anwender), und jede echte Seite erreicht das Wissen des
    /// Assistenten als eigener Abschnitt.</para>
    ///
    /// <para>Ohne Datenbank, ohne Oberfläche, ohne <c>Dienste.*</c>-Tausch — deshalb keine
    /// Sammlungsangabe.</para>
    /// </summary>
    public class BerechnungsHilfeTests
    {
        /// <summary>
        /// Die sechs Abschnitte, die JEDE Seite der Rubrik führt (Bauform des Pakets).
        /// Geprüft wird die Überschriftszeile im MediaWiki-Markup.
        /// </summary>
        private static readonly string[] Abschnitte =
        {
            "== Was berechnet wird ==",
            "== Eingangsgrößen ==",
            "== Rechenweg ==",
            "== Grenzen und Annahmen ==",
            "== Ergebnisse und wo sie stehen ==",
            "== Bezüge =="
        };

        // =====================================================================
        //  Bestand
        // =====================================================================

        /// <summary>
        /// Die Rubrik ist eingebettet und lesbar. Ein leerer Bestand liefe sonst durch
        /// jeden folgenden Fall grün hindurch, ohne je etwas geprüft zu haben — die
        /// <c>.wiki</c>-Dateien hängen an einem <c>EmbeddedResource</c>-Muster in
        /// <c>EPOS.Kern.csproj</c>, und ein Tippfehler im <c>LogicalName</c> fällt genau
        /// hier auf.
        /// </summary>
        [Fact]
        public void Die_Rubrik_ist_eingebettet_und_traegt_Seiten()
        {
            IReadOnlyList<BerechnungsSeite> seiten = BerechnungsHilfe.Seiten;

            Assert.NotNull(seiten);
            Assert.True(seiten.Count >= 1,
                "Die Rubrik 'Berechnung' führt keine einzige Seite. Ist das EmbeddedResource-Muster " +
                "'Allgemein\\Hilfe\\Berechnung\\*.wiki' mit LogicalName '" +
                BerechnungsHilfe.RESSOURCE_VORSATZ + "%(Filename)%(Extension)' noch in EPOS.Kern.csproj?");
        }

        /// <summary>
        /// Kein Seitenname doppelt — zwei Seiten gleichen Namens wären im Wiki eine.
        /// </summary>
        [Fact]
        public void Jeder_Seitenname_kommt_genau_einmal_vor()
        {
            var doppelt = BerechnungsHilfe.Seiten
                .GroupBy(s => s.Seitenname, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.True(doppelt.Count == 0, "Doppelte Seitennamen: " + string.Join(", ", doppelt));
        }

        // =====================================================================
        //  Kopfblock
        // =====================================================================

        /// <summary>
        /// Der Kopfblock ist die Beleglage: Er nennt die Seite, den Stand und die Dateien
        /// des Rechenkerns, gegen die der Text geschrieben wurde. Fehlt eine der drei
        /// Angaben, ist der Text nicht mehr nachprüfbar.
        /// </summary>
        [Fact]
        public void Jede_Seite_traegt_einen_vollstaendigen_Kopfblock()
        {
            var funde = new List<string>();

            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                if (string.IsNullOrWhiteSpace(seite.Seitenname)) funde.Add("(ohne Namen): Feld 'Seite' fehlt");
                if (string.IsNullOrWhiteSpace(seite.Stand)) funde.Add(seite.Seitenname + ": Feld 'Stand' fehlt");
                if (string.IsNullOrWhiteSpace(seite.Rechenkern))
                    funde.Add(seite.Seitenname + ": Feld 'Rechenkern' fehlt");
            }

            Assert.True(funde.Count == 0,
                "Diese Seiten der Rubrik 'Berechnung' haben keinen vollständigen Kopfblock " +
                "(Muster: <!-- EPOS-Plan Hilferubrik Berechnung | Seite: … | Stand: JJJJ-MM-TT | " +
                "Rechenkern: … -->):\n" + string.Join("\n", funde));
        }

        /// <summary>Der Stand hat die Form <c>JJJJ-MM-TT</c> — sonst ist er nicht sortierbar.</summary>
        [Fact]
        public void Der_Stand_ist_ein_Datum()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.True(DateTime.TryParseExact(seite.Stand, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out _),
                    seite.Seitenname + ": '" + seite.Stand + "' ist kein Datum der Form JJJJ-MM-TT.");
            }
        }

        /// <summary>
        /// Der Kopfblock nennt Dateien des Rechenkerns, nicht irgendetwas. Geprüft wird
        /// die Form: mindestens ein Pfad, der mit <c>EPOS.Kern/</c> beginnt.
        /// </summary>
        [Fact]
        public void Der_Kopfblock_nennt_Dateien_des_Rechenkerns()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.True(seite.Rechenkern.IndexOf("EPOS.Kern/", StringComparison.Ordinal) >= 0,
                    seite.Seitenname + ": Der Kopfblock nennt keine Datei unter EPOS.Kern/ — " +
                    "gefunden: '" + seite.Rechenkern + "'.");
            }
        }

        // =====================================================================
        //  Gliederung
        // =====================================================================

        /// <summary>
        /// Dieselbe Gliederung auf jeder Seite — das ist die Zusage an den Leser: Er weiß
        /// immer, wo die Vorgabewerte stehen und wo die Grenzen.
        /// </summary>
        [Fact]
        public void Jede_Seite_hat_die_sechs_Abschnitte()
        {
            var funde = new List<string>();

            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
                foreach (string abschnitt in Abschnitte)
                    if (seite.Markup.IndexOf(abschnitt, StringComparison.Ordinal) < 0)
                        funde.Add(seite.Seitenname + ": '" + abschnitt + "' fehlt");

            Assert.True(funde.Count == 0,
                "Diese Abschnitte fehlen (Bauform H13: sechs Abschnitte je Seite):\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// Der Abschnitt „Bezüge" verweist wirklich — mindestens ein Wikilink. Ohne ihn
        /// wäre die Rubrik eine Sackgasse, und genau das wollte der Anwenderwunsch nicht
        /// („aufrufbar aus den allgemeinen Erklärungen mit Bezügen").
        /// </summary>
        [Fact]
        public void Jede_Seite_verweist_auf_die_allgemeine_Dokumentation()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.True(seite.Markup.IndexOf("[[Programm Dokumentation", StringComparison.Ordinal) >= 0,
                    seite.Seitenname + ": kein Verweis auf die Rubrik 'Programm Dokumentation'.");
            }
        }

        // =====================================================================
        //  Beiwerk
        // =====================================================================

        /// <summary>
        /// Dateien mit führendem Unterstrich sind KEINE Wikiseiten: <c>_Index.wiki</c> ist
        /// die Rubrik-Startseite, <c>_Bezuege.wiki</c> die Vorlage der Abschnitte, die der
        /// Anwender in die allgemeinen Seiten einfügt. Kämen sie als Seiten durch, stünden
        /// sie im Wissen des Assistenten und in jeder Seitenliste.
        /// </summary>
        [Fact]
        public void Dateien_mit_Unterstrich_sind_keine_Seiten()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
                Assert.False(seite.Seitenname.StartsWith("_", StringComparison.Ordinal),
                    "'" + seite.Seitenname + "' beginnt mit einem Unterstrich und ist keine Wikiseite.");

            // Gegenprobe: Das Beiwerk ist wirklich vorhanden - sonst prüfte der Fall nichts.
            string[] ressourcen = typeof(BerechnungsHilfe).Assembly.GetManifestResourceNames();
            Assert.Contains(ressourcen,
                r => r.StartsWith(BerechnungsHilfe.RESSOURCE_VORSATZ + "_", StringComparison.Ordinal));
        }

        // =====================================================================
        //  Klartext
        // =====================================================================

        /// <summary>
        /// Der Klartext ist das, was der Assistent liest. Der Kopfblock gehört nicht in
        /// den Prompt (er nennt Quelltextpfade), die Auszeichnung auch nicht — jede Zahl
        /// dagegen schon.
        /// </summary>
        [Fact]
        public void Der_Klartext_traegt_keinen_Kopfblock_und_keine_Auszeichnung()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.False(string.IsNullOrWhiteSpace(seite.Klartext), seite.Seitenname + ": Klartext leer.");
                Assert.DoesNotContain("<!--", seite.Klartext);
                Assert.DoesNotContain("EPOS.Kern/", seite.Klartext);
                Assert.DoesNotContain("== ", seite.Klartext);
                Assert.DoesNotContain("'''", seite.Klartext);
                Assert.DoesNotContain("[[", seite.Klartext);
            }
        }

        /// <summary>
        /// <b>Gegenprobe zum Klartext:</b> Die Umwandlung nimmt die Auszeichnung, nicht den
        /// Inhalt. Geprüft an einem Ausschnitt, der jede Form enthält, die in den Seiten
        /// vorkommt — Überschrift, Liste, Tabelle, Verweis, Fettsatz, Formelzeile.
        /// </summary>
        [Fact]
        public void Der_Klartext_behaelt_Woerter_Zahlen_und_Formeln()
        {
            const string markup =
                "<!-- EPOS-Plan Hilferubrik Berechnung | Seite: Probe | Stand: 2026-09-06 | Rechenkern: EPOS.Kern/X.cs -->\n" +
                "== Rechenweg ==\n" +
                "Der '''Vorgabewert''' ist 0,95.\n" +
                "* Erster Punkt\n" +
                "{| class=\"wikitable\"\n" +
                "! Größe !! Einheit\n" +
                "|-\n" +
                "| Leistung || kW\n" +
                "|}\n" +
                " P_AC = min(P_DC, P_nenn)\n" +
                "Siehe [[Programm Dokumentation/Photovoltaik|Photovoltaik]].\n";

            string klartext = BerechnungsHilfe.AlsKlartext(markup);

            Assert.Contains("Rechenweg", klartext);
            Assert.Contains("Vorgabewert", klartext);
            Assert.Contains("0,95", klartext);
            Assert.Contains("Erster Punkt", klartext);
            Assert.Contains("Größe | Einheit", klartext);
            Assert.Contains("Leistung | kW", klartext);
            Assert.Contains("P_AC = min(P_DC, P_nenn)", klartext);
            Assert.Contains("Photovoltaik", klartext);

            Assert.DoesNotContain("<!--", klartext);
            Assert.DoesNotContain("'''", klartext);
            Assert.DoesNotContain("[[", klartext);
            Assert.DoesNotContain("{|", klartext);
        }

        /// <summary>
        /// <b>Gegenprobe zur Tag-Entfernung:</b> Ein Vergleichszeichen in einer Formelzeile
        /// ist kein HTML. Ein weit gefasstes Muster („alles zwischen &lt; und &gt;") machte
        /// aus „P &lt; 0 und Q &gt; 1" das sinnlose „P 1" — genau das darf nicht passieren.
        /// </summary>
        [Fact]
        public void Vergleichszeichen_in_einer_Formel_bleiben_stehen()
        {
            string klartext = BerechnungsHilfe.AlsKlartext(" wenn P < 0 und Q > 1 dann Rest = 0\n");

            Assert.Contains("P < 0 und Q > 1", klartext);
        }

        // =====================================================================
        //  Kopfblockleser
        // =====================================================================

        /// <summary>
        /// <b>Gegenprobe zum Kopfblockleser:</b> Er liest die drei Felder auch aus einem
        /// über zwei Zeilen umbrochenen Kommentar — so stehen sie in den Dateien.
        /// </summary>
        [Fact]
        public void Der_Kopfblockleser_versteht_den_umbrochenen_Kommentar()
        {
            const string markup =
                "<!-- EPOS-Plan Hilferubrik Berechnung | Seite: Wärmequelle Erdreich | Stand: 2026-09-06 | Rechenkern:\n" +
                "     EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs, EPOS.Kern/Allgemein/Simulation/VDI4640Pruefung.cs -->\n" +
                "== Was berechnet wird ==\n";

            BerechnungsHilfe.Kopfblock(markup, out string seite, out string stand, out string kern);

            Assert.Equal("Wärmequelle Erdreich", seite);
            Assert.Equal("2026-09-06", stand);
            Assert.Contains("ErdreichTemperatur.cs", kern);
            Assert.Contains("VDI4640Pruefung.cs", kern);
        }

        /// <summary>Ohne Kopfblock bleiben alle drei Felder leer — kein Wurf, kein Raten.</summary>
        [Fact]
        public void Ohne_Kopfblock_bleiben_die_Felder_leer()
        {
            BerechnungsHilfe.Kopfblock("== Rechenweg ==\n", out string seite, out string stand, out string kern);

            Assert.Equal("", seite);
            Assert.Equal("", stand);
            Assert.Equal("", kern);
        }

        // =====================================================================
        //  Nachschlagen
        // =====================================================================

        /// <summary>
        /// Eine Seite ist über ihren Namen UND über das Mapping-Ziel erreichbar —
        /// <c>help_mapping.txt</c> trägt „Berechnung/&lt;Seite&gt;".
        /// </summary>
        [Fact]
        public void Eine_Seite_ist_ueber_Namen_und_Ziel_erreichbar()
        {
            BerechnungsSeite erste = BerechnungsHilfe.Seiten[0];

            Assert.Same(erste, BerechnungsHilfe.Seite(erste.Seitenname));
            Assert.Same(erste, BerechnungsHilfe.Seite(erste.Seitenname.ToLowerInvariant()));
            Assert.Same(erste, BerechnungsHilfe.Seite(erste.Ziel));
            Assert.Null(BerechnungsHilfe.Seite("gibt es nicht"));
            Assert.Null(BerechnungsHilfe.Seite(""));
            Assert.Null(BerechnungsHilfe.Seite(null));
        }

        /// <summary>Titel, Ziel und Wikititel folgen der Rubrik — sie sind die Adressen der Seite.</summary>
        [Fact]
        public void Titel_Ziel_und_Wikititel_folgen_der_Rubrik()
        {
            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                Assert.Equal("Berechnung: " + seite.Seitenname, seite.Titel);
                Assert.Equal("Berechnung/" + seite.Seitenname, seite.Ziel);
                Assert.Equal("Programm Dokumentation/Berechnung/" + seite.Seitenname, seite.WikiTitel);
            }
        }

        // =====================================================================
        //  Anschluss an das Wissen des Assistenten
        // =====================================================================

        /// <summary>
        /// Der Assistent kann „Wie wird die Photovoltaik berechnet?" beantworten, weil je
        /// Seite ein Wissensabschnitt im eingebauten Wissen steht — auch ohne Netz und
        /// bevor die Seiten im Wiki angelegt sind.
        /// </summary>
        [Fact]
        public void Jede_Seite_steht_als_Wissensabschnitt_im_Assistenten()
        {
            List<WissensAbschnitt> abschnitte = HilfeWissen.Abschnitte;

            foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
            {
                WissensAbschnitt treffer = abschnitte.FirstOrDefault(
                    a => string.Equals(a.Titel, seite.Titel, StringComparison.Ordinal));

                Assert.True(treffer != null,
                    "Zur Seite '" + seite.Seitenname + "' fehlt der Wissensabschnitt '" + seite.Titel + "'.");
                Assert.Equal(BerechnungsHilfe.BEREICH, treffer.Bereich);
                Assert.Equal(seite.Klartext, treffer.Inhalt);
            }
        }

        /// <summary>
        /// Die Suche findet den Rechenweg zu einer Frage nach der Berechnung. Geprüft mit
        /// dem Seitennamen selbst — er zählt im Titel dreifach, der Bereich „Berechnung"
        /// doppelt. Gewichtung und Suche selbst sind von H13 unberührt.
        /// </summary>
        [Fact]
        public void Die_Suche_findet_den_Rechenweg()
        {
            BerechnungsSeite seite = BerechnungsHilfe.Seiten[0];

            List<WissensAbschnitt> treffer =
                HilfeWissen.Suchen("Berechnung " + seite.Seitenname, "", 4);

            Assert.Contains(treffer, a => string.Equals(a.Titel, seite.Titel, StringComparison.Ordinal));
        }
    }
}
