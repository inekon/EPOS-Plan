using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der zweite Ausgabeweg</b> (Konzept Diagramme, Etappe E2): Derselbe
    /// <c>Zeichenmodell</c>-Baum, den <c>SkiaMaler</c> ins PNG malt, wird hier zu
    /// SVG-Text.
    ///
    /// <para>Geprueft wird, was den Weg tragfaehig macht: der Aufbau des Baums, die
    /// Uebersetzung jedes Primitivs samt Stift und Fuellung, der Zuschnitt als
    /// <c>clipPath</c> mit eigener Kennung, die Aufloesung der Farbrollen gegen die
    /// UEBERGEBENE Palette, der Determinismus des Textes — und vor allem das INNERE
    /// <c>&lt;svg&gt;</c>: Die Reihen stehen dort in Datenkoordinaten, mit umgekehrter
    /// y-Achse, waehrend der gekuerzte Pixelpfad des PNG wegfaellt (Entscheid
    /// DG-E2-2).</para>
    ///
    /// <para>Alle Zahlen im Text stehen in <c>InvariantCulture</c>; die Faelle sind
    /// deshalb kulturfest. Wo die Kultur doch zaehlen koennte, pinnt die
    /// <see cref="Kulturvorrichtung"/> sie auf <c>de-DE</c> — dieselbe Vorrichtung wie
    /// in den Nachbartests.</para>
    /// </summary>
    public sealed class SvgSchreiberTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static Zeichenmodell Modell(int breite = 100, int hoehe = 50)
            => new Zeichenmodell(breite, hoehe, Farbton.Aus(Farbrolle.HINTERGRUND));

        private static Stift Strich(Farbrolle rolle = null, float breite = 1f)
            => new Stift(Farbton.Aus(rolle ?? Farbrolle.ACHSE), breite);

        /// <summary>Der Wert eines Attributs oder <c>null</c>.</summary>
        private static string Wert(SvgKnoten knoten, string name)
        {
            foreach (KeyValuePair<string, string> a in knoten.Attribute)
                if (a.Key == name) return a.Value;
            return null;
        }

        private static string Text(Zeichenmodell m, Farbpalette p = null, string kennung = "d")
            => SvgSchreiber.Text(m, p ?? Farbpalette.Vorgabe, kennung);

        // =====================================================================
        // 1 — Aufbau
        // =====================================================================

        /// <summary>
        /// Das aeussere <c>&lt;svg&gt;</c> traegt Namensraum, Mass, <c>viewBox</c> und
        /// die Schriftkette; darunter steht als erstes der HINTERGRUND. Ein
        /// <c>&lt;svg&gt;</c> ist durchsichtig, ein PNG nicht — ohne das Rechteck
        /// saehe der Bildschirm ein anderes Bild als der Bericht.
        /// </summary>
        [Fact]
        public void DerBaumBeginntMitSvgUndHintergrund()
        {
            SvgKnoten w = SvgSchreiber.Baum(Modell(), Farbpalette.Vorgabe);

            Assert.Equal("svg", w.Name);
            Assert.Equal("http://www.w3.org/2000/svg", Wert(w, "xmlns"));
            Assert.Equal("100", Wert(w, "width"));
            Assert.Equal("50", Wert(w, "height"));
            Assert.Equal("0 0 100 50", Wert(w, "viewBox"));
            Assert.Contains("Calibri", Wert(w, "font-family"));

            SvgKnoten hintergrund = w.Kinder[0];
            Assert.Equal("rect", hintergrund.Name);
            Assert.Equal("#FFFFFF", Wert(hintergrund, "fill"));
            Assert.Equal("100", Wert(hintergrund, "width"));
        }

        /// <summary>Jedes Primitiv bekommt sein Element und seine Koordinaten.</summary>
        [Fact]
        public void JedesPrimitivWirdZuSeinemElement()
        {
            Zeichenmodell m = Modell();
            m.Linie(1f, 2f, 3f, 4f, Strich());
            m.Rechteck(5f, 6f, 7f, 8f, Strich());
            m.Kreis(9f, 10f, 11f, Strich());
            m.Ellipse(0f, 0f, 40f, 20f, Strich());
            m.Pfad(new[] { new Punkt(1f, 1f), new Punkt(2f, 3f) }, false, Strich());

            string t = Text(m);

            Assert.Contains("<line x1=\"1\" y1=\"2\" x2=\"3\" y2=\"4\"", t);
            Assert.Contains("<rect x=\"5\" y=\"6\" width=\"7\" height=\"8\"", t);
            Assert.Contains("<circle cx=\"9\" cy=\"10\" r=\"11\"", t);
            Assert.Contains("<ellipse cx=\"20\" cy=\"10\" rx=\"20\" ry=\"10\"", t);
            Assert.Contains("<path d=\"M 1,1 L 2,3\"", t);
        }

        /// <summary>
        /// Ein geschlossener Pfad endet mit <c>Z</c> — das Vieleck des Bestands
        /// (Flaechen der Stapelbilder) haengt daran.
        /// </summary>
        [Fact]
        public void EinGeschlossenerPfadEndetMitZ()
        {
            Zeichenmodell m = Modell();
            m.Pfad(new[] { new Punkt(0f, 0f), new Punkt(10f, 0f), new Punkt(10f, 10f) },
                   true, null, new Fuellung(Farbton.Aus(Farbrolle.SERIE_1)));

            Assert.Contains("d=\"M 0,0 L 10,0 10,10 Z\"", Text(m));
        }

        /// <summary>
        /// <b>Der VOLLKREIS ist ein eigener Fall</b> — dieselbe Regel wie im Maler
        /// (Befund zu Auftrag #222): Ein Bogen ueber 360 Grad zieht nichts, Anfang und
        /// Ende fallen zusammen. Ab 360 Grad wird eine Ellipse geschrieben.
        /// </summary>
        [Fact]
        public void EinVollkreisWirdEineEllipseUndKeinBogen()
        {
            Zeichenmodell m = Modell();
            m.Fuege(new Kreissegment(0f, 0f, 40f, 20f, 0f, 360f, null,
                                     new Fuellung(Farbton.Aus(Farbrolle.SERIE_1))));

            string t = Text(m);
            Assert.Contains("<ellipse cx=\"20\" cy=\"10\" rx=\"20\" ry=\"10\"", t);
            Assert.DoesNotContain("<path", t);
        }

        /// <summary>Ein Tortenstueck wird ein Bogenpfad vom Mittelpunkt aus.</summary>
        [Fact]
        public void EinTortenstueckWirdEinBogenpfad()
        {
            Zeichenmodell m = Modell();
            m.Fuege(new Kreissegment(0f, 0f, 40f, 40f, 0f, 90f, null,
                                     new Fuellung(Farbton.Aus(Farbrolle.SERIE_1))));

            // Mittelpunkt (20,20), Radius 20: von (40,20) ueber den Viertelbogen
            // nach (20,40), im Uhrzeigersinn (sweep-flag 1), kleiner Bogen (0).
            Assert.Contains("d=\"M 20,20 L 40,20 A 20,20 0 0,1 20,40 Z\"", Text(m));
        }

        // =====================================================================
        // 2 — Text
        // =====================================================================

        /// <summary>
        /// Die AUSRICHTUNG wird zum <c>text-anchor</c>: Der Renderer gibt dem Maler
        /// die gerechnete x-Stelle und die Ausrichtung, und jeder Ausgabeweg setzt sie
        /// auf seine Art um — der Maler ueber die gemessene Breite, das SVG ueber das
        /// Attribut.
        /// </summary>
        [Theory]
        [InlineData(Ausrichtung.Links, "start")]
        [InlineData(Ausrichtung.Mitte, "middle")]
        [InlineData(Ausrichtung.Rechts, "end")]
        public void DieAusrichtungWirdZumTextAnker(Ausrichtung a, string anker)
        {
            Zeichenmodell m = Modell();
            m.Text("Reihe 1", 10f, 20f, new Schrift(15f), Farbton.Aus(Farbrolle.TEXT), a);

            Assert.Contains("text-anchor=\"" + anker + "\"", Text(m));
        }

        /// <summary>
        /// Groesse in BILDPUNKTEN (Punkt x 96/72 — dieselbe Umrechnung, die die
        /// Schriftkette fuer Skia macht), Stil, Farbe, Inhalt. Die y-Koordinate geht
        /// UNVERAENDERT durch; dass sie die OBERE Kante meint, sagt das
        /// <c>dominant-baseline</c> daneben.
        /// </summary>
        [Fact]
        public void EinTextTraegtGroesseStilFarbeUndInhalt()
        {
            Zeichenmodell m = Modell();
            m.Text("A & B", 10f, 20f, new Schrift(15f, Fett: true),
                   Farbton.Aus(Farbrolle.STAMM));

            string t = Text(m);
            Assert.Contains("<text x=\"10\" y=\"20\" font-size=\"20px\" font-weight=\"bold\"", t);
            Assert.Contains("dominant-baseline=\"text-before-edge\"", t);
            Assert.Contains("fill=\"#1F4E79\"", t);
            Assert.Contains(">A &amp; B</text>", t);
        }

        /// <summary>Ein leerer Text erzeugt kein Element — genau wie der Maler ihn uebergeht.</summary>
        [Fact]
        public void EinLeererTextErzeugtKeinElement()
        {
            Zeichenmodell m = Modell();
            m.Text("", 1f, 2f, new Schrift(15f), Farbton.Aus(Farbrolle.TEXT));

            Assert.DoesNotContain("<text", Text(m));
        }

        // =====================================================================
        // 3 — Stift und Fuellung
        // =====================================================================

        /// <summary>
        /// Strichmuster, Kappe und Verbindung stehen NUR, wo sie von der SVG-Vorgabe
        /// abweichen — dieselbe Sparsamkeit, mit der der Maler nur setzt, was vom
        /// Skia-Standard abweicht.
        /// </summary>
        [Fact]
        public void DerStiftWirdZuStrichFarbeUndMuster()
        {
            Zeichenmodell m = Modell();
            m.Linie(0f, 0f, 10f, 10f, new Stift(Farbton.Aus(Farbrolle.ACHSE), 2f,
                    new Strichmuster(3f, 4f, 1f), Strichkappe.Rund, Strichverbindung.Rund));
            m.Linie(0f, 0f, 10f, 10f, Strich());

            string t = Text(m);
            Assert.Contains("stroke=\"#696969\" stroke-width=\"2\" stroke-linecap=\"round\" " +
                            "stroke-linejoin=\"round\" stroke-dasharray=\"3 4\" " +
                            "stroke-dashoffset=\"1\"", t);

            // Der schlichte Strich traegt keine der vier Zusatzangaben.
            string schlicht = t.Substring(t.LastIndexOf("<line", StringComparison.Ordinal));
            Assert.DoesNotContain("linecap", schlicht);
            Assert.DoesNotContain("dasharray", schlicht);
        }

        /// <summary>
        /// <b>Ohne Fuellung steht <c>fill="none"</c>.</b> Die SVG-Vorgabe waere
        /// Schwarz — ein nur umrandetes Rechteck des Bestands stuende dann als
        /// schwarzer Block im Bild.
        /// </summary>
        [Fact]
        public void EinNurUmrandetesRechteckWirdNichtSchwarz()
        {
            Zeichenmodell m = Modell();
            m.Rechteck(1f, 2f, 3f, 4f, Strich(Farbrolle.RAHMEN));

            Assert.Contains("fill=\"none\" stroke=\"#C0C0C0\"", Text(m));
        }

        /// <summary>
        /// Die DECKUNG steht als eigenes Attribut und nur, wo sie fehlt: Die
        /// Hexschreibweise fuehrt sie nicht (sie gehoert zum Bildaufbau und wird nicht
        /// eingestellt).
        /// </summary>
        [Fact]
        public void DieDeckungStehtNurWoSieFehlt()
        {
            Zeichenmodell m = Modell();
            m.Rechteck(0f, 0f, 10f, 10f, null, new Fuellung(Farbton.Aus(Farbrolle.WAERME_WP)));
            m.Rechteck(0f, 0f, 10f, 10f, null,
                       new Fuellung(Farbton.Aus(Farbrolle.AUSSENTEMPERATUR)));

            string t = Text(m);
            Assert.Contains("fill=\"#4172C4\"/>", t);                          // deckend
            Assert.Contains("fill=\"#4682B4\" fill-opacity=\"0.353\"", t);     // 90 von 255
        }

        // =====================================================================
        // 4 — Zuschnitt und Marke
        // =====================================================================

        /// <summary>
        /// Eine zugeschnittene Gruppe wird ein <c>clipPath</c> in <c>&lt;defs&gt;</c>
        /// und ein <c>&lt;g clip-path&gt;</c>. Die KENNUNG steht im Namen: Stehen zwei
        /// Bilder auf einer Seite, schnitte das eine sonst am Rechteck des anderen.
        /// </summary>
        [Fact]
        public void EinZuschnittWirdClipPathMitKennung()
        {
            Zeichenmodell m = Modell();
            m.Gruppe(new Rahmen(5f, 6f, 20f, 30f), g => g.Linie(0f, 0f, 1f, 1f, Strich()));

            string t = Text(m, null, "bild7");
            Assert.Contains("<defs>", t);
            Assert.Contains("<clipPath id=\"bild7-c1\">", t);
            Assert.Contains("<rect x=\"5\" y=\"6\" width=\"20\" height=\"30\"/>", t);
            Assert.Contains("clip-path=\"url(#bild7-c1)\"", t);

            Assert.Contains("bild8-c1", Text(m, null, "bild8"));
            Assert.DoesNotContain("bild7", Text(m, null, "bild8"));
        }

        /// <summary>Eine Gruppe OHNE Zuschnitt ist nur eine Klammer — kein clipPath.</summary>
        [Fact]
        public void EineGruppeOhneZuschnittBleibtEineKlammer()
        {
            Zeichenmodell m = Modell();
            m.Gruppe(null, g => g.Linie(0f, 0f, 1f, 1f, Strich()));

            string t = Text(m);
            Assert.DoesNotContain("<defs>", t);
            Assert.DoesNotContain("clip-path", t);
            Assert.Contains("<g>", t);
        }

        /// <summary>Jede Marke geht als <c>data-marke</c> mit; ohne Marke steht kein Attribut.</summary>
        [Fact]
        public void JedeMarkeWirdZuDataMarke()
        {
            Zeichenmodell m = Modell();
            m.Markiert("xachse", z => z.Linie(0f, 0f, 1f, 1f, Strich()));
            m.Linie(2f, 2f, 3f, 3f, Strich());

            string t = Text(m);
            Assert.Contains("data-marke=\"xachse\"", t);

            SvgKnoten ohne = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                .Last(k => k.Name == "line");
            Assert.Null(Wert(ohne, "data-marke"));
        }

        // =====================================================================
        // 5 — Die Palette loest beim SCHREIBEN auf
        // =====================================================================

        /// <summary>
        /// Aufgeloest wird beim SCHREIBEN, gegen die uebergebene Palette — genau wie
        /// beim Malen. Deshalb tragen Bildschirm und Bericht dieselben Farben, und ein
        /// Tausch erreicht beide, ohne dass eine Zeichenmethode angefasst wird
        /// (DG-Q7).
        /// </summary>
        [Fact]
        public void DieUebergebenePaletteLoestDieRollenAuf()
        {
            Zeichenmodell m = Modell();
            m.Rechteck(0f, 0f, 10f, 10f, null, new Fuellung(Farbton.Aus(Farbrolle.WAERME_WP)));

            Assert.Contains("fill=\"#4172C4\"", SvgSchreiber.Text(m, Farbpalette.Vorgabe));

            var rot = new Farbpalette(
                new Dictionary<Farbrolle, Farbe>
                { { Farbrolle.WAERME_WP, new Farbe(0xFF, 0x00, 0x00) } },
                Farbpalette.Vorgabe);
            Assert.Contains("fill=\"#FF0000\"", SvgSchreiber.Text(m, rot));
        }

        // =====================================================================
        // 6 — Das innere svg in Datenkoordinaten (DG-E2-2)
        // =====================================================================

        private static Zeichenmodell Flaechenmodell()
        {
            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Linie(0f, 0f, 1f, 1f, new Stift(Farbton.Aus(Farbrolle.WAERME_WP), 2f))
            { Marke = "reihe:A" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(10f, 20f, 100f, 50f),
                                           new Datenfenster(0, 3, -10, 30));
            m.FuegeReihe(new Datenreihe("A", Farbton.Aus(Farbrolle.WAERME_WP), 2f, null,
                                        new double[] { 30, 0, -10, 10 }));
            return m;
        }

        /// <summary>
        /// Die Zeichenflaeche wird ein INNERES <c>&lt;svg&gt;</c>: Pixelrechteck als
        /// <c>x/y/width/height</c>, Datenfenster als <c>viewBox</c>,
        /// <c>preserveAspectRatio="none"</c>. Zoom und Verschieben sind damit EINE
        /// Attributaenderung.
        /// </summary>
        [Fact]
        public void DieFlaecheWirdEinInneresSvgInDatenkoordinaten()
        {
            SvgKnoten w = SvgSchreiber.Baum(Flaechenmodell(), Farbpalette.Vorgabe);
            SvgKnoten fl = w.Alle().Single(k => k.Name == "svg" && !ReferenceEquals(k, w));

            Assert.Equal("epos-flaeche", Wert(fl, "class"));
            Assert.Equal("10", Wert(fl, "x"));
            Assert.Equal("20", Wert(fl, "y"));
            Assert.Equal("100", Wert(fl, "width"));
            Assert.Equal("50", Wert(fl, "height"));
            Assert.Equal("0 0 3 40", Wert(fl, "viewBox"));
            Assert.Equal("none", Wert(fl, "preserveAspectRatio"));
        }

        /// <summary>
        /// <b>Die y-UMKEHR:</b> SVG zaehlt y nach unten, die Werte zaehlen nach oben.
        /// Gerechnet wird <c>y' = YBis - y</c>, damit oben <c>YBis</c> und unten
        /// <c>YVon</c> liegt und die <c>viewBox</c> senkrecht bei 0 beginnt — ein Zoom
        /// auf der ZEITACHSE aendert dann nur x und Breite.
        ///
        /// <para>Die Punkte selbst stehen in Datenkoordinaten: x ist die
        /// Stuetzstelle, y der umgerechnete Wert. Geklemmt wird NICHT — das innere
        /// svg schneidet selbst ab, und ein geklemmter Wert waere beim Zoom
        /// verloren.</para>
        /// </summary>
        [Fact]
        public void DieReiheStehtInDatenkoordinatenMitUmgekehrterYAchse()
        {
            SvgKnoten w = SvgSchreiber.Baum(Flaechenmodell(), Farbpalette.Vorgabe);
            SvgKnoten fl = w.Alle().Single(k => k.Name == "svg" && !ReferenceEquals(k, w));
            SvgKnoten pfad = Assert.Single(fl.Kinder);

            Assert.Equal("path", pfad.Name);
            Assert.Equal("epos-reihe", Wert(pfad, "class"));
            Assert.Equal("A", Wert(pfad, "data-reihe"));
            Assert.Equal("reihe:A", Wert(pfad, "data-marke"));
            Assert.Equal("none", Wert(pfad, "fill"));
            Assert.Equal("#4172C4", Wert(pfad, "stroke"));
            Assert.Equal("2", Wert(pfad, "stroke-width"));
            Assert.Equal("non-scaling-stroke", Wert(pfad, "vector-effect"));

            // 30 -> 0 (oben), 0 -> 30, -10 -> 40 (unten), 10 -> 20.
            Assert.Equal("M 0,0 L 1,30 2,40 3,20", Wert(pfad, "d"));
        }

        /// <summary>
        /// Der auf jeden n-ten Wert gekuerzte PIXELPFAD der Reihe faellt weg — er
        /// bleibt allein dem PNG (Entscheid DG-E2-2). An seiner Stelle steht das
        /// innere svg.
        /// </summary>
        [Fact]
        public void DerGekuerztePixelpfadDerReiheFaelltWeg()
        {
            SvgKnoten w = SvgSchreiber.Baum(Flaechenmodell(), Farbpalette.Vorgabe);

            Assert.DoesNotContain(w.Alle(), k => k.Name == "line");
            Assert.Single(w.Alle(), k => k.Name == "svg" && !ReferenceEquals(k, w));
        }

        /// <summary>
        /// <b>Ohne Flaeche — oder ohne Reihen — faellt NICHTS weg:</b> Dann bleibt der
        /// Pixelpfad stehen, und das Bild ist vollstaendig. Ein Kuchen, ein Ring und
        /// jedes noch nicht umgestellte Bild gehen diesen Weg.
        /// </summary>
        [Fact]
        public void OhneFlaecheBleibtDerPixelpfadStehen()
        {
            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Pfad(new Wertliste<Punkt>(new[] { new Punkt(0f, 0f), new Punkt(10f, 10f) }),
                             false, new Stift(Farbton.Aus(Farbrolle.WAERME_WP), 2f))
            { Marke = "reihe:A" });

            string ohneAlles = Text(m);
            Assert.Contains("data-marke=\"reihe:A\"", ohneAlles);
            Assert.Contains("d=\"M 0,0 L 10,10\"", ohneAlles);
            Assert.DoesNotContain("epos-flaeche", ohneAlles);

            // Auch eine Flaeche OHNE Datenreihen laesst den Pixelpfad stehen.
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 50f),
                                           new Datenfenster(0, 10, 0, 1));
            string ohneReihen = Text(m);
            Assert.Contains("d=\"M 0,0 L 10,10\"", ohneReihen);
            Assert.DoesNotContain("epos-flaeche", ohneReihen);
        }

        /// <summary>
        /// Ueber der Rohgrenze wird GEBUENDELT (DG-Q5): Der Pfad einer
        /// Viertelstundenreihe traegt hoechstens zwei Punkte je Bildpunktspalte statt
        /// 35 040 Stuetzstellen.
        /// </summary>
        [Fact]
        public void UeberDerRohgrenzeWirdGebuendelt()
        {
            var werte = new double[35040];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = 20 + 15 * Math.Sin(2 * Math.PI * i / 35040.0) + Math.Sin(i * 0.9);

            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Linie(0f, 0f, 1f, 1f, Strich()) { Marke = "reihe:V" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 50f),
                                           new Datenfenster(0, werte.Length - 1, 0, 40));
            m.FuegeReihe(new Datenreihe("V", Farbton.Aus(Farbrolle.WAERME_WP), 1f, null, werte));

            SvgKnoten w = SvgSchreiber.Baum(m, Farbpalette.Vorgabe);
            SvgKnoten pfad = w.Alle().Single(k => Wert(k, "class") == "epos-reihe");

            int punkte = Wert(pfad, "d").Count(c => c == ',');
            Assert.False(Pfadregel.Roh(werte.Length, 1));
            Assert.True(punkte <= 2 * 100,
                        "gebuendelt sind hoechstens zwei Punkte je Spalte, gezaehlt: " + punkte);
            Assert.True(punkte > 100, "und mindestens einer je Spalte, gezaehlt: " + punkte);
        }

        // =====================================================================
        // 7 — Determinismus
        // =====================================================================

        /// <summary>
        /// Zweimal geschrieben — und zweimal erzeugt — ergibt denselben Text, Zeichen
        /// fuer Zeichen. Zeilenenden sind LF; ein CR kaeme aus der Umgebung und
        /// machte den Vergleich plattformabhaengig.
        /// </summary>
        [Fact]
        public void ZweimalGeschriebenIstDerselbeText()
        {
            Zeichenmodell m = Flaechenmodell();

            string a = Text(m);
            Assert.Equal(a, Text(m));
            Assert.Equal(a, Text(Flaechenmodell()));
            Assert.DoesNotContain("\r", a);
        }
    }
}
