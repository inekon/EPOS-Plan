using System;
using System.Collections.Generic;
using SkiaSharp;

namespace WindowsFormsApplication1.Zeichnung
{
    /// <summary>
    /// Die Brücke zwischen dem Skia-freien <see cref="Zeichenmodell"/> und SkiaSharp.
    /// Sie steht bewusst in EINER eigenen Datei: Das Modell selbst darf SkiaSharp
    /// nicht kennen, damit ein zweiter Ausgabeweg (SVG, Etappe E2) es lesen kann.
    /// </summary>
    public static class SkiaBruecke
    {
        public static Farbe Modellfarbe(this SKColor f) => new Farbe(f.Red, f.Green, f.Blue, f.Alpha);

        /// <summary>
        /// Eine Skia-Farbe als FARBTON — über die Rückwärtssuche der Vorgabepalette.
        /// Das ist die Stelle, an der eine durchgereichte Hausfarbe ihre Rolle
        /// zurückbekommt; eine fremde Farbe bleibt ein Wert ohne Rolle.
        /// </summary>
        public static Farbton Ton(this SKColor f) => Farbpalette.Ton(f.Modellfarbe());

        public static SKColor Skiafarbe(this Farbe f) => new SKColor(f.R, f.G, f.B, f.A);

        public static Punkt Modellpunkt(this SKPoint p) => new Punkt(p.X, p.Y);

        public static SKPoint Skiapunkt(this Punkt p) => new SKPoint(p.X, p.Y);

        public static Rahmen Modellrahmen(this SKRect r) => new Rahmen(r.Left, r.Top, r.Width, r.Height);

        public static SKRect Skiarahmen(this Rahmen r)
            => SKRect.Create(r.X, r.Y, r.Breite, r.Hoehe);

        /// <summary>Eine ganze Punktfolge ins Modell übersetzt.</summary>
        public static List<Punkt> Modellpunkte(IReadOnlyList<SKPoint> punkte)
        {
            var liste = new List<Punkt>(punkte == null ? 0 : punkte.Count);
            if (punkte != null)
                for (int i = 0; i < punkte.Count; i++) liste.Add(new Punkt(punkte[i].X, punkte[i].Y));
            return liste;
        }
    }

    /// <summary>
    /// Die Schriftkette des Berichts — EINE Stelle für Schriftwahl, Punkt-nach-Pixel
    /// und Zeilenhöhe. Sie ist aus <c>ChartRenderer</c> hierher gezogen, damit auch
    /// der <see cref="SkiaMaler"/> einen Textbefehl in DIESELBE Schrift setzt, in der
    /// das Layout ihn vermessen hat.
    ///
    /// <para><b>Warum die Liste und nicht nur „Calibri" (Entscheidung iF19).</b> Ohne
    /// fontconfig — und genau ohne die läuft die native Linux-Fassung von SkiaSharp,
    /// die die CI benutzt — liefert <c>MatchFamily("Calibri")</c> nichts, und die reine
    /// Systemschrift wäre eine Serifenschrift. Carlito steht direkt hinter Calibri,
    /// weil es metrisch dazu passt. Fällt alles aus, greift die Systemschrift im
    /// gewünschten Stil, dann irgendeine Schrift mit einem „A", zuletzt
    /// <c>SKTypeface.Default</c>.</para>
    ///
    /// <para>Die Punktgrößen des Bestandes (14…22 pt) waren GDI+-Punkte bei 96 dpi.
    /// <c>SKFont</c> rechnet in Pixeln, deshalb pt × 96/72.</para>
    /// </summary>
    public static class Schriftkette
    {
        /// <summary>
        /// Die gesuchten Schriftfamilien in dieser Reihenfolge — die erste vorhandene
        /// gewinnt. Dieselbe Liste benutzt der Excel-Bericht für die Spaltenbreiten,
        /// damit Diagramm und Tabelle desselben Berichts nicht in verschiedenen
        /// Schriften vermessen werden.
        /// </summary>
        public static readonly string[] ERSATZSCHRIFTEN =
        { "Calibri", "Carlito", "Liberation Sans", "DejaVu Sans", "Helvetica", "Arial" };

        private static readonly Dictionary<int, SKTypeface> _schriftarten = new Dictionary<int, SKTypeface>();
        private static readonly object _schloss = new object();

