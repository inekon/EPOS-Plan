using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Gebäude in DB löschen"</b> (Befund 06.10.2026) gegen eine Kopie der Testdatenbank:
    /// <see cref="GebaeudeStammCtrl.Loeschen"/> löscht einen freien Anwendersatz VOLLSTÄNDIG —
    /// samt seiner Katalogkalender, Perioden und Vorgabezeilen (Beziehung <c>ON DELETE
    /// CASCADE</c>) — und lässt Projektkopien und Projektzuordnungen unberührt. Ein Satz, den
    /// ein Projekt führt, und ein Auslieferungssatz (<c>ReadOnly</c>) werden nicht gelöscht;
    /// <see cref="GebaeudeStammCtrl.Loeschsperrgrund"/> nennt den Grund.
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

        [Fact]
        public void Ein_von_einem_Projekt_gefuehrter_Satz_wird_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            using var kultur = new Kulturvorrichtung();
            long projektgebaeude = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude");

            string grund = GebaeudeStammCtrl.Loeschsperrgrund(NAME_GEFUEHRT);
            Assert.Contains("Laurentiuskirche", grund);
            Assert.False(GebaeudeStammCtrl.Loeschen(NAME_GEFUEHRT));

            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME_GEFUEHRT));
            Assert.Equal(projektgebaeude, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude"));
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
