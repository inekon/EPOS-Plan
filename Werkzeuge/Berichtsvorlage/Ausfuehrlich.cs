using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Eigenschaft = DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty;
using Eigenschaftsliste = DocumentFormat.OpenXml.CustomProperties.Properties;

namespace Berichtsvorlage
{
    /// <summary>
    /// <b>Die ausführliche Vorlage je Sprache</b> (Anwenderentscheid BV-E8-4; Konzept Berichtsvorlagen 6.3) — der volle
    /// Bericht aus <b>Einzelelementen</b>, frei umbaubar: inhaltlich wie der Standardbericht und in seiner Reihenfolge
    /// (Deckblatt, Inhaltsverzeichnis, Projektbeschreibung, Komponenten &amp; Varianten, Ergebnisse je Variante,
    /// Variantenvergleich, Wirtschaftlichkeit, Anhang, Anhang E), aber jeder Abschnitt aus einzelnen Platzhaltern:
    /// Einzelwerte, Blöcke <c>{{#je stand}}</c>/<c>{{#je variante}}</c>/<c>{{#je gebaeude}}</c>, Bedingungen
    /// <c>{{#wenn …}}</c>, Strukturtabellen <c>{{tabelle.…}}</c>/<c>{{stand.tabelle.…}}</c>, Bildplatzhalter
    /// <c>bild.*</c>/<c>stand.bild.*</c> in voller bzw. halber Satzspiegelbreite (Bildgröße Stufe 2), Musterzeilen mit
    /// Kennzahlen und Wirtschaftlichkeitswerten, Warnlisten. Kein Kapitelplatzhalter — die Kapitelköpfe sind
    /// <c>{{text.kapitel_&lt;name&gt;}}</c> im Format „EPOS Kapitelkopf“, das Inhaltsverzeichnis ein Word-Feld.
    ///
    /// <para><b>Je Sprache eine Datei</b> wie der Kurzbericht: Überschriften und Sätze, für die der Katalog keinen
    /// <c>text.*</c>-Platzhalter führt, sind fester Text der Sprache; Deckblatt, Kapitelköpfe, Kennzahlbeschriftungen
    /// (<c>kennzahl.&lt;k&gt;.beschriftung</c>) und Szenarionamen sind sprachneutrale Platzhalter. Beide Dateien sind
    /// gleich gebaut: dieselben Marken, Stile und Bildrahmen in derselben Folge. <c>custom.xml</c> trägt
    /// <c>EPOS.Katalogfassung</c>, <c>EPOS.Vorlage</c> <c>ausfuehrlich</c> und <c>EPOS.Sprache</c>.</para>
    ///
    /// <para><b>Erläuterungen</b> stehen als Word-Kommentare an jedem Abschnitt (was die Platzhalter tun, wie man sie
    /// ändert oder entfernt); die Engine entfernt sie beim Füllen. Neutral: kein Hersteller, kein Produkt, keine
    /// Kennwerte eines Geräts (Wache <c>WikiProduktdatenWacheTests</c>). Das Tabellenformat „EPOS Tabelle“ liegt in
    /// der Datei, die Mustertabelle <c>{{muster.tabelle}}</c> am Ende. <b>Byte-gleich wiederholbar</b> wie der
    /// Kurzbericht (festes Platzhalterbild, feste Teilnamen, Zeitstempel der Quelle).</para>
    /// </summary>
    internal static class Ausfuehrlich
    {
        /// <summary>Die beiden Dateien im Vorlagenordner — ihr Name legt die Sprache fest.</summary>
        internal const string DATEI_DEUTSCH = "Berichtsvorlage_Ausfuehrlich.docx";
        internal const string DATEI_ENGLISCH = "Berichtsvorlage_Ausfuehrlich_en.docx";

        /// <summary>Der Wert von <c>EPOS.Vorlage</c>.</summary>
        internal const string KENNUNG = "ausfuehrlich";

        /// <summary>Das Tabellenformat der Vorlage — Name und Kennung wie die Engine es anlegt (<c>WordVorlagenstile</c>).</summary>
        internal const string TABELLENSTIL_NAME = "EPOS Tabelle";
        internal const string TABELLENSTIL_ID = "EPOSTabelle";

        /// <summary>Satzspiegel der Stilvorlage in EMU (9355 twips × 635).</summary>
        internal const long VOLL = Beispielvorlage.INHALTSBREITE * 635L;

        /// <summary>Zwei Bilder nebeneinander — dasselbe Maß wie im Kurzbericht (über der Mindestbreite des Kuchens).</summary>
        internal const long HALB_BREITE = 2808000L;
        internal const long HALB_HOEHE = 1752000L;

