using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Zeichenmodell = WindowsFormsApplication1.Zeichnung.Zeichenmodell;
using SkiaMaler = WindowsFormsApplication1.Zeichnung.SkiaMaler;
using SvgSchreiber = WindowsFormsApplication1.Zeichnung.SvgSchreiber;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using T = WindowsFormsApplication1.WordVorlagentexte;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DIE DIAGRAMME DER WORD-ENGINE (Konzept Berichtsvorlagen 4.2, 4.6 BV-P5, 4.10, 6.5;
    // Etappe BV-E5): die Bildplatzhalter bild.*, stand.bild.*, stamm.bild.* — als
    // Alternativtext eines Bildes (der Rahmen bestimmt das Maß) oder als Text allein im
    // Absatz (das Bild in Satzspiegelbreite). Das Modell kommt aus dem Katalog
    // (Diagrammbild, gebaut von Berichtsbilder), die Bildteile aus Wordbilder — dieselben
    // wie im Bausteinweg: PNG mit SVG-Fassung (DG-E3-8).
    // ---------------------------------------------------------------------------

    public sealed partial class WordVorlagenfueller
    {
        private sealed partial class Lauf
        {
            /// <summary>Welche Meldung „kein Bild“ schon gegeben ist (Schlüssel und Grund) — je Paar einmal.</summary>
            private readonly HashSet<string> _bildGemeldet = new HashSet<string>(StringComparer.Ordinal);

            /// <summary>
            /// Ein Diagramm in den Rahmen eines Platzhalterbildes (Konzept 6.5): Das Zielmaß ist der Rahmen
            /// (<c>wp:extent</c>) mal dem Faktor des Bildes; der Renderer zeichnet darin (Stufe 2) — unter der
            /// Mindestbreite in ihr, dann verkleinert (Stufe 1). Die Breite des Rahmens bleibt, die Höhe folgt
            /// dem Seitenverhältnis des Modells bis zur Rahmenhöhe. Lage, Umbruch, Rahmen und Drehung bleiben,
            /// <c>a:srcRect</c> geht, der Alternativtext wird der Titel des Diagramms (BV-Q14). Ohne Modell steht an
            /// der Stelle des Bildes ein Hinweisabsatz mit dem Grund, und die Laufmeldung nennt das Bild.
            /// </summary>
            private void FuelleDiagramm(Bildstelle b, Vorlagenfeld feld, Platzhalter m)
            {
                Berichtswerte werte = WerteVon(b.DocPr);
                // Konzept 4.7: ein Bild je Stand nur im Standblock.
                if (feld.Kontext == Vorlagenfeldkontext.Stand && !werte.ImStandblock && !IstPaarschluessel(feld.Schluessel))
                {
                    Stehen(m, Fuellbefundart.Kontext, b.DocPr, b.Teil, null);
                    return;
                }

                OpenXmlElement rahmen = b.DocPr.Parent;
                DW.Extent ausdehnung = rahmen?.GetFirstChild<DW.Extent>();
                A.Blip blip = rahmen?.Descendants<A.Blip>().FirstOrDefault();
                if (ausdehnung == null || blip == null)
                {
                    Stehen(m, Fuellbefundart.FalscheStelle, b.DocPr, b.Teil, T.BILD_OHNE_BILD);
                    return;
                }

                Platzhalterwert w = Loese(feld, m, werte);
                string grund = w.Grund;
                if (w.Diagramm != null)
                {
                    long rahmenBreite = ausdehnung.Cx?.Value ?? 0L, rahmenHoehe = ausdehnung.Cy?.Value ?? 0L;
                    Bildmass? mass = w.Diagramm.MassFuer(rahmenBreite, rahmenHoehe);
                    Zeichenmodell modell = Baue(w.Diagramm, mass, feld);
                    if (modell != null)
                    {
                        A.Blip neu = Wordbilder.BaueBlip(b.Teil.Teil, SkiaMaler.Png(modell), SvgSchreiber.Text(modell));
                        List<string> alt = Wordbilder.ErsetzeBlip(blip, neu);

                        (long breite, long hoehe) = Eingepasst(rahmenBreite, rahmenHoehe, modell.Breite, modell.Hoehe);
                        ausdehnung.Cx = breite;
                        ausdehnung.Cy = hoehe;
                        foreach (A.Extents x in rahmen.Descendants<A.Extents>())
                        {
                            x.Cx = breite;
                            x.Cy = hoehe;
                        }
                        SetzeAlternativtext(b.DocPr, rahmen, Berichtsbilder.Titel(modell));
                        foreach (string id in alt.Distinct()) EntferneTeilWennFrei(b.Teil, id);
                        MeldeVerkleinert(feld, mass, modell, b.DocPr, b.Teil);
                        _ergebnis.Ersetzt++;
                        return;
                    }
                    grund = w.Diagramm.GrundOhneModell;
                    _ergebnis.Leer(feld.Schluessel);
                }

                // Ohne Modell: der Hinweis an der Stelle des Bildes (Konzept 4.10).
                Paragraph absatz = b.DocPr.Ancestors<Paragraph>().FirstOrDefault();
                if (absatz?.Parent != null) absatz.InsertBeforeSelf(Hinweisabsatz(absatz, grund));
                EntferneBild(b);
                MeldeOhneModell(feld, grund);
                _ergebnis.Ersetzt++;
            }

            /// <summary>
            /// Ein Diagramm als Text allein im Absatz oder als Block-Steuerelement (Konzept 4.2): Das Bild steht in
            /// der Breite des Satzspiegels am Ort (in einer Zelle: der Zellbreite), die Höhe folgt dem Modell; der
            /// Absatz behält sein Format. Ohne Modell ersetzt ihn ein Hinweisabsatz mit dem Grund.
            /// </summary>
            private void FuelleDiagrammImAbsatz(OpenXmlElement bezug, Teilinfo ti, Entscheid e)
            {
                Platzhalterwert w = Loese(e.Feld, e.Marke, e.Werte);
                string grund = w.Grund;
                Paragraph vorbild = bezug as Paragraph ?? bezug.Descendants<Paragraph>().FirstOrDefault();
                if (w.Diagramm != null)
                {
                    long breite = Zielbreite(bezug, ti) * Wordbilder.EMU_JE_PIXEL;
                    Bildmass? mass = w.Diagramm.MassFuer(breite, 0L);
                    Zeichenmodell modell = Baue(w.Diagramm, mass, e.Feld);
                    if (modell != null)
                    {
                        A.Blip blip = Wordbilder.BaueBlip(ti.Teil, SkiaMaler.Png(modell), SvgSchreiber.Text(modell));
                        (long cx, long cy) = Eingepasst(breite, 0L, modell.Breite, modell.Hoehe);
                        var p = new Paragraph();
                        ParagraphProperties format = OhneAbschnitt(vorbild?.ParagraphProperties);
                        if (format != null) p.AppendChild(format);
                        p.AppendChild(new Run(Wordbilder.Inline(blip, cx, cy, 1U, NullWennLeer(Berichtsbilder.Titel(modell)))));
                        ErsetzeAbsatz(bezug, new List<OpenXmlElement> { p });
                        MeldeVerkleinert(e.Feld, mass, modell, vorbild ?? bezug, ti);
                        _ergebnis.Ersetzt++;
                        return;
                    }
                    grund = w.Diagramm.GrundOhneModell;
                    _ergebnis.Leer(e.Feld.Schluessel);
                }
                ErsetzeAbsatz(bezug, new List<OpenXmlElement> { Hinweisabsatz(vorbild, grund) });
                MeldeOhneModell(e.Feld, grund);
                _ergebnis.Ersetzt++;
            }

            /// <summary>Das Modell im Zielmaß; ein Fehler des Renderers wird zur Warnung, das Bild entfällt.</summary>
            private Zeichenmodell Baue(Diagrammbild diagramm, Bildmass? mass, Vorlagenfeld feld)
            {
                try { return diagramm.Baue(mass); }
                catch (Exception ex)
                {
                    _ergebnis.Warnung(T.F(_englisch, T.AUSNAHME, feld.Schluessel, ex.Message));
                    return null;
                }
            }

            /// <summary>Stufe 1 griff (der Rahmen ist schmaler als die Mindestbreite): ein Hinweis mit dem Anteil.</summary>
            private void MeldeVerkleinert(Vorlagenfeld feld, Bildmass? mass, Zeichenmodell modell, OpenXmlElement bezug, Teilinfo ti)
            {
                if (!mass.HasValue || mass.Value.Breite <= 0 || modell.Breite <= mass.Value.Breite) return;
                double anteil = mass.Value.Breite / (double)modell.Breite * 100.0;
                _ergebnis.Hinweis(T.F(_englisch, T.BILD_VERKLEINERT, feld.Schluessel,
                                      anteil.ToString("N0", BerichtTexte.KulturFuer(_englisch)), Fundort(bezug, ti)));
            }

            /// <summary>Die Laufmeldung „kein Bild“ — je Schlüssel und Grund einmal.</summary>
            private void MeldeOhneModell(Vorlagenfeld feld, string grund)
            {
                string g = string.IsNullOrWhiteSpace(grund) ? _werte.Text(nameof(MyResource.Resource.BV_GRUND_BILD_OHNE_DATEN)) : grund;
                if (_bildGemeldet.Add(feld.Schluessel + "|" + g))
                    _ergebnis.Warnung(T.F(_englisch, T.BILD_OHNE_MODELL, feld.Schluessel, g));
            }

            /// <summary>Der Hinweisabsatz „Diagramm entfällt: Grund“ in der Rolle „Hinweis“ (EPOS Hinweis).</summary>
            private Paragraph Hinweisabsatz(Paragraph vorbild, string grund)
            {
                string g = string.IsNullOrWhiteSpace(grund) ? _werte.Text(nameof(MyResource.Resource.BV_GRUND_BILD_OHNE_DATEN)) : grund;
                var p = new Paragraph();
                var format = new ParagraphProperties(new ParagraphStyleId { Val = _stile.Id(WordVorlagenstile.HINWEIS) });
                Justification ausrichtung = vorbild?.ParagraphProperties?.Justification;
                if (ausrichtung != null) format.AppendChild((Justification)ausrichtung.CloneNode(true));
                p.AppendChild(format);
                p.AppendChild(new Run(new Text(T.F(_englisch, T.BILD_ENTFAELLT, g)) { Space = SpaceProcessingModeValues.Preserve }));
                return p;
            }

            /// <summary>
            /// Die Breite am Ort eines Absatzplatzhalters in Bildpunkten: in einer Tabellenzelle deren Breite (DXA),
            /// sonst die Inhaltsbreite des Abschnitts (Konzept 5.3) — in Kopf- und Fußzeilen die des letzten.
            /// </summary>
            private int Zielbreite(OpenXmlElement bezug, Teilinfo ti)
            {
                const int DXA_JE_PIXEL = 15;
                TableCell zelle = bezug.Ancestors<TableCell>().FirstOrDefault();
                TableCellWidth tcw = zelle?.TableCellProperties?.TableCellWidth;
                if (tcw?.Type?.Value == TableWidthUnitValues.Dxa &&
                    int.TryParse(tcw.Width?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int zellbreite) &&
                    zellbreite > 0)
                    return Math.Max(1, (zellbreite - 216) / DXA_JE_PIXEL);   // abzüglich der Zellränder (2 × 0,19 cm)

                SectionProperties abschnitt = ti.Art == Teilart.Rumpf && bezug.Parent != null
                    ? Einfuegeanker.Vor(bezug, ti.Teil).Abschnitt()
                    : _main.Document.Body.Elements<SectionProperties>().LastOrDefault();
                return Math.Max(1, WordKontext.InhaltsbreiteAus(abschnitt) / DXA_JE_PIXEL);
            }

            private static string NullWennLeer(string text) { return string.IsNullOrWhiteSpace(text) ? null : text; }
        }
    }
}
