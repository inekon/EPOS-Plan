using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Eigenschaft = DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty;
using Eigenschaftsliste = DocumentFormat.OpenXml.CustomProperties.Properties;
using Paket = System.IO.Packaging;

namespace Berichtsvorlage
{
    /// <summary>
    /// Welche Vorlage der Modus <c>beispiel</c> baut. Der Wert steht als <c>EPOS.Vorlage</c> in
    /// <c>docProps/custom.xml</c> (<see cref="Beispielvorlage.Kennung"/>).
    /// </summary>
    internal enum Vorlagenart
    {
        /// <summary>Beispielvorlage im vollen Aufbau, erläutert in Kommentaren (Lehrvorlage) — „beispiel“.</summary>
        Beispiel,

        /// <summary>Standardvorlage im vollen Aufbau ohne Kommentare (<c>--standard</c>, ab BV-E2) — „standard“.</summary>
        Standard,

        /// <summary>Standardvorlage in der Stufe mit dem Sammelanker (<c>--sammelanker</c>, BV-E1) — „standard-sammelanker“.</summary>
        StandardSammelanker,
    }

    /// <summary>
    /// <b>Beispiel- und Standardvorlage mit Platzhaltern</b> — aus der bereinigten Stilvorlage und dem
    /// Aufbau des bisherigen Berichts (Konzept Berichtsvorlagen, 4.9, 5.6, 6.3 und Anhang B.3;
    /// Anwenderentscheid 25.09.2026 zu BV-Q19 „Erstelle eine Vorlage aus dem bisherigen Bericht als
    /// Beispiel“ und zu BV-Q8: Ersteller mit Firma, Programmname und Versionsnummer).
    ///
    /// <para><b>Voller Aufbau (Beispielvorlage; mit <c>--standard</c> die Standardvorlage ab BV-E2).</b>
    /// Abschnitt 1 ist das Deckblatt ohne Kopf- und Fußzeile: Titel, Untertitel, die Tabelle
    /// Beschriftung · Wert, der Produktausweis des Gebäudemodells und die Zeile „Erstellt mit“; der
    /// Abschnittswechsel „nächste Seite“ ist der Seitenumbruch. Abschnitt 2 trägt Kopf- und Fußzeile
    /// der Vorlage, das Inhaltsverzeichnis <c>{{kapitel.inhalt}}</c> und je Kapitel einen Kapitelkopf im
    /// Format „EPOS Kapitelkopf“ mit dem Platzhalter <c>{{text.kapitel_&lt;name&gt;}}</c>, darunter den
    /// Kapitelplatzhalter mit <c>|ohne titel</c> in einem eigenen Absatz. Beschriftungen, Festtexte
    /// und Kapiteltitel sind <c>{{text.*}}</c> — die Vorlage bleibt sprachneutral (4.9). Die
    /// Beispielvorlage erläutert sich in drei Word-Kommentaren; die Standardvorlage trägt keine, denn
    /// die Engine entfernte sie bei jedem Lauf und nennte ihre Zahl in der Laufmeldung (6.7).</para>
    ///
    /// <para><b>Stufe mit Sammelanker (<c>--sammelanker</c>, BV-E1).</b> Der Rumpf besteht nur aus
    /// dem Absatz <c>{{bericht.inhalt}}</c> an der Stelle von Deckblatt, Inhaltsverzeichnis und
    /// Kapiteln (die dann noch Kapitel des Bausteinwegs sind); Kopf- und Fußzeile wie oben.</para>
    ///
    /// <para><b>In jeder Art</b> ist das Firmenlogo der Kopfzeile ein Bildplatzhalter (Entscheid
    /// BV-E2-1): Das Bild bleibt an Ort, in Größe und Umbruch, trägt im Alternativtext
    /// <c>{{bild.ersteller.logo}}</c> und zeigt statt des Logos ein neutrales Platzhalterbild
    /// (<see cref="LogoPlatzhalter"/>). <c>docProps/custom.xml</c> führt <c>EPOS.Katalogfassung</c>
    /// (5.6) und <c>EPOS.Vorlage</c>; vorhandene Eigenschaften bleiben. <b>Jeder Platzhalter steht in
    /// einem eigenen Run mit <c>w:noProof</c></b> — so findet die Engine ihn ungeteilt, und die
    /// Rechtschreibprüfung unterstreicht ihn nicht. Word erlaubt keine Kommentare in Kopf- und
    /// Fußzeilen; die Erläuterungen — auch die zum Logo — stehen am Deckblatt, am
    /// Inhaltsverzeichnis und am ersten Kapitelkopf.</para>
    ///
    /// <para><b>Byte-gleich wiederholbar:</b> Das Werkzeug ändert eine Kopie der Quelle, und
    /// <see cref="Zeitstempel"/> gibt jedem Eintrag des Pakets den Zeitstempel der Quelle — auch dem
    /// neu angelegten Platzhalterbild.</para>
    /// </summary>
    internal static class Beispielvorlage
    {
        internal const string AUTOR = "EPOS-Plan";
        internal const string KUERZEL = "EP";

        /// <summary>
        /// Die Katalogfassung, wenn <c>--katalogfassung</c> fehlt: Katalog v4 (BV-E5, Anwenderentscheid BV-E4-4 — die
        /// Standardvorlage steht auf der laufenden Fassung; Tabellen und Bilder deckt sie über ihre Kapitel).
        /// </summary>
        internal const int KATALOGFASSUNG_VORGABE = 4;

        /// <summary>Die Eigenschaften in <c>custom.xml</c>; dieselben Namen liest der <c>Vorlagenpruefer</c> des Kerns.</summary>
        internal const string EIGENSCHAFT_KATALOGFASSUNG = "EPOS.Katalogfassung";
        internal const string EIGENSCHAFT_VORLAGE = "EPOS.Vorlage";

