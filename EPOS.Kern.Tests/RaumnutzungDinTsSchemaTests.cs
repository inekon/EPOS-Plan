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
    /// <b>Schritt 190 — Kategorie DIN nach DIN/TS 18599-10:2025-10</b> (NP5b, E96, <see cref="RaumnutzungDinTsSchema"/>):
    /// Nummer, Ziel und Register; die Umnummerierung ab 22; die Testdatenbank auf dem Schritt; der Umbau der Saat 2018 mit
    /// bleibenden Ids, stehender Zuordnung und unberührten Zeilen des Anwenders; die Zuordnung DIN 19 → Verkehr und
    /// 20 → Lager nur, wo keine Zeile steht; Schritt 189 auf der Saat 2018 ohne zweite Kategorie; der benannte Abbruch samt
    /// Rücknahme. Nur Nummern und Namen — kein Wert der Norm (E90).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungDinTsSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Beschreibung der Kategorie in der Saat 2018 (Schritt 189 vor E96).</summary>
        private const string BESCHREIBUNG_2018 = "Nutzungsprofile nach DIN V 18599-10 mit Nummer und Name, ohne Werte";

        /// <summary>Quellenhinweis der Kategorie in der Saat 2018 (Schritt 189 vor E96).</summary>
        private const string QUELLE_2018 =
            "DIN V 18599-10:2018-09, Nutzungsrandbedingungen der Nichtwohn- und Wohngebäude. Werte nach Norm trägt " +
            "der Anwender aus seiner lizenzierten Ausgabe ein oder importiert sie.";

        // =============================================================================
        //  Teil 1 - Nummer, Register, Umnummerierung (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_ist_190_das_Ziel_und_die_Paketanhebung_fuehrt_Katalog()
        {
            Assert.Equal(RaumnutzungSchema.SCHRITT + 1, RaumnutzungDinTsSchema.SCHRITT);
            Assert.Equal(190, RaumnutzungDinTsSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= RaumnutzungDinTsSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == RaumnutzungDinTsSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal("DIN/TS 18599-10", RaumnutzungSaat.KATEGORIE_DIN);
            Assert.StartsWith("DIN/TS 18599-10:2025-10", RaumnutzungSaat.QUELLE_DIN, StringComparison.Ordinal);
            Assert.InRange(RaumnutzungSaat.QUELLE_DIN.Length, 1, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN);
        }

        [Fact]
        public void Die_Umnummerierung_folgt_der_Neunummerierung_ab_22()
        {
            Assert.Equal("1", RaumnutzungDinTsSchema.Nummer2025("1"));
            Assert.Equal("21", RaumnutzungDinTsSchema.Nummer2025("21"));
            Assert.Equal("22", RaumnutzungDinTsSchema.Nummer2025("22.1"));
            Assert.Equal("23", RaumnutzungDinTsSchema.Nummer2025("22.2"));
            Assert.Equal("24", RaumnutzungDinTsSchema.Nummer2025("22.3"));
            Assert.Equal("25", RaumnutzungDinTsSchema.Nummer2025("23"));
            Assert.Equal("43", RaumnutzungDinTsSchema.Nummer2025("41"));
            foreach (string keine in new[] { null, "", "0", "01", "22", "42", "47", "70", "71", "22.4", "x" })
                Assert.Null(RaumnutzungDinTsSchema.Nummer2025(keine));

            // Jedes Paar der Saat 2018 findet seine Nutzung 2025; wo die Norm den Namen nicht geändert hat, ist es derselbe.
            Assert.Equal(24, RaumnutzungDinTsSchema.SAAT_2018.Count);
            foreach ((string Nummer, string Name) p in RaumnutzungDinTsSchema.SAAT_2018)
                Assert.NotNull(RaumnutzungDinTsSchema.Name2025(RaumnutzungDinTsSchema.Nummer2025(p.Nummer)));
            foreach (string gleich in new[] { "Messe/Kongress", "Bibliothek – Lesesaal", "Bibliothek – Freihandbereich",
                                              "Saunabereich", "Fitnessraum", "Lagerhallen, Logistikhallen", "Einzelbüro", "Rechenzentrum" })
            {
                (string Nummer, string Name) alt = RaumnutzungDinTsSchema.SAAT_2018.Single(p => p.Name == gleich);
                Assert.Equal(gleich, RaumnutzungDinTsSchema.Name2025(RaumnutzungDinTsSchema.Nummer2025(alt.Nummer)));
            }
            Assert.Equal("Bühne (Theater und Veranstaltungsbauten)", RaumnutzungDinTsSchema.Name2025(RaumnutzungDinTsSchema.Nummer2025("25")));
            Assert.Equal("Zuschauerbereich (Theater und Veranstaltungsbauten)", RaumnutzungDinTsSchema.Name2025("25"));
        }

        // =============================================================================
        //  Teil 2 - Testdatenbank und Umbau
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= RaumnutzungDinTsSchema.SCHRITT);
            Assert.True(RaumnutzungDinTsSchema.Vollstaendig());
            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'DIN V 18599-10'"));
            long k = KategorieDin();
            Assert.Equal(RaumnutzungSaat.QUELLE_DIN, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Quellenhinweis FROM Tab_Raumnutzungskatalog WHERE ID = ?", new DbParam("@i", k)), CultureInfo.InvariantCulture));
            Assert.Equal(43L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ReadOnly = 1 AND ID_Katalog = " + k));
            foreach (RaumnutzungSaatprofil p in RaumnutzungSaat.Din)
                Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND Nummer = ? AND Bezeichner = ?",
                    new DbParam("@k", k), new DbParam("@n", p.Nummer), new DbParam("@b", p.Bezeichner)), CultureInfo.InvariantCulture));
            // Kein Wert, kein Zeilenbild, kein Stundenprofil in der Kategorie (E90).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + k + " AND (" +
                                  string.Join(" OR ", RaumnutzungSchema.SPALTEN_KENNWERTE.Select(s => s.Spalte + " IS NOT NULL")) + ")"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszeile z JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil WHERE p.ID_Katalog = " + k));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsstunden z JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil WHERE p.ID_Katalog = " + k));

            // Die Zuordnung DIN 19 und 20 steht ausgeliefert auf den EPOS-Mustern Verkehr und Lager (E96).
            Assert.Equal(new[] { "19", "20" }, RaumnutzungDinTsSchema.Zuordnungen.Select(z => z.Schluessel).OrderBy(s => s, StringComparer.Ordinal));
            Assert.Equal(27L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung WHERE ReadOnly = 1"));
            Assert.Equal(RaumnutzungSaat.VERKEHR, ZuordnungsprofilDin("19"));
            Assert.Equal(RaumnutzungSaat.LAGER, ZuordnungsprofilDin("20"));
        }

        [Fact]
        public void Der_Schritt_baut_die_Saat_2018_um_die_Ids_bleiben_und_Anwenderzeilen_sind_unberuehrt()
        {
            if (!_db.Vorhanden) return;
            Dictionary<string, long> ids = Fassung2018Herstellen();
            long buehne = ids["25"];

            // Zeilen des Anwenders: eine eigene Kategorie mit einem Profil gleichen Namens und gleicher Nummer, eine eigene
            // Zuordnung auf das ausgelieferte Profil „Bühne".
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Raumnutzungskatalog (Bezeichner, Art, ReadOnly) VALUES ('Eigene Probe', 'EIGEN', 0)");
            long eigen = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'Eigene Probe'");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Raumnutzungsprofil (ID_Katalog, Nummer, Bezeichner, ReadOnly) VALUES (?, '25', 'Bühne', 0)",
                                           new DbParam("@k", eigen));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Raumnutzungszuordnung (Art, Schluessel, ID_Profil, ReadOnly) VALUES ('HOTTCAD_RAUMTYP', 'mrtProbe', ?, 0)",
                                           new DbParam("@p", buehne));
            string zuordnungVorher = Zuordnungsbild();
            Assert.DoesNotContain(";DIN_NUMMER;19;", zuordnungVorher, StringComparison.Ordinal);

            Assert.False(RaumnutzungDinTsSchema.Vollstaendig());
            Assert.True(RaumnutzungDinTsSchema.Offen() > 0);

            var bericht = new List<string>();
            int n = RaumnutzungDinTsSchema.Ausfuehren(bericht);
            Assert.True(n > 0);
            Assert.Contains(bericht, z => z.Contains("24 ausgelieferte(s) Profil(e)", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("19 Nutzung(en) der Ausgabe 2025", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("DIN_NUMMER 19, 20: 2 Zeile(n)", StringComparison.Ordinal));
            Assert.True(RaumnutzungDinTsSchema.Vollstaendig());
            Assert.True(RaumnutzungSchema.Vollstaendig());

            // Die Ids der Saat 2018 tragen Nummer und Namen 2025.
            foreach ((string Nummer, string Name) p in RaumnutzungDinTsSchema.SAAT_2018)
            {
                DataRow r = DataRepository.GetDataTable("SELECT Nummer, Bezeichner, ReadOnly FROM Tab_Raumnutzungsprofil WHERE ID = ?",
                                                        new DbParam("@i", ids[p.Nummer])).Rows[0];
                string neu = RaumnutzungDinTsSchema.Nummer2025(p.Nummer);
                Assert.Equal(neu, Convert.ToString(r["Nummer"], CultureInfo.InvariantCulture));
                Assert.Equal(RaumnutzungDinTsSchema.Name2025(neu), Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
            }
            long k = KategorieDin();
            Assert.Equal(43L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + k));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE ReadOnly = 1"));

            // Zuordnung (ausgeliefert und eigen) und die eigene Kategorie unverändert; dazu DIN 19 und 20 auf den Mustern.
            Assert.Equal(zuordnungVorher, Zuordnungsbild(ohneDin1920: true));
            Assert.Equal(RaumnutzungSaat.VERKEHR, ZuordnungsprofilDin("19"));
            Assert.Equal(RaumnutzungSaat.LAGER, ZuordnungsprofilDin("20"));
            Assert.Equal(buehne, Zahl("SELECT ID_Profil FROM Tab_Raumnutzungszuordnung WHERE Schluessel = 'mrtProbe'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + eigen +
                                  " AND Nummer = '25' AND Bezeichner = 'Bühne' AND ReadOnly = 0"));

            // Wiederholbar: nichts zu tun, keine Zeile doppelt.
            long profile = Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil");
            long seq = Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Raumnutzungsprofil'");
            Assert.Equal(0, RaumnutzungDinTsSchema.Ausfuehren(null));
            Assert.Equal(0, RaumnutzungSchema.Ausfuehren(null));
            Assert.Equal(profile, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil"));
            Assert.Equal(seq, Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Raumnutzungsprofil'"));
            Assert.Equal(27L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung WHERE ReadOnly = 1"));
        }

        [Fact]
        public void Eine_eigene_Zeile_fuer_DIN_19_bleibt_DIN_20_kommt_dazu()
        {
            if (!_db.Vorhanden) return;
            // Der Anwender hat 19 auf „keine“ gestellt; die Zeile 20 fehlt (eine Datenbank, die Schritt 190 vor E96 durchlief).
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungszuordnung SET ID_Profil = NULL, ReadOnly = 0 WHERE Art = 'DIN_NUMMER' AND Schluessel = '19'");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungszuordnung WHERE Art = 'DIN_NUMMER' AND Schluessel = '20'");
            string vorher = Zuordnungsbild(ohneDin1920: true);
            Assert.Equal(1, RaumnutzungDinTsSchema.Offen());

            var bericht = new List<string>();
            Assert.Equal(1, RaumnutzungDinTsSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("DIN_NUMMER 19, 20: 1 Zeile(n)", StringComparison.Ordinal) &&
                                          z.Contains("1 stand(en) schon", StringComparison.Ordinal));
            Assert.True(RaumnutzungDinTsSchema.Vollstaendig());
            Assert.Equal(vorher, Zuordnungsbild(ohneDin1920: true));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungszuordnung WHERE Art = 'DIN_NUMMER' AND Schluessel = '19' " +
                                  "AND ID_Profil IS NULL AND ReadOnly = 0"));
            Assert.Equal(RaumnutzungSaat.LAGER, ZuordnungsprofilDin("20"));
            Assert.Equal(1L, Zahl("SELECT ReadOnly FROM Tab_Raumnutzungszuordnung WHERE Art = 'DIN_NUMMER' AND Schluessel = '20'"));
            Assert.Equal(0, RaumnutzungDinTsSchema.Ausfuehren(null));
        }

        [Fact]
        public void Fehlt_das_Muster_der_Zuordnung_bricht_der_Schritt_benannt_ab_und_nimmt_alles_zurueck()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungszuordnung WHERE Art = 'DIN_NUMMER' AND Schluessel IN ('19', '20')");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungsprofil WHERE ReadOnly = 1 AND Bezeichner = ?",
                                           new DbParam("@b", RaumnutzungSaat.VERKEHR));
            string vorher = Zuordnungsbild();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => RaumnutzungDinTsSchema.Ausfuehren(null));
            Assert.Contains("Schemaschritt 190", ex.Message, StringComparison.Ordinal);
            Assert.Contains("\"" + RaumnutzungSaat.VERKEHR + "\"", ex.Message, StringComparison.Ordinal);
            Assert.Equal(vorher, Zuordnungsbild());   // auch die Zeile 20 ist zurückgenommen
            Assert.False(RaumnutzungDinTsSchema.Vollstaendig());
        }

        [Fact]
        public void Schritt_189_auf_der_Saat_2018_legt_keine_zweite_Kategorie_an()
        {
            if (!_db.Vorhanden) return;
            Dictionary<string, long> ids = Fassung2018Herstellen();
            Assert.False(RaumnutzungSchema.Vollstaendig());

            RaumnutzungSchema.Ausfuehren(null);

            Assert.True(RaumnutzungSchema.Vollstaendig());
            Assert.True(RaumnutzungDinTsSchema.Vollstaendig());
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE ReadOnly = 1"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE Art = 'DIN_V_18599_10'"));
            Assert.Equal(52L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ReadOnly = 1"));
            Assert.Equal("Bühne (Theater und Veranstaltungsbauten)", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Raumnutzungsprofil WHERE ID = ?", new DbParam("@i", ids["25"])), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Eine_fremde_Zeile_mit_Name_oder_Nummer_2025_bricht_benannt_ab_und_nimmt_alles_zurueck()
        {
            if (!_db.Vorhanden) return;
            Fassung2018Herstellen();
            long k = Zahl("SELECT ID FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'DIN V 18599-10'");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Raumnutzungsprofil (ID_Katalog, Nummer, Bezeichner, ReadOnly) VALUES (?, '99', ?, 0)",
                                           new DbParam("@k", k), new DbParam("@b", "Bühne (Theater und Veranstaltungsbauten)"));
            string vorher = Profilbild();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => RaumnutzungDinTsSchema.Ausfuehren(null));
            Assert.Contains("Schemaschritt 190", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Bühne (Theater und Veranstaltungsbauten)", ex.Message, StringComparison.Ordinal);
            Assert.Equal(vorher, Profilbild());
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungskatalog WHERE Bezeichner = 'DIN V 18599-10'"));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>
        /// Stellt die Kategorie DIN der Saat 2018 her (Name, Texte, 24 Paare, ohne die 19 neuen Nutzungen, Zuordnung ohne
        /// DIN 19 und 20) und liefert die Id je
        /// Nummer 2018 — aus der Saat 2025 rückwärts über <see cref="RaumnutzungDinTsSchema.Nummer2025"/>.
        /// </summary>
        private static Dictionary<string, long> Fassung2018Herstellen()
        {
            long k = KategorieDin();
            var ids = new Dictionary<string, long>();
            foreach ((string Nummer, string Name) p in RaumnutzungDinTsSchema.SAAT_2018)
                ids[p.Nummer] = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT ID FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND Nummer = ?",
                    new DbParam("@k", k), new DbParam("@n", RaumnutzungDinTsSchema.Nummer2025(p.Nummer))), CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungsprofil SET Nummer = NULL, Bezeichner = '~p~' || ID WHERE ID_Katalog = ?",
                                           new DbParam("@k", k));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = ? AND ID NOT IN (" +
                                           string.Join(",", ids.Values.Select(i => i.ToString(CultureInfo.InvariantCulture))) + ")",
                                           new DbParam("@k", k));
            foreach ((string Nummer, string Name) p in RaumnutzungDinTsSchema.SAAT_2018)
                DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungsprofil SET Nummer = ?, Bezeichner = ? WHERE ID = ?",
                                               new DbParam("@n", p.Nummer), new DbParam("@b", p.Name), new DbParam("@i", ids[p.Nummer]));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Raumnutzungskatalog SET Bezeichner = ?, Beschreibung = ?, Quellenhinweis = ? WHERE ID = ?",
                                           new DbParam("@b", RaumnutzungDinTsSchema.KATEGORIE_DIN_2018), new DbParam("@d", BESCHREIBUNG_2018),
                                           new DbParam("@q", QUELLE_2018), new DbParam("@i", k));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Raumnutzungszuordnung WHERE Art = 'DIN_NUMMER' AND Schluessel IN ('19', '20')");
            Assert.Equal(24L, Zahl("SELECT COUNT(*) FROM Tab_Raumnutzungsprofil WHERE ID_Katalog = " + k));
            return ids;
        }

        private static long KategorieDin()
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Raumnutzungskatalog WHERE ReadOnly = 1 AND Bezeichner = ?",
                                                            new DbParam("@b", RaumnutzungSaat.KATEGORIE_DIN)), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilen der Zuordnung als Text; auf Wunsch ohne die Schlüssel DIN 19 und 20.</summary>
        private static string Zuordnungsbild(bool ohneDin1920 = false)
            => string.Join("|", DataRepository.GetDataTable("SELECT ID, Art, Schluessel, ID_Profil, ReadOnly FROM Tab_Raumnutzungszuordnung" +
                                                            (ohneDin1920 ? " WHERE NOT (Art = 'DIN_NUMMER' AND Schluessel IN ('19', '20'))" : "") +
                                                            " ORDER BY ID")
                .Rows.Cast<DataRow>().Select(r => string.Join(";", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))));

        private static string Profilbild()
            => string.Join("|", DataRepository.GetDataTable("SELECT ID, ID_Katalog, Nummer, Bezeichner, ReadOnly FROM Tab_Raumnutzungsprofil ORDER BY ID")
                .Rows.Cast<DataRow>().Select(r => string.Join(";", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))))
               + "#" + string.Join("|", DataRepository.GetDataTable("SELECT ID, Bezeichner, Quellenhinweis FROM Tab_Raumnutzungskatalog ORDER BY ID")
                .Rows.Cast<DataRow>().Select(r => string.Join(";", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))));

        /// <summary>Der Name des Profils, auf das die Zeile <c>DIN_NUMMER</c> des Schlüssels zeigt; <c>null</c> ohne Zeile oder bei „keine“.</summary>
        private static string ZuordnungsprofilDin(string schluessel)
            => DataRepository.ExecuteScalar("SELECT p.Bezeichner FROM Tab_Raumnutzungszuordnung z JOIN Tab_Raumnutzungsprofil p ON p.ID = z.ID_Profil " +
                                            "JOIN Tab_Raumnutzungskatalog k ON k.ID = p.ID_Katalog WHERE z.Art = 'DIN_NUMMER' AND z.Schluessel = ? " +
                                            "AND k.Art = 'EPOS_MUSTER' AND k.ReadOnly = 1 AND z.ReadOnly = 1", new DbParam("@s", schluessel)) is string s ? s : null;

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
