using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt der <b>eingebauten Typkennfelder der Kältemaschinen</b> (<see cref="KaeltemaschinenTypkennfelderSchema"/>,
    /// Welle KM2) auf einer Arbeitskopie der Testdatenbank.
    ///
    /// <para><b>Geprüft wird:</b> die Kette (<c>KuehlkurveSchema.SCHRITT + 1</c>), der Zielstand und die Stufe der
    /// Paketanhebung; dass der Schritt auf einem Stand 202 (Typkennfelder gelöscht, Marker 202) jedes Typkennfeld als
    /// gesperrten Satz mit 24 gesperrten Kennlinienpunkten, Katalogschlüssel und Prüfsumme anlegt, die drei
    /// Beispielgeräte Zelle für Zelle stehen lässt und <c>foreign_key_check</c> leer bleibt; dass ein zweiter Lauf nichts
    /// ändert; dass ein eigener Satz des Anwenders unter einem Bezeichner nicht überschrieben wird; dass Werkzeug,
    /// Migration und Testkopie den Schritt hinter 202 führen und die Testdatenbank ihn trägt.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltemaschinenTypkennfelderSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private const string TAB = KaeltemaschineSchema.TAB_STAMM;
        private const string TAB_K = KaeltemaschineSchema.TAB_KENNDATEN_STAMM;

        /// <summary>Die Beispielgeräte der Saat von Schritt 182 (<see cref="KaeltemaschineSchema"/>).</summary>
        private const int BEISPIELGERAETE = 3;

        private static int Anzahl => KaeltemaschinenTypkennfelder.Lesen().Count;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_folgt_auf_die_Kuehlkurve_und_ist_das_Ziel()
        {
            Assert.Equal(KuehlkurveSchema.SCHRITT + 1, KaeltemaschinenTypkennfelderSchema.SCHRITT);
            Assert.Equal(203, KaeltemaschinenTypkennfelderSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaeltemaschinenTypkennfelderSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KaeltemaschinenTypkennfelderSchema.SCHRITT + ".");
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KaeltemaschinenTypkennfelderSchema.SCHRITT &&
                                                       s.Wirkung == Paketanhebung.Art.Katalog);
            Assert.Equal(new[] { TAB, TAB_K }, KaeltemaschinenTypkennfelderSchema.Voraussetzungen());
        }

        // =============================================================================
        //  Teil 2 - Auf der Arbeitskopie der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Vom Stand 202 aus: jedes Typkennfeld als gesperrter Satz mit 24 gesperrten Kennlinienpunkten, Schlüssel und
        /// Prüfsumme; die Beispielgeräte bleiben, wie sie waren; der zweite Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Vom_Stand_202_saet_der_Schritt_alle_Typkennfelder_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Stand202Herstellen();
            Assert.False(KaeltemaschinenTypkennfelderSchema.Vollstaendig());
            Assert.Equal((long)BEISPIELGERAETE, Zahl("SELECT COUNT(*) FROM " + TAB));
            string beispieleVorher = Beispielgeraete();

            var bericht = new List<string>();
            KaeltemaschinenTypkennfelder.Einspielergebnis e = KaeltemaschinenTypkennfelderSchema.Ausfuehren(bericht);
            Assert.True(e.Ok, e.Fehler);
            Assert.Equal(Anzahl, e.Neu);
            Assert.Equal(0, e.Uebersprungen);
            Assert.Single(bericht);
            Assert.StartsWith(Anzahl + " Typkennfeld(er) der Kaeltemaschinen gesaet", bericht[0], StringComparison.Ordinal);
            Assert.True(KaeltemaschinenTypkennfelderSchema.Vollstaendig());

            Assert.Equal(BEISPIELGERAETE + (long)Anzahl, Zahl("SELECT COUNT(*) FROM " + TAB));
            Assert.Equal(BEISPIELGERAETE + (long)Anzahl, Zahl("SELECT COUNT(*) FROM " + TAB + " WHERE ReadOnly = 1"));
            foreach (KaeltemaschinenTypkennfelder.Typkennfeld t in KaeltemaschinenTypkennfelder.Lesen())
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, ReadOnly, " + Katalogfassung.SPALTE_SCHLUESSEL + ", " + Katalogfassung.SPALTE_PRUEFSUMME +
                    " FROM " + TAB + " WHERE Bezeichner = ?", new DbParam("?", t.Bezeichner));
                Assert.Equal(1, dt.Rows.Count);
                DataRow r = dt.Rows[0];
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(KaeltemaschinenTypkennfelder.Schluessel(t.Bezeichner),
                             Convert.ToString(r[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture));
                Assert.False(r[Katalogfassung.SPALTE_PRUEFSUMME] is DBNull, "Ohne Prüfsumme: " + t.Bezeichner);
                long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                Assert.Equal(24L, Zahl("SELECT COUNT(*) FROM " + TAB_K + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE +
                                       " = ? AND ReadOnly = 1", new DbParam("?", id)));
            }
            Assert.Equal(beispieleVorher, Beispielgeraete());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe3));

            long punkte = Zahl("SELECT COUNT(*) FROM " + TAB_K);
            KaeltemaschinenTypkennfelder.Einspielergebnis z = KaeltemaschinenTypkennfelderSchema.Ausfuehren(null);
            Assert.True(z.Ok, z.Fehler);
            Assert.Equal(0, z.Neu);
            Assert.Equal(Anzahl, z.Uebersprungen);
            Assert.Equal(BEISPIELGERAETE + (long)Anzahl, Zahl("SELECT COUNT(*) FROM " + TAB));
            Assert.Equal(punkte, Zahl("SELECT COUNT(*) FROM " + TAB_K));
            Assert.True(KaeltemaschinenTypkennfelderSchema.Vollstaendig());
        }

        /// <summary>Auf der gelieferten Testdatenbank (Stand 203) findet der Schritt nichts zu tun.</summary>
        [Fact]
        public void Auf_der_Testdatenbank_steht_der_Schritt_bereits()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltemaschinenTypkennfelderSchema.Vollstaendig());
            long saetze = Zahl("SELECT COUNT(*) FROM " + TAB);
            long punkte = Zahl("SELECT COUNT(*) FROM " + TAB_K);
            KaeltemaschinenTypkennfelder.Einspielergebnis e = KaeltemaschinenTypkennfelderSchema.Ausfuehren(null);
            Assert.True(e.Ok, e.Fehler);
            Assert.Equal(0, e.Neu);
            Assert.Equal(saetze, Zahl("SELECT COUNT(*) FROM " + TAB));
            Assert.Equal(punkte, Zahl("SELECT COUNT(*) FROM " + TAB_K));
        }

        /// <summary>
        /// Ein eigener Satz des Anwenders unter dem Bezeichner eines Typkennfelds bleibt, wie er ist: Der Schritt zählt
        /// ihn als übersprungen und legt die übrigen an.
        /// </summary>
        [Fact]
        public void Ein_eigener_Satz_unter_einem_Bezeichner_wird_nicht_ueberschrieben()
        {
            if (!_db.Vorhanden) return;

            Stand202Herstellen();
            string name = KaeltemaschinenTypkennfelder.Lesen()[0].Bezeichner;
            Assert.True(DataRepository.ExecuteSQL("UPDATE " + TAB + " SET Bezeichner = ? WHERE ID = 1", new DbParam("?", name)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE " + TAB + " SET ReadOnly = 0 WHERE ID = 1"));
            string vorher = Zeile(1);

            KaeltemaschinenTypkennfelder.Einspielergebnis e = KaeltemaschinenTypkennfelderSchema.Ausfuehren(null);
            Assert.True(e.Ok, e.Fehler);
            Assert.Equal(Anzahl - 1, e.Neu);
            Assert.Equal(1, e.Uebersprungen);
            Assert.Equal(vorher, Zeile(1));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM " + TAB + " WHERE Bezeichner = ?", new DbParam("?", name)));
            Assert.True(KaeltemaschinenTypkennfelderSchema.Vollstaendig());
        }

        /// <summary>Werkzeug, Migration und Testkopie führen den Schritt hinter 202; die Testdatenbank trägt ihn.</summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wSaat = werkzeug.IndexOf("KaeltemaschinenTypkennfelderSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wSaat > 0 && wSaat > werkzeug.IndexOf("KuehlkurveSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Die Saat steht im Werkzeug nicht hinter der Kühlkurve.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KAELTEMASCHINEN_TYPKENNFELDER = KaeltemaschinenTypkennfelderSchema.SCHRITT", migration,
                            StringComparison.Ordinal);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_KUEHLKURVE", StringComparison.Ordinal);
            int ortSaat = migration.IndexOf("new Schritt(SCHRITT_KAELTEMASCHINEN_TYPKENNFELDER", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSaat > ortVorher, "Der Schritt steht nicht hinter 202.");
            Assert.Contains("KaeltemaschinenTypkennfelderSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vSaat = vorrichtung.IndexOf("KaeltemaschinenTypkennfelderSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vSaat > 0 && vSaat > vorrichtung.IndexOf("KuehlkurveSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Saat steht in der Testkopie nicht hinter der Kühlkurve.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= KaeltemaschinenTypkennfelderSchema.SCHRITT);
            cmd.CommandText = "SELECT COUNT(*) FROM " + TAB + " WHERE ReadOnly = 1 AND " + Katalogfassung.SPALTE_SCHLUESSEL +
                              " = $s AND " + Katalogfassung.SPALTE_PRUEFSUMME + " IS NOT NULL";
            foreach (KaeltemaschinenTypkennfelder.Typkennfeld t in KaeltemaschinenTypkennfelder.Lesen())
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$s", KaeltemaschinenTypkennfelder.Schluessel(t.Bezeichner));
                Assert.Equal(1L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Löscht die Typkennfelder samt Kennlinie und setzt den Marker auf 202 — der Stand vor dem Schritt.</summary>
        private static void Stand202Herstellen()
        {
            foreach (KaeltemaschinenTypkennfelder.Typkennfeld t in KaeltemaschinenTypkennfelder.Lesen())
            {
                string schluessel = KaeltemaschinenTypkennfelder.Schluessel(t.Bezeichner);
                Assert.True(DataRepository.ExecuteSQL(
                    "DELETE FROM " + TAB_K + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE + " IN (SELECT ID FROM " + TAB +
                    " WHERE " + Katalogfassung.SPALTE_SCHLUESSEL + " = ?)", new DbParam("?", schluessel)));
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM " + TAB + " WHERE " + Katalogfassung.SPALTE_SCHLUESSEL + " = ?",
                                                      new DbParam("?", schluessel)));
            }
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Applikation SET SchemaVersion = ?",
                                                  new DbParam("?", KuehlkurveSchema.SCHRITT)));
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        /// <summary>Kopf und Kennlinie der Beispielgeräte, jede Zelle als Text.</summary>
        private static string Beispielgeraete()
        {
            var sb = new StringBuilder();
            for (int id = 1; id <= BEISPIELGERAETE; id++) sb.Append(Zeile(id)).Append('\n');
            DataTable k = DataRepository.GetDataTable("SELECT * FROM " + TAB_K + " WHERE " + KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE +
                                                      " <= ? ORDER BY ID", new DbParam("?", BEISPIELGERAETE));
            foreach (DataRow r in k.Rows)
                sb.Append(string.Join("|", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))).Append('\n');
            return sb.ToString();
        }

        private static string Zeile(int id)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + TAB + " WHERE ID = ?", new DbParam("?", id));
            Assert.Equal(1, dt.Rows.Count);
            return string.Join("|", dt.Rows[0].ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)));
        }
    }
}
