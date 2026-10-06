using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>HC-5 Teil A — Grundriss je Raum aus dem Dateikörper</b> (Konzept HottCAD-Verbund 11.2, 11.8; E94): die Ableitung
    /// <see cref="Koerpergrundriss"/> und die Grundrisse je Raum <see cref="GebaeudeRaumgrundrisse"/> an den Raumkörperproben
    /// unter <c>Referenzlaeufe/Importproben/</c> samt den zwei neuen (<c>ifc4_koerper_grundriss_stufe.ifc</c>,
    /// <c>ifc4_koerper_grundriss_ohne_boden.ifc</c>), an synthetischen Körpern (Rückfälle, Orientierung, Überlappung,
    /// Splitter, georeferenzierte Lage) und der Textform. Ohne Datenbank.
    /// </summary>
    public sealed class KoerpergrundrissTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly ITestOutputHelper _aus;

        public KoerpergrundrissTests(ITestOutputHelper aus) => _aus = aus;

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

        private static Raumgrundriss Einziger(string datei) => Assert.Single(Grundrisse(datei));

        private static AbbildRaum Raum(string datei) => Lesen(datei).Gebaeude[0].Raeume.Single();

        /// <summary>Ein Dateikörper aus Flächen [m] (je Fläche ein Fächer vom ersten Punkt, Normale aus dem Umlauf).</summary>
        internal static Dateikoerper Koerper(IEnumerable<double[][]> flaechen, bool offen = false)
        {
            var punkte = new List<double[]>();
            var dreiecke = new List<int[]>();
            var normalen = new List<double[]>();
            int Index(double[] p)
            {
                int i = punkte.FindIndex(q => q[0] == p[0] && q[1] == p[1] && q[2] == p[2]);
                if (i >= 0) return i;
                punkte.Add(p);
                return punkte.Count - 1;
            }
            foreach (double[][] f in flaechen)
                for (int k = 1; k + 1 < f.Length; k++)
                {
                    double[] a = f[0], b = f[k], c = f[k + 1];
                    double[] u = { b[0] - a[0], b[1] - a[1], b[2] - a[2] }, w = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                    double[] n = { u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0] };
                    double l = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
                    dreiecke.Add(new[] { Index(a), Index(b), Index(c) });
                    normalen.Add(new[] { n[0] / l, n[1] / l, n[2] / l });
                }
            return new Dateikoerper
            {
                PunkteM = punkte, Dreiecke = dreiecke, Normalen = normalen, Art = "Probe",
                Vermerke = offen ? new[] { Koerpervermerk.Offen } : Array.Empty<Koerpervermerk>(),
            };
        }

        /// <summary>Die sechs Flächen eines Quaders [m] mit Umlauf nach außen: Boden, Decke, Süd, Ost, Nord, West.</summary>
        internal static double[][][] Quader(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            double[] P(int i, int j, int k) => new[] { i == 0 ? x0 : x1, j == 0 ? y0 : y1, k == 0 ? z0 : z1 };
            return new[]
            {
                new[] { P(0, 0, 0), P(0, 1, 0), P(1, 1, 0), P(1, 0, 0) },
                new[] { P(0, 0, 1), P(1, 0, 1), P(1, 1, 1), P(0, 1, 1) },
                new[] { P(0, 0, 0), P(1, 0, 0), P(1, 0, 1), P(0, 0, 1) },
                new[] { P(1, 0, 0), P(1, 1, 0), P(1, 1, 1), P(1, 0, 1) },
                new[] { P(1, 1, 0), P(0, 1, 0), P(0, 1, 1), P(1, 1, 1) },
                new[] { P(0, 1, 0), P(0, 0, 0), P(0, 0, 1), P(0, 1, 1) },
            };
        }

        private static double[][][] Gekehrt(double[][][] flaechen) => flaechen.Select(f => f.Reverse().ToArray()).ToArray();

        // ==================================================================
        //  Proben aus 11.2
        // ==================================================================

        [Theory]
        [InlineData("ifc2x3_koerper_brep.ifc")]
        [InlineData("ifc4_koerper_vieleckssatz.ifc")]
        [InlineData("ifc4_koerper_dreiecksnetz.ifc")]
        [InlineData("ifc4_koerper_offen.ifc")]
        public void Quader_tragen_KoerperBoden_mit_einem_Ring_der_Bodenflaeche(string datei)
        {
            Raumgrundriss g = Einziger(datei);
            Assert.Equal(Umrissherleitung.KoerperBoden, g.Herleitung);
            Assert.Equal(Geometrieherkunft.Dateikoerper, g.Herkunft);
            Assert.Equal("1000,2000;5000,2000;5000,5000;1000,5000", g.RingeText);
            Assert.Equal(12.0, g.RingflaecheM2, 9);
            Assert.Equal(0.0, g.BodenM, 9);
            Assert.Equal(2.5, g.HoeheM, 9);
            // Der Mengensatz nennt 20 m²: −40 % — gespeichert wird trotzdem, mit Vermerk (F8).
            Assert.Equal(-0.4, g.Abweichung.Value, 9);
            Assert.Equal(new[] { Grundrissvermerk.Flaeche }, g.Vermerke);
            Assert.Equal("Raum", g.Raumname);
            Assert.Equal("Erdgeschoss", g.Geschoss);
            Assert.Equal(0.0, g.GeschossLageM.Value, 9);
        }

        [Fact]
        public void Extrusion_Polygon_deckt_sich_mit_dem_Profilring_auf_1_mm()
        {
            AbbildRaum r = Raum("ifc4_koerper_extrusion_polygon.ifc");
            Raumgrundriss g = Einziger("ifc4_koerper_extrusion_polygon.ifc");
            Assert.Equal(Umrissherleitung.KoerperBoden, g.Herleitung);
            Assert.NotNull(r.GrundrissM);
            Grundrissring ring = Assert.Single(g.Ringe);
            Assert.Equal(r.GrundrissM.Count, ring.PunkteMm.Count);
            foreach (double[] p in r.GrundrissM)
                Assert.Contains(ring.PunkteM, q => Math.Abs(q[0] - p[0]) <= 0.001 && Math.Abs(q[1] - p[1]) <= 0.001);
            Assert.Equal(14.0, g.RingflaecheM2, 9);
        }

        [Fact]
        public void Der_neue_Grundriss_speist_weder_Profilring_noch_Raumflaeche()
        {
            // 11.5: Ableitung und Speicherung lassen das Abbild unberührt — Profilring, Fläche und Herkunft der Fläche.
            GebaeudeAbbild a = Lesen("ifc4_koerper_extrusion_polygon.ifc");
            AbbildRaum r = a.Gebaeude[0].Raeume.Single();
            string Stand() => string.Join(";", r.GrundrissM.Select(p => p[0].ToString("R", CultureInfo.InvariantCulture) + "," +
                                                                         p[1].ToString("R", CultureInfo.InvariantCulture)))
                              + "|" + r.FlaecheM2?.ToString("R", CultureInfo.InvariantCulture) + "|" + r.FlaecheAusGrundriss;
            string vorher = Stand();
            Assert.Single(GebaeudeRaumgrundrisse.Bilden(a, 0));
            Assert.Equal(vorher, Stand());
            Zonengeometrie z = GebaeudeGrundriss.Bilden(a, 0);
            Assert.All(z.Raeume, u => Assert.NotEqual(Geometrieherkunft.Dateikoerper, u.Herkunft));
        }

        [Fact]
        public void Extrusion_mit_Loch_traegt_Aussenring_und_Loch_20_m2()
        {
            Raumgrundriss g = Einziger("ifc4_koerper_extrusion_loch.ifc");
            Assert.Equal(2, g.Ringe.Count);
            Assert.False(g.Ringe[0].IstLoch);
            Assert.True(g.Ringe[1].IstLoch);
            Assert.Equal(24.0, g.Ringe[0].FlaecheM2, 9);
            Assert.Equal(4.0, g.Ringe[1].FlaecheM2, 9);
            Assert.Equal(20.0, g.RingflaecheM2, 9);
            Assert.Equal("1000,2000;7000,2000;7000,6000;1000,6000|3000,3000;3000,5000;5000,5000;5000,3000", g.RingeText);
            Assert.Empty(g.Vermerke);
        }

        [Fact]
        public void Extrusion_mit_Bogen_behaelt_den_Sehnenzug()
        {
            IReadOnlyList<Raumgrundriss> g = Grundrisse("ifc4_koerper_extrusion_bogen.ifc");
            Assert.Equal(3, g.Count);
            Assert.All(g, x => Assert.Equal(Umrissherleitung.KoerperBoden, x.Herleitung));
            // Rechteck 4 × 2 m mit Halbkreis r = 2 m: vier Ecken plus 15 Bogenpunkte; der Kreis r = 1 m behält 32 Sehnen.
            Assert.Equal(19, g[0].Ringe.Single().PunkteMm.Count);
            Assert.Equal(19, g[1].Ringe.Single().PunkteMm.Count);
            Assert.Equal(32, g[2].Ringe.Single().PunkteMm.Count);
            Assert.InRange(g[0].RingflaecheM2, 14.2, 8.0 + Math.PI * 2.0);
        }

        [Fact]
        public void Abgebildet_und_Platzierung_tragen_Lage_und_Boden()
        {
            Raumgrundriss a = Einziger("ifc4_koerper_abgebildet.ifc");
            Assert.Equal("1100,2200;3100,2200;3100,3200;1100,3200", a.RingeText);
            Assert.Equal(3.0, a.HoeheM, 9);
            // Gedreht um 90°, versetzt um (10, 5) m, Geschoss 3 m höher, Längen in mm.
            Raumgrundriss p = Einziger("ifc4_koerper_platzierung.ifc");
            Assert.Equal("5000,6000;8000,6000;8000,10000;5000,10000", p.RingeText);
            Assert.Equal(3.0, p.BodenM, 9);
            Assert.Equal(3.0, p.GeschossLageM.Value, 9);
            Assert.DoesNotContain(Grundrissvermerk.Geschosslage, p.Vermerke);
        }

        [Fact]
        public void Beschnitt_traegt_den_Grundriss_mit_Vermerk_Dachschraege()
        {
            Raumgrundriss g = Einziger("ifc4_koerper_beschnitt.ifc");
            Assert.Equal(Umrissherleitung.KoerperBoden, g.Herleitung);
            Assert.Equal("1000,2000;5000,2000;5000,5000;1000,5000", g.RingeText);
            Assert.Equal(3.0, g.HoeheM, 9);                     // erster Operand — der Beschnitt auf 2,5 m fehlt
            Assert.Contains(Grundrissvermerk.Dachschraege, g.Vermerke);
        }

        [Fact]
        public void Nachbarn_tragen_mehrere_Raeume_auf_zwei_Geschossen()
        {
            foreach (string datei in new[] { "ifc4_koerper_nachbarn.ifc", "ifc4_koerper_nachbarn_grenzen.ifc", "ifc4_koerper_bauteile.ifc" })
            {
                IReadOnlyList<Raumgrundriss> g = Grundrisse(datei);
                Assert.True(g.Count >= 2, datei);
                Assert.All(g, x => Assert.True(x.RingflaecheM2 > 0, datei));
                Assert.Equal(g.Count, g.Select(x => x.Quellkennung).Distinct().Count());
            }
            IReadOnlyList<Raumgrundriss> n = Grundrisse("ifc4_koerper_nachbarn_grenzen.ifc");
            Assert.Equal(3, n.Count);
            Assert.Equal(new[] { 0.0, 0.0, 3.3 }, n.Select(x => Math.Round(x.BodenM, 6)));
            // F6: Wo der Umriss aus Raumgrenzen kommt, wird er gespeichert (Boden/Decke), nicht neu abgeleitet; der Flur
            // ohne Boden- und Deckengrenze nimmt den Körper.
            Assert.Equal(Umrissherleitung.Decke, n[0].Herleitung);
            Assert.Equal(Umrissherleitung.KoerperBoden, n[1].Herleitung);
            Assert.Equal(Umrissherleitung.Boden, n[2].Herleitung);
            Assert.Equal(Geometrieherkunft.Raumgrenzen, n[2].Herkunft);
            Assert.Equal("5240,0;8240,0;8240,4000;5240,4000", n[1].RingeText);
        }

        [Theory]
        [InlineData("ifc4_koerper_advancedbrep.ifc")]
        [InlineData("ifc4_z6_cad.ifc")]
        [InlineData("ifc4_z6_sollwerte.ifc")]
        public void Ohne_Koerper_kein_Grundriss_aus_dem_Koerper_Stufe_4(string datei)
        {
            Assert.DoesNotContain(GebaeudeRaumgrundrisse.Bilden(Lesen(datei), 0), x => x.Herkunft == Geometrieherkunft.Dateikoerper);
        }

        [Fact]
        public void Neue_Probe_Stufe_traegt_L_Form_mit_Vermerk_Stufen_und_Flaeche()
        {
            Raumgrundriss g = Einziger("ifc4_koerper_grundriss_stufe.ifc");
            Assert.Equal(Umrissherleitung.KoerperBoden, g.Herleitung);
            Assert.Equal("1000,2000;5000,2000;5000,5000;3000,5000;3000,8000;1000,8000", g.RingeText);
            Assert.Equal(18.0, g.RingflaecheM2, 9);
            Assert.Equal(0.0, g.BodenM, 9);
            Assert.Equal(3.0, g.HoeheM, 9);
            Assert.Equal(0.2, g.Abweichung.Value, 9);
            Assert.Equal(new[] { Grundrissvermerk.Stufen, Grundrissvermerk.Flaeche }, g.Vermerke);
        }

        [Fact]
        public void Neue_Probe_ohne_Boden_traegt_KoerperDecke()
        {
            Raumgrundriss g = Einziger("ifc4_koerper_grundriss_ohne_boden.ifc");
            Assert.Equal(Umrissherleitung.KoerperDecke, g.Herleitung);
            Assert.Equal("1000,2000;5000,2000;5000,5000;1000,5000", g.RingeText);
            Assert.Equal(12.0, g.RingflaecheM2, 9);
            Assert.Equal(0.0, g.Abweichung.Value, 9);
            Assert.Empty(g.Vermerke);
        }

        [Fact]
        public void Probe_25_Determinismus_zweimal_gelesen_ist_gleich()
        {
            foreach (string datei in IfcProbenErzeuger.Raumkoerperproben().Keys)
            {
                string Lauf() => string.Join("\n", Grundrisse(datei).Select(x => x + " " + x.RingeText + " "
                                                                               + x.BodenM.ToString("R", CultureInfo.InvariantCulture) + " "
                                                                               + x.HoeheM.ToString("R", CultureInfo.InvariantCulture)));
                Assert.Equal(Lauf(), Lauf());
            }
        }

        // ==================================================================
        //  Synthetische Körper: Orientierung, Rückfälle, Vermerke
        // ==================================================================

        [Fact]
        public void Nach_innen_gerichtete_Schale_findet_den_Boden_ueber_das_Volumenvorzeichen()
        {
            Koerpergrundriss aussen = Koerpergrundriss.Ableiten(Koerper(Quader(0, 0, 0, 4, 3, 2.5)));
            Koerpergrundriss innen = Koerpergrundriss.Ableiten(Koerper(Gekehrt(Quader(0, 0, 0, 4, 3, 2.5))));
            Assert.Equal(Umrissherleitung.KoerperBoden, innen.Herleitung);
            Assert.Equal(aussen.Text(), innen.Text());
            Assert.Equal("0,0;4000,0;4000,3000;0,3000", Raumgrundriss.RingeSchreiben(innen.Ringe));
        }

        [Fact]
        public void Nur_senkrechte_Flaechen_fallen_auf_die_konvexe_Huelle()
        {
            double[][][] q = Quader(0, 0, 0, 4, 3, 2.5);
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(q.Skip(2), offen: true));
            Assert.Equal(Umrissherleitung.KoerperHuelle, k.Herleitung);
            Assert.Equal(new[] { Grundrissvermerk.Konvex }, k.Vermerke);
            Assert.Equal("0,0;4000,0;4000,3000;0,3000", Raumgrundriss.RingeSchreiben(k.Ringe));
        }

        [Fact]
        public void Ohne_Koerper_oder_ohne_Flaeche_kein_Grundriss()
        {
            Assert.False(Koerpergrundriss.Ableiten(null).Traegt);
            Assert.False(Koerpergrundriss.Ableiten(new Dateikoerper()).Traegt);
            // Eine senkrechte Scheibe: keine Fläche im Grundriss, auch keine Hülle.
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(new[] { new[] { new[] { 0.0, 0, 0 }, new[] { 4.0, 0, 0 }, new[] { 4.0, 0, 3 }, new[] { 0.0, 0, 3 } } }, offen: true));
            Assert.False(k.Traegt);
            Assert.Null(k.Herleitung);
        }

        [Fact]
        public void Doppelte_Bodenflaeche_bleibt_einmal_mit_Vermerk_Ueberlappung()
        {
            double[][][] q = Quader(0, 0, 0, 4, 3, 2.5);
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(q.Prepend(q[0])));
            Assert.Equal(Umrissherleitung.KoerperBoden, k.Herleitung);
            Assert.Contains(Grundrissvermerk.Ueberlappung, k.Vermerke);
            Assert.Equal(12.0, k.RingflaecheM2, 9);
        }

        [Fact]
        public void Ein_Unterzug_im_Raum_doppelt_den_Grundriss_nicht()
        {
            // Quader 4 × 3 × 3 m, dazu die Unterseite eines Unterzugs (1 × 1 m auf 2,6 m, Normale nach unten).
            double[][][] q = Quader(0, 0, 0, 4, 3, 3);
            double[][] unterzug = { new[] { 1.0, 1, 2.6 }, new[] { 1.0, 2, 2.6 }, new[] { 2.0, 2, 2.6 }, new[] { 2.0, 1, 2.6 } };
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(q.Append(unterzug)));
            Assert.Single(k.Ringe);
            Assert.Equal(12.0, k.RingflaecheM2, 9);
            Assert.Contains(Grundrissvermerk.Ueberlappung, k.Vermerke);
        }

        [Fact]
        public void Splitter_unter_0_01_m2_entfallen_mit_Vermerk()
        {
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(Quader(0, 0, 0, 4, 3, 2.5).Concat(Quader(10, 10, 0, 10.05, 10.05, 2.5))));
            Assert.Single(k.Ringe);
            Assert.Contains(Grundrissvermerk.Splitter, k.Vermerke);
        }

        [Fact]
        public void Zwei_Raumteile_ergeben_zwei_Aussenringe_in_fester_Reihenfolge()
        {
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(Quader(10, 0, 0, 12, 2, 3).Concat(Quader(0, 0, 0, 2, 2, 3))));
            Assert.Equal("0,0;2000,0;2000,2000;0,2000|10000,0;12000,0;12000,2000;10000,2000", Raumgrundriss.RingeSchreiben(k.Ringe));
            Assert.Equal(8.0, k.RingflaecheM2, 9);
        }

        [Fact]
        public void Georeferenzierte_Lage_traegt_ganze_Millimeter_ohne_Verlust()
        {
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(Quader(3_500_000.123, 5_600_000.456, 120.0, 3_500_004.123, 5_600_003.456, 122.5)));
            Assert.Equal("3500000123,5600000456;3500004123,5600000456;3500004123,5600003456;3500000123,5600003456",
                         Raumgrundriss.RingeSchreiben(k.Ringe));
            Assert.Equal(12.0, k.RingflaecheM2, 9);
            Assert.Equal(120.0, k.BodenM, 9);
        }

        [Fact]
        public void Kollineare_Punkte_unter_1_mm_entfallen()
        {
            // Boden mit einem Zwischenpunkt 0,4 mm neben der Südkante (offene Schale): er entfällt.
            double[][] boden = { new[] { 2.0, -0.0004, 0 }, new[] { 0.0, 0, 0 }, new[] { 0.0, 3, 0 }, new[] { 4.0, 3, 0 }, new[] { 4.0, 0, 0 } };
            Koerpergrundriss k = Koerpergrundriss.Ableiten(Koerper(new[] { boden }, offen: true));
            Assert.Equal(Umrissherleitung.KoerperBoden, k.Herleitung);
            Assert.Equal("0,0;4000,0;4000,3000;0,3000", Raumgrundriss.RingeSchreiben(k.Ringe));
        }

        [Fact]
        public void Dachschraege_ab_10_Prozent_ueber_V_durch_A_und_Geschosslage_ab_0_5_m()
        {
            Grundrissring ring = Koerpergrundriss.Ableiten(Koerper(Quader(0, 0, 0, 4, 3, 2.5))).Ringe.Single();
            Raumgrundriss knapp = Raumgrundriss.Bilden("k", null, null, 0.4, 12, 36, Umrissherleitung.KoerperBoden,
                                                       new[] { ring }, 0, 3.29, null, false);
            Assert.Empty(knapp.Vermerke);
            Raumgrundriss hoch = Raumgrundriss.Bilden("k", null, null, 0.6, 12, 36, Umrissherleitung.KoerperBoden,
                                                      new[] { ring }, 0, 3.31, null, false);
            Assert.Equal(new[] { Grundrissvermerk.Dachschraege, Grundrissvermerk.Geschosslage }, hoch.Vermerke);
            Assert.Null(Raumgrundriss.Bilden("k", null, null, null, null, null, Umrissherleitung.KoerperBoden, new[] { ring }, 0, 3.0, null, false).Abweichung);
            Assert.Null(Raumgrundriss.Bilden("", null, null, null, 12, 36, Umrissherleitung.KoerperBoden, new[] { ring }, 0, 3.0, null, false));
            Assert.Null(Raumgrundriss.Bilden("k", null, null, null, 12, 36, Umrissherleitung.KoerperBoden, new[] { ring }, 0, 0.0, null, false));
        }

        [Fact]
        public void Textform_der_Ringe_und_Vermerke_im_Rundlauf()
        {
            Raumgrundriss g = Einziger("ifc4_koerper_extrusion_loch.ifc");
            IReadOnlyList<Grundrissring> zurueck = Raumgrundriss.RingeLesen(g.RingeText);
            Assert.Equal(g.RingeText, Raumgrundriss.RingeSchreiben(zurueck));
            Assert.Equal(new[] { false, true }, zurueck.Select(r => r.IstLoch));
            Assert.Empty(Raumgrundriss.RingeLesen("1,2;3"));
            Assert.Empty(Raumgrundriss.RingeLesen("1,2;3,4"));
            Assert.Equal(new[] { Grundrissvermerk.Stufen, Grundrissvermerk.Flaeche }, Raumgrundriss.VermerkeLesen("Flaeche,Stufen,Unbekannt,3"));
            // Die Textform hält den CHECK der Tabelle: nur Ziffern, Komma, Semikolon, Strich und Minus.
            Assert.Matches("^[0-9,;|-]+$", g.RingeText);
        }

        // ==================================================================
        //  Diagnose
        // ==================================================================

        [Fact]
        public void Diagnose_aller_Raumkoerperproben()
        {
            foreach (string datei in IfcProbenErzeuger.Raumkoerperproben().Keys)
            {
                GebaeudeAbbild a = Lesen(datei);
                foreach (AbbildRaum r in a.Gebaeude[0].Raeume)
                {
                    Raumgrundriss g = GebaeudeRaumgrundrisse.Bilden(a, 0).FirstOrDefault(x => x.Quellkennung == Quellkennung.Kuerzen(r.Kennung));
                    _aus.WriteLine(datei + " | " + r.Name + " | " + (g == null ? "kein Grundriss" :
                        g.Herleitung + " Boden " + g.BodenM.ToString("R", CultureInfo.InvariantCulture) + " Hoehe " +
                        g.HoeheM.ToString("R", CultureInfo.InvariantCulture) + " A " + g.RingflaecheM2.ToString("R", CultureInfo.InvariantCulture) +
                        " Abw " + g.Abweichung?.ToString("0.###", CultureInfo.InvariantCulture) + " V " + g.VermerkeText + " R " + g.RingeText));
                }
            }
        }
    }
}
