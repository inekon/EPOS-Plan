using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DER DURCHLAUF DURCH EINE WORD-VORLAGE (Konzept Berichtsvorlagen 4.2, 4.3, 6.7,
    // 6.8; Etappe BV-E1).
    //
    // Prüfer und Engine sehen dieselben Stellen: den Rumpf, die Kopf- und Fußzeilen
    // aller Abschnitte, Fuß- und Endnoten, Textfelder (w:txbxContent) und beide Zweige
    // von mc:AlternateContent. Je Absatz entsteht der zusammengesetzte Text — über
    // zerlegte Runs hinweg, so wie Word ihn zeigt —, dazu ein Fundort, den ein Mensch
    // in Word wiederfindet („Tabelle 3, Zeile 2, Zelle beginnt mit ‚Wärme‘“).
    //
    // Gelesen wird über Namen und Namensräume, nicht über die Typen des SDK: Was Word
    // in einen Zweig von mc:AlternateContent schreibt, kommt je nach Fassung als
    // bekanntes oder unbekanntes Element an; der Durchlauf sieht beides gleich.
    // Kommentare werden nicht durchlaufen (Konzept 6.7) — nur gezählt, und zwar mit
    // Kommentarzahl, der EINEN Zählung, die auch die Engine beim Entfernen nennt.
    //
    // ZWEI WEGE MIT ABSICHT (BV-E1 B1a). Dieser Durchlauf ist der Weg des PRÜFERS: Er
    // liest nur und ändert nichts, auch nicht im Speicher. Die Engine füllt über den
    // WordVorlagennormalisierer: Er arbeitet auf den typisierten Elementen des SDK und
    // ZIEHT zerlegte Runs zusammen, weil die Engine den Text an Ort und Stelle ersetzt —
    // das kann ein Leseweg nicht, und ein Prüfer, der die Vorlage umbaut, prüfte nicht
    // mehr die Bytes, die gefüllt werden. Gemeinsam ist die Erkennungsregel: Tabulator,
    // Umbruch, Feldzeichen, Symbol, Bild und jede Behältergrenze trennen, Rechtschreib-,
    // Text- und Kommentarmarken nicht. Wo die beiden sich unterscheiden:
    //   * mc:AlternateContent: Der Durchlauf zählt im Satz nur den ersten Zweig (die
    //     übrigen sind Doppel derselben Stelle), die Engine füllt jeden Zweig.
    //   * w:smartTag und w:fldSimple liest der Durchlauf als Behälter mit; der
    //     Normalisierer steigt nur in die Behälter, die er typisiert kennt (Hyperlink,
    //     Einfügung, customXml, bdo, dir, Inhaltssteuerelement im Satz). Ein Platzhalter
    //     in EINEM Run wird dort gefüllt, einer über mehrere Runs bleibt stehen.
    //   * w:bdo und w:dir sind für den Durchlauf keine Grenze, für den Normalisierer
    //     schon.
    // Die beiden letzten Fälle sind selten (w:smartTag stammt aus älteren Word-Fassungen);
    // sie sind benannt, damit niemand den zweiten Weg für einen Fehler hält.
    // ---------------------------------------------------------------------------

    /// <summary>Die Teile einer Word-Vorlage, in denen Platzhalter stehen können (Konzept 4.3).</summary>
    public enum Vorlagenteilart
    {
        /// <summary>Der Haupttext (<c>w:body</c>).</summary>
        Rumpf,

        /// <summary>Eine Kopfzeile, gezählt in der Folge ihrer Abschnitte.</summary>
        Kopfzeile,

        /// <summary>Eine Fußzeile, gezählt in der Folge ihrer Abschnitte.</summary>
        Fusszeile,

        /// <summary>Eine Fußnote (ohne die Trennlinien).</summary>
        Fussnote,

        /// <summary>Eine Endnote (ohne die Trennlinien).</summary>
        Endnote,

        /// <summary>Ein Textfeld (<c>w:txbxContent</c>) in einem der übrigen Teile.</summary>
        Textfeld,
    }

    /// <summary>
    /// Der Zweig von <c>mc:AlternateContent</c>, in dem eine Stelle liegt. Word schreibt
    /// Textfelder und Formen doppelt — als moderne Form im ersten Zweig, als Ersatz für ältere
    /// Leser im letzten. Beide werden durchlaufen und gleich nummeriert; gezählt wird nur der
    /// erste, damit kein Platzhalter doppelt erscheint.
    /// </summary>
    public enum Vorlagenzweig
    {
        /// <summary>Außerhalb von <c>mc:AlternateContent</c>.</summary>
        Keiner,

        /// <summary>Im ersten Zweig (in der Regel <c>mc:Choice</c>) — die Darstellung, die Word zeigt.</summary>
        Wahl,

        /// <summary>In einem weiteren Zweig (<c>mc:Fallback</c> oder eine zweite Wahl) — ein Doppel.</summary>
        Ersatz,
    }

    /// <summary>Wo ein Inhaltssteuerelement steht (Konzept 4.2, 6.6).</summary>
    public enum Steuerelementebene
    {
        /// <summary>Um ganze Absätze oder Tabellen (<c>w:sdt</c> auf Blockebene).</summary>
        Block,

        /// <summary>Im Satz (<c>w:sdt</c> in einem Absatz) — nur für Text, Zahl, Datum.</summary>
        Satz,

        /// <summary>Um Tabellenzeilen.</summary>
        Zeile,

        /// <summary>Um eine Tabellenzelle.</summary>
        Zelle,
    }

    /// <summary>
    /// <b>Der Fundort einer Stelle</b> — Teil, Absatz oder Tabellenzelle und die ersten Wörter,
    /// an denen ein Mensch die Stelle in Word wiederfindet. Die lesbare Fassung in der Sprache
    /// des Berichts bildet der <see cref="Vorlagenpruefer"/>; <see cref="ToString"/> ist eine
    /// deutsche Kurzform für Protokolle und Tests.
    /// </summary>
    public sealed class Vorlagenort
    {
        internal Vorlagenort(Vorlagenteilart teil, int teilnummer, Vorlagenteilart? wirt, int wirtsnummer,
                             int absatz, int? tabelle, int zeile, int zelle, int absatzInZelle,
                             int zellenInZeile, bool zeileVerbunden, string anfang, Vorlagenzweig zweig)
        {
            Teil = teil;
            Teilnummer = teilnummer;
            Wirt = wirt;
            Wirtsnummer = wirtsnummer;
            Absatz = absatz;
            Tabelle = tabelle;
            Zeile = zeile;
            Zelle = zelle;
            AbsatzInZelle = absatzInZelle;
            ZellenInZeile = zellenInZeile;
            ZeileVerbunden = zeileVerbunden;
            Anfang = anfang ?? "";
            Zweig = zweig;
        }

        /// <summary>Der Teil der Vorlage.</summary>
        public Vorlagenteilart Teil { get; }

        /// <summary>Die Nummer des Teils: Kopfzeile n, Fußnote n, Textfeld n; beim Rumpf 1.</summary>
        public int Teilnummer { get; }

        /// <summary>Bei einem Textfeld der Teil, in dem es verankert ist; sonst <c>null</c>.</summary>
        public Vorlagenteilart? Wirt { get; }

        /// <summary>Die Nummer des Wirtsteils (Kopfzeile 2 …); ohne Wirt 0.</summary>
        public int Wirtsnummer { get; }

        /// <summary>Die Nummer des Absatzes im Teil (1-basiert, Absätze außerhalb von Tabellen);
        /// in einer Tabellenzelle 0.</summary>
        public int Absatz { get; }

        /// <summary>Die Nummer der Tabelle im Teil (1-basiert, in Dokumentfolge, auch verschachtelte);
        /// außerhalb von Tabellen <c>null</c>.</summary>
        public int? Tabelle { get; }

        /// <summary>Die Zeile in der Tabelle (1-basiert); außerhalb 0.</summary>
        public int Zeile { get; }

        /// <summary>Die Zelle in der Zeile (1-basiert, eine verbundene Zelle zählt einmal); außerhalb 0.</summary>
        public int Zelle { get; }

        /// <summary>Der Absatz in der Zelle (1-basiert); außerhalb 0.</summary>
        public int AbsatzInZelle { get; }

        /// <summary>Die Zahl der Zellen in dieser Zeile; außerhalb 0.</summary>
        public int ZellenInZeile { get; }

        /// <summary>Trägt eine Zelle dieser Zeile einen Zellverbund (<c>gridSpan</c> &gt; 1, <c>vMerge</c>, <c>hMerge</c>)?</summary>
        public bool ZeileVerbunden { get; }

        /// <summary>Die ersten Wörter des Absatzes — in einer Tabelle die der Zelle; leer bei einer leeren Stelle.</summary>
        public string Anfang { get; }

        /// <summary>Der Zweig von <c>mc:AlternateContent</c>.</summary>
        public Vorlagenzweig Zweig { get; }

        /// <summary>Liegt die Stelle in einer Tabellenzelle?</summary>
        public bool InTabelle { get { return Tabelle.HasValue; } }

        /// <summary>
        /// Eine Kennung der Stelle ohne den Zweig: Die beiden Zweige eines Textfelds tragen dieselbe
        /// Kennung, damit eine Meldung nicht doppelt erscheint.
        /// </summary>
        public string Kennung
        {
            get
            {
                return string.Concat(Teil.ToString(), Teilnummer.ToString(CultureInfo.InvariantCulture), "/",
                                     Wirt?.ToString() ?? "", Wirtsnummer.ToString(CultureInfo.InvariantCulture), "/A",
                                     Absatz.ToString(CultureInfo.InvariantCulture), "/T",
                                     (Tabelle ?? 0).ToString(CultureInfo.InvariantCulture), "Z",
                                     Zeile.ToString(CultureInfo.InvariantCulture), "S",
                                     Zelle.ToString(CultureInfo.InvariantCulture), "P",
                                     AbsatzInZelle.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Deutsche Kurzform, etwa „Kopfzeile 1, Absatz 2 „EPOS-Plan …““.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Teil).Append(' ').Append(Teilnummer.ToString(CultureInfo.InvariantCulture));
            if (Wirt.HasValue) sb.Append(" in ").Append(Wirt.Value).Append(' ').Append(Wirtsnummer.ToString(CultureInfo.InvariantCulture));
            if (InTabelle)
                sb.Append(", Tabelle ").Append(Tabelle.Value.ToString(CultureInfo.InvariantCulture))
                  .Append(", Zeile ").Append(Zeile.ToString(CultureInfo.InvariantCulture))
                  .Append(", Zelle ").Append(Zelle.ToString(CultureInfo.InvariantCulture));
            else if (Absatz > 0)
                sb.Append(", Absatz ").Append(Absatz.ToString(CultureInfo.InvariantCulture));
            if (Anfang.Length > 0) sb.Append(" „").Append(Anfang).Append('“');
            return sb.ToString();
        }
    }

    /// <summary>Ein Absatz der Vorlage mit seinem zusammengesetzten Text und seiner Elternkette.</summary>
    public sealed class Vorlagenabsatz
    {
        internal Vorlagenabsatz(Vorlagenort ort, string text, string erkennungstext, bool inBlockSteuerelement,
                                string steuerelementTag, OpenXmlElement element, OpenXmlPart teil)
        {
            Ort = ort;
            Text = text ?? "";
            Erkennungstext = erkennungstext ?? "";
            ErsteWoerter = Vorlagenteile.Anfang(Text);
            InBlockSteuerelement = inBlockSteuerelement;
            SteuerelementTag = steuerelementTag;
            Element = element;
            Teil = teil;
        }

        /// <summary>Der Fundort.</summary>
        public Vorlagenort Ort { get; }

        /// <summary>
        /// Der Text des Absatzes, über alle Runs zusammengesetzt, wie Word ihn zeigt: <c>w:t</c>,
        /// Tabulator als <c>\t</c>, Umbruch als <c>\n</c>; ohne gelöschten Text (<c>w:del</c>,
        /// <c>w:moveFrom</c>), ohne Feldanweisungen, ohne die Absätze eingebetteter Textfelder und
        /// ohne Ersatzzweige im Satz.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Der Text, in dem Platzhalter ERKANNT werden — nach der Regel der Engine: Zerlegte Runs
        /// verbinden sich nur innerhalb eines Behälters (Absatz, Hyperlink, Inhaltssteuerelement im
        /// Satz, <c>w:ins</c>, <c>w:customXml</c> …) und nicht über Tabulator, Umbruch, Feldzeichen oder
        /// Bild hinweg. An jeder solchen Stelle steht hier <see cref="Vorlagenteile.TRENNER"/>; ein
        /// Platzhalter, der darüber reicht, wird nicht erkannt und bleibt als offene Klammer stehen.
        /// </summary>
        public string Erkennungstext { get; }

        /// <summary>Die ersten Wörter dieses Absatzes (auch in einer Tabellenzelle der Absatz selbst).</summary>
        public string ErsteWoerter { get; }

        /// <summary>Liegt der Absatz in einer Tabellenzelle?</summary>
        public bool InTabelle { get { return Ort.InTabelle; } }

        /// <summary>Liegt der Absatz in einem Inhaltssteuerelement auf Blockebene?</summary>
        public bool InBlockSteuerelement { get; }

        /// <summary>Der Tag (<c>w:tag</c>) des nächsten umgebenden Block-Steuerelements; <c>null</c> ohne.</summary>
        public string SteuerelementTag { get; }

        /// <summary>Liegt der Absatz in einem Textfeld?</summary>
        public bool ImTextfeld { get { return Ort.Teil == Vorlagenteilart.Textfeld; } }

        /// <summary>Zählt der Absatz mit (nicht im Ersatzzweig eines <c>mc:AlternateContent</c>)?</summary>
        public bool ZaehltMit { get { return Ort.Zweig != Vorlagenzweig.Ersatz; } }

        /// <summary>Das Element <c>w:p</c>.</summary>
        public OpenXmlElement Element { get; }

        /// <summary>Das Paketteil, in dem der Absatz steht.</summary>
        public OpenXmlPart Teil { get; }

        /// <inheritdoc/>
        public override string ToString() { return Ort + ": " + Text; }
    }

    /// <summary>Ein Inhaltssteuerelement (<c>w:sdt</c>) mit Tag (Konzept 4.2).</summary>
    public sealed class Vorlagensteuerelement
    {
        internal Vorlagensteuerelement(string tag, string alias, Steuerelementebene ebene, Vorlagenort ort,
                                       string inhalt, OpenXmlElement element)
        {
            Tag = tag ?? "";
            Alias = alias;
            Ebene = ebene;
            Ort = ort;
            Inhalt = inhalt ?? "";
            Element = element;
        }

        /// <summary>Der Tag (<c>w:tag/@w:val</c>).</summary>
        public string Tag { get; }

        /// <summary>Der Anzeigename (<c>w:alias/@w:val</c>); <c>null</c> ohne.</summary>
        public string Alias { get; }

        /// <summary>Block, Satz, Zeile oder Zelle.</summary>
        public Steuerelementebene Ebene { get; }

        /// <summary>Der Fundort.</summary>
        public Vorlagenort Ort { get; }

        /// <summary>Der Text im Steuerelement.</summary>
        public string Inhalt { get; }

        /// <summary>Zählt das Steuerelement mit (nicht im Ersatzzweig)?</summary>
        public bool ZaehltMit { get { return Ort.Zweig != Vorlagenzweig.Ersatz; } }

        /// <summary>Das Element <c>w:sdt</c>.</summary>
        public OpenXmlElement Element { get; }
    }

    /// <summary>Ein Bild (DrawingML) mit seinem Alternativtext (<c>wp:docPr</c>, Konzept 4.2).</summary>
    public sealed class Vorlagenbild
    {
        internal Vorlagenbild(string beschreibung, string titel, string name, string kennung, Vorlagenort ort,
                              OpenXmlElement element)
        {
            Beschreibung = beschreibung ?? "";
            Titel = titel ?? "";
            Name = name ?? "";
            Kennung = kennung ?? "";
            Ort = ort;
            Element = element;
        }

        /// <summary>Der Alternativtext (<c>docPr/@descr</c>) — dort steht der Schlüssel eines Bildplatzhalters.</summary>
        public string Beschreibung { get; }

        /// <summary>Der Titel des Alternativtexts (<c>docPr/@title</c>).</summary>
        public string Titel { get; }

        /// <summary>Der Name der Form (<c>docPr/@name</c>).</summary>
        public string Name { get; }

        /// <summary>Die Kennung (<c>docPr/@id</c>).</summary>
        public string Kennung { get; }

        /// <summary>Der Fundort (der Absatz, der das Bild trägt).</summary>
        public Vorlagenort Ort { get; }

        /// <summary>Zählt das Bild mit (nicht im Ersatzzweig)?</summary>
        public bool ZaehltMit { get { return Ort.Zweig != Vorlagenzweig.Ersatz; } }

        /// <summary>Das Element <c>wp:docPr</c>.</summary>
        public OpenXmlElement Element { get; }
    }

    /// <summary>Die Anweisung eines Word-Felds (<c>w:instrText</c> oder <c>w:fldSimple/@w:instr</c>, Konzept 6.7).</summary>
    public sealed class Feldanweisung
    {
        internal Feldanweisung(string anweisung, Vorlagenort ort, bool einfach)
        {
            Anweisung = (anweisung ?? "").Trim();
            string[] teile = Anweisung.Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries);
            Feldname = teile.Length > 0 ? teile[0].ToUpperInvariant() : "";
            Ort = ort;
            Einfach = einfach;
        }

        /// <summary>Die Anweisung, etwa <c>DATE \@ "dd.MM.yyyy"</c>.</summary>
        public string Anweisung { get; }

        /// <summary>Der Feldname in Großbuchstaben, etwa <c>DATE</c>, <c>PAGE</c>, <c>TOC</c>.</summary>
        public string Feldname { get; }

        /// <summary>Der Absatz, in dem das Feld beginnt.</summary>
        public Vorlagenort Ort { get; }

        /// <summary>Ein einfaches Feld (<c>w:fldSimple</c>) statt eines zusammengesetzten.</summary>
        public bool Einfach { get; }

        /// <summary>Zählt das Feld mit (nicht im Ersatzzweig)?</summary>
        public bool ZaehltMit { get { return Ort == null || Ort.Zweig != Vorlagenzweig.Ersatz; } }
    }

    /// <summary>Das Ergebnis von <see cref="Vorlagenteile.Durchlaufe"/>: alle Stellen in Dokumentfolge.</summary>
    public sealed class Vorlagendurchlauf
    {
        internal Vorlagendurchlauf(IReadOnlyList<Vorlagenabsatz> absaetze, IReadOnlyList<Vorlagensteuerelement> steuerelemente,
                                   IReadOnlyList<Vorlagenbild> bilder, IReadOnlyList<Feldanweisung> felder, int kommentare,
                                   IReadOnlyList<HeaderPart> kopfzeilen, IReadOnlyList<FooterPart> fusszeilen, int textfelder)
        {
            Absaetze = absaetze;
            Steuerelemente = steuerelemente;
            Bilder = bilder;
            Feldanweisungen = felder;
            Kommentare = kommentare;
            Kopfzeilen = kopfzeilen;
            Fusszeilen = fusszeilen;
            Textfelder = textfelder;
        }

        /// <summary>Alle Absätze: Rumpf, Kopfzeilen, Fußzeilen, Fußnoten, Endnoten; Textfelder an ihrer Stelle.</summary>
        public IReadOnlyList<Vorlagenabsatz> Absaetze { get; }

        /// <summary>Die Inhaltssteuerelemente mit Tag.</summary>
        public IReadOnlyList<Vorlagensteuerelement> Steuerelemente { get; }

        /// <summary>Die Bilder (DrawingML) mit ihrem Alternativtext.</summary>
        public IReadOnlyList<Vorlagenbild> Bilder { get; }

        /// <summary>Die Anweisungen der Word-Felder.</summary>
        public IReadOnlyList<Feldanweisung> Feldanweisungen { get; }

        /// <summary>Die Zahl der Kommentare (sie werden weder geprüft noch ersetzt, Konzept 6.7).</summary>
        public int Kommentare { get; }

        /// <summary>Die Kopfzeilen in der Nummerierung der Fundorte (Folge der Abschnitte).</summary>
        public IReadOnlyList<HeaderPart> Kopfzeilen { get; }

        /// <summary>Die Fußzeilen in der Nummerierung der Fundorte.</summary>
        public IReadOnlyList<FooterPart> Fusszeilen { get; }

        /// <summary>Die Zahl der Textfelder (ein doppelt geschriebenes zählt einmal).</summary>
        public int Textfelder { get; }
    }

    /// <summary>
    /// <b>Der Durchlauf durch alle Teile eines Word-Pakets</b> (Konzept Berichtsvorlagen 6.7):
    /// Rumpf, Kopf- und Fußzeilen aller Abschnitte, Fuß- und Endnoten, Textfelder und beide
    /// Zweige von <c>mc:AlternateContent</c>. Nur Lesen — das Dokument bleibt unverändert.
    /// </summary>
    public static class Vorlagenteile
    {
        /// <summary>Namensraum WordprocessingML.</summary>
        internal const string NS_W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        /// <summary>Namensraum Markup Compatibility.</summary>
        internal const string NS_MC = "http://schemas.openxmlformats.org/markup-compatibility/2006";

        /// <summary>Namensraum der Zeichnungsanker (<c>wp:docPr</c>).</summary>
        internal const string NS_WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";

        /// <summary>Namensraum der Beziehungen (<c>r:id</c>).</summary>
        internal const string NS_R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        /// <summary>
        /// Das Zeichen einer Trennstelle im <see cref="Vorlagenabsatz.Erkennungstext"/>: ein Zeilenumbruch,
        /// über den die Platzhaltersyntax nie hinwegliest.
        /// </summary>
        public const char TRENNER = '\n';

        /// <summary>Höchstzahl der Wörter im Fundort.</summary>
        public const int ANFANG_WOERTER = 4;

        /// <summary>Höchstzahl der Zeichen im Fundort.</summary>
        public const int ANFANG_ZEICHEN = 40;

        /// <summary>
        /// Durchläuft alle Teile der Vorlage. Wirft, wenn ein Teil kein lesbares XML ist — der
        /// Prüfer meldet das als „kann nicht gelesen werden“.
        /// </summary>
        public static Vorlagendurchlauf Durchlaufe(WordprocessingDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            var laeufer = new Laeufer();
            MainDocumentPart main = doc.MainDocumentPart;
            if (main == null || main.RootElement == null)
                return laeufer.Ergebnis(0, Array.Empty<HeaderPart>(), Array.Empty<FooterPart>());

            OpenXmlElement wurzel = main.RootElement;
            OpenXmlElement rumpf = wurzel.ChildElements.FirstOrDefault(e => IstW(e, "body")) ?? wurzel;
            laeufer.Teil(new Teilkontext(Vorlagenteilart.Rumpf, 1, null, 0, main), rumpf);

            List<HeaderPart> kopf = new List<HeaderPart>();
            List<FooterPart> fuss = new List<FooterPart>();
            OrdneKopfUndFuss(main, rumpf, kopf, fuss);
            for (int i = 0; i < kopf.Count; i++)
                if (kopf[i].RootElement != null)
                    laeufer.Teil(new Teilkontext(Vorlagenteilart.Kopfzeile, i + 1, null, 0, kopf[i]), kopf[i].RootElement);
            for (int i = 0; i < fuss.Count; i++)
                if (fuss[i].RootElement != null)
                    laeufer.Teil(new Teilkontext(Vorlagenteilart.Fusszeile, i + 1, null, 0, fuss[i]), fuss[i].RootElement);

            Noten(laeufer, main.FootnotesPart, "footnote", Vorlagenteilart.Fussnote);
            Noten(laeufer, main.EndnotesPart, "endnote", Vorlagenteilart.Endnote);

            return laeufer.Ergebnis(Kommentarzahl(main), kopf, fuss);
        }

        /// <summary>
        /// <b>Die Zahl der Kommentare einer Vorlage</b> — die EINE Zählung für Prüfer und Engine
        /// (Konzept 6.7): die Einträge <c>w:comment</c> des Kommentarteils. Die Prüfzeile
        /// (<see cref="Pruefbefund.Kommentare"/>) und die Laufmeldung
        /// (<see cref="Fuellergebnis.EntfernteKommentare"/>) nennen deshalb dieselbe Zahl. Bereichsmarken
        /// und Verweise ohne Eintrag im Kommentarteil zeigt Word nicht als Kommentar; die Engine entfernt
        /// sie mit, zählt sie aber nicht.
        /// </summary>
        public static int Kommentarzahl(MainDocumentPart main)
        {
            OpenXmlElement wurzel = main?.WordprocessingCommentsPart?.RootElement;
            return wurzel == null ? 0 : wurzel.ChildElements.Count(e => IstW(e, "comment"));
        }

        // =====================================================================
        //  Text und Wörter
        // =====================================================================

        /// <summary>
        /// Der Text eines Absatzes, über alle Runs zusammengesetzt (siehe <see cref="Vorlagenabsatz.Text"/>).
        /// </summary>
        public static string Absatztext(OpenXmlElement absatz)
        {
            if (absatz == null) return "";
            var sb = new StringBuilder();
            Sammle(absatz, sb, false);
            return sb.ToString();
        }

        /// <summary>
        /// Der Text eines Absatzes zum Erkennen der Platzhalter (siehe
        /// <see cref="Vorlagenabsatz.Erkennungstext"/>): wie <see cref="Absatztext"/>, aber mit
        /// <see cref="TRENNER"/> an Tabulator, Umbruch, Feldzeichen, Symbol, Noten- und Bildverweis und an
        /// jeder Grenze eines Behälters im Satz.
        /// </summary>
        public static string Erkennungstext(OpenXmlElement absatz)
        {
            if (absatz == null) return "";
            var sb = new StringBuilder();
            Sammle(absatz, sb, true);
            return sb.ToString();
        }

        /// <summary>
        /// Die ersten Wörter eines Texts für den Fundort: höchstens <see cref="ANFANG_WOERTER"/> Wörter
        /// und <see cref="ANFANG_ZEICHEN"/> Zeichen, gekürzt mit „…“; leer bei leerem Text.
        /// </summary>
        public static string Anfang(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string[] woerter = text.Replace("­", "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (woerter.Length == 0) return "";
            var sb = new StringBuilder();
            int n = 0;
            foreach (string wort in woerter)
            {
                if (n == ANFANG_WOERTER) break;
                if (n > 0 && sb.Length + 1 + wort.Length > ANFANG_ZEICHEN) break;
                if (n > 0) sb.Append(' ');
                sb.Append(wort);
                n++;
            }
            bool gekuerzt = n < woerter.Length;
            if (sb.Length > ANFANG_ZEICHEN)
            {
                sb.Length = ANFANG_ZEICHEN;
                gekuerzt = true;
            }
            return gekuerzt ? sb.ToString().TrimEnd() + " …" : sb.ToString();
        }

        private static void Sammle(OpenXmlElement e, StringBuilder sb, bool erkennung)
        {
            foreach (OpenXmlElement k in e.ChildElements)
            {
                if (k.NamespaceUri == NS_MC)
                {
                    // Im Satz zählt nur die erste Darstellung; die übrigen Zweige sind Doppel.
                    if (k.LocalName == "AlternateContent")
                    {
                        OpenXmlElement erster = k.ChildElements.FirstOrDefault(z => z.NamespaceUri == NS_MC &&
                                                                        (z.LocalName == "Choice" || z.LocalName == "Fallback"));
                        if (erster != null) Behaelter(erster, sb, erkennung);
                    }
                    continue;
                }
                if (k.NamespaceUri != NS_W)
                {
                    Sammle(k, sb, erkennung);
                    continue;
                }
                switch (k.LocalName)
                {
                    case "t":
                        sb.Append(k.InnerText);
                        break;
                    case "tab":
                    case "ptab":
                        sb.Append(erkennung ? TRENNER : '\t');
                        break;
                    case "br":
                    case "cr":
                        sb.Append('\n');
                        break;
                    case "noBreakHyphen":
                        sb.Append('\u2011');
                        break;
                    case "softHyphen":
                        sb.Append('\u00AD');
                        break;
                    // Stellen, über die die Engine keinen Platzhalter verbindet
                    case "fldChar":
                    case "sym":
                    case "footnoteReference":
                    case "endnoteReference":
                    case "drawing":
                    case "pict":
                    case "object":
                        if (erkennung) sb.Append(TRENNER);
                        break;
                    // Behälter im Satz: verbunden wird nur innerhalb
                    case "hyperlink":
                    case "sdt":
                    case "ins":
                    case "moveTo":
                    case "customXml":
                    case "smartTag":
                    case "fldSimple":
                        Behaelter(k, sb, erkennung);
                        break;
                    // Eigenschaften, Feldanweisungen, gelöschter Text, eingebettete Absätze
                    case "pPr":
                    case "rPr":
                    case "sdtPr":
                    case "sdtEndPr":
                    case "instrText":
                    case "delInstrText":
                    case "delText":
                    case "del":
                    case "moveFrom":
                    case "p":
                    case "tbl":
                    case "txbxContent":
                        break;
                    default:
                        Sammle(k, sb, erkennung);
                        break;
                }
            }
        }

        private static void Behaelter(OpenXmlElement k, StringBuilder sb, bool erkennung)
        {
            if (erkennung) sb.Append(TRENNER);
            Sammle(k, sb, erkennung);
            if (erkennung) sb.Append(TRENNER);
        }

        // =====================================================================
        //  Kopf- und Fußzeilen, Noten
        // =====================================================================

        /// <summary>Kopf- und Fußzeilen in der Folge der Abschnitte, dann die nicht verwiesenen.</summary>
        private static void OrdneKopfUndFuss(MainDocumentPart main, OpenXmlElement rumpf,
                                             List<HeaderPart> kopf, List<FooterPart> fuss)
        {
            foreach (OpenXmlElement abschnitt in rumpf.Descendants().Where(e => IstW(e, "sectPr")))
            {
                if (abschnitt.Parent != null && IstW(abschnitt.Parent, "sectPrChange")) continue;
                foreach (OpenXmlElement verweis in abschnitt.ChildElements)
                {
                    bool istKopf = IstW(verweis, "headerReference");
                    if (!istKopf && !IstW(verweis, "footerReference")) continue;
                    string id = Attribut(verweis, "id", NS_R);
                    if (string.IsNullOrEmpty(id)) continue;
                    OpenXmlPart teil = null;
                    try { teil = main.GetPartById(id); } catch { teil = null; }
                    if (istKopf && teil is HeaderPart h && !kopf.Contains(h)) kopf.Add(h);
                    if (!istKopf && teil is FooterPart f && !fuss.Contains(f)) fuss.Add(f);
                }
            }
            foreach (HeaderPart h in main.HeaderParts) if (!kopf.Contains(h)) kopf.Add(h);
            foreach (FooterPart f in main.FooterParts) if (!fuss.Contains(f)) fuss.Add(f);
        }

        private static void Noten(Laeufer laeufer, OpenXmlPart teil, string name, Vorlagenteilart art)
        {
            OpenXmlElement wurzel = teil?.RootElement;
            if (wurzel == null) return;
            int n = 0;
            foreach (OpenXmlElement note in wurzel.ChildElements.Where(e => IstW(e, name)))
            {
                string typ = Attribut(note, "type", NS_W);
                if (typ == "separator" || typ == "continuationSeparator" || typ == "continuationNotice") continue;
                n++;
                laeufer.Teil(new Teilkontext(art, n, null, 0, teil), note);
            }
        }

        // =====================================================================
        //  Helfer über Namen
        // =====================================================================

        internal static bool IstW(OpenXmlElement e, string name)
        {
            return e != null && e.LocalName == name && e.NamespaceUri == NS_W;
        }

        internal static bool IstMc(OpenXmlElement e, string name)
        {
            return e != null && e.LocalName == name && e.NamespaceUri == NS_MC;
        }

        /// <summary>Ein Attribut über Namen und Namensraum; <c>null</c>, wenn es fehlt.</summary>
        internal static string Attribut(OpenXmlElement e, string name, string ns)
        {
            if (e == null) return null;
            foreach (OpenXmlAttribute a in e.GetAttributes())
                if (a.LocalName == name && (a.NamespaceUri ?? "") == (ns ?? "")) return a.Value;
            return null;
        }

        /// <summary>Das erste Kind mit diesem WordprocessingML-Namen; <c>null</c> ohne.</summary>
        internal static OpenXmlElement KindW(OpenXmlElement e, string name)
        {
            return e?.ChildElements.FirstOrDefault(k => IstW(k, name));
        }

        /// <summary>Hülle um Blockinhalt, die keine eigene Stelle ist (Inhaltssteuerelement, eigenes XML).</summary>
        private static IEnumerable<OpenXmlElement> Durchgereicht(OpenXmlElement e, string gesucht)
        {
            foreach (OpenXmlElement k in e.ChildElements)
            {
                if (IstW(k, gesucht)) yield return k;
                else if (IstW(k, "sdt") || IstW(k, "sdtContent") || IstW(k, "customXml"))
                    foreach (OpenXmlElement t in Durchgereicht(k, gesucht)) yield return t;
            }
        }

        // =====================================================================
        //  Der Läufer
        // =====================================================================

        /// <summary>Zähler eines Teils: Absätze, Tabellen, offene Felder.</summary>
        private sealed class Teilkontext
        {
            public Teilkontext(Vorlagenteilart art, int nummer, Vorlagenteilart? wirt, int wirtsnummer, OpenXmlPart teil)
            {
                Art = art;
                Nummer = nummer;
                Wirt = wirt;
                Wirtsnummer = wirtsnummer;
                Teil = teil;
            }

            public readonly Vorlagenteilart Art;
            public readonly int Nummer;
            public readonly Vorlagenteilart? Wirt;
            public readonly int Wirtsnummer;
            public readonly OpenXmlPart Teil;
            public int Absaetze;
            public int Tabellen;
            public readonly Stack<Feldpuffer> Felder = new Stack<Feldpuffer>();
        }

        /// <summary>Die Lage in einer Tabelle; wird beim Durchlauf der Zeilen und Zellen fortgeschrieben.</summary>
        private sealed class Tabellenlage
        {
            public int Nummer;
            public Dictionary<OpenXmlElement, int> Zeilen;
            public Dictionary<OpenXmlElement, int> Zellen = new Dictionary<OpenXmlElement, int>();
            public int Zeile;
            public int Zelle;
            public int ZellenInZeile;
            public bool ZeileVerbunden;
            public string ZellenAnfang = "";
            public int AbsaetzeInZelle;
        }

        private sealed class Feldpuffer
        {
            public readonly StringBuilder Text = new StringBuilder();
            public Vorlagenort Ort;
            public bool Ergebnis;
        }

        /// <summary>Die Lage eines Elements: Teil, Tabelle, Block-Steuerelement, Zweig, umgebender Absatz.</summary>
        private sealed class Lage
        {
            public Teilkontext Teil;
            public Tabellenlage Tabelle;
            public bool InBlockSdt;
            public string BlockTag;
            public Vorlagenzweig Zweig;
            public Vorlagenabsatz Absatz;

            public Lage Kopie()
            {
                return (Lage)MemberwiseClone();
            }
        }

        private sealed class Laeufer
        {
            private readonly List<Vorlagenabsatz> _absaetze = new List<Vorlagenabsatz>();
            private readonly List<Vorlagensteuerelement> _steuerelemente = new List<Vorlagensteuerelement>();
            private readonly List<Vorlagenbild> _bilder = new List<Vorlagenbild>();
            private readonly List<Feldanweisung> _felder = new List<Feldanweisung>();
            private int _textfelder;

            public void Teil(Teilkontext teil, OpenXmlElement wurzel)
            {
                Gehe(wurzel, new Lage { Teil = teil, Zweig = Vorlagenzweig.Keiner });
            }

            public Vorlagendurchlauf Ergebnis(int kommentare, IReadOnlyList<HeaderPart> kopf, IReadOnlyList<FooterPart> fuss)
            {
                return new Vorlagendurchlauf(_absaetze, _steuerelemente, _bilder, _felder, kommentare, kopf, fuss, _textfelder);
            }

            private void Gehe(OpenXmlElement e, Lage lage)
            {
                foreach (OpenXmlElement k in e.ChildElements) Besuche(k, lage);
            }

            private void Besuche(OpenXmlElement k, Lage lage)
            {
                if (k.NamespaceUri == NS_MC)
                {
                    if (k.LocalName == "AlternateContent") Alternativen(k, lage);
                    else Gehe(k, lage);
                    return;
                }
                if (k.NamespaceUri == NS_WP)
                {
                    if (k.LocalName == "docPr") Bild(k, lage);
                    else Gehe(k, lage);
                    return;
                }
                if (k.NamespaceUri != NS_W)
                {
                    Gehe(k, lage);
                    return;
                }
                switch (k.LocalName)
                {
                    case "p": Absatz(k, lage); return;
                    case "tbl": Tabelle(k, lage); return;
                    case "tr": Zeile(k, lage); return;
                    case "tc": Zelle(k, lage); return;
                    case "sdt": Steuerelement(k, lage); return;
                    case "txbxContent": Textfeld(k, lage); return;
                    case "fldChar": Feldzeichen(k, lage); return;
                    case "instrText": Feldtext(k, lage); return;
                    case "fldSimple":
                        _felder.Add(new Feldanweisung(Attribut(k, "instr", NS_W), lage.Absatz?.Ort ?? OrtOhneAbsatz(lage, ""), true));
                        Gehe(k, lage);
                        return;
                    // Kommentarinhalte und gelöschte Feldanweisungen gehören nicht zur Vorlage.
                    case "delInstrText":
                        return;
                    default:
                        Gehe(k, lage);
                        return;
                }
            }

            /// <summary>
            /// Alle Zweige werden durchlaufen, jeder mit den Zählern vom Anfang: So tragen Absätze,
            /// Tabellen und Textfelder beider Zweige dieselben Nummern, und ihre Meldungen fallen
            /// zusammen. Danach gelten die Zähler nach dem ersten Zweig.
            /// </summary>
            private void Alternativen(OpenXmlElement ac, Lage lage)
            {
                List<OpenXmlElement> zweige = ac.ChildElements
                    .Where(z => z.NamespaceUri == NS_MC && (z.LocalName == "Choice" || z.LocalName == "Fallback"))
                    .ToList();
                if (zweige.Count == 0) return;

                Zaehlerstand vorher = Sichere(lage);
                Zaehlerstand nachErstem = null;
                for (int i = 0; i < zweige.Count; i++)
                {
                    Stelle(lage, vorher);
                    Lage im = lage.Kopie();
                    im.Zweig = i == 0 && lage.Zweig != Vorlagenzweig.Ersatz ? Vorlagenzweig.Wahl : Vorlagenzweig.Ersatz;
                    Gehe(zweige[i], im);
                    if (nachErstem == null) nachErstem = Sichere(lage);
                }
                Stelle(lage, nachErstem);
            }

            private sealed class Zaehlerstand
            {
                public int Absaetze, Tabellen, Textfelder, AbsaetzeInZelle;
            }

            private Zaehlerstand Sichere(Lage lage)
            {
                return new Zaehlerstand
                {
                    Absaetze = lage.Teil.Absaetze,
                    Tabellen = lage.Teil.Tabellen,
                    Textfelder = _textfelder,
                    AbsaetzeInZelle = lage.Tabelle?.AbsaetzeInZelle ?? 0,
                };
            }

            private void Stelle(Lage lage, Zaehlerstand stand)
            {
                lage.Teil.Absaetze = stand.Absaetze;
                lage.Teil.Tabellen = stand.Tabellen;
                _textfelder = stand.Textfelder;
                if (lage.Tabelle != null) lage.Tabelle.AbsaetzeInZelle = stand.AbsaetzeInZelle;
            }

            private void Absatz(OpenXmlElement p, Lage lage)
            {
                string text = Absatztext(p);
                Tabellenlage tab = lage.Tabelle;
                int absatz = 0;
                if (tab != null) tab.AbsaetzeInZelle++;
                else absatz = ++lage.Teil.Absaetze;

                Vorlagenort ort = NeuerOrt(lage, absatz, tab != null ? tab.ZellenAnfang : Anfang(text));
                var a = new Vorlagenabsatz(ort, text, Erkennungstext(p), lage.InBlockSdt, lage.BlockTag, p, lage.Teil.Teil);
                _absaetze.Add(a);

                Lage im = lage.Kopie();
                im.Absatz = a;
                Gehe(p, im);
            }

            private static Vorlagenort NeuerOrt(Lage lage, int absatz, string anfang)
            {
                Tabellenlage tab = lage.Tabelle;
                Teilkontext t = lage.Teil;
                return new Vorlagenort(t.Art, t.Nummer, t.Wirt, t.Wirtsnummer, absatz,
                                       tab?.Nummer, tab?.Zeile ?? 0, tab?.Zelle ?? 0, tab?.AbsaetzeInZelle ?? 0,
                                       tab?.ZellenInZeile ?? 0, tab?.ZeileVerbunden ?? false, anfang, lage.Zweig);
            }

            /// <summary>Der Ort einer Stelle, die keinen eigenen Absatz hat: der nächste Absatz des Teils.</summary>
            private static Vorlagenort OrtOhneAbsatz(Lage lage, string anfang)
            {
                int absatz = lage.Tabelle != null ? 0 : lage.Teil.Absaetze + 1;
                return NeuerOrt(lage, absatz, lage.Tabelle != null ? lage.Tabelle.ZellenAnfang : anfang);
            }

            private void Tabelle(OpenXmlElement tbl, Lage lage)
            {
                lage.Teil.Tabellen++;
                var tab = new Tabellenlage { Nummer = lage.Teil.Tabellen, Zeilen = new Dictionary<OpenXmlElement, int>() };
                int r = 0;
                foreach (OpenXmlElement zeile in Durchgereicht(tbl, "tr")) tab.Zeilen[zeile] = ++r;

                Lage im = lage.Kopie();
                im.Tabelle = tab;
                im.Absatz = null;
                Gehe(tbl, im);
            }

            private void Zeile(OpenXmlElement tr, Lage lage)
            {
                Tabellenlage tab = lage.Tabelle;
                if (tab == null)
                {
                    Gehe(tr, lage);
                    return;
                }
                tab.Zeile = tab.Zeilen.TryGetValue(tr, out int r) ? r : tab.Zeile + 1;
                List<OpenXmlElement> zellen = Durchgereicht(tr, "tc").ToList();
                tab.Zellen = new Dictionary<OpenXmlElement, int>();
                for (int i = 0; i < zellen.Count; i++) tab.Zellen[zellen[i]] = i + 1;
                tab.ZellenInZeile = zellen.Count;
                tab.ZeileVerbunden = zellen.Any(IstVerbunden);
                tab.Zelle = 0;
                Gehe(tr, lage);
            }

            private void Zelle(OpenXmlElement tc, Lage lage)
            {
                Tabellenlage tab = lage.Tabelle;
                if (tab == null)
                {
                    Gehe(tc, lage);
                    return;
                }
                tab.Zelle = tab.Zellen.TryGetValue(tc, out int c) ? c : tab.Zelle + 1;
                tab.ZellenAnfang = Anfang(Zellentext(tc));
                tab.AbsaetzeInZelle = 0;
                Gehe(tc, lage);
            }

            private static bool IstVerbunden(OpenXmlElement tc)
            {
                OpenXmlElement pr = KindW(tc, "tcPr");
                if (pr == null) return false;
                OpenXmlElement spanne = KindW(pr, "gridSpan");
                if (spanne != null && int.TryParse(Attribut(spanne, "val", NS_W), NumberStyles.Integer,
                                                   CultureInfo.InvariantCulture, out int n) && n > 1) return true;
                return KindW(pr, "vMerge") != null || KindW(pr, "hMerge") != null;
            }

            /// <summary>Der Text einer Zelle: ihre Absätze (ohne Textfelder), durch Leerzeichen getrennt.</summary>
            private static string Zellentext(OpenXmlElement tc)
            {
                var sb = new StringBuilder();
                foreach (OpenXmlElement p in tc.Descendants().Where(e => IstW(e, "p")))
                {
                    if (p.Ancestors().Any(a => IstW(a, "txbxContent"))) continue;
                    string t = Absatztext(p);
                    if (t.Trim().Length == 0) continue;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(t.Trim());
                    if (sb.Length > ANFANG_ZEICHEN * 2) break;
                }
                return sb.ToString();
            }

            private void Steuerelement(OpenXmlElement sdt, Lage lage)
            {
                OpenXmlElement pr = KindW(sdt, "sdtPr");
                string tag = Attribut(KindW(pr, "tag"), "val", NS_W);
                string alias = Attribut(KindW(pr, "alias"), "val", NS_W);

                Steuerelementebene ebene;
                if (lage.Absatz != null) ebene = Steuerelementebene.Satz;
                else if (IstW(sdt.Parent, "tr")) ebene = Steuerelementebene.Zelle;
                else if (IstW(sdt.Parent, "tbl")) ebene = Steuerelementebene.Zeile;
                else ebene = Steuerelementebene.Block;

                OpenXmlElement inhalt = KindW(sdt, "sdtContent");
                string text = inhalt == null ? "" : ebene == Steuerelementebene.Satz ? Absatztext(inhalt) : Blocktext(inhalt);

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    Vorlagenort ort;
                    if (ebene == Steuerelementebene.Satz) ort = lage.Absatz.Ort;
                    else if (ebene == Steuerelementebene.Block) ort = OrtOhneAbsatz(lage, Anfang(text));
                    else ort = OrtInTabelle(sdt, ebene, lage, Anfang(text));
                    _steuerelemente.Add(new Vorlagensteuerelement(tag.Trim(), alias, ebene, ort, text, sdt));
                }

                if (ebene == Steuerelementebene.Block)
                {
                    Lage im = lage.Kopie();
                    im.InBlockSdt = true;
                    if (!string.IsNullOrWhiteSpace(tag)) im.BlockTag = tag.Trim();
                    Gehe(sdt, im);
                }
                else
                {
                    Gehe(sdt, lage);
                }
            }

            private static Vorlagenort OrtInTabelle(OpenXmlElement sdt, Steuerelementebene ebene, Lage lage, string anfang)
            {
                Tabellenlage tab = lage.Tabelle;
                if (tab == null) return OrtOhneAbsatz(lage, anfang);
                int zeile = tab.Zeile, zelle = 0;
                if (ebene == Steuerelementebene.Zeile)
                {
                    // Um ganze Zeilen: der Fundort nennt die erste Zelle der ersten Zeile.
                    OpenXmlElement tr = sdt.Descendants().FirstOrDefault(e => IstW(e, "tr"));
                    if (tr != null && tab.Zeilen.TryGetValue(tr, out int r)) zeile = r;
                    zelle = 1;
                }
                else
                {
                    OpenXmlElement tc = sdt.Descendants().FirstOrDefault(e => IstW(e, "tc"));
                    if (tc != null && tab.Zellen.TryGetValue(tc, out int c)) zelle = c;
                }
                Teilkontext t = lage.Teil;
                return new Vorlagenort(t.Art, t.Nummer, t.Wirt, t.Wirtsnummer, 0, tab.Nummer, zeile, zelle, 0,
                                       tab.ZellenInZeile, tab.ZeileVerbunden, anfang, lage.Zweig);
            }

            /// <summary>Der Text eines Blockinhalts: seine Absätze (ohne Textfelder), durch Leerzeichen getrennt.</summary>
            private static string Blocktext(OpenXmlElement inhalt)
            {
                return string.Join(" ", inhalt.Descendants()
                    .Where(e => IstW(e, "p") && !e.Ancestors().Any(a => IstW(a, "txbxContent")))
                    .Select(p => Absatztext(p).Trim())
                    .Where(t => t.Length > 0)
                    .Take(8));
            }

            private void Textfeld(OpenXmlElement inhalt, Lage lage)
            {
                _textfelder++;
                Teilkontext wirt = lage.Teil;
                Vorlagenteilart wirtArt = wirt.Wirt ?? wirt.Art;
                int wirtNummer = wirt.Wirt.HasValue ? wirt.Wirtsnummer : wirt.Nummer;
                var teil = new Teilkontext(Vorlagenteilart.Textfeld, _textfelder, wirtArt, wirtNummer, wirt.Teil);
                Gehe(inhalt, new Lage { Teil = teil, Zweig = lage.Zweig });
            }

            private void Bild(OpenXmlElement docPr, Lage lage)
            {
                Vorlagenort ort = lage.Absatz?.Ort ?? OrtOhneAbsatz(lage, "");
                _bilder.Add(new Vorlagenbild(Attribut(docPr, "descr", ""), Attribut(docPr, "title", ""),
                                             Attribut(docPr, "name", ""), Attribut(docPr, "id", ""), ort, docPr));
            }

            private void Feldzeichen(OpenXmlElement zeichen, Lage lage)
            {
                Stack<Feldpuffer> felder = lage.Teil.Felder;
                switch (Attribut(zeichen, "fldCharType", NS_W))
                {
                    case "begin":
                        felder.Push(new Feldpuffer { Ort = lage.Absatz?.Ort ?? OrtOhneAbsatz(lage, "") });
                        break;
                    case "separate":
                        if (felder.Count > 0) felder.Peek().Ergebnis = true;
                        break;
                    case "end":
                        if (felder.Count > 0)
                        {
                            Feldpuffer puffer = felder.Pop();
                            string anweisung = puffer.Text.ToString();
                            if (anweisung.Trim().Length > 0) _felder.Add(new Feldanweisung(anweisung, puffer.Ort, false));
                        }
                        break;
                }
            }

            private static void Feldtext(OpenXmlElement text, Lage lage)
            {
                Stack<Feldpuffer> felder = lage.Teil.Felder;
                if (felder.Count > 0 && !felder.Peek().Ergebnis) felder.Peek().Text.Append(text.InnerText);
            }
        }
    }
}
