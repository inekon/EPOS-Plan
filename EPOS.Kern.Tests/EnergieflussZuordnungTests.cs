using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Darstellung „Zuordnung“ des Energieflussbilds</b> (Anwenderentscheid vom 17.10.2026) an synthetischen
    /// Ergebnisbäumen: Bänder Erzeuger → Verwendung aus der Deckung je Kanal, Speicherverlust als Band am Erzeuger, Netto der
    /// Wärmepumpe ohne die Quellentnahme der Booster-Kette, Heizstab an der Wärmepumpe, ungedeckt wie in der Kette,
    /// Eingangs- und Stromseite unverändert, jeder geprüfte Knoten geschlossen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EnergieflussZuordnungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static ErgebnisModel Baum(double heizung, double warmwasser, double netz, double waermerest = 0.0)
        {
            var e = new ErgebnisModel { Energiebedarf = new ErgebnisEnergiebedarfModel() };
            e.Energiebedarf.Stromrestbedarf = netz;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.HEIZUNG] = heizung;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.BRAUCHWASSER] = warmwasser;
            e.Energiebedarf.Waermebedarf_Gesamt = heizung + warmwasser;
            e.Energiebedarf.Waermerestbedarf = waermerest;
            return e;
        }

        /// <summary>Deckung je Kanal in % des Projektwärmebedarfs aus MWh (die Schreibweise des Runners umgekehrt).</summary>
        private static double[] Deckung(double basis, double heizungMwh, double warmwasserMwh)
        {
            var d = new double[Kanal.ANZAHL];
            d[Kanal.HEIZUNG] = heizungMwh / basis * 100.0;
            d[Kanal.BRAUCHWASSER] = warmwasserMwh / basis * 100.0;
            return d;
        }

        /// <summary>
        /// Wärmepumpe mit Heizstab deckt Heizung und Warmwasser, der Kessel nur Heizung; Puffer A ist die Quelle des
        /// Boosters (19 MWh Quellentnahme, 1 MWh Verlust), Kombispeicher B lädt 27, gibt 26 an das Warmwasser, 1 MWh Verlust.
        /// </summary>
        private static ErgebnisModel Booster()
        {
            ErgebnisModel e = Baum(heizung: 50.0, warmwasser: 30.0, netz: 30.0);
            e.Waermepumpe = new ErgebnisWaermepumpeModel { Waermeproduktion_WP = 95.0, Stromverbrauch_WP = 28.0, Stromverbrauch_Heizstab = 2.0 };
            e.Waermepumpe.Deckung_Kanal = Deckung(80.0, 46.0, 30.0);
            e.Heizkessel = new ErgebnisHeizkesselModel();
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Gas", CarrierId = 7, Verbrauch = 4.4, Waermeproduktion = 4.0 });
            e.Heizkessel.Deckung_Kanal = Deckung(80.0, 4.0, 0.0);
            var a = new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 1, Bezeichner = "Puffer A", Verwendung = DbWerte.PSP_VERWENDUNG_HEIZUNG,
                Ladung_gesamt = 20000.0, Entladung_gesamt = 19000.0, Verluste_gesamt = 1000.0,
            };
            var b = new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 2, Bezeichner = "Kombi B", Verwendung = DbWerte.PSP_VERWENDUNG_BRAUCHWASSER,
                Ladung_gesamt = 27000.0, Entladung_gesamt = 26000.0, Verluste_gesamt = 1000.0,
            };
            b.Entladung_Kanal[Kanal.BRAUCHWASSER] = 26000.0;
            e.Pufferspeicher.Add(a);
            e.Pufferspeicher.Add(b);
            return e;
        }

        private static EnergieflussBilanz Kette(ErgebnisModel e) => Energiefluss.Aus(e, null, false, "Stamm", 4711);

        private static EnergieflussBilanz Zuordnung(ErgebnisModel e, bool englisch = false)
            => Energiefluss.Aus(e, null, englisch, "Stamm", 4711, EnergieflussDarstellung.Zuordnung);

        private static void AlleKnotenGeschlossen(EnergieflussBilanz m)
        {
            foreach (EnergieflussKnoten k in m.Knoten.Where(k => k.Geprueft))
                Assert.True(Math.Abs(k.ZuflussMwh - k.AbflussMwh) <= Energiefluss.Toleranz(k.ZuflussMwh),
                            k.Id + ": Zufluss " + k.ZuflussMwh + " Abfluss " + k.AbflussMwh);
            Assert.Null(m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET));
            Assert.Null(m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET_QUELLE));
            Assert.Empty(m.Hinweise);
        }

        private static bool Waermeseite(string id)
            => id == Energiefluss.WAERME || id.StartsWith(Energiefluss.PUFFER, StringComparison.Ordinal)
               || id.StartsWith(Energiefluss.VERLUST + Energiefluss.PUFFER, StringComparison.Ordinal)
               || id.StartsWith(Energiefluss.FUELLSTAND + Energiefluss.PUFFER, StringComparison.Ordinal);

        /// <summary>Jedes Band der Kette abseits von Wärmebus und Wärmespeicher steht unverändert in der Zuordnung.</summary>
        private static void EingangUndStromWieInDerKette(EnergieflussBilanz kette, EnergieflussBilanz zuordnung)
        {
            foreach (EnergieflussFluss f in kette.Fluesse.Where(f => !Waermeseite(f.Von) && !Waermeseite(f.Nach)))
                Assert.Equal(f.Mwh, zuordnung.Fluss(f.Von, f.Nach), 9);
        }

        [Fact]
        public void Booster_Waermepumpe_und_Kessel_decken_direkt_mit_Speicherverlust_am_Erzeuger()
        {
            ErgebnisModel e = Booster();
            EnergieflussBilanz kette = Kette(e), m = Zuordnung(e);

            Assert.Equal(EnergieflussDarstellung.Zuordnung, m.Darstellung);
            Assert.Equal(EnergieflussDarstellung.Kette, kette.Darstellung);
            // Kein Wärmebus, kein Wärmespeicher.
            Assert.DoesNotContain(m.Knoten, k => Waermeseite(k.Id));
            // Bänder Erzeuger → Verwendung = Deckung je Kanal / 100 · Wärmebedarf.
            Assert.Equal(46.0, m.Fluss(Energiefluss.WP_HEIZEN, Energiefluss.HEIZUNG), 9);
            Assert.Equal(30.0, m.Fluss(Energiefluss.WP_HEIZEN, Energiefluss.WARMWASSER), 9);
            Assert.Equal(4.0, m.Fluss(Energiefluss.HEIZKESSEL, Energiefluss.HEIZUNG), 9);
            Assert.Equal(0.0, m.Fluss(Energiefluss.HEIZKESSEL, Energiefluss.WARMWASSER), 9);
            Assert.Equal(EnergieflussHerkunft.Abgeleitet, m.FlussMit(Energiefluss.WP_HEIZEN, Energiefluss.HEIZUNG).Herkunft);
            // Der Heizstab gibt seine Wärme an die Wärmepumpe, deren Deckung ihn einschließt.
            Assert.Equal(2.0, m.Fluss(Energiefluss.HEIZSTAB, Energiefluss.WP_HEIZEN), 9);
            // Netto der Wärmepumpe = 95 − 19 Quellentnahme + 2 Heizstab = 78; gedeckt 76 → 2 MWh Speicherverlust (A und B je 1).
            EnergieflussFluss v = m.FlussMit(Energiefluss.WP_HEIZEN, Energiefluss.SPEICHERVERLUST + Energiefluss.WP_HEIZEN);
            Assert.NotNull(v);
            Assert.Equal(2.0, v.Mwh, 9);
            Assert.Equal(EnergieflussForm.Verlust, v.Form);
            Assert.Equal(EnergieflussKnotenart.Verlust, m.KnotenMit(Energiefluss.SPEICHERVERLUST + Energiefluss.WP_HEIZEN).Art);
            Assert.Equal("Speicherverlust", m.KnotenMit(Energiefluss.SPEICHERVERLUST + Energiefluss.WP_HEIZEN).Name);
            // Der Kessel deckt alles, was er abgibt: kein Speicherverlust.
            Assert.Null(m.KnotenMit(Energiefluss.SPEICHERVERLUST + Energiefluss.HEIZKESSEL));
            // Die Quellwärme erscheint nicht doppelt: Umweltwärme wie in der Kette (76 − 28 = 48).
            Assert.Equal(48.0, m.Fluss(Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN), 9);
            Assert.Equal(kette.Fluss(Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN), m.Fluss(Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN), 9);
            // Die Verwendungen bekommen ihren Bedarf.
            Assert.Equal(50.0, m.KnotenMit(Energiefluss.HEIZUNG).ZuflussMwh, 9);
            Assert.Equal(30.0, m.KnotenMit(Energiefluss.WARMWASSER).ZuflussMwh, 9);
            EingangUndStromWieInDerKette(kette, m);
            AlleKnotenGeschlossen(m);
            AlleKnotenGeschlossen(kette);
        }

        [Fact]
        public void Bhkw_und_Solarthermie_decken_direkt_und_ungedeckt_bleibt_wie_in_der_Kette()
        {
            ErgebnisModel e = Baum(heizung: 25.0, warmwasser: 5.0, netz: 0.0, waermerest: 2.0);
            e.Energiebedarf.Strombedarf_Gesamt = 12.0;
            e.BHKW = new ErgebnisBHKWModel { Stromproduktion = 12.0, Waermeproduktion = 23.0, StromeinspeisungMwh = 0.0 };
            e.BHKW.Module.Add(new ErgebnisBHKWModulModel { Brennstoff = "Gas", CarrierId = 7, Verbrauch = 40.0 });
            double heizUngedeckt = 2.0 * 25.0 / 30.0, wwUngedeckt = 2.0 * 5.0 / 30.0;
            e.BHKW.Deckung_Kanal = Deckung(30.0, 25.0 - heizUngedeckt - 4.0, 5.0 - wwUngedeckt - 1.0);
            e.Solarthermie = new ErgebnisSolarthermieModel { Waermeproduktion = 5.0 };
            e.Solarthermie.Deckung_Kanal = Deckung(30.0, 4.0, 1.0);

            EnergieflussBilanz kette = Kette(e), m = Zuordnung(e);

            Assert.Equal(25.0 - heizUngedeckt - 4.0, m.Fluss(Energiefluss.BHKW, Energiefluss.HEIZUNG), 9);
            Assert.Equal(5.0 - wwUngedeckt - 1.0, m.Fluss(Energiefluss.BHKW, Energiefluss.WARMWASSER), 9);
            Assert.Equal(4.0, m.Fluss(Energiefluss.SOLARTHERMIE, Energiefluss.HEIZUNG), 9);
            Assert.Equal(1.0, m.Fluss(Energiefluss.SOLARTHERMIE, Energiefluss.WARMWASSER), 9);
            Assert.Equal(heizUngedeckt, m.Fluss(Energiefluss.UNGEDECKT, Energiefluss.HEIZUNG), 9);
            Assert.Equal(wwUngedeckt, m.Fluss(Energiefluss.UNGEDECKT, Energiefluss.WARMWASSER), 9);
            Assert.Equal(25.0, m.KnotenMit(Energiefluss.HEIZUNG).ZuflussMwh, 9);
            Assert.Equal(5.0, m.KnotenMit(Energiefluss.WARMWASSER).ZuflussMwh, 9);
            // Ohne Speicher deckt jeder Erzeuger alles, was er abgibt; der Wandlerverlust des BHKW bleibt (40 − 12 − 23).
            Assert.DoesNotContain(m.Knoten, k => k.Id.StartsWith(Energiefluss.SPEICHERVERLUST, StringComparison.Ordinal));
            Assert.Equal(5.0, m.Fluss(Energiefluss.BHKW, Energiefluss.VERLUST + Energiefluss.BHKW), 9);
            Assert.Null(m.KnotenMit(Energiefluss.WAERME));
            EingangUndStromWieInDerKette(kette, m);
            AlleKnotenGeschlossen(m);
        }

        [Fact]
        public void Brennstoff_und_Elektrokessel_teilen_die_Deckung_des_Heizkessels_nach_ihrer_Waermeabgabe()
        {
            ErgebnisModel e = Baum(heizung: 40.0, warmwasser: 0.0, netz: 10.0);
            e.Heizkessel = new ErgebnisHeizkesselModel { Stromverbrauch = 10.0 };
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Gas", CarrierId = 7, Verbrauch = 33.0, Waermeproduktion = 30.0 });
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Strom", Waermeproduktion = 10.0 });
            e.Heizkessel.Deckung_Kanal = Deckung(40.0, 40.0, 0.0);

            EnergieflussBilanz m = Zuordnung(e);

            Assert.Equal(30.0, m.Fluss(Energiefluss.HEIZKESSEL, Energiefluss.HEIZUNG), 9);
            Assert.Equal(10.0, m.Fluss(Energiefluss.ELEKTROKESSEL, Energiefluss.HEIZUNG), 9);
            EingangUndStromWieInDerKette(Kette(e), m);
            AlleKnotenGeschlossen(m);
        }

        [Fact]
        public void Ein_Erzeuger_der_mehr_deckt_als_er_abgibt_schliesst_nicht_und_wird_benannt()
        {
            ErgebnisModel e = Booster();
            e.Heizkessel.Deckung_Kanal = Deckung(80.0, 10.0, 0.0);   // 10 MWh gedeckt, 4 abgegeben

            EnergieflussBilanz m = Zuordnung(e);

            Assert.False(m.KnotenMit(Energiefluss.HEIZKESSEL).Geschlossen);
            Assert.Equal(6.0, m.Fluss(Energiefluss.NICHT_ZUGEORDNET_QUELLE, Energiefluss.HEIZKESSEL), 9);
            Assert.NotEmpty(m.Hinweise);
        }

        [Fact]
        public void Bild_der_Zuordnung_zeichnet_wie_die_Kette_ohne_Stufentitel_und_ohne_Waermebus()
        {
            EnergieflussBilanz m = Zuordnung(Booster());
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(m);
            Assert.DoesNotContain(d.Knoten, k => k.Kennung == Energiefluss.WAERME);
            Assert.Contains(d.Baender, b => b.Von == Energiefluss.WP_HEIZEN && b.Nach == Energiefluss.WARMWASSER);
            Assert.Contains(d.Baender, b => b.Von == Energiefluss.WP_HEIZEN && b.Nach == Energiefluss.SPEICHERVERLUST + Energiefluss.WP_HEIZEN
                                            && b.Art == EnergieflussBandart.Verlust);

            Zeichenmodell z = ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_BREIT, false, EnergieflussDarstellung.Zuordnung);
            Assert.DoesNotContain(z.Befehle, b => b.Marke != null && b.Marke.StartsWith("stufe:", StringComparison.Ordinal));
            Assert.Contains(z.Befehle, b => b.Marke == "fluss:" + Energiefluss.WP_HEIZEN + ":" + Energiefluss.HEIZUNG);
            Assert.True(z.Gleicht(ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_BREIT, false, EnergieflussDarstellung.Kette)));
        }

        [Fact]
        public void Darstellungstext_und_englische_Namen_der_Zuordnung()
        {
            Assert.Equal("Zuordnung", EnergieflussAbbildung.Darstellungstext(EnergieflussDarstellung.Zuordnung, false));
            Assert.Equal("Allocation", EnergieflussAbbildung.Darstellungstext(EnergieflussDarstellung.Zuordnung, true));
            EnergieflussBilanz m = Zuordnung(Booster(), englisch: true);
            Assert.Equal("Storage loss", m.KnotenMit(Energiefluss.SPEICHERVERLUST + Energiefluss.WP_HEIZEN).Name);
        }
    }
}
