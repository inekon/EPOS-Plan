using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die DDL des Zapfprofilgenerators (<see cref="TwwSchema"/>, Schemaschritte T1 und T2,
    /// Umsetzungskonzept Zapfprofilgenerator 3.1/3.2) gegen eine leere Datenbank im
    /// Speicher: zehn Tabellen aus T1 und die Zapfkategorien aus T2 (Schritt 115), STRICT,
    /// wiederholbar, Beziehungen über IDs, und die Prüfungen greifen. Alle Werte sind erfunden (Konzept Kapitel 6 (a)) — die Fälle
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
            [TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM] = 16,
            [TwwSchema.TAB_TWW_TYPTAG_IMPORT] = 11,
            [TwwSchema.TAB_TWW_MESSREIHE] = 10,
        };

        [Fact]
        public void Zehn_Tabellen_entstehen_STRICT_und_wiederholbar()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);
            Anlegen(c);   // IF NOT EXISTS - der zweite Lauf tut nichts

            Assert.Equal(10, TwwSchema.Anweisungen.Count());
            Assert.Equal(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, Assert.Single(TwwSchema.AnweisungenT2).Key);
            Assert.Equal(TwwSchema.TAB_TWW_TYPTAG_IMPORT, Assert.Single(TwwSchema.AnweisungenT3Typtage).Key);
            Assert.Equal(TwwSchema.TAB_TWW_MESSREIHE, Assert.Single(TwwSchema.AnweisungenT4Messreihen).Key);
            Assert.Equal(13, TwwSchema.AlleAnweisungen.Count());
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen)
            {
                string sql = Skalar(c, "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $n",
                                    ("$n", a.Key)) as string;
                Assert.NotNull(sql);
                Assert.EndsWith(") STRICT", sql, StringComparison.Ordinal);
                Assert.Equal(Spaltenzahl[a.Key], Spalten(c, a.Key).Count);
                Assert.StartsWith("Tab_Tww", a.Key, StringComparison.Ordinal);
            }
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));

            // Fuenf Indizes (vier aus T1, einer aus T4 "Messreihen"), je auf einer
            // Fremdschluesselspalte ihrer Tabelle.
            Assert.Equal(4, TwwSchema.Indizes.Count());
            Assert.Single(TwwSchema.IndizesT4Messreihen);
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes.Concat(TwwSchema.IndizesT4Messreihen))
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
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen)
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
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Volumenstrom_l_min", "-1.0"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Dauer_min", "0"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Dauer_min", "1441"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Anteil", "-0.5"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Sigma", "-1.0"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Kappung_l_min", "0.0"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Herkunftsart", "'GESCHAETZT'"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "Status", "'FREMD'"),
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, "ReadOnly", "2"),
            (TwwSchema.TAB_TWW_TYPTAG_IMPORT, "Art", "'FREMD'"),
            (TwwSchema.TAB_TWW_TYPTAG_IMPORT, "Klimazone", "-1"),
            (TwwSchema.TAB_TWW_TYPTAG_IMPORT, "Aufloesung_min", "0"),
            (TwwSchema.TAB_TWW_TYPTAG_IMPORT, "Aufloesung_min", "1441"),
            (TwwSchema.TAB_TWW_TYPTAG_IMPORT, "Zeilenindex", "-1"),
            (TwwSchema.TAB_TWW_MESSREIHE, "Groesse", "'FREMD'"),
            (TwwSchema.TAB_TWW_MESSREIHE, "Aufloesung_min", "0"),
            (TwwSchema.TAB_TWW_MESSREIHE, "Aufloesung_min", "1441"),
            (TwwSchema.TAB_TWW_MESSREIHE, "Zeilenindex", "-1"),
            (TwwSchema.TAB_TWW_MESSREIHE, "Wert", "-0.001"),
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
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen)
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
            Ausfuehren(c, "UPDATE \"Tab_TwwZapfkategorie_STAMM\" SET \"Dauer_min\" = 1440, \"Anteil\" = 0.0, \"Sigma\" = 0.0, \"Kappung_l_min\" = NULL");
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

            var ziele = new HashSet<string>(TwwSchema.AlleAnweisungen.Select(a => a.Key)) { "Tab_Projekt", "Tab_Gebaeude" };
            int beziehungen = 0;
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen)
            {
                foreach (object[] fk in Zeilen(c, "SELECT \"table\", \"from\", \"to\" FROM pragma_foreign_key_list($t)", ("$t", a.Key)))
                {
                    beziehungen++;
                    Assert.Contains((string)fk[0], ziele);
                    Assert.Equal("ID", (string)fk[2]);
                    Assert.StartsWith("ID_", (string)fk[1], StringComparison.Ordinal);
                }
            }
            // Tagesgang 1, Nutzungsart 2, Ereignis 1, Zone 4, Wohnungstyp 2, Projekt 2,
            // Zapfkategorie 1, Messreihe 1 (ID_Projekt, Schritt 135)
            Assert.Equal(14, beziehungen);
        }

        /// <summary>
        /// Der Schemaschritt T2 (Schritt 115) auf einer Datenbank mit Stand 114: Die zehn Tabellen
        /// aus T1 stehen samt einer Katalogkette, T2 legt die Zapfkategorien daneben — wiederholbar,
        /// ohne eine Zeile zu berühren. Eine Kategorie gehört genau einer Nutzungsart: kein Verweis
        /// ins Leere, kein Name doppelt je Nutzungsart, und sie geht mit ihrer Nutzungsart.
        /// </summary>
        [Fact]
        public void Schritt_T2_legt_die_Zapfkategorien_auf_Stand_114_an()
        {
            using SqliteConnection c = Datenbank();
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen) Ausfuehren(c, a.Value);
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes) Ausfuehren(c, i.Value);
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM));
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM));
            Assert.Null(Skalar(c, "SELECT name FROM sqlite_master WHERE name = $n", ("$n", TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM)));

            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT2) Ausfuehren(c, a.Value);
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT2) Ausfuehren(c, a.Value);   // IF NOT EXISTS
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwNutzungsart_STAMM\""));
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwZapfkategorie_STAMM\""));

            const string kategorie =
                "INSERT INTO \"Tab_TwwZapfkategorie_STAMM\" (\"ID_Nutzungsart\", \"Kategorie\", \"Reihenfolge\", " +
                "\"Volumenstrom_l_min\", \"Dauer_min\", \"Anteil\", \"Sigma\", \"Kappung_l_min\", \"Quelle\", " +
                "\"Version\", \"Herkunftsart\", \"Status\") VALUES ($n, $k, 1, 4.0, 2, 0.5, 1.0, $kap, " +
                "'Testkatalog (fiktiv)', 'V1', 'FIKTIV', 'EIGEN')";
            Ausfuehren(c, kategorie, ("$n", 1L), ("$k", "A"), ("$kap", 8.0));
            Ausfuehren(c, kategorie, ("$n", 1L), ("$k", "B"), ("$kap", DBNull.Value));
            Assert.Null(Skalar(c, "SELECT \"Kappung_l_min\" FROM \"Tab_TwwZapfkategorie_STAMM\" WHERE \"Kategorie\" = 'B'"));
            Assert.Equal(0L, Skalar(c, "SELECT \"ReadOnly\" FROM \"Tab_TwwZapfkategorie_STAMM\" WHERE \"Kategorie\" = 'A'"));

            // Natürlicher Schlüssel (Nutzungsart, Kategorie) und Verweis nur auf eine vorhandene Nutzungsart.
            Assert.True(Wirft(c, kategorie.Replace("$kap", "NULL"), ("$n", 1L), ("$k", "A")));
            Assert.True(Wirft(c, kategorie.Replace("$kap", "NULL"), ("$n", 99L), ("$k", "C")));

            // Die Nutzungsart nimmt ihre Kategorien mit.
            Ausfuehren(c, "DELETE FROM \"Tab_TwwNutzungsart_STAMM\"");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwZapfkategorie_STAMM\""));
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        /// <summary>
        /// Der Schritt 115 steht in der Migration der Schale NACH 114 (Kühlbetrieb am Erzeuger) und
        /// bedient sich derselben Quelle (<see cref="TwwSchema.AnweisungenT2"/>); das Ziel steht
        /// mindestens auf 115. Gelesen wird der Quelltext — die Schale ist kein Teil des Kern-Filters.
        /// </summary>
        [Fact]
        public void Schritt_115_steht_in_der_Migration_nach_114()
        {
            Assert.True(SchemaStand.Zielversion >= 115, "Zielstand " + SchemaStand.Zielversion + " liegt unter 115.");

            string datei = Migrationsquelle();
            if (datei == null) return;   // Quelle nicht im Baum: nichts zu pruefen
            string text = File.ReadAllText(datei);

            Assert.Contains("public const int SCHRITT_115_ZAPFKATEGORIEN = 115;", text, StringComparison.Ordinal);
            int ort114 = text.IndexOf("new Schritt(SCHRITT_114_KUEHLUNG_ERZEUGER", StringComparison.Ordinal);
            int ort115 = text.IndexOf("new Schritt(SCHRITT_115_ZAPFKATEGORIEN", StringComparison.Ordinal);
            Assert.True(ort114 > 0 && ort115 > ort114, "Schritt 115 steht nicht nach 114 in der Schrittliste.");

            int methode = text.IndexOf("private static bool Schritt_115_Zapfkategorien(Lauf l)", StringComparison.Ordinal);
            Assert.True(methode > 0, "Die Methode des Schrittes 115 fehlt.");
            int ende = text.IndexOf("return true;", methode, StringComparison.Ordinal);
            Assert.Contains("TwwSchema.AnweisungenT2", text.Substring(methode, ende - methode), StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Schemaschritt T3 (Schritt 124) auf einer Datenbank mit Stand 120: Die Tabellen aus T1
        /// stehen samt einer Projekt- und einer Bedarfstagzeile, T3 legt die sechs Spalten daneben —
        /// wiederholbar, ohne eine Zeile zu ändern: die vorhandene Projektzeile rechnet mit
        /// <c>Personen_Auto</c> = 1 und sonst NULL. Die CHECK-Klauseln kommen aus den Wertemengen
        /// von <see cref="TwwSchema"/>, derselben Quelle wie der Schreibweg.
        /// </summary>
        [Fact]
        public void Schritt_T3_legt_die_Laufangaben_und_die_Bezugsart_auf_Stand_120_an()
        {
            using SqliteConnection c = Datenbank();
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen) Ausfuehren(c, a.Value);
            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");
            Ausfuehren(c, "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (1)");
            Ausfuehren(c, Einfuegen(c, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM));
            int vorherProjekt = Spalten(c, TwwSchema.TAB_TWW_PROJEKT).Count;
            int vorherTag = Spalten(c, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM).Count;

            Assert.Equal(6, TwwSchema.SpaltenT3.Count);
            for (int lauf = 0; lauf < 2; lauf++)
                foreach (TwwSpalte s in TwwSchema.SpaltenT3)
                    if (!Spalten(c, s.Tabelle).Contains(s.Name)) Ausfuehren(c, TwwSchema.SpalteAnlegen(s));

            Assert.Equal(vorherProjekt + 5, Spalten(c, TwwSchema.TAB_TWW_PROJEKT).Count);
            Assert.Equal(vorherTag + 1, Spalten(c, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM).Count);
            Assert.Equal(1L, Skalar(c, "SELECT \"Personen_Auto\" FROM \"Tab_TwwProjekt\""));
            Assert.Null(Skalar(c, "SELECT \"Erzeugerart\" FROM \"Tab_TwwProjekt\""));
            Assert.Null(Skalar(c, "SELECT \"Fuellstand_Bezug\" FROM \"Tab_TwwProjekt\""));
            Assert.Null(Skalar(c, "SELECT \"Bezugsart\" FROM \"Tab_TwwBedarfstag_STAMM\""));

            // Die Wertemengen greifen — dieselben Zahlen wie im Schreibweg.
            Assert.Equal(new[] { 1, 2 }, TwwSchema.Werte(TwwSchema.ERZEUGERART_WERTE));
            Assert.Equal(new[] { 1, 2, 3, 4 }, TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE));
            Assert.Equal(Enum.GetValues(typeof(ZapfBezugsart)).Cast<int>().ToArray(), TwwSchema.Werte(TwwSchema.BEZUGSART_WERTE));
            Assert.Equal(Enum.GetValues(typeof(ZapfFuellstandbezug)).Cast<int>().ToArray(), TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE));
            Assert.Equal(Enum.GetValues(typeof(ZapfErzeugerart)).Cast<int>().ToArray(), TwwSchema.Werte(TwwSchema.ERZEUGERART_WERTE));
            Assert.Equal(Enum.GetValues(typeof(ZapfUebertragerwerkstoff)).Cast<int>().ToArray(), TwwSchema.Werte(TwwSchema.WERKSTOFF_WERTE));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Erzeugerart\" = 3"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Uebertrager_Werkstoff\" = 0"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Personen_Auto\" = 2"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Personen_Auto\" = NULL"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Personen_Manuell\" = -1"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Fuellstand_Bezug\" = 5"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwBedarfstag_STAMM\" SET \"Bezugsart\" = 8"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Personen_Manuell\" = 'viele'"));   // STRICT
            Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"Erzeugerart\" = 2, \"Uebertrager_Werkstoff\" = 1, " +
                          "\"Personen_Auto\" = 0, \"Personen_Manuell\" = 12.5, \"Fuellstand_Bezug\" = 4");
            Ausfuehren(c, "UPDATE \"Tab_TwwBedarfstag_STAMM\" SET \"Bezugsart\" = 2");
            Assert.Equal(12.5, Skalar(c, "SELECT \"Personen_Manuell\" FROM \"Tab_TwwProjekt\""));
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        /// <summary>
        /// Der Schritt 124 steht in der Migration der Schale NACH 123 (Anlagenkopplung AK-S3) und
        /// bedient sich derselben Quelle (<see cref="TwwSchema.SpaltenT3"/>); das Ziel steht auf
        /// mindestens 124.
        /// </summary>
        [Fact]
        public void Schritt_124_steht_in_der_Migration_nach_123()
        {
            Assert.True(SchemaStand.Zielversion >= 124, "Zielstand " + SchemaStand.Zielversion + " liegt unter 124.");

            string datei = Migrationsquelle();
            if (datei == null) return;
            string text = File.ReadAllText(datei);

            Assert.Contains("public const int SCHRITT_124_ZAPFPROFIL_LAUFANGABEN = 124;", text, StringComparison.Ordinal);
            int ort123 = text.IndexOf("new Schritt(SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS", StringComparison.Ordinal);
            int ort124 = text.IndexOf("new Schritt(SCHRITT_124_ZAPFPROFIL_LAUFANGABEN", StringComparison.Ordinal);
            Assert.True(ort123 > 0 && ort124 > ort123, "Schritt 124 steht nicht nach 123 in der Schrittliste.");

            int methode = text.IndexOf("private static bool Schritt_124_ZapfprofilLaufangaben(Lauf l)", StringComparison.Ordinal);
            Assert.True(methode > 0, "Die Methode des Schrittes 124 fehlt.");
            int ende = text.IndexOf("return true;", methode, StringComparison.Ordinal);
            Assert.Contains("TwwSchema.SpaltenT3", text.Substring(methode, ende - methode), StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Schritt 131 (T3 „Typtage", Stufe Z4b) steht in der Migration der Schale NACH dem
        /// letzten fremden Schritt (130, die Anschlusslängen im Gebäudekatalog)
        /// und bedient sich derselben Quelle (<see cref="TwwSchema.AnweisungenT3Typtage"/>); das
        /// Ziel steht auf mindestens 131.
        /// </summary>
        [Fact]
        public void Schritt_131_steht_in_der_Migration_nach_130()
        {
            Assert.True(SchemaStand.Zielversion >= 131, "Zielstand " + SchemaStand.Zielversion + " liegt unter 131.");

            string datei = Migrationsquelle();
            if (datei == null) return;
            string text = File.ReadAllText(datei);

            Assert.Contains("public const int SCHRITT_131_ZAPFPROFIL_TYPTAGE = 131;", text, StringComparison.Ordinal);
            int ort130 = text.IndexOf("new Schritt(SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN", StringComparison.Ordinal);
            int ort131 = text.IndexOf("new Schritt(SCHRITT_131_ZAPFPROFIL_TYPTAGE", StringComparison.Ordinal);
            Assert.True(ort130 > 0 && ort131 > ort130, "Schritt 131 steht nicht nach 130 in der Schrittliste.");

            int methode = text.IndexOf("private static bool Schritt_131_ZapfprofilTyptage(Lauf l)", StringComparison.Ordinal);
            Assert.True(methode > 0, "Die Methode des Schrittes 131 fehlt.");
            int ende = text.IndexOf("return true;", methode, StringComparison.Ordinal);
            string rumpf = text.Substring(methode, ende - methode);
            Assert.Contains("TwwSchema.AnweisungenT3Typtage", rumpf, StringComparison.Ordinal);
            // Die WAHL des Typtagwegs steht im SELBEN Schritt (Gruppe 2, N14 Folge (b)):
            // ein zweiter Schemaschritt fuer drei Spalten waere einer zu viel.
            Assert.Contains("TwwSchema.SpaltenT3Typtage", rumpf, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Die Wahl des Typtagwegs</b> (Schritt 131, Stufe Z4b, Gruppe 2) auf einer Datenbank mit
        /// Stand 124: Die drei Spalten entstehen an <c>Tab_TwwProjekt</c> neben der vorhandenen
        /// Projektzeile — wiederholbar, ohne eine Zeile zu ändern. Nach dem Schritt steht
        /// <c>Typtage_Aktiv</c> auf 0 und beide Angaben auf NULL: Das Projekt rechnet genau wie
        /// vorher über den Formvektor. Die CHECK-Klauseln greifen (0/1, Zone &gt; 0), und STRICT
        /// weist einen Text in der Zonenspalte ab.
        /// </summary>
        [Fact]
        public void Schritt_131_legt_die_Wahl_des_Typtagwegs_an()
        {
            using SqliteConnection c = Datenbank();
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen) Ausfuehren(c, a.Value);
            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");
            Ausfuehren(c, "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\") VALUES (1)");
            foreach (TwwSpalte s in TwwSchema.SpaltenT3) Ausfuehren(c, TwwSchema.SpalteAnlegen(s));
            int vorher = Spalten(c, TwwSchema.TAB_TWW_PROJEKT).Count;

            Assert.Equal(3, TwwSchema.SpaltenT3Typtage.Count);
            Assert.All(TwwSchema.SpaltenT3Typtage, s => Assert.Equal(TwwSchema.TAB_TWW_PROJEKT, s.Tabelle));
            for (int lauf = 0; lauf < 2; lauf++)
                foreach (TwwSpalte s in TwwSchema.SpaltenT3Typtage)
                    if (!Spalten(c, s.Tabelle).Contains(s.Name)) Ausfuehren(c, TwwSchema.SpalteAnlegen(s));

            Assert.Equal(vorher + 3, Spalten(c, TwwSchema.TAB_TWW_PROJEKT).Count);
            Assert.Equal(0L, Skalar(c, "SELECT \"Typtage_Aktiv\" FROM \"Tab_TwwProjekt\""));
            Assert.Null(Skalar(c, "SELECT \"Typtage_Klimazone\" FROM \"Tab_TwwProjekt\""));
            Assert.Null(Skalar(c, "SELECT \"Typtage_Gebaeudeart\" FROM \"Tab_TwwProjekt\""));

            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Typtage_Aktiv\" = 2"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Typtage_Aktiv\" = NULL"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Typtage_Klimazone\" = 0"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwProjekt\" SET \"Typtage_Klimazone\" = 'drei'"));   // STRICT
            Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"Typtage_Aktiv\" = 1, \"Typtage_Klimazone\" = 3, " +
                          "\"Typtage_Gebaeudeart\" = 'probehaus'");
            Assert.Equal(3L, Skalar(c, "SELECT \"Typtage_Klimazone\" FROM \"Tab_TwwProjekt\""));
            Assert.Equal("probehaus", Skalar(c, "SELECT \"Typtage_Gebaeudeart\" FROM \"Tab_TwwProjekt\""));
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        /// <summary>
        /// Der Schritt 135 (T4 „Messreihen", Stufe Z5) steht in der Migration der Schale NACH 131
        /// und bedient sich derselben Quelle (<see cref="TwwSchema.AnweisungenT4Messreihen"/>,
        /// <see cref="TwwSchema.IndizesT4Messreihen"/>); das Ziel steht auf mindestens 135.
        /// </summary>
        [Fact]
        public void Schritt_135_steht_in_der_Migration_nach_131()
        {
            Assert.True(SchemaStand.Zielversion >= 135, "Zielstand " + SchemaStand.Zielversion + " liegt unter 135.");

            string datei = Migrationsquelle();
            if (datei == null) return;
            string text = File.ReadAllText(datei);

            Assert.Contains("public const int SCHRITT_135_ZAPFPROFIL_MESSREIHEN = 135;", text, StringComparison.Ordinal);
            int ort131 = text.IndexOf("new Schritt(SCHRITT_131_ZAPFPROFIL_TYPTAGE", StringComparison.Ordinal);
            int ort135 = text.IndexOf("new Schritt(SCHRITT_135_ZAPFPROFIL_MESSREIHEN", StringComparison.Ordinal);
            Assert.True(ort131 > 0 && ort135 > ort131, "Schritt 135 steht nicht nach 131 in der Schrittliste.");

            int methode = text.IndexOf("private static bool Schritt_135_ZapfprofilMessreihen(Lauf l)", StringComparison.Ordinal);
            Assert.True(methode > 0, "Die Methode des Schrittes 135 fehlt.");
            int ende = text.IndexOf("return true;", methode, StringComparison.Ordinal);
            string rumpf = text.Substring(methode, ende - methode);
            Assert.Contains("TwwSchema.AnweisungenT4Messreihen", rumpf, StringComparison.Ordinal);
            // Der Index auf ID_Projekt steht im SELBEN Schritt - er ist der Suchweg jedes Zugriffs.
            Assert.Contains("TwwSchema.IndizesT4Messreihen", rumpf, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Die eingespielten Messreihen sind Bestandteil des Projekts</b> (Schritt 135, T4
        /// „Messreihen", Stufe Z5; Konzept Kapitel 9 K5): <c>Tab_TwwMessreihe</c> führt
        /// <c>ID_Projekt</c> mit <c>ON DELETE CASCADE</c> — damit reist sie mit einer Projektkopie
        /// und einem <c>.wpx</c>-Paket und verschwindet mit dem Projekt —, trägt aber weder
        /// <c>Status</c> noch <c>ReadOnly</c> noch eine Provenienzgruppe: Jede Zeile ist gemessen.
        /// Ihr natürlicher Schlüssel ist (ID_Projekt, Bezeichnung, Zeilenindex), und ein negativer
        /// Wert ist ausgeschlossen (eine Zapfung zählt nie rückwärts).
        /// </summary>
        [Fact]
        public void Die_eingespielten_Messreihen_gehoeren_dem_Projekt()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);

            List<string> spalten = Spalten(c, TwwSchema.TAB_TWW_MESSREIHE);
            Assert.Equal(new[] { "ID", "ID_Projekt", "Bezeichnung", "Groesse", "Aufloesung_min", "Beginn",
                                 "Zeilenindex", "Wert", "Quelle", "Datum_Import" }, spalten);
            foreach (string verboten in new[] { "Status", "ReadOnly", "Herkunftsart", "Version", "Katalogversion" })
                Assert.DoesNotContain(verboten, spalten);
            Assert.DoesNotContain("_STAMM", TwwSchema.TAB_TWW_MESSREIHE);

            // Der einzige Fremdschluessel zeigt auf Tab_Projekt und raeumt mit ihm auf.
            List<object[]> fk = Zeilen(c, "SELECT \"table\", \"from\", \"on_delete\" FROM pragma_foreign_key_list($t)",
                                       ("$t", TwwSchema.TAB_TWW_MESSREIHE));
            Assert.Single(fk);
            Assert.Equal("Tab_Projekt", (string)fk[0][0]);
            Assert.Equal("ID_Projekt", (string)fk[0][1]);
            Assert.Equal("CASCADE", (string)fk[0][2]);

            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");
            const string neu = "INSERT INTO \"Tab_TwwMessreihe\" (\"ID_Projekt\", \"Bezeichnung\", \"Groesse\", " +
                               "\"Aufloesung_min\", \"Beginn\", \"Zeilenindex\", \"Wert\", \"Quelle\", \"Datum_Import\") " +
                               "VALUES (1, 'Waermemengenzaehler (erfunden)', $g, 60, '2025-01-01T00:00', $i, 1.25, " +
                               "'Probe (erfunden)', '2026-09-25')";
            Ausfuehren(c, neu, ("$g", TwwSchema.MESSGROESSE_ENERGIE), ("$i", 0));
            Assert.True(Wirft(c, neu, ("$g", TwwSchema.MESSGROESSE_ENERGIE), ("$i", 0)));   // natuerlicher Schluessel
            Ausfuehren(c, neu, ("$g", TwwSchema.MESSGROESSE_ENERGIE), ("$i", 1));
            Assert.Equal(2L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwMessreihe\""));

            // Die drei Messgroessen der Konstanten sind genau die der Wertemenge.
            Assert.Equal(TwwSchema.Messgroessen, TwwSchema.MESSGROESSE_WERTE.Split(',').Select(s => s.Trim('\'')).ToList());
            Ausfuehren(c, "UPDATE \"Tab_TwwMessreihe\" SET \"Groesse\" = $g", ("$g", TwwSchema.MESSGROESSE_VOLUMEN));
            Ausfuehren(c, "UPDATE \"Tab_TwwMessreihe\" SET \"Groesse\" = $g", ("$g", TwwSchema.MESSGROESSE_LEISTUNG));

            // 0 geht (eine Stunde ohne Zapfung), ein negativer Wert nicht; STRICT weist Text ab.
            Ausfuehren(c, "UPDATE \"Tab_TwwMessreihe\" SET \"Wert\" = 0.0");
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwMessreihe\" SET \"Wert\" = -0.001"));
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwMessreihe\" SET \"Wert\" = 'viel'"));   // STRICT

            // Das Projekt raeumt seine Messreihen mit sich ab.
            Ausfuehren(c, "DELETE FROM \"Tab_Projekt\" WHERE \"ID\" = 1");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwMessreihe\""));
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        /// <summary>
        /// Die eingespielten Typtage (T3 „Typtage", Stufe Z4b) tragen weder <c>Status</c> noch
        /// <c>ReadOnly</c> noch eine Provenienzgruppe, führen kein <c>ID_Projekt</c> und keinen
        /// Fremdschlüssel — damit wandern sie weder in eine Projektkopie noch in ein
        /// <c>.wpx</c>-Paket (Konzept 3.1, Kapitel 6) —, und ihr natürlicher Schlüssel ist
        /// (Art, Klimazone, Gebaeudeart, Typtag, Zeilenindex).
        /// </summary>
        [Fact]
        public void Die_eingespielten_Typtage_sind_anwenderlokal()
        {
            using SqliteConnection c = Datenbank();
            Anlegen(c);

            List<string> spalten = Spalten(c, TwwSchema.TAB_TWW_TYPTAG_IMPORT);
            Assert.Equal(new[] { "ID", "Art", "Klimazone", "Gebaeudeart", "Typtag", "Aufloesung_min",
                                 "Zeilenindex", "Wert", "Quelle", "Ausgabe", "Datum_Import" }, spalten);
            foreach (string verboten in new[] { "Status", "ReadOnly", "ID_Projekt", "Herkunftsart", "Version", "Katalogversion" })
                Assert.DoesNotContain(verboten, spalten);
            Assert.Empty(Zeilen(c, "SELECT \"from\" FROM pragma_foreign_key_list($t)", ("$t", TwwSchema.TAB_TWW_TYPTAG_IMPORT)));
            Assert.DoesNotContain("_STAMM", TwwSchema.TAB_TWW_TYPTAG_IMPORT);

            // Der natuerliche Schluessel: zweimal dieselbe Zeile geht nicht, ein anderer Zeilenindex schon.
            const string neu = "INSERT INTO \"Tab_TwwTyptag_IMPORT\" (\"Art\", \"Klimazone\", \"Gebaeudeart\", " +
                               "\"Typtag\", \"Zeilenindex\", \"Wert\", \"Quelle\", \"Datum_Import\") " +
                               "VALUES ($a, 3, 'probehaus', 'PT1', $i, 0.25, 'Probe (erfunden)', '2026-09-24')";
            Ausfuehren(c, neu, ("$a", TwwSchema.TYPTAG_ART_GANG), ("$i", 0));
            Assert.True(Wirft(c, neu, ("$a", TwwSchema.TYPTAG_ART_GANG), ("$i", 0)));
            Ausfuehren(c, neu, ("$a", TwwSchema.TYPTAG_ART_GANG), ("$i", 1));
            Assert.Equal(2L, Skalar(c, "SELECT COUNT(*) FROM \"Tab_TwwTyptag_IMPORT\""));

            // Ein Faktor darf negativ sein (Schwankung um den Jahresmittelwert, Konzept 4.2).
            Ausfuehren(c, "INSERT INTO \"Tab_TwwTyptag_IMPORT\" (\"Art\", \"Klimazone\", \"Gebaeudeart\", " +
                          "\"Typtag\", \"Zeilenindex\", \"Wert\", \"Quelle\", \"Datum_Import\") " +
                          "VALUES ($a, 3, 'probehaus', 'PT1', 0, -0.0001, 'Probe (erfunden)', '2026-09-24')",
                      ("$a", TwwSchema.TYPTAG_ART_FAKTOR));

            // Die fuenf Satzarten der Konstanten sind genau die der Wertemenge.
            Assert.Equal(TwwSchema.TyptagArten, TwwSchema.TYPTAG_ART_WERTE.Split(',').Select(s => s.Trim('\'')).ToList());
            Assert.True(Wirft(c, "UPDATE \"Tab_TwwTyptag_IMPORT\" SET \"Wert\" = 'viel'"));   // STRICT
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
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen) Ausfuehren(c, a.Value);
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes.Concat(TwwSchema.IndizesT4Messreihen))
                Ausfuehren(c, i.Value);
        }

        /// <summary>
        /// Je Tabelle eine gültige Zeile mit erfundenen Werten, alle mit ID 1 und verkettet:
        /// Satz → Tagesgang, Nutzungsart → Zone → Wohnungstyp (Ausstattung → DIN-4708-Wert),
        /// Bedarfstag → Ereignis und Projekt.
        /// </summary>
        private static void Vollbelegen(SqliteConnection c)
        {
            Ausfuehren(c, "INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1)");
            foreach (KeyValuePair<string, string> a in TwwSchema.AlleAnweisungen) Ausfuehren(c, Einfuegen(c, a.Key));
            Ausfuehren(c, "UPDATE \"Tab_TwwWohnungstyp\" SET \"ID_Ausstattung\" = 1");
            Ausfuehren(c, "UPDATE \"Tab_TwwProjekt\" SET \"ID_Bedarfstag\" = 1");
            Assert.Empty(Zeilen(c, "PRAGMA foreign_key_check"));
        }

        /// <summary>Ob die Anweisung mit einer <see cref="SqliteException"/> scheitert.</summary>
        private static bool Wirft(SqliteConnection c, string sql, params (string Name, object Wert)[] parameter)
        {
            try { Ausfuehren(c, sql, parameter); return false; }
            catch (SqliteException) { return true; }
        }

        /// <summary><c>WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs</c>, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Migrationsquelle([CallerFilePath] string eigeneDatei = null)
        {
            var kandidaten = new List<string>();
            if (!string.IsNullOrEmpty(eigeneDatei)) kandidaten.Add(Path.GetDirectoryName(eigeneDatei));
            kandidaten.Add(AppContext.BaseDirectory);
            foreach (string start in kandidaten)
            {
                DirectoryInfo d = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                while (d != null)
                {
                    string p = Path.Combine(d.FullName, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs");
                    if (File.Exists(p)) return p;
                    d = d.Parent;
                }
            }
            return null;
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
                       : name == "Groesse" ? "'" + TwwSchema.MESSGROESSE_ENERGIE + "'"
                       : name == "Art" ? (tabelle == TwwSchema.TAB_TWW_TYPTAG_IMPORT
                                              ? "'" + TwwSchema.TYPTAG_ART_KENNWERT + "'"
                                              : "'" + TwwSchema.DIN4708_ART_AUSSTATTUNG + "'")
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
