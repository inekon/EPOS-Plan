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
    /// <b>Der Schemaschritt der Kühlkurve</b> (<see cref="KuehlkurveSchema"/>; Entwurf KK, Festlegungen 1, 3, 7, 12):
    /// fünf Eingabespalten an beiden Gebäudetabellen samt elftem Sichtneubau und Kopierwegen, drei Kennzahlen an
    /// <c>Tab_ErgebnisEnergiebedarf</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehlkurveSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt mit genau einem Gebäude (wie in den Schematests von AK3).</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Vorgaengerklasse_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(Ak3KSchema.SCHRITT + 1, KuehlkurveSchema.SCHRITT);
            Assert.Equal(202, KuehlkurveSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KuehlkurveSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KuehlkurveSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            string[] kurve =
            {
                "Kuehlkurve_Aktiv", "Kuehlkurve_Fusspunkt", "Kuehlkurve_Raumeinfluss", "Kuehlkurve_Auslegung_Weg",
                "Kuehlkurve_Auslegung_Aussen",
            };
            Assert.Equal(kurve.Select(k => "Tab_Gebaeude." + k)
                              .Concat(kurve.Select(k => "Tab_Gebaeude_STAMM." + k))
                              .Concat(new[]
                              {
                                  "Tab_ErgebnisEnergiebedarf.Kuehlkurve_Vorlauf_Mittel_C",
                                  "Tab_ErgebnisEnergiebedarf.Kuehlkurve_Absenkung_Kh",
                                  "Tab_ErgebnisEnergiebedarf.Kuehlkurve_Vorlaufgrenze_Stunden",
                              }).ToArray(),
                         KuehlkurveSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
            // Festlegung 15: keine Zonenspalte.
            Assert.DoesNotContain(KuehlkurveSchema.SPALTEN, x => x.Tabelle == "Tab_Zone");
        }

        [Fact]
        public void Der_elfte_Sichtneubau_haengt_die_Kuehlkurve_hinter_den_Raumeinfluss()
        {
            Assert.Equal(110, GebaeudeSchema.SICHT_KUEHLKURVE.Length);
            Assert.Equal(GebaeudeSchema.SICHT_AK3, GebaeudeSchema.SICHT_KUEHLKURVE.Take(105));
            Assert.Equal(GebaeudeSchema.KUEHLKURVE_SPALTEN, GebaeudeSchema.SICHT_KUEHLKURVE.Skip(105));
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLKURVE, GebaeudeSchema.SICHT_AKTUELL);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_KUEHLKURVE, GebaeudeSchema.SQL_VIEW_AKTUELL);
            foreach (string k in GebaeudeSchema.KUEHLKURVE_SPALTEN)
                Assert.Contains("Tab_Gebaeude." + k, GebaeudeSchema.SQL_VIEW_AKTUELL, StringComparison.Ordinal);
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Schalter 0/1, Bereiche, Wege, Kennzahlen.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_die_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach ((string _, string spalte, string typ) in KuehlkurveSchema.SPALTEN.GroupBy(x => x.Spalte).Select(g => g.First()))
                Ausfuehren(c, KuehlkurveSchema.Anlegen(("T", spalte, typ)));
            Pruefe(c, "Kuehlkurve_Aktiv", new[] { "NULL", "0", "1" }, new[] { "2", "-1", "'ja'" });
            Pruefe(c, "Kuehlkurve_Fusspunkt", new[] { "NULL", "4", "16.5", "22" }, new[] { "3.9", "22.1", "'abc'" });
            Pruefe(c, "Kuehlkurve_Raumeinfluss", new[] { "NULL", "0", "1", "10" }, new[] { "-0.1", "10.01" });
            Pruefe(c, "Kuehlkurve_Auslegung_Weg", new[] { "NULL", "'stunde'", "'tagesmittel'", "'eingabe'" },
                   new[] { "'Stunde'", "''", "'heute'", "1" });
            Pruefe(c, "Kuehlkurve_Auslegung_Aussen", new[] { "NULL", "0", "32", "60" }, new[] { "-0.5", "60.5" });
            Pruefe(c, "Kuehlkurve_Vorlauf_Mittel_C", new[] { "NULL", "16.25", "-3" }, new[] { "'x'" });
            Pruefe(c, "Kuehlkurve_Absenkung_Kh", new[] { "NULL", "0", "1234.5" }, new[] { "-1" });
            Pruefe(c, "Kuehlkurve_Vorlaufgrenze_Stunden", new[] { "NULL", "0", "8760" }, new[] { "-1", "8761" });
            // Die Wege der Prüfklausel sind die Persistenzwerte aus DbWerte.
            Assert.Equal("'stunde', 'tagesmittel', 'eingabe'", KuehlkurveSchema.WERTE_AUSLEGUNG_WEG);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank, die Rundreise und die Kopierwege
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KuehlkurveSchema.SCHRITT);
            Assert.True(KuehlkurveSchema.Vollstaendig());
            Assert.True(KuehlkurveSchema.ErgebnisspaltenVorhanden());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            // Kein DML: jede Zeile steht leer - fester Vorlauf (Festlegung 1), Kennzahlen „nicht erhoben".
            foreach (string t in new[] { "Tab_Gebaeude", "Tab_Gebaeude_STAMM" })
                foreach (string k in GebaeudeSchema.KUEHLKURVE_SPALTEN)
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + t + " WHERE " + k + " IS NOT NULL"));
            foreach (string k in KuehlkurveSchema.SPALTEN_ERGEBNIS)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE " + k + " IS NOT NULL"));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Kuehlkurve_Fusspunkt = 30 WHERE ID = " + GEBAEUDE));
        }

        /// <summary>Rundreise: aus dem Stand davor legt der Schritt alle Spalten und die Sicht an; zweimal = nichts.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach ((string tabelle, string spalte, string _) in KuehlkurveSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE " + tabelle + " DROP COLUMN " + spalte);
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_AK3);
            long zeilen = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude");
            Assert.False(KuehlkurveSchema.Vollstaendig());
            Assert.True(Ak3Schema.SichtSteht());

            var bericht = new List<string>();
            Assert.Equal(13, KuehlkurveSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("110 Spalten", StringComparison.Ordinal));
            Assert.True(KuehlkurveSchema.Vollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            Assert.Equal(0, KuehlkurveSchema.Ausfuehren(null));
        }

        /// <summary>Der Leser der Sicht liefert die Werte, NULL bleibt null bzw. aus; die Projektkopie liest sie ebenso.</summary>
        [Fact]
        public void Der_Leser_der_Sicht_liefert_die_Kuehlkurve()
        {
            if (!_db.Vorhanden) return;
            ProjektGebaeudeModel leer = Projektgebaeude();
            Assert.False(leer.Kuehlkurve_Aktiv);
            Assert.Null(leer.Kuehlkurve_Fusspunkt);
            Assert.Null(leer.Kuehlkurve_Raumeinfluss);
            Assert.Null(leer.Kuehlkurve_Auslegung_Weg);
            Assert.Null(leer.Kuehlkurve_Auslegung_Aussen);
            Setzen();
            ProjektGebaeudeModel g = Projektgebaeude();
            Assert.True(g.Kuehlkurve_Aktiv);
            Assert.Equal(19.0, g.Kuehlkurve_Fusspunkt);
            Assert.Equal(1.5, g.Kuehlkurve_Raumeinfluss);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE, g.Kuehlkurve_Auslegung_Weg);
            Assert.Equal(31.0, g.Kuehlkurve_Auslegung_Aussen);
            GebaeudeModel k = GebaeudeStammCtrl.LiesProjektkopie(GEBAEUDE);
            Assert.True(k.Kuehlkurve_Aktiv);
            Assert.Equal(19.0, k.Kuehlkurve_Fusspunkt);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE, k.Kuehlkurve_Auslegung_Weg);
        }

        /// <summary>Das Projektduplikat (derselbe Weg wie „Variante anlegen") trägt die Werte.</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Kuehlkurve()
        {
            if (!_db.Vorhanden) return;
            Setzen();
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " KK");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeGesetzt(neu);
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die Werte über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Kuehlkurve()
        {
            if (!_db.Vorhanden) return;
            Setzen();
            string ordner = Path.Combine(Path.GetTempPath(), "epos-kk-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer KK", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeGesetzt(neu);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { }
            }
        }

        /// <summary>
        /// Katalog ↔ Projekt: Speichern im Katalog und Lesen, Übernahme in ein Projekt (Projektkopie) und zurück in den
        /// Katalog (Kopfspalten) — die Werte gehen NULL-erhaltend hinüber.
        /// </summary>
        [Fact]
        public void Katalog_und_Projektkopie_tragen_die_Kuehlkurve()
        {
            if (!_db.Vorhanden) return;
            foreach (string k in GebaeudeSchema.KUEHLKURVE_SPALTEN)
                Assert.Contains("[" + k + "]", GebaeudeStammCtrl.KOPFSPALTEN, StringComparison.Ordinal);
            var ctrl = new GebaeudeStammCtrl();
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            GebaeudeModel m = ctrl.Lies(name);
            Assert.NotNull(m);
            Assert.False(m.Kuehlkurve_Aktiv);
            Assert.Null(m.Kuehlkurve_Fusspunkt);

            // Katalog speichern und lesen (Insert): die Werte und NULL bleiben, was sie sind.
            m.Gebaeudename = "KK Kuehlkurve Probe";
            m.Kuehlkurve_Aktiv = true;
            m.Kuehlkurve_Fusspunkt = 19.0;
            m.Kuehlkurve_Raumeinfluss = null;
            m.Kuehlkurve_Auslegung_Weg = DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE;
            m.Kuehlkurve_Auslegung_Aussen = null;
            Assert.True(ctrl.Insert(m));
            GebaeudeModel gelesen = ctrl.Lies("KK Kuehlkurve Probe");
            Assert.True(gelesen.Kuehlkurve_Aktiv);
            Assert.Equal(19.0, gelesen.Kuehlkurve_Fusspunkt);
            Assert.Null(gelesen.Kuehlkurve_Raumeinfluss);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE, gelesen.Kuehlkurve_Auslegung_Weg);
            Assert.Null(gelesen.Kuehlkurve_Auslegung_Aussen);

            // Katalog → Projekt (Projektkopie).
            int zuordnung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?", new DbParam("?", GEBAEUDE)), CultureInfo.InvariantCulture);
            int kopie = ctrl.CopyFromStamm("KK Kuehlkurve Probe", PROJEKT, zuordnung);
            Assert.True(kopie > 0, "Kopie in das Projekt fehlgeschlagen.");
            GebaeudeModel p = GebaeudeStammCtrl.LiesProjektkopie(kopie);
            Assert.True(p.Kuehlkurve_Aktiv);
            Assert.Equal(19.0, p.Kuehlkurve_Fusspunkt);
            Assert.Null(p.Kuehlkurve_Raumeinfluss);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE, p.Kuehlkurve_Auslegung_Weg);
            object roh = DataRepository.ExecuteScalar("SELECT Kuehlkurve_Raumeinfluss FROM Tab_Gebaeude WHERE ID = ?",
                                                      new DbParam("?", kopie));
            Assert.True(roh == null || roh is DBNull);

            // Überschreiben im Katalog (Overwrite) mit NULL: NULL bleibt NULL.
            GebaeudeModel n = ctrl.Lies("KK Kuehlkurve Probe");
            n.Kuehlkurve_Fusspunkt = null;
            n.Kuehlkurve_Auslegung_Weg = null;
            Assert.True(ctrl.Overwrite(n));
            GebaeudeModel o = ctrl.Lies("KK Kuehlkurve Probe");
            Assert.Null(o.Kuehlkurve_Fusspunkt);
            Assert.Null(o.Kuehlkurve_Auslegung_Weg);
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static void Setzen()
            => Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlkurve_Aktiv = 1, Kuehlkurve_Fusspunkt = ?, Kuehlkurve_Raumeinfluss = ?, " +
                "Kuehlkurve_Auslegung_Weg = ?, Kuehlkurve_Auslegung_Aussen = ? WHERE ID = ?",
                new DbParam("@f", 19.0), new DbParam("@r", 1.5), new DbParam("@w", DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE),
                new DbParam("@a", 31.0), new DbParam("@id", GEBAEUDE)));

        private static void PruefeGesetzt(int idProjekt)
        {
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT Kuehlkurve_Aktiv, Kuehlkurve_Fusspunkt, Kuehlkurve_Raumeinfluss, Kuehlkurve_Auslegung_Weg, " +
                "Kuehlkurve_Auslegung_Aussen FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("?", idProjekt));
            Assert.Single(dt.Rows);
            System.Data.DataRow r = dt.Rows[0];
            Assert.Equal(1L, Convert.ToInt64(r[0], CultureInfo.InvariantCulture));
            Assert.Equal(19.0, Convert.ToDouble(r[1], CultureInfo.InvariantCulture));
            Assert.Equal(1.5, Convert.ToDouble(r[2], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE, Convert.ToString(r[3], CultureInfo.InvariantCulture));
            Assert.Equal(31.0, Convert.ToDouble(r[4], CultureInfo.InvariantCulture));
        }

        private static ProjektGebaeudeModel Projektgebaeude()
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            return ctrl.items.Single(x => x.ID_Gebaeude == GEBAEUDE);
        }

        private static void Pruefe(SqliteConnection c, string spalte, string[] gut, string[] schlecht)
        {
            foreach (string w in gut)
                Assert.False(Wirft(c, "UPDATE T SET " + spalte + " = " + w), spalte + " = " + w);
            foreach (string w in schlecht)
                Assert.True(Wirft(c, "UPDATE T SET " + spalte + " = " + w), spalte + " = " + w);
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
