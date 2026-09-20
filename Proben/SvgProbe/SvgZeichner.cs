using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SvgProbe
{
    /// <summary>Wie die Stuetzstellen einer Reihe in den Pfad kommen.</summary>
    public enum Pfadart
    {
        /// <summary>Jede Stunde ein Punkt — 8 760 je Reihe.</summary>
        Roh = 0,

        /// <summary>
        /// Jeder n-te Wert, n = Stuetzstellen / Spalten — so zeichnet
        /// <c>ChartRenderer.Jahresgang</c> heute ins PNG. Spitzen zwischen zwei
        /// Stuetzstellen gehen verloren.
        /// </summary>
        JederNte = 1,

        /// <summary>
        /// Je Bildpunktspalte Minimum UND Maximum in Indexreihenfolge — hoechstens
        /// zwei Punkte je Spalte, keine Spitze geht verloren, das Bild ist bei 1:1
        /// vom rohen nicht zu unterscheiden.
        /// </summary>
        Gebuendelt = 2
    }

    /// <summary>
    /// Der Pruefling: ein Jahresgang als SVG, ohne Bibliothek, aus einem kleinen
    /// Zeichenmodell (Titel, Legende, Raster, Achsen, Reihen als Pfade).
    ///
    /// <para><b>Masse wie <c>ChartRenderer.Jahresgang</c>:</b> 1 304 × 440, Zeichenflaeche
    /// (110, 130, 1 154 × 240), Legende bei y = 76, fuenf Rasterstufen, Monatsachse
    /// 0…12. So vergleicht die Probe SVG und PNG bei gleichem Bild.</para>
    ///
    /// <para><b>Die Zeichenflaeche ist ein INNERES <c>&lt;svg&gt;</c> in DATENKOORDINATEN</b>
    /// (x = Stunde, y = 0…1 000 von oben), mit eigener <c>viewBox</c> und
    /// <c>preserveAspectRatio="none"</c>. Zoom und Verschieben sind dann EINE
    /// Attributaenderung dieser viewBox - kein Neuzeichnen, kein Rundlauf; die
    /// Strichstaerke bleibt ueber <c>vector-effect="non-scaling-stroke"</c> stehen.
    /// Genau dieses Verhalten misst svgprobe.mjs.</para>
    /// </summary>
    public static class SvgZeichner
    {
        public const int STUNDEN = 8760;
        public const int BREITE = 1304;
        public const int HOEHE = 440;

        /// <summary>Die Zeichenflaeche des Jahresgangs (links, oben, Breite, Hoehe).</summary>
        public const int FL_X = 110, FL_Y = 130, FL_B = 1154, FL_H = 240;

        /// <summary>Senkrechte Aufloesung der Datenkoordinaten im inneren svg.</summary>
        private const int Y_EINHEITEN = 1000;

        /// <summary>Hausfarben (Hexwerte von <c>ChartRenderer.C_WP … C_NETZ</c>).</summary>
        public static readonly string[] FARBEN =
            { "#4172C4", "#ED7D31", "#70AD47", "#FFC000", "#808080", "#9E480E" };

        private static readonly CultureInfo INV = CultureInfo.InvariantCulture;

        /// <summary>
        /// Synthetische Jahresreihen: Jahresgang (Winter hoch), Tagesgang, eine
        /// schnelle Welle und einzelne Spitzen — fest verdrahtet, ohne Zufall. Die
        /// Spitzen stehen absichtlich zwischen zwei Stuetzstellen der Schrittweite 7:
        /// Sie zeigen, was "jeder n-te" verliert und die Buendelung haelt.
        /// </summary>
        public static double[][] Jahresreihen(int anzahl)
        {
            var reihen = new double[anzahl][];
            for (int r = 0; r < anzahl; r++)
            {
                var w = new double[STUNDEN];
                double phase = r * 0.7;
                for (int i = 0; i < STUNDEN; i++)
                {
                    double tag = i / 24.0;
                    double saison = 0.5 + 0.5 * Math.Cos(2 * Math.PI * (tag - 15) / 365.0);
                    double tagesgang = 0.5 + 0.5 * Math.Sin(2 * Math.PI * (i % 24) / 24.0 + phase);
                    double wert = 20 + 60 * saison * (0.6 + 0.4 * tagesgang) + 10 * Math.Sin(i * 0.37 + r);
                    if (i % (997 + 13 * r) == 3) wert += 40;
                    w[i] = Math.Round(wert * (1 + 0.15 * r), 3);
                }
                reihen[r] = w;
            }
            return reihen;
        }

        /// <summary>
        /// Anzahl der Punkte, die <see cref="Pfad"/> fuer diese Reihe erzeugt — bei der
        /// Buendelung haengt sie von den Werten ab (eine Spalte mit nur einem Extrem
        /// gibt einen Punkt, sonst zwei), deshalb wird gezaehlt statt gerechnet.
        /// </summary>
        public static int Punktzahl(double[] werte, Pfadart art, int spalten)
        {
            var sb = new StringBuilder(werte.Length * 12);
            Pfad(sb, werte, art, spalten, 0, 1);
            int n = 0;
            foreach (char c in sb.ToString()) if (c == ',') n++;
            return n;
        }

        /// <summary>Der komplette Jahresgang als SVG-Text.</summary>
        public static string Svg(double[][] reihen, Pfadart art, int spalten = FL_B)
        {
            if (reihen == null || reihen.Length == 0) throw new ArgumentException("keine Reihen", nameof(reihen));

            double min = double.MaxValue, max = double.MinValue;
            foreach (double[] r in reihen)
                foreach (double w in r) { if (w < min) min = w; if (w > max) max = w; }
            if (min > 0) min = 0;
            if (max < 0) max = 0;
            if (max - min < 1e-9) max = min + 1;
            double schritt = SchoeneStufe((max - min) / 5.0);
            min = Math.Floor(min / schritt) * schritt;
            max = Math.Ceiling(max / schritt) * schritt;

            var sb = new StringBuilder(64 * 1024);
            sb.Append("<svg id=\"svgprobe\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
              .Append(BREITE).Append(' ').Append(HOEHE).Append("\" width=\"").Append(BREITE)
              .Append("\" height=\"").Append(HOEHE)
              .Append("\" role=\"img\" aria-label=\"Jahresgang der Pruefreihen\" ")
              .Append("font-family=\"Calibri, Carlito, 'Liberation Sans', 'DejaVu Sans', Helvetica, Arial, sans-serif\">\n");
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>\n");

            // Titel
            sb.Append("<text x=\"").Append(BREITE / 2).Append("\" y=\"44\" text-anchor=\"middle\" font-size=\"24\" font-weight=\"bold\" fill=\"#333\">")
              .Append("Jahresgang der Prüfreihen (").Append(reihen.Length).Append(" Reihen, ").Append(Name(art)).Append(")</text>\n");

            // Legende bei y = 76
            sb.Append("<g id=\"svgprobe-legende\" font-size=\"20\" fill=\"#333\">\n");
            int lx = FL_X;
            for (int r = 0; r < reihen.Length; r++)
            {
                sb.Append("<rect x=\"").Append(lx).Append("\" y=\"78\" width=\"18\" height=\"12\" fill=\"").Append(Farbe(r)).Append("\"/>");
                sb.Append("<text x=\"").Append(lx + 24).Append("\" y=\"90\" data-reihe=\"").Append(r).Append("\">Reihe ").Append(r + 1).Append("</text>\n");
                lx += 130;
            }
            sb.Append("</g>\n");

            // Raster + y-Beschriftung, gepunktet wie im Vorlaeufer
            sb.Append("<g id=\"svgprobe-raster\" stroke=\"#DCDCDC\" stroke-width=\"1\" stroke-dasharray=\"2 4\">\n");
            for (double wert = min; wert <= max + schritt / 2; wert += schritt)
            {
                double y = FL_Y + FL_H - (wert - min) / (max - min) * FL_H;
                sb.Append("<line x1=\"").Append(FL_X).Append("\" y1=\"").Append(F(y)).Append("\" x2=\"").Append(FL_X + FL_B).Append("\" y2=\"").Append(F(y)).Append("\"/>\n");
            }
            for (int m = 0; m <= 12; m++)
            {
                double x = FL_X + m / 12.0 * FL_B;
                sb.Append("<line x1=\"").Append(F(x)).Append("\" y1=\"").Append(FL_Y).Append("\" x2=\"").Append(F(x)).Append("\" y2=\"").Append(FL_Y + FL_H).Append("\"/>\n");
            }
            sb.Append("</g>\n");
            sb.Append("<g id=\"svgprobe-beschriftung\" font-size=\"20\" fill=\"#696969\">\n");
            for (double wert = min; wert <= max + schritt / 2; wert += schritt)
            {
                double y = FL_Y + FL_H - (wert - min) / (max - min) * FL_H;
                sb.Append("<text x=\"").Append(FL_X - 6).Append("\" y=\"").Append(F(y + 7)).Append("\" text-anchor=\"end\">")
                  .Append(wert.ToString("0.###", INV)).Append("</text>\n");
            }
            for (int m = 0; m <= 12; m++)
            {
                double x = FL_X + m / 12.0 * FL_B;
                sb.Append("<text x=\"").Append(F(x)).Append("\" y=\"").Append(FL_Y + FL_H + 26).Append("\" text-anchor=\"middle\">").Append(m).Append("</text>\n");
            }
            sb.Append("<text x=\"").Append(FL_X + FL_B + 10).Append("\" y=\"").Append(FL_Y + FL_H + 26).Append("\">Monat</text>\n");
            sb.Append("<text x=\"").Append(FL_X).Append("\" y=\"").Append(FL_Y - 10).Append("\">kW</text>\n");
            sb.Append("</g>\n");

            // Achsen
            sb.Append("<g stroke=\"#696969\" stroke-width=\"2\">")
              .Append("<line x1=\"").Append(FL_X).Append("\" y1=\"").Append(FL_Y).Append("\" x2=\"").Append(FL_X).Append("\" y2=\"").Append(FL_Y + FL_H).Append("\"/>")
              .Append("<line x1=\"").Append(FL_X).Append("\" y1=\"").Append(FL_Y + FL_H).Append("\" x2=\"").Append(FL_X + FL_B).Append("\" y2=\"").Append(FL_Y + FL_H).Append("\"/>")
              .Append("</g>\n");

            // Die Zeichenflaeche: inneres svg in Datenkoordinaten.
            int n = reihen[0].Length;
            sb.Append("<svg id=\"svgprobe-flaeche\" x=\"").Append(FL_X).Append("\" y=\"").Append(FL_Y)
              .Append("\" width=\"").Append(FL_B).Append("\" height=\"").Append(FL_H)
              .Append("\" viewBox=\"0 0 ").Append(n - 1).Append(' ').Append(Y_EINHEITEN)
              .Append("\" preserveAspectRatio=\"none\" data-stunden=\"").Append(n).Append("\">\n");
            for (int r = 0; r < reihen.Length; r++)
            {
                sb.Append("<path class=\"svgprobe-reihe\" data-reihe=\"").Append(r).Append("\" fill=\"none\" stroke=\"")
                  .Append(Farbe(r)).Append("\" stroke-width=\"").Append(r == 0 ? 2 : 1)
                  .Append("\" stroke-linejoin=\"round\" vector-effect=\"non-scaling-stroke\" d=\"");
                Pfad(sb, reihen[r], art, spalten, min, max);
                sb.Append("\"/>\n");
            }
            sb.Append("</svg>\n</svg>\n");
            return sb.ToString();
        }

        /// <summary>Der Pfad EINER Reihe in Datenkoordinaten (x = Index, y = 0…1 000).</summary>
        public static void Pfad(StringBuilder sb, double[] werte, Pfadart art, int spalten, double min, double max)
        {
            double spanne = max - min;
            bool erster = true;
            void Punkt(int i)
            {
                double y = (max - werte[i]) / spanne * Y_EINHEITEN;
                if (y < 0) y = 0; else if (y > Y_EINHEITEN) y = Y_EINHEITEN;
                sb.Append(erster ? "M" : " ").Append(i).Append(',').Append(y.ToString("0.#", INV));
                erster = false;
            }

            switch (art)
            {
                case Pfadart.Roh:
                    for (int i = 0; i < werte.Length; i++) Punkt(i);
                    break;
                case Pfadart.JederNte:
                    {
                        int schritt = Math.Max(1, werte.Length / spalten);
                        for (int i = 0; i < werte.Length; i += schritt) Punkt(i);
                        break;
                    }
                default:
                    foreach ((int von, int bis) in Buendel(werte.Length, spalten))
                    {
                        int iMin = von, iMax = von;
                        for (int i = von + 1; i < bis; i++)
                        {
                            if (werte[i] < werte[iMin]) iMin = i;
                            if (werte[i] > werte[iMax]) iMax = i;
                        }
                        if (iMin == iMax) Punkt(iMin);
                        else if (iMin < iMax) { Punkt(iMin); Punkt(iMax); }
                        else { Punkt(iMax); Punkt(iMin); }
                    }
                    break;
            }
        }

        /// <summary>Die Indexbereiche je Bildpunktspalte, [von, bis).</summary>
        private static IEnumerable<(int Von, int Bis)> Buendel(int n, int spalten)
        {
            int start = 0;
            for (int c = 1; c <= spalten; c++)
            {
                int ende = (int)((long)c * n / spalten);
                if (ende > start) { yield return (start, ende); start = ende; }
            }
        }

        private static double SchoeneStufe(double roh)
        {
            double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
            foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (zehner * f >= roh) return zehner * f;
            return zehner * 10;
        }

        private static string Farbe(int r) => FARBEN[r % FARBEN.Length];
        private static string F(double v) => v.ToString("0.#", INV);

        public static string Name(Pfadart art) =>
            art == Pfadart.Roh ? "roh" : art == Pfadart.JederNte ? "jeder n-te" : "gebuendelt";

        /// <summary>Die Pfadart aus dem Adresswort (<c>roh</c>, <c>jedernte</c>, <c>gebuendelt</c>).</summary>
        public static Pfadart Art(string? wort)
        {
            switch ((wort ?? "").Trim().ToLowerInvariant())
            {
                case "roh": return Pfadart.Roh;
                case "jedernte": return Pfadart.JederNte;
                default: return Pfadart.Gebuendelt;
            }
        }
    }
}
