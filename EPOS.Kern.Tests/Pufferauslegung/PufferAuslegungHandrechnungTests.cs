using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Handrechnungen des Konzepts</b> (Konzept Pufferspeicher-Auslegung, Abschnitt 10;
    /// Recherche Runde 3, Abschnitt 5.4), Konstante 1,16 Wh/(l·K). Ohne Datenbank.
    /// </summary>
    public class PufferAuslegungHandrechnungTests
    {
        private static readonly PufferAuslegungParameter P = PufferAuslegungParameter.Vorgabe();

        private static void Nahe(double erwartet, double ist, double abs) =>
            Assert.True(Math.Abs(ist - erwartet) <= abs, "erwartet " + erwartet + ", ist " + ist);

        [Fact]
        public void K2_Faustwert_nach_Geraetetyp()
        {
            Assert.Equal(1200, HeizzoneRechner.K2Faustwert(60, false, P), 9);
            Assert.Equal(180, HeizzoneRechner.K2Faustwert(60, true, P), 9);
        }

        [Fact]
        public void K3_Mindestlaufzeit_und_Gleichung_22()
        {
            // 18 kW · 10 min / (1,16 · 10 K · 0,85) = 304 l.
            Nahe(304.0, HeizzoneRechner.K3Mindestlaufzeit(18, 10, 1.16, 10, 0.85), 0.5);
            // VDI 4645 Anhang I3: 13,8 kW · 5 min, 30 K -> 1,15 kWh -> 33,0 l (ohne nutzbaren Anteil).
            Nahe(33.0, HeizzoneRechner.K3Mindestlaufzeit(13.8, 5, 1.16, 30, 1.0), 0.05);
        }

        /// <summary>Der Fall des Mockups: 82 kW, Sperre 2 h, Radiatoren, Heizgrenze 15 °C, 55/45 °C, 400 l Anlagenvolumen.</summary>
        private static PufferAuslegungEingang Mockup() => new PufferAuslegungEingang
        {
            KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO,
            Erzeuger = new PufferErzeuger { NennleistungKw = 82, IstWaermepumpe = true },
            VorlaufC = 55, RuecklaufC = 45, SchwelleEin = 0.10, SchwelleAus = 0.95,
            AuslegungsheizlastKw = 82, Uebergabeart = "RADIATOR", HeizgrenzeC = 15, AnlagenvolumenL = 400,
            Sperrfenster = new[] { new PufferSperrfenster(11, 2) }
        };

        [Fact]
        public void K4_Standardweg_nach_Gleichung_23()
        {
            Assert.Equal(1.0, HeizzoneRechner.Stillstand(15, "RADIATOR", P));
            Assert.Equal(1.5, HeizzoneRechner.Stillstand(15, "FLAECHE", P));
            Assert.Equal(3.5, HeizzoneRechner.Stillstand(10, "FLAECHE", P));
            Assert.Equal(15.0, HeizzoneRechner.Uebertemperatur("RADIATOR", P));
            // (82 · (2 − 1) · 1000) / (1,16 · ((55 + 5) − (20 + 15))) − 400 = 2 428 l.
            Nahe(2427.6, HeizzoneRechner.K4Standard(82, 2, 1, 55, 5, 20, 15, 400, 1.16).Value, 0.1);

            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Mockup());
            PufferZonenergebnis h = r.Zone(PufferZone.Heizung);
            Assert.Equal("K4", h.Bemessend);
            Nahe(2427.6, h.VolumenL, 0.1);
            Assert.Equal(3000, r.EmpfehlungL);
            Assert.Equal("Heizung: K4", r.Bemessend);
        }

        [Fact]
        public void K4_Expertenweg_aus_dem_Lastgang()
        {
            // 82 kW · 2 h · 1000 / (1,16 · 10 K · 0,85) = 16 630 l (gegenübergestellt; ohne Reihe gilt die Nennleistung).
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Mockup() with { SperrzeitExpertenweg = true });
            PufferZonenergebnis h = r.Zone(PufferZone.Heizung);
            Nahe(16630, h.Kriterium("K4e").VolumenL.Value, 5);
            Assert.Equal("K4e", h.Bemessend);
            Nahe(2427.6, h.Kriterium("K4").VolumenL.Value, 0.1);
            Assert.False(h.Kriterium("K4").Aktiv);
            Assert.Equal(17000, r.EmpfehlungL);
            Assert.True(r.UeberListenende);
        }

        [Fact]
        public void K9_Festbrennstoff_beide_Wege()
        {
            Assert.Equal(1650, HeizzoneRechner.K9Faustwert(PufferBrennstoff.Scheitholz, 30, P), 9);
            Assert.Equal(900, HeizzoneRechner.K9Faustwert(PufferBrennstoff.Pellets, 30, P), 9);
            Assert.Equal(600, HeizzoneRechner.K9Faustwert(PufferBrennstoff.Keiner, 30, P), 9);
            // 15 · 30 kW · 4 h · (1 − 0,3 · 20 kW / 15 kW) = 1 080 l.
            Assert.Equal(1080, HeizzoneRechner.K9Din303(30, 4, 20, 15), 9);

            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.FESTBRENNSTOFF,
                Erzeuger = new PufferErzeuger { NennleistungKw = 30, MindestleistungKw = 15, Brennstoff = PufferBrennstoff.Scheitholz },
                VorlaufC = 80, RuecklaufC = 60, AuslegungsheizlastKw = 20, Uebergabeart = "RADIATOR"
            });
            PufferZonenergebnis h = r.Zone(PufferZone.Heizung);
            Assert.Equal(1650, h.Kriterium("K9").VolumenL.Value, 9);
            Assert.Equal(1080, h.Kriterium("K9e").VolumenL.Value, 9);
            Assert.Equal("K9", h.Bemessend);
            Assert.Equal(2000, r.EmpfehlungL);
        }

        [Fact]
        public void Band_nach_Uebergabeart()
        {
            Assert.Equal((1500.0, 2700.0), HeizzoneRechner.Band("RADIATOR", 60, P));
            Assert.Equal((600.0, 1200.0), HeizzoneRechner.Band("FLAECHE", 60, P));
            Assert.Equal((1500.0, 2700.0), HeizzoneRechner.Band("KONVEKTOR", 60, P));
            Assert.Equal((720.0, 2100.0), HeizzoneRechner.BandWp(60, P));
        }

        [Fact]
        public void Kombipuffer_2000_und_700_ergibt_3000()
        {
            // Heizzone über K2: 20 l/kW · 100 kW = 2 000 l. Brauchwasser: 24 kWh · 1,15 · 1000 / (1,16 · 40 K · 0,85) = 700 l.
            Assert.Equal(699.8, BrauchwasserzoneRechner.Frischwasservolumen(24, 0, 0.15, 1.16, 40, 0.85), 1);
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseHeizung = true, KlasseBrauchwasser = true, Vorlage = PufferVorlage.WP_MONO,
                Erzeuger = new PufferErzeuger { NennleistungKw = 100, IstWaermepumpe = true },
                VorlaufC = 55, RuecklaufC = 45, AuslegungsheizlastKw = 100, Uebergabeart = "FLAECHE",
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 24, ZirkulationKwhD = 0 }
            });
            Assert.Equal(2000, r.Zone(PufferZone.Heizung).VolumenL, 6);
            Assert.Equal("K2", r.Zone(PufferZone.Heizung).Bemessend);
            Nahe(699.8, r.Zone(PufferZone.Brauchwasser).VolumenL, 0.1);
            Nahe(2699.8, r.SummeL, 0.1);
            Assert.Equal(3000, r.EmpfehlungL);
            Assert.Equal(2, r.Kennzahlen.SchichtenMindest);
            Nahe(2000 / 2699.8, r.Kennzahlen.ZonenanteilHeizung.Value, 1e-4);
            Assert.True(r.HatWarnung(PufferWarncode.EXTRAPOLATION));   // 3 000 l ohne Katalogsatz
        }

        [Fact]
        public void Brauchwasser_40_Personen_und_Ueberdimensionierung()
        {
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseBrauchwasser = true, Vorlage = PufferVorlage.KESSEL, VorlaufC = 70, RuecklaufC = 40,
                Zapfprofil = new PufferZapfprofil
                {
                    Topologie = PufferBwTopologie.Speicher, NenninhaltL = 3500, Personen = 40, TagesbedarfL = 1600, DmaxKwh = 50
                }
            });
            Assert.Equal(40, r.Kennzahlen.LiterJePerson.Value, 9);
            Assert.True(r.Kennzahlen.LiterJePersonImBand);
            Assert.Equal(3500, r.Zone(PufferZone.Brauchwasser).VolumenL);
            Assert.True(r.HatWarnung(PufferWarncode.BW_UEBERDIMENSIONIERT));
            Assert.True(r.HatWarnung(PufferWarncode.HYGIENE_W551));
            Assert.Equal(5000, r.EmpfehlungL);
        }

        [Fact]
        public void Zirkulation_je_Wohneinheit()
        {
            Assert.Equal(48, BrauchwasserzoneRechner.ZirkulationJeWe(20, 100), 9);
            var e = new PufferAuslegungEingang
            {
                ZirkulationWeg = PufferZirkulationWeg.JE_WE, Wohneinheiten = 20,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 24, TagesbedarfKwh = 96 }
            };
            Assert.Equal(PufferZirkulationWeg.JE_WE, BrauchwasserzoneRechner.Weg(e));
            Assert.Equal(48, BrauchwasserzoneRechner.ZirkulationKwhD(e, P, 1.16, PufferZirkulationWeg.JE_WE).Value, 9);
            // Zuschlag im Verhältnis D_max / Q_d: 48 · 24 / 96 = 12 kWh -> (24 + 12) · 1,15 · 1000 / (1,16 · 40 · 0,85).
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e with { KlasseBrauchwasser = true, VorlaufC = 65, RuecklaufC = 25 });
            Nahe(36 * 1.15 * 1000 / (1.16 * 40 * 0.85), r.Zone(PufferZone.Brauchwasser).VolumenL, 1e-6);
            Assert.Equal(48, r.Kennzahlen.ZirkulationKwhD.Value, 9);
            // Ohne Zirkulation im Zapfprofil und ohne Projektwert gilt der Anteil 35 %.
            Assert.Equal(PufferZirkulationWeg.ANTEIL, BrauchwasserzoneRechner.Weg(e with { ZirkulationWeg = null }));
            Assert.Equal(PufferZirkulationWeg.PROJEKT, BrauchwasserzoneRechner.Weg(e with { ZirkulationWeg = null, ZirkulationProjektKw = 1 }));
        }

        [Fact]
        public void K11_Bereitschaftsverlust()
        {
            // 2,5 kWh/d -> 2 500 / (24 · 45) = 2,315 W/K; Betriebsfaktor (45 − 15)/45 = 0,667 -> 608 kWh/a.
            PufferVerlust k = HeizzoneRechner.Bereitschaftsverlust(1000, 2.5, 50, 40, P);
            Assert.True(k.AusKatalog);
            Nahe(2.315, k.WJeK, 0.0005);
            Nahe(0.667, k.Betriebsfaktor, 0.0005);
            Nahe(608, k.KwhJeJahr, 0.5);
            // Klasse-C-Grenze 1 000 l: 16,66 + 8,33 · 1000^0,4 = 148,7 W -> 3,568 kWh/d -> 3,304 W/K.
            Nahe(148.7, HeizzoneRechner.KlasseCGrenzeW(1000), 0.05);
            PufferVerlust c = HeizzoneRechner.Bereitschaftsverlust(1000, null, 50, 40, P);
            Assert.False(c.AusKatalog);
            Nahe(3.568, c.KwhJeTag, 0.0015);
            Nahe(3.304, c.WJeK, 0.0005);
            Assert.False(c.Extrapoliert);
            Assert.True(HeizzoneRechner.Bereitschaftsverlust(2500, null, 50, 40, P).Extrapoliert);
            Assert.False(HeizzoneRechner.Bereitschaftsverlust(2500, 4.0, 50, 40, P).Extrapoliert);
        }

        [Fact]
        public void K10_Solar_und_Rundung()
        {
            Assert.Equal(1000, HeizzoneRechner.K10Solar(20, PufferKollektorart.Flach, P), 9);
            Assert.Equal(1300, HeizzoneRechner.K10Solar(20, PufferKollektorart.Roehre, P), 9);
            Assert.Equal(1500, PufferAuslegung.Runden(1001, null, 1000, out bool u1));
            Assert.False(u1);
            Assert.Equal(11000, PufferAuslegung.Runden(10001, null, 1000, out bool u2));
            Assert.True(u2);
            Assert.Equal(0, PufferAuslegung.Runden(0, null, 1000, out _));
        }
    }
}
