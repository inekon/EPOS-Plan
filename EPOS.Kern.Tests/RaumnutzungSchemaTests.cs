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
    /// <b>Schritt 189 — Katalog der Nutzungsprofile</b> (NP1a, <see cref="RaumnutzungSchema"/>, <see cref="RaumnutzungSaat"/>):
    /// Nummer, Ziel und Register; die fünf Tabellen samt Prüfklauseln; die Saat vollständig, idempotent und für die drei
    /// alten Muster gleich den Vorgabezeilen der Vorlagen; der Tabellenneubau der Nutzung zeilen- und wertgleich; die
    /// Zonenspalte NULL-erhaltend in Lesen, Schreiben, Duplikat und Transfer; der Stand davor und die Wiederholbarkeit.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Saat (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_189_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(VorlaufwahlSchema.SCHRITT + 1, RaumnutzungSchema.SCHRITT);
            Assert.Equal(189, RaumnutzungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= RaumnutzungSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == RaumnutzungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Tab_Raumnutzungskatalog", "Tab_Raumnutzungsprofil", "Tab_Raumnutzungszeile",
                    "Tab_Raumnutzungsstunden", "Tab_Raumnutzungszuordnung",
                },
                RaumnutzungSchema.TABELLEN.ToArray());
            Assert.Equal(25, RaumnutzungSchema.SPALTEN_KENNWERTE.Count);
            foreach (string t in new[]
                     {
                         RaumnutzungSchema.SQL_CREATE_KATALOG, RaumnutzungSchema.SQL_CREATE_PROFIL, RaumnutzungSchema.SQL_CREATE_ZEILE,
                         RaumnutzungSchema.SQL_CREATE_STUNDEN, RaumnutzungSchema.SQL_CREATE_ZUORDNUNG,
                     })
                Assert.EndsWith(") STRICT", t, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Saat_zaehlt_vier_Kategorien_52_Profile_50_Zeilen_25_Zuordnungen()
        {
            Assert.Equal(new[] { "EPOS-Muster", "DIN/TS 18599-10", "SIA 2024", "VDI 2078" },
                         RaumnutzungSaat.Kategorien.Select(k => k.Bezeichner));
            Assert.Equal(9, RaumnutzungSaat.Muster.Count);
            Assert.Equal(43, RaumnutzungSaat.Din.Count);
            Assert.Equal(52, RaumnutzungSaat.Profile.Count);
            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORGABEZEILEN + 4, RaumnutzungSaat.Profile.Sum(p => p.Zeilen.Count));
            Assert.Equal(25, RaumnutzungSaat.Zuordnungen.Count);
            Assert.Equal(16, RaumnutzungSaat.Zuordnungen.Count(z => z.Art == RaumnutzungSchema.ZUORDNUNG_DIN));
            Assert.Equal(9, RaumnutzungSaat.Zuordnungen.Count(z => z.Art == RaumnutzungSchema.ZUORDNUNG_IFC));

            // Die DIN-Profile tragen nichts als Nummer und Name (Konzept 5.2, E90, E94): die Nummern 1 bis 43 der
            // DIN/TS 18599-10:2025-10 durchgehend ganzzahlig, keine Nummer der Ausgabe 2018 (22.1 bis 22.3), keine
            // Wohnzeile der Projektdatei (70, 71), Namen eindeutig und höchstens 80 Zeichen.
            Assert.All(RaumnutzungSaat.Din, p => Assert.True(p.IstLeer, p.ToString()));
            Assert.Equal(Enumerable.Range(1, 43).Select(n => n.ToString(CultureInfo.InvariantCulture)), RaumnutzungSaat.Din.Select(p => p.Nummer));
            Assert.All(RaumnutzungSaat.Din, p => Assert.InRange(p.Bezeichner.Length, 1, RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN));
            Assert.Equal(43, RaumnutzungSaat.Din.Select(p => p.Bezeichner.ToUpperInvariant()).Distinct().Count());
            // Sonstige ohne Kennwert; jedes andere Muster mit; keines mit Nennwert (NP-F18).
            Assert.True(RaumnutzungSaat.Muster.Single(p => p.Bezeichner == RaumnutzungSaat.SONSTIGE).IstLeer);
            Assert.All(RaumnutzungSaat.Muster.Where(p => p.Bezeichner != RaumnutzungSaat.SONSTIGE), p => Assert.False(p.IstLeer));
            Assert.All(RaumnutzungSaat.Muster, p =>
            {
                Assert.Null(p.Personen_Flaeche);
                Assert.Null(p.Personen_Waerme);
                Assert.Null(p.Geraete_Leistung);
                Assert.Null(p.Beleuchtung_Leistung);
            });
            // Jede Zuordnung zeigt auf ein EPOS-Muster (NP-F13), Schlüssel eindeutig ohne Unterschied der Schreibung.
            Assert.All(RaumnutzungSaat.Zuordnungen, z => Assert.Contains(RaumnutzungSaat.Muster, p => p.Bezeichner == z.Profil));
            Assert.Equal(RaumnutzungSaat.Zuordnungen.Count,
                         RaumnutzungSaat.Zuordnungen.Select(z => z.Art + "|" + z.Schluessel.ToUpperInvariant()).Distinct().Count());
        }

        [Fact]
        public void Die_heutigen_festen_Paare_stehen_unveraendert_in_der_Saat()
        {
            var nachNutzung = new Dictionary<string, string>
            {
                [DbWerte.KOND_NUTZUNG_BUERO] = RaumnutzungSaat.BUERO,
                [DbWerte.KOND_NUTZUNG_SCHULE] = RaumnutzungSaat.SCHULE,
                [DbWerte.KOND_NUTZUNG_WOHNEN] = RaumnutzungSaat.WOHNEN,
            };
            foreach (KeyValuePair<int, string> p in Din18599Nutzung.Tabelle)
                Assert.Equal(nachNutzung[p.Value], RaumnutzungSaat.Zuordnungen.Single(z =>
                    z.Art == RaumnutzungSchema.ZUORDNUNG_DIN && z.Schluessel == p.Key.ToString(CultureInfo.InvariantCulture)).Profil);
            foreach (string klasse in new[] { "Buero", "Wohnen", "Schlafen", "Kueche" })
                Assert.Equal(nachNutzung[Zonenplan.NutzungAusKlasse(klasse)], RaumnutzungSaat.Zuordnungen.Single(z =>
                    z.Art == RaumnutzungSchema.ZUORDNUNG_IFC && z.Schluessel == klasse).Profil);
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank, Prüfklauseln, Saat
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_samt_Saat()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= RaumnutzungSchema.SCHRITT);
            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.Equal(0, RaumnutzungSchema.OffeneSaat());
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE ReadOnly = 1"));
            Assert.Equal(52L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ReadOnly = 1"));
            Assert.Equal(50L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszeile"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsstunden"));
            Assert.Equal(25L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung WHERE ReadOnly = 1 AND ID_Profil IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE Nutzungsprofil IS NOT NULL"));
            // Kein Wert in einer Normkategorie (NP-F21); SIA und VDI leer.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil p JOIN Tab_Raumnutzungskatalog k ON k.ID = p.ID_Katalog " +
                                  "WHERE k.Art IN ('SIA_2024','VDI_2078')"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil p JOIN Tab_Raumnutzungskatalog k ON k.ID = p.ID_Katalog " +
                                  "WHERE k.Art = 'DIN_V_18599_10' AND (" +
                                  string.Join(" OR ", RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte + " IS NOT NULL")) + ")"));
            foreach (string t in RaumnutzungSchema.TABELLEN)
                Assert.EndsWith(") STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE name = ?", new DbParam("@n", t))), StringComparison.Ordinal);
            // Die Pufferauslegung findet die Musternamen wie die Kennungen (NP-F15).
            IReadOnlyDictionary<string, PufferNutzungsprofil> tabelle = NutzungsprofilZuordnung.Lesen();
            foreach (string name in new[] { "Büro", "Schule", "BUERO", "SCHULE" })
                Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE,
                             NutzungsprofilZuordnung.Finden(tabelle, NutzungsprofilQuelle.KONDITIONIERUNG, name));
            Assert.Null(NutzungsprofilZuordnung.Finden(tabelle, NutzungsprofilQuelle.KONDITIONIERUNG, "Wohnen"));
        }

        /// <summary>
        /// Die Muster Wohnen, Büro und Schule tragen als Zeilenbild genau die Vorgabezeilen der ausgelieferten Vorlagen
        /// gleicher Nutzung — gelesen aus der Testdatenbank, nicht aus dem Code (Bitgleichheit, Konzept 4.3).
        /// </summary>
        [Fact]
        public void Das_Zeilenbild_der_alten_Muster_gleicht_den_Vorgabezeilen_der_Vorlagen()
        {
            if (!_db.Vorhanden) return;
            foreach ((string Muster, string Nutzung) m in new[]
                     {
                         (RaumnutzungSaat.WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN), (RaumnutzungSaat.BUERO, DbWerte.KOND_NUTZUNG_BUERO),
                         (RaumnutzungSaat.SCHULE, DbWerte.KOND_NUTZUNG_SCHULE),
                     })
            {
                List<string> vorlage = Zeilen(
                    "SELECT v.Groesse, z.Zeile, z.Wert, z.Aus, z.Von, z.Bis, z.Bedingt_K FROM Tab_Konditionierungsvorgabe z " +
                    "JOIN Tab_Konditionierungsvorlage_STAMM v ON v.ID = z.ID_Vorlage WHERE v.ReadOnly = 1 AND v.Nutzung = ?", m.Nutzung);
                List<string> profil = Zeilen(
                    "SELECT z.Groesse, z.Zeile, z.Wert, z.Aus, z.Von, z.Bis, z.Bedingt_K FROM Tab_Raumnutzungszeile z " +
                    "JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil JOIN Tab_Raumnutzungskatalog k ON k.ID = p.ID_Katalog " +
                    "WHERE k.Bezeichner = 'EPOS-Muster' AND p.Bezeichner = ?", m.Muster);
                Assert.NotEmpty(vorlage);
                Assert.Equal(vorlage, profil);

                // Feiertage „wie Sonntag" genau dann, wenn die Vorlagenkalender der Nutzung die neun Feiertage tragen.
                long feiertage = Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                      "ON k.ID = p.ID_Kalender JOIN Tab_Konditionierungsvorlage_STAMM v ON v.ID = k.ID_Vorlage " +
                                      "WHERE v.Nutzung = '" + m.Nutzung + "' AND p.Art = 'FEIERTAG'");
                long wieSonntag = Zahl("SELECT p.Feiertage_Wie_Sonntag FROM Tab_Raumnutzungsprofil p JOIN Tab_Raumnutzungskatalog k " +
                                       "ON k.ID = p.ID_Katalog WHERE k.Bezeichner = 'EPOS-Muster' AND p.Bezeichner = '" + m.Muster + "'");
                Assert.Equal(feiertage > 0 ? 1L : 0L, wieSonntag);
            }
        }

        private static List<string> Zeilen(string sql, string wert)
        {
            DataTable t = DataRepository.GetDataTable(sql + " ORDER BY 1, 2", new DbParam("@w", wert));
            return t.Rows.Cast<DataRow>().Select(r => string.Join("|", r.ItemArray.Select(x =>
                x == DBNull.Value ? "NULL" : Convert.ToString(x, CultureInfo.InvariantCulture)))).ToList();
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Art_Grenzen_Einheit_und_Namen()
        {
            if (!_db.Vorhanden) return;
            using var c = new SqliteConnection("Data Source=" + DataRepository.PfadUeberschreibung);
            c.Open();
            void Gut(string sql) { using var k = c.CreateCommand(); k.CommandText = sql; k.ExecuteNonQuery(); }
            void Schlecht(string sql) => Assert.Throws<SqliteException>(() => Gut(sql));
            Gut("PRAGMA foreign_keys = ON");

            Gut("INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art) VALUES ('Probe', 'EIGEN')");
            long k = Convert.ToInt64(new SqliteCommand("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'Probe'", c).ExecuteScalar());
            Schlecht("INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art) VALUES ('PROBE', 'EIGEN')");      // NOCASE
            Schlecht("INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art) VALUES ('Andere', 'NORM')");
            Schlecht("INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art) VALUES (' Rand', 'EIGEN')");
            Schlecht("INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art, ReadOnly) VALUES ('Andere', 'EIGEN', 2)");

            string p = "INSERT INTO Tab_Raumnutzungsprofil (ID_Katalog, Bezeichner, Nummer, ";
            Gut(p + "Heiz_Soll, Nutzungstage_Woche, Aussenluft, Aussenluft_Einheit) VALUES (" + k + ", 'Eins', '22.1', 20, '1111100', 2, 'm3/hm2')");
            Schlecht(p + "Heiz_Soll) VALUES (" + k + ", 'eins', NULL, 20)");                                // Name je Kategorie
            Schlecht(p + "Heiz_Soll) VALUES (" + k + ", 'Zwei', '22.1', 20)");                               // Nummer je Kategorie
            Schlecht(p + "Heiz_Soll) VALUES (" + k + ", 'Zwei', NULL, 31)");
            Schlecht(p + "Kuehl_Soll) VALUES (" + k + ", 'Zwei', NULL, 14)");
            Schlecht(p + "Nutzungstage_Woche) VALUES (" + k + ", 'Zwei', NULL, '1111102')");
            Schlecht(p + "Nutzungstage_Woche) VALUES (" + k + ", 'Zwei', NULL, '111110')");
            Schlecht(p + "Nutzung_Von) VALUES (" + k + ", 'Zwei', NULL, 25)");
            Schlecht(p + "Personen_Anteil) VALUES (" + k + ", 'Zwei', NULL, 1.5)");
            Schlecht(p + "Aussenluft) VALUES (" + k + ", 'Zwei', NULL, 0.5)");                              // ohne Einheit
            Schlecht(p + "Aussenluft, Aussenluft_Einheit) VALUES (" + k + ", 'Zwei', NULL, 0.5, 'l/s')");
            Schlecht(p + "Feiertage_Wie_Sonntag) VALUES (" + k + ", 'Zwei', NULL, 2)");
            Schlecht(p + "Heiz_Soll) VALUES (999999, 'Zwei', NULL, 20)");                                   // Fremdschlüssel
            long pr = Convert.ToInt64(new SqliteCommand("SELECT ID FROM Tab_Raumnutzungsprofil WHERE Bezeichner = 'Eins'", c).ExecuteScalar());

            Gut("INSERT INTO Tab_Raumnutzungszeile (ID_Profil, Groesse, Zeile, Wert, Von, Bis) VALUES (" + pr + ", 'HEIZSOLL', 'NACHT', 16, 18, 7)");
            Schlecht("INSERT INTO Tab_Raumnutzungszeile (ID_Profil, Groesse, Zeile, Wert) VALUES (" + pr + ", 'HEIZSOLL', 'NACHT', 15)");
            Schlecht("INSERT INTO Tab_Raumnutzungszeile (ID_Profil, Groesse, Zeile, Wert) VALUES (" + pr + ", 'HEIZSOLL', 'NENNWERT', 15)");
            Schlecht("INSERT INTO Tab_Raumnutzungszeile (ID_Profil, Groesse, Zeile, Wert) VALUES (" + pr + ", 'HEIZSOLL', 'SAISON', 15)");
            Schlecht("INSERT INTO Tab_Raumnutzungszeile (ID_Profil, Groesse, Zeile, Wert, Von) VALUES (" + pr + ", 'HEIZSOLL', 'TAG', 15, 3)");
            Schlecht("INSERT INTO Tab_Raumnutzungszeile (ID_Profil, Groesse, Zeile, Wert, Bedingt_K) VALUES (" + pr + ", 'HEIZSOLL', 'TAG', 15, 1)");
            Gut("INSERT INTO Tab_Raumnutzungsstunden (ID_Profil, Groesse, Tagesart, Werte) VALUES (" + pr + ", 'PERSONEN', 'WERKTAG', '0;0;1')");
            Schlecht("INSERT INTO Tab_Raumnutzungsstunden (ID_Profil, Groesse, Tagesart, Werte) VALUES (" + pr + ", 'PERSONEN', 'WERKTAG', '1')");
            Schlecht("INSERT INTO Tab_Raumnutzungsstunden (ID_Profil, Groesse, Tagesart, Werte) VALUES (" + pr + ", 'PERSONEN', 'FEIERTAG', '1')");

            Gut("INSERT INTO Tab_Raumnutzungszuordnung (Art, Schluessel, ID_Profil) VALUES ('HOTTCAD_RAUMTYP', 'mrtProbe', " + pr + ")");
            Schlecht("INSERT INTO Tab_Raumnutzungszuordnung (Art, Schluessel, ID_Profil) VALUES ('HOTTCAD_RAUMTYP', 'MRTPROBE', NULL)");
            Schlecht("INSERT INTO Tab_Raumnutzungszuordnung (Art, Schluessel) VALUES ('GBXML', 'x')");
            Schlecht("INSERT INTO Tab_Zone (ID_Gebaeude, Rang, Bezeichner, Nutzungsprofil) VALUES (" + GEBAEUDE + ", 99, 'x', '')");

            // Profil fällt: Zeilenbild und Stunden mit (CASCADE), die Zuordnung wird „keine" (SET NULL).
            Gut("DELETE FROM Tab_Raumnutzungsprofil WHERE ID = " + pr);
            Assert.Equal(0L, Convert.ToInt64(new SqliteCommand("SELECT COUNT(*) FROM Tab_Raumnutzungszeile WHERE ID_Profil = " + pr, c).ExecuteScalar()));
            Assert.Equal(0L, Convert.ToInt64(new SqliteCommand("SELECT COUNT(*) FROM Tab_Raumnutzungsstunden WHERE ID_Profil = " + pr, c).ExecuteScalar()));
            Assert.Equal(1L, Convert.ToInt64(new SqliteCommand("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung WHERE Schluessel = 'mrtProbe' AND ID_Profil IS NULL", c).ExecuteScalar()));
        }

        [Fact]
        public void Die_Nutzung_an_Kalender_und_Vorlage_ist_freier_Text()
        {
            if (!_db.Vorhanden) return;
            foreach (string t in RaumnutzungSchema.TABELLEN_NUTZUNG)
            {
                string sql = Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE name = ?", new DbParam("@n", t)));
                Assert.Contains(RaumnutzungSchema.NUTZUNG_NEU, sql, StringComparison.Ordinal);
                Assert.DoesNotContain("'SONSTIGE'", sql, StringComparison.Ordinal);
                Assert.True(RaumnutzungSchema.NutzungFrei(t));
            }
            long id = Zahl("SELECT MIN(ID) FROM Tab_Konditionierungskalender WHERE Nutzung = 'BUERO'");
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Konditionierungskalender SET Nutzung = ? WHERE ID = ?",
                                                  new DbParam("@n", "Einzelbüro · DIN V 18599-10 Nr. 1"), new DbParam("@i", id)));
            Assert.False(DataRepository.ExecuteSQL("UPDATE Tab_Konditionierungskalender SET Nutzung = ? WHERE ID = ?",
                                                   new DbParam("@n", new string('x', 121)), new DbParam("@i", id)));
            Assert.False(DataRepository.ExecuteSQL("UPDATE Tab_Konditionierungsvorlage_STAMM SET Nutzung = '' WHERE ID = 1"));
            DataRepository.StilleFehlerAbholen();
        }

        // =============================================================================
        //  Teil 3 - Der Stand davor, der Neubau und die Wiederholbarkeit
        // =============================================================================

        /// <summary>Die Prüfsumme einer Tabelle: alle Zeilen in ID-Folge.</summary>
        private static string Pruefsumme(string tabelle)
        {
            DataTable t = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" ORDER BY ID");
            return string.Join("\n", t.Rows.Cast<DataRow>().Select(r => string.Join("|", r.ItemArray.Select(x =>
                x == DBNull.Value ? "NULL" : Convert.ToString(x, CultureInfo.InvariantCulture)))));
        }

        private static readonly string[] Bestand =
        {
            "Tab_Konditionierungskalender", "Tab_Konditionierungsvorlage_STAMM", "Tab_Konditionierungsperiode",
            "Tab_Konditionierungsvorgabe", "Tab_Zone",
        };

        /// <summary>Baut die Tabelle mit dem CHECK auf die vier Kennungen zurück — der Stand 188.</summary>
        private static void NutzungZurueck(SqliteConnection c, string tabelle)
        {
            string sql = Convert.ToString(new SqliteCommand("SELECT sql FROM sqlite_master WHERE name = '" + tabelle + "'", c).ExecuteScalar());
            string alt = sql.Replace(RaumnutzungSchema.NUTZUNG_NEU, RaumnutzungSchema.NUTZUNG_ALT, StringComparison.Ordinal);
            Assert.NotEqual(sql, alt);
            using var k = c.CreateCommand();
            long stand = Convert.ToInt64(new SqliteCommand("PRAGMA schema_version", c).ExecuteScalar());
            k.CommandText = "PRAGMA writable_schema = ON; UPDATE sqlite_master SET sql = $s WHERE type = 'table' AND name = $n; " +
                            "PRAGMA writable_schema = OFF; PRAGMA schema_version = " + (stand + 1).ToString(CultureInfo.InvariantCulture) + ";";
            k.Parameters.AddWithValue("$s", alt);
            k.Parameters.AddWithValue("$n", tabelle);
            k.ExecuteNonQuery();
        }

        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_erhaelt_den_Bestand_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            // Ein Kalender mit Perioden, damit der Neubau eine Elterntabelle mit Kindern trägt.
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode") > 0);
            long seqKalender = Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Konditionierungskalender'");
            long indizes = Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND tbl_name IN " +
                                "('Tab_Konditionierungskalender', 'Tab_Konditionierungsvorlage_STAMM')");

            // ---- Stand 188 herstellen: Tabellen weg, Zonenspalte weg, Nutzung mit Kennungs-CHECK, Pufferzuordnung weg.
            foreach (string t in RaumnutzungSchema.TABELLEN.Reverse())
                DataRepository.ExecuteNonQuery("DROP TABLE \"" + t + "\"");
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Zone DROP COLUMN Nutzungsprofil");
            DataRepository.ExecuteNonQuery("DELETE FROM Z_Nutzungsprofil WHERE Quelle = 'KONDITIONIERUNG' AND Schluessel IN ('Büro', 'Schule')");
            using (var c = new SqliteConnection("Data Source=" + DataRepository.PfadUeberschreibung + ";Pooling=False"))
            {
                c.Open();
                foreach (string t in RaumnutzungSchema.TABELLEN_NUTZUNG) NutzungZurueck(c, t);
            }
            SqliteConnection.ClearAllPools();
            GebaeudeZonenanschluss.ProbeVerwerfen();
            Assert.False(RaumnutzungSchema.Vollstaendig());
            Assert.False(RaumnutzungSchema.NutzungFrei(KonditionierungSchema.TAB_KALENDER));
            Assert.False(GebaeudeZonenanschluss.NutzungsprofilVorhanden());
            // Ohne die Spalte liest und schreibt das Aggregat weiter (älterer Stand, etwa iOS).
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, ZweiZonen()).Ok);
            Dictionary<string, string> mitZonen = Bestand.ToDictionary(t => t, Pruefsumme);

            // ---- Der Schritt.
            var bericht = new List<string>();
            int n = RaumnutzungSchema.Ausfuehren(bericht);
            Assert.Equal(5 + RaumnutzungSchema.INDIZES.Count + 2 + 1, n);
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("Saat: 4 Kategorie(n), 52 Profil(e), 50 Zeile(n)", StringComparison.Ordinal));
            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.True(GebaeudeZonenanschluss.NutzungsprofilVorhanden());

            // ---- Zählwache: Kalender, Vorlagen, Perioden, Vorgaben zeilen- und wertgleich, Zähler und Indizes erhalten.
            foreach (string t in Bestand.Where(t => t != "Tab_Zone"))
                Assert.Equal(mitZonen[t], Pruefsumme(t));
            Assert.Equal(seqKalender, Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Konditionierungskalender'"));
            Assert.Equal(indizes, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND tbl_name IN " +
                                       "('Tab_Konditionierungskalender', 'Tab_Konditionierungsvorlage_STAMM')"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE name LIKE '%" + RaumnutzungSchema.HILFSZUSATZ + "%'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_list('Tab_Konditionierungsperiode') WHERE \"table\" = 'Tab_Konditionierungskalender'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_list('Tab_Konditionierungsvorgabe') WHERE \"table\" = 'Tab_Konditionierungsvorlage_STAMM'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_list('Tab_Konditionierungskalender') WHERE \"table\" = 'Tab_Konditionierungsvorlage_STAMM'"));

            // ---- Wiederholbar: nichts zu tun, keine Zeile doppelt.
            long profile = Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil");
            long seqProfil = Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Raumnutzungsprofil'");
            Assert.Equal(0, RaumnutzungSchema.Ausfuehren(null));
            Assert.Equal(profile, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil"));
            Assert.Equal(seqProfil, Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Raumnutzungsprofil'"));
        }

        [Fact]
        public void Eine_geloeschte_Saatzeile_kommt_beim_naechsten_Lauf_wieder_ohne_Dublette()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungszuordnung WHERE Schluessel = 'Technik'");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungszeile WHERE Groesse = 'PERSONEN'");
            Assert.False(RaumnutzungSchema.Vollstaendig());
            Assert.True(RaumnutzungSchema.OffeneSaat() > 0);
            Assert.Equal(0, RaumnutzungSchema.Ausfuehren(null));
            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.Equal(25L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung"));
            Assert.Equal(50L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszeile"));
            Assert.Equal(52L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil"));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog"));
        }

        [Fact]
        public void Der_Zieltext_ersetzt_genau_eine_Stelle_oder_bricht_benannt_ab()
        {
            string kopf = "CREATE TABLE \"T\" (\n    \"ID\" INTEGER PRIMARY KEY, " + RaumnutzungSchema.NUTZUNG_ALT;
            Assert.Equal("CREATE TABLE \"T\" (\n    \"ID\" INTEGER PRIMARY KEY, " + RaumnutzungSchema.NUTZUNG_NEU + ") STRICT",
                         RaumnutzungSchema.Zieltext("T", kopf + ") STRICT"));
            Assert.Throws<InvalidOperationException>(() => RaumnutzungSchema.Zieltext("T", kopf + ")"));
            Assert.Throws<InvalidOperationException>(() => RaumnutzungSchema.Zieltext("T", kopf + ", " + RaumnutzungSchema.NUTZUNG_ALT + ") STRICT"));
            Assert.Throws<InvalidOperationException>(() => RaumnutzungSchema.Zieltext("T", "CREATE TABLE \"T\" (\"ID\" INTEGER) STRICT"));
            Assert.Throws<InvalidOperationException>(() => RaumnutzungSchema.Zieltext("U", kopf + ") STRICT"));
        }

        // =============================================================================
        //  Teil 4 - Tab_Zone.Nutzungsprofil auf den Kopierwegen
        // =============================================================================

        private static List<ZoneModel> ZweiZonen()
        {
            ZoneModel mit = GebaeudeG3PruefregelTests.GueltigeZone();
            mit.Bezeichner = "Halle";
            mit.Nutzflaeche = 40;
            mit.Nutzungsprofil = "Turnhalle · DIN V 18599-10 Nr. 31";
            ZoneModel ohne = GebaeudeG3PruefregelTests.GueltigeZone();
            ohne.Bezeichner = "Flur";
            ohne.Nutzflaeche = 20;
            return new List<ZoneModel> { mit, ohne };
        }

        private static void PruefeZweiZonen(IList<ZoneModel> g)
        {
            Assert.Equal(new[] { "Halle", "Flur" }, g.Select(z => z.Bezeichner));
            Assert.Equal("Turnhalle · DIN V 18599-10 Nr. 31", g[0].Nutzungsprofil);
            Assert.Null(g[1].Nutzungsprofil);
        }

        [Fact]
        public void Lesen_und_Schreiben_tragen_das_Nutzungsprofil_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, ZweiZonen()).Ok);
            List<ZoneModel> g = ctrl.LesenJeGebaeude(GEBAEUDE);
            PruefeZweiZonen(g);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude = " + GEBAEUDE + " AND Nutzungsprofil IS NULL"));

            g[0].Nutzungsprofil = "  ";                       // leer wird NULL, nicht ''
            g[1].Nutzungsprofil = "Büro";
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, g).Ok);
            List<ZoneModel> h = ctrl.LesenJeProjekt(PROJEKT)[GEBAEUDE];
            Assert.Null(h[0].Nutzungsprofil);
            Assert.Equal("Büro", h[1].Nutzungsprofil);

            h[1].Nutzungsprofil = new string('x', RaumnutzungSchema.NUTZUNG_MAX_ZEICHEN + 1);
            Assert.False(ctrl.SpeichernJeGebaeude(GEBAEUDE, h).Ok);
        }

        private static List<ZoneModel> Zonen(int idProjekt)
            => new GebaeudeZonenCtrl().LesenJeProjekt(idProjekt).Values.Single();

        [Fact]
        public void Das_Projektduplikat_traegt_das_Nutzungsprofil()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, ZweiZonen()).Ok);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Nutzungsprofil NP1");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeZweiZonen(Zonen(neu));
        }

        [Fact]
        public void Der_Projekttransfer_traegt_das_Nutzungsprofil()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, ZweiZonen()).Ok);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-np1-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "n.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Nutzungsprofil NP1", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeZweiZonen(Zonen(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql));
    }
}
