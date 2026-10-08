using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt 186 — der Anlagenfahrplan</b> (AK2-1, <see cref="AnlagenfahrplanSchema"/>): Nummer, Ziel und
    /// Register; die acht Spalten samt Prüfklauseln und Spaltenzahl; der Stand davor und die Wiederholbarkeit;
    /// die Anlagen-Anweisung je Stand der Datenbank; die Kopierwege des Erzeugers (Speichern, Projektduplikat,
    /// Projekttransfer, Komponentenübernahme — NULL-erhaltend) und die Rundreise der sechs Ergebnisspalten.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenfahrplanSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int WP_ANLAGE = 10353;

        /// <summary>Ein gültiges Zeitprogramm: werktags 06–22 Uhr frei, sonst halb, sonntags gesperrt.</summary>
        internal static string Programm()
        {
            var w = new double[AnlagenkopplungSchema.WOCHENWERTE];
            for (int i = 0; i < w.Length; i++)
            {
                int tag = i / 24, h = i % 24;
                w[i] = tag == 6 ? 0.0 : (h >= 6 && h < 22 ? 1.0 : 0.5);
            }
            return AnlagenkopplungSchema.WochenprofilSchreiben(w);
        }

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_186_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(ZonenKaeltespitzeSchema.SCHRITT + 1, AnlagenfahrplanSchema.SCHRITT);
            Assert.Equal(186, AnlagenfahrplanSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= AnlagenfahrplanSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == AnlagenfahrplanSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Tab_Energieanlagen.Zeitprogramm", "Tab_Energieanlagen.Vorlauf_Max",
                    "Tab_ErgebnisEnergiebedarf.Komfort_Unterschreitungsstunden",
                    "Tab_ErgebnisEnergiebedarf.Komfort_Kelvinstunden",
                    "Tab_ErgebnisEnergiebedarf.Komfort_Laengste_Strecke",
                    "Tab_ErgebnisEnergiebedarf.Fahrplan_Begrenzt_Stunden",
                    "Tab_ErgebnisEnergiebedarf.Komfort_Ueberschreitungsstunden",
                    "Tab_ErgebnisEnergiebedarf.Komfort_Kelvinstunden_Kuehlung",
                },
                AnlagenfahrplanSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
        }

        /// <summary>
        /// Die Anlagen-Anweisung nennt die zwei Spalten nach der Albedo (danach nur noch die drei der freien Kühlung,
        /// Schritt 187); die Variante ohne sie hat zwei Platzhalter weniger als die Variante des Stands 186.
        /// </summary>
        [Fact]
        public void Die_Anlagenanweisung_fuehrt_die_zwei_Spalten_zuletzt()
        {
            string mit = AnlagenSql.SQL_ANLAGE_INSERT_OHNE_FREIE_KUEHLUNG, ohne = AnlagenSql.SQL_ANLAGE_INSERT_OHNE_FAHRPLAN;
            Assert.Contains("Albedo, Zeitprogramm, Vorlauf_Max)", mit, StringComparison.Ordinal);
            Assert.DoesNotContain("Zeitprogramm", ohne, StringComparison.Ordinal);
            int platzhalter = mit.Count(c => c == '?');
            Assert.Equal(platzhalter - 2, ohne.Count(c => c == '?'));
            int frei = FreieKuehlungSoleSchema.SPALTEN_ANLAGE.Count;
            DbParam[] p = AnlagenSql.AnlagenParameter(1, new WErzeugerModel { Zeitprogramm = "x", Vorlauf_Max = 55.0 });
            Assert.Equal(platzhalter + frei, p.Length);
            Assert.Equal("x", p[p.Length - frei - 2].Wert);
            Assert.Equal(55.0, p[p.Length - frei - 1].Wert);
            (string sql, DbParam[] werte) = AnlagenSql.Einfuegen(1, new WErzeugerModel(), null, false);
            Assert.Equal(ohne, sql);
            Assert.Equal(platzhalter - 2, werte.Length);
            Assert.Equal(mit, AnlagenSql.Einfuegen(1, new WErzeugerModel(), null, true, false).Sql);
            // Die Fachspaltenrettung des Speicherwegs sieht die zwei Spalten als Modellspalten.
            Assert.DoesNotContain(AnlagenfahrplanSchema.SPALTE_ZEITPROGRAMM, WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain(AnlagenfahrplanSchema.SPALTE_VORLAUF_MAX, WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Stunden 0 … 8760, Kelvinstunden ≥ 0, NULL erlaubt.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_Stunden_und_Kelvinstunden()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Energieanlagen\" (ID INTEGER PRIMARY KEY, Bezeichner TEXT) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisEnergiebedarf\" (ID INTEGER PRIMARY KEY) STRICT");
            foreach (var s in AnlagenfahrplanSchema.SPALTEN) Ausfuehren(c, AnlagenfahrplanSchema.Anlegen(s));
            Ausfuehren(c, "INSERT INTO Tab_Energieanlagen (ID, Bezeichner) VALUES (1, 'WP')");
            Ausfuehren(c, "INSERT INTO Tab_ErgebnisEnergiebedarf (ID) VALUES (1)");
            Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET Zeitprogramm = '" + Programm() + "', Vorlauf_Max = 55.5"));
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Vorlauf_Max = 'heiss'"));
            foreach (string s in new[] { "Komfort_Unterschreitungsstunden", "Komfort_Laengste_Strecke",
                                         "Fahrplan_Begrenzt_Stunden", "Komfort_Ueberschreitungsstunden" })
            {
                Assert.False(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = 8760"));
                Assert.False(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = NULL"));
                Assert.True(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = 8761"), s);
                Assert.True(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = -1"), s);
                Assert.True(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = 1.5"), s);
            }
            foreach (string s in new[] { "Komfort_Kelvinstunden", "Komfort_Kelvinstunden_Kuehlung" })
            {
                Assert.False(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = 12.25"));
                Assert.True(Wirft(c, "UPDATE Tab_ErgebnisEnergiebedarf SET " + s + " = -0.01"), s);
            }
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Stand davor, Wiederholbarkeit
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_traegt_keine_Saat()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= AnlagenfahrplanSchema.SCHRITT);
            Assert.True(AnlagenfahrplanSchema.Vollstaendig());
            // Gesät ist allein der Fahrplan des Referenzprojekts 1056 (AK2-4, referenzprojekt_1056_fahrplan.py) und seine
            // Kopie im Referenzprojekt AK3 1058 (AK3-W5a): Zeitprogramm an Kessel und BHKW, Vorlauf_Max an der Wärmepumpe.
            foreach (var s in AnlagenfahrplanSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL" +
                                      (s.Tabelle == AnlagenfahrplanSchema.TAB_ANLAGEN ? " AND ID_Projekt NOT IN (1056, 1058, 1059)" : "")));
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM \"" + AnlagenfahrplanSchema.TAB_ANLAGEN + "\" WHERE Zeitprogramm IS NOT NULL"));   // 1056, 1058, 1059 (K5a)
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM \"" + AnlagenfahrplanSchema.TAB_ANLAGEN + "\" WHERE Vorlauf_Max IS NOT NULL"));
            List<string> anlagen = DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ANLAGEN);
            List<string> ergebnis = DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ERGEBNIS);
            // Hinter den zwei Spalten stehen allein die drei der freien Kühlung (Schritt 187) und die sechs des
            // Erdsondenfeldes (Schritt 195).
            int frei = FreieKuehlungSoleSchema.SPALTEN_ANLAGE.Count + ErdsondenfeldSchema.SPALTEN.Count;
            Assert.Equal(new[] { "Zeitprogramm", "Vorlauf_Max" }, anlagen.Skip(anlagen.Count - frei - 2).Take(2).ToArray());
            // Dahinter stehen allein die sechs Kennzahlen des Kreises (Ak3Schema) und die sieben von AK3-K (Ak3KSchema).
            int ak3k = Ak3KSchema.SPALTEN_ERGEBNIS.Count;
            int ak3 = Ak3Schema.SPALTEN_ERGEBNIS.Count + ak3k;
            Assert.Equal(AnlagenfahrplanSchema.SPALTEN_ERGEBNIS.ToArray(), ergebnis.Skip(ergebnis.Count - ak3 - 6).Take(6).ToArray());
            Assert.Equal(Ak3Schema.SPALTEN_ERGEBNIS.ToArray(), ergebnis.Skip(ergebnis.Count - ak3).Take(ak3 - ak3k).ToArray());
            Assert.Equal(Ak3KSchema.SPALTEN_ERGEBNIS.ToArray(), ergebnis.Skip(ergebnis.Count - ak3k).ToArray());
            Assert.Equal(21 + 6 + ak3, ergebnis.Count);
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", AnlagenfahrplanSchema.TAB_ERGEBNIS)));
            Assert.Contains("\"Komfort_Unterschreitungsstunden\" INTEGER CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("\"Komfort_Kelvinstunden_Kuehlung\" REAL CHECK", ddl, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            int anlagen = DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ANLAGEN).Count;
            int ergebnis = DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ERGEBNIS).Count;
            foreach (var s in AnlagenfahrplanSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(AnlagenfahrplanSchema.Vollstaendig());
            Assert.False(AnlagenfahrplanSchema.AnlagenspaltenVorhanden());
            Assert.Equal(anlagen - 2, DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ANLAGEN).Count);

            // Ohne die Spalten (älterer Stand, etwa iOS) legt der Speicherweg die Anlagenzeile weiter an.
            WErzeugerCtrl a = NeuAnlegen(WP_ANLAGE);
            a.Zeitprogramm = Programm();
            Assert.True(a.Insert());

            var bericht = new List<string>();
            Assert.Equal(8, AnlagenfahrplanSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(AnlagenfahrplanSchema.Vollstaendig());
            Assert.Equal(anlagen, DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ANLAGEN).Count);
            Assert.Equal(ergebnis, DataRepository.SpaltenVonTabelle(AnlagenfahrplanSchema.TAB_ERGEBNIS).Count);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0, AnlagenfahrplanSchema.Ausfuehren(null));
        }

        // =============================================================================
        //  Teil 3 - Kopierwege des Erzeugers und Rundreise des Ergebnisses
        // =============================================================================

        private static WErzeugerCtrl Anlage(int id)
        {
            var c = new WErzeugerCtrl();
            c.ReadSingle("SELECT * FROM Tab_Energieanlagen WHERE ID = " + id.ToString(CultureInfo.InvariantCulture));
            return c;
        }

        /// <summary>
        /// Liest die Anlage und löscht ihre Zeile — der Speicherweg „Löschen + Neuanlegen"; ein Gerät hat je Projekt
        /// nur eine Anlagenzeile (eindeutiger Index ID_Projekt, ID_WP).
        /// </summary>
        private static WErzeugerCtrl NeuAnlegen(int id)
        {
            WErzeugerCtrl a = Anlage(id);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", id));
            return a;
        }

        /// <summary>Setzt die Wärmepumpe von 1007 auf Programm und 55 °C; alle übrigen Anlagen bleiben NULL.</summary>
        private static void Pflegen()
        {
            DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET Zeitprogramm = ?, Vorlauf_Max = ? WHERE ID = ?",
                new DbParam("@z", Programm()), new DbParam("@v", 55.0), new DbParam("@id", WP_ANLAGE));
        }

        /// <summary>Im Projekt: genau die Wärmepumpe trägt Programm und Vorlauf_Max, jede andere Anlage NULL.</summary>
        private static void PruefeProjekt(int idProjekt)
        {
            var c = new WErzeugerCtrl();
            c.ReadAllFilter("ID_Projekt = " + idProjekt.ToString(CultureInfo.InvariantCulture));
            List<WErzeugerModel> alle = c.items.ToList();
            Assert.True(alle.Count >= 2);
            WErzeugerModel wp = Assert.Single(alle, x => x.ID_Type == WizardItemClass.WP_TYP);
            Assert.Equal(Programm(), wp.Zeitprogramm);
            Assert.Equal(55.0, wp.Vorlauf_Max);
            Assert.All(alle.Where(x => x.ID_Type != WizardItemClass.WP_TYP), x =>
            {
                Assert.Null(x.Zeitprogramm);
                Assert.Null(x.Vorlauf_Max);
            });
        }

        [Fact]
        public void Lesen_und_Neuanlegen_tragen_beide_Spalten_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            WErzeugerCtrl a = NeuAnlegen(WP_ANLAGE);
            Assert.Equal(Programm(), a.Zeitprogramm);
            Assert.Equal(55.0, a.Vorlauf_Max);
            Assert.True(a.Insert());
            long neu = Zahl("SELECT MAX(ID) FROM Tab_Energieanlagen");
            Assert.Equal(Programm(), Convert.ToString(DataRepository.ExecuteScalar("SELECT Zeitprogramm FROM Tab_Energieanlagen WHERE ID = " + neu)));
            Assert.Equal(55.0, Convert.ToDouble(DataRepository.ExecuteScalar("SELECT Vorlauf_Max FROM Tab_Energieanlagen WHERE ID = " + neu)));

            WErzeugerCtrl leer = NeuAnlegen(10358);
            Assert.Null(leer.Zeitprogramm);
            Assert.Null(leer.Vorlauf_Max);
            Assert.True(leer.Insert());
            neu = Zahl("SELECT MAX(ID) FROM Tab_Energieanlagen");
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = " + neu + " AND Zeitprogramm IS NULL AND Vorlauf_Max IS NULL"));
        }

        [Fact]
        public void Das_Projektduplikat_traegt_den_Fahrplan()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Fahrplan AK2");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeProjekt(neu);
        }

        [Fact]
        public void Der_Projekttransfer_traegt_den_Fahrplan()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            string ordner = Path.Combine(Path.GetTempPath(), "epos-ak2-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "f.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Fahrplan AK2", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeProjekt(neu);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        [Fact]
        public void Die_Komponentenuebernahme_traegt_den_Fahrplan()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            int ziel = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Ziel AK2");
            Assert.True(ziel > 0);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET Zeitprogramm = NULL, Vorlauf_Max = NULL WHERE ID_Projekt = ?",
                                           new DbParam("@p", ziel));
            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(PROJEKT, ziel, "Wärmepumpe", out string fehler, out _), fehler);
            PruefeProjekt(ziel);
        }

        [Fact]
        public void Das_Ergebnis_traegt_die_sechs_Komfortspalten_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ErgebnisCtrl();
            var m = new ErgebnisModel
            {
                ID_Projekt = PROJEKT, Bezeichner = "AK2-1", Zeitstempel = new DateTime(2026, 10, 5),
                Sim_Energiebedarf = true,
                Energiebedarf = new ErgebnisEnergiebedarfModel { Waermebedarf_Gesamt = 12.5 },
            };
            Assert.True(ctrl.Save(m) > 0);
            ErgebnisEnergiebedarfModel leer = new ErgebnisCtrl().Load(PROJEKT).Energiebedarf;
            Assert.Null(leer.KomfortUnterschreitungsstundenH);
            Assert.Null(leer.KomfortKelvinstundenKh);
            Assert.Null(leer.FahrplanBegrenztStundenH);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE " +
                                  string.Join(" OR ", AnlagenfahrplanSchema.SPALTEN_ERGEBNIS.Select(s => s + " IS NOT NULL"))));

            m.Energiebedarf.KomfortUnterschreitungsstundenH = 120;
            m.Energiebedarf.KomfortKelvinstundenKh = 85.5;
            m.Energiebedarf.KomfortLaengsteStreckeH = 9;
            m.Energiebedarf.FahrplanBegrenztStundenH = 310;
            m.Energiebedarf.KomfortUeberschreitungsstundenH = 4;
            m.Energiebedarf.KomfortKelvinstundenKuehlungKh = 2.25;
            Assert.True(ctrl.Save(m) > 0);
            ErgebnisEnergiebedarfModel e = new ErgebnisCtrl().Load(PROJEKT).Energiebedarf;
            Assert.Equal(120, e.KomfortUnterschreitungsstundenH);
            Assert.Equal(85.5, e.KomfortKelvinstundenKh);
            Assert.Equal(9, e.KomfortLaengsteStreckeH);
            Assert.Equal(310, e.FahrplanBegrenztStundenH);
            Assert.Equal(4, e.KomfortUeberschreitungsstundenH);
            Assert.Equal(2.25, e.KomfortKelvinstundenKuehlungKh);
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql));

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            k.ExecuteNonQuery();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try
            {
                Ausfuehren(c, sql);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }
    }
}
