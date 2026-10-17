using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die fünf Tabellen der Wirtschaftlichkeit OHNE die Anlage-DDL in
    /// <see cref="WirtschaftlichkeitCtrl"/> (Muster: <c>BerichtCtrlKonfigurationTests</c>,
    /// Konzept Berichtsvorlagen, Etappe BV-E0).
    ///
    /// <para><b>Die Lage.</b> <c>Tab_ProjektWirtschaftlichkeit</c>,
    /// <c>Tab_ErgebnisWirtschaftlichkeit</c>, <c>Tab_ErgebnisWirtSensitivitaet</c>,
    /// <c>Tab_ProjektTarif</c> und <c>Tab_ErgebnisStromMatrix</c> stehen im Grundschema
    /// (<c>sql/schema/001_grundschema.sql</c>), aus dem jede Datenbank hervorgeht, alle fünf
    /// <c>STRICT</c>. Parameter- und Tariftabelle tragen ihren Fremdschlüssel auf
    /// <c>Tab_Projekt</c> mit <c>ON DELETE CASCADE</c> seit dem Grundschema, die drei
    /// Ergebnistabellen bekommen ihn in Schemaschritt 96 (<see cref="ProjektFremdschluessel"/>).
    /// Gemessen an der Testdatenbank: <c>PRAGMA foreign_key_list</c> liefert für jede der fünf
    /// genau eine Zeile <c>Tab_Projekt / ID_Projekt → ID / ON DELETE CASCADE</c>.
    /// Der Controller legt deshalb keine Tabelle an — eine solche Anlage entstünde ohne Beziehung
    /// und ohne <c>STRICT</c>, und Schritt 96 bräche an ihr ab
    /// (<see cref="ProjektFremdschluessel.Zieltext"/>) — und zieht keine Spalte nach: Die vier
    /// Ergebnisspalten ohne Grundschema legt der Schemaschritt
    /// <see cref="ErgebnisWirtschaftlichkeitSpaltenSchema"/> an.</para>
    ///
    /// <para><b>Geprüft wird an einer Arbeitskopie:</b> das Schema der fünf Tabellen; dass
    /// Speichern und Laden ohne die Anlage-DDL laufen; dass nach dem Löschen eines Projekts in
    /// keiner der fünf Tabellen eine Zeile zurückbleibt — über den Löschweg der Anwendung
    /// (<see cref="ProjektCtrl.LoeschenMitVorarbeiten"/>, der diese Tabellen nicht von Hand
    /// leert) und, als Beleg dafür, dass die Kaskade allein genügt, über ein nacktes
    /// <c>DELETE</c>; und dass der Ladeweg weder eine fehlende Tabelle anlegt noch eine
    /// entfernte Spalte zurückholt und die Quelle des Controllers keine DDL mehr führt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WirtschaftlichkeitCtrlTabellenTests
    {
        /// <summary>Die fünf Tabellen, die der Controller führt.</summary>
        private static readonly string[] TABELLEN =
        {
            WirtschaftlichkeitCtrl.TAB_PARAMETER,
            WirtschaftlichkeitCtrl.TAB_ERGEBNIS,
            WirtschaftlichkeitCtrl.TAB_SENS,
            WirtschaftlichkeitCtrl.TAB_TARIF,
            WirtschaftlichkeitCtrl.TAB_MATRIX
        };

        /// <summary>
        /// DAS SCHEMA: Jede der fünf Tabellen ist <c>STRICT</c> und trägt genau eine
        /// Beziehung <c>ID_Projekt → Tab_Projekt.ID</c> mit <c>ON DELETE CASCADE</c>; beide
        /// Zugriffswege des Kerns (<see cref="DataRepository"/> und <see cref="StilleDb"/>)
        /// schalten die Fremdschlüssel ein.
        /// </summary>
        [Fact]
        public void Die_fuenf_Tabellen_sind_STRICT_und_haengen_mit_Kaskade_am_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (string tabelle in TABELLEN)
            {
                string sql = Text(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                    new DbParam("@t", tabelle)));
                Assert.False(string.IsNullOrEmpty(sql), tabelle + " fehlt in der Testdatenbank.");
                Assert.True(sql.TrimEnd().EndsWith("STRICT", StringComparison.OrdinalIgnoreCase),
                            tabelle + " ist nicht STRICT: " + sql);

                DataTable beziehungen = DataRepository.GetDataTable(
                    "PRAGMA foreign_key_list(\"" + tabelle + "\")");
                Assert.NotNull(beziehungen);
                DataRow beziehung = Assert.Single(beziehungen.Rows.Cast<DataRow>());
                Assert.Equal("Tab_Projekt", Text(beziehung["table"]));
                Assert.Equal("ID_Projekt", Text(beziehung["from"]));
                Assert.Equal("ID", Text(beziehung["to"]));
                Assert.Equal("CASCADE", Text(beziehung["on_delete"]));
            }

            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar("PRAGMA foreign_keys"),
                                             CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(StilleDb.Scalar("PRAGMA foreign_keys"),
                                             CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// DER BELEG FÜR DIE KASKADE: ein nacktes <c>DELETE FROM Tab_Projekt</c> — ohne
        /// <see cref="ProjektCtrl"/> und seine Vorarbeiten — nimmt die Zeilen aller fünf
        /// Tabellen mit.
        /// </summary>
        [Fact]
        public void Die_Kaskade_nimmt_alle_fuenf_Tabellen_auch_ohne_Vorarbeit_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ProjektMitZeilenAnlegen("Kaskade Wirtschaftlichkeit");
            foreach (string tabelle in TABELLEN)
                Assert.Equal(1L, Zeilen(tabelle, id));

            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Projekt WHERE ID = ?",
                                                  new DbParam("@id", id)));

            foreach (string tabelle in TABELLEN)
                Assert.Equal(0L, Zeilen(tabelle, id));
        }

        /// <summary>
        /// Der Löschweg der Anwendung: Speichern ohne Anlage-DDL, Projekt löschen — danach steht
        /// weder eine Zeile des Projekts noch irgendeine Waise in einer der fünf Tabellen.
        /// <see cref="ProjektCtrl"/> leert diese Tabellen nicht von Hand; die Beziehung tut es.
        /// </summary>
        [Fact]
        public void Der_Loeschweg_der_Anwendung_laesst_in_keiner_der_fuenf_Tabellen_eine_Zeile_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string NAME = "Loeschweg Wirtschaftlichkeit";
            int id = ProjektMitZeilenAnlegen(NAME);

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(id, NAME);

            Assert.Equal(LoeschStand.Geloescht, befund.Stand);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?", id));
            foreach (string tabelle in TABELLEN)
            {
                Assert.Equal(0L, Zeilen(tabelle, id));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" t WHERE NOT EXISTS " +
                                      "(SELECT 1 FROM Tab_Projekt p WHERE p.ID = t.ID_Projekt)"));
            }
        }

        /// <summary>
        /// KEINE VORSORGE MEHR: Der Ladeweg (<see cref="WirtschaftlichkeitCtrl.LadeParameter"/>,
        /// <see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/>) legt eine fehlende Tabelle NICHT an
        /// (sie entstünde ohne <c>STRICT</c> und ohne Beziehung, und Schritt 96 bräche an ihr ab)
        /// und holt keine Spalte zurück, die ein Schemaschritt entfernt hat —
        /// <c>Aufschlaege_Anwenden</c> steht noch in der Liste
        /// <see cref="SchemaKatalog.Schritt21_Tarifmodell"/>, fiel aber mit Schritt 85. Die vier
        /// Ergebnisspalten ohne Grundschema stehen, weil der Schemaschritt sie anlegt.
        /// </summary>
        [Fact]
        public void Der_Ladeweg_legt_keine_Tabelle_an_und_holt_keine_entfernte_Spalte_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string parameter = WirtschaftlichkeitCtrl.TAB_PARAMETER;
            string entfernt = StrompreisAltspalten.SPALTE_AUFSCHLAEGE_ANWENDEN;
            Assert.False(DataRepository.SpalteVorhanden(parameter, entfernt),
                         "Die Testdatenbank führt " + entfernt + " noch — Schemaschritt 85 fehlt.");

            string tabelle = WirtschaftlichkeitCtrl.TAB_SENS;
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + tabelle + "\"");
            Assert.False(StilleDb.TabelleVorhanden(tabelle), "DROP TABLE hat nicht gegriffen.");

            var ctrl = new WirtschaftlichkeitCtrl();
            ctrl.LadeParameter(1030);
            ctrl.LadeErgebnisse(new System.Collections.Generic.List<int> { 1030 });
            ctrl.LadeSensitivitaet(new System.Collections.Generic.List<int> { 1030 });

            Assert.False(DataRepository.SpalteVorhanden(parameter, entfernt),
                         "Der Ladeweg hat " + entfernt + " zurückgeholt " +
                         "(Nachzug aus SchemaKatalog.Schritt21_Tarifmodell).");
            Assert.False(StilleDb.TabelleVorhanden(tabelle),
                         "Der Ladeweg hat " + tabelle + " angelegt — ohne STRICT und " +
                         "ohne Fremdschlüssel; Schemaschritt 96 bräche an dieser Tabelle ab.");

            Assert.True(ErgebnisWirtschaftlichkeitSpaltenSchema.Vollstaendig(),
                        "Die vier Ergebnisspalten des Schemaschritts fehlen in der Testdatenbank.");
        }

        /// <summary>
        /// DIE QUELLE: <c>WirtschaftlichkeitCtrl.cs</c> führt keine Spalten-DDL mehr — kein
        /// <c>ALTER TABLE</c>, kein <c>AlterTableAddColumn</c>, kein <c>SpalteSicher</c>, keine
        /// <c>Vorsorgewarnung</c>. Ein neuer Nachzug gehört in einen Schemaschritt.
        /// </summary>
        [Fact]
        public void Der_Controller_fuehrt_keine_Spalten_DDL()
        {
            string quelle = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Wurzel(), "EPOS.Kern", "Allgemein", "Wirtschaftlichkeit", "WirtschaftlichkeitCtrl.cs"));
            foreach (string verboten in new[] { "ALTER TABLE", "AlterTableAddColumn", "SpalteSicher(",
                                                "StelleTabellenSicher", "Vorsorgewarnung" })
                Assert.False(quelle.Contains(verboten, StringComparison.Ordinal),
                             "WirtschaftlichkeitCtrl.cs enthält wieder „" + verboten + "“.");
        }

        private static string Wurzel([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = "")
        {
            string ordner = System.IO.Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !System.IO.File.Exists(System.IO.Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = System.IO.Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        /// <summary>
        /// Legt ein leeres Projekt an und füllt jede der fünf Tabellen mit genau einer Zeile:
        /// Parameter und Tarif über den Controller (Speichern und Laden ohne Anlage-DDL), die
        /// drei Ergebnistabellen über das nackte <c>INSERT</c>, weil der Rechenweg dafür einen
        /// vollständigen Lauf bräuchte.
        /// </summary>
        private static int ProjektMitZeilenAnlegen(string name)
        {
            int id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                new[] { new DbParam("@name", name) });
            Assert.True(id > 0, "Das Probeprojekt liess sich nicht anlegen.");

            var ctrl = new WirtschaftlichkeitCtrl();

            WirtschaftlichkeitParameter p = ctrl.LadeParameter(id);
            p.Zinssatz = 4.25;
            Assert.True(ctrl.SpeichereParameter(p),
                        "Speichern der Parameter ist fehlgeschlagen: " + ctrl.Speicherfehler);
            Assert.Equal(4.25, ctrl.LadeParameter(id).Zinssatz);

            TarifParameter t = ctrl.LadeTarif(id);
            t.Aktiv = true;
            Assert.True(ctrl.SpeichereTarif(t),
                        "Speichern des Tarifs ist fehlgeschlagen: " + ctrl.Speicherfehler);
            Assert.True(ctrl.LadeTarif(id).Aktiv);

            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS + " (ID_Projekt, Szenario) VALUES (?, ?)",
                new DbParam("@p", id), new DbParam("@s", "Probe")));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO " + WirtschaftlichkeitCtrl.TAB_SENS + " (ID_Projekt, Parameter) VALUES (?, ?)",
                new DbParam("@p", id), new DbParam("@s", "Probe")));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO " + WirtschaftlichkeitCtrl.TAB_MATRIX + " (ID_Projekt, Zone) VALUES (?, ?)",
                new DbParam("@p", id), new DbParam("@s", "Probe")));
            return id;
        }

        private static long Zeilen(string tabelle, int idProjekt)
        {
            return Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE ID_Projekt = ?", idProjekt);
        }

        private static long Zahl(string sql, params object[] werte)
        {
            DbParam[] parameter = werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray();
            object wert = DataRepository.ExecuteScalar(sql, parameter);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
        {
            return Convert.ToString(wert, CultureInfo.InvariantCulture);
        }
    }
}
