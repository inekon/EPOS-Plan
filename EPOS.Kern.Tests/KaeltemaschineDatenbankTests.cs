using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

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

            Kaeltekaskade ohne = Rechnen().simulation_Kaeltebedarf.Kaskade;
            Assert.NotNull(ohne);
            Kaelteerzeuger wpOhne = Assert.Single(ohne.Erzeuger);
            Assert.True(ohne.RestGesamtKwh > 0, "1017 braucht ohne Kältemaschine eine Unterdeckung, sonst prüft der Fall nichts.");
            double[] wpKaelteOhne = (double[])wpOhne.Kaelte_stuendlich.Clone();

            int id = KaeltemaschineCtrl.AusKatalogUebernehmen(Stamm(LUFTGEKUEHLT), PROJEKT);
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

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1017 + {0}: Kältebedarf {1:F3} MWh/a; WP {2:F3}, Kältemaschine {3:F3} MWh/a; Kältestrom {4:F3} -> {5:F3} MWh/a; " +
                "Rest {6:F3} -> {7:F3} MWh/a; Takt {8} h, Randwert {9} h",
                LUFTGEKUEHLT, mit.BedarfGesamtKwh / 1000.0, wp.KaelteGesamtKwh / 1000.0, km.KaelteGesamtKwh / 1000.0,
                ohne.StromGesamtKwh / 1000.0, mit.StromGesamtKwh / 1000.0, ohne.RestGesamtKwh / 1000.0,
                mit.RestGesamtKwh / 1000.0, km.StundenTakt, km.StundenRandwert));
        }

        [Fact]
        public void Zu_kleine_Maschine_meldet_die_Unterdeckung_mit_Grund()
        {
            if (!_db.Vorhanden) return;

            int id = KaeltemaschineCtrl.AusKatalogUebernehmen(Stamm(LUFTGEKUEHLT), PROJEKT);
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

        [Fact]
        public void Ohne_Kuehlung_im_Projekt_rechnet_die_Maschine_nicht()
        {
            if (!_db.Vorhanden) return;

            KaeltemaschineCtrl.AusKatalogUebernehmen(Stamm(LUFTGEKUEHLT), PROJEKT);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Kuehlbetrieb = 0 WHERE ID_Projekt = ?",
                                           new DbParam("?", PROJEKT));

            SimulationRunner lauf = Rechnen();
            Kaeltekaskade k = lauf.simulation_Kaeltebedarf?.Kaskade;
            Assert.True(k == null || k.Erzeuger.All(e => e.Maschine == null));
            Assert.Contains(lauf.Protokoll.Hinweise, t => t.Contains(LUFTGEKUEHLT, StringComparison.Ordinal));
        }
    }
}
