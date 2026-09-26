using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using T = WindowsFormsApplication1.WordVorlagentexte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Word-Engine der Berichtsvorlagen</b> (Konzept Berichtsvorlagen mit Platzhaltern 4, 5.3,
    /// 6; Etappe BV-E1): füllt eine Vorlage mit Platzhaltern aus dem Wertesatz eines Berichtslaufs.
    /// Die Vorlage bleibt, wie sie ist — Deckblatt, Abschnitte, Kopf- und Fußzeilen; nichts wird
    /// still gelöscht (Konzept 6.1).
    ///
    /// <para><b>Ablauf.</b> (1) Alle Teile: Rumpf, alle Kopf- und Fußzeilen aller Abschnitte, Fuß-
    /// und Endnoten; Textfelder und beide Zweige von <c>mc:AlternateContent</c> liegen darin als
    /// eigene Absätze. (2) Kommentare und externe Beziehungen entfernen und melden
    /// (<see cref="WordVorlagenbereinigung"/>). (3) Zerlegte Platzhalter im Speicher in einen Run
    /// ziehen (<see cref="WordVorlagennormalisierer"/>). (4) Trägt die Vorlage keinen Platzhalter,
    /// kommt <c>{{bericht.inhalt}}</c> ans Ende des Rumpfs. (5) Die Kapitelstellen sammeln: Je
    /// Kapitel gilt die erste gültige Stelle (getippte Platzhalter vor Inhaltssteuerelementen, je in
    /// Dokumentfolge); jede weitere bleibt gelb stehen, der Sammelanker setzt die einzeln geführten
    /// Kapitel nicht noch einmal ein, und die Überschrift vor jedem Anker ist die Stelle der
    /// Anhang-E-Checkliste. (6) Ersetzen nach Art: Text, Zahl, Datum im Satz (Zeilenumbrüche als
    /// <c>w:br</c>); Liste und Kapitel nur allein im Absatz, der Absatz wird ersetzt — die Liste
    /// durch Absätze in seinem Format (im Satz mit „; “ verbunden), das Kapitel durch die angehakten
    /// Bausteine, die über den <see cref="WordKontext"/> an den <see cref="Einfuegeanker"/> schreiben,
    /// mit <c>|ohne titel</c> und <c>|ebene n</c> (Konzept 4.8). Liefert ein Kapitel nichts, entfällt
    /// ein unmittelbar davor stehender „EPOS Kapitelkopf“ mit (5.3). (7) Inhaltssteuerelemente mit
    /// einem Schlüssel als <c>w:tag</c> werden gefüllt und ausgepackt. (8) Bilder mit einem Schlüssel
    /// im Alternativtext: <c>bild.ersteller.logo</c> bekommt das Logo der Einstellungen, eingepasst in
    /// den Rahmen des Platzhalterbildes; ohne Logo entfällt das Bild. (9) Felder:
    /// <c>w:updateFields</c> nur bei TOC, PAGEREF, REF, SEQ, DOCPROPERTY. (10) <c>docPr/@id</c> in
    /// allen Teilen neu und eindeutig.</para>
    ///
    /// <para><b>Blöcke</b> (Konzept 4.2, 4.7, 6.4, 6.6; Etappe BV-E4): Vor dem Ersetzen werden
    /// Wiederholblöcke und Bedingungen des Rumpfs ausgewertet (<c>WordVorlagenbloecke.cs</c>); jede
    /// Wiederholung trägt ihren Kontext (<see cref="Berichtswerte.MitStand"/>,
    /// <see cref="Berichtswerte.MitGebaeude"/>), aus dem die Platzhalter darin aufgelöst werden.</para>
    ///
    /// <para><b>Fehlverhalten</b> (Konzept 4.10): Ein unbekannter Schlüssel, eine Blockmarke ohne
    /// Gegenstück oder an unzulässiger Stelle, ein Stand- oder Gebäudewert außerhalb seines Blocks
    /// und eine Art an der falschen Stelle bleiben stehen, gelb hervorgehoben, und stehen im <see cref="Fuellergebnis"/>. Ein
    /// leerer Wert bekommt den Leerwert des Eintrags, nie 0; eine Ausnahme bei einem Einzelwert
    /// „—“ mit Warnung — der Bericht bricht nicht ab.</para>
    ///
    /// <para><b>Plattformfrei:</b> Die Engine arbeitet auf den Bytes der Vorlage in einem
    /// <see cref="MemoryStream"/> und schreibt allein die Zieldatei (Konzept 8.4).</para>
    /// </summary>
    public sealed partial class WordVorlagenfueller
    {
        /// <summary>Höchstzahl der Zeichen je XML-Teil beim Öffnen (Konzept 8.5, Schutz vor Zip-Bomben).</summary>
        public const long MAX_ZEICHEN_JE_TEIL = 100_000_000L;

        /// <summary>Der Sammelanker, der die angehakten Kapitel einsetzt.</summary>
        public const string SAMMELANKER = "bericht.inhalt";

        /// <summary>Womit eine Liste im Satz verbunden wird.</summary>
        public const string LISTENTRENNER = "; ";

        /// <summary>Wie viele Zeichen eines Absatzes der Fundort zeigt.</summary>
        public const int AUSZUG_LAENGE = 40;

        /// <summary>Felder, die Word beim Öffnen nicht selbst aktualisiert — nur sie setzen <c>w:updateFields</c> (Konzept 6.7).</summary>
        public static readonly IReadOnlyList<string> AktualisierteFelder = new[] { "TOC", "PAGEREF", "REF", "SEQ", "DOCPROPERTY" };

        /// <summary>Felder, die das Datum des Öffnens zeigen — sie bekommen einen Hinweis (Konzept 6.7).</summary>
        public static readonly IReadOnlyList<string> Datumsfelder = new[] { "DATE", "TIME" };

        /// <summary>
        /// Füllt die Vorlage <paramref name="vorlage"/> mit dem Berichtslauf und schreibt
        /// <paramref name="zielDatei"/>. Die Sprache folgt der Oberfläche (<see cref="BerichtTexte.Englisch"/>).
        /// </summary>
        /// <exception cref="ArgumentException">Leere Vorlage oder keine Berichtsdaten.</exception>
        /// <exception cref="InvalidDataException">Die Vorlage ist kein Word-Dokument.</exception>
        /// <exception cref="NotSupportedException">Die Vorlage trägt Makros (<c>.docm</c>, <c>.dotm</c>).</exception>
        public Fuellergebnis Fuelle(byte[] vorlage, BerichtsDaten daten, BerichtsKonfiguration konfig,
                                    Erstellerangaben ersteller, string zielDatei)
        {
            bool englisch = BerichtTexte.Englisch;
            if (vorlage == null || vorlage.Length == 0)
                throw new ArgumentException(T.T(T.VORLAGE_LEER, englisch), nameof(vorlage));
            if (daten == null || daten.Varianten == null || daten.Varianten.Count == 0)
                throw new ArgumentException("Keine Berichtsdaten vorhanden.");
            if (string.IsNullOrWhiteSpace(zielDatei)) throw new ArgumentNullException(nameof(zielDatei));

            var ergebnis = new Fuellergebnis(zielDatei, englisch);
            byte[] fertig;
            using (var strom = new MemoryStream())
            {
                strom.Write(vorlage, 0, vorlage.Length);
                strom.Position = 0;
                using (WordprocessingDocument doc = Oeffne(strom, ergebnis, englisch))
                    new Lauf(doc, daten, konfig, ersteller, englisch, ergebnis).Fuehre();
                fertig = strom.ToArray();
            }
            File.WriteAllBytes(zielDatei, fertig);
            return ergebnis;
        }

        private static WordprocessingDocument Oeffne(MemoryStream strom, Fuellergebnis ergebnis, bool englisch)
        {
            WordprocessingDocument doc;
            try
            {
                doc = WordprocessingDocument.Open(strom, true, new OpenSettings { MaxCharactersInPart = MAX_ZEICHEN_JE_TEIL });
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                throw new InvalidDataException(T.T(T.VORLAGE_UNLESBAR, englisch), ex);
            }

            try
            {
                if (doc.DocumentType == WordprocessingDocumentType.MacroEnabledDocument ||
                    doc.DocumentType == WordprocessingDocumentType.MacroEnabledTemplate)
                    throw new NotSupportedException(T.T(T.VORLAGE_MAKROS, englisch));
                if (doc.DocumentType == WordprocessingDocumentType.Template)
                {
                    doc.ChangeDocumentType(WordprocessingDocumentType.Document);
                    ergebnis.Hinweis(T.T(T.DOTX, englisch));
                }
                if (doc.MainDocumentPart?.Document?.Body == null)
                    throw new InvalidDataException(T.T(T.VORLAGE_UNLESBAR, englisch));
                return doc;
            }
            catch (Exception ex) when (!(ex is NotSupportedException) && !(ex is InvalidDataException) && !(ex is OutOfMemoryException))
            {
                doc.Dispose();
                throw new InvalidDataException(T.T(T.VORLAGE_UNLESBAR, englisch), ex);
            }
            catch
            {
                doc.Dispose();
                throw;
            }
        }

        // =====================================================================
        //  Ein Füllvorgang
        // =====================================================================

        /// <summary>Welcher Teil des Dokuments.</summary>
        private enum Teilart { Rumpf, Kopfzeile, Fusszeile, Fussnoten, Endnoten }

        /// <summary>Wie ein Inhaltssteuerelement steht: keines, im Satz, als Block.</summary>
        private enum SdtForm { Keine, ImSatz, Block }

        private enum Entscheidart { Text, Liste, Kapitel, Tabelle, Bild, Stehen }

        /// <summary>Ein Teil samt Wurzel und — bei Kopf- und Fußzeilen — Abschnitt und Art.</summary>
        private sealed class Teilinfo
        {
            internal Teilinfo(OpenXmlPart teil, OpenXmlElement wurzel, Teilart art, int abschnitt, string typ)
            { Teil = teil; Wurzel = wurzel; Art = art; Abschnitt = abschnitt; Typ = typ; }

            internal OpenXmlPart Teil { get; }
            internal OpenXmlElement Wurzel { get; }
            internal Teilart Art { get; }
            internal int Abschnitt { get; }
            internal string Typ { get; }
        }

        /// <summary>Ein <c>w:t</c> mit seinen Platzhaltern (nach dem Normalisieren liegt jeder ganz darin).</summary>
        private sealed class Textstelle
        {
            internal Textstelle(Teilinfo teil, Paragraph absatz, Text text, List<Platzhalter> marken)
            { Teil = teil; Absatz = absatz; Text = text; Marken = marken; }

            internal Teilinfo Teil { get; }
            internal Paragraph Absatz { get; }
            internal Text Text { get; }
            internal List<Platzhalter> Marken { get; }
        }

        /// <summary>Ein Inhaltssteuerelement, dessen Tag ein Platzhalter ist.</summary>
        private sealed class Sdtstelle
        {
            internal Sdtstelle(SdtElement sdt, Teilinfo teil, Platzhalter marke)
            { Sdt = sdt; Teil = teil; Marke = marke; }

            internal SdtElement Sdt { get; }
            internal Teilinfo Teil { get; }
            internal Platzhalter Marke { get; }
        }

        /// <summary>Wo ein Platzhalter steht — für die Ortsregeln (Konzept 4.3).</summary>
        private sealed class Ortsangabe
        {
            internal Teilart Teil;
            internal bool InZelle;
            internal bool InTextfeld;

            /// <summary>Liste (und Tabelle): Rumpf, Tabellenzelle, Block-Inhaltssteuerelement.</summary>
            internal bool ErlaubtListe { get { return Teil == Teilart.Rumpf && !InTextfeld; } }

            /// <summary>Kapitel: Rumpf und Block-Inhaltssteuerelement, keine Tabellenzelle.</summary>
            internal bool ErlaubtKapitel { get { return Teil == Teilart.Rumpf && !InTextfeld && !InZelle; } }
        }

        /// <summary>Was mit einem Platzhalter geschieht.</summary>
        private sealed class Entscheid
        {
            internal static readonly Entscheid Stehen = new Entscheid { Art = Entscheidart.Stehen };
            internal Entscheidart Art;
            internal string Text = "";
            internal IReadOnlyList<string> Zeilen = Array.Empty<string>();

            /// <summary>Die Namen der Kapitel (<see cref="Berichtskapitel.Name"/>), die an die Stelle kommen.</summary>
            internal IReadOnlyList<string> Kapitel = Array.Empty<string>();

            /// <summary>Die Formatangaben des Kapitelplatzhalters (<c>|ohne titel</c>, <c>|ebene n</c>).</summary>
            internal IReadOnlyList<Formatangabe> Angaben = Array.Empty<Formatangabe>();

            /// <summary>Ein einzelnes Kapitel (<c>kapitel.&lt;name&gt;</c>), nicht der Sammelanker.</summary>
            internal bool Einzeln;

            /// <summary>Die Strukturtabelle an der Stelle (BV-E5).</summary>
            internal Berichtstabelle Tabelle;
            /// <summary>Ein Bild allein im Absatz (BV-E5): der Eintrag, seine Marke und der Wertesatz der Stelle.</summary>
            internal Vorlagenfeld Feld;
            internal Platzhalter Marke;
            internal Berichtswerte Werte;
        }

        /// <summary>Ein Bild, dessen Alternativtext ein Platzhalter ist (Konzept 4.2, 6.5).</summary>
        private sealed class Bildstelle
        {
            internal Bildstelle(DW.DocProperties docPr, Teilinfo teil, Platzhalter marke)
            { DocPr = docPr; Teil = teil; Marke = marke; }

            internal DW.DocProperties DocPr { get; }
            internal Teilinfo Teil { get; }
            internal Platzhalter Marke { get; }
        }

        /// <summary>Ein Stück des neuen Textes: Wortlaut (Vorlage oder Wert) oder ein stehen gebliebener Platzhalter.</summary>
        private readonly struct Stueck
        {
            internal Stueck(string text, bool markiert) { Text = text ?? ""; Markiert = markiert; }
            internal string Text { get; }
            internal bool Markiert { get; }
        }

        private sealed partial class Lauf
        {
            private readonly WordprocessingDocument _doc;
            private readonly MainDocumentPart _main;
            private readonly BerichtsDaten _daten;
            private readonly BerichtsKonfiguration _konfig;
            private readonly Erstellerangaben _ersteller;
            private readonly bool _englisch;
            private readonly Fuellergebnis _ergebnis;
            private readonly WordVorlagenstile _stile;
            private readonly List<Teilinfo> _teile = new List<Teilinfo>();
            private readonly List<Sdtstelle> _sdts = new List<Sdtstelle>();
            private readonly HashSet<OpenXmlElement> _sdtMenge = new HashSet<OpenXmlElement>();
            private readonly Dictionary<Paragraph, int> _absatzNummer = new Dictionary<Paragraph, int>();
            private readonly Dictionary<Table, int> _tabellenNummer = new Dictionary<Table, int>();
            private readonly List<Bildstelle> _bildstellen = new List<Bildstelle>();

            /// <summary>Je Kapitel (Name) die Stelle, die es füllt — die erste gültige (Konzept 5.3).</summary>
            private readonly Dictionary<string, OpenXmlElement> _kapitelOrt = new Dictionary<string, OpenXmlElement>(StringComparer.Ordinal);

            /// <summary>Je Kapitel (Name) seine Marke an dieser Stelle (Formatangaben).</summary>
            private readonly Dictionary<string, Platzhalter> _kapitelMarke = new Dictionary<string, Platzhalter>(StringComparer.Ordinal);

            /// <summary>Die Stelle, die der Sammelanker füllt; <c>null</c> ohne.</summary>
            private OpenXmlElement _sammelOrt;

            /// <summary>Die Überschrift vor jedem Kapitel im Bericht (Stelle der Anhang-E-Checkliste).</summary>
            private IReadOnlyDictionary<string, string> _kapitelstellen;

            private bool _logoGewarnt;
            private Berichtswerte _werte;

            internal Lauf(WordprocessingDocument doc, BerichtsDaten daten, BerichtsKonfiguration konfig,
                          Erstellerangaben ersteller, bool englisch, Fuellergebnis ergebnis)
            {
                _doc = doc;
                _main = doc.MainDocumentPart;
                _daten = daten;
                _konfig = konfig;
                _ersteller = ersteller;
                _englisch = englisch;
                _ergebnis = ergebnis;
                _stile = new WordVorlagenstile(_main);
            }

            internal void Fuehre()
            {
                SammleTeile();

                // Gezählt wie im Prüfer (Vorlagenteile.Kommentarzahl) — Prüfzeile und Laufmeldung nennen
                // dieselbe Zahl.
                int kommentare = WordVorlagenbereinigung.EntferneKommentare(_main, _teile.Select(t => t.Wurzel));
                _ergebnis.EntfernteKommentare = kommentare;
                if (kommentare > 0) _ergebnis.Hinweis(T.F(_englisch, T.KOMMENTARE, kommentare));
                WordVorlagenbereinigung.EntferneExterneBeziehungen(_doc, _ergebnis, _englisch);

                SammleSdts();
                foreach (Teilinfo ti in _teile)
                    foreach (Paragraph p in ti.Wurzel.Descendants<Paragraph>().ToList())
                        if (!InPlatzhalterSdt(p)) WordVorlagennormalisierer.NormalisiereAbsatz(p);

                if (!HatPlatzhalter()) SetzeSammelankerAnsEnde();

                // BV-E5: die Mustertabelle vor den Blöcken lesen und entfernen — keine Wiederholung klont sie.
                LiesMustertabellen();
                Nummeriere();
                _werte = Berichtswerte.Aus(_daten, _konfig, _englisch, _ersteller);
                ExpandiereBloecke();

                List<Textstelle> stellen = SammleTextstellen();
                SammleBildstellen();
                SammleKapitelstellen(stellen);
                _ergebnis.Kapitelstellen = _kapitelstellen;
                _werte.Kapitelstellen = _kapitelstellen;   // BV-E5: die Spalte „Stelle“ der Anhang-E-Tabelle

                foreach (Textstelle stelle in stellen) Ersetze(stelle);
                foreach (Sdtstelle stelle in _sdts) FuelleSdt(stelle);
                MeldeUebrigeBlocksdts();
                foreach (Bildstelle stelle in _bildstellen) FuelleBild(stelle);

                foreach (string name in _stile.Angelegt) _ergebnis.Hinweis(T.F(_englisch, T.STIL_ANGELEGT, name));
                PruefeFelder();
                VergibBildkennungen();
            }

            // ------------------------------------------------------------- Teile

            private void SammleTeile()
            {
                Body rumpf = _main.Document.Body;
                _teile.Add(new Teilinfo(_main, rumpf, Teilart.Rumpf, 0, null));

                // Welcher Abschnitt verweist auf welche Kopf- und Fußzeile (erster Verweis gilt).
                var verweise = new Dictionary<string, (int Abschnitt, string Typ)>(StringComparer.Ordinal);
                int nummer = 0;
                foreach (SectionProperties abschnitt in rumpf.Descendants<SectionProperties>())
                {
                    nummer++;
                    foreach (HeaderReference h in abschnitt.Elements<HeaderReference>())
                        if (h.Id?.Value != null && !verweise.ContainsKey("h:" + h.Id.Value))
                            verweise["h:" + h.Id.Value] = (nummer, Typ(h.Type?.Value));
                    foreach (FooterReference f in abschnitt.Elements<FooterReference>())
                        if (f.Id?.Value != null && !verweise.ContainsKey("f:" + f.Id.Value))
                            verweise["f:" + f.Id.Value] = (nummer, Typ(f.Type?.Value));
                }

                foreach (HeaderPart kopf in _main.HeaderParts)
                {
                    if (kopf.Header == null) continue;
                    verweise.TryGetValue("h:" + _main.GetIdOfPart(kopf), out var v);
                    _teile.Add(new Teilinfo(kopf, kopf.Header, Teilart.Kopfzeile, v.Abschnitt, v.Typ));
                }
                foreach (FooterPart fuss in _main.FooterParts)
                {
                    if (fuss.Footer == null) continue;
                    verweise.TryGetValue("f:" + _main.GetIdOfPart(fuss), out var v);
                    _teile.Add(new Teilinfo(fuss, fuss.Footer, Teilart.Fusszeile, v.Abschnitt, v.Typ));
                }
                if (_main.FootnotesPart?.Footnotes != null)
                    _teile.Add(new Teilinfo(_main.FootnotesPart, _main.FootnotesPart.Footnotes, Teilart.Fussnoten, 0, null));
                if (_main.EndnotesPart?.Endnotes != null)
                    _teile.Add(new Teilinfo(_main.EndnotesPart, _main.EndnotesPart.Endnotes, Teilart.Endnoten, 0, null));
            }

            private static string Typ(HeaderFooterValues? typ)
            {
                if (typ == HeaderFooterValues.First) return "first";
                if (typ == HeaderFooterValues.Even) return "even";
                return "default";
            }

            private void Nummeriere()
            {
                foreach (Teilinfo ti in _teile)
                {
                    int n = 0;
                    foreach (Paragraph p in ti.Wurzel.Descendants<Paragraph>()) _absatzNummer[p] = ++n;
                    int t = 0;
                    foreach (Table tab in ti.Wurzel.Descendants<Table>()) _tabellenNummer[tab] = ++t;
                }
            }

            // ------------------------------------------------------------- Inhaltssteuerelemente finden

            private void SammleSdts()
            {
                foreach (Teilinfo ti in _teile)
                    foreach (SdtElement sdt in ti.Wurzel.Descendants<SdtElement>().ToList())
                    {
                        Platzhalter marke = MarkeAusTag(sdt);
                        if (marke == null) continue;
                        // Ein Block-Steuerelement (Tag „#je stand“, „#wenn …“) ist ein Wiederhol- oder
                        // Bedingungsabschnitt: Sein Inhalt wird normalisiert und gefüllt wie der Rumpf.
                        if (marke.IstBlockmarke)
                        {
                            _blockSdts.Add(new Sdtstelle(sdt, ti, marke));
                            continue;
                        }
                        _sdts.Add(new Sdtstelle(sdt, ti, marke));
                        _sdtMenge.Add(sdt);
                    }
            }

            /// <summary>
            /// Der Platzhalter im <c>w:tag</c> eines Inhaltssteuerelements: in doppelten Klammern, als
            /// Blockmarke oder als gültiger Schlüssel mit Punkt (alle Katalogschlüssel tragen einen) —
            /// ein Tag wie „Kunde“ gehört dem Anwender und bleibt unberührt.
            /// </summary>
            private static Platzhalter MarkeAusTag(SdtElement sdt)
            {
                string tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                if (string.IsNullOrWhiteSpace(tag)) return null;
                Platzhalter marke = Platzhaltersyntax.Lies(tag);
                if (tag.Trim().StartsWith("{{", StringComparison.Ordinal)) return marke;
                if (marke.IstBlockmarke) return marke;
                if (marke.Art == Platzhalterart.Feld && marke.SchluesselGueltig && marke.Schluessel.IndexOf('.') > 0)
                    return marke;
                return null;
            }

            private bool InPlatzhalterSdt(OpenXmlElement e)
            {
                return _sdtMenge.Count > 0 && e.Ancestors<SdtElement>().Any(s => _sdtMenge.Contains(s));
            }

            // ------------------------------------------------------------- Ohne Platzhalter

            /// <summary>Trägt die Vorlage einen Platzhalter — im Text, als Tag oder im Alternativtext eines
            /// Bildes (wie der Prüfer zählt)?</summary>
            private bool HatPlatzhalter()
            {
                if (_sdts.Count > 0 || _blockSdts.Count > 0) return true;
                return _teile.Any(ti => ti.Wurzel.Descendants<Paragraph>()
                                          .Any(p => Platzhaltersyntax.EnthaeltPlatzhalter(p.InnerText)) ||
                                        ti.Wurzel.Descendants<DW.DocProperties>()
                                          .Any(d => Vorlagenpruefer.IstBildschluessel(d.Description?.Value)));
            }

            // ------------------------------------------------------------- Kapitelstellen (Konzept 5.3)

            /// <summary>
            /// Sammelt die Stellen der Kapitel VOR dem Füllen: Je Kapitel gilt die erste gültige Stelle —
            /// allein im Absatz des Rumpfs oder als Inhaltssteuerelement auf Blockebene, nicht in Zelle
            /// oder Textfeld; getippte Platzhalter in Dokumentfolge, danach die Steuerelemente. Jede
            /// weitere Stelle desselben Kapitels bleibt beim Füllen gelb stehen. Danach die Überschrift
            /// vor jedem Anker (<see cref="BerechneKapitelstellen"/>).
            /// </summary>
            private void SammleKapitelstellen(List<Textstelle> stellen)
            {
                foreach (Textstelle s in stellen)
                {
                    if (s.Teil.Art != Teilart.Rumpf || s.Marken.Count != 1) continue;
                    if (!OrtVon(s.Absatz, s.Teil).ErlaubtKapitel || !IstAllein(s.Absatz, s.Marken[0])) continue;
                    Beanspruche(s.Marken[0], s.Absatz);
                }
                foreach (Sdtstelle s in _sdts)
                    if (s.Sdt is SdtBlock && s.Teil.Art == Teilart.Rumpf && OrtVon(s.Sdt, s.Teil).ErlaubtKapitel)
                        Beanspruche(s.Marke, s.Sdt);
                _kapitelstellen = BerechneKapitelstellen();
            }

            private void Beanspruche(Platzhalter m, OpenXmlElement bezug)
            {
                if (m == null || m.Art != Platzhalterart.Feld) return;
                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(m.Schluessel);
                if (feld == null || feld.Art != Vorlagenfeldart.Kapitel) return;
                if (feld.Schluessel == SAMMELANKER)
                {
                    _sammelOrt ??= bezug;
                    return;
                }
                Berichtskapitel k = Berichtskapitel.Finde(feld.Schluessel);
                if (k == null || _kapitelOrt.ContainsKey(k.Name)) return;
                _kapitelOrt[k.Name] = bezug;
                _kapitelMarke[k.Name] = m;
            }

            /// <summary>Füllt diese Stelle ihr Kapitel — oder steht dasselbe Kapitel schon früher?</summary>
            private bool IstBeansprucht(Vorlagenfeld feld, OpenXmlElement bezug)
            {
                if (feld.Schluessel == SAMMELANKER) return ReferenceEquals(_sammelOrt, bezug);
                Berichtskapitel k = Berichtskapitel.Finde(feld.Schluessel);
                return k != null && _kapitelOrt.TryGetValue(k.Name, out OpenXmlElement ort) && ReferenceEquals(ort, bezug);
            }

            /// <summary>
            /// <b>Die Stelle jedes Kapitels im Bericht</b> (Konzept 11 Nr. 3), je
            /// <see cref="Berichtskapitel.Stellenschluessel"/>: der Kapitelkopf unmittelbar vor dem Anker
            /// (mit <c>|ohne titel</c> sonst die nächste Überschrift davor), ohne ihn die eigene
            /// Überschrift des Bausteins; über den Sammelanker die eigene Überschrift. <c>null</c>, wenn
            /// die Vorlage das Kapitel nicht führt oder sein Häkchen fehlt. Das Deckblatt steht auch dann
            /// im Bericht, wenn die Vorlage Deckblattangaben aus Platzhaltern trägt.
            /// </summary>
            private IReadOnlyDictionary<string, string> BerechneKapitelstellen()
            {
                string kopfstil = _stile.Finde(WordVorlagenstile.KAPITELKOPF);
                HashSet<string> ueberschriften = Berichtskapitel.UeberschriftIds(_stile);
                var stellen = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Berichtskapitel k in Berichtskapitel.Alle)
                {
                    string text = null;
                    if (k.IstAktiv(_konfig))
                    {
                        if (_kapitelOrt.TryGetValue(k.Name, out OpenXmlElement bezug))
                            text = Kopftext(k, bezug, _kapitelMarke[k.Name], kopfstil, ueberschriften);
                        else if (_sammelOrt != null)
                            text = k.Ueberschrift(_englisch);
                    }
                    if (text == null && k.Name == Berichtskapitel.DECKBLATT && DeckblattImRumpf())
                        text = k.Ueberschrift(_englisch);
                    stellen[k.Stellenschluessel] = text;
                }
                return stellen;
            }

            /// <summary>Die Überschrift vor dem Anker eines einzeln geführten Kapitels.</summary>
            private string Kopftext(Berichtskapitel k, OpenXmlElement bezug, Platzhalter m, string kopfstil,
                                    HashSet<string> ueberschriften)
            {
                OpenXmlElement kopf = Berichtskapitel.KapitelkopfVor(bezug, kopfstil);
                if (kopf == null && m.Angaben.Any(a => a.Art == Formatangabeart.OhneTitel))
                    kopf = Berichtskapitel.UeberschriftVor(bezug, ueberschriften);
                if (kopf is Paragraph p)
                {
                    string text = Vorlagenfeldkatalog.LoeseImText(string.Concat(EigeneTexte(p).Select(t => t.Text)), _werte).Trim();
                    if (text.Length > 0) return text;
                }
                return k.Ueberschrift(_englisch);
            }

            /// <summary>Trägt der Rumpf Deckblattangaben aus Platzhaltern (was das Kapitel Deckblatt deckt)?</summary>
            private bool DeckblattImRumpf()
            {
                ISet<string> angaben = Vorlagenfeldkatalog.Deckblattangaben;
                Teilinfo rumpf = _teile[0];
                foreach (Paragraph p in rumpf.Wurzel.Descendants<Paragraph>())
                    foreach (Platzhalter m in Platzhaltersyntax.Finde(p.InnerText))
                        if (m.Art == Platzhalterart.Feld && angaben.Contains(Vorlagenfeldkatalog.Finde(m.Schluessel)?.Schluessel ?? ""))
                            return true;
                return _sdts.Any(s => s.Teil.Art == Teilart.Rumpf && s.Marke.Art == Platzhalterart.Feld &&
                                      angaben.Contains(Vorlagenfeldkatalog.Finde(s.Marke.Schluessel)?.Schluessel ?? ""));
            }

            /// <summary>Konzept 6.1: ohne jeden Platzhalter kommen die Kapitel ans Ende des Rumpfs, vor
            /// die letzte Abschnittsangabe — nichts wird gelöscht.</summary>
            private void SetzeSammelankerAnsEnde()
            {
                Body rumpf = _main.Document.Body;
                var anker = new Paragraph(new Run(new Text("{{" + SAMMELANKER + "}}")));
                SectionProperties letzte = rumpf.Elements<SectionProperties>().LastOrDefault();
                if (letzte != null) rumpf.InsertBefore(anker, letzte);
                else rumpf.AppendChild(anker);
                _ergebnis.OhnePlatzhalter = true;
                _ergebnis.Warnung(T.T(T.OHNE_PLATZHALTER, _englisch));
            }

            // ------------------------------------------------------------- Textplatzhalter

            private List<Textstelle> SammleTextstellen()
            {
                var stellen = new List<Textstelle>();
                foreach (Teilinfo ti in _teile)
                    foreach (Paragraph p in ti.Wurzel.Descendants<Paragraph>().ToList())
                    {
                        if (InPlatzhalterSdt(p)) continue;
                        foreach (Text t in EigeneTexte(p))
                        {
                            if (t.Text.IndexOf("{{", StringComparison.Ordinal) < 0 || InPlatzhalterSdt(t)) continue;
                            List<Platzhalter> marken = Platzhaltersyntax.Finde(t.Text).ToList();
                            if (marken.Count > 0) stellen.Add(new Textstelle(ti, p, t, marken));
                        }
                    }
                return stellen;
            }

            private void Ersetze(Textstelle s)
            {
                if (!Haengt(s.Text, s.Teil.Wurzel)) return;
                Ortsangabe ort = OrtVon(s.Absatz, s.Teil);
                bool allein = s.Marken.Count == 1 && IstAllein(s.Absatz, s.Marken[0]);
                List<SdtElement> huellen = s.Text.Ancestors<SdtElement>().ToList();

                string text = s.Text.Text;
                var stuecke = new List<Stueck>();
                bool ersetzt = false;
                int pos = 0;
                foreach (Platzhalter m in s.Marken)
                {
                    if (m.Position > pos) stuecke.Add(new Stueck(text.Substring(pos, m.Position - pos), false));
                    Entscheid e = Entscheide(m, ort, allein, s.Absatz, s.Teil, SdtForm.Keine);
                    switch (e.Art)
                    {
                        case Entscheidart.Text:
                            stuecke.Add(new Stueck(e.Text, false));
                            ersetzt = true;
                            break;
                        case Entscheidart.Liste:
                            ErsetzeAbsatz(s.Absatz, Listenabsaetze(s.Absatz, (s.Text.Parent as Run)?.RunProperties, e.Zeilen));
                            Entbinde(huellen);
                            return;
                        case Entscheidart.Kapitel:
                            FuelleKapitel(s.Absatz, s.Teil, e);
                            Entbinde(huellen);
                            return;
                        case Entscheidart.Tabelle:
                            FuelleTabelle(s.Absatz, s.Teil, e);
                            Entbinde(huellen);
                            return;
                        case Entscheidart.Bild:
                            FuelleDiagrammImAbsatz(s.Absatz, s.Teil, e);
                            Entbinde(huellen);
                            return;
                        default:
                            stuecke.Add(new Stueck(m.Roh, true));
                            break;
                    }
                    pos = m.Position + m.Laenge;
                }
                if (pos < text.Length) stuecke.Add(new Stueck(text.Substring(pos), false));
                SchreibeStuecke(s.Text, stuecke);
                if (ersetzt) Entbinde(huellen);
            }

            /// <summary>
            /// Ein Inhaltssteuerelement des Anwenders (Tag kein Schlüssel), in dem ein getippter
            /// Platzhalter gefüllt wurde, bleibt — aber ohne <c>w:showingPlcHdr</c> (Word zeigte den Wert
            /// sonst grau als leeren Platzhalter), <c>w:temporary</c> und <c>w:dataBinding</c> (Word
            /// überschriebe den Wert beim Öffnen mit dem gebundenen Dokumenteigenschaftswert; Konzept 6.6).
            /// </summary>
            private static void Entbinde(IEnumerable<SdtElement> huellen)
            {
                foreach (SdtElement sdt in huellen)
                {
                    SdtProperties eigenschaften = sdt.SdtProperties;
                    if (eigenschaften == null) continue;
                    eigenschaften.RemoveAllChildren<ShowingPlaceholder>();
                    eigenschaften.RemoveAllChildren<TemporarySdt>();
                    eigenschaften.RemoveAllChildren<DataBinding>();
                    eigenschaften.RemoveAllChildren<DocumentFormat.OpenXml.Office2013.Word.DataBinding>();
                }
            }

            // ------------------------------------------------------------- Entscheiden und Auflösen

            /// <summary>
            /// Was mit einer Marke geschieht (Konzept 4.3, 4.10): Text, Liste, Kapitel oder stehen
            /// lassen. Befunde und Fehler landen im Ergebnis, ersetzte und leere Werte werden gezählt.
            /// </summary>
            private Entscheid Entscheide(Platzhalter m, Ortsangabe ort, bool allein, OpenXmlElement bezug,
                                         Teilinfo ti, SdtForm form)
            {
                if (m.IstBlockmarke) return Stehen(m, Fuellbefundart.Block, bezug, ti, null);
                if (m.Art != Platzhalterart.Feld) return Stehen(m, Fuellbefundart.Unbekannt, bezug, ti, null);
                Berichtswerte werte = WerteVon(bezug);

                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(m.Schluessel);
                if (feld == null)
                    return Stehen(m, IstSpaeterBereich(m.Schluessel) ? Fuellbefundart.NichtUnterstuetzt : Fuellbefundart.Unbekannt,
                                  bezug, ti, null);
                // Konzept 4.7: ein Wert je Stand nur im Standblock (außer dem Paarvergleich), je Gebäude
                // nur im Gebäudeblock.
                if (feld.Kontext == Vorlagenfeldkontext.Stand && !werte.ImStandblock && !IstPaarschluessel(feld.Schluessel))
                    return Stehen(m, Fuellbefundart.Kontext, bezug, ti, null);
                if (feld.Kontext == Vorlagenfeldkontext.Gebaeude && !werte.ImGebaeudeblock)
                    return Stehen(m, Fuellbefundart.Kontext, bezug, ti, null);
                if ((feld.Ausgaben & Vorlagenausgabe.Word) == 0)
                    return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, null);

                switch (feld.Art)
                {
                    case Vorlagenfeldart.Text:
                    case Vorlagenfeldart.Zahl:
                    case Vorlagenfeldart.Datum:
                        _ergebnis.Ersetzt++;
                        return new Entscheid { Art = Entscheidart.Text, Text = Loese(feld, m, werte).Text };

                    case Vorlagenfeldart.Liste:
                        {
                            if (form == SdtForm.ImSatz) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.SDT_IM_SATZ);
                            if (!ort.ErlaubtListe) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.LISTE_ORT);
                            Platzhalterwert w = Loese(feld, m, werte);
                            _ergebnis.Ersetzt++;
                            if (allein) return new Entscheid { Art = Entscheidart.Liste, Zeilen = w.Zeilen };
                            return new Entscheid
                            {
                                Art = Entscheidart.Text,
                                Text = w.Zeilen.Count > 0 ? string.Join(LISTENTRENNER, w.Zeilen) : w.Text,
                            };
                        }

                    case Vorlagenfeldart.Kapitel:
                        {
                            if (form == SdtForm.ImSatz) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.SDT_IM_SATZ);
                            if (!allein || !ort.ErlaubtKapitel) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.KAPITEL_ORT);
                            // Ein Kapitel zweimal: die erste Stelle gilt, jede weitere bleibt gelb stehen.
                            if (!IstBeansprucht(feld, bezug)) return Stehen(m, Fuellbefundart.Doppelt, bezug, ti, T.KAPITEL_DOPPELT);
                            Platzhalterwert w = Loese(feld, m, werte);
                            _ergebnis.Ersetzt++;
                            bool einzeln = feld.Schluessel != SAMMELANKER;
                            // Der Sammelanker setzt die angehakten Kapitel ohne die einzeln geführten ein.
                            IReadOnlyList<string> kapitel = einzeln ? w.Kapitel
                                : w.Kapitel.Where(n => !_kapitelOrt.ContainsKey(n)).ToList();
                            return new Entscheid { Art = Entscheidart.Kapitel, Kapitel = kapitel, Angaben = m.Angaben, Einzeln = einzeln };
                        }

                    case Vorlagenfeldart.Schalter:
                        // Ein Schalter wirkt nur als Bedingung {{#wenn …}} (Konzept 4.3).
                        return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, null);

                    case Vorlagenfeldart.Tabelle:
                        // BV-E5: die Strukturtabelle allein im Absatz (WordVorlagentabellen.cs).
                        return EntscheideTabelle(m, feld, ort, allein, bezug, ti, form, werte);

                    case Vorlagenfeldart.Bild:
                        {
                            // BV-E5 (Konzept 4.2): ein Diagramm als Text allein im Absatz oder als Block-Steuerelement —
                            // das Bild in Satzspiegelbreite; im Satz ein Fehler. Das Logo gibt es nur als Alternativtext.
                            if (string.Equals(feld.Schluessel, Vorlagenfeldkatalog.LOGO, StringComparison.Ordinal))
                                return Stehen(m, Fuellbefundart.NichtUnterstuetzt, bezug, ti, null);
                            if (form == SdtForm.ImSatz) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.SDT_IM_SATZ);
                            if (!allein) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.BILD_IM_SATZ);
                            if (ti.Art == Teilart.Fussnoten || ti.Art == Teilart.Endnoten)
                                return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.BILD_ORT);
                            return new Entscheid { Art = Entscheidart.Bild, Feld = feld, Marke = m, Werte = werte };
                        }

                    default:
                        // Blatt: spätere Etappen.
                        return Stehen(m, Fuellbefundart.NichtUnterstuetzt, bezug, ti, null);
                }
            }

            /// <summary>Stand- und Gebäudewerte ohne Katalogeintrag: Der Katalog führt sie schrittweise
            /// (BV-E4) — kein Tippfehler, sondern noch nicht da.</summary>
            private static bool IstSpaeterBereich(string schluessel)
            {
                return schluessel.StartsWith("stand.", StringComparison.Ordinal) ||
                       schluessel.StartsWith("gebaeude.", StringComparison.Ordinal);
            }

            private Entscheid Stehen(Platzhalter m, Fuellbefundart art, OpenXmlElement bezug, Teilinfo ti, string fehlermuster)
            {
                string fundort = Fundort(bezug, ti);
                string grund = art == Fuellbefundart.Unbekannt ? T.GRUND_UNBEKANNT
                             : art == Fuellbefundart.NichtUnterstuetzt ? T.GRUND_NICHT_UNTERSTUETZT
                             : art == Fuellbefundart.Doppelt ? T.GRUND_DOPPELT
                             : art == Fuellbefundart.Block ? T.GRUND_BLOCK
                             : art == Fuellbefundart.Kontext ? T.GRUND_KONTEXT
                             : T.GRUND_FALSCHE_STELLE;
                _ergebnis.Unbekannt(new Fuellbefund(m.Normalform, fundort, art, T.T(grund, _englisch)));
                if (fehlermuster != null) _ergebnis.Fehlermeldung(T.F(_englisch, fehlermuster, m.Normalform, fundort));
                return Entscheid.Stehen;
            }

            /// <summary>
            /// Löst auf; eine Ausnahme, die der Katalog nicht selbst fängt, wird hier zum Leerwert
            /// „—“ mit Warnung. Leere Werte werden je Schlüssel gezählt.
            /// </summary>
            private Platzhalterwert Loese(Vorlagenfeld feld, Platzhalter m, Berichtswerte werte = null)
            {
                werte ??= _werte;
                Platzhalterwert w;
                try
                {
                    w = Vorlagenfeldkatalog.Loese(feld, werte, m.Angaben);
                }
                catch (Exception ex)
                {
                    w = Platzhalterwert.Leer(feld.Art, Vorlagenfeld.STRICH,
                                             _werte.Text(nameof(MyResource.Resource.BV_GRUND_AUSNAHME)), ex.Message);
                }
                _ergebnis.Aufgeloest(feld.Schluessel, feld.Kontext);
                if (w.IstLeer) _ergebnis.Leer(feld.Schluessel);
                if (w.Ausnahme != null) _ergebnis.Warnung(T.F(_englisch, T.AUSNAHME, feld.Schluessel, w.Ausnahme));
                return w;
            }

            // ------------------------------------------------------------- Schreiben im Satz

            /// <summary>
            /// Ersetzt den <c>w:t</c> durch die Stücke. Ohne stehen gebliebene Marke bleibt alles im
            /// selben Run (Format bleibt; Zeilenumbrüche als <c>w:br</c>); sonst wird der Run geteilt,
            /// und jede stehen gebliebene Marke bekommt einen eigenen Run mit gelber Hervorhebung.
            /// </summary>
            private static void SchreibeStuecke(Text t, List<Stueck> stuecke)
            {
                if (!(t.Parent is Run lauf))
                {
                    t.Text = string.Concat(stuecke.Select(s => s.Text));
                    t.Space = SpaceProcessingModeValues.Preserve;
                    return;
                }

                if (!stuecke.Any(s => s.Markiert))
                {
                    foreach (OpenXmlElement e in TextElemente(string.Concat(stuecke.Select(s => s.Text))))
                        lauf.InsertBefore(e, t);
                    t.Remove();
                    return;
                }

                Run allein = Isoliere(lauf, t);
                RunProperties format = allein.RunProperties;
                var neue = new List<Run>();
                var puffer = new StringBuilder();
                foreach (Stueck s in stuecke)
                {
                    if (!s.Markiert) { puffer.Append(s.Text); continue; }
                    if (puffer.Length > 0) { neue.Add(NeuerLauf(format, TextElemente(puffer.ToString()), false)); puffer.Clear(); }
                    neue.Add(NeuerLauf(format, new OpenXmlElement[] { Textelement(s.Text) }, true));
                }
                if (puffer.Length > 0) neue.Add(NeuerLauf(format, TextElemente(puffer.ToString()), false));
                foreach (Run r in neue) allein.InsertBeforeSelf(r);
                allein.Remove();
            }

            /// <summary>Teilt den Run so, dass <paramref name="t"/> allein (mit dem Zeichenformat) darin steht.</summary>
            private static Run Isoliere(Run lauf, Text t)
            {
                List<OpenXmlElement> kinder = lauf.ChildElements.Where(c => !(c is RunProperties)).ToList();
                int i = kinder.IndexOf(t);
                List<OpenXmlElement> vor = kinder.Take(i).ToList();
                List<OpenXmlElement> nach = kinder.Skip(i + 1).ToList();
                if (vor.Count > 0)
                {
                    Run r = LeererLauf(lauf.RunProperties);
                    foreach (OpenXmlElement k in vor) { k.Remove(); r.AppendChild(k); }
                    lauf.InsertBeforeSelf(r);
                }
                if (nach.Count > 0)
                {
                    Run r = LeererLauf(lauf.RunProperties);
                    foreach (OpenXmlElement k in nach) { k.Remove(); r.AppendChild(k); }
                    lauf.InsertAfterSelf(r);
                }
                return lauf;
            }

            private static Run LeererLauf(RunProperties format)
            {
                var r = new Run();
                if (format != null) r.RunProperties = (RunProperties)format.CloneNode(true);
                return r;
            }

            /// <summary>Ein Run im Format <paramref name="format"/>; hervorgehoben gelb (Konzept 4.10).</summary>
            private static Run NeuerLauf(RunProperties format, IEnumerable<OpenXmlElement> inhalt, bool hervorgehoben)
            {
                Run r = LeererLauf(format);
                if (hervorgehoben)
                {
                    if (r.RunProperties == null) r.RunProperties = new RunProperties();
                    r.RunProperties.Highlight = new Highlight { Val = HighlightColorValues.Yellow };
                }
                foreach (OpenXmlElement e in inhalt) r.AppendChild(e);
                return r;
            }

            /// <summary>Text als <c>w:t</c>-Folge, Zeilenumbrüche als <c>w:br</c> (Konzept 4.6, BV-P1).</summary>
            private static List<OpenXmlElement> TextElemente(string text)
            {
                string[] zeilen = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                var elemente = new List<OpenXmlElement>();
                for (int i = 0; i < zeilen.Length; i++)
                {
                    if (i > 0) elemente.Add(new Break());
                    if (zeilen[i].Length > 0 || zeilen.Length == 1) elemente.Add(Textelement(zeilen[i]));
                }
                return elemente;
            }

            private static Text Textelement(string text)
            {
                return new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve };
            }

            // ------------------------------------------------------------- Absatzplatzhalter

            /// <summary>Die Einträge einer Liste als Absätze im Format des Platzhalterabsatzes.</summary>
            private static List<OpenXmlElement> Listenabsaetze(OpenXmlElement vorbild, RunProperties zeichen, IReadOnlyList<string> zeilen)
            {
                Paragraph muster = vorbild as Paragraph ?? vorbild.Descendants<Paragraph>().FirstOrDefault();
                var absaetze = new List<OpenXmlElement>();
                foreach (string zeile in zeilen)
                {
                    var p = new Paragraph();
                    ParagraphProperties format = OhneAbschnitt(muster?.ParagraphProperties);
                    if (format != null) p.AppendChild(format);
                    p.AppendChild(NeuerLauf(zeichen, TextElemente(zeile), false));
                    absaetze.Add(p);
                }
                return absaetze;
            }

            /// <summary>
            /// Das Kapitel am Anker (Konzept 4.8, 5.3): die Kapitel in Berichtsreihenfolge schreiben je über
            /// einen eigenen <see cref="WordKontext"/> vor <paramref name="bezug"/>; danach entfällt der
            /// Bezug. <c>|ohne titel</c> unterdrückt die eigene Überschrift des Bausteins, <c>|ebene n</c>
            /// rückt seine Überschriften tiefer. Steht ein einzelnes Kapitel mit <c>|ohne titel</c> unter
            /// einem Kapitelkopf, kommt, was der Baustein VOR seine Überschrift schreibt — der Seitenumbruch
            /// des Anhangs E —, vor den Kapitelkopf. Schreibt kein Baustein etwas (Häkchen ab, keine Daten),
            /// entfällt ein unmittelbar davor stehender „EPOS Kapitelkopf“ mit.
            /// </summary>
            private void FuelleKapitel(OpenXmlElement bezug, Teilinfo ti, Entscheid e)
            {
                bool ohneTitel = e.Angaben.Any(a => a.Art == Formatangabeart.OhneTitel);
                int ebene = e.Angaben.Where(a => a.Art == Formatangabeart.Ebene && a.Zahl.HasValue)
                                     .Select(a => a.Zahl.Value).DefaultIfEmpty(1).First();
                OpenXmlElement kopf = Berichtskapitel.KapitelkopfVor(bezug, _stile.Finde(WordVorlagenstile.KAPITELKOPF));

                int geschrieben = 0;
                var vorspann = new List<OpenXmlElement>();
                foreach (Berichtskapitel k in Berichtskapitel.Alle.Where(k => e.Kapitel.Contains(k.Name)))
                {
                    var kontext = new WordKontext(_main, Einfuegeanker.Vor(bezug, ti.Teil), _stile)
                    {
                        OhneTitel = ohneTitel,
                        Ebenenversatz = Math.Max(0, ebene - 1),
                        Kapitelstellen = _kapitelstellen,
                        Vorspann = e.Einzeln && ohneTitel && kopf != null ? Einfuegeanker.Vor(kopf, ti.Teil) : null,
                    };
                    k.NeuerBaustein().SchreibeWord(kontext, _daten, _konfig);
                    kontext.Abschliessen();
                    geschrieben += kontext.AmAnkerGeschrieben;
                    vorspann.AddRange(kontext.ImVorspann);
                }

                if (geschrieben == 0 && kopf != null)
                {
                    foreach (OpenXmlElement v in vorspann) v.Remove();
                    kopf.Remove();
                }
                ErsetzeAbsatz(bezug, new List<OpenXmlElement>());
            }

            /// <summary>
            /// Setzt <paramref name="neue"/> vor <paramref name="bezug"/> und entfernt ihn. Trägt er eine
            /// Abschnittsangabe, bleibt sie in einem leeren Absatz stehen; verlangt der Behälter einen
            /// Absatz (Zelle, Rumpf, Kopfzeile …), bleibt ein leerer.
            /// </summary>
            private static void ErsetzeAbsatz(OpenXmlElement bezug, List<OpenXmlElement> neue)
            {
                OpenXmlElement eltern = bezug.Parent;
                if (eltern == null) return;
                foreach (OpenXmlElement e in neue) eltern.InsertBefore(e, bezug);

                if (bezug is Paragraph absatz && absatz.ParagraphProperties?.SectionProperties != null)
                {
                    foreach (OpenXmlElement k in absatz.ChildElements.Where(c => !(c is ParagraphProperties)).ToList()) k.Remove();
                    return;
                }
                if (!(bezug is Paragraph))
                {
                    SectionProperties drin = bezug.Descendants<ParagraphProperties>()
                        .Select(pp => pp.SectionProperties).LastOrDefault(sp => sp != null);
                    if (drin != null)
                        eltern.InsertBefore(new Paragraph(new ParagraphProperties(drin.CloneNode(true))), bezug);
                }

                ParagraphProperties format = (bezug as Paragraph)?.ParagraphProperties
                                             ?? bezug.Descendants<Paragraph>().FirstOrDefault()?.ParagraphProperties;
                bezug.Remove();
                SichereAbsatz(eltern, format);
            }

            /// <summary>Wo Word einen Absatz verlangt, bleibt ein leerer: eine Zelle endet mit einem Absatz,
            /// Rumpf, Kopf- und Fußzeile, Fuß- und Endnote, Textfeld und Inhaltssteuerelement sind nie leer.</summary>
            private static void SichereAbsatz(OpenXmlElement eltern, ParagraphProperties format)
            {
                if (eltern is TableCell zelle)
                {
                    if (!(zelle.ChildElements.LastOrDefault(c => !(c is TableCellProperties)) is Paragraph))
                        zelle.AppendChild(LeererAbsatz(format));
                    return;
                }
                bool verlangt = eltern is Body || eltern is SdtContentBlock || eltern is TextBoxContent ||
                                eltern is Header || eltern is Footer || eltern is Footnote || eltern is Endnote;
                if (!verlangt) return;
                if (eltern.ChildElements.Any(c => c is Paragraph || c is Table || c is SdtBlock || c is CustomXmlBlock)) return;

                Paragraph leer = LeererAbsatz(format);
                SectionProperties abschnitt = eltern.Elements<SectionProperties>().FirstOrDefault();
                if (abschnitt != null) eltern.InsertBefore(leer, abschnitt);
                else eltern.AppendChild(leer);
            }

            private static Paragraph LeererAbsatz(ParagraphProperties format)
            {
                var p = new Paragraph();
                ParagraphProperties kopie = OhneAbschnitt(format);
                if (kopie != null) p.AppendChild(kopie);
                return p;
            }

            private static ParagraphProperties OhneAbschnitt(ParagraphProperties format)
            {
                if (format == null) return null;
                var kopie = (ParagraphProperties)format.CloneNode(true);
                kopie.RemoveAllChildren<SectionProperties>();
                return kopie;
            }

            // ------------------------------------------------------------- Inhaltssteuerelemente füllen

            /// <summary>
            /// Füllt ein Inhaltssteuerelement mit einem Schlüssel als Tag und packt es aus (Konzept 6.6,
            /// BV-Q14 a): Block für Text, Zahl, Datum, Liste und Kapitel, im Satz nur Text, Zahl, Datum.
            /// Danach steht der Inhalt ohne Steuerelement — kein <c>w:showingPlcHdr</c>, kein
            /// <c>w:dataBinding</c> bleibt zurück.
            /// </summary>
            private void FuelleSdt(Sdtstelle s)
            {
                if (!Haengt(s.Sdt, s.Teil.Wurzel)) return;
                Ortsangabe ort = OrtVon(s.Sdt, s.Teil);

                switch (s.Sdt)
                {
                    case SdtBlock block:
                        {
                            Entscheid e = Entscheide(s.Marke, ort, true, block, s.Teil, SdtForm.Block);
                            if (e.Art == Entscheidart.Text)
                                ErsetzeAbsatz(block, new List<OpenXmlElement> { Blockabsatz(block, e.Text) });
                            else if (e.Art == Entscheidart.Liste)
                                ErsetzeAbsatz(block, Listenabsaetze(block, SdtFormat(block), e.Zeilen));
                            else if (e.Art == Entscheidart.Kapitel)
                                FuelleKapitel(block, s.Teil, e);
                            else if (e.Art == Entscheidart.Tabelle)
                                FuelleTabelle(block, s.Teil, e);
                            else if (e.Art == Entscheidart.Bild)
                                FuelleDiagrammImAbsatz(block, s.Teil, e);
                            break;
                        }
                    case SdtRun imSatz:
                        {
                            Entscheid e = Entscheide(s.Marke, ort, false, imSatz, s.Teil, SdtForm.ImSatz);
                            if (e.Art == Entscheidart.Text)
                            {
                                imSatz.InsertBeforeSelf(NeuerLauf(SdtFormat(imSatz), TextElemente(e.Text), false));
                                imSatz.Remove();
                            }
                            break;
                        }
                    default:
                        Stehen(s.Marke, Fuellbefundart.NichtUnterstuetzt, s.Sdt, s.Teil, T.SDT_ZEILE);
                        break;
                }
            }

            /// <summary>Ein Absatz im Format des ersten Absatzes im Steuerelement, mit dem Wert.</summary>
            private Paragraph Blockabsatz(SdtBlock block, string text)
            {
                Paragraph erster = block.Descendants<Paragraph>().FirstOrDefault();
                var p = new Paragraph();
                ParagraphProperties format = OhneAbschnitt(erster?.ParagraphProperties);
                if (format != null) p.AppendChild(format);
                p.AppendChild(NeuerLauf(SdtFormat(block), TextElemente(text), false));
                return p;
            }

            /// <summary>
            /// Das Zeichenformat des Werts: das eigene des Steuerelements (<c>w:sdtPr/w:rPr</c>), sonst
            /// das seines ersten Runs — ohne das graue Format des Platzhaltertexts, das Word zeigt,
            /// solange das Steuerelement leer ist.
            /// </summary>
            private RunProperties SdtFormat(SdtElement sdt)
            {
                RunProperties eigen = sdt.SdtProperties?.GetFirstChild<RunProperties>();
                RunProperties format = eigen != null ? (RunProperties)eigen.CloneNode(true) : null;
                if (format == null)
                {
                    OpenXmlElement inhalt = sdt.ChildElements.FirstOrDefault(c => c.LocalName == "sdtContent");
                    RunProperties erster = inhalt?.Descendants<Run>().FirstOrDefault()?.RunProperties;
                    if (erster != null) format = (RunProperties)erster.CloneNode(true);
                }
                string stil = format?.RunStyle?.Val?.Value;
                if (stil != null && IstPlatzhaltertext(stil)) format.RemoveAllChildren<RunStyle>();
                return format != null && format.HasChildren ? format : null;
            }

            private bool IstPlatzhaltertext(string stilId)
            {
                if (string.Equals(stilId, "PlaceholderText", StringComparison.OrdinalIgnoreCase)) return true;
                Style stil = _main.StyleDefinitionsPart?.Styles?.Elements<Style>()
                    .FirstOrDefault(s => string.Equals(s.StyleId?.Value, stilId, StringComparison.Ordinal));
                return string.Equals(stil?.StyleName?.Val?.Value, "Placeholder Text", StringComparison.OrdinalIgnoreCase);
            }

            // ------------------------------------------------------------- Bildplatzhalter (Konzept 4.2, 6.5)

            /// <summary>Der Namensraum der Beziehungen (<c>r:embed</c>, <c>r:link</c>, <c>r:id</c>).</summary>
            private const string NS_BEZIEHUNG = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            /// <summary>Der Namensraum von <c>mc:AlternateContent</c>.</summary>
            private const string NS_MC = "http://schemas.openxmlformats.org/markup-compatibility/2006";

            /// <summary>
            /// Die Bilder, deren Alternativtext (<c>wp:docPr/@descr</c>) ein Platzhalter ist — in allen
            /// Teilen, gesammelt vor den Kapiteln (deren Bilder tragen keinen).
            /// </summary>
            private void SammleBildstellen()
            {
                foreach (Teilinfo ti in _teile)
                    foreach (DW.DocProperties d in ti.Wurzel.Descendants<DW.DocProperties>())
                    {
                        string beschreibung = d.Description?.Value;
                        if (!Vorlagenpruefer.IstBildschluessel(beschreibung)) continue;
                        string t = beschreibung.Trim();
                        Platzhalter marke = t.StartsWith("{{", StringComparison.Ordinal)
                            ? Platzhaltersyntax.Finde(t).FirstOrDefault()
                            : Platzhaltersyntax.Lies(t);
                        if (marke != null) _bildstellen.Add(new Bildstelle(d, ti, marke));
                    }
            }

            /// <summary>
            /// Füllt ein Platzhalterbild. <c>bild.ersteller.logo</c> bekommt das Logo der Einstellungen
            /// (Anwenderentscheid BV-E2-1), eingepasst in den Rahmen des Bildes; Lage, Umbruch und Rahmen
            /// bleiben. Ohne Logo entfällt das Bild samt Lauf — ein danach leerer Absatz auch, außer er
            /// steht allein in seinem Teil. Die Diagramme (BV-E5) füllt <see cref="FuelleDiagramm"/>.
            /// </summary>
            private void FuelleBild(Bildstelle b)
            {
                if (!Haengt(b.DocPr, b.Teil.Wurzel)) return;
                Platzhalter m = b.Marke;
                if (m.IstBlockmarke || m.Art != Platzhalterart.Feld)
                {
                    Stehen(m, Fuellbefundart.Unbekannt, b.DocPr, b.Teil, null);
                    return;
                }
                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(m.Schluessel);
                if (feld == null)
                {
                    Stehen(m, IstSpaeterBereich(m.Schluessel) ? Fuellbefundart.NichtUnterstuetzt : Fuellbefundart.Unbekannt,
                           b.DocPr, b.Teil, null);
                    return;
                }
                if (feld.Art != Vorlagenfeldart.Bild || (feld.Ausgaben & Vorlagenausgabe.Word) == 0)
                {
                    Stehen(m, Fuellbefundart.FalscheStelle, b.DocPr, b.Teil, null);
                    return;
                }
                if (b.Teil.Art == Teilart.Fussnoten || b.Teil.Art == Teilart.Endnoten)
                {
                    Stehen(m, Fuellbefundart.FalscheStelle, b.DocPr, b.Teil, T.BILD_ORT);
                    return;
                }
                if (!string.Equals(feld.Schluessel, Vorlagenfeldkatalog.LOGO, StringComparison.Ordinal))
                {
                    // BV-E5: die Diagramme (WordVorlagenbilder.cs).
                    FuelleDiagramm(b, feld, m);
                    return;
                }

                Platzhalterwert w = Loese(feld, m);
                if (w.Bild != null)
                {
                    if (!SetzeBild(b, w.Bild))
                    {
                        Stehen(m, Fuellbefundart.FalscheStelle, b.DocPr, b.Teil, T.BILD_OHNE_BILD);
                        return;
                    }
                }
                else
                {
                    EntferneBild(b);
                    string warnung = _werte.Ersteller?.LogoWarnung;
                    if (!string.IsNullOrWhiteSpace(warnung) && !_logoGewarnt)
                    {
                        _logoGewarnt = true;
                        _ergebnis.Warnung(warnung);
                    }
                }
                _ergebnis.Ersetzt++;
            }

            /// <summary>
            /// Setzt das Bild in den Rahmen des Platzhalterbildes: neuer Bildteil am Teil des Bildes, die
            /// Maße mit dem Seitenverhältnis des Bildes in Breite und Höhe des Rahmens eingepasst, der
            /// Alternativtext der Dateiname. Der alte Bildteil entfällt, wenn ihn nichts mehr nennt.
            /// <c>false</c>, wenn der Alternativtext an einer Form ohne Bild steht.
            /// </summary>
            private bool SetzeBild(Bildstelle b, Bildinhalt bild)
            {
                OpenXmlElement rahmen = b.DocPr.Parent;
                DW.Extent ausdehnung = rahmen?.GetFirstChild<DW.Extent>();
                A.Blip blip = rahmen?.Descendants<A.Blip>().FirstOrDefault();
                if (ausdehnung == null || blip == null) return false;

                ImagePart teil;
                switch (b.Teil.Teil)
                {
                    case MainDocumentPart haupt: teil = haupt.AddImagePart(Bildtyp(bild)); break;
                    case HeaderPart kopf: teil = kopf.AddImagePart(Bildtyp(bild)); break;
                    case FooterPart fuss: teil = fuss.AddImagePart(Bildtyp(bild)); break;
                    default: return false;
                }
                using (var strom = new MemoryStream(bild.Daten)) teil.FeedData(strom);

                var alt = new List<string>(Beziehungen(blip));
                blip.Embed = b.Teil.Teil.GetIdOfPart(teil);
                blip.Link = null;
                blip.RemoveAllChildren<A.BlipExtensionList>();   // die SVG-Fassung des Platzhalterbildes

                (long breite, long hoehe) = Eingepasst(ausdehnung.Cx?.Value ?? 0L, ausdehnung.Cy?.Value ?? 0L, bild);
                ausdehnung.Cx = breite;
                ausdehnung.Cy = hoehe;
                foreach (A.Extents x in rahmen.Descendants<A.Extents>())
                {
                    x.Cx = breite;
                    x.Cy = hoehe;
                }

                SetzeAlternativtext(b.DocPr, rahmen, bild.Dateiname);
                foreach (string id in alt.Distinct()) EntferneTeilWennFrei(b.Teil, id);
                return true;
            }

            /// <summary>Der Alternativtext nach dem Füllen: der Dateiname, ohne ihn keiner.</summary>
            private static void SetzeAlternativtext(DW.DocProperties docPr, OpenXmlElement rahmen, string dateiname)
            {
                string text = string.IsNullOrWhiteSpace(dateiname) ? null : dateiname;
                docPr.Description = text;
                foreach (PIC.NonVisualDrawingProperties nv in rahmen.Descendants<PIC.NonVisualDrawingProperties>())
                    if (Vorlagenpruefer.IstBildschluessel(nv.Description?.Value)) nv.Description = text;
            }

            private static PartTypeInfo Bildtyp(Bildinhalt bild)
            {
                return bild.Format == Bildformat.Png ? ImagePartType.Png : ImagePartType.Jpeg;
            }

            /// <summary>
            /// Breite und Höhe in EMU: das Bild mit seinem Seitenverhältnis so groß wie möglich in den
            /// Rahmen (<paramref name="breite"/> × <paramref name="hoehe"/>). Fehlt ein Maß des Rahmens,
            /// gilt das andere; fehlen beide, die Bildpunkte bei 96 dpi.
            /// </summary>
            internal static (long Breite, long Hoehe) Eingepasst(long breite, long hoehe, Bildinhalt bild)
            {
                return Eingepasst(breite, hoehe, bild.Breite, bild.Hoehe);
            }

            /// <summary>Dasselbe für ein Bild von <paramref name="bildBreite"/> × <paramref name="bildHoehe"/> Bildpunkten.</summary>
            internal static (long Breite, long Hoehe) Eingepasst(long breite, long hoehe, int bildBreite, int bildHoehe)
            {
                const long EMU_JE_PIXEL = 9525L;
                double b = Math.Max(1, bildBreite), h = Math.Max(1, bildHoehe);
                if (breite <= 0 && hoehe <= 0) return ((long)(b * EMU_JE_PIXEL), (long)(h * EMU_JE_PIXEL));
                double faktor = breite <= 0 ? hoehe / h
                              : hoehe <= 0 ? breite / b
                              : Math.Min(breite / b, hoehe / h);
                return (Math.Max(1L, (long)Math.Round(b * faktor, MidpointRounding.AwayFromZero)),
                        Math.Max(1L, (long)Math.Round(h * faktor, MidpointRounding.AwayFromZero)));
            }

            /// <summary>
            /// Ohne Logo entfällt das Platzhalterbild: die Zeichnung (in <c>mc:AlternateContent</c> samt
            /// ihrer Ersatzfassung), ein danach leerer Lauf, der nicht mehr genannte Bildteil — und ein
            /// danach leerer Absatz, außer er ist der einzige seines Teils oder trägt einen Abschnitt.
            /// </summary>
            private void EntferneBild(Bildstelle b)
            {
                OpenXmlElement weg = b.DocPr.Ancestors<Drawing>().FirstOrDefault();
                OpenXmlElement alternativ = weg?.Ancestors()
                    .FirstOrDefault(e => e.LocalName == "AlternateContent" && e.NamespaceUri == NS_MC);
                if (alternativ != null && alternativ.Parent is Run) weg = alternativ;
                if (weg == null) return;

                List<string> ids = weg.Descendants().SelectMany(Beziehungen).Distinct().ToList();
                Run lauf = weg.Parent as Run;
                Paragraph absatz = weg.Ancestors<Paragraph>().FirstOrDefault();
                weg.Remove();
                if (lauf != null && lauf.ChildElements.All(c => c is RunProperties)) lauf.Remove();
                foreach (string id in ids) EntferneTeilWennFrei(b.Teil, id);

                if (absatz == null || absatz.ParagraphProperties?.SectionProperties != null) return;
                bool leer = absatz.Descendants<Run>().All(r => r.ChildElements.All(c => c is RunProperties)) &&
                            !absatz.Descendants<SimpleField>().Any();
                OpenXmlElement eltern = absatz.Parent;
                bool allein = eltern == null || !eltern.ChildElements.Any(c =>
                    !ReferenceEquals(c, absatz) && (c is Paragraph || c is Table || c is SdtBlock || c is CustomXmlBlock));
                if (leer && !allein) absatz.Remove();
            }

            /// <summary>Die Beziehungskennungen, die ein Element nennt (<c>r:embed</c>, <c>r:link</c>, <c>r:id</c> …).</summary>
            private static IEnumerable<string> Beziehungen(OpenXmlElement e)
            {
                foreach (OpenXmlAttribute a in e.GetAttributes())
                    if (a.NamespaceUri == NS_BEZIEHUNG && !string.IsNullOrEmpty(a.Value)) yield return a.Value;
                foreach (OpenXmlElement kind in e.Descendants())
                    foreach (OpenXmlAttribute a in kind.GetAttributes())
                        if (a.NamespaceUri == NS_BEZIEHUNG && !string.IsNullOrEmpty(a.Value)) yield return a.Value;
            }

            /// <summary>Löscht den Teil zur Beziehung <paramref name="id"/>, wenn nichts im Teil ihn mehr nennt.</summary>
            private static void EntferneTeilWennFrei(Teilinfo ti, string id)
            {
                if (string.IsNullOrEmpty(id)) return;
                bool genannt = ti.Wurzel.Descendants().Any(e => e.GetAttributes()
                    .Any(a => a.NamespaceUri == NS_BEZIEHUNG && string.Equals(a.Value, id, StringComparison.Ordinal)));
                if (genannt) return;
                try
                {
                    if (ti.Teil.Parts.Any(p => p.RelationshipId == id)) ti.Teil.DeletePart(id);
                }
                catch (Exception)
                {
                    // Ein Teil, der sich nicht löschen lässt, bleibt ungenannt im Paket — das Dokument ist trotzdem gültig.
                }
            }

            // ------------------------------------------------------------- Felder, Bildkennungen

            /// <summary>
            /// Konzept 6.7: <c>w:updateFields</c> nur, wenn das Dokument Felder trägt, die Word beim Öffnen
            /// nicht selbst aktualisiert (TOC, PAGEREF, REF, SEQ, DOCPROPERTY) — an der Schemastelle der
            /// Einstellungen. DATE und TIME bekommen einen Hinweis.
            /// </summary>
            private void PruefeFelder()
            {
                var namen = new HashSet<string>(StringComparer.Ordinal);
                foreach (Teilinfo ti in _teile)
                {
                    var codes = new Stack<StringBuilder>();
                    var gelesen = new Stack<bool>();
                    foreach (OpenXmlElement e in ti.Wurzel.Descendants())
                    {
                        switch (e)
                        {
                            case SimpleField feld:
                                namen.Add(Feldname(feld.Instruction?.Value));
                                break;
                            case FieldChar zeichen:
                                {
                                    FieldCharValues? art = zeichen.FieldCharType?.Value;
                                    if (art == FieldCharValues.Begin)
                                    {
                                        codes.Push(new StringBuilder());
                                        gelesen.Push(false);
                                    }
                                    else if (art == FieldCharValues.Separate && codes.Count > 0 && !gelesen.Peek())
                                    {
                                        namen.Add(Feldname(codes.Peek().ToString()));
                                        gelesen.Pop();
                                        gelesen.Push(true);
                                    }
                                    else if (art == FieldCharValues.End && codes.Count > 0)
                                    {
                                        StringBuilder code = codes.Pop();
                                        if (!gelesen.Pop()) namen.Add(Feldname(code.ToString()));
                                    }
                                    break;
                                }
                            case FieldCode code:
                                if (codes.Count > 0 && !gelesen.Peek()) codes.Peek().Append(code.Text);
                                break;
                        }
                    }
                }

                if (namen.Overlaps(AktualisierteFelder)) WordBerichtGenerator.SetzeUpdateFields(_main);
                foreach (string datum in Datumsfelder)
                    if (namen.Contains(datum)) _ergebnis.Hinweis(T.F(_englisch, T.DATUMSFELD, datum));
            }

            private static string Feldname(string code)
            {
                string c = (code ?? "").Trim();
                int ende = c.IndexOfAny(new[] { ' ', '\t', '\\', '"' });
                return (ende < 0 ? c : c.Substring(0, ende)).ToUpperInvariant();
            }

            /// <summary>
            /// <c>wp:docPr/@id</c> in allen Teilen eindeutig neu (Befund BV-E0: Kopfzeilenlogo und
            /// erstes Rumpfbild tragen beide 1) — in Teilreihenfolge ab 1.
            /// </summary>
            private void VergibBildkennungen()
            {
                uint kennung = 1;
                foreach (Teilinfo ti in _teile)
                    foreach (DW.DocProperties d in ti.Wurzel.Descendants<DW.DocProperties>())
                        d.Id = kennung++;
            }

            // ------------------------------------------------------------- Orte und Fundorte

            private static Ortsangabe OrtVon(OpenXmlElement e, Teilinfo ti)
            {
                return new Ortsangabe
                {
                    Teil = ti.Art,
                    InZelle = e.Ancestors<TableCell>().Any(),
                    InTextfeld = e.Ancestors<TextBoxContent>().Any(),
                };
            }

            /// <summary>Der menschliche Fundort: Teil, Absatz, Tabelle, Textfeld, Steuerelement und der Anfang des Absatzes.</summary>
            private string Fundort(OpenXmlElement bezug, Teilinfo ti)
            {
                Paragraph p = bezug as Paragraph ?? bezug.Ancestors<Paragraph>().FirstOrDefault()
                              ?? bezug.Descendants<Paragraph>().FirstOrDefault();
                int nr = p != null && _absatzNummer.TryGetValue(p, out int n) ? n : 0;

                string ort;
                switch (ti.Art)
                {
                    case Teilart.Kopfzeile:
                        ort = ti.Abschnitt == 0 ? T.F(_englisch, T.FUNDORT_KOPF_OHNE, nr)
                            : T.F(_englisch, ti.Typ == "first" ? T.FUNDORT_KOPF_ERSTE : ti.Typ == "even" ? T.FUNDORT_KOPF_GERADE : T.FUNDORT_KOPF,
                                  ti.Abschnitt, nr);
                        break;
                    case Teilart.Fusszeile:
                        ort = ti.Abschnitt == 0 ? T.F(_englisch, T.FUNDORT_FUSS_OHNE, nr)
                            : T.F(_englisch, ti.Typ == "first" ? T.FUNDORT_FUSS_ERSTE : ti.Typ == "even" ? T.FUNDORT_FUSS_GERADE : T.FUNDORT_FUSS,
                                  ti.Abschnitt, nr);
                        break;
                    case Teilart.Fussnoten:
                        ort = T.F(_englisch, T.FUNDORT_FUSSNOTEN, nr);
                        break;
                    case Teilart.Endnoten:
                        ort = T.F(_englisch, T.FUNDORT_ENDNOTEN, nr);
                        break;
                    default:
                        ort = T.F(_englisch, T.FUNDORT_RUMPF, nr);
                        break;
                }

                TableCell zelle = bezug as TableCell ?? bezug.Ancestors<TableCell>().FirstOrDefault();
                if (zelle?.Parent is TableRow zeile && zeile.Parent is Table tabelle)
                {
                    int tNr = _tabellenNummer.TryGetValue(tabelle, out int tn) ? tn : 0;
                    int zNr = tabelle.Elements<TableRow>().ToList().IndexOf(zeile) + 1;
                    int sNr = zeile.Elements<TableCell>().ToList().IndexOf(zelle) + 1;
                    ort += T.F(_englisch, T.FUNDORT_TABELLE, tNr, zNr, sNr);
                }
                if (bezug.Ancestors<TextBoxContent>().Any()) ort += T.T(T.FUNDORT_TEXTFELD, _englisch);
                if (bezug is SdtElement) ort += T.T(T.FUNDORT_SDT, _englisch);

                string auszug = Auszug(p);
                if (auszug.Length > 0) ort += T.F(_englisch, T.FUNDORT_AUSZUG, auszug);
                return ort;
            }

            private static string Auszug(Paragraph p)
            {
                string text = (p?.InnerText ?? "").Trim();
                return text.Length <= AUSZUG_LAENGE ? text : text.Substring(0, AUSZUG_LAENGE) + "…";
            }

            // ------------------------------------------------------------- Hilfen

            /// <summary>Die <c>w:t</c> eines Absatzes ohne die seiner Textfelder (dort stehen eigene Absätze).</summary>
            private static List<Text> EigeneTexte(Paragraph p)
            {
                return p.Descendants<Text>()
                        .Where(t => ReferenceEquals(t.Ancestors<Paragraph>().FirstOrDefault(), p))
                        .ToList();
            }

            /// <summary>
            /// Steht die Marke allein im Absatz (Konzept 4.2: Absatzplatzhalter)? Der eigene Text des
            /// Absatzes ist ohne Leerraum die Marke, und kein Run trägt etwas anderes als Text — kein
            /// Bild, Feld, Tabulator oder Umbruch.
            /// </summary>
            private static bool IstAllein(Paragraph p, Platzhalter m)
            {
                string eigen = string.Concat(EigeneTexte(p).Select(t => t.Text));
                if (!string.Equals(OhneLeerraum(eigen), OhneLeerraum(m.Roh), StringComparison.Ordinal)) return false;
                foreach (Run r in p.Descendants<Run>())
                {
                    if (!ReferenceEquals(r.Ancestors<Paragraph>().FirstOrDefault(), p)) continue;
                    foreach (OpenXmlElement k in r.ChildElements)
                        if (!(k is RunProperties || k is Text || k is LastRenderedPageBreak)) return false;
                }
                return true;
            }

            private static string OhneLeerraum(string s)
            {
                return new string((s ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray());
            }

            /// <summary>Hängt das Element noch unter der Wurzel (oder wurde es mit einem Absatz entfernt)?</summary>
            private static bool Haengt(OpenXmlElement e, OpenXmlElement wurzel)
            {
                for (OpenXmlElement x = e; x != null; x = x.Parent)
                    if (ReferenceEquals(x, wurzel)) return true;
                return false;
            }
        }
    }
}
