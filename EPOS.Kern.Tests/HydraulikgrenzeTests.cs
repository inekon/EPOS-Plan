using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hydraulikgrenze</b> (Übergabegrenze UB‑E3; Fachkonzept 4.4, 2.8, 8.1 Zeilen 7, 10, 15): Höchstspreizung
    /// direkt, Weiche mit gemischtem Vorlauf, Mindestvolumenstrom als Höchstspreizung mit Überströmventil, Puffer an der
    /// Ladeseite; dazu die Übergabegrenze am gemischten Vorlauf. Ohne Datenbank.
    /// </summary>
    public class HydraulikgrenzeTests
    {
        private static Hydraulikeingang Eingang(Einbindungsart art, double wh, double vorlauf, double ruecklauf,
                                                double nennKw, double kennfeldKw, double anteil = 0.0,
                                                double sigmaMax = 10.0, double hoechstvorlauf = 95.0)
            => new Hydraulikeingang
            {
                Einbindung = art, WH = wh, VorlaufWpC = Math.Min(vorlauf, hoechstvorlauf), RuecklaufHeizkreisC = ruecklauf,
                PufferUntenC = double.NaN, HoechstvorlaufC = hoechstvorlauf, NennleistungKw = nennKw, KennfeldKw = kennfeldKw,
                SpreizungAuslegungK = 5.0, SpreizungMaxK = sigmaMax, SpreizungMinK = 3.0, MindestvolumenstromAnteil = anteil,
            };

        private static Uebergabezone Heizkoerper() => new Uebergabezone(10.0, 75.0, 60.0, 20.0, 1.3);

        [Fact]
        public void Hoechstspreizung_direkt_90_70_an_sigma_max_10()
        {
            // W_H = 10 kW/20 K = 0,5 kW/K; ohne Mindestvolumenstrom: Φ = W_H·10 K = 5 kW.
            Hydraulikergebnis e = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Direkt, 0.5, 90.0, 70.0, double.NaN, 30.0));
            Assert.Equal(5.0, e.LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.SpreizungMax, e.Grund);
            Assert.Equal(70.0, e.RuecklaufWpC, 12);
            Assert.False(e.Taktet);

            // Gegenprobe 80/75: 5 K unter σ_max — keine Begrenzung.
            Hydraulikergebnis frei = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Direkt, 0.5, 80.0, 75.0, double.NaN, 30.0));
            Assert.True(double.IsPositiveInfinity(frei.LeistungKw));
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, frei.Grund);
        }

        [Fact]
        public void Hoechstspreizung_wirkt_ueber_das_Bivalenzmodul_als_Grund_SPREIZUNG_MAX()
        {
            Geraetegrenzen g = Geraetegrenzen.Bilden(Geraetespalten.Leer, 95.0);
            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, null, 95.0, g.SpreizungMinK, false,
                                          Bivalenzbetriebsart.Parallel, true, true, g, "DIREKT", double.NaN);
            Bereichsergebnis b = modul.Bereich(0, true, Verfuegbarkeitsgrund.KeineBegrenzung, 90.0, 70.0, 20.0, 30.0);
            Assert.Equal(Betriebsbereich.Parallel, b.Bereich);
            Assert.Equal(10.0 / 15.0 * 10.0, b.LeistungKw, 9);
            Assert.Equal(Verfuegbarkeitsgrund.SpreizungMax, b.Grund);
        }

        [Fact]
        public void Weiche_mit_kleinerem_Waermepumpenstrom_mischt_den_Vorlauf_und_liefert_weniger_als_direkt()
        {
            // W_H 1 kW/K, Φ_N 4 kW, σ_A 5 K → ṁ_WP·c_p 0,8 kW/K < ṁ_HK; ṁ_min 0,48 kW/K, σ_max,eff = min(10, 4/0,48) = 8,33 K.
            Hydraulikergebnis w = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Weiche, 1.0, 50.0, 40.0, 4.0, 4.0, 0.6, 10.0, 55.0));
            double sigmaEff = 4.0 / 0.48;
            Assert.Equal(sigmaEff, w.SpreizungMaxEffK, 12);
            Assert.Equal(0.8 * sigmaEff, w.LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.SpreizungMax, w.Grund);
            double vorlaufWp = 40.0 + 0.8 * sigmaEff / 0.8;
            Assert.Equal((0.8 * vorlaufWp + 0.2 * 40.0) / 1.0, w.VorlaufHeizkreisC, 9);
            Assert.Equal(40.0, w.RuecklaufWpC, 12);

            Hydraulikergebnis d = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Direkt, 1.0, 50.0, 40.0, 4.0, 4.0, 0.6, 10.0, 55.0));
            Assert.True(w.LeistungKw < d.LeistungKw);

            // Der Hub bis θ_WP,max bindet: Rücklauf 50 °C, Höchstvorlauf 55 °C → ṁ_WP·5 K = 4 kW, Grund Höchstvorlauf.
            Hydraulikergebnis hub = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Weiche, 1.0, 60.0, 50.0, 4.0, 4.0, 0.6, 10.0, 55.0));
            Assert.Equal(4.0, hub.LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.UebergabeHoechstvorlauf, hub.Grund);
        }

        [Fact]
        public void Weiche_mit_groesserem_Waermepumpenstrom_mischt_den_Ruecklauf()
        {
            // ṁ_WP 2 kW/K ≥ ṁ_HK 0,5 kW/K: keine σ-Grenze aus dem Heizkreis, Rücklauf (0,5·40 + 1,5·45)/2 = 43,75 °C.
            Hydraulikergebnis e = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Weiche, 0.5, 45.0, 40.0, 10.0, 10.0, 0.6, 10.0, 55.0));
            Assert.True(double.IsPositiveInfinity(e.LeistungKw));
            Assert.Equal(43.75, e.RuecklaufWpC, 12);
            Assert.Equal(45.0, e.VorlaufHeizkreisC, 12);
        }

        [Fact]
        public void Mindestvolumenstrom_als_Hoechstspreizung_und_Ueberstroemventil()
        {
            // Φ_N 10 kW, σ_A 5 K → ṁ_N 2 kW/K, ṁ_min 1,2 kW/K.
            Assert.Equal(5.0, Hydraulikgrenze.SpreizungMaxEff(10.0, 6.0, 1.2), 12);
            Assert.Equal(10.0, Hydraulikgrenze.SpreizungMaxEff(10.0, 15.0, 1.2), 12);
            Assert.Equal(10.0, Hydraulikgrenze.SpreizungMaxEff(10.0, 15.0, 0.0), 12);

            // Überströmventil, ṁ_HK 0,5 < ṁ_min 1,2: Φ_HK = 3 kW < 0,36·Φ_N = 3,6 kW → taktet; Rücklauf gemischt.
            Hydraulikergebnis takt = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Direkt, 0.5, 45.0, 39.0, 10.0, 10.0, 0.6, 10.0, 55.0));
            Assert.True(takt.Taktet);
            Assert.Equal((0.5 * 39.0 + 0.7 * 45.0) / 1.2, takt.RuecklaufWpC, 12);
            Assert.True(double.IsPositiveInfinity(takt.LeistungKw));

            // Φ_HK = 4 kW ≥ 3,6 kW: taktet nicht.
            Hydraulikergebnis lauf = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Direkt, 0.5, 45.0, 37.0, 10.0, 10.0, 0.6, 10.0, 55.0));
            Assert.False(lauf.Taktet);
            Assert.Equal((0.5 * 37.0 + 0.7 * 45.0) / 1.2, lauf.RuecklaufWpC, 12);

            // Ohne Beimischung (ṁ_HK ≥ ṁ_min) unter σ_min: die Stunde taktet ebenfalls.
            Hydraulikergebnis klein = Hydraulikgrenze.Rechnen(Eingang(Einbindungsart.Direkt, 2.0, 41.0, 39.0, 10.0, 10.0, 0.6, 10.0, 55.0));
            Assert.True(klein.Taktet);
            Assert.Equal(39.0, klein.RuecklaufWpC, 12);
        }

        [Fact]
        public void Puffer_rechnet_die_Ladeseite_und_liest_die_unterste_Zone()
        {
            Hydraulikeingang e = Eingang(Einbindungsart.Puffer, 0.5, 50.0, 40.0, 10.0, 30.0, 0.6) with { PufferUntenC = 33.0 };
            Hydraulikergebnis p = Hydraulikgrenze.Rechnen(e);
            // ṁ_N·c_p 2 kW/K · σ_max,eff min(10, 30/1,2) = 20 kW < Φ_KF 30 kW.
            Assert.Equal(20.0, p.LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.SpreizungMax, p.Grund);
            Assert.Equal(33.0, p.RuecklaufWpC, 12);
        }

        [Fact]
        public void Puffer_wird_nie_ueber_den_Hoechstvorlauf_geladen()
        {
            Geraetegrenzen g = Geraetegrenzen.Bilden(Geraetespalten.Leer, 55.0);
            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, null, 55.0, g.SpreizungMinK, false,
                                          Bivalenzbetriebsart.Parallel, true, true, g, "PUFFER", 10.0);
            Bereichsergebnis b = modul.Bereich(0, true, Verfuegbarkeitsgrund.KeineBegrenzung, 65.0, 50.0, 8.0, 10.0, 40.0);
            Assert.Equal(Betriebsbereich.NurKessel, b.Bereich);
            Assert.Equal(0.0, b.LeistungKw);
        }

        [Fact]
        public void Uebergabegrenze_am_gemischten_Vorlauf_ist_eindeutig_und_kleiner()
        {
            Uebergabezone z = Heizkoerper();
            Zonengrenze ungemischt = Uebergabegrenze.Zone(z, 55.0, 20.0);
            Zonengrenze r1 = Uebergabegrenze.ZoneGemischt(z, 55.0, 20.0, 1.0);
            Assert.Equal(ungemischt.PhiUeMax, r1.PhiUeMax);
            Zonengrenze r = Uebergabegrenze.ZoneGemischt(z, 55.0, 20.0, 1.25);
            Assert.True(r.PhiUeMax < ungemischt.PhiUeMax);
            // Probe: Heizfläche bei θ_m = 55 − Φ·(r − ½)/W_H liefert Φ.
            double tm = 55.0 - r.PhiUeMax * 0.75 / z.WH;
            Assert.Equal(r.PhiUeMax, z.PhiN * Math.Pow((tm - 20.0) / z.DeltaThetaMN, z.Exponent), 9);
            Assert.Equal(55.0 - r.PhiUeMax * 1.25 / z.WH, r.RuecklaufC, 12);
        }
    }
}
