using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Off-Screen-Diagramm-Rendering für den Bericht (Konzept Kap. 6) auf Basis
    /// SkiaSharp — bewusst ohne UI-Handle und ohne Fremd-API, damit das Rendering
    /// im Hintergrund-Thread deterministisch läuft (gleiches Muster wie die
    /// Kuchendiagramme des Bestandsberichts). Alle Methoden liefern PNG-Bytes;
    /// gerendert wird in doppelter Zielauflösung (Einbettung skaliert herunter).
    ///
    /// <para><b>Etappe E1 — der Renderer zeichnet nicht mehr, er BESCHREIBT.</b> Jede
    /// der 26 Zeichenmethoden fuellt ein <see cref="Zeichenmodell"/> — eine Liste
    /// unveraenderlicher Befehle mit fertigen Koordinaten und einem
    /// <see cref="Farbton"/> je Farbe — und gibt es an <see cref="SkiaMaler.Png"/>.
    /// Hier steht deshalb KEIN Zeichenaufruf einer Grafikbibliothek mehr: keine
    /// Leinwand, kein Pinsel, kein Pfad. Was bleibt, ist das Layout — Bildmasse,
    /// Achsen, Skalen — und die TEXTVERMESSUNG (<see cref="Schriftmass"/>), die das
    /// Layout VOR dem Befehl braucht. Denselben Modellbaum kann ein zweiter
    /// Ausgabeweg (SVG, Etappe E2) lesen.</para>
    ///
    /// <para>Die oeffentliche Flaeche fuehrt weiter <see cref="SKColor"/>, <c>SKRect</c>
    /// und <c>SKPoint</c> als reine WERTTYPEN fuer Farbe, Rechteck und Punkt — sie
    /// zeichnen nichts. Ihr Ersatz durch die Modelltypen <c>Farbe</c>,
    /// <c>Rahmen</c> und <c>Punkt</c> beruehrt jede Huelle und gehoert deshalb in
    /// eine eigene Etappe.</para>
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
            new SKColor(0xC7, 0x15, 0x85),   // MediumVioletRed
            new SKColor(0x94, 0x00, 0xD3),   // DarkViolet
            new SKColor(0x00, 0x80, 0x80),   // Teal
            new SKColor(0x8B, 0x45, 0x13),   // SaddleBrown
            new SKColor(0x2F, 0x4F, 0x4F),   // DarkSlateGray
            new SKColor(0xDC, 0x14, 0x3C)    // Crimson
        };

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        public class Segment
        {
            public string Label; public double Wert; public SKColor Farbe;

            /// <summary>
            /// AUFTRAG U18 — die STRICHART der Linie, zu der der Eintrag gehört. Zu einer
            /// gestrichelten oder gepunkteten Linie wird das Farbfeld nicht gefüllt, sondern
            /// in derselben Strichart umrandet (ETAPPE E6: seit der dritten Strichart eine
            /// Aufzählung statt eines Schalters).
            ///
            /// <para><b>Vorgabe <see cref="Strichart.Durchgezogen"/></b>, damit jedes Bild,
            /// das das Merkmal nicht setzt, byte-gleich bleibt — die ChartProben
            /// vergleichen Bilder.</para>
            /// </summary>
            public Strichart Strichart;

            public Segment(string l, double w, SKColor f) { Label = l; Wert = w; Farbe = f; }

            public Segment(string l, double w, SKColor f, Strichart strichart)
            { Label = l; Wert = w; Farbe = f; Strichart = strichart; }
        }

        /// <summary>
        /// ETAPPE E6 (Konzept § 2.13 (5)) — die <b>Strichart</b> einer Linie. Bis E6 trug
        /// eine Reihe nur einen Schalter „gestrichelt"; der Verlauf mit allen drei
        /// Szenarien braucht eine DRITTE Art (Farbe = Variante, Strichart = Szenario).
        ///
        /// <para><b>Wertgleich zum Schalter:</b> <see cref="Durchgezogen"/> ist der alte
        /// Wert <c>false</c>, <see cref="Gestrichelt"/> der alte Wert <c>true</c> — mit
        /// derselben Strichfolge 8/5. Jedes Bild, das die dritte Art nicht setzt, bleibt
        /// damit byte-gleich.</para>
        /// </summary>
        public enum Strichart
        {
            /// <summary>Durchgezogen — die Vorgabe.</summary>
            Durchgezogen = 0,
            /// <summary>Gestrichelt, Strichfolge 8/5 (der alte Schalter <c>true</c>).</summary>
            Gestrichelt = 1,
            /// <summary>Gepunktet, Strichfolge <see cref="PUNKT_STRICH"/>/<see cref="PUNKT_LUECKE"/> (ETAPPE E6).</summary>
            Gepunktet = 2
        }

        /// <summary>Strichlänge der gepunkteten Linie [px] — kurz wie ein Punkt.</summary>
        public const float PUNKT_STRICH = 2.5f;

        /// <summary>Lücke der gepunkteten Linie [px].</summary>
        public const float PUNKT_LUECKE = 3.5f;

        /// <summary>
        /// ETAPPE E6 — die Strichfolge einer <see cref="Strichart"/>: <c>null</c> für
        /// durchgezogen, 8/5 für gestrichelt (dieselbe Folge wie der alte Schalter),
        /// <see cref="PUNKT_STRICH"/>/<see cref="PUNKT_LUECKE"/> für gepunktet.
        ///
        /// <para><b>Stumpfe Kappe auch für die Punkte:</b> Eine <see cref="Datenreihe"/>
        /// führt keine Kappe; mit einer runden Kappe im PNG zeichnete das SVG der Oberfläche
        /// eine andere Linie als das Bild des Berichts.</para>
        /// </summary>
        public static Strichmuster Strichfolge(Strichart art)
        {
            switch (art)
            {
                case Strichart.Gestrichelt: return new Strichmuster(8f, 5f);
                case Strichart.Gepunktet: return new Strichmuster(PUNKT_STRICH, PUNKT_LUECKE);
                default: return null;
            }
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
            /// Der FARBTON der Reihe (DG-E5, Anwenderentscheid DG-Q8): die
            /// <see cref="Farbrolle"/>, unter der sie gemalt wird — wahlweise mit
            /// einer Abwandlung (andere Deckung, oder eine im Layout gerechnete
            /// Farbe samt Herkunftsrolle, <see cref="Farbpalette.Gerechnet"/>).
            ///
            /// <para><c>null</c> heißt: Die Reihe kam als reiner Farbwert herein;
            /// ihre Rolle sucht die Palette dann rückwärts. <b>Jede Reihe, die in
            /// einer Legende steht, trägt eine Rolle</b> — ohne sie bleibt ihr
            /// Legendeneintrag ohne Farbwähler.</para>
            /// </summary>
            public Farbton Ton;

            /// <summary>
            /// Die Farbrolle der Reihe; <see cref="Farbrolle.UNBENANNT"/>, solange
            /// sie keine trägt.
            /// </summary>
            public Farbrolle Rolle => Ton == null ? Farbrolle.UNBENANNT : Ton.Rolle;

            /// <summary>
            /// Zu welchem Stapel die Reihe gehoert (iU9-W11a.6). Der Vorlaeufer trennte
            /// zwei Stapel in EINEM Diagramm ueber <c>StackedGroupName</c> — auf der
            /// Waermepumpenseite „Bedarf" (Flaeche) und „Produktion" (Saeule). Nur
            /// <see cref="ErzeugerStapel"/> wertet das Feld aus.
            /// </summary>
            public Stapelart Stapelgruppe = Stapelart.Keine;

            /// <summary>
            /// Die STRICHART der Linie (iU9-W11a.6, ETAPPE E6). Im Bestand traegt die UNTERE
            /// Speicherschicht <c>ChartDashStyle.Dash</c> — zwei Temperaturen desselben
            /// Behaelters gehoeren zusammen und sollen sich trotzdem unterscheiden. Bis E6
            /// ein Schalter „gestrichelt"; seit dem Verlauf mit drei Szenarien eine
            /// Aufzählung mit der Vorgabe <see cref="Strichart.Durchgezogen"/> — der alte
            /// Wert <c>true</c> ist <see cref="Strichart.Gestrichelt"/>.
            /// </summary>
            public Strichart Strichart;

            /// <summary>
            /// Strichstaerke; <c>0</c> = die Vorgabe des jeweiligen Bildes. Im Bestand
            /// tragen die Dauerlinien <c>BorderWidth 4</c> und die Konturlinie „Gesamt"
            /// ebenfalls 4, die uebrigen 1 bis 2.
            /// </summary>
            public float Breite;

            /// <summary>
            /// <b>Lücken statt Unbrauchbarkeit</b> (Anlagenkopplung AK1 Welle 3, Bild „Vorlauf und
            /// Rücklauf"): Ein nicht endlicher Wert ist eine LÜCKE — die Linie bricht dort ab und setzt
            /// beim nächsten endlichen Wert neu an, im PNG wie im SVG. Ohne den Schalter (Vorgabe) macht
            /// ein einziger nicht endlicher Wert die Reihe unbrauchbar, wie bisher; jedes Bild des
            /// Bestands bleibt damit byte-gleich. Ein Vorlauf jenseits der Heizgrenze ist keine Zahl,
            /// sondern „kein Heizbetrieb" — ihn mit einer erfundenen Zahl zu füllen, wäre eine Aussage,
            /// die niemand getroffen hat. Nur der Verlauf (<see cref="VerlaufsbildModell"/>) wertet ihn aus.
            /// </summary>
            public bool Luecken;

            public Reihe(string n, double[] w, SKColor f) { Name = n; Werte = w; Farbe = f; }

            public Reihe(string n, double[] w, SKColor f, Stapelart gruppe,
                         Strichart strichart = Strichart.Durchgezogen, float breite = 0f)
            {
                Name = n; Werte = w; Farbe = f;
                Stapelgruppe = gruppe; Strichart = strichart; Breite = breite;
            }

            /// <summary>
            /// <b>Die Reihe mit ihrer FARBROLLE</b> (DG-E5) — die Schreibweise für
            /// jeden Aufrufer, der die Größe benennen kann. <see cref="Farbe"/>
            /// entsteht dabei aus <see cref="Farbpalette.Aktuell"/>, damit der
            /// PNG-Weg unverändert malt.
            /// </summary>
            public Reihe(string n, double[] w, Farbrolle rolle)
                : this(n, w, Farbton.Aus(rolle)) { }

            /// <summary>Dieselbe Reihe mit Stapelart, Strichfolge und Stärke.</summary>
            public Reihe(string n, double[] w, Farbrolle rolle, Stapelart gruppe,
                         Strichart strichart = Strichart.Durchgezogen, float breite = 0f)
                : this(n, w, Farbton.Aus(rolle), gruppe, strichart, breite) { }

            /// <summary>
            /// Die Reihe mit einem fertigen <see cref="Farbton"/> — der Weg für eine
            /// andere Deckung und für eine im Layout GERECHNETE Farbe, die ihre
            /// Herkunftsrolle mitführt.
            /// </summary>
            public Reihe(string n, double[] w, Farbton ton)
            {
                Name = n; Werte = w; Ton = ton;
                Farbe = Farbpalette.Aktuell.Loese(ton).Skiafarbe();
            }

            /// <summary>Derselbe Farbton mit Stapelart, Strichfolge und Stärke.</summary>
            public Reihe(string n, double[] w, Farbton ton, Stapelart gruppe,
                         Strichart strichart = Strichart.Durchgezogen, float breite = 0f)
                : this(n, w, ton)
            {
                Stapelgruppe = gruppe; Strichart = strichart; Breite = breite;
            }
        }

        // =================================================================== Kuchen

        /// <summary>Kuchendiagramm (Deckungsanteile) — Portierung aus dem Bestandsbericht.</summary>
        public static byte[] Kuchen(string titel, List<Segment> segmente)
            => SkiaMaler.Png(KuchenModell(titel, segmente));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c).
        ///
        /// <para><b>Ein reines Pixelbild</b> (Entscheid DG-E3-7): Es hat keine
        /// Zeichenfläche und keine <c>Datenreihe</c> — ein Kuchen trägt keine
        /// Zeitachse, auf der man zoomen könnte. Jedes Segment ist stattdessen ein
        /// Pixel-Element mit der Marke <c>reihe:&lt;Segmentname&gt;</c> und seinem
        /// Anteil als <c>Wert</c>; die Legendeneinträge tragen
        /// <c>legende:&lt;Segmentname&gt;</c>. Damit schaltet ein Klick in der Legende
        /// dasselbe Segment, auf das der Zeiger zeigt.</para>
        /// </summary>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 960 × 600 wie bisher. Ist das Maß
        /// kaum breiter als hoch, steht die Legende unter dem Kuchen statt daneben.</param>
        public static Zeichenmodell KuchenModell(string titel, List<Segment> segmente, Bildmass? mass = null)
        {
            int W = Bildmass.BreiteOder(mass, 960), H = Bildmass.HoeheOder(mass, 600);
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel, W, mass.HasValue));

            double total = segmente.Sum(s => Math.Max(s.Wert, 0));
            if (total <= 0) total = 1;

            // Der Kuchen: links, so groß wie die Höhe unter dem Titel und die halbe Breite es erlauben
            // (Vorgabe 440); die Legende rechts daneben. Im schmalen Zielmaß steht sie darunter.
            float d = Math.Min(H - 160f, (W - 80f) * 0.5f);
            float kx = 40f, lx = 40f + d + 60f, ly = 110f;
            if (mass.HasValue && W < H * 1.1f)
            {
                d = Math.Max(120f, Math.Min(W - 80f, H - 160f - segmente.Count * 48f));
                kx = (W - d) / 2f;
                lx = 40f;
                ly = 90f + d + 40f;
            }
            var rect = SKRect.Create(kx, 90f, d, d);
            float start = -90f;
            foreach (Segment s in segmente)
            {
                float sweep = (float)(Math.Max(s.Wert, 0) / total * 360.0);
                float von = start;                 // fest fuer die Klammer
                z.Markiert("reihe:" + (s.Label ?? ""), Anteilwert(s.Label, s.Wert, total),
                           zs => Kreissegment(zs, rect, von, sweep, Flaeche(s.Farbe)));
                start += sweep;
            }
            z.Ellipse(rect.Left, rect.Top, rect.Width, rect.Height,
                      Stift(Farbrolle.HINTERGRUND, 3f));

            var rahmen = Stift(Farbrolle.LEGENDENRAHMEN, 1f);
            using (var lf = Schrift(19f))
                foreach (Segment s in segmente)
                {
                    float ex = lx, ey = ly;        // fest fuer die Klammer
                    // Dieses Bild baut seine Legende selbst (die Prozentzahl steht im
                    // Eintrag); die Marke setzt es deshalb hier statt im Helfer
                    // Legende - derselbe Schluessel wie am Segment.
                    z.Markiert("legende:" + (s.Label ?? ""), ze =>
                    {
                        ze.Rechteck(ex, ey, 28f, 28f, null, Flaeche(s.Farbe));
                        ze.Rechteck(ex, ey, 28f, 28f, rahmen);
                        Text(ze, s.Label + "   " + (s.Wert / total * 100.0).ToString("N1", DE) + " %",
                             lf, Farbrolle.TEXT, ex + 40f, ey + 1f);
                    });
                    ly += 48f;
                }
            return z;
        }

        // =================================================================== Balken

        /// <summary>
        /// Horizontale Balken (ein Balken je Variante, Diagramm wächst nach unten —
        /// Konzept Kap. 6.1). Stamm wird farblich hervorgehoben.
        /// </summary>
        public static byte[] BalkenHorizontal(string titel, string einheit, List<Balken> balken)
            => SkiaMaler.Png(BalkenHorizontalModell(titel, einheit, balken));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c).
        ///
        /// <para><b>Ein reines Pixelbild</b> (DG-E3-7): keine Zeichenfläche, keine
        /// <c>Datenreihe</c> — die Achse dieses Bildes zählt Varianten, keine Zeit.
        /// Eine ZEILE ist das Datenelement: Beschriftung, Balken, Rahmen und die Zahl
        /// dahinter stehen zusammen in der Klammer <c>reihe:&lt;Variante&gt;</c> und
        /// tragen denselben <c>Wert</c>. Dieses Bild führt keine eigene Legende — die
        /// Beschriftung links IST sie.</para>
        ///
        /// <para>Die senkrechte Achsenlinie bleibt ohne Marke: Sie muss auch dann
        /// stehen, wenn die Oberfläche eine Zeile ausblendet (dieselbe Regel wie beim
        /// Achsenkreuz der Ganglinien).</para>
        /// </summary>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß — nur die Breite wirkt, die Höhe folgt der Zahl
        /// der Balken; <c>null</c> = 1240 breit wie bisher.</param>
        public static Zeichenmodell BalkenHorizontalModell(string titel, string einheit,
                                                           List<Balken> balken, Bildmass? mass = null)
        {
            int W = Bildmass.BreiteOder(mass, 1240);
            int H = 150 + balken.Count * 64;
            var z = Modell(W, H);
            z.Markiert("titel", zt =>
                Titel(zt, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W, mass.HasValue));

            // Die Beschriftungsspalte: 300, im schmalen Zielmaß höchstens drei Zehntel der Breite.
            float links = Math.Min(300f, W * 0.3f), rechts = W - 150f, oben = 80f;
            double max = Math.Max(balken.Max(b => Math.Abs(b.Wert)), 1e-9);

            var rahmen = Stift(Farbrolle.LEGENDENRAHMEN, 1f);
            using (var lf = Schrift(18f))
            using (var wf = Schrift(17f))
            {
                for (int i = 0; i < balken.Count; i++)
                {
                    float y = oben + i * 64f;
                    Balken b = balken[i];
                    float laenge = (float)(Math.Abs(b.Wert) / max * (rechts - links));
                    Farbrolle farbe = b.Hervorheben ? Farbrolle.STAMM : Farbrolle.WAERME_WP;

                    // Label links (rechtsbündig).
                    float lbreite = lf.MeasureText(b.Label ?? "");

                    z.Markiert("reihe:" + (b.Label ?? ""),
                               Elementwert(b.Label, b.Wert, "N0", einheit), zb =>
                    {
                        Text(zb, b.Label, lf, Farbrolle.TEXT, links - 12f - lbreite, y + 8f);

                        zb.Rechteck(links, y, laenge, 40f, null, Flaeche(farbe));
                        zb.Rechteck(links, y, laenge, 40f, rahmen);
                        Text(zb, b.Wert.ToString("N0", DE), wf, Farbrolle.TEXT,
                             links + laenge + 10f, y + 9f);
                    });
                }
            }
            z.Linie(links, oben - 8f, links, oben + balken.Count * 64f - 16f,
                    Stift(Farbrolle.ACHSE, 2f));
            return z;
        }

        // =================================================================== Ganglinien

        /// <summary>
        /// Ganglinientyp 1: Wärmeerzeugung im Jahresverlauf — gestapelte Erzeugung
        /// (Tagesmittel), Wärmebedarf als Linie (Konzept Kap. 6.2 Nr. 1).
        /// </summary>
        public static byte[] JahresverlaufWaerme(ZeitreihenSatz z)
        {
            Zeichenmodell m = JahresverlaufWaermeModell(z);
            return m == null ? null : SkiaMaler.Png(m);
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe d).
        ///
        /// <para><b>Ein reines PIXELBILD</b> (Entscheid DG-E3-7): keine
        /// <see cref="Zeichenmodell.Flaeche"/> und keine <see cref="Datenreihe"/>. Der
        /// SVG-Weg schreibt deshalb jeden Befehl so, wie das PNG ihn malt — der Gewinn
        /// im Bericht ist Schärfe und durchsuchbarer Text, nicht Bedienung.</para>
        ///
        /// <para><c>null</c> heißt „kein Bild": Führt der Lauf weder Erzeugung noch
        /// Bedarf, entsteht auch kein Modell — der Bericht lässt die Stelle aus,
        /// statt einen Leerhinweis zu zeichnen.</para>
        /// </summary>
        /// <param name="z">Der Zeitreihensatz des Laufs.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 560 wie bisher.</param>
        public static Zeichenmodell JahresverlaufWaermeModell(ZeitreihenSatz z, Bildmass? mass = null)
        {
            var stapel = WaermeErzeugerReihen(z, tagesmittel: true);
            double[] bedarf = TagesMittel(z.Hole(ZeitreihenSatz.WAERMEBEDARF));
            if (stapel.Count == 0 && bedarf == null) return null;
            return StapelDiagrammModell("Wärmeerzeugung im Jahresverlauf (Tagesmittel)", "kW",
                stapel, bedarf, "Wärmebedarf", MonatsTicks365(), mass);
        }

        /// <summary>
        /// Ganglinientyp 2: Jahresdauerlinie Wärme — geordnete Bedarfslinie plus
        /// geordnete Dauerlinien der Erzeuger (Konzept Kap. 6.2 Nr. 2).
        /// </summary>
        public static byte[] DauerlinieWaerme(ZeitreihenSatz z)
        {
            Zeichenmodell m = DauerlinieWaermeModell(z);
            return m == null ? null : SkiaMaler.Png(m);
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe d) — ein reines
        /// Pixelbild nach Entscheid DG-E3-7; <c>null</c>, wenn der Lauf keinen
        /// Wärmebedarf führt. Siehe <see cref="JahresverlaufWaermeModell"/>.
        ///
        /// <para>Auf der x-Achse zählt hier der RANG, nicht die Jahresstunde — die
        /// Reihen sind absteigend sortiert.</para>
        /// </summary>
        /// <param name="z">Der Zeitreihensatz des Laufs.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 560 wie bisher.</param>
        public static Zeichenmodell DauerlinieWaermeModell(ZeitreihenSatz z, Bildmass? mass = null)
        {
            double[] bedarf = z.Hole(ZeitreihenSatz.WAERMEBEDARF);
            if (bedarf == null) return null;

            var reihen = new List<Reihe> { new Reihe("Wärmebedarf", SortiertAbsteigend(bedarf), C_BEDARF) };
            foreach (Reihe r in WaermeErzeugerReihen(z, tagesmittel: false))
                reihen.Add(Mit(r, SortiertAbsteigend(r.Werte)));

            return LinienDiagrammModell("Jahresdauerlinie Wärme", "kW", reihen,
                new[] { 0, 2190, 4380, 6570, 8760 },
                new[] { "0", "2.190", "4.380", "6.570", "8.760 h" }, mass);
        }

        /// <summary>
        /// Ganglinientyp 3: Strombilanz im Monatsverlauf — gestapelte Deckung
        /// (PV-Eigenverbrauch, BHKW, Netzbezug), Einspeisung als eigene Reihe,
        /// Strombedarf als Linie (Konzept Kap. 6.2 Nr. 3).
        /// </summary>
        public static byte[] StrombilanzMonate(ZeitreihenSatz z)
        {
            Zeichenmodell modell = StrombilanzMonateModell(z);
            return modell == null ? null : SkiaMaler.Png(modell);
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c); <c>null</c> in
        /// denselben Fällen, in denen <see cref="StrombilanzMonate"/> kein Bild
        /// liefert — ohne Strombedarf und ohne eine einzige Deckungsreihe.
        /// </summary>
        /// <param name="z">Der Zeitreihensatz des Laufs.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 560 wie bisher.</param>
        public static Zeichenmodell StrombilanzMonateModell(ZeitreihenSatz z, Bildmass? mass = null)
        {
            // E29 (#536, Entscheide E26‑Q6 / E29‑Q7 a): die Linie ist der Strombedarf des
            // Anschlusses — aller Verbraucher vor jeder Eigenerzeugung, dieselbe Bezugsgröße
            // wie die Strommatrix. Ohne Gesamtreihe (Satz ohne Simulationslauf) gilt der
            // Projektbedarf wie bisher; Beschriftung und Stapel bleiben.
            double[] bedarf = z.Hole(ZeitreihenSatz.STROMBEDARF_GESAMT)
                              ?? z.Hole(ZeitreihenSatz.STROMBEDARF);
            if (bedarf == null) return null;

            var serien = new List<Reihe>();
            if (z.Hat(ZeitreihenSatz.PV_GENUTZT))
                serien.Add(new Reihe("PV-Eigenverbrauch", MonatsSummenMWh(z.Hole(ZeitreihenSatz.PV_GENUTZT)), C_PV));
            if (z.Hat(ZeitreihenSatz.BHKW_STROM))
                serien.Add(new Reihe("BHKW-Strom", MonatsSummenMWh(z.Hole(ZeitreihenSatz.BHKW_STROM)), C_BHKW));
            if (z.Hat(ZeitreihenSatz.NETZBEZUG))
                serien.Add(new Reihe("Netzbezug", MonatsSummenMWh(z.Hole(ZeitreihenSatz.NETZBEZUG)), C_KESSEL));
            if (z.Hat(ZeitreihenSatz.NETZEINSPEISUNG))
                serien.Add(new Reihe("Netzeinspeisung gesamt",
                    MonatsSummenMWh(z.Hole(ZeitreihenSatz.NETZEINSPEISUNG)), C_NETZ));
            else if (z.Hat(ZeitreihenSatz.PV_UEBERSCHUSS))
                serien.Add(new Reihe("Einspeisung", MonatsSummenMWh(z.Hole(ZeitreihenSatz.PV_UEBERSCHUSS)), C_NETZ));
            if (serien.Count == 0) return null;

            return MonatsBalkenModell("Strombilanz im Monatsverlauf", "MWh/Monat",
                serien, MonatsSummenMWh(bedarf), "Strombedarf", mass);
        }

        /// <summary>
        /// Ganglinientyp 4: Speicherverlauf — Füllstand über drei charakteristische
        /// Wochen (Winter/Übergang/Sommer; Konzept Kap. 6.2 Nr. 4).
        /// </summary>
        public static byte[] Speicherverlauf(ZeitreihenSatz z)
        {
            Zeichenmodell m = SpeicherverlaufModell(z);
            return m == null ? null : SkiaMaler.Png(m);
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe d) — ein reines
        /// Pixelbild nach Entscheid DG-E3-7; <c>null</c>, wenn der Lauf keine
        /// Füllstandsreihe führt. Siehe <see cref="JahresverlaufWaermeModell"/>.
        ///
        /// <para><b>Drei Wochenfelder in EINEM Bild.</b> Jede Reihe wird deshalb
        /// DREIMAL gezeichnet — einmal je Feld —, und alle drei Befehle tragen
        /// dieselbe Marke <c>reihe:&lt;Name&gt;</c>. Eine Zeichenfläche in
        /// Datenkoordinaten gäbe es hier ohnehin nicht: Sie wäre nicht eine, sondern
        /// drei.</para>
        /// </summary>
        /// <param name="z">Der Zeitreihensatz des Laufs.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 520 wie bisher.</param>
        public static Zeichenmodell SpeicherverlaufModell(ZeitreihenSatz z, Bildmass? mass = null)
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

            int W = Bildmass.BreiteOder(mass, 1240), H = Bildmass.HoeheOder(mass, 520);
            List<Segment> leg = reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
            float umbruch = mass.HasValue ? W - 30f : 0f;
            float mehr = mass.HasValue ? (LegendenZeilen(leg, 70f, umbruch) - 1) * LEGENDE_ZEILE : 0f;
            if (mass.HasValue) H = Math.Max(H, (int)Math.Ceiling(190f + mehr + STUFE2_MIN_FLAECHE));

            var bild = Modell(W, H);
            bild.Markiert("titel", zt => Titel(zt, "Speicherverlauf — Füllstand [kWh]", W, mass.HasValue));

            double max = reihen.Max(r => r.Werte.Max());
            if (max <= 0) max = 1;

            float panelB = (W - 120f) / 3f;
            if (mass.HasValue) titelWoche = Wochentitel(titelWoche, panelB);
            for (int p = 0; p < 3; p++)
            {
                var rc = SKRect.Create(70f + p * (panelB + 12f), 100f, panelB - 24f, H - 190f - mehr);
                PanelRahmen(bild, rc, titelWoche[p]);
                int feld = p;
                foreach (Reihe r in reihen)
                {
                    Reihe reihe = r;
                    bild.Markiert("reihe:" + reihe.Name, zr =>
                        ZeichneLinie(zr, rc, Ausschnitt(reihe.Werte, fenster[feld], 168),
                                     0, max, reihe.Farbe, 3f));
                }
                // Y-Beschriftung nur links.
                if (p == 0)
                    using (var f = Schrift(15f))
                        bild.Markiert("yachse", zy =>
                        {
                            Text(zy, max.ToString("N0", DE), f, Farbrolle.ACHSE, rc.Left - 62f, rc.Top - 8f);
                            Text(zy, "0", f, Farbrolle.ACHSE, rc.Left - 24f, rc.Bottom - 10f);
                        });
            }
            Legende(bild, leg, 70f, H - 56f - mehr, umbruch);
            return bild;
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
            Zeichenmodell m = SpeichertemperaturenModell(z);
            return m == null ? null : SkiaMaler.Png(m);
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe d) — ein reines
        /// Pixelbild nach Entscheid DG-E3-7; <c>null</c>, wenn der Lauf keine
        /// Temperaturreihe führt. Drei Wochenfelder wie bei
        /// <see cref="SpeicherverlaufModell"/>, nur mit einer Achse, die beim
        /// kleinsten vorkommenden Wert beginnt.
        /// </summary>
        /// <param name="z">Der Zeitreihensatz des Laufs.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 560 wie bisher.</param>
        public static Zeichenmodell SpeichertemperaturenModell(ZeitreihenSatz z, Bildmass? mass = null)
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

            int W = Bildmass.BreiteOder(mass, 1240), H = Bildmass.HoeheOder(mass, 560);
            // Die Legende bricht bei W − 70 um und hat zwei Zeilen Platz; im Zielmaß räumt die
            // Zeichenfläche jeder weiteren Zeile Platz.
            List<Segment> leg = reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
            float mehr = mass.HasValue ? Math.Max(0, LegendenZeilen(leg, 70f, W - 70f) - 2) * LEGENDE_ZEILE : 0f;
            if (mass.HasValue) H = Math.Max(H, (int)Math.Ceiling(230f + mehr + STUFE2_MIN_FLAECHE));

            var bild = Modell(W, H);
            bild.Markiert("titel", zt =>
                Titel(zt, "Speichertemperaturen — oberste und unterste Schicht [°C]", W, mass.HasValue));

            float panelB = (W - 120f) / 3f;
            if (mass.HasValue) titelWoche = Wochentitel(titelWoche, panelB);
            for (int p = 0; p < 3; p++)
            {
                var rc = SKRect.Create(70f + p * (panelB + 12f), 100f, panelB - 24f, H - 230f - mehr);
                PanelRahmen(bild, rc, titelWoche[p]);
                int feld = p;
                foreach (Reihe r in reihen)
                {
                    Reihe reihe = r;
                    bild.Markiert("reihe:" + reihe.Name, zr =>
                        ZeichneLinie(zr, rc, Ausschnitt(reihe.Werte, fenster[feld], 168),
                                     min, max, reihe.Farbe, 3f));
                }

                if (p == 0)
                    using (var f = Schrift(15f))
                        bild.Markiert("yachse", zy =>
                        {
                            Text(zy, max.ToString("N0", DE), f, Farbrolle.ACHSE, rc.Left - 62f, rc.Top - 8f);
                            Text(zy, min.ToString("N0", DE), f, Farbrolle.ACHSE, rc.Left - 62f, rc.Bottom - 10f);
                        });
            }

            // Umbruch bei vielen Serien: zwei Reihen je Speicher füllen die Zeile
            // schneller als beim Füllstandsdiagramm.
            Legende(bild, leg, 70f, H - 96f - mehr, W - 70f);
            return bild;
        }

        // =================================================================== Kernzeichner

        /// <summary>
        /// Der gemeinsame Rumpf des gestapelten Jahresverlaufs — seit Etappe DG-E3
        /// (Gruppe d) ein <see cref="Zeichenmodell"/> statt PNG-Bytes. Ein reines
        /// Pixelbild nach Entscheid DG-E3-7: Die Marken <c>titel</c>, <c>xachse</c>,
        /// <c>yachse</c>, <c>reihe:…</c> und <c>legende:…</c> stehen im Baum, eine
        /// Zeichenfläche in Datenkoordinaten gibt es nicht.
        /// </summary>
        private static Zeichenmodell StapelDiagrammModell(string titel, string einheit,
                                             List<Reihe> stapel,
                                             double[] linie, string linienName,
                                             KeyValuePair<int[], string[]> xticks,
                                             Bildmass? mass = null)
        {
            int W = Bildmass.BreiteOder(mass, 1240), H = Bildmass.HoeheOder(mass, 560);
            var leg = stapel.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
            if (linie != null) leg.Add(new Segment(linienName, 0, C_BEDARF));
            // Stufe 2: Die Legende bricht an der Breite um, die Zeichenfläche räumt ihr den Platz.
            float umbruch = mass.HasValue ? W - 30f : 0f;
            float mehr = mass.HasValue ? (LegendenZeilen(leg, 90f, umbruch) - 1) * LEGENDE_ZEILE : 0f;
            if (mass.HasValue) H = Math.Max(H, (int)Math.Ceiling(180f + mehr + STUFE2_MIN_FLAECHE));

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel + "  [" + einheit + "]", W, mass.HasValue));
            var rc = SKRect.Create(90f, 80f, W - 130f, H - 180f - mehr);

            int n = stapel.Count > 0 ? stapel[0].Werte.Length : linie.Length;
            var summe = new double[n];
            foreach (Reihe r in stapel)
                for (int i = 0; i < n; i++) summe[i] += Math.Max(r.Werte[i], 0);

            double max = summe.Length > 0 ? summe.Max() : 0;
            if (linie != null) max = Math.Max(max, linie.Max());
            max = Nice(max);

            AchsenRaster(z, rc, max, xticks.Key, xticks.Value, n);

            // Stapel von unten nach oben zeichnen (kumulierte Flächen).
            var unten = new double[n];
            foreach (Reihe r in stapel)
            {
                var oben = new double[n];
                for (int i = 0; i < n; i++) oben[i] = unten[i] + Math.Max(r.Werte[i], 0);
                Reihe reihe = r;
                double[] unterkante = unten, oberkante = oben;
                z.Markiert("reihe:" + reihe.Name, zr =>
                    ZeichneFlaeche(zr, rc, unterkante, oberkante, max, reihe.Farbe));
                unten = oben;
            }
            if (linie != null)
                z.Markiert("reihe:" + linienName, zr =>
                    ZeichneLinie(zr, rc, linie, 0, max, C_BEDARF, 3f));

            Legende(z, leg, 90f, H - 64f - mehr, umbruch);
            return z;
        }

        /// <summary>
        /// Der gemeinsame Rumpf der Dauerlinie — seit Etappe DG-E3 (Gruppe d) ein
        /// <see cref="Zeichenmodell"/> statt PNG-Bytes; siehe
        /// <see cref="StapelDiagrammModell"/>.
        /// </summary>
        private static Zeichenmodell LinienDiagrammModell(string titel, string einheit,
                                             List<Reihe> reihen,
                                             int[] xpos, string[] xlab,
                                             Bildmass? mass = null)
        {
            int W = Bildmass.BreiteOder(mass, 1240), H = Bildmass.HoeheOder(mass, 560);
            var leg = reihen.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
            float umbruch = mass.HasValue ? W - 30f : 0f;
            float mehr = mass.HasValue ? (LegendenZeilen(leg, 90f, umbruch) - 1) * LEGENDE_ZEILE : 0f;
            if (mass.HasValue) H = Math.Max(H, (int)Math.Ceiling(180f + mehr + STUFE2_MIN_FLAECHE));
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel + "  [" + einheit + "]", W, mass.HasValue));
            var rc = SKRect.Create(90f, 80f, W - 130f, H - 180f - mehr);

            int n = reihen[0].Werte.Length;
            double max = Nice(reihen.Max(r => r.Werte.Max()));
            AchsenRaster(z, rc, max, xpos, xlab, n);

            foreach (Reihe r in reihen)
            {
                Reihe reihe = r;
                z.Markiert("reihe:" + reihe.Name, zr =>
                    ZeichneLinie(zr, rc, reihe.Werte, 0, max, reihe.Farbe,
                                 Traegt(reihe, Farbrolle.BEDARF, C_BEDARF) ? 3.5f : 2.5f));
            }

            Legende(z, leg, 90f, H - 64f - mehr, umbruch);
            return z;
        }

        /// <summary>
        /// Der gemeinsame Rumpf der Monatsbalken als ZEICHENMODELL (Etappe DG-E3,
        /// Gruppe c). Ein reines Pixelbild (DG-E3-7): keine Zeichenfläche, keine
        /// <c>Datenreihe</c> — die x-Achse zählt zwölf Monate, keine Stunde.
        ///
        /// <para>Jede Stapelschicht und jeder Nebenbalken trägt
        /// <c>reihe:&lt;Reihenname&gt;</c> und den Wert seines Monats. <b>Der
        /// Bedarfszug bekommt als Wert nur seinen Namen</b>: Er ist EIN Element über
        /// zwölf Monate und zeigt keine einzelne Zahl — die zwölf Punkte des PNG sind
        /// ein einziger Streckenzug.</para>
        /// </summary>
        private static Zeichenmodell MonatsBalkenModell(string titel, string einheit,
                                                        List<Reihe> serien,
                                                        double[] linie, string linienName,
                                                        Bildmass? mass = null)
        {
            int W = Bildmass.BreiteOder(mass, 1240), H = Bildmass.HoeheOder(mass, 560);
            string[] monate = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };
            float umbruch = 0f, mehr = 0f;
            if (mass.HasValue)
            {
                var vorab = serien.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
                if (linie != null) vorab.Add(new Segment(linienName, 0, C_BEDARF));
                umbruch = W - 30f;
                mehr = (LegendenZeilen(vorab, 90f, umbruch) - 1) * LEGENDE_ZEILE;
                H = Math.Max(H, (int)Math.Ceiling(180f + mehr + STUFE2_MIN_FLAECHE));
            }
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel + "  [" + einheit + "]", W, mass.HasValue));
            var rc = SKRect.Create(90f, 80f, W - 130f, H - 180f - mehr);

            // Einspeisung wird nicht gestapelt, sondern als schmaler Nebenbalken gezeigt.
            Reihe einspeisung = serien.FirstOrDefault(s => s.Name == "Einspeisung");
            var stapel = serien.Where(s => s != einspeisung).ToList();

            var summe = new double[12];
            foreach (Reihe r in stapel) for (int m = 0; m < 12; m++) summe[m] += r.Werte[m];
            double max = summe.Max();
            if (linie != null) max = Math.Max(max, linie.Max());
            if (einspeisung != null) max = Math.Max(max, einspeisung.Werte.Max());
            max = Nice(max);

            // Achsen + Monatslabels. Das Achsenkreuz bleibt ohne Marke - es steht
            // auch dann, wenn die Oberflaeche eine Achsenteilung ausblendet.
            z.Markiert("yachse", zy => AchsenRasterOhneKreuz(zy, rc, max, null, null, 12));
            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
                for (int m = 0; m < 12; m++)
                {
                    float x = rc.Left + (m + 0.5f) * rc.Width / 12f;
                    float breite = f.MeasureText(monate[m]);
                    string lab = monate[m];
                    z.Markiert("xachse", zx =>
                        Text(zx, lab, f, Farbrolle.ACHSE, x - breite / 2f, rc.Bottom + 8f));
                }

            float slot = rc.Width / 12f;
            float bBreit = slot * 0.5f, bSchmal = slot * 0.18f;
            for (int m = 0; m < 12; m++)
            {
                float x0 = rc.Left + m * slot + slot * 0.12f;
                float unten = rc.Bottom;
                string monat = monate[m];
                int im = m;
                foreach (Reihe r in stapel)
                {
                    float hoehe = (float)(r.Werte[m] / max * rc.Height);
                    float oben = unten - hoehe;      // fest fuer die Klammer
                    Reihe rr = r;
                    z.Markiert("reihe:" + (r.Name ?? ""),
                               Elementwert(monat + WERT_TRENNER + (r.Name ?? ""),
                                           r.Werte[im], "N0", einheit),
                               zs => zs.Rechteck(x0, oben, bBreit, hoehe, null, Flaeche(rr.Farbe)));
                    unten -= hoehe;
                }
                if (einspeisung != null)
                {
                    float hoehe = (float)(einspeisung.Werte[m] / max * rc.Height);
                    Reihe e = einspeisung;
                    z.Markiert("reihe:" + (e.Name ?? ""),
                               Elementwert(monat + WERT_TRENNER + (e.Name ?? ""),
                                           e.Werte[im], "N0", einheit),
                               zs => zs.Rechteck(x0 + bBreit + slot * 0.06f, rc.Bottom - hoehe,
                                                 bSchmal, hoehe, null, Flaeche(e.Farbe)));
                }
            }

            if (linie != null)
            {
                var punkte = new SKPoint[12];
                for (int m = 0; m < 12; m++)
                    punkte[m] = new SKPoint(rc.Left + (m + 0.5f) * slot,
                        rc.Bottom - (float)(linie[m] / max * rc.Height));
                z.Markiert("reihe:" + (linienName ?? ""), linienName,
                           zl => Linienzug(zl, punkte, Stift(C_BEDARF, 3f)));
            }

            var leg = serien.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
            if (linie != null) leg.Add(new Segment(linienName, 0, C_BEDARF));
            Legende(z, leg, 90f, H - 56f - mehr, umbruch);
            return z;
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
        /// <param name="stammGestrichelt">
        /// AUFTRAG U18 — die Stammlinie gestrichelt zeichnen. Sie ist die BEZUGSGRÖSSE
        /// und keine Version; im Schwarz-Weiß-Ausdruck des Wortberichts ist sie nur an
        /// der Strichart von den Versionen zu trennen.
        /// <para><b>Vorgabe false</b>, damit jedes Bild, das den Schalter nicht setzt,
        /// byte-gleich bleibt — die ChartProben vergleichen Bilder.</para>
        /// </param>
        public static List<Reihe> VerlaufsReihen(List<VerlaufSerie> serien, bool mitStamm,
                                                 bool stammGestrichelt = false)
        {
            var reihen = new List<Reihe>();
            int i = 0;
            foreach (VerlaufSerie s in serien)
            {
                if (s.Kumuliert == null) continue;
                if (s.IstStamm)
                {
                    if (mitStamm)
                        reihen.Add(new Reihe(s.Anzeige, s.Kumuliert, C_STAMM)
                        { Strichart = stammGestrichelt ? Strichart.Gestrichelt : Strichart.Durchgezogen });
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
            => SkiaMaler.Png(KapitalwertVerlaufModell(titel, reihen, fussnote));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe b) — der Rumpf, den
        /// <see cref="KapitalwertVerlauf"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>x ist das PROJEKTJAHR</b> 0 … N (<see cref="Achsenart.Wert"/>,
        /// Einheit „a"), nicht der Index einer Zeitreihe: Das Bild zeichnet die
        /// kumulierten Barwerte je Jahr, und eine kürzere Reihe endet früher — sie
        /// bekommt deshalb ihr eigenes Fenster (DG-E3-1). Die Werte sind Geldbeträge,
        /// die Reihen tragen die Einheit „€" für die Zeigerzeile.</para>
        ///
        /// <para><b>Das PNG bleibt byte-gleich</b>: Der Maler übergeht Marken,
        /// Zeichenfläche und Datenreihen.</para>
        /// </summary>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 620 wie bisher.</param>
        public static Zeichenmodell KapitalwertVerlaufModell(string titel, List<Reihe> reihen,
                                                             string fussnote, Bildmass? mass = null)
        {
            int W = Bildmass.BreiteOder(mass, 1240), H = Bildmass.HoeheOder(mass, 620);
            // Die Legende hat zwei Zeilen Platz; im Zielmaß räumt die Zeichenfläche jeder weiteren.
            float mehr = 0f;
            if (mass.HasValue && reihen != null)
                mehr = Math.Max(0, LegendenZeilen(reihen.Where(r => r.Werte != null)
                                                        .Select(r => new Segment(r.Name, 0, r.Farbe, r.Strichart)).ToList(),
                                                  110f, W - 30f) - 2) * LEGENDE_ZEILE;
            if (mass.HasValue) H = Math.Max(H, (int)Math.Ceiling(220f + mehr + STUFE2_MIN_FLAECHE));
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel + "  [€]", W, mass.HasValue));
            var rc = SKRect.Create(110f, 80f, W - 150f, H - 220f - mehr);

            var gueltig = reihen.Where(r => r.Werte != null && r.Werte.Length >= 2 &&
                                       r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w)))
                                .ToList();
            if (gueltig.Count == 0)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, "Keine berechenbaren Reihen.", f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            // ETAPPE E6: Skala, Raster, Achsen, Nulllinie und Zeichenfläche stehen in
            // VerlaufAchsen — dieselben Befehle in derselben Reihenfolge wie bisher, damit
            // teilt das Bild sie mit dem Verlauf der drei Szenarien.
            VerlaufAchsen(z, rc, gueltig, out double min, out double max, out int jahre, out _);

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
                // AUFTRAG U18: Das Merkmal Strichart der Reihe wird hier gelesen
                // — dieselbe Strichfolge wie in den übrigen Linienbildern (8/5).
                // Ohne gesetztes Merkmal entsteht kein Pfadeffekt und das Bild bleibt
                // byte-gleich dem von vorher.
                float staerke = r.Breite > 0 ? r.Breite
                              : Traegt(r, Farbrolle.STAMM, C_STAMM) ? 3.5f : 2.5f;
                Strichmuster muster = Strichfolge(r.Strichart);
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    Linienzug(zr, punkte,
                              Stift(r.Farbe, staerke, muster, Strichverbindung.Rund)));

                // Dieselbe Reihe in DATENWERTEN, ungekuerzt (DG-E2-2) - mit ihrem
                // EIGENEN Fenster, damit eine kuerzere Reihe im SVG dort endet, wo sie
                // im Bild endet (DG-E3-1).
                z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, muster,
                                            r.Werte,
                                            Reihenfenster(z.Flaeche.Daten, r.Werte.Length),
                                            Reihenart.Linie, null, null, null, "€"));
            }

            // AUFTRAG U18 — die Legende nennt JEDE Reihe mit Namen, Farbe und
            // Strichart. Im Wortbericht ist dieses Bild der einzige Ort, an dem die
            // Versionen nebeneinander stehen; ohne Legende wären die Linien
            // ununterscheidbar.
            Legende(z, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe, r.Strichart)).ToList(),
                    110f, H - 104f - mehr, W - 30f);   // Umbruch: 2 Zeilen Platz (Review 11)
            if (!string.IsNullOrEmpty(fussnote))
                using (var f = Schrift(14f, kursiv: true))
                    Text(z, fussnote, f, Farbrolle.ACHSE, 110f, H - 28f);
            return z;
        }

        /// <summary>
        /// Skala, Raster, Achsen, Nulllinie und Zeichenfläche des Kapitalwert-Verlaufs —
        /// für das Bild je Version (<see cref="KapitalwertVerlaufModell"/>) und für den
        /// Verlauf mit drei Szenarien (<see cref="KapitalwertSzenarienModell"/>).
        ///
        /// <para><b>Befehl für Befehl der bisherige Rumpf</b> (ETAPPE E6): Die Befehle stehen
        /// in derselben Reihenfolge wie vor dem Auszug, das Bild je Version bleibt damit
        /// byte-gleich — die ChartProben halten es fest.</para>
        /// </summary>
        /// <param name="min">Untere Grenze der y-Skala [€].</param>
        /// <param name="max">Obere Grenze der y-Skala [€].</param>
        /// <param name="jahre">Die Zahl der Jahre N (Stützstellen − 1).</param>
        /// <param name="y0">Die Nulllinie in Bildpunkten.</param>
        private static void VerlaufAchsen(Zeichenmodell z, SKRect rc, List<Reihe> gueltig,
                                          out double min, out double max, out int jahre,
                                          out float y0)
        {
            int n = gueltig.Max(r => r.Werte.Length);          // Stützstellen (Jahre + 1)

            // Vorzeichenfähige Skala mit „schönen" Stufen (5 Rasterlinien).
            double unten = Math.Min(0, gueltig.Min(r => r.Werte.Min()));
            double oben = Math.Max(0, gueltig.Max(r => r.Werte.Max()));
            if (oben - unten < 1e-9) { oben = unten + 1; }
            double roh = (oben - unten) / 5.0;
            double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
            double schritt = zehner;
            foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (zehner * f >= roh) { schritt = zehner * f; break; }
            unten = Math.Floor(unten / schritt) * schritt;
            oben = Math.Ceiling(oben / schritt) * schritt;
            double lo = unten, hi = oben;                       // fest fuer die Klammern

            // Raster + y-Beschriftung.
            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (double wert = lo; wert <= hi + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - lo) / (hi - lo) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("N0", DE);
                        float breite = f.MeasureText(lab);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }
            });

            // X-Achse: Jahre 0…N, Beschriftung in sinnvollen Schritten.
            int nJahre = n - 1;
            int xschritt = nJahre <= 12 ? 1 : nJahre <= 25 ? 2 : nJahre <= 50 ? 5 : 10;
            var xraster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (int t = 0; t <= nJahre; t += xschritt)
                    {
                        float x = rc.Left + (float)t / Math.Max(nJahre, 1) * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, xraster);
                        string lab = t.ToString(DE);
                        float breite = f.MeasureText(lab);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - breite / 2f, rc.Bottom + 8f);
                    }
            });
            using (var f = Schrift(15f))
                z.Markiert("xachse", zx =>
                    Text(zx, BerichtTexte.T("Jahr"), f, Farbrolle.ACHSE,
                         rc.Right + 10f, rc.Bottom + 8f));

            // Achsen + hervorgehobene Nulllinie. Das ACHSENKREUZ bleibt ohne Marke
            // (Regel aus E2): Es muss auch dann stehen, wenn die Oberfläche beim Zoom
            // die Teilung einer Achse ausblendet.
            Achsenkreuz(z, rc);
            float null0 = (float)(rc.Bottom - (0 - lo) / (hi - lo) * rc.Height);
            z.Markiert("nulllinie", zn =>
                zn.Linie(rc.Left, null0, rc.Right, null0,
                         Stift(Farbrolle.ACHSE, 2f, new Strichmuster(3f * 2f, 1f * 2f))));

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER (DG-E3-4). x zaehlt das PROJEKTJAHR
            // 0 … N - eine freie Groesse in Jahren, keine Jahresstunde.
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(),
                                           new Datenfenster(0, nJahre, lo, hi),
                                           Achsenart.Wert, "a");

            min = lo; max = hi; jahre = nJahre; y0 = null0;
        }

        // ================================ Kapitalwert-Verlauf mit drei Szenarien (E6)

        /// <summary>
        /// ETAPPE E6 — die BESCHRIFTUNGEN des Verlaufs mit drei Szenarien. Sie kommen vom
        /// Aufrufer: Hülle und Bericht nehmen <see cref="AusRessourcen"/> (Oberflächensprache),
        /// die ChartProben die deutsche Vorgabe — so hängt kein Probebild an der Sprache des
        /// Rechners, auf dem es gezeichnet wird.
        /// </summary>
        public sealed class VerlaufSzenarienTexte
        {
            /// <summary>Kopf des ersten Legendenteils (<c>WIRT_VERL_LEG_VARIANTEN</c>).</summary>
            public string Varianten { get; set; } = "Varianten:";

            /// <summary>Kopf des zweiten Legendenteils (<c>WIRT_VERL_LEG_SZENARIEN</c>).</summary>
            public string Szenarien { get; set; } = "Szenarien:";

            /// <summary>Legendeneintrag der Marken (<c>WIRT_VERL_NULLDURCHGANG</c>).</summary>
            public string Nulldurchgang { get; set; } = "Nulldurchgang = dynamische Amortisation";

            /// <summary>
            /// Die benannte Ablehnung über acht Stände; <c>{0}</c> = so viele Farben führt das
            /// Bild (<c>WIRT_VERL_ZU_VIELE</c>).
            /// </summary>
            public string ZuVieleVarianten { get; set; }
                = "Mehr als {0} Varianten gewählt — der Verlauf unterscheidet Varianten über die Farbe "
                  + "und kennt {0} Farben. Bitte höchstens {0} Varianten anhaken.";

            /// <summary>Der Leerhinweis ohne berechenbare Linie (<c>WIRT_VERL_KEINE_REIHE</c>).</summary>
            public string KeineReihen { get; set; } = "Keine berechenbaren Reihen.";

            /// <summary>Das Szenario „Worst" (<c>WIRT_SZEN_WORST</c>).</summary>
            public string Worst { get; set; } = "Ungünstig";

            /// <summary>Das Szenario „Erwartet" (<c>WIRT_SZEN_ERWARTET</c>).</summary>
            public string Erwartet { get; set; } = "Erwartet";

            /// <summary>Das Szenario „Best" (<c>WIRT_SZEN_BEST</c>).</summary>
            public string Best { get; set; } = "Günstig";

            /// <summary>Der Anzeigename eines Szenarios (Persistenzwert); ein unbekannter
            /// Wert steht, wie er ist.</summary>
            public string Szenarioname(string szenario)
            {
                if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal)) return Worst;
                if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal)) return Best;
                if (string.Equals(szenario, WirtschaftlichkeitSzenario.ERWARTET, StringComparison.Ordinal)) return Erwartet;
                return szenario ?? "";
            }

            /// <summary>Dieselben Texte in der Oberflächensprache (<c>MyResource</c>).</summary>
            public static VerlaufSzenarienTexte AusRessourcen()
            {
                return new VerlaufSzenarienTexte
                {
                    Varianten = MyResource.Resource.WIRT_VERL_LEG_VARIANTEN,
                    Szenarien = MyResource.Resource.WIRT_VERL_LEG_SZENARIEN,
                    Nulldurchgang = MyResource.Resource.WIRT_VERL_NULLDURCHGANG,
                    ZuVieleVarianten = MyResource.Resource.WIRT_VERL_ZU_VIELE,
                    KeineReihen = MyResource.Resource.WIRT_VERL_KEINE_REIHE,
                    Worst = MyResource.Resource.WIRT_SZEN_WORST,
                    Erwartet = MyResource.Resource.WIRT_SZEN_ERWARTET,
                    Best = MyResource.Resource.WIRT_SZEN_BEST
                };
            }
        }

        /// <summary>
        /// ETAPPE E6 — ein Nulldurchgang im Verlauf mit drei Szenarien: die dynamische
        /// Amortisation EINER Linie.
        /// </summary>
        /// <param name="Reihe">Name der Linie („Stand · Szenario").</param>
        /// <param name="Stand">Der Stand (Anzeigename).</param>
        /// <param name="Szenario">Das Szenario (Anzeigename).</param>
        /// <param name="Jahr">Das Jahr des Durchgangs [a], linear im Jahr interpoliert.</param>
        public sealed record Nulldurchgangsmarke(string Reihe, string Stand, string Szenario, double Jahr);

        /// <summary>
        /// ETAPPE E6 (Konzept § 2.13 (5)) — die Linien des Verlaufs mit drei Szenarien samt
        /// der ZWEIGETEILTEN Legende und den Nulldurchgängen, gebildet von
        /// <see cref="VerlaufsReihenSzenarien"/>.
        /// </summary>
        public sealed class Szenarienreihen
        {
            /// <summary>Je Stand und Szenario eine Linie: Name „Stand · Szenario",
            /// Farbe = Stand, Strichart = Szenario.</summary>
            public List<Reihe> Reihen { get; } = new List<Reihe>();

            /// <summary>Legende, erster Teil: je Stand sein Name und seine Farbe.</summary>
            public List<(string Name, Farbton Ton)> Varianten { get; } = new List<(string, Farbton)>();

            /// <summary>Legende, zweiter Teil: je Szenario sein Name und seine Strichart — so
            /// viele Einträge wie Varianten, dazu drei, nicht Varianten mal drei.</summary>
            public List<(string Name, Strichart Strichart)> Szenarien { get; }
                = new List<(string, Strichart)>();

            /// <summary>Die Nulldurchgänge der Linien, je Linie höchstens einer.</summary>
            public List<Nulldurchgangsmarke> Marken { get; } = new List<Nulldurchgangsmarke>();

            /// <summary>
            /// Die BENANNTE Ablehnung: mehr Stände als Farben (<see cref="HoechstensStaende"/>).
            /// <c>null</c> = das Bild wird gezeichnet.
            /// </summary>
            public string Ablehnung { get; set; }
        }

        /// <summary>
        /// Wie viele Stände der Verlauf mit drei Szenarien unterscheiden kann: so viele Farben
        /// führt die Palette der Variantenreihen (acht, <see cref="Serienrolle"/> — dieselben
        /// Hausfarben wie <see cref="C_SERIEN"/>, aber als ROLLE). Darüber lehnt die
        /// Reihenbildung BENANNT ab, statt Farben doppelt zu vergeben.
        /// </summary>
        public static int HoechstensStaende => SERIENROLLEN.Length;

        /// <summary>Der Trenner im Namen einer Linie: „Stand · Szenario".</summary>
        public const string REIHENTRENNER = " · ";

        /// <summary>
        /// ETAPPE E6 — die Strichart eines Szenarios (Mockup Kategorie 8, „Wie sicher ist
        /// das?"): Erwartet durchgezogen, Ungünstig gestrichelt, Günstig gepunktet.
        /// </summary>
        public static Strichart StrichartDesSzenarios(string szenario)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal))
                return Strichart.Gestrichelt;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal))
                return Strichart.Gepunktet;
            return Strichart.Durchgezogen;
        }

        /// <summary>
        /// ETAPPE E6 (Konzept § 2.13 (5)) — die REIHENBILDUNG des Verlaufs mit drei
        /// Szenarien: je Stand mit Differenzlinie EINE Farbe, je Szenario EINE Strichart,
        /// der Name „Stand · Szenario" — dasselbe Projekt in drei Szenarien bekommt also
        /// eine Farbe und drei unterscheidbare Namen, nicht drei beliebige Farben und
        /// dreimal denselben Namen (Befund A3).
        ///
        /// <para><b>Die Farbe hängt am PLATZ in der Gruppe</b>, solange sie höchstens acht
        /// Stände mit Linie führt: Ein abgewählter Stand färbt die übrigen nicht um. Führt
        /// die Gruppe mehr, zählt der Platz unter den gewählten. <b>Mehr als acht gewählte
        /// Stände lehnt die Reihenbildung BENANNT ab</b> (<see cref="Szenarienreihen.Ablehnung"/>) —
        /// die Palette kennt acht Farben, eine doppelt vergebene wäre eine stille Lüge.</para>
        ///
        /// <para><b>Nulldurchgang je Linie</b> über <see cref="KapitalwertRechner.Nulldurchgang"/>
        /// — dieselbe Regel wie die Amortisationskennzahl.</para>
        /// </summary>
        /// <param name="verlauf">Die drei Läufe (<see cref="WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien"/>).</param>
        /// <param name="texte">Szenarionamen und Ablehnungstext; <c>null</c> = die Vorgabe.</param>
        /// <param name="nurStaende">Nur diese Stände (<c>Tab_Projekt.ID</c>); <c>null</c> = alle.</param>
        /// <param name="nurSzenarien">Nur diese Szenarien (Persistenzwerte); <c>null</c> = alle drei.</param>
        public static Szenarienreihen VerlaufsReihenSzenarien(WirtschaftlichkeitVerlaufSzenarien verlauf,
                                                             VerlaufSzenarienTexte texte,
                                                             ICollection<int> nurStaende = null,
                                                             ICollection<string> nurSzenarien = null)
        {
            texte = texte ?? new VerlaufSzenarienTexte();
            var ergebnis = new Szenarienreihen();
            if (verlauf == null) return ergebnis;

            List<KeyValuePair<int, string>> alle = verlauf.Versionen();
            var gewaehlt = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<int, string> v in alle)
                if (nurStaende == null || nurStaende.Contains(v.Key)) gewaehlt.Add(v);

            if (gewaehlt.Count > SERIENROLLEN.Length)
            {
                ergebnis.Ablehnung = string.Format(DE, texte.ZuVieleVarianten ?? "", SERIENROLLEN.Length);
                return ergebnis;
            }

            var szenarien = new List<string>();
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                if (nurSzenarien == null || nurSzenarien.Contains(s)) szenarien.Add(s);

            bool festerPlatz = alle.Count <= SERIENROLLEN.Length;
            for (int i = 0; i < gewaehlt.Count; i++)
            {
                int platz = i;
                if (festerPlatz)
                    for (int k = 0; k < alle.Count; k++)
                        if (alle[k].Key == gewaehlt[i].Key) { platz = k; break; }

                string stand = gewaehlt[i].Value ?? "";
                Farbton ton = Farbton.Aus(SERIENROLLEN[platz]);
                ergebnis.Varianten.Add((stand, ton));

                foreach (string s in szenarien)
                {
                    VerlaufSerie d = verlauf.Differenz(gewaehlt[i].Key, s);
                    if (d == null) continue;
                    string szenario = texte.Szenarioname(s);
                    string name = stand + REIHENTRENNER + szenario;
                    ergebnis.Reihen.Add(new Reihe(name, d.Kumuliert, ton)
                    {
                        Strichart = StrichartDesSzenarios(s),
                        // Erwartet trägt die Aussage und steht kräftiger (Mockup: 2,4 zu 1,8).
                        Breite = string.Equals(s, WirtschaftlichkeitSzenario.ERWARTET, StringComparison.Ordinal)
                               ? 3f : 2.5f
                    });
                    double? jahr = KapitalwertRechner.Nulldurchgang(d.Kumuliert);
                    if (jahr.HasValue)
                        ergebnis.Marken.Add(new Nulldurchgangsmarke(name, stand, szenario, jahr.Value));
                }
            }

            foreach (string s in szenarien)
                ergebnis.Szenarien.Add((texte.Szenarioname(s), StrichartDesSzenarios(s)));
            return ergebnis;
        }

        /// <summary>
        /// ETAPPE E6 — der Verlauf mit drei Szenarien als PNG (Wortbericht). Siehe
        /// <see cref="KapitalwertSzenarienModell"/>.
        /// </summary>
        public static byte[] KapitalwertSzenarien(string titel, Szenarienreihen inhalt,
                                                  VerlaufSzenarienTexte texte, string fussnote)
            => SkiaMaler.Png(KapitalwertSzenarienModell(titel, inhalt, texte, fussnote));

        /// <summary>
        /// ETAPPE E6 (Konzept § 2.13 (5), Mockup Kategorie 8 „Der Verlauf über die Zeit —
        /// alle drei Szenarien") — der <b>kumulierte Barwert der Differenz zur Referenz</b>
        /// je Jahr, für jeden Stand in drei Stricharten: <b>Farbe = Stand, Strichart =
        /// Szenario</b>, dazu je Linie ihr Nulldurchgang (die dynamische Amortisation in
        /// diesem Szenario) als Marke auf der Nulllinie.
        ///
        /// <para><b>Dieselbe Skala und Achsenführung wie das Bild je Version</b>
        /// (<see cref="VerlaufAchsen"/>): x ist das Projektjahr, die Nulllinie ist
        /// hervorgehoben, die Zeichenfläche trägt das Datenfenster für den Zoom der
        /// Oberfläche.</para>
        ///
        /// <para><b>Die Legende ist ZWEIGETEILT</b>: erst die Stände mit ihrer Farbe, in
        /// einer neuen Zeile die drei Szenarien mit ihrer Strichart (in Textfarbe, denn die
        /// Strichart gilt für jede Farbe) und die Marke — so viele Einträge wie Stände,
        /// dazu drei, nicht Stände mal drei.</para>
        ///
        /// <para><b>Das Bildmaß</b> (Konzept: mehr als etwa zwei Legendenzeilen passen nicht
        /// in 1240 × 620): Zwei Legendenzeilen — ein Teil je Zeile — tragen das feste Maß;
        /// jede weitere Zeile, die viele oder lange Namen brauchen, verlängert das Bild um
        /// eine Legendenzeile (<see cref="LEGENDE_ZEILE"/>). Die Zeichenfläche bleibt
        /// 1090 × 400 — ein Bild mit vielen Ständen wird länger, nicht flacher.</para>
        ///
        /// <para><b>Die Beschriftung der Marken</b> („Ungünstig 3,02 a" bei EINEM Stand,
        /// sonst „3,02 a") steht in bis zu drei Zeilen neben der Nulllinie; was keinen Platz
        /// findet, bleibt ohne Text — der Wert steht am Element (<c>data-wert</c>) und in
        /// der Zeile unter dem Bild.</para>
        /// </summary>
        /// <param name="titel">Überschrift ohne Einheit; „[€]" hängt das Bild an.</param>
        /// <param name="inhalt">Die Linien (<see cref="VerlaufsReihenSzenarien"/>).</param>
        /// <param name="texte">Legendenköpfe und Hinweise; <c>null</c> = die Vorgabe.</param>
        /// <param name="fussnote">Kursive Zeile unter der Legende; leer = keine.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 breit, 620 hoch plus die
        /// Legendenzeilen über zwei — wie bisher. Im Zielmaß bleibt die Höhe, die Zeichenfläche räumt der
        /// Legende Platz.</param>
        public static Zeichenmodell KapitalwertSzenarienModell(string titel, Szenarienreihen inhalt,
                                                               VerlaufSzenarienTexte texte,
                                                               string fussnote, Bildmass? mass = null)
        {
            texte = texte ?? new VerlaufSzenarienTexte();
            inhalt = inhalt ?? new Szenarienreihen();
            int W = mass.HasValue ? Math.Max(SZENARIEN_MIN_BREITE, Bildmass.BreiteOder(mass, 1240)) : 1240;
            int H0 = Bildmass.HoeheOder(mass, 620);
            var rc = SKRect.Create(110f, 80f, W - 150f, H0 - 220f);
            float legendeOben = rc.Bottom + 36f;

            var gueltig = new List<Reihe>();
            if (inhalt.Ablehnung == null)
                foreach (Reihe r in inhalt.Reihen)
                    if (r != null && r.Werte != null && r.Werte.Length >= 2 &&
                        r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w)))
                        gueltig.Add(r);

            int zeilen = gueltig.Count == 0 ? 0
                       : LegendeZweigeteilt(null, inhalt, texte, 110f, legendeOben, W - 30f);
            int H = H0 + (int)LEGENDE_ZEILE * Math.Max(0, zeilen - 2);
            if (mass.HasValue && H > H0)
            {
                // Stufe 2: Das Bild behält die Zielhöhe, die Zeichenfläche wird um die Legendenzeilen
                // über zwei niedriger — höchstens bis zu ihrer kleinsten Höhe; dann wächst das Bild.
                float extra = H - H0;
                float flaeche = Math.Max(STUFE2_MIN_FLAECHE, H0 - 220f - extra);
                rc = SKRect.Create(110f, 80f, W - 150f, flaeche);
                legendeOben = rc.Bottom + 36f;
                H = (int)Math.Ceiling(220f + extra + flaeche);
            }

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, (titel ?? "") + "  [€]", W, mass.HasValue));

            if (gueltig.Count == 0)
            {
                // Die benannte Ablehnung ist ein Satz, kein Wort: Sie bricht an der Breite
                // der Zeichenfläche um (höchstens vier Zeilen).
                string hinweis = inhalt.Ablehnung ?? texte.KeineReihen ?? "";
                using (var f = Schrift(18f))
                {
                    List<string> zeilenText = Umbruchzeilen(hinweis, f, rc.Width, 4);
                    z.Markiert("leerhinweis", zl =>
                    {
                        for (int i = 0; i < zeilenText.Count; i++)
                            Text(zl, zeilenText[i], f, Farbrolle.ACHSE, rc.Left,
                                 rc.Top + 20f + i * (TextHoehe(f) + 6f));
                    });
                }
                return z;
            }

            VerlaufAchsen(z, rc, gueltig, out double min, out double max, out int jahre, out float y0);

            // Linien: Farbe = Stand (die Rolle der Reihe), Strichart = Szenario.
            foreach (Reihe r in gueltig)
            {
                var punkte = new SKPoint[r.Werte.Length];
                for (int t = 0; t < r.Werte.Length; t++)
                {
                    float x = rc.Left + (float)t / Math.Max(jahre, 1) * rc.Width;
                    float y = (float)(rc.Bottom - (r.Werte[t] - min) / (max - min) * rc.Height);
                    punkte[t] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                }
                float staerke = r.Breite > 0 ? r.Breite : 2.5f;
                Strichmuster muster = Strichfolge(r.Strichart);
                Farbton ton = Ton(r);
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    Linienzug(zr, punkte, Stift(ton, staerke, muster, Strichverbindung.Rund)));
                z.FuegeReihe(new Datenreihe(r.Name ?? "", ton, staerke, muster, r.Werte,
                                            Reihenfenster(z.Flaeche.Daten, r.Werte.Length),
                                            Reihenart.Linie, null, null, null, "€"));
            }

            Nulldurchgaenge(z, rc, inhalt, gueltig, jahre, y0);

            LegendeZweigeteilt(z, inhalt, texte, 110f, legendeOben, W - 30f);
            if (!string.IsNullOrEmpty(fussnote))
                using (var f = Schrift(14f, kursiv: true))
                    Text(z, fussnote, f, Farbrolle.ACHSE, 110f,
                         legendeOben + zeilen * LEGENDE_ZEILE + 16f);
            return z;
        }

        /// <summary>Wie viele Zeilen die Beschriftung der Nulldurchgänge nutzen darf.</summary>
        private const int MARKENZEILEN = 3;

        /// <summary>
        /// ETAPPE E6 — die Nulldurchgänge als Marken auf der Nulllinie: ein Punkt in
        /// Textfarbe mit hellem Rand, dazu — wo Platz ist — eine gestrichelte Senkrechte und
        /// das Jahr. Die Beschriftung sucht sich von links nach rechts die erste der
        /// <see cref="MARKENZEILEN"/> Zeilen, in der sie keine andere berührt; über der
        /// Nulllinie, wenn dort Platz ist, sonst darunter. Jede Marke trägt ihren Wert
        /// („Stand · Szenario: 3,02 a") am Element.
        /// </summary>
        private static void Nulldurchgaenge(Zeichenmodell z, SKRect rc, Szenarienreihen inhalt,
                                            List<Reihe> gueltig, int jahre, float y0)
        {
            var namen = new HashSet<string>(gueltig.Select(r => r.Name ?? ""), StringComparer.Ordinal);
            var marken = inhalt.Marken
                .Where(m => m != null && namen.Contains(m.Reihe ?? "") && m.Jahr >= 0 && m.Jahr <= jahre)
                .OrderBy(m => m.Jahr).ThenBy(m => m.Reihe, StringComparer.Ordinal)
                .ToList();
            if (marken.Count == 0) return;

            bool einStand = inhalt.Varianten.Count == 1;
            var belegt = new List<List<(float Von, float Bis)>>();
            for (int k = 0; k < MARKENZEILEN; k++) belegt.Add(new List<(float, float)>());

            using (var f = Schrift(13f))
            {
                float hoehe = TextHoehe(f);
                // Über der Nulllinie, wenn alle Zeilen dort Platz haben; sonst darunter.
                bool oberhalb = y0 - 24f - (MARKENZEILEN - 1) * 20f - hoehe >= rc.Top;

                foreach (Nulldurchgangsmarke m in marken)
                {
                    float x = rc.Left + (float)(m.Jahr / Math.Max(jahre, 1)) * rc.Width;
                    string jahr = m.Jahr.ToString("N2", DE) + " a";
                    string text = (einStand ? m.Szenario + " " : "") + jahr;
                    string wert = m.Reihe + ": " + jahr;

                    float breite = f.MeasureText(text);
                    float von = x + 5f, bis = x + 5f + breite;
                    if (bis > rc.Right) { von = x - 5f - breite; bis = x - 5f; }

                    int zeile = -1;
                    for (int k = 0; k < belegt.Count && zeile < 0; k++)
                    {
                        bool frei = true;
                        foreach ((float Von, float Bis) b in belegt[k])
                            if (von < b.Bis + 8f && bis > b.Von - 8f) { frei = false; break; }
                        if (frei) zeile = k;
                    }
                    if (zeile >= 0) belegt[zeile].Add((von, bis));

                    float ty = oberhalb ? y0 - 24f - zeile * 20f - hoehe : y0 + 12f + zeile * 20f;
                    float px = x, tx = von;
                    bool beschriftet = zeile >= 0;
                    z.Markiert("nulldurchgang", wert, zm =>
                    {
                        if (beschriftet)
                        {
                            float ende = ty + hoehe / 2f;
                            zm.Linie(px, y0, px, ende,
                                     Stift(Farbrolle.ACHSE, 1.2f, new Strichmuster(4f, 3f)));
                            Text(zm, text, f, Farbrolle.TEXT, tx, ty);
                        }
                        zm.Kreis(px, y0, 6f, Stift(Farbrolle.HINTERGRUND, 2f), Flaeche(Farbrolle.TEXT));
                    });
                }
            }
        }

        /// <summary>
        /// ETAPPE E6 — die ZWEIGETEILTE Legende des Verlaufs mit drei Szenarien: in der
        /// ersten Zeile der Kopf „Varianten:" und je Stand ein gefülltes Farbfeld, in einer
        /// neuen Zeile der Kopf „Szenarien:", je Szenario ein Linienmuster in seiner Strichart
        /// und — gibt es Marken — der Eintrag des Nulldurchgangs. Umgebrochen wird je Teil
        /// mit hängendem Einzug hinter dem breiteren der beiden Köpfe.
        ///
        /// <para>Mit <paramref name="z"/> = <c>null</c> MISST sie nur — das Bild braucht die
        /// Zeilenzahl, bevor es sein Maß kennt; gemessen und gezeichnet wird mit derselben
        /// Regel.</para>
        /// </summary>
        /// <returns>Die Zahl der Legendenzeilen.</returns>
        private static int LegendeZweigeteilt(IZeichenziel z, Szenarienreihen inhalt,
                                              VerlaufSzenarienTexte texte, float x, float y,
                                              float rechts)
        {
            var rahmen = Stift(Farbrolle.LEGENDENRAHMEN, 1f);
            using (var fk = Schrift(16f, fett: true))
            using (var f = Schrift(16f))
            {
                string kopfVarianten = texte.Varianten ?? "";
                string kopfSzenarien = texte.Szenarien ?? "";
                float kopf = Math.Max(fk.MeasureText(kopfVarianten), fk.MeasureText(kopfSzenarien)) + 16f;
                float start = x + kopf;
                int zeilen = 1;
                float ey = y, ex = start;

                // ---- Teil 1: die Stände mit ihrer Farbe ----
                if (z != null)
                {
                    float ky = ey;
                    Text(z, kopfVarianten, fk, Farbrolle.TEXT, x, ky + 1f);
                }
                foreach ((string Name, Farbton Ton) v in inhalt.Varianten)
                {
                    string name = v.Name ?? "";
                    float breite = 40f + f.MeasureText(name) + 24f;
                    if (ex > start && ex + breite > rechts) { ex = start; ey += LEGENDE_ZEILE; zeilen++; }
                    if (z != null)
                    {
                        float px = ex, py = ey;
                        Farbton ton = v.Ton;
                        z.Markiert("legende:" + name, ze =>
                        {
                            ze.Rechteck(px, py, 22f, 22f, null, Flaeche(ton));
                            ze.Rechteck(px, py, 22f, 22f, rahmen);
                            Text(ze, name, f, Farbrolle.TEXT, px + 28f, py + 1f);
                        });
                    }
                    ex += breite;
                }

                // ---- Teil 2: die Szenarien mit ihrer Strichart, dann die Marke ----
                ey += LEGENDE_ZEILE; zeilen++; ex = start;
                if (z != null)
                {
                    float ky = ey;
                    Text(z, kopfSzenarien, fk, Farbrolle.TEXT, x, ky + 1f);
                }
                foreach ((string Name, Strichart Strichart) s in inhalt.Szenarien)
                {
                    string name = s.Name ?? "";
                    float breite = 48f + f.MeasureText(name) + 24f;
                    if (ex > start && ex + breite > rechts) { ex = start; ey += LEGENDE_ZEILE; zeilen++; }
                    if (z != null)
                    {
                        float px = ex, py = ey;
                        Strichmuster muster = Strichfolge(s.Strichart);
                        z.Markiert("legende:" + name, ze =>
                        {
                            ze.Linie(px, py + 11f, px + 40f, py + 11f, Stift(Farbrolle.TEXT, 3f, muster));
                            Text(ze, name, f, Farbrolle.TEXT, px + 48f, py + 1f);
                        });
                    }
                    ex += breite;
                }
                if (inhalt.Marken.Count > 0)
                {
                    string name = texte.Nulldurchgang ?? "";
                    float breite = 40f + f.MeasureText(name) + 24f;
                    if (ex > start && ex + breite > rechts) { ex = start; ey += LEGENDE_ZEILE; zeilen++; }
                    if (z != null)
                    {
                        float px = ex, py = ey;
                        z.Markiert("legende:" + name, ze =>
                        {
                            ze.Kreis(px + 11f, py + 11f, 6f, Stift(Farbrolle.HINTERGRUND, 2f),
                                     Flaeche(Farbrolle.TEXT));
                            Text(ze, name, f, Farbrolle.TEXT, px + 28f, py + 1f);
                        });
                    }
                }
                return zeilen;
            }
        }

        // ====================== Spanne der Kapitalwertdifferenz je Version (E6, Nachtrag E5b)

        /// <summary>
        /// ETAPPE E6 (Nachträge E5b, Anwenderentscheid 22.09.2026 zu Frage (4)) — EIN Balken
        /// des Spannenbilds: die Kapitalwertdifferenz einer Version zur Referenz in den drei
        /// Szenarien. Ein fehlender oder nicht endlicher Wert zählt als nicht vorhanden.
        ///
        /// <para><b>Die Spanne ist dieselbe wie in der Tafel</b>
        /// (<see cref="BandbreitenZeile.Spanne"/>, Entscheid Q4 aus E5): vom kleinsten bis zum
        /// größten der drei Werte — nicht vom Etikett „ungünstig" zum Etikett „günstig" —, und
        /// nur, wenn Ungünstig UND Günstig vorliegen; eine Spanne aus einer Zahl gibt es
        /// nicht. Der Erwartungsfall steht dann allein als Punkt.</para>
        /// </summary>
        public sealed class Spannenbalken
        {
            /// <summary>Der Anzeigename der Version — die Beschriftung links.</summary>
            public string Name { get; set; } = "";

            /// <summary>Die Differenz im Szenario „Worst" (Ungünstig) [€].</summary>
            public double? Worst { get; set; }

            /// <summary>Die Differenz im Erwartungsfall [€] — der Punkt.</summary>
            public double? Erwartet { get; set; }

            /// <summary>Die Differenz im Szenario „Best" (Günstig) [€].</summary>
            public double? Best { get; set; }

            /// <summary>Das linke Ende des Balkens: der kleinste der drei Werte;
            /// <c>null</c> = keine Spanne.</summary>
            public double? Von => Grenze(true);

            /// <summary>Das rechte Ende des Balkens: der größte der drei Werte;
            /// <c>null</c> = keine Spanne.</summary>
            public double? Bis => Grenze(false);

            /// <summary>Der Erwartungsfall, wenn er endlich ist — sonst kein Punkt.</summary>
            public double? Punkt => EndlicherWert(Erwartet);

            /// <summary>Gibt es etwas zu zeichnen — einen Balken oder einen Punkt?</summary>
            public bool Zeichenbar => Von.HasValue || Punkt.HasValue;

            private double? Grenze(bool klein)
            {
                double? w = EndlicherWert(Worst), b = EndlicherWert(Best), e = EndlicherWert(Erwartet);
                if (!w.HasValue || !b.HasValue) return null;
                double g = klein ? Math.Min(w.Value, b.Value) : Math.Max(w.Value, b.Value);
                if (e.HasValue) g = klein ? Math.Min(g, e.Value) : Math.Max(g, e.Value);
                return g;
            }

            /// <summary>
            /// Die Balken einer Bandbreite — je Zeile (jeder Stand außer der Referenz) einer,
            /// in der Reihenfolge der Gruppe. Tafel und Bild lesen damit DASSELBE Modell.
            /// </summary>
            public static List<Spannenbalken> Aus(WirtschaftlichkeitBandbreite bandbreite)
            {
                var liste = new List<Spannenbalken>();
                if (bandbreite == null) return liste;
                foreach (BandbreitenZeile z in bandbreite.Zeilen)
                    if (z != null)
                        liste.Add(new Spannenbalken
                        {
                            Name = z.Anzeige ?? "", Worst = z.Worst, Erwartet = z.Erwartet, Best = z.Best
                        });
                return liste;
            }
        }

        /// <summary>
        /// ETAPPE E6 (Nachtrag E5b) — die Texte des Spannenbilds. Die Vorgaben sind der
        /// deutsche Wortlaut (die ChartProben hängen so nicht an der Sprache des Rechners);
        /// <see cref="AusRessourcen"/> liest die Oberflächensprache.
        /// </summary>
        public sealed class SpannenTexte
        {
            /// <summary>Die Überschrift (<c>WIRT_SPANNE_TITEL</c>); „[€]" hängt das Bild an.</summary>
            public string Titel { get; set; } = "Spanne der Kapitalwertdifferenz je Version";

            /// <summary>Der Legendeneintrag des Balkens (<c>WIRT_SPANNE_LEG_SPANNE</c>).</summary>
            public string Spanne { get; set; } = "Spanne ungünstig bis günstig";

            /// <summary>Der Legendeneintrag des Punkts (<c>WIRT_SPANNE_LEG_ERWARTET</c>).</summary>
            public string Erwartungsfall { get; set; } = "Erwartungsfall";

            /// <summary>Der Legendeneintrag der Marken unter null (<c>WIRT_SPANNE_LEG_UNTER</c>) —
            /// er steht nur, wenn ein Wert unter der Referenz liegt.</summary>
            public string UnterReferenz { get; set; } = "unter der Referenz";

            /// <summary>Der Achsentitel; <c>{0}</c> = die Referenz (<c>WIRT_SPANNE_ACHSE</c>).</summary>
            public string Achse { get; set; } = "Kapitalwertdifferenz zu {0} [€] — Nulllinie = Referenz";

            /// <summary>Die Referenz ohne Namen (<c>WIRT_EMPF_REFERENZ_UNBENANNT</c>).</summary>
            public string ReferenzUnbenannt { get; set; } = "dem Referenzfall";

            /// <summary>Der Leerhinweis ohne zeichenbaren Balken (<c>WIRT_SPANNE_LEER</c>).</summary>
            public string Leer { get; set; } = "Keine Version mit Szenarienwerten.";

            /// <summary>Das Szenario „Worst" (<c>WIRT_SZEN_WORST</c>) — im Wert am Element.</summary>
            public string Worst { get; set; } = "Ungünstig";

            /// <summary>Das Szenario „Erwartet" (<c>WIRT_SZEN_ERWARTET</c>).</summary>
            public string Erwartet { get; set; } = "Erwartet";

            /// <summary>Das Szenario „Best" (<c>WIRT_SZEN_BEST</c>).</summary>
            public string Best { get; set; } = "Günstig";

            /// <summary>Dieselben Texte in der Oberflächensprache (<c>MyResource</c>).</summary>
            public static SpannenTexte AusRessourcen()
            {
                return new SpannenTexte
                {
                    Titel = MyResource.Resource.WIRT_SPANNE_TITEL,
                    Spanne = MyResource.Resource.WIRT_SPANNE_LEG_SPANNE,
                    Erwartungsfall = MyResource.Resource.WIRT_SPANNE_LEG_ERWARTET,
                    UnterReferenz = MyResource.Resource.WIRT_SPANNE_LEG_UNTER,
                    Achse = MyResource.Resource.WIRT_SPANNE_ACHSE,
                    ReferenzUnbenannt = MyResource.Resource.WIRT_EMPF_REFERENZ_UNBENANNT,
                    Leer = MyResource.Resource.WIRT_SPANNE_LEER,
                    Worst = MyResource.Resource.WIRT_SZEN_WORST,
                    Erwartet = MyResource.Resource.WIRT_SZEN_ERWARTET,
                    Best = MyResource.Resource.WIRT_SZEN_BEST
                };
            }
        }

        /// <summary>Der Abstand zweier Balken [px].</summary>
        public const float SPANNE_ZEILE = 72f;

        /// <summary>Die Deckung des Balkens — die Hausfarbe hell, wie das Band im Mockup.</summary>
        private const byte SPANNE_BAND_DECKUNG = 64;

        /// <summary>
        /// ETAPPE E6 — das Spannenbild als PNG (Wortbericht). Siehe
        /// <see cref="KapitalwertSpanneModell"/>.
        /// </summary>
        public static byte[] KapitalwertSpanne(IReadOnlyList<Spannenbalken> balken, string referenz,
                                               SpannenTexte texte)
            => SkiaMaler.Png(KapitalwertSpanneModell(balken, referenz, texte));

        /// <summary>
        /// ETAPPE E6 (Nachträge E5b, Anwenderentscheid 22.09.2026 zu Frage (4), Mockup
        /// <c>valeri-f2</c> „Spanne der Kapitalwertdifferenz je Variante") — die
        /// <b>Bandbreite je Version als Balken</b>: je Version eine Zeile, der Balken vom
        /// kleinsten bis zum größten der drei Szenariowerte, der Erwartungsfall als Punkt mit
        /// seinem Betrag darüber, die <b>Referenz als Nulllinie</b>.
        ///
        /// <para><b>Die Achse</b> zählt Euro und schließt die Null immer ein — ein Balken
        /// ganz rechts von ihr liegt vollständig über der Referenz, einer, der sie kreuzt,
        /// fällt im ungünstigen Fall unter sie. Die Stufen sind die „schönen" Stufen des
        /// Verlaufsbilds (etwa fünf Rasterlinien).</para>
        ///
        /// <para><b>Die Farben sagen das Vorzeichen</b> (Regel der Jahresprojektion: eine
        /// negative Säule ist rot): Punkt, Betrag und Balkenenden stehen in
        /// <see cref="Farbrolle.RASTER_GUT"/>, solange ihr Wert nicht unter der Referenz
        /// liegt, sonst in <see cref="Farbrolle.RASTER_SCHLECHT"/>; der Balken selbst ist die
        /// Hausfarbe, hell. Die Legende nennt Balken und Punkt und — nur wenn es ihn gibt —
        /// den Wert unter der Referenz.</para>
        ///
        /// <para><b>Ein reines Pixelbild</b> wie <see cref="BalkenHorizontalModell"/>: keine
        /// Zeichenfläche, keine Datenreihe. Eine ZEILE ist das Datenelement — Name, Balken,
        /// Enden, Punkt und Betrag stehen in der Klammer <c>reihe:&lt;Version&gt;</c> und
        /// tragen alle drei Werte am Element. Die Legende schaltet nichts (Marke
        /// <c>legende</c> ohne Namen).</para>
        ///
        /// <para><b>Das Bildmaß:</b> 1240 breit, die Höhe wächst mit den Versionen um
        /// <see cref="SPANNE_ZEILE"/> je Zeile (eine Version: 290, drei: 434); ohne
        /// zeichenbare Version 1240 × 200 mit dem Leerhinweis.</para>
        /// </summary>
        /// <param name="balken">Die Versionen (<see cref="Spannenbalken.Aus"/>).</param>
        /// <param name="referenz">Der Name der Referenz — im Achsentitel.</param>
        /// <param name="texte">Überschrift, Legende, Achse; <c>null</c> = die Vorgabe.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß — nur die Breite wirkt, die Höhe folgt den
        /// Versionen; <c>null</c> = 1240 breit wie bisher.</param>
        public static Zeichenmodell KapitalwertSpanneModell(IReadOnlyList<Spannenbalken> balken,
                                                            string referenz, SpannenTexte texte,
                                                            Bildmass? mass = null)
        {
            texte = texte ?? new SpannenTexte();
            int W = mass.HasValue ? Math.Max(SPANNE_MIN_BREITE, Bildmass.BreiteOder(mass, 1240)) : 1240;
            string titel = (texte.Titel ?? "") + "  [€]";

            var gueltig = new List<Spannenbalken>();
            if (balken != null)
                foreach (Spannenbalken b in balken)
                    if (b != null && b.Zeichenbar) gueltig.Add(b);

            if (gueltig.Count == 0)
            {
                var leer = Modell(W, 200);
                leer.Markiert("titel", zt => Titel(zt, titel, W, mass.HasValue));
                using (var f = Schrift(18f))
                {
                    List<string> zeilen = Umbruchzeilen(texte.Leer ?? "", f, W - 150f, 3);
                    leer.Markiert("leerhinweis", zl =>
                    {
                        for (int i = 0; i < zeilen.Count; i++)
                            Text(zl, zeilen[i], f, Farbrolle.ACHSE, 110f, 80f + i * (TextHoehe(f) + 6f));
                    });
                }
                return leer;
            }

            // Die Beschriftungsspalte: so breit wie der längste Name, in Grenzen.
            float links;
            using (var lf = Schrift(17f))
            {
                float laengster = 0f;
                foreach (Spannenbalken b in gueltig) laengster = Math.Max(laengster, lf.MeasureText(b.Name ?? ""));
                links = Math.Max(220f, Math.Min(Math.Min(480f, W * 0.39f), laengster + 64f));
            }
            float rechts = W - 80f;
            float oben = 70f;                                    // Oberkante der Zeichenfläche
            float erste = oben + 50f;                            // Mitte der ersten Zeile
            float unten = erste + (gueltig.Count - 1) * SPANNE_ZEILE + 40f;
            int H = (int)(unten + 130f);

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel, W, mass.HasValue));

            // Die Skala schließt die Null (die Referenz) immer ein.
            double lo = 0.0, hi = 0.0;
            foreach (Spannenbalken b in gueltig)
                foreach (double? w in new[] { b.Von, b.Bis, b.Punkt })
                    if (w.HasValue) { lo = Math.Min(lo, w.Value); hi = Math.Max(hi, w.Value); }
            SchoeneStufen(ref lo, ref hi, out double schritt);
            float X(double w) => links + (float)((w - lo) / (hi - lo)) * (rechts - links);

            // Raster und Beschriftung der Euro-Achse.
            var raster = Stift(Farbrolle.RASTER, 1f);
            int stufen = (int)Math.Round((hi - lo) / schritt);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (int k = 0; k <= stufen; k++)
                    {
                        double wert = lo + k * schritt;
                        if (Math.Abs(wert) < schritt * 1e-9) wert = 0.0;   // keine „-0"
                        float x = X(wert);
                        zx.Linie(x, oben, x, unten, raster);
                        string lab = wert.ToString("N0", DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, unten + 8f);
                    }
            });
            using (var f = Schrift(15f))
            {
                string achse = string.Format(CultureInfo.InvariantCulture, texte.Achse ?? "",
                                             string.IsNullOrEmpty(referenz) ? texte.ReferenzUnbenannt : referenz);
                z.Markiert("xachse", zx =>
                    Text(zx, achse, f, Farbrolle.ACHSE,
                         links + (rechts - links - f.MeasureText(achse)) / 2f, unten + 34f));
            }

            // Die Grundlinie bleibt ohne Marke (Regel des Achsenkreuzes); die Nulllinie ist
            // die Referenz und trägt ihre Marke.
            z.Linie(links, unten, rechts, unten, Stift(Farbrolle.ACHSE, 1f));
            float x0 = X(0.0);
            z.Markiert("nulllinie", zn => zn.Linie(x0, oben - 6f, x0, unten, Stift(Farbrolle.ACHSE, 2f)));

            // Je Version eine Zeile: Name, Balken samt Enden, Punkt und Betrag.
            var band = Flaeche(Farbton.Aus(Farbrolle.STAMM).MitDeckung(SPANNE_BAND_DECKUNG));
            var ring = Stift(Farbrolle.HINTERGRUND, 2f);
            bool unter = false;
            using (var lf = Schrift(17f))
            using (var wf = Schrift(15f, fett: true))
            {
                for (int i = 0; i < gueltig.Count; i++)
                {
                    Spannenbalken b = gueltig[i];
                    float y = erste + i * SPANNE_ZEILE;
                    string name = b.Name ?? "";
                    float nb = lf.MeasureText(name);
                    double? von = b.Von, bis = b.Bis, punkt = b.Punkt;
                    if ((von.HasValue && von.Value < 0.0) || (punkt.HasValue && punkt.Value < 0.0)) unter = true;

                    z.Markiert("reihe:" + name, Spannenwert(b, texte), zr =>
                    {
                        Text(zr, name, lf, Farbrolle.TEXT, links - 16f - nb, y - TextHoehe(lf) / 2f);
                        if (von.HasValue && bis.HasValue)
                        {
                            float x1 = X(von.Value), x2 = X(bis.Value);
                            zr.Rechteck(x1, y - 11f, Math.Max(x2 - x1, 1f), 22f, null, band);
                            zr.Linie(x1, y - 17f, x1, y + 17f, Stift(Vorzeichenrolle(von.Value), 3f));
                            zr.Linie(x2, y - 17f, x2, y + 17f, Stift(Vorzeichenrolle(bis.Value), 3f));
                        }
                        if (punkt.HasValue)
                        {
                            float xe = X(punkt.Value);
                            Farbrolle rolle = Vorzeichenrolle(punkt.Value);
                            zr.Kreis(xe, y, 9f, ring, Flaeche(rolle));
                            string betrag = punkt.Value.ToString("N0", DE) + " €";
                            float bb = wf.MeasureText(betrag);
                            float bx = Math.Max(links, Math.Min(rechts - bb, xe - bb / 2f));
                            Text(zr, betrag, wf, rolle, bx, y - 20f - TextHoehe(wf));
                        }
                    });
                }
            }

            // Die Legende: Balken, Punkt und — nur wenn es ihn gibt — der Wert unter null.
            float ly = unten + 76f;
            using (var f = Schrift(16f))
            {
                string spanne = texte.Spanne ?? "", erwartet = texte.Erwartungsfall ?? "",
                       unterText = texte.UnterReferenz ?? "";
                bool mitUnter = unter;
                z.Markiert("legende", zl =>
                {
                    float ex = 110f;
                    zl.Rechteck(ex, ly + 5f, 40f, 12f, null, band);
                    zl.Linie(ex, ly + 1f, ex, ly + 21f, Stift(Farbrolle.RASTER_GUT, 3f));
                    zl.Linie(ex + 40f, ly + 1f, ex + 40f, ly + 21f, Stift(Farbrolle.RASTER_GUT, 3f));
                    Text(zl, spanne, f, Farbrolle.TEXT, ex + 50f, ly + 1f);
                    ex += 50f + f.MeasureText(spanne) + 32f;

                    zl.Kreis(ex + 11f, ly + 11f, 8f, ring, Flaeche(Farbrolle.RASTER_GUT));
                    Text(zl, erwartet, f, Farbrolle.TEXT, ex + 28f, ly + 1f);
                    ex += 28f + f.MeasureText(erwartet) + 32f;

                    if (mitUnter)
                    {
                        zl.Kreis(ex + 11f, ly + 11f, 8f, ring, Flaeche(Farbrolle.RASTER_SCHLECHT));
                        Text(zl, unterText, f, Farbrolle.TEXT, ex + 28f, ly + 1f);
                    }
                });
            }
            return z;
        }

        /// <summary>Die Farbe eines Werts im Spannenbild: unter der Referenz rot, sonst grün.</summary>
        private static Farbrolle Vorzeichenrolle(double wert)
            => wert < 0.0 ? Farbrolle.RASTER_SCHLECHT : Farbrolle.RASTER_GUT;

        /// <summary>Der Wert am Element einer Zeile: „BHKW: Ungünstig 1.506.740 € · Erwartet
        /// 1.660.205 € · Günstig 1.811.714 €" — „—" für einen fehlenden Wert.</summary>
        private static string Spannenwert(Spannenbalken b, SpannenTexte t)
        {
            string Betrag(double? x)
            {
                double? w = EndlicherWert(x);
                return w.HasValue ? w.Value.ToString("N0", DE) + " €" : "—";
            }
            return (b.Name ?? "") + ": " + t.Worst + " " + Betrag(b.Worst) + " · " +
                   t.Erwartet + " " + Betrag(b.Erwartet) + " · " + t.Best + " " + Betrag(b.Best);
        }

        /// <summary>Ein Wert, wenn er endlich ist — sonst <c>null</c> (nicht endliche Werte
        /// fallen weg, statt das Bild zu Fall zu bringen).</summary>
        private static double? EndlicherWert(double? x)
            => x.HasValue && !double.IsNaN(x.Value) && !double.IsInfinity(x.Value) ? x : null;

        // ============ Brücke von der Investition zur Kapitalwertdifferenz (E8a, U41)

        /// <summary>
        /// ETAPPE E8a (U41, Mockup Kategorie 8 „Woraus entsteht die Zahl?") — EIN Schritt des
        /// Brückenbilds: ein Bestandteil der Kapitalwertdifferenz mit seinem Namen und seinem
        /// Beitrag [€] — der Differenz der Barwerte, Stand minus Referenz. Negativ mindert,
        /// positiv mehrt die Differenz; ein nicht endlicher Wert fällt weg.
        /// </summary>
        public sealed class Brueckenschritt
        {
            /// <summary>Der Name des Bestandteils — die Beschriftung unter der Säule.</summary>
            public string Name { get; set; } = "";

            /// <summary>Der Beitrag zur Kapitalwertdifferenz [€].</summary>
            public double Wert { get; set; }

            /// <summary>
            /// Die sechs Schritte der Differenz zweier Gliederungen (Stand − Referenz) in der
            /// Reihenfolge der Gliederung — Investition, Betriebskosten, Energiekosten, Erlöse,
            /// Ersatzbeschaffungen, Restwert; dieselben Zahlen wie die Differenzspalte der Seite
            /// (<see cref="Zahlungsgliederung.Differenz"/>). Leer, wenn eine Seite fehlt.
            /// </summary>
            public static List<Brueckenschritt> Aus(Zahlungsgliederung stand, Zahlungsgliederung referenz)
            {
                var liste = new List<Brueckenschritt>();
                Zahlungsgliederung d = Zahlungsgliederung.Differenz(stand, referenz);
                if (d == null) return liste;
                // ETAPPE E15: mit Risikoabzug ein siebter Schritt vor dem Restwert.
                foreach (string s in d.Schluessel)
                    liste.Add(new Brueckenschritt { Name = Zahlungsgliederung.Titel(s), Wert = d.Bestandteil(s).Barwert });
                return liste;
            }
        }

        /// <summary>
        /// ETAPPE E8a (U41) — die Texte des Brückenbilds. Die Vorgaben sind der deutsche Wortlaut
        /// (die ChartProben hängen so nicht an der Sprache des Rechners);
        /// <see cref="AusRessourcen"/> liest die Oberflächensprache. Unterzeile und Fuß nennen
        /// Stand, Referenz, Szenario, Zins und Zeitraum — der Aufrufer bildet sie.
        /// </summary>
        public sealed class BrueckenTexte
        {
            /// <summary>Die Überschrift (<c>WIRT_BR_TITEL</c>); „[€]" hängt das Bild an.</summary>
            public string Titel { get; set; } = "Von der Investition zur Kapitalwertdifferenz";

            /// <summary>Die Zeile unter der Überschrift (<c>WIRT_BR_UNTER</c>, vom Aufrufer gefüllt).</summary>
            public string Unterzeile { get; set; } = "";

            /// <summary>Die Zeile unter den Säulen (<c>WIRT_BR_FUSS</c>, vom Aufrufer gefüllt).</summary>
            public string Fuss { get; set; } = "";

            /// <summary>Der Name der Ergebnissäule (<c>WIRT_BR_ERGEBNIS</c>).</summary>
            public string Ergebnis { get; set; } = "ΔKW";

            /// <summary>Der Legendeneintrag der roten Säulen (<c>WIRT_BR_LEG_MINDERT</c>).</summary>
            public string Mindert { get; set; } = "mindert die Differenz";

            /// <summary>Der Legendeneintrag der grünen Säulen (<c>WIRT_BR_LEG_MEHRT</c>).</summary>
            public string Mehrt { get; set; } = "mehrt die Differenz";

            /// <summary>Der Legendeneintrag der Ergebnissäule (<c>WIRT_BR_LEG_ERGEBNIS</c>).</summary>
            public string ErgebnisLegende { get; set; } = "Ergebnis";

            /// <summary>Der Leerhinweis ohne zeichenbaren Schritt (<c>WIRT_BR_LEER</c>).</summary>
            public string Leer { get; set; } = "Keine Differenz zu zeichnen — Stand oder Referenz ohne Zahlungsreihe.";

            /// <summary>Dieselben Texte in der Oberflächensprache (<c>MyResource</c>).</summary>
            public static BrueckenTexte AusRessourcen(string unterzeile, string fuss)
            {
                return new BrueckenTexte
                {
                    Titel = MyResource.Resource.WIRT_BR_TITEL,
                    Unterzeile = unterzeile ?? "",
                    Fuss = fuss ?? "",
                    Ergebnis = MyResource.Resource.WIRT_BR_ERGEBNIS,
                    Mindert = MyResource.Resource.WIRT_BR_LEG_MINDERT,
                    Mehrt = MyResource.Resource.WIRT_BR_LEG_MEHRT,
                    ErgebnisLegende = MyResource.Resource.WIRT_BR_LEG_ERGEBNIS,
                    Leer = MyResource.Resource.WIRT_BR_LEER
                };
            }

            /// <summary>
            /// Die Texte für die Brücke eines Standes gegen die Referenz in einem Szenario —
            /// Unterzeile („‹Stand› gegenüber ‹Referenz› · Barwerte · Szenario ‹S›") und Fuß
            /// (Referenz, Szenario, Zins, Zeitraum der Gliederung) aus <c>MyResource</c>. Seite
            /// und Wortbericht bilden sie hier, damit beide dasselbe sagen.
            /// </summary>
            public static BrueckenTexte Fuer(string stand, string referenz, string szenario,
                                             Zahlungsgliederung gliederung, CultureInfo kultur)
            {
                if (kultur == null) kultur = CultureInfo.CurrentCulture;
                string unterzeile = string.Format(kultur, MyResource.Resource.WIRT_BR_UNTER,
                                                  stand ?? "", referenz ?? "", szenario ?? "");
                string fuss = gliederung == null ? ""
                    : string.Format(kultur, MyResource.Resource.WIRT_BR_FUSS, referenz ?? "", szenario ?? "",
                                    gliederung.ZinsProzent.ToString("N1", kultur),
                                    gliederung.Jahre.ToString(CultureInfo.InvariantCulture));
                return AusRessourcen(unterzeile, fuss);
            }
        }

        /// <summary>Die Breite des Brückenbilds [px] — wie Spannenbild und Verlauf.</summary>
        public const int BRUECKE_BREITE = 1240;

        /// <summary>Die Höhe des Brückenbilds [px] (ohne zeichenbaren Schritt: 200).</summary>
        public const int BRUECKE_HOEHE = 610;

        /// <summary>Die Beträge der Schritte — mit Vorzeichen („+2.606.605", „−426.922").</summary>
        private const string BRUECKE_GELD = "+#,##0;−#,##0;0";

        /// <summary>
        /// ETAPPE E8a — das Brückenbild als PNG (Wortbericht). Siehe
        /// <see cref="KapitalwertBrueckeModell"/>.
        /// </summary>
        public static byte[] KapitalwertBruecke(IReadOnlyList<Brueckenschritt> schritte, BrueckenTexte texte)
            => SkiaMaler.Png(KapitalwertBrueckeModell(schritte, texte));

        /// <summary>
        /// ETAPPE E8a (U41, Anwenderentscheid E5b‑4 vom 22.09.2026; Mockup Kategorie 8 „Von der
        /// Investition zur Kapitalwertdifferenz") — die <b>Brücke</b> als Wasserfall: je
        /// Bestandteil eine Säule vom Stand vor bis zum Stand nach dem Schritt, rot, wenn er die
        /// Differenz mindert, grün, wenn er sie mehrt, gestrichelt verbunden auf der Höhe des
        /// neuen Standes; zuletzt die Ergebnissäule von null bis zur Summe in der Hausfarbe. Die
        /// Summe der Schritte IST die Kapitalwertdifferenz — das Bild rechnet nichts, es stapelt.
        ///
        /// <para><b>Die Achse</b> zählt Euro und schließt die Null immer ein; die Nulllinie ist
        /// die Referenz und trägt ihre Marke. Die Stufen sind die „schönen" Stufen des
        /// Spannenbilds (etwa fünf Rasterlinien).</para>
        ///
        /// <para><b>Ein reines Pixelbild</b> wie das Spannenbild: keine Zeichenfläche, keine
        /// Datenreihe. Eine SÄULE ist das Datenelement — Säule und Betrag stehen in der Klammer
        /// <c>reihe:&lt;Name&gt;</c> und tragen den Betrag am Element. Die Legende schaltet
        /// nichts (Marke <c>legende</c> ohne Namen).</para>
        ///
        /// <para><b>Das Bildmaß:</b> 1240 × 610; ohne zeichenbaren Schritt 1240 × 200 mit dem
        /// Leerhinweis. Deterministisch: dieselben Schritte, dasselbe Bild.</para>
        /// </summary>
        /// <param name="schritte">Die Bestandteile in ihrer Reihenfolge (<see cref="Brueckenschritt.Aus"/>).</param>
        /// <param name="texte">Überschrift, Unterzeile, Fuß, Legende; <c>null</c> = die Vorgabe.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 610 wie bisher.</param>
        public static Zeichenmodell KapitalwertBrueckeModell(IReadOnlyList<Brueckenschritt> schritte,
                                                             BrueckenTexte texte, Bildmass? mass = null)
        {
            texte = texte ?? new BrueckenTexte();
            int W = mass.HasValue ? Math.Max(BRUECKE_MIN_BREITE, Bildmass.BreiteOder(mass, BRUECKE_BREITE)) : BRUECKE_BREITE;
            int H = mass.HasValue ? Math.Max((int)(250f + STUFE2_MIN_FLAECHE), Bildmass.HoeheOder(mass, BRUECKE_HOEHE))
                                  : BRUECKE_HOEHE;
            string titel = (texte.Titel ?? "") + "  [€]";

            var gueltig = new List<Brueckenschritt>();
            if (schritte != null)
                foreach (Brueckenschritt s in schritte)
                    if (s != null && EndlicherWert(s.Wert).HasValue) gueltig.Add(s);

            if (gueltig.Count == 0)
            {
                var leer = Modell(W, 200);
                leer.Markiert("titel", zt => Titel(zt, titel, W, mass.HasValue));
                using (var f = Schrift(18f))
                {
                    List<string> zeilen = Umbruchzeilen(texte.Leer ?? "", f, W - 150f, 3);
                    leer.Markiert("leerhinweis", zl =>
                    {
                        for (int i = 0; i < zeilen.Count; i++)
                            Text(zl, zeilen[i], f, Farbrolle.ACHSE, 110f, 80f + i * (TextHoehe(f) + 6f));
                    });
                }
                return leer;
            }

            float links = 150f, rechts = W - 40f, oben = 110f, unten = H - 140f;
            var z = Modell(W, H);
            string unterzeile = texte.Unterzeile ?? "";
            z.Markiert("titel", zt =>
            {
                Titel(zt, titel, W, mass.HasValue);
                using (var f = Schrift(15f))
                    Text(zt, unterzeile, f, Farbrolle.ACHSE, 24f, 54f);
            });

            // Die Treppe: je Schritt der Stand davor; die Skala schließt Null und jeden Stand ein.
            var davor = new double[gueltig.Count];
            double stand = 0.0, lo = 0.0, hi = 0.0;
            for (int i = 0; i < gueltig.Count; i++)
            {
                davor[i] = stand;
                stand += gueltig[i].Wert;
                lo = Math.Min(lo, stand);
                hi = Math.Max(hi, stand);
            }
            double summe = stand;
            SchoeneStufen(ref lo, ref hi, out double schritt);
            float Y(double w) => unten - (float)((w - lo) / (hi - lo)) * (unten - oben);

            // Raster und Beschriftung der Euro-Achse.
            var raster = Stift(Farbrolle.RASTER, 1f);
            int stufen = (int)Math.Round((hi - lo) / schritt);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (int k = 0; k <= stufen; k++)
                    {
                        double wert = lo + k * schritt;
                        if (Math.Abs(wert) < schritt * 1e-9) wert = 0.0;   // keine „-0"
                        float y = Y(wert);
                        zy.Linie(links, y, rechts, y, raster);
                        string lab = wert.ToString("N0", DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, links - 12f - f.MeasureText(lab), y - TextHoehe(f) / 2f);
                    }
            });
            float y0 = Y(0.0);
            z.Markiert("nulllinie", zn => zn.Linie(links, y0, rechts, y0, Stift(Farbrolle.ACHSE, 2f)));

            int n = gueltig.Count + 1;
            float platz = (rechts - links) / n;
            float breite = platz * 0.62f;
            var verbinder = Stift(Farbrolle.ACHSE, 1f, new Strichmuster(4f, 3f));

            using (var wf = Schrift(15f))
            using (var ef = Schrift(16f, fett: true))
            {
                for (int i = 0; i < gueltig.Count; i++)
                {
                    Brueckenschritt s = gueltig[i];
                    double a = davor[i], b = a + s.Wert;
                    float x = links + i * platz + (platz - breite) / 2f;
                    float yo = Y(Math.Max(a, b)), yu = Y(Math.Min(a, b));
                    Farbrolle rolle = s.Wert < 0.0 ? Farbrolle.RASTER_SCHLECHT : Farbrolle.RASTER_GUT;
                    string betrag = s.Wert.ToString(BRUECKE_GELD, DE);
                    string name = s.Name ?? "";
                    z.Markiert("reihe:" + name, name + ": " + betrag + " €", zr =>
                    {
                        zr.Rechteck(x, yo, breite, Math.Max(yu - yo, 1f), null, Flaeche(rolle));
                        float bb = wf.MeasureText(betrag);
                        Text(zr, betrag, wf, Farbrolle.ACHSE, x + (breite - bb) / 2f, yo - 6f - TextHoehe(wf));
                    });

                    // Die Verbindung zur nächsten Säule, auf der Höhe des neuen Standes.
                    float yb = Y(b);
                    z.Linie(x + breite, yb, x + platz, yb, verbinder);
                }

                // Die Ergebnissäule: von null bis zur Summe, in der Hausfarbe.
                float xe = links + gueltig.Count * platz + (platz - breite) / 2f;
                float yeo = Y(Math.Max(0.0, summe)), yeu = Y(Math.Min(0.0, summe));
                string ergebnis = summe.ToString("#,##0;−#,##0;0", DE);
                string ergName = texte.Ergebnis ?? "";
                z.Markiert("reihe:" + ergName, ergName + ": " + ergebnis + " €", zr =>
                {
                    zr.Rechteck(xe, yeo, breite, Math.Max(yeu - yeo, 1f), null, Flaeche(Farbrolle.STAMM));
                    float eb = ef.MeasureText(ergebnis);
                    Text(zr, ergebnis, ef, Farbrolle.STAMM, xe + (breite - eb) / 2f, yeo - 6f - TextHoehe(ef));
                });
            }

            // Die Namen unter den Säulen (höchstens zwei Zeilen) und der Fuß.
            string fuss = texte.Fuss ?? "";
            string ergebnisname = texte.Ergebnis ?? "";
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                using (var ff = Schrift(15f, fett: true))
                {
                    for (int i = 0; i <= gueltig.Count; i++)
                    {
                        bool letzte = i == gueltig.Count;
                        Schriftmass fs = letzte ? ff : f;
                        List<string> zeilen = Umbruchzeilen(letzte ? ergebnisname : (gueltig[i].Name ?? ""),
                                                            fs, platz - 8f, 2);
                        float mitte = links + i * platz + platz / 2f;
                        for (int k = 0; k < zeilen.Count; k++)
                            Text(zx, zeilen[k], fs, letzte ? Farbrolle.TEXT : Farbrolle.ACHSE,
                                 mitte - fs.MeasureText(zeilen[k]) / 2f, unten + 10f + k * (TextHoehe(fs) + 2f));
                    }
                    Text(zx, fuss, f, Farbrolle.ACHSE, links, unten + 66f);
                }
            });

            // Die Legende: mindert, mehrt, Ergebnis — sie schaltet nichts.
            float ly = unten + 100f;
            using (var f = Schrift(16f))
            {
                var eintraege = new[]
                {
                    (Farbrolle.RASTER_SCHLECHT, texte.Mindert ?? ""),
                    (Farbrolle.RASTER_GUT, texte.Mehrt ?? ""),
                    (Farbrolle.STAMM, texte.ErgebnisLegende ?? "")
                };
                z.Markiert("legende", zl =>
                {
                    float ex = links;
                    foreach ((Farbrolle rolle, string text) in eintraege)
                    {
                        zl.Rechteck(ex, ly + 4f, 26f, 14f, null, Flaeche(rolle));
                        Text(zl, text, f, Farbrolle.TEXT, ex + 34f, ly + 1f);
                        ex += 34f + f.MeasureText(text) + 32f;
                    }
                });
            }
            return z;
        }

        /// <summary>
        /// „Schöne" Stufen für eine Wertachse mit etwa fünf Rasterlinien — derselbe Weg wie
        /// in <see cref="VerlaufAchsen"/>: Schritt 1, 2, 2,5, 5 oder 10 mal einer
        /// Zehnerpotenz, die Grenzen auf ganze Schritte erweitert.
        /// </summary>
        private static void SchoeneStufen(ref double unten, ref double oben, out double schritt)
        {
            if (oben - unten < 1e-9) oben = unten + 1.0;
            double roh = (oben - unten) / 5.0;
            double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
            schritt = zehner;
            foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (zehner * f >= roh) { schritt = zehner * f; break; }
            unten = Math.Floor(unten / schritt) * schritt;
            oben = Math.Ceiling(oben / schritt) * schritt;
        }

        // ============================================ Zahlungsstrom je Jahr (E8a, U42)

        /// <summary>
        /// ETAPPE E8a (U42, Anwenderentscheid E8a‑Q1 vom 23.09.2026, Lesart a) — EINE Reihe des
        /// Zahlungsstrombilds: eine Positionsspalte der Mehrjahrestafel mit ihrem Schlüssel,
        /// ihrem Namen und ihren nominalen Beträgen je Jahr 0…T [€], Ausgaben negativ.
        /// </summary>
        public sealed class Zahlungsstromreihe
        {
            /// <summary>Die Spalte der Mehrjahrestafel, die im Jahr 0 die Investition und danach
            /// die Ersatzbeschaffungen trägt.</summary>
            public const string INVEST_ERSATZ = "INVEST_ERSATZ";

            /// <summary>Der sprachneutrale Schlüssel der Spalte (<see cref="MehrjahresSpalte.Schluessel"/>);
            /// er wählt die Farbe.</summary>
            public string Schluessel { get; set; } = "";

            /// <summary>Der Name der Spalte — Legende und Wert am Element.</summary>
            public string Name { get; set; } = "";

            /// <summary>Die nominalen Beträge je Jahr [€], Index 0…T; Ausgaben negativ.</summary>
            public double[] JeJahr { get; set; } = Array.Empty<double>();

            /// <summary>
            /// Die Positionsspalten einer Mehrjahrestafel in ihrer Reihenfolge — dieselben
            /// Spalten, Namen und Beträge wie die Tafel des Berichts, ohne die Summenspalten
            /// (Netto, Barwert, kumuliert). Leer ohne Tafel.
            /// </summary>
            public static List<Zahlungsstromreihe> Aus(Mehrjahresbild bild)
            {
                var liste = new List<Zahlungsstromreihe>();
                if (bild == null) return liste;
                foreach (MehrjahresSpalte s in bild.Spalten)
                    if (s != null && !s.IstSumme && s.JeJahr != null)
                        liste.Add(new Zahlungsstromreihe
                        {
                            Schluessel = s.Schluessel ?? "", Name = s.Titel ?? "", JeJahr = (double[])s.JeJahr.Clone()
                        });
                return liste;
            }

            /// <summary>
            /// Die Jahre mit einer Ersatzbeschaffung, aufsteigend — die Jahre nach dem Jahr 0,
            /// in denen die Spalte „Investition und Ersatz" einen Betrag trägt (dieselben Jahre
            /// wie in „Was daraus im Lauf wird"). Leer ohne Tafel.
            /// </summary>
            public static List<int> Ersatzjahre(Mehrjahresbild bild)
            {
                var jahre = new List<int>();
                MehrjahresSpalte s = bild == null ? null
                    : bild.Spalten.FirstOrDefault(x => x != null && x.Schluessel == INVEST_ERSATZ);
                if (s == null || s.JeJahr == null) return jahre;
                for (int t = 1; t < s.JeJahr.Length; t++)
                    if (s.JeJahr[t] != 0.0) jahre.Add(t);
                return jahre;
            }
        }

        /// <summary>
        /// ETAPPE E8a (U42) — die Texte des Zahlungsstrombilds. Die Vorgaben sind der deutsche
        /// Wortlaut (die ChartProben hängen so nicht an der Sprache des Rechners);
        /// <see cref="AusRessourcen"/> liest die Oberflächensprache.
        /// </summary>
        public sealed class ZahlungsstromTexte
        {
            /// <summary>Die Überschrift (<c>WIRT_ZS_TITEL</c>); „[€]" hängt das Bild an.</summary>
            public string Titel { get; set; } = "Zahlungsstrom je Jahr";

            /// <summary>Die Zeile unter der Überschrift (<c>WIRT_ZS_UNTER</c>, vom Aufrufer gefüllt).</summary>
            public string Unterzeile { get; set; } = "";

            /// <summary>Die Beschriftung der Jahresachse und der Werte am Element (<c>WIRT_MJ_JAHR</c>).</summary>
            public string Jahr { get; set; } = "Jahr";

            /// <summary>Der Schlüssel der Ersatzjahr-Marke unter der Achse (<c>WIRT_ZS_ERSATZJAHR</c>).</summary>
            public string Ersatzjahr { get; set; } = "Ersatzjahr";

            /// <summary>Der Leerhinweis ohne zeichenbaren Betrag (<c>WIRT_ZS_LEER</c>).</summary>
            public string Leer { get; set; } = "Kein Zahlungsstrom zu zeichnen — der Stand trägt keine Jahresreihe.";

            /// <summary>Dieselben Texte in der Oberflächensprache (<c>MyResource</c>).</summary>
            public static ZahlungsstromTexte AusRessourcen(string unterzeile)
            {
                return new ZahlungsstromTexte
                {
                    Titel = MyResource.Resource.WIRT_ZS_TITEL,
                    Unterzeile = unterzeile ?? "",
                    Jahr = MyResource.Resource.WIRT_MJ_JAHR,
                    Ersatzjahr = MyResource.Resource.WIRT_ZS_ERSATZJAHR,
                    Leer = MyResource.Resource.WIRT_ZS_LEER
                };
            }

            /// <summary>
            /// Die Texte für den Zahlungsstrom eines Standes in einem Szenario — die Unterzeile
            /// („‹Stand› · Szenario ‹S› · nominal je Jahr, Ausgaben nach unten, ohne Restwert")
            /// aus <c>MyResource</c>. Seite und Wortbericht bilden sie hier, damit beide
            /// dasselbe sagen.
            /// </summary>
            public static ZahlungsstromTexte Fuer(string stand, string szenario, CultureInfo kultur)
            {
                if (kultur == null) kultur = CultureInfo.CurrentCulture;
                return AusRessourcen(string.Format(kultur, MyResource.Resource.WIRT_ZS_UNTER,
                                                   stand ?? "", szenario ?? ""));
            }
        }

        /// <summary>Die Breite des Zahlungsstrombilds [px] — wie Brücke, Spannenbild und Verlauf.</summary>
        public const int ZAHLUNGSSTROM_BREITE = 1240;

        /// <summary>Die Höhe des Zahlungsstrombilds [px] (ohne zeichenbaren Betrag: 200). Eine
        /// umbrechende Legende nimmt ihren Platz der Zeichenfläche, nicht dem Bild.</summary>
        public const int ZAHLUNGSSTROM_HOEHE = 620;

        /// <summary>Die Beträge eines Jahres am Element — wie in der Jahrestafel darüber
        /// („−182.000", „64.000").</summary>
        private const string ZAHLUNGSSTROM_GELD = "#,##0;−#,##0;0";

        /// <summary>Der Ton der Stromsteuer-Entlastung — ein helles Blau neben dem Blau der
        /// Energiesteuer und dem Violett der Stromsteuer-Befreiung.</summary>
        private static readonly SKColor C_ZAHLUNGSSTROM_HELLBLAU = new SKColor(0x5B, 0x9B, 0xD5);

        /// <summary>
        /// Die Farbe einer Reihe des Zahlungsstrombilds — FEST je Spalte, damit dieselbe
        /// Position in jedem Stand und jedem Szenario dieselbe Farbe trägt: die Investition in
        /// der Hausfarbe, Betrieb, Energie und CO₂-Abgabe warm und grau, die Erlöse grün und
        /// blau. Eine unbekannte Spalte nimmt die Serienpalette nach ihrer Stelle.
        /// </summary>
        private static SKColor Zahlungsstromfarbe(string schluessel, int stelle)
        {
            switch (schluessel)
            {
                case Zahlungsstromreihe.INVEST_ERSATZ: return C_STAMM;
                case "BETRIEB": return C_SERIEN[0];                                              // Orange
                case "ENERGIE": return C_SERIEN[6];                                              // Rot
                case "BEHG": return C_KESSEL;                                                    // Grau
                case "EINSPEISUNG": return C_SERIEN[1];                                          // Grün
                case KapitalwertRechner.ErloesReihe.KWKG: return C_SERIEN[5];                    // Petrol
                case KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE: return C_SERIEN[3];          // Braun
                case KapitalwertRechner.ErloesReihe.ENERGIESTEUER: return C_SERIEN[2];           // Blau
                case KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG: return C_SERIEN[4];   // Violett
                case KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG: return C_ZAHLUNGSSTROM_HELLBLAU;
                case KapitalwertRechner.ErloesReihe.PV_VERGUETUNG: return C_SERIEN[7];           // Ocker
                default: return C_SERIEN[Math.Max(0, stelle) % C_SERIEN.Length];
            }
        }

        /// <summary>Der Betrag einer Reihe im Jahr <paramref name="t"/>; ein fehlender oder
        /// nicht endlicher Betrag ist 0 (er fällt weg).</summary>
        private static double Zahlungsbetrag(Zahlungsstromreihe r, int t)
        {
            if (r == null || r.JeJahr == null || t < 0 || t >= r.JeJahr.Length) return 0.0;
            double? w = EndlicherWert(r.JeJahr[t]);
            return w.HasValue ? w.Value : 0.0;
        }

        /// <summary>
        /// ETAPPE E8a — das Zahlungsstrombild als PNG (Wortbericht). Siehe
        /// <see cref="ZahlungsstromModell"/>.
        /// </summary>
        public static byte[] Zahlungsstrom(IReadOnlyList<Zahlungsstromreihe> reihen, IReadOnlyList<int> ersatzjahre,
                                           ZahlungsstromTexte texte)
            => SkiaMaler.Png(ZahlungsstromModell(reihen, ersatzjahre, texte));

        /// <summary>
        /// ETAPPE E8a (U42, Anwenderentscheid E8a‑Q1 vom 23.09.2026, Lesart a) — der
        /// <b>Zahlungsstrom je Jahr</b> EINER Version in EINEM Szenario als gestapelte
        /// Jahresbalken: je Jahr 0…T die Positionen der Mehrjahrestafel, die Einnahmen von der
        /// Nulllinie nach oben, die Ausgaben nach unten, beide in der Reihenfolge der Tafel. Es
        /// ist der absolute Strom der Version — keine Differenz zu einer Referenz; die Summe
        /// eines Balkens über und unter der Null ist das Netto des Jahres ohne Restwert.
        ///
        /// <para><b>Die Ersatzjahre sind der GRUND, nicht eine Reihe</b> — dieselbe Marke wie
        /// in der Jahresprojektion der Speicherflotte: ein senkrechtes Band hinter dem Balken
        /// ihres Jahres und ein Dreieck am oberen Rand; unter der Achse steht ihr Schlüssel. Ihr
        /// Betrag steht als Teil der Spalte „Investition und Ersatz" im Balken.</para>
        ///
        /// <para><b>Ein reines Pixelbild</b> wie Brücke und Spannenbild: keine Zeichenfläche,
        /// keine Datenreihe. Jede Stapelschicht ist ein Datenelement mit der Marke
        /// <c>reihe:&lt;Spalte&gt;</c> und dem Wert „Jahr 5 · Energiekosten: −182.000 €"; die
        /// Legende nennt die Spalten in ihrer Farbe (fest je Spalte). Die Achse zählt Euro und
        /// schließt die Null immer ein, in den „schönen" Stufen der Brücke.</para>
        ///
        /// <para><b>Das Bildmaß:</b> 1240 × 620; ohne zeichenbaren Betrag 1240 × 200 mit dem
        /// Leerhinweis. Deterministisch: dieselben Reihen, dasselbe Bild.</para>
        /// </summary>
        /// <param name="reihen">Die Positionen in der Reihenfolge der Tafel (<see cref="Zahlungsstromreihe.Aus"/>).</param>
        /// <param name="ersatzjahre">Die Jahre mit Ersatzbeschaffung (<see cref="Zahlungsstromreihe.Ersatzjahre"/>);
        /// <c>null</c> = keine Marke.</param>
        /// <param name="texte">Überschrift, Unterzeile, Achse, Marke; <c>null</c> = die Vorgabe.</param>
        /// <param name="mass">Stufe 2 (BV-E5): das Zielmaß; <c>null</c> = 1240 × 620 wie bisher.</param>
        public static Zeichenmodell ZahlungsstromModell(IReadOnlyList<Zahlungsstromreihe> reihen,
                                                        IReadOnlyList<int> ersatzjahre,
                                                        ZahlungsstromTexte texte, Bildmass? mass = null)
        {
            texte = texte ?? new ZahlungsstromTexte();
            int W = Bildmass.BreiteOder(mass, ZAHLUNGSSTROM_BREITE), H = Bildmass.HoeheOder(mass, ZAHLUNGSSTROM_HOEHE);
            string titel = (texte.Titel ?? "") + "  [€]";
            string jahrText = texte.Jahr ?? "";

            // Gezeichnet werden die Reihen mit wenigstens einem endlichen Betrag ungleich 0.
            var gueltig = new List<Zahlungsstromreihe>();
            int n = 0;
            if (reihen != null)
                foreach (Zahlungsstromreihe r in reihen)
                {
                    if (r == null || r.JeJahr == null) continue;
                    bool belegt = false;
                    for (int t = 0; t < r.JeJahr.Length && !belegt; t++)
                        belegt = Zahlungsbetrag(r, t) != 0.0;
                    if (!belegt) continue;
                    gueltig.Add(r);
                    n = Math.Max(n, r.JeJahr.Length);
                }

            if (gueltig.Count == 0)
            {
                var leer = Modell(W, 200);
                leer.Markiert("titel", zt => Titel(zt, titel, W, mass.HasValue));
                using (var f = Schrift(18f))
                {
                    List<string> zeilen = Umbruchzeilen(texte.Leer ?? "", f, W - 150f, 3);
                    leer.Markiert("leerhinweis", zl =>
                    {
                        for (int i = 0; i < zeilen.Count; i++)
                            Text(zl, zeilen[i], f, Farbrolle.ACHSE, 110f, 80f + i * (TextHoehe(f) + 6f));
                    });
                }
                return leer;
            }

            // Die Legende: je Spalte ihr Name in ihrer Farbe. Sie macht sich selbst Platz —
            // jede Zeile über der ersten schiebt die Zeichenfläche nach unten.
            const float LEGENDE_X = 110f, LEGENDE_Y = 84f;
            var farben = new SKColor[gueltig.Count];
            var leg = new List<Segment>();
            for (int i = 0; i < gueltig.Count; i++)
            {
                farben[i] = Zahlungsstromfarbe(gueltig[i].Schluessel, i);
                leg.Add(new Segment(gueltig[i].Name ?? "", 0, farben[i]));
            }
            // Stufe 2: Nimmt die umbrechende Legende der Zeichenfläche zu viel, wird das Bild höher.
            if (mass.HasValue)
            {
                float vorab = LegendenZeilen(leg, LEGENDE_X, W - 30f) * LEGENDE_ZEILE;
                float obenVorab = LEGENDE_Y + LEGENDE_ZEILE + 30f + Math.Max(0f, vorab - LEGENDE_ZEILE);
                H = Math.Max(H, (int)Math.Ceiling(obenVorab + STUFE2_MIN_FLAECHE + 74f));
            }

            var z = Modell(W, H);
            string unterzeile = texte.Unterzeile ?? "";
            z.Markiert("titel", zt =>
            {
                Titel(zt, titel, W, mass.HasValue);
                using (var f = Schrift(15f))
                    Text(zt, unterzeile, f, Farbrolle.ACHSE, 24f, 54f);
            });

            float legendenhoehe = Legende(z, leg, LEGENDE_X, LEGENDE_Y, W - 30f);
            float schub = Math.Max(0f, legendenhoehe - LEGENDE_ZEILE);

            float links = 150f, rechts = W - 40f, unten = H - 74f;
            float oben = LEGENDE_Y + LEGENDE_ZEILE + 30f + schub;

            // Die Skala: je Jahr die Summe der Einnahmen und die der Ausgaben — sie schließt
            // die Null und beide Enden jedes Balkens ein.
            double lo = 0.0, hi = 0.0;
            for (int t = 0; t < n; t++)
            {
                double plus = 0.0, minus = 0.0;
                foreach (Zahlungsstromreihe r in gueltig)
                {
                    double w = Zahlungsbetrag(r, t);
                    if (w > 0.0) plus += w; else minus += w;
                }
                hi = Math.Max(hi, plus);
                lo = Math.Min(lo, minus);
            }
            SchoeneStufen(ref lo, ref hi, out double schritt);
            float Y(double w) => unten - (float)((w - lo) / (hi - lo)) * (unten - oben);

            // Raster und Beschriftung der Euro-Achse.
            var raster = Stift(Farbrolle.RASTER, 1f);
            int stufen = (int)Math.Round((hi - lo) / schritt);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (int k = 0; k <= stufen; k++)
                    {
                        double wert = lo + k * schritt;
                        if (Math.Abs(wert) < schritt * 1e-9) wert = 0.0;   // keine „-0"
                        float y = Y(wert);
                        zy.Linie(links, y, rechts, y, raster);
                        string lab = wert.ToString("N0", DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, links - 12f - f.MeasureText(lab), y - TextHoehe(f) / 2f);
                    }
            });

            float fach = (rechts - links) / n;

            // ERSATZJAHRE zuerst: Das Band steht HINTER dem Balken. Band und Dreieck sagen nicht
            // „wie viel", sondern „hier ist eine Ersatzbeschaffung fällig".
            var ersatz = new List<int>();
            if (ersatzjahre != null)
                foreach (int t in ersatzjahre)
                    if (t >= 0 && t < n && !ersatz.Contains(t)) ersatz.Add(t);
            ersatz.Sort();
            string ersatzText = texte.Ersatzjahr ?? "";
            if (ersatz.Count > 0)
            {
                var band = Flaeche(C_ERSATZJAHR);
                var dreieck = Flaeche(C_RASTER_SCHLECHT);
                z.Markiert("marke", zm =>
                {
                    foreach (int t in ersatz)
                    {
                        float mitte = links + (t + 0.5f) * fach;
                        string wert = jahrText + " " + t.ToString(DE) + ": " + ersatzText;
                        zm.Markiert("marke", wert, zb =>
                        {
                            zb.Rechteck(mitte - fach * 0.45f, oben, fach * 0.9f, unten - oben, null, band);
                            Vieleck(zb, new[]
                            {
                                new SKPoint(mitte - 7f, oben - 12f),
                                new SKPoint(mitte + 7f, oben - 12f),
                                new SKPoint(mitte, oben - 1f)
                            }, dreieck);
                        });
                    }
                });
            }

            // DIE BALKEN: je Jahr die Schichten in der Reihenfolge der Tafel, die Einnahmen von
            // der Null nach oben, die Ausgaben nach unten. Jede Schicht nennt Jahr, Spalte und
            // Betrag.
            float breite = fach * 0.62f;
            for (int t = 0; t < n; t++)
            {
                float x = links + t * fach + (fach - breite) / 2f;
                double plus = 0.0, minus = 0.0;
                for (int i = 0; i < gueltig.Count; i++)
                {
                    double w = Zahlungsbetrag(gueltig[i], t);
                    if (w == 0.0) continue;
                    double a = w > 0.0 ? plus : minus, b = a + w;
                    if (w > 0.0) plus = b; else minus = b;
                    float yo = Y(Math.Max(a, b)), yu = Y(Math.Min(a, b));
                    var fuellung = Flaeche(farben[i]);
                    string name = gueltig[i].Name ?? "";
                    string wert = Elementwert(jahrText + " " + t.ToString(DE) + WERT_TRENNER + name,
                                              w, ZAHLUNGSSTROM_GELD, "€");
                    z.Markiert("reihe:" + name, wert, zr => zr.Rechteck(x, yo, breite, yu - yo, null, fuellung));
                }
            }

            // Die Nulllinie ÜBER den Balken: Sie trennt Einnahmen und Ausgaben.
            float y0 = Y(0.0);
            z.Markiert("nulllinie", zn => zn.Linie(links, y0, rechts, y0, Stift(Farbrolle.ACHSE, 2f)));

            // Die Jahre unter der Achse (bei vielen Jahren jedes zweite oder fünfte), rechts der
            // Achsentitel, links der Schlüssel der Ersatzjahr-Marke.
            int jeX = fach >= 28f ? 1 : fach >= 14f ? 2 : 5;
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                {
                    for (int t = 0; t < n; t += jeX)
                    {
                        string lab = t.ToString(DE);
                        float mitte = links + (t + 0.5f) * fach;
                        Text(zx, lab, f, Farbrolle.ACHSE, mitte - f.MeasureText(lab) / 2f, unten + 8f);
                    }
                    Text(zx, jahrText, f, Farbrolle.ACHSE, rechts - f.MeasureText(jahrText), unten + 34f);
                }
            });
            if (ersatz.Count > 0)
            {
                float ky = unten + 36f;
                z.Markiert("marke", zm =>
                {
                    zm.Rechteck(links, ky, 22f, 16f, null, Flaeche(C_ERSATZJAHR));
                    Vieleck(zm, new[]
                    {
                        new SKPoint(links + 4f, ky - 1f),
                        new SKPoint(links + 18f, ky - 1f),
                        new SKPoint(links + 11f, ky + 10f)
                    }, Flaeche(C_RASTER_SCHLECHT));
                    using (var f = Schrift(15f))
                        Text(zm, ersatzText, f, Farbrolle.TEXT, links + 30f, ky - 1f);
                });
            }
            return z;
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
            => SkiaMaler.Png(KostenprofilModell(titel, stundenwerte, einheit, achseMonat));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a) — der Rumpf, den
        /// <see cref="Kostenprofil"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>Die x-Achse zählt hier den INDEX der Reihe</b>, nicht die
        /// Jahresstunde: Das Bild legt das Profil über seine eigene Länge auf die
        /// Monatsachse, und ein Profil muss keine 8 760 Werte führen. Das Modell nennt
        /// die Einheit nicht — der Baustein liest sie aus seinem eigenen Parameter.</para>
        ///
        /// <para><b>Das PNG bleibt byte-gleich</b>: Der Maler übergeht Marken,
        /// Zeichenfläche und Datenreihen.</para>
        /// </summary>
        public static Zeichenmodell KostenprofilModell(string titel, double[] stundenwerte,
                                                       string einheit, string achseMonat)
        {
            int W = 1296, H = 780;
            var z = Modell(W, H);
            z.Markiert("titel", zt =>
                Titel(zt, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W));
            var rc = SKRect.Create(110f, 80f, W - 150f, 560f);

            if (stundenwerte == null || stundenwerte.Length < 2)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, "Kein Profil vorhanden.", f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
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
            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (double wert = min; wert <= max + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.###", DE);
                        float breite = f.MeasureText(lab);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }
            });

            // x-Achse: Monatsgrenzen 0…12, Abstand 1 (AxisX.Interval = 1).
            var xraster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (int m = 0; m <= 12; m++)
                    {
                        float x = rc.Left + m / 12f * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, xraster);
                        string lab = m.ToString(DE);
                        float breite = f.MeasureText(lab);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - breite / 2f, rc.Bottom + 8f);
                    }
            });
            using (var f = Schrift(15f))
                z.Markiert("xachse", zx =>
                    Text(zx, achseMonat ?? "", f, Farbrolle.ACHSE, rc.Right + 10f, rc.Bottom + 8f));

            // Achsen + Nulllinie, wenn die Skala unter null reicht. Das ACHSENKREUZ
            // bleibt ohne Marke (Regel aus E2).
            Achsenkreuz(z, rc);
            if (min < 0)
            {
                float y0 = (float)(rc.Bottom - (0 - min) / (max - min) * rc.Height);
                z.Markiert("nulllinie", zn =>
                    zn.Linie(rc.Left, y0, rc.Right, y0,
                             Stift(Farbrolle.ACHSE, 2f, new Strichmuster(3f * 2f, 1f * 2f))));
            }

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER (Etappe E3). x zaehlt den INDEX
            // der Reihe (0 … n-1), y ist die fertige Skala.
            z.Flaeche = new Zeichenflaeche(
                rc.Modellrahmen(),
                new Datenfenster(0, stundenwerte.Length - 1, min, max),
                Achsenart.Index);

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
            string name = titel ?? "";
            z.Markiert("reihe:" + name, zr =>
                Linienzug(zr, punkte.ToArray(), Stift(C_PROFIL, 2f, null, Strichverbindung.Rund)));

            // Dieselbe Reihe in DATENWERTEN, ungekuerzt (DG-E2-2).
            z.FuegeReihe(new Datenreihe(name, C_PROFIL.Ton(), 2f, null, stundenwerte,
                                        z.Flaeche.Daten));

            Legende(z, new List<Segment> { new Segment(titel, 0, C_PROFIL) }, 110f, H - 56f);
            return z;
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
        /// <param name="minimumNull">
        /// <c>true</c>: Die y-Achse beginnt bei 0, auch wenn alle Werte darüber liegen
        /// (iU9-W14c.0j, Entscheid E-4).
        ///
        /// <para><b>Warum es den Schalter braucht.</b> Der Sonnenwinkel-Verlauf der
        /// Klimadaten stand im Vorläufer fest auf <c>YMinValue = 0</c>
        /// (<c>Form_Klimadaten.CreateChart:119</c>), während die Temperaturkurve daneben
        /// ihr Minimum aus den Werten nahm. Ohne den Schalter begänne die
        /// Sonnenwinkel-Achse am kleinsten Wert — das Bild sähe sichtbar anders aus als
        /// der Bestand. Die Vorgabe <c>false</c> lässt jeden bisherigen Aufruf
        /// unverändert.</para>
        /// </param>
        /// <param name="fenster">
        /// DATENZOOM (Anwenderwunsch KL‑8, 20.09.2026: „Chart soll Zoom/Ausschnitt möglich
        /// sein (wie andere Charts)"): der Zeitausschnitt, den der Anwender im Bild
        /// aufgezogen hat; <c>null</c> = das ganze Jahr und damit Bild für Bild das des
        /// Bestands. Zugeschnitten wird ZUERST — Skala, Raster und Linien beziehen sich
        /// danach auf den Ausschnitt, genau wie es <see cref="Verlaufsbild"/> hält.
        ///
        /// <para><b>Die x-Achse wechselt im Fenster auf die WIRKLICHEN Jahresstunden</b>
        /// (<see cref="XAchseFenster"/>). Das ist die Hausregel jedes zugeschnittenen
        /// Bildes: Die feste Monatsteilung 0…12 sagt im Ausschnitt nichts mehr — in einem
        /// Fenster von Stunde 3 100 bis 3 400 läge keine einzige Monatsgrenze —, die
        /// Stunde schon. Mit ihr wechselt auch der Achsentitel auf
        /// <c>CHART_ACHSE_JAHRESSTUNDEN</c>; <paramref name="xTitel"/> steht nur in der
        /// Vollansicht.</para>
        ///
        /// <para>Der SENKRECHTE Anteil (<see cref="Achsenfenster.YAnteil"/>) senkt die
        /// Obergrenze wie bei <see cref="Jahresverlauf"/>; die Untergrenze bleibt, wo sie
        /// ist. Dieses Bild trägt die Null IMMER (siehe unten), sie ist also da, auf die
        /// man den Zug beziehen kann.</para>
        /// </param>
        public static byte[] Jahresgang(string titel, IReadOnlyList<Reihe> reihen,
                                        string xTitel, string yTitel,
                                        bool minimumNull = false,
                                        Achsenfenster fenster = null)
            => SkiaMaler.Png(JahresgangModell(titel, reihen, xTitel, yTitel, minimumNull, fenster));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Konzept Diagramme, Etappe E2) — der Rumpf,
        /// den <see cref="Jahresgang"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>Was das Modell über die Befehlsliste hinaus trägt.</b> Erstens die
        /// <c>Zeichenflaeche</c>: das Pixelrechteck der Fläche und das Datenfenster
        /// darin (x = Stützstelle der Reihe, y = Werteinheit). Zweitens je Reihe eine
        /// <c>Datenreihe</c> mit den UNGEKÜRZTEN Werten — der Pixelpfad im Befehl ist
        /// auf jeden n-ten Wert gekürzt, der SVG-Weg zeichnet aus den Datenwerten
        /// (Entscheid DG-E2-2). Drittens die MARKEN: <c>titel</c>, <c>xachse</c>,
        /// <c>yachse</c>, <c>reihe:…</c>, <c>legende:…</c>, <c>nulllinie</c>,
        /// <c>leerhinweis</c>.</para>
        ///
        /// <para><b>Das PNG bleibt byte-gleich</b>: Der Maler übergeht Marken,
        /// Zeichenfläche und Datenreihen — sie stehen für den zweiten Ausgabeweg da.</para>
        /// </summary>
        public static Zeichenmodell JahresgangModell(string titel, IReadOnlyList<Reihe> reihen,
                                                     string xTitel, string yTitel,
                                                     bool minimumNull = false,
                                                     Achsenfenster fenster = null)
        {
            int W = 1304, H = 440;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            // Legende OBEN wie im Vorlaeufer, aber ueber der Zeichenflaeche statt
            // darin - sonst verdeckt sie bei zwei Reihen den Jahresanfang.
            var gueltig = (reihen ?? new List<Reihe>())
                .Where(r => r != null && r.Werte != null && r.Werte.Length >= 2 &&
                            r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w)))
                .ToList();

            // KL-8: Der Zuschnitt steht GANZ oben - alles darunter rechnet mit dem
            // Ausschnitt, ohne davon zu wissen. gesamt merkt sich die volle Laenge;
            // die Achsenbeschriftung nennt Jahresstunden, nicht Fensterstunden.
            int gesamt = gueltig.Count > 0 ? gueltig[0].Werte.Length : 0;
            if (fenster != null) gueltig = Brauchbare(Zugeschnitten(gueltig, fenster));

            var rc = SKRect.Create(110f, 130f, W - 150f, 240f);

            if (gueltig.Count == 0)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Kein Jahresgang vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            Legende(z, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                    110f, 76f, W - 30f);

            // Vorzeichenfaehige Skala mit "schoenen" Stufen (5 Rasterlinien) -
            // dieselbe Rechnung wie in KapitalwertVerlauf und Kostenprofil.
            double min = minimumNull ? 0.0 : gueltig.Min(r => r.Werte.Min());
            double max = gueltig.Max(r => r.Werte.Max());
            if (fenster != null && fenster.YAnteil > 0) max *= fenster.YAnteil;
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
            var raster = Stift(Farbrolle.RASTER, 1f, new Strichmuster(2f, 4f));
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                {
                    for (double wert = min; wert <= max + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.###", DE);
                        float breite = f.MeasureText(lab);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }
                }
            });

            // x-Achse: Monatsgrenzen 0…12, Abstand 1 (AxisX.Interval = 1). Im
            // FENSTER stehen dort die wirklichen Jahresstunden (KL-8, Regel des
            // zugeschnittenen Bildes) - eine Monatsgrenze traegt im Ausschnitt
            // keine Aussage mehr.
            if (fenster == null)
            {
                var xraster = Stift(Farbrolle.RASTER, 1f, new Strichmuster(2f, 4f));
                z.Markiert("xachse", zx =>
                {
                    using (var f = Schrift(15f))
                    {
                        for (int m = 0; m <= 12; m++)
                        {
                            float x = rc.Left + m / 12f * rc.Width;
                            zx.Linie(x, rc.Top, x, rc.Bottom, xraster);
                            string lab = m.ToString(DE);
                            float breite = f.MeasureText(lab);
                            Text(zx, lab, f, Farbrolle.ACHSE, x - breite / 2f, rc.Bottom + 8f);
                        }
                    }
                });
            }
            else z.Markiert("xachse", zx => XAchseFenster(zx, rc, fenster, gesamt));

            using (var f = Schrift(15f))
            {
                // Im Fenster steht der Achsentitel schon da - mittig unter der
                // Achse und mit dem Wort "Jahresstunde" (XAchseFenster).
                if (fenster == null)
                    z.Markiert("xachse", zx =>
                        Text(zx, xTitel ?? "", f, Farbrolle.ACHSE, rc.Right + 10f, rc.Bottom + 8f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));
            }

            // Achsen + Nulllinie, wenn die Skala unter null reicht. Das ACHSENKREUZ
            // bleibt ohne Marke: Es steht auch dann, wenn die Oberflaeche beim Zoom
            // die Teilung der x-Achse ausblendet.
            Achsenkreuz(z, rc);
            if (min < 0)
            {
                float y0 = (float)(rc.Bottom - (0 - min) / (max - min) * rc.Height);
                z.Markiert("nulllinie", zn =>
                    zn.Linie(rc.Left, y0, rc.Right, y0,
                             Stift(Farbrolle.ACHSE, 2f, new Strichmuster(3f * 2f, 1f * 2f))));
            }

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER (Etappe E2). x zaehlt die
            // Stuetzstellen des BILDES: ohne Fenster 0 … gesamt-1, mit Fenster dessen
            // Grenzen; y ist die fertige Skala. Daraus baut der SvgSchreiber das
            // innere <svg> in Datenkoordinaten.
            double xVon = fenster == null ? 0.0
                        : Math.Max(0, Math.Min(gesamt, fenster.Von));
            z.Flaeche = new Zeichenflaeche(
                rc.Modellrahmen(),
                new Datenfenster(xVon, xVon + gueltig[0].Werte.Length - 1, min, max),
                Achsenart.Stunden);

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
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    Linienzug(zr, punkte.ToArray(),
                              Stift(r.Farbe, staerke, null, Strichverbindung.Rund)));

                // DG-E2-2: dieselbe Reihe zusaetzlich in DATENWERTEN, ungekuerzt. Der
                // Pixelpfad darueber bleibt dem PNG; der SVG-Weg zeichnet aus dieser
                // Reihe, bis E4 beide Wege zusammenfallen laesst. DG-E3-1: mit IHREM
                // Fenster - eine kuerzere Reihe zeichnet das Bild ueber die volle
                // Breite, nicht bis zur Haelfte.
                z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, null, r.Werte,
                                            Reihenfenster(z.Flaeche.Daten, r.Werte.Length)));
            }

            return z;
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
            => SkiaMaler.Png(KennlinienModell(titel, yTitel, xTitel, reihen, marke));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe b) — der Rumpf, den
        /// <see cref="Kennlinien"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>Es gibt KEINE Zeichenfläche (Entscheid DG-E3-7).</b> Die x-Achse
        /// trägt die Außentemperatur einer Handvoll Stützstellen; ein Zoom darauf hat
        /// keine Aussage, und ein inneres <c>&lt;svg&gt;</c> nähme dem Bild seine
        /// Punktmarken (der Schreiber ersetzt jeden Befehl mit der Marke
        /// <c>reihe:…</c>). Das Bild bleibt deshalb ein reines Pixelbild mit Marken —
        /// die Oberfläche schaltet damit Linien über die Legende.</para>
        ///
        /// <para><b>Die Datenreihen stehen trotzdem im Modell</b>: Je Kennlinie eine,
        /// mit der Außentemperatur als <c>XWerte</c> und dem Wert als <c>Werte</c>.
        /// Daraus liest die Zeigerzeile „bei −7 °C: COP 3,1" — ohne sie müsste sie die
        /// Zahlen aus dem Pixelpfad zurückrechnen.</para>
        /// </summary>
        public static Zeichenmodell KennlinienModell(string titel, string yTitel, string xTitel,
                                                     IReadOnlyList<KennlinienReihe> reihen,
                                                     Kennlinienmarke marke)
        {
            int W = 968, H = 520;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel, W));
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
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Keine Kennlinien vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            double xMin = gueltig.Min(r => r.Punkte.Min(p => p.Temperatur));
            double xMax = gueltig.Max(r => r.Punkte.Max(p => p.Temperatur));
            double yMin = Math.Min(0, gueltig.Min(r => r.Punkte.Min(p => p.Wert)));
            double yMax = Math.Max(0, gueltig.Max(r => r.Punkte.Max(p => p.Wert)));

            double xSchritt = Stufe(ref xMin, ref xMax);
            double ySchritt = Stufe(ref yMin, ref yMax);

            var raster = Stift(Farbrolle.RASTER, 1f);

            // y-Raster und -Beschriftung.
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (double wert = yMin; wert <= yMax + ySchritt / 2; wert += ySchritt)
                    {
                        float y = (float)(rc.Bottom - (wert - yMin) / (yMax - yMin) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("0.###", DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
            });

            // x-Raster und -Beschriftung.
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (double wert = xMin; wert <= xMax + xSchritt / 2; wert += xSchritt)
                    {
                        float x = (float)(rc.Left + (wert - xMin) / (xMax - xMin) * rc.Width);
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = wert.ToString("0.###", DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            // Achsen, Achsentitel und - falls die Skala unter null reicht - die Nulllinie.
            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
            {
                // 26 px statt der 10 px des Kapitalwert-Verlaufs: Dort steht rechts
                // eine einstellige Jahreszahl, hier eine zweistellige Temperatur mit
                // Vorzeichen - bei 10 px stiessen Zahl und Titel aneinander.
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE, rc.Right + 26f, rc.Bottom + 8f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));
            }
            if (yMin < 0)
            {
                float y0 = (float)(rc.Bottom - (0 - yMin) / (yMax - yMin) * rc.Height);
                z.Markiert("nulllinie", zn =>
                    zn.Linie(rc.Left, y0, rc.Right, y0,
                             Stift(Farbrolle.ACHSE, 2f, new Strichmuster(6f, 2f))));
            }

            // Die Linien samt Punktmarken. Die Farbe kommt aus den Serienrollen und
            // wiederholt sich, wenn ein Gerät mehr Vorläufe führt als Farben da sind.
            for (int i = 0; i < gueltig.Count; i++)
            {
                Farbrolle rolle = Serienrolle(i);
                var punkte = new SKPoint[gueltig[i].Punkte.Count];
                var temperaturen = new double[punkte.Length];
                var werte = new double[punkte.Length];
                for (int t = 0; t < punkte.Length; t++)
                {
                    var p = gueltig[i].Punkte[t];
                    float x = (float)(rc.Left + (p.Temperatur - xMin) / (xMax - xMin) * rc.Width);
                    float y = (float)(rc.Bottom - (p.Wert - yMin) / (yMax - yMin) * rc.Height);
                    punkte[t] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                    temperaturen[t] = p.Temperatur;
                    werte[t] = p.Wert;
                }

                // Linie UND Punktmarken tragen die Marke der Reihe: Wer die Kennlinie
                // ueber die Legende abwaehlt, blendet beides zusammen aus.
                string name = gueltig[i].Vorlauf.ToString(DE) + "°C";

                // DG-E3-10: Jede Punktmarke nennt IHREN Wert — „Außentemperatur
                // −5 °C · 35°C: 4,25". Die Linie darunter zeigt keine einzelne Zahl
                // und nennt deshalb nur ihren Namen (Regel der Gruppe (c)).
                var werttexte = new string[punkte.Length];
                for (int t = 0; t < punkte.Length; t++)
                    werttexte[t] = Achsenwert(xTitel, temperaturen[t], "0.###") + WERT_TRENNER +
                                   Elementwert(name, werte[t], "0.###", Achseneinheit(yTitel));

                z.Markiert("reihe:" + name, name, zr =>
                {
                    Linienzug(zr, punkte, Stift(rolle, 3f, null, Strichverbindung.Rund));
                    Punktmarken(zr, punkte, rolle, marke, "reihe:" + name, werttexte);
                });

                // DG-E3-7: die Kennlinie in DATENWERTEN - x die Aussentemperatur, y der
                // Wert. Ohne Zeichenflaeche zeichnet der Schreiber daraus nichts; die
                // Reihe ist die Quelle der Zeigerzeile.
                z.FuegeReihe(new Datenreihe(name, Farbton.Aus(rolle), 3f, null, werte,
                                            null, Reihenart.Linie, null, null,
                                            temperaturen, yTitel));
            }

            Legende(z, gueltig.Select(r => new Segment(
                        r.Vorlauf.ToString(DE) + "°C", 0, C_SERIEN[gueltig.IndexOf(r) % C_SERIEN.Length]))
                    .ToList(), 90f, H - 96f, W - 30f);
            return z;
        }

        /// <summary>
        /// Die Punktmarken einer Kennlinie. <c>MarkerSize = 5</c> des Vorläufers bei
        /// einfacher Auflösung sind hier 10 px — dieselbe optische Größe.
        /// </summary>
        /// <param name="reihenmarke">
        /// Die Marke der Reihe — jede Marke steht in IHRER Klammer, damit sie ihren
        /// eigenen Wert tragen kann (DG-E3-10). Die Befehlsreihenfolge bleibt dieselbe,
        /// das PNG also auch.
        /// </param>
        /// <param name="werte">Der fertige Text je Punkt; <c>null</c> = ohne Wert.</param>
        private static void Punktmarken(IZeichenziel z, SKPoint[] punkte, Farbrolle rolle,
                                        Kennlinienmarke marke,
                                        string reihenmarke = null, string[] werte = null)
        {
            const float R = 5f;
            if (marke == Kennlinienmarke.Kreis)
            {
                for (int i = 0; i < punkte.Length; i++)
                {
                    SKPoint p = punkte[i];
                    z.Markiert(reihenmarke, Wertetext(werte, i),
                               zp => zp.Kreis(p.X, p.Y, R, null, Flaeche(rolle)));
                }
                return;
            }

            var stift = Stift(rolle, 2.5f);
            for (int i = 0; i < punkte.Length; i++)
            {
                SKPoint p = punkte[i];
                z.Markiert(reihenmarke, Wertetext(werte, i), zp =>
                {
                    zp.Linie(p.X - R, p.Y - R, p.X + R, p.Y + R, stift);
                    zp.Linie(p.X - R, p.Y + R, p.X + R, p.Y - R, stift);
                });
            }
        }

        /// <summary>
        /// Die Rolle der <paramref name="i"/>-ten Variantenreihe (DG-Q7) — die
        /// ausdrückliche Schreibweise für <see cref="C_SERIEN"/>. Beide Listen tragen
        /// dieselben acht Hausfarben in derselben Reihenfolge; die Rolle nennt die
        /// Absicht, wo der Wert sie nur trifft.
        /// </summary>
        private static Farbrolle Serienrolle(int i) => SERIENROLLEN[i % SERIENROLLEN.Length];

        private static readonly Farbrolle[] SERIENROLLEN =
        {
            Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4,
            Farbrolle.SERIE_5, Farbrolle.SERIE_6, Farbrolle.SERIE_7, Farbrolle.SERIE_8
        };

        /// <summary>
        /// Die „schöne" Achsenstufung der Liniendiagramme (5 Rasterlinien), aus
        /// <see cref="KapitalwertVerlauf"/> herausgezogen, weil die Kennlinien sie für
        /// BEIDE Achsen brauchen. Rundet <paramref name="min"/> ab und
        /// <paramref name="max"/> auf und liefert die Schrittweite.
        /// </summary>
        private static double Stufe(ref double min, ref double max) => Skala.Stufe(ref min, ref max);

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
        /// <summary>
        /// Die y-Achse der Bedarfsbilder: Schrittweite, Obergrenze und Zahlenformat aus dem
        /// Größtwert. Wörtlich aus <c>SkaliereYAchse</c> (dreimal gleichlautend in den drei
        /// Ergebnismasken) — samt dem Rückfall „Maximum 5, Intervall 1", wenn alle Werte
        /// null sind, und der Sicherung gegen eine Schrittweite ≤ 0.
        /// </summary>
        private static (double Schritt, double Max, string Format) BedarfsSkala(double maxWert)
            => Skala.Bedarf(maxWert);

        /// <summary>Zeichnet Raster, y-Beschriftung und die beiden Achsen einer Bedarfsskala.</summary>
        private static void BedarfsRaster(IZeichenziel z, SKRect rc, double schritt, double max, string format)
        {
            BedarfsRasterOhneKreuz(z, rc, schritt, max, format);
            Achsenkreuz(z, rc);
        }

        /// <summary>
        /// Derselbe Rasterblock OHNE das Achsenkreuz (Etappe E3). Ein Bild, das seine
        /// y-Achse MARKIERT, klammert nur diesen Teil: Das Achsenkreuz bleibt
        /// markenlos, weil es auch dann stehen muss, wenn die Oberfläche beim Zoom die
        /// Teilung einer Achse ausblendet (dieselbe Regel wie in E2).
        /// </summary>
        private static void BedarfsRasterOhneKreuz(IZeichenziel z, SKRect rc, double schritt,
                                                   double max, string format)
        {
            var raster = Stift(Farbrolle.RASTER, 1f);
            using (var f = Schrift(15f))
                for (double wert = 0; wert <= max + schritt / 2; wert += schritt)
                {
                    float y = (float)(rc.Bottom - wert / max * rc.Height);
                    z.Linie(rc.Left, y, rc.Right, y, raster);
                    string lab = wert.ToString(format, DE);
                    Text(z, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f, y - TextHoehe(f) / 2f);
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
            => SkiaMaler.Png(MonatsSaeulenModell(titel, werte, farbe, einheit, monatsnamen));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c).
        ///
        /// <para><b>Ein reines Pixelbild</b> (DG-E3-7): keine Zeichenfläche, keine
        /// <c>Datenreihe</c> — zwölf starre Fächer sind keine Zeitachse, auf der man
        /// zoomen könnte. Jede Säule trägt die Marke <c>reihe:&lt;Titel&gt;</c> (dieses
        /// Bild führt GENAU EINE Reihe, und ihr Name ist sein Titel) und als Wert
        /// „Jan: 12,5 MWh" — mit dem Zahlenformat SEINER y-Achse
        /// (<see cref="Skala.Bedarf"/>), damit Zeigetext und Achsenbeschriftung
        /// dieselben Nachkommastellen führen.</para>
        /// </summary>
        public static Zeichenmodell MonatsSaeulenModell(string titel, double[] werte, SKColor farbe,
                                                        string einheit,
                                                        IReadOnlyList<string> monatsnamen = null)
            => MonatsSaeulenModell(titel, werte, farbe.Ton(), einheit, monatsnamen);

        /// <summary>
        /// Dieselben Säulen mit AUSDRÜCKLICH genannter Farbrolle (DG-E5) — der Weg für
        /// jede Hülle, die die Größe benennen kann.
        /// </summary>
        public static Zeichenmodell MonatsSaeulenModell(string titel, double[] werte,
                                                        Farbrolle rolle, string einheit,
                                                        IReadOnlyList<string> monatsnamen = null)
            => MonatsSaeulenModell(titel, werte, Farbton.Aus(rolle), einheit, monatsnamen);

        private static Zeichenmodell MonatsSaeulenModell(string titel, double[] werte, Farbton ton,
                                                         string einheit,
                                                         IReadOnlyList<string> monatsnamen)
        {
            int W = 978, H = 542;
            var z = Modell(W, H);
            z.Markiert("titel", zt =>
                Titel(zt, titel + (string.IsNullOrEmpty(einheit) ? "" : "  [" + einheit + "]"), W));
            var rc = SKRect.Create(100f, 80f, W - 140f, 380f);

            if (werte == null || werte.Length < 12)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Keine Monatswerte vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            double maxWert = 0;
            for (int m = 0; m < 12; m++) if (werte[m] > maxWert) maxWert = werte[m];
            (double schritt, double max, string format) = BedarfsSkala(maxWert);

            z.Markiert("yachse", zy => BedarfsRasterOhneKreuz(zy, rc, schritt, max, format));
            Achsenkreuz(z, rc);

            float fach = rc.Width / 12f;
            float breite = fach * 0.6f;
            // Die Saeulenfarbe benennt die SICHT und kommt deshalb von aussen - als
            // Rolle, wo der Aufrufer sie kennt, sonst ueber die Rueckwaertssuche.
            Zeichnung.Fuellung pinsel = Flaeche(ton);
            string reihe = "reihe:" + (titel ?? "");
            using (var f = Schrift(15f))
                for (int m = 0; m < 12; m++)
                {
                    float mitte = rc.Left + (m + 0.5f) * fach;

                    double wert = werte[m] > 0 ? werte[m] : 0;   // y beginnt starr bei 0
                    float hoehe = (float)(Math.Min(wert, max) / max * rc.Height);
                    string lab = (monatsnamen != null && monatsnamen.Count > m)
                        ? monatsnamen[m] : MONATE_KURZ[m];
                    double roh = werte[m];

                    if (hoehe > 0)
                        z.Markiert(reihe, Elementwert(lab, roh, format, einheit), zs =>
                            zs.Rechteck(mitte - breite / 2f, rc.Bottom - hoehe, breite, hoehe,
                                        null, pinsel));

                    z.Markiert("xachse", zx =>
                        Text(zx, lab, f, Farbrolle.ACHSE, mitte - f.MeasureText(lab) / 2f,
                             rc.Bottom + 8f));
                }

            return z;
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
            => SkiaMaler.Png(StundenprofilModell(titel, werte, intervall, xTitel, yTitel));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a).
        ///
        /// <para><b>Die x-Achse zählt hier den INDEX der Reihe</b> — 24 Tages- oder 168
        /// Wochenstunden, keine Jahresstunde. Die Zeichenfläche spannt sich von 0 bis
        /// <c>n</c>, die REIHE aber von 1 bis <c>n</c>: Wert <c>i</c> steht am RECHTEN
        /// Rand seines Fachs (Stunde n meint das Intervall (n−1, n]), und genau das
        /// drückt ihr eigenes Fenster aus (DG-E3-1).</para>
        ///
        /// <para><b>Eine Reihe, eine Fläche mit Randlinie</b> (DG-E3-2): Füllung in
        /// <c>C_PROFILFLAECHE</c>, Rand in <c>C_PROFILLINIE</c> mit Stärke 2 — dieselben
        /// zwei Farben, die das PNG zieht.</para>
        /// </summary>
        public static Zeichenmodell StundenprofilModell(string titel, double[] werte, int intervall,
                                                        string xTitel, string yTitel)
        {
            int W = 1244, H = 464;
            var z = Modell(W, H);
            if (!string.IsNullOrEmpty(titel)) z.Markiert("titel", zt => Titel(zt, titel, W));
            var rc = SKRect.Create(100f, 76f, W - 200f, 300f);

            if (werte == null || werte.Length < 2)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Kein Profil vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            double maxWert = 0;
            foreach (double w in werte) if (w > maxWert) maxWert = w;
            double max = (maxWert > 0 ? maxWert : 1) * 1.1;

            // y-Raster in fünf Stufen; die Zahlen tragen so viele Stellen, wie der
            // Größtwert braucht (bei Verteilungen unter 1 sonst lauter Nullen).
            string format = max >= 10 ? "0" : max >= 1 ? "0.0" : "0.000";
            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (int i = 0; i <= 5; i++)
                    {
                        double wert = max * i / 5.0;
                        float y = rc.Bottom - (float)(i / 5.0) * rc.Height;
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString(format, DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
            });

            // x-Raster: die Stundenmarken des Vorläufers (Intervall 24 bzw. 2).
            int schrittX = intervall > 0 ? intervall : Math.Max(1, werte.Length / 6);
            var xraster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (int h = 0; h <= werte.Length; h += schrittX)
                    {
                        float x = rc.Left + (float)h / werte.Length * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, xraster);
                        string lab = h.ToString(DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));
            }

            // DIE ZEICHENFLAECHE: x laeuft von 0 bis n (dort stehen die Stundenmarken),
            // y von null bis zur Obergrenze.
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(),
                                           new Datenfenster(0, werte.Length, 0, max),
                                           Achsenart.Index);

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

            // Das Bild nennt die Reihe nicht (es fuehrt keine Legende); ihr Name ist
            // deshalb die Beschriftung der y-Achse - sie benennt die Groesse.
            string name = string.IsNullOrEmpty(yTitel) ? (titel ?? "") : yTitel;
            z.Markiert("reihe:" + name, zr =>
            {
                Vieleck(zr, flaechenzug, Flaeche(C_PROFILFLAECHE));
                Linienzug(zr, punkte, Stift(C_PROFILLINIE, 2f, null, Strichverbindung.Rund));
            });

            // EINE Datenreihe fuer beides (DG-E3-2): Fuellung, Randfarbe, Randstaerke.
            // Ihr Fenster beginnt bei 1 - der erste Wert steht am rechten Rand des
            // ersten Fachs.
            z.FuegeReihe(new Datenreihe(name, C_PROFILFLAECHE.Ton(), 2f, null, werte,
                                        new Datenfenster(1, werte.Length, 0, max),
                                        Reihenart.Flaeche, null, C_PROFILLINIE.Ton()));

            return z;
        }

        /// <summary>
        /// <b>STUNDENPROFIL MIT MEHREREN REIHEN</b> (Umsetzungskonzept Zapfprofilgenerator 5.6;
        /// Stufe Z1, Gruppe 3) — der Zwilling von <see cref="Stundenprofil"/> für mehr als eine
        /// Reihe: der Tagesgang je Tagtyp (Werktag, Samstag, Sonn-/Feiertag) samt Zirkulation
        /// über 24 Stunden, das Wochenprofil über 168 Stunden.
        ///
        /// <para><b>Dieselbe Achse wie das Stundenprofil:</b> x zählt den INDEX der Reihe
        /// (0 … n), Wert <c>i</c> steht am rechten Rand seines Fachs (Stunde n meint das
        /// Intervall (n−1, n]); y endet auf der runden Stufe über dem Größtwert aller Reihen
        /// (<c>Skala.Stufe</c>), Rückfall 1. Die erste
        /// Reihe ist eine Fläche in ihrer Farbe (Deckung 100) mit Randlinie, jede weitere eine
        /// Linie in ihrer Strichart. Die Legende steht oben; bricht sie um, rückt die
        /// Zeichenfläche nach unten und das Bild wird um die Zeile höher.</para>
        ///
        /// <para><b>Nur gleich lange Reihen.</b> Eine Reihe mit anderer Länge als die erste,
        /// mit weniger als zwei oder mit nicht endlichen Werten entfällt; ohne gültige Reihe
        /// steht der Leerhinweis.</para>
        /// </summary>
        /// <param name="titel">Überschrift; leer = ohne.</param>
        /// <param name="reihen">Die Reihen in Zeichenreihenfolge; die erste ist die Fläche.</param>
        /// <param name="intervall">Abstand der x-Beschriftung: 24 (Tagesgrenzen) bzw. 6 (Tagesstunden).</param>
        /// <param name="xTitel">Beschriftung der x-Achse.</param>
        /// <param name="yTitel">Beschriftung der y-Achse, z. B. „Leistung [kW]".</param>
        public static byte[] Stundenprofile(string titel, IReadOnlyList<Reihe> reihen, int intervall,
                                            string xTitel, string yTitel)
            => SkiaMaler.Png(StundenprofileModell(titel, reihen, intervall, xTitel, yTitel));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL — der Weg der Oberfläche (<c>DiagrammSvg</c>): je
        /// Reihe eine <see cref="Datenreihe"/> mit den ungekürzten Werten, die erste als Fläche
        /// mit Randfarbe, die übrigen als Linien; Marken <c>titel</c>, <c>xachse</c>,
        /// <c>yachse</c>, <c>reihe:…</c>, <c>legende:…</c>, <c>leerhinweis</c>.
        /// </summary>
        public static Zeichenmodell StundenprofileModell(string titel, IReadOnlyList<Reihe> reihen, int intervall,
                                                         string xTitel, string yTitel)
        {
            const int W = 1244;
            const float FLAECHE_HOEHE = 300f;
            const byte FLAECHE_DECKUNG = 100;

            var gueltig = new List<Reihe>();
            foreach (Reihe r in reihen ?? new List<Reihe>())
            {
                if (r == null || r.Werte == null || r.Werte.Length < 2) continue;
                if (r.Werte.Any(w => double.IsNaN(w) || double.IsInfinity(w))) continue;
                if (gueltig.Count > 0 && r.Werte.Length != gueltig[0].Werte.Length) continue;
                gueltig.Add(r);
            }

            // Die Legende belegt eine oder mehr Zeilen; ihre Hoehe ergibt sich aus derselben
            // Umbruchregel, mit der sie gezeichnet wird (LegendenHoehe).
            float legendeY = 62f;
            float legendenHoehe = 0f;
            if (gueltig.Count > 0)
                using (var f = Schrift(16f))
                    legendenHoehe = LegendenHoehe(
                        gueltig.Select(r => 40f + f.MeasureText(r.Name ?? "") + 24f).ToList(), 100f, W - 30f);
            float oben = gueltig.Count > 0 ? legendeY + legendenHoehe + 44f : 76f;
            int H = (int)Math.Ceiling(oben + FLAECHE_HOEHE + 88f);

            var z = Modell(W, H);
            if (!string.IsNullOrEmpty(titel)) z.Markiert("titel", zt => Titel(zt, titel, W));
            var rc = SKRect.Create(100f, oben, W - 200f, FLAECHE_HOEHE);

            if (gueltig.Count == 0)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Kein Profil vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            Legende(z, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe, r.Strichart)).ToList(),
                    100f, legendeY, W - 30f);

            int n = gueltig[0].Werte.Length;
            double maxWert = 0;
            foreach (Reihe r in gueltig)
                foreach (double w in r.Werte) if (w > maxWert) maxWert = w;
            // Die y-Achse endet auf einer RUNDEN Stufe (Skala.Stufe, Schritte 1/2/2,5/5 je
            // Zehnerpotenz): Beschriftungen wie 2,4 oder 7,2 lesen sich als Leistung schlecht.
            double min = 0.0, max = maxWert > 0 ? maxWert : 1.0;
            double schritt = Skala.Stufe(ref min, ref max);
            int stufen = (int)Math.Round(max / schritt);
            // So viele Nachkommastellen, wie die Stufe braucht (2,5 -> eine, 0,25 -> zwei).
            int stellen = 0;
            for (double s = schritt; stellen < 3 && Math.Abs(s - Math.Round(s)) > 1e-9; s *= 10) stellen++;
            string format = "N" + stellen.ToString(CultureInfo.InvariantCulture);
            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (int i = 0; i <= stufen; i++)
                    {
                        double wert = schritt * i;
                        float y = rc.Bottom - (float)(wert / max) * rc.Height;
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString(format, DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
            });

            int schrittX = intervall > 0 ? intervall : Math.Max(1, n / 6);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (int h = 0; h <= n; h += schrittX)
                    {
                        float x = rc.Left + (float)h / n * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = h.ToString(DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));
            }

            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(),
                                           new Datenfenster(0, n, 0, max),
                                           Achsenart.Index);

            for (int k = 0; k < gueltig.Count; k++)
            {
                Reihe r = gueltig[k];
                var punkte = new SKPoint[n + 1];
                for (int i = 0; i < n; i++)
                {
                    float x = rc.Left + (float)(i + 1) / n * rc.Width;
                    float y = (float)(rc.Bottom - Math.Max(0, r.Werte[i]) / max * rc.Height);
                    punkte[i + 1] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                }
                // Der erste Wert gilt ab dem linken Rand - wie im Stundenprofil.
                punkte[0] = new SKPoint(rc.Left, punkte[1].Y);

                float staerke = r.Breite > 0 ? r.Breite : 2f;
                Strichmuster muster = Strichfolge(r.Strichart);
                var fenster = new Datenfenster(1, n, 0, max);

                if (k == 0)
                {
                    var flaechenzug = new SKPoint[punkte.Length + 2];
                    flaechenzug[0] = new SKPoint(rc.Left, rc.Bottom);
                    Array.Copy(punkte, 0, flaechenzug, 1, punkte.Length);
                    flaechenzug[flaechenzug.Length - 1] = new SKPoint(punkte[punkte.Length - 1].X, rc.Bottom);

                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    {
                        Vieleck(zr, flaechenzug, Flaeche(Ton(r, FLAECHE_DECKUNG)));
                        Linienzug(zr, punkte, Stift(Ton(r), staerke, muster, Strichverbindung.Rund));
                    });
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r, FLAECHE_DECKUNG), staerke, muster, r.Werte,
                                                fenster, Reihenart.Flaeche, null, Ton(r)));
                }
                else
                {
                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                        Linienzug(zr, punkte, Stift(Ton(r), staerke, muster, Strichverbindung.Rund)));
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, muster, r.Werte, fenster));
                }
            }

            return z;
        }

        // ==================================== Summenlinie (Zapfprofil, Stufe Z2, Gruppe 2)

        /// <summary>
        /// Eine MARKE im <see cref="SummenlinieModell"/>: eine senkrechte Strecke bei
        /// <see cref="X"/> von <see cref="YVon"/> bis <see cref="YBis"/> in Werten der LINKEN
        /// Achse — etwa der Speicherinhalt zwischen Bedarfs- und Versorgungslinie oder der
        /// maßgebende Zeitpunkt — oder, mit <see cref="Punkt"/>, ein Kreis bei (X, YBis).
        /// Die Beschriftung steht rechts daneben.
        /// </summary>
        public sealed class Linienmarke
        {
            /// <summary>Die x-Stelle in der Einheit der x-Achse.</summary>
            public double X;

            /// <summary>Das untere Ende der Strecke; <c>null</c> = von der Achsennull.</summary>
            public double? YVon;

            /// <summary>Das obere Ende der Strecke bzw. die Höhe des Punkts.</summary>
            public double YBis;

            /// <summary>Die Beschriftung; leer = ohne.</summary>
            public string Text;

            /// <summary>Ein Punkt bei (X, YBis) statt einer Strecke.</summary>
            public bool Punkt;

            public Linienmarke(double x, double? yVon, double yBis, string text, bool punkt = false)
            { X = x; YVon = yVon; YBis = yBis; Text = text; Punkt = punkt; }
        }

        /// <summary>
        /// <b>SUMMENLINIE</b> (Umsetzungskonzept Zapfprofilgenerator 5.6; Stufe Z2, Gruppe 2) —
        /// ein Linienbild über einer x-Größe mit eigener Teilung, für die Bilder der Auslegung:
        /// die kumulierte Bedarfs- und Versorgungslinie des Bedarfstags über 1 441 Minutenwerte
        /// mit markiertem Speicherinhalt, die Wertepaarkurve Volumen über Leistung mit dem
        /// gewählten Punkt und die maßgebende Woche der Stundenbilanz mit Defizit und Füllstand
        /// auf der zweiten Achse.
        ///
        /// <para><b>Die x-Achse</b> trägt die Werte aus <paramref name="xWerte"/> (aufsteigend,
        /// so lang wie die Reihen), ohne sie den Index 0 … n−1. Geteilt wird alle
        /// <paramref name="xIntervall"/> (≤ 0: runde Stufe), beschriftet mit Teilung durch
        /// <paramref name="xTeiler"/> — so zeigen 1 441 Minutenwerte die Stunden 0, 6, 12, 18,
        /// 24. <b>Die linke Achse</b> ist vorzeichenfähig und endet auf runder Stufe
        /// (<c>Skala.Stufe</c>); die Reihen der <b>zweiten Achse</b> teilen sich rechts eine
        /// Skala von null bis zum geglätteten Höchstwert, in der Farbe ihrer ersten Reihe.</para>
        ///
        /// <para><b>Reihen:</b> eine Reihe mit <see cref="Stapelart.Flaeche"/> wird als Fläche
        /// ab der Achsennull mit Randlinie gezeichnet, jede andere als Linie in ihrer Strichart.
        /// Eine Reihe mit anderer Länge als die erste, mit weniger als zwei oder mit nicht
        /// endlichen Werten entfällt; ohne gültige linke Reihe steht der Leerhinweis. Die
        /// Legende steht oben und schiebt die Zeichenfläche bei einem Umbruch nach unten.</para>
        /// </summary>
        /// <param name="titel">Überschrift; leer = ohne.</param>
        /// <param name="reihen">Die Reihen der linken Achse in Zeichenreihenfolge.</param>
        /// <param name="xWerte">Die x-Stelle je Wert; <c>null</c> = der Index.</param>
        /// <param name="xIntervall">Abstand der x-Teilung in x-Einheiten; ≤ 0 = runde Stufe.</param>
        /// <param name="xTeiler">Teiler der x-Beschriftung (60: Minuten als Stunden); ≤ 0 = 1.</param>
        /// <param name="xTitel">Beschriftung der x-Achse.</param>
        /// <param name="yTitel">Beschriftung der linken y-Achse.</param>
        /// <param name="zweiteAchse">Die Reihen der rechten Achse; <c>null</c> oder leer = keine.</param>
        /// <param name="y2Titel">Beschriftung der rechten Achse.</param>
        /// <param name="marken">Strecken und Punkte über den Reihen; <c>null</c> = keine.</param>
        public static byte[] Summenlinie(string titel, IReadOnlyList<Reihe> reihen, double[] xWerte,
                                         double xIntervall, double xTeiler, string xTitel, string yTitel,
                                         IReadOnlyList<Reihe> zweiteAchse = null, string y2Titel = null,
                                         IReadOnlyList<Linienmarke> marken = null)
            => SkiaMaler.Png(SummenlinieModell(titel, reihen, xWerte, xIntervall, xTeiler, xTitel, yTitel,
                                               zweiteAchse, y2Titel, marken));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL — der Weg der Oberfläche (<c>DiagrammSvg</c>): eine
        /// Zeichenfläche mit x als freier Größe (<see cref="Achsenart.Wert"/>), je Reihe eine
        /// <see cref="Datenreihe"/> (mit ihrer x-Stelle je Wert, wo <paramref name="xWerte"/>
        /// gesetzt ist), die Reihen der zweiten Achse mit eigenem Fenster und
        /// <see cref="Achsenseite.Rechts"/>; Marken <c>titel</c>, <c>xachse</c>, <c>yachse</c>,
        /// <c>yachse2</c>, <c>reihe:…</c>, <c>legende:…</c>, <c>marke</c>, <c>leerhinweis</c>.
        /// </summary>
        public static Zeichenmodell SummenlinieModell(string titel, IReadOnlyList<Reihe> reihen, double[] xWerte,
                                                      double xIntervall, double xTeiler, string xTitel, string yTitel,
                                                      IReadOnlyList<Reihe> zweiteAchse = null, string y2Titel = null,
                                                      IReadOnlyList<Linienmarke> marken = null)
        {
            const int W = 1244;
            const float FLAECHE_HOEHE = 300f;
            const byte FLAECHE_DECKUNG = 100;

            List<Reihe> links = GleichLang(reihen, -1);
            int n = links.Count > 0 ? links[0].Werte.Length : 0;
            List<Reihe> rechts = n > 0 ? GleichLang(zweiteAchse, n) : new List<Reihe>();
            bool mitY2 = rechts.Count > 0;

            float legendeY = 62f;
            float legendenHoehe = 0f;
            var eintraege = links.Concat(rechts).ToList();
            if (eintraege.Count > 0)
                using (var f = Schrift(16f))
                    legendenHoehe = LegendenHoehe(
                        eintraege.Select(r => 40f + f.MeasureText(r.Name ?? "") + 24f).ToList(), 100f, W - 30f);
            float oben = eintraege.Count > 0 ? legendeY + legendenHoehe + 44f : 76f;
            int H = (int)Math.Ceiling(oben + FLAECHE_HOEHE + 88f);

            var z = Modell(W, H);
            if (!string.IsNullOrEmpty(titel)) z.Markiert("titel", zt => Titel(zt, titel, W));
            var rc = SKRect.Create(100f, oben, W - (mitY2 ? 230f : 200f), FLAECHE_HOEHE);

            if (links.Count == 0)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Kein Profil vorhanden."), f, Farbrolle.ACHSE, rc.Left, rc.Top + 20f));
                return z;
            }

            Legende(z, eintraege.Select(r => new Segment(r.Name, 0, r.Farbe, r.Strichart)).ToList(), 100f, legendeY, W - 30f);

            // --- x-Stellen: die übergebenen (aufsteigend, endlich, gleich lang) oder der Index ---
            bool eigeneX = xWerte != null && xWerte.Length == n
                           && xWerte.All(x => !double.IsNaN(x) && !double.IsInfinity(x));
            for (int i = 1; eigeneX && i < n; i++) eigeneX = xWerte[i] >= xWerte[i - 1];
            double[] xs = eigeneX ? xWerte : Enumerable.Range(0, n).Select(i => (double)i).ToArray();
            double xMin = xs[0], xMax = xs[n - 1];
            if (!(xMax - xMin > 1e-12)) xMax = xMin + 1.0;

            // --- linke Achse: vorzeichenfähig, runde Stufe ---------------------------------------
            double min = Math.Min(0.0, links.Min(r => r.Werte.Min()));
            double max = links.Max(r => r.Werte.Max());
            if (marken != null)
                foreach (Linienmarke m in marken)
                    if (m != null && !double.IsNaN(m.YBis) && !double.IsInfinity(m.YBis)) max = Math.Max(max, m.YBis);
            if (!(max > min)) max = min + 1.0;
            double schritt = Skala.Stufe(ref min, ref max);
            string format = Stellenformat(schritt);
            var raster = Stift(Farbrolle.RASTER, 1f);
            double minJ = min, maxJ = max;
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (double wert = minJ; wert <= maxJ + schritt / 2; wert += schritt)
                    {
                        float y = (float)(rc.Bottom - (wert - minJ) / (maxJ - minJ) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = (Math.Abs(wert) < schritt * 1e-9 ? 0.0 : wert).ToString(format, DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f, y - TextHoehe(f) / 2f);
                    }
            });

            // --- x-Teilung ------------------------------------------------------------------------
            double teiler = xTeiler > 0 ? xTeiler : 1.0;
            double xSchritt = xIntervall;
            if (!(xSchritt > 0))
            {
                double lo = xMin, hi = xMax;
                xSchritt = Skala.Stufe(ref lo, ref hi);
            }
            string xFormat = Stellenformat(xSchritt / teiler);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (double wert = Math.Ceiling(xMin / xSchritt - 1e-9) * xSchritt; wert <= xMax + xSchritt * 1e-9;
                         wert += xSchritt)
                    {
                        float x = rc.Left + (float)((wert - xMin) / (xMax - xMin)) * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = (Math.Abs(wert) < xSchritt * 1e-9 ? 0.0 : wert / teiler).ToString(xFormat, DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE, rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));
            }

            var fensterLinks = new Datenfenster(xMin, xMax, min, max);
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(), fensterLinks, Achsenart.Wert);

            SKPoint Punktlage(double x, double wert, double unten, double oben2)
            {
                float px = rc.Left + (float)((x - xMin) / (xMax - xMin)) * rc.Width;
                float py = (float)(rc.Bottom - (wert - unten) / (oben2 - unten) * rc.Height);
                return new SKPoint(px, Math.Max(rc.Top, Math.Min(rc.Bottom, py)));
            }

            // --- Reihen der linken Achse ----------------------------------------------------------
            double[] xDaten = eigeneX ? xs : null;
            foreach (Reihe r in links)
            {
                var punkte = new SKPoint[n];
                for (int i = 0; i < n; i++) punkte[i] = Punktlage(xs[i], r.Werte[i], min, max);
                float staerke = r.Breite > 0 ? r.Breite : 2f;
                Strichmuster muster = Strichfolge(r.Strichart);
                if (r.Stapelgruppe == Stapelart.Flaeche)
                {
                    float null0 = Punktlage(xMin, 0.0, min, max).Y;
                    var zug = new SKPoint[n + 2];
                    zug[0] = new SKPoint(punkte[0].X, null0);
                    Array.Copy(punkte, 0, zug, 1, n);
                    zug[n + 1] = new SKPoint(punkte[n - 1].X, null0);
                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    {
                        Vieleck(zr, zug, Flaeche(Ton(r, FLAECHE_DECKUNG)));
                        Linienzug(zr, punkte, Stift(Ton(r), staerke, muster, Strichverbindung.Rund));
                    });
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r, FLAECHE_DECKUNG), staerke, muster, r.Werte,
                                                fensterLinks, Reihenart.Flaeche, null, Ton(r), xDaten, yTitel));
                }
                else
                {
                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                        Linienzug(zr, punkte, Stift(Ton(r), staerke, muster, Strichverbindung.Rund)));
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, muster, r.Werte, fensterLinks,
                                                Reihenart.Linie, null, null, xDaten, yTitel));
                }
            }

            // --- zweite Achse: eine gemeinsame Skala 0 … max2 --------------------------------------
            if (mitY2)
            {
                double max2 = Nice(rechts.Max(r => r.Werte.Max()));
                if (!(max2 > 0)) max2 = 1.0;
                var fensterRechts = new Datenfenster(xMin, xMax, 0.0, max2);
                foreach (Reihe r in rechts)
                {
                    var punkte = new SKPoint[n];
                    for (int i = 0; i < n; i++) punkte[i] = Punktlage(xs[i], r.Werte[i], 0.0, max2);
                    float staerke = r.Breite > 0 ? r.Breite : 2f;
                    Strichmuster muster = Strichfolge(r.Strichart);
                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                        Linienzug(zr, punkte, Stift(Ton(r), staerke, muster, Strichverbindung.Rund)));
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, muster, r.Werte, fensterRechts,
                                                Reihenart.Linie, null, null, xDaten, y2Titel,
                                                Achsenseite: Achsenseite.Rechts));
                }

                Reihe erste = rechts[0];
                string format2 = Stellenformat(max2 / 4.0);
                z.Markiert("yachse2", zy2 =>
                {
                    zy2.Linie(rc.Right, rc.Top, rc.Right, rc.Bottom, Stift(Ton(erste), 2f));
                    using (var f = Schrift(15f))
                    {
                        for (int i = 0; i <= 4; i++)
                        {
                            double wert = max2 * i / 4.0;
                            float y = (float)(rc.Bottom - wert / max2 * rc.Height);
                            Text(zy2, wert.ToString(format2, DE), f, Ton(erste), rc.Right + 8f, y - TextHoehe(f) / 2f);
                        }
                        string t2 = y2Titel ?? "";
                        Text(zy2, t2, f, Ton(erste), W - 20f - f.MeasureText(t2), rc.Top - 24f);
                    }
                });
            }

            // --- Marken ---------------------------------------------------------------------------
            if (marken != null)
                foreach (Linienmarke m in marken)
                {
                    if (m == null || double.IsNaN(m.X) || double.IsInfinity(m.X) || m.X < xMin || m.X > xMax) continue;
                    if (double.IsNaN(m.YBis) || double.IsInfinity(m.YBis)) continue;
                    SKPoint obenP = Punktlage(m.X, m.YBis, min, max);
                    SKPoint untenP = Punktlage(m.X, m.YVon ?? 0.0, min, max);
                    string text = m.Text ?? "";
                    bool punkt = m.Punkt;
                    z.Markiert("marke", zm =>
                    {
                        if (punkt)
                            zm.Kreis(obenP.X, obenP.Y, 6f, Stift(Farbrolle.RASTER_SCHLECHT, 3f));
                        else
                            zm.Linie(untenP.X, untenP.Y, obenP.X, obenP.Y,
                                     Stift(Farbrolle.TEXT, 2.5f, new Strichmuster(6f, 3f)));
                        if (text.Length > 0)
                            using (var f = Schrift(14f))
                            {
                                float breite = f.MeasureText(text);
                                float tx = obenP.X + 10f;
                                if (tx + breite > rc.Right) tx = obenP.X - 10f - breite;
                                float ty = punkt ? obenP.Y - TextHoehe(f) - 6f : (obenP.Y + untenP.Y) / 2f - TextHoehe(f) / 2f;
                                Text(zm, text, f, Farbrolle.TEXT, tx, Math.Max(rc.Top, ty));
                            }
                    });
                }

            return z;
        }

        /// <summary>
        /// Die brauchbaren Reihen einer Liste: mindestens zwei endliche Werte und dieselbe Länge
        /// wie die erste — oder wie <paramref name="laenge"/>, wenn sie gesetzt ist (≥ 0).
        /// </summary>
        private static List<Reihe> GleichLang(IReadOnlyList<Reihe> reihen, int laenge)
        {
            var gueltig = new List<Reihe>();
            if (reihen == null) return gueltig;
            foreach (Reihe r in reihen)
            {
                if (r == null || r.Werte == null || r.Werte.Length < 2) continue;
                if (r.Werte.Any(w => double.IsNaN(w) || double.IsInfinity(w))) continue;
                int soll = laenge >= 0 ? laenge : gueltig.Count > 0 ? gueltig[0].Werte.Length : r.Werte.Length;
                if (r.Werte.Length != soll) continue;
                gueltig.Add(r);
            }
            return gueltig;
        }

        /// <summary>So viele Nachkommastellen, wie eine Stufe braucht (2,5 → „N1", 0,25 → „N2"; höchstens drei).</summary>
        private static string Stellenformat(double schritt)
        {
            int stellen = 0;
            for (double s = Math.Abs(schritt); stellen < 3 && s > 0 && Math.Abs(s - Math.Round(s)) > 1e-9; s *= 10) stellen++;
            return "N" + stellen.ToString(CultureInfo.InvariantCulture);
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
        ///
        /// <para><b>MIT ZEITAUSSCHNITT seit dem Anwenderwunsch W8‑E‑2</b> (Windows-Abnahme
        /// 05.09.2026): Derselbe Zeichenweg trägt jetzt auch die Woche und den Tag. Das
        /// Fenster wird ZUERST angelegt — Höchstwert, Skala und Linie beziehen sich danach
        /// auf den Ausschnitt, genau wie es <see cref="ErzeugerStapel"/> hält. Die x-Achse
        /// wechselt dabei von den Monatsgrenzen auf die WIRKLICHEN Jahresstunden
        /// (<see cref="XAchseFenster"/>): In einer Julinacht sagt „Jan" nichts mehr, die
        /// Stunde 4 700 schon.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Jahresübersicht".</param>
        /// <param name="stundenwerte">Der Jahresverlauf; jede Länge ≥ 2 geht.</param>
        /// <param name="yTitel">Beschriftung der y-Achse, z. B. „Wärmebedarf [kW]".</param>
        /// <param name="farbe">Linienfarbe (Vorläufer: <c>SteelBlue</c>).</param>
        /// <param name="fenster">
        /// Der Zeitausschnitt (Woche, Tag oder ein aufgezogener Bereich); <c>null</c> = das
        /// ganze Jahr und damit Bild für Bild das des Bestands.
        /// </param>
        public static byte[] Jahresverlauf(string titel, double[] stundenwerte, string yTitel,
                                           SKColor farbe, Achsenfenster fenster = null)
            => SkiaMaler.Png(JahresverlaufModell(titel, stundenwerte, yTitel, farbe, fenster));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a).
        ///
        /// <para><b>Die x-Achse zählt JAHRESSTUNDEN</b> — ohne Fenster 0 … n−1, mit
        /// Fenster dessen Grenzen. Die Reihe steht deshalb im Ausschnitt an derselben
        /// Stunde wie in der Vollansicht.</para>
        /// </summary>
        public static Zeichenmodell JahresverlaufModell(string titel, double[] stundenwerte,
                                                        string yTitel, SKColor farbe,
                                                        Achsenfenster fenster = null)
            => JahresverlaufModell(titel, stundenwerte, yTitel, farbe.Ton(), fenster);

        /// <summary>
        /// Dasselbe Bild mit AUSDRÜCKLICH genannter Farbrolle (DG-E5) — der Weg für
        /// jede Hülle, die die Größe benennen kann.
        /// </summary>
        public static Zeichenmodell JahresverlaufModell(string titel, double[] stundenwerte,
                                                        string yTitel, Farbrolle rolle,
                                                        Achsenfenster fenster = null)
            => JahresverlaufModell(titel, stundenwerte, yTitel, Farbton.Aus(rolle), fenster);

        private static Zeichenmodell JahresverlaufModell(string titel, double[] stundenwerte,
                                                         string yTitel, Farbton ton,
                                                         Achsenfenster fenster)
        {
            int W = 978, H = 542;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel, W));
            var rc = SKRect.Create(100f, 80f, W - 140f, 380f);

            // Der Zuschnitt steht GANZ oben - alles darunter rechnet mit dem
            // Ausschnitt, ohne davon zu wissen. gesamt merkt sich die volle Laenge:
            // Die Achsenbeschriftung nennt Jahresstunden, nicht Fensterstunden.
            int gesamt = stundenwerte == null ? 0 : stundenwerte.Length;
            double[] werte = Ausschnitt(stundenwerte, fenster);

            if (werte == null || werte.Length < 2)
            {
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl =>
                        Text(zl, BerichtTexte.T("Kein Jahresverlauf vorhanden."), f, Farbrolle.ACHSE,
                             rc.Left, rc.Top + 20f));
                return z;
            }

            double maxWert = 0;
            foreach (double w in werte) if (w > maxWert) maxWert = w;
            if (fenster != null && fenster.YAnteil > 0) maxWert *= fenster.YAnteil;
            (double schritt, double max, string format) = BedarfsSkala(maxWert);

            z.Markiert("yachse", zy => BedarfsRasterOhneKreuz(zy, rc, schritt, max, format));
            Achsenkreuz(z, rc);

            if (fenster == null)
            {
                // Monatsgrenzen statt Stundenzahlen (siehe Kopf).
                KeyValuePair<int[], string[]> ticks = MonatsTicks365();
                var raster = Stift(Farbrolle.RASTER, 1f);
                z.Markiert("xachse", zx =>
                {
                    using (var f = Schrift(15f))
                        for (int m = 0; m < 12; m++)
                        {
                            float x = rc.Left + (float)(ticks.Key[m] * 24) / werte.Length * rc.Width;
                            if (x > rc.Right) break;
                            zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                            Text(zx, ticks.Value[m], f, Farbrolle.ACHSE, x + 4f, rc.Bottom + 8f);
                        }
                });
            }
            else z.Markiert("xachse", zx => XAchseFenster(zx, rc, fenster, gesamt));

            using (var f = Schrift(15f))
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER (Etappe E3): x zaehlt die
            // Jahresstunden des BILDES, y die Bedarfsskala ab null.
            double xVon = fenster == null ? 0.0
                        : Math.Max(0, Math.Min(gesamt, fenster.Von));
            z.Flaeche = new Zeichenflaeche(
                rc.Modellrahmen(),
                new Datenfenster(xVon, xVon + werte.Length - 1, 0, max),
                Achsenart.Stunden);

            // 8 760 Punkte auf rund 840 Bildpunkte: jeder n-te genügt (wie beim
            // Kostenprofil) — mehr Punkte als Pixel zeichnen dasselbe Bild langsamer.
            // Im Fenster stehen weniger Werte als Bildpunkte; dann ist die
            // Schrittweite 1 und jede Stunde wird gezeichnet.
            int schrittweite = Math.Max(1, werte.Length / (int)rc.Width);
            var punkte = new List<SKPoint>();
            for (int i = 0; i < werte.Length; i += schrittweite)
            {
                float x = rc.Left + (float)i / (werte.Length - 1) * rc.Width;
                float y = (float)(rc.Bottom - Math.Max(0, werte[i]) / max * rc.Height);
                punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
            }
            // Das Bild fuehrt keine Legende; die y-Beschriftung benennt die Groesse.
            string name = string.IsNullOrEmpty(yTitel) ? (titel ?? "") : yTitel;
            z.Markiert("reihe:" + name, zr =>
                Linienzug(zr, punkte.ToArray(), Stift(ton, 2f, null, Strichverbindung.Rund)));

            z.FuegeReihe(new Datenreihe(name, ton, 2f, null, werte, z.Flaeche.Daten));

            return z;
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
        /// <param name="Ton">Der Farbton samt Rolle (DG-E5); <c>null</c> heißt: Die
        /// Farbe kam als Wert herein und bekommt ihre Rolle über die Rückwärtssuche.</param>
        public sealed record Punktreihe(string Name, IReadOnlyList<(double X, double Y)> Punkte,
                                        SKColor Farbe, Farbton Ton = null)
        {
            /// <summary>
            /// <b>Die Punktreihe mit ihrer FARBROLLE</b> (DG-E5): Die Deckung gehört
            /// zum Bildaufbau und bleibt, der Anwender wählt den Ton.
            /// </summary>
            public Punktreihe(string name, IReadOnlyList<(double X, double Y)> punkte,
                              Farbrolle rolle, byte deckung)
                : this(name, punkte,
                       Farbpalette.Aktuell.Loese(Farbton.Aus(rolle).MitDeckung(deckung)).Skiafarbe(),
                       Farbton.Aus(rolle).MitDeckung(deckung)) { }

            /// <summary>Die Farbrolle der Reihe; <c>UNBENANNT</c>, solange sie keine trägt.</summary>
            public Farbrolle Rolle => Ton == null ? Farbrolle.UNBENANNT : Ton.Rolle;
        }

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
        /// <param name="fenster">DATENZOOM (Windows-Abnahme 05.09.2026): der Zeitausschnitt,
        /// den der Anwender aufgezogen hat; <c>null</c> = das ganze Jahr. <b>Nur der
        /// ZEITausschnitt gilt</b> — die Prozentachse dieses Bildes ist per Definition
        /// 0…100 % des Jahreshöchstwerts, und der Bezugswert bleibt deshalb der der
        /// GANZEN Reihe. Ein senkrechter Zoom höbe die Aussage „so viel Prozent der
        /// Jahresspitze“ auf; dafür gibt es den Bildzoom des Bausteins.</param>
        public static byte[] GanglinieNormiert(string titel, IReadOnlyList<Reihe> reihen,
                                               string yTitel, Achse achse, bool sortiert,
                                               Achsenfenster fenster = null)
            => SkiaMaler.Png(GanglinieNormiertModell(titel, reihen, yTitel, achse, sortiert,
                                                     fenster));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a).
        ///
        /// <para><b>Die Datenreihen führen PROZENT</b>, nicht die Rohwerte: Das Bild
        /// zeichnet den Anteil am gemeinsamen Höchstwert, und das Modell trägt die
        /// Werte des BILDES. In der Dauerlinie sind sie zusätzlich absteigend sortiert —
        /// die x-Achse zählt dann den Rang, nicht die Stunde.</para>
        ///
        /// <para>x zählt Jahresstunden (im Fenster dessen Grenzen), y läuft von 0 bis
        /// 100,2 %.</para>
        /// </summary>
        public static Zeichenmodell GanglinieNormiertModell(string titel, IReadOnlyList<Reihe> reihen,
                                                            string yTitel, Achse achse, bool sortiert,
                                                            Achsenfenster fenster = null)
        {
            int W = 1240, H = 560;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));
            var rc = SKRect.Create(100f, 110f, W - 140f, 360f);

            List<Reihe> ganz = Brauchbare(reihen);
            List<Reihe> gueltig = fenster == null
                ? ganz
                : Brauchbare(Zugeschnitten(ganz, fenster));
            if (gueltig.Count == 0)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            Legende(z, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                    100f, 66f, W - 30f);

            // Der gemeinsame Bezugswert (siehe Kopf) — aus der GANZEN Reihe, damit
            // 100 % im Ausschnitt dasselbe heisst wie in der Vollansicht.
            double bezug = ganz.Max(r => r.Werte.Max());
            if (bezug <= 0) bezug = 1;

            z.Markiert("yachse", zy => ProzentRasterOhneKreuz(zy, rc));
            Achsenkreuz(z, rc);
            if (fenster == null) z.Markiert("xachse", zx => XAchse(zx, rc, achse, gueltig[0].Werte.Length));
            else z.Markiert("xachse", zx => XAchseFenster(zx, rc, fenster, ganz[0].Werte.Length));
            using (var f = Schrift(15f))
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));

            // DIE ZEICHENFLAECHE: x die Stuetzstellen des Bildes, y die Prozentachse.
            double xVon = fenster == null ? 0.0
                        : Math.Max(0, Math.Min(ganz[0].Werte.Length, fenster.Von));
            z.Flaeche = new Zeichenflaeche(
                rc.Modellrahmen(),
                new Datenfenster(xVon, xVon + gueltig[0].Werte.Length - 1, 0, Y_PROZENT_MAX),
                Zeitachsenart(sortiert));

            foreach (Reihe r in gueltig)
            {
                double[] werte = sortiert ? AbsteigendKopie(r.Werte) : r.Werte;
                double[] prozent = Normiert(werte, bezug);
                float staerke = r.Breite > 0 ? r.Breite : 2f;
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    ZeichneLinie(zr, rc, prozent, 0, Y_PROZENT_MAX, r.Farbe, staerke));

                z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, null, prozent,
                                            new Datenfenster(xVon, xVon + prozent.Length - 1,
                                                             0, Y_PROZENT_MAX)));
            }

            return z;
        }

        /// <summary>Obergrenze der Prozentachse — woertlich aus <c>init_Chart</c> :3378.</summary>
        private const double Y_PROZENT_MAX = 100.2;

        // ------------------------------------------------------------------ B2 / B3

        /// <summary>
        /// <b>B2 — ERZEUGERSTAPEL.</b> Das Arbeitspferd der Welle: gestapelte Erzeugung,
        /// Linien darueber, eine Konturlinie darunter — und wahlweise Reihen auf
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
        /// starten bei null.</para>
        ///
        /// <para><b>Windows-Abnahme 09.09.2026, Befund W11b-B-18 — NEBENEINANDER NUR BEI
        /// WENIGEN STUETZSTELLEN.</b> Bis dahin standen beide Gruppen IMMER nebeneinander,
        /// jede in ihrer Bildhaelfte — bei einer Kategorieachse mit wenigen Saeulen
        /// gewollt, bei einer Jahresganglinie mit 8 760 Stundenwerten aber falsch: Der
        /// Bedarf erschien links, die Produktion rechts, obwohl beide fuer JEDE Stunde
        /// gelten (Anwenderbefund: Bild „Waermelast Jahresganglinie" der Waermepumpe).
        /// Jetzt nur noch nebeneinander, wenn die Achse keine Stundenachse ist UND die
        /// Reihen wenige Stuetzstellen haben; sonst liegen beide Gruppen UEBEREINANDER
        /// ueber der vollen Breite, die Saeulengruppe halbtransparent, damit die Flaeche
        /// darunter sichtbar bleibt (siehe <see cref="StapelZeichnen"/>).</para>
        /// </summary>
        /// <param name="titel">Ueberschrift.</param>
        /// <param name="stapel">Die gestapelten Reihen in Kaskadenreihenfolge.</param>
        /// <param name="linien">Linien ueber dem Stapel, in Zeichenreihenfolge.</param>
        /// <param name="kontur">Die Summenlinie unter dem Stapel; <c>null</c> = keine.</param>
        /// <param name="yTitel">Beschriftung der linken y-Achse.</param>
        /// <param name="achse">Monatsgrenzen oder Jahresstunden.</param>
        /// <param name="sortiert">Dauerlinie statt Ganglinie — dann ohne Stapel.</param>
        /// <param name="zweiteAchsen">B3: die Reihen mit eigener Skala rechts;
        /// <c>null</c> oder leer = keine zweite Achse. <b>Seit #234 eine LISTE</b>
        /// (vorher genau eine Reihe): Auf der zweiten Achse des Wärmegangs stehen die
        /// Füllstände ALLER gewählten Pufferspeicher, und die sind zu mehreren. Sie
        /// teilen sich eine gemeinsame Obergrenze — zwei Speicher mit eigener Skala
        /// wären zwei Kurven, die dasselbe Bild meinen und Verschiedenes zeigen.</param>
        /// <param name="y2Titel">Beschriftung der rechten y-Achse.</param>
        /// <param name="fenster">DATENZOOM (Windows-Abnahme 05.09.2026): der Ausschnitt,
        /// den der Anwender aufgezogen hat; <c>null</c> = das ganze Jahr. Zugeschnitten
        /// wird ALLES — Stapel, Linien, Kontur und die Reihen der zweiten Achse —, und
        /// zwar VOR jeder Rechnung: Höchstwert, Sortierung und Stapelsumme beziehen sich
        /// dann auf den Ausschnitt, so wie es der Achsenzoom des Vorbilds tat.</param>
        public static byte[] ErzeugerStapel(string titel, IReadOnlyList<Reihe> stapel,
                                            IReadOnlyList<Reihe> linien, Reihe kontur,
                                            string yTitel, Achse achse, bool sortiert,
                                            IReadOnlyList<Reihe> zweiteAchsen = null,
                                            string y2Titel = null,
                                            Achsenfenster fenster = null)
            => SkiaMaler.Png(ErzeugerStapelModell(titel, stapel, linien, kontur, yTitel, achse,
                                                  sortiert, zweiteAchsen, y2Titel, fenster));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a) — das Bild mit den
        /// meisten Zutaten der Gruppe.
        ///
        /// <para><b>Jede Stapelschicht ist eine FLÄCHE</b> (DG-E3-2): <c>Werte</c> ist
        /// ihre Oberkante (die Summe bis einschließlich dieser Schicht),
        /// <c>Unten</c> die Summe der Schichten darunter. Bedarfs- und Konturlinien
        /// bleiben Linien; in der Dauerlinie (<paramref name="sortiert"/>) wird der
        /// Stapel ohnehin zu Linien.</para>
        ///
        /// <para><b>Die ZWEITE Achse trägt ihr eigenes Fenster</b> (DG-E3-1): Der
        /// Speicherinhalt rechts hat seine eigene Skala 0…max2 und steht trotzdem im
        /// selben inneren <c>&lt;svg&gt;</c>. Ihre Achsenlinie, ihre Zahlen und ihr
        /// Titel tragen die Marke <c>yachse2</c>.</para>
        ///
        /// <para>x zählt Stützstellen (Stunden oder Viertelstunden, im Fenster dessen
        /// Grenzen); zwei NEBENEINANDER gestellte Stapelgruppen sitzen über ihr eigenes
        /// Fenster in ihrer Bildhälfte.</para>
        /// </summary>
        public static Zeichenmodell ErzeugerStapelModell(string titel, IReadOnlyList<Reihe> stapel,
                                                         IReadOnlyList<Reihe> linien, Reihe kontur,
                                                         string yTitel, Achse achse, bool sortiert,
                                                         IReadOnlyList<Reihe> zweiteAchsen = null,
                                                         string y2Titel = null,
                                                         Achsenfenster fenster = null)
        {
            int W = 1240, H = 560;

            // Anschlag und Hoehe der Legende stehen als Namen da - das Rechteck
            // unten rechnet mit denselben Werten (Muster aus Verlaufsbild, W11b-B-28).
            const float LEGENDE_X = 100f, LEGENDE_Y = 66f;

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            // Der Zuschnitt steht GANZ oben: Alles darunter rechnet dann mit dem
            // Ausschnitt, ohne davon zu wissen.
            int gesamt = Laenge(stapel, linien, kontur, zweiteAchsen);
            if (fenster != null)
            {
                stapel = Zugeschnitten(stapel, fenster);
                linien = Zugeschnitten(linien, fenster);
                kontur = Zugeschnitten(kontur, fenster);
                zweiteAchsen = Zugeschnitten(zweiteAchsen, fenster);
            }

            List<Reihe> y2G = Brauchbare(zweiteAchsen);
            bool mitY2 = y2G.Count > 0;
            var rc = SKRect.Create(100f, 110f, W - (mitY2 ? 190f : 140f), 360f);

            List<Reihe> stapelG = Brauchbare(stapel);
            List<Reihe> linienG = Brauchbare(linien);
            bool mitKontur = Brauchbar(kontur);

            // Der Leerhinweis gilt erst, wenn AUCH die zweite Achse nichts trägt
            // (#234): Wer alle Erzeuger abwählt und nur einen Speicher stehen lässt,
            // hat eine Reihe gewählt — und bekommt sie zu sehen.
            if (stapelG.Count == 0 && linienG.Count == 0 && !mitKontur && !mitY2)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            // Legende: Kontur zuerst (sie steht im Bestand als erste Serie), dann der
            // Stapel, dann die Linien, zuletzt die Reihen der zweiten Achse.
            var leg = new List<Segment>();
            if (mitKontur) leg.Add(new Segment(kontur.Name, 0, kontur.Farbe));
            leg.AddRange(stapelG.Select(r => new Segment(r.Name, 0, r.Farbe)));
            leg.AddRange(linienG.Select(r => new Segment(r.Name, 0, r.Farbe)));
            leg.AddRange(y2G.Select(r => new Segment(r.Name, 0, r.Farbe)));

            // AUFTRAG #240: DIE LEGENDE MACHT SICH SELBST PLATZ - dasselbe Muster
            // wie in Verlaufsbild (W11b-B-28). Ein Waermebild mit zwei Speichern,
            // Kontur, Bedarfslinie und fuenf Erzeugern traegt NEUN Eintraege; die
            // brechen in eine zweite Zeile um, und die lag bis hierher auf dem
            // y-Achsentitel, der 24 px ueber der Zeichenflaeche steht. Jede Zeile
            // ueber der ersten schiebt das Rechteck um genau ihre Hoehe nach unten;
            // die Flaeche wird dabei niedriger, statt den Platz unter der x-Achse
            // aufzuzehren, wo deren Beschriftung steht. Bei EINER Legendenzeile ist
            // der Versatz null - jedes bisherige Bild bleibt byte-gleich.
            float legendenhoehe = Legende(z, leg, LEGENDE_X, LEGENDE_Y, W - 30f);
            float schub = Math.Max(0f, legendenhoehe - LEGENDE_ZEILE);
            if (schub > 0f) rc = SKRect.Create(rc.Left, rc.Top + schub, rc.Width, rc.Height - schub);

            int n = stapelG.Count > 0 ? stapelG[0].Werte.Length
                  : linienG.Count > 0 ? linienG[0].Werte.Length
                  : mitKontur ? kontur.Werte.Length
                  : y2G[0].Werte.Length;

            // Obergrenze: die hoechste Stapelsumme JE GRUPPE — UNVERAENDERT durch
            // W11b-B-18 (09.09.2026): Ob die Gruppen nebeneinander oder uebereinander
            // liegen, ihre Werte werden NICHT aufeinandergerechnet (Bedarf und
            // Produktion sind unabhaengige Groessen mit gemeinsamer Nulllinie).
            // Dazu Linien und Kontur.
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
            // Der senkrechte Anteil des aufgezogenen Rechtecks: Die Null bleibt
            // unten, die obere Kante wird die neue Obergrenze.
            if (fenster != null && fenster.YAnteil > 0) max = Nice(max * fenster.YAnteil);
            if (max <= 0) max = 1;

            z.Markiert("yachse", zy => YRasterOhneKreuz(zy, rc, max));
            Achsenkreuz(z, rc);
            if (fenster == null) z.Markiert("xachse", zx => XAchse(zx, rc, achse, n));
            else z.Markiert("xachse", zx => XAchseFenster(zx, rc, fenster, gesamt));
            using (var f = Schrift(15f))
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER DER LINKEN ACHSE (Etappe E3).
            double xVon = fenster == null ? 0.0 : Math.Max(0, Math.Min(gesamt, fenster.Von));
            var fensterLinks = new Datenfenster(xVon, xVon + n - 1, 0, max);
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(), fensterLinks,
                                           Zeitachsenart(sortiert));

            // (1) Kontur UNTER dem Stapel.
            if (mitKontur)
            {
                double[] konturwerte = sortiert ? AbsteigendKopie(kontur.Werte) : kontur.Werte;
                float konturstaerke = kontur.Breite > 0 ? kontur.Breite : 4f;
                z.Markiert("reihe:" + (kontur.Name ?? ""), zr =>
                    ZeichneLinie(zr, rc, konturwerte, 0, max, kontur.Farbe, konturstaerke));
                z.FuegeReihe(new Datenreihe(kontur.Name ?? "", Ton(kontur), konturstaerke,
                                            null, konturwerte,
                                            Reihenfenster(fensterLinks, konturwerte.Length)));
            }

            // (2) Der Stapel.
            if (sortiert)
            {
                // Ohne Stapel: jede Reihe fuer sich als Dauerlinie, BorderWidth 4.
                foreach (Reihe r in stapelG)
                {
                    double[] werte = AbsteigendKopie(r.Werte);
                    float staerke = r.Breite > 0 ? r.Breite : 4f;
                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                        ZeichneLinie(zr, rc, werte, 0, max, r.Farbe, staerke));
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, null, werte,
                                                Reihenfenster(fensterLinks, werte.Length)));
                }
            }
            else
            {
                bool zweiGruppen = stapelG.Any(r => r.Stapelgruppe == Stapelart.Flaeche) &&
                                   stapelG.Any(r => r.Stapelgruppe == Stapelart.Saeule);

                // Windows-Abnahme 09.09.2026, Befund W11b-B-18: „Nebeneinander" ist nur
                // bei einer echten Kategorieachse mit wenigen Stuetzstellen richtig
                // (z. B. zwoelf Monatssaeulen) — bei einer Stundenachse oder vielen
                // Stuetzstellen gilt jede Stuetzstelle fuer BEIDE Gruppen gleichzeitig,
                // und nebeneinander zerschnitt das Bild faelschlich in eine Bedarfs-
                // und eine Produktionshaelfte (Anwenderbefund: Jahresganglinie der
                // Waermepumpe, Bedarf und Produktion stimmen nicht ueberein).
                const int NEBENEINANDER_GRENZE = 60;
                bool nebeneinander = zweiGruppen && achse != Achse.Jahresstunden &&
                                     n <= NEBENEINANDER_GRENZE;

                StapelZeichnen(z, rc, stapelG, Stapelart.Flaeche, n, max,
                               nebeneinander ? -0.22f : 0f, nebeneinander ? 0.5f : 1f,
                               (byte)210, z, fensterLinks);
                // Ueberlagert (nicht nebeneinander): die Saeulengruppe (Produktion)
                // HALBTRANSPARENT ueber der Flaeche (Bedarf), damit der Bedarf darunter
                // sichtbar bleibt.
                StapelZeichnen(z, rc, stapelG, Stapelart.Saeule, n, max,
                               nebeneinander ? 0.22f : 0f, nebeneinander ? 0.5f : 1f,
                               nebeneinander ? (byte)210 : (byte)150, z, fensterLinks);
                // Reihen ohne ausdrueckliche Gruppe bilden den gemeinsamen Stapel.
                StapelZeichnen(z, rc, stapelG, Stapelart.Keine, n, max, 0f, 1f,
                               (byte)210, z, fensterLinks);
            }

            // (3) Die Linien darueber, in Zeichenreihenfolge.
            foreach (Reihe r in linienG)
            {
                double[] werte = sortiert ? AbsteigendKopie(r.Werte) : r.Werte;
                float staerke = r.Breite > 0 ? r.Breite : 2.5f;
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    ZeichneLinie(zr, rc, werte, 0, max, r.Farbe, staerke));
                z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, null, werte,
                                            Reihenfenster(fensterLinks, werte.Length)));
            }

            // (4) B3 — die zweite y-Achse mit EIGENER, GEMEINSAMER Skala.
            //
            // #234: Bis dahin trug sie genau EINE Reihe. Jetzt sind es beliebig
            // viele, und sie teilen sich die Obergrenze — sonst zeigten zwei
            // Speicherfuellstaende auf derselben Achse verschiedene Massstaebe.
            // Die ACHSE selbst faerbt sich nur bei EINER Reihe in deren Farbe (wie
            // bisher); bei mehreren waere das die Farbe einer beliebigen von ihnen,
            // deshalb steht sie dann neutral in DimGray wie die linke.
            if (mitY2)
            {
                double max2 = 0;
                foreach (Reihe r in y2G) max2 = Math.Max(max2, r.Werte.Max());
                max2 = Nice(max2);
                if (max2 <= 0) max2 = 1;

                Farbton achsenfarbe = y2G.Count == 1
                                    ? Ton(y2G[0])
                                    : Farbton.Aus(Farbrolle.ACHSE);

                // DG-E3-1: Die Reihen der zweiten Achse bekommen IHR Fenster - selbe
                // x-Spanne, eigene y-Skala 0…max2.
                foreach (Reihe r in y2G)
                {
                    double[] werte = sortiert ? AbsteigendKopie(r.Werte) : r.Werte;
                    float staerke = r.Breite > 0 ? r.Breite : 2f;
                    z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                        ZeichneLinie(zr, rc, werte, 0, max2, r.Farbe, staerke));
                    // DG-E3-12: Die Reihe SAGT, dass sie rechts steht — die Oberfläche
                    // muss es nicht mehr aus ihrer y-Spanne erraten.
                    z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, null, werte,
                                                new Datenfenster(fensterLinks.XVon,
                                                                 fensterLinks.XVon + werte.Length - 1,
                                                                 0, max2),
                                                Achsenseite: Achsenseite.Rechts));
                }

                z.Markiert("yachse2", zy2 =>
                {
                    zy2.Linie(rc.Right, rc.Top, rc.Right, rc.Bottom, Stift(achsenfarbe, 2f));
                    using (var f = Schrift(15f))
                    {
                        for (int i = 0; i <= 4; i++)
                        {
                            double wert = max2 * i / 4.0;
                            float y = (float)(rc.Bottom - wert / max2 * rc.Height);
                            Text(zy2, wert.ToString("N0", DE), f, achsenfarbe,
                                 rc.Right + 8f, y - TextHoehe(f) / 2f);
                        }
                        // #234: Der Titel der zweiten Achse stand starr bei
                        // rc.Right − 40. „Waermelast" passte damit noch ins Bild,
                        // „Speicherinhalt [kWh]" nicht mehr - die Einheit wurde am
                        // rechten Rand abgeschnitten. Er rueckt jetzt so weit nach
                        // links, wie er braucht, und endet 10 Bildpunkte vor der
                        // Kante; kurze Titel stehen unveraendert.
                        string t2 = y2Titel ?? "";
                        Text(zy2, t2, f, achsenfarbe,
                             Math.Min(rc.Right - 40f, W - 10f - f.MeasureText(t2)), rc.Top - 24f);
                    }
                });
            }

            return z;
        }

        /// <summary>
        /// Das Fenster EINER Reihe zur Zeichenfläche: dieselbe y-Skala, aber die
        /// x-Spanne über ihre EIGENE Länge (DG-E3-1). Eine kürzere Reihe zeichnet das
        /// Bild über die volle Breite, nicht bis zur Hälfte.
        /// </summary>
        private static Datenfenster Reihenfenster(Datenfenster flaeche, int laenge)
            => new Datenfenster(flaeche.XVon, flaeche.XVon + laenge - 1,
                                flaeche.YVon, flaeche.YBis);

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
        /// nebeneinander stehen koennen. <paramref name="alpha"/> (Windows-Abnahme
        /// 09.09.2026, Befund W11b-B-18): stehen zwei Gruppen stattdessen UEBEREINANDER
        /// (volle Breite je Gruppe), zeichnet die OBERE Gruppe mit einem niedrigeren Wert
        /// halbtransparent, damit die untere sichtbar bleibt; die Vorgabe 210 entspricht
        /// der bisherigen, undurchsichtigeren Flaeche.
        /// </summary>
        /// <param name="modell">
        /// Das Modell, dem die Schichten zusätzlich als <c>Datenreihe</c> beigelegt
        /// werden (Etappe E3); <c>null</c> = nur zeichnen.
        /// </param>
        /// <param name="fenster">
        /// Das Datenfenster der Zeichenfläche. Stehen zwei Gruppen NEBENEINANDER,
        /// bekommt jede Schicht daraus ihr eigenes, schmaleres x-Fenster — dieselbe
        /// Verschiebung, die <paramref name="versatz"/> und <paramref name="breite"/>
        /// im Bild machen (DG-E3-1).
        /// </param>
        private static void StapelZeichnen(IZeichenziel z, SKRect rc, List<Reihe> stapel,
                                           Stapelart gruppe, int n, double max,
                                           float versatz, float breite, byte alpha = 210,
                                           Zeichenmodell modell = null,
                                           Datenfenster fenster = null)
        {
            var teil = stapel.Where(r => r.Stapelgruppe == gruppe).ToList();
            if (teil.Count == 0) return;

            SKRect ziel = breite >= 1f
                ? rc
                : SKRect.Create(rc.Left + rc.Width * (0.5f + versatz - breite / 2f),
                                rc.Top, rc.Width * breite, rc.Height);

            Datenfenster gruppenfenster = fenster;
            if (fenster != null && breite < 1f)
            {
                double spanne = fenster.XBis - fenster.XVon;
                double links = fenster.XVon + spanne * (0.5 + versatz - breite / 2.0);
                gruppenfenster = new Datenfenster(links, links + spanne * breite,
                                                  fenster.YVon, fenster.YBis);
            }

            var unten = new double[n];
            foreach (Reihe r in teil)
            {
                var oben = new double[n];
                for (int i = 0; i < n; i++)
                    oben[i] = unten[i] + (i < r.Werte.Length ? Math.Max(r.Werte[i], 0) : 0);

                double[] unterkante = unten;
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    ZeichneFlaeche(zr, ziel, unterkante, oben, max, r.Farbe, alpha));

                // DG-E3-2: dieselbe Schicht als FLAECHE in Datenwerten - Oberkante die
                // Stapelsumme bis hierher, Unterkante die Summe darunter. Die Farbe
                // traegt dieselbe Deckung wie im Bild.
                if (modell != null)
                    modell.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r, alpha),
                                                     0f, null, oben, gruppenfenster,
                                                     Reihenart.Flaeche, unterkante));
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
        ///
        /// <para><b>Windows-Abnahme 05.09.2026, Befund W11b-B-3 — RUNDE TEILUNG UND
        /// PLATZ AN DEN RAENDERN.</b> Die x-Achse trug bis dahin fuenf Marken, die den
        /// vorkommenden Bereich in vier gleiche Teile schnitten: Bei einem Jahr von
        /// −18,2 °C bis 20,3 °C stand dort „−18,2 · −8,6 · 1,1 · 10,7 · 20,3“ — krumme
        /// Zahlen, an denen sich nichts ablesen laesst. Jetzt laeuft die Achse von einer
        /// RUNDEN Stufe zur naechsten (dieselbe Stufenfolge wie <see cref="Jahresgang"/>:
        /// 1 / 2 / 2,5 / 5 × 10^k), und der Wertebereich wird auf diese Stufen
        /// AUFGERUNDET — aus dem Beispiel wird „−20 · −15 · −10 · −5 · 0 · 5 · 10 · 15 ·
        /// 20“. Dazu bekommt das Bild rechts Platz (die letzte Marke stand auf der
        /// Kante), die Legende rueckt hoch und der y-Achsentitel darunter: Beide lagen
        /// mit 66 und 86 Bildpunkten so dicht, dass die Schriftzeilen sich beruehrten.
        /// Die Bildmasse bleiben 1 240 × 560.</para>
        /// </summary>
        public static byte[] Streuwolke(string titel, string xTitel, string yTitel,
                                        IReadOnlyList<Punktreihe> reihen)
            => SkiaMaler.Png(StreuwolkeModell(titel, xTitel, yTitel, reihen));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe b) — der Rumpf, den
        /// <see cref="Streuwolke"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>x ist die AUSSENTEMPERATUR</b> (<see cref="Achsenart.Wert"/>,
        /// Einheit „°C"), y die Leistung der Stunde. Jede Reihe wird eine
        /// <see cref="Reihenart.Punkte"/> mit ihrer x-Stelle je Punkt (DG-E3-5); im
        /// SVG entsteht daraus EIN Pfad je Reihe, ungebündelt — die Verdichtung der
        /// Wolke IST ihre Aussage.</para>
        ///
        /// <para><b>Geklemmt wird im SVG nicht.</b> Das Bild schiebt einen negativen
        /// Wert auf die Nulllinie und einen zu großen auf den oberen Rand; das innere
        /// <c>&lt;svg&gt;</c> schneidet stattdessen ab. Für eine Leistungswolke, die
        /// bei null beginnt, fällt das zusammen.</para>
        /// </summary>
        public static Zeichenmodell StreuwolkeModell(string titel, string xTitel, string yTitel,
                                                     IReadOnlyList<Punktreihe> reihen)
        {
            int W = 1240, H = 560;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));
            // Rechts 90 statt 40 Bildpunkte: Dort steht die letzte x-Marke, und
            // eine Marke wie "−20" ragt sonst ueber den Bildrand hinaus.
            var rc = SKRect.Create(100f, 110f, W - 190f, 360f);

            var gueltig = (reihen ?? new List<Punktreihe>())
                .Where(r => r != null && r.Punkte != null && r.Punkte.Count > 0)
                .ToList();
            if (gueltig.Count == 0)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            Legende(z, gueltig.Select(r => new Segment(r.Name, 0, Undurchsichtig(r.Farbe))).ToList(),
                    100f, 56f, W - 30f);

            double xRoh0 = gueltig.Min(r => r.Punkte.Min(p => p.X));
            double xRoh1 = gueltig.Max(r => r.Punkte.Max(p => p.X));
            if (xRoh1 - xRoh0 < 1e-9) { xRoh1 = xRoh0 + 1; }

            // Runde Teilung: Schrittweite aus der Spanne, Bereich auf die Stufen
            // aufgerundet. Fuenf bis acht Marken - genug zum Ablesen, wenig genug,
            // dass sich die Beschriftungen nicht beruehren.
            double xSchritt = RundeStufe((xRoh1 - xRoh0) / 6.0);
            double xMin = Math.Floor(xRoh0 / xSchritt) * xSchritt;
            double xMax = Math.Ceiling(xRoh1 / xSchritt) * xSchritt;
            if (xMax - xMin < 1e-9) { xMax = xMin + xSchritt; }

            double yMax = Nice(Math.Max(0, gueltig.Max(r => r.Punkte.Max(p => p.Y))));
            if (yMax <= 0) yMax = 1;

            z.Markiert("yachse", zy => YRasterOhneKreuz(zy, rc, yMax));
            Achsenkreuz(z, rc);

            // x-Skala: eine Marke je runder Stufe.
            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(15f))
                    for (double wert = xMin; wert <= xMax + xSchritt * 1e-6; wert += xSchritt)
                    {
                        float x = rc.Left + (float)((wert - xMin) / (xMax - xMin)) * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        // Die Null soll "0" heissen und nicht "-0" (Math.Floor auf
                        // negativen Zahlen liefert bei ganzzahligen Schritten -0).
                        string lab = (wert == 0 ? 0.0 : wert).ToString("0.#", DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });
            using (var f = Schrift(15f))
            {
                // Der x-Titel steht UNTER den Marken (34 statt 30 Bildpunkte), der
                // y-Titel ueber der Flaeche und UNTER der Legende.
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE,
                         rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER (DG-E3-4/7) - VOR dem ersten
            // Reihenbefehl, sonst setzt der Schreiber das innere svg an die falsche
            // Stelle. x ist die AUSSENTEMPERATUR, y die Leistung ab null.
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(),
                                           new Datenfenster(xMin, xMax, 0, yMax),
                                           Achsenart.Wert, "°C");

            foreach (Punktreihe r in gueltig)
            {
                // Die Reihenfarbe kommt von AUSSEN und behaelt die Rueckwaertssuche.
                Zeichnung.Fuellung punkt = Flaeche(r.Farbe);
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                {
                    foreach (var p in r.Punkte)
                    {
                        if (double.IsNaN(p.X) || double.IsNaN(p.Y)) continue;
                        float x = rc.Left + (float)((p.X - xMin) / (xMax - xMin)) * rc.Width;
                        float y = (float)(rc.Bottom - Math.Max(0, p.Y) / yMax * rc.Height);
                        if (y < rc.Top) y = rc.Top;
                        zr.Kreis(x, y, 2.5f, null, punkt);
                    }
                });

                // DG-E3-5: dieselbe Wolke in DATENWERTEN. Die Strichbreite ist der
                // DURCHMESSER des Kreises im Bild (Radius 2,5) - im SVG zeichnet eine
                // runde Strichkappe daraus denselben Punkt.
                var xw = new double[r.Punkte.Count];
                var yw = new double[r.Punkte.Count];
                for (int i = 0; i < r.Punkte.Count; i++)
                { xw[i] = r.Punkte[i].X; yw[i] = r.Punkte[i].Y; }
                z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), 5f, null, yw,
                                            z.Flaeche.Daten, Reihenart.Punkte, null, null,
                                            xw, yTitel));
            }

            return z;
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
            return SkiaMaler.Png(RingModell(titel, segmente, mitteWert, mitteEinheit));
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c) — die
        /// Kurzfassung mit Legende und ohne Unterzeile.
        /// </summary>
        public static Zeichenmodell RingModell(string titel, IReadOnlyList<Ringsegment> segmente,
                                               double mitteWert, string mitteEinheit)
        {
            return RingModell(titel, segmente, mitteWert, mitteEinheit, null, true);
        }

        /// <summary>
        /// <b>B5 mit ZWEI Zusaetzen (Auftrag #222, SIM-E-3).</b> Dieselbe Zeichnung mit
        /// einer UNTERZEILE unter der Mittelzahl und der Wahl, die Legende aus dem Bild
        /// zu lassen.
        ///
        /// <para><b>Warum die Legende hinaus darf.</b> Das Dashboard der Simulations-
        /// uebersicht setzt die Legende als HTML NEBEN den Ring: Dort ist sie kopierbar,
        /// sie waechst mit der Schriftgroesse des Anwenders, und sie wird nicht
        /// abgeschnitten, wenn der Rahmen schmaler ist als das Bild. Im Bild bliebe sie
        /// eine Rastergrafik, die bei jeder Fenstergroesse dieselben Bildpunkte
        /// beansprucht. Ohne Legende wird das Bild QUADRATISCH (420 x 420): Die
        /// 720 x 560 des Vorlaeufers waren zu zwei Fuenfteln Legendenflaeche, und ein
        /// Ring mit 140 Bildpunkten Weissraum darunter steht in einer Spalte schief.</para>
        ///
        /// <para><b>Warum die Unterzeile.</b> „0,0 %" allein sagt nicht, WAS null ist.
        /// Die Unterzeile traegt den Satz dazu — „gedeckt" beim Waermering, „Netzbezug
        /// 100 %" beim Stromring ohne Erzeuger.</para>
        /// </summary>
        /// <param name="mitteUnterzeile">Kleiner Text unter der Mittelzahl; leer = ohne.</param>
        /// <param name="mitLegende">
        /// <c>false</c> laesst die Legende weg und liefert das quadratische Mass.
        /// </param>
        public static byte[] Ring(string titel, IReadOnlyList<Ringsegment> segmente,
                                  double mitteWert, string mitteEinheit,
                                  string mitteUnterzeile, bool mitLegende)
        {
            return SkiaMaler.Png(RingModell(titel, segmente, mitteWert, mitteEinheit,
                                            mitteUnterzeile, mitLegende));
        }

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c).
        ///
        /// <para><b>Ein reines Pixelbild</b> (DG-E3-7): keine Zeichenfläche, keine
        /// <c>Datenreihe</c>. Jedes Segment trägt <c>reihe:&lt;Name&gt;</c> und seinen
        /// ANTEIL als Wert — ein <see cref="Ringsegment"/> nennt keine Einheit, und
        /// der Anteil ist die Aussage des Rings; er steht in derselben Stufung („N1")
        /// wie die Zahl in der Mitte. Das Innenloch, die Mittelzahl und ihre
        /// Unterzeile bleiben ohne Marke: Sie gehören zum Bildaufbau, nicht zu einem
        /// Segment.</para>
        /// </summary>
        public static Zeichenmodell RingModell(string titel, IReadOnlyList<Ringsegment> segmente,
                                               double mitteWert, string mitteEinheit,
                                               string mitteUnterzeile, bool mitLegende)
        {
            int W = mitLegende ? 720 : 420;
            int H = mitLegende ? 560 : 420;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            var gueltig = (segmente ?? new List<Ringsegment>())
                .Where(s => s != null && s.Wert > 0 && !double.IsNaN(s.Wert) && !double.IsInfinity(s.Wert))
                .ToList();

            var rc = mitLegende ? SKRect.Create(210f, 90f, 300f, 300f)
                                : SKRect.Create(60f, 70f, 300f, 300f);

            if (gueltig.Count == 0)
            {
                z.Markiert("leerhinweis",
                           zl => Leerhinweis(zl, SKRect.Create(60f, 100f, W - 120f, 100f)));
                return z;
            }

            double summe = gueltig.Sum(s => s.Wert);
            float start = -90f;   // 12 Uhr, wie im Vorlaeufer
            foreach (Ringsegment s in gueltig)
            {
                float winkel = (float)(s.Wert / summe * 360.0);
                float von = start;               // fest fuer die Klammer
                z.Markiert("reihe:" + (s.Name ?? ""), Anteilwert(s.Name, s.Wert, summe),
                           zs => Kreissegment(zs, rc, von, winkel, Flaeche(s.Farbe)));
                start += winkel;
            }

            // Das Innenloch: ein weisser Kreis auf demselben Mittelpunkt. Genau so
            // machte es DonutChartDrawer.
            z.Kreis(rc.MidX, rc.MidY, rc.Width * 0.30f, null, Flaeche(Farbrolle.HINTERGRUND));

            string mitte = mitteWert.ToString("N1", DE) + (string.IsNullOrEmpty(mitteEinheit)
                                                               ? "" : " " + mitteEinheit);
            bool unterzeile = !string.IsNullOrEmpty(mitteUnterzeile);
            using (var f = Schrift(26f, fett: true))
            {
                float y = rc.MidY - TextHoehe(f) / 2f;
                if (unterzeile) y -= 9f;      // Platz fuer die kleine Zeile darunter
                Text(z, mitte, f, Farbrolle.STAMM, rc.MidX - f.MeasureText(mitte) / 2f, y);
            }

            if (unterzeile)
                using (var f = Schrift(12f))
                    Text(z, mitteUnterzeile, f, Farbrolle.ACHSE,
                         rc.MidX - f.MeasureText(mitteUnterzeile) / 2f,
                         rc.MidY + TextHoehe(f) / 2f + 2f);

            if (mitLegende)
                Legende(z, gueltig.Select(s => new Segment(s.Name, 0, s.Farbe)).ToList(),
                        60f, 430f, W - 30f);

            return z;
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
            => SkiaMaler.Png(MonatsStapelModell(titel, einheit, reihen));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c).
        ///
        /// <para><b>Ein reines Pixelbild</b> (DG-E3-7): keine Zeichenfläche, keine
        /// <c>Datenreihe</c>. Jede Stapelschicht trägt <c>reihe:&lt;Reihenname&gt;</c>
        /// — denselben Schlüssel, den der Helfer <see cref="Legende"/> als
        /// <c>legende:&lt;Reihenname&gt;</c> setzt — und als Wert
        /// „Jan · Eigenverbrauch: 1.234 kWh", im Zahlenformat SEINER y-Achse.</para>
        /// </summary>
        public static Zeichenmodell MonatsStapelModell(string titel, string einheit,
                                                       IReadOnlyList<Reihe> reihen)
        {
            int W = 978, H = 542;
            string[] monate = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun",
                                "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

            var z = Modell(W, H);
            z.Markiert("titel", zt =>
                Titel(zt, string.IsNullOrEmpty(einheit) ? (titel ?? "")
                                                        : (titel ?? "") + "  [" + einheit + "]", W));

            var gueltig = (reihen ?? new List<Reihe>())
                .Where(r => r != null && r.Werte != null && r.Werte.Length >= 12)
                .ToList();

            var rc = SKRect.Create(100f, 120f, W - 140f, 320f);
            if (gueltig.Count == 0)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            // Legende OBEN und ZENTRIERT (Docking.Top, StringAlignment.Center).
            float legendenbreite = Legendenbreite(gueltig);
            Legende(z, gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList(),
                    Math.Max(20f, (W - legendenbreite) / 2f), 68f, W - 20f);

            var summe = new double[12];
            for (int m = 0; m < 12; m++)
                foreach (Reihe r in gueltig) summe[m] += Math.Max(r.Werte[m], 0);
            double max = Nice(summe.Max());
            if (max <= 0) max = 1;

            z.Markiert("yachse", zy => YRasterOhneKreuz(zy, rc, max));
            Achsenkreuz(z, rc);
            using (var f = Schrift(15f))
                for (int m = 0; m < 12; m++)
                {
                    float x = rc.Left + (m + 0.5f) * rc.Width / 12f;
                    string lab = monate[m];
                    z.Markiert("xachse", zx =>
                        Text(zx, lab, f, Farbrolle.ACHSE,
                             x - f.MeasureText(lab) / 2f, rc.Bottom + 8f));
                }

            // Das Zahlenformat der y-Achse (YRasterOhneKreuz) - der Zeigetext traegt
            // dieselben Nachkommastellen wie die Beschriftung daneben.
            string format = max >= 10 ? "N0" : "N1";

            float slot = rc.Width / 12f;
            float balken = slot * 0.6f;
            for (int m = 0; m < 12; m++)
            {
                float x0 = rc.Left + m * slot + (slot - balken) / 2f;
                float unten = rc.Bottom;
                string monat = monate[m];
                int im = m;
                foreach (Reihe r in gueltig)
                {
                    float hoehe = (float)(Math.Max(r.Werte[m], 0) / max * rc.Height);
                    if (hoehe <= 0) continue;
                    float oben = unten - hoehe;   // fest fuer die Klammer
                    Reihe rr = r;
                    // Die Reihenfarbe kommt von AUSSEN und behaelt die Rueckwaertssuche.
                    z.Markiert("reihe:" + (r.Name ?? ""),
                               Elementwert(monat + WERT_TRENNER + (r.Name ?? ""),
                                           rr.Werte[im], format, einheit),
                               zs => zs.Rechteck(x0, oben, balken, hoehe, null, Flaeche(rr.Farbe)));
                    unten -= hoehe;
                }
            }

            return z;
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
        /// <param name="reihen">Die Temperaturreihen; <see cref="Reihe.Strichart"/>
        /// (gestrichelt) kennzeichnet die untere Schicht.</param>
        /// <param name="minAuto">
        /// <c>true</c> = die Achse beginnt beim kleinsten vorkommenden Wert (der Regelfall
        /// des Vorlaeufers). <c>false</c> = sie beginnt bei null.
        /// </param>
        /// <param name="fenster">
        /// DATENZOOM (Anwenderentscheid 09.09.2026, W11b-B-24): der Zeitausschnitt, den der
        /// Anwender im Bild aufgezogen hat; <c>null</c> = das ganze Jahr und damit Bild fuer
        /// Bild das des Bestands. Zugeschnitten wird ZUERST — Spanne, Mindestspanne und
        /// Linien beziehen sich danach auf den Ausschnitt, genau wie es
        /// <see cref="ErzeugerStapel"/> haelt, und die x-Achse wechselt auf die
        /// WIRKLICHEN Jahresstunden (<see cref="XAchseFenster"/>).
        ///
        /// <para>Der SENKRECHTE Anteil (<see cref="Achsenfenster.YAnteil"/>) bleibt hier
        /// ohne Wirkung: Diese Achse hat KEINEN Nullpunkt, den man stehen lassen koennte
        /// (das ist ihr Wesenszug, siehe <paramref name="minAuto"/>), und sie spannt sich
        /// ohnehin ueber Min und Max des ANGEZEIGTEN Ausschnitts — der Zuschnitt spreizt
        /// die Temperaturen also von selbst.</para>
        /// </param>
        public static byte[] Temperaturverlauf(string titel, IReadOnlyList<Reihe> reihen, bool minAuto,
                                               Achsenfenster fenster = null)
            => SkiaMaler.Png(TemperaturverlaufModell(titel, reihen, minAuto, fenster));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a). x zählt
        /// Stützstellen (im Fenster dessen Grenzen), y die vorzeichenfähige
        /// Temperaturspanne des ANGEZEIGTEN Ausschnitts — diese Achse hat keinen
        /// Nullpunkt, das ist ihr Wesenszug.
        /// </summary>
        public static Zeichenmodell TemperaturverlaufModell(string titel, IReadOnlyList<Reihe> reihen,
                                                            bool minAuto,
                                                            Achsenfenster fenster = null)
            => VerlaufsbildModell(titel, reihen, minAuto, TEMPERATUR_MINDESTSPANNE, fenster);

        /// <summary>
        /// <b>RAUMTEMPERATUR EINES GEBÄUDES</b> (Gebäudesimulation VDI 6007, Stufe G2;
        /// Konzept 8.2 und 9, Umsetzungskonzept 2.7): der Jahresverlauf der Raumluft- und der
        /// operativen Temperatur mit dem <b>Sollwertband</b> — unten der Heizsollwert nach dem
        /// Fahrplan, oben die obere Raumtemperatur, beide gestrichelt. Gezeichnet wie der
        /// <see cref="Temperaturverlauf"/>: vorzeichenfähige Achse über Min und Max des
        /// angezeigten Ausschnitts, Mindestspanne 5 K, Datenzoom über
        /// <paramref name="fenster"/>.
        ///
        /// <para>Eine fehlende Reihe (<c>null</c>) entfällt still; ohne jede Reihe steht der
        /// Leerhinweis. Die obere Raumtemperatur ist ein Festwert je Gebäude und wird als
        /// konstante Reihe von der Länge der Luftreihe gezeichnet.</para>
        /// </summary>
        public static byte[] Raumtemperatur(string titel, double[] raumluft, double[] operativ,
                                            double[] heizsollwert, double? obereGrenze,
                                            Raumtemperaturnamen namen, Achsenfenster fenster = null)
            => SkiaMaler.Png(RaumtemperaturModell(titel, raumluft, operativ, heizsollwert, obereGrenze,
                                                  namen, fenster));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche (<c>DiagrammSvg</c>).</summary>
        public static Zeichenmodell RaumtemperaturModell(string titel, double[] raumluft, double[] operativ,
                                                         double[] heizsollwert, double? obereGrenze,
                                                         Raumtemperaturnamen namen, Achsenfenster fenster = null)
        {
            namen ??= new Raumtemperaturnamen();
            var reihen = new List<Reihe>();
            if (raumluft != null)
                reihen.Add(new Reihe(namen.Raumluft, raumluft, Farbrolle.SERIE_1));
            if (operativ != null)
                reihen.Add(new Reihe(namen.Operativ, operativ, Farbrolle.SERIE_2));
            if (heizsollwert != null)
                reihen.Add(new Reihe(namen.Heizsollwert, heizsollwert, Farbrolle.SERIE_3,
                                     Stapelart.Keine, Strichart.Gestrichelt));
            int laenge = raumluft?.Length ?? operativ?.Length ?? heizsollwert?.Length ?? 0;
            if (obereGrenze is double oben && !double.IsNaN(oben) && !double.IsInfinity(oben) && laenge > 0)
            {
                var konstant = new double[laenge];
                for (int i = 0; i < laenge; i++) konstant[i] = oben;
                reihen.Add(new Reihe(namen.ObereGrenze, konstant, Farbrolle.SERIE_4,
                                     Stapelart.Keine, Strichart.Gestrichelt));
            }
            return VerlaufsbildModell(titel, reihen, true, TEMPERATUR_MINDESTSPANNE, fenster,
                                      yTitel: namen.Achse);
        }

        /// <summary>
        /// <b>VORLAUF UND RÜCKLAUF EINES GEBÄUDES</b> (Anlagenkopplung AK1 Welle 3; Konzept 9.4, 12.1):
        /// der Jahresverlauf des gefahrenen Vorlaufs und des Rücklaufs zur gelieferten Leistung, dazu
        /// gestrichelt der Auslegungspunkt — Auslegungsvorlauf und -rücklauf als waagerechte Linien; das
        /// Band dazwischen ist die Spreizung, für die die Übergabe ausgelegt ist. Gezeichnet wie die
        /// <see cref="Raumtemperatur"/>: vorzeichenfähige Achse über dem angezeigten Ausschnitt,
        /// Mindestspanne 5 K, Datenzoom über <paramref name="fenster"/>.
        ///
        /// <para><b>Stunden ohne Heizbetrieb sind LÜCKEN</b> (<see cref="Reihe.Luecken"/>): Jenseits
        /// der Heizgrenze gibt es keinen Vorlauf, und die Linie bricht dort ab, statt eine Zahl zu
        /// erfinden. Eine fehlende Reihe (<c>null</c>) entfällt still, ein fehlender Auslegungswert
        /// ebenso; ohne jede Reihe steht der Leerhinweis.</para>
        /// </summary>
        public static byte[] VorlaufRuecklauf(string titel, double[] vorlauf, double[] ruecklauf,
                                              double? auslegungVorlauf, double? auslegungRuecklauf,
                                              VorlaufRuecklaufnamen namen, Achsenfenster fenster = null)
            => SkiaMaler.Png(VorlaufRuecklaufModell(titel, vorlauf, ruecklauf, auslegungVorlauf,
                                                    auslegungRuecklauf, namen, fenster));

        /// <summary>Dasselbe Bild als Zeichenmodell — der Weg der Oberfläche (<c>DiagrammSvg</c>).</summary>
        public static Zeichenmodell VorlaufRuecklaufModell(string titel, double[] vorlauf, double[] ruecklauf,
                                                           double? auslegungVorlauf, double? auslegungRuecklauf,
                                                           VorlaufRuecklaufnamen namen, Achsenfenster fenster = null)
        {
            namen ??= new VorlaufRuecklaufnamen();
            var reihen = new List<Reihe>();
            if (vorlauf != null)
                reihen.Add(new Reihe(namen.Vorlauf, vorlauf, Farbrolle.SERIE_1) { Luecken = true });
            if (ruecklauf != null)
                reihen.Add(new Reihe(namen.Ruecklauf, ruecklauf, Farbrolle.SERIE_2) { Luecken = true });
            int laenge = vorlauf?.Length ?? ruecklauf?.Length ?? 0;
            if (auslegungVorlauf is double av && Endlich(av) && laenge > 0)
                reihen.Add(new Reihe(namen.AuslegungVorlauf, Konstant(laenge, av), Farbrolle.SERIE_3,
                                     Stapelart.Keine, Strichart.Gestrichelt));
            if (auslegungRuecklauf is double ar && Endlich(ar) && laenge > 0)
                reihen.Add(new Reihe(namen.AuslegungRuecklauf, Konstant(laenge, ar), Farbrolle.SERIE_4,
                                     Stapelart.Keine, Strichart.Gestrichelt));
            return VerlaufsbildModell(titel, reihen, true, TEMPERATUR_MINDESTSPANNE, fenster,
                                      yTitel: namen.Achse);
        }

        private static double[] Konstant(int laenge, double wert)
        {
            var werte = new double[laenge];
            for (int i = 0; i < laenge; i++) werte[i] = wert;
            return werte;
        }

        /// <summary>Die Legendennamen und der Achsentitel des Bildes „Vorlauf und Rücklauf" — die Texte reicht der Aufrufer.</summary>
        public sealed class VorlaufRuecklaufnamen
        {
            /// <summary>Legende des gefahrenen Vorlaufs.</summary>
            public string Vorlauf { get; init; } = "Vorlauf";
            /// <summary>Legende des Rücklaufs.</summary>
            public string Ruecklauf { get; init; } = "Rücklauf";
            /// <summary>Legende des Auslegungsvorlaufs.</summary>
            public string AuslegungVorlauf { get; init; } = "Auslegung Vorlauf";
            /// <summary>Legende des Auslegungsrücklaufs.</summary>
            public string AuslegungRuecklauf { get; init; } = "Auslegung Rücklauf";
            /// <summary>Titel der y-Achse; <c>null</c> = keiner.</summary>
            public string Achse { get; init; } = "°C";
        }

        /// <summary>Die Legendennamen und der Achsentitel des Bildes „Raumtemperatur" — die Texte reicht der Aufrufer.</summary>
        public sealed class Raumtemperaturnamen
        {
            /// <summary>Legende der Raumlufttemperatur.</summary>
            public string Raumluft { get; init; } = "Raumluft";
            /// <summary>Legende der operativen Temperatur.</summary>
            public string Operativ { get; init; } = "operative Temperatur";
            /// <summary>Legende des Heizsollwerts.</summary>
            public string Heizsollwert { get; init; } = "Heizsollwert";
            /// <summary>Legende der oberen Raumtemperatur.</summary>
            public string ObereGrenze { get; init; } = "obere Raumtemperatur";
            /// <summary>Titel der y-Achse; <c>null</c> = keiner.</summary>
            public string Achse { get; init; } = "°C";
        }

        /// <summary>
        /// <b>B10 — LASTGANG UND SPEICHERBETRIEB</b> (Befund W11b‑B‑25, Windows-Abnahme
        /// 09.09.2026: „Lastgang und Speicherung in einer Grafik").
        ///
        /// <para><b>Die LEISTUNGEN teilen sich EINE Achse, und zwar in kW.</b> Netzbezug
        /// ohne Speicher, Netzbezug mit Speicher, die erreichte Kappungsschwelle und die
        /// Speicherleistung sind VIER LEISTUNGEN. Eine zweite Achse für sie behauptete
        /// eine zweite Einheit, wo keine ist, und machte die entscheidende Aussage des
        /// Bildes unlesbar: um wie viel die Speicherleistung die Bezugsspitze senkt.
        /// Die Speicherleistung trägt ihr Vorzeichen — Entladen positiv, Laden negativ —
        /// und liegt damit von selbst um die Nulllinie.</para>
        ///
        /// <para><b>DER LADEZUSTAND BEKOMMT DIE RECHTE ACHSE</b> (Anwenderwunsch W11b‑B‑26,
        /// 10.09.2026: „der Lastgang und die Kappung durch den Stromspeicher sowie der
        /// Ladezustand sollen in einer Grafik sichtbar sein"). Er ist eine ENERGIE [kWh]
        /// und damit der eine Fall, für den der Absatz darüber NICHT gilt: Auf der
        /// kW-Skala lägen bei 400 kWh Speicherinhalt und 40 kW Bezug die Leistungen platt
        /// auf der Nulllinie. Die rechte Achse fängt unten bei null an und trägt
        /// Beschriftung und Zahlen in der FARBE ihrer Reihe — ihre Null muss deshalb nicht
        /// auf der Null der linken liegen, die vorzeichenfähig ist. Genau so hält es
        /// <see cref="ErzeugerStapel"/> mit seiner zweiten Achse (B3).</para>
        ///
        /// <para>Gezeichnet wird sonst wie beim <see cref="Temperaturverlauf"/>:
        /// vorzeichenfähige linke Achse von Min bis Max des ANGEZEIGTEN Ausschnitts,
        /// Legende oben, Datenzoom über <paramref name="fenster"/> (W11b‑B‑24). Ohne
        /// Mindestspanne — die gibt es nur für Temperaturen.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Lastgang und Speicherbetrieb — ganzes Jahr".</param>
        /// <param name="reihen">Die Reihen der LINKEN Achse in Zeichenreihenfolge; leere entfallen still.</param>
        /// <param name="yTitel">Beschriftung der linken y-Achse; <c>null</c> = keine.</param>
        /// <param name="ladezustand">Die Reihe der RECHTEN Achse; <c>null</c> = keine zweite Achse.</param>
        /// <param name="y2Titel">Beschriftung der rechten y-Achse — sie nennt deren EINHEIT.</param>
        /// <param name="sortiert">
        /// Dauerlinie statt Ganglinie: jede Reihe FÜR SICH absteigend sortiert, die Reihe
        /// der rechten Achse eingeschlossen — dieselbe Regel wie im
        /// <see cref="ErzeugerStapel"/>. Zugeschnitten wird ZUERST, die Dauerlinie entsteht
        /// also aus dem gezeigten Ausschnitt (Doku_Simulationsergebnis_Darstellung.md, 5.1).
        /// </param>
        /// <param name="fenster">Der Zeitausschnitt; <c>null</c> = die volle Reihe.</param>
        public static byte[] Speicherbetrieb(string titel, IReadOnlyList<Reihe> reihen,
                                             string yTitel = null, Reihe ladezustand = null,
                                             string y2Titel = null, bool sortiert = false,
                                             Achsenfenster fenster = null)
            => SkiaMaler.Png(SpeicherbetriebModell(titel, reihen, yTitel, ladezustand, y2Titel,
                                                   sortiert, fenster));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe a). Die LEISTUNGEN
        /// teilen sich die vorzeichenfähige linke Achse, der LADEZUSTAND bekommt die
        /// rechte mit eigenem Fenster von null bis zum geglätteten Höchstwert
        /// (DG-E3-1); ihre Achsenlinie, ihre Zahlen und ihr Titel tragen die Marke
        /// <c>yachse2</c>. In der Dauerlinie zählt x den Rang, sonst die Stützstelle.
        /// </summary>
        public static Zeichenmodell SpeicherbetriebModell(string titel, IReadOnlyList<Reihe> reihen,
                                                          string yTitel = null, Reihe ladezustand = null,
                                                          string y2Titel = null, bool sortiert = false,
                                                          Achsenfenster fenster = null)
            => VerlaufsbildModell(titel, reihen, true, 0.0, fenster, yTitel, ladezustand,
                                  y2Titel, sortiert);

        /// <summary>
        /// Die gemeinsame Zeichnung von <see cref="Temperaturverlauf"/> und
        /// <see cref="Speicherbetrieb"/> — beide zeigen mehrere gleich skalierte
        /// Reihen über der Zeit, nur die Mindestspanne der Achse unterscheidet sie.
        ///
        /// <para>Die RECHTE Achse, die Achsenbeschriftung und die Dauerlinie kommen mit
        /// W11b‑B‑26 dazu und werden bislang nur vom Speicherbetrieb genutzt; der
        /// Temperaturverlauf lässt alle drei weg und zeichnet Bild für Bild das, was er
        /// vorher zeichnete.</para>
        /// </summary>
        private static Zeichenmodell VerlaufsbildModell(string titel, IReadOnlyList<Reihe> reihen,
                                                        bool minAuto,
                                                        double mindestspanne, Achsenfenster fenster,
                                                        string yTitel = null, Reihe zweiteAchse = null,
                                                        string y2Titel = null, bool sortiert = false)
        {
            int W = 1240, H = 560;

            // W11b-B-28: Anschlag und Hoehe der Legende stehen als Namen da - das
            // Rechteck unten rechnet mit denselben Werten.
            const float LEGENDE_X = 100f, LEGENDE_Y = 66f;

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            // Der Zuschnitt steht GANZ oben: Alles darunter rechnet mit dem Ausschnitt,
            // ohne davon zu wissen. gesamt merkt sich die volle Laenge - die
            // Achsenbeschriftung nennt Jahresstunden, nicht Fensterstunden.
            List<Reihe> ganz = Brauchbare(reihen);
            int gesamt = ganz.Count > 0 ? ganz[0].Werte.Length
                       : Brauchbar(zweiteAchse) ? zweiteAchse.Werte.Length : 0;
            List<Reihe> gueltig = fenster == null ? ganz : Brauchbare(Zugeschnitten(ganz, fenster));
            if (fenster != null) zweiteAchse = Zugeschnitten(zweiteAchse, fenster);

            // W11b-B-26: Die zweite Achse braucht Platz fuer ihre Zahlen - dieselben
            // 50 Bildpunkte, die der ErzeugerStapel ihr laesst (B3).
            bool mitY2 = Brauchbar(zweiteAchse);
            var rc = SKRect.Create(100f, 110f, W - (mitY2 ? 190f : 140f), 360f);

            if (gueltig.Count == 0 && !mitY2)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            var leg = gueltig.Select(r => new Segment(r.Name, 0, r.Farbe)).ToList();
            if (mitY2) leg.Add(new Segment(zweiteAchse.Name, 0, zweiteAchse.Farbe));

            // W11b-B-28: DIE LEGENDE MACHT SICH SELBST PLATZ. Bei vier Eintraegen
            // (Bezug ohne, Bezug mit, Speicherleistung, Ladezustand) bricht sie in
            // eine ZWEITE Zeile um - und die lag bis dahin auf dem Achsentitel, der
            // 24 px ueber der Zeichenflaeche steht. Jede Zeile ueber der ersten
            // schiebt das Rechteck um genau ihre Hoehe nach unten; die Flaeche wird
            // dabei niedriger und nicht der Platz unter der x-Achse aufgezehrt, wo
            // deren Beschriftung steht.
            float legendenhoehe = Legende(z, leg, LEGENDE_X, LEGENDE_Y, W - 30f);
            float schub = Math.Max(0f, legendenhoehe - LEGENDE_ZEILE);
            if (schub > 0f) rc = SKRect.Create(rc.Left, rc.Top + schub, rc.Width, rc.Height - schub);

            // Die LINKE Achse spannt sich ueber die Reihen der linken Achse; die Reihe
            // rechts hat ihre eigene Skala und darf sie nicht mitziehen. Ohne eine
            // einzige linke Reihe (der Anwender hat alle Leistungen abgewaehlt und nur
            // den Ladezustand stehen lassen) bleibt die linke Achse ganz weg - eine
            // Skala 0..1 ohne Reihe waere eine Behauptung ueber nichts.
            bool mitY1 = gueltig.Count > 0;
            double min = 0.0, max = 1.0;
            if (mitY1)
            {
                min = minAuto ? gueltig.Min(r => Kleinster(r)) : 0;
                max = gueltig.Max(r => Groesster(r));

                // MINDESTSPANNE (Temperatur: 5 K, woertlich aus
                // SpeichertemperaturAnzeigen :2607-2620; Leistungsbilder: keine).
                if (mindestspanne > 0.0 && max - min < mindestspanne)
                {
                    double mitte = (max + min) / 2.0;
                    min = mitte - mindestspanne / 2.0;
                    max = mitte + mindestspanne / 2.0;
                }

                // Eine Reihe aus lauter gleichen Werten (eine waagerechte Schwelle als
                // einzige gewaehlte Reihe) haette sonst eine Spanne von 0 und teilte
                // spaeter durch null.
                if (max - min <= 0.0) { min -= 0.5; max += 0.5; }
                min = Math.Floor(min);
                max = Math.Ceiling(max);

                // Raster und y-Beschriftung ueber die vorzeichenfaehige Spanne.
                var raster = Stift(Farbrolle.RASTER, 1f);
                double minJ = min, maxJ = max;   // fest fuer die Klammer der Marke
                z.Markiert("yachse", zy =>
                {
                    using (var f = Schrift(15f))
                        for (int i = 0; i <= 5; i++)
                        {
                            double wert = minJ + (maxJ - minJ) * i / 5.0;
                            float y = (float)(rc.Bottom - (wert - minJ) / (maxJ - minJ) * rc.Height);
                            zy.Linie(rc.Left, y, rc.Right, y, raster);
                            string lab = wert.ToString("N0", DE);
                            Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                                 y - TextHoehe(f) / 2f);
                        }
                });
            }

            Achsenkreuz(z, rc);
            if (!string.IsNullOrEmpty(yTitel))
                using (var f = Schrift(15f))
                    z.Markiert("yachse", zy =>
                        Text(zy, yTitel, f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));

            int n = mitY1 ? gueltig[0].Werte.Length : zweiteAchse.Werte.Length;
            if (fenster == null) z.Markiert("xachse", zx => XAchse(zx, rc, Achse.Jahresstunden, n));
            else z.Markiert("xachse", zx => XAchseFenster(zx, rc, fenster, gesamt));

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER DER LINKEN ACHSE (Etappe E3).
            double xVon = fenster == null ? 0.0 : Math.Max(0, Math.Min(gesamt, fenster.Von));
            var fensterLinks = new Datenfenster(xVon, xVon + n - 1, min, max);
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(), fensterLinks,
                                           Zeitachsenart(sortiert));

            foreach (Reihe r in gueltig)
            {
                double[] werte = sortiert ? AbsteigendKopie(r.Werte) : r.Werte;
                float staerke = r.Breite > 0 ? r.Breite : 2f;
                Strichmuster muster = Strichfolge(r.Strichart);
                z.Markiert("reihe:" + (r.Name ?? ""), zr =>
                    VerlaufLinie(zr, rc, werte, min, max, r.Farbe, staerke, r.Strichart, r.Luecken && !sortiert));
                z.FuegeReihe(new Datenreihe(r.Name ?? "", Ton(r), staerke, muster, werte,
                                            Reihenfenster(fensterLinks, werte.Length)));
            }

            // W11b-B-26: die zweite Achse mit EIGENER Skala, von null bis zum
            // geglaetteten Hoechstwert - wie im ErzeugerStapel (B3).
            if (mitY2)
            {
                double[] w2 = sortiert ? AbsteigendKopie(zweiteAchse.Werte) : zweiteAchse.Werte;
                double max2 = Nice(w2.Max());
                if (max2 <= 0) max2 = 1;

                float staerke2 = zweiteAchse.Breite > 0 ? zweiteAchse.Breite : 2f;
                Strichmuster muster2 = Strichfolge(zweiteAchse.Strichart);
                z.Markiert("reihe:" + (zweiteAchse.Name ?? ""), zr =>
                    VerlaufLinie(zr, rc, w2, 0, max2, zweiteAchse.Farbe, staerke2,
                                 zweiteAchse.Strichart));
                // DG-E3-12: Die Reihe SAGT, dass sie rechts steht.
                z.FuegeReihe(new Datenreihe(zweiteAchse.Name ?? "", Ton(zweiteAchse),
                                            staerke2, muster2, w2,
                                            new Datenfenster(xVon, xVon + w2.Length - 1, 0, max2),
                                            Achsenseite: Achsenseite.Rechts));

                z.Markiert("yachse2", zy2 =>
                {
                    zy2.Linie(rc.Right, rc.Top, rc.Right, rc.Bottom, Stift(zweiteAchse.Farbe, 2f));
                    using (var f = Schrift(15f))
                    {
                        for (int i = 0; i <= 4; i++)
                        {
                            double wert = max2 * i / 4.0;
                            float y = (float)(rc.Bottom - wert / max2 * rc.Height);
                            Text(zy2, wert.ToString("N0", DE), f, zweiteAchse.Farbe,
                                 rc.Right + 8f, y - TextHoehe(f) / 2f);
                        }

                        // W11b-B-28: RECHTSBUENDIG statt "rc.Right - 40f". Der feste
                        // Einzug war auf kurze Titel gerechnet; "Ladezustand [kWh]"
                        // lief ueber den rechten Bildrand hinaus und wurde
                        // abgeschnitten ("Ladezustand [kW"). Gemessen steht er im
                        // Bild - und weil er rechts endet, kommt er dem linken
                        // Achsentitel bei rc.Left nicht in die Quere.
                        string t2 = y2Titel ?? "";
                        Text(zy2, t2, f, zweiteAchse.Farbe,
                             W - 20f - f.MeasureText(t2), rc.Top - 24f);
                    }
                });
            }

            return z;
        }

        /// <summary>
        /// EINE Linie des <see cref="Verlaufsbild"/>: vorzeichenfaehige Skala von
        /// <paramref name="min"/> bis <paramref name="max"/>, untertastet auf die
        /// Bildbreite und an den Feldraendern geklemmt, wahlweise gestrichelt.
        /// </summary>
        /// <remarks>
        /// Sie steht neben <see cref="ZeichneLinie"/> und nicht darin: Diese hier kennt
        /// den Strichel (die untere Speicherschicht, die erreichte Kappungsschwelle) und
        /// klemmt Werte ausserhalb der Spanne an den Rand, statt sie aus dem Bild laufen
        /// zu lassen - beides braucht der Verlauf, und beides braucht der Stapel nicht.
        /// </remarks>
        private static void VerlaufLinie(IZeichenziel z, SKRect rc, double[] werte,
                                         double min, double max, SKColor farbe,
                                         float staerke, Strichart strichart, bool luecken = false)
        {
            if (werte == null || werte.Length < 2 || max - min <= 0.0) return;

            int schrittweite = Math.Max(1, werte.Length / (int)rc.Width);
            if (luecken)
            {
                VerlaufLinieMitLuecken(z, rc, werte, min, max, farbe, staerke, strichart, schrittweite);
                return;
            }
            var punkte = new List<SKPoint>();
            for (int i = 0; i < werte.Length; i += schrittweite)
            {
                float x = rc.Left + (float)i / (werte.Length - 1) * rc.Width;
                float y = (float)(rc.Bottom - (werte[i] - min) / (max - min) * rc.Height);
                punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
            }

            Linienzug(z, punkte.ToArray(),
                      Stift(farbe, staerke, Strichfolge(strichart), Strichverbindung.Rund));
        }

        /// <summary>
        /// Die Linie einer Reihe MIT LÜCKEN (<see cref="Reihe.Luecken"/>): je zusammenhängendem Stück
        /// endlicher Werte ein eigener Linienzug, untertastet wie die Linie ohne Lücken. Ein Stück aus
        /// einem einzigen Punkt zeichnet nichts — eine Linie braucht zwei.
        /// </summary>
        private static void VerlaufLinieMitLuecken(IZeichenziel z, SKRect rc, double[] werte,
                                                   double min, double max, SKColor farbe,
                                                   float staerke, Strichart strichart, int schrittweite)
        {
            var stift = Stift(farbe, staerke, Strichfolge(strichart), Strichverbindung.Rund);
            var punkte = new List<SKPoint>();
            for (int i = 0; i < werte.Length; i += schrittweite)
            {
                if (!Endlich(werte[i]))
                {
                    if (punkte.Count >= 2) Linienzug(z, punkte.ToArray(), stift);
                    punkte.Clear();
                    continue;
                }
                float x = rc.Left + (float)i / (werte.Length - 1) * rc.Width;
                float y = (float)(rc.Bottom - (werte[i] - min) / (max - min) * rc.Height);
                punkte.Add(new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y))));
            }
            if (punkte.Count >= 2) Linienzug(z, punkte.ToArray(), stift);
        }

        /// <summary>Der kleinste Wert einer Reihe — bei einer Reihe mit Lücken der kleinste endliche.</summary>
        private static double Kleinster(Reihe r) => r.Luecken ? r.Werte.Where(Endlich).Min() : r.Werte.Min();

        /// <summary>Der größte Wert einer Reihe — bei einer Reihe mit Lücken der größte endliche.</summary>
        private static double Groesster(Reihe r) => r.Luecken ? r.Werte.Where(Endlich).Max() : r.Werte.Max();

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        /// <summary>Mindestspanne der Temperaturachse [K] — woertlich aus dem Vorlaeufer.</summary>
        private const double TEMPERATUR_MINDESTSPANNE = 5.0;

        // ============================================ Auslegungsoptimierung (W11b-B-5)

        /// <summary>
        /// Der schlechteste Rasterwert der Auslegungsoptimierung — Firebrick, wörtlich
        /// die Farbe der abgelösten ScottPlot-Skala.
        /// </summary>
        public static readonly SKColor C_RASTER_SCHLECHT = new SKColor(0xB2, 0x22, 0x22);

        /// <summary>Der mittlere Rasterwert — Gold.</summary>
        public static readonly SKColor C_RASTER_MITTE = new SKColor(0xFF, 0xD7, 0x00);

        /// <summary>Der beste Rasterwert — ForestGreen.</summary>
        public static readonly SKColor C_RASTER_GUT = new SKColor(0x22, 0x8B, 0x22);

        /// <summary>
        /// Ein LOCH im Raster — eine Stelle ohne gerechneten Kandidaten (Auftrag #226).
        /// </summary>
        /// <remarks>
        /// Sie gehört NICHT auf die Dreifarbskala: Bis #226 bekam jeder nicht endliche
        /// Wert <see cref="C_RASTER_SCHLECHT"/> und stand damit als „schlechtester
        /// Kandidat" im Bild — eine Aussage über eine Variante, die nie gerechnet wurde.
        /// Das helle Grau liegt bewusst neben dem Weiß der Netzlinien: Ein Loch soll
        /// als Fläche erkennbar bleiben und nicht als Lücke im Netz.
        /// </remarks>
        public static readonly SKColor C_RASTER_LOCH = new SKColor(0xF2, 0xF2, 0xF2);

        /// <summary>
        /// Die Punkte der ZWEITEN Phase einer Rastersuche (Auftrag #224) — Bernstein.
        /// </summary>
        /// <remarks>
        /// Die Mappe V7 zeichnet Grob- und Feinraster als zwei Punktreihen; das Bild soll
        /// dieselbe Frage beantworten — „wo ist grob gerastert, wo ist nachgeschaerft?".
        /// Der Farbunterschied zu <see cref="C_STAMM"/> ist gross genug, um auch in
        /// Graustufen zu tragen (Helligkeit 0x8A gegen 0x4E), und die Feinpunkte werden
        /// zusaetzlich KLEINER gezeichnet: Farbe allein traegt keine Aussage.
        /// </remarks>
        public static readonly SKColor C_FEINRASTER = new SKColor(0xE0, 0x8A, 0x00);

        /// <summary>Stufen der Farbskala rechts. UNGERADE, damit die Mitte exakt Gold trifft.</summary>
        private const int FARBSKALA_STUFEN = 21;

        /// <summary>
        /// <b>B8 — RASTERKARTE der Auslegungsoptimierung</b> (Windows-Abnahme V2 vom
        /// 07.09.2026, W11b‑B‑5): der Jahresüberschuss ΔJ über Kapazität × C-Rate.
        ///
        /// <para><b>Vorbild und Ablösung.</b> Bis hierher zeichnete das
        /// <c>ScottPlot.WinForms</c> in <c>Form_SpeicherOptimierung</c> — der einzige
        /// Ort des Programms, an dem ScottPlot lief. Der Dialog stürzte dabei ab: Jeder
        /// Lauf hängte über <c>Plot.Add.ColorBar</c> eine weitere Farbskala an den Plot,
        /// und <c>Plot.Clear()</c> räumt Plottables, aber KEINE Panels — nach sieben
        /// Läufen war die Zeichenfläche auf 0 Bildpunkte geschrumpft. Hier gibt es
        /// keinen Zustand: Jeder Aufruf zeichnet ein vollständiges PNG aus den
        /// übergebenen Zahlen.</para>
        ///
        /// <para><b>Zellen statt Interpolation.</b> Gezeichnet wird ein Feld aus
        /// Rechtecken — eine Zelle je Rasterpunkt, in ihrer vollen Farbe. Das ist die
        /// ehrliche Darstellung einer Rastersuche: Zwischen zwei Stützstellen ist nichts
        /// gerechnet worden, und eine weiche Fläche behauptete das Gegenteil (die
        /// abgelöste Maske stellte <c>Smooth = false</c> aus demselben Grund).</para>
        ///
        /// <para><b>Die Kapazität wächst nach OBEN</b> — Zeile 0 der Matrix liegt unten.
        /// In der ScottPlot-Fassung musste die Matrix dafür umgedreht befüllt werden;
        /// hier rechnet die Zeichnung selbst von unten nach oben.</para>
        ///
        /// <para><b>Nicht endliche Werte fallen weg.</b> Ein einziges ±∞ in der Matrix
        /// brachte ScottPlot beim RENDERN zu Fall („min must be a real number") — und
        /// zwar im Anstrich des Steuerelements, also unfangbar. Werte, die keine Zahl
        /// sind, bekommen hier die Farbe des schlechtesten Punktes und gehen nicht in
        /// die Skala ein.</para>
        ///
        /// <para><b>UNZULÄSSIGE Zellen werden SCHRAFFIERT</b> (Auftrag #193, Konzept
        /// „Stromspeicher-Dialoge" 2.5). Ein Kandidat, der eine harte Grenze verletzt,
        /// gewinnt auch mit hohem Kapitalwert nicht (Spezifikation 9.4) — ohne
        /// Kennzeichnung stünde er als grünes Feld im Bild und wäre von einem gültigen
        /// Optimum nicht zu unterscheiden. Die Schraffur liegt ÜBER der Farbe und nimmt
        /// ihr nichts: Der Wert bleibt ablesbar, die Sperre kommt dazu. Sie ist außerdem
        /// eine MUSTER-Aussage und keine Farbaussage — in Graustufen und bei
        /// Farbenblindheit bleibt sie erkennbar (WCAG 1.4.1).</para>
        ///
        /// <para><b>Die FUSSZEILE nennt die Grenze der Aussage</b> (<c>SP‑O‑4</c>): Die
        /// Rastersuche kennt nur die gerechneten Punkte; zwischen zwei Stützstellen ist
        /// nichts geprüft, und ein „Optimum" ist das Beste des endlichen Rasters, nicht
        /// das globale. Der Satz steht IM BILD und nicht nur daneben, weil das Bild
        /// exportiert und in Berichte übernommen wird.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Jahresüberschuss ΔJ [€/a]".</param>
        /// <param name="xTitel">Beschriftung der C-Raten-Achse.</param>
        /// <param name="yTitel">Beschriftung der Kapazitätsachse.</param>
        /// <param name="skalaTitel">Beschriftung der Farbskala rechts.</param>
        /// <param name="cRaten">Die C-Raten der Spalten [1/h], aufsteigend.</param>
        /// <param name="kapazitaetenKwh">Die Kapazitäten der Zeilen [kWh], aufsteigend.</param>
        /// <param name="werte">Zielfunktionswerte <c>[iKapazität][iCRate]</c> [€/a].</param>
        /// <param name="besteZeile">Zeile des Optimums, oder -1 für „keine Marke".</param>
        /// <param name="besteSpalte">Spalte des Optimums, oder -1 für „keine Marke".</param>
        /// <param name="unzulaessig">Je Zelle <c>true</c> = schraffieren; <c>null</c> = keine
        /// Schraffur (der Stand vor #193, damit bestehende Aufrufer byte-gleich bleiben).</param>
        /// <param name="fusszeile">Der Hinweis unter dem Bild; leer oder <c>null</c> = keine
        /// Zeile und ein Bild wie zuvor.</param>
        /// <param name="fusszeileZusatz">Ein ZWEITER Hinweis, der auf einer EIGENEN Zeile
        /// unter <paramref name="fusszeile"/> steht; leer oder <c>null</c> = kein Zusatz und
        /// ein Bild wie zuvor. Mit Zusatz wächst das Bild so weit, dass beide Texte
        /// vollständig darin stehen (siehe <see cref="Fussblock"/>).</param>
        public static byte[] Optimierungsraster(string titel, string xTitel, string yTitel,
                                                string skalaTitel,
                                                IReadOnlyList<double> cRaten,
                                                IReadOnlyList<double> kapazitaetenKwh,
                                                double[][] werte,
                                                int besteZeile, int besteSpalte,
                                                bool[][] unzulaessig = null,
                                                string fusszeile = null,
                                                string fusszeileZusatz = null)
            => SkiaMaler.Png(OptimierungsrasterModell(titel, xTitel, yTitel, skalaTitel,
                                                      cRaten, kapazitaetenKwh, werte,
                                                      besteZeile, besteSpalte, unzulaessig,
                                                      fusszeile, fusszeileZusatz));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe c).
        ///
        /// <para><b>Ein reines Pixelbild</b> (DG-E3-7): keine Zeichenfläche, keine
        /// <c>Datenreihe</c> — die Achsen dieses Bildes tragen Kapazität und C-Rate,
        /// keine Zeit. Jede ZELLE samt ihrer Schraffur ist ein Datenelement: Marke
        /// <c>reihe:&lt;Skalentitel&gt;</c> — die Farbskala rechts IST die Legende
        /// dieses Bildes, und sie trägt die Marke <c>skala</c> — und als Wert die
        /// beiden Stützstellen mit ihrem Zielfunktionswert,
        /// „Kapazität 220 kWh · Entladeleistung 100 kW: 4.000 €". Die Stützstellen
        /// stehen in den Formaten IHRER Achsenbeschriftung, der Zellwert in dem der
        /// Farbskala.</para>
        ///
        /// <para>Ein LOCH (kein gerechneter Kandidat, Auftrag #226) nennt statt der
        /// Zahl seinen Grund, eine gesperrte Zelle nennt ihre Sperre — beides sagt das
        /// Bild schon, das eine durch die Lochfarbe, das andere durch die Schraffur.
        /// Titel, Achsen, Bestmarke und Leerhinweis tragen <c>titel</c>,
        /// <c>xachse</c>/<c>yachse</c>, <c>marke</c> und <c>leerhinweis</c>;
        /// Achsenkreuz und Netzlinien bleiben markenlos.</para>
        /// </summary>
        public static Zeichenmodell OptimierungsrasterModell(string titel, string xTitel,
                                                             string yTitel, string skalaTitel,
                                                             IReadOnlyList<double> cRaten,
                                                             IReadOnlyList<double> kapazitaetenKwh,
                                                             double[][] werte,
                                                             int besteZeile, int besteSpalte,
                                                             bool[][] unzulaessig = null,
                                                             string fusszeile = null,
                                                             string fusszeileZusatz = null)
        {
            int W = 860, H = 560;
            var rc = SKRect.Create(120f, 96f, W - 300f, 380f);
            float fussBreite = W - rc.Left - 14f;
            float fussOben = rc.Bottom + 56f;

            // DIE FUSSZEILE BESTIMMT DIE BILDHOEHE, nicht umgekehrt: Unter der
            // Achsenbeschriftung stehen bis zur Bildkante 560 nur rund 28 Bildpunkte,
            // und eine Zeile der 13-Punkt-Schrift ist je nach Schriftart 17 bis 22
            // Bildpunkte hoch. Ein zweiter Hinweis waere damit ganz oder fast ganz
            // UNTER dem Bildrand gezeichnet worden - sichtbar nur als Anschnitt und auf
            // manchen Schriftarten gar nicht. Deshalb wird der Block zuerst umgebrochen
            // und vermessen; das Bild bekommt danach die Hoehe, die er braucht.
            List<string> fussBlock = null;
            float fussZeilenhoehe = 0f;
            if (!string.IsNullOrWhiteSpace(fusszeile) && !string.IsNullOrWhiteSpace(fusszeileZusatz))
                using (var f = Schrift(13f))
                {
                    fussBlock = Fussblock(fusszeile, fusszeileZusatz, f, fussBreite);
                    fussZeilenhoehe = TextHoehe(f) + FUSS_ZEILENABSTAND;
                    float unterkante = fussOben + (fussBlock.Count - 1) * fussZeilenhoehe
                                     + TextHoehe(f) + FUSS_UNTERRAND;
                    if (unterkante > H) H = (int)Math.Ceiling(unterkante);
                }

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            int zeilen = kapazitaetenKwh?.Count ?? 0;
            int spalten = cRaten?.Count ?? 0;

            if (zeilen < 1 || spalten < 1 || werte == null || werte.Length < zeilen)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < zeilen; i++)
                for (int s = 0; s < spalten && s < werte[i].Length; s++)
                {
                    double v = werte[i][s];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            if (min > max) { min = 0.0; max = 0.0; }

            float breite = rc.Width / spalten;
            float hoehe = rc.Height / zeilen;
            string zellmarke = "reihe:" + (skalaTitel ?? "");

            for (int i = 0; i < zeilen; i++)
                for (int s = 0; s < spalten; s++)
                {
                    double v = s < werte[i].Length ? werte[i][s] : double.NaN;

                    // Zeile 0 unten: die Kapazitaet waechst nach oben.
                    float x = rc.Left + s * breite;
                    float y = rc.Bottom - (i + 1) * hoehe;
                    bool gesperrt = Gesetzt(unzulaessig, i, s);

                    z.Markiert(zellmarke,
                               Zellwert(yTitel, kapazitaetenKwh[i], xTitel, cRaten[s],
                                        v, skalaTitel, gesperrt), zz =>
                    {
                        zz.Rechteck(x, y, breite, hoehe, null,
                                    new Zeichnung.Fuellung(Rasterfarbe(v, min, max)));

                        // DIE SPERRE ALS MUSTER (#193): Schraffur ueber die Farbe, nicht
                        // statt ihrer - der Wert bleibt ablesbar.
                        if (gesperrt)
                            Schraffur(zz, SKRect.Create(x, y, breite, hoehe));
                    });
                }

            // Die Netzlinien tragen die Farbe des Bildgrundes — sie trennen zwei
            // Zellen, statt eine eigene Farbe zu behaupten.
            var netz = Stift(Farbrolle.HINTERGRUND, 1f);
            for (int i = 0; i <= zeilen; i++)
                z.Linie(rc.Left, rc.Bottom - i * hoehe, rc.Right, rc.Bottom - i * hoehe, netz);
            for (int s = 0; s <= spalten; s++)
                z.Linie(rc.Left + s * breite, rc.Top, rc.Left + s * breite, rc.Bottom, netz);

            // Das Optimum: ein offenes schwarzes Quadrat auf der Zellmitte -
            // dieselbe Marke, die die abgeloeste Maske setzte.
            if (besteZeile >= 0 && besteZeile < zeilen && besteSpalte >= 0 && besteSpalte < spalten)
            {
                float mx = rc.Left + (besteSpalte + 0.5f) * breite;
                float my = rc.Bottom - (besteZeile + 0.5f) * hoehe;
                float k = Math.Min(breite, hoehe) * 0.36f;
                double best = besteSpalte < werte[besteZeile].Length
                                  ? werte[besteZeile][besteSpalte] : double.NaN;
                z.Markiert("marke",
                           Zellwert(yTitel, kapazitaetenKwh[besteZeile], xTitel,
                                    cRaten[besteSpalte], best, skalaTitel,
                                    Gesetzt(unzulaessig, besteZeile, besteSpalte)),
                           zm => zm.Rechteck(mx - k, my - k, 2f * k, 2f * k,
                                             Stift(Farbrolle.TEXT, 3f)));
            }

            Achsenkreuz(z, rc);

            // Achsenmarken an den Zellmitten. Bei vielen Stuetzstellen wird
            // ausgeduennt, damit sich die Beschriftungen nicht beruehren.
            using (var f = Schrift(14f))
            {
                int xJede = Math.Max(1, (int)Math.Ceiling(spalten * 46f / rc.Width));
                z.Markiert("xachse", zx =>
                {
                    for (int s = 0; s < spalten; s += xJede)
                    {
                        string lab = cRaten[s].ToString("0.##", DE);
                        float x = rc.Left + (s + 0.5f) * breite;
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f,
                             rc.Bottom + 8f);
                    }
                });

                int yJede = Math.Max(1, (int)Math.Ceiling(zeilen * 22f / rc.Height));
                z.Markiert("yachse", zy =>
                {
                    for (int i = 0; i < zeilen; i += yJede)
                    {
                        string lab = kapazitaetenKwh[i].ToString("0.#", DE);
                        float y = rc.Bottom - (i + 0.5f) * hoehe;
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 8f,
                             y - TextHoehe(f) / 2f);
                    }
                });
            }

            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE,
                         rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            z.Markiert("skala", zs =>
                Farbskala(zs, SKRect.Create(rc.Right + 34f, rc.Top, 26f, rc.Height),
                          min, max, skalaTitel));

            // DIE FUSSZEILE (SP-O-4): Sie steht unter der Achsenbeschriftung und
            // bricht bei Bedarf um; ohne Text aendert sie nichts. MIT ZUSATZ ist der
            // Block oben schon vermessen - er wird Zeile fuer Zeile gesetzt und passt
            // in die dafuer gewachsene Bildhoehe. OHNE Zusatz bleibt es beim Bestand:
            // zwei Zeilen im Bild von 860 x 560.
            if (fussBlock != null)
                using (var f = Schrift(13f))
                    for (int i = 0; i < fussBlock.Count; i++)
                        Text(z, fussBlock[i], f, Farbrolle.ACHSE,
                             rc.Left, fussOben + i * fussZeilenhoehe);
            else if (!string.IsNullOrWhiteSpace(fusszeile))
                using (var f = Schrift(13f))
                    Umbruchtext(z, fusszeile, f, Farbrolle.ACHSE, rc.Left, fussOben,
                                fussBreite);

            return z;
        }

        /// <summary>
        /// Der Wert EINER Rasterzelle (DG-E3-6):
        /// <c>„Kapazität 220 kWh · Entladeleistung 100 kW: 4.000 €"</c>.
        ///
        /// <para>Beide Stützstellen stehen im Format ihrer eigenen Achsenbeschriftung
        /// (<c>0.#</c> senkrecht, <c>0.##</c> waagerecht), der Zellwert in dem der
        /// Farbskala („N0"). Ein LOCH nennt statt der Zahl seinen Grund — dort ist
        /// nichts gerechnet worden (#226) —, eine gesperrte Zelle nennt zusätzlich
        /// ihre Sperre (#193).</para>
        /// </summary>
        private static string Zellwert(string yTitel, double zeilenwert,
                                       string xTitel, double spaltenwert,
                                       double wert, string skalaTitel, bool gesperrt)
        {
            string stelle = Achsenwert(yTitel, zeilenwert, "0.#") + WERT_TRENNER +
                            Achsenwert(xTitel, spaltenwert, "0.##");

            string zahl = double.IsNaN(wert) || double.IsInfinity(wert)
                ? BerichtTexte.T("nicht gerechnet")
                : Elementwert(null, wert, "N0", Achseneinheit(skalaTitel));

            return stelle + ": " + zahl +
                   (gesperrt ? " (" + BerichtTexte.T("unzulässig") + ")" : "");
        }

        /// <summary>
        /// <b>Was die x-Achse eines ZEITREIHENbildes zählt</b> (DG-E3-11): die
        /// Stützstelle der Zeitreihe — oder, in der DAUERLINIE, den RANG.
        ///
        /// <para>Die Dauerlinie sortiert jede Reihe für sich absteigend; x zählt dann
        /// keine Zeit mehr, sondern den Platz in der Rangfolge. Eine Jahresstundenteilung
        /// mit Monatsnamen wäre dort schlicht falsch, und die Zeigerzeile schriebe „h"
        /// hinter eine Zahl, die keine Stunde ist. Das Bild selbst ändert sich dadurch
        /// nicht — der Maler übergeht die Zeichenfläche ganz.</para>
        /// </summary>
        private static Achsenart Zeitachsenart(bool sortiert)
            => sortiert ? Achsenart.Index : Achsenart.Stunden;

        /// <summary>
        /// Der Werttext an der Stelle <paramref name="i"/>; <c>null</c>, wo es keinen
        /// gibt — dann bleibt <c>data-wert</c> weg.
        /// </summary>
        private static string Wertetext(string[] werte, int i)
            => werte != null && i >= 0 && i < werte.Length ? werte[i] : null;

        /// <summary>Die Einheit aus einer Achsenbeschriftung — „Kapitalwert [€]" → „€".</summary>
        private static string Achseneinheit(string achsentitel)
        {
            string text = achsentitel ?? "";
            int auf = text.IndexOf('[');
            int zu = auf < 0 ? -1 : text.IndexOf(']', auf + 1);
            return zu > auf ? text.Substring(auf + 1, zu - auf - 1).Trim() : "";
        }

        /// <summary>Steht in der Schraffurmatrix an dieser Stelle <c>true</c>?</summary>
        private static bool Gesetzt(bool[][] matrix, int zeile, int spalte)
        {
            if (matrix == null || zeile < 0 || zeile >= matrix.Length) return false;
            bool[] reihe = matrix[zeile];
            return reihe != null && spalte >= 0 && spalte < reihe.Length && reihe[spalte];
        }

        /// <summary>
        /// Die DIAGONALSCHRAFFUR einer gesperrten Zelle — 45°, fester Abstand, in der
        /// Farbe des schlechtesten Rasterpunkts.
        /// </summary>
        /// <remarks>
        /// Der Abstand steht in BILDPUNKTEN und nicht in Zellbreiten: Sonst trüge eine
        /// schmale Zelle dieselbe Strichzahl wie eine breite, und das Muster sagte etwas
        /// über die Rastergröße statt über die Sperre. Die Striche stehen in einer auf
        /// die Zelle ZUGESCHNITTENEN Gruppe, damit sie nicht in die Nachbarzelle laufen.
        /// </remarks>
        private static void Schraffur(IZeichenziel z, SKRect zelle)
        {
            const float abstand = 7f;
            var stift = Stift(Farbrolle.RASTER_SCHLECHT, 1.5f);
            z.Gruppe(zelle.Modellrahmen(), inhalt =>
            {
                for (float v = -zelle.Height; v < zelle.Width; v += abstand)
                    inhalt.Linie(zelle.Left + v, zelle.Bottom,
                                 zelle.Left + v + zelle.Height, zelle.Top, stift);
            });
        }

        /// <summary>Abstand zwischen zwei Zeilen einer Fußzeile in Bildpunkten.</summary>
        private const float FUSS_ZEILENABSTAND = 3f;

        /// <summary>Luft zwischen der letzten Fußzeile und der unteren Bildkante.</summary>
        private const float FUSS_UNTERRAND = 10f;

        /// <summary>
        /// Höchstens so viele Zeilen je Fußzeilenteil — die Schranke gegen ein Bild, das
        /// ein übergebener Roman beliebig hoch zöge. Die Texte des Bestandes brauchen je
        /// nach Schriftart zwei bis drei.
        /// </summary>
        private const int FUSS_ZEILEN_JE_TEIL = 4;

        /// <summary>
        /// Zeilen der Fußzeile OHNE Zusatz — der Bestand: zwei Zeilen im Bild von 860 × 560.
        /// </summary>
        private const int FUSS_ZEILEN_BESTAND = 2;

        /// <summary>
        /// Die Zeilen eines zweiteiligen Fußtextes: erst der Grundtext, dann — auf einer
        /// EIGENEN Zeile — der Zusatz, beide an <paramref name="breite"/> umgebrochen.
        /// </summary>
        /// <remarks>
        /// Der Zusatz darf nicht einfach an den Grundtext angehängt werden: Dann hinge er
        /// hinter dessen letztem Wort, fiele mit der letzten Zeile unter den Bildrand und
        /// wäre — je nach Schriftart — gar nicht oder nur angeschnitten zu sehen. Auf einer
        /// eigenen Zeile ist er auf jeder Schriftart der Ersatzliste ganz im Bild.
        /// </remarks>
        private static List<string> Fussblock(string grundtext, string zusatz, Schriftmass f,
                                              float breite)
        {
            var block = Umbruchzeilen(grundtext, f, breite, FUSS_ZEILEN_JE_TEIL);
            block.AddRange(Umbruchzeilen(zusatz, f, breite, FUSS_ZEILEN_JE_TEIL));
            return block;
        }

        /// <summary>
        /// Der Text, an <paramref name="breite"/> in Zeilen gebrochen — höchstens
        /// <paramref name="hoechstens"/> davon; alles darüber hinaus fällt weg.
        /// </summary>
        private static List<string> Umbruchzeilen(string text, Schriftmass f, float breite,
                                                  int hoechstens)
        {
            var zeilen = new List<string>();
            string[] woerter = (text ?? "").Split(' ');
            var zeile = new StringBuilder();
            for (int i = 0; i < woerter.Length; i++)
            {
                string versuch = zeile.Length == 0 ? woerter[i] : zeile + " " + woerter[i];
                if (zeile.Length > 0 && f.MeasureText(versuch) > breite)
                {
                    zeilen.Add(zeile.ToString());
                    if (zeilen.Count >= hoechstens) return zeilen;
                    zeile.Clear();
                    zeile.Append(woerter[i]);
                }
                else
                {
                    zeile.Clear();
                    zeile.Append(versuch);
                }
            }
            if (zeile.Length > 0 && zeilen.Count < hoechstens) zeilen.Add(zeile.ToString());
            return zeilen;
        }

        /// <summary>
        /// Ein Hinweis, der an der Breite <paramref name="breite"/> umbricht — höchstens
        /// zwei Zeilen, damit die Fußzeile nicht ins Bild wächst.
        /// </summary>
        private static void Umbruchtext(IZeichenziel z, string text, Schriftmass f, Farbrolle rolle,
                                        float x, float y, float breite)
        {
            List<string> zeilen = Umbruchzeilen(text, f, breite, FUSS_ZEILEN_BESTAND);
            for (int i = 0; i < zeilen.Count; i++)
                Text(z, zeilen[i], f, rolle, x, y + i * (TextHoehe(f) + FUSS_ZEILENABSTAND));
        }

        /// <summary>
        /// Die Dreifarbskala eines Rasterwerts: Rot für den schlechtesten, Gold für den
        /// mittleren, Grün für den besten. Die Skala ist REIN RELATIV zum gezeigten
        /// Raster — sie sagt nichts darüber, ob der beste Punkt wirtschaftlich ist.
        /// </summary>
        private static Farbton Rasterfarbe(double wert, double min, double max)
        {
            // EIN LOCH IST KEIN SCHLECHTER WERT (Auftrag #226): An dieser Stelle wurde
            // nichts gerechnet. Auf der Minimumfarbe stand dort bis dahin die Aussage
            // „schlechtester Kandidat des Rasters" - im Modus Kapazität × Leistung
            // zerfiel die Karte damit in ein überwiegend rotes Feld.
            if (double.IsNaN(wert) || double.IsInfinity(wert))
                return Farbton.Aus(Farbrolle.RASTER_LOCH);
            double spanne = max - min;
            double t = spanne > 1e-12 ? (wert - min) / spanne : 0.5;
            return Farbstufe(t);
        }

        /// <summary>
        /// Farbton zum Anteil 0…1 — exakt Rot bei 0, Gold bei 0,5 und Grün bei 1.
        ///
        /// <para>Die beiden Enden sind REINE ROLLEN; dazwischen entsteht eine
        /// GERECHNETE Farbe, die ihre Herkunftsrolle mitführt (DG-Q7): die untere
        /// der beiden Rollen, aus denen gemischt wurde. So bleibt am Befehl
        /// ablesbar, woraus der Ton entstand, und eine getauschte Palette verschiebt
        /// die Enden der Skala.</para>
        /// </summary>
        private static Farbton Farbstufe(double t)
        {
            if (t <= 0.0) return Farbton.Aus(Farbrolle.RASTER_SCHLECHT);
            if (t >= 1.0) return Farbton.Aus(Farbrolle.RASTER_GUT);
            return t < 0.5
                ? Farbpalette.Gerechnet(Farbrolle.RASTER_SCHLECHT,
                                        Mischung(Farbrolle.RASTER_SCHLECHT, Farbrolle.RASTER_MITTE,
                                                 t * 2.0))
                : Farbpalette.Gerechnet(Farbrolle.RASTER_MITTE,
                                        Mischung(Farbrolle.RASTER_MITTE, Farbrolle.RASTER_GUT,
                                                 (t - 0.5) * 2.0));
        }

        /// <summary>
        /// Die lineare Mischung zweier ROLLENFARBEN — gegen
        /// <see cref="Farbpalette.Aktuell"/> aufgelöst, damit die Zwischentöne einer
        /// getauschten Palette folgen.
        /// </summary>
        private static Farbe Mischung(Farbrolle a, Farbrolle b, double t)
        {
            Farbe fa = Farbpalette.Aktuell[a];
            Farbe fb = Farbpalette.Aktuell[b];
            return new Farbe(
                (byte)Math.Round(fa.R + (fb.R - fa.R) * t),
                (byte)Math.Round(fa.G + (fb.G - fa.G) * t),
                (byte)Math.Round(fa.B + (fb.B - fa.B) * t));
        }

        /// <summary>Der senkrechte Farbbalken rechts neben der Rasterkarte.</summary>
        private static void Farbskala(IZeichenziel z, SKRect rc, double min, double max, string titel)
        {
            float stufe = rc.Height / FARBSKALA_STUFEN;
            for (int i = 0; i < FARBSKALA_STUFEN; i++)
            {
                double t = (double)i / (FARBSKALA_STUFEN - 1);
                z.Rechteck(rc.Left, rc.Bottom - (i + 1) * stufe, rc.Width, stufe + 1f,
                           null, new Zeichnung.Fuellung(Farbstufe(t)));
            }
            z.Rechteck(rc.Left, rc.Top, rc.Width, rc.Height, Stift(Farbrolle.ACHSE, 1f));

            using (var f = Schrift(14f))
            {
                Text(z, max.ToString("N0", DE), f, Farbrolle.ACHSE, rc.Right + 6f, rc.Top - 2f);
                Text(z, min.ToString("N0", DE), f, Farbrolle.ACHSE, rc.Right + 6f,
                     rc.Bottom - TextHoehe(f) + 2f);
            }
            using (var f = Schrift(15f))
                Text(z, titel ?? "", f, Farbrolle.ACHSE, rc.Left - 10f, rc.Top - 26f);
        }

        /// <summary>
        /// <b>B9 — SCHNITTKURVE der Auslegungsoptimierung</b> (W11b‑B‑5): ΔJ über der
        /// Kapazität bei der besten C-Rate, mit markiertem Optimum.
        ///
        /// <para>Die Kurve läuft ins Negative — ein zu großer Speicher trägt seinen
        /// Kapitaldienst nicht mehr. Die y-Achse beginnt deshalb NICHT bei null, und
        /// eine gestrichelte Nulllinie zeigt, wo der Überschuss kippt. Genau das ist die
        /// Aussage des Bildes; eine bei null abgeschnittene Achse verschwiege sie.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Schnittkurve bei 1,5 C".</param>
        /// <param name="xTitel">Beschriftung der Kapazitätsachse.</param>
        /// <param name="yTitel">Beschriftung der Wertachse.</param>
        /// <param name="kapazitaetenKwh">Die Kapazitäten [kWh], aufsteigend.</param>
        /// <param name="werte">Die Zielfunktionswerte [€/a] dazu.</param>
        /// <param name="optimumKwh">Kapazität des Optimums; wird als Kreis markiert.</param>
        /// <param name="optimumEur">Zielfunktionswert des Optimums.</param>
        /// <param name="feinpunkte">
        /// Je Stützstelle: Sie stammt aus der ZWEITEN Phase der Rastersuche (Auftrag
        /// #224). Ein Feinpunkt wird in <see cref="C_FEINRASTER"/> und kleiner
        /// gezeichnet. <c>null</c> — der Regelfall vor #224 — lässt das Bild
        /// byte-gleich.
        /// </param>
        public static byte[] Schnittkurve(string titel, string xTitel, string yTitel,
                                          IReadOnlyList<double> kapazitaetenKwh,
                                          IReadOnlyList<double> werte,
                                          double optimumKwh, double optimumEur,
                                          IReadOnlyList<bool> feinpunkte = null)
            => SkiaMaler.Png(SchnittkurveModell(titel, xTitel, yTitel, kapazitaetenKwh, werte,
                                                optimumKwh, optimumEur, feinpunkte));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe b) — der Rumpf, den
        /// <see cref="Schnittkurve"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>x ist die GRÖSSENACHSE der Rastersuche</b>
        /// (<see cref="Achsenart.Wert"/>) — die Kapazität, und je Suchmodus auch die
        /// Leistung oder die C-Rate. <b>Die Fläche nennt deshalb keine x-Einheit</b>:
        /// Sie wechselt mit dem Modus, und sie steht im Achsentitel, den der Aufrufer
        /// setzt.</para>
        ///
        /// <para><b>Die Stützstellen liegen UNGLEICHMÄSSIG</b> — die zweite Suchphase
        /// schiebt Feinpunkte zwischen die Grobpunkte. Die Datenreihe bringt ihre
        /// x-Stelle deshalb je Wert mit (<c>XWerte</c>, DG-E3-5); ohne sie säße im SVG
        /// jeder Feinpunkt an der falschen Kapazität.</para>
        ///
        /// <para><b>Die Stützpunkt-Kreise fallen im SVG weg.</b> Sie tragen die Marke
        /// ihrer Reihe, und der Schreiber ersetzt den ERSTEN Befehl mit einer
        /// <c>reihe:…</c>-Marke durch das innere <c>&lt;svg&gt;</c> und übergeht jeden
        /// weiteren (DG-E2-2). Die Kurve selbst steht dort als Pfad; die Punkte kann
        /// die Oberfläche später als zweite Reihe mit
        /// <see cref="Reihenart.Punkte"/> nachziehen.</para>
        /// </summary>
        public static Zeichenmodell SchnittkurveModell(string titel, string xTitel, string yTitel,
                                                       IReadOnlyList<double> kapazitaetenKwh,
                                                       IReadOnlyList<double> werte,
                                                       double optimumKwh, double optimumEur,
                                                       IReadOnlyList<bool> feinpunkte = null)
        {
            int W = 720, H = 460;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            var rc = SKRect.Create(110f, 92f, W - 170f, 280f);

            int n = Math.Min(kapazitaetenKwh?.Count ?? 0, werte?.Count ?? 0);
            var xw = new List<double>();
            var yw = new List<double>();
            var fein = new List<bool>();
            for (int i = 0; i < n; i++)
            {
                double x = kapazitaetenKwh[i], y = werte[i];
                if (double.IsNaN(x) || double.IsInfinity(x)) continue;
                if (double.IsNaN(y) || double.IsInfinity(y)) continue;
                xw.Add(x); yw.Add(y);
                fein.Add(feinpunkte != null && i < feinpunkte.Count && feinpunkte[i]);
            }

            if (xw.Count < 2)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            double xMin = xw.Min(), xMax = xw.Max();
            if (xMax - xMin < 1e-9) xMax = xMin + 1.0;

            double yRoh0 = yw.Min(), yRoh1 = yw.Max();
            double yStufe = RundeStufe(Math.Max(1e-9, (yRoh1 - yRoh0) / 5.0));
            double yMin = Math.Floor(yRoh0 / yStufe) * yStufe;
            double yMax = Math.Ceiling(yRoh1 / yStufe) * yStufe;
            if (yMax - yMin < 1e-9) yMax = yMin + yStufe;

            var raster = Stift(Farbrolle.RASTER, 1f);

            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(14f))
                    for (double wert = yMin; wert <= yMax + yStufe * 1e-6; wert += yStufe)
                    {
                        float y = (float)(rc.Bottom - (wert - yMin) / (yMax - yMin) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = (wert == 0 ? 0.0 : wert).ToString("N0", DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
            });

            // Die Nulllinie, wo die Achse sie enthaelt - dieselbe Strichelung wie
            // im Jahresgang.
            if (yMin < 0.0 && yMax > 0.0)
            {
                float y = (float)(rc.Bottom + yMin / (yMax - yMin) * rc.Height);
                z.Markiert("nulllinie", zn =>
                    zn.Linie(rc.Left, y, rc.Right, y,
                             Stift(Farbrolle.ACHSE, 1.5f, new Strichmuster(8f, 5f))));
            }

            double xStufe = RundeStufe((xMax - xMin) / 6.0);
            z.Markiert("xachse", zx =>
            {
                using (var f = Schrift(14f))
                    for (double wert = Math.Ceiling(xMin / xStufe) * xStufe;
                         wert <= xMax + xStufe * 1e-6; wert += xStufe)
                    {
                        float x = rc.Left + (float)((wert - xMin) / (xMax - xMin)) * rc.Width;
                        zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = (wert == 0 ? 0.0 : wert).ToString("N0", DE);
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
            });

            Achsenkreuz(z, rc);

            var punkte = new SKPoint[xw.Count];
            for (int i = 0; i < xw.Count; i++)
                punkte[i] = new SKPoint(
                    rc.Left + (float)((xw[i] - xMin) / (xMax - xMin)) * rc.Width,
                    (float)(rc.Bottom - (yw[i] - yMin) / (yMax - yMin) * rc.Height));

            // DIE ZEICHENFLAECHE SAMT DATENFENSTER (DG-E3-4/7), VOR dem ersten
            // Reihenbefehl. x ist die Groessenachse der Rastersuche; ihre Einheit
            // wechselt mit dem Suchmodus und steht deshalb nur im Achsentitel.
            z.Flaeche = new Zeichenflaeche(rc.Modellrahmen(),
                                           new Datenfenster(xMin, xMax, yMin, yMax),
                                           Achsenart.Wert);

            string name = titel ?? "";
            z.Markiert("reihe:" + name, zr =>
                Linienzug(zr, punkte, Stift(Farbrolle.STAMM, 2.5f, null, Strichverbindung.Rund)));

            Zeichnung.Fuellung grob = Flaeche(Farbrolle.STAMM);
            Zeichnung.Fuellung feinPunkt = Flaeche(Farbrolle.FEINRASTER);
            z.Markiert("reihe:" + name, zr =>
            {
                for (int i = 0; i < punkte.Length; i++)
                    if (fein[i]) zr.Kreis(punkte[i].X, punkte[i].Y, 2.5f, null, feinPunkt);
                    else zr.Kreis(punkte[i].X, punkte[i].Y, 3.5f, null, grob);
            });

            // Dieselbe Kurve in DATENWERTEN, mit ihrer x-Stelle je Stuetzstelle
            // (DG-E3-5): Grob- und Feinpunkte liegen ungleichmaessig.
            z.FuegeReihe(new Datenreihe(name, Farbton.Aus(Farbrolle.STAMM), 2.5f, null,
                                        yw.ToArray(), z.Flaeche.Daten, Reihenart.Linie,
                                        null, null, xw.ToArray(), yTitel));

            // DIE STUETZPUNKTE ALS EIGENE PUNKTREIHEN (Gruppe (b), offener Punkt).
            //
            // Die Kreise oben tragen die Marke der Reihe und werden deshalb vom
            // inneren svg verschluckt (DG-E2-2) - im SVG stuende sonst die nackte
            // Kurve. Als Reihe der Art Punkte zeichnet der Schreiber sie wieder:
            // Strichbreite = Punktdurchmesser des PNG (Radius 3,5 bzw. 2,5), runde
            // Kappe. NACH der Linie, damit sie wie im Bild darueber liegen, und unter
            // DEMSELBEN Namen - so schaltet ein Legendenklick Kurve und Punkte
            // zusammen.
            Stuetzpunktreihen(z, name, xw, yw, fein, yTitel);

            if (!double.IsNaN(optimumKwh) && !double.IsInfinity(optimumKwh)
                && !double.IsNaN(optimumEur) && !double.IsInfinity(optimumEur))
            {
                float x = rc.Left + (float)((optimumKwh - xMin) / (xMax - xMin)) * rc.Width;
                float y = (float)(rc.Bottom - (optimumEur - yMin) / (yMax - yMin) * rc.Height);
                if (x >= rc.Left - 20f && x <= rc.Right + 20f)
                    z.Markiert("marke", zm =>
                        zm.Kreis(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)), 9f,
                                 Stift(Farbrolle.RASTER_SCHLECHT, 3f)));
            }

            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE,
                         rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            return z;
        }



        // ==================================== Stueckzahlkurve (#247, SD-E-10 / SD-Q17)

        /// <summary>
        /// <b>B12 — KAPITALWERT ÜBER STÜCKZAHL</b> (Auftrag #247, Anwenderentscheid
        /// SD‑E‑10 / SD‑Q17, Konzept „Stromspeicher-Dialoge" 8.3).
        ///
        /// <para><b>Warum Balken und keine Kurve.</b> Die Stückzahl ist eine GANZE Zahl.
        /// Zwischen „zwei Geräten" und „drei Geräten" gibt es nichts — eine Linie
        /// behauptete einen Zwischenwert, den es weder gibt noch je gerechnet wurde. Das
        /// ist derselbe Grund, aus dem die Rasterkarte Zellen zeichnet und nicht
        /// interpoliert, und derselbe, aus dem es unter „Stückzahl suchen" kein Feinraster
        /// gibt.</para>
        ///
        /// <para><b>Die y-Achse ist vorzeichenfähig</b> und hebt ihre Null gestrichelt
        /// hervor — genau wie in der Schnittkurve und der Jahresprojektion: Zu viele
        /// Geräte tragen ihren Kapitaldienst nicht mehr, und das ist die Aussage des
        /// Bildes. Eine bei null abgeschnittene Achse verschwiege sie.</para>
        ///
        /// <para><b>Drei Farben mit drei Aussagen.</b> Eine gewöhnliche Säule steht in
        /// <see cref="C_STAMM"/>, die BESTE in <see cref="C_RASTER_GUT"/> samt der
        /// schwarzen Optimum-Marke der Rasterkarte, eine NEGATIVE in
        /// <see cref="C_RASTER_SCHLECHT"/> (Regel der Jahresprojektion). Unzulässige
        /// Stückzahlen bekommen die Diagonalschraffur der Rasterkarte — eine
        /// MUSTER-Aussage, die auch in Graustufen bleibt (WCAG 1.4.1).</para>
        ///
        /// <para>Bildmaß 720 × 460 wie die Schnittkurve daneben.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Kapitalwert über Stückzahl".</param>
        /// <param name="xTitel">Beschriftung der Stückzahlachse.</param>
        /// <param name="yTitel">Beschriftung der Wertachse.</param>
        /// <param name="stueckzahlen">Die geprüften Stückzahlen, aufsteigend.</param>
        /// <param name="werte">Der Kapitalwert [€] dazu; nicht endliche Werte fallen weg.</param>
        /// <param name="besteStelle">Stelle des Optimums in den Listen, oder -1 für „keine Marke".</param>
        /// <param name="unzulaessig">Je Säule <c>true</c> = schraffieren; <c>null</c> = keine Schraffur.</param>
        public static byte[] Stueckzahlkurve(string titel, string xTitel, string yTitel,
                                             IReadOnlyList<int> stueckzahlen,
                                             IReadOnlyList<double> werte,
                                             int besteStelle,
                                             IReadOnlyList<bool> unzulaessig = null)
            => SkiaMaler.Png(StueckzahlkurveModell(titel, xTitel, yTitel, stueckzahlen, werte,
                                                   besteStelle, unzulaessig));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe b) — der Rumpf, den
        /// <see cref="Stueckzahlkurve"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>Es gibt KEINE Zeichenfläche und KEINE Datenreihe (Entscheid
        /// DG-E3-7).</b> Die x-Achse zählt ganze Geräte — zwischen zwei Säulen liegt
        /// nichts, worauf ein Zoom zeigen könnte, und eine Zeigerzeile sagt an einer
        /// Säule nicht mehr, als die Säule selbst zeigt. Das Bild bleibt ein reines
        /// Pixelbild; die Marken sind der Griff der Oberfläche.</para>
        ///
        /// <para>Die Säulen samt Schraffur tragen <c>reihe:&lt;Titel&gt;</c>, die
        /// Bestwert-Marke <c>marke</c>.</para>
        /// </summary>
        public static Zeichenmodell StueckzahlkurveModell(string titel, string xTitel, string yTitel,
                                                          IReadOnlyList<int> stueckzahlen,
                                                          IReadOnlyList<double> werte,
                                                          int besteStelle,
                                                          IReadOnlyList<bool> unzulaessig = null)
        {
            int W = 720, H = 460;
            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            var rc = SKRect.Create(110f, 92f, W - 170f, 280f);

            int n = Math.Min(stueckzahlen?.Count ?? 0, werte?.Count ?? 0);
            if (n < 1)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            double yRoh0 = 0.0, yRoh1 = 0.0;
            bool etwas = false;
            for (int i = 0; i < n; i++)
            {
                double v = werte[i];
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                if (!etwas) { yRoh0 = v; yRoh1 = v; etwas = true; }
                if (v < yRoh0) yRoh0 = v;
                if (v > yRoh1) yRoh1 = v;
            }
            if (!etwas)
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            // Die Null gehoert IMMER auf die Achse: Eine Saeule waechst von ihr aus,
            // und ohne sie stuende der Fuss der Saeule an einer erfundenen Grundlinie.
            if (yRoh0 > 0.0) yRoh0 = 0.0;
            if (yRoh1 < 0.0) yRoh1 = 0.0;

            double yStufe = RundeStufe(Math.Max(1e-9, (yRoh1 - yRoh0) / 5.0));
            double yMin = Math.Floor(yRoh0 / yStufe) * yStufe;
            double yMax = Math.Ceiling(yRoh1 / yStufe) * yStufe;
            if (yMax - yMin < 1e-9) yMax = yMin + yStufe;

            using (var f = Schrift(14f))
            {
                var raster = Stift(Farbrolle.RASTER, 1f);
                z.Markiert("yachse", zy =>
                {
                    for (double wert = yMin; wert <= yMax + yStufe * 1e-6; wert += yStufe)
                    {
                        float y = (float)(rc.Bottom - (wert - yMin) / (yMax - yMin) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = (wert == 0 ? 0.0 : wert).ToString("N0", DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
                });
            }

            float nullhoehe = (float)(rc.Bottom + yMin / (yMax - yMin) * rc.Height);
            if (yMin < 0.0 && yMax > 0.0)
                z.Markiert("nulllinie", zn =>
                    zn.Linie(rc.Left, nullhoehe, rc.Right, nullhoehe,
                             Stift(Farbrolle.ACHSE, 1.5f, new Strichmuster(8f, 5f))));

            float fach = rc.Width / n;
            float breite = Math.Min(fach * 0.62f, 64f);

            // Der Saeulenrand trennt zwei benachbarte Saeulen; er traegt die Farbe des
            // Bildgrundes, und genau die nennt er auch als Rolle.
            var saeulenrand = Stift(Farbrolle.HINTERGRUND, 1f);
            string saeulenmarke = "reihe:" + (titel ?? "");
            z.Markiert(saeulenmarke, zr =>
            {
                for (int i = 0; i < n; i++)
                {
                    double v = werte[i];
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                    float y = (float)(rc.Bottom - (v - yMin) / (yMax - yMin) * rc.Height);
                    float mitte = rc.Left + (i + 0.5f) * fach;
                    float oben = Math.Min(y, nullhoehe);
                    float hoehe = Math.Max(1f, Math.Abs(y - nullhoehe));
                    var saeule = SKRect.Create(mitte - breite / 2f, oben, breite, hoehe);

                    Farbrolle rolle = v < 0.0 ? Farbrolle.RASTER_SCHLECHT
                                    : i == besteStelle ? Farbrolle.RASTER_GUT : Farbrolle.STAMM;

                    // DG-E3-10: Die Säule nennt ihre Stückzahl und ihren Wert; eine
                    // gesperrte hängt ihre Sperre an (Regel der Gruppe (c), #193).
                    // Fläche, Rand UND Schraffur stehen in DERSELBEN Klammer — sie
                    // sind EIN Element.
                    string text = Achsenwert(xTitel, stueckzahlen[i], "N0") + ": " +
                                  Elementwert(null, v, "N0", Achseneinheit(yTitel)) +
                                  (Gesetzt(unzulaessig, i)
                                      ? " (" + BerichtTexte.T("unzulässig") + ")" : "");

                    zr.Markiert(saeulenmarke, text, zs =>
                    {
                        zs.Rechteck(saeule.Left, saeule.Top, saeule.Width, saeule.Height,
                                    null, Flaeche(rolle));
                        zs.Rechteck(saeule.Left, saeule.Top, saeule.Width, saeule.Height,
                                    saeulenrand);

                        if (Gesetzt(unzulaessig, i)) Schraffur(zs, saeule);
                    });
                }
            });

            // Die Marke des Optimums — dasselbe offene schwarze Quadrat wie in der
            // Rasterkarte, damit beide Bilder dieselbe Zeichensprache sprechen.
            if (besteStelle >= 0 && besteStelle < n && double.IsFinite(werte[besteStelle]))
            {
                float mx = rc.Left + (besteStelle + 0.5f) * fach;
                float my = (float)(rc.Bottom - (werte[besteStelle] - yMin) / (yMax - yMin) * rc.Height);
                float k = Math.Min(breite, 26f) * 0.36f;
                // Die Marke liegt ÜBER der besten Säule; ohne eigenen Wert zeigte der
                // Zeiger dort nichts (DG-E3-10).
                string bestwert = Achsenwert(xTitel, stueckzahlen[besteStelle], "N0") + ": " +
                                  Elementwert(null, werte[besteStelle], "N0",
                                              Achseneinheit(yTitel)) +
                                  " (" + BerichtTexte.T("bester Wert") + ")";
                z.Markiert("marke", bestwert, zm =>
                    zm.Rechteck(mx - k, my - k, 2f * k, 2f * k, Stift(Farbrolle.TEXT, 3f)));
            }

            Achsenkreuz(z, rc);

            using (var f = Schrift(14f))
            {
                int jede = Math.Max(1, (int)Math.Ceiling(n * 46f / rc.Width));
                z.Markiert("xachse", zx =>
                {
                    for (int i = 0; i < n; i += jede)
                    {
                        string lab = stueckzahlen[i].ToString("N0", DE);
                        float x = rc.Left + (i + 0.5f) * fach;
                        Text(zx, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
                });
            }

            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE,
                         rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 34f));
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            return z;
        }

        /// <summary>
        /// Die Stützpunkte der Schnittkurve als <see cref="Reihenart.Punkte"/> — eine
        /// Reihe für die GROBEN, eine für die FEINEN, beide unter dem Namen der Kurve.
        ///
        /// <para>Zwei Reihen und nicht eine, weil das PNG zwei Punktarten zeichnet:
        /// Grobpunkte in <see cref="Farbrolle.STAMM"/> mit Radius 3,5, Feinpunkte der
        /// zweiten Suchphase in <see cref="Farbrolle.FEINRASTER"/> mit Radius 2,5. Eine
        /// Reihe trägt genau EINE Strichbreite und EINE Farbe; zusammengelegt sähe das
        /// SVG anders aus als das Bild. <b>Eine leere Art bekommt keine Reihe</b> — so
        /// bleibt die Zahl der Reihen bei einer Grobsuche bei zwei, und die
        /// <see cref="Pfadregel"/> bündelt die Kurve nicht plötzlich anders.</para>
        /// </summary>
        private static void Stuetzpunktreihen(Zeichenmodell z, string name,
                                              List<double> xw, List<double> yw, List<bool> fein,
                                              string yTitel)
        {
            Stuetzpunktreihe(z, name, xw, yw, fein, false, Farbrolle.STAMM, 7f, yTitel);
            Stuetzpunktreihe(z, name, xw, yw, fein, true, Farbrolle.FEINRASTER, 5f, yTitel);
        }

        /// <summary>Eine der beiden Punktarten; ohne Punkte entsteht keine Reihe.</summary>
        private static void Stuetzpunktreihe(Zeichenmodell z, string name,
                                             List<double> xw, List<double> yw, List<bool> fein,
                                             bool feinpunkte, Farbrolle rolle, float durchmesser,
                                             string yTitel)
        {
            var x = new List<double>();
            var y = new List<double>();
            for (int i = 0; i < xw.Count; i++)
                if (fein[i] == feinpunkte) { x.Add(xw[i]); y.Add(yw[i]); }

            if (x.Count == 0) return;
            z.FuegeReihe(new Datenreihe(name, Farbton.Aus(rolle), durchmesser, null,
                                        y.ToArray(), z.Flaeche.Daten, Reihenart.Punkte,
                                        null, null, x.ToArray(), yTitel));
        }

        /// <summary>Steht in der Sperrliste an dieser Stelle <c>true</c>?</summary>
        private static bool Gesetzt(IReadOnlyList<bool> liste, int stelle)
            => liste != null && stelle >= 0 && stelle < liste.Count && liste[stelle];

        // ============================================ Jahresprojektion (#184, P2)

        /// <summary>Der Grund der Ersatzjahr-Marke — Firebrick mit 40 von 255 Deckung.</summary>
        private static readonly SKColor C_ERSATZJAHR = new SKColor(0xB2, 0x22, 0x22, 40);

        /// <summary>
        /// <b>B11 — JAHRESPROJEKTION einer Speicherflotte</b> (Auftrag #184, Konzept
        /// „Stromspeicher-Dialoge" 2.2 Punkt 4, Anwenderentscheid SD‑Q6 vom 11.09.2026).
        ///
        /// <para><b>Warum ein Bild und nicht nur die Tabelle.</b> Die Jahreskonten standen
        /// bis hierher als zwanzigzeilige Tabelle im Ergebnis. Die zwei Fragen, die der
        /// Anwender an eine Projektion stellt — „trägt sich das jemals?" und „wann kippt
        /// es?" — beantwortet erst das Bild: die SÄULE zeigt den Netto-Cashflow des
        /// einzelnen Jahres, die LINIE den kumulierten Stand. Ihr Schnittpunkt mit der
        /// Nulllinie ist die Amortisation, und ein Ersatzjahr ist der Knick darin.</para>
        ///
        /// <para><b>Alles in EINER Einheit, deshalb EINE Achse</b> (Hausregel
        /// <c>Doku_Simulationsergebnis_Darstellung.md</c> § 5.3): Netto-Cashflow, Betrieb,
        /// Durchsatz, Ersatz und der kumulierte Stand sind allesamt Geldbeträge. Die Achse
        /// ist vorzeichenfähig und hebt ihre Null gestrichelt hervor — ein Jahr darf
        /// negativ sein, und die kumulierte Linie beginnt es immer.</para>
        ///
        /// <para><b>Die Ersatzjahre sind der GRUND, nicht eine Reihe.</b> Sie stehen als
        /// senkrechtes Band hinter der Säule ihres Jahres und als Dreieck am oberen Rand.
        /// Eine eigene Kurve hätten sie nicht verdient: Sie sagen nicht „wie viel", sondern
        /// „hier fällt die Ersatzinvestition an" — und genau diese Stelle sucht das Auge im
        /// Knick der kumulierten Linie.</para>
        ///
        /// <para><b>Eine negative Säule ist ROT</b> (<see cref="C_RASTER_SCHLECHT"/>),
        /// unabhängig von der Farbe der Reihe. Ein Verlustjahr in derselben Farbe wie ein
        /// Gewinnjahr wäre nur an seiner Richtung zu erkennen — bei zwanzig schmalen Säulen
        /// zu wenig.</para>
        ///
        /// <para>Bildmaß 1240 × 560 wie die übrigen Jahresbilder dieser Datei.</para>
        /// </summary>
        /// <param name="titel">Überschrift, z. B. „Jahresprojektion [€]".</param>
        /// <param name="jahre">Die Projektjahre in Reihenfolge (1…n); sie beschriften die x-Achse.</param>
        /// <param name="netto">Die Säulenreihe (Netto-Cashflow je Jahr); <c>null</c> = keine Säulen.</param>
        /// <param name="kumuliert">Die Linie des kumulierten Standes; <c>null</c> = keine Linie.</param>
        /// <param name="ersatzjahre">Die Jahresnummern mit Ersatzinvestition; <c>null</c> = keine Marke.</param>
        /// <param name="weitere">Weitere wählbare Linien (Betrieb, Durchsatz, Ersatz); <c>null</c> = keine.</param>
        /// <param name="yTitel">Beschriftung der y-Achse; <c>null</c> = keine.</param>
        /// <param name="xTitel">Beschriftung der x-Achse; <c>null</c> = keine.</param>
        public static byte[] Jahresprojektion(string titel, IReadOnlyList<int> jahre,
                                              Reihe netto, Reihe kumuliert,
                                              IReadOnlyList<int> ersatzjahre,
                                              IReadOnlyList<Reihe> weitere = null,
                                              string yTitel = null, string xTitel = null)
            => SkiaMaler.Png(JahresprojektionModell(titel, jahre, netto, kumuliert, ersatzjahre,
                                                    weitere, yTitel, xTitel));

        /// <summary>
        /// DASSELBE BILD ALS ZEICHENMODELL (Etappe DG-E3, Gruppe b) — der Rumpf, den
        /// <see cref="Jahresprojektion"/> an <c>SkiaMaler.Png</c> gibt.
        ///
        /// <para><b>Es gibt KEINE Zeichenfläche (Entscheid DG-E3-7).</b> Die x-Achse
        /// zählt zwanzig Projektjahre; zwischen zwei Jahren liegt nichts, und die
        /// Säulen sind an ihre Fächer gebunden. Ein inneres <c>&lt;svg&gt;</c>
        /// ersetzte sie durch Pfade und nähme dem Bild seine Aussage.</para>
        ///
        /// <para><b>Die Datenreihen stehen trotzdem im Modell</b>: der Netto-Cashflow,
        /// der kumulierte Stand und jede weitere Linie, je Jahr ein Wert mit dem
        /// Projektjahr als <c>XWerte</c>. Daraus liest die Zeigerzeile „Jahr 7:
        /// −420 €". Alles sind Geldbeträge — daher überall die Einheit „€" (Hausregel
        /// „eine Einheit, eine Achse").</para>
        /// </summary>
        public static Zeichenmodell JahresprojektionModell(string titel, IReadOnlyList<int> jahre,
                                                           Reihe netto, Reihe kumuliert,
                                                           IReadOnlyList<int> ersatzjahre,
                                                           IReadOnlyList<Reihe> weitere = null,
                                                           string yTitel = null, string xTitel = null)
        {
            int W = 1240, H = 560;
            const float LEGENDE_X = 100f, LEGENDE_Y = 66f;

            var z = Modell(W, H);
            z.Markiert("titel", zt => Titel(zt, titel ?? "", W));

            var rc = SKRect.Create(110f, 116f, W - 170f, 330f);

            int n = jahre == null ? 0 : jahre.Count;
            var linien = new List<Reihe>();
            if (Brauchbar(kumuliert)) linien.Add(kumuliert);
            foreach (Reihe r in weitere ?? new List<Reihe>())
                if (Brauchbar(r)) linien.Add(r);

            bool mitSaeulen = netto != null && netto.Werte != null && netto.Werte.Length > 0 &&
                              netto.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w));
            if (n < 1 || (!mitSaeulen && linien.Count == 0))
            {
                z.Markiert("leerhinweis", zl => Leerhinweis(zl, rc));
                return z;
            }

            // Die LEGENDE macht sich selbst Platz — dieselbe Regel wie im
            // Verlaufsbild (W11b-B-28): Jede Zeile über der ersten schiebt die
            // Zeichenfläche um ihre Höhe nach unten.
            var leg = new List<Segment>();
            if (mitSaeulen) leg.Add(new Segment(netto.Name, 0, netto.Farbe));
            foreach (Reihe r in linien) leg.Add(new Segment(r.Name, 0, r.Farbe));
            float legendenhoehe = Legende(z, leg, LEGENDE_X, LEGENDE_Y, W - 30f);
            float schub = Math.Max(0f, legendenhoehe - LEGENDE_ZEILE);
            if (schub > 0f) rc = SKRect.Create(rc.Left, rc.Top + schub, rc.Width, rc.Height - schub);

            // Die Skala trägt ALLES, was gezeichnet wird — Säulen und Linien teilen
            // sich die Achse, sonst wäre der kumulierte Stand nicht gegen den
            // Jahreswert zu lesen. Die Null ist immer dabei.
            double min = 0.0, max = 0.0;
            if (mitSaeulen)
            {
                min = Math.Min(min, netto.Werte.Min());
                max = Math.Max(max, netto.Werte.Max());
            }
            foreach (Reihe r in linien)
            {
                min = Math.Min(min, r.Werte.Min());
                max = Math.Max(max, r.Werte.Max());
            }
            double stufe = RundeStufe(Math.Max(1e-9, (max - min) / 5.0));
            min = Math.Floor(min / stufe) * stufe;
            max = Math.Ceiling(max / stufe) * stufe;
            if (max - min < 1e-9) max = min + stufe;

            var raster = Stift(Farbrolle.RASTER, 1f);
            z.Markiert("yachse", zy =>
            {
                using (var f = Schrift(15f))
                    for (double wert = min; wert <= max + stufe * 1e-6; wert += stufe)
                    {
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = wert.ToString("N0", DE);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                             y - TextHoehe(f) / 2f);
                    }
            });

            float fach = rc.Width / n;
            float y0 = (float)(rc.Bottom - (0.0 - min) / (max - min) * rc.Height);

            // ERSATZJAHRE zuerst: Das Band steht HINTER Säule und Linie. Band und
            // Dreieck sind eine MARKE, keine Reihe — sie sagen nicht „wie viel",
            // sondern „hier fällt die Ersatzinvestition an".
            if (ersatzjahre != null && ersatzjahre.Count > 0)
            {
                var band = Flaeche(C_ERSATZJAHR);
                var marke = Flaeche(C_RASTER_SCHLECHT);
                z.Markiert("marke", zm =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (!ersatzjahre.Contains(jahre[i])) continue;
                        float mitte = rc.Left + (i + 0.5f) * fach;

                        // DG-E3-10: Band und Dreieck sagen, WELCHES Jahr sie meinen —
                        // sie zeigen keine Zahl, sondern eine Stelle.
                        string wert = Achsenwert(xTitel, jahre[i], "N0") + ": " +
                                      BerichtTexte.T("Ersatzinvestition");
                        zm.Markiert("marke", wert, zb =>
                        {
                            zb.Rechteck(mitte - fach * 0.45f, rc.Top, fach * 0.9f, rc.Height,
                                        null, band);
                            Vieleck(zb, new[]
                            {
                                new SKPoint(mitte - 7f, rc.Top - 12f),
                                new SKPoint(mitte + 7f, rc.Top - 12f),
                                new SKPoint(mitte, rc.Top - 1f)
                            }, marke);
                        });
                    }
                });
            }

            // DIE SÄULEN — je Jahr eine, negative in Rot.
            if (mitSaeulen)
            {
                var gut = Flaeche(netto.Farbe);
                var schlecht = Flaeche(C_RASTER_SCHLECHT);
                string saeulenmarke = "reihe:" + (netto.Name ?? "");
                z.Markiert(saeulenmarke, zr =>
                {
                    for (int i = 0; i < n && i < netto.Werte.Length; i++)
                    {
                        double wert = netto.Werte[i];
                        float y = (float)(rc.Bottom - (wert - min) / (max - min) * rc.Height);
                        float oben = Math.Min(y, y0), unten = Math.Max(y, y0);
                        if (unten - oben < 1f) unten = oben + 1f;
                        float mitte = rc.Left + (i + 0.5f) * fach;

                        // DG-E3-10: Jede Säule nennt IHR Jahr und IHREN Betrag; die
                        // Zahl steht im Format der eigenen y-Achse („N0", wie die
                        // Beschriftung links).
                        string text = Achsenwert(xTitel, jahre[i], "N0") + WERT_TRENNER +
                                      Elementwert(netto.Name ?? "", wert, "N0",
                                                  Achseneinheit(yTitel ?? titel));
                        zr.Markiert(saeulenmarke, text, zs =>
                            zs.Rechteck(mitte - fach * 0.3f, oben, fach * 0.6f, unten - oben,
                                        null, wert < 0 ? schlecht : gut));
                    }
                });
                z.FuegeReihe(Jahresreihe(netto, jahre, n, 0f));
            }

            // DIE LINIEN — über den Säulenmitten, damit Jahr 1 über Säule 1 liegt.
            foreach (Reihe r in linien)
            {
                int m = Math.Min(n, r.Werte.Length);
                if (m < 2) continue;
                var punkte = new SKPoint[m];
                for (int i = 0; i < m; i++)
                {
                    float x = rc.Left + (i + 0.5f) * fach;
                    float y = (float)(rc.Bottom - (r.Werte[i] - min) / (max - min) * rc.Height);
                    punkte[i] = new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
                }
                float staerke = r.Breite > 0 ? r.Breite : 3f;
                Strichmuster muster = Strichfolge(r.Strichart);
                // Ein Zug über alle Jahre zeigt keine einzelne Zahl — er nennt seinen
                // Namen (Regel der Gruppe (c)); die Zeigerzeile liest die Reihe.
                z.Markiert("reihe:" + (r.Name ?? ""), r.Name ?? "", zr =>
                    Linienzug(zr, punkte,
                              Stift(r.Farbe, staerke, muster, Strichverbindung.Rund)));
                z.FuegeReihe(Jahresreihe(r, jahre, n, staerke));
            }

            // Achsen, gestrichelte Nulllinie und die Jahresbeschriftung.
            Achsenkreuz(z, rc);
            z.Markiert("nulllinie", zn =>
                zn.Linie(rc.Left, y0, rc.Right, y0,
                         Stift(Farbrolle.ACHSE, 2f, new Strichmuster(6f, 4f))));

            int schritt = n <= 12 ? 1 : n <= 25 ? 2 : n <= 50 ? 5 : 10;
            using (var f = Schrift(15f))
            {
                z.Markiert("xachse", zx =>
                {
                    for (int i = 0; i < n; i += schritt)
                    {
                        string lab = jahre[i].ToString(DE);
                        float mitte = rc.Left + (i + 0.5f) * fach;
                        Text(zx, lab, f, Farbrolle.ACHSE, mitte - f.MeasureText(lab) / 2f,
                             rc.Bottom + 8f);
                    }
                    Text(zx, xTitel ?? "", f, Farbrolle.ACHSE,
                         rc.Right - f.MeasureText(xTitel ?? ""), rc.Bottom + 34f);
                });
                z.Markiert("yachse", zy =>
                    Text(zy, yTitel ?? "", f, Farbrolle.ACHSE, rc.Left, rc.Top - 26f));
            }

            return z;
        }

        /// <summary>
        /// Eine Reihe der Jahresprojektion in DATENWERTEN (DG-E3-7): je Projektjahr ein
        /// Wert, das Jahr selbst als x-Stelle. Ohne Zeichenfläche zeichnet der
        /// Schreiber daraus nichts — die Reihe ist die Quelle der Zeigerzeile.
        /// </summary>
        /// <param name="r">Die gezeichnete Reihe.</param>
        /// <param name="jahre">Die Projektjahre in Reihenfolge.</param>
        /// <param name="n">So viele Jahre trägt das Bild.</param>
        /// <param name="staerke">Die Strichstärke; <c>0</c> für die Säulenreihe.</param>
        private static Datenreihe Jahresreihe(Reihe r, IReadOnlyList<int> jahre, int n,
                                              float staerke)
        {
            int m = Math.Min(n, r.Werte.Length);
            var werte = new double[m];
            var stellen = new double[m];
            for (int i = 0; i < m; i++) { werte[i] = r.Werte[i]; stellen[i] = jahre[i]; }
            return new Datenreihe(r.Name ?? "", Ton(r), staerke,
                                  Strichfolge(r.Strichart),
                                  werte, null, Reihenart.Linie, null, null, stellen, "€");
        }

        // ------------------------------------------------------- geteilte Helfer

        /// <summary>Die Reihen, die etwas zu zeichnen haben.</summary>
        private static List<Reihe> Brauchbare(IReadOnlyList<Reihe> reihen)
        {
            return (reihen ?? new List<Reihe>()).Where(Brauchbar).ToList();
        }

        private static bool Brauchbar(Reihe r)
        {
            if (r != null && r.Luecken && r.Werte != null && r.Werte.Length >= 2)
                return r.Werte.Count(Endlich) >= 2;
            return r != null && r.Werte != null && r.Werte.Length >= 2 &&
                   r.Werte.All(w => !double.IsNaN(w) && !double.IsInfinity(w));
        }

        private static void Leerhinweis(IZeichenziel z, SKRect rc)
        {
            using (var f = Schrift(18f))
                Text(z, BerichtTexte.T("Keine Simulationsdaten vorhanden."), f, Farbrolle.ACHSE,
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
        private static void ProzentRaster(IZeichenziel z, SKRect rc)
        {
            ProzentRasterOhneKreuz(z, rc);
            Achsenkreuz(z, rc);
        }

        /// <summary>Derselbe Block ohne das Achsenkreuz — siehe <see cref="BedarfsRasterOhneKreuz"/>.</summary>
        private static void ProzentRasterOhneKreuz(IZeichenziel z, SKRect rc)
        {
            var raster = Stift(Farbrolle.RASTER, 1f);
            using (var f = Schrift(15f))
                for (int p = 0; p <= 100; p += 20)
                {
                    float y = (float)(rc.Bottom - p / Y_PROZENT_MAX * rc.Height);
                    z.Linie(rc.Left, y, rc.Right, y, raster);
                    string lab = p.ToString(DE) + " %";
                    Text(z, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                         y - TextHoehe(f) / 2f);
                }
        }

        /// <summary>Die beiden Achsenlinien — links und unten, in jedem Rasterhelfer gleich.</summary>
        private static void Achsenkreuz(IZeichenziel z, SKRect rc)
        {
            var achse = Stift(Farbrolle.ACHSE, 2f);
            z.Linie(rc.Left, rc.Top, rc.Left, rc.Bottom, achse);
            z.Linie(rc.Left, rc.Bottom, rc.Right, rc.Bottom, achse);
        }

        /// <summary>Raster, y-Beschriftung und Achsen einer Skala 0…max mit fuenf Stufen.</summary>
        private static void YRaster(IZeichenziel z, SKRect rc, double max)
        {
            YRasterOhneKreuz(z, rc, max);
            Achsenkreuz(z, rc);
        }

        /// <summary>Derselbe Block ohne das Achsenkreuz — siehe <see cref="BedarfsRasterOhneKreuz"/>.</summary>
        private static void YRasterOhneKreuz(IZeichenziel z, SKRect rc, double max)
        {
            var raster = Stift(Farbrolle.RASTER, 1f);
            using (var f = Schrift(15f))
                for (int i = 0; i <= 5; i++)
                {
                    double wert = max * i / 5.0;
                    float y = (float)(rc.Bottom - wert / max * rc.Height);
                    z.Linie(rc.Left, y, rc.Right, y, raster);
                    string lab = wert.ToString(max >= 10 ? "N0" : "N1", DE);
                    Text(z, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 6f,
                         y - TextHoehe(f) / 2f);
                }
        }

        /// <summary>
        /// Die x-Achse: entweder Monatsgrenzen 0…12 (<c>ConfigureXAxisWithMonths</c>)
        /// oder die vier Stundenmarken 2000/4000/6000/8000
        /// (<c>ConfigureXAxisWithHours</c>). <paramref name="n"/> ist die Laenge der
        /// Reihen und traegt damit die Unterscheidung Stunden/Viertelstunden.
        ///
        /// <para><b>Anwenderrückmeldung 12.09.2026 (#234): die Einheit fehlte.</b> Die
        /// Marken standen nackt da — „0 … 12" bzw. „2.000 … 8.000" —, und weder Monat
        /// noch Jahresstunde waren daran zu erkennen. Der Titel steht seither UNTER den
        /// Marken (<see cref="XAchsentitel"/>); er kommt aus dem Ressourcenkatalog und
        /// wechselt damit die Sprache mit der Oberfläche.</para>
        /// </summary>
        private static void XAchse(IZeichenziel z, SKRect rc, Achse achse, int n)
        {
            var raster = Stift(Farbrolle.RASTER, 1f);
            using (var f = Schrift(15f))
            {
                if (achse == Achse.Monate)
                {
                    for (int m = 0; m <= 12; m++)
                    {
                        float x = rc.Left + m / 12f * rc.Width;
                        z.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = m.ToString(DE);
                        Text(z, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
                }
                else
                {
                    // Jahresstunden: die vier Marken des Vorlaeufers, auf die Reihenlaenge
                    // bezogen - damit stimmt das Bild auch im Viertelstundenraster.
                    int[] stunden = { 2000, 4000, 6000, 8000 };
                    double stundenJeWert = n > Kanalsatz.STUNDEN_JAHR ? 0.25 : 1.0;
                    foreach (int h in stunden)
                    {
                        double index = h / stundenJeWert;
                        if (index >= n) continue;
                        float x = rc.Left + (float)(index / (n - 1)) * rc.Width;
                        z.Linie(x, rc.Top, x, rc.Bottom, raster);
                        string lab = h.ToString("N0", DE);
                        Text(z, lab, f, Farbrolle.ACHSE, x - f.MeasureText(lab) / 2f, rc.Bottom + 8f);
                    }
                }
            }

            XAchsentitel(z, rc, achse == Achse.Monate
                                ? MyResource.Resource.CHART_ACHSE_MONAT
                                : MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN);
        }

        /// <summary>
        /// <b>Der Titel der x-Achse — EIN Weg für jedes Bild, das den Achsenhelfer
        /// nimmt</b> (#234). Er steht MITTIG unter den Marken: Diese liegen bei
        /// <c>rc.Bottom + 8</c> und sind rund 20 Bildpunkte hoch, der Titel beginnt
        /// deshalb bei <c>rc.Bottom + 30</c>. Bei allen Bildern, die hierher kommen,
        /// liegt <c>rc.Bottom</c> bei 460…470 und die Bildhöhe bei 542…560 — der Titel
        /// bleibt damit innerhalb der Fläche, und kein Bildmaß ändert sich.
        /// </summary>
        private static void XAchsentitel(IZeichenziel z, SKRect rc, string titel)
        {
            if (string.IsNullOrEmpty(titel)) return;
            using (var f = Schrift(15f))
                Text(z, titel, f, Farbrolle.ACHSE,
                     rc.Left + (rc.Width - f.MeasureText(titel)) / 2f, rc.Bottom + 30f);
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
        //   4. die letzte Ersatzschrift der Kette — der Notnagel, der nie null ist.
        //
        // Auf Systemen mit fontconfig kann Schritt 1 bereits bei „Calibri" eine
        // Ersatzschrift liefern (fontconfig antwortet immer mit einer Naeherung);
        // das ist gewollt — gebraucht wird eine lesbare serifenlose Schrift, nicht
        // ausgerechnet Calibri.
        //
        // Die Punktgroessen des Bestandes (14…22 pt) waren GDI+-Punkte bei 96 dpi.
        // Die Schriftkette rechnet in Pixeln, deshalb pt * 96/72. Damit bleiben
        // Textgroesse und Bildmasse dieselben wie vor der Portierung.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Schrift in Punkt (wie im Bestand) — ein <see cref="Schriftmass"/>, das die
        /// Vermessung (<c>MeasureText</c>) und den Modellsatz zugleich traegt. Die
        /// Textvermessung bleibt damit eine Kern-Funktion, die das Layout VOR dem
        /// Befehl nutzt; der Befehl selbst traegt fertige Koordinaten.
        /// </summary>
        private static Schriftmass Schrift(float punkt, bool fett = false, bool kursiv = false)
            => new Schriftmass(new Zeichnung.Schrift(punkt, fett, kursiv));

        /// <summary>Zeilenhöhe einer Schrift — Ersatz für <c>MeasureString(...).Height</c>.</summary>
        private static float TextHoehe(Schriftmass f) => f.Hoehe;

        /// <summary>
        /// Text an der linken OBEREN Ecke (x, y) — dieselbe Bezugsecke wie
        /// <c>Graphics.DrawString</c>. Skia bezieht sich auf die Grundlinie, deshalb wird
        /// der Aufstieg (negativ) abgezogen; das macht der Maler.
        /// </summary>
        private static void Text(IZeichenziel z, string text, Schriftmass f, Farbton ton,
                                 float x, float y)
        {
            if (string.IsNullOrEmpty(text)) return;
            z.Text(text, x, y, f.Satz, ton);
        }

        /// <summary>
        /// Derselbe Text mit AUSDRÜCKLICH genannter Farbrolle (DG-Q7) — die
        /// Schreibweise für jede Beschriftung, deren Farbe die Zeichenmethode selbst
        /// wählt. Sie trifft auch dann die gemeinte Rolle, wenn eine zweite denselben
        /// Wert trägt.
        /// </summary>
        private static void Text(IZeichenziel z, string text, Schriftmass f, Farbrolle rolle,
                                 float x, float y)
            => Text(z, text, f, Farbton.Aus(rolle), x, y);

        /// <summary>
        /// Derselbe Text in einer Farbe, die von AUSSEN kommt (eine Reihenfarbe) — sie
        /// bekommt ihre Rolle über die Rückwärtssuche der Palette.
        /// </summary>
        private static void Text(IZeichenziel z, string text, Schriftmass f, SKColor farbe,
                                 float x, float y)
            => Text(z, text, f, farbe.Ton(), x, y);

        // =================================================================== Helfer

        /// <summary>
        /// Ein Bild als ZEICHENMODELL: Jede Zeichenmethode fuellt eine Befehlsliste,
        /// die <c>SkiaMaler.Png</c> ausgibt. Eine Leinwand sieht der Renderer nicht.
        /// </summary>
        private static Zeichenmodell Modell(int breite, int hoehe)
            => new Zeichenmodell(breite, hoehe, new Farbton(Farbrolle.HINTERGRUND));

        /// <summary>Dieselbe Flächenfarbe als MODELLWERT (Rolle statt Zahl).</summary>
        private static Zeichnung.Fuellung Flaeche(SKColor farbe)
            => new Zeichnung.Fuellung(farbe.Ton());

        /// <summary>Die Fläche zu einem fertigen Farbton.</summary>
        private static Zeichnung.Fuellung Flaeche(Farbton ton)
            => new Zeichnung.Fuellung(ton);

        // ======================================== Der Farbton einer Reihe (DG-E5)

        /// <summary>
        /// <b>Der Farbton einer Reihe</b> (DG-E5, Anwenderentscheid DG-Q8): Nennt die
        /// Reihe ihre <see cref="Farbrolle"/>, gilt sie — auch dann, wenn ihre
        /// gerechnete Farbe zufällig die Hausfarbe einer anderen Rolle trifft. Erst
        /// eine Reihe OHNE Rolle geht durch die Rückwärtssuche der Palette.
        /// </summary>
        private static Farbton Ton(Reihe r)
        {
            if (r == null) return Farbton.Aus(Farbrolle.UNBENANNT);
            return r.Ton ?? r.Farbe.Ton();
        }

        /// <summary>Derselbe Farbton mit anderer Deckung (der Fall der Stapelflächen).</summary>
        private static Farbton Ton(Reihe r, byte deckung)
        {
            if (r == null) return Farbton.Aus(Farbrolle.UNBENANNT);
            return r.Ton != null ? r.Ton.MitDeckung(deckung) : r.Farbe.WithAlpha(deckung).Ton();
        }

        /// <summary>Der Farbton einer Punktreihe — dieselbe Regel wie bei der Reihe.</summary>
        private static Farbton Ton(Punktreihe r)
        {
            if (r == null) return Farbton.Aus(Farbrolle.UNBENANNT);
            return r.Ton ?? r.Farbe.Ton();
        }

        /// <summary>Trägt die Reihe DIESE Rolle? (Ohne Rolle entscheidet der Farbwert.)</summary>
        private static bool Traegt(Reihe r, Farbrolle rolle, SKColor hausfarbe)
            => r != null && (r.Ton != null ? r.Rolle == rolle : r.Farbe == hausfarbe);

        /// <summary>
        /// Die Fläche einer AUSDRÜCKLICH genannten Rolle (DG-Q7) — die Schreibweise
        /// für jede Farbe, die die Zeichenmethode selbst wählt. Die Rückwärtssuche
        /// <see cref="Flaeche(SKColor)"/> bleibt den Farben, die von außen kommen.
        /// </summary>
        private static Zeichnung.Fuellung Flaeche(Farbrolle rolle)
            => new Zeichnung.Fuellung(Farbton.Aus(rolle));

        /// <summary>Der Strich zu einem fertigen Farbton.</summary>
        private static Zeichnung.Stift Stift(Farbton ton, float staerke,
                                             Strichmuster muster = null,
                                             Strichverbindung verbindung = Strichverbindung.Gehrung)
            => new Zeichnung.Stift(ton, staerke, muster, Strichkappe.Stumpf, verbindung);

        /// <summary>Derselbe Strich in einer Farbe, die von AUSSEN kommt (Rückwärtssuche).</summary>
        private static Zeichnung.Stift Stift(SKColor farbe, float staerke,
                                             Strichmuster muster = null,
                                             Strichverbindung verbindung = Strichverbindung.Gehrung)
            => Stift(farbe.Ton(), staerke, muster, verbindung);

        /// <summary>Derselbe Strich mit AUSDRÜCKLICH genannter Rolle (DG-Q7).</summary>
        private static Zeichnung.Stift Stift(Farbrolle rolle, float staerke,
                                             Strichmuster muster = null,
                                             Strichverbindung verbindung = Strichverbindung.Gehrung)
            => Stift(Farbton.Aus(rolle), staerke, muster, verbindung);

        // ========================================= Der Wert am Element (DG-E3-6)
        //
        // Ein Datenelement der Gruppe (c) - eine Saeule, eine Stapelschicht, ein
        // Balken, eine Rasterzelle, ein Ring- oder Kuchensegment - traegt neben
        // seiner Marke den FERTIG FORMATIERTEN Text, den die Oberflaeche beim Zeigen
        // darauf anzeigt. Formatiert wird HIER, wo auch die Beschriftung des Bildes
        // entsteht: in der DE-Kultur des Renderers und mit den Nachkommastellen der
        // eigenen Achse. Die Oberflaeche rechnet nichts nach - sonst zeigte der
        // Zeigetext eine andere Zahl als das Bild darunter.

        /// <summary>Der Trenner zweier Teile einer Elementkennung — „Jan · Wärmepumpe".</summary>
        private const string WERT_TRENNER = " · ";

        /// <summary>
        /// Der Wert am Element: <c>„&lt;Was&gt;: &lt;Zahl&gt; &lt;Einheit&gt;"</c>.
        /// Ohne <paramref name="was"/> bleibt der Doppelpunkt weg, ohne
        /// <paramref name="einheit"/> die Einheit.
        /// </summary>
        /// <param name="was">Die Kennung des Elements — Monat, Reihenname, beides.</param>
        /// <param name="wert">Die Zahl, die das Element zeigt.</param>
        /// <param name="format">Das Zahlenformat DER EIGENEN ACHSE des Bildes.</param>
        /// <param name="einheit">Die Einheit; leer = ohne.</param>
        private static string Elementwert(string was, double wert, string format, string einheit)
            => (string.IsNullOrEmpty(was) ? "" : was + ": ") +
               wert.ToString(format, DE) +
               (string.IsNullOrEmpty(einheit) ? "" : " " + einheit);

        /// <summary>
        /// Der Wert eines ANTEILS-Elements (Kuchen, Ring): <c>„&lt;Name&gt;: 48,0 %"</c>.
        /// Ein Kreissegment zeigt keinen Betrag, sondern seinen Anteil am Ganzen —
        /// und in derselben Stufung („N1"), in der die Bilder ihn beschriften.
        /// </summary>
        private static string Anteilwert(string name, double wert, double summe)
            => Elementwert(name, summe > 0 ? wert / summe * 100.0 : 0.0, "N1", "%");

        /// <summary>
        /// Eine Achsenangabe für den Wert am Element: <c>„Kapazität 220 kWh"</c> aus
        /// der Achsenbeschriftung <c>„Kapazität [kWh]"</c> und der Stützstelle.
        ///
        /// <para>Die Einheit steht in der Beschriftung in eckigen Klammern; sie wandert
        /// hinter die Zahl, wo sie hingehört. Was hinter der Klammer noch folgt — die
        /// C-Raten-Achse trägt die Erläuterung „(Leistung = Kapazität × C-Rate)" —
        /// bleibt weg: Es ist ein Satz über die ACHSE und keiner über die Zelle.</para>
        /// </summary>
        private static string Achsenwert(string achsentitel, double wert, string format)
        {
            string name = achsentitel ?? "";
            string einheit = "";
            int auf = name.IndexOf('[');
            int zu = auf < 0 ? -1 : name.IndexOf(']', auf + 1);
            if (zu > auf)
            {
                einheit = name.Substring(auf + 1, zu - auf - 1).Trim();
                name = name.Substring(0, auf);
            }
            name = name.Trim();
            return (name.Length == 0 ? "" : name + " ") +
                   wert.ToString(format, DE) +
                   (einheit.Length == 0 ? "" : " " + einheit);
        }

        /// <summary>
        /// Ein Kreissegment als Modellbefehl (ersetzt <c>Graphics.FillPie</c>).
        ///
        /// <para><b>Der VOLLKREIS ist ein eigener Fall (Befund zu Auftrag #222).</b>
        /// Ein Bogen ueber 360° zieht NICHTS: Anfangs- und Endpunkt fallen zusammen,
        /// und der geschlossene Pfad ist die leere Strecke vom Mittelpunkt zum
        /// Kreisrand und zurueck. Genau das sah der Anwender am Stromring ohne
        /// Erzeuger — ein Segment ueber den ganzen Kreis, und im Bild stand ein
        /// LEERER Kreis („Strombedarfsdeckung bei 0 ist das Diagramm nicht gut",
        /// 11.09.2026). Ab 360° wird deshalb ein Kreis gezeichnet und kein Bogen;
        /// den Fall entscheidet der Ausgabeweg (<c>SkiaMaler</c>). Dieselbe Falle
        /// traf den Kuchen mit nur einem Segment.</para>
        /// </summary>
        private static void Kreissegment(IZeichenziel z, SKRect rect, float start, float sweep,
                                         Zeichnung.Fuellung fuellung)
            => z.Fuege(new Zeichnung.Kreissegment(rect.Left, rect.Top, rect.Width, rect.Height,
                                                  start, sweep, null, fuellung));

        /// <summary>Streckenzug als Modellbefehl.</summary>
        private static void Linienzug(IZeichenziel z, SKPoint[] punkte, Zeichnung.Stift stift)
        {
            if (punkte == null || punkte.Length < 2) return;
            z.Pfad(SkiaBruecke.Modellpunkte(punkte), false, stift);
        }

        /// <summary>Gefülltes Vieleck als Modellbefehl.</summary>
        private static void Vieleck(IZeichenziel z, SKPoint[] punkte, Zeichnung.Fuellung fuellung)
        {
            if (punkte == null || punkte.Length < 3) return;
            z.Pfad(SkiaBruecke.Modellpunkte(punkte), true, null, fuellung);
        }

        private static void Titel(IZeichenziel z, string text, int breite)
        {
            using (var f = Schrift(22f, fett: true))
                Text(z, text, f, Farbrolle.STAMM, 24f, 16f);
        }

        /// <summary>
        /// Der Titel im ZIELMASS (Stufe 2, BV-E5): Passt er nicht in die Breite, wird die Schrift in
        /// Schritten bis 14 pt kleiner. Ohne Zielmaß (<paramref name="einpassen"/> falsch) genau
        /// <see cref="Titel(IZeichenziel, string, int)"/> — das Bild bleibt byte-gleich.
        /// </summary>
        private static void Titel(IZeichenziel z, string text, int breite, bool einpassen)
        {
            if (!einpassen)
            {
                Titel(z, text, breite);
                return;
            }
            float punkt = 22f;
            while (punkt > 14f)
            {
                using (var probe = Schrift(punkt, fett: true))
                    if (probe.MeasureText(text ?? "") <= breite - 48f) break;
                punkt -= 1f;
            }
            using (var f = Schrift(punkt, fett: true))
                Text(z, text, f, Farbrolle.STAMM, 24f, 16f);
        }

        /// <summary>
        /// Wie viele Zeilen eine Legende dieser Einträge ab <paramref name="x"/> bis
        /// <paramref name="umbruchBei"/> belegt — dieselbe Vermessung wie <see cref="Legende"/>
        /// (Stufe 2: die Zeichenfläche räumt der umbrechenden Legende Platz).
        /// </summary>
        private static int LegendenZeilen(List<Segment> eintraege, float x, float umbruchBei)
        {
            if (eintraege == null || eintraege.Count == 0) return 1;
            var breiten = new List<float>();
            using (var f = Schrift(16f))
                foreach (Segment s in eintraege) breiten.Add(40f + f.MeasureText(s.Label ?? "") + 24f);
            return (int)Math.Round(LegendenHoehe(breiten, x, umbruchBei) / LEGENDE_ZEILE);
        }

        /// <summary>
        /// Der Rahmen EINES Wochenfeldes samt seiner Überschrift (Speicherverlauf,
        /// Speichertemperaturen).
        ///
        /// <para><b>Die Marken (Etappe DG-E3, Gruppe d).</b> Der Rahmen ist das
        /// Achsenkreuz des Feldes und bleibt deshalb OHNE Marke — dieselbe Regel wie
        /// beim <see cref="Achsenkreuz"/> der ganzflächigen Bilder. Die Überschrift
        /// sagt, welche Woche das Feld zeigt; sie ist die Beschriftung seiner
        /// Zeitachse und trägt darum <c>xachse</c>.</para>
        /// </summary>
        /// <summary>
        /// Stufe 2 (BV-E5): die Überschriften der drei Wochenfelder, wenn die langen („Winterwoche (Jan)“)
        /// nicht in ein Feld der Breite <paramref name="feldbreite"/> passen — erst „Winter (Jan)“, dann nur
        /// der Monat. Dieselbe Schrift wie <see cref="PanelRahmen"/>.
        /// </summary>
        private static string[] Wochentitel(string[] lang, float feldbreite)
        {
            string[][] stufen =
            {
                lang,
                new[] { "Winter (Jan)", "Übergang (Apr)", "Sommer (Jul)" },
                new[] { "Jan", "Apr", "Jul" },
            };
            using (var f = Schrift(16f, fett: true))
                foreach (string[] titel in stufen)
                    if (titel.All(t => f.MeasureText(t) <= feldbreite - 8f)) return titel;
            return stufen[stufen.Length - 1];
        }

        private static void PanelRahmen(IZeichenziel z, SKRect rc, string titel)
        {
            z.Rechteck(rc.Left, rc.Top, rc.Width, rc.Height, Stift(Farbrolle.RAHMEN, 1f));
            using (var f = Schrift(16f, fett: true))
                z.Markiert("xachse", zx => Text(zx, titel, f, Farbrolle.ACHSE, rc.Left, rc.Top - 28f));
        }

        /// <summary>
        /// Waagerechtes Raster samt y-Beschriftung, wahlweise senkrechte Teilung mit
        /// x-Beschriftung, dazu das Achsenkreuz.
        ///
        /// <para><b>Die Marken (Etappe DG-E3, Gruppe d).</b> Der y-Teil steht unter
        /// <c>yachse</c>, der x-Teil unter <c>xachse</c>, das <see cref="Achsenkreuz"/>
        /// unter keiner von beiden: Blendet die Oberfläche die Teilung einer Achse
        /// aus, müssen die zwei Achsenlinien stehen bleiben. Die Befehlsreihenfolge
        /// ändert sich dadurch nicht — das PNG bleibt byte-gleich.</para>
        /// </summary>
        private static void AchsenRaster(IZeichenziel z, SKRect rc, double max,
                                         int[] xpos, string[] xlab, int n)
        {
            AchsenRasterOhneKreuz(z, rc, max, xpos, xlab, n);
            Achsenkreuz(z, rc);
        }

        /// <summary>
        /// Derselbe Rasterblock OHNE das Achsenkreuz (Etappe E3) — siehe
        /// <see cref="BedarfsRasterOhneKreuz"/>: Ein Bild, das seine Achse MARKIERT,
        /// klammert nur diesen Teil; die beiden Achsenlinien bleiben markenlos.
        /// </summary>
        private static void AchsenRasterOhneKreuz(IZeichenziel z, SKRect rc, double max,
                                                  int[] xpos, string[] xlab, int n)
        {
            var raster = Stift(Farbrolle.RASTER, 1f);
            using (var f = Schrift(15f))
            {
                z.Markiert("yachse", zy =>
                {
                    for (int s = 0; s <= 4; s++)
                    {
                        float y = rc.Bottom - s * rc.Height / 4f;
                        zy.Linie(rc.Left, y, rc.Right, y, raster);
                        string lab = (max * s / 4.0).ToString("N0", DE);
                        float breite = f.MeasureText(lab);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - breite - 6f, y - TextHoehe(f) / 2f);
                    }
                });
                if (xpos != null)
                    z.Markiert("xachse", zx =>
                    {
                        for (int i = 0; i < xpos.Length; i++)
                        {
                            float x = rc.Left + (float)xpos[i] / Math.Max(n - 1, 1) * rc.Width;
                            zx.Linie(x, rc.Top, x, rc.Bottom, raster);
                            float breite = f.MeasureText(xlab[i]);
                            Text(zx, xlab[i], f, Farbrolle.ACHSE, x - breite / 2f, rc.Bottom + 8f);
                        }
                    });
            }
        }

        private static void ZeichneLinie(IZeichenziel z, SKRect rc, double[] werte,
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
                Linienzug(z, punkte.ToArray(),
                          Stift(farbe, staerke, null, Strichverbindung.Rund));
        }

        private static void ZeichneFlaeche(IZeichenziel z, SKRect rc, double[] unten,
                                           double[] oben, double max, SKColor farbe,
                                           byte alpha = 210)
        {
            int n = oben.Length;
            int schritt = Math.Max(1, n / (int)rc.Width);
            var pfad = new List<SKPoint>();
            for (int i = 0; i < n; i += schritt)
                pfad.Add(Punkt(rc, i, n, oben[i], max));
            for (int i = ((n - 1) / schritt) * schritt; i >= 0; i -= schritt)
                pfad.Add(Punkt(rc, i, n, unten[i], max));
            if (pfad.Count >= 3)
                Vieleck(z, pfad.ToArray(), Flaeche(farbe.WithAlpha(alpha)));
        }

        private static SKPoint Punkt(SKRect rc, int i, int n, double wert, double max)
        {
            float x = rc.Left + (float)i / (n - 1) * rc.Width;
            float y = rc.Bottom - (float)(wert / max * rc.Height);
            return new SKPoint(x, Math.Max(rc.Top, Math.Min(rc.Bottom, y)));
        }

        /// <summary>Höhe EINER Legendenzeile [px] — der Schritt des Umbruchs.</summary>
        public const float LEGENDE_ZEILE = 30f;

        /// <summary>
        /// Stufe 2 (BV-E5): die kleinste Höhe der Zeichenfläche im Zielmaß [px]. Räumt sie einer
        /// umbrechenden Legende mehr Platz, wird das Bild höher statt die Fläche flacher — die
        /// Engine passt es dann mit seinem Seitenverhältnis in den Rahmen.
        /// </summary>
        public const float STUFE2_MIN_FLAECHE = 140f;

        /// <summary>Stufe 2: die kleinste Breite des Brückenbilds — schmaler stoßen Säulennamen und Beträge
        /// aneinander; ein schmalerer Rahmen bekommt das Bild in dieser Breite, verkleinert (Stufe 1).</summary>
        public const int BRUECKE_MIN_BREITE = 1000;

        /// <summary>Stufe 2: die kleinste Breite des Spannenbilds (Euro-Achse und Legende).</summary>
        public const int SPANNE_MIN_BREITE = 900;

        /// <summary>Stufe 2: die kleinste Breite des Verlaufs mit drei Szenarien (zweigeteilte Legende).</summary>
        public const int SZENARIEN_MIN_BREITE = 760;

        /// <summary>
        /// Zeichnet die Legende und liefert die Höhe, die sie belegt hat [px]
        /// (Anzahl Zeilen × <see cref="LEGENDE_ZEILE"/>).
        ///
        /// <para><b>Warum sie ihre Höhe zurückgibt</b> (W11b‑B‑28): Bis dahin nahm jeder
        /// Aufrufer EINE Zeile an und setzte sein Zeichenrechteck auf einen festen
        /// Abstand darunter. Bei vier Serien bricht die Legende in eine zweite Zeile um
        /// (Befund am Bild „Lastgang und Speicherbetrieb"), und die lag dann auf dem
        /// Achsentitel. Wer den Platz kennt, kann ihn räumen.</para>
        ///
        /// <para><b>Die MARKE steht hier</b> (Etappe E2), nicht an den zwölf
        /// Aufrufstellen: Farbfeld, Rahmen und Text EINES Eintrags bekommen zusammen
        /// die Marke <c>legende:&lt;Label&gt;</c>. Damit trägt jedes der zwölf Bilder
        /// sie, und die Oberfläche hat eine Trefferfläche je Eintrag — die
        /// Voraussetzung dafür, dass ein Klick auf einen Legendeneintrag den
        /// Farbwähler öffnet (Farbrollen, Bedienung Teil 2).</para>
        /// </summary>
        private static float Legende(IZeichenziel z, List<Segment> eintraege, float x, float y,
                                     float umbruchBei = 0)
        {
            float startX = x;
            int zeilen = 1;
            var rahmen = Stift(Farbrolle.LEGENDENRAHMEN, 1f);
            using (var f = Schrift(16f))
                foreach (Segment s in eintraege)
                {
                    float breite = 40f + f.MeasureText(s.Label ?? "") + 24f;
                    if (umbruchBei > 0 && x > startX && x + breite > umbruchBei)
                    { x = startX; y += LEGENDE_ZEILE; zeilen++; }   // Umbruch bei vielen Serien (Review 11)

                    float ex = x, ey = y;      // fest fuer die Klammer der Marke
                    z.Markiert("legende:" + (s.Label ?? ""), ze =>
                    {
                        // AUFTRAG U18: Ein Eintrag zu einer gestrichelten Linie bekommt ein
                        // gestricheltes Feld in der Reihenfarbe statt einer vollen Füllung —
                        // sonst sagt die Legende über die Strichart nichts, und im
                        // Schwarz-Weiß-Ausdruck sind zwei Linien nicht auseinanderzuhalten.
                        // ETAPPE E6: dasselbe für die gepunktete Linie, in IHRER Folge.
                        if (s.Strichart != Strichart.Durchgezogen)
                            ze.Rechteck(ex, ey, 22f, 22f, Stift(s.Farbe, 3f, Strichfolge(s.Strichart)));
                        else
                            ze.Rechteck(ex, ey, 22f, 22f, null, Flaeche(s.Farbe));
                        ze.Rechteck(ex, ey, 22f, 22f, rahmen);
                        Text(ze, s.Label, f, Farbrolle.TEXT, ex + 28f, ey + 1f);
                    });
                    x += breite;
                }
            return zeilen * LEGENDE_ZEILE;
        }

        /// <summary>
        /// Die Höhe, die eine Legende dieser Eintragsbreiten belegt [px] — DIESELBE
        /// Umbruchregel wie <see cref="Legende"/>, nur ohne Zeichenfläche (W11b‑B‑28).
        ///
        /// <para>Sie ist die prüfbare Fassung der Regel: Ein bunit- oder xUnit-Fall kann
        /// keine Bildpunkte lesen, wohl aber diese Funktion mit vier gleich breiten
        /// Einträgen aufrufen und feststellen, dass sie bei zu schmalem Band auf zwei
        /// Zeilen geht. Die Breite eines Eintrags ist im Bild
        /// <c>40 + Textbreite + 24</c>.</para>
        /// </summary>
        /// <param name="eintragsbreiten">Breite je Eintrag [px], in Zeichenreihenfolge.</param>
        /// <param name="x">Linke Kante der Legende — zugleich der Anschlag nach einem Umbruch.</param>
        /// <param name="umbruchBei">
        /// Rechte Kante; <c>0</c> oder kleiner heisst „kein Umbruch" und damit genau eine Zeile.
        /// </param>
        public static float LegendenHoehe(IReadOnlyList<float> eintragsbreiten, float x, float umbruchBei)
        {
            if (eintragsbreiten == null || eintragsbreiten.Count == 0) return LEGENDE_ZEILE;

            float startX = x;
            int zeilen = 1;
            foreach (float breite in eintragsbreiten)
            {
                if (umbruchBei > 0 && x > startX && x + breite > umbruchBei)
                { x = startX; zeilen++; }
                x += breite;
            }
            return zeilen * LEGENDE_ZEILE;
        }

        /// <summary>
        /// Die Höhe einer Legende mit <paramref name="anzahl"/> GLEICH breiten Einträgen
        /// (W11b‑B‑28) — die bequeme Fassung von
        /// <see cref="LegendenHoehe(IReadOnlyList{float}, float, float)"/> für Prüfungen
        /// und Abschätzungen.
        /// </summary>
        public static float LegendenHoehe(int anzahl, float eintragsbreite,
                                          float x, float breiteVerfuegbar)
        {
            var breiten = new List<float>(Math.Max(0, anzahl));
            for (int i = 0; i < anzahl; i++) breiten.Add(eintragsbreite);
            return LegendenHoehe(breiten, x, x + breiteVerfuegbar);
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

        // =================================================================== Datenzoom
        //
        // WINDOWS-ABNAHME 05.09.2026, Befund A-1 („Allgemein bei Charts: das Zoomen
        // funktioniert nicht“). Der Baustein Diagramm in EPOS.UI vergrößert JEDES
        // Bild als Bild — für eine Ganglinie mit 8 760 Stützstellen auf 1 100
        // Bildpunkten ist das aber zu grob: Dort liegen acht Stunden auf einem
        // Bildpunkt, und vergrößerte Bildpunkte zeigen keine Stunde mehr. Deshalb
        // zieht der Anwender ein Rechteck auf, und das Bild wird mit DIESEM
        // Achsenbereich NEU gezeichnet — wie der Achsenzoom des WinForms-Vorbilds.
        //
        // ALLES HIER IST WAHLFREI. Ohne Fenster zeichnet jede Methode Bildpunkt für
        // Bildpunkt dasselbe wie vorher; die ChartProben prüfen genau das.

        /// <summary>
        /// Der Achsenbereich, mit dem ein Ganglinienbild neu gezeichnet wird.
        /// </summary>
        /// <param name="Von">Erste Stützstelle (einschließlich).</param>
        /// <param name="Bis">Letzte Stützstelle (ausschließlich).</param>
        /// <param name="YAnteil">Neue Obergrenze der y-Achse als Anteil der
        /// bisherigen; <c>0</c> = die Achse bleibt, wie sie war. <b>Die Null bleibt
        /// immer unten</b> — alle Ganglinienbilder zählen von null aufwärts, und
        /// ein Ausschnitt, der die Null verlöre, wäre als Leistungsbild nicht mehr
        /// zu lesen.</param>
        public sealed record Achsenfenster(int Von, int Bis, double YAnteil = 0);

        /// <summary>
        /// Das aufgezogene Rechteck als ANTEILE DES BILDES (0 links/oben bis 1
        /// rechts/unten). So und nicht anders kann die Oberfläche messen: Sie sieht
        /// ein PNG und weiß weder, wo die Zeichenfläche darin liegt, noch welche
        /// Stunde an welcher Stelle steht.
        /// </summary>
        public sealed record Bildausschnitt(double XVon, double XBis, double YVon, double YBis);

        // Die Zeichenflaeche der beiden Ganglinienbilder B1/B2 in Anteilen des Bildes:
        // SKRect.Create(100, 110, 1240 - 140, 360) auf 1 240 x 560. Steht die zweite
        // y-Achse mit im Bild (B3), ist die Flaeche 50 Bildpunkte schmaler; der Fehler
        // von vier Prozent der Breite faellt beim Aufziehen eines Bereichs nicht auf.
        private const double FLAECHE_LINKS = 100.0 / 1240.0;
        private const double FLAECHE_RECHTS = 1200.0 / 1240.0;
        private const double FLAECHE_OBEN = 110.0 / 560.0;
        private const double FLAECHE_UNTEN = 470.0 / 560.0;

        /// <summary>
        /// Rechnet ein aufgezogenes Rechteck in ein <see cref="Achsenfenster"/> um.
        /// Liefert <c>null</c>, wenn daraus kein sinnvoller Ausschnitt wird (zu
        /// schmal, oder die Reihe ist zu kurz) — dann bleibt das Bild, wie es ist.
        /// </summary>
        /// <param name="a">Das Rechteck in Bildanteilen.</param>
        /// <param name="laenge">Die Anzahl der Stützstellen der gezeigten Reihe.</param>
        public static Achsenfenster FensterAusBild(Bildausschnitt a, int laenge)
        {
            if (a == null || laenge < 4) return null;

            double x0 = Klemme((a.XVon - FLAECHE_LINKS) / (FLAECHE_RECHTS - FLAECHE_LINKS));
            double x1 = Klemme((a.XBis - FLAECHE_LINKS) / (FLAECHE_RECHTS - FLAECHE_LINKS));
            if (x1 - x0 < 1e-6) return null;

            int von = (int)Math.Floor(x0 * (laenge - 1));
            int bis = (int)Math.Ceiling(x1 * (laenge - 1)) + 1;
            von = Math.Max(0, Math.Min(laenge - 2, von));
            bis = Math.Max(von + 2, Math.Min(laenge, bis));
            if (von == 0 && bis == laenge) return null;   // nichts zugeschnitten

            // Die OBERE Kante des Rechtecks wird die neue Obergrenze; die Null bleibt
            // unten. Ein Rechteck, das ohnehin fast bis oben reicht, laesst die Achse
            // in Ruhe - sonst verschoebe sich die Skala bei jedem Zug ein wenig.
            double anteil = 1.0 - Klemme((a.YVon - FLAECHE_OBEN) / (FLAECHE_UNTEN - FLAECHE_OBEN));
            if (anteil > 0.98) anteil = 0;

            return new Achsenfenster(von, bis, anteil);
        }

        private static double Klemme(double wert)
            => wert < 0.0 ? 0.0 : wert > 1.0 ? 1.0 : wert;

        /// <summary>
        /// Eine nackte Wertereihe auf das Fenster zugeschnitten; ohne Fenster (oder wenn
        /// es die ganze Reihe umfasst) kommt sie unverändert zurück — dieselbe Regel wie
        /// bei <see cref="Zugeschnitten(Reihe, Achsenfenster)"/>, nur ohne die Hülle
        /// <see cref="Reihe"/>. Sie trägt den Zeitausschnitt von
        /// <see cref="Jahresverlauf"/> (Anwenderwunsch W8‑E‑2).
        /// </summary>
        private static double[] Ausschnitt(double[] werte, Achsenfenster f)
        {
            if (f == null || werte == null) return werte;

            int von = Math.Max(0, Math.Min(werte.Length, f.Von));
            int bis = Math.Max(von, Math.Min(werte.Length, f.Bis));
            if (von == 0 && bis == werte.Length) return werte;

            var teil = new double[bis - von];
            Array.Copy(werte, von, teil, 0, teil.Length);
            return teil;
        }

        /// <summary>Die Reihe auf das Fenster zugeschnitten; ohne Fenster unverändert.</summary>
        private static Reihe Zugeschnitten(Reihe r, Achsenfenster f)
        {
            if (f == null || r == null || r.Werte == null) return r;
            int von = Math.Max(0, Math.Min(r.Werte.Length, f.Von));
            int bis = Math.Max(von, Math.Min(r.Werte.Length, f.Bis));
            if (von == 0 && bis == r.Werte.Length) return r;

            var werte = new double[bis - von];
            Array.Copy(r.Werte, von, werte, 0, werte.Length);
            return Mit(r, werte);
        }

        /// <summary>
        /// Dieselbe Reihe mit ANDEREN Werten — Farbe, Farbrolle, Stapelart,
        /// Strichfolge und Stärke bleiben. <b>Ohne diesen Weg verlöre jede Kopie ihre
        /// Rolle</b> und damit ihr Farbfeld in der Legende (DG-E5).
        /// </summary>
        private static Reihe Mit(Reihe r, double[] werte)
        {
            Reihe kopie = r.Ton != null
                ? new Reihe(r.Name, werte, r.Ton, r.Stapelgruppe, r.Strichart, r.Breite)
                : new Reihe(r.Name, werte, r.Farbe, r.Stapelgruppe, r.Strichart, r.Breite);
            kopie.Farbe = r.Farbe;
            kopie.Luecken = r.Luecken;
            return kopie;
        }

        /// <summary>Dieselbe Zuschneidung für eine ganze Liste.</summary>
        private static List<Reihe> Zugeschnitten(IReadOnlyList<Reihe> reihen, Achsenfenster f)
        {
            var liste = new List<Reihe>();
            if (reihen == null) return liste;
            foreach (Reihe r in reihen) liste.Add(Zugeschnitten(r, f));
            return liste;
        }

        /// <summary>
        /// Die Anzahl der Stützstellen, die ein Stapelbild führt — die erste Reihe,
        /// die überhaupt eine hat. Sie wird VOR dem Zuschnitt genommen und sagt dem
        /// Fenster, ob es Stunden oder Viertelstunden zählt.
        /// </summary>
        private static int Laenge(IReadOnlyList<Reihe> stapel, IReadOnlyList<Reihe> linien,
                                  Reihe kontur, IReadOnlyList<Reihe> zweiteAchsen = null)
        {
            foreach (Reihe r in Brauchbare(stapel)) return r.Werte.Length;
            foreach (Reihe r in Brauchbare(linien)) return r.Werte.Length;
            if (Brauchbar(kontur)) return kontur.Werte.Length;
            // #234: Traegt NUR die zweite Achse eine Reihe, sagt sie die Laenge - sonst
            // zaehlte das Fenster im Viertelstundenraster Stunden.
            foreach (Reihe r in Brauchbare(zweiteAchsen)) return r.Werte.Length;
            return 0;
        }

        /// <summary>
        /// Die x-Achse eines ZUGESCHNITTENEN Bildes: runde Marken mit den WIRKLICHEN
        /// Jahresstunden des Fensters. Die vier festen Marken 2000/4000/6000/8000 der
        /// Vollansicht (<see cref="XAchse"/>) taugen hier nicht — in einem Fenster von
        /// Stunde 3 100 bis 3 400 läge keine einzige davon.
        ///
        /// <para>Auch ein Monatsbild bekommt im Fenster Stundenmarken: Monatsgrenzen
        /// sagen im Ausschnitt nichts mehr, die Stunde schon.</para>
        /// </summary>
        private static void XAchseFenster(IZeichenziel z, SKRect rc, Achsenfenster f, int gesamt)
        {
            double stundenJeWert = gesamt > Kanalsatz.STUNDEN_JAHR ? 0.25 : 1.0;
            double h0 = f.Von * stundenJeWert;
            double h1 = (f.Bis - 1) * stundenJeWert;
            if (h1 - h0 < 1e-9) return;

            var raster = Stift(Farbrolle.RASTER, 1f);
            using (var schrift = Schrift(15f))
                foreach ((double h, string lab) in Stundenteilung(h0, h1))
                {
                    float x = rc.Left + (float)((h - h0) / (h1 - h0)) * rc.Width;
                    z.Linie(x, rc.Top, x, rc.Bottom, raster);
                    Text(z, lab, schrift, Farbrolle.ACHSE,
                         x - schrift.MeasureText(lab) / 2f, rc.Bottom + 8f);
                }

            // #234: derselbe Achsentitel wie in der Vollansicht - im Fenster zaehlt die
            // Achse IMMER Jahresstunden, auch wenn das Bild sonst Monatsgrenzen traegt.
            XAchsentitel(z, rc, MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN);
        }

        /// <summary>
        /// DIE TEILUNG DER X-ACHSE EINES ZUGESCHNITTENEN BILDES als reine Funktion
        /// (Etappe E2): die runden Jahresstunden zwischen <paramref name="von"/> und
        /// <paramref name="bis"/> samt ihrer Beschriftung, in Zeichenreihenfolge.
        ///
        /// <para><b>Wofür.</b> Im SVG liegen nur die REIHEN in Datenkoordinaten
        /// (Entscheid DG-E2-3); Raster und Beschriftung bleiben Pixel-Elemente. Zoomt
        /// der Anwender die Zeitachse, blendet die Oberfläche die Elemente mit der
        /// Marke <c>xachse</c> aus und zeichnet die Ticks aus DIESER Funktion nach —
        /// nach derselben Regel, mit der <see cref="XAchseFenster"/> sie ins Bild
        /// setzt, und damit ohne Kernaufruf.</para>
        ///
        /// <para>Beide Wege gehen durch <c>Stundenteilung</c>; die gebrochenen Grenzen
        /// einer Viertelstundenreihe (Jahresstunde in Vierteln) bleiben dem Bild
        /// vorbehalten, die Oberfläche zoomt in ganzen Stunden.</para>
        /// </summary>
        /// <param name="von">Erste Jahresstunde des Fensters (einschließlich).</param>
        /// <param name="bis">Letzte Jahresstunde des Fensters (einschließlich).</param>
        public static IReadOnlyList<(int Stunde, string Text)> Jahresstundenteilung(int von, int bis)
        {
            var liste = new List<(int Stunde, string Text)>();
            foreach ((double stunde, string text) in Stundenteilung(von, bis))
                liste.Add(((int)stunde, text));
            return liste;
        }

        /// <summary>
        /// <b>DIE TEILUNG DER X-ACHSE EINES AUSSCHNITTS — für JEDE Achsenart
        /// (Entscheid DG-E3-4).</b> Die Marken zwischen <paramref name="von"/> und
        /// <paramref name="bis"/> samt Beschriftung, in Zeichenreihenfolge.
        ///
        /// <para><b>Wofür.</b> Im SVG liegen nur die Reihen in Datenkoordinaten
        /// (DG-E2-3); Raster und Beschriftung bleiben Pixel-Elemente. Zoomt der
        /// Anwender, blendet die Oberfläche die Elemente mit der Marke <c>xachse</c>
        /// aus und setzt die Marken aus DIESER Funktion neu — ohne Kernaufruf und ohne
        /// zu wissen, was die Achse zählt. Das weiß die
        /// <see cref="Zeichenflaeche.X"/>:</para>
        ///
        /// <list type="bullet">
        ///   <item><see cref="Achsenart.Stunden"/> — über
        ///   <see cref="Jahresstundenteilung"/>, die runden Jahresstunden.</item>
        ///   <item><see cref="Achsenart.Index"/> — GANZZAHLIG: dieselbe Stufenfolge,
        ///   auf eine ganze Zahl abgerundet, mindestens 1. Zwischen zwei Stützstellen
        ///   liegt nichts, was eine Marke benennen könnte.</item>
        ///   <item><see cref="Achsenart.Wert"/> — dieselben „schönen" Stufen
        ///   (1 / 2 / 2,5 / 5 × 10^k), mit denen das Bild seine Achse setzt
        ///   (<see cref="Skala.Rund"/>).</item>
        /// </list>
        ///
        /// <para>Die Beschriftung folgt der SCHRITTWEITE, nicht dem Wert: ab 1 mit
        /// Tausenderpunkt und ohne Nachkommastelle (<c>N0</c>, dieselbe Schreibweise
        /// wie die Jahresstunden), darunter mit bis zu drei Nachkommastellen. Eine
        /// leere Liste heißt „nichts zu teilen" — ein Fenster ohne Breite.</para>
        /// </summary>
        /// <param name="flaeche">Die Zeichenfläche des Modells; <c>null</c> = leere Liste.</param>
        /// <param name="von">Linker Rand des Ausschnitts (einschließlich).</param>
        /// <param name="bis">Rechter Rand des Ausschnitts (einschließlich).</param>
        public static IReadOnlyList<(double Wert, string Text)> Achsenteilung(
            Zeichenflaeche flaeche, double von, double bis)
        {
            var liste = new List<(double Wert, string Text)>();
            if (flaeche == null || double.IsNaN(von) || double.IsNaN(bis)) return liste;
            if (bis - von < 1e-9) return liste;

            if (flaeche.X == Achsenart.Stunden)
            {
                foreach ((int stunde, string text) in
                         Jahresstundenteilung((int)Math.Ceiling(von), (int)Math.Floor(bis)))
                    liste.Add((stunde, text));
                return liste;
            }

            double schritt = RundeStufe((bis - von) / 5.0);
            // Eine INDEXACHSE traegt nur ganze Stufen; 2,5 waere eine halbe
            // Stuetzstelle, und dort steht kein Wert.
            if (flaeche.X == Achsenart.Index) schritt = Math.Max(1.0, Math.Floor(schritt));
            if (schritt <= 0) return liste;

            string format = schritt >= 1.0 ? "N0" : "0.###";
            for (double wert = Math.Ceiling(von / schritt) * schritt;
                 wert <= bis + schritt * 1e-6; wert += schritt)
                // Die Null soll "0" heissen und nicht "-0" (Math.Ceiling auf negativen
                // Zahlen liefert bei ganzzahligen Schritten -0) - dieselbe Regel wie in
                // der Streuwolke und der Schnittkurve.
                liste.Add((wert, (wert == 0 ? 0.0 : wert).ToString(format, DE)));
            return liste;
        }

        /// <summary>
        /// Der gemeinsame Rumpf: runde Stufe über fünf Marken, erste Marke auf dem
        /// nächsten Vielfachen, Beschriftung mit Tausenderpunkt.
        /// </summary>
        private static List<(double Stunde, string Text)> Stundenteilung(double h0, double h1)
        {
            var liste = new List<(double Stunde, string Text)>();
            if (h1 - h0 < 1e-9) return liste;

            double schritt = RundeStufe((h1 - h0) / 5.0);
            double erste = Math.Ceiling(h0 / schritt) * schritt;
            for (double h = erste; h <= h1 + 1e-9; h += schritt)
                liste.Add((h, h.ToString("N0", DE)));
            return liste;
        }

        /// <summary>
        /// Die nächstgrößere „runde“ Schrittweite (1 / 2 / 2,5 / 5 × 10^k) — dieselbe
        /// Stufenfolge, die <see cref="Jahresgang"/> und <see cref="KapitalwertVerlauf"/>
        /// von Hand rechnen.
        /// </summary>
        private static double RundeStufe(double roh) => Skala.Rund(roh);

        // "Schöne" Achsen-Obergrenze (1/2/2,5/5 × 10^k).
        private static double Nice(double max) => Skala.Nice(max);
    }
}
