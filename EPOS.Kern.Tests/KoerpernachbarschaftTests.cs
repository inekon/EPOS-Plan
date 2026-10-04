using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7f-4 — Flächenpaare aus Raumkörpern</b> (Mehrzonenkonzept 6.2, „Trennflächen aus Raumkörpern“): zwei
    /// Quader mit gemeinsamer Wand, versetzt, zu weit auseinander, übereinander, verdreht, zu klein; Determinismus und
    /// Laufzeit. Ohne Datenbank.
    /// </summary>
    public sealed class KoerpernachbarschaftTests
    {
        /// <summary>Ein Quader als Dateikörper: zwölf Dreiecke, Umlauf und Normale nach außen.</summary>
        internal static Dateikoerper Quader(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            var p = new List<double[]>
            {
                new[] { x0, y0, z0 }, new[] { x1, y0, z0 }, new[] { x1, y1, z0 }, new[] { x0, y1, z0 },
                new[] { x0, y0, z1 }, new[] { x1, y0, z1 }, new[] { x1, y1, z1 }, new[] { x0, y1, z1 },
            };
            // Je Seite ein Viereck gegen den Uhrzeigersinn von außen gesehen.
            int[][] seiten =
            {
                new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 },
                new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 },
            };
            var d = new List<int[]>();
            var n = new List<double[]>();
            foreach (int[] s in seiten)
            {
                d.Add(new[] { s[0], s[1], s[2] });
                d.Add(new[] { s[0], s[2], s[3] });
                double[] a = p[s[0]], b = p[s[1]], c = p[s[2]];
                double[] k = { (b[1] - a[1]) * (c[2] - a[2]) - (b[2] - a[2]) * (c[1] - a[1]),
                               (b[2] - a[2]) * (c[0] - a[0]) - (b[0] - a[0]) * (c[2] - a[2]),
                               (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]) };
                double l = Math.Sqrt(k[0] * k[0] + k[1] * k[1] + k[2] * k[2]);
                n.Add(new[] { k[0] / l, k[1] / l, k[2] / l });
                n.Add(new[] { k[0] / l, k[1] / l, k[2] / l });
            }
            return new Dateikoerper { PunkteM = p, Dreiecke = d, Normalen = n, Art = "Quader" };
        }

        /// <summary>Ein Körper um die z-Achse durch (cx, cy) gedreht.</summary>
        private static Dateikoerper Gedreht(Dateikoerper k, double grad, double cx, double cy)
        {
            double w = grad * Math.PI / 180.0, c = Math.Cos(w), s = Math.Sin(w);
            return new Dateikoerper
            {
                PunkteM = k.PunkteM.Select(p => new[] { cx + c * (p[0] - cx) - s * (p[1] - cy), cy + s * (p[0] - cx) + c * (p[1] - cy), p[2] }).ToList(),
                Dreiecke = k.Dreiecke, Normalen = k.Normalen, Art = k.Art,
            };
        }

        [Fact]
        public void Gemeinsame_Wand_ergibt_eine_Trennwand_mit_der_Ueberlappung()
        {
            var paare = Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 0, 5, 4, 3), Quader(5.24, 0, 0, 10, 4, 3) });
            Koerperpaar p = Assert.Single(paare);
            Assert.False(p.Decke);
            Assert.Equal(0, p.RaumA);
            Assert.Equal(1, p.RaumB);
            Assert.Equal(12.0, p.FlaecheM2, 6);
            Assert.Equal(0.24, p.AbstandM, 6);
            Assert.Equal(new[] { 1.0, 0.0, 0.0 }, p.NormaleA);
            Assert.Equal(new[] { 5.0, 2.0, 1.5 }, p.SchwerpunktA);
            Assert.Equal(new[] { 5.24, 2.0, 1.5 }, p.SchwerpunktB);
            Assert.Equal(4, p.RandA.Count);
            Assert.All(p.RandA, q => Assert.Equal(5.0, q[0], 6));
        }

        [Fact]
        public void Versetzte_Quader_teilen_eine_Teilflaeche()
        {
            var paare = Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 0, 5, 4, 3), Quader(5.2, 2.5, 1, 9, 8, 4) });
            Koerperpaar p = Assert.Single(paare);
            Assert.Equal(1.5 * 2.0, p.FlaecheM2, 6);
        }

        [Fact]
        public void Abstand_ueber_der_Trenndicke_ergibt_kein_Paar()
            => Assert.Empty(Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 0, 5, 4, 3), Quader(6.0, 0, 0, 10, 4, 3) }));

        [Fact]
        public void Uebereinander_ergibt_eine_Trenndecke_mit_oberem_Raum()
        {
            var paare = Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 3.3, 6, 5, 6), Quader(0, 0, 0, 6, 5, 3) });
            Koerperpaar p = Assert.Single(paare);
            Assert.True(p.Decke);
            Assert.Equal(0, p.Oben);
            Assert.Equal(30.0, p.FlaecheM2, 6);
            Assert.Equal(0.3, p.AbstandM, 6);
        }

        [Fact]
        public void Verdrehung_um_zwei_Grad_ergibt_kein_Paar_ein_halbes_Grad_schon()
        {
            Dateikoerper a = Quader(0, 0, 0, 5, 4, 3);
            Assert.Empty(Koerpernachbarschaft.Paare(new[] { a, Gedreht(Quader(5.2, 0, 0, 10, 4, 3), 2.0, 5.2, 2.0) }));
            Assert.Single(Koerpernachbarschaft.Paare(new[] { a, Gedreht(Quader(5.2, 0, 0, 10, 4, 3), 0.5, 5.2, 2.0) }));
        }

        [Fact]
        public void Kleine_Schnittflaeche_entfaellt()
        {
            // Ecke an Ecke: 0,25 m × 0,2 m = 0,05 m² < 0,1 m².
            Assert.Empty(Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 0, 5, 4, 3), Quader(5.2, 3.75, 2.8, 9, 8, 6) }));
        }

        [Fact]
        public void Rueckseiten_und_derselbe_Raum_ergeben_kein_Paar()
        {
            // Überlappende Körper (abgewandte Flächen) und ein flacher Raum (Boden und Decke 0,5 m) — keines ist ein Paar.
            Assert.Empty(Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 0, 5, 4, 3), Quader(4.6, 0, 0, 10, 4, 3) }));
            Assert.Empty(Koerpernachbarschaft.Paare(new[] { Quader(0, 0, 0, 5, 4, 0.5) }));
        }

        [Fact]
        public void Determinismus_zweimal_gleich()
        {
            var koerper = new List<Dateikoerper>();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 3; j++)
                    koerper.Add(Quader(i * 5.2, j * 4.2, 0, i * 5.2 + 5, j * 4.2 + 4, 3));
            string Text(List<Koerperpaar> l) => string.Join(";", l.Select(p => p.RaumA + "-" + p.RaumB + ":" + p.FlaecheM2.ToString("R")
                + ":" + string.Join(",", p.SchwerpunktA.Select(x => x.ToString("R")))));
            string a = Text(Koerpernachbarschaft.Paare(koerper)), b = Text(Koerpernachbarschaft.Paare(koerper));
            Assert.Equal(a, b);
            Assert.Equal(3 * 3 + 4 * 2, Koerpernachbarschaft.Paare(koerper).Count);
        }

        [Fact]
        public void Zweihundert_Raeume_unter_einer_Sekunde()
        {
            var koerper = new List<Dateikoerper>();
            for (int g = 0; g < 2; g++)
                for (int i = 0; i < 10; i++)
                    for (int j = 0; j < 10; j++)
                        koerper.Add(Quader(i * 4.2, j * 4.2, g * 3.3, i * 4.2 + 4, j * 4.2 + 4, g * 3.3 + 3));
            Koerpernachbarschaft.Paare(koerper.Take(4).ToList());   // Aufwärmen
            var uhr = Stopwatch.StartNew();
            List<Koerperpaar> paare = Koerpernachbarschaft.Paare(koerper);
            uhr.Stop();
            // Je Geschoss 2·10·9 Wände, dazu 100 Decken.
            Assert.Equal(2 * 180 + 100, paare.Count);
            Assert.True(uhr.ElapsedMilliseconds < 1000, uhr.ElapsedMilliseconds + " ms");
        }
    }
}
