using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KU3-1 — die Kältemaschine: Schema, Saat, Katalog und Kopierwege</b>
    /// (<see cref="KaeltemaschineSchema"/>, <see cref="KaeltemaschineStammCtrl"/>, <see cref="KaeltemaschineCtrl"/>).
    /// Der Rechenweg liest die Tabellen nicht; der Referenzlauf ist nicht betroffen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";

        // =============================================================================
        //  Teil 1 - Nummer, Register, Prüfklauseln (ohne Testdatenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(ZonenUebergabeSchema.SCHRITT + 1, KaeltemaschineSchema.SCHRITT);
            Assert.Equal(182, KaeltemaschineSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaeltemaschineSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaeltemaschineSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Equal("Kältemaschine", DbWerte.ERZEUGER_KAELTEMASCHINE);
            Assert.Equal("Kältemaschine", DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE);
        }

        [Fact]
        public void Der_Katalog_steht_im_Register_mit_Kennlinie_als_Kind()
        {
            Katalogtabelle t = Katalogfassung.Tabelle(KaeltemaschineSchema.TAB_STAMM);
            Assert.NotNull(t);
            Assert.Equal(3, t.Stufe);
            Assert.Equal("KM", t.Kuerzel);
            Assert.Contains(t, Katalogfassung.Stufe3);
            Assert.DoesNotContain(t, Katalogfassung.Stufe1);
            Assert.DoesNotContain(t, Katalogfassung.Stufe2);
            Katalogkind k = Assert.Single(t.Kinder);
            Assert.Equal(KaeltemaschineSchema.TAB_KENNDATEN_STAMM, k.Tabelle);
            Assert.Equal(KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE, k.Fremdschluessel);
            Assert.Equal(KaeltemaschineSchema.KennlinienSpalten, k.Fachspalten);
            Assert.Contains(KatalogRegistry.Alle, d => d.Tabelle == KaeltemaschineSchema.TAB_STAMM && d.Schluessel == "KAELTEMASCHINE");
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Rueckkuehlart_Teillast_und_Kennlinie()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE \"Tab_Projekt\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Projekt (ID) VALUES (1)");
            foreach (var t in KaeltemaschineSchema.Tabellen()) Ausfuehren(c, t.Value);
            Ausfuehren(c, "INSERT INTO Tab_Kaeltemaschine_STAMM (ID, Bezeichner) VALUES (1, 'A')");
            foreach (string gut in KaeltemaschineSchema.RUECKKUEHLARTEN.Select(a => "'" + a + "'").Append("NULL"))
                Assert.False(Wirft(c, "UPDATE Tab_Kaeltemaschine_STAMM SET Rueckkuehlart = " + gut), gut);
            foreach (string schlecht in new[] { "'Luft'", "'KUEHLTURM'", "''" })
                Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine_STAMM SET Rueckkuehlart = " + schlecht), schlecht);
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine_STAMM SET Mindestteillast_Prozent = 101"));
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine_STAMM SET Nenn_EER = 0"));
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine_STAMM SET ReadOnly = 2"));
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine_STAMM SET Nennkaelteleistung_kW = 'viel'"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Kaeltemaschine_STAMM (Bezeichner) VALUES ('')"));
            Ausfuehren(c, "INSERT INTO Tab_Kenndaten_Kaeltemaschine_STAMM (ID_Kaeltemaschine, Rueckkuehltemperatur, Kaltwassertemperatur, EER) VALUES (1, 35, 6, 3)");
            Assert.True(Wirft(c, "INSERT INTO Tab_Kenndaten_Kaeltemaschine_STAMM (ID_Kaeltemaschine, Rueckkuehltemperatur, Kaltwassertemperatur, EER) VALUES (1, 35, 6, 4)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Kenndaten_Kaeltemaschine_STAMM (ID_Kaeltemaschine, Rueckkuehltemperatur, Kaltwassertemperatur, EER) VALUES (1, 25, 6, 0)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Kenndaten_Kaeltemaschine_STAMM (ID_Kaeltemaschine, Rueckkuehltemperatur, Kaltwassertemperatur) VALUES (99, 25, 6)"));
            // Projektkopie: der Katalogverweis fällt beim Löschen des Katalogsatzes auf NULL, die Kennlinie hängt mit Kaskade.
            Ausfuehren(c, "INSERT INTO Tab_Kaeltemaschine (ID, ID_Projekt, ID_Stamm, Bezeichner) VALUES (1, 1, 1, 'A')");
            Ausfuehren(c, "INSERT INTO Tab_Kenndaten_Kaeltemaschine (ID_Projekt, ID_Kaeltemaschine, Rueckkuehltemperatur, Kaltwassertemperatur) VALUES (1, 1, 35, 6)");
            Ausfuehren(c, "DELETE FROM Tab_Kaeltemaschine_STAMM WHERE ID = 1");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine_STAMM"));
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Stamm IS NULL"));
            Ausfuehren(c, "DELETE FROM Tab_Projekt WHERE ID = 1");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine"));
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_Kaeltemaschine"));
        }

        [Fact]
        public void Die_Saat_hat_drei_Geraete_mit_je_sechs_plausiblen_Punkten()
        {
            Assert.Equal(3, KaeltemaschineSchema.SAAT.Count);
            Assert.Equal(new[] { "LUFT", "TROCKENKUEHLER", "NASSKUEHLER" }, KaeltemaschineSchema.SAAT.Select(b => b.Rueckkuehlart));
            foreach (var b in KaeltemaschineSchema.SAAT)
            {
                Assert.Equal(6, b.Kennlinie.Count);
                Assert.Equal(6, b.Kennlinie.Select(k => (k.Rueckkuehl, k.Kaltwasser)).Distinct().Count());
                Assert.All(b.Kennlinie, k => Assert.InRange(k.Eer, 2.5, 6.0));
                // Wärmere Rückkühlung senkt den EER, wärmeres Kaltwasser hebt ihn.
                foreach (double kw in new[] { 6.0, 12.0 })
                {
                    double[] eer = b.Kennlinie.Where(k => k.Kaltwasser == kw).OrderBy(k => k.Rueckkuehl).Select(k => k.Eer).ToArray();
                    Assert.True(eer[0] > eer[1] && eer[1] > eer[2], b.Bezeichner);
                }
                Assert.Equal(b.NennEer, b.Kennlinie.Single(k => k.Rueckkuehl == 35 && k.Kaltwasser == 6).Eer);
                Assert.Equal(b.Nennkaelteleistung, b.Kennlinie.Single(k => k.Rueckkuehl == 35 && k.Kaltwasser == 6).Leistung);
            }
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank, der Schritt und seine Wiederholung
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_mit_Saat_und_Katalogschluesseln()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltemaschineSchema.Vollstaendig());
            Assert.Equal(17, DataRepository.SpaltenVonTabelle(KaeltemaschineSchema.TAB_STAMM).Count);
            Assert.Equal(17, DataRepository.SpaltenVonTabelle(KaeltemaschineSchema.TAB_PROJEKT).Count); // + Kuehl_Vorlauf, Kuehl_Hilfsstromanteil (183)
            Assert.Equal(7, DataRepository.SpaltenVonTabelle(KaeltemaschineSchema.TAB_KENNDATEN_STAMM).Count);
            Assert.Equal(7, DataRepository.SpaltenVonTabelle(KaeltemaschineSchema.TAB_KENNDATEN).Count);
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE ReadOnly = 1"));
            Assert.Equal(18L, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine_STAMM WHERE ReadOnly = 1"));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM WHERE Katalog_Schluessel LIKE 'KM:%' AND length(Katalog_Pruefsumme) = 64"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine"));
            foreach (string t in new[] { KaeltemaschineSchema.TAB_STAMM, KaeltemaschineSchema.TAB_PROJEKT,
                                         KaeltemaschineSchema.TAB_KENNDATEN_STAMM, KaeltemaschineSchema.TAB_KENNDATEN })
                Assert.Contains("STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE name = ?", new DbParam("?", t)), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach (string t in new[] { KaeltemaschineSchema.TAB_KENNDATEN, KaeltemaschineSchema.TAB_KENNDATEN_STAMM,
                                         KaeltemaschineSchema.TAB_PROJEKT, KaeltemaschineSchema.TAB_STAMM })
                DataRepository.ExecuteNonQuery("DROP TABLE \"" + t + "\"");
            Assert.False(KaeltemaschineSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.True(KaeltemaschineSchema.Ausfuehren(bericht) >= 4);
            Assert.Contains(bericht, z => z.Contains("3 Beispielgeraet", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("18 Kennlinienpunkt", StringComparison.Ordinal));
            Assert.True(KaeltemaschineSchema.Vollstaendig());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            var zweiter = new List<string>();
            Assert.Equal(0, KaeltemaschineSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("0 Beispielgeraet", StringComparison.Ordinal));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine_STAMM"));
            Assert.Equal(18L, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine_STAMM"));
        }

        // =============================================================================
        //  Teil 3 - Katalogpflege, Katalog -> Projekt, Abgleich, Duplizieren, Paket
        // =============================================================================

        [Fact]
        public void Der_Katalog_schreibt_prueft_sperrt_und_dupliziert_samt_Kennlinie()
        {
            if (!_db.Vorhanden) return;
            var liste = KaeltemaschineStammCtrl.Liste();
            Assert.Equal(3, liste.Count);
            Assert.All(liste, z => Assert.True(z.ReadOnly));
            int saat = liste.Single(z => z.Rueckkuehlart == "TROCKENKUEHLER").Id;

            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(saat);
            Assert.Equal(6, m.Kennlinie.Count);
            m.Nenn_EER = 9;
            Assert.False(KaeltemaschineStammCtrl.Speichern(m).Ok);                   // ausgeliefert: nie überschrieben

            Katalogkopie.Ergebnis kopie = KaeltemaschineStammCtrl.Duplizieren(saat, "Kältemaschine Kopie");
            Assert.True(kopie.Ok, kopie.Meldung);
            KaeltemaschineModel k = KaeltemaschineStammCtrl.Laden(kopie.Id);
            Assert.False(k.ReadOnly);
            Assert.Equal(6, k.Kennlinie.Count);
            Assert.Equal("TROCKENKUEHLER", k.Rueckkuehlart);

            k.Rueckkuehlart = "Trockenkühler";                                        // Anzeigetext ist kein Persistenzwert
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KM_MSG_RUECKKUEHLART_UNGUELTIG, KaeltemaschineStammCtrl.Speichern(k).Meldung);
            k.Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER;
            k.Kennlinie.Add(new KaeltemaschineKenndatenModel { Rueckkuehltemperatur = 35, Kaltwassertemperatur = 6, EER = 4 });
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KM_MSG_KENNLINIE_DOPPELT, KaeltemaschineStammCtrl.Speichern(k).Meldung);
            k.Kennlinie.RemoveAt(k.Kennlinie.Count - 1);
            k.Kennlinie.RemoveAt(0);
            Assert.True(KaeltemaschineStammCtrl.Speichern(k).Ok);
            Assert.Equal(5, KaeltemaschineStammCtrl.Laden(kopie.Id).Kennlinie.Count);
            Assert.Equal("NASSKUEHLER", KaeltemaschineStammCtrl.Laden(kopie.Id).Rueckkuehlart);

            var neu = new KaeltemaschineModel { Bezeichner = "Kältemaschine Kopie" };
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, KaeltemaschineStammCtrl.Speichern(neu).Meldung);

            Assert.False(KaeltemaschineStammCtrl.Loeschen(saat).Ok);
            Assert.True(KaeltemaschineStammCtrl.Loeschen(kopie.Id).Ok);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine_STAMM WHERE ID_Kaeltemaschine = " + kopie.Id));

            Assert.True(KaeltemaschineStammCtrl.SchlossSetzen(new[] { saat }, false).Ok);
            Assert.False(KaeltemaschineStammCtrl.Gesperrt(saat));
        }

        [Fact]
        public void Katalog_Projektkopie_Abgleich_Duplizieren_und_Paket_tragen_die_Kaeltemaschine()
        {
            if (!_db.Vorhanden) return;
            int stamm = KaeltemaschineStammCtrl.Liste().Single(z => z.Rueckkuehlart == "NASSKUEHLER").Id;

            // Katalog -> Projekt: Spalte für Spalte samt Kennlinie und Zuordnung über ID_Stamm.
            int kopie = KaeltemaschineCtrl.AusKatalogUebernehmen(stamm, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal(kopie, KaeltemaschineCtrl.AusKatalogUebernehmen(stamm, PROJEKT));
            Assert.Equal(-1, KaeltemaschineCtrl.AusKatalogUebernehmen(999999, PROJEKT));
            KaeltemaschineModel p = KaeltemaschineCtrl.Laden(kopie);
            KaeltemaschineModel s = KaeltemaschineStammCtrl.Laden(stamm);
            Assert.Equal(stamm, p.IdStamm);
            Assert.Equal(PROJEKT, p.IdProjekt);
            Assert.Equal(s.Bezeichner, p.Bezeichner);
            Assert.Equal(s.Nennkaelteleistung_kW, p.Nennkaelteleistung_kW);
            Assert.Equal(s.Hilfsstrom_Rueckkuehlung_kW, p.Hilfsstrom_Rueckkuehlung_kW);
            Assert.Equal(s.Kennlinie.Select(k => (k.Rueckkuehltemperatur, k.Kaltwassertemperatur, k.EER, k.Kaelteleistung_kW)),
                         p.Kennlinie.Select(k => (k.Rueckkuehltemperatur, k.Kaltwassertemperatur, k.EER, k.Kaelteleistung_kW)));
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Projekt = " + PROJEKT));

            // Katalogabgleich: ein gelöschter Auslieferungssatz kommt aus dem Paket der Fassung zurück, samt Kennlinie.
            Katalogpaket paket = Katalogpaket.AusDatenbank(Katalogabgleich.FassungDerDatenbank() ?? 1);
            int anderer = KaeltemaschineStammCtrl.Liste().Single(z => z.Rueckkuehlart == "LUFT").Id;
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltemaschine_STAMM WHERE ID = ?", new DbParam("?", anderer));
            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, false, true);
            Assert.True(e.Ausgefuehrt);
            KaeltemaschineStammCtrl.Listenzeile zurueck = KaeltemaschineStammCtrl.Liste().Single(z => z.Rueckkuehlart == "LUFT");
            Assert.Equal(6, KaeltemaschineStammCtrl.Laden(zurueck.Id).Kennlinie.Count);
            Assert.True(zurueck.ReadOnly);

            // Projekt duplizieren: die Kopie führt die Kältemaschine samt Kennlinie und Katalogverweis.
            int dup = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " KM");
            Assert.True(dup > 0);
            int km = Assert.Single(KaeltemaschineCtrl.IdsImProjekt(dup));
            Assert.NotEqual(kopie, km);
            KaeltemaschineModel d = KaeltemaschineCtrl.Laden(km);
            Assert.Equal(stamm, d.IdStamm);
            Assert.Equal(6, d.Kennlinie.Count);
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Projekt = " + dup));

            // Projektpaket: Export und Import; der Katalogverweis wird am Ziel über den Bezeichner nachgetragen.
            string datei = Path.Combine(Path.GetTempPath(), "km_" + Guid.NewGuid().ToString("N") + ".wpx");
            try
            {
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, datei));
                int neu = io.Importieren(datei, "Transfer KM", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                int imp = Assert.Single(KaeltemaschineCtrl.IdsImProjekt(neu));
                KaeltemaschineModel i = KaeltemaschineCtrl.Laden(imp);
                Assert.Equal(stamm, i.IdStamm);
                Assert.Equal(6, i.Kennlinie.Count);
                Assert.Equal("NASSKUEHLER", i.Rueckkuehlart);
            }
            finally
            {
                try { File.Delete(datei); } catch (IOException) { }
            }
        }

        // =============================================================================

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
