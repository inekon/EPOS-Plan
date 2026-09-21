using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID vom 21.09.2026 (Auftrag FK-1) — Schemaschritt <b>100</b>: Die
    /// Fremdschlüsselspalten verlieren ihre Vorgabe <c>0</c>.
    ///
    /// <para><b>Worum es geht.</b> Einundvierzig Fremdschlüsselspalten in fünfundzwanzig
    /// Tabellen trugen <c>DEFAULT 0</c>, und keine Elterntabelle hat eine Zeile 0. Ein
    /// Schreibweg, der eine solche Spalte weglässt, bekam damit still die 0 — und die 0
    /// verletzt die Beziehung. Ab hier steht dort keine Vorgabe mehr: Wo
    /// <c>NOT NULL</c> steht, meldet die weggelassene Spalte sofort
    /// <c>NOT NULL constraint failed</c> MIT Tabelle und Spalte; wo sie nullbar ist,
    /// wird sie NULL, und NULL lässt SQLite bei einer Beziehung immer durch.</para>
    ///
    /// <para><b>Geprüft wird dreierlei.</b> Die TEXTE (der Zieltext nimmt genau EINER
    /// Spalte ihre Vorgabe und lässt NOT NULL stehen; er verlangt STRICT und bricht bei
    /// einer anderen Vorgabe ab). Die WACHE über der Datei (nach dem Schritt trägt keine
    /// Fremdschlüsselspalte mehr die Vorgabe — gefragt über
    /// <c>pragma_foreign_key_list</c> × <c>pragma_table_info</c>). Und der UMBAU selbst
    /// an einer Probetabelle, die den Ausgangszustand eigens herstellt: Zeilen, Ids,
    /// <c>sqlite_sequence</c> und Indizes bleiben, der zweite Lauf tut nichts mehr, und
    /// eine Zeile mit dem Wert 0 hält den Schritt BENANNT an.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class FremdschluesselVorgabeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Die Probetabelle stellt den Zustand VOR dem Schritt selbst her.</summary>
        private const string PROBE = "Tab_FK1Probe";

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Stand und Texte (ohne Datenbank)
        // =============================================================================

        /// <summary>Der Zielstand ist 100.</summary>
        [Fact]
        public void Der_Zielstand_ist_100()
        {
            using var _ = new Kulturvorrichtung();

            Assert.True(SchemaStand.Zielversion >= 100,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 100.");
            Assert.Equal("0", FremdschluesselVorgabe.VORGABE);
        }

        /// <summary>
        /// DER ZIELTEXT IST DER BESTANDSTEXT MINUS EINER VORGABE — nichts sonst. Die
        /// Probe nimmt die Bauform von <c>Tab_ProjektWerte</c>: EINE Zeile, mehrere
        /// Spalten mit <c>DEFAULT 0</c>, und nur zwei davon tragen eine Beziehung. Die
        /// anderen behalten ihre Vorgabe.
        /// </summary>
        [Fact]
        public void Der_Zieltext_nimmt_nur_den_genannten_Spalten_ihre_Vorgabe()
        {
            using var _ = new Kulturvorrichtung();

            const string bestand =
                "CREATE TABLE \"Tab_X\" (\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "\"ProjektID\" INTEGER DEFAULT 0, " +
                "\"KategorieID\" INTEGER DEFAULT 0, " +
                "\"StammID\" INTEGER NOT NULL DEFAULT 0, " +
                "\"IstErloes\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstErloes\" IN (0,1)), " +
                "FOREIGN KEY (\"ProjektID\") REFERENCES \"Tab_Projekt\" (\"ID\")) STRICT";

            string ziel = FremdschluesselVorgabe.Zieltext(
                "Tab_X", bestand, new[] { "ProjektID", "StammID" });

            Assert.Contains("\"ProjektID\" INTEGER,", ziel, StringComparison.Ordinal);
            Assert.Contains("\"StammID\" INTEGER NOT NULL,", ziel, StringComparison.Ordinal);

            // Die zwei Unbeteiligten behalten ihre Vorgabe - samt der CHECK-Klausel,
            // die HINTER der Vorgabe steht.
            Assert.Contains("\"KategorieID\" INTEGER DEFAULT 0,", ziel, StringComparison.Ordinal);
            Assert.Contains("\"IstErloes\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstErloes\" IN (0,1))",
                            ziel, StringComparison.Ordinal);

            Assert.EndsWith(") STRICT", ziel, StringComparison.Ordinal);
            Assert.Equal(2, Vorkommen(bestand, "DEFAULT 0") - Vorkommen(ziel, "DEFAULT 0"));
        }

        /// <summary>
        /// Ohne <c>STRICT</c> baut der Schritt nicht um, und eine ANDERE Vorgabe fasst er
        /// nicht an — er fasst nur an, was er versteht.
        /// </summary>
        [Fact]
        public void Der_Zieltext_verlangt_STRICT_und_genau_diese_Vorgabe()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Throws<InvalidOperationException>(() => FremdschluesselVorgabe.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_X\" (\"ID_Projekt\" INTEGER DEFAULT 0)",
                new[] { "ID_Projekt" }));

            Assert.Throws<InvalidOperationException>(() => FremdschluesselVorgabe.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_X\" (\"ID_Projekt\" INTEGER DEFAULT 1) STRICT",
                new[] { "ID_Projekt" }));

            Assert.Throws<InvalidOperationException>(() => FremdschluesselVorgabe.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_X\" (\"ID_Projekt\" INTEGER) STRICT",
                new[] { "ID_Projekt" }));

            // Eine Spalte, die es im Text gar nicht gibt.
            Assert.Throws<InvalidOperationException>(() => FremdschluesselVorgabe.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_X\" (\"ID_Projekt\" INTEGER DEFAULT 0) STRICT",
                new[] { "ID_Anderes" }));
        }

        // =============================================================================
        //  Teil 2 - die Wache ueber der Datei
        // =============================================================================

        /// <summary>
        /// DIE WACHE (Zeuge a): Nach dem Schritt trägt KEINE Fremdschlüsselspalte mehr
        /// die Vorgabe <c>0</c>. Gefragt wird die Datei selbst — die Abfrage steht hier
        /// noch einmal ausgeschrieben, damit der Nachweis nicht dieselbe Methode prüft,
        /// die er beweisen soll.
        /// </summary>
        [Fact]
        public void Keine_Fremdschluesselspalte_traegt_noch_die_Vorgabe()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            DataTable treffer = DataRepository.GetDataTable(
                "SELECT DISTINCT m.name AS tabelle, f.\"from\" AS spalte " +
                "FROM sqlite_master m " +
                "JOIN pragma_foreign_key_list(m.name) f " +
                "JOIN pragma_table_info(m.name) t ON t.name = f.\"from\" " +
                "WHERE m.type = 'table' AND substr(m.name, 1, 7) <> 'sqlite_' " +
                "AND t.dflt_value = '0' " +
                "ORDER BY m.name, f.\"from\"");

            var namen = new List<string>();
            foreach (DataRow zeile in treffer.Rows)
                namen.Add(Convert.ToString(zeile["tabelle"], CultureInfo.InvariantCulture) + "." +
                          Convert.ToString(zeile["spalte"], CultureInfo.InvariantCulture));

            Assert.True(namen.Count == 0,
                        "Diese Fremdschluesselspalte(n) tragen noch die Vorgabe 0: " +
                        string.Join(", ", namen.ToArray()) + ".");

            Assert.Equal(0, FremdschluesselVorgabe.OffeneSpalten());
            Assert.Equal(0, FremdschluesselVorgabe.Offen());
            Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows);
        }

        // =============================================================================
        //  Teil 3 - der Umbau an einer Probetabelle
        // =============================================================================

        /// <summary>
        /// DER UMBAU: Die Probetabelle verliert ihre Vorgabe und behält alles andere —
        /// Zeilen, Ids, <c>NOT NULL</c>, den AUTOINCREMENT-Stand und ihren Index.
        /// </summary>
        [Fact]
        public void Die_Probetabelle_verliert_ihre_Vorgabe_und_behaelt_alles_andere()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            ProbeAnlegen();
            int projekt = (int)Zahl("SELECT MIN(ID) FROM Tab_Projekt");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"" + PROBE + "\" (ID, ID_Projekt, Bezeichner) VALUES (?, ?, ?)",
                new DbParam("p1", 7), new DbParam("p2", projekt), new DbParam("p3", "Probe"));

            Assert.True(FremdschluesselVorgabe.UmbauNoetig(PROBE));
            Assert.Equal(new[] { "ID_Projekt" }, FremdschluesselVorgabe.BetroffeneSpalten(PROBE).ToArray());

            var bericht = new List<string>();
            Assert.True(FremdschluesselVorgabe.Umbauen(PROBE, bericht));
            Assert.Single(bericht);
            Assert.Contains("ID_Projekt", bericht[0], StringComparison.Ordinal);

            // Die Vorgabe ist weg, NOT NULL steht.
            Assert.False(FremdschluesselVorgabe.UmbauNoetig(PROBE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_table_info(?) " +
                                  "WHERE name = 'ID_Projekt' AND dflt_value IS NOT NULL",
                                  new DbParam("p1", PROBE)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_table_info(?) " +
                                  "WHERE name = 'ID_Projekt' AND \"notnull\" = 1",
                                  new DbParam("p1", PROBE)));

            // Zeile, Id und Werte stehen unveraendert.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"" + PROBE + "\""));
            Assert.Equal(projekt, (int)Zahl("SELECT ID_Projekt FROM \"" + PROBE + "\" WHERE ID = 7"));

            // Der Zaehlerstand und der Index sind mitgereist.
            Assert.Equal(7L, Zahl("SELECT seq FROM sqlite_sequence WHERE name = ?",
                                  new DbParam("p1", PROBE)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM sqlite_master " +
                                  "WHERE type = 'index' AND tbl_name = ? AND name = 'idx_FK1Probe'",
                                  new DbParam("p1", PROBE)));

            // Und die Beziehung steht weiter.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check(?)",
                                  new DbParam("p1", PROBE)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_list(?) " +
                                  "WHERE \"table\" = 'Tab_Projekt' AND \"from\" = 'ID_Projekt'",
                                  new DbParam("p1", PROBE)));
        }

        /// <summary>
        /// WIEDERHOLBAR (Zeuge b): Der zweite Lauf findet nichts mehr und fasst nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_ist_wiederholbar()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            ProbeAnlegen();

            var ersterLauf = new List<string>();
            Assert.Equal(1, FremdschluesselVorgabe.Alle(ersterLauf));

            var zweiterLauf = new List<string>();
            Assert.Equal(0, FremdschluesselVorgabe.Alle(zweiterLauf));
            Assert.Empty(zweiterLauf);

            Assert.False(FremdschluesselVorgabe.Umbauen(PROBE, null));
            Assert.Equal(0, FremdschluesselVorgabe.OffeneSpalten());
        }

        /// <summary>
        /// DER ZEUGE VORHER: Eine Zeile, die den Wert 0 WIRKLICH trägt, hält den Schritt
        /// benannt an. Der Schritt ändert keine Werte — eine solche Zeile zeigt auf einen
        /// Elternsatz, den es nicht gibt, und ist vor dem Umbau zu klären.
        /// </summary>
        [Fact]
        public void Eine_Zeile_mit_dem_Wert_0_haelt_den_Schritt_benannt_an()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            ProbeAnlegen();

            // Die 0 kommt nur mit abgeschalteten Fremdschluesseln hinein - genau das ist
            // der Zustand, den der Schritt nicht zementieren soll.
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                // (object)0 und nicht 0: Die nackte Null ginge implizit in DbParamTyp
                // ueber, Roslyn waehlte DbParam(string, DbParamTyp), und gebunden waere
                // DBNull statt der Zahl (Wache DbParamNullkonstanteWacheTests).
                v.Ausfuehren("INSERT INTO \"" + PROBE + "\" (ID, ID_Projekt, Bezeichner) VALUES (?, ?, ?)",
                             new DbParam("p1", 3), new DbParam("p2", (object)0),
                             new DbParam("p3", "Waise"));
                v.Commit();
            }

            Assert.Equal(1L, FremdschluesselVorgabe.ZeilenMitVorgabe(PROBE, "ID_Projekt"));

            InvalidOperationException fehler =
                Assert.Throws<InvalidOperationException>(() => FremdschluesselVorgabe.Umbauen(PROBE, null));
            Assert.Contains(PROBE + ".ID_Projekt", fehler.Message, StringComparison.Ordinal);

            // Die Tabelle ist, wie sie war: Vorgabe noch da, Zeile noch da.
            Assert.True(FremdschluesselVorgabe.UmbauNoetig(PROBE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"" + PROBE + "\" WHERE ID = 3"));
        }

        // =============================================================================
        //  Kleinkram
        // =============================================================================

        /// <summary>
        /// Die Probetabelle: eine STRICT-Tabelle mit EINER Fremdschlüsselspalte, die
        /// <c>NOT NULL DEFAULT 0</c> trägt — der Zustand vor Schemaschritt 100 — samt
        /// Index. Sie entsteht auf der Arbeitskopie eines einzelnen Prüflaufs und
        /// verschwindet mit ihr; die Quelldatei bleibt unberührt.
        /// </summary>
        private static void ProbeAnlegen()
        {
            DataRepository.ExecuteNonQuery("DROP TABLE IF EXISTS \"" + PROBE + "\"");
            DataRepository.ExecuteNonQuery(
                "CREATE TABLE \"" + PROBE + "\" (" +
                "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "\"ID_Projekt\" INTEGER NOT NULL DEFAULT 0, " +
                "\"Bezeichner\" TEXT, " +
                "FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") " +
                "ON DELETE CASCADE ON UPDATE CASCADE) STRICT");
            DataRepository.ExecuteNonQuery(
                "CREATE INDEX IF NOT EXISTS \"idx_FK1Probe\" ON \"" + PROBE + "\" (\"ID_Projekt\")");
        }

        /// <summary>Eine Zählung; −1, wenn sie nichts liefert.</summary>
        private static long Zahl(string sql, params DbParam[] parameter)
        {
            object wert = DataRepository.ExecuteScalar(sql, parameter);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        /// <summary>Wie oft steht <paramref name="teil"/> in <paramref name="text"/>?</summary>
        private static int Vorkommen(string text, string teil)
        {
            int anzahl = 0;
            int stelle = text.IndexOf(teil, StringComparison.Ordinal);
            while (stelle >= 0)
            {
                anzahl++;
                stelle = text.IndexOf(teil, stelle + teil.Length, StringComparison.Ordinal);
            }
            return anzahl;
        }
    }
}
