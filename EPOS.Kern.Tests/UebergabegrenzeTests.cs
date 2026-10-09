using System;
using System.Diagnostics;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rechenproben der Übergabegrenze und der Bivalenzpunkte</b> (Fachkonzept
    /// Übergabegrenze 8.1, Zeilen 1–6 und 9; Umsetzungskonzept 5.1, UB‑E1) — ohne Datenbank,
    /// in kW. Zahlenbeispiel: Heizkörper 75/60/20 °C, n = 1,3, 10 kW; Gebäude 10 kW bei −12 °C;
    /// Kennfeld bei 55 °C 7 kW bei −7 °C bis 9 kW bei +7 °C; σ_min 3 K.
    /// </summary>
    public class UebergabegrenzeTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public UebergabegrenzeTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        private const double TOL_KW = 1e-3;
        private const double TOL_K = 0.01;
        private const double TOL_PUNKT_K = 0.05;

        private static Uebergabezone Heizkoerper(double n = 1.3)
            => new Uebergabezone(10.0, 75.0, 60.0, 20.0, n);

        private static Kennfeldgerade KennfeldBeispiel()
            => new Kennfeldgerade(new[] { new Kennfeldpunkt(-7.0, 7.0), new Kennfeldpunkt(7.0, 9.0) });

        /// <summary>Bisektion als unabhängige Gegenrechnung von f(Φ) = 0.</summary>
        private static double Bisektion(Uebergabezone z, double hoechstvorlaufC, double raumC)
        {
            double lo = 0.0, hi = 2.0 * z.WH * (hoechstvorlaufC - raumC);
            for (int i = 0; i < 200; i++)
            {
                double m = 0.5 * (lo + hi);
                double f = z.PhiN * Math.Pow((hoechstvorlaufC - m / (2.0 * z.WH) - raumC) / z.DeltaThetaMN, z.Exponent) - m;
                if (f > 0.0) lo = m; else hi = m;
            }
            return 0.5 * (lo + hi);
        }

        // =====================================================================
        //  UB‑a, UB‑b: Gleichgewicht beim Höchstvorlauf
        // =====================================================================

        [Fact]
        public void Kalibrierung_aus_dem_Auslegungspunkt()
        {
            Uebergabezone z = Heizkoerper();
            Assert.Equal(10.0 / 15.0, z.WH, 12);
            Assert.Equal(47.5, z.DeltaThetaMN, 12);
        }

        [Fact]
        public void Gleichgewicht_bei_55_Grad()
        {
            Zonengrenze g = Uebergabegrenze.Zone(Heizkoerper(), 55.0, 20.0);
            Assert.Equal(5.680, g.PhiUeMax, TOL_KW);
            Assert.Equal(46.48, g.RuecklaufC, TOL_K);
            Assert.Equal(8.52, g.SpreizungK, TOL_K);
            Assert.True(g.Begrenzt);
            Assert.InRange(g.Schritte, 1, Uebergabegrenze.NEWTON_SCHRITTE_MAX);
            Assert.False(g.Bisektion);
        }

        [Theory]
        [InlineData(50.0, 4.681, 42.98)]
        [InlineData(60.0, 6.714, 49.93)]
        [InlineData(70.0, 8.877, 56.68)]
        public void Gleichgewicht_bei_weiteren_Vorlaeufen(double vorlaufC, double phiKW, double ruecklaufC)
        {
            Zonengrenze g = Uebergabegrenze.Zone(Heizkoerper(), vorlaufC, 20.0);
            Assert.Equal(phiKW, g.PhiUeMax, TOL_KW);
            Assert.Equal(ruecklaufC, g.RuecklaufC, TOL_K);
            Assert.Equal(vorlaufC - ruecklaufC, g.SpreizungK, TOL_K);
        }

        [Fact]
        public void Exponent_eins_gegen_geschlossene_Loesung()
        {
            Uebergabezone z = Heizkoerper(1.0);
            // Φ = Φ_N·(a − Φ/(2W))/Δ  →  Φ = Φ_N·a/(Δ + Φ_N/(2W))
            double a = 55.0 - 20.0;
            double geschlossen = z.PhiN * a / (z.DeltaThetaMN + z.PhiN / (2.0 * z.WH));
            Zonengrenze g = Uebergabegrenze.Zone(z, 55.0, 20.0);
            Assert.Equal(geschlossen, g.PhiUeMax, 9);
        }

        [Theory]
        [InlineData(1.1)]
        [InlineData(1.4)]
        public void Exponenten_gegen_Bisektion(double n)
        {
            Uebergabezone z = Heizkoerper(n);
            foreach (double v in new[] { 30.0, 50.0, 55.0, 70.0 })
            {
                Zonengrenze g = Uebergabegrenze.Zone(z, v, 20.0);
                Assert.Equal(Bisektion(z, v, 20.0), g.PhiUeMax, 9);
            }
        }

        [Fact]
        public void Startwert_der_Vorstunde_und_Rueckfall_Bisektion()
        {
            Uebergabezone z = Heizkoerper();
            double erwartet = Uebergabegrenze.Zone(z, 55.0, 20.0).PhiUeMax;
            Zonengrenze nah = Uebergabegrenze.Zone(z, 55.0, 20.0, 5.6);
            Assert.Equal(erwartet, nah.PhiUeMax, 9);
            // ungeeignete Startwerte (Rand, außerhalb, NaN) führen auf dieselbe Nullstelle
            double rand = 2.0 * z.WH * 35.0;
            foreach (double start in new[] { rand * 0.999999, 1e-12, -1.0, rand * 2.0, double.NaN })
                Assert.Equal(erwartet, Uebergabegrenze.Zone(z, 55.0, 20.0, start).PhiUeMax, 9);

            // Rückfall: Mit n = 0,5 und starker Übergabe springt Newton aus dem Einschluss (über a/b) — der Schritt
            // rechnet als Bisektion, die Lösung bleibt die Nullstelle.
            double a = 35.0, bb = 0.5 / z.WH, c = z.DeltaThetaMN;
            double phi = Uebergabegrenze.Nullstelle(1000.0, 0.5, a, bb, c, 1e-9, out int schritte, out bool bisektion);
            Assert.True(bisektion);
            Assert.True(schritte > 0);
            Assert.Equal(0.0, 1000.0 * Math.Pow((a - bb * phi) / c, 0.5) - phi, 9);
        }

        // =====================================================================
        //  Grenzfälle
        // =====================================================================

        [Theory]
        [InlineData(75.0)]
        [InlineData(80.0)]
        public void Hoechstvorlauf_ueber_Auslegung_begrenzt_nicht(double vorlaufC)
        {
            Zonengrenze g = Uebergabegrenze.Zone(Heizkoerper(), vorlaufC, 20.0);
            Assert.False(g.Begrenzt);
            Assert.True(g.PhiUeMax >= 10.0 - 1e-9);
        }

        [Fact]
        public void Hoechstvorlauf_gegen_Raumtemperatur_geht_auf_null()
        {
            Uebergabezone z = Heizkoerper();
            Zonengrenze knapp = Uebergabegrenze.Zone(z, 20.0 + 1e-6, 20.0);
            Assert.False(double.IsNaN(knapp.PhiUeMax));
            Assert.InRange(knapp.PhiUeMax, 0.0, 1e-6);
            Assert.Equal(20.0, knapp.RuecklaufC, 5);

            foreach (double v in new[] { 20.0, 15.0 })
            {
                Zonengrenze g = Uebergabegrenze.Zone(z, v, 20.0);
                Assert.Equal(0.0, g.PhiUeMax);
                Assert.Equal(v, g.RuecklaufC);
                Assert.Equal(0.0, g.SpreizungK);
                Assert.False(double.IsNaN(g.RuecklaufC));
            }
        }

        [Fact]
        public void Flaechenheizung_35_28_begrenzt_nicht()
        {
            var flaeche = new Uebergabezone(10.0, 35.0, 28.0, 20.0, 1.1);
            Zonengrenze g = Uebergabegrenze.Zone(flaeche, 55.0, 20.0);
            Assert.False(g.Begrenzt);
            Assert.True(g.PhiUeMax >= 10.0);
            Assert.False(double.IsNaN(g.PhiUeMax));
        }

        // =====================================================================
        //  Mehrere Zonen
        // =====================================================================

        [Fact]
        public void Mehrere_Zonen_summieren_ihre_Grenzen()
        {
            var a = new Uebergabezone(6.0, 75.0, 60.0, 20.0, 1.3);
            var b = new Uebergabezone(4.0, 70.0, 55.0, 22.0, 1.4);
            Gebaeudegrenze g = Uebergabegrenze.Gebaeude(new[] { a, b }, null, 55.0, 20.0);
            double za = Uebergabegrenze.Zone(a, 55.0, 20.0).PhiUeMax;
            double zb = Uebergabegrenze.Zone(b, 55.0, 20.0).PhiUeMax;
            Assert.Equal(2, g.Zonen.Count);
            Assert.Equal(za + zb, g.PhiUeMax, 12);
            Assert.Equal(10.0, g.PhiN, 12);
            Assert.Equal(55.0 - (za + zb) / (a.WH + b.WH), g.RuecklaufC, 12);
            Assert.Equal(g.PhiUeMax, Uebergabegrenze.Projekt(new[] { g }), 12);
        }

        [Fact]
        public void Ohne_Zonen_rechnet_das_Gebaeude_als_eine_Zone()
        {
            Gebaeudegrenze g = Uebergabegrenze.Gebaeude(Array.Empty<Uebergabezone>(), Heizkoerper(), 55.0, 20.0);
            Assert.Single(g.Zonen);
            Assert.Equal(5.680, g.PhiUeMax, TOL_KW);
            Assert.Equal(46.48, g.RuecklaufC, TOL_K);
            Assert.Equal(8.52, g.SpreizungK, TOL_K);
            Assert.Equal(0.568, g.Anteil, 3);
        }

        // =====================================================================
        //  Bivalenzpunkte des Zahlenbeispiels
        // =====================================================================

        private static Bivalenzpunkte PunkteBeispiel(bool vorwaermen, Bivalenzbetriebsart art, double? abschaltpunkt)
        {
            double ue = Uebergabegrenze.Zone(Heizkoerper(), 55.0, 20.0).PhiUeMax;
            return Bivalenzrechner.Punkte(10.0, -12.0, 20.0, ue, KennfeldBeispiel(), Heizkoerper(),
                                          55.0, 3.0, vorwaermen, art, abschaltpunkt);
        }

        [Fact]
        public void Bivalenzpunkte_mit_Vorwaermbetrieb()
        {
            Bivalenzpunkte p = PunkteBeispiel(true, Bivalenzbetriebsart.Parallel, null);
            Assert.Equal(1.83, p.ErsterC, TOL_PUNKT_K);
            Assert.Equal(-3.55, p.ZweiterC, TOL_PUNKT_K);
            Assert.Equal(-3.84, p.KennfeldAlleinC, TOL_PUNKT_K);
            Assert.True(p.UebergabeBegrenzt);
            Assert.Null(p.AbschaltpunktC);
            Assert.Equal(p.ZweiterC, p.MassgebendC);
        }

        [Fact]
        public void Ohne_Vorwaermbetrieb_faellt_der_zweite_auf_den_ersten()
        {
            Bivalenzpunkte p = PunkteBeispiel(false, Bivalenzbetriebsart.Parallel, null);
            Assert.Equal(p.ErsterC, p.ZweiterC);
            Assert.Equal(1.83, p.ZweiterC, TOL_PUNKT_K);
            // alternativ: Vorwärmbetrieb nicht wählbar, rechnet als aus
            Bivalenzpunkte alt = PunkteBeispiel(true, Bivalenzbetriebsart.Alternativ, null);
            Assert.Equal(alt.ErsterC, alt.ZweiterC);
        }

        [Fact]
        public void Abschaltpunkt_kalt_eingegeben_berechneter_ist_massgebend()
        {
            Bivalenzpunkte p = PunkteBeispiel(true, Bivalenzbetriebsart.Teilparallel, -10.0);
            Assert.Equal(-10.0, p.AbschaltpunktC);
            Assert.Equal(-3.55, p.MassgebendC, TOL_PUNKT_K);
        }

        [Fact]
        public void Abschaltpunkt_warm_eingegeben_bleibt_massgebend()
        {
            Bivalenzpunkte p = PunkteBeispiel(true, Bivalenzbetriebsart.Teilparallel, 3.0);
            Assert.Equal(3.0, p.MassgebendC);
            // parallel kennt keinen Abschaltpunkt
            Bivalenzpunkte par = PunkteBeispiel(true, Bivalenzbetriebsart.Parallel, 3.0);
            Assert.Null(par.AbschaltpunktC);
            Assert.Equal(-3.55, par.MassgebendC, TOL_PUNKT_K);
        }

        [Fact]
        public void Massgebend_nach_Regel()
        {
            Assert.Equal(AbschaltpunktRegel.Deckel, Bivalenzrechner.REGEL);
            Assert.Equal(-3.5, Bivalenzrechner.Massgebend(-10.0, -3.5));
            Assert.Equal(3.0, Bivalenzrechner.Massgebend(3.0, -3.5));
            Assert.Equal(-3.5, Bivalenzrechner.Massgebend(null, -3.5));
            Assert.Equal(-10.0, Bivalenzrechner.Massgebend(-10.0, double.NaN));
            Assert.True(double.IsNaN(Bivalenzrechner.Massgebend(null, double.NaN)));
            Assert.Equal(-3.5, Bivalenzrechner.Massgebend(3.0, -3.5, AbschaltpunktRegel.Ersetzen));
            Assert.Equal(3.0, Bivalenzrechner.Massgebend(3.0, -3.5, AbschaltpunktRegel.NurAnzeigen));
        }

        [Fact]
        public void Kennfeldgerade_interpoliert_und_haelt()
        {
            Kennfeldgerade k = KennfeldBeispiel();
            Assert.Equal(7.0, k.Leistung(-20.0));
            Assert.Equal(8.0, k.Leistung(0.0), 12);
            Assert.Equal(9.0, k.Leistung(15.0));
        }

        // =====================================================================
        //  Herleitung und Vorgaben
        // =====================================================================

        private static BivalenzGeraetedaten GeraetBeispiel(string einbindung) => new BivalenzGeraetedaten
        {
            HoechstvorlaufC = 55.0,
            Kennfeld = new[] { new Kennfeldpunkt(-7.0, 7.0), new Kennfeldpunkt(7.0, 9.0) },
            SpreizungMinK = 3.0,
            Betriebsart = Bivalenzbetriebsart.Teilparallel,
            AbschaltpunktC = -10.0,
            Vorwaermbetrieb = true,
            Kesselleistung = 10.0,
            Einbindung = einbindung,
        };

        [Fact]
        public void Herleitung_des_Zahlenbeispiels()
        {
            var gebaeude = new BivalenzGebaeudedaten
            {
                Gebaeude = Heizkoerper(), HeizlastN = 10.0, AuslegungAussenC = -12.0, AuslegungRaumC = 20.0,
            };
            Bivalenzherleitung h = Bivalenzherleitung.Rechnen(gebaeude, GeraetBeispiel("DIREKT"));
            Assert.Equal(Herleitungszustand.Wirksam, h.Zustand);
            Assert.Equal(5.680, h.PhiUeMax, TOL_KW);
            Assert.Equal(0.568, h.Anteil, 3);
            Assert.Equal(46.48, h.RuecklaufUeC, TOL_K);
            Assert.Equal(8.52, h.SpreizungUeK, TOL_K);
            Assert.Equal(1.83, h.Punkte.ErsterC, TOL_PUNKT_K);
            Assert.Equal(-3.84, h.Punkte.KennfeldAlleinC, TOL_PUNKT_K);
            Assert.Equal(-3.55, h.Punkte.ZweiterC, TOL_PUNKT_K);
            Assert.Equal(-3.55, h.Punkte.MassgebendC, TOL_PUNKT_K);
            Assert.Equal(0.70, h.HybridAnteil, 12);
            Assert.Equal(0.30, h.HybridMindestanteil);
            Assert.Equal(33, h.Diagramm.Reihen.Count);          // −12 … +20 °C in 1 K
            Assert.Equal(5, h.Diagramm.Marken.Count);
            Assert.Equal(10.0, h.Diagramm.Reihen[0].Heizlast, 12);
            Assert.Equal(0.0, h.Diagramm.Reihen[32].Heizlast, 12);

            Bivalenzherleitung ruht = Bivalenzherleitung.Rechnen(gebaeude, GeraetBeispiel(null));
            Assert.Equal(Herleitungszustand.NichtWirksam, ruht.Zustand);
            Assert.Equal(h.PhiUeMax, ruht.PhiUeMax);
        }

        [Fact]
        public void Herleitung_ohne_Kopplung()
        {
            var gebaeude = new BivalenzGebaeudedaten { HeizlastN = 10.0, AuslegungAussenC = -12.0, AuslegungRaumC = 20.0 };
            Bivalenzherleitung h = Bivalenzherleitung.Rechnen(gebaeude, GeraetBeispiel("DIREKT"));
            Assert.Equal(Herleitungszustand.OhneKopplung, h.Zustand);
            Assert.Null(h.Grenze);
            Assert.True(double.IsNaN(h.PhiUeMax));
            Assert.Equal(-3.84, h.Punkte.ErsterC, TOL_PUNKT_K);
            Assert.Equal(h.Punkte.ErsterC, h.Punkte.KennfeldAlleinC);
        }

        [Fact]
        public void Vorgaben_je_Kaeltemittel()
        {
            Assert.Equal(11, Bivalenzvorgaben.Kaeltemittelcodes.Count);
            Assert.Equal(55.0, Bivalenzvorgaben.Vorgabe("R410A").HoechstvorlaufC);
            Assert.Equal(55.0, Bivalenzvorgaben.Vorgabe("r32").HoechstvorlaufC);
            Assert.Equal(70.0, Bivalenzvorgaben.Vorgabe("R290").HoechstvorlaufC);
            Assert.Equal(80.0, Bivalenzvorgaben.Vorgabe("R1234ze(E)").HoechstvorlaufC);
            Kaeltemittelvorgabe r744 = Bivalenzvorgaben.Vorgabe("R744");
            Assert.Equal(80.0, r744.HoechstvorlaufC);
            Assert.Equal(30.0, r744.SpreizungMaxK);
            Assert.Equal(40.0, r744.RuecklaufGrenzeC);
            Assert.Equal(30.0, r744.BezugsruecklaufC);
            Assert.Equal(2.5, r744.AbwertungProzentJeK);
            Assert.Equal(Kaeltemittelherkunft.Kaeltemittelklasse, r744.Herkunft);
            foreach (string code in new[] { null, "", "SONSTIGES", "R999", "R134a" })
            {
                Kaeltemittelvorgabe v = Bivalenzvorgaben.Vorgabe(code);
                Assert.Equal(Kaeltemittelherkunft.Allgemein, v.Herkunft);
                Assert.Null(v.HoechstvorlaufC);
                Assert.Null(v.RuecklaufGrenzeC);
                Assert.Equal(10.0, v.SpreizungMaxK);
                Assert.Equal(3.0, v.SpreizungMinK);
                Assert.Equal(5.0, v.SpreizungAuslegungK);
                Assert.Equal(0.60, v.MindestvolumenstromAnteil);
            }
            Assert.True(Bivalenzvorgaben.IstBekannt("R1233zd(E)"));
            Assert.False(Bivalenzvorgaben.IstBekannt("R999"));
        }

        [Fact]
        public void Laufzeit_der_Nullstelle()
        {
            Uebergabezone z = Heizkoerper();
            const int N = 200_000;
            double s = 0.0;
            var uhr = Stopwatch.StartNew();
            for (int i = 0; i < N; i++) s += Uebergabegrenze.Zone(z, 40.0 + (i % 40), 20.0).PhiUeMax;
            uhr.Stop();
            _ausgabe.WriteLine("Nullstelle je Aufruf: " + (uhr.Elapsed.TotalMilliseconds * 1000.0 / N).ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " µs");
            Assert.True(s > 0.0);
            // grobe Obergrenze, kein Leistungsmaß: 200 000 Aufrufe deutlich unter 5 s
            Assert.True(uhr.Elapsed.TotalSeconds < 5.0, "Laufzeit " + uhr.Elapsed.TotalMilliseconds + " ms");
        }

        // =====================================================================
        //  UB‑E1-b: Kennfeld bei Höchstvorlauf und Abbildung in die Herleitungszeile
        // =====================================================================

        [Fact]
        public void Kennfeld_bei_Hoechstvorlauf_interpoliert_ueber_den_Vorlauf_und_haelt_die_Randstufen()
        {
            var satz = new KennlinienSatz(
                Array.Empty<ChartRenderer.KennlinienReihe>(),
                new[]
                {
                    new ChartRenderer.KennlinienReihe(35, new[] { (-7.0, 9.0), (7.0, 12.0) }),
                    new ChartRenderer.KennlinienReihe(55, new[] { (-7.0, 7.0), (7.0, 9.0) }),
                });
            var mitte = BivalenzQuelle.KennfeldBeiVorlauf(satz, 45.0);
            Assert.Equal(2, mitte.Count);
            Assert.Equal(8.0, mitte[0].Leistung, 12);
            Assert.Equal(10.5, mitte[1].Leistung, 12);
            Assert.Equal(7.0, BivalenzQuelle.KennfeldBeiVorlauf(satz, 60.0)[0].Leistung, 12);
            Assert.Equal(12.0, BivalenzQuelle.KennfeldBeiVorlauf(satz, 30.0)[1].Leistung, 12);
            Assert.Empty(BivalenzQuelle.KennfeldBeiVorlauf(KennlinienSatz.Leer, 55.0));
        }

        [Fact]
        public void Abbildung_des_Zahlenbeispiels_ruht_ohne_Einbindung_und_zeigt_die_Werte()
        {
            using var kultur = new Kulturvorrichtung("de-DE");
            var projekt = new BivalenzProjektdaten
            {
                KesselleistungKw = 10.0,
                Gebaeude = new BivalenzGebaeudedaten
                {
                    Gebaeude = Heizkoerper(), HeizlastN = 10.0, AuslegungAussenC = -12.0, AuslegungRaumC = 20.0,
                },
            };
            BivalenzGeraetedaten geraet = GeraetBeispiel(null);
            EPOS.UI.Dialoge.Waermepumpe.WaermepumpeBivalenzWerte w = BivalenzAbbildung.Werte(projekt, geraet);
            Assert.Equal(EPOS.UI.Dialoge.Waermepumpe.BivalenzKennzeichen.NichtWirksam, w.Kennzeichen);
            string zeile = EPOS.UI.Dialoge.Waermepumpe.WaermepumpeBivalenzText.Zeile(
                w, new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKonfigurationTexte());
            // Der zweite Punkt rechnet der Kern zu −3,55 °C (Toleranz der Rechenprobe oben); gerundet steht er knapp
            // unter der Mitte und zeigt −3,5 °C — das Mockup nennt −3,6 °C (benannte Abweichung im Bericht UB‑E1-b).
            Assert.Equal(-3.55, w.ZweiterC, TOL_PUNKT_K);
            string zweiter = w.ZweiterC.ToString("+0.0;\u22120.0;0.0", System.Globalization.CultureInfo.CurrentCulture);
            Assert.Equal("Einbindung nicht gesetzt — Übergabegrenze ruht. Übergabe bei Höchstvorlauf 55 °C: 5,7 kW von "
                         + "10,0 kW Heizlast (57 %), Rücklauf 46,5 °C, Spreizung 8,5 K · erster Bivalenzpunkt +1,8 °C "
                         + "(nach Kennfeld allein −3,8 °C) · zweiter Bivalenzpunkt " + zweiter + " °C (Vorwärmbetrieb) · "
                         + "Wärmepumpe bei −7 °C 70 % der Kesselleistung (§ 43 GModG: mindestens 30 %).", zeile);

            // Ohne Gebäudedaten (Kopplung aus) bleibt es beim Kennzeichen; ohne Kennlinie nur die Übergabe.
            Assert.Equal(EPOS.UI.Dialoge.Waermepumpe.BivalenzKennzeichen.OhneKopplung,
                         BivalenzAbbildung.Werte(new BivalenzProjektdaten(), geraet).Kennzeichen);
            var ohneKennfeld = BivalenzAbbildung.Werte(projekt, new BivalenzGeraetedaten { HoechstvorlaufC = 55.0 });
            Assert.Equal(EPOS.UI.Dialoge.Waermepumpe.BivalenzKennzeichen.OhneKennfeld, ohneKennfeld.Kennzeichen);
            Assert.Equal(5.680, ohneKennfeld.UebergabeKw, TOL_KW);
        }

        [Fact]
        public void Abbildung_liest_den_Arbeitsstand_und_setzt_nie_eine_Einbindung()
        {
            var d = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten
            {
                Vorlauf = 50, VorlaufMax = null, BivalenterBetrieb = true,
                Betriebsart = DbWerte.WP_BETRIEBSART_ALTERNATIV, Abschaltpunkt = -5.0,
            };
            BivalenzGeraetedaten g = BivalenzAbbildung.Geraet(d, Array.Empty<Kennfeldpunkt>());
            Assert.Equal(50.0, g.HoechstvorlaufC);
            Assert.Equal(Bivalenzbetriebsart.Alternativ, g.Betriebsart);
            Assert.Equal(-5.0, g.AbschaltpunktC);
            Assert.Null(g.Einbindung);
            Assert.False(g.Vorwaermbetrieb);
            d.VorlaufMax = 55.0;
            Assert.Equal(55.0, BivalenzAbbildung.Hoechstvorlauf(d));
            Assert.Equal(11, BivalenzAbbildung.Kaeltemittelliste().Count);
        }
    }
}
