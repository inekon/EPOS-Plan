using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KM1 — Importart „Kältemaschine“ und eingebaute Typkennfelder</b>: Copper-Leser (synthetische Probe,
    /// Luft und Wasser), CSV-Kennfeldvorlage (drei Trennzeichen, Dezimalkomma), Rasterbildung mit dem
    /// LUFT-Versatz, Eurovent-Nennpunkt, Prüfregeln aller Typkennfelder, Einspielen (zweimal, idempotent) und
    /// der Importablauf gegen eine Arbeitskopie der Testdatenbank. Die Datenbankfälle schweigen ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineImportTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool MitKatalog => _db.Vorhanden && KaeltemaschineSchema.Vollstaendig();

        // CAPFT = 1 + 0,02·(x − 7) − 0,01·(y − 35); EIRFT = 1 − 0,02·(x − 7) + 0,02·(y − 35) (Luft, Bezug 7/35).
        // Wasser: dieselben Steigungen um den Bezug 6,67/29,4.
        private const string COPPER_PROBE = @"{
  ""0"": { ""eqp_type"": ""Chiller"", ""model"": ""ect_lwt"", ""ref_cap"": 10, ""ref_cap_unit"": ""ton"",
         ""full_eff"": 3.0, ""full_eff_unit"": ""cop"", ""compressor_type"": ""scroll"", ""condenser_type"": ""air"",
         ""compressor_speed"": null, ""min_plr"": 0.25, ""min_unloading"": 0.25, ""max_plr"": 1.0,
         ""set_of_curves"": {
           ""cap-f-t"": { ""type"": ""bi_quad"", ""units"": ""si"", ""x_min"": 4, ""x_max"": 16, ""y_min"": 10, ""y_max"": 40,
                        ""out_min"": 0, ""out_max"": null, ""coeff1"": 1.21, ""coeff2"": 0.02, ""coeff3"": 0, ""coeff4"": -0.01, ""coeff5"": 0, ""coeff6"": 0 },
           ""eir-f-t"": { ""type"": ""bi_quad"", ""units"": ""si"", ""x_min"": 4, ""x_max"": 16, ""y_min"": 10, ""y_max"": 40,
                        ""out_min"": 0, ""out_max"": null, ""coeff1"": 0.44, ""coeff2"": -0.02, ""coeff3"": 0, ""coeff4"": 0.02, ""coeff5"": 0, ""coeff6"": 0 },
           ""eir-f-plr"": { ""type"": ""quad"", ""units"": ""si"", ""x_min"": 0, ""x_max"": 1, ""coeff1"": 0.2, ""coeff2"": 0.3, ""coeff3"": 0.5 } } },
  ""1"": { ""eqp_type"": ""Chiller"", ""model"": ""ect_lwt"", ""ref_cap"": 500, ""ref_cap_unit"": ""kW"",
         ""full_eff"": 5.5, ""full_eff_unit"": ""cop"", ""compressor_type"": ""centrifugal"", ""condenser_type"": ""water"",
         ""compressor_speed"": ""variable"", ""min_plr"": null, ""min_unloading"": 0.15,
         ""set_of_curves"": {
           ""cap-f-t"": { ""type"": ""bi_quad"", ""units"": ""si"", ""x_min"": 5, ""x_max"": 10, ""y_min"": 15, ""y_max"": 35,
                        ""coeff1"": 1.1606, ""coeff2"": 0.02, ""coeff3"": 0, ""coeff4"": -0.01, ""coeff5"": 0, ""coeff6"": 0 },
           ""eir-f-t"": { ""type"": ""bi_quad"", ""units"": ""si"", ""x_min"": 5, ""x_max"": 10, ""y_min"": 15, ""y_max"": 35,
                        ""coeff1"": 0.5454, ""coeff2"": -0.02, ""coeff3"": 0, ""coeff4"": 0.02, ""coeff5"": 0, ""coeff6"": 0 } } },
  ""2"": { ""model"": ""lct_lwt"", ""ref_cap"": 100, ""full_eff"": 5, ""set_of_curves"": {} },
  ""3"": { ""model"": ""ect_lwt"", ""ref_cap"": null, ""full_eff"": 5, ""set_of_curves"": {} }
}";

        // =================================================================
        //  Copper-Leser und Kennfeld
        // =================================================================

        [Fact]
        public void Der_Copper_Leser_liest_Luft_und_Wasser_und_uebergeht_mit_Grund()
        {
            CopperKaltwassersatzLeser.Ergebnis e = CopperKaltwassersatzLeser.Lesen(COPPER_PROBE);
            Assert.Equal(2, e.Saetze.Count);
            Assert.Equal(2, e.Uebergangen.Count);
            Assert.Contains(e.Uebergangen, u => u.StartsWith("2:", StringComparison.Ordinal) && u.Contains("lct_lwt"));
            Assert.Contains(e.Uebergangen, u => u.StartsWith("3:", StringComparison.Ordinal));

            KaeltemaschinenKurvensatz luft = e.Saetze[0], wasser = e.Saetze[1];
            Assert.True(luft.IstLuft);
            Assert.False(wasser.IstLuft);
            Assert.Equal(10 * CopperKaltwassersatzLeser.KW_JE_TON, luft.ReferenzleistungKw, 9);
            Assert.Equal(500.0, wasser.ReferenzleistungKw, 9);
            Assert.Equal(25.0, KaeltemaschinenKennfeld.Mindestteillast(luft));
            Assert.Equal(15.0, KaeltemaschinenKennfeld.Mindestteillast(wasser)); // min_unloading als Rückfall
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_LUFT, KaeltemaschinenKennfeld.VorgabeRueckkuehlart(luft));
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER, KaeltemaschinenKennfeld.VorgabeRueckkuehlart(wasser));
        }

        [Fact]
        public void Das_Raster_traegt_bei_Luft_den_Versatz_der_Graedigkeit()
        {
            KaeltemaschinenKurvensatz luft = CopperKaltwassersatzLeser.Lesen(COPPER_PROBE).Saetze[0];
            List<KaeltemaschineKenndatenModel> raster = KaeltemaschinenKennfeld.Raster(luft, KaeltemaschineSchema.RUECKKUEHLART_LUFT);

            // Achse in der Kurventemperatur 15 … 40 (Vorgabeband 15 … 45 ∩ Gültigkeit 10 … 40), Raster +5 K.
            Assert.Equal(new[] { 20.0, 25.0, 30.0, 35.0, 40.0, 45.0 },
                         raster.Select(p => p.Rueckkuehltemperatur).Distinct().OrderBy(t => t).ToArray());
            Assert.Equal(KaeltemaschinenKennfeld.KALTWASSER_ACHSE.ToArray(),
                         raster.Select(p => p.Kaltwassertemperatur).Distinct().OrderBy(t => t).ToArray());
            Assert.Equal(24, raster.Count);

            // T_rk = 40 °C ist die Kurve bei 35 °C Außenluft — dort CAPFT = EIRFT = 1.
            KaeltemaschineKenndatenModel p40 = raster.Single(p => p.Rueckkuehltemperatur == 40.0 && p.Kaltwassertemperatur == 7.0);
            Assert.Equal(Math.Round(luft.ReferenzleistungKw, 2), p40.Kaelteleistung_kW.Value, 2);
            Assert.Equal(3.0, p40.EER.Value, 3);
            (double q35, double e35) = KaeltemaschinenKennfeld.Kurvenwert(luft, 7.0, 35.0);
            (double q40, double e40) = KaeltemaschinenKennfeld.Kurvenwert(luft, 7.0, 40.0);
            Assert.Equal(Math.Round(e35, 3), p40.EER.Value, 3);
            Assert.NotEqual(Math.Round(e40, 3), p40.EER.Value);
            Assert.Equal(Math.Round(q35, 2), p40.Kaelteleistung_kW.Value, 2);
            Assert.True(q40 < q35);

            // Wassergekühlt: kein Versatz.
            KaeltemaschinenKurvensatz wasser = CopperKaltwassersatzLeser.Lesen(COPPER_PROBE).Saetze[1];
            List<KaeltemaschineKenndatenModel> rw = KaeltemaschinenKennfeld.Raster(wasser, KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER);
            Assert.Equal(new[] { 15.0, 19.0, 23.0, 27.0, 31.0, 35.0 },
                         rw.Select(p => p.Rueckkuehltemperatur).Distinct().OrderBy(t => t).ToArray());
            KaeltemaschineKenndatenModel w23 = rw.Single(p => p.Rueckkuehltemperatur == 23.0 && p.Kaltwassertemperatur == 10.0);
            Assert.Equal(Math.Round(KaeltemaschinenKennfeld.Kurvenwert(wasser, 10.0, 23.0).Eer, 3), w23.EER.Value, 3);
        }

        [Fact]
        public void Nennwerte_stehen_am_Eurovent_Nennpunkt_und_die_Klasse_skaliert_nur_die_Leistung()
        {
            CopperKaltwassersatzLeser.Ergebnis e = CopperKaltwassersatzLeser.Lesen(COPPER_PROBE);
            KaeltemaschinenKurvensatz wasser = e.Saetze[1];
            // Wasser: 7 °C / 30 °C Eintritt, nicht der US-Bezug 6,67 / 29,4.
            double cap = 1.1606 + 0.02 * 7.0 - 0.01 * 30.0;
            double eir = 0.5454 - 0.02 * 7.0 + 0.02 * 30.0;
            (double q, double eer) = KaeltemaschinenKennfeld.Nennpunkt(wasser);
            Assert.Equal(500.0 * cap, q, 9);
            Assert.Equal(5.5 / eir, eer, 9);

            KaeltemaschineModel m = KaeltemaschinenKennfeld.Modell(wasser, "Probe", KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, 1000.0);
            Assert.Equal(1000.0, m.Nennkaelteleistung_kW);
            Assert.Equal(Math.Round(5.5 / eir, 2), m.Nenn_EER);
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, m.Rueckkuehlart);
            KaeltemaschineKenndatenModel p = m.Kennlinie.Single(k => k.Rueckkuehltemperatur == 31.0 && k.Kaltwassertemperatur == 7.0);
            Assert.Equal(Math.Round(KaeltemaschinenKennfeld.Kurvenwert(wasser, 7.0, 31.0).LeistungKw * 1000.0 / q, 2), p.Kaelteleistung_kW.Value, 2);
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));

            // Luft: Nennpunkt 35 °C Außenluft = CAPFT = EIRFT = 1; eine Wasser-Rückkühlart passt nicht zur Luftkurve.
            KaeltemaschineModel l = KaeltemaschinenKennfeld.Modell(e.Saetze[0], "Luftprobe", KaeltemaschineSchema.RUECKKUEHLART_WASSER);
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_LUFT, l.Rueckkuehlart);
            Assert.Equal(3.0, l.Nenn_EER);
            Assert.Equal(Math.Round(10 * CopperKaltwassersatzLeser.KW_JE_TON, 1), l.Nennkaelteleistung_kW);
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(l));
        }

        // =================================================================
        //  CSV-Kennfeldvorlage
        // =================================================================

        private static string Vorlage(char trenn, bool komma)
        {
            string z(double d) => komma ? d.ToString("0.0#", CultureInfo.GetCultureInfo("de-DE")) : d.ToString("0.0#", CultureInfo.InvariantCulture);
            string t = trenn.ToString();
            var zeilen = new List<string>
            {
                "# Kennfeldvorlage – Kommentar",
                "Bezeichner" + t + "Probe A",
                "Rückkühlart" + t + "Trockenkühler",
                "Nennkälteleistung [kW]" + t + z(120.5),
                "Mindestteillast [%]" + t + z(20),
                "Kältemittel" + t + "R290",
                "Rueckkuehltemperatur" + t + "Kaltwassertemperatur" + t + "Kaelteleistung_kW" + t + "EER",
            };
            foreach (double rk in new[] { 25.0, 35.0 })
                foreach (double kw in new[] { 7.0, 12.0 })
                    zeilen.Add(z(rk) + t + z(kw) + t + z(100 + kw - rk / 10) + t + z(5.5 - rk / 20 + kw / 50));
            zeilen.Add("");
            zeilen.Add("Bezeichner" + t + "Probe B");
            zeilen.Add("Rueckkuehlart" + t + "LUFT");
            zeilen.Add("Nenn-EER" + t + z(3.25));
            zeilen.Add(z(30) + t + z(7) + t + z(50) + t + z(3.5));
            zeilen.Add(z(45) + t + z(7) + t + z(40) + t + z(2.5));
            return string.Join("\r\n", zeilen);
        }

        [Theory]
        [InlineData(';', true)]
        [InlineData('\t', true)]
        [InlineData(',', false)]
        [InlineData(';', false)]
        public void Die_CSV_Vorlage_erkennt_Trennzeichen_und_Dezimalzeichen(char trenn, bool komma)
        {
            KaeltemaschineCsvLeser.Ergebnis e = KaeltemaschineCsvLeser.Lesen(Vorlage(trenn, komma));
            Assert.Equal(trenn, e.Trennzeichen);
            Assert.Empty(e.Uebergangen);
            Assert.Equal(2, e.Geraete.Count);

            KaeltemaschineModel a = e.Geraete[0];
            Assert.Equal("Probe A", a.Bezeichner);
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, a.Rueckkuehlart);
            Assert.Equal(120.5, a.Nennkaelteleistung_kW);
            Assert.Equal(20.0, a.Mindestteillast_Prozent);
            Assert.Equal("R290", a.Kaeltemittel);
            Assert.Equal(4, a.Kennlinie.Count);
            Assert.Equal(5.5 - 35 / 20.0 + 12 / 50.0, a.Kennlinie.Single(p => p.Rueckkuehltemperatur == 35 && p.Kaltwassertemperatur == 12).EER.Value, 2);
            // Nenn-EER fehlt: aus dem Kennfeld am Eurovent-Punkt (Wasser 30 °C, Kaltwasser 7 °C) — Mitte von 25 und 35.
            double e25 = 5.5 - 25 / 20.0 + 7 / 50.0, e35 = 5.5 - 35 / 20.0 + 7 / 50.0;
            Assert.Equal(Math.Round((e25 + e35) / 2, 2), a.Nenn_EER.Value, 2);
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(a));

            KaeltemaschineModel b = e.Geraete[1];
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_LUFT, b.Rueckkuehlart);
            Assert.Equal(3.25, b.Nenn_EER);
            // Nennleistung fehlt: Luft 35 °C + 5 K = 40 °C zwischen 30 und 45 °C.
            Assert.Equal(Math.Round(50 + (40 - 50) * (10.0 / 15.0), 1), b.Nennkaelteleistung_kW.Value, 1);
        }

        [Fact]
        public void Die_CSV_Vorlage_meldet_unbekannte_Angaben_und_die_Datei_im_Repo_ist_lesbar()
        {
            KaeltemaschineCsvLeser.Ergebnis e = KaeltemaschineCsvLeser.Lesen("Bezeichner;X\nFarbe;blau\nRueckkuehlart;Sole\n30;7;50;3\n");
            Assert.Single(e.Geraete);
            Assert.Equal(2, e.Uebergangen.Count);

            string pfad = Path.Combine(Repowurzel(), "Quellen", "Kaeltemaschine_Kennfeldvorlage.csv");
            KaeltemaschineImportDatei.Ergebnis d = KaeltemaschineImportDatei.Lesen(pfad);
            Assert.False(d.Copper);
            Assert.Empty(d.Uebergangen);
            Assert.NotEmpty(d.Saetze);
            Assert.All(d.Saetze, s => Assert.Null(KaeltemaschineStammCtrl.Pruefen(s.Modell)));
        }

        private static string Repowurzel()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) d = d.Parent;
            Assert.NotNull(d);
            return d.FullName;
        }

        // =================================================================
        //  Eingebaute Typkennfelder
        // =================================================================

        [Fact]
        public void Die_Typkennfelder_decken_die_Arten_ab_und_bestehen_die_Pruefregeln()
        {
            IReadOnlyList<KaeltemaschinenTypkennfelder.Typkennfeld> t = KaeltemaschinenTypkennfelder.Lesen();
            Assert.InRange(t.Count, 30, 50);
            Assert.Equal(t.Count, t.Select(x => x.Bezeichner).Distinct().Count());
            Assert.Equal(t.Count, t.Select(x => KaeltemaschinenTypkennfelder.Schluessel(x.Bezeichner)).Distinct().Count());
            foreach (string art in KaeltemaschineSchema.RUECKKUEHLARTEN)
                Assert.Contains(t, x => x.Rueckkuehlart == art);
            foreach (string v in new[] { "scroll", "screw", "centrifugal", "reciprocating" })
                Assert.Contains(t, x => x.Kurven.Verdichter == v);

            foreach (KaeltemaschinenTypkennfelder.Typkennfeld x in t)
            {
                KaeltemaschineModel m = x.Modell();
                Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));
                Assert.StartsWith("Typkennfeld ", m.Bezeichner, StringComparison.Ordinal);
                Assert.Null(m.Firma);
                Assert.Equal(KaeltemaschinenTypkennfelder.TYP, m.Typ);
                Assert.Contains("PNNL Copper, BSD-2", m.Beschreibung, StringComparison.Ordinal);
                Assert.Equal(x.KlasseKw, m.Nennkaelteleistung_kW);
                Assert.InRange(m.Nenn_EER.Value, 2.0, 8.0);
                Assert.Equal(24, m.Kennlinie.Count);
                Assert.All(m.Kennlinie, p => Assert.True(p.EER > 0 && p.Kaelteleistung_kW > 0));
                // Die Simulation trifft den Nennwert: bilinear am Eurovent-Punkt der Rückkühlart.
                var k = new KaeltemaschinenKennlinie(m.Kennlinie.Select(p => (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER, p.Kaelteleistung_kW)));
                KaeltemaschinenPunkt n = k.Auswerten(KaeltemaschinenKennfeld.Nennrueckkuehltemperatur(m.Rueckkuehlart), 7.0);
                if (!n.Randwert) Assert.Equal(m.Nenn_EER.Value, n.Eer, 1);
            }
        }

        [Fact]
        public void Einspielen_ist_idempotent_und_stempelt_Schluessel_und_Pruefsumme()
        {
            if (!MitKatalog) return;
            int vorher = KaeltemaschineStammCtrl.Liste().Count;
            int anzahl = KaeltemaschinenTypkennfelder.Lesen().Count;

            KaeltemaschinenTypkennfelder.Einspielergebnis a = KaeltemaschinenTypkennfelder.Einspielen();
            Assert.True(a.Ok, a.Fehler);
            Assert.Equal(anzahl, a.Neu + a.Uebersprungen);
            Assert.Equal(vorher + a.Neu, KaeltemaschineStammCtrl.Liste().Count);
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3));

            KaeltemaschinenTypkennfelder.Einspielergebnis b = KaeltemaschinenTypkennfelder.Einspielen();
            Assert.True(b.Ok, b.Fehler);
            Assert.Equal(0, b.Neu);
            Assert.Equal(anzahl, b.Uebersprungen);

            KaeltemaschineStammCtrl.Listenzeile z = KaeltemaschineStammCtrl.Liste().Single(l => l.Bezeichner == "Typkennfeld Luft Scroll 50 kW");
            Assert.True(z.ReadOnly);
            Assert.Equal(KaeltemaschinenTypkennfelder.TYP, z.Typ);
            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(z.Id);
            Assert.Equal(24, m.Kennlinie.Count);
            Assert.Equal("KM:TYPKENNFELD_LUFT_SCROLL_50_KW",
                         Convert.ToString(DataRepository.ExecuteScalar(
                             "SELECT Katalog_Schluessel FROM Tab_Kaeltemaschine_STAMM WHERE ID = ?", new DbParam("?", z.Id)),
                             CultureInfo.InvariantCulture));
            // Ein ausgelieferter Satz wird nie überschrieben.
            Assert.False(KaeltemaschineStammCtrl.Speichern(m).Ok);
        }

        // =================================================================
        //  Profil und Importablauf
        // =================================================================

        [Fact]
        public void Das_Profil_der_Kaeltemaschine_steht_im_Register()
        {
            KatalogImportProfil p = KatalogImportProfil.Finde(KatalogImportArt.Kaeltemaschine);
            Assert.Contains(KatalogImportArt.Kaeltemaschine, KatalogImportProfil.AlleArten);
            Assert.NotNull(p.Katalog);
            Assert.Equal(KaeltemaschineSchema.TAB_STAMM, p.Katalog.Tabelle);
            Assert.Equal(KatalogImportProfil.KaeltemaschineFilter, p.Dateifilter);
            Assert.Equal("IMP_KAT_HINWEIS_KAELTEMASCHINE", p.Hinweis);
            Assert.Contains(p.Detailfelder, f => f.Schluessel == "RUECKKUEHLART");
        }

        [Fact]
        public void Der_Importablauf_liest_Copper_schreibt_und_ueberschreibt_keinen_gesperrten_Satz()
        {
            if (!MitKatalog) return;
            string pfad = Path.Combine(Path.GetTempPath(), "epos-km1-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(pfad, COPPER_PROBE);
            try
            {
                var ablauf = new KatalogImportAblauf(KatalogImportProfil.Finde(KatalogImportArt.Kaeltemaschine));
                Assert.Equal(2, ablauf.Lesen(pfad));
                Assert.Contains(ablauf.Meldungen, m => m.Schluessel == "KM_IMP_MSG_UEBERGANGEN");
                Assert.All(ablauf.Saetze, s => Assert.StartsWith("Kurvensatz ", s.Name, StringComparison.Ordinal));
                Assert.True(ablauf.Saetze[0].Filterwert > 0);

                var markiert = new List<int> { 0, 1 };
                List<ImportPruefung> pruef = ablauf.Vorpruefen(markiert);
                ImportBilanz bilanz = ablauf.Ausfuehren(2, KatalogImportAblauf.AllesImportieren(pruef));
                Assert.Equal(2, bilanz.Gespeichert);
                KaeltemaschineStammCtrl.Listenzeile z = KaeltemaschineStammCtrl.Liste().Single(l => l.Bezeichner == ablauf.Saetze[1].Name);
                Assert.False(z.ReadOnly);
                Assert.Equal(24, KaeltemaschineStammCtrl.Laden(z.Id).Kennlinie.Count);

                // Zweiter Anlauf: derselbe Name ist ein Duplikat; Überschreiben eines Anwendersatzes geht,
                // eines gesperrten nicht.
                var satz = (KaeltemaschineImportSatz)ablauf.Saetze[1];
                Assert.Equal(VdiUebernahmeErgebnis.Duplikat, satz.Anlegen(satz.Name));
                Assert.Equal(VdiUebernahmeErgebnis.Ueberschrieben, satz.Ueberschreiben(z.Id));
                KaeltemaschineStammCtrl.SchlossSetzen(new[] { z.Id }, true);
                Assert.Equal(VdiUebernahmeErgebnis.Duplikat, satz.Ueberschreiben(z.Id));
            }
            finally
            {
                File.Delete(pfad);
            }
        }
    }
}
