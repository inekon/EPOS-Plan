using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>U33/U34/U35 — die kWp der Photovoltaik als Bemessung, Kennzahl und
    /// Herleitung.</b>
    ///
    /// <para><b>U33.</b> „je kWp Leistung" steht auch im BETRIEBSRASTER: Die Wartung
    /// einer PV-Anlage wird branchenüblich in €/kWp·a bemessen, und bis dahin ließ sich
    /// genau das nicht bemessen (fester Jahresbetrag, „% der Investition", „je kWh
    /// elektrisch"). Gerechnet wird ohne eine einzige neue Formel — der Rechenweg der
    /// Betriebskosten kennt die Art längst (Menge × Satz), und die Bezugsgröße kommt aus
    /// derselben kWp-Wahrheit wie auf der Investitionsseite
    /// (<see cref="PhotovoltaikCtrl.KwpSumme"/>).</para>
    ///
    /// <para><b>U35.</b> Die Bezugsgröße 300,00 kWp ist GERECHNET (Modulanzahl ×
    /// Modulleistung ÷ 1000) und stand in keiner Maske mit ihren Faktoren.
    /// <c>Tab_Energieanlagen.PV_Leistung</c> ist die MODULANZAHL,
    /// <c>Tab_PV.Leistung</c> die Modulleistung in Watt — wer 750 für die Leistung hält,
    /// liest einen Satz von 320,00 €/kWp als 240.000,00 € statt als 96.000,00 €.</para>
    ///
    /// <para><b>U34.</b> Dieselbe kWp-Summe trägt die Kennzahl des Summenfußes —
    /// „spezifisch 640,50 €/kWp", die Zahl, mit der eine PV-Investition verglichen
    /// wird. Reine Anzeige; der Kapitalwert sieht sie nicht.</para>
    ///
    /// <para>Die Zahlenproben sind die des Mockups
    /// <c>Dialog_Formel_Zahlenprobe.html</c>, Abschnitt 3. Die Fälle mit Datenbank
    /// arbeiten auf einer Arbeitskopie; fehlt die Datei, schweigen sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwpBemessungHerleitungTests
    {
        /// <summary><c>Tab_KostenKomponente.ID</c> der Photovoltaik.</summary>
        private const int K_PHOTOVOLTAIK = 3;

        /// <summary>Die Ost/West-Anlage der Testdatenbank (W6‑O‑4): EINE Anlagenzeile
        /// mit EINEM Modultyp an einem Wechselrichter.</summary>
        private const int PROJEKT_OSTWEST = 1045;
        private const int ANLAGE_OSTWEST = 14926;

        // =====================================================================
        //  U33 — Satz, Einheit und Herleitungszeile im Betriebsraster
        // =====================================================================

        /// <summary>
        /// Die Zahlenprobe des Mockups: 12,00 €/kWp·a × 300,00 kWp = 3.600,00 €/a. Der
        /// Satz trägt sein Jahr im Zeichen, die Bezugsgröße nicht — kWp ist eine
        /// Leistung und kennt kein Jahr.
        /// </summary>
        [Fact]
        public void Ein_Wartungssatz_je_kWp_ergibt_Satz_mal_kWp_im_Jahr()
        {
            using var kultur = new Kulturvorrichtung();

            double betrag = BetriebskostenCtrl.Betrag(
                DbWerte.BEMESSUNG_EUR_PRO_KWP, 0.0, 300.0, 12.0, false);
            Assert.Equal(3600.00, betrag, 2);

            Assert.Equal("€/kWp·a",
                BetriebskostenCtrl.SatzEinheit(DbWerte.BEMESSUNG_EUR_PRO_KWP, K_PHOTOVOLTAIK, true));
            Assert.Equal("€/kWp",
                BetriebskostenCtrl.SatzEinheit(DbWerte.BEMESSUNG_EUR_PRO_KWP, K_PHOTOVOLTAIK));
        }

        /// <summary>
        /// Werkzeugtipp und Herleitungszeile der Betriebszeile: Der SATZ steht in der
        /// Jahreseinheit, die BEZUGSGRÖSSE in ihrer physikalischen — „× 300,00 kWp ·
        /// kWp der Anlage". Ohne Kaskade gibt es keine Runde (Betriebsseite).
        /// </summary>
        [Fact]
        public void Die_Betriebszeile_nennt_Jahressatz_und_kWp_der_Anlage()
        {
            using var kultur = new Kulturvorrichtung();

            var p = new KostenVorlagenPosition
            {
                Bezeichnung = "Wartung / Inspektion PV-Anlage",
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KWP,
                Satz = 12.0,
                BetragNetto = 3600.0
            };
            var pz = new KostenProjektPositionenCtrl.Zeile
            {
                Basis = 300.0,
                Runde = 0,
                BasisHerkunft = KostenHerleitung.HERKUNFT_ANLAGE
            };

            KostenHerleitung.Angabe a =
                KostenHerleitung.Bilde(p, K_PHOTOVOLTAIK, pz, true, betrieb: true);

            Assert.Equal("300,00 kWp", a.BasisText);
            Assert.Equal("kWp der Anlage", a.HerkunftText);
            Assert.Equal("× 300,00 kWp · kWp der Anlage", a.Zeile);
            Assert.Equal("Aus Satz und Bezugsgröße des Projekts berechnet: 12,00 €/kWp·a × 300,00 kWp.",
                         a.Kurztext);
        }

        /// <summary>
        /// Gegenprobe zum Raster: Dieselbe Zeile auf der INVESTITIONSSEITE trägt den
        /// Satz ohne Jahr. Ein €/kWp-Satz kauft die Anlage einmal, ein €/kWp·a-Satz
        /// wartet sie jedes Jahr.
        /// </summary>
        [Fact]
        public void Dieselbe_Art_traegt_auf_der_Investitionsseite_kein_Jahr()
        {
            using var kultur = new Kulturvorrichtung();

            var p = new KostenVorlagenPosition
            {
                Bezeichnung = "PV-Module",
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KWP,
                Satz = 320.0,
                BetragNetto = 96000.0
            };
            var pz = new KostenProjektPositionenCtrl.Zeile
            {
                Basis = 300.0,
                Runde = 1,
                BasisHerkunft = KostenHerleitung.HERKUNFT_ANLAGE
            };

            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(p, K_PHOTOVOLTAIK, pz, true);

            Assert.Equal("× 300,00 kWp · kWp der Anlage · Runde 1", a.Zeile);
            Assert.Contains("320,00 €/kWp × 300,00 kWp", a.Kurztext);
        }

        // =====================================================================
        //  U35 — die Herleitung der kWp
        // =====================================================================

        /// <summary>
        /// Die Zahlenprobe des Mockups: 750 Module × 400 Wp = 300,00 kWp. Gemessen wird
        /// an einer Arbeitskopie, deren PV-Anlage auf genau diese beiden Faktoren
        /// gestellt ist — und die Summe kommt aus derselben Rechnung, die auch
        /// Simulation und Vergütungsdialog benutzen.
        /// </summary>
        [Fact]
        public void Die_Zahlenprobe_750_mal_400_Wp_ergibt_300_kWp()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            AnlageStellen(ANLAGE_OSTWEST, module: 750, wattJeModul: 400);

            Assert.Equal(300.0, PhotovoltaikCtrl.KwpSumme(PROJEKT_OSTWEST, ANLAGE_OSTWEST), 6);
            Assert.Equal("750 Module × 400 Wp = 300,00 kWp",
                         PhotovoltaikCtrl.KwpHerleitung(PROJEKT_OSTWEST, ANLAGE_OSTWEST));

            // Dieselbe Auskunft über die EINE Einstiegstür des Dialogs — und für beide
            // Namen derselben Größe („je kWp" und „je kW elektrisch").
            Assert.Equal("750 Module × 400 Wp = 300,00 kWp",
                TechnikPlanwertCtrl.BaugroesseHerleitung(
                    PROJEKT_OSTWEST, K_PHOTOVOLTAIK, DbWerte.BEMESSUNG_EUR_PRO_KWP, ANLAGE_OSTWEST));
            Assert.Equal("750 Module × 400 Wp = 300,00 kWp",
                TechnikPlanwertCtrl.BaugroesseHerleitung(
                    PROJEKT_OSTWEST, K_PHOTOVOLTAIK, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                    ANLAGE_OSTWEST));
        }

        /// <summary>
        /// Die MESSUNG an der Ost/West-Anlage der Testdatenbank (Projekt 1045): Sie ist
        /// EINE Anlagenzeile mit EINEM Modultyp an einem Wechselrichter — die lange Form
        /// mit mehreren Gliedern entsteht dort also gar nicht, und der kurze Satz reicht.
        /// </summary>
        [Fact]
        public void Die_OstWest_Anlage_traegt_einen_Modultyp_und_den_kurzen_Satz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            Assert.Equal("12 Module × 275,19 Wp = 3,30 kWp",
                         PhotovoltaikCtrl.KwpHerleitung(PROJEKT_OSTWEST, ANLAGE_OSTWEST));
        }

        /// <summary>
        /// Mehrere Stränge (Projektsicht): Der Satz nennt nur noch die Summe — Wort für
        /// Wort das Muster der Solarthermie, deren Herleitung bei mehreren Feldern
        /// ebenfalls auf die Gesamtgröße geht. Die Glieder stünden sonst zu mehreren in
        /// EINER Rasterzeile.
        /// </summary>
        [Fact]
        public void Mehrere_Straenge_nennen_nur_die_Summe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            AnlageStellen(ANLAGE_OSTWEST, module: 750, wattJeModul: 400);
            ZweiterStrang(PROJEKT_OSTWEST, module: 200, wattJeModul: 450);

            Assert.Equal(390.0, PhotovoltaikCtrl.KwpSumme(PROJEKT_OSTWEST, 0), 6);
            Assert.Equal("Σ Module × Leistung = 390,00 kWp",
                         PhotovoltaikCtrl.KwpHerleitung(PROJEKT_OSTWEST, 0));

            // Auf EINE Anlagenzeile eingegrenzt bleibt es beim ausführlichen Satz.
            Assert.Equal("750 Module × 400 Wp = 300,00 kWp",
                         PhotovoltaikCtrl.KwpHerleitung(PROJEKT_OSTWEST, ANLAGE_OSTWEST));
        }

        /// <summary>
        /// Ohne installierte Leistung gibt es nichts herzuleiten — dann steht im Raster
        /// das ⚠ und der Grund, nicht ein Satz über 0 Module.
        /// </summary>
        [Fact]
        public void Ohne_Modulleistung_bleibt_die_Herleitung_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            AnlageStellen(ANLAGE_OSTWEST, module: 0, wattJeModul: 400);

            Assert.Equal("", PhotovoltaikCtrl.KwpHerleitung(PROJEKT_OSTWEST, ANLAGE_OSTWEST));
            Assert.Equal("", TechnikPlanwertCtrl.BaugroesseHerleitung(
                PROJEKT_OSTWEST, K_PHOTOVOLTAIK, DbWerte.BEMESSUNG_EUR_PRO_KWP, ANLAGE_OSTWEST));
        }

        /// <summary>
        /// Eine Art, deren Bezugsgröße unmittelbar am Gerät steht, bekommt weiterhin
        /// keine Herleitung: Es gibt nichts zu erklären, die Zahl steht in der
        /// Gerätemaske.
        /// </summary>
        [Fact]
        public void Eine_abgelesene_Baugroesse_bekommt_keine_Herleitung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            Assert.Equal("", TechnikPlanwertCtrl.BaugroesseHerleitung(
                PROJEKT_OSTWEST, 7, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, 0));
        }

        // =====================================================================
        //  U34 — die Kennzahl des Summenfußes
        // =====================================================================

        /// <summary>
        /// Die Zahlenprobe des Mockups: 192.150,00 € ÷ 300,00 kWp = 640,50 €/kWp.
        /// </summary>
        [Fact]
        public void Die_Kennzahl_teilt_die_Nettosumme_durch_die_kWp()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            AnlageStellen(ANLAGE_OSTWEST, module: 750, wattJeModul: 400);

            Assert.Equal("spezifisch 640,50 €/kWp",
                KostenSummenCtrl.KennzahlText(PROJEKT_OSTWEST, K_PHOTOVOLTAIK,
                                              ANLAGE_OSTWEST, 192150.00));
        }

        /// <summary>
        /// Ohne kWp entfällt sie — eine Division durch nichts ist keine Kennzahl.
        /// Ebenso außerhalb der Photovoltaik und im Katalogkontext ohne Projekt: Dort
        /// gibt es keine Anlage, auf die sich etwas beziehen ließe.
        /// </summary>
        [Fact]
        public void Ohne_kWp_ohne_Photovoltaik_und_ohne_Projekt_entfaellt_die_Kennzahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            Assert.Equal("", KostenSummenCtrl.KennzahlText(
                PROJEKT_OSTWEST, 7, ANLAGE_OSTWEST, 192150.00));
            Assert.Equal("", KostenSummenCtrl.KennzahlText(
                0, K_PHOTOVOLTAIK, 0, 192150.00));

            AnlageStellen(ANLAGE_OSTWEST, module: 0, wattJeModul: 400);
            Assert.Equal("", KostenSummenCtrl.KennzahlText(
                PROJEKT_OSTWEST, K_PHOTOVOLTAIK, ANLAGE_OSTWEST, 192150.00));
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Stellt Modulanzahl und Modulleistung EINER Anlagenzeile.</summary>
        private static void AnlageStellen(int idAnlage, double module, double wattJeModul)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET PV_Leistung = ? WHERE ID = ?",
                new DbParam("@n", module), new DbParam("@id", idAnlage));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_PV SET Leistung = ? WHERE ID = " +
                "(SELECT ID_PV FROM Tab_Energieanlagen WHERE ID = ?)",
                new DbParam("@w", wattJeModul), new DbParam("@id", idAnlage));
        }

        /// <summary>Legt einen ZWEITEN PV-Strang mit eigenem Modultyp an.</summary>
        private static void ZweiterStrang(int idProjekt, double module, double wattJeModul)
        {
            int idModul = DataRepository.GetMaxID("Tab_PV") + 1;
            Assert.Equal(1, DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_PV (ID, ID_Projekt, Bezeichner, Leistung) VALUES (?, ?, ?, ?)",
                new DbParam("@id", idModul), new DbParam("@p", idProjekt),
                new DbParam("@b", "Probemodul 2"), new DbParam("@w", wattJeModul)));

            // Die uebrigen Geraeteverweise stehen ausdruecklich auf NULL: Ihre
            // Spaltenvorgabe ist 0, und 0 ist keine Zeile - die zehn Fremdschluessel
            // der Anlagentabelle weisen eine solche Zeile ab.
            int idAnlage = DataRepository.GetMaxID("Tab_Energieanlagen") + 1;
            Assert.Equal(1, DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Energieanlagen (ID, ID_Projekt, Bezeichner, ID_Type, ID_PV, " +
                "PV_Leistung, ID_WP, ID_SP, ID_Solar, ID_Kessel, ID_BHKW, ID_PUFFER, " +
                "WS_ID_Puffer, WS_ID_Puffer2, WQ_ID_Puffer, WQ_ID_Quellprofil) " +
                "VALUES (?, ?, ?, ?, ?, ?, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)",
                new DbParam("@id", idAnlage), new DbParam("@p", idProjekt),
                new DbParam("@b", "PV West"), new DbParam("@t", WizardItemClass.PV_TYP),
                new DbParam("@m", idModul), new DbParam("@n", module)));
        }
    }
}
