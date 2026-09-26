using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1.Zeichnung
{
    // =========================================================================
    // DER SVG-AUSGABEWEG (Konzept Diagramme, Etappe E2)
    //
    // Ein Modell, zwei Ausgaben aus EINER Abbildung: SkiaMaler.Png malt das
    // Zeichenmodell fuer den Bericht, SvgSchreiber schreibt dasselbe Modell
    // fuer den Bildschirm. Hier steht deshalb KEIN SkiaSharp - weder als Typ
    // noch als Aufruf; nur so bleibt der Bildschirmweg von der Rasterung frei.
    //
    // DREI ENTSCHEIDE, die hier haengen:
    //
    // DG-E2-2 - DIE REIHEN GEHEN AUS "Datenreihe", NICHT AUS DEM PIXELPFAD.
    // Der Linienzug im Befehl ist auf jeden n-ten Wert gekuerzt (so zeichnet
    // das PNG seit je). Ein Befehl mit der Marke "reihe:*" wird deshalb NICHT
    // geschrieben; an seiner Stelle entsteht das innere <svg>, und darin je
    // Datenreihe ein Pfad nach Pfadregel (roh bis 8 760 Stuetzstellen und drei
    // Reihen, sonst gebuendelt). Fehlt die Flaeche oder gibt es keine Reihen,
    // bleibt es beim Pixelpfad - es faellt nie etwas weg.
    //
    // DG-E2-3 - NUR DIE REIHEN LIEGEN IN DATENKOORDINATEN. Raster, Achsen,
    // Legende und Titel bleiben Pixel-Elemente. Beim Zoom (nur Zeitachse)
    // blendet die Oberflaeche die "xachse"-Elemente aus und zeichnet die Ticks
    // aus ChartRenderer.Jahresstundenteilung nach.
    //
    // DG-E3-1 - SENKRECHT BILDPUNKTE, JEDE REIHE MIT EIGENEM FENSTER. Das
    // innere svg traegt in x weiterhin Stunden, in y aber BILDPUNKTE der
    // Zeichenflaeche (viewBox "XVon 0 Breite Bild.Hoehe"). Jede Datenreihe
    // bringt ihr eigenes Datenfenster mit - den x-Bereich und den y-Bereich
    // IHRER Achse -, und der Schreiber rechnet
    //     y = Bild.Hoehe - (Wert - YVon) / (YBis - YVon) * Bild.Hoehe.
    // Damit tragen Reihen der ZWEITEN Achse (der Speicherinhalt rechts)
    // dasselbe innere svg; Zeichenflaeche.Daten bleibt das Fenster der linken
    // Achse und die Vorgabe fuer Reihen ohne eigenes.
    //
    // DIE Y-UMKEHR steht als RECHNUNG im Pfad, nicht als transform. Damit
    // beginnt die viewBox senkrecht bei 0, und ein Zoom auf der Zeitachse
    // aendert an ihr nur x und Breite - eine Attributaenderung, wie es der
    // Pruefstand gemessen hat. Ein transform="scale(1,-1)" taete dasselbe,
    // haette aber jedes Zoomen ueber zwei Stellen gefuehrt.
    //
    // DG-E3-2 - FLAECHEN. Eine Datenreihe mit Art = Flaeche wird ein
    // GESCHLOSSENER Pfad: Oberkante vorwaerts, Unterkante rueckwaerts, Z. Die
    // Unterkante ist "Unten" (die Summe der Schichten darunter) oder die
    // Achsennull. Gefuellt wird in der Reihenfarbe samt ihrer Deckung, ohne
    // Strich - es sei denn, das PNG zieht eine Randlinie (Randton).
    //
    // DG-E3-3 - DER REIHENPFAD IST OEFFENTLICH, mit einer Ueberladung fuer
    // einen Ausschnitt. Der Baustein rechnet damit beim Zoom ueber das
    // Vierfache den sichtbaren Bereich roh nach - aus den Datenreihen des
    // Modells, ohne Rundlauf in den Kern.
    //
    // DG-E3-5 - PUNKTWOLKEN. Eine Datenreihe mit Art = Punkte bringt ihre
    // x-Stelle JE WERT mit (XWerte) und wird EIN <path> aus Segmenten
    // "M x,y h 0" mit stroke-linecap="round"; die Strichbreite ist der
    // Punktdurchmesser des PNG. Gebuendelt wird nicht - die Verdichtung IST
    // die Aussage der Wolke, und 8 760 Punkte sind ein Knoten. XWerte traegt
    // auch eine LINIE mit ungleichmaessigen Stuetzstellen (die Schnittkurve
    // mischt Grob- und Feinpunkte); ohne sie liegen die Stuetzstellen
    // gleichmaessig zwischen XVon und XBis, wie bisher.
    // DG-E3-6 - DER WERT AM ELEMENT. Traegt ein Befehl neben der Marke einen
    // Zeichenbefehl.Wert - den fertig formatierten Text des Renderers -, so
    // steht er als data-wert AN DERSELBEN STELLE wie data-marke. Damit zeigt
    // die Oberflaeche beim Zeigen auf eine Saeule, eine Zelle oder ein
    // Kreissegment genau die Zahl, die das PNG beschriftet; formatiert wird
    // EINMAL, im Renderer. Der Maler uebergeht ihn wie die Marke.
    //
    // DETERMINISMUS: Attribute stehen in der Reihenfolge, in der sie gebaut
    // werden, Zahlen in InvariantCulture (Bildpunkte "0.##", Datenwerte
    // "0.###"), Zeilenenden sind LF. Kein Zufall, keine Zeitangabe - zweimal
    // Schreiben ergibt denselben Text, Byte fuer Byte.
    // =========================================================================

    /// <summary>
    /// Ein Knoten des SVG-Baums: Name, GEORDNETE Attribute, Kinder, Textinhalt und
    /// die Marke des Befehls, aus dem er entstand.
    ///
    /// <para><b>Warum ein Baum und nicht nur Text.</b> Die Oberfläche (Etappe E2,
    /// UI-Teil) zeichnet ihn als RAZOR-ELEMENTE und nicht als <c>MarkupString</c>:
    /// Blazor tauscht dann bei einer Legendenwahl das eine Attribut statt 150 KB
    /// Markup. <see cref="SvgSchreiber.Text"/> ist die zweite Ausgabe desselben
    /// Baums — für Prüfstand, Datei und Zwischenablage.</para>
    ///
    /// <para>Kein Namensraum-Zauber: Der Namensraum steht als gewöhnliches Attribut
    /// am äußeren <c>&lt;svg&gt;</c>.</para>
    /// </summary>
    public sealed class SvgKnoten
    {
        private readonly List<KeyValuePair<string, string>> _attribute =
            new List<KeyValuePair<string, string>>();
        private readonly List<SvgKnoten> _kinder = new List<SvgKnoten>();

        public SvgKnoten(string name, string marke = null, string inhalt = null)
        {
            Name = name;
            Marke = marke;
            Inhalt = inhalt;
        }

        /// <summary>Der Elementname ohne spitze Klammern (<c>svg</c>, <c>line</c>, …).</summary>
        public string Name { get; }

        /// <summary>Die Attribute in der Reihenfolge, in der sie gesetzt wurden.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Attribute => _attribute;

        /// <summary>Die Kinder in Zeichenreihenfolge.</summary>
        public IReadOnlyList<SvgKnoten> Kinder => _kinder;

        /// <summary>Der Textinhalt (nur <c>&lt;text&gt;</c>); sonst <c>null</c>.</summary>
        public string Inhalt { get; }

        /// <summary>Die Marke des Befehls, aus dem der Knoten entstand; <c>null</c> = keine.</summary>
        public string Marke { get; }

        /// <summary>
        /// Der fertig formatierte WERT des Befehls (DG-E3-6); <c>null</c> = keiner.
        ///
        /// <para>Er steht auch als Attribut <c>data-wert</c> im Baum. Die Eigenschaft
        /// daneben erspart der Oberfläche die Attributsuche: Die Zeigerzeile der Bilder
        /// ohne Zeichenfläche (DG-E3-10) liest je Knoten genau diesen Wert.</para>
        /// </summary>
        public string Wert { get; private set; }

        /// <summary>
        /// Den Wert setzen — genau dort, wo auch <c>data-wert</c> entsteht
        /// (<see cref="SvgSchreiber"/>).
        /// </summary>
        internal SvgKnoten MitWert(string wert)
        {
            Wert = wert;
            return this;
        }

        /// <summary>Ein Attribut anhängen; <c>null</c> als Wert lässt es weg.</summary>
        public SvgKnoten Attribut(string name, string wert)
        {
            if (wert != null) _attribute.Add(new KeyValuePair<string, string>(name, wert));
            return this;
        }

        /// <summary>Ein Kind anhängen.</summary>
        public SvgKnoten Fuege(SvgKnoten kind)
        {
            if (kind != null) _kinder.Add(kind);
            return this;
        }

        /// <summary>Alle Knoten des Teilbaums, dieser zuerst — für Prüfungen und Zählungen.</summary>
        public IEnumerable<SvgKnoten> Alle()
        {
            yield return this;
            foreach (SvgKnoten k in _kinder)
                foreach (SvgKnoten e in k.Alle())
                    yield return e;
        }
    }

    /// <summary>
    /// Schreibt ein <see cref="Zeichenmodell"/> als SVG — als Baum
    /// (<see cref="Baum"/>) oder als Text (<see cref="Text"/>).
    /// </summary>
    public static class SvgSchreiber
    {
        private static readonly CultureInfo INV = CultureInfo.InvariantCulture;

        /// <summary>
        /// Die Schriftkette des Bildschirms — dieselbe Reihenfolge, in der
        /// <c>Schriftkette.ERSATZSCHRIFTEN</c> die Schrift des PNG sucht.
        /// </summary>
        public const string SCHRIFTKETTE = "Calibri, Carlito, 'Liberation Sans', sans-serif";

        /// <summary>Der Klassenname eines Reihenpfads — der Griff der Oberfläche.</summary>
        public const string KLASSE_REIHE = "epos-reihe";

        /// <summary>Der Klassenname des inneren <c>&lt;svg&gt;</c> in Datenkoordinaten.</summary>
        public const string KLASSE_FLAECHE = "epos-flaeche";

        // =====================================================================
        // Der Baum
        // =====================================================================

        /// <summary>
        /// Das Modell als SVG-Baum.
        /// </summary>
        /// <param name="modell">Das fertige Zeichenmodell.</param>
        /// <param name="palette">
        /// Die Palette, gegen die die Farbrollen aufgelöst werden; <c>null</c> nimmt
        /// <see cref="Farbpalette.Aktuell"/>. <b>Aufgelöst wird beim SCHREIBEN</b> —
        /// genau wie beim Malen, deshalb trägt der Bildschirm dieselben Farben wie
        /// der Bericht.
        /// </param>
        /// <param name="kennung">
        /// Der Namensvorsatz der <c>clipPath</c>-Kennungen. Stehen zwei Bilder auf
        /// einer Seite, müssen sich ihre Kennungen unterscheiden — sonst schneidet
        /// das eine am Rechteck des anderen.
        /// </param>
        public static SvgKnoten Baum(Zeichenmodell modell, Farbpalette palette = null,
                                     string kennung = "d")
            => Baum(modell, palette, kennung, datenflaeche: true);

        /// <summary>
        /// <b>Das Modell als SVG für den DRUCK</b> (Wortbericht, Berichtsvorlagen) — die
        /// Reihen stehen als die Pixelpfade, die auch das PNG malt, nicht im inneren
        /// <c>&lt;svg&gt;</c> in Datenkoordinaten.
        ///
        /// <para><b>Warum.</b> Das innere <c>&lt;svg&gt;</c> dehnt seine viewBox mit
        /// <c>preserveAspectRatio="none"</c> ungleich auf die Zeichenfläche (beim
        /// Barwertverlauf 20 Jahre auf rund 1 000 Bildpunkte, Faktor 50 waagerecht) und hält
        /// die Strichstärke nur über <c>vector-effect="non-scaling-stroke"</c>. Der
        /// SVG-Leser von Word kennt diese Eigenschaft nicht: Er dehnt Strich und
        /// Strichfolge mit, die Linien werden zu breiten, gestreiften Bändern. Der
        /// Druck braucht weder Zoom noch Reihengriff, also nimmt er die Pixelpfade —
        /// deckungsgleich mit dem PNG-Rückfall.</para>
        /// </summary>
        public static SvgKnoten Druckbaum(Zeichenmodell modell, Farbpalette palette = null,
                                          string kennung = "d")
            => Baum(modell, palette, kennung, datenflaeche: false);

        private static SvgKnoten Baum(Zeichenmodell modell, Farbpalette palette,
                                      string kennung, bool datenflaeche)
        {
            if (modell == null) throw new ArgumentNullException(nameof(modell));
            palette = palette ?? Farbpalette.Aktuell;
            if (string.IsNullOrEmpty(kennung)) kennung = "d";

            var wurzel = new SvgKnoten("svg")
                .Attribut("xmlns", "http://www.w3.org/2000/svg")
                .Attribut("width", Px(modell.Breite))
                .Attribut("height", Px(modell.Hoehe))
                .Attribut("viewBox", "0 0 " + Px(modell.Breite) + " " + Px(modell.Hoehe))
                .Attribut("font-family", SCHRIFTKETTE);

            var defs = new SvgKnoten("defs");
            var inhalt = new List<SvgKnoten>();

            // Der Hintergrund als Rechteck: Ein <svg> ist durchsichtig, das PNG nicht.
            inhalt.Add(new SvgKnoten("rect")
                .Attribut("x", "0").Attribut("y", "0")
                .Attribut("width", Px(modell.Breite))
                .Attribut("height", Px(modell.Hoehe))
                .Attribut("fill", Hex(palette, modell.Hintergrund))
                .Attribut("fill-opacity", Deckung(palette, modell.Hintergrund)));

            var lage = new Lage(modell, palette, kennung, defs, datenflaeche);
            Schreibe(modell.Befehle, inhalt, lage);

            if (defs.Kinder.Count > 0) wurzel.Fuege(defs);
            foreach (SvgKnoten k in inhalt) wurzel.Fuege(k);
            return wurzel;
        }

        /// <summary>
        /// Dasselbe als Text — deterministisch, mit LF-Zeilenenden und ohne
        /// Zeitangabe. Zweimal gerufen ergibt denselben Text, Byte für Byte.
        /// </summary>
        public static string Text(Zeichenmodell modell, Farbpalette palette = null,
                                  string kennung = "d")
        {
            var sb = new StringBuilder(64 * 1024);
            Schreibe(sb, Baum(modell, palette, kennung));
            return sb.ToString();
        }

        /// <summary>
        /// <see cref="Druckbaum"/> als Text — der SVG-Teil des Wortberichts. Dieselbe
        /// Serialisierung, deterministisch wie <see cref="Text(Zeichenmodell, Farbpalette, string)"/>.
        /// </summary>
        public static string Drucktext(Zeichenmodell modell, Farbpalette palette = null,
                                       string kennung = "d")
        {
            var sb = new StringBuilder(64 * 1024);
            Schreibe(sb, Druckbaum(modell, palette, kennung));
            return sb.ToString();
        }

        /// <summary>Einen fertigen Baum als Text — dieselbe Serialisierung.</summary>
        public static string Text(SvgKnoten knoten)
        {
            if (knoten == null) return "";
            var sb = new StringBuilder(4096);
            Schreibe(sb, knoten);
            return sb.ToString();
        }

        // =====================================================================
        // Befehl fuer Befehl
        // =====================================================================

        /// <summary>Was ein Schreiblauf mitführt — Palette, Kennung, Zähler, Fläche.</summary>
        private sealed class Lage
        {
            public Lage(Zeichenmodell modell, Farbpalette palette, string kennung, SvgKnoten defs,
                        bool datenflaeche)
            {
                Modell = modell;
                Palette = palette;
                Kennung = kennung;
                Defs = defs;
                // Die Reihen wandern nur dann ins innere svg, wenn es eines geben KANN —
                // und nie im Druck (Druckbaum): Dort bleiben es die Pixelpfade des PNG.
                ReihenAlsFlaeche = datenflaeche && modell.Flaeche != null && modell.Reihen.Count > 0;
            }

            public Zeichenmodell Modell { get; }
            public Farbpalette Palette { get; }
            public string Kennung { get; }
            public SvgKnoten Defs { get; }
            public bool ReihenAlsFlaeche { get; }
            public bool FlaecheGesetzt { get; set; }
            public int Zuschnitte { get; set; }
        }

        private static void Schreibe(IReadOnlyList<Zeichenbefehl> befehle,
                                     List<SvgKnoten> ziel, Lage lage)
        {
            if (befehle == null) return;
            for (int i = 0; i < befehle.Count; i++)
            {
                Zeichenbefehl b = befehle[i];
                if (b == null) continue;

                // DG-E2-2: Der gekuerzte Pixelpfad einer Reihe bleibt dem PNG. An der
                // Stelle des ERSTEN solchen Befehls steht das innere svg.
                if (lage.ReihenAlsFlaeche && IstReihe(b.Marke))
                {
                    if (!lage.FlaecheGesetzt)
                    {
                        lage.FlaecheGesetzt = true;
                        ziel.Add(Flaechensvg(lage));
                    }
                    continue;
                }

                SvgKnoten k = Knoten(b, lage);
                if (k != null) ziel.Add(k);
            }
        }

        private static bool IstReihe(string marke)
            => marke != null && marke.StartsWith("reihe:", StringComparison.Ordinal);

        private static SvgKnoten Knoten(Zeichenbefehl befehl, Lage lage)
        {
            Farbpalette p = lage.Palette;

            switch (befehl)
            {
                case Linie l:
                    return Marke(new SvgKnoten("line", l.Marke)
                        .Attribut("x1", Px(l.X1)).Attribut("y1", Px(l.Y1))
                        .Attribut("x2", Px(l.X2)).Attribut("y2", Px(l.Y2)), l.Marke, l.Wert)
                        .Anhaengen(Stiftattribute(l.Stift, p));

                case Rechteck r:
                    return Marke(new SvgKnoten("rect", r.Marke)
                        .Attribut("x", Px(r.X)).Attribut("y", Px(r.Y))
                        .Attribut("width", Px(r.Breite)).Attribut("height", Px(r.Hoehe)),
                        r.Marke, r.Wert)
                        .Anhaengen(Fuellattribute(r.Fuellung, p))
                        .Anhaengen(Stiftattribute(r.Rand, p));

                case Kreis k:
                    return Marke(new SvgKnoten("circle", k.Marke)
                        .Attribut("cx", Px(k.X)).Attribut("cy", Px(k.Y))
                        .Attribut("r", Px(k.Radius)), k.Marke, k.Wert)
                        .Anhaengen(Fuellattribute(k.Fuellung, p))
                        .Anhaengen(Stiftattribute(k.Rand, p));

                case Ellipse e:
                    return Ellipsenknoten(e.Marke, e.Wert, e.X, e.Y, e.Breite, e.Hoehe,
                                          e.Fuellung, e.Rand, p);

                case Kreissegment s:
                    {
                        // Der VOLLKREIS ist ein eigener Fall - dieselbe Regel wie im
                        // Maler (Befund zu Auftrag #222): Ein Bogen ueber 360 Grad zieht
                        // nichts, Anfang und Ende fallen zusammen.
                        if (Math.Abs(s.Winkel) >= 360f)
                            return Ellipsenknoten(s.Marke, s.Wert, s.X, s.Y, s.Breite, s.Hoehe,
                                                  s.Fuellung, s.Rand, p);

                        return Marke(new SvgKnoten("path", s.Marke)
                            .Attribut("d", Bogen(s)), s.Marke, s.Wert)
                            .Anhaengen(Fuellattribute(s.Fuellung, p))
                            .Anhaengen(Stiftattribute(s.Rand, p));
                    }

                case Pfad f:
                    {
                        if (f.Punkte == null || f.Punkte.Count < 2) return null;
                        return Marke(new SvgKnoten("path", f.Marke)
                            .Attribut("d", Streckenzug(f)), f.Marke, f.Wert)
                            .Anhaengen(Fuellattribute(f.Fuellung, p))
                            .Anhaengen(Stiftattribute(f.Rand, p));
                    }

                case Zeichnung.Text t:
                    {
                        if (string.IsNullOrEmpty(t.Inhalt)) return null;

                        // DIE KOORDINATEN GEHEN UNVERAENDERT DURCH - beide, x wie y. Der
                        // Renderer hat sie fertig gerechnet und gibt sie dem Maler
                        // genauso; verschoben wird erst im AUSGABEWEG, und jeder auf
                        // seine Art:
                        //
                        //   x: Der Maler zieht die gemessene Breite ab, das SVG setzt
                        //      text-anchor - der Browser misst selbst.
                        //   y: Der Befehl nennt die linke OBERE Ecke (so haelt es das
                        //      Modell, und so rechnen die 46 Beschriftungen des
                        //      Bestands - eine y-Beschriftung steht auf
                        //      y - Zeilenhoehe/2). Der Maler zieht dafuer den Aufstieg
                        //      ab, das SVG setzt dominant-baseline="text-before-edge".
                        //      Eine Umrechnung hier braeuchte die Schriftmetrik und
                        //      damit SkiaSharp im Ausgabeweg - genau das soll nicht
                        //      sein.
                        //
                        // Die Groesse steht in BILDPUNKTEN (pt x 96/72), derselben
                        // Umrechnung, die Schriftkette.Erzeuge fuer Skia macht.
                        return Marke(new SvgKnoten("text", t.Marke, t.Inhalt)
                            .Attribut("x", Px(t.X)).Attribut("y", Px(t.Y))
                            .Attribut("font-size", Px(t.Schrift.Punkt * 96f / 72f) + "px")
                            .Attribut("font-weight", t.Schrift.Fett ? "bold" : null)
                            .Attribut("font-style", t.Schrift.Kursiv ? "italic" : null)
                            .Attribut("text-anchor", Anker(t.Ausrichtung))
                            .Attribut("dominant-baseline", "text-before-edge")
                            .Attribut("fill", Hex(p, t.Ton))
                            .Attribut("fill-opacity", Deckung(p, t.Ton)), t.Marke, t.Wert);
                    }

                case Gruppe g:
                    {
                        var knoten = Marke(new SvgKnoten("g", g.Marke), g.Marke, g.Wert);
                        if (g.Zuschnitt.HasValue)
                        {
                            lage.Zuschnitte++;
                            string id = lage.Kennung + "-c" + lage.Zuschnitte.ToString(INV);
                            Rahmen z = g.Zuschnitt.Value;
                            lage.Defs.Fuege(new SvgKnoten("clipPath")
                                .Attribut("id", id)
                                .Fuege(new SvgKnoten("rect")
                                    .Attribut("x", Px(z.X)).Attribut("y", Px(z.Y))
                                    .Attribut("width", Px(z.Breite)).Attribut("height", Px(z.Hoehe))));
                            knoten.Attribut("clip-path", "url(#" + id + ")");
                        }

                        var kinder = new List<SvgKnoten>();
                        Schreibe(g.Befehle, kinder, lage);
                        foreach (SvgKnoten k in kinder) knoten.Fuege(k);
                        return knoten;
                    }
            }
            return null;
        }

        private static SvgKnoten Ellipsenknoten(string marke, string wert, float x, float y,
                                                float breite, float hoehe,
                                                Fuellung fuellung, Stift rand, Farbpalette p)
        {
            return Marke(new SvgKnoten("ellipse", marke)
                .Attribut("cx", Px(x + breite / 2f)).Attribut("cy", Px(y + hoehe / 2f))
                .Attribut("rx", Px(breite / 2f)).Attribut("ry", Px(hoehe / 2f)), marke, wert)
                .Anhaengen(Fuellattribute(fuellung, p))
                .Anhaengen(Stiftattribute(rand, p));
        }

        /// <summary>
        /// Die Marke als <c>data-marke</c> und — seit DG-E3-6 — der Wert des Befehls
        /// als <c>data-wert</c> AN DERSELBEN STELLE. Ohne Marke bzw. ohne Wert bleibt
        /// das jeweilige Attribut weg; ein Befehl des Bestands schreibt sich damit
        /// wörtlich wie zuvor.
        /// </summary>
        private static SvgKnoten Marke(SvgKnoten knoten, string marke, string wert)
            => knoten.MitWert(wert)
                     .Attribut("data-marke", marke).Attribut("data-wert", wert);

        // =====================================================================
        // Die Zeichenflaeche: inneres svg in Datenkoordinaten
        // =====================================================================

        private static SvgKnoten Flaechensvg(Lage lage)
        {
            Zeichenflaeche fl = lage.Modell.Flaeche;
            Rahmen bild = fl.Bild;
            Datenfenster d = fl.Daten;

            double breite = d.XBis - d.XVon;
            if (breite <= 0) breite = 1;

            // DG-E3-1: Waagerecht Stunden, SENKRECHT BILDPUNKTE. Die viewBox ist
            // deshalb "XVon 0 Breite Bild.Hoehe"; jede Reihe rechnet ihren Wert ueber
            // IHR Fenster in Bildpunkte um. Erst damit tragen zwei Achsen dasselbe
            // innere svg - der Speicherinhalt rechts hat seine eigene Skala und steht
            // trotzdem an der Stelle, an der ihn das PNG zeichnet.
            //
            // Die y-UMKEHR steht als RECHNUNG im Pfad, nicht als transform: Dann
            // beginnt die viewBox senkrecht bei 0, und ein Zoom auf der Zeitachse
            // aendert an ihr nur x und Breite - eine Attributaenderung.
            var knoten = new SvgKnoten("svg")
                .Attribut("class", KLASSE_FLAECHE)
                .Attribut("x", Px(bild.X)).Attribut("y", Px(bild.Y))
                .Attribut("width", Px(bild.Breite)).Attribut("height", Px(bild.Hoehe))
                .Attribut("viewBox", Wert(d.XVon) + " 0 " + Wert(breite) + " " + Px(bild.Hoehe))
                .Attribut("preserveAspectRatio", "none");

            int reihen = lage.Modell.Reihen.Count;

            foreach (Datenreihe r in lage.Modell.Reihen)
            {
                int n = r.Werte == null ? 0 : r.Werte.Length;
                if (n < 2) continue;

                bool flaeche = r.Art == Reihenart.Flaeche;
                bool punkte = r.Art == Reihenart.Punkte;
                Farbton strich = flaeche ? r.Randton : r.Ton;
                string marke = "reihe:" + (r.Name ?? "");

                var pfad = new SvgKnoten("path", marke)
                    .Attribut("class", KLASSE_REIHE)
                    .Attribut("data-reihe", r.Name ?? "")
                    .Attribut("data-marke", marke)
                    .Attribut("fill", flaeche ? Hex(lage.Palette, r.Ton) : "none")
                    .Attribut("fill-opacity", flaeche ? Deckung(lage.Palette, r.Ton) : null)
                    .Attribut("stroke", strich == null ? "none" : Hex(lage.Palette, strich))
                    .Attribut("stroke-opacity", strich == null
                        ? null : Deckung(lage.Palette, strich))
                    .Attribut("stroke-width", strich == null ? null : Px(r.Staerke))
                    // DG-E3-5: Erst die RUNDE Kappe macht aus einer Strecke der Laenge
                    // null einen Punkt; ohne sie zeichnet der Browser nichts.
                    .Attribut("stroke-linecap", punkte ? "round" : null)
                    .Attribut("stroke-dasharray", r.Muster == null
                        ? null
                        : Px(r.Muster.Strich) + " " + Px(r.Muster.Luecke))
                    .Attribut("vector-effect", "non-scaling-stroke")
                    .Attribut("d", Reihenpfad(r, fl, Pfadregel.Roh(n, reihen)));
                knoten.Fuege(pfad);
            }
            return knoten;
        }

        // =====================================================================
        // Der Reihenpfad - oeffentlich (Entscheid DG-E3-3)
        // =====================================================================

        /// <summary>
        /// Der Pfad EINER Reihe in den Koordinaten des inneren <c>&lt;svg&gt;</c>:
        /// waagerecht Stunden (bzw. Stützstellen), senkrecht BILDPUNKTE der
        /// Zeichenfläche (DG-E3-1). Roh heißt jede Stützstelle, sonst wird nach
        /// <see cref="Pfadregel"/> gebündelt; eine <see cref="Reihenart.Flaeche"/>
        /// wird ein geschlossener Zug (Oberkante vorwärts, Unterkante rückwärts), eine
        /// <see cref="Reihenart.Punkte"/> eine ungebündelte Folge von
        /// <c>M x,y h 0</c> (DG-E3-5).
        ///
        /// <para>Geklemmt wird NICHT — das innere <c>&lt;svg&gt;</c> schneidet selbst
        /// ab, und ein geklemmter Wert wäre beim Zoom verloren.</para>
        /// </summary>
        public static string Reihenpfad(Datenreihe reihe, Zeichenflaeche flaeche, bool roh)
        {
            Datenfenster rf = Reihenfenster(reihe, flaeche);
            return rf == null ? "" : Reihenpfad(reihe, flaeche, rf.XVon, rf.XBis, roh);
        }

        /// <summary>
        /// <b>Derselbe Pfad für einen AUSSCHNITT (Entscheid DG-E3-3).</b>
        /// <paramref name="von"/> und <paramref name="bis"/> stehen in der Einheit der
        /// x-Achse (Jahresstunde bzw. Index); gezeichnet wird jede Stützstelle, die
        /// darin liegt.
        ///
        /// <para><b>Wofür.</b> Zoomt der Anwender einen GEBÜNDELTEN Pfad über etwa das
        /// Vierfache, zeigt die Bündelung nicht mehr die echte Stützstelle. Der
        /// Baustein rechnet den sichtbaren Ausschnitt dann mit
        /// <c>roh = true</c> NACH — aus den Datenreihen des Modells, die er ohnehin
        /// hält, also ohne einen Rundlauf in den Kern (DG-E2-4 wird so eingelöst).</para>
        ///
        /// <para>Der Fensterpfad einer rohen Reihe ist deshalb genau der Ausschnitt
        /// des Vollpfads: dieselben Punkte, dieselben Zahlen.</para>
        /// </summary>
        public static string Reihenpfad(Datenreihe reihe, Zeichenflaeche flaeche,
                                        double von, double bis, bool roh)
        {
            Datenfenster rf = Reihenfenster(reihe, flaeche);
            if (rf == null) return "";

            int n = reihe.Werte.Length;
            if (n < 2) return "";

            double hoehe = flaeche.Bild.Hoehe;
            double spanne = rf.YBis - rf.YVon;
            if (spanne <= 0) spanne = 1;
            double schritt = (rf.XBis - rf.XVon) / (n - 1);

            // DG-E3-5: Eine PUNKTWOLKE kennt weder Indexgrenzen noch Buendelung - jeder
            // Punkt steht fuer sich an seiner eigenen x-Stelle.
            if (reihe.Art == Reihenart.Punkte)
                return Punktwolke(reihe, rf, schritt, von, bis, hoehe, spanne);

            // Die Indexgrenzen des Ausschnitts. Ohne Schrittweite (eine Reihe ohne
            // x-Ausdehnung) bleibt es bei der ganzen Reihe.
            int ab = 0, biss = n - 1;
            if (reihe.XWerte != null)
            {
                // DG-E3-5: Eine Reihe mit EIGENEN x-Stellen traegt ihre Grenzen nicht im
                // Index - sie werden gesucht. Die Stellen stehen in Zeichenreihenfolge,
                // der Treffer ist deshalb ein zusammenhaengender Bereich.
                ab = n; biss = -1;
                for (int i = 0; i < n; i++)
                {
                    double x = XStelle(reihe, rf, schritt, i);
                    if (double.IsNaN(x) || x < von - 1e-9 || x > bis + 1e-9) continue;
                    if (i < ab) ab = i;
                    if (i > biss) biss = i;
                }
            }
            else if (schritt > 0)
            {
                ab = (int)Math.Ceiling((von - rf.XVon) / schritt - 1e-9);
                biss = (int)Math.Floor((bis - rf.XVon) / schritt + 1e-9);
                if (ab < 0) ab = 0;
                if (biss > n - 1) biss = n - 1;
            }
            if (biss < ab) return "";

            int laenge = biss - ab + 1;
            int spalten = (int)Math.Max(1.0, Math.Round(flaeche.Bild.Breite));
            var sb = new StringBuilder(laenge * 12 + 16);

            if (reihe.Art == Reihenart.Flaeche)
            {
                Flaechenzug(sb, reihe, rf, ab, laenge, roh, spalten, hoehe, spanne);
                return sb.ToString();
            }

            // Anlagenkopplung AK1 Welle 3: Eine Linie mit nicht endlichen Werten (einer LÜCKE - etwa
            // der Vorlauf jenseits der Heizgrenze) bricht dort ab und setzt beim nächsten endlichen
            // Wert mit eigenem "M" neu an. Eine Reihe ohne Lücke nimmt den Weg darunter, wörtlich wie
            // bisher - ein "NaN" im Pfad machte den ganzen Pfad ungültig.
            if (HatLuecke(reihe.Werte, ab, biss))
            {
                Lueckenzug(sb, reihe, rf, schritt, ab, biss, laenge, roh, spalten, hoehe, spanne);
                return sb.ToString();
            }

            if (roh)
            {
                for (int i = ab; i <= biss; i++)
                    Punkt(sb, i == ab, XStelle(reihe, rf, schritt, i),
                          Bildpunkt(reihe.Werte[i], rf, hoehe, spanne));
                return sb.ToString();
            }

            IReadOnlyList<Punkt> gebuendelt =
                Pfadregel.Gebuendelt(Teil(reihe.Werte, ab, laenge), spalten);
            for (int i = 0; i < gebuendelt.Count; i++)
                Punkt(sb, i == 0, XStelle(reihe, rf, schritt, ab + gebuendelt[i].X),
                      Bildpunkt(gebuendelt[i].Y, rf, hoehe, spanne));
            return sb.ToString();
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        /// <summary>Trägt der Ausschnitt <paramref name="ab"/> … <paramref name="bis"/> einen nicht endlichen Wert?</summary>
        private static bool HatLuecke(double[] werte, int ab, int bis)
        {
            for (int i = ab; i <= bis; i++)
                if (!Endlich(werte[i])) return true;
            return false;
        }

        /// <summary>
        /// <b>Der Pfad einer Linie mit Lücken</b> (AK1 Welle 3): je zusammenhängendem Stück endlicher
        /// Werte ein Teilpfad mit eigenem <c>M</c> — roh jede Stützstelle, sonst nach
        /// <see cref="Pfadregel"/> gebündelt, mit den Bildpunktspalten im Verhältnis der Stücklänge.
        /// Ein Stück aus einem einzigen Wert ist ein bloßes <c>M x,y</c> — gültig, aber ohne Strich.
        /// </summary>
        private static void Lueckenzug(StringBuilder sb, Datenreihe reihe, Datenfenster rf, double schritt,
                                       int ab, int bis, int laenge, bool roh, int spalten,
                                       double hoehe, double spanne)
        {
            int i = ab;
            while (i <= bis)
            {
                while (i <= bis && !Endlich(reihe.Werte[i])) i++;
                if (i > bis) break;
                int start = i;
                while (i <= bis && Endlich(reihe.Werte[i])) i++;
                int stueck = i - start;

                if (sb.Length > 0) sb.Append(' ');
                if (stueck == 1)
                {
                    sb.Append("M ").Append(Wert(XStelle(reihe, rf, schritt, start))).Append(',')
                      .Append(Px(Bildpunkt(reihe.Werte[start], rf, hoehe, spanne)));
                    continue;
                }

                if (roh)
                {
                    for (int k = start; k < start + stueck; k++)
                        Punkt(sb, k == start, XStelle(reihe, rf, schritt, k),
                              Bildpunkt(reihe.Werte[k], rf, hoehe, spanne));
                    continue;
                }

                int teilspalten = (int)Math.Max(1.0, Math.Round((double)spalten * stueck / Math.Max(1, laenge)));
                IReadOnlyList<Punkt> gebuendelt = Pfadregel.Gebuendelt(Teil(reihe.Werte, start, stueck), teilspalten);
                if (gebuendelt.Count == 1)
                {
                    sb.Append("M ").Append(Wert(XStelle(reihe, rf, schritt, start + gebuendelt[0].X))).Append(',')
                      .Append(Px(Bildpunkt(gebuendelt[0].Y, rf, hoehe, spanne)));
                    continue;
                }
                for (int k = 0; k < gebuendelt.Count; k++)
                    Punkt(sb, k == 0, XStelle(reihe, rf, schritt, start + gebuendelt[k].X),
                          Bildpunkt(gebuendelt[k].Y, rf, hoehe, spanne));
            }
        }

        /// <summary>
        /// <b>Die Punktwolke (Entscheid DG-E3-5).</b> Je Wert ein Segment
        /// <c>M x,y h 0</c> — eine Strecke der Länge null, die erst die runde
        /// Strichkappe zum Punkt macht. Nicht endliche Werte und Punkte außerhalb des
        /// Ausschnitts fallen weg; gebündelt wird nicht.
        /// </summary>
        private static string Punktwolke(Datenreihe reihe, Datenfenster rf, double schritt,
                                         double von, double bis, double hoehe, double spanne)
        {
            int n = reihe.Werte.Length;
            var sb = new StringBuilder(n * 16 + 16);
            for (int i = 0; i < n; i++)
            {
                double w = reihe.Werte[i];
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;

                double x = XStelle(reihe, rf, schritt, i);
                if (double.IsNaN(x) || double.IsInfinity(x)) continue;
                if (x < von - 1e-9 || x > bis + 1e-9) continue;

                if (sb.Length > 0) sb.Append(' ');
                sb.Append("M ").Append(Wert(x)).Append(',')
                  .Append(Px(Bildpunkt(w, rf, hoehe, spanne))).Append(" h 0");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Die x-Stelle der <paramref name="i"/>-ten Stützstelle: ihr eigener Wert aus
        /// <see cref="Datenreihe.XWerte"/>, sonst die gleichmäßige Teilung des Fensters
        /// (DG-E3-5).
        /// </summary>
        private static double XStelle(Datenreihe reihe, Datenfenster rf, double schritt, double i)
            => reihe.XWerte != null && i >= 0 && i < reihe.XWerte.Length
                ? reihe.XWerte[(int)i]
                : rf.XVon + i * schritt;

        /// <summary>
        /// Der geschlossene Zug einer Fläche: die Oberkante vorwärts, dann die
        /// Unterkante rückwärts, dann <c>Z</c>. Fehlt <c>Unten</c>, ist die Unterkante
        /// die ACHSENNULL, in das Fenster der Reihe geklemmt — eine Fläche, deren
        /// Achse gar nicht bis null reicht, liefe sonst aus dem Bild.
        /// </summary>
        private static void Flaechenzug(StringBuilder sb, Datenreihe r, Datenfenster rf,
                                        int ab, int laenge, bool roh, int spalten,
                                        double hoehe, double spanne)
        {
            int n = r.Werte.Length;
            double schritt = (rf.XBis - rf.XVon) / (n - 1);
            double null0 = rf.YVon > 0 ? rf.YVon : rf.YBis < 0 ? rf.YBis : 0.0;

            double[] oben = Teil(r.Werte, ab, laenge);
            double[] unten = r.Unten == null
                ? Gleichwert(laenge, null0)
                : Teil(r.Unten, ab, laenge);

            IReadOnlyList<Punkt> kanteOben = roh
                ? Rohkante(oben)
                : Pfadregel.GebuendelteKante(oben, spalten, true);
            IReadOnlyList<Punkt> kanteUnten = roh
                ? Rohkante(unten)
                : Pfadregel.GebuendelteKante(unten, spalten, false);
            if (kanteOben.Count == 0 || kanteUnten.Count == 0) return;

            for (int i = 0; i < kanteOben.Count; i++)
                Punkt(sb, i == 0, XStelle(r, rf, schritt, ab + kanteOben[i].X),
                      Bildpunkt(kanteOben[i].Y, rf, hoehe, spanne));
            for (int i = kanteUnten.Count - 1; i >= 0; i--)
                Punkt(sb, false, XStelle(r, rf, schritt, ab + kanteUnten[i].X),
                      Bildpunkt(kanteUnten[i].Y, rf, hoehe, spanne));
            sb.Append(" Z");
        }

        private static IReadOnlyList<Punkt> Rohkante(double[] werte)
        {
            var punkte = new List<Punkt>(werte.Length);
            for (int i = 0; i < werte.Length; i++) punkte.Add(new Punkt(i, (float)werte[i]));
            return punkte;
        }

        private static double[] Teil(double[] werte, int ab, int laenge)
        {
            if (werte == null) return new double[laenge];
            var teil = new double[laenge];
            for (int i = 0; i < laenge && ab + i < werte.Length; i++) teil[i] = werte[ab + i];
            return teil;
        }

        private static double[] Gleichwert(int laenge, double wert)
        {
            var werte = new double[laenge];
            for (int i = 0; i < laenge; i++) werte[i] = wert;
            return werte;
        }

        /// <summary>
        /// Das eigene Fenster der Reihe, sonst das der Zeichenfläche (DG-E3-1);
        /// <c>null</c>, wenn die Reihe nichts zu zeichnen hat.
        /// </summary>
        private static Datenfenster Reihenfenster(Datenreihe reihe, Zeichenflaeche flaeche)
        {
            if (reihe == null || reihe.Werte == null || reihe.Werte.Length < 2) return null;
            if (flaeche == null) return null;
            return reihe.Fenster ?? flaeche.Daten;
        }

        /// <summary>
        /// Der Wert als BILDPUNKT der Zeichenfläche, von oben gezählt:
        /// <c>Hoehe − (Wert − YVon) / (YBis − YVon) · Hoehe</c> (DG-E3-1).
        /// </summary>
        private static double Bildpunkt(double wert, Datenfenster rf, double hoehe, double spanne)
            => hoehe - (wert - rf.YVon) / spanne * hoehe;

        private static void Punkt(StringBuilder sb, bool erster, double x, double y)
        {
            sb.Append(erster ? "M " : " ").Append(Wert(x)).Append(',').Append(Px(y));
            if (erster) sb.Append(" L");
        }

        // =====================================================================
        // Stift, Fuellung, Farbe
        // =====================================================================

        private static IEnumerable<KeyValuePair<string, string>> Stiftattribute(
            Stift stift, Farbpalette palette)
        {
            if (stift == null) yield break;

            yield return Paar("stroke", Hex(palette, stift.Ton));
            yield return Paar("stroke-opacity", Deckung(palette, stift.Ton));
            yield return Paar("stroke-width", Px(stift.Breite));
            // Nur, was vom SVG-Standard abweicht - genau wie der Maler nur setzt,
            // was vom Skia-Standard abweicht.
            if (stift.Kappe != Strichkappe.Stumpf) yield return Paar("stroke-linecap", Kappe(stift.Kappe));
            if (stift.Verbindung != Strichverbindung.Gehrung)
                yield return Paar("stroke-linejoin", Verbindung(stift.Verbindung));
            if (stift.Muster != null)
            {
                yield return Paar("stroke-dasharray",
                                  Px(stift.Muster.Strich) + " " + Px(stift.Muster.Luecke));
                if (stift.Muster.Versatz != 0f)
                    yield return Paar("stroke-dashoffset", Px(stift.Muster.Versatz));
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> Fuellattribute(
            Fuellung fuellung, Farbpalette palette)
        {
            // OHNE Fuellung steht fill="none": Die SVG-Vorgabe waere Schwarz, und ein
            // nur umrandetes Rechteck des Bestands stuende dann als schwarzer Block da.
            if (fuellung == null)
            {
                yield return Paar("fill", "none");
                yield break;
            }
            yield return Paar("fill", Hex(palette, fuellung.Ton));
            yield return Paar("fill-opacity", Deckung(palette, fuellung.Ton));
        }

        private static KeyValuePair<string, string> Paar(string name, string wert)
            => new KeyValuePair<string, string>(name, wert);

        /// <summary>Mehrere Attribute in der gegebenen Reihenfolge anhängen.</summary>
        private static SvgKnoten Anhaengen(this SvgKnoten knoten,
                                           IEnumerable<KeyValuePair<string, string>> attribute)
        {
            foreach (KeyValuePair<string, string> a in attribute) knoten.Attribut(a.Key, a.Value);
            return knoten;
        }

        /// <summary>Die Farbe des Tons als <c>#RRGGBB</c>, gegen DIESE Palette aufgelöst.</summary>
        private static string Hex(Farbpalette palette, Farbton ton)
            => Diagrammfarben.Hex(palette.Loese(ton));

        /// <summary>
        /// Die Deckung als Anteil — oder <c>null</c>, wenn die Farbe voll deckt. Die
        /// Hexschreibweise führt keine Deckung (sie gehört zum Bildaufbau, nicht zur
        /// Einstellung), deshalb steht sie als eigenes Attribut daneben.
        /// </summary>
        private static string Deckung(Farbpalette palette, Farbton ton)
        {
            byte a = palette.Loese(ton).A;
            return a == 255 ? null : (a / 255.0).ToString("0.###", INV);
        }

        private static string Kappe(Strichkappe k)
            => k == Strichkappe.Rund ? "round" : k == Strichkappe.Quadratisch ? "square" : "butt";

        private static string Verbindung(Strichverbindung v)
            => v == Strichverbindung.Rund ? "round" : v == Strichverbindung.Fase ? "bevel" : "miter";

        private static string Anker(Ausrichtung a)
            => a == Ausrichtung.Mitte ? "middle" : a == Ausrichtung.Rechts ? "end" : "start";

        // =====================================================================
        // Pfaddaten
        // =====================================================================

        private static string Streckenzug(Pfad f)
        {
            var sb = new StringBuilder(f.Punkte.Count * 12);
            for (int i = 0; i < f.Punkte.Count; i++)
            {
                sb.Append(i == 0 ? "M " : " ").Append(Px(f.Punkte[i].X))
                  .Append(',').Append(Px(f.Punkte[i].Y));
                if (i == 0) sb.Append(" L");
            }
            if (f.Geschlossen) sb.Append(" Z");
            return sb.ToString();
        }

        /// <summary>
        /// Das Tortenstück als Bogenpfad: vom Mittelpunkt zum Anfang, über den Bogen
        /// zum Ende, zurück. Die Winkel zählen wie in Skia — 0° nach rechts, positiv
        /// im Uhrzeigersinn (die y-Achse zeigt nach unten), deshalb ist das
        /// SVG-<c>sweep-flag</c> genau das Vorzeichen des Winkels.
        /// </summary>
        private static string Bogen(Kreissegment s)
        {
            double rx = s.Breite / 2.0, ry = s.Hoehe / 2.0;
            double cx = s.X + rx, cy = s.Y + ry;
            double a0 = s.Startwinkel * Math.PI / 180.0;
            double a1 = (s.Startwinkel + s.Winkel) * Math.PI / 180.0;

            double x0 = cx + rx * Math.Cos(a0), y0 = cy + ry * Math.Sin(a0);
            double x1 = cx + rx * Math.Cos(a1), y1 = cy + ry * Math.Sin(a1);
            int gross = Math.Abs(s.Winkel) > 180f ? 1 : 0;
            int richtung = s.Winkel >= 0f ? 1 : 0;

            return "M " + Px(cx) + "," + Px(cy) +
                   " L " + Px(x0) + "," + Px(y0) +
                   " A " + Px(rx) + "," + Px(ry) + " 0 " +
                   gross.ToString(INV) + "," + richtung.ToString(INV) + " " +
                   Px(x1) + "," + Px(y1) + " Z";
        }

        // =====================================================================
        // Zahlen und Text
        // =====================================================================

        /// <summary>Ein BILDPUNKT-Wert: zwei Nachkommastellen genügen dem Bildschirm.</summary>
        private static string Px(double wert) => wert.ToString("0.##", INV);

        /// <summary>Ein DATEN-Wert: drei Nachkommastellen, wie im Achsentext.</summary>
        private static string Wert(double wert) => wert.ToString("0.###", INV);

        private static void Schreibe(StringBuilder sb, SvgKnoten k)
        {
            sb.Append('<').Append(k.Name);
            foreach (KeyValuePair<string, string> a in k.Attribute)
                sb.Append(' ').Append(a.Key).Append("=\"").Append(Maskiert(a.Value, true)).Append('"');

            if (k.Kinder.Count == 0 && string.IsNullOrEmpty(k.Inhalt))
            {
                sb.Append("/>\n");
                return;
            }

            sb.Append('>');
            if (!string.IsNullOrEmpty(k.Inhalt)) sb.Append(Maskiert(k.Inhalt, false));
            if (k.Kinder.Count > 0)
            {
                sb.Append('\n');
                foreach (SvgKnoten kind in k.Kinder) Schreibe(sb, kind);
            }
            sb.Append("</").Append(k.Name).Append(">\n");
        }

        /// <summary>
        /// Die Zeichen, die in XML nicht roh stehen dürfen. Ein Reihenname kommt aus
        /// den Daten des Anwenders — ein <c>&amp;</c> darin zerbräche sonst den Baum.
        ///
        /// <para>Das Hochkomma bleibt stehen: Attributwerte stehen in
        /// Anführungszeichen, und die Schriftkette führt es (<c>'Liberation Sans'</c>)
        /// als Teil ihres CSS-Werts.</para>
        /// </summary>
        private static string Maskiert(string text, bool imAttribut)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length + 8);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append(imAttribut ? "&quot;" : "\""); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
