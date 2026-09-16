using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Projektkopie einer Waermepumpe kennt ihren Katalogsatz</b>
    /// (Anwenderentscheid 16.09.2026, Auftrag #299) — Schemaschritt 80 und die vier
    /// Wege, die den Verweis benutzen.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> <c>Tab_WP</c> hing am Katalogsatz allein
    /// ueber den BEZEICHNER, und der ist in keiner der zwei Tabellen eindeutig. Wer
    /// einen Katalogsatz umbenannte, zerriss damit die Klammer zu jeder Projektkopie:
    /// „In Stamm uebernehmen" legte danach einen ZWEITEN Katalogsatz an, statt den
    /// vorhandenen zu pflegen. Am Rechenergebnis ist das nicht abzulesen — kein
    /// Rechenweg liest die Spalte.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> und bekommt ueber
    /// <see cref="TestDatenbank"/> als <c>IClassFixture</c> ihre eigene Arbeitskopie.
    /// Jeder Fall stellt her, was er braucht — die Faelle teilen sich eine Kopie und
    /// laufen in keiner zugesicherten Reihenfolge.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeKatalogverweisTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public WaermepumpeKatalogverweisTests(TestDatenbank db) { _db = db; }

        // --- Das FREIE Geraet: Katalogsatz 61 ohne Schreibschutz, Kopien in vier
        //     Projekten (1008, 1029, 1042, 1043).
        private const string NAME_FREI = "CS7800iLW 16";
        private const int STAMM_FREI = 61;
        private const int WP_1008 = 1008061;
        private const int PROJEKT_1008 = 1008;

        // --- Ein Projekt, das KEINE Kopie dieses Geraets fuehrt: dort legt
        //     CopyFromStamm eine neue an.
        private const int PROJEKT_OHNE_KOPIE = 1017;

        // =================================================================================
        // 1 - CopyFromStamm setzt den Verweis und findet die Kopie darueber wieder
        // =================================================================================

        /// <summary>
        /// Die Kopie merkt sich ihren Katalogsatz — und ein zweiter Aufruf findet sie
        /// darueber wieder, auch wenn der Bezeichner der Kopie inzwischen ein anderer
        /// ist.
        /// </summary>
        [Fact]
        public void CopyFromStamm_setzt_den_Verweis_und_erkennt_die_Kopie_darueber()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var ctrl = new WPCtrl();

            // Sauberer Ausgangszustand: das Projekt fuehrt keine Kopie dieses Geraets.
            Sql("DELETE FROM Tab_Kenndaten WHERE ID_WP IN " +
                "(SELECT ID FROM Tab_WP WHERE ID_Projekt = ? AND Bezeichner = ?)",
                new DbParam("@p", PROJEKT_OHNE_KOPIE), new DbParam("@b", NAME_FREI));
            Sql("DELETE FROM Tab_WP WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", PROJEKT_OHNE_KOPIE), new DbParam("@b", NAME_FREI));

            int neu = ctrl.CopyFromStamm(STAMM_FREI, PROJEKT_OHNE_KOPIE);
            Assert.True(neu > 0, "Die Kopie ist nicht entstanden.");
            Assert.Equal(STAMM_FREI, Verweis(neu));

            // Derselbe Aufruf noch einmal: KEINE zweite Kopie.
            Assert.Equal(neu, ctrl.CopyFromStamm(STAMM_FREI, PROJEKT_OHNE_KOPIE));

            // Und jetzt die eigentliche Zusage: Der Name der Kopie ist ein anderer, der
            // VERWEIS traegt trotzdem.
            Sql("UPDATE Tab_WP SET Bezeichner = ? WHERE ID = ?",
                new DbParam("@b", NAME_FREI + " (umbenannt)"), new DbParam("@id", neu));

            Assert.Equal(neu, ctrl.GetProjektIdZuStamm(STAMM_FREI, PROJEKT_OHNE_KOPIE));
            Assert.Equal(0, ctrl.GetProjektId(NAME_FREI, PROJEKT_OHNE_KOPIE));
            Assert.Equal(neu, ctrl.CopyFromStamm(STAMM_FREI, PROJEKT_OHNE_KOPIE));

            // Den Namen zuruecklegen: Die Faelle dieser Klasse teilen sich EINE
            // Arbeitskopie, und eine Kopie ohne Katalogsatz gleichen Namens waere fuer
            // den Nachtragsfall eine zweite, ungewollte Fundstelle.
            Sql("UPDATE Tab_WP SET Bezeichner = ? WHERE ID = ?",
                new DbParam("@b", NAME_FREI), new DbParam("@id", neu));
        }

        // =================================================================================
        // 2 - Die Uebernahme trifft den VERKNUEPFTEN Satz, auch nach einer Umbenennung
        // =================================================================================

        /// <summary>
        /// Wird der KATALOGSATZ umbenannt, trifft die Uebernahme weiter ihn — und die
        /// Vorschau nennt seinen abweichenden Namen, damit die Oberflaeche es sagen
        /// kann.
        /// </summary>
        /// <remarks>
        /// Ueber den Namen waere der Satz nicht mehr zu finden gewesen: Die Uebernahme
        /// haette einen ZWEITEN Katalogsatz angelegt, und der gepflegte waere
        /// zurueckgeblieben.
        /// </remarks>
        [Fact]
        public void Die_Uebernahme_trifft_nach_Umbenennung_den_verknuepften_Katalogsatz()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            // Der Verweis steht (Migrationsschritt 80 hat ihn nachgetragen).
            Assert.Equal(STAMM_FREI, Verweis(WP_1008));

            string neuerName = NAME_FREI + " (Katalog neu)";
            long saetzeVorher = Zahl("SELECT COUNT(*) FROM Tab_WP_STAMM");

            try
            {
                Sql("UPDATE Tab_WP_STAMM SET Bezeichner = ? WHERE ID = ?",
                    new DbParam("@b", neuerName), new DbParam("@id", STAMM_FREI));

                // Die Vorschau: Der Satz ist da, und sie nennt BEIDE Namen.
                WPStammCtrl.UebernahmeVorschauSatz v =
                    WPStammCtrl.UebernahmeVorschau(WP_1008, PROJEKT_1008);

                Assert.True(v.KatalogsatzVorhanden);
                Assert.False(v.ReadOnly);
                Assert.Equal(NAME_FREI, v.Bezeichner);
                Assert.Equal(neuerName, v.KatalogBezeichner);
                Assert.NotEqual(v.Bezeichner, v.KatalogBezeichner);

                // Die Uebernahme: Sie pflegt den VERKNUEPFTEN Satz, sie legt keinen an.
                Sql("UPDATE Tab_WP SET Firma = ? WHERE ID = ?",
                    new DbParam("@f", "Pruefhersteller 299"), new DbParam("@id", WP_1008));

                WPStammCtrl.SpeicherErgebnis e =
                    WPStammCtrl.UebernehmenAusProjekt(WP_1008, PROJEKT_1008, false);

                Assert.True(e.Ok, e.Meldung);
                Assert.Equal(saetzeVorher, Zahl("SELECT COUNT(*) FROM Tab_WP_STAMM"));
                Assert.Equal("Pruefhersteller 299", Text(
                    "SELECT Firma FROM Tab_WP_STAMM WHERE ID = ?", STAMM_FREI));

                // Der Name des Katalogsatzes bleibt seiner - die Uebernahme traegt die
                // FACHSPALTEN, nicht den Schluessel.
                Assert.Equal(neuerName, Text(
                    "SELECT Bezeichner FROM Tab_WP_STAMM WHERE ID = ?", STAMM_FREI));
            }
            finally
            {
                Sql("UPDATE Tab_WP_STAMM SET Bezeichner = ? WHERE ID = ?",
                    new DbParam("@b", NAME_FREI), new DbParam("@id", STAMM_FREI));
            }
        }

        // =================================================================================
        // 3 - Der Nachtrag raet nicht
        // =================================================================================

        /// <summary>
        /// Gefuellt wird nur, wo der Bezeichner GENAU EINEN Katalogsatz trifft; eine
        /// Projektkopie ohne Katalogsatz bleibt NULL — und keine bekommt einen falschen.
        /// </summary>
        /// <remarks>
        /// <para><b>Der mehrdeutige Fall ist im gesunden Bestand nicht herstellbar</b>
        /// und deshalb hier nicht gebaut: <c>Tab_WP_STAMM</c> traegt den eindeutigen
        /// Index <c>UX_Tab_WP_STAMM_Bezeichner</c>; ein zweiter Katalogsatz gleichen
        /// Namens wird von der Datenbank abgewiesen. Die Bedingung
        /// „<c>(Trefferzahl) = 1</c>" bleibt trotzdem stehen — sie ist die Zusage des
        /// Nachtrags, nicht eine Vermutung ueber den Index, und sie traegt auch eine
        /// Datenbank, die ihn (noch) nicht hat.</para>
        /// </remarks>
        [Fact]
        public void Der_Nachtrag_fuellt_nur_bei_eindeutigem_Bezeichner()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            string alterName = Text("SELECT Bezeichner FROM Tab_WP WHERE ID = ?", WP_1008);

            try
            {
                // Eine Projektkopie, deren Name im Katalog gar nicht vorkommt.
                Sql("UPDATE Tab_WP SET Bezeichner = ? WHERE ID = ?",
                    new DbParam("@b", "Geraet ohne Katalogsatz 299"), new DbParam("@id", WP_1008));

                // Alle Verweise loeschen und neu nachtragen - dieselbe Anweisung wie in
                // Migration und Werkzeug.
                Sql("UPDATE Tab_WP SET " + WaermepumpeKatalogverweis.SPALTE + " = NULL");
                Assert.True(WaermepumpeKatalogverweis.NachtragNoetig());

                DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SqlNachtrag());

                Assert.Equal(0, Verweis(WP_1008));       // kein Katalogsatz
                Assert.Equal(0L, Zahl(WaermepumpeKatalogverweis.Zaehlung()));

                // Ohne Verweis bleibt GENAU die Menge, deren Bezeichner den Katalog
                // nicht eindeutig trifft - gezaehlt aus den Daten, nicht als feste Zahl:
                // Die Faelle dieser Klasse teilen sich eine Kopie und laufen in keiner
                // zugesicherten Reihenfolge.
                Assert.Equal(
                    Zahl("SELECT COUNT(*) FROM Tab_WP w WHERE (SELECT COUNT(*) FROM " +
                         "Tab_WP_STAMM s WHERE s.Bezeichner = w.Bezeichner) <> 1"),
                    Zahl(WaermepumpeKatalogverweis.ZaehlungOhneVerweis()));

                // Alle uebrigen haben ihren Verweis bekommen, und zwar den richtigen:
                // Jeder zeigt auf einen Katalogsatz gleichen Bezeichners.
                Assert.Equal(0L, Zahl(
                    "SELECT COUNT(*) FROM Tab_WP w WHERE w." + WaermepumpeKatalogverweis.SPALTE +
                    " IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Tab_WP_STAMM s " +
                    "WHERE s.ID = w." + WaermepumpeKatalogverweis.SPALTE +
                    " AND s.Bezeichner = w.Bezeichner)"));

                // Der eindeutige Index des Katalogs - der Grund, warum der mehrdeutige
                // Fall hier nicht gebaut wird.
                Assert.Equal(1L, Zahl(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' " +
                    "AND tbl_name = 'Tab_WP_STAMM' AND sql LIKE '%UNIQUE%Bezeichner%'"));
            }
            finally
            {
                Sql("UPDATE Tab_WP SET Bezeichner = ? WHERE ID = ?",
                    new DbParam("@b", alterName), new DbParam("@id", WP_1008));
                DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SqlNachtrag());
            }
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Der Katalogverweis einer Projektkopie; NULL ergibt 0.</summary>
        private static int Verweis(int idWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT " + WaermepumpeKatalogverweis.SPALTE + " FROM Tab_WP WHERE ID = ?",
                new DbParam("@id", idWp));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static string Text(string sql, int id)
        {
            object v = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }

        private static long Zahl(string sql)
        {
            object v = DataRepository.ExecuteScalar(sql);
            return (v == null || v == DBNull.Value) ? -1 : Convert.ToInt64(v);
        }

        private static void Sql(string sql, params DbParam[] p)
        {
            DataRepository.ExecuteNonQuery(sql, p);
        }
    }
}
