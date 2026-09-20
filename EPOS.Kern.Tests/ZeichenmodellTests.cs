using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Das Zeichenmodell (Konzept Diagramme, Etappe E1): die Befehlsliste, die
    /// zwischen dem Layout des Renderers und der Ausgabe steht.
    ///
    /// <para>Geprueft wird, was das Modell zu einer tragfaehigen Zwischenstufe macht:
    /// Befehle sammeln sich in ZEICHENREIHENFOLGE, eine Gruppe traegt ihren
    /// Zuschnitt, Stifte und Fuellungen sind WERTGLEICH (ein Record, keine Referenz),
    /// die Farben stehen als ROLLE und nicht als nackte Zahl, und zweimal erzeugt
    /// ergibt dasselbe Modell. Ohne die letzte Eigenschaft waere kein
    /// Determinismus-Nachweis moeglich.</para>
    /// </summary>
    public class ZeichenmodellTests
    {
        private static Farbton Ton(Farbrolle rolle) => new Farbton(rolle);

        // =====================================================================
        // 1 — Befehle und Reihenfolge
        // =====================================================================

        /// <summary>
        /// Jeder Befehl landet in der Liste, und zwar in der Reihenfolge, in der er
        /// abgesetzt wurde: Das Modell ist eine Malanweisung, keine Menge.
        /// </summary>
        [Fact]
        public void BefehleStehenInZeichenreihenfolge()
        {
            var m = new Zeichenmodell(100, 50, Ton(Farbrolle.HINTERGRUND));
            var stift = new Stift(Ton(Farbrolle.ACHSE), 2f);

            m.Linie(0f, 0f, 10f, 10f, stift);
            m.Rechteck(1f, 2f, 3f, 4f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_WP)));
            m.Text("Ja", 5f, 6f, new Schrift(15f), Ton(Farbrolle.TEXT));

            Assert.Equal(3, m.Befehle.Count);
            Assert.IsType<Linie>(m.Befehle[0]);
            Assert.IsType<Rechteck>(m.Befehle[1]);
            Assert.IsType<Text>(m.Befehle[2]);
            Assert.Equal(100, m.Breite);
            Assert.Equal(50, m.Hoehe);
        }

        /// <summary>Jede Primitive traegt ihre Koordinaten unveraendert weiter.</summary>
        [Fact]
        public void PrimitiveTragenIhreKoordinaten()
        {
            var m = new Zeichenmodell(10, 10, Ton(Farbrolle.HINTERGRUND));
            var stift = new Stift(Ton(Farbrolle.ACHSE), 1f);
            var fuellung = new Fuellung(Ton(Farbrolle.REST));

            m.Linie(1f, 2f, 3f, 4f, stift);
            m.Kreis(5f, 6f, 7f, fuellung: fuellung);
            m.Ellipse(1f, 1f, 8f, 9f, fuellung: fuellung);
            m.Pfad(new[] { new Punkt(0f, 0f), new Punkt(1f, 1f) }, false, stift);
            m.Fuege(new Kreissegment(0f, 0f, 4f, 4f, 30f, 90f, null, fuellung));

            var l = (Linie)m.Befehle[0];
            Assert.Equal(1f, l.X1);
            Assert.Equal(4f, l.Y2);

            var k = (Kreis)m.Befehle[1];
            Assert.Equal(7f, k.Radius);

            var e = (Ellipse)m.Befehle[2];
            Assert.Equal(9f, e.Hoehe);

            var p = (Pfad)m.Befehle[3];
            Assert.Equal(2, p.Punkte.Count);
            Assert.False(p.Geschlossen);

            var s = (Kreissegment)m.Befehle[4];
            Assert.Equal(30f, s.Startwinkel);
            Assert.Equal(90f, s.Winkel);
        }

        // =====================================================================
        // 2 — Gruppe und Zuschnitt
        // =====================================================================

        /// <summary>
        /// Eine Gruppe sammelt ihre Befehle IN SICH und traegt ihr Zuschnittrechteck;
        /// das Modell selbst bekommt genau einen Befehl mehr.
        /// </summary>
        [Fact]
        public void GruppeTraegtZuschnittUndEigeneBefehle()
        {
            var m = new Zeichenmodell(20, 20, Ton(Farbrolle.HINTERGRUND));
            var stift = new Stift(Ton(Farbrolle.RASTER), 1f);

            m.Gruppe(new Rahmen(2f, 3f, 10f, 11f), z =>
            {
                z.Linie(0f, 0f, 1f, 1f, stift);
                z.Linie(1f, 1f, 2f, 2f, stift);
            });

            Assert.Single(m.Befehle);
            var g = Assert.IsType<Gruppe>(m.Befehle[0]);
            Assert.True(g.Zuschnitt.HasValue);
            Assert.Equal(2f, g.Zuschnitt.Value.X);
            Assert.Equal(12f, g.Zuschnitt.Value.Rechts);
            Assert.Equal(14f, g.Zuschnitt.Value.Unten);
            Assert.Equal(2, g.Befehle.Count);
        }

        /// <summary>Eine Gruppe ohne Zuschnitt ist nur eine Klammer.</summary>
        [Fact]
        public void GruppeOhneZuschnittIstNurEineKlammer()
        {
            var m = new Zeichenmodell(20, 20, Ton(Farbrolle.HINTERGRUND));
            m.Gruppe(null, z => z.Kreis(1f, 1f, 1f, fuellung: new Fuellung(Ton(Farbrolle.REST))));

            var g = Assert.IsType<Gruppe>(m.Befehle[0]);
            Assert.False(g.Zuschnitt.HasValue);
            Assert.Single(g.Befehle);
        }

        // =====================================================================
        // 3 — Wertgleichheit
        // =====================================================================

        /// <summary>
        /// Zwei gleich belegte Stifte sind GLEICH — sonst koennte kein Test zwei
        /// Modelle vergleichen. Dasselbe gilt fuer Fuellung, Schrift und Strichmuster.
        /// </summary>
        [Fact]
        public void StifteUndFuellungenSindWertgleich()
        {
            Assert.Equal(new Stift(Ton(Farbrolle.ACHSE), 2f),
                         new Stift(Ton(Farbrolle.ACHSE), 2f));
            Assert.NotEqual(new Stift(Ton(Farbrolle.ACHSE), 2f),
                            new Stift(Ton(Farbrolle.ACHSE), 3f));
            Assert.NotEqual(new Stift(Ton(Farbrolle.ACHSE), 2f),
                            new Stift(Ton(Farbrolle.RASTER), 2f));

            Assert.Equal(new Stift(Ton(Farbrolle.ACHSE), 1f, new Strichmuster(8f, 5f)),
                         new Stift(Ton(Farbrolle.ACHSE), 1f, new Strichmuster(8f, 5f)));
            Assert.NotEqual(new Stift(Ton(Farbrolle.ACHSE), 1f, new Strichmuster(8f, 5f)),
                            new Stift(Ton(Farbrolle.ACHSE), 1f, new Strichmuster(6f, 2f)));

            Assert.Equal(new Fuellung(Ton(Farbrolle.STROM_PV)), new Fuellung(Ton(Farbrolle.STROM_PV)));
            Assert.Equal(new Schrift(15f), new Schrift(15f));
            Assert.NotEqual(new Schrift(15f), new Schrift(15f, Fett: true));
        }

        /// <summary>
        /// Ein Pfad vergleicht seine PUNKTE, nicht die Liste: Ein Record mit einem
        /// nackten Array vergliche die Referenz, und zwei gleich gefuellte Pfade
        /// waeren verschieden.
        /// </summary>
        [Fact]
        public void PfadVergleichtSeinePunkte()
        {
            var stift = new Stift(Ton(Farbrolle.WAERME_WP), 2f);
            var a = new Pfad(new Wertliste<Punkt>(new[] { new Punkt(0f, 0f), new Punkt(1f, 2f) }), false, stift);
            var b = new Pfad(new Wertliste<Punkt>(new[] { new Punkt(0f, 0f), new Punkt(1f, 2f) }), false, stift);
            var c = new Pfad(new Wertliste<Punkt>(new[] { new Punkt(0f, 0f), new Punkt(1f, 3f) }), false, stift);

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
        }

        // =====================================================================
        // 4 — Determinismus
        // =====================================================================

        /// <summary>
        /// Zweimal erzeugt ist dasselbe Modell. Das ist die Modellfassung des
        /// Determinismus, den die ChartProben am Bild pruefen.
        /// </summary>
        [Fact]
        public void ZweimalErzeugtLiefertDasselbeModell()
        {
            Assert.True(Probemodell().Gleicht(Probemodell()));
        }

        /// <summary>Ein Unterschied faellt auf — sonst pruefte der Test nichts.</summary>
        [Fact]
        public void EinUnterschiedFaelltAuf()
        {
            Zeichenmodell anders = Probemodell();
            anders.Linie(0f, 0f, 1f, 1f, new Stift(Ton(Farbrolle.ACHSE), 1f));

            Assert.False(Probemodell().Gleicht(anders));
            Assert.False(Probemodell().Gleicht(null));
        }

        private static Zeichenmodell Probemodell()
        {
            var m = new Zeichenmodell(120, 60, Ton(Farbrolle.HINTERGRUND));
            var raster = new Stift(Ton(Farbrolle.RASTER), 1f);
            for (int i = 0; i <= 5; i++) m.Linie(0f, i * 10f, 120f, i * 10f, raster);
            m.Gruppe(new Rahmen(0f, 0f, 120f, 60f), z =>
                z.Pfad(new[] { new Punkt(0f, 0f), new Punkt(60f, 30f), new Punkt(120f, 10f) },
                       false, new Stift(Ton(Farbrolle.WAERME_BHKW), 2f)));
            m.Text("Probe", 4f, 4f, new Schrift(16f, Fett: true), Ton(Farbrolle.STAMM));
            return m;
        }

        // =====================================================================
        // 4b — Marke, Zeichenflaeche und Reihen (Etappe E2)
        // =====================================================================

        /// <summary>
        /// Die MARKE gehoert zum Befehl und damit zur Gleichheit: Ein Bild, dessen
        /// Legende ihre Marke verloere, waere nicht mehr dasselbe Modell — auch wenn
        /// das PNG gleich bliebe (der Maler uebergeht die Marke).
        /// </summary>
        [Fact]
        public void EineMarkeGehoertZumBefehlUndZurGleichheit()
        {
            var stift = new Stift(Ton(Farbrolle.ACHSE), 1f);
            var ohne = new Linie(0f, 0f, 1f, 1f, stift);
            Zeichenbefehl mit = ohne with { Marke = "xachse" };

            Assert.Null(ohne.Marke);
            Assert.Equal("xachse", mit.Marke);
            Assert.NotEqual((Zeichenbefehl)ohne, mit);
            Assert.Equal(mit, (Zeichenbefehl)(ohne with { Marke = "xachse" }));
        }

        /// <summary>
        /// <c>Markiert</c> setzt die Marke an einem ganzen BLOCK — die x-Achse sind
        /// sechs Rasterlinien, sechs Beschriftungen und ein Titel. Eine feinere Marke,
        /// die der Inhalt schon gesetzt hat, bleibt stehen.
        /// </summary>
        [Fact]
        public void MarkiertSetztDieMarkeAmGanzenBlock()
        {
            var m = new Zeichenmodell(10, 10, Ton(Farbrolle.HINTERGRUND));
            var stift = new Stift(Ton(Farbrolle.ACHSE), 1f);

            m.Markiert("yachse", z =>
            {
                z.Linie(0f, 0f, 1f, 1f, stift);
                z.Text("0", 1f, 2f, new Schrift(15f), Ton(Farbrolle.ACHSE));
                z.Markiert("nulllinie", zi => zi.Linie(2f, 2f, 3f, 3f, stift));
            });
            m.Linie(4f, 4f, 5f, 5f, stift);

            Assert.Equal(4, m.Befehle.Count);
            Assert.Equal("yachse", m.Befehle[0].Marke);
            Assert.Equal("yachse", m.Befehle[1].Marke);
            Assert.Equal("nulllinie", m.Befehle[2].Marke);
            Assert.Null(m.Befehle[3].Marke);
        }

        /// <summary>
        /// <c>Gleicht</c> bezieht seit Etappe E2 auch die ZEICHENFLAECHE und die
        /// REIHEN ein — beide gehen nicht ins PNG, wohl aber ins SVG; ein
        /// Determinismusnachweis ohne sie liefe an der halben Ausgabe vorbei.
        /// </summary>
        [Fact]
        public void GleichtBeziehtFlaecheUndReihenEin()
        {
            // Zweimal erzeugt: andere Felder, dieselben Werte — die Reihen werden
            // ueber ihre WERTE verglichen, nicht ueber die Referenz.
            Assert.True(Flaechenmodell().Gleicht(Flaechenmodell()));

            Zeichenmodell andereFlaeche = Flaechenmodell();
            andereFlaeche.Flaeche = new Zeichenflaeche(new Rahmen(0f, 0f, 10f, 10f),
                                                       new Datenfenster(0, 3, 0, 1));
            Assert.False(Flaechenmodell().Gleicht(andereFlaeche));

            Zeichenmodell ohneFlaeche = Flaechenmodell();
            ohneFlaeche.Flaeche = null;
            Assert.False(Flaechenmodell().Gleicht(ohneFlaeche));

            Zeichenmodell eineReiheMehr = Flaechenmodell();
            eineReiheMehr.FuegeReihe(new Datenreihe("B", Ton(Farbrolle.SERIE_2), 1f, null,
                                                     new double[] { 1, 2 }));
            Assert.False(Flaechenmodell().Gleicht(eineReiheMehr));

            Assert.False(Flaechenmodell().Gleicht(Flaechenmodell(7.0)));
        }

        private static Zeichenmodell Flaechenmodell(double letzter = 10.0)
        {
            Zeichenmodell m = Probemodell();
            m.Flaeche = new Zeichenflaeche(new Rahmen(10f, 20f, 100f, 50f),
                                           new Datenfenster(0, 3, -10, 30));
            m.FuegeReihe(new Datenreihe("A", Ton(Farbrolle.WAERME_WP), 2f, null,
                                        new double[] { 30, 0, -10, letzter }));
            return m;
        }

        /// <summary>
        /// <b>Die Vorgaben der Etappe E3 lassen jede Reihe des Bestands, wie sie war:</b>
        /// kein eigenes Fenster, eine Linie, keine Unterkante, kein Rand. Nur so bleibt
        /// jeder Aufruf der Etappe E2 woertlich derselbe.
        /// </summary>
        [Fact]
        public void EineDatenreiheIstOhneWeitereGabenEineLinie()
        {
            var r = new Datenreihe("A", Ton(Farbrolle.WAERME_WP), 2f, null, new double[] { 1, 2 });

            Assert.Null(r.Fenster);
            Assert.Equal(Reihenart.Linie, r.Art);
            Assert.Null(r.Unten);
            Assert.Null(r.Randton);
        }

        /// <summary>
        /// <c>Gleicht</c> bezieht die vier neuen Gaben mit ein (DG-E3-1/2): Zwei Reihen
        /// mit verschiedenem Fenster, verschiedener Art, verschiedener Unterkante oder
        /// verschiedener Randfarbe sind NICHT dieselbe Reihe — sonst liefe der
        /// Determinismusnachweis an der halben Aussage vorbei. <see cref="Datenreihe.Unten"/>
        /// wird dabei ueber die WERTE verglichen, nicht ueber die Referenz.
        /// </summary>
        [Fact]
        public void GleichtBeziehtFensterArtUnterkanteUndRandEin()
        {
            var werte = new double[] { 10, 20, 30 };
            var unten = new double[] { 0, 5, 10 };
            var grund = new Datenreihe("A", Ton(Farbrolle.WAERME_WP), 2f, null, werte);

            Assert.True(grund.Gleicht(new Datenreihe("A", Ton(Farbrolle.WAERME_WP), 2f, null,
                                                     new double[] { 10, 20, 30 })));

            Assert.False(grund.Gleicht(grund with { Fenster = new Datenfenster(0, 2, 0, 30) }));
            Assert.False(grund.Gleicht(grund with { Art = Reihenart.Flaeche }));
            Assert.False(grund.Gleicht(grund with { Unten = unten }));
            Assert.False(grund.Gleicht(grund with { Randton = Ton(Farbrolle.PROFILLINIE) }));

            // Die Unterkante ueber die WERTE, nicht ueber die Referenz.
            var a = grund with { Art = Reihenart.Flaeche, Unten = unten };
            var b = grund with { Art = Reihenart.Flaeche, Unten = new double[] { 0, 5, 10 } };
            Assert.True(a.Gleicht(b));
            Assert.False(a.Gleicht(grund with { Art = Reihenart.Flaeche,
                                                Unten = new double[] { 0, 5, 11 } }));
        }

        // =====================================================================
        // 5 — Farbrollen und Palette
        // =====================================================================

        /// <summary>
        /// Die Vorgabepalette traegt die HAUSFARBEN Wert fuer Wert. Diese Werte sind
        /// die Messlatte: Wer sie aendert, aendert jedes Bild.
        /// </summary>
        [Fact]
        public void VorgabepaletteTraegtDieHausfarben()
        {
            Assert.Equal(new Farbe(0x41, 0x72, 0xC4), Farbpalette.Vorgabe[Farbrolle.WAERME_WP]);
            Assert.Equal(new Farbe(0xED, 0x7D, 0x31), Farbpalette.Vorgabe[Farbrolle.WAERME_BHKW]);
            Assert.Equal(new Farbe(0x70, 0xAD, 0x47), Farbpalette.Vorgabe[Farbrolle.STROM_PV]);
            Assert.Equal(new Farbe(0x1F, 0x4E, 0x79), Farbpalette.Vorgabe[Farbrolle.STAMM]);
            Assert.Equal(new Farbe(0xDC, 0xDC, 0xDC), Farbpalette.Vorgabe[Farbrolle.RASTER]);
            Assert.Equal(new Farbe(0x69, 0x69, 0x69), Farbpalette.Vorgabe[Farbrolle.ACHSE]);
            Assert.Equal(new Farbe(0xFF, 0xFF, 0xFF), Farbpalette.Vorgabe[Farbrolle.HINTERGRUND]);
            Assert.Equal(new Farbe(0x00, 0x64, 0x00, 180), Farbpalette.Vorgabe[Farbrolle.KOSTENPROFIL]);
        }

        /// <summary>
        /// Jede Rolle der Vorgabepalette traegt dieselbe Farbe wie die entsprechende
        /// Konstante des Renderers — die Bruecke zwischen beiden Welten.
        /// </summary>
        [Fact]
        public void PaletteUndRendererkonstantenStimmenUeberein()
        {
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_WP.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.WAERME_WP]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_BHKW.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.WAERME_BHKW]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_KESSEL.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.WAERME_KESSEL]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_SOLAR.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.WAERME_SOLAR]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_PV.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.STROM_PV]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_NETZ.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.STROM_NETZ]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_REST.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.REST]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_BEDARF.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.BEDARF]);
            Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_STAMM.Modellfarbe(),
                         Farbpalette.Vorgabe[Farbrolle.STAMM]);

            var speicher = new[]
            {
                Farbrolle.SPEICHER_1, Farbrolle.SPEICHER_2, Farbrolle.SPEICHER_3,
                Farbrolle.SPEICHER_4, Farbrolle.SPEICHER_5, Farbrolle.SPEICHER_6
            };
            for (int i = 0; i < speicher.Length; i++)
                Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_SPEICHER[i].Modellfarbe(),
                             Farbpalette.Vorgabe[speicher[i]]);

            var serien = new[]
            {
                Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4,
                Farbrolle.SERIE_5, Farbrolle.SERIE_6, Farbrolle.SERIE_7, Farbrolle.SERIE_8
            };
            for (int i = 0; i < serien.Length; i++)
                Assert.Equal(WindowsFormsApplication1.ChartRenderer.C_SERIEN[i].Modellfarbe(),
                             Farbpalette.Vorgabe[serien[i]]);
        }

        /// <summary>
        /// Die Rueckwaertssuche gibt einer durchgereichten Hausfarbe ihre ROLLE
        /// zurueck — darauf beruht, dass ein spaeterer Palettentausch auch die Bilder
        /// erreicht, deren Farben von aussen hereinkommen.
        /// </summary>
        [Fact]
        public void HausfarbenBekommenIhreRolleZurueck()
        {
            Assert.Equal(Farbrolle.WAERME_WP, Farbpalette.Ton(new Farbe(0x41, 0x72, 0xC4)).Rolle);
            Assert.Equal(Farbrolle.RASTER, Farbpalette.Ton(new Farbe(0xDC, 0xDC, 0xDC)).Rolle);
            Assert.Equal(Farbrolle.KOSTENPROFIL, Farbpalette.Ton(new Farbe(0x00, 0x64, 0x00, 180)).Rolle);

            // Dieselbe Farbe mit anderer Deckung: Rolle plus Abwandlung.
            Farbton mitDeckung = Farbpalette.Ton(new Farbe(0x41, 0x72, 0xC4, 210));
            Assert.Equal(Farbrolle.WAERME_WP, mitDeckung.Rolle);
            Assert.Equal((byte)210, mitDeckung.Deckung);
            Assert.Null(mitDeckung.Fest);

            // Eine fremde Farbe bleibt ein Wert ohne Rolle.
            Farbton fremd = Farbpalette.Ton(new Farbe(0x12, 0x34, 0x56));
            Assert.Equal(Farbrolle.UNBENANNT, fremd.Rolle);
            Assert.Equal(new Farbe(0x12, 0x34, 0x56), fremd.Fest);
        }

        /// <summary>
        /// Die Aufloesung: Rolle allein fragt die Palette, Rolle plus Deckung
        /// aendert nur die Deckung, eine gerechnete Farbe schlaegt die Palette.
        /// </summary>
        [Fact]
        public void FarbtonWirdGegenDiePaletteAufgeloest()
        {
            Farbpalette p = Farbpalette.Vorgabe;

            Assert.Equal(new Farbe(0x41, 0x72, 0xC4), p.Loese(new Farbton(Farbrolle.WAERME_WP)));
            Assert.Equal(new Farbe(0x41, 0x72, 0xC4, 210),
                         p.Loese(new Farbton(Farbrolle.WAERME_WP, null, 210)));
            Assert.Equal(new Farbe(0x01, 0x02, 0x03),
                         p.Loese(Farbpalette.Gerechnet(Farbrolle.RASTER_MITTE, new Farbe(1, 2, 3))));
        }

        /// <summary>
        /// Eine getauschte Palette aendert die Farbe der Rolle — und nur die; eine
        /// gerechnete Farbe bleibt, wo sie ist. Das ist die Zusage an den
        /// Anwenderentscheid „die Farben sollen jeweils aenderbar sein".
        /// </summary>
        [Fact]
        public void EineGetauschtePaletteAendertDieRollenfarbe()
        {
            var getauscht = new Farbpalette(
                new Dictionary<Farbrolle, Farbe> { { Farbrolle.WAERME_WP, new Farbe(0x10, 0x20, 0x30) } },
                Farbpalette.Vorgabe);

            Assert.Equal(new Farbe(0x10, 0x20, 0x30), getauscht.Loese(new Farbton(Farbrolle.WAERME_WP)));
            Assert.Equal(new Farbe(0x10, 0x20, 0x30, 180),
                         getauscht.Loese(new Farbton(Farbrolle.WAERME_WP, null, 180)));

            // Alle uebrigen Rollen bleiben, wie die Vorgabe sie hat.
            Assert.Equal(Farbpalette.Vorgabe[Farbrolle.STROM_PV], getauscht[Farbrolle.STROM_PV]);

            // Die Vorgabe selbst ist unberuehrt — sie ist die Messlatte.
            Assert.Equal(new Farbe(0x41, 0x72, 0xC4), Farbpalette.Vorgabe[Farbrolle.WAERME_WP]);
        }

        /// <summary>Jede Rolle, die der Renderer benutzt, ist in der Vorgabe belegt.</summary>
        [Fact]
        public void JedeRolleIstInDerVorgabeBelegt()
        {
            List<Farbrolle> rollen = typeof(Farbrolle)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Farbrolle))
                .Select(f => (Farbrolle)f.GetValue(null))
                .Where(r => r != Farbrolle.UNBENANNT)
                .ToList();

            Assert.NotEmpty(rollen);
            foreach (Farbrolle r in rollen)
                Assert.True(Farbpalette.Vorgabe.Kennt(r), "Rolle ohne Vorgabefarbe: " + r.Name);
        }

        /// <summary>
        /// <b>Die ausdruecklich genannte Rolle ist wertgleich zur Rueckwaertssuche.</b>
        /// Eine Zeichenmethode, die ihre Farbe selbst waehlt, schreibt
        /// <c>Farbton.Aus(Farbrolle.X)</c> statt den Hausfarbenwert durchzureichen
        /// (Etappe E1b). Das darf kein Bild verschieben — und genau das prueft dieser
        /// Fall: Fuer jede Rolle, deren Vorgabefarbe die Rueckwaertssuche EINDEUTIG
        /// zurueckfindet, liefern beide Wege denselben Farbton.
        ///
        /// <para>Die Einschraenkung ist beabsichtigt: Wertgleiche Rollen — SERIE_1
        /// traegt die BHKW-Farbe, SERIE_3 die der Waermepumpe — treffen in der
        /// Rueckwaertssuche die zuerst eingetragene. Fuer sie ist die ausdrueckliche
        /// Rolle der einzige Weg, sie ueberhaupt zu benennen; der FARBWERT bleibt
        /// auch dort derselbe, und genau das steht hier zusaetzlich.</para>
        /// </summary>
        [Fact]
        public void AusdrueckenDerRolleIstWertgleichZurRueckwaertssuche()
        {
            List<Farbrolle> rollen = typeof(Farbrolle)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Farbrolle))
                .Select(f => (Farbrolle)f.GetValue(null))
                .Where(r => r != Farbrolle.UNBENANNT)
                .ToList();

            foreach (Farbrolle r in rollen)
            {
                Farbe wert = Farbpalette.Vorgabe[r];
                Farbton ausdruecklich = Farbton.Aus(r);

                // Der Farbwert ist in JEDEM Fall derselbe — das ist die Zusage an die
                // Hash-Messlatte.
                Assert.Equal(wert, Farbpalette.Vorgabe.Loese(ausdruecklich));

                // Und wo die Rueckwaertssuche eindeutig ist, ist auch der Ton derselbe.
                Farbton rueckwaerts = Farbpalette.Ton(wert);
                if (rueckwaerts.Rolle == r) Assert.Equal(rueckwaerts, ausdruecklich);
            }

            // Die Rollen, die die Saeulen-, Stapel-, Kuchen-, Ring- und Balkenbilder
            // ausdruecklich nennen, sind alle eindeutig — hier namentlich.
            var genannte = new[]
            {
                Farbrolle.HINTERGRUND, Farbrolle.TEXT, Farbrolle.ACHSE,
                Farbrolle.LEGENDENRAHMEN, Farbrolle.STAMM, Farbrolle.WAERME_WP
            };
            foreach (Farbrolle r in genannte)
                Assert.Equal(Farbpalette.Ton(Farbpalette.Vorgabe[r]), Farbton.Aus(r));

            // Eine fehlende Rolle faellt auf UNBENANNT zurueck statt zu werfen.
            Assert.Equal(Farbrolle.UNBENANNT, Farbton.Aus(null).Rolle);
        }

        // =====================================================================
        // Etappe E3, Gruppe (b): Achsenart, x-Stellen und Einheit
        // =====================================================================

        /// <summary>
        /// <b>Die Zeichenflaeche nennt, was ihre x-Achse ZAEHLT</b> (Entscheid
        /// DG-E3-4). Die Vorgabe ist <see cref="Achsenart.Stunden"/> ohne Einheit —
        /// damit bleibt jede Flaeche der frueheren Etappen woertlich, was sie war. Die
        /// beiden neuen Felder gehoeren zur Gleichheit: Zwei Flaechen ueber demselben
        /// Fenster, aber mit verschiedener Achsenart, sind VERSCHIEDEN.
        /// </summary>
        [Fact]
        public void DieZeichenflaecheNenntIhreAchsenartUndEinheit()
        {
            var bild = new Rahmen(0f, 0f, 100f, 50f);
            var daten = new Datenfenster(0, 10, 0, 100);

            var vorgabe = new Zeichenflaeche(bild, daten);
            Assert.Equal(Achsenart.Stunden, vorgabe.X);
            Assert.Null(vorgabe.XEinheit);
            Assert.Equal(vorgabe, new Zeichenflaeche(bild, daten, Achsenart.Stunden));

            Assert.NotEqual(vorgabe, new Zeichenflaeche(bild, daten, Achsenart.Index));
            Assert.NotEqual(new Zeichenflaeche(bild, daten, Achsenart.Wert, "°C"),
                            new Zeichenflaeche(bild, daten, Achsenart.Wert, "kWh"));
        }

        /// <summary>
        /// <b><c>Gleicht</c> vergleicht auch die x-Stellen und die Einheit</b>
        /// (DG-E3-5): Beide gehen in das Modell ein, das die Oberflaeche liest — eine
        /// Reihe mit anderen Stuetzstellen ist eine andere Reihe, und ohne den
        /// Vergleich liefe der Determinismusnachweis an ihnen vorbei.
        ///
        /// <para>Verglichen wird der INHALT der Felder, nicht die Referenz — derselbe
        /// Grund wie bei <c>Werte</c> und <c>Unten</c>.</para>
        /// </summary>
        [Fact]
        public void GleichtVergleichtXStellenUndEinheit()
        {
            var werte = new double[] { 1, 2, 3 };
            var stellen = new double[] { 0, 5, 10 };

            Datenreihe Bauen(double[] x, string einheit, Reihenart art = Reihenart.Linie)
                => new Datenreihe("A", Ton(Farbrolle.STAMM), 1f, null, (double[])werte.Clone(),
                                  null, art, null, null, x, einheit);

            Datenreihe a = Bauen((double[])stellen.Clone(), "kW");

            Assert.True(a.Gleicht(Bauen((double[])stellen.Clone(), "kW")));
            Assert.False(a.Gleicht(Bauen(new double[] { 0, 6, 10 }, "kW")));
            Assert.False(a.Gleicht(Bauen((double[])stellen.Clone(), "€")));
            Assert.False(a.Gleicht(Bauen(null, "kW")));
            Assert.False(a.Gleicht(Bauen((double[])stellen.Clone(), "kW", Reihenart.Punkte)));

            // Und die Vorgaben bleiben, was sie waren: keine Stellen, keine Einheit.
            var schlicht = new Datenreihe("A", Ton(Farbrolle.STAMM), 1f, null, werte);
            Assert.Null(schlicht.XWerte);
            Assert.Null(schlicht.Einheit);
            Assert.Equal(Reihenart.Linie, schlicht.Art);
        }

        // =====================================================================
        // 9 — Der Wert am Element (Etappe E3, Entscheid DG-E3-6)
        // =====================================================================

        /// <summary>
        /// <b>Der Wert ist wahlfrei und gehoert zur Gleichheit.</b> Ohne Angabe
        /// bleibt er <c>null</c> — jeder Befehl des Bestands ist damit woertlich, was
        /// er war —, und zwei Befehle, die sich NUR im Wert unterscheiden, sind
        /// verschieden. Das ist die Zusage, auf der der Determinismusnachweis des
        /// Modells steht: Ein Wert, den die Gleichheit uebersaehe, koennte zwischen
        /// zwei Laeufen wandern, ohne aufzufallen.
        /// </summary>
        [Fact]
        public void DerWertIstWahlfreiUndZaehltZurGleichheit()
        {
            var ohne = new Rechteck(0f, 0f, 10f, 10f, null, new Fuellung(Ton(Farbrolle.WAERME_WP)));
            Assert.Null(ohne.Wert);
            Assert.Null(ohne.Marke);

            Zeichenbefehl mit = ohne with { Marke = "reihe:WP", Wert = "Jan: 12,5 MWh" };
            Assert.Equal("Jan: 12,5 MWh", mit.Wert);
            Assert.Equal("reihe:WP", mit.Marke);

            // Nur der Wert unterscheidet sich — und das genuegt.
            Assert.NotEqual(mit, ohne with { Marke = "reihe:WP" });
            Assert.Equal(mit, ohne with { Marke = "reihe:WP", Wert = "Jan: 12,5 MWh" });

            // Und dasselbe im ganzen Modell: Gleicht vergleicht Befehl fuer Befehl.
            var a = new Zeichenmodell(10, 10, Ton(Farbrolle.HINTERGRUND));
            var b = new Zeichenmodell(10, 10, Ton(Farbrolle.HINTERGRUND));
            a.Fuege(mit);
            b.Fuege(ohne with { Marke = "reihe:WP" });
            Assert.False(a.Gleicht(b));
            Assert.True(a.Gleicht(a));
        }

        /// <summary>
        /// <b>Die Klammer setzt Marke UND Wert</b> — und ueberschreibt nichts, was der
        /// Inhalt schon gesetzt hat. Beides gilt FUER SICH: Ein Befehl mit eigener
        /// Marke, aber ohne Wert, bekommt den Wert der Klammer und behaelt seine
        /// Marke.
        /// </summary>
        [Fact]
        public void DieKlammerSetztMarkeUndWert()
        {
            var m = new Zeichenmodell(100, 50, Ton(Farbrolle.HINTERGRUND));
            var stift = new Stift(Ton(Farbrolle.ACHSE), 1f);

            m.Markiert("reihe:WP", "Jan · Waermepumpe: 12,5 MWh", z =>
            {
                z.Rechteck(0f, 0f, 5f, 5f, fuellung: new Fuellung(Ton(Farbrolle.WAERME_WP)));
                z.Fuege(new Linie(0f, 0f, 1f, 1f, stift) { Marke = "eigene" });
                z.Fuege(new Linie(1f, 1f, 2f, 2f, stift) { Wert = "eigener Wert" });
            });

            Assert.Equal(3, m.Befehle.Count);

            Assert.Equal("reihe:WP", m.Befehle[0].Marke);
            Assert.Equal("Jan · Waermepumpe: 12,5 MWh", m.Befehle[0].Wert);

            // Eigene Marke bleibt, der Wert kommt von der Klammer.
            Assert.Equal("eigene", m.Befehle[1].Marke);
            Assert.Equal("Jan · Waermepumpe: 12,5 MWh", m.Befehle[1].Wert);

            // Eigener Wert bleibt, die Marke kommt von der Klammer.
            Assert.Equal("reihe:WP", m.Befehle[2].Marke);
            Assert.Equal("eigener Wert", m.Befehle[2].Wert);

            // Die Klammer OHNE Wert ist die des Bestands: sie setzt nur die Marke.
            var n = new Zeichenmodell(100, 50, Ton(Farbrolle.HINTERGRUND));
            n.Markiert("titel", z => z.Text("T", 0f, 0f, new Schrift(12f), Ton(Farbrolle.TEXT)));
            Assert.Equal("titel", n.Befehle[0].Marke);
            Assert.Null(n.Befehle[0].Wert);
        }
    }
}
