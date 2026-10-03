using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Betriebssimulation gegen das Wärmespeicher-Tool</b> (Konzept 10): die Fälle aus
    /// <c>Quellen/Waermespeicher-Tool/Waermespeicher-Tool/tests/test_zweipunkt.py</c> und
    /// <c>test_sizing.py</c> als C#-Fälle. Wo das Tool mit 1,163 Wh/(l·K) rechnet, hält der Kern 1,16 —
    /// Toleranz 0,5 %. Ohne Datenbank.
    /// </summary>
    public class BetriebssimulationTests
    {
        private const double TOOL_C = 1.163;

        private static double[] Konstant(int n, double kw) => Enumerable.Repeat(kw, n).ToArray();

        private static void Relativ(double erwartet, double ist, double rel = 0.005) =>
            Assert.True(Math.Abs(ist - erwartet) <= rel * Math.Abs(erwartet), "erwartet " + erwartet + ", ist " + ist);

        // ---- test_zweipunkt.py ----

        [Fact]
        public void Zweipunkt_Handrechnung_konstante_Last()
        {
            ZweipunktErgebnis s = Betriebssimulation.Zweipunkt(Konstant(100, 10), Konstant(100, 30), 40, 0.2, 1.0, 0, false, 0);
            Assert.Equal(new double[] { 20, 40, 30, 20, 10, 30, 40, 30, 20, 10 }, s.Fuellstand.Take(10).ToArray());
            Assert.Equal(new double[] { 30, 30, 0, 0, 0, 30, 20, 0, 0, 0 }, s.Leistung.Take(10).ToArray());
            Assert.Equal(new[] { true, true, false, false, false, true, true, false, false, false }, s.Laden.Take(10).ToArray());
            for (int i = 5; i < 95; i++) Assert.Equal(s.Fuellstand[i], s.Fuellstand[i + 5], 9);
            Assert.Equal(19, s.Starts);
            Assert.Equal(0.0, s.UnterdeckungKwh);
            Assert.Equal(1.0, s.Deckungsgrad, 9);
            Assert.True(s.Fuellstand.Min() >= 8 - 1e-9 && s.Fuellstand.Max() <= 40 + 1e-9);
            Assert.InRange(s.Laden.Count(x => x) / (double)s.Starts, 1.85, 2.15);
        }

        [Fact]
        public void Kleinerer_Speicher_taktet_haeufiger()
        {
            ZweipunktErgebnis gross = Betriebssimulation.Zweipunkt(Konstant(2000, 10), Konstant(2000, 30), 40, 0.2, 1.0);
            ZweipunktErgebnis klein = Betriebssimulation.Zweipunkt(Konstant(2000, 10), Konstant(2000, 30), 20, 0.2, 1.0);
            Assert.Equal(399, gross.Starts);
            Assert.Equal(999, klein.Starts);
            Assert.Equal(2.0, gross.Laden.Count(x => x) / (double)gross.Starts, 1);
            Assert.Equal(1.0, klein.Laden.Count(x => x) / (double)klein.Starts, 1);
        }

        [Fact]
        public void Umschaltung_im_selben_Schritt()
        {
            ZweipunktErgebnis s = Betriebssimulation.Zweipunkt(Konstant(6, 30), Konstant(6, 200), 100, 0.2, 1.0, 0, false, 100);
            Assert.Equal(100, s.Fuellstand[0], 9);
            Assert.Equal(30, s.Leistung[0], 9);
            Assert.Equal(new double[] { 70, 40, 100 }, s.Fuellstand.Skip(1).Take(3).ToArray());
            Assert.Equal(new[] { false, false, true }, s.Laden.Skip(1).Take(3).ToArray());
            Assert.True(s.Fuellstand.Min() >= 20 - 1e-9);
            Assert.Equal(0.0, s.UnterdeckungKwh);
        }

        [Fact]
        public void Unterdeckung_haelt_den_Mindestfuellstand()
        {
            ZweipunktErgebnis s = Betriebssimulation.Zweipunkt(Konstant(5, 50), Konstant(5, 20), 100, 0.2, 1.0, 0, false, 0);
            Assert.All(s.Fuellstand, x => Assert.Equal(20, x, 9));
            Assert.Equal(50, s.Unterdeckung[0], 9);
            Assert.All(s.Unterdeckung.Skip(1), x => Assert.Equal(30, x, 9));
            Assert.Equal(5, s.UnterdeckungH);
            Assert.Equal(170, s.UnterdeckungKwh, 9);
        }

        [Fact]
        public void Sperrmaske_sperrt_den_Erzeuger_und_bricht_ueber_Mitternacht_um()
        {
            double[] m = Betriebssimulation.Verfuegbarkeit(48, new[] { new PufferSperrfenster(11, 2), new PufferSperrfenster(17, 2) });
            Assert.Equal(40, m.Sum());
            Assert.Equal(0, m[11]); Assert.Equal(0, m[12]); Assert.Equal(1, m[13]); Assert.Equal(0, m[17]); Assert.Equal(0, m[18]);
            double[] m2 = Betriebssimulation.Verfuegbarkeit(48, new[] { new PufferSperrfenster(23, 2) });
            Assert.Equal(0, m2[23]); Assert.Equal(0, m2[0]); Assert.Equal(1, m2[1]); Assert.Equal(0, m2[24]);

            double[] p = Betriebssimulation.Erzeugerleistung(48, 30, 0, false, m);
            ZweipunktErgebnis s = Betriebssimulation.Zweipunkt(Konstant(48, 10), p, 40, 0.2, 1.0);
            for (int i = 0; i < 48; i++) if (m[i] == 0) Assert.Equal(0.0, s.Leistung[i]);
            ZweipunktErgebnis frei = Betriebssimulation.Zweipunkt(Konstant(48, 10), Konstant(48, 30), 40, 0.2, 1.0);
            Assert.True(Enumerable.Range(0, 48).Where(i => m[i] == 0).Max(i => frei.Leistung[i]) > 0);
        }

        [Fact]
        public void Lange_Sperre_erzwingt_Unterdeckung_nur_in_der_Sperre()
        {
            double[] m = Betriebssimulation.Verfuegbarkeit(72, new[] { new PufferSperrfenster(0, 8) });
            ZweipunktErgebnis s = Betriebssimulation.Zweipunkt(Konstant(72, 20), Betriebssimulation.Erzeugerleistung(72, 60, 0, false, m),
                                                               40, 0.2, 1.0);
            Assert.True(s.UnterdeckungKwh > 0);
            for (int i = 0; i < 72; i++) if (m[i] == 1) Assert.Equal(0.0, s.Unterdeckung[i]);
            Assert.True(s.Deckungsgrad < 1);
            Assert.True(s.Fuellstand.Min() >= 8 - 1e-9);
        }

        [Fact]
        public void Modulationsmodus_startet_nur_unter_der_Mindestleistung()
        {
            // Last 10 kW, Mindestleistung 5 kW: das geregelte Gerät folgt der Last und startet nie neu.
            ZweipunktErgebnis mod = Betriebssimulation.Zweipunkt(Konstant(48, 10), Konstant(48, 30), 40, 0.1, 0.95, 5, true, 4);
            ZweipunktErgebnis ein = Betriebssimulation.Zweipunkt(Konstant(48, 10), Konstant(48, 30), 40, 0.1, 0.95, 0, false, 4);
            Assert.Equal(0, mod.Starts);
            Assert.True(ein.Starts > 5);
            // Last 2 kW unter der Mindestleistung: auch das geregelte Gerät taktet.
            ZweipunktErgebnis takt = Betriebssimulation.Zweipunkt(Konstant(48, 2), Konstant(48, 30), 40, 0.1, 0.95, 5, true, 4);
            Assert.True(takt.Starts > 0);
            Assert.Equal(0.0, takt.UnterdeckungKwh);
        }

        // ---- test_sizing.py ----

        [Fact]
        public void Rollierendes_Mittel_aus_dem_Profil()
        {
            double[] profil = Enumerable.Range(0, 120).Select(i => i % 24 < 4 ? 40.0 : 10.0).ToArray();
            Assert.Equal(40, Betriebssimulation.MaxMittelleistung(profil, 2), 9);
            Assert.Equal(40, Betriebssimulation.MaxMittelleistung(profil, 4), 9);
            Assert.Equal((4 * 40.0 + 2 * 10.0) / 6.0, Betriebssimulation.MaxMittelleistung(profil, 6), 9);
        }

        [Fact]
        public void Durchlauf_Rechteckfall_gegen_die_Referenzschleife()
        {
            double[] last = Enumerable.Range(0, 240).Select(i => i % 24 >= 6 && i % 24 < 10 ? 100.0 : 20.0).ToArray();
            // Tool: c_ref = 4 · (100 − 40) = 240 kWh; Volumen bei 10 K = 240 000 / (1,163 · 10).
            var (v, ok, lauf) = Betriebssimulation.MindestvolumenDeckung(last, Konstant(240, 40), 1.0, 1.16 * 10 / 1000.0, 100000);
            Assert.True(ok);
            Assert.True(lauf.Erreicht(1.0));
            Relativ(240.0 * 1000 / (TOOL_C * 10), v);
        }

        [Fact]
        public void Durchlauf_mit_Sperrzeiten_gegen_die_Referenzschleife()
        {
            double[] m = Betriebssimulation.Verfuegbarkeit(240, new[] { new PufferSperrfenster(11, 2), new PufferSperrfenster(17, 2) });
            var (v, ok, _) = Betriebssimulation.MindestvolumenDeckung(Konstant(240, 30), Betriebssimulation.Erzeugerleistung(240, 60, 0, false, m),
                                                                      1.0, 1.16 * 10 / 1000.0, 100000);
            Assert.True(ok);
            Relativ(60.0 * 1000 / (TOOL_C * 10), v);   // c_ref = 2 h · 30 kW = 60 kWh
        }

        [Fact]
        public void Durchlauf_ohne_erreichbare_Deckung_ist_ungueltig()
        {
            var (v, ok, lauf) = Betriebssimulation.MindestvolumenDeckung(Konstant(720, 100), Konstant(720, 20), 1.0, 1.16 * 10 / 1000.0, 100000);
            Assert.False(ok);
            Assert.Equal(100000, v);
            Assert.False(lauf.Erreicht(1.0));
        }

        [Fact]
        public void Deckende_Erzeugerleistung_braucht_keinen_Speicher()
        {
            var (v, ok, _) = Betriebssimulation.MindestvolumenDeckung(Konstant(120, 20), Konstant(120, 50), 1.0, 1.16 * 10 / 1000.0, 100000);
            Assert.True(ok);
            Assert.Equal(0.0, v);
        }

        [Fact]
        public void Abtau_Taktung_und_Sperrzeit_wie_das_Tool()
        {
            PufferAuslegungParameter p = PufferAuslegungParameter.Vorgabe();
            Assert.Equal(1000, HeizzoneRechner.K2Faustwert(50, false, p), 9);
            // 15 kW · 10 min, 10 K, ohne Hysterese: Tool 214,96 l.
            Relativ(214.96, HeizzoneRechner.K3Mindestlaufzeit(15, 10, 1.16, 10, 1.0));
            Relativ(214.96 / 2, HeizzoneRechner.K3Mindestlaufzeit(15, 10, 1.16, 20, 1.0));
            // Sperrzeit ohne Profil: P_WP 50 kW · 2 h: Tool 8 598,45 l; aus dem Profil 40 kW: 6 878,76 l; zwei Fenster: das 3-h-Fenster.
            Relativ(8598.45, HeizzoneRechner.K4Experte(50, 2, 1.16, 10, 1.0));
            Relativ(6878.76, HeizzoneRechner.K4Experte(40, 2, 1.16, 10, 1.0));
            Relativ(40.0 * 3 * 1000 / (TOOL_C * 10), HeizzoneRechner.K4Experte(40, 3, 1.16, 10, 1.0));
        }

        [Fact]
        public void Sperrzeit_ueber_die_Fassade_ohne_und_mit_Profil()
        {
            var basis = new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO, SperrzeitExpertenweg = true,
                Erzeuger = new PufferErzeuger { NennleistungKw = 50, IstWaermepumpe = true },
                VorlaufC = 45, RuecklaufC = 35, SchwelleEin = 0, SchwelleAus = 1, AuslegungsheizlastKw = 50,
                Uebergabeart = "FLAECHE", Sperrfenster = new[] { new PufferSperrfenster(11, 2) }
            };
            PufferAuslegungErgebnis ohne = PufferAuslegung.Rechnen(basis);
            Assert.Equal("K4e", ohne.Zone(PufferZone.Heizung).Bemessend);
            Relativ(8598.45, ohne.Zone(PufferZone.Heizung).VolumenL);

            double[] profil = Enumerable.Range(0, 120).Select(i => i % 24 < 4 ? 40.0 : 10.0).ToArray();
            PufferAuslegungErgebnis mit = PufferAuslegung.Rechnen(basis with { ReiheHeizung = profil });
            Relativ(6878.76, mit.Zone(PufferZone.Heizung).Kriterium("K4e").VolumenL.Value);
        }

        [Fact]
        public void Massgebend_ist_das_groesste_Kriterium()
        {
            // Abtau/Faustwert 1 000 l > Taktung 215 l, keine Sperrzeit -> K2; mit Sperrzeit -> K4e.
            var e = new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO, SperrzeitExpertenweg = true,
                Erzeuger = new PufferErzeuger { NennleistungKw = 50, IstWaermepumpe = true },
                VorlaufC = 45, RuecklaufC = 35, SchwelleEin = 0, SchwelleAus = 1, AuslegungsheizlastKw = 50, Uebergabeart = "FLAECHE",
                MindestlaufzeitMin = 10
            };
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            Assert.Equal("K2", r.Zone(PufferZone.Heizung).Bemessend);
            Assert.Equal(1000, r.EmpfehlungL);
            PufferAuslegungErgebnis r2 = PufferAuslegung.Rechnen(e with { Sperrfenster = new[] { new PufferSperrfenster(11, 2) } });
            Assert.Equal("K4e", r2.Zone(PufferZone.Heizung).Bemessend);
            Assert.Equal(10000, r2.EmpfehlungL);
        }
    }
}
