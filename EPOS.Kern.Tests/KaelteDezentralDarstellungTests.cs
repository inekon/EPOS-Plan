using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// K6b (Konzept Kältebedarf 4.3): die dezentrale (Split-)Deckung in Kältegang, Berichtsbild, Farbrolle und Deckungsgrad —
    /// mit Split-Zeile die neue Säule und die zwei Zahlen, ohne sie Zeichen für Zeichen das Bild von vorher.
    /// </summary>
    public class KaelteDezentralDarstellungTests
    {
        private static double[] Reihe(double wert) => Enumerable.Repeat(wert, 8760).ToArray();

        private static KaelteProduktionBild.Reihen Satz(double wp, double split, double rest)
        {
            var r = new KaelteProduktionBild.Reihen { Bedarf = Reihe(wp + split + rest), Rest = Reihe(rest) };
            r.Erzeuger.Add(new KaelteProduktionBild.Erzeugerreihe { Name = "", Werte = Reihe(wp) });
            if (split > 0) r.Dezentral = Reihe(split);
            return r;
        }

        [Fact]
        public void Die_Split_Saeule_steht_nach_den_Erzeugern_vor_dem_Rest()
        {
            var (stapel, linien) = KaelteProduktionBild.Reihenbilden(Satz(3.0, 0.5, 1.0),
                new KaelteProduktionBild.Texte { Erzeuger = "WP", Rest = "Rest", Bedarf = "Bedarf", Dezentral = "Split" }, false);

            Assert.Equal(new[] { "WP", "Split", "Rest" }, stapel.Select(s => s.Name).ToArray());
            Assert.Equal(new[] { Farbrolle.WAERME_WP, Farbrolle.KAELTE_DEZENTRAL, Farbrolle.REST },
                         stapel.Select(s => s.Rolle).ToArray());
            ChartRenderer.Reihe bedarf = Assert.Single(linien);
            for (int h = 0; h < 8760; h += 997)
                Assert.Equal(bedarf.Werte[h], stapel.Sum(s => s.Werte[h]), 12);
        }

        /// <summary>Gegenprobe: ohne Split-Zeile (leere Reihe oder lauter Nullen) bleibt das Bild byte-gleich.</summary>
        [Fact]
        public void Ohne_Split_bleibt_das_Bild_byte_gleich()
        {
            KaelteProduktionBild.Reihen ohne = Satz(3.0, 0.0, 1.0);
            KaelteProduktionBild.Reihen nullen = Satz(3.0, 0.0, 1.0);
            nullen.Dezentral = Reihe(0.0);
            Assert.Empty(ohne.Dezentral);
            Assert.Equal(KaelteProduktionBild.Png(ohne), KaelteProduktionBild.Png(nullen));
            Assert.DoesNotContain(KaelteProduktionBild.Reihenbilden(nullen, null, false).Stapel,
                                  s => s.Rolle == Farbrolle.KAELTE_DEZENTRAL);
            Assert.NotEqual(KaelteProduktionBild.Png(ohne), KaelteProduktionBild.Png(Satz(3.0, 0.5, 1.0)));
        }

        [Fact]
        public void Die_Farbrolle_hat_eine_eigene_Hausfarbe_und_einen_Namen()
        {
            Assert.True(Farbpalette.Vorgabe.Kennt(Farbrolle.KAELTE_DEZENTRAL));
            Assert.Equal(new Farbe(0x4B, 0xAC, 0xC6), Farbpalette.Vorgabe[Farbrolle.KAELTE_DEZENTRAL]);
            Assert.Contains(Farbrolle.KAELTE_DEZENTRAL, Diagrammfarben.Rollen);
            Assert.DoesNotContain(Diagrammfarben.Rollen.Where(r => r != Farbrolle.KAELTE_DEZENTRAL),
                                  r => Farbpalette.Vorgabe[r].Equals(Farbpalette.Vorgabe[Farbrolle.KAELTE_DEZENTRAL]));
        }

        [Fact]
        public void Der_Deckungsgrad_mit_Split_ist_Hauptzahl_und_Kaeltefolge()
        {
            // 60 MWh Kältefolge, 20 MWh dezentral, 100 MWh Bedarf: 80 % gesamt, 75 % des zentral zu deckenden Bedarfs.
            Assert.Equal(80.0, KaelteDeckungsgrad.Hauptzahl(60, 20, 100).Value, 9);
            Assert.Equal(75.0, KaelteDeckungsgrad.DavonKaeltefolge(60, 20, 100).Value, 9);
            Assert.Equal(100.0, KaelteDeckungsgrad.Hauptzahl(0, 100, 100).Value, 9);
            Assert.Null(KaelteDeckungsgrad.DavonKaeltefolge(0, 100, 100));
            // Ohne Split-Zeile keine Zahl - die gespeicherten Größen bleiben, wie sie sind.
            Assert.Null(KaelteDeckungsgrad.Hauptzahl(60, 0, 100));
            Assert.Null(KaelteDeckungsgrad.DavonKaeltefolge(60, 0, 100));
        }

        [Fact]
        public void Das_Berichtsbild_traegt_die_Split_Flaeche_nur_mit_ihrer_Reihe()
        {
            var z = new ZeitreihenSatz();
            double[] bedarf = Reihe(4.0);
            z.Reihen[ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG)] = bedarf;
            z.Reihen[ZeitreihenSatz.KAELTE_PRAEFIX + "1"] = Reihe(2.0);
            z.Beschriftungen[ZeitreihenSatz.KAELTE_PRAEFIX + "1"] = "";
            z.Kaeltereihen.Add(ZeitreihenSatz.KAELTE_PRAEFIX + "1");
            z.Reihen[ZeitreihenSatz.KAELTEREST] = Reihe(1.0);

            Berichtsbilder.Ergebnisbildplan ohne = Berichtsbilder.ErgebnisbildPlan("kaelte_produktion", z);
            Assert.Equal(2, ohne.Stapel.Count);

            z.Reihen[ZeitreihenSatz.KAELTE_DEZENTRAL] = Reihe(1.0);
            Berichtsbilder.Ergebnisbildplan mit = Berichtsbilder.ErgebnisbildPlan("kaelte_produktion", z);
            Assert.Equal(3, mit.Stapel.Count);
            Assert.Equal(Farbrolle.KAELTE_DEZENTRAL, mit.Stapel[1].Rolle);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.CHART_LEGENDE_KAELTE_DEZENTRAL, mit.Stapel[1].Name);
        }
    }
}
