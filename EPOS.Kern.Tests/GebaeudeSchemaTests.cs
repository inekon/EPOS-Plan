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
    /// Schemaschritt <b>101</b> — der Gebaeudespalten-Schritt M3 der Gebaeudesimulation
    /// (Stufe G1; Umsetzungskonzept Gebaeudesimulation 1.6, 1.7, 1.9; Entscheide E19,
    /// E27/U5, F-S1, F-S2).
    ///
    /// <para><b>Geprueft wird:</b> die Definitionen (dreissig Spalten, Sicht mit 58
    /// Bestandsspalten an unveraenderter Stelle und fuenfzehn neuen dahinter, die acht
    /// Bezeichner mit Umlaut oder Eszett buchstabengetreu); der Stand der Testdatenbank
    /// (Umbenennung, Spalten, Sicht aus <c>SQL_VIEW_NEU</c>); die Probe des Sichtneubaus
    /// (<c>ProjektGebaeudeCtrl</c> liefert alle Bestandsfelder aus der Tabelle); die
    /// NULL-erhaltende Katalogkopie und der Katalogschreibweg; und der Schritt selbst aus
    /// dem Stand VOR ihm, wiederholbar.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die acht Bestandsbezeichner mit Umlaut oder Eszett, buchstabengetreu.</summary>
        private static readonly string[] NICHT_ASCII =
        {
            "k_Wert_Außenwand", "Flaeche_Außenwand",
            "WBVK_Anschluß_Fenster_Wand", "WBVK_Anschluß_Wand_Dach",
            "WBVK_Anschluß_Außenwand_Kellerdecke",
            "Abmessung_Anschluß_Fenster_Wand", "Abmessung_Anschluß_Wand_Dach",
            "Abmessung_Anschluß_Außenwand_Kellerdecke",
        };

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Der_Zielstand_ist_mindestens_101()
        {
            Assert.True(SchemaStand.Zielversion >= 101,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 101.");
        }

        [Fact]
        public void Dreissig_Spalten_fuenfzehn_je_Tabelle_ascii_und_ohne_Dubletten()
        {
            Assert.Equal(30, GebaeudeSchema.Gebaeudespalten.Length);
            foreach (string t in GebaeudeSchema.TABELLEN)
                Assert.Equal(15, GebaeudeSchema.Gebaeudespalten.Count(s => s.Tabelle == t));
            Assert.Equal(30, GebaeudeSchema.Gebaeudespalten
                                .Select(s => s.Tabelle + "." + s.Name).Distinct().Count());
            foreach (SchemaSpalte s in GebaeudeSchema.Gebaeudespalten)
                Assert.True(s.Name.All(c => c < 128), s.Name + " ist nicht ASCII.");

            // Schalter: YESNO -> NOT NULL DEFAULT 0 mit CHECK; sonst keine DDL-Vorgabe.
            foreach (SchemaSpalte s in GebaeudeSchema.Gebaeudespalten)
            {
                string typ = StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition);
                if (GebaeudeSchema.SCHALTER.Contains(s.Name))
                    Assert.Equal("INTEGER NOT NULL DEFAULT 0 CHECK (\"" + s.Name + "\" IN (0,1))", typ);
                else
                    Assert.DoesNotContain("DEFAULT", typ, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Die_Sicht_behaelt_die_58_Bestandsstellen_und_haengt_die_neuen_hinter_ID()
        {
            Assert.Equal(58, GebaeudeSchema.SICHT_BESTAND.Length);
            Assert.Equal("ID_Projekt", GebaeudeSchema.SICHT_BESTAND[0]);
            Assert.Equal("Nutzflaeche", GebaeudeSchema.SICHT_BESTAND[32]);
            Assert.Equal("ID", GebaeudeSchema.SICHT_BESTAND[57]);
            Assert.DoesNotContain("Wohnflaeche", GebaeudeSchema.SICHT_BESTAND);
            Assert.Equal(73, GebaeudeSchema.SICHT_ALLE.Length);
            Assert.Equal(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key),
                         GebaeudeSchema.SICHT_ALLE.Skip(58));

            // Das neue Stueck der Sichtdefinition steht HINTER Tab_Gebaeude.ID.
            string sql = GebaeudeSchema.SQL_VIEW_NEU;
            Assert.True(sql.IndexOf("Tab_Gebaeude.ID, Tab_Gebaeude.Gebaeude_Modell", StringComparison.Ordinal) > 0);
            Assert.Contains("Tab_Gebaeude.Nutzflaeche, Tab_Gebaeude.Raumhoehe", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("Tab_Gebaeude.Wohnflaeche,", sql, StringComparison.Ordinal);
        }

        /// <summary>
        /// FELDBESTAND NAMENTLICH: Die acht nicht-ASCII-Bezeichner stehen buchstabengetreu in
        /// der Bestandsliste, in der Sichtdefinition und im Namensleser (BETRIEB_SQLITE.md
        /// 6.1) - ein grosses <c>SS</c> oder ein <c>ss</c> faende SQLite nicht.
        /// </summary>
        [Fact]
        public void Die_acht_Umlautbezeichner_stehen_buchstabengetreu()
        {
            foreach (string n in NICHT_ASCII)
            {
                Assert.Contains(n, GebaeudeSchema.SICHT_BESTAND);
                Assert.Contains("Tab_Gebaeude." + n, GebaeudeSchema.SQL_VIEW_NEU, StringComparison.Ordinal);
            }
            Assert.Equal(8, GebaeudeSchema.SICHT_BESTAND.Count(n => n.Any(c => c >= 128)));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank auf Stand 101
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;

            Assert.True(GebaeudeSchema.Vollstaendig());
            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.True(DataRepository.SpalteVorhanden(t, "Nutzflaeche"), t);
                Assert.False(DataRepository.SpalteVorhanden(t, "Wohnflaeche"), t);
                Assert.True(DataRepository.SpalteVorhanden(t, "Wohnflaeche_gesamt"), t);
            }

            // Die Sicht in der Datei ist wortgleich die GELTENDE: die des letzten Sichtneubaus
            // (die des Baujahrs, die die von M3 um die vier Kuehlspalten, die dreizehn
            // Uebergabespalten, die acht der Kuehluebergabe und das Baujahr verlaengert) - die 73 Spalten des Schritts 101 stehen
            // weiter an 0..72.
            string gespeichert = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, gespeichert);
            Assert.Equal(GebaeudeSchema.SICHT_ALLE,
                         GebaeudeSchema.SichtSpalten().Take(GebaeudeSchema.SICHT_ALLE.Length));

            // Die Sicht meldet die acht Umlautbezeichner buchstabengetreu.
            List<string> sicht = GebaeudeSchema.SichtSpalten();
            foreach (string n in NICHT_ASCII)
                Assert.Contains(n, sicht);

            // Die neuen Spalten stehen im Bestand leer (die Schalter auf 0). Einzige Ausnahme
            // ist die gesäte Zelle des Referenzprojekts auf dem Altweg (A15): Gebäude 10645 in
            // Projekt 1040 trägt Gebaeude_Modell = 'TAGESBILANZ' — bis zur Stufe GA
            // (Referenzlaeufe/Skripte/gebaeude_1040_tagesbilanz.py).
            foreach (SchemaSpalte s in GebaeudeSchema.Gebaeudespalten)
            {
                string ausnahme = s.Tabelle == "Tab_Gebaeude" && s.Name == "Gebaeude_Modell"
                    ? " AND NOT (ID = " + GebaeudeRueckwegTests.GEBAEUDE + " AND [Gebaeude_Modell] = '" +
                      DbWerte.GEBAEUDE_MODELL_TAGESBILANZ + "')"
                    : "";
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL AND [" +
                    s.Name + "] <> 0" + ausnahme);
                Assert.Equal(0L, Convert.ToInt64(n, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// DIE PROBE DES SICHTNEUBAUS: <c>ProjektGebaeudeCtrl</c> liefert fuer jedes Gebaeude
        /// jedes Projekts genau die Werte der Tabelle - alle 58 Bestandsfelder, gelesen beim
        /// Namen. Verglichen wird gegen eine eigene Abfrage auf <c>Tab_Gebaeude</c> und
        /// <c>Z_ProjektGebaeude</c>, nicht gegen die Sicht.
        /// </summary>
        [Fact]
        public void Der_Namensleser_liefert_alle_Bestandsfelder_aus_der_Tabelle()
        {
            if (!_db.Vorhanden) return;

            DataTable projekte = DataRepository.GetDataTable(
                "SELECT DISTINCT ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID_Projekt");
            int gebaeude = 0;
            foreach (DataRow p in projekte.Rows)
            {
                int idProjekt = Convert.ToInt32(p[0], CultureInfo.InvariantCulture);
                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(idProjekt);

                foreach (ProjektGebaeudeModel m in ctrl.items)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT g.*, z.Wohnflaeche_Waermebedarf, z.Jahresnutzungsgrad FROM Tab_Gebaeude g " +
                        "INNER JOIN Z_ProjektGebaeude z ON z.ID = g.ID_ProjektGebaeude WHERE g.ID = ?",
                        new DbParam("?", m.ID_Gebaeude));
                    Assert.Single(dt.Rows);
                    DataRow r = dt.Rows[0];

                    Assert.Equal(idProjekt, m.ID_Projekt);
                    Gleich(r, "Wohnflaeche_Waermebedarf", m.Z_AuswahlWohnflaeche);
                    Gleich(r, "Jahresnutzungsgrad", m.Jahresnutzungsgrad);
                    Gleich(r, "Gebaeudename", m.Gebaeudename);
                    Gleich(r, "Wohnflaeche_gesamt", m.Wohnflaeche_gesamt);
                    Gleich(r, "Bauweise", m.Bauweise);
                    Gleich(r, "Fensterflaeche_Ost_West", m.Fensterflaeche_OstWest);
                    Gleich(r, "Nutzflaeche", m.Nutzflaeche);
                    Gleich(r, "Raumhoehe", m.Raumhoehe);
                    Gleich(r, "k_Wert_Außenwand", m.k_Wert_Außenwand);
                    Gleich(r, "Flaeche_Außenwand", m.Flaeche_Außenwand);
                    Gleich(r, "WBVK_Anschluß_Fenster_Wand", m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand);
                    Gleich(r, "WBVK_Anschluß_Wand_Dach", m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach);
                    Gleich(r, "WBVK_Anschluß_Außenwand_Kellerdecke", m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke);
                    Gleich(r, "Abmessung_Anschluß_Fenster_Wand", m.Abmessung_Anschluß_Fenster_Wand);
                    Gleich(r, "Abmessung_Anschluß_Wand_Dach", m.Abmessung_Anschluß_Wand_Dach);
                    Gleich(r, "Abmessung_Anschluß_Außenwand_Kellerdecke", m.Abmessung_Anschluß_Außenwand_Kellerdecke);
                    Gleich(r, "Luftwechselrate", m.Luftwechselrate);
                    Gleich(r, "Ferienende_4", m.Ferienende_4);
                    Gleich(r, "Waermebedarf", m.Waermebedarf);
                    Gleich(r, "Wohngebaeude_Nicht_Wohngebaeude", m.Wohngebaeude_Nicht_Wohngebaeude);
                    gebaeude++;
                }
            }
            Assert.True(gebaeude >= 13, "nur " + gebaeude + " Gebaeude gelesen");
        }

        // =============================================================================
        //  Teil 3 - Katalog: Schreibweg und Kopie NULL-erhaltend
        // =============================================================================

        /// <summary>
        /// DIE KATALOGKOPIE HAELT NULL: Ein Katalogsatz mit einem gesetzten und vierzehn
        /// leeren neuen Feldern kommt so im Projekt an - NULL wird nicht zu 0 und nicht zu
        /// <c>""</c>; der gesetzte Schalter kommt als 1 an.
        /// </summary>
        [Fact]
        public void Die_Katalogkopie_haelt_NULL_und_traegt_gesetzte_Werte()
        {
            if (!_db.Vorhanden) return;

            DataRow stamm = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Nutzflaeche FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0];
            int idStamm = Convert.ToInt32(stamm["ID"], CultureInfo.InvariantCulture);
            string bezeichner = Convert.ToString(stamm["Bezeichner"], CultureInfo.InvariantCulture);

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Gebaeude_STAMM SET Rahmenanteil = ?, Sommerlueftung = 1 WHERE ID = ?",
                new DbParam("?", 0.25), new DbParam("?", idStamm));

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idNeu = new GebaeudeStammCtrl().CopyFromStamm(
                bezeichner, Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture),
                Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture));
            Assert.True(idNeu > 0);

            DataRow neu = DataRepository.GetDataTable(
                "SELECT * FROM Tab_Gebaeude WHERE ID = ?", new DbParam("?", idNeu)).Rows[0];
            Assert.Equal(0.25, Convert.ToDouble(neu["Rahmenanteil"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(neu["Sommerlueftung"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(neu["Aussenbauteile_Strahlung"], CultureInfo.InvariantCulture));
            Assert.Equal(stamm["Nutzflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(stamm["Nutzflaeche"], CultureInfo.InvariantCulture),
                         Convert.ToDouble(neu["Nutzflaeche"], CultureInfo.InvariantCulture));
            foreach (var s in GebaeudeSchema.NEUE_SPALTEN)
            {
                if (s.Key == GebaeudeSchema.SPALTE_RAHMENANTEIL || GebaeudeSchema.SCHALTER.Contains(s.Key)) continue;
                Assert.True(neu[s.Key] == DBNull.Value, s.Key + " ist in der Kopie nicht NULL.");
            }
        }

        /// <summary>
        /// DER KATALOGSCHREIBWEG HAELT NULL: Insert und Overwrite schreiben die neuen Felder
        /// des Modells so, wie sie sind; das Lesen bringt NULL als <c>null</c> zurueck.
        /// </summary>
        [Fact]
        public void Insert_Overwrite_und_Lesen_halten_NULL()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "M3-Probe Gebaeude";
            var m = new GebaeudeModel
            {
                Gebaeudename = NAME,
                Nutzflaeche = 100,
                Heizleistung_Max = 12.5,
                Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER,
                Aussenbauteile_Strahlung = true,
            };
            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(m));

            GebaeudeModel gelesen = Lesen(NAME);
            Assert.Equal(100.0, gelesen.Nutzflaeche);
            Assert.Equal(12.5, gelesen.Heizleistung_Max);
            Assert.Equal(DbWerte.GRUND_KELLER, gelesen.Grundflaeche_Randbedingung);
            Assert.True(gelesen.Aussenbauteile_Strahlung);
            Assert.False(gelesen.Sommerlueftung);
            Assert.Null(gelesen.Gebaeude_Modell);
            Assert.Null(gelesen.Rahmenanteil);
            Assert.Null(gelesen.Fensterflaeche_Ost);
            Assert.Null(gelesen.Luftwechsel_Nutzer);

            gelesen.Heizleistung_Max = null;
            gelesen.Rahmenanteil = 0.2;
            Assert.True(ctrl.Overwrite(gelesen));

            GebaeudeModel wieder = Lesen(NAME);
            Assert.Null(wieder.Heizleistung_Max);
            Assert.Equal(0.2, wieder.Rahmenanteil);
            Assert.Equal(DbWerte.GRUND_KELLER, wieder.Grundflaeche_Randbedingung);
        }

        // =============================================================================
        //  Teil 4 - der Schritt aus dem Stand VOR ihm
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand vor ihm: Die Probe stellt die alte Sicht und den alten
        /// Spaltennamen her und laesst den Schritt laufen. Die Werte gehen 1:1 hinueber, die
        /// Sicht steht wieder, und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void Der_Schritt_benennt_um_baut_die_Sicht_neu_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            var vorher = Flaechen();

            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (string t in GebaeudeSchema.TABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" RENAME COLUMN \"Nutzflaeche\" TO \"Wohnflaeche\"");
            Assert.False(GebaeudeSchema.Vollstaendig());

            var bericht = new List<string>();
            Assert.Equal(0, GebaeudeSchema.Alle(bericht));      // die Spalten stehen schon
            Assert.Contains(bericht, z => z.Contains("Tab_Gebaeude: Wohnflaeche -> Nutzflaeche"));
            Assert.Contains(bericht, z => z.Contains("Tab_Gebaeude_STAMM: Wohnflaeche -> Nutzflaeche"));
            Assert.True(GebaeudeSchema.Vollstaendig());
            Assert.Equal(vorher, Flaechen());

            Assert.Equal(0, GebaeudeSchema.Alle(null));
            Assert.True(GebaeudeSchema.Vollstaendig());
        }

        // -----------------------------------------------------------------------------

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        private static List<string> Flaechen()
        {
            var liste = new List<string>();
            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                DataTable dt = DataRepository.GetDataTable("SELECT ID, Nutzflaeche FROM [" + t + "] ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    liste.Add(t + ":" + Convert.ToString(r[0], CultureInfo.InvariantCulture) + "=" +
                              Convert.ToString(r[1], CultureInfo.InvariantCulture));
            }
            return liste;
        }

        /// <summary>Wert der Tabelle gegen das Modellfeld; NULL laesst das Feld auf seiner
        /// Vorbelegung und wird deshalb nicht verglichen.</summary>
        private static void Gleich(DataRow r, string spalte, double wert)
        {
            if (r[spalte] == DBNull.Value) return;
            Assert.Equal(Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture), wert);
        }

        private static void Gleich(DataRow r, string spalte, string wert)
        {
            if (r[spalte] == DBNull.Value) return;
            Assert.Equal(r[spalte].ToString(), wert);
        }
    }
}
