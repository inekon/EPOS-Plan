using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle A — was der IFC-Leser für die Zonierung liefert</b> (Mehrzonenkonzept 6.1,
    /// 6.2): die Raumgrenzen je Bauteil samt Lage, die Fläche einer Grenze ohne Geometriekern und die
    /// Beheizungsregeln B1…B6. Ohne Datenbank.
    /// </summary>
    public sealed class ZonenimportLeserTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Die Fläche einer Grenze (6.2)
        // ==================================================================

        [Fact]
        public void Ein_Rechteck_in_einem_gedrehten_Raum_hat_Inhalt_Schwerpunkt_und_Normale_in_Weltkoordinaten()
        {
            // Raum um 90° um z gedreht und verschoben; Punkte in Millimetern, eine senkrechte Wand x = 0.
            IfcRahmen raum = IfcPlatzierung.Achsen3D(new[] { 1000.0, 2000.0, 3000.0 }, null, new[] { 0.0, 1.0, 0.0 });
            var ring = new List<double[]>
            {
                new[] { 0.0, 0.0, 0.0 }, new[] { 0.0, 4000.0, 0.0 }, new[] { 0.0, 4000.0, 3000.0 }, new[] { 0.0, 0.0, 3000.0 },
            };
            IfcGrenzgeometrie.Flaeche f = IfcGrenzgeometrie.Auswerten(ring, raum, 0.001);
            Assert.Null(f.Fehler);
            Assert.Equal(12.0, f.FlaecheM2, 9);
            // Lokal (0; 2; 1,5) m → Welt: x = 1 − 2 = −1, y = 2 + 0 = 2, z = 3 + 1,5 = 4,5.
            Assert.Equal(-1.0, f.SchwerpunktM[0], 9);
            Assert.Equal(2.0, f.SchwerpunktM[1], 9);
            Assert.Equal(4.5, f.SchwerpunktM[2], 9);
            Assert.Equal(1.0, Math.Abs(f.Normale[1]), 9);
        }

        [Fact]
        public void Eine_Berandung_die_mehr_als_einen_Millimeter_aus_der_Ebene_faellt_ist_nicht_eben()
        {
            var ring = new List<double[]>
            {
                new[] { 0.0, 0.0, 0.0 }, new[] { 4.0, 0.0, 0.0 }, new[] { 4.0, 3.0, 0.003 }, new[] { 0.0, 3.0, 0.0 },
            };
            // Eine um 3 mm gehobene Ecke: die Abstände zur Newell-Ebene streuen um 1,5 mm.
            Assert.Equal("nicht eben", IfcGrenzgeometrie.Auswerten(ring, IfcRahmen.Welt, 1.0).Fehler);
            ring[2] = new[] { 4.0, 3.0, 0.0015 };
            Assert.Null(IfcGrenzgeometrie.Auswerten(ring, IfcRahmen.Welt, 1.0).Fehler);
        }

        // ==================================================================
        //  Das Probenhaus: Grenzen je Bauteil und Beheizungsregeln
        // ==================================================================

        [Fact]
        public void Das_Probenhaus_traegt_Grenzen_je_Seite_und_die_Beheizungsregel_je_Raum()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);
            Assert.True(g.ZahlGrenzen > 0);
            Assert.Equal(g.ZahlGrenzen, g.ZahlGrenzenZweiteEbene);

            // Der Heizsollwert der Datei (20 °C) entscheidet vor dem Namen (B3); der Keller trägt keinen (B4).
            Assert.All(g.Raeume.Where(r => r.Name != "Keller"), r => Assert.Equal("B3", r.Beheizungsregel));
            AbbildRaum keller = Assert.Single(g.Raeume, r => r.Name == "Keller");
            Assert.Equal("B4", keller.Beheizungsregel);
            Assert.False(keller.Beheizt);

            // Die Kellerdecke grenzt an Wohnen, Küche und Keller — drei Grenzen INTERNAL, ohne Geometrie.
            AbbildBauteil decke = Assert.Single(g.Bauteile, b => b.Name == "Kellerdecke");
            Assert.Equal(3, decke.Grenzen.Count);
            Assert.All(decke.Grenzen, x =>
            {
                Assert.Equal(Randbedingung.Innen, x.Lage);
                Assert.Null(x.FlaecheM2);
                Assert.False(x.Virtuell);
                Assert.NotNull(x.RaumKennung);
            });
            Assert.Equal(g.Geschosse.Single(s => s.Name == "Erdgeschoss").Kennung, decke.GeschossKennung);
            Assert.Equal(new[] { "Keller", "Küche", "Wohnen" },
                         decke.Grenzen.Select(x => g.Raeume.Single(r => r.Kennung == x.RaumKennung).Name).OrderBy(n => n, StringComparer.Ordinal));

            // Eine Außenwand an zwei Räumen: zwei Grenzen EXTERNAL.
            AbbildBauteil sued = Assert.Single(g.Bauteile, b => b.Name == "EG Süd");
            Assert.Equal(2, sued.Grenzen.Count);
            Assert.All(sued.Grenzen, x => Assert.Equal(Randbedingung.Aussenluft, x.Lage));
        }
    }
}
