using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7b — die Körperdaten der Ansicht</b> (<see cref="GebaeudeImportAnsicht"/>,
    /// <see cref="GebaeudeAnsichtDaten.Koerper"/>), ohne Datei und ohne Datenbank: Die Hülle füllt
    /// <c>Koerperraeume</c> und <c>Geschosslagen</c> aus dem Zonengeometrie-Modell — Höhe aus dem Raum, sonst aus der
    /// Zone; je Kante, Boden und Decke die Bauteilart der ersten Grenze; die Lage aus der Geschossangabe —, die
    /// Körper stapeln die Geschosse ohne Lage und setzen ohne Höhe die Vorgabe 3,0 m (schematisch). Die Polygone der
    /// Ansicht sind die angelegten Umrisse des Modells: dieselben wie die Körper der Datei (<see cref="Zonenkoerper"/>).
    /// </summary>
    public sealed class GebaeudeImportAnsichtKoerperTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static Umrissseite Seite(string kennung, string bauteil, Bauteilart art, Grenzstellung stellung, int? sektor, double flaeche,
                                         Randbedingung lage = Randbedingung.Aussenluft)
            => new Umrissseite
            {
                Verweis = new Grenzverweis(kennung, bauteil, art, stellung, lage, null),
                FlaecheM2 = flaeche, Sektor = sektor,
            };

        /// <summary>
        /// Erdgeschoss ohne Lage: A 10 × 5 m (V/A = 2,5 m) und B 6 × 5 m (ohne Volumen, Zonenhöhe 2,8 m) mit der
        /// Trennwand T (A Ost, B West); Obergeschoss ohne Lage: C 4 × 5 m ohne Höhe in einer Zone ohne Höhe.
        /// Mit <paramref name="lageOg"/> trägt das Obergeschoss eine Lage.
        /// </summary>
        internal static Umrisseingang Eingang(double? lageOg = null)
        {
            var e = new Umrisseingang();
            e.Geschosse.Add(new Umrissgeschoss("eg", "Erdgeschoss", null));
            e.Geschosse.Add(new Umrissgeschoss("og", "Obergeschoss", lageOg));
            e.Zonen.Add(new Umrisszone("z0", "Zone 0", true, 80.0, null, 2.8, false));
            e.Zonen.Add(new Umrisszone("z1", "Zone 1", true, 20.0, null, null, false));
            var a = new Umrissraum { Kennung = "A", Name = "Raum A", GeschossKennung = "eg", Zone = 0, Beheizt = true, FlaecheM2 = 50.0, VolumenM3 = 125.0 };
            a.Seiten.AddRange(new[]
            {
                Seite("A-S", "A-S", Bauteilart.Aussenwand, Grenzstellung.Wand, 2, 25.0),
                Seite("A-N", "A-N", Bauteilart.Aussenwand, Grenzstellung.Wand, 0, 25.0),
                Seite("A-W", "A-W", Bauteilart.Aussenwand, Grenzstellung.Wand, 3, 12.5),
                Seite("T-A", "T", Bauteilart.Innenwand, Grenzstellung.Wand, 1, 12.5, Randbedingung.Unbekannt),
                Seite("A-Boden", "A-Boden", Bauteilart.Bodenplatte, Grenzstellung.Boden, null, 50.0),
                Seite("A-Dach", "A-Dach", Bauteilart.Dach, Grenzstellung.Decke, null, 50.0),
            });
            var b = new Umrissraum { Kennung = "B", Name = "Raum B", GeschossKennung = "eg", Zone = 0, Beheizt = true, FlaecheM2 = 30.0 };
            b.Seiten.AddRange(new[]
            {
                Seite("T-B", "T", Bauteilart.Innenwand, Grenzstellung.Wand, 3, 12.5, Randbedingung.Unbekannt),
                Seite("B-O", "B-O", Bauteilart.Aussenwand, Grenzstellung.Wand, 1, 12.5),
                Seite("B-S", "B-S", Bauteilart.Aussenwand, Grenzstellung.Wand, 2, 15.0),
                Seite("B-N", "B-N", Bauteilart.Aussenwand, Grenzstellung.Wand, 0, 15.0),
                Seite("B-Boden", "B-Boden", Bauteilart.Bodenplatte, Grenzstellung.Boden, null, 30.0),
            });
            var c = new Umrissraum { Kennung = "C", Name = "Raum C", GeschossKennung = "og", Zone = 1, Beheizt = true, FlaecheM2 = 20.0 };
            c.Seiten.Add(Seite("C-N", "C-N", Bauteilart.Aussenwand, Grenzstellung.Wand, 0, 10.0));
            e.Raeume.AddRange(new[] { a, b, c });
            return e;
        }

        [Fact]
        public void Die_Huelle_fuellt_Koerperraeume_und_Geschosslagen_aus_dem_Modell()
        {
            Zonengeometrie z = Zonengeometrie.AusFlaechen(Eingang(lageOg: 3.2));
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(z, umhaengbar: true);

            Assert.Equal(new[] { "A", "B", "C" }, d.Koerperraeume.Select(k => k.Kennung));
            // Höhe: aus dem Raum (V/A), sonst aus der Zone, sonst keine.
            Assert.Equal(2.5, d.Koerperraum("A")!.HoeheM!.Value, 9);
            Assert.Equal(2.8, d.Koerperraum("B")!.HoeheM!.Value, 9);
            Assert.Null(d.Koerperraum("C")!.HoeheM);
            // Je Kante die Bauteilart der ersten Grenze (Süd, Ost, Nord, West), Boden und Decke.
            GebaeudeAnsichtKoerperraum ka = d.Koerperraum("A")!, kb = d.Koerperraum("B")!, kc = d.Koerperraum("C")!;
            Assert.Equal(new[] { "Aussenwand", "Innenwand", "Aussenwand", "Aussenwand" }, Assert.Single(ka.Kanten));
            Assert.Equal(("Bodenplatte", "Dach"), (ka.Boden, ka.Decke));
            Assert.Equal(new[] { "Aussenwand", "Aussenwand", "Aussenwand", "Innenwand" }, Assert.Single(kb.Kanten));
            Assert.Equal(("Bodenplatte", (string)null), (kb.Boden, kb.Decke));
            Assert.Equal(new[] { null, null, "Aussenwand", null }, Assert.Single(kc.Kanten));
            Assert.Null(kc.Boden);
            Assert.Null(kc.Decke);
            // Die Lage aus der Geschossangabe — Kennung wie im Geschoss der Ansicht.
            Assert.Equal(d.Geschosse.Select(g => g.Kennung), d.Geschosslagen.Select(l => l.Kennung));
            Assert.Null(d.Geschosslagen.Single(l => l.Kennung == "eg").LageM);
            Assert.Equal(3.2, d.Geschosslagen.Single(l => l.Kennung == "og").LageM);
        }

        [Fact]
        public void Die_Koerper_stapeln_ohne_Lage_und_setzen_ohne_Hoehe_die_Vorgabe_schematisch()
        {
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(Zonengeometrie.AusFlaechen(Eingang()), umhaengbar: true);
            IReadOnlyList<GebaeudeAnsichtKoerper> k = d.Koerper();
            Assert.Equal(new[] { "A", "B", "C" }, k.Select(x => x.Raum.Kennung));
            GebaeudeAnsichtKoerper a = k[0], b = k[1], c = k[2];
            Assert.Equal((0.0, 2.5, false), (a.UnterkanteM, a.HoeheM, a.HoeheVorgabe));
            Assert.Equal((0.0, 2.8, false), (b.UnterkanteM, b.HoeheM, b.HoeheVorgabe));
            // Obergeschoss ohne Lage: Unterkante = Unterkante EG + höchster Raum des EG (2,8 m); ohne Höhe die Vorgabe 3,0 m.
            Assert.Equal(2.8, c.UnterkanteM, 9);
            Assert.Equal(GebaeudeAnsichtDaten.VORGABEHOEHE_M, c.HoeheM);
            Assert.Equal(3.0, c.HoeheM);
            Assert.True(c.HoeheVorgabe);
            Assert.True(c.Schematisch);
            Assert.Same(d.Koerperraum("C"), c.Angaben);

            // Mit Lage gilt die Lage, nicht der Stapel.
            GebaeudeAnsichtDaten mitLage = GebaeudeImportAnsicht.AnsichtDaten(Zonengeometrie.AusFlaechen(Eingang(lageOg: 3.2)), umhaengbar: true);
            Assert.Equal(3.2, mitLage.Koerper().Single(x => x.Raum.Kennung == "C").UnterkanteM, 9);
        }

        /// <summary>
        /// <b>Dieselbe Geometrie in Ansicht und Datei:</b> Die Polygone der Ansicht sind die angelegten Rechtecke des
        /// Modells (verschoben und gestreckt) — dieselben wie die Körper der Datei; die Kante der Trennwand liegt in den
        /// Körperdaten beider Räume deckungsgleich, gegenläufig, und auf der Trennfläche des Dateikörpers.
        /// </summary>
        [Fact]
        public void Die_Ansicht_zeigt_die_angelegten_Umrisse_und_die_Trennwand_liegt_deckungsgleich()
        {
            Zonengeometrie z = Zonengeometrie.AusFlaechen(Eingang());
            Assert.True(Assert.Single(z.Nachbarpaare).Angelegt);
            Zonenkoerper koerper = Zonenkoerper.Bilden(z);
            GebaeudeAnsichtDaten d = GebaeudeImportAnsicht.AnsichtDaten(z, umhaengbar: true);

            foreach (string kennung in new[] { "A", "B" })
            {
                // Die Punkte der Ansicht sind die des angelegten Rechtecks im Modell.
                IReadOnlyList<GebaeudeAnsichtPunkt> ring = Assert.Single(d.Raum(kennung)!.Polygone);
                IReadOnlyList<double[]> modell = Assert.Single(z.Raum(kennung).Polygone).Punkte;
                Assert.Equal(4, ring.Count);
                for (int i = 0; i < 4; i++) Assert.Equal((modell[i][0], modell[i][1]), (ring[i].X, ring[i].Y));
            }
            // Der Körper der Datei (A: Höhe aus V/A) steht auf demselben Rechteck: der Boden der Schale ist der Ring in Folge 0, 3, 2, 1.
            IReadOnlyList<double[]> boden = koerper.Raum("A").Schale[0];
            int[] folge = { 0, 3, 2, 1 };
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(boden[i][0], d.Raum("A")!.Polygone[0][folge[i]].X, 6);
                Assert.Equal(boden[i][1], d.Raum("A")!.Polygone[0][folge[i]].Y, 6);
            }

            // Die Trennwand: Ostkante von A (Punkte 1 → 2) und Westkante von B (Punkte 3 → 0), gegenläufig deckungsgleich.
            IReadOnlyList<GebaeudeAnsichtPunkt> pa = d.Raum("A")!.Polygone[0], pb = d.Raum("B")!.Polygone[0];
            Assert.Equal("Innenwand", d.Koerperraum("A")!.Kanten[0][1]);
            Assert.Equal("Innenwand", d.Koerperraum("B")!.Kanten[0][3]);
            Punktgleich(pa[1], pb[0]);
            Punktgleich(pa[2], pb[3]);
            // … und auf der Trennfläche des Dateikörpers aus Sicht von A.
            Koerperflaeche t = koerper.Flaeche("A", "T");
            Assert.Equal(t.EckenM[0][0], pa[1].X, 6);
            Assert.Equal(t.EckenM[0][1], pa[1].Y, 6);
            Assert.Equal(t.EckenM[1][0], pa[2].X, 6);
            Assert.Equal(t.EckenM[1][1], pa[2].Y, 6);
            // Flächentreu gestreckt: B behält seine Fläche, seine Westkante ist so lang wie die Ostkante von A.
            Assert.Equal(5.0, Math.Abs(pb[0].Y - pb[3].Y), 9);
            Assert.Equal(30.0, Math.Abs(pb[1].X - pb[0].X) * Math.Abs(pb[3].Y - pb[0].Y), 9);
        }

        private static void Punktgleich(GebaeudeAnsichtPunkt x, GebaeudeAnsichtPunkt y)
        {
            Assert.Equal(x.X, y.X, 9);
            Assert.Equal(x.Y, y.Y, 9);
        }
    }
}
