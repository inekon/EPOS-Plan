using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Anwenderentscheids W14a-E-8-B1</b> (07.09.2026) und des
    /// damit geschlossenen offenen Punktes <b>W11a-O-2</b>.
    ///
    /// <para><b>Der Entscheid.</b> „Es soll der gepflegte CO₂-Wert herangezogen werden —
    /// der an dem Energieträger hängt (gilt generell für alle Erzeuger!)." Bis dahin
    /// las die Simulation ZWEI andere Quellen: der Kessel die alte Brennstofftabelle,
    /// das BHKW die fünf Gerätespalten seines Katalogs. Seither gilt für beide dieselbe
    /// Kette wie für Wirtschaftlichkeit und Kennzahlen —
    /// <see cref="Emissionsquelle"/> über <see cref="EmissionsFaktorLader"/>.</para>
    ///
    /// <para><b>Warum es hier geprüft wird und nicht im Referenzlauf.</b> Die
    /// Emissionswerte der Simulation stehen in KEINER Referenz-CSV: Weder
    /// <c>aggregate.csv</c> noch eine Vektordatei führt eine Emissionsgröße
    /// (<c>Referenzlauf/Ergebnisexport.cs</c> schreibt sie nicht, und
    /// <c>Tab_Ergebnis*</c> hat keine Emissionsspalte). Der Referenzlauf ist nach dem
    /// Umbau deshalb byte-gleich — und genau darum ist er hier KEIN Nachweis. Die
    /// Faktoren selbst und ihre Wirkung auf <c>Em_CO2_*</c> stehen nur in dieser
    /// Probe.</para>
    ///
    /// <para>Eine Arbeitskopie je Klasse (Regel seit iU9-W11a); fehlt die Datei,
    /// schweigen die Fälle. <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EmissionsquelleTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public EmissionsquelleTests(TestDatenbank db) { _db = db; }

        /// <summary>Kaskadenprojekt mit Kessel UND zwei BHKW-Modulen, alle drei am
        /// Energieträger 63 „Erdgas E".</summary>
        private const int PROJEKT = 1030;

        /// <summary><c>energy_carrier.id</c> von „Erdgas E" in der Testdatenbank;
        /// Projektwert CO₂ 240 g/kWh, SO₂ 0,3, NOx 110 mg/kWh.</summary>
        private const int ERDGAS_E = 63;

        /// <summary><c>Tab_Brennstoff_Stamm.ID</c> von „Stadtgas" — der Brennstoff, den
        /// die Anlagen der Projekte ohne Energieträger führen.</summary>
        private const int BRENNSTOFF_STADTGAS = 1;

        // =================================================================================
        // 1 — Die Kette und ihre Ebenen
        // =================================================================================

        /// <summary>
        /// Der Faktorsatz eines Trägers kommt aus der Kette des Konzepts (§ 3) und
        /// NENNT seine Ebene — ohne die Herkunft könnte der Bericht nicht sagen, woher
        /// die Zahl stammt.
        /// </summary>
        [Fact]
        public void Der_Traegerfaktor_kommt_aus_der_Kette_und_nennt_seine_Herkunft()
        {
            if (!_db.Vorhanden) return;

            Emissionsfaktoren f = Emissionsquelle.Fuer(
                PROJEKT, ERDGAS_E, 0, DbWerte.EMISSION_MODUS_CO2);

            Assert.Equal(ERDGAS_E, f.CarrierId);
            Assert.True(f.Co2Gepflegt);
            Assert.Equal(240.0, f.Co2GKwh, 6);            // Projektwert, nicht der Katalogwert 201
            Assert.Equal(EmissionsFaktorLader.EBENE_PROJEKT, f.Ebene);
            Assert.Equal(0.3, f.So2MgKwh, 6);
            Assert.Equal(110.0, f.NoxMgKwh, 6);
            Assert.Contains("Erdgas E", f.Herkunft);
        }

        /// <summary>
        /// <b>Staub kommt mit</b>, obwohl die Art im Auslieferungsstand ABGEWÄHLT ist
        /// (Konzept F5): Dann gilt <c>Tab_Brennstoff_Stamm.Staub</c> — dieselbe Ebene
        /// STAMM, die die Kette für die drei Kernarten ohnehin liest. Ohne ihn verlöre
        /// die Kessel- und BHKW-Bilanz ihre Staubzahl.
        /// </summary>
        [Fact]
        public void Der_Staub_kommt_aus_dem_Brennstoffkatalog_wenn_die_Art_abgewaehlt_ist()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0.5, EmissionsFaktorLader.Lade(PROJEKT, ERDGAS_E).Staub ?? 0, 6);
            Assert.Equal(0.5, Emissionsquelle.Fuer(
                PROJEKT, ERDGAS_E, 0, DbWerte.EMISSION_MODUS_CO2).StaubMgKwh, 6);
        }

        /// <summary>
        /// <b>Das fünfte Glied.</b> Eine Anlage ohne <c>ID_Carrier</c> hat keinen
        /// Träger, über den der Katalog erreichbar wäre — im Bestand der Regelfall
        /// älterer Projekte. Dann gilt der Brennstoff des GERÄTS gegen dieselbe
        /// <c>Tab_Brennstoff_Stamm</c>, in der die Kette ohnehin endet; das ist genau
        /// das Verhalten, das der Kessel vor B1 hatte.
        /// </summary>
        [Fact]
        public void Ohne_Energietraeger_gilt_der_Brennstoff_des_Geraets()
        {
            if (!_db.Vorhanden) return;

            Emissionsfaktoren f = Emissionsquelle.Fuer(
                PROJEKT, 0, BRENNSTOFF_STADTGAS, DbWerte.EMISSION_MODUS_CO2);

            Assert.Equal(0, f.CarrierId);
            Assert.True(f.Co2Gepflegt);
            Assert.Equal(240.0, f.Co2GKwh, 6);            // Tab_Brennstoff_Stamm.CO2 von Stadtgas
            Assert.Equal(Emissionsquelle.EBENE_BRENNSTOFF, f.Ebene);
            Assert.Contains("Stadtgas", f.Herkunft);
            Assert.Contains("kein Energieträger", f.Herkunft);
        }

        /// <summary>
        /// Weder Träger noch Brennstoff: Der Faktor bleibt 0 und sagt, dass er nicht
        /// gepflegt ist — keine erfundene Zahl.
        /// </summary>
        [Fact]
        public void Ohne_Traeger_und_ohne_Brennstoff_bleibt_der_Faktor_leer()
        {
            if (!_db.Vorhanden) return;

            Emissionsfaktoren f = Emissionsquelle.Fuer(
                PROJEKT, 0, 0, DbWerte.EMISSION_MODUS_CO2);

            Assert.False(f.Co2Gepflegt);
            Assert.Equal(0.0, f.Co2GKwh, 9);
            Assert.Equal("-", f.Ebene);
        }

        // =================================================================================
        // 2 — Der Berechnungsmodus (Konzept F7)
        // =================================================================================

        /// <summary>
        /// Im Modus <c>CO2E</c> liefert die Quelle das ÄQUIVALENT der ausgewählten
        /// Arten statt des reinen CO₂ (F6/F7).
        ///
        /// <para>Der Auslieferungsstand hat dafür keine Wirkung — SO₂ und NOx tragen
        /// GWP 0, und CH₄/N₂O sind abgewählt. Der Fall legt deshalb selbst eine Lage
        /// an, in der sich die beiden Modi UNTERSCHEIDEN: Methan fossil wird
        /// ausgewählt und der Träger bekommt 1 000 mg/kWh davon; mit GWP₁₀₀ 29,8 sind
        /// das 1 g/kWh × 29,8 = 29,8 g CO₂e/kWh obendrauf. Danach wird beides wieder
        /// zurückgenommen.</para>
        /// </summary>
        [Fact]
        public void Der_Modus_CO2E_summiert_die_ausgewaehlten_Arten()
        {
            if (!_db.Vorhanden) return;

            int artId = Ganzzahl("SELECT id FROM emissionsart WHERE kuerzel = 'CH4_FOSSIL'");
            if (artId <= 0) return;

            try
            {
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE emissionsart SET ausgewaehlt = 1 WHERE id = ?",
                    new DbParam("@a", artId)));
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO emissionswert (emissionsart_id, carrier_id, wert, " +
                    "ist_co2e, ist_aktiv, ist_auslieferung) VALUES (?, ?, 1000, 0, 1, 0)",
                    new DbParam("@a", artId), new DbParam("@c", ERDGAS_E)));

                // Reines CO2 - unberuehrt vom neuen Methanwert.
                Assert.Equal(240.0, Emissionsquelle.Fuer(
                    PROJEKT, ERDGAS_E, 0, DbWerte.EMISSION_MODUS_CO2).Co2GKwh, 6);

                // Aequivalent - 240 + 1 000 mg/kWh (= 1 g/kWh) x GWP100 29,8.
                Assert.Equal(240.0 + 29.8, Emissionsquelle.Fuer(
                    PROJEKT, ERDGAS_E, 0, DbWerte.EMISSION_MODUS_CO2E).Co2GKwh, 6);
            }
            finally
            {
                DataRepository.ExecuteSQL(
                    "DELETE FROM emissionswert WHERE emissionsart_id = ? AND carrier_id = ? " +
                    "AND ist_auslieferung = 0 AND wert = 1000",
                    new DbParam("@a", artId), new DbParam("@c", ERDGAS_E));
                DataRepository.ExecuteSQL(
                    "UPDATE emissionsart SET ausgewaehlt = 0 WHERE id = ?",
                    new DbParam("@a", artId));
            }
        }

        // =================================================================================
        // 3 — Die Simulation nimmt DIESE Quelle
        // =================================================================================

        /// <summary>
        /// <b>Der Kessel.</b> Vor B1 stand hier der Wert aus
        /// <c>Tab_Brennstoff_Stamm</c>, unmittelbar über die Brennstoff-ID des Geräts
        /// gelesen. Jetzt steht der wirksame Faktor des Energieträgers — im Projekt
        /// 1030 mit demselben Zahlenwert 240 g/kWh, aber über die Kette und damit im
        /// Modus des Projekts und mit der Projektübersteuerung.
        /// </summary>
        [Fact]
        public void Der_Kessel_nimmt_den_Faktor_des_Energietraegers()
        {
            if (!_db.Vorhanden) return;

            var lauf = new SimulationRunner();
            string fehler;
            Assert.True(lauf.Simuliere(PROJEKT, out fehler), fehler);

            Assert.Equal(240.0, lauf.sim.simulation_spk.CO2_SPK[0], 4);
            Assert.Equal(0.3, lauf.sim.simulation_spk.SO2_SPK[0], 4);
            Assert.Equal(110.0, lauf.sim.simulation_spk.NOX_SPK[0], 4);
            Assert.Equal(0.5, lauf.sim.simulation_spk.Staub_SPK[0], 4);

            // Der Katalogeintrag des Kessels traegt CO2 = 10 (g/MWh) - er ist NICHT die
            // Quelle, und das ist der Kern des Befunds B1.
            Assert.NotEqual(10.0, lauf.sim.simulation_spk.CO2_SPK[0]);
        }

        /// <summary>
        /// <b>Das BHKW — die eigentliche Änderung.</b> Seine beiden Module tragen im
        /// Katalog <c>CO2 = 0</c>; vor B1 war die CO₂-Emission der BHKW-Stufe deshalb
        /// GENAU NULL, obwohl 1 048 MWh Erdgas verbrannt wurden. Jetzt gilt der Faktor
        /// des Trägers: 1 048,27 MWh × 240 g/kWh ÷ 1 000 = 251,58 t/a.
        /// </summary>
        [Fact]
        public void Das_BHKW_nimmt_den_Faktor_des_Energietraegers_statt_der_Geraetespalten()
        {
            if (!_db.Vorhanden) return;

            var lauf = new SimulationRunner();
            string fehler;
            Assert.True(lauf.Simuliere(PROJEKT, out fehler), fehler);

            double verbrauch = lauf.sim.simulation_bhkw.GasverbrauchBhkwMwh;
            Assert.True(verbrauch > 0, "Das Prüfprojekt hat keinen BHKW-Verbrauch mehr.");

            Assert.Equal(verbrauch * 240.0 / 1000.0, lauf.sim.simulation_bhkw.Em_CO2_BHKW, 2);
            Assert.True(lauf.sim.simulation_bhkw.Em_CO2_BHKW > 0,
                        "Vor W14a-E-8-B1 war diese Zahl 0 - genau das war der Befund.");
        }

        /// <summary>
        /// <b>Die Gegenprobe zu „nur Anzeige".</b> Die fünf Emissionsspalten von Kessel
        /// UND BHKW werden auf 999 999 gesetzt; das Ergebnis darf sich um keine Stelle
        /// ändern. Fällt der Fall eines Tages rot aus, rechnet wieder eine Gerätespalte
        /// mit — und die Kennzeichnung in <see cref="ParameterVerwendung"/> wäre falsch.
        /// </summary>
        [Fact]
        public void Die_Geraetespalten_haben_keine_Wirkung_mehr()
        {
            if (!_db.Vorhanden) return;

            var vorher = new SimulationRunner();
            string fehler;
            Assert.True(vorher.Simuliere(PROJEKT, out fehler), fehler);
            double kessel = vorher.sim.simulation_spk.Em_CO2_SPK;
            double bhkw = vorher.sim.simulation_bhkw.Em_CO2_BHKW;

            try
            {
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Tab_Heizkessel SET CO2 = 999999, SO2 = 999999, NOx = 999999, " +
                    "CO = 999999, Staub = 999999 WHERE ID_Projekt = ?",
                    new DbParam("@p", PROJEKT)));
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Tab_BHKW SET CO2 = 999999, SO2 = 999999, NOX = 999999, " +
                    "CO = 999999, Staub = 999999 WHERE ID_Projekt = ?",
                    new DbParam("@p", PROJEKT)));

                var nachher = new SimulationRunner();
                Assert.True(nachher.Simuliere(PROJEKT, out fehler), fehler);

                Assert.Equal(kessel, nachher.sim.simulation_spk.Em_CO2_SPK, 6);
                Assert.Equal(bhkw, nachher.sim.simulation_bhkw.Em_CO2_BHKW, 3);
            }
            finally
            {
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_Heizkessel SET CO2 = 10, SO2 = 0, NOx = 51, CO = 10, Staub = 0 " +
                    "WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_BHKW SET CO2 = 0, SO2 = 0, CO = 0, Staub = 0 " +
                    "WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            }
        }

        // =================================================================================
        // 4 — Die Autarkie-Kachel (W11a-O-2)
        // =================================================================================

        /// <summary>
        /// <b>W11a-O-2, eingelöst.</b> Der Netzstromfaktor der Kachel kommt aus dem
        /// Träger des Projekts — 1030 führt „Elektrische Energie" mit dem Projektwert
        /// 560 g/kWh. Vor dem Entscheid rechnete die Kachel unabänderlich mit
        /// 0,42 kg/kWh, gleichgültig was im Katalog stand.
        /// </summary>
        [Fact]
        public void Die_Autarkiekachel_nimmt_den_Netzstromtraeger_des_Projekts()
        {
            if (!_db.Vorhanden) return;

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(PROJEKT, DbWerte.EMISSION_MODUS_CO2);

            Assert.True(f.CarrierId > 0);
            Assert.Equal(560.0, f.Co2GKwh, 6);
            Assert.NotEqual(EmissionsVorgaben.CO2_NETZSTROM_KG_JE_KWH * 1000.0, f.Co2GKwh);

            // Und die Kachelrechnung selbst: 1 000 kWh x 0,560 kg/kWh aus dem Katalog.
            double nurStrom = EmissionsVorgaben.Co2ErsparnisKg(PROJEKT, 1000.0, 0.0);
            Assert.Equal(560.0, nurStrom, 6);
        }

        /// <summary>
        /// Die WÄRMESEITE nimmt den Träger des ersten Wärmeerzeugers — im Projekt 1030
        /// den Kessel am Erdgas E (240 g/kWh). Ohne einen solchen Erzeuger bleibt der
        /// dokumentierte Rückfall 200 g/kWh, also die bisherige Kachelzahl.
        /// </summary>
        [Fact]
        public void Die_Autarkiekachel_nimmt_den_Waermetraeger_des_Projekts()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(ERDGAS_E, Emissionsquelle.WaermeTraeger(PROJEKT));
            Assert.Equal(240.0,
                Emissionsquelle.Waerme(PROJEKT, DbWerte.EMISSION_MODUS_CO2).Co2GKwh, 6);

            // Ohne Projekt gibt es keinen Traeger - dann gilt der Rueckfall.
            Assert.Equal(Emissionsquelle.WAERME_RUECKFALL_G_JE_KWH,
                Emissionsquelle.Waerme(0, DbWerte.EMISSION_MODUS_CO2).Co2GKwh, 6);
            Assert.Equal(Emissionsquelle.NETZSTROM_RUECKFALL_G_JE_KWH,
                Emissionsquelle.Netzstrom(0, DbWerte.EMISSION_MODUS_CO2).Co2GKwh, 6);
        }

        // =================================================================================
        //  Hilfsmittel
        // =================================================================================

        private static int Ganzzahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToInt32(o); } catch { return 0; }
        }
    }
}
