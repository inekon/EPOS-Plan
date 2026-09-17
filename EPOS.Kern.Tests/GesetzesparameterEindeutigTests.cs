using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Schemaschritts 87</b> — „eine Zeile je Schlüssel, Klasse und
    /// Stichjahr", ab diesem Schritt von der Datenbank gehalten und nicht mehr allein von
    /// der Pflegemaske (Anwenderentscheid US-E-1 (a) vom 17.09.2026, Ausgangslage aus
    /// Auftrag #319) — samt dem Beifang aus Auftrag #321: je Projekt genau EINE aktive
    /// Speichervariante.
    ///
    /// <para><b>Was hier geprüft wird.</b> Der eindeutige Index steht über genau den drei
    /// Schlüsselspalten; die Messlatte führt keine Dublette; ein zweiter Satz desselben
    /// Tripels wird abgewiesen; die Entdoppelung behält die Zeile mit der kleinsten ID
    /// und schreibt je entfernter Zeile eine Protokollzeile; ein zweiter Lauf ändert
    /// nichts. Dazu der Gegenbeweis, dass die Katalogsaat mit dem Index LEBT: Eine
    /// doppelte Saat endet in einer benannten Warnung, nicht im Abbruch der Generation.
    /// Und der Fall 1026: nach dem Schritt genau eine aktive Variante je Projekt.</para>
    ///
    /// <para><b>Warum die Fälle die Datenbank brauchen.</b> Der Index entsteht im
    /// Schemaschritt, und jeder Fall schreibt. Eine Arbeitskopie je FALL (Regel seit
    /// iU9‑W11a): geteilt sähe einer den anderen. Fehlt die Datei, schweigen die
    /// Fälle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GesetzesparameterEindeutigTests
    {
        // =================================================================
        //  Der Schemaschritt — der Gesetzeskatalog
        // =================================================================

        /// <summary>
        /// Der Index steht, ist EINDEUTIG und liegt ueber (Schluessel, Klasse, JahrVon) —
        /// genau das Tripel, das die Pflegemaske seit jeher prueft.
        /// </summary>
        [Fact]
        public void Der_eindeutige_Index_steht_ueber_Schluessel_Klasse_und_Jahr()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(1, Zahl(GesetzesparameterEindeutig.ZaehlungIndex()));

            DataTable liste = DataRepository.GetDataTable(
                "SELECT \"name\", \"unique\" FROM pragma_index_list(?)",
                new DbParam("@t", GesetzesparameterEindeutig.TABELLE));
            bool eindeutig = false;
            foreach (DataRow r in liste.Rows)
                if (string.Equals(Convert.ToString(r["name"]), GesetzesparameterEindeutig.INDEX,
                                  StringComparison.Ordinal))
                    eindeutig = Convert.ToInt32(r["unique"]) == 1;
            Assert.True(eindeutig, "Der Index " + GesetzesparameterEindeutig.INDEX +
                                   " steht nicht oder ist nicht eindeutig.");

            var spalten = new List<string>();
            DataTable info = DataRepository.GetDataTable(
                "SELECT \"name\" FROM pragma_index_info(?) ORDER BY \"seqno\"",
                new DbParam("@i", GesetzesparameterEindeutig.INDEX));
            foreach (DataRow r in info.Rows) spalten.Add(Convert.ToString(r["name"]));

            Assert.Equal(new[] { GesetzesparameterEindeutig.SPALTE_SCHLUESSEL,
                                 GesetzesparameterEindeutig.SPALTE_KLASSE,
                                 GesetzesparameterEindeutig.SPALTE_JAHR }, spalten);
        }

        /// <summary>
        /// Die Messlatte selbst ist sauber — sie hat nie ein Programm gestartet und
        /// deshalb auch nie abgebrochen gesaet.
        /// </summary>
        [Fact]
        public void Der_Katalog_der_Messlatte_fuehrt_keine_Dublette()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Zahl(GesetzesparameterEindeutig.Zaehlung()));
            Assert.False(GesetzesparameterEindeutig.EntdoppelungNoetig());
        }

        /// <summary>
        /// DIE EIGENTLICHE ZUSAGE: Eine zweite Zeile desselben Tripels kommt nicht mehr
        /// in die Datenbank.
        /// </summary>
        [Fact]
        public void Eine_zweite_Zeile_desselben_Tripels_wird_abgewiesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsteZeile(out string schluessel, out string klasse, out int jahr);
            Assert.True(id > 0);

            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren(Nachbau(), new DbParam("@id", id));
                v.Commit();
            });

            Assert.Equal(1, ZeilenDesTripels(schluessel, klasse, jahr));
        }

        /// <summary>
        /// DER SCHEMAFALL: Eine Arbeitskopie auf Stand 86 (Index gezogen) bekommt drei
        /// Zeilen desselben Tripels; die Entdoppelung laesst die mit der KLEINSTEN ID
        /// stehen, nennt die zwei entfernten je in einer Protokollzeile, danach steht der
        /// Index, und ein zweiter Lauf aendert nichts mehr.
        /// </summary>
        [Fact]
        public void Die_Entdoppelung_behaelt_die_kleinste_Id_und_protokolliert_jede_entfernte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsteZeile(out string schluessel, out string klasse, out int jahr);
            Assert.True(id > 0);

            // Stand 86: ohne den Index laesst die Datenbank die zweite Zeile wieder zu -
            // genau die Lage jeder produktiven Datei VOR diesem Schritt.
            DataRepository.ExecuteNonQuery("DROP INDEX \"" + GesetzesparameterEindeutig.INDEX + "\"");
            Assert.Equal(0, Zahl(GesetzesparameterEindeutig.ZaehlungIndex()));

            DataRepository.ExecuteSQL(Nachbau(), new DbParam("@id", id));
            DataRepository.ExecuteSQL(Nachbau(), new DbParam("@id", id));
            Assert.Equal(3, ZeilenDesTripels(schluessel, klasse, jahr));
            Assert.Equal(2, Zahl(GesetzesparameterEindeutig.Zaehlung()));
            Assert.True(GesetzesparameterEindeutig.EntdoppelungNoetig());

            // Zwei Protokollzeilen - je entfernter Zeile eine, mit ihrer ID.
            IList<string> protokoll = PvKoeffizientenReparatur.Zerlege(
                Text(GesetzesparameterEindeutig.Protokollabfrage()));
            Assert.Equal(2, protokoll.Count);
            foreach (string z in protokoll)
            {
                Assert.Contains(schluessel, z, StringComparison.Ordinal);
                Assert.Contains(klasse, z, StringComparison.Ordinal);
                Assert.Contains("Dublette entfernt", z, StringComparison.Ordinal);
            }

            DataRepository.ExecuteNonQuery(GesetzesparameterEindeutig.SQL_ENTDOPPELN);
            Assert.Equal(0, Zahl(GesetzesparameterEindeutig.Zaehlung()));
            Assert.Equal(1, ZeilenDesTripels(schluessel, klasse, jahr));
            Assert.Equal(id, KleinsteIdDesTripels(schluessel, klasse, jahr));

            // Danach laesst sich der Index anlegen - die Reihenfolge des Schritts.
            DataRepository.ExecuteNonQuery(GesetzesparameterEindeutig.SQL_INDEX);
            Assert.Equal(1, Zahl(GesetzesparameterEindeutig.ZaehlungIndex()));

            // Wiederholbar: ein zweiter Lauf findet nichts mehr, und die Protokollabfrage
            // liefert keine Zeile mehr.
            DataRepository.ExecuteNonQuery(GesetzesparameterEindeutig.SQL_ENTDOPPELN);
            Assert.Equal(0, Zahl(GesetzesparameterEindeutig.Zaehlung()));
            Assert.Empty(PvKoeffizientenReparatur.Zerlege(
                Text(GesetzesparameterEindeutig.Protokollabfrage())));
        }

        /// <summary>
        /// GEGENPROBE zur Entdoppelung: Haengt man sie aus — also legt man den Index auf
        /// einem unsauberen Bestand ohne vorheriges Entdoppeln an —, scheitert der
        /// Schritt. Genau dafuer steht die Entdoppelung VOR dem Index.
        /// </summary>
        [Fact]
        public void Ohne_Entdoppelung_scheitert_der_Index()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsteZeile(out _, out _, out _);
            Assert.True(id > 0);

            DataRepository.ExecuteNonQuery("DROP INDEX \"" + GesetzesparameterEindeutig.INDEX + "\"");
            DataRepository.ExecuteSQL(Nachbau(), new DbParam("@id", id));
            Assert.Equal(1, Zahl(GesetzesparameterEindeutig.Zaehlung()));

            Assert.ThrowsAny<Exception>(() =>
            {
                using DbVorgang v = DataRepository.Vorgang();
                v.Ausfuehren(GesetzesparameterEindeutig.SQL_INDEX);
                v.Commit();
            });
            Assert.Equal(0, Zahl(GesetzesparameterEindeutig.ZaehlungIndex()));
        }

        // =================================================================
        //  Die Saat lebt mit dem Index
        // =================================================================

        /// <summary>
        /// Die Katalogsaat darf am Index nicht mehr abbrechen: Steht eine Zeile der
        /// naechsten Generation schon da, wird sie BENANNT uebergangen, die Generation
        /// laeuft durch und der Marker steigt.
        ///
        /// <para>Nachgestellt wird das, indem der Marker um eine Generation
        /// zurueckgesetzt wird — dann versucht die Saat genau die Zeilen erneut, die
        /// schon dastehen. Vor AUFTRAG US-2 warf der erste dieser INSERTs, die Schleife
        /// brach ab und der Marker blieb unten; genau daraus entstanden die
        /// Dubletten.</para>
        /// </summary>
        [Fact]
        public void Eine_doppelte_Saat_warnt_benannt_und_bricht_die_Generation_nicht_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int ziel = GesetzKatalog.AktuelleGeneration;
            Assert.True(ziel > 1);

            int vorherZeilen = Zahl("SELECT COUNT(*) FROM " + GesetzesparameterEindeutig.TABELLE);

            // Marker eine Generation zurueck - die Saat versucht die Zeilen der letzten
            // Generation noch einmal.
            DataRepository.ExecuteSQL(
                "UPDATE " + GesetzesparameterEindeutig.TABELLE + " SET \"Wert\" = ? WHERE \"Schluessel\" = ?",
                new DbParam("@w", DbParamTyp.Double) { Wert = (double)(ziel - 1) },
                new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_KATALOG_GENERATION });

            GesetzKatalog.StelleKatalogSicher();

            // Der Marker steht wieder oben - die Generation ist NICHT abgebrochen.
            Assert.Equal(ziel, Zahl("SELECT CAST(\"Wert\" AS INTEGER) FROM " +
                                    GesetzesparameterEindeutig.TABELLE +
                                    " WHERE \"Schluessel\" = '" + DbWerte.GESETZ_KATALOG_GENERATION + "'"));

            // Keine einzige Zeile kam hinzu, keine Dublette entstand.
            Assert.Equal(vorherZeilen, Zahl("SELECT COUNT(*) FROM " + GesetzesparameterEindeutig.TABELLE));
            Assert.Equal(0, Zahl(GesetzesparameterEindeutig.Zaehlung()));
            Assert.Equal(0, GesetzKatalog.ZuletztNachgesaet);

            // Und jede uebergangene Zeile ist BENANNT.
            Assert.NotEmpty(GesetzKatalog.SaatWarnungen);
            foreach (string w in GesetzKatalog.SaatWarnungen)
                Assert.Contains("Schluessel ", w, StringComparison.Ordinal);
        }

        // =================================================================
        //  Der Beifang aus #321 — eine aktive Speichervariante je Projekt
        // =================================================================

        /// <summary>
        /// Nach dem Schritt fuehrt jedes Projekt hoechstens EINE aktive Speichervariante.
        /// Projekt 1026 fuehrte zwei (Anlage 11280, Varianten 10 und 13).
        /// </summary>
        [Fact]
        public void Jedes_Projekt_fuehrt_hoechstens_eine_aktive_Speichervariante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Zahl(SpeicherVarianteAktivEindeutig.Zaehlung()));
            Assert.False(SpeicherVarianteAktivEindeutig.EntdoppelungNoetig());

            // Auch je ANLAGE, die schaerfere Lesart des gemeldeten Falls.
            Assert.Equal(0, Zahl(
                "SELECT COUNT(*) FROM (SELECT \"" + SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE +
                "\" FROM \"" + SpeicherVarianteAktivEindeutig.TABELLE + "\" WHERE \"" +
                SpeicherVarianteAktivEindeutig.SPALTE_AKTIV + "\" = 1 GROUP BY \"" +
                SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE + "\" HAVING COUNT(*) > 1)"));
        }

        /// <summary>
        /// Die Entdoppelung behaelt je Projekt die aktive Zeile mit der KLEINSTEN ID —
        /// genau die, die <c>ReadAktiveVariante</c> schon bisher geliefert hat; jede
        /// abgeschaltete bekommt ihre Protokollzeile, und ein zweiter Lauf aendert
        /// nichts.
        /// </summary>
        [Fact]
        public void Die_Entdoppelung_der_aktiven_Varianten_behaelt_die_kleinste_Id()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"v\".\"ID\", \"a\".\"ID_Projekt\" FROM \"" +
                SpeicherVarianteAktivEindeutig.TABELLE + "\" AS \"v\" INNER JOIN \"" +
                SpeicherVarianteAktivEindeutig.TABELLE_ANLAGEN + "\" AS \"a\" ON \"a\".\"ID\" = \"v\".\"" +
                SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE + "\" WHERE \"v\".\"" +
                SpeicherVarianteAktivEindeutig.SPALTE_AKTIV + "\" = 1 ORDER BY \"v\".\"ID\" LIMIT 1");
            if (dt == null || dt.Rows.Count == 0) return;

            int aktiv = Convert.ToInt32(dt.Rows[0][0]);
            int projekt = Convert.ToInt32(dt.Rows[0][1]);

            // Eine zweite aktive Variante desselben Projekts nachstellen.
            int zweite = Zahl("SELECT COALESCE(MAX(\"ID\"), 0) + 1 FROM \"" +
                              SpeicherVarianteAktivEindeutig.TABELLE + "\"");
            DataRepository.ExecuteSQL(
                "INSERT INTO \"" + SpeicherVarianteAktivEindeutig.TABELLE +
                "\" (\"ID\", \"" + SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE + "\", \"" +
                SpeicherVarianteAktivEindeutig.SPALTE_AKTIV + "\") " +
                "SELECT ?, \"" + SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE + "\", 1 FROM \"" +
                SpeicherVarianteAktivEindeutig.TABELLE + "\" WHERE \"ID\" = ?",
                new DbParam("@neu", zweite), new DbParam("@id", aktiv));

            Assert.Equal(1, Zahl(SpeicherVarianteAktivEindeutig.Zaehlung()));

            IList<string> protokoll = PvKoeffizientenReparatur.Zerlege(
                Text(SpeicherVarianteAktivEindeutig.Protokollabfrage()));
            Assert.Single(protokoll);
            Assert.Contains("Projekt " + projekt, protokoll[0], StringComparison.Ordinal);
            Assert.Contains("(ID " + zweite + ")", protokoll[0], StringComparison.Ordinal);

            DataRepository.ExecuteNonQuery(SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN);
            Assert.Equal(0, Zahl(SpeicherVarianteAktivEindeutig.Zaehlung()));
            Assert.Equal(1, Zahl("SELECT \"" + SpeicherVarianteAktivEindeutig.SPALTE_AKTIV +
                                 "\" FROM \"" + SpeicherVarianteAktivEindeutig.TABELLE +
                                 "\" WHERE \"ID\" = " + aktiv));
            Assert.Equal(0, Zahl("SELECT \"" + SpeicherVarianteAktivEindeutig.SPALTE_AKTIV +
                                 "\" FROM \"" + SpeicherVarianteAktivEindeutig.TABELLE +
                                 "\" WHERE \"ID\" = " + zweite));

            // Wiederholbar.
            DataRepository.ExecuteNonQuery(SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN);
            Assert.Equal(0, Zahl(SpeicherVarianteAktivEindeutig.Zaehlung()));
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
            return "INSERT INTO \"" + GesetzesparameterEindeutig.TABELLE + "\" (\"" +
                   GesetzesparameterEindeutig.SPALTE_SCHLUESSEL + "\", \"" +
                   GesetzesparameterEindeutig.SPALTE_KLASSE + "\", \"" +
                   GesetzesparameterEindeutig.SPALTE_JAHR + "\", \"Wert\", \"Einheit\", " +
                   "\"Status\", \"Quelle\") " +
                   "SELECT \"" + GesetzesparameterEindeutig.SPALTE_SCHLUESSEL + "\", \"" +
                   GesetzesparameterEindeutig.SPALTE_KLASSE + "\", \"" +
                   GesetzesparameterEindeutig.SPALTE_JAHR + "\", \"Wert\", \"Einheit\", " +
                   "\"Status\", \"Quelle\" FROM \"" + GesetzesparameterEindeutig.TABELLE +
                   "\" WHERE \"ID\" = ?";
        }

        /// <summary>Die erste Katalogzeile samt ihrem Tripel.</summary>
        private static int ErsteZeile(out string schluessel, out string klasse, out int jahr)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID\", \"" + GesetzesparameterEindeutig.SPALTE_SCHLUESSEL + "\", \"" +
                GesetzesparameterEindeutig.SPALTE_KLASSE + "\", \"" +
                GesetzesparameterEindeutig.SPALTE_JAHR + "\" FROM \"" +
                GesetzesparameterEindeutig.TABELLE + "\" WHERE \"" +
                GesetzesparameterEindeutig.SPALTE_SCHLUESSEL + "\" IS NOT NULL AND \"" +
                GesetzesparameterEindeutig.SPALTE_KLASSE + "\" IS NOT NULL AND \"" +
                GesetzesparameterEindeutig.SPALTE_JAHR + "\" IS NOT NULL ORDER BY \"ID\" LIMIT 1");
            schluessel = "";
            klasse = "";
            jahr = 0;
            if (dt == null || dt.Rows.Count == 0) return 0;
            schluessel = Convert.ToString(dt.Rows[0][1]);
            klasse = Convert.ToString(dt.Rows[0][2]);
            jahr = Convert.ToInt32(dt.Rows[0][3]);
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        private static int ZeilenDesTripels(string schluessel, string klasse, int jahr)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + GesetzesparameterEindeutig.TABELLE + "\" WHERE \"" +
                GesetzesparameterEindeutig.SPALTE_SCHLUESSEL + "\" = ? AND \"" +
                GesetzesparameterEindeutig.SPALTE_KLASSE + "\" = ? AND \"" +
                GesetzesparameterEindeutig.SPALTE_JAHR + "\" = ?",
                new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = schluessel },
                new DbParam("@k", DbParamTyp.VarWChar, 40) { Wert = klasse },
                new DbParam("@j", DbParamTyp.Integer) { Wert = jahr });
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static int KleinsteIdDesTripels(string schluessel, string klasse, int jahr)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(\"ID\") FROM \"" + GesetzesparameterEindeutig.TABELLE + "\" WHERE \"" +
                GesetzesparameterEindeutig.SPALTE_SCHLUESSEL + "\" = ? AND \"" +
                GesetzesparameterEindeutig.SPALTE_KLASSE + "\" = ? AND \"" +
                GesetzesparameterEindeutig.SPALTE_JAHR + "\" = ?",
                new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = schluessel },
                new DbParam("@k", DbParamTyp.VarWChar, 40) { Wert = klasse },
                new DbParam("@j", DbParamTyp.Integer) { Wert = jahr });
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static string Text(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o) ?? "";
        }
    }
}
