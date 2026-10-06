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
    /// <b>HC-5 Teil A — Schemaschritt <see cref="RaumgrundrissSchema"/></b> ohne Datenbank: Nummer und Kette, Register,
    /// DDL (STRICT, Kaskade, SET NULL, CHECK-Liste der Herleitung), Nachziehstellen in Migration, Werkzeug und
    /// Testvorrichtung, und die REPO-Datei der Testdatenbank (nur lesend).
    /// </summary>
    public sealed class RaumgrundrissSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Der_Schritt_haengt_an_der_Kette_und_der_Zielstand_traegt_ihn()
        {
            // Vorläufig über RaumnutzungSchema.SCHRITT + 2: Schritt 190 (NP5) ist angemeldet, aber noch nicht gebaut.
            Assert.Equal(191, RaumgrundrissSchema.SCHRITT);
            Assert.True(RaumgrundrissSchema.SCHRITT > RaumnutzungSchema.SCHRITT);
            Assert.Equal(SchemaStand.Zielversion, RaumgrundrissSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == RaumgrundrissSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
        }

        [Fact]
        public void Die_DDL_ist_STRICT_mit_Kaskade_SET_NULL_und_den_CHECKs_des_Konzepts()
        {
            string sql = RaumgrundrissSchema.SQL_CREATE;
            Assert.StartsWith("CREATE TABLE IF NOT EXISTS \"Tab_Raumgrundriss\" (", sql, StringComparison.Ordinal);
            Assert.EndsWith(") STRICT", sql, StringComparison.Ordinal);
            Assert.Contains("\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT", sql);
            Assert.DoesNotContain("ID_Projekt", sql);
            Assert.Contains("REFERENCES \"Tab_Importquelle\" (\"ID\") ON DELETE CASCADE", sql);
            Assert.Contains("REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE SET NULL", sql);
            Assert.Contains("UNIQUE (\"ID_Importquelle\", \"Quellkennung\")", sql);
            Assert.Contains("\"Herleitung\" IN ('KoerperBoden','KoerperDecke','KoerperHuelle','Boden','Decke')", sql);
            Assert.Contains("NOT GLOB '*[^0-9,;|-]*'", sql);
            Assert.Equal(new[] { "Tab_Raumgrundriss", "idx_Raumgrundriss_Zone" }, RaumgrundrissSchema.Anweisungen.Select(a => a.Key));
            Assert.Equal(13, RaumgrundrissSchema.Spalten.Count);
            Assert.Equal(6, (int)Umrissherleitung.KoerperBoden);
            Assert.Equal(7, (int)Umrissherleitung.KoerperDecke);
            Assert.Equal(8, (int)Umrissherleitung.KoerperHuelle);
            Assert.Equal(2, (int)Geometrieherkunft.Dateikoerper);
        }

        [Fact]
        public void Migration_Werkzeug_Vorrichtung_und_Repo_Datei_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_RAUMGRUNDRISS = RaumgrundrissSchema.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_Raumgrundriss(Lauf l)", migration);
            int vorher = migration.IndexOf("new Schritt(SCHRITT_RAUMNUTZUNG", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_RAUMGRUNDRISS", StringComparison.Ordinal);
            Assert.True(vorher > 0 && ort > vorher, "Der Schritt steht nicht hinter dem Katalog der Nutzungsprofile.");
            Assert.Contains("RaumgrundrissSchema.Ausfuehren(", File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs")));
            Assert.Contains("RaumgrundrissSchema.Ausfuehren(null)", File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs")));

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= RaumgrundrissSchema.SCHRITT);
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_list WHERE name = 'Tab_Raumgrundriss' AND strict = 1"));
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'idx_Raumgrundriss_Zone'"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Raumgrundriss"));
        }

        internal static string Repowurzel()
        {
            string o = AppContext.BaseDirectory;
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf"))) o = Path.GetDirectoryName(o);
            return o;
        }

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// <b>HC-5 Teil A — die Ablage der Grundrisse</b> gegen die Arbeitskopie der Testdatenbank (Konzept HottCAD-Verbund
    /// 11.3): Schreiben beim Import im Vorgang der Herkunft (Zone aus der Paarung), Ersetzen an einer Quelle, Lesen der
    /// jüngsten Quelle je Gebäude und je Zone, Kaskade über die Quelle, SET NULL beim Löschen der Zone, die CHECKs,
    /// Duplizieren mit den Zonenkopien.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumgrundrissCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int GEBAEUDE = 10614;              // Projekt 1007
        private const string PROJEKTNAME = "Laurentiuskirche";

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        /// <summary>Ein Grundriss aus einem Quader [m] mit Kennung — über denselben Weg wie beim Import.</summary>
        private static Raumgrundriss Grundriss(string kennung, double x0, double y0, double x1, double y1, double? flaeche = null)
        {
            Koerpergrundriss k = Koerpergrundriss.Ableiten(KoerpergrundrissTests.Koerper(KoerpergrundrissTests.Quader(x0, y0, 0, x1, y1, 3)));
            return Raumgrundriss.Bilden(kennung, "Raum " + kennung, "EG", 0.0, flaeche, flaeche * 3, k.Herleitung.Value, k.Ringe,
                                        k.BodenM, k.HoeheM, k.Vermerke, false);
        }

        private static (int Wohnen, int Keller) ZonenAnlegen()
        {
            ZoneModel wohnen = GebaeudeG3PruefregelTests.GueltigeZone();
            wohnen.Nutzflaeche = 120;
            var keller = new ZoneModel { ID = -2, Bezeichner = "Keller", IstBeheizt = false, Nutzflaeche = 60 };
            GebaeudeZonenCtrl.Ergebnis z = new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { wohnen, keller });
            Assert.True(z.Ok, z.Meldung);
            return (wohnen.ID, keller.ID);
        }

        private static List<GebaeudeQuellzuordnung> Paarungen(int wohnen, int keller) => new List<GebaeudeQuellzuordnung>
        {
            new GebaeudeQuellzuordnung("Building", "bldg-1", ImportZiel.Gebaeude),
            new GebaeudeQuellzuordnung("IfcSpace", "space-wohnen", ImportZiel.Zone, wohnen),
            new GebaeudeQuellzuordnung("IfcSpace", "space-keller", ImportZiel.Zone, keller),
        };

        private static List<Raumgrundriss> Drei() => new List<Raumgrundriss>
        {
            Grundriss("space-wohnen", 0, 0, 10, 12, 120),
            Grundriss("space-keller", 0, 0, 10, 6, 60),
            Grundriss("space-ohne", 20, 0, 22, 2),          // ohne Paarung: ID_Zone NULL
        };

        private int Schreiben(out int wohnen, out int keller)
        {
            (wohnen, keller) = ZonenAnlegen();
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(
                GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle("IFC"), Paarungen(wohnen, keller), null, Drei());
            Assert.True(e.Ok, e.Meldung);
            return e.IdImportquelle;
        }

        [Fact]
        public void Import_schreibt_je_Raum_eine_Zeile_mit_der_Zone_aus_der_Paarung()
        {
            if (!_db.Vorhanden) return;
            int quelle = Schreiben(out int wohnen, out int keller);
            List<Raumgrundriss> g = new GebaeudeImportCtrl().LesenRaumgrundrisse(GEBAEUDE);
            Assert.Equal(new[] { "space-wohnen", "space-keller", "space-ohne" }, g.Select(x => x.Quellkennung));
            Assert.All(g, x => Assert.Equal(quelle, x.IdImportquelle));
            Assert.Equal(new int?[] { wohnen, keller, null }, g.Select(x => x.IdZone));
            Raumgrundriss w = g[0], soll = Drei()[0];
            Assert.Equal(soll.RingeText, w.RingeText);
            Assert.Equal("0,0;10000,0;10000,12000;0,12000", w.RingeText);
            Assert.Equal(120.0, w.RingflaecheM2, 9);
            Assert.Equal(0.0, w.Abweichung.Value, 9);
            Assert.Equal(3.0, w.HoeheM, 9);
            Assert.Equal(0.0, w.BodenM, 9);
            Assert.Equal(Umrissherleitung.KoerperBoden, w.Herleitung);
            Assert.Equal("Raum space-wohnen", w.Raumname);
            Assert.Equal("EG", w.Geschoss);
            Assert.Null(g[2].Abweichung);
            Assert.Empty(w.Vermerke);
            Assert.Equal(new[] { "space-keller" }, new GebaeudeImportCtrl().LesenRaumgrundrisseDerZone(keller).Select(x => x.Quellkennung));
        }

        [Fact]
        public void Ersetzen_an_der_Quelle_und_die_juengste_Quelle_gilt()
        {
            if (!_db.Vorhanden) return;
            int quelle = Schreiben(out int wohnen, out _);
            var ctrl = new GebaeudeImportCtrl();
            GebaeudeImportCtrl.Ergebnis e = ctrl.SchreibeRaumgrundrisse(quelle, new[] { Grundriss("space-wohnen", 0, 0, 5, 5, 25) });
            Assert.True(e.Ok, e.Meldung);
            Raumgrundriss einer = Assert.Single(ctrl.LesenRaumgrundrisse(GEBAEUDE));
            Assert.Equal(wohnen, einer.IdZone);                  // Zone aus den Paarungen der Quelle
            Assert.Equal(25.0, einer.RingflaecheM2, 9);

            // Ein zweiter Lauf an demselben Gebäude: gelesen wird die jüngste Quelle.
            GebaeudeImportCtrl.Ergebnis zwei = ctrl.SchreibeHerkunft(GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle("IFC"),
                new[] { new GebaeudeQuellzuordnung("Building", "bldg-1", ImportZiel.Gebaeude) }, null,
                new[] { Grundriss("space-neu", 0, 0, 2, 2) });
            Assert.True(zwei.Ok, zwei.Meldung);
            Assert.Equal(new[] { "space-neu" }, ctrl.LesenRaumgrundrisse(GEBAEUDE).Select(x => x.Quellkennung));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Raumgrundriss"));
            Assert.False(ctrl.SchreibeRaumgrundrisse(999999, Drei()).Ok);
        }

        [Fact]
        public void Kaskade_ueber_die_Quelle_und_SET_NULL_beim_Loeschen_der_Zone()
        {
            if (!_db.Vorhanden) return;
            int quelle = Schreiben(out int wohnen, out int keller);
            // Zone löschen: der Keller verschwindet aus der Liste, sein Grundriss bleibt ohne Zone.
            ZoneModel w = new GebaeudeZonenCtrl().LesenJeGebaeude(GEBAEUDE).Single(z => z.ID == wohnen);
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { w }).Ok);
            List<Raumgrundriss> g = new GebaeudeImportCtrl().LesenRaumgrundrisse(GEBAEUDE);
            Assert.Equal(3, g.Count);
            Assert.Null(g.Single(x => x.Quellkennung == "space-keller").IdZone);
            Assert.Empty(new GebaeudeImportCtrl().LesenRaumgrundrisseDerZone(keller));

            // Quelle löschen (wie mit dem Gebäude): die Grundrisse gehen mit.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Importquelle WHERE ID = ?", new DbParam("@q", quelle));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumgrundriss"));
            Assert.Empty(new GebaeudeImportCtrl().LesenRaumgrundrisse(GEBAEUDE));
        }

        [Theory]
        [InlineData("'KoerperBoden'", "'0,0;1000,0;0,1000'", 3.0, 0.5)]
        public void Die_CHECKs_der_Tabelle_halten(string herleitung, string ringe, double hoehe, double flaeche)
        {
            if (!_db.Vorhanden) return;
            int quelle = Schreiben(out _, out _);
            string Einfuegen(string k, string h, string r, string ho, string f)
                => "INSERT INTO Tab_Raumgrundriss (ID_Importquelle, Quellkennung, Boden_m, Hoehe_m, Ringe, Ringflaeche_m2, Herleitung) " +
                   "VALUES (" + quelle.ToString(CultureInfo.InvariantCulture) + ", " + k + ", 0, " + ho + ", " + r + ", " + f + ", " + h + ")";
            string ho0 = hoehe.ToString(CultureInfo.InvariantCulture), f0 = flaeche.ToString(CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery(Einfuegen("'gut'", herleitung, ringe, ho0, f0));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumgrundriss"));
            var falsch = new[]
            {
                Einfuegen("'gut'", herleitung, ringe, ho0, f0),                     // doppelte Kennung je Quelle
                Einfuegen("'a'", "'Rechteck'", ringe, ho0, f0),                     // Herleitung außerhalb der Liste
                Einfuegen("'b'", herleitung, "'0,0;1000,0;0,1x0'", ho0, f0),        // fremde Zeichen in den Ringen
                Einfuegen("'c'", herleitung, "'0,0;1,0'", ho0, f0),                 // zu kurz
                Einfuegen("'d'", herleitung, ringe, "0", f0),                       // Höhe 0
                Einfuegen("'e'", herleitung, ringe, ho0, "0"),                      // Fläche 0
                Einfuegen("''", herleitung, ringe, ho0, f0),                        // leere Kennung
            };
            foreach (string sql in falsch)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    Assert.ThrowsAny<Exception>(() => v.Ausfuehren(sql));
                }
            }
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumgrundriss"));
        }

        [Fact]
        public void Nicht_speicherbare_Grundrisse_werden_uebergangen_ohne_den_Import_scheitern_zu_lassen()
        {
            if (!_db.Vorhanden) return;
            (int wohnen, int keller) = ZonenAnlegen();
            Raumgrundriss gut = Grundriss("space-wohnen", 0, 0, 4, 3);
            var liste = new List<Raumgrundriss> { gut, gut, null, new Raumgrundriss { Quellkennung = "leer" } };
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(
                GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle("IFC"), Paarungen(wohnen, keller), null, liste);
            Assert.True(e.Ok, e.Meldung);
            Assert.Single(new GebaeudeImportCtrl().LesenRaumgrundrisse(GEBAEUDE));
            Assert.False(GebaeudeImportCtrl.Speicherbar(new Raumgrundriss { Quellkennung = "x", HoeheM = 3, RingflaecheM2 = 1 }));
        }

        [Fact]
        public void Duplizieren_nimmt_die_Grundrisse_mit_und_zeigt_auf_die_Zonenkopien()
        {
            if (!_db.Vorhanden) return;
            Schreiben(out _, out _);
            var plan = new ProjektDuplizierenCtrl().ErmittlePlan().ToDictionary(s => s.Tabelle, StringComparer.OrdinalIgnoreCase);
            Assert.StartsWith("ID_Importquelle IN (SELECT ID FROM Tab_Importquelle", plan[SchemaKatalog.TAB_RAUMGRUNDRISS].Filter);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " HC-5");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", neu)), CultureInfo.InvariantCulture);
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(gebaeude);
            List<Raumgrundriss> g = new GebaeudeImportCtrl().LesenRaumgrundrisse(gebaeude);
            Assert.Equal(new[] { "space-wohnen", "space-keller", "space-ohne" }, g.Select(x => x.Quellkennung));
            Assert.Equal(zonen.Single(z => z.Bezeichner == "Keller").ID, g[1].IdZone);
            Assert.Contains(g[0].IdZone.Value, zonen.Select(z => z.ID));
            Assert.Null(g[2].IdZone);
            Assert.Equal(Drei()[0].RingeText, g[0].RingeText);
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Raumgrundriss"));
            // Das Original bleibt unverändert.
            Assert.Equal(3, new GebaeudeImportCtrl().LesenRaumgrundrisse(GEBAEUDE).Count);
        }

        [Fact]
        public void Die_Tabelle_steht_STRICT_mit_Fremdschluesseln_und_Index()
        {
            if (!_db.Vorhanden) return;
            Assert.True(RaumgrundrissSchema.Vollstaendig());
            Assert.Equal(0, RaumgrundrissSchema.Ausfuehren(null));     // wiederholbar
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_table_list WHERE name = 'Tab_Raumgrundriss' AND strict = 1"));
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"from\", \"table\", on_delete FROM pragma_foreign_key_list(?) ORDER BY \"from\"", new DbParam("@t", RaumgrundrissSchema.TAB));
            Assert.Equal(new[] { "ID_Importquelle|Tab_Importquelle|CASCADE", "ID_Zone|Tab_Zone|SET NULL" },
                         t.Rows.Cast<DataRow>().Select(r => r[0] + "|" + r[1] + "|" + r[2]));
            Assert.Equal(new[] { "ID" }.Concat(RaumgrundrissSchema.Spalten), DataRepository.SpaltenVonTabelle(RaumgrundrissSchema.TAB));
        }
    }
}
