using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Warnlogik, Schätzhilfen, Dauerlinie und Auslastung der Bilanz</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.1, 4.3, 4.6, 5.3, 5.6; Stufe Z4, Gruppe 1) — ohne Datenbank, mit dem
    /// fiktiven Katalog und Parametersatz aus <see cref="ZapfprofilTestbau"/> (Werte erfunden).
    /// </summary>
    public sealed class ZapfprofilWarnlogikTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Nutzungsart[] Katalog = { Art(1), Art(2, bezug: ZapfBezugsart.Wohneinheiten) };

        private static ZapfprofilErgebnis Rechnen(ProjektStand p, params ZonenStand[] zonen)
            => ZapfprofilRechner.Rechnen(Eingang(p, Parameter(), zonen), Katalog);

        /// <summary>Eine Zone mit f_θ = 1: 10 Personen · 2 kWh · 365 = 7300 kWh/a.</summary>
        private static ZonenStand ZoneFlach(string name = "Zone A", double menge = 10.0, int id = 1, int art = 1)
            => Zone(name, art, menge, id) with { KaltwasserMittelC = 12.0 };

        // =================================================================================
        // Dauerlinie und Auslastung
        // =================================================================================

        [Fact]
        public void Die_Dauerlinie_ordnet_die_Stunden_und_setzt_die_Perzentilmarken()
        {
            var zapf = new double[8760];
            var zirk = new double[8760];
            for (int h = 0; h < 8760; h++) { zapf[h] = h % 100; zirk[h] = 0.5; }
            Zapfdauerlinie d = Zapfauswertung.Dauerlinie(new Bilanzreihe(zapf), new Bilanzreihe(zirk), 90.0);

            Assert.Equal(8760, d.GesamtKw.Count);
            Assert.Equal(99.5, d.GesamtKw[0]);
            Assert.Equal(0.5, d.GesamtKw[8759]);
            for (int i = 1; i < 8760; i++) Assert.True(d.GesamtKw[i] <= d.GesamtKw[i - 1]);
            Assert.Equal(zapf.Sum() + zirk.Sum(), d.GesamtKw.Sum(), 6);

            Assert.Equal(new[] { 50, 90, 95, 99 }, d.Marken.Select(m => m.Perzentil).ToArray());
            double[] auf = zapf.Select((z, i) => z + zirk[i]).OrderBy(x => x).ToArray();
            foreach (Dauerlinienmarke m in d.Marken)
            {
                int rang = (int)Math.Ceiling(m.Perzentil / 100.0 * 8760);
                Assert.Equal(auf[rang - 1], m.LeistungKw);
                Assert.Equal(8760 - rang + 1, m.Rang);
                Assert.Equal(m.LeistungKw, d.GesamtKw[m.Rang - 1]);
            }
            // Über 90 kW liegen die Stunden mit h % 100 ≥ 90 (90,5 … 99,5 kW): 10 von 100.
            Assert.Equal(8760 / 100 * 10 + (8760 % 100 > 90 ? 8760 % 100 - 90 : 0), d.StundenUeberSchwelle);
            Assert.Null(Zapfauswertung.Dauerlinie(new Bilanzreihe(zapf), null, null).StundenUeberSchwelle);
        }

        [Fact]
        public void Die_Auslastung_bezieht_Monat_Wochentag_und_Stunde_auf_das_Jahresmittel()
        {
            // Je Tag 24 kWh, im Januar doppelt; immer von 6 bis 18 Uhr gleich verteilt.
            var s = new double[8760];
            for (int d = 1; d <= 365; d++)
                for (int h = 6; h < 18; h++) s[(d - 1) * 24 + h] = d <= 31 ? 4.0 : 2.0;
            Zapfauslastung a = Zapfauswertung.Auslastung(new Bilanzreihe(s), 0);

            double mittel = (31 * 48.0 + 334 * 24.0) / 365.0;
            Assert.Equal(48.0 / mittel, a.Monate[0], 12);
            Assert.Equal(24.0 / mittel, a.Monate[5], 12);
            Assert.Equal(1.0, a.Monate.Select((m, i) => m * Zapfkalender.TageJeMonat[i]).Sum() / 365.0, 12);
            Assert.Equal(1.0, a.Wochentage.Average(), 2);
            Assert.Equal(0.0, a.Stunden[3]);
            Assert.Equal(2.0, a.Stunden[12], 12);
            Assert.Equal(1.0, a.Stunden.Average(), 12);

            Zapfauslastung leer = Zapfauswertung.Auslastung(Bilanzreihe.Null(), 0);
            Assert.All(leer.Monate, m => Assert.Equal(0.0, m));
        }

        [Fact]
        public void Der_Auslastungsgang_nennt_wirksame_Katalog_und_Zonenwerte()
        {
            var monate = new double?[12];
            monate[0] = 2.0;
            ZonenStand z = ZoneFlach() with { Auslastung = monate };
            Auslastungsgang g = Formvektor.Auslastungsgang(z, Katalog[0]);
            Assert.Equal(2.0, g.Wirksam[0]);
            Assert.Equal(1.0, g.Katalog[0]);
            Assert.Equal(1.0, g.Wirksam[1]);
            Assert.True(g.JeMonatUeberschrieben[0]);
            Assert.False(g.JeMonatUeberschrieben[1]);
            Assert.True(g.Ueberschrieben);
            Assert.False(Formvektor.Auslastungsgang(ZoneFlach(), Katalog[0]).Ueberschrieben);

            monate[1] = -1.0;
            Assert.Throws<ZapfprofilEingabeException>(() => Formvektor.Auslastungsgang(z with { Auslastung = monate }, Katalog[0]));

            // Die Rechnung trägt beides je Zone.
            ZapfprofilErgebnis e = Rechnen(Projekt(), ZoneFlach() with { Auslastung = new double?[12] });
            Assert.NotNull(e.JeZone[0].Auslastung);
            Assert.NotNull(e.JeZone[0].Auslastungsgang);
            Assert.NotNull(e.Dauerlinie);
            Assert.Equal(e.Kennzahlen.GroessterStundenwertKw, e.Dauerlinie.GesamtKw[0], 9);
        }

        // =================================================================================
        // Schätzhilfen
        // =================================================================================

        [Fact]
        public void Die_Schaetzhilfe_Tagesbedarf_nennt_Vorschlag_Manuell_und_Rechenweg()
        {
            ZapfprofilErgebnis auto = Rechnen(Projekt(), ZoneFlach());
            Schaetzhilfe a = auto.JeZone[0].SchaetzhilfeTagesbedarf;
            Assert.Equal(Schaetzhilfe.TAGESBEDARF, a.Art);
            Assert.True(a.Auto);
            Assert.Equal(20.0, a.Vorschlag, 9);
            Assert.Equal(a.Vorschlag, a.Angesetzt);
            Assert.Equal("kWh/d", a.Einheit);
            Assert.Equal("SCHAETZ_TAGESBEDARF", a.Rechenweg.Kennung);
            Assert.StartsWith("10 P × 2 kWh/(P·d) × f_θ 1 = 20 kWh/d", a.Rechenweg.Klartext);

            ZapfprofilErgebnis manuell = Rechnen(Projekt(), ZoneFlach() with { TagesbedarfAuto = false, TagesbedarfManuellKwh = 25.0 });
            Schaetzhilfe m = manuell.JeZone[0].SchaetzhilfeTagesbedarf;
            Assert.False(m.Auto);
            Assert.True(m.IstManuell);
            Assert.Equal(20.0, m.Vorschlag, 9);
            Assert.Equal(25.0, m.Angesetzt);
            Assert.Contains("(manuell)", m.Rechenweg.Klartext);
            Assert.Equal(25.0 * 365, manuell.JeZone[0].JahresbedarfZapfungKwh, 6);

            Schaetzhilfe ohne = Schaetzhilfe.Tagesbedarf(false, 5.0, null, ZapfBezugsart.Personen);
            Assert.False(ohne.HatVorschlag);
            Assert.Equal("SCHAETZ_TAGESBEDARF_OHNE_VORSCHLAG", ohne.Rechenweg.Kennung);
        }

        [Fact]
        public void Die_Schaetzhilfe_Zirkulation_rechnet_den_Vorschlag_auch_bei_manuell()
        {
            ProjektStand leitung = Projekt() with { ZirkMethode = ZapfZirkulationsmethode.Leitungslaenge, ZirkLaengeM = 100.0 };
            Schaetzhilfe auto = Rechnen(leitung, ZoneFlach()).SchaetzhilfeZirkulation;
            // α 1 · 100 m · 10 W/m ÷ 1000 = 1 kW (erfunden).
            Assert.Equal(1.0, auto.Vorschlag, 12);
            Assert.Equal(1.0, auto.Angesetzt, 12);
            Assert.Equal("SCHAETZ_ZIRKULATION", auto.Rechenweg.Kennung);
            ZapfSatz methode = Assert.IsType<ZapfSatz>(auto.Rechenweg.Werte[0]);
            Assert.Equal("SCHAETZ_ZIRK_LEITUNG", methode.Kennung);
            Assert.StartsWith("α 1 × 100 m × 10 W/m ÷ 1000 = 1 kW; × 18 h/d × 365 d = 6570 kWh/a", auto.Rechenweg.Klartext);

            Schaetzhilfe manuell = Rechnen(leitung with { ZirkAuto = false, ZirkManuellKw = 0.5 }, ZoneFlach()).SchaetzhilfeZirkulation;
            Assert.Equal(1.0, manuell.Vorschlag, 12);
            Assert.Equal(0.5, manuell.Angesetzt);
            Assert.True(manuell.IstManuell);

            // Die Methode ist ohne Länge nicht rechenbar: kein Vorschlag, der manuelle Wert gilt.
            Schaetzhilfe ohne = Rechnen(leitung with { ZirkLaengeM = null, ZirkAuto = false, ZirkManuellKw = 0.5 }, ZoneFlach())
                .SchaetzhilfeZirkulation;
            Assert.False(ohne.HatVorschlag);
            Assert.Equal("SCHAETZ_ZIRKULATION_OHNE_VORSCHLAG", ohne.Rechenweg.Kennung);
        }

        // =================================================================================
        // Warnlogik
        // =================================================================================

        /// <summary>
        /// Lehre 3 des Mockups: Eine Zirkulation, die mehr verliert, als gezapft wird, ist in kleinen
        /// Mehrfamilienhäusern üblich — ein HINWEIS erst über dem Verhältnis des Katalogs
        /// (<see cref="ZapfParameter.ZIRKULATION_HINWEISVERHAELTNIS"/>), keine Warnung; ohne Parameter
        /// oder mit ungültigem Wert keiner.
        /// </summary>
        [Fact]
        public void Eine_grosse_Zirkulation_ist_ein_Hinweis_ueber_dem_Verhaeltnis_des_Katalogs()
        {
            static ZapfprofilErgebnis Mit(double kw, double? verhaeltnis)
                => ZapfprofilRechner.Rechnen(Eingang(Projekt() with { ZirkAuto = false, ZirkManuellKw = kw },
                    verhaeltnis.HasValue
                        ? Parameter(new Dictionary<string, double> { [ZapfParameter.ZIRKULATION_HINWEISVERHAELTNIS] = verhaeltnis.Value })
                        : Parameter(), ZoneFlach()), Katalog);

            // 7300 kWh/a Zapfung, manuell 2 kW · 18 h · 365 = 13 140 kWh/a Zirkulation: das 1,8-Fache.
            ZapfHinweis h = Assert.Single(Mit(2.0, 1.5).Hinweise, x => x.Code == ZapfprofilRechner.HINWEIS_ZIRKULATION_GROSS);
            Assert.False(h.Warnung);
            Assert.Equal(new object[] { 13140.0, 1.8, 7300.0, 1.5 }, h.Satz.Werte.Select(w => (object)Math.Round((double)w, 6)).ToArray());
            Assert.StartsWith("Die Zirkulation verliert im Jahr 13140 kWh, das 1.8-Fache der Zapfung (7300 kWh/a)", h.Text);

            // Größer als die Zapfung, aber unter dem Verhältnis: kein Hinweis (1,2 kW ergibt das 1,08-Fache).
            Assert.DoesNotContain(Mit(1.2, 1.5).Hinweise, x => x.Code == ZapfprofilRechner.HINWEIS_ZIRKULATION_GROSS);
            // Ohne Parameter oder mit einem Wert, der nicht positiv ist: kein Hinweis, auch keiner zum Parameter.
            Assert.DoesNotContain(Mit(2.0, null).Hinweise, x => x.Code == ZapfprofilRechner.HINWEIS_ZIRKULATION_GROSS);
            Assert.DoesNotContain(Mit(2.0, 0.0).Hinweise, x => x.Code == ZapfprofilRechner.HINWEIS_ZIRKULATION_GROSS);
            Assert.DoesNotContain(Mit(2.0, null).Hinweise, x => x.Code == ZapfHinweis.PARAMETER_FEHLT);
        }

        [Fact]
        public void Eine_abweichende_Bezugsmenge_neben_der_Wohnungstabelle_wird_genannt()
        {
            ZonenStand z = ZoneFlach("Wohnen", 7.0, 1, 2) with { Wohnungen = new[] { new WohnungstypStand { Anzahl = 5 } } };
            ZapfprofilErgebnis e = Rechnen(Projekt(), z);
            ZapfHinweis h = Assert.Single(e.Hinweise, x => x.Code == Mengengeruest.HINWEIS_WOHNUNGSTABELLE);
            Assert.False(h.Warnung);
            Assert.Equal("Wohnen", h.Zone);
            Assert.Equal(5.0, e.JeZone[0].Bezugsmenge);

            ZapfprofilErgebnis gleich = Rechnen(Projekt(), z with { Bezugsmenge = 5.0 });
            Assert.DoesNotContain(gleich.Hinweise, x => x.Code == Mengengeruest.HINWEIS_WOHNUNGSTABELLE);
        }

        [Fact]
        public void Ein_manueller_Tagesbedarf_ausserhalb_der_Bandbreite_ist_eine_Warnung()
        {
            var band = new Bedarfsbandbreite(new double?[] { null, 1.5, null }, new double?[] { null, 2.5, null });
            var katalog = new[] { Art(1, bandbreite: band) };
            ZonenStand z = ZoneFlach() with { TagesbedarfAuto = false, TagesbedarfManuellKwh = 40.0 };
            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(Eingang(Projekt(), Parameter(), z), katalog);
            ZapfHinweis h = Assert.Single(e.Hinweise, x => x.Code == Mengengeruest.HINWEIS_BANDBREITE);
            Assert.True(h.Warnung);
            Assert.Equal(4.0, (double)h.Satz.Werte[1], 12);

            ZapfprofilErgebnis drin = ZapfprofilRechner.Rechnen(
                Eingang(Projekt(), Parameter(), z with { TagesbedarfManuellKwh = 20.0 }), katalog);
            Assert.DoesNotContain(drin.Hinweise, x => x.Code == Mengengeruest.HINWEIS_BANDBREITE);
        }

        [Fact]
        public void ZU5_ist_eine_Warnung_und_Hinweise_des_Eingangs_stehen_vorn()
        {
            ZapfHinweis vorab = new ZapfHinweis("", "EINSTELLUNG_UNGUELTIG", ZapfSatz.Neu("HINWEIS_EINSTELLUNG_UNGUELTIG", "x", "y"));
            Zapfprofileingang e = Eingang(Projekt() with { ZirkFlaecheM2 = 200.0 }, Parameter(), ZoneFlach())
                                  with { NetzverlusteProjekt = 5.0, Vorhinweise = new[] { vorab } };
            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(e, Katalog);
            Assert.Equal(vorab, r.Hinweise[0]);
            Assert.True(Assert.Single(r.Hinweise, x => x.Code == ZapfprofilRechner.HINWEIS_NETZVERLUST).Warnung);
        }
    }
}
