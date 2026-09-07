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
    /// <para><b>Warum es hier geprüft wird — und seit Em‑9.8 auch im Referenzlauf.</b>
    /// Bis zum 07.09.2026 standen die Emissionswerte der Simulation in KEINER
    /// Referenz-CSV: Weder <c>aggregate.csv</c> noch eine Vektordatei führte eine
    /// Emissionsgröße, und <c>Tab_Ergebnis*</c> hat bis heute keine Emissionsspalte
    /// (Weg B des Konzepts, 11.2.1, ist ausdrücklich abgelehnt — eine gespeicherte
    /// Emissionszahl liefe gegen den Bericht auseinander). Der Referenzlauf war nach
    /// dem Umbau B1 deshalb byte-gleich, und diese Probe war der EINZIGE Nachweis.
    /// <b>Mit dem Anwenderentscheid Em‑9.8 (07.09.2026, „Empfehlung") schreibt
    /// <c>Referenzlauf/Ergebnisexport.cs</c> die zehn Jahressummen als Skalare
    /// <c>Em.Kessel.*</c> / <c>Em.Bhkw.*</c></b>; die Basis
    /// <c>2026-09-07_R5_Zahlenrand</c> friert sie ein. Beide Nachweise bleiben und
    /// prüfen Verschiedenes: Diese Probe prüft die KETTE (welcher Faktor gilt und
    /// warum), der Referenzlauf prüft die ZAHL am Ende von 8 760 Stunden. Abschnitt 5
    /// unten hält die Naht zwischen beiden.</para>
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
        // 5 — Die zehn Skalare des Referenzexports (Em-9.8)
        // =================================================================================

        /// <summary>
        /// <b>Der Nachweis zu Em‑9.8‑Q2.</b> Projekt 1030 fährt Kessel UND BHKW; alle
        /// zehn Größen entstehen, und acht davon sind ≠ 0. Die zwei CO-Größen sind 0 —
        /// nicht aus Versehen, sondern weil es die Emissionsart <c>CO</c> nicht gibt
        /// (Em‑9.9: „bewusst nicht", bis eine Quelle mit CO-Faktoren je Energieträger
        /// vorliegt). Genau deshalb stehen sie trotzdem im Export: So ÄNDERT ein
        /// späterer Trägerwert eine Zahl, statt einen Schlüssel hinzuzufügen.
        /// </summary>
        [Fact]
        public void Die_zehn_Emissionsgroessen_entstehen_und_acht_davon_sind_ungleich_null()
        {
            if (!_db.Vorhanden) return;

            var lauf = new SimulationRunner();
            string fehler;
            Assert.True(lauf.Simuliere(PROJEKT, out fehler), fehler);

            Assert.True(lauf.sim.bSimulationKessel && lauf.sim.simulation_spk != null,
                        "Prüfprojekt ohne Kesselstufe - der Fall prüft dann die halbe Sache.");
            Assert.True(lauf.sim.bSimulationBHKW && lauf.sim.simulation_bhkw != null,
                        "Prüfprojekt ohne BHKW-Stufe - der Fall prüft dann die halbe Sache.");

            SimulationSPK spk = lauf.sim.simulation_spk;
            SimulationBHKW bh = lauf.sim.simulation_bhkw;

            Assert.True(spk.Em_CO2_SPK    > 0, "Em.Kessel.Co2T ist 0.");
            Assert.True(spk.Em_SO2_SPK    > 0, "Em.Kessel.So2Kg ist 0.");
            Assert.True(spk.Em_NOX_SPK    > 0, "Em.Kessel.NoxKg ist 0.");
            Assert.True(spk.Em_Staub_SPK  > 0, "Em.Kessel.StaubKg ist 0.");
            Assert.True(bh.Em_CO2_BHKW    > 0, "Em.Bhkw.Co2T ist 0.");
            Assert.True(bh.Em_SO2_BHKW    > 0, "Em.Bhkw.So2Kg ist 0.");
            Assert.True(bh.Em_NOX_BHKW    > 0, "Em.Bhkw.NoxKg ist 0.");
            Assert.True(bh.Em_Staub_BHKW  > 0, "Em.Bhkw.StaubKg ist 0.");

            // Em-9.9: keine Emissionsart "CO", also strukturell 0 - auf beiden Seiten.
            Assert.Equal(0.0, spk.Em_CO_SPK, 9);
            Assert.Equal(0.0, bh.Em_CO_BHKW, 9);
            Assert.Equal(0, Ganzzahl("SELECT count(*) FROM emissionsart WHERE kuerzel = 'CO'"));
        }

        /// <summary>
        /// <b>Der Wächter über die Naht.</b> Jeder der zehn Schlüssel steht in
        /// <c>Referenzlauf/Ergebnisexport.cs</c>, und zwar gebunden an SEIN Feld: Ein
        /// vertauschtes Paar (<c>Em.Kessel.NoxKg</c> an <c>Em_SO2_SPK</c>) fiele im
        /// Referenzvergleich nie auf, weil beide Zahlen von da an einfach so
        /// dastünden. Geprüft wird der Quelltext, weil die Testprojekte das Werkzeug
        /// nicht referenzieren — dieselbe Bauart wie <see cref="DoubleWacheTests"/>.
        ///
        /// <para>Mitgeprüft wird die Einheit im Namen (Em‑9.8‑Q3): CO₂ endet auf
        /// <c>T</c> (t/a), die vier übrigen auf <c>Kg</c> (kg/a) — und im Export steht
        /// kein Faktor 1 000, der die eine in die andere umrechnete.</para>
        /// </summary>
        [Fact]
        public void Der_Export_bindet_jeden_der_zehn_Schluessel_an_sein_Feld()
        {
            string quelle = Ergebnisexportquelle();

            var paare = new (string Schluessel, string Feld)[]
            {
                ("Em.Kessel.Co2T",    "Em_CO2_SPK"),
                ("Em.Kessel.So2Kg",   "Em_SO2_SPK"),
                ("Em.Kessel.NoxKg",   "Em_NOX_SPK"),
                ("Em.Kessel.CoKg",    "Em_CO_SPK"),
                ("Em.Kessel.StaubKg", "Em_Staub_SPK"),
                ("Em.Bhkw.Co2T",      "Em_CO2_BHKW"),
                ("Em.Bhkw.So2Kg",     "Em_SO2_BHKW"),
                ("Em.Bhkw.NoxKg",     "Em_NOX_BHKW"),
                ("Em.Bhkw.CoKg",      "Em_CO_BHKW"),
                ("Em.Bhkw.StaubKg",   "Em_Staub_BHKW")
            };

            foreach (var p in paare)
            {
                string zeile = System.Linq.Enumerable.FirstOrDefault(
                    quelle.Replace("\r\n", "\n").Split('\n'),
                    z => z.Contains("\"" + p.Schluessel + "\"", StringComparison.Ordinal));

                Assert.True(zeile != null, "Der Schluessel " + p.Schluessel + " fehlt im Export.");
                Assert.Contains(p.Feld, zeile, StringComparison.Ordinal);
            }

            // Die Bedingung nach 11.2.2: ohne gelaufene Stufe KEIN Schluessel.
            Assert.Contains("sim.bSimulationKessel && sim.simulation_spk != null", quelle, StringComparison.Ordinal);
            Assert.Contains("sim.bSimulationBHKW && sim.simulation_bhkw != null", quelle, StringComparison.Ordinal);
        }

        // =================================================================================
        //  Hilfsmittel
        // =================================================================================

        /// <summary>
        /// Der Quelltext von <c>Referenzlauf/Ergebnisexport.cs</c> — derselbe Weg zur
        /// Wurzel des Arbeitsbaums wie in <see cref="DoubleWacheTests"/>. Die Datei ist
        /// EINE Fassung für drei Werkzeuge (<c>Referenzlauf</c>, <c>EPOS.Referenzlauf</c>
        /// und den iOS-Prüfmodus); wer sie ändert, ändert alle drei.
        /// </summary>
        private static string Ergebnisexportquelle(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            string ordner = System.IO.Path.GetDirectoryName(eigeneDatei);
            string wurzel = ordner == null ? null : System.IO.Path.GetDirectoryName(ordner);
            Assert.True(wurzel != null && System.IO.File.Exists(System.IO.Path.Combine(wurzel, "WP-Plan.sln")),
                        "Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");

            string datei = System.IO.Path.Combine(wurzel, "Referenzlauf", "Ergebnisexport.cs");
            Assert.True(System.IO.File.Exists(datei), "Datei nicht gefunden: " + datei);
            return System.IO.File.ReadAllText(datei);
        }

        private static int Ganzzahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToInt32(o); } catch { return 0; }
        }
    }
}
