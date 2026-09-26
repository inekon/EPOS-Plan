using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bausteinvorlage</b> (Etappe BV-E9, <see cref="WordBausteinvorlage"/>): eine Dokumentvorlage (<c>.dotx</c>) je
    /// Sprache, deren Schnellbausteine jeden Platzhalter des Katalogs mit Ausgabe Word anbieten.
    ///
    /// <para><b>Glossar:</b> jeder Word-Schlüssel der Word-Fassung genau einmal — dieselbe Auswahl wie der
    /// <see cref="WordBaukasten"/>, die Paarsicht als Muster —, in seiner Kategorie, in der Galerie „Schnellbausteine“, mit der
    /// Beschreibung des Katalogs in der Sprache der Datei und dem Platzhalter in der Form seiner Art (Satz, Absatz, Bild mit
    /// Alternativtext, Rahmen), jeder Platzhalterlauf mit <c>w:noProof</c>.</para>
    ///
    /// <para><b>Aktuell:</b> Die ausgelieferten Dateien sind die, die der Kern aus der Standardvorlage erzeugt (Vergleich über
    /// den Inhaltsschlüssel des Musterordners); ihr Rumpf ist der der Standardvorlage.</para>
    ///
    /// <para><b>Füllweg:</b> Die <c>.dotx</c> selbst als Vorlage und ein Dokument, das Word aus ihr anlegt, füllen 1030 ohne
    /// Prüferfehler; der Bericht ist eine <c>.docx</c> ohne Glossar und ohne <c>{{</c> in irgendeinem Teil. Platzhaltertexte
    /// von Inhaltssteuerelementen im Glossar einer eigenen Vorlage bleiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordBausteinvorlageTests : IDisposable
    {
        private readonly ITestOutputHelper _ausgabe;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner;

        public WordBausteinvorlageTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
            _ordner = Path.Combine(Path.GetTempPath(), "epos-bausteine-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            _kultur.Dispose();
            try { Directory.Delete(_ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }

        public static IEnumerable<object[]> Dateien()
        {
            yield return new object[] { BerichtsvorlageDateiWacheTests.BAUSTEINE, false };
            yield return new object[] { BerichtsvorlageDateiWacheTests.BAUSTEINE_EN, true };
        }

        // =====================================================================
        //  Glossar
        // =====================================================================

        [Theory]
        [MemberData(nameof(Dateien))]
        public void Das_Glossar_bietet_jeden_Word_Schluessel_genau_einmal_in_seiner_Kategorie(string datei, bool englisch)
        {
            using WordprocessingDocument doc = BerichtsvorlageDateiWacheTests.Oeffnen(datei);
            if (doc == null) return;
            Assert.Equal(WordprocessingDocumentType.Template, doc.DocumentType);

            List<DocPart> teile = doc.MainDocumentPart.GlossaryDocumentPart.GlossaryDocument.DocParts.Elements<DocPart>().ToList();
            var nachName = new Dictionary<string, DocPart>(StringComparer.Ordinal);
            foreach (DocPart t in teile)
                Assert.True(nachName.TryAdd(Name(t), t), datei + ": Baustein „" + Name(t) + "“ doppelt.");

            // Die Word-Schlüssel der Word-Fassung: dieselbe Auswahl wie der Baukasten (Paarsicht als Muster).
            int fassung = Vorlagenfeldkatalog.KatalogfassungWord;
            List<Vorlagenfeld> soll = WordBaukasten.Abschnitte(fassung).SelectMany(a => a.Eintraege).ToList();
            List<Vorlagenfeld> wordfelder = Vorlagenfeldkatalog.Alle
                .Where(f => f.Seit <= fassung && (f.Ausgaben & Vorlagenausgabe.Word) != 0).ToList();
            Assert.Equal(wordfelder.Where(f => !Vorlagenpruefer.IstPaarschluessel(f.Schluessel)).Select(f => f.Schluessel).OrderBy(s => s, StringComparer.Ordinal),
                         soll.Where(f => !Vorlagenpruefer.IstPaarschluessel(f.Schluessel)).Select(f => f.Schluessel).OrderBy(s => s, StringComparer.Ordinal));
            Assert.InRange(soll.Count(f => Vorlagenpruefer.IstPaarschluessel(f.Schluessel)), 1, 5);
            Assert.Equal(soll.Count + WordBausteinvorlage.Wiederholbloecke.Count, teile.Count);
            _ausgabe.WriteLine(datei + ": " + teile.Count + " Schnellbausteine");

            foreach (Vorlagenfeld f in soll)
            {
                Assert.True(nachName.TryGetValue(f.Schluessel, out DocPart t), datei + ": " + f.Schluessel + " fehlt im Glossar.");
                Bausteinkategorie k = WordBausteinvorlage.Kategorie(f);
                Assert.Equal(WordBausteinvorlage.Kategoriename(k, englisch), Kategorie(t));
                Assert.Equal(DocPartGalleryValues.DocumentPart, Galerie(t));
                Assert.Equal(WordBaukasten.Entschaerft(Vorlagenfeldkatalog.Beschreibung(f, englisch)),
                             t.DocPartProperties.GetFirstChild<Description>()?.Val?.Value);
                PruefeInhalt(datei, f, t);
            }
            foreach (string block in WordBausteinvorlage.Wiederholbloecke)
            {
                DocPart t = nachName["#" + block];
                Assert.Equal(WordBausteinvorlage.Kategoriename(Bausteinkategorie.Bloecke, englisch), Kategorie(t));
                Assert.Equal(new[] { "{{#" + block + "}}", "", "{{/je}}" }, Absaetze(t));
            }

            // Die Kategorie folgt der Art vor dem Kontext — Stichproben unabhängig von WordBausteinvorlage.Kategorie.
            string K(Bausteinkategorie b) => WordBausteinvorlage.Kategoriename(b, englisch);
            Assert.Equal(K(Bausteinkategorie.Bericht), Kategorie(nachName["bericht.titel"]));
            Assert.Equal(K(Bausteinkategorie.Bericht), Kategorie(nachName["kapitel.projekt"]));
            Assert.Equal(K(Bausteinkategorie.Installation), Kategorie(nachName["ersteller.firma"]));
            Assert.Equal(K(Bausteinkategorie.Stamm), Kategorie(nachName["projekt.kunde"]));
            Assert.Equal(K(Bausteinkategorie.Stand), Kategorie(nachName["stand.anzeige"]));
            Assert.Equal(K(Bausteinkategorie.Gebaeude), Kategorie(nachName["gebaeude.name"]));
            Assert.Equal(K(Bausteinkategorie.Bilder), Kategorie(nachName[Vorlagenfeldkatalog.LOGO]));
            Assert.Equal(K(Bausteinkategorie.Tabellen), Kategorie(nachName[Vorlagenfeldkatalog.MUSTER_TABELLE]));
            Assert.Equal(K(Bausteinkategorie.Tabellen), Kategorie(nachName["tabelle.komponenten.matrix"]));
            Assert.Equal(K(Bausteinkategorie.Bloecke), Kategorie(nachName["hat.kaelte"]));
            Assert.All(teile.Where(t => Vorlagenpruefer.IstPaarschluessel(Name(t))),
                       t => Assert.Equal(K(Bausteinkategorie.Paarsicht), Kategorie(t)));
            Assert.Equal(10, teile.Select(Kategorie).Distinct().Count());
            Assert.StartsWith("EPOS · ", Kategorie(teile[0]), StringComparison.Ordinal);
        }

        /// <summary>Der Inhalt eines Bausteins in der Form seiner Art, jeder Platzhalterlauf mit <c>w:noProof</c>.</summary>
        private static void PruefeInhalt(string datei, Vorlagenfeld f, DocPart t)
        {
            string marke = "{{" + f.Schluessel + "}}";
            Behavior verhalten = t.DocPartProperties.GetFirstChild<Behaviors>()?.GetFirstChild<Behavior>();
            foreach (Run r in t.DocPartBody.Descendants<Run>().Where(r => r.InnerText.Contains("{{", StringComparison.Ordinal)))
                Assert.True(r.RunProperties?.NoProof != null, datei + ", " + f.Schluessel + ": Platzhalterlauf ohne w:noProof.");

            if (f.Schluessel == Vorlagenfeldkatalog.MUSTER_TABELLE)
            {
                Table tabelle = Assert.Single(t.DocPartBody.Elements<Table>());
                Assert.Equal(marke, tabelle.GetFirstChild<TableProperties>().GetFirstChild<TableDescription>().Val.Value);
                return;
            }
            switch (f.Art)
            {
                case Vorlagenfeldart.Bild:
                    DW.DocProperties docPr = Assert.Single(t.DocPartBody.Descendants<DW.DocProperties>());
                    Assert.Equal(marke, docPr.Description?.Value);
                    Assert.Equal("rIdBausteinbild", Assert.Single(t.DocPartBody.Descendants<A.Blip>()).Embed?.Value);
                    Assert.Equal(DocPartBehaviorValues.Paragraph, verhalten.Val.Value);
                    break;
                case Vorlagenfeldart.Schalter:
                    Assert.Equal(new[] { "{{#wenn " + f.Schluessel + "}}", "", "{{/wenn}}" }, Absaetze(t));
                    Assert.Equal(DocPartBehaviorValues.Paragraph, verhalten.Val.Value);
                    break;
                case Vorlagenfeldart.Text:
                case Vorlagenfeldart.Zahl:
                case Vorlagenfeldart.Datum:
                    Assert.Equal(new[] { marke }, Absaetze(t));
                    Assert.Equal(DocPartBehaviorValues.Content, verhalten.Val.Value);
                    break;
                default:
                    // Liste, Kapitel, Tabelle: allein im Absatz.
                    Assert.Equal(new[] { marke }, Absaetze(t));
                    Assert.Equal(DocPartBehaviorValues.Paragraph, verhalten.Val.Value);
                    break;
            }
        }

        // =====================================================================
        //  Aktuell und aus der Standardvorlage
        // =====================================================================

        /// <summary>
        /// Die ausgelieferte Datei ist die, die der Kern aus der ausgelieferten Standardvorlage erzeugt — sonst ist das
        /// Werkzeug neu zu ziehen (<c>bausteine</c>). Der Rumpf ist der der Standardvorlage, Byte für Byte; <c>custom.xml</c>
        /// nennt Word-Fassung und Art, aber keine Sprache (der Rumpf ist sprachneutral).
        /// </summary>
        [Theory]
        [MemberData(nameof(Dateien))]
        public void Die_ausgelieferte_Bausteinvorlage_ist_die_des_Kerns_aus_der_Standardvorlage(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            string standard = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (pfad == null || standard == null) return;
            byte[] ausgeliefert = File.ReadAllBytes(pfad);
            byte[] erzeugt = WordBausteinvorlage.Erzeuge(File.ReadAllBytes(standard), englisch);

            Assert.True(BerichtsvorlagenCtrl.Inhaltsschluessel(ausgeliefert) == BerichtsvorlagenCtrl.Inhaltsschluessel(erzeugt),
                datei + " ist nicht aktuell — mit „dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- bausteine … --sprache "
                + (englisch ? "en" : "de") + "“ neu erzeugen.");
            Assert.Equal(Eintrag(File.ReadAllBytes(standard), "word/document.xml"), Eintrag(ausgeliefert, "word/document.xml"));

            using var ms = new MemoryStream(ausgeliefert);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ms, false);
            Dictionary<string, string> eigen = doc.CustomFilePropertiesPart.Properties.ChildElements
                .ToDictionary(e => e.GetAttribute("name", "").Value, e => e.InnerText, StringComparer.Ordinal);
            Assert.Equal(Vorlagenfeldkatalog.KatalogfassungWord.ToString(System.Globalization.CultureInfo.InvariantCulture),
                         eigen[Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG]);
            Assert.Equal(WordBausteinvorlage.VORLAGENART, eigen[WordBaukasten.EIGENSCHAFT_VORLAGE]);
            Assert.False(eigen.ContainsKey(Vorlagenpruefer.EIGENSCHAFT_SPRACHE));
            Assert.Equal(WordBausteinvorlage.TEIL_GLOSSAR, doc.MainDocumentPart.GlossaryDocumentPart.Uri.ToString());
            Assert.Equal("/word/document.xml", doc.MainDocumentPart.Uri.ToString());
        }

        /// <summary>Zweimal erzeugt, derselbe Inhalt — und das Platzhalterbild ist ein gültiges PNG in festen Bytes.</summary>
        [Fact]
        public void Die_Erzeugung_ist_wiederholbar()
        {
            string standard = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.STANDARD);
            if (standard == null) return;
            byte[] quelle = File.ReadAllBytes(standard);
            Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(WordBausteinvorlage.Erzeuge(quelle, false)),
                         BerichtsvorlagenCtrl.Inhaltsschluessel(WordBausteinvorlage.Erzeuge(quelle, false)));
            Assert.NotEqual(BerichtsvorlagenCtrl.Inhaltsschluessel(WordBausteinvorlage.Erzeuge(quelle, false)),
                            BerichtsvorlagenCtrl.Inhaltsschluessel(WordBausteinvorlage.Erzeuge(quelle, true)));

            byte[] png = WordBausteinvorlage.Platzhalterbild();
            Assert.Equal(png, WordBausteinvorlage.Platzhalterbild());
            using SkiaSharp.SKBitmap bild = SkiaSharp.SKBitmap.Decode(png);
            Assert.Equal(WordBausteinvorlage.BILD_BREITE, bild.Width);
            Assert.Equal(WordBausteinvorlage.BILD_HOEHE, bild.Height);
        }

        // =====================================================================
        //  Füllweg
        // =====================================================================

        /// <summary>
        /// Die <c>.dotx</c> selbst als gewählte Vorlage füllt 1030 ohne Prüferfehler: Der Bericht ist ein Dokument ohne
        /// Glossar, kein Teil des Pakets trägt noch <c>{{</c>, der Validator ist grün; die Laufmeldung nennt die Umstellung
        /// und die Zahl der entfernten Schnellbausteine.
        /// </summary>
        [Theory]
        [MemberData(nameof(Dateien))]
        public void Die_Bausteinvorlage_als_Vorlage_fuellt_1030_zu_einem_Dokument_ohne_Bausteine(string datei, bool englisch)
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(datei);
            if (pfad == null || !File.Exists(pfad)) return;
            byte[] vorlage = File.ReadAllBytes(pfad);
            Fuellergebnis ergebnis = Fuelle(vorlage, datei, englisch, "bausteine");
            if (ergebnis == null) return;

            int bausteine = WordBausteinvorlage.Bausteine(Vorlagenfeldkatalog.KatalogfassungWord).Count;
            Assert.Contains(WordVorlagentexte.F(englisch, WordVorlagentexte.DOTX), ergebnis.Hinweise);
            Assert.Contains(WordVorlagentexte.F(englisch, WordVorlagentexte.SCHNELLBAUSTEINE, bausteine), ergebnis.Hinweise);
        }

        /// <summary>
        /// Ein Dokument, das Word aus der Bausteinvorlage anlegt (Datei › Neu): Inhaltstyp Dokument, ohne Glossar, mit dem
        /// Verweis auf die Vorlage in den Einstellungen — es füllt 1030 wie die Standardvorlage; den Verweis nennt ein Hinweis.
        /// </summary>
        [Fact]
        public void Ein_Dokument_aus_der_Bausteinvorlage_fuellt_1030()
        {
            string pfad = BerichtsvorlageDateiWacheTests.Pfad(BerichtsvorlageDateiWacheTests.BAUSTEINE);
            if (pfad == null || !File.Exists(pfad)) return;

            byte[] dokument;
            using (var ms = new MemoryStream())
            {
                byte[] vorlage = File.ReadAllBytes(pfad);
                ms.Write(vorlage, 0, vorlage.Length);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, true))
                {
                    doc.ChangeDocumentType(WordprocessingDocumentType.Document);
                    MainDocumentPart main = doc.MainDocumentPart;
                    main.DeletePart(main.GlossaryDocumentPart);
                    DocumentSettingsPart einstellungen = main.DocumentSettingsPart;
                    ExternalRelationship verweis = einstellungen.AddExternalRelationship(
                        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/attachedTemplate",
                        new Uri("file:///C:/Vorlagen/Berichtsvorlage_Bausteine.dotx"));
                    einstellungen.Settings.PrependChild(new AttachedTemplate { Id = verweis.Id });
                    einstellungen.Settings.Save();
                }
                dokument = ms.ToArray();
            }

            Fuellergebnis ergebnis = Fuelle(dokument, "Bericht aus Bausteinen.docx", false, "dokument");
            if (ergebnis == null) return;
            Assert.DoesNotContain(WordVorlagentexte.F(false, WordVorlagentexte.DOTX), ergebnis.Hinweise);
            Assert.Contains(ergebnis.Hinweise, h => h.StartsWith(WordVorlagentexte.DOKUMENTVORLAGE.Replace("{0}", ""), StringComparison.Ordinal));
        }

        /// <summary>Prüfer und Lauf für 1030; der Bericht ist ein Dokument ohne Glossar und ohne <c>{{</c>. <c>null</c> ohne Testdatenbank.</summary>
        private Fuellergebnis Fuelle(byte[] vorlage, string datei, bool englisch, string art)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return null;
            int vorher = Sprache.Nummer;
            try
            {
                using var kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
                Sprache.Nummer = englisch ? 1 : 0;
                BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();

                Pruefbefund befund = Vorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll, Pruefkontext.Aus(konfig, englisch, 1, datei));
                foreach (Pruefmeldung m in befund.Meldungen) _ausgabe.WriteLine("Prüfer: " + m.Stufe + ": " + m.Text);
                Assert.Empty(befund.Meldungen.Where(m => m.Stufe == Befundstufe.Fehler).Select(m => m.Text));
                Assert.Empty(befund.UnbekannteSchluessel);
                Assert.False(befund.SpracheAbweichend);

                string ziel = Path.Combine(_ordner, art + (englisch ? "_en" : "") + ".docx");
                Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(
                    BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030), konfig, vorlage,
                    new Erstellerangaben { Firma = "Firma 1", Version = "1.0" }, ziel);
                foreach (string m in ergebnis.Hinweise.Concat(ergebnis.Warnungen).Concat(ergebnis.Fehler)) _ausgabe.WriteLine("Lauf: " + m);
                Assert.Empty(ergebnis.Unbekannte.Select(u => u.Normalform + " @ " + u.Fundort));
                Assert.Empty(ergebnis.Fehler);

                using (WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false))
                {
                    Assert.Equal(WordprocessingDocumentType.Document, doc.DocumentType);
                    Assert.Null(doc.MainDocumentPart.GlossaryDocumentPart);
                    List<string> fehler = BerichtsvorlageDateiWacheTests.Validatorfehler(doc);
                    Assert.True(fehler.Count == 0, string.Join(" | ", fehler.Take(5)));
                }
                using (ZipArchive zip = ZipFile.OpenRead(ziel))
                    foreach (ZipArchiveEntry e in zip.Entries.Where(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
                    {
                        using var leser = new StreamReader(e.Open(), Encoding.UTF8);
                        Assert.False(leser.ReadToEnd().Contains("{{", StringComparison.Ordinal), art + ": „{{“ in " + e.FullName);
                        Assert.False(e.FullName.Contains("glossary", StringComparison.OrdinalIgnoreCase), art + ": " + e.FullName);
                    }
                return ergebnis;
            }
            finally { Sprache.Nummer = vorher; }
        }

        // =====================================================================
        //  Bereinigung: Platzhaltertexte bleiben
        // =====================================================================

        /// <summary>
        /// Eine eigene Vorlage mit Glossar: Schnellbausteine und AutoText fallen weg, der Platzhaltertext eines
        /// Inhaltssteuerelements (Galerie <c>placeholder</c>) bleibt samt Glossarteil; ohne ihn entfällt der Teil ganz.
        /// </summary>
        [Fact]
        public void Die_Bereinigung_entfernt_Bausteine_und_behaelt_Platzhaltertexte()
        {
            using (WordprocessingDocument doc = Glossarprobe(true))
            {
                Assert.Equal(2, WordVorlagenbereinigung.EntferneSchnellbausteine(doc.MainDocumentPart));
                DocPart bleibt = Assert.Single(doc.MainDocumentPart.GlossaryDocumentPart.GlossaryDocument.DocParts.Elements<DocPart>());
                Assert.Equal("Platzhalter", Name(bleibt));
            }
            using (WordprocessingDocument doc = Glossarprobe(false))
            {
                Assert.Equal(2, WordVorlagenbereinigung.EntferneSchnellbausteine(doc.MainDocumentPart));
                Assert.Null(doc.MainDocumentPart.GlossaryDocumentPart);
            }
        }

        private static WordprocessingDocument Glossarprobe(bool mitPlatzhalter)
        {
            var ms = new MemoryStream();
            WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text("Probe")))));
            GlossaryDocumentPart glossar = main.AddNewPart<GlossaryDocumentPart>();
            var teile = new DocParts(Probeteil("Baustein", DocPartGalleryValues.DocumentPart), Probeteil("AutoText", DocPartGalleryValues.AutoText));
            if (mitPlatzhalter) teile.Append(Probeteil("Platzhalter", DocPartGalleryValues.Placeholder));
            glossar.GlossaryDocument = new GlossaryDocument(teile);
            return doc;
        }

        private static DocPart Probeteil(string name, DocPartGalleryValues galerie)
            => new DocPart(
                new DocPartProperties(new DocPartName { Val = name },
                                      new Category(new Name { Val = "Probe" }, new Gallery { Val = galerie })),
                new DocPartBody(new Paragraph(new Run(new Text(name)))));

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static string Name(DocPart t) => t.DocPartProperties?.GetFirstChild<DocPartName>()?.Val?.Value ?? "";

        private static string Kategorie(DocPart t)
            => t.DocPartProperties?.GetFirstChild<Category>()?.GetFirstChild<Name>()?.Val?.Value ?? "";

        private static DocPartGalleryValues? Galerie(DocPart t)
            => t.DocPartProperties?.GetFirstChild<Category>()?.GetFirstChild<Gallery>()?.Val?.Value;

        private static string[] Absaetze(DocPart t)
            => t.DocPartBody.Elements<Paragraph>().Select(p => string.Concat(p.Descendants<Text>().Select(x => x.Text))).ToArray();

        private static byte[] Eintrag(byte[] paket, string name)
        {
            using var ms = new MemoryStream(paket);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            using Stream s = zip.GetEntry(name).Open();
            using var aus = new MemoryStream();
            s.CopyTo(aus);
            return aus.ToArray();
        }
    }
}
