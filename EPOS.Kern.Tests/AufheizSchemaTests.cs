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
    /// Die Schemaschritte der <b>Aufheizoptimierung</b> — KP-S2 (<see cref="AufheizvorgabeSchema"/>, fünf
    /// Projektspalten an <c>Tab_Einstellungen</c>) und KP-S3 (<see cref="AufheizErgebnisSchema"/>, je vierzehn
    /// Ergebnisspalten an <c>Tab_ErgebnisGebaeude</c> und <c>Tab_ErgebnisZone</c>); Entwurf KP3 Abschnitt 4.
    ///
    /// <para><b>Geprüft wird:</b> die Nummern (lückenlos hinter den Änderungsstempeln, ohne festen Pin des
    /// Zielstands) und beide Einträge im Register der Paketanhebung; die Definitionen — Namen, Reihenfolge,
    /// Spaltenzahlen 32 → 37, 26 → 40, 15 → 29 —, und an einer STRICT-Tabelle im Speicher: jede Prüfklausel
    /// weist einen ungültigen Wert ab, jede nimmt NULL (außer dem Schalter) und ihre Grenzwerte; der Stand
    /// der Testdatenbank (alles steht, Schalter 0, alles übrige leer, die Stempeltrigger unberührt); beide
    /// Schritte aus dem Stand davor, zweimal und je Spalte wiederholbar, ohne DML; Migration, Werkzeug und
    /// Testkopie führen sie aus derselben Quelle, und die Repo-Datei trägt sie.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die fünf Projektspalten in der Reihenfolge des Entwurfs.</summary>
        private static readonly string[] SPALTEN_VORGABE =
        {
            "Aufheizoptimierung", "Aufheiz_Bemessung", "Aufheiz_Abzug_K", "Aufheiz_Reserve", "Aufheiz_Art",
        };

        /// <summary>Die vierzehn Spalten der Gebäudetabelle in der Reihenfolge des Entwurfs.</summary>
        private static readonly string[] SPALTEN_GEBAEUDE =
        {
            "Aufheiz_Zustand", "Aufheiz_Bemessung", "Aufheizzeit_Max_H", "Aufheiz_Aussen_C", "Aufheiz_Leistung_Kw",
            "Aufheiz_Leistungsquelle", "Aufheiztage", "Aufheiztage_Begrenzt", "Aufheiztage_Unerreichbar",
            "Aufheiztage_Nachweisband", "Aufheizstunden_H", "Aufheizzeit_Laengste_H", "Aufheizspruenge_Aus",
            "HeizleistungMax_H",
        };

        /// <summary>Die vierzehn Spalten der Zonentabelle: ohne Bemessung, mit den Sommerlüftungsstunden.</summary>
        private static readonly string[] SPALTEN_ZONE =
        {
            "Aufheiz_Zustand", "Aufheizzeit_Max_H", "Aufheiz_Aussen_C", "Aufheiz_Leistung_Kw",
            "Aufheiz_Leistungsquelle", "Aufheiztage", "Aufheiztage_Begrenzt", "Aufheiztage_Unerreichbar",
            "Aufheiztage_Nachweisband", "Aufheizstunden_H", "Aufheizzeit_Laengste_H", "Aufheizspruenge_Aus",
            "HeizleistungMax_H", "Sommerlueftungsstunden_H",
        };

        // =============================================================================
        //  Teil 1 - Nummern, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// Die Nummern folgen lückenlos auf die Änderungsstempel (159): 160 die Projekteinstellung, 161 die
        /// Ergebnisspalten; der Zielstand reicht mindestens bis zu ihnen — ein späterer Schritt bricht den
        /// Test nicht (kein Pin auf den Zielstand).
        /// </summary>
        [Fact]
        public void Die_Nummern_folgen_lueckenlos_auf_die_Aenderungsstempel()
        {
            Assert.Equal(KostenStempelSchema.SCHRITT + 1, AufheizvorgabeSchema.SCHRITT);
            Assert.Equal(AufheizvorgabeSchema.SCHRITT + 1, AufheizErgebnisSchema.SCHRITT);
            Assert.Equal(160, AufheizvorgabeSchema.SCHRITT);
            Assert.Equal(161, AufheizErgebnisSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= AufheizErgebnisSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + AufheizErgebnisSchema.SCHRITT + ".");
        }

        /// <summary>
        /// Die Paketanhebung führt beide Stufen getrennt als reines DDL (Festlegung 23); ein Paket von den
        /// Stempeln (159) bis zum Zielstand braucht für sie keine Umformung.
        /// </summary>
        [Fact]
        public void Die_Paketanhebung_fuehrt_beide_Stufen_als_DDL()
        {
            foreach (int nr in new[] { AufheizvorgabeSchema.SCHRITT, AufheizErgebnisSchema.SCHRITT })
            {
                Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == nr);
                Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
                Assert.Null(s.Umformung);
                Assert.False(string.IsNullOrWhiteSpace(s.Text));
            }

            // Die Kette steht lückenlos bis zum Zielstand, aufsteigend und ohne Doppel.
            List<int> nummern = Paketanhebung.Stufen.Select(x => x.Nr).ToList();
            Assert.Equal(Enumerable.Range(Paketanhebung.UNTERE_GRENZE + 1,
                                          SchemaStand.Zielversion - Paketanhebung.UNTERE_GRENZE).ToList(), nummern);

            Paketanhebung.Vorschau v = Paketanhebung.Vorschauen(KostenStempelSchema.SCHRITT);
            Assert.True(v.Noetig);
            Assert.Equal(SchemaStand.Zielversion - KostenStempelSchema.SCHRITT, v.Schritte);
            Assert.Equal(Paketanhebung.Stufen.Count(x => x.Nr > KostenStempelSchema.SCHRITT &&
                                                         x.Wirkung == Paketanhebung.Art.Umformung), v.Umformungen);
        }

        /// <summary>Die Spalten tragen die Namen des Entwurfs in seiner Reihenfolge; die Spaltenzahlen stimmen (B24).</summary>
        [Fact]
        public void Die_Spalten_tragen_Namen_und_Reihenfolge_des_Entwurfs()
        {
            Assert.Equal("Tab_Einstellungen", AufheizvorgabeSchema.TABELLE);
            Assert.Equal(SPALTEN_VORGABE, AufheizvorgabeSchema.SPALTEN.Select(s => s.Key).ToArray());
            Assert.Equal(SPALTEN_GEBAEUDE, AufheizErgebnisSchema.SpaltenGebaeude.Select(s => s.Key).ToArray());
            Assert.Equal(SPALTEN_ZONE, AufheizErgebnisSchema.SpaltenZone.Select(s => s.Key).ToArray());
            Assert.Equal(new[] { "Tab_ErgebnisGebaeude", "Tab_ErgebnisZone" }, AufheizErgebnisSchema.TABELLEN);

            Assert.Equal(37, AufheizvorgabeSchema.SPALTENZAHL);
            Assert.Equal(40, AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE);
            Assert.Equal(29, AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_ZONE);

            // Die Vorgaben, für die NULL steht.
            Assert.Equal(2.0, AufheizvorgabeSchema.ABZUG_VORGABE_K);
            Assert.Equal(0.2, AufheizvorgabeSchema.RESERVE_VORGABE);
        }

        /// <summary>
        /// <b>Jede Prüfklausel der Projektspalten greift</b> — an einer STRICT-Tabelle im Speicher, mit
        /// genau der Typdefinition des Schritts: Ein Bestandssatz bekommt den Schalter 0 und sonst NULL;
        /// die Grenzwerte und NULL bestehen, jeder Wert außerhalb wird abgelehnt.
        /// </summary>
        [Fact]
        public void Jede_Pruefklausel_der_Projektspalten_greift()
        {
            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> s in AufheizvorgabeSchema.SPALTEN)
                Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + s.Key + "\" " + s.Value);

            Assert.Equal(0L, Skalar(c, "SELECT Aufheizoptimierung FROM T WHERE ID = 1"));
            foreach (string sp in SPALTEN_VORGABE.Skip(1))
                Assert.True(Skalar(c, "SELECT \"" + sp + "\" FROM T WHERE ID = 1") is DBNull, sp + " steht im Bestand auf NULL.");

            Pruefe(c, "Aufheizoptimierung", new[] { "0", "1" }, new[] { "2", "-1", "NULL", "'an'" });
            Pruefe(c, "Aufheiz_Bemessung", new[] { "'STUNDE'", "'STUNDE_ABZUG'", "NULL" }, new[] { "'stunde'", "'ABZUG'", "''" });
            Pruefe(c, "Aufheiz_Abzug_K", new[] { "0", "2", "2.5", "10", "NULL" }, new[] { "-0.1", "10.01", "'zwei'" });
            Pruefe(c, "Aufheiz_Reserve", new[] { "0.01", "0.2", "1", "NULL" }, new[] { "0", "-0.2", "1.01", "20" });
            Pruefe(c, "Aufheiz_Art", new[] { "'TAEGLICH'", "'FEST'", "NULL" }, new[] { "'taeglich'", "'WOECHENTLICH'", "''" });
        }

        /// <summary>
        /// <b>Jede Prüfklausel der Ergebnisspalten greift</b> — je Tabelle mit ihrer eigenen Wertliste:
        /// UNBEHEIZT nur an der Zone, GEKOPPELT und GEMISCHT nur am Gebäude; Zeiten 0 bis 47, Tage 0 bis 365,
        /// Stunden 0 bis 8 760, die Leistung über 0; jede Spalte nimmt NULL.
        /// </summary>
        [Fact]
        public void Jede_Pruefklausel_der_Ergebnisspalten_greift()
        {
            foreach (string tabelle in AufheizErgebnisSchema.TABELLEN)
            {
                bool zone = tabelle == AufheizErgebnisSchema.TAB_ZONE;
                using SqliteConnection c = Speicher();
                Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
                Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
                foreach (KeyValuePair<string, string> s in AufheizErgebnisSchema.Spalten(tabelle))
                    Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + s.Key + "\" " + s.Value);
                foreach (KeyValuePair<string, string> s in AufheizErgebnisSchema.Spalten(tabelle))
                    Assert.True(Skalar(c, "SELECT \"" + s.Key + "\" FROM T WHERE ID = 1") is DBNull,
                                tabelle + "." + s.Key + " steht im Bestand auf NULL.");

                Pruefe(c, "Aufheiz_Zustand",
                       zone ? new[] { "'BEMESSEN'", "'UNERREICHBAR'", "'UNBEHEIZT'", "NULL" }
                            : new[] { "'BEMESSEN'", "'UNERREICHBAR'", "'GEKOPPELT'", "NULL" },
                       zone ? new[] { "'GEKOPPELT'", "'bemessen'", "''" } : new[] { "'UNBEHEIZT'", "'bemessen'", "''" });
                Pruefe(c, "Aufheiz_Leistungsquelle",
                       zone ? new[] { "'GRENZE'", "'ZIEL'", "NULL" } : new[] { "'GRENZE'", "'ZIEL'", "'GEMISCHT'", "NULL" },
                       zone ? new[] { "'GEMISCHT'", "'grenze'" } : new[] { "'grenze'", "'ANDERE'" });
                foreach (string sp in new[] { "Aufheizzeit_Max_H", "Aufheizzeit_Laengste_H" })
                    Pruefe(c, sp, new[] { "0", "47", "NULL" }, new[] { "-1", "48", "1.5" });
                foreach (string sp in new[] { "Aufheiztage", "Aufheiztage_Begrenzt", "Aufheiztage_Unerreichbar", "Aufheiztage_Nachweisband" })
                    Pruefe(c, sp, new[] { "0", "365", "NULL" }, new[] { "-1", "366" });
                foreach (string sp in new[] { "Aufheizstunden_H", "Aufheizspruenge_Aus" })
                    Pruefe(c, sp, new[] { "0", "8760", "NULL" }, new[] { "-1", "8761" });
                Pruefe(c, "Aufheiz_Aussen_C", new[] { "-18.2", "0", "35", "NULL" }, new[] { "'kalt'" });
                Pruefe(c, "Aufheiz_Leistung_Kw", new[] { "0.001", "37.04", "NULL" }, new[] { "0", "-1" });
                Pruefe(c, "HeizleistungMax_H", new[] { "0", "12.25", "8760", "NULL" }, new[] { "-0.5", "8760.5" });
                if (zone)
                    Pruefe(c, "Sommerlueftungsstunden_H", new[] { "0", "8760", "NULL" }, new[] { "-1", "8761" });
                else
                    Pruefe(c, "Aufheiz_Bemessung", new[] { "'STUNDE'", "'STUNDE_ABZUG'", "NULL" }, new[] { "'X'" });
            }
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und die Schritte aus dem Stand davor
        // =============================================================================

        /// <summary>
        /// Die Testdatenbank steht auf dem Zielstand: alle Spalten mit Typ, Vorgabe und Reihenfolge, der
        /// Schalter überall 0, alles übrige leer; die drei Tabellen bleiben STRICT, die Stempeltrigger stehen.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_und_alles_ist_aus()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= AufheizErgebnisSchema.SCHRITT);
            Assert.True(AufheizvorgabeSchema.Vollstaendig());
            Assert.True(AufheizErgebnisSchema.Vollstaendig());
            Assert.Empty(AufheizvorgabeSchema.Anweisungen);
            Assert.Empty(AufheizErgebnisSchema.Anweisungen);
            Assert.True(KostenStempelSchema.Vollstaendig());
            Assert.Empty(KostenStempelSchema.FehlendeTrigger());

            List<string> einst = DataRepository.SpaltenVonTabelle("Tab_Einstellungen");
            // Hinter den fünf Spalten folgen die acht des Schritts BedarfNetzKalenderSchema (Welle M3b)
            // und die zwei der Einspeisegrenze (StromViertelstundenSchema, Welle M5) und die fünf der
            // Desinfektion (PufferOptionenSchema, Welle M7) und die zwei des Aufschlags (KP-S4).
            Assert.Equal(AufheizvorgabeSchema.SPALTENZAHL + BedarfNetzKalenderSchema.SPALTEN_EINSTELLUNGEN.Count +
                         StromViertelstundenSchema.EINSTELLUNGSSPALTEN.Length +
                         PufferOptionenSchema.EINSTELLUNGSSPALTEN.Length + AufheizManuellSchema.EINSTELLUNGSSPALTEN, einst.Count);
            Assert.Equal(SPALTEN_VORGABE, einst.Skip(AufheizvorgabeSchema.SPALTENZAHL_VORHER).Take(SPALTEN_VORGABE.Length).ToArray());
            List<string> geb = DataRepository.SpaltenVonTabelle("Tab_ErgebnisGebaeude");
            // Hinter den vierzehn Spalten folgen die drei bzw. eine des Schritts KP-S4.
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE, geb.Count);
            Assert.Equal(SPALTEN_GEBAEUDE, geb.Skip(AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE - 14).Take(14).ToArray());
            List<string> zone = DataRepository.SpaltenVonTabelle("Tab_ErgebnisZone");
            Assert.Equal(ZonenUebergabeSchema.SPALTENZAHL_ERGEBNIS_ZONE, zone.Count);
            Assert.Equal(SPALTEN_ZONE, zone.Skip(AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_ZONE - 14).Take(14).ToArray());

            // Aus ist alles ausser dem Zonenprojekt 1052 (G6d, Aufheizoptimierung an; ZonenReferenzprojektWacheTests)
            // und dem Referenzprojekt Konditionierung 1051 (KP3, RP1; Bemessung (b) mit 2 K).
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Einstellungen") > 0);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Aufheizoptimierung <> 0 AND ID_Projekt NOT IN (1051, 1052)"));
            foreach (string sp in SPALTEN_VORGABE.Skip(1))
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE \"" + sp + "\" IS NOT NULL AND ID_Projekt NOT IN (1051, 1052)"));
            foreach (string sp in SPALTEN_GEBAEUDE)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE \"" + sp + "\" IS NOT NULL"));
            foreach (string sp in SPALTEN_ZONE)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE \"" + sp + "\" IS NOT NULL"));

            foreach (string t in new[] { "Tab_Einstellungen", "Tab_ErgebnisGebaeude", "Tab_ErgebnisZone" })
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd());
            }
        }

        /// <summary>
        /// <b>Die Prüfklauseln greifen auch an der Testdatenbank</b>: Ein Wert außerhalb wird abgelehnt
        /// (ρ = 0 nach Festlegung 15, ΔT_K über 10 K, eine fremde Variante, eine Zeit über 47 h), die Zeile
        /// bleibt, wie sie war; NULL und ein gültiger Wert bestehen.
        /// </summary>
        [Fact]
        public void Die_Pruefklauseln_greifen_an_der_Testdatenbank()
        {
            if (!_db.Vorhanden) return;

            foreach (string falsch in new[]
                     {
                         "Aufheiz_Reserve = 0", "Aufheiz_Reserve = 1.5", "Aufheiz_Abzug_K = 11",
                         "Aufheiz_Bemessung = 'STUNDE_PLUS'", "Aufheiz_Art = 'MONATLICH'", "Aufheizoptimierung = 2",
                     })
                Assert.True(StilleDb.NonQuery("UPDATE Tab_Einstellungen SET " + falsch + " WHERE ID_Projekt = 1007") < 0, falsch);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE (Aufheizoptimierung <> 0 " +
                                  "OR Aufheiz_Reserve IS NOT NULL OR Aufheiz_Abzug_K IS NOT NULL) AND ID_Projekt NOT IN (1051, 1052)"));

            Assert.Equal(1, StilleDb.NonQuery("UPDATE Tab_Einstellungen SET Aufheizoptimierung = 1, Aufheiz_Reserve = 0.25, " +
                                              "Aufheiz_Abzug_K = 0, Aufheiz_Bemessung = 'STUNDE_ABZUG', Aufheiz_Art = 'FEST' " +
                                              "WHERE ID_Projekt = 1007"));
            Assert.Equal(1, StilleDb.NonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Reserve = NULL, Aufheiz_Abzug_K = NULL, " +
                                              "Aufheiz_Bemessung = NULL, Aufheiz_Art = NULL WHERE ID_Projekt = 1007"));
        }

        /// <summary>
        /// KP-S2 aus dem Stand VOR ihm: Die fünf Spalten werden entfernt; der Schritt legt sie in einem Vorgang
        /// an, die Bestandswerte der Zeilen bleiben, der Schalter steht auf 0; ein zweiter Lauf legt nichts an.
        /// Fehlt nur eine Spalte, legt er nur sie an (wiederholbar je Spalte).
        /// </summary>
        [Fact]
        public void KP_S2_aus_dem_Stand_davor_zweimal_und_je_Spalte()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand("SELECT ID, ID_Projekt, Tool_1, Kuehlbetrieb, Anlagenkopplung, Kessel_Heizgrenze " +
                                          "FROM Tab_Einstellungen ORDER BY ID");
            // Der Stand VOR KP-S2 kennt auch die acht späteren Spalten (BedarfNetzKalenderSchema) und die
            // zwei des Aufschlags (KP-S4) nicht.
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"Aufheiz_Aufschlag_Prozent\"");
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"Aufheiz_Aufschlag_H\"");
            foreach (KeyValuePair<string, string> sp in BedarfNetzKalenderSchema.SPALTEN_EINSTELLUNGEN.Reverse())
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"" + sp.Key + "\"");
            foreach (string sp in SPALTEN_VORGABE)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"" + sp + "\"");
            Assert.False(AufheizvorgabeSchema.Vollstaendig());
            Assert.Equal(AufheizvorgabeSchema.SPALTENZAHL_VORHER + StromViertelstundenSchema.EINSTELLUNGSSPALTEN.Length +
                         PufferOptionenSchema.EINSTELLUNGSSPALTEN.Length,
                         DataRepository.SpaltenVonTabelle("Tab_Einstellungen").Count);
            Assert.Equal(5, AufheizvorgabeSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(5, AufheizvorgabeSchema.Ausfuehren(bericht));
            Assert.Equal(SPALTEN_VORGABE.Select(s => "Tab_Einstellungen." + s + " anlegen"), bericht);
            Assert.True(AufheizvorgabeSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand("SELECT ID, ID_Projekt, Tool_1, Kuehlbetrieb, Anlagenkopplung, Kessel_Heizgrenze " +
                                         "FROM Tab_Einstellungen ORDER BY ID"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Aufheizoptimierung <> 0 OR " +
                                  "Aufheiz_Bemessung IS NOT NULL OR Aufheiz_Abzug_K IS NOT NULL OR " +
                                  "Aufheiz_Reserve IS NOT NULL OR Aufheiz_Art IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, AufheizvorgabeSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));

            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"Aufheiz_Reserve\"");
            Assert.Equal(new[] { "Tab_Einstellungen.Aufheiz_Reserve anlegen" }, AufheizvorgabeSchema.Anweisungen.Select(a => a.Key));
            Assert.Equal(1, AufheizvorgabeSchema.Ausfuehren(null));
            Assert.True(AufheizvorgabeSchema.Vollstaendig());
            Assert.Equal(SPALTEN_VORGABE.Take(3).Concat(new[] { "Aufheiz_Art", "Aufheiz_Reserve" }).ToArray(),
                         DataRepository.SpaltenVonTabelle("Tab_Einstellungen").Where(s => s.StartsWith("Aufheiz", StringComparison.Ordinal)).ToArray());
            Assert.Empty(KostenStempelSchema.FehlendeTrigger());
        }

        /// <summary>
        /// KP-S3 aus dem Stand VOR ihm: Die 28 Spalten werden entfernt; der Schritt legt sie in einem Vorgang
        /// an, die Ergebniszeilen bleiben, alles steht leer; ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void KP_S3_aus_dem_Stand_davor_zweimal_und_je_Spalte()
        {
            if (!_db.Vorhanden) return;

            long gebaeudeZeilen = Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude");
            long zonenZeilen = Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone");
            // Der Stand vor dem Schritt der Uebergabe je Zone (E63) kennt dessen drei Ergebnisspalten nicht.
            foreach ((string tabelle, string spalte, string _) in ZonenUebergabeSchema.SPALTEN)
                if (tabelle == ZonenUebergabeSchema.TAB_ERGEBNIS_ZONE)
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            // Der Stand VOR KP-S3 kennt die Spalten des späteren Schritts KP-S4 nicht.
            foreach ((string tabelle, string spalte, string _) in AufheizManuellSchema.SPALTEN)
                if (tabelle == "Tab_ErgebnisGebaeude" || tabelle == "Tab_ErgebnisZone")
                    DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"");
            foreach (string sp in SPALTEN_GEBAEUDE)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_ErgebnisGebaeude\" DROP COLUMN \"" + sp + "\"");
            foreach (string sp in SPALTEN_ZONE)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_ErgebnisZone\" DROP COLUMN \"" + sp + "\"");
            Assert.False(AufheizErgebnisSchema.Vollstaendig());
            Assert.Equal(KonditionierungVorlagenSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE,
                         DataRepository.SpaltenVonTabelle("Tab_ErgebnisGebaeude").Count);
            Assert.Equal(KonditionierungVorlagenSchema.SPALTENZAHL_ERGEBNIS_ZONE,
                         DataRepository.SpaltenVonTabelle("Tab_ErgebnisZone").Count);
            Assert.Equal(28, AufheizErgebnisSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(28, AufheizErgebnisSchema.Ausfuehren(bericht));
            Assert.Equal(SPALTEN_GEBAEUDE.Select(s => "Tab_ErgebnisGebaeude." + s + " anlegen")
                                          .Concat(SPALTEN_ZONE.Select(s => "Tab_ErgebnisZone." + s + " anlegen")), bericht);
            Assert.True(AufheizErgebnisSchema.Vollstaendig());
            Assert.Equal(AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisGebaeude").Count);
            Assert.Equal(AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle("Tab_ErgebnisZone").Count);
            Assert.Equal(gebaeudeZeilen, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude"));
            Assert.Equal(zonenZeilen, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone"));

            var zweiter = new List<string>();
            Assert.Equal(0, AufheizErgebnisSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));

            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_ErgebnisZone\" DROP COLUMN \"Sommerlueftungsstunden_H\"");
            Assert.Equal(new[] { "Tab_ErgebnisZone.Sommerlueftungsstunden_H anlegen" },
                         AufheizErgebnisSchema.Anweisungen.Select(a => a.Key));
            Assert.Equal(1, AufheizErgebnisSchema.Ausfuehren(null));
            Assert.True(AufheizErgebnisSchema.Vollstaendig());
        }

        // =============================================================================
        //  Teil 3 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und die
        /// Nachzieh-Liste der Tests führen beide Schritte aus derselben Quelle, hinter den Stempeln; die
        /// REPO-Datei trägt sie leer (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_beide_Schritte()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wStempel = werkzeug.IndexOf("KostenStempelSchema.Ausfuehren(", StringComparison.Ordinal);
            int wVorgabe = werkzeug.IndexOf("AufheizvorgabeSchema.Ausfuehren(", StringComparison.Ordinal);
            int wErgebnis = werkzeug.IndexOf("AufheizErgebnisSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wStempel > 0 && wVorgabe > wStempel && wErgebnis > wVorgabe, "Das Werkzeug führt die Schritte nicht hinter 159.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_AUFHEIZ_VORGABE = AufheizvorgabeSchema.SCHRITT", migration);
            Assert.Contains("SCHRITT_AUFHEIZ_ERGEBNIS = AufheizErgebnisSchema.SCHRITT", migration);
            int mStempel = migration.IndexOf("new Schritt(SCHRITT_KOSTEN_STEMPEL", StringComparison.Ordinal);
            int mVorgabe = migration.IndexOf("new Schritt(SCHRITT_AUFHEIZ_VORGABE", StringComparison.Ordinal);
            int mErgebnis = migration.IndexOf("new Schritt(SCHRITT_AUFHEIZ_ERGEBNIS", StringComparison.Ordinal);
            Assert.True(mStempel > 0 && mVorgabe > mStempel && mErgebnis > mVorgabe, "Die Schritte stehen nicht hinter 159.");
            Assert.Contains("AufheizvorgabeSchema.Anweisungen", migration);
            Assert.Contains("AufheizErgebnisSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vStempel = vorrichtung.IndexOf("KostenStempelSchema.Ausfuehren(null)", StringComparison.Ordinal);
            int vVorgabe = vorrichtung.IndexOf("AufheizvorgabeSchema.Ausfuehren(null)", StringComparison.Ordinal);
            int vErgebnis = vorrichtung.IndexOf("AufheizErgebnisSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vStempel > 0 && vVorgabe > vStempel && vErgebnis > vVorgabe, "Die Testkopie führt die Schritte nicht hinter 159.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= AufheizErgebnisSchema.SCHRITT);
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Einstellungen') " +
                                              "WHERE name = 'Aufheizoptimierung' AND type = 'INTEGER' AND \"notnull\" = 1 " +
                                              "AND dflt_value = '0'"));
            // Dazu die zwei des Aufschlags (KP-S4), ebenso nullbar.
            Assert.Equal(6L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Einstellungen') " +
                                              "WHERE name LIKE 'Aufheiz\\_%' ESCAPE '\\' AND \"notnull\" = 0 AND dflt_value IS NULL"));
            // Dazu Aufheiz_Art und Aufheizzuschlag_Kw des Schritts KP-S4.
            Assert.Equal(16L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_ErgebnisGebaeude') " +
                                               "WHERE (name LIKE 'Aufheiz%' OR name = 'HeizleistungMax_H') AND \"notnull\" = 0"));
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_ErgebnisZone') " +
                                              "WHERE name = 'Sommerlueftungsstunden_H' AND type = 'INTEGER'"));
            // Ausser dem Zonenprojekt 1052 (G6d) und dem Referenzprojekt 1051 (KP3, RP1) steht jedes Projekt auf „aus".
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Einstellungen WHERE (Aufheizoptimierung <> 0 OR " +
                                              "Aufheiz_Bemessung IS NOT NULL OR Aufheiz_Abzug_K IS NOT NULL OR " +
                                              "Aufheiz_Reserve IS NOT NULL OR Aufheiz_Art IS NOT NULL) AND ID_Projekt NOT IN (1051, 1052)"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE Aufheiz_Zustand IS NOT NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE Aufheiz_Zustand IS NOT NULL"));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Prüft eine Spalte der Tabelle <c>T</c>: jeder <paramref name="gut"/>e Wert besteht, jeder
        /// <paramref name="schlecht"/>e scheitert an der Prüfklausel (bzw. an STRICT).
        /// </summary>
        private static void Pruefe(SqliteConnection c, string spalte, string[] gut, string[] schlecht)
        {
            foreach (string w in gut)
                Assert.False(Wirft(c, "UPDATE T SET \"" + spalte + "\" = " + w), spalte + " = " + w + " wurde abgelehnt.");
            foreach (string w in schlecht)
                Assert.True(Wirft(c, "UPDATE T SET \"" + spalte + "\" = " + w), spalte + " = " + w + " wurde angenommen.");
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static List<string> Bestand(string sql)
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(sql);
            foreach (DataRow r in dt.Rows)
                liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            return liste;
        }

        /// <summary>Eine leere Datenbank im Speicher — für die Prüfungen der Definition ohne Testdatenbank.</summary>
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

        private static object Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
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
