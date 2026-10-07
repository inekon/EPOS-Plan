using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G5-1 — der Bauteilkörper als Rechengröße</b> (Abstimmung G5): Fläche, Neigung und Azimut je Darstellungsart
    /// gegen Handrechnung, die Gliederung (unter 5° zusammengefasst, ab 5° getrennt), die Außenseite am Gebäudeschwerpunkt,
    /// Placement-Kette mit TrueNorth bzw. <c>IfcMapConversion</c>, Millimeter, die Rangfolge Mengensatz vor Körper samt
    /// Meldung über 2 % und die Herkunft je Fläche. Die Proben sind mit <see cref="IfcProbenErzeuger.Bauteilkoerperproben"/>
    /// byte-gleich neu erzeugbar. Ohne Datenbank.
    /// </summary>
    public sealed class IfcBauteilkoerperTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const double FLAECHE_TOL = 1e-6;
        private const double WINKEL_TOL = 1e-6;
        private const double H = 2.5;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("en-US");

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

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string name)
            => a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.Abbild.BauteileOhneGebaeude).Single(b => b.Name == name);

        private static double Norm(double w) => ((w % 360.0) + 360.0) % 360.0;

        /// <summary>Die Drehung von TrueNorth [−2, 1, 0]: Nord des Modells liegt bei 360° − atan 2.</summary>
        private static readonly double ATAN2_GRAD = Math.Atan(2.0) * 180.0 / Math.PI;

        private static void Nah(double erwartet, double? ist, double tol, string wo = "")
        {
            Assert.True(ist.HasValue, wo + ": kein Wert");
            Assert.True(Math.Abs(erwartet - ist.Value) <= tol, wo + ": erwartet " + erwartet.ToString("R") + ", ist " + ist.Value.ToString("R"));
        }

        /// <summary>
        /// Die senkrechten Flächen eines Prismas über dem Grundriss <paramref name="ring"/> [m] (gegen den Uhrzeigersinn) von
        /// z = 0 bis <paramref name="hoehe"/>, je Kante zwei Dreiecke mit der Normalen nach außen; Deckel fehlen (für Wände
        /// ohne Belang).
        /// </summary>
        private static Dateikoerper Prisma(IReadOnlyList<(double X, double Y)> ring, double hoehe)
        {
            var punkte = new List<double[]>();
            var dreiecke = new List<int[]>();
            for (int i = 0; i < ring.Count; i++)
            {
                (double X, double Y) a = ring[i], b = ring[(i + 1) % ring.Count];
                int n = punkte.Count;
                punkte.Add(new[] { a.X, a.Y, 0.0 });
                punkte.Add(new[] { b.X, b.Y, 0.0 });
                punkte.Add(new[] { b.X, b.Y, hoehe });
                punkte.Add(new[] { a.X, a.Y, hoehe });
                dreiecke.Add(new[] { n, n + 1, n + 2 });
                dreiecke.Add(new[] { n, n + 2, n + 3 });
            }
            return new Dateikoerper { PunkteM = punkte, Dreiecke = dreiecke };
        }

        /// <summary>Ein Wandstück 0,3 m dick: Außenseite längs der Punkte <paramref name="aussen"/>, innen um die Dicke versetzt (nach links).</summary>
        private static Dateikoerper Wandzug(params (double X, double Y)[] aussen)
        {
            // Innenlinie: je Ecke um 0,3 m nach links versetzt, an Knicken auf Gehrung — die Innenseiten bleiben parallel.
            var innen = new List<(double X, double Y)>();
            for (int i = 0; i < aussen.Length; i++)
            {
                (double X, double Y)? n1 = null, n2 = null;
                if (i > 0) { (double dx, double dy) = Richtung(aussen[i - 1], aussen[i]); n1 = (-dy, dx); }
                if (i < aussen.Length - 1) { (double dx, double dy) = Richtung(aussen[i], aussen[i + 1]); n2 = (-dy, dx); }
                (double X, double Y) a = n1 ?? n2.Value, b = n2 ?? n1.Value;
                double f = 0.3 / (1.0 + a.X * b.X + a.Y * b.Y);
                innen.Add((aussen[i].X + f * (a.X + b.X), aussen[i].Y + f * (a.Y + b.Y)));
            }
            innen.Reverse();
            return Prisma(aussen.Concat(innen).ToList(), H);
        }

        private static (double, double) Richtung((double X, double Y) a, (double X, double Y) b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, l = Math.Sqrt(dx * dx + dy * dy);
            return (dx / l, dy / l);
        }

        // ==================================================================
        //  Auswertung je Körper
        // ==================================================================

        [Fact]
        public void Gerade_Wand_Flaeche_aus_Laenge_mal_Hoehe_Azimut_der_Aussenseite()
        {
            // Außenseite längs der x-Achse, der Körper liegt nördlich (y 0 … 0,3); der Schwerpunkt weiter nördlich.
            Dateikoerper k = Wandzug((0, 0), (10, 0));
            Bauteilkoerperflaeche f = IfcBauteilkoerper.Auswerten(k, Bauteilkoerperart.Wand, new[] { 5.0, 4.0, 1.0 }, 0.0);
            Nah(25.0, f.FlaecheM2, FLAECHE_TOL);
            Nah(180.0, f.AzimutGrad, WINKEL_TOL);
            Nah(90.0, f.NeigungGrad, WINKEL_TOL);
            Assert.False(f.Gegliedert);
            Assert.False(f.AussenseiteUnbestimmt);

            // Liegt der Schwerpunkt südlich, ist die Nordseite außen.
            Bauteilkoerperflaeche g = IfcBauteilkoerper.Auswerten(k, Bauteilkoerperart.Wand, new[] { 5.0, -4.0, 1.0 }, 0.0);
            Nah(0.0, g.AzimutGrad, WINKEL_TOL);
            Nah(25.0, g.FlaecheM2, FLAECHE_TOL);
        }

        [Fact]
        public void Ohne_Schwerpunkt_ist_die_Aussenseite_unbestimmt_und_folgt_der_groessten_Flaeche()
        {
            Bauteilkoerperflaeche f = IfcBauteilkoerper.Auswerten(Wandzug((0, 0), (10, 0)), Bauteilkoerperart.Wand, null, 0.0);
            Assert.True(f.AussenseiteUnbestimmt);
            Nah(25.0, f.FlaecheM2, FLAECHE_TOL);
            Assert.False(f.Gegliedert);
        }

        [Theory]
        [InlineData(3.0, 1)]
        [InlineData(4.9, 1)]
        [InlineData(5.1, 2)]
        [InlineData(7.0, 2)]
        public void Gegliederte_Wand_unter_5_Grad_zusammengefasst_ab_5_Grad_getrennt(double knickGrad, int teile)
        {
            double w = knickGrad * Math.PI / 180.0;
            // Zwei Außenstücke von je 5 m: das erste nach Ost, das zweite um den Knick nach links (Nord) gedreht.
            Dateikoerper k = Wandzug((0, 0), (5, 0), (5 + 5 * Math.Cos(w), 5 * Math.Sin(w)));
            Bauteilkoerperflaeche f = IfcBauteilkoerper.Auswerten(k, Bauteilkoerperart.Wand, new[] { 5.0, 6.0, 1.0 }, 0.0);
            Assert.Equal(teile, f.Teile.Count);
            Assert.Equal(teile == 1, f.Zusammengefasst);
            Nah(2 * 5 * H, f.FlaecheM2, 1e-9, "Summe der Außenseite");
            if (teile == 1)
                // Flächengewichtet: die Mitte der beiden Richtungen (gleich große Stücke).
                Nah(Norm(180.0 - knickGrad / 2.0), f.AzimutGrad, WINKEL_TOL);
            else
            {
                Nah(5 * H, f.Teile[0].FlaecheM2, 1e-9);
                Nah(5 * H, f.Teile[1].FlaecheM2, 1e-9);
                // Gleich große Teile: die Reihenfolge entscheidet das letzte Bit — nach Azimut geordnet prüfen.
                List<double> az = f.Teile.Select(t => t.AzimutGrad.Value).OrderBy(x => x).ToList();
                Nah(Norm(180.0 - knickGrad), az[0], WINKEL_TOL, "geknickter Teil");
                Nah(180.0, az[1], WINKEL_TOL, "gerader Teil");
            }
        }

        [Fact]
        public void TrueNorth_und_MapConversion_drehen_den_Azimut_wie_im_Raumweg()
        {
            // Wie Probe ifc4_mapconversion: TrueNorth [−2, 1, 0] und Umrechnung 90° — es gilt die Umrechnung.
            double drehung = IfcPlatzierung.Drehung(IfcPlatzierung.DrehungAusTrueNorth(-2, 1), IfcPlatzierung.DrehungAusMapConversion(0, 1));
            // Außenseite nach Modell-Nord (Körper südlich der Außenlinie, Schwerpunkt südlich).
            Dateikoerper k = Wandzug((10, 8), (0, 8));
            Bauteilkoerperflaeche f = IfcBauteilkoerper.Auswerten(k, Bauteilkoerperart.Wand, new[] { 5.0, 4.0, 1.0 }, drehung);
            Nah(270.0, f.AzimutGrad, WINKEL_TOL);

            // Nur TrueNorth: 0° − (360° − atan 2) = atan 2.
            Bauteilkoerperflaeche t = IfcBauteilkoerper.Auswerten(k, Bauteilkoerperart.Wand, new[] { 5.0, 4.0, 1.0 },
                                                                   IfcPlatzierung.Drehung(IfcPlatzierung.DrehungAusTrueNorth(-2, 1), null));
            Nah(ATAN2_GRAD, t.AzimutGrad, WINKEL_TOL);
        }

        // ==================================================================
        //  Proben: Wandhaus (Extrusion, Placement-Kette, mm, TrueNorth)
        // ==================================================================

        [Fact]
        public void Wandhaus_ohne_Mengen_Flaeche_und_Azimut_aus_der_Extrusion()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_wand_extrusion.ifc");
            // Gebäude 90° gedreht: Süd des Gebäudes zeigt nach Modell-Ost (90°), dazu 360° − atan 2 gegen Nord.
            (string Name, double Flaeche, double Azimut)[] erwartet =
            {
                ("Wand Süd", 10 * H, Norm(90.0 + ATAN2_GRAD)),
                ("Wand Ost", (7.7 + 2.0) * H, Norm(0.0 + ATAN2_GRAD)),
                ("Wand Nord", 10 * H, Norm(270.0 + ATAN2_GRAD)),
                ("Wand West", 8 * H, Norm(180.0 + ATAN2_GRAD)),
            };
            foreach ((string name, double flaeche, double azimut) in erwartet)
            {
                AbbildBauteil b = Bauteil(a, name);
                Nah(flaeche, b.BruttoflaecheM2, FLAECHE_TOL, name);
                Nah(azimut, b.AzimutGrad, WINKEL_TOL, name);
                Assert.Equal(Flaechenherkunft.Koerper, b.Flaechenherkunft);
                Assert.Null(b.NettoflaecheM2);   // brutto; den Abzug der Öffnungen bringt G5-2
                Assert.False(b.Koerperflaeche.AussenseiteUnbestimmt, name);
            }

            // Die L-Wand: zwei Teile (Außenseite 7,7 m und die Nordseite des Flügels 2 m), getrennt (90°).
            Bauteilkoerperflaeche l = Bauteil(a, "Wand Ost").Koerperflaeche;
            Assert.True(l.Gegliedert);
            Assert.False(l.Zusammengefasst);
            Nah(7.7 * H, l.Teile[0].FlaecheM2, FLAECHE_TOL);
            Nah(2.0 * H, l.Teile[1].FlaecheM2, FLAECHE_TOL);
            Nah(Norm(270.0 + ATAN2_GRAD), l.Teile[1].AzimutGrad, WINKEL_TOL);

            Assert.Equal("4", a.Meldungen.Single(m => m.Schluessel == P + "FLAECHE_KOERPER").Werte[0]);
            Assert.Equal(new[] { "1", "Wand Ost" }, a.Meldungen.Single(m => m.Schluessel == P + "KOERPER_GEGLIEDERT").Werte);
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == P + "KEINE_MENGEN" || m.Schluessel == P + "SEITE_UNBESTIMMT"
                                                    || m.Schluessel == P + "KOERPER_ABWEICHUNG");
        }

        [Fact]
        public void Gegenprobe_Mengensatz_bleibt_Quelle_und_nur_ueber_2_Prozent_wird_gemeldet()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_mengen_gegenprobe.ifc");
            (string Name, double Menge, double Koerper)[] werte =
            {
                ("Wand Süd", 25.2, 25.0), ("Wand Ost", 24.25, 24.25), ("Wand Nord", 26.25, 25.0), ("Wand West", 19.9, 20.0),
            };
            foreach ((string name, double menge, double koerper) in werte)
            {
                AbbildBauteil b = Bauteil(a, name);
                Nah(menge, b.BruttoflaecheM2, 1e-12, name);
                Assert.Equal(Flaechenherkunft.Mengensatz, b.Flaechenherkunft);
                Nah(koerper, b.Koerperflaeche.FlaecheM2, FLAECHE_TOL, name);
            }
            PruefMeldung m = Assert.Single(a.Meldungen, x => x.Schluessel == P + "KOERPER_ABWEICHUNG");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "Wand Nord", "26.25", "25", "4.8", "2" }, m.Werte);
            Assert.DoesNotContain(a.Meldungen, x => x.Schluessel == P + "FLAECHE_KOERPER");
        }

        // ==================================================================
        //  Probe: Körperhaus (BRep, MappedItem, Flächenmodell, Dreiecksnetz, Dach, Bodenplatte)
        // ==================================================================

        [Fact]
        public void Koerperhaus_je_Darstellungsart_Flaeche_Neigung_und_Azimut()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_wand_brep_mapped.ifc");
            (string Name, double Flaeche, double? Azimut, double Neigung)[] erwartet =
            {
                ("Wand Süd", 10 * H, 180.0, 90.0),     // IfcFacetedBrep
                ("Wand Ost", 8 * H, 90.0, 90.0),       // IfcMappedItem, gestreckt 8 : 1 : 2,5
                ("Wand Nord", 10 * H, 0.0, 90.0),      // IfcShellBasedSurfaceModel
                ("Wand West", 8 * H, 270.0, 90.0),     // IfcTriangulatedFaceSet
                ("Dachplatte", 100.0, 180.0, Math.Acos(0.8) * 180.0 / Math.PI),   // schräge Extrusion 3 : 4
                ("Bodenplatte", 80.0, null, 180.0),
            };
            foreach ((string name, double flaeche, double? azimut, double neigung) in erwartet)
            {
                AbbildBauteil b = Bauteil(a, name);
                Nah(flaeche, b.BruttoflaecheM2, FLAECHE_TOL, name);
                Assert.Equal(Flaechenherkunft.Koerper, b.Flaechenherkunft);
                Nah(neigung, b.NeigungGrad, WINKEL_TOL, name);
                if (azimut.HasValue) Nah(azimut.Value, b.AzimutGrad, WINKEL_TOL, name);
                else Assert.Null(b.AzimutGrad);
            }
            Assert.Equal(Bauteilart.Dach, Bauteil(a, "Dachplatte").Art);
            Assert.Equal("6", a.Meldungen.Single(m => m.Schluessel == P + "FLAECHE_KOERPER").Werte[0]);
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == P + "KEINE_MENGEN");
        }

        // ==================================================================
        //  Rangfolge und Herkunft bis zur Bauteilzeile
        // ==================================================================

        [Fact]
        public void Ohne_Mengen_kommen_die_Flaechen_aus_dem_Koerper_bis_in_die_Bauteilzeilen()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_ohne_mengen.ifc");
            foreach (AbbildBauteil b in a.Abbild.Gebaeude[0].Bauteile)
            {
                Assert.Equal(Flaechenherkunft.Koerper, b.Flaechenherkunft);
                Nah(b.Name.Contains("Süd") || b.Name.Contains("Nord") ? 25.0 : 20.0, b.BruttoflaecheM2, FLAECHE_TOL, b.Name);
            }
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            List<GebaeudeBauteilzeile> waende = v.Zeilen.Where(z => z.Summenfeld == GebaeudeZielfelder.FLAECHE_AUSSENWAND).ToList();
            Assert.Equal(4, waende.Count);
            Assert.All(waende, z => Assert.Equal(Flaechenherkunft.Koerper, z.Flaechenherkunft));
            Nah(90.0, waende.Sum(z => z.Bauteil.Flaeche), FLAECHE_TOL);
            Assert.Equal(FlaechenherkunftWerte.KOERPER, FlaechenherkunftWerte.Wert(waende[0].Flaechenherkunft));
        }

        [Fact]
        public void Mit_Mengensatz_tragen_die_Bauteilzeilen_die_Herkunft_Mengensatz()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_haus.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            Assert.Contains(v.Zeilen, z => z.Flaechenherkunft == Flaechenherkunft.Mengensatz);
            Assert.DoesNotContain(v.Zeilen, z => z.Flaechenherkunft == Flaechenherkunft.Koerper);
        }

        // ==================================================================
        //  Proben
        // ==================================================================

        [Fact(Skip = "Erzeuger: schreibt die Bauteilkörperproben nach Referenzlaeufe/Importproben — nur von Hand, siehe IfcProbenTests.")]
        public void Erzeuger_schreibt_die_Bauteilkoerperproben()
        {
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Bauteilkoerperproben())
                File.WriteAllBytes(Path.Combine(IfcProbenTests.Ordner(), p.Key), p.Value);
        }

        [Fact]
        public void Die_abgelegten_Bauteilkoerperproben_sind_byte_gleich_neu_erzeugbar()
        {
            var funde = new List<string>();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Bauteilkoerperproben())
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), p.Key);
                if (!File.Exists(pfad)) funde.Add(p.Key + ": fehlt");
                else if (!File.ReadAllBytes(pfad).SequenceEqual(p.Value)) funde.Add(p.Key + ": weicht ab");
                else Assert.True(p.Value.Length < 32 * 1024, p.Key + " ist keine Kleinstdatei");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }
    }
}
