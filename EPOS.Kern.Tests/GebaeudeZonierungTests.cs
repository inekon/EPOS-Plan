using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle A — die Zonierung</b> (<see cref="GebaeudeZonierung"/>; Mehrzonenkonzept 6.1,
    /// 6.2, 6.6; E50): Regeln und Vorgabe, Seiten je Zone, Paarbildung über die Geometrie, Gegenprobe,
    /// Mindestgröße und Obergrenze — an den Importproben und an kleinen Abbildern im Speicher. Ohne Datenbank.
    /// </summary>
    public sealed class GebaeudeZonierungTests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";
        private const string X = "IMP_GBXML_PROT_";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Kleine Abbilder im Speicher
        // ==================================================================

        /// <summary>Ein Abbild mit einem Gebäude im Format <paramref name="format"/>.</summary>
        internal static (GebaeudeAbbild Abbild, AbbildGebaeude Gebaeude) Abbild(string format = GebaeudeQuelle.FORMAT_IFC)
        {
            var a = new GebaeudeAbbild { Format = format };
            var g = new AbbildGebaeude { Kennung = "geb", Name = "Probe" };
            a.Gebaeude.Add(g);
            return (a, g);
        }

        internal static AbbildRaum Raum(AbbildGebaeude g, string kennung, double flaeche, string geschoss, string zone = null, bool beheizt = true)
        {
            var r = new AbbildRaum
            {
                Kennung = kennung, Name = kennung, FlaecheM2 = flaeche, VolumenM3 = flaeche * 2.5, GeschossKennung = geschoss,
                ZonenKennung = zone, ZonenName = zone, Beheizt = beheizt,
            };
            g.Raeume.Add(r);
            return r;
        }

        internal static AbbildGrenze Grenze(string raum, Randbedingung lage, double? flaeche = null, double[] punkt = null,
                                            double[] normale = null, string kennung = null, string gegenstueck = null)
            => new AbbildGrenze
            {
                Kennung = kennung ?? raum + "-" + Guid.NewGuid().ToString("N").Substring(0, 6), RaumKennung = raum, Lage = lage,
                FlaecheM2 = flaeche, SchwerpunktM = punkt, Normale = normale, GegenstueckKennung = gegenstueck,
            };

        internal static AbbildBauteil Bauteil(AbbildGebaeude g, string kennung, Bauteilart art, Randbedingung rand, double brutto,
                                              double? dicke, params AbbildGrenze[] grenzen)
        {
            var b = new AbbildBauteil
            {
                Kennung = kennung, Name = kennung, Quelltyp = "IfcWall", Art = art, Randbedingung = rand, BruttoflaecheM2 = brutto,
                DickeM = dicke, NeigungGrad = art == Bauteilart.Decke ? (double?)null : 90.0, AzimutGrad = rand == Randbedingung.Aussenluft ? 180.0 : (double?)null,
                UWertWm2K = 0.3,
            };
            b.Grenzen.AddRange(grenzen);
            g.ZahlGrenzen += grenzen.Length;
            g.ZahlGrenzenZweiteEbene += grenzen.Length;
            g.Bauteile.Add(b);
            return b;
        }

        private static bool Hat(GebaeudeZonierung z, string schluessel) => z.Meldungen.Any(m => m.Schluessel == schluessel);

        // ==================================================================
        //  Regeln und Vorgabe an den Importproben
        // ==================================================================

        [Fact]
        public void Das_Probenhaus_wird_je_Geschoss_zoniert_und_die_Decken_werden_Trennflaechen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.False(z.Abgelehnt);
            Assert.Equal("Z4", z.Vorgabe);
            Assert.Equal("Z4", z.Regel);
            Assert.Equal(new[] { "Z4", "Z5" }, z.Regeln);
            Assert.False(z.Einzonig);

            // Drei Zonen: der unbeheizte Keller, Erd- und Obergeschoss — in Dateifolge der Räume.
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { false, true, true }, z.Zonen.Select(x => x.IstBeheizt));
            Assert.Equal(new double?[] { 70.0, 65.0, 65.0 }, z.Zonen.Select(x => x.FlaecheM2));
            Assert.Equal(4.0, z.MindestflaecheM2, 9);

            // Kellerdecke und Geschossdecke: je eine Trennfläche von 80 m², von beiden Seiten gleich.
            Assert.Equal(2, z.Trennungen.Count);
            Assert.Contains(z.Trennungen, t => t.ZoneA == 0 && t.ZoneB == 1 && t.FlaecheA == 80.0 && t.FlaecheB == 80.0);
            Assert.Contains(z.Trennungen, t => t.ZoneA == 1 && t.ZoneB == 2 && t.FlaecheA == 80.0 && !t.Ungleich);
            // Die Innenwand im Erdgeschoss ist innere Masse beider Seiten.
            Zonenflaeche iw = Assert.Single(z.Flaechen, f => f.Bauteil.Name == "EG Innenwand");
            Assert.Equal(Zonenrand.Innen, iw.Rand);
            Assert.True(iw.Beidseitig);
            Assert.Equal(1, iw.Zone);
            // Die Südwand im EG grenzt an zwei Räume derselben Zone — ein Teil, die ganze Fläche.
            Zonenflaeche sued = Assert.Single(z.Flaechen, f => f.Bauteil.Name == "EG Süd");
            Assert.Equal(28.0, sued.BruttoM2);
            Assert.Equal(Zonenrand.Aussenluft, sued.Rand);
            Assert.Single(sued.Oeffnungen);
            // Die Kellerwände gehören zur Kellerzone, gegen Erdreich.
            Assert.All(z.Flaechen.Where(f => f.Bauteil.Name.StartsWith("KG ", StringComparison.Ordinal)),
                       f => { Assert.Equal(0, f.Zone); Assert.Equal(Zonenrand.Erdreich, f.Rand); });
            Assert.False(Hat(z, I + "OHNE_GEGENSTUECK"));
            Assert.True(Hat(z, I + "ZONENREGEL"));
        }

        [Fact]
        public void Z5_ist_der_Einzonenweg_ohne_unbeheizte_Raeume()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0, "Z5");
            Assert.True(z.Einzonig);
            Importzone zone = Assert.Single(z.Zonen);
            Assert.Equal(4, zone.Raeume.Count);
            Assert.True(zone.IstBeheizt);
            Assert.Empty(z.Flaechen);
        }

        [Fact]
        public void Eine_Regel_die_die_Datei_nicht_traegt_wird_benannt_abgelehnt()
        {
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(BauteilvorschlagProbe.Lesen("ifc4_haus.ifc").Abbild, 0, "Z1");
            Assert.True(z.Abgelehnt);
            Assert.True(Hat(z, I + "ZONENREGEL_UNGUELTIG"));
            Assert.Empty(z.Zonen);
        }

        [Fact]
        public void Das_gbXML_Haus_legt_X1_nahe_und_X2_bildet_drei_Geschosszonen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_haus_si.xml");
            GebaeudeZonierung x1 = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("X1", x1.Vorgabe);
            Assert.Equal(new[] { "X1", "X2", "X3", "X4" }, x1.Regeln);
            // X1: die Zone der Datei (drei beheizte Räume) und der Keller ohne Zone, unbeheizt.
            Assert.Equal(2, x1.Zonen.Count);
            Assert.Equal(3, x1.Zonen[0].Raeume.Count);
            Assert.False(x1.Zonen[1].IstBeheizt);

            GebaeudeZonierung x2 = GebaeudeZonierung.Bilden(a.Abbild, 0, "X2");
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss", "Keller" }, x2.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { true, true, false }, x2.Zonen.Select(z => z.IstBeheizt));
            Assert.Contains(x2.Trennungen, t => t.ZoneA == 0 && t.ZoneB == 1);
            Assert.Contains(x2.Trennungen, t => t.ZoneA == 0 && t.ZoneB == 2);
            Assert.All(x2.Trennungen, t => Assert.False(t.Ungleich));
            Assert.False(Hat(x2, X + "OHNE_GEGENSTUECK"));
            // Die Innenwand zwischen Wohnen und Küche bleibt innere Masse des Erdgeschosses.
            Zonenflaeche iw = Assert.Single(x2.Flaechen, f => f.Bauteil.Kennung == "iw-eg");
            Assert.Equal(Zonenrand.Innen, iw.Rand);

            // X3: eine Zone je Raum — die Innenwand wird Trennfläche.
            GebaeudeZonierung x3 = GebaeudeZonierung.Bilden(a.Abbild, 0, "X3");
            Assert.Equal(4, x3.Zonen.Count);
            Assert.Contains(x3.Flaechen, f => f.Bauteil.Kennung == "iw-eg" && f.Rand == Zonenrand.Zone);
        }

        // ==================================================================
        //  Paarbildung über die Geometrie (M13) und Gegenprobe
        // ==================================================================

        /// <summary>Eine Decke unter vier Räumen in drei Zonen (Z1): die Paare folgen allein aus der Geometrie.</summary>
        private static (GebaeudeAbbild, AbbildGebaeude) Deckenprobe(double flaecheD, string gegenstueckB = null)
        {
            (GebaeudeAbbild a, AbbildGebaeude g) = Abbild();
            Raum(g, "A", 48, "eg", "za");
            Raum(g, "B", 32, "eg", "zb");
            Raum(g, "C", 48, "og", "zc");
            Raum(g, "D", flaecheD, "og", "zc");
            double[] hoch = { 0, 0, 1 }, runter = { 0, 0, -1 };
            Bauteil(g, "decke", Bauteilart.Decke, Randbedingung.Innen, 80, 0.2,
                Grenze("A", Randbedingung.Innen, 48, new[] { 3.0, 4.0, 2.8 }, hoch, "gA"),
                Grenze("B", Randbedingung.Innen, 32, new[] { 8.0, 4.0, 2.8 }, hoch, "gB", gegenstueckB),
                Grenze("C", Randbedingung.Innen, 48, new[] { 3.0, 4.0, 3.0 }, runter, "gC"),
                Grenze("D", Randbedingung.Innen, flaecheD, new[] { 8.0, 4.0, 3.0 }, runter, "gD", gegenstueckB == null ? null : "gB"));
            foreach (string r in new[] { "A", "B", "C", "D" })
                Bauteil(g, "aw-" + r, Bauteilart.Aussenwand, Randbedingung.Aussenluft, 20, 0.3, Grenze(r, Randbedingung.Aussenluft, 20));
            return (a, g);
        }

        [Fact]
        public void Drei_Zonen_an_einer_Decke_paaren_sich_ueber_Flaeche_Abstand_und_Normale()
        {
            (GebaeudeAbbild a, _) = Deckenprobe(32);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "Z1");
            Assert.Equal(new[] { "za", "zb", "zc" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(2, z.Trennungen.Count);
            Assert.Contains(z.Trennungen, t => t.ZoneA == 0 && t.ZoneB == 2 && t.FlaecheA == 48.0 && t.FlaecheB == 48.0);
            Assert.Contains(z.Trennungen, t => t.ZoneA == 1 && t.ZoneB == 2 && t.FlaecheA == 32.0 && t.FlaecheB == 32.0);
            Assert.False(Hat(z, I + "OHNE_GEGENSTUECK"));
        }

        [Fact]
        public void Ohne_passende_Geometrie_bleibt_eine_Grenze_ohne_Gegenstueck_und_grenzt_an_unbeheizt()
        {
            (GebaeudeAbbild a, _) = Deckenprobe(30);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "Z1");
            // A↔C paart sich; B (32) und D (30) weichen um mehr als 1 % ab — beide ohne Gegenstück.
            Assert.Single(z.Trennungen);
            List<Zonenflaeche> ohne = z.Flaechen.Where(f => f.OhneGegenstueck).ToList();
            Assert.Equal(2, ohne.Count);
            Assert.All(ohne, f => Assert.Equal(Zonenrand.Unbeheizt, f.Rand));
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == I + "OHNE_GEGENSTUECK");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
        }

        [Fact]
        public void Das_Gegenstueck_der_Datei_paart_vor_der_Geometrie_und_die_Gegenprobe_meldet_ab_zwei_Prozent()
        {
            (GebaeudeAbbild a, _) = Deckenprobe(30, gegenstueckB: "gD");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "Z1");
            Zonentrennung bc = Assert.Single(z.Trennungen, t => t.ZoneA == 1);
            Assert.Equal(32.0, bc.FlaecheA);
            Assert.Equal(30.0, bc.FlaecheB);
            Assert.True(bc.Ungleich);
            Assert.True(Hat(z, I + "TRENNFLAECHE_UNGLEICH"));
            Assert.False(Hat(z, I + "OHNE_GEGENSTUECK"));
            // Gerechnet wird mit der größeren Beschreibung.
            Assert.All(z.Flaechen.Where(f => f.Rand == Zonenrand.Zone && (f.Zone == 1 || f.Nachbarzone == 1)), f => Assert.Equal(32.0, f.GroessereM2));

            // Unter Z4 liegt die Decke zwischen zwei Zonen: 80 gegen 78 m² — 2,5 %.
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a, 0, "Z4");
            Zonentrennung t = Assert.Single(z4.Trennungen);
            Assert.Equal(80.0, t.FlaecheA);
            Assert.Equal(78.0, t.FlaecheB);
            Assert.True(t.Ungleich);
        }

        [Fact]
        public void Eine_Aussenwand_ueber_zwei_Geschosse_teilt_sich_nach_Polygonen_sonst_nach_der_Zahl_der_Grenzen()
        {
            (GebaeudeAbbild a, AbbildGebaeude g) = Abbild();
            Raum(g, "A", 40, "eg");
            Raum(g, "C", 40, "og");
            Bauteil(g, "fassade", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 60, 0.3,
                Grenze("A", Randbedingung.Aussenluft, 28), Grenze("C", Randbedingung.Aussenluft, 26));
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "Z4");
            Assert.Equal(new double?[] { 28.0, 26.0 }, z.Flaechen.Where(f => f.Bauteil.Kennung == "fassade").Select(f => f.BruttoM2));
            Assert.False(Hat(z, I + "FLAECHE_AUFGETEILT"));

            (GebaeudeAbbild b, AbbildGebaeude h) = Abbild();
            Raum(h, "A", 40, "eg");
            Raum(h, "C", 40, "og");
            Bauteil(h, "fassade", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 60, 0.3,
                Grenze("A", Randbedingung.Aussenluft), Grenze("C", Randbedingung.Aussenluft));
            GebaeudeZonierung y = GebaeudeZonierung.Bilden(b, 0, "Z4");
            Assert.Equal(new double?[] { 30.0, 30.0 }, y.Flaechen.Where(f => f.Bauteil.Kennung == "fassade").Select(f => f.BruttoM2));
            Assert.All(y.Flaechen, f => Assert.True(f.Aufgeteilt));
            Assert.True(Hat(y, I + "FLAECHE_AUFGETEILT"));
        }

        // ==================================================================
        //  Mindestgröße (M8) und Obergrenze (M12)
        // ==================================================================

        [Fact]
        public void Eine_zu_kleine_Zone_geht_an_den_Nachbarn_gleicher_Beheizung_mit_der_groessten_Trennflaeche()
        {
            (GebaeudeAbbild a, AbbildGebaeude g) = Abbild();
            Raum(g, "A", 48, "eg", "za");
            Raum(g, "B", 32, "eg", "zb");
            Raum(g, "E", 1.5, "eg", "ze");
            Raum(g, "F", 1.0, "eg", "zf", beheizt: false);
            Bauteil(g, "iw-ab", Bauteilart.Innenwand, Randbedingung.Innen, 20, 0.1, Grenze("A", Randbedingung.Innen), Grenze("B", Randbedingung.Innen));
            Bauteil(g, "iw-ae", Bauteilart.Innenwand, Randbedingung.Innen, 3, 0.1, Grenze("A", Randbedingung.Innen), Grenze("E", Randbedingung.Innen));
            Bauteil(g, "iw-be", Bauteilart.Innenwand, Randbedingung.Innen, 5, 0.1, Grenze("B", Randbedingung.Innen), Grenze("E", Randbedingung.Innen));
            Bauteil(g, "iw-bf", Bauteilart.Innenwand, Randbedingung.Innen, 4, 0.1, Grenze("B", Randbedingung.Innen), Grenze("F", Randbedingung.Innen));
            foreach (string r in new[] { "A", "B", "E", "F" })
                Bauteil(g, "aw-" + r, Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, 0.3, Grenze(r, Randbedingung.Aussenluft, 10));

            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "Z1");
            Assert.Equal(2.0, z.MindestflaecheM2, 9);   // max(2 m², 2 % von 82,5 m²)
            // E (1,5 m²) geht an B — die größere gemeinsame Fläche (5 gegen 3 m²); F bleibt: kein unbeheizter Nachbar.
            Assert.Equal(new[] { "za", "zb", "zf" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "B", "E" }, z.Zonen[1].Raeume.Select(r => r.Kennung));
            Assert.Equal(new[] { "ze" }, z.Zonen[1].Zugeschlagen);
            Assert.True(z.Zonen[2].ZuKlein);
            Assert.True(Hat(z, I + "ZONE_ZUGESCHLAGEN"));
            Assert.True(Hat(z, I + "ZONE_ZU_KLEIN"));
            // Die Wand B/E liegt jetzt innerhalb der Zone, A/E ist Trennfläche zu ihr.
            Assert.Equal(Zonenrand.Innen, Assert.Single(z.Flaechen, f => f.Bauteil.Kennung == "iw-be").Rand);
            Assert.Equal(new[] { 1, 0 }, z.Flaechen.Where(f => f.Bauteil.Kennung == "iw-ae").Select(f => f.Nachbarzone));
        }

        [Fact]
        public void Mehr_als_fuenfzig_Zonen_warnen_und_schlagen_die_Geschossregel_vor()
        {
            (GebaeudeAbbild a, AbbildGebaeude g) = Abbild(GebaeudeQuelle.FORMAT_GBXML);
            for (int i = 0; i < 60; i++)
            {
                string r = "r" + i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
                Raum(g, r, 10, i < 30 ? "eg" : "og");
                AbbildBauteil w = Bauteil(g, "aw-" + r, Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, 0.3);
                w.Nachbarn.Add(new AbbildNachbar(r, null));
            }
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "X3");
            Assert.Equal(60, z.Zonen.Count);   // alle zu klein (12 m²), aber ohne Nachbarn
            Assert.True(z.ZuVieleZonen);
            Assert.Equal("X2", z.Vorschlagsregel);
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == X + "ZU_VIELE_ZONEN_VORSCHLAG");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "60", "50", "X2", "2" }, m.Werte);

            GebaeudeZonierung x2 = GebaeudeZonierung.Bilden(a, 0, "X2");
            Assert.Equal(2, x2.Zonen.Count);
            Assert.False(x2.ZuVieleZonen);
        }

        [Fact]
        public void Ohne_Raumgrenzen_ist_eine_Zone_die_Vorgabe_und_Z4_nur_mit_Warnung_zu_haben()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_rueckfaelle.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            Assert.True(g.ZahlGrenzen > 0);
            // Das Rückfallhaus führt Grenzen; ohne sie (alle entfernt) gilt Z5.
            g.ZahlGrenzen = 0;
            g.ZahlGrenzenZweiteEbene = 0;
            foreach (AbbildBauteil b in g.Bauteile) b.Grenzen.Clear();
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z5", z.Vorgabe);
            Assert.True(Hat(z, I + "KEINE_GRENZEN"));
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0, "Z4");
            Assert.False(z4.Abgelehnt);
            Assert.Equal(2, z4.Zonen.Count);
            Assert.True(Hat(z4, I + "GRENZEN_ENTKOPPELT"));
        }
    }
}
