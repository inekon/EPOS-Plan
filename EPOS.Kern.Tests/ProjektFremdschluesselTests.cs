using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID vom 19.09.2026 — Schemaschritt <b>96</b>: Die achtundzwanzig
    /// Projekttabellen bekommen ihren Fremdschlüssel auf <c>Tab_Projekt</c>.
    ///
    /// <para>Geprüft wird dreierlei: der KATALOG gegen die Datenbank (keine Tabelle mit
    /// Projektspalte darf fehlen), die TEXTE (Zieltext, Klauseln, Wiederholbarkeit des
    /// Indextextes) — und der UMBAU selbst auf einer Arbeitskopie, die dafür erst wieder
    /// auf den Stand 95 zurückgebaut wird (<see cref="TestDatenbank.ProjektFremdschluesselZuruecknehmen"/>).
    /// Dabei müssen Zeilenzahl, Ids, <c>sqlite_sequence</c>, Spalten, Indizes und Sichten
    /// unverändert bleiben, <c>foreign_key_check</c> leer und <c>integrity_check</c>
    /// „ok" sein.</para>
    ///
    /// <para><b>Der Waisenfall bekommt eine eigene Probe</b>, denn der Schritt ist der
    /// erste, der Zeilen ENTFERNT: eine synthetische Zeile ohne Projekt muss gezählt,
    /// gelöscht und im Bericht genannt werden — und eine Zeile, deren Projektspalte sich
    /// aus dem Elternsatz HEILEN lässt, muss bleiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektFremdschluesselTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Katalog und Texte
        // =============================================================================

        /// <summary>Der Zielstand ist 96, und der Katalog führt achtundzwanzig Tabellen.</summary>
        [Fact]
        public void Der_Zielstand_ist_96_und_der_Katalog_fuehrt_28_Tabellen()
        {
            using var _ = new Kulturvorrichtung();

            Assert.True(SchemaStand.Zielversion >= 96,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 96.");
            Assert.Equal(28, ProjektFremdschluessel.Katalog.Length);

            // Kein Name doppelt - ein zweiter Eintrag baute dieselbe Tabelle zweimal um.
            var namen = ProjektFremdschluessel.Katalog.Select(e => e.Tabelle).ToList();
            Assert.Equal(namen.Count, namen.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            // Tab_Variante ist die eine Tabelle mit ZWEI Projektspalten.
            ProjektFremdschluessel.Eintrag variante = ProjektFremdschluessel.Finde("Tab_Variante");
            Assert.NotNull(variante);
            Assert.Equal(new[] { "ID_Projekt", "ID_ProjektRef" }, variante.Spalten);
            Assert.Equal(29, ProjektFremdschluessel.Katalog.Sum(e => e.Spalten.Length));
        }

        /// <summary>
        /// DIE WACHE: Jede Tabelle der Datenbank mit einer Projektspalte steht im Katalog
        /// oder auf der benannten Ausnahmeliste — eine neue Projekttabelle darf nicht
        /// still ohne Beziehung bleiben.
        /// </summary>
        [Fact]
        public void Jede_Tabelle_mit_Projektspalte_steht_im_Katalog_oder_ist_benannt_ausgenommen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var fehlend = new List<string>();

            foreach (DataRow zeile in DataRepository.GetDataTable(
                         "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name").Rows)
            {
                string tabelle = Convert.ToString(zeile["name"], CultureInfo.InvariantCulture);
                if (tabelle.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase)) continue;

                bool hatProjektspalte = ProjektFremdschluessel.Projektspalten
                    .Any(s => DataRepository.SpalteVorhanden(tabelle, s));
                if (!hatProjektspalte) continue;

                if (ProjektFremdschluessel.Finde(tabelle) != null) continue;
                if (ProjektFremdschluessel.Ausgenommen
                        .Any(a => string.Equals(a, tabelle, StringComparison.OrdinalIgnoreCase))) continue;
                if (ProjektFremdschluessel.Projektspalten
                        .Any(s => DataRepository.SpalteVorhanden(tabelle, s) &&
                                  ProjektFremdschluessel.FremdschluesselSteht(tabelle, s))) continue;

                fehlend.Add(tabelle);
            }

            Assert.True(fehlend.Count == 0,
                        "Diese Tabelle(n) tragen eine Projektspalte, stehen aber weder im " +
                        "Katalog von Schemaschritt 96 noch auf seiner Ausnahmeliste und " +
                        "haben auch keinen Fremdschluessel: " + string.Join(", ", fehlend) + ".");
        }

        /// <summary>
        /// Der Zieltext ist der Bestandstext plus Klauseln — nichts sonst. Er bricht ab,
        /// wenn die Tabelle kein <c>STRICT</c> trägt: Dann baut der Schritt sie nicht um,
        /// statt sie in eine andere Bauform zu zwingen.
        /// </summary>
        [Fact]
        public void Der_Zieltext_ergaenzt_nur_die_Klauseln_und_verlangt_STRICT()
        {
            using var _ = new Kulturvorrichtung();

            const string bestand = "CREATE TABLE \"Tab_X\" (\"ID\" INTEGER PRIMARY KEY, \"ID_Projekt\" INTEGER) STRICT";
            string ziel = ProjektFremdschluessel.Zieltext("Tab_X", bestand, new[] { "ID_Projekt" });

            Assert.StartsWith("CREATE TABLE \"Tab_X\" (\"ID\" INTEGER PRIMARY KEY, \"ID_Projekt\" INTEGER",
                              ziel, StringComparison.Ordinal);
            Assert.EndsWith(") STRICT", ziel, StringComparison.Ordinal);
            Assert.Contains("FOREIGN KEY (\"ID_Projekt\") REFERENCES \"Tab_Projekt\" (\"ID\") " +
                            "ON DELETE CASCADE ON UPDATE CASCADE", ziel, StringComparison.Ordinal);

            // Zwei Spalten - zwei Klauseln.
            string zwei = ProjektFremdschluessel.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_X\" (\"ID_Projekt\" INTEGER, \"ID_ProjektRef\" INTEGER) STRICT",
                new[] { "ID_Projekt", "ID_ProjektRef" });
            Assert.Equal(2, Vorkommen(zwei, "FOREIGN KEY"));

            // Ohne STRICT: benannt abgelehnt, nicht still umgebaut.
            Assert.Throws<InvalidOperationException>(() => ProjektFremdschluessel.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_X\" (\"ID_Projekt\" INTEGER)", new[] { "ID_Projekt" }));

            // Anderer Name im Kopf: ebenfalls abgelehnt.
            Assert.Throws<InvalidOperationException>(() => ProjektFremdschluessel.Zieltext(
                "Tab_X", "CREATE TABLE \"Tab_Y\" (\"ID_Projekt\" INTEGER) STRICT", new[] { "ID_Projekt" }));
        }

        /// <summary>Ein wiederhergestellter Index trägt <c>IF NOT EXISTS</c> — genau einmal.</summary>
        [Fact]
        public void Der_Indextext_wird_wiederholbar_gemacht()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal("CREATE INDEX IF NOT EXISTS \"i\" ON \"t\" (\"s\")",
                         ProjektFremdschluessel.MitIfNotExists("CREATE INDEX \"i\" ON \"t\" (\"s\")"));
            Assert.Equal("CREATE UNIQUE INDEX IF NOT EXISTS \"i\" ON \"t\" (\"s\")",
                         ProjektFremdschluessel.MitIfNotExists("CREATE UNIQUE INDEX \"i\" ON \"t\" (\"s\")"));

            // Schon vorhanden: unveraendert.
            const string schon = "CREATE INDEX IF NOT EXISTS \"i\" ON \"t\" (\"s\")";
            Assert.Equal(schon, ProjektFremdschluessel.MitIfNotExists(schon));
        }

        // =============================================================================
        //  Teil 2 - der Umbau auf der Arbeitskopie
        // =============================================================================

        /// <summary>
        /// Die Arbeitskopie steht auf dem Zielstand: JEDE Projektspalte des Katalogs
        /// trägt ihren Fremdschlüssel, und <c>Offen()</c> zählt null.
        /// </summary>
        [Fact]
        public void Auf_dem_Zielstand_traegt_jede_Projektspalte_ihren_Fremdschluessel()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Assert.Equal(0, ProjektFremdschluessel.Offen());

            foreach (ProjektFremdschluessel.Eintrag e in ProjektFremdschluessel.Katalog)
            {
                if (!DataRepository.TabelleVorhanden(e.Tabelle)) continue;
                foreach (string spalte in e.Spalten)
                {
                    if (!DataRepository.SpalteVorhanden(e.Tabelle, spalte)) continue;
                    Assert.True(ProjektFremdschluessel.FremdschluesselSteht(e.Tabelle, spalte),
                                e.Tabelle + "." + spalte + " traegt keinen Fremdschluessel auf Tab_Projekt.");
                }
            }

            Assert.Equal("ok", Skalar("PRAGMA integrity_check"));
            Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows
                                       .Cast<DataRow>().ToList());
        }

        /// <summary>
        /// DER UMBAU, an fünf Tabellen von unterschiedlichem Zuschnitt gemessen: eine
        /// Elterntabelle mit kaskadierenden Kindern (<c>Tab_WP</c>), eine mit
        /// <c>NO ACTION</c>-Kindern und fehlendem Index (<c>Tab_Ergebnis</c>), eine
        /// Tabelle mit zwei Projektspalten (<c>Tab_Variante</c>), eine, die eine Sicht
        /// nennt (<c>Tab_Gebaeude</c>), und eine ohne AUTOINCREMENT
        /// (<c>Berichtskonfiguration</c>).
        ///
        /// <para>Nach dem Rückbau auf Stand 95 und dem erneuten Umbau muss ALLES so
        /// stehen wie vorher — Zeilen, Ids, Zählerstand, Spalten, Indizes, Sichten — und
        /// die Beziehung obendrein.</para>
        /// </summary>
        [Fact]
        public void Der_Umbau_laesst_Zeilen_Ids_Zaehler_Spalten_Indizes_und_Sichten_unveraendert()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            string[] proben =
            {
                "Tab_WP", "Tab_Ergebnis", "Tab_Variante", "Tab_Gebaeude", "Berichtskonfiguration"
            };

            Dictionary<string, string> sichtenVorher = Sichten();
            var vorher = proben.ToDictionary(t => t, Zustand);

            foreach (string tabelle in proben)
            {
                TestDatenbank.ProjektFremdschluesselZuruecknehmen(tabelle);
                Assert.True(ProjektFremdschluessel.UmbauNoetig(tabelle),
                            "Der Rueckbau von " + tabelle + " hat die Beziehung nicht entfernt.");
            }

            var bericht = new List<string>();
            foreach (string tabelle in proben)
                Assert.True(ProjektFremdschluessel.Umbauen(tabelle, bericht),
                            "Der Umbau von " + tabelle + " meldete 'nichts zu tun'.");

            foreach (string tabelle in proben)
            {
                Zustandsbild jetzt = Zustand(tabelle);
                Zustandsbild alt = vorher[tabelle];

                Assert.Equal(alt.Zeilen, jetzt.Zeilen);
                Assert.Equal(alt.Ids, jetzt.Ids);
                Assert.Equal(alt.Zaehlerstand, jetzt.Zaehlerstand);
                Assert.Equal(alt.Spalten, jetzt.Spalten);
                Assert.Equal(alt.Indizes, jetzt.Indizes);
                Assert.Equal(alt.ProjektBeziehungen, jetzt.ProjektBeziehungen);
                Assert.Equal(alt.FremdeBeziehungen, jetzt.FremdeBeziehungen);
            }

            // Tab_Kenndaten behaelt seinen Fremdschluessel auf Tab_WP - der Umbau der
            // ELTERNtabelle darf die Beziehung des Kindes nicht anfassen.
            Assert.True(BeziehungSteht("Tab_Kenndaten", "ID_WP", "Tab_WP"),
                        "Tab_Kenndaten hat beim Umbau von Tab_WP seinen Fremdschluessel verloren.");

            Assert.Equal(sichtenVorher, Sichten());
            foreach (string sicht in sichtenVorher.Keys)
                DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + sicht + "\"");

            Assert.Equal("ok", Skalar("PRAGMA integrity_check"));
            Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows
                                       .Cast<DataRow>().ToList());

            // Der fehlende Index auf der Projektspalte ist ergaenzt.
            Assert.Contains("Tab_Ergebnis_ID_Projekt", Zustand("Tab_Ergebnis").Indizes);
            Assert.Contains("Tab_Variante_ID_ProjektRef", Zustand("Tab_Variante").Indizes);
        }

        /// <summary>
        /// WIEDERHOLBAR: Ein zweiter Lauf baut nichts mehr um und ändert keine Zeile.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Lauf_baut_nichts_mehr_um()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var bericht = new List<string>();
            Assert.Equal(0, ProjektFremdschluessel.Alle(bericht));
            Assert.Equal(28, bericht.Count);
            Assert.All(bericht, z => Assert.Contains("uebersprungen", z, StringComparison.Ordinal));
            Assert.Equal(0, ProjektFremdschluessel.Offen());
        }

        /// <summary>
        /// EINE TEILVERKNUEPFTE DATENBANK — der Fall des Anwenders, der 24 der 28
        /// Beziehungen schon selbst nachgerüstet hat: Wer sie trägt, wird übersprungen;
        /// nur die fehlenden werden gebaut.
        /// </summary>
        [Fact]
        public void Eine_Tabelle_mit_vorhandener_Beziehung_wird_uebersprungen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            TestDatenbank.ProjektFremdschluesselZuruecknehmen("Tab_Heizkessel");
            Assert.Equal(1, ProjektFremdschluessel.Offen());

            var bericht = new List<string>();
            Assert.Equal(1, ProjektFremdschluessel.Alle(bericht));

            Assert.Equal(27, bericht.Count(z => z.Contains("uebersprungen", StringComparison.Ordinal)));
            Assert.Single(bericht, z => z.StartsWith("Tab_Heizkessel:", StringComparison.Ordinal) &&
                                        z.Contains("gesetzt", StringComparison.Ordinal));
            Assert.Equal(0, ProjektFremdschluessel.Offen());
        }

        /// <summary>
        /// DER WAISENFALL: Eine Zeile ohne Projekt wird gezählt, gelöscht und im Bericht
        /// genannt — mitsamt den abhängigen Zeilen, die an ihr hängen. Eine Zeile mit
        /// gültigem Projekt bleibt unberührt.
        /// </summary>
        [Fact]
        public void Eine_Waise_wird_gezaehlt_geloescht_und_im_Bericht_genannt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int OhneProjekt = 987654;
            int gutesProjekt = Convert.ToInt32(
                DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Projekt"), CultureInfo.InvariantCulture);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + OhneProjekt));

            // ERST zurueckbauen: Solange die Beziehung steht, laesst SQLite die Waise gar
            // nicht erst herein - das ist ja der Sinn des Schritts.
            TestDatenbank.ProjektFremdschluesselZuruecknehmen("Tab_Stromganglinie");

            long vorher = Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie");
            int waise = Ganglinie(OhneProjekt);
            int gute = Ganglinie(gutesProjekt);

            // Eine abhaengige Zeile an der Waise - sie muss mitfallen.
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_StromganglinieDaten (ID_Ganglinie, Wert) VALUES (?, 1.0)",
                new DbParam("p1", waise));
            long datenVorher = Zahl("SELECT COUNT(*) FROM Tab_StromganglinieDaten");

            Assert.Equal(1, ProjektFremdschluessel.Waisen("Tab_Stromganglinie"));

            var bericht = new List<string>();
            Assert.True(ProjektFremdschluessel.Umbauen("Tab_Stromganglinie", bericht));

            Assert.Equal(0, ProjektFremdschluessel.Waisen("Tab_Stromganglinie"));
            Assert.Equal(vorher + 1, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie"));
            Assert.Equal(datenVorher - 1, Zahl("SELECT COUNT(*) FROM Tab_StromganglinieDaten"));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie WHERE ID = " + gute));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie WHERE ID = " + waise));

            string zeile = Assert.Single(bericht);
            Assert.Contains("geloescht 1", zeile, StringComparison.Ordinal);
            Assert.Contains("mitgeloescht 1", zeile, StringComparison.Ordinal);

            Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows
                                       .Cast<DataRow>().ToList());
        }

        /// <summary>
        /// DIE HEILUNG: Eine Kennlinienzeile mit ungepflegter Projektspalte, die an einer
        /// gültigen Wärmepumpe hängt, wird NACHGEZOGEN statt gelöscht — das ist der Fall
        /// der 1.446 Zeilen der Testdatenbank, von denen 165 zum Referenzprojekt 1045
        /// gehören.
        /// </summary>
        [Fact]
        public void Eine_heilbare_Zeile_wird_nachgezogen_statt_geloescht()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataRow wp = DataRepository.GetDataTable(
                "SELECT w.ID, w.ID_Projekt FROM Tab_WP w " +
                "WHERE EXISTS (SELECT 1 FROM Tab_Projekt p WHERE p.ID = w.ID_Projekt) LIMIT 1").Rows[0];
            int idWp = Convert.ToInt32(wp["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(wp["ID_Projekt"], CultureInfo.InvariantCulture);

            // ERST zurueckbauen - die stehende Beziehung liesse die 0 nicht herein.
            TestDatenbank.ProjektFremdschluesselZuruecknehmen("Tab_Kenndaten");

            // Eine Zeile mit ungepflegter Projektspalte (0), aber gueltiger Waermepumpe.
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Kenndaten (ID_Projekt, ID_WP, Vorlauf, Temperatur, COP, Ptherm) " +
                "VALUES (0, ?, 35, 7, 4.2, 9.5)", new DbParam("p1", idWp));
            int idZeile = Convert.ToInt32(
                DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Kenndaten"), CultureInfo.InvariantCulture);

            long zeilenVorher = Zahl("SELECT COUNT(*) FROM Tab_Kenndaten");
            Assert.Equal(1, ProjektFremdschluessel.Waisen("Tab_Kenndaten"));

            var bericht = new List<string>();
            Assert.True(ProjektFremdschluessel.Umbauen("Tab_Kenndaten", bericht));

            // Die Zeile LEBT, und ihre Projektspalte zeigt jetzt auf das Projekt der Pumpe.
            Assert.Equal(zeilenVorher, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten"));
            Assert.Equal(idProjekt, Zahl("SELECT ID_Projekt FROM Tab_Kenndaten WHERE ID = " + idZeile));

            string zeile = Assert.Single(bericht);
            Assert.Contains("geheilt 1", zeile, StringComparison.Ordinal);
            Assert.Contains("geloescht 0", zeile, StringComparison.Ordinal);
        }

        /// <summary>
        /// DIE KASKADE, wofür der Schritt gemacht ist: Ein gelöschtes Projekt nimmt seine
        /// Zeilen in ALLEN achtundzwanzig Tabellen mit, und die Sichten bleiben
        /// abfragbar.
        /// </summary>
        [Fact]
        public void Ein_geloeschtes_Projekt_nimmt_seine_Zeilen_aus_allen_28_Tabellen_mit()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            // Das Projekt mit den meisten belegten Tabellen - so misst der Fall etwas.
            int idProjekt = Convert.ToInt32(
                DataRepository.ExecuteScalar(
                    "SELECT ID_Projekt FROM Tab_WP WHERE EXISTS " +
                    "(SELECT 1 FROM Tab_Projekt p WHERE p.ID = Tab_WP.ID_Projekt) LIMIT 1"),
                CultureInfo.InvariantCulture);

            var belegt = new List<string>();
            foreach (ProjektFremdschluessel.Eintrag e in ProjektFremdschluessel.Katalog)
            {
                if (!DataRepository.TabelleVorhanden(e.Tabelle)) continue;
                if (Zahl("SELECT COUNT(*) FROM \"" + e.Tabelle + "\" WHERE \"" + e.Spalten[0] +
                         "\" = " + idProjekt) > 0) belegt.Add(e.Tabelle);
            }
            Assert.NotEmpty(belegt);

            // Die Vorarbeiten des Loeschwegs bleiben aussen vor - hier soll die KASKADE
            // allein zeigen, was sie kann.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Projekt WHERE ID = " + idProjekt);

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + idProjekt));
            foreach (string tabelle in belegt)
            {
                ProjektFremdschluessel.Eintrag e = ProjektFremdschluessel.Finde(tabelle);
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" +
                                     e.Spalten[0] + "\" = " + idProjekt));
            }

            foreach (string sicht in Sichten().Keys)
                DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + sicht + "\"");

            Assert.Equal("ok", Skalar("PRAGMA integrity_check"));
        }

        /// <summary>
        /// Die Vorgangsklammer schaltet die Fremdschlüssel wieder EIN — ließe sie sie
        /// aus, trüge die nächste Ausleihe derselben Verbindung keine mehr, und eine
        /// Beziehungsverletzung ginge still durch.
        /// </summary>
        [Fact]
        public void Nach_dem_Vorgang_ohne_Fremdschluessel_stehen_sie_wieder()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                Assert.Equal(0L, Convert.ToInt64(v.Skalar("PRAGMA foreign_keys"),
                                                 CultureInfo.InvariantCulture));
                v.Commit();
            }

            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.Equal(1L, Convert.ToInt64(v.Skalar("PRAGMA foreign_keys"),
                                                 CultureInfo.InvariantCulture));
                v.Commit();
            }
        }

        // =============================================================================
        //  Kleinkram
        // =============================================================================

        private sealed class Zustandsbild
        {
            public long Zeilen;
            public List<long> Ids;
            public string Zaehlerstand;
            public List<string> Spalten;
            public List<string> Indizes;
            public List<string> ProjektBeziehungen;
            public List<string> FremdeBeziehungen;
        }

        private static Zustandsbild Zustand(string tabelle)
        {
            var bild = new Zustandsbild
            {
                Zeilen = Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\""),
                Ids = new List<long>(),
                Zaehlerstand = Skalar("SELECT seq FROM sqlite_sequence WHERE name = '" + tabelle + "'"),
                Spalten = DataRepository.SpaltenVonTabelle(tabelle),
                Indizes = new List<string>(),
                ProjektBeziehungen = new List<string>(),
                FremdeBeziehungen = new List<string>()
            };

            foreach (DataRow z in DataRepository.GetDataTable(
                         "SELECT ID FROM \"" + tabelle + "\" ORDER BY ID").Rows)
                bild.Ids.Add(Convert.ToInt64(z["ID"], CultureInfo.InvariantCulture));

            foreach (DataRow z in DataRepository.GetDataTable(
                         "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = ? ORDER BY name",
                         new DbParam("p1", tabelle)).Rows)
                bild.Indizes.Add(Convert.ToString(z["name"], CultureInfo.InvariantCulture));

            foreach (DataRow z in DataRepository.GetDataTable(
                         "SELECT \"table\" AS ziel, \"from\" AS von, on_delete AS del, on_update AS upd " +
                         "FROM pragma_foreign_key_list(?) ORDER BY \"table\", \"from\"",
                         new DbParam("p1", tabelle)).Rows)
            {
                string ziel = Convert.ToString(z["ziel"], CultureInfo.InvariantCulture);
                string text = ziel + "." + Convert.ToString(z["von"], CultureInfo.InvariantCulture) +
                              " del=" + Convert.ToString(z["del"], CultureInfo.InvariantCulture) +
                              " upd=" + Convert.ToString(z["upd"], CultureInfo.InvariantCulture);
                if (string.Equals(ziel, ProjektFremdschluessel.ZIEL, StringComparison.Ordinal))
                    bild.ProjektBeziehungen.Add(text);
                else
                    bild.FremdeBeziehungen.Add(text);
            }

            bild.ProjektBeziehungen.Sort(StringComparer.Ordinal);
            bild.FremdeBeziehungen.Sort(StringComparer.Ordinal);
            return bild;
        }

        private static Dictionary<string, string> Sichten()
        {
            var sichten = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (DataRow z in DataRepository.GetDataTable(
                         "SELECT name, sql FROM sqlite_master WHERE type = 'view' ORDER BY name").Rows)
                sichten[Convert.ToString(z["name"], CultureInfo.InvariantCulture)] =
                    Convert.ToString(z["sql"], CultureInfo.InvariantCulture);
            return sichten;
        }

        private static bool BeziehungSteht(string tabelle, string spalte, string ziel)
        {
            object wert = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM pragma_foreign_key_list(?) WHERE \"table\" = ? AND \"from\" = ?",
                new DbParam("p1", tabelle), new DbParam("p2", ziel), new DbParam("p3", spalte));
            return wert != null && Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }

        private static int Ganglinie(int idProjekt)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Stromganglinie (ID_Projekt, Bezeichner) VALUES (?, 'Probe 96')",
                new DbParam("p1", idProjekt));
            return Convert.ToInt32(
                DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Stromganglinie"),
                CultureInfo.InvariantCulture);
        }

        private static long Zahl(string sql)
        {
            object wert = DataRepository.ExecuteScalar(sql);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Skalar(string sql)
        {
            object wert = DataRepository.ExecuteScalar(sql);
            return wert == null || wert == DBNull.Value
                ? null
                : Convert.ToString(wert, CultureInfo.InvariantCulture);
        }

        private static int Vorkommen(string text, string suche)
        {
            int anzahl = 0;
            for (int i = text.IndexOf(suche, StringComparison.Ordinal); i >= 0;
                 i = text.IndexOf(suche, i + 1, StringComparison.Ordinal)) anzahl++;
            return anzahl;
        }
    }
}
