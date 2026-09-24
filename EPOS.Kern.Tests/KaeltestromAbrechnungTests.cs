using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kältestrom in Kosten und Emissionen — Stufe KU2 Welle 3</b> (Kühlkonzept 6.1–6.4, 8.4;
    /// Entscheid E34, Konzept Gebäudesimulation N1.39): Schemaschritt 115, die Abrechnungsart an der
    /// Anlagenzeile samt Speicherweg, die Aufteilung des Netzbezugs im Lauf, die Bepreisung und
    /// Bewertung genau einmal im <c>KostenEmissionRechner</c> und die Kennzahlen der Kälteseite.
    ///
    /// <para><b>Phantasiewerte.</b> Kennlinie, Preise und Faktoren der Fälle sind runde, erfundene
    /// Zahlen — kein Produkt, keine Normzahl.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltestromAbrechnungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaeltestromAbrechnungTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>1045: Luft-Wasser-Wärmepumpe (Anlage 14924) als erster Erzeuger, Gaskessel, Photovoltaik, VDI-Gebäude 10651.</summary>
        private const int PROJEKT = 1045, WP = 1672044, ANLAGE = 14924, GEBAEUDE = 10651, GASTRAEGER = 63;

        /// <summary>Zwei Stromträger des Katalogs: der des Projekts (Heizbetrieb) und der der Kühlung.</summary>
        private const int PROJEKTTRAEGER = 60, KUEHLTRAEGER = 58;

        /// <summary>Phantasiepreise [€/kWh] und -faktoren [g/kWh].</summary>
        private const double PREIS_PROJEKT = 0.30, PREIS_KUEHLUNG = 0.20, PREIS_GAS = 0.08;
        private const double CO2_PROJEKT = 400.0, CO2_KUEHLUNG = 100.0;

        // =============================================================================
        //  Teil 1 — ohne Datenbank: Schritt 115, Anteilsregel, Abweichung, Anteile
        // =============================================================================

        /// <summary>
        /// Schritt 115: die Abrechnungsart als nullbarer Wahrheitswert OHNE Vorgabe an der
        /// Anlagenzeile (keine DDL-Vorgabe, kein NOT NULL — nicht vom Vorgabewert-Problem der
        /// Fachspaltenrettung betroffen), sieben Ergebnisspalten; keine davon in der Rückfallebene.
        /// </summary>
        [Fact]
        public void Schritt_115_Definitionen_nullbar_ohne_Vorgabe_und_nicht_in_der_Rueckfallebene()
        {
            Assert.True(SchemaStand.Zielversion >= 115);
            Assert.Equal("Kuehl_EigenerZaehler", KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER);
            Assert.Equal(SchemaKatalog.TAB_ENERGIEANLAGEN, KuehlungSchema.Abrechnungsspalte.Tabelle);
            string typ = StilleDb.SqliteSpaltenTyp(KuehlungSchema.Abrechnungsspalte.Name,
                                                   KuehlungSchema.Abrechnungsspalte.TypDefinition);
            Assert.Equal("INTEGER CHECK (\"Kuehl_EigenerZaehler\" IN (0,1))", typ);
            Assert.DoesNotContain("DEFAULT", typ, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NOT NULL", typ, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(7, KuehlungSchema.Kaelteerzeugerspalten.Length);
            Assert.Equal(8, KuehlungSchema.Schritt115Spalten().Count());
            var rueckfall = new HashSet<string>(SchemaKatalog.Alle.Select(s => s.Tabelle + "." + s.Name),
                                                StringComparer.OrdinalIgnoreCase);
            foreach (SchemaSpalte s in KuehlungSchema.Schritt115Spalten())
                Assert.DoesNotContain(s.Tabelle + "." + s.Name, rueckfall);
        }

        /// <summary>Die Abrechnungsart ist eine MODELLspalte: Die eine Einfügeanweisung nennt sie, Platzhalter und Parameter bleiben gleich viele.</summary>
        [Fact]
        public void Die_Einfuegeanweisung_der_Anlagenzeile_nennt_die_Abrechnungsart()
        {
            Assert.Contains("Kuehl_EigenerZaehler", AnlagenSql.SQL_ANLAGE_INSERT, StringComparison.Ordinal);
            int platzhalter = AnlagenSql.SQL_ANLAGE_INSERT.Count(c => c == '?');
            Assert.Equal(66, platzhalter);
            Assert.Equal(platzhalter, AnlagenSql.AnlagenParameter(1, new WErzeugerModel()).Length);

            Assert.Null(AnlagenSql.EigenerZaehlerOderNull(null));
            Assert.Null(AnlagenSql.EigenerZaehlerOderNull(false));
            Assert.Equal(1, AnlagenSql.EigenerZaehlerOderNull(true));
        }

        /// <summary>
        /// <b>Die Anteilsregel gegen die Handrechnung</b> (E34, Wahl 1): je Viertelstunde trägt der
        /// Kältestrom Netzbezug · Kältestrom / Stromverbrauch; ein Viertel ohne Netzbezug, ohne
        /// Verbrauch oder ohne Kältestrom trägt nichts; der Anteil ist höchstens 1.
        /// </summary>
        [Fact]
        public void Die_Anteilsregel_rechnet_je_Viertelstunde_wie_von_Hand()
        {
            var netz = new double[8];
            var verbrauch = new double[8];
            var kaelte = new double[2];

            // Stunde 0: 2 kW Netzbezug bei 4 kW Verbrauch, 1 kWh Kältestrom -> je Viertel 2·1/4/4 = 0,125 kWh.
            for (int q = 0; q < 4; q++) { netz[q] = 2.0; verbrauch[q] = 4.0; }
            kaelte[0] = 1.0;
            Assert.Equal(0.5, Kaeltekaskade.NetzbezugAnteilKwh(netz, verbrauch, kaelte), 12);

            // Stunde 1: zwei Viertel ohne Netzbezug (PV deckt), eines mit Anteil > 1 (gekappt).
            netz[4] = 0.0; netz[5] = 0.0; netz[6] = 3.0; netz[7] = 1.0;
            verbrauch[4] = 2.0; verbrauch[5] = 2.0; verbrauch[6] = 1.0; verbrauch[7] = 4.0;
            kaelte[1] = 2.0;
            double erwartet = 0.5 + 3.0 * 1.0 / 4.0 + 1.0 * 0.5 / 4.0;
            Assert.Equal(erwartet, Kaeltekaskade.NetzbezugAnteilKwh(netz, verbrauch, kaelte), 12);

            // Ohne Kältestrom nichts; nicht endliche Werte tragen nichts.
            Assert.Equal(0.0, Kaeltekaskade.NetzbezugAnteilKwh(netz, verbrauch, new double[2]));
            netz[0] = double.NaN; netz[1] = double.PositiveInfinity;
            Assert.Equal(erwartet - 0.25, Kaeltekaskade.NetzbezugAnteilKwh(netz, verbrauch, kaelte), 12);
            Assert.Equal(0.0, Kaeltekaskade.NetzbezugAnteilKwh(null, verbrauch, kaelte));
        }

        /// <summary>Nur ein ABWEICHENDER Kühlträger wirkt: NULL, 0 oder der Träger des Projekts heißen „wie Heizbetrieb".</summary>
        [Fact]
        public void Nur_ein_abweichender_Kuehltraeger_wirkt()
        {
            Assert.False(Kaeltestromabrechnung.Abweichend(null, 60));
            Assert.False(Kaeltestromabrechnung.Abweichend(0, 60));
            Assert.False(Kaeltestromabrechnung.Abweichend(60, 60));
            Assert.True(Kaeltestromabrechnung.Abweichend(58, 60));
            Assert.True(Kaeltestromabrechnung.Abweichend(58, 0));
        }

        /// <summary>
        /// Die Anteile des gespeicherten Ergebnisses — je Kühlträger und Abrechnungsart zusammengefasst;
        /// Module ohne abweichenden Kühlträger oder ohne Menge zählen nicht.
        /// </summary>
        [Fact]
        public void Die_Anteile_fassen_je_Traeger_und_Abrechnungsart_zusammen()
        {
            var m = new ErgebnisModel { Waermepumpe = new ErgebnisWaermepumpeModel() };
            m.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "A", Kaeltestrom_Netzbezug = 1.0, Kuehl_CarrierId = 58, Kuehl_EigenerZaehler = false });
            m.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "B", Kaeltestrom_Netzbezug = 2.0, Kuehl_CarrierId = 58, Kuehl_EigenerZaehler = null });
            m.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "C", Kaeltestrom_Netzbezug = 4.0, Kuehl_CarrierId = 58, Kuehl_EigenerZaehler = true });
            m.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "D", Kaeltestrom_Netzbezug = 8.0 });
            m.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel { Modul = "E", Kaeltestrom_Netzbezug = 0.0, Kuehl_CarrierId = 54 });

            List<Kaeltestromabrechnung.Anteil> a = Kaeltestromabrechnung.Anteile(m);
            Assert.Equal(2, a.Count);
            Assert.False(a[0].EigenerZaehler);
            Assert.Equal(3.0, a[0].MengeMwh);
            Assert.Equal(new[] { "A", "B" }, a[0].Anlagen);
            Assert.True(a[1].EigenerZaehler);
            Assert.Equal(4.0, a[1].MengeMwh);
            Assert.Equal(3.0, Kaeltestromabrechnung.AnteiligMwh(m));
            Assert.Equal(4.0, Kaeltestromabrechnung.EigenerZaehlerMwh(m));
            Assert.Equal(15.0, Kaeltestromabrechnung.NetzbezugKaeltestromMwh(m));
            Assert.Null(Kaeltestromabrechnung.NetzbezugKaeltestromMwh(new ErgebnisModel()));
            Assert.Empty(Kaeltestromabrechnung.Anteile(null));
        }

        // =============================================================================
        //  Teil 2 — die Testdatenbank auf Stand 115 und die Anlagenzeile
        // =============================================================================

        /// <summary>Alle acht Spalten stehen, alle NULL; die Prüfung weist eine 2 ab; STRICT bleibt.</summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_115_und_alles_ist_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= 115);
            Assert.True(KuehlungSchema.Schritt115Vollstaendig());
            foreach (SchemaSpalte s in KuehlungSchema.Schritt115Spalten())
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL"));

            try
            {
                DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET Kuehl_EigenerZaehler = 2 WHERE ID = " +
                                               ANLAGE.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception) { }
            Assert.True(Leer(DataRepository.ExecuteScalar("SELECT Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID = " +
                                                          ANLAGE.ToString(CultureInfo.InvariantCulture))));

            foreach (string t in new[] { "Tab_Energieanlagen", "Tab_ErgebnisWaermepumpe", "Tab_ErgebnisWaermepumpeModul" })
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
            }
        }

        /// <summary>Schritt 115 aus dem Stand davor: Die acht Spalten entstehen, der Bestand bleibt, ein zweiter Lauf legt nichts an.</summary>
        [Fact]
        public void Schritt_115_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            string bestand = Abdruck("SELECT ID, Bezeichner, ID_Carrier, Kuehl_ID_Carrier FROM Tab_Energieanlagen ORDER BY ID");
            foreach (SchemaSpalte s in KuehlungSchema.Schritt115Spalten())
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            Assert.False(KuehlungSchema.Schritt115Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(8, KuehlungSchema.Schritt115Alle(bericht));
            Assert.Contains(bericht, z => z.Contains("8 von 8 Spalte(n)"));
            Assert.True(KuehlungSchema.Schritt115Vollstaendig());
            Assert.Equal(bestand, Abdruck("SELECT ID, Bezeichner, ID_Carrier, Kuehl_ID_Carrier FROM Tab_Energieanlagen ORDER BY ID"));

            Assert.Equal(0, KuehlungSchema.Schritt115Alle(null));
        }

        /// <summary>
        /// Der Konfigurationsschreibweg: <c>true</c> schreibt 1, <c>false</c> NULL (die Vorgabe, nie 0),
        /// <c>null</c> lässt stehen; der Leser liefert jeden Stand NULL-erhaltend.
        /// </summary>
        [Fact]
        public void Der_Konfigurationsschreibweg_fuehrt_die_Abrechnungsart_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            Assert.Null(Anlage().Kuehl_EigenerZaehler);
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: KUEHLTRAEGER, KuehlEigenerZaehler: true)).Ok);
            Assert.Equal(true, Anlage().Kuehl_EigenerZaehler);
            Assert.Equal(1L, Zahl(ABRECHNUNG_DER_ANLAGE));

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(Heizstab: true)).Ok);
            Assert.Equal(true, Anlage().Kuehl_EigenerZaehler);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlEigenerZaehler: false)).Ok);
            Assert.Null(Anlage().Kuehl_EigenerZaehler);
            Assert.True(Leer(DataRepository.ExecuteScalar(ABRECHNUNG_DER_ANLAGE)));
        }

        /// <summary>
        /// <b>Der Speicherweg Löschen + Neuanlegen des Assistenten</b>
        /// (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> + <c>Add_WP_Waermeerzeuger</c>): Die
        /// Abrechnungsart reist als Modellspalte mit — sie ist keine Fachspalte und hängt nicht an
        /// deren Rettung —, NULL bleibt NULL, 1 bleibt 1.
        /// </summary>
        [Fact]
        public void Die_Abrechnungsart_ueberlebt_Loeschen_und_Neuanlegen_des_Assistenten()
        {
            if (!_db.Vorhanden) return;

            Assert.DoesNotContain(KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER, WizardCtrl.Fachspalten(),
                                  StringComparer.OrdinalIgnoreCase);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: KUEHLTRAEGER, KuehlEigenerZaehler: true)).Ok);
            WErzeugerModel m = Anlage();
            Speichern(m);
            Assert.Equal(1L, Zahl(ABRECHNUNG_DER_WAERMEPUMPE));
            Assert.Equal((long)KUEHLTRAEGER, Zahl(
                "SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID_Projekt = 1045 AND ID_Type = 1"));

            // Zweimal hintereinander - die neue Zeile ist wieder der Ausgang.
            Speichern(Neu());
            Assert.Equal(1L, Zahl(ABRECHNUNG_DER_WAERMEPUMPE));

            WErzeugerModel n = Neu();
            n.Kuehl_EigenerZaehler = null;
            Speichern(n);
            Assert.True(Leer(DataRepository.ExecuteScalar(ABRECHNUNG_DER_WAERMEPUMPE)));
        }

        /// <summary>Das Duplizieren eines Projekts trägt die Abrechnungsart mit.</summary>
        [Fact]
        public void Das_Duplizieren_traegt_die_Abrechnungsart_mit()
        {
            if (!_db.Vorhanden) return;

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: KUEHLTRAEGER, KuehlEigenerZaehler: true)).Ok);
            string quelle = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", PROJEKT)), CultureInfo.InvariantCulture);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(quelle, "E34-Probe Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(1L, Zahl("SELECT Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                                  kopie.ToString(CultureInfo.InvariantCulture) + " AND ID_Type = 1"));
        }

        // =============================================================================
        //  Teil 3 — Läufe: anteilig am Netzbezug und eigener Zähler (1045 mit Photovoltaik)
        // =============================================================================

        /// <summary>Ein Lauf samt Kosten- und Emissionsrechnung aus dem GESPEICHERTEN Ergebnis.</summary>
        private sealed class Stand
        {
            public SimulationRunner Lauf;
            public VariantenDaten Daten;
            public ErgebnisWaermepumpeModulModel Modul;
            public double NetzbezugMwh => Daten.Ergebnis.Energiebedarf.Stromrestbedarf;
            public double PvEigenMwh => Daten.Ergebnis.Photovoltaik == null ? 0.0
                : Daten.Ergebnis.Photovoltaik.Stromproduktion - Daten.Ergebnis.Photovoltaik.Ueberschuss;
        }

        private Stand Rechnen()
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));

            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.NotNull(erg);
            var v = new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            KennzahlenKatalog.Berechne(v);
            ErgebnisWaermepumpeModulModel modul = Assert.Single(erg.Waermepumpe.Module);
            return new Stand { Lauf = lauf, Daten = v, Modul = modul };
        }

        /// <summary>Richtet 1045 mit Kühlung, reversibler Wärmepumpe und zwei bepreisten Stromträgern ein.</summary>
        private void Einrichten()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24 WHERE ID = ?", new DbParam("@id", GEBAEUDE)));
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, true));
            foreach (var z in new[] { (18, 20, 5.5, 15.0), (18, 30, 4.5, 14.0), (18, 40, 3.5, 13.0) })
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) " +
                    "VALUES ((SELECT COALESCE(MAX(ID), 0) + 1 FROM Tab_Kenndaten_Kuehlung), ?, ?, ?, ?, ?, 100)",
                    new DbParam("@wp", WP), new DbParam("@v", z.Item1), new DbParam("@t", z.Item2),
                    new DbParam("@e", z.Item3), new DbParam("@p", z.Item4)));
            WPCtrl.SpeicherErgebnis e = WPCtrl.KuehlkonfigurationSchreiben(WP, PROJEKT, true, 18, 0.05);
            Assert.True(e.Ok, e.Meldung);

            Traeger(PROJEKTTRAEGER, PREIS_PROJEKT, CO2_PROJEKT);
            Traeger(KUEHLTRAEGER, PREIS_KUEHLUNG, CO2_KUEHLUNG);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_work = ?, custom_hi = 1.0 WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@w", PREIS_GAS), new DbParam("@p", PROJEKT), new DbParam("@c", GASTRAEGER)));
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(IdCarrier: PROJEKTTRAEGER)).Ok);
            Assert.Equal(PROJEKTTRAEGER, Kaeltestromabrechnung.Projekttraeger(PROJEKT));
        }

        /// <summary>
        /// <b>E34 an 1045 — ohne Kühlträger, anteilig am Netzbezug, eigener Zähler.</b>
        /// <list type="bullet">
        /// <item>Anteilig: Die Stufenrechnung bleibt Zeichen für Zeichen die ohne Kühlträger
        /// (Netzbezug, PV-Eigenverbrauch, Kältestrom, sein Netzbezug); nur die Bepreisung wechselt:
        /// Der Kühlträger trägt den Netzbezug des Kältestroms mit seinem Preis und Faktor, der
        /// Projektträger den Rest.</item>
        /// <item>Eigener Zähler: Der Kältestrom verlässt die Stufenrechnung — der Netzbezug des
        /// Anschlusses sinkt, der PV-Eigenverbrauch steigt nicht, und der ganze Kältestrom trägt den
        /// Kühlträger.</item>
        /// </list>
        /// </summary>
        [Fact]
        public void E34_anteilig_und_eigener_Zaehler_an_1045()
        {
            if (!_db.Vorhanden) return;
            Einrichten();

            // (a) ohne Kühlträger - Tarif und Faktor des Projekts.
            Stand ohne = Rechnen();
            Assert.NotNull(ohne.Lauf.simulation_Kaeltebedarf.Kaskade);
            Assert.True(ohne.Modul.Stromverbrauch_Kuehlung > 0);
            Assert.True(ohne.Modul.Kaeltestrom_Netzbezug > 0);
            Assert.True(ohne.Modul.Kaeltestrom_Netzbezug <= ohne.Modul.Stromverbrauch_Kuehlung + 1e-9);
            Assert.Null(ohne.Modul.Kuehl_CarrierId);
            Assert.Equal(0.0, ohne.Daten.StromkostenKuehltraeger);
            Assert.NotNull(ohne.Daten.Energiekosten);
            Assert.Equal(ohne.NetzbezugMwh * 1000.0 * PREIS_PROJEKT, ohne.Daten.StromkostenNetz.Value, 6);
            Assert.Equal(ohne.Modul.Kaeltestrom_Netzbezug.Value * 1000.0 * PREIS_PROJEKT, ohne.Daten.KaeltestromKosten.Value, 6);
            Assert.Equal(ohne.Modul.Kaeltestrom_Netzbezug.Value * CO2_PROJEKT / 1000.0, ohne.Daten.KaeltestromCO2t.Value, 9);

            // (b) anteilig am Netzbezug (Vorgabe, NULL).
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: KUEHLTRAEGER, KuehlEigenerZaehler: false)).Ok);
            Stand anteilig = Rechnen();
            Assert.Equal(ohne.Lauf.sim.ReststromMwh, anteilig.Lauf.sim.ReststromMwh);          // bitgleich
            Assert.Equal(ohne.NetzbezugMwh, anteilig.NetzbezugMwh);
            Assert.Equal(ohne.PvEigenMwh, anteilig.PvEigenMwh);
            Assert.Equal(ohne.Modul.Kaeltestrom_Netzbezug, anteilig.Modul.Kaeltestrom_Netzbezug);
            Assert.Equal(KUEHLTRAEGER, anteilig.Modul.Kuehl_CarrierId);
            Assert.Equal(false, anteilig.Modul.Kuehl_EigenerZaehler);

            double m = anteilig.Modul.Kaeltestrom_Netzbezug.Value;
            Assert.Equal(m, anteilig.Daten.NetzbezugKuehltraegerMWh, 12);
            Assert.Equal(0.0, anteilig.Daten.KuehlzaehlerMWh);
            Assert.Equal((anteilig.NetzbezugMwh - m) * 1000.0 * PREIS_PROJEKT, anteilig.Daten.StromkostenNetz.Value, 6);
            Assert.Equal(m * 1000.0 * PREIS_KUEHLUNG, anteilig.Daten.StromkostenKuehltraeger, 6);
            Assert.Equal(ohne.Daten.Energiekosten.Value - m * 1000.0 * (PREIS_PROJEKT - PREIS_KUEHLUNG),
                         anteilig.Daten.Energiekosten.Value, 6);
            Assert.Equal(ohne.Daten.CO2Gesamt.Value - m * (CO2_PROJEKT - CO2_KUEHLUNG) / 1000.0,
                         anteilig.Daten.CO2Gesamt.Value, 9);
            Assert.Equal(m * 1000.0 * PREIS_KUEHLUNG, anteilig.Daten.KaeltestromKosten.Value, 6);
            Assert.Equal(m * CO2_KUEHLUNG / 1000.0, anteilig.Daten.KaeltestromCO2t.Value, 9);
            Assert.Contains(anteilig.Daten.EnergiekostenJeAnlage, z => z.Anlage.StartsWith("Kältestrom", StringComparison.Ordinal) &&
                                                                     Math.Abs(z.MengeMWh - m) < 1e-12);
            Assert.Contains(anteilig.Lauf.Protokoll.Hinweise, h => h.Contains("anteilig am Netzbezug"));

            // (c) eigener Zähler.
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlEigenerZaehler: true)).Ok);
            Stand zaehler = Rechnen();
            double k = zaehler.Modul.Stromverbrauch_Kuehlung.Value;
            Assert.Equal(ohne.Modul.Stromverbrauch_Kuehlung, zaehler.Modul.Stromverbrauch_Kuehlung);   // dieselbe Kälteseite
            Assert.Equal(k, zaehler.Modul.Kaeltestrom_Netzbezug.Value);                           // ganz aus dem Netz
            Assert.Equal(true, zaehler.Modul.Kuehl_EigenerZaehler);
            Assert.True(zaehler.NetzbezugMwh < ohne.NetzbezugMwh, "Der Netzbezug des Anschlusses sinkt ohne den Kältestrom.");
            Assert.True(ohne.NetzbezugMwh - zaehler.NetzbezugMwh <= k + 0.01, "Mehr als der Kältestrom kann nicht wegfallen.");
            Assert.True(zaehler.PvEigenMwh <= ohne.PvEigenMwh + 1e-9, "Der Kältestrom darf den PV-Eigenverbrauch nicht erhöhen.");

            Assert.Equal(0.0, zaehler.Daten.NetzbezugKuehltraegerMWh);
            Assert.Equal(k, zaehler.Daten.KuehlzaehlerMWh, 12);
            Assert.Equal(zaehler.NetzbezugMwh * 1000.0 * PREIS_PROJEKT, zaehler.Daten.StromkostenNetz.Value, 6);
            Assert.Equal(k * 1000.0 * PREIS_KUEHLUNG, zaehler.Daten.StromkostenKuehltraeger, 6);
            Assert.Equal(k * 1000.0 * PREIS_KUEHLUNG, zaehler.Daten.KaeltestromKosten.Value, 6);
            Assert.Equal(k * CO2_KUEHLUNG / 1000.0, zaehler.Daten.KaeltestromCO2t.Value, 9);
            Assert.Contains(zaehler.Lauf.Protokoll.Hinweise, h => h.Contains("eigenen Zähler"));

            // Die Autarkie zählt den eigenen Zähler als Bezug.
            double strombedarf = zaehler.Daten.Ergebnis.Energiebedarf.Strombedarf_Gesamt;
            double autarkie = Math.Max(0.0, (1.0 - (zaehler.NetzbezugMwh + k) / strombedarf) * 100.0);
            Assert.Equal(autarkie, zaehler.Daten.Kennzahlen["eff.autarkie"].Value, 9);

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1045 E34: Kältestrom {0:F2} MWh; Netzbezug ohne/anteilig/Zähler {1:F2}/{2:F2}/{3:F2} MWh; " +
                "Netzbezug Kältestrom anteilig {4:F2} MWh; PV-Eigenverbrauch {5:F2}/{6:F2}/{7:F2} MWh; " +
                "Energiekosten {8:F0}/{9:F0}/{10:F0} €/a; CO₂ {11:F3}/{12:F3}/{13:F3} t/a; Kältestrom-Kosten {14:F0}/{15:F0}/{16:F0} €/a",
                k, ohne.NetzbezugMwh, anteilig.NetzbezugMwh, zaehler.NetzbezugMwh, m,
                ohne.PvEigenMwh, anteilig.PvEigenMwh, zaehler.PvEigenMwh,
                ohne.Daten.Energiekosten, anteilig.Daten.Energiekosten, zaehler.Daten.Energiekosten,
                ohne.Daten.CO2Gesamt, anteilig.Daten.CO2Gesamt, zaehler.Daten.CO2Gesamt,
                ohne.Daten.KaeltestromKosten, anteilig.Daten.KaeltestromKosten, zaehler.Daten.KaeltestromKosten));
        }

        /// <summary>
        /// Ein Kühlträger ohne Arbeitspreis ist eine Datenlücke, keine Aufforderung zum Rückfall: Die
        /// Energiekosten bleiben aus, und der Grund nennt den Träger.
        /// </summary>
        [Fact]
        public void Ein_Kuehltraeger_ohne_Arbeitspreis_laesst_die_Energiekosten_aus_und_nennt_ihn()
        {
            if (!_db.Vorhanden) return;
            Einrichten();
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE energy_project_settings SET custom_price_work = 0 WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", KUEHLTRAEGER)));
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: KUEHLTRAEGER)).Ok);

            Stand s = Rechnen();
            Assert.Null(s.Daten.Energiekosten);
            Assert.Null(s.Daten.KaeltestromKosten);
            Assert.Contains(Emissionsquelle.TraegerName(KUEHLTRAEGER), s.Daten.EnergiekostenGrund);
            Assert.StartsWith("Energiekosten nicht bestimmbar: Der Stromträger der Kühlung", s.Daten.EnergiekostenGrund);
        }

        /// <summary>
        /// <b>Die Kennzahlen der Kälteseite</b> (6.4, K15, K5): Deckungsgrad, Kälteerzeugung, JAZ Kälte,
        /// Kältestrom samt Netzbezug, Kosten und Emissionen stehen mit gerechneter Kälteerzeugung — und
        /// jede Beschriftung trägt die Grenze; ohne Kälteerzeuger fehlt jede Kältezahl der Gruppe.
        /// </summary>
        [Fact]
        public void Die_Kennzahlen_der_Kaelteseite_stehen_nur_mit_Kaelteerzeugung()
        {
            if (!_db.Vorhanden) return;
            Einrichten();
            Stand s = Rechnen();
            ErgebnisWaermepumpeModel w = s.Daten.Ergebnis.Waermepumpe;

            Assert.Equal(w.Kaelteproduktion_WP.Value / w.Stromverbrauch_Kuehlung.Value,
                         s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_JAZ].Value, 12);
            Assert.Equal(w.Stromverbrauch_Kuehlung, s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_STROM]);
            Assert.Equal(w.Kaelteproduktion_WP, s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_ERZEUGUNG]);
            Assert.NotNull(s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD]);
            Assert.NotNull(s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_REST]);
            Assert.NotNull(s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_NETZBEZUG]);
            Assert.NotNull(s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_KOSTEN]);
            Assert.NotNull(s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_CO2]);
            Assert.Equal(KennzahlenKatalog.DeckungKanalKaelte(s.Daten),
                         s.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD]);

            foreach (Kennzahl kz in KennzahlenKatalog.Alle(DbWerte.EMISSION_MODUS_CO2E)
                         .Where(x => x.Schluessel.StartsWith("kaelte.", StringComparison.Ordinal)))
            {
                Assert.Contains("(sensibel)", kz.LabelDe);
                Assert.Contains("(sensible)", kz.LabelEn);
            }
            Assert.All(KennzahlenKatalog.Alle(DbWerte.EMISSION_MODUS_CO2).Where(x => x.Gruppe == KennzahlenKatalog.GR_KAELTE),
                       kz => Assert.StartsWith("kaelte.", kz.Schluessel, StringComparison.Ordinal));

            // Ohne Kälteerzeuger (die Wärmepumpe kühlt nicht): keine Zahl der Gruppe Kälte.
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP, PROJEKT, false, null, null).Ok);
            Stand ohne = Rechnen();
            Assert.Null(ohne.Daten.Ergebnis.Waermepumpe.Kaelteproduktion_WP);
            foreach (Kennzahl kz in KennzahlenKatalog.Alle(DbWerte.EMISSION_MODUS_CO2).Where(x => x.Gruppe == KennzahlenKatalog.GR_KAELTE))
                Assert.Null(ohne.Daten.Kennzahlen[kz.Schluessel]);
            Assert.Null(ohne.Daten.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD]);
        }

        // -----------------------------------------------------------------------------

        private const string ABRECHNUNG_DER_ANLAGE =
            "SELECT Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID = 14924";

        private const string ABRECHNUNG_DER_WAERMEPUMPE =
            "SELECT Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID_Projekt = 1045 AND ID_Type = 1";

        private static void Traeger(int carrierId, double preis, double co2)
        {
            DataRepository.ExecuteSQL("DELETE FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", carrierId));
            object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM energy_project_settings");
            int id = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max, CultureInfo.InvariantCulture)) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID, ID_Projekt, [ID_Energieträger], " +
                "custom_hi, custom_price_work, custom_price_base, custom_price_power, co2) " +
                "VALUES (?, ?, ?, 1.0, ?, 0.0, 0.0, ?)",
                new DbParam("@id", id), new DbParam("@p", PROJEKT), new DbParam("@c", carrierId),
                new DbParam("@w", preis), new DbParam("@co2", co2)));
        }

        /// <summary>Der Speicherweg des Assistenten für die Wärmepumpen von 1045.</summary>
        private static void Speichern(WErzeugerModel m)
        {
            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.WP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, new List<WErzeugerModel> { m }));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = 1045 AND ID_Type = 1"));
        }

        /// <summary>Die (neu angelegte) Wärmepumpenzeile von 1045, gelesen über den Leser des Modells.</summary>
        private static WErzeugerModel Neu()
        {
            var c = new WErzeugerCtrl();
            c.ReadSingle("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = 1045 AND ID_Type = 1");
            return c;
        }

        private static WErzeugerModel Anlage()
        {
            var c = new WErzeugerCtrl();
            c.ReadSingle("SELECT * FROM Tab_Energieanlagen WHERE ID = " + ANLAGE.ToString(CultureInfo.InvariantCulture));
            return c;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static bool Leer(object wert) => wert == null || wert == DBNull.Value;

        private static string Abdruck(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            var sb = new System.Text.StringBuilder();
            foreach (DataRow r in dt.Rows)
                sb.Append(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))))
                  .Append('\n');
            return sb.ToString();
        }
    }
}
