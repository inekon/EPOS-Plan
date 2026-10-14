using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe K3 — Körper aus gbXML</b> (Datenaustauschkonzept 17.2 bis 17.4, Probe 41, Rechenzeit Probe 42): Raumkörper
    /// aus <c>ClosedShell</c> und aus den Flächen je Raum, Volumen gegen die Handrechnung, Quellfläche je Dreieck,
    /// Bauteilkörper mit Dicke aus den Schichten (mm und m), Vorgabedicke, Richtung nach der Bezugsebene, Fenster
    /// ausgespart, offene Hülle als benannter Rückfall, Determinismus. Ohne Datenbank.
    /// </summary>
    public sealed class GbxmlKoerperTests : IDisposable
    {
        private const string SCHALE = "gbxml_g5_closedshell.xml";
        private const string FLAECHEN = "gbxml_g5_flaechen.xml";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly ITestOutputHelper _ausgabe;

        public GbxmlKoerperTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose() => _kultur.Dispose();

        // ------------------------------------------------------------------
        //  Raumkörper
        // ------------------------------------------------------------------

        [Fact]
        public void Raumkoerper_aus_der_Schale_mit_Volumen_der_Handrechnung()
        {
            AbbildGebaeude g = Gebaeude(LesenDatei(SCHALE));
            Dateikoerper a = Raum(g, "raum-a").Koerper, b = Raum(g, "raum-b").Koerper;
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.Equal(GbxmlKoerper.ART_CLOSEDSHELL, a.Art);
            Assert.Equal(Koerperquelle.AusFlaechen, b.Quelle);
            // Innenmaß: (5 − 0,30 − 0,06) × (4 − 2 · 0,30) × 3 bzw. dasselbe Feld unter dem Pultdach z = 3 + y/4.
            Assert.Equal(4.64 * 3.4 * 3.0, Volumen(a), 6);
            Assert.Equal(4.64 * (3.0 * 3.4 + (3.7 * 3.7 - 0.3 * 0.3) / 8.0), Volumen(b), 6);
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel.Contains("KOERPER_"));
        }

        [Fact]
        public void Raumkoerper_aus_den_Flaechen_je_Raum_ohne_Schale()
        {
            AbbildGebaeude g = Gebaeude(LesenDatei(FLAECHEN));
            Dateikoerper a = Raum(g, "raum-a").Koerper, b = Raum(g, "raum-b").Koerper;
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.Equal(GbxmlKoerper.ART_RAUMFLAECHEN, a.Art);
            Assert.Equal(5.0 * 4.0 * 3.0, Volumen(a), 6);
            Assert.Equal(5.0 * 4.0 * 3.5, Volumen(b), 6);
            // Die Innenwand dient beiden Räumen — je Raum gerichtet, die Normale zeigt aus dem Raum.
            Assert.Contains("iw-ab", a.Quellflaechen);
            Assert.Contains("iw-ab", b.Quellflaechen);
            Assert.True(Normale(a, "iw-ab")[0] > 0.99);
            Assert.True(Normale(b, "iw-ab")[0] < -0.99);
        }

        [Fact]
        public void Jedes_Dreieck_traegt_seine_Quellflaeche()
        {
            foreach (string datei in new[] { SCHALE, FLAECHEN })
            {
                AbbildGebaeude g = Gebaeude(LesenDatei(datei));
                var kennungen = new HashSet<string>(g.Bauteile.Select(x => x.Kennung));
                foreach (AbbildRaum r in g.Raeume)
                {
                    Assert.Equal(r.Koerper.DreieckZahl, r.Koerper.Quellflaechen.Count);
                    Assert.All(r.Koerper.Quellflaechen, q => Assert.Contains(q, kennungen));
                }
            }
            // Die Schale von Raum B: Westseite an der Innenwand, Decke am Pultdach — nicht am Giebel.
            Dateikoerper b = Raum(Gebaeude(LesenDatei(SCHALE)), "raum-b").Koerper;
            Assert.Equal(new[] { "aw-b-nord", "aw-b-ost", "aw-b-sued", "boden-b", "dach-b", "iw-ab" },
                         b.Quellflaechen.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }

        [Fact]
        public void Schalenflaeche_ohne_passende_Flaeche_bekommt_eine_Ersatzkennung()
        {
            XDocument d = XDocument.Load(GbxmlImportTests.Probe(SCHALE));
            Element(d, "Surface", "dach-a").Remove();
            AbbildRaum a = Raum(Gebaeude(Lesen(d)), "raum-a");
            Assert.NotNull(a.Koerper);
            Assert.Contains("raum-a:Schale1", a.Koerper.Quellflaechen);
        }

        [Fact]
        public void Offene_Huelle_faellt_benannt_auf_das_Umrissprisma_zurueck()
        {
            XDocument d = XDocument.Load(GbxmlImportTests.Probe(FLAECHEN));
            Element(d, "Surface", "dach-a").Remove();
            AbbildGebaeude g = Gebaeude(Lesen(d));
            Assert.Null(Raum(g, "raum-a").Koerper);
            Assert.NotNull(Raum(g, "raum-b").Koerper);
            PruefMeldung m = Assert.Single(g.Meldungen, x => x.Schluessel == GbxmlImportProfil.MELDUNGSPRAEFIX + "KOERPER_NICHT_GESCHLOSSEN");
            Assert.Equal("raum-a", m.Werte[0]);
            Assert.Equal(GbxmlKoerper.ART_RAUMFLAECHEN, m.Werte[1]);
            Assert.Equal("4", m.Werte[2]);
            Assert.Equal(Koerperbildner.RUECKFALL_UMRISSPRISMA, Koerperbildner.Huelle(Array.Empty<Quellflaeche>()).Rueckfall);
            Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(m.Schluessel, CultureInfo.CurrentUICulture)));
        }

        [Fact]
        public void Ohne_Schale_und_ohne_Polygone_bleibt_der_Raum_ohne_Koerper()
        {
            AbbildGebaeude g = Gebaeude(LesenDatei("gbxml_innenflaechen_teilweise.xml"));
            Assert.All(g.Raeume, r => Assert.Null(r.Koerper));
            Assert.All(g.Bauteile, b => Assert.Null(b.Koerper));
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel.Contains("KOERPER_"));
        }

        // ------------------------------------------------------------------
        //  Bauteilkörper
        // ------------------------------------------------------------------

        [Fact]
        public void Aussenwand_im_Aussenmass_wird_nach_innen_extrudiert_mit_Fenster_ausgespart()
        {
            AbbildBauteil w = Bauteil(Gebaeude(LesenDatei(SCHALE)), "aw-a-sued");
            Assert.Equal(Koerperbildner.ART_FLAECHENEXTRUSION, w.Koerper.Art);
            Assert.Empty(w.Koerper.Vermerke);
            // Dicke 200 mm + 100 mm, Fläche 15 m² − 1,8 m² Fenster; der Körper liegt zwischen y = 0 und y = 0,30.
            Assert.Equal((15.0 - 1.8) * 0.3, Volumen(w.Koerper), 6);
            Assert.Equal(0.0, w.Koerper.PunkteM.Min(p => p[1]), 9);
            Assert.Equal(0.3, w.Koerper.PunkteM.Max(p => p[1]), 9);
            // Die Laibung trägt die Öffnung.
            Assert.Contains("fenster-a-sued", w.Koerper.Quellflaechen);

            Dateikoerper f = Assert.Single(w.Oeffnungen).Koerper;
            Assert.NotNull(f);
            Assert.Equal(new[] { Koerpervermerk.Vorgabedicke }, f.Vermerke);
            double dicke = GbxmlKoerper.Vorgabedicke(Bauteilart.Fenster);
            Assert.Equal(1.8 * dicke, Volumen(f), 6);
            Assert.Equal(0.15 - dicke / 2.0, f.PunkteM.Min(p => p[1]), 9);
            Assert.Equal(0.15 + dicke / 2.0, f.PunkteM.Max(p => p[1]), 9);
        }

        [Fact]
        public void Innenwand_in_der_Achse_beidseitig_Boden_und_Dach_von_der_Innenoberflaeche_weg()
        {
            AbbildGebaeude g = Gebaeude(LesenDatei(SCHALE));
            AbbildBauteil iw = Bauteil(g, "iw-ab");
            Assert.Empty(iw.Koerper.Vermerke);
            Assert.Equal(4.0 * 3.0 * 0.12, Volumen(iw.Koerper), 6);
            Assert.Equal(4.94, iw.Koerper.PunkteM.Min(p => p[0]), 9);
            Assert.Equal(5.06, iw.Koerper.PunkteM.Max(p => p[0]), 9);

            AbbildBauteil boden = Bauteil(g, "boden-a");
            Assert.Equal(-0.25, boden.Koerper.PunkteM.Min(p => p[2]), 9);
            Assert.Equal(0.0, boden.Koerper.PunkteM.Max(p => p[2]), 9);

            // Pultdach: 50 mm in Metern nach dem Dokumentkopf plus 200 mm, vom Raum weg nach oben.
            AbbildBauteil dach = Bauteil(g, "dach-b");
            Assert.Empty(dach.Koerper.Vermerke);
            Assert.Equal(5.0 * Math.Sqrt(17.0) * 0.25, Volumen(dach.Koerper), 4); // Punkte auf Zonenkoerper.STELLEN gerundet
            Assert.True(dach.Koerper.PunkteM.Max(p => p[2]) > 4.0);
        }

        [Fact]
        public void Dicke_aus_Schichten_in_mm_und_m()
        {
            AbbildGebaeude g = Gebaeude(LesenDatei(SCHALE));
            Assert.Equal(0.25, Bauteil(g, "dach-b").Aufbau.Schichten.Sum(s => s.DickeM.Value), 12);
            Assert.Equal(0.30, Bauteil(g, "aw-b-ost").Aufbau.Schichten.Sum(s => s.DickeM.Value), 12);
            // Ostwand B (Trapez): Außenmaß, nach innen.
            Dateikoerper ost = Bauteil(g, "aw-b-ost").Koerper;
            Assert.Equal(4.0 * 3.5 * 0.3, Volumen(ost), 6);
            Assert.Equal(9.7, ost.PunkteM.Min(p => p[0]), 9);
        }

        [Fact]
        public void Ohne_Aufbau_gilt_die_Vorgabedicke_und_ohne_Messung_die_Achse()
        {
            AbbildBauteil giebel = Bauteil(Gebaeude(LesenDatei(SCHALE)), "aw-b-giebel");
            Assert.Equal(new[] { Koerpervermerk.Vorgabedicke, Koerpervermerk.Bezugsebene_angenommen }, giebel.Koerper.Vermerke);
            double dicke = GbxmlKoerper.Vorgabedicke(Bauteilart.Aussenwand);
            Assert.Equal(2.0 * dicke, Volumen(giebel.Koerper), 6);
            Assert.Equal(5.0 - dicke / 2.0, giebel.Koerper.PunkteM.Min(p => p[0]), 9);

            // Ohne Schale misst nichts: alle Flächen beidseitig, angenommen.
            AbbildGebaeude g = Gebaeude(LesenDatei(FLAECHEN));
            Assert.All(g.Bauteile, b => Assert.Contains(Koerpervermerk.Bezugsebene_angenommen, b.Koerper.Vermerke));
            Assert.Equal(-0.15, Bauteil(g, "aw-a-sued").Koerper.PunkteM.Min(p => p[1]), 9);
        }

        [Fact]
        public void Jede_Flaeche_ergibt_einen_Bauteilkoerper_und_alle_bleiben_unter_der_Grenze()
        {
            AbbildGebaeude g = Gebaeude(LesenDatei(SCHALE));
            Assert.Equal(12, g.Bauteile.Count);
            Assert.All(g.Bauteile, b => Assert.NotNull(b.Koerper));
            Assert.All(g.Bauteile, b => Assert.Equal(Koerperquelle.AusFlaechen, b.Koerper.Quelle));
            int dreiecke = g.Raeume.Sum(r => r.Koerper.DreieckZahl) + g.Bauteile.Sum(b => b.Koerper.DreieckZahl + b.Oeffnungen.Sum(o => o.Koerper.DreieckZahl));
            Assert.InRange(dreiecke, 1, Dateikoerper.DREIECKSGRENZE);
        }

        // ------------------------------------------------------------------
        //  Determinismus und Rechenzeit
        // ------------------------------------------------------------------

        [Fact]
        public void Dieselbe_Datei_ergibt_dieselben_Koerper_byteweise()
        {
            foreach (string datei in new[] { SCHALE, FLAECHEN })
                Assert.Equal(Text(LesenDatei(datei)), Text(LesenDatei(datei)));
        }

        /// <summary>
        /// Probe 42 (Rechenzeit): ein Raster aus 10 × 10 Räumen mit gemeinsamen Innenwänden, mit und ohne Schale; gemessen
        /// wird allein die Körperbildung des Gebäudes. Ziel unter 50 ms je Gebäude (17.7) — die Zusicherung lässt dem Läufer Luft.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Rechenzeit_der_Koerperbildung_je_Gebaeude(bool mitSchale)
        {
            XDocument d = Raster(10, mitSchale);
            var abbild = (GbxmlAbbild)Lesen(d);
            AbbildGebaeude g = Gebaeude(abbild);
            Assert.All(g.Raeume, r => Assert.NotNull(r.Koerper));
            Assert.Equal(mitSchale ? GbxmlKoerper.ART_CLOSEDSHELL : GbxmlKoerper.ART_RAUMFLAECHEN, g.Raeume[0].Koerper.Art);
            double beste = double.MaxValue;
            for (int i = 0; i < 20; i++)
            {
                var uhr = Stopwatch.StartNew();
                GbxmlKoerper.Bilden(abbild, d.Root);
                beste = Math.Min(beste, uhr.Elapsed.TotalMilliseconds);
            }
            int dreiecke = g.Raeume.Sum(r => r.Koerper.DreieckZahl) + g.Bauteile.Sum(b => b.Koerper?.DreieckZahl ?? 0);

            // Anteil der Hüllprüfung je Raum (aus den Flächen des Raums): sie läuft je Raum, nicht je Gebäude.
            var je = g.Raeume.Select(r => g.Bauteile.Where(b => b.Nachbarn.Any(x => x.Kennung == r.Kennung))
                                                    .Select(b => new Quellflaeche { Kennung = b.Kennung, Aussen = b.RandpunkteM }).ToList()).ToList();
            double huelle = double.MaxValue;
            for (int i = 0; i < 20; i++)
            {
                var uhr = Stopwatch.StartNew();
                foreach (List<Quellflaeche> q in je) Assert.True(Koerperbildner.Huelle(q).Gebildet);
                huelle = Math.Min(huelle, uhr.Elapsed.TotalMilliseconds);
            }
            double extrusion = double.MaxValue;
            for (int i = 0; i < 20; i++)
            {
                var uhr = Stopwatch.StartNew();
                foreach (AbbildBauteil b in g.Bauteile)
                    Koerperbildner.Extrusion(new Quellflaeche { Kennung = b.Kennung, Aussen = b.RandpunkteM }, 0.2, Extrusionsrichtung.Beidseitig);
                extrusion = Math.Min(extrusion, uhr.Elapsed.TotalMilliseconds);
            }
            _ausgabe.WriteLine("Extrusionen allein " + extrusion.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
            _ausgabe.WriteLine("Körperbildung " + (mitSchale ? "mit Schale" : "aus Flächen") + ": " + g.Raeume.Count + " Räume, "
                               + g.Bauteile.Count + " Flächen, " + dreiecke + " Dreiecke, " + beste.ToString("0.0", CultureInfo.InvariantCulture)
                               + " ms; davon Hüllen aus Flächen je Raum " + huelle.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
            Assert.True(beste < 2000.0, "Körperbildung " + beste + " ms");
        }

        // ------------------------------------------------------------------
        //  Hilfen
        // ------------------------------------------------------------------

        private static GebaeudeAbbild LesenDatei(string name)
        {
            using (FileStream s = File.OpenRead(GbxmlImportTests.Probe(name)))
                return new GbxmlLeser().Lesen(s, new GbxmlImportProfil(), null, CancellationToken.None);
        }

        private static GebaeudeAbbild Lesen(XDocument d)
        {
            using (var s = new MemoryStream(Encoding.UTF8.GetBytes(d.ToString())))
                return new GbxmlLeser().Lesen(s, new GbxmlImportProfil(), null, CancellationToken.None);
        }

        private static AbbildGebaeude Gebaeude(GebaeudeAbbild a) => Assert.Single(a.Gebaeude);

        private static AbbildRaum Raum(AbbildGebaeude g, string kennung) => g.Raeume.Single(r => r.Kennung == kennung);

        private static AbbildBauteil Bauteil(AbbildGebaeude g, string kennung) => g.Bauteile.Single(b => b.Kennung == kennung);

        private static XElement Element(XDocument d, string name, string id)
            => d.Descendants().Single(e => e.Name.LocalName == name && (string)e.Attribute("id") == id);

        /// <summary>Das Volumen eines geschlossenen Netzes nach dem Divergenzsatz [m³].</summary>
        private static double Volumen(Dateikoerper k)
        {
            double v = 0.0;
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                v += a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0]);
            }
            return v / 6.0;
        }

        private static double[] Normale(Dateikoerper k, string quelle)
            => k.Normalen[Enumerable.Range(0, k.DreieckZahl).First(i => k.Quellflaechen[i] == quelle)];

        private static string Text(GebaeudeAbbild a)
        {
            var t = new StringBuilder();
            foreach (AbbildGebaeude g in a.Gebaeude)
            {
                foreach (AbbildRaum r in g.Raeume) t.Append("R ").Append(r.Kennung).Append('\n').Append(r.Koerper?.Text());
                foreach (AbbildBauteil b in g.Bauteile)
                {
                    t.Append("B ").Append(b.Kennung).Append('\n').Append(b.Koerper?.Text());
                    foreach (AbbildBauteil o in b.Oeffnungen) t.Append("O ").Append(o.Kennung).Append('\n').Append(o.Koerper?.Text());
                }
            }
            return t.ToString();
        }

        /// <summary>Ein Raster aus n × n Räumen (4 × 4 × 3 m), Außenwände außen, Innenwände in der Achse, Schale wahlweise.</summary>
        private static XDocument Raster(int n, bool mitSchale)
        {
            XNamespace ns = "http://www.gbxml.org/schema";
            XElement Schleife(params double[][] p)
                => new XElement(ns + "PolyLoop", p.Select(q => new XElement(ns + "CartesianPoint",
                       q.Select(c => new XElement(ns + "Coordinate", c.ToString("R", CultureInfo.InvariantCulture))))));
            double[] P(double x, double y, double z) => new[] { x, y, z };
            string R(int i, int j) => "r" + i + "_" + j;
            var building = new XElement(ns + "Building", new XAttribute("id", "geb"), new XAttribute("buildingType", "Office"));
            var campus = new XElement(ns + "Campus", new XAttribute("id", "c"), building);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var space = new XElement(ns + "Space", new XAttribute("id", R(i, j)), new XElement(ns + "Area", "16"));
                    if (mitSchale)
                    {
                        double x0 = 4 * i + (i == 0 ? 0.3 : 0.06), x1 = 4 * i + 4 - (i == n - 1 ? 0.3 : 0.06);
                        double y0 = 4 * j + (j == 0 ? 0.3 : 0.06), y1 = 4 * j + 4 - (j == n - 1 ? 0.3 : 0.06);
                        space.Add(new XElement(ns + "ShellGeometry", new XElement(ns + "ClosedShell",
                            Schleife(P(x0, y0, 0), P(x0, y1, 0), P(x1, y1, 0), P(x1, y0, 0)),
                            Schleife(P(x0, y0, 3), P(x1, y0, 3), P(x1, y1, 3), P(x0, y1, 3)),
                            Schleife(P(x0, y0, 0), P(x1, y0, 0), P(x1, y0, 3), P(x0, y0, 3)),
                            Schleife(P(x1, y0, 0), P(x1, y1, 0), P(x1, y1, 3), P(x1, y0, 3)),
                            Schleife(P(x1, y1, 0), P(x0, y1, 0), P(x0, y1, 3), P(x1, y1, 3)),
                            Schleife(P(x0, y1, 0), P(x0, y0, 0), P(x0, y0, 3), P(x0, y1, 3)))));
                    }
                    building.Add(space);
                }
            int z = 0;
            void Flaeche(string typ, string[] raeume, XElement schleife)
                => campus.Add(new XElement(ns + "Surface", new XAttribute("id", "s" + z++), new XAttribute("surfaceType", typ),
                       raeume.Select(r => new XElement(ns + "AdjacentSpaceId", new XAttribute("spaceIdRef", r))),
                       new XElement(ns + "PlanarGeometry", schleife)));
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    double x0 = 4 * i, x1 = x0 + 4, y0 = 4 * j, y1 = y0 + 4;
                    Flaeche("SlabOnGrade", new[] { R(i, j) }, Schleife(P(x0, y0, 0), P(x0, y1, 0), P(x1, y1, 0), P(x1, y0, 0)));
                    Flaeche("Roof", new[] { R(i, j) }, Schleife(P(x0, y0, 3), P(x1, y0, 3), P(x1, y1, 3), P(x0, y1, 3)));
                    if (j == 0) Flaeche("ExteriorWall", new[] { R(i, j) }, Schleife(P(x0, y0, 0), P(x1, y0, 0), P(x1, y0, 3), P(x0, y0, 3)));
                    if (i == 0) Flaeche("ExteriorWall", new[] { R(i, j) }, Schleife(P(x0, y1, 0), P(x0, y0, 0), P(x0, y0, 3), P(x0, y1, 3)));
                    Flaeche(i == n - 1 ? "ExteriorWall" : "InteriorWall", i == n - 1 ? new[] { R(i, j) } : new[] { R(i, j), R(i + 1, j) },
                            Schleife(P(x1, y0, 0), P(x1, y1, 0), P(x1, y1, 3), P(x1, y0, 3)));
                    Flaeche(j == n - 1 ? "ExteriorWall" : "InteriorWall", j == n - 1 ? new[] { R(i, j) } : new[] { R(i, j), R(i, j + 1) },
                            Schleife(P(x1, y1, 0), P(x0, y1, 0), P(x0, y1, 3), P(x1, y1, 3)));
                }
            return new XDocument(new XElement(ns + "gbXML", new XAttribute("lengthUnit", "Meters"), new XAttribute("areaUnit", "SquareMeters"),
                                              new XAttribute("volumeUnit", "CubicMeters"), new XAttribute("temperatureUnit", "C"), campus));
        }
    }
}
