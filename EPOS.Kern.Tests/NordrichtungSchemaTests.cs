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
    /// <b>Schritt 199 — Herkunft des Nordwinkels</b> (<see cref="NordrichtungSchema"/>, Abstimmungspapier G5, N6): Nummer, Ziel
    /// und Register; die Spalte samt Prüfklausel; der Stand davor, das Nachfüllen und die Wiederholbarkeit; das Schreiben über
    /// den Importweg (Annahme ohne Eingabe, Eingabe, Dateiwert) und über „Ausrichtung ändern“; die Werkzeug-Wache; die Kopierwege
    /// Projekt duplizieren und Projektpaket.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class NordrichtungSchemaTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const int GEBAEUDE = 10614;              // Projekt 1007, ohne Zonen und ohne Quelle in der Testdatenbank

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
            GebaeudeZonenanschluss.ProbeVerwerfen();
        }

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definition (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_199_das_Ziel_steht_darauf_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(Ak3Schema.SCHRITT + 1, NordrichtungSchema.SCHRITT);
            Assert.Equal(199, NordrichtungSchema.SCHRITT);
            Assert.Equal(NordrichtungSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == NordrichtungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal("Tab_Importquelle", NordrichtungSchema.TAB_QUELLE);
            Assert.Equal("Nordwinkel_Herkunft", NordrichtungSchema.SPALTE);
            Assert.Equal(NordrichtungSchema.SPALTE, GebaeudeImportCtrl.SPALTE_NORDWINKEL_HERKUNFT);
            // EINE Quelle der Wertliste: je Wert der Aufzählung einer, in ihrer Reihenfolge.
            Assert.Equal(Enum.GetValues(typeof(Nordwinkelherkunft)).Cast<Nordwinkelherkunft>().Select(NordwinkelherkunftWerte.Wert),
                         NordrichtungSchema.WERTE);
            Assert.Equal(new[] { "ANNAHME", "DATEI", "EINGABE" }, NordrichtungSchema.WERTE.ToArray());
            foreach (Nordwinkelherkunft h in Enum.GetValues(typeof(Nordwinkelherkunft)))
            {
                Assert.Equal(h, NordwinkelherkunftWerte.Aus(NordwinkelherkunftWerte.Wert(h)));
                Assert.Equal(NordwinkelherkunftWerte.Wert(h), GebaeudeImportCtrl.HerkunftWert(h));
            }
            Assert.Null(NordwinkelherkunftWerte.Aus(null));
            Assert.Null(NordwinkelherkunftWerte.Aus("datei"));
            // Ohne Spaltenwert: Nordwinkel vorhanden → Datei, NULL → Annahme - dieselbe Regel wie das Nachfüllen.
            Assert.Equal(Nordwinkelherkunft.Datei, GebaeudeImportCtrl.Nordherkunft(12.0, null));
            Assert.Equal(Nordwinkelherkunft.Annahme, GebaeudeImportCtrl.Nordherkunft(null, null));
            Assert.Equal(Nordwinkelherkunft.Eingabe, GebaeudeImportCtrl.Nordherkunft(null, "EINGABE"));
        }

        /// <summary>Die Prüfklausel und das Nachfüllen an einer STRICT-Tabelle.</summary>
        [Fact]
        public void Die_Pruefklausel_haelt_die_Spalte_und_das_Nachfuellen_folgt_dem_Nordwinkel()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Importquelle\" (ID INTEGER PRIMARY KEY, \"Nordwinkel_Grad\" REAL) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Importquelle (ID, Nordwinkel_Grad) VALUES (1, 30.0), (2, NULL), (3, 0.0)");
            Ausfuehren(c, NordrichtungSchema.Anlegen());
            Assert.Equal(3L, Skalar(c, "SELECT COUNT(*) FROM Tab_Importquelle WHERE Nordwinkel_Herkunft IS NULL"));
            Ausfuehren(c, "UPDATE Tab_Importquelle SET Nordwinkel_Herkunft = 'EINGABE' WHERE ID = 3");
            Ausfuehren(c, NordrichtungSchema.Nachfuellen());
            Assert.Equal("DATEI", Text(c, "SELECT Nordwinkel_Herkunft FROM Tab_Importquelle WHERE ID = 1"));
            Assert.Equal("ANNAHME", Text(c, "SELECT Nordwinkel_Herkunft FROM Tab_Importquelle WHERE ID = 2"));
            Assert.Equal("EINGABE", Text(c, "SELECT Nordwinkel_Herkunft FROM Tab_Importquelle WHERE ID = 3"));   // gesetzt bleibt
            foreach (string w in NordrichtungSchema.WERTE.Select(x => "'" + x + "'").Append("NULL"))
                Assert.False(Wirft(c, "UPDATE Tab_Importquelle SET Nordwinkel_Herkunft = " + w), w);
            foreach (string w in new[] { "'datei'", "'IFC'", "''", "1" })
                Assert.True(Wirft(c, "UPDATE Tab_Importquelle SET Nordwinkel_Herkunft = " + w), w);
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Stand davor, Nachfüllen, Wiederholbarkeit
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= NordrichtungSchema.SCHRITT);
            Assert.True(NordrichtungSchema.Vollstaendig());
            Assert.True(GebaeudeImportCtrl.NordherkunftVorhanden());
            List<string> spalten = DataRepository.SpaltenVonTabelle(NordrichtungSchema.TAB_QUELLE);
            Assert.Equal(NordrichtungSchema.SPALTE, spalten[spalten.Count - 1]);
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", NordrichtungSchema.TAB_QUELLE)),
                                          CultureInfo.InvariantCulture);
            Assert.Contains("\"Nordwinkel_Herkunft\" TEXT CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("IN ('ANNAHME','DATEI','EINGABE')", ddl, StringComparison.Ordinal);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Importquelle WHERE Nordwinkel_Herkunft IS NULL"));
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_fuellt_nach_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            int andereGebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(\"ID\") FROM \"Tab_Gebaeude\" WHERE \"ID\" <> ? AND \"ID\" NOT IN (SELECT \"ID_Gebaeude\" FROM \"Tab_Importquelle\")",
                new DbParam("@g", GEBAEUDE)), CultureInfo.InvariantCulture);
            int vorher = DataRepository.SpaltenVonTabelle(NordrichtungSchema.TAB_QUELLE).Count;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Importquelle\" DROP COLUMN \"Nordwinkel_Herkunft\"");
            GebaeudeZonenanschluss.ProbeVerwerfen();
            Assert.False(NordrichtungSchema.Vollstaendig());
            Assert.False(GebaeudeImportCtrl.NordherkunftVorhanden());

            // Vor dem Schritt schreibt der Importweg ohne die Spalte - nichts geworfen; gelesen gilt Wert → Datei, NULL → Annahme.
            var ctrl = new GebaeudeImportCtrl();
            int mit = QuelleSchreiben(GEBAEUDE, 30.0, Nordwinkelherkunft.Eingabe);
            int ohne = QuelleSchreiben(andereGebaeude, null, Nordwinkelherkunft.Annahme);
            Assert.Equal(Nordwinkelherkunft.Datei, ctrl.LesenAusrichtung(GEBAEUDE).Herkunft);
            Assert.Equal(Nordwinkelherkunft.Annahme, ctrl.LesenAusrichtung(andereGebaeude).Herkunft);

            var bericht = new List<string>();
            Assert.Equal(1, NordrichtungSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("angelegt", StringComparison.Ordinal));
            Assert.True(NordrichtungSchema.Vollstaendig());
            Assert.True(GebaeudeImportCtrl.NordherkunftVorhanden());
            Assert.Equal(vorher, DataRepository.SpaltenVonTabelle(NordrichtungSchema.TAB_QUELLE).Count);
            Assert.Equal("DATEI", Gespeichert(mit));
            Assert.Equal("ANNAHME", Gespeichert(ohne));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Importquelle WHERE Nordwinkel_Herkunft IS NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            // Wiederholt: keine Spalte mehr, eine gesetzte Herkunft bleibt, eine geleerte wird nachgefüllt.
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Importquelle\" SET \"Nordwinkel_Herkunft\" = 'EINGABE' WHERE \"ID\" = ?", new DbParam("@q", mit));
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Importquelle\" SET \"Nordwinkel_Herkunft\" = NULL WHERE \"ID\" = ?", new DbParam("@q", ohne));
            Assert.Equal(0, NordrichtungSchema.Ausfuehren(null));
            Assert.Equal("EINGABE", Gespeichert(mit));
            Assert.Equal("ANNAHME", Gespeichert(ohne));
            Assert.Equal(Nordwinkelherkunft.Eingabe, ctrl.LesenAusrichtung(GEBAEUDE).Herkunft);
        }

        [Fact]
        public void Ohne_den_Nordwinkel_der_Quelle_wirft_der_Schritt_benannt()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Importquelle\" DROP COLUMN \"Nordwinkel_Herkunft\"");
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Importquelle\" DROP COLUMN \"Nordwinkel_Grad\"");
            GebaeudeZonenanschluss.ProbeVerwerfen();
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => NordrichtungSchema.Ausfuehren(null));
            Assert.Contains("Nordwinkel_Grad", ex.Message, StringComparison.Ordinal);
            Assert.False(NordrichtungSchema.Vollstaendig());
        }

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt aus
        /// derselben Quelle NACH 198; die Repo-Datei trägt die Spalte (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int w = werkzeug.IndexOf("NordrichtungSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(w > 0 && w > werkzeug.IndexOf("Ak3Schema.Ausfuehren(", StringComparison.Ordinal),
                        "Der Schritt steht im Werkzeug nicht hinter 198.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_NORDRICHTUNG = NordrichtungSchema.SCHRITT", migration, StringComparison.Ordinal);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_AK3", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_NORDRICHTUNG", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ort > ortVorher, "Der Schritt steht nicht hinter 198.");
            Assert.Contains("NordrichtungSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int v = vorrichtung.IndexOf("NordrichtungSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(v > 0 && v > vorrichtung.IndexOf("Ak3Schema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Der Schritt steht in der Testkopie nicht hinter 198.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Skalar(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= NordrichtungSchema.SCHRITT);
            Assert.Equal(1L, Skalar(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Importquelle') WHERE name = 'Nordwinkel_Herkunft'"));
            // Kein Referenzprojekt hat eine Importquelle - der Schritt ist ergebnisneutral.
            Assert.Equal(0L, Skalar(verbindung,
                "SELECT COUNT(*) FROM Tab_Importquelle q INNER JOIN Tab_Gebaeude g ON g.ID = q.ID_Gebaeude " +
                "WHERE g.ID_Projekt IN (1030, 1007, 1017, 1045, 1046, 1047, 1049, 1051)"));
        }

        // =============================================================================
        //  Teil 3 - Schreiben und Lesen über Import und Ausrichtung
        // =============================================================================

        /// <summary>Der Importweg schreibt die Herkunft des Ablaufs: Dateiwert, Annahme ohne Eingabe, Eingabe.</summary>
        [Theory]
        [InlineData("ifc4_g5_oeffnungen.ifc", false, "DATEI")]
        [InlineData("ifc4_g5_oeffnungen.ifc", true, "EINGABE")]
        [InlineData("gbxml_norddrehung.xml", false, "ANNAHME")]
        [InlineData("gbxml_norddrehung.xml", true, "EINGABE")]
        public void Der_Import_schreibt_und_liest_die_Herkunft(string probe, bool mitEingabe, string erwartet)
        {
            if (!_db.Vorhanden) return;
            string datei = Path.Combine(IfcProbenTests.Ordner(), probe);
            var a = new GebaeudeImportAblauf { NordwinkelVorgabeGrad = mitEingabe ? 135.0 : null };
            using (var s = new MemoryStream(File.ReadAllBytes(datei)))
                a.Lesen(s, probe, GebaeudeImportProfil.FuerDatei(probe));
            Assert.True(a.Quelle != null, probe + ": " + string.Join(" | ", a.Meldungen.Select(m => m.ToString())));
            Assert.Equal(erwartet, NordwinkelherkunftWerte.Wert(a.Quelle.NordwinkelHerkunft));

            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE, a.Quelle, new List<GebaeudeQuellzuordnung>
            {
                new GebaeudeQuellzuordnung("IfcBuilding", "bldg-1", ImportZiel.Gebaeude),
            });
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(erwartet, Gespeichert(e.IdImportquelle));
            var ctrl = new GebaeudeImportCtrl();
            Assert.Equal(a.Quelle.NordwinkelHerkunft, ctrl.LesenAusrichtung(GEBAEUDE).Herkunft);
            Assert.Equal(a.Quelle.NordwinkelHerkunft, Assert.Single(ctrl.LesenQuellen(GEBAEUDE)).NordwinkelHerkunft);
            if (mitEingabe) Assert.Equal(135.0, ctrl.LesenAusrichtung(GEBAEUDE).NordwinkelGrad.Value, 9);
        }

        [Fact]
        public void Ausrichtung_aendern_schreibt_die_Eingabe()
        {
            if (!_db.Vorhanden) return;
            int quelle = QuelleSchreiben(GEBAEUDE, 30.0, Nordwinkelherkunft.Datei);
            Assert.Equal("DATEI", Gespeichert(quelle));
            var ctrl = new GebaeudeImportCtrl();
            Assert.True(ctrl.AusrichtungAendern(GEBAEUDE, 90.0).Ok);
            Assert.Equal("EINGABE", Gespeichert(quelle));
            GebaeudeImportCtrl.Ausrichtung a = ctrl.LesenAusrichtung(GEBAEUDE);
            Assert.Equal(Nordwinkelherkunft.Eingabe, a.Herkunft);
            Assert.Equal(270.0, a.NordwinkelGrad.Value, 9);
            Assert.Equal(90.0, a.PlanoberseiteGrad, 9);
        }

        // =============================================================================
        //  Teil 4 - Kopierwege
        // =============================================================================

        [Fact]
        public void Das_Projektduplikat_traegt_die_Herkunft()
        {
            if (!_db.Vorhanden) return;
            QuelleSchreiben(GEBAEUDE, 30.0, Nordwinkelherkunft.Eingabe);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(Projektname(), Projektname() + " G5-N");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(new[] { "EINGABE" }, WerteDesProjekts(neu));
        }

        [Fact]
        public void Der_Projekttransfer_traegt_die_Herkunft()
        {
            if (!_db.Vorhanden) return;
            QuelleSchreiben(GEBAEUDE, 30.0, Nordwinkelherkunft.Eingabe);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-g5n-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "n.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Projektname(), paket));
                int neu = io.Importieren(paket, "Transfer G5-N", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(new[] { "EINGABE" }, WerteDesProjekts(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Schreibt eine Quelle mit Nordwinkel und Herkunft an das Gebäude; liefert ihre ID.</summary>
        private static int QuelleSchreiben(int idGebaeude, double? nordwinkel, Nordwinkelherkunft herkunft)
        {
            GebaeudeQuelle q = new GebaeudeQuelle("IFC", "haus.ifc", new string('c', 64), 4711, "IFC4", "2026-10-07T10:00:00+02:00", "1.0", "X4", 0)
            {
                NordwinkelGrad = nordwinkel,
                NordwinkelHerkunft = herkunft,
            };
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(idGebaeude, q, new List<GebaeudeQuellzuordnung>
            {
                new GebaeudeQuellzuordnung("IfcBuilding", "bldg-1", ImportZiel.Gebaeude),
            });
            Assert.True(e.Ok, e.Meldung);
            return e.IdImportquelle;
        }

        private static string Gespeichert(int idQuelle)
        {
            object o = DataRepository.ExecuteScalar("SELECT \"Nordwinkel_Herkunft\" FROM \"Tab_Importquelle\" WHERE \"ID\" = ?", new DbParam("@q", idQuelle));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static List<string> WerteDesProjekts(int idProjekt)
            => DataRepository.GetDataTable(
                    "SELECT q.\"Nordwinkel_Herkunft\" FROM \"Tab_Importquelle\" q INNER JOIN \"Tab_Gebaeude\" g ON g.\"ID\" = q.\"ID_Gebaeude\" " +
                    "WHERE g.\"ID_Projekt\" = ?", new DbParam("@p", idProjekt))
                .Rows.Cast<System.Data.DataRow>()
                .Select(r => r[0] == DBNull.Value ? null : Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToList();

        private static string Projektname()
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT)),
                                CultureInfo.InvariantCulture);

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            k.ExecuteNonQuery();
        }

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToInt64(k.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string Text(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToString(k.ExecuteScalar(), CultureInfo.InvariantCulture);
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
