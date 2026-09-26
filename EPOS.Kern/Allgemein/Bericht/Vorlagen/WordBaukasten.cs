using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Wordprocessing;
using SkiaSharp;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Die Abschnitte des Baukastens in ihrer Reihenfolge (Konzept Berichtsvorlagen 4.7, 6.3 Nr. 3).</summary>
    public enum Baukastenabschnittsart
    {
        /// <summary>Der Bericht als Ganzes — auch die Kapitel und die Schalter der Häkchen.</summary>
        Bericht,

        /// <summary>Die Installation, die den Bericht erstellt (<c>ersteller.*</c>).</summary>
        Installation,

        /// <summary>Stammdaten und Ergebnisse des Stammprojekts.</summary>
        Stamm,

        /// <summary>Über alle gewählten Stände — ohne den Paarvergleich.</summary>
        Gruppe,

        /// <summary>Der laufende Stand, im fertigen Block <c>{{#je stand}}</c> … <c>{{/je}}</c>.</summary>
        Stand,

        /// <summary>Das laufende Gebäude, im fertigen Block <c>{{#je gebaeude}}</c> … <c>{{/je}}</c>.</summary>
        Gebaeude,

        /// <summary>
        /// Der Paarvergleich <c>stand.a.*</c>/<c>stand.b.*</c> (Sicht 2) — nur als MUSTER (Anwenderentscheid BV-E5-4,
        /// Lesart b): wenige Beispiele als Text ohne Klammern und die Regel, dass jeder Standschlüssel (ohne Bilder und
        /// Tabellen) seinen Zwilling hat.
        /// </summary>
        Paarsicht,

        /// <summary>
        /// Die Stände nach Position <c>stand.&lt;n&gt;.*</c>/<c>variante.&lt;n&gt;.*</c> (BV-E9, ab Katalogfassung 7) — wie
        /// der Paarvergleich nur als MUSTER: die zwei Musterschlüssel und je ein Beispiel als Text ohne Klammern.
        /// </summary>
        Positionen,

        /// <summary>Die Mustertabelle der Tabellenrollen, einmal (Konzept 6.4 Nr. 2).</summary>
        Mustertabelle,
    }

    /// <summary>Ein Abschnitt des Baukastens: seine Art und seine Einträge in Folge (nach Art, dann Katalogfolge).</summary>
    public sealed class Baukastenabschnitt
    {
        internal Baukastenabschnitt(Baukastenabschnittsart art, IReadOnlyList<Vorlagenfeld> eintraege)
        {
            Art = art;
            Eintraege = eintraege;
        }

        /// <summary>Welcher Abschnitt.</summary>
        public Baukastenabschnittsart Art { get; }

        /// <summary>Die Einträge des Abschnitts in der Folge des Baukastens.</summary>
        public IReadOnlyList<Vorlagenfeld> Eintraege { get; }
    }

    /// <summary>
    /// <b>Der Baukasten der Word-Vorlagen</b> (Konzept Berichtsvorlagen 6.3 Nr. 3, 12; Etappe BV-E5) — ein
    /// Word-Dokument, das der Kern <b>aus dem Katalog erzeugt</b>: jeder Eintrag mit Ausgabe Word und
    /// <see cref="Vorlagenfeld.Seit"/> ≤ der gewählten Fassung, gegliedert nach Kontext — Bericht,
    /// Installation, Stamm, Gruppe als Liste; Stand und Gebäude in fertigen Blöcken
    /// (<c>{{#je stand}}</c>, <c>{{#je gebaeude}}</c>); der Paarvergleich; die Mustertabelle
    /// <c>{{muster.tabelle}}</c> einmal. Plattformfrei (OpenXML und SkiaSharp), ohne Datenbank und ohne
    /// Datei — der Aufrufer bekommt die Bytes; zweimal erzeugt ist der Inhalt gleich.
    ///
    /// <para><b>Je Eintrag</b> ein Beschreibungsabsatz (Beschreibung in der Sprache des Laufs, dahinter der
    /// Schlüssel grau) und darunter der Platzhalter in der Form, die seine Art verlangt (Konzept 4.2, 4.3):
    /// Text, Zahl und Datum als <c>{{schlüssel}}</c>; Tabelle, Liste und Kapitel allein im Absatz; ein Bild
    /// als neutrales <b>Musterbild</b> mit dem Schlüssel im Alternativtext — volle Satzspiegelbreite, je Stand
    /// halbe; ein Schalter in einem Muster <c>{{#wenn …}}</c>/<c>{{#wenn nicht …}}</c>. Jeder Lauf mit einem
    /// Platzhalter trägt <c>w:noProof</c> gegen die Rechtschreibprüfung.</para>
    ///
    /// <para><b>Gefüllt</b> ergibt der Baukasten einen Probebericht mit jedem Wert (Rundlauf, Konzept 12:
    /// „füllt ohne Prüferfehler; kein <c>{{</c> bleibt übrig“). Der Paarvergleich gilt in Sicht 2 oder mit
    /// genau einer Variante; in Sicht 1 mit mehreren Varianten meldet der Prüfer ihn (Konzept 4.7).</para>
    ///
    /// <para><b>Nicht hier:</b> der Excel-Baukasten — er kommt mit der Ausgabe Excel (BV-E7); die Form als
    /// Inhaltssteuerelement nennt der Einleitungsabsatz, der Baukasten führt die getippte Form.</para>
    /// </summary>
    public static class WordBaukasten
    {
        /// <summary>Die Dokumenteigenschaft mit der Art der Vorlage (wie die Standardvorlage: <c>standard</c>).</summary>
        public const string EIGENSCHAFT_VORLAGE = "EPOS.Vorlage";

        /// <summary>Die Art des Baukastens in <see cref="EIGENSCHAFT_VORLAGE"/>.</summary>
        public const string VORLAGENART = "baukasten";

        /// <summary>Der Formatbezeichner der eigenen Dokumenteigenschaften (wie Word ihn schreibt).</summary>
        private const string FORMAT_EIGENE = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

        /// <summary>A4 hoch, Ränder oben/unten 25/20 mm, links/rechts 25/20 mm: Inhaltsbreite 9 355 DXA.</summary>
        private const int SEITE_B = 11906, SEITE_H = 16838, RAND_L = 1417, RAND_R = 1134, RAND_O = 1417, RAND_U = 1134;

        /// <summary>EMU je DXA (635).</summary>
        private const long EMU_JE_DXA = 635L;

        /// <summary>Das Musterbild in voller Breite: 620 × 280 Bildpunkte (Seitenverhältnis der Berichtsbilder).</summary>
        private const int VOLL_B = 620, VOLL_H = 280;

        /// <summary>Das Musterbild in halber Breite: 311 × 200 Bildpunkte.</summary>
        private const int HALB_B = 311, HALB_H = 200;

        /// <summary>Das Musterbild des Logos: 150 × 82 Bildpunkte wie das Platzhalterbild der Standardvorlage.</summary>
        private const int LOGO_B = 150, LOGO_H = 82;

        /// <summary>Die Folge der Arten innerhalb eines Abschnitts.</summary>
        private static readonly Vorlagenfeldart[] Artfolge =
        {
            Vorlagenfeldart.Text, Vorlagenfeldart.Zahl, Vorlagenfeldart.Datum, Vorlagenfeldart.Liste,
            Vorlagenfeldart.Tabelle, Vorlagenfeldart.Bild, Vorlagenfeldart.Kapitel, Vorlagenfeldart.Schalter,
            Vorlagenfeldart.Blatt,
        };

        // =====================================================================
        //  Gliederung
        // =====================================================================

        /// <summary>
        /// Die Gliederung des Baukastens der Fassung <paramref name="fassung"/>: jeder Eintrag des Katalogs mit
        /// Ausgabe Word und <see cref="Vorlagenfeld.Seit"/> ≤ <paramref name="fassung"/> genau einmal, in den
        /// Abschnitten nach Kontext; leere Abschnitte fehlen. <b>Ausnahme Paarsicht</b> (BV-E5-4): Die Zwillinge
        /// <c>stand.a.*</c>/<c>stand.b.*</c> deckt die Musterregel — der Abschnitt führt nur die
        /// <see cref="Paarbeispiele"/>, und sie stehen als Text, nicht als Platzhalter.
        /// </summary>
        public static IReadOnlyList<Baukastenabschnitt> Abschnitte(int fassung)
        {
            List<Vorlagenfeld> alle = Vorlagenfeldkatalog.Alle
                .Where(f => f != null && f.Seit <= fassung && (f.Ausgaben & Vorlagenausgabe.Word) != 0)
                .ToList();

            var abschnitte = new List<Baukastenabschnitt>();
            foreach (Baukastenabschnittsart art in Enum.GetValues(typeof(Baukastenabschnittsart)))
            {
                List<Vorlagenfeld> eintraege = alle.Where(f => Abschnitt(f) == art).ToList();
                if (art == Baukastenabschnittsart.Paarsicht) eintraege = Paarbeispiele(eintraege);
                if (art == Baukastenabschnittsart.Positionen) eintraege = Positionsbeispiele(fassung);
                // Nach Art ordnen; innerhalb einer Art bleibt die Katalogfolge (stabil sortiert).
                eintraege = eintraege.Select((f, i) => (f, i))
                    .OrderBy(x => Array.IndexOf(Artfolge, x.f.Art)).ThenBy(x => x.i)
                    .Select(x => x.f).ToList();
                if (eintraege.Count > 0) abschnitte.Add(new Baukastenabschnitt(art, eintraege));
            }
            return abschnitte;
        }

        /// <summary>
        /// Die Beispiele des Paarvergleichs: ein Text, eine Kennzahl und eine Zeile der Wirtschaftlichkeit — je der
        /// erste Eintrag seiner Art, die Kennzahl als Stand B, die übrigen als Stand A. Mehr braucht es nicht: jeder
        /// Standschlüssel hat seinen Zwilling (Regel im Hinweis des Abschnitts).
        /// </summary>
        private static List<Vorlagenfeld> Paarbeispiele(List<Vorlagenfeld> paare)
        {
            var wahl = new List<Vorlagenfeld>();
            void Nimm(Func<Vorlagenfeld, bool> passt)
            {
                Vorlagenfeld f = paare.FirstOrDefault(x => passt(x) && !wahl.Contains(x));
                if (f != null) wahl.Add(f);
            }
            Nimm(f => f.Schluessel.StartsWith("stand.a.", StringComparison.Ordinal) && f.Art == Vorlagenfeldart.Text);
            Nimm(f => f.Schluessel.StartsWith("stand.b.kennzahl.", StringComparison.Ordinal) && f.Art == Vorlagenfeldart.Zahl);
            Nimm(f => f.Schluessel.StartsWith("stand.a.wirtschaft.", StringComparison.Ordinal) && f.Art == Vorlagenfeldart.Zahl);
            if (wahl.Count == 0 && paare.Count > 0) wahl.Add(paare[0]);
            return wahl;
        }

        /// <summary>
        /// Die Beispiele der Positionsadressierung (BV-E9): je Muster sein Beispiel (<see cref="Vorlagenfeldmuster.Beispiel"/>),
        /// aufgelöst über den Katalog; vor Fassung 7 keine.
        /// </summary>
        private static List<Vorlagenfeld> Positionsbeispiele(int fassung)
        {
            if (fassung < Vorlagenfeldkatalog.FASSUNG_POSITION) return new List<Vorlagenfeld>();
            return Vorlagenfeldkatalog.Positionsmuster.Select(m => Vorlagenfeldkatalog.Finde(m.Beispiel))
                .Where(f => f != null && (f.Ausgaben & Vorlagenausgabe.Word) != 0).ToList();
        }

        /// <summary>Der Abschnitt eines Eintrags.</summary>
        public static Baukastenabschnittsart Abschnitt(Vorlagenfeld feld)
        {
            if (feld == null) throw new ArgumentNullException(nameof(feld));
            if (string.Equals(feld.Schluessel, Vorlagenfeldkatalog.MUSTER_TABELLE, StringComparison.Ordinal))
                return Baukastenabschnittsart.Mustertabelle;
            if (Vorlagenpruefer.IstPaarschluessel(feld.Schluessel)) return Baukastenabschnittsart.Paarsicht;
            if (Vorlagenfeldkatalog.IstPositionsschluessel(feld.Schluessel, out _, out _, out _)) return Baukastenabschnittsart.Positionen;
            switch (feld.Kontext)
            {
                case Vorlagenfeldkontext.Installation: return Baukastenabschnittsart.Installation;
                case Vorlagenfeldkontext.Stamm: return Baukastenabschnittsart.Stamm;
                case Vorlagenfeldkontext.Gruppe: return Baukastenabschnittsart.Gruppe;
                case Vorlagenfeldkontext.Stand: return Baukastenabschnittsart.Stand;
                case Vorlagenfeldkontext.Gebaeude: return Baukastenabschnittsart.Gebaeude;
                default: return Baukastenabschnittsart.Bericht;
            }
        }

        // =====================================================================
        //  Erzeugen
        // =====================================================================

        /// <summary>Der Baukasten in der Sprache der Kultur <paramref name="kultur"/> (Englisch, wenn sie englisch ist, sonst Deutsch).</summary>
        public static byte[] Erzeuge(CultureInfo kultur, int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG)
        {
            bool englisch = kultur != null && string.Equals(kultur.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase);
            return Erzeuge(englisch, fassung);
        }

        /// <summary>
        /// <b>Erzeugt den Baukasten</b> der Katalogfassung <paramref name="fassung"/> (Vorgabe: die laufende) auf
        /// Deutsch oder Englisch — die Bytes einer <c>.docx</c> mit <c>EPOS.Katalogfassung</c>,
        /// <c>EPOS.Vorlage</c> = <see cref="VORLAGENART"/> und <c>EPOS.Sprache</c> in <c>custom.xml</c>.
        /// </summary>
        public static byte[] Erzeuge(bool englisch, int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG)
        {
            if (fassung < 1) throw new ArgumentOutOfRangeException(nameof(fassung), fassung, "Katalogfassung ab 1");
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            IReadOnlyList<Baukastenabschnitt> abschnitte = Abschnitte(fassung);

            using var ms = new MemoryStream();
            using (WordprocessingDocument doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                Stile(main);
                var bau = new Bau(main, kultur, englisch);
                var body = new Body();

                body.Append(Absatz(WordVorlagenstile.TITEL, Lauf(T(nameof(R.VF_BAUKASTEN_TITEL), kultur))));
                body.Append(Absatz(WordVorlagenstile.STANDARD,
                    Lauf(T(nameof(R.VF_BAUKASTEN_EINLEITUNG), kultur, fassung, abschnitte.Sum(a => a.Eintraege.Count)))));
                foreach (Baukastenabschnitt a in abschnitte) bau.Abschnitt(body, a);

                body.Append(new SectionProperties(
                    new PageSize { Width = (UInt32Value)(uint)SEITE_B, Height = (UInt32Value)(uint)SEITE_H },
                    new PageMargin
                    {
                        Top = RAND_O, Right = (UInt32Value)(uint)RAND_R, Bottom = RAND_U, Left = (UInt32Value)(uint)RAND_L,
                        Header = 708U, Footer = 708U, Gutter = 0U,
                    }));
                main.Document = new Document(body);
                main.Document.Save();
                Eigenschaften(doc, fassung, englisch);
            }
            return ms.ToArray();
        }

        /// <summary>Der Bau eines Dokuments: Bildteile, Kennungen der Zeichnungen und die Texte einer Sprache.</summary>
        private sealed class Bau
        {
            private readonly MainDocumentPart _main;
            private readonly CultureInfo _kultur;
            private readonly bool _englisch;
            private readonly Dictionary<(int, int), string> _bilder = new Dictionary<(int, int), string>();
            private uint _kennung;

            public Bau(MainDocumentPart main, CultureInfo kultur, bool englisch)
            {
                _main = main;
                _kultur = kultur;
                _englisch = englisch;
            }

            public void Abschnitt(Body body, Baukastenabschnitt a)
            {
                body.Append(Absatz(WordVorlagenstile.UEBERSCHRIFT1, Lauf(Titel(a.Art))));
                string hinweis = Hinweis(a.Art);
                if (hinweis != null) body.Append(Absatz(WordVorlagenstile.HINWEIS, Lauf(hinweis)));

                switch (a.Art)
                {
                    case Baukastenabschnittsart.Stand:
                        body.Append(Marke("{{#je stand}}"));
                        body.Append(Absatz(WordVorlagenstile.UEBERSCHRIFT2, Platzhalterlauf("{{stand.anzeige}}")));
                        Eintraege(body, a.Eintraege, WordVorlagenstile.UEBERSCHRIFT3, true);
                        body.Append(Marke("{{/je}}"));
                        break;
                    case Baukastenabschnittsart.Gebaeude:
                        body.Append(Marke("{{#je gebaeude}}"));
                        body.Append(Absatz(WordVorlagenstile.UEBERSCHRIFT2, Platzhalterlauf("{{gebaeude.name}}")));
                        Eintraege(body, a.Eintraege, WordVorlagenstile.UEBERSCHRIFT3, false);
                        body.Append(Marke("{{/je}}"));
                        break;
                    case Baukastenabschnittsart.Paarsicht:
                        // Nur Beschreibung und Schlüssel als TEXT: Ein Platzhalter stand.a.*/stand.b.* wäre in Sicht 1
                        // mit mehreren Varianten ein Prüferfehler — so bleibt der Baukasten in jeder Sicht füllbar.
                        foreach (Vorlagenfeld f in a.Eintraege) body.Append(Beschreibung(f));
                        break;
                    case Baukastenabschnittsart.Positionen:
                        // Wie die Paarsicht nur als TEXT: die Muster mit ihrer Beschreibung, dann je ein Beispiel — ein
                        // Platzhalter nach Position bliebe in einem Lauf mit weniger Ständen leer.
                        foreach (Vorlagenfeldmuster m in Vorlagenfeldkatalog.Positionsmuster) body.Append(Musterbeschreibung(m));
                        foreach (Vorlagenfeld f in a.Eintraege) body.Append(Beschreibung(f));
                        break;
                    case Baukastenabschnittsart.Mustertabelle:
                        foreach (Vorlagenfeld f in a.Eintraege)
                        {
                            body.Append(Beschreibung(f));
                            body.Append(Mustertabelle(_kultur));
                            body.Append(Absatz(WordVorlagenstile.STANDARD));
                        }
                        break;
                    default:
                        Eintraege(body, a.Eintraege, WordVorlagenstile.UEBERSCHRIFT2, false);
                        break;
                }
            }

            /// <summary>Die Einträge eines Abschnitts, je Art unter einer Zwischenüberschrift.</summary>
            private void Eintraege(Body body, IReadOnlyList<Vorlagenfeld> eintraege, string stil, bool halb)
            {
                foreach (IGrouping<Vorlagenfeldart, Vorlagenfeld> gruppe in eintraege.GroupBy(f => f.Art))
                {
                    body.Append(Absatz(stil, Lauf(Arttext(gruppe.Key))));
                    foreach (Vorlagenfeld f in gruppe) Eintrag(body, f, halb);
                }
            }

            /// <summary>Ein Eintrag: Beschreibungsabsatz und der Platzhalter in der Form seiner Art.</summary>
            private void Eintrag(Body body, Vorlagenfeld f, bool halb)
            {
                string marke = "{{" + f.Schluessel + "}}";
                body.Append(Beschreibung(f));
                switch (f.Art)
                {
                    case Vorlagenfeldart.Bild:
                        body.Append(new Paragraph(new Run(Musterbild(f.Schluessel, halb))));
                        break;
                    case Vorlagenfeldart.Schalter:
                        string text = Entschaerft(Vorlagenfeldkatalog.Beschreibung(f, _englisch));
                        body.Append(Marke("{{#wenn " + f.Schluessel + "}}"));
                        body.Append(Absatz(WordVorlagenstile.STANDARD, Lauf(text + ": " + T(nameof(R.VF_BAUKASTEN_SCHALTER_JA), _kultur))));
                        body.Append(Marke("{{/wenn}}"));
                        body.Append(Marke("{{#wenn nicht " + f.Schluessel + "}}"));
                        body.Append(Absatz(WordVorlagenstile.STANDARD, Lauf(text + ": " + T(nameof(R.VF_BAUKASTEN_SCHALTER_NEIN), _kultur))));
                        body.Append(Marke("{{/wenn}}"));
                        break;
                    default:
                        // Text, Zahl, Datum im Satz; Tabelle, Liste, Kapitel allein im Absatz — beides ein eigener Absatz.
                        body.Append(Absatz(WordVorlagenstile.STANDARD, Platzhalterlauf(marke)));
                        break;
                }
            }

            /// <summary>Der Beschreibungsabsatz: Beschreibung, dahinter der Schlüssel grau (ohne Klammern — kein Platzhalter).</summary>
            private Paragraph Beschreibung(Vorlagenfeld f)
            {
                string text = Entschaerft(Vorlagenfeldkatalog.Beschreibung(f, _englisch));
                var p = Absatz(WordVorlagenstile.HINWEIS, Lauf(text.Length > 0 ? text + " · " : ""));
                var schluessel = new Run(new RunProperties(new NoProof(), new Color { Val = "7F7F7F" }),
                                         new Text(f.Schluessel) { Space = SpaceProcessingModeValues.Preserve });
                p.Append(schluessel);
                p.ParagraphProperties.Append(new KeepNext());
                return p;
            }

            /// <summary>Ein Musterschlüssel mit Parameter (BV-E9): seine Beschreibung, dahinter das Muster grau.</summary>
            private Paragraph Musterbeschreibung(Vorlagenfeldmuster m)
            {
                string text = Entschaerft(m.Beschreibung(_englisch));
                var p = Absatz(WordVorlagenstile.HINWEIS, Lauf(text.Length > 0 ? text + " · " : ""));
                p.Append(new Run(new RunProperties(new NoProof(), new Color { Val = "7F7F7F" }),
                                 new Text(m.Muster) { Space = SpaceProcessingModeValues.Preserve }));
                p.ParagraphProperties.Append(new KeepNext());
                return p;
            }

            /// <summary>Das neutrale Musterbild mit dem Schlüssel im Alternativtext.</summary>
            private Drawing Musterbild(string schluessel, bool halb)
            {
                bool logo = string.Equals(schluessel, Vorlagenfeldkatalog.LOGO, StringComparison.Ordinal);
                int b = logo ? LOGO_B : halb ? HALB_B : VOLL_B;
                int h = logo ? LOGO_H : halb ? HALB_H : VOLL_H;
                long inhalt = (SEITE_B - RAND_L - RAND_R) * EMU_JE_DXA;
                long cx = logo ? b * Wordbilder.EMU_JE_PIXEL : halb ? inhalt / 2 : inhalt;
                long cy = logo ? h * Wordbilder.EMU_JE_PIXEL : cx * h / b;

                if (!_bilder.TryGetValue((b, h), out string relId))
                {
                    // Feste Beziehungskennung — zweimal erzeugt, derselbe Rumpf.
                    ImagePart teil = _main.AddImagePart(ImagePartType.Png, "rIdMusterbild" + (_bilder.Count + 1).ToString(CultureInfo.InvariantCulture));
                    using (var s = new MemoryStream(MusterPng(b, h))) teil.FeedData(s);
                    relId = _main.GetIdOfPart(teil);
                    _bilder[(b, h)] = relId;
                }
                var blip = new DocumentFormat.OpenXml.Drawing.Blip { Embed = relId };
                Drawing zeichnung = Wordbilder.Inline(blip, cx, cy, ++_kennung, "{{" + schluessel + "}}");
                var docPr = zeichnung.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().First();
                docPr.Name = "Baukasten " + schluessel;
                return zeichnung;
            }

            private string Titel(Baukastenabschnittsart art)
            {
                switch (art)
                {
                    case Baukastenabschnittsart.Paarsicht: return T(nameof(R.VF_BAUKASTEN_PAARSICHT), _kultur);
                    case Baukastenabschnittsart.Positionen: return T(nameof(R.VF_BAUKASTEN_POSITIONEN), _kultur);
                    case Baukastenabschnittsart.Mustertabelle: return T(nameof(R.VF_BAUKASTEN_MUSTERTABELLE), _kultur);
                    default:
                        string t = T("VF_KATALOG_KONTEXT_" + art.ToString().ToUpperInvariant(), _kultur);
                        return t.Length > 0 ? char.ToUpper(t[0], _kultur) + t.Substring(1) : art.ToString();
                }
            }

            private string Hinweis(Baukastenabschnittsart art)
            {
                switch (art)
                {
                    case Baukastenabschnittsart.Stand: return T(nameof(R.VF_BAUKASTEN_HINWEIS_STAND), _kultur);
                    case Baukastenabschnittsart.Gebaeude: return T(nameof(R.VF_BAUKASTEN_HINWEIS_GEBAEUDE), _kultur);
                    case Baukastenabschnittsart.Paarsicht: return T(nameof(R.VF_BAUKASTEN_HINWEIS_PAAR), _kultur);
                    case Baukastenabschnittsart.Positionen: return T(nameof(R.VF_BAUKASTEN_HINWEIS_POSITION), _kultur);
                    case Baukastenabschnittsart.Mustertabelle: return T(nameof(R.VF_BAUKASTEN_HINWEIS_MUSTER), _kultur);
                    default: return null;
                }
            }

            private string Arttext(Vorlagenfeldart art)
            {
                string t = T("VF_KATALOG_ART_" + art.ToString().ToUpperInvariant(), _kultur);
                return t.Length > 0 ? t : art.ToString();
            }
        }

        // =====================================================================
        //  Bausteine
        // =====================================================================

        /// <summary>
        /// Die Mustertabelle (Konzept 6.4 Nr. 2): Alternativtext <c>{{muster.tabelle}}</c>, je Rolle eine Zelle mit
        /// Schattierung und Zeichenformat — Stamm, Gruppe, Summe, Warnung. Die Engine liest sie und entfernt sie. Auch die
        /// Bausteinvorlage (<see cref="WordBausteinvorlage"/>) bietet sie so an.
        /// </summary>
        internal static Table Mustertabelle(CultureInfo kultur)
        {
            int breite = (SEITE_B - RAND_L - RAND_R) / 4;
            var zeile = new TableRow(
                Zelle(T(nameof(R.VF_BAUKASTEN_ROLLE_STAMM), kultur), "DEEAF6", null, false, breite),
                Zelle(T(nameof(R.VF_BAUKASTEN_ROLLE_GRUPPE), kultur), "F2F2F2", null, true, breite),
                Zelle(T(nameof(R.VF_BAUKASTEN_ROLLE_SUMME), kultur), "E7E6E6", null, true, breite),
                Zelle(T(nameof(R.VF_BAUKASTEN_ROLLE_WARNUNG), kultur), "FFF2CC", "C00000", false, breite));
            var rand = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new RightBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" });
            var eigenschaften = new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                rand,
                new TableDescription { Val = "{{" + Vorlagenfeldkatalog.MUSTER_TABELLE + "}}" });
            var raster = new TableGrid(Enumerable.Range(0, 4).Select(_ => new GridColumn { Width = breite.ToString(CultureInfo.InvariantCulture) }));
            return new Table(eigenschaften, raster, zeile);
        }

        private static TableCell Zelle(string text, string fuellung, string farbe, bool fett, int breite)
        {
            var rp = new RunProperties();
            if (fett) rp.Append(new Bold());
            if (farbe != null) rp.Append(new Color { Val = farbe });
            var zelle = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = breite.ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa },
                    new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fuellung }),
                new Paragraph(new Run(rp, new Text(text ?? ""))));
            return zelle;
        }

        /// <summary>Ein Blockmarke allein im Absatz (<c>{{#je stand}}</c>, <c>{{/wenn}}</c> …), ohne Prüfung.</summary>
        private static Paragraph Marke(string marke)
        {
            return Absatz(WordVorlagenstile.STANDARD, Platzhalterlauf(marke));
        }

        /// <summary>Ein Lauf mit einem Platzhalter: <c>w:noProof</c> gegen die Rechtschreibprüfung (Konzept 6.3 Nr. 3).</summary>
        internal static Run Platzhalterlauf(string marke)
        {
            return new Run(new RunProperties(new NoProof()), new Text(marke) { Space = SpaceProcessingModeValues.Preserve });
        }

        private static Run Lauf(string text)
        {
            return new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
        }

        private static Paragraph Absatz(string stil, params OpenXmlElement[] inhalt)
        {
            var p = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = stil }));
            foreach (OpenXmlElement e in inhalt) p.Append(e);
            return p;
        }

        /// <summary>
        /// Ein Beschreibungstext darf keine Marke bilden — sonst läse die Engine ihn als Platzhalter (die Schalter
        /// nennen „nur als Bedingung in {{#wenn …}}“). Die doppelten geschweiften Klammern fallen weg:
        /// „#wenn …“ bleibt lesbar und ist keine Marke.
        /// </summary>
        internal static string Entschaerft(string text)
        {
            return (text ?? "").Replace("{{", "").Replace("}}", "");
        }

        /// <summary>
        /// Das neutrale Musterbild: hellgraue Fläche mit Rahmen und fünf Balken, ohne Schrift — auf jeder
        /// Plattform dasselbe Bild. Die Größe folgt dem Rahmen.
        /// </summary>
        internal static byte[] MusterPng(int breite, int hoehe)
        {
            using (var flaeche = SKSurface.Create(new SKImageInfo(breite, hoehe, SKColorType.Rgba8888, SKAlphaType.Premul)))
            {
                SKCanvas g = flaeche.Canvas;
                g.Clear(new SKColor(0xF2, 0xF2, 0xF2));
                using (var rahmen = new SKPaint { Color = new SKColor(0xBF, 0xBF, 0xBF), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = false })
                    g.DrawRect(1, 1, breite - 2, hoehe - 2, rahmen);
                using (var balken = new SKPaint { Color = new SKColor(0xD0, 0xD0, 0xD0), Style = SKPaintStyle.Fill, IsAntialias = false })
                {
                    float[] anteile = { 0.45f, 0.7f, 0.55f, 0.85f, 0.6f };
                    float rand = breite * 0.1f, spalte = (breite - 2 * rand) / anteile.Length;
                    float boden = hoehe - hoehe * 0.12f, hoechst = hoehe * 0.7f;
                    for (int i = 0; i < anteile.Length; i++)
                    {
                        float x = rand + i * spalte + spalte * 0.15f;
                        g.DrawRect(x, boden - hoechst * anteile[i], spalte * 0.7f, hoechst * anteile[i], balken);
                    }
                }
                using (SKImage bild = flaeche.Snapshot())
                using (SKData daten = bild.Encode(SKEncodedImageFormat.Png, 100))
                    return daten.ToArray();
            }
        }

        /// <summary>Die Formatvorlagen: Standard, Titel, Überschrift 1 bis 3 (mit Gliederungsebene) und „EPOS Hinweis“.</summary>
        private static void Stile(MainDocumentPart main)
        {
            StyleDefinitionsPart teil = main.AddNewPart<StyleDefinitionsPart>();
            var stile = new Styles();
            stile.Append(Stil(WordVorlagenstile.STANDARD, "Normal", null, 21, false, null, null, true, 120));
            stile.Append(Stil(WordVorlagenstile.TITEL, "Title", WordVorlagenstile.STANDARD, 48, true, "1F4E79", null, false, 240));
            stile.Append(Stil(WordVorlagenstile.UEBERSCHRIFT1, "heading 1", WordVorlagenstile.STANDARD, 30, true, "1F4E79", 0, false, 240));
            stile.Append(Stil(WordVorlagenstile.UEBERSCHRIFT2, "heading 2", WordVorlagenstile.STANDARD, 25, true, "1F4E79", 1, false, 160));
            stile.Append(Stil(WordVorlagenstile.UEBERSCHRIFT3, "heading 3", WordVorlagenstile.STANDARD, 22, true, "595959", 2, false, 120));
            stile.Append(Stil(WordVorlagenstile.HINWEIS, WordVorlagenstile.NAME_HINWEIS, WordVorlagenstile.STANDARD, 18, false, "595959", null, false, 0));
            teil.Styles = stile;
            teil.Styles.Save();
        }

        private static Style Stil(string id, string name, string basis, int halbpunkte, bool fett, string farbe,
                                  int? ebene, bool standard, int abstandVor)
        {
            var s = new Style { Type = StyleValues.Paragraph, StyleId = id };
            if (standard) s.Default = true;
            s.Append(new StyleName { Val = name });
            if (basis != null) s.Append(new BasedOn { Val = basis });
            if (!standard) s.Append(new NextParagraphStyle { Val = WordVorlagenstile.STANDARD });
            s.Append(new PrimaryStyle());
            var pp = new StyleParagraphProperties();
            if (ebene.HasValue) pp.Append(new KeepNext());
            pp.Append(new SpacingBetweenLines { Before = abstandVor.ToString(CultureInfo.InvariantCulture), After = "60" });
            if (ebene.HasValue) pp.Append(new OutlineLevel { Val = ebene.Value });
            s.Append(pp);
            var rp = new StyleRunProperties();
            rp.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", ComplexScript = "Calibri" });
            if (fett) rp.Append(new Bold());
            if (farbe != null) rp.Append(new Color { Val = farbe });
            rp.Append(new FontSize { Val = halbpunkte.ToString(CultureInfo.InvariantCulture) });
            s.Append(rp);
            return s;
        }

        /// <summary>Katalogfassung (<c>vt:i4</c>), Art und Sprache (<c>vt:lpwstr</c>) in <c>docProps/custom.xml</c>.</summary>
        private static void Eigenschaften(WordprocessingDocument doc, int fassung, bool englisch)
        {
            CustomFilePropertiesPart teil = doc.AddCustomFilePropertiesPart();
            var liste = new DocumentFormat.OpenXml.CustomProperties.Properties();
            liste.Append(new CustomDocumentProperty(new VTInt32(fassung.ToString(CultureInfo.InvariantCulture)))
                { FormatId = FORMAT_EIGENE, PropertyId = 2, Name = Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG });
            liste.Append(new CustomDocumentProperty(new VTLPWSTR(VORLAGENART))
                { FormatId = FORMAT_EIGENE, PropertyId = 3, Name = EIGENSCHAFT_VORLAGE });
            liste.Append(new CustomDocumentProperty(new VTLPWSTR(englisch ? "en" : "de"))
                { FormatId = FORMAT_EIGENE, PropertyId = 4, Name = Vorlagenpruefer.EIGENSCHAFT_SPRACHE });
            teil.Properties = liste;
            teil.Properties.Save();
        }

        /// <summary>Ein Text der Ressourcen in der Kultur des Baukastens, formatiert; ein fehlender Schlüssel ist leer.</summary>
        private static string T(string schluessel, CultureInfo kultur, params object[] argumente)
        {
            string muster;
            try { muster = R.ResourceManager.GetString(schluessel, kultur) ?? ""; }
            catch (Exception) { muster = ""; }
            if (argumente == null || argumente.Length == 0) return muster;
            try { return string.Format(kultur, muster, argumente); }
            catch (FormatException) { return muster; }
        }
    }
}
