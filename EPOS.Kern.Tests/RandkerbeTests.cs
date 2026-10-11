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
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Randkerbe</b> (Datenaustauschkonzept 17.3 Nr. 4): Eine Öffnung, die den Rand ihrer Wandfläche berührt oder
    /// überschreitet, wird als Kerbe aus der Fläche geschnitten — Laibung nur an den Kanten im Inneren der Wand,
    /// Zerfall in Teile, ganz außerhalb ohne Aussparung, Determinismus, Volumen und Oberfläche gegen die Handrechnung;
    /// dazu die Probe der Projektdatei mit Fenstertür, die gbXML-Probe und die Meldungen der Flächenklassifikation mit dem
    /// Präfix ihres Formats. Ohne Datenbank.
    /// </summary>
    public sealed class RandkerbeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private static double[] P(double x, double y, double z) => new[] { x, y, z };

        /// <summary>Wand 4 × 3 m in der Ebene y = 0 (x = 0 … 4, z = 0 … 3); der Raum liegt bei y &gt; 0.</summary>
        private static Quellflaeche Wand() => new Quellflaeche { Kennung = "Wand1", Aussen = new[] { P(0, 0, 0), P(4, 0, 0), P(4, 0, 3), P(0, 0, 3) } };

        /// <summary>Eine Öffnung als Rechteck in der Ebene y = 0.</summary>
        private static Wandoeffnung Oeffnung(string kennung, double x0, double x1, double z0, double z1)
            => new Wandoeffnung { Kennung = kennung, Ring = new[] { P(x0, 0, z0), P(x1, 0, z0), P(x1, 0, z1), P(x0, 0, z1) } };

        private static Koerperergebnis Bilden(out List<Aussparung> a, params Wandoeffnung[] o)
            => Koerperbildner.Extrusion(Wand(), o, 0.3, Extrusionsrichtung.NachInnen, P(0, 1, 0), out a);

        private static double Volumen(Dateikoerper k)
            => k.Dreiecke.Sum(d =>
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                return (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;
            });

        private static double Oberflaeche(Dateikoerper k, string quelle = null)
            => Enumerable.Range(0, k.DreieckZahl).Where(t => quelle == null || k.Quellflaechen[t] == quelle).Sum(t =>
            {
                int[] d = k.Dreiecke[t];
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double[] u = { b[0] - a[0], b[1] - a[1], b[2] - a[2] }, v = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                double x = u[1] * v[2] - u[2] * v[1], y = u[2] * v[0] - u[0] * v[2], z = u[0] * v[1] - u[1] * v[0];
                return Math.Sqrt(x * x + y * y + z * z) / 2.0;
            });

        /// <summary>Jede gerichtete Kante genau einmal und ihre Gegenkante auch; Volumen positiv.</summary>
        private static void GeschlossenNachAussen(Dateikoerper k)
        {
            var gerichtet = new Dictionary<(int, int), int>();
            foreach (int[] d in k.Dreiecke)
                for (int e = 0; e < 3; e++)
                {
                    (int, int) s = (d[e], d[(e + 1) % 3]);
                    gerichtet[s] = gerichtet.TryGetValue(s, out int z) ? z + 1 : 1;
                }
            foreach (KeyValuePair<(int, int), int> e in gerichtet)
            {
                Assert.True(e.Value == 1, "Kante doppelt gleichsinnig: " + e.Key);
                Assert.True(gerichtet.ContainsKey((e.Key.Item2, e.Key.Item1)), "Kante offen: " + e.Key);
            }
            Assert.True(Volumen(k) > 0.0, "Normalen zeigen nach innen");
            Assert.Equal(k.DreieckZahl, k.Quellflaechen.Count);
        }

        /// <summary>Die Laibung einer Öffnung: Fläche der Dreiecke mit ihrer Kennung, Normalen in der Wandebene (y = 0).</summary>
        private static double Laibung(Dateikoerper k, string kennung)
        {
            for (int t = 0; t < k.DreieckZahl; t++)
                if (k.Quellflaechen[t] == kennung) Assert.Equal(0.0, k.Normalen[t][1], 9);
            return Oberflaeche(k, kennung);
        }

        // ==================================================================
        //  Körperbildner
        // ==================================================================

        [Fact]
        public void Fenstertuer_bis_zum_Boden_wird_als_Kerbe_geschnitten_Laibung_an_drei_Kanten()
        {
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("Tuer1", 1.0, 2.0, 0.0, 2.1));
            Assert.True(e.Gebildet, e.ToString());
            Assert.Equal(Aussparungsart.Kerbe, Assert.Single(a).Art);
            Dateikoerper k = e.Koerper;
            GeschlossenNachAussen(k);
            // Fläche 12 − 2,1 = 9,9 m²; Umfang 14 − 1 (Türbreite am Boden) + 2,1 + 1 + 2,1 = 18,2 m.
            Assert.Equal(9.9 * 0.3, Volumen(k), 6);
            Assert.Equal(2 * 9.9 + 18.2 * 0.3, Oberflaeche(k), 6);
            // Laibung nur an den drei Kanten im Inneren der Wand: 2,1 + 1 + 2,1 = 5,2 m × 0,3 m.
            Assert.Equal(5.2 * 0.3, Laibung(k, "Tuer1"), 6);
            Assert.Equal(6, k.Quellflaechen.Count(q => q == "Tuer1"));
            // Am Boden keine Laibung: kein Dreieck der Tür liegt in z = 0.
            for (int t = 0; t < k.DreieckZahl; t++)
                if (k.Quellflaechen[t] == "Tuer1") Assert.True(k.Dreiecke[t].Any(i => k.PunkteM[i][2] > 1e-9));
            Assert.Equal(0.0, k.PunkteM.Min(p => p[1]), 9);
            Assert.Equal(0.3, k.PunkteM.Max(p => p[1]), 9);
        }

        [Fact]
        public void Fenstertuer_unter_den_Boden_ueberschreitet_den_Rand_und_wird_ebenso_geschnitten()
        {
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("Tuer1", 1.0, 2.0, -0.05, 2.1));
            Assert.True(e.Gebildet, e.ToString());
            Assert.Equal(Aussparungsart.Kerbe, Assert.Single(a).Art);
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal(9.9 * 0.3, Volumen(e.Koerper), 6);
            Assert.Equal(5.2 * 0.3, Laibung(e.Koerper, "Tuer1"), 6);
        }

        [Fact]
        public void Oeffnung_an_der_Wandkante_ueber_zwei_Raender()
        {
            // Ecke oben rechts, über beide Ränder hinaus: abgeschnitten 1 × 1 m.
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("Ecke1", 3.0, 4.5, 2.0, 3.5));
            Assert.True(e.Gebildet, e.ToString());
            Assert.Equal(Aussparungsart.Kerbe, Assert.Single(a).Art);
            GeschlossenNachAussen(e.Koerper);
            // L-Form: Fläche 11 m², Umfang unverändert 14 m; Laibung an zwei Kanten zu je 1 m.
            Assert.Equal(11.0 * 0.3, Volumen(e.Koerper), 6);
            Assert.Equal(2 * 11.0 + 14.0 * 0.3, Oberflaeche(e.Koerper), 6);
            Assert.Equal(2.0 * 0.3, Laibung(e.Koerper, "Ecke1"), 6);
            Assert.Equal(4, e.Koerper.Quellflaechen.Count(q => q == "Ecke1"));
        }

        [Fact]
        public void Oeffnung_ueber_die_ganze_Hoehe_teilt_die_Wand_in_zwei_Teile()
        {
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("Tor1", 1.5, 2.5, -0.1, 3.1));
            Assert.True(e.Gebildet, e.ToString());
            Assert.Equal(Aussparungsart.Kerbe, Assert.Single(a).Art);
            Dateikoerper k = e.Koerper;
            GeschlossenNachAussen(k);
            // Zwei Teile 1,5 × 3 m; Umfang je 9 m; Laibung je eine Kante von 3 m.
            Assert.Equal(9.0 * 0.3, Volumen(k), 6);
            Assert.Equal(2 * 9.0 + 18.0 * 0.3, Oberflaeche(k), 6);
            Assert.Equal(2 * 3.0 * 0.3, Laibung(k, "Tor1"), 6);
            // Zwei getrennte Bestandteile: links x ≤ 1,5, rechts x ≥ 2,5, kein Punkt dazwischen.
            Assert.DoesNotContain(k.PunkteM, p => p[0] > 1.5 + 1e-9 && p[0] < 2.5 - 1e-9);
            Assert.Contains(k.PunkteM, p => p[0] < 1e-9);
            Assert.Contains(k.PunkteM, p => p[0] > 4.0 - 1e-9);
        }

        [Fact]
        public void Oeffnung_ganz_ausserhalb_bleibt_ohne_Aussparung()
        {
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("Weit1", 5.0, 6.0, 1.0, 2.0));
            Assert.True(e.Gebildet, e.ToString());
            Aussparung x = Assert.Single(a);
            Assert.Equal(Aussparungsart.Keine, x.Art);
            Assert.Equal(Koerperbildner.GRUND_OEFFNUNG_AUSSERHALB, x.Grund);
            Assert.Equal(Koerperbildner.Extrusion(Wand(), 0.3, Extrusionsrichtung.NachInnen, P(0, 1, 0)).Koerper.Text(), e.Koerper.Text());
        }

        [Fact]
        public void Schmale_Oeffnung_am_Rand_ist_ohne_Flaeche_und_bleibt_ohne_Aussparung()
        {
            // 2,4 mm breit, an der Wandkante: im Mittel nicht breiter als zweimal die Kerbtoleranz.
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("Spalt1", 3.9976, 4.0, 1.0, 2.0));
            Aussparung x = Assert.Single(a);
            Assert.Equal(Aussparungsart.Keine, x.Art);
            Assert.Equal(Koerperbildner.GRUND_OEFFNUNG_LEER, x.Grund);
            Assert.Equal(Koerperbildner.Extrusion(Wand(), 0.3, Extrusionsrichtung.NachInnen, P(0, 1, 0)).Koerper.Text(), e.Koerper.Text());
        }

        [Fact]
        public void Ohne_Kerbe_ist_der_Koerper_derselbe_wie_mit_Loechern()
        {
            Koerperergebnis neu = Bilden(out List<Aussparung> a, Oeffnung("Fenster1", 1.0, 2.5, 1.0, 2.2));
            Assert.Equal(Aussparungsart.Loch, Assert.Single(a).Art);
            Quellflaeche mitLoch = Wand();
            mitLoch = new Quellflaeche
            {
                Kennung = mitLoch.Kennung, Aussen = mitLoch.Aussen,
                Loecher = new[] { Oeffnung("Fenster1", 1.0, 2.5, 1.0, 2.2).Ring }, Lochkennungen = new[] { "Fenster1" },
            };
            Koerperergebnis alt = Koerperbildner.Extrusion(mitLoch, 0.3, Extrusionsrichtung.NachInnen, P(0, 1, 0));
            Assert.Equal(alt.Koerper.Text(), neu.Koerper.Text());
        }

        [Fact]
        public void Kerbe_und_ueberlappende_Loecher_in_einer_Wand_deterministisch()
        {
            Wandoeffnung[] o =
            {
                Oeffnung("Tuer1", 0.5, 1.5, 0.0, 2.1),
                Oeffnung("Fenster1", 2.0, 3.5, 1.0, 2.2),
                Oeffnung("Doppelt1", 3.0, 3.8, 1.5, 2.5),   // überlappt Fenster1 auf 0,5 × 0,7 m
            };
            Koerperergebnis e1 = Bilden(out List<Aussparung> a, o);
            Koerperergebnis e2 = Bilden(out _, o.Reverse().Reverse().ToArray());
            Assert.True(e1.Gebildet, e1.ToString());
            Assert.Equal(new[] { Aussparungsart.Kerbe, Aussparungsart.Loch, Aussparungsart.Loch }, a.Select(x => x.Art));
            GeschlossenNachAussen(e1.Koerper);
            // Vereinigung der beiden Fenster: 1,8 + 0,8 − 0,35 = 2,25 m².
            Assert.Equal((12.0 - 2.1 - 2.25) * 0.3, Volumen(e1.Koerper), 6);
            Assert.Equal(e1.Koerper.Text(), e2.Koerper.Text());
            // Ein Umlauf im Uhrzeigersinn ändert die Fläche nicht.
            var umgekehrt = new Quellflaeche { Kennung = "Wand1", Aussen = Wand().Aussen.Reverse().ToList() };
            Koerperergebnis e3 = Koerperbildner.Extrusion(umgekehrt, o, 0.3, Extrusionsrichtung.NachInnen, P(0, 1, 0), out _);
            Assert.Equal(Volumen(e1.Koerper), Volumen(e3.Koerper), 9);
        }

        [Fact]
        public void Fensterband_aus_sich_beruehrenden_Fenstern_wird_gemeinsam_ausgespart()
        {
            Koerperergebnis e = Bilden(out List<Aussparung> a, Oeffnung("FensterA", 1.0, 2.0, 1.0, 2.0), Oeffnung("FensterB", 2.0, 3.0, 1.0, 2.0));
            Assert.True(e.Gebildet, e.ToString());
            Assert.Equal(new[] { Aussparungsart.Loch, Aussparungsart.Loch }, a.Select(x => x.Art));
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal(10.0 * 0.3, Volumen(e.Koerper), 6);
            // Die Laibung läuft um den gemeinsamen Umriss: je Fenster drei Kanten zu 1 m, an der Stoßkante keine.
            Assert.Equal(3.0 * 0.3, Laibung(e.Koerper, "FensterA"), 6);
            Assert.Equal(3.0 * 0.3, Laibung(e.Koerper, "FensterB"), 6);
            Assert.Equal(2 * 10.0 + (14.0 + 6.0) * 0.3, Oberflaeche(e.Koerper), 6);
        }

        [Fact]
        public void Nichtkonvexe_Giebelwand_mit_Kerbe_an_der_Traufe()
        {
            // Giebelwand 6 m breit, Traufe 3 m, First 5 m; Fenstertür am Boden und eine Öffnung, die die Dachschräge schneidet.
            var giebel = new Quellflaeche { Kennung = "Giebel", Aussen = new[] { P(0, 0, 0), P(6, 0, 0), P(6, 0, 3), P(3, 0, 5), P(0, 0, 3) } };
            Koerperergebnis e = Koerperbildner.Extrusion(giebel, new[] { Oeffnung("Tuer1", 1.0, 2.0, 0.0, 2.0), Oeffnung("Dach1", 4.0, 5.0, 3.0, 4.0) },
                                                         0.3, Extrusionsrichtung.Beidseitig, P(0, 1, 0), out List<Aussparung> a);
            Assert.True(e.Gebildet, e.ToString());
            Assert.All(a, x => Assert.Equal(Aussparungsart.Kerbe, x.Art));
            GeschlossenNachAussen(e.Koerper);
            // Fläche 18 + 6 = 24 m²; Tür 2 m². Dachöffnung: die Schräge z = 3 + 2/3 · (6 − x) liegt bis x = 4,5 über
            // z = 4 (volle Höhe 1 m auf 0,5 m Breite), danach fällt sie auf 3,667 bei x = 5: 0,5 · 1 + 0,5 · (1 + 2/3) / 2.
            double dach = 0.5 * 1.0 + 0.5 * (1.0 + 2.0 / 3.0) / 2.0;
            Assert.Equal((24.0 - 2.0 - dach) * 0.3, Volumen(e.Koerper), 6);
        }

        // ==================================================================
        //  Projektdatei und gbXML
        // ==================================================================

        private GebaeudeAbbild LesenSqproj(SqprojProbenErzeuger e)
        {
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad("randkerbe"));
            _dateien.Add(pfad);
            var profil = new SqprojImportProfil();
            using (FileStream f = File.OpenRead(pfad))
                return profil.LeserErzeugen().Lesen(f, profil, null, CancellationToken.None);
        }

        private static IEnumerable<AbbildBauteil> Alle(GebaeudeAbbild a)
        {
            List<AbbildBauteil> b = a.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.BauteileOhneGebaeude).Distinct().ToList();
            return b.Concat(b.SelectMany(x => x.Oeffnungen));
        }

        private static IEnumerable<PruefMeldung> Meldungen(GebaeudeAbbild a)
            => a.Meldungen.Concat(a.Gebaeude.SelectMany(g => g.Meldungen))
                .Concat(Alle(a).SelectMany(b => b.Meldungen));

        [Fact]
        public void Projektdatei_Fenstertuer_wird_als_Kerbe_ausgespart()
        {
            GebaeudeAbbild a = LesenSqproj(SqprojProbenErzeuger.Fenstertuerhaus());
            AbbildBauteil wand = Alle(a).First(b => b.Kennung == "AW");
            AbbildBauteil tuer = Alle(a).First(b => b.Kennung == "FT");
            Assert.NotNull(wand.Koerper);
            // Fenster (Loch, 1 m²) und Fenstertür (Kerbe, 2,1 m²) aus 11,05 m² Bruttofläche, Dicke 0,30 m.
            Assert.Equal((11.05 - 1.0 - 2.1) * 0.30, Koerpergrundriss.Volumen(wand.Koerper), 6);
            Assert.Contains("FT", wand.Koerper.Quellflaechen);
            Assert.Contains("FS", wand.Koerper.Quellflaechen);
            Assert.DoesNotContain(tuer.Meldungen, m => m.Schluessel == SqprojGeometrie.OEFFNUNG_RAND || m.Schluessel == SqprojGeometrie.OEFFNUNG_OHNE_WAND);
            Assert.NotNull(tuer.Koerper);
        }

        [Fact]
        public void Projektdatei_und_gbXML_melden_die_Klassifikation_mit_ihrem_Praefix()
        {
            List<PruefMeldung> sq = Meldungen(LesenSqproj(SqprojProbenErzeuger.Fenstertuerhaus())).ToList();
            Assert.DoesNotContain(sq, m => m.Schluessel.StartsWith(IfcImportProfil.MELDUNGSPRAEFIX, StringComparison.Ordinal));
            Assert.Contains(sq, m => m.Schluessel == SqprojImportProfil.MELDUNGSPRAEFIX + Flaechenklassifikation.NAME_OHNE_BAUTEIL);

            List<PruefMeldung> gb = Meldungen(LesenGbxml(XDocument.Load(GbxmlImportTests.Probe("gbxml_g5_closedshell.xml")))).ToList();
            Assert.DoesNotContain(gb, m => m.Schluessel.StartsWith(IfcImportProfil.MELDUNGSPRAEFIX, StringComparison.Ordinal));

            // Jeder Schlüssel hat einen Text in beiden Sprachen; der IFC-Weg bleibt bei seinem Schlüssel.
            foreach (string p in new[] { SqprojImportProfil.MELDUNGSPRAEFIX, GbxmlImportProfil.MELDUNGSPRAEFIX, IfcImportProfil.MELDUNGSPRAEFIX })
                foreach (string n in new[] { Flaechenklassifikation.NAME_OHNE_BAUTEIL, Flaechenklassifikation.NAME_ABWEICHUNG, "OEFFNUNG_RAND" })
                {
                    if (p == IfcImportProfil.MELDUNGSPRAEFIX && n == "OEFFNUNG_RAND") continue;
                    foreach (string kultur in new[] { "de-DE", "en-US" })
                    {
                        string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(p + n, new CultureInfo(kultur));
                        Assert.False(string.IsNullOrEmpty(text), p + n + " " + kultur);
                        Assert.DoesNotContain("IFC", text);
                    }
                }
            Assert.Equal("IMP_IFC_PROT_FLAECHE_OHNE_BAUTEIL", Flaechenklassifikation.MELDUNG_OHNE_BAUTEIL);
            Assert.Equal("IMP_IFC_PROT_FLAECHENGRUPPE_ABWEICHUNG", Flaechenklassifikation.MELDUNG_ABWEICHUNG);
            Assert.Equal(IfcImportProfil.MELDUNGSPRAEFIX, Flaechenklassifikation.Praefix(GebaeudeQuelle.FORMAT_IFC));
            Assert.Equal(GbxmlImportProfil.MELDUNGSPRAEFIX, Flaechenklassifikation.Praefix(GebaeudeQuelle.FORMAT_GBXML));
            Assert.Equal(SqprojImportProfil.MELDUNGSPRAEFIX, Flaechenklassifikation.Praefix(GebaeudeQuelle.FORMAT_SQPROJ));
        }

        private static GebaeudeAbbild LesenGbxml(XDocument d)
        {
            using (var s = new MemoryStream(Encoding.UTF8.GetBytes(d.ToString())))
                return new GbxmlLeser().Lesen(s, new GbxmlImportProfil(), null, CancellationToken.None);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-0.2)]
        public void GbXML_Fenstertuer_am_Boden_wird_als_Kerbe_ausgespart(double unten)
        {
            XDocument d = XDocument.Load(GbxmlImportTests.Probe("gbxml_g5_closedshell.xml"));
            XElement o = d.Descendants().Single(e => e.Name.LocalName == "Opening" && (string)e.Attribute("id") == "fenster-a-sued");
            foreach (XElement p in o.Descendants().Where(e => e.Name.LocalName == "CartesianPoint"))
            {
                XElement z = p.Elements().Last();
                if (z.Value == "1") z.Value = unten.ToString(CultureInfo.InvariantCulture);
            }
            GebaeudeAbbild a = LesenGbxml(d);
            AbbildBauteil w = a.Gebaeude.Single().Bauteile.Single(b => b.Kennung == "aw-a-sued");
            // Fläche 15 m² minus die Tür 1,5 × 2,2 m (im Inneren der Wand), Dicke 0,30 m.
            Assert.Equal((15.0 - 1.5 * 2.2) * 0.3, Koerpergrundriss.Volumen(w.Koerper), 6);
            Assert.Contains("fenster-a-sued", w.Koerper.Quellflaechen);
            AbbildBauteil f = Assert.Single(w.Oeffnungen);
            Assert.NotNull(f.Koerper);
            Assert.DoesNotContain(f.Meldungen, m => m.Schluessel.EndsWith("OEFFNUNG_OHNE_WAND", StringComparison.Ordinal)
                                                    || m.Schluessel.EndsWith("OEFFNUNG_RAND", StringComparison.Ordinal));
        }
    }
}
