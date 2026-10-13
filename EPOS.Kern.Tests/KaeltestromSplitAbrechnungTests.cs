using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt 224 — die Abrechnung des Split-Stroms ohne Datenbank</b> (Konzept Kältebedarf 4.2; E34/E35): Kühlträger
    /// nur abweichend, eigener Zähler neben der Stufenrechnung, die Split-Zeilen in der Abrechnung nach den Kältemaschinen
    /// mit eigenem Schlüsselbereich, die Szenariokopie samt der Spalten aus 213 und die Zeile der Kälteerzeugertafel.
    /// </summary>
    public class KaeltestromSplitAbrechnungTests
    {
        private const int STUNDEN = 8760;
        private const int PROJEKTTRAEGER = 7, KUEHLTRAEGER = 58;

        private static double[] Konstant(double w) => Enumerable.Repeat(w, STUNDEN).ToArray();

        private static Kaeltedeckung Profil(string name, double eer, int? traeger, bool zaehler, int id) =>
            Kaeltedeckung.AusProfilzeile(new Z_ProjektKaeltebedarfModel
            {
                ID_Z = id, Bezeichner = name, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_FEST,
                Eer1 = eer, Taussen1 = 35, KuehlIdCarrier = traeger, KuehlEigenerZaehler = zaehler,
            });

        private static Kaeltedeckung Lastgang(string name, int? traeger, bool zaehler, int id) =>
            Kaeltedeckung.AusLastgang(new Z_ProjWaermebedarfModel
            {
                m_ID_Z = id, m_szBezeichner = name, Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25,
                KuehlIdCarrier = traeger, KuehlEigenerZaehler = zaehler,
            });

        private static SimulationKaeltebedarf Fassade()
        {
            SimulationProtokoll.NeuStarten();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(new Kanalsatz(), true);
            kaelte.Aussentemperatur = Enumerable.Range(0, STUNDEN).Select(h => 20.0 + 10.0 * (h % 3)).ToArray();
            return kaelte;
        }

        [Fact]
        public void Die_Zeile_traegt_ihre_Zuordnung_und_nur_einen_abweichenden_Kuehltraeger()
        {
            SimulationKaeltebedarf kaelte = Fassade();
            kaelte.BedarfBuchen(Konstant(1.0), Lastgang("Vorgabe", null, true, 11), false);           // Projektträger
            kaelte.BedarfBuchen(Konstant(0.5), Profil("Gleich", 3.0, PROJEKTTRAEGER, true, 21), true); // derselbe Träger
            kaelte.BedarfBuchen(Konstant(0.3), Profil("Zaehler", 2.5, KUEHLTRAEGER, true, 22), true);
            kaelte.BedarfBuchen(Konstant(0.2), Profil("Anteilig", 4.0, KUEHLTRAEGER, false, 23), true);
            kaelte.KuehltraegerFestlegen(PROJEKTTRAEGER);

            List<SimulationKaeltebedarf.DezentraleZeile> z = kaelte.DezentraleZeilen;
            Assert.Equal(new int?[] { 11, 21, 22, 23 }, z.Select(x => x.IdZuordnung));
            Assert.Equal(new[] { false, true, true, true }, z.Select(x => x.Profil));
            Assert.Equal(new[] { 0, 0, KUEHLTRAEGER, KUEHLTRAEGER }, z.Select(x => x.Kuehltraeger));
            Assert.Equal(new[] { false, false, true, false }, z.Select(x => x.NebenDerStufenrechnung));
            Assert.Equal(0.3 * STUNDEN / 2.5, z[2].StromKwh, 6);
            Assert.Equal(2.5, z[2].JahresEer, 9);
            Assert.Equal(0.3, z[2].KaelteMaxKw, 12);

            // Die Stufenrechnung bekommt alle Zeilen außer der mit eigenem Zähler.
            double[] stufe = kaelte.StromDerStufenrechnung(out double summe);
            double erwartet = z[0].StromKwh + z[1].StromKwh + z[3].StromKwh;
            Assert.Equal(erwartet, summe, 6);
            Assert.Equal(erwartet, stufe.Sum(), 6);
            for (int h = 0; h < STUNDEN; h++)
                Assert.Equal(kaelte.Strom_Dezentral[h] - z[2].Strom_stuendlich[h], stufe[h], 9);
        }

        [Fact]
        public void Ohne_Zeile_neben_der_Stufenrechnung_ist_die_Reihe_Zeichen_fuer_Zeichen_der_ganze_Split_Strom()
        {
            SimulationKaeltebedarf kaelte = Fassade();
            kaelte.BedarfBuchen(Konstant(0.7), Lastgang("A", null, false, 1), false);
            kaelte.BedarfBuchen(Konstant(0.4), Profil("B", 3.3, KUEHLTRAEGER, false, 2), true);
            kaelte.KuehltraegerFestlegen(PROJEKTTRAEGER);

            double[] stufe = kaelte.StromDerStufenrechnung(out double summe);
            Assert.NotSame(kaelte.Strom_Dezentral, stufe);
            Assert.Equal(kaelte.Strom_Dezentral, stufe);                         // bitgleich
            double s = 0.0;
            for (int h = 0; h < STUNDEN; h++) s += kaelte.Strom_Dezentral[h];
            Assert.Equal(s, summe);
        }

        [Fact]
        public void Die_Mengenmeldung_nennt_Zeile_Netzbezug_Strom_und_Traeger()
        {
            using var kultur = new Kulturvorrichtung();
            var z = new SimulationKaeltebedarf.DezentraleZeile { Bezeichner = "Kühlraum", StromKwh = 4000.0, NetzbezugKwh = 2500.0 };
            string text = SimulationControl.KuehltraegerMengeHinweis(z, "Strom 2");
            Assert.Contains("Kühlraum", text);
            Assert.Contains("2,50", text);
            Assert.Contains("4,00", text);
            Assert.Contains("Strom 2", text);
        }

        private static ErgebnisModel Probe()
        {
            var m = new ErgebnisModel();
            m.Kaeltemaschinen.Add(new ErgebnisKaeltemaschineModel
            {
                Bezeichner = "KM", Stromverbrauch_MWh = 3.0, Kaeltestrom_Netzbezug_MWh = 2.0,
                Kuehl_CarrierId = KUEHLTRAEGER, Kuehl_EigenerZaehler = true, Stromspitze_kW = 4.0,
            });
            m.KaelteDezentral.Add(new ErgebnisKaelteDezentralModel
            {
                Quelle = KaeltebedarfSplitAbrechnungSchema.QUELLE_LASTGANG, Bezeichner = "Split Projekt", Kaelte_MWh = 6.0,
                Strom_MWh = 2.0, Kaeltestrom_Netzbezug_MWh = 1.5,
            });
            m.KaelteDezentral.Add(new ErgebnisKaelteDezentralModel
            {
                Quelle = KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL, ID_Zuordnung = 5, Bezeichner = "Split Zähler",
                Kaelte_MWh = 9.0, Strom_MWh = 3.0, Jahres_EER = 3.0, Kaelte_Spitze_kW = 2.0, Kaeltestrom_Netzbezug_MWh = 3.0,
                Kuehl_CarrierId = KUEHLTRAEGER, Kuehl_EigenerZaehler = true, Stromspitze_kW = 0.8,
            });
            m.KaelteDezentral.Add(new ErgebnisKaelteDezentralModel
            {
                Quelle = KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL, Bezeichner = "Split anteilig", Kaelte_MWh = 4.0,
                Strom_MWh = 1.0, Kaeltestrom_Netzbezug_MWh = 0.6, Kuehl_CarrierId = KUEHLTRAEGER, Kuehl_EigenerZaehler = false,
                Stromspitze_kW = 0.3,
            });
            return m;
        }

        [Fact]
        public void Die_Abrechnung_nimmt_die_Split_Zeilen_nach_den_Kaeltemaschinen_mit()
        {
            ErgebnisModel m = Probe();
            List<Kaeltestromabrechnung.Anteil> a = Kaeltestromabrechnung.Anteile(m);
            Kaeltestromabrechnung.Anteil zaehler = a.Single(x => x.EigenerZaehler);
            Assert.Equal(2.0 + 3.0, zaehler.MengeMwh, 12);
            Assert.Equal(new[] { "KM", "Split Zähler" }, zaehler.Anlagen);
            Assert.Equal(0.6, a.Single(x => !x.EigenerZaehler).MengeMwh, 12);
            Assert.Equal(0.6, Kaeltestromabrechnung.AnteiligMwh(m), 12);
            Assert.Equal(5.0, Kaeltestromabrechnung.EigenerZaehlerMwh(m), 12);
            Assert.Equal(2.0 + 1.5 + 3.0 + 0.6, Kaeltestromabrechnung.NetzbezugKaeltestromMwh(m).Value, 12);

            List<Kaeltestromabrechnung.Zaehler> z = Kaeltestromabrechnung.EigeneZaehler(m);
            Assert.Equal(2, z.Count);
            Assert.Equal(Kaeltestromabrechnung.SchluesselKaeltemaschine(0), z[0].Modulindex);
            Assert.Equal(Kaeltestromabrechnung.SchluesselDezentral(1), z[1].Modulindex);
            Assert.Equal("Split Zähler", z[1].Anlage);
            Assert.Equal(0.8, z[1].StromspitzeKw);
            Assert.Equal(3.0, z[1].MengeMwh, 12);

            // Der Schlüsselbereich: nie ein Modulplatz, nie eine Kältemaschine.
            Assert.Equal(-1_000_001, Kaeltestromabrechnung.SchluesselDezentral(0));
            for (int i = 0; i < 1000; i++)
                Assert.True(Kaeltestromabrechnung.SchluesselDezentral(i) < Kaeltestromabrechnung.SchluesselKaeltemaschine(999_999));

            // Ein Lauf ohne Split-Zeile rechnet wie bisher.
            m.KaelteDezentral.Clear();
            Assert.Equal(2.0, Kaeltestromabrechnung.NetzbezugKaeltestromMwh(m).Value, 12);
            Assert.Single(Kaeltestromabrechnung.EigeneZaehler(m));
        }

        [Fact]
        public void Ein_Mengenszenario_skaliert_Split_Zeilen_und_behaelt_die_Spalten_aus_213()
        {
            ErgebnisModel m = Probe();
            m.Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Kaeltebedarf_Gesamt = 20.0, KaeltebedarfProfilMwh = 13.0, KaeltebedarfDezentralMwh = 19.0,
                KaelteDezentralSpitzeKw = 4.5, StromDezentralMwh = 6.0, JahresEerDezentral = 19.0 / 6.0,
            };
            ErgebnisModel k = SzenarioMengen.Ergebnis(m, 1.1);

            Assert.Equal(13.0 * 1.1, k.Energiebedarf.KaeltebedarfProfilMwh.Value, 12);
            Assert.Equal(19.0 * 1.1, k.Energiebedarf.KaeltebedarfDezentralMwh.Value, 12);
            Assert.Equal(6.0 * 1.1, k.Energiebedarf.StromDezentralMwh.Value, 12);
            Assert.Equal(4.5, k.Energiebedarf.KaelteDezentralSpitzeKw);
            Assert.Equal(19.0 / 6.0, k.Energiebedarf.JahresEerDezentral);

            Assert.Equal(3, k.KaelteDezentral.Count);
            ErgebnisKaelteDezentralModel d = k.KaelteDezentral[1];
            Assert.Equal(9.0 * 1.1, d.Kaelte_MWh, 12);
            Assert.Equal(3.0 * 1.1, d.Strom_MWh, 12);
            Assert.Equal(3.0 * 1.1, d.Kaeltestrom_Netzbezug_MWh.Value, 12);
            Assert.Equal(0.8 * 1.1, d.Stromspitze_kW.Value, 12);
            Assert.Equal(2.0, d.Kaelte_Spitze_kW);
            Assert.Equal(3.0, d.Jahres_EER);
            Assert.Equal(KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL, d.Quelle);
            Assert.Equal(5, d.ID_Zuordnung);
            Assert.Equal(KUEHLTRAEGER, d.Kuehl_CarrierId);
            Assert.Equal(true, d.Kuehl_EigenerZaehler);
            Assert.Null(k.KaelteDezentral[0].Kuehl_CarrierId);
            Assert.Equal(5.0 * 1.1, Kaeltestromabrechnung.EigenerZaehlerMwh(k), 12);

            // Ohne Wert bleibt null („nicht erhoben").
            m.Energiebedarf = new ErgebnisEnergiebedarfModel();
            Assert.Null(SzenarioMengen.Ergebnis(m, 1.1).Energiebedarf.KaeltebedarfProfilMwh);
        }

        [Fact]
        public void Die_Kaelteerzeugertafel_fuehrt_je_Split_Zeile_Netzbezug_und_Traeger()
        {
            ErgebnisModel m = Probe();
            CultureInfo de = CultureInfo.GetCultureInfo("de-DE");
            Func<int, string> name = id => "Träger " + id.ToString(CultureInfo.InvariantCulture);
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(null, name, false, de, null, null, m.KaelteDezentral);
            Assert.False(t.IstLeer);
            Assert.Equal(3, t.Zeilen.Count);
            Tabellenzeile z = t.Zeilen.Single(r => r.Zellen[0].Text == "Split Zähler (Split)");
            Assert.Equal(9.0, z.Zellen[1].Zahl.Value, 9);
            Assert.Equal(3.0, z.Zellen[3].Zahl.Value, 9);
            Assert.Equal(3.0, z.Zellen[4].Zahl.Value, 9);
            Assert.Equal(ProjektbeschreibungBaustein.KuehltraegerText(KUEHLTRAEGER, true, name), z.Zellen[5].Text);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BER_KAELTE_TRAEGER_PROJEKT,
                         t.Zeilen.Single(r => r.Zellen[0].Text == "Split Projekt (Split)").Zellen[5].Text);

            // Ohne Split-Zeile und ohne Erzeuger bleibt die Tafel leer wie bisher.
            Assert.True(Berichtstabellen.Kaelteerzeuger(null, name, false, de).IstLeer);
        }
    }
    /// <summary>
    /// <b>Schritt 224 im Lauf</b> auf einer Kopie der Testdatenbank (Projekt 1017 mit Kühlbetrieb, Tabelle 224 angelegt):
    /// eine Profilzeile mit abweichendem Kühlträger und eigenem Zähler läuft neben der Stufenrechnung, eine Lastgangzeile mit
    /// abweichendem Träger anteilig; Ergebniszeilen je Split-Zeile im Rundweg, Mengenmeldung, Kosten mit Grund- und
    /// Leistungspreis, Szenario, Bericht und Export — und der Projektträger als Vorgabe samt Warnkriterium.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltestromSplitAbrechnungDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string KUEHLRAUM = "Kühlraum";
        private const string LASTGANG = "K6a_Kuehlung";
        private const double PREIS_KUEHLUNG = 0.20, CO2_KUEHLUNG = 100.0, GRUND = 120.0, LEISTUNG = 100.0;
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static long Zahl(string sql, params object[] werte) => Convert.ToInt64(DataRepository.ExecuteScalar(sql,
            werte.Select(w => new DbParam("?", w)).ToArray()), CultureInfo.InvariantCulture);

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

        private static void ZeilenAnlegen(int kuehl)
        {
            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", KUEHLRAUM)), CultureInfo.InvariantCulture);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 12, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35, KuehlIdCarrier = kuehl, KuehlEigenerZaehler = true,
                },
            }));
            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(LASTGANG,
                Enumerable.Range(0, 8760).Select(h => 0.2 + 0.3 * ((h / 24) % 2)).ToList()));
            List<Z_ProjWaermebedarfModel> zeilen = Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT);
            zeilen.Add(new Z_ProjWaermebedarfModel
            {
                m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25,
                KuehlIdCarrier = kuehl, KuehlEigenerZaehler = false,
            });
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(PROJEKT));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(PROJEKT, zeilen));
        }

        private static SimulationRunner Rechnen(out ErgebnisModel erg)
        {
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            erg = new ErgebnisCtrl().Load(PROJEKT);
            return lauf;
        }

        /// <summary>Ist der Hinweis die Mengenmeldung einer Split-Zeile? Der feste Anfang der Vorlage vor {0} unterscheidet sie.</summary>
        private static bool Mengenmeldung(string hinweis)
        {
            string vorlage = WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_DEZENTRAL_KUEHLTRAEGER;
            return hinweis.StartsWith(vorlage.Substring(0, vorlage.IndexOf("{0}", StringComparison.Ordinal)), StringComparison.Ordinal);
        }

        [Fact]
        public void Split_Strom_je_Traeger_und_Zaehler_von_Lauf_bis_Bericht()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(1, KaeltebedarfSplitAbrechnungSchema.Ausfuehren(null));
            int projekttraeger = Kaeltestromabrechnung.Projekttraeger(PROJEKT);
            int kuehl = Kuehltraeger(projekttraeger);
            bool leistungVorher = KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT);
            ZeilenAnlegen(kuehl);
            Assert.True(KostenEmissionRechner.StromLeistungspreisGepflegt(PROJEKT),
                "Der eigene Zähler der Split-Zeile verlangt die Zeitreihen des Laufs (vorher: " + leistungVorher + ").");
            Assert.DoesNotContain(Warnkriterien.PruefeProjekt(PROJEKT),
                                  b => b.Kriterium == Warnkriterien.DEZENTRALE_KAELTE_ZAEHLER_OHNE_TRAEGER);

            SimulationRunner lauf = Rechnen(out ErgebnisModel erg);
            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.Equal(2, k.DezentraleZeilen.Count);
            SimulationKaeltebedarf.DezentraleZeile lg = k.DezentraleZeilen[0], pr = k.DezentraleZeilen[1];
            Assert.False(lg.Profil);
            Assert.True(pr.Profil);
            Assert.Equal(kuehl, lg.Kuehltraeger);
            Assert.False(lg.NebenDerStufenrechnung);
            Assert.True(pr.NebenDerStufenrechnung);

            // Stufenrechnung: der Profilstrom mit eigenem Zähler läuft daneben, der Lastgangstrom darin.
            Assert.Equal(lg.StromKwh, lauf.sim.Kaeltestrom_Dezentral_Stufenrechnung_stuendlich.Sum(), 3);
            Assert.Equal(lg.StromKwh + pr.StromKwh, lauf.sim.Kaeltestrom_Dezentral_stuendlich.Sum(), 3);
            double kaskade = k.Kaskade == null ? 0.0
                : k.Kaskade.Erzeuger.Where(e => !e.NebenDerStufenrechnung).Sum(e => e.StromGesamtKwh);
            Assert.Equal(kaskade + lg.StromKwh, lauf.sim.Kaeltestrom_Stufenrechnung_stuendlich.Sum(), 3);
            Assert.Equal(pr.StromKwh, pr.NetzbezugKwh, 9);
            Assert.InRange(lg.NetzbezugKwh, 0.0, lg.StromKwh + 1e-9);
            Assert.Equal(2, lauf.Protokoll.Hinweise.Count(Mengenmeldung));
            Assert.Contains(lauf.Protokoll.Hinweise, h => Mengenmeldung(h) && h.Contains(KUEHLRAUM));

            // Rundweg über Tab_ErgebnisKaelteDezentral (gerundet gespeichert wie jede Ergebnisspalte).
            Assert.Equal(2, erg.KaelteDezentral.Count);
            for (int i = 0; i < 2; i++)
            {
                ErgebnisKaelteDezentralModel d = erg.KaelteDezentral[i];
                SimulationKaeltebedarf.DezentraleZeile z = k.DezentraleZeilen[i];
                Assert.Equal(z.Profil ? KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL : KaeltebedarfSplitAbrechnungSchema.QUELLE_LASTGANG,
                             d.Quelle);
                Assert.True(z.IdZuordnung > 0);
                Assert.Equal(z.IdZuordnung, d.ID_Zuordnung);
                Assert.Equal(z.Bezeichner, d.Bezeichner);
                Assert.Equal(Math.Round(z.KaelteKwh / 1000.0, 2), d.Kaelte_MWh, 9);
                Assert.Equal(Math.Round(z.StromKwh / 1000.0, 2), d.Strom_MWh, 9);
                Assert.Equal(Math.Round(z.KaelteMaxKw, 2), d.Kaelte_Spitze_kW.Value, 9);
                Assert.Equal(Math.Round(z.JahresEer, 2), d.Jahres_EER.Value, 9);
                Assert.Equal(Math.Round(z.NetzbezugKwh / 1000.0, 2), d.Kaeltestrom_Netzbezug_MWh.Value, 9);
                Assert.Equal(kuehl, d.Kuehl_CarrierId);
                Assert.Equal(z.EigenerZaehler, d.Kuehl_EigenerZaehler);
                Assert.Equal(Math.Round(Kaeltestromabrechnung.Stundenspitze(z.Strom_stuendlich).Value, 2), d.Stromspitze_kW.Value, 9);
            }
            Assert.Equal(KaeltebedarfSplitAbrechnungSchema.QUELLE_LASTGANG, erg.KaelteDezentral[0].Quelle);

            // Kosten: Arbeitspreis auf beide Mengen, Grundpreis und Leistungspreis auf die eigene Spitze der Profilzeile.
            double nbLg = erg.KaelteDezentral[0].Kaeltestrom_Netzbezug_MWh.Value, nbPr = erg.KaelteDezentral[1].Kaeltestrom_Netzbezug_MWh.Value;
            var v = new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = erg, Zeitreihen = ZeitreihenExtraktor.AusLauf(lauf) };
            KostenEmissionRechner.Berechne(v);
            Netzbezugsspitze eigene = v.Zeitreihen.Kaeltestromspitzen[Kaeltestromabrechnung.SchluesselDezentral(1)];
            Assert.False(v.Zeitreihen.Kaeltestromspitzen.ContainsKey(Kaeltestromabrechnung.SchluesselDezentral(0)));
            Assert.Equal(nbPr, v.KuehlzaehlerMWh, 9);
            Assert.Equal((nbLg + nbPr) * 1000.0 * PREIS_KUEHLUNG + GRUND + LEISTUNG * eigene.JahrKW, v.StromkostenKuehltraeger, 6);
            Assert.Contains(v.EnergiekostenJeAnlage, z => z.Einheit == "kW" && z.Anlage.Contains(KUEHLRAUM));
            Assert.Contains(v.EnergiekostenJeAnlage, z => z.Anlage.Contains(LASTGANG) && Math.Abs(z.MengeMWh - nbLg) < 1e-9);

            // Ohne Zeitreihen trägt die gespeicherte Spitze den Satz je Jahr; das Szenario skaliert Menge und Spitze.
            var ohne = new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = erg };
            KostenEmissionRechner.Berechne(ohne);
            double spitze = erg.KaelteDezentral[1].Stromspitze_kW.Value;
            Assert.Equal((nbLg + nbPr) * 1000.0 * PREIS_KUEHLUNG + GRUND + LEISTUNG * spitze, ohne.StromkostenKuehltraeger, 6);
            VariantenDaten kopie = SzenarioMengen.Variante(new VariantenDaten { IdProjekt = PROJEKT, Ergebnis = erg }, 1.1);
            KostenEmissionRechner.Berechne(kopie);
            Assert.Equal(nbPr * 1.1, kopie.KuehlzaehlerMWh, 9);

            // Export und Bericht.
            Dictionary<string, double> skalare = KaelteErgebnisexport.Skalare(k).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(1.0, skalare["KaelteDezentral[1].EigenerZaehler"]);
            Assert.Equal(kuehl, skalare["KaelteDezentral[1].Kuehltraeger"]);
            Assert.Equal(0.0, skalare["KaelteDezentral[0].EigenerZaehler"]);
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(erg.Waermepumpe, Emissionsquelle.TraegerName, false,
                CultureInfo.GetCultureInfo("de-DE"), erg.Kaeltemaschinen, null, erg.KaelteDezentral);
            Tabellenzeile zeile = t.Zeilen.Single(r => r.Zellen[0].Text == KUEHLRAUM + " (Split)");
            Assert.Equal(nbPr, zeile.Zellen[4].Zahl.Value, 9);
            Assert.Equal(ProjektbeschreibungBaustein.KuehltraegerText(kuehl, true, Emissionsquelle.TraegerName), zeile.Zellen[5].Text);

            // Der Projektträger als Vorgabe: ohne Kühlträger wirkt der Zähler nicht - Warnkriterium, alles in der Stufe.
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Kaeltebedarf SET Kuehl_ID_Carrier = NULL WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_ProjektWaermebedarf SET Kuehl_ID_Carrier = NULL WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT)));
            Warnbefund w = Assert.Single(Warnkriterien.PruefeProjekt(PROJEKT),
                                         b => b.Kriterium == Warnkriterien.DEZENTRALE_KAELTE_ZAEHLER_OHNE_TRAEGER);
            Assert.Contains(KUEHLRAUM, w.Text);

            SimulationRunner vorgabe = Rechnen(out ErgebnisModel erg2);
            SimulationKaeltebedarf k2 = vorgabe.simulation_Kaeltebedarf;
            Assert.All(k2.DezentraleZeilen, z => Assert.Equal(0, z.Kuehltraeger));
            Assert.Equal(k2.Strom_Dezentral.Sum(), vorgabe.sim.Kaeltestrom_Dezentral_Stufenrechnung_stuendlich.Sum(), 6);
            Assert.True(lauf.sim.ReststromMwh < vorgabe.sim.ReststromMwh, "Mit eigenem Zähler sinkt der Netzbezug des Anschlusses.");
            Assert.DoesNotContain(vorgabe.Protokoll.Hinweise, Mengenmeldung);
            Assert.All(erg2.KaelteDezentral, d => { Assert.Null(d.Kuehl_CarrierId); Assert.Null(d.Kuehl_EigenerZaehler); });
            Assert.Empty(Kaeltestromabrechnung.EigeneZaehler(erg2).Where(z => z.Modulindex <= Kaeltestromabrechnung.SchluesselDezentral(0)));
        }
            [Fact]
        public void Ohne_Kaelteerzeuger_teilt_der_Lauf_den_Netzbezug_der_Split_Zeile_trotzdem_auf()
        {
            if (!_db.Vorhanden) return;
            const int OHNE = 1030;
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(OHNE, true));
            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", KUEHLRAUM)), CultureInfo.InvariantCulture);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(OHNE, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 12, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35,
                },
            }));

            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(OHNE, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.Null(k.Kaskade);
            SimulationKaeltebedarf.DezentraleZeile z = Assert.Single(k.DezentraleZeilen);
            Assert.Equal(0, z.Kuehltraeger);
            Assert.Equal(4000.0, z.StromKwh, 3);                                     // 12 MWh / EER 3
            Assert.True(z.NetzbezugKwh > 0 && z.NetzbezugKwh <= z.StromKwh + 1e-9, "Netzbezug " + z.NetzbezugKwh);
            Assert.Equal(lauf.sim.Kaeltestrom_Dezentral_stuendlich, lauf.sim.Kaeltestrom_Stufenrechnung_stuendlich);
            Assert.DoesNotContain(lauf.Protokoll.Hinweise, Mengenmeldung);           // Projektträger: keine Mengenmeldung

            // Ohne Tabelle 224 (Testdatenbank vor dem Schritt) bleibt das Ergebnis ohne Zeile je Split-Zeile.
            ErgebnisModel erg = new ErgebnisCtrl().Load(OHNE);
            if (KaeltebedarfSplitAbrechnungSchema.TabelleVorhanden()) Assert.Single(erg.KaelteDezentral);
            else Assert.Empty(erg.KaelteDezentral);
        }
    }
}
