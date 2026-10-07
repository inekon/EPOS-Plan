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
    /// <b>Der Schemaschritt der Stufe AK3</b> (<see cref="Ak3Schema"/>; Entwurf AK3, Festlegungen 22 und 23):
    /// <c>Heizkurve_Raumeinfluss</c> an beiden Gebäudetabellen samt zehntem Sichtneubau und Kopierwegen, die Kennzahlen
    /// des Kreises an <c>Tab_ErgebnisEnergiebedarf</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class Ak3SchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt mit genau einem Gebäude (wie in den Schematests der Erdreichvorgabe).</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Vorgaengerklasse_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(FlaechenherkunftSchema.SCHRITT + 1, Ak3Schema.SCHRITT);
            Assert.Equal(198, Ak3Schema.SCHRITT);
            Assert.True(Ak3Schema.SCHRITT <= SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == Ak3Schema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Tab_Gebaeude.Heizkurve_Raumeinfluss", "Tab_Gebaeude_STAMM.Heizkurve_Raumeinfluss",
                    "Tab_ErgebnisEnergiebedarf.Ak3_Durchlaeufe_Mittel", "Tab_ErgebnisEnergiebedarf.Ak3_Durchlaeufe_Max",
                    "Tab_ErgebnisEnergiebedarf.Ak3_Fallwechsel", "Tab_ErgebnisEnergiebedarf.Ak3_Schranke_Stunden",
                    "Tab_ErgebnisEnergiebedarf.Ak3_Speicher_Leer_Stunden", "Tab_ErgebnisEnergiebedarf.Ak3_Restbedarf_Stunden",
                },
                Ak3Schema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
        }

        [Fact]
        public void Der_zehnte_Sichtneubau_haengt_den_Raumeinfluss_hinter_die_Erdreichvorgabe()
        {
            Assert.Equal(105, GebaeudeSchema.SICHT_AK3.Length);
            Assert.Equal("Heizkurve_Raumeinfluss", GebaeudeSchema.SICHT_AK3[104]);
            Assert.Equal(GebaeudeSchema.SICHT_ERDREICH_VORGABE, GebaeudeSchema.SICHT_AK3.Take(104));
            Assert.Equal(GebaeudeSchema.SICHT_AK3, GebaeudeSchema.SICHT_AKTUELL);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AK3, GebaeudeSchema.SQL_VIEW_AKTUELL);
            Assert.Contains("Tab_Gebaeude.Heizkurve_Raumeinfluss", GebaeudeSchema.SQL_VIEW_AKTUELL, StringComparison.Ordinal);
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Raumeinfluss 0 bis 10, Kennzahlen in ihren Grenzen.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach ((string _, string spalte, string typ) in Ak3Schema.SPALTEN.GroupBy(x => x.Spalte).Select(g => g.First()))
                Ausfuehren(c, Ak3Schema.Anlegen(("T", spalte, typ)));
            foreach (string gut in new[] { "NULL", "0", "0.5", "1", "10" })
                Assert.False(Wirft(c, "UPDATE T SET Heizkurve_Raumeinfluss = " + gut), gut);
            foreach (string schlecht in new[] { "-0.1", "10.01", "'abc'" })
                Assert.True(Wirft(c, "UPDATE T SET Heizkurve_Raumeinfluss = " + schlecht), schlecht);
            Assert.False(Wirft(c, "UPDATE T SET Ak3_Durchlaeufe_Mittel = 1.25, Ak3_Durchlaeufe_Max = 3, Ak3_Fallwechsel = 0, " +
                                  "Ak3_Schranke_Stunden = 8760, Ak3_Speicher_Leer_Stunden = 0, Ak3_Restbedarf_Stunden = 12"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Durchlaeufe_Mittel = 0.5"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Durchlaeufe_Max = 0"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Fallwechsel = -1"));
            Assert.True(Wirft(c, "UPDATE T SET Ak3_Schranke_Stunden = 8761"));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank, die Rundreise und die Kopierwege
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= Ak3Schema.SCHRITT);
            Assert.True(Ak3Schema.Vollstaendig());
            Assert.True(Ak3Schema.ErgebnisspaltenVorhanden());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            // Gesät ist allein das Referenzprojekt AK3 1058 (AK3-W5a, referenzprojekt_1058_ak3.py): Stufe AK3 und k_R 1 K/K.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Heizkurve_Raumeinfluss IS NOT NULL AND ID_Projekt <> 1058"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Heizkurve_Raumeinfluss = 1.0 AND ID_Projekt = 1058"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Heizkurve_Raumeinfluss IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE Ak3_Durchlaeufe_Mittel IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Anlagenkopplung = 'AK3' AND ID_Projekt <> 1058"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Anlagenkopplung = 'AK3' AND ID_Projekt = 1058"));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Heizkurve_Raumeinfluss = -1 WHERE ID = " + GEBAEUDE));
        }

        /// <summary>Rundreise: aus dem Stand davor legt der Schritt alle Spalten und die Sicht an; zweimal = nichts.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach ((string tabelle, string spalte, string _) in Ak3Schema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE " + tabelle + " DROP COLUMN " + spalte);
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_ERDREICH_VORGABE);
            long zeilen = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude");
            Assert.False(Ak3Schema.Vollstaendig());
            Assert.True(ErdreichVorgabeSchema.SichtSteht());

            var bericht = new List<string>();
            Assert.Equal(8, Ak3Schema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("105 Spalten", StringComparison.Ordinal));
            Assert.True(Ak3Schema.Vollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            Assert.Equal(0, Ak3Schema.Ausfuehren(null));
        }

        /// <summary>Der Leser der Sicht liefert den Wert, NULL bleibt null; die Projektkopie liest ihn ebenso.</summary>
        [Fact]
        public void Der_Leser_der_Sicht_liefert_den_Raumeinfluss()
        {
            if (!_db.Vorhanden) return;
            Assert.Null(Projektgebaeude().Heizkurve_Raumeinfluss);
            Setzen(1.5);
            Assert.Equal(1.5, Projektgebaeude().Heizkurve_Raumeinfluss);
            Assert.Equal(1.5, GebaeudeStammCtrl.LiesProjektkopie(GEBAEUDE).Heizkurve_Raumeinfluss);
        }

        /// <summary>Das Projektduplikat (derselbe Weg wie „Variante anlegen") trägt den Wert.</summary>
        [Fact]
        public void Projektduplikat_traegt_den_Raumeinfluss()
        {
            if (!_db.Vorhanden) return;
            Setzen(1.5);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " AK3");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(1.5, Wert(neu));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt den Wert über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_den_Raumeinfluss()
        {
            if (!_db.Vorhanden) return;
            Setzen(1.5);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-ak3-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer AK3", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(1.5, Wert(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { }
            }
        }

        /// <summary>
        /// Katalog ↔ Projekt: Speichern im Katalog und Lesen, Übernahme in ein Projekt (Projektkopie) und zurück in den
        /// Katalog (Kopfspalten) — der Wert geht NULL-erhaltend hinüber.
        /// </summary>
        [Fact]
        public void Katalog_und_Projektkopie_tragen_den_Raumeinfluss()
        {
            if (!_db.Vorhanden) return;
            Assert.Contains("[Heizkurve_Raumeinfluss]", GebaeudeStammCtrl.KOPFSPALTEN, StringComparison.Ordinal);
            var ctrl = new GebaeudeStammCtrl();
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            GebaeudeModel m = ctrl.Lies(name);
            Assert.NotNull(m);
            Assert.Null(m.Heizkurve_Raumeinfluss);

            // Katalog speichern und lesen (Insert): der Wert und NULL bleiben, was sie sind.
            m.Gebaeudename = "AK3 Raumeinfluss Probe";
            m.Heizkurve_Raumeinfluss = 2.0;
            Assert.True(ctrl.Insert(m));
            Assert.Equal(2.0, ctrl.Lies("AK3 Raumeinfluss Probe").Heizkurve_Raumeinfluss);

            // Katalog → Projekt (Projektkopie).
            int zuordnung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?", new DbParam("?", GEBAEUDE)), CultureInfo.InvariantCulture);
            int kopie = ctrl.CopyFromStamm("AK3 Raumeinfluss Probe", PROJEKT, zuordnung);
            Assert.True(kopie > 0, "Kopie in das Projekt fehlgeschlagen.");
            Assert.Equal(2.0, GebaeudeStammCtrl.LiesProjektkopie(kopie).Heizkurve_Raumeinfluss);

            // Überschreiben im Katalog (Overwrite) mit NULL: NULL bleibt NULL.
            GebaeudeModel n = ctrl.Lies("AK3 Raumeinfluss Probe");
            n.Heizkurve_Raumeinfluss = null;
            Assert.True(ctrl.Overwrite(n));
            Assert.Null(ctrl.Lies("AK3 Raumeinfluss Probe").Heizkurve_Raumeinfluss);
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static void Setzen(double wert)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Heizkurve_Raumeinfluss = ? WHERE ID = ?",
                                                     new DbParam("@w", wert), new DbParam("@id", GEBAEUDE)));

        private static double? Wert(int idProjekt)
        {
            object w = DataRepository.ExecuteScalar("SELECT Heizkurve_Raumeinfluss FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                                                    new DbParam("?", idProjekt));
            return w == null || w == DBNull.Value ? null : Convert.ToDouble(w, CultureInfo.InvariantCulture);
        }

        private static ProjektGebaeudeModel Projektgebaeude()
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            return ctrl.items.Single(x => x.ID_Gebaeude == GEBAEUDE);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static bool Wirft(string sql)
        {
            try { return !DataRepository.ExecuteSQL(sql); }
            catch (Exception) { return true; }
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
