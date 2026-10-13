using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die verwaisten Satzvorlagen</b> (<see cref="Katalogrueckweg.SatzvorlagenAbraeumen()"/>, KB-4): Eine
    /// Vorlage ohne Katalogsatz und ohne Projektzeile fällt, eine Standardvorlage und eine mit Verweis
    /// bleiben — die Regel, mit der die Auslieferungsvorlage nach dem Bereinigen aufräumt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class SatzvorlagenAbraeumenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Waise_faellt_Standard_und_verwiesene_Vorlage_bleiben()
        {
            if (!_db.Vorhanden) return;

            int waise = Vorlage("KB4 Waise", standard: false);
            int standard = Vorlage("KB4 Standard", standard: true);
            int verwiesen = Vorlage("KB4 Verwiesen", standard: false);

            // Der Verweis eines Katalogsatzes hält seine Vorlage — derselbe Weg, den die Saat gelegt hat.
            DataTable dt = DataRepository.GetDataTable("SELECT \"ID\" FROM \"Tab_WP_STAMM\" ORDER BY \"ID\" LIMIT 1");
            Assert.NotNull(dt);
            Assert.NotEmpty(dt.Rows);
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_WP_STAMM\" SET \"" + KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION +
                "\" = ? WHERE \"ID\" = ?",
                new DbParam("@v", verwiesen),
                new DbParam("@id", Convert.ToInt32(dt.Rows[0]["ID"], CultureInfo.InvariantCulture)));

            int geloescht = Katalogrueckweg.SatzvorlagenAbraeumen();
            Assert.True(geloescht >= 1, "Die Waise ist nicht gefallen (" + geloescht + ").");

            Assert.False(Steht(waise), "Die verwaiste Vorlage steht noch.");
            Assert.True(Steht(standard), "Die Standardvorlage ist gefallen.");
            Assert.True(Steht(verwiesen), "Die verwiesene Vorlage ist gefallen.");
            Assert.Equal(0, Positionen(waise));

            // Ein zweiter Lauf findet keine Waise mehr.
            Assert.Equal(0, Katalogrueckweg.SatzvorlagenAbraeumen());
        }

        [Fact]
        public void Die_Satzvorlagen_der_Katalogsaetze_stehen_als_Gruppe_hinter_den_Varianten()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KostenVorlagenCtrl.KATALOGSAETZE_GRUPPIEREN,
                        "Die Gruppierung ist abgeschaltet — die Umkehr ist eine Zeile.");

            // Die Saat hat jedem Katalogsatz mit Kosten eine Satzvorlage gegeben; sie gehören damit
            // zur Gruppe „Katalogsätze". Eine frei angelegte Variante gehört nicht dazu.
            int frei = Vorlage("KB4 Freie Variante", standard: false);
            ISet<int> katalogsaetze = KostenVorlagenCtrl.SatzvorlagenDerKataloge();
            Assert.NotEmpty(katalogsaetze);
            Assert.DoesNotContain(frei, katalogsaetze);

            // Die Wärmepumpe (Komponente 1) führt gesäte Satzvorlagen der Investition.
            IList<KostenVorlageKopf> liste =
                KostenVorlagenCtrl.Vorlagen(1, DbWerte.KOSTEN_KATEGORIE_INVESTITION);
            Assert.Contains(liste, k => k.IstKatalogsatz);

            // NICHT VERMISCHT: Hinter dem ersten Katalogsatz folgt keine freie Variante mehr.
            bool imKatalogteil = false;
            foreach (KostenVorlageKopf k in liste)
            {
                if (k.IstKatalogsatz) { imKatalogteil = true; continue; }
                Assert.False(imKatalogteil, "Die Variante „" + k.Name + "\" steht hinter einem Katalogsatz.");
            }
        }

        private static int Vorlage(string name, bool standard)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" (\"KomponentenID\", \"KategorieID\", " +
                "\"Name\", \"IstStandard\", \"ReadOnly\") VALUES (1, ?, ?, ?, 0)",
                new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION),
                new DbParam("@n", name), new DbParam("@s", standard ? 1 : 0));
            int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MAX(\"ID\") FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\""), CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_KostenVorlagePosition\" (\"VorlageID\", \"Bezeichnung\", \"Bemessung\", \"Satz\") " +
                "VALUES (?, ?, ?, 1000)",
                new DbParam("@v", id), new DbParam("@b", name), new DbParam("@m", DbWerte.BEMESSUNG_BETRAG));
            return id;
        }

        private static bool Steht(int id)
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                   "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" WHERE \"ID\" = ?",
                   new DbParam("@id", id)), CultureInfo.InvariantCulture) > 0;

        private static int Positionen(int id)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                   "SELECT COUNT(*) FROM \"Tab_KostenVorlagePosition\" WHERE \"VorlageID\" = ?",
                   new DbParam("@id", id)), CultureInfo.InvariantCulture);
    }
}
