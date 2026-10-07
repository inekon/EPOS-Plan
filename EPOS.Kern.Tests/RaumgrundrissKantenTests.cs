using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using WindowsFormsApplication1;
using Xbim.Ifc4.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>HC-5c — die Bauteilplatten an den Kanten der Grundriss-Prismen</b> (Konzept HottCAD-Verbund 11.4 „Körper“): Wände nach
    /// wahrem Azimut auf die Kanten (ungedreht, gedreht, L-förmig, mehrere Räume je Zone), Fenster auf ihrer Wand, Boden und Decke
    /// auf den Prismen, der Vermerk ohne Nordwinkel, IFC- und gbXML-Rundlauf, Rechteckzonen bytegleich. Ohne Datenbank.
    /// </summary>
    public sealed class RaumgrundrissKantenTests : IDisposable
    {
        private static readonly XNamespace NS = GbxmlLeser.NAMENSRAUM;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>Ein Grundriss aus Punkten [m] (Außenring gegen den Uhrzeigersinn), um <paramref name="dreh"/> ° im Uhrzeigersinn gedreht.</summary>
        internal static Raumgrundriss Gr(string kennung, double[][] punkte, double dreh = 0.0, double? drehung = null, bool unbekannt = false,
                                         double hoehe = 2.5)
        {
            double r = dreh * Math.PI / 180.0;
            string ring = string.Join(";", punkte.Select(p =>
            {
                double x = p[0] * Math.Cos(r) + p[1] * Math.Sin(r), y = -p[0] * Math.Sin(r) + p[1] * Math.Cos(r);
                return Math.Round(x * 1000.0).ToString(CultureInfo.InvariantCulture) + "," + Math.Round(y * 1000.0).ToString(CultureInfo.InvariantCulture);
            }));
            IReadOnlyList<Grundrissring> ringe = Raumgrundriss.RingeLesen(ring);
            return new Raumgrundriss
            {
                Quellkennung = kennung, BodenM = 0.0, HoeheM = hoehe, Ringe = ringe, RingflaecheM2 = ringe.Sum(x => x.FlaecheM2),
                Herleitung = Umrissherleitung.KoerperBoden, DrehungGrad = drehung, NordwinkelUnbekannt = unbekannt,
            };
        }

        private static double[][] Rechteck(double x0, double y0, double x1, double y1)
            => new[] { new[] { x0, y0 }, new[] { x1, y0 }, new[] { x1, y1 }, new[] { x0, y1 } };

        /// <summary>L-förmig: 10 × 3 m unten, 4 × 2 m darüber links (38 m²).</summary>
        private static readonly double[][] L = { new[] { 0.0, 0 }, new[] { 10.0, 0 }, new[] { 10.0, 3 }, new[] { 4.0, 3 }, new[] { 4.0, 5 }, new[] { 0.0, 5 } };

        /// <summary>Das Zweizonenhaus (Büro 10 × 5 m, Lager), Zone Büro mit den Grundrissen.</summary>
        private static GebaeudeAbbild Haus(params Raumgrundriss[] gr)
        {
            GebaeudeAbbild a = GbxmlStufe2Tests.Zweizonen();
            a.Gebaeude[0].Raeume[0].Grundrisse = gr;
            return a;
        }

        private static Raumumriss Buero(GebaeudeAbbild a) => GebaeudeGrundriss.Bilden(a, 0).Raeume.Single(r => r.Name == "Büro");

        private static string B(int nr) => GebaeudeExportKennung.Bauteil(nr);

        private static double Summe(Raumumriss r, int nr) => r.Platten.Where(p => p.Verweis.BauteilKennung == B(nr)).Sum(p => p.FlaecheM2);

        /// <summary>Der Modellazimut der Außennormalen einer Wandplatte [°].</summary>
        private static double Normale(Grundrissplatte p)
        {
            double dx = p.EckenM[1][0] - p.EckenM[0][0], dy = p.EckenM[1][1] - p.EckenM[0][1];
            return Zonengeometrie.ModellAzimut(dy, -dx);
        }

        // ==================================================================
        //  Kantenzuordnung
        // ==================================================================

        [Fact]
        public void Ungedreht_steht_jede_Wand_auf_ihrer_Kante_mit_ihrer_Flaeche()
        {
            Raumumriss r = Buero(Haus(Gr("a", Rechteck(0, 0, 10, 5), drehung: 0.0)));
            Assert.True(r.AusGrundriss);
            // Süd 25, Nord 25, West 12,5, Trennwand Ost 12,5 — je eine Platte der Raumhöhe; Boden und Dach je 50.
            foreach ((int nr, double flaeche, double azimut) in new[] { (201, 25.0, 180.0), (202, 25.0, 0.0), (203, 12.5, 270.0), (120, 12.5, 90.0) })
            {
                Grundrissplatte p = Assert.Single(r.Platten, x => x.Verweis.BauteilKennung == B(nr));
                Assert.Equal(flaeche, p.FlaecheM2, 9);
                Assert.Equal(azimut, Normale(p), 6);
                Assert.Equal(2.5, p.EckenM[2][2] - p.EckenM[1][2], 9);
            }
            Assert.Equal(50.0, Summe(r, 204), 6);
            Assert.Equal(50.0, Summe(r, 205), 6);
            Assert.All(r.Platten.Where(p => p.Stellung == Grenzstellung.Boden).SelectMany(p => p.EckenM), q => Assert.Equal(0.0, q[2]));
            Assert.All(r.Platten.Where(p => p.Stellung == Grenzstellung.Decke).SelectMany(p => p.EckenM), q => Assert.Equal(2.5, q[2]));
            Assert.DoesNotContain(r.OhneKante, v => v.Stellung == Grenzstellung.Wand);
            // Die Kanten tragen ihre Wand (Ansicht).
            Assert.Equal(4, r.Polygone[0].Kanten.Count(k => k.Grenzen.Count == 1));
        }

        [Theory]
        [InlineData(30.0)]
        [InlineData(60.0)]
        [InlineData(-75.0)]
        public void Gedreht_dreht_der_Nordwinkel_die_Kanten_und_die_Waende_folgen(double dreh)
        {
            // Das Modell ist um dreh gedreht; die Quelle nennt den Nordwinkel: wahrer Azimut = Modellazimut − Nordwinkel.
            // Die Prismenhöhe 2,6 m lässt Platz für die Rundung der gedrehten Ecken auf ganze Millimeter (Plattenhöhe ≤ Prismenhöhe).
            Raumumriss r = Buero(Haus(Gr("a", Rechteck(0, 0, 10, 5), dreh, drehung: dreh, hoehe: 2.6)));
            foreach ((int nr, double azimut) in new[] { (201, 180.0), (202, 0.0), (203, 270.0), (120, 90.0) })
            {
                Grundrissplatte p = Assert.Single(r.Platten, x => x.Verweis.BauteilKennung == B(nr));
                Assert.True(Zonengeometrie.Winkelabstand(Normale(p), azimut + dreh) < 0.01, nr + ": " + Normale(p));
            }
            Assert.Equal(25.0, Summe(r, 201), 6);
            Assert.False(r.NordwinkelAngenommen);

            // Ohne Nordwinkel (Modell-Nord = Nord) landet die Südwand bei 60° auf einer anderen Kante.
            if (Math.Abs(dreh) >= 45.0)
            {
                Raumumriss ohne = Buero(Haus(Gr("a", Rechteck(0, 0, 10, 5), dreh, drehung: 0.0, unbekannt: true)));
                Grundrissplatte sued = ohne.Platten.First(x => x.Verweis.BauteilKennung == B(201));
                Assert.False(Zonengeometrie.Winkelabstand(Normale(sued), 180.0 + dreh) < 0.01);
                Assert.True(ohne.NordwinkelAngenommen);
            }
        }

        [Fact]
        public void L_foermig_verteilt_sich_eine_Wand_nach_Kantenlaenge_auf_die_Kanten_ihrer_Richtung()
        {
            Raumumriss r = Buero(Haus(Gr("a", L, drehung: 0.0)));
            // Nord 25 m² auf den Kanten 6 m und 4 m: Höhe 2,5 m, Platten 15 und 10 m², die größte zuerst.
            List<Grundrissplatte> nord = r.Platten.Where(x => x.Verweis.BauteilKennung == B(202)).ToList();
            Assert.Equal(new[] { 15.0, 10.0 }, nord.Select(p => Math.Round(p.FlaecheM2, 9)));
            // Trennwand Ost 12,5 auf 3 m und 2 m.
            Assert.Equal(new[] { 7.5, 5.0 }, r.Platten.Where(x => x.Verweis.BauteilKennung == B(120)).Select(p => Math.Round(p.FlaecheM2, 9)));
            // Je Richtung die Fläche des Mengensatzes.
            foreach ((int nr, double a) in new[] { (201, 25.0), (202, 25.0), (203, 12.5), (120, 12.5) })
                Assert.Equal(a, Summe(r, nr), 6);
            // Boden 50 m² auf einem Prisma von 38 m²: der Streifen füllt das Prisma.
            Assert.Equal(38.0, Summe(r, 204), 6);
        }

        [Fact]
        public void Mehrere_Raeume_je_Zone_innen_keine_Platte_Boden_auf_beiden_Prismen()
        {
            Raumumriss r = Buero(Haus(Gr("a", Rechteck(0, 0, 5, 5), drehung: 0.0), Gr("b", Rechteck(5, 0, 10, 5), drehung: 0.0)));
            Assert.Equal(2, r.Polygone.Count);
            // Die gemeinsame Kante x = 5 ist innen: keine Platte dort.
            Assert.DoesNotContain(r.Platten, p => p.Stellung == Grenzstellung.Wand && p.EckenM.All(q => Math.Abs(q[0] - 5.0) < 1e-9));
            Assert.Equal(new[] { 12.5, 12.5 }, r.Platten.Where(x => x.Verweis.BauteilKennung == B(201)).Select(p => Math.Round(p.FlaecheM2, 9)));
            Grundrissplatte ost = Assert.Single(r.Platten, x => x.Verweis.BauteilKennung == B(120));
            Assert.All(ost.EckenM, q => Assert.Equal(10.0, q[0], 9));
            List<Grundrissplatte> boden = r.Platten.Where(x => x.Verweis.BauteilKennung == B(204)).ToList();
            Assert.Equal(2, boden.Count);
            Assert.Equal(50.0, boden.Sum(p => p.FlaecheM2), 6);
        }

        [Fact]
        public void Das_Fenster_steht_in_seiner_Wand_und_ohne_Nordwinkel_steht_der_Vermerk()
        {
            GebaeudeAbbild haus = Haus(Gr("a", Rechteck(0, 0, 10, 5), drehung: 0.0, unbekannt: true));
            (Zonengeometrie z, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(haus);
            string raum = haus.Gebaeude[0].Raeume[0].Kennung;
            Koerperflaeche sued = k.Flaeche(raum, B(201));
            Assert.NotNull(sued);
            IReadOnlyList<double[]> fenster = Zonenkoerper.Oeffnung(sued, 0, 1, 3.0, out bool begrenzt);
            Assert.NotNull(fenster);
            Assert.False(begrenzt);
            Assert.All(fenster, q => Assert.Equal(0.0, q[1], 9));

            Assert.Contains(z.Meldungen, m => m.Schluessel == Zonengeometrie.NORDWINKEL_ANGENOMMEN);
            Assert.Contains(GbxmlSchreiber.Geometriemeldungen(haus), m => m.Schluessel == GbxmlSchreiber.NORDWINKEL_ANGENOMMEN);
            Assert.Contains("Modell-Nord angenommen", WindowsFormsApplication1.MyResource.Resource.GEXP_PROT_NORDWINKEL_ANGENOMMEN, StringComparison.Ordinal);
            Assert.Contains("model north assumed", WindowsFormsApplication1.MyResource.Resource.ResourceManager
                .GetString(GbxmlSchreiber.NORDWINKEL_ANGENOMMEN, CultureInfo.GetCultureInfo("en-US")), StringComparison.Ordinal);

            // Mit Nordwinkel kein Vermerk.
            Assert.DoesNotContain(GbxmlSchreiber.Geometriemeldungen(Haus(Gr("a", Rechteck(0, 0, 10, 5), drehung: 0.0))),
                                  m => m.Schluessel == GbxmlSchreiber.NORDWINKEL_ANGENOMMEN);
        }

        // ==================================================================
        //  Rundläufe
        // ==================================================================

        [Theory]
        [InlineData(0.0)]
        [InlineData(30.0)]
        public void IFC_Export_schreibt_die_Platten_je_Kante_mit_der_Flaeche_des_Mengensatzes(double dreh)
        {
            GebaeudeAbbild haus = Haus(Gr("a", L, dreh, drehung: dreh, hoehe: 2.6));
            byte[] ifc = IfcExportProbe.Schreiben(haus);
            var m = IfcExportProbe.Modell(ifc);
            foreach ((int nr, double a, int platten) in new[] { (201, 25.0, 1), (202, 25.0, 2), (203, 12.5, 1), (120, 12.5, 2) })
            {
                IIfcElement e = m.Instances.OfType<IIfcElement>().Single(x => x.Name == "Bauteil " + nr);
                List<IIfcExtrudedAreaSolid> koerper = e.Representation.Representations.SelectMany(x => x.Items).OfType<IIfcExtrudedAreaSolid>().ToList();
                Assert.Equal(platten, koerper.Count);
                double geometrie = koerper.Sum(s => s.SweptArea is IIfcRectangleProfileDef p ? (double)p.XDim * p.YDim : double.NaN);
                Assert.Equal(a, geometrie, 4);                     // Koordinaten auf 1 µm gerundet
                Assert.Equal(a, IfcExportProbe.Mengen(e, "Qto_WallBaseQuantities")["GrossSideArea"], 6);
                Assert.Contains("Platte am Prisma", e.Description?.ToString() ?? "", StringComparison.Ordinal);
            }
            // Das Fenster trägt einen Körper vor seiner Wand.
            IIfcWindow fenster = m.Instances.OfType<IIfcWindow>().Single(x => x.Name == "Fenster Süd");
            Assert.NotNull(fenster.Representation);
            // Die Nordrichtung folgt der Quelle — nur wenn kein Körper schematisch ist (hier steht das Lager als Rechteck daneben).
            IIfcGeometricRepresentationContext kontext = m.Instances.OfType<IIfcGeometricRepresentationContext>().First(x => !(x is IIfcGeometricRepresentationSubContext));
            Assert.Equal(0.0, (double)kontext.TrueNorth.DirectionRatios[0], 9);
        }

        [Fact]
        public void IFC_und_gbXML_schreiben_die_Nordrichtung_der_Quelle_wenn_jeder_Koerper_ein_Prisma_ist()
        {
            GebaeudeAbbild haus = Haus(Gr("a", Rechteck(0, 0, 10, 5), 30.0, drehung: 30.0));
            haus.Gebaeude[0].Raeume[1].Grundrisse = new[] { Gr("b", Rechteck(10, 0, 16, 5), 30.0, drehung: 30.0) };
            var m = IfcExportProbe.Modell(IfcExportProbe.Schreiben(haus));
            IIfcGeometricRepresentationContext kontext = m.Instances.OfType<IIfcGeometricRepresentationContext>().First(x => !(x is IIfcGeometricRepresentationSubContext));
            Assert.Equal(30.0, IfcPlatzierung.DrehungAusTrueNorth((double)kontext.TrueNorth.DirectionRatios[0], (double)kontext.TrueNorth.DirectionRatios[1]), 6);
            XDocument x = XDocument.Load(new MemoryStream(GbxmlExportProbe.Schreiben(haus)));
            Assert.Equal(30.0, double.Parse(x.Descendants(NS + "CADModelAzimuth").Single().Value, CultureInfo.InvariantCulture), 6);
        }

        [Fact]
        public void GbXML_schreibt_die_groesste_Platte_und_die_Flaechen_bleiben()
        {
            GebaeudeAbbild haus = Haus(Gr("a", L, drehung: 0.0));
            byte[] datei = GbxmlExportProbe.Schreiben(haus, null, out GebaeudeExportBilanz bilanz);
            XDocument x = XDocument.Load(new MemoryStream(datei));
            XElement nord = x.Descendants(NS + "Surface").Single(s => (string)s.Attribute("id") == B(202));
            List<double[]> ring = nord.Element(NS + "PlanarGeometry").Element(NS + "PolyLoop").Elements(NS + "CartesianPoint")
                .Select(c => c.Elements(NS + "Coordinate").Select(v => double.Parse(v.Value, CultureInfo.InvariantCulture)).ToArray()).ToList();
            Assert.All(ring, q => Assert.Equal(3.0, q[1], 6));                 // die 6-m-Kante bei y = 3
            Assert.Contains(bilanz.Meldungen, mm => mm.Schluessel == GbxmlSchreiber.FLAECHE_TEILPLATTEN);
            Assert.DoesNotContain(bilanz.Meldungen, mm => mm.Schluessel == GbxmlSchreiber.FLAECHE_OHNE_POLYGON
                                                         && mm.Werte.Any(w => w != null && w.Contains("Bauteil 20", StringComparison.Ordinal)));
            // Der eigene Leser liest dieselben Flächen je Richtung zurück (RectangularGeometry).
            GbxmlAbbild zurueck = GbxmlExportProbe.Lesen(datei);
            foreach ((int nr, double a) in new[] { (201, 25.0), (202, 25.0), (203, 12.5), (120, 12.5) })
                Assert.Equal(a, zurueck.Gebaeude[0].Bauteile.Single(b => b.Kennung == B(nr)).BruttoflaecheM2.Value, 6);
        }

        [Fact]
        public void Rechteckzonen_ohne_Grundriss_bleiben_bytegleich_und_ohne_Platten()
        {
            Assert.Equal(IfcExportProbe.Schreiben(GbxmlStufe2Tests.Zweizonen()), IfcExportProbe.Schreiben(Haus()));
            Assert.Equal(GbxmlExportProbe.Schreiben(GbxmlStufe2Tests.Zweizonen()), GbxmlExportProbe.Schreiben(Haus()));
            Assert.All(GebaeudeGrundriss.Bilden(Haus(), 0).Raeume, r => Assert.Empty(r.Platten));
            Assert.Null(GebaeudeGrundriss.Bilden(Haus(), 0).PrismenDrehungGrad);
        }

        [Fact]
        public void Platten_beruehren_keine_Rechengroesse()
        {
            GebaeudeAbbild haus = Haus(Gr("a", L, 30.0, drehung: 30.0));
            List<(string, double?, double?)> vorher = haus.Gebaeude[0].Bauteile.Select(b => (b.Kennung, b.BruttoflaecheM2, b.AzimutGrad)).ToList();
            GbxmlSchreiber.Raumgeometrie(haus);
            IfcExportProbe.Schreiben(haus);
            Assert.Equal(vorher, haus.Gebaeude[0].Bauteile.Select(b => (b.Kennung, b.BruttoflaecheM2, b.AzimutGrad)).ToList());
        }

        // ==================================================================
        //  Importweg: die Probe mit gedrehtem TrueNorth
        // ==================================================================

        private static GebaeudeImportAblauf Lesen(string datei, string ersetzen = null, string durch = null)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            string text = File.ReadAllText(pfad, Encoding.ASCII);
            if (ersetzen != null)
            {
                Assert.Contains(ersetzen, text, StringComparison.Ordinal);
                text = text.Replace(ersetzen, durch, StringComparison.Ordinal);
            }
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(Encoding.ASCII.GetBytes(text)))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        [Fact]
        public void Der_Leser_liefert_den_Nordwinkel_und_die_Quelle_traegt_ihn()
        {
            GebaeudeImportAblauf haus = Lesen("ifc4_haus.ifc");                 // TrueNorth [−2, 1, 0]
            Assert.Equal(IfcPlatzierung.DrehungAusTrueNorth(-2, 1), haus.Abbild.NordwinkelGrad.Value, 9);
            Assert.Equal(haus.Abbild.NordwinkelGrad, haus.Quelle.NordwinkelGrad);
            Assert.Equal(296.565051177, RaumgrundrissSchema.Normiert(haus.Quelle.NordwinkelGrad).Value, 6);

            GebaeudeImportAblauf gb = new GebaeudeImportAblauf();
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "gbxml_norddrehung.xml");
            using (FileStream s = File.OpenRead(pfad)) gb.Lesen(s, pfad, new GbxmlImportProfil());
            Assert.Equal(60.0, gb.Quelle.NordwinkelGrad.Value, 9);

            Assert.Null(RaumgrundrissSchema.Normiert(double.NaN));
            Assert.Equal(330.0, RaumgrundrissSchema.Normiert(-30.0).Value, 9);
            Assert.Equal(0.0, RaumgrundrissSchema.Normiert(360.0).Value, 9);
        }

        [Fact]
        public void Gedrehte_Probe_dreht_Kanten_und_Waende_gemeinsam()
        {
            const string NORD = "#11=IFCDIRECTION((0.,1.,0.));";
            GebaeudeImportAblauf gerade = Lesen("ifc4_koerper_nachbarn.ifc");
            GebaeudeImportAblauf gedreht = Lesen("ifc4_koerper_nachbarn.ifc", NORD, "#11=IFCDIRECTION((0.5,0.866025403784439,0.));");
            Assert.Equal(0.0, gerade.Quelle.NordwinkelGrad.Value, 9);            // TrueNorth (0, 1)
            Assert.Equal(30.0, gedreht.Quelle.NordwinkelGrad.Value, 6);

            Zonengeometrie a = GebaeudeGrundriss.BildenMitGrundriss(gerade.Abbild, 0, null, out _);
            Zonengeometrie b = GebaeudeGrundriss.BildenMitGrundriss(gedreht.Abbild, 0, null, out _);
            Raumumriss ra = a.Raeume.Single(r => r.Name == "Büro"), rb = b.Raeume.Single(r => r.Name == "Büro");
            Assert.True(ra.AusGrundriss);
            Assert.Equal(Zonengeometrie.Winkelabstand(ra.Polygone[0].Kanten[0].AzimutGrad.Value - 30.0, rb.Polygone[0].Kanten[0].AzimutGrad.Value), 0.0, 6);
            // Dieselbe Wand steht in beiden Fällen auf derselben Kante: Die Drehung trifft Kante und Wand gleich.
            Assert.Equal(ra.Platten.Select(p => p.Verweis.BauteilKennung + "@" + p.Kante), rb.Platten.Select(p => p.Verweis.BauteilKennung + "@" + p.Kante));
            Assert.Contains(ra.Platten, p => p.Stellung == Grenzstellung.Wand);
        }
    }

    /// <summary>
    /// <b>HC-5c — Diagnose an <c>Quellen/*.ifc</c></b>: je Datei der Nordwinkel und der Anteil der Wandfläche der Räume mit
    /// Grundriss-Prisma, der eine Kante findet. Übersprungen mit Namen, wenn eine Datei fehlt.
    /// </summary>
    public sealed class RaumgrundrissKantenQuellenDiagnose : IDisposable
    {
        private readonly ITestOutputHelper _aus;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public RaumgrundrissKantenQuellenDiagnose(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        [Theory]
        [InlineData("MFH-Klein-unsaniert-1964.ifc")]
        [InlineData("MFH_mittel_1984.ifc")]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc")]
        [InlineData("Sportheim_1970_unsaniert.ifc")]
        [InlineData("Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc")]
        [InlineData("WG-EH55_Poroton-GModG-2026.ifc")]
        public void Nordwinkel_und_Anteil_zugeordneter_Wandflaeche(string datei)
        {
            string pfad = IfcQuelldateienDiagnoseTests.Pfad(datei);
            if (pfad == null)
            {
                _aus.WriteLine(datei + ": übersprungen (fehlt)");
                return;
            }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad)) a.Lesen(s, pfad, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            double gesamt = 0, zugeordnet = 0;
            int prismen = 0;
            for (int i = 0; i < a.Abbild.Gebaeude.Count; i++)
            {
                Zonengeometrie z = GebaeudeGrundriss.BildenMitGrundriss(a.Abbild, i, null, out _);
                foreach (Raumumriss r in z.Raeume.Where(x => x.AusGrundriss))
                {
                    prismen++;
                    zugeordnet += r.Platten.Where(p => p.Stellung == Grenzstellung.Wand).Sum(p => p.FlaecheM2);
                }
                Umrisseingang e = GebaeudeGrundriss.Eingang(a.Abbild, i, null, GebaeudeRaumgrundrisse.Bilden(a.Abbild, i));
                gesamt += e.Raeume.Where(u => z.Raum(u.Kennung)?.AusGrundriss == true)
                              .SelectMany(u => u.Seiten).Where(s => s.Verweis?.Stellung == Grenzstellung.Wand).Sum(s => s.FlaecheM2 ?? 0.0);
            }
            string nord = a.Quelle.NordwinkelGrad is double n ? RaumgrundrissSchema.Normiert(n).Value.ToString("0.###", CultureInfo.InvariantCulture) + "°" : "keiner";
            string anteil = gesamt > 0 ? (zugeordnet / gesamt).ToString("P1", CultureInfo.InvariantCulture) : "–";
            _aus.WriteLine(datei + ": Nordwinkel " + nord + ", Räume mit Prisma " + prismen + ", Wandfläche " +
                           gesamt.ToString("0.0", CultureInfo.InvariantCulture) + " m², zugeordnet " + anteil);
            Assert.True(zugeordnet <= gesamt + 1e-6);
        }
    }
    /// <summary>
    /// <b>HC-5c — der Nordwinkel an der Importquelle</b> (Schritt <see cref="RaumgrundrissSchema.SCHRITT"/>): Spalte und CHECK,
    /// Schreiben beim Import und beim Nachtragen, Lesen mit der Drehung der Quelle, Duplizieren.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumgrundrissNordwinkelTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int GEBAEUDE = 10614;              // Projekt 1007
        private const string PROJEKTNAME = "Laurentiuskirche";

        private static GebaeudeQuelle Quelle(string format, double? nord)
        {
            GebaeudeQuelle q = ImportzuordnungSchemaRegelTests.Quelle(format);
            return new GebaeudeQuelle(q.Format, q.Dateiname, q.Hash, q.Groesse, q.Schemastand, q.Zeitpunkt, q.Programmfassung, q.Zonenregel,
                                      q.FehlendeEntitaeten) { NordwinkelGrad = nord };
        }

        private static int Schreiben(string format, double? nord)
        {
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(
                GEBAEUDE, Quelle(format, nord), new List<GebaeudeQuellzuordnung> { new GebaeudeQuellzuordnung("Building", "bldg-1", ImportZiel.Gebaeude) },
                null, new[] { RaumgrundrissKantenTests.Gr("space-a", new[] { new[] { 0.0, 0 }, new[] { 4.0, 0 }, new[] { 4.0, 3 } }) });
            Assert.True(e.Ok, e.Meldung);
            return e.IdImportquelle;
        }

        [Fact]
        public void Die_Spalte_steht_nullbar_mit_CHECK_und_der_Schritt_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(RaumgrundrissSchema.NordwinkelVorhanden());
            Assert.True(RaumgrundrissSchema.Vollstaendig());
            Assert.Equal(0, RaumgrundrissSchema.Ausfuehren(null));
            DataTable t = DataRepository.GetDataTable("SELECT type, \"notnull\" FROM pragma_table_info(?) WHERE name = ?",
                                                      new DbParam("@t", RaumgrundrissSchema.TAB_QUELLE), new DbParam("@s", RaumgrundrissSchema.SPALTE_NORDWINKEL));
            Assert.Equal("REAL|0", t.Rows[0][0] + "|" + t.Rows[0][1]);
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", RaumgrundrissSchema.TAB_QUELLE)), CultureInfo.InvariantCulture);
            Assert.Contains("\"Nordwinkel_Grad\" >= 0 AND \"Nordwinkel_Grad\" < 360", ddl, StringComparison.Ordinal);
            Assert.Contains(RaumgrundrissSchema.SQL_NORDWINKEL.Substring(RaumgrundrissSchema.SQL_NORDWINKEL.IndexOf("CHECK", StringComparison.Ordinal)), ddl,
                            StringComparison.Ordinal);
        }

        [Fact]
        public void Import_schreibt_den_Nordwinkel_und_das_Lesen_traegt_die_Drehung_der_Quelle()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeImportCtrl();
            Schreiben("IFC", -30.0);
            Assert.Equal(330.0, ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelGrad.Value, 9);
            Raumgrundriss g = Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE));
            Assert.Equal(330.0, g.DrehungGrad.Value, 9);
            Assert.False(g.NordwinkelUnbekannt);

            Schreiben("GBXML", 12.0);                     // gbXML: Angabe, die Azimute der Datei sind nicht gedreht
            Assert.Equal(12.0, ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelGrad.Value, 9);
            Assert.Equal(0.0, Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE)).DrehungGrad.Value, 9);

            int ohne = Schreiben("IFC", null);
            Assert.Null(ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelGrad);
            g = Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE));
            Assert.True(g.NordwinkelUnbekannt);
            Assert.Equal(0.0, g.DrehungGrad.Value, 9);

            // F7: Nachtragen schreibt den frisch gelesenen Nordwinkel an die Quelle.
            Assert.False(GebaeudeImportCtrl.NordwinkelGleich(null, 45.0));
            Assert.True(ctrl.SchreibeRaumgrundrisse(ohne, ctrl.LesenRaumgrundrisseDerQuelle(ohne), null, 45.0).Ok);
            Assert.Equal(45.0, ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelGrad.Value, 9);
            Assert.True(GebaeudeImportCtrl.NordwinkelGleich(ctrl.LesenQuellen(GEBAEUDE)[0].NordwinkelGrad, 405.0));
        }

        [Fact]
        public void Duplizieren_nimmt_den_Nordwinkel_mit()
        {
            if (!_db.Vorhanden) return;
            Schreiben("IFC", 30.0);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " HC-5c");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", neu)), CultureInfo.InvariantCulture);
            Assert.Equal(30.0, new GebaeudeImportCtrl().LesenQuellen(gebaeude)[0].NordwinkelGrad.Value, 9);
            Assert.Equal(30.0, Assert.Single(new GebaeudeImportCtrl().LesenRaumgrundrisse(gebaeude)).DrehungGrad.Value, 9);
        }
    }
}
