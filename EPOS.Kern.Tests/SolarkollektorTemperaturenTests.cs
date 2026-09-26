using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Vor- und Rücklauf des Solarkollektors entfallen</b> (Anwenderentscheid 26.09.2026,
    /// „Katalogspalten VL/RL entfernen — keine Funktion") — der Schemaschritt
    /// <see cref="SolarkollektorTemperaturen"/> und die Leser, die ein stehengebliebenes Paar
    /// der Anlagenzeile übergehen.
    ///
    /// <para><b>Geprüft wird:</b> die Nummer und das Ziel; dass der Schritt auf dem Stand davor
    /// genau die vier Spalten entfernt, sonst keine Zelle ändert und wiederholbar ist; dass die
    /// Testdatenbank ihn trägt; dass Migration, Werkzeug und Testkopie ihn aus derselben Quelle
    /// fahren; dass die Solarthermie die Systemvorgabe neuer Puffer nicht mehr herunterzieht
    /// (Kessel 70/50 + Solar 45/30 → 70/50), weder Erzeugerkarte noch Hydraulikbild ihr Paar
    /// lesen und die Vorbelegung aus dem Katalog für sie nichts tut.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SolarkollektorTemperaturenTests : IDisposable
    {
        /// <summary>Ein Projekt der Testdatenbank mit Wärmepumpe, Solarthermie und Kessel.</summary>
        private const int PROJEKT = 1026;
        private const int ANLAGE_WP = 14917;
        private const int ANLAGE_SOLAR = 11274;
        private const int ANLAGE_KESSEL = 11275;

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf die Katalogsätze M und A und ist das Ziel.</summary>
        [Fact]
        public void Die_Nummer_folgt_auf_die_Gebaeudesaat_und_ist_das_Ziel()
        {
            Assert.Equal(GebaeudeSaatSchema.SCHRITT + 1, SolarkollektorTemperaturen.SCHRITT);
            Assert.Equal(150, SolarkollektorTemperaturen.SCHRITT);
            Assert.Equal(SolarkollektorTemperaturen.SCHRITT, SchemaStand.Zielversion);
        }

        /// <summary>Vier Spalten, zwei je Tabelle, in fester Reihenfolge.</summary>
        [Fact]
        public void Die_Spalten_sind_Vorlauf_und_Ruecklauf_an_Katalog_und_Projektkopie()
        {
            Assert.Equal(new[]
            {
                "Tab_Solarkollektoren_STAMM.Vorlauf", "Tab_Solarkollektoren_STAMM.Ruecklauf",
                "Tab_Solarkollektoren.Vorlauf", "Tab_Solarkollektoren.Ruecklauf"
            }, SolarkollektorTemperaturen.Spalten.Select(s => s.Key + "." + s.Value));
        }

        /// <summary>Nur der Solarkollektor führt kein Temperaturpaar.</summary>
        [Fact]
        public void Nur_der_Solarkollektor_fuehrt_kein_Temperaturpaar()
        {
            Assert.False(AnlagenTemperaturen.FuehrtTemperaturpaar(WizardItemClass.SOLAR_TYP));
            Assert.False(AnlagenTemperaturen.FuehrtTemperaturpaar(WizardItemClass.REF_SOLAR_TYP));
            Assert.True(AnlagenTemperaturen.FuehrtTemperaturpaar(WizardItemClass.WP_TYP));
            Assert.True(AnlagenTemperaturen.FuehrtTemperaturpaar(WizardItemClass.KESSEL_TYP));
            Assert.True(AnlagenTemperaturen.FuehrtTemperaturpaar(WizardItemClass.BHKW_TYP));

            // Die Systemvorgabe nimmt die Solarthermie nicht mit, die Ladeordnung schon.
            Assert.DoesNotContain(ProjektPuffer.TYP_SOLARTHERMIE.ToString(CultureInfo.InvariantCulture),
                                  ProjektPuffer.SYSTEMVORGABE_TYPEN.Split(','));
            Assert.Contains(ProjektPuffer.TYP_SOLARTHERMIE.ToString(CultureInfo.InvariantCulture),
                            ProjektPuffer.WAERMEERZEUGER_TYPEN.Split(','));
        }

        // =============================================================================
        //  Teil 2 - der Schritt an der Datenbank
        // =============================================================================

        /// <summary>
        /// <b>Auf dem Stand davor</b> (die vier Spalten wieder angelegt und belegt) entfernt der
        /// Schritt genau sie; jede andere Zelle beider Tabellen bleibt, wie sie war. Ein zweiter
        /// Lauf fasst nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_entfernt_genau_die_vier_Spalten_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            string vorherStamm = Abzug(SolarkollektorTemperaturen.TABELLE_STAMM);
            string vorherProjekt = Abzug(SolarkollektorTemperaturen.TABELLE_PROJEKT);
            Assert.NotEqual("", vorherStamm);

            // Den Stand davor herstellen: die Spalten wie im Grundschema, mit Werten.
            foreach (KeyValuePair<string, string> s in SolarkollektorTemperaturen.Spalten)
            {
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Key + "\" ADD COLUMN \"" + s.Value +
                                               "\" INTEGER DEFAULT 0");
                DataRepository.ExecuteNonQuery("UPDATE \"" + s.Key + "\" SET \"" + s.Value + "\" = " +
                                               (s.Value == "Vorlauf" ? "60" : "40"));
            }
            Assert.Equal(4, SolarkollektorTemperaturen.Offen());

            var bericht = new List<string>();
            Assert.Equal(4, SolarkollektorTemperaturen.Ausfuehren(bericht));
            Assert.Equal(4, bericht.Count);
            Assert.Equal(0, SolarkollektorTemperaturen.Offen());
            foreach (KeyValuePair<string, string> s in SolarkollektorTemperaturen.Spalten)
                Assert.False(SolarkollektorTemperaturen.Vorhanden(s.Key, s.Value), s.Key + "." + s.Value);

            Assert.Equal(vorherStamm, Abzug(SolarkollektorTemperaturen.TABELLE_STAMM));
            Assert.Equal(vorherProjekt, Abzug(SolarkollektorTemperaturen.TABELLE_PROJEKT));

            // Wiederholbar: nichts mehr offen, nichts geschrieben.
            Assert.Empty(SolarkollektorTemperaturen.Anweisungen);
            Assert.Equal(0, SolarkollektorTemperaturen.Ausfuehren(null));
            Assert.Equal(vorherStamm, Abzug(SolarkollektorTemperaturen.TABELLE_STAMM));

            // Beide Tabellen bleiben STRICT, die Indizes stehen.
            Assert.Contains("STRICT", Definition(SolarkollektorTemperaturen.TABELLE_STAMM), StringComparison.Ordinal);
            Assert.Contains("STRICT", Definition(SolarkollektorTemperaturen.TABELLE_PROJEKT), StringComparison.Ordinal);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'Tab_Solarkollektoren_ID_Projekt'"));
        }

        /// <summary>Die Arbeitskopie steht auf dem Ziel: keine der vier Spalten, nichts zu tun.</summary>
        [Fact]
        public void Auf_dem_Zielstand_ist_nichts_mehr_zu_tun()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(0, SolarkollektorTemperaturen.Offen());
            Assert.Empty(SolarkollektorTemperaturen.Anweisungen);
        }

        /// <summary>
        /// Katalog, Übernahme ins Projekt und Speicherweg des Aufklappers laufen ohne die Spalten:
        /// der Katalogsatz kopiert sich ins Projekt, der Aufklapper zeigt keinen Schlüssel dafür.
        /// </summary>
        [Fact]
        public void Kopie_ins_Projekt_und_Aufklapper_kommen_ohne_die_Spalten_aus()
        {
            if (!_db.Vorhanden) return;

            object id = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Solarkollektoren_STAMM");
            int stammId = Convert.ToInt32(id, CultureInfo.InvariantCulture);
            SolarkollektorenModel stamm = SolarkollektorenStammCtrl.ReadById(stammId);
            Assert.NotNull(stamm);

            int kopie = new SolarkollektorenCtrl().CopyFromStamm(stammId, 1030);
            Assert.True(kopie > 0, "Die Kopie ins Projekt ist gescheitert.");

            IReadOnlyDictionary<string, string> anzeige =
                SolarkollektorenStammCtrl.KatalogsatzAnzeige(stamm.m_szKollektorname);
            Assert.NotNull(anzeige);
            Assert.False(anzeige.ContainsKey(KatalogBrowserProfil.FeldVorlauf));
            Assert.False(anzeige.ContainsKey(KatalogBrowserProfil.FeldRuecklauf));
        }

        // =============================================================================
        //  Teil 3 - die Leser der Anlagenzeile übergehen das Paar der Solarthermie
        // =============================================================================

        /// <summary>
        /// <b>Kessel 70/50 und Solarthermie 45/30 ergeben die Systemvorgabe 70/50</b> — bis zum
        /// Entscheid zog der Kollektor sie auf 45/50 herunter.
        /// </summary>
        [Fact]
        public void Die_Solarthermie_zieht_die_Systemvorgabe_nicht_herunter()
        {
            if (!_db.Vorhanden) return;

            Paar(ANLAGE_WP, 0, 0);
            Paar(ANLAGE_KESSEL, 70, 50);
            Paar(ANLAGE_SOLAR, 45, 30);

            Assert.Equal(70, PufferSpCtrl.SystemVorlauf(PROJEKT));
            Assert.Equal(50, PufferSpCtrl.SystemRuecklauf(PROJEKT));

            // Ohne Kessel bleibt die Vorgabe leer - die Solarthermie allein stiftet keine.
            Paar(ANLAGE_KESSEL, 0, 0);
            Assert.Null(PufferSpCtrl.SystemVorlauf(PROJEKT));
            Assert.Null(PufferSpCtrl.SystemRuecklauf(PROJEKT));
        }

        /// <summary>Erzeugerkarte und Hydraulikbild lesen das Paar der Solarthermie nicht.</summary>
        [Fact]
        public void Erzeugerkarte_und_Hydraulikbild_uebergehen_das_Paar_der_Solarthermie()
        {
            if (!_db.Vorhanden) return;

            Paar(ANLAGE_SOLAR, 45, 30);
            Paar(ANLAGE_KESSEL, 70, 50);

            AnlagenInfo solar = WErzeugerCtrl.AnlagenMitWp(PROJEKT, WizardItemClass.SOLAR_TYP)
                                             .Single(a => a.ID == ANLAGE_SOLAR);
            Assert.Equal(0, solar.Vorlauf);
            Assert.Equal(0, solar.Ruecklauf);

            AnlagenInfo kessel = WErzeugerCtrl.AnlagenMitWp(PROJEKT, WizardItemClass.KESSEL_TYP)
                                              .Single(a => a.ID == ANLAGE_KESSEL);
            Assert.Equal(70, kessel.Vorlauf);
            Assert.Equal(50, kessel.Ruecklauf);

            Hydraulikbild bild = Hydraulikbild.Lesen(PROJEKT);
            Assert.NotNull(bild);
            Assert.Equal(0, bild.JeId[ANLAGE_SOLAR].Vorlauf);
            Assert.Equal(0, bild.JeId[ANLAGE_SOLAR].Ruecklauf);
            Assert.Equal(70, bild.JeId[ANLAGE_KESSEL].Vorlauf);
        }

        /// <summary>Die Vorbelegung aus dem Katalog tut beim Solarkollektor nichts.</summary>
        [Fact]
        public void Die_Vorbelegung_aus_dem_Katalog_tut_beim_Kollektor_nichts()
        {
            if (!_db.Vorhanden) return;

            object id = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Solarkollektoren_STAMM");
            int stammId = Convert.ToInt32(id, CultureInfo.InvariantCulture);
            var item = new WErzeugerModel { ID_Type = WizardItemClass.SOLAR_TYP, ID_Solar = stammId };

            Assert.False(AnlagenTemperaturen.AusStammsatz(item, stammId));
            Assert.False(AnlagenTemperaturen.AusGeraetekopie(item));
            Assert.Equal(0, item.Vorlauf);
            Assert.Equal(0, item.Ruecklauf);
        }

        // =============================================================================
        //  Teil 4 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und
        /// Testkopie führen den Schritt aus derselben Quelle NACH der Gebäudesaat; die Repo-Datei
        /// trägt ihn (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wSchritt = werkzeug.IndexOf("SolarkollektorTemperaturen.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wSchritt > werkzeug.IndexOf("GebaeudeSaatSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Der Schritt steht im Werkzeug nicht hinter der Gebaeudesaat.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_SOLAR_TEMPERATUREN = SolarkollektorTemperaturen.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDESAAT", StringComparison.Ordinal);
            int ortSchritt = migration.IndexOf("new Schritt(SCHRITT_SOLAR_TEMPERATUREN", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSchritt > ortVorher, "Der Schritt steht nicht hinter 149.");
            Assert.Contains("in SolarkollektorTemperaturen.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vSchritt = vorrichtung.IndexOf("SolarkollektorTemperaturen.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vSchritt > vorrichtung.IndexOf("GebaeudeSaatSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Der Schritt steht in der Testkopie nicht hinter der Gebaeudesaat.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= SolarkollektorTemperaturen.SCHRITT);
            foreach (KeyValuePair<string, string> s in SolarkollektorTemperaturen.Spalten)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('" + s.Key + "') WHERE name = '" + s.Value + "'";
                Assert.Equal(0L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Temperaturpaar einer Anlagenzeile setzen (die Spalte trägt den Umlaut).</summary>
        private static void Paar(int idAnlage, int vorlauf, int ruecklauf)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Vorlauf = ?, [Rücklauf] = ? WHERE ID = ?",
                new DbParam("@v", vorlauf), new DbParam("@r", ruecklauf), new DbParam("@id", idAnlage)));
        }

        /// <summary>
        /// Alle Zellen einer Tabelle als Text, nach ID geordnet — ohne die vier Spalten des
        /// Schritts, damit der Stand davor und danach vergleichbar ist.
        /// </summary>
        private static string Abzug(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" ORDER BY ID");
            if (dt == null) return "";
            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    if (c.ColumnName == "Vorlauf" || c.ColumnName == "Ruecklauf") continue;
                    object v = r[c];
                    sb.Append(c.ColumnName).Append('=')
                      .Append(v == DBNull.Value ? "NULL" : Convert.ToString(v, CultureInfo.InvariantCulture))
                      .Append('|');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static string Definition(string tabelle)
            => Convert.ToString(DataRepository.ExecuteScalar(
                   "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", tabelle)),
                   CultureInfo.InvariantCulture) ?? "";

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
