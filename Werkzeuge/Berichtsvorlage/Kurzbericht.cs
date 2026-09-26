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
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Eigenschaft = DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty;
using Eigenschaftsliste = DocumentFormat.OpenXml.CustomProperties.Properties;
using Paket = System.IO.Packaging;

namespace Berichtsvorlage
{
    /// <summary>
    /// <b>Der Kurzbericht je Sprache</b> (Konzept Berichtsvorlagen 4.9, 6.3 Nr. 2, 6.5, Anhang B.1; Etappe BV-E5) — eine
    /// Lehrvorlage aus der bereinigten Stilvorlage, die zeigt, wie ein Bericht <b>ohne ganze Kapitel</b> entsteht: aus
    /// Einzelwerten, Blöcken (<c>{{#je stand}}</c>, <c>{{#wenn …}}</c>), einer Musterzeile, einer Strukturtabelle und
    /// Bildplatzhaltern — zwei davon nebeneinander in halber Satzspiegelbreite (Bildgröße Stufe 2). Einzig der Anhang
    /// kommt als Kapitel (<c>{{kapitel.anhang|ohne titel|ebene 2}}</c>) unter einem Kapitelkopf, dazu am Ende die
    /// Mustertabelle <c>{{muster.tabelle}}</c>, aus der die Engine die Rollen der Strukturtabellen liest.
    ///
    /// <para><b>Je Sprache eine Datei</b> (4.9: „Der Kurzbericht kommt je Sprache“): Überschriften, Beschriftungen
    /// und Sätze sind fester Text in der Sprache der Datei, die Erläuterungen Word-Kommentare derselben Sprache.
    /// <c>custom.xml</c> trägt <c>EPOS.Sprache</c> — die Vorprüfung fragt zurück, wenn die Oberfläche eine andere
    /// Sprache spricht —, dazu <c>EPOS.Katalogfassung</c> und <c>EPOS.Vorlage</c> <c>kurzbericht</c>.</para>
    ///
    /// <para><b>Neutral</b> (Konzept 12, Zeile „Produktdaten“): Kein Hersteller, kein Produkt, keine Kennwerte eines
    /// Geräts; Beispiele in den Kommentaren tragen neutrale Namen mit runden Werten („Variante 1“, „Speicher 1,
    /// 100 kWh“). Die Wache <c>WikiProduktdatenWacheTests</c> hält den Text gegen die Katalognamen der
    /// Testdatenbank.</para>
    ///
    /// <para>Kopf- und Fußzeile, Logo, Zeitstempel und Prüfungen wie <see cref="Beispielvorlage"/>: Die Kopfzeile
    /// trägt <c>{{projekt.name}} · {{bericht.titel}}</c> und den Bildplatzhalter des Logos, die Fußzeile Firma, Datum
    /// und Seite. <b>Byte-gleich wiederholbar</b> auf einem System: Das Platzhalterbild der Bildrahmen entsteht aus
    /// festen Bytes (<see cref="Bildplatzhalter"/>, PNG mit ungepackten Deflate-Blöcken), Teile und Beziehungen tragen
    /// feste Namen, alle Einträge den Zeitstempel der Quelle.</para>
    /// </summary>
    internal static class Kurzbericht
    {
        /// <summary>Die beiden Dateien im Vorlagenordner — ihr Name legt die Sprache fest.</summary>
        internal const string DATEI_DEUTSCH = "Berichtsvorlage_Kurzbericht.docx";
        internal const string DATEI_ENGLISCH = "Berichtsvorlage_Kurzbericht_en.docx";

        /// <summary>Der Wert von <c>EPOS.Vorlage</c>.</summary>
        internal const string KENNUNG = "kurzbericht";

        /// <summary>Die Sprache der Vorlage in <c>custom.xml</c>; derselbe Name wie <c>Vorlagenpruefer.EIGENSCHAFT_SPRACHE</c>.</summary>
        internal const string EIGENSCHAFT_SPRACHE = "EPOS.Sprache";

        /// <summary>Der Teil des Platzhalterbilds der Bildrahmen und seine Beziehung vom Rumpf — fest.</summary>
        private static readonly Uri TEIL_BILD = new Uri("/word/media/bildplatzhalter.png", UriKind.Relative);
        private static readonly Uri TEIL_RUMPF = new Uri("/word/document.xml", UriKind.Relative);
        internal const string KENNUNG_BILD = "rIdBildplatzhalter";

        /// <summary>Satzspiegel der Stilvorlage in EMU (9355 twips × 635).</summary>
        private const long SATZSPIEGEL_EMU = Beispielvorlage.INHALTSBREITE * 635L;

        /// <summary>Ein Bild in voller Breite: das Spannenbild, Höhe halb so groß — die Engine passt das Modell ein.</summary>
        private const long BILD_VOLL_BREITE = SATZSPIEGEL_EMU;
        private const long BILD_VOLL_HOEHE = SATZSPIEGEL_EMU / 2;

        /// <summary>
        /// Zwei Bilder nebeneinander: je 7,8 cm breit (die halbe Tabelle abzüglich der Zellränder), im Seitenverhältnis
        /// des Deckungskuchens (420 × 262). Über der Mindestbreite des Kuchens — die Engine zeichnet in Zielgröße (Stufe 2).
        /// </summary>
        private const long BILD_HALB_BREITE = 2808000L;
        private const long BILD_HALB_HOEHE = 1752000L;

        /// <summary>Die Kennungen <c>wp:docPr/@id</c> der Bildrahmen — über denen der Kopfzeile, fest.</summary>
        private const uint ERSTE_BILDKENNUNG = 101;

        /// <summary>Der Rumpf nennt die Platzhalter in dieser Form (wie <see cref="Beispielvorlage.Platzhaltermuster"/>, dazu Blockmarken).</summary>
        internal static readonly Regex Markenmuster =
            new Regex(@"\{\{[#/]?[a-z0-9_. ]+(\|[a-z0-9 ]+)*\}\}", RegexOptions.CultureInvariant);

