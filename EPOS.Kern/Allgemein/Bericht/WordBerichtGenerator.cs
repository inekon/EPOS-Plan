using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Word-Erzeugung des Variantenberichts über das OpenXML SDK (Konzept Kap. 4/8).
    ///
    /// Grundlage ist die Rahmen-/Stylevorlage Vorlagen\Berichtsvorlage.docx
    /// (Styles: Title, Subtitle, Heading1–3, Normal, Hinweis, Beschriftung; Kopfzeile
    /// mit Logo, Fußzeile mit Seitenfeldern). Fehlt die Vorlage, wird das Dokument
    /// mit programmatisch angelegten Ersatz-Styles erzeugt — der Bericht entsteht
    /// in jedem Fall. Die Kapitel schreiben IBerichtsBaustein-Implementierungen
    /// über den WordKontext; dieser Generator kennt Rahmen, Styles und Tabellenbau.
    /// </summary>
    public class WordBerichtGenerator
    {
        /// <summary>
        /// Nutzbare Inhaltsbreite in DXA (A4, Ränder 25/20 mm → 165 mm) — nur noch der Rückfall,
        /// wenn der Abschnitt am Anker kein Seitenmaß trägt. Die Bausteine lesen
        /// <see cref="WordKontext.Inhaltsbreite"/> (Konzept Berichtsvorlagen 5.3).
        /// </summary>
        public const int INHALT_B = 9355;

        public const string HEAD_FILL = "D9E1F2";
        public const string STAMM_FILL = "F2F2F2";
        public const string RAHMEN = "BFBFBF";

        /// <summary>Schriftgröße der Tabellenzellen in Halbpunkten (9 pt).</summary>
        public const int SCHRIFT_TABELLE = 18;

        /// <summary>
        /// Schriftgröße für breite Zahlentabellen in Halbpunkten (7 pt, Etappe E7) —
        /// die Mehrjahresübersicht führt bis zu dreizehn Spalten auf A4 hoch.
        /// </summary>
        public const int SCHRIFT_TABELLE_SCHMAL = 14;

        /// <summary>
        /// <b>Die Office-Erweiterung, die ein SVG an einen Blip hängt</b> (Entscheid
        /// DG-E3-8). Der Wert ist festgelegt — Word erkennt die Erweiterung an dieser
        /// Kennung, nicht am Elementnamen.
        /// </summary>
        public const string SVG_EXT_URI = "{96DAC541-7B7A-43D3-8B79-37D633B846F1}";

        /// <summary>
        /// Erzeugt den Bericht. Rückgabe: Pfad der geschriebenen Datei. Die Vorlage sucht
        /// <see cref="FindeVorlage"/> an den bekannten Orten.
        /// </summary>
        public string Erzeuge(BerichtsDaten daten, BerichtsKonfiguration konfig, string zielDatei)
            => Erzeuge(daten, konfig, zielDatei, null);

        /// <summary>
        /// Erzeugt den Bericht aus einer ausdrücklich benannten Vorlage (Konzept
        /// Berichtsvorlagen, Etappe BV-E0: der Test mit der echten Vorlage). Rückgabe: Pfad
        /// der geschriebenen Datei.
        /// </summary>
        /// <param name="vorlagePfad">Die <c>.docx</c>, die als Rahmen kopiert wird;
        /// <c>null</c> = <see cref="FindeVorlage"/> wie bisher. Eine benannte, aber fehlende
        /// Vorlage bricht mit ihrem Pfad ab, statt still auf die Ersatzstile auszuweichen.</param>
        public string Erzeuge(BerichtsDaten daten, BerichtsKonfiguration konfig, string zielDatei,
                              string vorlagePfad)
        {
            if (daten == null || daten.Varianten.Count == 0)
                throw new ArgumentException("Keine Berichtsdaten vorhanden.");
            if (vorlagePfad != null && !File.Exists(vorlagePfad))
                throw new FileNotFoundException("Die Berichtsvorlage fehlt: " + vorlagePfad, vorlagePfad);

            string vorlage = vorlagePfad ?? FindeVorlage();
            if (vorlage != null) File.Copy(vorlage, zielDatei, true);

            using (WordprocessingDocument doc = vorlage != null
                ? WordprocessingDocument.Open(zielDatei, true)
                : WordprocessingDocument.Create(zielDatei, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.MainDocumentPart;
                if (main == null)
                {
                    main = doc.AddMainDocumentPart();
                    main.Document = new Document(new Body());
                }
                if (main.Document == null) main.Document = new Document(new Body());
                Body body = main.Document.Body ?? main.Document.AppendChild(new Body());

                if (vorlage == null) ErgaenzeErsatzStyles(main);

                // Body leeren — die SectionProperties (Kopf-/Fußzeilen-Verweise, Ränder)
                // der Vorlage bleiben unangetastet; Inhalte werden davor eingefügt.
                SectionProperties sect = body.Elements<SectionProperties>().LastOrDefault();
                foreach (OpenXmlElement el in body.ChildElements.Where(c => !(c is SectionProperties)).ToList())
                    el.Remove();

                var kontext = new WordKontext(main, body, sect);

                // Felder (Inhaltsverzeichnis, Datum, Seitenzahlen) beim Öffnen aktualisieren.
                SetzeUpdateFields(main);

                foreach (IBerichtsBaustein baustein in AktiveBausteine(konfig))
                    baustein.SchreibeWord(kontext, daten, konfig);

                main.Document.Save();
            }
            return zielDatei;
        }

        /// <summary>
        /// <b>Der Bericht aus einer Vorlage mit Platzhaltern</b> (Konzept Berichtsvorlagen, Etappe
        /// BV-E1): Die Vorlage bleibt, wie sie ist — Deckblatt, Kopf- und Fußzeilen, Abschnitte —,
        /// ihre Platzhalter werden gefüllt, und <c>{{bericht.inhalt}}</c> setzt die angehakten
        /// Bausteine an seine Stelle (<see cref="WordVorlagenfueller"/>). Welche Vorlage genommen wird,
        /// entscheidet der Aufrufer — im Programm <see cref="BerichtCtrl.ErzeugeWordLauf"/> über
        /// <see cref="BerichtsvorlagenCtrl.VorlageFuer"/>, einmal gelesen und mit den Erstellerangaben
        /// des Controllers; <see cref="Erzeuge(BerichtsDaten, BerichtsKonfiguration, string, string)"/>
        /// bleibt der Weg der Stilvorlage und des Code-Rückfalls.
        /// </summary>
        /// <param name="vorlage">Die Bytes der <c>.docx</c> bzw. <c>.dotx</c>; sie werden nicht verändert.</param>
        /// <param name="ersteller">Firma, Programm und Fassung für <c>ersteller.*</c>; <c>null</c> = ohne Firma.</param>
        /// <returns>Was ersetzt, leer geblieben, unbekannt oder entfernt ist — für die Laufmeldung.</returns>
        public Fuellergebnis ErzeugeMitVorlage(BerichtsDaten daten, BerichtsKonfiguration konfig, byte[] vorlage,
                                               Erstellerangaben ersteller, string zielDatei)
        {
            return new WordVorlagenfueller().Fuelle(vorlage, daten, konfig, ersteller, zielDatei);
        }

        /// <summary>
        /// Bausteine in Berichtsreihenfolge, gefiltert auf die aktive Auswahl. Folge und Klassen stehen
        /// EINMAL in <see cref="Berichtskapitel.Alle"/>: Deckblatt, Inhaltsverzeichnis, Projekt,
        /// Komponenten, Ergebnisse, Vergleich, Wirtschaftlichkeit (liest den Wertesatz <see cref="BerichtsDaten.Wirtschaft"/>),
        /// Anhang und die Anhang-E-Checkliste als Abschlussseite am Schlüssel der Wirtschaftlichkeit.
        /// </summary>
        public static List<IBerichtsBaustein> AktiveBausteine(BerichtsKonfiguration konfig)
        {
            return Berichtskapitel.Alle.Where(k => k.IstAktiv(konfig)).Select(k => k.NeuerBaustein()).ToList();
        }

        // ------------------------------------------------------------- Vorlage

        /// <summary>
        /// Sucht die STILVORLAGE des bisherigen Wegs (<see cref="BerichtsvorlagenCtrl.DATEI_RUECKFALL"/>)
        /// im Ordner der ausgelieferten Vorlagen (<see cref="IPfade.Berichtsvorlagen"/>:
        /// <c>{app}\Vorlagen</c>, auf iOS das Anwendungspaket); <c>null</c> = nicht gefunden, dann
        /// gelten die Ersatzstile. Die Standardvorlage mit Platzhaltern
        /// (<see cref="BerichtsvorlagenCtrl.DATEI_STANDARD"/>) sucht sie nicht: Die füllt die Engine
        /// (<see cref="ErzeugeMitVorlage"/>) — dieser Weg leert den Rumpf und ließe ihre Platzhalter in
        /// Kopf- und Fußzeile stehen. Die Dateinamen stehen EINMAL, im <see cref="BerichtsvorlagenCtrl"/>.
        /// </summary>
        public static string FindeVorlage()
        {
            string ordner = "";
            try { ordner = Dienste.Pfade.Berichtsvorlagen ?? ""; } catch { ordner = ""; }
            if (ordner.Length == 0) return null;

            string pfad = Path.Combine(ordner, BerichtsvorlagenCtrl.DATEI_RUECKFALL);
            return File.Exists(pfad) ? pfad : null;
        }

        /// <summary>Setzt <c>w:updateFields</c> an die Schemastelle der Einstellungen (auch für den
        /// Vorlagenweg, dort nur bei Feldern, die Word nicht selbst aktualisiert).</summary>
        internal static void SetzeUpdateFields(MainDocumentPart main)
        {
            DocumentSettingsPart sp = main.DocumentSettingsPart ?? main.AddNewPart<DocumentSettingsPart>();
            if (sp.Settings == null) sp.Settings = new Settings();
            sp.Settings.RemoveAllChildren<UpdateFieldsOnOpen>();
            // An die Stelle, die das Schema vorgibt (CT_Settings ist eine feste Folge;
            // w:updateFields steht hinter w:evenAndOddHeaders und vor w:compat). Vorangestellt
            // war die Einstellung nur in der leeren Ersatzdatei gültig — die Einstellungen der
            // Vorlage (w:displayBackgroundShape, w:evenAndOddHeaders, w:compat) standen dann
            // dahinter, und der Validator wies das Dokument in jeder Office-Fassung zurück.
            sp.Settings.AddChild(new UpdateFieldsOnOpen { Val = true });
            sp.Settings.Save();
        }

        // Minimale Ersatz-Styles, falls die Vorlage fehlt (gleiche Style-IDs wie die Vorlage).
        private static void ErgaenzeErsatzStyles(MainDocumentPart main)
        {
            StyleDefinitionsPart part = main.StyleDefinitionsPart ?? main.AddNewPart<StyleDefinitionsPart>();
            if (part.Styles == null) part.Styles = new Styles();

            part.Styles.Append(ErsatzStyle("Normal", null, 21, false, null, true));
            part.Styles.Append(ErsatzStyle("Title", "Normal", 56, true, "1F4E79", false));
            part.Styles.Append(ErsatzStyle("Subtitle", "Normal", 28, false, "595959", false));
            part.Styles.Append(ErsatzStyle("Heading1", "Normal", 30, true, "1F4E79", false));
            part.Styles.Append(ErsatzStyle("Heading2", "Normal", 25, true, "1F4E79", false));
            part.Styles.Append(ErsatzStyle("Heading3", "Normal", 22, true, "595959", false));
            part.Styles.Append(ErsatzStyle("Hinweis", "Normal", 18, false, "595959", false));
            part.Styles.Append(ErsatzStyle("Beschriftung", "Normal", 18, false, "595959", false));
            part.Styles.Save();
        }

        private static Style ErsatzStyle(string id, string basedOn, int sizeHalf, bool bold, string farbe, bool standard)
        {
            var s = new Style { Type = StyleValues.Paragraph, StyleId = id, Default = standard };
            s.Append(new StyleName { Val = id });
            if (basedOn != null) s.Append(new BasedOn { Val = basedOn });
            var rp = new StyleRunProperties();
            rp.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
            if (bold) rp.Append(new Bold());
            if (farbe != null) rp.Append(new Color { Val = farbe });
            rp.Append(new FontSize { Val = sizeHalf.ToString() });
            s.Append(rp);
            return s;
        }
    }

    // =======================================================================
    /// <summary>
    /// Schreib-API für die Bausteine: Style-basierte Absätze, Tabellenbau mit den
    /// Berichtskonstanten, Zahlformatierung (Berichtssprache de, Phase 5: UI-Sprache),
    /// Blocksplitting-Hilfen für unbegrenzt viele Varianten (Konzept Kap. 5.1).
    /// </summary>
    public class WordKontext
    {
        public readonly MainDocumentPart Main;
        public readonly Body Body;
        private readonly SectionProperties _sect;

        /// <summary>Der Einfügeanker des Vorlagenwegs; <c>null</c> = vor der Abschnittsangabe des Rumpfs.</summary>
        private readonly Einfuegeanker _anker;

        /// <summary>Die Rollenauflösung des Vorlagenwegs; <c>null</c> = feste Stil-IDs.</summary>
        private readonly WordVorlagenstile _stile;

        /// <summary>Der Teil, an dem die Bildteile hängen (Teil des Ankers).</summary>
        private readonly OpenXmlPart _teil;

        /// <summary>Kultur der Berichtssprache (= UI-Sprache; BerichtTexte, Phase 5).</summary>
        public readonly CultureInfo Kultur = BerichtTexte.Kultur;

        /// <summary>Max. Varianten je Tabellenblock (A4 hoch; Stamm-Spalte wird je Block wiederholt).</summary>
        public const int MAX_VARIANTEN_JE_BLOCK = 3;

        /// <summary>Kleinste Inhaltsbreite in DXA, die ein Abschnitt liefern darf.</summary>
        public const int MIN_INHALTSBREITE = 1000;

        /// <summary>Kleinste Spaltenbreite in DXA, auf die das Einpassen eine Tabellenspalte setzt.</summary>
        public const int MIN_SPALTE = 200;

        /// <summary>DXA je Pixel bei 96 dpi (1 440 DXA je Zoll).</summary>
        private const int DXA_JE_PIXEL = 15;

        /// <summary>
        /// Der bisherige Weg: eingefügt wird vor der Abschnittsangabe <paramref name="sect"/> des
        /// Rumpfs (ohne sie am Ende), die Stil-IDs sind die Rollennamen selbst (Stilvorlage
        /// <c>Berichtsvorlage.docx</c> und Ersatzstile — Messprobe 5 von BV-E0 hält das fest). Die
        /// Inhaltsbreite kommt aus <paramref name="sect"/>, ohne Seitenmaß aus <see cref="WordBerichtGenerator.INHALT_B"/>.
        /// </summary>
        public WordKontext(MainDocumentPart main, Body body, SectionProperties sect)
        {
            Main = main; Body = body; _sect = sect; _teil = main;
            Inhaltsbreite = InhaltsbreiteAus(sect);
        }

        /// <summary>
        /// <b>Der Vorlagenweg</b> (Konzept Berichtsvorlagen 4.3, 5.3, 6.2): eingefügt wird am
        /// Einfügeanker, die Bildteile hängen an dessen Teil, die Stilrollen löst
        /// <paramref name="stile"/> je Dokument auf, und die Inhaltsbreite kommt aus der
        /// Abschnittsangabe des Abschnitts, in dem der Anker steht.
        /// </summary>
        public WordKontext(MainDocumentPart main, Einfuegeanker anker, WordVorlagenstile stile)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            _anker = anker ?? throw new ArgumentNullException(nameof(anker));
            _stile = stile;
            _teil = anker.Teil;
            Body = main.Document?.Body;
            Inhaltsbreite = InhaltsbreiteAus(anker.Abschnitt());
        }

        /// <summary>
        /// <b>Die nutzbare Breite am Anker</b> in DXA (Konzept 5.3): Seitenbreite minus Ränder und
        /// Bundsteg aus der Abschnittsangabe des Ankerabschnitts, bei Spalten die Spaltenbreite.
        /// Sie ersetzt die feste <see cref="WordBerichtGenerator.INHALT_B"/> in den Bausteinen;
        /// Bilder werden höchstens so breit, Tabellen darauf eingepasst.
        /// </summary>
        public int Inhaltsbreite { get; }

        // ------------------------------------------------------------- Kapitelformat (Konzept 4.8, 5.3)

        /// <summary>
        /// <c>|ohne titel</c>: Die eigene Überschrift des Bausteins entfällt — sein erster Aufruf von
        /// Überschrift 1 oder 2 (die Kapitelüberschrift) schreibt nichts; der Kapitelkopf der Vorlage
        /// steht an ihrer Stelle.
        /// </summary>
        public bool OhneTitel { get; internal set; }

        /// <summary>
        /// <c>|ebene n</c> minus 1: um so viele Ebenen rücken die Überschriften des Bausteins tiefer —
        /// Überschrift 1 wird Überschrift n, 2 wird n + 1 …, höchstens 9 (<see cref="WordVorlagenstile.EBENE_MAX"/>).
        /// </summary>
        public int Ebenenversatz { get; internal set; }

        /// <summary>
        /// Die Stellen der Kapitel im Bericht, wie ihn die Vorlage baut (Konzept 11 Nr. 3): je
        /// <see cref="Berichtskapitel.Stellenschluessel"/> die Überschrift vor dem Anker, <c>null</c> =
        /// nicht im Bericht. Die Anhang-E-Checkliste nennt sie in ihrer Spalte „Stelle“; <c>null</c> (der
        /// bisherige Weg) heißt: die eigenen Überschriften der angehakten Kapitel.
        /// </summary>
        public IReadOnlyDictionary<string, string> Kapitelstellen { get; internal set; }

        /// <summary>
        /// Wohin, was der Baustein VOR seine entfallene Überschrift schreibt, kommt: vor den Kapitelkopf
        /// der Vorlage (<c>|ohne titel</c> unter einem Kapitelkopf). So steht der Seitenumbruch des
        /// Anhangs E vor dem Kapitelkopf, und die Überschrift hängt nie allein am Seitenende.
        /// <c>null</c> = alles an den Anker.
        /// </summary>
        internal Einfuegeanker Vorspann { get; set; }

        /// <summary>Wie viele Elemente der Baustein an den Anker geschrieben hat (Konzept 5.3 „Entfall“).</summary>
        internal int AmAnkerGeschrieben { get; private set; }

        /// <summary>Die Elemente, die vor den Kapitelkopf gewandert sind.</summary>
        internal IReadOnlyList<OpenXmlElement> ImVorspann { get { return _imVorspann; } }

        private readonly List<OpenXmlElement> _puffer = new List<OpenXmlElement>();
        private readonly List<OpenXmlElement> _imVorspann = new List<OpenXmlElement>();
        private bool _titelErledigt;

        /// <summary>Wird zurückgehalten, was vor der Überschrift kommt (bis klar ist, ob sie entfällt)?</summary>
        private bool Puffert { get { return OhneTitel && Vorspann != null && !_titelErledigt; } }

        /// <summary>
        /// Nach dem Baustein: Schrieb er keine Überschrift, gehört das Zurückgehaltene an den Anker —
        /// in der Folge, in der er es schrieb.
        /// </summary>
        internal void Abschliessen()
        {
            _titelErledigt = true;
            foreach (OpenXmlElement el in _puffer) Setze(el);
            _puffer.Clear();
        }

        /// <summary>Die Stil-ID einer Rolle (<see cref="WordVorlagenstile"/>): im Vorlagenweg
        /// aufgelöst, sonst die feste ID.</summary>
        public string StilId(string rolle)
        {
            return _stile != null ? _stile.Id(rolle) : WordVorlagenstile.FesteId(rolle);
        }

        /// <summary>
        /// Die Inhaltsbreite eines Abschnitts in DXA: <c>w:pgSz/@w:w</c> minus linker und rechter Rand
        /// und Bundsteg; bei mehreren Spalten die Spaltenbreite (gleich breite Spalten abzüglich der
        /// Abstände, sonst die schmalste). Ohne Seitenmaß <see cref="WordBerichtGenerator.INHALT_B"/>.
        /// </summary>
        public static int InhaltsbreiteAus(SectionProperties abschnitt)
        {
            PageSize seite = abschnitt?.GetFirstChild<PageSize>();
            if (seite?.Width == null) return WordBerichtGenerator.INHALT_B;

            long breite = seite.Width.Value;
            PageMargin rand = abschnitt.GetFirstChild<PageMargin>();
            if (rand != null)
                breite -= (long)(rand.Left?.Value ?? 0U) + (rand.Right?.Value ?? 0U) + (rand.Gutter?.Value ?? 0U);

            Columns spalten = abschnitt.GetFirstChild<Columns>();
            if (spalten != null)
            {
                List<long> einzeln = spalten.Elements<Column>()
                    .Select(c => Dxa(c.Width?.Value)).Where(w => w > 0).ToList();
                bool gleich = spalten.EqualWidth == null || spalten.EqualWidth.Value;
                if (!gleich && einzeln.Count > 0)
                {
                    breite = Math.Min(breite, einzeln.Min());
                }
                else
                {
                    int zahl = spalten.ColumnCount?.Value ?? 1;
                    if (zahl > 1)
                    {
                        long abstand = spalten.Space?.Value != null ? Dxa(spalten.Space.Value) : 720;
                        breite = (breite - (zahl - 1) * abstand) / zahl;
                    }
                }
            }
            return (int)Math.Max(MIN_INHALTSBREITE, Math.Min(int.MaxValue, breite));
        }

        private static long Dxa(string wert)
        {
            return long.TryParse(wert, NumberStyles.Integer, CultureInfo.InvariantCulture, out long d) ? d : 0L;
        }

        /// <summary>
        /// Fügt ein Element am Einfügeanker ein — im bisherigen Weg vor den SectionProperties
        /// (Kopf-/Fußzeile bleiben erhalten). Eine Tabelle, die breiter ist als
        /// <see cref="Inhaltsbreite"/>, wird vorher eingepasst.
        /// </summary>
        public void Fuege(OpenXmlElement el)
        {
            if (el is Table t) PasseEin(t);
            if (Puffert)
            {
                _puffer.Add(el);
                return;
            }
            Setze(el);
        }

        private void Setze(OpenXmlElement el)
        {
            AmAnkerGeschrieben++;
            if (_anker != null) _anker.Fuege(el);
            else if (_sect != null) Body.InsertBefore(el, _sect);
            else Body.Append(el);
        }

        /// <summary>
        /// Passt eine Tabelle in die <see cref="Inhaltsbreite"/> ein: Spalten unter
        /// <see cref="MIN_SPALTE"/> (etwa ein Rest, der in einem schmalen Satzspiegel negativ würde)
        /// werden angehoben, und ist die Summe breiter als der Satzspiegel, schrumpfen alle Spalten
        /// im selben Verhältnis — Raster, Tabellenbreite und Zellbreiten gemeinsam. Passt die
        /// Tabelle, bleibt sie unberührt.
        /// </summary>
        private void PasseEin(Table t)
        {
            TableGrid raster = t.GetFirstChild<TableGrid>();
            List<GridColumn> spalten = raster?.Elements<GridColumn>().ToList();
            if (spalten == null || spalten.Count == 0) return;

            long[] alt = spalten.Select(g => Dxa(g.Width?.Value)).ToArray();
            long summe = alt.Sum(w => Math.Max(w, MIN_SPALTE));
            if (alt.All(w => w >= MIN_SPALTE) && summe <= Inhaltsbreite) return;

            double faktor = summe > Inhaltsbreite ? (double)Inhaltsbreite / summe : 1.0;
            int[] neu = alt.Select(w => (int)Math.Max(1, Math.Floor(Math.Max(w, MIN_SPALTE) * faktor))).ToArray();
            for (int i = 0; i < spalten.Count; i++)
                spalten[i].Width = neu[i].ToString(CultureInfo.InvariantCulture);

            TableWidth tw = t.GetFirstChild<TableProperties>()?.GetFirstChild<TableWidth>();
            if (tw != null && (tw.Type == null || tw.Type.Value == TableWidthUnitValues.Dxa))
                tw.Width = neu.Sum().ToString(CultureInfo.InvariantCulture);

            foreach (TableRow zeile in t.Elements<TableRow>())
            {
                int spalte = 0;
                foreach (TableCell zelle in zeile.Elements<TableCell>())
                {
                    int breit = zelle.TableCellProperties?.GridSpan?.Val?.Value ?? 1;
                    int w = 0;
                    for (int k = spalte; k < spalte + breit && k < neu.Length; k++) w += neu[k];
                    spalte += breit;
                    TableCellWidth zw = zelle.TableCellProperties?.TableCellWidth;
                    if (zw != null && w > 0 && (zw.Type == null || zw.Type.Value == TableWidthUnitValues.Dxa))
                        zw.Width = w.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        // ------------------------------------------------------------- Absätze

        public void MitStil(string styleId, string text)
        {
            // Berichtssprache: bekannte Texte werden übersetzt, dynamische laufen durch.
            text = BerichtTexte.T(text);
            MitStilRoh(styleId, text);
        }

        /// <summary>
        /// Absatz OHNE <c>BerichtTexte.T()</c> — für Texte, die bereits aus
        /// <c>MyResource</c> kommen und damit schon in der Berichtssprache stehen
        /// (Etappe E7).
        ///
        /// <para><b>Warum das nötig ist.</b> <c>T()</c> ist ein Wörterbuch Deutsch → Englisch;
        /// ein bereits übersetzter Text läuft heute nur deshalb unverändert durch, weil er
        /// darin nicht vorkommt. Sobald ein deutscher <c>MyResource</c>-Wert einmal ins
        /// Wörterbuch gerät, übersetzt die Kette doppelt. Neue und umgestellte Texte gehen
        /// deshalb diesen Weg.</para>
        /// </summary>
        public void MitStilRoh(string styleId, string text)
        {
            // Kapitelformat (Konzept 4.8): Die erste Überschrift 1 oder 2 ist die Kapitelüberschrift —
            // mit |ohne titel entfällt sie, und was davor stand, wandert vor den Kapitelkopf. |ebene n
            // rückt jede Überschrift des Bausteins um n − 1 Ebenen tiefer.
            int? ebene = WordVorlagenstile.Ueberschriftebene(styleId);
            if (ebene.HasValue && ebene.Value <= 2 && !_titelErledigt)
            {
                _titelErledigt = true;
                if (OhneTitel)
                {
                    foreach (OpenXmlElement el in _puffer)
                    {
                        Vorspann.Fuege(el);
                        _imVorspann.Add(el);
                    }
                    _puffer.Clear();
                    return;
                }
            }
            if (ebene.HasValue && Ebenenversatz > 0) styleId = WordVorlagenstile.Ueberschrift(ebene.Value + Ebenenversatz);

            // styleId ist eine ROLLE (WordVorlagenstile): im Vorlagenweg je Dokument aufgelöst.
            var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = StilId(styleId) }));
            p.Append(new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
            Fuege(p);
        }

        /// <summary>
        /// Der leere Abstandsabsatz zwischen Tabellen und Abschnitten — die Rolle
        /// <see cref="WordVorlagenstile.ABSTAND"/> (bisher <c>Beschriftung(" ")</c>; im bisherigen
        /// Weg bleibt es genau dieser Absatz).
        /// </summary>
        public void Abstand() { MitStilRoh(WordVorlagenstile.ABSTAND, " "); }

        public void Titel(string t) { MitStil("Title", t); }
        public void Untertitel(string t) { MitStil("Subtitle", t); }
        public void Ueberschrift1(string t) { MitStil("Heading1", t); }
        public void Ueberschrift2(string t) { MitStil("Heading2", t); }
        public void Ueberschrift3(string t) { MitStil("Heading3", t); }
        public void Text(string t) { MitStil("Normal", t); }
        public void Hinweis(string t) { MitStil("Hinweis", t); }
        public void Beschriftung(string t) { MitStil("Beschriftung", t); }

        /// <inheritdoc cref="MitStilRoh"/>
        public void Ueberschrift2Roh(string t) { MitStilRoh("Heading2", t); }
        /// <inheritdoc cref="MitStilRoh"/>
        public void Ueberschrift3Roh(string t) { MitStilRoh("Heading3", t); }
        /// <inheritdoc cref="MitStilRoh"/>
        public void HinweisRoh(string t) { MitStilRoh("Hinweis", t); }
        /// <inheritdoc cref="MitStilRoh"/>
        public void TextRoh(string t) { MitStilRoh("Normal", t); }

        public void Seitenumbruch()
        { Fuege(new Paragraph(new Run(new Break { Type = BreakValues.Page }))); }

        /// <summary>Inhaltsverzeichnis-Feld (Word aktualisiert beim Öffnen; UpdateFieldsOnOpen ist gesetzt).</summary>
        public void TocFeld()
        {
            var p = new Paragraph();
            p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            p.Append(new Run(new FieldCode(" TOC \\o \"1-3\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve }));
            p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            p.Append(new Run(new Text("Das Inhaltsverzeichnis wird beim Öffnen in Word aktualisiert.")));
            p.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
            Fuege(p);
        }

        // ------------------------------------------------------------- Bilder

        private uint _bildId = 1;

        /// <summary>
        /// Bettet ein PNG als Inline-Grafik ein (Anzeigegröße in Pixel bei 96 dpi;
        /// gerendert wird in doppelter Auflösung → scharfer Druck). Portierung der
        /// BildDrawing-Logik aus dem Bestandsbericht. png == null wird ignoriert.
        ///
        /// <para><b>Kein Baustein des Berichts ruft ihn mehr</b> — jedes Diagramm des
        /// Wortberichts geht über <see cref="Bild(Zeichnung.Zeichenmodell, int, int)"/>
        /// und trägt damit SVG mit PNG-Rückfall. Der Weg bleibt für FREMDBILDER: ein
        /// Bild, das nicht aus dem <c>ChartRenderer</c> kommt. Einen Renderer ohne
        /// Zeichenmodell gibt es nicht mehr — auch die zwei Oberflächenbilder
        /// (<c>PeakShavingBild</c>, <c>SpeicherBetriebsbild</c>) führen eines.</para>
        /// </summary>
        public void Bild(byte[] png, int anzeigeBreitePx, int anzeigeHoehePx)
            => BildTeile(png, null, anzeigeBreitePx, anzeigeHoehePx);

        /// <summary>
        /// <b>Dasselbe Bild aus dem ZEICHENMODELL — als SVG mit PNG-Rückfall</b>
        /// (Entscheid DG-E3-8, Anwenderentscheid 20.09.2026 „alle Grafiken, soweit
        /// möglich").
        ///
        /// <para>Es entstehen ZWEI Teile im Dokument: das PNG aus
        /// <c>SkiaMaler.Png</c> als gewöhnlicher <c>a:blip</c> und der SVG-Text aus
        /// <c>SkiaMaler.Drucksvg</c> (Grundlinien ausgerechnet, denn der SVG-Leser von Word
        /// übergeht <c>dominant-baseline</c>) als zweiter <c>ImagePart</c> mit dem Inhaltstyp
        /// <c>image/svg+xml</c>, verknüpft über <c>asvg:svgBlip</c> in der
        /// Erweiterungsliste des Blips (<see cref="SVG_EXT_URI"/>). Word ab 2016 zeigt
        /// das SVG und druckt es in Gerätauflösung; jeder ältere Leser — und jeder
        /// Konverter, der die Erweiterung nicht kennt — zeigt das PNG. Maße und Lage
        /// sind dieselben wie beim reinen PNG.</para>
        ///
        /// <para><c>null</c> wird übergangen: Ein Bild, das der Lauf nicht hergibt,
        /// lässt die Stelle aus.</para>
        /// </summary>
        public void Bild(Zeichnung.Zeichenmodell modell, int anzeigeBreitePx, int anzeigeHoehePx)
        {
            if (modell == null) return;
            BildTeile(Zeichnung.SkiaMaler.Png(modell), Zeichnung.SkiaMaler.Drucksvg(modell),
                      anzeigeBreitePx, anzeigeHoehePx);
        }

        /// <summary>
        /// Der gemeinsame Rumpf beider Bildwege. <paramref name="svg"/> leer heißt
        /// „nur PNG"; sonst kommt der zweite Teil dazu und der Blip bekommt seine
        /// Erweiterungsliste. BV-E5: Blip und Zeichnung baut <see cref="Wordbilder"/> — dieselben
        /// Teile, die die Word-Engine in einen Bildplatzhalter setzt.
        /// </summary>
        private void BildTeile(byte[] png, string svg, int anzeigeBreitePx, int anzeigeHoehePx)
        {
            if (png == null || png.Length == 0) return;

            // Höchstens so breit wie der Satzspiegel am Anker (Konzept 5.3: min(heutige Breite,
            // Satzspiegel)); die Höhe folgt dem Seitenverhältnis.
            int maxPx = Inhaltsbreite / DXA_JE_PIXEL;
            if (maxPx > 0 && anzeigeBreitePx > maxPx)
            {
                anzeigeHoehePx = (int)Math.Round(anzeigeHoehePx * (double)maxPx / anzeigeBreitePx, MidpointRounding.AwayFromZero);
                anzeigeBreitePx = maxPx;
            }

            // Die Bildteile hängen am Teil des Ankers (Konzept 4.3) — im Rumpf der Hauptteil.
            A.Blip blip = Wordbilder.BaueBlip(_teil, png, svg);
            long cx = anzeigeBreitePx * Wordbilder.EMU_JE_PIXEL;
            long cy = anzeigeHoehePx * Wordbilder.EMU_JE_PIXEL;
            uint id = _bildId++;
            Fuege(new Paragraph(new Run(Wordbilder.Inline(blip, cx, cy, id, null))));
        }

        // ------------------------------------------------------------- Tabellen

        public Table NeueTabelle(int[] breiten)
        {
            var t = new Table();
            var tp = new TableProperties();
            tp.Append(new TableWidth { Type = TableWidthUnitValues.Dxa, Width = breiten.Sum().ToString() });
            // DIE REIHENFOLGE IST TEIL DES SCHEMAS (Befund DG-E3d): `CT_TblBorders`
            // führt top, left, bottom, right, insideH, insideV — in GENAU dieser
            // Folge. Bis hierher stand links hinter unten; der `OpenXmlValidator`
            // meldete deshalb je Tabelle „unexpected child element w:left". Word zeigt
            // den Rahmen trotzdem, die Datei war aber nicht schemagültig, und jeder
            // strengere Leser hätte sie zurückgewiesen. Am Bild ändert der Tausch
            // nichts: Es sind dieselben sechs Rahmen in denselben Farben.
            tp.Append(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new RightBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN }));
            t.Append(tp);
            var grid = new TableGrid();
            foreach (int b in breiten) grid.Append(new GridColumn { Width = b.ToString() });
            t.Append(grid);
            return t;
        }

        public TableCell Zelle(string text, int breite, bool fett, string fill, JustificationValues just)
        {
            return Zelle(text, breite, fett, fill, just, true,
                         WordBerichtGenerator.SCHRIFT_TABELLE);
        }

        /// <param name="uebersetzen">
        /// false = der Text kommt bereits aus <c>MyResource</c> und darf nicht noch
        /// einmal durch <c>BerichtTexte.T()</c> laufen (Etappe E7, siehe
        /// <see cref="MitStilRoh"/>).
        /// </param>
        /// <param name="schriftHalb">Schriftgröße in Halbpunkten.</param>
        public TableCell Zelle(string text, int breite, bool fett, string fill,
                               JustificationValues just, bool uebersetzen, int schriftHalb)
        {
            // Kopf-/Labelzellen (fett) durch die Berichtssprache übersetzen;
            // Datenzellen (nicht fett) bleiben unangetastet.
            if (fett && uebersetzen) text = BerichtTexte.T(text);
            var tc = new TableCell();
            var tcp = new TableCellProperties();
            tcp.Append(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = breite.ToString() });
            if (fill != null) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fill });
            tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            tc.Append(tcp);

            var p = new Paragraph();
            var pp = new ParagraphProperties();
            pp.Append(new SpacingBetweenLines { Before = "20", After = "20" });
            pp.Append(new Justification { Val = just });
            p.Append(pp);
            var rp = new RunProperties();
            if (fett) rp.Append(new Bold());
            rp.Append(new FontSize { Val = schriftHalb.ToString(CultureInfo.InvariantCulture) });
            var r = new Run();
            r.Append(rp);
            r.Append(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(r);
            tc.Append(p);
            return tc;
        }

        /// <summary>Zweispaltige Eigenschaftstabelle (Label · Wert) — z. B. Projektkopf.</summary>
        public void Eigenschaften(params string[] labelWertPaare)
        {
            int wLabel = 2800, wWert = Inhaltsbreite - 2800;
            Table t = NeueTabelle(new[] { wLabel, wWert });
            for (int i = 0; i + 1 < labelWertPaare.Length; i += 2)
            {
                var tr = new TableRow();
                tr.Append(Zelle(labelWertPaare[i], wLabel, true, WordBerichtGenerator.STAMM_FILL, JustificationValues.Left));
                tr.Append(Zelle(labelWertPaare[i + 1], wWert, false, null, JustificationValues.Left));
                t.Append(tr);
            }
            Fuege(t);
        }

        // ------------------------------------------------------------- Blocksplitting

        /// <summary>
        /// Zerlegt die Varianten (ohne Stamm) in Blöcke zu maximal MAX_VARIANTEN_JE_BLOCK;
        /// die Stamm-Spalte wird in jedem Block wiederholt (Konzept Kap. 5.1).
        /// Ohne Varianten liefert das genau einen Block mit leerer Liste (nur Stamm).
        /// </summary>
        public List<List<VariantenDaten>> VariantenBloecke(BerichtsDaten daten)
        {
            var varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            var bloecke = new List<List<VariantenDaten>>();
            if (varianten.Count == 0) { bloecke.Add(new List<VariantenDaten>()); return bloecke; }
            for (int i = 0; i < varianten.Count; i += MAX_VARIANTEN_JE_BLOCK)
                bloecke.Add(varianten.Skip(i).Take(MAX_VARIANTEN_JE_BLOCK).ToList());
            return bloecke;
        }

        // ------------------------------------------------------------- Formatierung

        public string F(double v, int dez) { return v.ToString("N" + dez, Kultur); }

        /// <summary>Kennzahlwert formatiert; null → „—" (nie 0, Konzept Kap. 5).</summary>
        public string FW(double? v, string format)
        { return v.HasValue ? v.Value.ToString(format, Kultur) : "—"; }

        /// <summary>Δ Variante − Stamm mit Vorzeichen (aus Rohwerten, Befund B7 behoben).</summary>
        public string Delta(double? stamm, double? variante, string format)
        {
            if (!stamm.HasValue || !variante.HasValue) return "—";
            double d = variante.Value - stamm.Value;
            string betrag = Math.Abs(d).ToString(format, Kultur);
            if (d > 0) return "+" + betrag;
            if (d < 0) return "−" + betrag;
            return "±" + 0.0.ToString(format, Kultur);
        }

        /// <summary>Δ in Prozent zum Stammwert ("+12,3 %"); „—" wenn nicht berechenbar.</summary>
        public string DeltaProzent(double? stamm, double? variante)
        {
            if (!stamm.HasValue || !variante.HasValue || Math.Abs(stamm.Value) < 1e-9) return "—";
            double p = (variante.Value - stamm.Value) / Math.Abs(stamm.Value) * 100.0;
            string betrag = Math.Abs(p).ToString("N1", Kultur) + " %";
            if (p > 0.05) return "+" + betrag;
            if (p < -0.05) return "−" + betrag;
            return "±0,0 %";
        }
    }

    // =======================================================================
    /// <summary>
    /// <b>Der Einfügeanker</b> (Konzept Berichtsvorlagen 4.3, 5.3): wohin die Bausteine über den
    /// <see cref="WordKontext"/> schreiben — ein Tripel aus Elternelement, Bezugselement und
    /// OpenXML-Teil. Eingefügt wird mit <c>Eltern.InsertBefore(neu, Bezug)</c>, ohne Bezug am
    /// Ende des Elternelements; die Bildteile hängen am <see cref="Teil"/> des Ankers statt
    /// pauschal am Hauptteil. Bei einem Kapitelplatzhalter ist der Bezug sein Absatz (oder sein
    /// Inhaltssteuerelement): Alles landet in Schreibreihenfolge davor, danach entfernt die
    /// Engine den Bezug.
    /// </summary>
    /// <summary>
    /// <b>Ein Diagramm als Word-Bild</b> (Etappe BV-E5, Konzept Berichtsvorlagen 6.5) — der zerlegte Rumpf von
    /// <c>WordKontext.BildTeile</c>: <see cref="BaueBlip"/> legt PNG und SVG als Bildteile an einem Teil an und
    /// baut den Blip mit PNG-Rückfall (Entscheid DG-E3-8); <see cref="Inline"/> baut die Zeichnung, die der
    /// Bausteinweg und ein getippter Bildplatzhalter einfügen. Ein Bildplatzhalter mit Rahmen bekommt nur
    /// den Blip — Lage, Umbruch, Rahmen und Drehung seiner Zeichnung bleiben.
    /// </summary>
    public static class Wordbilder
    {
        /// <summary>EMU je Bildpunkt bei 96 dpi.</summary>
        public const long EMU_JE_PIXEL = 9525L;

        /// <summary>
        /// Legt das PNG und — wenn gegeben — den SVG-Text als Bildteile an <paramref name="teil"/> an und
        /// baut den Blip: <c>r:embed</c> auf das PNG, das SVG über <c>asvg:svgBlip</c> in der
        /// Erweiterungsliste (<see cref="WordBerichtGenerator.SVG_EXT_URI"/>).
        /// </summary>
        public static A.Blip BaueBlip(OpenXmlPart teil, byte[] png, string svg)
        {
            if (png == null || png.Length == 0) throw new ArgumentException("Kein PNG.", nameof(png));
            ImagePart imgPart = NeuerBildteil(teil, ImagePartType.Png);
            using (var ms = new System.IO.MemoryStream(png)) imgPart.FeedData(ms);
            string relId = teil.GetIdOfPart(imgPart);

            // Der SVG-Teil: UTF-8 OHNE Vorzeichenfolge — ein BOM vor dem "<" macht
            // das Bild fuer manche Leser zu einer kaputten XML-Datei.
            string svgRelId = null;
            if (!string.IsNullOrEmpty(svg))
            {
                ImagePart svgPart = NeuerBildteil(teil, ImagePartType.Svg);
                byte[] roh = new System.Text.UTF8Encoding(false).GetBytes(svg);
                using (var ms = new System.IO.MemoryStream(roh)) svgPart.FeedData(ms);
                svgRelId = teil.GetIdOfPart(svgPart);
            }

            var blip = new A.Blip { Embed = relId };
            if (svgRelId != null)
                blip.Append(new A.BlipExtensionList(
                    new A.BlipExtension(
                        new DocumentFormat.OpenXml.Office2019.Drawing.SVG.SVGBlip { Embed = svgRelId })
                    { Uri = WordBerichtGenerator.SVG_EXT_URI }));
            return blip;
        }

        /// <summary>
        /// Die Zeichnung eines Bildes im Satz (<c>wp:inline</c>) mit dem Blip, in der Größe
        /// <paramref name="cx"/> × <paramref name="cy"/> EMU, Kennung <paramref name="id"/>; der Alternativtext
        /// <paramref name="beschreibung"/> nur, wenn gegeben.
        /// </summary>
        public static Drawing Inline(A.Blip blip, long cx, long cy, uint id, string beschreibung)
        {
            var docPr = new DW.DocProperties { Id = id, Name = "Diagramm" + id };
            var nvPr = new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Diagramm" + id + ".png" };
            if (!string.IsNullOrEmpty(beschreibung))
            {
                docPr.Description = beschreibung;
                nvPr.Description = beschreibung;
            }
            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = cx, Cy = cy },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    docPr,
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    nvPr,
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    blip,
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });
        }

        /// <summary>
        /// Ersetzt den Blip eines vorhandenen Bildes (Bildplatzhalter) durch <paramref name="neu"/>: Der alte
        /// Blip samt Erweiterungen geht, <c>a:srcRect</c> (Zuschnitt des Platzhalterbildes) auch; Füllart,
        /// Rahmen und Drehung bleiben. Rückgabe: die Beziehungskennungen, die der alte Blip nannte.
        /// </summary>
        public static List<string> ErsetzeBlip(A.Blip alt, A.Blip neu)
        {
            var ids = new List<string>();
            if (alt == null || neu == null) return ids;
            foreach (OpenXmlElement e in new[] { (OpenXmlElement)alt }.Concat(alt.Descendants()))
                foreach (OpenXmlAttribute a in e.GetAttributes())
                    if (a.NamespaceUri == "http://schemas.openxmlformats.org/officeDocument/2006/relationships" &&
                        !string.IsNullOrEmpty(a.Value))
                        ids.Add(a.Value);
            OpenXmlElement fuellung = alt.Parent;
            alt.InsertBeforeSelf(neu);
            alt.Remove();
            fuellung?.RemoveAllChildren<A.SourceRectangle>();
            return ids;
        }

        /// <summary>Ein neuer Bildteil an <paramref name="teil"/>.</summary>
        public static ImagePart NeuerBildteil(OpenXmlPart teil, PartTypeInfo typ)
        {
            switch (teil)
            {
                case MainDocumentPart m: return m.AddImagePart(typ);
                case HeaderPart h: return h.AddImagePart(typ);
                case FooterPart f: return f.AddImagePart(typ);
                case FootnotesPart fn: return fn.AddImagePart(typ);
                case EndnotesPart en: return en.AddImagePart(typ);
                default:
                    throw new InvalidOperationException(
                        "Der Einfügeanker liegt in einem Teil, der keine Bilder trägt: " + teil?.Uri);
            }
        }
    }

    public sealed class Einfuegeanker
    {
        /// <summary>Legt den Anker an; <paramref name="bezug"/> muss ein Kind von
        /// <paramref name="eltern"/> sein oder <c>null</c> (am Ende).</summary>
        public Einfuegeanker(OpenXmlElement eltern, OpenXmlElement bezug, OpenXmlPart teil)
        {
            Eltern = eltern ?? throw new ArgumentNullException(nameof(eltern));
            if (bezug != null && !ReferenceEquals(bezug.Parent, eltern))
                throw new ArgumentException("Das Bezugselement ist kein Kind des Elternelements.", nameof(bezug));
            Bezug = bezug;
            Teil = teil ?? throw new ArgumentNullException(nameof(teil));
        }

        /// <summary>Das Element, in das eingefügt wird (Rumpf, Inhalt eines Inhaltssteuerelements …).</summary>
        public OpenXmlElement Eltern { get; }

        /// <summary>Das Element, vor dem eingefügt wird; <c>null</c> = am Ende von <see cref="Eltern"/>.</summary>
        public OpenXmlElement Bezug { get; }

        /// <summary>Der OpenXML-Teil des Ankers — an ihm hängen die Bildteile.</summary>
        public OpenXmlPart Teil { get; }

        /// <summary>Vor dem Element <paramref name="bezug"/>, etwa dem Absatz eines Kapitelplatzhalters.</summary>
        public static Einfuegeanker Vor(OpenXmlElement bezug, OpenXmlPart teil)
        {
            if (bezug?.Parent == null) throw new ArgumentException("Das Bezugselement hängt in keinem Dokument.", nameof(bezug));
            return new Einfuegeanker(bezug.Parent, bezug, teil);
        }

        /// <summary>Am Ende des Rumpfs vor der letzten Abschnittsangabe — die Stelle des bisherigen Wegs.</summary>
        public static Einfuegeanker AmRumpfende(MainDocumentPart main)
        {
            Body rumpf = main.Document.Body;
            return new Einfuegeanker(rumpf, rumpf.Elements<SectionProperties>().LastOrDefault(), main);
        }

        internal void Fuege(OpenXmlElement el)
        {
            if (Bezug != null) Eltern.InsertBefore(el, Bezug);
            else Eltern.AppendChild(el);
        }

        /// <summary>
        /// Die Abschnittsangabe des Abschnitts, in dem der Anker steht: Word speichert sie am
        /// ENDE des Abschnitts — im letzten Absatz (<c>w:pPr/w:sectPr</c>) oder als letztes Kind des
        /// Rumpfs. Gesucht wird ab dem Rumpfkind, das den Anker enthält, vorwärts. <c>null</c> =
        /// der Anker liegt nicht im Rumpf oder das Dokument trägt keine.
        /// </summary>
        public SectionProperties Abschnitt()
        {
            OpenXmlElement ebene = Bezug ?? Eltern;
            if (ebene is Body rumpf)
                return rumpf.Elements<SectionProperties>().LastOrDefault();
            while (ebene != null && !(ebene.Parent is Body)) ebene = ebene.Parent;
            for (OpenXmlElement e = ebene; e != null; e = e.NextSibling())
            {
                if (e is SectionProperties s) return s;
                if (e is Paragraph p && p.ParagraphProperties?.SectionProperties != null)
                    return p.ParagraphProperties.SectionProperties;
                if (e is SdtBlock sdt)
                {
                    SectionProperties drin = sdt.Descendants<ParagraphProperties>()
                        .Select(pp => pp.SectionProperties).FirstOrDefault(sp => sp != null);
                    if (drin != null) return drin;
                }
            }
            return null;
        }
    }
}
