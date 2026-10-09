using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KM3-E2-a — Rechenproben der Teillast, des Taktens und des Randwegs</b> der Kältemaschine ohne Datenbank: die
    /// Zahlenbeispiele des Fachkonzepts Teillast und Takten (3.2 Teillast 50 %, 3.3 Takten, 3.4 Gütegrad), die Vorgaben
    /// (C_d, Vorgabekurve, Bestandsweg), Rückfall und Normierung, der Randweg und die Folgeschaltung (3.6).
    /// </summary>
    public sealed class KaeltemaschineTeillastTests : IDisposable
    {
        private const double TOL = 1e-3;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Kurve des Zahlenbeispiels: a = 0,10, b = 0,60, c = 0,30, x_u = 0,2, Mindestteillast 20 %.</summary>
        private static Kaeltemaschinenteillast Beispiel(double? cd = null) =>
            Kaeltemaschinenteillast.Bilden("KURVE", 0.10, 0.60, 0.30, 0.2, cd, null, null, 0.2);

        // ---- Fachkonzept 3.2 und 3.3 ----

        [Fact]
        public void Teillast_50_Prozent_trifft_das_Zahlenbeispiel()
        {
            var t = Beispiel();
            Assert.Equal(KaeltemaschinenKurvenherkunft.Kurve, t.Herkunft);
            Assert.Equal(0.475, t.E(0.5), 9);
            var s = t.Stunde(10.0, 20.0, 4.0, 20.0, 0.2);
            Assert.Equal(10.0, s.KaelteKwh, 9);
            Assert.Equal(2.375, s.VerdichterKwh, TOL);
            Assert.Equal(4.21, s.Eer, 2);
            Assert.False(s.Takt);
            Assert.Equal(0.0, s.MehrstromKwh, 12);
        }

        [Fact]
        public void Takten_unter_der_Mindestteillast_trifft_das_Zahlenbeispiel()
        {
            var t = Beispiel();
            Assert.Equal(0.8621, t.G(0.2), 4);
            var s = t.Stunde(2.0, 20.0, 4.0, 20.0, 0.2);
            Assert.True(s.Takt);
            Assert.Equal(0.638, s.VerdichterKwh, TOL);
            Assert.Equal(0.058, s.MehrstromKwh, TOL);
            Assert.Equal(Waermepumpentakt.StartsImTakt(2.0, 4.0), s.Starts);
            Assert.True(s.Starts > 0);
        }

        [Fact]
        public void Takt_rechnet_genau_die_Formel_der_Waermepumpe()
        {
            var t = Beispiel(0.75);
            var s = t.Stunde(1.3, 20.0, 4.0, 20.0, 0.2);
            double strom0 = 1.3 / (4.0 * t.G(0.2));
            Assert.Equal(Waermepumpentakt.Mehrstrom(strom0, 1.3, 4.0, 0.75), s.MehrstromKwh, 12);
            Assert.Equal(strom0 + s.MehrstromKwh, s.VerdichterKwh, 12);
        }

        [Fact]
        public void Vorgabe_Cd_greift_ohne_Eingabe()
        {
            Assert.Equal(Waermepumpentakt.VORGABE_CD, Beispiel().Cd, 12);
            Assert.Equal(0.5, Beispiel(0.5).Cd, 12);
            // C_d = 1: kein Taktverlust.
            Assert.Equal(0.0, Beispiel(1.0).Stunde(2.0, 20.0, 4.0, 20.0, 0.2).MehrstromKwh, 12);
        }

        [Fact]
        public void Linear_mit_Cd_trifft_den_Vergleichswert_des_Papiers()
        {
            var t = Kaeltemaschinenteillast.Bilden("LINEAR", null, null, null, null, null, null, null, 0.2);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Linear, t.Herkunft);
            Assert.Equal(2.5, t.Stunde(10.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, 12);
            Assert.Equal(0.550, t.Stunde(2.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, TOL);
        }

        [Fact]
        public void Weg_leer_rechnet_wie_die_heutige_Kaeltemaschine()
        {
            var t = Kaeltemaschinenteillast.Bilden(null, 0.10, 0.60, 0.30, 0.2, 0.5, "DREHZAHL", null, 0.2);
            Assert.True(t.Bestandsweg);
            var km = new Kaeltemaschine
            {
                NennleistungKw = 20.0,
                Mindestteillast = 0.2,
                Kaltwassertemperatur = 7.0,
                Kennlinie = new KaeltemaschinenKennlinie(new[] { (25.0, 7.0, (double?)4.0, (double?)20.0) })
            };
            foreach (double last in new[] { 0.5, 2.0, 3.999, 4.0, 10.0, 20.0, 35.0 })
            {
                var heute = km.Stunde(0, last);
                var neu = t.Stunde(last, 20.0, 4.0, 20.0, 0.2);
                Assert.Equal(heute.KaelteKwh, neu.KaelteKwh);
                Assert.Equal(heute.VerdichterKwh, neu.VerdichterKwh);
                Assert.Equal(heute.Takt, neu.Takt);
                Assert.Equal(0.0, neu.MehrstromKwh);
            }
        }

        [Theory]
        [InlineData("EIN_AUS")]
        [InlineData("STUFEN")]
        [InlineData("drehzahl")]
        public void Kurve_ohne_Beiwerte_nimmt_die_Vorgabekurve_der_Regelung(string regelung)
        {
            var t = Kaeltemaschinenteillast.Bilden("KURVE", null, null, null, null, null, regelung, null, 0.2);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Vorgabekurve, t.Herkunft);
            KaeltemaschineTeillastkurve.Kurve v = KaeltemaschineTeillastkurve.Vorgabekurve(regelung.ToUpperInvariant()).Value;
            Assert.Equal(v.A, t.A, 12);
            Assert.Equal(v.B, t.B, 12);
            Assert.Equal(v.C, t.C, 12);
            Assert.Equal(0.2, t.UntereGueltigkeit, 12);
        }

        [Fact]
        public void Kurve_ohne_Beiwerte_und_ohne_Regelung_rechnet_linear()
        {
            var t = Kaeltemaschinenteillast.Bilden("KURVE", null, null, null, null, null, null, null, 0.2);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Linear, t.Herkunft);
            Assert.Equal(1.0, t.G(0.5), 12);
        }

        [Fact]
        public void Unplausible_oder_halbe_Kurve_faellt_auf_linear_zurueck()
        {
            // EIRFPLR(1) = 1,5 liegt außerhalb 0,9 … 1,1.
            var t = Kaeltemaschinenteillast.Bilden("KURVE", 0.5, 0.5, 0.5, 0.2, null, "STUFEN", null, 0.2);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Verworfen, t.Herkunft);
            Assert.True(t.Linear);
            Assert.Equal(2.5, t.Stunde(10.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, 12);
            // Takten bleibt mit Verlust (Weg gesetzt).
            Assert.Equal(0.550, t.Stunde(2.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, TOL);

            var halb = Kaeltemaschinenteillast.Bilden("KURVE", 0.1, null, 0.3, 0.2, null, null, null, 0.2);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Verworfen, halb.Herkunft);
        }

        [Fact]
        public void Normierung_haelt_Volllast_auch_bei_EIRFPLR1_ungleich_1()
        {
            // EIRFPLR(1) = 1,05: Volllast trifft dennoch das Kennfeld, Teillast nach E(x) = EIRFPLR(x) / 1,05.
            var t = Kaeltemaschinenteillast.Bilden("KURVE", 0.105, 0.63, 0.315, 0.2, null, null, null, 0.2);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Kurve, t.Herkunft);
            Assert.Equal(1.0, t.E(1.0), 12);
            Assert.Equal(5.0, t.Stunde(20.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, 12);
            Assert.Equal(2.375, t.Stunde(10.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, TOL);
        }

        [Fact]
        public void Unter_der_Kurvengueltigkeit_bleibt_das_Guetemass_an_x_u()
        {
            // Mindestteillast 10 %, x_u 0,3: zwischen 0,1 und 0,3 kein Takt, g bleibt bei g(0,3).
            var t = Kaeltemaschinenteillast.Bilden("KURVE", 0.10, 0.60, 0.30, 0.3, null, null, null, 0.1);
            Assert.Equal(t.G(0.3), t.G(0.15), 12);
            var s = t.Stunde(3.0, 20.0, 4.0, 20.0, 0.1);
            Assert.False(s.Takt);
            Assert.Equal(3.0 / (4.0 * t.G(0.3)), s.VerdichterKwh, 12);
        }

        [Fact]
        public void AusModell_liest_die_acht_Felder_und_die_Mindestteillast()
        {
            var m = new KaeltemaschineModel
            {
                Teillast_Weg = "KURVE", Teillastkurve_a = 0.10, Teillastkurve_b = 0.60, Teillastkurve_c = 0.30,
                Mindestteillast_Prozent = 20, Taktverlustfaktor_Cd = null, Kennfeld_Randweg = "GUETEGRAD"
            };
            var t = Kaeltemaschinenteillast.AusModell(m);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Kurve, t.Herkunft);
            Assert.Equal(0.2, t.UntereGueltigkeit, 12);
            Assert.Equal(KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD, t.Randweg);
            Assert.Equal(0.638, t.Stunde(2.0, 20.0, 4.0, 20.0, 0.2).VerdichterKwh, TOL);
            Assert.Equal(KaeltemaschineTeillastSchema.RANDWEG_RANDWERT, Kaeltemaschinenteillast.AusModell(new KaeltemaschineModel()).Randweg);
        }

        // ---- Fachkonzept 3.4 ----

        /// <summary>Kennfeld mit den Randpunkten der Zahlenbeispiele: 12 °C / 30 °C EER 5,0 und 7 °C / 45 °C EER 2,6.</summary>
        private static KaeltemaschinenKennlinie Kennfeld() => new KaeltemaschinenKennlinie(new[]
        {
            (30.0, 7.0, (double?)4.4, (double?)20.0),
            (45.0, 7.0, (double?)2.6, (double?)16.0),
            (30.0, 12.0, (double?)5.0, (double?)22.0),
            (45.0, 12.0, (double?)3.0, (double?)18.0)
        });

        [Fact]
        public void Guetegrad_Kaltwasser_ueber_dem_Rand_trifft_das_Zahlenbeispiel()
        {
            Assert.Equal(0.3156, KaeltemaschinenRand.Guetegrad(5.0, 12.0, 30.0), 4);
            var p = KaeltemaschinenRand.Auswerten(Kennfeld(), 30.0, 16.0, "GUETEGRAD");
            Assert.Equal(6.52, p.Eer, 2);
            Assert.Equal(22.0, p.LeistungKw, 12);
            Assert.True(p.Randwert);
            Assert.True(p.Extrapoliert);
        }

        [Fact]
        public void Guetegrad_Rueckkuehlung_ueber_dem_Rand_trifft_das_Zahlenbeispiel()
        {
            Assert.Equal(0.3527, KaeltemaschinenRand.Guetegrad(2.6, 7.0, 45.0), 4);
            var p = KaeltemaschinenRand.Auswerten(Kennfeld(), 50.0, 7.0, "GUETEGRAD");
            Assert.Equal(2.30, p.Eer, 2);
            Assert.Equal(16.0, p.LeistungKw, 12);
            Assert.True(p.Extrapoliert);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("RANDWERT")]
        public void Randwert_bleibt_unveraendert(string randweg)
        {
            var k = Kennfeld();
            foreach (var (rk, kw) in new[] { (30.0, 16.0), (50.0, 7.0), (20.0, 3.0), (35.0, 9.0) })
            {
                KaeltemaschinenPunkt heute = k.Auswerten(rk, kw);
                var p = KaeltemaschinenRand.Auswerten(k, rk, kw, randweg);
                Assert.Equal(heute.Eer, p.Eer);
                Assert.Equal(heute.LeistungKw, p.LeistungKw);
                Assert.Equal(heute.Randwert, p.Randwert);
                Assert.False(p.Extrapoliert);
            }
        }

        [Fact]
        public void Guetegrad_ist_am_Rand_stetig_und_innen_unveraendert()
        {
            var k = Kennfeld();
            Assert.Equal(k.Auswerten(30.0, 12.0).Eer, KaeltemaschinenRand.Auswerten(k, 30.0, 12.0, "GUETEGRAD").Eer, 12);
            Assert.Equal(k.Auswerten(30.0, 12.0).Eer, KaeltemaschinenRand.Auswerten(k, 30.0, 12.0 + 1e-9, "GUETEGRAD").Eer, 6);
            Assert.Equal(k.Auswerten(45.0, 7.0).Eer, KaeltemaschinenRand.Auswerten(k, 45.0 + 1e-9, 7.0, "GUETEGRAD").Eer, 6);
            var innen = KaeltemaschinenRand.Auswerten(k, 35.0, 9.0, "GUETEGRAD");
            Assert.Equal(k.Auswerten(35.0, 9.0).Eer, innen.Eer);
            Assert.False(innen.Randwert);
            Assert.False(innen.Extrapoliert);
        }

        [Fact]
        public void Guetegrad_begrenzt_die_Weite_auf_10_K_und_deckelt_auf_15()
        {
            var k = Kennfeld();
            double bei10 = KaeltemaschinenRand.Auswerten(k, 60.0, 7.0, "GUETEGRAD").Eer;
            Assert.Equal(bei10, KaeltemaschinenRand.Auswerten(k, 70.0, 7.0, "GUETEGRAD").Eer, 12);
            Assert.Equal(KaeltemaschinenRand.EerFortgesetzt(2.6, 7.0, 45.0, 7.0, 55.0), bei10, 12);
            // Kaltwasser 16 °C bei Rückkühlung 20 °C (beide Achsen außerhalb): Carnot-Hub am Mindesthub, EER gedeckelt.
            Assert.Equal(KaelteFestwerte.FREIE_KUEHLUNG_EER, KaeltemaschinenRand.Auswerten(k, 20.0, 16.0, "GUETEGRAD").Eer, 12);
            Assert.Equal((12.0 + 273.15) / 5.0, KaeltemaschinenRand.CarnotEer(12.0, 14.0), 12);
        }

        // ---- Fachkonzept 3.6 ----

        [Fact]
        public void Folgeschaltung_schaltet_so_viele_Maschinen_wie_noetig()
        {
            Assert.Equal((1, 15.0), Kaeltemaschinenteillast.Folgeschaltung(15.0, 20.0, 2));
            Assert.Equal((1, 20.0), Kaeltemaschinenteillast.Folgeschaltung(20.0, 20.0, 2));
            Assert.Equal((2, 15.0), Kaeltemaschinenteillast.Folgeschaltung(30.0, 20.0, 2));
            Assert.Equal((2, 25.0), Kaeltemaschinenteillast.Folgeschaltung(50.0, 20.0, 2));
            Assert.Equal((0, 0.0), Kaeltemaschinenteillast.Folgeschaltung(0.0, 20.0, 2));
            Assert.Equal((1, 5.0), Kaeltemaschinenteillast.Folgeschaltung(5.0, 20.0, 0));
        }
    }
}
