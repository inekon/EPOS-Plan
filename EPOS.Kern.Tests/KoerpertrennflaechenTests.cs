using System;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7f-4 — Trennflächen aus Raumkörpern im Ablauf</b> (Mehrzonenkonzept 6.2): die Probe
    /// <c>ifc4_koerper_nachbarn.ifc</c> (drei <c>IfcFacetedBrep</c>-Räume auf zwei Geschossen, Raumbezüge, keine
    /// Raumgrenzen) und ihre Gegenprobe <c>ifc4_koerper_nachbarn_grenzen.ifc</c> mit Raumgrenzen. Ohne Datenbank.
    /// </summary>
    public sealed class KoerpertrennflaechenTests
    {
        private const string P = "IMP_IFC_PROT_";

        private static GebaeudeImportAblauf Lesen(string datei)
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.Raumkoerperproben()[datei]))
                a.Lesen(s, datei, new IfcImportProfil());
            return a;
        }

        private static string Name(GebaeudeZonierung z, int zone) => z.Zonen[zone].Name;

        [Fact]
        public void Ohne_Raumgrenzen_tragen_die_Koerperpaare_die_Nachbarschaft()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_koerper_nachbarn.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.Equal(0, g.ZahlGrenzen);
            Assert.True(g.KoerperpaareGebildet);
            Assert.Equal(2, g.ZahlKoerperpaare);
            Assert.Equal(12.0, g.KoerperTrennwandM2, 6);
            Assert.Equal(20.0, g.KoerperTrenndeckeM2, 6);
            Assert.True(g.GeschosseGekoppelt);
            Assert.Single(g.Meldungen, m => m.Schluessel == P + "GRENZEN_AUS_KOERPER");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == P + "KOERPER_OHNE_PAAR" || m.Schluessel == P + "KOERPERPAAR_SCHWACH");
            // Wand und Decke der Datei gehen in den Körperpaaren auf.
            Assert.Equal(2, g.Bauteile.Count);
            Assert.Equal(2, g.Bauteile.Count(b => b.Grenzen.Count == 2 && b.Grenzen.All(x => x.Herkunft == Grenzherkunft.Koerper)));
            AbbildBauteil wand = g.Bauteile.Single(b => b.Art == Bauteilart.Innenwand);
            Assert.Equal(1.2, wand.UWertWm2K);
            Assert.Equal(wand.Grenzen[0].Kennung, wand.Grenzen[1].GegenstueckKennung);

            // Z4 (Vorgabe): zwei Geschosszonen, die Trenndecke aus den Körpern, die Wand innere Masse im EG.
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z4, z4.Regel);
            Assert.Equal(2, z4.Zonen.Count);
            Assert.DoesNotContain(z4.Meldungen, m => m.Schluessel.EndsWith(GebaeudeZonierung.GRENZEN_ENTKOPPELT, StringComparison.Ordinal));
            Zonentrennung decke = Assert.Single(z4.Trennungen);
            Assert.Equal(20.0, Math.Max(decke.FlaecheA, decke.FlaecheB), 6);
            Zonenflaeche innen = Assert.Single(z4.Flaechen, f => f.Bauteil == wand);
            Assert.Equal(Zonenrand.Innen, innen.Rand);
            Assert.True(innen.Beidseitig);
            Assert.Equal(12.0, innen.BruttoM2.Value, 6);

            // Z6 (zwei Temperaturen): die Wand ist Zonen-Trennfläche mit dem U-Wert der referenzierten Wand.
            GebaeudeZonierung z6 = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z6);
            Assert.Equal(2, z6.Zonen.Count);
            Assert.DoesNotContain(z6.Meldungen, m => m.Schluessel.EndsWith(GebaeudeZonierung.GRENZEN_ENTKOPPELT, StringComparison.Ordinal));
            Zonentrennung trennwand = Assert.Single(z6.Trennungen);
            Assert.Equal(12.0, Math.Max(trennwand.FlaecheA, trennwand.FlaecheB), 6);
            GebaeudeBauteilvorschlag v6 = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, IfcImportProfil.ZONENREGEL_Z6);
            // Je Paar eine Zeile mit Randbedingung ZONE.
            GebaeudeBauteilzeile r = Assert.Single(v6.Zeilen, x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE);
            Assert.Equal(1.2, r.Bauteil.U_Wert);
            Assert.Equal(12.0, r.Bauteil.Flaeche, 6);
            Assert.Equal(GebaeudeBauteilzeile.BELEG_KOERPER, r.Beleg?.Schluessel);
            Assert.Equal(new[] { "Büro", "Flur", "12" }, r.Beleg.Werte);
        }

        [Fact]
        public void Mit_Raumgrenzen_werden_die_Koerperpaare_nur_gezaehlt()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_koerper_nachbarn_grenzen.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            Assert.True(g.ZahlGrenzen > 0);
            Assert.False(g.KoerperpaareGebildet);
            Assert.Equal(2, g.ZahlKoerperpaare);
            Assert.Single(g.Meldungen, m => m.Schluessel == P + "KOERPERPAARE_GEZAEHLT");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == P + "GRENZEN_AUS_KOERPER");
            Assert.Equal(2, g.Bauteile.Count);
            Assert.All(g.Bauteile.SelectMany(b => b.Grenzen), x => Assert.Equal(Grenzherkunft.Raumgrenze, x.Herkunft));

            // Die Trenndecke kommt aus den Raumgrenzen der Datei, mit deren Fläche.
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            Zonentrennung decke = Assert.Single(z4.Trennungen);
            Assert.Equal(20.0, Math.Max(decke.FlaecheA, decke.FlaecheB), 6);
            Assert.Equal("Decke EG/OG", z4.Flaechen.First(f => f.Rand == Zonenrand.Zone).Bauteil.Name);
        }
    }
}
