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

            /// <summary>
            /// Zu welchem Stapel die Reihe gehoert (iU9-W11a.6). Der Vorlaeufer trennte
            /// zwei Stapel in EINEM Diagramm ueber <c>StackedGroupName</c> — auf der
            /// Waermepumpenseite „Bedarf" (Flaeche) und „Produktion" (Saeule). Nur
            /// <see cref="ErzeugerStapel"/> wertet das Feld aus.
            /// </summary>
            public Stapelart Stapelgruppe = Stapelart.Keine;

            /// <summary>
            /// Gestrichelt zeichnen (iU9-W11a.6). Im Bestand traegt die UNTERE
            /// Speicherschicht <c>ChartDashStyle.Dash</c> — zwei Temperaturen desselben
            /// Behaelters gehoeren zusammen und sollen sich trotzdem unterscheiden.
            /// </summary>
            public bool Gestrichelt;

            /// <summary>
            /// Strichstaerke; <c>0</c> = die Vorgabe des jeweiligen Bildes. Im Bestand
            /// tragen die Dauerlinien <c>BorderWidth 4</c> und die Konturlinie „Gesamt"
            /// ebenfalls 4, die uebrigen 1 bis 2.
            /// </summary>
            public float Breite;

            public Reihe(string n, double[] w, SKColor f) { Name = n; Werte = w; Farbe = f; }

            public Reihe(string n, double[] w, SKColor f, Stapelart gruppe,
                         bool gestrichelt = false, float breite = 0f)
            {
                Name = n; Werte = w; Farbe = f;
                Stapelgruppe = gruppe; Gestrichelt = gestrichelt; Breite = breite;
            }
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

        // =================================================================== Jahresgang

        /// <summary>
        /// Quelltemperatur im Jahresgang — <c>Color.FromArgb(200, Color.SaddleBrown)</c>
        /// aus <c>Form_QuelleErdreich.ChartAufbauen</c>:634. SkiaSharp nimmt die Deckung
        /// als vierten Wert.
        /// </summary>
        public static readonly SKColor C_QUELLTEMPERATUR = new SKColor(0x8B, 0x45, 0x13, 200);

        /// <summary>
        /// Außentemperatur im Jahresgang — <c>Color.FromArgb(90, Color.SteelBlue)</c>
        /// (ebenda :647). Sie ist die BEZUGSlinie und deshalb blasser und dünner als die
        /// Quelltemperatur; das war im Vorläufer eine Entscheidung und keine Zufälligkeit.
        /// </summary>
        public static readonly SKColor C_AUSSENTEMPERATUR = new SKColor(0x46, 0x82, 0xB4, 90);

        /// <summary>
        /// Liniendiagramm „Jahresgang" (Paket iU9‑W10a.0d): MEHRERE Stundenreihen über
        /// einer Monatsachse 0…12, mit Legende oben.
        ///
        /// <para><b>Vorbild.</b> Das <c>Chart</c> der Maske <c>Form_QuelleErdreich</c>
        /// (652 × 170, <c>ChartAufbauen</c> :611-659): ein Diagrammbereich „Jahr",
        /// x-Achse 0…12 im Abstand 1, beide Raster gepunktet, Legende oben und zentriert,
        /// ZWEI Reihen <c>FastLine</c> — Quelltemperatur in halbtransparentem SaddleBrown
        /// mit Stärke 2, Außentemperatur in stark transparentem SteelBlue mit Stärke 1.
        /// Die Punkte entstanden dort aus <c>x = i * 12 / 8760</c>; dieselbe Abbildung
        /// steht hier.</para>
        ///
        /// <para><b>Warum nicht <see cref="Jahresverlauf"/>.</b> Jenes Bild aus Welle 8
        /// zeichnet EINE Reihe, führt keine Legende und beschriftet die x-Achse mit
        /// Monatsnamen statt mit den Zahlen 0…12. Der Erdreich-Dialog braucht die zweite
        /// Reihe: Die Quelltemperatur ist nur im Vergleich zur Außentemperatur zu lesen —
        /// gedämpft und phasenverschoben ist eine Aussage ÜBER die Außentemperatur.</para>
        ///
        /// <para><b>Bildmaß 1304 × 440.</b> Die Breite und die Diagrammhöhe sind die
        /// doppelte Zielauflösung des Vorläufers (2 × 652 × 170), wie bei allen Bildern
        /// dieser Datei. Dazu kommen 100 px für die Legende: Sie stand im WinForms-Chart
        /// INNERHALB der Zeichenfläche (<c>Docking.Top</c>) und verdeckte dort den
        /// Jahresanfang beider Linien.</para>
        ///
        /// <para><b>Die y-Achse ist vorzeichenfähig.</b> Eine Quelltemperatur unter 0 °C
        /// ist der Normalfall, nicht die Ausnahme; die Nulllinie wird dann gestrichelt
        /// hervorgehoben — dieselbe Regel wie beim Kostenprofil.</para>
        /// </summary>
        /// <param name="titel">Überschrift ohne Einheit.</param>
        /// <param name="reihen">
        /// Die Reihen in Zeichenreihenfolge; jede mit eigener Farbe und eigenem Namen für
        /// die Legende. Reihen ohne Werte fallen still weg — der Vorläufer zeichnete die
        /// Außentemperatur ebenfalls nur, wenn es Klimadaten gab.
        /// </param>
        /// <param name="xTitel">Beschriftung der x-Achse (Resource CHART_ACHSE_MONAT).</param>
        /// <param name="yTitel">Beschriftung der y-Achse (Resource CHART_ACHSE_QUELLTEMPERATUR).</param>
        public static byte[] Jahresgang(string titel, IReadOnlyList<Reihe> reihen,
                                        string xTitel, string yTitel)
        {
            int W = 1304, H = 440;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel ?? "", W);

                // Legende OBEN wie im Vorlaeufer, aber ueber der Zeichenflaeche statt
                // darin - sonst verdeckt sie bei zwei Reihen den Jahresanfang.
                var gueltig = (reihen ?? new List<Reihe>())
                    .Where(r => r != null && r.Werte != null && r.Werte.Length >= 2 &&
                                r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w)))
                    .ToList();

                var rc = SKRect.Create(110f, 130f, W - 150f, 240f);

                if (gueltig.Count == 0)
                {
                    using (var f = Schrift(18f))
                        Text(g, BerichtTexte.T("Kein Jahresgang vorhanden."), f, SKColors.DimGray,
                             rc.Left, rc.Top + 20f);
                    return Png(flaeche);
                }

                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        110f, 76f, W - 30f);

                // Vorzeichenfaehige Skala mit "schoenen" Stufen (5 Rasterlinien) -
                // dieselbe Rechnung wie in KapitalwertVerlauf und Kostenprofil.
                double min = gueltig.Min(r => r.Werte.Min());
                double max = gueltig.Max(r => r.Werte.Max());
                if (min > 0) min = 0;              // die Null gehoert ins Bild
                if (max < 0) max = 0;
                if (max - min < 1e-9) { max = min + 1; }
                double roh = (max - min) / 5.0;
                double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
                double schritt = zehner;
                foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                    if (zehner * f >= roh) { schritt = zehner * f; break; }
                min = Math.Floor(min / schritt) * schritt;
                max = Math.Ceiling(max / schritt) * schritt;

                // Raster + y-Beschriftung. GEPUNKTET wie im Vorlaeufer
                // (ChartDashStyle.Dot auf beiden Achsen).
                using (var punktiert = SKPathEffect.CreateDash(new[] { 2f, 4f }, 0f))
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                {
                    raster.PathEffect = punktiert;
                    for (double wert = min; wert <= max + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        g.DrawLine(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.###", DE);
                        float breite = f.MeasureText(lab);
                        Text(g, lab, f, SKColors.DimGray, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }
                }

                // x-Achse: Monatsgrenzen 0…12, Abstand 1 (AxisX.Interval = 1).
                using (var punktiert = SKPathEffect.CreateDash(new[] { 2f, 4f }, 0f))
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                {
                    raster.PathEffect = punktiert;
                    for (int m = 0; m <= 12; m++)
                    {
                        float x = rc.Left + m / 12f * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = m.ToString(DE);
                        float breite = f.MeasureText(lab);
                        Text(g, lab, f, SKColors.DimGray, x - breite / 2f, rc.Bottom + 8f);
                    }
                }
                using (var f = Schrift(15f))
                {
                    Text(g, xTitel ?? "", f, SKColors.DimGray, rc.Right + 10f, rc.Bottom + 8f);
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);
                }

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

                // Die Linien. Der Vorlaeufer legte die Reihen mit x = i * 12 / 8760 auf
                // die Monatsachse; ueber die eigene Laenge gerechnet ist das dasselbe und
                // traegt zusaetzlich kuerzere Reihen. Mehr Punkte als Bildpunkte zeichnen
                // dasselbe Bild langsamer - deshalb jeder n-te Wert.
                foreach (Reihe r in gueltig)
                {
                    // Strichstaerke woertlich: die erste Reihe 2, jede weitere 1
                    // (BorderWidth 2 fuer die Quelltemperatur, 1 fuer die Aussentemperatur).
                    float staerke = ReferenceEquals(r, gueltig[0]) ? 2f : 1f;

                    int schrittweite = Math.Max(1, r.Werte.Length / (int)rc.Width);
                    var punkte = new List<SKPoint>();
                    for (int i = 0; i < r.Werte.Length; i += schrittweite)
                    {
                        float x = rc.Left + (float)i / (r.Werte.Length - 1) * rc.Width;
                        float y = (float)(rc.Bottom - (r.Werte[i] - min) / (max - min) * rc.Height);
                        punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
                    }
                    using (var stift = Strich(r.Farbe, staerke))
                    {
                        stift.StrokeJoin = SKStrokeJoin.Round;
                        Linienzug(g, punkte.ToArray(), stift);
                    }
                }

                return Png(flaeche);
            }
        }

        // =================================================================== Kennlinien

        /// <summary>
        /// EINE Kennlinie — die Stützstellen einer Vorlauftemperatur (iU9-W7.0c).
        /// </summary>
        /// <param name="Vorlauf">Vorlauftemperatur [°C]; sie beschriftet die Reihe.</param>
        /// <param name="Punkte">
        /// Die Stützstellen (Außentemperatur, Wert) in Anzeigereihenfolge. Der Renderer
        /// sortiert NICHT — das tut die Abfrage (<c>ORDER BY Temperatur ASC</c>), wie im
        /// Vorläufer.
        /// </param>
        public sealed record KennlinienReihe(int Vorlauf, IReadOnlyList<(double Temperatur, double Wert)> Punkte);

        /// <summary>Die Punktmarke einer Kennlinie — Kreis für COP, Kreuz für die Leistung.</summary>
        public enum Kennlinienmarke
        {
            /// <summary>Kreis (<c>MarkerStyle.Circle</c> des Vorläufers, Form_WP:314).</summary>
            Kreis,

            /// <summary>Kreuz (<c>MarkerStyle.Cross</c> des Vorläufers, Form_WP:321).</summary>
            Kreuz
        }

        /// <summary>
        /// Kennliniendiagramm einer Wärmepumpe (Paket iU9-W7.0c): COP bzw. Leistung über
        /// der Außentemperatur, EINE Linie je Vorlauftemperatur.
        ///
        /// <para><b>Vorbild.</b> Die vier <c>Chart</c>-Steuerelemente von
        /// <c>Form_WP</c> (<c>InitChart</c>, Z. 243-331) und <c>Wizard_WPItem</c>
        /// (<c>listBox_WP_SelectedIndexChanged</c>, Z. 333-383) — je zwei in einem
        /// <c>TabControl</c> mit den Blättern „COP" und „Leistung". Beide Masken bauten
        /// dieselben Reihen aus denselben Abfragen auf; sie unterscheiden sich nur darin,
        /// dass <c>Form_WP</c> zwischen Wärme- und Kühlkennlinien umschalten kann.</para>
        ///
        /// <para><b>Bildmaß 968 × 520.</b> Der breitere der vier Vorläufer-Charts maß
        /// 484 × 195 (<c>Form_WP.chart1</c>); doppelte Zielauflösung wie bei allen Bildern
        /// dieser Datei ergibt 968 × 390. Dazu kommen 130 px für die Legende, die hier
        /// UNTER dem Diagramm steht statt wie im WinForms-Chart darin — bei acht Reihen
        /// verdeckte sie dort die Linien.</para>
        ///
        /// <para><b>Die x-Achse trägt echte Werte</b>, nicht Stützstellennummern: Die
        /// Außentemperaturen zweier Vorlauf-Kennlinien müssen nicht dieselben sein, und
        /// bei ungleichen Reihen läge sonst -15 °C der einen über -7 °C der anderen.
        /// Beide Achsen bekommen die „schöne" Stufung der übrigen Liniendiagramme.</para>
        ///
        /// <para><b>Die y-Achse schließt die Null ein.</b> COP und Leistung sind
        /// positiv; ein Diagramm, das erst bei 2,8 beginnt, macht aus einem Unterschied
        /// von 10 % optisch einen von 80 %. Dasselbe hält der Kapitalwert-Verlauf so
        /// (Abweichung A-4 des Protokolls W7 — das WinForms-Chart skalierte
        /// selbsttätig).</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Kennlinien COP".</param>
        /// <param name="yTitel">Beschriftung der y-Achse — „COP" bzw. „Leistung".</param>
        /// <param name="xTitel">Beschriftung der x-Achse — „Temperatur".</param>
        /// <param name="reihen">Eine Reihe je Vorlauftemperatur.</param>
        /// <param name="marke">Punktmarke: Kreis für COP, Kreuz für die Leistung.</param>
        public static byte[] Kennlinien(string titel, string yTitel, string xTitel,
                                        IReadOnlyList<KennlinienReihe> reihen,
                                        Kennlinienmarke marke)
        {
            int W = 968, H = 520;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel, W);
                // Rechts bleiben 150 px stehen: Dort steht die Beschriftung der x-Achse,
                // und die letzte Rasterzahl braucht ihre halbe Breite (der Bericht setzt
                // den Achsentitel genauso, KapitalwertVerlauf mit „Jahr").
                var rc = SKRect.Create(90f, 76f, W - 240f, 296f);

                var gueltig = new List<KennlinienReihe>();
                if (reihen != null)
                    foreach (KennlinienReihe r in reihen)
                        if (r != null && r.Punkte != null && r.Punkte.Count > 0 &&
                            r.Punkte.All(p => !double.IsNaN(p.Temperatur) && !double.IsInfinity(p.Temperatur) &&
                                              !double.IsNaN(p.Wert) && !double.IsInfinity(p.Wert)))
                            gueltig.Add(r);

                if (gueltig.Count == 0)
                {
                    using (var f = Schrift(18f))
                        Text(g, BerichtTexte.T("Keine Kennlinien vorhanden."), f, SKColors.DimGray,
                             rc.Left, rc.Top + 20f);
                    return Png(flaeche);
                }

                double xMin = gueltig.Min(r => r.Punkte.Min(p => p.Temperatur));
                double xMax = gueltig.Max(r => r.Punkte.Max(p => p.Temperatur));
                double yMin = Math.Min(0, gueltig.Min(r => r.Punkte.Min(p => p.Wert)));
                double yMax = Math.Max(0, gueltig.Max(r => r.Punkte.Max(p => p.Wert)));

                double xSchritt = Stufe(ref xMin, ref xMax);
                double ySchritt = Stufe(ref yMin, ref yMax);

                // y-Raster und -Beschriftung.
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (double wert = yMin; wert <= yMax + ySchritt / 2; wert += ySchritt)
                    {
                        float y = (float)(rc.Bottom - (wert - yMin) / (yMax - yMin) * rc.Height);
                        g.DrawLine(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.###", DE);
                        Text(g, lab, f, SKColors.DimGray, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }

                // x-Raster und -Beschriftung.
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (double wert = xMin; wert <= xMax + xSchritt / 2; wert += xSchritt)
                    {
                        float x = (float)(rc.Left + (wert - xMin) / (xMax - xMin) * rc.Width);
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = wert.ToString("0.###", DE);
                        Text(g, lab, f, SKColors.DimGray, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }

                // Achsen, Achsentitel und - falls die Skala unter null reicht - die Nulllinie.
                using (var achse = Strich(SKColors.DimGray, 2f))
                {
                    g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                    g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
                }
                using (var f = Schrift(15f))
                {
                    // 26 px statt der 10 px des Kapitalwert-Verlaufs: Dort steht rechts
                    // eine einstellige Jahreszahl, hier eine zweistellige Temperatur mit
                    // Vorzeichen - bei 10 px stiessen Zahl und Titel aneinander.
                    Text(g, xTitel ?? "", f, SKColors.DimGray, rc.Right + 26f, rc.Bottom + 8f);
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);
                }
                if (yMin < 0)
                {
                    float y0 = (float)(rc.Bottom - (0 - yMin) / (yMax - yMin) * rc.Height);
                    using (var strichel = SKPathEffect.CreateDash(new[] { 6f, 2f }, 0f))
                    using (var stift = Strich(SKColors.DimGray, 2f))
                    {
                        stift.PathEffect = strichel;
                        g.DrawLine(rc.Left, y0, rc.Right, y0, stift);
                    }
                }

                // Die Linien samt Punktmarken. Die Farbe kommt aus C_SERIEN und
                // wiederholt sich, wenn ein Gerät mehr Vorläufe führt als Farben da sind.
                for (int i = 0; i < gueltig.Count; i++)
                {
                    SKColor farbe = C_SERIEN[i % C_SERIEN.Length];
                    var punkte = new SKPoint[gueltig[i].Punkte.Count];
                    for (int t = 0; t < punkte.Length; t++)
                    {
                        var p = gueltig[i].Punkte[t];
                        float x = (float)(rc.Left + (p.Temperatur - xMin) / (xMax - xMin) * rc.Width);
                        float y = (float)(rc.Bottom - (p.Wert - yMin) / (yMax - yMin) * rc.Height);
                        punkte[t] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                    }

                    using (var stift = Strich(farbe, 3f))
                    {
                        stift.StrokeJoin = SKStrokeJoin.Round;
                        Linienzug(g, punkte, stift);
                    }
                    Punktmarken(g, punkte, farbe, marke);
                }

                Legende(g, gueltig.Select(r => new Segment(
                            r.Vorlauf.ToString(DE) + "°C", 0, C_SERIEN[gueltig.IndexOf(r) % C_SERIEN.Length]))
                        .ToList(), 90f, H - 96f, W - 30f);
                return Png(flaeche);
            }
        }

        /// <summary>
        /// Die Punktmarken einer Kennlinie. <c>MarkerSize = 5</c> des Vorläufers bei
        /// einfacher Auflösung sind hier 10 px — dieselbe optische Größe.
        /// </summary>
        private static void Punktmarken(SKCanvas g, SKPoint[] punkte, SKColor farbe, Kennlinienmarke marke)
        {
            const float R = 5f;
            if (marke == Kennlinienmarke.Kreis)
            {
                using (var b = Fuellung(farbe))
                    foreach (SKPoint p in punkte) g.DrawCircle(p, R, b);
                return;
            }

            using (var stift = Strich(farbe, 2.5f))
                foreach (SKPoint p in punkte)
                {
                    g.DrawLine(p.X - R, p.Y - R, p.X + R, p.Y + R, stift);
                    g.DrawLine(p.X - R, p.Y + R, p.X + R, p.Y - R, stift);
                }
        }

        /// <summary>
        /// Die „schöne" Achsenstufung der Liniendiagramme (5 Rasterlinien), aus
        /// <see cref="KapitalwertVerlauf"/> herausgezogen, weil die Kennlinien sie für
        /// BEIDE Achsen brauchen. Rundet <paramref name="min"/> ab und
        /// <paramref name="max"/> auf und liefert die Schrittweite.
        /// </summary>
        private static double Stufe(ref double min, ref double max)
        {
            if (max - min < 1e-9) max = min + 1;
            double roh = (max - min) / 5.0;
            double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
            double schritt = zehner;
            foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (zehner * f >= roh) { schritt = zehner * f; break; }
            min = Math.Floor(min / schritt) * schritt;
            max = Math.Ceiling(max / schritt) * schritt;
            return schritt;
        }

        // =================================================================== Bedarfsbilder (iU9-W8.0c)

        /// <summary>
        /// Die zwölf Monatsnamen, wie die Bedarfsmasken sie an der x-Achse trugen
        /// (<c>Form_ErgStromverbraucher.monate</c>). „Mrz" statt des „Mär" der
        /// Berichtsbilder — wörtlich aus dem Vorläufer, weil dieses Bild ihn ersetzt.
        /// </summary>
        private static readonly string[] MONATE_KURZ =
        { "Jan", "Feb", "Mrz", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

        /// <summary>
        /// Die Füllfarbe des Stundenprofils — halbtransparentes Blau, wörtlich aus
        /// <c>Form_EingStromTyp.ChartAktualisieren</c> (<c>Color.FromArgb(100, Color.Blue)</c>).
        /// </summary>
        public static readonly SKColor C_PROFILFLAECHE = new SKColor(0x00, 0x00, 0xFF, 100);

        /// <summary>Die Randlinie des Stundenprofils — dasselbe Blau, deckend.</summary>
        public static readonly SKColor C_PROFILLINIE = new SKColor(0x00, 0x00, 0xFF);

        /// <summary>
        /// Die „schönen" Schrittweiten der Bedarfsmasken. Wörtlich aus
        /// <c>Form_ErgBrauchwasserwaerme.SkaliereYAchse</c>:288 — eine andere Reihe als die
        /// des Kapitalwert-Verlaufs (dort 1/2/2,5/5/10), weil die Bedarfsbilder auch
        /// Zehntel brauchen.
        /// </summary>
        private static readonly double[] SCHOENE_SCHRITTE =
        { 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 2.5, 5.0, 10.0 };

        /// <summary>
        /// Die y-Achse der Bedarfsbilder: Schrittweite, Obergrenze und Zahlenformat aus dem
        /// Größtwert. Wörtlich aus <c>SkaliereYAchse</c> (dreimal gleichlautend in den drei
        /// Ergebnismasken) — samt dem Rückfall „Maximum 5, Intervall 1", wenn alle Werte
        /// null sind, und der Sicherung gegen eine Schrittweite ≤ 0.
        /// </summary>
        private static (double Schritt, double Max, string Format) BedarfsSkala(double maxWert)
        {
            if (maxWert <= 0) return (1.0, 5.0, "N0");

            double zielSchrittweite = (maxWert * 1.1) / 4.5;
            double groessenordnung = Math.Pow(10, Math.Floor(Math.Log10(zielSchrittweite)));
            double normiert = zielSchrittweite / groessenordnung;

            double gewaehlt = SCHOENE_SCHRITTE[SCHOENE_SCHRITTE.Length - 1];
            foreach (double schritt in SCHOENE_SCHRITTE)
                if (normiert <= schritt) { gewaehlt = schritt; break; }

            double finale = gewaehlt * groessenordnung;
            double obergrenze = Math.Round(Math.Ceiling((maxWert * 1.05) / finale) * finale, 4);
            if (finale <= 0) { finale = 0.5; obergrenze = 2.0; }

            string format = finale >= 1.0 ? "N0" : finale >= 0.1 ? "N1" : "N2";
            return (finale, obergrenze, format);
        }

        /// <summary>Zeichnet Raster, y-Beschriftung und die beiden Achsen einer Bedarfsskala.</summary>
        private static void BedarfsRaster(SKCanvas g, SKRect rc, double schritt, double max, string format)
        {
            using (var raster = Strich(SKColors.Gainsboro, 1f))
            using (var f = Schrift(15f))
                for (double wert = 0; wert <= max + schritt / 2; wert += schritt)
                {
                    float y = (float)(rc.Bottom - wert / max * rc.Height);
                    g.DrawLine(rc.Left, y, rc.Right, y, raster);
                    string lab = wert.ToString(format, DE);
                    Text(g, lab, f, SKColors.DimGray, rc.Left - f.MeasureText(lab) - 6f, y - TextHoehe(f) / 2f);
                }

            using (var achse = Strich(SKColors.DimGray, 2f))
            {
                g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
            }
        }

        /// <summary>
        /// Senkrechte Monatssäulen (Paket iU9-W8.0c) — das Bild der drei Ergebnismasken
        /// <c>Form_ErgStromverbraucher</c>, <c>Form_ErgProzesswaerme</c> und
        /// <c>Form_ErgBrauchwasserwaerme</c>.
        ///
        /// <para><b>Vorbild.</b> <c>ZeigeStromGrafik</c>:83 bzw. <c>ZeigeMonatsGrafik</c>: ein
        /// <c>SeriesChartType.Column</c> auf einer x-Achse, die STARR von 1 bis 12 läuft
        /// (<c>Minimum = 1</c>, <c>Maximum = 12</c>, <c>Interval = 1</c>), y ab 0, keine
        /// Legende. Dieselbe Starrheit steht hier: zwölf gleich breite Fächer, jedes mit
        /// seinem Monatsnamen, unabhängig davon, wie viele Werte ungleich null sind.</para>
        ///
        /// <para><b>Bildmaß 978 × 542.</b> Der größte der drei Vorläufer-Charts maß 489 × 271
        /// (<c>Form_ErgBrauchwasserwaerme.chart1</c>); doppelte Zielauflösung wie bei allen
        /// Bildern dieser Datei.</para>
        ///
        /// <para><b>Die Farbe kommt von außen</b>, weil sie die SICHT benennt: gelbgrün für
        /// den Strombedarf, rot für die Prozesse, blau für die Gebäude, orange für das
        /// Brauchwasser — wörtlich die vier Farben der Vorläufer.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Strombedarf Monatsübersicht".</param>
        /// <param name="werte">Die zwölf Monatswerte; kürzere Reihen zeichnen nur den Hinweis.</param>
        /// <param name="farbe">Säulenfarbe der Sicht.</param>
        /// <param name="einheit">Einheit für die Überschrift, z. B. „MWh"; leer = ohne.</param>
        /// <param name="monatsnamen">Die zwölf Beschriftungen; <c>null</c> = die deutschen des Vorläufers.</param>
        public static byte[] MonatsSaeulen(string titel, double[] werte, SKColor farbe,
                                           string einheit, IReadOnlyList<string> monatsnamen = null)
        {
            int W = 978, H = 542;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W);
                var rc = SKRect.Create(100f, 80f, W - 140f, 380f);

                if (werte == null || werte.Length < 12)
                {
                    using (var f = Schrift(18f))
                        Text(g, BerichtTexte.T("Keine Monatswerte vorhanden."), f, SKColors.DimGray,
                             rc.Left, rc.Top + 20f);
                    return Png(flaeche);
                }

                double maxWert = 0;
                for (int m = 0; m < 12; m++) if (werte[m] > maxWert) maxWert = werte[m];
                (double schritt, double max, string format) = BedarfsSkala(maxWert);

                BedarfsRaster(g, rc, schritt, max, format);

                float fach = rc.Width / 12f;
                float breite = fach * 0.6f;
                using (var f = Schrift(15f))
                using (var pinsel = Fuellung(farbe))
                    for (int m = 0; m < 12; m++)
                    {
                        float mitte = rc.Left + (m + 0.5f) * fach;

                        double wert = werte[m] > 0 ? werte[m] : 0;   // y beginnt starr bei 0
                        float hoehe = (float)(Math.Min(wert, max) / max * rc.Height);
                        if (hoehe > 0) g.DrawRect(mitte - breite / 2f, rc.Bottom - hoehe, breite, hoehe, pinsel);

                        string lab = (monatsnamen != null && monatsnamen.Count > m)
                            ? monatsnamen[m] : MONATE_KURZ[m];
                        Text(g, lab, f, SKColors.DimGray, mitte - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }

                return Png(flaeche);
            }
        }

        /// <summary>
        /// Stundenprofil als Fläche über einer numerischen Stundenachse (Paket iU9-W8.0c) —
        /// das Bild der drei Typprofilmasken (168 Wochenstunden) UND das der
        /// Gebäudetypmaske (24 Tagesstunden).
        ///
        /// <para><b>Zwei Vorbilder, ein Bild.</b> <c>Form_EingStromTyp.ChartAktualisieren</c>:37
        /// zeichnete eine halbtransparent blaue FLÄCHE über x 0…168 mit Tagesgrenzen alle
        /// 24 Stunden und y bis 1,1 × Größtwert; <c>Form_EingGebTyp.init_Chart</c>:171 eine
        /// LINIE über x 0…24 im Abstand 2 mit gepunktetem Raster. Beides ist dieselbe
        /// Darstellung in zwei Auflösungen; der Unterschied Fläche/Linie war keine
        /// Entscheidung, sondern die Voreinstellung zweier verschiedener Diagrammverwalter.
        /// Hier steht immer die Fläche mit ihrer Randlinie — bei 24 Punkten ist sie so gut
        /// lesbar wie die reine Linie, bei 168 deutlich besser.</para>
        ///
        /// <para><b>Bildmaß 1244 × 464</b> — die doppelte Zielauflösung des breiteren der
        /// beiden Vorläufer (<c>Form_EingGebTyp.chart1</c>, 622 × 203) bei der Höhe des
        /// höheren (<c>Form_EingStromTyp.chart1</c>, 537 × 232).</para>
        ///
        /// <para><b>Die y-Achse endet bei 1,1 × Größtwert</b> (<c>ChartAktualisieren</c>:39),
        /// mit demselben Rückfall auf 1, wenn alle Werte null sind — der abgelöste
        /// Diagrammverwalter hätte dort 100 angenommen.</para>
        /// </summary>
        /// <param name="titel">Überschrift; leer = ohne.</param>
        /// <param name="werte">Die Stundenwerte — 24 oder 168, aber jede Länge ≥ 2 geht.</param>
        /// <param name="intervall">Abstand der x-Beschriftung: 24 (Tagesgrenzen) bzw. 2.</param>
        /// <param name="xTitel">Beschriftung der x-Achse, z. B. „Wochenstunde (1..168)".</param>
        /// <param name="yTitel">Beschriftung der y-Achse, z. B. „Verteilung".</param>
        public static byte[] Stundenprofil(string titel, double[] werte, int intervall,
                                           string xTitel, string yTitel)
        {
            int W = 1244, H = 464;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                if (!string.IsNullOrEmpty(titel)) Titel(g, titel, W);
                var rc = SKRect.Create(100f, 76f, W - 200f, 300f);

                if (werte == null || werte.Length < 2)
                {
                    using (var f = Schrift(18f))
                        Text(g, BerichtTexte.T("Kein Profil vorhanden."), f, SKColors.DimGray,
                             rc.Left, rc.Top + 20f);
                    return Png(flaeche);
                }

                double maxWert = 0;
                foreach (double w in werte) if (w > maxWert) maxWert = w;
                double max = (maxWert > 0 ? maxWert : 1) * 1.1;

                // y-Raster in fünf Stufen; die Zahlen tragen so viele Stellen, wie der
                // Größtwert braucht (bei Verteilungen unter 1 sonst lauter Nullen).
                string format = max >= 10 ? "0" : max >= 1 ? "0.0" : "0.000";
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int i = 0; i <= 5; i++)
                    {
                        double wert = max * i / 5.0;
                        float y = rc.Bottom - (float)(i / 5.0) * rc.Height;
                        g.DrawLine(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString(format, DE);
                        Text(g, lab, f, SKColors.DimGray, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }

                // x-Raster: die Stundenmarken des Vorläufers (Intervall 24 bzw. 2).
                int schrittX = intervall > 0 ? intervall : Math.Max(1, werte.Length / 6);
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int h = 0; h <= werte.Length; h += schrittX)
                    {
                        float x = rc.Left + (float)h / werte.Length * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = h.ToString(DE);
                        Text(g, lab, f, SKColors.DimGray, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }

                using (var achse = Strich(SKColors.DimGray, 2f))
                {
                    g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                    g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
                }
                using (var f = Schrift(15f))
                {
                    Text(g, xTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Bottom + 34f);
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);
                }

                // Die Fläche: ein Punkt je Wert, am rechten Rand seines Fachs — Stunde n
                // steht für das Intervall (n-1, n], wie im Vorläufer.
                var punkte = new SKPoint[werte.Length];
                for (int i = 0; i < werte.Length; i++)
                {
                    float x = rc.Left + (float)(i + 1) / werte.Length * rc.Width;
                    float y = (float)(rc.Bottom - Math.Max(0, werte[i]) / max * rc.Height);
                    punkte[i] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                }

                var flaechenzug = new SKPoint[punkte.Length + 3];
                flaechenzug[0] = new SKPoint(rc.Left, rc.Bottom);
                flaechenzug[1] = new SKPoint(rc.Left, punkte[0].Y);
                Array.Copy(punkte, 0, flaechenzug, 2, punkte.Length);
                flaechenzug[flaechenzug.Length - 1] = new SKPoint(punkte[punkte.Length - 1].X, rc.Bottom);

                using (var fuellung = Fuellung(C_PROFILFLAECHE)) Vieleck(g, flaechenzug, fuellung);
                using (var stift = Strich(C_PROFILLINIE, 2f))
                {
                    stift.StrokeJoin = SKStrokeJoin.Round;
                    Linienzug(g, punkte, stift);
                }

                return Png(flaeche);
            }
        }

        /// <summary>
        /// Jahresverlauf über alle 8 760 Stunden (Paket iU9-W8.0c) — die Jahresansicht des
        /// Brauchwasser-Ergebnisdialogs (<c>ZeigeJahresGrafik</c>:166).
        ///
        /// <para><b>OHNE den Mausrad-Zoom des Vorläufers.</b> Der abgelöste
        /// Diagrammverwalter spreizte die Achse am Mausrad und passte die Beschriftung mit;
        /// ein PNG kann das nicht. Ein Bild bleibt ein Bild — Zoomen ist W3-O2/W11
        /// (Abweichung A-1 der Welle 8). Dafür trägt die x-Achse hier die MONATSGRENZEN
        /// statt der Stundenzahlen: „Stunde 5 832" sagt nichts, „Sep" schon.</para>
        ///
        /// <para><b>Bildmaß 978 × 542</b> wie die Monatssäulen — beide teilen sich in der
        /// Maske dieselbe Fläche und wechseln über einen Schalter.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Jahresübersicht".</param>
        /// <param name="stundenwerte">Der Jahresverlauf; jede Länge ≥ 2 geht.</param>
        /// <param name="yTitel">Beschriftung der y-Achse, z. B. „Wärmebedarf [kW]".</param>
        /// <param name="farbe">Linienfarbe (Vorläufer: <c>SteelBlue</c>).</param>
        public static byte[] Jahresverlauf(string titel, double[] stundenwerte, string yTitel, SKColor farbe)
        {
            int W = 978, H = 542;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel, W);
                var rc = SKRect.Create(100f, 80f, W - 140f, 380f);

                if (stundenwerte == null || stundenwerte.Length < 2)
                {
                    using (var f = Schrift(18f))
                        Text(g, BerichtTexte.T("Kein Jahresverlauf vorhanden."), f, SKColors.DimGray,
                             rc.Left, rc.Top + 20f);
                    return Png(flaeche);
                }

                double maxWert = 0;
                foreach (double w in stundenwerte) if (w > maxWert) maxWert = w;
                (double schritt, double max, string format) = BedarfsSkala(maxWert);

                BedarfsRaster(g, rc, schritt, max, format);

                // Monatsgrenzen statt Stundenzahlen (siehe Kopf).
                KeyValuePair<int[], string[]> ticks = MonatsTicks365();
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int m = 0; m < 12; m++)
                    {
                        float x = rc.Left + (float)(ticks.Key[m] * 24) / stundenwerte.Length * rc.Width;
                        if (x > rc.Right) break;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        Text(g, ticks.Value[m], f, SKColors.DimGray, x + 4f, rc.Bottom + 8f);
                    }
                using (var f = Schrift(15f))
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);

                // 8 760 Punkte auf rund 840 Bildpunkte: jeder n-te genügt (wie beim
                // Kostenprofil) — mehr Punkte als Pixel zeichnen dasselbe Bild langsamer.
                int schrittweite = Math.Max(1, stundenwerte.Length / (int)rc.Width);
                var punkte = new List<SKPoint>();
                for (int i = 0; i < stundenwerte.Length; i += schrittweite)
                {
                    float x = rc.Left + (float)i / (stundenwerte.Length - 1) * rc.Width;
                    float y = (float)(rc.Bottom - Math.Max(0, stundenwerte[i]) / max * rc.Height);
                    punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
                }
                using (var stift = Strich(farbe, 2f))
                {
                    stift.StrokeJoin = SKStrokeJoin.Round;
                    Linienzug(g, punkte.ToArray(), stift);
                }

                return Png(flaeche);
            }
        }

        // ================================================== Ergebnisbilder (iU9-W11a.6)
        //
        // Die sieben Bilder der WELLE 11 (Simulationsergebnis). Sie loesen die
        // 17 Zeichenflaechen der sechs Ergebnismasken ab; vier sind neu (B1, B2, B4, B5),
        // drei verallgemeinern vorhandene Methoden (B3 als Option von B1/B2, B6 als
        // freie Fassung von StrombilanzMonate/MonatsSaeulen, B7 als freie Fassung von
        // Speichertemperaturen).
        //
        // WAS SIE VON DEN BERICHTSBILDERN UNTERSCHEIDET. JahresverlaufWaerme,
        // DauerlinieWaerme, StrombilanzMonate und Speichertemperaturen nehmen einen
        // ZeitreihenSatz und tragen feste deutsche Titel im Quelltext - sie sind
        // Berichtsbilder. Der Bildschirm braucht freie Reihenlisten, freie Titel,
        // umschaltbare Achsen und Praesenzfilterung. Die vorhandenen Bilder bleiben
        // deshalb unangetastet (ChartProben prueft sie); ihre Zusammenfuehrung mit den
        // neuen ist ein eigener Schritt (offener Punkt W11a-O-3).

        /// <summary>Die x-Achse eines Ergebnisbildes.</summary>
        public enum Achse
        {
            /// <summary>Monatsgrenzen 0…12 (<c>ConfigureXAxisWithMonths</c>).</summary>
            Monate = 0,
            /// <summary>Jahresstunden mit den Marken 2000/4000/6000/8000
            /// (<c>ConfigureXAxisWithHours</c>).</summary>
            Jahresstunden = 1
        }

        /// <summary>
        /// Zu welchem Stapel eine Reihe gehoert. MS-Chart trennte zwei Stapel in EINEM
        /// Diagramm ueber <c>StackedGroupName</c> — auf der Wärmepumpenseite „Bedarf"
        /// (StackedArea) und „Produktion" (StackedColumn).
        /// </summary>
        public enum Stapelart
        {
            /// <summary>Kein Stapel — die Reihe ist eine Linie.</summary>
            Keine = 0,
            /// <summary>Gestapelte FLAECHE (Vorbild <c>SeriesChartType.StackedArea</c>).</summary>
            Flaeche = 1,
            /// <summary>Gestapelte SAEULE (Vorbild <c>SeriesChartType.StackedColumn</c>).</summary>
            Saeule = 2
        }

        /// <summary>
        /// Ein Segment eines Ringdiagramms (B5).
        /// </summary>
        /// <param name="Name">Anzeigetext der Legende.</param>
        /// <param name="Wert">Der Anteil; Segmente mit <c>&lt;= 0</c> entfallen samt Legende.</param>
        /// <param name="Farbe">Segmentfarbe — sie kommt vom Aufrufer, weil sie den
        /// Erzeuger benennt.</param>
        public sealed record Ringsegment(string Name, double Wert, SKColor Farbe);

        /// <summary>
        /// Eine Punktreihe der Streuwolke (B4).
        /// </summary>
        /// <param name="Name">Anzeigetext der Legende.</param>
        /// <param name="Punkte">Die Punkte (x, y) in beliebiger Reihenfolge.</param>
        /// <param name="Farbe">Punktfarbe — halbtransparent, damit sich 8 760 Punkte
        /// nicht gegenseitig ausloeschen.</param>
        public sealed record Punktreihe(string Name, IReadOnlyList<(double X, double Y)> Punkte,
                                        SKColor Farbe);

        // ------------------------------------------------------------------ B1

        /// <summary>
        /// <b>B1 — NORMIERTE GANGLINIE.</b> Eine bis vier Linien, jede auf DENSELBEN
        /// Hoechstwert normiert und in Prozent gezeichnet (iU9-W11a.6).
        ///
        /// <para><b>Vorbild.</b> <c>chart1</c> (Waermelast) und <c>chart2</c>
        /// (Strombedarf) der Bedarfsseite: <c>AxisY.Maximum = 100,2</c>, die Gesamtkurve
        /// plus bis zu drei Kanalserien in Rot / DeepSkyBlue / <c>7E57A6</c>, x-Achse
        /// umschaltbar zwischen Monatsgrenzen und Jahresstunden, Umschalter
        /// Ganglinie/Dauerlinie.</para>
        ///
        /// <para><b>Warum 100,2 und nicht 100.</b> Woertlich aus <c>init_Chart</c> :3378 —
        /// eine Kurve, die den Hoechstwert erreicht, laege sonst genau auf der oberen
        /// Rahmenlinie.</para>
        ///
        /// <para><b>Der Bezugswert ist der GEMEINSAME Hoechstwert aller Reihen</b>, nicht
        /// je Reihe einer: Die Kanalkurven sollen ihren Anteil an der Gesamtlast zeigen,
        /// und drei je fuer sich normierte Kurven wuerden alle bei 100 % enden.</para>
        ///
        /// <para><b>Stunden- und Viertelstundenraster</b> ergeben sich aus der Laenge der
        /// Reihen; gezeichnet wird ueber die eigene Laenge, nicht ueber 8 760.</para>
        /// </summary>
        /// <param name="titel">Ueberschrift.</param>
        /// <param name="reihen">Ein bis vier Reihen; leere und ungueltige entfallen.</param>
        /// <param name="yTitel">Beschriftung der y-Achse (der Prozentbezug).</param>
        /// <param name="achse">Monatsgrenzen oder Jahresstunden.</param>
        /// <param name="sortiert">Dauerlinie statt Ganglinie — jede Reihe FUER SICH
        /// absteigend sortiert (dieselbe Regel wie <see cref="Ganglinie.Dauerlinie"/>).</param>
        public static byte[] GanglinieNormiert(string titel, IReadOnlyList<Reihe> reihen,
                                               string yTitel, Achse achse, bool sortiert)
        {
            int W = 1240, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel ?? "", W);
                var rc = SKRect.Create(100f, 110f, W - 140f, 360f);

                List<Reihe> gueltig = Brauchbare(reihen);
                if (gueltig.Count == 0)
                {
                    Leerhinweis(g, rc);
                    return Png(flaeche);
                }

                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        100f, 66f, W - 30f);

                // Der gemeinsame Bezugswert (siehe Kopf).
                double bezug = gueltig.Max(r => r.Werte.Max());
                if (bezug <= 0) bezug = 1;

                ProzentRaster(g, rc);
                XAchse(g, rc, achse, gueltig[0].Werte.Length);
                using (var f = Schrift(15f))
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);

                foreach (Reihe r in gueltig)
                {
                    double[] werte = sortiert ? AbsteigendKopie(r.Werte) : r.Werte;
                    ZeichneLinie(g, rc, Normiert(werte, bezug), 0, Y_PROZENT_MAX,
                                 r.Farbe, r.Breite > 0 ? r.Breite : 2f);
                }

                return Png(flaeche);
            }
        }

        /// <summary>Obergrenze der Prozentachse — woertlich aus <c>init_Chart</c> :3378.</summary>
        private const double Y_PROZENT_MAX = 100.2;

        // ------------------------------------------------------------------ B2 / B3

        /// <summary>
        /// <b>B2 — ERZEUGERSTAPEL.</b> Das Arbeitspferd der Welle: gestapelte Erzeugung,
        /// Linien darueber, eine Konturlinie darunter — und wahlweise eine Reihe auf
        /// einer zweiten y-Achse (B3). Es traegt SECHS der siebzehn Zeichenflaechen
        /// (<c>chart3</c>, <c>chart_Kessel</c>, <c>chart8</c>, <c>chart_BHKW_Waerme</c>,
        /// <c>chart_Waerme</c>, <c>chart7</c>).
        ///
        /// <para><b>Die Zeichenlage ist Fachaussage, nicht Geschmack</b> (Begruendung in
        /// <c>NavigatorWaerme.SerienAufbauen</c> :587-635):</para>
        /// <list type="number">
        ///   <item>Die KONTUR („Gesamt") liegt UNTER dem Stapel — sie ist die Summe und
        ///   darf ihn nicht ueberdecken.</item>
        ///   <item>Der STAPEL in Kaskadenreihenfolge, von unten nach oben.</item>
        ///   <item>Die LINIEN darueber, in ihrer Listenreihenfolge — die letzte liegt
        ///   ganz oben (im Bestand ist das der Waermebedarf).</item>
        /// </list>
        ///
        /// <para><b>Sortiert wird NICHT gestapelt</b> (<c>GanglinienDarstellung.Stapeltyp</c>):
        /// In der Dauerlinie ist jede Reihe fuer sich sortiert, eine Summe daraus waere
        /// frei erfunden. Der Stapel wird dann zu Linien, im Bestand mit
        /// <c>BorderWidth 4</c> — deshalb die dickere Strichstaerke.</para>
        ///
        /// <para><b>Zwei Stapelgruppen.</b> <see cref="Reihe.Stapelgruppe"/> trennt sie
        /// wie <c>StackedGroupName</c> im Vorlaeufer: Auf der Waermepumpenseite steht der
        /// BEDARF als Flaeche und die PRODUKTION als Saeule im selben Bild. Beide Gruppen
        /// starten bei null und werden nebeneinander gezeichnet, die Saeulen etwas
        /// schmaler — so bleiben sie unterscheidbar.</para>
        /// </summary>
        /// <param name="titel">Ueberschrift.</param>
        /// <param name="stapel">Die gestapelten Reihen in Kaskadenreihenfolge.</param>
        /// <param name="linien">Linien ueber dem Stapel, in Zeichenreihenfolge.</param>
        /// <param name="kontur">Die Summenlinie unter dem Stapel; <c>null</c> = keine.</param>
        /// <param name="yTitel">Beschriftung der linken y-Achse.</param>
        /// <param name="achse">Monatsgrenzen oder Jahresstunden.</param>
        /// <param name="sortiert">Dauerlinie statt Ganglinie — dann ohne Stapel.</param>
        /// <param name="zweiteAchse">B3: eine Reihe mit eigener Skala rechts; <c>null</c> = keine.</param>
        /// <param name="y2Titel">Beschriftung der rechten y-Achse.</param>
        public static byte[] ErzeugerStapel(string titel, IReadOnlyList<Reihe> stapel,
                                            IReadOnlyList<Reihe> linien, Reihe kontur,
                                            string yTitel, Achse achse, bool sortiert,
                                            Reihe zweiteAchse = null, string y2Titel = null)
        {
            int W = 1240, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel ?? "", W);

                bool mitY2 = Brauchbar(zweiteAchse);
                var rc = SKRect.Create(100f, 110f, W - (mitY2 ? 190f : 140f), 360f);

                List<Reihe> stapelG = Brauchbare(stapel);
                List<Reihe> linienG = Brauchbare(linien);
                bool mitKontur = Brauchbar(kontur);

                if (stapelG.Count == 0 && linienG.Count == 0 && !mitKontur)
                {
                    Leerhinweis(g, rc);
                    return Png(flaeche);
                }

                // Legende: Kontur zuerst (sie steht im Bestand als erste Serie), dann der
                // Stapel, dann die Linien, zuletzt die Reihe der zweiten Achse.
                var leg = new List<Segment>();
                if (mitKontur) leg.Add(new Segment(kontur.Name, 0, kontur.Farbe));
                leg.AddRange(stapelG.Select(r => new Segment(r.Name, 0, r.Farbe)));
                leg.AddRange(linienG.Select(r => new Segment(r.Name, 0, r.Farbe)));
                if (mitY2) leg.Add(new Segment(zweiteAchse.Name, 0, zweiteAchse.Farbe));
                Legende(g, leg, 100f, 66f, W - 30f);

                int n = stapelG.Count > 0 ? stapelG[0].Werte.Length
                      : linienG.Count > 0 ? linienG[0].Werte.Length
                      : kontur.Werte.Length;

                // Obergrenze: die hoechste Stapelsumme JE GRUPPE (die Gruppen stehen
                // nebeneinander, nicht uebereinander), dazu Linien und Kontur.
                double max = 0;
                if (!sortiert)
                {
                    max = Math.Max(max, Stapelhoehe(stapelG, Stapelart.Flaeche, n));
                    max = Math.Max(max, Stapelhoehe(stapelG, Stapelart.Saeule, n));
                }
                else
                    foreach (Reihe r in stapelG) max = Math.Max(max, r.Werte.Max());
                foreach (Reihe r in linienG) max = Math.Max(max, r.Werte.Max());
                if (mitKontur) max = Math.Max(max, kontur.Werte.Max());
                max = Nice(max);
                if (max <= 0) max = 1;

                YRaster(g, rc, max);
                XAchse(g, rc, achse, n);
                using (var f = Schrift(15f))
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);

                // (1) Kontur UNTER dem Stapel.
                if (mitKontur)
                    ZeichneLinie(g, rc, sortiert ? AbsteigendKopie(kontur.Werte) : kontur.Werte,
                                 0, max, kontur.Farbe, kontur.Breite > 0 ? kontur.Breite : 4f);

                // (2) Der Stapel.
                if (sortiert)
                {
                    // Ohne Stapel: jede Reihe fuer sich als Dauerlinie, BorderWidth 4.
                    foreach (Reihe r in stapelG)
                        ZeichneLinie(g, rc, AbsteigendKopie(r.Werte), 0, max, r.Farbe,
                                     r.Breite > 0 ? r.Breite : 4f);
                }
                else
                {
                    bool zweiGruppen = stapelG.Any(r => r.Stapelgruppe == Stapelart.Flaeche) &&
                                       stapelG.Any(r => r.Stapelgruppe == Stapelart.Saeule);

                    StapelZeichnen(g, rc, stapelG, Stapelart.Flaeche, n, max,
                                   zweiGruppen ? -0.22f : 0f, zweiGruppen ? 0.5f : 1f);
                    StapelZeichnen(g, rc, stapelG, Stapelart.Saeule, n, max,
                                   zweiGruppen ? 0.22f : 0f, zweiGruppen ? 0.5f : 1f);
                    // Reihen ohne ausdrueckliche Gruppe bilden den gemeinsamen Stapel.
                    StapelZeichnen(g, rc, stapelG, Stapelart.Keine, n, max, 0f, 1f);
                }

                // (3) Die Linien darueber, in Zeichenreihenfolge.
                foreach (Reihe r in linienG)
                    ZeichneLinie(g, rc, sortiert ? AbsteigendKopie(r.Werte) : r.Werte,
                                 0, max, r.Farbe, r.Breite > 0 ? r.Breite : 2.5f);

                // (4) B3 — die zweite y-Achse mit EIGENER Skala.
                if (mitY2)
                {
                    double[] w2 = sortiert ? AbsteigendKopie(zweiteAchse.Werte) : zweiteAchse.Werte;
                    double max2 = Nice(w2.Max());
                    if (max2 <= 0) max2 = 1;

                    ZeichneLinie(g, rc, w2, 0, max2, zweiteAchse.Farbe,
                                 zweiteAchse.Breite > 0 ? zweiteAchse.Breite : 2f);

                    using (var achsenstift = Strich(zweiteAchse.Farbe, 2f))
                        g.DrawLine(rc.Right, rc.Top, rc.Right, rc.Bottom, achsenstift);
                    using (var f = Schrift(15f))
                    {
                        for (int i = 0; i <= 4; i++)
                        {
                            double wert = max2 * i / 4.0;
                            float y = (float)(rc.Bottom - wert / max2 * rc.Height);
                            Text(g, wert.ToString("N0", DE), f, zweiteAchse.Farbe,
                                 rc.Right + 8f, y - TextHoehe(f) / 2f);
                        }
                        Text(g, y2Titel ?? "", f, zweiteAchse.Farbe, rc.Right - 40f, rc.Top - 24f);
                    }
                }

                return Png(flaeche);
            }
        }

        /// <summary>Die hoechste Summe EINER Stapelgruppe ueber alle Stuetzstellen.</summary>
        private static double Stapelhoehe(List<Reihe> stapel, Stapelart gruppe, int n)
        {
            var summe = new double[n];
            foreach (Reihe r in stapel)
            {
                if (r.Stapelgruppe != gruppe) continue;
                for (int i = 0; i < n && i < r.Werte.Length; i++) summe[i] += Math.Max(r.Werte[i], 0);
            }
            return n > 0 ? summe.Max() : 0;
        }

        /// <summary>
        /// Zeichnet EINE Stapelgruppe als kumulierte Flaechen.
        /// <paramref name="versatz"/> und <paramref name="breite"/> in Anteilen der
        /// Zeichenflaeche verschieben und schmaelern die Gruppe, damit zwei Gruppen
        /// nebeneinander stehen koennen.
        /// </summary>
        private static void StapelZeichnen(SKCanvas g, SKRect rc, List<Reihe> stapel,
                                           Stapelart gruppe, int n, double max,
                                           float versatz, float breite)
        {
            var teil = stapel.Where(r => r.Stapelgruppe == gruppe).ToList();
            if (teil.Count == 0) return;

            SKRect ziel = breite >= 1f
                ? rc
                : SKRect.Create(rc.Left + rc.Width * (0.5f + versatz - breite / 2f),
                                rc.Top, rc.Width * breite, rc.Height);

            var unten = new double[n];
            foreach (Reihe r in teil)
            {
                var oben = new double[n];
                for (int i = 0; i < n; i++)
                    oben[i] = unten[i] + (i < r.Werte.Length ? Math.Max(r.Werte[i], 0) : 0);
                ZeichneFlaeche(g, ziel, unten, oben, max, r.Farbe);
                unten = oben;
            }
        }

        // ------------------------------------------------------------------ B4

        /// <summary>
        /// <b>B4 — STREUWOLKE.</b> Eine bis drei halbtransparente Punktreihen ueber einer
        /// freien x-Groesse (iU9-W11a.6).
        ///
        /// <para><b>Vorbild.</b> <c>chart4</c> „Leistung ueber Aussentemperatur" der
        /// Waermepumpenseite: drei Reihen (Waermebedarf, Heizstab, Waermeproduktion) als
        /// XY-Punkte in <c>ARGB(120, …)</c>, x = Aussentemperatur, y = Leistung.</para>
        ///
        /// <para><b>Halbtransparent ist wesentlich.</b> 8 760 Punkte auf 1 000 Bildpunkten
        /// liegen vielfach uebereinander; erst die Transparenz macht sichtbar, WO sich
        /// die Wolke verdichtet. Der Aufrufer liefert die Farbe samt Alphawert — dieselbe
        /// Entscheidung wie im Vorlaeufer.</para>
        ///
        /// <para><b>Die x-Achse kann ins Negative reichen</b> (Aussentemperatur) und
        /// bekommt deshalb eine vorzeichenfaehige Skala; y beginnt bei null.</para>
        /// </summary>
        public static byte[] Streuwolke(string titel, string xTitel, string yTitel,
                                        IReadOnlyList<Punktreihe> reihen)
        {
            int W = 1240, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel ?? "", W);
                var rc = SKRect.Create(100f, 110f, W - 140f, 360f);

                var gueltig = (reihen ?? new List<Punktreihe>())
                    .Where(r => r != null && r.Punkte != null && r.Punkte.Count > 0)
                    .ToList();
                if (gueltig.Count == 0)
                {
                    Leerhinweis(g, rc);
                    return Png(flaeche);
                }

                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, Undurchsichtig(r.Farbe))).ToList(),
                        100f, 66f, W - 30f);

                double xMin = gueltig.Min(r => r.Punkte.Min(p => p.X));
                double xMax = gueltig.Max(r => r.Punkte.Max(p => p.X));
                if (xMax - xMin < 1e-9) { xMax = xMin + 1; }
                double yMax = Nice(Math.Max(0, gueltig.Max(r => r.Punkte.Max(p => p.Y))));
                if (yMax <= 0) yMax = 1;

                YRaster(g, rc, yMax);

                // x-Skala mit fuenf Marken ueber den vorkommenden Bereich.
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int i = 0; i <= 4; i++)
                    {
                        double wert = xMin + (xMax - xMin) * i / 4.0;
                        float x = rc.Left + (float)i / 4f * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = wert.ToString("0.#", DE);
                        Text(g, lab, f, SKColors.DimGray, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
                using (var f = Schrift(15f))
                {
                    Text(g, xTitel ?? "", f, SKColors.DimGray, rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 30f);
                    Text(g, yTitel ?? "", f, SKColors.DimGray, rc.Left, rc.Top - 24f);
                }

                foreach (Punktreihe r in gueltig)
                    using (var punkt = Fuellung(r.Farbe))
                        foreach (var p in r.Punkte)
                        {
                            if (double.IsNaN(p.X) || double.IsNaN(p.Y)) continue;
                            float x = rc.Left + (float)((p.X - xMin) / (xMax - xMin)) * rc.Width;
                            float y = (float)(rc.Bottom - Math.Max(0, p.Y) / yMax * rc.Height);
                            if (y < rc.Top) y = rc.Top;
                            g.DrawCircle(x, y, 2.5f, punkt);
                        }

                return Png(flaeche);
            }
        }

        /// <summary>Dieselbe Farbe ohne Alphawert — fuer das Legendenkaestchen.</summary>
        private static SKColor Undurchsichtig(SKColor f)
        {
            return new SKColor(f.Red, f.Green, f.Blue);
        }

        // ------------------------------------------------------------------ B5

        /// <summary>
        /// <b>B5 — RING.</b> Ein Kuchen mit Innenloch, der Kennzahl in der Mitte und
        /// einer Legende, die NUR die vorhandenen Segmente nennt (iU9-W11a.6).
        ///
        /// <para><b>Vorbild.</b> Die beiden GDI-Donuts der <c>NavigatorUebersicht</c>
        /// (Waerme- und Stromdeckung, <c>DonutChartDrawer.DrawChartWithDynamicLegend</c>).
        /// Sie zeichneten Werte, Namen und Farben aus DREI parallelen Listen und mussten
        /// alle drei gemeinsam filtern, sonst verrutschte die Farbzuordnung
        /// (<c>NavigatorUebersicht</c> :304-306). Ein Segment traegt hier alles
        /// zusammen — die Falle gibt es nicht mehr.</para>
        ///
        /// <para><b>Dynamisch heisst: Segmente mit Wert &lt;= 0 entfallen samt Legende.</b>
        /// Ein Projekt ohne BHKW soll kein BHKW-Segment der Groesse null zeigen.</para>
        /// </summary>
        /// <param name="titel">Ueberschrift, z. B. „Waermebedarfsdeckung".</param>
        /// <param name="segmente">Die Segmente in Zeichenreihenfolge.</param>
        /// <param name="mitteWert">Die Zahl in der Mitte (im Vorlaeufer der Deckungsgrad).</param>
        /// <param name="mitteEinheit">Ihre Einheit, z. B. „%".</param>
        public static byte[] Ring(string titel, IReadOnlyList<Ringsegment> segmente,
                                  double mitteWert, string mitteEinheit)
        {
            int W = 720, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel ?? "", W);

                var gueltig = (segmente ?? new List<Ringsegment>())
                    .Where(s => s != null && s.Wert > 0 && !double.IsNaN(s.Wert) && !double.IsInfinity(s.Wert))
                    .ToList();

                var rc = SKRect.Create(210f, 90f, 300f, 300f);

                if (gueltig.Count == 0)
                {
                    Leerhinweis(g, SKRect.Create(60f, 100f, W - 120f, 100f));
                    return Png(flaeche);
                }

                double summe = gueltig.Sum(s => s.Wert);
                float start = -90f;   // 12 Uhr, wie im Vorlaeufer
                foreach (Ringsegment s in gueltig)
                {
                    float winkel = (float)(s.Wert / summe * 360.0);
                    using (var b = Fuellung(s.Farbe)) Kreissegment(g, rc, start, winkel, b);
                    start += winkel;
                }

                // Das Innenloch: ein weisser Kreis auf demselben Mittelpunkt. Genau so
                // machte es DonutChartDrawer.
                using (var loch = Fuellung(SKColors.White))
                    g.DrawCircle(rc.MidX, rc.MidY, rc.Width * 0.30f, loch);

                string mitte = mitteWert.ToString("N1", DE) + (string.IsNullOrEmpty(mitteEinheit)
                                                                   ? "" : " " + mitteEinheit);
                using (var f = Schrift(26f, fett: true))
                    Text(g, mitte, f, C_STAMM, rc.MidX - f.MeasureText(mitte) / 2f,
                         rc.MidY - TextHoehe(f) / 2f);

                Legende(g, gueltig.Select(s => new Segment(s.Name, 0, s.Farbe)).ToList(),
                        60f, 430f, W - 30f);

                return Png(flaeche);
            }
        }

        // ------------------------------------------------------------------ B6

        /// <summary>
        /// <b>B6 — MONATSSTAPEL.</b> Zwoelf gestapelte Monatssaeulen mit FREIER
        /// Reihenliste (iU9-W11a.6).
        ///
        /// <para><b>Vorbild.</b> <c>chartSolar</c> des Dashboards: drei
        /// <c>StackedColumn</c>-Reihen (Direktverbrauch Gold, Speichernutzung LightGreen,
        /// Netzbezug Rot), Legende OBEN und ZENTRIERT, y ab null.</para>
        ///
        /// <para><b>Was ihn von <see cref="StrombilanzMonate"/> unterscheidet:</b> Der
        /// Berichtsbruder nimmt einen <c>ZeitreihenSatz</c>, kennt seine vier Reihen
        /// namentlich und behandelt „Einspeisung" als Nebenbalken. Hier kommt die
        /// Reihenliste vom Aufrufer, und gestapelt wird alles.</para>
        /// </summary>
        /// <param name="titel">Ueberschrift.</param>
        /// <param name="einheit">Einheit fuer die Ueberschrift; leer = ohne.</param>
        /// <param name="reihen">Reihen mit je zwoelf Monatswerten, von unten nach oben.</param>
        public static byte[] MonatsStapel(string titel, string einheit, IReadOnlyList<Reihe> reihen)
        {
            int W = 978, H = 542;
            string[] monate = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun",
                                "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, string.IsNullOrEmpty(einheit) ? (titel ?? "")
                                                       : (titel ?? "") + "  [" + einheit + "]", W);

                var gueltig = (reihen ?? new List<Reihe>())
                    .Where(r => r != null && r.Werte != null && r.Werte.Length >= 12)
                    .ToList();

                var rc = SKRect.Create(100f, 120f, W - 140f, 320f);
                if (gueltig.Count == 0)
                {
                    Leerhinweis(g, rc);
                    return Png(flaeche);
                }

                // Legende OBEN und ZENTRIERT (Docking.Top, StringAlignment.Center).
                float legendenbreite = Legendenbreite(gueltig);
                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        Math.Max(20f, (W - legendenbreite) / 2f), 68f, W - 20f);

                var summe = new double[12];
                for (int m = 0; m < 12; m++)
                    foreach (Reihe r in gueltig) summe[m] += Math.Max(r.Werte[m], 0);
                double max = Nice(summe.Max());
                if (max <= 0) max = 1;

                YRaster(g, rc, max);
                using (var f = Schrift(15f))
                    for (int m = 0; m < 12; m++)
                    {
                        float x = rc.Left + (m + 0.5f) * rc.Width / 12f;
                        Text(g, monate[m], f, SKColors.DimGray,
                             x - f.MeasureText(monate[m]) / 2f, rc.Bottom + 8f);
                    }

                float slot = rc.Width / 12f;
                float balken = slot * 0.6f;
                for (int m = 0; m < 12; m++)
                {
                    float x0 = rc.Left + m * slot + (slot - balken) / 2f;
                    float unten = rc.Bottom;
                    foreach (Reihe r in gueltig)
                    {
                        float hoehe = (float)(Math.Max(r.Werte[m], 0) / max * rc.Height);
                        if (hoehe <= 0) continue;
                        using (var br = Fuellung(r.Farbe))
                            g.DrawRect(x0, unten - hoehe, balken, hoehe, br);
                        unten -= hoehe;
                    }
                }

                return Png(flaeche);
            }
        }

        /// <summary>Breite, die die Legende dieser Reihen braucht — fuer die Zentrierung.</summary>
        private static float Legendenbreite(List<Reihe> reihen)
        {
            float breite = 0;
            using (var f = Schrift(16f))
                foreach (Reihe r in reihen) breite += 40f + f.MeasureText(r.Name ?? "") + 24f;
            return breite;
        }

        // ------------------------------------------------------------------ B7

        /// <summary>
        /// <b>B7 — TEMPERATURVERLAUF.</b> n Linien mit FREIER Reihenliste, die untere
        /// Schicht je Speicher gestrichelt, und eine y-Achse OHNE Nullpunkt
        /// (iU9-W11a.6).
        ///
        /// <para><b>Vorbild.</b> <c>chart_Speichertemperatur</c> der Waermepumpenseite:
        /// je Senkenspeicher zwei Reihen (oben/unten, die untere
        /// <c>ChartDashStyle.Dash</c>), je temperaturgekoppeltem Erzeuger eine
        /// Quelltemperatur, y-Achse aus Min/Max ueber alle Reihen mit einer
        /// MINDESTSPANNE von 5 K.</para>
        ///
        /// <para><b>Was ihn von <see cref="Speichertemperaturen"/> unterscheidet:</b> Der
        /// Berichtsbruder nimmt einen <c>ZeitreihenSatz</c> und zeigt drei
        /// charakteristische Wochen in drei Feldern. Hier kommt die Reihenliste vom
        /// Aufrufer, und gezeichnet wird das ganze Jahr in einem Feld.</para>
        ///
        /// <para><b>Die Mindestspanne 5 K</b> ist woertlich uebernommen (:2607-2620):
        /// Ohne sie spreizt ein Speicher, der das ganze Jahr auf 60 °C steht, den
        /// Rundungsfehler seiner letzten Nachkommastelle ueber die volle Bildhoehe.</para>
        /// </summary>
        /// <param name="titel">Ueberschrift.</param>
        /// <param name="reihen">Die Temperaturreihen; <see cref="Reihe.Gestrichelt"/>
        /// kennzeichnet die untere Schicht.</param>
        /// <param name="minAuto">
        /// <c>true</c> = die Achse beginnt beim kleinsten vorkommenden Wert (der Regelfall
        /// des Vorlaeufers). <c>false</c> = sie beginnt bei null.
        /// </param>
        public static byte[] Temperaturverlauf(string titel, IReadOnlyList<Reihe> reihen, bool minAuto)
        {
            int W = 1240, H = 560;
            using (var flaeche = Start(W, H))
            {
                SKCanvas g = flaeche.Canvas;
                Titel(g, titel ?? "", W);
                var rc = SKRect.Create(100f, 110f, W - 140f, 360f);

                List<Reihe> gueltig = Brauchbare(reihen);
                if (gueltig.Count == 0)
                {
                    Leerhinweis(g, rc);
                    return Png(flaeche);
                }

                Legende(g, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                        100f, 66f, W - 30f);

                double min = minAuto ? gueltig.Min(r => r.Werte.Min()) : 0;
                double max = gueltig.Max(r => r.Werte.Max());

                // MINDESTSPANNE 5 K, woertlich aus SpeichertemperaturAnzeigen :2607-2620.
                if (max - min < TEMPERATUR_MINDESTSPANNE)
                {
                    double mitte = (max + min) / 2.0;
                    min = mitte - TEMPERATUR_MINDESTSPANNE / 2.0;
                    max = mitte + TEMPERATUR_MINDESTSPANNE / 2.0;
                }
                min = Math.Floor(min);
                max = Math.Ceiling(max);

                // Raster und y-Beschriftung ueber die vorzeichenfaehige Spanne.
                using (var raster = Strich(SKColors.Gainsboro, 1f))
                using (var f = Schrift(15f))
                    for (int i = 0; i <= 5; i++)
                    {
                        double wert = min + (max - min) * i / 5.0;
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        g.DrawLine(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("N0", DE);
                        Text(g, lab, f, SKColors.DimGray, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
                using (var achse = Strich(SKColors.DimGray, 2f))
                {
                    g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                    g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
                }
                XAchse(g, rc, Achse.Jahresstunden, gueltig[0].Werte.Length);

                foreach (Reihe r in gueltig)
                {
                    int schrittweite = Math.Max(1, r.Werte.Length / (int)rc.Width);
                    var punkte = new List<SKPoint>();
                    for (int i = 0; i < r.Werte.Length; i += schrittweite)
                    {
                        float x = rc.Left + (float)i / (r.Werte.Length - 1) * rc.Width;
                        float y = (float)(rc.Bottom - (r.Werte[i] - min) / (max - min) * rc.Height);
                        punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
                    }
                    using (var strichel = r.Gestrichelt
                               ? SKPathEffect.CreateDash(new[] { 8f, 5f }, 0f) : null)
                    using (var stift = Strich(r.Farbe, r.Breite > 0 ? r.Breite : 2f))
                    {
                        stift.StrokeJoin = SKStrokeJoin.Round;
                        if (strichel != null) stift.PathEffect = strichel;
                        Linienzug(g, punkte.ToArray(), stift);
                    }
                }

                return Png(flaeche);
            }
        }

        /// <summary>Mindestspanne der Temperaturachse [K] — woertlich aus dem Vorlaeufer.</summary>
        private const double TEMPERATUR_MINDESTSPANNE = 5.0;

        // ------------------------------------------------------- geteilte Helfer

        /// <summary>Die Reihen, die etwas zu zeichnen haben.</summary>
        private static List<Reihe> Brauchbare(IReadOnlyList<Reihe> reihen)
        {
            return (reihen ?? new List<Reihe>()).Where(Brauchbar).ToList();
        }

        private static bool Brauchbar(Reihe r)
        {
            return r != null && r.Werte != null && r.Werte.Length >= 2 &&
                   r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w));
        }

        private static void Leerhinweis(SKCanvas g, SKRect rc)
        {
            using (var f = Schrift(18f))
                Text(g, BerichtTexte.T("Keine Simulationsdaten vorhanden."), f, SKColors.DimGray,
                     rc.Left, rc.Top + 20f);
        }

        /// <summary>Eine absteigend sortierte KOPIE — dieselbe Regel wie <see cref="Ganglinie.Dauerlinie"/>.</summary>
        private static double[] AbsteigendKopie(double[] werte)
        {
            var kopie = (double[])werte.Clone();
            Array.Sort(kopie);
            Array.Reverse(kopie);
            return kopie;
        }

        /// <summary>Die Werte in Prozent des Bezugswerts.</summary>
        private static double[] Normiert(double[] werte, double bezug)
        {
            var r = new double[werte.Length];
            for (int i = 0; i < werte.Length; i++) r[i] = werte[i] / bezug * 100.0;
            return r;
        }

        /// <summary>Raster und y-Beschriftung einer Prozentachse 0…100,2.</summary>
        private static void ProzentRaster(SKCanvas g, SKRect rc)
        {
            using (var raster = Strich(SKColors.Gainsboro, 1f))
            using (var f = Schrift(15f))
                for (int p = 0; p <= 100; p += 20)
                {
                    float y = (float)(rc.Bottom - p / Y_PROZENT_MAX * rc.Height);
                    g.DrawLine(rc.Left, y, rc.Right, y, raster);
                    string lab = p.ToString(DE) + " %";
                    Text(g, lab, f, SKColors.DimGray, rc.Left - f.MeasureText(lab) - 6f,
                         y - TextHoehe(f) / 2f);
                }
            using (var achse = Strich(SKColors.DimGray, 2f))
            {
                g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
            }
        }

        /// <summary>Raster, y-Beschriftung und Achsen einer Skala 0…max mit fuenf Stufen.</summary>
        private static void YRaster(SKCanvas g, SKRect rc, double max)
        {
            using (var raster = Strich(SKColors.Gainsboro, 1f))
            using (var f = Schrift(15f))
                for (int i = 0; i <= 5; i++)
                {
                    double wert = max * i / 5.0;
                    float y = (float)(rc.Bottom - wert / max * rc.Height);
                    g.DrawLine(rc.Left, y, rc.Right, y, raster);
                    string lab = wert.ToString(max >= 10 ? "N0" : "N1", DE);
                    Text(g, lab, f, SKColors.DimGray, rc.Left - f.MeasureText(lab) - 6f,
                         y - TextHoehe(f) / 2f);
                }
            using (var achse = Strich(SKColors.DimGray, 2f))
            {
                g.DrawLine(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
                g.DrawLine(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
            }
        }

        /// <summary>
        /// Die x-Achse: entweder Monatsgrenzen 0…12 (<c>ConfigureXAxisWithMonths</c>)
        /// oder die vier Stundenmarken 2000/4000/6000/8000
        /// (<c>ConfigureXAxisWithHours</c>). <paramref name="n"/> ist die Laenge der
        /// Reihen und traegt damit die Unterscheidung Stunden/Viertelstunden.
        /// </summary>
        private static void XAchse(SKCanvas g, SKRect rc, Achse achse, int n)
        {
            using (var raster = Strich(SKColors.Gainsboro, 1f))
            using (var f = Schrift(15f))
            {
                if (achse == Achse.Monate)
                {
                    for (int m = 0; m <= 12; m++)
                    {
                        float x = rc.Left + m / 12f * rc.Width;
                        g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                        string lab = m.ToString(DE);
                        Text(g, lab, f, SKColors.DimGray, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
                    return;
                }

                // Jahresstunden: die vier Marken des Vorlaeufers, auf die Reihenlaenge
                // bezogen - damit stimmt das Bild auch im Viertelstundenraster.
                int[] stunden = { 2000, 4000, 6000, 8000 };
                double stundenJeWert = n > Kanalsatz.STUNDEN_JAHR ? 0.25 : 1.0;
                foreach (int h in stunden)
                {
                    double index = h / stundenJeWert;
                    if (index >= n) continue;
                    float x = rc.Left + (float)(index / (n - 1)) * rc.Width;
                    g.DrawLine(x, rc.Top, x, rc.Bottom, raster);
                    string lab = h.ToString("N0", DE);
                    Text(g, lab, f, SKColors.DimGray, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                }
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
