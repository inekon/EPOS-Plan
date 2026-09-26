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
        /// und die Kommentarteile. Rückgabe: die Zahl der Kommentare, gezählt wie im Prüfer
        /// (<see cref="Vorlagenteile.Kommentarzahl"/>) — Marken ohne Eintrag im Kommentarteil entfallen
        /// mit, zählen aber nicht.
        /// </summary>
        internal static int EntferneKommentare(MainDocumentPart main, IEnumerable<OpenXmlElement> wurzeln)
        {
            int zahl = Vorlagenteile.Kommentarzahl(main);

            foreach (OpenXmlElement wurzel in wurzeln.Where(w => w != null))
            {
                foreach (CommentRangeStart s in wurzel.Descendants<CommentRangeStart>().ToList()) s.Remove();
                foreach (CommentRangeEnd e in wurzel.Descendants<CommentRangeEnd>().ToList()) e.Remove();
                foreach (CommentReference r in wurzel.Descendants<CommentReference>().ToList())
                {
                    Run lauf = r.Parent as Run;
                    r.Remove();
                    if (lauf != null && !lauf.ChildElements.Any(c => !(c is RunProperties))) lauf.Remove();
                }
            }

            if (main.WordprocessingCommentsPart != null) main.DeletePart(main.WordprocessingCommentsPart);
            if (main.WordprocessingCommentsExPart != null) main.DeletePart(main.WordprocessingCommentsExPart);
            if (main.WordprocessingCommentsIdsPart != null) main.DeletePart(main.WordprocessingCommentsIdsPart);
            if (main.WordCommentsExtensiblePart != null) main.DeletePart(main.WordCommentsExtensiblePart);

            return zahl;
        }

        // ------------------------------------------------------------- Schnellbausteine

        /// <summary>
        /// Entfernt die Bausteine des Glossars (<c>word/glossary/document.xml</c>) — Schnellbausteine, AutoText und jede
        /// andere Galerie — und behält allein die Platzhaltertexte der Inhaltssteuerelemente (Galerie
        /// <c>placeholder</c>), auf die ein Steuerelement im Bericht über <c>w:docPart</c> verweisen kann. Bleibt kein
        /// Eintrag, entfällt der Glossarteil samt seinen Teilen. Grund (Bausteinvorlage, BV-E9): Bausteine sind
        /// Werkzeug der Vorlage, nicht Inhalt des Berichts — so hält es auch Word, das ein Dokument aus einer
        /// <c>.dotx</c> ohne deren Bausteine anlegt; im Bericht stünden sie mit ungefüllten Platzhaltern.
        /// Rückgabe: die Zahl der entfernten Bausteine.
        /// </summary>
        internal static int EntferneSchnellbausteine(MainDocumentPart main)
        {
            GlossaryDocumentPart glossar = main?.GlossaryDocumentPart;
            if (glossar == null) return 0;

            DocParts eintraege = glossar.GlossaryDocument?.DocParts;
            List<DocPart> weg = eintraege == null
                ? new List<DocPart>()
                : eintraege.Elements<DocPart>().Where(d => !IstPlatzhaltertext(d)).ToList();
            bool bleibt = eintraege != null && eintraege.Elements<DocPart>().Any(IstPlatzhaltertext);

            if (!bleibt)
            {
                main.DeletePart(glossar);
                return weg.Count;
            }
            foreach (DocPart d in weg) d.Remove();
            glossar.GlossaryDocument.Save();
            return weg.Count;
        }

        /// <summary>Ein Platzhaltertext eines Inhaltssteuerelements: Galerie <c>placeholder</c>.</summary>
        private static bool IstPlatzhaltertext(DocPart d)
        {
            Gallery galerie = d.DocPartProperties?.GetFirstChild<Category>()?.GetFirstChild<Gallery>();
            return galerie?.Val != null && galerie.Val.Value == DocPartGalleryValues.Placeholder;
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
