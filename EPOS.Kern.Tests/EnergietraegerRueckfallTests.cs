using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Fehlender CO₂-Wert, fehlender Preis — Warnung und Übernahme aus der
    /// Kategorie, nur mit Rückfrage</b> (Anwenderentscheid 15.09.2026).
    ///
    /// <para><b>Was geprüft wird.</b> Die Trägerkarte nennt die Lücke, wo sie
    /// entsteht; der Übernahmeweg legt die Träger DERSELBEN KATEGORIE
    /// (<c>energy_carrier.pricing_model</c>) mit ihrem Wert vor; ohne Bestätigung
    /// wird NICHTS geschrieben; nach der Bestätigung steht der Wert in der
    /// Projektzeile und der Katalog ist unverändert; und ein Träger ohne Preis
    /// lässt sich weiterhin speichern.</para>
    ///
    /// <para><b>Die Lage in der Testdatenbank.</b> Projekt 1030 führt „Elektrische
    /// Energie" (60) und „Erdgas E" (63, 0,84 €/Nm³). Ein frisch zugeordneter
    /// Katalogträger bekommt eine Projektzeile ohne Preise — genau die Lücke des
    /// Entscheids. Keine der drei Wood-Zeilen des Katalogs (Scheitholz,
    /// Holzpellets, Holzhackschnitzel) trägt in irgendeiner Ebene der Lesekette
    /// einen CO₂-Wert; sie sind der CO₂-Fall.</para>
    ///
    /// <para>Die Fälle arbeiten auf der ARBEITSKOPIE (<see cref="TestDatenbank"/>) —
    /// die Quelldatei ist die Messlatte der Referenzläufe und bleibt unberührt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerRueckfallTests
    {
        private const int PROJEKT = 1030;

        // Gasförmige Brennstoffe (GASEOUS_FUEL, Nm³)
        private const int ERDGAS_E = 63;      // im Projekt, 0,84 €/Nm³
        private const int ERDGAS_LL = 52;
        private const int STADTGAS = 64;

        // Feste Brennstoffe (SOLID_FUEL, kg)
        private const int HOLZPELLETS = 76;   // ohne CO₂ in jeder Ebene
        private const int BRAUNKOHLEBRIKETT = 74;   // 400 g/kWh
        private const int KOKS = 59;                // 335 g/kWh
        private const int STEINKOHLE = 73;          // 340 g/kWh

        // Wärme (HEAT) - Fernwärme ist der einzige Träger seiner Kategorie.
        private const int FERNWAERME = 51;

        // =================================================================
        //  Hilfen
        // =================================================================

        private static IReadOnlyDictionary<string, object> Geladen(
            int projekt, int traegerId, out EnergietraegerStand stand)
        {
            var huelle = new EnergietraegerHuelle(projekt);
            IReadOnlyDictionary<string, object> gaben = huelle.Gaben(traegerId);
            var laden = (Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"];
            EnergietraegerAnsicht a = laden(traegerId);
            Assert.NotNull(a.Stand);
            stand = a.Stand;
            return gaben;
        }

        private static Wertluecke Zeile(EnergietraegerStand stand, string groesse)
        {
            foreach (Wertluecke l in stand.Wertluecken)
                if (string.Equals(l.Groesse, groesse, StringComparison.Ordinal)) return l;
            return null;
        }

        private static Uebernahmewahl Wahl(IReadOnlyDictionary<string, object> gaben,
                                           string groesse)
        {
            return ((Func<string, Uebernahmewahl>)gaben["LueckenWahl"])(groesse);
        }

        private static bool Uebernehmen(IReadOnlyDictionary<string, object> gaben,
                                        string groesse, int geber)
        {
            return ((Func<string, int, bool>)gaben["LueckeUebernehmen"])(groesse, geber);
        }

        private static double ProjektArbeitspreis(int traegerId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT custom_price_work FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", traegerId));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o);
        }

        private static double ProjektCo2(int traegerId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT co2 FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", traegerId));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o);
        }

        private static double KatalogArbeitspreis(int traegerId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT price_work FROM energy_carrier WHERE id = ?",
                new DbParam("@c", traegerId));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o);
        }

        private static double KatalogCo2(int traegerId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT co2 FROM energy_carrier WHERE id = ?",
                new DbParam("@c", traegerId));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o);
        }

        private static void ArbeitspreisSetzen(int traegerId, double preis)
        {
            EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, traegerId);
            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET custom_price_work = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@w", preis), new DbParam("@p", PROJEKT),
                new DbParam("@c", traegerId));
        }

        // =================================================================
        //  Träger MIT eigenem Wert - kein Hinweis
        // =================================================================

        [Fact]
        public void Ein_Traeger_mit_eigenem_Arbeitspreis_bekommt_keinen_Hinweis()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergietraegerStand stand;
            Geladen(PROJEKT, ERDGAS_E, out stand);

            // 0,84 EUR/Nm3 stehen in der Projektzeile - nichts fehlt.
            Assert.Null(Zeile(stand, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS));
            Assert.Null(Zeile(stand, EnergietraegerRueckfall.GROESSE_CO2));
        }

        // =================================================================
        //  Träger OHNE Wert, Kategorie hat EINEN
        // =================================================================

        [Fact]
        public void Ohne_Preis_nennt_die_Karte_die_Luecke_und_bietet_den_Weg_an()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, STADTGAS));

            EnergietraegerStand stand;
            Geladen(PROJEKT, STADTGAS, out stand);

            Wertluecke l = Zeile(stand, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS);
            Assert.NotNull(l);
            Assert.NotEqual("", l.Hinweis);
            Assert.NotEqual("", l.KnopfText);    // der Weg steht daneben
            Assert.Equal("", l.Leihzeile);       // noch nichts geliehen
        }

        [Fact]
        public void Genau_ein_Kandidat_wird_trotzdem_vorgelegt_und_schreibt_nichts()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, STADTGAS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, STADTGAS, out stand);

            Uebernahmewahl wahl = Wahl(gaben, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS);

            // Erdgas E ist der einzige gasförmige Träger mit Preis in diesem Projekt.
            Assert.Single(wahl.Kandidaten);
            Assert.Equal(ERDGAS_E, wahl.Kandidaten[0].Id);
            Assert.Contains("0,84", wahl.Kandidaten[0].Text);
            Assert.NotEqual("", wahl.Frage);

            // DAS VORLEGEN ALLEIN SCHREIBT NICHTS.
            Assert.Equal(0.0, ProjektArbeitspreis(STADTGAS), 6);
        }

        // =================================================================
        //  Kategorie hat MEHRERE - feste Reihenfolge
        // =================================================================

        [Fact]
        public void Mehrere_Kandidaten_stehen_in_fester_Reihenfolge()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ArbeitspreisSetzen(ERDGAS_LL, 0.70);
            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, STADTGAS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, STADTGAS, out stand);

            Uebernahmewahl wahl = Wahl(gaben, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS);

            // Beide sind dem Projekt zugeordnet - dann entscheidet der Name:
            // „Erdgas E" vor „Erdgas LL".
            Assert.Equal(2, wahl.Kandidaten.Count);
            Assert.Equal(ERDGAS_E, wahl.Kandidaten[0].Id);
            Assert.Equal(ERDGAS_LL, wahl.Kandidaten[1].Id);

            // Und sie ist wiederholbar.
            Uebernahmewahl zweite = Wahl(gaben, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS);
            Assert.Equal(ERDGAS_E, zweite.Kandidaten[0].Id);
            Assert.Equal(ERDGAS_LL, zweite.Kandidaten[1].Id);
        }

        [Fact]
        public void Der_CO2_Weg_ordnet_die_Kategorie_nach_Namen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, HOLZPELLETS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, HOLZPELLETS, out stand);

            Assert.NotNull(Zeile(stand, EnergietraegerRueckfall.GROESSE_CO2));

            Uebernahmewahl wahl = Wahl(gaben, EnergietraegerRueckfall.GROESSE_CO2);

            // Die drei festen Brennstoffe MIT Wert - die beiden anderen Holzzeilen
            // tragen selbst keinen und werden nicht angeboten.
            Assert.Equal(3, wahl.Kandidaten.Count);
            Assert.Equal(BRAUNKOHLEBRIKETT, wahl.Kandidaten[0].Id);
            Assert.Equal(KOKS, wahl.Kandidaten[1].Id);
            Assert.Equal(STEINKOHLE, wahl.Kandidaten[2].Id);
        }

        // =================================================================
        //  Kategorie hat KEINEN
        // =================================================================

        [Fact]
        public void Ohne_Kandidaten_bietet_der_Weg_nichts_an_und_der_Wert_bleibt()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, FERNWAERME));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, FERNWAERME, out stand);

            Assert.NotNull(Zeile(stand, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS));

            Uebernahmewahl wahl = Wahl(gaben, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS);
            Assert.Empty(wahl.Kandidaten);
            Assert.NotEqual("", wahl.LeerText);   // der Weg sagt, warum er nichts anbietet

            Assert.Equal(0.0, ProjektArbeitspreis(FERNWAERME), 6);
        }

        // =================================================================
        //  Nach der Bestätigung
        // =================================================================

        [Fact]
        public void Der_bestaetigte_Preis_steht_in_der_Projektzeile_und_nicht_im_Katalog()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, STADTGAS));
            double katalogVorher = KatalogArbeitspreis(STADTGAS);

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, STADTGAS, out stand);

            Assert.True(Uebernehmen(gaben, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS, ERDGAS_E));

            // Geschrieben: die Projektübersteuerung. Unberührt: der Katalog.
            Assert.Equal(0.84, ProjektArbeitspreis(STADTGAS), 6);
            Assert.Equal(katalogVorher, KatalogArbeitspreis(STADTGAS), 6);

            // In der Karte steht der Wert, die Lücke ist weg, und die Herleitung
            // sagt, woher er kommt.
            Assert.Equal(0.84, stand.Arbeitspreis, 6);
            Wertluecke l = Zeile(stand, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS);
            Assert.NotNull(l);
            Assert.Equal("", l.Hinweis);
            Assert.Contains("Erdgas E", l.Leihzeile);
        }

        [Fact]
        public void Der_bestaetigte_CO2_Wert_steht_in_der_Projektzeile_und_nicht_im_Katalog()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, HOLZPELLETS));
            double katalogVorher = KatalogCo2(HOLZPELLETS);

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, HOLZPELLETS, out stand);

            Assert.True(Uebernehmen(gaben, EnergietraegerRueckfall.GROESSE_CO2, KOKS));

            Assert.Equal(335.0, ProjektCo2(HOLZPELLETS), 6);
            Assert.Equal(katalogVorher, KatalogCo2(HOLZPELLETS), 6);

            Wertluecke l = Zeile(stand, EnergietraegerRueckfall.GROESSE_CO2);
            Assert.NotNull(l);
            Assert.Equal("", l.Hinweis);
            Assert.Contains("Koks", l.Leihzeile);
        }

        [Fact]
        public void Ein_Traeger_der_kein_Kandidat_ist_wird_nicht_uebernommen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, STADTGAS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, STADTGAS, out stand);

            // „Elektrische Energie" trägt einen Preis, gehört aber einer anderen
            // Kategorie an - der Weg nimmt ihn nicht.
            Assert.False(Uebernehmen(gaben, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS, 60));
            Assert.Equal(0.0, ProjektArbeitspreis(STADTGAS), 6);
        }

        // =================================================================
        //  Der Hinweis sperrt nichts
        // =================================================================

        [Fact]
        public void Ein_Traeger_ohne_Preis_laesst_sich_speichern()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(PROJEKT, STADTGAS));

            EnergietraegerStand stand;
            IReadOnlyDictionary<string, object> gaben = Geladen(PROJEKT, STADTGAS, out stand);
            Assert.NotNull(Zeile(stand, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS));

            Assert.True(((Func<bool>)gaben["Speichern"])());

            // Gespeichert - und derselbe Hinweis steht weiter da.
            Assert.NotEqual("", ((Func<string>)gaben["SpeichernHinweis"])());
            Assert.NotNull(Zeile(stand, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS));
        }

        // =================================================================
        //  Der Rechenweg bleibt unberührt
        // =================================================================

        [Fact]
        public void Die_Emissionsquelle_faellt_weiterhin_nicht_auf_die_Kategorie_zurueck()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Holzpellets tragen in keiner Ebene einen CO2-Wert. Die Kategorie
            // SOLID_FUEL hat drei Träger, die einen tragen - der Rechenweg nimmt
            // trotzdem keinen davon.
            Emissionsfaktoren f = Emissionsquelle.Fuer(
                PROJEKT, HOLZPELLETS, 0, Emissionsquelle.Modus(PROJEKT));

            Assert.False(f.Co2Gepflegt);
            Assert.Equal(0.0, f.Co2GKwh, 6);
        }

        [Fact]
        public void Die_Kategorie_ist_das_pricing_model()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal("GASEOUS_FUEL", EnergietraegerRueckfall.Kategorie(ERDGAS_E));
            Assert.Equal("SOLID_FUEL", EnergietraegerRueckfall.Kategorie(HOLZPELLETS));
            Assert.Equal("", EnergietraegerRueckfall.Kategorie(0));
        }
    }
}
