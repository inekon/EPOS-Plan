using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Schemaschritts 76</b> — „ein Preis und ein Emissionssatz je
    /// Energieträger im Projekt", ab diesem Schritt von der Datenbank gehalten und nicht
    /// mehr allein von der Anwendungslogik (Auftrag #278, Anwenderentscheid vom
    /// 15.09.2026; Ausgangslage aus Auftrag #268).
    ///
    /// <para><b>Was hier geprüft wird.</b> Der eindeutige Index steht über genau der
    /// gemessenen Spaltenkombination; die Messlatte führt keine Dublette; ein zweiter
    /// Satz je Träger wird abgewiesen; die Entdoppelung behält die Zeile, die jede
    /// Lesekette schon bisher genommen hat; und die vier Schreibwege — Katalogübernahme,
    /// Variantenanlage, Projektkopie, Projekttransfer — laufen weiter durch, ohne dass
    /// eine SQLite-Ausnahme bis zum Anwender käme.</para>
    ///
    /// <para><b>Warum die Fälle die Datenbank brauchen.</b> Der Index entsteht im
    /// Schemaschritt, und die vier Schreibwege schreiben. Eine Arbeitskopie je FALL
    /// (Regel seit iU9‑W11a): Jeder schreibt, geteilt sähe einer den anderen. Fehlt die
    /// Datei, schweigen die Fälle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektEnergietraegerEindeutigTests
    {
        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030) — es fuehrt Traegerzeilen.</summary>
        private const string PROJEKT = "Referenz BHKW-Kaskade (Regressionstest)";

        // =================================================================
        //  Der Schemaschritt
        // =================================================================

        /// <summary>
        /// Der Index steht, ist EINDEUTIG und liegt ueber (ID_Projekt, ID_Energieträger)
        /// — in dieser Reihenfolge, denn so fragt jede Lesekette.
        /// </summary>
        [Fact]
        public void Der_eindeutige_Index_steht_ueber_Projekt_und_Traeger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(1, Zahl(ProjektEnergietraegerEindeutig.ZaehlungIndex()));

            DataTable liste = DataRepository.GetDataTable(
                "SELECT \"name\", \"unique\" FROM pragma_index_list(?)",
                new DbParam("@t", ProjektEnergietraegerEindeutig.TABELLE));
            bool eindeutig = false;
            foreach (DataRow r in liste.Rows)
                if (string.Equals(Convert.ToString(r["name"]), ProjektEnergietraegerEindeutig.INDEX,
                                  StringComparison.Ordinal))
                    eindeutig = Convert.ToInt32(r["unique"]) == 1;
            Assert.True(eindeutig, "Der Index " + ProjektEnergietraegerEindeutig.INDEX +
                                   " steht nicht oder ist nicht eindeutig.");

            var spalten = new List<string>();
            DataTable info = DataRepository.GetDataTable(
                "SELECT \"name\" FROM pragma_index_info(?) ORDER BY \"seqno\"",
                new DbParam("@i", ProjektEnergietraegerEindeutig.INDEX));
            foreach (DataRow r in info.Rows) spalten.Add(Convert.ToString(r["name"]));

            Assert.Equal(new[] { ProjektEnergietraegerEindeutig.SPALTE_PROJEKT,
                                 ProjektEnergietraegerEindeutig.SPALTE_TRAEGER }, spalten);
        }

        /// <summary>
        /// Die Messlatte selbst ist sauber — sonst haette der Schemaschritt entdoppelt,
        /// und der Referenzlauf waere nicht mehr byte-gleich.
        /// </summary>
        [Fact]
        public void Kein_Projekt_fuehrt_einen_Traeger_zweimal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
            Assert.False(ProjektEnergietraegerEindeutig.EntdoppelungNoetig());
        }

        /// <summary>
        /// DIE EIGENTLICHE ZUSAGE: Ein zweiter Satz desselben Traegers im selben Projekt
        /// kommt nicht mehr in die Datenbank.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Satz_je_Traeger_wird_abgewiesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsteZeile(out int projekt, out int traeger);
            Assert.True(id > 0);

            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren(Nachbau(), new DbParam("@id", id));
                v.Commit();
            });

            // Und der Bestand steht unveraendert da.
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + ProjektEnergietraegerEindeutig.TABELLE +
                                 "\" WHERE \"" + ProjektEnergietraegerEindeutig.SPALTE_PROJEKT +
                                 "\" = " + projekt + " AND \"" +
                                 ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\" = " + traeger));
        }

        /// <summary>
        /// Die Entdoppelung des Schemaschritts auf einem UNSAUBEREN Bestand: Sie behaelt
        /// je Paar die Zeile mit der kleinsten ID — genau die, die SQLite ohne ORDER BY
        /// als erste liefert und die deshalb schon bisher galt. Der Schritt ist damit
        /// ergebnisneutral.
        ///
        /// <para>Der unsaubere Bestand wird hier eigens hergestellt: Ohne den Index
        /// laesst die Datenbank die zweite Zeile wieder zu — genau die Lage jeder
        /// produktiven Datei VOR diesem Schritt.</para>
        /// </summary>
        [Fact]
        public void Die_Entdoppelung_behaelt_die_Zeile_mit_der_kleinsten_Id()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsteZeile(out int projekt, out int traeger);
            Assert.True(id > 0);

            DataRepository.ExecuteNonQuery("DROP INDEX \"" + ProjektEnergietraegerEindeutig.INDEX + "\"");
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.ZaehlungIndex()));

            DataRepository.ExecuteSQL(Nachbau(), new DbParam("@id", id));
            DataRepository.ExecuteSQL(Nachbau(), new DbParam("@id", id));
            Assert.Equal(2, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
            Assert.True(ProjektEnergietraegerEindeutig.EntdoppelungNoetig());

            DataRepository.ExecuteNonQuery(ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN);
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
            Assert.Equal(id, Zahl("SELECT \"ID\" FROM \"" + ProjektEnergietraegerEindeutig.TABELLE +
                                  "\" WHERE \"" + ProjektEnergietraegerEindeutig.SPALTE_PROJEKT +
                                  "\" = " + projekt + " AND \"" +
                                  ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\" = " + traeger));

            // Danach laesst sich der Index anlegen - die Reihenfolge des Schritts.
            DataRepository.ExecuteNonQuery(ProjektEnergietraegerEindeutig.SQL_INDEX);
            Assert.Equal(1, Zahl(ProjektEnergietraegerEindeutig.ZaehlungIndex()));

            // Wiederholbar: ein zweiter Lauf findet nichts mehr.
            DataRepository.ExecuteNonQuery(ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN);
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
        }

        // =================================================================
        //  Die vier Schreibwege
        // =================================================================

        /// <summary>
        /// SCHREIBWEG 1 — Katalogtraeger ins Projekt uebernehmen
        /// (<c>EnergietraegerKatalogCtrl.InsProjekt</c>). Zweimal aufgerufen bleibt es
        /// bei EINER Zeile, und der zweite Aufruf meldet Erfolg statt einer Ausnahme.
        /// </summary>
        [Fact]
        public void Die_Kataloguebernahme_laeuft_zweimal_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(projekt > 0);

            List<EnergyCarrier> frei = EnergietraegerKatalogCtrl.NichtZugeordnete(projekt);
            Assert.NotEmpty(frei);
            int carrier = frei[0].ID;

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(projekt, carrier));
            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(projekt, carrier));

            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + ProjektEnergietraegerEindeutig.TABELLE +
                                 "\" WHERE \"" + ProjektEnergietraegerEindeutig.SPALTE_PROJEKT +
                                 "\" = " + projekt + " AND \"" +
                                 ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\" = " + carrier));
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
        }

        /// <summary>
        /// SCHREIBWEG 2 — die VARIANTENANLAGE. Sie kopiert den Traegerbestand des Stamms;
        /// die Variante traegt danach dieselbe Zahl Zeilen, und keine doppelt.
        /// </summary>
        [Fact]
        public void Die_Variantenanlage_laeuft_weiter_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int stamm = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(stamm > 0);
            int imStamm = ZeilenJeProjekt(stamm);
            Assert.True(imStamm > 0);

            int variante = new VariantenCtrl().AnlegenAusStamm(stamm, PROJEKT, "Eindeutig 278",
                                                               out string fehler);
            Assert.True(variante > 0, fehler);

            Assert.Equal(imStamm, ZeilenJeProjekt(variante));
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
        }

        /// <summary>
        /// SCHREIBWEG 3 — die PROJEKTKOPIE. Sie schreibt in ein NEUES Projekt; ist die
        /// Quelle eindeutig, ist es die Kopie auch.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_laeuft_weiter_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            int quelle = dup.GetProjektId(PROJEKT);
            Assert.True(quelle > 0);
            int inQuelle = ZeilenJeProjekt(quelle);
            Assert.True(inQuelle > 0);

            int neu = dup.Duplizieren(PROJEKT, "Eindeutig 278 Kopie");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.NotEqual(quelle, neu);

            Assert.Equal(inQuelle, ZeilenJeProjekt(neu));
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
        }

        /// <summary>
        /// SCHREIBWEG 4 — der PROJEKTTRANSFER. Die Rundreise legt ein neues Projekt an;
        /// auch dort steht jeder Traeger genau einmal.
        /// </summary>
        [Fact]
        public void Der_Projekttransfer_laeuft_weiter_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(quelle > 0);
            int inQuelle = ZeilenJeProjekt(quelle);
            Assert.True(inQuelle > 0);

            string paket = ordner.Datei("eindeutig278.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            int neu = io.Importieren(paket, "Eindeutig 278 Transfer",
                                     ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.NotEqual(quelle, neu);

            Assert.Equal(inQuelle, ZeilenJeProjekt(neu));
            Assert.Equal(0, Zahl(ProjektEnergietraegerEindeutig.Zaehlung()));
        }

        // =================================================================
        //  Handwerkszeug
        // =================================================================

        /// <summary>
        /// Baut EINE bestehende Zeile ohne ihre <c>ID</c> nach — der kuerzeste Weg zu
        /// einer Dublette, der keine Spaltenliste von Hand pflegt.
        /// </summary>
        private static string Nachbau()
        {
            return "INSERT INTO \"" + ProjektEnergietraegerEindeutig.TABELLE + "\" (\"" +
                   ProjektEnergietraegerEindeutig.SPALTE_PROJEKT + "\", \"" +
                   ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\", \"custom_price_work\") " +
                   "SELECT \"" + ProjektEnergietraegerEindeutig.SPALTE_PROJEKT + "\", \"" +
                   ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\", \"custom_price_work\" " +
                   "FROM \"" + ProjektEnergietraegerEindeutig.TABELLE + "\" WHERE \"ID\" = ?";
        }

        /// <summary>Die erste Zeile der Tabelle samt Projekt und Traeger.</summary>
        private static int ErsteZeile(out int projekt, out int traeger)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID\", \"" + ProjektEnergietraegerEindeutig.SPALTE_PROJEKT + "\", \"" +
                ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\" FROM \"" +
                ProjektEnergietraegerEindeutig.TABELLE + "\" WHERE \"" +
                ProjektEnergietraegerEindeutig.SPALTE_TRAEGER + "\" IS NOT NULL " +
                "ORDER BY \"ID\" LIMIT 1");
            projekt = 0;
            traeger = 0;
            if (dt == null || dt.Rows.Count == 0) return 0;
            projekt = Convert.ToInt32(dt.Rows[0][1]);
            traeger = Convert.ToInt32(dt.Rows[0][2]);
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        private static int ZeilenJeProjekt(int projektId)
        {
            return Zahl("SELECT COUNT(*) FROM \"" + ProjektEnergietraegerEindeutig.TABELLE +
                        "\" WHERE \"" + ProjektEnergietraegerEindeutig.SPALTE_PROJEKT +
                        "\" = " + projektId);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Ein Wegwerf-Ordner fuer das Transferpaket.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad;

            public Arbeitsordner()
            {
                _pfad = Path.Combine(Path.GetTempPath(),
                                     "epos-eindeutig-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(_pfad);
            }

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }
    }
}
