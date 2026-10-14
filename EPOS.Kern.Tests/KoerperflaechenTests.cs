using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Bauteilflächen je Raum und Zone aus den Körpern</b> (Abstimmung G5, Teil G5-3, A1–A5): die Probe
    /// <c>ifc4_g5_kleinhaus_ohne_mengen.ifc</c> ohne Mengensätze, Raumgrenzen und Raumbezüge gegen die Handrechnung — Fläche
    /// je Raum (1e-6 m²), Azimut und Neigung, Randbedingung (Außenluft, Erdreich, Trennfläche, unbeheizt), Öffnungen nach
    /// Lage, Herkunft <see cref="Flaechenherkunft.Koerper"/> je Fläche, keine schematische Fläche; die Gegenprobe
    /// <c>ifc4_g5_kleinhaus_mit_mengen.ifc</c> (Raumgrenzen gehen vor) innerhalb 2 % je Zone und Randbedingung.
    /// </summary>
    public sealed class KoerperflaechenTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        internal const string OHNE = "ifc4_g5_kleinhaus_ohne_mengen.ifc";
        internal const string MIT = "ifc4_g5_kleinhaus_mit_mengen.ifc";
        private static readonly double DACHNEIGUNG = Math.Acos(0.8) * 180.0 / Math.PI;

        // Die Handrechnung [m²]: Raumflächen 9,4 × 5,4 (Keller, Schlafen), 5,58 × 5,4 (Wohnen), 3,58 × 5,4 (Küche).
        private const double GRUND = 9.4 * 5.4, WOHNEN = 5.58 * 5.4, KUECHE = 3.58 * 5.4;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        internal static GebaeudeImportAblauf Lesen(string datei, bool koerperflaechenAus = false)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil { KoerperflaechenAus = koerperflaechenAus });
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static AbbildGebaeude Gebaeude(GebaeudeImportAblauf a) => Assert.Single(a.Abbild.Gebaeude);

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string name) => Gebaeude(a).Bauteile.Single(b => b.Name == name);

        private static string Raum(GebaeudeImportAblauf a, string name) => Gebaeude(a).Raeume.Single(r => r.Name == name).Kennung;

        private static void Nah(double erwartet, double? ist, string wo, double toleranz = 1e-6)
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(ist.Value - erwartet) <= toleranz,
                wo + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>Die Grenzen eines Bauteils an einem Raum: Summe der Fläche, Lage.</summary>
        private static List<AbbildGrenze> Grenzen(GebaeudeImportAblauf a, string bauteil, string raum)
            => Bauteil(a, bauteil).Grenzen.Where(g => g.RaumKennung == Raum(a, raum)).ToList();

        private static void Flaeche(GebaeudeImportAblauf a, string bauteil, string raum, double erwartet, Randbedingung lage)
        {
            List<AbbildGrenze> g = Grenzen(a, bauteil, raum);
            Assert.True(g.Count > 0, bauteil + " / " + raum + ": keine Grenze");
            Nah(erwartet, g.Sum(x => x.FlaecheM2 ?? 0.0), bauteil + " / " + raum);
            Assert.All(g, x => Assert.Equal(lage, x.Lage));
            Assert.All(g, x => Assert.Equal(Grenzherkunft.Bauteilkoerper, x.Herkunft));
        }

        // ==================================================================
        //  Flächen je Raum und Randbedingung
        // ==================================================================

        [Fact]
        public void Wandflaechen_je_Raum_gegen_die_Handrechnung()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            Flaeche(a, "Wand Süd KG", "Keller", 9.4 * 2.2, Randbedingung.Erdreich);
            Flaeche(a, "Wand Nord KG", "Keller", 9.4 * 2.2, Randbedingung.Erdreich);
            Flaeche(a, "Wand West KG", "Keller", 5.4 * 2.2, Randbedingung.Erdreich);
            Flaeche(a, "Wand Ost KG", "Keller", 5.4 * 2.2, Randbedingung.Erdreich);
            Flaeche(a, "Wand Süd EG", "Wohnen", 5.58 * 2.5, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Süd EG", "Küche", 3.58 * 2.5, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Nord EG", "Wohnen", 5.58 * 2.5, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Nord EG", "Küche", 3.58 * 2.5, Randbedingung.Aussenluft);
            Flaeche(a, "Wand West EG", "Wohnen", 5.4 * 2.5, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Ost EG", "Küche", 5.4 * 2.5, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Süd OG", "Schlafen", 9.4 * 1.0, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Nord OG", "Schlafen", 9.4 * 5.05, Randbedingung.Aussenluft);
            // Ost und West: Trapez 5,4 m lang, 1,0 m bis 5,05 m hoch.
            Flaeche(a, "Wand West OG", "Schlafen", 5.4 * (1.0 + 5.05) / 2.0, Randbedingung.Aussenluft);
            Flaeche(a, "Wand Ost OG", "Schlafen", 5.4 * (1.0 + 5.05) / 2.0, Randbedingung.Aussenluft);
            // Eine Außenwand des Erdgeschosses liegt an keinem anderen Raum.
            Assert.Empty(Grenzen(a, "Wand Süd EG", "Keller"));
            Assert.Empty(Grenzen(a, "Wand Süd EG", "Schlafen"));
        }

        [Fact]
        public void Trennflaechen_tragen_Gegenstuecke_und_der_Deckenstreifen_geht_anteilig_auf()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            // Innenwand: beide Seiten 5,4 × 2,5 m, wechselseitig Gegenstücke.
            Flaeche(a, "Innenwand", "Wohnen", 5.4 * 2.5, Randbedingung.Innen);
            Flaeche(a, "Innenwand", "Küche", 5.4 * 2.5, Randbedingung.Innen);
            AbbildGrenze w = Assert.Single(Grenzen(a, "Innenwand", "Wohnen"));
            AbbildGrenze k = Assert.Single(Grenzen(a, "Innenwand", "Küche"));
            Assert.Equal(k.Kennung, w.GegenstueckKennung);
            Assert.Equal(w.Kennung, k.GegenstueckKennung);

            // Kellerdecke: oben je Raum seine Bodenfläche, unten der Keller ganz — der Streifen unter der Innenwand
            // (0,24 × 5,4 m) geht anteilig an die beiden Trennflächen.
            Flaeche(a, "Kellerdecke", "Wohnen", WOHNEN, Randbedingung.Innen);
            Flaeche(a, "Kellerdecke", "Küche", KUECHE, Randbedingung.Innen);
            Flaeche(a, "Kellerdecke", "Keller", GRUND, Randbedingung.Innen);
            List<AbbildGrenze> unten = Grenzen(a, "Kellerdecke", "Keller");
            Assert.Equal(2, unten.Count);
            AbbildBauteil kd = Bauteil(a, "Kellerdecke");
            AbbildGrenze unterWohnen = unten.Single(x => kd.Grenzen.Single(y => y.Kennung == x.GegenstueckKennung).RaumKennung == Raum(a, "Wohnen"));
            Nah(GRUND * WOHNEN / (WOHNEN + KUECHE), unterWohnen.FlaecheM2, "Keller unter Wohnen");

            Flaeche(a, "Geschossdecke", "Wohnen", WOHNEN, Randbedingung.Innen);
            Flaeche(a, "Geschossdecke", "Küche", KUECHE, Randbedingung.Innen);
            Flaeche(a, "Geschossdecke", "Schlafen", GRUND, Randbedingung.Innen);

            // Kein zweites Trennbauteil aus den Raumkörpern für dieselben Raumpaare.
            Assert.DoesNotContain(Gebaeude(a).Bauteile, b => b.Grenzen.Any(g => g.Herkunft == Grenzherkunft.Koerper));
        }

        [Fact]
        public void Boden_am_Erdreich_und_Dach_mit_Neigung_und_Azimut_aus_der_Aussennormale()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            Flaeche(a, "Bodenplatte", "Keller", GRUND, Randbedingung.Erdreich);
            // Dach: 9,4 × 5,4 m in der Projektion, geneigt 3 : 4 → ÷ 0,8.
            Flaeche(a, "Dachplatte", "Schlafen", GRUND / 0.8, Randbedingung.Aussenluft);
            AbbildGrenze dach = Assert.Single(Grenzen(a, "Dachplatte", "Schlafen"));
            Nah(DACHNEIGUNG, IfcBauteilkoerper.Neigung(dach.Normale), "Neigung Dach");
            Nah(180.0, IfcBauteilkoerper.Azimut(dach.Normale, 0.0), "Azimut Dach");
            AbbildGrenze boden = Assert.Single(Grenzen(a, "Bodenplatte", "Keller"));
            Nah(180.0, IfcBauteilkoerper.Neigung(boden.Normale), "Neigung Boden");

            // Die Außennormale einer Wand zeigt vom Raum weg: Süd 180°, Ost 90°, Nord 0°, West 270°.
            foreach ((string wand, double azimut) in new[] { ("Wand Süd EG", 180.0), ("Wand Ost EG", 90.0), ("Wand Nord EG", 0.0), ("Wand West EG", 270.0) })
            {
                AbbildGrenze g = Bauteil(a, wand).Grenzen.First();
                Nah(azimut, IfcBauteilkoerper.Azimut(g.Normale, 0.0), wand);
                Nah(90.0, IfcBauteilkoerper.Neigung(g.Normale), wand);
            }
            // Die Innenwand aus Sicht der Küche zeigt nach West.
            Nah(270.0, IfcBauteilkoerper.Azimut(Assert.Single(Grenzen(a, "Innenwand", "Küche")).Normale, 0.0), "Innenwand Küche");
        }

        [Fact]
        public void Oeffnungen_gehen_nach_ihrer_Lage_an_den_Raum()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            AbbildBauteil sued = Bauteil(a, "Wand Süd EG");
            AbbildBauteil fw = sued.Oeffnungen.Single(o => o.Name == "Fenster Wohnen");
            AbbildBauteil fk = sued.Oeffnungen.Single(o => o.Name == "Fenster Küche");
            AbbildBauteil fs = Bauteil(a, "Wand Ost OG").Oeffnungen.Single(o => o.Name == "Fenster Schlafen");
            Assert.Equal(new[] { Raum(a, "Wohnen") }, fw.Grenzen.Select(g => g.RaumKennung));
            Assert.Equal(new[] { Raum(a, "Küche") }, fk.Grenzen.Select(g => g.RaumKennung));
            Assert.Equal(new[] { Raum(a, "Schlafen") }, fs.Grenzen.Select(g => g.RaumKennung));
            Assert.All(new[] { fw, fk, fs }, o => Assert.Equal(Randbedingung.Aussenluft, Assert.Single(o.Grenzen).Lage));
            Nah(1.8, fw.BruttoflaecheM2, "Fenster Wohnen");
            Nah(1.2, fk.BruttoflaecheM2, "Fenster Küche");
        }

        [Fact]
        public void Gegenprobe_meldet_die_Abweichung_der_Summe_je_Raum_gegen_die_Koerperflaeche_je_Bauteilart()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            PruefMeldung m = Assert.Single(Gebaeude(a).Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN");
            // 17 Bauteile mit Körper, 26 Flächen je Raum: 115,88 (Keller) + 2 × 100,224 (Decken) + 72,8 + 27 (Erdgeschoss)
            // + 89,54 + 63,45 (Obergeschoss) = 569,118 m².
            Assert.Equal(new[] { "Kleinhaus", "17", "26", "569.12" }, m.Werte);
            // Wände über die volle Länge: Die Körperfläche zeigt die ganze Wandseite, die Räume sehen weniger — Süd EG
            // 22,9 von 28 m² (18,2 %); Dach 63,45 von 75 m², Bodenplatte 50,76 von 60 m² (je 15,4 %).
            Assert.Equal(new[] { "10", "18.2", "Wand Süd EG", "2" },
                         Assert.Single(a.Abbild.Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN_ABWEICHUNG_AUSSENWAND").Werte);
            Assert.Equal(new[] { "1", "15.4", "Dachplatte", "2" },
                         Assert.Single(a.Abbild.Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN_ABWEICHUNG_DACH").Werte);
            Assert.Equal(new[] { "1", "15.4", "Bodenplatte", "2" },
                         Assert.Single(a.Abbild.Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN_ABWEICHUNG_BODEN_DECKE").Werte);
            // Innenwand und Decken gehen ohne Rest auf.
            Assert.DoesNotContain(a.Abbild.Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN_ABWEICHUNG_INNENWAND");
        }

        [Fact]
        public void Raeume_ohne_Mengen_tragen_Flaeche_und_Volumen_ihres_Koerpers()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            AbbildGebaeude g = Gebaeude(a);
            Nah(GRUND, g.Raeume.Single(r => r.Name == "Keller").FlaecheM2, "Keller");
            Nah(GRUND * 2.2, g.Raeume.Single(r => r.Name == "Keller").VolumenM3, "Keller Volumen");
            Nah(WOHNEN, g.Raeume.Single(r => r.Name == "Wohnen").FlaecheM2, "Wohnen");
            // Schlafen unter dem Pultdach: mittlere Höhe (1,0 + 5,05) / 2 m.
            Nah(GRUND, g.Raeume.Single(r => r.Name == "Schlafen").FlaecheM2, "Schlafen");
            Nah(GRUND * 3.025, g.Raeume.Single(r => r.Name == "Schlafen").VolumenM3, "Schlafen Volumen");
            Assert.All(g.Raeume, r => Assert.True(r.FlaecheAusGrundriss));
            Assert.Equal(new[] { "4", "151" }, Assert.Single(g.Meldungen, x => x.Schluessel == P + "RAUMFLAECHE_KOERPER").Werte.Take(2));
            Assert.DoesNotContain(g.Meldungen, x => x.Schluessel == P + "KEINE_RAEUME");
        }

        // ==================================================================
        //  Zonen, Herkunft, Rangfolge
        // ==================================================================

        /// <summary>Die Bruttoflächen der Zonierung je Zone und Rand (Zonenname, Rand) → m².</summary>
        private static SortedDictionary<string, double> Zonensummen(GebaeudeImportAblauf a, out GebaeudeZonierung z)
        {
            z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            var summen = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (Zonenflaeche f in z.Flaechen)
            {
                string s = z.Zonen[f.Zone].Name + "|" + f.Rand;
                summen[s] = (summen.TryGetValue(s, out double w) ? w : 0.0) + (f.BruttoM2 ?? 0.0);
            }
            return summen;
        }

        [Fact]
        public void Zonenflaechen_tragen_die_Herkunft_Koerper_und_keine_ist_schematisch()
        {
            GebaeudeImportAblauf a = Lesen(OHNE);
            SortedDictionary<string, double> summen = Zonensummen(a, out GebaeudeZonierung z);
            Assert.NotEmpty(z.Flaechen);
            Assert.All(z.Flaechen.Where(f => f.Bauteil.Art != Bauteilart.Fenster && f.Bauteil.Art != Bauteilart.Tuer),
                       f => Assert.Equal(Flaechenherkunft.Koerper, f.Flaechenherkunft));
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            Assert.NotEmpty(v.Zeilen);
            Assert.DoesNotContain(v.Zeilen, x => x.Flaechenherkunft == Flaechenherkunft.Schematisch);
            Assert.All(v.Zeilen.Where(x => x.Bauteil.Bauteilart != DbWerte.BAUTEILART_FENSTER), x => Assert.Equal(Flaechenherkunft.Koerper, x.Flaechenherkunft));
            Assert.True(summen.Count > 0);
        }

        [Fact]
        public void Raumgrenzen_der_Gegenprobe_gehen_vor_und_die_Zonenflaechen_liegen_innerhalb_zwei_Prozent()
        {
            GebaeudeImportAblauf mit = Lesen(MIT);
            Assert.DoesNotContain(Gebaeude(mit).Bauteile.SelectMany(b => b.Grenzen), g => g.Herkunft == Grenzherkunft.Bauteilkoerper);
            Assert.DoesNotContain(Gebaeude(mit).Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN");
            SortedDictionary<string, double> sMit = Zonensummen(mit, out GebaeudeZonierung zMit);
            Assert.All(zMit.Flaechen.Where(f => f.Bauteil.Art != Bauteilart.Fenster && f.Bauteil.Art != Bauteilart.Tuer),
                       f => Assert.Equal(Flaechenherkunft.Raumgrenze, f.Flaechenherkunft));
            SortedDictionary<string, double> sOhne = Zonensummen(Lesen(OHNE), out _);
            Assert.Equal(sMit.Keys, sOhne.Keys);
            foreach (KeyValuePair<string, double> e in sMit)
                Assert.True(IfcBauteilkoerper.Abweichung(e.Value, sOhne[e.Key]) <= IfcBauteilkoerper.ABWEICHUNG_GRENZE,
                    e.Key + ": mit Mengen " + e.Value.ToString("R", CultureInfo.InvariantCulture) + ", ohne " + sOhne[e.Key].ToString("R", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Ausgeschaltet_bleibt_der_Rueckfall_ohne_Raumzuordnung()
        {
            GebaeudeImportAblauf a = Lesen(OHNE, koerperflaechenAus: true);
            Assert.DoesNotContain(Gebaeude(a).Bauteile.SelectMany(b => b.Grenzen), g => g.Herkunft == Grenzherkunft.Bauteilkoerper);
            Assert.DoesNotContain(Gebaeude(a).Meldungen, x => x.Schluessel == P + "KOERPERFLAECHEN");
        }

        // ==================================================================
        //  Der Kern ohne Datei
        // ==================================================================

        private static Dateikoerper Kasten(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            var punkte = new List<double[]>();
            for (int i = 0; i < 8; i++) punkte.Add(new[] { (i & 1) == 0 ? x0 : x1, (i & 2) == 0 ? y0 : y1, (i & 4) == 0 ? z0 : z1 });
            int[][] flaechen =
            {
                new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 1, 3, 7, 5 }, new[] { 3, 2, 6, 7 }, new[] { 2, 0, 4, 6 },
            };
            var dreiecke = new List<int[]>();
            foreach (int[] f in flaechen)
            {
                dreiecke.Add(new[] { f[0], f[1], f[2] });
                dreiecke.Add(new[] { f[0], f[2], f[3] });
            }
            return new Dateikoerper { PunkteM = punkte, Dreiecke = dreiecke };
        }

        [Fact]
        public void Raumflaeche_trifft_die_Wand_bis_zur_halben_Dicke_und_Toleranz_davor_oder_dahinter()
        {
            // Wand 0,3 m dick (x 0 … 0,3), Raum bis zur Achse (x 0,15) bzw. 0,21 m in die Wand (über der Grenze).
            var wand = new[] { Kasten(0, 0, 0, 0.3, 5, 2.5) };
            var achse = new Koerperflaechenraum { Koerper = Kasten(0.15, 1, 0, 4, 3, 2.5) };
            var zuTief = new Koerperflaechenraum { Koerper = Kasten(0.09, 1, 0, 4, 3, 2.5) };
            Koerperflaechenergebnis e = Koerperflaechen.Zuordnen(new[] { achse }, wand, 0.3, innen: false);
            Koerperflaechenstueck s = Assert.Single(e.Stuecke);
            Nah(2.0 * 2.5, s.FlaecheM2, "bis zur Achse");
            Assert.Equal(Randbedingung.Aussenluft, s.Lage);
            Assert.Empty(Koerperflaechen.Zuordnen(new[] { zuTief }, wand, 0.3, innen: false).Stuecke);
            // Mit Fuge vor der Wand: 0,19 m trifft noch, 0,21 m nicht mehr.
            var fuge = new Koerperflaechenraum { Koerper = Kasten(0.49, 1, 0, 4, 3, 2.5) };
            var zuWeit = new Koerperflaechenraum { Koerper = Kasten(0.51, 1, 0, 4, 3, 2.5) };
            Nah(5.0, Assert.Single(Koerperflaechen.Zuordnen(new[] { fuge }, wand, 0.3, innen: false).Stuecke).FlaecheM2, "Fuge");
            Assert.Empty(Koerperflaechen.Zuordnen(new[] { zuWeit }, wand, 0.3, innen: false).Stuecke);
        }

        /// <summary>Ein senkrechtes Prisma über dem Ring (gegen den Uhrzeigersinn) zwischen z0 und z1 [m], Dreiecke nach außen.</summary>
        private static Dateikoerper Prisma((double X, double Y)[] ring, double z0, double z1)
        {
            int n = ring.Length;
            var punkte = ring.Select(p => new[] { p.X, p.Y, z0 }).Concat(ring.Select(p => new[] { p.X, p.Y, z1 })).ToList();
            var dreiecke = new List<int[]>();
            for (int i = 1; i < n - 1; i++)
            {
                dreiecke.Add(new[] { 0, i + 1, i });
                dreiecke.Add(new[] { n, n + i, n + i + 1 });
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                dreiecke.Add(new[] { i, j, n + j });
                dreiecke.Add(new[] { i, n + j, n + i });
            }
            return new Dateikoerper { PunkteM = punkte, Dreiecke = dreiecke };
        }

        [Theory]
        [InlineData(4.0, true)]
        [InlineData(6.0, false)]
        public void Parallel_bis_fuenf_Grad(double grad, bool trifft)
        {
            // Die Westseite des Raums um den Fußpunkt (0,3 | 1) gedreht; die Wand 0,3 m dick bei x 0 … 0,3.
            double dx = 2.0 * Math.Tan(grad * Math.PI / 180.0);
            var raum = new Koerperflaechenraum { Koerper = Prisma(new[] { (0.3, 1.0), (4.0, 1.0), (4.0, 3.0), (0.3 + dx, 3.0) }, 0.0, 2.5) };
            List<Koerperflaechenstueck> s = Koerperflaechen.Zuordnen(new[] { raum }, new[] { Kasten(0, 0, 0, 0.3, 5, 2.5) }, 0.3, innen: false).Stuecke;
            Assert.Equal(trifft, s.Count == 1);
        }

        [Fact]
        public void Erzeuger_schreibt_die_Kleinhausproben_byte_gleich()
        {
            var funde = new List<string>();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Kleinhausproben())
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), p.Key);
                if (!File.Exists(pfad)) funde.Add(p.Key + ": fehlt");
                else if (!File.ReadAllBytes(pfad).SequenceEqual(p.Value)) funde.Add(p.Key + ": weicht ab");
                else Assert.True(p.Value.Length < 128 * 1024, p.Key + " ist keine Kleinstdatei");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        [Fact(Skip = "Erzeuger: schreibt die Kleinhausproben nach Referenzlaeufe/Importproben — nur von Hand, siehe IfcProbenTests.")]
        public void Erzeuger_schreibt_die_Kleinhausproben()
        {
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Kleinhausproben())
                File.WriteAllBytes(Path.Combine(IfcProbenTests.Ordner(), p.Key), p.Value);
        }
    }
}
