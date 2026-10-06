using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Gebäude in DB löschen"</b> (Befund 06.10.2026) gegen eine Kopie der Testdatenbank:
    /// <see cref="GebaeudeStammCtrl.Loeschen"/> löscht einen freien Anwendersatz VOLLSTÄNDIG —
    /// samt seiner Katalogkalender, Perioden und Vorgabezeilen (Beziehung <c>ON DELETE
    /// CASCADE</c>) — und lässt Projektkopien und Projektzuordnungen unberührt. Allein ein
    /// Auslieferungssatz (<c>ReadOnly</c>) wird nicht gelöscht;
    /// <see cref="GebaeudeStammCtrl.Loeschsperrgrund"/> nennt den Grund.
    ///
    /// <para><b>Anwenderentscheid 06.10.2026:</b> Ein Satz, den Projekte führen, wird gelöscht —
    /// jedes Projekt trägt eine vollständige Kopie. Die Fälle belegen es: Kopf, Zonen, Bauteile,
    /// Luftströme, Konditionierung, Aufbauten und Tagesverteilung der Kopien stehen danach
    /// zeilengleich, nur der Verweis <c>ID_Gebaeude_Stamm</c> ist leer, und der Lauf des Projekts
    /// schreibt bytegleiche Ergebnisse. Gelöscht wird nur auf der Arbeitskopie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeStammLoeschenTests
    {
        /// <summary>Katalogsatz 1 „AltenH-95-EnEV2016": Anwendersatz, von keinem Projekt geführt.</summary>
        private const int STAMM_FREI = 1;
        private const string NAME_FREI = "AltenH-95-EnEV2016";

        /// <summary>„Referenzbau Konditionierung" (289): Kalender und Vorgaben, geführt von 1051.</summary>
        private const int STAMM_KONDITIONIERUNG = 289;

        /// <summary>Katalogsatz 142 „EFH-A-TS-212": geführt von 1007 „Laurentiuskirche" u. a.</summary>
        private const string NAME_GEFUEHRT = "EFH-A-TS-212";

        /// <summary>Katalogsatz 283 „EFH-GEG-Ref": Auslieferungssatz.</summary>
        private const string NAME_AUSLIEFERUNG = "EFH-GEG-Ref";

        [Fact]
        public void Ein_freier_Satz_wird_samt_Katalogkalendern_vollstaendig_geloescht()
        {
            using var db = new TestDatenbank();
            KalenderKopieren(STAMM_KONDITIONIERUNG, STAMM_FREI);
            Sql("INSERT INTO Tab_Konditionierungsvorgabe (ID_Gebaeude_Stamm, Groesse, Zeile, Wert) " +
                "SELECT ?, Groesse, Zeile, Wert FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude_Stamm = ?",
                STAMM_FREI, STAMM_KONDITIONIERUNG);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ?", STAMM_FREI) > 0);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude_Stamm = ?", STAMM_FREI) > 0);

            long projektgebaeude = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude");
            long zuordnungen = Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude");
            long fremdeKalender = Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ?", STAMM_KONDITIONIERUNG);

            Assert.Equal("", GebaeudeStammCtrl.Loeschsperrgrund(NAME_FREI));
            Assert.True(GebaeudeStammCtrl.Loeschen(NAME_FREI));

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ID = ?", STAMM_FREI));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ?", STAMM_FREI));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude_Stamm = ?", STAMM_FREI));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p " +
                                  "LEFT JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender WHERE k.ID IS NULL"));

            // Nichts aus den Projekten, nichts aus fremden Katalogsätzen.
            Assert.Equal(projektgebaeude, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude"));
            Assert.Equal(zuordnungen, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude"));
            Assert.Equal(fremdeKalender, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ?", STAMM_KONDITIONIERUNG));
            Assert.DoesNotContain(GebaeudeStammCtrl.Katalogfilterzeilen(), z => z.Bezeichner == NAME_FREI);
        }

        /// <summary>Katalogsatz 56 „Hotel-G-136": geführt von 1052 und 1054, je drei Zonen mit Bauteilen.</summary>
        private const string NAME_ZONEN = "Hotel-G-136";

        /// <summary>„Referenzbau Konditionierung" (289) — die Kopie von 1051 trägt Kalender und Vorgaben.</summary>
        private const string NAME_KONDITIONIERUNG = "Referenzbau Konditionierung";
        private const int GEBAEUDE_1051 = 10657;

        [Fact]
        public void Ein_von_Projekten_gefuehrter_Satz_wird_geloescht_und_die_Kopien_bleiben_vollstaendig()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            // Die Kopien beider Saetze (1051, 1052, 1054 und weitere) - ihre Ids, fest fuer nachher.
            string ids = string.Join(",", DataRepository.GetDataTable(
                    "SELECT ID FROM Tab_Gebaeude WHERE ID_Gebaeude_Stamm IN (56, 289) ORDER BY ID").Rows
                .Cast<DataRow>().Select(z => Convert.ToString(z["ID"], CultureInfo.InvariantCulture)));
            string KOPIEN = "SELECT ID FROM Tab_Gebaeude WHERE ID IN (" + ids + ")";
            Assert.True(Zahl("SELECT COUNT(*) FROM (" + KOPIEN + ")") >= 3);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude IN (" + KOPIEN + ")") > 0);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Zone IN (SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN (" + KOPIEN + "))") > 0);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + KOPIEN + ")") > 0);
            string vorher = Abdruck(KOPIEN);

            // Kein Sperrgrund mehr; die Rückfrage nennt die Projekte, die ihre Kopie behalten.
            Assert.Equal("", GebaeudeStammCtrl.Loeschsperrgrund(NAME_ZONEN));
            Assert.Contains(Projektname(1052), GebaeudeStammCtrl.Projektkopien(NAME_ZONEN));
            Assert.Contains(Projektname(1054), GebaeudeStammCtrl.Projektkopien(NAME_ZONEN));
            string hinweis = GebaeudeStammCtrl.Loeschhinweis(NAME_ZONEN);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture,
                             WindowsFormsApplication1.MyResource.Resource.GEB_MSG_LOESCHHINWEIS_KOPIEN,
                             string.Join(", ", GebaeudeStammCtrl.Projektkopien(NAME_ZONEN))), hinweis);
            Assert.Contains(Projektname(1052), hinweis);
            Assert.Contains(Projektname(1054), hinweis);
            Assert.Equal("", GebaeudeStammCtrl.Loeschhinweis(NAME_FREI));

            long zuordnungen = Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude");
            Assert.True(GebaeudeStammCtrl.Loeschen(NAME_ZONEN));
            Assert.True(GebaeudeStammCtrl.Loeschen(NAME_KONDITIONIERUNG));

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ID IN (56, 289)"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID IN (" + ids + ") AND ID_Gebaeude_Stamm IS NOT NULL"));
            Assert.Equal(vorher, Abdruck(KOPIEN));
            Assert.Equal(zuordnungen, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_Gebaeude')"));
            Assert.Empty(GebaeudeStammCtrl.Projektkopien(NAME_ZONEN));
            Assert.Equal("", GebaeudeStammCtrl.Loeschhinweis(NAME_ZONEN));
        }

        /// <summary>
        /// <b>Die Simulation liest nichts aus dem Katalog nach:</b> Der Lauf von 1051 (Kalender,
        /// Vorgaben, Aufheizoptimierung an der Kopie) schreibt nach dem Löschen ihres Katalogsatzes
        /// dieselben Ergebnisdateien Byte für Byte.
        /// </summary>
        [Fact]
        public void Der_Lauf_des_Projekts_ist_nach_dem_Loeschen_bytegleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string a = Ordner("vorher"), b = Ordner("nachher");
            try
            {
                Assert.True(Ergebnisexport.ProjektAusfuehren(1051, a, new Protokoll()) > 0);
                Assert.True(GebaeudeStammCtrl.Loeschen(NAME_KONDITIONIERUNG));
                Assert.Null(Wert("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1051));
                Assert.True(Ergebnisexport.ProjektAusfuehren(1051, b, new Protokoll()) > 0);

                string[] da = Dateien(a), dbb = Dateien(b);
                Assert.Equal(da, dbb);
                Assert.Contains("aggregate.csv", da);
                foreach (string d in da)
                    Assert.True(File.ReadAllBytes(Path.Combine(a, d)).AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(b, d))),
                                d + " weicht nach dem Löschen des Katalogsatzes ab.");
            }
            finally
            {
                Directory.Delete(a, true);
                Directory.Delete(b, true);
            }
        }

        /// <summary>
        /// <b>Mit leerem Verweis:</b> Projektliste, Neuschreiben der Liste (auch mit dem Verweis von
        /// VOR dem Löschen, wie ihn der offene Gebäudedialog noch trägt), „Konditionierung erneut
        /// übernehmen" und „In DB übernehmen" arbeiten sauber. Die Übernahme legt einen NEUEN Satz an;
        /// auch unter dem alten Namen bindet sie die Kopie nicht an — der Namensrückfall greift nicht.
        /// </summary>
        [Fact]
        public void Mit_leerem_Verweis_bleibt_die_Kopie_und_In_DB_uebernehmen_legt_einen_neuen_Satz_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();

            List<Z_ProjGebModel> veraltet = Z_ProjGebCtrl.LiesProjekt(1051);
            Z_ProjGebModel zeile = Assert.Single(veraltet);
            Assert.NotNull(zeile.ID_Gebaeude_Stamm);
            long kalender = Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ?", GEBAEUDE_1051);
            Assert.True(kalender > 0);
            Assert.NotNull(GebaeudeStammCtrl.KonditionierungErneutUebernehmenRueckfrage(GEBAEUDE_1051));

            Assert.True(GebaeudeStammCtrl.Loeschen(NAME_KONDITIONIERUNG));

            // Die Projektliste: dieselbe Zeile, ohne Verweis.
            Z_ProjGebModel frisch = Assert.Single(Z_ProjGebCtrl.LiesProjekt(1051));
            Assert.Equal(zeile.ID_Z, frisch.ID_Z);
            Assert.Equal(NAME_KONDITIONIERUNG, frisch.Gebaeudename);
            Assert.Null(frisch.ID_Gebaeude_Stamm);

            // Ohne Katalogbau keine Katalogebene — benannt, nicht still.
            Assert.Null(GebaeudeStammCtrl.KonditionierungErneutUebernehmenRueckfrage(GEBAEUDE_1051));
            Assert.Null(GebaeudeStammCtrl.KatalogebeneDerKopie(GEBAEUDE_1051, out _));
            Assert.False(GebaeudeStammCtrl.KonditionierungErneutUebernehmen(GEBAEUDE_1051).Ok);

            // Neuschreiben mit dem veralteten Verweis: die Kopie BLEIBT samt Kalendern.
            var wizard = new WizardCtrl();
            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(1051, veraltet).Gelungen);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ? AND ID_Projekt = 1051", GEBAEUDE_1051));
            Assert.Equal(kalender, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ?", GEBAEUDE_1051));

            // „In DB übernehmen" unter dem alten Namen: ein NEUER Satz, die Kopie bleibt ohne Verweis.
            GebaeudeStammCtrl.ProjektuebernahmeErgebnis e = GebaeudeStammCtrl.AusProjektUebernehmen(zeile.ID_Z, NAME_KONDITIONIERUNG);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME_KONDITIONIERUNG));
            Assert.Equal(kalender, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ?", e.Id));
            Assert.Null(Wert("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1051));
            Assert.Null(GebaeudeStammCtrl.KonditionierungErneutUebernehmenRueckfrage(GEBAEUDE_1051));
            Assert.Empty(GebaeudeStammCtrl.Projektkopien(NAME_KONDITIONIERUNG));

            // Und noch einmal neu schreiben — auch wenn die alte Id an den neuen Satz ging.
            Assert.True(wizard.Speichere_Projekt_Gebaeudeliste(1051, veraltet).Gelungen);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ? AND ID_Projekt = 1051", GEBAEUDE_1051));
            Assert.Null(Wert("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1051));
            Assert.Equal(kalender, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ?", GEBAEUDE_1051));
        }

        /// <summary>
        /// <b>Die Gebäudeverwaltung löscht nach derselben Regel</b> (Anwenderentscheid 06.10.2026): Ihre
        /// Hülle reicht die Sperre, den Hinweis und das Löschen des Kerns. Ein Satz, den Projekte führen,
        /// hat keinen Sperrgrund, der Hinweis nennt die Projekte, gelöscht wird er, ihre Kopien bleiben;
        /// der Auslieferungssatz bleibt gesperrt.
        /// </summary>
        [Fact]
        public void Die_Verwaltung_loescht_trotz_Projektkopie_und_sperrt_den_Auslieferungssatz()
        {
            using var db = new TestDatenbank();
            using var kultur = new Kulturvorrichtung();
            IReadOnlyDictionary<string, object> gaben = GebaeudeAdminHuelle.Gaben();
            Assert.False(gaben.ContainsKey("Verwendung"));
            var sperre = (Func<string, string>)gaben["Loeschsperre"];
            var hinweis = (Func<IReadOnlyList<string>, string>)gaben["Loeschhinweis"];
            var loeschen = (Func<string, bool>)gaben["Loeschen"];

            IReadOnlyList<string> projekte = GebaeudeStammCtrl.Projektkopien(NAME_GEFUEHRT);
            Assert.Contains("Laurentiuskirche", projekte);
            Assert.Equal("", sperre(NAME_GEFUEHRT));
            Assert.Contains("Laurentiuskirche", hinweis(new[] { NAME_GEFUEHRT, NAME_FREI }));
            Assert.Equal(GebaeudeStammCtrl.Loeschhinweis(NAME_GEFUEHRT), hinweis(new[] { NAME_GEFUEHRT }));

            long kopien = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Gebaeude_Stamm = 142");
            Assert.True(kopien > 0);
            Assert.True(loeschen(NAME_GEFUEHRT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ID = 142"));
            Assert.Equal(kopien, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Gebaeudename = ? AND ID_Gebaeude_Stamm IS NULL", NAME_GEFUEHRT));

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BADM_MSG_SCHREIBGESCHUETZT, sperre(NAME_AUSLIEFERUNG));
            Assert.False(loeschen(NAME_AUSLIEFERUNG));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME_AUSLIEFERUNG));
        }

        [Fact]
        public void Ein_Auslieferungssatz_wird_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            using var kultur = new Kulturvorrichtung();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BADM_MSG_SCHREIBGESCHUETZT,
                         GebaeudeStammCtrl.Loeschsperrgrund(NAME_AUSLIEFERUNG));
            Assert.False(GebaeudeStammCtrl.Loeschen(NAME_AUSLIEFERUNG));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME_AUSLIEFERUNG));
        }

        // =====================================================================

        /// <summary>Kopiert die Katalogkalender eines Satzes samt Perioden auf einen anderen.</summary>
        private static void KalenderKopieren(int von, int nach)
        {
            System.Data.DataTable kalender = DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ? ORDER BY ID",
                new DbParam("@von", von));
            foreach (System.Data.DataRow r in kalender.Rows)
            {
                long alt = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                Sql("INSERT INTO Tab_Konditionierungskalender (ID_Gebaeude_Stamm, Groesse, Wert, Aus, Woche, Nennwert, Bemerkung, Nutzung) " +
                    "SELECT ?, Groesse, Wert, Aus, Woche, Nennwert, Bemerkung, Nutzung FROM Tab_Konditionierungskalender WHERE ID = ?",
                    nach, alt);
                long neu = Zahl("SELECT MAX(ID) FROM Tab_Konditionierungskalender");
                Sql("INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Feiertagsregel, Wert, Aus, Woche, WieWochentag) " +
                    "SELECT ?, Rang, Art, Bezeichner, Beginn, Ende, Feiertagsregel, Wert, Aus, Woche, WieWochentag " +
                    "FROM Tab_Konditionierungsperiode WHERE ID_Kalender = ?",
                    neu, alt);
            }
        }

        /// <summary>
        /// Der Abdruck aller Projektzeilen der Kopien <paramref name="kopien"/> (eine Abfrage ihrer
        /// Ids): Kopf ohne Katalogverweis, Zuordnung, Zonen, Bauteile, Luftströme, Kalender samt
        /// Perioden, Vorgaben, Tagesverteilung, Trinkwarmwasserzonen, Aufbauten, Schichten und
        /// Baustoffe — je Tabelle nach der ersten Spalte geordnet, jede Zelle invariant.
        /// </summary>
        private static string Abdruck(string kopien)
        {
            string zonen = "SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN (" + kopien + ")";
            string kal = "SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + kopien + ") OR ID_Zone IN (" + zonen + ")";
            string aufbau = "SELECT ID_Aufbau FROM Tab_Bauteil WHERE ID_Zone IN (" + zonen + ")";
            var abfragen = new (string Name, string Sql)[]
            {
                ("Tab_Gebaeude", "SELECT * FROM Tab_Gebaeude WHERE ID IN (" + kopien + ")"),
                ("Z_ProjektGebaeude", "SELECT * FROM Z_ProjektGebaeude WHERE ID IN (SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID IN (" + kopien + "))"),
                ("Tab_Zone", "SELECT * FROM Tab_Zone WHERE ID IN (" + zonen + ")"),
                ("Tab_Bauteil", "SELECT * FROM Tab_Bauteil WHERE ID_Zone IN (" + zonen + ")"),
                ("Tab_Zonenluftstrom", "SELECT * FROM Tab_Zonenluftstrom WHERE ID_ZoneA IN (" + zonen + ") OR ID_ZoneB IN (" + zonen + ")"),
                ("Tab_Konditionierungskalender", "SELECT * FROM Tab_Konditionierungskalender WHERE ID IN (" + kal + ")"),
                ("Tab_Konditionierungsperiode", "SELECT * FROM Tab_Konditionierungsperiode WHERE ID_Kalender IN (" + kal + ")"),
                ("Tab_Konditionierungsvorgabe", "SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude IN (" + kopien + ") OR ID_Zone IN (" + zonen + ")"),
                ("Tab_DBTagV", "SELECT * FROM Tab_DBTagV WHERE ID_Gebaeude IN (" + kopien + ")"),
                ("Tab_TwwZone", "SELECT * FROM Tab_TwwZone WHERE ID_Gebaeude IN (" + kopien + ")"),
                ("Tab_Bauteilaufbau", "SELECT * FROM Tab_Bauteilaufbau WHERE ID IN (" + aufbau + ")"),
                ("Tab_Bauteilschicht", "SELECT * FROM Tab_Bauteilschicht WHERE ID_Aufbau IN (" + aufbau + ")"),
                ("Tab_Baustoff", "SELECT * FROM Tab_Baustoff WHERE ID IN (SELECT ID_Baustoff FROM Tab_Bauteilschicht WHERE ID_Aufbau IN (" + aufbau + "))"),
            };
            var sb = new StringBuilder();
            foreach ((string name, string sql) in abfragen)
            {
                DataTable dt = DataRepository.GetDataTable(sql + " ORDER BY 1");
                Assert.NotNull(dt);
                sb.Append('#').Append(name).Append(' ').Append(dt.Rows.Count).Append('\n');
                foreach (DataRow r in dt.Rows)
                {
                    foreach (DataColumn c in dt.Columns)
                    {
                        if (c.ColumnName == "ID_Gebaeude_Stamm") continue;
                        sb.Append(c.ColumnName).Append('=')
                          .Append(Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append('|');
                    }
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        private static string Projektname(int id)
            => Convert.ToString(Wert("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", id), CultureInfo.InvariantCulture);

        private static string Ordner(string name)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-gebloeschen-" + name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static string[] Dateien(string ordner)
            => Directory.GetFiles(ordner).Select(Path.GetFileName).Where(f => f != "protokoll.txt")
                        .OrderBy(f => f, StringComparer.Ordinal).ToArray();

        private static object Wert(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == DBNull.Value ? null : o;
        }

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static void Sql(string sql, params object[] werte)
            => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), "Fehlgeschlagen: " + sql);

        private static long Zahl(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
