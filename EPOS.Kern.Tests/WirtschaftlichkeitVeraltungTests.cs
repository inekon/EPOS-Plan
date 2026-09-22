using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderbefund vom 22.09.2026</b>: Die Statuszeile
    /// „Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand" erschien für
    /// Ergebniszeilen MIT Fehlgrund nie.
    ///
    /// <para><b>Die Lage im Bestand.</b> <c>WirtschaftlichkeitCtrl.ErgebnisAktuell</c>
    /// beantwortete zwei Fragen zugleich: „passt das Ergebnis zum Simulationslauf"
    /// UND „steht überhaupt eine Kennzahl" (<c>Fehlgrund == null</c>). Wer beides
    /// zusammen fragte — die Hülle der Wirtschaftlichkeitsseite tat das —, filterte
    /// Zeilen mit Fehlgrund doppelt heraus: Sie galten als nicht aktuell UND wurden
    /// wegen ihres Fehlgrunds übersprungen. Ausgerechnet bei den Zeilen, bei denen
    /// neu zu rechnen am nötigsten ist, blieb die Zeile also stumm.</para>
    ///
    /// <para>Die beiden Berichtsstellen, die wirklich BEIDES wissen müssen
    /// (<c>BausteineWirtschaftlichkeit</c>, <c>ExcelBerichtGenerator</c>), fragen
    /// unverändert beides ab — sie tragen ihren <c>Fehlgrund == null</c>-Test selbst.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WirtschaftlichkeitVeraltungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public WirtschaftlichkeitVeraltungTests(TestDatenbank db) { _db = db; }

        /// <summary>„Referenz BHKW-Kaskade" — führt ein gespeichertes Ergebnis.</summary>
        private const int PROJEKT = 1030;

        /// <summary>
        /// Eine Zeile MIT Fehlgrund, deren Lauf-Id zum gespeicherten Simulationslauf
        /// passt, ist AKTUELL. Vorher war sie es nie — und damit auch nie „veraltet",
        /// denn wer sie prüfte, schloss sie vorher aus.
        /// </summary>
        [Fact]
        public void Eine_Zeile_mit_Fehlgrund_passt_zum_Lauf_wenn_ihre_Lauf_Id_stimmt()
        {
            if (!_db.Vorhanden) return;

            int lauf = LaufId();
            Assert.True(lauf > 0);

            var ctrl = new WirtschaftlichkeitCtrl();

            Assert.True(ctrl.ErgebnisAktuell(Zeile(lauf, "Energiekosten nicht bestimmbar: …")));
            Assert.True(ctrl.ErgebnisAktuell(Zeile(lauf, null)));
        }

        /// <summary>
        /// Die Gegenprobe: Dieselbe Zeile mit einer FREMDEN Lauf-Id ist veraltet — und
        /// zwar gleich, ob sie einen Fehlgrund trägt oder nicht. Genau das ist die
        /// Frage, die diese Methode jetzt allein beantwortet.
        /// </summary>
        [Fact]
        public void Eine_fremde_Lauf_Id_ist_veraltet_mit_und_ohne_Fehlgrund()
        {
            if (!_db.Vorhanden) return;

            int fremd = LaufId() + 1;
            var ctrl = new WirtschaftlichkeitCtrl();

            Assert.False(ctrl.ErgebnisAktuell(Zeile(fremd, "Energiekosten nicht bestimmbar: …")));
            Assert.False(ctrl.ErgebnisAktuell(Zeile(fremd, null)));
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static WirtschaftlichkeitErgebnis Zeile(int idErgebnis, string fehlgrund)
            => new WirtschaftlichkeitErgebnis
            {
                IdProjekt = PROJEKT,
                IdErgebnis = idErgebnis,
                Fehlgrund = fehlgrund
            };

        /// <summary>Der Simulationslauf, zu dem ein Ergebnis passen muss — dieselbe
        /// Abfrage wie <c>WirtschaftlichkeitCtrl.LiesErgebnisId</c>.</summary>
        private static int LaufId()
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + ErgebnisCtrl.TAB_KOPF +
                " WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1",
                new DbParam("@p", PROJEKT));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }
    }
}
