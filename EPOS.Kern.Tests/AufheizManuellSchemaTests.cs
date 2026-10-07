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
    /// Der Schemaschritt <b>KP-S4</b> (<see cref="AufheizManuellSchema"/>; Entscheid E59 samt Folgeentscheiden,
    /// E60; Entwurf KP3 Abschnitt 4, Festlegung 43): Aufschlag an <c>Tab_Einstellungen</c>, manuelle Aufheizzeit an
    /// <c>Tab_Gebaeude</c> samt achtem Sichtneubau, <c>Aufheiz_Art</c>, Auslegungsheizlast und Aufheizzuschlag im
    /// Ergebnis, <c>GEKOPPELT</c> an der Zone per kleinem Neubau.
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter der Katalogfassung, ohne festen Pin des Zielstands)
    /// und die Stufe der Paketanhebung; die Definitionen und Spaltenzahlen (43, 30, Sicht 103); jede Prüfklausel an
    /// einer STRICT-Tabelle im Speicher; der Zieltext des Neubaus samt benannter Abbrüche; der Stand der
    /// Testdatenbank; der Schritt aus dem Stand davor — zweimal, Zeilen, IDs, Fremdschlüssel und Indizes der
    /// Zonentabelle erhalten, <c>Tab_ErgebnisGebaeude</c> NICHT neu gebaut, <c>foreign_key_check</c> leer —, der
    /// Sichtneubau ZULETZT (ein älterer Durchgang schneidet die Spalte heraus, der Schritt setzt sie wieder);
    /// Migration, Werkzeug und Testkopie aus derselben Quelle und die Repo-Datei.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizManuellSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf die Katalogfassung der Stufe 2; der Zielstand reicht mindestens bis zu ihr (kein Pin).</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(KatalogfassungStufe2Schema.SCHRITT + 1, AufheizManuellSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= AufheizManuellSchema.SCHRITT);

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == AufheizManuellSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
            List<int> nummern = Paketanhebung.Stufen.Select(x => x.Nr).ToList();
            Assert.Equal(Enumerable.Range(Paketanhebung.UNTERE_GRENZE + 1,
                                          SchemaStand.Zielversion - Paketanhebung.UNTERE_GRENZE).ToList(), nummern);
        }

        /// <summary>Namen, Tabellen und Reihenfolge der sieben Spalten; Spaltenzahlen (B24); die Sicht mit 103 Spalten.</summary>
        [Fact]
        public void Die_Spalten_tragen_Namen_Tabellen_und_Reihenfolge_des_Entwurfs()
        {
            Assert.Equal(new[]
            {
                "Tab_Einstellungen.Aufheiz_Aufschlag_H", "Tab_Einstellungen.Aufheiz_Aufschlag_Prozent",
                "Tab_Gebaeude.Aufheizzeit_Manuell_H", "Tab_ErgebnisGebaeude.Aufheiz_Art",
                "Tab_ErgebnisGebaeude.Auslegungsheizlast_Kw", "Tab_ErgebnisGebaeude.Aufheizzuschlag_Kw",
                "Tab_ErgebnisZone.Aufheiz_Art",
            }, AufheizManuellSchema.SPALTEN.Select(s => s.Tabelle + "." + s.Spalte).ToArray());
            Assert.DoesNotContain(AufheizManuellSchema.SPALTEN, s => s.Tabelle == GebaeudeSchema.TAB_GEBAEUDE_STAMM);

            Assert.Equal(43, AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE);
            Assert.Equal(30, AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_ZONE);
            Assert.Equal(new[] { "TAEGLICH", "FEST", "MANUELL" }, DbWerte.AUFHEIZ_ERGEBNIS_ARTEN.ToArray());
            Assert.Equal(new[] { "TAEGLICH", "FEST" }, DbWerte.AUFHEIZ_ARTEN.ToArray());   // die Projektart bleibt zweiwertig

            Assert.Equal(103, GebaeudeSchema.SICHT_AUFHEIZ_MANUELL.Length);
            Assert.Equal("Aufheizzeit_Manuell_H", GebaeudeSchema.SICHT_AUFHEIZ_MANUELL[102]);
            Assert.Equal(GebaeudeSchema.SICHT_ENERGIESTANDARD, GebaeudeSchema.SICHT_AUFHEIZ_MANUELL.Take(102));
            Assert.Equal(GebaeudeSchema.SICHT_AUFHEIZ_MANUELL, GebaeudeSchema.SICHT_AKTUELL.Take(103));
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AK3, GebaeudeSchema.SQL_VIEW_AKTUELL);
            Assert.Contains("Tab_Gebaeude.Aufheizzeit_Manuell_H", GebaeudeSchema.SQL_VIEW_AKTUELL, StringComparison.Ordinal);

            Assert.Equal("CHECK (\"Aufheiz_Zustand\" IN ('BEMESSEN','UNERREICHBAR','UNBEHEIZT'))", AufheizManuellSchema.CHECK_ZUSTAND_ALT);
            Assert.Equal("CHECK (\"Aufheiz_Zustand\" IN ('BEMESSEN','UNERREICHBAR','GEKOPPELT','UNBEHEIZT'))", AufheizManuellSchema.CHECK_ZUSTAND);
            // Die alte Klausel ist wörtlich die des Schritts 161.
            Assert.Contains(AufheizManuellSchema.CHECK_ZUSTAND_ALT, AufheizErgebnisSchema.SpaltenZone[0].Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Jede Prüfklausel greift</b> — an einer STRICT-Tabelle im Speicher mit genau der Typdefinition des
        /// Schritts: ein Bestandssatz steht NULL; NULL und die Grenzwerte bestehen, jeder Wert außerhalb scheitert.
        /// </summary>
        [Fact]
        public void Jede_Pruefklausel_greift()
        {
            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach ((string _, string spalte, string typ) in AufheizManuellSchema.SPALTEN.GroupBy(s => s.Spalte).Select(g => g.First()))
                Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + spalte + "\" " + typ);
            foreach (string sp in AufheizManuellSchema.SPALTEN.Select(s => s.Spalte).Distinct())
                Assert.Equal(DBNull.Value, Skalar(c, "SELECT \"" + sp + "\" FROM T"));

            Pruefe(c, "Aufheiz_Aufschlag_H", new[] { "NULL", "0", "1", "24" }, new[] { "-1", "25", "'x'" });
            Pruefe(c, "Aufheiz_Aufschlag_Prozent", new[] { "NULL", "0", "12.5", "100" }, new[] { "-0.1", "100.5", "'x'" });
            Pruefe(c, "Aufheizzeit_Manuell_H", new[] { "NULL", "1", "5", "47" }, new[] { "0", "48", "-3", "'x'" });
            Pruefe(c, "Aufheiz_Art", new[] { "NULL", "'TAEGLICH'", "'FEST'", "'MANUELL'" }, new[] { "'manuell'", "'X'", "''" });
            Pruefe(c, "Auslegungsheizlast_Kw", new[] { "NULL", "0.001", "250" }, new[] { "0", "-1" });
            Pruefe(c, "Aufheizzuschlag_Kw", new[] { "NULL", "0", "5.5" }, new[] { "-0.001" });
        }

        /// <summary>
        /// Der Zieltext tauscht die EINE alte Zustandsklausel, sonst Zeichen für Zeichen der Bestand; fehlt sie, steht
        /// sie zweimal da oder ist die Tabelle nicht STRICT, bricht der Aufruf benannt ab.
        /// </summary>
        [Fact]
        public void Der_Zieltext_tauscht_genau_die_eine_Klausel()
        {
            string kopf = "CREATE TABLE \"Tab_ErgebnisZone\" (\"ID\" INTEGER PRIMARY KEY, \"Aufheiz_Zustand\" TEXT ";
            string bestand = kopf + AufheizManuellSchema.CHECK_ZUSTAND_ALT + ", \"X\" INTEGER) STRICT";
            Assert.Equal(kopf + AufheizManuellSchema.CHECK_ZUSTAND + ", \"X\" INTEGER) STRICT", AufheizManuellSchema.Zieltext(bestand));

            Assert.Throws<InvalidOperationException>(() => AufheizManuellSchema.Zieltext(kopf + "TEXT) STRICT"));
            Assert.Throws<InvalidOperationException>(() => AufheizManuellSchema.Zieltext(
                kopf + AufheizManuellSchema.CHECK_ZUSTAND_ALT + ", \"Y\" TEXT " + AufheizManuellSchema.CHECK_ZUSTAND_ALT + ") STRICT"));
            Assert.Throws<InvalidOperationException>(() => AufheizManuellSchema.Zieltext(kopf + AufheizManuellSchema.CHECK_ZUSTAND_ALT + ")"));
            Assert.Throws<InvalidOperationException>(() => AufheizManuellSchema.Zieltext(
                "CREATE TABLE \"Andere\" (" + AufheizManuellSchema.CHECK_ZUSTAND_ALT + ") STRICT"));
            Assert.Throws<InvalidOperationException>(() => AufheizManuellSchema.Zieltext(null));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>
        /// Die Testdatenbank steht auf dem Schritt: alle Spalten leer, die Sicht ist die geltende, die Zonentabelle
        /// STRICT mit beiden Fremdschlüsseln und Indizes, <c>GEKOPPELT</c> wird angenommen, ein fremder Zustand nicht.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= AufheizManuellSchema.SCHRITT);
            Assert.True(AufheizManuellSchema.Vollstaendig());
            Assert.Empty(AufheizManuellSchema.SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)));
            Assert.False(DataRepository.SpalteVorhanden(GebaeudeSchema.TAB_GEBAEUDE_STAMM, AufheizManuellSchema.SPALTE_MANUELL));
            Assert.Equal(AufheizAufschlagErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisGebaeude").Count);
            Assert.Equal(ZonenKaeltespitzeSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisZone").Count);
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, Text("SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?", GebaeudeSchema.VIEW));

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Aufheiz_Aufschlag_H IS NOT NULL OR Aufheiz_Aufschlag_Prozent IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Aufheizzeit_Manuell_H IS NOT NULL"));

            string zone = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", "Tab_ErgebnisZone");
            Assert.EndsWith(") STRICT", zone.TrimEnd());
            Assert.Contains(AufheizManuellSchema.CHECK_ZUSTAND, zone, StringComparison.Ordinal);
            Assert.DoesNotContain(AufheizManuellSchema.CHECK_ZUSTAND_ALT, zone, StringComparison.Ordinal);
            Assert.Equal(new[] { "Tab_ErgebnisGebaeude", "Tab_Zone" }, Fremdschluessel("Tab_ErgebnisZone"));
            Assert.Equal(new[] { "idx_ErgebnisZone_ErgebnisGebaeude", "idx_ErgebnisZone_Zone" }, Indizes("Tab_ErgebnisZone"));

            long idZone = ZonenzeileAnlegen(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT);
            Assert.True(idZone > 0);
            Assert.True(StilleDb.NonQuery("UPDATE Tab_ErgebnisZone SET Aufheiz_Zustand = 'GEMISCHT' WHERE ID = " +
                                          idZone.ToString(CultureInfo.InvariantCulture)) < 0);
            Assert.Equal(1, StilleDb.NonQuery("UPDATE Tab_ErgebnisZone SET Aufheiz_Art = 'MANUELL' WHERE ID = " +
                                              idZone.ToString(CultureInfo.InvariantCulture)));
            Assert.True(StilleDb.NonQuery("UPDATE Tab_Gebaeude SET Aufheizzeit_Manuell_H = 48 WHERE ID = (SELECT MIN(ID) FROM Tab_Gebaeude)") < 0);
            Assert.True(StilleDb.NonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Aufschlag_H = 25 WHERE ID_Projekt = 1007") < 0);
            Assert.Equal(1, StilleDb.NonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Aufschlag_H = 24, Aufheiz_Aufschlag_Prozent = 100 WHERE ID_Projekt = 1007"));
        }

        /// <summary>
        /// <b>Aus dem Stand davor, zweimal:</b> Die sieben Spalten fehlen, die Zonentabelle trägt die alte Klausel und
        /// eine Zeile, die Sicht ist die des Energiestandards. Der Schritt baut die Zonentabelle neu (Zeile samt ID,
        /// Fremdschlüssel und Indizes erhalten), legt die Spalten an, baut die Sicht — und baut
        /// <c>Tab_ErgebnisGebaeude</c> NICHT neu; ein zweiter Lauf tut nichts.
        /// </summary>
        [Fact]
        public void Aus_dem_Stand_davor_zweimal()
        {
            if (!_db.Vorhanden) return;

            long idZone = ZonenzeileAnlegen(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT);
            StandDavorHerstellen();
            Assert.False(AufheizManuellSchema.Vollstaendig());
            Assert.False(AufheizManuellSchema.ZoneKenntGekoppelt());
            Assert.False(AufheizManuellSchema.SichtSteht());
            Assert.Equal(AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisGebaeude").Count);
            Assert.Equal(AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisZone").Count);
            Assert.True(StilleDb.NonQuery("UPDATE Tab_ErgebnisZone SET Aufheiz_Zustand = 'GEKOPPELT'") < 0);

            string gebaeudeVorher = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", "Tab_ErgebnisGebaeude");
            List<string> zonenVorher = Bestand("SELECT ID, ID_ErgebnisGebaeude, ID_Zone, Rang, Bezeichner, IstBeheizt, Aufheiz_Zustand FROM Tab_ErgebnisZone ORDER BY ID");

            var bericht = new List<string>();
            Assert.Equal(7, AufheizManuellSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("GEKOPPELT (Neubau)", StringComparison.Ordinal) && z.Contains("2 Index(e)", StringComparison.Ordinal));
            Assert.Equal(7, bericht.Count(z => z.EndsWith("angelegt (leer)", StringComparison.Ordinal)));
            Assert.Contains(bericht, z => z.Contains("neu gebaut (103 Spalten)", StringComparison.Ordinal));
            Assert.True(AufheizManuellSchema.Vollstaendig());

            // Tab_ErgebnisGebaeude nur um drei Spalten verlaengert - kein Neubau (Entscheid Schemaweg A1).
            string gebaeudeNachher = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", "Tab_ErgebnisGebaeude");
            Assert.StartsWith(gebaeudeVorher.TrimEnd().Substring(0, gebaeudeVorher.TrimEnd().Length - ") STRICT".Length), gebaeudeNachher, StringComparison.Ordinal);
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisGebaeude").Count);
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisZone").Count);

            // Zeilen und IDs der Zone erhalten, Fremdschluessel und Indizes stehen, foreign_key_check leer.
            Assert.Equal(zonenVorher, Bestand("SELECT ID, ID_ErgebnisGebaeude, ID_Zone, Rang, Bezeichner, IstBeheizt, Aufheiz_Zustand FROM Tab_ErgebnisZone ORDER BY ID"));
            Assert.Equal(new[] { "Tab_ErgebnisGebaeude", "Tab_Zone" }, Fremdschluessel("Tab_ErgebnisZone"));
            Assert.Equal(new[] { "idx_ErgebnisZone_ErgebnisGebaeude", "idx_ErgebnisZone_Zone" }, Indizes("Tab_ErgebnisZone"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_ErgebnisZone')"));
            Assert.EndsWith(") STRICT", Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", "Tab_ErgebnisZone").TrimEnd());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE name LIKE '%\\_alt' ESCAPE '\\'"));
            Assert.Equal(1, StilleDb.NonQuery("UPDATE Tab_ErgebnisZone SET Aufheiz_Zustand = 'GEKOPPELT' WHERE ID = " +
                                              idZone.ToString(CultureInfo.InvariantCulture)));
            Assert.Equal("ok", Text("PRAGMA integrity_check", null));

            // Die Kaskade wirkt weiter: Die Gebaeudezeile nimmt ihre Zone mit.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_ErgebnisGebaeude WHERE ID = (SELECT ID_ErgebnisGebaeude FROM Tab_ErgebnisZone WHERE ID = ?)",
                                           new DbParam("?", idZone));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE ID = " + idZone.ToString(CultureInfo.InvariantCulture)));

            var zweiter = new List<string>();
            Assert.Equal(0, AufheizManuellSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("steht bereits", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Der Sichtneubau läuft ZULETZT:</b> Der ältere Durchgang des Energiestandards baut die Sicht in seiner Form
        /// und schneidet die manuelle Aufheizzeit heraus; der Schritt erkennt das und baut nur die Sicht neu. Fehlt nur
        /// eine Spalte, legt er nur sie an.
        /// </summary>
        [Fact]
        public void Der_Sichtneubau_laeuft_zuletzt_und_je_Teil_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            BaualtersklassenSchema.Ausfuehren(null);
            Assert.Equal(GebaeudeSchema.SICHT_ENERGIESTANDARD, GebaeudeSchema.SichtSpalten());
            Assert.False(AufheizManuellSchema.Vollstaendig());
            var nurSicht = new List<string>();
            Assert.Equal(0, AufheizManuellSchema.Ausfuehren(nurSicht));
            Assert.Equal(new[] { "Sicht Abfrage_Projektgebaeude neu gebaut (103 Spalten)" }, nurSicht.Take(1).ToArray());
            Assert.Equal(GebaeudeSchema.SICHT_AUFHEIZ_MANUELL, GebaeudeSchema.SichtSpalten());
            Assert.True(AufheizManuellSchema.Vollstaendig());

            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"Aufheiz_Aufschlag_Prozent\"");
            var bericht = new List<string>();
            Assert.Equal(1, AufheizManuellSchema.Ausfuehren(bericht));
            Assert.Contains("Tab_Einstellungen.Aufheiz_Aufschlag_Prozent angelegt (leer)", bericht);
            Assert.True(AufheizManuellSchema.Vollstaendig());
        }

        // =============================================================================
        //  Teil 3 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug und Testkopie führen den Schritt aus derselben
        /// Quelle, hinter der Katalogfassung der Stufe 2 und hinter dem älteren Sichtdurchgang des Energiestandards; die
        /// REPO-Datei trägt ihn leer (nur lesend).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wKatalog = werkzeug.IndexOf("KatalogfassungStufe2Schema.Ausfuehren(", StringComparison.Ordinal);
            int wSicht = werkzeug.IndexOf("BaualtersklassenSchema.Ausfuehren(", StringComparison.Ordinal);
            int wManuell = werkzeug.IndexOf("AufheizManuellSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wKatalog > 0 && wSicht > 0 && wManuell > wKatalog && wManuell > wSicht, "Das Werkzeug führt den Schritt nicht zuletzt.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_AUFHEIZ_MANUELL = AufheizManuellSchema.SCHRITT", migration);
            int mKatalog = migration.IndexOf("new Schritt(SCHRITT_KATALOGFASSUNG_STUFE2", StringComparison.Ordinal);
            int mManuell = migration.IndexOf("new Schritt(SCHRITT_AUFHEIZ_MANUELL", StringComparison.Ordinal);
            Assert.True(mKatalog > 0 && mManuell > mKatalog, "Der Schritt steht nicht hinter der Katalogfassung der Stufe 2.");
            Assert.Contains("AufheizManuellSchema.Ausfuehren(", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vKatalog = vorrichtung.IndexOf("KatalogfassungStufe2Schema.Ausfuehren(null)", StringComparison.Ordinal);
            int vSicht = vorrichtung.IndexOf("BaualtersklassenSchema.Ausfuehren(null)", StringComparison.Ordinal);
            int vManuell = vorrichtung.IndexOf("AufheizManuellSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vKatalog > 0 && vSicht > 0 && vManuell > vKatalog && vManuell > vSicht, "Die Testkopie führt den Schritt nicht zuletzt.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= AufheizManuellSchema.SCHRITT);
            foreach ((string tabelle, string spalte, string _) in AufheizManuellSchema.SPALTEN)
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + tabelle + "') WHERE name = '" + spalte +
                                                  "' AND \"notnull\" = 0 AND dflt_value IS NULL"));
            Assert.Equal((long)GebaeudeSchema.SICHT_AKTUELL.Length, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Abfrage_Projektgebaeude')"));
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'Tab_ErgebnisZone' AND sql LIKE '%''GEKOPPELT'',''UNBEHEIZT''%'"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Einstellungen WHERE Aufheiz_Aufschlag_H IS NOT NULL OR Aufheiz_Aufschlag_Prozent IS NOT NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE Aufheizzeit_Manuell_H IS NOT NULL"));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Stellt den Stand VOR dem Schritt her: die sieben Spalten fort (die Sicht zuvor in der Form des
        /// Energiestandards), die Zonentabelle mit der alten Zustandsklausel per Rückbau nach demselben Rezept.
        /// </summary>
        private static void StandDavorHerstellen()
        {
            // Der Stand vor dem Schritt der Uebergabe je Zone (E63) kennt dessen drei Ergebnisspalten nicht.
            // Der Stand davor kennt auch die Kältespitze je Zone (Schritt 185) und Aufschlag und bemessene Zeit (Schritt 194) nicht.
            foreach ((string tabelle, string spalte, string _) in AufheizAufschlagErgebnisSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            foreach ((string tabelle, string spalte, string _) in ZonenKaeltespitzeSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            foreach ((string tabelle, string spalte, string _) in ZonenUebergabeSchema.SPALTEN)
                if (tabelle == ZonenUebergabeSchema.TAB_ERGEBNIS_ZONE)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach ((string tabelle, string spalte, string _) in AufheizManuellSchema.SPALTEN.Reverse())
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD);

            using DbVorgang v = DataRepository.VorgangOhneFremdschluessel();
            string sql = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Tab_ErgebnisZone'"),
                                          CultureInfo.InvariantCulture);
            string alt = sql.Replace(AufheizManuellSchema.CHECK_ZUSTAND, AufheizManuellSchema.CHECK_ZUSTAND_ALT, StringComparison.Ordinal);
            Assert.NotEqual(sql, alt);
            var indizes = new List<string>();
            foreach (DataRow r in v.Lese("SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = 'Tab_ErgebnisZone' AND sql IS NOT NULL").Rows)
                indizes.Add(Convert.ToString(r["sql"], CultureInfo.InvariantCulture));
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"Tab_ErgebnisZone\" RENAME TO \"Tab_ErgebnisZone_rueck\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");
            v.Ausfuehren(alt);
            v.Ausfuehren("INSERT INTO \"Tab_ErgebnisZone\" SELECT * FROM \"Tab_ErgebnisZone_rueck\"");
            v.Ausfuehren("DROP TABLE \"Tab_ErgebnisZone_rueck\"");
            foreach (string i in indizes) v.Ausfuehren(i);
            v.Commit();
        }

        /// <summary>Legt eine Ergebniszeile des Gebäudes und darunter eine Zonenzeile mit <paramref name="zustand"/> an; die ID der Zonenzeile.</summary>
        private static long ZonenzeileAnlegen(string zustand)
        {
            long ergebnis = Zahl("SELECT MIN(ID) FROM Tab_Ergebnis");
            long gebaeude = Zahl("SELECT MIN(ID) FROM Tab_Gebaeude");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ErgebnisGebaeude (ID_Ergebnis, ID_Gebaeude, Merkplatz, Rechenweg, Heizwaerme_Mwh, Spitze_Kw, " +
                "SpitzeTagesmittel_Kw, Spitze95_Kw) VALUES (?, ?, 0, 'VDI6007', 1.0, 2.0, 1.5, 1.8)",
                new DbParam("?", ergebnis), new DbParam("?", gebaeude));
            long idGeb = Zahl("SELECT MAX(ID) FROM Tab_ErgebnisGebaeude");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ErgebnisZone (ID_ErgebnisGebaeude, Rang, Bezeichner, IstBeheizt, Aufheiz_Zustand) VALUES (?, 1, 'Zone A', 1, ?)",
                new DbParam("?", idGeb), new DbParam("?", zustand));
            return Zahl("SELECT MAX(ID) FROM Tab_ErgebnisZone");
        }

        private static string[] Fremdschluessel(string tabelle)
        {
            var liste = new List<string>();
            foreach (DataRow r in DataRepository.GetDataTable("SELECT \"table\" FROM pragma_foreign_key_list(?) ORDER BY \"table\"",
                                                              new DbParam("?", tabelle)).Rows)
                liste.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));
            return liste.ToArray();
        }

        private static string[] Indizes(string tabelle)
        {
            var liste = new List<string>();
            foreach (DataRow r in DataRepository.GetDataTable(
                         "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = ? AND sql IS NOT NULL ORDER BY name",
                         new DbParam("?", tabelle)).Rows)
                liste.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));
            return liste.ToArray();
        }

        private static void Pruefe(SqliteConnection c, string spalte, string[] gut, string[] schlecht)
        {
            foreach (string w in gut)
                Assert.False(Wirft(c, "UPDATE T SET \"" + spalte + "\" = " + w), spalte + " = " + w + " wurde abgelehnt.");
            foreach (string w in schlecht)
                Assert.True(Wirft(c, "UPDATE T SET \"" + spalte + "\" = " + w), spalte + " = " + w + " wurde angenommen.");
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql, string parameter)
            => Convert.ToString(parameter == null ? DataRepository.ExecuteScalar(sql)
                                                  : DataRepository.ExecuteScalar(sql, new DbParam("?", parameter)),
                                CultureInfo.InvariantCulture);

        private static List<string> Bestand(string sql)
        {
            var liste = new List<string>();
            foreach (DataRow r in DataRepository.GetDataTable(sql).Rows)
                liste.Add(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
            return liste;
        }

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

        private static object Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
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

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
