using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die HÜLLE der Energieträgerverwaltung gegen die Testdatenbank — die drei
    /// Befunde des Anwenders vom 14.09.2026.
    ///
    /// <para><b>B1 Einheit und Umrechnung.</b> Heizwert und Brennwert sind
    /// Stoffwerte je ABRECHNUNGSEINHEIT; nur der Arbeitspreis folgt der
    /// Preisbasis. Der Nachweis ist der Kreis öffnen–speichern–öffnen: Er muss
    /// Hi, Hs und den Arbeitspreis unverändert lassen. Vorher multiplizierte
    /// jedes Speichern alle drei erneut mit dem Faktor der Preisbasis.</para>
    ///
    /// <para><b>B2 Preishistorie.</b> <c>Stand.Historie</c> wurde nie befüllt —
    /// <c>EnergietraegerPreisCtrl.Historie</c> hatte in der ganzen Trägerkarte
    /// keinen Aufrufer, die Tabelle blieb leer. Sie wird jetzt beim
    /// Trägerwechsel und nach jedem Speichern gelesen, im Projekt- wie im
    /// Katalogkontext.</para>
    ///
    /// <para><b>B3 Katalogwerte übernehmen.</b> Eine einmalige Kopie der
    /// Katalogzeile in die Felder (Anwenderentscheid 14.09.2026: kein „dem
    /// Katalog folgen"); geschrieben wird erst mit „Speichern".</para>
    ///
    /// <para>Die Fälle arbeiten auf der ARBEITSKOPIE der Testdatenbank
    /// (<see cref="TestDatenbank"/>) — die Quelldatei ist die Messlatte der
    /// Referenzläufe und bleibt unberührt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerHuelleTests
    {
        /// <summary>Erdgas E (Brennstoff 3, Abrechnungseinheit Nm³, Hi 10,5 / Hs 11,6).</summary>
        private const int ERDGAS_E = 63;

        /// <summary>Ein Referenzprojekt, dem Erdgas E zugeordnet ist.</summary>
        private const int PROJEKT = 1030;

        // =================================================================
        // Hilfen
        // =================================================================

        private static EnergietraegerStand Karte(EnergietraegerHuelle h, int traegerId)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(traegerId);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            EnergietraegerAnsicht a = laden(traegerId);
            Assert.NotNull(a.Stand);
            return a.Stand;
        }

        /// <summary>Der Parametersatz EINER Hüllen-Instanz samt geladener Karte.</summary>
        private static IReadOnlyDictionary<string, object> Geladen(
            EnergietraegerHuelle h, int traegerId, out EnergietraegerStand stand)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(traegerId);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            EnergietraegerAnsicht a = laden(traegerId);
            Assert.NotNull(a.Stand);
            stand = a.Stand;
            return gaben;
        }

        private static bool Speichern(IReadOnlyDictionary<string, object> gaben)
        {
            return ((Func<bool>)gaben["Speichern"])();
        }

        private static Task PreisbasisSetzen(IReadOnlyDictionary<string, object> gaben, int index)
        {
            return ((EventCallback<int>)gaben["PreisbasisGewechselt"]).InvokeAsync(index);
        }

        private static Task KatalogUebernehmen(IReadOnlyDictionary<string, object> gaben)
        {
            return ((EventCallback)gaben["KatalogUebernehmen"]).InvokeAsync();
        }

        /// <summary>
        /// Eine Feldeingabe, wie die Oberfläche sie macht: Wert setzen, dann
        /// nachrechnen lassen (die Komponente ruft dafür <c>Geaendert</c>).
        /// Ohne das Nachrechnen bleibt der Basiswert stehen — und ohne
        /// geänderten Basiswert entsteht keine Historienzeile.
        /// </summary>
        private static void Feld(IReadOnlyDictionary<string, object> gaben, Action eingabe)
        {
            eingabe();
            ((Func<EnergietraegerAnsicht>)gaben["Nachrechnen"])();
        }

        private static int IndexDerEinheit(EnergietraegerStand stand, string einheit)
        {
            for (int i = 0; i < stand.Preisbasen.Count; i++)
                if (EnergietraegerPreisCtrl.EinheitSchluessel(stand.Preisbasen[i].Text)
                    == EnergietraegerPreisCtrl.EinheitSchluessel(einheit)) return i;
            return -1;
        }

        private static int HistorienZeilen(int traegerId, int projektId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_price WHERE carrier_id = ? AND id_projekt = ?",
                new DbParam("@c", traegerId), new DbParam("@p", projektId));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        // =================================================================
        // B1 - Einheiten und der Kreis oeffnen - speichern - oeffnen
        // =================================================================

        [Fact]
        public void Heizwert_und_Brennwert_tragen_die_Abrechnungseinheit()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand = Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);

            // Der gemeldete Fall: Hier stand „kWh/kWh".
            Assert.Equal("Nm³", stand.Basiseinheit);
            Assert.Equal("kWh/Nm³", stand.EinheitHeizwert);
            Assert.Equal("kWh/Nm³", stand.EinheitBrennwert);
            Assert.Equal("€/(kW·a)", stand.EinheitLeistungspreis);
            Assert.Equal(10.5, stand.Heizwert, 6);
            Assert.Equal(11.6, stand.Brennwert, 6);
        }

        [Fact]
        public async Task Die_Preisbasis_rechnet_nur_den_Arbeitspreis_um()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            int kwh = IndexDerEinheit(stand, "kWh");
            Assert.True(kwh >= 0, "Erdgas E muss kWh als Preisbasis anbieten.");

            double arbeitspreisVorher = stand.Arbeitspreis;
            double faktor = 0.5;   // Regel 67: Nm³ -> kWh in der Testdatenbank

            await PreisbasisSetzen(gaben, kwh);

            // Nur der Arbeitspreis folgt - Hi, Hs, Leistungs- und Grundpreis bleiben.
            Assert.Equal(arbeitspreisVorher / faktor, stand.Arbeitspreis, 6);
            Assert.Equal(10.5, stand.Heizwert, 6);
            Assert.Equal(11.6, stand.Brennwert, 6);

            // Die Einheit von Hi/Hs bleibt die Abrechnungseinheit; der
            // Arbeitspreis nennt jetzt die Preisbasis.
            Assert.Equal("kWh/Nm³", stand.EinheitHeizwert);
            Assert.Equal("€/kWh", stand.EinheitArbeitspreis);
        }

        [Fact]
        public async Task Oeffnen_Speichern_Oeffnen_laesst_Hi_Hs_und_Arbeitspreis_stehen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // (1) Öffnen, einen Arbeitspreis setzen und die Preisbasis auf kWh
            //     stellen - genau die Lage des Anwenderbefunds.
            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            int kwh = IndexDerEinheit(stand, "kWh");
            Assert.True(kwh >= 0);

            Feld(gaben, () => stand.Arbeitspreis = 0.84);   // €/Nm³, Preisbasis ist noch Nm³
            await PreisbasisSetzen(gaben, kwh);

            double anzeigeInKwh = stand.Arbeitspreis;
            Assert.True(Speichern(gaben));

            // (2) und (3): zweimal neu öffnen und speichern. Vorher wuchsen Hi,
            //     Hs und Arbeitspreis bei jedem Durchgang um den Faktor.
            for (int runde = 0; runde < 2; runde++)
            {
                EnergietraegerStand neu;
                IReadOnlyDictionary<string, object> g2 =
                    Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out neu);

                Assert.Equal(10.5, neu.Heizwert, 6);
                Assert.Equal(11.6, neu.Brennwert, 6);
                Assert.Equal("kWh/Nm³", neu.EinheitHeizwert);

                // Die gespeicherte Preisbasis steht wieder auf kWh, und der
                // Arbeitspreis wird in sie umgerechnet ANGEZEIGT.
                Assert.Equal(kwh, neu.PreisbasisId);
                Assert.Equal(anzeigeInKwh, neu.Arbeitspreis, 6);

                Assert.True(Speichern(g2));
            }

            // In der Datenbank steht der Basiswert je Abrechnungseinheit.
            EnergietraegerPreisCtrl.Projektpreis p =
                EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT, ERDGAS_E);
            Assert.NotNull(p);
            Assert.Equal(0.84, p.Arbeitspreis.Value, 4);
            Assert.Equal(10.5, p.Hi.Value, 4);
            Assert.Equal(11.6, p.Hs.Value, 4);
        }

        [Fact]
        public void Die_Formelzeile_nennt_die_Abrechnungseinheit()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            Feld(gaben, () => stand.Arbeitspreis = 0.84);

            // Gemeldet war „0,00 € ÷ 21,00 kWh = 0,0000 €/kWh" - ohne Einheiten
            // und mit dem verdoppelten Heizwert.
            Assert.Equal("0,84 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0800 €/kWh", stand.FormelText);
            Assert.Equal("0,0800 €", stand.PreisJeKwh);
            Assert.Equal("effektiv: 1 Nm³ = 10,50 kWh (Hi) / 11,60 kWh (Hs)", stand.EffektivText);
        }

        // =================================================================
        // B2 - Preishistorie
        // =================================================================

        [Fact]
        public void Die_Historie_steht_schon_beim_Oeffnen_da()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorhanden = HistorienZeilen(ERDGAS_E, PROJEKT);
            Assert.True(vorhanden > 0, "Die Testdatenbank führt für diesen Fall eine Historienzeile.");

            EnergietraegerStand stand = Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);

            // Gemeldet war: „die Tabelle bleibt leer".
            Assert.Equal(vorhanden, stand.Historie.Count);
            Assert.All(stand.Historie, z => Assert.NotEqual("", z.GueltigAb));
        }

        [Fact]
        public void Speichern_im_Projekt_schreibt_eine_Zeile_zum_Datum_aus_Gueltig_ab()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = HistorienZeilen(ERDGAS_E, PROJEKT);

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            var tag = new DateOnly(2026, 9, 14);
            stand.GueltigAb = tag;
            Feld(gaben, () => stand.Arbeitspreis = 0.91);
            Assert.True(Speichern(gaben));

            Assert.Equal(vorher + 1, HistorienZeilen(ERDGAS_E, PROJEKT));
            Assert.Equal(vorher + 1, stand.Historie.Count);

            // Das Datum kommt aus dem Feld, nicht aus der Uhr.
            object gespeichert = DataRepository.ExecuteScalar(
                "SELECT arbeitspreis FROM energy_price WHERE carrier_id = ? AND id_projekt = ? " +
                "AND valid_from = ?",
                new DbParam("@c", ERDGAS_E), new DbParam("@p", PROJEKT),
                new DbParam("@d", DbParamTyp.Date) { Wert = tag.ToDateTime(TimeOnly.MinValue) });
            Assert.NotNull(gespeichert);
            Assert.Equal(0.91, Convert.ToDouble(gespeichert), 4);
        }

        [Fact]
        public void Ein_zweites_Speichern_am_selben_Tag_aktualisiert_statt_zu_doppeln()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            stand.GueltigAb = new DateOnly(2026, 9, 14);
            Feld(gaben, () => stand.Arbeitspreis = 0.91);
            Assert.True(Speichern(gaben));
            int nachErstem = HistorienZeilen(ERDGAS_E, PROJEKT);

            Feld(gaben, () => stand.Arbeitspreis = 0.95);
            Assert.True(Speichern(gaben));

            Assert.Equal(nachErstem, HistorienZeilen(ERDGAS_E, PROJEKT));
            Assert.Equal(nachErstem, stand.Historie.Count);
        }

        /// <summary>
        /// Der Katalogkontext bekommt KEINE Historienzeile — und sagt warum.
        ///
        /// <para><c>energy_price.ID_Projekt</c> trägt einen Fremdschlüssel auf
        /// <c>Tab_Projekt.ID</c>, und das Projekt 0 gibt es nicht; ein
        /// Schreibversuch endete in einem Fremdschlüsselfehler statt in einer
        /// Zeile. Die Karte nennt den Grund unter der Tabelle, statt ihn still
        /// zu übergehen. Geschrieben wird im Katalog die Katalogzeile selbst.</para>
        /// </summary>
        [Fact]
        public void Der_Katalogkontext_nennt_statt_stumm_keine_Zeile_zu_schreiben()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = HistorienZeilen(ERDGAS_E, 0);

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(0), ERDGAS_E, out stand);

            Assert.Contains("je Projekt", stand.HistorieHinweis);

            stand.GueltigAb = new DateOnly(2026, 9, 14);
            Feld(gaben, () => stand.Arbeitspreis = 0.77);
            Assert.True(Speichern(gaben));

            Assert.Equal(vorher, HistorienZeilen(ERDGAS_E, 0));

            // Die Katalogzeile selbst trägt den neuen Preis.
            Assert.Equal(0.77, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT price_work FROM energy_carrier WHERE id = ?",
                new DbParam("@id", ERDGAS_E))), 4);
        }

        [Fact]
        public void Im_Projektkontext_steht_kein_Katalog_Hinweis()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal("", Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E).HistorieHinweis);
        }

        // =================================================================
        // B3 - Katalogwerte uebernehmen
        // =================================================================

        [Fact]
        public void Der_Uebernahmeknopf_steht_nur_im_Projektkontext()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E).MitKatalogUebernahme);
            Assert.False(Karte(new EnergietraegerHuelle(0), ERDGAS_E).MitKatalogUebernahme);
        }

        [Fact]
        public async Task Katalogwerte_uebernehmen_fuellt_die_Felder_und_schreibt_erst_mit_Speichern()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Katalog bekommt einen unverwechselbaren Arbeitspreis.
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_work = ?, price_base = ? WHERE id = ?",
                new DbParam("@w", 1.23), new DbParam("@b", 45.0), new DbParam("@id", ERDGAS_E));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            Feld(gaben, () => { stand.Arbeitspreis = 0.11; stand.Grundpreis = 0.0; });

            await KatalogUebernehmen(gaben);

            // Die Felder tragen die Katalogwerte, die Preisbasis steht auf der
            // Abrechnungseinheit, und der Hinweis sagt: noch nicht geschrieben.
            Assert.Equal(1.23, stand.Arbeitspreis, 6);
            Assert.Equal(45.0, stand.Grundpreis, 6);
            Assert.Equal(10.5, stand.Heizwert, 6);
            Assert.Equal(IndexDerEinheit(stand, "Nm³"), stand.PreisbasisId);
            Assert.Contains("noch nicht gespeichert", stand.UebernahmeHinweis);

            // Noch steht in der Projektzeile der alte Wert.
            EnergietraegerPreisCtrl.Projektpreis vorher =
                EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT, ERDGAS_E);
            Assert.NotEqual(1.23, vorher.Arbeitspreis ?? 0.0, 4);

            Assert.True(Speichern(gaben));

            EnergietraegerPreisCtrl.Projektpreis nachher =
                EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT, ERDGAS_E);
            Assert.Equal(1.23, nachher.Arbeitspreis.Value, 4);
            Assert.Equal("", stand.UebernahmeHinweis);
        }

        [Fact]
        public async Task Die_Uebernahme_ist_eine_Kopie_und_kein_Folgen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_work = ? WHERE id = ?",
                new DbParam("@w", 1.50), new DbParam("@id", ERDGAS_E));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            await KatalogUebernehmen(gaben);
            Assert.True(Speichern(gaben));

            // Der Katalog wandert weiter - das Projekt bleibt, wo es ist
            // (Anwenderentscheid 14.09.2026: einmalige Kopie).
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_work = ? WHERE id = ?",
                new DbParam("@w", 9.99), new DbParam("@id", ERDGAS_E));

            EnergietraegerStand neu = Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);
            Assert.Equal(1.50, neu.Arbeitspreis, 4);
        }
    }
}
