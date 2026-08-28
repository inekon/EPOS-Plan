using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Off-Screen-Diagramm-Rendering für den Bericht (Konzept Kap. 6) auf Basis
    /// System.Drawing — bewusst ohne UI-Handle und ohne Fremd-API, damit das Rendering
    /// im Hintergrund-Thread deterministisch läuft (gleiches Muster wie die
    /// Kuchendiagramme des Bestandsberichts). Alle Methoden liefern PNG-Bytes;
    /// gerendert wird in doppelter Zielauflösung (Einbettung skaliert herunter).
    ///
    /// Feste Farbzuordnung je Erzeuger über alle Diagramme (Konzept Kap. 6):
    /// WP blau, BHKW orange, Kessel grau, Solar gelb, PV grün, Netz/Rest neutral.
    /// </summary>
    public static class ChartRenderer
    {
        // Palette (identisch zum Bestandsbericht).
        public static readonly Color C_WP = Color.FromArgb(0x41, 0x72, 0xC4);
        public static readonly Color C_BHKW = Color.FromArgb(0xED, 0x7D, 0x31);
        public static readonly Color C_KESSEL = Color.FromArgb(0x80, 0x80, 0x80);
        public static readonly Color C_SOLAR = Color.FromArgb(0xFF, 0xC0, 0x00);
        public static readonly Color C_PV = Color.FromArgb(0x70, 0xAD, 0x47);
        public static readonly Color C_NETZ = Color.FromArgb(0x9E, 0x48, 0x0E);
        public static readonly Color C_REST = Color.FromArgb(0xBF, 0xBF, 0xBF);
        public static readonly Color C_BEDARF = Color.FromArgb(0x33, 0x33, 0x33);
        public static readonly Color C_STAMM = Color.FromArgb(0x1F, 0x4E, 0x79);

        /// <summary>
        /// PAKET E1: Farbfolge der Wärmespeicher-Füllstandslinien (Konzept 6.3) — sie
        /// wiederholt sich, wenn ein Projekt mehr Speicher führt als Farben da sind.
        /// Dieselbe Reihenfolge wie <c>NavigatorWaerme.SPEICHER_FARBEN</c>, damit
        /// Bildschirm und Bericht denselben Speicher gleich einfärben.
        /// </summary>
        public static readonly Color[] C_SPEICHER =
        {
            Color.MediumVioletRed, Color.DarkViolet, Color.Teal,
            Color.SaddleBrown, Color.DarkSlateGray, Color.Crimson
        };

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        public class Segment
        {
            public string Label; public double Wert; public Color Farbe;
            public Segment(string l, double w, Color f) { Label = l; Wert = w; Farbe = f; }
        }

        public class Balken
        {
            public string Label; public double Wert; public bool Hervorheben;
            public Balken(string l, double w, bool hervor) { Label = l; Wert = w; Hervorheben = hervor; }
        }

        public class Reihe
        {
            public string Name; public double[] Werte; public Color Farbe;
            public Reihe(string n, double[] w, Color f) { Name = n; Werte = w; Farbe = f; }
        }

        // =================================================================== Kuchen

        /// <summary>Kuchendiagramm (Deckungsanteile) — Portierung aus dem Bestandsbericht.</summary>
        public static byte[] Kuchen(string titel, List<Segment> segmente)
        {
            int W = 960, H = 600;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, titel, W);

                double total = segmente.Sum(s => Math.Max(s.Wert, 0));
                if (total <= 0) total = 1;

                var rect = new RectangleF(40f, 90f, 440f, 440f);
                float start = -90f;
                foreach (Segment s in segmente)
                {
                    float sweep = (float)(Math.Max(s.Wert, 0) / total * 360.0);
                    using (var b = new SolidBrush(s.Farbe))
                        g.FillPie(b, rect.X, rect.Y, rect.Width, rect.Height, start, sweep);
                    start += sweep;
                }
                using (var stift = new Pen(Color.White, 3f)) g.DrawEllipse(stift, rect);

                float lx = 540f, ly = 110f;
                using (var lf = new Font("Calibri", 19f))
                    foreach (Segment s in segmente)
                    {
                        using (var b = new SolidBrush(s.Farbe)) g.FillRectangle(b, lx, ly, 28f, 28f);
                        g.DrawRectangle(Pens.Gray, lx, ly, 28f, 28f);
                        g.DrawString(s.Label + "   " + (s.Wert / total * 100.0).ToString("N1", DE) + " %",
                                     lf, Brushes.Black, lx + 40f, ly + 1f);
                        ly += 48f;
                    }
                return Png(bmp);
            }
        }

        // =================================================================== Balken

        /// <summary>
        /// Horizontale Balken (ein Balken je Variante, Diagramm wächst nach unten —
        /// Konzept Kap. 6.1). Stamm wird farblich hervorgehoben.
        /// </summary>
        public static byte[] BalkenHorizontal(string titel, string einheit, List<Balken> balken)
        {
            int W = 1240;
            int H = 150 + balken.Count * 64;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W);

                float links = 300f, rechts = W - 150f, oben = 80f;
                double max = Math.Max(balken.Max(b => Math.Abs(b.Wert)), 1e-9);

                using (var lf = new Font("Calibri", 18f))
                using (var wf = new Font("Calibri", 17f))
                {
                    for (int i = 0; i < balken.Count; i++)
                    {
                        float y = oben + i * 64f;
                        Balken b = balken[i];
                        float laenge = (float)(Math.Abs(b.Wert) / max * (rechts - links));
                        Color farbe = b.Hervorheben ? C_STAMM : C_WP;

                        // Label links (rechtsbündig).
                        var lgr = g.MeasureString(b.Label, lf);
                        g.DrawString(b.Label, lf, Brushes.Black, links - 12f - lgr.Width, y + 8f);

                        using (var br = new SolidBrush(farbe)) g.FillRectangle(br, links, y, laenge, 40f);
                        g.DrawRectangle(Pens.Gray, links, y, laenge, 40f);
                        g.DrawString(b.Wert.ToString("N0", DE), wf, Brushes.Black, links + laenge + 10f, y + 9f);
                    }
                }
                using (var achse = new Pen(Color.DimGray, 2f))
                    g.DrawLine(achse, links, oben - 8f, links, oben + balken.Count * 64f - 16f);
                return Png(bmp);
            }
        }

        // =================================================================== Ganglinien

        /// <summary>
        /// Ganglinientyp 1: Wärmeerzeugung im Jahresverlauf — gestapelte Erzeugung
        /// (Tagesmittel), Wärmebedarf als Linie (Konzept Kap. 6.2 Nr. 1).
        /// </summary>
        public static byte[] JahresverlaufWaerme(ZeitreihenSatz z)
        {
            var stapel = WaermeErzeugerReihen(z, tagesmittel: true);
            double[] bedarf = TagesMittel(z.Hole(ZeitreihenSatz.WAERMEBEDARF));
            if (stapel.Count == 0 && bedarf == null) return null;
            return StapelDiagramm("Wärmeerzeugung im Jahresverlauf (Tagesmittel)", "kW",
                stapel, bedarf, "Wärmebedarf", MonatsTicks365());
        }

        /// <summary>
        /// Ganglinientyp 2: Jahresdauerlinie Wärme — geordnete Bedarfslinie plus
        /// geordnete Dauerlinien der Erzeuger (Konzept Kap. 6.2 Nr. 2).
        /// </summary>
        public static byte[] DauerlinieWaerme(ZeitreihenSatz z)
        {
            double[] bedarf = z.Hole(ZeitreihenSatz.WAERMEBEDARF);
            if (bedarf == null) return null;

            var reihen = new List<Reihe> { new Reihe("Wärmebedarf", SortiertAbsteigend(bedarf), C_BEDARF) };
            foreach (Reihe r in WaermeErzeugerReihen(z, tagesmittel: false))
                reihen.Add(new Reihe(r.Name, SortiertAbsteigend(r.Werte), r.Farbe));

            return LinienDiagramm("Jahresdauerlinie Wärme", "kW", reihen,
                new[] { 0, 2190, 4380, 6570, 8760 },
                new[] { "0", "2.190", "4.380", "6.570", "8.760 h" });
        }

        /// <summary>
        /// Ganglinientyp 3: Strombilanz im Monatsverlauf — gestapelte Deckung
        /// (PV-Eigenverbrauch, BHKW, Netzbezug), Einspeisung als eigene Reihe,
        /// Strombedarf als Linie (Konzept Kap. 6.2 Nr. 3).
        /// </summary>
        public static byte[] StrombilanzMonate(ZeitreihenSatz z)
        {
            double[] bedarf = z.Hole(ZeitreihenSatz.STROMBEDARF);
            if (bedarf == null) return null;

            var serien = new List<Reihe>();
            if (z.Hat(ZeitreihenSatz.PV_GENUTZT))
                serien.Add(new Reihe("PV-Eigenverbrauch", MonatsSummenMWh(z.Hole(ZeitreihenSatz.PV_GENUTZT)), C_PV));
            if (z.Hat(ZeitreihenSatz.BHKW_STROM))
                serien.Add(new Reihe("BHKW-Strom", MonatsSummenMWh(z.Hole(ZeitreihenSatz.BHKW_STROM)), C_BHKW));
            if (z.Hat(ZeitreihenSatz.NETZBEZUG))
                serien.Add(new Reihe("Netzbezug", MonatsSummenMWh(z.Hole(ZeitreihenSatz.NETZBEZUG)), C_KESSEL));
            if (z.Hat(ZeitreihenSatz.PV_UEBERSCHUSS))
                serien.Add(new Reihe("Einspeisung", MonatsSummenMWh(z.Hole(ZeitreihenSatz.PV_UEBERSCHUSS)), C_NETZ));
            if (serien.Count == 0) return null;

            return MonatsBalken("Strombilanz im Monatsverlauf", "MWh/Monat",
                serien, MonatsSummenMWh(bedarf), "Strombedarf");
        }

        /// <summary>
        /// Ganglinientyp 4: Speicherverlauf — Füllstand über drei charakteristische
        /// Wochen (Winter/Übergang/Sommer; Konzept Kap. 6.2 Nr. 4).
        /// </summary>
        public static byte[] Speicherverlauf(ZeitreihenSatz z)
        {
            var reihen = new List<Reihe>();

            // PAKET E1 (Konzept 6.3, Befund S-1): eine Linie JE WÄRMESPEICHER statt der
            // einen Reihe „Puffer_SOC", die nur den ersten Heizungspuffer zeigte. Die
            // Beschriftung kommt aus dem Zeitreihensatz („Bezeichner (Rolle)"), die
            // Farbfolge wiederholt sich bei mehr als vier Speichern — dieselbe Bauform
            // wie die Speicherserien des NavigatorWaerme.
            for (int i = 0; i < z.Speicherreihen.Count; i++)
            {
                string s = z.Speicherreihen[i];
                if (!z.Hat(s)) continue;
                reihen.Add(new Reihe(z.Beschriftung(s), z.Hole(s), C_SPEICHER[i % C_SPEICHER.Length]));
            }

            if (z.Hat(ZeitreihenSatz.PV_SPEICHER_SOC))
                reihen.Add(new Reihe("Stromspeicher (PV)", z.Hole(ZeitreihenSatz.PV_SPEICHER_SOC), C_PV));
            if (reihen.Count == 0) return null;

            // Wochenfenster: 15.01. (h 336), 15.04. (h 2496), 15.07. (h 4680), je 168 h.
            var fenster = new[] { 336, 2496, 4680 };
            var titelWoche = new[] { "Winterwoche (Jan)", "Übergangswoche (Apr)", "Sommerwoche (Jul)" };

            int W = 1240, H = 520;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, "Speicherverlauf — Füllstand [kWh]", W);

                double max = reihen.Max(r => r.Werte.Max());
                if (max <= 0) max = 1;

                float panelB = (W - 120f) / 3f;
                for (int p = 0; p < 3; p++)
                {
                    var rc = new RectangleF(70f + p * (panelB + 12f), 100f, panelB - 24f, 330f);
                    PanelRahmen(g, rc, titelWoche[p]);
                    foreach (Reihe r in reihen)
                        ZeichneLinie(g, rc, Ausschnitt(r.Werte, fenster[p], 168), 0, max, r.Farbe, 3f);
                    // Y-Beschriftung nur links.
                    if (p == 0)
                        using (var f = new Font("Calibri", 15f))
                        {
                            g.DrawString(max.ToString("N0", DE), f, Brushes.DimGray, rc.X - 62f, rc.Y - 8f);
                            g.DrawString("0", f, Brushes.DimGray, rc.X - 24f, rc.Bottom - 10f);
                        }
                }
                Legende(g, reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(), 70f, H - 56f);
                return Png(bmp);
            }
        }

        /// <summary>
        /// Ganglinientyp 5 (PAKET P2, Konzept 7.4/7.5): SPEICHERTEMPERATUREN — oberste
        /// und unterste Schicht je Senkenspeicher, dazu die Quelltemperatur der
        /// temperaturgekoppelten Erzeuger (Paket B1). <c>null</c>, wenn der Lauf keine
        /// Temperaturreihe trägt (kein Senkenspeicher, oder ein Ergebnis von vor P1).
        ///
        /// <para><b>Dasselbe Bild wie <see cref="Speicherverlauf"/></b> — dieselben drei
        /// charakteristischen Wochen, dieselbe Panelaufteilung, dieselbe Farbfolge je
        /// Speicher. Nur die Achse ist eine andere: °C statt kWh, und sie beginnt beim
        /// kleinsten vorkommenden Wert statt bei 0. Eine bei 0 beginnende Achse drückte
        /// das Temperaturband (Rücklauf … Vorlauf) in den oberen Rand des Bildes.</para>
        ///
        /// <para>Die UNTERE Schicht läuft in derselben Farbe wie ihre obere, nur
        /// halbtransparent: Zwei Temperaturen desselben Behälters gehören zusammen, und
        /// bei drei Speichern wären sechs eigene Farben nicht mehr zu unterscheiden.</para>
        /// </summary>
        public static byte[] Speichertemperaturen(ZeitreihenSatz z)
        {
            var reihen = new List<Reihe>();

            // Je Speicher zwei Reihen — die Reihenfolge kommt aus z.Speicherreihen und ist
            // damit dieselbe stabile Aufnahmereihenfolge wie beim Füllstandsdiagramm.
            for (int i = 0; i < z.Speicherreihen.Count; i++)
            {
                string s = z.Speicherreihen[i];
                Color farbe = C_SPEICHER[i % C_SPEICHER.Length];

                string oben = s + ZeitreihenSatz.SUFFIX_T_OBEN;
                string unten = s + ZeitreihenSatz.SUFFIX_T_UNTEN;

                if (z.Hat(oben)) reihen.Add(new Reihe(z.Beschriftung(oben), z.Hole(oben), farbe));
                if (z.Hat(unten))
                    reihen.Add(new Reihe(z.Beschriftung(unten), z.Hole(unten),
                                         Color.FromArgb(150, farbe)));
            }

            // Quelltemperaturen: eigene Schlüsselfamilie ohne Speicherbezug. SORTIERT,
            // weil die Reihenfolge eines Dictionary nicht zugesichert ist — die Legende
            // darf sich zwischen zwei Berichten nicht umsortieren (dieselbe Begründung
            // wie bei ZeitreihenSatz.Speicherreihen).
            var quellen = new List<string>();
            foreach (KeyValuePair<string, double[]> p in z.Reihen)
                if (p.Key.StartsWith(ZeitreihenSatz.QUELLTEMP_PRAEFIX, StringComparison.Ordinal) &&
                    z.Hat(p.Key))
                    quellen.Add(p.Key);
            quellen.Sort(StringComparer.Ordinal);

            foreach (string q in quellen)
                reihen.Add(new Reihe(z.Beschriftung(q), z.Hole(q), C_NETZ));

            if (reihen.Count == 0) return null;

            double min = reihen.Min(r => r.Werte.Min());
            double max = reihen.Max(r => r.Werte.Max());
            if (max - min < 5) max = min + 5;      // flaches Band nicht auf eine Linie pressen

            // Wochenfenster wie beim Füllstand: 15.01. (h 336), 15.04. (h 2496), 15.07. (h 4680).
            var fenster = new[] { 336, 2496, 4680 };
            var titelWoche = new[] { "Winterwoche (Jan)", "Übergangswoche (Apr)", "Sommerwoche (Jul)" };

            int W = 1240, H = 560;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, "Speichertemperaturen — oberste und unterste Schicht [°C]", W);

                float panelB = (W - 120f) / 3f;
                for (int p = 0; p < 3; p++)
                {
                    var rc = new RectangleF(70f + p * (panelB + 12f), 100f, panelB - 24f, 330f);
                    PanelRahmen(g, rc, titelWoche[p]);
                    foreach (Reihe r in reihen)
                        ZeichneLinie(g, rc, Ausschnitt(r.Werte, fenster[p], 168), min, max, r.Farbe, 3f);

                    if (p == 0)
                        using (var f = new Font("Calibri", 15f))
                        {
                            g.DrawString(max.ToString("N0", DE), f, Brushes.DimGray, rc.X - 62f, rc.Y - 8f);
                            g.DrawString(min.ToString("N0", DE), f, Brushes.DimGray, rc.X - 62f, rc.Bottom - 10f);
                        }
                }

                // Umbruch bei vielen Serien: zwei Reihen je Speicher füllen die Zeile
                // schneller als beim Füllstandsdiagramm.
                Legende(g, reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        70f, H - 96f, W - 70f);
                return Png(bmp);
            }
        }

        // =================================================================== Kernzeichner

        private static byte[] StapelDiagramm(string titel, string einheit, List<Reihe> stapel,
                                             double[] linie, string linienName,
                                             KeyValuePair<int[], string[]> xticks)
        {
            int W = 1240, H = 560;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, titel + "  [" + einheit + "]", W);
                var rc = new RectangleF(90f, 80f, W - 130f, 380f);

                int n = stapel.Count > 0 ? stapel[0].Werte.Length : linie.Length;
                var summe = new double[n];
                foreach (Reihe r in stapel)
                    for (int i = 0; i < n; i++) summe[i] += Math.Max(r.Werte[i], 0);

                double max = summe.Length > 0 ? summe.Max() : 0;
                if (linie != null) max = Math.Max(max, linie.Max());
                max = Nice(max);

                AchsenRaster(g, rc, max, xticks.Key, xticks.Value, n);

                // Stapel von unten nach oben zeichnen (kumulierte Flächen).
                var unten = new double[n];
                foreach (Reihe r in stapel)
                {
                    var oben = new double[n];
                    for (int i = 0; i < n; i++) oben[i] = unten[i] + Math.Max(r.Werte[i], 0);
                    ZeichneFlaeche(g, rc, unten, oben, max, r.Farbe);
                    unten = oben;
                }
                if (linie != null) ZeichneLinie(g, rc, linie, 0, max, C_BEDARF, 3f);

                var leg = stapel.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
                if (linie != null) leg.Add(new Segment(linienName, 0, C_BEDARF));
                Legende(g, leg, 90f, H - 64f);
                return Png(bmp);
            }
        }

        private static byte[] LinienDiagramm(string titel, string einheit, List<Reihe> reihen,
                                             int[] xpos, string[] xlab)
        {
            int W = 1240, H = 560;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, titel + "  [" + einheit + "]", W);
                var rc = new RectangleF(90f, 80f, W - 130f, 380f);

                int n = reihen[0].Werte.Length;
                double max = Nice(reihen.Max(r => r.Werte.Max()));
                AchsenRaster(g, rc, max, xpos, xlab, n);

                foreach (Reihe r in reihen)
                    ZeichneLinie(g, rc, r.Werte, 0, max, r.Farbe, r.Farbe == C_BEDARF ? 3.5f : 2.5f);

                Legende(g, reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(), 90f, H - 64f);
                return Png(bmp);
            }
        }

        private static byte[] MonatsBalken(string titel, string einheit, List<Reihe> serien,
                                           double[] linie, string linienName)
        {
            int W = 1240, H = 560;
            string[] monate = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, titel + "  [" + einheit + "]", W);
                var rc = new RectangleF(90f, 80f, W - 130f, 380f);

                // Einspeisung wird nicht gestapelt, sondern als schmaler Nebenbalken gezeigt.
                Reihe einspeisung = serien.FirstOrDefault(s => s.Name == "Einspeisung");
                var stapel = serien.Where(s => s != einspeisung).ToList();

                var summe = new double[12];
                foreach (Reihe r in stapel) for (int m = 0; m < 12; m++) summe[m] += r.Werte[m];
                double max = summe.Max();
                if (linie != null) max = Math.Max(max, linie.Max());
                if (einspeisung != null) max = Math.Max(max, einspeisung.Werte.Max());
                max = Nice(max);

                // Achsen + Monatslabels.
                AchsenRaster(g, rc, max, null, null, 12);
                using (var f = new Font("Calibri", 15f))
                    for (int m = 0; m < 12; m++)
                    {
                        float x = rc.X + (m + 0.5f) * rc.Width / 12f;
                        var gr = g.MeasureString(monate[m], f);
                        g.DrawString(monate[m], f, Brushes.DimGray, x - gr.Width / 2f, rc.Bottom + 8f);
                    }

                float slot = rc.Width / 12f;
                float bBreit = slot * 0.5f, bSchmal = slot * 0.18f;
                for (int m = 0; m < 12; m++)
                {
                    float x0 = rc.X + m * slot + slot * 0.12f;
                    float unten = rc.Bottom;
                    foreach (Reihe r in stapel)
                    {
                        float hoehe = (float)(r.Werte[m] / max * rc.Height);
                        using (var br = new SolidBrush(r.Farbe))
                            g.FillRectangle(br, x0, unten - hoehe, bBreit, hoehe);
                        unten -= hoehe;
                    }
                    if (einspeisung != null)
                    {
                        float hoehe = (float)(einspeisung.Werte[m] / max * rc.Height);
                        using (var br = new SolidBrush(einspeisung.Farbe))
                            g.FillRectangle(br, x0 + bBreit + slot * 0.06f, rc.Bottom - hoehe, bSchmal, hoehe);
                    }
                }

                if (linie != null)
                {
                    var punkte = new PointF[12];
                    for (int m = 0; m < 12; m++)
                        punkte[m] = new PointF(rc.X + (m + 0.5f) * slot,
                            rc.Bottom - (float)(linie[m] / max * rc.Height));
                    using (var stift = new Pen(C_BEDARF, 3f)) g.DrawLines(stift, punkte);
                }

                var leg = serien.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
                if (linie != null) leg.Add(new Segment(linienName, 0, C_BEDARF));
                Legende(g, leg, 90f, H - 56f);
                return Png(bmp);
            }
        }

        // ============================================== Kapitalwert-Verlauf (Phase 11)

        /// <summary>Serienfarben der Verlaufslinien (Variante 1…n; Stamm = C_STAMM).</summary>
        public static readonly Color[] C_SERIEN =
        {
            Color.FromArgb(0xED, 0x7D, 0x31),   // Orange
            Color.FromArgb(0x70, 0xAD, 0x47),   // Grün
            Color.FromArgb(0x41, 0x72, 0xC4),   // Blau
            Color.FromArgb(0x9E, 0x48, 0x0E),   // Braun
            Color.FromArgb(0x7A, 0x5C, 0xA8),   // Violett
            Color.FromArgb(0x2E, 0x8B, 0x8B),   // Petrol
            Color.FromArgb(0xC0, 0x50, 0x4D),   // Rot
            Color.FromArgb(0xBF, 0x8F, 0x00)    // Ocker
        };

        /// <summary>Verlaufsserien → Diagramm-Reihen (Stamm dunkel/dick, Varianten
        /// aus der Serien-Palette; Reihen ohne Werte werden übersprungen).</summary>
        public static List<Reihe> VerlaufsReihen(List<VerlaufSerie> serien, bool mitStamm)
        {
            var reihen = new List<Reihe>();
            int i = 0;
            foreach (VerlaufSerie s in serien)
            {
                if (s.Kumuliert == null) continue;
                if (s.IstStamm)
                {
                    if (mitStamm) reihen.Add(new Reihe(s.Anzeige, s.Kumuliert, C_STAMM));
                    continue;
                }
                reihen.Add(new Reihe(s.Anzeige, s.Kumuliert, C_SERIEN[i++ % C_SERIEN.Length]));
            }
            return reihen;
        }

        /// <summary>
        /// Liniendiagramm „Kapitalwert über den Nutzungszeitraum" (Phase 11):
        /// kumulierte diskontierte Zahlungsströme je Jahr 0…N, y-Achse mit
        /// negativem Bereich und hervorgehobener Nulllinie (Schnittpunkt der
        /// Differenzlinie = dynamische Amortisation). X-Achse in Jahren.
        /// </summary>
        public static byte[] KapitalwertVerlauf(string titel, List<Reihe> reihen, string fussnote)
        {
            int W = 1240, H = 620;
            using (var bmp = new Bitmap(W, H))
            using (var g = Start(bmp))
            {
                Titel(g, titel + "  [€]", W);
                var rc = new RectangleF(110f, 80f, W - 150f, 400f);

                var gueltig = reihen.Where(r => r.Werte != null && r.Werte.Length >= 2 &&
                                           r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w)))
                                    .ToList();
                if (gueltig.Count == 0)
                {
                    using (var f = new Font("Calibri", 18f))
                        g.DrawString("Keine berechenbaren Reihen.", f, Brushes.DimGray, rc.X, rc.Y + 20f);
                    return Png(bmp);
                }
                int n = gueltig.Max(r => r.Werte.Length);          // Stützstellen (Jahre + 1)

                // Vorzeichenfähige Skala mit „schönen" Stufen (5 Rasterlinien).
                double min = Math.Min(0, gueltig.Min(r => r.Werte.Min()));
                double max = Math.Max(0, gueltig.Max(r => r.Werte.Max()));
                if (max - min < 1e-9) { max = min + 1; }
                double roh = (max - min) / 5.0;
                double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
                double schritt = zehner;
                foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                    if (zehner * f >= roh) { schritt = zehner * f; break; }
                min = Math.Floor(min / schritt) * schritt;
                max = Math.Ceiling(max / schritt) * schritt;

                // Raster + y-Beschriftung.
                using (var raster = new Pen(Color.Gainsboro, 1f))
                using (var f = new Font("Calibri", 15f))
                    for (double wert = min; wert <= max + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        g.DrawLine(raster, rc.X, y, rc.Right, y);
                        string lab = wert.ToString("N0", DE);
                        var gr = g.MeasureString(lab, f);
                        g.DrawString(lab, f, Brushes.DimGray, rc.X - gr.Width - 6f, y - gr.Height / 2f);
                    }

                // X-Achse: Jahre 0…N, Beschriftung in sinnvollen Schritten.
                int jahre = n - 1;
                int xschritt = jahre <= 12 ? 1 : jahre <= 25 ? 2 : jahre <= 50 ? 5 : 10;
                using (var raster = new Pen(Color.Gainsboro, 1f))
                using (var f = new Font("Calibri", 15f))
                    for (int t = 0; t <= jahre; t += xschritt)
                    {
                        float x = rc.X + (float)t / Math.Max(jahre, 1) * rc.Width;
                        g.DrawLine(raster, x, rc.Y, x, rc.Bottom);
                        string lab = t.ToString(DE);
                        var gr = g.MeasureString(lab, f);
                        g.DrawString(lab, f, Brushes.DimGray, x - gr.Width / 2f, rc.Bottom + 8f);
                    }
                using (var f = new Font("Calibri", 15f))
                    g.DrawString(BerichtTexte.T("Jahr"), f, Brushes.DimGray, rc.Right + 10f, rc.Bottom + 8f);

                // Achsen + hervorgehobene Nulllinie.
                using (var achse = new Pen(Color.DimGray, 2f))
                {
                    g.DrawLine(achse, rc.X, rc.Y, rc.X, rc.Bottom);
                    g.DrawLine(achse, rc.X, rc.Bottom, rc.Right, rc.Bottom);
                }
                float y0 = (float)(rc.Bottom - (0 - min) / (max - min) * rc.Height);
                using (var stift = new Pen(Color.DimGray, 2f) { DashStyle = DashStyle.Dash })
                    g.DrawLine(stift, rc.X, y0, rc.Right, y0);

                // Linien (kürzere Reihen enden früher; x bezieht sich auf N).
                foreach (Reihe r in gueltig)
                {
                    var punkte = new PointF[r.Werte.Length];
                    for (int t = 0; t < r.Werte.Length; t++)
                    {
                        float x = rc.X + (float)t / Math.Max(jahre, 1) * rc.Width;
                        float y = (float)(rc.Bottom - (r.Werte[t] - min) / (max - min) * rc.Height);
                        punkte[t] = new PointF(x, Math.Max(rc.Y, Math.Min(rc.Bottom, y)));
                    }
                    using (var stift = new Pen(r.Farbe, r.Farbe == C_STAMM ? 3.5f : 2.5f)
                                       { LineJoin = LineJoin.Round })
                        g.DrawLines(stift, punkte);
                }

                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        110f, H - 104f, W - 30f);   // Umbruch: 2 Zeilen Platz (Review 11)
                if (!string.IsNullOrEmpty(fussnote))
                    using (var f = new Font("Calibri", 14f, FontStyle.Italic))
                        g.DrawString(fussnote, f, Brushes.DimGray, 110f, H - 28f);
                return Png(bmp);
            }
        }

        // =================================================================== Helfer

        private static Graphics Start(Bitmap bmp)
        {
            var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);
            return g;
        }

        private static void Titel(Graphics g, string text, int breite)
        {
            using (var f = new Font("Calibri", 22f, FontStyle.Bold))
                g.DrawString(text, f, new SolidBrush(C_STAMM), 24f, 16f);
        }

        private static void PanelRahmen(Graphics g, RectangleF rc, string titel)
        {
            g.DrawRectangle(Pens.Silver, rc.X, rc.Y, rc.Width, rc.Height);
            using (var f = new Font("Calibri", 16f, FontStyle.Bold))
                g.DrawString(titel, f, Brushes.DimGray, rc.X, rc.Y - 28f);
        }

        private static void AchsenRaster(Graphics g, RectangleF rc, double max,
                                         int[] xpos, string[] xlab, int n)
        {
            using (var raster = new Pen(Color.Gainsboro, 1f))
            using (var f = new Font("Calibri", 15f))
            {
                for (int s = 0; s <= 4; s++)
                {
                    float y = rc.Bottom - s * rc.Height / 4f;
                    g.DrawLine(raster, rc.X, y, rc.Right, y);
                    string lab = (max * s / 4.0).ToString("N0", DE);
                    var gr = g.MeasureString(lab, f);
                    g.DrawString(lab, f, Brushes.DimGray, rc.X - gr.Width - 6f, y - gr.Height / 2f);
                }
                if (xpos != null)
                    for (int i = 0; i < xpos.Length; i++)
                    {
                        float x = rc.X + (float)xpos[i] / Math.Max(n - 1, 1) * rc.Width;
                        g.DrawLine(raster, x, rc.Y, x, rc.Bottom);
                        var gr = g.MeasureString(xlab[i], f);
                        g.DrawString(xlab[i], f, Brushes.DimGray, x - gr.Width / 2f, rc.Bottom + 8f);
                    }
            }
            using (var achse = new Pen(Color.DimGray, 2f))
            {
                g.DrawLine(achse, rc.X, rc.Y, rc.X, rc.Bottom);
                g.DrawLine(achse, rc.X, rc.Bottom, rc.Right, rc.Bottom);
            }
        }

        private static void ZeichneLinie(Graphics g, RectangleF rc, double[] werte,
                                         double min, double max, Color farbe, float staerke)
        {
            if (werte == null || werte.Length < 2) return;
            int schritt = Math.Max(1, werte.Length / (int)rc.Width);
            var punkte = new List<PointF>();
            for (int i = 0; i < werte.Length; i += schritt)
            {
                float x = rc.X + (float)i / (werte.Length - 1) * rc.Width;
                float y = rc.Bottom - (float)((werte[i] - min) / (max - min) * rc.Height);
                punkte.Add(new PointF(x, Math.Max(rc.Y, Math.Min(rc.Bottom, y))));
            }
            if (punkte.Count >= 2)
                using (var stift = new Pen(farbe, staerke) { LineJoin = LineJoin.Round })
                    g.DrawLines(stift, punkte.ToArray());
        }

        private static void ZeichneFlaeche(Graphics g, RectangleF rc, double[] unten,
                                           double[] oben, double max, Color farbe)
        {
            int n = oben.Length;
            int schritt = Math.Max(1, n / (int)rc.Width);
            var pfad = new List<PointF>();
            for (int i = 0; i < n; i += schritt)
                pfad.Add(Punkt(rc, i, n, oben[i], max));
            for (int i = ((n - 1) / schritt) * schritt; i >= 0; i -= schritt)
                pfad.Add(Punkt(rc, i, n, unten[i], max));
            if (pfad.Count >= 3)
                using (var br = new SolidBrush(Color.FromArgb(210, farbe)))
                    g.FillPolygon(br, pfad.ToArray());
        }

        private static PointF Punkt(RectangleF rc, int i, int n, double wert, double max)
        {
            float x = rc.X + (float)i / (n - 1) * rc.Width;
            float y = rc.Bottom - (float)(wert / max * rc.Height);
            return new PointF(x, Math.Max(rc.Y, Math.Min(rc.Bottom, y)));
        }

        private static void Legende(Graphics g, List<Segment> eintraege, float x, float y,
                                    float umbruchBei = 0)
        {
            float startX = x;
            using (var f = new Font("Calibri", 16f))
                foreach (Segment s in eintraege)
                {
                    float breite = 40f + g.MeasureString(s.Label, f).Width + 24f;
                    if (umbruchBei > 0 && x > startX && x + breite > umbruchBei)
                    { x = startX; y += 30f; }   // Umbruch bei vielen Serien (Review 11)
                    using (var b = new SolidBrush(s.Farbe)) g.FillRectangle(b, x, y, 22f, 22f);
                    g.DrawRectangle(Pens.Gray, x, y, 22f, 22f);
                    g.DrawString(s.Label, f, Brushes.Black, x + 28f, y + 1f);
                    x += breite;
                }
        }

        private static byte[] Png(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        // Erzeugerreihen Wärme in fester Stapelreihenfolge (Solar unten … Kessel oben).
        private static List<Reihe> WaermeErzeugerReihen(ZeitreihenSatz z, bool tagesmittel)
        {
            var l = new List<Reihe>();
            Action<string, string, Color> add = (key, name, farbe) =>
            {
                if (!z.Hat(key)) return;
                double[] w = z.Hole(key);
                l.Add(new Reihe(name, tagesmittel ? TagesMittel(w) : w, farbe));
            };
            add(ZeitreihenSatz.SOLAR_WAERME, "Solarthermie", C_SOLAR);
            add(ZeitreihenSatz.WP_WAERME, "Wärmepumpe", C_WP);
            add(ZeitreihenSatz.HEIZSTAB, "Heizstab", C_NETZ);
            add(ZeitreihenSatz.BHKW_WAERME, "BHKW", C_BHKW);
            add(ZeitreihenSatz.KESSEL_WAERME, "Spitzenkessel", C_KESSEL);
            return l;
        }

        public static double[] TagesMittel(double[] stunden)
        {
            if (stunden == null) return null;
            int tage = stunden.Length / 24;
            var r = new double[tage];
            for (int t = 0; t < tage; t++)
            {
                double s = 0;
                for (int h = 0; h < 24; h++) s += stunden[t * 24 + h];
                r[t] = s / 24.0;
            }
            return r;
        }

        public static double[] MonatsSummenMWh(double[] stunden)
        {
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            var r = new double[12];
            if (stunden == null) return r;
            int h0 = 0;
            for (int m = 0; m < 12; m++)
            {
                int hn = tage[m] * 24;
                double s = 0;
                for (int h = h0; h < h0 + hn && h < stunden.Length; h++) s += stunden[h];
                r[m] = s / 1000.0;
                h0 += hn;
            }
            return r;
        }

        private static double[] SortiertAbsteigend(double[] q)
        {
            var r = (double[])q.Clone();
            Array.Sort(r);
            Array.Reverse(r);
            return r;
        }

        private static double[] Ausschnitt(double[] q, int start, int laenge)
        {
            var r = new double[laenge];
            for (int i = 0; i < laenge && start + i < q.Length; i++) r[i] = q[start + i];
            return r;
        }

        private static KeyValuePair<int[], string[]> MonatsTicks365()
        {
            return new KeyValuePair<int[], string[]>(
                new[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 },
                new[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" });
        }

        // "Schöne" Achsen-Obergrenze (1/2/2,5/5 × 10^k).
        private static double Nice(double max)
        {
            if (max <= 0) return 1;
            double exp = Math.Pow(10, Math.Floor(Math.Log10(max)));
            double f = max / exp;
            double nf = f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10;
            return nf * exp;
        }
    }
}
