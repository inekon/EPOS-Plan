using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rechenproben des Rückkühlwerks ohne Datenbank</b> (K-F2; Entwurf Split/VRF/Rückkühlwerk 5.3, 5.4, 5.6): je Bauart
    /// die Bezugstemperatur (trocken, Kühlturm über die Feuchtkugel, adiabat und hybrid unter und über der Schaltgrenze, ohne
    /// Feuchte), die lastabhängige Annäherung (Nennpunkt, Teillast 0, Überlast, ein Schritt), der Ventilator je Regelung samt
    /// kleinster Drehzahl und Stufen, keine Doppelzählung des Hilfsstroms, die Wasserbilanz gegen die Handrechnung und die
    /// Wasserkosten mit und ohne Preis.
    /// </summary>
    public class RueckkuehlwerkRechenwegTests
    {
        private static long Bits(double d) => BitConverter.DoubleToInt64Bits(d);

        private static Rueckkuehlwerk Rk(RueckkuehlwerkModel m) => Rueckkuehlwerk.AusModell(m);

        /// <summary>Eine Kältemaschine mit Kennlinie über die Rückkühltemperatur 20…40 °C bei Kaltwasser 7 °C.</summary>
        private static Kaeltemaschine Maschine(double hilfsstromKw = 0.0, int anzahl = 1)
        {
            var punkte = new List<(double, double, double?, double?)>
            {
                (20.0, 7.0, 6.0, 100.0), (30.0, 7.0, 4.5, 90.0), (40.0, 7.0, 3.0, 80.0),
            };
            return new Kaeltemaschine
            {
                Bezeichner = "KM", Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, NennleistungKw = 90,
                Kaltwassertemperatur = 7.0, HilfsstromRueckkuehlungKw = hilfsstromKw, Anzahl = anzahl,
                Kennlinie = new KaeltemaschinenKennlinie(punkte),
            };
        }

        /// <summary>Setzt das Rückkühlwerk und bildet die Rückkühltemperaturen aus einer Außentemperatur je Stunde.</summary>
        private static void Ansetzen(Kaeltemaschine k, Rueckkuehlwerk r, double[] aussen, double[] feuchte = null)
        {
            k.RueckkuehlwerkSetzen(r);
            k.Rueckkuehltemperatur_stuendlich = k.RueckkuehltemperaturenBilden(aussen, feuchte, out _);
        }

        // ------------------------------------------------------------------
        //  Bezugstemperatur je Bauart
        // ------------------------------------------------------------------

        [Fact]
        public void Trocken_rechnet_Aussenluft_plus_Annaeherung()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = "TROCKEN", Annaeherung_Nenn_K = 8 });
            Assert.Equal(38.0, r.Rueckkuehltemperatur(30.0, 40.0, out _, out bool bef, out double vk, out bool ohne));
            Assert.False(bef);
            Assert.Equal(0.0, vk);
            Assert.False(ohne);
            Assert.False(r.Verdunstungskuehlanlage);
        }

        [Theory]
        [InlineData("KUEHLTURM_OFFEN")]
        [InlineData("KUEHLTURM_GESCHLOSSEN")]
        public void Kuehlturm_rechnet_die_Feuchtkugel(string bauart)
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = bauart });
            double tw = Kaeltemaschine.Feuchtkugeltemperatur(28.0, 45.0);
            Assert.Equal(Bits(tw + KaelteFestwerte.GRAEDIGKEIT_NASSKUEHLER_K), Bits(r.Rueckkuehltemperatur(28.0, 45.0, out bool ohne)));
            Assert.False(ohne);
            Assert.True(r.NassInStunde(0));
            Assert.True(r.Verdunstungskuehlanlage);
            // Ohne Feuchte: der heutige Ausweichweg, benannt.
            Assert.Equal(28.0 - KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K, r.Rueckkuehltemperatur(28.0, null, out bool ohne2));
            Assert.True(ohne2);
        }

        [Fact]
        public void Adiabat_befeuchtet_erst_ueber_der_Schaltgrenze()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = "ADIABAT" });
            Assert.Equal(KaelteFestwerte.RKW_BEFEUCHTUNG_AB_C, r.BefeuchtungAbC);
            Assert.Equal(KaelteFestwerte.RKW_BEFEUCHTUNG_WIRKUNGSGRAD, r.BefeuchtungWirkungsgrad);
            // Unter (und an) der Schaltgrenze trocken wie ein Trockenkühler.
            foreach (double t in new[] { 10.0, 25.0 })
            {
                Assert.Equal(Bits(t + KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K),
                             Bits(r.Rueckkuehltemperatur(t, 40.0, out _, out bool bef, out _, out _)));
                Assert.False(bef);
            }
            // Darüber: Lufteintritt = Trocken − η · (Trocken − Feuchtkugel).
            double tw = Kaeltemaschine.Feuchtkugeltemperatur(32.0, 40.0);
            double erwartet = 32.0 - 0.8 * (32.0 - tw) + 10.0;
            Assert.Equal(erwartet, r.Rueckkuehltemperatur(32.0, 40.0, out _, out bool bef2, out double vk, out bool ohne), 12);
            Assert.True(bef2);
            Assert.Equal(0.8 * (32.0 - tw), vk, 12);
            Assert.False(ohne);
            Assert.True(32.0 + 10.0 - erwartet > 3.0, "Die Befeuchtung kühlt spürbar vor.");
        }

        [Fact]
        public void Hybrid_faehrt_trocken_bis_zur_eigenen_Schaltgrenze()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = "HYBRID", Befeuchtung_Ab_C = 28, Befeuchtung_Wirkungsgrad = 0.6 });
            Assert.Equal(37.0, r.Rueckkuehltemperatur(27.0, 40.0, out _, out bool b1, out _, out _));
            Assert.False(b1);
            double tw = Kaeltemaschine.Feuchtkugeltemperatur(30.0, 40.0);
            Assert.Equal(30.0 - 0.6 * (30.0 - tw) + 10.0, r.Rueckkuehltemperatur(30.0, 40.0, out _, out bool b2, out _, out _), 12);
            Assert.True(b2);
        }

        [Fact]
        public void Fehlt_die_Feuchte_rechnet_adiabat_trocken_und_zaehlt_die_Stunden()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = "ADIABAT" });
            double[] aussen = { 20.0, 30.0, 33.0, 24.0 };
            double[] rk = r.RueckkuehltemperaturenBilden(aussen, null, out int ohneNass);
            Assert.Equal(new[] { 30.0, 40.0, 43.0, 34.0 }, rk);
            Assert.Equal(0, ohneNass);
            Assert.Equal(2, r.StundenOhneFeuchteTrocken);
            Assert.False(r.NassInStunde(1));
            // Mit Feuchte in einer Stunde NaN: nur diese Stunde trocken.
            double[] rk2 = r.RueckkuehltemperaturenBilden(aussen, new[] { 50.0, 50.0, double.NaN, 50.0 }, out _);
            Assert.Equal(1, r.StundenOhneFeuchteTrocken);
            Assert.True(r.NassInStunde(1));
            Assert.True(rk2[1] < 40.0);
        }

        // ------------------------------------------------------------------
        //  Lastabhängige Annäherung
        // ------------------------------------------------------------------

        [Fact]
        public void Lastabhaengig_Nennpunkt_trifft_den_Festwert_Teillast_0_die_Bezugstemperatur()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 100 });
            double rkNenn = 37.3;
            Assert.Equal(Bits(rkNenn), Bits(r.RueckkuehltemperaturLastabhaengig(rkNenn, 1.0)));
            Assert.Equal(37.3 - 10.0, r.RueckkuehltemperaturLastabhaengig(rkNenn, 0.0), 12);
            Assert.Equal(37.3 - 10.0 + 5.0, r.RueckkuehltemperaturLastabhaengig(rkNenn, 0.5), 12);
            // FEST: immer der Festwert.
            Rueckkuehlwerk fest = Rk(new RueckkuehlwerkModel { Nennleistung_kW = 100 });
            Assert.Equal(Bits(rkNenn), Bits(fest.RueckkuehltemperaturLastabhaengig(rkNenn, 0.3)));
        }

        [Fact]
        public void Ueberlast_hebt_die_Annaeherung_linear_bis_zur_Grenze()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 100 });
            Assert.Equal(30.0 - 10.0 + 15.0, r.RueckkuehltemperaturLastabhaengig(30.0, 1.5), 12);
            Assert.Equal(30.0 - 10.0 + RueckkuehlwerkSchema.ANNAEHERUNG_MAX_K, r.RueckkuehltemperaturLastabhaengig(30.0, 10.0), 12);
            Assert.True(r.Stunde(0, 150, 100, 120, 35.0).Ueberlast);
            Assert.False(r.Stunde(0, 100, 80, 120, 35.0).Ueberlast);
        }

        [Fact]
        public void Drehzahl_haelt_die_Nenn_Annaeherung_Ein_Aus_und_Stufen_folgen_der_Last()
        {
            Rueckkuehlwerk dz = Rk(new RueckkuehlwerkModel
            {
                Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 100, Ventilator_Regelung = "DREHZAHL", Ventilator_Drehzahl_Min = 0.25
            });
            Assert.Equal(Bits(30.0), Bits(dz.RueckkuehltemperaturLastabhaengig(30.0, 0.6)));
            Assert.Equal(Bits(30.0), Bits(dz.RueckkuehltemperaturLastabhaengig(30.0, 0.25)));
            // Unter der kleinsten Drehzahl läuft mehr Luft als nötig: die Annäherung sinkt (x / 0,25).
            Assert.Equal(20.0 + 10.0 * 0.1 / 0.25, dz.RueckkuehltemperaturLastabhaengig(30.0, 0.1), 12);

            Rueckkuehlwerk st = Rk(new RueckkuehlwerkModel
            {
                Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 100, Ventilator_Regelung = "STUFEN", Ventilator_Stufen = 4
            });
            Assert.Equal(20.0 + 10.0 * 0.6 / 0.75, st.RueckkuehltemperaturLastabhaengig(30.0, 0.6), 12);
            Rueckkuehlwerk ea = Rk(new RueckkuehlwerkModel { Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 100 });
            Assert.Equal(26.0, ea.RueckkuehltemperaturLastabhaengig(30.0, 0.6), 12);
        }

        [Fact]
        public void Lastabhaengig_rechnet_genau_einen_Schritt_deterministisch()
        {
            var aussen = new[] { 22.0 };
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 120 });
            Kaeltemaschine k = Maschine();
            Ansetzen(k, r, aussen);
            KaeltemaschinenStunde a = k.Stunde(0, 50.0, 7.0), b = k.Stunde(0, 50.0, 7.0);
            Assert.Equal(Bits(a.VerdichterKwh), Bits(b.VerdichterKwh));
            Assert.Equal(Bits(a.Rueckkuehlung.RueckkuehlC), Bits(b.Rueckkuehlung.RueckkuehlC));

            // Handrechnung des einen Schritts: Schritt 1 bei der Nenn-Annäherung (rk 32 °C), dann die Stunde bei der
            // lastabhängigen Rückkühltemperatur - das Ergebnis gleicht einer Maschine ohne Rückkühlwerk an dieser Temperatur.
            Kaeltemaschine ohne = Maschine();
            ohne.Rueckkuehltemperatur_stuendlich = new[] { 32.0 };
            KaeltemaschinenStunde s0 = ohne.Stunde(0, 50.0, 7.0);
            double x0 = (s0.KaelteKwh + s0.VerdichterKwh) / 120.0;
            double rk1 = 32.0 - 10.0 + 10.0 * x0;
            Assert.Equal(rk1, a.Rueckkuehlung.RueckkuehlC, 12);
            ohne.Rueckkuehltemperatur_stuendlich = new[] { a.Rueckkuehlung.RueckkuehlC };
            KaeltemaschinenStunde s1 = ohne.Stunde(0, 50.0, 7.0);
            Assert.Equal(Bits(s1.VerdichterKwh), Bits(a.VerdichterKwh));
            Assert.True(a.VerdichterKwh < s0.VerdichterKwh, "Teillast senkt die Rückkühltemperatur und den Strom.");
            // Die Bilanz trägt die Abwärme der zweiten Rechnung.
            Assert.Equal(a.KaelteKwh + a.VerdichterKwh, a.Rueckkuehlung.AbwaermeKwh, 12);
        }

        [Fact]
        public void Rueckkuehlwerk_mit_leeren_Feldern_rechnet_die_Stunde_bitgleich_zur_Maschine_ohne()
        {
            var aussen = new[] { 5.0, 18.0, 31.0 };
            foreach (double hilfs in new[] { 0.0, 4.0 })
                foreach (int anzahl in new[] { 1, 3 })
                {
                    Kaeltemaschine ohne = Maschine(hilfs, anzahl);
                    ohne.Rueckkuehltemperatur_stuendlich = ohne.RueckkuehltemperaturenBilden(aussen, null, out _);
                    Kaeltemaschine mit = Maschine(hilfs, anzahl);
                    Ansetzen(mit, Rk(new RueckkuehlwerkModel { Bauart = "TROCKEN", Annaeherung_Weg = "FEST", Freikuehlung_Schaltung = "PARALLEL" }), aussen);
                    for (int h = 0; h < aussen.Length; h++)
                        foreach (double last in new[] { 0.0, 20.0, 400.0 })
                        {
                            KaeltemaschinenStunde a = ohne.Stunde(h, last, 7.0), b = mit.Stunde(h, last, 7.0);
                            Assert.Equal(Bits(a.KaelteKwh), Bits(b.KaelteKwh));
                            Assert.Equal(Bits(a.VerdichterKwh), Bits(b.VerdichterKwh));
                            Assert.Equal(Bits(a.HilfsstromKwh), Bits(b.HilfsstromKwh));
                            Assert.Equal(a.FreieKuehlung, b.FreieKuehlung);
                            Assert.Equal(last > 0, b.MitRueckkuehlwerk);
                            if (last > 0) Assert.Equal(0.0, b.Rueckkuehlung.WasserM3);
                        }
                }
        }

        // ------------------------------------------------------------------
        //  Ventilator
        // ------------------------------------------------------------------

        [Fact]
        public void Ventilator_je_Regelung_samt_kleinster_Drehzahl_und_Stufen()
        {
            Rueckkuehlwerk ea = Rk(new RueckkuehlwerkModel { Ventilator_Nenn_kW = 4 });
            Assert.Equal(RueckkuehlwerkSchema.REGELUNG_EIN_AUS, ea.Regelung);
            Assert.Equal(1.6, ea.VentilatorKwh(0.4), 12);
            Assert.Equal(4.0, ea.VentilatorKwh(1.7), 12);
            Assert.Equal(0.0, ea.VentilatorKwh(0.0));

            Rueckkuehlwerk st = Rk(new RueckkuehlwerkModel { Ventilator_Nenn_kW = 6, Ventilator_Regelung = "STUFEN", Ventilator_Stufen = 3 });
            Assert.Equal(4.0, st.VentilatorKwh(0.4), 12);            // nächsthöhere Stufe 2/3
            Assert.Equal(2.0, st.VentilatorKwh(1.0 / 3.0), 12);      // genau eine Stufe, nicht die nächste
            Assert.Equal(2.0, st.VentilatorKwh(0.01), 12);           // mindestens die erste Stufe
            Assert.Equal(6.0, st.VentilatorKwh(0.9), 12);
            Rueckkuehlwerk st0 = Rk(new RueckkuehlwerkModel { Ventilator_Nenn_kW = 6, Ventilator_Regelung = "STUFEN" });
            Assert.Equal(KaelteFestwerte.RKW_VENTILATOR_STUFEN, st0.Stufen);
            Assert.Equal(3.0, st0.VentilatorKwh(0.3), 12);

            Rueckkuehlwerk dz = Rk(new RueckkuehlwerkModel { Ventilator_Nenn_kW = 8, Ventilator_Regelung = "DREHZAHL" });
            Assert.Equal(KaelteFestwerte.RKW_VENTILATOR_DREHZAHL_MIN, dz.DrehzahlMin);
            Assert.Equal(1.0, dz.VentilatorKwh(0.5), 12);                         // 8 · 0,5³
            Assert.Equal(8.0 * 0.2 * 0.2 * 0.2, dz.VentilatorKwh(0.05), 12);      // nie unter der kleinsten Drehzahl
            Assert.Equal(8.0, dz.VentilatorKwh(1.4), 12);

            Assert.Equal(0.0, Rk(new RueckkuehlwerkModel { Ventilator_Regelung = "DREHZAHL" }).VentilatorKwh(0.5));
        }

        [Fact]
        public void Mit_Ventilator_entfaellt_der_Hilfsstrom_der_Rueckkuehlung_keine_Doppelzaehlung()
        {
            var aussen = new[] { 24.0 };
            Kaeltemaschine k = Maschine(hilfsstromKw: 5.0);
            Ansetzen(k, Rk(new RueckkuehlwerkModel { Ventilator_Nenn_kW = 2.0, Nennleistung_kW = 150 }), aussen);
            KaeltemaschinenStunde s = k.Stunde(0, 40.0, 7.0);
            double x = (s.KaelteKwh + s.VerdichterKwh) / 150.0;
            Assert.Equal(2.0 * x, s.HilfsstromKwh, 12);
            Assert.Equal(s.HilfsstromKwh, s.Rueckkuehlung.VentilatorKwh);

            // Ohne Nennleistung: der Lastanteil der Maschinen - EIN_AUS rechnet dann wie der Hilfsstrom × Laufanteil.
            Kaeltemaschine k2 = Maschine(hilfsstromKw: 5.0);
            Ansetzen(k2, Rk(new RueckkuehlwerkModel { Ventilator_Nenn_kW = 2.0 }), aussen);
            KaeltemaschinenStunde s2 = k2.Stunde(0, 40.0, 7.0);
            Assert.Equal(2.0 * s2.KaelteKwh / s2.KapazitaetKw, s2.HilfsstromKwh, 12);
        }

        // ------------------------------------------------------------------
        //  Wasser und Kosten
        // ------------------------------------------------------------------

        [Fact]
        public void Wasserbilanz_des_Kuehlturms_gegen_die_Handrechnung()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = "KUEHLTURM_OFFEN", Nennleistung_kW = 200 });
            RueckkuehlwerkStunde s = r.Stunde(0, 100.0, 80.0, 120.0, 30.0);
            // 100 kWh · 3 600 kJ/kWh / 2 450 kJ/kg = 146,94 kg; × 0,9 = 132,24 kg = 0,13224 m³.
            Assert.Equal(0.13224489795918368, s.VerdunstungM3, 12);
            Assert.Equal(0.13224489795918368 / 2.0, s.AbflutungM3, 12);       // Eindickung 3
            // Drift 0,01 % des Umlaufs: 100 · 3 600 / (4,19 · 5) = 17 183,77 kg = 17,18 m³.
            Assert.Equal(0.0001 * 100.0 * 3600.0 / (4.19 * 5.0) / 1000.0, s.DriftM3, 15);
            Assert.True(s.Nass);
            Assert.Equal(s.VerdunstungM3 + s.AbflutungM3 + s.DriftM3, s.WasserM3, 15);

            Rueckkuehlwerk g = Rk(new RueckkuehlwerkModel { Bauart = "KUEHLTURM_GESCHLOSSEN", Verdunstung_Faktor = 0.8, Eindickung = 5, Drift_Anteil = 0 });
            RueckkuehlwerkStunde t = g.Stunde(0, 245.0, 200.0, 250.0, 30.0);
            Assert.Equal(245.0 * 3600.0 / 2450.0 / 1000.0 * 0.8, t.VerdunstungM3, 12);   // 0,288 m³
            Assert.Equal(t.VerdunstungM3 / 4.0, t.AbflutungM3, 12);
            Assert.Equal(0.0, t.DriftM3);
        }

        [Fact]
        public void Wasserbilanz_adiabat_nur_in_Nassstunden_aus_der_Vorkuehlwaerme()
        {
            Rueckkuehlwerk r = Rk(new RueckkuehlwerkModel { Bauart = "ADIABAT" });
            r.RueckkuehltemperaturenBilden(new[] { 20.0, 34.0 }, new[] { 60.0, 60.0 }, out _);
            RueckkuehlwerkStunde trocken = r.Stunde(0, 100.0, 80.0, 120.0, 30.0);
            Assert.False(trocken.Nass);
            Assert.Equal(0.0, trocken.WasserM3);
            RueckkuehlwerkStunde nass = r.Stunde(1, 100.0, 80.0, 120.0, 40.0);
            Assert.True(nass.Nass);
            double vk = r.VorkuehlungK(1);
            Assert.True(vk > 0 && vk < KaelteFestwerte.RKW_LUFTERWAERMUNG_K, "Vorkühlung unter der Lufterwärmung - keine Kappung.");
            double erwartet = 100.0 * vk / KaelteFestwerte.RKW_LUFTERWAERMUNG_K * 3600.0 / 2450.0 / 1000.0;
            Assert.Equal(erwartet, nass.VerdunstungM3, 12);
            Assert.Equal(erwartet / 2.0, nass.AbflutungM3, 12);
            Assert.Equal(0.0, nass.DriftM3);   // Vorgabe 0 an Matten und Besprühung
            Rueckkuehlwerk d = Rk(new RueckkuehlwerkModel { Bauart = "HYBRID", Drift_Anteil = 0.01 });
            d.RueckkuehltemperaturenBilden(new[] { 34.0 }, new[] { 30.0 }, out _);
            RueckkuehlwerkStunde dh = d.Stunde(0, 100.0, 80.0, 120.0, 40.0);
            Assert.Equal(0.01 * dh.VerdunstungM3, dh.DriftM3, 15);
        }

        [Fact]
        public void Trocken_verbraucht_kein_Wasser()
        {
            RueckkuehlwerkStunde s = Rk(new RueckkuehlwerkModel { Bauart = "TROCKEN", Drift_Anteil = 0.1 }).Stunde(0, 100.0, 80.0, 120.0, 30.0);
            Assert.False(s.Nass);
            Assert.Equal(0.0, s.WasserM3);
        }

        [Fact]
        public void Wasserkosten_mit_und_ohne_Preis()
        {
            Assert.Equal(25.0, Rueckkuehlwerk.WasserkostenEur(10.0, 2.5));
            Assert.Null(Rueckkuehlwerk.WasserkostenEur(10.0, null));
            Assert.Null(Rueckkuehlwerk.WasserkostenEur(10.0, double.NaN));
        }

        [Fact]
        public void Freie_Kuehlung_rechnet_die_Nenn_Annaeherung_und_fuehrt_die_Kaelte_ab()
        {
            var aussen = new[] { -5.0 };
            Kaeltemaschine k = Maschine();
            Ansetzen(k, Rk(new RueckkuehlwerkModel { Bauart = "KUEHLTURM_OFFEN", Annaeherung_Weg = "LASTABHAENGIG", Nennleistung_kW = 200 }), aussen);
            KaeltemaschinenStunde s = k.Stunde(0, 30.0, 7.0);
            Assert.True(s.FreieKuehlung);
            Assert.Equal(Bits(k.Rueckkuehltemperatur_stuendlich[0]), Bits(s.Rueckkuehlung.RueckkuehlC));
            Assert.Equal(s.KaelteKwh, s.Rueckkuehlung.AbwaermeKwh);
        }

        // ------------------------------------------------------------------
        //  Kennzahlen
        // ------------------------------------------------------------------

        private static double? KennzahlWert(string schluessel, params ErgebnisKaeltemaschineModel[] km)
        {
            var e = new ErgebnisModel();
            e.Kaeltemaschinen.AddRange(km);
            return KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel).Wert(new VariantenDaten { Ergebnis = e });
        }

        [Fact]
        public void Kennzahlen_summieren_die_Maschinen_mit_Rueckkuehlwerk_und_fehlen_ohne()
        {
            var a = new ErgebnisKaeltemaschineModel { Bezeichner = "A", Ventilatorstrom_MWh = 1.25, Wasser_m3 = 300 };
            var b = new ErgebnisKaeltemaschineModel { Bezeichner = "B", Ventilatorstrom_MWh = 0.5, Wasser_m3 = 0 };
            var ohne = new ErgebnisKaeltemaschineModel { Bezeichner = "C" };
            Assert.Equal(1.75, KennzahlWert(KennzahlenKatalog.SCHLUESSEL_RK_VENTILATOR, a, b, ohne)!.Value, 12);
            Assert.Equal(300.0, KennzahlWert(KennzahlenKatalog.SCHLUESSEL_RK_WASSER, a, b, ohne));
            Assert.Null(KennzahlWert(KennzahlenKatalog.SCHLUESSEL_RK_VENTILATOR, ohne));
            Assert.Null(KennzahlWert(KennzahlenKatalog.SCHLUESSEL_RK_WASSER));
            foreach (Kennzahl k in KennzahlenKatalog.Alle().Where(k => k.Schluessel.StartsWith("kaelte.rk.", StringComparison.Ordinal)))
            {
                Assert.NotEqual(k.LabelDe, k.LabelEn);
                Assert.Equal(19, k.Seit);
                Assert.False(string.IsNullOrEmpty(k.Einheit));
            }
        }
    }
}
