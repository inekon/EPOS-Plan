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
    /// <b>Schritt 187 — freie Kühlung über die Wärmequelle</b> (KU3-6a, <see cref="FreieKuehlungSoleSchema"/>): Nummer,
    /// Ziel und Register; die sieben Spalten samt Prüfklauseln; der Stand davor und die Wiederholbarkeit; die
    /// Anlagen-Anweisung je Stand der Datenbank; die Kopierwege des Erzeugers (Speichern, Konfiguration,
    /// Projektduplikat, Projekttransfer, Komponentenübernahme — NULL-erhaltend) und die Rundreise der Zähler im
    /// Ergebnis der Wärmepumpe.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class FreieKuehlungSoleSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int WP_ANLAGE = 10353;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_187_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(AnlagenfahrplanSchema.SCHRITT + 1, FreieKuehlungSoleSchema.SCHRITT);
            Assert.Equal(187, FreieKuehlungSoleSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= FreieKuehlungSoleSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == FreieKuehlungSoleSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Tab_Energieanlagen.Kuehl_Frei", "Tab_Energieanlagen.Kuehl_Frei_Graedigkeit_K",
                    "Tab_Energieanlagen.Kuehl_Frei_Leistung_kW",
                    "Tab_ErgebnisWaermepumpe.FreieKuehlung_MWh", "Tab_ErgebnisWaermepumpe.FreieKuehlung_Stunden",
                    "Tab_ErgebnisWaermepumpeModul.FreieKuehlung_MWh", "Tab_ErgebnisWaermepumpeModul.FreieKuehlung_Stunden",
                },
                FreieKuehlungSoleSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
        }

        /// <summary>
        /// Die Anlagen-Anweisung nennt die drei Spalten zuletzt; die Variante des Stands 186 hat drei Platzhalter
        /// weniger, die vor 186 fünf. Werte außerhalb der Prüfklausel fallen zu NULL, der Schalter wird 0/1.
        /// </summary>
        [Fact]
        public void Die_Anlagenanweisung_fuehrt_die_drei_Spalten_zuletzt()
        {
            // UB-E2: Hinter den drei Spalten stehen Einbindung und Vorwaermbetrieb - die Anweisung ohne Uebergabegrenze
            // fuehrt die drei zuletzt.
            string mit = AnlagenSql.SQL_ANLAGE_INSERT_OHNE_UEBERGABE;
            string ohne = AnlagenSql.SQL_ANLAGE_INSERT_OHNE_FREIE_KUEHLUNG;
            int ueb = UebergabegrenzeSchema.SPALTEN_ANLAGE.Count;
            Assert.Contains("Vorlauf_Max, Kuehl_Frei, Kuehl_Frei_Graedigkeit_K, Kuehl_Frei_Leistung_kW)", mit, StringComparison.Ordinal);
            Assert.DoesNotContain("Kuehl_Frei", ohne, StringComparison.Ordinal);
            int platzhalter = mit.Count(c => c == '?');
            Assert.Equal(platzhalter + ueb, AnlagenSql.SQL_ANLAGE_INSERT.Count(c => c == '?'));
            Assert.Equal(platzhalter - 3, ohne.Count(c => c == '?'));
            Assert.Equal(platzhalter - 5, AnlagenSql.SQL_ANLAGE_INSERT_OHNE_FAHRPLAN.Count(c => c == '?'));

            DbParam[] p = AnlagenSql.AnlagenParameter(1, new WErzeugerModel
                { Kuehl_Frei = true, Kuehl_Frei_Graedigkeit_K = 2.5, Kuehl_Frei_Leistung_kW = 40.0 });
            Assert.Equal(platzhalter + ueb, p.Length);
            Assert.Equal(1, p[p.Length - ueb - 3].Wert);
            Assert.Equal(2.5, p[p.Length - ueb - 2].Wert);
            Assert.Equal(40.0, p[p.Length - ueb - 1].Wert);

            DbParam[] leer = AnlagenSql.AnlagenParameter(1, new WErzeugerModel
                { Kuehl_Frei_Graedigkeit_K = 20.5, Kuehl_Frei_Leistung_kW = 0.0 });
            Assert.Equal(0, leer[leer.Length - ueb - 3].Wert);
            Assert.True(leer[leer.Length - ueb - 2].Wert == null || leer[leer.Length - ueb - 2].Wert == DBNull.Value);
            Assert.True(leer[leer.Length - ueb - 1].Wert == null || leer[leer.Length - ueb - 1].Wert == DBNull.Value);

            Assert.Equal(mit, AnlagenSql.Einfuegen(1, new WErzeugerModel(), null, true, true, false).Sql);
            Assert.Equal(AnlagenSql.SQL_ANLAGE_INSERT, AnlagenSql.Einfuegen(1, new WErzeugerModel(), null, true, true, true).Sql);
            (string sql, DbParam[] werte) = AnlagenSql.Einfuegen(1, new WErzeugerModel(), null, true, false);
            Assert.Equal(ohne, sql);
            Assert.Equal(platzhalter - 3, werte.Length);
            // Ohne Fahrplan entfällt auch die freie Kühlung - der spätere Schritt setzt den früheren voraus.
            (sql, werte) = AnlagenSql.Einfuegen(1, new WErzeugerModel(), null, false, true);
            Assert.Equal(AnlagenSql.SQL_ANLAGE_INSERT_OHNE_FAHRPLAN, sql);
            Assert.Equal(platzhalter - 5, werte.Length);

            // Die Fachspaltenrettung des Speicherwegs sieht die drei Spalten als Modellspalten.
            foreach (string s in FreieKuehlungSoleSchema.SPALTEN_ANLAGE)
                Assert.DoesNotContain(s, WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Schalter 0/1, Grädigkeit 0 … 20, Leistung &gt; 0, Zähler.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_Schalter_Graedigkeit_Leistung_und_Zaehler()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Energieanlagen\" (ID INTEGER PRIMARY KEY, Bezeichner TEXT) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisWaermepumpe\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisWaermepumpeModul\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Energieanlagen (ID, Bezeichner) VALUES (1, 'WP')");
            foreach (var s in FreieKuehlungSoleSchema.SPALTEN) Ausfuehren(c, FreieKuehlungSoleSchema.Anlegen(s));
            Assert.Equal(0L, Skalar(c, "SELECT Kuehl_Frei FROM Tab_Energieanlagen WHERE ID = 1"));
            Ausfuehren(c, "INSERT INTO Tab_ErgebnisWaermepumpe (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_ErgebnisWaermepumpeModul (ID) VALUES (1)");

            Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei = 1"));
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei = 2"));
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei = NULL"));
            foreach (string w in new[] { "0", "3.0", "20", "NULL" })
                Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei_Graedigkeit_K = " + w), w);
            foreach (string w in new[] { "-0.1", "20.01" })
                Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei_Graedigkeit_K = " + w), w);
            foreach (string w in new[] { "0.5", "NULL" })
                Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei_Leistung_kW = " + w), w);
            foreach (string w in new[] { "0", "-5" })
                Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Kuehl_Frei_Leistung_kW = " + w), w);

            foreach (string t in FreieKuehlungSoleSchema.TABELLEN_ERGEBNIS)
            {
                Assert.False(Wirft(c, "UPDATE " + t + " SET FreieKuehlung_MWh = 12.5, FreieKuehlung_Stunden = 8760"), t);
                Assert.False(Wirft(c, "UPDATE " + t + " SET FreieKuehlung_MWh = NULL, FreieKuehlung_Stunden = NULL"), t);
                Assert.True(Wirft(c, "UPDATE " + t + " SET FreieKuehlung_MWh = -0.01"), t);
                Assert.True(Wirft(c, "UPDATE " + t + " SET FreieKuehlung_Stunden = 8761"), t);
                Assert.True(Wirft(c, "UPDATE " + t + " SET FreieKuehlung_Stunden = -1"), t);
                Assert.True(Wirft(c, "UPDATE " + t + " SET FreieKuehlung_Stunden = 1.5"), t);
            }
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Stand davor, Wiederholbarkeit
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_traegt_keine_Saat()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= FreieKuehlungSoleSchema.SCHRITT);
            Assert.True(FreieKuehlungSoleSchema.Vollstaendig());
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen") > 0);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kuehl_Frei <> 0"));
            foreach (var s in FreieKuehlungSoleSchema.SPALTEN.Where(x => x.Spalte != FreieKuehlungSoleSchema.SPALTE_KUEHL_FREI))
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            List<string> anlagen = DataRepository.SpaltenVonTabelle(FreieKuehlungSoleSchema.TAB_ANLAGEN);
            // Hinter den drei Spalten stehen allein die sechs des Erdsondenfeldes (Schritt 195) und die zwei der
            // Uebergabegrenze (Schritt 205).
            int sonde = ErdsondenfeldSchema.SPALTEN.Count + UebergabegrenzeSchema.SPALTEN_ANLAGE.Count;
            Assert.Equal(FreieKuehlungSoleSchema.SPALTEN_ANLAGE.ToArray(), anlagen.Skip(anlagen.Count - sonde - 3).Take(3).ToArray());
            foreach (string t in FreieKuehlungSoleSchema.TABELLEN_ERGEBNIS)
            {
                List<string> erg = DataRepository.SpaltenVonTabelle(t);
                // An der Modulzeile stehen dahinter allein die drei Spalten der Vorlaufwahl (Schritt 188).
                // Dahinter die Ergebnisspalten der Uebergabegrenze (Schritt 205) an beiden Tabellen.
                int danach = (t == VorlaufwahlSchema.TAB_ERGEBNIS_WP_MODUL ? VorlaufwahlSchema.SPALTEN_ERGEBNIS.Count : 0) +
                             UebergabegrenzeSchema.SPALTEN.Count(x => x.Tabelle == t);
                Assert.Equal(FreieKuehlungSoleSchema.SPALTEN_ERGEBNIS.ToArray(), erg.Skip(erg.Count - danach - 2).Take(2).ToArray());
            }
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", FreieKuehlungSoleSchema.TAB_ANLAGEN)));
            Assert.Contains("\"Kuehl_Frei\" INTEGER NOT NULL DEFAULT 0 CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("\"Kuehl_Frei_Graedigkeit_K\" REAL CHECK", ddl, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            int anlagen = DataRepository.SpaltenVonTabelle(FreieKuehlungSoleSchema.TAB_ANLAGEN).Count;
            foreach (var s in FreieKuehlungSoleSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(FreieKuehlungSoleSchema.Vollstaendig());
            Assert.False(FreieKuehlungSoleSchema.AnlagenspaltenVorhanden());
            Assert.False(FreieKuehlungSoleSchema.ErgebnisspaltenVorhanden());
            Assert.Equal(anlagen - 3, DataRepository.SpaltenVonTabelle(FreieKuehlungSoleSchema.TAB_ANLAGEN).Count);

            // Ohne die Spalten (Stand 186, etwa iOS) legen Speicherweg und Ergebnis ihre Zeilen weiter an.
            WErzeugerCtrl a = NeuAnlegen(WP_ANLAGE);
            a.Kuehl_Frei = true;
            Assert.True(a.Insert());
            Assert.True(new ErgebnisCtrl().Save(ErgebnisMitWaermepumpe(5.0, 300)) > 0);

            var bericht = new List<string>();
            Assert.Equal(7, FreieKuehlungSoleSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(FreieKuehlungSoleSchema.Vollstaendig());
            Assert.Equal(anlagen, DataRepository.SpaltenVonTabelle(FreieKuehlungSoleSchema.TAB_ANLAGEN).Count);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kuehl_Frei <> 0"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0, FreieKuehlungSoleSchema.Ausfuehren(null));
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

        /// <summary>Liest die Anlage und löscht ihre Zeile — der Speicherweg „Löschen + Neuanlegen".</summary>
        private static WErzeugerCtrl NeuAnlegen(int id)
        {
            WErzeugerCtrl a = Anlage(id);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", id));
            return a;
        }

        /// <summary>Setzt die Wärmepumpe von 1007 auf freie Kühlung, 2,5 K und 40 kW; alle übrigen Anlagen bleiben aus und NULL.</summary>
        private static void Pflegen()
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET Kuehl_Frei = 1, Kuehl_Frei_Graedigkeit_K = ?, Kuehl_Frei_Leistung_kW = ? WHERE ID = ?",
                new DbParam("@g", 2.5), new DbParam("@l", 40.0), new DbParam("@id", WP_ANLAGE));
        }

        /// <summary>Im Projekt: genau die Wärmepumpe trägt die freie Kühlung, jede andere Anlage aus und NULL.</summary>
        private static void PruefeProjekt(int idProjekt)
        {
            var c = new WErzeugerCtrl();
            c.ReadAllFilter("ID_Projekt = " + idProjekt.ToString(CultureInfo.InvariantCulture));
            List<WErzeugerModel> alle = c.items.ToList();
            Assert.True(alle.Count >= 2);
            WErzeugerModel wp = Assert.Single(alle, x => x.ID_Type == WizardItemClass.WP_TYP);
            Assert.True(wp.Kuehl_Frei);
            Assert.Equal(2.5, wp.Kuehl_Frei_Graedigkeit_K);
            Assert.Equal(40.0, wp.Kuehl_Frei_Leistung_kW);
            Assert.All(alle.Where(x => x.ID_Type != WizardItemClass.WP_TYP), x =>
            {
                Assert.False(x.Kuehl_Frei);
                Assert.Null(x.Kuehl_Frei_Graedigkeit_K);
                Assert.Null(x.Kuehl_Frei_Leistung_kW);
            });
        }

        [Fact]
        public void Lesen_und_Neuanlegen_tragen_die_drei_Spalten_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            WErzeugerCtrl a = NeuAnlegen(WP_ANLAGE);
            Assert.True(a.Kuehl_Frei);
            Assert.Equal(2.5, a.Kuehl_Frei_Graedigkeit_K);
            Assert.Equal(40.0, a.Kuehl_Frei_Leistung_kW);
            Assert.True(a.Insert());
            long neu = Zahl("SELECT MAX(ID) FROM Tab_Energieanlagen");
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = " + neu +
                                  " AND Kuehl_Frei = 1 AND Kuehl_Frei_Graedigkeit_K = 2.5 AND Kuehl_Frei_Leistung_kW = 40.0"));

            WErzeugerCtrl leer = NeuAnlegen(10358);
            Assert.False(leer.Kuehl_Frei);
            Assert.Null(leer.Kuehl_Frei_Graedigkeit_K);
            Assert.Null(leer.Kuehl_Frei_Leistung_kW);
            Assert.True(leer.Insert());
            neu = Zahl("SELECT MAX(ID) FROM Tab_Energieanlagen");
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = " + neu +
                                  " AND Kuehl_Frei = 0 AND Kuehl_Frei_Graedigkeit_K IS NULL AND Kuehl_Frei_Leistung_kW IS NULL"));
        }

        /// <summary>Der Konfigurationsweg schreibt die drei Felder nur, wenn der Wirt sie führt — NULL bleibt NULL.</summary>
        [Fact]
        public void Die_Konfiguration_schreibt_die_drei_Felder_nur_auf_Zuruf()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(WP_ANLAGE, PROJEKT, new WErzeugerCtrl.KonfigurationFelder()).Ok);
            Assert.Equal(1L, Zahl("SELECT Kuehl_Frei FROM Tab_Energieanlagen WHERE ID = " + WP_ANLAGE));
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(WP_ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(FreieKuehlung: true, KuehlFrei: false,
                                                       KuehlFreiGraedigkeitK: null, KuehlFreiLeistungKw: 25.0)).Ok);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = " + WP_ANLAGE +
                                  " AND Kuehl_Frei = 0 AND Kuehl_Frei_Graedigkeit_K IS NULL AND Kuehl_Frei_Leistung_kW = 25.0"));
        }

        [Fact]
        public void Das_Projektduplikat_traegt_die_freie_Kuehlung()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Freie Kuehlung KU3-6");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeProjekt(neu);
        }

        [Fact]
        public void Der_Projekttransfer_traegt_die_freie_Kuehlung()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            string ordner = Path.Combine(Path.GetTempPath(), "epos-ku36-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "f.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Freie Kuehlung KU3-6", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
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
        public void Die_Komponentenuebernahme_traegt_die_freie_Kuehlung()
        {
            if (!_db.Vorhanden) return;
            Pflegen();
            int ziel = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Ziel KU3-6");
            Assert.True(ziel > 0);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET Kuehl_Frei = 0, Kuehl_Frei_Graedigkeit_K = NULL, Kuehl_Frei_Leistung_kW = NULL WHERE ID_Projekt = ?",
                new DbParam("@p", ziel));
            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(PROJEKT, ziel, "Wärmepumpe", out string fehler, out _), fehler);
            PruefeProjekt(ziel);
        }

        private static ErgebnisModel ErgebnisMitWaermepumpe(double? frei, int? stunden)
        {
            return new ErgebnisModel
            {
                ID_Projekt = PROJEKT, Bezeichner = "KU3-6a", Zeitstempel = new DateTime(2026, 10, 5),
                Sim_Waermepumpe = true,
                Waermepumpe = new ErgebnisWaermepumpeModel
                {
                    Waermeproduktion_WP = 80.0, Kaelteproduktion_WP = 20.0,
                    FreieKuehlung_MWh = frei, FreieKuehlung_Stunden = stunden,
                    Module = new List<ErgebnisWaermepumpeModulModel>
                    {
                        new ErgebnisWaermepumpeModulModel
                        {
                            Modul = "WP 1", Leistung = 50.0, Waermeproduktion = 80.0, Kaelteproduktion = 20.0,
                            FreieKuehlung_MWh = frei, FreieKuehlung_Stunden = stunden,
                        },
                    },
                },
            };
        }

        [Fact]
        public void Das_Ergebnis_traegt_die_zwei_Zaehler_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ErgebnisCtrl();
            Assert.True(ctrl.Save(ErgebnisMitWaermepumpe(null, null)) > 0);
            ErgebnisWaermepumpeModel leer = new ErgebnisCtrl().Load(PROJEKT).Waermepumpe;
            Assert.NotNull(leer);
            Assert.Null(leer.FreieKuehlung_MWh);
            Assert.Null(leer.FreieKuehlung_Stunden);
            Assert.Null(Assert.Single(leer.Module).FreieKuehlung_MWh);
            Assert.Null(leer.Module[0].FreieKuehlung_Stunden);
            foreach (string t in FreieKuehlungSoleSchema.TABELLEN_ERGEBNIS)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + t + " WHERE FreieKuehlung_MWh IS NOT NULL OR FreieKuehlung_Stunden IS NOT NULL"));

            Assert.True(ctrl.Save(ErgebnisMitWaermepumpe(6.25, 412)) > 0);
            ErgebnisWaermepumpeModel w = new ErgebnisCtrl().Load(PROJEKT).Waermepumpe;
            Assert.Equal(6.25, w.FreieKuehlung_MWh);
            Assert.Equal(412, w.FreieKuehlung_Stunden);
            ErgebnisWaermepumpeModulModel mo = Assert.Single(w.Module);
            Assert.Equal(6.25, mo.FreieKuehlung_MWh);
            Assert.Equal(412, mo.FreieKuehlung_Stunden);
            Assert.Equal(20.0, mo.Kaelteproduktion);
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

        private static long Skalar(SqliteConnection c, string sql)
        {
            using var k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToInt64(k.ExecuteScalar());
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
