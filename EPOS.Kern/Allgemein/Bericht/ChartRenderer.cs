using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Off-Screen-Diagramm-Rendering für den Bericht (Konzept Kap. 6) auf Basis
    /// SkiaSharp — bewusst ohne UI-Handle und ohne Fremd-API, damit das Rendering
    /// im Hintergrund-Thread deterministisch läuft (gleiches Muster wie die
    /// Kuchendiagramme des Bestandsberichts). Alle Methoden liefern PNG-Bytes;
    /// gerendert wird in doppelter Zielauflösung (Einbettung skaliert herunter).
    ///
    /// <para><b>Paket iU7 — die Portierung von GDI+ auf SkiaSharp.</b> Bis zum
    /// 03.09.2026 zeichnete diese Datei mit GDI+ und war damit die letzte
    /// Windows-Bindung im Berichtsweg. Übersetzt wurde 1:1 und ohne jede Änderung an
    /// Bildmaßen, Farben, Texten oder Achsenlogik: <c>Graphics</c> → <see cref="SKCanvas"/>,
    /// <c>Pen</c> → <see cref="SKPaint"/> im Stroke-Stil, <c>Brush</c> → <see cref="SKPaint"/>
    /// im Fill-Stil, <c>MeasureString</c> → <see cref="SKFont.MeasureText(string)"/>,
    /// <c>FillPie</c> → <see cref="SKPath.ArcTo(SKRect, float, float, bool)"/>,
    /// <c>Bitmap.Save(Png)</c> → <see cref="SKSurface"/> + <see cref="SKImage.Encode(SKEncodedImageFormat, int)"/>.
    /// Die öffentliche Fläche ist unverändert; nur die Farbfelder tragen jetzt
    /// <see cref="SKColor"/> statt des GDI+-Farbtyps <c>Color</c>. Die Datei hat damit
    /// KEINE Windows-Bindung mehr und kann in den plattformfreien Kern ziehen.</para>
    ///
    /// <para>Der eingefrorene GDI+-Stand (<c>ChartRendererGdi</c>) und der Modus
    /// <c>bildvergleich</c> der Referenzlauf-Suite sind mit Entscheid iF23 am 03.09.2026
    /// gelöscht; dieser Renderer ist die einzige Fassung. Wächter sind die Renderer-Tests
    /// im Kern und <c>Proben/ChartProben</c> (Maße, Farben, Determinismus).</para>
    ///
    /// Feste Farbzuordnung je Erzeuger über alle Diagramme (Konzept Kap. 6):
    /// WP blau, BHKW orange, Kessel grau, Solar gelb, PV grün, Netz/Rest neutral.
    /// </summary>
    public static class ChartRenderer
    {
        // Palette (identisch zum Bestandsbericht).
        public static readonly SKColor C_WP = new SKColor(0x41, 0x72, 0xC4);
        public static readonly SKColor C_BHKW = new SKColor(0xED, 0x7D, 0x31);
        public static readonly SKColor C_KESSEL = new SKColor(0x80, 0x80, 0x80);
        public static readonly SKColor C_SOLAR = new SKColor(0xFF, 0xC0, 0x00);
        public static readonly SKColor C_PV = new SKColor(0x70, 0xAD, 0x47);
        public static readonly SKColor C_NETZ = new SKColor(0x9E, 0x48, 0x0E);
        public static readonly SKColor C_REST = new SKColor(0xBF, 0xBF, 0xBF);
        public static readonly SKColor C_BEDARF = new SKColor(0x33, 0x33, 0x33);
        public static readonly SKColor C_STAMM = new SKColor(0x1F, 0x4E, 0x79);

        /// <summary>
        /// PAKET E1: Farbfolge der Wärmespeicher-Füllstandslinien (Konzept 6.3) — sie
        /// wiederholt sich, wenn ein Projekt mehr Speicher führt als Farben da sind.
        /// Dieselbe Reihenfolge wie <c>NavigatorWaerme.SPEICHER_FARBEN</c>, damit
        /// Bildschirm und Bericht denselben Speicher gleich einfärben.
        /// </summary>
        public static readonly SKColor[] C_SPEICHER =
        {
            SKColors.MediumVioletRed, SKColors.DarkViolet, SKColors.Teal,
            SKColors.SaddleBrown, SKColors.DarkSlateGray, SKColors.Crimson
        };

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        public class Segment
        {
            public string Label; public double Wert; public SKColor Farbe;
            public Segment(string l, double w, SKColor f) { Label = l; Wert = w; Farbe = f; }
        }

        public class Balken
        {
            public string Label; public double Wert; public bool Hervorheben;
            public Balken(string l, double w, bool hervor) { Label = l; Wert = w; Hervorheben = hervor; }
        }

        public class Reihe
        {
            public string Name; public double[] Werte; public SKColor Farbe;
            public Reihe(string n, double[] w, SKColor f) { Name = n; Werte = w; Farbe = f; }
        }

        // =================================================================== Kuchen

        /// <summary>Kuchendiagramm (Deckungsanteile) — Portierung aus dem Bestandsbericht.</summary>
        public static byte[] Kuchen(string titel, List<Segment> segmente)
        {
            int W = 960, H = 600;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel, W);

                double total = segmente.Sum(s => Math.Max(s.Wert, 0));
                if (total <= 0) total = 1;

                var rect = SKRect.Create(40f, 90f, 440f, 440f);
                float start = -90f;
                foreach (Segment s in segmente)
                {
                    float sweep = (float)(Math.Max(s.Wert, 0) / total * 360.0);
                    using (var b = Fuellung(s.Farbe))
                        Kreissegment(g, rect, start, sweep, b);
                    start += sweep;
                }
                using (var stift = Strich(SKColors.White, 3f)) g.DrawOval(rect, stift);

                float lx = 540f, ly = 110f;
                using (var lf = Schrift(19f))
                using (var rahmen = Strich(SKColors.Gray, 1f))
                    foreach (Segment s in segmente)
                    {
                        using (var b = Fuellung(s.Farbe)) g.DrawRect(lx, ly, 28f, 28f, b);
                        g.DrawRect(lx, ly, 28f, 28f, rahmen);
                        Text(g, s.Label + "   " + (s.Wert / total * 100.0).ToString("N1", DE) + " %",
                             lf, SKColors.Black, lx + 40f, ly + 1f);
                        ly += 48f;
                    }
                return Png(flaeche);
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
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W);

                float links = 300f, rechts = W - 150f, oben = 80f;
                double max = Math.Max(balken.Max(b => Math.Abs(b.Wert)), 1e-9);

                using (var lf = Schrift(18f))
                using (var wf = Schrift(17f))
                using (var rahmen = Strich(SKColors.Gray, 1f))
                {
                    for (int i = 0; i < balken.Count; i++)
                    {
                        float y = oben + i * 64f;
                        Balken b = balken[i];
                        float laenge = (float)(Math.Abs(b.Wert) / max * (rechts - links));
                        SKColor farbe = b.Hervorheben ? C_STAMM : C_WP;

                        // Label links (rechtsbündig).
                        float lbreite = lf.MeasureText(b.Label ?? "");
                        Text(g, b.Label, lf, SKColors.Black, links - 12f - lbreite, y + 8f);

                        using (var br = Fuellung(farbe)) g.DrawRect(links, y, laenge, 40f, br);
                        g.DrawRect(links, y, laenge, 40f, rahmen);
                        Text(g, b.Wert.ToString("N0", DE), wf, SKColors.Black, links + laenge + 10f, y + 9f);
                    }
                }
                using (var achse = Strich(SKColors.DimGray, 2f))
                    g.DrawLine(links, oben - 8f, links, oben + balken.Count * 64f - 16f, achse);
                return Png(flaeche);
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
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, "Speicherverlauf — Füllstand [kWh]", W);

                double max = reihen.Max(r => r.Werte.Max());
                if (max <= 0) max = 1;

                float panelB = (W - 120f) / 3f;
                for (int p = 0; p < 3; p++)
                {
                    var rc = SKRect.Create(70f + p * (panelB + 12f), 100f, panelB - 24f, 330f);
                    PanelRahmen(g, rc, titelWoche[p]);
                    foreach (Reihe r in reihen)
                        ZeichneLinie(g, rc, Ausschnitt(r.Werte, fenster[p], 168), 0, max, r.Farbe, 3f);
                    // Y-Beschriftung nur links.
                    if (p == 0)
                        using (var f = Schrift(15f))
                        {
                            Text(g, max.ToString("N0", DE), f, SKColors.DimGray, rc.Left - 62f, rc.Top - 8f);
                            Text(g, "0", f, SKColors.DimGray, rc.Left - 24f, rc.Bottom - 10f);
                        }
                }
                Legende(g, reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(), 70f, H - 56f);
                return Png(flaeche);
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
                SKColor farbe = C_SPEICHER[i % C_SPEICHER.Length];

                string oben = s + ZeitreihenSatz.SUFFIX_T_OBEN;
                string unten = s + ZeitreihenSatz.SUFFIX_T_UNTEN;

                if (z.Hat(oben)) reihen.Add(new Reihe(z.Beschriftung(oben), z.Hole(oben), farbe));
                if (z.Hat(unten))
                    reihen.Add(new Reihe(z.Beschriftung(unten), z.Hole(unten),
                                         farbe.WithAlpha(150)));
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
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, "Speichertemperaturen — oberste und unterste Schicht [°C]", W);

                float panelB = (W - 120f) / 3f;
                for (int p = 0; p < 3; p++)
                {
                    var rc = SKRect.Create(70f + p * (panelB + 12f), 100f, panelB - 24f, 330f);
                    PanelRahmen(g, rc, titelWoche[p]);
                    foreach (Reihe r in reihen)
                        ZeichneLinie(g, rc, Ausschnitt(r.Werte, fenster[p], 168), min, max, r.Farbe, 3f);

                    if (p == 0)
                        using (var f = Schrift(15f))
                        {
                            Text(g, max.ToString("N0", DE), f, SKColors.DimGray, rc.Left - 62f, rc.Top - 8f);
                            Text(g, min.ToString("N0", DE), f, SKColors.DimGray, rc.Left - 62f, rc.Bottom - 10f);
                        }
                }

                // Umbruch bei vielen Serien: zwei Reihen je Speicher füllen die Zeile
                // schneller als beim Füllstandsdiagramm.
                Legende(g, reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        70f, H - 96f, W - 70f);
                return Png(flaeche);
            }
        }

        // =================================================================== Kernzeichner

        private static byte[] StapelDiagramm(string titel, string einheit, List<Reihe> stapel,
                                             double[] linie, string linienName,
                                             KeyValuePair<int[], string[]> xticks)
        {
            int W = 1240, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + "  [" + einheit + "]", W);
                var rc = SKRect.Create(90f, 80f, W - 130f, 380f);

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
                return Png(flaeche);
            }
        }

        private static byte[] LinienDiagramm(string titel, string einheit, List<Reihe> reihen,
                                             int[] xpos, string[] xlab)
        {
            int W = 1240, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + "  [" + einheit + "]", W);
                var rc = SKRect.Create(90f, 80f, W - 130f, 380f);

                int n = reihen[0].Werte.Length;
                double max = Nice(reihen.Max(r => r.Werte.Max()));
                AchsenRaster(g, rc, max, xpos, xlab, n);

                foreach (Reihe r in reihen)
                    ZeichneLinie(g, rc, r.Werte, 0, max, r.Farbe, r.Farbe == C_BEDARF ? 3.5f : 2.5f);

                Legende(g, reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(), 90f, H - 64f);
                return Png(flaeche);
            }
        }

        private static byte[] MonatsBalken(string titel, string einheit, List<Reihe> serien,
                                           double[] linie, string linienName)
        {
            int W = 1240, H = 560;
            string[] monate = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + "  [" + einheit + "]", W);
                var rc = SKRect.Create(90f, 80f, W - 130f, 380f);

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
                using (var f = Schrift(15f))
                    for (int m = 0; m < 12; m++)
                    {
                        float x = rc.Left + (m + 0.5f) * rc.Width / 12f;
                        float breite = f.MeasureText(monate[m]);
                        Text(g, monate[m], f, SKColors.DimGray, x - breite / 2f, rc.Bottom + 8f);
                    }

                float slot = rc.Width / 12f;
                float bBreit = slot * 0.5f, bSchmal = slot * 0.18f;
                for (int m = 0; m < 12; m++)
                {
                    float x0 = rc.Left + m * slot + slot * 0.12f;
                    float unten = rc.Bottom;
                    foreach (Reihe r in stapel)
                    {
                        float hoehe = (float)(r.Werte[m] / max * rc.Height);
                        using (var br = Fuellung(r.Farbe))
                            g.DrawRect(x0, unten - hoehe, bBreit, hoehe, br);
                        unten -= hoehe;
                    }
                    if (einspeisung != null)
                    {
                        float hoehe = (float)(einspeisung.Werte[m] / max * rc.Height);
                        using (var br = Fuellung(einspeisung.Farbe))
                            g.DrawRect(x0 + bBreit + slot * 0.06f, rc.Bottom - hoehe, bSchmal, hoehe, br);
                    }
                }

                if (linie != null)
                {
                    var punkte = new SKPoint[12];
                    for (int m = 0; m < 12; m++)
                        punkte[m] = new SKPoint(rc.Left + (m + 0.5f) * slot,
                            rc.Bottom - (float)(linie[m] / max * rc.Height));
                    using (var stift = Strich(C_BEDARF, 3f)) Linienzug(g, punkte, stift);
                }

                var leg = serien.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
                if (linie != null) leg.Add(new Segment(linienName, 0, C_BEDARF));
                Legende(g, leg, 90f, H - 56f);
                return Png(flaeche);
            }
        }

        // ============================================== Kapitalwert-Verlauf (Phase 11)

        /// <summary>Serienfarben der Verlaufslinien (Variante 1…n; Stamm = C_STAMM).</summary>
        public static readonly SKColor[] C_SERIEN =
        {
            new SKColor(0xED, 0x7D, 0x31),   // Orange
            new SKColor(0x70, 0xAD, 0x47),   // Grün
            new SKColor(0x41, 0x72, 0xC4),   // Blau
            new SKColor(0x9E, 0x48, 0x0E),   // Braun
            new SKColor(0x7A, 0x5C, 0xA8),   // Violett
            new SKColor(0x2E, 0x8B, 0x8B),   // Petrol
            new SKColor(0xC0, 0x50, 0x4D),   // Rot
            new SKColor(0xBF, 0x8F, 0x00)    // Ocker
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
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + "  [€]", W);
                var rc = SKRect.Create(110f, 80f, W - 150f, 400f);

                var gueltig = reihen.Where(r => r.Werte != null && r.Werte.Length >= 2 &&
                                           r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w)))
                                    .ToList();
                if (gueltig.Count == 0)
                {
                    using (var f = Schrift(18f))
                        Text(g, "Keine berechenbaren Reihen.", f, SKColors.DimGray, rc.Left, rc.Top + 20f);
                    return Png(flaeche);
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
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (double wert = min; wert <= max + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        g.DrawLine(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("N0", DE);
                        float breite = f.MeasureText(lab);
                        Text(g, lab, f, SKColors.DimGray, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }

                // X-Achse: Jahre 0…N, Beschriftung in sinnvollen Schritten.
                int jahre = n - 1;
                int xschritt = jahre <= 12 ? 1 : jahre <= 25 ? 2 : jahre <= 50 ? 5 : 10;
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int t = 0; t <= jahre; t += xschritt)
                    {
                        float x = rc.Left + (float)t / Math.Max(jahre, 1) * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = t.ToString(DE);
                        float breite = f.MeasureText(lab);
                        Text(g, lab, f, SKColors.DimGray, x - breite / 2f, rc.Bottom + 8f);
                    }
                using (var f = Schrift(15f))
                    Text(g, BerichtTexte.T("Jahr"), f, SKColors.DimGray, rc.Right + 10f, rc.Bottom + 8f);

                // Achsen + hervorgehobene Nulllinie.
                using (var achse = Strich(SKColors.DimGray, 2f))
                {
                    g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                    g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
                }
                float y0 = (float)(rc.Bottom - (0 - min) / (max - min) * rc.Height);
                using (var strichel = SKPathEffect.CreateDash(new[] { 3f * 2f, 1f * 2f }, 0f))
                using (var stift = Strich(SKColors.DimGray, 2f))
                {
                    stift.PathEffect = strichel;
                    g.DrawLine(rc.Left, y0, rc.Right, y0, stift);
                }

                // Linien (kürzere Reihen enden früher; x bezieht sich auf N).
                foreach (Reihe r in gueltig)
                {
                    var punkte = new SKPoint[r.Werte.Length];
                    for (int t = 0; t < r.Werte.Length; t++)
                    {
                        float x = rc.Left + (float)t / Math.Max(jahre, 1) * rc.Width;
                        float y = (float)(rc.Bottom - (r.Werte[t] - min) / (max - min) * rc.Height);
                        punkte[t] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                    }
                    using (var stift = Strich(r.Farbe, r.Farbe == C_STAMM ? 3.5f : 2.5f))
                    {
                        stift.StrokeJoin = SKStrokeJoin.Round;
                        Linienzug(g, punkte, stift);
                    }
                }

                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        110f, H - 104f, W - 30f);   // Umbruch: 2 Zeilen Platz (Review 11)
                if (!string.IsNullOrEmpty(fussnote))
                    using (var f = Schrift(14f, kursiv: true))
                        Text(g, fussnote, f, SKColors.DimGray, 110f, H - 28f);
                return Png(flaeche);
            }
        }

        // =================================================================== Kostenprofil

        /// <summary>
        /// Die Linienfarbe des Kostenprofils — halbtransparentes Dunkelgrün.
        /// Wortgleich aus <c>Form_Kostenprofil.ChartKonfigurieren</c>
        /// (<c>Color.FromArgb(180, Color.DarkGreen)</c>); SkiaSharp nimmt die
        /// Deckung als vierten Wert.
        /// </summary>
        public static readonly SKColor C_PROFIL = new SKColor(0x00, 0x64, 0x00, 180);

        /// <summary>
        /// Liniendiagramm „Kostenprofil im Jahresverlauf" (Paket iU9-W3.4):
        /// das aus zwölf Monatsniveaus und 168 Wochenwerten konstruierte
        /// Jahresprofil (8 760 Stunden) über einer Monatsachse 0…12.
        ///
        /// <para><b>Vorbild.</b> Das <c>Chart</c> der Maske
        /// <c>Form_Kostenprofil</c> (648 × 390): ein Diagrammbereich „Jahr",
        /// x-Achse 0…12 im Abstand 1, beide Raster gepunktet, eine Linie
        /// „KOSTENPROFIL" in halbtransparentem Dunkelgrün, Stärke 2. Die Punkte
        /// entstanden dort aus <c>x = i * 12 / 8760</c> — dieselbe Abbildung
        /// steht hier. Das Bildmaß ist die doppelte Zielauflösung des
        /// Vorläufers (1296 × 780), wie bei allen Bildern dieser Datei.</para>
        ///
        /// <para><b>Die y-Achse ist vorzeichenfähig</b> — wie beim
        /// Kapitalwert-Verlauf und aus demselben Grund: Ein Wochenwert ist eine
        /// ABWEICHUNG und darf den Monatswert unter null ziehen. Die Nulllinie
        /// wird dann gestrichelt hervorgehoben.</para>
        /// </summary>
        /// <param name="titel">Überschrift ohne Einheit.</param>
        /// <param name="stundenwerte">Das Jahresprofil; kürzere Reihen werden
        /// über ihre eigene Länge auf die Monatsachse gelegt.</param>
        /// <param name="einheit">Einheit für Überschrift und y-Achse, z. B. „ct/kWh".</param>
        /// <param name="achseMonat">Beschriftung der x-Achse (Resource CHART_ACHSE_MONAT).</param>
        public static byte[] Kostenprofil(string titel, double[] stundenwerte,
                                          string einheit, string achseMonat)
        {
            int W = 1296, H = 780;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W);
                var rc = SKRect.Create(110f, 80f, W - 150f, 560f);

                if (stundenwerte == null || stundenwerte.Length < 2)
                {
                    using (var f = Schrift(18f))
                        Text(g, "Kein Profil vorhanden.", f, SKColors.DimGray, rc.Left, rc.Top + 20f);
                    return Png(flaeche);
                }

                // Vorzeichenfähige Skala mit „schönen" Stufen (5 Rasterlinien) —
                // dieselbe Rechnung wie in KapitalwertVerlauf.
                double min = Math.Min(0, stundenwerte.Min());
                double max = Math.Max(0, stundenwerte.Max());
                if (max - min < 1e-9) { max = min + 1; }
                double roh = (max - min) / 5.0;
                double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
                double schritt = zehner;
                foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                    if (zehner * f >= roh) { schritt = zehner * f; break; }
                min = Math.Floor(min / schritt) * schritt;
                max = Math.Ceiling(max / schritt) * schritt;

                // Raster + y-Beschriftung.
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (double wert = min; wert <= max + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        g.DrawLine(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.###", DE);
                        float breite = f.MeasureText(lab);
                        Text(g, lab, f, SKColors.DimGray, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }

                // x-Achse: Monatsgrenzen 0…12, Abstand 1 (AxisX.Interval = 1).
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int m = 0; m <= 12; m++)
                    {
                        float x = rc.Left + m / 12f * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = m.ToString(DE);
                        float breite = f.MeasureText(lab);
                        Text(g, lab, f, SKColors.DimGray, x - breite / 2f, rc.Bottom + 8f);
                    }
                using (var f = Schrift(15f))
                    Text(g, achseMonat ?? "", f, SKColors.DimGray, rc.Right + 10f, rc.Bottom + 8f);

                // Achsen + Nulllinie, wenn die Skala unter null reicht.
                using (var achse = Strich(SKColors.DimGray, 2f))
                {
                    g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                    g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
                }
                if (min < 0)
                {
                    float y0 = (float)(rc.Bottom - (0 - min) / (max - min) * rc.Height);
                    using (var strichel = SKPathEffect.CreateDash(new[] { 3f * 2f, 1f * 2f }, 0f))
                    using (var stift = Strich(SKColors.DimGray, 2f))
                    {
                        stift.PathEffect = strichel;
                        g.DrawLine(rc.Left, y0, rc.Right, y0, stift);
                    }
                }

                // Die Linie. 8 760 Punkte auf 1 146 Bildpunkte: jeder n-te Wert
                // genügt — mehr Punkte als Pixel zeichnen dasselbe Bild langsamer.
                int schrittweite = Math.Max(1, stundenwerte.Length / (int)rc.Width);
                var punkte = new List<SKPoint>();
                for (int i = 0; i < stundenwerte.Length; i += schrittweite)
                {
                    float x = rc.Left + (float)i / (stundenwerte.Length - 1) * rc.Width;
                    float y = (float)(rc.Bottom - (stundenwerte[i] - min) / (max - min) * rc.Height);
                    punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
                }
                using (var stift = Strich(C_PROFIL, 2f))
                {
                    stift.StrokeJoin = SKStrokeJoin.Round;
                    Linienzug(g, punkte.ToArray(), stift);
                }

                Legende(g, new List<Segment> { new Segment(titel, 0, C_PROFIL) }, 110f, H - 56f);
                return Png(flaeche);
            }
        }

        // =================================================================== Schrift

        // ---------------------------------------------------------------------
        // ENTSCHEIDUNG iF19 — keine mitgelieferte Schriftdatei.
        //
        // Der Bericht schrieb seit jeher hart „Calibri". Unter Windows ist die
        // Schrift mit Office da, auf jedem anderen System nicht. Statt eine
        // Schriftdatei mitzuliefern (Lizenz, Paketgroesse, Pflege) faellt der
        // Renderer der Reihe nach zurueck:
        //
        //   1. die Familien aus ERSATZSCHRIFTEN, in dieser Reihenfolge
        //      → Windows: die echte Calibri, damit sich am Bild NICHTS aendert.
        //      → Linux/CI: Carlito (metrisch wie Calibri), sonst Liberation Sans
        //        oder DejaVu Sans.
        //   2. die Systemschrift im gewuenschten Stil (MatchFamily(null, Stil))
        //      → iOS/macOS: Helvetica bzw. SF Pro.
        //   3. irgendeine Schrift, die ein 'A' zeichnen kann (MatchCharacter)
        //   4. SKTypeface.Default — der Notnagel, der nie null ist.
        //
        // Auf Systemen mit fontconfig kann Schritt 1 bereits bei „Calibri" eine
        // Ersatzschrift liefern (fontconfig antwortet immer mit einer Naeherung);
        // das ist gewollt — gebraucht wird eine lesbare serifenlose Schrift, nicht
        // ausgerechnet Calibri.
        //
        // Die Punktgroessen des Bestandes (14…22 pt) waren GDI+-Punkte bei 96 dpi.
        // SKFont rechnet in Pixeln, deshalb pt * 96/72. Damit bleiben Textgroesse
        // und Bildmasse dieselben wie vor der Portierung.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Die gesuchten Schriftfamilien in dieser Reihenfolge — die erste vorhandene
        /// gewinnt. Dieselbe Liste benutzt der Excel-Bericht für die Spaltenbreiten
        /// (ExcelBerichtGenerator, Paket iU7-4), damit Diagramm und Tabelle desselben
        /// Berichts nicht in verschiedenen Schriften vermessen werden.
        ///
        /// <para><b>Warum die Liste und nicht nur „Calibri".</b> Ohne fontconfig — und
        /// genau ohne die läuft die native Linux-Fassung von SkiaSharp, die die CI
        /// benutzt — liefert <c>MatchFamily("Calibri")</c> nichts, und die reine
        /// Systemschrift war auf dem Probelauf am 03.09.2026 <b>DejaVu Serif</b>. Eine
        /// Serifenschrift in Achsen und Legenden ist gegenüber Calibri ein sichtbarer
        /// Rückschritt; die Liste hält den Bericht auf einer serifenlosen Schrift.
        /// Carlito steht direkt hinter Calibri, weil es metrisch dazu passt.</para>
        /// </summary>
        private static readonly string[] ERSATZSCHRIFTEN =
        { "Calibri", "Carlito", "Liberation Sans", "DejaVu Sans", "Helvetica", "Arial" };

        private static readonly Dictionary<int, SKTypeface> _schriftarten = new Dictionary<int, SKTypeface>();
        private static readonly object _schriftSchloss = new object();

        /// <summary>Schriftart je Stil, einmal ermittelt und dann gehalten.</summary>
        private static SKTypeface Schriftart(bool fett, bool kursiv)
        {
            int schluessel = (fett ? 1 : 0) | (kursiv ? 2 : 0);
            lock (_schriftSchloss)
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

        /// <summary>Schrift in Punkt (wie im Bestand) — intern nach Pixeln umgerechnet.</summary>
        private static SKFont Schrift(float punkt, bool fett = false, bool kursiv = false)
        {
            return new SKFont(Schriftart(fett, kursiv), punkt * 96f / 72f)
            {
                Edging = SKFontEdging.Antialias,
                Subpixel = true
            };
        }

        /// <summary>Zeilenhöhe einer Schrift — Ersatz für <c>MeasureString(...).Height</c>.</summary>
        private static float TextHoehe(SKFont f)
        {
            SKFontMetrics m = f.Metrics;
            return m.Descent - m.Ascent;
        }

        /// <summary>
        /// Text an der linken OBEREN Ecke (x, y) — dieselbe Bezugsecke wie
        /// <c>Graphics.DrawString</c>. Skia bezieht sich auf die Grundlinie, deshalb wird
        /// der Aufstieg (negativ) abgezogen.
        /// </summary>
        private static void Text(SKCanvas g, string text, SKFont f, SKColor farbe, float x, float y)
        {
            if (string.IsNullOrEmpty(text)) return;
            using (var p = Fuellung(farbe))
                g.DrawText(text, x, y - f.Metrics.Ascent, f, p);
        }

        // =================================================================== Helfer

        /// <summary>Zeichenfläche mit weißem Grund (ersetzt Bitmap + Graphics.Clear).</summary>
        private static SKSurface Start(int breite, int hoehe)
        {
            var flaeche = SKSurface.Create(
                new SKImageInfo(breite, hoehe, SKColorType.Rgba8888, SKAlphaType.Premul));
            flaeche.Canvas.Clear(SKColors.White);
            return flaeche;
        }

        /// <summary>Flächenfarbe (ersetzt SolidBrush) — immer kantengeglättet.</summary>
        private static SKPaint Fuellung(SKColor farbe)
        {
            return new SKPaint { Color = farbe, Style = SKPaintStyle.Fill, IsAntialias = true };
        }

        /// <summary>Strichfarbe und -stärke (ersetzt Pen) — immer kantengeglättet.</summary>
        private static SKPaint Strich(SKColor farbe, float staerke)
        {
            return new SKPaint
            {
                Color = farbe,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = staerke,
                IsAntialias = true
            };
        }

        /// <summary>Kreissegment vom Mittelpunkt aus (ersetzt Graphics.FillPie).</summary>
        private static void Kreissegment(SKCanvas g, SKRect rect, float start, float sweep, SKPaint fuellung)
        {
            using (var pfad = new SKPath())
            {
                pfad.MoveTo(rect.MidX, rect.MidY);
                pfad.ArcTo(rect, start, sweep, false);
                pfad.Close();
                g.DrawPath(pfad, fuellung);
            }
        }

        /// <summary>Streckenzug (ersetzt Graphics.DrawLines).</summary>
        private static void Linienzug(SKCanvas g, SKPoint[] punkte, SKPaint stift)
        {
            if (punkte == null || punkte.Length < 2) return;
            using (var pfad = new SKPath())
            {
                pfad.MoveTo(punkte[0]);
                for (int i = 1; i < punkte.Length; i++) pfad.LineTo(punkte[i]);
                g.DrawPath(pfad, stift);
            }
        }

        /// <summary>Gefülltes Vieleck (ersetzt Graphics.FillPolygon).</summary>
        private static void Vieleck(SKCanvas g, SKPoint[] punkte, SKPaint fuellung)
        {
            if (punkte == null || punkte.Length < 3) return;
            using (var pfad = new SKPath())
            {
                pfad.MoveTo(punkte[0]);
                for (int i = 1; i < punkte.Length; i++) pfad.LineTo(punkte[i]);
                pfad.Close();
                g.DrawPath(pfad, fuellung);
            }
        }

        private static void Titel(SKCanvas g, string text, int breite)
        {
            using (var f = Schrift(22f, fett: true))
                Text(g, text, f, C_STAMM, 24f, 16f);
        }

        private static void PanelRahmen(SKCanvas g, SKRect rc, string titel)
        {
            using (var rahmen = Strich(SKColors.Silver, 1f))
                g.DrawRect(rc.Left, rc.Top, rc.Width, rc.Height, rahmen);
            using (var f = Schrift(16f, fett: true))
                Text(g, titel, f, SKColors.DimGray, rc.Left, rc.Top - 28f);
        }

        private static void AchsenRaster(SKCanvas g, SKRect rc, double max,
                                         int[] xpos, string[] xlab, int n)
        {
            using (var raster = Strich(SKColors.Gainsboro, 1f))
            using (var f = Schrift(15f))
            {
                for (int s = 0; s <= 4; s++)
                {
                    float y = rc.Bottom - s * rc.Height / 4f;
                    g.DrawLine(rc.Left, y, rc.Right, y, raster);
                    string lab = (max * s / 4.0).ToString("N0", DE);
                    float breite = f.MeasureText(lab);
                    Text(g, lab, f, SKColors.DimGray, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                }
                if (xpos != null)
                    for (int i = 0; i < xpos.Length; i++)
                    {
                        float x = rc.Left + (float)xpos[i] / Math.Max(n - 1, 1) * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        float breite = f.MeasureText(xlab[i]);
                        Text(g, xlab[i], f, SKColors.DimGray, x - breite / 2f, rc.Bottom + 8f);
                    }
            }
            using (var achse = Strich(SKColors.DimGray, 2f))
            {
                g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
            }
        }

        private static void ZeichneLinie(SKCanvas g, SKRect rc, double[] werte,
                                         double min, double max, SKColor farbe, float staerke)
        {
            if (werte == null || werte.Length < 2) return;
            int schritt = Math.Max(1, werte.Length / (int)rc.Width);
            var punkte = new List<SKPoint>();
            for (int i = 0; i < werte.Length; i += schritt)
            {
                float x = rc.Left + (float)i / (werte.Length - 1) * rc.Width;
                float y = rc.Bottom - (float)((werte[i] - min) / (max - min) * rc.Height);
                punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
            }
            if (punkte.Count >= 2)
                using (var stift = Strich(farbe, staerke))
                {
                    stift.StrokeJoin = SKStrokeJoin.Round;
                    Linienzug(g, punkte.ToArray(), stift);
                }
        }

        private static void ZeichneFlaeche(SKCanvas g, SKRect rc, double[] unten,
                                           double[] oben, double max, SKColor farbe)
        {
            int n = oben.Length;
            int schritt = Math.Max(1, n / (int)rc.Width);
            var pfad = new List<SKPoint>();
            for (int i = 0; i < n; i += schritt)
                pfad.Add(Punkt(rc, i, n, oben[i], max));
            for (int i = ((n - 1) / schritt) * schritt; i >= 0; i -= schritt)
                pfad.Add(Punkt(rc, i, n, unten[i], max));
            if (pfad.Count >= 3)
                using (var br = Fuellung(farbe.WithAlpha(210)))
                    Vieleck(g, pfad.ToArray(), br);
        }

        private static SKPoint Punkt(SKRect rc, int i, int n, double wert, double max)
        {
            float x = rc.Left + (float)i / (n - 1) * rc.Width;
            float y = rc.Bottom - (float)(wert / max * rc.Height);
            return new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
        }

        private static void Legende(SKCanvas g, List<Segment> eintraege, float x, float y,
                                    float umbruchBei = 0)
        {
            float startX = x;
            using (var f = Schrift(16f))
            using (var rahmen = Strich(SKColors.Gray, 1f))
                foreach (Segment s in eintraege)
                {
                    float breite = 40f + f.MeasureText(s.Label ?? "") + 24f;
                    if (umbruchBei > 0 && x > startX && x + breite > umbruchBei)
                    { x = startX; y += 30f; }   // Umbruch bei vielen Serien (Review 11)
                    using (var b = Fuellung(s.Farbe)) g.DrawRect(x, y, 22f, 22f, b);
                    g.DrawRect(x, y, 22f, 22f, rahmen);
                    Text(g, s.Label, f, SKColors.Black, x + 28f, y + 1f);
                    x += breite;
                }
        }

        private static byte[] Png(SKSurface flaeche)
        {
            using (SKImage bild = flaeche.Snapshot())
            using (SKData daten = bild.Encode(SKEncodedImageFormat.Png, 100))
                return daten.ToArray();
        }

        // Erzeugerreihen Wärme in fester Stapelreihenfolge (Solar unten … Kessel oben).
        private static List<Reihe> WaermeErzeugerReihen(ZeitreihenSatz z, bool tagesmittel)
        {
            var l = new List<Reihe>();
            Action<string, string, SKColor> add = (key, name, farbe) =>
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
