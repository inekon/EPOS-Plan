using System;
using System.IO;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Waechter ueber den SCHEMASTAND der Repo-Testdatenbank (Auftrag Kostenbereich,
    /// Punkt 5).
    ///
    /// <para><b>Warum es ihn braucht.</b> <see cref="TestDatenbank"/> zieht jede
    /// ARBEITSKOPIE selbst nach (<c>SchemaNachziehen</c>), damit ein Testlauf auch auf
    /// einer aelteren Datei gruen wird. Genau das verdeckt aber, wenn die REPO-Datei
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> beim Anlegen eines neuen
    /// Migrationsschritts vergessen wurde: Tests und Referenzlauf liefen auf ihrer
    /// nachgezogenen Kopie weiter, waehrend die eingecheckte Datei — die Messlatte fuer
    /// CI, Werkzeuge und jeden neuen Klon — zurueckblieb. Das faellt sonst erst dort auf,
    /// wo niemand mehr den Zusammenhang sieht.</para>
    ///
    /// <para><b>Nur LESEND, und ohne Spuren.</b> Geoeffnet wird ueber eine
    /// <c>file:</c>-URI mit <c>mode=ro</c> UND <c>immutable=1</c>. Ohne
    /// <c>immutable=1</c> legt SQLite beim Oeffnen Sperr- und Journaldateien
    /// (<c>-wal</c>, <c>-shm</c>) neben der Datenbank an — und die meldet der
    /// <see cref="RepositoryOrdnungWacheTests"/> als Rueckstand im Bestand. Mit
    /// <c>immutable=1</c> sagt der Aufrufer zu, dass niemand sonst schreibt; SQLite
    /// verzichtet dann auf jede Sperre.</para>
    ///
    /// <para><b>Fehlt die Datei, wird nicht geprueft</b> — wie in
    /// <see cref="TestDatenbank"/>. Ein LFS-Zeiger dagegen bekommt seine eigene,
    /// benannte Meldung ueber <see cref="LfsZeigerProbe"/>.</para>
    /// </summary>
    public class TestdatenbankSchemastandWacheTests
    {
        [Fact]
        public void Die_Repo_Testdatenbank_steht_auf_der_Zielversion()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;              // keine Datei - nichts zu pruefen

            LfsZeigerProbe.Sicherstellen(pfad);

            int stand = SchemaStandLesen(pfad);

            Assert.True(stand == SchemaStand.Zielversion,
                "Die Testdatenbank " + pfad + " steht auf Schemastand " + stand +
                ", SchemaStand.Zielversion ist " + SchemaStand.Zielversion + ". " +
                "Wer einen Migrationsschritt anlegt, zieht die Repo-Datei im selben " +
                "Schritt nach (Werkzeug Testdatenbankschema) und aktualisiert " +
                "Referenzlaeufe/LIESMICH.md.");
        }

        /// <summary>
        /// Gegenprobe zu WT-a: Ein Nachbar oeffnet eine Datei gepoolt mit derselben
        /// <c>immutable</c>-Zeichenfolge, danach wird die Datei gehoben — die Wache liest den
        /// gehobenen Stand, nicht den des Nachbarn. Auf einer eigenen kleinen Datei, nie auf
        /// der Repo-Datei.
        /// </summary>
        [Fact]
        public void Die_Wache_liest_den_Stand_der_Platte_auch_nach_einem_gepoolten_Nachbarn()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "SchemastandWache_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            string datei = Path.Combine(ordner, "Kenndaten.sqlite");
            try
            {
                Schreiben(datei, "CREATE TABLE Tab_Applikation (" + ApplikationCtrl.SPALTE_SCHEMAVERSION +
                                 " INTEGER); INSERT INTO Tab_Applikation VALUES (183);");

                // Der Nachbar: dieselbe Zeichenfolge wie die Nachbarklassen, MIT Pool.
                string uri = "file:" + datei.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
                string nachbar = new SqliteConnectionStringBuilder { DataSource = uri }.ToString();
                using (var c = new SqliteConnection(nachbar))
                {
                    c.Open();
                    using SqliteCommand b = c.CreateCommand();
                    b.CommandText = "SELECT " + ApplikationCtrl.SPALTE_SCHEMAVERSION + " FROM Tab_Applikation";
                    Assert.Equal(183L, b.ExecuteScalar());
                }

                Schreiben(datei, "UPDATE Tab_Applikation SET " + ApplikationCtrl.SPALTE_SCHEMAVERSION + " = 185;");

                Assert.Equal(185, SchemaStandLesen(datei));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                try { Directory.Delete(ordner, true); } catch (IOException) { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        private static void Schreiben(string datei, string sql)
        {
            using var c = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = datei,
                Pooling = false
            }.ToString());
            c.Open();
            using SqliteCommand b = c.CreateCommand();
            b.CommandText = sql;
            b.ExecuteNonQuery();
        }

        /// <summary>
        /// Liest <c>Tab_Applikation.SchemaVersion</c> — dieselbe Ablage, die
        /// <c>ApplikationCtrl.GetSchemaVersion</c> und <c>Erstbereitstellung</c> lesen und
        /// <c>TestDatenbank.SchemaNachziehen</c> schreibt. Hier ueber eine EIGENE,
        /// schreibgeschuetzte Verbindung statt ueber <c>DataRepository</c>: Dessen
        /// <c>PfadUeberschreibung</c> ist prozessweiter Zustand, und dieser Fall soll die
        /// REPO-Datei messen, nie eine Arbeitskopie.
        ///
        /// <para><b>Ohne Verbindungspool</b> (Befund WT-a: im breiten Lauf nicht reihenfest).
        /// Rund dreissig Nachbarklassen oeffnen die Repo-Datei mit DERSELBEN Zeichenfolge
        /// (<c>file:…?mode=ro&amp;immutable=1</c>); Microsoft.Data.Sqlite poolt nach der
        /// Zeichenfolge, haelt also nach dem ersten Nachbarn eine offene Verbindung bereit.
        /// Mit <c>immutable=1</c> prueft SQLite die Datei nie wieder auf Aenderungen — eine
        /// gepoolte Verbindung liest den Stand, den die Datei beim ERSTEN Oeffnen hatte,
        /// auch wenn sie danach an Ort und Stelle gehoben oder durch Checkout ersetzt wurde.
        /// Ob die Wache den Stand der Platte oder einen alten las, hing damit davon ab, ob
        /// ein Nachbar vor ihr lief. Ohne Pool liest jedes Oeffnen die Datei frisch.</para>
        /// </summary>
        internal static int SchemaStandLesen(string pfad)
        {
            // Der Pfad geht als URI hinein - Rueckwaertsschraegstriche sind darin
            // Sonderzeichen, und ein '?' im Pfad wuerde die Abfragezeichenfolge
            // anschneiden. Beides ist in diesem Repowurzel-Pfad nicht zu erwarten, die
            // Umschrift kostet aber nichts.
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") +
                         "?mode=ro&immutable=1";

            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = uri,
                Pooling = false
            }.ToString());
            verbindung.Open();

            using SqliteCommand befehl = verbindung.CreateCommand();
            befehl.CommandText = "SELECT " + ApplikationCtrl.SPALTE_SCHEMAVERSION +
                                 " FROM Tab_Applikation LIMIT 1";
            object o = befehl.ExecuteScalar();
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> unter der Repowurzel;
        /// <c>null</c>, wenn es sie nicht gibt. Die Wurzel wird wie in den Nachbarwachen
        /// gefunden: erst ueber den Pfad DIESER Quelldatei, sonst aufwaerts vom Laufordner
        /// bis zu <c>WP-Plan.sln</c>.
        /// </summary>
        private static string Testdatenbank(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            string wurzel = null;

            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln")))
                    wurzel = kandidat;
            }

            if (wurzel == null)
            {
                DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null && wurzel == null)
                {
                    if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) wurzel = d.FullName;
                    d = d.Parent;
                }
            }

            if (wurzel == null) return null;

            string datei = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            return File.Exists(datei) ? datei : null;
        }
    }
}
