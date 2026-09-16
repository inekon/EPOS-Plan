using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #302 (16.09.2026) — Migrationsschritt <b>81</b>: Der Fremdschluessel
    /// <c>Tab_ProjektWerte.StammID → Tab_Kostenfaktor(StammID)</c> traegt
    /// <c>ON DELETE RESTRICT</c> statt <c>ON DELETE CASCADE</c>.
    ///
    /// <para><b>Der Befund.</b> EINEN Katalogeintrag im Dialog „Administration
    /// Kostenfaktoren" zu loeschen riss JEDE Projektposition derselben <c>StammID</c>
    /// mit — quer durch alle Projekte und Gewerke. <c>PRAGMA foreign_keys = ON</c> steht
    /// je Verbindung, die Kaskade war also scharf. Die erste Schicht dagegen ist
    /// <c>KostenfaktorCtrl.Loeschen</c> (siehe <see cref="KostenfaktorCtrlTests"/>);
    /// DIESE Faelle nageln die zweite fest — die Datenbank selbst.</para>
    ///
    /// <para>Geprueft wird dreierlei: die TEXTE (Zielstand, Spaltenliste, Hilfsname,
    /// Reihenfolge der Anweisungen) ohne Datenbank; die WACHE auf der Arbeitskopie (die
    /// Regel steht, und die Kaskade greift nicht mehr); und die MIGRATION selbst — vom
    /// alten Stand aus, mit Zeilen, IDs, Zaehlerstand, Indizes, der abhaengigen Sicht und
    /// einem zweiten Lauf, der nichts mehr tut.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektWerteSchemaWacheTests
    {
        /// <summary>„Wärmepumpe (Aggregat)" — ein Kostenfaktor mit Projektpositionen.</summary>
        private const int WaermepumpeAggregat = 108;

        /// <summary>Die Sicht, die den Umbau zur Umbenennung unter legacy-Modus zwingt.</summary>
        private const string Sicht = "Abfrage_Kostenfaktoren";

        // =============================================================================
        //  Teil 1 — die Texte. Ohne Datenbank entscheidbar.
        // =============================================================================

        /// <summary>Der Zielstand ist 81, und der CREATE-Text traegt die neue Regel.</summary>
        [Fact]
        public void Der_Zielstand_ist_81_und_der_CREATE_Text_traegt_RESTRICT()
        {
            Assert.True(SchemaStand.Zielversion >= 81,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 81.");

            Assert.Contains("ON UPDATE CASCADE ON DELETE RESTRICT",
                            ProjektWerteLoeschschutz.SQL_TABELLE, StringComparison.Ordinal);
            Assert.DoesNotContain("ON UPDATE CASCADE ON DELETE CASCADE, ",
                                  ProjektWerteLoeschschutz.SQL_TABELLE, StringComparison.Ordinal);

            // Der Projektbezug BEHAELT seine Kaskade - ein geloeschtes Projekt nimmt
            // seine Kosten weiterhin mit.
            Assert.Contains("REFERENCES \"Tab_Projekt\" (\"ID\") ON UPDATE CASCADE ON DELETE CASCADE",
                            ProjektWerteLoeschschutz.SQL_TABELLE, StringComparison.Ordinal);

            // STRICT und AUTOINCREMENT bleiben, wie sie waren.
            Assert.EndsWith(") STRICT", ProjektWerteLoeschschutz.SQL_TABELLE, StringComparison.Ordinal);
            Assert.Contains("\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT",
                            ProjektWerteLoeschschutz.SQL_TABELLE, StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Hilfsname entsteht aus dem echten Namen; im Hilfs-CREATE kommt der echte
        /// Name nur noch als Teil des Hilfsnamens vor.
        /// </summary>
        [Fact]
        public void Die_Neuanlage_traegt_den_Hilfsnamen()
        {
            string neu = ProjektWerteLoeschschutz.Neuanlage();

            Assert.Equal(ProjektWerteLoeschschutz.TABELLE + "_neu", ProjektWerteLoeschschutz.HILFSTABELLE);
            Assert.Contains(ProjektWerteLoeschschutz.HILFSTABELLE, neu, StringComparison.Ordinal);
            Assert.DoesNotContain(" \"" + ProjektWerteLoeschschutz.TABELLE + "\" (", neu,
                                  StringComparison.Ordinal);

            // EINE Quelle: derselbe Text, nur der Name ist getauscht.
            Assert.Equal(ProjektWerteLoeschschutz.SQL_TABELLE,
                         neu.Replace(" \"" + ProjektWerteLoeschschutz.HILFSTABELLE + "\" (",
                                     " \"" + ProjektWerteLoeschschutz.TABELLE + "\" ("));
        }

        /// <summary>
        /// Die Spaltenliste des Kopierschritts deckt sich mit dem CREATE-Text — sie wird
        /// namentlich genannt und nicht als <c>SELECT *</c> geschrieben.
        /// </summary>
        [Fact]
        public void Die_Spaltenliste_deckt_sich_mit_dem_CREATE_Text()
        {
            Assert.Equal(24, ProjektWerteLoeschschutz.SPALTEN.Length);
            foreach (string spalte in ProjektWerteLoeschschutz.SPALTEN)
                Assert.Contains("\"" + spalte + "\" ", ProjektWerteLoeschschutz.SQL_TABELLE,
                                StringComparison.Ordinal);

            string uebernahme = ProjektWerteLoeschschutz.Uebernahme(
                ProjektWerteLoeschschutz.HILFSTABELLE, ProjektWerteLoeschschutz.TABELLE);
            Assert.DoesNotContain("SELECT *", uebernahme, StringComparison.Ordinal);
            Assert.Contains(ProjektWerteLoeschschutz.Spaltenliste(), uebernahme, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Anweisungen stehen in der Reihenfolge des Rezepts, und die Umbenennung
        /// steht ZWISCHEN den beiden <c>legacy_alter_table</c>-Klammern — sonst
        /// scheiterte sie an der Sicht <c>Abfrage_Kostenfaktoren</c>.
        /// </summary>
        [Fact]
        public void Die_Anweisungen_stehen_in_der_Reihenfolge_des_Rezepts()
        {
            var sql = new List<string>();
            foreach (KeyValuePair<string, string> a in ProjektWerteLoeschschutz.Anweisungen)
                sql.Add(a.Value);

            Assert.Equal(10 + ProjektWerteLoeschschutz.SQL_INDIZES.Length, sql.Count);
            Assert.StartsWith("DROP TABLE IF EXISTS", sql[0], StringComparison.Ordinal);
            Assert.StartsWith("CREATE TABLE IF NOT EXISTS", sql[1], StringComparison.Ordinal);
            Assert.StartsWith("INSERT INTO", sql[2], StringComparison.Ordinal);
            Assert.StartsWith("DELETE FROM sqlite_sequence", sql[3], StringComparison.Ordinal);
            Assert.StartsWith("INSERT INTO sqlite_sequence", sql[4], StringComparison.Ordinal);
            Assert.StartsWith("DROP TABLE IF EXISTS", sql[5], StringComparison.Ordinal);
            Assert.Equal("PRAGMA legacy_alter_table = ON", sql[6]);
            Assert.StartsWith("ALTER TABLE", sql[7], StringComparison.Ordinal);
            Assert.Equal("PRAGMA legacy_alter_table = OFF", sql[8]);
            Assert.StartsWith("UPDATE sqlite_sequence", sql[9], StringComparison.Ordinal);

            for (int i = 0; i < ProjektWerteLoeschschutz.SQL_INDIZES.Length; i++)
                Assert.Equal(ProjektWerteLoeschschutz.SQL_INDIZES[i], sql[10 + i]);
        }

        // =============================================================================
        //  Teil 2 — die Wache auf der ausgelieferten Testdatenbank
        // =============================================================================

        /// <summary>
        /// DIE WACHE: Der Fremdschluessel auf <c>Tab_Kostenfaktor</c> traegt kein
        /// <c>CASCADE</c> mehr — und die Kaskade greift auch tatsaechlich nicht mehr.
        /// </summary>
        [Fact]
        public void Der_Fremdschluessel_auf_den_Katalog_loescht_nicht_mehr_weiter()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            DataRow eintrag = Fremdschluessel(ProjektWerteLoeschschutz.KATALOG);
            Assert.NotNull(eintrag);
            Assert.NotEqual("CASCADE", Convert.ToString(eintrag["on_delete"]));
            Assert.Equal(ProjektWerteLoeschschutz.LOESCHREGEL, Convert.ToString(eintrag["on_delete"]));
            Assert.Equal("CASCADE", Convert.ToString(eintrag["on_update"]));

            // Und die Probe aufs Exempel: Der DELETE des Dialogs wird von der Datenbank
            // abgewiesen, statt Projektpositionen mitzunehmen.
            long vorher = Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen());
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE StammID = " +
                             WaermepumpeAggregat) > 0);

            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren("DELETE FROM Tab_Kostenfaktor WHERE StammID = ?",
                             new DbParam("@sid", WaermepumpeAggregat));
                v.Commit();
            });

            Assert.Equal(vorher, Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen()));
        }

        /// <summary>
        /// Der Projektbezug BEHAELT seine Kaskade: Ein geloeschtes Projekt nimmt seine
        /// Kostenpositionen weiterhin mit. Der Schritt fasst genau EINEN der fuenf
        /// Fremdschluessel an.
        /// </summary>
        [Fact]
        public void Die_vier_anderen_Fremdschluessel_bleiben_wie_sie_waren()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            DataTable liste = DataRepository.GetDataTable(
                "SELECT * FROM pragma_foreign_key_list('" + ProjektWerteLoeschschutz.TABELLE + "')");
            Assert.Equal(5, liste.Rows.Count);

            Assert.Equal("CASCADE", Konto("Tab_Projekt", "on_delete"));
            Assert.Equal("CASCADE", Konto("Tab_Projekt", "on_update"));
            Assert.Equal("NO ACTION", Konto("Tab_KostenKomponente", "on_delete"));
            Assert.Equal("CASCADE", Konto("Tab_KostenKomponente", "on_update"));
            Assert.Equal("NO ACTION", Konto("Tab_KostenGruppenKatalog", "on_delete"));
            Assert.Equal("NO ACTION", Konto("Tab_Nutzungsdauer", "on_delete"));
        }

        // =============================================================================
        //  Teil 3 — die Migration selbst, vom alten Stand aus
        // =============================================================================

        /// <summary>
        /// Der vollstaendige Nachweis: Auf einer Kopie wird der ALTE Stand hergestellt
        /// (derselbe CREATE, aber mit <c>ON DELETE CASCADE</c>), dann laeuft Schritt 81.
        /// Danach ist die Regel geaendert, jede Zeile steht Zeichen fuer Zeichen an ihrem
        /// Platz, der AUTOINCREMENT-Stand ist derselbe, die fuenf Indizes stehen wieder,
        /// die abhaengige Sicht laesst sich lesen — und ein zweiter Lauf tut nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_baut_um_erhaelt_alles_und_ist_wiederholbar()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            AltenStandHerstellen();
            Assert.Equal(1L, Zahl(ProjektWerteLoeschschutz.Zaehlung()));
            Assert.True(ProjektWerteLoeschschutz.UmbauNoetig());

            long zeilenVorher = Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen());
            long zaehlerVorher = Zahl("SELECT seq FROM sqlite_sequence WHERE name = '" +
                                      ProjektWerteLoeschschutz.TABELLE + "'");
            long sichtVorher = Zahl("SELECT COUNT(*) FROM " + Sicht);
            DataTable vorher = Alles();

            Assert.True(ProjektWerteLoeschschutz.Umbauen(), "Der erste Umbau meldet, er habe nichts getan.");

            // Die Regel.
            Assert.Equal(0L, Zahl(ProjektWerteLoeschschutz.Zaehlung()));
            Assert.Equal(1L, Zahl(ProjektWerteLoeschschutz.ZaehlungNeueRegel()));

            // Die Zeilen - Spalte fuer Spalte.
            DataTable nachher = Alles();
            Assert.Equal(zeilenVorher, Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen()));
            Assert.Equal(vorher.Rows.Count, nachher.Rows.Count);
            for (int i = 0; i < vorher.Rows.Count; i++)
                foreach (string spalte in ProjektWerteLoeschschutz.SPALTEN)
                    Assert.Equal(vorher.Rows[i][spalte], nachher.Rows[i][spalte]);

            // Der Zaehlerstand - sonst kaeme eine vergebene Id ein zweites Mal heraus.
            Assert.Equal(zaehlerVorher, Zahl("SELECT seq FROM sqlite_sequence WHERE name = '" +
                                             ProjektWerteLoeschschutz.TABELLE + "'"));

            // Die fuenf Indizes.
            Assert.Equal(5L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' " +
                                  "AND tbl_name = '" + ProjektWerteLoeschschutz.TABELLE +
                                  "' AND sql IS NOT NULL"));

            // Die abhaengige Sicht - sie ist der Grund fuer den legacy-Modus.
            Assert.Equal(sichtVorher, Zahl("SELECT COUNT(*) FROM " + Sicht));

            // Die Datenbank ist heil.
            Assert.Equal("ok", Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check")));
            Assert.Equal(0, DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count);

            // Wiederholbar: zweiter Lauf ohne jede Wirkung, das Schema bleibt Wort fuer Wort.
            string schema = SchemaText();
            Assert.False(ProjektWerteLoeschschutz.Umbauen(), "Der zweite Umbau hat noch einmal gearbeitet.");
            Assert.False(ProjektWerteLoeschschutz.UmbauNoetig());
            Assert.Equal(schema, SchemaText());
            Assert.Equal(zeilenVorher, Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen()));
        }

        /// <summary>
        /// Der Gegenbeweis, dass es die Kaskade WAR: Auf dem alten Stand nimmt ein
        /// geloeschter Katalogeintrag Projektpositionen mit — nach dem Schritt nicht mehr.
        /// </summary>
        [Fact]
        public void Auf_dem_alten_Stand_reisst_der_Katalogeintrag_Positionen_mit()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            AltenStandHerstellen();

            long vorher = Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen());
            long eigene = Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE StammID = " +
                               WaermepumpeAggregat);
            Assert.True(eigene > 0);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("DELETE FROM Tab_Kostenfaktor WHERE StammID = ?",
                             new DbParam("@sid", WaermepumpeAggregat));
                v.Commit();
            }

            // GENAU DER BEFUND: Der Katalogeintrag nahm die Positionen mit.
            Assert.Equal(vorher - eigene, Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen()));
        }

        // =============================================================================
        //  Handreichungen
        // =============================================================================

        /// <summary>
        /// Stellt den Stand VOR Schritt 81 her: dieselbe Tabelle, aber mit
        /// <c>ON DELETE CASCADE</c> — wortgleich der Text, den die Datenbank bis zu
        /// diesem Auftrag trug. Gebaut wird er aus der EINEN Quelle, damit die beiden
        /// Staende sich in nichts anderem unterscheiden koennen.
        /// </summary>
        private static void AltenStandHerstellen()
        {
            string alt = ProjektWerteLoeschschutz.SQL_TABELLE.Replace(
                "ON UPDATE CASCADE ON DELETE " + ProjektWerteLoeschschutz.LOESCHREGEL,
                "ON UPDATE CASCADE ON DELETE " + ProjektWerteLoeschschutz.ALTE_LOESCHREGEL);
            Assert.NotEqual(ProjektWerteLoeschschutz.SQL_TABELLE, alt);

            string hilfsname = alt.Replace(" \"" + ProjektWerteLoeschschutz.TABELLE + "\" (",
                                           " \"" + ProjektWerteLoeschschutz.HILFSTABELLE + "\" (");
            string spalten = ProjektWerteLoeschschutz.Spaltenliste();

            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren("PRAGMA defer_foreign_keys = ON");
            v.Ausfuehren("DROP TABLE IF EXISTS \"" + ProjektWerteLoeschschutz.HILFSTABELLE + "\"");
            v.Ausfuehren(hilfsname);
            v.Ausfuehren("INSERT INTO \"" + ProjektWerteLoeschschutz.HILFSTABELLE + "\" (" + spalten +
                         ") SELECT " + spalten + " FROM \"" + ProjektWerteLoeschschutz.TABELLE + "\"");
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = '" +
                         ProjektWerteLoeschschutz.HILFSTABELLE + "'");
            v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) SELECT '" +
                         ProjektWerteLoeschschutz.HILFSTABELLE + "', seq FROM sqlite_sequence " +
                         "WHERE name = '" + ProjektWerteLoeschschutz.TABELLE + "'");
            v.Ausfuehren("DROP TABLE IF EXISTS \"" + ProjektWerteLoeschschutz.TABELLE + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + ProjektWerteLoeschschutz.HILFSTABELLE +
                         "\" RENAME TO \"" + ProjektWerteLoeschschutz.TABELLE + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");
            v.Ausfuehren("UPDATE sqlite_sequence SET name = '" + ProjektWerteLoeschschutz.TABELLE +
                         "' WHERE name = '" + ProjektWerteLoeschschutz.HILFSTABELLE + "'");
            foreach (string index in ProjektWerteLoeschschutz.SQL_INDIZES) v.Ausfuehren(index);
            v.Commit();
        }

        /// <summary>Der Fremdschluesseleintrag auf eine bestimmte Tabelle, oder <c>null</c>.</summary>
        private static DataRow Fremdschluessel(string ziel)
        {
            DataTable liste = DataRepository.GetDataTable(
                "SELECT * FROM pragma_foreign_key_list('" + ProjektWerteLoeschschutz.TABELLE + "')");
            foreach (DataRow zeile in liste.Rows)
                if (string.Equals(Convert.ToString(zeile["table"]), ziel, StringComparison.Ordinal))
                    return zeile;
            return null;
        }

        private static string Konto(string ziel, string spalte)
        {
            DataRow zeile = Fremdschluessel(ziel);
            Assert.NotNull(zeile);
            return Convert.ToString(zeile[spalte]);
        }

        private static DataTable Alles()
        {
            return DataRepository.GetDataTable(
                "SELECT " + ProjektWerteLoeschschutz.Spaltenliste() +
                " FROM \"" + ProjektWerteLoeschschutz.TABELLE + "\" ORDER BY \"ID\"");
        }

        private static string SchemaText()
        {
            return Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT group_concat(sql, '|') FROM sqlite_master WHERE tbl_name = '" +
                ProjektWerteLoeschschutz.TABELLE + "'"));
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return (o == null || o == DBNull.Value)
                ? -1
                : Convert.ToInt64(o, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
