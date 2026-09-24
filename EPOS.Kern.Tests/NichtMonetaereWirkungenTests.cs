using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E17 (V‑G11, DIN EN 17463 6.1 und 8.2) — die REGEL der nicht monetarisierbaren
    /// Wirkungen ohne Datenbank: Beurteilung = Dauer × stärkste Wirkung, „nicht beurteilt",
    /// Prüfung, Kurztext und die Anzeigetexte in beiden Sprachen.
    /// </summary>
    public class NichtMonetaereWirkungenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Theory]
        [InlineData(1, 0, 0, 0, 0)]
        [InlineData(1, 1, null, null, 1)]
        [InlineData(2, 1, 3, 2, 6)]
        [InlineData(3, 3, 3, 3, 9)]
        [InlineData(3, null, null, 2, 6)]
        [InlineData(2, 0, null, 0, 0)]
        public void Beurteilung_ist_Dauer_mal_staerkste_Wirkung(int dauer, int? org, int? ma, int? umw, int erwartet)
        {
            Assert.Equal(erwartet, NichtMonetaereWirkungen.Beurteilung(dauer, org, ma, umw));
        }

        [Theory]
        [InlineData(null, 2, 2, 2)]      // ohne Dauer
        [InlineData(2, null, null, null)] // ohne jeden Wirkungsgrad
        [InlineData(0, 1, 1, 1)]          // Dauer außerhalb der Skala
        [InlineData(4, 1, 1, 1)]
        [InlineData(2, 4, null, null)]    // Wirkung außerhalb der Skala
        [InlineData(2, -1, null, null)]
        public void Ohne_Dauer_oder_Wirkung_oder_ausserhalb_der_Skala_nicht_beurteilt(int? dauer, int? org, int? ma, int? umw)
        {
            Assert.Null(NichtMonetaereWirkungen.Beurteilung(dauer, org, ma, umw));
        }

        /// <summary>Die stärkste, nicht die Summe (E17‑Q2 a): drei geringe Wirkungen sind keine starke.</summary>
        [Fact]
        public void Die_staerkste_zaehlt_nicht_die_Summe()
        {
            Assert.Equal(3, NichtMonetaereWirkungen.Beurteilung(3, 1, 1, 1));
            Assert.Equal(NichtMonetaereWirkungen.DAUER_MAX * NichtMonetaereWirkungen.WIRKUNG_MAX,
                         NichtMonetaereWirkungen.BEURTEILUNG_MAX);
        }

        [Fact]
        public void Die_Zeile_rechnet_ihre_Beurteilung_mit_derselben_Regel()
        {
            var w = new ProjektWirkung { Beschreibung = "Komfort", Dauer = 2, WirkungMitarbeiter = 3 };
            Assert.Equal(6, w.Beurteilung);
            Assert.Equal(NichtMonetaereWirkungen.Beurteilung(w), w.Beurteilung);
            Assert.Equal("6 von 9", NichtMonetaereWirkungen.BeurteilungText(w));
            Assert.Equal(R.WIRT_NM_NICHT_BEURTEILT, NichtMonetaereWirkungen.BeurteilungText(new ProjektWirkung()));

            ProjektWirkung k = w.Kopie();
            k.Dauer = 1;
            Assert.Equal(2, w.Dauer);   // die Kopie ist entkoppelt
        }

        [Fact]
        public void Benannt_Beurteilt_und_Kurztext_folgen_den_Beschreibungen()
        {
            var liste = new List<ProjektWirkung>
            {
                new() { Beschreibung = "  Versorgungssicherheit " },
                new() { Beschreibung = "   ", Dauer = 3, WirkungUmwelt = 3 },
                new() { Beschreibung = "Komfort" }
            };
            Assert.True(NichtMonetaereWirkungen.Benannt(liste));
            Assert.False(NichtMonetaereWirkungen.Beurteilt(liste));   // die beurteilte Zeile hat keine Beschreibung
            Assert.Equal("Versorgungssicherheit; Komfort", NichtMonetaereWirkungen.Kurztext(liste));

            liste[2].Dauer = 1;
            liste[2].WirkungOrganisation = 0;
            Assert.True(NichtMonetaereWirkungen.Beurteilt(liste));

            Assert.False(NichtMonetaereWirkungen.Benannt(null));
            Assert.False(NichtMonetaereWirkungen.Benannt(new List<ProjektWirkung>()));
            Assert.Equal("", NichtMonetaereWirkungen.Kurztext(null));
        }

        [Fact]
        public void Pruefen_nennt_Kategorie_Beschreibung_Dauer_und_Wirkung()
        {
            Assert.Null(NichtMonetaereWirkungen.Pruefen(new ProjektWirkung()));   // leere Zeile: nichts zu prüfen
            Assert.True(NichtMonetaereWirkungen.IstLeer(new ProjektWirkung()));
            Assert.Null(NichtMonetaereWirkungen.Pruefen(new ProjektWirkung { Beschreibung = "x", Dauer = 3, WirkungUmwelt = 0 }));

            Assert.Equal(R.WIRT_NM_FEHLER_BESCHREIBUNG,
                         NichtMonetaereWirkungen.Pruefen(new ProjektWirkung { Dauer = 2 }));
            Assert.Equal(R.WIRT_NM_FEHLER_DAUER,
                         NichtMonetaereWirkungen.Pruefen(new ProjektWirkung { Beschreibung = "x", Dauer = 5 }));
            Assert.Equal(R.WIRT_NM_FEHLER_WIRKUNG,
                         NichtMonetaereWirkungen.Pruefen(new ProjektWirkung { Beschreibung = "x", WirkungMitarbeiter = 7 }));
            Assert.Contains("GEFUEHL",
                            NichtMonetaereWirkungen.Pruefen(new ProjektWirkung { Beschreibung = "x", Kategorie = "GEFUEHL" }));
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Kategorien_und_Stufen_tragen_eigene_Texte_in_beiden_Sprachen(string kultur)
        {
            using (new Kulturvorrichtung(kultur))
            {
                Assert.Equal(3, NichtMonetaereWirkungen.Kategorien().Count);
                Assert.Equal(new[] { 1, 2, 3 }, NichtMonetaereWirkungen.Dauerstufen().Select(s => s.Key));
                Assert.Equal(new[] { 0, 1, 2, 3 }, NichtMonetaereWirkungen.Wirkungsgrade().Select(s => s.Key));
                foreach (var liste in new[] { NichtMonetaereWirkungen.Kategorien(), NichtMonetaereWirkungen.Dauerstufen(),
                                              NichtMonetaereWirkungen.Wirkungsgrade() })
                {
                    Assert.All(liste, s => Assert.False(string.IsNullOrWhiteSpace(s.Value)));
                    Assert.Equal(liste.Count, liste.Select(s => s.Value).Distinct().Count());
                }
            }

            string de, en;
            using (new Kulturvorrichtung("de-DE")) de = NichtMonetaereWirkungen.KategorieText(NichtMonetaereWirkungen.FINANZIELL);
            using (new Kulturvorrichtung("en-US")) en = NichtMonetaereWirkungen.KategorieText(NichtMonetaereWirkungen.FINANZIELL);
            Assert.NotEqual(de, en);
        }

        /// <summary>Die drei Kategorien der Regel und der CHECK der Tabelle sind dieselben.</summary>
        [Fact]
        public void Kategorien_der_Regel_stehen_im_CHECK_der_Tabelle()
        {
            foreach (string k in NichtMonetaereWirkungen.KATEGORIEN)
                Assert.Contains("'" + k + "'", ProjektWirkungSchema.SQL_CREATE);
            Assert.Contains("BETWEEN 1 AND 3", ProjektWirkungSchema.SQL_CREATE);
            Assert.Contains("\"Wirkung_Umwelt\" BETWEEN 0 AND 3", ProjektWirkungSchema.SQL_CREATE);
            Assert.EndsWith(") STRICT", ProjektWirkungSchema.SQL_CREATE, StringComparison.Ordinal);
            Assert.Contains("REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE", ProjektWirkungSchema.SQL_CREATE);
        }
    }

    /// <summary>
    /// ETAPPE E17 — Persistenz, Schemaschritt 127 und die Freitextübernahme gegen die
    /// Arbeitskopie der Testdatenbank; dazu der Anker „keine Rechenwirkung".
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektWirkungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        [Fact]
        public void Zielstand_und_Tabelle_stehen()
        {
            if (!_db.Vorhanden) return;

            Assert.True(SchemaStand.Zielversion >= ProjektWirkungSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + ProjektWirkungSchema.SCHRITT + ".");
            Assert.True(ProjektWirkungSchema.TabelleVorhanden());
            Assert.True(ProjektWirkungSchema.Vollstaendig());
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_table_list WHERE name = 'Tab_ProjektWirkung' AND strict = 1"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_list('Tab_ProjektWirkung') " +
                                  "WHERE \"table\" = 'Tab_Projekt' AND on_delete = 'CASCADE'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                                  new DbParam("@i", ProjektWirkungSchema.INDEX)));
        }

        [Fact]
        public void Speichern_und_Laden_halten_Reihenfolge_und_Werte()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektWirkungCtrl();

            Assert.True(ctrl.Speichern(PROJEKT, new[]
            {
                new ProjektWirkung { Kategorie = NichtMonetaereWirkungen.FINANZIELL, Beschreibung = " Image ", Dauer = 2,
                                     WirkungOrganisation = 3 },
                new ProjektWirkung(),   // leere Zeile fällt weg
                new ProjektWirkung { Kategorie = NichtMonetaereWirkungen.SONSTIG, Beschreibung = "Komfort" }
            }), ctrl.Speicherfehler);

            List<ProjektWirkung> l = ctrl.Laden(PROJEKT);
            Assert.Null(ctrl.Ladefehler);
            Assert.Equal(2, l.Count);
            Assert.Equal(new[] { 1, 2 }, l.Select(w => w.Sortierung));
            Assert.Equal("Image", l[0].Beschreibung);
            Assert.Equal(NichtMonetaereWirkungen.FINANZIELL, l[0].Kategorie);
            Assert.Equal(2, l[0].Dauer);
            Assert.Equal(3, l[0].WirkungOrganisation);
            Assert.Null(l[0].WirkungMitarbeiter);
            Assert.Equal(6, l[0].Beurteilung);
            Assert.Null(l[1].Beurteilung);

            // Speichern ERSETZT die Liste.
            Assert.True(ctrl.Speichern(PROJEKT, new[] { l[1] }));
            Assert.Equal("Komfort", Assert.Single(ctrl.Laden(PROJEKT)).Beschreibung);

            Assert.True(ctrl.Speichern(PROJEKT, Array.Empty<ProjektWirkung>()));
            Assert.Empty(ctrl.Laden(PROJEKT));
        }

        [Fact]
        public void Ein_Pruefbefund_bricht_ab_bevor_etwas_geschrieben_ist()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektWirkungCtrl();
            Assert.True(ctrl.Speichern(PROJEKT, new[] { new ProjektWirkung { Beschreibung = "bleibt" } }));

            Assert.False(ctrl.Speichern(PROJEKT, new[]
            {
                new ProjektWirkung { Beschreibung = "neu" },
                new ProjektWirkung { Beschreibung = "falsch", Dauer = 9 }
            }));
            Assert.Equal(R.WIRT_NM_FEHLER_DAUER, ctrl.Speicherfehler);
            Assert.Equal("bleibt", Assert.Single(ctrl.Laden(PROJEKT)).Beschreibung);

            Assert.False(ctrl.Speichern(0, new[] { new ProjektWirkung { Beschreibung = "x" } }));
            Assert.Equal(R.WIRT_NM_FEHLER_PROJEKT, ctrl.Speicherfehler);
        }

        /// <summary>Die Tabelle hält ihre Wertebereiche selbst — auch an der Oberfläche vorbei.</summary>
        [Fact]
        public void Der_CHECK_der_Tabelle_weist_fremde_Werte_ab()
        {
            if (!_db.Vorhanden) return;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.ThrowsAny<Exception>(() => v.Ausfuehren(
                    "INSERT INTO Tab_ProjektWirkung (ID_Projekt, Sortierung, Kategorie, Beschreibung, Dauer) VALUES (?, 1, 'SONSTIG', 'x', 4)",
                    new DbParam("@p", PROJEKT)));
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.ThrowsAny<Exception>(() => v.Ausfuehren(
                    "INSERT INTO Tab_ProjektWirkung (ID_Projekt, Sortierung, Kategorie, Beschreibung) VALUES (?, 1, 'GEFUEHL', 'x')",
                    new DbParam("@p", PROJEKT)));
            }
        }

        /// <summary>Das Projektduplikat (derselbe Weg wie „Variante anlegen") trägt die Wirkungen mit.</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Wirkungen_mit()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektWirkungCtrl();
            Assert.True(ctrl.Speichern(PROJEKT, new[]
            {
                new ProjektWirkung { Beschreibung = "Versorgungssicherheit", Dauer = 3, WirkungUmwelt = 2 },
                new ProjektWirkung { Beschreibung = "Komfort" }
            }));

            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " E17");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            List<ProjektWirkung> kopie = ctrl.Laden(neu);
            Assert.Equal(new[] { "Versorgungssicherheit", "Komfort" }, kopie.Select(w => w.Beschreibung));
            Assert.Equal(6, kopie[0].Beurteilung);
            Assert.Equal(2, ctrl.Laden(PROJEKT).Count);   // die Quelle bleibt
        }

        /// <summary>
        /// Schritt 127 übernimmt einen gepflegten Freitext als EINE Wirkung SONSTIG ohne
        /// Beurteilung — nur bei nicht leerem Text, nur ohne eigene Wirkung, wiederholbar; das
        /// Freitextfeld bleibt stehen (Altfeld).
        /// </summary>
        [Fact]
        public void Schritt_127_uebernimmt_den_Freitext_als_Wirkung_sonstig()
        {
            if (!_db.Vorhanden) return;

            List<long> ids = new List<long>();
            DataTable t = DataRepository.GetDataTable(
                "SELECT w.ID_Projekt FROM Tab_ProjektWirtschaftlichkeit w " +
                "WHERE EXISTS (SELECT 1 FROM Tab_Projekt p WHERE p.ID = w.ID_Projekt) ORDER BY w.ID_Projekt LIMIT 3");
            foreach (DataRow r in t.Rows) ids.Add(Convert.ToInt64(r[0], CultureInfo.InvariantCulture));
            Assert.True(ids.Count == 3, "Die Testdatenbank braucht drei Parametersätze.");

            // Vor dem Schritt: keine Tabelle, drei Projekte mit Text, Leerraum und schon eigener Wirkung.
            DataRepository.ExecuteNonQuery("DROP TABLE Tab_ProjektWirkung");
            Assert.False(ProjektWirkungSchema.TabelleVorhanden());
            Assert.False(ProjektWirkungSchema.Vollstaendig());
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = NULL");
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                                           new DbParam("@t", "  Versorgungssicherheit, Komfort  "), new DbParam("@p", ids[0]));
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                                           new DbParam("@t", "   "), new DbParam("@p", ids[1]));
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                                           new DbParam("@t", "Außenwirkung"), new DbParam("@p", ids[2]));

            ProjektWirkungSchema.Bericht b = ProjektWirkungSchema.Ausfuehren();
            Assert.True(b.TabelleAngelegt);
            Assert.Equal(2, b.Uebernommen);
            Assert.True(ProjektWirkungSchema.Vollstaendig());

            var ctrl = new ProjektWirkungCtrl();
            ProjektWirkung w = Assert.Single(ctrl.Laden((int)ids[0]));
            Assert.Equal(NichtMonetaereWirkungen.SONSTIG, w.Kategorie);
            Assert.Equal("Versorgungssicherheit, Komfort", w.Beschreibung);
            Assert.Equal(1, w.Sortierung);
            Assert.Null(w.Dauer);
            Assert.Null(w.WirkungOrganisation);
            Assert.Null(w.WirkungMitarbeiter);
            Assert.Null(w.WirkungUmwelt);
            Assert.Null(w.Beurteilung);
            Assert.Empty(ctrl.Laden((int)ids[1]));            // Leerraum ist kein Text
            Assert.Single(ctrl.Laden((int)ids[2]));

            // Das Altfeld bleibt stehen und lesbar (die Spalte unverändert, der Leseweg trimmt).
            Assert.Equal("  Versorgungssicherheit, Komfort  ", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Nicht_Monetaer FROM Tab_ProjektWirtschaftlichkeit WHERE ID_Projekt = ?",
                new DbParam("@p", ids[0])), CultureInfo.InvariantCulture));
            Assert.Equal("Versorgungssicherheit, Komfort", new WirtschaftlichkeitCtrl().LadeParameter((int)ids[0]).NichtMonetaer);

            // Wiederholbar: ein zweiter Lauf legt nichts an und übernimmt nichts doppelt —
            // auch nicht, wenn der Anwender die übernommene Wirkung geändert hat.
            Assert.True(ctrl.Speichern((int)ids[2], new[] { new ProjektWirkung { Beschreibung = "geändert", Dauer = 1, WirkungUmwelt = 1 } }));
            ProjektWirkungSchema.Bericht zweiter = ProjektWirkungSchema.Ausfuehren();
            Assert.False(zweiter.TabelleAngelegt);
            Assert.Equal(0, zweiter.Uebernommen);
            Assert.Equal("geändert", Assert.Single(ctrl.Laden((int)ids[2])).Beschreibung);
        }

        /// <summary>
        /// <b>Anker „keine Rechenwirkung"</b>: Dieselbe Gruppe rechnet mit und ohne Wirkungen
        /// am Stamm bitgleich — Kapitalwert, Differenz, Annuität, Amortisation je Stand und
        /// Szenario.
        /// </summary>
        [Fact]
        public void Die_Wirkungen_aendern_keinen_Kapitalwert_bitgleich()
        {
            if (!_db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> ohne = Rechne();
            Assert.True(new ProjektWirkungCtrl().Speichern(1040, new[]
            {
                new ProjektWirkung { Kategorie = NichtMonetaereWirkungen.ENERGIEFLUSS, Beschreibung = "Versorgungssicherheit",
                                     Dauer = 3, WirkungOrganisation = 3, WirkungMitarbeiter = 3, WirkungUmwelt = 3 },
                new ProjektWirkung { Kategorie = NichtMonetaereWirkungen.FINANZIELL, Beschreibung = "Image", Dauer = 1, WirkungUmwelt = 0 }
            }));
            List<WirtschaftlichkeitErgebnis> mit = Rechne();

            Assert.Equal(ohne.Count, mit.Count);
            Assert.True(ohne.Count > 0);
            for (int i = 0; i < ohne.Count; i++)
            {
                Assert.Equal(ohne[i].IdProjekt, mit[i].IdProjekt);
                Assert.Equal(ohne[i].Szenario, mit[i].Szenario);
                Assert.Equal(Bits(ohne[i].Kapitalwert), Bits(mit[i].Kapitalwert));
                Assert.Equal(Bits(ohne[i].KapitalwertDiff), Bits(mit[i].KapitalwertDiff));
                Assert.Equal(Bits(ohne[i].AnnuitaetKW), Bits(mit[i].AnnuitaetKW));
                Assert.Equal(Bits(ohne[i].AmortisationJahre), Bits(mit[i].AmortisationJahre));
            }
        }

        private static long? Bits(double? d) => d.HasValue ? BitConverter.DoubleToInt64Bits(d.Value) : (long?)null;

        private static List<WirtschaftlichkeitErgebnis> Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(1040);
            p.IdStamm = 1040;
            var daten = new BerichtsDaten { IdStamm = 1040, Stammprojektname = "Stammprojekt" };
            int[] ids = { 1040, 1041, 1042 };
            double[] energie = { 12000.0, 9000.0, 7000.0 };
            for (int i = 0; i < ids.Length; i++)
                daten.Varianten.Add(new VariantenDaten
                {
                    IdProjekt = ids[i], IstStamm = i == 0, Projektname = "Stammprojekt",
                    Variantenname = i == 0 ? "" : "Variante " + i, Ergebnis = new ErgebnisModel(),
                    Energiekosten = energie[i]
                });
            return ctrl.Berechne(daten, p, 0, false);
        }

        /// <summary>
        /// Die Werkzeug-Wache: Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und die
        /// Nachzieh-Liste der Tests bedienen sich aus DERSELBEN Quelle, der Schritt steht nach
        /// 124, und die REPO-Datei trägt Stand und Tabelle (gelesen nur lesend).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt_127()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("ProjektWirkungSchema.Ausfuehren()", werkzeug);

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN = ProjektWirkungSchema.SCHRITT", migration);
            int ort124 = migration.IndexOf("new Schritt(SCHRITT_124_ZAPFPROFIL_LAUFANGABEN", StringComparison.Ordinal);
            int ort127 = migration.IndexOf("new Schritt(SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN", StringComparison.Ordinal);
            Assert.True(ort124 > 0 && ort127 > ort124, "Der Schritt 127 steht nicht nach 124.");
            Assert.Contains("ProjektWirkungSchema.Ausfuehren()", migration);
            Assert.Contains("ProjektWirkungSchema.Vollstaendig()", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("ProjektWirkungSchema.Ausfuehren()", vorrichtung);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= ProjektWirkungSchema.SCHRITT);
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_list WHERE name = 'Tab_ProjektWirkung' AND strict = 1"));
            Assert.Equal(0L, Repo(verbindung,
                "SELECT COUNT(*) FROM Tab_ProjektWirtschaftlichkeit w WHERE TRIM(COALESCE(w.Nicht_Monetaer, '')) <> '' " +
                "AND EXISTS (SELECT 1 FROM Tab_Projekt p WHERE p.ID = w.ID_Projekt) " +
                "AND NOT EXISTS (SELECT 1 FROM Tab_ProjektWirkung x WHERE x.ID_Projekt = w.ID_Projekt)"));
            verbindung.Close();
            SqliteConnection.ClearPool(verbindung);
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
