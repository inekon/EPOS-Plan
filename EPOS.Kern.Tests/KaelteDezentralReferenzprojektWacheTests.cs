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
    /// <b>Wache des Referenzprojekts 1072 „Referenzprojekt Kältebedarf dezentral"</b> (K6c, Konzept Kältebedarf 4 und 8.1)
    /// — die Kopie des Referenzprojekts 1055 (Kältemaschine mit Trockenkühler und eigenem Zähler, Kaltwasserspeicher, die
    /// Wärmepumpe heizt nur) mit drei Kältebedarfsprofilen. Gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_1072_kaeltebedarf.cs</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht wie im Skript: drei Zuordnungen in <c>Z_Projekt_Kaeltebedarf</c> samt
    /// Projektkopien (Katalogsatz, Summe, Deckungsart, EER-Weg und -Werte, Kühlträger, Zähler), Kühlbetrieb an; die
    /// benutzten Katalogzeilen unverändert.</item>
    /// <item><b>1072 ist eine Kopie von 1055</b>: dieselben Anlagen samt Kältemaschine und Kältespeicher, dasselbe Gebäude;
    /// 1055 selbst führt kein Kältebedarfsprofil.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> deckt das zentrale Profil über die Kältefolge, die zwei Split-Zeilen daneben,
    /// schreibt zwei Zeilen in <c>Tab_ErgebnisKaelteDezentral</c>, führt den Strom von „Split Serverraum" über den eigenen
    /// Zähler mit dem Träger 58 neben der Stufenrechnung, zeigt die Hauptzahl des Deckungsgrads und trifft die
    /// eingefrorenen Jahreswerte der Basis R55.</item>
    /// </list>
    /// 1072 gehört zur Einfrierregel „gesäte Kältebedarfs-Zuordnungen" und — als Kopie von 1055 — zu „gesäte
    /// Kältemaschinendaten" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaelteDezentralReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaelteDezentralReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1072, VORLAGE = 1055;
        private const string NAME = "Referenzprojekt Kältebedarf dezentral";
        private const int TYP_KAELTEMASCHINE = 13, KUEHLTRAEGER = 58;
        private const string BUERO = "Büro Kälteprofil", SPLIT_BUERO = "Split Büro", SPLIT_SERVER = "Split Serverraum";

        // Die Jahreswerte der Basis R56 (unverändert in R57, Referenzlaeufe/2026-10-16_R57_Schnellaufheizung/Projekt_1072), je Split-Zeile
        // aus dem Lauf: Kälte, Strom und Netzbezug [MWh/a].
        private const double R55_DEZENTRAL_MWH = 5.0;
        private const double R55_STROM_DEZENTRAL_MWH = 1.52919445;
        private const double R55_BUERO_STROM_MWH = 1.0, R55_BUERO_NETZ_MWH = 0.884362971;
        private const double R55_SERVER_STROM_MWH = 0.529194446, R55_SERVER_NETZ_MWH = 0.529194446;
        private const double R55_KAELTEFOLGE_MWH = 5.05051694;

        private static object Skalar(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static long Zahl(string sql, params object[] w) => Convert.ToInt64(Skalar(sql, w), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, params object[] w)
        {
            DataTable t = DataRepository.GetDataTable(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static double D(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);
        private static long L(object o) => Convert.ToInt64(o, CultureInfo.InvariantCulture);
        private static string S(object o) => o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>Relative Toleranz 1e-4 wie der Vergleich der Basis, nie über die Stellenzahl.</summary>
        private static void Nahe(double soll, double ist, string was)
            => Assert.True(Math.Abs(ist - soll) <= 1e-4 * Math.Abs(soll) + 1e-9,
                           was + ": " + ist.ToString("R", CultureInfo.InvariantCulture) + " statt " + soll.ToString("R", CultureInfo.InvariantCulture));

        // =====================================================================
        //  Jede gesäte Zelle
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Skript()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, S(Skalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", PROJEKT)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT));

            DataTable t = DataRepository.GetDataTable(
                "SELECT z.ID_Kaeltebedarf, z.Bezeichner AS ZBez, k.Bezeichner AS KBez, k.ID_Projekt AS KProj, k.ReadOnly AS KRo, " +
                "s.Bezeichner AS Katalog, z.Summe, z.ID_Betriebskalender, z.Deckung, z.Split_EER_Weg, z.Split_EER_1, z.Split_Taussen_1, " +
                "z.Split_EER_2, z.Split_Taussen_2, z.Kuehl_ID_Carrier, z.Kuehl_EigenerZaehler, z.Kaelte_Art FROM Z_Projekt_Kaeltebedarf z " +
                "INNER JOIN Tab_Kaeltebedarf k ON z.ID_Kaeltebedarf = k.ID LEFT JOIN Tab_Kaeltebedarf_STAMM s ON k.ID_Stamm = s.ID " +
                "WHERE z.ID_Projekt = ? ORDER BY z.ID", new DbParam("@p0", PROJEKT));
            Assert.Equal(3, t.Rows.Count);
            var soll = new (string Name, string Katalog, double Summe, string Deckung, string Weg, double? E1, double? T1, double? E2, double? T2, long? Traeger, long Zaehler)[]
            {
                (BUERO,        "Raumkühlung Büro", 1.0, KaeltebedarfSchema.DECKUNG_ZENTRAL, null, null, null, null, null, null, 0),
                (SPLIT_BUERO,  "Raumkühlung Büro", 3.0, KaeltebedarfSchema.DECKUNG_SPLIT, KaeltebedarfSchema.EER_FEST, 3.0, 35.0, null, null, null, 0),
                (SPLIT_SERVER, "Serverraum",       2.0, KaeltebedarfSchema.DECKUNG_SPLIT, KaeltebedarfSchema.EER_LINEAR, 4.0, 10.0, 2.5, 35.0, KUEHLTRAEGER, 1),
            };
            double? N(object o) => o == DBNull.Value ? (double?)null : D(o);
            for (int i = 0; i < soll.Length; i++)
            {
                DataRow r = t.Rows[i];
                var s = soll[i];
                Assert.Equal(s.Name, S(r["ZBez"]));
                Assert.Equal(s.Name, S(r["KBez"]));                 // die Projektkopie trägt den Namen der Zeile
                Assert.Equal((long)PROJEKT, L(r["KProj"]));
                Assert.Equal(0L, L(r["KRo"]));
                Assert.Equal(s.Katalog, S(r["Katalog"]));           // ID_Stamm zeigt auf den Katalogsatz
                Assert.Equal(s.Summe, D(r["Summe"]));
                Assert.True(r["ID_Betriebskalender"] == DBNull.Value, s.Name + ": Betriebskalender nicht leer");
                Assert.Equal(s.Deckung, S(r["Deckung"]));
                Assert.Equal(s.Weg, S(r["Split_EER_Weg"]));
                Assert.Equal(s.E1, N(r["Split_EER_1"]));
                Assert.Equal(s.T1, N(r["Split_Taussen_1"]));
                Assert.Equal(s.E2, N(r["Split_EER_2"]));
                Assert.Equal(s.T2, N(r["Split_Taussen_2"]));
                Assert.Equal(s.Traeger, r["Kuehl_ID_Carrier"] == DBNull.Value ? (long?)null : L(r["Kuehl_ID_Carrier"]));
                Assert.Equal(s.Zaehler, L(r["Kuehl_EigenerZaehler"]));
                Assert.Equal(KaelteArtSchema.RAUM, S(r["Kaelte_Art"]));
                // Je Projektkopie genau ein Typprofil des Projekts.
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyp WHERE ID_Kaeltebedarf = ? AND ID_Projekt = ?", L(r["ID_Kaeltebedarf"]), PROJEKT));
                Assert.Null(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT)[i]));
            }

            // Die benutzten Katalogzeilen sind Auslieferung und tragen ihren Schlüssel.
            Assert.Equal("KB:RAUMKUEHLUNG_BUERO", S(Skalar("SELECT Katalog_Schluessel FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ? AND ReadOnly = 1", "Raumkühlung Büro")));
            Assert.Equal("KB:SERVERRAUM", S(Skalar("SELECT Katalog_Schluessel FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ? AND ReadOnly = 1", "Serverraum")));

            // Der Kühlträger 58 ist ein Stromträger des Projekts und weicht vom Projektträger ab - der eigene Zähler wirkt.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ? AND \"ID_Energieträger\" = ?", PROJEKT, KUEHLTRAEGER));
            Assert.True(Kaeltestromabrechnung.Abweichend(KUEHLTRAEGER, Kaeltestromabrechnung.Projekttraeger(PROJEKT)));

            // Die Kältemaschine wie in 1055: Träger 58, eigener Zähler.
            DataRow a = Zeile("SELECT Kuehl_ID_Carrier, Kuehl_EigenerZaehler FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", PROJEKT, TYP_KAELTEMASCHINE);
            Assert.Equal((long)KUEHLTRAEGER, L(a["Kuehl_ID_Carrier"]));
            Assert.Equal(1L, L(a["Kuehl_EigenerZaehler"]));
        }

        [Fact]
        public void Das_Projekt_ist_eine_Kopie_von_1055_und_die_Vorlage_bleibt_unberuehrt()
        {
            if (!_db.Vorhanden) return;
            string anlagen = "SELECT Bezeichner || '|' || ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY Bezeichner, ID_Type";
            List<string> Liste(int projekt) => DataRepository.GetDataTable(anlagen, new DbParam("@p0", projekt)).Rows.Cast<DataRow>()
                .Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToList();
            Assert.Equal(Liste(VORLAGE), Liste(PROJEKT));
            Assert.Equal(6, Liste(PROJEKT).Count);   // vier Anlagen aus 1017, Kältemaschine und Kältespeicher

            string geb = "SELECT Gebaeudename || '|' || Kuehlung_Aktiv || '|' || Kuehl_Sollwert || '|' || Kuehlleistung_Max FROM Tab_Gebaeude WHERE ID_Projekt = ?";
            Assert.Equal(S(Skalar(geb, VORLAGE)), S(Skalar(geb, PROJEKT)));
            string km = "SELECT Bezeichner || '|' || Nennkaelteleistung_kW || '|' || Nenn_EER || '|' || Rueckkuehlart FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?";
            Assert.Equal(S(Skalar(km, VORLAGE)), S(Skalar(km, PROJEKT)));

            // Die Vorlage führt kein Kältebedarfsprofil; 1072 ist das einzige Projekt der Testdatenbank mit einer Zuordnung.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_Projekt_Kaeltebedarf WHERE ID_Projekt = ?", VORLAGE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf WHERE ID_Projekt = ?", VORLAGE));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Z_Projekt_Kaeltebedarf"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_Projekt_Kaeltebedarf WHERE ID_Projekt <> ?", PROJEKT));
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        [Fact]
        public void Ein_Lauf_deckt_zentral_ueber_die_Kaeltefolge_und_die_Split_Zeilen_daneben()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));

            // Die Kältefolge deckt Gebäude und zentrales Profil voll.
            Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf.Kaskade;
            Assert.NotNull(kaskade);
            Assert.True(kaskade.RestGesamtKwh <= 1e-3 * kaskade.BedarfGesamtKwh,
                        "Unterdeckung " + kaskade.RestGesamtKwh.ToString("F1", CultureInfo.InvariantCulture) + " kWh");

            // Die Split-Zeilen in Buchungsfolge: „Split Büro" anteilig, „Split Serverraum" über den eigenen Zähler.
            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.Equal(2, k.DezentraleZeilen.Count);
            SimulationKaeltebedarf.DezentraleZeile buero = k.DezentraleZeilen[0], server = k.DezentraleZeilen[1];
            Assert.Equal(SPLIT_BUERO, buero.Bezeichner);
            Assert.True(buero.Profil);
            Assert.Equal(0, buero.Kuehltraeger);
            Assert.False(buero.NebenDerStufenrechnung);
            Assert.Equal(SPLIT_SERVER, server.Bezeichner);
            Assert.True(server.Profil);
            Assert.Equal(KUEHLTRAEGER, server.Kuehltraeger);
            Assert.True(server.NebenDerStufenrechnung);
            Assert.Equal(3.0, buero.JahresEer, 9);                         // EER fest
            Assert.InRange(server.JahresEer, 2.5, 4.0);                     // EER linear, geklemmt

            // Die Ergebnisseite: zwei Zeilen in Tab_ErgebnisKaelteDezentral, der Strom von (c) mit eigenem Zähler.
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisKaelteDezentral WHERE ID_Ergebnis = ?", kopf));
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Equal(2, erg.KaelteDezentral.Count);
            Assert.Contains(Kaeltestromabrechnung.EigeneZaehler(erg),
                            z => z.Traeger == KUEHLTRAEGER && z.Modulindex <= Kaeltestromabrechnung.SchluesselDezentral(0) &&
                                 z.Anlage.Contains(SPLIT_SERVER));

            // Die Kälteseite des Ergebnisses: Hauptzahl mit der Split-Deckung, „davon Kältefolge".
            SimulationErgebnisCtrl.KaelteErgebnis e = SimulationErgebnisCtrl.Kaelte(lauf.simulation_Waermebedarf);
            Assert.True(e.MitDezentral);
            Assert.NotNull(e.DeckungsgradGesamtProzent);
            Assert.NotNull(e.DeckungsgradKaeltefolgeProzent);
            Assert.InRange(e.DeckungsgradGesamtProzent.Value, 99.9, 100.0 + 1e-9);
            Assert.Equal(6.0, e.ProfilMwh, 6);                              // drei Profile, zentral und dezentral

            // K6c-N: Die Laufmeldung nennt mit Split-Zeile die Hauptzahl wie die Übersicht, „davon Kältefolge“ und die
            // dezentrale Menge — nicht mehr die Kältefolge allein am Gesamtbedarf (gespeichert bleibt diese).
            string meldung = Assert.Single(lauf.Protokoll.Hinweise, h => h.StartsWith("Kälteerzeugung:", StringComparison.Ordinal));
            Assert.StartsWith("Kälteerzeugung: 10,05 MWh/a von 10,05 MWh/a Kältebedarf gedeckt (100,0 %; davon Kältefolge 100,0 %, " +
                              "dezentral 5,00 MWh/a), Kältestrom der Kältefolge ", meldung, StringComparison.Ordinal);
            Assert.EndsWith(" Kühltage.", meldung, StringComparison.Ordinal);
            Assert.Equal(SimulationControl.KaeltedeckungMeldung(kaskade.DeckungGesamtKwh / 1000.0, k.Kaeltebedarf_Dezentral_Gesamt,
                             k.Kaeltebedarf_Gesamt, kaskade.StromGesamtKwh / 1000.0, kaskade.EerJahreswert, kaskade.AnzahlKuehltage),
                         meldung);
            Assert.Contains(KaelteErgebnisexport.Skalare(k), s => s.Key == "Kaelte.DeckungsgradProzent" &&
                            Math.Abs(s.Value - SimulationRunner.DeckungProzent(kaskade.DeckungGesamtKwh / 1000.0, k.Kaeltebedarf_Gesamt)) < 1e-12);

            // aggregate.csv führt mit Split-Zeile die Hauptzahl und „davon Kältefolge“ der Laufmeldung — dieselbe Stelle.
            var skalare = KaelteErgebnisexport.Skalare(k);
            double? hauptzahl = KaelteDeckungsgrad.Hauptzahl(kaskade.DeckungGesamtKwh / 1000.0, k.Kaeltebedarf_Dezentral_Gesamt,
                                                             k.Kaeltebedarf_Gesamt);
            double? davon = KaelteDeckungsgrad.DavonKaeltefolge(kaskade.DeckungGesamtKwh / 1000.0, k.Kaeltebedarf_Dezentral_Gesamt,
                                                                k.Kaeltebedarf_Gesamt);
            Assert.NotNull(hauptzahl);
            Assert.NotNull(davon);
            Assert.Equal(hauptzahl.Value, Assert.Single(skalare, s => s.Key == "Kaelte.DeckungsgradGesamtProzent").Value);
            Assert.Equal(davon.Value, Assert.Single(skalare, s => s.Key == "Kaelte.DeckungsgradKaeltefolgeProzent").Value);
            Assert.Contains("(" + hauptzahl.Value.ToString("N1", CultureInfo.CurrentCulture) + " %; davon Kältefolge " +
                            davon.Value.ToString("N1", CultureInfo.CurrentCulture) + " %", meldung, StringComparison.Ordinal);
            Assert.Equal(e.DeckungsgradGesamtProzent.Value, hauptzahl.Value, 9);

            // Kein Befund der dezentralen Kälte; die Warnkriterien wie in der Vorlage.
            List<Warnbefund> befunde = Warnkriterien.PruefeProjekt(PROJEKT);
            Assert.DoesNotContain(befunde, b => b.Kriterium == Warnkriterien.DEZENTRALE_KAELTE_OHNE_EER ||
                                                b.Kriterium == Warnkriterien.DEZENTRALE_KAELTE_ZAEHLER_OHNE_TRAEGER);
            Assert.Equal(Warnkriterien.PruefeProjekt(VORLAGE).Select(b => b.Kriterium).OrderBy(x => x),
                         befunde.Select(b => b.Kriterium).OrderBy(x => x));

            _aus.WriteLine("1072: Kältefolge {0:R} MWh, dezentral {1:R} MWh, Strom dezentral {2:R} MWh, Büro Strom {3:R} / Netz {4:R}, " +
                           "Server Strom {5:R} / Netz {6:R}, Deckungsgrad {7:R} %, davon Kältefolge {8:R} %, Befunde {9}",
                           kaskade.BedarfGesamtKwh / 1000.0, k.Kaeltebedarf_Dezentral_Gesamt, k.Strom_Dezentral_Gesamt,
                           buero.StromKwh / 1000.0, buero.NetzbezugKwh / 1000.0, server.StromKwh / 1000.0, server.NetzbezugKwh / 1000.0,
                           e.DeckungsgradGesamtProzent, e.DeckungsgradKaeltefolgeProzent, string.Join(",", befunde.Select(b => b.Kriterium)));

            // Die eingefrorenen Jahreswerte der Basis R55.
            Nahe(R55_KAELTEFOLGE_MWH, kaskade.BedarfGesamtKwh / 1000.0, "Kältebedarf der Kältefolge");
            Nahe(R55_DEZENTRAL_MWH, k.Kaeltebedarf_Dezentral_Gesamt, "Kälte dezentral");
            Nahe(R55_STROM_DEZENTRAL_MWH, k.Strom_Dezentral_Gesamt, "Strom dezentral");
            Nahe(3.0, buero.KaelteKwh / 1000.0, "Kälte Split Büro");
            Nahe(2.0, server.KaelteKwh / 1000.0, "Kälte Split Serverraum");
            Nahe(R55_BUERO_STROM_MWH, buero.StromKwh / 1000.0, "Strom Split Büro");
            Nahe(R55_BUERO_NETZ_MWH, buero.NetzbezugKwh / 1000.0, "Netzbezug Split Büro");
            Nahe(R55_SERVER_STROM_MWH, server.StromKwh / 1000.0, "Strom Split Serverraum");
            Nahe(R55_SERVER_NETZ_MWH, server.NetzbezugKwh / 1000.0, "Netzbezug Split Serverraum");
        }

        // =====================================================================
        //  Die Laufmeldung der Kälteerzeugung (K6c-N)
        // =====================================================================

        [Fact]
        public void Die_Laufmeldung_nennt_mit_Split_die_Hauptzahl_und_ohne_Split_den_Bestand()
        {
            // Mit Split-Zeile: Hauptzahl (60 + 20) / 100, „davon Kältefolge“ 60 / (100 − 20), die dezentrale Menge.
            Assert.Equal("Kälteerzeugung: 80,00 MWh/a von 100,00 MWh/a Kältebedarf gedeckt (80,0 %; davon Kältefolge 75,0 %, " +
                         "dezentral 20,00 MWh/a), Kältestrom der Kältefolge 20,00 MWh/a, EER-Jahreswert 3,00; 120 Kühltage.",
                         SimulationControl.KaeltedeckungMeldung(60.0, 20.0, 100.0, 20.0, 3.0, 120));

            // Ohne Split-Zeile wörtlich wie immer: die Kältefolge am Gesamtbedarf.
            Assert.Equal("Kälteerzeugung: 60,00 MWh/a von 100,00 MWh/a Kältebedarf gedeckt (60,0 %), Kältestrom 20,00 MWh/a, " +
                         "EER-Jahreswert 3,00; 120 Kühltage.",
                         SimulationControl.KaeltedeckungMeldung(60.0, 0.0, 100.0, 20.0, 3.0, 120));

            // Bleibt kein zentral zu deckender Bedarf, entfällt „davon Kältefolge“.
            Assert.Equal("Kälteerzeugung: 100,00 MWh/a von 100,00 MWh/a Kältebedarf gedeckt (100,0 %; ganz dezentral, 100,00 MWh/a), " +
                         "Kältestrom der Kältefolge 0,00 MWh/a, EER-Jahreswert 0,00; 0 Kühltage.",
                         SimulationControl.KaeltedeckungMeldung(0.0, 100.0, 100.0, 0.0, 0.0, 0));
        }

        [Fact]
        public void Die_Vorlage_1055_ohne_Split_meldet_die_Kaelteerzeugung_wie_bisher()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(VORLAGE, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf.Kaskade;
            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.False(k.MitDezentral);
            string meldung = Assert.Single(lauf.Protokoll.Hinweise, h => h.StartsWith("Kälteerzeugung:", StringComparison.Ordinal));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_DECKUNG,
                             (kaskade.DeckungGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                             k.Kaeltebedarf_Gesamt.ToString("N2", CultureInfo.CurrentCulture),
                             SimulationRunner.DeckungProzent(kaskade.DeckungGesamtKwh / 1000.0, k.Kaeltebedarf_Gesamt)
                                             .ToString("N1", CultureInfo.CurrentCulture),
                             (kaskade.StromGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                             kaskade.EerJahreswert.ToString("N2", CultureInfo.CurrentCulture),
                             kaskade.AnzahlKuehltage),
                         meldung);
            Assert.DoesNotContain("davon Kältefolge", meldung, StringComparison.Ordinal);
            Assert.DoesNotContain(KaelteErgebnisexport.Skalare(k), s => s.Key == "Kaelte.DeckungsgradGesamtProzent" ||
                                                                       s.Key == "Kaelte.DeckungsgradKaeltefolgeProzent");
        }
    }
}
