using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Geometrie der Projektdatei</b> (Datenaustauschkonzept 17.2 bis 17.5, Stufe K2; Proben 40 und 43 aus 17.8): Raumkörper
    /// aus dem Raumpolygon mit der Quellfläche je Dreieck, Bauteilkörper in der gemessenen Richtung, Öffnungen als Loch mit
    /// Laibung, Grundriss je Raum, benannte Rückfälle. Alle Proben synthetisch (<see cref="SqprojProbenErzeuger"/>).
    /// </summary>
    public sealed class SqprojGeometrieTests : IDisposable
    {
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private GebaeudeAbbild Lesen(SqprojProbenErzeuger e)
        {
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad("geometrie"));
            _dateien.Add(pfad);
            var profil = new SqprojImportProfil();
            using (FileStream f = File.OpenRead(pfad))
                return profil.LeserErzeugen().Lesen(f, profil, null, CancellationToken.None);
        }

        private static AbbildRaum Raum(GebaeudeAbbild a, string k) => a.Gebaeude.SelectMany(g => g.Raeume).Single(r => r.Kennung == k);

        private static IEnumerable<AbbildBauteil> Alle(GebaeudeAbbild a)
        {
            List<AbbildBauteil> b = a.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.BauteileOhneGebaeude).Distinct().ToList();
            return b.Concat(b.SelectMany(x => x.Oeffnungen));
        }

        private static AbbildBauteil Bauteil(GebaeudeAbbild a, string k) => Alle(a).First(b => b.Kennung == k);

        private static double Volumen(Dateikoerper k) => Koerpergrundriss.Volumen(k);

        private static (double Min, double Max) Spanne(Dateikoerper k, int achse) => (k.PunkteM.Min(p => p[achse]), k.PunkteM.Max(p => p[achse]));

        /// <summary>Jede Kante gehört genau zwei Dreiecken mit gegenläufigem Umlauf (auf ganze Mikrometer gerundet).</summary>
        private static void Geschlossen(Dateikoerper k)
        {
            var kanten = new Dictionary<(long, long, long, long, long, long), int>();
            (long, long, long) P(int i) => ((long)Math.Round(k.PunkteM[i][0] * 1e6), (long)Math.Round(k.PunkteM[i][1] * 1e6), (long)Math.Round(k.PunkteM[i][2] * 1e6));
            foreach (int[] d in k.Dreiecke)
                for (int i = 0; i < 3; i++)
                {
                    var a = P(d[i]);
                    var b = P(d[(i + 1) % 3]);
                    var s = (a.Item1, a.Item2, a.Item3, b.Item1, b.Item2, b.Item3);
                    kanten[s] = kanten.TryGetValue(s, out int n) ? n + 1 : 1;
                }
            foreach (var e in kanten)
            {
                var g = (e.Key.Item4, e.Key.Item5, e.Key.Item6, e.Key.Item1, e.Key.Item2, e.Key.Item3);
                Assert.True(kanten.TryGetValue(g, out int m) && m == e.Value, "offene Kante");
            }
        }

        // ---------------- Probe 40 ----------------

        [Fact]
        public void Probe40_Raumkoerper_aus_dem_Raumpolygon_geschlossen_mit_Volumen_und_Quelle_AusFlaechen()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Dateikoerper k = Raum(a, "R1").Koerper;
            Assert.NotNull(k);
            Assert.Equal(Koerperquelle.AusFlaechen, k.Quelle);
            Assert.Equal(Koerperbildner.ART_RAUMPOLYGON, k.Art);
            Assert.Equal(50.0, Volumen(k), 6);
            Geschlossen(k);
            Assert.NotNull(Raum(a, "R2").Koerper);
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojGeometrie.GEOMETRIE && m.Werte[0] == "2" && m.Werte[1] == "3");
        }

        [Fact]
        public void Probe40_ohne_Raum_XML_der_benannte_Rueckfall()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Assert.Null(Raum(a, "R3").Koerper);
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojGeometrie.RAUM_OHNE_POLYGON && m.Werte[0] == "Raum Drei");
        }

        [Fact]
        public void Probe40_Aussenwand_in_der_Aussenoberflaeche_wird_nach_innen_extrudiert()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Dateikoerper k = Bauteil(a, "AW").Koerper;
            Assert.NotNull(k);
            Assert.Equal(Koerperbildner.ART_FLAECHENEXTRUSION, k.Art);
            (double y0, double y1) = Spanne(k, 1);
            Assert.Equal(-0.30, y0, 6);
            Assert.Equal(0.0, y1, 6);
            Assert.DoesNotContain(Koerpervermerk.Bezugsebene_angenommen, k.Vermerke);
            Assert.DoesNotContain(Koerpervermerk.Vorgabedicke, k.Vermerke);
            Geschlossen(k);
        }

        [Fact]
        public void Probe40_Innenwand_in_der_Achse_wird_beidseitig_extrudiert_und_einmal_gebildet()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            List<AbbildBauteil> iw = Alle(a).Where(b => b.Kennung == "IW").Distinct().ToList();
            Assert.Single(iw);
            (double x0, double x1) = Spanne(iw[0].Koerper, 0);
            Assert.Equal(4.0, x0, 6);
            Assert.Equal(4.24, x1, 6);
            Assert.DoesNotContain(Koerpervermerk.Bezugsebene_angenommen, iw[0].Koerper.Vermerke);
        }

        [Fact]
        public void Vorgabedicke_ohne_Aufbau_und_ohne_Dicke_an_der_Flaeche_mit_Vermerk()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Dateikoerper bp = Bauteil(a, "BP").Koerper;
            Assert.Contains(Koerpervermerk.Vorgabedicke, bp.Vermerke);
            (double z0, double z1) = Spanne(bp, 2);
            Assert.Equal(-SqprojGeometrie.Vorgabedicke(Bauteilart.Bodenplatte), z0, 6);
            Assert.Equal(0.0, z1, 6);
            // Dicke an der Fläche (Thickness) geht der Vorgabe vor; die Fläche liegt in der Innenoberfläche: vom Raum weg.
            Dateikoerper da = Bauteil(a, "DA").Koerper;
            Assert.DoesNotContain(Koerpervermerk.Vorgabedicke, da.Vermerke);
            Assert.Equal((2.5, 2.7), (Math.Round(Spanne(da, 2).Min, 6), Math.Round(Spanne(da, 2).Max, 6)));
        }

        [Fact]
        public void Probe40_Fenster_als_Loch_mit_Laibung_und_eigener_Koerper_mittig_in_der_Wand()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Dateikoerper wand = Bauteil(a, "AW").Koerper;
            // Die Laibung trägt die Kennung der Öffnung; die Wand verliert die Fensterfläche zweimal.
            Assert.Contains("FS", wand.Quellflaechen);
            Assert.Equal((11.05 - 1.0) * 0.30, Volumen(wand), 6);
            Dateikoerper fe = Bauteil(a, "FS").Koerper;
            Assert.NotNull(fe);
            (double y0, double y1) = Spanne(fe, 1);
            Assert.Equal(-0.15, (y0 + y1) / 2.0, 6);
            Assert.Equal(SqprojGeometrie.Vorgabedicke(Bauteilart.Fenster), y1 - y0, 6);
            Assert.Contains(Koerpervermerk.Vorgabedicke, fe.Vermerke);
            Assert.Empty(Bauteil(a, "FS").Meldungen.Where(m => m.Schluessel == SqprojGeometrie.OEFFNUNG_OHNE_WAND));
        }

        [Fact]
        public void Oeffnung_ausserhalb_ihrer_Wand_meldet_OEFFNUNG_OHNE_WAND()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus()
                .Flaeche("FX", 3, 3, 1.0, 1.0, 180.0, 90.0, eltern: "AW", u: 1.3).Bezug("E9", "R1", "FX", 5)
                .Geometrie("FX", SqprojProbenErzeuger.GeoXml(null, ("Inner", SqprojProbenErzeuger.RechteckY(-0.30, 10.0, 11.0, 1.0, 2.0)))));
            AbbildBauteil fx = Bauteil(a, "FX");
            Assert.Contains(fx.Meldungen, m => m.Schluessel == SqprojGeometrie.OEFFNUNG_OHNE_WAND);
            Assert.NotNull(fx.Koerper);
            Assert.DoesNotContain("FX", Bauteil(a, "AW").Koerper.Quellflaechen);
        }

        [Fact]
        public void Gemischter_Umlaufsinn_Randpunkte_zeigen_vom_Raum_weg()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            // AW ist im Uhrzeigersinn umlaufen, BP und DA gegen ihn: alle Normalen zeigen vom Raum R1 weg.
            double[] Normale(string k) => Polygonnetz.Normiert(Polygonnetz.Newell(Bauteil(a, k).RandpunkteM));
            Assert.Equal(-1.0, Normale("AW")[1], 6);
            Assert.Equal(-1.0, Normale("FS")[1], 6);
            Assert.Equal(-1.0, Normale("BP")[2], 6);
            Assert.Equal(1.0, Normale("DA")[2], 6);
            Assert.Equal(1.0, Normale("IW")[0], 6);
        }

        [Fact]
        public void Flaechen_der_Polygone_treffen_GrossArea_auf_ein_Prozent()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            foreach (AbbildBauteil b in Alle(a).Where(b => b.RandpunkteM != null))
            {
                double[] n = Polygonnetz.Newell(b.RandpunkteM);
                double f = Math.Sqrt(Polygonnetz.Punkt(n, n)) / 2.0;
                Assert.True(Math.Abs(f - b.BruttoflaecheM2.Value) <= 0.01 * b.BruttoflaecheM2.Value, b.Kennung);
            }
        }

        [Fact]
        public void Quellflaeche_je_Dreieck_Mantel_an_den_Waenden_geteilt_sonst_Ersatzkennung()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Dateikoerper k = Raum(a, "R1").Koerper;
            Assert.Equal(k.Dreiecke.Count, k.Quellflaechen.Count);
            string Quelle(Func<double[], bool> lage, double[] normale)
                => k.Quellflaechen.Where((q, i) => Math.Abs(Polygonnetz.Punkt(k.Normalen[i], normale) - 1.0) < 1e-6
                                                   && k.Dreiecke[i].All(j => lage(k.PunkteM[j]))).Distinct().Single();
            Assert.Equal("AW", Quelle(p => Math.Abs(p[1]) < 1e-6, new[] { 0.0, -1.0, 0.0 }));
            Assert.Equal("IW", Quelle(p => Math.Abs(p[0] - 4.0) < 1e-6, new[] { 1.0, 0.0, 0.0 }));
            Assert.Equal("BP", Quelle(p => Math.Abs(p[2]) < 1e-6, new[] { 0.0, 0.0, -1.0 }));
            Assert.Equal("DA", Quelle(p => Math.Abs(p[2] - 2.5) < 1e-6, new[] { 0.0, 0.0, 1.0 }));
            Assert.StartsWith("R1:Mantel", Quelle(p => Math.Abs(p[1] - 5.0) < 1e-6, new[] { 0.0, 1.0, 0.0 }));
            // R2 hat keine Decke in der Datei: sprechende Ersatzkennung.
            Assert.Contains("R2:Decke0", Raum(a, "R2").Koerper.Quellflaechen);
        }

        [Fact]
        public void Wand_mit_zwei_Abschnitten_teilt_die_Mantelflaeche()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus()
                .Flaeche("N1", 1, 3, 5.0, 5.0, 0.0, 90.0).Bezug("E10", "R1", "N1", 1)
                .Geometrie("N1", SqprojProbenErzeuger.GeoXml(null, ("DIN18599_2011", SqprojProbenErzeuger.RechteckY(5.3, -0.3, 2.0, 0.0, 2.5))), 0.30)
                .Flaeche("N2", 1, 2, 5.0, 5.0, 0.0, 90.0).Bezug("E11", "R1", "N2", 1)
                .Geometrie("N2", SqprojProbenErzeuger.GeoXml(null, ("DIN18599_2011", SqprojProbenErzeuger.RechteckY(5.3, 2.0, 4.12, 0.0, 2.5))), 0.30));
            Dateikoerper k = Raum(a, "R1").Koerper;
            Geschlossen(k);
            var nord = Enumerable.Range(0, k.Dreiecke.Count).Where(i => Math.Abs(k.Normalen[i][1] - 1.0) < 1e-6).ToList();
            Assert.All(nord.Where(i => k.Dreiecke[i].All(j => k.PunkteM[j][0] <= 2.0 + 1e-9)), i => Assert.Equal("N1", k.Quellflaechen[i]));
            Assert.All(nord.Where(i => k.Dreiecke[i].All(j => k.PunkteM[j][0] >= 2.0 - 1e-9)), i => Assert.Equal("N2", k.Quellflaechen[i]));
            Assert.Equal(50.0, Volumen(k), 6);
        }

        [Fact]
        public void Raum_mit_Pultdach()
        {
            var grund = new[] { new[] { 0.0, 0.0 }, new[] { 4.0, 0.0 }, new[] { 4.0, 5.0 }, new[] { 0.0, 5.0 } };
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus()
                .Raum("R4", "Raum Vier", "FE", null, 20.0, heizung: 1)
                .Raumgeometrie("R4", SqprojProbenErzeuger.RaumXml(grund, 10.0, (x, y) => 12.5 + 0.2 * y)));
            Dateikoerper k = Raum(a, "R4").Koerper;
            Assert.NotNull(k);
            Geschlossen(k);
            Assert.Equal(4.0 * 5.0 * (2.5 + 0.5), Volumen(k), 6);
            Assert.Equal(13.5, Spanne(k, 2).Max, 6);
        }

        [Fact]
        public void Raum_mit_zwei_Polygonen_und_mit_Loch()
        {
            var grund = new[] { new[] { 0.0, 0.0 }, new[] { 4.0, 0.0 }, new[] { 4.0, 5.0 }, new[] { 0.0, 5.0 } };
            var anbau = new[] { new[] { 4.0, 0.0 }, new[] { 6.0, 0.0 }, new[] { 6.0, 2.0 }, new[] { 4.0, 2.0 } };
            var loch = new[] { new[] { 1.0, 1.0 }, new[] { 2.0, 1.0 }, new[] { 2.0, 2.0 }, new[] { 1.0, 2.0 } };
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus()
                .Raum("R5", "Raum Fünf", "FE", null, 24.0, heizung: 1)
                .Raumgeometrie("R5", SqprojProbenErzeuger.RaumXml(grund, 20.0, (_, _) => 22.5, (anbau, 20.0, 22.0)))
                .Raum("R6", "Raum Sechs", "FE", null, 19.0, heizung: 1)
                .Raumgeometrie("R6", SqprojProbenErzeuger.RaumXml(grund, 30.0, (_, _) => 32.5, (loch, 30.0, 32.5))));
            Dateikoerper k5 = Raum(a, "R5").Koerper;
            Assert.Equal(20.0 * 2.5 + 4.0 * 2.0, Volumen(k5), 6);
            Dateikoerper k6 = Raum(a, "R6").Koerper;
            Geschlossen(k6);
            Assert.Equal(19.0 * 2.5, Volumen(k6), 6);
            // Grundriss: zwei Außenringe bzw. Außenring mit Loch.
            IReadOnlyList<Raumgrundriss> gr = GebaeudeRaumgrundrisse.Bilden(a, 0);
            Assert.Equal(2, gr.Single(g => g.Quellkennung == "R5").Ringe.Count(r => !r.IstLoch));
            Assert.Single(gr.Single(g => g.Quellkennung == "R6").Ringe, r => r.IstLoch);
            Assert.Equal(19.0, gr.Single(g => g.Quellkennung == "R6").RingflaecheM2, 6);
        }

        [Fact]
        public void Grundriss_je_Raum_unmittelbar_aus_dem_Raumpolygon()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Raumgrundriss g = GebaeudeRaumgrundrisse.Bilden(a, 0).Single(x => x.Quellkennung == "R1");
            Assert.Equal(Umrissherleitung.Boden, g.Herleitung);
            Assert.Contains(Grundrissvermerk.Raumpolygon, g.Vermerke);
            Assert.Equal(20.0, g.RingflaecheM2, 6);
            Assert.Equal(0.0, g.BodenM, 6);
            Assert.Equal(2.5, g.HoeheM, 6);
        }

        [Fact]
        public void Ungueltiges_Raumpolygon_ergibt_Rueckfall_mit_Meldung_und_Grund()
        {
            // Sanduhr: die Kanten kreuzen sich, der Körper schließt nicht bzw. ist nicht richtbar.
            var sanduhr = new[] { new[] { 0.0, 0.0 }, new[] { 4.0, 5.0 }, new[] { 4.0, 0.0 }, new[] { 0.0, 5.0 } };
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus()
                .Raum("R7", "Raum Sieben", "FE", null, 10.0, heizung: 1)
                .Raumgeometrie("R7", SqprojProbenErzeuger.RaumXml(sanduhr, 0.0, (_, _) => -1.0)));
            Assert.Null(Raum(a, "R7").Koerper);
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == SqprojGeometrie.KOERPER_RAUM);
            Assert.Equal("Raum Sieben", m.Werte[0]);
            Assert.StartsWith("KOERPER_", m.Werte[1]);
        }

        [Fact]
        public void Offene_Huelle_ergibt_keinen_Koerper_sondern_den_Rueckfall_Umrissprisma()
        {
            // Der Hüllflächenweg (17.2 b) mit zwei fehlenden Flächen eines Quaders: kein Körper, Grund mit offenen Kanten.
            var flaechen = new[]
            {
                new Quellflaeche { Kennung = "U", Aussen = SqprojProbenErzeuger.RechteckZ(0.0, 0.0, 1.0, 0.0, 1.0) },
                new Quellflaeche { Kennung = "O", Aussen = SqprojProbenErzeuger.RechteckZ(1.0, 0.0, 1.0, 0.0, 1.0) },
                new Quellflaeche { Kennung = "S", Aussen = SqprojProbenErzeuger.RechteckY(0.0, 0.0, 1.0, 0.0, 1.0) },
                new Quellflaeche { Kennung = "N", Aussen = SqprojProbenErzeuger.RechteckY(1.0, 0.0, 1.0, 0.0, 1.0) },
            };
            Koerperergebnis e = Koerperbildner.Huelle(flaechen);
            Assert.False(e.Gebildet);
            Assert.Equal(Koerperbildner.GRUND_NICHT_GESCHLOSSEN, e.Grund);
            Assert.Equal(Koerperbildner.RUECKFALL_UMRISSPRISMA, e.Rueckfall);
        }

        [Fact]
        public void Schleifen_Matrix_wird_angewandt_und_die_Rangfolge_gilt()
        {
            double[] m = { 0, 1, 0, -1, 0, 0, 0, 0, 1, 10.0, 20.0, 0.0 };   // Drehung um 90° und Verschiebung
            IReadOnlyList<SqprojSchleife> s = SqprojGeometrie.SchleifenLesen(SqprojProbenErzeuger.GeoXml(m,
                ("Inner", SqprojProbenErzeuger.RechteckY(0.0, 0.0, 1.0, 0.0, 1.0)),
                ("EN12831", SqprojProbenErzeuger.RechteckY(0.0, 0.0, 3.0, 0.0, 1.0)),
                ("DIN18599_2011", SqprojProbenErzeuger.RechteckY(0.0, 0.0, 2.0, 0.0, 1.0))));
            Assert.Equal(3, s.Count);
            // (1, 0, 0) → x′ = 1·0 + 0·(−1) + 10 = 10, y′ = 1·1 + 20 = 21.
            Assert.Equal(new[] { 10.0, 21.0, 0.0 }, s[0].PunkteM[1]);
            Assert.Equal(SqprojGeometrie.REGEL_DIN18599_2011, SqprojGeometrie.Waehlen(s, SqprojGeometrie.RANG_OPAK).Value.Regel);
            Assert.Equal(SqprojGeometrie.REGEL_INNER, SqprojGeometrie.Waehlen(s, SqprojGeometrie.RANG_OEFFNUNG).Value.Regel);
            Assert.Empty(SqprojGeometrie.SchleifenLesen("<kein xml"));
        }

        [Fact]
        public void Richtung_aus_dem_gemessenen_Abstand()
        {
            Assert.Equal((Extrusionsrichtung.NachAussen, false), SqprojGeometrie.Richtung(0.005, 0.30));
            Assert.Equal((Extrusionsrichtung.Beidseitig, false), SqprojGeometrie.Richtung(0.15, 0.30));
            Assert.Equal((Extrusionsrichtung.NachInnen, false), SqprojGeometrie.Richtung(0.31, 0.30));
            Assert.Equal((Extrusionsrichtung.Beidseitig, true), SqprojGeometrie.Richtung(0.60, 0.30));
        }

        [Fact]
        public void Bezugsebene_angenommen_wenn_die_Lage_zu_keinem_Fall_passt()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus()
                .Flaeche("OW", 1, 3, 12.5, 12.5, 270.0, 90.0).Bezug("E12", "R1", "OW", 1)
                .Geometrie("OW", SqprojProbenErzeuger.GeoXml(null, ("DIN18599_2011", SqprojProbenErzeuger.RechteckX(-0.35, 0.0, 5.0, 0.0, 2.5))), 0.50));
            Dateikoerper k = Bauteil(a, "OW").Koerper;
            Assert.Contains(Koerpervermerk.Bezugsebene_angenommen, k.Vermerke);
            (double x0, double x1) = Spanne(k, 0);
            Assert.Equal(-0.60, x0, 6);
            Assert.Equal(-0.10, x1, 6);
        }

        // ---------------- Probe 43 und Determinismus ----------------

        [Fact]
        public void Probe43_keine_zweite_Quelle()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Geometriehaus());
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel.Contains("KOERPER_ABWEICHUNG", StringComparison.Ordinal)
                                                     || m.Schluessel.Contains("KOERPER_REST", StringComparison.Ordinal));
            Assert.All(Alle(a), b => Assert.NotEqual(Flaechenherkunft.Koerper, b.Flaechenherkunft));
            Assert.All(Alle(a), b => Assert.DoesNotContain(b.Grenzen, g => g.Herkunft != Grenzherkunft.Raumgrenze));
            Assert.All(Alle(a).Where(b => b.Koerper != null).Select(b => b.Koerper)
                              .Concat(a.Gebaeude.SelectMany(g => g.Raeume).Where(r => r.Koerper != null).Select(r => r.Koerper)),
                       k => Assert.Equal(Koerperquelle.AusFlaechen, k.Quelle));
        }

        [Fact]
        public void Gleiche_Datei_gleiche_Koerper_byteweise()
        {
            string Text(GebaeudeAbbild a)
                => string.Join("\n", a.Gebaeude.SelectMany(g => g.Raeume).Select(r => r.Kennung + ":" + r.Koerper?.Text())
                                     .Concat(Alle(a).Select(b => b.Kennung + ":" + b.Koerper?.Text() + ":" + string.Join(";", b.Koerper?.Quellflaechen ?? Array.Empty<string>()))));
            Assert.Equal(Text(Lesen(SqprojProbenErzeuger.Geometriehaus())), Text(Lesen(SqprojProbenErzeuger.Geometriehaus())));
        }

        [Fact]
        public void Datei_ohne_Geometrie_meldet_einmal_GEOMETRIE_FEHLT()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Standard());
            Assert.Single(a.Meldungen, m => m.Schluessel == SqprojGeometrie.GEOMETRIE_FEHLT);
            Assert.All(a.Gebaeude.SelectMany(g => g.Raeume), r => Assert.Null(r.Koerper));
        }
    }
}
