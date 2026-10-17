using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Abgleich einer Anlage mit dem zentralen Kostenkatalog</b> (<see cref="Katalogkostenabgleich"/>, KB-3):
    /// die drei Ergebnisarten, die benannten Absagen und dass nichts von selbst geschrieben wird.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogkostenabgleichTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private const int KOMPONENTE_WP = 1;

        [Fact]
        public void Ohne_Gewerk_und_ohne_Zuordnung_sagt_er_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            // Eine Art, die keinen Ursprung führt (Kältemaschine, Rückkühlwerk): kein Gewerk.
            Assert.Equal(Abgleichabsage.KeinGewerk,
                         Katalogkostenabgleich.Vorschau(1030, 1, 99, KOMPONENTE_WP).Absage);
            Assert.Equal(Abgleichabsage.KeinGewerk,
                         Katalogkostenabgleich.Vorschau(0, 0, WizardItemClass.WP_TYP, KOMPONENTE_WP).Absage);
            Assert.False(Katalogrueckweg.Gewerkbekannt(99));
            Assert.True(Katalogrueckweg.Gewerkbekannt(WizardItemClass.WP_TYP));

            // Eine Anlage, deren Projektkopie ihren Katalogsatz nicht kennt (ID_Stamm leer, Schritt 208).
            // Gesucht wird sie mit DIESER Bedingung, nicht nach Reihenfolge: Die Arbeitskopie der
            // Sammlung kann einen Ursprung tragen, den ein anderer Fall gesetzt hat.
            DataRow a = AnlageOhneUrsprung(ohneStamm: true);
            if (a == null) return;
            Assert.Equal(Abgleichabsage.KeineZuordnung,
                         Katalogkostenabgleich.Vorschau(Ganz(a, "ID_Projekt"), Ganz(a, "ID"),
                                                        WizardItemClass.WP_TYP, KOMPONENTE_WP).Absage);
        }

        [Fact]
        public void Fehlt_dann_gleich_dann_weicht_ab_und_wieder_gleich()
        {
            if (!_db.Vorhanden) return;
            DataRow a = AnlageOhneUrsprung();
            if (a == null) return;
            int idProjekt = Ganz(a, "ID_Projekt"), idAnlage = Ganz(a, "ID"), idKopie = Ganz(a, "ID_WP");

            // Einen Katalogsatz mit gesäter Investitionsvorlage als Ursprung setzen (nur in der Arbeitskopie).
            object satz = DataRepository.ExecuteScalar(
                "SELECT MIN(\"ID\") FROM \"Tab_WP_STAMM\" WHERE \"" +
                KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION + "\" IS NOT NULL");
            if (satz == null || satz == DBNull.Value) return;
            int idSatz = Convert.ToInt32(satz, CultureInfo.InvariantCulture);
            DataRepository.ExecuteSQL("UPDATE \"Tab_WP\" SET \"ID_Stamm\" = ? WHERE \"ID\" = ?",
                                      new DbParam("@s", idSatz), new DbParam("@k", idKopie));
            Assert.Equal(idSatz, Katalogrueckweg.UrsprungsSatz(idAnlage, WizardItemClass.WP_TYP));

            // KB-4: Der Katalogwert steht in der Satzvorlage, nicht mehr in Tab_WP_STAMM.Modulkosten
            // (Schemaschritt GeraetekostenEntfallSchema.SCHRITT).
            int idVorlage = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT \"" + KatalogkostenInvestitionSchema.SPALTE_ID_KOSTENVORLAGE_INVESTITION +
                "\" FROM \"Tab_WP_STAMM\" WHERE \"ID\" = ?", new DbParam("@s", idSatz)),
                CultureInfo.InvariantCulture);
            System.Collections.Generic.IList<KostenVorlagenPosition> vorlage = KostenVorlagenCtrl.Positionen(idVorlage);
            Assert.NotEmpty(vorlage);
            Assert.True(vorlage[0].Satz.HasValue);
            double katalogwert = vorlage[0].Satz.Value;
            Assert.True(katalogwert > 0);

            // (1) Vorschau: die Position der Satzvorlage fehlt an der Anlage - und die Vorschau schreibt nichts.
            long vorher = Positionen(idProjekt);
            Abgleichbefund b1 = Katalogkostenabgleich.Vorschau(idProjekt, idAnlage, WizardItemClass.WP_TYP, KOMPONENTE_WP);
            Assert.Equal(Abgleichabsage.Keine, b1.Absage);
            Assert.True(b1.Handlungsbedarf);
            Assert.Equal(1, b1.Zeilen.Count);
            Assert.Equal(DbWerte.KOSTEN_KATEGORIE_INVESTITION, b1.Zeilen[0].Kategorie);
            Assert.Equal(DbWerte.BEMESSUNG_BETRAG, b1.Zeilen[0].Bemessung);
            Assert.Equal(katalogwert, b1.Zeilen[0].Katalogwert.Value, 6);
            Assert.Equal(vorher, Positionen(idProjekt));

            // (2) Ausführen legt sie an; ein zweiter Durchgang meldet „gleich".
            Abgleichergebnis e1 = Katalogkostenabgleich.Ausfuehren(idProjekt, idAnlage, WizardItemClass.WP_TYP, KOMPONENTE_WP);
            Assert.False(e1.Fehler);
            Assert.Equal(1, e1.Angelegt);
            Assert.Equal(0, e1.Gesetzt);
            Abgleichbefund b2 = Katalogkostenabgleich.Vorschau(idProjekt, idAnlage, WizardItemClass.WP_TYP, KOMPONENTE_WP);
            Assert.False(b2.Handlungsbedarf);
            Assert.Equal(1, b2.Gleich);
            Assert.Equal(katalogwert, b2.Zeilen[0].Projektwert.Value, 6);

            // (3) Projektwert verstellen: die Zeile weicht ab, beide Zahlen stehen darin.
            int idPosition = b2.Zeilen[0].PositionsId;
            Assert.True(idPosition > 0);
            Assert.True(KostenPositionCtrl.SetzeBetragNachId(idPosition, katalogwert + 1234.0));
            Abgleichbefund b3 = Katalogkostenabgleich.Vorschau(idProjekt, idAnlage, WizardItemClass.WP_TYP, KOMPONENTE_WP);
            Assert.Equal(1, b3.WeichtAb);
            Assert.Equal(katalogwert + 1234.0, b3.Zeilen[0].Projektwert.Value, 6);
            Assert.Equal(katalogwert, b3.Zeilen[0].Katalogwert.Value, 6);

            // (4) Ausführen setzt sie auf den Katalogwert; danach ist alles gleich.
            Abgleichergebnis e2 = Katalogkostenabgleich.Ausfuehren(idProjekt, idAnlage, WizardItemClass.WP_TYP, KOMPONENTE_WP);
            Assert.False(e2.Fehler);
            Assert.Equal(0, e2.Angelegt);
            Assert.Equal(1, e2.Gesetzt);
            Assert.Equal(katalogwert, KostenPositionCtrl.LiesBetrag(idPosition), 6);
            Assert.False(Katalogkostenabgleich.Vorschau(idProjekt, idAnlage, WizardItemClass.WP_TYP,
                                                        KOMPONENTE_WP).Handlungsbedarf);
        }

        [Fact]
        public void Gleich_unterscheidet_fehlend_von_null()
        {
            Assert.True(Katalogkostenabgleich.Gleich(null, null));
            Assert.False(Katalogkostenabgleich.Gleich(0, null));
            Assert.False(Katalogkostenabgleich.Gleich(null, 0));
            Assert.True(Katalogkostenabgleich.Gleich(100.0, 100.001));
            Assert.False(Katalogkostenabgleich.Gleich(100.0, 100.01));
            Assert.True(Katalogkostenabgleich.Absolut(DbWerte.BEMESSUNG_BETRAG));
            Assert.True(Katalogkostenabgleich.Absolut(""));
            Assert.False(Katalogkostenabgleich.Absolut(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET));
        }

        /// <summary>
        /// Eine Waermepumpen-Anlage der Arbeitskopie; mit <paramref name="ohneStamm"/> nur eine, deren
        /// Projektkopie ihren Ursprung (noch) nicht kennt.
        /// </summary>
        private static DataRow AnlageOhneUrsprung(bool ohneStamm = false)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT e.\"ID\", e.\"ID_Projekt\", e.\"ID_WP\" FROM \"Tab_Energieanlagen\" e " +
                "JOIN \"Tab_WP\" w ON w.\"ID\" = e.\"ID_WP\" WHERE e.\"ID_Type\" = ?" +
                (ohneStamm ? " AND w.\"ID_Stamm\" IS NULL" : "") + " ORDER BY e.\"ID\"",
                new DbParam("@t", WizardItemClass.WP_TYP));
            return dt == null || dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        private static long Positionen(int idProjekt)
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                   "SELECT COUNT(*) FROM \"Tab_ProjektWerte\" WHERE \"ProjektID\" = ?",
                   new DbParam("@p", idProjekt)), CultureInfo.InvariantCulture);

        private static int Ganz(DataRow r, string spalte)
            => r[spalte] == DBNull.Value ? 0 : Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);
    }
}
