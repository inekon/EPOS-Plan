using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #178 (11.09.2026) — Migrationsschritt <b>74</b>: <c>Tab_SpeicherAuslegung</c>
    /// wird eine <b>STRICT</b>-Tabelle.
    ///
    /// <para>Beim Nachweis des iOS-Laufs 41 gezählt: <c>Kenndaten_Test.sqlite</c> führt
    /// 119 Tabellen, davon 117 STRICT — die zwei ohne sind <c>sqlite_sequence</c> (System)
    /// und <c>Tab_SpeicherAuslegung</c> aus Schritt 73. SQLite kennt kein
    /// <c>ALTER TABLE … STRICT</c>, also baut
    /// <see cref="SpeicherAuslegungStrict"/> die Tabelle nach dem Rezept des Handbuchs
    /// neu auf.</para>
    ///
    /// <para>Geprüft wird beides: die TEXTE (Zielstand, <c>STRICT</c> im CREATE,
    /// Spaltenliste, Hilfsname) ohne Datenbank — und der UMBAU selbst auf einer
    /// Arbeitskopie: leere Tabelle, gefüllte Tabelle mit Umlauten und langer Nutzlast,
    /// Wiederholbarkeit, der eindeutige Index danach und die Rückrollung, wenn ein Wert
    /// den STRICT-Typ verletzt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class Migration74Tests
    {
        // =============================================================================
        //  Teil 1 — die Texte. Ohne Datenbank entscheidbar.
        // =============================================================================

        /// <summary>Der Zielstand ist 74, und der CREATE-Text trägt <c>STRICT</c>.</summary>
        [Fact]
        public void Der_Zielstand_ist_74_und_der_CREATE_Text_traegt_STRICT()
        {
            Assert.True(SchemaStand.Zielversion >= 74,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 74.");
            Assert.EndsWith(") STRICT", SpeicherAuslegungCtrl.SQL_TABELLE, StringComparison.Ordinal);
            Assert.Contains(SpeicherAuslegungStrict.TABELLE, SpeicherAuslegungCtrl.SQL_TABELLE,
                            StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Hilfsname entsteht aus dem echten Namen und steht in keiner anderen
        /// Anweisung als denen des Umbaus — sonst bliebe eine halbfertige Tabelle unter
        /// einem Namen stehen, den niemand mehr sucht.
        /// </summary>
        [Fact]
        public void Die_Neuanlage_traegt_den_Hilfsnamen_und_STRICT()
        {
            string neu = SpeicherAuslegungStrict.Neuanlage();

            Assert.Equal(SpeicherAuslegungStrict.TABELLE + "_neu", SpeicherAuslegungStrict.HILFSTABELLE);
            Assert.Contains(SpeicherAuslegungStrict.HILFSTABELLE, neu, StringComparison.Ordinal);
            Assert.EndsWith(") STRICT", neu, StringComparison.Ordinal);

            // Der echte Name darf im Hilfs-CREATE nur noch als Teil des Hilfsnamens
            // vorkommen - sonst legte der Schritt die Tabelle unter ihrem eigenen Namen
            // ein zweites Mal an.
            Assert.DoesNotContain(" " + SpeicherAuslegungStrict.TABELLE + " (", neu,
                                  StringComparison.Ordinal);

            // Und es bleibt EINE Quelle: derselbe Text, nur der Name ist getauscht.
            Assert.Equal(SpeicherAuslegungCtrl.SQL_TABELLE,
                         neu.Replace(" " + SpeicherAuslegungStrict.HILFSTABELLE + " (",
                                     " " + SpeicherAuslegungStrict.TABELLE + " ("));
        }

        /// <summary>
        /// Die Spaltenliste des Kopierschritts deckt sich mit dem CREATE-Text. Sie wird
        /// NAMENTLICH genannt und nicht als <c>SELECT *</c> geschrieben — deshalb muss
        /// sie geprüft werden, und zwar gegen die eine Quelle.
        /// </summary>
        [Fact]
        public void Die_Spaltenliste_deckt_sich_mit_dem_CREATE_Text()
        {
            Assert.Equal(6, SpeicherAuslegungStrict.SPALTEN.Length);
            foreach (string spalte in SpeicherAuslegungStrict.SPALTEN)
                Assert.Contains(spalte + " ", SpeicherAuslegungCtrl.SQL_TABELLE, StringComparison.Ordinal);

            string uebernahme = SpeicherAuslegungStrict.Uebernahme(
                SpeicherAuslegungStrict.HILFSTABELLE, SpeicherAuslegungStrict.TABELLE);
            Assert.DoesNotContain("SELECT *", uebernahme, StringComparison.Ordinal);
            Assert.Contains(SpeicherAuslegungStrict.Spaltenliste(), uebernahme, StringComparison.Ordinal);
        }

        /// <summary>
        /// Sechs Anweisungen in der Reihenfolge des Rezepts: Rest abräumen, Hilfstabelle
        /// anlegen, Zeilen übernehmen, alte Tabelle löschen, umbenennen, Index neu.
        /// </summary>
        [Fact]
        public void Die_sechs_Anweisungen_stehen_in_der_Reihenfolge_des_Rezepts()
        {
            var sql = new System.Collections.Generic.List<string>();
            foreach (var a in SpeicherAuslegungStrict.Anweisungen) sql.Add(a.Value);

            Assert.Equal(6, sql.Count);
            Assert.StartsWith("DROP TABLE IF EXISTS", sql[0], StringComparison.Ordinal);
            Assert.StartsWith("CREATE TABLE", sql[1], StringComparison.Ordinal);
            Assert.StartsWith("INSERT INTO", sql[2], StringComparison.Ordinal);
            Assert.StartsWith("DROP TABLE IF EXISTS", sql[3], StringComparison.Ordinal);
            Assert.StartsWith("ALTER TABLE", sql[4], StringComparison.Ordinal);
            Assert.Equal(SpeicherAuslegungCtrl.SQL_INDEX, sql[5]);
        }

        // =============================================================================
        //  Teil 2 — der Umbau auf einer Arbeitskopie.
        // =============================================================================

        /// <summary>
        /// Eine LEERE Tabelle ohne STRICT wird umgebaut; danach trägt sie STRICT und den
        /// eindeutigen Index, und ein zweiter Lauf tut nichts mehr.
        /// </summary>
        [Fact]
        public void Eine_leere_Tabelle_ohne_STRICT_wird_umgebaut_und_der_Schritt_ist_wiederholbar()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            AltenStandHerstellen();
            Assert.False(IstStrict(), "Der alte Stand trägt fälschlich schon STRICT.");
            Assert.True(SpeicherAuslegungStrict.UmbauNoetig());

            Assert.True(SpeicherAuslegungStrict.Umbauen(), "Der erste Umbau meldet, er habe nichts getan.");
            Assert.True(IstStrict(), "Die Tabelle trägt nach dem Umbau kein STRICT.");
            Assert.True(IndexVorhanden(), "Der eindeutige Index fehlt nach dem Umbau.");
            Assert.Equal(0L, Zeilen());

            // Wiederholbar: zweiter Lauf ohne jede Wirkung, das Schema bleibt Wort fuer Wort.
            string vorher = SchemaText();
            Assert.False(SpeicherAuslegungStrict.Umbauen(), "Der zweite Umbau hat noch einmal gearbeitet.");
            Assert.False(SpeicherAuslegungStrict.UmbauNoetig());
            Assert.Equal(vorher, SchemaText());
        }

        /// <summary>
        /// Eine GEFÜLLTE Tabelle behält Zeilenzahl, <c>ID</c>, Umlaute, NULL im
        /// Anlagenbezug und die vollständige Nutzlast — Zeichen für Zeichen.
        /// </summary>
        [Fact]
        public void Eine_gefuellte_Tabelle_behaelt_Zeilen_IDs_Umlaute_und_die_volle_Nutzlast()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            AltenStandHerstellen();

            // Die Zeile bekommt alles, was schiefgehen kann: eine von Hand gesetzte ID,
            // Umlaute und ein Sonderzeichen im Bezeichner, NULL im Anlagenbezug und eine
            // Nutzlast von 200 000 Zeichen (die echten Auslegungsprofile sind gz1:-Base64).
            const int Id = 4711;
            const string Bezeichner = "Größe & Maß — Prüfprofil „Süd\"";
            string daten = "gz1:" + new string('Q', 200000);
            const string Stand = "2026-09-11T00:00:00.0000000+00:00";

            Sql("INSERT INTO Tab_SpeicherAuslegung (ID,ID_Projekt,ID_Energieanlage,Bezeichner,Daten,Stand) " +
                "VALUES (?,?,NULL,?,?,?)",
                new DbParam("@id", Id), new DbParam("@p", Projekt()),
                new DbParam("@n", Bezeichner), new DbParam("@d", daten), new DbParam("@s", Stand));

            DataTable vorher = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, ID_Energieanlage, Bezeichner, Daten, Stand FROM Tab_SpeicherAuslegung ORDER BY ID");

            Assert.True(SpeicherAuslegungStrict.Umbauen());
            Assert.True(IstStrict());

            DataTable nachher = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, ID_Energieanlage, Bezeichner, Daten, Stand FROM Tab_SpeicherAuslegung ORDER BY ID");

            Assert.Equal(vorher.Rows.Count, nachher.Rows.Count);
            Assert.Equal(1, nachher.Rows.Count);
            for (int i = 0; i < vorher.Rows.Count; i++)
                foreach (string spalte in SpeicherAuslegungStrict.SPALTEN)
                    Assert.Equal(vorher.Rows[i][spalte], nachher.Rows[i][spalte]);

            Assert.Equal(Id, Convert.ToInt32(nachher.Rows[0]["ID"]));
            Assert.Equal(Bezeichner, Convert.ToString(nachher.Rows[0]["Bezeichner"]));
            Assert.Equal(daten, Convert.ToString(nachher.Rows[0]["Daten"]));
            Assert.Equal(DBNull.Value, nachher.Rows[0]["ID_Energieanlage"]);

            // Der eindeutige Index ist wieder da und wirkt: derselbe Bezeichner im selben
            // Projekt ohne Anlagenbezug laesst sich kein zweites Mal anlegen.
            Assert.True(IndexVorhanden());
            Assert.ThrowsAny<Exception>(() =>
                Sql("INSERT INTO Tab_SpeicherAuslegung (ID_Projekt,ID_Energieanlage,Bezeichner,Daten,Stand) " +
                    "VALUES (?,NULL,?,?,?)",
                    new DbParam("@p", Projekt()), new DbParam("@n", Bezeichner),
                    new DbParam("@d", "gz1:x"), new DbParam("@s", Stand)));
        }

        /// <summary>
        /// Der Gegenbeweis, dass die Transaktionsklammer trägt: Steht in der Spalte
        /// <c>Daten</c> ein BLOB — in einer Tabelle OHNE STRICT geht das durch —, weist
        /// die STRICT-Zieltabelle den Wert ab. Dann bleibt die ALTE Tabelle mit ihrer
        /// Zeile stehen, statt dass der Umbau auf halbem Weg endet.
        ///
        /// <para>Genau dieser Fall ist der Grund für den Schritt: Eine Tabelle ohne
        /// STRICT nimmt in einer TEXT-Spalte alles an.</para>
        /// </summary>
        [Fact]
        public void Ein_BLOB_in_einer_TEXT_Spalte_laesst_den_Umbau_zurueckrollen()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            AltenStandHerstellen();

            // Der BLOB kommt als SQL-Literal, damit ihn keine Parameternormalisierung
            // unterwegs in Text verwandelt.
            Sql("INSERT INTO Tab_SpeicherAuslegung (ID,ID_Projekt,ID_Energieanlage,Bezeichner,Daten,Stand) " +
                "VALUES (815,?,NULL,'Blobprofil',x'01020304','2026-09-11T00:00:00.0000000+00:00')",
                new DbParam("@p", Projekt()));

            Assert.Equal("blob", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT typeof(Daten) FROM Tab_SpeicherAuslegung WHERE ID = 815")));

            Assert.ThrowsAny<Exception>(() => SpeicherAuslegungStrict.Umbauen());

            // Zurueckgerollt: die alte Tabelle steht unveraendert, die Hilfstabelle nicht.
            Assert.False(IstStrict(), "Die alte Tabelle ist verschwunden - der Rollback trug nicht.");
            Assert.Equal(1L, Zeilen());
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Tab_SpeicherAuslegung_neu'")));
        }

        // =============================================================================
        //  Handreichungen
        // =============================================================================

        /// <summary>
        /// Stellt den Stand VOR dem Schritt 74 her: dieselbe Tabelle, aber ohne
        /// <c>STRICT</c> — wortgleich der Text, den Schritt 73 bis zu diesem Auftrag
        /// ausgeführt hat.
        /// </summary>
        private static void AltenStandHerstellen()
        {
            string ohneStrict = SpeicherAuslegungCtrl.SQL_TABELLE
                .Substring(0, SpeicherAuslegungCtrl.SQL_TABELLE.Length - " STRICT".Length);

            using var db = DataRepository.Vorgang();
            db.Ausfuehren("DROP TABLE IF EXISTS Tab_SpeicherAuslegung");
            db.Ausfuehren(ohneStrict);
            db.Ausfuehren(SpeicherAuslegungCtrl.SQL_INDEX);
            db.Commit();
        }

        /// <summary>Irgendein vorhandenes Projekt — der Fremdschlüssel verlangt eines.</summary>
        private static int Projekt()
        {
            return Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Projekt ORDER BY ID LIMIT 1"));
        }

        private static void Sql(string sql, params DbParam[] parameter)
        {
            using var db = DataRepository.Vorgang();
            db.Ausfuehren(sql, parameter);
            db.Commit();
        }

        private static bool IstStrict()
        {
            return Convert.ToInt64(DataRepository.ExecuteScalar(SpeicherAuslegungStrict.Zaehlung())) == 0;
        }

        private static string SchemaText()
        {
            return Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT group_concat(sql, '|') FROM sqlite_master WHERE tbl_name = 'Tab_SpeicherAuslegung'"));
        }

        private static bool IndexVorhanden()
        {
            return Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'idx_SpeicherAuslegung'")) == 1;
        }

        private static long Zeilen()
        {
            return Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_SpeicherAuslegung"));
        }
    }
}
