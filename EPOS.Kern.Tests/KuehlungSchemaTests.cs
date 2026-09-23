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
    /// Die Schemaschritte der Kühlung, Stufe KU1 — <b>108 (KU-S1)</b>, <b>109 (KU-S2)</b>,
    /// <b>110 (KU-S4)</b> (Kühlkonzept Kapitel 7; Entscheide E27, E31).
    ///
    /// <para><b>Geprüft wird:</b> die Definitionen gegen das Papier (acht Gebäudespalten,
    /// eine Projekteinstellung, neun Ergebnisspalten — namentlich und mit Typ); der Stand
    /// der Testdatenbank (alle Strukturen stehen, alles leer bzw. 0, die Sicht ist die
    /// geltende, STRICT bleibt); KU-S1 aus dem Stand davor, wiederholbar; die Leser und
    /// Schreiber der Gebäudespalten NULL-erhaltend (Namensleser der Sicht, Katalogkopie,
    /// Insert/Overwrite); und dass ein Lauf die neun Ergebnisspalten leer lässt, solange das
    /// Projekt keine Kälte rechnet.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehlungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Der_Zielstand_ist_mindestens_110()
        {
            Assert.True(SchemaStand.Zielversion >= 110,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 110.");
        }

        /// <summary>
        /// KU-S1 nach Kühlkonzept 7.1: vier Spalten je Gebäudetabelle, acht Einträge;
        /// Sollwert, Grenze und Nachtwert nullbar, der Schalter 0/1 mit Vorgabe 0.
        /// </summary>
        [Fact]
        public void KU_S1_acht_Eintraege_vier_je_Gebaeudetabelle_mit_den_Typen_aus_7_1()
        {
            Assert.Equal(8, GebaeudeSchema.Kuehlspalten.Length);
            foreach (string t in GebaeudeSchema.TABELLEN)
                Assert.Equal(new[] { "Kuehl_Sollwert", "Kuehlleistung_Max", "Kuehlung_Aktiv", "Kuehl_Sollwert_Nacht" },
                             GebaeudeSchema.Kuehlspalten.Where(s => s.Tabelle == t).Select(s => s.Name).ToArray());

            foreach (SchemaSpalte s in GebaeudeSchema.Kuehlspalten)
            {
                Assert.True(s.Name.All(c => c < 128), s.Name + " ist nicht ASCII.");
                Assert.DoesNotContain(s.Name, GebaeudeSchema.NEUE_SPALTEN.Select(n => n.Key));
                string typ = StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition);
                if (GebaeudeSchema.KUEHL_SCHALTER.Contains(s.Name))
                    Assert.Equal("INTEGER NOT NULL DEFAULT 0 CHECK (\"" + s.Name + "\" IN (0,1))", typ);
                else
                    Assert.Equal("REAL", typ);      // nullbar, ohne Vorgabe
            }
        }

        /// <summary>
        /// Der zweite Sichtneubau: die 73 Spalten von M3 an ihren Stellen, dahinter die vier
        /// Kühlspalten. Die Bauvorschrift ist dieselbe (<see cref="GebaeudeSchema.SichtSql"/>).
        /// </summary>
        [Fact]
        public void KU_S1_haengt_die_vier_Spalten_hinter_M3_an_die_Sicht()
        {
            Assert.Equal(77, GebaeudeSchema.SICHT_KUEHLUNG.Length);
            Assert.Equal(GebaeudeSchema.SICHT_ALLE, GebaeudeSchema.SICHT_KUEHLUNG.Take(73));
            Assert.Equal(GebaeudeSchema.KUEHL_SPALTEN.Select(s => s.Key), GebaeudeSchema.SICHT_KUEHLUNG.Skip(73));

            Assert.Equal(GebaeudeSchema.SichtSql(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key)),
                         GebaeudeSchema.SQL_VIEW_NEU);
            foreach (var s in GebaeudeSchema.KUEHL_SPALTEN)
            {
                Assert.Contains("Tab_Gebaeude." + s.Key, GebaeudeSchema.SQL_VIEW_KUEHLUNG, StringComparison.Ordinal);
                Assert.DoesNotContain("Tab_Gebaeude." + s.Key, GebaeudeSchema.SQL_VIEW_NEU, StringComparison.Ordinal);
            }
            Assert.Equal(GebaeudeSchema.SQL_VIEW_KUEHLUNG, GebaeudeSchema.SQL_VIEW_AKTUELL);
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUNG, GebaeudeSchema.SICHT_AKTUELL);
        }

        /// <summary>KU-S2 nach Kühlkonzept 7.2: eine Spalte in <c>Tab_Einstellungen</c>, 0/1, Vorgabe 0.</summary>
        [Fact]
        public void KU_S2_eine_Projekteinstellung_0_1_mit_Vorgabe_0()
        {
            SchemaSpalte s = Assert.Single(KuehlungSchema.Projekteinstellung);
            Assert.Equal("Tab_Einstellungen", s.Tabelle);
            Assert.Equal("Kuehlbetrieb", s.Name);
            Assert.Equal("INTEGER NOT NULL DEFAULT 0 CHECK (\"Kuehlbetrieb\" IN (0,1))",
                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
        }

        /// <summary>
        /// KU-S4 nach Kühlkonzept 7.4: <b>neun</b> Spalten (E21), in der Reihenfolge des Papiers,
        /// alle nullbar — und keine zehnte für den Kältestrom (K24/K18a, E27).
        /// </summary>
        [Fact]
        public void KU_S4_neun_Ergebnisspalten_nach_7_4_ohne_Kaeltestrom()
        {
            string[] soll =
            {
                "Tab_ErgebnisEnergiebedarf.Waermebedarf_Kuehlung",
                "Tab_ErgebnisWaermepumpe.Deckung_Kuehlung",
                "Tab_ErgebnisHeizkessel.Deckung_Kuehlung",
                "Tab_ErgebnisBHKW.Deckung_Kuehlung",
                "Tab_ErgebnisSolarthermie.Deckung_Kuehlung",
                "Tab_ErgebnisPufferspeicher.Entladung_Kuehlung",
                "Tab_ErgebnisEnergiebedarf.Kaeltebedarf_Gesamt",
                "Tab_ErgebnisEnergiebedarf.Kaeltelast_Max",
                "Tab_ErgebnisEnergiebedarf.Kaelterestbedarf",
            };
            Assert.Equal(soll, KuehlungSchema.Ergebnisspalten.Select(s => s.Tabelle + "." + s.Name));
            foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten)
            {
                Assert.Equal("REAL", StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                Assert.DoesNotContain("strom", s.Name, StringComparison.OrdinalIgnoreCase);
            }

            // Wie Schritt 52: nicht in der Rückfallebene der Eingabeseite.
            var rueckfall = new HashSet<string>(SchemaKatalog.Alle.Select(s => s.Tabelle + "." + s.Name),
                                                StringComparer.OrdinalIgnoreCase);
            foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten.Concat(KuehlungSchema.Projekteinstellung)
                                                                     .Concat(GebaeudeSchema.Kuehlspalten))
                Assert.DoesNotContain(s.Tabelle + "." + s.Name, rueckfall);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank auf Stand 110
        // =============================================================================

        /// <summary>
        /// Alle Strukturen stehen, und alle sind leer: Kühleingaben NULL (der Schalter 0),
        /// <c>Kuehlbetrieb</c> in jedem Projekt 0, die Ergebnisspalten jedes gespeicherten
        /// Laufs NULL. Die Sicht ist die geltende, und die angefassten Tabellen bleiben STRICT.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_110_mit_leeren_Kuehlstrukturen()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= 110);
            Assert.True(GebaeudeSchema.Vollstaendig());
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.True(KuehlungSchema.ProjekteinstellungVollstaendig());
            Assert.True(KuehlungSchema.ErgebnisspaltenVollstaendig());

            string sicht = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_KUEHLUNG, sicht);
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUNG, GebaeudeSchema.SichtSpalten());

            foreach (SchemaSpalte s in GebaeudeSchema.Kuehlspalten)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name +
                                      "] IS NOT NULL AND [" + s.Name + "] <> 0"));
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Einstellungen") > 0);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Kuehlbetrieb <> 0"));
            foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL"));

            foreach (string t in GebaeudeSchema.Kuehlspalten.Concat(KuehlungSchema.Projekteinstellung)
                                                            .Concat(KuehlungSchema.Ergebnisspalten)
                                                            .Select(s => s.Tabelle).Distinct())
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// KU-S1 aus dem Stand VOR ihm: Sicht und Spalten werden entfernt, der Schritt legt die
        /// acht Spalten an und baut die Sicht neu; die Bestandswerte bleiben, M3 steht weiter,
        /// und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void KU_S1_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();

            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (SchemaSpalte s in GebaeudeSchema.Kuehlspalten)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_NEU);    // der Stand nach Schritt 101
            Assert.True(GebaeudeSchema.Vollstaendig());
            Assert.False(GebaeudeSchema.KuehlspaltenVollstaendig());

            var bericht = new List<string>();
            Assert.Equal(8, GebaeudeSchema.KuehlspaltenAlle(bericht));
            Assert.Contains(bericht, z => z.Contains("8 von 8 Kuehlspalte(n) angelegt"));
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.True(GebaeudeSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());

            Assert.Equal(0, GebaeudeSchema.KuehlspaltenAlle(null));
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUNG, GebaeudeSchema.SichtSpalten());
        }

        // =============================================================================
        //  Teil 3 - Leser und Schreiber der Gebäudespalten, NULL-erhaltend
        // =============================================================================

        /// <summary>
        /// DER NAMENSLESER DER SICHT: <c>ProjektGebaeudeCtrl</c> liefert die Kühleingaben eines
        /// Gebäudes so, wie sie in der Tabelle stehen — ein gesetzter Sollwert und der Schalter
        /// kommen an, die leere Grenze bleibt <c>null</c>; die übrigen Gebäude bleiben „aus".
        /// </summary>
        [Fact]
        public void Der_Namensleser_liefert_die_Kuehleingaben_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            DataRow g = DataRepository.GetDataTable(
                "SELECT g.ID, z.ID_Projekt FROM Tab_Gebaeude g INNER JOIN Z_ProjektGebaeude z " +
                "ON z.ID = g.ID_ProjektGebaeude ORDER BY g.ID LIMIT 1").Rows[0];
            int idGebaeude = Convert.ToInt32(g["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(g["ID_Projekt"], CultureInfo.InvariantCulture);

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Gebaeude SET Kuehl_Sollwert = ?, Kuehlung_Aktiv = 1, Kuehl_Sollwert_Nacht = ? WHERE ID = ?",
                new DbParam("?", 26.0), new DbParam("?", 28.0), new DbParam("?", idGebaeude));

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel m = ctrl.items.Single(x => x.ID_Gebaeude == idGebaeude);
            Assert.Equal(26.0, m.Kuehl_Sollwert);
            Assert.True(m.Kuehlung_Aktiv);
            Assert.Null(m.Kuehlleistung_Max);
            Assert.Equal(28.0, m.Kuehl_Sollwert_Nacht);

            foreach (ProjektGebaeudeModel andere in ctrl.items.Where(x => x.ID_Gebaeude != idGebaeude))
            {
                Assert.Null(andere.Kuehl_Sollwert);
                Assert.False(andere.Kuehlung_Aktiv);
            }
        }

        /// <summary>
        /// DIE KATALOGKOPIE HÄLT NULL: Ein Katalogsatz mit gesetztem Sollwert und Schalter und
        /// leerer Grenze kommt so im Projekt an — NULL wird nicht zu 0.
        /// </summary>
        [Fact]
        public void Die_Katalogkopie_haelt_NULL_und_traegt_die_gesetzten_Kuehlwerte()
        {
            if (!_db.Vorhanden) return;

            DataRow stamm = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0];
            int idStamm = Convert.ToInt32(stamm["ID"], CultureInfo.InvariantCulture);
            string bezeichner = Convert.ToString(stamm["Bezeichner"], CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Gebaeude_STAMM SET Kuehl_Sollwert = ?, Kuehlung_Aktiv = 1 WHERE ID = ?",
                new DbParam("?", 25.0), new DbParam("?", idStamm));

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idNeu = new GebaeudeStammCtrl().CopyFromStamm(
                bezeichner, Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture),
                Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture));
            Assert.True(idNeu > 0);

            DataRow neu = DataRepository.GetDataTable(
                "SELECT * FROM Tab_Gebaeude WHERE ID = ?", new DbParam("?", idNeu)).Rows[0];
            Assert.Equal(25.0, Convert.ToDouble(neu["Kuehl_Sollwert"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(neu["Kuehlung_Aktiv"], CultureInfo.InvariantCulture));
            Assert.True(neu["Kuehlleistung_Max"] == DBNull.Value, "Kuehlleistung_Max ist in der Kopie nicht NULL.");
            Assert.True(neu["Kuehl_Sollwert_Nacht"] == DBNull.Value, "Kuehl_Sollwert_Nacht ist in der Kopie nicht NULL.");

            // Und der Leser der Projektzeile (GebaeudeCtrl ueber NeueSpaltenLesen) sieht dasselbe.
            var projekt = new GebaeudeCtrl();
            projekt.ReadAll("ID = " + idNeu.ToString(CultureInfo.InvariantCulture));
            GebaeudeModel gelesen = Assert.Single(projekt.items);
            Assert.Equal(25.0, gelesen.Kuehl_Sollwert);
            Assert.True(gelesen.Kuehlung_Aktiv);
            Assert.Null(gelesen.Kuehlleistung_Max);
        }

        /// <summary>
        /// DER KATALOGSCHREIBWEG HÄLT NULL: Insert und Overwrite schreiben die Kühlfelder des
        /// Modells so, wie sie sind; das Lesen bringt NULL als <c>null</c> zurück.
        /// </summary>
        [Fact]
        public void Insert_Overwrite_und_Lesen_halten_die_Kuehlfelder_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "KU-S1-Probe Gebaeude";
            var m = new GebaeudeModel
            {
                Gebaeudename = NAME,
                Nutzflaeche = 100,
                Kuehlleistung_Max = 40,
                Kuehlung_Aktiv = true,
            };
            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(m));

            GebaeudeModel gelesen = Lesen(NAME);
            Assert.Null(gelesen.Kuehl_Sollwert);
            Assert.Equal(40.0, gelesen.Kuehlleistung_Max);
            Assert.True(gelesen.Kuehlung_Aktiv);
            Assert.Null(gelesen.Kuehl_Sollwert_Nacht);

            gelesen.Kuehl_Sollwert = 26;
            gelesen.Kuehlleistung_Max = null;
            gelesen.Kuehlung_Aktiv = false;
            Assert.True(ctrl.Overwrite(gelesen));

            GebaeudeModel wieder = Lesen(NAME);
            Assert.Equal(26.0, wieder.Kuehl_Sollwert);
            Assert.Null(wieder.Kuehlleistung_Max);
            Assert.False(wieder.Kuehlung_Aktiv);
        }

        // =============================================================================
        //  Teil 4 - ohne Kuehlbetrieb bleiben die Ergebnisspalten leer
        // =============================================================================

        /// <summary>
        /// Ein Lauf auf der Testdatenbank schreibt seine Ergebniszeilen wie bisher — und lässt
        /// die neun Spalten von KU-S4 leer: NULL heißt „nicht erhoben". Seit der zweiten Welle
        /// erhebt der Kanal Kälte, aber nur mit dem Projektschalter <c>Kuehlbetrieb</c>; jedes
        /// Referenzprojekt steht auf aus (Gegenfall mit Kühlung: <c>KaeltebedarfTests</c>).
        /// </summary>
        [Fact]
        public void Ein_Lauf_laesst_die_neun_Ergebnisspalten_leer()
        {
            if (!_db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            int kopf = laeufer.SimuliereUndSpeichere(1030, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            // Alle sechs Ergebnistabellen haengen ueber ID_Ergebnis am Kopf des Laufs.
            string lauf = kopf.ToString(CultureInfo.InvariantCulture);
            long zeilen = 0;
            foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten)
            {
                zeilen += Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE ID_Ergebnis = " + lauf);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE ID_Ergebnis = " + lauf +
                                      " AND [" + s.Name + "] IS NOT NULL"));
            }
            Assert.True(zeilen > 0, "Der Lauf hat keine Ergebniszeile geschrieben.");
        }

        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        /// <summary>Die Bestandswerte der Gebäudetabellen, an denen KU-S1 nichts ändern darf.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Nutzflaeche, Maximaleraumtemperatur, Heizleistung_Max FROM [" + t + "] ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(t + ":" + string.Join("|", r.ItemArray.Select(
                        v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }
    }
}
