using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using Paket = System.IO.Packaging;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Die Kategorien der Schnellbausteine der Bausteinvorlage in ihrer Reihenfolge (Konzept Berichtsvorlagen 6.3).</summary>
    public enum Bausteinkategorie
    {
        /// <summary>Werte, Listen und Kapitel des Berichts als Ganzes.</summary>
        Bericht,

        /// <summary>Die Installation, die den Bericht erstellt (<c>ersteller.*</c>).</summary>
        Installation,

        /// <summary>Stammdaten und Ergebnisse des Stammprojekts.</summary>
        Stamm,

        /// <summary>Werte über alle gewählten Stände.</summary>
        Gruppe,

        /// <summary>Werte des laufenden Stands — im Block <c>{{#je stand}}</c> oder <c>{{#je variante}}</c>.</summary>
        Stand,

        /// <summary>Werte des laufenden Gebäudes — im Block <c>{{#je gebaeude}}</c>.</summary>
        Gebaeude,

        /// <summary>Der Paarvergleich als Muster (Entscheid BV-E5-4): wenige Beispiele, jeder Standwert hat seinen Zwilling.</summary>
        Paarsicht,

        /// <summary>Die Wiederholblöcke und je Schalter ein Rahmen <c>{{#wenn …}}</c> … <c>{{/wenn}}</c>.</summary>
        Bloecke,

        /// <summary>Die Strukturtabellen (allein im Absatz) und die Mustertabelle.</summary>
        Tabellen,

        /// <summary>Die Bildplatzhalter: ein Platzhalterbild mit dem Schlüssel im Alternativtext.</summary>
        Bilder,
    }

    /// <summary>Ein Schnellbaustein der Bausteinvorlage: Name, Kategorie, Beschreibung und der Katalogeintrag (bei Blockrahmen <c>null</c>).</summary>
    public sealed class Schnellbaustein
    {
        internal Schnellbaustein(string name, Bausteinkategorie kategorie, Vorlagenfeld feld, string block)
        {
            Name = name;
            Kategorie = kategorie;
            Feld = feld;
            Block = block;
        }

        /// <summary>Der Name in Word: der Schlüssel, bei einem Blockrahmen die Blockmarke ohne Klammern (<c>#je stand</c>).</summary>
        public string Name { get; }

        /// <summary>Die Kategorie.</summary>
        public Bausteinkategorie Kategorie { get; }

        /// <summary>Der Katalogeintrag; <c>null</c> bei einem Blockrahmen.</summary>
        public Vorlagenfeld Feld { get; }

        /// <summary>Bei einem Wiederholblock das Blockwort mit Gegenstand (<c>je stand</c>); sonst <c>null</c>.</summary>
        public string Block { get; }
    }

    /// <summary>
    /// <b>Die Bausteinvorlage</b> (Etappe BV-E9, Konzept Berichtsvorlagen 6.3 Nr. 5): eine Word-<b>Dokumentvorlage</b>
    /// (<c>.dotx</c>), deren <b>Schnellbausteine</b> (Glossar, Galerie „Schnellbausteine“ = <c>docParts</c>) jeden
    /// Platzhalter des Katalogs mit Ausgabe Word anbieten — gegliedert wie der <see cref="WordBaukasten"/> (dieselben
    /// Einträge, Paarsicht als Muster), in Kategorien nach Kontext und dazu „Blöcke“, „Tabellen“, „Bilder“. In Word:
    /// Einfügen › Schnellbausteine › Eintrag; der Name ist der Schlüssel, die Beschreibung die des Katalogs in der
    /// Sprache der Datei.
    ///
    /// <para><b>Je Art die Form</b> (Konzept 4.2, 4.3): Text, Zahl und Datum setzen <c>{{schlüssel}}</c> in den Satz
    /// (Verhalten „nur Inhalt“); Liste, Kapitel und Tabelle stehen allein im Absatz, ein Bild ist das neutrale
    /// Platzhalterbild mit dem Schlüssel im Alternativtext, ein Schalter ein Rahmen <c>{{#wenn …}}</c> …
    /// <c>{{/wenn}}</c>, ein Wiederholblock <c>{{#je …}}</c> … <c>{{/je}}</c> (Verhalten „eigener Absatz“). Jeder
    /// Lauf mit Platzhalter trägt <c>w:noProof</c>.</para>
    ///
    /// <para><b>Der Rumpf ist die Standardvorlage</b> (<see cref="BerichtsvorlagenCtrl.DATEI_STANDARD"/>): Stile, Deckblatt,
    /// Kapitel, Kopf- und Fußzeile mit dem Bildplatzhalter des Logos. So ergibt die Datei — als Vorlage gewählt oder als
    /// Dokument, das Word aus ihr anlegt — ohne weiteres den Standardbericht, und der Anwender ersetzt oder ergänzt
    /// Kapitel mit den Schnellbausteinen; ein erklärender Einleitungstext stünde dagegen in jedem Bericht und müsste
    /// erst gelöscht werden. Der Rumpf bleibt sprachneutral, darum führt <c>custom.xml</c> keine
    /// <c>EPOS.Sprache</c> — nur Beschreibungen und Kategorien der Bausteine sind in der Sprache der Datei.</para>
    ///
    /// <para><b>Wiederholbar:</b> Glossar, Stile des Glossars und das Platzhalterbild tragen feste Teilnamen und
    /// Beziehungskennungen, die Bausteine feste Kennungen (aus Sprache und Name abgeleitet), das Bild feste Bytes. Das
    /// Werkzeug <c>Werkzeuge/Berichtsvorlage</c> (Befehl <c>bausteine</c>) schreibt die ausgelieferten Dateien daraus
    /// und stempelt die Zeiten; die Wache vergleicht sie über <see cref="BerichtsvorlagenCtrl.Inhaltsschluessel"/>.</para>
    /// </summary>
    public static class WordBausteinvorlage
    {
        /// <summary>Die Art der Vorlage in <c>EPOS.Vorlage</c>.</summary>
        public const string VORLAGENART = "bausteine";

        /// <summary>Der Teil des Glossars.</summary>
        public const string TEIL_GLOSSAR = "/word/glossary/document.xml";

        /// <summary>Die Stile des Glossars (eine Kopie der Stile des Rumpfs — die Bausteine nennen dieselben Stilkennungen).</summary>
        public const string TEIL_GLOSSAR_STILE = "/word/glossary/styles.xml";

        /// <summary>Das Platzhalterbild der Bildbausteine.</summary>
        public const string TEIL_BILD = "/word/glossary/media/bausteinbild.png";

        /// <summary>Die Beziehung vom Rumpf zum Glossar.</summary>
        public const string KENNUNG_GLOSSAR = "rIdBausteine";

        private const string KENNUNG_STILE = "rIdBausteinstile";
        private const string KENNUNG_BILD = "rIdBausteinbild";

        private const string TYP_GLOSSAR = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/glossaryDocument";
        private const string TYP_STILE = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";
        private const string TYP_BILD = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
        private const string INHALT_GLOSSAR = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.glossary+xml";
        private const string INHALT_VORLAGE = "application/vnd.openxmlformats-officedocument.wordprocessingml.template.main+xml";
        private const string INHALT_STILE = "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml";

        /// <summary>Der Formatbezeichner der eigenen Dokumenteigenschaften (wie Word ihn schreibt).</summary>
        private const string FORMAT_EIGENE = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

        /// <summary>Satzspiegel der Standardvorlage: 9 355 DXA × 635 EMU.</summary>
        private const long INHALT_EMU = 9355L * 635L;

        /// <summary>Das Logo: 150 × 82 Bildpunkte wie das Platzhalterbild der Kopfzeile.</summary>
        private const long LOGO_CX = 150L * 9525L, LOGO_CY = 82L * 9525L;

        /// <summary>Das Platzhalterbild: 80 × 50 Bildpunkte, hellgrau mit Rahmen, ohne Schrift.</summary>
        internal const int BILD_BREITE = 80, BILD_HOEHE = 50;

        /// <summary>Die drei Wiederholblöcke (Konzept 4.2): Blockwort und Gegenstand, in dieser Folge.</summary>
        public static readonly IReadOnlyList<string> Wiederholbloecke = new[] { "je stand", "je variante", "je gebaeude" };

        // =====================================================================
        //  Gliederung
        // =====================================================================

        /// <summary>
        /// Die Schnellbausteine der Katalogfassung <paramref name="fassung"/>: jeder Eintrag des
        /// <see cref="WordBaukasten"/> genau einmal (dieselbe Auswahl — Ausgabe Word, <c>Seit</c> ≤ Fassung, Paarsicht nur
        /// als Beispiele), in Katalogfolge nach Kategorie geordnet, dazu die drei Wiederholblöcke.
        /// </summary>
        public static IReadOnlyList<Schnellbaustein> Bausteine(int fassung)
        {
            var liste = new List<Schnellbaustein>();
            foreach (string block in Wiederholbloecke)
                liste.Add(new Schnellbaustein("#" + block, Bausteinkategorie.Bloecke, null, block));
            foreach (Vorlagenfeld f in WordBaukasten.Abschnitte(fassung).SelectMany(a => a.Eintraege))
                liste.Add(new Schnellbaustein(f.Schluessel, Kategorie(f), f, null));
            return liste.Select((b, i) => (b, i)).OrderBy(x => (int)x.b.Kategorie).ThenBy(x => x.i).Select(x => x.b).ToList();
        }

        /// <summary>Die Kategorie eines Katalogeintrags: Paar vor Art, Art (Tabelle, Bild, Schalter) vor Kontext.</summary>
        public static Bausteinkategorie Kategorie(Vorlagenfeld feld)
        {
            if (feld == null) throw new ArgumentNullException(nameof(feld));
            if (Vorlagenpruefer.IstPaarschluessel(feld.Schluessel)) return Bausteinkategorie.Paarsicht;
            if (string.Equals(feld.Schluessel, Vorlagenfeldkatalog.MUSTER_TABELLE, StringComparison.Ordinal))
                return Bausteinkategorie.Tabellen;
            switch (feld.Art)
            {
                case Vorlagenfeldart.Tabelle: return Bausteinkategorie.Tabellen;
                case Vorlagenfeldart.Bild: return Bausteinkategorie.Bilder;
                case Vorlagenfeldart.Schalter: return Bausteinkategorie.Bloecke;
            }
            switch (WordBaukasten.Abschnitt(feld))
            {
                case Baukastenabschnittsart.Installation: return Bausteinkategorie.Installation;
                case Baukastenabschnittsart.Stamm: return Bausteinkategorie.Stamm;
                case Baukastenabschnittsart.Gruppe: return Bausteinkategorie.Gruppe;
                case Baukastenabschnittsart.Stand: return Bausteinkategorie.Stand;
                case Baukastenabschnittsart.Gebaeude: return Bausteinkategorie.Gebaeude;
                default: return Bausteinkategorie.Bericht;
            }
        }

        /// <summary>Der Name der Kategorie in Word, in der Sprache der Datei: „EPOS · Bericht“, „EPOS · Tabellen“ …</summary>
        public static string Kategoriename(Bausteinkategorie kategorie, bool englisch)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            string name;
            switch (kategorie)
            {
                case Bausteinkategorie.Paarsicht: name = Text(nameof(R.VF_BAUKASTEN_PAARSICHT), kultur); break;
                case Bausteinkategorie.Bloecke: name = Text(nameof(R.VF_BAUSTEINE_BLOECKE), kultur); break;
                case Bausteinkategorie.Tabellen: name = Text(nameof(R.VF_BAUSTEINE_TABELLEN), kultur); break;
                case Bausteinkategorie.Bilder: name = Text(nameof(R.VF_BAUSTEINE_BILDER), kultur); break;
                default:
                    name = Text("VF_KATALOG_KONTEXT_" + kategorie.ToString().ToUpperInvariant(), kultur);
                    if (name.Length > 0) name = char.ToUpper(name[0], kultur) + name.Substring(1);
                    break;
            }
            if (name.Length == 0) name = kategorie.ToString();
            string muster = Text(nameof(R.VF_BAUSTEINE_KATEGORIE), kultur);
            return muster.Contains("{0}", StringComparison.Ordinal) ? muster.Replace("{0}", name) : name;
        }

        /// <summary>Die Beschreibung eines Bausteins in der Sprache der Datei, ohne doppelte Klammern.</summary>
        public static string Beschreibung(Schnellbaustein baustein, bool englisch)
        {
            if (baustein == null) throw new ArgumentNullException(nameof(baustein));
            if (baustein.Feld != null) return WordBaukasten.Entschaerft(Vorlagenfeldkatalog.Beschreibung(baustein.Feld, englisch));
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            switch (baustein.Block)
            {
                case "je stand": return Text(nameof(R.VF_BAUSTEINE_BLOCK_STAND), kultur);
                case "je variante": return Text(nameof(R.VF_BAUSTEINE_BLOCK_VARIANTE), kultur);
                default: return Text(nameof(R.VF_BAUSTEINE_BLOCK_GEBAEUDE), kultur);
            }
        }

        // =====================================================================
        //  Erzeugen
        // =====================================================================

        /// <summary>
        /// <b>Erzeugt die Bausteinvorlage</b> aus dem Rumpf <paramref name="rumpfvorlage"/> (die Bytes der Standardvorlage):
        /// die Datei als Dokumentvorlage, ein vorhandenes Glossar ersetzt durch die <see cref="Bausteine"/> der Word-Fassung
        /// des Katalogs (<see cref="Vorlagenfeldkatalog.KatalogfassungWord"/>), in <c>custom.xml</c>
        /// <c>EPOS.Katalogfassung</c> = diese Fassung und <c>EPOS.Vorlage</c> = <see cref="VORLAGENART"/>, ohne
        /// <c>EPOS.Sprache</c>. Die Rumpfvorlage bleibt unverändert.
        /// </summary>
        public static byte[] Erzeuge(byte[] rumpfvorlage, bool englisch)
        {
            if (rumpfvorlage == null || rumpfvorlage.Length == 0) throw new ArgumentException("Die Rumpfvorlage fehlt.", nameof(rumpfvorlage));
            int fassung = Vorlagenfeldkatalog.KatalogfassungWord;
            IReadOnlyList<Schnellbaustein> bausteine = Bausteine(fassung);

            using var strom = new MemoryStream();
            strom.Write(rumpfvorlage, 0, rumpfvorlage.Length);
            strom.Position = 0;

            byte[] stile;
            Uri rumpf;
            using (WordprocessingDocument doc = WordprocessingDocument.Open(strom, true))
            {
                MainDocumentPart main = doc.MainDocumentPart ?? throw new InvalidDataException("Die Rumpfvorlage hat keinen Rumpf.");
                rumpf = main.Uri;
                if (main.GlossaryDocumentPart != null) main.DeletePart(main.GlossaryDocumentPart);
                stile = TeilBytes(main.StyleDefinitionsPart ?? throw new InvalidDataException("Die Rumpfvorlage hat keine Stile."));
                Eigenschaften(doc, fassung);
            }

            // Die neuen Teile legt System.IO.Packaging an: So tragen Teil und Beziehung auf jedem System denselben Namen.
            strom.Position = 0;
            using (Paket.Package paket = Paket.Package.Open(strom, FileMode.Open, FileAccess.ReadWrite))
            {
                Dokumentvorlage(paket, rumpf);
                Uri glossarUri = new Uri(TEIL_GLOSSAR, UriKind.Relative);
                Uri stileUri = new Uri(TEIL_GLOSSAR_STILE, UriKind.Relative);
                Uri bildUri = new Uri(TEIL_BILD, UriKind.Relative);

                Paket.PackagePart glossar = paket.CreatePart(glossarUri, INHALT_GLOSSAR, Paket.CompressionOption.Normal);
                Schreibe(glossar, Encoding.UTF8.GetBytes(Glossar(bausteine, englisch)));
                paket.GetPart(rumpf).CreateRelationship(Paket.PackUriHelper.GetRelativeUri(rumpf, glossarUri),
                                                        Paket.TargetMode.Internal, TYP_GLOSSAR, KENNUNG_GLOSSAR);

                Paket.PackagePart stilteil = paket.CreatePart(stileUri, INHALT_STILE, Paket.CompressionOption.Normal);
                Schreibe(stilteil, stile);
                glossar.CreateRelationship(Paket.PackUriHelper.GetRelativeUri(glossarUri, stileUri),
                                           Paket.TargetMode.Internal, TYP_STILE, KENNUNG_STILE);

                Paket.PackagePart bild = paket.CreatePart(bildUri, "image/png", Paket.CompressionOption.Normal);
                Schreibe(bild, Platzhalterbild());
                glossar.CreateRelationship(Paket.PackUriHelper.GetRelativeUri(glossarUri, bildUri),
                                           Paket.TargetMode.Internal, TYP_BILD, KENNUNG_BILD);
            }
            return strom.ToArray();
        }

        /// <summary>
        /// Stellt den Rumpf auf den Inhaltstyp der Dokumentvorlage um — Teil gelöscht und unter DEMSELBEN Namen mit seinen
        /// Beziehungen (gleiche Kennungen) neu angelegt. <c>ChangeDocumentType</c> des SDK legte ihn unter einem neuen Namen
        /// an (<c>document2.xml</c>); so bleibt es <c>word/document.xml</c> wie in jeder Word-Vorlage.
        /// </summary>
        private static void Dokumentvorlage(Paket.Package paket, Uri rumpf)
        {
            Paket.PackagePart alt = paket.GetPart(rumpf);
            if (string.Equals(alt.ContentType, INHALT_VORLAGE, StringComparison.OrdinalIgnoreCase)) return;
            if (alt.ContentType.Contains("macroEnabled", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Die Rumpfvorlage enthält Makros.");
            byte[] inhalt;
            using (Stream s = alt.GetStream(FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                inhalt = ms.ToArray();
            }
            var beziehungen = alt.GetRelationships()
                .Select(b => (b.TargetUri, b.TargetMode, b.RelationshipType, b.Id)).ToList();
            paket.DeletePart(rumpf);
            Paket.PackagePart neu = paket.CreatePart(rumpf, INHALT_VORLAGE, Paket.CompressionOption.Normal);
            Schreibe(neu, inhalt);
            foreach ((Uri ziel, Paket.TargetMode art, string typ, string id) in beziehungen)
                neu.CreateRelationship(ziel, art, typ, id);
        }

        private static byte[] TeilBytes(OpenXmlPart teil)
        {
            // Nur den Strom lesen — ein geladenes RootElement schriebe das SDK beim Schließen neu.
            using Stream s = teil.GetStream(FileMode.Open, FileAccess.Read);
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        private static void Schreibe(Paket.PackagePart teil, byte[] bytes)
        {
            using Stream s = teil.GetStream(FileMode.Create, FileAccess.Write);
            s.Write(bytes, 0, bytes.Length);
        }

        /// <summary><c>EPOS.Katalogfassung</c> und <c>EPOS.Vorlage</c> setzen, <c>EPOS.Sprache</c> entfernen — der Rumpf ist sprachneutral.</summary>
        private static void Eigenschaften(WordprocessingDocument doc, int fassung)
        {
            CustomFilePropertiesPart teil = doc.CustomFilePropertiesPart ?? doc.AddCustomFilePropertiesPart();
            teil.Properties ??= new DocumentFormat.OpenXml.CustomProperties.Properties();
            DocumentFormat.OpenXml.CustomProperties.Properties liste = teil.Properties;

            foreach (CustomDocumentProperty p in liste.Elements<CustomDocumentProperty>()
                         .Where(p => string.Equals(p.Name?.Value, Vorlagenpruefer.EIGENSCHAFT_SPRACHE, StringComparison.OrdinalIgnoreCase)).ToList())
                p.Remove();
            Setze(liste, Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG, new VTInt32(fassung.ToString(CultureInfo.InvariantCulture)));
            Setze(liste, WordBaukasten.EIGENSCHAFT_VORLAGE, new VTLPWSTR(VORLAGENART));
            liste.Save();
        }

        private static void Setze(DocumentFormat.OpenXml.CustomProperties.Properties liste, string name, OpenXmlElement wert)
        {
            CustomDocumentProperty p = liste.Elements<CustomDocumentProperty>()
                .FirstOrDefault(e => string.Equals(e.Name?.Value, name, StringComparison.OrdinalIgnoreCase));
            if (p == null)
            {
                int naechste = liste.Elements<CustomDocumentProperty>().Select(e => e.PropertyId?.Value ?? 1).DefaultIfEmpty(1).Max() + 1;
                p = new CustomDocumentProperty { FormatId = FORMAT_EIGENE, PropertyId = Math.Max(2, naechste), Name = name };
                liste.Append(p);
            }
            p.RemoveAllChildren();
            p.Append(wert);
        }

        // =====================================================================
        //  Glossar
        // =====================================================================

        /// <summary>Das XML des Glossars: je Baustein ein <c>w:docPart</c> in der Galerie „Schnellbausteine“.</summary>
        private static string Glossar(IReadOnlyList<Schnellbaustein> bausteine, bool englisch)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            var teile = new DocParts();
            uint bildkennung = 0;
            foreach (Schnellbaustein b in bausteine)
            {
                bool imSatz = b.Feld != null && (b.Feld.Art == Vorlagenfeldart.Text || b.Feld.Art == Vorlagenfeldart.Zahl
                                                 || b.Feld.Art == Vorlagenfeldart.Datum);
                var eigenschaften = new DocPartProperties(
                    new DocPartName { Val = b.Name },
                    new Category(new Name { Val = Kategoriename(b.Kategorie, englisch) },
                                 new Gallery { Val = DocPartGalleryValues.DocumentPart }),
                    new Behaviors(new Behavior { Val = imSatz ? DocPartBehaviorValues.Content : DocPartBehaviorValues.Paragraph }),
                    new Description { Val = Beschreibung(b, englisch) },
                    new DocPartId { Val = Kennung(b.Name, englisch) });
                var rumpf = new DocPartBody();
                foreach (OpenXmlElement e in Inhalt(b, kultur, ref bildkennung)) rumpf.Append(e);
                teile.Append(new DocPart(eigenschaften, rumpf));
            }

            var glossar = new GlossaryDocument(teile);
            glossar.AddNamespaceDeclaration("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            glossar.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            glossar.AddNamespaceDeclaration("wp", "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing");
            glossar.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            glossar.AddNamespaceDeclaration("pic", "http://schemas.openxmlformats.org/drawingml/2006/picture");
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" + glossar.OuterXml;
        }

        /// <summary>Der Inhalt eines Bausteins in der Form seiner Art (Konzept 4.2, 4.3).</summary>
        private static IEnumerable<OpenXmlElement> Inhalt(Schnellbaustein b, CultureInfo kultur, ref uint bildkennung)
        {
            if (b.Block != null)
                return new[] { Marke("{{#" + b.Block + "}}"), new Paragraph(), Marke("{{/je}}") };

            Vorlagenfeld f = b.Feld;
            string marke = "{{" + f.Schluessel + "}}";
            if (string.Equals(f.Schluessel, Vorlagenfeldkatalog.MUSTER_TABELLE, StringComparison.Ordinal))
                return new OpenXmlElement[] { WordBaukasten.Mustertabelle(kultur), new Paragraph() };

            switch (f.Art)
            {
                case Vorlagenfeldart.Schalter:
                    return new[] { Marke("{{#wenn " + f.Schluessel + "}}"), new Paragraph(), Marke("{{/wenn}}") };
                case Vorlagenfeldart.Bild:
                    return new[] { new Paragraph(new Run(Bild(f, ++bildkennung))) };
                default:
                    return new[] { Marke(marke) };
            }
        }

        /// <summary>Ein Absatz mit einem Platzhalterlauf (<c>w:noProof</c>), ohne Formatvorlage — er nimmt die der Einfügestelle.</summary>
        private static Paragraph Marke(string marke)
        {
            return new Paragraph(WordBaukasten.Platzhalterlauf(marke));
        }

        /// <summary>Das Platzhalterbild mit dem Schlüssel im Alternativtext: volle Satzspiegelbreite, je Stand halbe, das Logo in Kopfzeilengröße.</summary>
        private static Drawing Bild(Vorlagenfeld f, uint kennung)
        {
            bool logo = string.Equals(f.Schluessel, Vorlagenfeldkatalog.LOGO, StringComparison.Ordinal);
            bool halb = f.Kontext == Vorlagenfeldkontext.Stand;
            long cx = logo ? LOGO_CX : halb ? INHALT_EMU / 2 : INHALT_EMU;
            long cy = logo ? LOGO_CY : cx * BILD_HOEHE / BILD_BREITE;
            var blip = new DocumentFormat.OpenXml.Drawing.Blip { Embed = KENNUNG_BILD };
            Drawing zeichnung = Wordbilder.Inline(blip, cx, cy, kennung, "{{" + f.Schluessel + "}}");
            zeichnung.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().First().Name = "Baustein " + f.Schluessel;
            return zeichnung;
        }

        /// <summary>Die feste Kennung eines Bausteins: aus Sprache und Name abgeleitet (MD5), in geschweiften Klammern.</summary>
        internal static string Kennung(string name, bool englisch)
        {
            byte[] h = MD5.HashData(Encoding.UTF8.GetBytes("EPOS-Bausteinvorlage|" + (englisch ? "en" : "de") + "|" + name));
            return new Guid(h).ToString("B").ToUpperInvariant();
        }

        // =====================================================================
        //  Platzhalterbild
        // =====================================================================

        /// <summary>
        /// Das neutrale Platzhalterbild der Bildbausteine: hellgrau <c>F2F2F2</c> mit einem Pixel Rahmen <c>BFBFBF</c>, ohne
        /// Schrift — ein PNG (8 Bit RGB) aus festen Bytes mit ungepackten Deflate-Blöcken, auf jedem System dasselbe (wie
        /// das Platzhalterbild des Kurzberichts im Werkzeug).
        /// </summary>
        internal static byte[] Platzhalterbild()
        {
            var roh = new MemoryStream();
            for (int y = 0; y < BILD_HOEHE; y++)
            {
                roh.WriteByte(0);
                for (int x = 0; x < BILD_BREITE; x++)
                {
                    bool rand = x == 0 || y == 0 || x == BILD_BREITE - 1 || y == BILD_HOEHE - 1;
                    byte w = rand ? (byte)0xBF : (byte)0xF2;
                    roh.WriteByte(w); roh.WriteByte(w); roh.WriteByte(w);
                }
            }
            byte[] daten = roh.ToArray();

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
            var kopf = new MemoryStream();
            Zahl(kopf, (uint)BILD_BREITE); Zahl(kopf, (uint)BILD_HOEHE);
            kopf.Write(new byte[] { 8, 2, 0, 0, 0 }, 0, 5);
            Abschnitt(png, "IHDR", kopf.ToArray());
            Abschnitt(png, "IDAT", zlib.ToArray());
            Abschnitt(png, "IEND", Array.Empty<byte>());
            return png.ToArray();
        }

        private static void Abschnitt(Stream s, string art, byte[] daten)
        {
            Zahl(s, (uint)daten.Length);
            byte[] name = Encoding.ASCII.GetBytes(art);
            s.Write(name, 0, 4);
            s.Write(daten, 0, daten.Length);
            uint crc = 0xFFFFFFFF;
            foreach (byte x in name.Concat(daten))
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

        private static string Text(string schluessel, CultureInfo kultur)
        {
            try { return R.ResourceManager.GetString(schluessel, kultur) ?? ""; }
            catch (Exception) { return ""; }
        }
    }
}
