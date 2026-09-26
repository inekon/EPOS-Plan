using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Formatvorlagen als Rollen</b> (Konzept Berichtsvorlagen 6.2, Etappe BV-E1): Die Bausteine
    /// rufen weiter <c>WordKontext.MitStil("Title" | "Subtitle" | "Heading1" … "Heading3" |
    /// "Normal" | "Hinweis" | "Beschriftung")</c>; diese Namen sind hier ROLLEN, keine Stil-IDs.
    /// Je Dokument wird die Rolle auf die tatsächliche Stil-ID aufgelöst — eine im deutschen Word
    /// angelegte Vorlage führt übersetzte IDs (<c>berschrift1</c>, <c>Titel</c>, <c>Untertitel</c>,
    /// <c>Standard</c>), und mit festen IDs erschienen die Kapitel dort als Standardtext, das
    /// Inhaltsverzeichnis bliebe leer (Messprobe 5 von BV-E0).
    ///
    /// <para><b>Auflösung.</b> Eingebaute Vorlagen über <c>w:name</c> ohne Rücksicht auf Groß- und
    /// Kleinschreibung (<c>title</c>, <c>subtitle</c>, <c>heading 1</c> bis <c>heading 3</c>,
    /// <c>caption</c>) — Word schreibt den englischen Namen auch im deutschen Dokument —, der
    /// Standardabsatz über <c>w:default="1"</c> mit Rückfall über <c>w:name</c> „normal“ bzw.
    /// „Standard“; eigene über <c>w:name</c> (<c>Hinweis</c>, <c>Beschriftung</c>,
    /// <c>EPOS Kapitelkopf</c>, <c>EPOS Tabelle</c>). Trägt kein Stil den Namen, aber einer die
    /// ID der Rolle, gilt er (so entsteht keine zweite, kollidierende ID).</para>
    ///
    /// <para><b>Abstand.</b> Der leere Abstandsabsatz der Bausteine (bisher
    /// <c>Beschriftung(" ")</c>) ist die Rolle <see cref="ABSTAND"/>: der Stil „EPOS Abstand“, sonst
    /// der EIGENE Stil „Beschriftung“ der mitgelieferten Vorlagen — nie die eingebaute
    /// Beschriftung (<c>caption</c>), sonst stünden leere Einträge in einem Abbildungsverzeichnis.</para>
    ///
    /// <para><b>Anlegen.</b> Nur echt fehlende Stile legt die Engine an — eingebaute mit ihrem
    /// eingebauten Namen (Word ordnet sie darüber zu, auch im deutschen Word), eigene mit
    /// eindeutigem Namen und ID (<c>EPOS Hinweis</c>, <c>EPOS Abstand</c>); jede Anlage steht in
    /// <see cref="Angelegt"/> und wird im Füllergebnis als Hinweis genannt. Den Kapitelkopf legt die Engine nie
    /// an; das Tabellenformat „EPOS Tabelle“ (BV-E5) legt sie an, wenn eine Strukturtabelle es braucht — mit dem
    /// Aussehen der heutigen Direktformatierung (Rahmen, hinterlegte fette Kopfzeile, 9 pt).</para>
    /// </summary>
    public sealed class WordVorlagenstile
    {
        /// <summary>Rolle Titel (eingebaut <c>title</c>).</summary>
        public const string TITEL = "Title";

        /// <summary>Rolle Untertitel (eingebaut <c>subtitle</c>).</summary>
        public const string UNTERTITEL = "Subtitle";

        /// <summary>Rolle Überschrift 1 (eingebaut <c>heading 1</c>).</summary>
        public const string UEBERSCHRIFT1 = "Heading1";

        /// <summary>Rolle Überschrift 2 (eingebaut <c>heading 2</c>).</summary>
        public const string UEBERSCHRIFT2 = "Heading2";

        /// <summary>Rolle Überschrift 3 (eingebaut <c>heading 3</c>).</summary>
        public const string UEBERSCHRIFT3 = "Heading3";

        /// <summary>
        /// Die tiefste Überschriftenebene: Word kennt neun. Die Rollen <c>Heading4</c> bis
        /// <c>Heading9</c> braucht erst <c>|ebene n</c> (Konzept 4.8): Sie verschiebt die Überschriften
        /// eines Kapitels, Überschrift 1 wird Überschrift n, 2 wird n + 1 …, höchstens 9.
        /// </summary>
        public const int EBENE_MAX = 9;

        /// <summary>Vorsilbe der Überschriftenrollen (<c>Heading1</c> … <c>Heading9</c>).</summary>
        private const string ROLLE_UEBERSCHRIFT = "Heading";

        /// <summary>Rolle Standardabsatz (<c>w:default="1"</c>).</summary>
        public const string STANDARD = "Normal";

        /// <summary>Rolle Hinweis (eigener Stil „Hinweis“).</summary>
        public const string HINWEIS = "Hinweis";

        /// <summary>Rolle Beschriftung (eigener Stil „Beschriftung“, sonst eingebaut <c>caption</c>).</summary>
        public const string BESCHRIFTUNG = "Beschriftung";

        /// <summary>Rolle Abstand — der leere Absatz zwischen Tabellen und Abschnitten.</summary>
        public const string ABSTAND = "Abstand";

        /// <summary>Rolle Kapitelkopf (eigener Stil „EPOS Kapitelkopf“; nur gelesen).</summary>
        public const string KAPITELKOPF = "Kapitelkopf";

        /// <summary>Rolle Tabellenformat (eigener Tabellenstil „EPOS Tabelle“; nur gelesen, ab BV-E5).</summary>
        public const string TABELLE = "Tabelle";

        /// <summary>Name des angelegten Hinweisstils.</summary>
        public const string NAME_HINWEIS = "EPOS Hinweis";

        /// <summary>Name des angelegten Abstandsstils.</summary>
        public const string NAME_ABSTAND = "EPOS Abstand";

        /// <summary>Name des Kapitelkopfs der mitgelieferten Vorlagen.</summary>
        public const string NAME_KAPITELKOPF = "EPOS Kapitelkopf";

        /// <summary>Name des Tabellenformats (BV-E5).</summary>
        public const string NAME_TABELLE = "EPOS Tabelle";

        /// <summary>Alle Rollen in fester Reihenfolge; die Überschriften 4 bis 9 stehen am Ende.</summary>
        public static readonly IReadOnlyList<string> Rollen = new[]
        {
            TITEL, UNTERTITEL, UEBERSCHRIFT1, UEBERSCHRIFT2, UEBERSCHRIFT3, STANDARD,
            HINWEIS, BESCHRIFTUNG, ABSTAND, KAPITELKOPF, TABELLE,
            ROLLE_UEBERSCHRIFT + "4", ROLLE_UEBERSCHRIFT + "5", ROLLE_UEBERSCHRIFT + "6",
            ROLLE_UEBERSCHRIFT + "7", ROLLE_UEBERSCHRIFT + "8", ROLLE_UEBERSCHRIFT + "9",
        };

        /// <summary>Die Rolle der Überschrift der Ebene <paramref name="ebene"/> (1 bis 9, eingegrenzt): <c>Heading1</c> … <c>Heading9</c>.</summary>
        public static string Ueberschrift(int ebene)
        {
            int n = Math.Max(1, Math.Min(EBENE_MAX, ebene));
            return ROLLE_UEBERSCHRIFT + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Die Ebene einer Überschriftenrolle (<c>Heading1</c> → 1 … <c>Heading9</c> → 9); sonst <c>null</c>.</summary>
        public static int? Ueberschriftebene(string rolle)
        {
            if (rolle == null || rolle.Length != ROLLE_UEBERSCHRIFT.Length + 1 ||
                !rolle.StartsWith(ROLLE_UEBERSCHRIFT, StringComparison.Ordinal)) return null;
            int n = rolle[rolle.Length - 1] - '0';
            return n >= 1 && n <= EBENE_MAX ? n : (int?)null;
        }

        private readonly MainDocumentPart _main;
        private readonly Dictionary<string, string> _ids = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _angelegt = new List<string>();

        /// <summary>Löst die Rollen im Dokument <paramref name="main"/> auf.</summary>
        public WordVorlagenstile(MainDocumentPart main)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));
        }

        /// <summary>Die Namen der Stile, die die Engine angelegt hat, weil sie fehlten.</summary>
        public IReadOnlyList<string> Angelegt { get { return _angelegt; } }

        /// <summary>
        /// Die feste Stil-ID einer Rolle — der Weg ohne Auflösung (<c>WordKontext</c> mit
        /// Abschnittsangabe, Stilvorlage <c>Berichtsvorlage.docx</c> und Ersatzstile): die Rolle
        /// selbst, der Abstand ist die Beschriftung wie bisher.
        /// </summary>
        public static string FesteId(string rolle)
        {
            return string.Equals(rolle, ABSTAND, StringComparison.Ordinal) ? BESCHRIFTUNG : rolle;
        }

        /// <summary>
        /// Die Stil-ID der Rolle in diesem Dokument; ein echt fehlender Stil wird angelegt (nicht
        /// Kapitelkopf und Tabellenformat — dort <c>null</c>). Ein Name, der keine Rolle ist, geht
        /// unverändert als Stil-ID durch.
        /// </summary>
        public string Id(string rolle)
        {
            if (string.IsNullOrEmpty(rolle) || !Rollen.Contains(rolle)) return rolle;
            if (_ids.TryGetValue(rolle, out string id)) return id;

            id = Finde(rolle);
            if (id == null && rolle == TABELLE) id = LegeTabelleAn();
            else if (id == null && rolle != KAPITELKOPF) id = LegeAn(rolle);
            if (id != null) _ids[rolle] = id;
            return id;
        }

        /// <summary>Die Stil-ID der Rolle, ohne etwas anzulegen; <c>null</c> = fehlt im Dokument.</summary>
        public string Finde(string rolle)
        {
            if (_ids.TryGetValue(rolle ?? "", out string bekannt)) return bekannt;
            List<Style> stile = Stile();
            switch (rolle)
            {
                case TITEL: return NachName(stile, "title", Absatz) ?? NachId(stile, TITEL, Absatz);
                case UNTERTITEL: return NachName(stile, "subtitle", Absatz) ?? NachId(stile, UNTERTITEL, Absatz);
                case UEBERSCHRIFT1: return NachName(stile, "heading 1", Absatz) ?? NachId(stile, UEBERSCHRIFT1, Absatz);
                case UEBERSCHRIFT2: return NachName(stile, "heading 2", Absatz) ?? NachId(stile, UEBERSCHRIFT2, Absatz);
                case UEBERSCHRIFT3: return NachName(stile, "heading 3", Absatz) ?? NachId(stile, UEBERSCHRIFT3, Absatz);
                case STANDARD:
                    return stile.FirstOrDefault(s => Absatz(s) && s.Default?.Value == true)?.StyleId?.Value
                           ?? NachName(stile, "normal", Absatz) ?? NachName(stile, "standard", Absatz)
                           ?? NachId(stile, STANDARD, Absatz) ?? NachId(stile, "Standard", Absatz);
                case HINWEIS: return NachName(stile, "Hinweis", Absatz) ?? NachName(stile, NAME_HINWEIS, Absatz);
                case BESCHRIFTUNG: return NachName(stile, "Beschriftung", Absatz) ?? NachName(stile, "caption", Absatz);
                case ABSTAND: return NachName(stile, NAME_ABSTAND, Absatz) ?? NachName(stile, "Beschriftung", Absatz);
                case KAPITELKOPF: return NachName(stile, NAME_KAPITELKOPF, Absatz);
                case TABELLE: return NachName(stile, NAME_TABELLE, s => s.Type?.Value == StyleValues.Table);
                default:
                    {
                        // Überschrift 4 bis 9 (|ebene n): wie 1 bis 3 über den eingebauten Namen, sonst die ID.
                        int? ebene = Ueberschriftebene(rolle);
                        if (ebene == null) return null;
                        return NachName(stile, "heading " + ebene.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), Absatz)
                               ?? NachId(stile, rolle, Absatz);
                    }
            }
        }

        // ------------------------------------------------------------- Anlegen

        private string LegeAn(string rolle)
        {
            // Der Standardabsatz zuerst: Er ist die Grundlage jedes angelegten Stils.
            string standard = rolle == STANDARD ? null : Id(STANDARD);
            Style stil;
            switch (rolle)
            {
                case TITEL:
                    stil = Neu(EindeutigeId("Title"), "Title", standard, true, 2400, 240, null, true, false, "1F4E79", 56);
                    break;
                case UNTERTITEL:
                    stil = Neu(EindeutigeId("Subtitle"), "Subtitle", standard, false, null, 1200, null, false, false, "595959", 28);
                    break;
                case UEBERSCHRIFT1:
                    stil = Neu(EindeutigeId("Heading1"), "heading 1", standard, true, 360, 160, 0, true, false, "1F4E79", 30);
                    break;
                case UEBERSCHRIFT2:
                    stil = Neu(EindeutigeId("Heading2"), "heading 2", standard, true, 280, 120, 1, true, false, "1F4E79", 25);
                    break;
                case UEBERSCHRIFT3:
                    stil = Neu(EindeutigeId("Heading3"), "heading 3", standard, true, 200, 100, 2, true, false, "595959", 22);
                    break;
                case STANDARD:
                    stil = Neu(EindeutigeId("Normal"), "Normal", null, true, null, null, null, false, false, null, null);
                    // Standard nur, wenn es noch keinen Standardabsatz gibt — sonst wären es zwei.
                    if (!Stile().Any(s => Absatz(s) && s.Default?.Value == true)) stil.Default = OnOffValue.FromBoolean(true);
                    break;
                case HINWEIS:
                    stil = Neu(EindeutigeId("EPOSHinweis"), NAME_HINWEIS, standard, false, null, 100, null, false, true, "595959", 18);
                    break;
                case BESCHRIFTUNG:
                    stil = Neu(EindeutigeId("Caption"), "caption", standard, true, 60, 160, null, false, false, "595959", 18);
                    break;
                case ABSTAND:
                    stil = Neu(EindeutigeId("EPOSAbstand"), NAME_ABSTAND, standard, false, 60, 160, null, false, false, "595959", 18);
                    break;
                default:
                    {
                        // Überschrift 4 bis 9 wie Überschrift 3, mit der Gliederungsebene der Rolle.
                        int? ebene = Ueberschriftebene(rolle);
                        if (ebene == null || ebene.Value <= 3) return null;
                        string n = ebene.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        stil = Neu(EindeutigeId(ROLLE_UEBERSCHRIFT + n), "heading " + n, standard, true, 160, 80,
                                   ebene.Value - 1, true, false, "595959", 21);
                        break;
                    }
            }

            Styles wurzel = Stilwurzel();
            wurzel.AppendChild(stil);
            _angelegt.Add(stil.StyleName?.Val?.Value ?? stil.StyleId?.Value ?? rolle);
            return stil.StyleId.Value;
        }

        /// <summary>
        /// Legt das Tabellenformat „EPOS Tabelle“ an (BV-E5): Rahmen wie der Bausteinweg (<see cref="WordBerichtGenerator.RAHMEN"/>),
        /// Schrift 9 pt, die Kopfzeile fett auf <see cref="WordBerichtGenerator.HEAD_FILL"/>.
        /// </summary>
        private string LegeTabelleAn()
        {
            var s = new Style { Type = StyleValues.Table, StyleId = EindeutigeId("EPOSTabelle"), CustomStyle = true };
            s.Append(new StyleName { Val = NAME_TABELLE });
            s.Append(new PrimaryStyle());
            s.Append(new StyleParagraphProperties(new SpacingBetweenLines { Before = "20", After = "20" }));
            string g = WordBerichtGenerator.SCHRIFT_TABELLE.ToString(System.Globalization.CultureInfo.InvariantCulture);
            s.Append(new StyleRunProperties(new FontSize { Val = g }, new FontSizeComplexScript { Val = g }));
            s.Append(new StyleTableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new RightBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN })));
            s.Append(new TableStyleProperties(
                new RunPropertiesBaseStyle(new Bold(), new BoldComplexScript()),
                new TableStyleConditionalFormattingTableCellProperties(
                    new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = WordBerichtGenerator.HEAD_FILL }))
            { Type = TableStyleOverrideValues.FirstRow });

            Stilwurzel().AppendChild(s);
            _angelegt.Add(NAME_TABELLE);
            return s.StyleId.Value;
        }

        /// <summary>Ein Absatzstil in Schemafolge (name, basedOn, next, qFormat, pPr, rPr).</summary>
        private static Style Neu(string id, string name, string basisId, bool format, int? vor, int? nach,
                                 int? gliederung, bool fett, bool kursiv, string farbe, int? groesseHalb)
        {
            var s = new Style { Type = StyleValues.Paragraph, StyleId = id };
            s.Append(new StyleName { Val = name });
            if (basisId != null) s.Append(new BasedOn { Val = basisId });
            if (basisId != null) s.Append(new NextParagraphStyle { Val = basisId });
            if (format) s.Append(new PrimaryStyle());

            if (vor.HasValue || nach.HasValue || gliederung.HasValue)
            {
                var pp = new StyleParagraphProperties();
                if (vor.HasValue || nach.HasValue)
                {
                    var abstand = new SpacingBetweenLines();
                    if (vor.HasValue) abstand.Before = vor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (nach.HasValue) abstand.After = nach.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    pp.Append(abstand);
                }
                if (gliederung.HasValue) pp.Append(new OutlineLevel { Val = gliederung.Value });
                s.Append(pp);
            }

            if (fett || kursiv || farbe != null || groesseHalb.HasValue)
            {
                var rp = new StyleRunProperties();
                if (fett) { rp.Append(new Bold()); rp.Append(new BoldComplexScript()); }
                if (kursiv) { rp.Append(new Italic()); rp.Append(new ItalicComplexScript()); }
                if (farbe != null) rp.Append(new Color { Val = farbe });
                if (groesseHalb.HasValue)
                {
                    string g = groesseHalb.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    rp.Append(new FontSize { Val = g });
                    rp.Append(new FontSizeComplexScript { Val = g });
                }
                s.Append(rp);
            }
            return s;
        }

        private string EindeutigeId(string basis)
        {
            var belegt = new HashSet<string>(Stile().Select(s => s.StyleId?.Value).Where(i => i != null),
                                             StringComparer.OrdinalIgnoreCase);
            if (!belegt.Contains(basis)) return basis;
            for (int i = 1; ; i++)
                if (!belegt.Contains(basis + i)) return basis + i;
        }

        private Styles Stilwurzel()
        {
            StyleDefinitionsPart teil = _main.StyleDefinitionsPart ?? _main.AddNewPart<StyleDefinitionsPart>();
            if (teil.Styles == null) teil.Styles = new Styles();
            return teil.Styles;
        }

        private List<Style> Stile()
        {
            Styles wurzel = _main.StyleDefinitionsPart?.Styles;
            return wurzel == null ? new List<Style>() : wurzel.Elements<Style>().ToList();
        }

        private static bool Absatz(Style s)
        {
            return s.Type == null || s.Type.Value == StyleValues.Paragraph;
        }

        private static string NachName(List<Style> stile, string name, Func<Style, bool> art)
        {
            return stile.FirstOrDefault(s => art(s) &&
                string.Equals(s.StyleName?.Val?.Value?.Trim(), name, StringComparison.OrdinalIgnoreCase))?.StyleId?.Value;
        }

        private static string NachId(List<Style> stile, string id, Func<Style, bool> art)
        {
            return stile.FirstOrDefault(s => art(s) &&
                string.Equals(s.StyleId?.Value, id, StringComparison.Ordinal))?.StyleId?.Value;
        }
    }
}