        /// <summary>Die Formatkennung benutzerdefinierter Dokumenteigenschaften, wie Word sie schreibt.</summary>
        internal const string FORMAT_EIGENE = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

        /// <summary>Der Alternativtext des Bildplatzhalters, der das Firmenlogo in der Kopfzeile vertritt (Entscheid BV-E2-1).</summary>
        internal const string LOGO_PLATZHALTER = "{{bild.ersteller.logo}}";

        /// <summary>
        /// Das neutrale Platzhalterbild als eingebettete Ressource (<c>Logoplatzhalter.png</c>, siehe
        /// LIESMICH): eine feste Datei, damit jeder Lauf auf jedem Rechner dieselben Bytes schreibt.
        /// </summary>
        private const string RESSOURCE_LOGO = "Berichtsvorlage.Logoplatzhalter.png";

        /// <summary>Der Teil des Platzhalterbilds im Paket — fest, damit jeder Lauf auf jedem System dieselben Bytes schreibt.</summary>
        private static readonly Uri TEIL_PLATZHALTER = new Uri("/word/media/logoplatzhalter.png", UriKind.Relative);

        internal const string BEZIEHUNG_BILD = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";

        /// <summary>Die beiden Vorlagen mit Platzhaltern im Vorlagenordner — ihr Name legt die Art fest (<see cref="ZielUndArt"/>).</summary>
        internal const string DATEI_STANDARD = "Berichtsvorlage_Standard.docx";
        internal const string DATEI_BEISPIEL = "Berichtsvorlage_Beispiel.docx";

        /// <summary>
        /// Die Form eines Platzhalters, wie ihn die Wache sucht: Schlüssel, dahinter
        /// wahlweise eine Formatangabe nach „|“ (etwa <c>|ohne titel</c>).
        /// </summary>
        internal static readonly Regex Platzhaltermuster =
            new Regex(@"\{\{[a-z_.]+(\|[a-z ]+)?\}\}", RegexOptions.CultureInvariant);

        /// <summary>Ein Kapitelplatzhalter unter einem Kapitelkopf; Gruppe 1 ist der Kapitelname.</summary>
        private static readonly Regex Kapitelmuster =
            new Regex(@"^\{\{kapitel\.([a-z_]+)\|ohne titel\}\}$", RegexOptions.CultureInvariant);

        private const string KOMMENTAR_DECKBLATT = "0";
        private const string KOMMENTAR_KAPITEL = "1";
        private const string KOMMENTAR_KAPITELKOPF = "2";

        // Maße und Farben der Eigenschaftstabelle wie im heutigen Deckblatt
        // (WordKontext.Eigenschaften, NeueTabelle, Zelle; WordBerichtGenerator.INHALT_B,
        // STAMM_FILL, RAHMEN, SCHRIFT_TABELLE) — hier als Werte, damit das Werkzeug ohne
        // EPOS.Kern auskommt.
        internal const int INHALTSBREITE = 9355;
        private const int SPALTE_BESCHRIFTUNG = 2800;
        internal const string FUELLUNG_BESCHRIFTUNG = "F2F2F2";
        internal const string RAHMENFARBE = "BFBFBF";
        internal const string SCHRIFT_TABELLE = "18";

        /// <summary>Ein Kapitel der Vorlage: Name, wie ihn <c>kapitel.&lt;name&gt;</c> führt, und ob ein Kapitelkopf darüber steht.</summary>
        internal sealed class Kapitel
        {
            internal string Name;
            internal bool MitKopf;

            /// <summary>Der Kapitelplatzhalter; unter einem Kapitelkopf mit <c>|ohne titel</c> — die Überschrift trägt der Kopf.</summary>
            internal string Platzhalter => "kapitel." + Name + (MitKopf ? "|ohne titel" : "");

            /// <summary>Der Platzhalter des Kapitelkopfs: der Kapiteltitel in der Sprache des Berichts (4.9); null = kein Kopf.</summary>
            internal string Kopfplatzhalter => MitKopf ? "text.kapitel_" + Name : null;
        }

        /// <summary>
        /// Die Kapitel in heutiger Folge (<c>WordBerichtGenerator.AktiveBausteine</c> ohne
        /// Deckblatt: Anhang E als letzte Seite nach dem Anhang). Das Inhaltsverzeichnis bringt
        /// seine Überschrift „Inhalt“ selbst mit und steht ohne Kapitelkopf; über jedem anderen
        /// Kapitel steht <c>{{text.kapitel_&lt;name&gt;}}</c>, den der Katalog mit dem Titel füllt,
        /// den der Bericht heute druckt (Projektbeschreibung, Komponenten &amp; Varianten,
        /// Berechnungsergebnisse je Variante, Variantenvergleich, Wirtschaftlichkeit, Anhang,
        /// Checkliste für den Bewertungsbericht (DIN EN 17463, Anhang E)). Die Folge hält die Wache
        /// <c>BerichtsvorlageDateiWacheTests</c> gegen den Code.
        /// </summary>
        internal static readonly Kapitel[] Kapitelfolge =
        {
            new Kapitel { Name = "inhalt" },
            new Kapitel { Name = "projekt", MitKopf = true },
            new Kapitel { Name = "komponenten", MitKopf = true },
            new Kapitel { Name = "ergebnisse", MitKopf = true },
            new Kapitel { Name = "vergleich", MitKopf = true },
            new Kapitel { Name = "wirtschaftlichkeit", MitKopf = true },
            new Kapitel { Name = "anhang", MitKopf = true },
            new Kapitel { Name = "anhang_e", MitKopf = true },
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

        /// <summary>Der Wert von <c>EPOS.Vorlage</c> in <c>custom.xml</c> je Art.</summary>
        internal static string Kennung(Vorlagenart art)
            => art == Vorlagenart.Standard ? "standard"
             : art == Vorlagenart.StandardSammelanker ? "standard-sammelanker"
             : "beispiel";

        private static string Bezeichnung(Vorlagenart art)
            => art == Vorlagenart.Standard ? "Standardvorlage, voller Aufbau"
             : art == Vorlagenart.StandardSammelanker ? "Standardvorlage, Stufe mit Sammelanker"
             : "Beispielvorlage";

        internal static int Ausfuehren(string quelle, string ziel, Vorlagenart art, int katalogfassung, TextWriter aus)
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
            string unstimmig = ZielUndArt(ziel, art);
            if (unstimmig != null)
            {
                Console.Error.WriteLine(unstimmig);
                return Program.AUFRUF;
            }

            aus.WriteLine(Bezeichnung(art) + ": " + quelle + " → " + ziel);

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
                Logostelle logo;
                using (WordprocessingDocument doc = WordprocessingDocument.Open(kopie, true))
                    logo = Baue(doc, art, katalogfassung, aus);
                Bildtausch(kopie, logo, aus);
                Zeitstempel(kopie, quelle);

                if (!Abschlusspruefung(kopie, art, katalogfassung, aus))
                {
                    Console.Error.WriteLine("Die Vorlage ist nicht in Ordnung — das Ziel bleibt unverändert.");
                    return Program.PRUEFUNG;
                }
                File.Copy(kopie, ziel, true);
                aus.WriteLine("  Geschrieben: " + ziel);
                return Program.OK;
            }
            finally { Program.Loeschen(kopie); }
        }

