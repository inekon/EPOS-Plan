using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die kleine Kurve des Kesseleditors</b> (Konzept Kesselkennlinie 5, erster Punkt): der Wirkungsgrad über der
    /// Last, bei der Brennwertkennlinie je Rücklauf eine Linie. Die Punkte rechnet der Kern
    /// (<see cref="Kesselkennlinie.Kurven"/>); hier wird nur gezeichnet.
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>Breite des Kesselkennlinienbilds [px].</summary>
        public const int KESSELKENNLINIE_BREITE = 720;

        /// <summary>Höhe des Kesselkennlinienbilds [px].</summary>
        public const int KESSELKENNLINIE_HOEHE = 430;

        /// <summary>Luft über und unter den Wirkungsgraden, bevor die Achse gestuft wird (Faktor).</summary>
        private const double KESSELKENNLINIE_LUFT = 0.01;

        /// <summary>Eine Linie des Kesselkennlinienbilds: ihr Name (Legende) und ihre Punkte η(β).</summary>
        public sealed record KesselkennlinienReihe(string Name, IReadOnlyList<Kesselkurvenpunkt> Punkte);

        /// <summary>
        /// Das Kesselkennlinienbild als PNG — für die Proben; die Oberfläche nimmt
        /// <see cref="KesselkennlinieModell"/>.
        /// </summary>
        public static byte[] KesselkennlinieBild(string titel, string xTitel, string yTitel,
                                                 IReadOnlyList<KesselkennlinienReihe> reihen)
            => SkiaMaler.Png(KesselkennlinieModell(titel, xTitel, yTitel, reihen));

        /// <summary>
        /// <b>Wirkungsgrad über der Last</b> als Zeichenmodell: x die Laststufe in Prozent der Nennleistung (0 bis 100),
        /// y der Wirkungsgrad als Faktor, eine Linie samt Punktmarken je Reihe.
        /// </summary>
        /// <remarks>
        /// <para><b>Ein reines Pixelbild ohne Zeichenfläche</b> (wie die Kennlinien der Wärmepumpe, Entscheid DG-E3-7): zehn
        /// Stützstellen tragen keinen Zoom. Jede Punktmarke nennt ihren Wert („Last 30 % · Rücklauf 30 °C: 1,050“), und je
        /// Linie steht eine <c>Datenreihe</c> mit der Last als x-Stelle — die Quelle der Zeigerzeile. Linie und Marken tragen
        /// die Marke ihrer Reihe; die Legende schaltet beide zusammen.</para>
        /// <para><b>Die y-Achse schließt die Null NICHT ein</b> — anders als bei COP und Leistung: Die Aussage des Bildes
        /// sind wenige Prozentpunkte zwischen Teillast und Nennlast und zwischen den Rückläufen; auf einer Achse ab null
        /// lägen alle Linien aufeinander. Die Achse steht auf den Werten mit
        /// <see cref="KESSELKENNLINIE_LUFT"/> Luft, gestuft wie die Kennlinien (<see cref="Skala.Stufe"/>).</para>
        /// <para><b>Nicht endliche Werte fallen weg</b>; eine Reihe mit weniger als zwei Punkten fällt ganz weg, ohne Reihe
        /// steht der Leerhinweis.</para>
        /// </remarks>
        /// <param name="titel">Überschrift, z. B. „Wirkungsgrad über der Last“.</param>
        /// <param name="xTitel">Beschriftung der Lastachse, z. B. „Last [%]“.</param>
        /// <param name="yTitel">Beschriftung der Wirkungsgradachse, z. B. „Wirkungsgrad“.</param>
        /// <param name="reihen">Eine Reihe je Rücklauf, ohne Brennwertkennlinie genau eine.</param>
        public static Zeichenmodell KesselkennlinieModell(string titel, string xTitel, string yTitel,
                                                          IReadOnlyList<KesselkennlinienReihe> reihen)
        {
            int W = KESSELKENNLINIE_BREITE, H = KESSELKENNLINIE_HOEHE;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            // Links die Wirkungsgrade, rechts Luft für die letzte Lastzahl; unter der Fläche Lastzahlen, Achsentitel
            // und die Legende - sie beginnt am Titel, damit drei Rückläufe in einer Zeile stehen, und hat Platz für
            // eine zweite.
            var rc = SKRect.Create(90f, 80f, W - 130f, 230f);

            var gueltig = new List<(string Name, double[] X, double[] Y)>();
            foreach (KesselkennlinienReihe r in reihen ?? Array.Empty<KesselkennlinienReihe>())
            {
                if (r?.Punkte == null) continue;
                var x = new List<double>();
                var y = new List<double>();
                foreach (Kesselkurvenpunkt p in r.Punkte)
                {
                    if (!Endlich(p.Laststufe) || !Endlich(p.Wirkungsgrad)) continue;
                    x.Add(p.Laststufe * 100.0);
                    y.Add(p.Wirkungsgrad);
                }
                if (x.Count >= 2) gueltig.Add((r.Name ?? "", x.ToArray(), y.ToArray()));
            }

            if (gueltig.Count == 0)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Keine Kennlinien vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            const double xMin = 0.0, xMax = 100.0, xSchritt = 20.0;
            double yMin = gueltig.Min(g => g.Y.Min()) - KESSELKENNLINIE_LUFT;
            double yMax = gueltig.Max(g => g.Y.Max()) + KESSELKENNLINIE_LUFT;
            double ySchritt = Skala.Stufe(ref yMin, ref yMax);
            // Zwei Nachkommastellen, wo die Stufe sie trägt (0,05; 0,1), sonst drei (0,025) - sonst stünde
            // 1,075 als „1,08“ an der Achse.
            string yFormat = Math.Abs(ySchritt * 100.0 - Math.Round(ySchritt * 100.0)) < 1e-9 ? "0.00" : "0.000";

            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(14f))
                    for (double wert = yMin; wert <= yMax + ySchritt / 2; wert += ySchritt)
                    {
                        float y = (float)(rc.Bottom - (wert - yMin) / (yMax - yMin) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString(yFormat, Zahlkultur);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f, y - TextHoehe(f) / 2f);
                    }
            });
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(14f))
                    for (double wert = xMin; wert <= xMax + xSchritt / 2; wert += xSchritt)
                    {
                        float x = (float)(rc.Left + (wert - xMin) / (xMax - xMin) * rc.Width);
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = wert.ToString("0", Zahlkultur);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE, rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 32f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            for (int i = 0; i < gueltig.Count; i++)
            {
                (string name, double[] xw, double[] yw) = gueltig[i];
                Farbrolle rolle = Serienrolle(i);
                var punkte = new SKPoint[xw.Length];
                var werttexte = new string[xw.Length];
                for (int t = 0; t < xw.Length; t++)
                {
                    float x = (float)(rc.Left + (xw[t] - xMin) / (xMax - xMin) * rc.Width);
                    float y = (float)(rc.Bottom - (yw[t] - yMin) / (yMax - yMin) * rc.Height);
                    punkte[t] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                    werttexte[t] = Achsenwert(xTitel, xw[t], "0") + WERT_TRENNER +
                                   Elementwert(name, yw[t], "0.000", "");
                }

                string marke = "reihe:" + name;
                z.Markiert(marke, name, zr =>
                {
                    Linienzug(zr, punkte, Stift(rolle, 3f, null, Strichverbindung.Rund));
                    Punktmarken(zr, punkte, rolle, Kennlinienmarke.Kreis, marke, werttexte);
                });

                // Die Linie in DATENWERTEN - x die Last in Prozent, y der Wirkungsgrad. Ohne Zeichenfläche zeichnet der
                // Schreiber daraus nichts; die Reihe ist die Quelle der Zeigerzeile.
                z.FuegeReihe(new Datenreihe(name, Farbton.Aus(rolle), 3f, null, yw, null, Reihenart.Linie, null, null,
                                            xw, yTitel));
            }

            // Das Legendenfeld trägt die Serienrolle seiner Linie (Regel „Die Farbe einer Reihe ist ihre Rolle“).
            Legende(z, gueltig.Select((g, i) => new Segment(g.Name, 0, C_SERIEN[i % C_SERIEN.Length])
                        { Ton = Farbton.Aus(Serienrolle(i)) }).ToList(),
                    24f, rc.Bottom + 58f, W - 30f);
            return z;
        }
    }
}
