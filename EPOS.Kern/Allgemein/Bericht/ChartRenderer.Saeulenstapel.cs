using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Gestapelte Säulen mit freier Fächerzahl</b> — die Summen je Monat (12), Woche (52) oder Tag
    /// (365) im Grafikreiter des Dialogs „Wärme-/Strombedarf“, eine Stapelschicht je Bedarfsart.
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>Breite des Säulenstapelbilds [px] — dieselbe wie die des Jahresgangs.</summary>
        public const int SAEULENSTAPEL_BREITE = 1304;

        /// <summary>Höhe des Säulenstapelbilds [px] — dieselbe wie die des Jahresgangs.</summary>
        public const int SAEULENSTAPEL_HOEHE = 440;

        /// <summary>
        /// <b>Das Zeichenmodell der gestapelten Säulen</b>: Maß und Lage von Titel, Legende und Fläche
        /// wie beim <see cref="JahresgangModell"/>, damit Jahr und Summen im Reiter gleich groß stehen;
        /// die Schichten wie beim <see cref="MonatsStapelModell"/> — <c>reihe:&lt;Name&gt;</c> mit dem
        /// Wert „Woche 7 · Gebäude: 12,3 MWh“ am Zeiger.
        ///
        /// <para><b>Ein reines Pixelbild</b> wie der Monatsstapel: keine Zeichenfläche, keine
        /// <c>Datenreihe</c> — Fächer sind keine Zeitachse, es gibt nichts zu zoomen. Den CSV-Export
        /// der Summen trägt der Wirt.</para>
        /// </summary>
        /// <param name="titel">Überschrift.</param>
        /// <param name="einheit">Einheit für Überschrift und Zeigetext; leer = ohne.</param>
        /// <param name="reihen">Reihen mit je <paramref name="namen"/>.Count Werten, von unten nach oben.</param>
        /// <param name="namen">Der Name je Fach für den Zeigetext („Jan“, „Woche 7“, „Tag 45“).</param>
        /// <param name="achse">Die Beschriftung je Fach unter der Achse; ein leerer Eintrag bleibt unbeschriftet.</param>
        public static Zeichenmodell SaeulenstapelModell(string titel, string einheit,
                                                        IReadOnlyList<Reihe> reihen,
                                                        IReadOnlyList<string> namen,
                                                        IReadOnlyList<string> achse)
        {
            int W = SAEULENSTAPEL_BREITE, H = SAEULENSTAPEL_HOEHE;
            int n = namen?.Count ?? 0;

            var z = Modell(W, H);
            z.Markiert("titel", zt =>
                Titel(zt, string.IsNullOrEmpty(einheit) ? (titel ?? "")
                                                        : (titel ?? "") + "  [" + einheit + "]", W));

            var gueltig = (reihen ?? new List<Reihe>())
                .Where(r => r != null && r.Werte != null && n > 0 && r.Werte.Length >= n)
                .ToList();

            var rc = SKRect.Create(110f, 130f, W - 150f, 240f);
            if (gueltig.Count == 0)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            Legende(z, gueltig.Select(r => Eintrag(r)).ToList(), 110f, 76f, W - 30f);

            var summe = new double[n];
            for (int i = 0; i < n; i++)
                foreach (Reihe r in gueltig)
                    if (double.IsFinite(r.Werte[i])) summe[i] += Math.Max(r.Werte[i], 0);
            double max = Nice(summe.Max());
            if (max <= 0) max = 1;

            z.Markiert("yachse", zy => YRasterOhneKreuz(zy, rc, max));
            Achsenkreuz(z, rc);

            float slot = rc.Width / n;
            using (var f = Schrift(13f))
                for (int i = 0; i < n; i++)
                {
                    string lab = (achse != null && i < achse.Count) ? achse[i] : null;
                    if (string.IsNullOrEmpty(lab)) continue;
                    float x = rc.Left + (i + 0.5f) * slot;
                    z.Markiert("xachse", zx =>
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f));
                }

            // Das Zahlenformat der y-Achse (YRasterOhneKreuz) — der Zeigetext trägt dieselben
            // Nachkommastellen wie die Beschriftung daneben.
            string format = max >= 10 ? "N0" : (max >= 1 ? "N1" : "N2");

            // Ab etwa 100 Fächern (Tage) steht die Säule ohne Lücke, sonst wäre sie dünner als ein Bildpunkt.
            float balken = n > 100 ? slot : slot * 0.6f;
            for (int i = 0; i < n; i++)
            {
                float x0 = rc.Left + i * slot + (slot - balken) / 2f;
                float unten = rc.Bottom;
                string fach = namen[i] ?? "";
                int ii = i;
                foreach (Reihe r in gueltig)
                {
                    double wert = double.IsFinite(r.Werte[i]) ? Math.Max(r.Werte[i], 0) : 0;
                    float hoehe = (float)(wert / max * rc.Height);
                    if (hoehe <= 0) continue;
                    float oben = unten - hoehe;
                    Reihe rr = r;
                    z.Markiert("reihe:" + (r.Name ?? ""),
                               Elementwert(fach + WERT_TRENNER + (r.Name ?? ""),
                                           rr.Werte[ii], format, einheit),
                               zs => zs.Rechteck(x0, oben, balken, hoehe, null, Flaeche(Ton(rr))));
                    unten -= hoehe;
                }
            }

            return z;
        }
    }
}