        /// <summary>
        /// Die beiden Vorlagen des Vorlagenordners entstehen nur in ihrer Art: Die ausgelieferte
        /// Standardvorlage darf die Kommentare der Lehrvorlage nicht tragen, die Beispielvorlage nicht
        /// die Kennung der Standardvorlage. Jedes andere Ziel (etwa eine Probe) nimmt jede Art.
        /// Rückgabe: der Aufruffehler oder null.
        /// </summary>
        private static string ZielUndArt(string ziel, Vorlagenart art)
        {
            string name = Path.GetFileName(ziel);
            if (string.Equals(name, DATEI_STANDARD, StringComparison.OrdinalIgnoreCase) && art == Vorlagenart.Beispiel)
                return "Die Standardvorlage entsteht mit --standard (voller Aufbau) oder --sammelanker (Stufe mit Sammelanker); "
                       + "ohne Schalter entstünde die Beispielvorlage mit ihren Kommentaren.";
            if (string.Equals(name, DATEI_BEISPIEL, StringComparison.OrdinalIgnoreCase) && art != Vorlagenart.Beispiel)
                return "Die Beispielvorlage entsteht ohne --standard und ohne --sammelanker.";
            return null;
        }

        // ------------------------------------------------------------- Aufbau

        /// <summary>Baut die Vorlage in der geöffneten Kopie; Rückgabe: die Stelle des Logos für <see cref="Bildtausch"/>.</summary>
        private static Logostelle Baue(WordprocessingDocument doc, Vorlagenart art, int katalogfassung, TextWriter aus)
        {
            MainDocumentPart main = doc.MainDocumentPart;
            Body body = main.Document.Body;
            SectionProperties hauptabschnitt = body.Elements<SectionProperties>().LastOrDefault()
                ?? throw new InvalidOperationException("Die Vorlage hat keine Abschnittseigenschaften (w:sectPr) am Rumpfende.");

            // Rumpf leeren; die sectPr mit Kopf-/Fußzeilenverweisen, Seitengröße und Rändern bleibt.
            foreach (OpenXmlElement el in body.ChildElements.Where(c => !(c is SectionProperties)).ToList())
                el.Remove();

            if (art == Vorlagenart.StandardSammelanker)
            {
                body.InsertBefore(Absatz("Normal", Platzhalter("bericht.inhalt")), hauptabschnitt);
            }
            else
            {
                bool kommentiert = art == Vorlagenart.Beispiel;
                foreach (OpenXmlElement el in Deckblatt(hauptabschnitt, kommentiert))
                    body.InsertBefore(el, hauptabschnitt);

                bool erstesKapitel = true, ersterKopf = true;
                foreach (Kapitel k in Kapitelfolge)
                {
                    if (k.MitKopf)
                    {
                        Run kopf = Platzhalter(k.Kopfplatzhalter);
                        body.InsertBefore(Absatz(Pruefung.KAPITELKOPF_ID,
                                                 kommentiert && ersterKopf ? Kommentiert(KOMMENTAR_KAPITELKOPF, kopf) : new OpenXmlElement[] { kopf }),
                                          hauptabschnitt);
                        ersterKopf = false;
                    }
                    Run platzhalter = Platzhalter(k.Platzhalter);
                    body.InsertBefore(Absatz("Normal",
                                             kommentiert && erstesKapitel ? Kommentiert(KOMMENTAR_KAPITEL, platzhalter) : new OpenXmlElement[] { platzhalter }),
                                      hauptabschnitt);
                    erstesKapitel = false;
                }
            }
            main.Document.Save();

            Kopfzeile(main, aus);
            Logostelle logo = LogoPlatzhalter(main);
            Fusszeile(main, aus);
            if (art == Vorlagenart.Beispiel) Kommentare(main);
            Dokumenteigenschaften(doc, katalogfassung, Kennung(art), aus);

            doc.PackageProperties.LastModifiedBy = AUTOR;
            switch (art)
            {
                case Vorlagenart.StandardSammelanker:
                    doc.PackageProperties.Title = "Berichtsvorlage EPOS-Plan — Stufe mit Sammelanker";
                    doc.PackageProperties.Description =
                        "Standardvorlage des Word-Berichts in der Stufe mit Sammelanker: der Rumpf setzt den ganzen Bericht ein; Kopf- und Fußzeile als Platzhalter.";
                    break;
                case Vorlagenart.Standard:
                    doc.PackageProperties.Title = "Berichtsvorlage EPOS-Plan — Standardvorlage";
                    doc.PackageProperties.Description =
                        "Standardvorlage des Word-Berichts: Deckblatt, Inhaltsverzeichnis, Kapitelköpfe und Kapitel, Kopf- und Fußzeile als Platzhalter.";
                    break;
                default:
                    doc.PackageProperties.Title = "Berichtsvorlage EPOS-Plan — Beispiel mit Platzhaltern";
                    doc.PackageProperties.Description =
                        "Aus dem bisherigen Bericht abgeleitete Lehrvorlage: Deckblatt, Kapitelköpfe, Kapitel, Kopf- und Fußzeile als Platzhalter, erläutert in Kommentaren; die Standardvorlage hat denselben Aufbau ohne Kommentare.";
                    break;
            }
            return logo;
        }

