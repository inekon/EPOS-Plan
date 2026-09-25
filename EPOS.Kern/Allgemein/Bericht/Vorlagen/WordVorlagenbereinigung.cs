using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Was die Engine an der Vorlage entfernt, bevor sie füllt</b> (Konzept Berichtsvorlagen 6.1,
    /// 6.7): alle Kommentare samt Bereichsmarken, Verweisen und Kommentarteilen, und jede externe
    /// Beziehung — verknüpfte Bilder und Mappen, andere Verknüpfungen, den Verweis auf die
    /// Dokumentvorlage (<c>w:attachedTemplate</c>). Nichts davon geschieht still: Die Zahl der
    /// Kommentare und jede Verknüpfung stehen im <see cref="Fuellergebnis"/>. Hyperlinks sind keine
    /// externen Beziehungen in diesem Sinn und bleiben.
    /// </summary>
    internal static class WordVorlagenbereinigung
    {
        /// <summary>Namensraum der Beziehungskennungen (<c>r:id</c>, <c>r:embed</c>, <c>r:link</c>).</summary>
        private const string NS_BEZIEHUNG = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // ------------------------------------------------------------- Kommentare

        /// <summary>
        /// Entfernt alle Kommentare: <c>commentRangeStart</c>/<c>commentRangeEnd</c> und
        /// <c>commentReference</c> in allen Wurzeln (ein Run, der nur die Referenz trug, entfällt mit)
        /// und die Kommentarteile. Rückgabe: die Zahl der Kommentare.
        /// </summary>
        internal static int EntferneKommentare(MainDocumentPart main, IEnumerable<OpenXmlElement> wurzeln)
        {
            var kennungen = new HashSet<string>(StringComparer.Ordinal);
            int imTeil = main.WordprocessingCommentsPart?.Comments?.Elements<Comment>().Count() ?? 0;

            foreach (OpenXmlElement wurzel in wurzeln.Where(w => w != null))
            {
                foreach (CommentRangeStart s in wurzel.Descendants<CommentRangeStart>().ToList())
                { if (s.Id?.Value != null) kennungen.Add(s.Id.Value); s.Remove(); }
                foreach (CommentRangeEnd e in wurzel.Descendants<CommentRangeEnd>().ToList())
                { if (e.Id?.Value != null) kennungen.Add(e.Id.Value); e.Remove(); }
                foreach (CommentReference r in wurzel.Descendants<CommentReference>().ToList())
                {
                    if (r.Id?.Value != null) kennungen.Add(r.Id.Value);
                    Run lauf = r.Parent as Run;
                    r.Remove();
                    if (lauf != null && !lauf.ChildElements.Any(c => !(c is RunProperties))) lauf.Remove();
                }
            }

            if (main.WordprocessingCommentsPart != null) main.DeletePart(main.WordprocessingCommentsPart);
            if (main.WordprocessingCommentsExPart != null) main.DeletePart(main.WordprocessingCommentsExPart);
            if (main.WordprocessingCommentsIdsPart != null) main.DeletePart(main.WordprocessingCommentsIdsPart);
            if (main.WordCommentsExtensiblePart != null) main.DeletePart(main.WordCommentsExtensiblePart);

            return Math.Max(imTeil, kennungen.Count);
        }

        // ------------------------------------------------------------- Externe Beziehungen

        /// <summary>
        /// Entfernt jede externe Beziehung aller Teile samt der Elemente, die auf sie verweisen, und
        /// meldet sie: den Verweis auf die Dokumentvorlage als Hinweis, verknüpfte Bilder und
        /// andere Verknüpfungen als Warnung.
        /// </summary>
        internal static void EntferneExterneBeziehungen(WordprocessingDocument doc, Fuellergebnis ergebnis, bool englisch)
        {
            foreach (OpenXmlPart teil in doc.GetAllParts().ToList())
            {
                List<ExternalRelationship> externe = teil.ExternalRelationships.ToList();
                if (externe.Count == 0) continue;

                OpenXmlPartRootElement wurzel = teil.RootElement;
                foreach (ExternalRelationship bez in externe)
                {
                    string ziel = bez.Uri?.OriginalString ?? "";
                    string typ = bez.RelationshipType ?? "";
                    string art = typ.Substring(typ.LastIndexOf('/') + 1);

                    if (wurzel != null) EntferneVerweise(wurzel, bez.Id);
                    teil.DeleteExternalRelationship(bez);

                    if (art == "attachedTemplate")
                        ergebnis.Hinweis(WordVorlagentexte.F(englisch, WordVorlagentexte.DOKUMENTVORLAGE, ziel));
                    else if (art == "image")
                        ergebnis.Warnung(WordVorlagentexte.F(englisch, WordVorlagentexte.VERKNUEPFTES_BILD, ziel));
                    else
                        ergebnis.Warnung(WordVorlagentexte.F(englisch, WordVorlagentexte.VERKNUEPFUNG, art, ziel));
                }
            }
        }

        /// <summary>
        /// Entfernt, was auf die Beziehung <paramref name="kennung"/> verweist: am Bild den
        /// <c>r:link</c> (trägt es danach kein eingebettetes Bild, entfällt das ganze Bild), die
        /// Dokumentvorlage und die Serienbriefquelle in den Einstellungen, sonst das verweisende
        /// Element — liegt es in einem Run, den Run.
        /// </summary>
        private static void EntferneVerweise(OpenXmlElement wurzel, string kennung)
        {
            List<OpenXmlElement> treffer = wurzel.Descendants()
                .Where(e => e.GetAttributes().Any(a => a.NamespaceUri == NS_BEZIEHUNG && a.Value == kennung))
                .ToList();

            foreach (OpenXmlElement e in treffer)
            {
                if (e.Parent == null && !ReferenceEquals(e, wurzel)) continue;   // schon mit einem Vorfahren entfernt

                if (e is A.Blip blip)
                {
                    if (blip.Link?.Value == kennung) blip.Link = null;
                    if (blip.Embed?.Value == kennung) blip.Embed = null;
                    if (blip.Embed == null) EntferneBild(blip);
                    continue;
                }
                if (e is AttachedTemplate || e is DataSourceReference || e is HeaderSource)
                {
                    OpenXmlElement serienbrief = e.Ancestors<MailMerge>().FirstOrDefault();
                    (serienbrief ?? e).Remove();
                    continue;
                }

                Run lauf = e.Ancestors<Run>().FirstOrDefault();
                if (lauf != null) lauf.Remove();
                else e.Remove();
            }
        }

        /// <summary>Entfernt das Bild eines Blips ohne eingebettete Daten: den Run der Zeichnung.</summary>
        private static void EntferneBild(A.Blip blip)
        {
            OpenXmlElement zeichnung = (OpenXmlElement)blip.Ancestors<Drawing>().FirstOrDefault()
                                       ?? blip.Ancestors<Picture>().FirstOrDefault();
            Run lauf = zeichnung?.Ancestors<Run>().FirstOrDefault();
            if (lauf != null) lauf.Remove();
            else zeichnung?.Remove();
        }
    }
}
