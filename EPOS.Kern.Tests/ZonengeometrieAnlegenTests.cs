using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G7b — Aneinanderlegen im Zonengeometrie-Modell</b> (Datenaustauschkonzept 5.5 Punkt 3, 14.1;
    /// Mehrzonenkonzept M13): Zonen mit gemeinsamer Trennwand liegen deckungsgleich aneinander, gedreht in
    /// 90°-Schritten, wo nötig; die Gegenstücke sind wechselseitig nachgezogen; eine widersprüchliche Anordnung
    /// ist benannt abgelehnt; Probe 25 (Determinismus der Geometrie) am Modell. Ohne Datenbank.
    /// </summary>
    public sealed class ZonengeometrieAnlegenTests
    {
        private const double Genau = 1e-9;

        internal static Umrissseite Wand(string kennung, string bauteil, int? sektor, double flaeche,
                                         Randbedingung lage = Randbedingung.Aussenluft)
            => new Umrissseite
            {
                Verweis = new Grenzverweis(kennung, bauteil, Bauteilart.Aussenwand, Grenzstellung.Wand, lage, null),
                FlaecheM2 = flaeche, Sektor = sektor,
            };

        internal static Umrissseite Platte(string kennung, Grenzstellung stellung, double flaeche)
            => new Umrissseite
            {
                Verweis = new Grenzverweis(kennung, kennung, stellung == Grenzstellung.Boden ? Bauteilart.Bodenplatte : Bauteilart.Dach,
                                           stellung, Randbedingung.Aussenluft, null),
                FlaecheM2 = flaeche,
            };

        internal static Umrissraum Raum(string kennung, double flaeche, double volumen, params Umrissseite[] seiten)
        {
            var r = new Umrissraum { Kennung = kennung, Name = "Raum " + kennung, Zone = -1, Beheizt = true, FlaecheM2 = flaeche, VolumenM3 = volumen };
            r.Seiten.AddRange(seiten);
            return r;
        }

        /// <summary>
        /// A: 10 × 5 m (h = 2,5 m), Trennwand T an der Ostkante; B: 6 × 5 m, T an der West- (<paramref name="sektorB"/> = 3)
        /// oder einer anderen Kante.
        /// </summary>
        internal static Umrisseingang Zwei(int sektorB = 3, bool umgekehrt = false)
        {
            var e = new Umrisseingang();
            Umrissraum a = Raum("A", 50.0, 125.0,
                Wand("A-S", "A-S", 2, 25.0), Wand("A-N", "A-N", 0, 25.0), Wand("A-W", "A-W", 3, 12.5),
                Wand("T-A", "T", 1, 12.5, Randbedingung.Unbekannt), Platte("A-Boden", Grenzstellung.Boden, 50.0), Platte("A-Dach", Grenzstellung.Decke, 50.0));
            Umrissraum b = Raum("B", 30.0, 75.0,
                Wand("T-B", "T", sektorB, 12.5, Randbedingung.Unbekannt), Wand("B-O", "B-O", sektorB == 3 ? 1 : 3, 12.5),
                Wand("B-S", "B-S", 2, 15.0), Wand("B-N", "B-N", 0, 15.0), Platte("B-Boden", Grenzstellung.Boden, 30.0));
            if (umgekehrt)
            {
                b.Seiten.Reverse();
                a.Seiten.Reverse();
                e.Raeume.Add(b);
                e.Raeume.Add(a);
            }
            else
            {
                e.Raeume.Add(a);
                e.Raeume.Add(b);
            }
            return e;
        }

        /// <summary>Die Strecke einer Wand in Weltkoordinaten (Anfang, Ende) am Polygon eines Raums.</summary>
        internal static (double[] Von, double[] Bis) Strecke(Raumumriss r, string kennung)
        {
            Umrisspolygon p = Assert.Single(r.Polygone);
            foreach (Umrisskante k in p.Kanten)
                foreach (Kantenabschnitt x in k.Abschnitte)
                    if (x.Verweis.Kennung == kennung)
                    {
                        double[] a = p.Punkte[k.Index], b = p.Punkte[(k.Index + 1) % p.Punkte.Count];
                        double ux = (b[0] - a[0]) / k.LaengeM, uy = (b[1] - a[1]) / k.LaengeM;
                        return (new[] { a[0] + ux * x.VonM, a[1] + uy * x.VonM }, new[] { a[0] + ux * x.BisM, a[1] + uy * x.BisM });
                    }
            throw new InvalidOperationException("Keine Strecke " + kennung);
        }

        private static void Gleich(double[] x, double[] y, double genau = Genau)
        {
            Assert.Equal(x[0], y[0], genau);
            Assert.Equal(x[1], y[1], genau);
        }

        private static bool Ueberlappt(Raumumriss x, Raumumriss y)
        {
            double[] a = ZonengeometrieTests.Kasten(x.Polygone[0]), b = ZonengeometrieTests.Kasten(y.Polygone[0]);
            return Math.Min(a[2], b[2]) - Math.Max(a[0], b[0]) > 1e-6 && Math.Min(a[3], b[3]) - Math.Max(a[1], b[1]) > 1e-6;
        }

        [Fact]
        public void Zwei_Zonen_mit_Trennwand_liegen_deckungsgleich_aneinander_und_die_Gegenstuecke_sind_wechselseitig()
        {
            Zonengeometrie z = Zonengeometrie.AusFlaechen(Zwei());
            Raumumriss a = z.Raum("A"), b = z.Raum("B");
            Assert.True(a.Angelegt && b.Angelegt);
            Assert.Equal(0, a.DrehungGrad);
            Assert.Equal(0, b.DrehungGrad);
            Assert.False(Ueberlappt(a, b));

            // Die Trennwand: dieselbe Strecke von beiden Seiten, gegenläufig, an der Ostkante von A.
            (double[] a0, double[] a1) = Strecke(a, "T-A");
            (double[] b0, double[] b1) = Strecke(b, "T-B");
            Gleich(a0, b1);
            Gleich(a1, b0);
            double[] kastenA = ZonengeometrieTests.Kasten(a.Polygone[0]), kastenB = ZonengeometrieTests.Kasten(b.Polygone[0]);
            Assert.Equal(kastenA[2], a0[0], Genau);
            Assert.Equal(kastenA[2], kastenB[0], Genau);
            // Flächentreu: A 10 × 5, B 6 × 5 — die Trennwand füllt beide Kanten.
            Assert.Equal(50.0, a.PolygonflaecheM2, 1e-9);
            Assert.Equal(30.0, b.PolygonflaecheM2, 1e-9);
            Assert.Equal(5.0, Abstand(a0, a1), 1e-9);

            // Das Paar und die nachgezogenen Gegenstücke — an Kante, Strecke und Paar.
            Nachbarpaar paar = Assert.Single(z.Nachbarpaare);
            Assert.Equal(("A", "B", "T"), (paar.RaumA, paar.RaumB, paar.BauteilKennung));
            Assert.True(paar.Angelegt);
            Assert.False(paar.Abgelehnt);
            Assert.Equal("T-B", paar.VerweisA.GegenstueckKennung);
            Assert.Equal("T-A", paar.VerweisB.GegenstueckKennung);
            Grenzverweis ostA = Assert.Single(a.Polygone[0].Kanten[1].Grenzen);
            Grenzverweis westB = Assert.Single(b.Polygone[0].Kanten[3].Grenzen, v => v.BauteilKennung == "T");
            Assert.Equal("T-B", ostA.GegenstueckKennung);
            Assert.Equal("T-A", westB.GegenstueckKennung);
            Assert.False(z.AnordnungAbgelehnt);
            Assert.Contains(z.Meldungen, m => m.Schluessel == Zonengeometrie.ANGELEGT && m.Werte[0] == "1");
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel == Zonengeometrie.ANORDNUNG_ABGELEHNT);
            Assert.All(z.Raeume, r => Assert.Equal(Geometrieherkunft.Schematisch, r.Herkunft));
        }

        private static double Abstand(double[] a, double[] b) => Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]));

        [Fact]
        public void Steht_die_Trennwand_auf_keiner_Gegenkante_wird_der_Partner_in_90_Grad_Schritten_gedreht()
        {
            // T in B nach Norden statt Westen: B wird um 90° gedreht, die Strecken liegen trotzdem deckungsgleich.
            Zonengeometrie z = Zonengeometrie.AusFlaechen(Zwei(sektorB: 0));
            Raumumriss a = z.Raum("A"), b = z.Raum("B");
            Assert.Equal(0, a.DrehungGrad);
            Assert.Equal(90, b.DrehungGrad);
            (double[] a0, double[] a1) = Strecke(a, "T-A");
            (double[] b0, double[] b1) = Strecke(b, "T-B");
            Gleich(a0, b1);
            Gleich(a1, b0);
            Assert.False(Ueberlappt(a, b));
            // Die Kante der Trennwand zeigt nach Westen (Azimut 270°), die Fläche bleibt.
            Umrisskante kante = b.Polygone[0].Kanten.Single(k => k.Grenzen.Any(v => v.BauteilKennung == "T"));
            Assert.Equal(270.0, kante.AzimutGrad.Value, Genau);
            Assert.Equal(30.0, b.PolygonflaecheM2, 1e-9);
            Assert.Contains(z.Meldungen, m => m.Schluessel == Zonengeometrie.GEDREHT && m.Werte[1] == "Raum B");
        }

        /// <summary>Drei Räume: A–B über T1 (A Ost), B–C über T2 (B Nord), A–C über T3 (A Nord) — C kann nicht an beiden liegen.</summary>
        internal static Umrisseingang Widerspruch()
        {
            var e = new Umrisseingang();
            e.Raeume.Add(Raum("A", 50.0, 125.0, Wand("A-S", "A-S", 2, 25.0), Wand("T3-A", "T3", 0, 25.0), Wand("A-W", "A-W", 3, 12.5), Wand("T1-A", "T1", 1, 12.5)));
            e.Raeume.Add(Raum("B", 30.0, 75.0, Wand("T1-B", "T1", 3, 12.5), Wand("B-O", "B-O", 1, 12.5), Wand("B-S", "B-S", 2, 15.0), Wand("T2-B", "T2", 0, 15.0)));
            e.Raeume.Add(Raum("C", 40.0, 100.0, Wand("T2-C", "T2", 2, 10.0), Wand("T3-C", "T3", 2, 10.0), Wand("C-N", "C-N", 0, 20.0),
                              Wand("C-O", "C-O", 1, 10.0), Wand("C-W", "C-W", 3, 10.0)));
            return e;
        }

        [Fact]
        public void Eine_widerspruechliche_Anordnung_ist_benannt_abgelehnt_und_die_Gruppe_steht_gereiht()
        {
            Zonengeometrie z = Zonengeometrie.AusFlaechen(Widerspruch());
            Assert.True(z.AnordnungAbgelehnt);
            Assert.Equal(3, z.Nachbarpaare.Count);
            Assert.All(z.Nachbarpaare, p => Assert.True(p.Abgelehnt && !p.Angelegt));
            Nachbarpaar w = Assert.Single(z.Nachbarpaare, p => p.Widerspruch);
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == Zonengeometrie.ANORDNUNG_ABGELEHNT);
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal("1", m.Werte[0]);
            Assert.Contains("Raum " + w.RaumA, m.Werte[1]);
            // Kein stiller Rückfall auf ein Anlegen: keiner liegt an, keiner überlappt, alle in Nordrichtung.
            Assert.All(z.Raeume, r => Assert.False(r.Angelegt));
            Assert.All(z.Raeume, r => Assert.Equal(0, r.DrehungGrad));
            for (int i = 0; i < z.Raeume.Count; i++)
                for (int j = i + 1; j < z.Raeume.Count; j++)
                    Assert.False(Ueberlappt(z.Raeume[i], z.Raeume[j]));
            // Die Gegenstücke sind trotzdem nachgezogen — die Nachbarschaft ist ein Datum, keine Anordnung.
            Assert.All(z.Nachbarpaare, p => Assert.Equal(p.VerweisB.Kennung, p.VerweisA.GegenstueckKennung));
            Assert.Null(Zonenkoerper.Bilden(z).Raeume.FirstOrDefault(r => r.RaumKennung == "nicht da"));
        }

        /// <summary>
        /// <b>Probe 25 am Modell:</b> dieselbe Eingabe gibt denselben Fingerabdruck; umgekehrte Reihenfolge der Räume und
        /// Seiten gibt dieselben Koordinaten auf 1e‑6 — das Aneinanderlegen sortiert nach Kennungen.
        /// </summary>
        [Fact]
        public void Probe25_dieselbe_Eingabe_gibt_dieselben_Koordinaten_auch_in_anderer_Listenfolge()
        {
            Assert.Equal(ZonengeometrieTests.Fingerabdruck(Zonengeometrie.AusFlaechen(Zwei()), true),
                         ZonengeometrieTests.Fingerabdruck(Zonengeometrie.AusFlaechen(Zwei()), true));
            Zonengeometrie vor = Zonengeometrie.AusFlaechen(Zwei()), rueck = Zonengeometrie.AusFlaechen(Zwei(umgekehrt: true));
            foreach (string k in new[] { "A", "B" })
            {
                double[] x = ZonengeometrieTests.Kasten(vor.Raum(k).Polygone[0]), y = ZonengeometrieTests.Kasten(rueck.Raum(k).Polygone[0]);
                for (int i = 0; i < 4; i++) Assert.Equal(x[i], y[i], 1e-6);
            }
            Zonenkoerper kv = Zonenkoerper.Bilden(vor), kr = Zonenkoerper.Bilden(rueck);
            foreach (Raumkoerper r in kv.Raeume)
                foreach (Koerperflaeche f in r.Flaechen)
                {
                    Koerperflaeche g = kr.Flaeche(r.RaumKennung, f.Verweis.BauteilKennung);
                    Assert.Equal(f.PunkteM.Count, g.PunkteM.Count);
                    for (int i = 0; i < f.PunkteM.Count; i++)
                        for (int d = 0; d < 3; d++) Assert.Equal(f.PunkteM[i][d], g.PunkteM[i][d], 1e-6);
                }
        }

        [Fact]
        public void Die_Raumkoerper_sind_kantenschluessig_und_die_Normalen_zeigen_nach_aussen()
        {
            Zonengeometrie z = Zonengeometrie.AusFlaechen(Zwei());
            Zonenkoerper k = Zonenkoerper.Bilden(z);
            Assert.Equal(2, k.Raeume.Count);
            foreach (Raumkoerper r in k.Raeume)
            {
                Raumumriss u = z.Raum(r.RaumKennung);
                double[] kasten = ZonengeometrieTests.Kasten(u.Polygone[0]);
                double mx = (kasten[0] + kasten[2]) / 2.0, my = (kasten[1] + kasten[3]) / 2.0, mz = r.BodenM + r.HoeheM / 2.0;
                Assert.Equal(6, r.Schale.Count);
                foreach (IReadOnlyList<double[]> ring in r.Schale.Concat(r.Flaechen.Select(f => f.PunkteM)))
                {
                    double[] n = Newell(ring);
                    double[] p = ring[0];
                    // Vom Raummittelpunkt zur Fläche zeigt dieselbe Richtung wie die Normale.
                    Assert.True(n[0] * (p[0] - mx) + n[1] * (p[1] - my) + n[2] * (p[2] - mz) > 0.0);
                    foreach (double[] q in ring)
                    {
                        Assert.InRange(q[0], kasten[0] - 1e-6, kasten[2] + 1e-6);
                        Assert.InRange(q[1], kasten[1] - 1e-6, kasten[3] + 1e-6);
                        Assert.InRange(q[2], r.BodenM - 1e-9, r.BodenM + r.HoeheM + 1e-9);
                    }
                }
                // Die Wände tragen die Fläche ihrer Strecke × Höhe; Boden- und Deckenstreifen füllen das Rechteck.
                Assert.Equal(u.PolygonflaecheM2, r.Flaechen.Where(f => f.Stellung == Grenzstellung.Boden).Sum(f => Betrag(Newell(f.PunkteM))), 1e-6);
                // Kantenschlüssig: jeder Eckpunkt, der auf einer Kante einer anderen Fläche liegt, ist deren Punkt.
                List<double[]> alle = r.Flaechen.SelectMany(f => f.EckenM).ToList();
                foreach (Koerperflaeche f in r.Flaechen)
                    Assert.Equal(f.PunkteM.Count, Zonenkoerper.Kantenschluessig(f.PunkteM, alle).Count);
            }
            // Die Trennwand: dieselben vier Ecken aus beiden Räumen, gegenläufig.
            Koerperflaeche ta = k.Flaeche("A", "T"), tb = k.Flaeche("B", "T");
            Assert.Equal(Text(ta.EckenM[0]), Text(tb.EckenM[1]));
            Assert.Equal(Text(ta.EckenM[1]), Text(tb.EckenM[0]));
        }

        internal static string Text(double[] p) => string.Join(";", p.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));

        internal static double[] Newell(IReadOnlyList<double[]> ring)
        {
            double x = 0, y = 0, z = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                x += (a[1] - b[1]) * (a[2] + b[2]);
                y += (a[2] - b[2]) * (a[0] + b[0]);
                z += (a[0] - b[0]) * (a[1] + b[1]);
            }
            return new[] { x / 2.0, y / 2.0, z / 2.0 };
        }

        internal static double Betrag(double[] v) => Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
    }
}
