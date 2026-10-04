using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using static EPOS.Kern.Tests.GbxmlExportProbe;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7b — gbXML Stufe 2</b> (Datenaustauschkonzept 5.5, 5.6, 8.4, 14.3, 14.5): <c>PlanarGeometry/PolyLoop</c>
    /// je Fläche und Öffnung, <c>ShellGeometry/ClosedShell</c> je Raum, <c>Results</c> je Zone, die Kennzeichnung
    /// „schematisch“ an drei Stellen, die benannte Ablehnung bei widersprüchlicher Anordnung, Probe 25 (byteweise
    /// gleiche Dateien), Probe 27 (jede <c>PolyLoop</c> gegen das Zonengeometrie-Modell), die Schemaprüfung der
    /// Stufe-2-Datei und der Rundlauf. Ohne Datenbank.
    /// </summary>
    public sealed class GbxmlStufe2Tests : IDisposable
    {
        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public GbxmlStufe2Tests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose() => _kultur.Dispose();

        private const int ZA = 21, ZB = 22;

        /// <summary>
        /// Zwei beheizte Zonen: A 10 × 5 m (50 m², 125 m³) mit Fenster in der Südwand, B 6 × 5 m (30 m², 75 m³), dazwischen die
        /// Trennwand 120 (Azimut 90° aus Sicht von A); je Zone Bodenplatte und Dach. Optional mit Jahresergebnissen,
        /// mit drittem Raum im Widerspruch oder ohne Flächen (kein Körper).
        /// </summary>
        internal static GebaeudeAbbild Zweizonen(bool ergebnisse = false, bool widerspruch = false, bool ohneFlaeche = false)
        {
            var a = new GbxmlAbbild { CampusKennung = GebaeudeExportKennung.Campus(GEB), Plz = "01067", NordwinkelGrad = 0.0 };
            var g = new AbbildGebaeude { Kennung = GebaeudeExportKennung.Gebaeude(GEB), Name = "Zweizonenhaus", Art = "Office", Beschreibung = "Kopfzeile" };
            a.Gebaeude.Add(g);
            string ra = GebaeudeExportKennung.Raum(ZA), rb = GebaeudeExportKennung.Raum(ZB), rc = GebaeudeExportKennung.Raum(23);
            AbbildRaum Raum(string k, int zone, string name, double f, double v) => new AbbildRaum
            {
                Kennung = k, Name = name, FlaecheM2 = ohneFlaeche ? (double?)null : f, VolumenM3 = ohneFlaeche ? (double?)null : v, Beheizt = true,
                Zustandsangabe = GbxmlVokabular.Heated, SollHeizenC = 20.0, ZonenKennung = GebaeudeExportKennung.Zone(zone),
                Ergebnis = ergebnisse ? new AbbildErgebnis { EnergieKWh = 1234.5, HeizlastW = 2500.0, MitteltemperaturC = 20.4, Beginn = new DateTime(2026, 1, 1) } : null,
            };
            g.Raeume.Add(Raum(ra, ZA, "Büro", 50.0, 125.0));
            g.Raeume.Add(Raum(rb, ZB, "Lager", 30.0, 75.0));

            AbbildAufbau aw = Aussenwand(Waermestromrichtung.Horizontal, Bauteilrand.Aussenluft, 0.3);
            AbbildBauteil sued = Flaeche(201, GbxmlVokabular.ExteriorWall, aw, 10.0, 2.5, 180.0, 90.0, N(ra));
            sued.Oeffnungen.Add(new AbbildBauteil
            {
                Kennung = GebaeudeExportKennung.Oeffnung(211), Quelltyp = "Opening", Name = "Fenster Süd", Quellart = GbxmlVokabular.FixedWindow,
                Art = Bauteilart.Fenster, FenstertypKennung = GebaeudeExportKennung.Fenstertyp(211), UWertWm2K = 1.1, GWert = 0.6,
                BreiteM = 2.0, HoeheM = 1.5, BruttoflaecheM2 = 3.0, AzimutGrad = 180.0, NeigungGrad = 90.0,
            });
            g.Bauteile.Add(sued);
            g.Bauteile.Add(Flaeche(202, GbxmlVokabular.ExteriorWall, aw, 10.0, 2.5, 0.0, 90.0, N(ra)));
            g.Bauteile.Add(Flaeche(203, GbxmlVokabular.ExteriorWall, aw, 5.0, 2.5, 270.0, 90.0, N(ra)));
            g.Bauteile.Add(Flaeche(204, GbxmlVokabular.SlabOnGrade, Bodenplatte(), 10.0, 5.0, null, 180.0, N(ra, GbxmlVokabular.SlabOnGrade)));
            g.Bauteile.Add(Flaeche(205, GbxmlVokabular.Roof, Dach(), 10.0, 5.0, null, 0.0, N(ra, GbxmlVokabular.Roof)));
            g.Bauteile.Add(Flaeche(120, GbxmlVokabular.InteriorWall, Innenwand(), 5.0, 2.5, 90.0, 90.0, N(ra), N(rb)));
            g.Bauteile.Add(Flaeche(301, GbxmlVokabular.ExteriorWall, aw, 6.0, 2.5, 180.0, 90.0, N(rb)));
            g.Bauteile.Add(Flaeche(302, GbxmlVokabular.ExteriorWall, aw, 6.0, 2.5, 0.0, 90.0, N(rb)));
            g.Bauteile.Add(Flaeche(303, GbxmlVokabular.ExteriorWall, aw, 5.0, 2.5, 90.0, 90.0, N(rb)));
            g.Bauteile.Add(Flaeche(304, GbxmlVokabular.SlabOnGrade, Bodenplatte(), 6.0, 5.0, null, 180.0, N(rb, GbxmlVokabular.SlabOnGrade)));
            g.Bauteile.Add(Flaeche(305, GbxmlVokabular.Roof, Dach(), 6.0, 5.0, null, 0.0, N(rb, GbxmlVokabular.Roof)));
            if (widerspruch)
            {
                // Ein dritter Raum C über den Nordwänden von A (Trennwand 131) und B (Trennwand 122) — auf der Südkante von C steht 122 vor 131, C kann nicht an beiden liegen.
                g.Raeume.Add(Raum(rc, 23, "Flur", 40.0, 100.0));
                g.Bauteile.RemoveAll(b => b.Kennung == GebaeudeExportKennung.Bauteil(202) || b.Kennung == GebaeudeExportKennung.Bauteil(302));
                g.Bauteile.Add(Flaeche(131, GbxmlVokabular.InteriorWall, Innenwand(), 10.0, 2.5, 0.0, 90.0, N(ra), N(rc)));
                g.Bauteile.Add(Flaeche(122, GbxmlVokabular.InteriorWall, Innenwand(), 6.0, 2.5, 0.0, 90.0, N(rb), N(rc)));
                g.Bauteile.Add(Flaeche(401, GbxmlVokabular.ExteriorWall, aw, 8.0, 2.5, 0.0, 90.0, N(rc)));
            }
            return a;
        }

        private static XDocument Laden(byte[] datei) => XDocument.Load(new MemoryStream(datei));

        private static double[] Punkt(XElement cp)
            => cp.Elements(NS + "Coordinate").Select(c => double.Parse(c.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();

        private static List<double[]> Ring(XElement planar) => planar.Element(NS + "PolyLoop").Elements(NS + "CartesianPoint").Select(Punkt).ToList();

        private static void GleicherRing(IReadOnlyList<double[]> modell, IReadOnlyList<double[]> datei)
        {
            Assert.Equal(modell.Count, datei.Count);
            for (int i = 0; i < modell.Count; i++)
                for (int d = 0; d < 3; d++) Assert.Equal(modell[i][d], datei[i][d]);
        }

        private string Text(string schluessel) => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, CultureInfo.GetCultureInfo("de-DE"));

        [Fact]
        public void Stufe2_schreibt_PolyLoop_ShellGeometry_Results_und_die_Kennzeichnung()
        {
            byte[] datei = Schreiben(Zweizonen(ergebnisse: true), null, out GebaeudeExportBilanz bilanz);
            XDocument d = Laden(datei);
            List<XElement> flaechen = d.Descendants(NS + "Surface").ToList();
            Assert.Equal(11, flaechen.Count);
            Assert.All(flaechen, f => Assert.NotNull(f.Element(NS + "PlanarGeometry")));
            Assert.All(flaechen, f => Assert.NotNull(f.Element(NS + "RectangularGeometry")));
            Assert.All(d.Descendants(NS + "Space"), s => Assert.Equal(6, s.Element(NS + "ShellGeometry").Element(NS + "ClosedShell").Elements(NS + "PolyLoop").Count()));
            Assert.NotNull(d.Descendants(NS + "Opening").Single().Element(NS + "PlanarGeometry"));

            // Kennzeichnung: Campus, Building, Plan/Bilanz — nur weil das Modell schematisch ist.
            string vermerk = Text("GEXP_DATEI_SCHEMATISCH");
            Assert.EndsWith(vermerk, d.Root.Element(NS + "Campus").Element(NS + "Description").Value);
            Assert.StartsWith("Kopfzeile", d.Root.Element(NS + "Campus").Element(NS + "Description").Value);
            Assert.Equal(vermerk, d.Descendants(NS + "Building").Single().Element(NS + "Description").Value);
            Assert.Contains(GbxmlSchreiber.Geometriemeldungen(Zweizonen()), m => m.Schluessel == GbxmlSchreiber.GEOMETRIE_SCHEMATISCH && m.Werte[0] == "2");
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Schluessel == GbxmlSchreiber.FLAECHE_OHNE_POLYGON);

            // Results: je Zone drei, in den zulässigen Einheiten.
            List<XElement> ergebnisse = d.Root.Elements(NS + "Results").ToList();
            Assert.Equal(6, ergebnisse.Count);
            Assert.Equal(new[] { "Energy|KilowattHours", "HeatLoad|Watt", "DryBulbTemperature|C" },
                         ergebnisse.Take(3).Select(e => (string)e.Attribute("resultsType") + "|" + (string)e.Attribute("unit")));
            Assert.Equal(GebaeudeExportKennung.Zone(ZA), ergebnisse[0].Element(NS + "ObjectId").Value);
            Assert.Equal("1234.5", ergebnisse[0].Element(NS + "Value").Value);
            Assert.Equal("2026-01-01T00:00:00", (string)ergebnisse[0].Attribute("startTime"));
        }

        [Fact]
        public void Ohne_Koerper_steht_kein_Vermerk_und_keine_Geometrie()
        {
            XDocument d = Laden(Schreiben(Zweizonen(ohneFlaeche: true)));
            Assert.Empty(d.Descendants(NS + "PlanarGeometry"));
            Assert.Empty(d.Descendants(NS + "ShellGeometry"));
            Assert.Empty(d.Root.Elements(NS + "Results"));
            Assert.Null(d.Descendants(NS + "Building").Single().Element(NS + "Description"));
            Assert.Equal("Kopfzeile\n" + Text("GEXP_DATEI_OHNE_GEOMETRIE"), d.Root.Element(NS + "Campus").Element(NS + "Description").Value);
            Assert.DoesNotContain(Text("GEXP_DATEI_SCHEMATISCH"), d.ToString());
            Assert.Empty(GbxmlSchreiber.Geometriemeldungen(Zweizonen(ohneFlaeche: true)));
        }

        [Fact]
        public void Widerspruechliche_Anordnung_lehnt_Stufe2_benannt_ab()
        {
            GebaeudeAbbild abbild = Zweizonen(widerspruch: true);
            (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(abbild);
            Assert.True(z.AnordnungAbgelehnt);
            Assert.Null(k);
            XDocument d = Laden(Schreiben(abbild));
            Assert.Empty(d.Descendants(NS + "PlanarGeometry"));
            Assert.Empty(d.Descendants(NS + "ShellGeometry"));
            string beschreibung = d.Root.Element(NS + "Campus").Element(NS + "Description").Value;
            Assert.EndsWith(string.Format(CultureInfo.GetCultureInfo("de-DE"), Text("GEXP_DATEI_GEOMETRIE_ABGELEHNT"), GbxmlSchreiber.Widersprueche(z)), beschreibung);
            Assert.Contains("–", GbxmlSchreiber.Widersprueche(z));
            PruefMeldung m = Assert.Single(GbxmlSchreiber.Geometriemeldungen(abbild));
            Assert.Equal((PruefStufe.Warnung, GbxmlSchreiber.GEOMETRIE_ABGELEHNT), (m.Stufe, m.Schluessel));
        }

        /// <summary><b>Probe 25:</b> zwei Läufe derselben Eingabe — byteweise gleiche Dateien, auch für das Probenabbild.</summary>
        [Fact]
        public void Probe25_zwei_Laeufe_schreiben_byteweise_gleiche_Dateien()
        {
            Assert.Equal(Schreiben(Zweizonen(ergebnisse: true)), Schreiben(Zweizonen(ergebnisse: true)));
            Assert.Equal(Schreiben(Abbild()), Schreiben(Abbild()));
            // Eine andere Reihenfolge der Bauteile ändert die Folge der Flächen, nicht ihre Koordinaten.
            GebaeudeAbbild umgekehrt = Zweizonen();
            umgekehrt.Gebaeude[0].Bauteile.Reverse();
            XDocument x = Laden(Schreiben(Zweizonen())), y = Laden(Schreiben(umgekehrt));
            foreach (XElement f in x.Descendants(NS + "Surface"))
            {
                XElement g = y.Descendants(NS + "Surface").Single(s => (string)s.Attribute("id") == (string)f.Attribute("id"));
                Assert.Equal(f.Element(NS + "PlanarGeometry").ToString(), g.Element(NS + "PlanarGeometry").ToString());
            }
        }

        /// <summary>
        /// <b>Probe 27:</b> jede <c>PolyLoop</c> der Datei wird gegen das Zonengeometrie-Modell gehalten — die Fläche
        /// gegen die Körperfläche ihres ersten Nachbarraums, die Hülle gegen das Prisma, die Öffnung gegen ihre Wand —
        /// und jeder Punkt liegt im Rechteck und in der Höhe seines Raums.
        /// </summary>
        [Fact]
        public void Probe27_jede_PolyLoop_entspricht_dem_Zonengeometrie_Modell()
        {
            foreach (GebaeudeAbbild abbild in new[] { Zweizonen(), Abbild() })
            {
                (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(abbild);
                Assert.NotNull(k);
                XDocument d = Laden(Schreiben(abbild));
                int geprueft = 0;
                foreach (XElement s in d.Descendants(NS + "Surface"))
                {
                    XElement planar = s.Element(NS + "PlanarGeometry");
                    string id = (string)s.Attribute("id");
                    List<string> nachbarn = s.Elements(NS + "AdjacentSpaceId").Select(n => (string)n.Attribute("spaceIdRef")).ToList();
                    if (planar == null)
                    {
                        // Ohne Polygon steht nur innere Masse einer Zone.
                        Assert.Equal(2, nachbarn.Count);
                        Assert.Equal(nachbarn[0], nachbarn[1]);
                        continue;
                    }
                    Koerperflaeche f = k.Flaeche(nachbarn[0], id);
                    Assert.NotNull(f);
                    GleicherRing(f.PunkteM, Ring(planar));
                    Raumumriss r = z.Raum(nachbarn[0]);
                    Raumkoerper rk = k.Raum(nachbarn[0]);
                    double[] kasten = ZonengeometrieTests.Kasten(r.Polygone[0]);
                    foreach (double[] p in Ring(planar))
                    {
                        Assert.InRange(p[0], kasten[0] - 1e-6, kasten[2] + 1e-6);
                        Assert.InRange(p[1], kasten[1] - 1e-6, kasten[3] + 1e-6);
                        Assert.InRange(p[2], rk.BodenM, rk.BodenM + rk.HoeheM);
                    }
                    // Die Öffnungen: in der Ebene ihrer Wand, mit Randabstand.
                    List<XElement> oeffnungen = s.Elements(NS + "Opening").ToList();
                    for (int i = 0; i < oeffnungen.Count; i++)
                    {
                        IReadOnlyList<double[]> soll = Zonenkoerper.Oeffnung(f, i, oeffnungen.Count,
                            double.Parse(oeffnungen[i].Element(NS + "RectangularGeometry").Element(NS + "Width").Value, CultureInfo.InvariantCulture)
                            * double.Parse(oeffnungen[i].Element(NS + "RectangularGeometry").Element(NS + "Height").Value, CultureInfo.InvariantCulture), out _);
                        List<double[]> ist = Ring(oeffnungen[i].Element(NS + "PlanarGeometry"));
                        GleicherRing(soll, ist);
                        double[] nw = ZonengeometrieAnlegenTests.Newell(f.EckenM), no = ZonengeometrieAnlegenTests.Newell(ist);
                        Assert.Equal(1.0, (nw[0] * no[0] + nw[1] * no[1] + nw[2] * no[2]) / ZonengeometrieAnlegenTests.Betrag(nw) / ZonengeometrieAnlegenTests.Betrag(no), 9);
                        foreach (double[] p in ist) Assert.InRange(p[2], f.EckenM[0][2] + 0.1 - 1e-6, f.EckenM[3][2] - 0.1 + 1e-6);
                    }
                    geprueft++;
                }
                Assert.True(geprueft >= 7);
                foreach (XElement space in d.Descendants(NS + "Space").Where(x => x.Element(NS + "ShellGeometry") != null))
                {
                    Raumkoerper rk = k.Raum((string)space.Attribute("id"));
                    List<XElement> ringe = space.Element(NS + "ShellGeometry").Element(NS + "ClosedShell").Elements(NS + "PolyLoop").ToList();
                    Assert.Equal(rk.Schale.Count, ringe.Count);
                    for (int i = 0; i < ringe.Count; i++)
                        GleicherRing(rk.Schale[i], ringe[i].Elements(NS + "CartesianPoint").Select(Punkt).ToList());
                }
            }
        }

        /// <summary>
        /// Bei jedem angelegten Paar passt der Azimut der <c>RectangularGeometry</c> jeder Wand zur Richtung ihrer
        /// <c>PolyLoop</c>: die Normale des Rings zeigt in den Azimut (Toleranz 1°) — die Anordnung dreht nie.
        /// </summary>
        [Fact]
        public void Azimut_der_RectangularGeometry_und_Richtung_der_PolyLoop_passen_zusammen()
        {
            int paarwaendeAlle = 0;
            foreach (GebaeudeAbbild abbild in new[] { Zweizonen(), Abbild() })
            {
                (Zonengeometrie z, _) = GbxmlSchreiber.Raumgeometrie(abbild);
                var trennwaende = new HashSet<string>(z.Nachbarpaare.Where(p => p.Angelegt).Select(p => p.BauteilKennung), StringComparer.Ordinal);
                int waende = 0, paarwaende = 0;
                foreach (XElement s in Laden(Schreiben(abbild)).Descendants(NS + "Surface"))
                {
                    XElement planar = s.Element(NS + "PlanarGeometry"), rechteck = s.Element(NS + "RectangularGeometry");
                    if (planar == null || rechteck?.Element(NS + "Azimuth") == null) continue;
                    if (Math.Abs(double.Parse(rechteck.Element(NS + "Tilt").Value, CultureInfo.InvariantCulture) - 90.0) > 1e-9) continue;
                    double azimut = double.Parse(rechteck.Element(NS + "Azimuth").Value, CultureInfo.InvariantCulture);
                    Assert.Equal(0.0, ZonengeometrieAnlegenTests.Winkelabstand(azimut, ZonengeometrieAnlegenTests.AzimutDerNormalen(Ring(planar))), 1.0);
                    waende++;
                    if (trennwaende.Contains((string)s.Attribute("id"))) paarwaende++;
                }
                Assert.True(waende >= 4);
                Assert.Equal(trennwaende.Count, paarwaende);
                paarwaendeAlle += paarwaende;
            }
            Assert.True(paarwaendeAlle >= 1);
        }

        [Fact]
        public void Die_Trennflaeche_steht_einmal_mit_beiden_Raeumen_und_liegt_auf_beiden_Koerpern()
        {
            GebaeudeAbbild abbild = Zweizonen();
            (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(abbild);
            string ra = GebaeudeExportKennung.Raum(ZA), rb = GebaeudeExportKennung.Raum(ZB), t = GebaeudeExportKennung.Bauteil(120);
            Nachbarpaar paar = Assert.Single(z.Nachbarpaare);
            Assert.True(paar.Angelegt);
            Assert.Equal(paar.VerweisB.Kennung, paar.VerweisA.GegenstueckKennung);
            Assert.Equal(paar.VerweisA.Kennung, paar.VerweisB.GegenstueckKennung);
            XElement s = Laden(Schreiben(abbild)).Descendants(NS + "Surface").Single(x => (string)x.Attribute("id") == t);
            Assert.Equal(new[] { ra, rb }, s.Elements(NS + "AdjacentSpaceId").Select(n => (string)n.Attribute("spaceIdRef")));
            List<double[]> ring = Ring(s.Element(NS + "PlanarGeometry"));
            Koerperflaeche gegen = k.Flaeche(rb, t);
            // Dieselben Ecken von der Gegenseite, gegenläufig — auf 1 mm (hier exakt).
            Assert.Equal(ZonengeometrieAnlegenTests.Text(gegen.EckenM[1]), ZonengeometrieAnlegenTests.Text(ring[0]));
            Assert.Equal(ZonengeometrieAnlegenTests.Text(gegen.EckenM[0]), ZonengeometrieAnlegenTests.Text(ring[1]));
        }

        [Fact]
        public void Schemapruefung_der_Stufe2_Datei_ohne_Befund()
        {
            string schema = Schemakopie();
            if (schema == null)
            {
                _ausgabe.WriteLine("Schemaprüfung übersprungen: Referenzlaeufe/Schemakopien/" + SCHEMAKOPIE + " liegt nicht bei (D17).");
                return;
            }
            foreach ((string name, GebaeudeAbbild abbild) in new[] { ("Zweizonen mit Results", Zweizonen(ergebnisse: true)), ("Probenabbild", Abbild()),
                                                                      ("Widerspruch", Zweizonen(widerspruch: true)) })
            {
                byte[] datei = Schreiben(abbild);
                string[] befunde = Schemapruefung(schema, datei);
                Assert.True(befunde.Length == 0, name + ":\n" + string.Join("\n", befunde));
                _ausgabe.WriteLine(name + ": " + datei.Length.ToString(CultureInfo.InvariantCulture) + " Byte, gültig");
            }
        }

        [Fact]
        public void Rundlauf_die_Stufe2_Datei_liest_sich_mit_gleichen_Flaechen_und_Ringen_zurueck()
        {
            GebaeudeAbbild quelle = Zweizonen(ergebnisse: true);
            byte[] datei = Schreiben(quelle);
            GbxmlAbbild zurueck = Lesen(datei);
            AbbildGebaeude g0 = quelle.Gebaeude[0], g1 = Assert.Single(zurueck.Gebaeude);
            Assert.Equal(g0.Bauteile.Count, g1.Bauteile.Count);
            (Zonengeometrie _, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(quelle);
            foreach (AbbildBauteil b0 in g0.Bauteile)
            {
                AbbildBauteil b1 = Assert.Single(g1.Bauteile, b => b.Kennung == b0.Kennung);
                Assert.Equal(b0.BruttoflaecheM2.Value, b1.BruttoflaecheM2.Value, 9);
                Assert.Equal(b0.NeigungGrad.Value, b1.NeigungGrad.Value, 9);
                Assert.Equal(b0.Nachbarn.Select(n => n.Kennung), b1.Nachbarn.Select(n => n.Kennung));
                Assert.NotNull(b1.RandpunkteM);
                Koerperflaeche f = k.Flaeche(b0.Nachbarn[0].Kennung, b0.Kennung);
                // Der Leser lässt nur den Schlusspunkt und doppelte Folgepunkte weg — der Ring kommt gleich zurück.
                GleicherRing(f.PunkteM, b1.RandpunkteM);
            }
            // Wieder eingelesen trägt jeder Raum seinen Umriss aus den Raumgrenzen (Bodenring), flächengleich.
            Zonengeometrie wieder = GebaeudeGrundriss.Bilden(zurueck, 0);
            foreach (Raumumriss r in wieder.Raeume)
            {
                Assert.Equal(Geometrieherkunft.Raumgrenzen, r.Herkunft);
                Assert.Equal(r.FlaecheM2.Value, r.PolygonflaecheM2, 6);
            }
        }
    }
}
