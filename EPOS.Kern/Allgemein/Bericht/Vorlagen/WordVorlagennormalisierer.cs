using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Run-Normalisierer</b> (Konzept Berichtsvorlagen 4.2, Messprobe 4 von BV-E0): Word
    /// verteilt einen getippten Platzhalter gern über mehrere Runs — nach Autokorrektur, Formatwechsel
    /// mitten im Wort oder Rechtschreibmarken steht <c>{{projekt.kunde}}</c> in keinem einzelnen
    /// <c>w:t</c>, nur im zusammengesetzten Absatztext. Der Normalisierer zieht jeden solchen
    /// Platzhalter IM SPEICHER in EINEN Run.
    ///
    /// <para><b>Regel.</b> Der Run, in dem <c>{{</c> steht, nimmt den ganzen Platzhalter auf und
    /// behält sein Zeichenformat; aus den folgenden Runs verschwindet nur der Anteil des
    /// Platzhalters — was dort davor oder danach steht, bleibt in seinem Run mit seinem Format. Ein
    /// Run, der danach nichts mehr trägt, entfällt. Runs ohne Platzhalteranteil bleiben
    /// unberührt.</para>
    ///
    /// <para><b>Was verbindet, was trennt.</b> Gelesen wird je Absatz Behälter für Behälter —
    /// der Absatz selbst, Hyperlink, Inhaltssteuerelement im Satz, nachverfolgte Einfügung,
    /// <c>w:customXml</c>, <c>w:bdo</c>, <c>w:dir</c>, jeder für sich. Innerhalb eines Behälters
    /// verbinden aufeinanderfolgende <c>w:t</c> (auch über Runs hinweg);
    /// Rechtschreibmarken, Textmarken, Bearbeitungsbereiche und Kommentarmarken dazwischen
    /// stören nicht. Alles andere trennt: Tabulator, Umbruch, Feldzeichen, Bild, ein
    /// Behälterwechsel. Ein Platzhalter über eine solche Grenze hinweg bleibt unerkannt stehen
    /// (der Prüfer meldet ihn als offene Klammer). Absätze in Textfeldern liest der Aufrufer als
    /// eigene Absätze; Feldergebnisse (<c>w:fldSimple</c>) bleiben unberührt.</para>
    ///
    /// <para><b>Warum der Prüfer ihn nicht benutzt.</b> Der Normalisierer ändert den Baum — der
    /// Prüfer darf das nicht, er liest über <see cref="Vorlagenteile"/>. Beide erkennen nach
    /// derselben Trennregel; wo sie sich unterscheiden (Zweige von <c>mc:AlternateContent</c>,
    /// <c>w:smartTag</c>, <c>w:fldSimple</c>, <c>w:bdo</c>/<c>w:dir</c>), steht im Kopf von
    /// <c>Vorlagenteile.cs</c>.</para>
    /// </summary>
    internal static class WordVorlagennormalisierer
    {
        /// <summary>Zieht die zerlegten Platzhalter eines Absatzes zusammen; Rückgabe: wie viele.</summary>
        internal static int NormalisiereAbsatz(Paragraph absatz)
        {
            return absatz == null ? 0 : Behaelter(absatz);
        }

        private static int Behaelter(OpenXmlElement behaelter)
        {
            int zahl = 0;
            var segment = new List<Text>();
            foreach (OpenXmlElement kind in behaelter.ChildElements.ToList())
            {
                if (kind is ParagraphProperties) continue;
                if (kind is Run lauf)
                {
                    foreach (OpenXmlElement rk in lauf.ChildElements.ToList())
                    {
                        if (rk is RunProperties || rk is LastRenderedPageBreak) continue;
                        if (rk is Text t) segment.Add(t);
                        else zahl += Schliesse(segment);
                    }
                    continue;
                }
                if (IstDurchlaessig(kind)) continue;

                zahl += Schliesse(segment);
                OpenXmlElement innen = Innen(kind);
                if (innen != null) zahl += Behaelter(innen);
            }
            zahl += Schliesse(segment);
            return zahl;
        }

        /// <summary>Marken, die zwischen zwei Runs eines Platzhalters stehen dürfen.</summary>
        private static bool IstDurchlaessig(OpenXmlElement e)
        {
            return e is ProofError || e is BookmarkStart || e is BookmarkEnd || e is PermStart || e is PermEnd ||
                   e is CommentRangeStart || e is CommentRangeEnd;
        }

        /// <summary>Der Behälter im Satz, dessen Runs als eigene Folge gelesen werden; <c>null</c> = keiner.</summary>
        private static OpenXmlElement Innen(OpenXmlElement e)
        {
            switch (e)
            {
                case Hyperlink _:
                case InsertedRun _:
                case MoveToRun _:
                case CustomXmlRun _:
                case BidirectionalOverride _:
                case BidirectionalEmbedding _:
                    return e;
                case SdtRun sdt:
                    return sdt.SdtContentRun;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Verarbeitet eine Folge verbundener <c>w:t</c> und leert sie. Die Platzhalter werden von
        /// hinten nach vorn zusammengezogen: Der Anteil eines Textes VOR einer Fundstelle bleibt
        /// dabei unverändert, damit gelten die Positionen der früheren weiter.
        /// </summary>
        private static int Schliesse(List<Text> segment)
        {
            if (segment.Count < 2) { segment.Clear(); return 0; }

            string ganz = string.Concat(segment.Select(t => t.Text));
            if (ganz.IndexOf("{{", System.StringComparison.Ordinal) < 0) { segment.Clear(); return 0; }

            var anfang = new int[segment.Count];
            int pos = 0;
            for (int i = 0; i < segment.Count; i++) { anfang[i] = pos; pos += segment[i].Text.Length; }

            List<Platzhalter> marken = Platzhaltersyntax.Finde(ganz).ToList();
            int zahl = 0;
            var geaendert = new HashSet<Text>();
            for (int m = marken.Count - 1; m >= 0; m--)
            {
                int a = marken[m].Position, e = a + marken[m].Laenge;
                int i = Index(anfang, a), j = Index(anfang, e - 1);
                if (i == j) continue;

                Text erster = segment[i];
                erster.Text = erster.Text.Substring(0, a - anfang[i]) + ganz.Substring(a, e - a);
                for (int k = i + 1; k < j; k++) segment[k].Text = "";
                Text letzter = segment[j];
                letzter.Text = letzter.Text.Substring(e - anfang[j]);
                geaendert.Add(erster); geaendert.Add(letzter);
                for (int k = i + 1; k < j; k++) geaendert.Add(segment[k]);
                zahl++;
            }

            foreach (Text t in geaendert)
            {
                if (t.Text.Length > 0)
                {
                    t.Space = SpaceProcessingModeValues.Preserve;
                    continue;
                }
                Run lauf = t.Parent as Run;
                t.Remove();
                if (lauf != null && !lauf.ChildElements.Any(c => !(c is RunProperties))) lauf.Remove();
            }
            segment.Clear();
            return zahl;
        }

        /// <summary>Der Index des Textes, in dem die Position liegt.</summary>
        private static int Index(int[] anfang, int position)
        {
            int i = anfang.Length - 1;
            while (i > 0 && anfang[i] > position) i--;
            // Leere Texte teilen ihren Anfang mit dem nächsten: der letzte mit diesem Anfang trägt das Zeichen.
            return i;
        }
    }
}
