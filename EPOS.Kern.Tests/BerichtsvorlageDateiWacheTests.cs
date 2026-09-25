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
    /// <b>Die Wache über die drei Word-Vorlagen im Repository</b> (Konzept Berichtsvorlagen,
    /// Etappen BV-E0 und BV-E1, 6.2, 6.3, Anhang B.3): die Stilvorlage des Generators
    /// <c>Berichtsvorlage.docx</c>, die Standardvorlage <c>Berichtsvorlage_Standard.docx</c> im
    /// vollen Aufbau (BV-E2: Deckblatt aus Platzhaltern, Kapitel einzeln) und die Beispielvorlage
    /// <c>Berichtsvorlage_Beispiel.docx</c> im vollen Aufbau, alle drei erzeugt mit
    /// <c>Werkzeuge/Berichtsvorlage</c>. Welche davon ausgeliefert werden, hält
    /// <see cref="AuslieferungsvorlagenWacheTests"/>.
    ///
    /// <para><b>Warum eine Wache.</b> Die Dateien sind Binärdateien, die niemand im Diff
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

        /// <summary>Der Ordner der Vorlagen, repo-relativ.</summary>
        internal const string ORDNER_REPO = "WindowsFormsApplication1/Allgemein/Bericht/Vorlagen";

        /// <summary>Die Stilvorlage des heutigen Generators — Rückfall des Codes und Quelle der Bereinigung.</summary>
        internal const string STILVORLAGE = "Berichtsvorlage.docx";

        /// <summary>Die Standardvorlage im vollen Aufbau der Beispielvorlage (BV-E2).</summary>
        internal const string STANDARD = "Berichtsvorlage_Standard.docx";

        /// <summary>Die Beispielvorlage im vollen Aufbau (Anhang B.3) — Anschauung bis BV-E2.</summary>
        internal const string BEISPIEL = "Berichtsvorlage_Beispiel.docx";

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

        /// <summary>Die Platzhalter des Rumpfs der Beispielvorlage in Dokumentfolge (Anhang B.3).</summary>
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

        /// <summary>
        /// Der Rumpf der Standardvorlage im vollen Aufbau (BV-E2, Anhang B.3): Deckblatt aus Platzhaltern,
        /// Inhaltsverzeichnis und je Kapitel der Kapitelplatzhalter — ohne die Kapitelköpfe, die als Text oder
        /// als <c>{{text.kapitel_*}}</c> stehen dürfen.
        /// </summary>
        private static readonly string[] StandardRumpfErwartet =
        {
            "{{bericht.titel}}", "{{bericht.untertitel}}",
            "{{text.kunde}}", "{{projekt.kunde}}",
            "{{text.bearbeiter}}", "{{projekt.bearbeiter}}",
            "{{text.ersteller}}", "{{ersteller.firma}}",
            "{{text.varianten}}", "{{bericht.varianten.liste}}",
            "{{text.datum}}", "{{bericht.datum}}",
            "{{bericht.gebaeudemodell.ausweis|leer statt strich}}",
            "{{text.erstellt_mit}}", "{{ersteller.programm}}", "{{ersteller.version}}",
            "{{kapitel.inhalt}}",
            "{{kapitel.projekt|ohne titel}}", "{{kapitel.komponenten|ohne titel}}", "{{kapitel.ergebnisse|ohne titel}}",
            "{{kapitel.vergleich|ohne titel}}", "{{kapitel.wirtschaftlichkeit|ohne titel}}",
            "{{kapitel.anhang|ohne titel}}", "{{kapitel.anhang_e|ohne titel}}",
        };

        /// <summary>Kopfzeile beider Vorlagen mit Platzhaltern.</summary>
        private static readonly string[] KopfzeileErwartet = { "{{ersteller.programm}}" };

        /// <summary>Links die Firma, in der Mitte das Datum (an der Stelle des früheren DATE-Felds), rechts die Seite.</summary>
        private static readonly string[] FusszeileErwartet = { "{{ersteller.firma}}", "{{bericht.datum}}", "{{text.seite}}" };

        /// <summary>Alle drei Vorlagen: Stilregeln und Validator.</summary>
        public static IEnumerable<object[]> Vorlagen()
        {
            yield return new object[] { STILVORLAGE };
            yield return new object[] { STANDARD };
            yield return new object[] { BEISPIEL };
        }

        /// <summary>Die beiden Vorlagen mit Platzhaltern: Runs, Fußzeile, Firmenname.</summary>
        public static IEnumerable<object[]> VorlagenMitPlatzhaltern()
        {
            yield return new object[] { STANDARD };
            yield return new object[] { BEISPIEL };
        }

        // =====================================================================
        //  Alle Vorlagen: Stile und Validator
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

            List<string> fehler = Validatorfehler(doc);
            Assert.True(fehler.Count == 0, datei + ": " + fehler.Count + " Fehler — " + string.Join(" | ", fehler.Take(5)));
        }

        // =====================================================================
        //  Vorlagen mit Platzhaltern: Platzhalter, Runs, Kapitel, Fußzeile, Firmenname
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
        /// Die Standardvorlage im vollen Aufbau (BV-E2, Anhang B.3): Der Rumpf trägt das Deckblatt aus
        /// Platzhaltern, <c>{{kapitel.inhalt}}</c> und je Kapitel den Kapitelplatzhalter mit <c>|ohne titel</c>
        /// unmittelbar unter einem Kapitelkopf, dessen Text die heutige Überschrift ist oder ihr
        /// <c>{{text.kapitel_*}}</c>; keinen Sammelanker. Das Deckblatt ist Abschnitt 1, der letzte Abschnitt
        /// trägt Kopf- und Fußzeile mit denselben Platzhaltern wie die Beispielvorlage.
        /// </summary>
        [Fact]
        public void Die_Standardvorlage_fuehrt_genau_die_erwarteten_Platzhalter()
        {
            using WordprocessingDocument doc = Oeffnen(STANDARD);
            if (doc == null) return;
            MainDocumentPart main = doc.MainDocumentPart;
            Body rumpf = main.Document.Body;

            List<string> platzhalter = Platzhalter(rumpf);
            Assert.Equal(StandardRumpfErwartet,
                         platzhalter.Where(p => !p.StartsWith("{{text.kapitel_", StringComparison.Ordinal)).ToArray());
            Assert.DoesNotContain("{{bericht.inhalt}}", platzhalter);
            Assert.Equal(KopfzeileErwartet, main.HeaderParts.SelectMany(h => Platzhalter(h.Header)).ToArray());
            Assert.Equal(FusszeileErwartet, main.FooterParts.SelectMany(f => Platzhalter(f.Footer)).ToArray());

            List<Paragraph> absaetze = rumpf.Elements<Paragraph>().ToList();
            var koepfe = new List<string>();
            for (int i = 0; i < absaetze.Count; i++)
            {
                Match m = Kapitelmuster.Match(Absatztext(absaetze[i]));
                if (!m.Success || !Absatztext(absaetze[i]).EndsWith("|ohne titel}}", StringComparison.Ordinal)) continue;
                Assert.True(i > 0 && Stilkennung(absaetze[i - 1]) == KAPITELKOPF_ID,
                    "„" + Absatztext(absaetze[i]) + "“ steht nicht unter einem Kapitelkopf.");
                string kopf = Absatztext(absaetze[i - 1]);
                Berichtskapitel kapitel = Berichtskapitel.Finde(m.Groups[1].Value);
                Assert.True(kopf == kapitel.Ueberschrift(false) || kopf == "{{" + kapitel.Kopfschluessel + "}}",
                    "Kapitelkopf „" + kopf + "“ über {{" + kapitel.Schluessel + "}}");
                koepfe.Add(kapitel.Name);
            }
            Assert.Equal(Berichtskapitel.Alle.Where(k => k.Kopfschluessel != null).Select(k => k.Name), koepfe);

            SectionProperties letzter = rumpf.Elements<SectionProperties>().Single();
            Assert.NotEmpty(letzter.Elements<HeaderReference>());
            Assert.NotEmpty(letzter.Elements<FooterReference>());
            Assert.Equal(2, rumpf.Descendants<SectionProperties>().Count());
        }

        /// <summary>
        /// Jeder Platzhalter steht ungeteilt und allein in einem Run, und der Run trägt
        /// <c>w:noProof</c> — sonst fände die Engine ihn nur über den Normalisierer, und Word
        /// unterstriche ihn als Rechtschreibfehler.
        /// </summary>
        [Theory]
        [MemberData(nameof(VorlagenMitPlatzhaltern))]
        public void Jeder_Platzhalter_steht_allein_in_einem_Run_mit_noProof(string datei)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
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
                        datei + ", " + teil + ": Run „" + text + "“ trägt neben dem Platzhalter weiteren Text.");
                    NoProof aus = r.RunProperties?.NoProof;
                    Assert.True(aus != null && (aus.Val == null || aus.Val.Value),
                        datei + ", " + teil + ": Platzhalter „" + text + "“ ohne w:noProof.");
                }
                Assert.True(imText == inRuns,
                    datei + ", " + teil + ": " + imText + " Platzhalter im Text, aber nur " + inRuns + " ungeteilt in einem Run.");
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
        /// Die Fußzeile trägt die Seitenfelder PAGE und NUMPAGES, aber kein DATE-Feld mehr:
        /// DATE zeigte das Datum des Öffnens, nicht das des Berichts — an seiner Stelle steht
        /// <c>{{bericht.datum}}</c> (Konzept 6.7, BV-Q8).
        /// </summary>
        [Theory]
        [MemberData(nameof(VorlagenMitPlatzhaltern))]
        public void Die_Fusszeile_traegt_die_Seitenfelder_und_kein_DATE_Feld(string datei)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;

            List<string> felder = doc.MainDocumentPart.FooterParts
                .SelectMany(f => f.Footer.Descendants<FieldCode>().Select(c => c.Text)
                    .Concat(f.Footer.Descendants<SimpleField>().Select(s => s.Instruction?.Value)))
                .Select(Feldname)
                .ToList();
            Assert.True(felder.Contains("PAGE") && felder.Contains("NUMPAGES") && !felder.Contains("DATE"),
                datei + ": Felder der Fußzeile " + string.Join(", ", felder) + " — erwartet PAGE und NUMPAGES, kein DATE.");
        }

        /// <summary>
        /// Der Name des Herstellers steht nirgends mehr im Paket (BV-Q8: der Bericht nennt den
        /// Ersteller) — weder in Rumpf, Kopf- und Fußzeile noch in Kommentaren oder Eigenschaften.
        /// </summary>
        [Theory]
        [MemberData(nameof(VorlagenMitPlatzhaltern))]
        public void Die_Vorlage_mit_Platzhaltern_nennt_INEKON_GmbH_nicht_mehr(string datei)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;

            foreach ((string teil, OpenXmlElement wurzel) in Teile(doc))
                Assert.False(wurzel.InnerText.Contains("INEKON GmbH", StringComparison.Ordinal),
                    datei + ": „INEKON GmbH“ steht noch in " + teil);

            foreach (OpenXmlPart teil in doc.GetAllParts().Where(p => p.ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase)))
            {
                using var leser = new StreamReader(teil.GetStream(FileMode.Open, FileAccess.Read), Encoding.UTF8);
                Assert.True(!leser.ReadToEnd().Contains("INEKON GmbH", StringComparison.Ordinal),
                    datei + ": „INEKON GmbH“ steht noch in " + teil.Uri);
            }

            string eigenschaften = string.Join(" | ", doc.PackageProperties.Creator, doc.PackageProperties.Title,
                doc.PackageProperties.Description, doc.PackageProperties.LastModifiedBy, doc.PackageProperties.Subject);
            Assert.DoesNotContain("INEKON GmbH", eigenschaften, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Helfer (auch für AuslieferungsvorlagenWacheTests)
        // =====================================================================

        /// <summary>Office 2007 bis 2021 — jede Fassung, die der Validator kennt.</summary>
        private static readonly FileFormatVersions[] Fassungen =
        {
            FileFormatVersions.Office2007, FileFormatVersions.Office2010,
            FileFormatVersions.Office2013, FileFormatVersions.Office2016,
            FileFormatVersions.Office2019, FileFormatVersions.Office2021
        };

        /// <summary>
        /// Die Befunde des <see cref="OpenXmlValidator"/> in jeder Fassung, je Befund
        /// „Fassung: Beschreibung @ Teil Pfad“; leer = gültig.
        /// </summary>
        internal static List<string> Validatorfehler(WordprocessingDocument doc)
            => Fassungen.SelectMany(fassung => new OpenXmlValidator(fassung).Validate(doc)
                            .Select(f => fassung + ": " + f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath))
                        .ToList();

        /// <summary>Der Pfad einer Vorlage im Repository; <c>null</c> außerhalb des Repositoriums.</summary>
        internal static string Pfad(string datei)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            return wurzel == null ? null : Path.Combine(wurzel, ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
        }

        /// <summary>Öffnet eine Vorlage aus dem Repository lesend; <c>null</c> außerhalb des Repositoriums.</summary>
        internal static WordprocessingDocument Oeffnen(string datei)
        {
            string pfad = Pfad(datei);
            if (pfad == null) return null;
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

        /// <summary>Der Feldname einer Feldanweisung (<c>w:instrText</c>, <c>w:fldSimple/@w:instr</c>), groß.</summary>
        private static string Feldname(string anweisung)
            => (anweisung ?? "").Trim().Split(' ', 2)[0].ToUpperInvariant();

        /// <summary>Der Kapitelname eines Bausteins, wie ihn <c>kapitel.&lt;name&gt;</c> führt (Konzept Anhang A).</summary>
        private static string Kapitelname(IBerichtsBaustein b)
        {
            if (b is AnhangEChecklisteBaustein) return "anhang_e";
            if (b.Schluessel == BerichtsKonfiguration.B_INHALT) return "inhalt";
            if (b.Schluessel == BerichtsKonfiguration.B_PROJEKT) return "projekt";
            return b.Schluessel;
        }
    }
}
