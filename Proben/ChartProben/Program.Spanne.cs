using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>ETAPPE E6, NACHTRAG E5b — DAS SPANNENBILD</b> (Anwenderentscheid 22.09.2026 zu
    /// Frage (4), Mockup <c>valeri-f2</c> „Spanne der Kapitalwertdifferenz je Variante").
    ///
    /// <para><b>Was hier gezeichnet wird.</b> Die Bandbreite je Version als Balken
    /// (<c>ChartRenderer.KapitalwertSpanne</c>): vom kleinsten bis zum größten der drei
    /// Szenariowerte, der Erwartungsfall als Punkt mit seinem Betrag, die Referenz als
    /// Nulllinie. Die Werte der ersten Probe sind die des Mockups (drei Versionen, alle über
    /// der Referenz); die zweite mischt eine Version, die die Nulllinie kreuzt, eine ganz
    /// darunter, eine darüber und eine ohne Spanne (nur Erwartet).</para>
    ///
    /// <para><b>Die Texte sind die deutsche Vorgabe</b> (<c>SpannenTexte</c>) — kein
    /// Probebild hängt an der Oberflächensprache des Rechners.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung
    /// nicht sehen: dass der Punkt dem Erwartungsfall folgt, dass der Balken der Spanne
    /// folgt — und im SVG, dass die Nulllinie bei der Referenz liegt: links jedes Balkens
    /// über ihr, rechts jedes Balkens unter ihr, mitten in dem, der sie kreuzt.</para>
    /// </summary>
    internal static partial class Program
    {
        private const string SPANNE_REFERENZ = "Stammprojekt — Weiterbetrieb";

        /// <summary>
        /// Die Proben des Spannenbilds — eine Zeile in <c>Program.cs</c> ruft sie.
        /// </summary>
        private static void SpannenProben(string ziel)
        {
            var texte = new ChartRenderer.SpannenTexte();

            // ---- Maßproben -------------------------------------------------------

            // Die drei Versionen des Mockups, alle über der Referenz: grüne Marken, kein
            // Legendeneintrag „unter der Referenz".
            Pruefe(ziel, "kapitalwert_spanne", 1240, 434,
                   new[] { ChartRenderer.C_RASTER_GUT },
                   () => ChartRenderer.KapitalwertSpanne(Mockupbalken(), SPANNE_REFERENZ, texte));

            // Vier Versionen, zwei davon (ganz oder teilweise) unter der Referenz: Das Bild
            // wird um eine Zeile (72 px) länger und trägt beide Vorzeichenfarben.
            Pruefe(ziel, "kapitalwert_spanne_unter_referenz", 1240, 506,
                   new[] { ChartRenderer.C_RASTER_GUT, ChartRenderer.C_RASTER_SCHLECHT },
                   () => ChartRenderer.KapitalwertSpanne(Gemischtebalken(), SPANNE_REFERENZ, texte));

            // Keine zeichenbare Version: der Leerhinweis an der Stelle des Bildes.
            Pruefe(ziel, "kapitalwert_spanne_leer", 1240, 200,
                   new SKColor[0],
                   () => ChartRenderer.KapitalwertSpanne(new List<ChartRenderer.Spannenbalken>
                   {
                       new ChartRenderer.Spannenbalken { Name = "ohne Werte" }
                   }, SPANNE_REFERENZ, texte));

            // ---- Gegenproben -----------------------------------------------------

            // Erstens der PUNKT: dieselben Balken, nur der Erwartungsfall der ersten Version
            // liegt anders in ihrer Spanne.
            Unterschiedlich("kapitalwert_spanne_erwartet_wirkt",
                () => ChartRenderer.KapitalwertSpanne(Mockupbalken(), SPANNE_REFERENZ, texte),
                () =>
                {
                    List<ChartRenderer.Spannenbalken> b = Mockupbalken();
                    b[0].Erwartet = b[0].Worst + 20000.0;
                    return ChartRenderer.KapitalwertSpanne(b, SPANNE_REFERENZ, texte);
                });

            // Zweitens der BALKEN: derselbe Erwartungsfall, nur die Spanne der ersten Version
            // ist enger. Ein Renderer, der den Balken um den Punkt legte statt von Worst bis
            // Best, bestuende jede Mass- und Farbpruefung.
            Unterschiedlich("kapitalwert_spanne_spanne_wirkt",
                () => ChartRenderer.KapitalwertSpanne(Mockupbalken(), SPANNE_REFERENZ, texte),
                () =>
                {
                    List<ChartRenderer.Spannenbalken> b = Mockupbalken();
                    b[0].Worst += 60000.0;
                    b[0].Best -= 60000.0;
                    return ChartRenderer.KapitalwertSpanne(b, SPANNE_REFERENZ, texte);
                });

            // ---- SVG: dasselbe Modell auf dem Bildschirmweg ----------------------

            SvgPixelbildprobe("kapitalwert_spanne",
                () => ChartRenderer.KapitalwertSpanneModell(Mockupbalken(), SPANNE_REFERENZ, texte));
            SpannenNulllinienprobe(texte);

            // Sichtprüfung (--svg-alle): beide Modelle als Dateien neben den Skia-PNG.
            if (_svgordner != null)
                SvgOrdnerSchreiben(new List<KeyValuePair<string, Func<Zeichenmodell>>>
                {
                    new KeyValuePair<string, Func<Zeichenmodell>>("kapitalwert_spanne",
                        () => ChartRenderer.KapitalwertSpanneModell(Mockupbalken(), SPANNE_REFERENZ, texte)),
                    new KeyValuePair<string, Func<Zeichenmodell>>("kapitalwert_spanne_unter_referenz",
                        () => ChartRenderer.KapitalwertSpanneModell(Gemischtebalken(), SPANNE_REFERENZ, texte))
                });
        }

        /// <summary>
        /// <b>Die Referenz ist die Nulllinie</b> — im SVG nachgemessen: Der Balken einer
        /// Version über der Referenz beginnt rechts der Nulllinie, der einer Version darunter
        /// endet links von ihr, und der Balken, der sie kreuzt, schließt sie ein. Die Version
        /// ohne Spanne trägt keinen Balken, aber ihren Punkt; jede Zeile nennt ihre drei Werte
        /// am Element.
        /// </summary>
        private static void SpannenNulllinienprobe(ChartRenderer.SpannenTexte texte)
        {
            SvgProbe("svg_kapitalwert_spanne_nulllinie", e =>
            {
                Zeichenmodell m = ChartRenderer.KapitalwertSpanneModell(Gemischtebalken(), SPANNE_REFERENZ, texte);
                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                SvgKnoten null0 = alle.FirstOrDefault(
                    k => k.Name == "line" && Attributwert(k, "data-marke") == "nulllinie");
                if (null0 == null) { e.Maengel.Add("keine Nulllinie im Baum"); return; }
                double x0 = Zahl(Attributwert(null0, "x1"));

                (double Von, double Bis)? Balken(string name)
                {
                    SvgKnoten r = alle.FirstOrDefault(
                        k => k.Name == "rect" && Attributwert(k, "data-marke") == "reihe:" + name);
                    if (r == null) return null;
                    double x = Zahl(Attributwert(r, "x"));
                    return (x, x + Zahl(Attributwert(r, "width")));
                }

                (double Von, double Bis)? ueber = Balken("Photovoltaik");
                (double Von, double Bis)? unter = Balken("Kessel neu");
                (double Von, double Bis)? kreuzt = Balken("Wärmepumpe");
                if (ueber == null || unter == null || kreuzt == null)
                    e.Maengel.Add("ein Balken fehlt im Baum");
                else
                {
                    if (!(ueber.Value.Von > x0))
                        e.Maengel.Add("der Balken über der Referenz beginnt nicht rechts der Nulllinie");
                    if (!(unter.Value.Bis < x0))
                        e.Maengel.Add("der Balken unter der Referenz endet nicht links der Nulllinie");
                    if (!(kreuzt.Value.Von < x0 && kreuzt.Value.Bis > x0))
                        e.Maengel.Add("der Balken, der die Referenz kreuzt, schließt die Nulllinie nicht ein");
                }

                // Ohne Spanne kein Balken, aber der Punkt.
                if (Balken("Speicher") != null) e.Maengel.Add("die Version ohne Spanne trägt einen Balken");
                if (!alle.Any(k => k.Name == "circle" && Attributwert(k, "data-marke") == "reihe:Speicher"))
                    e.Maengel.Add("die Version ohne Spanne trägt keinen Punkt");

                // Jede Zeile nennt ihre drei Werte am Element.
                foreach (string name in new[] { "Wärmepumpe", "Kessel neu", "Photovoltaik", "Speicher" })
                {
                    SvgKnoten k = alle.FirstOrDefault(x => Attributwert(x, "data-marke") == "reihe:" + name);
                    string wert = k == null ? null : Attributwert(k, "data-wert");
                    if (string.IsNullOrEmpty(wert) || !wert.Contains(texte.Worst, StringComparison.Ordinal) ||
                        !wert.Contains(texte.Erwartet, StringComparison.Ordinal) ||
                        !wert.Contains(texte.Best, StringComparison.Ordinal))
                        e.Maengel.Add("die Zeile " + name + " nennt ihre drei Werte nicht: " + (wert ?? "kein Wert"));
                }
            });
        }

        /// <summary>Eine Zahl eines SVG-Attributs (InvariantCulture, wie der Schreiber sie setzt).</summary>
        private static double Zahl(string text)
            => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
               ? w : double.NaN;

        /// <summary>Die drei Versionen des Mockups <c>valeri-f2</c> (Kapitalwertdifferenz zum
        /// Stammprojekt in €).</summary>
        private static List<ChartRenderer.Spannenbalken> Mockupbalken() => new List<ChartRenderer.Spannenbalken>
        {
            new ChartRenderer.Spannenbalken { Name = "BHKW", Worst = 1506740.0, Erwartet = 1660205.0, Best = 1811714.0 },
            new ChartRenderer.Spannenbalken { Name = "Photovoltaik", Worst = 129296.0, Erwartet = 182491.0, Best = 236921.0 },
            new ChartRenderer.Spannenbalken { Name = "BHKW + Photovoltaik", Worst = 1636035.0, Erwartet = 1842695.0, Best = 2048635.0 }
        };

        /// <summary>Vier Versionen um die Referenz: eine kreuzt die Nulllinie, eine liegt ganz
        /// darunter, eine darüber, eine hat nur den Erwartungsfall.</summary>
        private static List<ChartRenderer.Spannenbalken> Gemischtebalken() => new List<ChartRenderer.Spannenbalken>
        {
            new ChartRenderer.Spannenbalken { Name = "Wärmepumpe", Worst = -120000.0, Erwartet = 40000.0, Best = 150000.0 },
            new ChartRenderer.Spannenbalken { Name = "Kessel neu", Worst = -260000.0, Erwartet = -180000.0, Best = -90000.0 },
            new ChartRenderer.Spannenbalken { Name = "Photovoltaik", Worst = 129296.0, Erwartet = 182491.0, Best = 236921.0 },
            new ChartRenderer.Spannenbalken { Name = "Speicher", Erwartet = 60000.0 }
        };
    }
}
