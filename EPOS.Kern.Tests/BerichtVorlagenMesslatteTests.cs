using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Messlatte der Berichtsvorlagen</b> (Konzept Berichtsvorlagen mit Platzhaltern,
    /// Etappe BV-E0; Abschnitt 11 „Migration“ Nr. 1 und 5).
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtVorlagenMesslatteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public BerichtVorlagenMesslatteTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose() => _kultur.Dispose();

        /// <summary>Probe „Referenzprojekt 1030“ (<see cref="Berichtsdatenproben.Projektdaten1030"/>).</summary>
        public const string PROBE_1030 = "1030";

        /// <summary>Probe „synthetische Gruppe“ (<see cref="Berichtsdatenproben.Gruppendaten"/>).</summary>
        public const string PROBE_GRUPPE = "Gruppe";

        // =====================================================================
        //  1 — der Bericht mit der echten Vorlage
        // =====================================================================

        /// <summary>
        /// <b>Mit echter Vorlage.</b> Der Wortbericht, erzeugt aus der Berichtsvorlage des
        /// Repositoriums mit allen Bausteinen samt Wirtschaftlichkeit, ist (a) in jeder
        /// Office-Fassung von 2007 bis 2021 gültiges OpenXML und (b) endet mit der
        /// Abschnittsangabe der Vorlage: Hinter der letzten <c>w:sectPr</c> des Rumpfs steht
        /// kein Element. Beides traf die Wirkungstafel, solange sie am Einfügeanker vorbei an
        /// den Rumpf gehängt wurde (Konzept 2.4, Befund BW:678) — ohne Vorlage fällt das nicht
        /// auf, weil der Rumpf dann keine Abschnittsangabe hat.
        /// </summary>
        [Theory]
        [InlineData(PROBE_1030)]
        [InlineData(PROBE_GRUPPE)]
        public void Mit_echter_Vorlage_gueltig_und_nichts_hinter_der_Abschnittsangabe(string probe)
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "bericht.docx");
                new WordBerichtGenerator().Erzeuge(Probe(probe), Berichtsdatenproben.VolleKonfiguration(), ziel, vorlage);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                // Die Probe löst die Wirkungstafel aus — sonst prüfte der Fall die Stelle nicht.
                Assert.Contains(body.Descendants<Table>(), t => Kopf(t).FirstOrDefault() == R.WIRT_NM_SP_KATEGORIE);

                // (b) Hinter der letzten Abschnittsangabe steht nichts.
                SectionProperties sect = body.Elements<SectionProperties>().LastOrDefault();
                Assert.NotNull(sect);
                List<string> dahinter = sect.ElementsAfter().Select(Beschreibe).ToList();

                // (a) Der Validator in allen sechs Fassungen.
                var befunde = new List<string>();
                foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen)
                {
                    List<ValidationErrorInfo> alle = new OpenXmlValidator(fassung).Validate(doc).ToList();
                    List<ValidationErrorInfo> fehler = alle.Where(f => !Ausgenommen(f)).ToList();
                    _ausgabe.WriteLine(probe + " · " + fassung + ": " + alle.Count + " Meldungen, davon " +
                                       (alle.Count - fehler.Count) + " ausgenommen");
                    foreach (ValidationErrorInfo f in alle)
                        _ausgabe.WriteLine("   " + (Ausgenommen(f) ? "[ausgenommen] " : "") + Text(f));
                    if (fehler.Count > 0)
                        befunde.Add(fassung + ": " + fehler.Count + " Fehler — " +
                                    string.Join(" | ", fehler.Take(3).Select(Text)));
                }
                _ausgabe.WriteLine(probe + " · hinter der Abschnittsangabe: " + dahinter.Count +
                                   (dahinter.Count > 0 ? " (" + string.Join(", ", dahinter) + ")" : ""));

                Assert.True(dahinter.Count == 0 && befunde.Count == 0,
                    "Probe " + probe + ": " + dahinter.Count + " Element(e) hinter der Abschnittsangabe" +
                    (dahinter.Count > 0 ? " (" + string.Join(", ", dahinter) + ")" : "") +
                    (befunde.Count > 0 ? "; Validator: " + string.Join(" || ", befunde) : ""));
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>
        /// <b>AUSSCHLUSSLISTE — VORLÄUFIG.</b> Die Berichtsvorlage führt die Formatvorlagen
        /// <c>Title</c> und <c>Heading1</c>–<c>Heading3</c> in <c>word/styles.xml</c> ZWEIMAL
        /// (Konzept Berichtsvorlagen 2.1, gemessen); der Validator meldet je Doppel
        /// <c>Sem_UniqueAttributeValue</c> auf <c>w:styleId</c>, in jeder Office-Fassung vier
        /// Meldungen. Die Vorlage wird parallel bereinigt (BV-E0 „doppelte Stile bereinigen“);
        /// nach dem Zusammenführen entfällt diese Liste, und der Test muss ohne sie grün sein.
        /// Ausgenommen ist NUR diese Meldung an diesen vier Stilen — jede andere zählt.
        /// </summary>
        private static readonly string[] DoppelteStileDerVorlage = { "Title", "Heading1", "Heading2", "Heading3" };

        private static bool Ausgenommen(ValidationErrorInfo f)
            => f.Id == "Sem_UniqueAttributeValue"
               && f.Part != null && f.Part.Uri.ToString() == "/word/styles.xml"
               && f.Node is Style stil && stil.StyleId != null
               && DoppelteStileDerVorlage.Contains(stil.StyleId.Value);

        private static string Text(ValidationErrorInfo f)
            => f.Id + " @ " + (f.Part != null ? f.Part.Uri.ToString() : "?") + " " + f.Path?.XPath + ": " + f.Description;

        /// <summary>Der Berichtsbaum der Probe, mit Wirtschaftlichkeit und Wirkungen.</summary>
        internal static BerichtsDaten Probe(string probe, int staende = 2)
        {
            BerichtsDaten daten;
            WirtschaftlichkeitParameter p;
            if (probe == PROBE_1030)
            {
                daten = Berichtsdatenproben.Projektdaten1030();
                p = new WirtschaftlichkeitCtrl().LadeParameter(Berichtsdatenproben.PROJEKT_1030);
                p.IdStamm = Berichtsdatenproben.PROJEKT_1030;
            }
            else
            {
                daten = Berichtsdatenproben.Gruppendaten(staende);
                p = Berichtsdatenproben.Parametersatz(Berichtsdatenproben.STAMM);
            }
            Berichtsdatenproben.MitWirtschaftlichkeit(daten, p);
            Berichtsdatenproben.MitWirkungen(daten, p);
            return daten;
        }

        private static string Beschreibe(OpenXmlElement e)
        {
            string text = e.InnerText ?? "";
            return e.LocalName + (text.Length > 0 ? " „" + (text.Length > 40 ? text.Substring(0, 40) + "…" : text) + "“" : "");
        }

        private static string[] Kopf(Table t)
        {
            TableRow r = t.Elements<TableRow>().FirstOrDefault();
            return r == null ? new string[0] : r.Elements<TableCell>().Select(c => c.InnerText).ToArray();
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bv-e0-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