        /// <summary>Die festen Texte je Sprache.</summary>
        private sealed class Texte
        {
            internal string Sprache, Untertitel, Kunde, Bearbeitung, Ersteller, Datum, Verglichen, ErstelltMit;
            internal string Ausgangslage, Klimaregion, Ergebnisse, Variante, Waermebedarf, Jaz, Co2, Kapitalwertdiff;
            internal string Empfehlung, BesteVor, BesteMitte, BesteNach, Wirtschaft, Kuehlung, KuehlungVor, KuehlungMitte, KuehlungNach;
            internal string Deckung, Anhang, Stamm, Gruppe, Summe, Warnung;
            internal string Titel, Beschreibung;
            internal string[][] Kommentare;
        }

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

            Texte t = englisch ? Englisch() : Deutsch();
            aus.WriteLine("Kurzbericht (" + t.Sprache + "): " + quelle + " → " + ziel);

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
                Beispielvorlage.Logostelle logo;
                using (WordprocessingDocument doc = WordprocessingDocument.Open(kopie, true))
                    logo = Baue(doc, t, katalogfassung, aus);
                Beispielvorlage.Bildtausch(kopie, logo, aus);
                Bildteil(kopie, aus);
                Beispielvorlage.Zeitstempel(kopie, quelle);

                if (!Abschlusspruefung(kopie, t, katalogfassung, aus))
                {
                    Console.Error.WriteLine("Der Kurzbericht ist nicht in Ordnung — das Ziel bleibt unverändert.");
                    return Program.PRUEFUNG;
                }
                File.Copy(kopie, ziel, true);
                aus.WriteLine("  Geschrieben: " + ziel);
                return Program.OK;
            }
            finally { Program.Loeschen(kopie); }
        }

        /// <summary>Die beiden Dateien des Vorlagenordners entstehen nur in ihrer Sprache; jedes andere Ziel nimmt jede.</summary>
        private static string ZielUndSprache(string ziel, bool englisch)
        {
            string name = Path.GetFileName(ziel);
            if (string.Equals(name, DATEI_DEUTSCH, StringComparison.OrdinalIgnoreCase) && englisch)
                return DATEI_DEUTSCH + " ist der deutsche Kurzbericht — mit --sprache de bauen.";
            if (string.Equals(name, DATEI_ENGLISCH, StringComparison.OrdinalIgnoreCase) && !englisch)
                return DATEI_ENGLISCH + " ist der englische Kurzbericht — mit --sprache en bauen.";
            if (string.Equals(name, Beispielvorlage.DATEI_STANDARD, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Beispielvorlage.DATEI_BEISPIEL, StringComparison.OrdinalIgnoreCase))
                return name + " entsteht mit „beispiel“, nicht mit „kurzbericht“.";
            return null;
        }

        // ------------------------------------------------------------- Aufbau

        private static Beispielvorlage.Logostelle Baue(WordprocessingDocument doc, Texte t, int katalogfassung, TextWriter aus)
        {
            MainDocumentPart main = doc.MainDocumentPart;
            Body body = main.Document.Body;
            SectionProperties hauptabschnitt = body.Elements<SectionProperties>().LastOrDefault()
                ?? throw new InvalidOperationException("Die Vorlage hat keine Abschnittseigenschaften (w:sectPr) am Rumpfende.");
            foreach (OpenXmlElement el in body.ChildElements.Where(c => !(c is SectionProperties)).ToList())
                el.Remove();

            uint bildkennung = ERSTE_BILDKENNUNG;
            foreach (OpenXmlElement el in Rumpf(hauptabschnitt, t, ref bildkennung))
                body.InsertBefore(el, hauptabschnitt);
            main.Document.Save();

            Kopfzeile(main, aus);
            Beispielvorlage.Logostelle logo = Beispielvorlage.LogoPlatzhalter(main);
            Beispielvorlage.Fusszeile(main, aus);
            Kommentare(main, t);
            Dokumenteigenschaften(doc, katalogfassung, t.Sprache, aus);

            doc.PackageProperties.LastModifiedBy = Beispielvorlage.AUTOR;
            doc.PackageProperties.Title = t.Titel;
            doc.PackageProperties.Description = t.Beschreibung;
            return logo;
        }

        /// <summary>Der Rumpf nach Anhang B.1 — in Dokumentfolge; die Kommentare setzt <see cref="Kommentare"/> nach ihren Kennungen.</summary>
        private static IEnumerable<OpenXmlElement> Rumpf(SectionProperties hauptabschnitt, Texte t, ref uint bildkennung)
        {
            var r = new List<OpenXmlElement>();

            // 1 — Deckblatt (Abschnitt 1 ohne Kopf- und Fußzeile)
            r.Add(Beispielvorlage.Absatz("Title", new CommentRangeStart { Id = "0" }, P("bericht.titel")));
            r.Add(Beispielvorlage.Absatz("Subtitle", Beispielvorlage.Lauf(t.Untertitel)));
            r.Add(Beschriftungstabelle(new[]
            {
                (t.Kunde, "projekt.kunde"), (t.Bearbeitung, "projekt.bearbeiter"), (t.Ersteller, "ersteller.firma"), (t.Datum, "bericht.datum"),
            }));
            r.Add(Beispielvorlage.Absatz("Hinweis", Beispielvorlage.Lauf(t.Verglichen + " "), P("bericht.varianten.liste")));
            Paragraph erstelltMit = Beispielvorlage.Absatz("Hinweis", Beispielvorlage.Lauf(t.ErstelltMit + " "), P("ersteller.programm"),
                                                          Beispielvorlage.Lauf(" "), P("ersteller.version"));
            erstelltMit.Append(Ende("0"));
            var deckblattAbschnitt = (SectionProperties)hauptabschnitt.CloneNode(true);
            deckblattAbschnitt.RemoveAllChildren<HeaderReference>();
            deckblattAbschnitt.RemoveAllChildren<FooterReference>();
            erstelltMit.ParagraphProperties.AddChild(deckblattAbschnitt);
            r.Add(erstelltMit);

            // 3 — Ausgangslage: Einzelwerte im Fließtext
            r.Add(Kommentiert("Heading1", t.Ausgangslage, "1"));
            r.Add(Beispielvorlage.Absatz("Normal", P("projekt.beschreibung")));
            r.Add(Beispielvorlage.Absatz("Normal", Beispielvorlage.Lauf(t.Klimaregion + ": "), P("projekt.klimaregion")));

            // 4 — Ergebnisse im Überblick: Musterzeile je Stand, darunter die Warnungen
            r.Add(Kommentiert("Heading1", t.Ergebnisse, "2"));
            r.Add(Ergebnistabelle(t));
            r.Add(Beispielvorlage.Absatz("Normal", P("bericht.warnungen")));

            // 5 — Empfehlung: Werte der besten Variante im Satz
            r.Add(Kommentiert("Heading1", t.Empfehlung, "3"));
            r.Add(Beispielvorlage.Absatz("Normal", Beispielvorlage.Lauf(t.BesteVor + " "), P("wirtschaft.beste.anzeige"),
                                         Beispielvorlage.Lauf(" " + t.BesteMitte + " "), P("wirtschaft.beste.kapitalwert_diff"),
                                         Beispielvorlage.Lauf(t.BesteNach)));
            r.Add(Beispielvorlage.Absatz("Normal", P("wirtschaft.vorschlag")));

            // 6 — Wirtschaftlichkeit: Bild in voller Breite (nur mit Modell), Strukturtabelle, Warnungen
            r.Add(Kommentiert("Heading1", t.Wirtschaft, "4"));
            r.Add(Beispielvorlage.Absatz("Normal", P("#wenn hat.bild.wirtschaft.spanne")));
            r.Add(Beispielvorlage.Absatz("Normal", Bildrahmen("bild.wirtschaft.spanne", BILD_VOLL_BREITE, BILD_VOLL_HOEHE, bildkennung++)));
            r.Add(Beispielvorlage.Absatz("Normal", P("/wenn")));
            r.Add(Beispielvorlage.Absatz("Normal", P("tabelle.wirtschaft.szenarien")));
            r.Add(Beispielvorlage.Absatz("Normal", P("wirtschaft.warnungen")));

            // 7 — Bedingter Abschnitt: nur mit Kälte
            Paragraph wennKaelte = Beispielvorlage.Absatz("Normal", new CommentRangeStart { Id = "5" }, P("#wenn hat.kaelte"));
            wennKaelte.Append(Ende("5"));
            r.Add(wennKaelte);
            r.Add(Beispielvorlage.Absatz("Heading2", Beispielvorlage.Lauf(t.Kuehlung)));
            r.Add(Beispielvorlage.Absatz("Normal", Beispielvorlage.Lauf(t.KuehlungVor + " "), P("stamm.kennzahl.kaelte.jahresbedarf"),
                                         Beispielvorlage.Lauf(t.KuehlungMitte + " "), P("stamm.kennzahl.kaelte.deckungsgrad"),
                                         Beispielvorlage.Lauf(t.KuehlungNach)));
            r.Add(Beispielvorlage.Absatz("Normal", P("/wenn")));

            // 8 — Zwei Bilder nebeneinander je Stand (Bildgröße Stufe 2)
            r.Add(Kommentiert("Heading1", t.Deckung, "6"));
            r.Add(Beispielvorlage.Absatz("Normal", P("#je stand")));
            r.Add(Beispielvorlage.Absatz("Heading2", P("stand.anzeige")));
            r.Add(Bildertabelle(ref bildkennung));
            r.Add(Beispielvorlage.Absatz("Normal", P("/je")));

            // 9 — Anhang als Kapitel unter einem Kapitelkopf
            r.Add(Kommentiert(Pruefung.KAPITELKOPF_ID, t.Anhang, "7"));
            r.Add(Beispielvorlage.Absatz("Normal", P("kapitel.anhang|ohne titel|ebene 2")));

            // 10 — Mustertabelle am Ende; die Engine liest sie und entfernt sie
            r.Add(Mustertabelle(t));
            return r;
        }

        /// <summary>Ein Platzhalter oder eine Blockmarke in eigenem Run mit <c>w:noProof</c>.</summary>
        private static Run P(string inhalt) => Beispielvorlage.Platzhalter(inhalt);

        /// <summary>Ein Absatz mit festem Text, den Kommentar <paramref name="id"/> umspannend.</summary>
        private static Paragraph Kommentiert(string stil, string text, string id)
        {
            var p = Beispielvorlage.Absatz(stil, new CommentRangeStart { Id = id }, Beispielvorlage.Lauf(text));
            p.Append(Ende(id));
            return p;
        }

        private static OpenXmlElement[] Ende(string id)
            => new OpenXmlElement[] { new CommentRangeEnd { Id = id }, new Run(new CommentReference { Id = id }) };

        /// <summary>Beschriftung (fester Text) · Wert (Platzhalter), gestaltet wie die Eigenschaftstabelle der Standardvorlage.</summary>
        private static Table Beschriftungstabelle((string Beschriftung, string Schluessel)[] zeilen)
        {
            int beschriftung = 2800, wert = Beispielvorlage.INHALTSBREITE - beschriftung;
            Table tabelle = Rahmentabelle(new[] { beschriftung, wert });
            foreach ((string text, string schluessel) in zeilen)
                tabelle.Append(new TableRow(Zelle(beschriftung, Beispielvorlage.FUELLUNG_BESCHRIFTUNG, true, Beispielvorlage.Lauf(text)),
                                            Zelle(wert, null, false, P(schluessel))));
            return tabelle;
        }

        /// <summary>
        /// Die Ergebnistabelle: Kopfzeile mit festen Beschriftungen (die Einheiten als Platzhalter), darunter EINE
        /// Musterzeile — <c>{{#je stand}}</c> in der ersten, <c>{{/je}}</c> in der letzten Zelle; die Engine klont sie je Stand.
        /// </summary>
        private static Table Ergebnistabelle(Texte t)
        {
            int[] breiten = { 2555, 1700, 1100, 1700, 2300 };
            Table tabelle = Rahmentabelle(breiten);
            string f = Beispielvorlage.FUELLUNG_BESCHRIFTUNG;
            tabelle.Append(new TableRow(
                new TableRowProperties(new TableHeader()),
                Zelle(breiten[0], f, true, Beispielvorlage.Lauf(t.Variante)),
                Zelle(breiten[1], f, true, Beispielvorlage.Lauf(t.Waermebedarf + " ["), P("kennzahl.energie.waermebedarf.einheit"), Beispielvorlage.Lauf("]")),
                Zelle(breiten[2], f, true, Beispielvorlage.Lauf(t.Jaz)),
                Zelle(breiten[3], f, true, Beispielvorlage.Lauf(t.Co2 + " ["), P("kennzahl.em.co2.einheit"), Beispielvorlage.Lauf("]")),
                Zelle(breiten[4], f, true, Beispielvorlage.Lauf(t.Kapitalwertdiff))));
            tabelle.Append(new TableRow(
                Zelle(breiten[0], null, false, P("#je stand"), P("stand.anzeige")),
                Zelle(breiten[1], null, false, P("stand.kennzahl.energie.waermebedarf|ohne einheit")),
                Zelle(breiten[2], null, false, P("stand.kennzahl.eff.jaz|stellen 1")),
                Zelle(breiten[3], null, false, P("stand.kennzahl.em.co2|ohne einheit")),
                Zelle(breiten[4], null, false, P("stand.wirtschaft.kapitalwert_diff|mit grund"), P("/je"))));
            return tabelle;
        }

        /// <summary>Zwei Bildrahmen nebeneinander: eine zweispaltige Tabelle ohne Rahmen, je Zelle ein Bild in halber Breite.</summary>
        private static Table Bildertabelle(ref uint bildkennung)
        {
            int spalte = Beispielvorlage.INHALTSBREITE / 2;
            var tabelle = new Table(
                new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Dxa, Width = Beispielvorlage.Zahl(2 * spalte) },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.None }, new LeftBorder { Val = BorderValues.None },
                        new BottomBorder { Val = BorderValues.None }, new RightBorder { Val = BorderValues.None },
                        new InsideHorizontalBorder { Val = BorderValues.None }, new InsideVerticalBorder { Val = BorderValues.None }),
                    new TableLayout { Type = TableLayoutValues.Fixed}),
                new TableGrid(new GridColumn { Width = Beispielvorlage.Zahl(spalte) }, new GridColumn { Width = Beispielvorlage.Zahl(spalte) }));
            Run waerme = Bildrahmen("stand.bild.deckung_waerme", BILD_HALB_BREITE, BILD_HALB_HOEHE, bildkennung++);
            Run strom = Bildrahmen("stand.bild.deckung_strom", BILD_HALB_BREITE, BILD_HALB_HOEHE, bildkennung++);
            tabelle.Append(new TableRow(Zelle(spalte, null, false, waerme), Zelle(spalte, null, false, strom)));
            return tabelle;
        }

        /// <summary>
        /// Die Mustertabelle (Konzept 6.4 Nr. 2): Alternativtext <c>{{muster.tabelle}}</c>, je Rolle eine Zelle mit
        /// Schattierung und Zeichenformat — Stamm hellgrau, Gruppe fett auf Blaugrau, Summe fett mit Linie, Warnung
        /// rot. Die Engine liest die Rollen und entfernt die Tabelle.
        /// </summary>
        private static Table Mustertabelle(Texte t)
        {
            int spalte = Beispielvorlage.INHALTSBREITE / 4;
            Table tabelle = Rahmentabelle(new[] { spalte, spalte, spalte, spalte });
            tabelle.GetFirstChild<TableProperties>().Append(new TableDescription { Val = "{{muster.tabelle}}" });
            TableCell Rolle(string text, string fuellung, bool fett, string farbe, bool ersteZelle)
            {
                var rp = new RunProperties();
                if (fett) rp.Bold = new Bold();
                if (farbe != null) rp.Color = new Color { Val = farbe };
                rp.FontSize = new FontSize { Val = Beispielvorlage.SCHRIFT_TABELLE };
                var lauf = new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
                return ersteZelle
                    ? Zelle(spalte, fuellung, false, new CommentRangeStart { Id = "8" }, lauf, Ende("8")[0], Ende("8")[1])
                    : Zelle(spalte, fuellung, false, lauf);
            }
            tabelle.Append(new TableRow(
                Rolle(t.Stamm, "F2F2F2", false, null, true),
                Rolle(t.Gruppe, "DCE6F0", true, "1F4E79", false),
                Rolle(t.Summe, "E7E6E6", true, null, false),
                Rolle(t.Warnung, null, false, "C00000", false)));
            return tabelle;
        }

        /// <summary>Eine Tabelle mit festen Spalten und dem dünnen grauen Rahmen der Eigenschaftstabelle.</summary>
        private static Table Rahmentabelle(int[] breiten)
        {
            string r = Beispielvorlage.RAHMENFARBE;
            return new Table(
                new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Dxa, Width = Beispielvorlage.Zahl(breiten.Sum()) },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4U, Color = r },
                        new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = r },
                        new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = r },
                        new RightBorder { Val = BorderValues.Single, Size = 4U, Color = r },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = r },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = r }),
                    new TableLayout { Type = TableLayoutValues.Fixed }),
                new TableGrid(breiten.Select(b => new GridColumn { Width = Beispielvorlage.Zahl(b) })));
        }

        private static TableCell Zelle(int breite, string fuellung, bool fett, params OpenXmlElement[] inhalt)
        {
            var tcp = new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = Beispielvorlage.Zahl(breite) });
            if (fuellung != null) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fuellung });
            tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            var p = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "20", After = "20" }));
            foreach (OpenXmlElement e in inhalt)
            {
                if (e is Run lauf && fett)
                {
                    RunProperties rp = lauf.RunProperties ?? lauf.PrependChild(new RunProperties());
                    if (rp.Bold == null) rp.Bold = new Bold();
                }
                if (e is Run r2)
                {
                    RunProperties rp = r2.RunProperties ?? r2.PrependChild(new RunProperties());
                    if (rp.FontSize == null) rp.FontSize = new FontSize { Val = Beispielvorlage.SCHRIFT_TABELLE };
                }
                p.Append(e);
            }
            return new TableCell(tcp, p);
        }

        /// <summary>
        /// Ein Bildrahmen: ein eingebettetes Bild (<c>wp:inline</c>) im gegebenen Maß mit dem Platzhalterbild und dem
        /// Schlüssel im Alternativtext (<c>wp:docPr/@descr</c>, Konzept 4.2). Die Beziehung <see cref="KENNUNG_BILD"/> legt
        /// <see cref="Bildteil"/> nach dem Schließen an — alle Rahmen teilen einen Bildteil.
        /// </summary>
        private static Run Bildrahmen(string schluessel, long breite, long hoehe, uint kennung)
        {
            string name = "Bildplatzhalter " + kennung.ToString(CultureInfo.InvariantCulture);
            var bild = new Drawing(new DW.Inline(
                new DW.Extent { Cx = breite, Cy = hoehe },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = kennung, Name = name, Description = "{{" + schluessel + "}}" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = 0U, Name = "bildplatzhalter.png" },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(new A.Blip { Embed = KENNUNG_BILD }, new A.Stretch(new A.FillRectangle())),
                        new PIC.ShapeProperties(
                            new A.Transform2D(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = breite, Cy = hoehe }),
                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U,
            });
            return new Run(new RunProperties(new NoProof()), bild);
        }

        /// <summary>Kopfzeile: „EPOS-Plan“ wird <c>{{projekt.name}}</c>, der Werbetext dahinter <c>{{bericht.titel}}</c>; das Logo bleibt.</summary>
        private static void Kopfzeile(MainDocumentPart main, TextWriter aus)
        {
            int name = 0, titel = 0;
            foreach (HeaderPart teil in main.HeaderParts)
            {
                if (Beispielvorlage.ErsetzeText(teil.Header, "EPOS-Plan", "projekt.name") != null) name++;
                if (Beispielvorlage.ErsetzeText(teil.Header, "Energie · Planung · Optimierung · Simulation", "bericht.titel") != null) titel++;
                teil.Header.Save();
            }
            if (name != 1 || titel != 1)
                throw new InvalidOperationException("Kopfzeile: „EPOS-Plan“ " + name + "-mal, Werbetext " + titel + "-mal gefunden, erwartet je einmal.");
            aus.WriteLine("  Kopfzeile: {{projekt.name}} · {{bericht.titel}}");
        }

        /// <summary>
        /// Legt das Platzhalterbild der Bildrahmen an, nachdem das SDK das Paket geschlossen hat — wie
        /// <see cref="Beispielvorlage.Bildtausch"/> über <c>System.IO.Packaging</c>, damit Teil und Beziehung auf jedem
        /// System denselben Namen tragen.
        /// </summary>
        private static void Bildteil(string pfad, TextWriter aus)
        {
            byte[] png = Bildplatzhalter();
            using (Paket.Package paket = Paket.Package.Open(pfad, FileMode.Open, FileAccess.ReadWrite))
            {
                Paket.PackagePart rumpf = paket.GetPart(TEIL_RUMPF);
                Paket.PackagePart bild = paket.CreatePart(TEIL_BILD, "image/png", Paket.CompressionOption.Normal);
                using (Stream daten = bild.GetStream(FileMode.Create, FileAccess.Write))
                    daten.Write(png, 0, png.Length);
                rumpf.CreateRelationship(Paket.PackUriHelper.GetRelativeUri(TEIL_RUMPF, TEIL_BILD),
                                         Paket.TargetMode.Internal, Beispielvorlage.BEZIEHUNG_BILD, KENNUNG_BILD);
            }
            aus.WriteLine("  Bildrahmen: Platzhalterbild " + TEIL_BILD + " (" + png.Length + " Byte), Beziehung " + KENNUNG_BILD);
        }

        // ------------------------------------------------------------- Platzhalterbild

        /// <summary>Maß des Platzhalterbilds in Pixeln — im Seitenverhältnis 8 : 5, gestreckt auf jeden Rahmen.</summary>
        internal const int BILD_BREITE = 80, BILD_HOEHE = 50;

        /// <summary>
        /// Das neutrale Platzhalterbild der Bildrahmen: hellgrau <c>F2F2F2</c> mit einem Pixel Rahmen <c>BFBFBF</c>, ohne
        /// Schrift — ein PNG (8 Bit RGB) aus festen Bytes: Die Bilddaten stehen in ungepackten Deflate-Blöcken, so
        /// schreibt jeder Lauf auf jedem System dieselben Bytes. Die Engine ersetzt es beim Füllen durch das Diagramm.
        /// </summary>
        internal static byte[] Bildplatzhalter()
        {
            var roh = new MemoryStream();
            for (int y = 0; y < BILD_HOEHE; y++)
            {
                roh.WriteByte(0); // Filter „keiner“
                for (int x = 0; x < BILD_BREITE; x++)
                {
                    bool rand = x == 0 || y == 0 || x == BILD_BREITE - 1 || y == BILD_HOEHE - 1;
                    byte w = rand ? (byte)0xBF : (byte)0xF2;
                    roh.WriteByte(w); roh.WriteByte(w); roh.WriteByte(w);
                }
            }
            byte[] daten = roh.ToArray();

            // zlib: Kopf 78 01, ungepackte Blöcke je höchstens 65535 Byte, Adler-32.
            var zlib = new MemoryStream();
            zlib.WriteByte(0x78); zlib.WriteByte(0x01);
            for (int i = 0; i < daten.Length; i += 65535)
            {
                int n = Math.Min(65535, daten.Length - i);
                zlib.WriteByte((byte)(i + n >= daten.Length ? 1 : 0));
                zlib.WriteByte((byte)(n & 0xFF)); zlib.WriteByte((byte)(n >> 8));
                zlib.WriteByte((byte)(~n & 0xFF)); zlib.WriteByte((byte)((~n >> 8) & 0xFF));
                zlib.Write(daten, i, n);
            }
            uint a = 1, b = 0;
            foreach (byte d in daten) { a = (a + d) % 65521; b = (b + a) % 65521; }
            Zahl(zlib, (b << 16) | a);

            var png = new MemoryStream();
            png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
            var ihdr = new MemoryStream();
            Zahl(ihdr, BILD_BREITE); Zahl(ihdr, BILD_HOEHE);
            ihdr.Write(new byte[] { 8, 2, 0, 0, 0 }, 0, 5);
            Abschnitt(png, "IHDR", ihdr.ToArray());
            Abschnitt(png, "IDAT", zlib.ToArray());
            Abschnitt(png, "IEND", new byte[0]);
            return png.ToArray();
        }

        private static void Abschnitt(Stream s, string art, byte[] daten)
        {
            Zahl(s, (uint)daten.Length);
            byte[] kopf = System.Text.Encoding.ASCII.GetBytes(art);
            s.Write(kopf, 0, 4);
            s.Write(daten, 0, daten.Length);
            uint crc = 0xFFFFFFFF;
            foreach (byte x in kopf.Concat(daten))
            {
                crc ^= x;
                for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
            Zahl(s, crc ^ 0xFFFFFFFF);
        }

        private static void Zahl(Stream s, uint wert)
        {
            s.WriteByte((byte)(wert >> 24)); s.WriteByte((byte)(wert >> 16)); s.WriteByte((byte)(wert >> 8)); s.WriteByte((byte)wert);
        }

        private static void Zahl(Stream s, int wert) => Zahl(s, (uint)wert);

        // ------------------------------------------------------------- Kommentare, Eigenschaften

        /// <summary>Die Erläuterungen als Word-Kommentare (ohne Datum — die Datei bleibt bei jedem Lauf gleich), Kennungen 0 bis 8.</summary>
        private static void Kommentare(MainDocumentPart main, Texte t)
        {
            WordprocessingCommentsPart teil = main.WordprocessingCommentsPart ?? main.AddNewPart<WordprocessingCommentsPart>();
            teil.Comments = new Comments(t.Kommentare.Select((absaetze, i) =>
                Beispielvorlage.Kommentar(i.ToString(CultureInfo.InvariantCulture), absaetze)));
            teil.Comments.Save();
        }

        /// <summary>Katalogfassung, Art und Sprache in <c>docProps/custom.xml</c> (Konzept 4.9, 5.6).</summary>
        private static void Dokumenteigenschaften(WordprocessingDocument doc, int katalogfassung, string sprache, TextWriter aus)
        {
            CustomFilePropertiesPart teil = doc.CustomFilePropertiesPart ?? doc.AddCustomFilePropertiesPart();
            Eigenschaftsliste liste = teil.Properties;
            if (liste == null) teil.Properties = liste = new Eigenschaftsliste();
            Beispielvorlage.Setze(liste, Beispielvorlage.EIGENSCHAFT_KATALOGFASSUNG, new VTInt32(katalogfassung.ToString(CultureInfo.InvariantCulture)));
            Beispielvorlage.Setze(liste, Beispielvorlage.EIGENSCHAFT_VORLAGE, new VTLPWSTR(KENNUNG));
            Beispielvorlage.Setze(liste, EIGENSCHAFT_SPRACHE, new VTLPWSTR(sprache));
            liste.Save();
            aus.WriteLine("  custom.xml: " + string.Join(", ", liste.Elements<Eigenschaft>().Select(e => e.Name?.Value + " = " + e.InnerText)));
        }

        // ------------------------------------------------------------- Abschlussprüfung

        /// <summary>
        /// Stilregeln, Validator und die Regeln der Wache: jede Marke in eigenem Run mit <c>w:noProof</c>, kein
        /// „INEKON“, die Kopfzeile mit dem Bildplatzhalter des Logos, drei Bildrahmen mit Schlüssel im Alternativtext
        /// auf dem Platzhalterbild, neun Kommentare mit je einem Verweis, Blockmarken paarig, die Mustertabelle da und
        /// die drei Eigenschaften in <c>custom.xml</c>.
        /// </summary>
        private static bool Abschlusspruefung(string pfad, Texte t, int katalogfassung, TextWriter aus)
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
                        .SelectMany(p => Markenmuster.Matches(Beispielvorlage.Absatztext(p)).Select(m => m.Value)).ToList();
                    aus.WriteLine("  " + name + ": " + gefunden.Count + " Marken");
                    int inRuns = 0;
                    foreach (Run r in wurzel.Descendants<Run>())
                    {
                        string text = string.Concat(r.Elements<Text>().Select(x => x.Text));
                        if (!Markenmuster.IsMatch(text)) continue;
                        inRuns++;
                        if (Markenmuster.Match(text).Value != text)
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
                    .SelectMany(p => Markenmuster.Matches(Beispielvorlage.Absatztext(p)).Select(m => m.Value)).ToList();
                if (marken.Count(m => m.StartsWith("{{#je", StringComparison.Ordinal)) != marken.Count(m => m == "{{/je}}")
                    || marken.Count(m => m.StartsWith("{{#wenn", StringComparison.Ordinal)) != marken.Count(m => m == "{{/wenn}}"))
                    befunde.Add("Rumpf: Blockmarken nicht paarig.");

                befunde.AddRange(Beispielvorlage.Bildbefunde(main));
                List<DW.DocProperties> rahmen = main.Document.Body.Descendants<DW.DocProperties>().ToList();
                string[] erwartet = { "{{bild.wirtschaft.spanne}}", "{{stand.bild.deckung_waerme}}", "{{stand.bild.deckung_strom}}" };
                if (!rahmen.Select(d => d.Description?.Value).SequenceEqual(erwartet))
                    befunde.Add("Rumpf: Bildrahmen " + string.Join(", ", rahmen.Select(d => d.Description?.Value)) + ", erwartet " + string.Join(", ", erwartet));
                if (main.ImageParts.Count() != 1 || !main.TryGetPartById(KENNUNG_BILD, out OpenXmlPart bildteil)
                    || !Beispielvorlage.Bytes(bildteil).SequenceEqual(Bildplatzhalter()))
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
                if (kommentare.Count != t.Kommentare.Length || !kommentare.SequenceEqual(verweise))
                    befunde.Add("Kommentare (" + string.Join(", ", kommentare) + ") und Verweise (" + string.Join(", ", verweise) + ") passen nicht.");

                List<Eigenschaft> liste = doc.CustomFilePropertiesPart?.Properties?.Elements<Eigenschaft>().ToList() ?? new List<Eigenschaft>();
                foreach ((string name, string wert) in new[]
                         {
                             (Beispielvorlage.EIGENSCHAFT_KATALOGFASSUNG, katalogfassung.ToString(CultureInfo.InvariantCulture)),
                             (Beispielvorlage.EIGENSCHAFT_VORLAGE, KENNUNG), (EIGENSCHAFT_SPRACHE, t.Sprache),
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

        // ------------------------------------------------------------- Texte

        private static Texte Deutsch() => new Texte
        {
            Sprache = "de",
            Titel = "Berichtsvorlage EPOS-Plan — Kurzbericht",
            Beschreibung = "Lehrvorlage eines Kurzberichts aus Einzelwerten, Blöcken, Tabellen und Bildern, erläutert in Kommentaren; Kopie über „Neue Vorlage…“.",
            Untertitel = "Kurzbericht",
            Kunde = "Kunde", Bearbeitung = "Bearbeitung", Ersteller = "Erstellt von", Datum = "Datum",
            Verglichen = "Verglichen:", ErstelltMit = "Erstellt mit",
            Ausgangslage = "Ausgangslage", Klimaregion = "Klimaregion",
            Ergebnisse = "Ergebnisse im Überblick", Variante = "Variante", Waermebedarf = "Wärmebedarf", Jaz = "JAZ",
            Co2 = "CO₂", Kapitalwertdiff = "Kapitalwertdifferenz",
            Empfehlung = "Empfehlung", BesteVor = "Beste Variante:", BesteMitte = "mit einer Kapitalwertdifferenz von", BesteNach = ".",
            Wirtschaft = "Wirtschaftlichkeit",
            Kuehlung = "Kühlung", KuehlungVor = "Der Kältebedarf beträgt", KuehlungMitte = "; davon deckt die Anlage", KuehlungNach = ".",
            Deckung = "Deckung je Variante", Anhang = "Anhang",
            Stamm = "Stamm", Gruppe = "Gruppe", Summe = "Summe", Warnung = "Warnung",
            Kommentare = new[]
            {
                new[]
                {
                    "Kurzbericht – eine Lehrvorlage. Sie zeigt, wie ein Bericht aus einzelnen Werten, Blöcken, Tabellen und Bildern "
                    + "entsteht, statt aus ganzen Kapiteln. Text in doppelten geschweiften Klammern ist ein Platzhalter, etwa "
                    + "{{projekt.kunde}}: Beim Erstellen setzt EPOS-Plan an seine Stelle den Wert aus dem Projekt, in Schrift, Größe "
                    + "und Farbe des Platzhalters.",
                    "Das Deckblatt ist ein eigener Abschnitt ohne Kopf- und Fußzeile. Die Beschriftungen („Kunde“, „Datum“) sind "
                    + "fester Text dieser Datei – der Kurzbericht liegt je Sprache vor. Kopfzeile, Fußzeile und das Logo rechts oben "
                    + "tragen ebenfalls Platzhalter; das Logo setzt EPOS-Plan aus den Einstellungen ein.",
                    "Diese Datei ist schreibgeschützt. „Neue Vorlage…“ legt eine Kopie an, die Sie frei ändern. Kommentare "
                    + "erscheinen nicht im fertigen Bericht.",
                },
                new[]
                {
                    "Einzelwerte stehen mitten im Satz. Fehlt ein Wert, steht ein Strich – oder mit der Angabe |leer statt strich "
                    + "nichts.",
                },
                new[]
                {
                    "Die zweite Zeile der Tabelle ist eine Musterzeile: {{#je stand}} in der ersten und {{/je}} in der letzten Zelle. "
                    + "EPOS-Plan wiederholt sie für das Projekt und jede verglichene Variante – bei „Variante 1“ und „Variante 2“ "
                    + "entstehen drei Zeilen.",
                    "Formatangaben hinter dem senkrechten Strich: |ohne einheit lässt die Einheit weg (sie steht dann im Kopf), "
                    + "|stellen 1 zeigt eine Nachkommastelle, |mit grund nennt den Grund, wenn ein Wert fehlt. Die Warnungen darunter "
                    + "erscheinen nur, wenn es welche gibt.",
                },
                new[]
                {
                    "Werte der besten Variante im Fließtext, etwa „Beste Variante: Variante 1 mit einer Kapitalwertdifferenz von "
                    + "10.000 €.“ Welche Variante die beste ist, bestimmt die Wirtschaftlichkeitsrechnung.",
                },
                new[]
                {
                    "Ein Bild ist ein Bildrahmen mit dem Schlüssel im Alternativtext, hier {{bild.wirtschaft.spanne}}. EPOS-Plan "
                    + "zeichnet das Diagramm in der Breite des Rahmens. Der Block {{#wenn hat.bild.wirtschaft.spanne}} lässt Rahmen "
                    + "und Absatz entfallen, wenn es nichts zu zeichnen gibt.",
                    "{{tabelle.wirtschaft.szenarien}} allein in einem Absatz ist eine Strukturtabelle: EPOS-Plan baut Zeilen und "
                    + "Spalten selbst. Ihr Aussehen folgt der Mustertabelle am Ende dieser Datei.",
                },
                new[]
                {
                    "Ein bedingter Abschnitt: Alles zwischen {{#wenn hat.kaelte}} und {{/wenn}} erscheint nur, wenn das Projekt "
                    + "Kälte rechnet. Die Absätze mit den Blockmarken entfallen im Bericht.",
                },
                new[]
                {
                    "Zwei Bilder nebeneinander: eine zweispaltige Tabelle ohne Rahmen, in jeder Zelle ein Bildrahmen in halber "
                    + "Breite. EPOS-Plan zeichnet jedes Diagramm in dieser Größe, die Schrift bleibt lesbar.",
                    "Der Block {{#je stand}} … {{/je}} wiederholt Überschrift und Bilder für das Projekt und jede Variante.",
                },
                new[]
                {
                    "Ein ganzes Kapitel unter einem Kapitelkopf im Format „EPOS Kapitelkopf“. |ohne titel unterdrückt die eigene "
                    + "Überschrift des Kapitels, |ebene 2 rückt seine übrigen Überschriften eine Ebene tiefer. Wird das Kapitel "
                    + "abgewählt, entfällt der Kapitelkopf mit.",
                },
                new[]
                {
                    "Die Mustertabelle mit dem Alternativtext {{muster.tabelle}}: Schattierung und Schrift jeder Zelle gelten für "
                    + "die Zeilen dieser Rolle in allen Strukturtabellen – Stamm, Gruppe, Summe, Warnung. EPOS-Plan liest sie und "
                    + "entfernt sie aus dem Bericht.",
                },
            },
        };

        private static Texte Englisch() => new Texte
        {
            Sprache = "en",
            Titel = "EPOS-Plan report template — summary report",
            Beschreibung = "Teaching template of a summary report built from single values, blocks, tables and images, explained in comments; copy it via “New template…”.",
            Untertitel = "Summary report",
            Kunde = "Client", Bearbeitung = "Prepared by", Ersteller = "Issued by", Datum = "Date",
            Verglichen = "Compared:", ErstelltMit = "Created with",
            Ausgangslage = "Initial situation", Klimaregion = "Climate region",
            Ergebnisse = "Results at a glance", Variante = "Variant", Waermebedarf = "Heat demand", Jaz = "SPF",
            Co2 = "CO₂", Kapitalwertdiff = "Net present value difference",
            Empfehlung = "Recommendation", BesteVor = "Best variant:", BesteMitte = "with a net present value difference of", BesteNach = ".",
            Wirtschaft = "Economic viability",
            Kuehlung = "Cooling", KuehlungVor = "The cooling demand is", KuehlungMitte = "; the system covers", KuehlungNach = " of it.",
            Deckung = "Coverage per variant", Anhang = "Appendix",
            Stamm = "Base", Gruppe = "Group", Summe = "Total", Warnung = "Warning",
            Kommentare = new[]
            {
                new[]
                {
                    "Summary report – a teaching template. It shows how a report is built from single values, blocks, tables and "
                    + "images instead of whole chapters. Text in double curly braces is a placeholder, e.g. {{projekt.kunde}}: when "
                    + "the report is created, EPOS-Plan replaces it with the value from the project, in the font, size and colour "
                    + "of the placeholder.",
                    "The title page is a section of its own without header and footer. The labels (“Client”, “Date”) are fixed "
                    + "text of this file – the summary report comes in each language. Header, footer and the logo at the top right "
                    + "carry placeholders as well; EPOS-Plan inserts the logo from the settings.",
                    "This file is read-only. “New template…” creates a copy you can change freely. Comments do not appear in the "
                    + "finished report.",
                },
                new[]
                {
                    "Single values stand in the middle of a sentence. If a value is missing, a dash appears – or nothing with the "
                    + "option |leer statt strich.",
                },
                new[]
                {
                    "The second row of the table is a pattern row: {{#je stand}} in the first and {{/je}} in the last cell. EPOS-Plan "
                    + "repeats it for the project and every compared variant – with “Variant 1” and “Variant 2” you get three rows.",
                    "Options after the vertical bar: |ohne einheit omits the unit (it is shown in the header), |stellen 1 shows one "
                    + "decimal place, |mit grund names the reason when a value is missing. The warnings below appear only if there "
                    + "are any.",
                },
                new[]
                {
                    "Values of the best variant in running text, e.g. “Best variant: Variant 1 with a net present value difference "
                    + "of €10,000.” The economic viability calculation decides which variant is best.",
                },
                new[]
                {
                    "An image is a picture frame with the key in its alternative text, here {{bild.wirtschaft.spanne}}. EPOS-Plan "
                    + "draws the chart in the width of the frame. The block {{#wenn hat.bild.wirtschaft.spanne}} drops frame and "
                    + "paragraph when there is nothing to draw.",
                    "{{tabelle.wirtschaft.szenarien}} alone in a paragraph is a structured table: EPOS-Plan builds rows and "
                    + "columns itself. Its look follows the pattern table at the end of this file.",
                },
                new[]
                {
                    "A conditional section: everything between {{#wenn hat.kaelte}} and {{/wenn}} appears only if the project "
                    + "calculates cooling. The paragraphs holding the block markers are removed from the report.",
                },
                new[]
                {
                    "Two images side by side: a two-column table without borders, a picture frame of half width in each cell. "
                    + "EPOS-Plan draws each chart at this size, so the lettering stays legible.",
                    "The block {{#je stand}} … {{/je}} repeats heading and images for the project and every variant.",
                },
                new[]
                {
                    "A whole chapter under a chapter heading in the style “EPOS Kapitelkopf”. |ohne titel suppresses the chapter’s "
                    + "own heading, |ebene 2 moves its other headings one level down. If the chapter is deselected, the chapter "
                    + "heading is dropped with it.",
                },
                new[]
                {
                    "The pattern table with the alternative text {{muster.tabelle}}: shading and font of each cell apply to the "
                    + "rows of that role in all structured tables – base, group, total, warning. EPOS-Plan reads it and removes it "
                    + "from the report.",
                },
            },
        };
    }
}
