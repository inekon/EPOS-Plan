using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7e — IFC-Export S3, schematische Körper</b> (Datenaustauschkonzept 6.7, 8.4, 14.3 bis 14.5): Probe 27
    /// (jeder <c>IfcExtrudedAreaSolid</c> gegen das Zonengeometrie-Modell), die Placement-Kette, der Validator trotz
    /// Placement-Pflicht, die Kennzeichnung an vier Stellen, ohne Umriss keine Geometrie, bei Widerspruch S1 mit
    /// Vermerk, Probe 12 auf der S3-Datei, der Rundlauf über den <see cref="IfcLeser"/> (gleich wie ohne Körper), die
    /// Anreicherung ohne Geometrie und der Vergleich mit gbXML Stufe 2 derselben Eingabe. Ohne Datenbank.
    /// </summary>
    public sealed class IfcKoerperTests : IDisposable
    {
        private const double Genau = 1e-6;
        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly ITestOutputHelper _aus;

        public IfcKoerperTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string T(string schluessel) => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, CultureInfo.GetCultureInfo("de-DE"));

        /// <summary>Das Zweizonenhaus aus G7b (zwei schematische Räume, ein Fenster) — dieselbe Eingabe wie gbXML Stufe 2.</summary>
        private static GebaeudeAbbild Haus(bool widerspruch = false, bool ohneFlaeche = false)
            => GbxmlStufe2Tests.Zweizonen(ergebnisse: false, widerspruch: widerspruch, ohneFlaeche: ohneFlaeche);

        private static byte[] Schreiben(GebaeudeAbbild abbild, out GebaeudeExportBilanz bilanz, DateTime? zeit = null, Action<IModel> eingriff = null)
            => IfcExportProbe.Schreiben(abbild, IfcExportProbe.Profil(zeit), null, eingriff, out bilanz);

        // ==================================================================
        //  Auswertung eines Körpers in Weltkoordinaten
        // ==================================================================

        private static double[] P(IIfcCartesianPoint p)
        {
            var k = p.Coordinates.Select(c => (double)c).ToList();
            return new[] { k[0], k[1], k.Count > 2 ? k[2] : 0.0 };
        }

        private static double[] D(IIfcDirection d)
        {
            var k = d.DirectionRatios.Select(c => (double)c).ToList();
            double[] r = { k[0], k[1], k.Count > 2 ? k[2] : 0.0 };
            double l = Math.Sqrt(r[0] * r[0] + r[1] * r[1] + r[2] * r[2]);
            return new[] { r[0] / l, r[1] / l, r[2] / l };
        }

        private static double[] Kreuz(double[] a, double[] b)
            => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };

        /// <summary>Die Ecken eines <c>IfcExtrudedAreaSolid</c> in Weltkoordinaten: erst der Fuß, dann der Kopf.</summary>
        internal static List<double[]> Ecken(IIfcExtrudedAreaSolid s)
        {
            List<double[]> eben;
            if (s.SweptArea is IIfcRectangleProfileDef r)
            {
                double[] l = r.Position == null ? new[] { 0.0, 0.0, 0.0 } : P(r.Position.Location);
                double[] x = r.Position?.RefDirection == null ? new[] { 1.0, 0.0, 0.0 } : D(r.Position.RefDirection);
                double[] y = { -x[1], x[0], 0.0 };
                double hx = (double)r.XDim / 2.0, hy = (double)r.YDim / 2.0;
                eben = new[] { (-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy) }
                    .Select(e => new[] { l[0] + e.Item1 * x[0] + e.Item2 * y[0], l[1] + e.Item1 * x[1] + e.Item2 * y[1] }).ToList();
            }
            else
            {
                var poly = (IIfcPolyline)((IIfcArbitraryClosedProfileDef)s.SweptArea).OuterCurve;
                eben = poly.Points.Select(P).Select(p => new[] { p[0], p[1] }).ToList();
                eben.RemoveAt(eben.Count - 1);
            }
            double[] o = P(s.Position.Location);
            double[] a = s.Position.Axis == null ? new[] { 0.0, 0.0, 1.0 } : D(s.Position.Axis);
            double[] rx = s.Position.RefDirection == null ? new[] { 1.0, 0.0, 0.0 } : D(s.Position.RefDirection);
            double dot = rx[0] * a[0] + rx[1] * a[1] + rx[2] * a[2];
            double[] ux = { rx[0] - dot * a[0], rx[1] - dot * a[1], rx[2] - dot * a[2] };
            double lx = Math.Sqrt(ux[0] * ux[0] + ux[1] * ux[1] + ux[2] * ux[2]);
            ux = new[] { ux[0] / lx, ux[1] / lx, ux[2] / lx };
            double[] uy = Kreuz(a, ux);
            double[] ed = D(s.ExtrudedDirection);
            double[] w = Enumerable.Range(0, 3).Select(i => (ed[0] * ux[i] + ed[1] * uy[i] + ed[2] * a[i]) * (double)s.Depth).ToArray();
            var fuss = eben.Select(p => Enumerable.Range(0, 3).Select(i => o[i] + p[0] * ux[i] + p[1] * uy[i]).ToArray()).ToList();
            return fuss.Concat(fuss.Select(p => new[] { p[0] + w[0], p[1] + w[1], p[2] + w[2] })).ToList();
        }

        /// <summary>Gleiche Punktmengen auf <see cref="Genau"/>.</summary>
        private static void GleichePunkte(IEnumerable<double[]> erwartet, IReadOnlyList<double[]> ist, string wo)
        {
            List<double[]> e = erwartet.ToList();
            Assert.True(e.Count == ist.Count, wo + ": " + e.Count + " erwartete, " + ist.Count + " geschriebene Ecken");
            foreach (double[] p in e)
                Assert.True(ist.Any(q => Math.Abs(q[0] - p[0]) <= Genau && Math.Abs(q[1] - p[1]) <= Genau && Math.Abs(q[2] - p[2]) <= Genau),
                            wo + ": Ecke (" + string.Join("; ", p.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + ") fehlt");
        }

        private static IReadOnlyList<double[]> Versetzt(IReadOnlyList<double[]> ring, double[] n, double s)
            => ring.Select(p => new[] { p[0] + n[0] * s, p[1] + n[1] * s, p[2] + n[2] * s }).ToList();

        private static IIfcExtrudedAreaSolid Koerper(IIfcProduct p)
        {
            IIfcShapeRepresentation r = Assert.Single(p.Representation.Representations.OfType<IIfcShapeRepresentation>());
            Assert.Equal(IfcKoerper.BODY, r.RepresentationIdentifier?.ToString());
            Assert.Equal(IfcKoerper.SWEPT_SOLID, r.RepresentationType?.ToString());
            return Assert.IsAssignableFrom<IIfcExtrudedAreaSolid>(Assert.Single(r.Items));
        }

        // ==================================================================
        //  Probe 27
        // ==================================================================

        [Fact]
        public void Probe27_jeder_Koerper_entspricht_dem_Zonengeometrie_Modell()
        {
            GebaeudeAbbild abbild = Haus();
            (Zonengeometrie _, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(abbild);
            Assert.NotNull(k);
            byte[] datei = Schreiben(abbild, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            AbbildGebaeude g = abbild.Gebaeude[0];
            int erwartet = 0;
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                // Räume: das Prisma über dem Rechteck, Boden und Höhe aus dem Modell.
                foreach (AbbildRaum r in g.Raeume)
                {
                    Raumkoerper rk = k.Raum(r.Kennung);
                    IIfcSpace s = m.Instances.OfType<IIfcSpace>().Single(x => x.Name == r.Name);
                    IIfcExtrudedAreaSolid solid = Koerper(s);
                    IReadOnlyList<double[]> boden = rk.Schale[0];
                    IReadOnlyList<double[]> decke = rk.Schale[1];
                    GleichePunkte(boden.Concat(decke), Ecken(solid), "Raum " + r.Name);
                    Assert.Equal(rk.HoeheM, (double)solid.Depth, 9);
                    erwartet++;
                }
                // Flächen und Öffnungen: Platte nach außen, Öffnung vor der Wandplatte.
                foreach (AbbildBauteil b in g.Bauteile)
                {
                    Koerperflaeche f = b.Nachbarn.Select(n => k.Flaeche(n.Kennung, b.Kennung)).FirstOrDefault(x => x != null);
                    IIfcElement e = m.Instances.OfType<IIfcElement>().Single(x => x.Tag == b.Kennung);
                    Assert.NotNull(f);
                    double[] n = IfcKoerper.Normale(f.EckenM);
                    GleichePunkte(f.EckenM.Concat(Versetzt(f.EckenM, n, IfcKoerper.PLATTE_M)), Ecken(Koerper(e)), "Fläche " + b.Name);
                    erwartet++;
                    for (int i = 0; i < b.Oeffnungen.Count; i++)
                    {
                        AbbildBauteil o = b.Oeffnungen[i];
                        IReadOnlyList<double[]> ring = Zonenkoerper.Oeffnung(f, i, b.Oeffnungen.Count, o.BruttoflaecheM2.Value, out _);
                        IIfcElement oe = m.Instances.OfType<IIfcElement>().Single(x => x.Tag == o.Kennung);
                        IReadOnlyList<double[]> vorn = Versetzt(ring, n, IfcKoerper.PLATTE_M);
                        GleichePunkte(vorn.Concat(Versetzt(ring, n, IfcKoerper.PLATTE_M + IfcKoerper.OEFFNUNG_M)), Ecken(Koerper(oe)), "Öffnung " + o.Name);
                        erwartet++;
                    }
                }
                Assert.Equal(erwartet, m.Instances.OfType<IIfcExtrudedAreaSolid>().Count());
                // Das Öffnungselement bleibt geometrielos (kein Ausschnitt).
                Assert.All(m.Instances.OfType<IIfcOpeningElement>(), o => Assert.Null(o.Representation));
                Assert.Empty(m.Instances.OfType<IIfcBooleanResult>());
            }
            _aus.WriteLine("Körper: " + erwartet);
        }

        [Fact]
        public void Zusammengefasste_Bauteile_tragen_keinen_Koerper_aber_eine_Platzierung()
        {
            GebaeudeAbbild abbild = Haus();
            AbbildBauteil west = abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == GebaeudeExportKennung.Bauteil(203));
            west.Kennung = IfcSchreiber.PRAEFIX_ZUSAMMENFASSUNG + "aussenwand";
            byte[] datei = Schreiben(abbild, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Schluessel == GbxmlSchreiber.FLAECHE_OHNE_POLYGON);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcElement e = m.Instances.OfType<IIfcElement>().Single(x => x.Tag == west.Kennung);
                Assert.Null(e.Representation);
                Assert.NotNull(e.ObjectPlacement);
                Assert.DoesNotContain(T(IfcSchreiber.ELEMENT_SCHEMATISCH), e.Description?.ToString() ?? "");
                Assert.Empty(IfcSchreiber.Pruefen(m));
            }
        }

        // ==================================================================
        //  Placement-Kette, Kontext, Validator
        // ==================================================================

        [Fact]
        public void Placement_Kette_vollstaendig_und_Kontext_mit_TrueNorth_auf_der_Vorgabe()
        {
            byte[] datei = Schreiben(Haus(), out _);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                IIfcSite site = m.Instances.OfType<IIfcSite>().Single();
                IIfcBuilding gebaeude = m.Instances.OfType<IIfcBuilding>().Single();
                var siteLage = Assert.IsAssignableFrom<IIfcLocalPlacement>(site.ObjectPlacement);
                var gebaeudeLage = Assert.IsAssignableFrom<IIfcLocalPlacement>(gebaeude.ObjectPlacement);
                Assert.Null(siteLage.PlacementRelTo);
                Assert.Same(siteLage, gebaeudeLage.PlacementRelTo);
                foreach (IIfcProduct p in m.Instances.OfType<IIfcProduct>())
                {
                    var lage = Assert.IsAssignableFrom<IIfcLocalPlacement>(p.ObjectPlacement);
                    if (!(p is IIfcSite) && !(p is IIfcBuilding)) Assert.Same(gebaeudeLage, lage.PlacementRelTo);
                    // Im Ursprung ohne Drehung: die Lage trägt allein die Position des Körpers.
                    var a = Assert.IsAssignableFrom<IIfcAxis2Placement3D>(lage.RelativePlacement);
                    Assert.Equal(new[] { 0.0, 0.0, 0.0 }, P(a.Location));
                    Assert.Null(a.Axis);
                    Assert.Null(a.RefDirection);
                }
                IIfcGeometricRepresentationContext kontext = m.Instances.OfType<IIfcProject>().Single().RepresentationContexts
                    .OfType<IIfcGeometricRepresentationContext>().Single();
                Assert.Equal(3, (int)kontext.CoordinateSpaceDimension);
                Assert.Equal(new[] { 0.0, 1.0 }, kontext.TrueNorth.DirectionRatios.Select(c => (double)c).ToArray());
                Assert.All(m.Instances.OfType<IIfcShapeRepresentation>(),
                           r => Assert.Same(kontext, ((IIfcGeometricRepresentationSubContext)r.ContextOfItems).ParentContext));
                IIfcSIUnit laenge = m.Instances.OfType<IIfcProject>().Single().UnitsInContext.Units.OfType<IIfcSIUnit>()
                    .Single(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
                Assert.Equal(IfcSIUnitName.METRE, laenge.Name);
                Assert.Null(laenge.Prefix);
            }
        }

        [Fact]
        public void Validator_gruen_trotz_Placement_Pflicht_und_ein_fehlendes_Placement_wird_gemeldet()
        {
            byte[] datei = Schreiben(Haus(), out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
                Assert.Empty(IfcSchreiber.Pruefen(m));

            // Gegenprobe: ein Raum mit Körper ohne Placement verletzt PlacementForShapeRepresentation.
            byte[] kaputt = Schreiben(Haus(), out GebaeudeExportBilanz b2, eingriff: m =>
            {
                using (ITransaction t = m.BeginTransaction("Probe"))
                {
                    ((Xbim.Ifc4.ProductExtension.IfcSpace)m.Instances.OfType<IIfcSpace>().First()).ObjectPlacement = null;
                    t.Commit();
                }
            });
            Assert.Empty(kaputt);
            Assert.Contains(b2.Meldungen, x => x.Schluessel == IfcSchreiber.SCHEMA && x.Werte.Any(w => w.Contains("IfcSpace", StringComparison.OrdinalIgnoreCase)));
            Assert.Contains(b2.Meldungen, x => x.Schluessel == IfcSchreiber.ABGEBROCHEN);
        }

        // ==================================================================
        //  Kennzeichnung (8.4)
        // ==================================================================

        [Fact]
        public void Kennzeichnung_an_vier_Stellen()
        {
            GebaeudeAbbild abbild = Haus();
            byte[] datei = Schreiben(abbild, out _);
            string vermerk = T(IfcSchreiber.ELEMENT_SCHEMATISCH);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                // 1. Projektname.
                IIfcProject projekt = m.Instances.OfType<IIfcProject>().Single();
                Assert.Equal(string.Format(CultureInfo.GetCultureInfo("de-DE"), T(IfcSchreiber.PROJEKT_SCHEMATISCH), abbild.Gebaeude[0].Anzeigename.Trim()),
                             projekt.Name.ToString());
                Assert.EndsWith("(schematisch)", projekt.Name.ToString());
                Assert.Equal(T(IfcSchreiber.DATEI_STUFE_S3), projekt.Description.ToString());
                // 2. FILE_DESCRIPTION.
                Assert.Equal(new[] { T(IfcSchreiber.DATEI_STUFE_S3) }, m.Header.FileDescription.Description.ToArray());
                // 3. Description jedes Produkts mit Körper.
                List<IIfcProduct> mitKoerper = m.Instances.OfType<IIfcProduct>().Where(p => p.Representation != null).ToList();
                Assert.Equal(m.Instances.OfType<IIfcExtrudedAreaSolid>().Count(), mitKoerper.Count);
                Assert.All(mitKoerper, p => Assert.EndsWith(vermerk, p.Description.ToString()));
                Assert.Contains(mitKoerper, p => p is IIfcSpace);
                // 4. IfcAnnotation am Gebäude.
                IIfcAnnotation a = m.Instances.OfType<IIfcAnnotation>().Single();
                Assert.Equal(T(IfcSchreiber.KENNZEICHNUNG_SCHEMATISCH), a.Description.ToString());
                Assert.Same(m.Instances.OfType<IIfcBuilding>().Single(), a.ContainedInStructure.Single().RelatingStructure);
            }
            // Englisch: dieselben Stellen in der Sprache des Profils.
            byte[] en = IfcExportProbe.Schreiben(abbild, IfcExportProbe.Profil(sprache: "en-US"));
            using (MemoryModel m = IfcExportProbe.Modell(en))
            {
                Assert.EndsWith("(schematic)", m.Instances.OfType<IIfcProject>().Single().Name.ToString());
                Assert.StartsWith("IFC export from EPOS-Plan, stage S3", m.Header.FileDescription.Description.Single());
            }
            // Plan: dieselbe Meldung wie gbXML Stufe 2 (zu bestätigen im Dialog), mit der Zahl der Räume.
            IReadOnlyList<PruefMeldung> vorschau = new IfcSchreiber().Vorschau(abbild, IfcExportProbe.Profil());
            Assert.Contains(vorschau, x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_SCHEMATISCH && x.Werte[0] == "2");
            Assert.DoesNotContain(vorschau, x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_OHNE);
        }

        [Fact]
        public void Ohne_Umriss_keine_Geometrie_und_kein_Vermerk()
        {
            GebaeudeAbbild abbild = Haus(ohneFlaeche: true);
            byte[] datei = Schreiben(abbild, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            string text = Encoding.UTF8.GetString(datei);
            foreach (string typ in new[] { "IFCEXTRUDEDAREASOLID(", "IFCSHAPEREPRESENTATION(", "IFCLOCALPLACEMENT(", "IFCGEOMETRICREPRESENTATIONCONTEXT(", "IFCANNOTATION(" })
                Assert.DoesNotContain(typ, text);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                Assert.Equal(new[] { T(IfcSchreiber.DATEI_STUFE_S1) }, m.Header.FileDescription.Description.ToArray());
                IIfcProject p = m.Instances.OfType<IIfcProject>().Single();
                Assert.DoesNotContain("schematisch", p.Name.ToString());
                Assert.Equal(T(IfcSchreiber.DATEI_STUFE_S1), p.Description.ToString());
                Assert.All(m.Instances.OfType<IIfcProduct>(), x => Assert.Null(x.ObjectPlacement));
            }
            IReadOnlyList<PruefMeldung> vorschau = new IfcSchreiber().Vorschau(abbild, IfcExportProbe.Profil());
            Assert.Contains(vorschau, x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_OHNE);
            Assert.DoesNotContain(vorschau, x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_SCHEMATISCH || x.Schluessel == GbxmlSchreiber.GEOMETRIE_ABGELEHNT);
        }

        [Fact]
        public void Bei_Widerspruch_S1_mit_Vermerk()
        {
            GebaeudeAbbild abbild = Haus(widerspruch: true);
            (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(abbild);
            Assert.True(z.AnordnungAbgelehnt);
            Assert.Null(k);
            byte[] datei = Schreiben(abbild, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            using (MemoryModel m = IfcExportProbe.Modell(datei))
            {
                Assert.Empty(m.Instances.OfType<IIfcExtrudedAreaSolid>());
                Assert.Empty(m.Instances.OfType<IIfcAnnotation>());
                Assert.Equal(new[] { T(IfcSchreiber.DATEI_STUFE_S1), T(IfcSchreiber.DATEI_ABGELEHNT) }, m.Header.FileDescription.Description.ToArray());
                string beschreibung = m.Instances.OfType<IIfcProject>().Single().Description.ToString();
                Assert.StartsWith(T(IfcSchreiber.DATEI_STUFE_S1), beschreibung);
                Assert.EndsWith(string.Format(CultureInfo.GetCultureInfo("de-DE"), T(IfcSchreiber.GEOMETRIE_ABGELEHNT_TEXT), GbxmlSchreiber.Widersprueche(z)), beschreibung);
                Assert.DoesNotContain("schematisch", m.Instances.OfType<IIfcProject>().Single().Name.ToString());
            }
            PruefMeldung w = Assert.Single(new IfcSchreiber().Vorschau(abbild, IfcExportProbe.Profil()),
                                           x => x.Schluessel == GbxmlSchreiber.GEOMETRIE_ABGELEHNT);
            Assert.Equal(PruefStufe.Warnung, w.Stufe);
        }

        // ==================================================================
        //  Probe 12, Rundlauf, Anreicherung
        // ==================================================================

        [Fact]
        public void Probe12_S3_zweimal_byte_gleich_ausser_dem_Zeitstempel()
        {
            byte[] a = Schreiben(Haus(), out _);
            byte[] b = Schreiben(Haus(), out _);
            Assert.Equal(a, b);
            byte[] c = Schreiben(Haus(), out _, new DateTime(2027, 1, 2, 3, 4, 5));
            string[] za = Encoding.UTF8.GetString(a).Split("\r\n");
            string[] zc = Encoding.UTF8.GetString(c).Split("\r\n");
            Assert.Equal(za.Length, zc.Length);
            List<int> anders = Enumerable.Range(0, za.Length).Where(i => za[i] != zc[i]).ToList();
            Assert.Equal(2, anders.Count);
            Assert.StartsWith("FILE_NAME (", za[anders[0]]);
            Assert.Contains("IFCOWNERHISTORY(", za[anders[1]]);
        }

        [Fact]
        public void Rundlauf_ueber_den_IfcLeser_gleich_wie_ohne_Koerper()
        {
            GebaeudeAbbild quelle = Haus();
            byte[] mit = Schreiben(quelle, out _);
            // Dieselbe Datei ohne Körper und Platzierungen: der Stand der Stufe S1.
            byte[] ohne = Schreiben(Haus(), out GebaeudeExportBilanz b, eingriff: m =>
            {
                using (ITransaction t = m.BeginTransaction("ohne Körper"))
                {
                    foreach (var p in m.Instances.OfType<Xbim.Ifc4.Kernel.IfcProduct>().ToList())
                    {
                        p.Representation = null;
                        p.ObjectPlacement = null;
                    }
                    m.Instances.OfType<Xbim.Ifc4.Kernel.IfcProject>().Single().RepresentationContexts.Clear();
                    t.Commit();
                }
            });
            Assert.DoesNotContain(b.Meldungen, x => x.Stufe == PruefStufe.Fehler);
            GebaeudeAbbild zm = IfcExportProbe.Lesen(mit), zo = IfcExportProbe.Lesen(ohne);
            Assert.DoesNotContain(zm.Meldungen, x => x.Stufe == PruefStufe.Fehler);
            // Einziger Unterschied der Meldungen: Der Kontext der Stufe S3 nennt den Norden (TrueNorth auf der Vorgabe).
            Assert.Contains(zo.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_KEIN_NORDEN");
            Assert.DoesNotContain(zm.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_KEIN_NORDEN");
            // Mit Körper kommt die Orientierung, die die Datei bei Mengensatz nicht nennt, aus dem Körper (N4): Statt „Seite
            // unbestimmt“ steht die Info der ergänzten Orientierung.
            Assert.Contains(zo.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_SEITE_UNBESTIMMT");
            Assert.DoesNotContain(zm.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_SEITE_UNBESTIMMT");
            Assert.Contains(zm.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT");
            string[] orientierung = { "IMP_IFC_PROT_KEIN_NORDEN", "IMP_IFC_PROT_SEITE_UNBESTIMMT", "IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT" };
            Assert.Equal(zo.Meldungen.Where(x => !orientierung.Contains(x.Schluessel)).Select(x => x.Schluessel + "|" + string.Join("|", x.Werte)),
                         zm.Meldungen.Where(x => !orientierung.Contains(x.Schluessel)).Select(x => x.Schluessel + "|" + string.Join("|", x.Werte)));
            AbbildGebaeude gm = Assert.Single(zm.Gebaeude), go = Assert.Single(zo.Gebaeude);
            Assert.Equal(go.Raeume.Count, gm.Raeume.Count);
            Assert.Equal(quelle.Gebaeude[0].Raeume.Count, gm.Raeume.Count);
            foreach (AbbildRaum ro in go.Raeume)
            {
                AbbildRaum rm = gm.Raeume.Single(r => r.Name == ro.Name);
                Assert.Equal((ro.FlaecheM2, ro.VolumenM3, ro.HoeheM, ro.Beheizt, ro.SollHeizenC, ro.ZonenKennung),
                             (rm.FlaecheM2, rm.VolumenM3, rm.HoeheM, rm.Beheizt, rm.SollHeizenC, rm.ZonenKennung));
            }
            Assert.Equal(go.Bauteile.Count, gm.Bauteile.Count);
            foreach (AbbildBauteil bo in go.Bauteile)
            {
                AbbildBauteil bm = gm.Bauteile.Single(x => x.Name == bo.Name);
                Assert.Equal((bo.Art, bo.Randbedingung, bo.BruttoflaecheM2, bo.UWertWm2K, bo.Oeffnungen.Count),
                             (bm.Art, bm.Randbedingung, bm.BruttoflaecheM2, bm.UWertWm2K, bm.Oeffnungen.Count));
                // Was die Datei ohne Körper an Orientierung trägt, bleibt; der Körper ergänzt nur, was fehlt.
                if (bo.AzimutGrad.HasValue) Assert.Equal(bo.AzimutGrad, bm.AzimutGrad);
                if (bo.Art != Bauteilart.Dach) Assert.Equal(bo.NeigungGrad, bm.NeigungGrad);
                Assert.Equal(bo.Nachbarn.Select(n => n.Kennung), bm.Nachbarn.Select(n => n.Kennung));
            }
            // Und gegen die Quelle: Flächen, Räume und Öffnungen wie geschrieben.
            foreach (AbbildBauteil q in quelle.Gebaeude[0].Bauteile)
            {
                AbbildBauteil x = gm.Bauteile.Single(y => y.Name == q.Name);
                Assert.Equal(q.BruttoflaecheM2.Value, x.BruttoflaecheM2.Value, 9);
                Assert.Equal(q.Oeffnungen.Count, x.Oeffnungen.Count);
                // Die Orientierung aus dem Körper trifft die der Quelle.
                if (q.AzimutGrad.HasValue && x.AzimutGrad.HasValue) Assert.Equal(q.AzimutGrad.Value, x.AzimutGrad.Value, 6);
                if (q.NeigungGrad.HasValue && x.NeigungGrad.HasValue) Assert.Equal(q.NeigungGrad.Value, x.NeigungGrad.Value, 6);
            }
        }

        [Fact]
        public void Anreicherung_schreibt_nie_Geometrie()
        {
            string[] geometrie =
            {
                "IFCEXTRUDEDAREASOLID(", "IFCRECTANGLEPROFILEDEF(", "IFCARBITRARYCLOSEDPROFILEDEF(", "IFCPOLYLINE(", "IFCSHAPEREPRESENTATION(",
                "IFCPRODUCTDEFINITIONSHAPE(", "IFCLOCALPLACEMENT(", "IFCAXIS2PLACEMENT3D(", "IFCGEOMETRICREPRESENTATIONCONTEXT(",
                "IFCGEOMETRICREPRESENTATIONSUBCONTEXT(", "IFCANNOTATION(",
            };
            foreach (bool mitGeometrie in new[] { false, true })
            {
                byte[] datei = IfcAnreicherungTests.Fremdhaus(geometrie: mitGeometrie);
                GbxmlAbbild abbild = IfcAnreicherungTests.Abbild();
                byte[] aus;
                using (var ziel = new MemoryStream())
                {
                    var a = new IfcAnreicherung(IfcErgebnisse.AusAbbild(abbild));
                    GebaeudeAnreicherungBilanz bilanz = a.Anreichern(datei, IfcAnreicherungTests.Quelle(datei), IfcAnreicherungTests.Zuordnungen(),
                                                                     abbild, ziel, IfcExportProbe.Profil(), CancellationToken.None);
                    Assert.True(bilanz.Geschrieben);
                    aus = ziel.ToArray();
                }
                string vorher = Encoding.UTF8.GetString(datei), nachher = Encoding.UTF8.GetString(aus);
                foreach (string typ in geometrie)
                    Assert.True(Zaehlen(vorher, typ) == Zaehlen(nachher, typ), typ + " (Geometrie " + mitGeometrie + ")");
                Assert.DoesNotContain(T(IfcSchreiber.ELEMENT_SCHEMATISCH), nachher);
            }
        }

        private static int Zaehlen(string text, string typ)
        {
            int n = 0, i = 0;
            while ((i = text.IndexOf("=" + typ, i, StringComparison.Ordinal)) >= 0) { n++; i++; }
            return n;
        }

        // ==================================================================
        //  Ansicht und Datei zeigen dasselbe: gbXML Stufe 2 und IFC S3
        // ==================================================================

        [Fact]
        public void Grundflaeche_und_Hoehe_je_Raum_gleich_wie_gbXML_Stufe2()
        {
            GebaeudeAbbild abbild = Haus();
            XDocument gbxml = XDocument.Load(new MemoryStream(GbxmlExportProbe.Schreiben(abbild)));
            byte[] ifc = Schreiben(Haus(), out _);
            using (MemoryModel m = IfcExportProbe.Modell(ifc))
            {
                int verglichen = 0;
                foreach (XElement space in gbxml.Descendants(NS + "Space"))
                {
                    List<List<double[]>> ringe = space.Element(NS + "ShellGeometry").Element(NS + "ClosedShell").Elements(NS + "PolyLoop")
                        .Select(l => l.Elements(NS + "CartesianPoint").Select(cp => cp.Elements(NS + "Coordinate")
                            .Select(c => double.Parse(c.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray()).ToList()).ToList();
                    double zMin = ringe.SelectMany(r => r).Min(p => p[2]), zMax = ringe.SelectMany(r => r).Max(p => p[2]);
                    List<double[]> boden = ringe.First(r => r.All(p => Math.Abs(p[2] - zMin) < Genau));
                    double flaecheX = Math.Abs(Flaeche(boden));

                    string name = space.Element(NS + "Name").Value;
                    IIfcSpace s = m.Instances.OfType<IIfcSpace>().Single(x => x.Name == name);
                    List<double[]> ecken = Ecken(Koerper(s));
                    double flaecheI = Math.Abs(Flaeche(ecken.Take(ecken.Count / 2).ToList()));
                    double hoeheI = ecken.Max(p => p[2]) - ecken.Min(p => p[2]);
                    Assert.Equal(flaecheX, flaecheI, 6);
                    Assert.Equal(zMax - zMin, hoeheI, 6);
                    Assert.Equal(zMin, ecken.Min(p => p[2]), 6);
                    verglichen++;
                }
                Assert.Equal(2, verglichen);
            }
        }

        private static double Flaeche(List<double[]> ring)
        {
            double s = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                s += a[0] * b[1] - b[0] * a[1];
            }
            return s / 2.0;
        }

        [Fact]
        public void Rechteckpruefung_und_Normale()
        {
            Assert.True(IfcKoerper.Rechteck(new[] { new[] { 0.0, 0.0 }, new[] { 4.0, 0.0 }, new[] { 4.0, 2.5 }, new[] { 0.0, 2.5 } },
                                            out double w, out double h, out double[] mitte));
            Assert.Equal((4.0, 2.5, 2.0, 1.25), (w, h, mitte[0], mitte[1]));
            Assert.False(IfcKoerper.Rechteck(new[] { new[] { 0.0, 0.0 }, new[] { 4.0, 0.0 }, new[] { 3.0, 2.5 }, new[] { 0.0, 2.5 } }, out _, out _, out _));
            // Südwand, Umlauf von West nach Ost unten: Normale nach Süden (−y).
            double[] n = IfcKoerper.Normale(new[] { new[] { 0.0, 0.0, 0.0 }, new[] { 10.0, 0.0, 0.0 }, new[] { 10.0, 0.0, 2.5 }, new[] { 0.0, 0.0, 2.5 } });
            Assert.Equal(new[] { 0.0, -1.0, 0.0 }, n.Select(x => Math.Round(x, 12) + 0.0).ToArray());
        }
    }
}
