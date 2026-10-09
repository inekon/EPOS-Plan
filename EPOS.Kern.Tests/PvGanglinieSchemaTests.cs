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
    /// Der Schemaschritt <b>Photovoltaik mit Ganglinie</b> (<see cref="PvGanglinieSchema"/>, Schritt 205): Kette und
    /// Zielstand, die fünf Tabellen STRICT mit ihren Fremdschlüsseln, die Hebung einer Kopie ohne den Schritt, die
    /// Einträge in Werkzeug, Migration und Testkopie und das Katalogregister „PVG".
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvGanglinieSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_folgt_auf_die_Zonen_im_Gebaeudekatalog_und_ist_das_Ziel()
        {
            Assert.Equal(ZonenKatalogSchema.SCHRITT + 1, PvGanglinieSchema.SCHRITT);
            Assert.Equal(205, PvGanglinieSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= PvGanglinieSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == PvGanglinieSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Katalog);
        }

        [Fact]
        public void Die_Tabellen_sind_STRICT_und_haengen_ueber_IDs()
        {
            if (!_db.Vorhanden) return;
            Assert.True(PvGanglinieSchema.Vollstaendig());
            foreach (KeyValuePair<string, string> a in PvGanglinieSchema.Tabellenanweisungen)
            {
                string sql = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", a.Key)),
                    CultureInfo.InvariantCulture) ?? "";
                Assert.EndsWith("STRICT", sql.TrimEnd());
            }
            Assert.True(DataRepository.SpalteVorhanden(PvGanglinieSchema.TAB_KOPF_STAMM, "Raster_Minuten"));
            Assert.True(DataRepository.SpalteVorhanden(PvGanglinieSchema.TAB_KOPF, "Nennleistung_kWp"));
            Assert.Contains(("ID_Ganglinie", "Tab_PvGanglinie"), Fremdschluessel(PvGanglinieSchema.TAB_ZUORDNUNG));
            Assert.Contains(("ID_Projekt", "Tab_Projekt"), Fremdschluessel(PvGanglinieSchema.TAB_ZUORDNUNG));
            Assert.Contains(("ID_Ganglinie", "Tab_PvGanglinie_STAMM"), Fremdschluessel(PvGanglinieSchema.TAB_DATEN_STAMM));

            // Das Raster kennt nur 60 und 15 Minuten.
            Assert.False(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_PvGanglinie_STAMM (Bezeichner, Raster_Minuten) VALUES ('PVG-Probe', 30)"));
        }

        private static List<(string Von, string Ziel)> Fremdschluessel(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT \"from\", \"table\" FROM pragma_foreign_key_list(?)",
                                                       new DbParam("@t", tabelle));
            return dt.Rows.Cast<DataRow>()
                     .Select(r => (Convert.ToString(r[0], CultureInfo.InvariantCulture) ?? "",
                                   Convert.ToString(r[1], CultureInfo.InvariantCulture) ?? ""))
                     .ToList();
        }

        [Fact]
        public void Der_Schritt_hebt_eine_Kopie_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach (string t in new[] { PvGanglinieSchema.TAB_ZUORDNUNG, PvGanglinieSchema.TAB_DATEN, PvGanglinieSchema.TAB_KOPF,
                                         PvGanglinieSchema.TAB_DATEN_STAMM, PvGanglinieSchema.TAB_KOPF_STAMM })
                Assert.True(DataRepository.ExecuteSQL("DROP TABLE \"" + t + "\""));
            Assert.False(PvGanglinieSchema.Lesbar());

            var bericht = new List<string>();
            Assert.Equal(5, PvGanglinieSchema.Ausfuehren(bericht));
            Assert.Single(bericht);
            Assert.True(PvGanglinieSchema.Vollstaendig());

            Assert.Equal(0, PvGanglinieSchema.Ausfuehren(null));
            Assert.True(PvGanglinieSchema.Vollstaendig());
        }

        [Fact]
        public void Das_Katalogregister_fuehrt_die_PV_Ganglinie_mit_ihren_Werten()
        {
            Katalogtabelle t = Katalogfassung.Tabelle(PvGanglinieSchema.TAB_KOPF_STAMM);
            Assert.NotNull(t);
            Assert.Equal("PVG", t.Kuerzel);
            Assert.Contains(Katalogfassung.KinderUndEnkel(t), k => k.Tabelle == PvGanglinieSchema.TAB_DATEN_STAMM);
            Assert.NotNull(KatalogRegistry.Finde("PVGANGLINIE"));
        }

        /// <summary>Werkzeug, Migration und Testkopie führen den Schritt hinter 204; die Testdatenbank trägt ihn.</summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int w = werkzeug.IndexOf("PvGanglinieSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(w > 0 && w > werkzeug.IndexOf("ZonenKatalogSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_PV_GANGLINIE = PvGanglinieSchema.SCHRITT", migration, StringComparison.Ordinal);
            int vorher = migration.IndexOf("new Schritt(SCHRITT_ZONEN_KATALOG", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_PV_GANGLINIE", StringComparison.Ordinal);
            Assert.True(vorher > 0 && ort > vorher, "Der Schritt steht nicht hinter 204.");
            Assert.Contains("PvGanglinieSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int v = vorrichtung.IndexOf("PvGanglinieSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(v > 0 && v > vorrichtung.IndexOf("ZonenKatalogSchema.Ausfuehren(null)", StringComparison.Ordinal));

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= PvGanglinieSchema.SCHRITT);
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN " +
                              "('Tab_PvGanglinie_STAMM', 'Tab_PvGanglinieDaten_STAMM', 'Tab_PvGanglinie', 'Tab_PvGanglinieDaten', " +
                              "'Z_ProjektPvGanglinie')";
            Assert.Equal(5L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            // Kein Referenzprojekt führt eine PV-Ganglinie: Der Schritt ist reines DDL.
            cmd.CommandText = "SELECT COUNT(*) FROM \"Z_ProjektPvGanglinie\"";
            Assert.Equal(0L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
        }

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
            return d;
        }
    }
}
