using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// DER LOESCHSCHUTZ DES KOSTENFAKTOR-KATALOGS (Auftrag #302).
    ///
    /// <para><b>Der Befund.</b> Die Seite „Berichte &amp; Kosten → Kosten", Knopf
    /// „Kostenverwaltung oeffnen…", Ueberlagerung „Administration Kostenfaktoren",
    /// Knopf „Loeschen" fuehrte auf
    /// <c>DELETE FROM Tab_Kostenfaktor WHERE StammID = ? AND IsMainComponent = False</c>.
    /// Der Fremdschluessel <c>Tab_ProjektWerte.StammID → Tab_Kostenfaktor(StammID)</c>
    /// trug dabei <c>ON DELETE CASCADE</c>, und <c>PRAGMA foreign_keys = ON</c> steht je
    /// Verbindung. EIN Katalogeintrag zu loeschen riss damit JEDE Projektposition
    /// derselben <c>StammID</c> mit - in allen Projekten und allen Gewerken. Die
    /// Rueckfrage nannte nur „Kostenfaktor '{0}' wirklich loeschen?".</para>
    ///
    /// <para><b>Was diese Faelle festnageln.</b> Ein Katalogeintrag, auf den
    /// Projektpositionen (<c>Tab_ProjektWerte</c>) oder Vorlagenpositionen
    /// (<c>Tab_KostenVorlagePosition</c>) verweisen, wird NICHT geloescht; der Grund wird
    /// benannt statt still verweigert - dasselbe Muster wie
    /// <c>EnergietraegerKatalogCtrl.Loeschen</c>. Ein UNBENUTZTER Eintrag bleibt
    /// loeschbar, und der Bestandsschutz der Hauptkomponenten
    /// (<c>IsMainComponent = 1</c>) bleibt, wie er war.</para>
    ///
    /// <para><b>Die Zahlen stammen aus der Testdatenbank</b>
    /// (<c>Referenzlaeufe/Kenndaten_Test.sqlite</c>): 75 Katalogeintraege, davon 10
    /// Hauptkomponenten; 175 Zeilen in <c>Tab_ProjektWerte</c>. Sie stehen hier als
    /// gelesene Groessen, nicht als feste Erwartung - fest ist nur, dass der
    /// abgewiesene Loeschversuch NICHTS aendert.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenfaktorCtrlTests
    {
        /// <summary>„Waermepumpe (Aggregat)" — ein Kostenfaktor mit Projektpositionen.</summary>
        private const int WaermepumpeAggregat = 108;

        /// <summary>„Planung / Baunebenkosten" — quer ueber mehrere Gewerke benutzt.</summary>
        private const int PlanungBaunebenkosten = 114;

        /// <summary>„Pufferspeicher" — eine Hauptkomponente (<c>IsMainComponent = 1</c>).</summary>
        private const int Pufferspeicher = 81;

        // =============================================================================
        //  Fall 1 und 2 - der Befund selbst
        // =============================================================================

        /// <summary>
        /// Ein benutzter Kostenfaktor wird nicht geloescht, und die Kaskade bleibt aus:
        /// <c>Tab_ProjektWerte</c> behaelt jede Zeile, die drei Projekte behalten ihre
        /// Waermepumpen-Investition von 13.000,00 €.
        /// </summary>
        [Fact]
        public void Ein_benutzter_Kostenfaktor_wird_nicht_geloescht_und_reisst_nichts_mit()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            long positionenVorher = Positionen();
            long eigeneVorher = PositionenZu(WaermepumpeAggregat);
            long projekteVorher = ProjekteZu(WaermepumpeAggregat);
            long dreizehntausend = MitBetrag(WaermepumpeAggregat, 13000.0);

            Assert.True(eigeneVorher > 0, "Der Fall braucht einen Kostenfaktor MIT Projektpositionen.");
            Assert.Equal(3L, dreizehntausend);

            string grund;
            bool ok = KostenfaktorCtrl.Loeschen(WaermepumpeAggregat, out grund);

            Assert.False(ok);
            Assert.Contains(eigeneVorher.ToString(CultureInfo.InvariantCulture), grund, StringComparison.Ordinal);
            Assert.Contains(projekteVorher.ToString(CultureInfo.InvariantCulture), grund, StringComparison.Ordinal);

            // Nichts angefasst: der Katalogeintrag steht, seine Positionen stehen,
            // und die Gesamtzahl der Projektpositionen ist unveraendert.
            Assert.True(Vorhanden(WaermepumpeAggregat), "Der Katalogeintrag ist verschwunden.");
            Assert.Equal(positionenVorher, Positionen());
            Assert.Equal(eigeneVorher, PositionenZu(WaermepumpeAggregat));
            Assert.Equal(3L, MitBetrag(WaermepumpeAggregat, 13000.0));
        }

        /// <summary>
        /// Derselbe Schutz fuer einen Kostenfaktor, der QUER ueber mehrere Gewerke und
        /// Projekte benutzt wird — „Planung / Baunebenkosten".
        /// </summary>
        [Fact]
        public void Ein_gewerkeuebergreifender_Kostenfaktor_wird_nicht_geloescht()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            long positionenVorher = Positionen();
            long eigeneVorher = PositionenZu(PlanungBaunebenkosten);
            long projekteVorher = ProjekteZu(PlanungBaunebenkosten);
            Assert.True(eigeneVorher > projekteVorher,
                        "Der Fall braucht einen Kostenfaktor, der in einem Projekt MEHRFACH steht.");

            string grund;
            bool ok = KostenfaktorCtrl.Loeschen(PlanungBaunebenkosten, out grund);

            Assert.False(ok);
            Assert.Contains(eigeneVorher.ToString(CultureInfo.InvariantCulture), grund, StringComparison.Ordinal);
            Assert.Contains(projekteVorher.ToString(CultureInfo.InvariantCulture), grund, StringComparison.Ordinal);

            Assert.True(Vorhanden(PlanungBaunebenkosten), "Der Katalogeintrag ist verschwunden.");
            Assert.Equal(positionenVorher, Positionen());
            Assert.Equal(eigeneVorher, PositionenZu(PlanungBaunebenkosten));
        }

        // =============================================================================
        //  Fall 3 und 4 - was NICHT gesperrt wird und was Bestand bleibt
        // =============================================================================

        /// <summary>
        /// Ein frisch angelegter, von niemandem benutzter Kostenfaktor bleibt loeschbar —
        /// der Schutz sperrt die Pflege nicht.
        /// </summary>
        [Fact]
        public void Ein_unbenutzter_Kostenfaktor_bleibt_loeschbar()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            long positionenVorher = Positionen();

            int neueId = KostenfaktorCtrl.Neu("Prüfposten Löschschutz");
            Assert.True(neueId > 0, "Der Kostenfaktor liess sich nicht anlegen.");
            Assert.Equal(0L, PositionenZu(neueId));

            string grund;
            bool ok = KostenfaktorCtrl.Loeschen(neueId, out grund);

            Assert.True(ok, "Ein unbenutzter Kostenfaktor liess sich nicht loeschen: " + grund);
            Assert.Equal("", grund);
            Assert.False(Vorhanden(neueId), "Der Katalogeintrag steht nach dem Loeschen noch.");
            Assert.Equal(positionenVorher, Positionen());
        }

        /// <summary>
        /// BESTAND: Eine Hauptkomponente (<c>IsMainComponent = 1</c>) bleibt geschuetzt.
        /// Der Filter stammt aus Befund B4 (11.08.2026) und wird durch den neuen Schutz
        /// weder ersetzt noch aufgeweicht.
        /// </summary>
        [Fact]
        public void Eine_Hauptkomponente_bleibt_geschuetzt()
        {
            using var kopie = new TestDatenbank();
            Assert.True(kopie.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT IsMainComponent FROM Tab_Kostenfaktor WHERE StammID = ?",
                new DbParam("@sid", Pufferspeicher))));

            long positionenVorher = Positionen();

            string grund;
            bool ok = KostenfaktorCtrl.Loeschen(Pufferspeicher, out grund);

            Assert.False(ok);
            Assert.True(Vorhanden(Pufferspeicher), "Die Hauptkomponente ist verschwunden.");
            Assert.Equal(positionenVorher, Positionen());
        }

        // =============================================================================
        //  Handreichungen
        // =============================================================================

        private static long Positionen()
        {
            return Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte");
        }

        private static long PositionenZu(int stammId)
        {
            return Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE StammID = ?",
                        new DbParam("@sid", stammId));
        }

        private static long ProjekteZu(int stammId)
        {
            return Zahl("SELECT COUNT(DISTINCT ProjektID) FROM Tab_ProjektWerte WHERE StammID = ?",
                        new DbParam("@sid", stammId));
        }

        /// <summary>Wie viele Projektpositionen dieses Kostenfaktors tragen genau diesen Betrag?</summary>
        private static long MitBetrag(int stammId, double betrag)
        {
            return Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE StammID = ? AND EingegebenerWert = ?",
                        new DbParam("@sid", stammId), new DbParam("@wert", betrag));
        }

        private static bool Vorhanden(int stammId)
        {
            return Zahl("SELECT COUNT(*) FROM Tab_Kostenfaktor WHERE StammID = ?",
                        new DbParam("@sid", stammId)) == 1;
        }

        private static long Zahl(string sql, params DbParam[] parameter)
        {
            object o = DataRepository.ExecuteScalar(sql, parameter);
            return (o == null || o == DBNull.Value)
                ? -1
                : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
