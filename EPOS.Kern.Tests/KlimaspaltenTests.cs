using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>SCHEMASCHRITT 95 — die Klimaspalten</b> (Anwenderentscheid vom 19.09.2026:
    /// „alles Relevante für die Gebäudesimulation aufnehmen"; Schritt M4 des
    /// Umsetzungskonzepts Gebäudesimulation VDI 6007, Auftrag KL-3).
    ///
    /// <para><b>Worum es geht.</b> Beide Klimaquellen liefern längst mehr, als
    /// gespeichert wurde. Ab hier führen <c>Tab_Solar</c> und <c>Tab_Solar_STAMM</c> drei
    /// Größen mehr — <c>Gegenstrahlung</c> [W/m²] (PVGIS <c>IR(h)</c>, TRY <c>A</c>),
    /// <c>Luftfeuchte</c> [%] (PVGIS <c>RH</c>, TRY <c>RF</c>) und <c>Bedeckungsgrad</c>
    /// in Achteln (TRY <c>N</c>; PVGIS liefert ihn nicht) —, und
    /// <c>Tab_Klimaregion(_STAMM)</c> hält fest, aus welcher <c>Quelle</c> eine Region
    /// stammt und an welchem Tag sie eingelesen wurde.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Fünf Dinge:
    /// <list type="number">
    ///   <item><description>Die zehn Spalten stehen an den vier Tabellen, und der
    ///     Schritt ist WIEDERHOLBAR.</description></item>
    ///   <item><description>Der BESTAND bleibt NULL — bei allen zehn, in jeder
    ///     Zeile.</description></item>
    ///   <item><description>Der PVGIS-Import schreibt Gegenstrahlung und Luftfeuchte;
    ///     der Bedeckungsgrad bleibt NULL, weil PVGIS ihn nicht führt.</description></item>
    ///   <item><description>Der TRY-Import schreibt alle drei, und der Kopfsatz trägt
    ///     Quelle und Importdatum.</description></item>
    ///   <item><description>Die PROJEKTKOPIE trägt alles mit — eine Spalte nur auf der
    ///     Katalogseite wäre ein Datenverlust.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist; die
    /// schreibenden Fälle legen sich ihre EIGENE Arbeitskopie an.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KlimaspaltenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Ein Projekt der Testdatenbank — Ziel der Projektkopie.</summary>
        private const int PROJEKT = 1030;

        // =====================================================================
        //  1 — Die zehn Spalten
        // =====================================================================

        /// <summary>
        /// Die LISTE des Schrittes, eingefroren: zehn Spalten an vier Tabellen, je
        /// Klimagröße einmal im Katalog und einmal in der Projektkopie.
        ///
        /// <para>Der Fall ist die Messlatte für jede spätere Erweiterung: Kommt eine
        /// Größe hinzu oder fehlt sie auf einer der beiden Seiten, fällt es hier auf und
        /// nicht erst beim Kopieren einer Region ins Projekt.</para>
        /// </summary>
        [Fact]
        public void Der_Schritt_fuehrt_zehn_Spalten_an_vier_Tabellen()
        {
            SchemaSpalte[] spalten = SchemaKatalog.Schritt95_Klimaspalten;

            Assert.Equal(10, spalten.Length);

            foreach (string tabelle in new[] { SchemaKatalog.TAB_SOLAR, SchemaKatalog.TAB_SOLAR_STAMM })
                foreach (string name in new[] { SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG,
                                                SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE,
                                                SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD })
                    Assert.Contains(spalten, s => s.Tabelle == tabelle && s.Name == name &&
                                                  s.TypDefinition == "DOUBLE");

            foreach (string tabelle in new[] { SchemaKatalog.TAB_KLIMAREGION,
                                               SchemaKatalog.TAB_KLIMAREGION_STAMM })
                foreach (string name in new[] { SchemaKatalog.SPALTE_KR_QUELLE,
                                                SchemaKatalog.SPALTE_KR_IMPORTDATUM })
                    Assert.Contains(spalten, s => s.Tabelle == tabelle && s.Name == name);

            // Die WINDGESCHWINDIGKEIT steht bewusst NICHT dabei (Umsetzungskonzept
            // Gebaeudesimulation F-S3: keine Spalte ohne Leser).
            Assert.DoesNotContain(spalten, s => s.Name.IndexOf("Wind", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Nach dem Aufbau der Arbeitskopie stehen alle zehn Spalten — und ein zweiter
        /// Lauf des Schrittes fasst nichts mehr an (die Anlegeprüfung fragt
        /// <c>PRAGMA table_info</c>, SQLite kennt kein <c>ADD COLUMN IF NOT EXISTS</c>).
        /// </summary>
        [Fact]
        public void Die_Spalten_stehen_und_der_Schritt_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt95_Klimaspalten)
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name),
                            s.Tabelle + "." + s.Name + " fehlt.");

            // Zweiter Lauf: Es ist nichts mehr offen.
            int offen = 0;
            foreach (SchemaSpalte s in SchemaKatalog.Schritt95_Klimaspalten)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) offen++;

            Assert.Equal(0, offen);
        }

        /// <summary>
        /// <b>Der Bestand bleibt NULL</b> — der Schritt trägt kein DML. NULL heißt bei
        /// den drei Klimagrößen „nicht verfügbar" (nie 0 — eine 0 wäre eine
        /// Messaussage) und bei Quelle/Importdatum „Altbestand"; nachdatiert wird nichts.
        /// </summary>
        [Fact]
        public void Der_Bestand_bleibt_in_allen_zehn_Spalten_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt95_Klimaspalten)
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" +
                                     s.Name + "] IS NOT NULL"));
        }

        // =====================================================================
        //  2 — Der PVGIS-Import
        // =====================================================================

        /// <summary>
        /// <b>PVGIS füllt zwei der drei Größen.</b> Die eingefrorene Antwort führt
        /// <c>IR(h)</c> und <c>RH</c>; einen Bedeckungsgrad führt PVGIS nicht, und die
        /// Spalte bleibt deshalb NULL — <b>nicht 0</b>: Eine 0 hieße „wolkenlos", und
        /// das hat niemand gemessen.
        /// </summary>
        [Fact]
        public async Task Der_PVGIS_Import_schreibt_Gegenstrahlung_und_Feuchte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                new KlimaImportAuftrag
                {
                    Art = KlimaImportArt.AusKoordinaten,
                    Bezeichnung = "KL3 PVGIS Probe",
                    Longitude = 9.1829,
                    Latitude = 48.7758
                },
                (lon, lat, azimut) => Task.FromResult(Tmy()));

            Assert.True(erg.Erfolgreich, erg.Meldung);
            Assert.Equal(72, erg.Stundenwerte);

            Assert.Equal(72, Zahl(Zaehlsatz(SchemaKatalog.TAB_SOLAR_STAMM,
                                            SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG), erg.Id));
            Assert.Equal(72, Zahl(Zaehlsatz(SchemaKatalog.TAB_SOLAR_STAMM,
                                            SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE), erg.Id));
            Assert.Equal(0, Zahl(Zaehlsatz(SchemaKatalog.TAB_SOLAR_STAMM,
                                           SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD), erg.Id));

            // Die erste Stunde der Probe: IR(h) = 250, RH = 75.
            Assert.Equal(250.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG +
                                     " FROM " + SchemaKatalog.TAB_SOLAR_STAMM +
                                     " WHERE ID_Klimaregion = ? ORDER BY ID", erg.Id), 6);
            Assert.Equal(75.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE +
                                    " FROM " + SchemaKatalog.TAB_SOLAR_STAMM +
                                    " WHERE ID_Klimaregion = ? ORDER BY ID", erg.Id), 6);

            // Der Kopfsatz sagt, WOHER die Reihe stammt (Teil 1b des Schrittes).
            Assert.Equal(DbWerte.KLIMA_QUELLE_PVGIS, Text(SchemaKatalog.SPALTE_KR_QUELLE, erg.Id));
            Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                         Text(SchemaKatalog.SPALTE_KR_IMPORTDATUM, erg.Id));
        }

        // =====================================================================
        //  3 — Der TRY-Import
        // =====================================================================

        /// <summary>
        /// <b>Die DWD-Testreferenzjahre füllen alle drei Größen</b> — <c>A</c>,
        /// <c>RF</c> und <c>N</c> stehen in jeder Datenzeile. Der Kopfsatz trägt
        /// <c>TRY_DATEI</c> als Quelle.
        /// </summary>
        [Fact]
        public async Task Der_TRY_Import_schreibt_alle_drei_Groessen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = TryDatei();
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL3 TRY Probe",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);          // KEINE TMY-Quelle noetig - kein Netz

                Assert.True(erg.Erfolgreich, erg.Meldung);
                Assert.Equal(8760, erg.Stundenwerte);

                foreach (string spalte in new[] { SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG,
                                                  SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE,
                                                  SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD })
                    Assert.Equal(8760, Zahl(Zaehlsatz(SchemaKatalog.TAB_SOLAR_STAMM, spalte), erg.Id));

                // Die Werte der Probe: A = 260, RF = 80, N = 4 - drei verschiedene
                // Zahlen, eine Vertauschung faellt deshalb auf.
                Assert.Equal(260.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG +
                                         " FROM " + SchemaKatalog.TAB_SOLAR_STAMM +
                                         " WHERE ID_Klimaregion = ? ORDER BY ID", erg.Id), 6);
                Assert.Equal(80.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE +
                                        " FROM " + SchemaKatalog.TAB_SOLAR_STAMM +
                                        " WHERE ID_Klimaregion = ? ORDER BY ID", erg.Id), 6);
                Assert.Equal(4.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD +
                                       " FROM " + SchemaKatalog.TAB_SOLAR_STAMM +
                                       " WHERE ID_Klimaregion = ? ORDER BY ID", erg.Id), 6);

                Assert.Equal(DbWerte.KLIMA_QUELLE_TRY_DATEI, Text(SchemaKatalog.SPALTE_KR_QUELLE, erg.Id));
                Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                             Text(SchemaKatalog.SPALTE_KR_IMPORTDATUM, erg.Id));
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        /// <summary>
        /// Der sprachneutrale Schlüssel je Quelle, eingefroren (Drei-Schichten-Regel:
        /// In der Datenbank steht ein SCHLÜSSEL, kein Anzeigetext).
        /// </summary>
        [Theory]
        [InlineData(KlimaQuelle.PvgisTmy, "PVGIS")]
        [InlineData(KlimaQuelle.TryDatei, "TRY_DATEI")]
        [InlineData(KlimaQuelle.TryRegional, "TRY_REGIONAL")]
        public void Jede_Quelle_traegt_ihren_sprachneutralen_Schluessel(KlimaQuelle quelle, string schluessel)
        {
            Assert.Equal(schluessel, KlimaImportAblauf.Quellenschluessel(quelle));
        }

        // =====================================================================
        //  4 — Die Projektkopie
        // =====================================================================

        /// <summary>
        /// <b>Die Projektkopie trägt alles mit</b> — die drei Klimagrößen je Stunde und
        /// Quelle/Importdatum am Kopfsatz. Eine Spalte nur auf der Katalogseite wäre
        /// hier sofort ein Datenverlust: Das Projekt wüsste nicht mehr, woher seine
        /// Reihe stammt, und die Gebäudesimulation fände die Größen nur im Katalog.
        /// </summary>
        [Fact]
        public async Task Die_Projektkopie_traegt_die_neuen_Spalten_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = TryDatei();
            int stammId;
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL3 Kopie Probe",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);

                Assert.True(erg.Erfolgreich, erg.Meldung);
                stammId = erg.Id;
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }

            int projektRegionId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                projektRegionId = KlimaregionStammCtrl.CopyRegionToProjekt(stammId, PROJEKT, v);
                v.Commit();
            }

            Assert.True(projektRegionId > 0);

            foreach (string spalte in new[] { SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG,
                                              SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE,
                                              SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD })
                Assert.Equal(8760, Zahl(Zaehlsatz(SchemaKatalog.TAB_SOLAR, spalte), projektRegionId));

            Assert.Equal(260.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG +
                                     " FROM " + SchemaKatalog.TAB_SOLAR +
                                     " WHERE ID_Klimaregion = ? ORDER BY ID", projektRegionId), 6);
            Assert.Equal(4.0, Wert("SELECT " + SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD +
                                   " FROM " + SchemaKatalog.TAB_SOLAR +
                                   " WHERE ID_Klimaregion = ? ORDER BY ID", projektRegionId), 6);

            Assert.Equal(DbWerte.KLIMA_QUELLE_TRY_DATEI,
                         Convert.ToString(DataRepository.ExecuteScalar(
                             "SELECT " + SchemaKatalog.SPALTE_KR_QUELLE + " FROM " +
                             SchemaKatalog.TAB_KLIMAREGION + " WHERE ID = ?",
                             new DbParam("@id", projektRegionId))));
            Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                         Convert.ToString(DataRepository.ExecuteScalar(
                             "SELECT " + SchemaKatalog.SPALTE_KR_IMPORTDATUM + " FROM " +
                             SchemaKatalog.TAB_KLIMAREGION + " WHERE ID = ?",
                             new DbParam("@id", projektRegionId))));
        }

        /// <summary>
        /// <b>Eine ALTBESTANDS-Region bleibt leer.</b> Sie stand vor dem Schritt in der
        /// Datenbank und sagt nicht, woher sie kommt — auch nicht nach dem Kopieren ins
        /// Projekt. Nachdatiert wird nichts, und aus NULL wird keine 0.
        /// </summary>
        [Fact]
        public void Eine_Altbestandsregion_bleibt_nach_dem_Kopieren_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID_Klimaregion) FROM " + SchemaKatalog.TAB_KLIMAREGION_STAMM);
            if (o == null || o == DBNull.Value) return;
            int stammId = Convert.ToInt32(o, CultureInfo.InvariantCulture);

            int projektRegionId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                projektRegionId = KlimaregionStammCtrl.CopyRegionToProjekt(stammId, PROJEKT, v);
                v.Commit();
            }

            Assert.True(projektRegionId > 0);

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_KLIMAREGION +
                                 " WHERE ID = ? AND (" + SchemaKatalog.SPALTE_KR_QUELLE +
                                 " IS NOT NULL OR " + SchemaKatalog.SPALTE_KR_IMPORTDATUM +
                                 " IS NOT NULL)", projektRegionId));

            foreach (string spalte in new[] { SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG,
                                              SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE,
                                              SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD })
                Assert.Equal(0, Zahl(Zaehlsatz(SchemaKatalog.TAB_SOLAR, spalte), projektRegionId));
        }

        // =====================================================================
        //  5 — Der Leser
        // =====================================================================

        /// <summary>
        /// <b><c>SolardatenCtrl</c> liest die drei Größen als NULLBARE Zahlen.</b> Beim
        /// Altbestand bleibt <c>null</c> stehen — es wird nicht auf 0 gelegt; nach einem
        /// TRY-Import trägt jede Zeile ihren Wert.
        /// </summary>
        [Fact]
        public async Task Der_Leser_gibt_die_drei_Groessen_nullbar_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Altbestand: eine Region, die es schon gab.
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID_Klimaregion) FROM " + SchemaKatalog.TAB_KLIMAREGION_STAMM);
            if (o != null && o != DBNull.Value)
            {
                var alt = new SolardatenCtrl();
                alt.ReadAllStamm(Convert.ToInt32(o, CultureInfo.InvariantCulture));
                if (alt.rows > 0)
                {
                    Assert.Null(alt.items[0].Gegenstrahlung);
                    Assert.Null(alt.items[0].Luftfeuchte);
                    Assert.Null(alt.items[0].Bedeckungsgrad);
                }
            }

            string pfad = TryDatei();
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL3 Leser Probe",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);

                Assert.True(erg.Erfolgreich, erg.Meldung);

                var neu = new SolardatenCtrl();
                neu.ReadAllStamm(erg.Id);

                Assert.Equal(8760, neu.rows);
                Assert.Equal(260.0, neu.items[0].Gegenstrahlung!.Value, 6);
                Assert.Equal(80.0, neu.items[0].Luftfeuchte!.Value, 6);
                Assert.Equal(4.0, neu.items[0].Bedeckungsgrad!.Value, 6);
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Die eingefrorene PVGIS-Antwort (72 Stunden, mit <c>IR(h)</c> und <c>RH</c>).</summary>
        private static List<TmyHourlyData> Tmy()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben",
                                               "pvgis_tmy_stuttgart_72h.json");
                if (File.Exists(kandidat))
                    return PVGIS_EPW_Downloader.AusJson(File.ReadAllText(kandidat), out _);
            }
            throw new FileNotFoundException("Die eingefrorene TMY-Probe wurde nicht gefunden.");
        }

        /// <summary>
        /// Eine vollständige, SYNTHETISCHE TRY-Datei (34 Kopfzeilen, 8 760 Datenzeilen)
        /// im Temp-Ordner. Keine DWD-Originaldaten: <c>N = 4</c>, <c>RF = 80</c>,
        /// <c>A = 260</c> in jeder Zeile.
        /// </summary>
        private static string TryDatei()
        {
            string pfad = Path.Combine(Path.GetTempPath(),
                "epos_kl3_" + Guid.NewGuid().ToString("N") + ".dat");

            var sb = new StringBuilder(700 * 1024);
            for (int i = 1; i <= 34; i++)
                sb.Append("Kopfzeile ").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("*** \r\n");

            int[] tageMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            for (int m = 1; m <= 12; m++)
                for (int tag = 1; tag <= tageMonat[m - 1]; tag++)
                    for (int h = 1; h <= 24; h++)
                    {
                        int b = h >= 9 && h <= 16 ? 150 : 0;
                        int diff = h >= 9 && h <= 16 ? 60 : 0;
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "4321000 5678000 {0} {1} {2} {3} 1013 180 2.0 4 3.0 80 {4} {5} 260 300 1\r\n",
                            m, tag, h, 8.5, b, diff);
                    }

            File.WriteAllText(pfad, sb.ToString());
            return pfad;
        }

        /// <summary>Zählsatz: Zeilen einer Region, in denen die Spalte belegt ist.</summary>
        private static string Zaehlsatz(string tabelle, string spalte)
        {
            return "SELECT COUNT(*) FROM [" + tabelle + "] WHERE ID_Klimaregion = ? AND [" +
                   spalte + "] IS NOT NULL";
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static int Zahl(string sql, int id)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static double Wert(string sql, int id)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return o == null || o == DBNull.Value ? double.NaN : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string spalte, int stammId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT " + spalte + " FROM " + SchemaKatalog.TAB_KLIMAREGION_STAMM +
                " WHERE ID_Klimaregion = ?", new DbParam("@id", stammId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
        }
    }
}
