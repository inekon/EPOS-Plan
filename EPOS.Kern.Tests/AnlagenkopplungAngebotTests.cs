using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Angebotsfunktion und der Speicherleser</b> (AK3-W3a; Entwurf AK3 2.3 bis 2.5, Festlegungen 5 bis 8,
    /// E102 Q-AK3-3) — ohne Datenbank, mit Phantasiewerten: Angebot = Summe der Teile an Handfällen, Kapazität der
    /// Wärmepumpe am Vorlauf auf und zwischen den Stützstellen (Schalter an und aus), der Speicherleser verändert
    /// nichts, Determinismus.
    /// </summary>
    public class AnlagenkopplungAngebotTests
    {
        private const int H = 8760;

        // ---------------------------------------------------------------------------------------------
        //  Bausteine
        // ---------------------------------------------------------------------------------------------

        private static bool[] Nur(params int[] stunden)
        {
            var m = new bool[H];
            foreach (int h in stunden) m[h] = true;
            return m;
        }

        private static Anlagenzeitprogramm Programm(Func<int, double> faktor)
            => Anlagenzeitprogramm.Lesen(string.Join(";", Enumerable.Range(0, 168).Select(i =>
                   faktor(i).ToString(System.Globalization.CultureInfo.InvariantCulture))));

        private static SimulationWaermepumpe._Kenndaten Kurve(int vorlauf, params (double t, double cop, double p)[] punkte)
            => new SimulationWaermepumpe._Kenndaten
            {
                Vorlauf = vorlauf,
                anz = punkte.Length,
                dat = punkte.Select(x => new SimulationWaermepumpe._DAT { Temperatur = x.t, COP = x.cop, Leistung = x.p }).ToArray(),
            };

        /// <summary>Drei Kennlinien 35/45/55 °C, je drei Punkte (Quelle absteigend wie <c>KennlinienwahlLaden</c>).</summary>
        private static SimulationWaermepumpe._Kenndaten[] DreiKurven() => new[]
        {
            Kurve(35, (10, 5.0, 12.0), (0, 4.0, 10.0), (-10, 3.0, 8.0)),
            Kurve(45, (10, 4.0, 11.0), (0, 3.2, 9.0), (-10, 2.4, 7.0)),
            Kurve(55, (10, 3.0, 10.0), (0, 2.5, 8.0), (-10, 2.0, 6.0)),
        };

        private static double[] Konstant(double wert) => Enumerable.Repeat(wert, H).ToArray();

        private static WaermepumpeKapazitaet Wp(Fahrplanerzeuger f = null, double quelle = 0.0, bool extrapolation = true,
                                                bool? interpolieren = false, int anzahl = 1)
            => new WaermepumpeKapazitaet(f ?? new Fahrplanerzeuger { Bezeichner = "WP" }, DreiKurven(), null,
                                         new Quellprofil(Konstant(quelle)), extrapolation, anzahl, interpolieren);

        private static FesteKapazitaet Kessel(double kw, double vorlaufMax = double.NaN, bool[] gesperrt = null,
                                              Anlagenzeitprogramm zp = null)
            => new FesteKapazitaet(new Fahrplanerzeuger
            {
                Bezeichner = "Kessel", NennleistungKw = kw, VorlaufAngebotC = vorlaufMax, Gesperrt = gesperrt, Zeitprogramm = zp,
            });

        /// <summary>Ein Testdouble der Naht: fester Speicheranteil.</summary>
        private sealed class FesterSpeicher : ISpeicherangebot
        {
            private readonly double _kwh;
            internal FesterSpeicher(double kwh) { _kwh = kwh; }
            public bool Vorhanden => true;
            public double EntnehmbarKwh(int stunde) => _kwh;
        }

        private static SimulationPufferspeicher Puffer(double fuellungKwh, double entladeMax = 0.0)
        {
            var sp = new SimulationPufferspeicher();
            sp.Init(3000, 75, 35, 3.24);
            sp.EntladeleistungMax = entladeMax;
            sp.SchichtenAufbauen();
            if (fuellungKwh > 0) sp.Laden(fuellungKwh, 0);
            return sp;
        }

        private static readonly Stundenvorrang OhneVorrang = new Stundenvorrang(0, 0, 0, 0);

        // ---------------------------------------------------------------------------------------------
        //  Kapazität der Wärmepumpe am Vorlauf (Festlegungen 5, 8; I-1, I-2)
        // ---------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(35.0, 10.0)]
        [InlineData(45.0, 9.0)]
        [InlineData(55.0, 8.0)]
        public void Auf_der_Stuetzstelle_liefert_die_Kennlinie_ihr_Ptherm_mit_Schalter_an_und_aus(double vorlauf, double erwartet)
        {
            Assert.Equal(erwartet, Wp(interpolieren: false).Abfragen(100, vorlauf).VerfuegbarKw);
            Assert.Equal(erwartet, Wp(interpolieren: true).Abfragen(100, vorlauf).VerfuegbarKw);
        }

        [Fact]
        public void Zwischen_den_Stuetzstellen_naechste_Stuetzstelle_ohne_und_linear_mit_Schalter()
        {
            // 40 °C: Gleichstand → die höhere Stützstelle (H-F4), 9 kW; mit Schalter 10 + 0,5 · (9 − 10) = 9,5 kW.
            Assert.Equal(9.0, Wp(interpolieren: false).Abfragen(0, 40.0).VerfuegbarKw);
            Assert.Equal(9.5, Wp(interpolieren: true).Abfragen(0, 40.0).VerfuegbarKw, 12);
            // 48 °C: nächste 45 °C → 9 kW; linear 9 + 0,3 · (8 − 9) = 8,7 kW.
            Assert.Equal(9.0, Wp(interpolieren: false).Abfragen(0, 48.0).VerfuegbarKw);
            Assert.Equal(8.7, Wp(interpolieren: true).Abfragen(0, 48.0).VerfuegbarKw, 12);
        }

        [Fact]
        public void Der_Kernschalter_wird_beim_Aufbau_gelesen()
        {
            WaermepumpeKapazitaet ein;
            using (VorlaufInterpolation.Schalten(true)) ein = Wp(interpolieren: null);
            WaermepumpeKapazitaet aus;
            using (VorlaufInterpolation.Schalten(false)) aus = Wp(interpolieren: null);
            Assert.True(ein.Interpolieren);
            Assert.False(aus.Interpolieren);
            Assert.Equal(9.5, ein.Abfragen(0, 40.0).VerfuegbarKw, 12);
        }

        [Fact]
        public void Raender_nach_F_A8_und_verbotene_Extrapolation_stellt_den_Vorlauf_nicht()
        {
            Assert.Equal(10.0, Wp().Abfragen(0, 25.0).VerfuegbarKw);                         // darunter: unterste
            Assert.Equal(8.0, Wp(extrapolation: true).Abfragen(0, 65.0).VerfuegbarKw);       // darüber, erlaubt: oberste
            Erzeugerangebot verboten = Wp(extrapolation: false).Abfragen(0, 65.0);
            Assert.Equal(0.0, verboten.VerfuegbarKw);
            Assert.True(verboten.VorlaufNichtErreicht);
            Assert.Equal(55.0, verboten.VorlaufAngebotC);                                   // Festlegung 11
        }

        [Theory]
        [InlineData(5.0)]
        [InlineData(-3.0)]
        [InlineData(10.0)]
        [InlineData(14.0)]   // über der obersten Quellstützstelle: gekappt
        public void Die_Kapazitaet_an_der_Quelle_rechnet_wie_berechne_wptherm(double quelle)
        {
            var sim = new SimulationWaermepumpe();
            foreach (SimulationWaermepumpe._Kenndaten k in DreiKurven())
                Assert.Equal(sim.berechne_wptherm(quelle, null, k, -1)[2], WaermepumpeKapazitaet.Leistung(k, quelle, false));
        }

        [Fact]
        public void Unter_der_Quellachse_extrapoliert_oder_kappt_die_gekoppelte_Quelle()
        {
            SimulationWaermepumpe._Kenndaten k = DreiKurven()[0];
            Assert.Equal(6.0, WaermepumpeKapazitaet.Leistung(k, -20.0, false), 12);   // 8 + (−10) · (10 − 8) / 10
            Assert.Equal(8.0, WaermepumpeKapazitaet.Leistung(k, -20.0, true));
        }

        [Fact]
        public void Die_Quelltemperatur_kommt_aus_der_Naht_und_die_Anzahl_vervielfacht()
        {
            var reihe = Konstant(0.0);
            reihe[7] = 10.0;
            var wp = new WaermepumpeKapazitaet(new Fahrplanerzeuger(), DreiKurven(), null, new Quellprofil(reihe), true, 2, false);
            Assert.Equal(18.0, wp.Abfragen(6, 45.0).VerfuegbarKw);
            Assert.Equal(22.0, wp.Abfragen(7, 45.0).VerfuegbarKw);
        }

        [Fact]
        public void Ohne_Vorlauf_rechnet_die_projektierte_Kennlinie()
        {
            SimulationWaermepumpe._Kenndaten[] k = DreiKurven();
            var wp = new WaermepumpeKapazitaet(new Fahrplanerzeuger(), k, k[2], new Quellprofil(Konstant(0.0)), true, 1, false);
            Assert.Equal(8.0, wp.Abfragen(0, double.NaN).VerfuegbarKw);
        }

        // ---------------------------------------------------------------------------------------------
        //  Verfügbarkeit aus dem Fahrplan (Sperre, Zeitprogramm, Vorlauf_Max)
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void Sperre_nimmt_die_Waermepumpe_heraus_und_nennt_die_Sperrzeit()
        {
            var wp = Wp(new Fahrplanerzeuger { Gesperrt = Nur(3) });
            Erzeugerangebot a = wp.Abfragen(3, 45.0);
            Assert.Equal(9.0, a.KapazitaetKw);
            Assert.Equal(0.0, a.VerfuegbarKw);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, a.Grund);
            Assert.False(a.Freigegeben);
            Assert.Equal(9.0, wp.Abfragen(4, 45.0).VerfuegbarKw);
        }

        [Fact]
        public void Zeitprogramm_0_und_halber_Faktor()
        {
            var k = Kessel(20.0, zp: Programm(i => i == 2 ? 0.0 : i == 3 ? 0.5 : 1.0));
            Assert.Equal(0.0, k.Abfragen(2, 50.0).VerfuegbarKw);
            Assert.Equal(Verfuegbarkeitsgrund.Zeitprogramm, k.Abfragen(2, 50.0).Grund);
            Assert.Equal(10.0, k.Abfragen(3, 50.0).VerfuegbarKw);
            Assert.Equal(20.0, k.Abfragen(4, 50.0).VerfuegbarKw);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, k.Abfragen(4, 50.0).Grund);
        }

        [Fact]
        public void Vorlauf_Max_ueberschritten_liefert_nichts_ohne_Grund()
        {
            var k = Kessel(20.0, vorlaufMax: 50.0);
            Erzeugerangebot a = k.Abfragen(0, 55.0);
            Assert.Equal(0.0, a.VerfuegbarKw);
            Assert.True(a.VorlaufNichtErreicht);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, a.Grund);
            Assert.Equal(20.0, k.Abfragen(0, 50.0).VerfuegbarKw);   // auf der Grenze: liefert
        }

        // ---------------------------------------------------------------------------------------------
        //  Angebot = Summe der Teile (Festlegungen 5 bis 7)
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void Angebot_ist_die_Summe_der_Teile()
        {
            var erzeuger = new IErzeugerkapazitaet[] { Wp(new Fahrplanerzeuger { VorlaufAngebotC = 60.0 }), Kessel(20.0, vorlaufMax: 70.0) };
            Stundenangebot a = Angebotsfunktion.Angebot(0, 45.0, erzeuger, new FesterSpeicher(5.0), new Stundenvorrang(3, 1, 0.5, 2));
            Assert.Equal(29.0, a.ErzeugerKw);
            Assert.Equal(5.0, a.SpeicherKw);
            Assert.Equal(6.5, a.VorrangKw);
            Assert.Equal(27.5, a.LeistungKw);
            Assert.Equal(70.0, a.VorlaufC);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, a.Grund);
            Anlagenverfuegbarkeit v = a.AlsVerfuegbarkeit();
            Assert.Equal(27.5, v.LeistungKw);
            Assert.Equal(70.0, v.VorlaufC);
        }

        [Fact]
        public void Vorrang_ueber_der_Kapazitaet_gibt_0()
        {
            Stundenangebot a = Angebotsfunktion.Angebot(0, 45.0, new IErzeugerkapazitaet[] { Kessel(10.0) },
                                                        new FesterSpeicher(2.0), new Stundenvorrang(8, 3, 1, 0));
            Assert.Equal(0.0, a.LeistungKw);
        }

        [Fact]
        public void Negative_Vorrangteile_zaehlen_nicht()
            => Assert.Equal(4.0, new Stundenvorrang(-5, 4, double.NaN, double.PositiveInfinity).SummeKw);

        [Fact]
        public void Sperre_mit_Speicher_fuellt_die_Luecke_und_nennt_sonst_Speicher_leer()
        {
            var erzeuger = new IErzeugerkapazitaet[] { Wp(new Fahrplanerzeuger { Gesperrt = Nur(5) }), Kessel(4.0) };
            Stundenangebot reicht = Angebotsfunktion.Angebot(5, 45.0, erzeuger, new FesterSpeicher(12.0), OhneVorrang);
            Assert.Equal(16.0, reicht.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, reicht.Grund);

            Stundenangebot knapp = Angebotsfunktion.Angebot(5, 45.0, erzeuger, new FesterSpeicher(2.0), OhneVorrang);
            Assert.Equal(6.0, knapp.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.SpeicherLeer, knapp.Grund);

            Stundenangebot ohne = Angebotsfunktion.Angebot(5, 45.0, erzeuger, null, OhneVorrang);
            Assert.Equal(4.0, ohne.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, ohne.Grund);
        }

        [Fact]
        public void Ohne_Erzeuger_heisst_der_Grund_kein_Erzeuger_und_der_Speicher_traegt_nichts_wie_im_Fahrplan()
        {
            Stundenangebot a = Angebotsfunktion.Angebot(0, 45.0, Array.Empty<IErzeugerkapazitaet>(), new FesterSpeicher(3.0), OhneVorrang);
            Assert.Equal(Verfuegbarkeitsgrund.KeinErzeuger, a.Grund);
            Assert.Equal(0.0, a.LeistungKw);
            Assert.Equal(0.0, a.SpeicherKw);

            // Gegenprobe Fahrplan: ohne Erzeuger Leistung 0 trotz Vorrat.
            Anlagenfahrplan f = Anlagenfahrplan.Rechnen(Array.Empty<Fahrplanerzeuger>(), 3.0);
            Assert.Equal(f.Stunde(0).LeistungKw, a.LeistungKw);
            Assert.Equal(f.Stunde(0).Grund, a.Grund);
        }

        [Fact]
        public void Zeitprogramm_und_Abschaltpunkt_in_derselben_Stunde_teilen_den_Grund_wie_der_Fahrplan()
        {
            // Zwei Erzeuger: A mit Zeitprogramm 0,7 und Abschaltpunkt (Ausfall 0,3·10 Zeitprogramm, 0,7·10 Abschalt),
            // B mit Zeitprogramm 0,2 (Ausfall 0,8·10 Zeitprogramm). Summe Zeitprogramm 11 > Abschaltpunkt 7.
            var a = new Fahrplanerzeuger { Bezeichner = "A", NennleistungKw = 10.0, Zeitprogramm = Programm(_ => 0.7), Abgeschaltet = Nur(0) };
            var b = new Fahrplanerzeuger { Bezeichner = "B", NennleistungKw = 10.0, Zeitprogramm = Programm(_ => 0.2) };
            Anlagenfahrplan f = Anlagenfahrplan.Rechnen(new[] { a, b }, 0.0);
            Stundenangebot s = Angebotsfunktion.Angebot(0, double.NaN, new IErzeugerkapazitaet[] { new FesteKapazitaet(a), new FesteKapazitaet(b) },
                                                        null, OhneVorrang);
            Assert.Equal(Verfuegbarkeitsgrund.Zeitprogramm, f.Stunde(0).Grund);
            Assert.Equal(f.Stunde(0).Grund, s.Grund);
            Assert.Equal(f.Stunde(0).LeistungKw, s.LeistungKw, 12);

            // Gegenprobe: ohne B überwiegt der Abschaltpunkt (7 gegen 3) — in beiden.
            Anlagenfahrplan f1 = Anlagenfahrplan.Rechnen(new[] { a }, 0.0);
            Stundenangebot s1 = Angebotsfunktion.Angebot(0, double.NaN, new IErzeugerkapazitaet[] { new FesteKapazitaet(a) }, null, OhneVorrang);
            Assert.Equal(Verfuegbarkeitsgrund.Abschaltpunkt, f1.Stunde(0).Grund);
            Assert.Equal(f1.Stunde(0).Grund, s1.Grund);
        }

        [Fact]
        public void Verbotene_Extrapolation_verlaengert_die_Quellachse_nicht()
        {
            // Quelle −20 °C unter der untersten Stützstelle (−10 °C): erlaubt linear verlängert, verboten gekappt.
            double erlaubt = Wp(quelle: -20.0, extrapolation: true).Abfragen(0, 35.0).VerfuegbarKw;
            double verboten = Wp(quelle: -20.0, extrapolation: false).Abfragen(0, 35.0).VerfuegbarKw;
            Assert.Equal(6.0, erlaubt, 12);
            Assert.Equal(8.0, verboten, 12);
            // Gegenprobe: über der untersten Stützstelle rechnen beide gleich.
            Assert.Equal(Wp(quelle: -5.0, extrapolation: true).Abfragen(0, 35.0).VerfuegbarKw,
                         Wp(quelle: -5.0, extrapolation: false).Abfragen(0, 35.0).VerfuegbarKw);
        }

        [Fact]
        public void Vorlauf_der_Stunde_ist_das_hoechste_Angebot_der_freigegebenen_Erzeuger()
        {
            var erzeuger = new IErzeugerkapazitaet[] { Kessel(5.0, 60.0), Kessel(5.0, 80.0, gesperrt: Nur(1)) };
            Assert.Equal(80.0, Angebotsfunktion.Angebot(0, 45.0, erzeuger, null, OhneVorrang).VorlaufC);
            Assert.Equal(60.0, Angebotsfunktion.Angebot(1, 45.0, erzeuger, null, OhneVorrang).VorlaufC);
            var mitOffenem = new IErzeugerkapazitaet[] { Kessel(5.0, 60.0), Kessel(5.0) };
            Assert.True(double.IsNaN(Angebotsfunktion.Angebot(0, 45.0, mitOffenem, null, OhneVorrang).VorlaufC));
        }

        [Fact]
        public void Die_Abfrage_ist_deterministisch_und_ohne_Nebenwirkung()
        {
            var erzeuger = new IErzeugerkapazitaet[] { Wp(interpolieren: true), Kessel(20.0, zp: Programm(i => 0.3)) };
            SimulationPufferspeicher sp = Puffer(60.0);
            var leser = new Speicherleser(new[] { sp });
            var vorher = Angebotsfunktion.Angebot(9, 41.3, erzeuger, leser, new Stundenvorrang(1, 0, 0, 0));
            for (int i = 0; i < 5; i++)
            {
                var nochmal = Angebotsfunktion.Angebot(9, 41.3, erzeuger, leser, new Stundenvorrang(1, 0, 0, 0));
                Assert.Equal(vorher.LeistungKw, nochmal.LeistungKw);
                Assert.Equal(vorher.ErzeugerKw, nochmal.ErzeugerKw);
                Assert.Equal(vorher.SpeicherKw, nochmal.SpeicherKw);
            }
            Assert.Equal(60.0, sp.SOC, 9);
        }

        // ---------------------------------------------------------------------------------------------
        //  Speicherleser (2.5, Festlegung 6)
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void Speicherleser_veraendert_den_Speicher_nicht()
        {
            SimulationPufferspeicher sp = Puffer(80.0, entladeMax: 15.0);
            sp.StundeBeginnen(4);
            sp.Entladen(5.0, 4);
            sp.StundeAbschliessen(4);
            double soc = sp.SOC, faehig = sp.Entnahmefaehigkeit(), kanal = sp.EntladefaehigkeitKanal(Kanal.HEIZUNG);
            double[] socReihe = (double[])sp.SOC_stuendlich.Clone();

            var leser = new Speicherleser(new[] { sp });
            double e1 = leser.EntnehmbarKwh(5);
            double e2 = leser.EntnehmbarKwh(5);

            Assert.Equal(e1, e2);
            Assert.Equal(soc, sp.SOC);
            Assert.Equal(faehig, sp.Entnahmefaehigkeit());
            Assert.Equal(kanal, sp.EntladefaehigkeitKanal(Kanal.HEIZUNG));
            Assert.Equal(socReihe, sp.SOC_stuendlich);
        }

        [Fact]
        public void Entnehmbar_ist_das_Minimum_aus_Ladezustand_und_Budget_der_Stunde()
        {
            SimulationPufferspeicher sp = Puffer(80.0, entladeMax: 15.0);
            Assert.Equal(15.0, sp.EntnehmbarAmStundenbeginn(0));        // Budget der neuen Stunde

            sp.StundeBeginnen(4);
            sp.Entladen(5.0, 4);
            Assert.Equal(10.0, sp.EntnehmbarAmStundenbeginn(4), 9);     // Rest der laufenden Stunde
            Assert.Equal(15.0, sp.EntnehmbarAmStundenbeginn(5));        // volle Stunde danach

            SimulationPufferspeicher klein = Puffer(6.0, entladeMax: 15.0);
            Assert.Equal(6.0, klein.EntnehmbarAmStundenbeginn(0), 9);   // Ladezustand begrenzt

            SimulationPufferspeicher leer = Puffer(0.0);
            Assert.Equal(0.0, leer.EntnehmbarAmStundenbeginn(0));
            Assert.Equal(0.0, new Speicherleser(new[] { leer }).EntnehmbarKwh(0));
        }

        [Fact]
        public void Speicherleser_nimmt_nur_Heizungspuffer_und_summiert()
        {
            SimulationPufferspeicher a = Puffer(20.0), b = Puffer(30.0);
            var leser = new Speicherleser(new[] { a, b, null });
            Assert.True(leser.Vorhanden);
            Assert.Equal(50.0, leser.EntnehmbarKwh(0), 9);
            Assert.False(new Speicherleser(null).Vorhanden);
            Assert.Equal(0.0, new Speicherleser(null).EntnehmbarKwh(0));
        }

        [Fact]
        public void Puffer_leer_und_voll_im_Angebot()
        {
            var erzeuger = new IErzeugerkapazitaet[] { Wp(new Fahrplanerzeuger { Gesperrt = Nur(2) }) };
            Stundenangebot leer = Angebotsfunktion.Angebot(2, 45.0, erzeuger, new Speicherleser(new[] { Puffer(0.0) }), OhneVorrang);
            Assert.Equal(0.0, leer.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.SpeicherLeer, leer.Grund);

            SimulationPufferspeicher vollSp = Puffer(1000.0);
            Stundenangebot voll = Angebotsfunktion.Angebot(2, 45.0, erzeuger, new Speicherleser(new[] { vollSp }), OhneVorrang);
            Assert.Equal(vollSp.SOC, voll.LeistungKw, 9);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, voll.Grund);
        }
    }
}