        /// <summary>
        /// Abschnitt 1: Titel, Untertitel, Tabelle Beschriftung · Wert, Produktausweis, Zeile
        /// „Erstellt mit“; in der Beispielvorlage umspannt der erste Kommentar das Deckblatt. Der
        /// letzte Absatz trägt die Abschnittseigenschaften des Deckblatts — eine Kopie der
        /// Hauptabschnitts-sectPr (Seitengröße, Ränder) ohne Kopf- und Fußzeilenverweis: Der erste
        /// Abschnitt hat damit weder Kopf- noch Fußzeile, der zweite behält seine ausdrücklichen
        /// Verweise.
        /// </summary>
        private static IEnumerable<OpenXmlElement> Deckblatt(SectionProperties hauptabschnitt, bool kommentiert)
        {
            Paragraph titel = kommentiert
                ? Absatz("Title", new CommentRangeStart { Id = KOMMENTAR_DECKBLATT }, Platzhalter("bericht.titel"))
                : Absatz("Title", Platzhalter("bericht.titel"));
            Paragraph untertitel = Absatz("Subtitle", Platzhalter("bericht.untertitel"));
            Table tabelle = Eigenschaftstabelle();
            Paragraph ausweis = Absatz("Hinweis", Platzhalter("bericht.gebaeudemodell.ausweis|leer statt strich"));
            Paragraph erstelltMit = Absatz("Hinweis", Platzhalter("text.erstellt_mit"), Lauf(" "),
                                           Platzhalter("ersteller.programm"), Lauf(" "), Platzhalter("ersteller.version"));
            if (kommentiert)
                erstelltMit.Append(new CommentRangeEnd { Id = KOMMENTAR_DECKBLATT },
                                   new Run(new CommentReference { Id = KOMMENTAR_DECKBLATT }));

            var deckblattAbschnitt = (SectionProperties)hauptabschnitt.CloneNode(true);
            deckblattAbschnitt.RemoveAllChildren<HeaderReference>();
            deckblattAbschnitt.RemoveAllChildren<FooterReference>();
            erstelltMit.ParagraphProperties.AddChild(deckblattAbschnitt);

            return new OpenXmlElement[] { titel, untertitel, tabelle, ausweis, erstelltMit };
        }

        /// <summary>Die Tabelle Beschriftung · Wert, gestaltet wie die heutige Eigenschaftstabelle des Deckblatts.</summary>
        internal static Table Eigenschaftstabelle()
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

        internal static TableCell Zelle(string schluessel, int breite, bool fett, string fuellung)
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

        /// <summary>Wo das Logo steht — Kopfzeilenteil, Beziehungskennung, Bildteil —, für <see cref="Bildtausch"/>.</summary>
        internal sealed class Logostelle
        {
            internal Uri Kopfteil;
            internal string Kennung;
            internal Uri Logoteil;
            internal string Ausdehnung;
            internal (int Breite, int Hoehe) Pixel;
        }

        /// <summary>
        /// Macht aus dem Firmenlogo der Stilvorlage einen Bildplatzhalter (Entscheid BV-E2-1): Das
        /// Bild bleibt an Ort, in Größe und Umbruch — Run, <c>wp:inline</c> und <c>wp:extent</c>
        /// unverändert — und trägt im Alternativtext (<c>wp:docPr/@descr</c>)
        /// <c>{{bild.ersteller.logo}}</c>; den Bildinhalt tauscht danach <see cref="Bildtausch"/>
        /// gegen das neutrale Platzhalterbild, ein hellgraues Rechteck mit dem Wort „Logo“ im
        /// Pixelmaß des Logos. Beim Füllen setzt die Engine dort das Logo aus der Einstellung ein
        /// oder entfernt das Bild, wenn keines gesetzt ist; wer ein festes eigenes Logo will, ersetzt
        /// das Bild in seiner Kopie und entfernt den Alternativtext.
        /// </summary>
        internal static Logostelle LogoPlatzhalter(MainDocumentPart main)
        {
            var bilder = main.HeaderParts
                .SelectMany(h => h.Header.Descendants<Drawing>().Select(d => (Teil: h, Bild: d)))
                .ToList();
            if (bilder.Count != 1 || main.HeaderParts.Any(h => h.Header.Descendants<Picture>().Any()))
                throw new InvalidOperationException("Kopfzeile: " + bilder.Count
                    + " DrawingML-Bilder gefunden, erwartet genau eines (das Logo) und kein VML-Bild.");

            (HeaderPart teil, Drawing bild) = bilder[0];
            string kennung = bild.Descendants<A.Blip>().SingleOrDefault()?.Embed?.Value;
            if (kennung == null || !teil.TryGetPartById(kennung, out OpenXmlPart gefunden) || !(gefunden is ImagePart logo))
                throw new InvalidOperationException("Kopfzeile: Das Logo verweist auf keinen eingebetteten Bildteil (a:blip/@r:embed).");
            // Der Tausch löscht den Bildteil des Logos — kein anderer Teil darf ihn benutzen.
            if (main.OpenXmlPackage.GetAllParts().SelectMany(p => p.Parts).Count(p => p.OpenXmlPart == logo) != 1)
                throw new InvalidOperationException("Kopfzeile: Der Bildteil des Logos " + logo.Uri + " wird auch anderswo benutzt.");

            (int Breite, int Hoehe) massLogo = Pixelmass(Bytes(logo));
            (int Breite, int Hoehe) massPlatzhalter = Pixelmass(Platzhalterbild());
            if (massLogo != massPlatzhalter)
                throw new InvalidOperationException("Das Platzhalterbild misst " + massPlatzhalter.Breite + " × " + massPlatzhalter.Hoehe
                    + " Pixel, das Logo der Quelle " + massLogo.Breite + " × " + massLogo.Hoehe
                    + " — Logoplatzhalter.png im Pixelmaß des Logos neu zeichnen (LIESMICH).");

            bild.Descendants<DW.DocProperties>().Single().Description = LOGO_PLATZHALTER;
            teil.Header.Save();

            DW.Extent ausdehnung = bild.Descendants<DW.Extent>().Single();
            return new Logostelle
            {
                Kopfteil = teil.Uri,
                Kennung = kennung,
                Logoteil = logo.Uri,
                Ausdehnung = ausdehnung.Cx?.Value + " × " + ausdehnung.Cy?.Value + " EMU",
                Pixel = massPlatzhalter,
            };
        }

