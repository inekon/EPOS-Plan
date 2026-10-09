using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die wiederholbaren Saaten lassen den <b>AUTOINCREMENT-Stand</b> (<c>sqlite_sequence</c>) stehen, wenn sie nichts
    /// anlegen. SQLite zählt den Stand auch bei einem ignorierten <c>INSERT OR IGNORE</c> hoch — jeder Lauf von
    /// <c>Werkzeuge/Testdatenbankschema</c> hätte die Testdatenbank sonst ohne Inhaltsänderung verändert. Die Saaten
    /// fügen deshalb über <c>INSERT … SELECT … WHERE NOT EXISTS</c> ein.
    ///
    /// <para><b>Geprüft wird:</b> ein Wiederholungslauf der Saaten von <see cref="KaeltemaschineSchema"/>,
    /// <see cref="ProzessNutzungSchema"/> und <see cref="RaumnutzungSchema"/> legt keine Zeile an und lässt die Stände
    /// von <c>Tab_Kenndaten_Kaeltemaschine_STAMM</c>, <c>Tab_Nutzungsprofil_STAMM</c> und <c>Z_Nutzungsprofil</c>
    /// unverändert; eine gelöschte Saatzeile kommt beim nächsten Lauf mit demselben Inhalt zurück.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SaatAutoincrementTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static readonly string[] TABELLEN =
        {
            KaeltemaschineSchema.TAB_KENNDATEN_STAMM, ProzessNutzungSchema.TAB_PROFIL, ProzessNutzungSchema.TAB_ZUORDNUNG,
        };

        private static Dictionary<string, (long Stand, long Zeilen)> Lesen()
        {
            var d = new Dictionary<string, (long, long)>();
            foreach (string t in TABELLEN)
            {
                long stand = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT IFNULL(MAX(seq), 0) FROM sqlite_sequence WHERE name = ?", new DbParam("?", t)), CultureInfo.InvariantCulture);
                long zeilen = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + t + "\""),
                                              CultureInfo.InvariantCulture);
                d[t] = (stand, zeilen);
            }
            return d;
        }

        private static void SaatenLaufen()
        {
            KaeltemaschineSchema.Saat(null);
            ProzessNutzungSchema.Saat(null);
            RaumnutzungSchema.Ausfuehren(null);
        }

        [Fact]
        public void Ein_Wiederholungslauf_laesst_Staende_und_Zeilen_stehen()
        {
            if (!_db.Vorhanden) return;
            SaatenLaufen();
            var vorher = Lesen();
            SaatenLaufen();
            SaatenLaufen();
            var nachher = Lesen();
            foreach (string t in TABELLEN)
                Assert.True(vorher[t] == nachher[t], t + ": vorher " + vorher[t] + ", nachher " + nachher[t]);
        }

        [Fact]
        public void Eine_geloeschte_Saatzeile_kommt_mit_demselben_Inhalt_zurueck()
        {
            if (!_db.Vorhanden) return;
            SaatenLaufen();
            const string sql = "SELECT z.\"Quelle\", z.\"Schluessel\", p.\"Kennung\" FROM \"" + ProzessNutzungSchema.TAB_ZUORDNUNG +
                               "\" z JOIN \"" + ProzessNutzungSchema.TAB_PROFIL + "\" p ON p.\"ID\" = z.\"ID_Nutzungsprofil\" " +
                               "ORDER BY z.\"Quelle\", z.\"Schluessel\"";
            DataTable vorher = DataRepository.GetDataTable(sql);
            Assert.True(vorher.Rows.Count > 0);
            string quelle = Convert.ToString(vorher.Rows[0]["Quelle"], CultureInfo.InvariantCulture);
            string schluessel = Convert.ToString(vorher.Rows[0]["Schluessel"], CultureInfo.InvariantCulture);
            DataRepository.ExecuteSQL("DELETE FROM \"" + ProzessNutzungSchema.TAB_ZUORDNUNG + "\" WHERE \"Quelle\" = ? AND \"Schluessel\" = ?",
                                      new DbParam("?", quelle), new DbParam("?", schluessel));
            SaatenLaufen();
            DataTable nachher = DataRepository.GetDataTable(sql);
            Assert.Equal(vorher.Rows.Count, nachher.Rows.Count);
            for (int i = 0; i < vorher.Rows.Count; i++)
                Assert.Equal(vorher.Rows[i].ItemArray, nachher.Rows[i].ItemArray);
        }
    }
}
