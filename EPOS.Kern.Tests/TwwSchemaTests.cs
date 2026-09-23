using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die DDL des Zapfprofilgenerators (<see cref="TwwSchema"/>, Schemaschritt T1,
    /// Umsetzungskonzept Zapfprofilgenerator 3.1/3.2) gegen eine leere Datenbank im
    /// Speicher: zehn Tabellen, STRICT, wiederholbar, Beziehungen über IDs, und die
    /// Prüfungen greifen. Alle Werte sind erfunden (Konzept Kapitel 6 (a)) — die Fälle
    /// prüfen Struktur, nie eine Normzahl.
    /// </summary>
    public sealed class TwwSchemaTests
    {
        /// <summary>Spaltenzahl je Tabelle nach Konzept 3.1.</summary>
        private static readonly Dictionary<string, int> Spaltenzahl = new()
        {
            [TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM] = 6,
            [TwwSchema.TAB_TWW_TAGESGANG_STAMM] = 31,
            [TwwSchema.TAB_TWW_NUTZUNGSART_STAMM] = 55,
            [TwwSchema.TAB_TWW_BEDARFSTAG_STAMM] = 12,
            [TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM] = 6,
            [TwwSchema.TAB_TWW_PARAMETER_STAMM] = 12,
            [TwwSchema.TAB_TWW_DIN4708_WERT_STAMM] = 12,
            [TwwSchema.TAB_TWW_ZONE] = 45,
            [TwwSchema.TAB_TWW_WOHNUNGSTYP] = 7,
            [TwwSchema.TAB_TWW_PROJEKT] = 40,
        };

        [Fact]
        public void Zehn_Tabellen_entstehen_STRICT_und_wiederholbar()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);
            Anlegen(c);   // IF NOT EXISTS - der zweite Lauf tut nichts

            Assert.Equal(10, TwwSchema.Anweisungen.Count());
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
            {
                string sql = Skalar(c, "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $n",
                                    ("$n", a.Key)) as string;
                Assert.NotNull(sql);
                Assert.EndsWith(") STRICT", sql, StringComparison.Ordinal);
                Assert.Equal(Spaltenzahl[a.Key], Spalten(c, a.Key).Count);
                Assert.StartsWith("Tab_Tww", a.Key, StringComparison.Ordinal);
            }
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));

            // Vier Indizes, je auf einer Fremdschluesselspalte ihrer Tabelle.
            Assert.Equal(4, TwwSchema.Indizes.Count());
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes)
            {
                string tabelle = Skalar(c, "SELECT tbl_name FROM sqlite_master WHERE type = 'index' AND name = $n",
                                        ("$n", i.Key)) as string;
                Assert.NotNull(tabelle);
                List<object[]> spalten = Zeilen(c, "SELECT name FROM pragma_index_info($i)", ("$i", i.Key));
                Assert.Single(spalten);
                Assert.Contains(Zeilen(c, "SELECT \"from\" FROM pragma_foreign_key_list($t)", ("$t", tabelle)),
                                fk => (string)fk[0] == (string)spalten[0][0]);
            }
        }

        [Fact]
        public void Eine_geloeschte_ID_wird_nie_wieder_vergeben()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);

            // Jede Tabelle zaehlt ihre ID fort (Muster WechselrichterSchema).
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                Assert.Contains("\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,", a.Value, StringComparison.Ordinal);

            // Die hoechste Katalogzeile geht - die naechste bekommt trotzdem eine neue ID.
            const string neu = "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\") VALUES ($b, 'V1', 'EIGEN')";
            Ausfuehren(c, neu, ("$b", "A"));
            Ausfuehren(c, neu, ("$b", "B"));
            Assert.Equal(2L, Skalar(c, "SELECT MAX(\"ID\") FROM \"Tab_TwwTagesgangsatz_STAMM\""));
            Ausfuehren(c, "DELETE FROM \"Tab_TwwTagesgangsatz_STAMM\" WHERE \"ID\" = 2");
            Ausfuehren(c, neu, ("$b", "C"));
            Assert.Equal(3L, Skalar(c, "SELECT \"ID\" FROM \"Tab_TwwTagesgangsatz_STAMM\" WHERE \"Bezeichner\" = 'C'"));
        }

        /// <summary>
        /// Jede Spalte mit CHECK samt einem Wert außerhalb ihrer Wertemenge; einige Spalten
        /// mit zwei Werten (unter und über dem Bereich). Die Liste muss jede CHECK-Spalte der
        /// DDL führen — eine neue Prüfung ohne Probe fällt auf.
        /// </summary>
        private static readonly (string Tabelle, string Spalte, string Fremdwert)[] Fremdwerte =
        {
            (TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "Status", "'FREMD'"),
            (TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "ReadOnly", "2"),
            (TwwSchema.TAB_TWW_TAGESGANG_STAMM, "Tagtyp", "5"),
            (TwwSchema.TAB_TWW_TAGESGANG_STAMM, "Tagtyp", "0"),
            (TwwSchema.TAB_TWW_TAGESGANG_STAMM, "Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bezugsart", "8"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bezugsart", "0"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bedarf_Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bilanzgrenze", "4"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Kalenderart", "6"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Jahresgang_Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Wochengang_Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Status", "'FREMD'"),
            (TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "ReadOnly", "2"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Quelle_Art", "1"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Quelle_Art", "6"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Status", "'FREMD'"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "ReadOnly", "2"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, "Minute_Beginn", "-1"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, "Minute_Beginn", "1440"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, "Dauer_min", "0"),
            (TwwSchema.TAB_TWW_PARAMETER_STAMM, "Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_PARAMETER_STAMM, "Status", "'FREMD'"),
            (TwwSchema.TAB_TWW_PARAMETER_STAMM, "ReadOnly", "2"),
            (TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, "Art", "'FREMD'"),
            (TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, "Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, "Status", "'FREMD'"),
            (TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, "ReadOnly", "2"),
            (TwwSchema.TAB_TWW_ZONE, "Bezugsmenge", "0.0"),
            (TwwSchema.TAB_TWW_ZONE, "Bezugsmenge", "-1.0"),
            (TwwSchema.TAB_TWW_ZONE, "Niveau", "4"),
            (TwwSchema.TAB_TWW_ZONE, "Topologie", "5"),
            (TwwSchema.TAB_TWW_ZONE, "Zirkulation", "2"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienbeginn_1", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienbeginn_1", "-1"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienende_1", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienbeginn_2", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienende_2", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienbeginn_3", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienende_3", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienbeginn_4", "367"),
            (TwwSchema.TAB_TWW_ZONE, "Ferienende_4", "-1"),
            (TwwSchema.TAB_TWW_ZONE, "Jahresmesswert_Einheit", "3"),
            (TwwSchema.TAB_TWW_ZONE, "Jahresmesswert_Bilanzgrenze", "4"),
            (TwwSchema.TAB_TWW_ZONE, "Tagesbedarf_Auto", "2"),
            (TwwSchema.TAB_TWW_WOHNUNGSTYP, "Anzahl", "0"),
            (TwwSchema.TAB_TWW_PROJEKT, "Weg", "'ANDERS'"),
            (TwwSchema.TAB_TWW_PROJEKT, "Jahresreihe_Stochastisch", "2"),
            (TwwSchema.TAB_TWW_PROJEKT, "Realisierungen", "0"),
            (TwwSchema.TAB_TWW_PROJEKT, "Realisierungen_Auslegung", "0"),
            (TwwSchema.TAB_TWW_PROJEKT, "Perzentil", "50"),
            (TwwSchema.TAB_TWW_PROJEKT, "Zirk_Auto", "2"),
            (TwwSchema.TAB_TWW_PROJEKT, "Zirk_Methode", "4"),
            (TwwSchema.TAB_TWW_PROJEKT, "Zirk_Lage", "3"),
            (TwwSchema.TAB_TWW_PROJEKT, "Lade_Auto", "2"),
            (TwwSchema.TAB_TWW_PROJEKT, "Speicherart", "3"),
            (TwwSchema.TAB_TWW_PROJEKT, "Bedarfstag_Quelle", "6"),
        };

        [Fact]
        public void Jede_Pruefung_weist_ihren_Fremdwert_ab()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);
            Vollbelegen(c);

            // Die Liste deckt jede CHECK-Spalte der DDL.
            var geprueft = new HashSet<string>(Fremdwerte.Select(f => f.Tabelle + "." + f.Spalte), StringComparer.Ordinal);
            var fehlend = new List<string>();
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                foreach (System.Text.RegularExpressions.Match m in
                         System.Text.RegularExpressions.Regex.Matches(a.Value, "CHECK \\(\"([A-Za-z0-9_]+)\""))
                    if (!geprueft.Contains(a.Key + "." + m.Groups[1].Value)) fehlend.Add(a.Key + "." + m.Groups[1].Value);
            Assert.True(fehlend.Count == 0, "CHECK ohne Probe: " + string.Join(", ", fehlend));

            foreach (var f in Fremdwerte)
            {
                Assert.Contains(f.Spalte, Spalten(c, f.Tabelle));
                string sql = "UPDATE \"" + f.Tabelle + "\" SET \"" + f.Spalte + "\" = " + f.Fremdwert;
                Assert.True(Wirft(c, sql), f.Tabelle + "." + f.Spalte + " nimmt " + f.Fremdwert + " an.");
            }

            // Die Ereignisse: auch NULL ist ausgeschlossen, das ein CHECK allein durchliesse.
            foreach (string spalte in new[] { "Minute_Beginn", "Dauer_min", "Reihenfolge" })
                Assert.True(Wirft(c, "UPDATE \"Tab_TwwBedarfstagEreignis_STAMM\" SET \"" + spalte + "\" = NULL"), spalte + " nimmt NULL an.");

            // Gegenprobe: die Grenzen selbst und NULL bei einer Ueberschreibung gehen durch.
            Ausfuehren(c, "UPDATE \"Tab_TwwZone\" SET \"Ferienbeginn_1\" = 0, \"Ferienende_1\" = 366, \"Ferienbeginn_2\" = NULL");
            Ausfuehren(c, "UPDATE \"Tab_TwwBedarfstagEreignis_STAMM\" SET \"Minute_Beginn\" = 1439");
            Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"Realisierungen\" = 1, \"Realisierungen_Auslegung\" = NULL");
        }

        [Fact]
        public void Benutzte_Kataloge_sind_gesperrt_und_der_Bedarfstag_loest_sich()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);
            Vollbelegen(c);

            // Ein Tagesgangsatz, den nur eine Zone waehlt, ist ebenso gesperrt wie der der Nutzungsart.
            Ausfuehren(c, "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\") VALUES ('Zweiter', 'V1', 'EIGEN')");
            Ausfuehren(c, "UPDATE \"Tab_TwwZone\" SET \"ID_Tagesgangsatz\" = 2");
            Assert.True(Wirft(c, "DELETE FROM \"Tab_TwwTagesgangsatz_STAMM\" WHERE \"ID\" = 2"));
            Assert.True(Wirft(c, "DELETE FROM \"Tab_TwwTagesgangsatz_STAMM\" WHERE \"ID\" = 1"));

            // Ein DIN-4708-Wert, auf den ein Wohnungstyp zeigt, bleibt.
            Assert.True(Wirft(c, "DELETE FROM \"Tab_TwwDin4708Wert_STAMM\""));

            // Der Bedarfstag geht: seine Ereignisse mit ihm, das Projekt verliert nur die Bindung.
            Assert.Equal(1L, Skalar(c, "SELECT \"ID_Bedarfstag\" FROM \"Tab_TwwProjekt\""));
            Ausfuehren(c, "DELETE FROM \"Tab_TwwBedarfstag_STAMM\"");
            Assert.Null(Skalar(c, "SELECT \"ID_Bedarfstag\" FROM \"Tab_TwwProjekt\""));
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwProjekt\""));
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwBedarfstagEreignis_STAMM\""));

            // Ohne Wohnungstyp und Zone gehen DIN-4708-Wert und der zweite Satz.
            Ausfuehren(c, "DELETE FROM \"Tab_TwwZone\"");
            Ausfuehren(c, "DELETE FROM \"Tab_TwwDin4708Wert_STAMM\"");
            Ausfuehren(c, "DELETE FROM \"Tab_TwwTagesgangsatz_STAMM\" WHERE \"ID\" = 2");
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        [Fact]
        public void Jede_Beziehung_zeigt_auf_die_ID_einer_bekannten_Tabelle()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);

            var ziele = new HashSet<string>(TwwSchema.Anweisungen.Select(a => a.Key)) { "Tab_Projekt", "Tab_Gebaeude" };
            int beziehungen = 0;
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
            {
                foreach (object[] fk in Zeilen(c, "SELECT \"table\", \"from\", \"to\" FROM pragma_foreign_key_list($t)", ("$t", a.Key)))
                {
                    beziehungen++;
                    Assert.Contains((string)fk[0], ziele);
                    Assert.Equal("ID", (string)fk[2]);
                    Assert.StartsWith("ID_", (string)fk[1], StringComparison.Ordinal);
                }
            }
            // Tagesgang 1, Nutzungsart 2, Ereignis 1, Zone 4, Wohnungstyp 2, Projekt 2
            Assert.Equal(12, beziehungen);
        }

        [Fact]
        public void Die_Pruefungen_weisen_ungueltige_Werte_ab()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);
            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");

            // Status, ReadOnly und Herkunftsart haben feste Wertemengen.
            Assert.Throws<SqliteException>(() => Ausfuehren(c,
                "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\") VALUES ('A', 'V1', 'FREMD')"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c,
                "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\", \"ReadOnly\") VALUES ('A', 'V1', 'EIGEN', 2)"));
            Ausfuehren(c, "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\") VALUES ('A', 'V1', 'EIGEN')");
            // Bezeichner und Katalogversion sind der natuerliche Schluessel.
            Assert.Throws<SqliteException>(() => Ausfuehren(c,
                "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\") VALUES ('A', 'V1', 'EIGEN')"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c,
                "INSERT INTO \"Tab_TwwParameter_STAMM\" (\"Schluessel\", \"Wert\", \"Katalogversion\", \"Quelle\", \"Version\", \"Herkunftsart\", \"Status\") " +
                "VALUES ('Probe.a', 1.0, 'V1', 'Testkatalog (fiktiv)', 'V1', 'GESCHAETZT', 'EIGEN')"));

            // Die Weiche: Vorgabe BESTAND, eine Zeile je Projekt, nur die zwei Wege.
            Ausfuehren(c, "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (1)");
            Assert.Equal(TwwSchema.WEG_BESTAND, Skalar(c, "SELECT \"Weg\" FROM \"Tab_TwwProjekt\" WHERE \"ID_Projekt\" = 1"));
            Assert.Null(Skalar(c, "SELECT \"Realisierungen_Auslegung\" FROM \"Tab_TwwProjekt\" WHERE \"ID_Projekt\" = 1"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (1)"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"Weg\" = 'ANDERS'"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"Perzentil\" = 50"));

            // Ereignis: Minute im Tag, Dauer mindestens 1.
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM));
            Assert.Throws<SqliteException>(() => Ausfuehren(c,
                "INSERT INTO \"Tab_TwwBedarfstagEreignis_STAMM\" (\"ID_Bedarfstag\", \"Minute_Beginn\", \"Dauer_min\", \"Energie_Kwh\", \"Reihenfolge\") VALUES (1, 1440, 1, 1.0, 1)"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c,
                "INSERT INTO \"Tab_TwwBedarfstagEreignis_STAMM\" (\"ID_Bedarfstag\", \"Minute_Beginn\", \"Dauer_min\", \"Energie_Kwh\", \"Reihenfolge\") VALUES (1, 0, 0, 1.0, 1)"));
        }

        [Fact]
        public void Loeschen_folgt_den_Beziehungen()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);
            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");
            Ausfuehren(c, "INSERT INTO \"Tab_Gebaeude\" (\"ID\") VALUES (1)");

            // Eine Kette mit erfundenen Werten: Satz -> Tagesgang, Nutzungsart -> Zone -> Wohnungstyp.
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_TAGESGANG_STAMM));
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_DIN4708_WERT_STAMM));
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_ZONE));
            Ausfuehren(c, "UPDATE \"Tab_TwwZone\" SET \"ID_Gebaeude\" = 1");
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_WOHNUNGSTYP));
            Ausfuehren(c, "UPDATE \"Tab_TwwWohnungstyp\" SET \"ID_Ausstattung\" = 1");
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));

            // Eine benutzte Nutzungsart laesst sich nicht loeschen.
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "DELETE FROM \"Tab_TwwNutzungsart_STAMM\""));

            // Das Gebaeude geht, die Zone bleibt - nur ohne Bindung.
            Ausfuehren(c, "DELETE FROM \"Tab_Gebaeude\"");
            Assert.Null(Skalar(c, "SELECT \"ID_Gebaeude\" FROM \"Tab_TwwZone\""));

            // Das Projekt nimmt Zonen und Wohnungstypen mit; der Katalog bleibt.
            Ausfuehren(c, "DELETE FROM \"Tab_Projekt\"");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwZone\""));
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwWohnungstyp\""));
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwNutzungsart_STAMM\""));

            // Unbenutzt geht die Nutzungsart; der Satz nimmt dann seine Tagesgaenge mit.
            Ausfuehren(c, "DELETE FROM \"Tab_TwwNutzungsart_STAMM\"");
            Ausfuehren(c, "DELETE FROM \"Tab_TwwTagesgangsatz_STAMM\"");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwTagesgang_STAMM\""));
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>Eine leere Datenbank im Speicher mit eingeschalteten Fremdschlüsseln und zwei Elterntabellen als Stummel.</summary>
        private static SqliteConnection Datenbank()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE \"Tab_Projekt\" (\"ID\" INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_Gebaeude\" (\"ID\" INTEGER PRIMARY KEY) STRICT");
            return c;
        }

        private static void Anlegen(SqliteConnection c)
        {
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen) Ausfuehren(c, a.Value);
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes) Ausfuehren(c, i.Value);
        }

        /// <summary>
        /// Je Tabelle eine gültige Zeile mit erfundenen Werten, alle mit ID 1 und verkettet:
        /// Satz → Tagesgang, Nutzungsart → Zone → Wohnungstyp (Ausstattung → DIN-4708-Wert),
        /// Bedarfstag → Ereignis und Projekt.
        /// </summary>
        private static void Vollbelegen(SqliteConnection c)
        {
            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen) Ausfuehren(c, Einfuegen(c, a.Key));
            Ausfuehren(c, "UPDATE \"Tab_TwwWohnungstyp\" SET \"ID_Ausstattung\" = 1");
            Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"ID_Bedarfstag\" = 1");
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        /// <summary>Ob die Anweisung mit einer <see cref="SqliteException"/> scheitert.</summary>
        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }

        /// <summary>
        /// Ein <c>INSERT</c> mit einer Zeile, die jede Pflichtspalte ohne Vorgabe mit einem
        /// erfundenen, gültigen Wert belegt: Fremdschlüssel 1, Aufzählungen 1, Zahlen 1,0,
        /// Texte „Probe" bzw. der erste Wert ihrer Wertemenge.
        /// </summary>
        private static string Einfuegen(SqliteConnection c, string tabelle)
        {
            var spalten = new List<string>();
            var werte = new List<string>();
            foreach (object[] s in Zeilen(c, "SELECT name, type, \"notnull\", dflt_value, pk FROM pragma_table_info($t)", ("$t", tabelle)))
            {
                string name = (string)s[0];
                bool pflicht = Convert.ToInt64(s[2]) == 1 && s[3] == null && Convert.ToInt64(s[4]) == 0;
                if (!pflicht) continue;
                spalten.Add("\"" + name + "\"");
                werte.Add((string)s[1] switch
                {
                    "INTEGER" => name == "Quelle_Art" ? "2" : "1",   // Wertemenge 2..5
                    "REAL" => "1.0",
                    _ => name == "Status" ? "'" + TwwSchema.STATUS_EIGEN + "'"
                       : name.EndsWith("Herkunftsart", StringComparison.Ordinal) ? "'" + TwwSchema.HERKUNFT_FIKTIV + "'"
                       : name == "Art" ? "'" + TwwSchema.DIN4708_ART_AUSSTATTUNG + "'"
                       : "'Probe'",
                });
            }
            return "INSERT INTO \"" + tabelle + "\" (" + string.Join(", ", spalten) + ") VALUES (" + string.Join(", ", werte) + ")";
        }

        private static List<string> Spalten(SqliteConnection c, string tabelle)
            => Zeilen(c, "SELECT name FROM pragma_table_info($t)", ("$t", tabelle)).Select(z => (string)z[0]).ToList();

        private static void Ausfuehren(SqliteConnection c, string sql, params (string Name, object Wert)[] parameter)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            foreach (var p in parameter) k.Parameters.AddWithValue(p.Name, p.Wert);
            k.ExecuteNonQuery();
        }

        private static object Skalar(SqliteConnection c, string sql, params (string Name, object Wert)[] parameter)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            foreach (var p in parameter) k.Parameters.AddWithValue(p.Name, p.Wert);
            object r = k.ExecuteScalar();
            return r is DBNull ? null : r;
        }

        private static List<object[]> Zeilen(SqliteConnection c, string sql, params (string Name, object Wert)[] parameter)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            foreach (var p in parameter) k.Parameters.AddWithValue(p.Name, p.Wert);
            var liste = new List<object[]>();
            using SqliteDataReader r = k.ExecuteReader();
            while (r.Read())
            {
                var z = new object[r.FieldCount];
                for (int i = 0; i < r.FieldCount; i++) z[i] = r.IsDBNull(i) ? null : r.GetValue(i);
                liste.Add(z);
            }
            return liste;
        }
    }
}
