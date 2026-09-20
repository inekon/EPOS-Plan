using System;
using System.Collections.Generic;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der SkiaMaler (Konzept Diagramme, Etappe E1): Er malt ein Zeichenmodell mit
    /// GENAU derselben Paint-Belegung, die der ChartRenderer vor der Umstellung
    /// unmittelbar gesetzt hat.
    ///
    /// <para><b>Die Pruefung ist der Byte-Vergleich.</b> Jeder Fall malt dasselbe
    /// zweimal — einmal ueber das Modell, einmal von Hand mit SkiaSharp, woertlich so,
    /// wie die Helfer des Bestands es taten — und vergleicht die PNG-Bytes. Waere
    /// irgendwo ein Antialias-Schalter, eine Strichverbindung oder eine Reihenfolge
    /// anders, faende dieser Vergleich es; die Hash-Messlatte der ChartProben faende
    /// es am ganzen Bild, aber nicht an der Primitive.</para>
    /// </summary>
    public class SkiaMalerTests
    {
        private const int B = 120;
        private const int H = 80;

        private static Farbton Ton(Farbrolle rolle) => new Farbton(rolle);

        /// <summary>Das Gegenstueck von Hand: Flaeche wie <c>Start</c>, PNG wie <c>Png</c>.</summary>
        private static byte[] VonHand(Action<SKCanvas> malen)
        {
            using (var flaeche = SKSurface.Create(
                       new SKImageInfo(B, H, SKColorType.Rgba8888, SKAlphaType.Premul)))
            {
                flaeche.Canvas.Clear(SKColors.White);
                malen(flaeche.Canvas);
                using (SKImage bild = flaeche.Snapshot())
                using (SKData daten = bild.Encode(SKEncodedImageFormat.Png, 100))
                    return daten.ToArray();
            }
        }

        private static Zeichenmodell Leer()
            => new Zeichenmodell(B, H, Ton(Farbrolle.HINTERGRUND));

        /// <summary>Die Strichbelegung des Bestands (<c>Strich</c>).</summary>
        private static SKPaint Strich(SKColor farbe, float staerke)
            => new SKPaint
            {
                Color = farbe,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = staerke,
                IsAntialias = true
            };

        /// <summary>Die Flaechenbelegung des Bestands (<c>Fuellung</c>).</summary>
        private static SKPaint Flaeche(SKColor farbe)
            => new SKPaint { Color = farbe, Style = SKPaintStyle.Fill, IsAntialias = true };

        private static readonly SKColor BLAU = new SKColor(0x41, 0x72, 0xC4);
        private static readonly SKColor GRAU = new SKColor(0x69, 0x69, 0x69);

        // =====================================================================
        // 1 — die Primitiven, je eine gegen ihr Gegenstueck
        // =====================================================================

        [Fact]
        public void LinieIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Linie(10f, 10f, 100f, 70f, new Stift(Ton(Farbrolle.ACHSE), 2f));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint p = Strich(GRAU, 2f)) g.DrawLine(10f, 10f, 100f, 70f, p);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        [Fact]
        public void GestrichelteLinieIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Linie(5f, 40f, 115f, 40f,
                    new Stift(Ton(Farbrolle.ACHSE), 3f, new Strichmuster(8f, 5f)));

            byte[] hand = VonHand(g =>
            {
                using (var strichel = SKPathEffect.CreateDash(new[] { 8f, 5f }, 0f))
                using (SKPaint p = Strich(GRAU, 3f))
                {
                    p.PathEffect = strichel;
                    g.DrawLine(5f, 40f, 115f, 40f, p);
                }
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        [Fact]
        public void RechteckMitFuellungUndRandIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Rechteck(20f, 15f, 60f, 40f,
                       new Stift(Ton(Farbrolle.LEGENDENRAHMEN), 1f),
                       new Fuellung(Ton(Farbrolle.WAERME_WP)));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint b = Flaeche(BLAU)) g.DrawRect(20f, 15f, 60f, 40f, b);
                using (SKPaint r = Strich(SKColors.Gray, 1f)) g.DrawRect(20f, 15f, 60f, 40f, r);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        [Fact]
        public void KreisIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Kreis(60f, 40f, 25f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_WP)));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint b = Flaeche(BLAU)) g.DrawCircle(60f, 40f, 25f, b);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        [Fact]
        public void EllipseIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Ellipse(10f, 10f, 100f, 60f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_SOLAR)));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint b = Flaeche(new SKColor(0xFF, 0xC0, 0x00)))
                    g.DrawOval(SKRect.Create(10f, 10f, 100f, 60f), b);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        /// <summary>Der Streckenzug — woertlich <c>Linienzug</c> des Bestands.</summary>
        [Fact]
        public void StreckenzugIstByteGleich()
        {
            var punkte = new[] { new SKPoint(5f, 70f), new SKPoint(40f, 20f), new SKPoint(115f, 55f) };

            Zeichenmodell m = Leer();
            m.Pfad(SkiaBruecke.Modellpunkte(punkte), false,
                   new Stift(Ton(Farbrolle.WAERME_WP), 2f, null,
                             Strichkappe.Stumpf, Strichverbindung.Rund));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint stift = Strich(BLAU, 2f))
                {
                    stift.StrokeJoin = SKStrokeJoin.Round;
                    using (var pfad = new SKPath())
                    {
                        pfad.MoveTo(punkte[0]);
                        for (int i = 1; i < punkte.Length; i++) pfad.LineTo(punkte[i]);
                        g.DrawPath(pfad, stift);
                    }
                }
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        /// <summary>Das gefuellte Vieleck — woertlich <c>Vieleck</c> des Bestands.</summary>
        [Fact]
        public void ViereckIstByteGleich()
        {
            var punkte = new[]
            {
                new SKPoint(10f, 70f), new SKPoint(40f, 20f),
                new SKPoint(90f, 30f), new SKPoint(110f, 70f)
            };

            Zeichenmodell m = Leer();
            m.Pfad(SkiaBruecke.Modellpunkte(punkte), true, null,
                   new Fuellung(new Farbton(Farbrolle.WAERME_WP, null, 210)));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint b = Flaeche(BLAU.WithAlpha(210)))
                using (var pfad = new SKPath())
                {
                    pfad.MoveTo(punkte[0]);
                    for (int i = 1; i < punkte.Length; i++) pfad.LineTo(punkte[i]);
                    pfad.Close();
                    g.DrawPath(pfad, b);
                }
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        /// <summary>Das Kreissegment — woertlich <c>Kreissegment</c> des Bestands.</summary>
        [Fact]
        public void KreissegmentIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Fuege(new Kreissegment(15f, 10f, 60f, 60f, -90f, 120f, null,
                                     new Fuellung(Ton(Farbrolle.WAERME_BHKW))));

            byte[] hand = VonHand(g =>
            {
                SKRect rect = SKRect.Create(15f, 10f, 60f, 60f);
                using (SKPaint b = Flaeche(new SKColor(0xED, 0x7D, 0x31)))
                using (var pfad = new SKPath())
                {
                    pfad.MoveTo(rect.MidX, rect.MidY);
                    pfad.ArcTo(rect, -90f, 120f, false);
                    pfad.Close();
                    g.DrawPath(pfad, b);
                }
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        /// <summary>
        /// Der VOLLKREIS ist ein eigener Fall (Befund zu Auftrag #222): ab 360 Grad
        /// eine Ellipse, kein Bogen — sonst bliebe das Bild leer.
        /// </summary>
        [Fact]
        public void VollkreisWirdAlsEllipseGemalt()
        {
            Zeichenmodell m = Leer();
            m.Fuege(new Kreissegment(15f, 10f, 60f, 60f, -90f, 360f, null,
                                     new Fuellung(Ton(Farbrolle.WAERME_BHKW))));

            byte[] hand = VonHand(g =>
            {
                using (SKPaint b = Flaeche(new SKColor(0xED, 0x7D, 0x31)))
                    g.DrawOval(SKRect.Create(15f, 10f, 60f, 60f), b);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));

            // Gegenprobe: das Bild ist NICHT das leere weisse Blatt.
            Assert.NotEqual(SkiaMaler.Png(Leer()), SkiaMaler.Png(m));
        }

        /// <summary>Der Text — woertlich <c>Text</c> des Bestands (linke OBERE Ecke).</summary>
        [Fact]
        public void TextIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Text("Wärme 12,5", 8f, 20f, new Schrift(15f), Ton(Farbrolle.ACHSE));

            byte[] hand = VonHand(g =>
            {
                using (SKFont f = Schriftkette.Erzeuge(15f))
                using (SKPaint p = Flaeche(GRAU))
                    g.DrawText("Wärme 12,5", 8f, 20f - f.Metrics.Ascent, f, p);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        [Fact]
        public void FetterTextIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Text("Titel", 8f, 6f, new Schrift(22f, Fett: true), Ton(Farbrolle.STAMM));

            byte[] hand = VonHand(g =>
            {
                using (SKFont f = Schriftkette.Erzeuge(22f, fett: true))
                using (SKPaint p = Flaeche(new SKColor(0x1F, 0x4E, 0x79)))
                    g.DrawText("Titel", 8f, 6f - f.Metrics.Ascent, f, p);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        /// <summary>Ein leerer Text zeichnet nichts — wie im Bestand.</summary>
        [Fact]
        public void LeererTextZeichnetNichts()
        {
            Zeichenmodell m = Leer();
            m.Text("", 8f, 20f, new Schrift(15f), Ton(Farbrolle.ACHSE));
            m.Text(null, 8f, 40f, new Schrift(15f), Ton(Farbrolle.ACHSE));

            Assert.Equal(SkiaMaler.Png(Leer()), SkiaMaler.Png(m));
        }

        // =====================================================================
        // 2 — Gruppe und Zuschnitt
        // =====================================================================

        /// <summary>Die Gruppe mit Zuschnitt — woertlich Save/ClipRect/Restore.</summary>
        [Fact]
        public void GruppeMitZuschnittIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Gruppe(new Rahmen(20f, 20f, 40f, 40f),
                     z => z.Linie(0f, 0f, 120f, 80f, new Stift(Ton(Farbrolle.WAERME_WP), 4f)));
            m.Linie(0f, 78f, 120f, 78f, new Stift(Ton(Farbrolle.ACHSE), 1f));

            byte[] hand = VonHand(g =>
            {
                g.Save();
                g.ClipRect(SKRect.Create(20f, 20f, 40f, 40f));
                using (SKPaint p = Strich(BLAU, 4f)) g.DrawLine(0f, 0f, 120f, 80f, p);
                g.Restore();
                using (SKPaint p = Strich(GRAU, 1f)) g.DrawLine(0f, 78f, 120f, 78f, p);
            });

            Assert.Equal(hand, SkiaMaler.Png(m));
        }

        /// <summary>Der Zuschnitt WIRKT — sonst sagte der Vergleich oben nichts.</summary>
        [Fact]
        public void DerZuschnittWirkt()
        {
            Zeichenmodell mit = Leer();
            mit.Gruppe(new Rahmen(20f, 20f, 40f, 40f),
                       z => z.Linie(0f, 0f, 120f, 80f, new Stift(Ton(Farbrolle.WAERME_WP), 4f)));

            Zeichenmodell ohne = Leer();
            ohne.Gruppe(null,
                        z => z.Linie(0f, 0f, 120f, 80f, new Stift(Ton(Farbrolle.WAERME_WP), 4f)));

            Assert.NotEqual(SkiaMaler.Png(ohne), SkiaMaler.Png(mit));
        }

        // =====================================================================
        // 3 — Reihenfolge, Determinismus und Palette
        // =====================================================================

        /// <summary>
        /// Die Zeichenreihenfolge zaehlt: Wer zuerst kommt, liegt unten. Eine
        /// Ausgabe, die sortierte oder buendelte, faellt hier auf.
        /// </summary>
        [Fact]
        public void DieZeichenreihenfolgeZaehlt()
        {
            Zeichenmodell a = Leer();
            a.Rechteck(10f, 10f, 60f, 40f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_WP)));
            a.Rechteck(40f, 30f, 60f, 40f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_SOLAR)));

            Zeichenmodell b = Leer();
            b.Rechteck(40f, 30f, 60f, 40f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_SOLAR)));
            b.Rechteck(10f, 10f, 60f, 40f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_WP)));

            Assert.NotEqual(SkiaMaler.Png(b), SkiaMaler.Png(a));
        }

        /// <summary>Zweimal gemalt ist byte-gleich — der Bericht darf nicht wandern.</summary>
        [Fact]
        public void ZweimalGemaltIstByteGleich()
        {
            Zeichenmodell m = Leer();
            m.Linie(0f, 0f, 120f, 80f, new Stift(Ton(Farbrolle.ACHSE), 2f));
            m.Text("Probe", 6f, 6f, new Schrift(16f), Ton(Farbrolle.TEXT));

            Assert.Equal(SkiaMaler.Png(m), SkiaMaler.Png(m));
        }

        /// <summary>
        /// Ohne eigene Palette malt der Maler mit der Vorgabe — und die ist die
        /// Messlatte. Eine getauschte Palette aendert das Bild, ohne dass ein Befehl
        /// angefasst wird: genau das ist der Zweck der Rollen.
        /// </summary>
        [Fact]
        public void EineGetauschtePaletteAendertDasBildOhneBefehlsaenderung()
        {
            Zeichenmodell m = Leer();
            m.Rechteck(10f, 10f, 100f, 60f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_WP)));

            var getauscht = new Farbpalette(
                new Dictionary<Farbrolle, Farbe> { { Farbrolle.WAERME_WP, new Farbe(0xFF, 0x00, 0x00) } },
                Farbpalette.Vorgabe);

            Assert.Equal(SkiaMaler.Png(m, Farbpalette.Vorgabe), SkiaMaler.Png(m));
            Assert.NotEqual(SkiaMaler.Png(m, Farbpalette.Vorgabe), SkiaMaler.Png(m, getauscht));

            byte[] rot = VonHand(g =>
            {
                using (SKPaint b = Flaeche(new SKColor(0xFF, 0x00, 0x00)))
                    g.DrawRect(10f, 10f, 100f, 60f, b);
            });
            Assert.Equal(rot, SkiaMaler.Png(m, getauscht));
        }
    }
}
