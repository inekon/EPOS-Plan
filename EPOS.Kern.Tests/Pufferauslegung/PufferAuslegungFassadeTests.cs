using System;
using System.Linq;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Die Fassade der Pufferspeicher-Auslegung</b> (Konzept 3.4–3.6, 5): Determinismus über ein
    /// ganzes Jahr, die Warncodes der Liste, Vorprüfung, Praxisgrenze, Katalogvorschlag, Prozesszone und
    /// die Ableitung des Nutzungsprofils. Ohne Datenbank.
    /// </summary>
    public class PufferAuslegungFassadeTests
    {
        /// <summary>Eine synthetische Heizreihe über 8 760 h: Heizgrenze 15 °C, Tagesgang, keine Zufallsgröße.</summary>
        private static double[] Jahresreihe(double spitzeKw)
        {
            var r = new double[8760];
            for (int i = 0; i < 8760; i++)
            {
                double tag = i / 24;
                double aussen = 8 - 10 * Math.Cos(2 * Math.PI * (tag - 15) / 365.0) + 3 * Math.Sin(2 * Math.PI * (i % 24 - 9) / 24.0);
                r[i] = Math.Max(0, (15 - aussen) / 27.0 * spitzeKw);
            }
            return r;
        }

        private static PufferAuslegungEingang Waermepumpe() => new PufferAuslegungEingang
        {
            KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO,
            Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = false },
            VorlaufC = 50, RuecklaufC = 40, Uebergabeart = "FLAECHE", HeizgrenzeC = 15,
            ReiheHeizung = Jahresreihe(40), Sperrfenster = PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI")
        };

        [Fact]
        public void Zweimal_gerechnet_ist_gleich()
        {
            PufferAuslegungEingang e = Waermepumpe() with { KlasseBrauchwasser = true,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 20, TagesbedarfL = 2000, Personen = 50 } };
            string a = JsonSerializer.Serialize(PufferAuslegung.Rechnen(e));
            string b = JsonSerializer.Serialize(PufferAuslegung.Rechnen(e));
            Assert.Equal(a, b);
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            PufferZonenergebnis h = r.Zone(PufferZone.Heizung);
            Assert.NotNull(h.Kriterium("D1"));
            Assert.NotNull(h.Kriterium("D2"));
            Assert.NotNull(h.Betriebsbild);
            Assert.True(h.Betriebsbild.Heizstunden > 0 && h.Betriebsbild.Heizstunden < 8760);
            Assert.True(r.EmpfehlungL >= r.SummeL);
            Assert.All(h.Kriterien, k => Assert.False(string.IsNullOrEmpty(k.Herkunft)));
        }

        [Fact]
        public void D2_haelt_das_Startziel()
        {
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Waermepumpe() with { StartzielJeTag = 6 });
            PufferKriterium d2 = r.Zone(PufferZone.Heizung).Kriterium("D2");
            Assert.True(d2.Gueltig);
            // Mit dem D2-Volumen gerechnet hält die Simulation das Ziel.
            double[] reihe = Jahresreihe(40);
            double[] p = Betriebssimulation.Erzeugerleistung(8760, 40, 0, false,
                Betriebssimulation.Verfuegbarkeit(8760, PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI")));
            double kap = d2.VolumenL.Value * 1.16 * 10 / 1000.0;
            PufferBetriebsbild b = Betriebssimulation.Bild(Betriebssimulation.Zweipunkt(reihe, p, kap, 0.1, 0.95, 0, false, 0.1 * kap), reihe, d2.VolumenL.Value);
            Assert.True(b.StartsJeTag <= 6 + 1e-9, "Starts je Tag " + b.StartsJeTag);
        }

        [Fact]
        public void Vorpruefung_ohne_Puffer_und_ohne_Puffer_nur_geregelt()
        {
            PufferAuslegungEingang e = Waermepumpe() with
            {
                Einzelraumregelung = false, AnlagenvolumenL = 300, Sperrfenster = Array.Empty<PufferSperrfenster>(),
                Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = true }
            };
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            Assert.True(r.Zone(PufferZone.Heizung).KeinPuffer);
            Assert.Equal(0, r.EmpfehlungL);
            Assert.True(r.HatWarnung(PufferWarncode.KEIN_PUFFER));

            PufferAuslegungErgebnis f = PufferAuslegung.Rechnen(e with { Erzeuger = e.Erzeuger with { Geregelt = false } });
            Assert.False(f.Zone(PufferZone.Heizung).KeinPuffer);
            Assert.True(f.HatWarnung(PufferWarncode.OHNE_PUFFER_GEREGELT));
            Assert.True(f.EmpfehlungL > 0);
        }

        [Fact]
        public void Bivalenz_Uebergabe_und_Abtaureserve()
        {
            PufferAuslegungEingang frei = Waermepumpe() with
            {
                Vorlage = PufferVorlage.WP_BIVALENT, Uebergabeart = null, Trinkwasservorrang = true, AuslegungsheizlastKw = 200,
                Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, ZweiterzeugerKw = 30, ZweiterzeugerFrei = true }
            };
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(frei);
            Assert.True(r.HatWarnung(PufferWarncode.ZWEITERZEUGER_FREI));
            Assert.False(r.Zone(PufferZone.Heizung).Kriterium("K4").Aktiv);
            Assert.True(r.HatWarnung(PufferWarncode.UEBERGABE_UNBEKANNT));

            PufferAuslegungErgebnis s = PufferAuslegung.Rechnen(frei with { Erzeuger = frei.Erzeuger with { Heizstab = true } });
            Assert.True(s.HatWarnung(PufferWarncode.HEIZSTAB_GESPERRT));
            Assert.False(s.HatWarnung(PufferWarncode.ZWEITERZEUGER_FREI));
            Assert.True(s.Zone(PufferZone.Heizung).Kriterium("K4").Aktiv);

            // Trinkwasservorrang: geregelt 3 l/kW · 40 kW = 120 l < 20 l/kW · 40 kW Heizlast.
            PufferAuslegungErgebnis a = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO, VorlaufC = 35, RuecklaufC = 30, Uebergabeart = "FLAECHE",
                Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = true, MindestleistungKw = 1 },
                AuslegungsheizlastKw = 40, Trinkwasservorrang = true
            });
            Assert.True(a.HatWarnung(PufferWarncode.ABTAU_VORRANG));
            Assert.False(PufferAuslegung.Rechnen(Waermepumpe()).HatWarnung(PufferWarncode.ABTAU_VORRANG));
        }

        [Fact]
        public void Band_Praxisgrenze_und_leere_Reihe()
        {
            // Heizlast 82 kW an Radiatoren, nur K2 geregelt 3 l/kW = 120 l -> unter dem Band.
            var e = new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO, VorlaufC = 55, RuecklaufC = 45,
                Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = true, MindestleistungKw = 1 },
                AuslegungsheizlastKw = 82, Uebergabeart = "RADIATOR"
            };
            PufferAuslegungErgebnis u = PufferAuslegung.Rechnen(e);
            Assert.True(u.HatWarnung(PufferWarncode.BAND_UNTER));
            Assert.True(u.HatWarnung(PufferWarncode.KEINE_REIHE));

            // Festbrennstoff mit 2 000 kW: 110 000 l > Praxisgrenze.
            PufferAuslegungErgebnis g = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.FESTBRENNSTOFF, VorlaufC = 80, RuecklaufC = 60, Uebergabeart = "RADIATOR",
                Erzeuger = new PufferErzeuger { NennleistungKw = 2000, Brennstoff = PufferBrennstoff.Scheitholz }, AuslegungsheizlastKw = 1500
            });
            Assert.True(g.AnPraxisgrenze);
            Assert.Equal(100000, g.EmpfehlungL);
            Assert.True(g.HatWarnung(PufferWarncode.PRAXISGRENZE));
            Assert.True(g.HatWarnung(PufferWarncode.BAND_UEBER));
        }

        [Fact]
        public void Starts_je_Tag_und_je_Heizperiode()
        {
            // Kessel Ein/Aus 30 kW an 10 kW Dauerlast, Startziel 24: D2 bemisst nicht, K3 = 507 l -> jede Stunde ein Start.
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.KESSEL, VorlaufC = 60, RuecklaufC = 50, Uebergabeart = "RADIATOR",
                Erzeuger = new PufferErzeuger { NennleistungKw = 30 }, ReiheHeizung = Enumerable.Repeat(10.0, 8760).ToArray(),
                StartzielJeTag = 24
            });
            PufferZonenergebnis h = r.Zone(PufferZone.Heizung);
            Assert.Equal("K3", h.Bemessend);
            Assert.True(h.Betriebsbild.StartsJeTag > 15);
            Assert.True(r.HatWarnung(PufferWarncode.STARTS_TAG));
            Assert.True(r.HatWarnung(PufferWarncode.STARTS_JAHR));
        }

        [Fact]
        public void Kombi_mit_Trinkwasserspeicher_und_Hygiene()
        {
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Waermepumpe() with
            {
                KlasseBrauchwasser = true, TPufferObenC = 50,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Speicher, NenninhaltL = 300 }
            });
            Assert.True(r.HatWarnung(PufferWarncode.TANK_IM_TANK));
            PufferAuslegungErgebnis f = PufferAuslegung.Rechnen(Waermepumpe() with
            {
                KlasseBrauchwasser = true, TPufferObenC = 50,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Wohnungsstation, DmaxKwh = 10 }
            });
            Assert.True(f.HatWarnung(PufferWarncode.HYGIENE_TEMPERATUR));
            Assert.False(f.HatWarnung(PufferWarncode.TANK_IM_TANK));
        }

        [Fact]
        public void Katalogvorschlag_nach_Bauform()
        {
            var katalog = new[]
            {
                new PufferKatalogsatz(1, "Puffer 1, 1000 l", 1000, 2.0, false),
                new PufferKatalogsatz(2, "Puffer 2, 3000 l", 3000, 3.0, false),
                new PufferKatalogsatz(3, "Kombi 1, 3000 l", 3000, 3.5, true),
            };
            var e = new PufferAuslegungEingang
            {
                KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO, VorlaufC = 55, RuecklaufC = 45, Uebergabeart = "FLAECHE",
                Erzeuger = new PufferErzeuger { NennleistungKw = 100, IstWaermepumpe = true }, AuslegungsheizlastKw = 100, Katalog = katalog
            };
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
            Assert.Equal(2, r.Katalogvorschlag.Id);
            Assert.True(r.Kennzahlen.Verlust.AusKatalog);
            Assert.Equal(3.0, r.Kennzahlen.Verlust.KwhJeTag);
            PufferAuslegungErgebnis k = PufferAuslegung.Rechnen(e with
            {
                KlasseBrauchwasser = true,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 5, ZirkulationKwhD = 0 }
            });
            Assert.Equal(3, k.Katalogvorschlag.Id);
        }

        [Fact]
        public void Prozesszone_aus_der_Prozessreihe()
        {
            double[] q = Enumerable.Range(0, 24 * 14).Select(i => i % 24 >= 8 && i % 24 < 16 ? 50.0 : 0.0).ToArray();
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseProzess = true, Vorlage = PufferVorlage.PROZESS, VorlaufC = 90, RuecklaufC = 70,
                Erzeuger = new PufferErzeuger { NennleistungKw = 30 }, ReiheProzess = q
            });
            PufferZonenergebnis p = r.Zone(PufferZone.Prozess);
            // Durchlauf: 8 h · (50 − 30) kW = 160 kWh nutzbar -> 160 000 / (1,16 · 20 K · 0,85).
            Assert.True(Math.Abs(p.Kriterium("D1").VolumenL.Value - 160000 / (1.16 * 20 * 0.85)) <= 2);
            Assert.NotNull(p.Kriterium("D2"));
            Assert.True(p.VolumenL >= p.Kriterium("D1").VolumenL.Value);
            Assert.True(PufferAuslegung.Rechnen(new PufferAuslegungEingang
            {
                KlasseProzess = true, Vorlage = PufferVorlage.PROZESS, VorlaufC = 90, RuecklaufC = 70
            }).HatWarnung(PufferWarncode.KEINE_REIHE));
        }

        [Fact]
        public void Unzulaessiger_Eingang_wird_benannt_abgelehnt()
        {
            Assert.Throws<ArgumentException>(() => PufferAuslegung.Rechnen(Waermepumpe() with { VorlaufC = 40, RuecklaufC = 40 }));
            Assert.Throws<ArgumentException>(() => PufferAuslegung.Rechnen(Waermepumpe() with { SchwelleEin = 0.9, SchwelleAus = 0.5 }));
            Assert.Throws<ArgumentNullException>(() => PufferAuslegung.Rechnen(null));
        }

        [Fact]
        public void Warncodes_und_Ressourcenschluessel()
        {
            Assert.Equal(17, PufferWarncode.ALLE.Count);
            Assert.All(PufferWarncode.ALLE, c => Assert.StartsWith("PA-", c, StringComparison.Ordinal));
            Assert.Equal("PA_KEIN_PUFFER",
                         new PufferWarnung(PufferWarncode.KEIN_PUFFER, PufferStufe.Hinweis, "", "", null).Ressourcenschluessel);
        }

        [Fact]
        public void Nutzungsprofil_aus_den_Fakten()
        {
            Assert.Equal(PufferNutzungsprofil.BEHERBERGUNG,
                         Nutzungsprofil.Ableiten(new[] { "Hotel (aus Messung, je Zimmer)" }, true, null).Profil);
            Assert.Equal(PufferNutzungsprofil.PFLEGE, Nutzungsprofil.Ableiten(new[] { "Seniorenheim (abgeleitet)" }, false, null).Profil);
            Assert.Equal(PufferNutzungsprofil.PFLEGE, Nutzungsprofil.Ableiten(new[] { "Krankenhaus (abgeleitet)" }, false, null).Profil);
            Assert.Equal(PufferNutzungsprofil.WOHNEN,
                         Nutzungsprofil.Ableiten(new[] { "Testnutzung A (fiktiv)", "Ein- und Zweifamilienhaus (abgeleitet)" }, false, null).Profil);
            Assert.Equal(PufferNutzungsprofil.WOHNEN, Nutzungsprofil.Ableiten(new[] { "Studentenwohnheim (abgeleitet)" }, false, null).Profil);
            Assert.Equal(PufferNutzungsprofil.GEWERBE, Nutzungsprofil.Ableiten(new[] { "Testnutzung B (fiktiv)" }, true, new[] { "BUERO" }).Profil);
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, Nutzungsprofil.Ableiten(null, false, new[] { "WOHNEN", "SCHULE" }).Profil);
            PufferNutzungsprofilAbleitung v = Nutzungsprofil.Ableiten(null, false, new[] { "SONSTIGE" });
            Assert.Equal(PufferNutzungsprofil.WOHNEN, v.Profil);
            Assert.True(v.Vorgabe);
        }

        [Fact]
        public void Sperrprofile()
        {
            Assert.Empty(PufferSperrprofil.Fenster("KEINE"));
            Assert.Equal(2, PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI").Count);
            Assert.Equal(3, PufferSperrprofil.Fenster("DREI_MAL_ZWEI").Count);
            Assert.Equal(new PufferSperrfenster(22, 4), PufferSperrprofil.Fenster("EIGEN", 22, 4).Single());
            Assert.Throws<ArgumentException>(() => PufferSperrprofil.Fenster("DIMMUNG"));
        }
    }
}
