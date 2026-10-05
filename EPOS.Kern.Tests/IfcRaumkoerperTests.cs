using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7f-1 — Raumkörper aus der IFC-Datei</b> (Datenaustauschkonzept 15.2, 15.3, 15.7): Probe 28 (Rundlauf
    /// über den eigenen Schreiber G7e — jede Raumextrusion kommt als Dateikörper zurück, dessen Punkte die Schale des
    /// <see cref="Zonenkoerper"/> sind; die Ausgabe ist byteweise gleich) und Probe 29 (je selbst erzeugter Kleinstdatei
    /// die erwartete Art, Dreieckszahl und Vermerke, dazu die Placement-Kette mit Drehung und Längen in Millimetern).
    /// Die Proben liegen unter <c>Referenzlaeufe/Importproben/</c> und sind mit
    /// <see cref="IfcProbenErzeuger.Raumkoerperproben"/> byte-gleich neu erzeugbar. Ohne Datenbank.
    /// </summary>
    public sealed class IfcRaumkoerperTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly ITestOutputHelper _aus;

        public IfcRaumkoerperTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static GebaeudeImportAblauf Lesen(string datei)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static string Schluessel(double[] p)
            => string.Join(";", p.Select(x => Math.Round(x, Zonenkoerper.STELLEN).ToString("R", CultureInfo.InvariantCulture)));

        /// <summary>Das Volumen über den Divergenzsatz: positiv, wenn die Normalen nach außen zeigen.</summary>
        private static double Volumen(Dateikoerper k)
            => k.Dreiecke.Sum(d =>
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                return (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;
            });

        /// <summary>Bei einem konvexen Körper zeigt jede Normale vom Schwerpunkt der Punkte weg.</summary>
        private static void NormalenNachAussen(Dateikoerper k, string wo)
        {
            double[] m = Enumerable.Range(0, 3).Select(i => k.PunkteM.Average(p => p[i])).ToArray();
            for (int t = 0; t < k.DreieckZahl; t++)
            {
                int[] d = k.Dreiecke[t];
                double[] s = Enumerable.Range(0, 3).Select(i => (k.PunkteM[d[0]][i] + k.PunkteM[d[1]][i] + k.PunkteM[d[2]][i]) / 3.0 - m[i]).ToArray();
                double[] n = k.Normalen[t];
                Assert.True(n[0] * s[0] + n[1] * s[1] + n[2] * s[2] > 0.0, wo + ": Dreieck " + t + " zeigt nach innen");
                Assert.Equal(1.0, Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]), 5);
            }
        }

        // ==================================================================
        //  Probe 28
        // ==================================================================

        [Fact]
        public void Probe28_Rundlauf_ueber_den_eigenen_Schreiber_ergibt_die_Schale_des_Zonenkoerpers()
        {
            GebaeudeAbbild quelle = GbxmlStufe2Tests.Zweizonen();
            (Zonengeometrie _, Zonenkoerper k) = GbxmlSchreiber.Raumgeometrie(quelle);
            Assert.NotEmpty(k.Raeume);
            byte[] datei = IfcExportProbe.Schreiben(quelle, IfcExportProbe.Profil(), null, null, out GebaeudeExportBilanz bilanz);
            Assert.DoesNotContain(bilanz.Meldungen, m => m.Stufe == PruefStufe.Fehler);

            GebaeudeAbbild zurueck = IfcExportProbe.Lesen(datei);
            AbbildGebaeude g = Assert.Single(zurueck.Gebaeude);
            foreach (Raumkoerper rk in k.Raeume)
            {
                string name = quelle.Gebaeude[0].Raeume.Single(r => r.Kennung == rk.RaumKennung).Name;
                Dateikoerper d = g.Raeume.Single(r => r.Name == name).Koerper;
                Assert.NotNull(d);
                Assert.Equal("ExtrudedAreaSolid", d.Art);
                Assert.Empty(d.Vermerke);
                Assert.Equal(12, d.DreieckZahl);
                Assert.Equal(12, d.Randkanten.Count);
                // Die Punkte nach Rundung auf 1e-6 sind genau die Ecken der Schale.
                List<string> erwartet = rk.Schale.SelectMany(f => f).Select(Schluessel).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
                List<string> ist = d.PunkteM.Select(Schluessel).OrderBy(x => x, StringComparer.Ordinal).ToList();
                Assert.Equal(erwartet, ist);
                // Jede Normale ist die äußere Normale einer Schalenfläche, in deren Ebene das Dreieck liegt.
                NormalenNachAussen(d, name);
                double grund = Math.Abs(rk.Schale[0].Select((p, i) => p[0] * rk.Schale[0][(i + 1) % 4][1] - rk.Schale[0][(i + 1) % 4][0] * p[1]).Sum() / 2.0);
                Assert.Equal(grund * rk.HoeheM, Volumen(d), 6);
            }
            Assert.Equal(k.Raeume.Count, g.Raeume.Count(r => r.Koerper != null));
            Assert.Contains(g.Meldungen, m => m.Schluessel == P + "KOERPER_GELESEN"
                                              && m.Werte.SequenceEqual(new[] { k.Raeume.Count.ToString(CultureInfo.InvariantCulture), "0", (12 * k.Raeume.Count).ToString(CultureInfo.InvariantCulture) }));
            Assert.DoesNotContain(zurueck.Meldungen, m => m.Schluessel == P + "KOERPER_ART");

            // Probe 25 für den Körper: zweimal gelesen, byteweise gleich (invariante Kultur).
            byte[] Ausgabe(GebaeudeAbbild a) => Encoding.UTF8.GetBytes(string.Join("\n---\n", a.Gebaeude[0].Raeume.Select(r => r.Name + "\n" + r.Koerper?.Text())));
            byte[] erste = Ausgabe(zurueck);
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal(erste, Ausgabe(IfcExportProbe.Lesen(datei)));

            // Durchgereicht bis in die Zonengeometrie; der Grundriss bleibt der aus den Raumgrenzen bzw. schematisch.
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(zurueck, 0);
            Assert.True(zg.HatDateikoerper);
            Assert.Equal(12 * k.Raeume.Count, zg.DateikoerperDreiecke);
            foreach (AbbildRaum r in g.Raeume)
                Assert.Same(r.Koerper, zg.Raum(r.Kennung).Koerper);
            _aus.WriteLine("Probe 28: " + k.Raeume.Count + " Räume, " + zg.DateikoerperDreiecke + " Dreiecke");
        }

        // ==================================================================
        //  Probe 29
        // ==================================================================

        private static double Bogenflaeche() => 4.0 * 2.0 + 0.5 * 4.0 * 16 * Math.Sin(Math.PI / 16);

        public static IEnumerable<object[]> Arten()
        {
            // Datei, Raum, Art, Dreiecke, Punkte, Randkanten, Vermerke, Volumen [m³] (NaN = offen), konvex
            yield return new object[] { "ifc4_koerper_extrusion_polygon.ifc", 0, "ExtrudedAreaSolid", 20, 12, 18, "", 42.0, false };
            yield return new object[] { "ifc4_koerper_extrusion_bogen.ifc", 0, "ExtrudedAreaSolid", 72, 38, 42, "Bogen", Bogenflaeche() * 3.0, true };
            yield return new object[] { "ifc4_koerper_extrusion_bogen.ifc", 1, "ExtrudedAreaSolid", 72, 38, 42, "Bogen", Bogenflaeche() * 3.0, true };
            yield return new object[] { "ifc4_koerper_extrusion_bogen.ifc", 2, "ExtrudedAreaSolid", 124, 64, 64, "Bogen", 0.5 * 32 * Math.Sin(2 * Math.PI / 32) * 3.0, true };
            yield return new object[] { "ifc4_koerper_extrusion_loch.ifc", 0, "ExtrudedAreaSolid", 32, 16, 24, "", 60.0, false };
            yield return new object[] { "ifc2x3_koerper_brep.ifc", 0, "FacetedBrep", 12, 8, 12, "", 30.0, true };
            yield return new object[] { "ifc4_koerper_dreiecksnetz.ifc", 0, "TriangulatedFaceSet", 12, 8, 12, "", 30.0, true };
            yield return new object[] { "ifc4_koerper_vieleckssatz.ifc", 0, "PolygonalFaceSet", 12, 8, 12, "", 30.0, true };
            yield return new object[] { "ifc4_koerper_abgebildet.ifc", 0, "MappedItem", 12, 8, 12, "", 6.0, true };
            yield return new object[] { "ifc4_koerper_beschnitt.ifc", 0, "BooleanClippingResult", 12, 8, 12, "OhneBeschnitt", 36.0, true };
            yield return new object[] { "ifc4_koerper_offen.ifc", 0, "ShellBasedSurfaceModel", 10, 8, 12, "Offen", double.NaN, false };
            yield return new object[] { "ifc4_koerper_platzierung.ifc", 0, "ExtrudedAreaSolid", 12, 8, 12, "", 30.0, true };
        }

        [Theory]
        [MemberData(nameof(Arten))]
        public void Probe29_jede_Darstellungsart_ergibt_Art_Dreiecke_und_Vermerke(string datei, int raum, string art, int dreiecke,
                                                                                  int punkte, int kanten, string vermerke, double volumen, bool konvex)
        {
            GebaeudeImportAblauf a = Lesen(datei);
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);
            Dateikoerper k = g.Raeume[raum].Koerper;
            Assert.NotNull(k);
            _aus.WriteLine(datei + " Raum " + raum + ": " + k + ", " + k.PunkteM.Count + " Punkte, " + k.Randkanten.Count + " Kanten, V = "
                           + Volumen(k).ToString("R", CultureInfo.InvariantCulture));
            Assert.Equal(art, k.Art);
            Assert.Equal(dreiecke, k.DreieckZahl);
            Assert.Equal(punkte, k.PunkteM.Count);
            Assert.Equal(kanten, k.Randkanten.Count);
            Assert.Equal(vermerke, string.Join(",", k.Vermerke));
            Assert.Equal(k.DreieckZahl, k.Normalen.Count);
            Assert.All(k.Randkanten, e => Assert.True(e[0] < e[1]));
            // Die Punkte sind auf 1e-6 m gerundet — das Volumen trifft relativ auf 1e-6.
            if (!double.IsNaN(volumen))
                Assert.True(Math.Abs(volumen - Volumen(k)) <= 1e-6 * volumen, "Volumen " + Volumen(k).ToString("R", CultureInfo.InvariantCulture) + " statt " + volumen.ToString("R", CultureInfo.InvariantCulture));
            if (konvex) NormalenNachAussen(k, datei);
            // Der Körper ist Anzeige: Er kommt bis in die Zonengeometrie, der Grundriss bleibt schematisch.
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(a.Abbild, 0);
            Raumumriss u = zg.Raum(g.Raeume[raum].Kennung);
            Assert.Same(k, u.Koerper);
            Assert.Equal(Geometrieherkunft.Schematisch, u.Herkunft);
            Assert.Equal(g.Raeume.Sum(r => r.Koerper.DreieckZahl), zg.DateikoerperDreiecke);
            Assert.Contains(g.Meldungen, m => m.Schluessel == P + "KOERPER_GELESEN"
                                              && m.Werte.SequenceEqual(new[] { g.Raeume.Count.ToString(CultureInfo.InvariantCulture), "0", zg.DateikoerperDreiecke.ToString(CultureInfo.InvariantCulture) }));
            // Determinismus: ein zweites Lesen ergibt dieselbe Ausgabe.
            Assert.Equal(k.Text(), Lesen(datei).Abbild.Gebaeude[0].Raeume[raum].Koerper.Text());
        }

        [Fact]
        public void Probe29_Placement_Kette_mit_Drehung_und_Millimetern_in_Weltkoordinaten()
        {
            // Gebäude um 90° gedreht und um (10, 5, 0) m versetzt, Geschoss 3 m höher, Raum bei (1, 2, 0) m im Geschoss,
            // Körper 4 × 3 × 2,5 m: lokal (x, y, z) → Welt (10 − (y + 2), 5 + (x + 1), z + 3).
            Dateikoerper k = Lesen("ifc4_koerper_platzierung.ifc").Abbild.Gebaeude[0].Raeume[0].Koerper;
            var erwartet = new List<string>();
            foreach (double x in new[] { 0.0, 4.0 })
                foreach (double y in new[] { 0.0, 3.0 })
                    foreach (double z in new[] { 0.0, 2.5 })
                        erwartet.Add(Schluessel(new[] { 10.0 - (y + 2.0), 5.0 + (x + 1.0), z + 3.0 }));
            Assert.Equal(erwartet.OrderBy(x => x, StringComparer.Ordinal), k.PunkteM.Select(Schluessel).OrderBy(x => x, StringComparer.Ordinal));
        }

        [Fact]
        public void Probe29_Abbildung_mit_ungleichem_Massstab_und_Beschnitt_als_erster_Operand()
        {
            // Würfel 1 m, Ziel (0,1; 0,2; 0) m, Maßstab 2 : 1 : 3, Raum bei (1, 2, 0) m.
            Dateikoerper m = Lesen("ifc4_koerper_abgebildet.ifc").Abbild.Gebaeude[0].Raeume[0].Koerper;
            Assert.Equal(new[] { 1.1, 2.2, 0.0 }, Enumerable.Range(0, 3).Select(i => m.PunkteM.Min(p => p[i])));
            Assert.Equal(new[] { 3.1, 3.2, 3.0 }, Enumerable.Range(0, 3).Select(i => m.PunkteM.Max(p => p[i])));
            // Der Halbraum über 2,5 m fehlt: Der Körper reicht bis 3 m.
            Dateikoerper b = Lesen("ifc4_koerper_beschnitt.ifc").Abbild.Gebaeude[0].Raeume[0].Koerper;
            Assert.Equal(3.0, b.PunkteM.Max(p => p[2]));
        }

        [Fact]
        public void Probe29_AdvancedBrep_ergibt_keinen_Koerper_und_eine_Meldung()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_koerper_advancedbrep.ifc");
            AbbildGebaeude g = Assert.Single(a.Abbild.Gebaeude);
            Assert.Null(Assert.Single(g.Raeume).Koerper);
            Assert.Contains(a.Abbild.Meldungen, m => m.Schluessel == P + "KOERPER_ART" && m.Stufe == PruefStufe.Info
                                                      && m.Werte.SequenceEqual(new[] { "1", "IfcAdvancedBrep" }));
            Assert.Contains(g.Meldungen, m => m.Schluessel == P + "KOERPER_GELESEN" && m.Werte.SequenceEqual(new[] { "0", "1", "0" }));
            Zonengeometrie zg = GebaeudeGrundriss.Bilden(a.Abbild, 0);
            Assert.False(zg.HatDateikoerper);
            Assert.Equal(0, zg.DateikoerperDreiecke);
            Raumumriss u = Assert.Single(zg.Raeume);
            Assert.Null(u.Koerper);
            Assert.Equal(Geometrieherkunft.Schematisch, u.Herkunft);   // Rückfall: schematisch (keine Raumgrenzen)
        }

        [Fact]
        public void Ohne_Darstellung_kein_Koerper_und_keine_Koerpermeldung()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_haus.ifc");
            Assert.All(a.Abbild.Gebaeude.SelectMany(g => g.Raeume), r => Assert.Null(r.Koerper));
            IEnumerable<PruefMeldung> alle = a.Abbild.Meldungen.Concat(a.Abbild.Gebaeude.SelectMany(g => g.Meldungen));
            Assert.DoesNotContain(alle, m => m.Schluessel.StartsWith(P + "KOERPER", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Meldungstexte_stehen_in_beiden_Sprachen(string sprache)
        {
            CultureInfo k = CultureInfo.GetCultureInfo(sprache);
            string art = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(P + "KOERPER_ART", k);
            string gelesen = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(P + "KOERPER_GELESEN", k);
            Assert.Contains("IfcAdvancedBrep", string.Format(k, art, "1", "IfcAdvancedBrep"));
            Assert.Contains("300", string.Format(k, gelesen, "3", "0", "300"));
            Assert.NotEqual(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(P + "KOERPER_ART", CultureInfo.GetCultureInfo("de-DE")),
                            WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(P + "KOERPER_ART", CultureInfo.GetCultureInfo("en-US")));
        }

        // ==================================================================
        //  Ohrenschnitt und Proben
        // ==================================================================

        [Fact]
        public void Ohrenschnitt_mit_Loch_und_kollinearer_Ecke_deterministisch()
        {
            var p = new List<double[]>
            {
                new[] { 0.0, 0.0 }, new[] { 2.0, 0.0 }, new[] { 4.0, 0.0 }, new[] { 4.0, 4.0 }, new[] { 0.0, 4.0 },
                new[] { 1.0, 1.0 }, new[] { 1.0, 3.0 }, new[] { 3.0, 3.0 }, new[] { 3.0, 1.0 },
            };
            var ringe = new List<List<int>> { new List<int> { 0, 1, 2, 3, 4 }, new List<int> { 5, 6, 7, 8 } };
            List<int[]> d1 = IfcRaumkoerper.Dreiecke2D(p, ringe, out List<int> verloren);
            Assert.Empty(verloren);
            double flaeche = d1.Sum(d => ((p[d[1]][0] - p[d[0]][0]) * (p[d[2]][1] - p[d[0]][1]) - (p[d[1]][1] - p[d[0]][1]) * (p[d[2]][0] - p[d[0]][0])) / 2.0);
            Assert.Equal(12.0, flaeche, 9);
            Assert.All(d1, d => Assert.True((p[d[1]][0] - p[d[0]][0]) * (p[d[2]][1] - p[d[0]][1]) - (p[d[1]][1] - p[d[0]][1]) * (p[d[2]][0] - p[d[0]][0]) > 0));
            Assert.Equal(d1.Select(d => string.Join(",", d)), IfcRaumkoerper.Dreiecke2D(p, ringe, out _).Select(d => string.Join(",", d)));
            Assert.Equal(2, IfcRaumkoerper.Sehnen(0.1));
            Assert.Equal(16, IfcRaumkoerper.Sehnen(Math.PI));
            Assert.Equal(Dateikoerper.SEHNEN_VOLLKREIS, IfcRaumkoerper.Sehnen(2 * Math.PI));
        }

        [Fact]
        public void Huellbauteile_tragen_ihren_Koerper_ohne_Darstellung_keinen()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_koerper_bauteile.ifc");
            AbbildGebaeude g = a.Abbild.Gebaeude.Single();
            AbbildBauteil sued = g.Bauteile.Single(b => b.Name == "Außenwand Süd");
            AbbildBauteil fenster = sued.Oeffnungen.Single();
            Assert.NotNull(sued.Koerper);
            Assert.Equal(12, sued.Koerper.DreieckZahl);
            NormalenNachAussen(sued.Koerper, "Außenwand Süd");
            Assert.Equal(9.24 * 0.3 * 3.0, Volumen(sued.Koerper), 6);
            Assert.NotNull(fenster.Koerper);
            Assert.Equal(1.5 * 0.3 * 1.2, Volumen(fenster.Koerper), 6);
            Assert.Equal(-0.3, fenster.Koerper.PunkteM.Min(p => p[1]), 6);
            foreach (string name in new[] { "Innenwand", "Bodenplatte" })
                Assert.Null(g.Bauteile.Single(b => b.Name == name).Koerper);
            Assert.All(g.Raeume, r => Assert.NotNull(r.Koerper));
            Assert.Contains(g.Meldungen, m => m.Schluessel == P + "BAUTEILKOERPER_GELESEN" && m.Werte[0] == "2" && m.Werte[1] == "24");
            Assert.DoesNotContain(g.Meldungen, m => m.Schluessel == P + "BAUTEILKOERPER_GRENZE");
            Assert.DoesNotContain(a.Abbild.Meldungen, m => m.Schluessel == P + "BAUTEILKOERPER_ART");
        }

        [Fact]
        public void Bauteilkoerper_teilen_sich_die_Dreiecksgrenze_mit_den_Raeumen()
        {
            Assert.Equal(200_000, (int)(Dateikoerper.DREIECKSGRENZE * IfcAbbildBauer.BAUTEILKOERPER_RAUMANTEIL + 1e-9));
            // Ohne Bauteilkörper bleibt alles wie zuvor: die Nachbarprobe trägt keine.
            GebaeudeImportAblauf a = Lesen("ifc4_koerper_nachbarn.ifc");
            Assert.All(a.Abbild.Gebaeude.Single().Bauteile, b => Assert.Null(b.Koerper));
            Assert.DoesNotContain(a.Abbild.Gebaeude.Single().Meldungen, m => m.Schluessel == P + "BAUTEILKOERPER_GELESEN");
        }

        [Fact(Skip = "Erzeuger: schreibt die Raumkörperproben nach Referenzlaeufe/Importproben — nur von Hand, siehe IfcProbenTests.")]
        public void Erzeuger_schreibt_die_Raumkoerperproben()
        {
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Raumkoerperproben())
                File.WriteAllBytes(Path.Combine(IfcProbenTests.Ordner(), p.Key), p.Value);
        }

        [Fact]
        public void Die_abgelegten_Raumkoerperproben_sind_byte_gleich_neu_erzeugbar()
        {
            var funde = new List<string>();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Raumkoerperproben())
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), p.Key);
                if (!File.Exists(pfad)) funde.Add(p.Key + ": fehlt");
                else if (!File.ReadAllBytes(pfad).SequenceEqual(p.Value)) funde.Add(p.Key + ": weicht ab");
                else Assert.True(p.Value.Length < 16 * 1024, p.Key + " ist keine Kleinstdatei");
            }
            Assert.True(funde.Count == 0, "Die Raumkörperproben weichen vom Erzeuger ab:\n" + string.Join("\n", funde));
        }
    }
}
