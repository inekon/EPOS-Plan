using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe K1 — der formatfreie Körperbildner</b> (Datenaustauschkonzept 17.2 bis 17.5, Probe 39): Raumprisma aus
    /// Polygonen mit Höhen je Punkt (Quader, L-Grundriss, Loch, Pultdach, zwei Polygone je Raum), Hüllprüfung mit
    /// T-Teilung und Ausrichtung über die Kantenpaare, Extrusion einer Fläche mit Loch und Laibung in drei Richtungen,
    /// Quellfläche je Dreieck, Determinismus — und die IFC-Körper byteweise wie vor dem Herausziehen des Ohrenschnitts.
    /// Ohne Datenbank.
    /// </summary>
    public sealed class KoerperbildnerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Körperproben, deren Raum- und Bauteilkörper vor und nach dem Herausziehen des Ohrenschnitts byteweise gleich sind.</summary>
        private static readonly string[] IFC_PROBEN =
        {
            "ifc2x3_koerper_brep.ifc", "ifc4_koerper_abgebildet.ifc", "ifc4_koerper_bauteile.ifc", "ifc4_koerper_beschnitt.ifc",
            "ifc4_koerper_dreiecksnetz.ifc", "ifc4_koerper_extrusion_bogen.ifc", "ifc4_koerper_extrusion_loch.ifc",
            "ifc4_koerper_extrusion_polygon.ifc", "ifc4_koerper_offen.ifc", "ifc4_koerper_platzierung.ifc", "ifc4_koerper_vieleckssatz.ifc",
        };

        /// <summary>SHA-256 der Körpertexte aller <see cref="IFC_PROBEN"/>, aufgenommen vor dem Herausziehen.</summary>
        private const string IFC_KOERPER_SHA256 = "8545A70009FFE85EE58ADBEE9EA5FCDB8B1225B69D46B1E116228597E8EE6C4E";

        [Fact]
        public void Ifc_Koerper_bleiben_nach_dem_Herausziehen_des_Ohrenschnitts_byte_gleich()
        {
            var t = new StringBuilder();
            foreach (string datei in IFC_PROBEN)
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                t.Append("# ").Append(datei).Append('\n');
                foreach (AbbildGebaeude g in a.Abbild.Gebaeude)
                {
                    foreach (var r in g.Raeume) t.Append("R ").Append(r.Name).Append('\n').Append(r.Koerper?.Text());
                    foreach (AbbildBauteil b in g.Bauteile)
                    {
                        t.Append("B ").Append(b.Name).Append('\n').Append(b.Koerper?.Text());
                        foreach (AbbildBauteil o in b.Oeffnungen) t.Append("O ").Append(o.Name).Append('\n').Append(o.Koerper?.Text());
                    }
                }
            }
            byte[] bytes = Encoding.UTF8.GetBytes(t.ToString());
            Assert.True(bytes.Length > 10_000, "zu wenig Körpertext: " + bytes.Length);
            string h = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.True(h == IFC_KOERPER_SHA256, "SHA-256 der Körpertexte: " + h);
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static double[] P(double x, double y, double z) => new[] { x, y, z };

        private static double[] Q(double x, double y) => new[] { x, y };

        /// <summary>Das Volumen über den Divergenzsatz: positiv, wenn die Normalen nach außen zeigen.</summary>
        private static double Volumen(Dateikoerper k)
            => k.Dreiecke.Sum(d =>
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                return (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;
            });

        private static double Oberflaeche(Dateikoerper k)
            => k.Dreiecke.Sum(d =>
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double[] u = { b[0] - a[0], b[1] - a[1], b[2] - a[2] }, v = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                double x = u[1] * v[2] - u[2] * v[1], y = u[2] * v[0] - u[0] * v[2], z = u[0] * v[1] - u[1] * v[0];
                return Math.Sqrt(x * x + y * y + z * z) / 2.0;
            });

        /// <summary>
        /// Geschlossen und gleichsinnig: Jede gerichtete Kante kommt genau einmal vor und ihre Gegenkante auch; dazu ein
        /// positives Volumen — dann zeigen alle Normalen nach außen, auch bei nichtkonvexen Körpern.
        /// </summary>
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
            foreach (double[] n in k.Normalen) Assert.Equal(1.0, Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]), 5);
            Assert.Equal(Koerperquelle.AusFlaechen, k.Quelle);
            Assert.Equal(k.DreieckZahl, k.Quellflaechen.Count);
            Assert.True(k.DreieckZahl <= Dateikoerper.DREIECKSGRENZE);
        }

        private static Raumpolygon Rechteckraum(string kennung, double x0, double y0, double x1, double y1, double boden, double decke, bool imUhrzeiger = false)
        {
            var punkte = new List<double[]> { Q(x0, y0), Q(x1, y0), Q(x1, y1), Q(x0, y1) };
            if (imUhrzeiger) punkte.Reverse();
            return new Raumpolygon { Kennung = kennung, Aussen = Raumring.Eben(punkte, boden, decke) };
        }

        private static Quellflaeche Flaeche(string kennung, params double[][] punkte) => new Quellflaeche { Kennung = kennung, Aussen = punkte };

        /// <summary>Die sechs Flächen eines Quaders [0,a]×[0,b]×[0,c], jede zweite gegen den Umlauf der übrigen.</summary>
        private static List<Quellflaeche> Quaderflaechen(double a, double b, double c)
            => new List<Quellflaeche>
            {
                Flaeche("Boden", P(0, 0, 0), P(a, 0, 0), P(a, b, 0), P(0, b, 0)),
                Flaeche("Decke", P(0, 0, c), P(a, 0, c), P(a, b, c), P(0, b, c)),
                Flaeche("Sued", P(0, 0, 0), P(a, 0, 0), P(a, 0, c), P(0, 0, c)),
                Flaeche("Nord", P(0, b, c), P(a, b, c), P(a, b, 0), P(0, b, 0)),
                Flaeche("West", P(0, 0, 0), P(0, b, 0), P(0, b, c), P(0, 0, c)),
                Flaeche("Ost", P(a, b, 0), P(a, 0, 0), P(a, 0, c), P(a, b, c)),
            };

        // ==================================================================
        //  Probe 39 — Raumprisma
        // ==================================================================

        [Fact]
        public void Probe39_Quader_aus_dem_Raumpolygon_geschlossen_mit_Volumen_und_Quellflaechen()
        {
            Koerperergebnis e = Koerperbildner.Raumprisma(new[] { Rechteckraum("R1", 0, 0, 4, 3, 0, 2.5) });
            Assert.True(e.Gebildet, e.ToString());
            Dateikoerper k = e.Koerper;
            GeschlossenNachAussen(k);
            Assert.Equal(12, k.DreieckZahl);
            Assert.Equal(8, k.PunkteM.Count);
            Assert.Equal(30.0, Volumen(k), 6);
            Assert.Equal(2 * 12.0 + 14.0 * 2.5, Oberflaeche(k), 6);
            Assert.Equal(Koerperbildner.ART_RAUMPOLYGON, k.Art);
            Assert.Empty(k.Vermerke);
            Assert.Equal(new[] { "R1:Boden", "R1:Decke", "R1:Kante0.0", "R1:Kante0.1", "R1:Kante0.2", "R1:Kante0.3" },
                         k.Quellflaechen.Distinct().ToArray());
            Assert.Equal(2, k.Quellflaechen.Count(q => q == "R1:Boden"));
            // Die Bodendreiecke zeigen nach unten, die Deckendreiecke nach oben.
            for (int t = 0; t < k.DreieckZahl; t++)
            {
                if (k.Quellflaechen[t] == "R1:Boden") Assert.Equal(-1.0, k.Normalen[t][2], 9);
                if (k.Quellflaechen[t] == "R1:Decke") Assert.Equal(1.0, k.Normalen[t][2], 9);
            }
            Assert.Equal(12, k.Randkanten.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Probe39_L_Grundriss_nichtkonvex_in_beiden_Umlaufrichtungen(bool imUhrzeiger)
        {
            var punkte = new List<double[]> { Q(0, 0), Q(6, 0), Q(6, 2), Q(2, 2), Q(2, 5), Q(0, 5) };
            if (imUhrzeiger) punkte.Reverse();
            Koerperergebnis e = Koerperbildner.Raumprisma(new[] { new Raumpolygon { Kennung = "L", Aussen = Raumring.Eben(punkte, 1.0, 4.0) } });
            Assert.True(e.Gebildet, e.ToString());
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal((6 * 2 + 2 * 3) * 3.0, Volumen(e.Koerper), 6);
            Assert.Equal(2 * 18.0 + 22.0 * 3.0, Oberflaeche(e.Koerper), 6);
            Assert.Equal(2 * 4 + 6 * 2, e.Koerper.DreieckZahl);
        }

        [Fact]
        public void Probe39_Grundriss_mit_Loch_ergibt_Innenmantel()
        {
            var loch = Raumring.Eben(new[] { Q(2, 2), Q(4, 2), Q(4, 4), Q(2, 4) }, 0.0, 3.0);
            var raum = new Raumpolygon { Kennung = "Ring", Aussen = Raumring.Eben(new[] { Q(0, 0), Q(6, 0), Q(6, 6), Q(0, 6) }, 0.0, 3.0), Loecher = new[] { loch } };
            Koerperergebnis e = Koerperbildner.Raumprisma(new[] { raum });
            Assert.True(e.Gebildet, e.ToString());
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal(32.0 * 3.0, Volumen(e.Koerper), 6);
            Assert.Equal(2 * 32.0 + 24.0 * 3.0 + 8.0 * 3.0, Oberflaeche(e.Koerper), 6);
            Assert.Equal(8, e.Koerper.Quellflaechen.Count(q => q.StartsWith("Ring:Kante1.", StringComparison.Ordinal)));
            Assert.DoesNotContain(Koerpervermerk.Loch, e.Koerper.Vermerke);
        }

        [Fact]
        public void Probe39_Pultdach_mit_Hoehen_je_Punkt_und_aus_Kantenhoehen()
        {
            // 4 × 5 m, Decke 2,5 m an der Südkante, 3,5 m an der Nordkante: Volumen 20 · 3 m.
            double[][] grund = { Q(0, 0), Q(4, 0), Q(4, 5), Q(0, 5) };
            var jePunkt = new Raumring { Punkte = grund, Boden = new[] { 0.0, 0.0, 0.0, 0.0 }, Decke = new[] { 2.5, 2.5, 3.5, 3.5 } };
            Koerperergebnis e = Koerperbildner.Raumprisma(new[] { new Raumpolygon { Kennung = "Pult", Aussen = jePunkt } });
            Assert.True(e.Gebildet, e.ToString());
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal(60.0, Volumen(e.Koerper), 6);
            Assert.Empty(e.Koerper.Vermerke);
            double schraeg = Math.Sqrt(25.0 + 1.0) * 4.0;
            Assert.Equal(20.0 + schraeg + 4 * 2.5 + 2 * 5 * 3.0 + 4 * 3.5, Oberflaeche(e.Koerper), 6);
            double[] n = e.Koerper.Normalen[e.Koerper.Quellflaechen.ToList().IndexOf("Pult:Decke")];
            Assert.True(n[1] < 0.0 && n[2] > 0.0, "Dachnormale nach oben und Süden");

            // Dieselben Höhen je Kante (Boden Anfang, Boden Ende, Decke Anfang, Decke Ende) ergeben denselben Körper.
            var kanten = new[] { new[] { 0, 0, 2.5, 2.5 }, new[] { 0, 0, 2.5, 3.5 }, new[] { 0, 0, 3.5, 3.5 }, new[] { 0, 0, 3.5, 2.5 } };
            Raumring ausKanten = Raumring.AusKantenhoehen(grund, kanten, Koerperbildner.TOLERANZ_M, out int unstetig);
            Assert.Equal(-1, unstetig);
            Koerperergebnis e2 = Koerperbildner.Raumprisma(new[] { new Raumpolygon { Kennung = "Pult", Aussen = ausKanten } });
            Assert.Equal(e.Koerper.Text(), e2.Koerper.Text());

            // Ein Sprung der Deckenhöhe an einer Ecke ist kein Raumring.
            kanten[1] = new[] { 0, 0, 2.6, 3.5 };
            Assert.Null(Raumring.AusKantenhoehen(grund, kanten, Koerperbildner.TOLERANZ_M, out unstetig));
            Assert.Equal(1, unstetig);
        }

        [Fact]
        public void Probe39_Zwei_Polygone_je_Raum_ergeben_einen_Koerper_aus_zwei_Bestandteilen()
        {
            Koerperergebnis e = Koerperbildner.Raumprisma(new[]
            {
                Rechteckraum("A", 0, 0, 2, 2, 0, 2.5),
                Rechteckraum("B", 5, 0, 8, 2, 0, 2.5, imUhrzeiger: true),
            });
            Assert.True(e.Gebildet, e.ToString());
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal((4.0 + 6.0) * 2.5, Volumen(e.Koerper), 6);
            Assert.Equal(24, e.Koerper.DreieckZahl);
            Assert.Contains("B:Decke", e.Koerper.Quellflaechen);
        }

        [Fact]
        public void Probe39_Raumprisma_ohne_passende_Hoehen_ist_ein_benannter_Fehlschlag()
        {
            var r = new Raumring { Punkte = new[] { Q(0, 0), Q(1, 0), Q(1, 1) }, Boden = new[] { 0.0, 0.0 }, Decke = new[] { 2.0, 2.0, 2.0 } };
            Koerperergebnis e = Koerperbildner.Raumprisma(new[] { new Raumpolygon { Aussen = r } });
            Assert.False(e.Gebildet);
            Assert.Equal(Koerperbildner.GRUND_HOEHEN, e.Grund);
            Assert.Equal(Koerperbildner.RUECKFALL_UMRISSPRISMA, e.Rueckfall);
            Assert.Equal(Koerperbildner.GRUND_LEER, Koerperbildner.Raumprisma(Array.Empty<Raumpolygon>()).Grund);
        }

        // ==================================================================
        //  Probe 39 — Hüllprüfung
        // ==================================================================

        [Fact]
        public void Probe39_Huelle_mit_gemischtem_Umlaufsinn_wird_nach_aussen_gerichtet()
        {
            Koerperergebnis e = Koerperbildner.Huelle(Quaderflaechen(2, 3, 4));
            Assert.True(e.Gebildet, e.ToString());
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal(24.0, Volumen(e.Koerper), 6);
            Assert.Equal(Koerperbildner.ART_HUELLFLAECHEN, e.Koerper.Art);
            // Die Südfläche (y = 0) zeigt nach Süden, die Ostfläche nach Osten — gleich, wie die Datei sie umläuft.
            for (int t = 0; t < e.Koerper.DreieckZahl; t++)
            {
                if (e.Koerper.Quellflaechen[t] == "Sued") Assert.Equal(-1.0, e.Koerper.Normalen[t][1], 9);
                if (e.Koerper.Quellflaechen[t] == "Ost") Assert.Equal(1.0, e.Koerper.Normalen[t][0], 9);
            }
            // Ganz gewendet bleibt es derselbe Körper.
            List<Quellflaeche> gewendet = Quaderflaechen(2, 3, 4).Select(f => Flaeche(f.Kennung, f.Aussen.Reverse().ToArray())).ToList();
            Assert.Equal(24.0, Volumen(Koerperbildner.Huelle(gewendet).Koerper), 6);
        }

        [Fact]
        public void Probe39_Offene_Huelle_ergibt_keinen_Koerper_sondern_den_Rueckfall_mit_Meldung()
        {
            List<Quellflaeche> fuenf = Quaderflaechen(2, 3, 4);
            fuenf.RemoveAt(1);   // ohne Decke
            Koerperergebnis e = Koerperbildner.Huelle(fuenf);
            Assert.False(e.Gebildet);
            Assert.Null(e.Koerper);
            Assert.Equal(Koerperbildner.GRUND_NICHT_GESCHLOSSEN, e.Grund);
            Assert.Equal("KOERPER_NICHT_GESCHLOSSEN", e.Grund);
            Assert.Equal(4, e.OffeneKanten);
            Assert.Equal(new[] { "4" }, e.Werte);
            Assert.Equal("Umrissprisma", e.Rueckfall);
        }

        [Fact]
        public void Probe39_T_Stoss_wird_geteilt_und_die_Huelle_schliesst()
        {
            // Quader 2 × 1 × 1; Decke und Boden in zwei Hälften, die Längswände ganz — vier T-Stöße an x = 1.
            var f = new List<Quellflaeche>
            {
                Flaeche("Boden1", P(0, 0, 0), P(1, 0, 0), P(1, 1, 0), P(0, 1, 0)),
                Flaeche("Boden2", P(1, 0, 0), P(2, 0, 0), P(2, 1, 0), P(1, 1, 0)),
                Flaeche("Decke1", P(0, 0, 1), P(0, 1, 1), P(1, 1, 1), P(1, 0, 1)),
                Flaeche("Decke2", P(1, 0, 1), P(2, 0, 1), P(2, 1, 1), P(1, 1, 1)),
                Flaeche("Sued", P(0, 0, 0), P(2, 0, 0), P(2, 0, 1), P(0, 0, 1)),
                Flaeche("Nord", P(0, 1, 0), P(2, 1, 0), P(2, 1, 1), P(0, 1, 1)),
                Flaeche("West", P(0, 0, 0), P(0, 1, 0), P(0, 1, 1), P(0, 0, 1)),
                Flaeche("Ost", P(2, 0, 0), P(2, 1, 0), P(2, 1, 1), P(2, 0, 1)),
            };
            Koerperergebnis e = Koerperbildner.Huelle(f);
            Assert.True(e.Gebildet, e.ToString());
            GeschlossenNachAussen(e.Koerper);
            Assert.Equal(2.0, Volumen(e.Koerper), 6);
            Assert.Equal(10.0, Oberflaeche(e.Koerper), 6);
            Assert.True(e.Koerper.Quellflaechen.Count(q => q == "Sued") > 2, "Die Südwand ist am T-Stoß geteilt");

            // Mit einem Versatz über der Toleranz schließt sie nicht.
            f[1] = Flaeche("Boden2", P(1.01, 0, 0), P(2, 0, 0), P(2, 1, 0), P(1.01, 1, 0));
            Assert.Equal(Koerperbildner.GRUND_NICHT_GESCHLOSSEN, Koerperbildner.Huelle(f).Grund);

            // Ein Versatz innerhalb der Toleranz (0,4 mm) wird zusammengelegt.
            f[1] = Flaeche("Boden2", P(1.0004, 0, 0), P(2, 0, 0), P(2, 1, 0), P(1.0004, 1, 0));
            Assert.True(Koerperbildner.Huelle(f).Gebildet);
        }

        // ==================================================================
        //  Probe 39 — Extrusion
        // ==================================================================

        /// <summary>Wand 4 × 3 m in der Ebene y = 0 mit Fenster 1,5 × 1,2 m; der Raum liegt bei y &gt; 0.</summary>
        private static Quellflaeche Wand(bool umgekehrt)
        {
            var aussen = new List<double[]> { P(0, 0, 0), P(4, 0, 0), P(4, 0, 3), P(0, 0, 3) };
            var loch = new List<double[]> { P(1, 0, 1), P(1, 0, 2.2), P(2.5, 0, 2.2), P(2.5, 0, 1) };
            if (umgekehrt) { aussen.Reverse(); loch.Reverse(); }
            return new Quellflaeche { Kennung = "Wand1", Aussen = aussen, Loecher = new[] { loch }, Lochkennungen = new[] { "Fenster1" } };
        }

        [Theory]
        [InlineData((int)Extrusionsrichtung.NachInnen, 0.0, 0.3, false)]
        [InlineData((int)Extrusionsrichtung.NachAussen, -0.3, 0.0, false)]
        [InlineData((int)Extrusionsrichtung.Beidseitig, -0.15, 0.15, false)]
        [InlineData((int)Extrusionsrichtung.NachInnen, 0.0, 0.3, true)]
        [InlineData((int)Extrusionsrichtung.Beidseitig, -0.15, 0.15, true)]
        public void Probe39_Extrusion_mit_Loch_und_Laibung_gegen_die_Handrechnung(int richtung, double yMin, double yMax, bool umgekehrt)
        {
            Koerperergebnis e = Koerperbildner.Extrusion(Wand(umgekehrt), 0.3, (Extrusionsrichtung)richtung, zumRaum: P(0, 1, 0));
            Assert.True(e.Gebildet, e.ToString());
            Dateikoerper k = e.Koerper;
            GeschlossenNachAussen(k);
            Assert.Equal(Koerperbildner.ART_FLAECHENEXTRUSION, k.Art);
            double netto = 12.0 - 1.5 * 1.2;
            Assert.Equal(netto * 0.3, Volumen(k), 6);
            Assert.Equal(2 * netto + 14.0 * 0.3 + 5.4 * 0.3, Oberflaeche(k), 6);
            Assert.Equal(yMin, k.PunkteM.Min(p => p[1]), 9);
            Assert.Equal(yMax, k.PunkteM.Max(p => p[1]), 9);
            // Die Laibung: vier Vierecke, acht Dreiecke mit der Kennung des Fensters, Normalen in die Öffnung.
            Assert.Equal(8, k.Quellflaechen.Count(q => q == "Fenster1"));
            Assert.Equal(k.DreieckZahl - 8, k.Quellflaechen.Count(q => q == "Wand1"));
            for (int t = 0; t < k.DreieckZahl; t++)
                if (k.Quellflaechen[t] == "Fenster1") Assert.Equal(0.0, k.Normalen[t][1], 9);
            Assert.Empty(k.Vermerke);
        }

        [Fact]
        public void Probe39_Extrusion_ohne_Raumseite_und_ohne_Dicke()
        {
            // Ohne Raumseite gilt die Gegenrichtung der Newell-Normale als Raum: Umlauf (0,0,0)→(4,0,0)→(4,0,3) ergibt n = (0,−1,0).
            Koerperergebnis e = Koerperbildner.Extrusion(Wand(false), 0.2, Extrusionsrichtung.NachInnen);
            Assert.True(e.Gebildet, e.ToString());
            Assert.Equal(0.2, e.Koerper.PunkteM.Max(p => p[1]), 9);
            Koerperergebnis null0 = Koerperbildner.Extrusion(Wand(false), 0.0, Extrusionsrichtung.Beidseitig);
            Assert.False(null0.Gebildet);
            Assert.Equal(Koerperbildner.GRUND_DICKE, null0.Grund);
        }

        // ==================================================================
        //  Determinismus und Herkunft
        // ==================================================================

        [Fact]
        public void Probe39_Gleiche_Eingabe_gleiche_Bytes_und_Quelle_im_Text()
        {
            string Lauf()
            {
                var t = new StringBuilder();
                t.Append(Koerperbildner.Raumprisma(new[] { Rechteckraum("R1", 0, 0, 4, 3, 0, 2.5), Rechteckraum("R2", 5, 0, 7, 3, 0, 2.5) }).Koerper.Text());
                t.Append(Koerperbildner.Huelle(Quaderflaechen(2, 3, 4)).Koerper.Text());
                t.Append(Koerperbildner.Extrusion(Wand(true), 0.3, Extrusionsrichtung.Beidseitig, P(0, 1, 0)).Koerper.Text());
                return t.ToString();
            }
            string a = Lauf(), b = Lauf();
            Assert.Equal(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
            Assert.Contains("\nQuelle AusFlaechen\n", a);
            Assert.Contains(" Q R1:Boden\n", a);
            Assert.Contains(" Q Fenster1\n", a);
        }

        [Fact]
        public void Ein_Koerper_aus_der_Datei_traegt_Quelle_Datei_und_keine_Quellflaechen()
        {
            var k = new Dateikoerper { Art = "FacetedBrep" };
            Assert.Equal(Koerperquelle.Datei, k.Quelle);
            Assert.Empty(k.Quellflaechen);
            Assert.DoesNotContain("Quelle", k.Text());
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "ifc4_koerper_extrusion_loch.ifc");
            var ablauf = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                ablauf.Lesen(s, pfad, new IfcImportProfil());
            Assert.All(ablauf.Abbild.Gebaeude.SelectMany(g => g.Raeume).Where(r => r.Koerper != null), r =>
            {
                Assert.Equal(Koerperquelle.Datei, r.Koerper.Quelle);
                Assert.Empty(r.Koerper.Quellflaechen);
            });
        }
    }
}
