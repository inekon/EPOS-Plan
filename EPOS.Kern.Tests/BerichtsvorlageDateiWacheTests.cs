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
    /// <b>Die Wache über die fünf Word-Vorlagen im Repository</b> (Konzept Berichtsvorlagen,
    /// Etappen BV-E0 und BV-E1, 6.2, 6.3, Anhang B.3): die Stilvorlage des Generators
    /// <c>Berichtsvorlage.docx</c>, die Standardvorlage <c>Berichtsvorlage_Standard.docx</c> im
    /// vollen Aufbau (BV-E2: Deckblatt aus Platzhaltern, Kapitel einzeln) und die Beispielvorlage
    /// <c>Berichtsvorlage_Beispiel.docx</c> im vollen Aufbau und der Kurzbericht je Sprache (BV-E5, Anhang B.1), alle erzeugt mit
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

        /// <summary>Der Kurzbericht auf Deutsch (Konzept 6.3 Nr. 2, Anhang B.1; BV-E5) — Lehrvorlage, nur als Kopie wählbar.</summary>
        internal const string KURZBERICHT = BerichtsvorlagenCtrl.DATEI_KURZBERICHT;

        /// <summary>Der Kurzbericht auf Englisch.</summary>
        internal const string KURZBERICHT_EN = BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN;

        /// <summary>
        /// Die ausführliche Vorlage auf Deutsch (Entscheid BV-E8-4) — der volle Bericht aus Einzelelementen, nur als Kopie
        /// wählbar; ihren Rundlauf gegen den Standardbericht hält <see cref="AusfuehrlichRundlaufTests"/>.
        /// </summary>
        internal const string AUSFUEHRLICH = BerichtsvorlagenCtrl.DATEI_AUSFUEHRLICH;

        /// <summary>Die ausführliche Vorlage auf Englisch.</summary>
        internal const string AUSFUEHRLICH_EN = BerichtsvorlagenCtrl.DATEI_AUSFUEHRLICH_EN;

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
            // Inhaltsverzeichnis, dann je Kapitel der Kapitelkopf (Titel in der Sprache des Berichts) und der Kapitelplatzhalter
            "{{kapitel.inhalt}}",
            "{{text.kapitel_projekt}}", "{{kapitel.projekt|ohne titel}}",
            "{{text.kapitel_komponenten}}", "{{kapitel.komponenten|ohne titel}}",
            "{{text.kapitel_ergebnisse}}", "{{kapitel.ergebnisse|ohne titel}}",
            "{{text.kapitel_vergleich}}", "{{kapitel.vergleich|ohne titel}}",
            "{{text.kapitel_wirtschaftlichkeit}}", "{{kapitel.wirtschaftlichkeit|ohne titel}}",
            "{{text.kapitel_anhang}}", "{{kapitel.anhang|ohne titel}}",
            "{{text.kapitel_anhang_e}}", "{{kapitel.anhang_e|ohne titel}}",
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

        /// <summary>Alle fünf Vorlagen: Stilregeln und Validator.</summary>
        public static IEnumerable<object[]> Vorlagen()
        {
            yield return new object[] { STILVORLAGE };
            yield return new object[] { STANDARD };
            yield return new object[] { BEISPIEL };
            yield return new object[] { KURZBERICHT };
            yield return new object[] { KURZBERICHT_EN };
            yield return new object[] { AUSFUEHRLICH };
            yield return new object[] { AUSFUEHRLICH_EN };
        }

        /// <summary>Die Vorlagen mit Platzhaltern: Runs, Fußzeile, Firmenname.</summary>
        public static IEnumerable<object[]> VorlagenMitPlatzhaltern()
        {
            yield return new object[] { STANDARD };
            yield return new object[] { BEISPIEL };
            yield return new object[] { KURZBERICHT };
            yield return new object[] { KURZBERICHT_EN };
            yield return new object[] { AUSFUEHRLICH };
            yield return new object[] { AUSFUEHRLICH_EN };
        }

        /// <summary>Die beiden ausführlichen Vorlagen mit ihrer Sprache.</summary>
        public static IEnumerable<object[]> Ausfuehrliche()
        {
            yield return new object[] { AUSFUEHRLICH, "de" };
            yield return new object[] { AUSFUEHRLICH_EN, "en" };
        }

        /// <summary>Die beiden Kurzberichte mit ihrer Sprache.</summary>
        public static IEnumerable<object[]> Kurzberichte()
        {
            yield return new object[] { KURZBERICHT, "de" };
            yield return new object[] { KURZBERICHT_EN, "en" };
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
        /// Jeder Kapitelplatzhalter mit <c>|ohne titel</c> steht unmittelbar unter seinem
        /// Kapitelkopf: einem Absatz im Format „EPOS Kapitelkopf“, der allein den Platzhalter
        /// <c>{{text.kapitel_&lt;name&gt;}}</c> desselben Kapitels trägt — den Titel setzt der Bericht
        /// in seiner Sprache ein, die Vorlage bleibt sprachneutral (Konzept 4.9). Jeder Kapitelkopf
        /// steht über einem Kapitelplatzhalter, die Kapitel stehen in der Folge des heutigen
        /// Berichts (<see cref="WordBerichtGenerator.AktiveBausteine"/>), und am ersten Kapitelkopf
        /// erläutert ein Kommentar den Platzhalter.
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
                    Assert.Equal("{{text.kapitel_" + m.Groups[1].Value + "}}", Absatztext(absaetze[i - 1]));
                    koepfe.Add(m.Groups[1].Value);
                }
                if (istKopf)
                    Assert.True(i + 1 < absaetze.Count && Absatztext(absaetze[i + 1]).EndsWith("|ohne titel}}", StringComparison.Ordinal),
                        "Kapitelkopf „" + text + "“ steht über keinem Kapitelplatzhalter.");
            }

            Assert.Equal(
                new[] { "projekt", "komponenten", "ergebnisse", "vergleich", "wirtschaftlichkeit", "anhang", "anhang_e" },
                koepfe.ToArray());

            List<string> heute = WordBerichtGenerator.AktiveBausteine(null)
                .Where(b => b.Schluessel != BerichtsKonfiguration.B_DECKBLATT)
                .Select(Kapitelname)
                .ToList();
            Assert.Equal(heute, kapitel);

            Paragraph ersterKopf = absaetze.First(p => Stilkennung(p) == KAPITELKOPF_ID);
            Assert.NotEmpty(ersterKopf.Descendants<CommentReference>());
        }

        /// <summary>
        /// Das Firmenlogo der Kopfzeile ist ein Bildplatzhalter (Entscheid BV-E2-1): genau ein Bild
        /// mit dem Alternativtext <c>{{bild.ersteller.logo}}</c>, an Ort, in Größe und Umbruch des
        /// Logos der Stilvorlage (eingebettet, gleiche Ausdehnung), aber mit einem neutralen
        /// Platzhalterbild — kein Bildteil des Pakets trägt die Bytes des Logos. Der Text der
        /// Kopfzeile bleibt.
        /// </summary>
        [Fact]
        public void Die_Beispielvorlage_traegt_in_der_Kopfzeile_den_Bildplatzhalter_des_Logos()
        {
            using WordprocessingDocument doc = Oeffnen(BEISPIEL);
            using WordprocessingDocument stil = Oeffnen(STILVORLAGE);
            if (doc == null || stil == null) return;

            HeaderPart kopf = Assert.Single(doc.MainDocumentPart.HeaderParts);
            Assert.Equal("{{ersteller.programm}} · Energie · Planung · Optimierung · Simulation",
                         string.Concat(kopf.Header.Descendants<Text>().Select(t => t.Text)));
            Assert.Empty(kopf.Header.Descendants<Picture>());
            Drawing bild = Assert.Single(kopf.Header.Descendants<Drawing>());
            Assert.Equal("{{bild.ersteller.logo}}",
                         bild.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().Single().Description?.Value);

            HeaderPart stilkopf = Assert.Single(stil.MainDocumentPart.HeaderParts);
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent ausdehnung =
                Assert.Single(bild.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>()).Extent;
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent logoausdehnung =
                Assert.Single(stilkopf.Header.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>()).Extent;
            Assert.Equal((logoausdehnung.Cx.Value, logoausdehnung.Cy.Value), (ausdehnung.Cx.Value, ausdehnung.Cy.Value));

            string kennung = bild.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Single().Embed?.Value;
            Assert.Equal("image/png", kopf.GetPartById(kennung).ContentType);
            byte[] firmenlogo = Teilbytes(Assert.Single(stilkopf.ImageParts));
            foreach (ImagePart teil in doc.GetAllParts().OfType<ImagePart>())
                Assert.False(Teilbytes(teil).AsSpan().SequenceEqual(firmenlogo),
                    BEISPIEL + ": Der Bildteil " + teil.Uri + " trägt noch das Firmenlogo.");

            static byte[] Teilbytes(OpenXmlPart teil)
            {
                using Stream quelle = teil.GetStream(FileMode.Open, FileAccess.Read);
                using var puffer = new MemoryStream();
                quelle.CopyTo(puffer);
                return puffer.ToArray();
            }
        }

        /// <summary>
        /// <c>docProps/custom.xml</c> nennt die Katalogfassung, für die die Beispielvorlage gebaut ist
        /// (Konzept 5.6; Katalog v2 führt <c>text.kapitel_*</c>), und die Art der Vorlage.
        /// </summary>
        [Fact]
        public void Die_Beispielvorlage_nennt_Katalogfassung_und_Art_in_custom_xml()
        {
            using WordprocessingDocument doc = Oeffnen(BEISPIEL);
            if (doc == null) return;

            OpenXmlElement eigenschaften = doc.CustomFilePropertiesPart?.RootElement;
            Assert.NotNull(eigenschaften);
            Dictionary<string, string> werte = eigenschaften.ChildElements
                .ToDictionary(e => e.GetAttribute("name", "").Value, e => e.InnerText, StringComparer.Ordinal);
            Assert.Equal("4", werte[Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG]);
            Assert.Equal("beispiel", werte["EPOS.Vorlage"]);
        }

        /// <summary>
        /// Die Standardvorlage steht auf der laufenden Katalogfassung (Anwenderentscheid BV-E4-4, mit BV-E5 auf Fassung 4):
        /// Inhalt und Aussehen bleiben, <c>custom.xml</c> nennt die Fassung — Tabellen und Bilder deckt sie über ihre
        /// Kapitel (BV-E5-3, Deckungswache). Eine Sprache trägt sie nicht: Sie ist sprachneutral (4.9).
        /// </summary>
        [Fact]
        public void Die_Standardvorlage_nennt_die_laufende_Katalogfassung()
        {
            using WordprocessingDocument doc = Oeffnen(STANDARD);
            if (doc == null) return;
            Dictionary<string, string> werte = Eigenschaften(doc);
            Assert.Equal(Vorlagenfeldkatalog.KatalogfassungWord.ToString(System.Globalization.CultureInfo.InvariantCulture),
                         werte[Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG]);
            Assert.Equal("standard", werte["EPOS.Vorlage"]);
            Assert.False(werte.ContainsKey(Vorlagenpruefer.EIGENSCHAFT_SPRACHE));
        }

        // =====================================================================
        //  Die Kurzberichte (BV-E5, Konzept 6.3 Nr. 2, Anhang B.1)
        // =====================================================================

        /// <summary>Eine Marke des Kurzberichts: Platzhalter mit beliebig vielen Formatangaben oder Blockmarke.</summary>
        private static readonly Regex Markenmuster = new Regex(@"\{\{[#/]?[a-z0-9_. ]+(\|[a-z0-9 ]+)*\}\}", RegexOptions.CultureInvariant);

        /// <summary>Die Marken des Rumpfs beider Kurzberichte in Dokumentfolge (Anhang B.1) — sprachgleich.</summary>
        private static readonly string[] KurzberichtRumpf =
        {
            "{{bericht.titel}}", "{{projekt.kunde}}", "{{projekt.bearbeiter}}", "{{ersteller.firma}}", "{{bericht.datum}}",
            "{{bericht.varianten.liste}}", "{{ersteller.programm}}", "{{ersteller.version}}",
            "{{projekt.beschreibung}}", "{{projekt.klimaregion}}",
            "{{kennzahl.energie.waermebedarf.einheit}}", "{{kennzahl.em.co2.einheit}}",
            "{{#je stand}}", "{{stand.anzeige}}", "{{stand.kennzahl.energie.waermebedarf|ohne einheit}}",
            "{{stand.kennzahl.eff.jaz|stellen 1}}", "{{stand.kennzahl.em.co2|ohne einheit}}",
            "{{stand.wirtschaft.kapitalwert_diff|mit grund}}", "{{/je}}",
            "{{bericht.warnungen}}",
            "{{wirtschaft.beste.anzeige}}", "{{wirtschaft.beste.kapitalwert_diff}}", "{{wirtschaft.vorschlag}}",
            "{{#wenn hat.bild.wirtschaft.spanne}}", "{{/wenn}}", "{{tabelle.wirtschaft.szenarien}}", "{{wirtschaft.warnungen}}",
            "{{#wenn hat.kaelte}}", "{{stamm.kennzahl.kaelte.jahresbedarf}}", "{{stamm.kennzahl.kaelte.deckungsgrad}}", "{{/wenn}}",
            "{{#je stand}}", "{{stand.anzeige}}", "{{/je}}",
            "{{kapitel.anhang|ohne titel|ebene 2}}",
        };

        /// <summary>
        /// Der Kurzbericht je Sprache (Anhang B.1): der Rumpf aus Einzelwerten, Musterzeile, Blöcken, Strukturtabelle,
        /// drei Bildrahmen — das Spannenbild in voller Breite, die Deckungsbilder nebeneinander in einer Tabelle ohne
        /// Rahmen — und dem Anhang als einzigem Kapitel; am Ende die Mustertabelle. Jede Marke steht allein in einem Run
        /// mit <c>w:noProof</c>; Kopfzeile <c>{{projekt.name}} · {{bericht.titel}}</c> mit dem Bildplatzhalter des Logos,
        /// Fußzeile wie die Standardvorlage; neun Kommentare in der Sprache der Datei, jeder mit genau einem Verweis;
        /// <c>custom.xml</c> mit Fassung, Art <c>kurzbericht</c> und Sprache.
        /// </summary>
        [Theory]
        [MemberData(nameof(Kurzberichte))]
        public void Der_Kurzbericht_fuehrt_Werte_Bloecke_Tabellen_und_Bilder(string datei, string sprache)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;
            MainDocumentPart main = doc.MainDocumentPart;
            Body rumpf = main.Document.Body;

            Assert.Equal(KurzberichtRumpf, Marken(rumpf).ToArray());
            Assert.Equal(new[] { "{{projekt.name}}", "{{bericht.titel}}" }, main.HeaderParts.SelectMany(h => Marken(h.Header)).ToArray());
            Assert.Equal(FusszeileErwartet, main.FooterParts.SelectMany(f => Marken(f.Footer)).ToArray());
            foreach ((string teil, OpenXmlElement wurzel) in Teile(doc))
                foreach (Run r in wurzel.Descendants<Run>())
                {
                    string text = string.Concat(r.Elements<Text>().Select(t => t.Text));
                    if (!Markenmuster.IsMatch(text)) continue;
                    Assert.True(Markenmuster.Match(text).Value == text, datei + ", " + teil + ": Run „" + text + "“ trägt weiteren Text.");
                    Assert.NotNull(r.RunProperties?.NoProof);
                }

            // Bildrahmen: Schlüssel im Alternativtext, das Spannenbild voll, die Deckungsbilder halb und nebeneinander.
            List<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline> rahmen =
                rumpf.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().ToList();
            Assert.Equal(new[] { "{{bild.wirtschaft.spanne}}", "{{stand.bild.deckung_waerme}}", "{{stand.bild.deckung_strom}}" },
                         rahmen.Select(i => i.DocProperties.Description?.Value));
            Assert.Equal(rahmen[1].Extent.Cx.Value, rahmen[2].Extent.Cx.Value);
            Assert.True(rahmen[1].Extent.Cx.Value * 2 < rahmen[0].Extent.Cx.Value, "die Deckungsbilder in halber Breite");
            Table nebeneinander = rahmen[1].Ancestors<Table>().Single();
            Assert.Same(nebeneinander, rahmen[2].Ancestors<Table>().Single());
            Assert.Equal(2, nebeneinander.Descendants<TableCell>().Count());
            Assert.Equal(BorderValues.None, nebeneinander.GetFirstChild<TableProperties>().TableBorders.TopBorder.Val.Value);

            // Musterzeile: #je in der ersten, /je in der letzten Zelle derselben Zeile.
            TableRow muster = rumpf.Descendants<TableRow>().Single(z => z.InnerText.StartsWith("{{#je stand}}", StringComparison.Ordinal));
            Assert.EndsWith("{{/je}}", muster.Elements<TableCell>().Last().InnerText, StringComparison.Ordinal);

            // Der Anhang unter einem Kapitelkopf, die Mustertabelle am Ende.
            List<Paragraph> absaetze = rumpf.Elements<Paragraph>().ToList();
            int anhang = absaetze.FindIndex(p => Absatztext(p) == "{{kapitel.anhang|ohne titel|ebene 2}}");
            Assert.Equal(KAPITELKOPF_ID, Stilkennung(absaetze[anhang - 1]));
            Table letzte = rumpf.Elements<Table>().Last();
            Assert.Equal("{{muster.tabelle}}", letzte.GetFirstChild<TableProperties>().GetFirstChild<TableDescription>()?.Val?.Value);
            Assert.Equal(sprache == "en" ? new[] { "Base", "Group", "Total", "Warning" } : new[] { "Stamm", "Gruppe", "Summe", "Warnung" },
                         letzte.Descendants<TableCell>().Select(c => c.InnerText));

            // Kommentare je Stelle, in der Sprache der Datei.
            List<string> kommentare = main.WordprocessingCommentsPart.Comments.Elements<Comment>().Select(c => c.Id.Value).ToList();
            Assert.Equal(9, kommentare.Count);
            Assert.Equal(kommentare.OrderBy(k => k, StringComparer.Ordinal),
                         rumpf.Descendants<CommentReference>().Select(r => r.Id.Value).OrderBy(k => k, StringComparer.Ordinal));
            Assert.Contains(sprache == "en" ? "Summary report" : "Kurzbericht", main.WordprocessingCommentsPart.Comments.InnerText,
                            StringComparison.Ordinal);

            Dictionary<string, string> werte = Eigenschaften(doc);
            Assert.Equal(Vorlagenfeldkatalog.KatalogfassungWord.ToString(System.Globalization.CultureInfo.InvariantCulture),
                         werte[Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG]);
            Assert.Equal("kurzbericht", werte["EPOS.Vorlage"]);
            Assert.Equal(sprache, werte[Vorlagenpruefer.EIGENSCHAFT_SPRACHE]);
        }

        /// <summary>
        /// Die beiden Kurzberichte gleichen einander bis auf die Sprache: dieselben Marken in derselben Folge, dieselben
        /// Absatzformate, dieselbe Zahl von Absätzen und Tabellen — aber verschiedener Text.
        /// </summary>
        [Fact]
        public void Die_Kurzberichte_gleichen_einander_bis_auf_die_Sprache()
        {
            using WordprocessingDocument de = Oeffnen(KURZBERICHT);
            using WordprocessingDocument en = Oeffnen(KURZBERICHT_EN);
            if (de == null || en == null) return;
            Body a = de.MainDocumentPart.Document.Body, b = en.MainDocumentPart.Document.Body;
            Assert.Equal(Marken(a), Marken(b));
            Assert.Equal(a.Descendants<Paragraph>().Count(), b.Descendants<Paragraph>().Count());
            Assert.Equal(a.Descendants<Table>().Count(), b.Descendants<Table>().Count());
            Assert.Equal(a.Elements<Paragraph>().Select(Stilkennung), b.Elements<Paragraph>().Select(Stilkennung));
            Assert.NotEqual(a.InnerText, b.InnerText);
        }

        /// <summary>Die Kapitelköpfe der ausführlichen Vorlage in der Folge des Standardberichts.</summary>
        private static readonly string[] AusfuehrlichKoepfe =
        {
            "{{text.kapitel_projekt}}", "{{text.kapitel_komponenten}}", "{{text.kapitel_ergebnisse}}", "{{text.kapitel_vergleich}}",
            "{{text.kapitel_wirtschaftlichkeit}}", "{{text.kapitel_anhang}}", "{{text.kapitel_anhang_e}}",
        };

        /// <summary>
        /// Die ausführliche Vorlage je Sprache (Entscheid BV-E8-4): Deckblatt wie die Standardvorlage, ein Inhaltsverzeichnis
        /// als Word-Feld, die sieben Kapitelköpfe <c>{{text.kapitel_*}}</c> in der Folge des Standardberichts — und
        /// <b>kein</b> Kapitelplatzhalter: Jeder Abschnitt besteht aus Einzelwerten, Blöcken <c>je stand</c>, <c>je variante</c>,
        /// <c>je gebaeude</c> und <c>wenn</c>, Strukturtabellen, Musterzeilen und Warnlisten. Die Bildrahmen tragen ihren
        /// Schlüssel im Alternativtext und haben volle oder halbe Satzspiegelbreite, die halben stehen paarweise in einer
        /// Tabelle ohne Rahmen. Das Tabellenformat „EPOS Tabelle“ liegt in der Datei, die Mustertabelle am Ende; Kopf- und
        /// Fußzeile wie die Standardvorlage; je Abschnitt ein Kommentar mit genau einem Verweis; <c>custom.xml</c> mit
        /// Katalogfassung, Art <c>ausfuehrlich</c> und Sprache. Deckblatt, Projektbeschreibung, Anhang und Anhang E deckt die
        /// Vorlage aus Einzelschlüsseln (<see cref="Vorlagenfeldkatalog.Gedeckt(IEnumerable{string})"/>).
        /// </summary>
        [Theory]
        [MemberData(nameof(Ausfuehrliche))]
        public void Die_ausfuehrliche_Vorlage_baut_jeden_Abschnitt_aus_Einzelelementen(string datei, string sprache)
        {
            using WordprocessingDocument doc = Oeffnen(datei);
            if (doc == null) return;
            MainDocumentPart main = doc.MainDocumentPart;
            Body rumpf = main.Document.Body;
            List<string> marken = Marken(rumpf);

            Assert.Empty(marken.Where(m => m.StartsWith("{{kapitel.", StringComparison.Ordinal) || m == "{{bericht.inhalt}}"));
            Assert.Equal(AusfuehrlichKoepfe, rumpf.Elements<Paragraph>().Where(p => Stilkennung(p) == KAPITELKOPF_ID).Select(Absatztext));
            Assert.Contains(rumpf.Descendants<FieldCode>(), c => Feldname(c.Text) == "TOC");
            foreach (string art in new[] { "{{#je stand}}", "{{#je variante}}", "{{#je gebaeude}}", "{{#wenn hat.", "{{#wenn nicht ",
                                           "{{tabelle.", "{{stand.tabelle.", "{{stamm.kennzahl.", "{{stand.kennzahl.",
                                           "{{kennzahl.", "{{stand.wirtschaft.", "{{wirtschaft.beste.", "{{vergleich.",
                                           "{{gebaeude.", "{{wirtschaft.warnungen}}", "{{bericht.warnungen}}" })
                Assert.True(marken.Any(m => m.StartsWith(art, StringComparison.Ordinal)), datei + ": keine Marke „" + art + "…“");
            Assert.Equal(marken.Count(m => m.StartsWith("{{#je", StringComparison.Ordinal)), marken.Count(m => m == "{{/je}}"));
            Assert.Equal(marken.Count(m => m.StartsWith("{{#wenn", StringComparison.Ordinal)), marken.Count(m => m == "{{/wenn}}"));
            Assert.Equal(new[] { "{{ersteller.programm}}" }, main.HeaderParts.SelectMany(h => Marken(h.Header)).ToArray());
            Assert.Equal(FusszeileErwartet, main.FooterParts.SelectMany(f => Marken(f.Footer)).ToArray());
            foreach ((string teil, OpenXmlElement wurzel) in Teile(doc))
                foreach (Run r in wurzel.Descendants<Run>())
                {
                    string text = string.Concat(r.Elements<Text>().Select(t => t.Text));
                    if (!Markenmuster.IsMatch(text)) continue;
                    Assert.True(Markenmuster.Match(text).Value == text, datei + ", " + teil + ": Run „" + text + "“ trägt weiteren Text.");
                    Assert.NotNull(r.RunProperties?.NoProof);
                }

            // Bildrahmen: Schlüssel eines Bildes im Alternativtext, volle oder halbe Breite, die halben paarweise ohne Rahmen.
            List<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline> rahmen =
                rumpf.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().ToList();
            Assert.Equal(16, rahmen.Count);
            long voll = rahmen.Max(i => i.Extent.Cx.Value);
            foreach (var bild in rahmen)
            {
                string schluessel = bild.DocProperties.Description?.Value ?? "";
                Assert.Matches(@"^\{\{(stand\.|stamm\.)?bild\.[a-z0-9_.]+\}\}$", schluessel);
                Assert.Equal(Vorlagenfeldart.Bild, Vorlagenfeldkatalog.Finde(schluessel.Trim('{', '}'))?.Art);
                Assert.True(bild.Extent.Cx.Value == voll || bild.Extent.Cx.Value * 2 < voll, schluessel + ": weder volle noch halbe Breite");
            }
            List<Table> paare = rahmen.Where(i => i.Extent.Cx.Value < voll).Select(i => i.Ancestors<Table>().Single()).Distinct().ToList();
            Assert.NotEmpty(paare);
            foreach (Table paar in paare)
            {
                Assert.Equal(2, paar.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().Count());
                Assert.Equal(BorderValues.None, paar.GetFirstChild<TableProperties>().TableBorders.TopBorder.Val.Value);
            }

            // Musterzeilen: #je in der ersten, /je in der letzten Zelle derselben Zeile.
            List<TableRow> muster = rumpf.Descendants<TableRow>().Where(z => z.InnerText.StartsWith("{{#je stand}}", StringComparison.Ordinal)).ToList();
            Assert.Equal(2, muster.Count);
            foreach (TableRow z in muster)
                Assert.EndsWith("{{/je}}", z.Elements<TableCell>().Last().InnerText, StringComparison.Ordinal);

            // Tabellenformat und Mustertabelle.
            Style tabellenstil = Stile(doc).Elements<Style>().SingleOrDefault(s => s.StyleName?.Val?.Value == "EPOS Tabelle");
            Assert.NotNull(tabellenstil);
            Assert.Equal(StyleValues.Table, tabellenstil.Type.Value);
            Table letzte = rumpf.Elements<Table>().Last();
            Assert.Equal("{{muster.tabelle}}", letzte.GetFirstChild<TableProperties>().GetFirstChild<TableDescription>()?.Val?.Value);
            Assert.Equal(sprache == "en" ? new[] { "Base", "Group", "Total", "Warning" } : new[] { "Stamm", "Gruppe", "Summe", "Warnung" },
                         letzte.Descendants<TableCell>().Select(c => c.InnerText));

            // Kommentare je Abschnitt, in der Sprache der Datei.
            List<string> kommentare = main.WordprocessingCommentsPart.Comments.Elements<Comment>().Select(c => c.Id.Value).ToList();
            Assert.True(kommentare.Count >= AusfuehrlichKoepfe.Length + 2, kommentare.Count + " Kommentare");
            Assert.Equal(kommentare.OrderBy(k => k, StringComparer.Ordinal),
                         rumpf.Descendants<CommentReference>().Select(r => r.Id.Value).OrderBy(k => k, StringComparer.Ordinal));
            foreach (Paragraph kopf in rumpf.Elements<Paragraph>().Where(p => Stilkennung(p) == KAPITELKOPF_ID && Absatztext(p) != "{{text.kapitel_anhang_e}}"))
                Assert.True(kopf.Descendants<CommentReference>().Any(), Absatztext(kopf) + " ohne Kommentar");
            Assert.Contains(sprache == "en" ? "Detailed template" : "Ausführliche Vorlage", main.WordprocessingCommentsPart.Comments.InnerText,
                            StringComparison.Ordinal);

            // Gedeckt aus Einzelschlüsseln: Deckblatt, Projektbeschreibung, Anhang, Anhang E.
            IEnumerable<string> genutzt = marken.Where(m => !m.StartsWith("{{#", StringComparison.Ordinal) && !m.StartsWith("{{/", StringComparison.Ordinal))
                .Select(m => m.Trim('{', '}').Split('|')[0].Trim())
                .Concat(rahmen.Select(i => i.DocProperties.Description.Value.Trim('{', '}')));
            HashSet<string> gedeckt = Vorlagenfeldkatalog.Gedeckt(genutzt);
            foreach (string kapitel in new[] { "kapitel.deckblatt", "kapitel.projekt", "kapitel.anhang", "kapitel.anhang_e" })
                Assert.Contains(kapitel, gedeckt);

            Dictionary<string, string> werte = Eigenschaften(doc);
            Assert.Equal(Vorlagenfeldkatalog.KatalogfassungWord.ToString(System.Globalization.CultureInfo.InvariantCulture),
                         werte[Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG]);
            Assert.Equal("ausfuehrlich", werte["EPOS.Vorlage"]);
            Assert.Equal(sprache, werte[Vorlagenpruefer.EIGENSCHAFT_SPRACHE]);
        }

        /// <summary>
        /// Die beiden ausführlichen Vorlagen sind gleich gebaut: dieselben Marken in derselben Folge, dieselben Absatzformate
        /// (auch in den Tabellenzellen), dieselben Bildrahmen in denselben Maßen, dieselbe Zahl von Tabellen und
        /// Kommentaren — nur der feste Text unterscheidet sich.
        /// </summary>
        [Fact]
        public void Die_ausfuehrlichen_Vorlagen_gleichen_einander_bis_auf_die_Sprache()
        {
            using WordprocessingDocument de = Oeffnen(AUSFUEHRLICH);
            using WordprocessingDocument en = Oeffnen(AUSFUEHRLICH_EN);
            if (de == null || en == null) return;
            Body a = de.MainDocumentPart.Document.Body, b = en.MainDocumentPart.Document.Body;
            Assert.Equal(Marken(a), Marken(b));
            Assert.Equal(a.Descendants<Paragraph>().Select(Stilkennung), b.Descendants<Paragraph>().Select(Stilkennung));
            Assert.Equal(a.Descendants<Table>().Count(), b.Descendants<Table>().Count());
            Assert.Equal(a.Descendants<TableRow>().Select(z => z.Elements<TableCell>().Count()),
                         b.Descendants<TableRow>().Select(z => z.Elements<TableCell>().Count()));
            Assert.Equal(a.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().Select(i => (i.DocProperties.Description.Value, i.Extent.Cx.Value, i.Extent.Cy.Value)),
                         b.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().Select(i => (i.DocProperties.Description.Value, i.Extent.Cx.Value, i.Extent.Cy.Value)));
            Assert.Equal(a.Descendants<CommentReference>().Select(r => r.Id.Value), b.Descendants<CommentReference>().Select(r => r.Id.Value));
            Assert.NotEqual(a.InnerText, b.InnerText);
        }

        private static List<string> Marken(OpenXmlElement wurzel)
            => wurzel.Descendants<Paragraph>().SelectMany(p => Markenmuster.Matches(Absatztext(p)).Select(m => m.Value)).ToList();

        private static Dictionary<string, string> Eigenschaften(WordprocessingDocument doc)
        {
            OpenXmlElement eigenschaften = doc.CustomFilePropertiesPart?.RootElement;
            Assert.NotNull(eigenschaften);
            return eigenschaften.ChildElements.ToDictionary(e => e.GetAttribute("name", "").Value, e => e.InnerText, StringComparer.Ordinal);
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
                            .Where(f => !IstAlternativtextDerMustertabelle(fassung, f))
                            .Select(f => fassung + ": " + f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath))
                        .ToList();

        /// <summary>
        /// Die eine Ausnahme (wie im Werkzeug, <c>Pruefung.IstAusnahmeMustertabelle</c>): Office 2007 kennt keinen
        /// Alternativtext einer Tabelle (<c>w:tblDescription</c>, ab Word 2010). Eine Vorlage mit Mustertabelle trägt ihn
        /// als <c>{{muster.tabelle}}</c> — nur dieser Befund zählt dort nicht; die Engine entfernt die Mustertabelle,
        /// der fertige Bericht bleibt in jeder Fassung gültig.
        /// </summary>
        private static bool IstAlternativtextDerMustertabelle(FileFormatVersions fassung, ValidationErrorInfo f)
            => fassung == FileFormatVersions.Office2007
               && f.Node is TableProperties tp
               && (f.Description ?? "").Contains(":tblDescription", StringComparison.Ordinal)
               && tp.GetFirstChild<TableDescription>()?.Val?.Value == "{{muster.tabelle}}";

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
