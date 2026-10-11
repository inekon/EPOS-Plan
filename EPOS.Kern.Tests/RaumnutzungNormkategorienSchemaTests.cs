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
    /// <b>Der Schemaschritt NP6</b> (<see cref="RaumnutzungNormkategorienSchema"/>; Entscheid E129): Nummer und Kette, die
    /// Testkopie auf dem Schritt (nur EPOS-Muster und DIN), der Lauf über einen Stand vor dem Schritt (die Saat des Schritts 189
    /// mit SIA 2024 und VDI 2078) samt eigener Kategorie und Zuordnung, die wider Erwarten befüllte Kategorie (wird eine eigene,
    /// nichts gelöscht) und die Wiederholbarkeit.
    /// </summary>
    [Collection("Testdatenbank")]
    public class RaumnutzungNormkategorienSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =================================================================
        //  Nummer und Kette (ohne Datenbank)
        // =================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_Katalog()
        {
            // UMHÄNGEN: nach Merge von Schritt 218 (Kälteanlagen) an dessen Klasse + 1 → 219
            Assert.Equal(SchnellaufheizungSchema.SCHRITT + 1, RaumnutzungNormkategorienSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= RaumnutzungNormkategorienSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == RaumnutzungNormkategorienSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_entfallenen_Arten_stehen_noch_im_Check_aber_nicht_mehr_unter_den_Normen()
        {
            Assert.Equal(new[] { "SIA_2024", "VDI_2078" }, RaumnutzungNormkategorienSchema.ARTEN_ENTFALLEN);
            Assert.All(RaumnutzungNormkategorienSchema.ARTEN_ENTFALLEN, a => Assert.Contains(a, RaumnutzungSchema.ARTEN_KATALOG));
            Assert.Equal(new[] { RaumnutzungSchema.ART_DIN_V_18599_10 }, RaumnutzungSchema.ARTEN_NORM);
        }

        // =================================================================
        //  Testdatenbank
        // =================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_mit_EPOS_Muster_und_DIN()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= RaumnutzungNormkategorienSchema.SCHRITT);
            Assert.True(RaumnutzungNormkategorienSchema.Vollstaendig());
            Assert.Equal(0, RaumnutzungNormkategorienSchema.Offen());
            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.Equal(new[] { RaumnutzungSaat.KATEGORIE_EPOS, RaumnutzungSaat.KATEGORIE_DIN }, Ausgelieferte());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE Art IN ('SIA_2024','VDI_2078')"));
            // Der CHECK behält beide Werte (kein Tabellenneubau).
            string sql = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Tab_Raumnutzungskatalog'"), CultureInfo.InvariantCulture);
            Assert.Contains("'SIA_2024'", sql, StringComparison.Ordinal);
            Assert.Contains("'VDI_2078'", sql, StringComparison.Ordinal);
            // Die DIN trägt 45 Profile: 43 Nutzungen mit Nummer, dahinter die zwei Wohnprofile ohne Nummer und ohne Werte.
            long din = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Art = 'DIN_V_18599_10'");
            Assert.Equal(45L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + din));
            WohnprofileOhneWerte(din);
        }

        [Fact]
        public void Die_Wohnprofile_stehen_am_Ende_der_DIN_Gruppe_und_die_Zuordnung_70_71_bleibt_auf_dem_Muster()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCtrl.Kategorie din = ctrl.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_DIN_V_18599_10);
            List<Raumnutzungsprofil> profile = ctrl.Profile(din.Id).ToList();
            Assert.Equal(45, profile.Count);
            Assert.Equal(new[] { RaumnutzungNormkategorienSchema.WOHNEN_EFH, RaumnutzungNormkategorienSchema.WOHNEN_MFH },
                         profile.Skip(43).Select(p => p.Bezeichner));
            Assert.All(profile.Take(43), p => Assert.False(string.IsNullOrEmpty(p.Nummer)));
            Assert.All(profile.Skip(43), p => Assert.True(string.IsNullOrEmpty(p.Nummer)));
            // 70 und 71 der Projektdatei führen weiter auf das Muster Wohnen (NP-F13, NP-F19).
            foreach (string s in new[] { "70", "71" })
                Assert.Equal(RaumnutzungSaat.WOHNEN, Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT p.Bezeichner FROM Tab_Raumnutzungszuordnung z JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil " +
                    "WHERE z.Art = 'DIN_NUMMER' AND z.Schluessel = ?", new DbParam("@s", s)), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void In_Zonenbaum_und_Uebernahmeliste_stehen_die_Wohnprofile_am_Ende_der_DIN_Gruppe()
        {
            if (!_db.Vorhanden) return;
            string dinName = RaumnutzungSaat.KATEGORIE_DIN;
            var erwartet = new[] { RaumnutzungNormkategorienSchema.WOHNEN_EFH, RaumnutzungNormkategorienSchema.WOHNEN_MFH };

            // Zonenbaum des Imports: die Auswahl der Vorbelegung in der Ordnung des Katalogs.
            var auswahl = Raumnutzungsvorbelegung.Lesen().Auswahl().Where(a => a.Kategorie == dinName).ToList();
            Assert.Equal(45, auswahl.Count);
            Assert.Equal(erwartet, auswahl.Skip(43).Select(a => a.Name));
            Assert.DoesNotContain(Raumnutzungsvorbelegung.Lesen().Auswahl(),
                                  a => a.Art == "SIA_2024" || a.Art == "VDI_2078");

            // „Nutzungsprofil übernehmen…“: dieselbe Ordnung, je Kategorie gruppiert, EPOS-Muster vor DIN.
            var wahl = RaumnutzungHuelle.Profilwahl();
            Assert.Equal(new[] { RaumnutzungSaat.KATEGORIE_EPOS, dinName }, wahl.Select(w => w.Kategorie).Distinct().ToArray());
            var dinWahl = wahl.Where(w => w.Kategorie == dinName).ToList();
            Assert.Equal(erwartet, dinWahl.Skip(43).Select(w => w.Name));
            Assert.All(dinWahl.Skip(43), w => Assert.True(w.OhneWerte));
        }

        [Fact]
        public void Die_Wohnprofile_entstehen_ohne_Werte_einmal_und_ein_eigenes_gleichen_Namens_stoert_nicht()
        {
            if (!_db.Vorhanden) return;
            long din = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Art = 'DIN_V_18599_10'");
            // Stand vor dem Schritt: ohne Wohnprofile; dazu eine eigene Kategorie mit einem Profil desselben Namens.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND Nummer IS NULL", new DbParam("@k", din));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art, ReadOnly, Reihenfolge) VALUES ('Eigene Wohnen', 'EIGEN', 0, 9)");
            long eigen = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'Eigene Wohnen'");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Raumnutzungsprofil (ID_Katalog, Bezeichner, ReadOnly) VALUES (?, ?, 0)",
                                           new DbParam("@k", eigen), new DbParam("@b", RaumnutzungNormkategorienSchema.WOHNEN_EFH.ToUpperInvariant()));
            Assert.Equal(2, RaumnutzungNormkategorienSchema.Offen());

            var bericht = new List<string>();
            Assert.Equal(2, RaumnutzungNormkategorienSchema.Ausfuehren(bericht));
            Assert.Equal(2, bericht.Count(z => z.StartsWith("Wohnprofil '", StringComparison.Ordinal)));
            Assert.True(RaumnutzungNormkategorienSchema.Vollstaendig());
            WohnprofileOhneWerte(din);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + eigen));

            // Zweiter Lauf ändert nichts; ein DIN-Profil gleichen Namens in anderer Schreibung zählt als vorhanden.
            Assert.Equal(0, RaumnutzungNormkategorienSchema.Ausfuehren(null));
            Assert.Equal(45L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + din));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungsprofil SET Bezeichner = upper(Bezeichner) WHERE ID_Katalog = ? AND Nummer IS NULL",
                                           new DbParam("@k", din));
            Assert.Equal(0, RaumnutzungNormkategorienSchema.Ausfuehren(null));
            Assert.Equal(45L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + din));
        }

        // =================================================================
        //  Lauf über einen Stand vor dem Schritt
        // =================================================================

        [Fact]
        public void Der_Schritt_nimmt_die_leeren_Kategorien_heraus_und_laesst_Eigenes_unberuehrt()
        {
            if (!_db.Vorhanden) return;
            StandVorDemSchritt();
            long eigen = EigeneKategorieMitZuordnung();
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE ReadOnly = 1"));
            Assert.False(RaumnutzungNormkategorienSchema.Vollstaendig());
            Assert.Equal(2, RaumnutzungNormkategorienSchema.Offen());
            Assert.Equal(0, RaumnutzungSchema.OffeneSaat());   // die entfallenen Kategorien zählen nicht als offene Saat
            string bildVorher = Bild();

            var bericht = new List<string>();
            Assert.Equal(2, RaumnutzungNormkategorienSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("'SIA 2024' (SIA_2024, ohne Profil) entfernt", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("'VDI 2078' (VDI_2078, ohne Profil) entfernt", StringComparison.Ordinal));

            Assert.True(RaumnutzungNormkategorienSchema.Vollstaendig());
            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.Equal(new[] { RaumnutzungSaat.KATEGORIE_EPOS, RaumnutzungSaat.KATEGORIE_DIN }, Ausgelieferte());
            // Eigene Kategorie, Profile und Zuordnungen (ausgeliefert und eigen) unverändert.
            Assert.Equal(bildVorher, Bild());
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE ID = " + eigen + " AND ReadOnly = 0"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung WHERE Schluessel = 'mrtNp6' AND ID_Profil IS NOT NULL"));

            // Zweiter Lauf: nichts zu tun, nichts ändert sich.
            var zweiter = new List<string>();
            Assert.Equal(0, RaumnutzungNormkategorienSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));
            Assert.Equal(bildVorher, Bild());
        }

        [Fact]
        public void Eine_befuellte_Kategorie_wird_eine_eigene_und_nichts_wird_geloescht()
        {
            if (!_db.Vorhanden) return;
            StandVorDemSchritt();
            long sia = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Art = 'SIA_2024'");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Raumnutzungsprofil (ID_Katalog, Nummer, Bezeichner, ReadOnly) VALUES (?, '1.1', 'Probe SIA', 0)",
                new DbParam("@k", sia));
            long profil = Zahl("SELECT ID FROM Tab_Raumnutzungsprofil WHERE Bezeichner = 'Probe SIA'");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Raumnutzungszuordnung (Art, Schluessel, ID_Profil, ReadOnly) VALUES ('HOTTCAD_RAUMTYP', 'mrtSia', ?, 0)",
                new DbParam("@p", profil));

            var bericht = new List<string>();
            Assert.Equal(2, RaumnutzungNormkategorienSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("'SIA 2024' (SIA_2024) traegt 1 Profil(e) - NICHT geloescht", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("'VDI 2078' (VDI_2078, ohne Profil) entfernt", StringComparison.Ordinal));

            // Die Kategorie bleibt mit ihrer Id und ihrem Namen, jetzt eigen; Profil und Zuordnung stehen.
            DataRow r = DataRepository.GetDataTable("SELECT Bezeichner, Art, ReadOnly FROM Tab_Raumnutzungskatalog WHERE ID = ?",
                                                    new DbParam("@i", sia)).Rows[0];
            Assert.Equal("SIA 2024", Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
            Assert.Equal(RaumnutzungSchema.ART_EIGEN, Convert.ToString(r["Art"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID = " + profil + " AND ID_Katalog = " + sia));
            Assert.Equal(profil, Zahl("SELECT ID_Profil FROM Tab_Raumnutzungszuordnung WHERE Schluessel = 'mrtSia'"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE Art = 'VDI_2078'"));
            Assert.True(RaumnutzungNormkategorienSchema.Vollstaendig());

            // Zweiter Lauf ändert nichts.
            Assert.Equal(0, RaumnutzungNormkategorienSchema.Ausfuehren(null));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE ID = " + sia));
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        /// <summary>Beide Wohnprofile stehen in der DIN genau einmal: ohne Nummer, ohne Kennwert, Zeilenbild, Stunden, ausgeliefert.</summary>
        private static void WohnprofileOhneWerte(long din)
        {
            foreach (string name in RaumnutzungNormkategorienSchema.WOHNPROFILE)
            {
                DataTable t = DataRepository.GetDataTable(
                    "SELECT ID, Nummer, Beschreibung, ReadOnly FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND Bezeichner = ? COLLATE NOCASE",
                    new DbParam("@k", din), new DbParam("@b", name));
                Assert.Single(t.Rows);
                DataRow r = t.Rows[0];
                long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                Assert.Equal(DBNull.Value, r["Nummer"]);
                Assert.Equal(RaumnutzungNormkategorienSchema.WOHNEN_BESCHREIBUNG, Convert.ToString(r["Beschreibung"], CultureInfo.InvariantCulture));
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID = " + id + " AND (" +
                                      string.Join(" OR ", RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte + " IS NOT NULL")) + ")"));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszeile WHERE ID_Profil = " + id));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsstunden WHERE ID_Profil = " + id));
            }
        }

        /// <summary>Stellt den Stand vor dem Schritt her: die Kategorien SIA 2024 und VDI 2078 wie aus der Saat des Schritts 189.</summary>
        private static void StandVorDemSchritt()
        {
            foreach (RaumnutzungSaatkategorie k in RaumnutzungSaat.Kategorien
                         .Where(k => RaumnutzungNormkategorienSchema.ARTEN_ENTFALLEN.Contains(k.Art)))
                DataRepository.ExecuteNonQuery(RaumnutzungSchema.SQL_SAAT_KATALOG,
                    new DbParam("@b", k.Bezeichner), new DbParam("@a", k.Art), new DbParam("@d", k.Beschreibung),
                    new DbParam("@q", k.Quellenhinweis), new DbParam("@r", k.Reihenfolge), new DbParam("@b2", k.Bezeichner));
        }

        /// <summary>Eine eigene Kategorie mit einem Profil und einer eigenen Zuordnung darauf; liefert die Id der Kategorie.</summary>
        private static long EigeneKategorieMitZuordnung()
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art, ReadOnly, Reihenfolge) VALUES ('Eigene NP6', 'EIGEN', 0, 9)");
            long k = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'Eigene NP6'");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Raumnutzungsprofil (ID_Katalog, Bezeichner, ReadOnly) VALUES (?, 'Werkraum NP6', 0)", new DbParam("@k", k));
            long p = Zahl("SELECT ID FROM Tab_Raumnutzungsprofil WHERE Bezeichner = 'Werkraum NP6'");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Raumnutzungszuordnung (Art, Schluessel, ID_Profil, ReadOnly) VALUES ('HOTTCAD_RAUMTYP', 'mrtNp6', ?, 0)",
                new DbParam("@p", p));
            return k;
        }

        /// <summary>Die ausgelieferten Kategorien in Anzeigereihenfolge.</summary>
        private static string[] Ausgelieferte()
            => DataRepository.GetDataTable("SELECT Bezeichner FROM Tab_Raumnutzungskatalog WHERE ReadOnly = 1 ORDER BY Reihenfolge, ID")
                             .Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToArray();

        /// <summary>Profile, Zuordnungen und die Kategorien außer denen der entfallenen Arten als Text.</summary>
        private static string Bild()
        {
            var zeilen = new List<string>();
            foreach (DataRow r in DataRepository.GetDataTable(
                         "SELECT ID, ID_Katalog, Bezeichner, ReadOnly FROM Tab_Raumnutzungsprofil ORDER BY ID").Rows)
                zeilen.Add("P;" + string.Join(";", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))));
            foreach (DataRow r in DataRepository.GetDataTable(
                         "SELECT ID, Art, Schluessel, ID_Profil, ReadOnly FROM Tab_Raumnutzungszuordnung ORDER BY ID").Rows)
                zeilen.Add("Z;" + string.Join(";", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))));
            foreach (DataRow r in DataRepository.GetDataTable(
                         "SELECT ID, Bezeichner, Art, ReadOnly FROM Tab_Raumnutzungskatalog " +
                         "WHERE Art NOT IN ('SIA_2024','VDI_2078') ORDER BY ID").Rows)
                zeilen.Add("K;" + string.Join(";", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))));
            return string.Join("\n", zeilen);
        }

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
