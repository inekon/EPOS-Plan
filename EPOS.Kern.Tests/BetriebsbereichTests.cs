using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rechenproben der Betriebsbereiche B0–B4</b> (Fachkonzept Übergabegrenze 4.5 und 8.1; Umsetzungskonzept
    /// 5.2, UB‑E2‑b) — ohne Datenbank, in kW. Zahlenbeispiel wie <see cref="UebergabegrenzeTests"/>: Heizkörper
    /// 75/60/20 °C, n = 1,3, 10 kW; Gebäude 10 kW bei −12 °C; Kennfeld bei 55 °C 7 kW bei −7 °C bis 9 kW bei +7 °C;
    /// θ_WP,max 55 °C, σ_min nach <see cref="Bivalenzvorgaben"/> (3 K).
    /// </summary>
    public class BetriebsbereichTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public BetriebsbereichTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        private const double TOL_KW = 0.01;
        private const double HOECHSTVORLAUF = 55.0;

        private static Uebergabezone Heizkoerper() => new Uebergabezone(10.0, 75.0, 60.0, 20.0, 1.3);

        private static Kennfeldgerade Kennfeld()
            => new Kennfeldgerade(new[] { new Kennfeldpunkt(-7.0, 7.0), new Kennfeldpunkt(7.0, 9.0) });

        private static Heizkurve Kurve()
        {
            Uebergabezone z = Heizkoerper();
            return new Heizkurve(new Uebergabekennwerte(z.PhiN, z.Exponent, z.AuslegungVorlaufC, z.AuslegungRuecklaufC,
                                                        z.AuslegungRaumC), -12.0, 0.0, 1.0);
        }

        /// <summary>Die Eingänge der Stunde bei Außentemperatur <paramref name="aussenC"/> aus Heizkurve, Heizlast und Kennfeld.</summary>
        private static Bereichseingang Stunde(double aussenC, bool vorwaermen = true,
                                              Bivalenzbetriebsart art = Bivalenzbetriebsart.Parallel,
                                              bool zweiter = true, bool vorDemKessel = true, double? ruecklaufC = null)
        {
            Heizkurve k = Kurve();
            return new Bereichseingang
            {
                Verfuegbar = true,
                VorlaufSollC = k.VorlaufC(20.0, aussenC),
                RuecklaufC = ruecklaufC ?? k.RuecklaufSollC(20.0, aussenC),
                BedarfKw = Bivalenzrechner.Heizlast(10.0, -12.0, 20.0, aussenC),
                KennfeldKw = Kennfeld().Leistung(aussenC),
                UebergabeMaxKw = Uebergabegrenze.Zone(Heizkoerper(), HOECHSTVORLAUF, 20.0).PhiUeMax,
                HydraulikKw = double.PositiveInfinity,
                WH = Heizkoerper().WH,
                HoechstvorlaufC = HOECHSTVORLAUF,
                SpreizungMinK = Bivalenzvorgaben.SPREIZUNG_MIN_K,
                Vorwaermbetrieb = vorwaermen,
                Betriebsart = art,
                ZweiterErzeuger = zweiter,
                VorDemKessel = vorDemKessel,
            };
        }

        [Fact]
        public void Vorwaermanteil_bei_0_Grad_im_Zahlenbeispiel()
        {
            Bereichseingang e = Stunde(0.0);
            Bereichsergebnis b = Bivalenzrechner.Bereich(e);
            _ausgabe.WriteLine($"0 °C: θ_V {e.VorlaufSollC:0.00} θ_R {e.RuecklaufC:0.00} Bedarf {e.BedarfKw:0.000} WP {b.LeistungKw:0.000} a {b.Anteil:0.0000}");
            Assert.Equal(Betriebsbereich.Vorwaermung, b.Bereich);
            Assert.Equal(6.25, e.BedarfKw, 1e-9);
            Assert.Equal(4.40, b.LeistungKw, TOL_KW);
            Assert.Equal(0.704, b.Anteil, 0.001);
            Assert.Equal(b.Anteil * e.BedarfKw, b.LeistungKw, TOL_KW);
            Assert.Equal(HOECHSTVORLAUF, b.VorwaermvorlaufC);
            Assert.True(b.AmHoechstvorlauf);

            Bereichsergebnis minus2 = Bivalenzrechner.Bereich(Stunde(-2.0));
            Assert.Equal(Betriebsbereich.Vorwaermung, minus2.Bereich);
            Assert.Equal(3.03, minus2.LeistungKw, TOL_KW);
        }

        [Fact]
        public void Ruecklauf_ueber_Hoechstvorlauf_minus_Mindestspreizung_steht_die_Waermepumpe()
        {
            double schwelle = HOECHSTVORLAUF - Bivalenzvorgaben.SPREIZUNG_MIN_K;
            Bereichsergebnis b = Bivalenzrechner.Bereich(Stunde(0.0, ruecklaufC: schwelle + 0.5));
            Assert.Equal(Betriebsbereich.Vorwaermung, b.Bereich);
            Assert.Equal(0.0, b.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.SpreizungMin, b.Grund);
            Assert.True(double.IsNaN(b.VorwaermvorlaufC));
            // Ohne bekannten Rücklauf ebenso: keine Spreizung nachweisbar.
            Assert.Equal(0.0, Bivalenzrechner.Bereich(Stunde(0.0, ruecklaufC: double.NaN)).LeistungKw);
        }

        [Fact]
        public void Alternativ_mit_Last_ueber_der_Grenze_deckt_der_Kessel_allein()
        {
            // +4 °C: θ_V,soll unter 55 °C, Bedarf 5 kW; Kennfeld künstlich 3 kW → Φ_WP,grenz reicht nicht.
            Bereichseingang e = Stunde(4.0, art: Bivalenzbetriebsart.Alternativ) with { KennfeldKw = 3.0 };
            Assert.True(e.VorlaufSollC <= HOECHSTVORLAUF);
            Bereichsergebnis b = Bivalenzrechner.Bereich(e);
            Assert.Equal(Betriebsbereich.NurKessel, b.Bereich);
            Assert.Equal(0.0, b.LeistungKw);
            // parallel dagegen B2: die Wärmepumpe liefert Φ_WP,grenz, der Kessel den Rest.
            Bereichsergebnis p = Bivalenzrechner.Bereich(e with { Betriebsart = Bivalenzbetriebsart.Parallel });
            Assert.Equal(Betriebsbereich.Parallel, p.Bereich);
            Assert.Equal(3.0, p.LeistungKw);
            // über θ_WP,max: alternativ kennt keinen Vorwärmbetrieb → B4.
            Bereichsergebnis ueber = Bivalenzrechner.Bereich(Stunde(0.0, art: Bivalenzbetriebsart.Alternativ));
            Assert.Equal(Betriebsbereich.NurKessel, ueber.Bereich);
            Assert.Equal(Verfuegbarkeitsgrund.UebergabeHoechstvorlauf, ueber.Grund);
        }

        [Fact]
        public void Teilparallel_unter_dem_Abschaltpunkt_ist_B0()
        {
            Bereichsergebnis b = Bivalenzrechner.Bereich(Stunde(-10.0, art: Bivalenzbetriebsart.Teilparallel) with
            {
                Verfuegbar = false,
                GrundNichtVerfuegbar = Verfuegbarkeitsgrund.Abschaltpunkt,
            });
            Assert.Equal(Betriebsbereich.NichtVerfuegbar, b.Bereich);
            Assert.Equal(0.0, b.LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.Abschaltpunkt, b.Grund);
        }

        [Fact]
        public void Ohne_Einbindung_entsteht_kein_Bivalenzobjekt()
        {
            Assert.False(Bivalenzmodul.Wirksam(null, true));
            Assert.False(Bivalenzmodul.Wirksam("  ", true));
            Assert.False(Bivalenzmodul.Wirksam("DIREKT", false));
            Assert.True(Bivalenzmodul.Wirksam("DIREKT", true));
            Assert.True(Bivalenzmodul.Wirksam("PUFFER", true));
        }

        [Fact]
        public void Bestand_unter_dem_Hoechstvorlauf_bleibt_bitgleich()
        {
            // Einbindung gesetzt, Vorwärmbetrieb 0, θ_V,soll ≤ θ_WP,max → B1/B2 mit der Kennfeldkapazität, unverändert.
            foreach (double ta in new[] { 2.0, 5.0, 10.0, 15.0 })
            {
                Bereichseingang e = Stunde(ta, vorwaermen: false);
                Assert.True(e.VorlaufSollC <= HOECHSTVORLAUF);
                Bereichsergebnis b = Bivalenzrechner.Bereich(e);
                Assert.Equal(Betriebsbereich.WpAllein, b.Bereich);
                Assert.Equal(BitConverter.DoubleToInt64Bits(e.KennfeldKw), BitConverter.DoubleToInt64Bits(b.LeistungKw));
                Assert.False(b.AmHoechstvorlauf);
                Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, b.Grund);
                Bereichsergebnis p = Bivalenzrechner.Bereich(e with { BedarfKw = e.KennfeldKw + 1.0 });
                Assert.Equal(Betriebsbereich.Parallel, p.Bereich);
                Assert.Equal(BitConverter.DoubleToInt64Bits(e.KennfeldKw), BitConverter.DoubleToInt64Bits(p.LeistungKw));
            }
            // darüber ohne Vorwärmbetrieb: neu B4 (UB‑Q1).
            Assert.Equal(Betriebsbereich.NurKessel, Bivalenzrechner.Bereich(Stunde(0.0, vorwaermen: false)).Bereich);
        }

        [Fact]
        public void Waermepumpe_hinter_dem_Kessel_rechnet_B4_und_meldet()
        {
            Bereichsergebnis b = Bivalenzrechner.Bereich(Stunde(0.0, vorDemKessel: false));
            Assert.Equal(Betriebsbereich.NurKessel, b.Bereich);
            Assert.True(b.KaskadeVerletzt);
            Assert.Equal(0.0, b.LeistungKw);
            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, null, HOECHSTVORLAUF, 3.0, true,
                                          Bivalenzbetriebsart.Parallel, zweiterErzeuger: true, vorDemKessel: false);
            Assert.True(modul.KaskadeVerletzt);
            modul.Zaehlen(b, 0.0);
            Assert.Equal(1, modul.KaskadeVerletztStunden);
            Assert.Equal(1, modul.NurKesselStunden);
            // Die benannte Laufmeldung steht in beiden Sprachen und nennt die Anlage.
            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                string text = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString("SIMENG_WP_VORWAERMUNG_KASKADE",
                    System.Globalization.CultureInfo.GetCultureInfo(kultur));
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.Contains("{0}", text);
            }
        }

        [Fact]
        public void Ohne_zweiten_Erzeuger_liefert_die_Waermepumpe_bis_zur_Uebergabegrenze()
        {
            Bereichseingang e = Stunde(0.0, zweiter: false);
            Bereichsergebnis b = Bivalenzrechner.Bereich(e);
            Assert.Equal(Betriebsbereich.WpAllein, b.Bereich);
            Assert.Equal(Verfuegbarkeitsgrund.UebergabeHoechstvorlauf, b.Grund);
            Assert.Equal(Math.Min(e.UebergabeMaxKw, e.KennfeldKw), b.LeistungKw, 1e-12);
        }

        [Fact]
        public void Bivalenzmodul_speichert_die_Uebergabegrenze_je_Stunde()
        {
            int aufrufe = 0;
            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, (z, h) => { aufrufe++; return 20.0; }, HOECHSTVORLAUF, 3.0,
                                          false, Bivalenzbetriebsart.Parallel, false, true);
            double a = modul.UebergabeMaxKw(5);
            double b = modul.UebergabeMaxKw(5);
            Assert.Equal(1, aufrufe);
            Assert.Equal(a, b);
            Assert.Equal(Uebergabegrenze.Zone(Heizkoerper(), HOECHSTVORLAUF, 20.0).PhiUeMax, a, 1e-9);
            Assert.Equal(a, modul.UebergabeMaxAuslegungKw, 1e-9);
            Assert.Equal(10.0 / 15.0, modul.WH, 1e-12);
        }

        [Fact]
        public void Angebot_im_Kreis_rechnet_B3_ueber_dem_Hoechstvorlauf()
        {
            // Drei Kennlinien 35/45/55, Quelle 0 °C, Vorlaufangebot 55 °C; verlangt 65 °C, Kreisrücklauf 50 °C.
            SimulationWaermepumpe._Kenndaten Kurve(int v, double p0) => new SimulationWaermepumpe._Kenndaten
            {
                Vorlauf = v,
                anz = 2,
                dat = new[]
                {
                    new SimulationWaermepumpe._DAT { Temperatur = 10, COP = 3.0, Leistung = p0 + 2.0 },
                    new SimulationWaermepumpe._DAT { Temperatur = -10, COP = 2.0, Leistung = p0 - 2.0 },
                },
            };
            var kurven = new[] { Kurve(35, 10.0), Kurve(45, 9.0), Kurve(55, 8.0) };
            var fahrplan = new Fahrplanerzeuger { Bezeichner = "WP", VorlaufAngebotC = HOECHSTVORLAUF };
            var wp = new WaermepumpeKapazitaet(fahrplan, kurven, null, new Quellprofil(new double[8760]), true);
            Assert.Equal(0.0, wp.Abfragen(0, 65.0).KapazitaetKw);   // Bestand: über dem Angebot nichts

            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, null, HOECHSTVORLAUF, 3.0, true,
                                          Bivalenzbetriebsart.Parallel, true, true);
            var ruecklauf = Enumerable.Repeat(double.NaN, 8760).ToArray();
            ruecklauf[0] = 50.0;
            modul.KreisruecklaufC = ruecklauf;
            wp.Bivalenz = modul;
            Erzeugerangebot a = wp.Abfragen(0, 65.0);
            Assert.Equal(Math.Min(modul.WH * 5.0, 8.0), a.KapazitaetKw, 1e-9);
            Assert.False(a.VorlaufNichtErreicht);
            // unter dem Höchstvorlauf unverändert
            Assert.Equal(new WaermepumpeKapazitaet(fahrplan, kurven, null, new Quellprofil(new double[8760]), true)
                             .Abfragen(0, 50.0).KapazitaetKw, wp.Abfragen(0, 50.0).KapazitaetKw);
            // Rücklauf über 52 °C: keine Spreizung → 0
            ruecklauf[0] = 53.0;
            Assert.Equal(0.0, wp.Abfragen(0, 65.0).KapazitaetKw);
        }
    }
}
