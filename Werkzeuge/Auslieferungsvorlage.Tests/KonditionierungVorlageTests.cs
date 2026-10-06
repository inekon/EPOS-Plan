using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// <b>Die Konditionierung in der Auslieferungsvorlage</b> (Konzept Konditionierungsprofile 5.5
    /// und 5.7, E54): Der Prüfbericht zählt Vorlagen, Kalender, Vorgaben und Perioden je
    /// Eigentümerart, und die Katalogbereinigung <c>--kataloge readonly</c> räumt <b>eigene</b>
    /// Vorlagen samt Kalendern, Perioden und Vorgaben über die Kaskade des Fremdschlüssels aus
    /// Schritt <see cref="KonditionierungVorlagenSchema.SCHRITT"/>.
    ///
    /// <para>Quelle ist eine KOPIE der Testdatenbank; die Probe legt darin ihren eigenen Bestand an
    /// — zwei Vorlagen (eine gesperrt, eine eigen), einen gesperrten Katalogbau mit Kalender und ein
    /// Projektgebäude samt Zone mit Kalendern. Die Datei im Repository wird nie geöffnet.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class KonditionierungVorlageTests
    {
        private const string VORLAGE_GESPERRT = "Probe gesperrt";
        private const string VORLAGE_EIGEN = "Probe eigen";
        private const double WOCHENENDWERT = 16.0;

        [Fact]
        public void Der_Vorlagenlauf_raeumt_eigene_Vorlagen_und_zaehlt_je_Eigentuemerart()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            long gesperrt = 0, eigen = 0, katalogbau = 0, gebaeude = 0, zone = 0;

            Bearbeiten(quelle, () =>
            {
                if (!KonditionierungVorlagenSchema.Lesbar())
                    throw new InvalidOperationException(
                        "Die Testdatenbank steht vor Schritt " + KonditionierungVorlagenSchema.SCHRITT + ".");

                // Zwei Vorlagen: eine gehoert zur Auslieferung, eine dem Anwender.
                gesperrt = VorlageAnlegen(VORLAGE_GESPERRT, readOnly: 1);
                eigen = VorlageAnlegen(VORLAGE_EIGEN, readOnly: 0);
                InhaltAnlegen("ID_Vorlage", gesperrt);
                InhaltAnlegen("ID_Vorlage", eigen);

                // Ein GESPERRTER Katalogbau - er ueberlebt --kataloge readonly und traegt
                // Katalogzeilen, die bleiben muessen.
                katalogbau = Eine("SELECT MIN(\"ID\") FROM \"" + Matrixzellenort.TAB_KATALOGBAU +
                                  "\" WHERE \"ReadOnly\" = 1");
                Assert.True(katalogbau > 0, "Die Testdatenbank führt keinen gesperrten Katalogbau.");
                InhaltAnlegen("ID_Gebaeude_Stamm", katalogbau);

                // Ein Projektgebaeude samt Zone - beides faellt in der Projektbereinigung.
                gebaeude = Eine("SELECT MIN(\"ID\") FROM \"" + Matrixzellenort.TAB_GEBAEUDE + "\"");
                Assert.True(gebaeude > 0);
                InhaltAnlegen("ID_Gebaeude", gebaeude);

                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO \"" + Matrixzellenort.TAB_ZONE +
                    "\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\") VALUES (?, 1, 'Probezone', 1)",
                    new DbParam("@g", gebaeude)));
                zone = Eine("SELECT MAX(\"ID\") FROM \"" + Matrixzellenort.TAB_ZONE + "\"");
                ZonenInhaltAnlegen(gebaeude, zone);
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly",
                                                           "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);

            // ---- Der Bericht zaehlt je Groesse und je Eigentuemerart ----
            // Gesperrt sind die Probevorlage UND die ausgelieferten der Saat (Schritt 156): drei in
            // der Heizliste, 14 in allen fuenf.
            int saatHeizen = KonditionierungsvorlagenSaattabelle.Alle.Count(s => s.Groesse == Konditionierungsgroesse.Heizsoll);
            Assert.Contains("Konditionierung", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Vorlagen " + DbWerte.KOND_GROESSE_HEIZSOLL.PadRight(10) +
                            " gesperrt " + (1 + saatHeizen) + ", eigen 0", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Vorlagen gesamt: gesperrt " + (1 + KonditionierungsvorlagenSaattabelle.VORLAGEN) + ", eigen 0",
                            e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Kalender Vorlage", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Kalender Katalogbau", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Kalender Gebaeude ohne Zone", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Kalender Zone", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("Vorgaben Vorlage", e.Ausgabe, StringComparison.Ordinal);

            // ---- Die fuenf Pruefungen stehen auf ok ----
            Assert.Contains("ok      ausgelieferte Vorlagen der Saat: 14 von 14 gesperrt", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("ok      Vorlageninhalt in fremder Groesse: 0", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("ok      Vorlagen mit Nennwert oder Saison (E54): 0", e.Ausgabe,
                            StringComparison.Ordinal);
            Assert.Contains("ok      Waisen in den drei Tabellen (foreign_key_check): 0", e.Ausgabe,
                            StringComparison.Ordinal);
            Assert.Contains("ok      eigene Vorlagen nach --kataloge readonly: 0", e.Ausgabe,
                            StringComparison.Ordinal);

            // ---- Und der Bestand der fertigen Vorlage ----
            Lesen(ziel, () =>
            {
                // Die gesperrte Vorlage bleibt samt Inhalt, die eigene ist ueber die Kaskade fort.
                Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" +
                                     KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"ID\" = ?",
                                     new DbParam("@id", gesperrt)));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" +
                                     KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"ID\" = ?",
                                     new DbParam("@id", eigen)));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" +
                                     KonditionierungVorlagenSchema.TAB_VORLAGE +
                                     "\" WHERE \"ReadOnly\" IS NULL OR \"ReadOnly\" = 0"));

                foreach (string tabelle in new[] { KonditionierungSchema.TAB_KALENDER,
                                                   KonditionierungSchema.TAB_VORGABE })
                {
                    Assert.True(Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"ID_Vorlage\" = ?",
                                     new DbParam("@v", gesperrt)) > 0,
                                tabelle + ": die gesperrte Vorlage hat ihren Inhalt verloren.");
                    Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"ID_Vorlage\" = ?",
                                         new DbParam("@v", eigen)));
                }

                // Die Perioden sind mit ihrem Kalender gefallen - keine Waise bleibt.
                Assert.True(Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE +
                                 "\" p JOIN \"" + KonditionierungSchema.TAB_KALENDER +
                                 "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Vorlage\" = ?",
                                 new DbParam("@v", gesperrt)) > 0);
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE +
                                     "\" p LEFT JOIN \"" + KonditionierungSchema.TAB_KALENDER +
                                     "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID\" IS NULL"));

                // Die Katalogzeilen des gesperrten Katalogbaus bleiben …
                Assert.True(Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                 "\" WHERE \"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", katalogbau)) > 0);
                Assert.True(Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                 "\" WHERE \"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", katalogbau)) > 0);

                // … und die Projektzeilen sind nach der Projektbereinigung fort.
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                     "\" WHERE \"ID_Gebaeude\" IS NOT NULL"));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                     "\" WHERE \"ID_Gebaeude\" IS NOT NULL"));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + Matrixzellenort.TAB_ZONE + "\""));
                Assert.True(gebaeude > 0 && zone > 0);
            });
        }

        [Fact]
        public void Der_Bericht_nennt_Inhalt_in_fremder_Groesse_und_Nennwert_oder_Saison()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Bearbeiten(quelle, () =>
            {
                if (!KonditionierungVorlagenSchema.Lesbar())
                    throw new InvalidOperationException("Schemastand zu alt.");

                // Eine GESPERRTE Vorlage - sie ueberlebt die Bereinigung und traegt den Verstoss.
                long id = VorlageAnlegen("Probe Verstoss", readOnly: 1);

                // (a) Inhalt in einer FREMDEN Groesse (die Vorlage gehoert HEIZSOLL).
                Vorgabe(id, DbWerte.KOND_GROESSE_KUEHLSOLL, DbWerte.KOND_ZEILE_TAG, 26.0);
                // (b) Eine Nennwert- und eine Saisonzeile (E54).
                Vorgabe(id, DbWerte.KOND_GROESSE_HEIZSOLL, DbWerte.KOND_ZEILE_NENNWERT, 20.0);
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                    "\" (\"ID_Vorlage\", \"Groesse\", \"Zeile\", \"Von\", \"Bis\", \"Aus\") " +
                    "VALUES (?, ?, ?, 100, 200, 0)",
                    new DbParam("@v", id),
                    new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL),
                    new DbParam("@ze", DbWerte.KOND_ZEILE_SAISON)));
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly",
                                                           "--katalogleerung-zulassen");

            // Ein Befund macht die Abnahme rot - genau dafuer ist der Pruefbericht da.
            Assert.NotEqual(0, e.Code);
            Assert.Contains("FEHLER  Vorlageninhalt in fremder Groesse: 1", e.Ausgabe, StringComparison.Ordinal);
            Assert.Contains("FEHLER  Vorlagen mit Nennwert oder Saison (E54): 2", e.Ausgabe,
                            StringComparison.Ordinal);
            // Die Saat selbst ist vollstaendig - der Befund liegt allein an der Probevorlage.
            Assert.Contains("ok      ausgelieferte Vorlagen der Saat: 14 von 14 gesperrt", e.Ausgabe, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Fehlt eine ausgelieferte Vorlage der Saat, ist der Prüfbericht rot</b> (KP-S1b, E56): Die Quelle
        /// verliert „Büro" in der Heizliste; der Lauf endet mit Fehler und nennt, was fehlt.
        /// </summary>
        [Fact]
        public void Fehlt_eine_ausgelieferte_Vorlage_ist_der_Pruefbericht_rot()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Bearbeiten(quelle, () =>
            {
                if (!KonditionierungVorlagenSchema.Lesbar())
                    throw new InvalidOperationException("Schemastand zu alt.");
                Assert.Equal(1, DataRepository.ExecuteNonQuery(
                    "DELETE FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"Groesse\" = ? AND \"Bezeichner\" = ?",
                    new DbParam("@g", DbWerte.KOND_GROESSE_HEIZSOLL),
                    new DbParam("@b", KonditionierungsvorlagenSaattabelle.BUERO)));
            });

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly",
                                                           "--katalogleerung-zulassen");

            Assert.NotEqual(0, e.Code);
            Assert.Contains("FEHLER  ausgelieferte Vorlagen der Saat: 13 von 14 gesperrt   (es fehlen " +
                            DbWerte.KOND_GROESSE_HEIZSOLL + "/" + KonditionierungsvorlagenSaattabelle.BUERO + ")",
                            e.Ausgabe, StringComparison.Ordinal);
        }

        // =============================================================================
        //  Vorrichtung
        // =============================================================================

        /// <summary>Legt eine Vorlage der Größe <c>HEIZSOLL</c> an und gibt ihre Id zurück.</summary>
        private static long VorlageAnlegen(string bezeichner, int readOnly)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" (\"Groesse\", \"Bezeichner\", \"ReadOnly\") VALUES (?, ?, ?)",
                new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL),
                new DbParam("@bz", bezeichner),
                new DbParam("@ro", readOnly)));
            return Eine("SELECT MAX(\"ID\") FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\"");
        }

        /// <summary>
        /// Legt einem Eigentümer eine Vorgabezeile, einen Kalender und eine Periode an — dieselbe
        /// Form für alle vier Eigentümerarten, nur die Eigentümerspalte wechselt.
        /// </summary>
        private static void InhaltAnlegen(string spalte, long id)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                "\" (\"" + spalte + "\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\") VALUES (?, ?, ?, ?, 0)",
                new DbParam("@id", id),
                new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL),
                new DbParam("@ze", DbWerte.KOND_ZEILE_WOCHENENDE),
                new DbParam("@we", WOCHENENDWERT)));

            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER +
                "\" (\"" + spalte + "\", \"Groesse\", \"Wert\", \"Aus\") VALUES (?, ?, 20.0, 0)",
                new DbParam("@id", id),
                new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL)));
            PeriodeAnlegen(Eine("SELECT MAX(\"ID\") FROM \"" + KonditionierungSchema.TAB_KALENDER + "\""));
        }

        /// <summary>Dasselbe für eine Zone — sie führt Gebäude UND Zone (Eigentümerregel 5.1).</summary>
        private static void ZonenInhaltAnlegen(long idGebaeude, long idZone)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                "\" (\"ID_Gebaeude\", \"ID_Zone\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\") " +
                "VALUES (?, ?, ?, ?, ?, 0)",
                new DbParam("@g", idGebaeude), new DbParam("@z", idZone),
                new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL),
                new DbParam("@ze", DbWerte.KOND_ZEILE_WOCHENENDE),
                new DbParam("@we", WOCHENENDWERT)));

            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER +
                "\" (\"ID_Gebaeude\", \"ID_Zone\", \"Groesse\", \"Wert\", \"Aus\") VALUES (?, ?, ?, 20.0, 0)",
                new DbParam("@g", idGebaeude), new DbParam("@z", idZone),
                new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL)));
            PeriodeAnlegen(Eine("SELECT MAX(\"ID\") FROM \"" + KonditionierungSchema.TAB_KALENDER + "\""));
        }

        private static void PeriodeAnlegen(long idKalender)
            => Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE +
                "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", \"Ende\", \"Wert\", \"Aus\") " +
                "VALUES (?, ?, ?, 'Probe', 100, 110, 18.0, 0)",
                new DbParam("@k", idKalender),
                new DbParam("@r", Standardfahrplan.RANG_EIGEN),
                new DbParam("@a", DbWerte.KOND_ART_ZEITRAUM)));

        private static void Vorgabe(long idVorlage, string groesse, string zeile, double wert)
            => Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                "\" (\"ID_Vorlage\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\") VALUES (?, ?, ?, ?, 0)",
                new DbParam("@v", idVorlage),
                new DbParam("@gr", groesse),
                new DbParam("@ze", zeile),
                new DbParam("@we", wert)));

        private static long Eine(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static long Zahl(string sql, params DbParam[] p) => Eine(sql, p);

        private static void Bearbeiten(string datei, Action aktion)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (Konditionierungsprobe vorbereiten)");
                aktion();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }

        private static void Lesen(string datei, Action aktion)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                aktion();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }
    }
}