        /// <summary>
        /// Tauscht den Bildteil des Logos gegen das Platzhalterbild, nachdem das SDK das Paket
        /// geschlossen hat — über <c>System.IO.Packaging</c>, denn das SDK legt einen neuen Bildteil
        /// unter Windows an der Paketwurzel an (<c>/media/image.png</c>) und vergibt ohne Vorgabe
        /// eine zufällige Beziehungskennung. So liegt das Platzhalterbild auf jedem System unter
        /// <c>/word/media/logoplatzhalter.png</c>, und die Beziehung behält die Kennung des Logos; in
        /// der Kopfzeile ändert sich nur der Alternativtext.
        /// </summary>
        internal static void Bildtausch(string pfad, Logostelle stelle, TextWriter aus)
        {
            byte[] platzhalter = Platzhalterbild();
            using (Paket.Package paket = Paket.Package.Open(pfad, FileMode.Open, FileAccess.ReadWrite))
            {
                Paket.PackagePart kopf = paket.GetPart(stelle.Kopfteil);
                kopf.DeleteRelationship(stelle.Kennung);
                paket.DeletePart(stelle.Logoteil);

                Paket.PackagePart bild = paket.CreatePart(TEIL_PLATZHALTER, "image/png", Paket.CompressionOption.Normal);
                using (Stream daten = bild.GetStream(FileMode.Create, FileAccess.Write))
                    daten.Write(platzhalter, 0, platzhalter.Length);
                kopf.CreateRelationship(Paket.PackUriHelper.GetRelativeUri(stelle.Kopfteil, TEIL_PLATZHALTER),
                                        Paket.TargetMode.Internal, BEZIEHUNG_BILD, stelle.Kennung);
            }
            aus.WriteLine("  Kopfzeile: Logo → Bildplatzhalter " + LOGO_PLATZHALTER + " (Alternativtext); Platzhalterbild "
                          + TEIL_PLATZHALTER + ", " + stelle.Pixel.Breite + " × " + stelle.Pixel.Hoehe + " Pixel, statt "
                          + stelle.Logoteil + "; Ort, Größe (" + stelle.Ausdehnung + ") und Umbruch bleiben");
        }

        /// <summary>Das neutrale Platzhalterbild aus der eingebetteten Ressource <c>Logoplatzhalter.png</c>.</summary>
        internal static byte[] Platzhalterbild()
        {
            using Stream quelle = typeof(Beispielvorlage).Assembly.GetManifestResourceStream(RESSOURCE_LOGO)
                ?? throw new InvalidOperationException("Die Ressource " + RESSOURCE_LOGO + " fehlt im Werkzeug.");
            var puffer = new MemoryStream();
            quelle.CopyTo(puffer);
            return puffer.ToArray();
        }

        internal static byte[] Bytes(OpenXmlPart teil)
        {
            using Stream quelle = teil.GetStream(FileMode.Open, FileAccess.Read);
            var puffer = new MemoryStream();
            quelle.CopyTo(puffer);
            return puffer.ToArray();
        }