        /// <summary>Schriftart je Stil, einmal ermittelt und dann gehalten.</summary>
        public static SKTypeface Schriftart(bool fett, bool kursiv)
        {
            int schluessel = (fett ? 1 : 0) | (kursiv ? 2 : 0);
            lock (_schloss)
            {
                SKTypeface gefunden;
                if (_schriftarten.TryGetValue(schluessel, out gefunden)) return gefunden;

                var stil = new SKFontStyle(
                    fett ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    SKFontStyleWidth.Normal,
                    kursiv ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

                SKFontManager verwaltung = SKFontManager.Default;
                SKTypeface t = null;
                foreach (string familie in ERSATZSCHRIFTEN)
                {
                    try { t = verwaltung.MatchFamily(familie, stil); } catch { }
                    if (t != null) break;
                }
                if (t == null) try { t = verwaltung.MatchFamily(null, stil); } catch { }
                if (t == null) try { t = verwaltung.MatchCharacter(null, stil, null, 'A'); } catch { }
                if (t == null) t = SKTypeface.Default;

                _schriftarten[schluessel] = t;
                return t;
            }
        }

        /// <summary>Schrift in Punkt (wie im Bestand) — intern nach Bildpunkten umgerechnet.</summary>
        public static SKFont Erzeuge(float punkt, bool fett = false, bool kursiv = false)
        {
            return new SKFont(Schriftart(fett, kursiv), punkt * 96f / 72f)
            {
                Edging = SKFontEdging.Antialias,
                Subpixel = true
            };
        }

        /// <summary>Dieselbe Schrift aus dem Modellsatz.</summary>
        public static SKFont Erzeuge(Schrift schrift)
            => Erzeuge(schrift.Punkt, schrift.Fett, schrift.Kursiv);

        /// <summary>Zeilenhöhe einer Schrift — Ersatz für <c>MeasureString(...).Height</c>.</summary>
        public static float Zeilenhoehe(SKFont f)
        {
            SKFontMetrics m = f.Metrics;
            return m.Descent - m.Ascent;
        }

        /// <summary>
        /// Der Aufstieg einer Schrift in Bildpunkten — der Abstand von der Oberkante eines
        /// Textbefehls zu seiner Grundlinie, genau der Wert, den der Maler beim Zeichnen
        /// abzieht (<c>t.Y - Metrics.Ascent</c>). Das Druck-SVG schreibt damit seine
        /// Grundlinie (<see cref="SkiaMaler.Drucksvg"/>).
        /// </summary>
        public static float Aufstieg(Schrift schrift)
        {
            using (SKFont f = Erzeuge(schrift)) return -f.Metrics.Ascent;
        }
    }

    /// <summary>
    /// Eine Schrift, die BEIDES kann: vermessen und in einen Befehl gehen.
    ///
    /// <para><b>Warum es diesen Typ gibt.</b> Die Layouts des Berichts sind
    /// metrikgetrieben — 46 Stellen setzen einen Text rechtsbündig oder mittig, indem
    /// sie ihn vorher messen. Das Modell trägt dagegen nur fertige Koordinaten und
    /// den Schriftsatz. Ein <see cref="Schriftmass"/> hält beide Seiten zusammen:
    /// <see cref="MeasureText"/> für das Layout, <see cref="Satz"/> für den Befehl.
    /// So bleibt die Textvermessung eine Kern-Funktion, und der Ausgabeweg misst
    /// nichts nach.</para>
    /// </summary>
    public sealed class Schriftmass : IDisposable
    {
        public Schriftmass(Schrift satz)
        {
            Satz = satz;
            Font = Schriftkette.Erzeuge(satz);
        }

        /// <summary>Der Schriftsatz, wie er in den Textbefehl geht.</summary>
        public Schrift Satz { get; }

        /// <summary>Die Skia-Schrift der Vermessung.</summary>
        public SKFont Font { get; }

        /// <summary>Die Breite des Textes [px] — der Ersatz für <c>MeasureString</c>.</summary>
        public float MeasureText(string text) => Font.MeasureText(text ?? "");

        /// <summary>Die Zeilenhöhe [px].</summary>
        public float Hoehe => Schriftkette.Zeilenhoehe(Font);

        public SKFontMetrics Metrics => Font.Metrics;

        public void Dispose() => Font.Dispose();
    }

