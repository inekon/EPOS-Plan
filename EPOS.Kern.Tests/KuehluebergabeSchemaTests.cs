using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Schemaschritte der Kälteseite der Anlagenkopplung — <b>KAK-S1</b>, <b>KAK-S3</b> und
    /// der <b>Zonenschritt</b> (Entscheid E37; Konzept Anlagenkopplung 8.1 und 8.3; Nummern bei
    /// <see cref="KuehluebergabeSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Definitionen gegen das Papier (acht Spalten je
    /// Gebäudetabelle mit dem Schalter zuerst, drei plus fünf Ergebnisspalten, drei Zonenspalten —
    /// namentlich, mit Typ und Reihenfolge), der vierte Sichtneubau; der Stand der Testdatenbank
    /// (alles steht, alles leer bzw. 0, die Sicht ist die geltende, STRICT bleibt, die Prüfungen
    /// greifen); alle drei Schritte aus dem Stand davor, wiederholbar; <b>die vier fest
    /// verdrahteten Kopierwege</b> von <c>GebaeudeStammCtrl</c> samt Namensleser NULL-erhaltend
    /// und der Schalter nach A1 (die Art bleibt beim Abschalten); die Wege über die ganze Zeile
    /// (Katalog duplizieren, Projektduplikat, Projekttransfer); ein Lauf lässt die
    /// Ergebnisspalten leer; Repo-Datei, Werkzeug und Migration führen die Schritte.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehluebergabeSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt mit genau einem Gebäude.</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        /// <summary>Ein Referenzprojekt für den Lauf.</summary>
        private const int REFERENZ = 1030;

        /// <summary>Die acht Spalten von KAK-S1 in der Reihenfolge des Papiers (8.1).</summary>
        private static readonly string[] SPALTEN_KAK_S1 =
        {
            "Kuehluebergabe_Aktiv", "Kuehl_Uebergabe_Art", "Kuehl_Uebergabe_Exponent",
            "Kuehl_Uebergabe_Leistung_Nenn", "Kuehl_Auslegung_Vorlauf", "Kuehl_Auslegung_Ruecklauf",
            "Kuehl_Auslegung_Raumtemperatur", "Kuehl_Vorlaufgrenze",
        };

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Drei Schritte, drei aufeinander folgende Nummern hinter S-C; der Zielstand reicht bis zum letzten.</summary>
        [Fact]
        public void Die_Nummern_folgen_lueckenlos_hinter_S_C()
        {
            Assert.True(KuehluebergabeSchema.SCHRITT > ZonenSchema.SCHRITT);
            Assert.Equal(KuehluebergabeSchema.SCHRITT + 1, KuehluebergabeSchema.SCHRITT_ERGEBNIS);
            Assert.Equal(KuehluebergabeSchema.SCHRITT + 2, KuehluebergabeSchema.SCHRITT_ZONE);
            Assert.True(SchemaStand.Zielversion >= KuehluebergabeSchema.SCHRITT_ZONE,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KuehluebergabeSchema.SCHRITT_ZONE + ".");
        }

        /// <summary>
        /// KAK-S1: acht Spalten je Gebäudetabelle, der Schalter zuerst (A1) — 16 Einträge, mit den
        /// Typen des Papiers; übersetzt wird wie bei AK-S1 (Schalter NOT NULL DEFAULT 0, Art mit
        /// Längenprüfung, Zahlen nullbar).
        /// </summary>
        [Fact]
        public void KAK_S1_acht_Spalten_je_Gebaeudetabelle_mit_ihren_Typen()
        {
            Assert.Equal(SPALTEN_KAK_S1, GebaeudeSchema.KUEHLUEBERGABE_SPALTEN.Select(s => s.Key));
            Assert.Equal(16, GebaeudeSchema.Kuehluebergabespalten.Length);
            Assert.Equal(GebaeudeSchema.TABELLEN.SelectMany(t => SPALTEN_KAK_S1.Select(s => t + "." + s)),
                         GebaeudeSchema.Kuehluebergabespalten.Select(s => s.Tabelle + "." + s.Name));
            Assert.Equal("YESNO", GebaeudeSchema.KUEHLUEBERGABE_SPALTEN[0].Value);
            Assert.Equal("TEXT(20)", GebaeudeSchema.KUEHLUEBERGABE_SPALTEN[1].Value);
            Assert.All(GebaeudeSchema.KUEHLUEBERGABE_SPALTEN.Skip(2), s => Assert.Equal("DOUBLE", s.Value));
            Assert.Equal(new[] { "Kuehluebergabe_Aktiv" }, GebaeudeSchema.KUEHLUEBERGABE_SCHALTER);

            // Wie Heizkreis_Aktiv (A1): INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1)).
            Assert.Equal(StilleDb.SqliteSpaltenTyp("Heizkreis_Aktiv", "YESNO").Replace("Heizkreis_Aktiv", "X"),
                         StilleDb.SqliteSpaltenTyp("Kuehluebergabe_Aktiv", "YESNO").Replace("Kuehluebergabe_Aktiv", "X"));
            Assert.StartsWith("INTEGER NOT NULL DEFAULT 0 CHECK", StilleDb.SqliteSpaltenTyp("Kuehluebergabe_Aktiv", "YESNO"),
                              StringComparison.Ordinal);
            // Keine Spalte gleicht einer der Heizseite oder des Bestands.
            Assert.Empty(SPALTEN_KAK_S1.Intersect(GebaeudeSchema.SICHT_UEBERGABE));
        }

        /// <summary>
        /// Der vierte Sichtneubau hängt die acht Spalten HINTER die 90 von AK-S1 (Stellen 90..97);
        /// die Bauvorschrift ist dieselbe (<see cref="GebaeudeSchema.SichtSql"/>), und sie ist die
        /// GELTENDE Sicht.
        /// </summary>
        [Fact]
        public void KAK_S1_haengt_die_acht_Spalten_hinter_AK_S1_an_die_Sicht()
        {
            Assert.Equal(98, GebaeudeSchema.SICHT_KUEHLUEBERGABE.Length);
            Assert.Equal(GebaeudeSchema.SICHT_UEBERGABE, GebaeudeSchema.SICHT_KUEHLUEBERGABE.Take(90));
            Assert.Equal(SPALTEN_KAK_S1, GebaeudeSchema.SICHT_KUEHLUEBERGABE.Skip(90));
            Assert.Equal(GebaeudeSchema.SichtSql(GebaeudeSchema.NEUE_SPALTEN.Select(s => s.Key)
                                                 .Concat(GebaeudeSchema.KUEHL_SPALTEN.Select(s => s.Key))
                                                 .Concat(GebaeudeSchema.UEBERGABE_SPALTEN.Select(s => s.Key))
                                                 .Concat(SPALTEN_KAK_S1)),
                         GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE);
            foreach (string s in SPALTEN_KAK_S1)
            {
                Assert.Contains("Tab_Gebaeude." + s, GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE, StringComparison.Ordinal);
                Assert.DoesNotContain("Tab_Gebaeude." + s, GebaeudeSchema.SQL_VIEW_UEBERGABE, StringComparison.Ordinal);
            }
            // Die GELTENDE Sicht ist die des fünften Durchgangs (Baujahr); sie beginnt mit den 98
            // Spalten von KAK-S1 an ihren Stellen.
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUEBERGABE, GebaeudeSchema.SICHT_AKTUELL.Take(98));
        }

        /// <summary>
        /// KAK-S3: drei nullbare Spalten an <c>Tab_ErgebnisEnergiebedarf</c> (Muster 123) und fünf an
        /// <c>Tab_ErgebnisGebaeude</c> (Muster 128) — die Art mit den drei Arten ohne IDEAL, die
        /// Stunden 0 … 8 760. Die Komfortzahlen kommen mit AK2.
        /// </summary>
        [Fact]
        public void KAK_S3_drei_plus_fuenf_Ergebnisspalten()
        {
            Assert.Equal(new[] { "Kuehl_Vorlauf_Mittel", "Kuehl_Ruecklauf_Mittel", "Kuehl_Uebergabe_Begrenzt_Stunden" },
                         KuehluebergabeSchema.Ergebnisspalten.Select(s => s.Name));
            Assert.All(KuehluebergabeSchema.Ergebnisspalten, s =>
            {
                Assert.Equal(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF, s.Tabelle);
                Assert.Equal("DOUBLE", s.TypDefinition);
            });
            Assert.Empty(KuehluebergabeSchema.Ergebnisspalten.Select(s => s.Name)
                             .Intersect(AnlagenkopplungSchema.Ergebnisspalten.Select(s => s.Name)));

            Assert.Equal(new[] { "Kuehl_Uebergabe_Art", "KuehlVorlaufMittel_C", "KuehlRuecklaufMittel_C",
                                 "KuehlUebergabeBegrenzt_H", "KuehlVorlaufgrenze_H" },
                         KuehluebergabeSchema.SpaltenKuehlkreis.Select(s => s.Key));
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_HEIZKREIS + KuehluebergabeSchema.SpaltenKuehlkreis.Count,
                         ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_KUEHLKREIS);

            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenKuehlkreis)
                Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + s.Key + "\" " + s.Value);
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach (string art in new[] { DbWerte.KUEHLUEBERGABE_KUEHLDECKE, DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG,
                                           DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR })
                Assert.False(Wirft(c, "UPDATE T SET Kuehl_Uebergabe_Art = '" + art + "'"), art);
            Assert.True(Wirft(c, "UPDATE T SET Kuehl_Uebergabe_Art = 'IDEAL'"), "IDEAL heißt im Ergebnis NULL");
            Assert.True(Wirft(c, "UPDATE T SET Kuehl_Uebergabe_Art = 'FLAECHE'"));
            Assert.True(Wirft(c, "UPDATE T SET KuehlUebergabeBegrenzt_H = 8761"));
            Assert.True(Wirft(c, "UPDATE T SET KuehlVorlaufgrenze_H = -1"));
            Assert.True(Wirft(c, "UPDATE T SET KuehlVorlaufMittel_C = 'kalt'"));
            Assert.False(Wirft(c, "UPDATE T SET KuehlUebergabeBegrenzt_H = 12.5, KuehlVorlaufgrenze_H = 3.25"));
        }

        /// <summary>
        /// Zone: drei Spalten, ohne Schalter wie die Heizseite; die Art mit Wertliste EINSCHLIESSLICH
        /// IDEAL (Muster <see cref="ZonenSchema.WERTE_UEBERGABE_ART"/>), Exponent und Nennleistung REAL.
        /// </summary>
        [Fact]
        public void Zone_drei_Spalten_ohne_Schalter()
        {
            Assert.Equal(new[] { "Kuehl_Uebergabe_Art", "Kuehl_Uebergabe_Exponent", "Kuehl_Uebergabe_Leistung_Nenn" },
                         KuehluebergabeSchema.SpaltenZone.Select(s => s.Key));
            Assert.Contains("'IDEAL'", KuehluebergabeSchema.WERTE_KUEHLUEBERGABE_ART_ZONE, StringComparison.Ordinal);
            Assert.DoesNotContain("'IDEAL'", KuehluebergabeSchema.WERTE_KUEHLUEBERGABE_ART, StringComparison.Ordinal);
            Assert.Equal("REAL", KuehluebergabeSchema.SpaltenZone[1].Value);
            Assert.Equal("REAL", KuehluebergabeSchema.SpaltenZone[2].Value);
            Assert.DoesNotContain(KuehluebergabeSchema.SpaltenZone, s => s.Key == "Kuehluebergabe_Aktiv");
            // ZonenSchema selbst bleibt unberührt: Seine Liste kennt die Spalten nicht.
            Assert.Empty(ZonenSchema.Zonenspalten.Intersect(KuehluebergabeSchema.SpaltenZone.Select(s => s.Key)));
        }

        /// <summary>Die Persistenzwerte sind ASCII und passen in <c>TEXT(20)</c>; FLAECHE der Heizseite wird nicht wiederverwendet.</summary>
        [Fact]
        public void Die_Persistenzwerte_sind_ASCII_und_passen_in_ihre_Spalte()
        {
            foreach (string w in new[] { DbWerte.KUEHLUEBERGABE_IDEAL, DbWerte.KUEHLUEBERGABE_KUEHLDECKE,
                                         DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR })
            {
                Assert.True(w.All(ch => ch < 128 && (char.IsUpper(ch) || ch == '_')), w);
                Assert.True(w.Length <= 20, w);
            }
            Assert.NotEqual(DbWerte.UEBERGABE_FLAECHE, DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und die Schritte aus dem Stand davor
        // =============================================================================

        /// <summary>
        /// Alle Strukturen stehen, die Sicht ist die geltende, alle neuen Spalten sind leer (der
        /// Schalter 0) — die Testdatenbank trägt keine gesäten Daten der Kühlübergabe. Die
        /// angefassten Tabellen bleiben STRICT, wo sie es waren.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_und_alle_neuen_Spalten_sind_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KuehluebergabeSchema.SCHRITT_ZONE);
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());
            Assert.True(KuehluebergabeSchema.ErgebnisVollstaendig());
            Assert.True(KuehluebergabeSchema.ZoneVollstaendig());
            Assert.True(AnlagenkopplungSchema.UebergabeVollstaendig());

            string sicht = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture);
            // Die Sicht ist die GELTENDE (die des Baujahrs); die 98 Spalten von KAK-S1 stehen darin
            // an ihren Stellen.
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, sicht);
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUEBERGABE, GebaeudeSchema.SichtSpalten().Take(98));

            foreach (SchemaSpalte s in GebaeudeSchema.Kuehluebergabespalten.Concat(KuehluebergabeSchema.Ergebnisspalten))
            {
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL AND [" +
                                      s.Name + "] <> 0"));
                if (!GebaeudeSchema.KUEHLUEBERGABE_SCHALTER.Contains(s.Name))
                    Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL"));
            }
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenKuehlkreis)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + ErgebnisGebaeudeSchema.TAB + "] WHERE [" + s.Key + "] IS NOT NULL"));
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenZone)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + ZonenSchema.TAB_ZONE + "] WHERE [" + s.Key + "] IS NOT NULL"));
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Gebaeude") > 0);

            foreach (string t in new[] { ErgebnisGebaeudeSchema.TAB, ZonenSchema.TAB_ZONE })
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// KAK-S1 aus dem Stand VOR ihm: Sicht und die 16 Spalten werden entfernt, die Sicht von
        /// AK-S1 steht; der Schritt legt die 16 Spalten an und baut die Sicht neu — die
        /// Bestandswerte bleiben, AK-S1 steht weiter, und ein zweiter Lauf legt nichts mehr an.
        /// </summary>
        [Fact]
        public void KAK_S1_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();

            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (SchemaSpalte s in GebaeudeSchema.Kuehluebergabespalten)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_UEBERGABE);    // der Stand nach AK-S1
            Assert.True(AnlagenkopplungSchema.UebergabeVollstaendig());
            Assert.False(KuehluebergabeSchema.GebaeudeVollstaendig());

            var bericht = new List<string>();
            Assert.Equal(16, KuehluebergabeSchema.GebaeudeAlle(bericht));
            Assert.Contains(bericht, z => z.Contains("16 von 16 Spalte(n) der Kuehluebergabe angelegt"));
            Assert.Contains(bericht, z => z.Contains("(98 Spalten)"));
            Assert.True(KuehluebergabeSchema.GebaeudeVollstaendig());
            Assert.True(AnlagenkopplungSchema.UebergabeVollstaendig());
            Assert.True(GebaeudeSchema.KuehlspaltenVollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Kuehluebergabe_Aktiv <> 0 OR Kuehl_Uebergabe_Art IS NOT NULL"));

            Assert.Equal(0, KuehluebergabeSchema.GebaeudeAlle(null));
            Assert.Equal(GebaeudeSchema.SICHT_KUEHLUEBERGABE, GebaeudeSchema.SichtSpalten());
        }

        /// <summary>KAK-S3 aus dem Stand VOR ihm, wiederholbar; kein DML.</summary>
        [Fact]
        public void KAK_S3_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            foreach (SchemaSpalte s in KuehluebergabeSchema.Ergebnisspalten)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenKuehlkreis)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + ErgebnisGebaeudeSchema.TAB + "\" DROP COLUMN \"" + s.Key + "\"");
            Assert.False(KuehluebergabeSchema.ErgebnisVollstaendig());
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_HEIZKREIS, DataRepository.SpaltenVonTabelle(ErgebnisGebaeudeSchema.TAB).Count);

            var bericht = new List<string>();
            Assert.Equal(8, KuehluebergabeSchema.ErgebnisAlle(bericht));
            Assert.Contains(bericht, z => z.StartsWith("8 von 8", StringComparison.Ordinal));
            Assert.True(KuehluebergabeSchema.ErgebnisVollstaendig());
            Assert.Equal(ErgebnisGebaeudeSchema.SPALTENZAHL_MIT_KUEHLKREIS, DataRepository.SpaltenVonTabelle(ErgebnisGebaeudeSchema.TAB).Count);
            Assert.Equal(0, KuehluebergabeSchema.ErgebnisAlle(null));
        }

        /// <summary>Der Zonenschritt aus dem Stand VOR ihm, wiederholbar; ohne <c>Tab_Zone</c> tut er nichts.</summary>
        [Fact]
        public void Zone_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenZone)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + ZonenSchema.TAB_ZONE + "\" DROP COLUMN \"" + s.Key + "\"");
            Assert.False(KuehluebergabeSchema.ZoneVollstaendig());
            Assert.True(ZonenSchema.Vollstaendig());

            Assert.Equal(3, KuehluebergabeSchema.ZoneAlle(null));
            Assert.True(KuehluebergabeSchema.ZoneVollstaendig());
            Assert.Equal(0, KuehluebergabeSchema.ZoneAlle(null));

            // Ohne Tabelle (Stand vor S-C) legt er nichts an. Die Tabellen des späteren Schritts S-F
            // (ImportzuordnungSchema) zeigen auf Zone und Bauteil und fallen deshalb zuerst - eine
            // Datei vor S-C trägt sie nicht.
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + ImportzuordnungSchema.TAB_ZUORDNUNG + "\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + ImportzuordnungSchema.TAB_QUELLE + "\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + ZonenSchema.TAB_BAUTEIL + "\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + ZonenSchema.TAB_ZONE + "\"");
            var bericht = new List<string>();
            Assert.Equal(0, KuehluebergabeSchema.ZoneAlle(bericht));
            Assert.Contains(bericht, z => z.Contains("fehlt"));
            Assert.False(KuehluebergabeSchema.ZoneVollstaendig());
        }

        /// <summary>
        /// Die Prüfungen der Spalten greifen: kein Schalter außer 0/1 und kein NULL, keine Art über
        /// 20 Zeichen, keine fremde Art an der Zone, IDEAL an der Zone erlaubt — und NULL geht durch.
        /// </summary>
        [Fact]
        public void Die_Pruefungen_der_Spalten_weisen_ungueltige_Werte_ab()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Kuehluebergabe_Aktiv = 2 WHERE ID = " + GEBAEUDE));
            Assert.True(Wirft("UPDATE Tab_Gebaeude_STAMM SET Kuehluebergabe_Aktiv = NULL WHERE ID = 1"));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Kuehl_Uebergabe_Art = ? WHERE ID = " + GEBAEUDE,
                              new DbParam("?", new string('X', 21))));
            Assert.True(Wirft("INSERT INTO Tab_ErgebnisEnergiebedarf (ID, Kuehl_Vorlauf_Mittel) VALUES (999999, 'kalt')"));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Kuehluebergabe_Aktiv = 1, Kuehl_Uebergabe_Art = ? WHERE ID = ?",
                                                  new DbParam("?", DbWerte.KUEHLUEBERGABE_KUEHLDECKE), new DbParam("?", GEBAEUDE)));

            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Zone (ID_Gebaeude, Rang, Bezeichner) VALUES (" + GEBAEUDE + ", 1, 'Zone 1')");
            Assert.True(Wirft("UPDATE Tab_Zone SET Kuehl_Uebergabe_Art = 'FLAECHE'"));
            Assert.True(Wirft("UPDATE Tab_Zone SET Kuehl_Uebergabe_Exponent = 'steil'"));
            foreach (string art in new[] { DbWerte.KUEHLUEBERGABE_IDEAL, DbWerte.KUEHLUEBERGABE_KUEHLDECKE,
                                           DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG, DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR })
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Zone SET Kuehl_Uebergabe_Art = ?", new DbParam("?", art)), art);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Zone SET Kuehl_Uebergabe_Art = NULL, Kuehl_Uebergabe_Exponent = 1.1, Kuehl_Uebergabe_Leistung_Nenn = 2.5"));
        }

        // =============================================================================
        //  Teil 3 - die vier fest verdrahteten Kopierwege von GebaeudeStammCtrl (8.6)
        // =============================================================================

        /// <summary>KOPIERWEG 1 UND 2 — <c>BuildValueParams</c> und <c>Insert</c>: NULL wird nicht 0, der Leser bringt NULL als <c>null</c>.</summary>
        [Fact]
        public void Kopierweg_1_und_2_BuildValueParams_und_Insert_halten_NULL()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "KAK-S1-Probe Insert";
            Assert.True(new GebaeudeStammCtrl().Insert(Teilbelegt(NAME)));
            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME));
            PruefeTeilbelegt(Lesen(NAME));
        }

        /// <summary>
        /// KOPIERWEG 3 — <c>Overwrite</c>: gesetzte Felder werden NULL, leere bekommen einen Wert;
        /// der Schalter geht aus, und die Art bleibt stehen (A1).
        /// </summary>
        [Fact]
        public void Kopierweg_3_Overwrite_haelt_NULL_und_die_Art_beim_Abschalten()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "KAK-S1-Probe Overwrite";
            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(Teilbelegt(NAME)));

            GebaeudeModel g = Lesen(NAME);
            g.Kuehluebergabe_Aktiv = false;
            g.Kuehl_Uebergabe_Exponent = 1.05;
            g.Kuehl_Auslegung_Vorlauf = null;
            g.Kuehl_Vorlaufgrenze = null;
            g.Kuehl_Auslegung_Raumtemperatur = 26;
            Assert.True(ctrl.Overwrite(g));

            DataRow r = Zeile("SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME);
            Assert.Equal(0L, Convert.ToInt64(r["Kuehluebergabe_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, Convert.ToString(r["Kuehl_Uebergabe_Art"], CultureInfo.InvariantCulture));
            Assert.Equal(1.05, Convert.ToDouble(r["Kuehl_Uebergabe_Exponent"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Kuehl_Uebergabe_Leistung_Nenn"]);
            Assert.Equal(DBNull.Value, r["Kuehl_Auslegung_Vorlauf"]);
            Assert.Equal(19.0, Convert.ToDouble(r["Kuehl_Auslegung_Ruecklauf"], CultureInfo.InvariantCulture));
            Assert.Equal(26.0, Convert.ToDouble(r["Kuehl_Auslegung_Raumtemperatur"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r["Kuehl_Vorlaufgrenze"]);

            GebaeudeModel wieder = Lesen(NAME);
            Assert.False(wieder.Kuehluebergabe_Aktiv);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, wieder.Kuehl_Uebergabe_Art);
            Assert.Null(wieder.Kuehl_Auslegung_Vorlauf);
            Assert.Null(wieder.Kuehl_Vorlaufgrenze);
        }

        /// <summary>KOPIERWEG 4 — <c>CopyFromStamm</c>: aus einem leeren Exponenten wird KEIN 0,0; Schalter und Werte kommen an.</summary>
        [Fact]
        public void Kopierweg_4_CopyFromStamm_haelt_NULL_und_traegt_gesetzte_Werte()
        {
            if (!_db.Vorhanden) return;

            DataRow stamm = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0];
            int idStamm = Convert.ToInt32(stamm["ID"], CultureInfo.InvariantCulture);
            string bezeichner = Convert.ToString(stamm["Bezeichner"], CultureInfo.InvariantCulture);
            SetzeTeilbelegt("Tab_Gebaeude_STAMM", idStamm);

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idNeu = new GebaeudeStammCtrl().CopyFromStamm(
                bezeichner, Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture),
                Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture));
            Assert.True(idNeu > 0);

            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID = ?", idNeu));
            Assert.Equal(idStamm, Convert.ToInt32(Zeile("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", idNeu)[0],
                                                  CultureInfo.InvariantCulture));

            var projekt = new GebaeudeCtrl();
            projekt.ReadAll("ID = " + idNeu.ToString(CultureInfo.InvariantCulture));
            PruefeTeilbelegt(Assert.Single(projekt.items));
        }

        /// <summary>DER NAMENSLESER DER SICHT: <c>ProjektGebaeudeCtrl</c> liefert die acht Felder NULL-erhaltend; die übrigen Gebäude bleiben leer bzw. aus.</summary>
        [Fact]
        public void Der_Namensleser_der_Sicht_liefert_die_Kuehluebergabe_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            DataRow g = DataRepository.GetDataTable(
                "SELECT g.ID, z.ID_Projekt FROM Tab_Gebaeude g INNER JOIN Z_ProjektGebaeude z " +
                "ON z.ID = g.ID_ProjektGebaeude ORDER BY g.ID LIMIT 1").Rows[0];
            int idGebaeude = Convert.ToInt32(g["ID"], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(g["ID_Projekt"], CultureInfo.InvariantCulture);
            SetzeTeilbelegt("Tab_Gebaeude", idGebaeude);

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            PruefeTeilbelegt(ctrl.items.Single(x => x.ID_Gebaeude == idGebaeude));
            foreach (ProjektGebaeudeModel andere in ctrl.items.Where(x => x.ID_Gebaeude != idGebaeude))
            {
                Assert.False(andere.Kuehluebergabe_Aktiv);
                Assert.Null(andere.Kuehl_Uebergabe_Art);
                Assert.Null(andere.Kuehl_Vorlaufgrenze);
            }
        }

        // =============================================================================
        //  Teil 4 - die Wege über die ganze Zeile
        // =============================================================================

        /// <summary>„Duplizieren…" der Gebäudeverwaltung kopiert jede Spalte — auch die acht, NULL bleibt NULL.</summary>
        [Fact]
        public void Katalog_Duplizieren_traegt_die_Kuehluebergabe()
        {
            if (!_db.Vorhanden) return;

            int idStamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            SetzeTeilbelegt("Tab_Gebaeude_STAMM", idStamm);

            Katalogkopie.Ergebnis e = GebaeudeStammCtrl.Duplizieren(idStamm, "KAK-S1-Probe Kopie");
            Assert.True(e.Ok, e.Meldung);
            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude_STAMM WHERE ID = ?", e.Id));
        }

        /// <summary>Das Projektduplikat trägt die acht Spalten mit (11.3).</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Kuehluebergabe()
        {
            if (!_db.Vorhanden) return;

            SetzeTeilbelegt("Tab_Gebaeude", GEBAEUDE);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " KAK-S1");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die acht Spalten NULL-erhaltend über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Kuehluebergabe()
        {
            if (!_db.Vorhanden) return;

            SetzeTeilbelegt("Tab_Gebaeude", GEBAEUDE);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-kaks1-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer KAK-S1", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeTeilbelegt(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Teil 5 - ein Lauf, Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>Ein Lauf auf der Testdatenbank lässt die drei Ergebnisspalten der Kälteseite leer.</summary>
        [Fact]
        public void Ein_Lauf_laesst_die_Ergebnisspalten_der_Kaelteseite_leer()
        {
            if (!_db.Vorhanden) return;

            var laeufer = new SimulationRunner();
            int kopf = laeufer.SimuliereUndSpeichere(REFERENZ, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            string lauf = kopf.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = " + lauf));
            foreach (SchemaSpalte s in KuehluebergabeSchema.Ergebnisspalten)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = " + lauf +
                                      " AND [" + s.Name + "] IS NOT NULL"));
        }

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Alle drei Wege führen die Schritte (Migration der Schale,
        /// Werkzeug <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests) aus derselben Quelle, der
        /// Sichtneubau steht in Werkzeug und Testkopie ZULETZT, und die REPO-Datei trägt die
        /// Schritte (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_die_Schritte_der_Kaelteseite()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            foreach (string aufruf in new[] { "KuehluebergabeSchema.GebaeudeAlle(", "KuehluebergabeSchema.ErgebnisAlle(",
                                              "KuehluebergabeSchema.ZoneAlle(" })
                Assert.Contains(aufruf, werkzeug);
            Assert.True(werkzeug.IndexOf("KuehluebergabeSchema.GebaeudeAlle(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("AnlagenkopplungSchema.UebergabeAlle(", StringComparison.Ordinal),
                        "Der vierte Sichtneubau steht im Werkzeug nicht hinter AK-S1.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KUEHLUEBERGABE = KuehluebergabeSchema.SCHRITT", migration);
            Assert.Contains("SCHRITT_KUEHLUEBERGABE_ERGEBNIS = KuehluebergabeSchema.SCHRITT_ERGEBNIS", migration);
            Assert.Contains("SCHRITT_KUEHLUEBERGABE_ZONE = KuehluebergabeSchema.SCHRITT_ZONE", migration);
            int ortZonen = migration.IndexOf("new Schritt(SCHRITT_ZONEN", StringComparison.Ordinal);
            int ort1 = migration.IndexOf("new Schritt(SCHRITT_KUEHLUEBERGABE,", StringComparison.Ordinal);
            int ort3 = migration.IndexOf("new Schritt(SCHRITT_KUEHLUEBERGABE_ERGEBNIS", StringComparison.Ordinal);
            int ortZ = migration.IndexOf("new Schritt(SCHRITT_KUEHLUEBERGABE_ZONE", StringComparison.Ordinal);
            Assert.True(ortZonen > 0 && ort1 > ortZonen && ort3 > ort1 && ortZ > ort3, "Die Schritte stehen nicht nach S-C in ihrer Folge.");
            Assert.Contains("GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE", migration);
            Assert.Contains("KuehluebergabeSchema.SpaltenKuehlkreis", migration);
            Assert.Contains("KuehluebergabeSchema.SpaltenZone", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vGebaeude = vorrichtung.IndexOf("KuehluebergabeSchema.GebaeudeAlle(null)", StringComparison.Ordinal);
            Assert.True(vGebaeude > vorrichtung.IndexOf("AnlagenkopplungSchema.UebergabeAlle(null)", StringComparison.Ordinal),
                        "Der vierte Sichtneubau steht in der Testkopie nicht hinter AK-S1.");
            Assert.Contains("KuehluebergabeSchema.ErgebnisAlle(null)", vorrichtung);
            Assert.Contains("KuehluebergabeSchema.ZoneAlle(null)", vorrichtung);

            string export = File.ReadAllText(Path.Combine(wurzel, "Referenzlauf", "Ergebnisexport.cs"));
            Assert.Contains("KuehluebergabeSchema.Ergebnisspalten", export);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= KuehluebergabeSchema.SCHRITT_ZONE);
            foreach (SchemaSpalte s in GebaeudeSchema.Kuehluebergabespalten.Concat(KuehluebergabeSchema.Ergebnisspalten))
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + s.Tabelle + "') WHERE name = '" +
                                                  s.Name + "'"));
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenKuehlkreis)
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + ErgebnisGebaeudeSchema.TAB +
                                                  "') WHERE name = '" + s.Key + "'"));
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenZone)
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + ZonenSchema.TAB_ZONE +
                                                  "') WHERE name = '" + s.Key + "'"));
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = '" + GebaeudeSchema.VIEW + "'";
                Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Die Belegung der Proben: Kühlübergabe an, Kühldecke, Auslegung 16/19 °C, Vorlaufgrenze
        /// 17 °C — Exponent, Nennleistung und Raumtemperatur bleiben leer. Runde, erfundene Werte.
        /// </summary>
        private static GebaeudeModel Teilbelegt(string name) => new GebaeudeModel
        {
            Gebaeudename = name,
            Nutzflaeche = 100,
            Kuehluebergabe_Aktiv = true,
            Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE,
            Kuehl_Auslegung_Vorlauf = 16,
            Kuehl_Auslegung_Ruecklauf = 19,
            Kuehl_Vorlaufgrenze = 17,
        };

        /// <summary>Dieselbe Belegung per SQL in eine Tabellenzeile (Katalog oder Projekt).</summary>
        private static void SetzeTeilbelegt(string tabelle, int id)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE [" + tabelle + "] SET Kuehluebergabe_Aktiv = 1, Kuehl_Uebergabe_Art = ?, Kuehl_Uebergabe_Exponent = NULL, " +
                "Kuehl_Uebergabe_Leistung_Nenn = NULL, Kuehl_Auslegung_Vorlauf = ?, Kuehl_Auslegung_Ruecklauf = ?, " +
                "Kuehl_Auslegung_Raumtemperatur = NULL, Kuehl_Vorlaufgrenze = ? WHERE ID = ?",
                new DbParam("?", DbWerte.KUEHLUEBERGABE_KUEHLDECKE), new DbParam("?", 16.0), new DbParam("?", 19.0),
                new DbParam("?", 17.0), new DbParam("?", id)));
        }

        /// <summary>Die Tabellenzeile trägt die Belegung — NULL als DBNull, nicht als 0.</summary>
        private static void PruefeTeilbelegt(DataRow r)
        {
            Assert.Equal(1L, Convert.ToInt64(r["Kuehluebergabe_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, Convert.ToString(r["Kuehl_Uebergabe_Art"], CultureInfo.InvariantCulture));
            Assert.True(r["Kuehl_Uebergabe_Exponent"] == DBNull.Value, "Kuehl_Uebergabe_Exponent ist nicht NULL.");
            Assert.True(r["Kuehl_Uebergabe_Leistung_Nenn"] == DBNull.Value, "Kuehl_Uebergabe_Leistung_Nenn ist nicht NULL.");
            Assert.Equal(16.0, Convert.ToDouble(r["Kuehl_Auslegung_Vorlauf"], CultureInfo.InvariantCulture));
            Assert.Equal(19.0, Convert.ToDouble(r["Kuehl_Auslegung_Ruecklauf"], CultureInfo.InvariantCulture));
            Assert.True(r["Kuehl_Auslegung_Raumtemperatur"] == DBNull.Value, "Kuehl_Auslegung_Raumtemperatur ist nicht NULL.");
            Assert.Equal(17.0, Convert.ToDouble(r["Kuehl_Vorlaufgrenze"], CultureInfo.InvariantCulture));
        }

        /// <summary>Das Katalog- bzw. Projektmodell trägt die Belegung — NULL als <c>null</c>.</summary>
        private static void PruefeTeilbelegt(GebaeudeModel m)
        {
            Assert.True(m.Kuehluebergabe_Aktiv);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, m.Kuehl_Uebergabe_Art);
            Assert.Null(m.Kuehl_Uebergabe_Exponent);
            Assert.Null(m.Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Equal(16.0, m.Kuehl_Auslegung_Vorlauf);
            Assert.Equal(19.0, m.Kuehl_Auslegung_Ruecklauf);
            Assert.Null(m.Kuehl_Auslegung_Raumtemperatur);
            Assert.Equal(17.0, m.Kuehl_Vorlaufgrenze);
        }

        /// <summary>Das Modell des Namenslesers trägt die Belegung — NULL als <c>null</c>.</summary>
        private static void PruefeTeilbelegt(ProjektGebaeudeModel m)
        {
            Assert.True(m.Kuehluebergabe_Aktiv);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, m.Kuehl_Uebergabe_Art);
            Assert.Null(m.Kuehl_Uebergabe_Exponent);
            Assert.Null(m.Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Equal(16.0, m.Kuehl_Auslegung_Vorlauf);
            Assert.Equal(19.0, m.Kuehl_Auslegung_Ruecklauf);
            Assert.Null(m.Kuehl_Auslegung_Raumtemperatur);
            Assert.Equal(17.0, m.Kuehl_Vorlaufgrenze);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, object wert)
        {
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", wert));
            Assert.True(dt != null && dt.Rows.Count == 1, "Keine eindeutige Zeile: " + sql);
            return dt.Rows[0];
        }

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        /// <summary>Scheitert die Anweisung an einer Prüfung? Der Vorgang wird nie festgeschrieben.</summary>
        private static bool Wirft(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, parameter);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        /// <summary>Eine leere Datenbank im Speicher — für die Prüfungen der Definitionen ohne Testdatenbank.</summary>
        private static SqliteConnection Speicher()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            return c;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try
            {
                Ausfuehren(c, sql);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        /// <summary>Die Bestandswerte der angefassten Tabellen, an denen KAK-S1 nichts ändern darf.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT ID, Nutzflaeche, Kuehlung_Aktiv, Kuehl_Sollwert, Heizkreis_Aktiv, Uebergabe_Art FROM Tab_Gebaeude ORDER BY ID",
                         "SELECT ID, Nutzflaeche, Kuehlung_Aktiv, Kuehl_Sollwert, Heizkreis_Aktiv, Uebergabe_Art FROM Tab_Gebaeude_STAMM ORDER BY ID",
                     })
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                foreach (DataRow r in dt.Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
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
