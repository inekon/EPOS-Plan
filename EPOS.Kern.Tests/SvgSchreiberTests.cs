using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <c>dominant-baseline</c> daneben — auf dem BILDSCHIRM (Chromium kennt es).
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

        /// <summary>
        /// Anwenderbefund Word-Export: Der SVG-Leser von Word (und von LibreOffice) kennt
        /// <c>dominant-baseline</c> nicht und nimmt y als GRUNDLINIE — jeder Text stand um den
        /// Aufstieg zu hoch, der Titel oben aus dem Bild. Der DRUCK schreibt deshalb die
        /// Grundlinie ausgerechnet: y = Oberkante + Aufstieg, ohne das Attribut.
        /// </summary>
        [Fact]
        public void ImDruckStehtDieGrundlinieAusgerechnetOhneDominantBaseline()
        {
            Zeichenmodell m = Modell();
            m.Text("A", 10f, 20f, new Schrift(15f), Farbton.Aus(Farbrolle.TEXT));

            string t = SvgSchreiber.Drucktext(m, null, "d", s => 7.5f);
            Assert.Contains("<text x=\"10\" y=\"27.5\" font-size=\"20px\"", t);
            Assert.DoesNotContain("dominant-baseline", t);

            // Ohne Metrik: der Naeherungswert je Geviert (20 px x AUFSTIEG_EM).
            string ohne = SvgSchreiber.Drucktext(m);
            float y = 20f + SvgSchreiber.AUFSTIEG_EM * 20f;
            Assert.Contains("y=\"" + y.ToString("0.##", CultureInfo.InvariantCulture) + "\"", ohne);
            Assert.DoesNotContain("dominant-baseline", ohne);

            // Der Bildschirm bleibt beim Attribut - kein doppeltes Verschieben.
            string schirm = SvgSchreiber.Text(m);
            Assert.Contains("<text x=\"10\" y=\"20\"", schirm);
            Assert.Contains("dominant-baseline=\"text-before-edge\"", schirm);
        }

        /// <summary>
        /// Das Druck-SVG des Berichts (<c>SkiaMaler.Drucksvg</c>) steht auf DERSELBEN Grundlinie
        /// wie das PNG: Oberkante minus <c>Metrics.Ascent</c> der Skia-Schrift — für jede
        /// Größe und jeden Schnitt.
        /// </summary>
        [Theory]
        [InlineData(13f, false, false)]
        [InlineData(15f, false, false)]
        [InlineData(14f, false, true)]
        [InlineData(22f, true, false)]
        public void DasDrucksvgStehtAufDerGrundlinieDesMalers(float punkt, bool fett, bool kursiv)
        {
            var schrift = new Schrift(punkt, fett, kursiv);
            Zeichenmodell m = Modell(400, 100);
            m.Text("Kumulierter Barwert", 10f, 16f, schrift, Farbton.Aus(Farbrolle.TEXT));

            float erwartet;
            using (SkiaSharp.SKFont f = Schriftkette.Erzeuge(schrift)) erwartet = 16f - f.Metrics.Ascent;
            Assert.True(erwartet > 16f, "Der Aufstieg ist positiv.");

            SvgKnoten text = SvgSchreiber.Druckbaum(m, null, "d", Schriftkette.Aufstieg)
                .Alle().Single(k => k.Name == "text");
            float y = float.Parse(Wert(text, "y"), CultureInfo.InvariantCulture);
            Assert.Equal(erwartet, y, 2);
            Assert.Null(Wert(text, "dominant-baseline"));

            Assert.Contains("y=\"" + erwartet.ToString("0.##", CultureInfo.InvariantCulture) + "\"",
                            SkiaMaler.Drucksvg(m));
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
        /// <c>x/y/width/height</c>, <c>preserveAspectRatio="none"</c>. Zoom und
        /// Verschieben sind damit EINE Attributaenderung.
        ///
        /// <para><b>Die viewBox traegt waagerecht Stunden, senkrecht BILDPUNKTE</b>
        /// (Entscheid DG-E3-1): <c>XVon 0 Breite Bild.Hoehe</c>. Erst damit koennen
        /// zwei Achsen dasselbe innere svg tragen — jede Reihe rechnet ihren Wert
        /// ueber IHR Fenster in Bildpunkte um.</para>
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
            Assert.Equal("0 0 3 50", Wert(fl, "viewBox"));
            Assert.Equal("none", Wert(fl, "preserveAspectRatio"));
        }

        /// <summary>
        /// <b>Die SENKRECHTE steht in BILDPUNKTEN (DG-E3-1):</b> SVG zaehlt y nach
        /// unten, die Werte zaehlen nach oben. Gerechnet wird
        /// <c>y = Hoehe − (Wert − YVon) / (YBis − YVon) · Hoehe</c>, damit die viewBox
        /// senkrecht bei 0 beginnt und bei der Bildhoehe endet — ein Zoom auf der
        /// ZEITACHSE aendert dann nur x und Breite, und eine Reihe der zweiten Achse
        /// steht mit IHRER Skala im selben inneren svg.
        ///
        /// <para>x bleibt in Datenkoordinaten (die Stuetzstelle). Geklemmt wird
        /// NICHT — das innere svg schneidet selbst ab, und ein geklemmter Wert waere
        /// beim Zoom verloren.</para>
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

            // Fenster -10…30 auf 50 Bildpunkte: 30 -> 0 (oben), 0 -> 37,5,
            // -10 -> 50 (unten), 10 -> 25.
            Assert.Equal("M 0,0 L 1,37.5 2,50 3,25", Wert(pfad, "d"));
        }

        /// <summary>
        /// <b>Eine Reihe mit EIGENEM Fenster</b> (DG-E3-1): Dieselbe Zeichenflaeche
        /// traegt eine zweite Reihe, deren y-Achse eine ganz andere ist — der
        /// Speicherinhalt rechts neben den Leistungen links. Gerechnet wird gegen IHR
        /// Fenster, gezeichnet wird in DASSELBE innere svg.
        /// </summary>
        [Fact]
        public void EineReiheMitEigenemFensterRechnetGegenIhreAchse()
        {
            Zeichenmodell m = Flaechenmodell();
            m.FuegeReihe(new Datenreihe("B", Farbton.Aus(Farbrolle.SPEICHER_1), 2f, null,
                                        new double[] { 0, 500, 1000, 250 },
                                        new Datenfenster(0, 3, 0, 1000)));

            SvgKnoten fl = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                .Single(k => Wert(k, "class") == "epos-flaeche");
            SvgKnoten b = fl.Kinder.Single(k => Wert(k, "data-reihe") == "B");

            // 0 -> 50 (unten), 500 -> 25, 1000 -> 0 (oben), 250 -> 37,5.
            Assert.Equal("M 0,50 L 1,25 2,0 3,37.5", Wert(b, "d"));

            // Die Reihe der LINKEN Achse bleibt unberuehrt.
            Assert.Equal("M 0,0 L 1,37.5 2,50 3,25",
                         Wert(fl.Kinder.Single(k => Wert(k, "data-reihe") == "A"), "d"));
        }

        /// <summary>
        /// <b>Eine FLAECHE wird ein geschlossener Pfad</b> (DG-E3-2): Oberkante
        /// vorwaerts, Unterkante rueckwaerts, <c>Z</c>. Gefuellt wird in der
        /// Reihenfarbe, ohne Strich — der Stapel des Bestands zieht keine Randlinie.
        /// </summary>
        [Fact]
        public void EineFlaecheMitUntenWirdEinGeschlossenerPfad()
        {
            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Linie(0f, 0f, 1f, 1f, Strich()) { Marke = "reihe:S" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 50f),
                                           new Datenfenster(0, 3, 0, 100));
            m.FuegeReihe(new Datenreihe("S", Farbton.Aus(Farbrolle.WAERME_WP), 0f, null,
                                        new double[] { 60, 80, 100, 40 },
                                        new Datenfenster(0, 3, 0, 100),
                                        Reihenart.Flaeche,
                                        new double[] { 20, 40, 50, 20 }));

            SvgKnoten pfad = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                .Single(k => Wert(k, "class") == "epos-reihe");

            Assert.Equal("#4172C4", Wert(pfad, "fill"));
            Assert.Equal("none", Wert(pfad, "stroke"));
            Assert.Null(Wert(pfad, "stroke-width"));
            // Oberkante 60/80/100/40 -> 20/10/0/30, Unterkante 20/40/50/20 -> 40/30/25/40.
            Assert.Equal("M 0,20 L 1,10 2,0 3,30 3,40 2,25 1,30 0,40 Z", Wert(pfad, "d"));
        }

        /// <summary>
        /// <b>Ohne <c>Unten</c> schliesst die Flaeche auf der ACHSENNULL</b>, in das
        /// Fenster geklemmt — eine Achse, die gar nicht bis null reicht, liefe sonst
        /// aus dem Bild. Zieht das PNG eine Randlinie, traegt die Flaeche sie in
        /// DEREN Farbe (das Profilband).
        /// </summary>
        [Fact]
        public void EineFlaecheOhneUntenSchliesstAufDerAchsennull()
        {
            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Linie(0f, 0f, 1f, 1f, Strich()) { Marke = "reihe:P" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 50f),
                                           new Datenfenster(0, 2, 0, 10));
            m.FuegeReihe(new Datenreihe("P", Farbton.Aus(Farbrolle.PROFILFLAECHE), 2f, null,
                                        new double[] { 5, 10, 0 },
                                        new Datenfenster(0, 2, 0, 10),
                                        Reihenart.Flaeche, null,
                                        Farbton.Aus(Farbrolle.PROFILLINIE)));

            SvgKnoten pfad = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                .Single(k => Wert(k, "class") == "epos-reihe");

            Assert.Equal("#0000FF", Wert(pfad, "fill"));
            Assert.Equal("0.392", Wert(pfad, "fill-opacity"));   // 100 von 255
            Assert.Equal("#0000FF", Wert(pfad, "stroke"));
            Assert.Equal("2", Wert(pfad, "stroke-width"));
            Assert.Equal("M 0,25 L 1,0 2,50 2,50 1,50 0,50 Z", Wert(pfad, "d"));
        }

        /// <summary>
        /// <b>Die gebuendelte Flaeche ist die KONSERVATIVE HUELLE</b> (DG-E3-2): je
        /// Bildpunktspalte der Hoechstwert der Oberkante und der Kleinstwert der
        /// Unterkante — hoechstens ein Punkt je Spalte und Kante, und nie weniger
        /// Flaeche als roh.
        /// </summary>
        [Fact]
        public void EineGebuendelteFlaecheTraegtHoechstUndKleinstwert()
        {
            var oben = new double[20000];
            var unten = new double[20000];
            for (int i = 0; i < oben.Length; i++)
            {
                unten[i] = 10 + 5 * Math.Sin(i * 0.7);
                oben[i] = unten[i] + 20 + 10 * Math.Sin(i * 0.31);
            }

            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Linie(0f, 0f, 1f, 1f, Strich()) { Marke = "reihe:F" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 50f),
                                           new Datenfenster(0, oben.Length - 1, 0, 50));
            m.FuegeReihe(new Datenreihe("F", Farbton.Aus(Farbrolle.WAERME_WP), 0f, null, oben,
                                        null, Reihenart.Flaeche, unten));

            SvgKnoten pfad = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                .Single(k => Wert(k, "class") == "epos-reihe");
            string d = Wert(pfad, "d");

            Assert.False(Pfadregel.Roh(oben.Length, 1));
            Assert.EndsWith(" Z", d);
            int punkte = d.Count(c => c == ',');
            Assert.True(punkte <= 2 * 100, "hoechstens ein Punkt je Spalte und Kante: " + punkte);
            Assert.True(punkte >= 2 * 100 - 2, "und mindestens einer je Spalte: " + punkte);
        }

        // =====================================================================
        // 6b — Der Fensterpfad (DG-E3-3)
        // =====================================================================

        /// <summary>
        /// <b>Der Fensterpfad einer ROHEN Reihe ist der Ausschnitt des Vollpfads.</b>
        /// Der Baustein rechnet beim Zoom ueber das Vierfache den sichtbaren Bereich
        /// damit nach — aus den Datenreihen des Modells, ohne Rundlauf in den Kern
        /// (DG-E2-4, eingeloest mit DG-E3-3).
        /// </summary>
        [Fact]
        public void DerFensterpfadIstDerAusschnittDesVollpfads()
        {
            var werte = new double[200];
            for (int i = 0; i < werte.Length; i++) werte[i] = Math.Sin(i * 0.11) * 40;

            var flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 400f, 100f),
                                             new Datenfenster(0, werte.Length - 1, -50, 50));
            var reihe = new Datenreihe("R", Farbton.Aus(Farbrolle.WAERME_WP), 2f, null, werte,
                                       flaeche.Daten);

            string[] voll = Punkte(SvgSchreiber.Reihenpfad(reihe, flaeche, true));
            string[] teil = Punkte(SvgSchreiber.Reihenpfad(reihe, flaeche, 40, 59, true));

            Assert.Equal(200, voll.Length);
            Assert.Equal(20, teil.Length);
            Assert.Equal(voll.Skip(40).Take(20).ToArray(), teil);
        }

        /// <summary>
        /// Derselbe Ausschnitt einer GEBUENDELTEN Reihe geht wahlweise roh: Genau das
        /// ist das Nachladen ab dem Vierfachen — dieselbe Reihe, mehr Punkte.
        /// </summary>
        [Fact]
        public void DerFensterpfadKannRohErzwungenWerden()
        {
            var werte = new double[35040];
            for (int i = 0; i < werte.Length; i++) werte[i] = Math.Sin(i * 0.01) * 30;

            var flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 400f, 100f),
                                             new Datenfenster(0, werte.Length - 1, -50, 50));
            var reihe = new Datenreihe("V", Farbton.Aus(Farbrolle.WAERME_WP), 1f, null, werte,
                                       flaeche.Daten);

            int gebuendelt = Punkte(SvgSchreiber.Reihenpfad(reihe, flaeche, 1000, 1999, false)).Length;
            int roh = Punkte(SvgSchreiber.Reihenpfad(reihe, flaeche, 1000, 1999, true)).Length;

            Assert.Equal(1000, roh);
            Assert.True(gebuendelt < roh,
                        "gebuendelt sind es weniger Punkte als roh: " + gebuendelt + " zu " + roh);
        }

        /// <summary>Die Punktpaare eines Pfads — ohne die Befehlsbuchstaben.</summary>
        private static string[] Punkte(string d)
            => (d ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(t => t.Contains(',')).ToArray();

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

        // =====================================================================
        // 8 — Punktwolke und eigene x-Stellen (Etappe E3, Gruppe b)
        // =====================================================================

        /// <summary>Das Modell einer Punktwolke: x = Temperatur, y = Leistung.</summary>
        private static Zeichenmodell Wolkenmodell()
        {
            var x = new double[] { -10, -5, 0, 5, 10 };
            var y = new double[] { 40, 30, 20, 10, 0 };

            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Kreis(1f, 1f, 2.5f, null, new Fuellung(Farbton.Aus(Farbrolle.WAERME_WP)))
            { Marke = "reihe:W" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 50f),
                                           new Datenfenster(-10, 10, 0, 40),
                                           Achsenart.Wert, "°C");
            m.FuegeReihe(new Datenreihe("W", Farbton.Aus(Farbrolle.WAERME_WP), 5f, null, y,
                                        m.Flaeche.Daten, Reihenart.Punkte, null, null, x, "kW"));
            return m;
        }

        /// <summary>
        /// <b>Eine Punktwolke wird EIN Pfad aus Punktsegmenten</b> (Entscheid
        /// DG-E3-5): je Wert ein <c>M x,y h 0</c> — eine Strecke der Laenge null, die
        /// erst die RUNDE Strichkappe zum Punkt macht. Die Strichbreite ist der
        /// Punktdurchmesser des PNG, gefuellt wird nicht, und 8 760 Punkte sind EIN
        /// Knoten statt 8 760.
        ///
        /// <para>Gebuendelt wird NICHT: Eine Buendelung je Bildpunktspalte naehme
        /// genau die Verdichtung weg, die die Aussage der Wolke ist.</para>
        /// </summary>
        [Fact]
        public void EinePunktwolkeWirdEinPfadAusPunktsegmenten()
        {
            SvgKnoten w = SvgSchreiber.Baum(Wolkenmodell(), Farbpalette.Vorgabe);
            SvgKnoten pfad = w.Alle().Single(k => Wert(k, "class") == "epos-reihe");

            Assert.Equal("none", Wert(pfad, "fill"));
            Assert.Equal("round", Wert(pfad, "stroke-linecap"));
            Assert.Equal("5", Wert(pfad, "stroke-width"));
            Assert.Equal("non-scaling-stroke", Wert(pfad, "vector-effect"));

            // y = Hoehe − (Wert − YVon) / (YBis − YVon) · Hoehe, mit Hoehe = 50.
            Assert.Equal("M -10,0 h 0 M -5,12.5 h 0 M 0,25 h 0 M 5,37.5 h 0 M 10,50 h 0",
                         Wert(pfad, "d"));
        }

        /// <summary>
        /// Der AUSSCHNITT einer Punktwolke laesst weg, was ausserhalb liegt — Punkt
        /// fuer Punkt an seiner eigenen x-Stelle, ohne Indexrechnung.
        /// </summary>
        [Fact]
        public void DerFensterpfadDerPunktwolkeLaesstAeussereWeg()
        {
            Zeichenmodell m = Wolkenmodell();
            Datenreihe r = m.Reihen.Single();

            Assert.Equal("M -5,12.5 h 0 M 0,25 h 0 M 5,37.5 h 0",
                         SvgSchreiber.Reihenpfad(r, m.Flaeche, -5, 5, true));
            // Der Vollpfad ist derselbe wie im Baum.
            Assert.Equal("M -10,0 h 0 M -5,12.5 h 0 M 0,25 h 0 M 5,37.5 h 0 M 10,50 h 0",
                         SvgSchreiber.Reihenpfad(r, m.Flaeche, true));
        }

        /// <summary>
        /// <b>Eine LINIE mit eigenen x-Stellen sitzt an ihnen</b> (DG-E3-5). Die
        /// Schnittkurve der Rastersuche mischt Grob- und Feinpunkte; ohne
        /// <c>XWerte</c> laegen die vier Werte gleichmaessig verteilt — hier bei
        /// 0/100/200/300 statt bei 0/100/250/300.
        /// </summary>
        [Fact]
        public void EineLinieMitEigenenXStellenSitztAnIhnen()
        {
            var x = new double[] { 0, 100, 250, 300 };
            var y = new double[] { 0, 10, 20, 40 };

            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Linie(0f, 0f, 1f, 1f, Strich()) { Marke = "reihe:S" });
            m.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 100f, 40f),
                                           new Datenfenster(0, 300, 0, 40), Achsenart.Wert);
            m.FuegeReihe(new Datenreihe("S", Farbton.Aus(Farbrolle.STAMM), 2f, null, y,
                                        m.Flaeche.Daten, Reihenart.Linie, null, null, x));

            SvgKnoten pfad = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                .Single(k => Wert(k, "class") == "epos-reihe");

            Assert.Equal("M 0,40 L 100,30 250,20 300,0", Wert(pfad, "d"));
            // Und der Ausschnitt schneidet nach der x-STELLE, nicht nach dem Index.
            Assert.Equal("M 100,30 L 250,20", SvgSchreiber.Reihenpfad(
                m.Reihen.Single(), m.Flaeche, 100, 250, true));
        }

        // =====================================================================
        // 9 — Der Wert am Element (Etappe E3, Entscheid DG-E3-6)
        // =====================================================================

        /// <summary>
        /// <b><c>data-wert</c> steht an derselben Stelle wie <c>data-marke</c></b> —
        /// und zwar an JEDEM Primitiv, denn die sieben Bilder der Gruppe (c) tragen
        /// ihre Zahl auf einem Rechteck (Saeule, Balken, Rasterzelle), auf einem
        /// Kreissegment (Kuchen, Ring) oder auf einem Streckenzug (die Bedarfslinie
        /// des Monatsbalkens).
        /// </summary>
        [Fact]
        public void DerWertStehtAlsDataWertNebenDerMarke()
        {
            Zeichenmodell m = Modell(200, 100);
            m.Fuege(new Rechteck(1f, 2f, 3f, 4f, null, new Fuellung(Farbton.Aus(Farbrolle.WAERME_WP)))
            { Marke = "reihe:WP", Wert = "Jan: 12,5 MWh" });
            m.Fuege(new Kreissegment(0f, 0f, 10f, 10f, -90f, 90f, null,
                                     new Fuellung(Farbton.Aus(Farbrolle.WAERME_WP)))
            { Marke = "reihe:WP", Wert = "Waermepumpe: 48,0 %" });
            m.Fuege(new Pfad(new Wertliste<Punkt>(new[] { new Punkt(0f, 0f), new Punkt(5f, 5f) }),
                             false, Strich())
            { Marke = "reihe:Bedarf", Wert = "Strombedarf" });
            m.Fuege(new Linie(0f, 0f, 1f, 1f, Strich()) { Marke = "marke", Wert = "Optimum" });
            m.Fuege(new Kreis(5f, 5f, 2f, Strich()) { Marke = "skala", Wert = "4.000 €" });
            m.Fuege(new WindowsFormsApplication1.Zeichnung.Text(
                        "T", 0f, 0f, new Schrift(12f), Farbton.Aus(Farbrolle.TEXT))
            { Marke = "xachse", Wert = "Jan" });
            m.Gruppe(null, z => z.Linie(0f, 0f, 1f, 1f, Strich()));

            SvgKnoten w = SvgSchreiber.Baum(m, Farbpalette.Vorgabe);
            List<SvgKnoten> alle = w.Alle().ToList();

            foreach (string name in new[] { "rect", "path", "line", "circle", "text" })
            {
                SvgKnoten k = alle.First(n => n.Name == name && Wert(n, "data-wert") != null);
                Assert.NotNull(Wert(k, "data-marke"));

                // An DERSELBEN Stelle heisst: unmittelbar hinter data-marke.
                List<string> namen = k.Attribute.Select(a => a.Key).ToList();
                Assert.Equal(namen.IndexOf("data-marke") + 1, namen.IndexOf("data-wert"));
            }

            Assert.Equal("Jan: 12,5 MWh",
                         Wert(alle.Single(k => k.Name == "rect" && Wert(k, "data-wert") != null),
                              "data-wert"));
            Assert.Equal("Waermepumpe: 48,0 %",
                         Wert(alle.Single(k => k.Name == "path" && Wert(k, "d").Contains(" A ")),
                              "data-wert"));

            // Ein Befehl OHNE Wert schreibt sich woertlich wie zuvor: kein Attribut.
            SvgKnoten gruppe = alle.Single(k => k.Name == "g");
            Assert.Null(Wert(gruppe, "data-wert"));
            Assert.Null(Wert(gruppe, "data-marke"));
            Assert.DoesNotContain("data-wert=\"\"", Text(m));
        }

        /// <summary>
        /// <b>Der Wert wird maskiert wie jeder Attributwert.</b> Er kommt aus den
        /// Daten des Anwenders — ein Reihenname mit <c>&amp;</c> oder ein
        /// Anfuehrungszeichen zerbraeche den Baum sonst.
        /// </summary>
        [Fact]
        public void DerWertWirdMaskiert()
        {
            Zeichenmodell m = Modell(50, 50);
            m.Fuege(new Rechteck(0f, 0f, 5f, 5f, Strich())
            { Marke = "reihe:A&B", Wert = "A & B \"gross\": 1,5 <MWh>" });

            string text = Text(m);
            Assert.Contains("data-wert=\"A &amp; B &quot;gross&quot;: 1,5 &lt;MWh&gt;\"", text);
            Assert.Contains("data-marke=\"reihe:A&amp;B\"", text);

            // Und der Baum traegt den UNMASKIERTEN Wert - maskiert wird erst beim
            // Schreiben, damit die Oberflaeche ihn roh weiterreichen kann.
            SvgKnoten k = SvgSchreiber.Baum(m, Farbpalette.Vorgabe).Alle()
                                      .Single(n => n.Name == "rect" && Wert(n, "data-wert") != null);
            Assert.Equal("A & B \"gross\": 1,5 <MWh>", Wert(k, "data-wert"));
        }
    }
}
