using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle D1 — das Zonengeometrie-Modell</b> (<see cref="Zonengeometrie"/>,
    /// <see cref="GebaeudeGrundriss"/>; Entscheid E11, Mehrzonenkonzept 6.7, Softwarearchitektur 1.3): die
    /// Randpunktringe der Raumgrenzen (IFC) und der <c>PolyLoop</c> (gbXML) in Weltkoordinaten, die Umrisse
    /// aus Boden- bzw. Deckengrenzen samt Kantenverweisen, der Rechteckersatz nach Formel mit Reihung je
    /// Geschoss und der Determinismus. Ohne Datenbank.
    /// </summary>
    public sealed class ZonengeometrieTests : IDisposable
    {
        private const string ZONENHAUS = "ifc4_zonen.ifc";
        private const string ZELLEN = "gbxml_zonen_viele.xml";
        private const double Genau = 1e-9;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static AbbildRaum Raum(AbbildGebaeude g, string name) => Assert.Single(g.Raeume, r => r.Name == name);

        private static void Punkt(double[] erwartet, double[] ist)
        {
            Assert.Equal(erwartet.Length, ist.Length);
            for (int i = 0; i < erwartet.Length; i++) Assert.Equal(erwartet[i], ist[i], Genau);
        }

        private static void Ring(double[][] erwartet, IReadOnlyList<double[]> ist)
        {
            Assert.NotNull(ist);
            Assert.Equal(erwartet.Length, ist.Count);
            for (int i = 0; i < erwartet.Length; i++) Punkt(erwartet[i], ist[i]);
        }

        // ==================================================================
        //  Randpunkte (Auftrag D1, Punkt 1)
        // ==================================================================

        [Fact]
        public void Der_Randpunktring_einer_Grenze_steht_in_Weltkoordinaten_in_der_Reihenfolge_der_Datei()
        {
            // Raum um 90° um z gedreht und verschoben; Punkte in Millimetern, eine senkrechte Wand x = 0.
            IfcRahmen raum = IfcPlatzierung.Achsen3D(new[] { 1000.0, 2000.0, 3000.0 }, null, new[] { 0.0, 1.0, 0.0 });
            var ring = new List<double[]>
            {
                new[] { 0.0, 0.0, 0.0 }, new[] { 0.0, 4000.0, 0.0 }, new[] { 0.0, 4000.0, 3000.0 }, new[] { 0.0, 0.0, 3000.0 },
            };
            IfcGrenzgeometrie.Flaeche f = IfcGrenzgeometrie.Auswerten(ring, raum, 0.001);
            Assert.Null(f.Fehler);
            // Lokal y wird Welt −x: (0; 4; 0) m → (1 − 4; 2; 3).
            Ring(new[] { new[] { 1.0, 2.0, 3.0 }, new[] { -3.0, 2.0, 3.0 }, new[] { -3.0, 2.0, 6.0 }, new[] { 1.0, 2.0, 6.0 } }, f.RandpunkteM);
            Assert.Equal(12.0, f.FlaecheM2, 9);
            Assert.True(Zonengeometrie.Ebenheit(f.RandpunkteM) <= Zonengeometrie.EBEN_TOLERANZ_M);

            // Nicht eben: keine Geometrie, also auch kein Ring.
            var schief = new List<double[]> { new[] { 0.0, 0.0, 0.0 }, new[] { 4.0, 0.0, 0.0 }, new[] { 4.0, 3.0, 0.003 }, new[] { 0.0, 3.0, 0.0 } };
            IfcGrenzgeometrie.Flaeche s = IfcGrenzgeometrie.Auswerten(schief, IfcRahmen.Welt, 1.0);
            Assert.Equal("nicht eben", s.Fehler);
            Assert.Null(s.RandpunkteM);
            Assert.Equal(IfcGrenzgeometrie.EBEN_TOLERANZ_M, Zonengeometrie.EBEN_TOLERANZ_M);
        }

        [Fact]
        public void Die_Zonenprobe_traegt_die_Randpunkte_an_Fassade_und_Geschossdecke()
        {
            AbbildGebaeude g = Assert.Single(BauteilvorschlagProbe.Lesen(ZONENHAUS).Abbild.Gebaeude);

            // Die Fassade Süd: vier Grenzen, je ein Ring mit vier Punkten auf y = 0 — ohne Schlusspunkt.
            AbbildBauteil fassade = Assert.Single(g.Bauteile, b => b.Name == "Fassade Süd");
            Assert.All(fassade.Grenzen, x => Assert.Equal(4, x.RandpunkteM.Count));
            Assert.All(fassade.Grenzen.SelectMany(x => x.RandpunkteM), p => Assert.Equal(0.0, p[1], Genau));
            AbbildGrenze wohnen = fassade.Grenzen.Single(x => x.RaumKennung == Raum(g, "Wohnen").Kennung);
            Ring(new[] { new[] { 0.0, 0.0, 0.0 }, new[] { 6.0, 0.0, 0.0 }, new[] { 6.0, 0.0, 2.8 }, new[] { 0.0, 0.0, 2.8 } }, wohnen.RandpunkteM);

            // Die Geschossdecke: der Boden des Bads im OG (z = 3 m) in der Reihenfolge der Datei, eben.
            AbbildBauteil decke = Assert.Single(g.Bauteile, b => b.Name == "Geschossdecke");
            Assert.Equal(5, decke.Grenzen.Count(x => x.RandpunkteM != null));
            AbbildGrenze bad = decke.Grenzen.Single(x => x.RaumKennung == Raum(g, "Bad").Kennung);
            Ring(new[] { new[] { 6.0, 0.0, 3.0 }, new[] { 6.0, 7.5, 3.0 }, new[] { 10.0, 7.5, 3.0 }, new[] { 10.0, 0.0, 3.0 } }, bad.RandpunkteM);
            Assert.All(decke.Grenzen, x => Assert.True(Zonengeometrie.Ebenheit(x.RandpunkteM) <= Zonengeometrie.EBEN_TOLERANZ_M));
            // Die Fläche der Grenze ist die ihres Rings.
            Assert.Equal(30.0, bad.FlaecheM2.Value, Genau);

            // Eine Grenze ohne Geometrie trägt keinen Ring.
            AbbildBauteil kellerdecke = Assert.Single(g.Bauteile, b => b.Name == "Kellerdecke");
            Assert.All(kellerdecke.Grenzen, x => Assert.Null(x.RandpunkteM));
        }

        [Fact]
        public void Die_gbXML_Probe_traegt_den_PolyLoop_als_Ring_in_Metern()
        {
            AbbildGebaeude si = Assert.Single(BauteilvorschlagProbe.Lesen("gbxml_haus_si.xml").Abbild.Gebaeude);
            AbbildGebaeude fuss = Assert.Single(BauteilvorschlagProbe.Lesen("gbxml_haus_fuss.xml").Abbild.Gebaeude);

            // Die Ostwand des Obergeschosses steht nur als PolyLoop: vier Punkte in Metern, Reihenfolge der Datei.
            double[][] ost = { new[] { 12.0, 0.0, 2.5 }, new[] { 12.0, 5.0, 2.5 }, new[] { 12.0, 5.0, 5.0 }, new[] { 12.0, 0.0, 5.0 } };
            AbbildBauteil siOst = Assert.Single(si.Bauteile, b => b.Kennung == "aw-og-ost");
            Ring(ost, siOst.RandpunkteM);
            Assert.Equal(12.5, siOst.BruttoflaecheM2.Value, Genau);
            // Dieselbe Wand in Fuß: umgerechnet derselbe Ring.
            AbbildBauteil fussOst = Assert.Single(fuss.Bauteile, b => b.Kennung == "aw-og-ost");
            Assert.Equal(4, fussOst.RandpunkteM.Count);
            for (int i = 0; i < 4; i++)
                for (int k = 0; k < 3; k++) Assert.Equal(ost[i][k], fussOst.RandpunkteM[i][k], 1e-6);

            // Das Dach trägt Rechteck UND PolyLoop: der Ring steht trotzdem (z = 5), die Fläche kommt weiter aus dem Rechteck.
            AbbildBauteil dach = Assert.Single(si.Bauteile, b => b.RandpunkteM != null && b.Art == Bauteilart.Dach);
            Assert.Equal(4, dach.RandpunkteM.Count);
            Assert.All(dach.RandpunkteM, p => Assert.Equal(5.0, p[2], Genau));
            // Ohne PolyLoop kein Ring; genau diese zwei Flächen tragen einen.
            Assert.Equal(2, si.Bauteile.Count(b => b.RandpunkteM != null));
            Assert.DoesNotContain(si.Bauteile.SelectMany(b => b.Meldungen), m => m.Schluessel.EndsWith("UMRISS_NICHT_EBEN", StringComparison.Ordinal));
        }

        [Fact]
        public void Ein_unebener_PolyLoop_wird_benannt_weggelassen_und_die_Flaeche_bleibt()
        {
            static string Punkt3(double x, double y, double z)
                => "<CartesianPoint><Coordinate>" + x.ToString(CultureInfo.InvariantCulture) + "</Coordinate><Coordinate>"
                   + y.ToString(CultureInfo.InvariantCulture) + "</Coordinate><Coordinate>" + z.ToString(CultureInfo.InvariantCulture)
                   + "</Coordinate></CartesianPoint>";
            string flaeche(string id, double hub)
                => "<Surface id=\"" + id + "\" surfaceType=\"ExteriorWall\"><AdjacentSpaceId spaceIdRef=\"raum-1\"/><PlanarGeometry><PolyLoop>"
                   + Punkt3(0, 0, 0) + Punkt3(4, 0, 0) + Punkt3(4, hub, 3) + Punkt3(0, 0, 3) + "</PolyLoop></PlanarGeometry></Surface>";
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(flaeche("schief", 0.003) + flaeche("gerade", 0.0)));
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);

            AbbildBauteil schief = Assert.Single(g.Bauteile, b => b.Kennung == "schief");
            Assert.Null(schief.RandpunkteM);
            PruefMeldung m = Assert.Single(schief.Meldungen, x => x.Schluessel == "IMP_GBXML_PROT_UMRISS_NICHT_EBEN");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "schief", "1.5" }, m.Werte);
            // Die Fläche kommt wie bisher aus dem PolyLoop.
            Assert.True(schief.BruttoflaecheM2 > 11.9);
            AbbildBauteil gerade = Assert.Single(g.Bauteile, b => b.Kennung == "gerade");
            Assert.Equal(4, gerade.RandpunkteM.Count);
            Assert.Equal(12.0, gerade.BruttoflaecheM2.Value, Genau);
        }

        [Fact]
        public void Der_gbXML_Leser_liest_die_Hoehenlage_der_Geschosse()
        {
            AbbildGebaeude g = Assert.Single(BauteilvorschlagProbe.Lesen(ZELLEN).Abbild.Gebaeude);
            Assert.All(g.Raeume.Where(r => r.GeschossKennung == "gs-eg"), r => Assert.Equal(0.0, r.GeschossLageM));
            Assert.All(g.Raeume.Where(r => r.GeschossKennung == "gs-og"), r => Assert.Equal(2.5, r.GeschossLageM));
            // gbXML führt weiter keine Geschosse am Gebäude — die Lage steht am Raum.
            Assert.Empty(g.Geschosse);
        }

        // ==================================================================
        //  Die Zonengeometrie aus den Proben (Punkt 2)
        // ==================================================================

        [Fact]
        public void Das_Zonenhaus_je_Geschoss_hat_Umrisse_aus_Boden_Decke_und_Rechteck_mit_Kantenverweisen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(ZONENHAUS);
            AbbildGebaeude ab = a.Abbild.Gebaeude[0];
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal("Z4", z.Regel);
            Zonengeometrie g = GebaeudeGrundriss.Bilden(a.Abbild, 0, z);

            // Drei Geschosse nach Höhenlage, drei Zonen in der Rangfolge der Zonierung, je ein Umriss.
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, g.Geschosse.Select(s => s.Name));
            Assert.Equal(new double?[] { -3.0, 0.0, 3.0 }, g.Geschosse.Select(s => s.LageM));
            Assert.Equal(z.Zonen.Select(x => x.Schluessel), g.Zonen.Select(x => x.Schluessel));
            Assert.Equal(3, g.Umrisse.Count);
            Assert.Equal(new[] { (0, 0), (1, 1), (2, 2) }, g.Umrisse.Select(u => (u.Zone, u.Geschoss)));
            Assert.Equal(new[] { Geometrieherkunft.Schematisch, Geometrieherkunft.Raumgrenzen, Geometrieherkunft.Raumgrenzen },
                         g.Zonen.Select(x => x.Herkunft));
            Assert.True(g.Schematisch);
            Assert.True(g.Geschosse[0].Schematisch);
            Assert.False(g.Geschosse[1].Schematisch);

            // Obergeschoss aus den Bodengrenzen, Erdgeschoss aus den Deckengrenzen: je Raum ein Polygon,
            // dessen Fläche die seiner Grenze ist.
            AbbildBauteil decke = Assert.Single(ab.Bauteile, b => b.Name == "Geschossdecke");
            foreach ((string name, Umrissherleitung herleitung) in new[]
                     {
                         ("Wohnen", Umrissherleitung.Decke), ("Küche", Umrissherleitung.Decke), ("Schlafen", Umrissherleitung.Boden),
                         ("Bad", Umrissherleitung.Boden), ("Abstellraum", Umrissherleitung.Boden),
                     })
            {
                Raumumriss r = g.Raum(Raum(ab, name).Kennung);
                Assert.Equal(Geometrieherkunft.Raumgrenzen, r.Herkunft);
                Assert.Equal(herleitung, r.Herleitung);
                Umrisspolygon p = Assert.Single(r.Polygone);
                AbbildGrenze grenze = decke.Grenzen.Single(x => x.RaumKennung == r.RaumKennung);
                Assert.Equal(grenze.FlaecheM2.Value, p.FlaecheM2, 1e-9);
                Assert.Equal(grenze.Kennung, p.Quelle.Kennung);
                Assert.Equal(r.FlaecheM2.Value, p.FlaecheM2, 1e-9);
                Assert.True(Zonengeometrie.DoppelteFlaeche(p.Punkte) > 0.0, name + ": gegen den Uhrzeigersinn");
                Assert.Equal(4, p.Kanten.Count);
            }
            // Die Räume eines Geschosses passen zueinander: Wohnen 0…6, Küche 6…10 (Weltkoordinaten).
            Assert.Equal(new[] { 0.0, 6.0 }, g.Raum(Raum(ab, "Wohnen").Kennung).Polygone[0].Punkte.Select(q => q[0]).Distinct().OrderBy(x => x));
            Assert.Equal(new[] { 6.0, 10.0 }, g.Raum(Raum(ab, "Küche").Kennung).Polygone[0].Punkte.Select(q => q[0]).Distinct().OrderBy(x => x));
            Assert.Equal(2.8, g.Raum(Raum(ab, "Wohnen").Kennung).Polygone[0].EbeneM.Value, 1e-9);
            Assert.Equal(3.0, g.Raum(Raum(ab, "Bad").Kennung).Polygone[0].EbeneM.Value, 1e-9);

            // Kantenverweise: Die Südkante (Außennormale 180°) trägt die Grenze der Fassade.
            AbbildBauteil fassade = Assert.Single(ab.Bauteile, b => b.Name == "Fassade Süd");
            foreach (string name in new[] { "Wohnen", "Küche", "Schlafen", "Bad" })
            {
                Raumumriss r = g.Raum(Raum(ab, name).Kennung);
                Umrisskante sued = Assert.Single(r.Polygone[0].Kanten, k => k.Grenzen.Count > 0);
                Assert.Equal(180.0, sued.AzimutGrad.Value, 1e-9);
                Grenzverweis v = Assert.Single(sued.Grenzen);
                Assert.Equal(fassade.Grenzen.Single(x => x.RaumKennung == r.RaumKennung).Kennung, v.Kennung);
                Assert.Equal(fassade.Kennung, v.BauteilKennung);
                Assert.Equal(Grenzstellung.Wand, v.Stellung);
                Assert.Equal(Randbedingung.Aussenluft, v.Lage);
                Assert.Equal(Bauteilart.Aussenwand, v.Bauteilart);
                // Die Wände ohne Ring stehen benannt an keiner Kante.
                Assert.NotEmpty(r.OhneKante);
                Assert.All(r.OhneKante, x => Assert.Equal(Grenzstellung.Wand, x.Stellung));
            }

            // Boden und Decke: die Geschossdecke unter Schlafen, über Wohnen; das Gegenstück der Datei bleibt.
            Raumumriss schlafen = g.Raum(Raum(ab, "Schlafen").Kennung);
            Assert.Contains(schlafen.Boden, v => v.BauteilKennung == decke.Kennung && v.Stellung == Grenzstellung.Boden);
            Raumumriss wohnen = g.Raum(Raum(ab, "Wohnen").Kennung);
            Assert.Contains(wohnen.Decke, v => v.BauteilKennung == decke.Kennung && v.Stellung == Grenzstellung.Decke);
            Assert.Contains(wohnen.Boden, v => v.Bauteilart == Bauteilart.Decke);   // die Kellerdecke, ohne Normale aus Sicht und Lage
            Raumumriss badU = g.Raum(Raum(ab, "Bad").Kennung);
            Raumumriss kueche = g.Raum(Raum(ab, "Küche").Kennung);
            Assert.Equal(kueche.Polygone[0].Quelle.Kennung, badU.Polygone[0].Quelle.GegenstueckKennung);

            // Das Lager ohne jede Grenzgeometrie: Rechteckersatz aus Fläche und Seitenverhältnis der Kellerwände
            // (A_NS 60 m², A_OW 48 m², h 2,6 m → 11,54 × 9,23 m = 106,5 m², über 10 % → flächentreu 10 × 8 m).
            Raumumriss lager = g.Raum(Raum(ab, "Lager").Kennung);
            Assert.Equal(Geometrieherkunft.Schematisch, lager.Herkunft);
            Assert.Equal(Umrissherleitung.Seitenverhaeltnis, lager.Herleitung);
            Assert.Equal(10.0, lager.LaengeM.Value, 1e-9);
            Assert.Equal(8.0, lager.BreiteM.Value, 1e-9);
            Assert.Equal(80.0, lager.PolygonflaecheM2, 1e-9);
            Assert.Null(lager.Polygone[0].EbeneM);
            Assert.Null(lager.Polygone[0].Quelle);
            string[] kg = { "KG Süd", "KG Ost", "KG Nord", "KG West" };
            int[] kante = { 0, 1, 2, 3 };
            for (int i = 0; i < 4; i++)
                Assert.Equal(ab.Bauteile.Single(b => b.Name == kg[i]).Kennung, Assert.Single(lager.Polygone[0].Kanten[kante[i]].Grenzen).BauteilKennung);
            Assert.Contains(lager.Boden, v => v.Bauteilart == Bauteilart.Bodenplatte);
            Assert.Contains(lager.Decke, v => v.Bauteilart == Bauteilart.Decke);

            PruefMeldung schematisch = Assert.Single(g.Meldungen, m => m.Schluessel == Zonengeometrie.SCHEMATISCH);
            Assert.Equal(new[] { "1", "Lager" }, schematisch.Werte);
            Assert.Contains(g.Meldungen, m => m.Schluessel == Zonengeometrie.SEITENVERHAELTNIS);
        }

        [Fact]
        public void Sechzig_Zellen_stehen_schematisch_als_Quadrate_je_Geschoss_in_Zeilen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(ZELLEN);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0, GebaeudeImportProfil.ZONENREGEL_X2);
            Zonengeometrie g = GebaeudeGrundriss.Bilden(a.Abbild, 0, z);

            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, g.Geschosse.Select(s => s.Name));
            Assert.Equal(2, g.Zonen.Count);
            Assert.Equal(60, g.Raeume.Count);
            Assert.All(g.Raeume, r =>
            {
                Assert.Equal(Geometrieherkunft.Schematisch, r.Herkunft);
                Assert.Equal(Umrissherleitung.Quadrat, r.Herleitung);
                Umrisspolygon p = Assert.Single(r.Polygone);
                Assert.Equal(10.0, p.FlaecheM2, 1e-9);
                Assert.Equal(r.FlaecheM2.Value, p.FlaecheM2, 1e-9);
                // Die eine Außenwand steht an der Kante ihres Sektors.
                Umrisskante k = Assert.Single(p.Kanten, x => x.Grenzen.Count > 0);
                AbbildBauteil wand = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == Assert.Single(k.Grenzen).BauteilKennung);
                Assert.Equal(wand.AzimutGrad.Value, k.AzimutGrad.Value, 1e-9);
            });
            Assert.Equal(new[] { 30, 30 }, g.Umrisse.Select(u => u.Raeume.Count));
            Assert.All(g.Umrisse, u => Assert.Equal(300.0, u.FlaecheM2, 1e-6));
            Assert.All(g.Zonen, x => Assert.Equal(Geometrieherkunft.Schematisch, x.Herkunft));

            // Je Geschoss in Zeilen gereiht (Block etwa 3 : 2), ohne Überdeckung: sechs je Zeile, fünf Zeilen.
            foreach (Geschossangabe s in g.Geschosse)
            {
                List<double[]> kasten = g.RaeumeIm(s.Stelle).Select(r => Kasten(r.Polygone[0])).ToList();
                Assert.Equal(30, kasten.Count);
                for (int i = 0; i < kasten.Count; i++)
                    for (int k = i + 1; k < kasten.Count; k++)
                        Assert.False(Ueberdeckt(kasten[i], kasten[k]), "Überdeckung " + i + "/" + k);
                Assert.Equal(5, kasten.Select(b => Math.Round(b[3], 6)).Distinct().Count());
                Assert.Equal(6, kasten.Count(b => Math.Abs(b[3] - kasten[0][3]) < 1e-9));
                Assert.True(s.MaxX - s.MinX > s.MaxY - s.MinY, "breiter als hoch");
            }
            Assert.Equal("60", Assert.Single(g.Meldungen, m => m.Schluessel == Zonengeometrie.SCHEMATISCH).Werte[0]);
            Assert.Equal("60", Assert.Single(g.Meldungen, m => m.Schluessel == Zonengeometrie.QUADRAT).Werte[0]);
        }

        [Fact]
        public void Der_Einzonenfall_bekommt_dieselbe_Geometrie_und_zeigt_die_Raeume_ohne_Zone()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(ZONENHAUS);
            GebaeudeZonierung z4 = GebaeudeZonierung.Bilden(a.Abbild, 0);
            GebaeudeZonierung z5 = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z5);
            Zonengeometrie g4 = GebaeudeGrundriss.Bilden(a.Abbild, 0, z4);
            Zonengeometrie g5 = GebaeudeGrundriss.Bilden(a.Abbild, 0, z5);

            Assert.True(z5.Einzonig);
            Assert.Single(g5.Zonen);
            // Dieselben Polygone — nur die Zuordnung der Räume zu den Zonen unterscheidet sich.
            Assert.Equal(Fingerabdruck(g4, mitZonen: false), Fingerabdruck(g5, mitZonen: false));
            // Das unbeheizte Lager liegt im Einzonenweg in keiner Zone.
            Raumumriss lager = g5.Raum(a.Abbild.Gebaeude[0].Raeume.Single(r => r.Name == "Lager").Kennung);
            Assert.Equal(-1, lager.Zone);
            Assert.False(lager.Beheizt);
            // Die Zone hat Umrisse in Erd- und Obergeschoss.
            Assert.Equal(new[] { 1, 2 }, g5.Zonen[0].Umrisse.Select(u => u.Geschoss));
            // Ohne Zonierung stehen alle Räume ohne Zone.
            Assert.All(GebaeudeGrundriss.Bilden(a.Abbild, 0).Raeume, r => Assert.Equal(-1, r.Zone));
        }

        // ==================================================================
        //  Rechteckersatz (Punkt 2) an einem Eingang ohne Datei
        // ==================================================================

        private static Umrissseite Wand(string kennung, int sektor, double flaeche)
            => new Umrissseite
            {
                Verweis = new Grenzverweis(kennung, kennung, Bauteilart.Aussenwand, Grenzstellung.Wand, Randbedingung.Aussenluft, null),
                FlaecheM2 = flaeche, Sektor = sektor,
            };

        private static Umrissraum Raum(string kennung, string geschoss, int zone, double? flaeche, double? volumen, double? hoehe,
                                       params Umrissseite[] seiten)
        {
            var r = new Umrissraum
            {
                Kennung = kennung, Name = kennung, GeschossKennung = geschoss, Zone = zone, Beheizt = true,
                FlaecheM2 = flaeche, VolumenM3 = volumen, HoeheM = hoehe,
            };
            r.Seiten.AddRange(seiten);
            return r;
        }

        private static Umrisseingang Rechteckeingang()
        {
            var e = new Umrisseingang();
            e.Geschosse.Add(new Umrissgeschoss("eg", "Erdgeschoss", 0.0));
            e.Geschosse.Add(new Umrissgeschoss("kg", "Keller", -3.0));
            e.Zonen.Add(new Umrisszone("z0", "Zone 0", true, 16.0, 40.0, 2.5, false));
            e.Zonen.Add(new Umrisszone("z1", "Zone 1", true, 40.0, 100.0, 2.5, false));
            // A: Wände passen zur Fläche — l = 25/(2·2,5) = 5, b = 20/(2·2,5) = 4, l·b = 20.
            e.Raeume.Add(Raum("A", "eg", 1, 20.0, 50.0, null, Wand("A-N", 0, 12.5), Wand("A-S", 2, 12.5), Wand("A-O", 1, 10.0), Wand("A-W", 3, 10.0)));
            // B: l = 20/5 = 4, b = 10/5 = 2, l·b = 8 ≠ 20 → flächentreu mit l : b = 2.
            e.Raeume.Add(Raum("B", "eg", 1, 20.0, 50.0, null, Wand("B-N", 0, 10.0), Wand("B-S", 2, 10.0), Wand("B-O", 1, 5.0), Wand("B-W", 3, 5.0)));
            // C: ohne Wände → Quadrat.
            e.Raeume.Add(Raum("C", "eg", 0, 16.0, 40.0, null));
            // D: ohne Fläche → kein Umriss.
            e.Raeume.Add(Raum("D", "eg", 0, null, null, null));
            // E: ohne Volumen, Höhe der Datei 2,5 m; ohne Zone.
            e.Raeume.Add(Raum("E", "eg", -1, 10.0, null, 2.5, Wand("E-N", 0, 5.0), Wand("E-S", 2, 5.0), Wand("E-O", 1, 4.0), Wand("E-W", 3, 4.0)));
            // F: im Keller; G: ohne Geschoss.
            e.Raeume.Add(Raum("F", "kg", 0, 9.0, 22.5, null));
            e.Raeume.Add(Raum("G", null, 0, 4.0, 10.0, null));
            return e;
        }

        [Fact]
        public void Der_Rechteckersatz_folgt_der_Formel_und_reiht_je_Geschoss()
        {
            Zonengeometrie g = Zonengeometrie.AusFlaechen(Rechteckeingang());

            // Geschosse nach Höhenlage, ohne Geschoss zuletzt.
            Assert.Equal(new[] { "kg", "eg", "" }, g.Geschosse.Select(s => s.Kennung));

            Raumumriss a = g.Raum("A"), b = g.Raum("B"), c = g.Raum("C"), d = g.Raum("D"), e = g.Raum("E");
            Assert.Equal(Umrissherleitung.Wandflaechen, a.Herleitung);
            Assert.Equal(5.0, a.LaengeM.Value, Genau);
            Assert.Equal(4.0, a.BreiteM.Value, Genau);
            Assert.Equal(Umrissherleitung.Seitenverhaeltnis, b.Herleitung);
            Assert.Equal(20.0, b.LaengeM.Value * b.BreiteM.Value, Genau);
            Assert.Equal(2.0, b.LaengeM.Value / b.BreiteM.Value, Genau);
            Assert.Equal(Umrissherleitung.Quadrat, c.Herleitung);
            Assert.Equal(4.0, c.LaengeM.Value, Genau);
            Assert.Equal(4.0, c.BreiteM.Value, Genau);
            Assert.Equal(Umrissherleitung.Keine, d.Herleitung);
            Assert.Empty(d.Polygone);
            // E: h aus der Datei, l = 10/5 = 2, b = 8/5 = 1,6 → l·b 3,2 ≠ 10 → flächentreu.
            Assert.Equal(Umrissherleitung.Seitenverhaeltnis, e.Herleitung);
            Assert.Equal(10.0, e.PolygonflaecheM2, Genau);
            Assert.Equal(1.25, e.LaengeM.Value / e.BreiteM.Value, Genau);
            Assert.All(g.Raeume.Where(r => r.Polygone.Count > 0), r =>
            {
                Assert.Equal(Geometrieherkunft.Schematisch, r.Herkunft);
                Assert.Equal(r.FlaecheM2.Value, r.PolygonflaecheM2, Genau);
                Assert.True(Zonengeometrie.DoppelteFlaeche(r.Polygone[0].Punkte) > 0.0);
            });

            // Kanten Süd, Ost, Nord, West tragen die Wände ihres Sektors.
            Assert.Equal(new[] { "A-S", "A-O", "A-N", "A-W" }, a.Polygone[0].Kanten.Select(k => Assert.Single(k.Grenzen).Kennung));
            Assert.Equal(new double?[] { 180.0, 90.0, 0.0, 270.0 }, a.Polygone[0].Kanten.Select(k => k.AzimutGrad));
            Assert.Equal(new[] { 5.0, 4.0, 5.0, 4.0 }, a.Polygone[0].Kanten.Select(k => Math.Round(k.LaengeM, 9)));

            // Reihung im Erdgeschoss: Zone 0 (C), dann Zone 1 (A, B), dann ohne Zone (E); zeilenweise von
            // links oben, oben bündig, 0,5 m Abstand; Zeilenbreite √(1,5 · Σ (l + 0,5)(b + 0,5)) ≈ 11,2 m.
            List<Raumumriss> eg = g.RaeumeIm(1).Where(r => r.Polygone.Count > 0).ToList();
            List<string> folge = eg.OrderBy(r => -Kasten(r.Polygone[0])[3]).ThenBy(r => Kasten(r.Polygone[0])[0]).Select(r => r.RaumKennung).ToList();
            Assert.Equal(new[] { "C", "A", "B", "E" }, folge);
            double[] kc = Kasten(c.Polygone[0]), ka = Kasten(a.Polygone[0]), kb = Kasten(b.Polygone[0]), ke = Kasten(e.Polygone[0]);
            Assert.Equal(0.0, kc[0], Genau);
            Assert.Equal(0.0, kc[3], Genau);
            Assert.Equal(kc[2] + Zonengeometrie.ABSTAND_M, ka[0], Genau);
            Assert.Equal(0.0, ka[3], Genau);
            // Zweite Zeile unter der höchsten der ersten.
            Assert.Equal(0.0, kb[0], Genau);
            Assert.Equal(-(4.0 + Zonengeometrie.ABSTAND_M), kb[3], Genau);
            Assert.Equal(kb[2] + Zonengeometrie.ABSTAND_M, ke[0], Genau);
            Assert.Equal(kb[3], ke[3], Genau);
            for (int i = 0; i < eg.Count; i++)
                for (int k = i + 1; k < eg.Count; k++)
                    Assert.False(Ueberdeckt(Kasten(eg[i].Polygone[0]), Kasten(eg[k].Polygone[0])));

            // Umrisse je Zone und Geschoss; ohne Zone keiner.
            Assert.Equal(new[] { (0, 0), (0, 1), (0, 2), (1, 1) }, g.Umrisse.Select(u => (u.Zone, u.Geschoss)));
            Assert.Equal(new[] { "A", "B" }, g.Umrisse.Single(u => u.Zone == 1).Raeume.Select(r => r.RaumKennung));
            Assert.Equal(2.5, g.Umrisse.Single(u => u.Zone == 1).HoeheM.Value, Genau);

            Assert.Equal(new[] { Zonengeometrie.SCHEMATISCH, Zonengeometrie.SEITENVERHAELTNIS, Zonengeometrie.QUADRAT, Zonengeometrie.OHNE_FLAECHE },
                         g.Meldungen.Select(m => m.Schluessel));
            Assert.Equal(PruefStufe.Warnung, g.Meldungen.Single(m => m.Schluessel == Zonengeometrie.OHNE_FLAECHE).Stufe);
            Assert.Equal(new[] { "1", "D" }, g.Meldungen.Single(m => m.Schluessel == Zonengeometrie.OHNE_FLAECHE).Werte);
        }

        [Fact]
        public void Rechtecke_stehen_rechts_neben_den_Umrissen_aus_Raumgrenzen_und_AusFlaechen_uebergeht_die_Ringe()
        {
            var e = new Umrisseingang();
            e.Geschosse.Add(new Umrissgeschoss("eg", "EG", 0.0));
            e.Zonen.Add(new Umrisszone("z", "Z", true, 22.0, 55.0, 2.5, false));
            Umrissraum echt = Raum("echt", "eg", 0, 12.0, 30.0, null);
            // Ein Bodenring im Uhrzeigersinn (von oben): 0…4 × 0…3 m auf z = 0,2.
            echt.Seiten.Add(new Umrissseite
            {
                Verweis = new Grenzverweis("boden", "platte", Bauteilart.Bodenplatte, Grenzstellung.Boden, Randbedingung.Erdreich, null),
                RandpunkteM = new[] { new[] { 0.0, 0.0, 0.2 }, new[] { 0.0, 3.0, 0.2 }, new[] { 4.0, 3.0, 0.2 }, new[] { 4.0, 0.0, 0.2 } },
                FlaecheM2 = 12.0,
            });
            // Eine Wand auf der Westkante x = 0 (Ring senkrecht, 2 cm daneben), eine ohne Ring.
            echt.Seiten.Add(new Umrissseite
            {
                Verweis = new Grenzverweis("west", "wand-w", Bauteilart.Aussenwand, Grenzstellung.Wand, Randbedingung.Aussenluft, null),
                RandpunkteM = new[] { new[] { 0.02, 3.0, 0.2 }, new[] { 0.02, 0.0, 0.2 }, new[] { 0.02, 0.0, 2.7 }, new[] { 0.02, 3.0, 2.7 } },
                FlaecheM2 = 7.5, Sektor = 3,
            });
            echt.Seiten.Add(Wand("ohne-ring", 0, 10.0));
            e.Raeume.Add(echt);
            e.Raeume.Add(Raum("ersatz", "eg", 0, 10.0, 25.0, null));

            Zonengeometrie g = Zonengeometrie.AusRaumgrenzen(e);
            Raumumriss r = g.Raum("echt");
            Assert.Equal(Geometrieherkunft.Raumgrenzen, r.Herkunft);
            Umrisspolygon p = Assert.Single(r.Polygone);
            // Gegen den Uhrzeigersinn gedreht, erster Punkt bleibt.
            Assert.Equal(new[] { "0;0", "4;0", "4;3", "0;3" }, p.Punkte.Select(q => q[0].ToString(CultureInfo.InvariantCulture) + ";" + q[1].ToString(CultureInfo.InvariantCulture)));
            Assert.Equal(12.0, p.FlaecheM2, Genau);
            Assert.Equal(0.2, p.EbeneM.Value, Genau);
            Umrisskante west = p.Kanten[3];
            Assert.Equal(270.0, west.AzimutGrad.Value, 1e-9);
            Assert.Equal("west", Assert.Single(west.Grenzen).Kennung);
            Assert.Equal("ohne-ring", Assert.Single(r.OhneKante).Kennung);
            Assert.Equal("boden", Assert.Single(r.Boden).Kennung);

            // Das Rechteck steht rechts daneben: x ab 4 + 2 m, oben bündig mit y = 3.
            double[] k = Kasten(g.Raum("ersatz").Polygone[0]);
            Assert.Equal(4.0 + Zonengeometrie.ABSTAND_BLOCK_M, k[0], Genau);
            Assert.Equal(3.0, k[3], Genau);
            Assert.Equal(Geometrieherkunft.Schematisch, g.Zonen[0].Herkunft);
            Assert.True(g.Geschosse[0].Schematisch);

            // AusFlaechen übergeht die Ringe: beide Räume schematisch.
            Zonengeometrie f = Zonengeometrie.AusFlaechen(e);
            Assert.All(f.Raeume, x => Assert.Equal(Geometrieherkunft.Schematisch, x.Herkunft));
        }

        // ==================================================================
        //  Determinismus (Punkt 4)
        // ==================================================================

        [Fact]
        public void Zweimal_gebildet_ist_die_Geometrie_tief_gleich()
        {
            foreach ((string probe, string regel) in new[] { (ZONENHAUS, (string)null), (ZELLEN, GebaeudeImportProfil.ZONENREGEL_X2), ("gbxml_haus_si.xml", null) })
            {
                string[] abdruck = new string[2];
                for (int lauf = 0; lauf < 2; lauf++)
                {
                    GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(probe);
                    GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0, regel);
                    abdruck[lauf] = Fingerabdruck(GebaeudeGrundriss.Bilden(a.Abbild, 0, z), mitZonen: true);
                }
                Assert.True(abdruck[0].Length > 1000, probe);
                Assert.Equal(abdruck[0], abdruck[1]);
            }
            Assert.Equal(Fingerabdruck(Zonengeometrie.AusFlaechen(Rechteckeingang()), true),
                         Fingerabdruck(Zonengeometrie.AusFlaechen(Rechteckeingang()), true));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>Der Kasten eines Polygons: minX, minY, maxX, maxY.</summary>
        internal static double[] Kasten(Umrisspolygon p)
            => new[] { p.Punkte.Min(q => q[0]), p.Punkte.Min(q => q[1]), p.Punkte.Max(q => q[0]), p.Punkte.Max(q => q[1]) };

        private static bool Ueberdeckt(double[] a, double[] b)
            => Math.Min(a[2], b[2]) - Math.Max(a[0], b[0]) > 1e-9 && Math.Min(a[3], b[3]) - Math.Max(a[1], b[1]) > 1e-9;

        /// <summary>
        /// Der Fingerabdruck einer Zonengeometrie — jede Zahl mit „R" invariant, jede Liste in ihrer Reihenfolge:
        /// Geschosse, Zonen, Räume mit Polygonen, Kanten und Verweisen, Umrisse, Meldungen.
        /// </summary>
        internal static string Fingerabdruck(Zonengeometrie g, bool mitZonen)
        {
            var s = new StringBuilder();
            string Z(double? w) => w.HasValue ? w.Value.ToString("R", CultureInfo.InvariantCulture) : "-";
            string V(Grenzverweis v) => v == null ? "-" : v.Kennung + "|" + v.BauteilKennung + "|" + v.Bauteilart + "|" + v.Stellung + "|" + v.Lage + "|" + v.GegenstueckKennung;
            s.Append("N").Append(Z(g.NordwinkelGrad)).Append(g.NordwinkelAngewandt).AppendLine();
            foreach (Geschossangabe x in g.Geschosse)
                s.Append("G").Append(x.Stelle).Append(x.Kennung).Append(x.Name).Append(Z(x.LageM)).Append(x.Schematisch)
                 .Append(Z(x.MinX)).Append(Z(x.MinY)).Append(Z(x.MaxX)).Append(Z(x.MaxY)).AppendLine();
            if (mitZonen)
                foreach (Zonenangabe x in g.Zonen)
                    s.Append("Z").Append(x.Stelle).Append(x.Schluessel).Append(x.Name).Append(x.IstBeheizt).Append(x.Handgeaendert).Append(x.Herkunft)
                     .Append(Z(x.FlaecheM2)).Append(Z(x.VolumenM3)).Append(Z(x.HoeheM)).AppendLine();
            foreach (Raumumriss r in g.Raeume)
            {
                s.Append("R").Append(r.RaumKennung).Append(r.Name).Append(r.GeschossKennung).Append(r.Geschoss).Append(mitZonen ? r.Zone.ToString(CultureInfo.InvariantCulture) : "")
                 .Append(r.Herkunft).Append(r.Herleitung).Append(Z(r.FlaecheM2)).Append(Z(r.PolygonflaecheM2)).Append(Z(r.HoeheM))
                 .Append(Z(r.LaengeM)).Append(Z(r.BreiteM)).AppendLine();
                foreach (Umrisspolygon p in r.Polygone)
                {
                    s.Append(" P").Append(Z(p.FlaecheM2)).Append(Z(p.EbeneM)).Append(V(p.Quelle));
                    foreach (double[] q in p.Punkte) s.Append(" ").Append(Z(q[0])).Append(";").Append(Z(q[1]));
                    s.AppendLine();
                    foreach (Umrisskante k in p.Kanten)
                        s.Append("  K").Append(k.Index).Append(Z(k.LaengeM)).Append(Z(k.AzimutGrad)).Append(string.Join(",", k.Grenzen.Select(V))).AppendLine();
                }
                s.Append(" B").Append(string.Join(",", r.Boden.Select(V))).Append(" D").Append(string.Join(",", r.Decke.Select(V)))
                 .Append(" O").Append(string.Join(",", r.OhneKante.Select(V))).AppendLine();
            }
            if (mitZonen)
                foreach (Zonenumriss u in g.Umrisse)
                    s.Append("U").Append(u.Zone).Append(u.Geschoss).Append(u.Herkunft).Append(Z(u.FlaecheM2)).Append(Z(u.HoeheM))
                     .Append(string.Join(",", u.Raeume.Select(r => r.RaumKennung))).AppendLine();
            foreach (PruefMeldung m in g.Meldungen) s.Append("M").Append(m).AppendLine();
            return s.ToString();
        }
    }
}
