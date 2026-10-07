using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kältemaschine als Anlage im Rechenweg</b> (KU3-4; Kühlkonzept 5.3, 5.5, 6.1): Arbeitskopie von
    /// 1017. Eine Projektkopie ohne Anlagenzeile rechnet nicht und wird benannt; die Anlagenzeile trägt Anzahl,
    /// Kaltwasservorlauf, Hilfsstromanteil; das Ergebnis je Maschine wird gespeichert und gelesen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineAnlageDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

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
            return lauf;
        }

        [Fact]
        public void Eine_Projektkopie_ohne_Anlagenzeile_rechnet_nicht_und_wird_benannt()
        {
            if (!_db.Vorhanden) return;
            KaeltemaschineCtrl.AusKatalogUebernehmen(Stamm(LUFTGEKUEHLT), PROJEKT);
            SimulationRunner lauf = Rechnen();
            Kaeltekaskade k = lauf.simulation_Kaeltebedarf.Kaskade;
            Assert.All(k.Erzeuger, e => Assert.Null(e.Maschine));
            Assert.Contains(lauf.Protokoll.Hinweise, t => t.Contains(LUFTGEKUEHLT, StringComparison.Ordinal) &&
                                                          t.Contains("Anlagenzeile", StringComparison.Ordinal));
            Assert.Empty(new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen);
        }

        [Fact]
        public void Die_Anlagenzeile_traegt_Anzahl_Vorlauf_Hilfsstrom_und_das_Ergebnis_wird_gespeichert()
        {
            if (!_db.Vorhanden) return;
            // Die Wärmepumpe von 1017 an der Außenluft: Mit der gesäten Erdsonde deckte sie die Kälte fast ganz,
            // und die Maschine hinter ihr bekäme keinen Kältestrom für die Endenergiezeile.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET WQ_Typ = NULL WHERE ID = 10211");
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, Stamm(LUFTGEKUEHLT), "KM Halle");
            Assert.True(anlage > 0);
            KaeltemaschineAnlageModel a = KaeltemaschineAnlageCtrl.Laden(anlage);
            Assert.Equal(1, a.Anzahl);
            Assert.Equal(KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Type FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("?", anlage)), CultureInfo.InvariantCulture));
            // Die Nennleistung klein halten, damit zwei Maschinen sichtbar mehr decken als eine.
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_KENNDATEN + " SET " +
                KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " = " + KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " / 20 WHERE " +
                KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", a.IdKaeltemaschine.Value));
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_PROJEKT + " SET " +
                KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG + " = 2.5 WHERE ID = ?", new DbParam("?", a.IdKaeltemaschine.Value));

            Kaeltekaskade einzeln = Rechnen().simulation_Kaeltebedarf.Kaskade;
            Kaelteerzeuger e1 = einzeln.Erzeuger.Single(e => e.Maschine != null);
            Assert.Equal(anlage, e1.AnlagenID);
            Assert.Equal("KM Halle", e1.Bezeichner);
            Assert.Equal(6.0, e1.Maschine.Kaltwassertemperatur);   // kleinste Stützstelle
            Assert.True(e1.StundenLeistungsgrenze > 0, "Der Fall braucht eine Maschine an der Leistungsgrenze.");

            a.Anzahl = 2;
            a.KuehlVorlauf = 12;
            a.KuehlHilfsstromanteil = 0.05;
            Assert.Null(KaeltemaschineAnlageCtrl.Speichern(a));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KM_ANLAGE_ANZAHL_UNGUELTIG,
                         KaeltemaschineAnlageCtrl.Pruefen(new KaeltemaschineAnlageModel { Bezeichner = "x", Anzahl = 0 }));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KM_ANLAGE_HILFSSTROM_UNGUELTIG,
                         KaeltemaschineAnlageCtrl.Pruefen(new KaeltemaschineAnlageModel { Bezeichner = "x", KuehlHilfsstromanteil = 1 }));

            SimulationRunner lauf = Rechnen();
            Kaelteerzeuger e2 = lauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(e => e.Maschine != null);
            Assert.Equal(2, e2.Maschine.Anzahl);
            Assert.Equal(12.0, e2.Maschine.Kaltwassertemperatur);
            Assert.Equal(0.05, e2.Hilfsstromanteil);
            Assert.True(e2.KaelteGesamtKwh > e1.KaelteGesamtKwh);
            Assert.True(e2.HilfsstromGesamtKwh > 0);

            ErgebnisKaeltemaschineModel ek = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen);
            Assert.Equal(a.IdKaeltemaschine, ek.ID_Kaeltemaschine);
            Assert.Equal("KM Halle", ek.Bezeichner);
            Assert.Equal(2, ek.Anzahl);
            Assert.InRange(ek.Kaelteproduktion_MWh, e2.KaelteGesamtKwh / 1000.0 - 0.006, e2.KaelteGesamtKwh / 1000.0 + 0.006);
            Assert.InRange(ek.Hilfsstrom_MWh, e2.HilfsstromGesamtKwh / 1000.0 - 0.006, e2.HilfsstromGesamtKwh / 1000.0 + 0.006);
            Assert.Equal(e2.StundenLeistungsgrenze, ek.Stunden_Leistungsgrenze);
            Assert.InRange(ek.Unterdeckung_MWh, e2.OffenAnLeistungsgrenzeKwh / 1000.0 - 0.006, e2.OffenAnLeistungsgrenzeKwh / 1000.0 + 0.006);

            // Wirtschaftlichkeit (Kühlkonzept 6.2): Investitionsbezug = Nennkälteleistung x Anzahl, Endenergiezeile = Kältestrom.
            Assert.Equal(5.0, TechnikPlanwertCtrl.BaugroesseSumme(PROJEKT, KaeltemaschineAnlageSchema.KOMPONENTE_KAELTEMASCHINE,
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, anlage));
            EndenergieAufloeser.Groesse g = EndenergieAufloeser.FuerProjekt(PROJEKT)
                .FuerPosition(KaeltemaschineAnlageSchema.KOMPONENTE_KAELTEMASCHINE, anlage);
            Assert.NotNull(g);
            Assert.InRange(g.BedarfKwh, ek.Stromverbrauch_MWh * 1000.0 - 1e-6, ek.Stromverbrauch_MWh * 1000.0 + 1e-6);
            Assert.Null(EndenergieAufloeser.FuerProjekt(PROJEKT).FuerPosition(KaeltemaschineAnlageSchema.KOMPONENTE_KAELTEMASCHINE, 999999));
            Assert.False(Waermegestehung.AnlagentypZaehlt(KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE));
            Assert.Contains(DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE, KostenVorlagenCtrl.WaehlbareKomponenten);

            // Löschen: Anlagenzeile und Projektkopie verschwinden, das Ergebnis behält seine Zeile ohne Verweis.
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage));
            Assert.Null(KaeltemaschineCtrl.Laden(a.IdKaeltemaschine.Value));
        }
    }
}
