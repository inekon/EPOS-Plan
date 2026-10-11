using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Proben der Zulässigkeitsprüfung nach Anhang IV</b> (Konzept Kältemittel als Stammdatum 3.3,
    /// 5.1 und 10; KM4-E2-a) — ohne Datenbank, gegen den gesäten Regelsatz der Generation 10
    /// (<see cref="GesetzKatalog.Vorbelegung"/>). Die Stoffwerte entsprechen den Zeilen der
    /// Kältemitteltabelle der Auslieferung.
    /// </summary>
    public class KaeltemittelZulaessigkeitTests
    {
        private static readonly IReadOnlyList<KaeltemittelRegel> REGELN =
            Kaeltemittelregeln.Zerlegen(GesetzKatalog.Vorbelegung());

        private static readonly KaeltemittelStoff R32 = new("R32", "HFKW", 675.0, true);
        private static readonly KaeltemittelStoff R410A = new("R410A", "GEMISCH", 2087.5, true);
        private static readonly KaeltemittelStoff R404A = new("R404A", "GEMISCH", 3921.6, true);
        private static readonly KaeltemittelStoff R513A = new("R513A", "GEMISCH", 629.5, true);
        private static readonly KaeltemittelStoff R1234ZE = new("R1234ze(E)", "HFO", 1.37, true);
        private static readonly KaeltemittelStoff R1234YF = new("R1234yf", "HFO", 0.501, true);
        private static readonly KaeltemittelStoff R290 = new("R290", "NATUERLICH", 0.02, false);

        private static KaeltemittelErgebnis Pruefe(string klasse, double? kw, KaeltemittelStoff stoff, int? jahr,
                                                   double? geraeteGwp = null, IReadOnlyList<KaeltemittelRegel> regeln = null)
        {
            return Kaeltemittelzulaessigkeit.Pruefen(new KaeltemittelEingang
            {
                Geraeteklasse = klasse, LeistungKw = kw, Stoff = stoff, Jahr = jahr,
                GeraeteGwp = geraeteGwp, Regeln = regeln ?? REGELN
            });
        }

        private static void Warnung(KaeltemittelErgebnis e, string schluessel, int stichtag, double grenze)
        {
            Assert.NotNull(e);
            Assert.Equal(KaeltemittelErgebnisklasse.AbStichtag, e.Klasse);
            Assert.Equal(schluessel, e.Regelschluessel);
            Assert.Equal(stichtag, e.Stichtag);
            Assert.Equal(grenze, e.Grenze);
        }

        private static void Zulaessig(KaeltemittelErgebnis e)
        {
            Assert.NotNull(e);
            Assert.Equal(KaeltemittelErgebnisklasse.Zulaessig, e.Klasse);
            Assert.Null(e.Regelschluessel);
        }

        private static void NichtPruefbar(KaeltemittelErgebnis e, string fehlt)
        {
            Assert.NotNull(e);
            Assert.Equal(KaeltemittelErgebnisklasse.NichtPruefbar, e.Klasse);
            Assert.Equal(fehlt, e.FehlendeGroesse);
        }

        private const string K = DbWerte.FGAS_KLASSE_KUEHLER;
        private const string M = DbWerte.FGAS_KLASSE_MONOBLOCK;
        private const string SLW = DbWerte.FGAS_KLASSE_SPLIT_LUFTWASSER;
        private const string SLL = DbWerte.FGAS_KLASSE_SPLIT_LUFTLUFT;

        // ---------------------------------------------------------------- Klassen 1 und 2

        [Fact]
        public void Kuehler_20_kW_mit_R513A_ist_2027_zulaessig()
        {
            // 1063: GWP 629,5 unter der Grenze 750 der Nr. 7 d; kein HFKW-Gemisch über 2 500 (Nr. 7 a).
            KaeltemittelErgebnis e = Pruefe(K, 20.0, R513A, 2027);
            Zulaessig(e);
            Assert.Equal(2027, e.Jahr);
            Assert.Equal(20.0, e.LeistungKw);
            Assert.Equal(629.5, e.Gwp);
        }

        [Fact]
        public void Kuehler_10_kW_mit_R513A_warnt_ab_2027_und_ist_2026_zulaessig()
        {
            // 1059 skaliert: Nr. 7 b (bis 12 kW, GWP ≥ 150, 2027).
            KaeltemittelErgebnis e = Pruefe(K, 10.0, R513A, 2027);
            Warnung(e, DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_GWP, 2027, 150.0);
            Assert.Equal(2027, e.Jahr);
            Assert.Equal(10.0, e.LeistungKw);

            Zulaessig(Pruefe(K, 10.0, R513A, 2026));
        }

        [Fact]
        public void Treffen_mehrere_Regeln_nennt_das_Ergebnis_die_des_fruehesten_Stichtags()
        {
            // 2033: Nr. 7 b (2027) und Nr. 7 c (2032) treffen — genannt wird 2027.
            Warnung(Pruefe(K, 10.0, R513A, 2033), DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_GWP, 2027, 150.0);
            // R404A 20 kW, 2028: Nr. 7 a (2020) und 7 d (2027) — genannt wird 2020.
            Warnung(Pruefe(K, 20.0, R404A, 2028), DbWerte.GESETZ_FGAS_IV_KUEHLER_ALLE_HFKW, 2020, 2500.0);
        }

        // ---------------------------------------------------------------- Klasse 4 und ohne Jahr

        [Fact]
        public void Ohne_Jahr_gibt_es_kein_Ergebnis()
        {
            Assert.Null(Pruefe(K, 10.0, R513A, null));
            Assert.Null(Pruefe(null, null, null, null));
        }

        [Fact]
        public void Absorption_ergibt_keinen_Befund()
        {
            Assert.Null(Pruefe(Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION, 100.0, null, 2030));
        }

        [Fact]
        public void Fehlt_eine_Eingangsgroesse_ist_das_Ergebnis_nicht_pruefbar()
        {
            NichtPruefbar(Pruefe(K, null, R513A, 2027), Kaeltemittelzulaessigkeit.FEHLT_LEISTUNG);
            NichtPruefbar(Pruefe(K, 0.0, R513A, 2027), Kaeltemittelzulaessigkeit.FEHLT_LEISTUNG);
            // SONSTIGES oder ein unbekannter Text: kein Tabellentreffer und kein Geräte-GWP.
            NichtPruefbar(Pruefe(K, 10.0, null, 2027), Kaeltemittelzulaessigkeit.FEHLT_KAELTEMITTEL);
            // Bauart leer an der Wärmepumpe: keine Geräteklasse.
            NichtPruefbar(Pruefe(Kaeltemittelregeln.GeraeteklasseWaermepumpe(null, "Luft-Wasser"), 10.0, R32, 2027),
                          Kaeltemittelzulaessigkeit.FEHLT_GERAETEKLASSE);
            // Eine Tabellenzeile ohne F-Gas-Kennzeichen entscheidet „jedes F-Gas“ nicht.
            NichtPruefbar(Pruefe(K, 10.0, new KaeltemittelStoff("RX", "SONSTIGE", 100.0, null), 2032),
                          Kaeltemittelzulaessigkeit.FEHLT_FGAS);
        }

        [Fact]
        public void Eine_treffende_Regel_geht_einer_offenen_vor()
        {
            // 2033: 7 b trifft (GWP 200 ≥ 150), 7 c bliebe ohne Kennzeichen offen.
            Warnung(Pruefe(K, 10.0, new KaeltemittelStoff("RX", "SONSTIGE", 200.0, null), 2033),
                    DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_GWP, 2027, 150.0);
        }

        // ---------------------------------------------------------------- Oberklasse, Bänder

        [Fact]
        public void Split_Luft_Wasser_10_kW_trifft_die_eigene_Regel_und_die_der_Oberklasse()
        {
            Warnung(Pruefe(SLW, 10.0, R32, 2027), DbWerte.GESETZ_FGAS_IV_SPLIT_LUFTWASSER_BIS12_GWP, 2027, 150.0);
            // R1234yf (GWP 0,5): erst „jedes F-Gas“ der Oberklasse Split (Nr. 9 d, 2035).
            Zulaessig(Pruefe(SLW, 10.0, R1234YF, 2034));
            Warnung(Pruefe(SLW, 10.0, R1234YF, 2035), DbWerte.GESETZ_FGAS_IV_SPLIT_BIS12_FGAS, 2035, 0.0);
            // Ein natürliches Kältemittel ist kein F-Gas.
            Zulaessig(Pruefe(SLW, 10.0, R290, 2040));
            // Luft-Luft erbt dieselbe Oberklasse, hat aber eine eigene Regel (Nr. 9 c, 2029).
            Zulaessig(Pruefe(SLL, 10.0, R32, 2028));
            Warnung(Pruefe(SLL, 10.0, R32, 2029), DbWerte.GESETZ_FGAS_IV_SPLIT_LUFTLUFT_BIS12_GWP, 2029, 150.0);
        }

        [Fact]
        public void Split_14_kW_trifft_die_Regeln_ueber_12_kW_als_Zeitreihe()
        {
            // R32 (675): unter 750 (Nr. 9 e, 2029), ab 2033 über 150 (Nr. 9 f).
            Zulaessig(Pruefe(SLW, 14.0, R32, 2032));
            Warnung(Pruefe(SLW, 14.0, R32, 2033), DbWerte.GESETZ_FGAS_IV_SPLIT_UEBER12_GWP, 2033, 150.0);
            // R410A (2 087,5): ab 2029 — und 2034 bleibt der Stichtag 2029, nicht die Verschärfung 2033.
            Zulaessig(Pruefe(SLW, 14.0, R410A, 2028));
            Warnung(Pruefe(SLW, 14.0, R410A, 2029), DbWerte.GESETZ_FGAS_IV_SPLIT_UEBER12_GWP, 2029, 750.0);
            Warnung(Pruefe(SLW, 14.0, R410A, 2034), DbWerte.GESETZ_FGAS_IV_SPLIT_UEBER12_GWP, 2029, 750.0);
        }

        [Fact]
        public void Die_Baender_sind_Intervalle_mit_einschliesslicher_oberer_Grenze()
        {
            // 12 kW gehört zu „bis einschließlich 12 kW“, darüber zu „über 12 kW“.
            Warnung(Pruefe(K, 12.0, R513A, 2027), DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_GWP, 2027, 150.0);
            Zulaessig(Pruefe(K, 12.0001, R513A, 2027));
            // 50 kW gehört zu „über 12 bis 50 kW“ (Nr. 8 d, 2027), darüber nur Nr. 8 e (2030).
            Warnung(Pruefe(M, 50.0, R32, 2027), DbWerte.GESETZ_FGAS_IV_MONOBLOCK_UEBER12BIS50_GWP, 2027, 150.0);
            Zulaessig(Pruefe(M, 50.1, R32, 2029));
            Warnung(Pruefe(M, 50.1, R32, 2030), DbWerte.GESETZ_FGAS_IV_MONOBLOCK_ALLE_GWP, 2030, 150.0);
            // Monoblock bis 12 kW: Nr. 8 b (2027) vor Nr. 8 e (2030).
            Warnung(Pruefe(M, 8.0, R32, 2031), DbWerte.GESETZ_FGAS_IV_MONOBLOCK_BIS12_GWP, 2027, 150.0);
        }

        [Fact]
        public void BandEnthaelt_liest_die_Intervalle()
        {
            Assert.True(Kaeltemittelzulaessigkeit.BandEnthaelt(DbWerte.FGAS_BAND_BIS12, 12.0));
            Assert.False(Kaeltemittelzulaessigkeit.BandEnthaelt(DbWerte.FGAS_BAND_UEBER12, 12.0));
            Assert.True(Kaeltemittelzulaessigkeit.BandEnthaelt(DbWerte.FGAS_BAND_UEBER12BIS50, 50.0));
            Assert.False(Kaeltemittelzulaessigkeit.BandEnthaelt(DbWerte.FGAS_BAND_UEBER50, 50.0));
            Assert.True(Kaeltemittelzulaessigkeit.BandEnthaelt(DbWerte.FGAS_BAND_UEBER50, 50.5));
            Assert.True(Kaeltemittelzulaessigkeit.BandEnthaelt(DbWerte.FGAS_BAND_ALLE, 1000.0));
            Assert.False(Kaeltemittelzulaessigkeit.BandEnthaelt("BIS13", 5.0));
        }

        // ---------------------------------------------------------------- HFKW gegen jedes F-Gas

        [Fact]
        public void Nur_HFKW_trifft_HFO_nicht_jedes_F_Gas_aber_schon()
        {
            // R1234ze(E) ist ein F-Gas (Anhang II), aber kein HFKW (Art. 3 Nr. 4: Anhang I Gruppe 1).
            // Nr. 7 a (nur HFKW) trifft es nicht, Nr. 7 b (GWP ≥ 150) wegen GWP 1,37 auch nicht,
            // Nr. 7 c (jedes F-Gas, bis 12 kW, 2032) schon.
            Zulaessig(Pruefe(K, 10.0, R1234ZE, 2031));
            Warnung(Pruefe(K, 10.0, R1234ZE, 2032), DbWerte.GESETZ_FGAS_IV_KUEHLER_BIS12_FGAS, 2032, 0.0);
            Zulaessig(Pruefe(K, 500.0, R1234ZE, 2040));

            // Ein Olefin mit GWP über 2 500 (Probe, kein Stoff der Auslieferung): Nr. 7 a trifft
            // nicht, weil es kein HFKW ist — ein HFKW mit demselben GWP schon.
            var hfo = new KaeltemittelStoff("PROBE-HFO", "HFO", 3000.0, true);
            var hfkw = new KaeltemittelStoff("PROBE-HFKW", "HFKW", 3000.0, true);
            Zulaessig(Pruefe(K, 20.0, hfo, 2025));
            Warnung(Pruefe(K, 20.0, hfkw, 2025), DbWerte.GESETZ_FGAS_IV_KUEHLER_ALLE_HFKW, 2020, 2500.0);
            // Ein Gemisch, dessen HFKW-Anteil nicht festgelegt ist: Stoffklasse unbekannt.
            NichtPruefbar(Pruefe(K, 20.0, new KaeltemittelStoff("R999X", "GEMISCH", 3000.0, true), 2025),
                          Kaeltemittelzulaessigkeit.FEHLT_STOFFKLASSE);
            // Die festgelegten Gemische mit HFKW-Bestandteil: R404A trifft Nr. 7 a.
            Warnung(Pruefe(K, 20.0, R404A, 2025), DbWerte.GESETZ_FGAS_IV_KUEHLER_ALLE_HFKW, 2020, 2500.0);
        }

        [Fact]
        public void EnthaeltHfkw_folgt_der_Stoffgruppe_und_der_Gemischliste()
        {
            Assert.True(Kaeltemittelzulaessigkeit.EnthaeltHfkw(R32));
            Assert.True(Kaeltemittelzulaessigkeit.EnthaeltHfkw(R513A));
            Assert.False(Kaeltemittelzulaessigkeit.EnthaeltHfkw(R1234ZE));
            Assert.False(Kaeltemittelzulaessigkeit.EnthaeltHfkw(new KaeltemittelStoff("R1233zd(E)", "HFCKW", 3.88, true)));
            Assert.False(Kaeltemittelzulaessigkeit.EnthaeltHfkw(R290));
            Assert.Null(Kaeltemittelzulaessigkeit.EnthaeltHfkw(new KaeltemittelStoff("R999X", "GEMISCH", 10.0, true)));
            Assert.Null(Kaeltemittelzulaessigkeit.EnthaeltHfkw(new KaeltemittelStoff("RX", null, 10.0, true)));
            Assert.Null(Kaeltemittelzulaessigkeit.EnthaeltHfkw(null));
        }

        /// <summary>
        /// R1233zd(E) führt die Saat als HFCKW (die Verordnung nennt es „HFCKW-1233zd“, Anhang II). HFCKW ist kein HFKW
        /// (Art. 3 Nr. 4), aber ein F-Gas (Art. 2 Buchst. a): Die Regeln urteilen über den Saatsatz genau so wie über
        /// denselben Stoff unter der früheren Gruppe HFO — auch bei einem GWP über 2 500 trifft keine reine HFKW-Regel.
        /// </summary>
        [Fact]
        public void R1233zd_E_der_Saat_ist_HFCKW_kein_HFKW_und_die_Regeln_bleiben_unberuehrt()
        {
            KaeltemittelSchema.Kaeltemittel saat = KaeltemittelSchema.SAAT.Single(k => k.Code == "R1233zd(E)");
            Assert.Equal(DbWerte.KAELTEMITTEL_GRUPPE_HFCKW, saat.Gruppe);
            Assert.True(saat.FGas);
            var hfckw = new KaeltemittelStoff(saat.Code, saat.Gruppe, saat.Gwp, saat.FGas);
            var alsHfo = new KaeltemittelStoff(saat.Code, DbWerte.KAELTEMITTEL_GRUPPE_HFO, saat.Gwp, saat.FGas);
            Assert.False(Kaeltemittelzulaessigkeit.EnthaeltHfkw(hfckw));

            foreach (string klasse in new[] { K, M, SLW, SLL })
                foreach (double kw in new[] { 10.0, 14.0, 60.0, 500.0 })
                    foreach (int jahr in new[] { 2020, 2025, 2027, 2030, 2032, 2035 })
                    {
                        KaeltemittelErgebnis a = Pruefe(klasse, kw, hfckw, jahr);
                        KaeltemittelErgebnis b = Pruefe(klasse, kw, alsHfo, jahr);
                        Assert.Equal((b.Klasse, b.Regelschluessel, b.Stichtag, b.Grenze),
                                     (a.Klasse, a.Regelschluessel, a.Stichtag, a.Grenze));
                    }

            // Mit einem GWP über 2 500 (Probe): die reine HFKW-Regel Nr. 7 a trifft ein HFCKW nicht, ein HFKW schon.
            Zulaessig(Pruefe(K, 20.0, new KaeltemittelStoff("PROBE-HFCKW", DbWerte.KAELTEMITTEL_GRUPPE_HFCKW, 3000.0, true), 2025));
            Warnung(Pruefe(K, 20.0, new KaeltemittelStoff("PROBE-HFKW", DbWerte.KAELTEMITTEL_GRUPPE_HFKW, 3000.0, true), 2025),
                    DbWerte.GESETZ_FGAS_IV_KUEHLER_ALLE_HFKW, 2020, 2500.0);
        }

        // ---------------------------------------------------------------- GWP-Vorrang (KM4-Q17 a)

        [Fact]
        public void Das_Tabellen_GWP_geht_dem_Geraete_GWP_vor()
        {
            // Tabelle 629,5 vor Gerät 800: unter 750, zulässig.
            KaeltemittelErgebnis e = Pruefe(K, 20.0, R513A, 2027, geraeteGwp: 800.0);
            Zulaessig(e);
            Assert.Equal(629.5, e.Gwp);
            Assert.True(e.GwpAusTabelle);

            // Ohne Tabellenzeile (Anwendercode) zählt das Geräte-GWP.
            e = Pruefe(K, 20.0, null, 2027, geraeteGwp: 800.0);
            Warnung(e, DbWerte.GESETZ_FGAS_IV_KUEHLER_UEBER12_GWP, 2027, 750.0);
            Assert.False(e.GwpAusTabelle);

            // Tabellenzeile ohne GWP: das Geräte-GWP springt ein.
            e = Pruefe(K, 20.0, new KaeltemittelStoff("RX", "SONSTIGE", null, true), 2027, geraeteGwp: 800.0);
            Warnung(e, DbWerte.GESETZ_FGAS_IV_KUEHLER_UEBER12_GWP, 2027, 750.0);
            Assert.Equal(800.0, e.Gwp);
        }

        // ---------------------------------------------------------------- Zeitreihe mit Entfall

        [Fact]
        public void Eine_Zeile_ohne_Wert_laesst_die_Regel_entfallen()
        {
            var regeln = new List<KaeltemittelRegel>
            {
                new("FGAS_IV_KUEHLER_BIS12_GWP", K, DbWerte.FGAS_BAND_BIS12, DbWerte.FGAS_KRIT_GWP, 2027, 150.0, "GESICHERT", ""),
                new("FGAS_IV_KUEHLER_BIS12_GWP", K, DbWerte.FGAS_BAND_BIS12, DbWerte.FGAS_KRIT_GWP, 2030, null, "GESICHERT", "")
            };
            Warnung(Pruefe(K, 10.0, R513A, 2028, regeln: regeln), "FGAS_IV_KUEHLER_BIS12_GWP", 2027, 150.0);
            Zulaessig(Pruefe(K, 10.0, R513A, 2030, regeln: regeln));
        }

        [Fact]
        public void Die_Klasse_Bestand_tritt_in_dieser_Fassung_nicht_ein()
        {
            foreach (int jahr in new[] { 2020, 2027, 2035 })
                foreach (string k in new[] { K, M, SLW, SLL })
                    Assert.NotEqual(KaeltemittelErgebnisklasse.Bestand, Pruefe(k, 10.0, R404A, jahr).Klasse);
        }
    }
}
