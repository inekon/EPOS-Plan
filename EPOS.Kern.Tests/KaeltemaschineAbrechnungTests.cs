using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KU3-4d — die Kältestromabrechnung der Kältemaschine</b> (Kühlkonzept 6.1–6.3; Schemaschritt 184): Anteile,
    /// eigener Zähler mit Grund- und Leistungspreis, Netzbezug, Szenario und Emissionen nach dem Muster der Wärmepumpe;
    /// der erneuerte Stempeltrigger; die geteilte Projektkopie; Kältespeicher in Bericht, Navigator und Präsenz.
    ///
    /// <para><b>Phantasiewerte.</b> Preise und Faktoren sind runde, erfundene Zahlen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineAbrechnungTests : IDisposable
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
        private const double PREIS_KUEHLUNG = 0.20, CO2_KUEHLUNG = 100.0, GRUND = 120.0, LEISTUNG = 100.0;

        // =============================================================================
        //  Teil 1 — ohne Datenbank
        // =============================================================================

        private static ErgebnisModel Probe()
        {
            var m = new ErgebnisModel
            {
                Waermepumpe = new ErgebnisWaermepumpeModel(),
            };
            m.Waermepumpe.Module.Add(new ErgebnisWaermepumpeModulModel
            {
                Modul = "WP", Kaeltestrom_Netzbezug = 1.0, Stromverbrauch_Kuehlung = 1.5,
            });
            m.Kaeltemaschinen.Add(new ErgebnisKaeltemaschineModel
            {
                Bezeichner = "KM anteilig", Stromverbrauch_MWh = 3.0, Kaeltestrom_Netzbezug_MWh = 2.0,
                Kuehl_CarrierId = 58, Kuehl_EigenerZaehler = false, Stromspitze_kW = 4.0,
            });
            m.Kaeltemaschinen.Add(new ErgebnisKaeltemaschineModel
            {
                Bezeichner = "KM Zähler", Anzahl = 2, Stromverbrauch_MWh = 5.0, Kaeltestrom_Netzbezug_MWh = 5.0,
                Kuehl_CarrierId = 58, Kuehl_EigenerZaehler = true, Stromspitze_kW = 7.5,
            });
            m.Kaeltemaschinen.Add(new ErgebnisKaeltemaschineModel
            {
                Bezeichner = "KM alt", Stromverbrauch_MWh = 1.0,   // Lauf vor Schritt 184: keine Abrechnungswerte
            });
            return m;
        }

        [Fact]
        public void Die_Abrechnung_nimmt_die_Kaeltemaschinen_nach_den_Waermepumpen_mit()
        {
            ErgebnisModel m = Probe();
            List<Kaeltestromabrechnung.Anteil> a = Kaeltestromabrechnung.Anteile(m);
            Assert.Equal(2, a.Count);
            Assert.Equal(2.0, a.Single(x => !x.EigenerZaehler).MengeMwh, 12);
            Assert.Equal(new[] { "KM anteilig" }, a.Single(x => !x.EigenerZaehler).Anlagen);
            Assert.Equal(5.0, a.Single(x => x.EigenerZaehler).MengeMwh, 12);
            Assert.Equal(2.0, Kaeltestromabrechnung.AnteiligMwh(m), 12);
            Assert.Equal(5.0, Kaeltestromabrechnung.EigenerZaehlerMwh(m), 12);
            Assert.Equal(1.0 + 2.0 + 5.0, Kaeltestromabrechnung.NetzbezugKaeltestromMwh(m).Value, 12);

            Kaeltestromabrechnung.Zaehler z = Assert.Single(Kaeltestromabrechnung.EigeneZaehler(m));
            Assert.Equal(Kaeltestromabrechnung.SchluesselKaeltemaschine(1), z.Modulindex);
            Assert.Equal(-2, z.Modulindex);
            Assert.Equal("KM Zähler", z.Anlage);
            Assert.Equal(7.5, z.StromspitzeKw);
            Assert.Equal(58, z.Traeger);

            Assert.Equal(6.0, Kaeltestromabrechnung.Stundenspitze(new[] { 1.0, 6.0, 2.0 }));
            Assert.Null(Kaeltestromabrechnung.Stundenspitze(null));

            // Ohne Wärmepumpe rechnet die Kältemaschine allein.
            m.Waermepumpe = null;
            Assert.Equal(7.0, Kaeltestromabrechnung.NetzbezugKaeltestromMwh(m).Value, 12);
            Assert.Null(Kaeltestromabrechnung.NetzbezugKaeltestromMwh(new ErgebnisModel()));
        }

        [Fact]
        public void Ein_Mengenszenario_skaliert_die_Kaeltemaschinen()
        {
            ErgebnisModel m = Probe();
            ErgebnisModel k = SzenarioMengen.Ergebnis(m, 1.1);
            Assert.Equal(3, k.Kaeltemaschinen.Count);
            ErgebnisKaeltemaschineModel z = k.Kaeltemaschinen[1];
            Assert.Equal(5.5, z.Kaeltestrom_Netzbezug_MWh.Value, 12);
            Assert.Equal(5.5, z.Stromverbrauch_MWh, 12);
            Assert.Equal(7.5 * 1.1, z.Stromspitze_kW.Value, 12);
            Assert.Equal(58, z.Kuehl_CarrierId);
            Assert.Equal(true, z.Kuehl_EigenerZaehler);
            Assert.Equal(2, z.Anzahl);
            Assert.Null(k.Kaeltemaschinen[2].Kaeltestrom_Netzbezug_MWh);
            Assert.Equal(5.5, Kaeltestromabrechnung.EigenerZaehlerMwh(k), 12);
        }

        [Fact]
        public void Die_Kaeltespeichertafel_fuehrt_je_Kaeltespeicher_eine_Zeile()
        {
            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            var stamm = new VariantenDaten { Ergebnis = new ErgebnisModel() };
            stamm.Ergebnis.Pufferspeicher.Add(new ErgebnisPufferspeicherModel
            {
                Bezeichner = "Puffer", Verwendung = SimulationPufferspeicher.VERWENDUNG_HEIZUNG, Q_max = 100, T_oben_Mittel = 55,
            });
            Assert.True(Berichtstabellen.Kaeltespeicher(stamm, false, de).IstLeer);
            stamm.Ergebnis.Pufferspeicher.Add(new ErgebnisPufferspeicherModel
            {
                Bezeichner = "Kaltwasser", Verwendung = SimulationPufferspeicher.VERWENDUNG_KAELTE, Q_max = 139.2,
                Ladung_gesamt = 12000, Entladung_gesamt = 11000, Verluste_gesamt = 900, Vollzyklen = 86.2, T_oben_Mittel = 8,
            });
            Berichtstabelle t = Berichtstabellen.Kaeltespeicher(stamm, false, de);
            Tabellenzeile z = Assert.Single(t.Zeilen);
            Assert.Equal("Kaltwasser", z.Zellen[0].Text);
            Assert.Equal(139.2, z.Zellen[1].Zahl.Value, 9);
            Assert.Equal(12.0, z.Zellen[2].Zahl.Value, 9);
            Assert.Equal(11.0, z.Zellen[3].Zahl.Value, 9);
            Assert.Equal(0.9, z.Zellen[4].Zahl.Value, 9);
            Assert.Equal(86.2, z.Zellen[5].Zahl.Value, 9);
            Assert.Equal("Wärmeeintrag [MWh/a]", t.Kopf.Zellen[4].Text);
            Assert.Equal("Heat gain [MWh/a]", Berichtstabellen.Kaeltespeicher(stamm, true, de).Kopf.Zellen[4].Text);

            // Die Speichertemperaturen nennen die Rolle des Kältespeichers.
            Berichtstabelle temp = Berichtstabellen.Speichertemperaturen(stamm, false, de);
            Assert.Equal(new[] { "Puffer", "Kaltwasser (Kältespeicher)" }, temp.Zeilen.Select(r => r.Zellen[0].Text));

            // Feldkatalog: Fassung 12, Kapitel Projektbeschreibung, Schalter dahinter.
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde("tabelle.kaeltespeicher");
            Assert.Equal(12, f.Seit);
            Assert.Equal(Vorlagenfeldart.Tabelle, f.Art);
            Assert.Contains("tabelle.kaeltespeicher", Vorlagenfeldkatalog.Finde("kapitel.projekt").Deckt);
        }

        // =============================================================================
        //  Teil 2 — mit Datenbank (Arbeitskopie von 1017)
        // =============================================================================

        private static int Stamm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", bezeichner)),
            CultureInfo.InvariantCulture);

        private static long Zahl(string sql, params object[] werte) => Convert.ToInt64(DataRepository.ExecuteScalar(sql,
            werte.Select(w => new DbParam("?", w)).ToArray()), CultureInfo.InvariantCulture);

        /// <summary>Eine Kältemaschine mit kleiner Leistung, damit sie neben der Wärmepumpe läuft.</summary>
        private static KaeltemaschineAnlageModel Maschine()
        {
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, Stamm(LUFTGEKUEHLT), "KM Halle");
            KaeltemaschineAnlageModel a = KaeltemaschineAnlageCtrl.Laden(anlage);
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_KENNDATEN + " SET " +
                KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " = " + KaeltemaschineSchema.SPALTE_KAELTELEISTUNG + " / 20 WHERE " +
                KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " = ?", new DbParam("?", a.IdKaeltemaschine.Value));
            DataRepository.ExecuteNonQuery("UPDATE " + KaeltemaschineSchema.TAB_PROJEKT + " SET " +
                KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG + " = 2.5 WHERE ID = ?", new DbParam("?", a.IdKaeltemaschine.Value));
            return a;
        }

        /// <summary>Ein Stromträger des Katalogs, der nicht der des Projekts ist, mit Phantasiepreisen im Projekt.</summary>
        private static int Kuehltraeger(int projekttraeger)
        {
            int traeger = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT id FROM energy_carrier WHERE pricing_model = 'ELECTRICITY' AND id <> ? ORDER BY id LIMIT 1",
                new DbParam("?", projekttraeger)), CultureInfo.InvariantCulture);
            DataRepository.ExecuteSQL("DELETE FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", PROJEKT), new DbParam("@c", traeger));
            int id = (int)Zahl("SELECT COALESCE(MAX(ID), 0) + 1 FROM energy_project_settings");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID, ID_Projekt, [ID_Energieträger], " +
                "custom_hi, custom_price_work, custom_price_base, custom_price_power, co2) VALUES (?, ?, ?, 1.0, ?, ?, ?, ?)",
                new DbParam("@id", id), new DbParam("@p", PROJEKT), new DbParam("@c", traeger),
                new DbParam("@w", PREIS_KUEHLUNG), new DbParam("@g", GRUND), new DbParam("@l", LEISTUNG),
                new DbParam("@co2", CO2_KUEHLUNG)));
            return traeger;
        }

        private sealed class Stand
        {
            public SimulationRunner Lauf;
            public ErgebnisModel Ergebnis;
            public ErgebnisKaeltemaschineModel Maschine;
            public VariantenDaten Rechne(bool mitReihen)
            {
                var v = new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = Ergebnis,
                                             Zeitreihen = mitReihen ? ZeitreihenExtraktor.AusLauf(Lauf) : null };
                KostenEmissionRechner.Berechne(v);
                return v;
            }
        }

        private static Stand Rechnen()
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            return new Stand { Lauf = lauf, Ergebnis = erg, Maschine = Assert.Single(erg.Kaeltemaschinen) };
        }

        [Fact]
        public void Kuehltraeger_der_Kaeltemaschine_anteilig_und_mit_eigenem_Zaehler_an_1017()
        {
            if (!_db.Vorhanden) return;
            int projekttraeger = Kaeltestromabrechnung.Projekttraeger(PROJEKT);
            KaeltemaschineAnlageModel a = Maschine();
            int kuehl = Kuehltraeger(projekttraeger);
            Assert.Equal(projekttraeger, Kaeltestromabrechnung.Projekttraeger(PROJEKT));

            // (a) ohne Kühlträger: der Kältestrom der Maschine läuft am Netzbezug des Projekts.
            Stand ohne = Rechnen();
            Assert.True(ohne.Maschine.Stromverbrauch_MWh > 0, "Der Fall braucht Kältestrom der Maschine.");
            Assert.True(ohne.Maschine.Kaeltestrom_Netzbezug_MWh.HasValue);
            Assert.Null(ohne.Maschine.Kuehl_CarrierId);
            Assert.True(ohne.Maschine.Stromspitze_kW > 0);
            VariantenDaten vo = ohne.Rechne(false);
            double total = Kaeltestromabrechnung.NetzbezugKaeltestromMwh(ohne.Ergebnis).Value;
            Assert.Equal(total, vo.KaeltestromNetzbezugMWh.Value, 9);
            Assert.True(total >= ohne.Maschine.Kaeltestrom_Netzbezug_MWh.Value);
            Assert.Equal(0.0, vo.NetzbezugKuehltraegerMWh);
            double stromCO2 = vo.KaeltestromCO2t.Value * 1000.0 / total;

            // (b) anteilig: dieselbe Stufenrechnung, der Kühlträger bepreist den Netzbezug der Maschine.
            a.KuehlIdCarrier = kuehl;
            a.KuehlEigenerZaehler = false;
            Assert.Null(KaeltemaschineAnlageCtrl.Speichern(a));
            Stand anteilig = Rechnen();
            Assert.Equal(ohne.Lauf.sim.ReststromMwh, anteilig.Lauf.sim.ReststromMwh);
            Assert.Equal(kuehl, anteilig.Maschine.Kuehl_CarrierId);
            Assert.Equal(false, anteilig.Maschine.Kuehl_EigenerZaehler);
            double m = anteilig.Maschine.Kaeltestrom_Netzbezug_MWh.Value;
            Assert.True(m > 0);
            VariantenDaten va = anteilig.Rechne(false);
            Assert.Equal(m, va.NetzbezugKuehltraegerMWh, 9);
            Assert.Equal(m * 1000.0 * PREIS_KUEHLUNG, va.StromkostenKuehltraeger, 6);
            Assert.Equal(total, va.KaeltestromNetzbezugMWh.Value, 9);
            Assert.Equal(m * CO2_KUEHLUNG / 1000.0 + (total - m) * stromCO2 / 1000.0, va.KaeltestromCO2t.Value, 9);
            Assert.Contains(va.EnergiekostenJeAnlage, z => z.Anlage.Contains("KM Halle") && Math.Abs(z.MengeMWh - m) < 1e-9);
            EndenergieAufloeser.Groesse g = EndenergieAufloeser.FuerProjekt(PROJEKT)
                .FuerPosition(KaeltemaschineAnlageSchema.KOMPONENTE_KAELTEMASCHINE, a.AnlagenId);
            Assert.True(g.EigenerStromtraeger);
            Assert.Equal(anteilig.Maschine.Stromverbrauch_MWh * 1000.0 * PREIS_KUEHLUNG, g.KostenEuro.Value, 3);

            // (c) eigener Zähler: der Kältestrom verlässt die Stufenrechnung und trägt Grund- und Leistungspreis.
            a.KuehlEigenerZaehler = true;
            Assert.Null(KaeltemaschineAnlageCtrl.Speichern(a));
            Stand zaehler = Rechnen();
            double k = zaehler.Maschine.Kaeltestrom_Netzbezug_MWh.Value;
            Assert.Equal(zaehler.Maschine.Stromverbrauch_MWh, k, 3);
            Assert.True(zaehler.Lauf.sim.ReststromMwh < ohne.Lauf.sim.ReststromMwh, "Der Netzbezug des Anschlusses sinkt.");
            double spitze = zaehler.Maschine.Stromspitze_kW.Value;
            Assert.True(spitze > 0);

            VariantenDaten mitReihen = zaehler.Rechne(true);
            Netzbezugsspitze eigene = mitReihen.Zeitreihen.Kaeltestromspitzen[Kaeltestromabrechnung.SchluesselKaeltemaschine(0)];
            Assert.InRange(k, zaehler.Maschine.Stromverbrauch_MWh - 0.006, zaehler.Maschine.Stromverbrauch_MWh + 0.006);   // ganz aus dem Netz (gerundet gespeichert)
            Assert.Equal(k, mitReihen.KuehlzaehlerMWh, 9);
            Assert.Equal(k * 1000.0 * PREIS_KUEHLUNG + GRUND + LEISTUNG * eigene.JahrKW, mitReihen.StromkostenKuehltraeger, 6);
            Assert.Contains(mitReihen.EnergiekostenJeAnlage, z => z.Einheit == "kW" && z.Anlage.Contains("KM Halle"));

            // Ohne Zeitreihen trägt die gespeicherte Jahresspitze den Satz je Jahr.
            VariantenDaten ohneReihen = zaehler.Rechne(false);
            Assert.Equal(k * 1000.0 * PREIS_KUEHLUNG + GRUND + LEISTUNG * spitze, ohneReihen.StromkostenKuehltraeger, 6);
            Assert.Equal(k * CO2_KUEHLUNG / 1000.0 + (ohneReihen.KaeltestromNetzbezugMWh.Value - k) * stromCO2 / 1000.0,
                         ohneReihen.KaeltestromCO2t.Value, 6);

            // Szenario: Menge und Spitze folgen dem Faktor.
            VariantenDaten kopie = SzenarioMengen.Variante(new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = zaehler.Ergebnis }, 1.1);
            KostenEmissionRechner.Berechne(kopie);
            Assert.Equal(k * 1.1, kopie.KuehlzaehlerMWh, 9);
            Assert.Equal(k * 1.1 * 1000.0 * PREIS_KUEHLUNG + GRUND + LEISTUNG * spitze * 1.1, kopie.StromkostenKuehltraeger, 6);

            // Bericht: die Tafel der Kälteerzeuger nennt Netzbezug und Träger der Maschine.
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(zaehler.Ergebnis.Waermepumpe, Emissionsquelle.TraegerName, false,
                                                                CultureInfo.GetCultureInfo("de-DE"), zaehler.Ergebnis.Kaeltemaschinen);
            Tabellenzeile km = t.Zeilen.Single(r => r.Zellen[0].Text.StartsWith("KM Halle", StringComparison.Ordinal));
            Assert.Equal(k, km.Zellen[4].Zahl.Value, 6);
            Assert.Equal(ProjektbeschreibungBaustein.KuehltraegerText(kuehl, true, Emissionsquelle.TraegerName), km.Zellen[5].Text);
        }

        [Fact]
        public void Schritt_184_ist_wiederholbar_und_erneuert_einen_alten_Trigger()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(184, KaeltestromabrechnungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaeltestromabrechnungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, Paketanhebung.Stufen.Single(x => x.Nr == KaeltestromabrechnungSchema.SCHRITT).Wirkung);
            Assert.True(KaeltestromabrechnungSchema.Vollstaendig());
            Assert.Equal(0, KaeltestromabrechnungSchema.Ausfuehren(null));

            // Der Trigger in der Form vor dem Schritt (ohne Anzahl und Gerät) wird ersetzt.
            string name = KaeltestromabrechnungSchema.TriggerName;
            DataRepository.ExecuteNonQuery("DROP TRIGGER \"" + name + "\"");
            DataRepository.ExecuteNonQuery("CREATE TRIGGER \"" + name + "\" AFTER UPDATE OF \"ID_Carrier\" ON \"Tab_Energieanlagen\" " +
                "FOR EACH ROW BEGIN UPDATE \"Tab_Projekt\" SET \"Kosten_Geaendert\" = datetime('now','localtime') " +
                "WHERE \"ID\" IN (OLD.\"ID_Projekt\", NEW.\"ID_Projekt\"); END");
            Assert.False(KaeltestromabrechnungSchema.TriggerAktuell());
            Assert.Equal(2, KaeltestromabrechnungSchema.Ausfuehren(null));
            Assert.True(KaeltestromabrechnungSchema.Vollstaendig());
            Assert.Equal(0, KaeltestromabrechnungSchema.Ausfuehren(null));
            Assert.True(KostenStempelSchema.Vollstaendig());
        }

        [Fact]
        public void Anzahl_und_Geraet_der_Kaeltemaschine_stempeln_die_Kosten_des_Projekts()
        {
            if (!_db.Vorhanden) return;
            KaeltemaschineAnlageModel a = Maschine();
            Func<long> gestempelt = () => Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NOT NULL", PROJEKT);
            Action leeren = () => DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL");

            leeren();
            Assert.Equal(0L, gestempelt());
            a.Anzahl = 3;
            Assert.Null(KaeltemaschineAnlageCtrl.Speichern(a));
            Assert.Equal(1L, gestempelt());

            leeren();
            DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET ID_Kaeltemaschine = ID_Kaeltemaschine WHERE ID = ?",
                new DbParam("?", a.AnlagenId));
            Assert.Equal(1L, gestempelt());

            // Die Gerätetabelle stempelt nicht (wie Tab_WP) - sie wirkt über den Lauf.
            leeren();
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine SET Kuehl_Vorlauf = 10 WHERE ID = ?",
                new DbParam("?", a.IdKaeltemaschine.Value));
            Assert.Equal(0L, gestempelt());
        }

        [Fact]
        public void Zwei_Anlagen_desselben_Katalogsatzes_teilen_eine_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            int stamm = Stamm(LUFTGEKUEHLT);
            int erste = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Nord");
            int zweite = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Süd");
            KaeltemaschineAnlageModel a = KaeltemaschineAnlageCtrl.Laden(erste), b = KaeltemaschineAnlageCtrl.Laden(zweite);
            Assert.NotEqual(erste, zweite);
            Assert.Equal(a.IdKaeltemaschine, b.IdKaeltemaschine);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + KaeltemaschineSchema.TAB_PROJEKT + " WHERE ID_Projekt = ?", PROJEKT));

            // Vorlauf und Hilfsstrom gelten je Gerät (an der Kopie), die Anzahl je Anlagenzeile.
            a.Anzahl = 2;
            a.KuehlVorlauf = 12;
            Assert.Null(KaeltemaschineAnlageCtrl.Speichern(a));
            KaeltemaschineAnlageModel b2 = KaeltemaschineAnlageCtrl.Laden(zweite);
            Assert.Equal(12.0, b2.KuehlVorlauf);
            Assert.Equal(1, b2.Anzahl);

            SimulationRunner lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);
            List<ErgebnisKaeltemaschineModel> erg = new ErgebnisCtrl().Load(PROJEKT).Kaeltemaschinen;
            Assert.Equal(new[] { "KM Nord", "KM Süd" }, erg.Select(e => e.Bezeichner));
            Assert.Equal(new[] { 2, 1 }, erg.Select(e => e.Anzahl));

            // Löschen: die Kopie bleibt, solange eine Anlage sie führt.
            KaeltemaschineAnlageCtrl.Loeschen(erste);
            Assert.NotNull(KaeltemaschineCtrl.Laden(b.IdKaeltemaschine.Value));
            KaeltemaschineAnlageCtrl.Loeschen(zweite);
            Assert.Null(KaeltemaschineCtrl.Laden(b.IdKaeltemaschine.Value));
        }

        [Fact]
        public void Der_Kaeltespeicher_steht_in_Navigator_Praesenz_und_Bericht()
        {
            if (!_db.Vorhanden) return;
            Maschine();
            int id = PufferSpCtrl.ProjektPufferAnlegen(PROJEKT, "Kaltwasserspeicher Test", "", "", 20000, 2.0, 0,
                                                       DbWerte.PSP_VERWENDUNG_KAELTE, 6, 12, 10, 95, null, 0);
            Assert.True(id > 0);
            SimulationRunner lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, fehler);

            List<SimulationPufferspeicher> alle = lauf.sim.SpeicherSamtKaelte();
            Assert.Equal(lauf.sim.AlleSpeicher().Count + 1, alle.Count);
            SimulationPufferspeicher kalt = alle.Last();
            Assert.Equal(id, kalt.ID_Pufferspeicher);
            Assert.Equal("PUFFER_" + id.ToString(CultureInfo.InvariantCulture), kalt.Schluessel(alle.Count - 1));
            Assert.Equal(alle.Count, alle.Select((s, i) => s.Schluessel(i)).Distinct().Count());
            Assert.Equal(8760, kalt.SOC_stuendlich.Length);
            Assert.True(ErgebnisPraesenz.Ermitteln(lauf.sim).Speicher);
            Assert.Equal("Kaltwasserspeicher Test (" + WindowsFormsApplication1.MyResource.Resource.PSP_ROLLE_KAELTESPEICHER + ")",
                         SimulationErgebnisHuelle.SpeicherLegende(kalt));

            var stamm = new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = new ErgebnisCtrl().Load(PROJEKT) };
            Berichtstabelle t = Berichtstabellen.Kaeltespeicher(stamm, false, CultureInfo.GetCultureInfo("de-DE"));
            Tabellenzeile z = Assert.Single(t.Zeilen);
            Assert.Equal("Kaltwasserspeicher Test", z.Zellen[0].Text);
            Assert.Equal(kalt.Q_max, z.Zellen[1].Zahl.Value, 1);
            Assert.True(z.Zellen[3].Zahl.Value > 0);
        }
    }
}
