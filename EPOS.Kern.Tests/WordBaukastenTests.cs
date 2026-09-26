using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using SVG = DocumentFormat.OpenXml.Office2019.Drawing.SVG;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Baukasten der Word-Vorlagen</b> (Konzept Berichtsvorlagen 6.3 Nr. 3, 12; Etappe BV-E5):
    /// <see cref="WordBaukasten"/> erzeugt aus dem Katalog ein Dokument, das jeden Platzhalter mit Ausgabe Word
    /// der Fassung genau einmal trägt, gegliedert nach Kontext.
    ///
    /// <para><b>Was sie halten.</b> (a) Gliederung: jeder Eintrag genau einmal, im Abschnitt seines Kontexts,
    /// die Paarschlüssel für sich, die Mustertabelle einmal; keine Beschreibung bildet eine Marke. (b) Das
    /// Dokument: Validator grün, jede Marke mit <c>w:noProof</c>, je Bildeintrag ein Musterbild mit dem
    /// Schlüssel im Alternativtext, Fassung, Art und Sprache in <c>custom.xml</c>, zweimal erzeugt derselbe
    /// Inhalt, die volle Prüfung ohne Fehler. (c) <b>Der Rundlauf</b> (Konzept 12): de und en, gefüllt mit
    /// 1030, der Gruppe 1019 und der synthetischen Gruppe mit drei Ständen — keine Prüferfehler, kein
    /// unbekannter Schlüssel, kein <c>{{</c> übrig, Validator grün in allen Fassungen 2007 bis 2021, jede
    /// Bildstelle mit PNG und SVG. (d) <b>Produktdaten:</b> der Text des Baukastens nennt keinen Katalognamen
    /// der Testdatenbank (Regel und Prüfung wie <see cref="WikiProduktdatenWacheTests"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WordBaukastenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e5-baukasten");

        public WordBaukastenTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            Probevorlagen.Aufraeumen(_ordner);
            _kultur.Dispose();
        }

        /// <summary>Die Gruppe der Testdatenbank mit zwei Varianten und Kraftwerkspark.</summary>
        private const int GRUPPE_1019 = 1019;

        private const int FASSUNG = Vorlagenfeldkatalog.KATALOGFASSUNG;

        // =====================================================================
        //  (a) Gliederung
        // =====================================================================

        /// <summary>
        /// Jeder Eintrag mit Ausgabe Word und <c>Seit</c> ≤ Fassung steht genau einmal im Baukasten, in dem
        /// Abschnitt seines Kontexts — außer den Paarschlüsseln: Die deckt die Musterregel (Anwenderentscheid
        /// BV-E5-4, Lesart b). Jeder Paarschlüssel hat sein Vorbild <c>stand.&lt;rest&gt;</c> im Abschnitt „Je Stand“,
        /// und der Abschnitt Paarsicht führt nur wenige Beispiele (Text, Kennzahl, Wirtschaftlichkeit).
        /// <c>muster.tabelle</c> steht allein. Eine ältere Fassung führt nur ihre Einträge.
        /// </summary>
        [Fact]
        public void Jeder_Eintrag_der_Fassung_steht_genau_einmal_im_Abschnitt_seines_Kontexts()
        {
            IReadOnlyList<Baukastenabschnitt> abschnitte = WordBaukasten.Abschnitte(FASSUNG);
            List<Vorlagenfeld> soll = Vorlagenfeldkatalog.Alle
                .Where(f => f.Seit <= FASSUNG && (f.Ausgaben & Vorlagenausgabe.Word) != 0).ToList();
            List<Vorlagenfeld> ist = abschnitte.SelectMany(a => a.Eintraege).ToList();

            List<Vorlagenfeld> paare = soll.Where(f => Vorlagenpruefer.IstPaarschluessel(f.Schluessel)).ToList();
            List<Vorlagenfeld> ohnePaar = ist.Where(f => !Vorlagenpruefer.IstPaarschluessel(f.Schluessel)).ToList();
            Assert.Equal(ohnePaar.Count, ohnePaar.Select(f => f.Schluessel).Distinct().Count());
            Assert.Equal(soll.Except(paare).Select(f => f.Schluessel).OrderBy(s => s, StringComparer.Ordinal),
                         ohnePaar.Select(f => f.Schluessel).OrderBy(s => s, StringComparer.Ordinal));

            // Die Musterregel deckt jeden Paarschlüssel: sein Vorbild steht im Abschnitt „Je Stand“.
            var stand = new HashSet<string>(abschnitte.Single(a => a.Art == Baukastenabschnittsart.Stand).Eintraege
                                                      .Select(f => f.Schluessel), StringComparer.Ordinal);
            Assert.NotEmpty(paare);
            Assert.All(paare, f => Assert.Contains("stand." + f.Schluessel.Substring("stand.a.".Length), stand));

            foreach (Baukastenabschnitt a in abschnitte)
            {
                _ausgabe.WriteLine(a.Art + ": " + a.Eintraege.Count + " (" +
                                   string.Join(", ", a.Eintraege.GroupBy(f => f.Art).Select(g => g.Key + " " + g.Count())) + ")");
                foreach (Vorlagenfeld f in a.Eintraege) Assert.Equal(a.Art, WordBaukasten.Abschnitt(f));
            }

            Baukastenabschnitt paar = abschnitte.Single(a => a.Art == Baukastenabschnittsart.Paarsicht);
            Assert.All(paar.Eintraege, f => Assert.True(f.Schluessel.StartsWith("stand.a.", StringComparison.Ordinal) ||
                                                        f.Schluessel.StartsWith("stand.b.", StringComparison.Ordinal)));
            Assert.InRange(paar.Eintraege.Count, 1, 3);
            Assert.Contains(paar.Eintraege, f => f.Art == Vorlagenfeldart.Text);
            Assert.Contains(paar.Eintraege, f => f.Schluessel.StartsWith("stand.b.kennzahl.", StringComparison.Ordinal));
            Assert.Contains(paar.Eintraege, f => f.Schluessel.StartsWith("stand.a.wirtschaft.", StringComparison.Ordinal));
            Assert.Equal(Vorlagenfeldkatalog.MUSTER_TABELLE,
                         Assert.Single(abschnitte.Single(a => a.Art == Baukastenabschnittsart.Mustertabelle).Eintraege).Schluessel);
            Assert.All(abschnitte.Single(a => a.Art == Baukastenabschnittsart.Stand).Eintraege,
                       f => Assert.Equal(Vorlagenfeldkontext.Stand, f.Kontext));

            // Eine ältere Fassung führt nur ihre Einträge.
            Assert.All(WordBaukasten.Abschnitte(1).SelectMany(a => a.Eintraege), f => Assert.True(f.Seit <= 1, f.Schluessel));
            Assert.True(WordBaukasten.Abschnitte(1).Sum(a => a.Eintraege.Count) < ohnePaar.Count);
        }

        /// <summary>
        /// Keine Beschreibung bildet im Baukasten eine Marke — sonst läse die Engine den Beschreibungstext als
        /// Platzhalter (die Schalter nennen „{{#wenn …}}“); jede hat einen Text in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Keine_Beschreibung_bildet_eine_Marke()
        {
            int mitKlammern = 0;
            foreach (bool englisch in new[] { false, true })
                foreach (Vorlagenfeld f in WordBaukasten.Abschnitte(FASSUNG).SelectMany(a => a.Eintraege))
                {
                    string roh = Vorlagenfeldkatalog.Beschreibung(f, englisch);
                    if (roh.Contains("{{")) mitKlammern++;
                    string b = WordBaukasten.Entschaerft(roh);
                    Assert.False(b.Contains("{{") || b.Contains("}}"), f.Schluessel + ": " + b);
                    Assert.Empty(Platzhaltersyntax.Finde(b));
                    Assert.False(string.IsNullOrWhiteSpace(b), f.Schluessel + " ohne Beschreibung (" + (englisch ? "en" : "de") + ")");
                }
            _ausgabe.WriteLine("Beschreibungen mit Klammern: " + mitKlammern);
            Assert.Equal("nur in #wenn x.", WordBaukasten.Entschaerft("nur in {{#wenn x}}."));
        }

        // =====================================================================
        //  (b) Das Dokument
        // =====================================================================

        /// <summary>
        /// Der Baukasten selbst: gültiges OpenXML (die Mustertabelle trägt ihren Alternativtext als
        /// <c>w:tblDescription</c> — ein Element ab Office 2010, deshalb 2010 bis 2021), jede Marke mit
        /// <c>w:noProof</c>, je Bildeintrag ein Musterbild mit dem Schlüssel im Alternativtext, Fassung, Art und
        /// Sprache in <c>custom.xml</c>; zweimal erzeugt derselbe Rumpf. Die volle Prüfung meldet keinen Fehler
        /// und keinen unbekannten Schlüssel — in JEDER Sicht: Stamm allein, Sicht 1 mit zwei Varianten und die
        /// Paarsicht. Die Paarbeispiele stehen als Text, kein Platzhalter <c>stand.a.*</c>/<c>stand.b.*</c>.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Der_Baukasten_ist_gueltig_und_ohne_Pruefbefund(bool englisch)
        {
            byte[] baukasten = WordBaukasten.Erzeuge(englisch);
            string pfad = Path.Combine(_ordner, "baukasten_" + (englisch ? "en" : "de") + ".docx");
            File.WriteAllBytes(pfad, baukasten);
            _ausgabe.WriteLine("Baukasten " + (englisch ? "en" : "de") + ": " + baukasten.Length + " Byte");

            using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
            {
                foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen.Where(f => f >= FileFormatVersions.Office2010))
                {
                    List<ValidationErrorInfo> fehler = new OpenXmlValidator(fassung).Validate(doc).Take(5).ToList();
                    Assert.True(fehler.Count == 0, fassung + ": " + string.Join(" | ", fehler.Select(f => f.Description + " @ " + f.Path?.XPath)));
                }

                Body body = doc.MainDocumentPart.Document.Body;
                List<Run> marken = body.Descendants<Run>().Where(r => r.InnerText.Contains("{{")).ToList();
                Assert.NotEmpty(marken);
                Assert.All(marken, r => Assert.NotNull(r.RunProperties?.GetFirstChild<NoProof>()));

                int bilder = WordBaukasten.Abschnitte(FASSUNG).SelectMany(a => a.Eintraege).Count(f => f.Art == Vorlagenfeldart.Bild);
                List<DW.DocProperties> rahmen = body.Descendants<DW.DocProperties>().ToList();
                Assert.Equal(bilder, rahmen.Count);
                Assert.All(rahmen, d => Assert.Matches(@"^\{\{[a-z0-9_.]+\}\}$", d.Description?.Value ?? ""));
                Assert.Equal(rahmen.Count, rahmen.Select(d => d.Id.Value).Distinct().Count());
                Assert.Single(body.Descendants<Table>(), t => Tabellenmuster.IstMuster(t));

                var eigenschaften = doc.CustomFilePropertiesPart.Properties
                    .Elements<DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty>()
                    .ToDictionary(e => e.Name.Value, e => e.InnerText);
                Assert.Equal(FASSUNG.ToString(), eigenschaften[Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG]);
                Assert.Equal(WordBaukasten.VORLAGENART, eigenschaften[WordBaukasten.EIGENSCHAFT_VORLAGE]);
                Assert.Equal(englisch ? "en" : "de", eigenschaften[Vorlagenpruefer.EIGENSCHAFT_SPRACHE]);
            }

            // Wiederholbar: derselbe Rumpf, dieselben Bilder.
            Assert.Equal(Rumpf(baukasten), Rumpf(WordBaukasten.Erzeuge(englisch)));

            // Die volle Prüfung: Stamm allein und Paarsicht ohne Fehler.
            foreach (Pruefkontext kontext in new[]
                     {
                         new Pruefkontext { AnzahlVarianten = 0, Englisch = englisch, Dateiname = "Baukasten.docx" },
                         new Pruefkontext { AnzahlVarianten = 2, Sicht = 1, Englisch = englisch, Dateiname = "Baukasten.docx" },
                         new Pruefkontext { AnzahlVarianten = 2, Sicht = 2, Englisch = englisch, Dateiname = "Baukasten.docx" },
                     })
            {
                Pruefbefund befund = Vorlagenpruefer.Pruefe(baukasten, Pruefstufe.Voll, kontext);
                foreach (Pruefmeldung m in befund.Meldungen) _ausgabe.WriteLine(kontext.AnzahlVarianten + "/" + kontext.Sicht + " " + m.Stufe + ": " + m.Text);
                Assert.Empty(befund.UnbekannteSchluessel);
                Assert.DoesNotContain(befund.Meldungen, m => m.Stufe == Befundstufe.Fehler);
            }
            string text = Text(baukasten);
            Assert.DoesNotContain("{{stand.a.", text);
            Assert.DoesNotContain("{{stand.b.", text);
            foreach (Vorlagenfeld f in WordBaukasten.Abschnitte(FASSUNG).Single(a => a.Art == Baukastenabschnittsart.Paarsicht).Eintraege)
                Assert.Contains(f.Schluessel, text);
        }

        // =====================================================================
        //  (c) Der Rundlauf
        // =====================================================================

        /// <summary>
        /// <b>Der Rundlauf</b> (Konzept 12, Abnahme BV-E5): der Baukasten in der Sprache des Laufs, geprüft und
        /// gefüllt mit dem Bedarf der Vorlage nach dem Sammler — 1030 (Stamm allein, Sicht 1) und die Gruppe 1019
        /// (zwei Varianten) in Sicht 1 und in der Paarsicht Stamm gegen die erste Variante. Keine Prüferfehler, kein unbekannter
        /// Schlüssel, kein <c>{{</c> übrig, Validator grün in allen Fassungen, jede Bildstelle PNG mit SVG.
        /// </summary>
        [Theory]
        [InlineData(Berichtsdatenproben.PROJEKT_1030, false, 1)]
        [InlineData(Berichtsdatenproben.PROJEKT_1030, true, 1)]
        [InlineData(GRUPPE_1019, false, 1)]
        [InlineData(GRUPPE_1019, true, 1)]
        [InlineData(GRUPPE_1019, false, 2)]
        [InlineData(GRUPPE_1019, true, 2)]
        public void Rundlauf_mit_der_Testdatenbank(int stamm, bool englisch, int sicht)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var sprache = new Sprachwahl(englisch);

            List<int> varianten = new VariantenCtrl().LadeGruppe(stamm, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            byte[] baukasten = WordBaukasten.Erzeuge(englisch);
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            konfig.VariantenIds = varianten;
            Pruefkontext kontext = new Pruefkontext { AnzahlVarianten = varianten.Count, Sicht = sicht, Englisch = englisch };
            Pruefbefund befund = Vorlagenpruefer.Pruefe(baukasten, Pruefstufe.Voll, kontext);
            Assert.DoesNotContain(befund.Meldungen, m => m.Stufe == Befundstufe.Fehler);

            BerichtsDaten daten = new BerichtsDatenSammler().SammleFuerBericht(stamm, "Probe " + stamm, varianten,
                Berichtsbedarf.AusVorlage(befund, konfig), null, CancellationToken.None, null);
            if (sicht == 2) daten.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = stamm, IdB = varianten[0] };

            PruefeGefuellt(baukasten, daten, konfig, stamm + "_sicht" + sicht + (englisch ? "_en" : "_de"));
        }

        /// <summary>
        /// Der Rundlauf mit der synthetischen Gruppe aus drei Ständen samt Wirtschaftlichkeit und Wirkungen (jedes
        /// Bild mit Modell), in Sicht 1 und in der Paarsicht Variante A gegen Variante B.
        /// </summary>
        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 2)]
        public void Rundlauf_mit_der_synthetischen_Gruppe(bool englisch, int sicht)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var sprache = new Sprachwahl(englisch);

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, 3);
            Assert.Equal(3, daten.Varianten.Count);
            if (sicht == 2)
                daten.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = daten.Varianten[1].IdProjekt, IdB = daten.Varianten[2].IdProjekt };
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            konfig.VariantenIds = daten.Varianten.Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();

            byte[] baukasten = WordBaukasten.Erzeuge(englisch);
            Pruefbefund befund = Vorlagenpruefer.Pruefe(baukasten, Pruefstufe.Voll,
                new Pruefkontext { AnzahlVarianten = 2, Sicht = sicht, Englisch = englisch });
            Assert.DoesNotContain(befund.Meldungen, m => m.Stufe == Befundstufe.Fehler);

            PruefeGefuellt(baukasten, daten, konfig, "gruppe3_sicht" + sicht + (englisch ? "_en" : "_de"));
        }

        /// <summary>Füllt den Baukasten und hält den Rundlauf: kein Unbekannter, kein <c>{{</c>, Validator, Bildstellen.</summary>
        private void PruefeGefuellt(byte[] baukasten, BerichtsDaten daten, BerichtsKonfiguration konfig, string name)
        {
            string ziel = Path.Combine(_ordner, "gefuellt_" + name + ".docx");
            var uhr = System.Diagnostics.Stopwatch.StartNew();
            Fuellergebnis ergebnis = new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, baukasten,
                new Erstellerangaben { Firma = "Musterfirma", Version = "9.9.9.9" }, ziel);
            uhr.Stop();
            _ausgabe.WriteLine(name + ": gefüllt in " + uhr.ElapsedMilliseconds + " ms, " + ergebnis.Ersetzt + " ersetzt, " +
                               new FileInfo(ziel).Length + " Byte");
            foreach (string m in ergebnis.Meldungen().Take(20)) _ausgabe.WriteLine("  " + m);

            Assert.Empty(ergebnis.Unbekannte);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            MainDocumentPart main = doc.MainDocumentPart;
            var teile = new List<OpenXmlPart> { main };
            teile.AddRange(main.HeaderParts);
            teile.AddRange(main.FooterParts);
            foreach (OpenXmlPart teil in teile)
            {
                string text = teil.RootElement?.InnerText ?? "";
                Assert.False(text.Contains("{{"), name + ": „{{“ übrig in " + teil.Uri + ": " +
                             Umgebung(text, text.IndexOf("{{", StringComparison.Ordinal)));
            }
            Assert.DoesNotContain(main.Document.Body.Descendants<Table>(), t => Tabellenmuster.IstMuster(t));

            // Jede Bildstelle: PNG mit SVG-Fassung, Alternativtext ohne Marke.
            List<DW.DocProperties> rahmen = main.Document.Body.Descendants<DW.DocProperties>().ToList();
            Assert.NotEmpty(rahmen);
            foreach (DW.DocProperties d in rahmen)
            {
                Assert.DoesNotContain("{{", d.Description?.Value ?? "");
                A.Blip blip = d.Parent.Descendants<A.Blip>().Single();
                Assert.Equal("image/png", main.GetPartById(blip.Embed.Value).ContentType);
                SVG.SVGBlip svg = blip.Descendants<SVG.SVGBlip>().SingleOrDefault();
                Assert.True(svg != null, name + ": Bild ohne SVG — " + d.Name + " / " + d.Description);
                Assert.Equal("image/svg+xml", main.GetPartById(svg.Embed.Value).ContentType);
            }
            _ausgabe.WriteLine(name + ": " + rahmen.Count + " Bildstellen");

            foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
            {
                List<ValidationErrorInfo> fehler = new OpenXmlValidator(fassung).Validate(doc).Take(5).ToList();
                Assert.True(fehler.Count == 0, name + " · " + fassung + ": " +
                            string.Join(" | ", fehler.Select(f => f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath)));
            }
        }

        // =====================================================================
        //  (d) Produktdaten
        // =====================================================================

        /// <summary>
        /// <b>Die Produktdatenwache des Baukastens</b> (Konzept 12 „Produktdaten“): Der Text beider Sprachen —
        /// Rumpf samt Alternativtexten der Musterbilder — nennt keinen Katalognamen der Testdatenbank, keinen
        /// Hersteller der festen Liste und keinen Typcode. Eine Gegenprobe zeigt, dass die Regel greift.
        /// </summary>
        [Fact]
        public void Der_Baukasten_nennt_keine_Produktdaten()
        {
            using var db = new TestDatenbank();
            var wache = new WikiProduktdatenWacheTests(db);
            try
            {
                var funde = new List<string>();
                foreach (bool englisch in new[] { false, true })
                    funde.AddRange(wache.FundstellenIn("Baukasten_" + (englisch ? "en" : "de"), Text(WordBaukasten.Erzeuge(englisch))));
                Assert.True(funde.Count == 0, "Der Baukasten nennt Produktdaten:\n" + string.Join("\n", funde.Take(20)));

                Assert.NotEmpty(wache.FundstellenIn("Gegenprobe", "Ein Modul JKM400M liefert 400 W."));
            }
            finally { wache.Dispose(); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Der Text eines Baukastens: je Absatz eine Zeile, dazu die Alternativtexte der Bilder.</summary>
        private static string Text(byte[] docx)
        {
            using var ms = new MemoryStream(docx);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ms, false);
            Body body = doc.MainDocumentPart.Document.Body;
            IEnumerable<string> absaetze = body.Descendants<Paragraph>().Select(p => p.InnerText);
            IEnumerable<string> alt = body.Descendants<DW.DocProperties>().Select(d => d.Description?.Value ?? "");
            return string.Join("\n", absaetze.Concat(alt));
        }

        /// <summary>Der Rumpf als XML — für den Vergleich zweier Erzeugungen.</summary>
        private static string Rumpf(byte[] docx)
        {
            using var ms = new MemoryStream(docx);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ms, false);
            return doc.MainDocumentPart.Document.OuterXml + "|" +
                   string.Join(",", doc.MainDocumentPart.ImageParts.Select(p => { using Stream s = p.GetStream(); return s.Length.ToString(); }));
        }

        private static string Umgebung(string text, int i)
        {
            if (i < 0) return "";
            int von = Math.Max(0, i - 60);
            return text.Substring(von, Math.Min(text.Length - von, 160));
        }

        /// <summary>Stellt die Sprache des Berichts (Sprachnummer und Kultur) für einen Fall um.</summary>
        private sealed class Sprachwahl : IDisposable
        {
            private readonly int _vorher = Sprache.Nummer;
            private readonly Kulturvorrichtung _kultur;

            public Sprachwahl(bool englisch)
            {
                Sprache.Nummer = englisch ? 1 : 0;
                _kultur = new Kulturvorrichtung(englisch ? "en-US" : "de-DE");
            }

            public void Dispose()
            {
                _kultur.Dispose();
                Sprache.Nummer = _vorher;
            }
        }
    }
}
