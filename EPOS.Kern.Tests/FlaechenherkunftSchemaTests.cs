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
    /// <b>Schritt 197 — Herkunft der Bauteilfläche</b> (<see cref="FlaechenherkunftSchema"/>, Abstimmung G5, A4): Nummer,
    /// Ziel und Register; die Spalte samt Prüfklausel; der Stand davor und die Wiederholbarkeit; das Schreiben über den
    /// Importweg (<see cref="GebaeudeZonenCtrl.VorschlagSchreiben"/>) und das Lesen (<see cref="BauteilModel.Flaechenherkunft"/>);
    /// die Pflege (Fläche von Hand geändert oder Zeile von Hand angelegt → NULL); die Kopierwege Projekt duplizieren und
    /// Projektpaket.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class FlaechenherkunftSchemaTests : IDisposable
    {
        private const int PROJEKT = 1045;

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definition (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_197_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(StandardlastprofilPvSchema.SCHRITT + 1, FlaechenherkunftSchema.SCHRITT);
            Assert.Equal(197, FlaechenherkunftSchema.SCHRITT);
            Assert.Equal(FlaechenherkunftSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == FlaechenherkunftSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal("Tab_Bauteil", FlaechenherkunftSchema.TAB_BAUTEIL);
            Assert.Equal("Flaechenherkunft", FlaechenherkunftSchema.SPALTE);
            // EINE Quelle der Wertliste: die Werte von FlaechenherkunftWerte, je Aufzählungswert einer.
            Assert.Equal(new[]
                {
                    FlaechenherkunftWerte.MENGENSATZ, FlaechenherkunftWerte.RAUMGRENZE,
                    FlaechenherkunftWerte.KOERPER, FlaechenherkunftWerte.SCHEMATISCH,
                },
                FlaechenherkunftSchema.WERTE.ToArray());
            Assert.Equal(new[] { "MENGENSATZ", "RAUMGRENZE", "KOERPER", "SCHEMATISCH" }, FlaechenherkunftSchema.WERTE.ToArray());
            Assert.Null(FlaechenherkunftWerte.Wert(null));
        }

        /// <summary>Die Prüfklausel an einer STRICT-Tabelle.</summary>
        [Fact]
        public void Die_Pruefklausel_haelt_die_Spalte()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Bauteil\" (ID INTEGER PRIMARY KEY, Flaeche REAL NOT NULL) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Bauteil (ID, Flaeche) VALUES (1, 10.0)");
            Ausfuehren(c, FlaechenherkunftSchema.Anlegen());
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_Bauteil WHERE Flaechenherkunft IS NOT NULL"));
            foreach (string w in FlaechenherkunftSchema.WERTE.Select(x => "'" + x + "'").Append("NULL"))
                Assert.False(Wirft(c, "UPDATE Tab_Bauteil SET Flaechenherkunft = " + w), w);
            foreach (string w in new[] { "'mengensatz'", "'IFC'", "''", "1" })
                Assert.True(Wirft(c, "UPDATE Tab_Bauteil SET Flaechenherkunft = " + w), w);
        }

        [Theory]
        [InlineData(0, "MENGENSATZ")]
        [InlineData(1, "RAUMGRENZE")]
        [InlineData(2, "KOERPER")]
        [InlineData(3, "SCHEMATISCH")]
        public void Die_Bauteilzeile_traegt_den_gespeicherten_Wert_in_ihr_Bauteil(int h, string wert)
        {
            var z = new GebaeudeBauteilzeile(new BauteilModel(), null, null, null, null) { Flaechenherkunft = (Flaechenherkunft)h };
            Assert.Equal(wert, z.Bauteil.Flaechenherkunft);
            z.Flaechenherkunft = null;
            Assert.Null(z.Bauteil.Flaechenherkunft);
        }

        [Fact]
        public void Die_Pflege_haelt_die_Herkunft_nur_bei_unveraenderter_Flaeche()
        {
            Assert.Equal("KOERPER", GebaeudeZonenCtrl.FlaechenherkunftNachPflege("KOERPER", 12.5, 12.5));
            Assert.Equal("KOERPER", GebaeudeZonenCtrl.FlaechenherkunftNachPflege("KOERPER", 12.5, 12.5 + 1e-12));
            Assert.Null(GebaeudeZonenCtrl.FlaechenherkunftNachPflege("KOERPER", 12.5, 12.6));
            Assert.Null(GebaeudeZonenCtrl.FlaechenherkunftNachPflege(null, 12.5, 12.5));
            Assert.Null(GebaeudeZonenCtrl.FlaechenherkunftNachPflege("MENGENSATZ", null, 12.5));
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Stand davor, Wiederholbarkeit
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_die_Spalte_ist_leer()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= FlaechenherkunftSchema.SCHRITT);
            Assert.True(FlaechenherkunftSchema.Vollstaendig());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil WHERE Flaechenherkunft IS NOT NULL"));
            List<string> spalten = DataRepository.SpaltenVonTabelle(FlaechenherkunftSchema.TAB_BAUTEIL);
            Assert.Equal(FlaechenherkunftSchema.SPALTE, spalten[spalten.Count - 1]);
            string ddl = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?",
                                                                       new DbParam("@n", FlaechenherkunftSchema.TAB_BAUTEIL)),
                                          CultureInfo.InvariantCulture);
            Assert.Contains("\"Flaechenherkunft\" TEXT CHECK", ddl, StringComparison.Ordinal);
            Assert.Contains("IN ('MENGENSATZ','RAUMGRENZE','KOERPER','SCHEMATISCH')", ddl, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            int vorher = DataRepository.SpaltenVonTabelle(FlaechenherkunftSchema.TAB_BAUTEIL).Count;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Bauteil\" DROP COLUMN \"Flaechenherkunft\"");
            GebaeudeZonenanschluss.ProbeVerwerfen();
            Assert.False(FlaechenherkunftSchema.Vollstaendig());
            Assert.False(GebaeudeZonenanschluss.FlaechenherkunftVorhanden());

            // Vor dem Schritt schreibt der Importweg ohne die Spalte - benannt nichts verloren, nichts geworfen.
            ProjektGebaeudeModel g = GebaeudeBauteilvorschlagDatenbankTests.Zeile(PROJEKT);
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag("ifc4_ohne_mengen.ifc");
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);
            Assert.All(new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).SelectMany(z => z.Bauteile),
                       b => Assert.Null(b.Flaechenherkunft));

            var bericht = new List<string>();
            Assert.Equal(1, FlaechenherkunftSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(FlaechenherkunftSchema.Vollstaendig());
            Assert.True(GebaeudeZonenanschluss.FlaechenherkunftVorhanden());
            Assert.Equal(vorher, DataRepository.SpaltenVonTabelle(FlaechenherkunftSchema.TAB_BAUTEIL).Count);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteil WHERE Flaechenherkunft IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0, FlaechenherkunftSchema.Ausfuehren(null));
        }

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den
        /// Schritt aus derselben Quelle NACH 196; die Repo-Datei trägt die Spalte (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int w = werkzeug.IndexOf("FlaechenherkunftSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(w > 0 && w > werkzeug.IndexOf("StandardlastprofilPvSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Der Schritt steht im Werkzeug nicht hinter 196.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_FLAECHENHERKUNFT = FlaechenherkunftSchema.SCHRITT", migration, StringComparison.Ordinal);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_STANDARDLASTPROFIL_PV", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_FLAECHENHERKUNFT", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ort > ortVorher, "Der Schritt steht nicht hinter 196.");
            Assert.Contains("FlaechenherkunftSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int v = vorrichtung.IndexOf("FlaechenherkunftSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(v > 0 && v > vorrichtung.IndexOf("StandardlastprofilPvSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Der Schritt steht in der Testkopie nicht hinter 196.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Skalar(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= FlaechenherkunftSchema.SCHRITT);
            Assert.Equal(1L, Skalar(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Bauteil') WHERE name = 'Flaechenherkunft'"));
            Assert.Equal(0L, Skalar(verbindung, "SELECT COUNT(*) FROM Tab_Bauteil WHERE Flaechenherkunft IS NOT NULL"));
        }

        // =============================================================================
        //  Teil 3 - Importweg, Lesen, Pflege
        // =============================================================================

        /// <summary>Ohne Mengensätze stammen die Wandflächen aus dem Bauteilkörper — gespeichert als KOERPER.</summary>
        [Fact]
        public void Import_ohne_Mengen_schreibt_KOERPER()
        {
            if (!_db.Vorhanden) return;
            Dictionary<int, string> werte = Importieren("ifc4_ohne_mengen.ifc", out GebaeudeZonenCtrl.Vorschlagsergebnis e);
            Assert.Contains(FlaechenherkunftWerte.KOERPER, werte.Values);
            Assert.DoesNotContain(FlaechenherkunftWerte.MENGENSATZ, werte.Values);
            Assert.All(e.Zonen.SelectMany(z => z.Bauteile).Where(b => b.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND),
                       b => Assert.Equal(FlaechenherkunftWerte.KOERPER, werte[b.ID]));
        }

        /// <summary>Mit Mengensätzen (und Raumgrenzen) bleibt der Mengensatz Quelle — kein KOERPER.</summary>
        [Fact]
        public void Import_mit_Mengen_schreibt_MENGENSATZ_oder_RAUMGRENZE()
        {
            if (!_db.Vorhanden) return;
            Dictionary<int, string> werte = Importieren("ifc4_haus.ifc", out _);
            Assert.Contains(FlaechenherkunftWerte.MENGENSATZ, werte.Values);
            Assert.DoesNotContain(FlaechenherkunftWerte.KOERPER, werte.Values);
            Assert.All(werte.Values.Where(w => w != null), w => Assert.Contains(w, new[]
            {
                FlaechenherkunftWerte.MENGENSATZ, FlaechenherkunftWerte.RAUMGRENZE, FlaechenherkunftWerte.SCHEMATISCH,
            }));
        }

        /// <summary>Eine von Hand geänderte Fläche verliert ihre Herkunft, eine von Hand angelegte Zeile hat keine; der Rest bleibt.</summary>
        [Fact]
        public void Handaenderung_der_Flaeche_und_Handanlage_setzen_NULL()
        {
            if (!_db.Vorhanden) return;
            Dictionary<int, string> vorher = Importieren("ifc4_ohne_mengen.ifc", out GebaeudeZonenCtrl.Vorschlagsergebnis e);
            int idGebaeude = e.Zonen[0].ID_Gebaeude;
            var ctrl = new GebaeudeZonenCtrl();
            List<ZoneModel> zonen = ctrl.LesenJeGebaeude(idGebaeude);
            List<BauteilModel> mitHerkunft = zonen.SelectMany(z => z.Bauteile).Where(b => b.Flaechenherkunft != null).ToList();
            Assert.True(mitHerkunft.Count >= 2);
            BauteilModel geaendert = mitHerkunft[0];
            BauteilModel unveraendert = mitHerkunft[1];
            geaendert.Flaeche += 1.0;
            ZoneModel zone = zonen.First(z => z.Bauteile.Contains(unveraendert));
            BauteilModel kopie = unveraendert.Kopie();
            kopie.ID = -1;
            kopie.Bezeichner = "Handanlage";
            zone.Bauteile.Add(kopie);

            Assert.True(ctrl.SpeichernJeGebaeude(idGebaeude, zonen).Ok);
            Assert.Null(kopie.Flaechenherkunft);
            Dictionary<int, string> nachher = Gespeichert(idGebaeude);
            Assert.Null(nachher[geaendert.ID]);
            Assert.Equal(vorher[unveraendert.ID], nachher[unveraendert.ID]);
            Assert.Null(nachher[kopie.ID]);
            foreach (KeyValuePair<int, string> p in vorher.Where(p => p.Key != geaendert.ID))
                Assert.Equal(p.Value, nachher[p.Key]);
            // Gelesen wie gespeichert.
            Assert.Equal(vorher[unveraendert.ID],
                         ctrl.LesenJeGebaeude(idGebaeude).SelectMany(z => z.Bauteile).Single(b => b.ID == unveraendert.ID).Flaechenherkunft);
        }

        // =============================================================================
        //  Teil 4 - Kopierwege
        // =============================================================================

        [Fact]
        public void Das_Projektduplikat_traegt_die_Flaechenherkunft()
        {
            if (!_db.Vorhanden) return;
            Dictionary<int, string> quelle = Importieren("ifc4_ohne_mengen.ifc", out _);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(Projektname(), Projektname() + " G5-0");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(Verteilung(quelle.Values), Verteilung(WerteDesProjekts(neu)));
        }

        [Fact]
        public void Der_Projekttransfer_traegt_die_Flaechenherkunft()
        {
            if (!_db.Vorhanden) return;
            Dictionary<int, string> quelle = Importieren("ifc4_ohne_mengen.ifc", out _);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-g50-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "f.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Projektname(), paket));
                int neu = io.Importieren(paket, "Transfer G5-0", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(Verteilung(quelle.Values), Verteilung(WerteDesProjekts(neu)));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Schreibt den Vorschlag der Probe an das Gebäude des Projekts; Rückgabe: gespeicherte Herkunft je Bauteil-Id.</summary>
        private static Dictionary<int, string> Importieren(string probe, out GebaeudeZonenCtrl.Vorschlagsergebnis e)
        {
            ProjektGebaeudeModel g = GebaeudeBauteilvorschlagDatenbankTests.Zeile(PROJEKT);
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag(probe);
            Assert.False(v.Abgelehnt);
            e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);
            Dictionary<int, string> werte = Gespeichert(g.ID_Gebaeude);
            // Jede Zeile trägt den Wert ihrer Bauteilzeile (FlaechenherkunftWerte.Wert), über alle Zonen.
            List<BauteilModel> geschrieben = e.Zonen.SelectMany(z => z.Bauteile).ToList();
            Assert.Equal(geschrieben.Count, werte.Count);
            foreach (BauteilModel b in geschrieben) Assert.Equal(b.Flaechenherkunft, werte[b.ID]);
            Assert.Equal(v.Zeilen.Count(z => z.Flaechenherkunft.HasValue), werte.Values.Count(w => w != null));
            return werte;
        }

        private static Dictionary<int, string> Gespeichert(int idGebaeude)
            => DataRepository.GetDataTable(
                    "SELECT b.ID, b.Flaechenherkunft FROM Tab_Bauteil b INNER JOIN Tab_Zone z ON z.ID = b.ID_Zone WHERE z.ID_Gebaeude = ?",
                    new DbParam("@g", idGebaeude))
                .Rows.Cast<System.Data.DataRow>()
                .ToDictionary(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture),
                              r => r[1] == DBNull.Value ? null : Convert.ToString(r[1], CultureInfo.InvariantCulture));

        private static IEnumerable<string> WerteDesProjekts(int idProjekt)
            => DataRepository.GetDataTable(
                    "SELECT b.Flaechenherkunft FROM Tab_Bauteil b INNER JOIN Tab_Zone z ON z.ID = b.ID_Zone " +
                    "INNER JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude WHERE g.ID_Projekt = ?", new DbParam("@p", idProjekt))
                .Rows.Cast<System.Data.DataRow>()
                .Select(r => r[0] == DBNull.Value ? null : Convert.ToString(r[0], CultureInfo.InvariantCulture));

        private static string Verteilung(IEnumerable<string> werte)
            => string.Join(";", werte.GroupBy(w => w ?? "NULL").OrderBy(x => x.Key, StringComparer.Ordinal)
                                     .Select(x => x.Key + "=" + x.Count().ToString(CultureInfo.InvariantCulture)));

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
