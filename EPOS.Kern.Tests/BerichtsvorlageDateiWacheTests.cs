using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache über die beiden Word-Vorlagen im Repository</b> (Konzept Berichtsvorlagen,
    /// Etappe BV-E0, 6.2, 6.3, Anhang B.3): die Stilvorlage des Generators
    /// <c>Berichtsvorlage.docx</c> und die Beispielvorlage <c>Berichtsvorlage_Beispiel.docx</c>,
    /// beide erzeugt mit <c>Werkzeuge/Berichtsvorlage</c>.
    ///
    /// <para><b>Warum eine Wache.</b> Beide Dateien sind Binärdateien, die niemand im Diff
    /// liest. Eine in Word gespeicherte Fassung kann still doppelte Stil-IDs, übersetzte
    /// Stilnamen oder zerlegte Platzhalter mitbringen — Word zeigt dann alles richtig an, der
    /// Validator aber meldet die Datei, und die Engine fände einen Platzhalter nicht mehr, der
    /// über zwei Runs verteilt ist. Die Regeln hier sind dieselben, die das Werkzeug vor dem
    /// Schreiben zieht.</para>
    /// </summary>
    public class BerichtsvorlageDateiWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const string STANDARD = "Berichtsvorlage.docx";
        private const string BEISPIEL = "Berichtsvorlage_Beispiel.docx";

        /// <summary>Die Stil-IDs, die der <c>WordBerichtGenerator</c> über <c>WordKontext.MitStil</c> anspricht.</summary>
        private static readonly string[] Pflichtstile =
        {
            "Title", "Subtitle", "Heading1", "Heading2", "Heading3", "Normal", "Hinweis", "Beschriftung"
        };

        private const string KAPITELKOPF_ID = "EPOSKapitelkopf";
        private const string KAPITELKOPF_NAME = "EPOS Kapitelkopf";

        /// <summary>Ein Platzhalter: Schlüssel, wahlweise eine Formatangabe nach „|“.</summary>
        private static readonly Regex Platzhaltermuster = new Regex(@"\{\{[a-z_.]+(\|[a-z ]+)?\}\}", RegexOptions.CultureInvariant);

        private static readonly Regex Kapitelmuster = new Regex(@"^\{\{kapitel\.([a-z_]+)(\|[a-z ]+)?\}\}$", RegexOptions.CultureInvariant);

        /// <summary>Die Platzhalter des Rumpfs in Dokumentfolge (Anhang B.3).</summary>
        private static readonly string[] RumpfErwartet =
        {
            // Deckblatt
            "{{bericht.titel}}", "{{bericht.untertitel}}",
            "{{text.kunde}}", "{{projekt.kunde}}",
            "{{text.bearbeiter}}", "{{projekt.bearbeiter}}",
            "{{text.ersteller}}", "{{ersteller.firma}}",
            "{{text.varianten}}", "{{bericht.varianten.liste}}",
            "{{text.datum}}", "{{bericht.datum}}",
            "{{bericht.gebaeudemodell.ausweis|leer statt strich}}",
            "{{text.erstellt_mit}}", "{{ersteller.programm}}", "{{ersteller.version}}",
            // Inhaltsverzeichnis und Kapitel
            "{{kapitel.inhalt}}",
            "{{kapitel.projekt|ohne titel}}", "{{kapitel.komponenten|ohne titel}}", "{{kapitel.ergebnisse|ohne titel}}",
            "{{kapitel.vergleich|ohne titel}}", "{{kapitel.wirtschaftlichkeit|ohne titel}}",
            "{{kapitel.anhang|ohne titel}}", "{{kapitel.anhang_e|ohne titel}}",
        };

        private static readonly string[] KopfzeileErwartet = { "{{ersteller.programm}}" };

        /// <summary>Links die Firma, in der Mitte das Datum (an der Stelle des früheren DATE-Felds), rechts die Seite.</summary>
        private static readonly string[] FusszeileErwartet = { "{{ersteller.firma}}", "{{bericht.datum}}", "{{text.seite}}" };

        public static IEnumerable<object[]> Vorlagen()
        {
            yield return new object[] { STANDARD };
            yield return new object[] { BEISPIEL };
        }

        // =====================================================================
        //  Beide Vorlagen: Stile und Validator
        // =====================================================================

        [Theory]
        [MemberData(nameof(Vorlagen))]
        public void Keine_Stil_ID_ist_doppelt(string datei)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;

            List<string> doppelt = Stile(doc).Elements<Style>()
                .Where(s => s.StyleId?.Value != null)
                .GroupBy(s => s.StyleId.Value, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key + " ×" + g.Count())
                .ToList();
            Assert.True(doppelt.Count == 0, datei + ": doppelte Stil-IDs " + string.Join(", ", doppelt));
        }

        /// <summary>
        /// Die acht Stile des Generators sind da, Überschrift 1–3 tragen eine Gliederungsebene
        /// (sonst bliebe das Inhaltsverzeichnis in jedem Leser außer Word leer), und das
        /// Absatzformat „EPOS Kapitelkopf“ steht so bereit, wie Anhang B.3 es verlangt.
        /// </summary>
        [Theory]
        [MemberData(nameof(Vorlagen))]
        public void Die_Pflichtstile_und_der_Kapitelkopf_sind_da(string datei)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;
            Styles stile = Stile(doc);

            foreach (string id in Pflichtstile)
                Assert.True(Stil(stile, id) != null, datei + ": Stil „" + id + "“ fehlt.");

            Assert.Equal(0, Stil(stile, "Heading1").StyleParagraphProperties?.OutlineLevel?.Val?.Value);
            Assert.Equal(1, Stil(stile, "Heading2").StyleParagraphProperties?.OutlineLevel?.Val?.Value);
            Assert.Equal(2, Stil(stile, "Heading3").StyleParagraphProperties?.OutlineLevel?.Val?.Value);

            Style kopf = Stil(stile, KAPITELKOPF_ID);
            Assert.True(kopf != null, datei + ": Absatzformat „" + KAPITELKOPF_NAME + "“ fehlt.");
            Assert.Equal(KAPITELKOPF_NAME, kopf.StyleName?.Val?.Value);
            Assert.Equal("Heading1", kopf.BasedOn?.Val?.Value);
            Assert.Equal("Normal", kopf.NextParagraphStyle?.Val?.Value);
            Assert.Equal(0, kopf.StyleParagraphProperties?.OutlineLevel?.Val?.Value);
        }

        /// <summary>Gültiges OpenXML in jeder Fassung von Office 2007 bis 2021 (wie <c>WordBerichtSvgWacheTests</c>).</summary>
        [Theory]
        [MemberData(nameof(Vorlagen))]
        public void Der_Validator_meldet_in_keiner_Fassung_einen_Fehler(string datei)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;

            foreach (FileFormatVersions fassung in Fassungen)
            {
                List<ValidationErrorInfo> fehler = new OpenXmlValidator(fassung).Validate(doc).ToList();
                Assert.True(fehler.Count == 0,
                    datei + ", " + fassung + ": " + fehler.Count + " Fehler — " + string.Join(" | ",
                        fehler.Take(5).Select(f => f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath)));
            }
        }

        // =====================================================================
        //  Beispielvorlage: Platzhalter, Runs, Kapitel, Firmenname
        // =====================================================================

        [Fact]
        public void Die_Beispielvorlage_fuehrt_genau_die_erwarteten_Platzhalter()
        {
            using WordprocessingDocument doc = Oeffnen(BEISPIEL);
            if (doc == null) return;
            MainDocumentPart main = doc.MainDocumentPart;

            Assert.Equal(RumpfErwartet, Platzhalter(main.Document.Body).ToArray());
            Assert.Equal(KopfzeileErwartet, main.HeaderParts.SelectMany(h => Platzhalter(h.Header)).ToArray());
            Assert.Equal(FusszeileErwartet, main.FooterParts.SelectMany(f => Platzhalter(f.Footer)).ToArray());
        }

        /// <summary>
        /// Jeder Platzhalter steht ungeteilt und allein in einem Run, und der Run trägt
        /// <c>w:noProof</c> — sonst fände die Engine ihn nur über den Normalisierer, und Word
        /// unterstriche ihn als Rechtschreibfehler.
        /// </summary>
        [Fact]
        public void Jeder_Platzhalter_steht_allein_in_einem_Run_mit_noProof()
        {
            using WordprocessingDocument doc = Oeffnen(BEISPIEL);
            if (doc == null) return;

            foreach ((string teil, OpenXmlElement wurzel) in Teile(doc))
            {
                int imText = Platzhalter(wurzel).Count;
                int inRuns = 0;
                foreach (Run r in wurzel.Descendants<Run>())
                {
                    string text = string.Concat(r.Elements<Text>().Select(t => t.Text));
                    if (!Platzhaltermuster.IsMatch(text)) continue;
                    inRuns++;
                    Assert.True(Platzhaltermuster.Match(text).Value == text,
                        teil + ": Run „" + text + "“ trägt neben dem Platzhalter weiteren Text.");
                    NoProof aus = r.RunProperties?.NoProof;
                    Assert.True(aus != null && (aus.Val == null || aus.Val.Value),
                        teil + ": Platzhalter „" + text + "“ ohne w:noProof.");
                }
                Assert.True(imText == inRuns,
                    teil + ": " + imText + " Platzhalter im Text, aber nur " + inRuns + " ungeteilt in einem Run.");
            }
        }

        /// <summary>
        /// Jeder Kapitelplatzhalter mit <c>|ohne titel</c> steht unmittelbar unter einer
        /// Überschrift im Format „EPOS Kapitelkopf“, jede solche Überschrift über einem
        /// Kapitelplatzhalter; die Überschriften sind die, die der Bericht heute druckt, und die
        /// Kapitel stehen in der Folge des heutigen Berichts
        /// (<see cref="WordBerichtGenerator.AktiveBausteine"/>).
        /// </summary>
        [Fact]
        public void Jede_Kapitelueberschrift_traegt_den_Kapitelkopf_in_heutiger_Folge()
        {
            using WordprocessingDocument doc = Oeffnen(BEISPIEL);
            if (doc == null) return;

            List<Paragraph> absaetze = doc.MainDocumentPart.Document.Body.Elements<Paragraph>().ToList();
            var koepfe = new List<string>();
            var kapitel = new List<string>();
            for (int i = 0; i < absaetze.Count; i++)
            {
                string text = Absatztext(absaetze[i]);
                bool istKopf = Stilkennung(absaetze[i]) == KAPITELKOPF_ID;
                Match m = Kapitelmuster.Match(text);
                if (m.Success) kapitel.Add(m.Groups[1].Value);

                if (text.EndsWith("|ohne titel}}", StringComparison.Ordinal))
                {
                    Assert.True(i > 0 && Stilkennung(absaetze[i - 1]) == KAPITELKOPF_ID,
                        "„" + text + "“ steht nicht unter einer Überschrift im Format „" + KAPITELKOPF_NAME + "“.");
                    koepfe.Add(Absatztext(absaetze[i - 1]));
                }
                if (istKopf)
                    Assert.True(i + 1 < absaetze.Count && Absatztext(absaetze[i + 1]).EndsWith("|ohne titel}}", StringComparison.Ordinal),
                        "Kapitelkopf „" + text + "“ steht über keinem Kapitelplatzhalter.");
            }

            Assert.Equal(
                new[]
                {
                    "Projektbeschreibung",
                    "Komponenten & Varianten",
                    "Berechnungsergebnisse je Variante",
                    "Variantenvergleich",
                    "Wirtschaftlichkeit",
                    "Anhang",
                    new AnhangEChecklisteBaustein().Titel,
                },
                koepfe.ToArray());

            List<string> heute = WordBerichtGenerator.AktiveBausteine(null)
                .Where(b => b.Schluessel != BerichtsKonfiguration.B_DECKBLATT)
                .Select(Kapitelname)
                .ToList();
            Assert.Equal(heute, kapitel);
        }

        /// <summary>
        /// Der Name des Herstellers steht nirgends mehr im Paket (BV-Q8: der Bericht nennt den
        /// Ersteller) — weder in Rumpf, Kopf- und Fußzeile noch in Kommentaren oder Eigenschaften.
        /// </summary>
        [Fact]
        public void Die_Beispielvorlage_nennt_INEKON_GmbH_nicht_mehr()
        {
            using WordprocessingDocument doc = Oeffnen(BEISPIEL);
            if (doc == null) return;

            foreach ((string teil, OpenXmlElement wurzel) in Teile(doc))
                Assert.DoesNotContain("INEKON GmbH", wurzel.InnerText, StringComparison.Ordinal);

            foreach (OpenXmlPart teil in doc.GetAllParts().Where(p => p.ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase)))
            {
                using var leser = new StreamReader(teil.GetStream(FileMode.Open, FileAccess.Read), Encoding.UTF8);
                Assert.True(!leser.ReadToEnd().Contains("INEKON GmbH", StringComparison.Ordinal),
                    "„INEKON GmbH“ steht noch in " + teil.Uri);
            }

            string eigenschaften = string.Join(" | ", doc.PackageProperties.Creator, doc.PackageProperties.Title,
                doc.PackageProperties.Description, doc.PackageProperties.LastModifiedBy, doc.PackageProperties.Subject);
            Assert.DoesNotContain("INEKON GmbH", eigenschaften, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Office 2007 bis 2021 — jede Fassung, die der Validator kennt.</summary>
        private static readonly FileFormatVersions[] Fassungen =
        {
            FileFormatVersions.Office2007, FileFormatVersions.Office2010,
            FileFormatVersions.Office2013, FileFormatVersions.Office2016,
            FileFormatVersions.Office2019, FileFormatVersions.Office2021
        };

        /// <summary>Öffnet eine Vorlage aus dem Repository lesend; <c>null</c> außerhalb des Repositoriums.</summary>
        private static WordprocessingDocument Oeffnen(string datei)
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Bericht", "Vorlagen", datei);
            Assert.True(File.Exists(pfad), "Vorlage fehlt: " + pfad);
            return WordprocessingDocument.Open(pfad, false);
        }

        private static Styles Stile(WordprocessingDocument doc)
        {
            Styles stile = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
            Assert.NotNull(stile);
            return stile;
        }

        private static Style Stil(Styles stile, string id)
            => stile.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == id);

        private static IEnumerable<(string Teil, OpenXmlElement Wurzel)> Teile(WordprocessingDocument doc)
        {
            MainDocumentPart main = doc.MainDocumentPart;
            yield return ("Rumpf", main.Document.Body);
            foreach (HeaderPart h in main.HeaderParts) yield return ("Kopfzeile", h.Header);
            foreach (FooterPart f in main.FooterParts) yield return ("Fußzeile", f.Footer);
        }

        private static List<string> Platzhalter(OpenXmlElement wurzel)
            => wurzel.Descendants<Paragraph>()
                     .SelectMany(p => Platzhaltermuster.Matches(Absatztext(p)).Select(m => m.Value))
                     .ToList();

        private static string Absatztext(Paragraph p)
            => string.Concat(p.Descendants<Text>().Select(t => t.Text));

        private static string Stilkennung(Paragraph p)
            => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

        /// <summary>Der Kapitelname eines Bausteins, wie ihn <c>kapitel.&lt;name&gt;</c> führt (Konzept Anhang A).</summary>
        private static string Kapitelname(IBerichtsBaustein b)
        {
            if (b is AnhangEChecklisteBaustein) return "anhang_e";
            if (b.Schluessel == BerichtsKonfiguration.B_INHALT) return "inhalt";
            if (b.Schluessel == BerichtsKonfiguration.B_PROJEKT) return "projekt";
            return b.Schluessel;
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
