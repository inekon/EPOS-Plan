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
    /// Der Schemaschritt <b>„Brennwertkennzeichen der Projektkessel nachziehen“</b> (Konzept
    /// Kesselkennlinie, Etappe E2b; Anwenderentscheid B-1 vom 30.09.2026; Nummer bei
    /// <see cref="KesselBrennwertNachzug"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter der Saat der Konditionierungsvorlagen)
    /// und ihr Eintrag im Register der Paketanhebung als Umformung; die Regel ohne Datenbank (Katalog
    /// einig, widersprüchlich mit Leistung, mehrdeutig, ohne Katalogsatz nach der Beschreibung); der
    /// Schritt an der Testdatenbank aus dem Stand davor — welche Kopien er setzt, dass er nie löscht,
    /// dass er wiederholbar ist und was ohne Zuordnung bleibt; die Repo-Datei trägt ihn; Migration,
    /// Werkzeug und Testvorrichtung führen ihn aus derselben Quelle.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselBrennwertNachzugTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Elektrokessel der Testdatenbank (1017, 1024, 1047) — ihr Katalogsatz ist keiner.</summary>
        private static readonly int[] ELEKTROKESSEL = { 1017237, 1018320, 1018344 };

        /// <summary>Die Projektkopie ohne Katalogsatz („… (2)“ in 1009), Beschreibung „Brennwert-Kombi-Kessel“.</summary>
        private const int OHNE_KATALOG = 1018328;

        // =============================================================================
        //  Teil 1 - Nummer, Register und die Regel (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_die_Saat_der_Konditionierungsvorlagen()
        {
            Assert.Equal(KonditionierungsvorlagenSaatSchema.SCHRITT + 1, KesselBrennwertNachzug.SCHRITT);
            Assert.Equal(158, KesselBrennwertNachzug.SCHRITT);
            Assert.Equal(KesselBrennwertNachzug.SCHRITT + 1, KostenStempelSchema.SCHRITT);
        }

        [Fact]
        public void Das_Register_der_Paketanhebung_fuehrt_den_Schritt_als_Umformung()
        {
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KesselBrennwertNachzug.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Umformung, s.Wirkung);
            Assert.NotNull(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
        }

        private static KesselBrennwertNachzug.Katalogsatz K(int id, bool brennwert, double? ptherm)
            => new KesselBrennwertNachzug.Katalogsatz(id, brennwert, ptherm);

        [Fact]
        public void Ein_einiger_Katalog_entscheidet()
        {
            var ja = KesselBrennwertNachzug.Entscheiden(new[] { K(1, true, 22.1) }, 22.1, "Zentralheizung");
            Assert.True(ja.Setzen);
            Assert.Equal(KesselBrennwertNachzug.Quelle.Katalog, ja.Herkunft);

            // Der Katalogsatz geht vor der Beschreibung der Kopie - auch wenn die „Brennwert" sagt.
            var nein = KesselBrennwertNachzug.Entscheiden(new[] { K(1, false, 28), K(2, false, 10) }, 28, "Brennwert-Kessel");
            Assert.False(nein.Setzen);
            Assert.Equal(KesselBrennwertNachzug.Quelle.Katalog, nein.Herkunft);
            Assert.False(nein.KatalogMehrdeutig);

            // Die Nennleistung der Kopie zählt nicht, solange der Katalog einig ist (1030: 2 200 kW
            // gegen 80 kW im Katalog).
            Assert.True(KesselBrennwertNachzug.Entscheiden(new[] { K(1, true, 80) }, 2200, null).Setzen);
        }

        [Fact]
        public void Ein_widerspruechlicher_Katalog_entscheidet_nach_der_Leistung()
        {
            var saetze = new[] { K(1, true, 22.1), K(2, false, 28.0) };
            var e = KesselBrennwertNachzug.Entscheiden(saetze, 22.12, "Zentralheizung");
            Assert.True(e.Setzen);
            Assert.Equal(KesselBrennwertNachzug.Quelle.Katalog, e.Herkunft);

            Assert.False(KesselBrennwertNachzug.Entscheiden(saetze, 28.0, "Brennwert-Kessel").Setzen);
        }

        [Fact]
        public void Ohne_eindeutigen_Katalogsatz_gilt_die_Beschreibung()
        {
            // Kein Katalogsatz: die Bauart der Beschreibung, wie beim Import.
            var bw = KesselBrennwertNachzug.Entscheiden(Array.Empty<KesselBrennwertNachzug.Katalogsatz>(), 22.1,
                                                        "Brennwert-Kombi-Kessel");
            Assert.True(bw.Setzen);
            Assert.Equal(KesselBrennwertNachzug.Quelle.Beschreibung, bw.Herkunft);
            Assert.False(bw.KatalogMehrdeutig);
            Assert.False(KesselBrennwertNachzug.Entscheiden(null, 22.1, "Niedertemperatur-Kessel").Setzen);
            Assert.False(KesselBrennwertNachzug.Entscheiden(null, null, null).Setzen);

            // Widersprüchlich, und die Leistung trennt nicht: mehrdeutig, dann die Beschreibung.
            var saetze = new[] { K(1, true, 22.1), K(2, false, 22.1) };
            var m = KesselBrennwertNachzug.Entscheiden(saetze, 22.1, "brennwertkessel");
            Assert.True(m.Setzen);
            Assert.True(m.KatalogMehrdeutig);
            Assert.Equal(KesselBrennwertNachzug.Quelle.Beschreibung, m.Herkunft);
            Assert.False(KesselBrennwertNachzug.Entscheiden(saetze, null, "Kessel").Setzen);
        }

        [Fact]
        public void Ein_Katalogsatz_ist_Brennwertkessel_nach_Schalter_oder_Bauart()
        {
            Assert.True(KesselBrennwertNachzug.IstBrennwertkessel(true, "Zentralheizung"));
            Assert.True(KesselBrennwertNachzug.IstBrennwertkessel(false, "Brennwert-Kessel"));
            Assert.False(KesselBrennwertNachzug.IstBrennwertkessel(false, "Niedertemperatur-Kessel"));
            Assert.False(KesselBrennwertNachzug.IstBrennwertkessel(false, null));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank
        // =============================================================================

        /// <summary>
        /// Aus dem Stand davor (jede Projektkopie ohne Kennzeichen): 23 Kopien bekommen das Kennzeichen
        /// nach ihrem Katalogsatz (darin die Kopie des Kessels von 1018 im Zonenprojekt 1052, G6d), die Kopie ohne Katalogsatz nach ihrer Beschreibung; die drei
        /// Elektrokessel bleiben (ihr Katalogsatz ist keiner). Ein zweiter Lauf setzt nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KesselBrennwertNachzug.Vollstaendig(), "Die Testvorrichtung hat den Schritt schon gezogen.");
            long kopien = Zahl("SELECT COUNT(*) FROM Tab_Heizkessel");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Heizkessel SET Brennwert = 0");
            Assert.False(KesselBrennwertNachzug.Vollstaendig());

            var zeilen = new List<string>();
            KesselBrennwertNachzug.Bericht b = KesselBrennwertNachzug.Ausfuehren(zeilen);
            Assert.Equal(kopien, b.Geprueft);
            Assert.Equal(23, b.AusKatalog.Count);
            Assert.Single(b.AusBeschreibung);
            Assert.Contains("Kessel " + OHNE_KATALOG, b.AusBeschreibung[0]);
            Assert.Empty(b.OhneZuordnung);
            Assert.Empty(b.Mehrdeutig);
            Assert.Equal(24, b.Gesetzt);
            Assert.StartsWith("24 von " + kopien, zeilen[0]);
            Assert.Contains(zeilen, z => z.Contains(OHNE_KATALOG.ToString(CultureInfo.InvariantCulture)));

            Assert.Equal(kopien - ELEKTROKESSEL.Length, Zahl("SELECT COUNT(*) FROM Tab_Heizkessel WHERE Brennwert = 1"));
            foreach (int id in ELEKTROKESSEL)
                Assert.Equal(0L, Zahl("SELECT Brennwert FROM Tab_Heizkessel WHERE ID = " + id));
            Assert.True(KesselBrennwertNachzug.Vollstaendig());

            KesselBrennwertNachzug.Bericht zweiter = KesselBrennwertNachzug.Ausfuehren(null);
            Assert.Equal(0, zweiter.Gesetzt);
            Assert.Equal(ELEKTROKESSEL.Length, zweiter.Geprueft);
        }

        /// <summary>Nur setzen, nie löschen: Ein stehender Schalter bleibt, auch gegen den Katalog.</summary>
        [Fact]
        public void Der_Schritt_loescht_nie_ein_Kennzeichen()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("UPDATE Tab_Heizkessel SET Brennwert = 1 WHERE ID = " + ELEKTROKESSEL[0]);
            KesselBrennwertNachzug.Bericht b = KesselBrennwertNachzug.Ausfuehren(null);
            Assert.Equal(0, b.Gesetzt);
            Assert.Equal(1L, Zahl("SELECT Brennwert FROM Tab_Heizkessel WHERE ID = " + ELEKTROKESSEL[0]));
        }

        /// <summary>
        /// Umbenannte Kopien ohne Katalogsatz: Eine mit „Brennwert“ in der Beschreibung bekommt das
        /// Kennzeichen nach ihr, eine ohne bleibt ohne und steht benannt im Bericht. (Widersprüchliche
        /// Katalogsätze unter einem Bezeichner kann der Katalog seit seinem eindeutigen Index nicht mehr
        /// führen; die Regel dafür hält <see cref="Ohne_eindeutigen_Katalogsatz_gilt_die_Beschreibung"/>.)
        /// </summary>
        [Fact]
        public void Ohne_Zuordnung_steht_benannt_im_Bericht()
        {
            if (!_db.Vorhanden) return;

            // 1024: Elektrokessel „eloBLOCK VE 10" - umbenannt, Beschreibung „Zentralheizung".
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Heizkessel SET Bezeichner = ?, Brennwert = 0 WHERE ID = ?",
                                                  new DbParam("@b", "Kessel ohne Katalog"), new DbParam("@id", 1018320)));
            // 1007: umbenannt, Beschreibung „Brennwert-Kombi-Kessel".
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Heizkessel SET Bezeichner = ?, Brennwert = 0 WHERE ID = ?",
                                                  new DbParam("@b", "Eigener Kessel"), new DbParam("@id", 1007239)));

            var zeilen = new List<string>();
            KesselBrennwertNachzug.Bericht b = KesselBrennwertNachzug.Ausfuehren(zeilen);

            Assert.Contains(b.OhneZuordnung, s => s.Contains("Kessel ohne Katalog") && s.Contains("Zentralheizung"));
            Assert.Contains(b.AusBeschreibung, s => s.Contains("Kessel 1007239") && s.Contains("Eigener Kessel"));
            Assert.Empty(b.Mehrdeutig);
            Assert.Equal(1L, Zahl("SELECT Brennwert FROM Tab_Heizkessel WHERE ID = 1007239"));
            Assert.Equal(0L, Zahl("SELECT Brennwert FROM Tab_Heizkessel WHERE ID = 1018320"));
            Assert.Contains(zeilen, z => z.StartsWith("ohne Zuordnung", StringComparison.Ordinal) && z.Contains("Kessel ohne Katalog"));
            Assert.Contains(zeilen, z => z.StartsWith("nach der Beschreibung", StringComparison.Ordinal) && z.Contains("1007239"));
        }

        /// <summary>
        /// <b>Die Repo-Datei trägt den Schritt:</b> Schemastand, jede Brennstoff-Projektkopie der
        /// Referenzprojekte als Brennwertkessel gekennzeichnet, die Elektrokessel nicht, und der Schritt
        /// hätte nichts mehr zu tun (Einfrierregel „gesäte Kesseldaten“).
        /// </summary>
        [Fact]
        public void Die_Repo_Datei_traegt_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad) || new FileInfo(pfad).Length < 1_000_000) return;       // LFS-Zeiger

            using var verbindung = new SqliteConnection("Data Source=" + pfad + ";Mode=ReadOnly");
            verbindung.Open();
            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= KesselBrennwertNachzug.SCHRITT);
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Heizkessel WHERE Brennwert = 0 AND Brennstoff <> 13"));
            Assert.Equal(3L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Heizkessel WHERE Brennwert = 0 AND Brennstoff = 13"));
            // Der Schalter der Brennwertkennlinie bleibt allein an 1050 (Konzept 3.1).
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Heizkessel WHERE Kennlinie_Brennwert = 1"));
        }

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und die
        /// Testvorrichtung führen den Schritt aus derselben Quelle, jeweils hinter der Saat der
        /// Konditionierungsvorlagen.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("KesselBrennwertNachzug.Ausfuehren(", werkzeug);
            Assert.True(werkzeug.IndexOf("KesselBrennwertNachzug.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("KonditionierungsvorlagenSaatSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KESSEL_BRENNWERT_NACHZUG = KesselBrennwertNachzug.SCHRITT", migration);
            Assert.Contains("new Schritt(SCHRITT_KESSEL_BRENNWERT_NACHZUG", migration);
            Assert.Contains("KesselBrennwertNachzug.Ausfuehren(zeilen)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("KesselBrennwertNachzug.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("KonditionierungsvorlagenSaatSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
