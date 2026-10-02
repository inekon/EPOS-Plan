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
    /// <b>Der Bereitschaftsverlust des Heizkessels in kW oder % der Nennleistung</b>
    /// (Anwenderentscheid vom 02.10.2026; Schritt bei <see cref="KesselBereitschaftEinheitSchema"/>,
    /// Rechenregel bei <see cref="KesselBereitschaft"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Umrechnung in kW (einmal im Kern), die Prüfgrenzen je Einheit,
    /// dass der Bestand kW bleibt (Vorgabe der Spalte, Lesen ohne Spalte), der Schritt aus dem Stand
    /// davor und wiederholbar, die Wege Katalog → Projektkopie, Editor, Katalogbrowser und Import, und
    /// dass Migration, Werkzeug und Testkopie den Schritt aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselBereitschaftEinheitTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const string KW = DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW;
        private const string PROZENT = DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT;

        // =============================================================================
        //  Teil 1 - Rechenregel und Grenzen (ohne Datenbank)
        // =============================================================================

        /// <summary>kW bleibt der Wert, Prozent wird mit der Nennleistung zu kW; leer und negativ = 0.</summary>
        [Fact]
        public void Die_Umrechnung_in_kW()
        {
            Assert.Equal(0.075, KesselBereitschaft.LeistungKw(0.075, KW, 22));
            Assert.Equal(0.11, KesselBereitschaft.LeistungKw(0.5, PROZENT, 22), 12);
            Assert.Equal(1.0, KesselBereitschaft.LeistungKw(1.0, PROZENT, 100), 12);

            Assert.Equal(0, KesselBereitschaft.LeistungKw(0, PROZENT, 22));
            Assert.Equal(0, KesselBereitschaft.LeistungKw(-1, KW, 22));
            Assert.Equal(0, KesselBereitschaft.LeistungKw(double.NaN, KW, 22));
            Assert.Equal(0, KesselBereitschaft.LeistungKw(0.5, PROZENT, 0));   // ohne Nennleistung kein Anteil

            // Der Rechenweg des Laufs ruft dieselbe Regel; die Altüberladung bleibt kW.
            Assert.Equal(0.11, SimulationSPK.BereitschaftsleistungKw(0.5, PROZENT, 22), 12);
            Assert.Equal(0.075, SimulationSPK.BereitschaftsleistungKw(0.075));
        }

        /// <summary>Ein Text außer „%" ist kW — leer, NULL und jede andere Schreibweise.</summary>
        [Fact]
        public void Bestand_und_Unbekanntes_ist_kW()
        {
            Assert.Equal(KW, KesselBereitschaft.Einheit(null));
            Assert.Equal(KW, KesselBereitschaft.Einheit(""));
            Assert.Equal(KW, KesselBereitschaft.Einheit("kW"));
            Assert.Equal(KW, KesselBereitschaft.Einheit("KW "));
            Assert.Equal(PROZENT, KesselBereitschaft.Einheit(" % "));
            Assert.True(KesselBereitschaft.IstProzent("%"));
            Assert.False(KesselBereitschaft.IstProzent(null));
            Assert.Equal(KW, new HeizkesselModel().Bereitschaft_Einheit);
            Assert.Equal(new[] { KW, PROZENT }, KesselBereitschaft.EINHEITEN);
        }

        /// <summary>kW: 0 … Nennleistung (ohne Nennleistung nur nicht negativ), %: 0 … 100.</summary>
        [Fact]
        public void Die_Grenzen_je_Einheit()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Null(KesselBereitschaft.Verstoss(0, KW, 22, "BB"));
            Assert.Null(KesselBereitschaft.Verstoss(22, KW, 22, "BB"));
            Assert.Contains("22", KesselBereitschaft.Verstoss(22.5, KW, 22, "BB"));
            Assert.NotNull(KesselBereitschaft.Verstoss(-0.1, KW, 22, "BB"));
            Assert.Null(KesselBereitschaft.Verstoss(500, KW, null, "BB"));          // ohne Nennleistung
            Assert.NotNull(KesselBereitschaft.Verstoss(-1, KW, null, "BB"));

            Assert.Null(KesselBereitschaft.Verstoss(100, PROZENT, 22, "BB"));
            Assert.Null(KesselBereitschaft.Verstoss(50, PROZENT, null, "BB"));
            string grund = KesselBereitschaft.Verstoss(100.5, PROZENT, 22, "BB");
            Assert.Contains("BB", grund);
            Assert.Contains("100", grund);
            Assert.NotNull(KesselBereitschaft.Verstoss(double.NaN, PROZENT, 22, "BB"));
        }

        /// <summary>
        /// Die Spalte an einer STRICT-Tabelle: Vorgabe kW für Bestandszeilen und neue Zeilen, nur kW
        /// und % zulässig, nicht NULL.
        /// </summary>
        [Fact]
        public void Die_Spalte_hat_Vorgabe_kW_und_Pruefklausel()
        {
            Assert.Equal(AufheizErgebnisSchema.SCHRITT + 1, KesselBereitschaftEinheitSchema.SCHRITT);
            Assert.Equal(162, KesselBereitschaftEinheitSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KesselBereitschaftEinheitSchema.SCHRITT);
            Assert.Equal(new[] { "Tab_Heizkessel_STAMM", "Tab_Heizkessel" }, KesselBereitschaftEinheitSchema.TABELLEN);

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KesselBereitschaftEinheitSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);

            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + KesselBereitschaftEinheitSchema.SPALTE + "\" " +
                          KesselBereitschaftEinheitSchema.TYP);
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (2)");
            Assert.Equal(2L, Skalar(c, "SELECT COUNT(*) FROM T WHERE Bereitschaft_Einheit = 'kW'"));
            Assert.False(Wirft(c, "UPDATE T SET Bereitschaft_Einheit = '%' WHERE ID = 2"));
            Assert.True(Wirft(c, "UPDATE T SET Bereitschaft_Einheit = 'W'"), "Nur kW und % sind zulässig.");
            Assert.True(Wirft(c, "UPDATE T SET Bereitschaft_Einheit = NULL"), "Die Spalte ist NOT NULL.");
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die Spalte wird entfernt; der Schritt legt beide an, jede
        /// Zeile steht auf kW, die übrigen Werte bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar_und_laesst_den_Bestand_auf_kW()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KesselBereitschaftEinheitSchema.Vollstaendig());
            Assert.Empty(KesselBereitschaftEinheitSchema.Anweisungen);
            List<string> vorher = Bestand();

            foreach (string tabelle in KesselBereitschaftEinheitSchema.TABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" +
                                               KesselBereitschaftEinheitSchema.SPALTE + "\"");
            Assert.False(KesselBereitschaftEinheitSchema.Vollstaendig());
            Assert.Equal(2, KesselBereitschaftEinheitSchema.Anweisungen.Count());

            // Ohne Spalte lesen die Controller kW.
            var ohne = new HeizkesselCtrl();
            ohne.ReadAll();
            Assert.NotEmpty(ohne.items);
            Assert.All(ohne.items, k => Assert.Equal(KW, k.Bereitschaft_Einheit));

            var bericht = new List<string>();
            Assert.Equal(2, KesselBereitschaftEinheitSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Heizkessel_STAMM.Bereitschaft_Einheit anlegen"));
            Assert.True(KesselBereitschaftEinheitSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            foreach (string tabelle in KesselBereitschaftEinheitSchema.TABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE Bereitschaft_Einheit <> 'kW'"));

            var zweiter = new List<string>();
            Assert.Equal(0, KesselBereitschaftEinheitSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        // =============================================================================
        //  Teil 3 - die Wege
        // =============================================================================

        /// <summary>
        /// Editor und Projektkopie: Ein in Prozent angelegter Katalogsatz trägt die Einheit in die
        /// Projektkopie, und der Lauf liest dort Wert UND Einheit.
        /// </summary>
        [Fact]
        public void Prozent_im_Katalog_geht_in_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var m = new HeizkesselModel
            {
                Name = "Probe Bereitschaft %", Ptherm = 40, Wirkungsgrad_Gas = 0.95, Brennstoff = 1,
                Betriebsbereitschaftverlust = 0.5, Bereitschaft_Einheit = PROZENT
            };
            HeizkesselStammCtrl.SpeicherErgebnis e = HeizkesselStammCtrl.Anlegen(m, m.Name);
            Assert.True(e.Ok, e.Meldung);

            var stamm = new HeizkesselStammCtrl();
            stamm.ReadSingle(m.Name);
            Assert.Equal(PROZENT, stamm.Bereitschaft_Einheit);
            Assert.Equal(0.5, stamm.Betriebsbereitschaftverlust);

            int kopie = new HeizkesselCtrl().CopyFromStamm(stamm.ID, 1007);
            Assert.True(kopie > 0);
            Assert.Equal(PROZENT, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bereitschaft_Einheit FROM Tab_Heizkessel WHERE ID = ?", new DbParam("?", kopie)),
                CultureInfo.InvariantCulture));

            var projekt = new HeizkesselCtrl();
            projekt.ReadAll();
            HeizkesselModel k = projekt.items.Single(x => x.ID == kopie);
            Assert.Equal(0.2, KesselBereitschaft.LeistungKw(k.Betriebsbereitschaftverlust,
                                                            k.Bereitschaft_Einheit, k.Ptherm), 12);
        }

        /// <summary>Der Editor lehnt Werte außerhalb der Grenzen ihrer Einheit benannt ab.</summary>
        [Fact]
        public void Der_Editor_prueft_die_Grenzen_je_Einheit()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var prozent = new HeizkesselModel
            {
                Name = "Probe 120 %", Ptherm = 40, Brennstoff = 1,
                Betriebsbereitschaftverlust = 120, Bereitschaft_Einheit = PROZENT
            };
            HeizkesselStammCtrl.SpeicherErgebnis e = HeizkesselStammCtrl.Anlegen(prozent, prozent.Name);
            Assert.False(e.Ok);
            Assert.Contains("100", e.Meldung);

            var kw = new HeizkesselModel
            {
                Name = "Probe 50 kW", Ptherm = 40, Brennstoff = 1,
                Betriebsbereitschaftverlust = 50, Bereitschaft_Einheit = KW
            };
            e = HeizkesselStammCtrl.Anlegen(kw, kw.Name);
            Assert.False(e.Ok);
            Assert.Contains("40", e.Meldung);

            kw.Bereitschaft_Einheit = PROZENT;          // 50 % von 40 kW sind zulässig
            e = HeizkesselStammCtrl.Anlegen(kw, kw.Name);
            Assert.True(e.Ok, e.Meldung);
        }

        /// <summary>
        /// Der Katalogbrowser zeigt und schreibt die Einheit als Text („kW"/„%"); eine unbekannte wird
        /// benannt abgelehnt, ein leeres Feld lässt sie stehen, und die Grenze folgt der Einheit.
        /// </summary>
        [Fact]
        public void Der_Katalogbrowser_fuehrt_die_Einheit()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            string kessel = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Heizkessel_STAMM GROUP BY Bezeichner HAVING COUNT(*) = 1 ORDER BY MIN(ID) LIMIT 1"),
                CultureInfo.InvariantCulture);
            Assert.False(string.IsNullOrEmpty(kessel));
            Assert.Equal(KW, new HeizkesselStammCtrl().KatalogsatzAnzeige(kessel)[KatalogBrowserProfil.FeldBBEinheit]);

            HeizkesselStammCtrl.AnzeigefelderHeizkessel Satz(double? bb, string einheit)
                => new HeizkesselStammCtrl.AnzeigefelderHeizkessel(
                    "Probe", 30, 100, false, 70, 50, Betriebsbereitschaftverlust: bb, BereitschaftEinheit: einheit);

            var ok = HeizkesselStammCtrl.AnzeigefelderSchreiben(kessel, Satz(0.4, "%"));
            Assert.True(ok.Ok, ok.Meldung);
            var nachher = new HeizkesselStammCtrl().KatalogsatzAnzeige(kessel);
            Assert.Equal(PROZENT, nachher[KatalogBrowserProfil.FeldBBEinheit]);
            Assert.Equal("0,4", nachher[KatalogBrowserProfil.FeldBBVerlust]);

            // Leer = stehen lassen.
            Assert.True(HeizkesselStammCtrl.AnzeigefelderSchreiben(kessel, Satz(null, "")).Ok);
            Assert.Equal(PROZENT, new HeizkesselStammCtrl().KatalogsatzAnzeige(kessel)[KatalogBrowserProfil.FeldBBEinheit]);

            var unbekannt = HeizkesselStammCtrl.AnzeigefelderSchreiben(kessel, Satz(0.4, "W"));
            Assert.False(unbekannt.Ok);
            Assert.Contains("W", unbekannt.Meldung);

            var ueber = HeizkesselStammCtrl.AnzeigefelderSchreiben(kessel, Satz(101, null));   // 101 % bei gespeichertem %
            Assert.False(ueber.Ok);
            Assert.Contains("100", ueber.Meldung);

            var kwUeber = HeizkesselStammCtrl.AnzeigefelderSchreiben(kessel, Satz(31, "kW"));  // über der Nennleistung 30
            Assert.False(kwUeber.Ok);
            Assert.Contains("30", kwUeber.Meldung);

            Assert.True(HeizkesselStammCtrl.AnzeigefelderSchreiben(kessel, Satz(0.1, "kW")).Ok);
            Assert.Equal(KW, new HeizkesselStammCtrl().KatalogsatzAnzeige(kessel)[KatalogBrowserProfil.FeldBBEinheit]);
        }

        /// <summary>
        /// Der Import liefert kW — auch beim Überschreiben eines in Prozent gepflegten Satzes kommt
        /// mit dem kW-Wert der Datei die Einheit kW zurück.
        /// </summary>
        [Fact]
        public void Der_Import_setzt_die_Einheit_kW()
        {
            if (!_db.Vorhanden) return;

            int id = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Heizkessel_STAMM"),
                                     CultureInfo.InvariantCulture);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel_STAMM SET Bereitschaft_Einheit = '%' WHERE ID = ?", new DbParam("?", id)));

            var stamm = new HeizkesselStammCtrl { Ptherm = 20, Brennstoff = 1, Betriebsbereitschaftverlust = 0.08 };
            Assert.Equal(KW, stamm.Bereitschaft_Einheit);
            Assert.True(stamm.UpdateImport(id));
            Assert.Equal(KW, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bereitschaft_Einheit FROM Tab_Heizkessel_STAMM WHERE ID = ?", new DbParam("?", id)),
                CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter den Änderungsstempeln.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("KesselBereitschaftEinheitSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("AufheizErgebnisSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KESSEL_BEREITSCHAFT_EINHEIT = KesselBereitschaftEinheitSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_KESSEL_BEREITSCHAFT_EINHEIT", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_AUFHEIZ_ERGEBNIS", StringComparison.Ordinal));
            Assert.Contains("KesselBereitschaftEinheitSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("KesselBereitschaftEinheitSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("AufheizErgebnisSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilen beider Kesseltabellen ohne die neue Spalte — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string tabelle in KesselBereitschaftEinheitSchema.TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Ptherm, Betriebsbereitschaftverlust, Brennwert FROM \"" + tabelle +
                    "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(tabelle + "|" + string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
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

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