        internal static int Ausfuehren(string quelle, string ziel, bool englisch, int katalogfassung, TextWriter aus)
        {
            if (!File.Exists(quelle))
            {
                Console.Error.WriteLine("Quelle nicht gefunden: " + quelle);
                return Program.DATEI;
            }
            if (!string.Equals(Path.GetExtension(quelle), ".docx", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(ziel), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Quelle und Ziel müssen .docx-Dateien sein.");
                return Program.DATEI;
            }
            if (string.Equals(quelle, ziel, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Quelle und Ziel müssen verschieden sein — die Quelle bleibt die Stilvorlage des Generators.");
                return Program.AUFRUF;
            }
            string unstimmig = ZielUndSprache(ziel, englisch);
            if (unstimmig != null)
            {
                Console.Error.WriteLine(unstimmig);
                return Program.AUFRUF;
            }

            string sprache = englisch ? "en" : "de";
            aus.WriteLine("Ausführliche Vorlage (" + sprache + "): " + quelle + " → " + ziel);

            List<string> vorbefunde;
            using (WordprocessingDocument doc = WordprocessingDocument.Open(quelle, false))
                vorbefunde = Pruefung.Stilbefunde(doc.MainDocumentPart?.StyleDefinitionsPart?.Styles);
            if (vorbefunde.Count > 0)
            {
                foreach (string b in vorbefunde) Console.Error.WriteLine("  Stilbefund der Quelle: " + b);
                Console.Error.WriteLine("Die Quelle ist nicht bereinigt — zuerst „bereinigen“ laufen lassen.");
                return Program.PRUEFUNG;
            }

            string kopie = Program.Arbeitskopie(quelle);
            try
            {
                var bauer = new Bauer(englisch);
                Beispielvorlage.Logostelle logo;
                using (WordprocessingDocument doc = WordprocessingDocument.Open(kopie, true))
                    logo = bauer.Baue(doc, katalogfassung, aus);
                Beispielvorlage.Bildtausch(kopie, logo, aus);
                Kurzbericht.Bildteil(kopie, aus);
                Beispielvorlage.Zeitstempel(kopie, quelle);

                if (!Abschlusspruefung(kopie, bauer, katalogfassung, aus))
                {
                    Console.Error.WriteLine("Die ausführliche Vorlage ist nicht in Ordnung — das Ziel bleibt unverändert.");
                    return Program.PRUEFUNG;
                }
                File.Copy(kopie, ziel, true);
                aus.WriteLine("  Geschrieben: " + ziel);
                return Program.OK;
            }
            finally { Program.Loeschen(kopie); }
        }

        /// <summary>Die beiden Dateien des Vorlagenordners entstehen nur in ihrer Sprache; die übrigen mitgelieferten nie.</summary>
        private static string ZielUndSprache(string ziel, bool englisch)
        {
            string name = Path.GetFileName(ziel);
            if (string.Equals(name, DATEI_DEUTSCH, StringComparison.OrdinalIgnoreCase) && englisch)
                return DATEI_DEUTSCH + " ist die deutsche ausführliche Vorlage — mit --sprache de bauen.";
            if (string.Equals(name, DATEI_ENGLISCH, StringComparison.OrdinalIgnoreCase) && !englisch)
                return DATEI_ENGLISCH + " ist die englische ausführliche Vorlage — mit --sprache en bauen.";
            foreach (string fremd in new[] { Beispielvorlage.DATEI_STANDARD, Beispielvorlage.DATEI_BEISPIEL,
                                             Kurzbericht.DATEI_DEUTSCH, Kurzbericht.DATEI_ENGLISCH })
                if (string.Equals(name, fremd, StringComparison.OrdinalIgnoreCase))
                    return name + " entsteht nicht mit „ausfuehrlich“.";
            return null;
        }

        // ------------------------------------------------------------- Aufbau

        /// <summary>Baut eine Sprache; sammelt Kommentare und Bildschlüssel für die Abschlussprüfung.</summary>
        internal sealed class Bauer
        {
            private readonly bool _en;
            private uint _bildkennung = 101;
            internal readonly List<string[]> Kommentare = new List<string[]>();
            internal readonly List<string> Bildschluessel = new List<string>();
            internal string Sprache => _en ? "en" : "de";

            internal Bauer(bool englisch) { _en = englisch; }

            /// <summary>Der Text der Sprache.</summary>
            private string L(string de, string en) => _en ? en : de;

            internal Beispielvorlage.Logostelle Baue(WordprocessingDocument doc, int katalogfassung, TextWriter aus)
            {
                MainDocumentPart main = doc.MainDocumentPart;
                Body body = main.Document.Body;
                SectionProperties hauptabschnitt = body.Elements<SectionProperties>().LastOrDefault()
                    ?? throw new InvalidOperationException("Die Vorlage hat keine Abschnittseigenschaften (w:sectPr) am Rumpfende.");
                foreach (OpenXmlElement el in body.ChildElements.Where(c => !(c is SectionProperties)).ToList())
                    el.Remove();

                foreach (OpenXmlElement el in Rumpf(hauptabschnitt))
                    body.InsertBefore(el, hauptabschnitt);
                main.Document.Save();

                Tabellenstil(main, aus);
                Beispielvorlage.Kopfzeile(main, aus);
                Beispielvorlage.Logostelle logo = Beispielvorlage.LogoPlatzhalter(main);
                Beispielvorlage.Fusszeile(main, aus);

                WordprocessingCommentsPart teil = main.WordprocessingCommentsPart ?? main.AddNewPart<WordprocessingCommentsPart>();
                teil.Comments = new Comments(Kommentare.Select((absaetze, i) =>
                    Beispielvorlage.Kommentar(i.ToString(CultureInfo.InvariantCulture), absaetze)));
                teil.Comments.Save();

                CustomFilePropertiesPart eigen = doc.CustomFilePropertiesPart ?? doc.AddCustomFilePropertiesPart();
                Eigenschaftsliste liste = eigen.Properties;
                if (liste == null) eigen.Properties = liste = new Eigenschaftsliste();
                Beispielvorlage.Setze(liste, Beispielvorlage.EIGENSCHAFT_KATALOGFASSUNG, new VTInt32(katalogfassung.ToString(CultureInfo.InvariantCulture)));
                Beispielvorlage.Setze(liste, Beispielvorlage.EIGENSCHAFT_VORLAGE, new VTLPWSTR(KENNUNG));
                Beispielvorlage.Setze(liste, Kurzbericht.EIGENSCHAFT_SPRACHE, new VTLPWSTR(Sprache));
                liste.Save();
                aus.WriteLine("  custom.xml: " + string.Join(", ", liste.Elements<Eigenschaft>().Select(e => e.Name?.Value + " = " + e.InnerText)));
                aus.WriteLine("  Rumpf: " + Kommentare.Count + " Kommentare, " + Bildschluessel.Count + " Bildrahmen");

                doc.PackageProperties.LastModifiedBy = Beispielvorlage.AUTOR;
                doc.PackageProperties.Title = L("Berichtsvorlage EPOS-Plan — ausführliche Vorlage",
                                                "EPOS-Plan report template — detailed template");
                doc.PackageProperties.Description = L(
                    "Der volle Bericht aus Einzelelementen, frei umbaubar: Einzelwerte, Blöcke, Tabellen und Bilder in der Folge des Standardberichts, erläutert in Kommentaren; Kopie über „Neue Vorlage…“.",
                    "The full report built from single elements, freely adaptable: single values, blocks, tables and images in the order of the standard report, explained in comments; copy it via “New template…”.");
                return logo;
            }

            /// <summary>Legt das Tabellenformat „EPOS Tabelle“ an — wie die Engine es anlegt, hier aber in der Datei, damit man es ändern kann.</summary>
            private static void Tabellenstil(MainDocumentPart main, TextWriter aus)
            {
                Styles stile = main.StyleDefinitionsPart.Styles;
                if (stile.Elements<Style>().Any(s => s.StyleId?.Value == TABELLENSTIL_ID))
                    throw new InvalidOperationException("Die Quelle führt schon ein Format „" + TABELLENSTIL_ID + "“.");
                const string rahmen = "BFBFBF", kopf = "D9E1F2", groesse = "18";
                var s = new Style { Type = StyleValues.Table, StyleId = TABELLENSTIL_ID, CustomStyle = true };
                s.Append(new StyleName { Val = TABELLENSTIL_NAME });
                s.Append(new PrimaryStyle());
                s.Append(new StyleParagraphProperties(new SpacingBetweenLines { Before = "20", After = "20" }));
                s.Append(new StyleRunProperties(new FontSize { Val = groesse }, new FontSizeComplexScript { Val = groesse }));
                s.Append(new StyleTableProperties(new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4U, Color = rahmen },
                    new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = rahmen },
                    new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = rahmen },
                    new RightBorder { Val = BorderValues.Single, Size = 4U, Color = rahmen },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = rahmen },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = rahmen })));
                s.Append(new TableStyleProperties(
                    new RunPropertiesBaseStyle(new Bold(), new BoldComplexScript()),
                    new TableStyleConditionalFormattingTableCellProperties(
                        new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = kopf }))
                { Type = TableStyleOverrideValues.FirstRow });
                stile.Append(s);
                stile.Save();
                aus.WriteLine("  Tabellenformat: „" + TABELLENSTIL_NAME + "“ (" + TABELLENSTIL_ID + ")");
            }

            // --------------------------------------------------------- Bausteine der Absätze

            private static Run P(string inhalt) => Beispielvorlage.Platzhalter(inhalt);
            private static Run T(string text) => Beispielvorlage.Lauf(text);
            private static Paragraph A(string stil, params OpenXmlElement[] inhalt) => Beispielvorlage.Absatz(stil, inhalt);
            private static Paragraph Marke(string marke) => A("Normal", P(marke));

            /// <summary>Legt einen Kommentar an (Deutsch, Englisch — je Sprache einer) und gibt seine Kennung.</summary>
            private string Kommentar(string[] de, string[] en)
            {
                Kommentare.Add(_en ? en : de);
                return (Kommentare.Count - 1).ToString(CultureInfo.InvariantCulture);
            }

            /// <summary>Hängt einen Kommentar an einen Absatz: Bereich um seinen Inhalt, Verweis am Ende.</summary>
            private Paragraph Erklaert(Paragraph p, string[] de, string[] en)
            {
                string id = Kommentar(de, en);
                OpenXmlElement erstes = p.ParagraphProperties;
                if (erstes != null) p.InsertAfter(new CommentRangeStart { Id = id }, erstes);
                else p.PrependChild(new CommentRangeStart { Id = id });
                p.Append(new CommentRangeEnd { Id = id }, new Run(new CommentReference { Id = id }));
                return p;
            }

            private Paragraph Kapitelkopf(string name) => A(Pruefung.KAPITELKOPF_ID, P("text.kapitel_" + name));
            private Paragraph H2(string de, string en) => A("Heading2", T(L(de, en)));
            private Paragraph H3(string de, string en) => A("Heading3", T(L(de, en)));
            private Paragraph Hinweis(string de, string en) => A("Hinweis", T(L(de, en)));
            private Paragraph Text(string de, string en) => A("Normal", T(L(de, en)));
            private Paragraph Beschriftung(string de, string en) => A("Beschriftung", T(L(de, en)));

            /// <summary>Überschrift 3 „Stamm — Name“ bzw. „Variante — Name“ im Standblock, sprachneutral.</summary>
            private static Paragraph Standkopf(string stil = "Heading3") => A(stil, P("stand.rolle"), T(" — "), P("stand.anzeige"));

            /// <summary>Ein Bildrahmen allein im Absatz — der Schlüssel im Alternativtext.</summary>
            private Paragraph Bild(string schluessel, long breite, long hoehe)
            {
                Bildschluessel.Add("{{" + schluessel + "}}");
                return A("Normal", Kurzbericht.Bildrahmen(schluessel, breite, hoehe, _bildkennung++));
            }

            /// <summary>Ein Bild in voller Breite im Seitenverhältnis <paramref name="hoehe"/> : <paramref name="breite"/> (Modellmaß).</summary>
            private Paragraph BildVoll(string schluessel, int breite, int hoehe) => Bild(schluessel, VOLL, VOLL * hoehe / breite);

            /// <summary>Eine Tabelle im Format „EPOS Tabelle“ mit festen Spalten.</summary>
            private static Table Tabelle(params int[] breiten)
            {
                return new Table(
                    new TableProperties(
                        new TableStyle { Val = TABELLENSTIL_ID },
                        new TableWidth { Type = TableWidthUnitValues.Dxa, Width = Beispielvorlage.Zahl(breiten.Sum()) },
                        new TableLayout { Type = TableLayoutValues.Fixed },
                        new TableLook { Val = "0420" }),
                    new TableGrid(breiten.Select(b => new GridColumn { Width = Beispielvorlage.Zahl(b) })));
            }

            private static TableCell Zelle(int breite, string fuellung, params OpenXmlElement[] inhalt)
            {
                var tcp = new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = Beispielvorlage.Zahl(breite) });
                if (fuellung != null) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fuellung });
                tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
                var p = new Paragraph();
                foreach (OpenXmlElement e in inhalt) p.Append(e);
                return new TableCell(tcp, p);
            }

            /// <summary>Beschriftung · Wert: je Zeile eine Beschriftung (fester Text oder Platzhalter) und ein Wert.</summary>
            private Table Wertetabelle(params (OpenXmlElement Beschriftung, string Wert)[] zeilen)
            {
                const int links = 3600;
                int rechts = Beispielvorlage.INHALTSBREITE - links;
                Table t = Tabelle(links, rechts);
                t.GetFirstChild<TableProperties>().GetFirstChild<TableLook>().Val = "0400";
                foreach ((OpenXmlElement beschriftung, string wert) in zeilen)
                    t.Append(new TableRow(Zelle(links, Beispielvorlage.FUELLUNG_BESCHRIFTUNG, beschriftung), Zelle(rechts, null, P(wert))));
                return t;
            }

            private (OpenXmlElement, string) Zeile(string de, string en, string wert) => (T(L(de, en)), wert);
            private static (OpenXmlElement, string) Kennzahl(string k, string quelle = "stamm") => (P("kennzahl." + k + ".beschriftung"), quelle + ".kennzahl." + k);

            /// <summary>Zwei Bildrahmen nebeneinander: eine zweispaltige Tabelle ohne Rahmen, je Zelle ein Bild in halber Breite.</summary>
            private Table Bildpaar(string links, string rechts)
            {
                int spalte = Beispielvorlage.INHALTSBREITE / 2;
                var t = new Table(
                    new TableProperties(
                        new TableWidth { Type = TableWidthUnitValues.Dxa, Width = Beispielvorlage.Zahl(2 * spalte) },
                        new TableBorders(
                            new TopBorder { Val = BorderValues.None }, new LeftBorder { Val = BorderValues.None },
                            new BottomBorder { Val = BorderValues.None }, new RightBorder { Val = BorderValues.None },
                            new InsideHorizontalBorder { Val = BorderValues.None }, new InsideVerticalBorder { Val = BorderValues.None }),
                        new TableLayout { Type = TableLayoutValues.Fixed }),
                    new TableGrid(new GridColumn { Width = Beispielvorlage.Zahl(spalte) }, new GridColumn { Width = Beispielvorlage.Zahl(spalte) }));
                Bildschluessel.Add("{{" + links + "}}");
                Run a = Kurzbericht.Bildrahmen(links, HALB_BREITE, HALB_HOEHE, _bildkennung++);
                Bildschluessel.Add("{{" + rechts + "}}");
                Run b = Kurzbericht.Bildrahmen(rechts, HALB_BREITE, HALB_HOEHE, _bildkennung++);
                t.Append(new TableRow(Zelle(spalte, null, a), Zelle(spalte, null, b)));
                return t;
            }

            /// <summary>Das Inhaltsverzeichnis als Word-Feld TOC (Ebenen 1 bis 3) — Word aktualisiert es beim Öffnen.</summary>
            private Paragraph Inhaltsverzeichnis()
            {
                var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Normal" }));
                p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
                p.Append(new Run(new FieldCode(" TOC \\o \"1-3\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve }));
                p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
                p.Append(T(L("Das Inhaltsverzeichnis wird beim Öffnen in Word aktualisiert.",
                             "The table of contents is updated when the document is opened in Word.")));
                p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
                return p;
            }

            private static Paragraph Seitenumbruch() => new Paragraph(new Run(new Break { Type = BreakValues.Page }));

            // --------------------------------------------------------- Rumpf

            private List<OpenXmlElement> Rumpf(SectionProperties hauptabschnitt)
            {
                var r = new List<OpenXmlElement>();
                Deckblatt(r, hauptabschnitt);
                Inhalt(r);
                Projekt(r);
                Komponenten(r);
                Ergebnisse(r);
                Vergleich(r);
                Wirtschaftlichkeit(r);
                Anhang(r);
                AnhangE(r);
                r.Add(Mustertabelle());
                return r;
            }

            /// <summary>1 — Deckblatt wie die Standardvorlage (sprachneutral), eigener Abschnitt ohne Kopf- und Fußzeile.</summary>
            private void Deckblatt(List<OpenXmlElement> r, SectionProperties hauptabschnitt)
            {
                // Kennung 0 — Beispielvorlage.Deckblatt setzt den Bereich des Kommentars "0" um das ganze Deckblatt.
                Kommentar(new[]
                {
                    "Ausführliche Vorlage – der volle Bericht aus Einzelelementen, frei umbaubar. Sie enthält dieselben Inhalte "
                    + "in derselben Reihenfolge wie die Standardvorlage, aber jeder Abschnitt besteht aus einzelnen Platzhaltern "
                    + "statt aus einem ganzen Kapitel. So lässt sich jeder Teil verschieben, umformatieren, kürzen oder löschen.",
                    "Text in doppelten geschweiften Klammern ist ein Platzhalter, etwa {{projekt.kunde}}: Beim Erstellen setzt "
                    + "EPOS-Plan an seine Stelle den Wert aus dem Projekt, in Schrift, Größe und Farbe des Platzhalters. Hinter "
                    + "einem senkrechten Strich folgt wahlweise eine Formatangabe, etwa |leer statt strich. Der Text zwischen den "
                    + "Klammern muss unverändert bleiben; der Platzhalterkatalog der Berichtsseite nennt alle Schlüssel.",
                    "Das Deckblatt ist ein eigener Abschnitt ohne Kopf- und Fußzeile; seine Beschriftungen ({{text.kunde}} …) "
                    + "erscheinen in der Sprache des Berichts. Kopf- und Fußzeile tragen ebenfalls Platzhalter (Word erlaubt dort "
                    + "keine Kommentare): Das Bild rechts oben ist der Bildplatzhalter {{bild.ersteller.logo}} für das Logo aus "
                    + "den Einstellungen; die Seitenzahlen sind Word-Felder.",
                    "Diese Datei ist schreibgeschützt. „Neue Vorlage…“ mit „Kopie von: Ausführliche Vorlage“ legt eine Kopie an, "
                    + "die Sie frei ändern. Kommentare erscheinen nicht im fertigen Bericht.",
                }, new[]
                {
                    "Detailed template – the full report built from single elements, freely adaptable. It holds the same content in "
                    + "the same order as the standard template, but every section consists of single placeholders instead of a "
                    + "whole chapter. So every part can be moved, reformatted, shortened or deleted.",
                    "Text in double curly braces is a placeholder, e.g. {{projekt.kunde}}: when the report is created, EPOS-Plan "
                    + "replaces it with the value from the project, in the font, size and colour of the placeholder. An option may "
                    + "follow a vertical bar, e.g. |leer statt strich. The text between the braces must stay unchanged; the "
                    + "placeholder catalogue of the report page lists every key.",
                    "The title page is a section of its own without header and footer; its labels ({{text.kunde}} …) appear in the "
                    + "language of the report. Header and footer carry placeholders as well (Word allows no comments there): the "
                    + "picture at the top right is the image placeholder {{bild.ersteller.logo}} for the logo from the settings; "
                    + "the page numbers are Word fields.",
                    "This file is read-only. “New template…” with “Copy of: Detailed template” creates a copy you can change freely. "
                    + "Comments do not appear in the finished report.",
                });
                r.AddRange(Beispielvorlage.Deckblatt(hauptabschnitt, true));
            }

            /// <summary>2 — Inhaltsverzeichnis: Überschrift, Word-Feld TOC, Seitenumbruch.</summary>
            private void Inhalt(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(A("Heading1", T(L("Inhalt", "Contents"))), new[]
                {
                    "Das Inhaltsverzeichnis ist ein gewöhnliches Word-Feld (TOC) über die Überschriften 1 bis 3 – kein Platzhalter. "
                    + "EPOS-Plan lässt Word das Feld beim Öffnen des Berichts aktualisieren. Wer kein Verzeichnis will, löscht "
                    + "Überschrift, Feld und Seitenumbruch.",
                }, new[]
                {
                    "The table of contents is an ordinary Word field (TOC) over headings 1 to 3 – not a placeholder. EPOS-Plan lets "
                    + "Word update the field when the report is opened. If you want no table of contents, delete heading, field and "
                    + "page break.",
                }));
                r.Add(Inhaltsverzeichnis());
                r.Add(Seitenumbruch());
            }

            /// <summary>3 — Projektbeschreibung aus Einzelwerten, Gebäudeblock, Kennzahltafeln, Strukturtabellen und Bild.</summary>
            private void Projekt(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Kapitelkopf("projekt"), new[]
                {
                    "Kapitelkopf im Absatzformat „EPOS Kapitelkopf“: {{text.kapitel_projekt}} setzt den Titel des Abschnitts in "
                    + "der Sprache des Berichts ein; eigener Text statt des Platzhalters ist erlaubt. Darunter steht die "
                    + "Projektbeschreibung aus Einzelwerten – die Tabelle hat links feste Beschriftungen, rechts Platzhalter.",
                    "Nicht als Einzelelement verfügbar sind die Zonentafeln je Gebäude und die Tafeln der Heiz- und Kühlkreise "
                    + "(Anlagenkopplung). Wer sie braucht, ersetzt diesen Abschnitt bis zum nächsten Kapitelkopf durch einen Absatz "
                    + "mit {{kapitel.projekt|ohne titel}} – dann schreibt EPOS-Plan das ganze Kapitel.",
                }, new[]
                {
                    "Chapter heading in the paragraph style “EPOS Kapitelkopf”: {{text.kapitel_projekt}} inserts the title of the "
                    + "section in the language of the report; your own text instead of the placeholder is allowed. Below it, the "
                    + "project description is built from single values – the table has fixed labels on the left, placeholders on "
                    + "the right.",
                    "Not available as single elements are the zone tables per building and the tables of the heating and cooling "
                    + "circuits (plant coupling). If you need them, replace this section up to the next chapter heading by a "
                    + "paragraph with {{kapitel.projekt|ohne titel}} – EPOS-Plan then writes the whole chapter.",
                }));
                r.Add(Wertetabelle(
                    Zeile("Projektname", "Project name", "projekt.name"),
                    (P("text.kunde"), "projekt.kunde"),
                    (P("text.bearbeiter"), "projekt.bearbeiter"),
                    Zeile("Beschreibung", "Description", "projekt.beschreibung"),
                    Zeile("Klimaregion", "Climate region", "projekt.klimaregion"),
                    Zeile("Angelegt", "Created", "projekt.angelegt"),
                    Zeile("Zuletzt geändert", "Last modified", "projekt.geaendert"),
                    Zeile("Simulationsstand", "Simulation timestamp", "projekt.simulationsstand")));

                // Gebäude: Bedingung um die Überschrift, der Block je Gebäude darunter.
                r.Add(Erklaert(Marke("#wenn hat.gebaeude"), new[]
                {
                    "{{#wenn hat.gebaeude}} … {{/wenn}} zeigt die Überschrift nur, wenn das Projekt Gebäude führt. Der Block "
                    + "{{#je gebaeude}} … {{/je}} wiederholt Überschrift und Tabelle für jedes Gebäude des Stammprojekts; "
                    + "{{gebaeude.*}}-Platzhalter gelten nur darin. Die Absätze mit den Blockmarken entfallen im Bericht.",
                }, new[]
                {
                    "{{#wenn hat.gebaeude}} … {{/wenn}} shows the heading only if the project has buildings. The block "
                    + "{{#je gebaeude}} … {{/je}} repeats heading and table for every building of the base project; "
                    + "{{gebaeude.*}} placeholders are valid only inside it. The paragraphs holding the block markers are removed "
                    + "from the report.",
                }));
                r.Add(H2("Gebäude", "Buildings"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#je gebaeude"));
                r.Add(A("Heading3", P("gebaeude.name")));
                r.Add(Wertetabelle(
                    Zeile("Gebäudeart", "Building type", "gebaeude.art"),
                    Zeile("Baualtersklasse", "Construction period", "gebaeude.baualtersklasse"),
                    Zeile("Wohn-/Nutzfläche", "Living/usable area", "gebaeude.flaeche"),
                    Zeile("Bewohner/Nutzer", "Occupants/users", "gebaeude.nutzer"),
                    Zeile("Wärmebedarf", "Heat demand", "gebaeude.waermebedarf"),
                    Zeile("spez. Wärmeverbrauch", "Specific heat consumption", "gebaeude.spez_waermeverbrauch"),
                    Zeile("Warmwasserbedarf", "Hot water demand", "gebaeude.ww_bedarf"),
                    Zeile("Raumhöhe", "Room height", "gebaeude.raumhoehe")));
                r.Add(Marke("/je"));

                // Energiebedarf und Deckungsgrade des Stamms: Kennzahlen mit sprachneutraler Beschriftung.
                r.Add(Erklaert(Marke("#wenn hat.ergebnis"), new[]
                {
                    "Kennzahlen des Stammprojekts: links {{kennzahl.<k>.beschriftung}} – die Beschriftung der Kennzahl in der "
                    + "Sprache des Berichts –, rechts {{stamm.kennzahl.<k>}} mit Zahl und Einheit. Eine Zeile entfernen oder eine "
                    + "weitere Kennzahl eintragen ist erlaubt; der Platzhalterkatalog nennt alle Kennzahlen. Fehlt ein Wert, "
                    + "steht ein Strich.",
                }, new[]
                {
                    "Key figures of the base project: on the left {{kennzahl.<k>.beschriftung}} – the label of the key figure in "
                    + "the language of the report –, on the right {{stamm.kennzahl.<k>}} with number and unit. Removing a row or "
                    + "adding another key figure is allowed; the placeholder catalogue lists every key figure. A missing value "
                    + "shows a dash.",
                }));
                r.Add(H2("Energiebedarf (Simulationsergebnis Stamm)", "Energy demand (base simulation result)"));
                r.Add(Wertetabelle(
                    Kennzahl("energie.waermebedarf"), Kennzahl("energie.waermebedarf_heizung"),
                    Kennzahl("energie.waermebedarf_brauchwasser"), Kennzahl("energie.waermebedarf_prozess"),
                    Kennzahl("energie.waermelast"), Kennzahl("energie.strombedarf"), Kennzahl("energie.strommax")));
                r.Add(H2("Deckungsgrade je Bedarfsart", "Coverage by demand type"));
                r.Add(Wertetabelle(
                    Kennzahl("energie.deckung_heizung"), Kennzahl("energie.deckung_brauchwasser"),
                    Kennzahl("energie.deckung_prozess")));
                r.Add(Marke("/wenn"));

                // Kälte: nur mit Kältebedarf.
                r.Add(Erklaert(Marke("#wenn hat.kaelte"), new[]
                {
                    "Ein bedingter Abschnitt: Alles zwischen {{#wenn hat.kaelte}} und {{/wenn}} erscheint nur, wenn das Projekt "
                    + "Kälte rechnet. {{tabelle.kaelteerzeuger}} allein im Absatz ist eine Strukturtabelle: EPOS-Plan baut "
                    + "Zeilen und Spalten selbst, das Aussehen folgt dem Format „EPOS Tabelle“ und der Mustertabelle am Ende.",
                }, new[]
                {
                    "A conditional section: everything between {{#wenn hat.kaelte}} and {{/wenn}} appears only if the project "
                    + "calculates cooling. {{tabelle.kaelteerzeuger}} alone in a paragraph is a structured table: EPOS-Plan builds "
                    + "rows and columns itself, its look follows the table style “EPOS Tabelle” and the pattern table at the end.",
                }));
                r.Add(H2("Kältebedarf und -deckung (Simulationsergebnis Stamm)", "Cooling demand and coverage (base simulation result)"));
                r.Add(Wertetabelle(
                    Kennzahl("kaelte.jahresbedarf"), Kennzahl("kaelte.spitze"), Kennzahl("kaelte.stunden"),
                    Kennzahl("kaelte.deckungsgrad"), Kennzahl("kaelte.erzeugung"), Kennzahl("kaelte.strom"),
                    Kennzahl("kaelte.jaz"), Kennzahl("kaelte.netzbezug"), Kennzahl("kaelte.kosten"), Kennzahl("kaelte.co2")));
                r.Add(H3("Kälteerzeuger", "Cooling generators"));
                r.Add(Marke("tabelle.kaelteerzeuger"));
                r.Add(Marke("/wenn"));

                r.Add(Marke("#wenn hat.tabelle.gebaeude.ergebnis"));
                r.Add(H2("Gebäude (Simulationsergebnis Stamm)", "Buildings (base simulation result)"));
                r.Add(Marke("tabelle.gebaeude.ergebnis"));
                r.Add(Marke("/wenn"));

                r.Add(Erklaert(Marke("#wenn hat.tabelle.speichertemperaturen"), new[]
                {
                    "Jede Strukturtabelle und jedes Bild hat einen Schalter hat.tabelle.<name> bzw. hat.bild.<name>: Mit "
                    + "{{#wenn …}} entfällt die Überschrift, wenn es nichts zu zeigen gibt. Das Bild darunter ist ein Bildrahmen "
                    + "mit dem Schlüssel {{stamm.bild.speichertemperaturen}} im Alternativtext; EPOS-Plan zeichnet das Diagramm "
                    + "in der Breite des Rahmens. Rahmen verkleinern oder verschieben ist erlaubt.",
                }, new[]
                {
                    "Every structured table and every image has a switch hat.tabelle.<name> or hat.bild.<name>: with {{#wenn …}} "
                    + "the heading is dropped when there is nothing to show. The image below is a picture frame with the key "
                    + "{{stamm.bild.speichertemperaturen}} in its alternative text; EPOS-Plan draws the chart in the width of the "
                    + "frame. Resizing or moving the frame is allowed.",
                }));
                r.Add(H2("Speichertemperaturen (Schichtmodell)", "Storage temperatures (stratified model)"));
                r.Add(Marke("tabelle.speichertemperaturen"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#wenn hat.bild.speichertemperaturen"));
                r.Add(BildVoll("stamm.bild.speichertemperaturen", 620, 280));
                r.Add(Beschriftung("Speichertemperaturen in charakteristischen Wochen (Winter/Übergang/Sommer)",
                                   "Storage temperatures in characteristic weeks (winter/transition/summer)"));
                r.Add(Marke("/wenn"));
            }

            /// <summary>Die Gewerke der Kenndatentafeln in der Folge des Standardberichts.</summary>
            private static readonly (string Schluessel, string De, string En)[] Gewerke =
            {
                ("waermepumpe", "Wärmepumpe", "Heat pump"),
                ("bhkw", "BHKW", "CHP unit"),
                ("spitzenkessel", "Spitzenkessel", "Peak boiler"),
                ("solarthermie", "Solarthermie", "Solar thermal"),
                ("photovoltaik", "Photovoltaik", "Photovoltaics"),
                ("pufferspeicher", "Pufferspeicher", "Buffer storage"),
                ("stromspeicher", "Stromspeicher", "Battery storage"),
            };

            /// <summary>4 — Komponenten &amp; Varianten: Matrix, Kenndaten je Gewerk, Abweichungen je Variante.</summary>
            private void Komponenten(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Kapitelkopf("komponenten"), new[]
                {
                    "Komponenten und Varianten aus Strukturtabellen. Tabellen mit Spalten je Stand teilt EPOS-Plan in Blöcke zu "
                    + "höchstens drei Varianten, die Stammspalte wiederholt sich je Block; mit |block 2 hinter dem Schlüssel "
                    + "werden die Blöcke kleiner.",
                    "Je Gewerk steht die Tafel in einem {{#wenn hat.tabelle.komponenten.kenndaten.<gewerk>}}-Block: Ohne Gerät "
                    + "dieses Gewerks entfällt die Überschrift. Ein Gewerk, das nicht interessiert, löscht man samt Block.",
                }, new[]
                {
                    "Components and variants from structured tables. EPOS-Plan splits tables with one column per state into blocks "
                    + "of at most three variants, repeating the base column in each block; |block 2 after the key makes the blocks "
                    + "smaller.",
                    "Each trade has its table inside a {{#wenn hat.tabelle.komponenten.kenndaten.<trade>}} block: without a unit "
                    + "of this trade the heading is dropped. Delete a trade you are not interested in together with its block.",
                }));
                r.Add(H2("Komponentenübersicht", "Component overview"));
                r.Add(Marke("tabelle.komponenten.matrix"));
                foreach ((string schluessel, string de, string en) in Gewerke)
                {
                    r.Add(Marke("#wenn hat.tabelle.komponenten.kenndaten." + schluessel));
                    r.Add(H2(de, en));
                    r.Add(Marke("tabelle.komponenten.kenndaten." + schluessel));
                    r.Add(Marke("/wenn"));
                }

                r.Add(Erklaert(Marke("#wenn hat.varianten"), new[]
                {
                    "{{#je variante}} … {{/je}} wiederholt seinen Inhalt für jede verglichene Variante, ohne das Stammprojekt "
                    + "({{#je stand}} nimmt das Stammprojekt dazu). {{stand.tabelle.abweichungen}} ist die Tabelle des laufenden "
                    + "Stands; ohne Abweichung steht dort ein Strich mit Grund.",
                }, new[]
                {
                    "{{#je variante}} … {{/je}} repeats its content for every compared variant, without the base project "
                    + "({{#je stand}} includes the base project). {{stand.tabelle.abweichungen}} is the table of the current state; "
                    + "without deviations a dash with the reason appears.",
                }));
                r.Add(H2("Abweichungen der Varianten gegenüber dem Stamm", "Deviations of the variants from the base"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#je variante"));
                r.Add(A("Heading3", P("stand.anzeige")));
                r.Add(Marke("stand.tabelle.abweichungen"));
                r.Add(Marke("/je"));
            }

            /// <summary>5 — Ergebnisse je Variante: Block je Stand mit Kennzahltabelle und vier Bildern.</summary>
            private void Ergebnisse(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Kapitelkopf("ergebnisse"), new[]
                {
                    "Der Block {{#je stand}} … {{/je}} wiederholt alles dazwischen für das Stammprojekt und jede Variante: "
                    + "Überschrift, Simulationsstand, Kennzahltabelle und die Diagramme des Stands. {{stand.rolle}} ist „Stamm“ "
                    + "oder „Variante“, {{stand.anzeige}} der Name.",
                    "Innerhalb des Blocks gelten die Schalter für den laufenden Stand: {{#wenn hat.bild.waerme_jahresverlauf}} "
                    + "zeigt das Bild nur, wenn der Stand Stundenreihen hat. Blöcke dürfen zwei Ebenen tief geschachtelt werden, "
                    + "nicht tiefer.",
                }, new[]
                {
                    "The block {{#je stand}} … {{/je}} repeats everything in between for the base project and every variant: "
                    + "heading, simulation timestamp, key figure table and the charts of the state. {{stand.rolle}} is “Base” or "
                    + "“Variant”, {{stand.anzeige}} the name.",
                    "Inside the block the switches refer to the current state: {{#wenn hat.bild.waerme_jahresverlauf}} shows the "
                    + "image only if the state has hourly series. Blocks may be nested two levels deep, not deeper.",
                }));
                r.Add(Marke("#je stand"));
                r.Add(Standkopf("Heading2"));
                r.Add(A("Hinweis", T(L("Simulationsstand: ", "Simulation timestamp: ")), P("stand.simulationsstand"),
                        T(" "), P("stand.hinweis")));
                r.Add(Marke("#wenn stand.hat_fehler"));
                r.Add(A("Normal", P("stand.fehler")));
                r.Add(Marke("/wenn"));
                r.Add(Marke("stand.tabelle.kennzahlen"));
                (string Schluessel, int Hoehe, string De, string En)[] bilder =
                {
                    ("waerme_jahresverlauf", 280, "Wärmeerzeugung im Jahresverlauf (gestapelte Erzeuger, Bedarf als Linie, Tagesmittel)",
                     "Heat generation over the year (stacked generators, demand as line, daily mean)"),
                    ("waerme_dauerlinie", 280, "Jahresdauerlinie Wärme (geordnete Bedarfs- und Erzeugerdauerlinien)",
                     "Annual heat duration curve (ordered demand and generator duration curves)"),
                    ("strombilanz_monate", 280, "Strombilanz im Monatsverlauf (Deckung gestapelt, Einspeisung separat, Bedarf als Linie)",
                     "Monthly electricity balance (coverage stacked, feed-in separate, demand as line)"),
                    ("speicherverlauf", 260, "Speicherverlauf in charakteristischen Wochen (Winter/Übergang/Sommer)",
                     "Storage profile in characteristic weeks (winter/transition/summer)"),
                };
                foreach ((string schluessel, int hoehe, string de, string en) in bilder)
                {
                    r.Add(Marke("#wenn hat.bild." + schluessel));
                    r.Add(BildVoll("stand.bild." + schluessel, 620, hoehe));
                    r.Add(Beschriftung(de, en));
                    r.Add(Marke("/wenn"));
                }
                r.Add(Marke("/je"));
            }

            /// <summary>Die Gruppen der Vergleichstafeln in der Folge des Standardberichts.</summary>
            private static readonly (string Schluessel, string De, string En)[] Gruppen =
            {
                ("energiebilanz", "Energiebilanz", "Energy balance"),
                ("effizienz", "Effizienz", "Efficiency"),
                ("kaelte", "Kälte", "Cooling"),
                ("emissionen", "Emissionen", "Emissions"),
                ("kosten", "Kosten", "Costs"),
            };

            /// <summary>Die Kennzahlen der Vergleichsbalken (<c>bild.vergleich.balken.&lt;k&gt;</c>).</summary>
            private static readonly string[] Balken = { "energie.brennstoff", "energie.netzbezug", "energie.waermerest", "eff.jaz" };

            /// <summary>6 — Variantenvergleich: Gruppentafeln, Δ-Tafel, Musterzeile, Balken, Deckungskuchen, Erzeuger, Brennstoffmengen.</summary>
            private void Vergleich(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Kapitelkopf("vergleich"), new[]
                {
                    "Der Variantenvergleich aus Strukturtabellen je Kennzahlgruppe; darüber die verglichenen Varianten "
                    + "({{text.varianten}}, {{bericht.varianten.liste}}). Die Δ-Spalte erscheint nur bei genau einer "
                    + "Variante, die Tafel der prozentualen Abweichung ab zwei Varianten. Ohne Varianten zeigt der Hinweis im "
                    + "Block {{#wenn nicht hat.varianten}}, dass nur das Stammprojekt verglichen wird.",
                }, new[]
                {
                    "The comparison of variants from structured tables per key figure group; above them the compared variants "
                    + "({{text.varianten}}, {{bericht.varianten.liste}}). The Δ column appears only with exactly "
                    + "one variant, the table of percentage deviations from two variants on. Without variants, the note in the "
                    + "block {{#wenn nicht hat.varianten}} says that only the base project is compared.",
                }));
                // Ein Absatz zwischen Kapitelkopf und erstem Block: Die Engine nimmt eine Überschrift, auf die nach einem
                // entfallenden Block nur noch eine Überschrift folgt, mit weg.
                r.Add(A("Hinweis", P("text.varianten"), T(": "), P("bericht.varianten.liste")));
                r.Add(Marke("#wenn nicht hat.varianten"));
                r.Add(Hinweis("Es wurden keine Varianten ausgewählt — die Tabellen zeigen nur das Stammprojekt.",
                              "No variants were selected — the tables show the base project only."));
                r.Add(Marke("/wenn"));
                foreach ((string schluessel, string de, string en) in Gruppen)
                {
                    r.Add(Marke("#wenn hat.tabelle.vergleich." + schluessel));
                    r.Add(H2(de, en));
                    r.Add(Marke("tabelle.vergleich." + schluessel));
                    r.Add(Marke("/wenn"));
                }
                r.Add(Marke("#wenn hat.tabelle.vergleich.delta_prozent"));
                r.Add(H2("Abweichung zum Stamm (Schlüsselkennzahlen, in %)", "Deviation from base (key figures, %)"));
                r.Add(Marke("tabelle.vergleich.delta_prozent"));
                r.Add(Marke("/wenn"));

                // Musterzeile aus Einzelwerten: Kopf aus Beschriftung und Einheit der Kennzahl, Werte ohne Einheit.
                r.Add(Erklaert(H2("Schlüsselkennzahlen je Stand", "Key figures per state"), new[]
                {
                    "Eine Tabelle aus Einzelwerten: Die zweite Zeile ist eine Musterzeile – {{#je stand}} in der ersten, {{/je}} "
                    + "in der letzten Zelle. EPOS-Plan wiederholt sie für das Stammprojekt und jede Variante; bei „Variante 1“ und "
                    + "„Variante 2“ entstehen drei Zeilen. Verbundene Zellen sind in der Musterzeile nicht erlaubt.",
                    "Der Kopf nennt Beschriftung und Einheit jeder Kennzahl ({{kennzahl.<k>.beschriftung}}, "
                    + "{{kennzahl.<k>.einheit}}), die Werte stehen deshalb mit |ohne einheit; |stellen 1 zeigt eine "
                    + "Nachkommastelle. Eine Spalte austauschen: den Schlüssel in Kopf und Musterzeile ändern.",
                    "Darunter Einzelwerte über alle Stände: {{vergleich.minimum.<k>}} und {{vergleich.maximum.<k>}} nennen den "
                    + "kleinsten und größten Wert einer Kennzahl.",
                }, new[]
                {
                    "A table of single values: the second row is a pattern row – {{#je stand}} in the first, {{/je}} in the last "
                    + "cell. EPOS-Plan repeats it for the base project and every variant; with “Variant 1” and “Variant 2” you get "
                    + "three rows. Merged cells are not allowed in the pattern row.",
                    "The header names label and unit of each key figure ({{kennzahl.<k>.beschriftung}}, {{kennzahl.<k>.einheit}}), "
                    + "so the values carry |ohne einheit; |stellen 1 shows one decimal place. To exchange a column, change the key "
                    + "in header and pattern row.",
                    "Below, single values across all states: {{vergleich.minimum.<k>}} and {{vergleich.maximum.<k>}} give the "
                    + "smallest and largest value of a key figure.",
                }));
                r.Add(Musterzeilentabelle(
                    new[] { "energie.waermebedarf", "energie.netzbezug", "eff.jaz", "em.co2" },
                    new[] { "|ohne einheit", "|ohne einheit", "|stellen 1", "|ohne einheit" }));
                r.Add(A("Normal", T(L("Spanne der Jahresarbeitszahl über alle Stände: ", "Range of the seasonal performance factor across all states: ")),
                        P("vergleich.minimum.eff.jaz|stellen 1"), T(L(" bis ", " to ")), P("vergleich.maximum.eff.jaz|stellen 1"), T(".")));

                r.Add(Erklaert(Marke("#wenn hat.varianten"), new[]
                {
                    "Die Balkendiagramme vergleichen je eine Kennzahl über alle Stände. Jeder Bildrahmen hat die volle Breite; die "
                    + "Höhe passt EPOS-Plan der Zahl der Stände an. Die Beschriftung darunter setzt sich aus "
                    + "{{kennzahl.<k>.beschriftung}} und festem Text zusammen.",
                }, new[]
                {
                    "The bar charts compare one key figure each across all states. Every picture frame has the full width; "
                    + "EPOS-Plan adapts the height to the number of states. The caption below is made of "
                    + "{{kennzahl.<k>.beschriftung}} and fixed text.",
                }));
                r.Add(H2("Kennzahlen im Vergleich (Diagramme)", "Key figures compared (charts)"));
                r.Add(Marke("/wenn"));
                foreach (string k in Balken)
                {
                    r.Add(Marke("#wenn hat.bild.vergleich.balken." + k));
                    r.Add(Bild("bild.vergleich.balken." + k, VOLL, VOLL * 3 / 5));
                    r.Add(A("Beschriftung", P("kennzahl." + k + ".beschriftung"), T(L(" je Variante (Stamm hervorgehoben)", " per variant (base highlighted)"))));
                    r.Add(Marke("/wenn"));
                }

                r.Add(Erklaert(H2("Deckungsdiagramme", "Coverage charts"), new[]
                {
                    "Zwei Bilder nebeneinander: eine zweispaltige Tabelle ohne Rahmen, in jeder Zelle ein Bildrahmen in halber "
                    + "Breite. EPOS-Plan zeichnet jedes Diagramm in dieser Größe, die Schrift bleibt lesbar. Der Block "
                    + "{{#je stand}} wiederholt Überschrift und Bilder je Stand, {{#wenn hat.ergebnis}} lässt einen Stand ohne "
                    + "Ergebnis aus.",
                }, new[]
                {
                    "Two images side by side: a two-column table without borders, a picture frame of half width in each cell. "
                    + "EPOS-Plan draws each chart at this size, so the lettering stays legible. The block {{#je stand}} repeats "
                    + "heading and images per state, {{#wenn hat.ergebnis}} leaves out a state without result.",
                }));
                r.Add(Hinweis("Anteile an der Wärme- bzw. Stromdeckung je Projekt (aus den Deckungsgraden der Erzeuger; der Rest ist ungedeckte Wärme bzw. Netzbezug).",
                              "Shares of heat and electricity coverage per project (from the coverage ratios of the generators; the rest is uncovered heat or grid purchase)."));
                r.Add(Marke("#je stand"));
                r.Add(Marke("#wenn hat.ergebnis"));
                r.Add(Standkopf());
                r.Add(Bildpaar("stand.bild.deckung_waerme", "stand.bild.deckung_strom"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("/je"));

                r.Add(H2("Erzeuger — Einzelauflistung je Projekt", "Generators — itemised per project"));
                r.Add(Hinweis("Je Projekt eine Zeile pro Gerät (Modul) mit erzeugter Energie; bei BHKW/Kessel der Brennstoff, bei der Wärmepumpe Strom (inkl. Heizstab).",
                              "Per project one row per unit (module) with the energy generated; for CHP units and boilers the fuel, for the heat pump the electricity (incl. heating rod)."));
                r.Add(Marke("#je stand"));
                r.Add(Standkopf());
                r.Add(Marke("stand.tabelle.erzeuger"));
                r.Add(Marke("/je"));

                r.Add(Marke("#wenn hat.tabelle.brennstoffmengen"));
                r.Add(H2("Brennstoffmengen", "Fuel quantities"));
                r.Add(Hinweis("Aus dem Brennstoffverbrauch über den projektspezifischen effektiven Heizwert in die Abrechnungseinheit umgerechnete Menge.",
                              "Quantity converted from the fuel consumption into the billing unit via the project-specific effective calorific value."));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#je stand"));
                r.Add(Marke("#wenn hat.tabelle.brennstoffmengen"));
                r.Add(Standkopf());
                r.Add(Marke("stand.tabelle.brennstoffmengen"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("/je"));
            }

            /// <summary>Kopf aus Beschriftung und Einheit je Kennzahl, darunter eine Musterzeile je Stand.</summary>
            private Table Musterzeilentabelle(string[] kennzahlen, string[] angaben)
            {
                int erste = 2555, rest = (Beispielvorlage.INHALTSBREITE - erste) / kennzahlen.Length;
                int[] breiten = new[] { erste }.Concat(kennzahlen.Select(_ => rest)).ToArray();
                breiten[0] += Beispielvorlage.INHALTSBREITE - breiten.Sum();
                Table t = Tabelle(breiten);
                var kopf = new TableRow(new TableRowProperties(new TableHeader()),
                                        Zelle(breiten[0], null, T(L("Stand", "State"))));
                for (int i = 0; i < kennzahlen.Length; i++)
                    kopf.Append(Zelle(breiten[i + 1], null, P("kennzahl." + kennzahlen[i] + ".beschriftung"), T(" ["),
                                      P("kennzahl." + kennzahlen[i] + ".einheit"), T("]")));
                t.Append(kopf);
                var zeile = new TableRow(Zelle(breiten[0], null, P("#je stand"), P("stand.anzeige")));
                for (int i = 0; i < kennzahlen.Length; i++)
                {
                    var inhalt = new List<OpenXmlElement> { P("stand.kennzahl." + kennzahlen[i] + angaben[i]) };
                    if (i == kennzahlen.Length - 1) inhalt.Add(P("/je"));
                    zeile.Append(Zelle(breiten[i + 1], null, inhalt.ToArray()));
                }
                t.Append(zeile);
                return t;
            }

            /// <summary>7 — Wirtschaftlichkeit: Methodik, Nachweise, Tafeln, Musterzeile, Verlauf, Brücke, Mehrjahres, Szenarien, Anhänge.</summary>
            private void Wirtschaftlichkeit(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Kapitelkopf("wirtschaftlichkeit"), new[]
                {
                    "Die Wirtschaftlichkeit aus Einzelelementen. {{wirtschaft.methodik}} ist der Methodiksatz, "
                    + "{{wirtschaft.parameternachweis}} die Nachweiszeile der Parameter dieses Rechenlaufs. Listen wie "
                    + "{{wirtschaft.valeri_hinweise}} und {{wirtschaft.deklarationen}} werden zu einer Aufzählung und entfallen, "
                    + "wenn sie leer sind.",
                    "{{wirtschaft.warnungen}} ist die Warnliste der Wirtschaftlichkeit (veraltete oder unvollständige Rechnung, "
                    + "Rückfall auf den gespeicherten Stand, Warnungen einzelner Zellen). Wer Wirtschaftlichkeitswerte zeigt, "
                    + "sollte sie behalten – sonst meldet die Vorlagenprüfung „Gültigkeitshinweise fehlen“.",
                }, new[]
                {
                    "Economic viability from single elements. {{wirtschaft.methodik}} is the methodology statement, "
                    + "{{wirtschaft.parameternachweis}} the disclosure line of the parameters of this calculation run. Lists such "
                    + "as {{wirtschaft.valeri_hinweise}} and {{wirtschaft.deklarationen}} become a bulleted list and are dropped "
                    + "when empty.",
                    "{{wirtschaft.warnungen}} is the warning list of the economic viability (outdated or incomplete calculation, "
                    + "fallback to the stored result, warnings of single cells). If you show economic values, keep it – otherwise "
                    + "the template check reports “validity notes missing”.",
                }));
                r.Add(Marke("wirtschaft.warnungen"));
                r.Add(A("Normal", P("wirtschaft.methodik")));
                r.Add(A("Hinweis", P("wirtschaft.parameternachweis")));
                r.Add(Marke("wirtschaft.valeri_hinweise"));
                r.Add(Marke("wirtschaft.deklarationen"));

                r.Add(H2("Kennzahlen im Szenario „Erwartet“", "Key figures, scenario \"Expected\""));
                r.Add(Marke("tabelle.wirtschaft.kennzahlen"));
                r.Add(Erklaert(A("Normal", T(L("Auf einen Blick:", "At a glance:"))), new[]
                {
                    "Eine Musterzeile mit Wirtschaftlichkeitswerten je Stand: {{stand.wirtschaft.<zeile>}} nimmt eine Zeile der "
                    + "Kennzahltafel im Szenario „Erwartet“, mit .guenstig bzw. .unguenstig am Ende das andere Szenario. "
                    + "Die Einheit steht im Kopf, die Werte deshalb mit |ohne einheit; |mit grund nennt den Grund, wenn ein Wert "
                    + "fehlt. Die Zahlen sind dieselben wie in der Tafel darüber.",
                }, new[]
                {
                    "A pattern row with economic values per state: {{stand.wirtschaft.<row>}} takes one row of the key figure table "
                    + "in the scenario “Expected”, with .guenstig or .unguenstig at the end the other scenario. The unit is in the "
                    + "header, so the values carry |ohne einheit; |mit grund names the reason when a value is missing. The numbers "
                    + "are the same as in the table above.",
                }));
                r.Add(Wirtschaftstabelle());

                r.Add(Marke("#wenn hat.tabelle.kwkg_module"));
                r.Add(H2("KWK-Zuschlag je Modul", "CHP bonus per module"));
                r.Add(Marke("/wenn"));
                Standtafeln(r, "kwkg_module");

                r.Add(Marke("#wenn hat.tabelle.betriebskosten"));
                r.Add(H2("Betriebskosten nach Kostenarten", "Operating cost by cost type"));
                r.Add(Marke("/wenn"));
                Standtafeln(r, "betriebskosten");

                r.Add(Erklaert(Marke("#wenn hat.bild.wirtschaft.kapitalwert_szenarien"), new[]
                {
                    "Die Diagramme der Wirtschaftlichkeit stehen je in einem {{#wenn hat.bild.…}}-Block: Ohne Modell – etwa "
                    + "ohne Stundenreihen – entfallen Überschrift und Rahmen.",
                }, new[]
                {
                    "The charts of the economic viability each stand in a {{#wenn hat.bild.…}} block: without a model – e.g. "
                    + "without hourly series – heading and frame are dropped.",
                }));
                r.Add(H2("Kapitalwert-Verlauf über den Betrachtungszeitraum", "Net present value over the assessment period"));
                r.Add(Bild("bild.wirtschaft.kapitalwert_szenarien", VOLL, VOLL * 3 / 5));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#wenn hat.bild.wirtschaft.barwerte_kumuliert"));
                r.Add(BildVoll("bild.wirtschaft.barwerte_kumuliert", 620, 310));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#wenn hat.bild.wirtschaft.bruecke"));
                r.Add(H2("Von der Investition zur Kapitalwertdifferenz", "From the investment to the net present value difference"));
                r.Add(Bild("bild.wirtschaft.bruecke", VOLL, VOLL * 3 / 5));
                r.Add(Marke("/wenn"));

                r.Add(Erklaert(Marke("#wenn hat.tabelle.mehrjahres"), new[]
                {
                    "Je Stand der Zahlungsstrom als Bild und die Mehrjahrestafel, darunter der Nachweis der vermiedenen Kosten "
                    + "(nicht Teil des Zahlungsstroms). Bild und Tafeln gelten für den laufenden Stand.",
                }, new[]
                {
                    "Per state the cash flow as an image and the multi-year table, below it the disclosure of the avoided cost "
                    + "(not part of the cash flow). Image and tables refer to the current state.",
                }));
                r.Add(H2("Mehrjahresübersicht der Zahlungsströme", "Multi-year overview of the cash flows"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("#je stand"));
                r.Add(Marke("#wenn hat.tabelle.mehrjahres"));
                r.Add(Standkopf());
                r.Add(Bild("stand.bild.zahlungsstrom", VOLL, VOLL * 3 / 5));
                r.Add(Marke("stand.tabelle.mehrjahres"));
                r.Add(A("Heading4", T(L("Nachweis — nicht Teil des Zahlungsstroms", "Disclosure — not part of the cash flow"))));
                r.Add(Marke("stand.tabelle.vermiedene_kosten"));
                r.Add(Marke("/wenn"));
                r.Add(Marke("/je"));

                r.Add(Erklaert(A("Heading2", T(L("Szenarien ", "Scenarios "))), new[]
                {
                    "Die Überschrift setzt die Namen der drei Szenarien als Platzhalter ein ({{wirtschaft.szenario.<s>.name}}). "
                    + "Darunter die Szenarientafel, das Bild der Spanne, die Annahmen der Szenarien, der Vorschlag zur "
                    + "Entscheidung und ein Satz mit den Werten der besten Variante ({{wirtschaft.beste.*}}).",
                }, new[]
                {
                    "The heading inserts the names of the three scenarios as placeholders ({{wirtschaft.szenario.<s>.name}}). Below "
                    + "it the scenario table, the image of the range, the assumptions of the scenarios, the proposal for the "
                    + "decision and a sentence with the values of the best variant ({{wirtschaft.beste.*}}).",
                }));
                Paragraph szenarien = (Paragraph)r[r.Count - 1];
                szenarien.InsertBefore(P("wirtschaft.szenario.unguenstig.name"), szenarien.GetFirstChild<CommentRangeEnd>());
                szenarien.InsertBefore(T(" / "), szenarien.GetFirstChild<CommentRangeEnd>());
                szenarien.InsertBefore(P("wirtschaft.szenario.erwartet.name"), szenarien.GetFirstChild<CommentRangeEnd>());
                szenarien.InsertBefore(T(" / "), szenarien.GetFirstChild<CommentRangeEnd>());
                szenarien.InsertBefore(P("wirtschaft.szenario.guenstig.name"), szenarien.GetFirstChild<CommentRangeEnd>());
                r.Add(Marke("tabelle.wirtschaft.szenarien"));
                r.Add(Marke("#wenn hat.bild.wirtschaft.spanne"));
                r.Add(Bild("bild.wirtschaft.spanne", VOLL, VOLL / 2));
                r.Add(Marke("/wenn"));
                r.Add(A("Hinweis", P("wirtschaft.szenario.unguenstig.annahmen")));
                r.Add(A("Hinweis", P("wirtschaft.szenario.unguenstig.traegerpreise")));
                r.Add(A("Hinweis", P("wirtschaft.szenario.guenstig.annahmen")));
                r.Add(A("Hinweis", P("wirtschaft.szenario.guenstig.traegerpreise")));
                r.Add(A("Hinweis", P("wirtschaft.szenarioabdeckung")));
                r.Add(A("Normal", P("wirtschaft.vorschlag")));
                r.Add(Marke("#wenn hat.varianten"));
                r.Add(A("Normal", T(L("Beste Variante: ", "Best variant: ")), P("wirtschaft.beste.anzeige"),
                        T(L(" mit einer Kapitalwertdifferenz von ", " with a net present value difference of ")),
                        P("wirtschaft.beste.kapitalwert_diff"), T(".")));
                r.Add(Marke("/wenn"));

                r.Add(Marke("#wenn hat.tabelle.wirtschaft.nicht_monetaer"));
                r.Add(H2("Nicht monetäre Wirkungen", "Non-monetary effects"));
                r.Add(Marke("tabelle.wirtschaft.nicht_monetaer"));
                r.Add(Marke("/wenn"));

                r.Add(Marke("#wenn hat.sensitivitaet"));
                r.Add(H2("Sensitivitätsanalyse (Szenario „Erwartet“)", "Sensitivity analysis (scenario \"Expected\")"));
                r.Add(Hinweis("Kapitalwert der Variante gegenüber dem Stamm bei Veränderung je eines Einflussparameters; Zins und Preissteigerung wirken auf beide Projekte, Investitions- und Energiekosten-Ausschlag nur auf die Variante.",
                              "Net present value of the variant against the base when one influencing parameter changes at a time; interest and price escalation act on both projects, investment and energy cost deflection only on the variant."));
                r.Add(Marke("/wenn"));
                Standtafeln(r, "sensitivitaet");

                r.Add(Marke("#wenn hat.tabelle.strommengen"));
                r.Add(H2("Strommengen", "Electricity volumes"));
                r.Add(Marke("/wenn"));
                Standtafeln(r, "strommengen");

                r.Add(Marke("#wenn hat.emissionsbilanz"));
                r.Add(H2("Emissionsbilanz — gekoppelte vs. getrennte Erzeugung", "Emission balance — combined vs. separate generation"));
                r.Add(Marke("/wenn"));
                Standtafeln(r, "emissionsbilanz");

                r.Add(Marke("wirtschaft.hinweise"));
            }

            /// <summary>Je Stand mit dieser Tafel: Überschrift 3 und <c>{{stand.tabelle.&lt;name&gt;}}</c>.</summary>
            private static void Standtafeln(List<OpenXmlElement> r, string name)
            {
                r.Add(Marke("#je stand"));
                r.Add(Marke("#wenn hat.tabelle." + name));
                r.Add(Standkopf());
                r.Add(Marke("stand.tabelle." + name));
                r.Add(Marke("/wenn"));
                r.Add(Marke("/je"));
            }

            /// <summary>Die Musterzeile der Wirtschaftlichkeitswerte je Stand (Szenario „Erwartet“).</summary>
            private Table Wirtschaftstabelle()
            {
                (string De, string En, string Schluessel)[] spalten =
                {
                    ("Investition [€]", "Investment [€]", "stand.wirtschaft.investition|ohne einheit"),
                    ("Betriebskosten [€/a]", "Operating cost [€/a]", "stand.wirtschaft.betriebskosten|ohne einheit"),
                    ("Energiekosten [€/a]", "Energy cost [€/a]", "stand.wirtschaft.energiekosten|ohne einheit"),
                    ("Nettobarwert [€]", "Net present value [€]", "stand.wirtschaft.nettobarwert|ohne einheit"),
                    ("Kapitalwertdifferenz [€]", "NPV difference [€]", "stand.wirtschaft.kapitalwert_diff|ohne einheit|mit grund"),
                    ("Amortisation [a]", "Payback [a]", "stand.wirtschaft.amortisation|ohne einheit|mit grund"),
                };
                int erste = 1855, rest = (Beispielvorlage.INHALTSBREITE - erste) / spalten.Length;
                int[] breiten = new[] { erste }.Concat(spalten.Select(_ => rest)).ToArray();
                breiten[0] += Beispielvorlage.INHALTSBREITE - breiten.Sum();
                Table t = Tabelle(breiten);
                var kopf = new TableRow(new TableRowProperties(new TableHeader()), Zelle(breiten[0], null, T(L("Stand", "State"))));
                for (int i = 0; i < spalten.Length; i++) kopf.Append(Zelle(breiten[i + 1], null, T(L(spalten[i].De, spalten[i].En))));
                t.Append(kopf);
                var zeile = new TableRow(Zelle(breiten[0], null, P("#je stand"), P("stand.anzeige")));
                for (int i = 0; i < spalten.Length; i++)
                {
                    var inhalt = new List<OpenXmlElement> { P(spalten[i].Schluessel) };
                    if (i == spalten.Length - 1) inhalt.Add(P("/je"));
                    zeile.Append(Zelle(breiten[i + 1], null, inhalt.ToArray()));
                }
                t.Append(zeile);
                return t;
            }

            /// <summary>8 — Anhang: Simulationsstände, Datengrundlage, Hinweise des Laufs.</summary>
            private void Anhang(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Kapitelkopf("anhang"), new[]
                {
                    "Der Anhang: die Tafel der Simulationsstände, feste Sätze zur Datengrundlage – frei zu ändern – und "
                    + "{{bericht.warnungen}}, die Hinweise dieses Berichtslaufs als Aufzählung; ohne Hinweise entfällt sie.",
                }, new[]
                {
                    "The appendix: the table of simulation timestamps, fixed sentences on the data basis – change them freely – and "
                    + "{{bericht.warnungen}}, the notes of this report run as a bulleted list; without notes it is dropped.",
                }));
                r.Add(H2("Simulationsstände", "Simulation timestamps"));
                r.Add(Marke("tabelle.anhang.simulationsstaende"));
                r.Add(H2("Datengrundlage und Methodik", "Data basis and methodology"));
                r.Add(Text("Für diesen Bericht wurde jedes aufgeführte Projekt neu simuliert (stündliche Jahresrechnung) und anschließend wirtschaftlich bewertet; die Zahlen aller Kapitel stammen damit aus demselben Rechenlauf.",
                           "Every project listed here was simulated anew for this report (hourly annual calculation) and then evaluated economically; all chapters therefore share one calculation run."));
                r.Add(Text("Grundlage sind die je Projekt gespeicherten Simulationsergebnisse der EPOS-Plan-Simulation (stündliche Jahresrechnung). Varianten sind eigenständige Projektkopien, verknüpft über die Variantenliste des Stammprojekts.",
                           "The basis are the simulation results of the EPOS-Plan simulation stored per project (hourly annual calculation). Variants are independent project copies, linked through the variant list of the base project."));
                r.Add(Text("Gerätedaten stammen aus den hinterlegten Katalogen oder manuellen Eingaben, Klimadaten aus der dem Projekt zugeordneten Klimaregion.",
                           "Unit data come from the stored catalogues or manual input, climate data from the climate region assigned to the project."));
                r.Add(Marke("bericht.warnungen"));
            }

            /// <summary>9 — Anhang E: die Checkliste als Strukturtabelle, nur mit Wirtschaftlichkeit.</summary>
            private void AnhangE(List<OpenXmlElement> r)
            {
                r.Add(Erklaert(Marke("#wenn hat.wirtschaft"), new[]
                {
                    "Anhang E: die Checkliste für den Bewertungsbericht als Strukturtabelle, nur wenn eine Wirtschaftlichkeit "
                    + "vorliegt. Die Spalte „Stelle“ nennt die Kapitel eines Berichts, der seine Kapitel als "
                    + "Kapitelplatzhalter führt; in einer Vorlage aus Einzelelementen steht dort „nicht im Bericht“. Wer die "
                    + "Stellen braucht, setzt hier {{kapitel.anhang_e|ohne titel}} und für die genannten Abschnitte ihre "
                    + "Kapitelplatzhalter.",
                }, new[]
                {
                    "Annex E: the checklist for the evaluation report as a structured table, only when an economic viability "
                    + "exists. The column “Location” names the chapters of a report that holds its chapters as chapter "
                    + "placeholders; in a template of single elements it reads “not in the report”. If you need the locations, "
                    + "use {{kapitel.anhang_e|ohne titel}} here and chapter placeholders for the sections named.",
                }));
                var kopf = Kapitelkopf("anhang_e");
                kopf.ParagraphProperties.Append(new PageBreakBefore());
                r.Add(kopf);
                r.Add(Marke("tabelle.anhang_e.checkliste"));
                r.Add(Marke("/wenn"));
            }

            /// <summary>
            /// Die Mustertabelle (Konzept 6.4 Nr. 2): Alternativtext <c>{{muster.tabelle}}</c>, je Rolle eine Zelle mit
            /// Schattierung und Zeichenformat — wie im Kurzbericht. Die Engine liest die Rollen und entfernt die Tabelle.
            /// </summary>
            private Table Mustertabelle()
            {
                int spalte = Beispielvorlage.INHALTSBREITE / 4;
                Table t = Tabelle(spalte, spalte, spalte, spalte);
                TableProperties tp = t.GetFirstChild<TableProperties>();
                tp.GetFirstChild<TableLook>().Val = "0400";
                tp.Append(new TableDescription { Val = "{{muster.tabelle}}" });
                string id = Kommentar(new[]
                {
                    "Die Mustertabelle mit dem Alternativtext {{muster.tabelle}}: Schattierung und Schrift jeder Zelle gelten für "
                    + "die Zeilen dieser Rolle in allen Strukturtabellen – Stamm, Gruppe, Summe, Warnung. Rahmen, Kopfzeile und "
                    + "Schriftgröße aller Tabellen folgen dem Tabellenformat „EPOS Tabelle“ dieser Datei. EPOS-Plan liest die "
                    + "Mustertabelle und entfernt sie aus dem Bericht.",
                }, new[]
                {
                    "The pattern table with the alternative text {{muster.tabelle}}: shading and font of each cell apply to the rows "
                    + "of that role in all structured tables – base, group, total, warning. Borders, header row and font size of "
                    + "all tables follow the table style “EPOS Tabelle” of this file. EPOS-Plan reads the pattern table and "
                    + "removes it from the report.",
                });
                TableCell Rolle(string text, string fuellung, bool fett, string farbe, bool erste)
                {
                    var rp = new RunProperties();
                    if (fett) rp.Bold = new Bold();
                    if (farbe != null) rp.Color = new Color { Val = farbe };
                    var lauf = new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
                    return erste
                        ? Zelle(spalte, fuellung, new CommentRangeStart { Id = id }, lauf, new CommentRangeEnd { Id = id },
                                new Run(new CommentReference { Id = id }))
                        : Zelle(spalte, fuellung, lauf);
                }
                t.Append(new TableRow(
                    Rolle(L("Stamm", "Base"), "F2F2F2", false, null, true),
                    Rolle(L("Gruppe", "Group"), "DCE6F0", true, "1F4E79", false),
                    Rolle(L("Summe", "Total"), "E7E6E6", true, null, false),
                    Rolle(L("Warnung", "Warning"), null, false, "C00000", false)));
                return t;
            }
        }

        // ------------------------------------------------------------- Abschlussprüfung

        /// <summary>
        /// Stilregeln, Validator und die Regeln der Wache: jede Marke in eigenem Run mit <c>w:noProof</c>, kein „INEKON“,
        /// Blockmarken paarig, das Logo als Bildplatzhalter der Kopfzeile, die Bildrahmen mit Schlüssel im Alternativtext
        /// auf dem einen Platzhalterbild, eindeutige <c>wp:docPr/@id</c>, die Mustertabelle, das Tabellenformat, kein
        /// Kapitelplatzhalter, je Kommentar ein Verweis und die drei Eigenschaften in <c>custom.xml</c>.
        /// </summary>
        private static bool Abschlusspruefung(string pfad, Bauer bauer, int katalogfassung, TextWriter aus)
        {
            var befunde = new List<string>();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
            {
                MainDocumentPart main = doc.MainDocumentPart;
                befunde.AddRange(Pruefung.Stilbefunde(main.StyleDefinitionsPart?.Styles));
                if (!main.StyleDefinitionsPart.Styles.Elements<Style>().Any(s => s.StyleId?.Value == TABELLENSTIL_ID
                        && s.StyleName?.Val?.Value == TABELLENSTIL_NAME && s.Type?.Value == StyleValues.Table))
                    befunde.Add("Das Tabellenformat „" + TABELLENSTIL_NAME + "“ fehlt.");

                var teile = new List<(string Name, OpenXmlElement Wurzel)> { ("Rumpf", main.Document.Body) };
                teile.AddRange(main.HeaderParts.Select(h => ("Kopfzeile", (OpenXmlElement)h.Header)));
                teile.AddRange(main.FooterParts.Select(f => ("Fußzeile", (OpenXmlElement)f.Footer)));
                foreach ((string name, OpenXmlElement wurzel) in teile)
                {
                    List<string> gefunden = wurzel.Descendants<Paragraph>()
                        .SelectMany(p => Kurzbericht.Markenmuster.Matches(Beispielvorlage.Absatztext(p)).Select(m => m.Value)).ToList();
                    aus.WriteLine("  " + name + ": " + gefunden.Count + " Marken");
                    int inRuns = 0;
                    foreach (Run r in wurzel.Descendants<Run>())
                    {
                        string text = string.Concat(r.Elements<Text>().Select(x => x.Text));
                        if (!Kurzbericht.Markenmuster.IsMatch(text)) continue;
                        inRuns++;
                        if (Kurzbericht.Markenmuster.Match(text).Value != text)
                            befunde.Add(name + ": Run „" + text + "“ trägt neben der Marke weiteren Text.");
                        if (r.RunProperties?.NoProof == null)
                            befunde.Add(name + ": Marke „" + text + "“ ohne w:noProof.");
                    }
                    if (inRuns != gefunden.Count)
                        befunde.Add(name + ": " + gefunden.Count + " Marken im Text, aber " + inRuns + " in eigenen Runs.");
                    if (wurzel.InnerText.Contains("INEKON", StringComparison.Ordinal))
                        befunde.Add(name + ": enthält „INEKON“.");
                }

                List<string> marken = main.Document.Body.Descendants<Paragraph>()
                    .SelectMany(p => Kurzbericht.Markenmuster.Matches(Beispielvorlage.Absatztext(p)).Select(m => m.Value)).ToList();
                if (marken.Count(m => m.StartsWith("{{#je", StringComparison.Ordinal)) != marken.Count(m => m == "{{/je}}")
                    || marken.Count(m => m.StartsWith("{{#wenn", StringComparison.Ordinal)) != marken.Count(m => m == "{{/wenn}}"))
                    befunde.Add("Rumpf: Blockmarken nicht paarig.");
                if (marken.Any(m => m.StartsWith("{{kapitel.", StringComparison.Ordinal) || m.StartsWith("{{bericht.inhalt", StringComparison.Ordinal)))
                    befunde.Add("Rumpf: ein Kapitelplatzhalter — die ausführliche Vorlage baut jeden Abschnitt aus Einzelelementen.");

                befunde.AddRange(Beispielvorlage.Bildbefunde(main));
                List<string> rahmen = main.Document.Body.Descendants<DW.DocProperties>().Select(d => d.Description?.Value).ToList();
                if (!rahmen.SequenceEqual(bauer.Bildschluessel))
                    befunde.Add("Rumpf: Bildrahmen " + string.Join(", ", rahmen) + ", erwartet " + string.Join(", ", bauer.Bildschluessel));
                if (main.ImageParts.Count() != 1 || !main.TryGetPartById(Kurzbericht.KENNUNG_BILD, out OpenXmlPart bildteil)
                    || !Beispielvorlage.Bytes(bildteil).SequenceEqual(Kurzbericht.Bildplatzhalter()))
                    befunde.Add("Rumpf: Das Platzhalterbild der Bildrahmen fehlt oder weicht ab.");
                List<uint> kennungen = main.Document.Body.Descendants<DW.DocProperties>()
                    .Concat(main.HeaderParts.SelectMany(h => h.Header.Descendants<DW.DocProperties>()))
                    .Select(d => d.Id?.Value ?? 0U).ToList();
                if (kennungen.Distinct().Count() != kennungen.Count)
                    befunde.Add("wp:docPr/@id nicht eindeutig: " + string.Join(", ", kennungen));

                if (!main.Document.Body.Elements<Table>().Any(tb =>
                        tb.GetFirstChild<TableProperties>()?.GetFirstChild<TableDescription>()?.Val?.Value == "{{muster.tabelle}}"))
                    befunde.Add("Rumpf: Die Mustertabelle fehlt.");

                List<string> kommentare = main.WordprocessingCommentsPart?.Comments?.Elements<Comment>()
                    .Select(c => c.Id?.Value).OrderBy(x => x, StringComparer.Ordinal).ToList() ?? new List<string>();
                List<string> verweise = main.Document.Body.Descendants<CommentReference>()
                    .Select(r => r.Id?.Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
                if (kommentare.Count != bauer.Kommentare.Count || !kommentare.SequenceEqual(verweise))
                    befunde.Add("Kommentare (" + string.Join(", ", kommentare) + ") und Verweise (" + string.Join(", ", verweise) + ") passen nicht.");
                if (main.Document.Body.Elements<Paragraph>().Count(p => Beispielvorlage.Stilkennung(p) == Pruefung.KAPITELKOPF_ID) != 7)
                    befunde.Add("Rumpf: erwartet sieben Kapitelköpfe (Projekt bis Anhang E).");

                List<Eigenschaft> liste = doc.CustomFilePropertiesPart?.Properties?.Elements<Eigenschaft>().ToList() ?? new List<Eigenschaft>();
                foreach ((string name, string wert) in new[]
                         {
                             (Beispielvorlage.EIGENSCHAFT_KATALOGFASSUNG, katalogfassung.ToString(CultureInfo.InvariantCulture)),
                             (Beispielvorlage.EIGENSCHAFT_VORLAGE, KENNUNG), (Kurzbericht.EIGENSCHAFT_SPRACHE, bauer.Sprache),
                         })
                {
                    List<string> ist = liste.Where(e => e.Name?.Value == name).Select(e => e.InnerText).ToList();
                    if (ist.Count != 1 || ist[0] != wert) befunde.Add("custom.xml: " + name + " = „" + string.Join("“, „", ist) + "“, erwartet „" + wert + "“.");
                }
            }
            foreach (string b in befunde) aus.WriteLine("    Befund: " + b);
            int fehler = Pruefung.Validieren(pfad, aus, mustertabelle: true);
            aus.WriteLine("  Regeln: " + (befunde.Count == 0 ? "grün" : befunde.Count + " Befunde") + "; Validator: " + fehler + " Fehler");
            return befunde.Count == 0 && fehler == 0;
        }
    }
}
