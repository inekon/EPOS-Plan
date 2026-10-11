using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Datenbankfälle der Kältemaschine</b> (KU3-2; Kühlkonzept 10.3): eine Arbeitskopie von 1017 (die
    /// Vorrichtung rechnet auf einer Kopie der Testdatenbank, nichts bleibt) mit zusätzlicher Kältemaschine
    /// aus der Saat. Die Wärmepumpe im Kühlbetrieb deckt zuerst und unverändert, die Kältemaschine danach;
    /// der Kältestrom wächst, der ungedeckte Rest sinkt. Eine zu kleine Maschine meldet die Unterdeckung
    /// mit Menge und Grund.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaeltemaschineDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        private static int Stamm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", bezeichner)),
            CultureInfo.InvariantCulture);

        /// <summary>
        /// Legt die Maschine als Anlage an (KU3-4: Anlagenzeile mit Typ 13 und Projektkopie) und liefert die ID
        /// der Projektkopie.
        /// </summary>
        internal static int Anlegen(string bezeichner)
        {
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, Stamm(bezeichner), null);
            Assert.True(anlage > 0);
            return KaeltemaschineAnlageCtrl.Laden(anlage).IdKaeltemaschine.Value;
        }

        private static SimulationRunner Rechnen()
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));
            return lauf;
        }

        [Fact]
        public void Waermepumpe_vor_Kaeltemaschine_und_der_Rest_sinkt()
        {
            if (!_db.Vorhanden) return;
            // Fallbildung: Mit der Zonensperre deckt die Wärmepumpe von 1017 die Kälte ganz (Basis R43); die
            // Unterdeckung kommt aus ihrer geminderten Leistungsgrenze.
            KaelteUnterdeckung.WaermepumpeMindern(PROJEKT);

            Kaeltekaskade ohne = Rechnen().simulation_Kaeltebedarf.Kaskade;
            Assert.NotNull(ohne);
            Kaelteerzeuger wpOhne = Assert.Single(ohne.Erzeuger);
            Assert.True(ohne.RestGesamtKwh > 0, "1017 braucht ohne Kältemaschine eine Unterdeckung, sonst prüft der Fall nichts.");
            double[] wpKaelteOhne = (double[])wpOhne.Kaelte_stuendlich.Clone();

            int id = Anlegen(LUFTGEKUEHLT);
            Assert.True(id > 0);

            SimulationRunner lauf = Rechnen();
            Kaeltekaskade mit = lauf.simulation_Kaeltebedarf.Kaskade;
            Assert.Equal(2, mit.Erzeuger.Count);
            Kaelteerzeuger wp = mit.Erzeuger[0], km = mit.Erzeuger[1];
            Assert.Null(wp.Maschine);
            Assert.NotNull(km.Maschine);
            Assert.Equal(id, km.Maschine.Id);
            Assert.Equal(-1, km.Modulindex);

            // Reihenfolge: Die Wärmepumpe deckt Stunde für Stunde genau wie ohne Kältemaschine.
            for (int h = 0; h < Kaeltekaskade.STUNDEN; h++)
                Assert.Equal(wpKaelteOhne[h], wp.Kaelte_stuendlich[h]);

            Assert.True(km.KaelteGesamtKwh > 0);
            Assert.True(km.StromGesamtKwh > 0);
            Assert.True(mit.RestGesamtKwh < ohne.RestGesamtKwh);
            Assert.True(mit.StromGesamtKwh > ohne.StromGesamtKwh);
            Assert.Equal(0, km.StundenFreieKuehlung);   // luftgekühlt
            // 50 kW luftgekühlt reicht für 1017: kein Rest mehr.
            Assert.True(mit.RestGesamtKwh < 1e-6, "Rest " + mit.RestGesamtKwh.ToString("F3", CultureInfo.InvariantCulture));
            Assert.Equal(lauf.simulation_Kaeltebedarf.Kaelterestbedarf, mit.RestGesamtKwh / 1000.0, 9);
            Assert.Contains(lauf.Protokoll.Hinweise, t => t.Contains(LUFTGEKUEHLT, StringComparison.Ordinal));

            // Gespeichert: Kälte und Kältestrom der Wärmepumpenzeile tragen allein die Wärmepumpe.
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.InRange(erg.Waermepumpe.Kaelteproduktion_WP.Value, wp.KaelteGesamtKwh / 1000.0 - 0.006, wp.KaelteGesamtKwh / 1000.0 + 0.006);
            Assert.InRange(erg.Waermepumpe.Stromverbrauch_Kuehlung.Value, wp.StromGesamtKwh / 1000.0 - 0.006, wp.StromGesamtKwh / 1000.0 + 0.006);
            // KU3-4: das Ergebnis je Maschine steht in Tab_ErgebnisKaeltemaschine.
            ErgebnisKaeltemaschineModel ek = Assert.Single(erg.Kaeltemaschinen);
            Assert.Equal(id, ek.ID_Kaeltemaschine);
            Assert.Equal(LUFTGEKUEHLT, ek.Bezeichner);
            Assert.InRange(ek.Kaelteproduktion_MWh, km.KaelteGesamtKwh / 1000.0 - 0.006, km.KaelteGesamtKwh / 1000.0 + 0.006);
            Assert.InRange(ek.Stromverbrauch_MWh, km.StromGesamtKwh / 1000.0 - 0.006, km.StromGesamtKwh / 1000.0 + 0.006);
            Assert.Equal(km.Taktstunden, ek.Taktstunden);

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1017 + {0}: Kältebedarf {1:F3} MWh/a; WP {2:F3}, Kältemaschine {3:F3} MWh/a; Kältestrom {4:F3} -> {5:F3} MWh/a; " +
                "Rest {6:F3} -> {7:F3} MWh/a; Takt {8} h, Randwert {9} h",
                LUFTGEKUEHLT, mit.BedarfGesamtKwh / 1000.0, wp.KaelteGesamtKwh / 1000.0, km.KaelteGesamtKwh / 1000.0,
                ohne.StromGesamtKwh / 1000.0, mit.StromGesamtKwh / 1000.0, ohne.RestGesamtKwh / 1000.0,
                mit.RestGesamtKwh / 1000.0, km.Taktstunden, km.StundenRandwert));
        }

        [Fact]
        public void Zu_kleine_Maschine_meldet_die_Unterdeckung_mit_Grund()
        {
            if (!_db.Vorhanden) return;
            // Fallbildung: Mit der Zonensperre deckt die Wärmepumpe von 1017 die Kälte ganz (Basis R43); die
            // Unterdeckung kommt aus ihrer geminderten Leistungsgrenze.
            KaelteUnterdeckung.WaermepumpeMindern(PROJEKT);

            int id = Anlegen(LUFTGEKUEHLT);
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_KENNDATEN + " SET " +
                KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " = 0.2 WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?",
                new DbParam("?", id));
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_PROJEKT + " SET " +
                KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG + " = 0.2 WHERE ID = ?", new DbParam("?", id));

            SimulationRunner lauf = Rechnen();
            Kaeltekaskade k = lauf.simulation_Kaeltebedarf.Kaskade;
            Kaelteerzeuger km = k.Erzeuger.Single(e => e.Maschine != null);
            Assert.True(km.StundenLeistungsgrenze > 0);
            Assert.True(km.OffenAnLeistungsgrenzeKwh > 0);
            Assert.True(k.RestGesamtKwh > 0);
            Assert.Contains(lauf.Protokoll.Warnungen, t => t.Contains(LUFTGEKUEHLT, StringComparison.Ordinal) &&
                                                           t.Contains(km.StundenLeistungsgrenze.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal));
        }

        /// <summary>
        /// Ohne Wärmepumpe in der Kaskade (alle Plätze der Wärmepumpe geleert) rechnet die Kältemaschine
        /// allein — am Ende der Wärmekaskade, vor der Stufenrechnung des Stroms.
        /// </summary>
        [Fact]
        public void Ohne_Waermepumpe_rechnet_die_Kaeltemaschine_allein()
        {
            if (!_db.Vorhanden) return;

            Anlegen(LUFTGEKUEHLT);
            foreach (string tool in new[] { "Tool_1", "Tool_2", "Tool_3", "Tool_4" })
                DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET " + tool + " = '' WHERE ID_Projekt = ? AND " + tool + " = ?",
                                               new DbParam("?", PROJEKT), new DbParam("?", DbWerte.ERZEUGER_WAERMEPUMPE));

            SimulationRunner lauf = Rechnen();
            Assert.False(lauf.sim.WPInSpeicherstufe);
            Kaeltekaskade k = lauf.simulation_Kaeltebedarf.Kaskade;
            Assert.NotNull(k);
            Kaelteerzeuger km = Assert.Single(k.Erzeuger);
            Assert.NotNull(km.Maschine);
            Assert.True(km.KaelteGesamtKwh > 0);
            Assert.True(k.StromGesamtKwh > 0);
            Assert.True(k.RestGesamtKwh < 1e-6);
            Assert.Equal(0.0, lauf.simulation_Kaeltebedarf.Kaelterestbedarf, 9);
        }

        [Fact]
        public void Ohne_Kuehlung_im_Projekt_rechnet_die_Maschine_nicht()
        {
            if (!_db.Vorhanden) return;

            Anlegen(LUFTGEKUEHLT);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Kuehlbetrieb = 0 WHERE ID_Projekt = ?",
                                           new DbParam("?", PROJEKT));

            SimulationRunner lauf = Rechnen();
            Kaeltekaskade k = lauf.simulation_Kaeltebedarf?.Kaskade;
            Assert.True(k == null || k.Erzeuger.All(e => e.Maschine == null));
            Assert.Contains(lauf.Protokoll.Hinweise, t => t.Contains(LUFTGEKUEHLT, StringComparison.Ordinal));
        }
    

        // =============================================================================
        //  Teillast und Takten (KM3-E1-a, Schritt 210): Lesen und Speichern der acht Felder, ohne Rechenwirkung
        // =============================================================================

        private static double? Z(string text)
        {
            Assert.True(KaeltemaschineStammCtrl.ZahlLesen(text, out double? w), text);
            return w;
        }

        private static KaeltemaschineModel Teillastsatz(string name) => new KaeltemaschineModel
        {
            Bezeichner = name,
            Nennkaelteleistung_kW = 20,
            Nenn_EER = 4,
            Mindestteillast_Prozent = 20,
            Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_LUFT,
            Teillast_Weg = KaeltemaschineTeillastSchema.WEG_KURVE,
            Teillastkurve_a = Z("0,10"),
            Teillastkurve_b = Z("0.60"),
            Teillastkurve_c = Z(" 0,30 "),
            Teillastkurve_Lastgrad_Min = Z("0.2"),
            Taktverlustfaktor_Cd = Z("0,9"),
            Verdichterregelung = KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL,
            Kennfeld_Randweg = KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD,
        };

        [Fact]
        public void Teillastfelder_laufen_rund_durch_Katalog_und_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            KaeltemaschineStammCtrl.SpeicherErgebnis s = KaeltemaschineStammCtrl.Speichern(Teillastsatz("Teillast Rundlauf"));
            Assert.True(s.Ok, s.Meldung);
            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(s.Id);
            Assert.Equal("KURVE", m.Teillast_Weg);
            Assert.Equal(0.10, m.Teillastkurve_a);
            Assert.Equal(0.60, m.Teillastkurve_b);
            Assert.Equal(0.30, m.Teillastkurve_c);
            Assert.Equal(0.2, m.Teillastkurve_Lastgrad_Min);
            Assert.Equal(0.9, m.Taktverlustfaktor_Cd);
            Assert.Equal("DREHZAHL", m.Verdichterregelung);
            Assert.Equal("GUETEGRAD", m.Kennfeld_Randweg);

            // Ueberschreiben mit leeren Feldern: NULL, nie 0.
            m.Teillast_Weg = null; m.Teillastkurve_a = Z(""); m.Teillastkurve_b = Z("  "); m.Teillastkurve_c = Z(null);
            m.Teillastkurve_Lastgrad_Min = null; m.Taktverlustfaktor_Cd = null; m.Verdichterregelung = null; m.Kennfeld_Randweg = null;
            Assert.True(KaeltemaschineStammCtrl.Speichern(m).Ok);
            foreach (string sp in KaeltemaschineTeillastSchema.EINGABESPALTEN)
                Assert.True(DataRepository.ExecuteScalar(
                    "SELECT \"" + sp + "\" FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE ID = ?", new DbParam("?", s.Id)) is null or DBNull, sp);

            // Die Projektkopie nimmt die acht Felder mit.
            KaeltemaschineStammCtrl.SpeicherErgebnis s2 = KaeltemaschineStammCtrl.Speichern(Teillastsatz("Teillast Kopie"));
            Assert.True(s2.Ok, s2.Meldung);
            int kopie = KaeltemaschineCtrl.AusKatalogUebernehmen(s2.Id, PROJEKT);
            KaeltemaschineModel k = KaeltemaschineCtrl.Laden(kopie);
            Assert.Equal(s2.Id, k.IdStamm);
            Assert.Equal("KURVE", k.Teillast_Weg);
            Assert.Equal(0.30, k.Teillastkurve_c);
            Assert.Equal(0.9, k.Taktverlustfaktor_Cd);
            Assert.Equal("DREHZAHL", k.Verdichterregelung);
            Assert.Equal("GUETEGRAD", k.Kennfeld_Randweg);
        }

        [Fact]
        public void Zahlen_mit_Komma_und_Punkt_leer_ist_null_Unsinn_wird_abgewiesen()
        {
            Assert.Equal(0.25, Z("0,25"));
            Assert.Equal(0.25, Z("0.25"));
            Assert.Equal(-1.5, Z("-1,5"));
            Assert.Null(Z(""));
            Assert.Null(Z(null));
            Assert.False(KaeltemaschineStammCtrl.ZahlLesen("abc", out _));
            Assert.False(KaeltemaschineStammCtrl.ZahlLesen("NaN", out _));
        }

        [Fact]
        public void Pruefen_weist_unzulaessige_Teillastfelder_benannt_ab()
        {
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(Teillastsatz("ok")));
            // Linear und ohne jede Eingabe: gueltig.
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(new KaeltemaschineModel { Bezeichner = "leer" }));

            KaeltemaschineModel w = Teillastsatz("x"); w.Teillast_Weg = "STUFEN";
            Assert.Equal(R.KM_MSG_TEILLASTWEG_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(w));
            w = Teillastsatz("x"); w.Verdichterregelung = "KURVE";
            Assert.Equal(R.KM_MSG_VERDICHTERREGELUNG_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(w));
            w = Teillastsatz("x"); w.Kennfeld_Randweg = "randwert";
            Assert.Equal(R.KM_MSG_RANDWEG_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(w));
            w = Teillastsatz("x"); w.Taktverlustfaktor_Cd = 1.2;
            Assert.Equal(R.KM_MSG_TEILLASTANTEIL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(w));
            w = Teillastsatz("x"); w.Teillastkurve_Lastgrad_Min = -0.1;
            Assert.Equal(R.KM_MSG_TEILLASTANTEIL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(w));
            w = Teillastsatz("x"); w.Teillastkurve_c = null;
            Assert.Equal(R.KM_MSG_TEILLASTKURVE_BEIWERTE, KaeltemaschineStammCtrl.Pruefen(w));
            w = Teillastsatz("x"); w.Teillastkurve_a = 2.5;
            Assert.Equal(R.KM_MSG_TEILLASTKURVE_BEIWERTE, KaeltemaschineStammCtrl.Pruefen(w));
            // EIRFPLR(1) = 1,3 liegt ausserhalb 0,9 ... 1,1.
            w = Teillastsatz("x"); w.Teillastkurve_c = 0.6;
            Assert.Equal(R.KM_MSG_TEILLASTKURVE_UNPLAUSIBEL, KaeltemaschineStammCtrl.Pruefen(w));
            // E(x) <= 0 im Gueltigkeitsbereich: a = -0,5, b = 0,5, c = 1 (EIRFPLR(1) = 1, E(0,2) < 0).
            w = Teillastsatz("x"); w.Teillastkurve_a = -0.5; w.Teillastkurve_b = 0.5; w.Teillastkurve_c = 1.0;
            Assert.Equal(R.KM_MSG_TEILLASTKURVE_UNPLAUSIBEL, KaeltemaschineStammCtrl.Pruefen(w));
            // g(x) > 2 bei kleiner Last: a = 0, b = 0, c = 1 (E(x) = x²).
            w = Teillastsatz("x"); w.Teillastkurve_a = 0; w.Teillastkurve_b = 0; w.Teillastkurve_c = 1.0;
            Assert.Equal(R.KM_MSG_TEILLASTKURVE_UNPLAUSIBEL, KaeltemaschineStammCtrl.Pruefen(w));
            // Das Zahlenbeispiel des Fachkonzepts 3.2 besteht (x_u 0,2).
            Assert.True(KaeltemaschineStammCtrl.KurvePlausibel(0.10, 0.60, 0.30, 0.2));
            // Starke Teillastabwertung: g(0,1) = 0,425 liegt zwischen 0,3 und 0,5 und ist plausibel; g(0,1) = 0,24 nicht.
            Assert.True(KaeltemaschineStammCtrl.KurvePlausibel(0.20, 0.30, 0.50, 0.1));
            Assert.False(KaeltemaschineStammCtrl.KurvePlausibel(0.40, 0.10, 0.50, 0.1));
        }

        [Fact]
        public void Ein_CHECK_Verstoss_wird_beim_Speichern_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            KaeltemaschineModel w = Teillastsatz("Teillast falsch");
            w.Verdichterregelung = "TURBO";
            KaeltemaschineStammCtrl.SpeicherErgebnis s = KaeltemaschineStammCtrl.Speichern(w);
            Assert.False(s.Ok);
            Assert.Equal(R.KM_MSG_VERDICHTERREGELUNG_UNGUELTIG, s.Meldung);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", "Teillast falsch")),
                CultureInfo.InvariantCulture));
            // Und die Datenbank selbst haelt die Klausel, falls ein Schreiber an der Pruefung vorbeigeht.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.ThrowsAny<Exception>(() => v.Ausfuehren(
                    "UPDATE " + KaeltemaschineSchema.TAB_STAMM + " SET Kennfeld_Randweg = ?", new DbParam("?", "KANTE")));
                v.Rollback();
            }
        }
}
}
