using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt <b>Kalenderbedienung Stufe 2</b> (<see cref="KalenderbedienungSchema"/>): Kette über die Konstante,
    /// Tabellen und Spalten, die Prüfregeln (gültige und ungültige Werte), die Migration der gekoppelten Kopien an
    /// Projekt 1051, der Ferienspiegel, das Ausbreiten beim Lesen, die Länderregeln an bekannten Tagen und die
    /// Wochenendmaske.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KalenderbedienungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Das Gebäude des Referenzprojekts 1051 (Kalender in allen fünf Größen, Ferien 1 und 2).</summary>
        private const long GEBAEUDE_1051 = 10657;

        [Fact]
        public void Die_Nummer_haengt_ueber_die_Klasse_an_der_PV_Ganglinie_und_ist_das_Ziel()
        {
            Assert.Equal(PvGanglinieSchema.SCHRITT + 1, KalenderbedienungSchema.SCHRITT);
            Assert.Equal(KalenderbedienungSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KalenderbedienungSchema.SCHRITT);
            object stand = DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation");
            if (_db.Vorhanden) Assert.Equal(KalenderbedienungSchema.SCHRITT, Convert.ToInt32(stand, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Tabellen_Spalten_Indizes_und_Trigger_stehen_und_der_Schritt_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KalenderbedienungSchema.Vollstaendig());
            string sql = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", KalenderbedienungSchema.TAB_WOCHE);
            Assert.EndsWith("STRICT", sql.TrimEnd());
            Assert.True(DataRepository.SpalteVorhanden(KonditionierungSchema.TAB_PERIODE, KalenderbedienungSchema.SPALTE_ID_WOCHE));
            foreach (string t in KalenderbedienungSchema.Gebaeudetabellen)
            {
                Assert.True(DataRepository.SpalteVorhanden(t, KalenderbedienungSchema.SPALTE_WOCHENENDTAGE));
                Assert.True(DataRepository.SpalteVorhanden(t, KalenderbedienungSchema.SPALTE_FEIERTAGSLAND));
            }
            var bericht = new List<string>();
            Assert.Equal(0, KalenderbedienungSchema.Ausfuehren(bericht));
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.Equal(0, Kalendergemeinschaft.AlleZusammenfuehren(v, out int kopien));
                Assert.Equal(0, kopien);
                v.Rollback();
            }
        }

        [Fact]
        public void Die_Pruefregeln_nehmen_gueltige_Werte_an_und_weisen_ungueltige_ab()
        {
            if (!_db.Vorhanden) return;
            long alle = Zahl("SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL AND Groesse = 'ALLE'",
                             GEBAEUDE_1051);
            long heiz = Zahl("SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL AND Groesse = 'HEIZSOLL'",
                             GEBAEUDE_1051);
            Assert.True(alle > 0 && heiz > 0);

            // Gültig: eine Länderregel im Größenkalender, eine Periode mit Maske im gemeinsamen Kalender, Wochenende und Land.
            Gut("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Feiertagsregel, WieWochentag) " +
                "VALUES (?, 500, 'FEIERTAG', 'Fronleichnam', 'FRONLEICHNAM', 7)", heiz);
            Gut("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Aus, Gilt_Fuer) " +
                "VALUES (?, 501, 'ZEITRAUM', 'Betriebsurlaub', 200, 210, 1, 5)", alle);
            Gut("UPDATE Tab_Gebaeude SET Wochenendtage = 64, Feiertagsland = 'BY' WHERE ID = ?", GEBAEUDE_1051);

            // Ungültig.
            Schlecht("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Feiertagsregel, WieWochentag) " +
                     "VALUES (?, 502, 'FEIERTAG', 'Unbekannt', 'AUGSBURGER_FRIEDENSFEST', 7)", heiz);
            Schlecht("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Aus) " +
                     "VALUES (?, 503, 'ZEITRAUM', 'ohne Maske', 1, 2, 1)", alle);
            Schlecht("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Aus, Gilt_Fuer) " +
                     "VALUES (?, 504, 'ZEITRAUM', 'Maske zu gross', 1, 2, 1, 32)", alle);
            Schlecht("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Aus, Gilt_Fuer) " +
                     "VALUES (?, 505, 'ZEITRAUM', 'Maske im Groessenkalender', 1, 2, 1, 1)", heiz);
            Schlecht("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende) " +
                     "VALUES (?, 506, 'FERIEN', 'ohne Angabe im Groessenkalender', 1, 2)", heiz);
            Schlecht("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Gilt_Fuer) " +
                     "VALUES (?, 507, 'ZEITRAUM', 'ohne Angabe und nicht Ferien', 1, 2, 31)", alle);
            Schlecht("INSERT INTO Tab_Konditionierungskalender (ID_Gebaeude, Groesse, Wert) VALUES (?, 'ALLE', 20)", GEBAEUDE_1051);
            Schlecht("UPDATE Tab_Gebaeude SET Wochenendtage = 128 WHERE ID = ?", GEBAEUDE_1051);
            Schlecht("UPDATE Tab_Gebaeude SET Feiertagsland = 'XX' WHERE ID = ?", GEBAEUDE_1051);
            Schlecht("INSERT INTO Tab_Konditionierungswoche (ID_Gebaeude, ID_Gebaeude_Stamm, Groesse, Name, Woche) " +
                     "VALUES (?, 289, 'HEIZSOLL', 'zwei Eigentuemer', '20')", GEBAEUDE_1051);
        }

        [Fact]
        public void Eine_benannte_Woche_geht_der_eingebetteten_vor_und_faellt_nicht_unter_einer_Periode_weg()
        {
            if (!_db.Vorhanden) return;
            long alle = Zahl("SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL AND Groesse = 'ALLE'",
                             GEBAEUDE_1051);
            string woche = string.Join(";", Enumerable.Repeat("19", 168));
            Gut("INSERT INTO Tab_Konditionierungswoche (ID_Gebaeude, Groesse, Name, Woche) VALUES (?, 'HEIZSOLL', 'Sparwoche', ?)",
                GEBAEUDE_1051, woche);
            long idWoche = Zahl("SELECT ID FROM Tab_Konditionierungswoche WHERE Name = 'Sparwoche'");
            Gut("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, ID_Woche, Gilt_Fuer) " +
                "VALUES (?, 510, 'ZEITRAUM', 'Sparzeit', 40, 50, ?, 1)", alle, idWoche);
            Schlecht("INSERT INTO Tab_Konditionierungswoche (ID_Gebaeude, Groesse, Name, Woche) VALUES (?, 'HEIZSOLL', 'Sparwoche', '1')",
                     GEBAEUDE_1051);
            Schlecht("DELETE FROM Tab_Konditionierungswoche WHERE ID = ?", idWoche);

            List<Kalenderzeile> kalender = Konditionierungdatenweg.Kalenderzeilen(GEBAEUDE_1051, null);
            List<Periodenzeile> perioden = Konditionierungdatenweg.Periodenzeilen(GEBAEUDE_1051, null);
            Kalendergemeinschaft.Ausbreiten(kalender, perioden);
            long heiz = kalender.Single(k => k.Groesse == DbWerte.KOND_GROESSE_HEIZSOLL).Id;
            Periodenzeile p = Assert.Single(perioden, x => x.IdKalender == heiz && x.Rang == 510);
            Assert.Equal(woche, p.Woche);
            Assert.Equal(idWoche, p.IdWoche);
            Assert.DoesNotContain(perioden, x => x.Rang == 510 && x.IdKalender != heiz);
        }

        [Fact]
        public void Die_Migration_hat_die_gekoppelten_Feiertage_von_1051_zusammengefuehrt_und_der_Leser_breitet_sie_aus()
        {
            if (!_db.Vorhanden) return;
            long alle = Zahl("SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL AND Groesse = 'ALLE'",
                             GEBAEUDE_1051);
            Assert.Equal(DbWerte.KOND_FEIERTAGE.Count,
                         Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode WHERE ID_Kalender = ? AND Art = 'FEIERTAG' AND Gilt_Fuer = 31",
                              alle));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                 "ON k.ID = p.ID_Kalender WHERE k.ID_Gebaeude = ? AND k.ID_Zone IS NULL AND k.Groesse <> 'ALLE' " +
                                 "AND p.Art = 'FEIERTAG'", GEBAEUDE_1051));
            // Die gespeicherten Ferienperioden je Größe tragen verschiedene Angaben - sie bleiben Kopien.
            Assert.Equal(8, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                 "ON k.ID = p.ID_Kalender WHERE k.ID_Gebaeude = ? AND k.Groesse <> 'ALLE' AND p.Art = 'FERIEN'",
                                 GEBAEUDE_1051));

            List<Kalenderzeile> kalender = Konditionierungdatenweg.Kalenderzeilen(GEBAEUDE_1051, null);
            List<Periodenzeile> perioden = Konditionierungdatenweg.Periodenzeilen(GEBAEUDE_1051, null);
            Kalendergemeinschaft.Ausbreiten(kalender, perioden);
            List<Kalenderzeile> groessen = Kalendergemeinschaft.OhneGemeinsam(kalender);
            Assert.Equal(5, groessen.Count);
            foreach (Kalenderzeile k in groessen)
            {
                List<Periodenzeile> eigene = perioden.Where(p => p.IdKalender == k.Id).ToList();
                Assert.Equal(DbWerte.KOND_FEIERTAGE.Count, eigene.Count(p => p.Art == DbWerte.KOND_ART_FEIERTAG));
                Assert.Equal(eigene.OrderByDescending(p => p.Rang).Select(p => p.Rang), eigene.Select(p => p.Rang));
                Kalenderlesung l = Kalenderleser.Lesen(k, perioden);
                Assert.Equal(Kalenderbefund.Gelesen, l.Befund);
            }
            // Die Ferienliste (ohne Angabe) breitet der Leser nicht aus.
            Assert.DoesNotContain(perioden, p => p.GiltFuer.HasValue);
        }

        [Fact]
        public void Die_Ferienliste_spiegelt_die_vier_Ferienspalten()
        {
            if (!_db.Vorhanden) return;
            string liste = "SELECT group_concat(p.Rang || ':' || p.Beginn || '-' || p.Ende, ',') FROM (SELECT p.* FROM " +
                           "Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender WHERE " +
                           "k.ID_Gebaeude = ? AND k.Groesse = 'ALLE' AND p.Art = 'FERIEN' ORDER BY p.Rang) p";
            Assert.Equal("200:357-6,201:213-226", Text(liste, GEBAEUDE_1051));

            Gut("UPDATE Tab_Gebaeude SET Ferienbeginn_3 = 100, Ferienende_3 = 110 WHERE ID = ?", GEBAEUDE_1051);
            Assert.Equal("200:357-6,201:213-226,202:100-110", Text(liste, GEBAEUDE_1051));
            Gut("UPDATE Tab_Gebaeude SET Ferienbeginn_1 = 366 WHERE ID = ?", GEBAEUDE_1051);
            Assert.Equal("201:213-226,202:100-110", Text(liste, GEBAEUDE_1051));

            // Ein Gebäude ohne Größenkalender (es rechnet auf dem Bestandszweig) bekommt keine Ferienliste.
            long ohne = Zahl("SELECT g.ID FROM Tab_Gebaeude g WHERE NOT EXISTS (SELECT 1 FROM Tab_Konditionierungskalender k " +
                             "WHERE k.ID_Gebaeude = g.ID) ORDER BY g.ID LIMIT 1");
            Gut("UPDATE Tab_Gebaeude SET Ferienbeginn_4 = 300, Ferienende_4 = 305 WHERE ID = ?", ohne);
            Assert.Equal("", Text(liste, ohne));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ?", ohne));
        }

        [Fact]
        public void Die_Laenderregeln_treffen_die_bekannten_Tage_des_Bezugsjahrs()
        {
            // 2026: Ostern am 5. April; Fronleichnam am 4. Juni, Buß- und Bettag am 18. November.
            Assert.Equal(Feiertage.Gemeinjahrestag(6, 4), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_FRONLEICHNAM, 2026));
            Assert.Equal(Feiertage.Gemeinjahrestag(11, 18), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, 2026));
            Assert.Equal(Feiertage.Gemeinjahrestag(11, 22), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_BUSS_UND_BETTAG, 2023));
            Assert.Equal(Feiertage.Gemeinjahrestag(1, 6), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_HEILIGE_DREI_KOENIGE, 2026));
            Assert.Equal(Feiertage.Gemeinjahrestag(10, 31), Feiertage.Jahrestag(DbWerte.KOND_FEIERTAG_REFORMATIONSTAG, 2026));
            Assert.Equal(9, DbWerte.KOND_FEIERTAGE.Count);
            Assert.Equal(17, DbWerte.KOND_FEIERTAGE_ALLE.Count);
            foreach (string r in DbWerte.KOND_FEIERTAGE_ALLE) Assert.True(Feiertage.Bekannt(r), r);

            // Gegenprobe: die Regeln je Land ergeben dieselben Tage wie die Landestage der Stufe 1.
            foreach (string land in Landesfeiertage.BUNDESLAENDER)
                foreach (int jahr in new[] { 2023, 2024, 2026 })
                {
                    var tage = new SortedSet<int>(Feiertage.Regeln.Select(r => Feiertage.Jahrestag(r, jahr)));
                    foreach (string r in Landesfeiertage.Regeln(land)) tage.Add(Feiertage.Jahrestag(r, jahr));
                    Assert.Equal(Landesfeiertage.Jahrestage(land, jahr), tage.ToList());
                }
            Assert.Equal(new[] { DbWerte.KOND_FEIERTAG_FRAUENTAG }, Landesfeiertage.Regeln("BE"));
            Assert.Empty(Landesfeiertage.Regeln(null));
        }

        [Fact]
        public void Die_Wochenendmaske_zaehlt_Montag_als_Bit_0_und_die_Vorgabe_ist_Samstag_und_Sonntag()
        {
            Assert.Equal(96, KalenderbedienungSchema.WOCHENENDE_VORGABE);
            for (int w = 0; w < 7; w++)
                Assert.Equal(w >= 5, KalenderbedienungSchema.IstWochenendtag(KalenderbedienungSchema.WOCHENENDE_VORGABE, w));
            Assert.True(KalenderbedienungSchema.IstWochenendtag(1 << 4, 4));
            Assert.False(KalenderbedienungSchema.IstWochenendtag(127, 7));
            Assert.Equal(1, KalenderbedienungSchema.Maskenbit(DbWerte.KOND_GROESSE_HEIZSOLL));
            Assert.Equal(16, KalenderbedienungSchema.Maskenbit(DbWerte.KOND_GROESSE_PERSONEN));
            Assert.Equal(0, KalenderbedienungSchema.Maskenbit(DbWerte.KOND_GROESSE_ALLE));

            var g = new ProjektGebaeudeModel();
            Assert.Equal(96, g.Wochenendtage);
            g.Wochenendtage = 1 << 6;
            Assert.Equal(1 << 6, Konditionierungseingang.Bestand(g, false, false).Wochenendtage);
        }

        [Fact]
        public void Repo_Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt_hinter_der_PV_Ganglinie()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("KalenderbedienungSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("PvGanglinieSchema.Ausfuehren(", StringComparison.Ordinal));
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KALENDERBEDIENUNG = KalenderbedienungSchema.SCHRITT", migration, StringComparison.Ordinal);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_KALENDERBEDIENUNG", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_PV_GANGLINIE", StringComparison.Ordinal));
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("KalenderbedienungSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("PvGanglinieSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Sicht_traegt_Wochenende_und_Feiertagsland_bis_ins_Projektmodell()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KalenderbedienungSchema.SichtSteht());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(GebaeudeSchema.KALENDER_SPALTEN, GebaeudeSchema.SichtSpalten().Skip(110));

            long projekt = Zahl("SELECT ID_Projekt FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1051);
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll((int)projekt);
            ProjektGebaeudeModel vorher = Assert.Single(ctrl.items, g => g.ID_Gebaeude == GEBAEUDE_1051);
            Assert.Equal(KalenderbedienungSchema.WOCHENENDE_VORGABE, vorher.Wochenendtage);
            Assert.Null(vorher.Feiertagsland);

            // Freitag + Samstag (Bit 4 und 5), Land Bayern.
            Gut("UPDATE Tab_Gebaeude SET Wochenendtage = 48, Feiertagsland = 'BY' WHERE ID = ?", GEBAEUDE_1051);
            ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll((int)projekt);
            ProjektGebaeudeModel nachher = Assert.Single(ctrl.items, g => g.ID_Gebaeude == GEBAEUDE_1051);
            Assert.Equal(48, nachher.Wochenendtage);
            Assert.Equal("BY", nachher.Feiertagsland);
        }

        // =================================================================
        //  Helfer
        // =================================================================

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static void Gut(string sql, params object[] werte)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql, Parameter(werte));
            v.Commit();
        }

        private static void Schlecht(string sql, params object[] werte)
        {
            using DbVorgang v = DataRepository.Vorgang();
            Assert.ThrowsAny<Exception>(() => v.Ausfuehren(sql, Parameter(werte)));
            v.Rollback();
        }

        private static long Zahl(string sql, params object[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, Parameter(werte)) ?? 0L, CultureInfo.InvariantCulture);

        private static string Text(string sql, params object[] werte)
            => Convert.ToString(DataRepository.ExecuteScalar(sql, Parameter(werte)), CultureInfo.InvariantCulture) ?? "";

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
            return d;
        }
    }
}
