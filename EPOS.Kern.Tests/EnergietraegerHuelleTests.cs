using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        /// <summary>
        /// Der Listenplatz einer Preisbasis. Die Klappliste nennt die Einheit des
        /// ARBEITSPREISES („€/Nm³", „€/kWh" — ET-D-4); gefragt wird mit der
        /// Mengeneinheit.
        /// </summary>
        private static int IndexDerEinheit(EnergietraegerStand stand, string einheit)
        {
            string gesucht = EnergietraegerPreisCtrl.EinheitSchluessel(
                EnergietraegerPreiskarte.ArbeitspreisEinheit(einheit, true));
            for (int i = 0; i < stand.Preisbasen.Count; i++)
                if (EnergietraegerPreisCtrl.EinheitSchluessel(stand.Preisbasen[i].Text) == gesucht)
                    return i;
            return -1;
        }

        private static int HistorienZeilen(int traegerId, int projektId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_price WHERE carrier_id = ? AND id_projekt = ?",
                new DbParam("@c", traegerId), new DbParam("@p", projektId));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Die Ansicht EINER Hüllen-Instanz samt geladener Karte.</summary>
        private static EnergietraegerAnsicht Ansicht(EnergietraegerHuelle h, int traegerId)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(traegerId);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            return laden(traegerId);
        }

        // =================================================================
        // ET-D: Die Anzeigekante der Preisbestandteile
        // =================================================================

        /// <summary>
        /// <b>ET-D-1 (a).</b> Die Hülle nennt die Einheit AM WERT: Ein Gasträger
        /// pflegt seine Bestandteile in €/Nm³, und der Heizwert ist der Faktor,
        /// mit dem die Karte umrechnet. Gerechnet wird weiterhin in ct/kWh.
        /// </summary>
        [Fact]
        public void Die_Bestandteile_kommen_in_der_Abrechnungseinheit_herein()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerAnsicht a = Ansicht(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);

            Assert.NotNull(a.Stand.Bestandteile);
            Assert.Equal("€/Nm³", a.BestandteilEinheit);
            Assert.Equal(10.5, a.BestandteilHeizwert, 9);
            Assert.Equal("", a.BestandteilHinweis);
        }

        /// <summary>
        /// Die Kohärenzzeile ist eine PRÜFUNG: Sie steht auf „abweichend", sobald
        /// die Summe der Anteile den Arbeitspreis um mehr als 0,0001 €/kWh
        /// verfehlt. Bis ET-D stand sie ausnahmslos auf „✓".
        /// </summary>
        [Fact]
        public void Die_Kohaerenzzeile_meldet_eine_Abweichung()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);
            Assert.NotNull(stand.Bestandteile);

            // Ein Arbeitspreis und EIN Anteil, der ihn nicht deckt.
            Feld(gaben, () => stand.Arbeitspreis = 0.7560);
            stand.Bestandteile.Energiesteuer = 0.6076;
            stand.Bestandteile.EnergiesteuerAktiv = true;
            stand.Bestandteile.CO2 = null; stand.Bestandteile.CO2Aktiv = false;
            stand.Bestandteile.Netzentgelt = null; stand.Bestandteile.NetzentgeltAktiv = false;
            stand.Bestandteile.Vertrieb = null; stand.Bestandteile.VertriebAktiv = false;

            EnergietraegerAnsicht a = ((Func<EnergietraegerAnsicht>)gaben["Nachrechnen"])();

            Assert.True(a.BestandteilAnzeige.Abweichend);
            Assert.Contains("weicht um", a.BestandteilAnzeige.KohaerenzText);
            Assert.Contains("€/Nm³", a.BestandteilAnzeige.KohaerenzText);

            // Der Rest wird vorgeschlagen, nicht geschrieben.
            Assert.NotNull(a.VertriebVorschlag);
            Assert.Null(stand.Bestandteile.Vertrieb);

            // Deckt der Anteil den Preis, steht die Zeile auf „deckungsgleich".
            stand.Bestandteile.Energiesteuer = a.ArbeitspreisCtKwh;
            a = ((Func<EnergietraegerAnsicht>)gaben["Nachrechnen"])();

            Assert.False(a.BestandteilAnzeige.Abweichend);
            Assert.Contains("deckungsgleich", a.BestandteilAnzeige.KohaerenzText);
        }

        /// <summary>
        /// Die Herleitung des BEHG-Anteils nennt Preis und CO₂-Masse JE
        /// ABRECHNUNGSEINHEIT — „… €/t × 2,109 kg/Nm³" bei EF 200,9 g/kWh und
        /// Hi 10,5.
        /// </summary>
        [Fact]
        public void Die_BEHG_Herleitung_nennt_die_CO2_Masse_je_Abrechnungseinheit()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerAnsicht a = Ansicht(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);

            Assert.False(string.IsNullOrEmpty(a.HerleitungCo2));
            Assert.Contains("€/t ×", a.HerleitungCo2);
            Assert.Contains("kg/Nm³", a.HerleitungCo2);
        }

        /// <summary>
        /// ET-D: Die Herleitungszeile des Blocks „Preis und Heizwert" nennt den
        /// Preis je kWh und den Umrechnungsfaktor Hs/Hi = 11,6 / 10,5 = 1,1048.
        /// </summary>
        [Fact]
        public void Die_Herleitungszeile_nennt_den_Faktor_Hs_durch_Hi()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand = Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);

            Assert.StartsWith("→", stand.HerleitungPreis);
            Assert.Contains("/kWh", stand.HerleitungPreis);
            Assert.Contains("Hs/Hi = 1,1048", stand.HerleitungPreis);
        }

        /// <summary>
        /// ET-D-2: Die Bilanzierungsmethode ist Projektsache; im Katalogkontext
        /// steht sie gesperrt, und die Fußnote sagt, welche Größe gezeigt wird.
        /// </summary>
        [Fact]
        public void Die_Bilanzierungsmethode_ist_im_Katalog_nur_lesbar()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand projekt = Karte(new EnergietraegerHuelle(PROJEKT), ERDGAS_E);
            Assert.False(projekt.ModusNurLesend);
            Assert.Equal("", projekt.ModusHinweis);
            Assert.Contains("CO₂", projekt.EmissionsFussnote);

            EnergietraegerStand katalog = Karte(new EnergietraegerHuelle(0), ERDGAS_E);
            Assert.True(katalog.ModusNurLesend);
            Assert.Contains("Projektvorgabe", katalog.ModusHinweis);
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

        /// <summary>
        /// <b>UR-1 (Anwenderfoto 18.09.2026).</b> Der Wechsel auf die Preisbasis
        /// „kWh" misst gegen den HEIZWERT. Bis ET-D stand hier der Faktor der
        /// Regel 67 (<c>Nm³ → kWh</c>, <b>0,5</b> in der Testdatenbank) — dieser
        /// Fall hat den Fehler als Zusicherung geführt.
        /// </summary>
        [Fact]
        public async Task Die_Preisbasis_rechnet_den_Arbeitspreis_mit_dem_Heizwert_um()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT), ERDGAS_E, out stand);

            int kwh = IndexDerEinheit(stand, "kWh");
            Assert.True(kwh >= 0, "Erdgas E muss kWh als Preisbasis anbieten.");

            // Genau zwei Basen, und die zweite ist die Kilowattstunde.
            Assert.Equal(2, stand.Preisbasen.Count);
            Assert.Equal(1, kwh);

            Feld(gaben, () => stand.Arbeitspreis = 0.735);   // €/Nm³
            await PreisbasisSetzen(gaben, kwh);

            // 0,735 €/Nm³ ÷ 10,5 kWh/Nm³ = 0,07 €/kWh. Mit dem Regelfaktor 0,5
            // stand hier 1,47 €/kWh.
            Assert.Equal(0.07, stand.Arbeitspreis, 9);
            Assert.NotEqual(1.47, stand.Arbeitspreis, 6);

            // Nur der Arbeitspreis folgt - Hi, Hs, Leistungs- und Grundpreis bleiben.
            Assert.Equal(10.5, stand.Heizwert, 6);
            Assert.Equal(11.6, stand.Brennwert, 6);

            // Die Einheit von Hi/Hs bleibt die Abrechnungseinheit; der
            // Arbeitspreis nennt jetzt die Preisbasis.
            Assert.Equal("kWh/Nm³", stand.EinheitHeizwert);
            Assert.Equal("€/kWh", stand.EinheitArbeitspreis);

            // Und zurück: derselbe Wert je Nm³, den wir eingegeben haben.
            await PreisbasisSetzen(gaben, 0);
            Assert.Equal(0.735, stand.Arbeitspreis, 9);
        }

        /// <summary>
        /// <b>ETAPPE E7c, Schritt F — die Abnahme von U32:</b> Öffnen–Speichern–Öffnen
        /// hält die Preisbasis „kWh", auch wenn der Brennstoff KEINE Regel nach kWh führt
        /// (Stadtgas: nur Nm³ → Nm³ und m³ → Nm³). Vor Schemaschritt 112 merkte sich die
        /// Karte die Basis allein über die Regelkennung und fiel still auf Nm³ zurück.
        /// </summary>
        [Fact]
        public async Task Die_Preisbasis_kWh_bleibt_ohne_Regel_nach_kWh_stehen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int projekt = 1024, stadtgas = 64;
            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(projekt), stadtgas, out stand);

            int kwh = IndexDerEinheit(stand, "kWh");
            Assert.True(kwh > 0, "Stadtgas muss kWh als Preisbasis anbieten (Hi 4,8).");
            Assert.Equal(0, stand.PreisbasisId);            // Nm³ aus dem Datenteil
            Assert.Equal("", stand.PreisbasisHerleitung);   // Spalte steht - keine Zeile

            await PreisbasisSetzen(gaben, kwh);
            Assert.True(Speichern(gaben));

            EnergietraegerStand neu;
            Geladen(new EnergietraegerHuelle(projekt), stadtgas, out neu);
            Assert.Equal(kwh, neu.PreisbasisId);

            EnergietraegerPreisCtrl.Projektpreis p = EnergietraegerPreisCtrl.ProjektpreisLesen(projekt, stadtgas);
            Assert.Equal("kWh", p.Preisbasis);
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
            // ANWENDERENTSCHEID Q7 (Etappe E2): der Zähler mit vier Nachkommastellen.
            Assert.Equal("0,8400 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0800 €/kWh", stand.FormelText);
            Assert.Equal("0,0800 €", stand.PreisJeKwh);
            Assert.Equal("effektiv: 1 Nm³ = 10,50 kWh (Hi) / 11,60 kWh (Hs)", stand.EffektivText);
        }

        // =================================================================
        // ET-D-4 - Der Arbeitspreis wahlweise in €/kWh (Anwenderwunsch)
        // =================================================================

        /// <summary>Stadtgas: Mengeneinheit Nm³, Hi 4,8 kWh/Nm³.</summary>
        private const int STADTGAS = 64;

        /// <summary>Projekt 1024 führt Stadtgas mit 0,35 €/Nm³.</summary>
        private const int PROJEKT_STADTGAS = 1024;

        private static EnergietraegerPreisCtrl.Projektpreis Gespeichert()
            => EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT_STADTGAS, STADTGAS);

        /// <summary>
        /// Die Klappliste nennt die Einheit des Arbeitspreises; der Wechsel auf
        /// „€/kWh" rechnet nur die ANZEIGE über Hi um, gespeichert wird je Nm³ — hin
        /// und zurück, und auch nach einer Eingabe in €/kWh.
        /// </summary>
        [Fact]
        public async Task Die_Preisbasis_kWh_zeigt_je_kWh_und_speichert_je_Mengeneinheit()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT_STADTGAS), STADTGAS, out stand);

            Assert.Equal(new[] { (0, "€/Nm³"), (1, "€/kWh") }, stand.Preisbasen);
            Assert.Equal(0, stand.PreisbasisId);
            Assert.Equal(0.35, stand.Arbeitspreis, 9);
            Assert.Equal("", stand.PreisbasisHinweis);

            // Umschalten: 0,35 €/Nm³ ÷ 4,8 kWh/Nm³ - gespeichert bleibt 0,35 €/Nm³.
            await PreisbasisSetzen(gaben, 1);
            Assert.Equal(0.35 / 4.8, stand.Arbeitspreis, 9);
            Assert.Equal("€/kWh", stand.EinheitArbeitspreis);
            Assert.Equal("kWh/Nm³", stand.EinheitHeizwert);
            Assert.Equal("0,0729 €/kWh × 4,80 kWh/Nm³ = 0,3500 €/Nm³ (gespeichert je Nm³)",
                         stand.FormelText);

            Assert.True(Speichern(gaben));
            Assert.Equal(0.35, Gespeichert().Arbeitspreis.Value, 9);
            Assert.Equal("kWh", Gespeichert().Preisbasis);

            // Eine Eingabe in €/kWh: 0,07 × 4,8 = 0,336 €/Nm³ in der Datenbank.
            Feld(gaben, () => stand.Arbeitspreis = 0.07);
            Assert.Equal("0,0700 €/kWh × 4,80 kWh/Nm³ = 0,3360 €/Nm³ (gespeichert je Nm³)",
                         stand.FormelText);
            Assert.True(Speichern(gaben));
            Assert.Equal(0.336, Gespeichert().Arbeitspreis.Value, 9);
            Assert.Equal(4.8, Gespeichert().Hi.Value, 9);

            // Und zurück: derselbe Preis je Nm³, der gespeichert ist.
            await PreisbasisSetzen(gaben, 0);
            Assert.Equal(0.336, stand.Arbeitspreis, 9);
            Assert.Equal("€/Nm³", stand.EinheitArbeitspreis);
            Assert.Equal("0,3360 €/Nm³ ÷ 4,80 kWh/Nm³ = 0,0700 €/kWh", stand.FormelText);
        }

        /// <summary>
        /// „Immer": Ein Träger, dessen Heizwert erst im Dialog eingetragen wird,
        /// bekommt „€/kWh" in derselben Sitzung angeboten. Bis dahin sagt eine leise
        /// Zeile, warum es fehlt.
        /// </summary>
        [Fact]
        public async Task Ein_nachtraeglich_eingetragener_Heizwert_macht_kWh_waehlbar()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Katalogträger ohne Heizwert - im Katalogkontext gelesen.
            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET hi_kwh_per_unit = ?, hs_kwh_per_unit = ?, price_work = ? " +
                "WHERE id = ?",
                new DbParam("@hi", 0.0), new DbParam("@hs", 0.0), new DbParam("@w", 0.48),
                new DbParam("@id", STADTGAS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(0), STADTGAS, out stand);

            Assert.Equal(new[] { (0, "€/Nm³") }, stand.Preisbasen);
            Assert.Contains("sobald ein Heizwert gepflegt ist", stand.PreisbasisHinweis);

            Feld(gaben, () => stand.Heizwert = 4.8);

            Assert.Equal(new[] { (0, "€/Nm³"), (1, "€/kWh") }, stand.Preisbasen);
            Assert.Equal(0, stand.PreisbasisId);
            Assert.Equal("", stand.PreisbasisHinweis);

            await PreisbasisSetzen(gaben, 1);
            Assert.Equal(0.1, stand.Arbeitspreis, 9);             // 0,48 ÷ 4,8
            Assert.Equal("€/kWh", stand.EinheitArbeitspreis);
        }

        /// <summary>
        /// Die Gegenrichtung: Fällt der Heizwert auf 0, lässt sich eine Eingabe in
        /// €/kWh nicht mehr umrechnen. Die Karte fällt auf die Mengeneinheit und
        /// zeigt den zuletzt gültigen Preis je Nm³ — sie deutet die Zahl nicht still um.
        /// </summary>
        [Fact]
        public async Task Ohne_Heizwert_faellt_die_Preisbasis_auf_die_Mengeneinheit()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT_STADTGAS), STADTGAS, out stand);

            await PreisbasisSetzen(gaben, 1);
            Feld(gaben, () => stand.Arbeitspreis = 0.07);         // = 0,336 €/Nm³

            Feld(gaben, () => stand.Heizwert = 0.0);

            Assert.Equal(new[] { (0, "€/Nm³") }, stand.Preisbasen);
            Assert.Equal(0, stand.PreisbasisId);
            Assert.Equal("€/Nm³", stand.EinheitArbeitspreis);
            Assert.Equal(0.336, stand.Arbeitspreis, 9);
            Assert.Contains("sobald ein Heizwert gepflegt ist", stand.PreisbasisHinweis);
        }

        /// <summary>
        /// „Katalogwerte übernehmen" lässt die gewählte Preisbasis stehen: Der
        /// Katalogpreis je Nm³ erscheint in €/kWh, gespeichert wird er je Nm³.
        /// </summary>
        [Fact]
        public async Task Katalogwerte_uebernehmen_behaelt_die_Preisbasis_kWh()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET price_work = ? WHERE id = ?",
                new DbParam("@w", 0.48), new DbParam("@id", STADTGAS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT_STADTGAS), STADTGAS, out stand);

            await PreisbasisSetzen(gaben, 1);
            await KatalogUebernehmen(gaben);

            Assert.Equal(1, stand.PreisbasisId);
            Assert.Equal("€/kWh", stand.EinheitArbeitspreis);
            Assert.Equal(0.1, stand.Arbeitspreis, 9);             // 0,48 €/Nm³ ÷ 4,8
            Assert.Contains("noch nicht gespeichert", stand.UebernahmeHinweis);

            Assert.True(Speichern(gaben));
            Assert.Equal(0.48, Gespeichert().Arbeitspreis.Value, 9);
            Assert.Equal("kWh", Gespeichert().Preisbasis);
        }

        /// <summary>
        /// Der Assistent setzt die Preisbasis über DENSELBEN Weg wie die Klappliste.
        /// Ginge er den Weg eines Zahlenfeldes (Id setzen, nachziehen), läse die
        /// Hülle die stehende Zahl als Eingabe in der neuen Einheit — gespeichert
        /// würden 0,35 × 4,8 = 1,68 €/Nm³ statt 0,35.
        /// </summary>
        [Fact]
        public void Die_KI_Sicht_setzt_die_Preisbasis_ohne_den_Preis_zu_verschieben()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(PROJEKT_STADTGAS), STADTGAS, out stand);

            var ki = new EnergietraegerKiSicht
            {
                StandLesen = () => stand,
                Nachziehen = () => ((Func<EnergietraegerAnsicht>)gaben["Nachrechnen"])(),
                PreisbasisSetzen = i => PreisbasisSetzen(gaben, i).GetAwaiter().GetResult()
            };

            Assert.Equal(new[] { "€/Nm³", "€/kWh" },
                         ki.PreisbasisWahl.Select(w => w.Text).ToArray());

            ki.Preisbasis = 1;

            Assert.Equal(1, stand.PreisbasisId);
            Assert.Equal(0.35 / 4.8, ki.Arbeitspreis, 9);
            Assert.True(Speichern(gaben));
            Assert.Equal(0.35, Gespeichert().Arbeitspreis.Value, 9);

            // Ohne den Weg der Klappliste setzt der Assistent die Preisbasis nicht.
            var ohneWeg = new EnergietraegerKiSicht
            {
                StandLesen = () => stand,
                Nachziehen = () => ((Func<EnergietraegerAnsicht>)gaben["Nachrechnen"])()
            };
            ohneWeg.Preisbasis = 0;
            Assert.Equal(1, stand.PreisbasisId);
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

            // Die Felder tragen die Katalogwerte, die Preisbasis bleibt, wo sie
            // stand (hier die Abrechnungseinheit - ET-D-4), und der Hinweis sagt:
            // noch nicht geschrieben.
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

        // =================================================================
        // Loeschen einzelner Preisstaende (Anwenderwunsch 14.09.2026)
        // =================================================================

        /// <summary>Legt einen Stand zum Tag an und gibt seine Id zurueck.</summary>
        private static int StandSchreiben(DateTime tag, double arbeitspreis)
        {
            EnergietraegerPreisCtrl.HistorieSchreiben(ERDGAS_E, PROJEKT, tag,
                new EnergietraegerPreisCtrl.Preisstand
                {
                    Arbeitspreis = arbeitspreis,
                    Hi = 10.5,
                    Basiseinheit = "Nm³"
                });

            foreach (EnergietraegerPreisCtrl.Historienzeile z in
                     EnergietraegerPreisCtrl.Historie(ERDGAS_E, PROJEKT))
                if (z.GueltigAb.Date == tag.Date) return z.Id;
            return 0;
        }

        private static bool HistorieLoeschen(IReadOnlyDictionary<string, object> gaben,
                                             PreishistorieZeile zeile)
        {
            return ((Func<PreishistorieZeile, bool>)gaben["HistorieLoeschen"])(zeile);
        }

        private static string HistorieLoeschenGrund(IReadOnlyDictionary<string, object> gaben)
        {
            return ((Func<string>)gaben["HistorieLoeschenGrund"])();
        }

        /// <summary>
        /// Ohne Schlüssel ließe sich eine Zeile nicht einzeln löschen — zwei
        /// Stände desselben Tages wären über (Träger, Projekt, Datum) nicht
        /// auseinanderzuhalten.
        /// </summary>
        [Fact]
        public void Die_Historie_liefert_den_Schluessel_jeder_Zeile()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StandSchreiben(new DateTime(2026, 9, 14), 0.91);
            StandSchreiben(new DateTime(2026, 9, 15), 0.92);

            List<EnergietraegerPreisCtrl.Historienzeile> zeilen =
                EnergietraegerPreisCtrl.Historie(ERDGAS_E, PROJEKT);

            Assert.True(zeilen.Count >= 2);
            Assert.All(zeilen, z => Assert.True(z.Id > 0));
            Assert.Equal(zeilen.Count, new HashSet<int>(zeilen.ConvertAll(z => z.Id)).Count);
        }

        [Fact]
        public void HistorieLoeschen_entfernt_genau_die_Zeile_und_laesst_die_Nachbarn()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int bleibt = StandSchreiben(new DateTime(2026, 9, 14), 0.91);
            int weg = StandSchreiben(new DateTime(2026, 9, 15), 0.92);
            Assert.True(bleibt > 0 && weg > 0 && bleibt != weg);

            int vorher = HistorienZeilen(ERDGAS_E, PROJEKT);

            Assert.Equal(1, EnergietraegerPreisCtrl.HistorieLoeschen(weg, ERDGAS_E, PROJEKT));

            Assert.Equal(vorher - 1, HistorienZeilen(ERDGAS_E, PROJEKT));
            List<EnergietraegerPreisCtrl.Historienzeile> zeilen =
                EnergietraegerPreisCtrl.Historie(ERDGAS_E, PROJEKT);
            Assert.DoesNotContain(zeilen, z => z.Id == weg);
            Assert.Contains(zeilen, z => z.Id == bleibt);
        }

        /// <summary>
        /// Träger und Projekt sind der Riegel: Ein veralteter Stand der Karte
        /// darf nicht die Zeile eines anderen Trägers oder Projekts treffen.
        /// </summary>
        [Fact]
        public void Eine_fremde_Traeger_oder_Projekt_Id_loescht_nichts()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = StandSchreiben(new DateTime(2026, 9, 14), 0.91);
            Assert.True(id > 0);
            int vorher = HistorienZeilen(ERDGAS_E, PROJEKT);

            Assert.Equal(0, EnergietraegerPreisCtrl.HistorieLoeschen(id, ERDGAS_E + 1, PROJEKT));
            Assert.Equal(0, EnergietraegerPreisCtrl.HistorieLoeschen(id, ERDGAS_E, PROJEKT + 1));
            Assert.Equal(0, EnergietraegerPreisCtrl.HistorieLoeschen(0, ERDGAS_E, PROJEKT));

            Assert.Equal(vorher, HistorienZeilen(ERDGAS_E, PROJEKT));
        }

        /// <summary>
        /// BEFUND zur gemeldeten Doppelzeile: <c>valid_from</c> ist TEXT und
        /// trägt im Bestand beides — Tagesstände aus dieser Karte und
        /// ZEITPUNKTE aus der Zuordnung (<c>WizardCtrl</c> und
        /// <c>EnergietraegerVarianteCtrl</c> schreiben <c>DateTime.Now</c>). Der
        /// Vergleich auf Gleichheit verfehlte den Zeitpunkt desselben Tages und
        /// legte eine ZWEITE Zeile an; die Karte zeigte zweimal dasselbe
        /// „Gültig ab". Verglichen wird deshalb der Kalendertag.
        /// </summary>
        [Fact]
        public void Ein_Stand_mit_Uhrzeit_wird_am_selben_Tag_aktualisiert_statt_gedoppelt()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Eine Bestandszeile MIT Uhrzeit, wie die Zuordnung sie schreibt.
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_price (carrier_id, id_projekt, arbeitspreis, heizwert, " +
                "grundpreis, valid_from, arbeitspreis_unit, leistungspreis) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam[]
                {
                    new DbParam("@c", ERDGAS_E),
                    new DbParam("@p", PROJEKT),
                    new DbParam("@ap", 0.50),
                    new DbParam("@hi", 10.5),
                    new DbParam("@gp", 0.0),
                    new DbParam("@d", new DateTime(2026, 9, 14, 20, 33, 16)),
                    new DbParam("@au", "Nm³"),
                    new DbParam("@lp", 0.0)
                });

            int vorher = HistorienZeilen(ERDGAS_E, PROJEKT);

            StandSchreiben(new DateTime(2026, 9, 14), 0.91);

            // Keine zweite Zeile desselben Tages - der Zeitpunkt wurde getroffen.
            Assert.Equal(vorher, HistorienZeilen(ERDGAS_E, PROJEKT));
            Assert.Equal(0.91, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT arbeitspreis FROM energy_price WHERE carrier_id = ? AND id_projekt = ? " +
                "AND valid_from = ?",
                new DbParam("@c", ERDGAS_E), new DbParam("@p", PROJEKT),
                new DbParam("@d", new DateTime(2026, 9, 14, 20, 33, 16)))), 4);
        }

        [Fact]
        public void Die_Huelle_loescht_im_Projekt_und_liest_die_Tabelle_neu()
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

            int vorher = HistorienZeilen(ERDGAS_E, PROJEKT);
            Assert.True(stand.Historie.Count > 0);
            PreishistorieZeile zeile = stand.Historie[0];
            Assert.True(zeile.Id > 0);

            Assert.True(HistorieLoeschen(gaben, zeile));
            Assert.Equal("", HistorieLoeschenGrund(gaben));

            Assert.Equal(vorher - 1, HistorienZeilen(ERDGAS_E, PROJEKT));
            Assert.Equal(vorher - 1, stand.Historie.Count);
            Assert.DoesNotContain(stand.Historie, z => z.Id == zeile.Id);
        }

        [Fact]
        public void Der_Katalogkontext_lehnt_das_Loeschen_benannt_ab()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben =
                Geladen(new EnergietraegerHuelle(0), ERDGAS_E, out stand);

            var zeile = new PreishistorieZeile("14.09.2026", "10,50", "Nm³", "0,9100",
                                               "0,00", "0,00", 4711);

            Assert.False(HistorieLoeschen(gaben, zeile));
            Assert.Contains("je Projekt", HistorieLoeschenGrund(gaben));
        }

        // =================================================================
        // Komponentenkontext (Anwenderwunsch 14.09.2026, Auftrag 268)
        // =================================================================

        /// <summary>„Elektrische Energie" (Gruppe Strom) — dem Projekt 1030 zugeordnet.</summary>
        private const int STROM = 60;

        /// <summary>Der Gasheizkessel des Projekts 1030 (Brennstoff Erdgas E).</summary>
        private const int KESSEL_GAS = 1018330;

        /// <summary>Die Trägerzeilen eines Parametersatzes (ohne Gruppenköpfe).</summary>
        private static List<EnergietraegerDialog.EnergietraegerListe> Zeilen(
            IReadOnlyDictionary<string, object> gaben)
        {
            var liste = new List<EnergietraegerDialog.EnergietraegerListe>();
            foreach (EnergietraegerDialog.EnergietraegerListe e in
                     (IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>)gaben["Liste"])
                if (e.Traeger.HasValue) liste.Add(e);
            return liste;
        }

        private static List<int> Ids(IReadOnlyDictionary<string, object> gaben)
        {
            var ids = new List<int>();
            foreach (EnergietraegerDialog.EnergietraegerListe e in Zeilen(gaben)) ids.Add(e.Traeger.Value);
            return ids;
        }

        [Fact]
        public void Ohne_Komponentenkontext_bleibt_die_Liste_ungefiltert()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> ids = Ids(new EnergietraegerHuelle(PROJEKT).Gaben());

            Assert.Contains(ERDGAS_E, ids);
            Assert.Contains(STROM, ids);
            Assert.DoesNotContain("für", (string)new EnergietraegerHuelle(PROJEKT).Gaben()["KontextText"]);
        }

        [Fact]
        public void Eine_Waermepumpe_sieht_nur_die_Stromtraeger()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                new EnergietraegerHuelle(PROJEKT).Gaben(0, DbWerte.ERZEUGER_WAERMEPUMPE);

            List<int> ids = Ids(gaben);
            Assert.Contains(STROM, ids);
            Assert.DoesNotContain(ERDGAS_E, ids);

            string kopf = (string)gaben["KontextText"];
            Assert.Contains(DbWerte.ERZEUGER_WAERMEPUMPE, kopf);
            Assert.Contains("Strom", kopf);
        }

        [Fact]
        public void Ein_Gaskessel_sieht_seine_Gastraeger_und_keinen_Strom()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> ids = Ids(new EnergietraegerHuelle(PROJEKT)
                .Gaben(0, DbWerte.ERZEUGER_HEIZKESSEL, KESSEL_GAS));

            Assert.Contains(ERDGAS_E, ids);
            Assert.DoesNotContain(STROM, ids);
        }

        [Fact]
        public void Der_zugeordnete_Traeger_bleibt_in_der_Liste_und_wird_markiert()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Erdgas E als Vorwahl einer WAERMEPUMPE: unzulaessig, aber zugeordnet -
            // er verschwindet nicht, sondern steht mit Hinweis da.
            IReadOnlyDictionary<string, object> gaben = new EnergietraegerHuelle(PROJEKT)
                .Gaben(ERDGAS_E, DbWerte.ERZEUGER_WAERMEPUMPE);

            List<EnergietraegerDialog.EnergietraegerListe> zeilen = Zeilen(gaben);
            EnergietraegerDialog.EnergietraegerListe erdgas =
                zeilen.Find(z => z.Traeger == ERDGAS_E);
            EnergietraegerDialog.EnergietraegerListe strom = zeilen.Find(z => z.Traeger == STROM);

            Assert.NotNull(erdgas);
            Assert.False(erdgas.Passend);
            Assert.NotNull(strom);
            Assert.True(strom.Passend);
        }

        [Fact]
        public void Die_Kataloguebernahme_bietet_nur_zulaessige_Traeger_an()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                new EnergietraegerHuelle(PROJEKT).Gaben(0, DbWerte.ERZEUGER_WAERMEPUMPE);

            var freie = ((Func<IReadOnlyList<ValueTuple<int, string>>>)gaben["FreieLaden"])();
            Assert.NotEmpty(freie);
            foreach (ValueTuple<int, string> f in freie)
                Assert.StartsWith("Strom ", f.Item2);
        }

        [Fact]
        public void Solarthermie_engt_nicht_ein_sondern_sagt_es_in_der_Kopfzeile()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben =
                new EnergietraegerHuelle(PROJEKT).Gaben(0, DbWerte.ERZEUGER_SOLARTHERMIE);

            // Die Verwaltung pflegt die Traeger des PROJEKTS - eine leere Liste waere
            // eine Sackgasse. Gesagt wird es trotzdem.
            List<int> ids = Ids(gaben);
            Assert.Contains(ERDGAS_E, ids);
            Assert.Contains(STROM, ids);
            Assert.Contains(DbWerte.ERZEUGER_SOLARTHERMIE, (string)gaben["KontextText"]);
            Assert.Contains("kein eigener", (string)gaben["KontextText"]);
        }

        // =================================================================
        // ET-E-3 (Anwenderentscheid 17.09.2026): ohne Erzeugerart engt das PROJEKT ein
        // =================================================================

        private static IReadOnlyList<ValueTuple<int, string>> Freie(
            IReadOnlyDictionary<string, object> gaben)
        {
            return ((Func<IReadOnlyList<ValueTuple<int, string>>>)gaben["FreieLaden"])();
        }

        /// <summary>Die Gruppe eines Trägers — aus der Datenbank, nicht aus dem Test.</summary>
        private static string Gruppe(int carrierId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT group_code FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }

        [Fact]
        public void Ohne_Erzeugerart_bietet_die_Uebernahme_nur_die_Traeger_der_Projektanlagen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> vereinigung =
                EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT);
            Assert.NotNull(vereinigung);   // 1030: Gaskessel und zwei Gas-BHKW

            IReadOnlyList<ValueTuple<int, string>> freie =
                Freie(new EnergietraegerHuelle(PROJEKT).Gaben());

            Assert.NotEmpty(freie);
            foreach (ValueTuple<int, string> f in freie)
                Assert.Contains(Gruppe(f.Item1), vereinigung);

            // Die Einengung greift wirklich: der ganze freie Katalog ist größer.
            Assert.True(freie.Count < EnergietraegerKatalogCtrl.NichtZugeordnete(PROJEKT).Count,
                        "Die Übernahme wurde nicht eingeengt.");
        }

        /// <summary><c>energy_carrier.id</c> von „Fernwärme" — ein Träger, den keine Anlage
        /// des Projekts 1030 beziehen kann.</summary>
        private const int FERNWAERME = 51;

        [Fact]
        public void Ein_zugeordneter_Traeger_ausserhalb_der_Vereinigung_bleibt_in_der_Liste()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Fernwärme hält im Projekt 1030 keine Anlage - sie wird trotzdem zugeordnet.
            // (Bis zum Anwenderentscheid vom 22.09.2026 stand hier der STROM: Die zwei
            // BHKW des Projekts zählen seither als Stromverwendung, siehe den Fall
            // darunter.)
            Assert.True(new WizardCtrl().TraegerSatzAnlegen(PROJEKT, FERNWAERME));

            IReadOnlyList<string> vereinigung =
                EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT);
            Assert.NotNull(vereinigung);
            Assert.DoesNotContain(Gruppe(FERNWAERME), vereinigung);

            IReadOnlyDictionary<string, object> gaben = new EnergietraegerHuelle(PROJEKT).Gaben();

            // Die linke Liste führt die Träger des PROJEKTS; eine vorhandene Zuordnung
            // wird nicht versteckt - eingeengt wird allein die Übernahme.
            Assert.Contains(FERNWAERME, Ids(gaben));
            foreach (ValueTuple<int, string> f in Freie(gaben))
                Assert.NotEqual(FERNWAERME, f.Item1);
        }

        /// <summary>
        /// <b>Das BHKW verwendet Strom</b> (Anwenderentscheide 22.09.2026): Es erzeugt ihn,
        /// sein Eigenverbrauch deckt den Strombedarf, der Rest wird bezogen. Die
        /// Trägerauswahl eines BHKW-Projekts bietet deshalb Strom an — dieselbe Antwort,
        /// die Stromträger-Automatik, Rückfallträger und Wirtschaftlichkeit geben.
        /// </summary>
        [Fact]
        public void Ein_BHKW_Projekt_bietet_den_Strom_an()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ProjektEnergietraegerCtrl.BrauchtStromTraeger(PROJEKT));
            IReadOnlyList<string> vereinigung =
                EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT);
            Assert.NotNull(vereinigung);
            Assert.Contains(Gruppe(STROM), vereinigung);
            Assert.Contains(Gruppe(ERDGAS_E), vereinigung);
        }

        [Fact]
        public void Im_Katalogkontext_bleibt_alles_frei()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyDictionary<string, object> gaben = new EnergietraegerHuelle(0).Gaben();

            Assert.Equal("Katalog · Preise netto", (string)gaben["KontextText"]);
            Assert.Equal(KostenSummenCtrl.GetAllCarriers(0).Count, Ids(gaben).Count);
        }

        [Fact]
        public void Mit_Erzeugerart_bleibt_es_beim_Einzelfall()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Eine Wärmepumpe in einem Gasprojekt: die Komponente gewinnt, nicht das Projekt.
            IReadOnlyDictionary<string, object> gaben =
                new EnergietraegerHuelle(PROJEKT).Gaben(0, DbWerte.ERZEUGER_WAERMEPUMPE);

            IReadOnlyList<ValueTuple<int, string>> freie = Freie(gaben);
            Assert.NotEmpty(freie);
            foreach (ValueTuple<int, string> f in freie)
                Assert.Equal(Gruppe(STROM), Gruppe(f.Item1));

            string kopf = (string)gaben["KontextText"];
            Assert.Contains(DbWerte.ERZEUGER_WAERMEPUMPE, kopf);
            Assert.DoesNotContain("Anlagen des Projekts", kopf);
        }

        [Fact]
        public void Die_Kopfzeile_nennt_die_Einengung_auf_die_Projektanlagen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string kopf = (string)new EnergietraegerHuelle(PROJEKT).Gaben()["KontextText"];

            // ET-D: Die Kopfzeile nennt den PROJEKTNAMEN und die Preisstellung.
            Assert.StartsWith(StartseiteCtrl.Projektname(PROJEKT) + " · Preise netto", kopf);
            Assert.Contains("Anlagen des Projekts", kopf);
            foreach (string g in EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(PROJEKT))
                Assert.Contains(g, kopf);
        }

        [Fact]
        public void Die_Kopfzeile_der_Projekteinengung_steht_auch_auf_Englisch()
        {
            using var _ = new Kulturvorrichtung("en-US");
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string kopf = (string)new EnergietraegerHuelle(PROJEKT).Gaben()["KontextText"];

            Assert.Contains("· prices net", kopf);
            Assert.Contains("project's systems", kopf);
        }

        [Fact]
        public void Ein_Projekt_ohne_Anlagen_engt_die_Uebernahme_nicht_ein()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int OHNE_ANLAGEN = 19;
            IReadOnlyDictionary<string, object> gaben = new EnergietraegerHuelle(OHNE_ANLAGEN).Gaben();

            Assert.Equal(StartseiteCtrl.Projektname(OHNE_ANLAGEN) + " · Preise netto",
                         (string)gaben["KontextText"]);
            Assert.Equal(EnergietraegerKatalogCtrl.NichtZugeordnete(OHNE_ANLAGEN).Count,
                         Freie(gaben).Count);
        }

        // =================================================================
        // ET-5 / ET-6 (Anwenderbefund 16.09.2026): der Rueckweg der Liste
        // =================================================================

        /// <summary>Projekt 1024 — acht Traegerzuordnungen, Waermepumpe mit Heizstab.</summary>
        private const int PROJEKT_1024 = 1024;

        /// <summary>„Heizoel L" — 1024 zugeordnet, von KEINER Anlage gehalten.</summary>
        private const int OHNE_ANLAGE = 62;

        private static IReadOnlyList<EnergietraegerDialog.EnergietraegerListe> Neugeladen(
            IReadOnlyDictionary<string, object> gaben)
        {
            return ((Func<IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>>)
                    gaben["ListeNeuLaden"])();
        }

        private static bool Waehlen(IReadOnlyDictionary<string, object> gaben, int traegerId)
        {
            return ((Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"])(traegerId) != null;
        }

        private static ValueTuple<bool, string> Entfernen(IReadOnlyDictionary<string, object> gaben)
        {
            return ((Func<ValueTuple<bool, string>>)gaben["AusProjekt"])();
        }

        private static bool Steht(IReadOnlyList<EnergietraegerDialog.EnergietraegerListe> liste, int id)
        {
            foreach (EnergietraegerDialog.EnergietraegerListe e in liste)
                if (e.Traeger == id) return true;
            return false;
        }

        /// <summary>
        /// ET-5: Der Wert <c>["Liste"]</c> ist der Stand des OEFFNENS und bleibt es —
        /// der Gabensatz lebt so lange wie der Dialog. Frisch wird die Liste nur ueber
        /// den Delegaten <c>ListeNeuLaden</c>; genau er fehlte, weshalb ein entfernter
        /// Traeger in der Oberflaeche stehen blieb.
        /// </summary>
        [Fact]
        public void ListeNeuLaden_liefert_den_frischen_Stand_der_eingefrorene_Wert_nicht()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var huelle = new EnergietraegerHuelle(PROJEKT_1024);
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben(OHNE_ANLAGE);
            Assert.True(Steht(Neugeladen(gaben), OHNE_ANLAGE));

            Assert.True(Waehlen(gaben, OHNE_ANLAGE));
            ValueTuple<bool, string> e = Entfernen(gaben);
            Assert.True(e.Item1);

            // Der Delegat sieht den neuen Stand ...
            Assert.False(Steht(Neugeladen(gaben), OHNE_ANLAGE));
            // ... der eingefrorene Wert nicht. Das ist der Befund, kein Fehler:
            // Deshalb liest die Komponente ab ET-5 ueber den Delegaten.
            Assert.True(Steht(
                (IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>)gaben["Liste"], OHNE_ANLAGE));
        }

        /// <summary>
        /// ET-6: Ohne Wiederzuordnung sagt die Huelle nichts — der Hinweis ist kein
        /// Begleittext des Entfernens, sondern die Antwort auf ein Ereignis.
        /// </summary>
        [Fact]
        public void Ein_gewoehnliches_Entfernen_nennt_keinen_Stromtraeger()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var huelle = new EnergietraegerHuelle(PROJEKT_1024);
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben(OHNE_ANLAGE);
            Assert.True(Waehlen(gaben, OHNE_ANLAGE));

            Assert.True(Entfernen(gaben).Item1);
            Assert.Equal("", ((Func<string>)gaben["StromZugeordnet"])());
        }

        /// <summary>
        /// ET-6: <c>ListeLaden</c> ruft vor jedem Lesen
        /// <c>ProjektEnergietraegerCtrl.StromTraegerSicherstellen</c>. Verliert ein
        /// Projekt mit elektrischer Welt dabei seinen letzten zugeordneten Stromtraeger,
        /// ordnet der Aufruf im selben Atemzug den Auslieferungstraeger zu — bis hierher
        /// STILL. Der Kern bleibt, wie er ist (Anwenderentscheid ET-2 vom 08.09.2026);
        /// die Huelle NENNT den Traeger, damit der Dialog es sagen kann.
        ///
        /// <para>Der Stand wird hier hergestellt: Waermepumpe und Photovoltaik des
        /// Projekts 1045 waehlen „Elektrische Energie 2" (Katalogtraeger, dem Projekt
        /// NICHT zugeordnet — ET-5-Anlagenwahl vom 08.09.2026), zugeordnet ist allein
        /// „Elektrische Energie". Damit haelt die elektrische Welt den zugeordneten
        /// Traeger nicht fest, sein Entfernen ist erlaubt, und der Wiederzuordner
        /// greift.</para>
        /// </summary>
        [Fact]
        public void Das_Entfernen_nennt_den_wieder_zugeordneten_Stromtraeger()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int PROJEKT_1045 = 1045;
            const int STROM_2 = 58;

            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET ID_Carrier = ? " +
                "WHERE ID_Projekt = ? AND (ID_WP > 0 OR ID_PV > 0 OR ID_SP > 0)",
                new DbParam("@c", STROM_2), new DbParam("@p", PROJEKT_1045));
            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT_1045, STROM));

            var huelle = new EnergietraegerHuelle(PROJEKT_1045);
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben(STROM);
            Assert.True(Steht(Neugeladen(gaben), STROM));
            Assert.True(Waehlen(gaben, STROM));

            ValueTuple<bool, string> e = Entfernen(gaben);
            Assert.True(e.Item1);

            // Der Traeger ist wieder da - und die Huelle sagt, WELCHER.
            string name = ((Func<string>)gaben["StromZugeordnet"])();
            Assert.False(string.IsNullOrEmpty(name));
            Assert.True(Steht(Neugeladen(gaben), STROM));
        }

        /// <summary>
        /// Der Hinweistext kommt aus dem Ressourcenkatalog und folgt der
        /// Oberflaechensprache (Regel seit iU9-W8) — er ist in BEIDEN Sprachen gepflegt.
        /// </summary>
        [Fact]
        public void Der_Hinweis_zum_Stromtraeger_steht_in_beiden_Sprachen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string de, en;
            using (var _ = new Kulturvorrichtung("de-DE"))
                de = (string)new EnergietraegerHuelle(PROJEKT_1024).Gaben()["VorlageStromZugeordnet"];
            using (var _ = new Kulturvorrichtung("en-US"))
                en = (string)new EnergietraegerHuelle(PROJEKT_1024).Gaben()["VorlageStromZugeordnet"];

            Assert.Contains("Stromträger", de);
            Assert.Contains("{0}", de);
            Assert.Contains("electricity carrier", en);
            Assert.Contains("{0}", en);
            Assert.NotEqual(de, en);
        }

        /// <summary>
        /// DL-Q5 (#390): Der Stammkopf hat keinen eigenen Knopf mehr - Bezeichnung und
        /// Gruppe werden mit Speichern/OK geschrieben. Die Gabe "StammSpeichernText" gibt
        /// es deshalb nicht mehr; der Abbrechen-Kurztext sagt, was Abbrechen verwirft.
        /// </summary>
        [Fact]
        public void Der_Stammkopf_hat_keinen_eigenen_Knopf_mehr()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            using (var _ = new Kulturvorrichtung("de-DE"))
            {
                IReadOnlyDictionary<string, object> g = new EnergietraegerHuelle(0).Gaben();
                Assert.False(g.ContainsKey("StammSpeichernText"));
                Assert.False(string.IsNullOrWhiteSpace((string)g["AbbrechenKurztext"]));
            }
        }
    }
}
