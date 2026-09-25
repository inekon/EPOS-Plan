using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Berichtsvorlage
{
    /// <summary>
    /// <b>Doppelte Stildefinitionen zusammenführen</b> — je <c>w:styleId</c> bleibt genau
    /// eine Definition; dazu das Absatzformat „EPOS Kapitelkopf“ anlegen, wenn es fehlt
    /// (<see cref="KapitelkopfErgaenzen"/>, Konzept Anhang B.3).
    ///
    /// <para><b>Woher die Doppelungen kommen.</b> Die Vorlage ist mit der JavaScript-
    /// Bibliothek <c>docx</c> erzeugt worden: Sie schreibt zuerst ihre eigenen Vorgaben für
    /// <c>Title</c> und <c>Heading1–6</c> und hängt danach die Absatzstile des Aufrufers an —
    /// mit derselben ID. Die Vorlage führte so <c>Title</c> und <c>Heading1–3</c> je zweimal.</para>
    ///
    /// <para><b>Welche Definition bleibt — gemessen, nicht vermutet.</b> ECMA-376 Teil 1
    /// (17.7.4.17) verlangt eindeutige IDs; der <c>OpenXmlValidator</c> meldet jede Doppelung
    /// („should have unique value“). Word erkennt eingebaute Stile an <c>w:name</c>
    /// („Title“, „heading 1“), und beide Definitionen tragen denselben Namen. Gemessen mit
    /// Word 16.0 (Build 16.0.20326) über COM, je eine Probe mit der Originaldatei, nur der
    /// ersten und nur der letzten Definition:</para>
    /// <list type="bullet">
    /// <item>Schrift, Größe, Farbe, Fett, Abstände, Rahmen und Gliederungsebene sind die der
    /// <b>letzten</b> Definition (Überschrift 1: 15 pt fett 1F4E79 mit Rahmen unten — nicht
    /// 16 pt 2E74B5 der ersten). Die erste allein änderte jede Überschrift und den Titel.</item>
    /// <item>Was nur die frühere Definition trägt, wirkt trotzdem: Nach dem Titel folgt
    /// „Standard“ (<c>w:next</c> der ersten), mit der letzten allein folgte „Titel“.</item>
    /// </list>
    /// <para>Word vereinigt also, und die spätere Angabe gewinnt. Deshalb bleibt je ID die
    /// <b>letzte</b> Definition, und was nur eine frühere trägt (Merkmale des
    /// <c>w:style</c>, Kindelemente, Kindelemente von <c>w:pPr</c>/<c>w:rPr</c>), wird
    /// übernommen. Die Vorlage wirkt danach in Word unverändert. Die letzte Definition
    /// ist zugleich die, die der Generator als Ersatzstil nachbildet
    /// (<c>WordBerichtGenerator.ErgaenzeErsatzStyles</c>: 28/15/12,5/11 pt, fett) und die
    /// allein <c>w:outlineLvl</c> trägt.</para>
    /// </summary>
    internal static class Stilbereinigung
    {
        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

        internal static int Ausfuehren(string pfad, TextWriter aus)
        {
            if (!File.Exists(pfad))
            {
                Console.Error.WriteLine("Datei nicht gefunden: " + pfad);
                return Program.DATEI;
            }
            if (!string.Equals(Path.GetExtension(pfad), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Erwartet wird eine .docx-Datei: " + pfad);
                return Program.DATEI;
            }

            aus.WriteLine("Berichtsvorlage bereinigen: " + pfad);

            int doppelte;
            bool kapitelkopfFehlt;
            using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
            {
                Styles stile = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles;
                if (stile == null)
                {
                    Console.Error.WriteLine("Die Datei hat keinen Stilteil (word/styles.xml).");
                    return Program.PRUEFUNG;
                }
                List<List<Style>> gruppen = DoppelteGruppen(stile);
                doppelte = gruppen.Count;
                kapitelkopfFehlt = Pruefung.Finde(stile, Pruefung.KAPITELKOPF_ID) == null;
                aus.WriteLine("  " + stile.Elements<Style>().Count() + " Stildefinitionen, doppelte IDs: "
                              + (doppelte == 0 ? "keine" : string.Join(", ", gruppen.Select(g => g[0].StyleId.Value + " ×" + g.Count)))
                              + "; „" + Pruefung.KAPITELKOPF_NAME + "“: " + (kapitelkopfFehlt ? "fehlt" : "vorhanden"));
                if (doppelte > 0)
                {
                    aus.WriteLine();
                    aus.WriteLine("  Vergleich der Definitionen (— = nicht gesetzt, erbt von basedOn bzw. den Dokumentvorgaben):");
                    List<Style> alle = stile.Elements<Style>().ToList();
                    foreach (List<Style> gruppe in gruppen) Vergleich(gruppe, alle, aus);
                    aus.WriteLine("  Regel: Es bleibt die letzte Definition; was nur eine frühere trägt, wird übernommen");
                    aus.WriteLine("  (Word wendet die letzte an und behält die übrigen Angaben — gemessen, siehe LIESMICH.md).");
                }
            }

            if (doppelte == 0 && !kapitelkopfFehlt)
            {
                aus.WriteLine("  Nichts zu tun — die Datei bleibt unverändert.");
                return Abschlusspruefung(pfad, aus) ? Program.OK : Program.PRUEFUNG;
            }

            aus.WriteLine();
            aus.WriteLine("  Validator vorher:");
            Pruefung.Validieren(pfad, aus);

            string kopie = Program.Arbeitskopie(pfad);
            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(kopie, true))
                {
                    MainDocumentPart main = doc.MainDocumentPart;
                    foreach (Styles stile in new[] { main.StyleDefinitionsPart?.Styles, main.StylesWithEffectsPart?.Styles })
                    {
                        if (stile == null) continue;
                        foreach (List<Style> gruppe in DoppelteGruppen(stile))
                        {
                            List<string> uebernommen = Zusammenfuehren(gruppe);
                            aus.WriteLine("  " + gruppe[0].StyleId.Value + ": letzte Definition bleibt"
                                          + (uebernommen.Count == 0 ? "" : ", übernommen aus früherer: " + string.Join(", ", uebernommen)));
                        }
                        if (KapitelkopfErgaenzen(stile))
                            aus.WriteLine("  " + Pruefung.KAPITELKOPF_ID + ": Absatzformat „" + Pruefung.KAPITELKOPF_NAME
                                          + "“ ergänzt (basedOn Heading1, Gliederungsebene 1, next Normal)");
                        stile.Save();
                    }
                }

                aus.WriteLine();
                aus.WriteLine("  Validator nachher:");
                if (!Abschlusspruefung(kopie, aus))
                {
                    Console.Error.WriteLine("Die bereinigte Fassung ist nicht in Ordnung — die Datei bleibt unverändert.");
                    return Program.PRUEFUNG;
                }
                File.Copy(kopie, pfad, true);
                aus.WriteLine("  Geschrieben: " + pfad);
                return Program.OK;
            }
            finally { Program.Loeschen(kopie); }
        }

        /// <summary>Stilregeln und Validator; true = grün.</summary>
        private static bool Abschlusspruefung(string pfad, TextWriter aus)
        {
            List<string> befunde;
            using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
                befunde = Pruefung.Stilbefunde(doc.MainDocumentPart?.StyleDefinitionsPart?.Styles);
            foreach (string b in befunde) aus.WriteLine("    Stilbefund: " + b);
            int fehler = Pruefung.Validieren(pfad, aus);
            aus.WriteLine("  Stilregeln: " + (befunde.Count == 0 ? "grün" : befunde.Count + " Befunde")
                          + "; Validator: " + fehler + " Fehler");
            return befunde.Count == 0 && fehler == 0;
        }

        /// <summary>
        /// Legt das Absatzformat „EPOS Kapitelkopf“ an, wenn es fehlt (Konzept 5.3, 6.2,
        /// Anhang B.3): benutzerdefiniert, auf Heading1 aufgebaut — sieht also aus wie
        /// Überschrift 1 —, Gliederungsebene 1, danach Standard. Der Generator von heute
        /// benutzt es nicht; der Bericht ändert sich dadurch nicht. Rückgabe: angelegt?
        /// </summary>
        internal static bool KapitelkopfErgaenzen(Styles stile)
        {
            if (Pruefung.Finde(stile, Pruefung.KAPITELKOPF_ID) != null) return false;
            var s = new Style { Type = StyleValues.Paragraph, StyleId = Pruefung.KAPITELKOPF_ID, CustomStyle = true };
            s.Append(new StyleName { Val = Pruefung.KAPITELKOPF_NAME });
            s.Append(new BasedOn { Val = "Heading1" });
            s.Append(new NextParagraphStyle { Val = "Normal" });
            s.Append(new StyleParagraphProperties(new OutlineLevel { Val = 0 }));
            stile.Append(s);
            return true;
        }

        /// <summary>Je ID, die mehr als einmal vorkommt, die Definitionen in Dateifolge.</summary>
        internal static List<List<Style>> DoppelteGruppen(Styles stile)
            => stile.Elements<Style>()
                    .Where(s => s.StyleId?.Value != null)
                    .GroupBy(s => s.StyleId.Value, StringComparer.Ordinal)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.ToList())
                    .ToList();

        /// <summary>
        /// Lässt die letzte Definition stehen, übernimmt in sie, was nur eine frühere trägt
        /// (die spätere frühere zuerst — sie gewinnt, wie in Word), und entfernt die
        /// früheren. Rückgabe: die übernommenen Angaben, für die Ausgabe.
        /// </summary>
        internal static List<string> Zusammenfuehren(List<Style> definitionen)
        {
            Style bleibt = definitionen[definitionen.Count - 1];
            var uebernommen = new List<string>();
            for (int i = definitionen.Count - 2; i >= 0; i--)
            {
                Style frueher = definitionen[i];

                foreach (OpenXmlAttribute a in frueher.GetAttributes())
                    if (!bleibt.GetAttributes().Any(b => b.LocalName == a.LocalName && b.NamespaceUri == a.NamespaceUri))
                    {
                        bleibt.SetAttribute(a);
                        uebernommen.Add("@" + a.LocalName);
                    }

                foreach (OpenXmlElement kind in frueher.ChildElements)
                {
                    OpenXmlElement da = bleibt.ChildElements.FirstOrDefault(x => Schluessel(x) == Schluessel(kind));
                    if (da == null)
                    {
                        bleibt.AddChild(kind.CloneNode(true));
                        uebernommen.Add(kind.LocalName);
                    }
                    else if (da is OpenXmlCompositeElement ziel
                             && (kind is StyleParagraphProperties || kind is StyleRunProperties))
                    {
                        foreach (OpenXmlElement enkel in kind.ChildElements)
                            if (!ziel.ChildElements.Any(x => Schluessel(x) == Schluessel(enkel)))
                            {
                                ziel.AddChild(enkel.CloneNode(true));
                                uebernommen.Add(kind.LocalName + "/" + enkel.LocalName);
                            }
                    }
                }
                frueher.Remove();
            }
            return uebernommen;
        }

        /// <summary>Gleichartige Kindelemente: Name und Namensraum, bei <c>w:tblStylePr</c> dazu der Typ.</summary>
        private static string Schluessel(OpenXmlElement e)
        {
            string k = e.NamespaceUri + "|" + e.LocalName;
            if (e.LocalName == "tblStylePr")
                k += "|" + e.GetAttributes().FirstOrDefault(a => a.LocalName == "type").Value;
            return k;
        }

        // ------------------------------------------------------------- Vergleich

        private static void Vergleich(List<Style> gruppe, List<Style> alle, TextWriter aus)
        {
            aus.WriteLine();
            aus.WriteLine("  Stil-ID „" + gruppe[0].StyleId.Value + "“ — " + gruppe.Count + " Definitionen (Stelle im Stilteil: "
                          + string.Join(", ", gruppe.Select(s => (alle.IndexOf(s) + 1).ToString(De))) + " von " + alle.Count + ")");
            List<(string Merkmal, string[] Werte)> zeilen = Merkmale(gruppe);
            aus.Write("    " + "Merkmal".PadRight(20));
            for (int i = 0; i < gruppe.Count; i++) aus.Write((i + 1) + ". Definition".PadRight(18));
            aus.WriteLine();
            foreach ((string merkmal, string[] werte) in zeilen)
            {
                aus.Write("    " + merkmal.PadRight(20));
                foreach (string w in werte) aus.Write(w.PadRight(19));
                aus.WriteLine(werte.Distinct().Count() > 1 ? "  ≠" : "");
            }
        }

        private static List<(string, string[])> Merkmale(List<Style> gruppe)
        {
            var z = new List<(string, string[])>
            {
                ("Name", gruppe.Select(s => s.StyleName?.Val?.Value ?? "—").ToArray()),
                ("basedOn", gruppe.Select(s => s.BasedOn?.Val?.Value ?? "—").ToArray()),
                ("next", gruppe.Select(s => s.NextParagraphStyle?.Val?.Value ?? "—").ToArray()),
                ("qFormat", gruppe.Select(s => s.PrimaryStyle != null ? "ja" : "—").ToArray()),
                ("Schrift", gruppe.Select(s => Rpr<RunFonts>(s)?.Ascii?.Value ?? "—").ToArray()),
                ("Größe", gruppe.Select(s => Groesse(Rpr<FontSize>(s)?.Val?.Value)).ToArray()),
                ("Farbe", gruppe.Select(s => Rpr<Color>(s)?.Val?.Value ?? "—").ToArray()),
                ("fett", gruppe.Select(s => Schalter(Rpr<Bold>(s)?.Val, Rpr<Bold>(s) != null)).ToArray()),
                ("kursiv", gruppe.Select(s => Schalter(Rpr<Italic>(s)?.Val, Rpr<Italic>(s) != null)).ToArray()),
                ("Abstand vor/nach", gruppe.Select(s => Abstand(s.StyleParagraphProperties?.SpacingBetweenLines)).ToArray()),
                ("outlineLvl", gruppe.Select(s => s.StyleParagraphProperties?.OutlineLevel?.Val?.Value.ToString(De) ?? "—").ToArray()),
                ("Rahmen", gruppe.Select(s => s.StyleParagraphProperties?.ParagraphBorders != null
                                              ? string.Join("+", s.StyleParagraphProperties.ParagraphBorders.ChildElements.Select(c => c.LocalName))
                                              : "—").ToArray()),
            };
            return z;
        }

        private static T Rpr<T>(Style s) where T : OpenXmlElement
            => s.StyleRunProperties?.GetFirstChild<T>();

        private static string Groesse(string halbpunkte)
        {
            if (string.IsNullOrEmpty(halbpunkte)) return "—";
            return double.TryParse(halbpunkte, NumberStyles.Number, CultureInfo.InvariantCulture, out double h)
                ? (h / 2.0).ToString("0.#", De) + " pt"
                : halbpunkte;
        }

        private static string Schalter(OnOffValue wert, bool vorhanden)
            => !vorhanden ? "—" : (wert == null || wert.Value ? "ja" : "nein");

        private static string Abstand(SpacingBetweenLines sp)
            => sp == null ? "—" : (sp.Before?.Value ?? "—") + "/" + (sp.After?.Value ?? "—") + " twip";
    }
}
