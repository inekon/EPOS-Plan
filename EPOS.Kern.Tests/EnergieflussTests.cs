using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Flussbau des Energieflussbilds</b> (<see cref="Energiefluss"/>, Konzept Energieflussbild Abschnitte 1, 2
    /// und 6) an synthetischen Ergebnisbäumen: Knoten und Bänder je Anlagenart, Bilanz und Toleranz, bilanzierte
    /// BHKW-Einspeisung, „nicht zugeordnet“ beidseitig, ungedeckt, Heizstab 1:1, Brennwert mit Kondensationswärme,
    /// Kälte mit Speicher und Split, Speicher mit Verlust- und Füllstandsband, Bündelung, Reihenfolge, englische Namen,
    /// Zeigertexte, KI-Flussliste, leere und nicht simulierte Version.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EnergieflussTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const double EPS = 1e-9;

        private static ErgebnisModel Baum(double strombedarf = 0.0, double netz = 0.0, double heizung = 0.0,
                                          double warmwasser = 0.0, double prozess = 0.0, double waermerest = 0.0)
        {
            var e = new ErgebnisModel { Energiebedarf = new ErgebnisEnergiebedarfModel() };
            e.Energiebedarf.Strombedarf_Gesamt = strombedarf;
            e.Energiebedarf.Stromrestbedarf = netz;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.HEIZUNG] = heizung;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.BRAUCHWASSER] = warmwasser;
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.PROZESS] = prozess;
            e.Energiebedarf.Waermebedarf_Gesamt = heizung + warmwasser + prozess;
            e.Energiebedarf.Waermerestbedarf = waermerest;
            return e;
        }

        private static EnergieflussBilanz Aus(ErgebnisModel e, bool englisch = false, IReadOnlyList<EnergieTraegerNachweis> traeger = null)
            => Energiefluss.Aus(e, traeger, englisch, "Stamm", 4711);

        /// <summary>Jeder geprüfte Knoten schließt: Zufluss = Abfluss (die Ausgleichsbänder eingerechnet).</summary>
        private static void AlleKnotenGeschlossen(EnergieflussBilanz m)
        {
            foreach (EnergieflussKnoten k in m.Knoten.Where(k => k.Geprueft))
                Assert.True(Math.Abs(k.ZuflussMwh - k.AbflussMwh) <= Energiefluss.Toleranz(k.ZuflussMwh),
                            k.Id + ": Zufluss " + k.ZuflussMwh + " Abfluss " + k.AbflussMwh);
        }

        private static ErgebnisModel WaermepumpeMitPuffer(double socEndeKwh)
        {
            ErgebnisModel e = Baum(strombedarf: 20.0, netz: 40.0, heizung: 50.0, warmwasser: 10.0);
            e.Waermepumpe = new ErgebnisWaermepumpeModel { Waermeproduktion_WP = 46.0, Stromverbrauch_WP = 15.0, Stromverbrauch_Heizstab = 5.0 };
            e.Heizkessel = new ErgebnisHeizkesselModel();
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Gas", CarrierId = 7, Verbrauch = 12.0, Waermeproduktion = 11.0 });
            // Puffer: Ladung 30 MWh, Entladung 28 (25 Heizung, 3 Warmwasser), Verlust 2 − Füllstand.
            var p = new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 3, Bezeichner = "Puffer 1", Verwendung = DbWerte.PSP_VERWENDUNG_HEIZUNG,
                Ladung_gesamt = 30000.0, Entladung_gesamt = 28000.0, Verluste_gesamt = 2000.0 - socEndeKwh, SOC_Ende = socEndeKwh,
            };
            p.Entladung_Kanal[Kanal.HEIZUNG] = 25000.0;
            p.Entladung_Kanal[Kanal.BRAUCHWASSER] = 3000.0;
            e.Pufferspeicher.Add(p);
            return e;
        }

        // =====================================================================
        //  Leer und nicht simuliert
        // =====================================================================

        [Fact]
        public void Nicht_simulierte_Version_hat_kein_Bild_und_einen_benannten_Grund()
        {
            EnergieflussBilanz m = Energiefluss.Aus((VariantenDaten)null, false, "Variante B");
            Assert.False(m.Simuliert);
            Assert.True(m.Leer);
            Assert.Empty(m.Knoten);
            Assert.Equal("Für die Version „Variante B“ liegt kein Simulationsergebnis vor.", m.Grund);

            var v = new VariantenDaten { IdProjekt = 12, Variantenname = "Variante C" };
            EnergieflussBilanz m2 = Energiefluss.Aus(v, true);
            Assert.False(m2.Simuliert);
            Assert.Equal("There is no simulation result for version “Variante C”.", m2.Grund);
            Assert.Contains("[flow] Energy flow — Variante C", m2.KiText());
        }

        [Fact]
        public void Leere_Version_ist_simuliert_hat_aber_keine_Fluesse()
        {
            EnergieflussBilanz m = Aus(new ErgebnisModel());
            Assert.True(m.Simuliert);
            Assert.True(m.Leer);
            Assert.Empty(m.Knoten);
            Assert.Equal("Die Version „Stamm“ hat keine Energieflüsse.", m.Grund);
        }

        // =====================================================================
        //  Wärmepumpe, Heizstab, Kessel, Puffer
        // =====================================================================

        [Fact]
        public void Waermepumpe_Heizstab_Kessel_und_Puffer_schliessen()
        {
            EnergieflussBilanz m = Aus(WaermepumpeMitPuffer(socEndeKwh: 100.0));

            Assert.Equal(40.0, m.Fluss(Energiefluss.NETZBEZUG, Energiefluss.STROM), 9);
            Assert.Equal(15.0, m.Fluss(Energiefluss.STROM, Energiefluss.WP_HEIZEN), 9);
            Assert.Equal(46.0, m.Fluss(Energiefluss.WP_HEIZEN, Energiefluss.WAERME), 9);
            // Umweltwärme = Wärme − Strom, abgeleitet.
            EnergieflussFluss u = m.FlussMit(Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN);
            Assert.Equal(31.0, u.Mwh, 9);
            Assert.Equal(EnergieflussHerkunft.Abgeleitet, u.Herkunft);
            Assert.Equal(EnergieflussForm.Umwelt, u.Form);
            // Heizstab 1:1.
            Assert.Equal(5.0, m.Fluss(Energiefluss.STROM, Energiefluss.HEIZSTAB), 9);
            Assert.Equal(5.0, m.Fluss(Energiefluss.HEIZSTAB, Energiefluss.WAERME), 9);
            Assert.Null(m.KnotenMit(Energiefluss.VERLUST + Energiefluss.HEIZSTAB));
            // Kessel mit Verlust 1, Träger aus dem Brennstoffwort.
            Assert.Equal(12.0, m.Fluss("traeger:7", Energiefluss.HEIZKESSEL), 9);
            Assert.Equal("Gas", m.KnotenMit("traeger:7").Name);
            Assert.Equal(1.0, m.Fluss(Energiefluss.HEIZKESSEL, Energiefluss.VERLUST + Energiefluss.HEIZKESSEL), 9);
            Assert.Null(m.KnotenMit(Energiefluss.KONDENSATION));
            // Puffer: kWh → MWh an einer Stelle; Ladung, Entladung je Kanal, Verlust samt Füllstand (unter 1 %).
            string puffer = Energiefluss.PUFFER + "3:0";
            Assert.Equal(30.0, m.Fluss(Energiefluss.WAERME, puffer), 9);
            Assert.Equal(25.0, m.Fluss(puffer, Energiefluss.HEIZUNG), 9);
            Assert.Equal(3.0, m.Fluss(puffer, Energiefluss.WARMWASSER), 9);
            Assert.Equal(2.0, m.Fluss(puffer, Energiefluss.VERLUST + puffer), 9);
            Assert.Null(m.KnotenMit(Energiefluss.FUELLSTAND + puffer));
            Assert.Contains("Verlust 1,90 · Füllstandsänderung 0,10 MWh/a", m.KnotenMit(puffer).Zeigertext);
            // Direktdeckung = Bedarf − Entladung.
            Assert.Equal(25.0, m.Fluss(Energiefluss.WAERME, Energiefluss.HEIZUNG), 9);
            Assert.Equal(7.0, m.Fluss(Energiefluss.WAERME, Energiefluss.WARMWASSER), 9);
            Assert.Equal(20.0, m.Fluss(Energiefluss.STROM, Energiefluss.STROMBEDARF), 9);

            AlleKnotenGeschlossen(m);
            Assert.All(m.Knoten.Where(k => k.Geprueft), k => Assert.True(k.Geschlossen, k.Id));
            Assert.Empty(m.Hinweise);
            Assert.Null(m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET));
            // Nur heizend: ein Knoten „Wärmepumpe“.
            Assert.Equal("Wärmepumpe", m.KnotenMit(Energiefluss.WP_HEIZEN).Name);
        }

        [Fact]
        public void Fuellstandsaenderung_ab_einem_Prozent_der_Ladung_bekommt_ein_eigenes_Band()
        {
            EnergieflussBilanz m = Aus(WaermepumpeMitPuffer(socEndeKwh: 600.0));
            string puffer = Energiefluss.PUFFER + "3:0";
            EnergieflussFluss f = m.FlussMit(puffer, Energiefluss.FUELLSTAND + puffer);
            Assert.NotNull(f);
            Assert.Equal(0.6, f.Mwh, 9);
            Assert.Equal(1.4, m.Fluss(puffer, Energiefluss.VERLUST + puffer), 9);
            Assert.Equal(Energiefluss.SPALTE_SPEICHER, m.KnotenMit(Energiefluss.FUELLSTAND + puffer).Spalte);
            AlleKnotenGeschlossen(m);
        }

        [Fact]
        public void Verlustknoten_stehen_in_der_Luecke_neben_ihrem_Wandler()
        {
            EnergieflussBilanz m = Aus(WaermepumpeMitPuffer(100.0));
            EnergieflussKnoten v = m.KnotenMit(Energiefluss.VERLUST + Energiefluss.HEIZKESSEL);
            Assert.Equal(EnergieflussKnotenart.Verlust, v.Art);
            Assert.Equal(Energiefluss.SPALTE_WANDLER_I, v.Spalte);
        }

        // =====================================================================
        //  Brennwert, Elektrokessel
        // =====================================================================

        [Fact]
        public void Brennwertkessel_ueber_hundert_Prozent_bekommt_Kondensationswaerme_und_keinen_Verlust()
        {
            ErgebnisModel e = Baum(heizung: 10.5);
            e.Heizkessel = new ErgebnisHeizkesselModel();
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Gas", CarrierId = 3, Verbrauch = 10.0, Waermeproduktion = 10.5 });
            var traeger = new List<EnergieTraegerNachweis> { new EnergieTraegerNachweis { CarrierId = 3, Traeger = "Erdgas H" } };
            EnergieflussBilanz m = Aus(e, traeger: traeger);
            EnergieflussFluss k = m.FlussMit(Energiefluss.KONDENSATION, Energiefluss.HEIZKESSEL);
            Assert.Equal(0.5, k.Mwh, 9);
            Assert.Equal(EnergieflussHerkunft.Abgeleitet, k.Herkunft);
            Assert.Null(m.KnotenMit(Energiefluss.VERLUST + Energiefluss.HEIZKESSEL));
            Assert.Equal("Erdgas H", m.KnotenMit("traeger:3").Name);
            AlleKnotenGeschlossen(m);
            Assert.Empty(m.Hinweise);
        }

        [Fact]
        public void Elektrokessel_haengt_am_Strom_nicht_an_einem_Brennstoff()
        {
            ErgebnisModel e = Baum(netz: 8.0, heizung: 7.8);
            e.Heizkessel = new ErgebnisHeizkesselModel { Stromverbrauch = 8.0 };
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Strom", Waermeproduktion = 7.8 });
            EnergieflussBilanz m = Aus(e);
            Assert.Equal(8.0, m.Fluss(Energiefluss.STROM, Energiefluss.ELEKTROKESSEL), 9);
            Assert.Equal(7.8, m.Fluss(Energiefluss.ELEKTROKESSEL, Energiefluss.WAERME), 9);
            Assert.Equal(0.2, m.Fluss(Energiefluss.ELEKTROKESSEL, Energiefluss.VERLUST + Energiefluss.ELEKTROKESSEL), 9);
            Assert.Null(m.KnotenMit(Energiefluss.HEIZKESSEL));
            Assert.DoesNotContain(m.Knoten, k => k.Id.StartsWith(Energiefluss.TRAEGER, StringComparison.Ordinal));
            AlleKnotenGeschlossen(m);
        }

        // =====================================================================
        //  BHKW-Einspeisung, nicht zugeordnet, Toleranz
        // =====================================================================

        private static ErgebnisModel Bhkw(double strombedarf, double netz, double bhkwStrom = 10.0)
        {
            ErgebnisModel e = Baum(strombedarf: strombedarf, netz: netz, heizung: 15.0);
            e.BHKW = new ErgebnisBHKWModel { Stromproduktion = bhkwStrom, Waermeproduktion = 15.0 };
            e.BHKW.Module.Add(new ErgebnisBHKWModulModel { Brennstoff = "Gas", CarrierId = 5, Verbrauch = 30.0 });
            return e;
        }

        [Fact]
        public void BHKW_Einspeisung_wird_am_Stromknoten_bilanziert()
        {
            EnergieflussBilanz m = Aus(Bhkw(strombedarf: 6.0, netz: 1.0));
            EnergieflussFluss f = m.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW);
            Assert.Equal(5.0, f.Mwh, 9);
            Assert.Equal(EnergieflussHerkunft.Bilanziert, f.Herkunft);
            Assert.EndsWith("· bilanziert aus der Strombilanz des Laufs", f.Zeigertext);
            Assert.True(m.KnotenMit(Energiefluss.STROM).Geschlossen);
            Assert.Equal(5.0, m.KnotenMit(Energiefluss.STROM).RestMwh, 9);
            Assert.Equal(5.0, m.Fluss(Energiefluss.BHKW, Energiefluss.VERLUST + Energiefluss.BHKW), 9);
            Assert.Empty(m.Hinweise);
            AlleKnotenGeschlossen(m);
        }

        [Fact]
        public void Rest_ueber_dem_BHKW_Strom_ist_nicht_zugeordnet_mit_Hinweis()
        {
            // Rest 15 > BHKW-Strom 10 + Toleranz: kein Einspeiseband, sondern „nicht zugeordnet“.
            EnergieflussBilanz m = Aus(Bhkw(strombedarf: 5.0, netz: 10.0));
            Assert.Null(m.KnotenMit(Energiefluss.EINSPEISUNG_BHKW));
            EnergieflussFluss f = m.FlussMit(Energiefluss.STROM, Energiefluss.NICHT_ZUGEORDNET);
            Assert.Equal(15.0, f.Mwh, 9);
            Assert.Equal(EnergieflussForm.Rest, f.Form);
            Assert.False(m.KnotenMit(Energiefluss.STROM).Geschlossen);
            Assert.Equal("Bilanz am Knoten Strom schließt nicht: 15,0 MWh/a nicht zugeordnet", Assert.Single(m.Hinweise));
            Assert.Contains("Bilanz am Knoten Strom schließt nicht", m.KiText());
            AlleKnotenGeschlossen(m);
        }

        [Fact]
        public void Negativer_Rest_kommt_aus_der_Quellenspalte()
        {
            // Abfluss 12 bei Zufluss 10: 2 MWh fehlen auf der Zuflussseite.
            EnergieflussBilanz m = Aus(Baum(strombedarf: 12.0, netz: 10.0));
            EnergieflussFluss f = m.FlussMit(Energiefluss.NICHT_ZUGEORDNET_QUELLE, Energiefluss.STROM);
            Assert.Equal(2.0, f.Mwh, 9);
            Assert.Equal(EnergieflussKnotenart.Quelle, m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET_QUELLE).Art);
            Assert.Equal(-2.0, m.KnotenMit(Energiefluss.STROM).RestMwh, 9);
            Assert.Single(m.Hinweise);
        }

        [Theory]
        [InlineData(10.0, 10.04, true)]    // 0,04 ≤ 0,05 absolut
        [InlineData(10.0, 10.06, false)]   // 0,06 > 0,05
        [InlineData(100.0, 100.4, true)]   // 0,4 ≤ 0,5 % von 100
        [InlineData(100.0, 100.6, false)]  // 0,6 > 0,5 %
        public void Toleranz_ist_das_Groessere_aus_fuenf_Hundertstel_und_einem_halben_Prozent(double netz, double bedarf, bool geschlossen)
        {
            EnergieflussBilanz m = Aus(Baum(strombedarf: bedarf, netz: netz));
            Assert.Equal(geschlossen, m.KnotenMit(Energiefluss.STROM).Geschlossen);
            Assert.Equal(geschlossen, m.Hinweise.Count == 0);
            Assert.Equal(geschlossen, m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET_QUELLE) == null);
        }

        [Fact]
        public void Toleranzformel()
        {
            Assert.Equal(0.05, Energiefluss.Toleranz(1.0), 12);
            Assert.Equal(0.5, Energiefluss.Toleranz(100.0), 12);
        }

        // =====================================================================
        //  Ungedeckt
        // =====================================================================

        [Fact]
        public void Ungedeckter_Waermebedarf_ist_ein_Band_aus_der_Quellenspalte_anteilig_nach_Bedarf()
        {
            ErgebnisModel e = Baum(heizung: 30.0, warmwasser: 10.0, waermerest: 4.0);
            e.Solarthermie = new ErgebnisSolarthermieModel { Waermeproduktion = 36.0, Ueberschuss = 21.0 };
            EnergieflussBilanz m = Aus(e);
            Assert.Equal(3.0, m.Fluss(Energiefluss.UNGEDECKT, Energiefluss.HEIZUNG), 9);
            Assert.Equal(1.0, m.Fluss(Energiefluss.UNGEDECKT, Energiefluss.WARMWASSER), 9);
            Assert.Equal(27.0, m.Fluss(Energiefluss.WAERME, Energiefluss.HEIZUNG), 9);
            Assert.Equal(EnergieflussKnotenart.Quelle, m.KnotenMit(Energiefluss.UNGEDECKT).Art);
            Assert.Equal(30.0, m.KnotenMit(Energiefluss.HEIZUNG).ZuflussMwh, 9);
            // SK-Q3: Der Solarüberschuss ist kein Band, nur Zeigertext.
            Assert.Equal(36.0, m.KnotenMit(Energiefluss.SOLARTHERMIE).AbflussMwh, 9);
            Assert.Contains("nicht nutzbarer Überschuss 21,0 MWh/a (nicht gezeichnet)", m.KnotenMit(Energiefluss.SOLARTHERMIE).Zeigertext);
            AlleKnotenGeschlossen(m);
            Assert.Empty(m.Hinweise);
        }

        // =====================================================================
        //  Kälte: Wärmepumpe heizt und kühlt, Kältemaschine mit Speicher, freie Kühlung, Split
        // =====================================================================

        [Fact]
        public void Kaelte_mit_Waermepumpe_Kaeltemaschine_Speicher_freier_Kuehlung_und_Split()
        {
            ErgebnisModel e = Baum(netz: 10.0, heizung: 20.0);
            e.Energiebedarf.Kaeltebedarf_Gesamt = 20.0;
            e.Energiebedarf.Kaelterestbedarf = 0.0;
            e.Waermepumpe = new ErgebnisWaermepumpeModel
            {
                Waermeproduktion_WP = 20.0, Stromverbrauch_WP = 5.0, Kaelteproduktion_WP = 6.0, Stromverbrauch_Kuehlung = 1.5, FreieKuehlung_MWh = 1.0,
            };
            e.Kaeltemaschinen.Add(new ErgebnisKaeltemaschineModel { Bezeichner = "KM 1", Kaelteproduktion_MWh = 9.1, Stromverbrauch_MWh = 2.5, Hilfsstrom_MWh = 0.5 });
            e.KaelteDezentral.Add(new ErgebnisKaelteDezentralModel { Bezeichner = "Split Büro", Kaelte_MWh = 3.0, Strom_MWh = 1.0, Kuehl_CarrierId = 58, Kuehl_EigenerZaehler = true });
            e.KaelteDezentral.Add(new ErgebnisKaelteDezentralModel { Bezeichner = "Split Lager", Kaelte_MWh = 2.0, Strom_MWh = 0.5 });
            var ks = new ErgebnisPufferspeicherModel
            {
                ID_Pufferspeicher = 9, Bezeichner = "Kaltwasser", Verwendung = DbWerte.PSP_VERWENDUNG_KAELTE,
                Ladung_gesamt = 4000.0, Entladung_gesamt = 3900.0, Verluste_gesamt = 100.0,
            };
            ks.Entladung_Kanal[Kanal.KUEHLUNG] = 3900.0;
            e.Pufferspeicher.Add(ks);
            EnergieflussBilanz m = Aus(e);

            Assert.Equal("Wärmepumpe (Heizen)", m.KnotenMit(Energiefluss.WP_HEIZEN).Name);
            Assert.Equal("Wärmepumpe (Kühlen)", m.KnotenMit(Energiefluss.WP_KUEHLEN).Name);
            Assert.Equal(1.5, m.Fluss(Energiefluss.STROM, Energiefluss.WP_KUEHLEN), 9);
            Assert.Equal(5.0, m.Fluss(Energiefluss.WP_KUEHLEN, Energiefluss.KAELTE), 9);
            Assert.Equal(3.5, m.Fluss(Energiefluss.UMWELTKAELTE, Energiefluss.WP_KUEHLEN), 9);
            Assert.Equal(1.0, m.Fluss(Energiefluss.FREIE_KUEHLUNG, Energiefluss.KAELTE), 9);
            Assert.Equal(1.0, m.Fluss(Energiefluss.UMWELTKAELTE, Energiefluss.FREIE_KUEHLUNG), 9);
            string km = Energiefluss.KAELTEMASCHINE + "1";
            Assert.Equal("KM 1", m.KnotenMit(km).Name);
            Assert.Equal(3.0, m.Fluss(Energiefluss.STROM, km), 9);
            Assert.Equal(6.1, m.Fluss(Energiefluss.UMWELTKAELTE, km), 9);
            // Split mit eigenem Zähler: neben der Stufenrechnung, nicht über den Stromknoten.
            Assert.Equal(1.0, m.Fluss(Energiefluss.KUEHLSTROM_EIGEN, Energiefluss.SPLIT + "1"), 9);
            Assert.Equal(0.0, m.Fluss(Energiefluss.STROM, Energiefluss.SPLIT + "1"), 9);
            Assert.Equal(0.5, m.Fluss(Energiefluss.STROM, Energiefluss.SPLIT + "2"), 9);
            // Kältespeicher.
            string sp = Energiefluss.KAELTESPEICHER + "9:0";
            Assert.Equal(4.0, m.Fluss(Energiefluss.KAELTE, sp), 9);
            Assert.Equal(3.9, m.Fluss(sp, Energiefluss.KUEHLUNG), 9);
            Assert.Equal(0.1, m.Fluss(sp, Energiefluss.VERLUST + sp), 9);
            Assert.Equal(16.1, m.Fluss(Energiefluss.KAELTE, Energiefluss.KUEHLUNG), 9);
            Assert.Equal(20.0, m.KnotenMit(Energiefluss.KUEHLUNG).ZuflussMwh, 9);
            Assert.Equal(20.1, m.KnotenMit(Energiefluss.KAELTE).ZuflussMwh, 9);
            Assert.Empty(m.Hinweise);
            Assert.True(m.KnotenMit(Energiefluss.KAELTE).Geschlossen);
            AlleKnotenGeschlossen(m);
        }

        // =====================================================================
        //  Stromspeicher
        // =====================================================================

        [Fact]
        public void Stromspeicher_ist_Zwischenknoten_mit_Entladung_an_den_Strombedarf()
        {
            ErgebnisModel e = Baum(strombedarf: 20.0, netz: 16.0);
            e.Photovoltaik = new ErgebnisPhotovoltaikModel { Stromproduktion = 6.0, Ueberschuss = 2.5 };
            e.Stromspeicher.Add(new ErgebnisStromspeicherModel
            {
                Bezeichner = "Batterie", Ladung_Gesamt = 1200.0, Entladung_Gesamt = 1000.0, Verluste_Gesamt = 150.0, Einspeisung_Mit = 1800.0,
            });
            EnergieflussBilanz m = Aus(e);
            string sp = Energiefluss.STROMSPEICHER + "1";
            Assert.Equal(1.2, m.Fluss(Energiefluss.STROM, sp), 9);
            Assert.Equal(1.0, m.Fluss(sp, Energiefluss.STROMBEDARF), 9);
            // Füllstand 0,05 ≥ 1 % von 1,2: eigenes Band.
            Assert.Equal(0.15, m.Fluss(sp, Energiefluss.VERLUST + sp), 9);
            Assert.Equal(0.05, m.Fluss(sp, Energiefluss.FUELLSTAND + sp), 9);
            Assert.Equal(19.0, m.Fluss(Energiefluss.STROM, Energiefluss.STROMBEDARF), 9);
            // Mit Speicher die Einspeisung der Speicherzeile.
            Assert.Equal(1.8, m.Fluss(Energiefluss.STROM, Energiefluss.EINSPEISUNG_PV), 9);
            Assert.Contains("eine Zuordnung zu einzelnen Verbrauchern gibt es nicht", m.KnotenMit(sp).Zeigertext);
            AlleKnotenGeschlossen(m);
            Assert.Empty(m.Hinweise);
        }

        // =====================================================================
        //  Bündelung
        // =====================================================================

        private static ErgebnisModel Traeger(int anzahlKlein)
        {
            ErgebnisModel e = Baum(heizung: 0.0);
            e.Heizkessel = new ErgebnisHeizkesselModel();
            e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Gas", CarrierId = 1, Verbrauch = 100.0, Waermeproduktion = 100.0 });
            for (int i = 0; i < anzahlKlein; i++)
                e.Heizkessel.Module.Add(new ErgebnisHeizkesselModulModel { Brennstoff = "Holz", CarrierId = 10 + i, Verbrauch = 0.2, Waermeproduktion = 0.2 });
            e.Energiebedarf.Waermebedarf_Kanal[Kanal.HEIZUNG] = 100.0 + 0.2 * anzahlKlein;
            return e;
        }

        [Fact]
        public void Kleine_Brennstofftraeger_werden_ab_vier_gebuendelt()
        {
            EnergieflussBilanz m = Aus(Traeger(4));
            EnergieflussKnoten b = m.KnotenMit(Energiefluss.WEITERE + "traeger");
            Assert.NotNull(b);
            Assert.Equal("weitere (4)", b.Name);
            Assert.Equal(4, b.Gebuendelt.Count);
            Assert.Equal(0.8, m.Fluss(b.Id, Energiefluss.HEIZKESSEL), 9);
            Assert.Contains("gebündelt: ", b.Zeigertext);
            Assert.Equal(100.0, m.Fluss("traeger:1", Energiefluss.HEIZKESSEL), 9);
            Assert.DoesNotContain(m.Knoten, k => k.Id == "traeger:10");
            AlleKnotenGeschlossen(m);
        }

        [Fact]
        public void Drei_kleine_Traeger_bleiben_einzeln_stehen()
        {
            EnergieflussBilanz m = Aus(Traeger(3));
            Assert.Null(m.KnotenMit(Energiefluss.WEITERE + "traeger"));
            Assert.Equal(4, m.Knoten.Count(k => k.Id.StartsWith(Energiefluss.TRAEGER, StringComparison.Ordinal)));
        }

        // =====================================================================
        //  Reihenfolge, Sprache, Zeigertexte, KI-Text
        // =====================================================================

        [Fact]
        public void Knoten_stehen_nach_Spalten_und_in_der_Reihenfolge_der_Tabelle()
        {
            ErgebnisModel e = Bhkw(strombedarf: 6.0, netz: 1.0);
            e.Photovoltaik = new ErgebnisPhotovoltaikModel { Stromproduktion = 2.0, Ueberschuss = 0.0 };
            EnergieflussBilanz m = Aus(e);
            var spalten = m.Knoten.Select(k => k.Spalte).ToList();
            Assert.Equal(spalten.OrderBy(s => s).ToList(), spalten);
            var quellen = m.Knoten.Where(k => k.Art == EnergieflussKnotenart.Quelle).Select(k => k.Id).ToList();
            Assert.Equal(new[] { Energiefluss.NETZBEZUG, Energiefluss.PHOTOVOLTAIK, "traeger:5" }, quellen);
            // Bänder in der Reihenfolge ihrer Quellknoten.
            Assert.Equal(Energiefluss.NETZBEZUG, m.Fluesse[0].Von);
        }

        [Fact]
        public void Englische_Namen_und_Zeigertexte()
        {
            EnergieflussBilanz m = Aus(WaermepumpeMitPuffer(100.0), englisch: true);
            Assert.Equal("Electricity", m.KnotenMit(Energiefluss.STROM).Name);
            Assert.Equal("Heat pump", m.KnotenMit(Energiefluss.WP_HEIZEN).Name);
            Assert.Equal("HP", m.KnotenMit(Energiefluss.WP_HEIZEN).Kurzname);
            Assert.Equal("Ambient heat → Heat pump: 31.0 MWh/a (67 %) · derived: heat − electricity",
                         m.FlussMit(Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN).Zeigertext);
        }

        [Fact]
        public void Zeigertexte_Marken_und_Kurznamen()
        {
            EnergieflussBilanz m = Aus(WaermepumpeMitPuffer(100.0));
            EnergieflussFluss f = m.FlussMit(Energiefluss.NETZBEZUG, Energiefluss.STROM);
            Assert.Equal("Netzbezug → Strom: 40,0 MWh/a (100 %)", f.Zeigertext);
            Assert.Equal("fluss:netzbezug:strom", f.Marke);
            // Bedarfssenke: Anteil am Bedarf.
            Assert.Equal("Wärme → Heizung: 25,0 MWh/a (50 %) · abgeleitet: Bedarf − Speicherentladung − ungedeckt",
                         m.FlussMit(Energiefluss.WAERME, Energiefluss.HEIZUNG).Zeigertext);
            EnergieflussKnoten k = m.KnotenMit(Energiefluss.WAERME);
            Assert.Equal("knoten:waerme", k.Marke);
            Assert.Equal("Wärme: Zufluss 62,0 · Abfluss 62,0 MWh/a", k.Zeigertext);
            Assert.All(m.Knoten, x => Assert.False(string.IsNullOrEmpty(x.Kurzname), x.Id));
            Assert.All(m.Fluesse, x => Assert.False(string.IsNullOrEmpty(x.Zeigertext)));
            Assert.Equal("WP", m.KnotenMit(Energiefluss.WP_HEIZEN).Kurzname);
        }

        [Fact]
        public void KI_Flussliste_nennt_jedes_Band_mit_Herkunft()
        {
            EnergieflussBilanz m = Aus(Bhkw(strombedarf: 6.0, netz: 1.0));
            string ki = m.KiText();
            Assert.StartsWith("[fluss] Energiefluss — Stamm", ki);
            Assert.Contains("Netzbezug → Strom: 1,00 MWh/a (9 %) · direkt", ki);
            Assert.Contains("Strom → Einspeisung BHKW: 5,00 MWh/a (45 %) · bilanziert", ki);
            Assert.Contains("BHKW → Verlust: 5,00 MWh/a (17 %) · abgeleitet", ki);
            Assert.Equal(m.Fluesse.Count + 1, ki.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        }
    }
}
