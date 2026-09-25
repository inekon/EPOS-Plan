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
    /// Der Schemaschritt der <b>Nachtzeit</b> (Entscheid E43, Konzept-Nachtrag N1.48; Nummer bei
    /// <see cref="NachtzeitSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter 143), die Definition der zwei Spalten
    /// samt Bereichsprüfung und der sechste Sichtneubau; der Stand der Testdatenbank (die Spalten
    /// stehen an beiden Gebäudetabellen, sind überall leer, die Sicht ist die geltende); der Schritt aus
    /// dem Stand davor, wiederholbar; <b>die vier fest verdrahteten Kopierwege</b> von
    /// <c>GebaeudeStammCtrl</c> samt Namensleser NULL-erhaltend; die Wege über die ganze Zeile (Katalog
    /// duplizieren, Projektduplikat, Projekttransfer); Repo-Datei, Werkzeug und Migration führen den
    /// Schritt zuletzt.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class NachtzeitSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt (1007) mit genau einem Gebäude.</summary>
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        /// <summary>Eine erfundene Nachtzeit über Mitternacht für die Proben.</summary>
        private const int BEGINN = 23, ENDE = 5;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf 143; der Zielstand reicht bis zu ihr.</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_143()
        {
            Assert.Equal(BaustoffQuellenBerichtigung.SCHRITT + 1, NachtzeitSchema.SCHRITT);
            Assert.Equal(144, NachtzeitSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= NachtzeitSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + NachtzeitSchema.SCHRITT + ".");
        }

        /// <summary>
        /// Die Spalten: <c>INTEGER</c>, nullbar, <c>CHECK</c> 0 … 23 — wortgleich mit dem Auftrag; dieselben
        /// Grenzen wie die Prüfregel <see cref="Nachtzeit.Pruefen"/>.
        /// </summary>
        [Fact]
        public void Die_Spalten_sind_nullbare_Stunden_von_0_bis_23()
        {
            Assert.Equal(new[] { "Nachtabsenkung_Beginn", "Nachtabsenkung_Ende" }, GebaeudeSchema.NACHTZEIT_SPALTEN);
            Assert.Equal("INTEGER CHECK (Nachtabsenkung_Beginn IS NULL OR Nachtabsenkung_Beginn BETWEEN 0 AND 23)",
                         GebaeudeSchema.SqliteNachtstunde(GebaeudeSchema.SPALTE_NACHTABSENKUNG_BEGINN));
            Assert.Equal("INTEGER CHECK (Nachtabsenkung_Ende IS NULL OR Nachtabsenkung_Ende BETWEEN 0 AND 23)",
                         GebaeudeSchema.SqliteNachtstunde(GebaeudeSchema.SPALTE_NACHTABSENKUNG_ENDE));
            Assert.Equal(GebaeudeSchema.NACHTSTUNDE_MIN, Nachtzeit.STUNDE_MIN);
            Assert.Equal(GebaeudeSchema.NACHTSTUNDE_MAX, Nachtzeit.STUNDE_MAX);
            foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                Assert.DoesNotContain(s, GebaeudeSchema.SICHT_BAUJAHR);

            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                Ausfuehren(c, GebaeudeSchema.NachtstundeAnlegen("T", s));
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            Assert.True(Leer(c, "SELECT Nachtabsenkung_Beginn FROM T WHERE ID = 1"), "Ohne Wert ist der Beginn NULL.");
            Assert.True(Leer(c, "SELECT Nachtabsenkung_Ende FROM T WHERE ID = 1"), "Ohne Wert ist das Ende NULL.");
            foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
            {
                foreach (string gut in new[] { "0", "23", "6", "22", "NULL" })
                    Assert.False(Wirft(c, "UPDATE T SET " + s + " = " + gut), s + " = " + gut);
                foreach (string schlecht in new[] { "-1", "24", "'22 Uhr'", "5.5" })
                    Assert.True(Wirft(c, "UPDATE T SET " + s + " = " + schlecht), s + " = " + schlecht);
            }
        }

        /// <summary>
        /// Der sechste Sichtneubau hängt Beginn und Ende HINTER die 99 Spalten des Baujahrs (Stellen 99
        /// und 100) — mit den acht Spalten der Kühlübergabe und dem Baujahr an ihren Stellen; die
        /// Bauvorschrift ist dieselbe, und er ist die GELTENDE Sicht.
        /// </summary>
        [Fact]
        public void Der_sechste_Sichtneubau_haengt_die_Nachtzeit_hinter_das_Baujahr()
        {
            Assert.Equal(101, GebaeudeSchema.SICHT_NACHTZEIT.Length);
            Assert.Equal(GebaeudeSchema.SICHT_BAUJAHR, GebaeudeSchema.SICHT_NACHTZEIT.Take(99));
            Assert.Equal("Baujahr", GebaeudeSchema.SICHT_NACHTZEIT[98]);
            Assert.Equal("Nachtabsenkung_Beginn", GebaeudeSchema.SICHT_NACHTZEIT[99]);
            Assert.Equal("Nachtabsenkung_Ende", GebaeudeSchema.SICHT_NACHTZEIT[100]);
            foreach (string s in GebaeudeSchema.KUEHLUEBERGABE_SPALTEN.Select(k => k.Key))
                Assert.Contains("Tab_Gebaeude." + s, GebaeudeSchema.SQL_VIEW_NACHTZEIT, StringComparison.Ordinal);
            Assert.Equal(GebaeudeSchema.SichtSql(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key)
                                                 .Concat(GebaeudeSchema.KUEHL_SPALTEN.Select(s => s.Key))
                                                 .Concat(GebaeudeSchema.UEBERGABE_SPALTEN.Select(s => s.Key))
                                                 .Concat(GebaeudeSchema.KUEHLUEBERGABE_SPALTEN.Select(s => s.Key))
                                                 .Concat(new[] { "Baujahr", "Nachtabsenkung_Beginn", "Nachtabsenkung_Ende" })),
                         GebaeudeSchema.SQL_VIEW_NACHTZEIT);
            Assert.DoesNotContain("Tab_Gebaeude.Nachtabsenkung_Beginn", GebaeudeSchema.SQL_VIEW_BAUJAHR, StringComparison.Ordinal);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_NACHTZEIT, GebaeudeSchema.SQL_VIEW_AKTUELL);
            Assert.Equal(GebaeudeSchema.SICHT_NACHTZEIT, GebaeudeSchema.SICHT_AKTUELL);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>Die Spalten stehen an beiden Gebäudetabellen, sind überall leer, die Sicht ist die geltende; STRICT bleibt.</summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_und_die_Nachtzeit_ist_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= NachtzeitSchema.SCHRITT);
            Assert.True(NachtzeitSchema.Vollstaendig());
            Assert.True(BaujahrSchema.Vollstaendig());
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());

            string sicht = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, sicht);
            Assert.Equal(GebaeudeSchema.SICHT_NACHTZEIT, GebaeudeSchema.SichtSpalten());

            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.True(Zahl("SELECT COUNT(*) FROM [" + t + "]") > 0, t);
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
                foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                {
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + t + "] WHERE " + s + " IS NOT NULL"));
                    Assert.Contains(GebaeudeSchema.SqliteNachtstunde(s), ddl, StringComparison.Ordinal);
                }
            }
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm (143): Sicht und Spalten werden entfernt, die Sicht des
        /// Baujahrs steht; der Schritt legt die vier Spalten an und baut die Sicht neu — die Bestandswerte
        /// bleiben, Baujahr und Kühlübergabe stehen weiter, und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();

            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (string t in GebaeudeSchema.TABELLEN)
                foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"" + s + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_BAUJAHR);    // der Stand nach 139 bis 143
            Assert.True(BaujahrSchema.Vollstaendig());
            Assert.False(NachtzeitSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(4, NachtzeitSchema.Alle(bericht));
            Assert.Contains(bericht, z => z.Contains("4 von 4 Spalte(n) der Nachtzeit angelegt"));
            Assert.Contains(bericht, z => z.Contains("(101 Spalten)"));
            Assert.True(NachtzeitSchema.Vollstaendig());
            Assert.True(BaujahrSchema.Vollstaendig());
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Nachtabsenkung_Beginn IS NOT NULL " +
                                  "OR Nachtabsenkung_Ende IS NOT NULL"));

            Assert.Equal(0, NachtzeitSchema.Alle(null));
            Assert.Equal(GebaeudeSchema.SICHT_NACHTZEIT, GebaeudeSchema.SichtSpalten());
        }

        /// <summary>Die Prüfung der Spalten greift an beiden Tabellen; NULL und eine Stunde im Tag gehen durch.</summary>
        [Fact]
        public void Die_Pruefung_der_Spalten_weist_ungueltige_Stunden_ab()
        {
            if (!_db.Vorhanden) return;

            foreach (string t in GebaeudeSchema.TABELLEN)
                foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                {
                    Assert.True(Wirft("UPDATE [" + t + "] SET " + s + " = 24"), t + "." + s);
                    Assert.True(Wirft("UPDATE [" + t + "] SET " + s + " = -1"), t + "." + s);
                    Assert.True(Wirft("UPDATE [" + t + "] SET " + s + " = '22 Uhr'"), t + "." + s);
                    Assert.True(DataRepository.ExecuteSQL("UPDATE [" + t + "] SET " + s + " = ?", new DbParam("?", 0)), t);
                    Assert.True(DataRepository.ExecuteSQL("UPDATE [" + t + "] SET " + s + " = NULL"), t);
                }
        }

        // =============================================================================
        //  Teil 3 - die vier fest verdrahteten Kopierwege von GebaeudeStammCtrl
        // =============================================================================

        /// <summary>KOPIERWEG 1 UND 2 — <c>BuildValueParams</c> und <c>Insert</c>: die Stunden kommen an, NULL bleibt NULL.</summary>
        [Fact]
        public void Kopierweg_1_und_2_BuildValueParams_und_Insert_tragen_die_Nachtzeit()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = "Nachtzeit-Probe mit", Nutzflaeche = 100,
                                                         Nachtabsenkung_Beginn = BEGINN, Nachtabsenkung_Ende = ENDE }));
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = "Nachtzeit-Probe ohne", Nutzflaeche = 100 }));

            DataRow mit = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?",
                                "Nachtzeit-Probe mit");
            Assert.Equal((long)BEGINN, Convert.ToInt64(mit[0], CultureInfo.InvariantCulture));
            Assert.Equal((long)ENDE, Convert.ToInt64(mit[1], CultureInfo.InvariantCulture));
            DataRow ohne = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?",
                                 "Nachtzeit-Probe ohne");
            Assert.Equal(DBNull.Value, ohne[0]);
            Assert.Equal(DBNull.Value, ohne[1]);
            GebaeudeModel gelesen = Lesen("Nachtzeit-Probe mit");
            Assert.Equal(BEGINN, gelesen.Nachtabsenkung_Beginn);
            Assert.Equal(ENDE, gelesen.Nachtabsenkung_Ende);
            Assert.Null(Lesen("Nachtzeit-Probe ohne").Nachtabsenkung_Beginn);
            Assert.Null(Lesen("Nachtzeit-Probe ohne").Nachtabsenkung_Ende);
        }

        /// <summary>KOPIERWEG 3 — <c>Overwrite</c>: eine gesetzte Nachtzeit wird NULL, eine leere bekommt Werte.</summary>
        [Fact]
        public void Kopierweg_3_Overwrite_setzt_und_leert_die_Nachtzeit()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "Nachtzeit-Probe Overwrite";
            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = NAME, Nutzflaeche = 100,
                                                         Nachtabsenkung_Beginn = BEGINN, Nachtabsenkung_Ende = ENDE }));

            GebaeudeModel g = Lesen(NAME);
            g.Nachtabsenkung_Beginn = null;
            g.Nachtabsenkung_Ende = null;
            Assert.True(ctrl.Overwrite(g));
            DataRow z = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME);
            Assert.Equal(DBNull.Value, z[0]);
            Assert.Equal(DBNull.Value, z[1]);
            Assert.Null(Lesen(NAME).Nachtabsenkung_Beginn);

            g = Lesen(NAME);
            g.Nachtabsenkung_Beginn = 0;
            g.Nachtabsenkung_Ende = 6;
            Assert.True(ctrl.Overwrite(g));
            Assert.Equal(0, Lesen(NAME).Nachtabsenkung_Beginn);
            Assert.Equal(6, Lesen(NAME).Nachtabsenkung_Ende);
        }

        /// <summary>KOPIERWEG 4 — <c>CopyFromStamm</c>: die Nachtzeit geht in die Projektkopie; eine leere wird KEIN 0.</summary>
        [Fact]
        public void Kopierweg_4_CopyFromStamm_traegt_die_Nachtzeit_und_haelt_NULL()
        {
            if (!_db.Vorhanden) return;

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idProjekt = Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture);
            int idZuordnung = Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture);
            DataTable stamm = DataRepository.GetDataTable("SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 2");
            int idMit = Convert.ToInt32(stamm.Rows[0]["ID"], CultureInfo.InvariantCulture);
            string mit = Convert.ToString(stamm.Rows[0]["Bezeichner"], CultureInfo.InvariantCulture);
            string ohne = Convert.ToString(stamm.Rows[1]["Bezeichner"], CultureInfo.InvariantCulture);
            SetzeNachtzeit("Tab_Gebaeude_STAMM", idMit, BEGINN, ENDE);

            var ctrl = new GebaeudeStammCtrl();
            int neuMit = ctrl.CopyFromStamm(mit, idProjekt, idZuordnung);
            int neuOhne = ctrl.CopyFromStamm(ohne, idProjekt, idZuordnung);
            Assert.True(neuMit > 0 && neuOhne > 0);

            DataRow kopieMit = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende, ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", neuMit);
            Assert.Equal((long)BEGINN, Convert.ToInt64(kopieMit[0], CultureInfo.InvariantCulture));
            Assert.Equal((long)ENDE, Convert.ToInt64(kopieMit[1], CultureInfo.InvariantCulture));
            Assert.Equal((long)idMit, Convert.ToInt64(kopieMit[2], CultureInfo.InvariantCulture));   // die Klammer bleibt die letzte Stelle
            DataRow kopieOhne = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude WHERE ID = ?", neuOhne);
            Assert.Equal(DBNull.Value, kopieOhne[0]);
            Assert.Equal(DBNull.Value, kopieOhne[1]);

            var projekt = new GebaeudeCtrl();
            projekt.ReadAll("ID = " + neuMit.ToString(CultureInfo.InvariantCulture));
            GebaeudeModel p = Assert.Single(projekt.items);
            Assert.Equal(BEGINN, p.Nachtabsenkung_Beginn);
            Assert.Equal(ENDE, p.Nachtabsenkung_Ende);
        }

        /// <summary>DER NAMENSLESER DER SICHT: <c>ProjektGebaeudeCtrl</c> liefert die Nachtzeit NULL-erhaltend.</summary>
        [Fact]
        public void Der_Namensleser_der_Sicht_liefert_die_Nachtzeit_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            DataRow g = DataRepository.GetDataTable(
                "SELECT g.ID, z.ID_Projekt FROM Tab_Gebaeude g INNER JOIN Z_ProjektGebaeude z " +
                "ON z.ID = g.ID_ProjektGebaeude ORDER BY g.ID LIMIT 1").Rows[0];
            int idGebaeude = Convert.ToInt32(g["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(g["ID_Projekt"], CultureInfo.InvariantCulture);
            SetzeNachtzeit("Tab_Gebaeude", idGebaeude, BEGINN, ENDE);

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel gesetzt = ctrl.items.Single(x => x.ID_Gebaeude == idGebaeude);
            Assert.Equal(BEGINN, gesetzt.Nachtabsenkung_Beginn);
            Assert.Equal(ENDE, gesetzt.Nachtabsenkung_Ende);
            foreach (ProjektGebaeudeModel andere in ctrl.items.Where(x => x.ID_Gebaeude != idGebaeude))
            {
                Assert.Null(andere.Nachtabsenkung_Beginn);
                Assert.Null(andere.Nachtabsenkung_Ende);
            }

            // Der Feldspiegel Katalog → Projekt (UebergabeHerleitungsquelle) führt beide Felder mit.
            Assert.Contains("Nachtabsenkung_Beginn", UebergabeHerleitungsquelle.Uebertragen());
            Assert.Contains("Nachtabsenkung_Ende", UebergabeHerleitungsquelle.Uebertragen());
        }

        // =============================================================================
        //  Teil 4 - die Wege über die ganze Zeile
        // =============================================================================

        /// <summary>„Duplizieren…" der Gebäudeverwaltung kopiert jede Spalte — auch die Nachtzeit.</summary>
        [Fact]
        public void Katalog_Duplizieren_traegt_die_Nachtzeit()
        {
            if (!_db.Vorhanden) return;

            int idStamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            SetzeNachtzeit("Tab_Gebaeude_STAMM", idStamm, BEGINN, ENDE);

            Katalogkopie.Ergebnis e = GebaeudeStammCtrl.Duplizieren(idStamm, "Nachtzeit-Probe Kopie");
            Assert.True(e.Ok, e.Meldung);
            DataRow z = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude_STAMM WHERE ID = ?", e.Id);
            Assert.Equal((long)BEGINN, Convert.ToInt64(z[0], CultureInfo.InvariantCulture));
            Assert.Equal((long)ENDE, Convert.ToInt64(z[1], CultureInfo.InvariantCulture));
        }

        /// <summary>Das Projektduplikat trägt die Nachtzeit mit.</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Nachtzeit()
        {
            if (!_db.Vorhanden) return;

            SetzeNachtzeit("Tab_Gebaeude", GEBAEUDE, BEGINN, ENDE);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Nachtzeit");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            DataRow z = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu);
            Assert.Equal((long)BEGINN, Convert.ToInt64(z[0], CultureInfo.InvariantCulture));
            Assert.Equal((long)ENDE, Convert.ToInt64(z[1], CultureInfo.InvariantCulture));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die Nachtzeit über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Nachtzeit()
        {
            if (!_db.Vorhanden) return;

            SetzeNachtzeit("Tab_Gebaeude", GEBAEUDE, BEGINN, ENDE);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-nachtzeit-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Nachtzeit", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                DataRow z = Zeile("SELECT Nachtabsenkung_Beginn, Nachtabsenkung_Ende FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu);
                Assert.Equal((long)BEGINN, Convert.ToInt64(z[0], CultureInfo.InvariantCulture));
                Assert.Equal((long)ENDE, Convert.ToInt64(z[1], CultureInfo.InvariantCulture));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Teil 5 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Alle drei Wege führen den Schritt (Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests) aus derselben Quelle, der Sichtneubau
        /// steht in Werkzeug und Testkopie ZULETZT (hinter dem Baujahr), und die REPO-Datei trägt den
        /// Schritt (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt_zuletzt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wNacht = werkzeug.IndexOf("NachtzeitSchema.Alle(", StringComparison.Ordinal);
            Assert.True(wNacht > werkzeug.IndexOf("BaujahrSchema.Alle(", StringComparison.Ordinal) &&
                        wNacht > werkzeug.IndexOf("KuehluebergabeSchema.GebaeudeAlle(", StringComparison.Ordinal) &&
                        wNacht > werkzeug.IndexOf("BaustoffQuellenBerichtigung.Ausfuehren(", StringComparison.Ordinal),
                        "Der sechste Sichtneubau steht im Werkzeug nicht zuletzt.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_NACHTZEIT = NachtzeitSchema.SCHRITT", migration);
            int ortQuellen = migration.IndexOf("new Schritt(SCHRITT_BAUSTOFF_QUELLEN", StringComparison.Ordinal);
            int ortNacht = migration.IndexOf("new Schritt(SCHRITT_NACHTZEIT", StringComparison.Ordinal);
            Assert.True(ortQuellen > 0 && ortNacht > ortQuellen, "Der Schritt steht nicht hinter 143.");
            Assert.Contains("GebaeudeSchema.SQL_VIEW_NACHTZEIT", migration);
            Assert.Contains("GebaeudeSchema.SqliteNachtstunde(s)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vNacht = vorrichtung.IndexOf("NachtzeitSchema.Alle(null)", StringComparison.Ordinal);
            Assert.True(vNacht > vorrichtung.IndexOf("BaujahrSchema.Alle(null)", StringComparison.Ordinal) &&
                        vNacht > vorrichtung.IndexOf("KuehluebergabeSchema.GebaeudeAlle(null)", StringComparison.Ordinal) &&
                        vNacht > vorrichtung.IndexOf("BaustoffQuellenBerichtigung.Ausfuehren()", StringComparison.Ordinal),
                        "Der sechste Sichtneubau steht in der Testkopie nicht zuletzt.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= NachtzeitSchema.SCHRITT);
            foreach (string t in GebaeudeSchema.TABELLEN)
                foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                {
                    Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + t + "') WHERE name = '" + s + "'"));
                    Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM [" + t + "] WHERE " + s + " IS NOT NULL"));
                }
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = '" + GebaeudeSchema.VIEW + "'";
                Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void SetzeNachtzeit(string tabelle, int id, int beginn, int ende)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE [" + tabelle + "] SET Nachtabsenkung_Beginn = ?, Nachtabsenkung_Ende = ? WHERE ID = ?",
                new DbParam("?", beginn), new DbParam("?", ende), new DbParam("?", id)));
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, object wert)
        {
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", wert));
            Assert.True(dt != null && dt.Rows.Count == 1, "Keine eindeutige Zeile: " + sql);
            return dt.Rows[0];
        }

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        /// <summary>Scheitert die Anweisung an einer Prüfung? Der Vorgang wird nie festgeschrieben.</summary>
        private static bool Wirft(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, parameter);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        /// <summary>Eine leere Datenbank im Speicher — für die Prüfungen der Definition ohne Testdatenbank.</summary>
        private static SqliteConnection Speicher()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            return c;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Leer(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar() is DBNull;
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

        /// <summary>Die Bestandswerte der angefassten Tabellen, an denen der Schritt nichts ändern darf.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT ID, Nutzflaeche, Raumsolltemperatur_Tag, Raumsolltemperatur_Nachtabsenkung, Baujahr, Kuehluebergabe_Aktiv FROM Tab_Gebaeude ORDER BY ID",
                         "SELECT ID, Nutzflaeche, Raumsolltemperatur_Tag, Raumsolltemperatur_Nachtabsenkung, Baujahr, Kuehluebergabe_Aktiv FROM Tab_Gebaeude_STAMM ORDER BY ID",
                     })
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                foreach (DataRow r in dt.Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

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