    /// <summary>
    /// Malt ein <see cref="Zeichenmodell"/> mit SkiaSharp — Befehl für Befehl, in
    /// Zeichenreihenfolge, mit GENAU derselben Paint-Belegung, die der
    /// <c>ChartRenderer</c> vor der Etappe E1 unmittelbar gesetzt hat. Deshalb
    /// ändert die Umstellung kein Bild um ein Byte; die Hash-Messlatte der
    /// ChartProben ist der Nachweis.
    /// </summary>
    public static class SkiaMaler
    {
        /// <summary>
        /// Das Modell als PNG-Bytes — der Weg des Berichts. Ohne eigene Palette malt
        /// er mit <see cref="Farbpalette.Aktuell"/>.
        /// </summary>
        public static byte[] Png(Zeichenmodell modell, Farbpalette palette = null)
        {
            palette = palette ?? Farbpalette.Aktuell;
            using (var flaeche = SKSurface.Create(new SKImageInfo(
                       modell.Breite, modell.Hoehe, SKColorType.Rgba8888, SKAlphaType.Premul)))
            {
                SKCanvas g = flaeche.Canvas;
                g.Clear(palette.Loese(modell.Hintergrund).Skiafarbe());
                Male(g, modell.Befehle, palette);

                using (SKImage bild = flaeche.Snapshot())
                using (SKData daten = bild.Encode(SKEncodedImageFormat.Png, 100))
                    return daten.ToArray();
            }
        }

        /// <summary>
        /// Das Modell als SVG für den DRUCK (Wortbericht, Berichtsvorlagen) —
        /// <see cref="SvgSchreiber.Drucktext"/> mit der Schriftmetrik dieses Malers: Jeder
        /// Text steht auf ausgerechneter Grundlinie (Oberkante + Skia-Aufstieg), ohne
        /// <c>dominant-baseline</c>, das der SVG-Leser von Word übergeht. So liegen PNG und
        /// SVG des Berichts auf derselben Grundlinie.
        /// </summary>
        public static string Drucksvg(Zeichenmodell modell, Farbpalette palette = null)
            => SvgSchreiber.Drucktext(modell, palette, "d", Schriftkette.Aufstieg);

        /// <summary>Eine ganze Befehlsfolge auf eine bestehende Leinwand.</summary>
        public static void Male(SKCanvas g, IReadOnlyList<Zeichenbefehl> befehle, Farbpalette palette = null)
        {
            if (befehle == null) return;
            palette = palette ?? Farbpalette.Aktuell;
            for (int i = 0; i < befehle.Count; i++) Male(g, befehle[i], palette);
        }