        /// <summary>Breite und Höhe eines PNG (<c>IHDR</c>) oder JPEG (<c>SOFn</c>) in Pixeln; (0, 0) bei unbekanntem Format.</summary>
        internal static (int Breite, int Hoehe) Pixelmass(byte[] b)
        {
            if (b.Length >= 24 && b[0] == 0x89 && b[1] == (byte)'P' && b[2] == (byte)'N' && b[3] == (byte)'G')
                return (LiesZahl(b, 16, 4), LiesZahl(b, 20, 4));
            if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8)
            {
                int i = 2;
                while (i + 8 < b.Length && b[i] == 0xFF)
                {
                    byte marke = b[i + 1];
                    if (marke >= 0xC0 && marke <= 0xCF && marke != 0xC4 && marke != 0xC8 && marke != 0xCC)
                        return (LiesZahl(b, i + 7, 2), LiesZahl(b, i + 5, 2));
                    i += 2 + LiesZahl(b, i + 2, 2);
                }
            }
            return (0, 0);
        }

        /// <summary>Eine vorzeichenlose Zahl in Netzreihenfolge (höchstes Byte zuerst).</summary>
        private static int LiesZahl(byte[] b, int stelle, int laenge)
        {
            int wert = 0;
            for (int i = 0; i < laenge; i++) wert = (wert << 8) | b[stelle + i];
            return wert;
        }

        /// <summary>
        /// Gibt jedem Eintrag des Pakets den Zeitstempel der Quelle. Einen neu angelegten Teil — das
        /// Platzhalterbild — stempelt die Paketbibliothek mit der Laufzeit; zwei Läufe schrieben sonst
        /// verschiedene Bytes. Die übrigen Einträge tragen den Stempel der Quelle ohnehin; ihre
        /// gepackten Daten übernimmt die Zip-Bibliothek beim Neuschreiben unverändert.
        /// </summary>
        internal static void Zeitstempel(string pfad, string quelle)
        {
            DateTimeOffset stempel;
            using (ZipArchive q = ZipFile.OpenRead(quelle))
                stempel = (q.GetEntry("word/document.xml") ?? q.Entries.First()).LastWriteTime;
            using (ZipArchive z = ZipFile.Open(pfad, ZipArchiveMode.Update))
                foreach (ZipArchiveEntry eintrag in z.Entries)
                    eintrag.LastWriteTime = stempel;
        }

        /// <summary>
        /// Fußzeile: „INEKON GmbH“ wird <c>{{ersteller.firma}}</c>, „Seite“ wird
        /// <c>{{text.seite}}</c>, das DATE-Feld wird an seiner Stelle <c>{{bericht.datum}}</c>
        /// (6.7: DATE zeigt das Datum des Öffnens, nicht das des Berichts); PAGE und NUMPAGES
        /// bleiben Felder. Die Tabstopps bleiben: links Firma, Mitte Datum, rechts Seite.
        /// </summary>
        internal static void Fusszeile(MainDocumentPart main, TextWriter aus)
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
        internal static RunProperties ErsetzeText(OpenXmlElement wurzel, string alt, string schluessel)
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

        /// <summary>
        /// Die Erläuterungen der Beispielvorlage als Word-Kommentare (ohne Datum — die Datei bleibt bei
        /// jedem Lauf gleich): am Deckblatt die Platzhalter überhaupt, am Inhaltsverzeichnis die
        /// Kapitelplatzhalter, am ersten Kapitelkopf der Kapitelkopf als Platzhalter des Kapiteltitels.
        /// </summary>
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
                    "Zur Kopfzeile, weil Word dort keine Kommentare erlaubt: Das Bild rechts ist ein Bildplatzhalter. "
                    + "Sein Alternativtext {{bild.ersteller.logo}} lässt EPOS-Plan an seiner Stelle das Firmenlogo aus den "
                    + "Einstellungen einsetzen; ist keines hinterlegt, entfällt das Bild. Ein festes eigenes Logo ist ebenso "
                    + "möglich: in der eigenen Kopie das Bild durch das Logo ersetzen und den Alternativtext entfernen – sonst "
                    + "setzt EPOS-Plan dort weiter das Logo aus den Einstellungen ein.",
                    "Diese Datei ist aus dem bisherigen Bericht abgeleitet; die Standardvorlage des Word-Berichts hat denselben "
                    + "Aufbau, nur ohne Kommentare. Kommentare erscheinen nicht im fertigen Bericht."),
                Kommentar(KOMMENTAR_KAPITEL,
                    "Ein Kapitelplatzhalter wie {{kapitel.inhalt}} steht allein in seinem Absatz. Beim Erstellen ersetzt "
                    + "EPOS-Plan den ganzen Absatz durch einen ganzen Abschnitt des Berichts – mit Überschriften, Texten, "
                    + "Tabellen und Diagrammen. {{kapitel.inhalt}} erzeugt das Inhaltsverzeichnis samt seiner Überschrift.",
                    "Die übrigen Kapitel stehen in der Reihenfolge des bisherigen Berichts, jedes unter seinem Kapitelkopf. "
                    + "Die Angabe |ohne titel unterdrückt die eigene Überschrift des Kapitels – die trägt der Kapitelkopf."),
                Kommentar(KOMMENTAR_KAPITELKOPF,
                    "Der Kapitelkopf ist die Überschrift über einem Kapitel, im Absatzformat „EPOS Kapitelkopf“. "
                    + "{{text.kapitel_projekt}} ist ein Platzhalter für den Kapiteltitel: EPOS-Plan setzt ihn in der Sprache des "
                    + "Berichts ein, wie die Beschriftungen des Deckblatts. Der Platzhalter darf durch eigenen Text ersetzt "
                    + "werden; der steht dann in jeder Sprache so im Bericht.",
                    "Entfällt ein Kapitel – abgewählt oder ohne Daten –, entfällt der Kapitelkopf darüber mit, auch mit "
                    + "eigenem Text. Kapitelkopf und Kapitelplatzhalter gehören zusammen: Wer die Reihenfolge ändert, "
                    + "verschiebt beide."));
            teil.Comments.Save();
        }

        internal static Comment Kommentar(string id, params string[] absaetze)
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

        /// <summary>
        /// Schreibt Katalogfassung und Art in <c>docProps/custom.xml</c> (Konzept 5.6):
        /// <c>EPOS.Katalogfassung</c> als ganze Zahl (<c>vt:i4</c>), <c>EPOS.Vorlage</c> als Text
        /// (<c>vt:lpwstr</c>). Vorhandene Eigenschaften bleiben; eine gleichnamige bekommt den neuen
        /// Wert und behält ihre Nummer, eine neue die nächste freie (ab 2, wie Word zählt). Die
        /// Stilvorlage führt den Teil schon — so behält er beim Schreiben ihren Zeitstempel.
        /// </summary>
        private static void Dokumenteigenschaften(WordprocessingDocument doc, int katalogfassung, string kennung, TextWriter aus)
        {
            CustomFilePropertiesPart teil = doc.CustomFilePropertiesPart ?? doc.AddCustomFilePropertiesPart();
            Eigenschaftsliste liste = teil.Properties;
            if (liste == null) teil.Properties = liste = new Eigenschaftsliste();

            Setze(liste, EIGENSCHAFT_KATALOGFASSUNG, new VTInt32(katalogfassung.ToString(CultureInfo.InvariantCulture)));
            Setze(liste, EIGENSCHAFT_VORLAGE, new VTLPWSTR(kennung));
            liste.Save();
            aus.WriteLine("  custom.xml: " + string.Join(", ", liste.Elements<Eigenschaft>().Select(e => e.Name?.Value + " = " + e.InnerText)));
        }

        internal static void Setze(Eigenschaftsliste liste, string name, OpenXmlElement wert)
        {
            Eigenschaft e = liste.Elements<Eigenschaft>()
                .FirstOrDefault(p => string.Equals(p.Name?.Value, name, StringComparison.OrdinalIgnoreCase));
            if (e == null)
            {
                int naechste = liste.Elements<Eigenschaft>().Select(p => p.PropertyId?.Value ?? 1).DefaultIfEmpty(1).Max() + 1;
                e = new Eigenschaft { FormatId = FORMAT_EIGENE, PropertyId = Math.Max(2, naechste), Name = name };
                liste.Append(e);
            }
            e.RemoveAllChildren();
            e.Append(wert);
        }

        // ------------------------------------------------------------- Bausteine der Absätze

        internal static Paragraph Absatz(string stil, params OpenXmlElement[] inhalt)
        {
            var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = stil }));
            foreach (OpenXmlElement e in inhalt) p.Append(e);
            return p;
        }

        /// <summary><paramref name="inhalt"/> im Kommentarbereich <paramref name="id"/>, der Kommentarverweis dahinter.</summary>
        internal static OpenXmlElement[] Kommentiert(string id, OpenXmlElement inhalt)
            => new OpenXmlElement[]
            {
                new CommentRangeStart { Id = id }, inhalt, new CommentRangeEnd { Id = id },
                new Run(new CommentReference { Id = id }),
            };

        internal static Run Lauf(string text)
            => new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        internal static Run NeuerLauf(RunProperties rp)
        {
            var r = new Run();
            if (rp != null) r.Append(rp.CloneNode(true));
            return r;
        }

        /// <summary>Ein Platzhalter in eigenem Run, mit <c>w:noProof</c> (Zeichenformat nach <paramref name="muster"/>).</summary>
        internal static Run Platzhalter(string schluessel, RunProperties muster = null)
        {
            RunProperties rp = muster != null ? (RunProperties)muster.CloneNode(true) : new RunProperties();
            rp.NoProof = new NoProof();
            return new Run(rp, new Text("{{" + schluessel + "}}") { Space = SpaceProcessingModeValues.Preserve });
        }

        internal static string Zahl(int wert) => wert.ToString(CultureInfo.InvariantCulture);

        // ------------------------------------------------------------- Abschlussprüfung

        /// <summary>
        /// Stilregeln, Validator und die Regeln der Wache
        /// (<c>EPOS.Kern.Tests/BerichtsvorlageDateiWacheTests</c>): jeder Platzhalter in eigenem Run
        /// mit <c>w:noProof</c>, kein „INEKON“ mehr, in der Kopfzeile allein der Bildplatzhalter des
        /// Logos, jeder Kapitelplatzhalter mit <c>|ohne titel</c> unter dem Kapitelkopf seines
        /// Kapitels, die Kommentare der Art und beide Eigenschaften in <c>custom.xml</c>. Gibt die
        /// Platzhalter je Teil aus.
        /// </summary>
        private static bool Abschlusspruefung(string pfad, Vorlagenart art, int katalogfassung, TextWriter aus)
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

                if (art != Vorlagenart.StandardSammelanker) befunde.AddRange(Kapitelbefunde(main.Document.Body));
                befunde.AddRange(Bildbefunde(main));
                befunde.AddRange(Kommentarbefunde(main, art == Vorlagenart.Beispiel ? 3 : 0));
                befunde.AddRange(Eigenschaftsbefunde(doc, katalogfassung, Kennung(art)));
            }
            foreach (string b in befunde) aus.WriteLine("    Befund: " + b);
            int fehler = Pruefung.Validieren(pfad, aus);
            aus.WriteLine("  Regeln: " + (befunde.Count == 0 ? "grün" : befunde.Count + " Befunde") + "; Validator: " + fehler + " Fehler");
            return befunde.Count == 0 && fehler == 0;
        }

        /// <summary>
        /// Jeder Kapitelplatzhalter mit <c>|ohne titel</c> steht unmittelbar unter seinem Kapitelkopf —
        /// einem Absatz im Format „EPOS Kapitelkopf“ mit genau <c>{{text.kapitel_&lt;name&gt;}}</c>
        /// desselben Kapitels —, und jeder Kapitelkopf steht über einem solchen Platzhalter.
        /// </summary>
        private static IEnumerable<string> Kapitelbefunde(Body body)
        {
            List<Paragraph> absaetze = body.Elements<Paragraph>().ToList();
            for (int i = 0; i < absaetze.Count; i++)
            {
                string text = Absatztext(absaetze[i]);
                if (Stilkennung(absaetze[i]) == Pruefung.KAPITELKOPF_ID)
                {
                    Match unten = i + 1 < absaetze.Count ? Kapitelmuster.Match(Absatztext(absaetze[i + 1])) : Match.Empty;
                    if (!unten.Success)
                        yield return "Rumpf: Kapitelkopf „" + text + "“ steht über keinem Kapitelplatzhalter mit |ohne titel.";
                    else if (text != "{{text.kapitel_" + unten.Groups[1].Value + "}}")
                        yield return "Rumpf: Kapitelkopf „" + text + "“ passt nicht zu „" + unten.Value
                                     + "“, erwartet {{text.kapitel_" + unten.Groups[1].Value + "}}.";
                }
                else if (text.Contains("|ohne titel}}", StringComparison.Ordinal)
                         && (i == 0 || Stilkennung(absaetze[i - 1]) != Pruefung.KAPITELKOPF_ID))
                {
                    yield return "Rumpf: „" + text + "“ steht nicht unter einem Kapitelkopf.";
                }
            }
        }

        /// <summary>
        /// Die Kopfzeile trägt genau ein Bild, den Bildplatzhalter des Logos (Entscheid BV-E2-1): im
        /// Alternativtext <c>{{bild.ersteller.logo}}</c>, als Bildteil das Platzhalterbild (PNG, Bytes
        /// der Ressource) — kein VML-Bild, kein weiterer Bildteil, also auch nicht das Logo.
        /// </summary>
        internal static IEnumerable<string> Bildbefunde(MainDocumentPart main)
        {
            byte[] platzhalter = Platzhalterbild();
            var bilder = main.HeaderParts
                .SelectMany(h => h.Header.Descendants<Drawing>().Select(d => (Teil: h, Bild: d)))
                .ToList();
            if (bilder.Count != 1)
                yield return "Kopfzeile: " + bilder.Count + " Bilder, erwartet genau eines — den Bildplatzhalter des Logos.";
            if (main.HeaderParts.Any(h => h.Header.Descendants<Picture>().Any()))
                yield return "Kopfzeile: ein VML-Bild (w:pict) — nicht vorgesehen.";
            foreach ((HeaderPart teil, Drawing bild) in bilder)
            {
                string alternativtext = bild.Descendants<DW.DocProperties>().FirstOrDefault()?.Description?.Value;
                if (alternativtext != LOGO_PLATZHALTER)
                    yield return "Kopfzeile: Alternativtext „" + alternativtext + "“, erwartet " + LOGO_PLATZHALTER + ".";
                string kennung = bild.Descendants<A.Blip>().FirstOrDefault()?.Embed?.Value;
                if (kennung == null || !teil.TryGetPartById(kennung, out OpenXmlPart gefunden) || !(gefunden is ImagePart bildteil))
                {
                    yield return "Kopfzeile: Das Bild verweist auf keinen eingebetteten Bildteil.";
                    continue;
                }
                if (bildteil.ContentType != "image/png" || !Bytes(bildteil).SequenceEqual(platzhalter))
                    yield return "Kopfzeile: Der Bildteil " + bildteil.Uri + " ist nicht das Platzhalterbild.";
            }
            int bildteile = main.HeaderParts.Sum(h => h.ImageParts.Count());
            if (bildteile != 1)
                yield return "Kopfzeile: " + bildteile + " Bildteile, erwartet genau einen — das Platzhalterbild.";
        }

        /// <summary>
        /// Die Kommentare: in der Beispielvorlage drei — am Deckblatt, am Inhaltsverzeichnis und am
        /// ersten Kapitelkopf —, jeder mit genau einem Verweis im Rumpf; in den Standardvorlagen keiner.
        /// </summary>
        private static IEnumerable<string> Kommentarbefunde(MainDocumentPart main, int erwartet)
        {
            List<string> kommentare = main.WordprocessingCommentsPart?.Comments?.Elements<Comment>()
                .Select(c => c.Id?.Value).OrderBy(x => x, StringComparer.Ordinal).ToList() ?? new List<string>();
            List<string> verweise = main.Document.Body.Descendants<CommentReference>()
                .Select(r => r.Id?.Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (kommentare.Count != erwartet)
                yield return "Kommentare: " + kommentare.Count + ", erwartet " + erwartet + ".";
            if (!kommentare.SequenceEqual(verweise))
                yield return "Kommentare (" + string.Join(", ", kommentare) + ") und Verweise im Rumpf ("
                             + string.Join(", ", verweise) + ") passen nicht zusammen.";
            if (erwartet > 0)
            {
                Paragraph ersterKopf = main.Document.Body.Elements<Paragraph>()
                    .FirstOrDefault(p => Stilkennung(p) == Pruefung.KAPITELKOPF_ID);
                if (ersterKopf == null || !ersterKopf.Descendants<CommentReference>().Any(r => r.Id?.Value == KOMMENTAR_KAPITELKOPF))
                    yield return "Kommentare: Der erste Kapitelkopf trägt keinen Kommentar.";
            }
        }

        /// <summary>Beide Eigenschaften stehen genau einmal in <c>custom.xml</c>, mit dem Wert dieses Laufs.</summary>
        private static IEnumerable<string> Eigenschaftsbefunde(WordprocessingDocument doc, int katalogfassung, string kennung)
        {
            List<Eigenschaft> liste = doc.CustomFilePropertiesPart?.Properties?.Elements<Eigenschaft>().ToList()
                ?? new List<Eigenschaft>();
            var soll = new[]
            {
                (Name: EIGENSCHAFT_KATALOGFASSUNG, Wert: katalogfassung.ToString(CultureInfo.InvariantCulture)),
                (Name: EIGENSCHAFT_VORLAGE, Wert: kennung),
            };
            foreach ((string name, string wert) in soll)
            {
                List<string> ist = liste.Where(e => string.Equals(e.Name?.Value, name, StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.InnerText).ToList();
                if (ist.Count != 1 || ist[0] != wert)
                    yield return "custom.xml: " + name + " = „" + string.Join("“, „", ist) + "“, erwartet genau einmal „" + wert + "“.";
            }
        }

        internal static string Absatztext(Paragraph p)
            => string.Concat(p.Descendants<Text>().Select(t => t.Text));

        internal static string Stilkennung(Paragraph p)
            => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
    }
}
