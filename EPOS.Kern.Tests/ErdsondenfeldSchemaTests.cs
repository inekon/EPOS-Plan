using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schritt 195 — Erdsondenfeld je Anlage</b> (<see cref="ErdsondenfeldSchema"/>): Nummer, Ziel und Register; die
    /// sechs Spalten samt Prüfklauseln; der Stand davor und die Wiederholbarkeit; die Fachspaltenrettung des
    /// Speicherwegs; das Lesen im Kern (<see cref="WaermequelleClass.SondenfeldgeometrieDerAnlage"/>: leer = Norm) und
    /// der Datenweg des Dialogs (<see cref="ErdsondenfeldCtrl"/>); die Wirkung einer gepflegten Geometrie auf das Feld.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErdsondenfeldSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Sonde des Referenzprojekts 1057 (4 × 90 m).</summary>
        private const int SONDE_1057 = 23850;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_195_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(AufheizAufschlagErgebnisSchema.SCHRITT + 1, ErdsondenfeldSchema.SCHRITT);
            Assert.Equal(195, ErdsondenfeldSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ErdsondenfeldSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ErdsondenfeldSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "WQ_Sondenabstand", "WQ_Bohrlochdurchmesser", "WQ_Bohrlochwiderstand",
                    "WQ_Kopfueberdeckung", "WQ_Betrachtungsjahr", "WQ_Sondenanordnung",
                },
                ErdsondenfeldSchema.Spaltennamen().ToArray());
            Assert.All(ErdsondenfeldSchema.SPALTEN, x => Assert.Equal("Tab_Energieanlagen", x.Tabelle));
            Assert.Equal(new[] { "Quadratisch", "Reihe" }, ErdsondenfeldSchema.ANORDNUNGEN.ToArray());
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_jede_Spalte()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Energieanlagen\" (ID INTEGER PRIMARY KEY, Bezeichner TEXT) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Energieanlagen (ID, Bezeichner) VALUES (1, 'WP')");
            foreach (var s in ErdsondenfeldSchema.SPALTEN) Ausfuehren(c, ErdsondenfeldSchema.Anlegen(s));
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE " +
                                       string.Join(" OR ", ErdsondenfeldSchema.Spaltennamen().Select(n => n + " IS NOT NULL"))));

            foreach (string sp in new[] { "WQ_Sondenabstand", "WQ_Bohrlochdurchmesser", "WQ_Bohrlochwiderstand" })
            {
                foreach (string w in new[] { "0.01", "6.0", "NULL" })
                    Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET " + sp + " = " + w), sp + " " + w);
                foreach (string w in new[] { "0", "-1", "'x'" })
                    Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET " + sp + " = " + w), sp + " " + w);
            }
            foreach (string w in new[] { "0", "2.5", "NULL" })
                Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET WQ_Kopfueberdeckung = " + w), w);
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET WQ_Kopfueberdeckung = -0.1"));
            foreach (string w in new[] { "1", "25", "NULL" })
                Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET WQ_Betrachtungsjahr = " + w), w);
            foreach (string w in new[] { "0", "1.5" })
                Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET WQ_Betrachtungsjahr = " + w), w);
            foreach (string w in new[] { "'Quadratisch'", "'Reihe'", "NULL" })
                Assert.False(Wirft(c, "UPDATE Tab_Energieanlagen SET WQ_Sondenanordnung = " + w), w);
            foreach (string w in new[] { "'quadratisch'", "'Ring'", "''" })
                Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET WQ_Sondenanordnung = " + w), w);
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Stand davor, Wiederholbarkeit, Rettung
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_traegt_keine_Saat()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= ErdsondenfeldSchema.SCHRITT);
            Assert.True(ErdsondenfeldSchema.Vollstaendig());
            foreach (string s in ErdsondenfeldSchema.Spaltennamen())
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE \"" + s + "\" IS NOT NULL"));
            List<string> anlagen = DataRepository.SpaltenVonTabelle(ErdsondenfeldSchema.TAB_ANLAGEN);
            // Dahinter stehen allein die zwei Anlagenspalten der Uebergabegrenze (Schritt 203).
            int ub = UebergabegrenzeSchema.SPALTEN_ANLAGE.Count;
            Assert.Equal(ErdsondenfeldSchema.Spaltennamen().ToArray(), anlagen.Skip(anlagen.Count - ub - 6).Take(6).ToArray());
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", ErdsondenfeldSchema.TAB_ANLAGEN)));
            Assert.Contains("\"WQ_Sondenabstand\" REAL CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("\"WQ_Betrachtungsjahr\" INTEGER CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("IN ('Quadratisch','Reihe')", ddl, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            int anlagen = DataRepository.SpaltenVonTabelle(ErdsondenfeldSchema.TAB_ANLAGEN).Count;
            foreach (var s in ErdsondenfeldSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(ErdsondenfeldSchema.Vollstaendig());

            // Vor dem Schritt rechnet jede Anlage mit der Norm, und der Dialog schreibt nichts.
            AssertNorm(WaermequelleClass.SondenfeldgeometrieDerAnlage(SONDE_1057));
            Assert.False(ErdsondenfeldCtrl.Schreiben(SONDE_1057, new ErdsondenfeldEingabe { AbstandM = 8 }));
            Assert.Null(ErdsondenfeldCtrl.Lesen(SONDE_1057).AbstandM);

            var bericht = new List<string>();
            Assert.Equal(6, ErdsondenfeldSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(ErdsondenfeldSchema.Vollstaendig());
            Assert.Equal(anlagen, DataRepository.SpaltenVonTabelle(ErdsondenfeldSchema.TAB_ANLAGEN).Count);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0, ErdsondenfeldSchema.Ausfuehren(null));
        }

        /// <summary>Die sechs Spalten sind Fachspalten: Die Rettung des Speicherwegs hält sie von selbst.</summary>
        [Fact]
        public void Die_Spalten_sind_Fachspalten_des_Speicherwegs()
        {
            if (!_db.Vorhanden) return;
            foreach (string s in ErdsondenfeldSchema.Spaltennamen())
                Assert.Contains(s, WizardCtrl.Fachspalten(), StringComparer.OrdinalIgnoreCase);
        }

        // =============================================================================
        //  Teil 3 - Kern: Lesen, Datenweg des Dialogs, Wirkung
        // =============================================================================

        [Fact]
        public void Leere_Spalten_ergeben_die_Norm()
        {
            if (!_db.Vorhanden) return;
            AssertNorm(WaermequelleClass.SondenfeldgeometrieDerAnlage(SONDE_1057));
            ErdsondenfeldEingabe e = ErdsondenfeldCtrl.Lesen(SONDE_1057);
            Assert.Null(e.AbstandM);
            Assert.Null(e.BohrlochdurchmesserMm);
            Assert.Null(e.Bohrlochwiderstand);
            Assert.Null(e.KopfueberdeckungM);
            Assert.Null(e.Betrachtungsjahr);
            Assert.Null(e.Anordnung);
            Assert.Equal(150.0, ErdsondenfeldCtrl.VorgabeBohrlochdurchmesserMm, 9);
        }

        [Fact]
        public void Die_Geometrie_kommt_aus_den_Spalten_und_leer_bleibt_Vorgabe()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ErdsondenfeldCtrl.Schreiben(SONDE_1057, new ErdsondenfeldEingabe
            {
                AbstandM = 8.0, BohrlochdurchmesserMm = 180.0, Bohrlochwiderstand = 0.08,
                KopfueberdeckungM = 0.0, Betrachtungsjahr = 25, Anordnung = Sondenanordnung.Reihe,
            }));
            Sondenfeldgeometrie g = WaermequelleClass.SondenfeldgeometrieDerAnlage(SONDE_1057);
            Assert.Equal(8.0, g.AbstandM);
            Assert.Equal(0.09, g.BohrlochradiusM, 12);
            Assert.Equal(0.08, g.Bohrlochwiderstand);
            Assert.Equal(0.0, g.KopfueberdeckungM);
            Assert.Equal(25, g.Betrachtungsjahr);
            Assert.Equal(Sondenanordnung.Reihe, g.Anordnung);
            Assert.Equal("Reihe", Text("SELECT WQ_Sondenanordnung FROM Tab_Energieanlagen WHERE ID = " + SONDE_1057));

            // Nur der Abstand gepflegt: alles andere ist wieder Vorgabe.
            Assert.True(ErdsondenfeldCtrl.Schreiben(SONDE_1057, new ErdsondenfeldEingabe { AbstandM = 5.0 }));
            g = WaermequelleClass.SondenfeldgeometrieDerAnlage(SONDE_1057);
            Assert.Equal(5.0, g.AbstandM);
            Sondenfeldgeometrie n = Sondenfeldgeometrie.Norm;
            Assert.Equal(n.BohrlochradiusM, g.BohrlochradiusM);
            Assert.Equal(n.Bohrlochwiderstand, g.Bohrlochwiderstand);
            Assert.Equal(n.KopfueberdeckungM, g.KopfueberdeckungM);
            Assert.Equal(n.Betrachtungsjahr, g.Betrachtungsjahr);
            Assert.Equal(n.Anordnung, g.Anordnung);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WQ_Sondenabstand IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WQ_Bohrlochdurchmesser IS NOT NULL"));

            // Alles leer: wieder die Norm.
            Assert.True(ErdsondenfeldCtrl.Schreiben(SONDE_1057, new ErdsondenfeldEingabe()));
            AssertNorm(WaermequelleClass.SondenfeldgeometrieDerAnlage(SONDE_1057));
        }

        [Fact]
        public void Ein_unzulaessiger_Wert_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            Assert.False(ErdsondenfeldCtrl.Zulaessig(new ErdsondenfeldEingabe { AbstandM = 0 }));
            Assert.False(ErdsondenfeldCtrl.Zulaessig(new ErdsondenfeldEingabe { KopfueberdeckungM = -1 }));
            Assert.False(ErdsondenfeldCtrl.Zulaessig(new ErdsondenfeldEingabe { Betrachtungsjahr = 0 }));
            Assert.False(ErdsondenfeldCtrl.Zulaessig(new ErdsondenfeldEingabe { Bohrlochwiderstand = double.NaN }));
            Assert.True(ErdsondenfeldCtrl.Zulaessig(new ErdsondenfeldEingabe { KopfueberdeckungM = 0 }));
            Assert.False(ErdsondenfeldCtrl.Schreiben(SONDE_1057, new ErdsondenfeldEingabe { AbstandM = 7, BohrlochdurchmesserMm = -5 }));
            Assert.Null(ErdsondenfeldCtrl.Lesen(SONDE_1057).AbstandM);
            Assert.Null(ErdsondenfeldCtrl.AnordnungAusText("Ring"));
            Assert.Equal(Sondenanordnung.Reihe, ErdsondenfeldCtrl.AnordnungAusText(" reihe "));
        }

        /// <summary>Ein anderer Abstand und die Reihe ändern die Sprungantwort des Feldes; die Norm ist die Vorgabe.</summary>
        [Fact]
        public void Ein_anderer_Abstand_wirkt_auf_das_Feld()
        {
            const double TAU = 8760.0 * 10;
            var norm = new Erdsondenfeld(90, 4, 2.0, 2.2, 11.0, Sondenfeldgeometrie.Norm);
            var ohne = new Erdsondenfeld(90, 4, 2.0, 2.2, 11.0);
            Assert.Equal(norm.G(TAU), ohne.G(TAU));

            var eng = new Erdsondenfeld(90, 4, 2.0, 2.2, 11.0, new Sondenfeldgeometrie { AbstandM = 3.0 });
            var weit = new Erdsondenfeld(90, 4, 2.0, 2.2, 11.0, new Sondenfeldgeometrie { AbstandM = 10.0 });
            Assert.True(eng.G(TAU) > norm.G(TAU));
            Assert.True(weit.G(TAU) < norm.G(TAU));

            // Vier Sonden in einer Reihe stehen weiter auseinander als im Quadrat 2 × 2: weniger Einfluss.
            var reihe = new Erdsondenfeld(90, 4, 2.0, 2.2, 11.0, new Sondenfeldgeometrie { Anordnung = Sondenanordnung.Reihe });
            Assert.True(reihe.G(TAU) < norm.G(TAU));
            Assert.Equal(new[] { (0.0, 0.0), (6.0, 0.0), (12.0, 0.0), (18.0, 0.0) },
                         Erdsondenfeld.Lagen(4, 6.0, Sondenanordnung.Reihe).ToArray());
            Assert.Equal(Erdsondenfeld.Lagen(4, 6.0).ToArray(), Erdsondenfeld.Lagen(4, 6.0, Sondenanordnung.Quadratisch).ToArray());
        }

        // =============================================================================

        private static void AssertNorm(Sondenfeldgeometrie g)
        {
            Sondenfeldgeometrie n = Sondenfeldgeometrie.Norm;
            Assert.Equal(n.AbstandM, g.AbstandM);
            Assert.Equal(n.BohrlochradiusM, g.BohrlochradiusM);
            Assert.Equal(n.Bohrlochwiderstand, g.Bohrlochwiderstand);
            Assert.Equal(n.KopfueberdeckungM, g.KopfueberdeckungM);
            Assert.Equal(n.Betrachtungsjahr, g.Betrachtungsjahr);
            Assert.Equal(n.Anordnung, g.Anordnung);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql)
            => Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

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
