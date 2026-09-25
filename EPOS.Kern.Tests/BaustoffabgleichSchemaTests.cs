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
    /// <b>Der Schemaschritt des Namensabgleichs ohne Datenbank</b> (<see cref="BaustoffabgleichSchema"/>): die
    /// Nummer, STRICT, die Verweise und Kaskaden, die Indizes.
    /// </summary>
    public class BaustoffabgleichSchemaRegelTests
    {
        [Fact]
        public void Die_Nummer_folgt_auf_die_Nachtzeit_und_der_Zielstand_traegt_sie()
        {
            Assert.True(BaustoffabgleichSchema.SCHRITT > NachtzeitSchema.SCHRITT);
            Assert.Equal(146, BaustoffabgleichSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= BaustoffabgleichSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + BaustoffabgleichSchema.SCHRITT + ".");
        }

        [Fact]
        public void Beide_Tabellen_sind_STRICT_mit_Verweis_auf_den_Katalog_und_Loeschweitergabe()
        {
            List<KeyValuePair<string, string>> t = BaustoffabgleichSchema.Tabellenanweisungen.ToList();
            Assert.Equal(new[] { "Tab_Baustoffsynonym_STAMM", "Tab_Baustoffzuordnung" }, t.Select(x => x.Key));
            foreach (KeyValuePair<string, string> a in t)
            {
                Assert.StartsWith("CREATE TABLE IF NOT EXISTS \"" + a.Key + "\" (", a.Value, StringComparison.Ordinal);
                Assert.EndsWith(") STRICT", a.Value, StringComparison.Ordinal);
                Assert.Contains("\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT", a.Value);
                Assert.Contains("FOREIGN KEY (\"ID_Baustoff\") REFERENCES \"Tab_Baustoff_STAMM\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE", a.Value);
            }
            Assert.Contains("\"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))", BaustoffabgleichSchema.SQL_CREATE_SYNONYM);
            Assert.Contains("CHECK (\"Sprache\" IN ('de','en'))", BaustoffabgleichSchema.SQL_CREATE_SYNONYM);
            Assert.Contains("FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE",
                            BaustoffabgleichSchema.SQL_CREATE_ZUORDNUNG);
            Assert.DoesNotContain("ReadOnly", BaustoffabgleichSchema.SQL_CREATE_ZUORDNUNG);
            Assert.Equal(4, BaustoffabgleichSchema.Indexanweisungen.Count());
            Assert.Equal(SchemaKatalog.TAB_BAUSTOFFSYNONYM_STAMM, BaustoffabgleichSchema.TAB_SYNONYM);
            Assert.Equal(SchemaKatalog.TAB_BAUSTOFFZUORDNUNG, BaustoffabgleichSchema.TAB_ZUORDNUNG);
            // Der Katalog heißt ordinal auf _STAMM (Auslieferungsvorlage), die Zuordnung nicht.
            Assert.EndsWith("_STAMM", BaustoffabgleichSchema.TAB_SYNONYM, StringComparison.Ordinal);
            Assert.False(BaustoffabgleichSchema.TAB_ZUORDNUNG.EndsWith("_STAMM", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// <b>Der Schemaschritt des Namensabgleichs an der Testdatenbank</b>: Stand der Repo-Datei (Tabellen,
    /// Indizes, Saat vollständig mit <c>ReadOnly = 1</c>), der Schritt aus dem Stand davor und wiederholbar,
    /// die Kaskaden und Prüfungen, das Merken und Vergessen einer Zuordnung (N7), der Abgleich über die
    /// Datenbank gleich dem über die Saat, der Weg durch das Projektduplikat, und Migration, Werkzeug und
    /// Testvorrichtung führen den Schritt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BaustoffabgleichSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Projekt 1007 (ein Gebäude) — Projektname für das Duplikat.</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        // =====================================================================
        //  Der Stand der Testdatenbank
        // =====================================================================

        [Fact]
        public void Die_Testdatenbank_traegt_Tabellen_Indizes_und_die_ganze_Saat()
        {
            if (!_db.Vorhanden) return;

            Assert.True(BaustoffabgleichSchema.Vollstaendig());
            foreach (KeyValuePair<string, string> a in BaustoffabgleichSchema.Tabellenanweisungen)
            {
                string sql = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", a.Key)), CultureInfo.InvariantCulture);
                Assert.Equal(a.Value.Replace("IF NOT EXISTS ", ""), sql);
            }
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Materialname\", \"Sprache\", \"ID_Baustoff\", \"Quelle\", \"ReadOnly\" FROM \"Tab_Baustoffsynonym_STAMM\" ORDER BY \"ID\"");
            Assert.Equal(BaustoffabgleichSchema.Saat.Count, t.Rows.Count);
            for (int i = 0; i < t.Rows.Count; i++)
            {
                BaustoffsynonymSaat s = BaustoffabgleichSchema.Saat[i];
                DataRow r = t.Rows[i];
                Assert.Equal((s.Id, s.Materialname, s.Sprache, s.IdBaustoff, s.Quelle, 1L),
                             (Convert.ToInt32(r["ID"]), Convert.ToString(r["Materialname"]), Convert.ToString(r["Sprache"]),
                              Convert.ToInt32(r["ID_Baustoff"]), Convert.ToString(r["Quelle"]), Convert.ToInt64(r["ReadOnly"])));
            }
            Assert.Equal(BaustoffabgleichSchema.SAAT_ID_GRENZE - 1,
                         Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Baustoffsynonym_STAMM'"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoffzuordnung\""));
            // Jedes Synonym zeigt auf eine herstellerneutrale Saatzeile.
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoffsynonym_STAMM\" s JOIN \"Tab_Baustoff_STAMM\" b ON b.\"ID\" = s.\"ID_Baustoff\" " +
                                 "WHERE b.\"Hersteller\" IS NOT NULL OR b.\"ReadOnly\" <> 1"));
        }

        [Fact]
        public void Der_Schritt_laeuft_aus_dem_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            BaustoffabgleichSchema.Bericht zweiter = BaustoffabgleichSchema.Ausfuehren();
            Assert.Equal(0, zweiter.TabellenAngelegt);
            Assert.Equal(0, zweiter.Gesaet);

            // Eine vom Anwender geänderte Saatzeile bleibt; eine fehlende kommt wieder.
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Baustoffsynonym_STAMM\" SET \"Quelle\" = 'eigen' WHERE \"ID\" = 1");
            DataRepository.ExecuteNonQuery("DELETE FROM \"Tab_Baustoffsynonym_STAMM\" WHERE \"ID\" = 2");
            Assert.False(BaustoffabgleichSchema.Vollstaendig());
            Assert.Equal(1, BaustoffabgleichSchema.SaatSchreiben());
            Assert.Equal("eigen", Convert.ToString(DataRepository.ExecuteScalar("SELECT \"Quelle\" FROM \"Tab_Baustoffsynonym_STAMM\" WHERE \"ID\" = 1")));
            Assert.True(BaustoffabgleichSchema.Vollstaendig());

            // Eine Datei vor dem Schritt.
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Baustoffzuordnung\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Baustoffsynonym_STAMM\"");
            DataRepository.ExecuteNonQuery("DELETE FROM sqlite_sequence WHERE name = 'Tab_Baustoffsynonym_STAMM'");
            Assert.False(BaustoffabgleichSchema.TabellenVorhanden());
            Assert.Empty(new BaustoffabgleichCtrl(PROJEKT).Synonyme());          // die Leser bleiben still
            Assert.Empty(new BaustoffabgleichCtrl(PROJEKT).Anwenderzuordnungen());
            BaustoffabgleichSchema.Bericht b = BaustoffabgleichSchema.Ausfuehren();
            Assert.Equal(2, b.TabellenAngelegt);
            Assert.Equal(BaustoffabgleichSchema.Saat.Count, b.Gesaet);
            Assert.True(BaustoffabgleichSchema.Vollstaendig());
            Assert.Contains("212 von 212 Synonym(en)", b.Zeile());
        }

        [Fact]
        public void Kaskaden_Pruefungen_und_die_Saatgrenze()
        {
            if (!_db.Vorhanden) return;

            // Ein eigener Katalogbaustoff bekommt eine Id über der Saatgrenze; ein eigenes Synonym ebenso.
            Assert.True(new BaustoffCtrl().KatalogAnlegen(new BaustoffModel { Bezeichner = "Probestoff", Lambda = 0.5, Rho = 900, Cp = 1000 }).Ok);
            int stoff = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT \"ID\" FROM \"Tab_Baustoff_STAMM\" WHERE \"Bezeichner\" = 'Probestoff'"));
            Assert.True(stoff >= BaustoffSchema.SAAT_ID_GRENZE);
            Assert.Equal(1, DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\", \"Sprache\", \"ID_Baustoff\") VALUES (?, 'de', ?)",
                new DbParam("@m", "probestoffname"), new DbParam("@b", stoff)));
            Assert.True(Zahl("SELECT MAX(\"ID\") FROM \"Tab_Baustoffsynonym_STAMM\"") >= BaustoffabgleichSchema.SAAT_ID_GRENZE);
            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "Probestoff 12345678", stoff, "2026-09-25T12:00:00Z").Ok);

            // Prüfungen: Sprache, ReadOnly, Name leer, Verweis ins Leere, doppelter Name.
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\", \"Sprache\", \"ID_Baustoff\") VALUES ('x1', 'fr', 1)"));
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\", \"Sprache\", \"ID_Baustoff\", \"ReadOnly\") VALUES ('x2', 'de', 1, 2)"));
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\", \"Sprache\", \"ID_Baustoff\") VALUES ('', 'de', 1)"));
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\", \"Sprache\", \"ID_Baustoff\") VALUES ('x3', 'de', 999999)"));
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffsynonym_STAMM\" (\"Materialname\", \"Sprache\", \"ID_Baustoff\") VALUES ('putz', 'de', 1)"));
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffzuordnung\" (\"ID_Projekt\", \"Materialname\", \"ID_Baustoff\", \"Zeitpunkt\") VALUES (999999, 'x', 1, 'jetzt')"));
            Assert.False(DataRepository.ExecuteSQL("INSERT INTO \"Tab_Baustoffzuordnung\" (\"ID_Projekt\", \"Materialname\", \"ID_Baustoff\", \"Zeitpunkt\") VALUES (?, 'probestoff', 1, 'jetzt')",
                                                   new DbParam("@p", PROJEKT)));   // je Projekt und Name eine

            // Fällt der Katalogbaustoff, fallen Synonym und Zuordnung mit.
            Assert.True(new BaustoffCtrl().KatalogLoeschen(stoff).Ok);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoffsynonym_STAMM\" WHERE \"ID_Baustoff\" = ?", new DbParam("@b", stoff)));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoffzuordnung\" WHERE \"ID_Baustoff\" = ?", new DbParam("@b", stoff)));

            // Fällt das Projekt, fallen seine Zuordnungen (ein eigenes, leeres Probeprojekt).
            Assert.Equal(1, DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Projekt\" (\"Projektname\") VALUES ('Abgleich-Probe')"));
            int probe = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT \"ID\" FROM \"Tab_Projekt\" WHERE \"Projektname\" = 'Abgleich-Probe'"));
            Assert.True(BaustoffabgleichCtrl.Merken(probe, "Fußbodenaufbau", 5).Ok);
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoffzuordnung\" WHERE \"ID_Projekt\" = ?", new DbParam("@p", probe)));
            DataRepository.ExecuteNonQuery("DELETE FROM \"Tab_Projekt\" WHERE \"ID\" = ?", new DbParam("@p", probe));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoffzuordnung\" WHERE \"ID_Projekt\" = ?", new DbParam("@p", probe)));
        }

        // =====================================================================
        //  Merken, Vergessen, Abgleich über die Datenbank (N7)
        // =====================================================================

        [Fact]
        public void Eine_Zuordnung_wird_je_Projekt_gemerkt_ersetzt_und_vergessen()
        {
            if (!_db.Vorhanden) return;

            BaustoffabgleichCtrl.Ergebnis e = BaustoffabgleichCtrl.Merken(PROJEKT, "Fußbodenaufbau", 5, "2026-09-25T10:00:00Z");
            Assert.True(e.Ok, e.Meldung);
            Assert.True(e.Id > 0);
            BaustoffNamenzuordnung z = Assert.Single(BaustoffabgleichCtrl.LesenJeProjekt(PROJEKT));
            Assert.Equal(("fussbodenaufbau", 5, "2026-09-25T10:00:00Z"), (z.Materialname, z.IdBaustoff, z.Zeitpunkt));

            // Derselbe Name (anders geschrieben) ersetzt; ein anderes Projekt bleibt unberührt.
            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "FUSSBODENAUFBAU 1234567", 6).Ok);
            z = Assert.Single(BaustoffabgleichCtrl.LesenJeProjekt(PROJEKT));
            Assert.Equal(6, z.IdBaustoff);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", z.Zeitpunkt);
            Assert.Empty(BaustoffabgleichCtrl.LesenJeProjekt(1030));

            // Benannt abgelehnt.
            BaustoffabgleichCtrl.Ergebnis leer = BaustoffabgleichCtrl.Merken(PROJEKT, "  ", 5);
            Assert.False(leer.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_NAME, leer.Meldung);
            BaustoffabgleichCtrl.Ergebnis fehlt = BaustoffabgleichCtrl.Merken(PROJEKT, "Holz", 999999);
            Assert.False(fehlt.Ok);
            Assert.Contains("999999", fehlt.Meldung);
            Assert.False(BaustoffabgleichCtrl.Merken(999999, "Holz", 5).Ok);    // Projekt fehlt: Fremdschlüssel

            // Der Abgleich über die Datenbank kennt die Zuordnung des Projekts — und nur dort.
            Abgleichtreffer t = new BaustoffabgleichCtrl(PROJEKT).Abgleich().Abgleichen("Fußbodenaufbau");
            Assert.Equal((Abgleichstufe.Anwender, 6), (t.Stufe, t.Baustoff.ID));
            Assert.False(new BaustoffabgleichCtrl(1030).Abgleich().Abgleichen("Fußbodenaufbau").Getroffen);
            Assert.False(new BaustoffabgleichCtrl().Abgleich().Abgleichen("Fußbodenaufbau").Getroffen);

            Assert.True(BaustoffabgleichCtrl.Vergessen(PROJEKT, "Fußbodenaufbau"));
            Assert.False(BaustoffabgleichCtrl.Vergessen(PROJEKT, "Fußbodenaufbau"));
            Assert.Empty(BaustoffabgleichCtrl.LesenJeProjekt(PROJEKT));
        }

        /// <summary>Der Abgleich über die Datenbank trifft wie der über die Saat — dieselben Listen.</summary>
        [Fact]
        public void Der_Abgleich_ueber_die_Datenbank_gleicht_dem_ueber_die_Saat()
        {
            if (!_db.Vorhanden) return;

            Baustoffabgleich db = new BaustoffabgleichCtrl(PROJEKT).Abgleich();
            var saat = new Baustoffabgleich(BaustoffabgleichDaten.AusSaat());
            Assert.Equal(saat.Katalogzahl, db.Katalogzahl);
            Assert.Equal(saat.Synonymzahl, db.Synonymzahl);
            foreach (string n in new[]
                     {
                         "Leichtbeton 102890359", "Stahlbeton 65690", "Kalksandstein 2816491304", "Luftschicht", "Holz",
                         "Aluminium 131198", "Leer", "Solid 397409098", "Ortbeton - bewehrt", "Ortbeton - bewehrt Verputzt",
                         "Fußbodenaufbau", "Mauerwerk - Naturstein", "Radial Gradient Fill 1515460218",
                         "Concrete, Cast-in-Place gray", "Air", "Stahlbetondecke", "KS 1400",
                     })
            {
                Abgleichtreffer a = db.Abgleichen(n), b = saat.Abgleichen(n);
                Assert.True(a.Stufe == b.Stufe && a.Sonderfall == b.Sonderfall && a.Baustoff?.ID == b.Baustoff?.ID, n + ": " + a + " / " + b);
                if (a.Getroffen)
                    Assert.Equal((b.Baustoff.Lambda, b.Baustoff.Rho, b.Baustoff.Cp), (a.Baustoff.Lambda, a.Baustoff.Rho, a.Baustoff.Cp));
            }
        }

        /// <summary>
        /// Das Projektduplikat nimmt die gemerkten Zuordnungen mit — sie zeigen weiter auf DENSELBEN
        /// Katalogbaustoff (der Katalog steht nie im Kopierplan).
        /// </summary>
        [Fact]
        public void Das_Projektduplikat_nimmt_die_Zuordnungen_mit_und_zeigt_auf_denselben_Katalogbaustoff()
        {
            if (!_db.Vorhanden) return;

            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "Fußbodenaufbau", 5).Ok);
            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "Ständerwand", 29).Ok);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Abgleich");
            Assert.True(neu > 0);
            Assert.Equal(BaustoffabgleichCtrl.LesenJeProjekt(PROJEKT).Select(z => (z.Materialname, z.IdBaustoff)),
                         BaustoffabgleichCtrl.LesenJeProjekt(neu).Select(z => (z.Materialname, z.IdBaustoff)));
            Assert.Equal(2, BaustoffabgleichCtrl.LesenJeProjekt(PROJEKT).Count);
        }

        // =====================================================================
        //  Migration, Werkzeug, Testvorrichtung, Reduzierskript
        // =====================================================================

        [Fact]
        public void Migration_Werkzeug_Testvorrichtung_und_Reduzierskript_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_BAUSTOFFABGLEICH = BaustoffabgleichSchema.SCHRITT", migration);
            int nacht = migration.IndexOf("new Schritt(SCHRITT_NACHTZEIT", StringComparison.Ordinal);
            int abgleich = migration.IndexOf("new Schritt(SCHRITT_BAUSTOFFABGLEICH", StringComparison.Ordinal);
            Assert.True(nacht > 0 && abgleich > nacht, "Der Schritt steht nicht hinter 144.");
            Assert.Contains("BaustoffabgleichSchema.SaatSchreiben()", migration);

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("BaustoffabgleichSchema.Ausfuehren()", StringComparison.Ordinal) >
                        werkzeug.IndexOf("BaustoffSchema.Ausfuehren()", StringComparison.Ordinal), "Das Werkzeug führt den Schritt nicht nach dem Baustoffkatalog.");
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("BaustoffabgleichSchema.Ausfuehren()", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("BaustoffSchema.Ausfuehren()", StringComparison.Ordinal));
            string reduziere = File.ReadAllText(Path.Combine(wurzel, "sql", "tools", "Reduziere-Testdatenbank.sql"));
            Assert.Contains("DELETE FROM \"Tab_Baustoffzuordnung\"", reduziere);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT (SELECT SchemaVersion FROM Tab_Applikation), (SELECT COUNT(*) FROM Tab_Baustoffsynonym_STAMM WHERE ReadOnly = 1), " +
                              "(SELECT COUNT(*) FROM Tab_Baustoffzuordnung)";
            using SqliteDataReader r = cmd.ExecuteReader();
            Assert.True(r.Read());
            Assert.True(r.GetInt64(0) >= BaustoffabgleichSchema.SCHRITT);
            Assert.Equal(BaustoffabgleichSchema.Saat.Count, r.GetInt64(1));
            Assert.Equal(0L, r.GetInt64(2));
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
