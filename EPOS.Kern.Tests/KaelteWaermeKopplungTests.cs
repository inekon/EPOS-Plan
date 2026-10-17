using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KWN‑1a — Rechenproben der Kälte-Wärme-Kopplung an der Kältemaschine</b> ohne Datenbank (Kühlkonzept 5.6.3,
    /// 5.6.4, 5.6.8): Handrechnung je Führung, Schnittmenge an beiden Grenzen, Hub- und Gerätegrenze, Teillast und Takt,
    /// kein Bedarf, Luftgerät, Energie- und Stromprobe, Gesamtleistungszahl, Jahresreihe und Determinismus.
    /// </summary>
    public sealed class KaelteWaermeKopplungTests
    {
        private const double TOL = 1e-9;

        /// <summary>Kennlinie mit festem EER 3 und 100 kW auf dem Raster Rückkühlung 25…65 °C, Kaltwasser 6…18 °C.</summary>
        private static KaeltemaschinenKennlinie Fest() => new KaeltemaschinenKennlinie(new[]
        {
            (25.0, 6.0, (double?)3.0, (double?)100.0), (65.0, 6.0, (double?)3.0, (double?)100.0),
            (25.0, 18.0, (double?)3.0, (double?)100.0), (65.0, 18.0, (double?)3.0, (double?)100.0)
        });

        /// <summary>Kennlinie mit EER 5 bei 25 °C und 1 bei 65 °C Rückkühlung, 100 kW.</summary>
        private static KaeltemaschinenKennlinie Fallend() => new KaeltemaschinenKennlinie(new[]
        {
            (25.0, 6.0, (double?)5.0, (double?)100.0), (65.0, 6.0, (double?)1.0, (double?)100.0),
            (25.0, 18.0, (double?)5.0, (double?)100.0), (65.0, 18.0, (double?)1.0, (double?)100.0)
        });

        private static Kaeltemaschine Maschine(KaeltemaschinenKennlinie k = null, string art = null, double mindest = 0.0,
                                               int anzahl = 1, Kaeltemaschinenteillast teillast = null) =>
            new Kaeltemaschine
            {
                Rueckkuehlart = art ?? KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER,
                NennleistungKw = 100.0,
                Mindestteillast = mindest,
                Anzahl = anzahl,
                HilfsstromRueckkuehlungKw = 2.0,
                Kaltwassertemperatur = 6.0,
                Kennlinie = k ?? Fest(),
                Teillast = teillast
            };

        private static KaelteWaermeKopplung Kopplung(Kaeltemaschine m, KopplungsFuehrung f = KopplungsFuehrung.Schnittmenge,
                                                     double? kondMax = null, double? gr = null) =>
            new KaelteWaermeKopplung(m, f, kondMax, gr);

        /// <summary>Eine Stunde: Kaltwasser 6/12 °C, Wärme 50/40 °C, Rückkühlung 30 °C.</summary>
        private static KopplungsEingabe Eingabe(double kBed, double wBed, double wVl = 50.0, double rk = 30.0) =>
            new KopplungsEingabe(kBed, 6.0, 12.0, wBed, wVl, wVl - 10.0, rk);

        // ---- Handrechnung je Führung ----

        [Fact]
        public void Schnittmenge_bei_Kaelteueberhang_koppelt_die_Waermeseite_und_rechnet_den_Rest_einzeln()
        {
            KopplungsStunde s = Kopplung(Maschine()).Stunde(Eingabe(60.0, 40.0));
            Assert.Null(s.Grund);
            Assert.True(s.Gekoppelt);
            Assert.Equal(53.0, s.KondensatorC, 12);
            Assert.Equal(47.0, s.HubK, 12);
            // Q_K = 40 · 3/4 = 30, P = 10, Q_W = 40 — kein Überschuss.
            Assert.Equal(30.0, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(10.0, s.StromGekoppeltKwh, TOL);
            Assert.Equal(40.0, s.WaermeGenutztKwh, TOL);
            Assert.Equal(0.0, s.UeberschussWaermeKwh, 12);
            Assert.Equal(0.0, s.RestwaermeKwh, 12);
            // Restbetrieb: τ_kop = 0,3; die übrigen 30 kWh im Einzelbetrieb (EER 3, Hilfsstrom 2 kW · 0,3).
            Assert.Equal(0.3, s.ZeitanteilKopplung, 12);
            Assert.Equal(30.0, s.KaelteEinzelKwh, TOL);
            Assert.Equal(10.0 + 0.6, s.StromEinzelKwh, TOL);
            Assert.Equal(0.3, s.ZeitanteilEinzel, 12);
            Assert.Equal(0.0, s.RestkaelteKwh, 12);
        }

        [Fact]
        public void Schnittmenge_bei_Waermeueberhang_koppelt_die_ganze_Kaelte()
        {
            KopplungsStunde s = Kopplung(Maschine()).Stunde(Eingabe(30.0, 100.0));
            Assert.Equal(30.0, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(40.0, s.WaermeGenutztKwh, TOL);
            Assert.Equal(60.0, s.RestwaermeKwh, TOL);
            Assert.Equal(0.0, s.KaelteEinzelKwh, 12);
            Assert.Equal(0.0, s.RestkaelteKwh, 12);
            Assert.False(s.AnGrenze);
        }

        [Fact]
        public void Schnittmenge_mit_fallender_Kennlinie_trifft_die_Handrechnung()
        {
            // Kondensator 53 °C: EER = 5 − 4 · 28/40 = 2,2; Q_K = 50 · 2,2/3,2 = 34,375; P = 15,625.
            KopplungsStunde s = Kopplung(Maschine(Fallend())).Stunde(Eingabe(100.0, 50.0));
            Assert.Equal(2.2, s.Eer, 9);
            Assert.Equal(34.375, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(15.625, s.StromGekoppeltKwh, TOL);
            Assert.Equal(50.0, s.WaermeGenutztKwh, TOL);
            Assert.Equal(2.0 * 2.2 + 1.0, s.Gesamtleistungszahl, 9);
        }

        [Fact]
        public void Kaeltegefuehrt_koppelt_die_ganze_Kaelte_und_weist_den_Waermeueberschuss_aus()
        {
            KopplungsStunde s = Kopplung(Maschine(), KopplungsFuehrung.Kaeltegefuehrt).Stunde(Eingabe(60.0, 40.0));
            Assert.Null(s.Grund);
            Assert.Equal(60.0, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(20.0, s.StromGekoppeltKwh, TOL);
            Assert.Equal(40.0, s.WaermeGenutztKwh, TOL);
            Assert.Equal(40.0, s.UeberschussWaermeKwh, TOL);
            Assert.Equal(80.0, s.WaermeKondensatorKwh, TOL);
            // Hilfsstrom der Rückkühlung 2 kW · 0,6 Laufanteil, davon die Hälfte für die Überschusswärme.
            Assert.Equal(0.6, s.HilfsstromUeberschussKwh, TOL);
            Assert.Equal(0.0, s.RestkaelteKwh, 12);
            Assert.Equal(0.0, s.RestwaermeKwh, 12);
        }

        [Fact]
        public void Waermegefuehrt_teilt_rein_mit_Kaelteueberschuss_und_ist_an_der_Kaeltemaschine_benannt_abgelehnt()
        {
            Kopplungsaufteilung a = KaelteWaermeKopplung.Aufteilen(KopplungsFuehrung.Waermegefuehrt, 20.0, 40.0, 3.0, 100.0);
            Assert.Equal(40.0, a.WaermeErzeugtKwh, TOL);
            Assert.Equal(30.0, a.KaelteErzeugtKwh, TOL);
            Assert.Equal(10.0, a.StromKwh, TOL);
            Assert.Equal(20.0, a.KaelteGenutztKwh, TOL);
            Assert.Equal(10.0, a.UeberschussKaelteKwh, TOL);

            KopplungsStunde s = Kopplung(Maschine(), KopplungsFuehrung.Waermegefuehrt).Stunde(Eingabe(20.0, 40.0));
            Assert.Equal(KaelteWaermeKopplung.GRUND_FUEHRUNG, s.Grund);
            Assert.Equal(0.0, s.KaelteGekoppeltKwh, 12);
            Assert.Equal(20.0, s.KaelteEinzelKwh, TOL);
            Assert.Equal(40.0, s.RestwaermeKwh, TOL);
        }

        [Fact]
        public void Aufteilung_der_Schnittmenge_an_beiden_Grenzen_ohne_Ueberschuss()
        {
            Kopplungsaufteilung w = KaelteWaermeKopplung.Aufteilen(KopplungsFuehrung.Schnittmenge, 60.0, 40.0, 3.0, 100.0);
            Assert.Equal(40.0, w.WaermeErzeugtKwh, 12);
            Assert.Equal(30.0, w.KaelteErzeugtKwh, TOL);
            Kopplungsaufteilung k = KaelteWaermeKopplung.Aufteilen(KopplungsFuehrung.Schnittmenge, 30.0, 100.0, 3.0, 100.0);
            Assert.Equal(30.0, k.KaelteErzeugtKwh, 12);
            Assert.Equal(40.0, k.WaermeErzeugtKwh, TOL);
            foreach (var a in new[] { w, k })
            {
                Assert.Equal(0.0, a.UeberschussKaelteKwh, 12);
                Assert.Equal(0.0, a.UeberschussWaermeKwh, 12);
                Assert.Equal(a.KaelteErzeugtKwh + a.StromKwh, a.WaermeErzeugtKwh, 12);
            }
        }

        // ---- Grenzen ----

        [Fact]
        public void Hubgrenze_der_Eingabe_verhindert_die_Kopplung()
        {
            // Vorlauf 55 + 3 K = 58 °C über der Grenze 55 °C.
            KopplungsStunde s = Kopplung(Maschine(), kondMax: 55.0).Stunde(Eingabe(60.0, 40.0, wVl: 55.0));
            Assert.Equal(KaelteWaermeKopplung.GRUND_HUB, s.Grund);
            Assert.Equal(0.0, s.KaelteGekoppeltKwh, 12);
            Assert.Equal(0.0, s.ZeitanteilKopplung, 12);
            Assert.Equal(60.0, s.KaelteEinzelKwh, TOL);
            Assert.Equal(40.0, s.RestwaermeKwh, TOL);
            // Genau an der Grenze koppelt sie: 52 + 3 = 55 °C.
            Assert.Null(Kopplung(Maschine(), kondMax: 55.0).Stunde(Eingabe(60.0, 40.0, wVl: 52.0)).Grund);
            // Grädigkeit der Eingabe statt des Festwerts.
            Assert.Null(Kopplung(Maschine(), kondMax: 55.0, gr: 0.0).Stunde(Eingabe(60.0, 40.0, wVl: 55.0)).Grund);
        }

        [Fact]
        public void Ohne_Hubgrenze_gilt_der_Rand_der_Kennlinie_mit_Guetegrad_um_die_Extrapolationsweite_darueber()
        {
            // 63 + 3 = 66 °C liegt über dem Kennlinienrand 65 °C.
            Assert.Equal(KaelteWaermeKopplung.GRUND_HUB,
                         Kopplung(Maschine(Fallend())).Stunde(Eingabe(60.0, 40.0, wVl: 63.0)).Grund);
            var guete = Kaeltemaschinenteillast.Bilden(null, null, null, null, null, null, null,
                                                       KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD, 0.0);
            KopplungsStunde s = Kopplung(Maschine(Fallend(), teillast: guete)).Stunde(Eingabe(60.0, 40.0, wVl: 63.0));
            Assert.Null(s.Grund);
            Assert.True(s.Extrapoliert);
            Assert.True(s.Randwert);
            Assert.True(s.Eer > 0 && s.Eer < 1.0);
            // 73 + 3 = 76 °C liegt auch über 65 + 10 °C.
            Assert.Equal(KaelteWaermeKopplung.GRUND_HUB,
                         Kopplung(Maschine(Fallend(), teillast: guete)).Stunde(Eingabe(60.0, 40.0, wVl: 73.0)).Grund);
        }

        [Fact]
        public void Kein_positiver_Hub_oder_Kaltwasser_unter_der_Verdampfergrenze_koppelt_nicht()
        {
            var e = new KopplungsEingabe(60.0, 20.0, 25.0, 40.0, 15.0, 10.0, 30.0);
            Assert.Equal(KaelteWaermeKopplung.GRUND_HUB, Kopplung(Maschine()).Stunde(e).Grund);
            Kaeltemaschine m = Maschine();
            m.KaltwasserVorlaufMinC = 8.0;
            Assert.Equal(KaelteWaermeKopplung.GRUND_HUB, Kopplung(m).Stunde(Eingabe(60.0, 40.0)).Grund);
        }

        [Fact]
        public void Geraetegrenze_begrenzt_die_Kopplung_und_laesst_keinen_Restbetrieb()
        {
            KopplungsStunde s = Kopplung(Maschine()).Stunde(Eingabe(500.0, 1000.0));
            Assert.True(s.AnGrenze);
            Assert.Equal(100.0, s.KapazitaetKw, 12);
            Assert.Equal(100.0, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(1.0, s.ZeitanteilKopplung, 12);
            Assert.Equal(0.0, s.KaelteEinzelKwh, 12);
            Assert.Equal(400.0, s.RestkaelteKwh, TOL);
            Assert.Equal(1000.0 - 400.0 / 3.0, s.RestwaermeKwh, TOL);

            KopplungsStunde zwei = Kopplung(Maschine(anzahl: 2)).Stunde(Eingabe(500.0, 1000.0));
            Assert.Equal(200.0, zwei.KapazitaetKw, 12);
            Assert.Equal(200.0, zwei.KaelteGekoppeltKwh, TOL);
        }

        [Fact]
        public void Ohne_Kennlinie_meldet_die_Kopplung_die_Grenze()
        {
            Kaeltemaschine m = Maschine();
            m.Kennlinie = null;
            Assert.Equal(KaelteWaermeKopplung.GRUND_GRENZE, Kopplung(m).Stunde(Eingabe(60.0, 40.0)).Grund);
        }

        // ---- Teillast und Takt ----

        [Fact]
        public void Unter_der_Mindestteillast_taktet_der_Bestandsweg_ohne_Mehrstrom()
        {
            KopplungsStunde s = Kopplung(Maschine(mindest: 0.3)).Stunde(Eingabe(10.0, 100.0));
            Assert.True(s.Takt);
            Assert.Equal(10.0, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(10.0 / 3.0, s.StromGekoppeltKwh, TOL);
            Assert.Equal(0.0, s.MehrstromKwh, 12);
        }

        [Fact]
        public void Mit_Teillastkurve_senkt_die_Schnittmenge_den_Anteil_einmal_auf_den_Waermebedarf()
        {
            var kurve = Kaeltemaschinenteillast.Bilden(KaeltemaschineTeillastSchema.WEG_KURVE, 0.10, 0.60, 0.30, 0.2,
                                                       null, null, null, 0.2);
            double wBed = 10.0 * 4.0 / 3.0;
            KopplungsStunde s = Kopplung(Maschine(mindest: 0.2, teillast: kurve)).Stunde(Eingabe(100.0, wBed));
            Assert.Null(s.Grund);
            Assert.True(s.Takt);
            Assert.True(s.Korrigiert);
            Assert.True(s.MehrstromKwh > 0);
            Assert.True(s.Eer < 3.0);
            Assert.True(s.KaelteGekoppeltKwh < 10.0);
            Assert.Equal(wBed, s.WaermeGenutztKwh, 12);
            Assert.Equal(0.0, s.UeberschussWaermeKwh, 12);
            Assert.Equal(0.0, s.RestwaermeKwh, 12);
        }

        // ---- Bedarf und Gerät ----

        [Fact]
        public void Ohne_Bedarf_einer_Seite_koppelt_nichts()
        {
            KopplungsStunde ohneWaerme = Kopplung(Maschine()).Stunde(Eingabe(60.0, 0.0));
            Assert.Equal(KaelteWaermeKopplung.GRUND_KEIN_BEDARF, ohneWaerme.Grund);
            Assert.False(ohneWaerme.Gekoppelt);
            Assert.Equal(60.0, ohneWaerme.KaelteEinzelKwh, TOL);

            KopplungsStunde ohneKaelte = Kopplung(Maschine()).Stunde(Eingabe(0.0, 40.0));
            Assert.Equal(KaelteWaermeKopplung.GRUND_KEIN_BEDARF, ohneKaelte.Grund);
            Assert.Equal(40.0, ohneKaelte.RestwaermeKwh, 12);
            Assert.Equal(0.0, ohneKaelte.StromEinzelKwh, 12);
        }

        [Fact]
        public void Luftgekuehlte_Maschine_ist_benannt_abgelehnt_und_kuehlt_einzeln()
        {
            KopplungsStunde s = Kopplung(Maschine(art: KaeltemaschineSchema.RUECKKUEHLART_LUFT)).Stunde(Eingabe(60.0, 40.0));
            Assert.Equal(KaelteWaermeKopplung.GRUND_LUFTGERAET, s.Grund);
            Assert.Equal(0.0, s.KaelteGekoppeltKwh, 12);
            Assert.Equal(60.0, s.KaelteEinzelKwh, TOL);
            Assert.Equal(20.0 + 1.2, s.StromEinzelKwh, TOL);
            Assert.Equal(40.0, s.RestwaermeKwh, TOL);
        }

        [Fact]
        public void Ohne_Rueckkuehltemperatur_geht_der_Kaelterest_an_die_Kaeltefolge()
        {
            KopplungsStunde s = Kopplung(Maschine()).Stunde(Eingabe(60.0, 40.0, rk: double.NaN));
            Assert.Equal(30.0, s.KaelteGekoppeltKwh, TOL);
            Assert.Equal(0.0, s.KaelteEinzelKwh, 12);
            Assert.Equal(30.0, s.RestkaelteKwh, TOL);
        }

        [Fact]
        public void Fuehrung_liest_die_Persistenzwerte()
        {
            Assert.Equal(KopplungsFuehrung.Schnittmenge, KaelteWaermeKopplung.FuehrungLesen(null, out bool u0));
            Assert.False(u0);
            Assert.Equal(KopplungsFuehrung.Kaeltegefuehrt, KaelteWaermeKopplung.FuehrungLesen("kaelte", out _));
            Assert.Equal(KopplungsFuehrung.Waermegefuehrt, KaelteWaermeKopplung.FuehrungLesen("WAERMEGEFUEHRT", out _));
            Assert.Equal(KopplungsFuehrung.Schnittmenge, KaelteWaermeKopplung.FuehrungLesen("XY", out bool u1));
            Assert.True(u1);
        }

        // ---- Proben ----

        [Theory]
        [InlineData(KopplungsFuehrung.Schnittmenge)]
        [InlineData(KopplungsFuehrung.Kaeltegefuehrt)]
        public void Energieprobe_und_Stromaufteilung_je_Stunde(KopplungsFuehrung f)
        {
            var kurve = Kaeltemaschinenteillast.Bilden(KaeltemaschineTeillastSchema.WEG_KURVE, 0.10, 0.60, 0.30, 0.2,
                                                       null, null, null, 0.2);
            foreach (Kaeltemaschine m in new[] { Maschine(Fallend(), mindest: 0.3), Maschine(Fallend(), mindest: 0.2, teillast: kurve) })
            {
                var k = Kopplung(m, f);
                foreach (double kBed in new[] { 3.0, 17.5, 60.0, 140.0 })
                foreach (double wBed in new[] { 2.0, 25.0, 80.0, 300.0 })
                foreach (double wVl in new[] { 35.0, 50.0, 60.0 })
                {
                    KopplungsStunde s = k.Stunde(Eingabe(kBed, wBed, wVl));
                    Assert.Null(s.Grund);
                    // Q_W = Q_K + P_el − Q_V
                    Assert.Equal(s.KaelteGekoppeltKwh + s.UeberschussKaelteKwh + s.StromGekoppeltKwh - s.WaermeverlustKwh,
                                 s.WaermeKondensatorKwh, 9);
                    Assert.Equal(s.StromGekoppeltKwh, s.StromAnteilWaermeKwh + s.StromAnteilKaelteKwh, 12);
                    Assert.Equal(s.Gesamtleistungszahl, s.WaermeGenutztKwh / s.StromAnteilWaermeKwh, 9);
                    Assert.Equal(s.Gesamtleistungszahl, s.KaelteGekoppeltKwh / s.StromAnteilKaelteKwh, 9);
                    Assert.True(s.ZeitanteilKopplung + s.ZeitanteilEinzel <= 1.0 + 1e-12);
                    Assert.Equal(kBed, s.KaelteGekoppeltKwh + s.KaelteEinzelKwh + s.RestkaelteKwh, 9);
                    Assert.Equal(wBed, s.WaermeGenutztKwh + s.RestwaermeKwh, 9);
                    if (f == KopplungsFuehrung.Schnittmenge) Assert.Equal(0.0, s.UeberschussWaermeKwh, 12);
                }
            }
        }

        [Fact]
        public void Gesamtleistungszahl_ist_zwei_EER_plus_eins_und_zwei_COP_minus_eins()
        {
            KopplungsStunde s = Kopplung(Maschine()).Stunde(Eingabe(60.0, 40.0));
            double eer = s.Eer, cop = s.WaermeGenutztKwh / s.StromGekoppeltKwh;
            Assert.Equal(3.0, eer, 12);
            Assert.Equal(eer + 1.0, cop, 9);
            Assert.Equal(2.0 * eer + 1.0, s.Gesamtleistungszahl, 9);
            Assert.Equal(2.0 * cop - 1.0, s.Gesamtleistungszahl, 9);
        }

        [Fact]
        public void Jahresreihe_summiert_in_MWh_und_zaehlt_Kopplungs_und_Hubstunden()
        {
            const int n = 8760;
            double[] k = Reihe(n, h => 60.0), w = Reihe(n, h => 40.0);
            double[] kwVl = Reihe(n, h => 6.0), kwRl = Reihe(n, h => 12.0), rk = Reihe(n, h => 30.0);
            double[] wVl = Reihe(n, h => h < 8000 ? 50.0 : 60.0), wRl = Reihe(n, h => 40.0);
            KopplungsJahr j = Kopplung(Maschine(), kondMax: 55.0).Jahr(k, kwVl, kwRl, w, wVl, wRl, rk);
            Assert.Equal(8000, j.Kopplungsstunden);
            Assert.Equal(760, j.Hubstunden);
            Assert.Equal(30.0 * 8000 / 1000.0, j.KaelteGekoppeltMwh, 9);
            Assert.Equal(40.0 * 8000 / 1000.0, j.WaermeGenutztMwh, 9);
            Assert.Equal(10.0 * 8000 / 1000.0, j.StromGekoppeltMwh, 9);
            Assert.Equal(7.0, j.Gesamtleistungszahl, 9);
            Assert.Equal(60.0, j.KaelteEinzelKwh[8500], 9);
            Assert.Equal(KaelteWaermeKopplung.GRUND_HUB, j.Grund[8500]);
        }

        [Fact]
        public void Jahresreihe_ist_deterministisch()
        {
            const int n = 8760;
            double[] k = Reihe(n, h => 20.0 + (h % 24) * 3.5), w = Reihe(n, h => 5.0 + (h % 168) * 0.7);
            double[] kwVl = Reihe(n, h => 6.0 + (h % 5)), kwRl = Reihe(n, h => 12.0 + (h % 5));
            double[] wVl = Reihe(n, h => 35.0 + (h % 45)), wRl = Reihe(n, h => 30.0), rk = Reihe(n, h => 15.0 + (h % 20));
            var kurve = Kaeltemaschinenteillast.Bilden(KaeltemaschineTeillastSchema.WEG_KURVE, 0.10, 0.60, 0.30, 0.2,
                                                       null, null, KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD, 0.2);
            KopplungsJahr a = Kopplung(Maschine(Fallend(), mindest: 0.2, teillast: kurve)).Jahr(k, kwVl, kwRl, w, wVl, wRl, rk);
            KopplungsJahr b = Kopplung(Maschine(Fallend(), mindest: 0.2, teillast: kurve)).Jahr(k, kwVl, kwRl, w, wVl, wRl, rk);
            Assert.True(a.Kopplungsstunden > 0);
            Assert.True(a.Hubstunden > 0);
            for (int h = 0; h < n; h++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(a.KaelteGekoppeltKwh[h]), BitConverter.DoubleToInt64Bits(b.KaelteGekoppeltKwh[h]));
                Assert.Equal(BitConverter.DoubleToInt64Bits(a.StromGekoppeltKwh[h]), BitConverter.DoubleToInt64Bits(b.StromGekoppeltKwh[h]));
                Assert.Equal(BitConverter.DoubleToInt64Bits(a.KaelteEinzelKwh[h]), BitConverter.DoubleToInt64Bits(b.KaelteEinzelKwh[h]));
            }
            Assert.Equal(BitConverter.DoubleToInt64Bits(a.WaermeGenutztMwh), BitConverter.DoubleToInt64Bits(b.WaermeGenutztMwh));
        }

        private static double[] Reihe(int n, Func<int, double> f)
        {
            var r = new double[n];
            for (int h = 0; h < n; h++) r[h] = f(h);
            return r;
        }
    }
}
