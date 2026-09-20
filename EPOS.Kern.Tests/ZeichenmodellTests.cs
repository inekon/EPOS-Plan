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
    }
}
