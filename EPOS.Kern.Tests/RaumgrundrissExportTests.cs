using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Ifc4.Interfaces;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>HC-5 Teil B — der gespeicherte Grundriss je Raum im Exportmodell</b> (Konzept HottCAD-Verbund 11.4, 11.9 F8, F10):
    /// Vorrang und Rückfall in der <see cref="Zonengeometrie"/>, Löcher, mehrere Räume je Zone, Zeilen ohne Zone, der
    /// IFC-Rundlauf (Prismen je Zone, mit dem eigenen Leser zurückgelesen) und der gbXML-Rundlauf an den Raumkörperproben,
    /// ohne Grundriss bytegleich, keine Rechengröße berührt, die Zeilen des Importprotokolls. Ohne Datenbank.
    /// </summary>
    public sealed class RaumgrundrissExportTests : IDisposable
    {
        private const double MM = 0.001;
        private const double M2 = 1e-6;
        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static GebaeudeAbbild Lesen(string datei)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a.Abbild;
        }

        private static IReadOnlyList<Raumgrundriss> Grundrisse(string datei) => GebaeudeRaumgrundrisse.Bilden(Lesen(datei), 0);

        /// <summary>Das Zweizonenhaus des Exports; Zone A trägt die Grundrisse der Probe, Zone B keine.</summary>
        private static GebaeudeAbbild Haus(IReadOnlyList<Raumgrundriss> zoneA)
        {
            GebaeudeAbbild a = GbxmlStufe2Tests.Zweizonen();
            a.Gebaeude[0].Raeume[0].Grundrisse = zoneA;
            return a;
        }

        private static double Flaeche(IReadOnlyList<double[]> ring)
        {
            double s = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] p = ring[i], q = ring[(i + 1) % ring.Count];
                s += p[0] * q[1] - q[0] * p[1];
            }
            return s / 2.0;
        }

        /// <summary>Die Außenringe einer Grundrissliste samt Fläche ohne Löcher, Boden und Höhe — die erwarteten Prismen.</summary>
        private static List<(IReadOnlyList<double[]> Aussen, double Flaeche, double Boden, double Hoehe)> Prismen(IReadOnlyList<Raumgrundriss> gr)
        {
            var liste = new List<(IReadOnlyList<double[]>, double, double, double)>();
            foreach (Raumgrundriss g in gr)
                for (int i = 0; i < g.Ringe.Count; i++)
                {
                    if (g.Ringe[i].IstLoch) continue;
                    double f = g.Ringe[i].FlaecheM2;
                    for (int j = i + 1; j < g.Ringe.Count && g.Ringe[j].IstLoch; j++) f -= g.Ringe[j].FlaecheM2;
                    liste.Add((g.Ringe[i].PunkteM, f, g.BodenM, g.HoeheM));
                }
            return liste;
        }

        public static IEnumerable<object[]> Proben() => new[]
        {
            new object[] { "ifc4_koerper_nachbarn.ifc" },
            new object[] { "ifc4_koerper_extrusion_loch.ifc" },
            new object[] { "ifc4_koerper_grundriss_stufe.ifc" },
        };

        // ==================================================================
        //  Zonengeometrie: Vorrang, Rückfall, Löcher, mehrere Räume je Zone
        // ==================================================================

        [Theory]
        [MemberData(nameof(Proben))]
        public void Die_Zone_mit_Grundriss_steht_als_Prisma_die_ohne_als_Rechteck_daneben(string datei)
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse(datei);
            Assert.NotEmpty(gr);
            (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(Haus(gr));
            Raumumriss a = z.Raeume[0], b = z.Raeume[1];

            Assert.Equal(Geometrieherkunft.Dateikoerper, a.Herkunft);
            Assert.True(a.AusGrundriss);
            var erwartet = Prismen(gr);
            Assert.Equal(erwartet.Count, a.Polygone.Count);
            for (int i = 0; i < erwartet.Count; i++)
            {
                Assert.Equal(erwartet[i].Flaeche, a.Polygone[i].FlaecheM2, 9);
                Assert.Equal(erwartet[i].Boden, a.Polygone[i].PrismaBodenM.Value, 9);
                Assert.Equal(erwartet[i].Hoehe, a.Polygone[i].PrismaHoeheM.Value, 9);
                Assert.Equal(erwartet[i].Aussen.Count, a.Polygone[i].Punkte.Count);
            }
            Assert.Equal(erwartet.Sum(x => x.Flaeche), a.PolygonflaecheM2, 9);
            Assert.Equal(50.0, a.FlaecheM2);   // die Fläche der Zone bleibt die des Modells

            // Rückfall: Zone B schematisch, rechts neben den Prismen.
            Assert.Equal(Geometrieherkunft.Schematisch, b.Herkunft);
            Assert.True(b.Polygone[0].Punkte.Min(p => p[0]) > a.Polygone.SelectMany(p => p.Punkte).Max(p => p[0]));
            Assert.Contains(z.Meldungen, m => m.Schluessel == Zonengeometrie.DATEIKOERPER);
            Assert.Contains(z.Meldungen, m => m.Schluessel == Zonengeometrie.SCHEMATISCH);

            // Körper: A aus dem Grundriss, B das schematische Prisma.
            Assert.NotNull(k);
            Assert.True(k.Raum(a.RaumKennung).AusGrundriss);
            Assert.False(k.Raum(b.RaumKennung).AusGrundriss);
            Assert.True(k.Schematisch);
            Assert.Equal(1, k.ZahlAusGrundriss);
        }

        [Fact]
        public void Loecher_folgen_ihrem_Aussenring_und_mindern_die_Flaeche()
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse("ifc4_koerper_extrusion_loch.ifc");
            Assert.Contains(gr, g => g.Ringe.Any(r => r.IstLoch));
            Zonengeometrie z = GbxmlSchreiber.Raumgeometrie(Haus(gr)).Geometrie;
            Umrisspolygon p = Assert.Single(z.Raeume[0].Polygone);
            Assert.Single(p.Loecher);
            Assert.True(Flaeche(p.Loecher[0]) < 0, "Loch im Uhrzeigersinn");
            Assert.Equal(Flaeche(p.Punkte) + Flaeche(p.Loecher[0]), p.FlaecheM2, 9);
            Assert.Equal(gr.Single().RingflaecheM2, p.FlaecheM2, 9);
        }

        [Fact]
        public void Mehrere_Raeume_je_Zone_und_ohne_Grundriss_keine_Aenderung()
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse("ifc4_koerper_nachbarn.ifc");
            Assert.True(gr.Count >= 2);
            Zonengeometrie z = GbxmlSchreiber.Raumgeometrie(Haus(gr)).Geometrie;
            Assert.Equal(gr.Count, z.Raeume[0].Polygone.Count);
            Assert.Equal(Geometrieherkunft.Dateikoerper, z.Zonen.Count > 0 ? Gesamt(z) : Geometrieherkunft.Dateikoerper);

            // Ohne Grundriss: dieselbe Geometrie wie bisher (schematisch, keine Prismen).
            Zonengeometrie ohne = GbxmlSchreiber.Raumgeometrie(Haus(Array.Empty<Raumgrundriss>())).Geometrie;
            Assert.All(ohne.Raeume, r => Assert.Equal(Geometrieherkunft.Schematisch, r.Herkunft));
            Assert.All(ohne.Raeume, r => Assert.False(r.AusGrundriss));
            Assert.DoesNotContain(ohne.Meldungen, m => m.Schluessel == Zonengeometrie.DATEIKOERPER);
        }

        private static Geometrieherkunft Gesamt(Zonengeometrie z) => z.Raeume[0].Herkunft;

        [Fact]
        public void Echte_Raumgrenzen_behalten_Vorrang_vor_einem_Grundriss_aus_Raumgrenzen()
        {
            // Ein Raum mit Bodengrenze 4 x 5 m; der Grundriss aus Raumgrenzen (F6) ändert nichts, der aus dem Körper geht vor.
            Umrissraum Raum(Raumgrundriss g) => new Umrissraum
            {
                Kennung = "R1", Name = "Raum", FlaecheM2 = 20.0, VolumenM3 = 50.0,
                Grundrisse = g == null ? Array.Empty<Raumgrundriss>() : new[] { g },
            };
            static void Boden(Umrissraum r) => r.Seiten.Add(new Umrissseite
            {
                Verweis = new Grenzverweis("G1", "B1", Bauteilart.Bodenplatte, Grenzstellung.Boden, Randbedingung.Erdreich, null),
                RandpunkteM = new[] { new[] { 0.0, 0.0, 0.0 }, new[] { 4.0, 0.0, 0.0 }, new[] { 4.0, 5.0, 0.0 }, new[] { 0.0, 5.0, 0.0 } },
                FlaecheM2 = 20.0,
            });
            var ring = new Grundrissring(new List<long[]> { new long[] { 0, 0 }, new long[] { 3000, 0 }, new long[] { 3000, 3000 }, new long[] { 0, 3000 } });
            Raumgrundriss ausGrenzen = Raumgrundriss.Bilden("R1", "Raum", null, null, 20.0, 50.0, Umrissherleitung.Boden,
                                                            new[] { ring }, 0.0, 2.5, null, false);
            Raumgrundriss ausKoerper = Raumgrundriss.Bilden("R1", "Raum", null, null, 20.0, 50.0, Umrissherleitung.KoerperBoden,
                                                            new[] { ring }, 0.0, 2.5, null, false);

            Zonengeometrie Bilden(Raumgrundriss g, bool mitGrenze)
            {
                var e = new Umrisseingang();
                Umrissraum r = Raum(g);
                if (mitGrenze) Boden(r);
                e.Raeume.Add(r);
                return Zonengeometrie.AusRaumgrenzen(e);
            }

            Raumumriss echt = Bilden(ausGrenzen, true).Raeume[0];
            Assert.Equal(Geometrieherkunft.Raumgrenzen, echt.Herkunft);
            Assert.False(echt.AusGrundriss);
            Assert.Equal(20.0, echt.PolygonflaecheM2, 9);

            Raumumriss koerper = Bilden(ausKoerper, true).Raeume[0];
            Assert.Equal(Geometrieherkunft.Dateikoerper, koerper.Herkunft);
            Assert.Equal(9.0, koerper.PolygonflaecheM2, 9);

            Raumumriss nurGrenzenGrundriss = Bilden(ausGrenzen, false).Raeume[0];   // Export: keine Raumgrenzen im Abbild
            Assert.Equal(Geometrieherkunft.Raumgrenzen, nurGrenzenGrundriss.Herkunft);
            Assert.True(nurGrenzenGrundriss.AusGrundriss);

            Raumumriss rueckfall = Bilden(null, false).Raeume[0];
            Assert.Equal(Geometrieherkunft.Schematisch, rueckfall.Herkunft);
        }

        [Fact]
        public void Zeilen_ohne_Zone_uebergeht_der_Export()
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse("ifc4_koerper_nachbarn.ifc");
            var zeilen = gr.Select((g, i) => new Raumgrundriss
            {
                IdZone = i == 0 ? (int?)null : 7, Quellkennung = g.Quellkennung, BodenM = g.BodenM, HoeheM = g.HoeheM, Ringe = g.Ringe,
                RingflaecheM2 = g.RingflaecheM2, Herleitung = g.Herleitung,
            }).ToList();
            IReadOnlyDictionary<int, IReadOnlyList<Raumgrundriss>> je = GebaeudeExportSatz.GrundrisseJeZone(zeilen);
            Assert.Equal(new[] { 7 }, je.Keys.ToArray());
            Assert.Equal(gr.Count - 1, je[7].Count);
            Assert.Equal(zeilen.Skip(1).Select(x => x.Quellkennung), je[7].Select(x => x.Quellkennung));
        }

        // ==================================================================
        //  IFC-Rundlauf
        // ==================================================================

        [Theory]
        [MemberData(nameof(Proben))]
        public void IFC_Export_schreibt_je_Zone_die_Prismen_und_der_Leser_findet_sie_wieder(string datei)
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse(datei);
            GebaeudeAbbild haus = Haus(gr);
            byte[] ifc = IfcExportProbe.Schreiben(haus);
            var m = IfcExportProbe.Modell(ifc);

            IIfcSpace raum = m.Instances.OfType<IIfcSpace>().Single(s => s.Name == "Büro");
            List<IIfcExtrudedAreaSolid> koerper = raum.Representation.Representations.SelectMany(r => r.Items).OfType<IIfcExtrudedAreaSolid>().ToList();
            var erwartet = Prismen(gr);
            Assert.Equal(erwartet.Count, koerper.Count);
            for (int i = 0; i < erwartet.Count; i++)
            {
                IIfcExtrudedAreaSolid s = koerper[i];
                Assert.Equal(erwartet[i].Boden, (double)s.Position.Location.Z, 3);
                Assert.Equal(0.0, (double)s.Position.Location.X, 9);
                Assert.Equal(erwartet[i].Hoehe, (double)s.Depth, 3);
                IIfcArbitraryClosedProfileDef p = Assert.IsAssignableFrom<IIfcArbitraryClosedProfileDef>(s.SweptArea);
                List<double[]> aussen = Punkte(p.OuterCurve);
                double f = Flaeche(aussen);
                if (p is IIfcArbitraryProfileDefWithVoids mitLoch)
                    foreach (IIfcCurve l in mitLoch.InnerCurves) f += Flaeche(Punkte(l));
                Assert.Equal(erwartet[i].Flaeche, f, 6);
                for (int j = 0; j < erwartet[i].Aussen.Count; j++)
                {
                    Assert.Equal(erwartet[i].Aussen[j][0], aussen[j][0], 3);
                    Assert.Equal(erwartet[i].Aussen[j][1], aussen[j][1], 3);
                }
            }
            Assert.Equal(erwartet.Any(x => x.Aussen != null) && gr.Any(g => g.Ringe.Any(r => r.IstLoch)),
                         koerper.Any(s => s.SweptArea is IIfcArbitraryProfileDefWithVoids));
            Assert.Contains("aus Dateikörper", raum.Description?.ToString() ?? "", StringComparison.Ordinal);

            // Der eigene Leser liest den Raumkörper der Zone zurück: Fläche, Boden und Höhe des Grundrisses.
            GebaeudeAbbild zurueck = IfcExportProbe.Lesen(ifc);
            AbbildRaum r = zurueck.Gebaeude[0].Raeume.Single(x => x.Name == "Büro");
            Assert.NotNull(r.Koerper);
            Koerpergrundriss neu = Koerpergrundriss.Ableiten(r.Koerper);
            Assert.True(neu.Traegt);
            Assert.Equal(erwartet.Min(x => x.Boden), neu.BodenM, 3);
            Assert.Equal(erwartet.Max(x => x.Boden + x.Hoehe) - erwartet.Min(x => x.Boden), neu.HoeheM, 3);
            if (erwartet.Select(x => x.Boden).Distinct().Count() == 1)
                Assert.Equal(erwartet.Sum(x => x.Flaeche), neu.RingflaecheM2, 6);
        }

        private static List<double[]> Punkte(IIfcCurve kurve)
        {
            var l = Assert.IsAssignableFrom<IIfcPolyline>(kurve);
            List<double[]> p = l.Points.Select(q => new[] { (double)q.X, (double)q.Y }).ToList();
            p.RemoveAt(p.Count - 1);   // Schlusspunkt
            return p;
        }

        // ==================================================================
        //  gbXML-Rundlauf
        // ==================================================================

        [Theory]
        [MemberData(nameof(Proben))]
        public void GbXML_Export_schreibt_die_Prismen_als_ClosedShell(string datei)
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse(datei);
            XDocument x = XDocument.Load(new MemoryStream(GbxmlExportProbe.Schreiben(Haus(gr))));
            XElement space = x.Descendants(NS + "Space").Single(s => (string)s.Element(NS + "Name") == "Büro");
            List<List<double[]>> loops = space.Element(NS + "ShellGeometry").Element(NS + "ClosedShell").Elements(NS + "PolyLoop")
                .Select(pl => pl.Elements(NS + "CartesianPoint")
                    .Select(c => c.Elements(NS + "Coordinate").Select(v => double.Parse(v.Value, CultureInfo.InvariantCulture)).ToArray()).ToList())
                .ToList();
            var erwartet = Prismen(gr);
            int seiten = gr.Sum(g => g.Ringe.Sum(r => r.PunkteMm.Count));
            Assert.Equal(2 * erwartet.Count + seiten, loops.Count);
            foreach (var e in erwartet)
            {
                // Decke: waagerecht auf Boden + Höhe, mit Stegen um die Löcher - die Fläche ist die des Rings ohne Löcher.
                List<double[]> decke = loops.Single(l => l.All(p => Math.Abs(p[2] - (e.Boden + e.Hoehe)) < MM) && l.Count >= e.Aussen.Count
                                                        && Math.Abs(Flaeche(l) - e.Flaeche) < M2 * 10 && l.Any(p => Math.Abs(p[0] - e.Aussen[0][0]) < MM && Math.Abs(p[1] - e.Aussen[0][1]) < MM));
                Assert.Equal(e.Flaeche, Flaeche(decke), 6);
                List<double[]> boden = loops.First(l => l.All(p => Math.Abs(p[2] - e.Boden) < MM) && Math.Abs(Flaeche(l) + e.Flaeche) < M2 * 10);
                Assert.Equal(-e.Flaeche, Flaeche(boden), 6);   // Boden nach unten: im Uhrzeigersinn von oben
            }
        }

        [Fact]
        public void Ohne_Rechteck_entfaellt_schematisch_an_Datei_und_Projekt()
        {
            IReadOnlyList<Raumgrundriss> gr = Grundrisse("ifc4_koerper_nachbarn.ifc");
            GebaeudeAbbild haus = Haus(gr);
            haus.Gebaeude[0].Raeume[1].Grundrisse = Grundrisse("ifc4_koerper_extrusion_loch.ifc");
            (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(haus);
            Assert.False(z.Schematisch);
            Assert.False(k.Schematisch);
            Assert.All(z.Zonen, x => Assert.NotEqual(Geometrieherkunft.Schematisch, x.Herkunft));

            byte[] ifc = IfcExportProbe.Schreiben(haus);
            var m = IfcExportProbe.Modell(ifc);
            Assert.DoesNotContain(T(IfcSchreiber.PROJEKT_SCHEMATISCH).Replace("{0}", "").Trim(),
                                  m.Instances.OfType<IIfcProject>().Single().Name?.ToString() ?? "", StringComparison.Ordinal);
            Assert.Empty(m.Instances.OfType<IIfcAnnotation>());
            Assert.Contains(T(IfcSchreiber.DATEI_STUFE_S3_GRUNDRISS), m.Header.FileDescription.Description);

            XDocument x = XDocument.Load(new MemoryStream(GbxmlExportProbe.Schreiben(haus)));
            Assert.Equal(T("GEXP_DATEI_DATEIKOERPER"), x.Descendants(NS + "Building").Single().Element(NS + "Description").Value);
            Assert.Equal("2", GbxmlSchreiber.Geometriemeldungen(haus).Single().Werte[0]);
        }

        [Fact]
        public void Ohne_Grundriss_sind_beide_Exporte_bytegleich()
        {
            byte[] ifcVorher = IfcExportProbe.Schreiben(GbxmlStufe2Tests.Zweizonen());
            byte[] ifcLeer = IfcExportProbe.Schreiben(Haus(Array.Empty<Raumgrundriss>()));
            Assert.Equal(ifcVorher, ifcLeer);
            byte[] gbVorher = GbxmlExportProbe.Schreiben(GbxmlStufe2Tests.Zweizonen());
            byte[] gbLeer = GbxmlExportProbe.Schreiben(Haus(Array.Empty<Raumgrundriss>()));
            Assert.Equal(gbVorher, gbLeer);
            Assert.DoesNotContain(GbxmlSchreiber.Geometriemeldungen(Haus(Array.Empty<Raumgrundriss>())),
                                  m => m.Schluessel == GbxmlSchreiber.GEOMETRIE_DATEIKOERPER);
        }

        [Fact]
        public void Die_Vorschau_zaehlt_schematisch_nur_die_Rechtecke()
        {
            IReadOnlyList<PruefMeldung> m = GbxmlSchreiber.Geometriemeldungen(Haus(Grundrisse("ifc4_koerper_nachbarn.ifc")));
            Assert.Equal("1", m.Single(x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_SCHEMATISCH).Werte[0]);
            Assert.Equal("1", m.Single(x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_DATEIKOERPER).Werte[0]);
        }

        // ==================================================================
        //  Keine Rechengröße; Importprotokoll
        // ==================================================================

        [Theory]
        [MemberData(nameof(Proben))]
        public void Ansicht_und_Export_beruehren_keine_Rechengroesse(string datei)
        {
            GebaeudeAbbild a = Lesen(datei);
            string Stand() => string.Join("|", a.Gebaeude[0].Raeume.Select(r => string.Join(";",
                r.FlaecheM2?.ToString("R", CultureInfo.InvariantCulture), r.VolumenM3?.ToString("R", CultureInfo.InvariantCulture),
                r.HoeheM?.ToString("R", CultureInfo.InvariantCulture),
                r.GrundrissM == null ? "" : string.Join(",", r.GrundrissM.Select(p => p[0].ToString("R", CultureInfo.InvariantCulture) + " " + p[1].ToString("R", CultureInfo.InvariantCulture))))));
            string vorher = Stand();
            Zonengeometrie z = GebaeudeGrundriss.BildenMitGrundriss(a, 0, null, out IReadOnlyList<Raumgrundriss> gr);
            Assert.NotEmpty(gr);
            Assert.Contains(z.Raeume, r => r.Herkunft != Geometrieherkunft.Schematisch);
            Assert.Equal(vorher, Stand());
            Assert.Equal(a.Gebaeude[0].Raeume.Select(r => r.FlaecheM2), z.Raeume.Select(r => r.FlaecheM2));
        }

        [Fact]
        public void Das_Importprotokoll_nennt_Herleitungen_und_Flaechenabweichung()
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "ifc4_koerper_grundriss_stufe.ifc");
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            PruefMeldung h = Assert.Single(a.Meldungen, m => m.Schluessel == GebaeudeRaumgrundrisse.PROT_HERLEITUNG);
            IReadOnlyList<Raumgrundriss> gr = GebaeudeRaumgrundrisse.Bilden(a.Abbild, 0);
            Assert.Equal(gr.Count.ToString(CultureInfo.InvariantCulture), h.Werte[0]);
            bool abweichend = gr.Any(g => g.Vermerke.Contains(Grundrissvermerk.Flaeche));
            Assert.Equal(abweichend, a.Meldungen.Any(m => m.Schluessel == GebaeudeRaumgrundrisse.PROT_FLAECHE));
            Assert.False(string.IsNullOrEmpty(T(GebaeudeRaumgrundrisse.PROT_FLAECHE)));
            Assert.False(string.IsNullOrEmpty(T(GebaeudeRaumgrundrisse.PROT_HERLEITUNG)));

            var g1 = Raumgrundriss.Bilden("X", "Saal", null, null, 10.0, 25.0, Umrissherleitung.KoerperHuelle,
                new[] { new Grundrissring(new List<long[]> { new long[] { 0, 0 }, new long[] { 4000, 0 }, new long[] { 4000, 4000 }, new long[] { 0, 4000 } }) },
                0.0, 2.5, null, false);
            IReadOnlyList<PruefMeldung> m2 = GebaeudeRaumgrundrisse.Meldungen(new[] { g1 });
            PruefMeldung f = Assert.Single(m2, m => m.Schluessel == GebaeudeRaumgrundrisse.PROT_FLAECHE);
            Assert.Equal(PruefStufe.Warnung, f.Stufe);
            Assert.Contains("Saal (+60 %)", f.Werte[1], StringComparison.Ordinal);
            Assert.Equal(new[] { "1", "0", "0", "1", "0" }, m2.Single(m => m.Schluessel == GebaeudeRaumgrundrisse.PROT_HERLEITUNG).Werte);
            Assert.Empty(GebaeudeRaumgrundrisse.Meldungen(Array.Empty<Raumgrundriss>()));
        }

        private static string T(string schluessel) => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, CultureInfo.GetCultureInfo("de-DE"));
    }
}
