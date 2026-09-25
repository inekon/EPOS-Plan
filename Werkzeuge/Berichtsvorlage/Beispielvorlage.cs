using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Berichtsvorlage
{
    /// <summary>
    /// <b>Die Beispielvorlage mit Platzhaltern</b> — aus der bereinigten Vorlage und dem Aufbau
    /// des bisherigen Berichts (Konzept Berichtsvorlagen, 6.3 und Anhang B.3; Anwenderentscheid
    /// 25.09.2026 zu BV-Q19 „Erstelle eine Vorlage aus dem bisherigen Bericht als Beispiel“ und
    /// zu BV-Q8: Ersteller mit Firma, Programmname und Versionsnummer).
    ///
    /// <para><b>Aufbau (volle Stufe, ab BV-E2).</b> Abschnitt 1 ist das Deckblatt ohne Kopf-
    /// und Fußzeile: Titel, Untertitel, die Tabelle Beschriftung · Wert, der Produktausweis
    /// des Gebäudemodells und die Zeile „Erstellt mit“; der Abschnittswechsel „nächste Seite“
    /// ist der Seitenumbruch. Abschnitt 2 trägt Kopf- und Fußzeile der Vorlage, das
    /// Inhaltsverzeichnis <c>{{kapitel.inhalt}}</c> und je Kapitel eine Überschrift im Format
    /// „EPOS Kapitelkopf“ mit dem heutigen Kapiteltitel, darunter den Kapitelplatzhalter mit
    /// <c>|ohne titel</c> in einem eigenen Absatz. Beschriftungen und Festtexte des Deckblatts
    /// sind <c>{{text.*}}</c> — die Vorlage bleibt sprachneutral (4.9).</para>
    ///
    /// <para><b>Stufe mit Sammelanker (<c>--sammelanker</c>, BV-E1).</b> Der Rumpf besteht nur
    /// aus dem Absatz <c>{{bericht.inhalt}}</c> an der Stelle von Deckblatt, Inhaltsverzeichnis
    /// und Kapiteln (die dann noch Kapitel des Bausteinwegs sind); Kopf- und Fußzeile wie oben.</para>
    ///
    /// <para><b>Jeder Platzhalter steht in einem eigenen Run mit <c>w:noProof</c></b> — so
    /// findet die Engine ihn ungeteilt, und die Rechtschreibprüfung unterstreicht ihn nicht.
    /// Die Erläuterungen stehen als Word-Kommentare am Deckblatt und am ersten
    /// Kapitelplatzhalter; Word erlaubt keine Kommentare in Kopf- und Fußzeilen.</para>
    /// </summary>
    internal static class Beispielvorlage
    {
        internal const string AUTOR = "EPOS-Plan";
        internal const string KUERZEL = "EP";

        /// <summary>
        /// Die Form eines Platzhalters, wie ihn die Wache sucht: Schlüssel, dahinter
        /// wahlweise eine Formatangabe nach „|“ (etwa <c>|ohne titel</c>).
        /// </summary>
        internal static readonly Regex Platzhaltermuster =
            new Regex(@"\{\{[a-z_.]+(\|[a-z ]+)?\}\}", RegexOptions.CultureInvariant);

        private const string KOMMENTAR_DECKBLATT = "0";
        private const string KOMMENTAR_KAPITEL = "1";

        // Maße und Farben der Eigenschaftstabelle wie im heutigen Deckblatt
        // (WordKontext.Eigenschaften, NeueTabelle, Zelle; WordBerichtGenerator.INHALT_B,
        // STAMM_FILL, RAHMEN, SCHRIFT_TABELLE) — hier als Werte, damit das Werkzeug ohne
        // EPOS.Kern auskommt.
        private const int INHALTSBREITE = 9355;
        private const int SPALTE_BESCHRIFTUNG = 2800;
        private const string FUELLUNG_BESCHRIFTUNG = "F2F2F2";
        private const string RAHMENFARBE = "BFBFBF";
        private const string SCHRIFT_TABELLE = "18";

        /// <summary>Ein Kapitel der Vorlage: Platzhalter samt Formatangabe, Überschrift (null = keine eigene).</summary>
        internal sealed class Kapitel
        {
            internal string Platzhalter;
            internal string Ueberschrift;
        }

        /// <summary>
        /// Die Kapitel in heutiger Folge (<c>WordBerichtGenerator.AktiveBausteine</c> ohne
        /// Deckblatt: Anhang E als letzte Seite nach dem Anhang). Die Überschriften sind die,
        /// die der Bericht heute druckt (<c>Ueberschrift1</c> der Bausteine; Anhang E
        /// <c>WIRT_AE_TITEL</c>) — dieselbe Liste hält die Messlatte
        /// <c>BerichtBlattstrukturWacheTests</c>. Das Inhaltsverzeichnis bringt seine
        /// Überschrift „Inhalt“ selbst mit und steht ohne Kapitelkopf.
        /// </summary>
        internal static readonly Kapitel[] Kapitelfolge =
        {
            new Kapitel { Platzhalter = "kapitel.inhalt" },
            new Kapitel { Platzhalter = "kapitel.projekt|ohne titel", Ueberschrift = "Projektbeschreibung" },
            new Kapitel { Platzhalter = "kapitel.komponenten|ohne titel", Ueberschrift = "Komponenten & Varianten" },
            new Kapitel { Platzhalter = "kapitel.ergebnisse|ohne titel", Ueberschrift = "Berechnungsergebnisse je Variante" },
            new Kapitel { Platzhalter = "kapitel.vergleich|ohne titel", Ueberschrift = "Variantenvergleich" },
            new Kapitel { Platzhalter = "kapitel.wirtschaftlichkeit|ohne titel", Ueberschrift = "Wirtschaftlichkeit" },
            new Kapitel { Platzhalter = "kapitel.anhang|ohne titel", Ueberschrift = "Anhang" },
            new Kapitel
            {
                Platzhalter = "kapitel.anhang_e|ohne titel",
                Ueberschrift = "Checkliste für den Bewertungsbericht (DIN EN 17463, Anhang E)"
            },
        };

        /// <summary>Die Zeilen der Deckblatt-Tabelle: Beschriftung · Wert (Anhang B.3).</summary>
        private static readonly (string Beschriftung, string Wert)[] Eigenschaften =
        {
            ("text.kunde", "projekt.kunde"),
            ("text.bearbeiter", "projekt.bearbeiter"),
            ("text.ersteller", "ersteller.firma"),
            ("text.varianten", "bericht.varianten.liste"),
            ("text.datum", "bericht.datum"),
        };

        internal static int Ausfuehren(string quelle, string ziel, bool sammelanker, TextWriter aus)
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

            aus.WriteLine((sammelanker ? "Beispielvorlage, Stufe mit Sammelanker: " : "Beispielvorlage: ") + quelle + " → " + ziel);

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
                using (WordprocessingDocument doc = WordprocessingDocument.Open(kopie, true))
                    Baue(doc, sammelanker, aus);

                if (!Abschlusspruefung(kopie, sammelanker, aus))
                {
                    Console.Error.WriteLine("Die Beispielvorlage ist nicht in Ordnung — das Ziel bleibt unverändert.");
                    return Program.PRUEFUNG;
                }
                File.Copy(kopie, ziel, true);
                aus.WriteLine("  Geschrieben: " + ziel);
                return Program.OK;
            }
            finally { Program.Loeschen(kopie); }
        }

        // ------------------------------------------------------------- Aufbau

        private static void Baue(WordprocessingDocument doc, bool sammelanker, TextWriter aus)
        {
            MainDocumentPart main = doc.MainDocumentPart;
            Body body = main.Document.Body;
            SectionProperties hauptabschnitt = body.Elements<SectionProperties>().LastOrDefault()
                ?? throw new InvalidOperationException("Die Vorlage hat keine Abschnittseigenschaften (w:sectPr) am Rumpfende.");

            // Rumpf leeren; die sectPr mit Kopf-/Fußzeilenverweisen, Seitengröße und Rändern bleibt.
            foreach (OpenXmlElement el in body.ChildElements.Where(c => !(c is SectionProperties)).ToList())
                el.Remove();

            if (sammelanker)
            {
                body.InsertBefore(Absatz("Normal", Platzhalter("bericht.inhalt")), hauptabschnitt);
            }
            else
            {
                foreach (OpenXmlElement el in Deckblatt(hauptabschnitt))
                    body.InsertBefore(el, hauptabschnitt);

                bool erstes = true;
                foreach (Kapitel k in Kapitelfolge)
                {
                    if (k.Ueberschrift != null)
                        body.InsertBefore(Absatz(Pruefung.KAPITELKOPF_ID, Lauf(k.Ueberschrift)), hauptabschnitt);
                    Paragraph p = erstes
                        ? Absatz("Normal", new CommentRangeStart { Id = KOMMENTAR_KAPITEL }, Platzhalter(k.Platzhalter),
                                 new CommentRangeEnd { Id = KOMMENTAR_KAPITEL }, new Run(new CommentReference { Id = KOMMENTAR_KAPITEL }))
                        : Absatz("Normal", Platzhalter(k.Platzhalter));
                    body.InsertBefore(p, hauptabschnitt);
                    erstes = false;
                }
            }
            main.Document.Save();

            Kopfzeile(main, aus);
            Fusszeile(main, aus);
            if (!sammelanker) Kommentare(main);

            doc.PackageProperties.LastModifiedBy = AUTOR;
            if (sammelanker)
            {
                doc.PackageProperties.Title = "Berichtsvorlage EPOS-Plan — Stufe mit Sammelanker";
                doc.PackageProperties.Description =
                    "Standardvorlage des Word-Berichts in der Stufe mit Sammelanker: der Rumpf setzt den ganzen Bericht ein; Kopf- und Fußzeile als Platzhalter.";
            }
            else
            {
                doc.PackageProperties.Title = "Berichtsvorlage EPOS-Plan — Beispiel mit Platzhaltern";
                doc.PackageProperties.Description =
                    "Aus dem bisherigen Bericht abgeleitete Vorlage: Deckblatt, Kopf- und Fußzeile und Kapitel als Platzhalter; spätere Standardvorlage des Word-Berichts.";
            }
        }

        /// <summary>
        /// Abschnitt 1: Titel, Untertitel, Tabelle Beschriftung · Wert, Produktausweis, Zeile
        /// „Erstellt mit“. Der letzte Absatz trägt die Abschnittseigenschaften des Deckblatts —
        /// eine Kopie der Hauptabschnitts-sectPr (Seitengröße, Ränder) ohne Kopf- und
        /// Fußzeilenverweis: Der erste Abschnitt hat damit weder Kopf- noch Fußzeile, der
        /// zweite behält seine ausdrücklichen Verweise.
        /// </summary>
        private static IEnumerable<OpenXmlElement> Deckblatt(SectionProperties hauptabschnitt)
        {
            Paragraph titel = Absatz("Title", new CommentRangeStart { Id = KOMMENTAR_DECKBLATT }, Platzhalter("bericht.titel"));
            Paragraph untertitel = Absatz("Subtitle", Platzhalter("bericht.untertitel"));
            Table tabelle = Eigenschaftstabelle();
            Paragraph ausweis = Absatz("Hinweis", Platzhalter("bericht.gebaeudemodell.ausweis|leer statt strich"));
            Paragraph erstelltMit = Absatz("Hinweis", Platzhalter("text.erstellt_mit"), Lauf(" "),
                                           Platzhalter("ersteller.programm"), Lauf(" "), Platzhalter("ersteller.version"),
                                           new CommentRangeEnd { Id = KOMMENTAR_DECKBLATT },
                                           new Run(new CommentReference { Id = KOMMENTAR_DECKBLATT }));

            var deckblattAbschnitt = (SectionProperties)hauptabschnitt.CloneNode(true);
            deckblattAbschnitt.RemoveAllChildren<HeaderReference>();
            deckblattAbschnitt.RemoveAllChildren<FooterReference>();
            erstelltMit.ParagraphProperties.AddChild(deckblattAbschnitt);

            return new OpenXmlElement[] { titel, untertitel, tabelle, ausweis, erstelltMit };
        }

        /// <summary>Die Tabelle Beschriftung · Wert, gestaltet wie die heutige Eigenschaftstabelle des Deckblatts.</summary>
        private static Table Eigenschaftstabelle()
        {
            int wert = INHALTSBREITE - SPALTE_BESCHRIFTUNG;
            var t = new Table(
                new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Dxa, Width = Zahl(INHALTSBREITE) },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4U, Color = RAHMENFARBE },
                        new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = RAHMENFARBE },
                        new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = RAHMENFARBE },
                        new RightBorder { Val = BorderValues.Single, Size = 4U, Color = RAHMENFARBE },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = RAHMENFARBE },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = RAHMENFARBE })),
                new TableGrid(new GridColumn { Width = Zahl(SPALTE_BESCHRIFTUNG) }, new GridColumn { Width = Zahl(wert) }));
            foreach ((string beschriftung, string schluessel) in Eigenschaften)
                t.Append(new TableRow(Zelle(beschriftung, SPALTE_BESCHRIFTUNG, true, FUELLUNG_BESCHRIFTUNG),
                                      Zelle(schluessel, wert, false, null)));
            return t;
        }

        private static TableCell Zelle(string schluessel, int breite, bool fett, string fuellung)
        {
            var tcp = new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = Zahl(breite) });
            if (fuellung != null) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fuellung });
            tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });

            var rp = new RunProperties();
            if (fett) rp.Bold = new Bold();
            rp.FontSize = new FontSize { Val = SCHRIFT_TABELLE };
            var p = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { Before = "20", After = "20" },
                                        new Justification { Val = JustificationValues.Left }),
                Platzhalter(schluessel, rp));
            return new TableCell(tcp, p);
        }

        /// <summary>Kopfzeile: „EPOS-Plan“ wird <c>{{ersteller.programm}}</c>, der Rest bleibt.</summary>
        private static void Kopfzeile(MainDocumentPart main, TextWriter aus)
        {
            int treffer = 0;
            foreach (HeaderPart teil in main.HeaderParts)
            {
                if (ErsetzeText(teil.Header, "EPOS-Plan", "ersteller.programm") != null) treffer++;
                teil.Header.Save();
            }
            if (treffer != 1)
                throw new InvalidOperationException("Kopfzeile: „EPOS-Plan“ " + treffer + "-mal gefunden, erwartet genau einmal.");
            aus.WriteLine("  Kopfzeile: „EPOS-Plan“ → {{ersteller.programm}}");
        }

        /// <summary>
        /// Fußzeile: „INEKON GmbH“ wird <c>{{ersteller.firma}}</c>, „Seite“ wird
        /// <c>{{text.seite}}</c>, das DATE-Feld wird an seiner Stelle <c>{{bericht.datum}}</c>
        /// (6.7: DATE zeigt das Datum des Öffnens, nicht das des Berichts); PAGE und NUMPAGES
        /// bleiben Felder. Die Tabstopps bleiben: links Firma, Mitte Datum, rechts Seite.
        /// </summary>
        private static void Fusszeile(MainDocumentPart main, TextWriter aus)
        {
            int firma = 0, datum = 0, seite = 0;
            foreach (FooterPart teil in main.FooterParts)
            {
                Footer f = teil.Footer;
                RunProperties muster = ErsetzeText(f, "INEKON GmbH", "ersteller.firma");
                if (muster != null) firma++;

                foreach (SimpleField feld in f.Descendants<SimpleField>()
                             .Where(x => (x.Instruction?.Value ?? "").TrimStart().StartsWith("DATE", StringComparison.OrdinalIgnoreCase))
                             .ToList())
                {
                    feld.InsertBeforeSelf(Platzhalter("bericht.datum", muster));
                    feld.Remove();
                    datum++;
                }

                if (ErsetzeText(f, "Seite", "text.seite") != null) seite++;
                f.Save();
            }
            if (firma != 1 || seite != 1)
                throw new InvalidOperationException("Fußzeile: „INEKON GmbH“ " + firma + "-mal, „Seite“ " + seite + "-mal gefunden, erwartet je einmal.");
            aus.WriteLine("  Fußzeile: „INEKON GmbH“ → {{ersteller.firma}}, DATE-Feld (" + datum
                          + ") → {{bericht.datum}}, „Seite“ → {{text.seite}}; PAGE/NUMPAGES bleiben Felder");
        }

        /// <summary>
        /// Teilt den Run, dessen Text <paramref name="alt"/> enthält, in bis zu drei Runs:
        /// davor, den Platzhalter (eigener Run, Zeichenformat des Originals plus
        /// <c>w:noProof</c>) und danach — samt den übrigen Kindern des Runs (Tabulator,
        /// Feldzeichen), in ihrer Reihenfolge. Rückgabe: das Zeichenformat des Originals
        /// (für weitere Platzhalter derselben Zeile) oder null, wenn nichts gefunden wurde.
        /// </summary>
        private static RunProperties ErsetzeText(OpenXmlElement wurzel, string alt, string schluessel)
        {
            Text t = wurzel.Descendants<Text>().FirstOrDefault(x => x.Text.Contains(alt, StringComparison.Ordinal));
            if (t == null || !(t.Parent is Run run)) return null;

            RunProperties rp = run.RunProperties;
            int i = t.Text.IndexOf(alt, StringComparison.Ordinal);
            string vor = t.Text.Substring(0, i);
            string nach = t.Text.Substring(i + alt.Length);
            List<OpenXmlElement> kinder = run.ChildElements.Where(c => !(c is RunProperties)).ToList();
            int stelle = kinder.IndexOf(t);

            Run davor = NeuerLauf(rp);
            foreach (OpenXmlElement c in kinder.Take(stelle)) davor.Append(c.CloneNode(true));
            if (vor.Length > 0) davor.Append(new Text(vor) { Space = SpaceProcessingModeValues.Preserve });

            Run danach = NeuerLauf(rp);
            if (nach.Length > 0) danach.Append(new Text(nach) { Space = SpaceProcessingModeValues.Preserve });
            foreach (OpenXmlElement c in kinder.Skip(stelle + 1)) danach.Append(c.CloneNode(true));

            if (davor.ChildElements.Any(c => !(c is RunProperties))) run.InsertBeforeSelf(davor);
            run.InsertBeforeSelf(Platzhalter(schluessel, rp));
            if (danach.ChildElements.Any(c => !(c is RunProperties))) run.InsertBeforeSelf(danach);
            run.Remove();
            return rp == null ? new RunProperties() : (RunProperties)rp.CloneNode(true);
        }

        /// <summary>Die Erläuterungen als Word-Kommentare (ohne Datum — die Datei bleibt bei jedem Lauf gleich).</summary>
        private static void Kommentare(MainDocumentPart main)
        {
            WordprocessingCommentsPart teil = main.WordprocessingCommentsPart ?? main.AddNewPart<WordprocessingCommentsPart>();
            teil.Comments = new Comments(
                Kommentar(KOMMENTAR_DECKBLATT,
                    "Beispielvorlage des Berichts. Text in doppelten geschweiften Klammern ist ein Platzhalter, etwa "
                    + "{{projekt.kunde}}: Beim Erstellen des Berichts setzt EPOS-Plan an seine Stelle den Wert aus dem Projekt. "
                    + "Schrift, Größe und Farbe des Platzhalters gelten für den eingesetzten Wert. Hinter einem senkrechten "
                    + "Strich folgt wahlweise eine Formatangabe, etwa |leer statt strich: Fehlt der Wert, bleibt die Stelle leer.",
                    "Beschriftungen wie {{text.kunde}} erscheinen in der Sprache der Oberfläche, so bleibt die Vorlage "
                    + "sprachneutral. Platzhalter dürfen verschoben, formatiert und gelöscht werden; der Text zwischen den "
                    + "Klammern muss unverändert bleiben. Auch Kopf- und Fußzeile tragen Platzhalter, die Seitenzahlen sind "
                    + "Word-Felder. Das Deckblatt ist ein eigener Abschnitt ohne Kopf- und Fußzeile.",
                    "Diese Datei ist aus dem bisherigen Bericht abgeleitet und die spätere Standardvorlage des Word-Berichts. "
                    + "Kommentare erscheinen nicht im fertigen Bericht."),
                Kommentar(KOMMENTAR_KAPITEL,
                    "Ein Kapitelplatzhalter wie {{kapitel.inhalt}} steht allein in seinem Absatz. Beim Erstellen ersetzt "
                    + "EPOS-Plan den ganzen Absatz durch einen ganzen Abschnitt des Berichts – mit Überschriften, Texten, "
                    + "Tabellen und Diagrammen. {{kapitel.inhalt}} erzeugt das Inhaltsverzeichnis samt seiner Überschrift.",
                    "Bei den übrigen Kapiteln steht die Überschrift in der Vorlage, im Absatzformat „EPOS Kapitelkopf“; die "
                    + "Angabe |ohne titel unterdrückt die eigene Überschrift des Kapitels. Entfällt ein Kapitel – abgewählt "
                    + "oder ohne Daten –, entfällt die Überschrift darüber mit. Die Kapitel stehen in der Reihenfolge des "
                    + "bisherigen Berichts."));
            teil.Comments.Save();
        }

        private static Comment Kommentar(string id, params string[] absaetze)
        {
            var c = new Comment { Id = id, Author = AUTOR, Initials = KUERZEL };
            for (int i = 0; i < absaetze.Length; i++)
            {
                var p = new Paragraph();
                if (i == 0) p.Append(new Run(new AnnotationReferenceMark()));
                p.Append(new Run(new Text(absaetze[i]) { Space = SpaceProcessingModeValues.Preserve }));
                c.Append(p);
            }
            return c;
        }

        // ------------------------------------------------------------- Bausteine der Absätze

        private static Paragraph Absatz(string stil, params OpenXmlElement[] inhalt)
        {
            var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = stil }));
            foreach (OpenXmlElement e in inhalt) p.Append(e);
            return p;
        }

        private static Run Lauf(string text)
            => new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        private static Run NeuerLauf(RunProperties rp)
        {
            var r = new Run();
            if (rp != null) r.Append(rp.CloneNode(true));
            return r;
        }

        /// <summary>Ein Platzhalter in eigenem Run, mit <c>w:noProof</c> (Zeichenformat nach <paramref name="muster"/>).</summary>
        private static Run Platzhalter(string schluessel, RunProperties muster = null)
        {
            RunProperties rp = muster != null ? (RunProperties)muster.CloneNode(true) : new RunProperties();
            rp.NoProof = new NoProof();
            return new Run(rp, new Text("{{" + schluessel + "}}") { Space = SpaceProcessingModeValues.Preserve });
        }

        private static string Zahl(int wert) => wert.ToString(CultureInfo.InvariantCulture);

        // ------------------------------------------------------------- Abschlussprüfung

        /// <summary>
        /// Stilregeln, Validator und die Regeln der Wache
        /// (<c>EPOS.Kern.Tests/BerichtsvorlageDateiWacheTests</c>): jeder Platzhalter in
        /// eigenem Run mit <c>w:noProof</c>, kein „INEKON GmbH“ mehr, jeder Kapitelplatzhalter
        /// mit <c>|ohne titel</c> unter einer Überschrift im Format „EPOS Kapitelkopf“. Gibt die
        /// Platzhalter je Teil aus.
        /// </summary>
        private static bool Abschlusspruefung(string pfad, bool sammelanker, TextWriter aus)
        {
            var befunde = new List<string>();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
            {
                MainDocumentPart main = doc.MainDocumentPart;
                befunde.AddRange(Pruefung.Stilbefunde(main.StyleDefinitionsPart?.Styles));

                var teile = new List<(string Name, OpenXmlElement Wurzel)> { ("Rumpf", main.Document.Body) };
                teile.AddRange(main.HeaderParts.Select(h => ("Kopfzeile", (OpenXmlElement)h.Header)));
                teile.AddRange(main.FooterParts.Select(f => ("Fußzeile", (OpenXmlElement)f.Footer)));
                foreach ((string name, OpenXmlElement wurzel) in teile)
                {
                    List<string> gefunden = wurzel.Descendants<Paragraph>()
                        .SelectMany(p => Platzhaltermuster.Matches(Absatztext(p)).Select(m => m.Value)).ToList();
                    aus.WriteLine("  " + name + ": " + gefunden.Count + " Platzhalter — " + string.Join(" ", gefunden));

                    int inRuns = 0;
                    foreach (Run r in wurzel.Descendants<Run>())
                    {
                        string text = string.Concat(r.Elements<Text>().Select(t => t.Text));
                        if (!Platzhaltermuster.IsMatch(text)) continue;
                        inRuns++;
                        if (Platzhaltermuster.Match(text).Value != text)
                            befunde.Add(name + ": Run „" + text + "“ trägt neben dem Platzhalter weiteren Text.");
                        if (r.RunProperties?.NoProof == null)
                            befunde.Add(name + ": Platzhalter „" + text + "“ ohne w:noProof.");
                    }
                    if (inRuns != gefunden.Count)
                        befunde.Add(name + ": " + gefunden.Count + " Platzhalter im Text, aber " + inRuns + " in eigenen Runs.");
                    if (wurzel.InnerText.Contains("INEKON", StringComparison.Ordinal))
                        befunde.Add(name + ": enthält noch „INEKON“.");
                }

                if (!sammelanker)
                {
                    List<Paragraph> absaetze = main.Document.Body.Elements<Paragraph>().ToList();
                    for (int i = 0; i < absaetze.Count; i++)
                    {
                        if (!Absatztext(absaetze[i]).Contains("|ohne titel}}", StringComparison.Ordinal)) continue;
                        Paragraph kopf = i > 0 ? absaetze[i - 1] : null;
                        if (kopf?.ParagraphProperties?.ParagraphStyleId?.Val?.Value != Pruefung.KAPITELKOPF_ID
                            || Absatztext(kopf).Trim().Length == 0)
                            befunde.Add("Rumpf: „" + Absatztext(absaetze[i]) + "“ steht nicht unter einem Kapitelkopf.");
                    }
                }
            }
            foreach (string b in befunde) aus.WriteLine("    Befund: " + b);
            int fehler = Pruefung.Validieren(pfad, aus);
            aus.WriteLine("  Regeln: " + (befunde.Count == 0 ? "grün" : befunde.Count + " Befunde") + "; Validator: " + fehler + " Fehler");
            return befunde.Count == 0 && fehler == 0;
        }

        private static string Absatztext(Paragraph p)
            => string.Concat(p.Descendants<Text>().Select(t => t.Text));
    }
}