        /// <summary>Ein einzelner Befehl auf eine bestehende Leinwand.</summary>
        public static void Male(SKCanvas g, Zeichenbefehl befehl, Farbpalette palette = null)
        {
            palette = palette ?? Farbpalette.Aktuell;
            switch (befehl)
            {
                case Linie l:
                    using (SKPaint p = Stiftpaint(l.Stift, palette)) g.DrawLine(l.X1, l.Y1, l.X2, l.Y2, p);
                    break;

                case Rechteck r:
                    if (r.Fuellung != null)
                        using (SKPaint p = Fuellpaint(r.Fuellung, palette))
                            g.DrawRect(r.X, r.Y, r.Breite, r.Hoehe, p);
                    if (r.Rand != null)
                        using (SKPaint p = Stiftpaint(r.Rand, palette))
                            g.DrawRect(r.X, r.Y, r.Breite, r.Hoehe, p);
                    break;

                case Kreis k:
                    if (k.Fuellung != null)
                        using (SKPaint p = Fuellpaint(k.Fuellung, palette)) g.DrawCircle(k.X, k.Y, k.Radius, p);
                    if (k.Rand != null)
                        using (SKPaint p = Stiftpaint(k.Rand, palette)) g.DrawCircle(k.X, k.Y, k.Radius, p);
                    break;

                case Ellipse e:
                    {
                        SKRect rect = SKRect.Create(e.X, e.Y, e.Breite, e.Hoehe);
                        if (e.Fuellung != null)
                            using (SKPaint p = Fuellpaint(e.Fuellung, palette)) g.DrawOval(rect, p);
                        if (e.Rand != null)
                            using (SKPaint p = Stiftpaint(e.Rand, palette)) g.DrawOval(rect, p);
                        break;
                    }

                case Kreissegment s:
                    {
                        SKRect rect = SKRect.Create(s.X, s.Y, s.Breite, s.Hoehe);

                        // Der VOLLKREIS ist ein eigener Fall (Befund zu Auftrag #222):
                        // SKPath.ArcTo zieht bei 360 Grad NICHTS, Anfang und Ende fallen
                        // zusammen. Ab 360 Grad wird deshalb eine Ellipse gezeichnet.
                        if (Math.Abs(s.Winkel) >= 360f)
                        {
                            if (s.Fuellung != null)
                                using (SKPaint p = Fuellpaint(s.Fuellung, palette)) g.DrawOval(rect, p);
                            if (s.Rand != null)
                                using (SKPaint p = Stiftpaint(s.Rand, palette)) g.DrawOval(rect, p);
                            break;
                        }

                        using (var pfad = new SKPath())
                        {
                            pfad.MoveTo(rect.MidX, rect.MidY);
                            pfad.ArcTo(rect, s.Startwinkel, s.Winkel, false);
                            pfad.Close();
                            if (s.Fuellung != null)
                                using (SKPaint p = Fuellpaint(s.Fuellung, palette)) g.DrawPath(pfad, p);
                            if (s.Rand != null)
                                using (SKPaint p = Stiftpaint(s.Rand, palette)) g.DrawPath(pfad, p);
                        }
                        break;
                    }

                case Pfad f:
                    {
                        if (f.Punkte == null || f.Punkte.Count < 2) break;
                        using (var pfad = new SKPath())
                        {
                            pfad.MoveTo(f.Punkte[0].X, f.Punkte[0].Y);
                            for (int i = 1; i < f.Punkte.Count; i++)
                                pfad.LineTo(f.Punkte[i].X, f.Punkte[i].Y);
                            if (f.Geschlossen) pfad.Close();
                            if (f.Fuellung != null)
                                using (SKPaint p = Fuellpaint(f.Fuellung, palette)) g.DrawPath(pfad, p);
                            if (f.Rand != null)
                                using (SKPaint p = Stiftpaint(f.Rand, palette)) g.DrawPath(pfad, p);
                        }
                        break;
                    }

                case Text t:
                    {
                        if (string.IsNullOrEmpty(t.Inhalt)) break;
                        using (SKFont f = Schriftkette.Erzeuge(t.Schrift))
                        using (SKPaint p = Fuellpaint(new Fuellung(t.Ton), palette))
                        {
                            float x = t.X;
                            if (t.Ausrichtung == Ausrichtung.Mitte) x -= f.MeasureText(t.Inhalt) / 2f;
                            else if (t.Ausrichtung == Ausrichtung.Rechts) x -= f.MeasureText(t.Inhalt);
                            g.DrawText(t.Inhalt, x, t.Y - f.Metrics.Ascent, f, p);
                        }
                        break;
                    }

                case Gruppe gr:
                    if (gr.Zuschnitt.HasValue)
                    {
                        g.Save();
                        g.ClipRect(gr.Zuschnitt.Value.Skiarahmen());
                        Male(g, gr.Befehle, palette);
                        g.Restore();
                    }
                    else Male(g, gr.Befehle, palette);
                    break;
            }
        }

        /// <summary>Der Stift als <c>SKPaint</c> — wörtlich die Belegung von <c>Strich</c>.</summary>
        public static SKPaint Stiftpaint(Stift stift, Farbpalette palette = null)
        {
            palette = palette ?? Farbpalette.Aktuell;
            var p = new SKPaint
            {
                Color = palette.Loese(stift.Ton).Skiafarbe(),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = stift.Breite,
                IsAntialias = stift.Antialias
            };
            if (stift.Kappe != Strichkappe.Stumpf) p.StrokeCap = (SKStrokeCap)(int)stift.Kappe;
            if (stift.Verbindung != Strichverbindung.Gehrung)
                p.StrokeJoin = (SKStrokeJoin)(int)stift.Verbindung;
            if (stift.Muster != null)
                p.PathEffect = SKPathEffect.CreateDash(
                    new[] { stift.Muster.Strich, stift.Muster.Luecke }, stift.Muster.Versatz);
            return p;
        }

        /// <summary>Die Füllung als <c>SKPaint</c> — wörtlich die Belegung von <c>Fuellung</c>.</summary>
        public static SKPaint Fuellpaint(Fuellung fuellung, Farbpalette palette = null)
        {
            palette = palette ?? Farbpalette.Aktuell;
            return new SKPaint
            {
                Color = palette.Loese(fuellung.Ton).Skiafarbe(),
                Style = SKPaintStyle.Fill,
                IsAntialias = fuellung.Antialias
            };
        }
    }
}
