using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// BV-E0 (Konzept Berichtsvorlagen 10.3, Zeile „Datenbank") — die Tabelle
    /// <c>Berichtskonfiguration</c> OHNE die Vorsorge-DDL in <see cref="BerichtCtrl"/>.
    ///
    /// <para><b>Die Lage.</b> Die Tabelle steht im Grundschema und bekommt in
    /// Schemaschritt 96 ihren Fremdschlüssel auf <c>Tab_Projekt</c> mit
    /// <c>ON DELETE CASCADE</c> (<see cref="ProjektFremdschluessel"/>). Der Controller legt
    /// sie deshalb nicht mehr selbst an — eine solche Anlage entstünde ohne Beziehung und
    /// ohne <c>STRICT</c>. Gemessen an der Testdatenbank:
    /// <c>PRAGMA foreign_key_list(Berichtskonfiguration)</c> liefert genau eine Zeile
    /// <c>Tab_Projekt / ProjektID → ID / ON DELETE CASCADE / ON UPDATE CASCADE</c>.</para>
    ///
    /// <para><b>Geprüft wird an einer Arbeitskopie:</b> dass Speichern und Laden ohne die
    /// Vorsorge laufen und dass nach dem Löschen eines Projekts keine Zeile in
    /// <c>Berichtskonfiguration</c> zurückbleibt — über den Löschweg der Anwendung
    /// (<see cref="ProjektCtrl.LoeschenMitVorarbeiten"/>) und, als Beleg dafür, dass die
    /// Kaskade allein genügt, über ein nacktes <c>DELETE</c> ohne die Vorarbeit von Hand.
    /// Dass <c>ProjektCtrl</c> die Zeile trotzdem vorher löscht, ist ein Rückfall für eine
    /// Datenbank unter Stand 96 (Begründung an <c>BerichtsKonfigurationEntfernen</c>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtCtrlKonfigurationTests
    {
        /// <summary>
        /// Der Löschweg der Anwendung: Speichern ohne Vorsorge-DDL, Laden, Projekt löschen —
        /// danach steht weder die Zeile des Projekts noch irgendeine Waise in der Tabelle.
        /// </summary>
        [Fact]
        public void Nach_dem_Loeschen_eines_Projekts_bleibt_keine_Berichtskonfiguration()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string NAME = "Berichtskonfiguration BV-E0";
            int id = ProjektAnlegen(NAME);
            Assert.True(id > 0, "Das Probeprojekt liess sich nicht anlegen.");

            var ctrl = new BerichtCtrl();
            BerichtsKonfiguration konfig = BerichtsKonfiguration.Standard();
            konfig.ZielOrdner = "Probeordner BV-E0";
            Assert.True(ctrl.Speichere(id, konfig), "Speichern ohne Vorsorge-DDL ist fehlgeschlagen.");
            Assert.Equal("Probeordner BV-E0", ctrl.Lade(id).ZielOrdner);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = ?", id));

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(id, NAME);

            Assert.Equal(LoeschStand.Geloescht, befund.Stand);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?", id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = ?", id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration b WHERE NOT EXISTS " +
                                  "(SELECT 1 FROM Tab_Projekt p WHERE p.ID = b.ProjektID)"));
        }

        /// <summary>
        /// DER BELEG FÜR DIE KASKADE: Die Beziehung steht, beide Zugriffswege des Kerns
        /// (<see cref="DataRepository"/> und <see cref="StilleDb"/>) schalten die
        /// Fremdschlüssel ein, und ein nacktes <c>DELETE FROM Tab_Projekt</c> — ohne
        /// <see cref="ProjektCtrl.Delete"/> und seine Vorarbeiten — nimmt die
        /// Konfigurationszeile mit.
        /// </summary>
        [Fact]
        public void Die_Kaskade_nimmt_die_Konfiguration_auch_ohne_Vorarbeit_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataTable beziehungen = DataRepository.GetDataTable("PRAGMA foreign_key_list(Berichtskonfiguration)");
            Assert.NotNull(beziehungen);
            DataRow beziehung = Assert.Single(beziehungen.Rows.Cast<DataRow>());
            Assert.Equal("Tab_Projekt", Text(beziehung["table"]));
            Assert.Equal("ProjektID", Text(beziehung["from"]));
            Assert.Equal("ID", Text(beziehung["to"]));
            Assert.Equal("CASCADE", Text(beziehung["on_delete"]));

            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar("PRAGMA foreign_keys"),
                                             CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(StilleDb.Scalar("PRAGMA foreign_keys"),
                                             CultureInfo.InvariantCulture));

            int id = ProjektAnlegen("Kaskade BV-E0");
            Assert.True(id > 0, "Das Probeprojekt liess sich nicht anlegen.");
            Assert.True(new BerichtCtrl().Speichere(id, BerichtsKonfiguration.Standard()));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = ?", id));

            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Projekt WHERE ID = ?",
                                                  new DbParam("@id", id)));

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Berichtskonfiguration WHERE ProjektID = ?", id));
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        /// <summary>Legt ein leeres Projekt an und liefert seine Kennung.</summary>
        private static int ProjektAnlegen(string name)
        {
            return DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                new[] { new DbParam("@name", name) });
        }

        private static long Zahl(string sql, params object[] werte)
        {
            DbParam[] parameter = werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray();
            object wert = DataRepository.ExecuteScalar(sql, parameter);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
        {
            return Convert.ToString(wert, CultureInfo.InvariantCulture);
        }
    }
}
