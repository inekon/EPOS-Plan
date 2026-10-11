using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Vorheizen Option 1 „Vorheizzeit vorgeben“</b> (Entwurf Vorheizrampe Fassung 2, 2.4, 2.5, 2.7; Welle V2): Einstellung
    /// mit Vorgabe Sollwertrampe (bitgleich), Φ_K,max, P_K und P_V für % und kW, Fenster, Sollwert, Deckelreihe und Floor je
    /// Stunde an einem kleinen Kalender (Werktag, Wochenende, Ferien &gt; 47 h, zwei Anstiege 16 → 18 → 20 °C, Übergang aus
    /// „aus“), Nacht ohne Absenkung, Nachweisgrößen, Mehrzonen mit Geltung Zone, AK1 mit Schalter an/aus, AK3-Rückfall mit
    /// Hinweis, die Probe gegen die erste Ordnung (2.3) und die Messung an Projekt 1051.
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizVorgabeTests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public VorheizVorgabeTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Werktag (Tag mod 7 &lt; 5) 7–17 Uhr 20 °C, sonst 16 °C; „aus“ (NaN) zwischen Tag 60 und 299.</summary>
        internal static double[] Kalender()
        {
            var s = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
            {
                int tag = h / 24, stunde = h % 24;
                if (tag >= 60 && tag < 300) { s[h] = double.NaN; continue; }
                bool werktag = tag % 7 < 5;
                bool ferien = tag >= 20 && tag <= 24;
                s[h] = werktag && !ferien && stunde >= 7 && stunde <= 17 ? 20.0 : 16.0;
                // Tag 2: zwei Anstiege, 18 °C ab 6 Uhr, 20 °C ab 8 Uhr.
                if (tag == 2 && stunde == 6) s[h] = 18.0;
                if (tag == 2 && stunde == 7) s[h] = 18.0;
            }
            return s;
        }

        private static Aufheizvorgabe Vorgabe(int tV, Vorheiztoleranzart art = Vorheiztoleranzart.Prozent, double? wert = null,
                                              bool heizkreis = true, Vorheizgeltung geltung = Vorheizgeltung.Gebaeude)
            => new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, tV, art, wert, null, geltung, heizkreis),
            };

        private static GebaeudeModellEingang Probeeingang(double[] soll = null)
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            e.HeizsollwertMitRampeSetzen(soll ?? Kalender());
            return e;
        }

        private static GebaeudeModellEingang Gekoppelt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false,
                DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            e.HeizsollwertMitRampeSetzen(Kalender());
            return e;
        }

        /// <summary>Plan, Lauf und Nachweis eines Einzoneneingangs — derselbe Ablauf wie <c>Vdi6007Rechenweg.Rechnen</c>.</summary>
        private static (Aufheizplan Plan, GebaeudeModellErgebnis Lauf) Rechnen(GebaeudeModellEingang e, Aufheizvorgabe v)
        {
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, v, 0, 1);
            GebaeudeModellErgebnis lauf = Vdi6007Rechenweg.Laufen(e, 0, 1, plan);
            Vorheizplanung.NachweisenEinzone(e, plan, lauf);
            return (plan, lauf);
        }

        // =====================================================================
        //  Einstellung
        // =====================================================================

        [Fact]
        public void Die_Vorgabe_ist_die_Sollwertrampe_und_die_Felder_setzen_die_Kernkonstanten_ein()
        {
            var an = new Aufheizvorgabe(true, null, null, null, null);
            Assert.Same(Vorheizvorgabe.Sollwertrampe, an.Vorheizen);
            Assert.Same(Vorheizvorgabe.Sollwertrampe, Aufheizvorgabe.Aus.Vorheizen);
            Assert.False(Vorheizplanung.Anwendbar(an));
            Assert.Equal(Vorheizrueckfall.Keiner, Vorheizplanung.Rueckfall(an, DbWerte.ANLAGENKOPPLUNG_AK3, true));

            var v = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6);
            Assert.Equal(1.0, v.GenauigkeitWirksamK);
            Assert.True(v.ToleranzIstVorgabe);
            Assert.True(v.HeizkreisEinbeziehen);
            Assert.Equal(Vorheizgeltung.Gebaeude, v.Geltung);
            Assert.Equal(1.2 * 10000.0, v.DeckelW(10000.0), 9);
            Assert.Equal(1.1 * 10000.0, new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6, Vorheiztoleranzart.Prozent, 10.0).DeckelW(10000.0), 9);
            Assert.Equal(13000.0, new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6, Vorheiztoleranzart.Kilowatt, 3.0).DeckelW(10000.0), 9);
            var null0 = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6, Vorheiztoleranzart.Prozent, 0.0);
            Assert.True(null0.ToleranzNull);
            Assert.Equal(10000.0, null0.DeckelW(10000.0));
            Assert.Null(new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6, Vorheiztoleranzart.Prozent, double.NaN, double.NaN).ToleranzWert);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 48));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 0));

            // Eingeschaltet() behält das Vorheizen.
            Aufheizvorgabe aus = new Aufheizvorgabe(false, null, null, null, null) { Vorheizen = v };
            Assert.Same(v, aus.Eingeschaltet().Vorheizen);
        }

        [Fact]
        public void Rueckfall_AK3_Heizkreis_ohne_Zeit_und_Berechnet()
        {
            Aufheizvorgabe v = Vorgabe(6);
            Assert.True(Vorheizplanung.Anwendbar(v));
            Assert.Equal(Vorheizrueckfall.Keiner, Vorheizplanung.Rueckfall(v, null, false));
            Assert.Equal(Vorheizrueckfall.Keiner, Vorheizplanung.Rueckfall(v, DbWerte.ANLAGENKOPPLUNG_AK1, true));
            Assert.Equal(Vorheizrueckfall.Ak3, Vorheizplanung.Rueckfall(v, DbWerte.ANLAGENKOPPLUNG_AK3, true));
            Assert.Equal(Vorheizrueckfall.Heizkreis, Vorheizplanung.Rueckfall(Vorgabe(6, heizkreis: false), DbWerte.ANLAGENKOPPLUNG_AK1, true));
            Assert.Equal(Vorheizrueckfall.Keiner, Vorheizplanung.Rueckfall(Vorgabe(6, heizkreis: false), DbWerte.ANLAGENKOPPLUNG_AK1, false));
            var ohne = new Aufheizvorgabe(true, null, null, null, null) { Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Vorgabe) };
            Assert.Equal(Vorheizrueckfall.OhneVorheizzeit, Vorheizplanung.Rueckfall(ohne, null, false));
            var berechnet = new Aufheizvorgabe(true, null, null, null, null) { Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Berechnet) };
            Assert.True(Vorheizplanung.Anwendbar(berechnet));
            Assert.Equal(Vorheizrueckfall.Keiner, Vorheizplanung.Rueckfall(berechnet, null, false));
            Assert.Equal(Vorheizrueckfall.Ak3, Vorheizplanung.Rueckfall(berechnet, DbWerte.ANLAGENKOPPLUNG_AK3, false));

            Aufheizvorgabe wirksam = Vorheizplanung.Wirksam(v, Vorheizrueckfall.Ak3);
            Assert.False(Vorheizplanung.Anwendbar(wirksam));
            Assert.Same(Vorheizvorgabe.Sollwertrampe, wirksam.Vorheizen);
            Assert.True(wirksam.An);
            Assert.Same(v, Vorheizplanung.Wirksam(v, Vorheizrueckfall.Keiner));
        }

        [Fact]
        public void Der_Ruckfall_AK3_steht_als_Hinweis_einmal_je_Gebaeude_in_beiden_Sprachen()
        {
            foreach ((string kultur, string text) in new[] { ("de-DE", "geschlossenen Kreis (AK3)"), ("en-US", "closed loop (AK3)") })
            {
                using var k = new Kulturvorrichtung(kultur);
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Ak3, null, "Gebäude A");
                Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Ak3, null, "Gebäude A");
                Assert.Single(p.Hinweise, z => z.Contains(text, StringComparison.Ordinal) && z.Contains("Gebäude A", StringComparison.Ordinal));
            }
        }

        // =====================================================================
        //  Plan
        // =====================================================================

        [Fact]
        public void Fenster_Sollwert_Deckelreihe_und_Floor_je_Stunde_am_kleinen_Kalender()
        {
            GebaeudeModellEingang e = Probeeingang();
            double[] s = (double[])e.ThetaSoll.Clone();
            Aufheizzone zone = Aufheizzone.Aus(ZonenEingang.Einzeln(e));
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, Vorgabe(6), 0, 1);
            Vorheizplan vp = plan.Vorheizen;
            Assert.NotNull(vp);

            // Φ_K,max = Jahresmaximum von Φ_ref(h_s); P_K = 1,2·Φ_K,max; P_V = min(P_K, P_verf).
            double phiMax = vp.Spruenge.Max(x => x.PhiRefW);
            Assert.Equal(Bits(phiMax), Bits(vp.PhiKMaxW));
            Assert.Equal(1.2 * phiMax, vp.DeckelW, 9);
            Assert.Equal(Math.Min(vp.DeckelW, plan.Bemessung.AufheizleistungW), vp.VorheizleistungW);
            foreach (Vorheizsprung sp in vp.Spruenge)
                Assert.Equal(Bits(Vorheizplanung.PhiRef(zone, s[sp.Sprungstunde], sp.Sprungstunde)), Bits(sp.PhiRefW));

            // Werktag Dienstag (Tag 1): h_s = 31, D = 13, Fenster [25, 30], Block [31, 41].
            Vorheizsprung di = vp.Spruenge.Single(x => x.Sprungstunde == 31);
            Assert.Equal(13, di.AbsenkdauerH);
            Assert.Equal(6, di.FensterH);
            Assert.Equal(41, di.Blockende);
            Assert.False(di.NachtOhneAbsenkung);
            Assert.Equal(16.0, e.ThetaSoll[24]);
            for (int h = 25; h <= 30; h++)
            {
                Assert.Equal(20.0, e.ThetaSoll[h]);
                Assert.Equal(vp.VorheizleistungW, plan.Deckelreihe[h]);
            }
            for (int h = 31; h <= 41; h++)
                Assert.Equal(Math.Max(vp.DeckelW, Vorheizplanung.PhiRef(zone, 20.0, h)), plan.Deckelreihe[h]);
            Assert.True(double.IsNaN(plan.Deckelreihe[24]));
            Assert.True(double.IsNaN(plan.Deckelreihe[42]));

            // Zwei Anstiege an Tag 2: 16 → 18 um 6 Uhr (Fenster 6 h), 18 → 20 um 8 Uhr (Fenster beginnt am ersten Anstieg).
            Vorheizsprung a1 = vp.Spruenge.Single(x => x.Sprungstunde == 54);
            Vorheizsprung a2 = vp.Spruenge.Single(x => x.Sprungstunde == 56);
            Assert.Equal(6, a1.FensterH);
            Assert.Equal(2, a2.FensterH);
            Assert.Equal(18.0, e.ThetaSoll[48]);
            Assert.Equal(20.0, e.ThetaSoll[54]);
            Assert.Equal(20.0, e.ThetaSoll[55]);
            Assert.Equal(Math.Max(vp.DeckelW, Vorheizplanung.PhiRef(zone, 18.0, 54)), plan.Deckelreihe[54], 6);

            // Montag nach dem Wochenende (Tag 7): D = 61, Fenster 6; nach den Ferien (Tag 25, nach Tag 21–24 und dem Wochenende davor): D > 47.
            Vorheizsprung mo = vp.Spruenge.Single(x => x.Sprungstunde == 7 * 24 + 7);
            Assert.Equal(61, mo.AbsenkdauerH);
            Assert.Equal(6, mo.FensterH);
            Vorheizsprung nachFerien = vp.Spruenge.Single(x => x.Sprungstunde == 25 * 24 + 7);
            Assert.True(nachFerien.AbsenkdauerH > 47);
            Assert.Equal(6, nachFerien.FensterH);

            // Übergang aus „aus“ (Tag 300, 0 Uhr): kein Fenster, eigens gezählt (W4).
            Assert.True(vp.SpruengeAus >= 1);
            Assert.DoesNotContain(vp.Spruenge, x => x.Sprungstunde == 300 * 24);
            Assert.True(double.IsNaN(plan.Deckelreihe[300 * 24 - 1]));
            Assert.True(double.IsNaN(e.ThetaSoll[300 * 24 - 1]));

            // Floor: gezählte Blockstunden mit Φ_ref(h) > P_K.
            int floor = 0;
            for (int h = 0; h < STUNDEN; h++)
                if (!double.IsNaN(plan.Deckelreihe[h]) && plan.Deckelreihe[h] > vp.DeckelW && plan.Deckelreihe[h] != vp.VorheizleistungW) floor++;
            Assert.Equal(vp.FloorstundenH, floor);
            Assert.Equal(0, vp.NaechteOhneAbsenkung);
        }

        [Fact]
        public void Kilowatt_Toleranz_und_Vorheizzeit_ueber_der_Absenkdauer_heizen_die_Nacht_durch()
        {
            GebaeudeModellEingang e = Probeeingang();
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, Vorgabe(15, Vorheiztoleranzart.Kilowatt, 3.0), 0, 1);
            Vorheizplan vp = plan.Vorheizen;
            Assert.Equal(vp.PhiKMaxW + 3000.0, vp.DeckelW, 9);

            // t_V = 15 ≥ D = 13: das Fenster beginnt am Blockende des Vortags, die Nacht zählt.
            Vorheizsprung di = vp.Spruenge.Single(x => x.Sprungstunde == 31);
            Assert.Equal(13, di.FensterH);
            Assert.True(di.NachtOhneAbsenkung);
            for (int h = 18; h <= 30; h++) Assert.Equal(20.0, e.ThetaSoll[h]);
            Assert.True(vp.NaechteOhneAbsenkung > 0);
            Assert.Equal(vp.Spruenge.Count(x => x.NachtOhneAbsenkung), vp.NaechteOhneAbsenkung);
            // Nach dem Wochenende (D = 61) nicht.
            Assert.False(vp.Spruenge.Single(x => x.Sprungstunde == 7 * 24 + 7).NachtOhneAbsenkung);
        }

        [Fact]
        public void Die_manuelle_Aufheizzeit_des_Gebaeudes_uebersteuert_die_Vorheizzeit()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Aufheizzeit_Manuell_H = 3;
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            e.HeizsollwertMitRampeSetzen(Kalender());
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, Vorgabe(6), 0, 1);
            Assert.Equal(3, plan.Vorheizen.VorheizzeitH);
            Assert.Equal(3, plan.Vorheizen.Spruenge.Single(x => x.Sprungstunde == 31).FensterH);
        }

        // =====================================================================
        //  Lauf und Nachweis
        // =====================================================================

        [Fact]
        public void Nachweisgroessen_am_konstruierten_Fall()
        {
            (Aufheizplan plan, GebaeudeModellErgebnis lauf) = Rechnen(Probeeingang(), Vorgabe(6));
            Vorheizplan vp = plan.Vorheizen;
            Vorheiznachweis n = vp.Nachweis;
            Assert.NotNull(n);
            Assert.Equal(vp.Spruenge.Count, n.Pruefungen.Count);

            // Die Grenze hält in jeder Stunde mit eigener Grenze.
            for (int h = 0; h < STUNDEN; h++)
                if (!double.IsNaN(plan.Deckelreihe[h]))
                    Assert.True(lauf.HeizlastW[h] <= plan.Deckelreihe[h] * (1.0 + 1e-9) + 1e-6, "Stunde " + h);

            var tage = new HashSet<int>();
            for (int i = 0; i < n.Pruefungen.Count; i++)
            {
                Vorheizpruefung pr = n.Pruefungen[i];
                Vorheizsprung sp = vp.Spruenge[i];
                Assert.Equal(sp.Sprungstunde, pr.Sprungstunde);
                Assert.True(pr.DeltaThetaK >= 0.0);
                Assert.Equal(pr.DeltaThetaK <= 1.0 + 1e-9, pr.Angekommen);
                if (!pr.Angekommen) tage.Add(pr.Sprungstunde / 24);
                Assert.Equal(Bits(lauf.Raumtemperatur[pr.Sprungstunde]), Bits(pr.LuftMittelC));
                Assert.Equal(lauf.HeizlastW[pr.Sprungstunde] - lauf.HeizlastW[pr.Sprungstunde - 1], pr.SprungW);
                Assert.Equal(lauf.HeizlastW[pr.Sprungstunde] - sp.PhiRefW, pr.SprungBW);
                Assert.InRange(pr.UnterschreitungH, 0, sp.Blockende - sp.Sprungstunde + 1);
            }
            Assert.Equal(tage.Count, n.TageOhneAnkunftAnzahl);
            Assert.Equal(lauf.VerbrauchAltKwh - vp.HeizwaermeVorlaufKwh, n.MehrwaermeKwh);
            Assert.Equal(lauf.HeizlastW.Max(), n.SpitzeW);
            Assert.Equal(n.Pruefungen.Max(x => x.SprungW), n.SprungMaxW);

            // Der Vorlauf hat die Massen erfasst (für V3), der Lauf ebenso.
            Assert.NotNull(vp.VorlaufMassenAw);
            Assert.Equal(STUNDEN, vp.VorlaufMassenIw.Length);
            Assert.NotNull(lauf.MassenEndeAw);

            Vorheizgebaeude g = Vorheizplanung.Gebaeudewerte(new[] { plan });
            Assert.Equal(n.TageOhneAnkunftAnzahl, g.TageOhneAnkunftAnzahl);
            Assert.Equal(vp.DeckelW, g.DeckelW);
            Assert.Equal(n.SpitzeW, g.SpitzeW);
        }

        [Fact]
        public void Laengeres_Vorheizen_kommt_oefter_an_und_kostet_Mehrwaerme()
        {
            Vorheiznachweis kurz = Rechnen(Probeeingang(), Vorgabe(1, Vorheiztoleranzart.Prozent, 0.0)).Plan.Vorheizen.Nachweis;
            Vorheiznachweis lang = Rechnen(Probeeingang(), Vorgabe(15)).Plan.Vorheizen.Nachweis;
            Assert.True(kurz.TageOhneAnkunftAnzahl > 0, "Mit 1 h und x = 0 kommt die Zone an keinem Tag zu spät?");
            Assert.True(lang.TageOhneAnkunftAnzahl < kurz.TageOhneAnkunftAnzahl);
            Assert.True(lang.MehrwaermeKwh > 0.0);
            Assert.True(lang.MehrwaermeKwh > kurz.MehrwaermeKwh);
        }

        [Fact]
        public void Mehrzonen_mit_Geltung_Zone_planen_und_pruefen_je_Zone()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, AufheizMehrzonenTests.Klima());
            foreach (ZonenEingang z in zonen)
                if (z.Eingang.ThetaSoll.Any(x => !double.IsNaN(x))) z.Eingang.HeizsollwertMitRampeSetzen(Kalender());
            Aufheizvorgabe v = Vorgabe(6, geltung: Vorheizgeltung.Zone);
            IReadOnlyList<Aufheizplan> plaene = Vorheizplanung.AnwendenZonen(zonen, v, 0, 1, "Dreizonen");
            GebaeudeStepper s = GebaeudeStepper.Mehrzonen(new Zonenschleife(zonen, "Dreizonen"));
            s.Beginnen();
            s.Jahr();
            GebaeudeModellErgebnis[] e = s.Abschluss(0, 1);
            Vorheizplanung.NachweisenZonen(zonen, e);

            List<Vorheizplan> mit = plaene.Where(p => p.Vorheizen != null).Select(p => p.Vorheizen).ToList();
            Assert.True(mit.Count >= 2);
            foreach (Vorheizplan vp in mit)
            {
                Assert.NotNull(vp.Nachweis);
                Assert.Equal(1.2 * vp.PhiKMaxW, vp.DeckelW, 6);
            }
            Assert.Contains(plaene, p => p.Unbeheizt);
            Vorheizgebaeude geb = Vorheizplanung.Gebaeudewerte(plaene, null);
            Assert.Equal(mit.Sum(x => x.DeckelW), geb.DeckelW, 6);
            Assert.Equal(mit.Max(x => x.Nachweis.UnterschreitungMaxK), geb.UnterschreitungMaxK);
            var tage = new bool[365];
            foreach (Vorheizplan vp in mit)
                for (int t = 0; t < 365; t++) tage[t] |= vp.Nachweis.TageOhneAnkunft[t];
            Assert.Equal(tage.Count(x => x), geb.TageOhneAnkunftAnzahl);
        }

        [Fact]
        public void AK1_rechnet_auf_dem_Kopplungsweg_mit_Schalter_an_und_faellt_mit_Schalter_aus_zurueck()
        {
            GebaeudeModellEingang e = Gekoppelt();
            Assert.True(e.KopplungWirksam);
            Assert.Equal(Vorheizrueckfall.Keiner, Vorheizplanung.Rueckfall(Vorgabe(6), DbWerte.ANLAGENKOPPLUNG_AK1, e.KopplungWirksam));
            (Aufheizplan plan, GebaeudeModellErgebnis lauf) = Rechnen(e, Vorgabe(6));
            Assert.NotNull(plan.Vorheizen.Nachweis);
            Assert.NotNull(lauf.Heizkreis);
            Assert.True(plan.Vorheizen.Spruenge.Count > 50);

            Aufheizvorgabe aus = Vorgabe(6, heizkreis: false);
            Vorheizrueckfall r = Vorheizplanung.Rueckfall(aus, DbWerte.ANLAGENKOPPLUNG_AK1, e.KopplungWirksam);
            Assert.Equal(Vorheizrueckfall.Heizkreis, r);
            Aufheizplan bestand = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(Gekoppelt()), Vorheizplanung.Wirksam(aus, r));
            Assert.Null(bestand.Vorheizen);
            Assert.True(bestand.Gekoppelt);
        }

        // =====================================================================
        //  Probe gegen die erste Ordnung (2.3)
        // =====================================================================

        /// <summary>
        /// <b>Probe gegen die erste Ordnung</b>: Bau ohne Sonne und Gewinne bei fester Außenluft −5 °C, lange bei 16 °C
        /// (Massen im Gleichgewicht, ΔT_m = 4 K), dann ein Fenster von 47 h vor 20 °C unter P_V = Φ_stat + Δ. Das Plateau des
        /// Laufs (Σ Kappungsanteile im Fenster) ist die Ankunftszeit; die erste Ordnung t = C_w·ΔT_m/(P_V − Φ_stat) lädt die
        /// ganze Masse auf das Gleichgewicht bei 20 °C und ist deshalb eine obere Schranke. Die untere: Bis zur Ankunft muss
        /// mindestens der Anteil der Luft- und Oberflächenstufe G_0 − H_s geladen sein, der den Sprung trägt — die Augenblicksform
        /// der ersten Ordnung (<see cref="Aufheizstufen.ErsteOrdnungAugenblick"/> ohne Masse) ist dafür eine sehr weite Grenze;
        /// das Band [0,25; 1,0]·t_1 ist daraus begründet; gemessen 0,40 bis 0,51 (die schnelle Mode lädt die Luftseite vor der Masse).
        /// </summary>
        [Theory]
        [InlineData(1.0)]
        [InlineData(2.0)]
        [InlineData(4.0)]
        public void Die_Ankunftszeit_liegt_im_Band_der_ersten_Ordnung(double deltaKw)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Interne_Waermegewinne = 0.0;
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(_ => -5.0, mitSonne: false));
            const int HS = 40 * 24 + 7;
            var soll = Enumerable.Repeat(16.0, STUNDEN).ToArray();
            for (int h = HS; h <= HS + 10; h++) soll[h] = 20.0;
            e.HeizsollwertMitRampeSetzen(soll);
            Aufheizzone zone = Aufheizzone.Aus(ZonenEingang.Einzeln(e));
            double phiStat = Vorheizplanung.PhiRef(zone, 20.0, HS - 1);
            Aufheizantwort antwort = zone.Modell.Aufheizantwort(zone.Strahlungsanteil, zone.Zusatzleitwert(HS - 1));

            (Aufheizplan plan, GebaeudeModellErgebnis lauf) = Rechnen(e, Vorgabe(47, Vorheiztoleranzart.Kilowatt, deltaKw));
            Vorheizplan vp = plan.Vorheizen;
            Assert.Single(vp.Spruenge);
            Assert.Equal(47, vp.Spruenge[0].FensterH);
            double reserve = vp.VorheizleistungW - phiStat;
            Assert.Equal(deltaKw * 1000.0, reserve, 3);

            double plateauH = 0.0;
            for (int h = HS - 47; h < HS; h++) plateauH += lauf.HeizleistungMaxAnteil[h];
            double massenK = 0.5 * (lauf.MassenEndeAw[HS - 48] + lauf.MassenEndeIw[HS - 48]);
            double tErsteOrdnungH = antwort.CwJk * 4.0 / reserve / 3600.0;
            _aus_Zeile(string.Format(CultureInfo.InvariantCulture,
                "Δ = {0} kW: Plateau {1:0.00} h, erste Ordnung {2:0.00} h, Verhältnis {3:0.000}; Massen vor dem Fenster {4:0.00} °C; δθ = {5:0.000} K",
                deltaKw, plateauH, tErsteOrdnungH, plateauH / tErsteOrdnungH, massenK, vp.Nachweis.Pruefungen[0].DeltaThetaK));
            // Die Massen stehen vor dem Fenster im Gleichgewicht bei 16 °C (ΔT_m = ΔT = 4 K).
            Assert.True(Math.Abs(lauf.MassenEndeAw[HS - 48] - lauf.MassenEndeAw[HS - 72]) < 0.01);
            Assert.True(Math.Abs(lauf.MassenEndeIw[HS - 48] - lauf.MassenEndeIw[HS - 72]) < 0.01);
            Assert.True(plateauH > 0.0);
            Assert.InRange(plateauH / tErsteOrdnungH, 0.25, 1.0);
        }

        private void _aus_Zeile(string text) => _aus.WriteLine(text);

        // =====================================================================
        //  Bitgleich und Messung über die Fassade (Testdatenbank)
        // =====================================================================

        [Fact]
        public void Mit_Verfahren_Sollwertrampe_rechnet_der_Lauf_bitgleich()
        {
            if (!_db.Vorhanden) return;
            var an = new Aufheizvorgabe(true, null, null, null, null);
            Aufheizvorgabe rampe = an with
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Sollwertrampe, 6, Vorheiztoleranzart.Kilowatt, 3.0, 0.5),
            };
            AufheizLauf.Gebaeudelauf a = AufheizLauf.Projekt(1018, an, double.NaN, x => x.ID_Gebaeude == 10632).Single();
            AufheizLauf.Gebaeudelauf b = AufheizLauf.Projekt(1018, rampe, double.NaN, x => x.ID_Gebaeude == 10632).Single();
            Assert.True(a.Gerechnet && b.Gerechnet);
            Assert.Null(b.Plan.Vorheizen);
            Assert.Null(b.Ergebnis.Vorheizen);
            for (int h = 0; h < STUNDEN; h++) Assert.Equal(Bits(a.Ziel[h]), Bits(b.Ziel[h]));
        }

        /// <summary>
        /// <b>Messung an Projekt 1051</b> (Entwurf 2.8, kein harter Wert — die Wache kommt mit V7): Verfahren Vorgabe mit
        /// t_V = 6 h und 15 h, Toleranz 20 %. Ausgabe: Φ_K,max, P_K, P_V, Spitze, Tage ohne Ankunft, Nächte ohne Absenkung,
        /// Mehrwärme.
        /// </summary>
        [Fact]
        public void Messung_an_Projekt_1051()
        {
            if (!_db.Vorhanden) return;
            using var k = new Kulturvorrichtung("de-DE");
            foreach (int tV in new[] { 6, 15 })
            {
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                foreach (AufheizLauf.Gebaeudelauf l in AufheizLauf.Projekt(1051, Vorgabe(tV)))
                {
                    Assert.True(l.Gerechnet, l.ToString());
                    Vorheizgebaeude v = l.Ergebnis.Vorheizen;
                    Assert.NotNull(v);
                    _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "1051 Gebäude {0}, t_V = {1} h: Φ_K,max {2:0.00} kW, P_K {3:0.00} kW, P_V {4:0.00} kW, Spitze {5:0.00} kW, " +
                        "S_max,0 {6:0.00} kW, S_max {7:0.00} kW, Tage ohne Ankunft {8}, Unterschreitung bis {9:0.00} K ({10} h), " +
                        "Deckelstunden {11}, Floorstunden {12}, Nächte ohne Absenkung {13}, aus „aus“ {14}, Heizwärme Vorlauf {15:0.0} kWh, " +
                        "Mehrwärme {16:0.0} kWh ({17:0.0} %)",
                        l.Gebaeude, tV, v.PhiKMaxW / 1000.0, v.DeckelW / 1000.0, v.VorheizleistungW / 1000.0, v.SpitzeW / 1000.0,
                        v.Sprung0MaxW / 1000.0, v.SprungMaxW / 1000.0, v.TageOhneAnkunftAnzahl, v.UnterschreitungMaxK,
                        v.UnterschreitungsstundenH, v.DeckelstundenH, v.FloorstundenH, v.NaechteOhneAbsenkung, v.SpruengeAus,
                        v.HeizwaermeVorlaufKwh, v.MehrwaermeKwh, v.MehrwaermeProzent));
                    Assert.True(v.DeckelW > v.PhiKMaxW);
                }
                foreach (string z in p.Hinweise.Where(z => z.Contains("Vorheiz", StringComparison.Ordinal))) _aus.WriteLine(z);
                Assert.Contains(p.Hinweise, z => z.Contains("Vorheizen " + tV.ToString(CultureInfo.InvariantCulture) + " h", StringComparison.Ordinal));
            }
        }
    }
}
