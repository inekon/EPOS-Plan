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
    /// Die Kopierwege des Schemaschritts <b>Kalenderbedienung Stufe 2</b> (<see cref="KalenderbedienungSchema"/>): Der
    /// gemeinsame Kalender „alle Größen" mit seinen Masken und eine benannte Woche samt Verweis reisen mit dem
    /// Projektduplikat, dem Projektpaket (.wpx) und der Übernahme aus dem Gebäudekatalog — umgeschlüsselt auf die
    /// Kopie —, und die Löschkaskade des Gebäudes lässt keine Woche und keine Periode zurück.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KalenderbedienungKopierwegeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Das Gebäude des Referenzprojekts 1051 (gemeinsamer Kalender nach der Migration).</summary>
        private const long GEBAEUDE_1051 = 10657;

        /// <summary>Der Referenzkatalogbau „Referenzbau Konditionierung" (gemeinsamer Kalender nach der Migration).</summary>
        private const long KATALOGBAU = 289;

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar() && Kalendergemeinschaft.SchrittSteht();

        [Fact]
        public void Ein_Projektduplikat_nimmt_den_gemeinsamen_Kalender_und_die_benannte_Woche_umgeschluesselt_mit()
        {
            if (!Bereit()) return;
            Stand quelle = WocheAnlegen("ID_Gebaeude", GEBAEUDE_1051);
            string name = Text("SELECT Projektname FROM Tab_Projekt WHERE ID = (SELECT ID_Projekt FROM Tab_Gebaeude WHERE ID = ?)",
                               GEBAEUDE_1051);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(name, "K2 Duplikat");
            Assert.True(neu > 0);

            Pruefen(GebaeudeVon(neu), quelle);
        }

        [Fact]
        public void Das_Projektpaket_traegt_den_gemeinsamen_Kalender_und_die_benannte_Woche()
        {
            if (!Bereit()) return;
            Stand quelle = WocheAnlegen("ID_Gebaeude", GEBAEUDE_1051);
            string name = Text("SELECT Projektname FROM Tab_Projekt WHERE ID = (SELECT ID_Projekt FROM Tab_Gebaeude WHERE ID = ?)",
                               GEBAEUDE_1051);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-k2kopie-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "k2.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(name, paket));
                int neu = io.Importieren(paket, "K2 Rundlauf", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, fehler);

                Pruefen(GebaeudeVon(neu), quelle);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }

        [Fact]
        public void Die_Uebernahme_aus_dem_Katalog_nimmt_den_gemeinsamen_Kalender_und_die_benannte_Woche_mit()
        {
            if (!Bereit()) return;
            Stand quelle = WocheAnlegen("ID_Gebaeude_Stamm", KATALOGBAU);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                Konditionierungskopie.Befund b = Konditionierungskopie.Kopieren(v, KonditionierungCtrl.Eigner.Katalogbau(KATALOGBAU),
                    KonditionierungCtrl.Eigner.Gebaeude((int)GEBAEUDE_1051), Konditionierungskopie.Auswahl.Ersetzend);
                Assert.True(b.Ok, b.Meldung);
                v.Commit();
            }

            Pruefen(GEBAEUDE_1051, quelle);
        }

        [Fact]
        public void Die_Loeschkaskade_des_Gebaeudes_laesst_keine_Woche_und_keine_Periode_zurueck()
        {
            if (!Bereit()) return;
            WocheAnlegen("ID_Gebaeude", GEBAEUDE_1051);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Konditionierungswoche WHERE ID_Gebaeude = ?", GEBAEUDE_1051) > 0);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("DELETE FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@p0", GEBAEUDE_1051));
                v.Commit();
            }

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungswoche WHERE ID_Gebaeude = ?", GEBAEUDE_1051));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ?", GEBAEUDE_1051));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p WHERE NOT EXISTS " +
                                 "(SELECT 1 FROM Tab_Konditionierungskalender k WHERE k.ID = p.ID_Kalender)"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_Konditionierungsperiode')"));
        }

        // =================================================================
        //  Helfer
        // =================================================================

        /// <summary>Was die Quelle trägt: die Perioden des gemeinsamen Kalenders und die Woche samt Verweis.</summary>
        private sealed record Stand(List<string> Perioden, string Woche);

        /// <summary>
        /// Legt am Eigentümer eine benannte Woche und im gemeinsamen Kalender eine Periode an, die auf sie verweist, und
        /// liefert den Stand des gemeinsamen Kalenders (Rang, Art, Bezeichner, Tage, Regel, Maske, Wochenname).
        /// </summary>
        private static Stand WocheAnlegen(string spalte, long eigner)
        {
            long alle = Zahl("SELECT ID FROM Tab_Konditionierungskalender WHERE " + spalte + " = ? AND ID_Zone IS NULL AND " +
                             "ID_Zone_Stamm IS NULL AND Groesse = 'ALLE'", eigner);
            Assert.True(alle > 0);
            string woche = string.Join(";", Enumerable.Repeat("18", 168));
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("INSERT INTO Tab_Konditionierungswoche (" + spalte + ", Groesse, Name, Woche) VALUES (?, 'HEIZSOLL', 'Kopierwoche', ?)",
                             new DbParam("@p0", eigner), new DbParam("@p1", woche));
                long idWoche = Convert.ToInt64(v.Skalar("SELECT ID FROM Tab_Konditionierungswoche WHERE " + spalte +
                                                        " = ? AND Name = 'Kopierwoche'", new DbParam("@p0", eigner)),
                                               CultureInfo.InvariantCulture);
                v.Ausfuehren("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, ID_Woche, " +
                             "Gilt_Fuer) VALUES (?, 520, 'ZEITRAUM', 'Kopierzeit', 60, 70, ?, 1)",
                             new DbParam("@p0", alle), new DbParam("@p1", idWoche));
                v.Commit();
            }
            return new Stand(Perioden(alle), woche);
        }

        private static void Pruefen(long gebaeude, Stand quelle)
        {
            long alle = Zahl("SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL AND Groesse = 'ALLE'",
                             gebaeude);
            Assert.True(alle > 0, "Der gemeinsame Kalender fehlt an der Kopie.");
            Assert.Equal(quelle.Perioden, Perioden(alle));
            Assert.Equal(DbWerte.KOND_FEIERTAGE.Count,
                         Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode WHERE ID_Kalender = ? AND Art = 'FEIERTAG' AND " +
                              "Gilt_Fuer = 31", alle));

            // Der Verweis zeigt auf die Woche der KOPIE, nicht auf die der Quelle.
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungswoche w " +
                                 "ON w.ID = p.ID_Woche WHERE p.ID_Kalender = ? AND p.Rang = 520 AND w.ID_Gebaeude = ? AND " +
                                 "w.Name = 'Kopierwoche' AND w.Woche = ?", alle, gebaeude, quelle.Woche));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_Konditionierungsperiode')"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_Konditionierungswoche')"));
        }

        private static List<string> Perioden(long kalender)
        {
            var liste = new List<string>();
            System.Data.DataTable t = DataRepository.GetDataTable(
                "SELECT p.Rang, p.Art, p.Bezeichner, p.Beginn, p.Ende, p.Feiertagsregel, p.Gilt_Fuer, w.Name " +
                "FROM Tab_Konditionierungsperiode p LEFT JOIN Tab_Konditionierungswoche w ON w.ID = p.ID_Woche " +
                "WHERE p.ID_Kalender = ? ORDER BY p.Rang", new DbParam("@p0", kalender));
            foreach (System.Data.DataRow r in t.Rows)
                liste.Add(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
            return liste;
        }

        private static long GebaeudeVon(int projekt)
            => Zahl("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY ID LIMIT 1", projekt);

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static long Zahl(string sql, params object[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, Parameter(werte)) ?? 0L, CultureInfo.InvariantCulture);

        private static string Text(string sql, params object[] werte)
            => Convert.ToString(DataRepository.ExecuteScalar(sql, Parameter(werte)), CultureInfo.InvariantCulture) ?? "";
    }
}
