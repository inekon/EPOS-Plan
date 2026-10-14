using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Bivalenzdiagramm</b> (Fachkonzept Übergabegrenze 7.2; Umsetzungskonzept 7.2): Leistung über der
    /// Außentemperatur mit den Bereichsflächen, der Heizlast, dem Kennfeld und der Übergabegrenze bei Höchstvorlauf, der
    /// Leistung der Wärmepumpe als Fläche und den senkrechten Marken. Alles Gerechnete liefert
    /// <see cref="BivalenzdiagrammModell"/>; hier wird nur gezeichnet.
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>Breite des Bivalenzdiagramms [px].</summary>
        public const int BIVALENZ_BREITE = 900;

        /// <summary>Höhe des Bivalenzdiagramms [px].</summary>
        public const int BIVALENZ_HOEHE = 540;

        /// <summary>Deckung der Bereichsflächen (0–255).</summary>
        private const byte BIVALENZ_BEREICH_DECKUNG = 40;

        /// <summary>Deckung der Fläche der Wärmepumpe (0–255).</summary>
        private const byte BIVALENZ_WP_DECKUNG = 90;

        /// <summary>Das Bivalenzdiagramm als PNG — für Bericht und Proben; die Oberfläche nimmt <see cref="BivalenzdiagrammZeichnung"/>.</summary>
        public static byte[] Bivalenzdiagramm(BivalenzdiagrammModell modell)
            => SkiaMaler.Png(BivalenzdiagrammZeichnung(modell));

        /// <summary>Die Farbrolle einer Bereichsfläche (Diagrammfarbtafel, keine eigene Farbe).</summary>
        private static Farbrolle BivalenzFlaechenrolle(BivalenzFlaechenart art) => art switch
        {
            BivalenzFlaechenart.NurKessel => Farbrolle.WAERME_KESSEL,
            BivalenzFlaechenart.Vorwaermung => Farbrolle.UEBERSCHUSS,
            BivalenzFlaechenart.Parallel => Farbrolle.SERIE_3,
            _ => Farbrolle.SERIE_2,
        };

        /// <summary>
        /// <b>Das Bivalenzdiagramm als Zeichenmodell.</b> x die Außentemperatur, y die Leistung, beide Achsen aus dem
        /// Modell (runde Schritte). Von hinten nach vorn: Bereichsflächen (nur Kessel, Vorwärmung, parallel,
        /// Wärmepumpe allein) mit Namen oben, Raster, die Wärmepumpe als Fläche (Rolle <c>SERIE_2</c>), Stundenpunkte
        /// (<c>SERIE_5</c>), Übergabegrenze (Waagerechte, gepunktet, <c>SERIE_8</c>), Kennfeld (gestrichelt,
        /// <c>SERIE_3</c>), Heizlast (<c>BEDARF</c>), Marken θ_biv,1, θ_biv,2 (<c>ACHSE</c>, gestrichelt), Abschaltpunkt
        /// (<c>WAERME_KESSEL</c>) und „nach Kennfeld“ (<c>SERIE_3</c>, gepunktet), zuletzt die Legende. Ohne Übergabedaten
        /// steht der Platzhaltertext.
        /// </summary>
        public static Zeichenmodell BivalenzdiagrammZeichnung(BivalenzdiagrammModell modell)
        {
            int W = BIVALENZ_BREITE, H = BIVALENZ_HOEHE;
            modell ??= BivalenzdiagrammModell.Platzhalter();
            BivalenzdiagrammTexte t = modell.Texte;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, t.Titel ?? "", W));
            var rc = SKRect.Create(80f, 80f, W - 120f, 330f);

            if (!modell.MitUebergabe)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, t.Platzhalter, f, Farbrolle.ACHSE, rc.Left, rc.Top + 20f));
                return z;
            }

            double xMin = modell.XVon, xMax = modell.XBis, yMax = modell.YBis;
            float X(double x) => (float)(rc.Left + (x - xMin) / (xMax - xMin) * rc.Width);
            float Y(double y) => (float)(rc.Bottom - Math.Max(0.0, Math.Min(yMax, y)) / yMax * rc.Height);
            string Grad(double c) => c.ToString("+0.0;−0.0;0.0", Zahlkultur);
            string vorlauf = modell.HoechstvorlaufC.ToString("0", Zahlkultur);

            // Bereichsflächen mit ihrem Namen oben, wenn er hineinpasst.
            z.Markiert("bereiche", zb =>
            {
                using (var f = Schrift(13f))
                    foreach (BivalenzFlaeche fl in modell.Flaechen)
                    {
                        float x0 = X(fl.VonC), x1 = X(fl.BisC);
                        if (!(x1 > x0)) continue;
                        Farbrolle rolle = BivalenzFlaechenrolle(fl.Art);
                        zb.Rechteck(x0, rc.Top, x1 - x0, rc.Height, null,
                                    Flaeche(Farbton.Aus(rolle).MitDeckung(BIVALENZ_BEREICH_DECKUNG)));
                        string name = fl.Art switch
                        {
                            BivalenzFlaechenart.NurKessel => t.NurKessel,
                            BivalenzFlaechenart.Vorwaermung => t.Vorwaermung,
                            BivalenzFlaechenart.Parallel => t.Parallel,
                            _ => t.WaermepumpeAllein,
                        };
                        float breite = f.MeasureText(name ?? "");
                        if (breite + 6f < x1 - x0)
                            Text(zb, name, f, Farbrolle.TEXT, (x0 + x1 - breite) / 2f, rc.Top + 6f);
                    }
            });

            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(14f))
                    for (double wert = 0.0; wert <= yMax + modell.YSchritt / 2; wert += modell.YSchritt)
                    {
                        float y = Y(wert);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.#", Zahlkultur);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f, y - TextHoehe(f) / 2f);
                    }
            });
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(14f))
                    for (double wert = xMin; wert <= xMax + modell.XSchritt / 2; wert += modell.XSchritt)
                    {
                        float x = X(wert);
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = wert.ToString("0.#;−0.#;0", Zahlkultur);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            // Die Wärmepumpe als Fläche bis zur Nulllinie.
            if (modell.Waermepumpe.Count >= 2)
            {
                var umriss = new List<Punkt>();
                umriss.Add(new Punkt(X(modell.Waermepumpe[0].AussenC), Y(0.0)));
                foreach (BivalenzKurvenpunkt p in modell.Waermepumpe) umriss.Add(new Punkt(X(p.AussenC), Y(p.Leistung)));
                umriss.Add(new Punkt(X(modell.Waermepumpe[modell.Waermepumpe.Count - 1].AussenC), Y(0.0)));
                z.Markiert("reihe:" + t.Waermepumpe, t.Waermepumpe, zw =>
                    zw.Pfad(umriss, true, Stift(Farbrolle.SERIE_2, 1.5f),
                            Flaeche(Farbton.Aus(Farbrolle.SERIE_2).MitDeckung(BIVALENZ_WP_DECKUNG))));
            }

            if (modell.Stundenpunkte.Count > 0)
                z.Markiert("reihe:" + t.Stunden, t.Stunden, zs =>
                {
                    foreach (BivalenzStundenpunkt p in modell.Stundenpunkte)
                        zs.Kreis(X(p.AussenC), Y(p.Leistung), 2f, null, Flaeche(Farbrolle.SERIE_5));
                });

            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, t.AchseX ?? "", f, Farbrolle.ACHSE, rc.Right - f.MeasureText(t.AchseX ?? ""), rc.Bottom + 32f));
                z.Markiert("yachse", zy =>
                    Text(zy, t.AchseY ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            string uebergabe = string.Format(Zahlkultur, t.Uebergabe ?? "", vorlauf);
            string kennfeld = string.Format(Zahlkultur, t.Kennfeld ?? "", vorlauf);
            if (Endlich(modell.Uebergabegrenze))
            {
                float y = Y(modell.Uebergabegrenze);
                z.Markiert("reihe:" + uebergabe, uebergabe, zu =>
                    zu.Linie(rc.Left, y, rc.Right, y, Stift(Farbrolle.SERIE_8, 2.5f, Strichfolge(Strichart.Gepunktet))));
            }
            z.Markiert("reihe:" + kennfeld, kennfeld, zk =>
                Linienzug(zk, modell.Kennfeld.Select(p => new SKPoint(X(p.AussenC), Y(p.Leistung))).ToArray(),
                          Stift(Farbrolle.SERIE_3, 2.5f, Strichfolge(Strichart.Gestrichelt))));
            z.Markiert("reihe:" + t.Heizlast, t.Heizlast, zh =>
                Linienzug(zh, modell.Heizlast.Select(p => new SKPoint(X(p.AussenC), Y(p.Leistung))).ToArray(),
                          Stift(Farbrolle.BEDARF, 3f, null, Strichverbindung.Rund)));

            // Die Marken: senkrechte Linien, die Beschriftung je Art in eigener Zeile, rechts der Linie oder - wo der
            // Platz rechts nicht reicht oder die Art es verlangt - links davon.
            using (var f = Schrift(13f))
                foreach (BivalenzMarke m in modell.Marken)
                {
                    float x = X(m.AussenC);
                    (Farbrolle rolle, Strichart art, string vorlage, float zeileY, bool links) = m.Art switch
                    {
                        BivalenzMarkenart.ErsterBivalenzpunkt => (Farbrolle.ACHSE, Strichart.Gestrichelt, t.ErsterBivalenzpunkt, rc.Top + 30f, false),
                        BivalenzMarkenart.ZweiterBivalenzpunkt => (Farbrolle.ACHSE, Strichart.Gestrichelt, t.ZweiterBivalenzpunkt, rc.Top + 50f, true),
                        BivalenzMarkenart.NachKennfeld => (Farbrolle.SERIE_3, Strichart.Gepunktet, t.NachKennfeld, rc.Top + 70f, true),
                        _ => (Farbrolle.WAERME_KESSEL, Strichart.Gepunktet, t.Abschaltpunkt, rc.Bottom - 22f, false),
                    };
                    string text = string.Format(Zahlkultur, vorlage ?? "", Grad(m.AussenC));
                    float breite = f.MeasureText(text);
                    if (!links && x + 4f + breite > rc.Right) links = true;
                    if (links && x - 4f - breite < rc.Left) links = false;
                    float tx = links ? x - 4f - breite : x + 4f;
                    z.Markiert("marke:" + m.Art, text, zm =>
                    {
                        zm.Linie(x, rc.Top, x, rc.Bottom, Stift(rolle, 1.5f, Strichfolge(art)));
                        Text(zm, text, f, Farbrolle.TEXT, tx, zeileY);
                    });
                }

            var legende = new List<Segment>
            {
                new Segment(t.Heizlast, 0, C_SERIEN[0]) { Ton = Farbton.Aus(Farbrolle.BEDARF) },
                new Segment(kennfeld, 0, C_SERIEN[0], Strichart.Gestrichelt) { Ton = Farbton.Aus(Farbrolle.SERIE_3) },
            };
            if (Endlich(modell.Uebergabegrenze))
                legende.Add(new Segment(uebergabe, 0, C_SERIEN[0], Strichart.Gepunktet) { Ton = Farbton.Aus(Farbrolle.SERIE_8) });
            legende.Add(new Segment(t.Waermepumpe, 0, C_SERIEN[0]) { Ton = Farbton.Aus(Farbrolle.SERIE_2) });
            if (modell.Stundenpunkte.Count > 0)
                legende.Add(new Segment(t.Stunden, 0, C_SERIEN[0]) { Ton = Farbton.Aus(Farbrolle.SERIE_5) });
            Legende(z, legende, 24f, rc.Bottom + 62f, W - 30f);
            return z;
        }
    }
}
