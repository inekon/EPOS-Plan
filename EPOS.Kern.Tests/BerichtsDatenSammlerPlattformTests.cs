using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der ZEUGE der Etappe E3, Schritt 4 (Befund P1): Der Rechenaufruf des
    /// Berichts läuft im Kern und braucht keine Schale.
    ///
    /// <para><b>Was hier bewiesen wird.</b> <see cref="BerichtsDatenSammler"/> lag
    /// bis E3/4 in der Windows-Anwendung; damit hatte
    /// <c>WirtschaftlichkeitCtrl.Berechne(daten, p)</c> seine einzige
    /// Produktions-Aufrufstelle in der Schale, und iOS konnte keine
    /// Wirtschaftlichkeit rechnen. Dieser Prüfstand fährt dieselbe Kette —
    /// Status ermitteln, sammeln, rechnen — ohne <c>Program.Main</c>, ohne
    /// belegtes <c>Dienste.*</c> und ohne Fenster.</para>
    ///
    /// <para>Die Fälle arbeiten auf der ARBEITSKOPIE der Testdatenbank
    /// (<see cref="TestDatenbank"/>) und rechnen NICHT neu
    /// (<c>neuRechnen: false</c>): Geprüft wird die Erreichbarkeit des Weges,
    /// nicht das Ergebnis — die Zahlen hält <c>WirtschaftlichkeitAnkerTests</c>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtsDatenSammlerPlattformTests
    {
        /// <summary>Ein Referenzprojekt mit gespeichertem Simulationsergebnis.</summary>
        private const int PROJEKT_BHKW = 1030;

        private static BerichtsDatenSammler.VariantenStatus Stammzeile()
        {
            List<BerichtsDatenSammler.VariantenStatus> stand =
                BerichtsDatenSammler.ErmittleStatus(PROJEKT_BHKW, "");
            Assert.NotEmpty(stand);
            return stand.First(s => s.IstStamm);
        }

        [Fact]
        public void Der_Status_der_Gruppe_kommt_ohne_Windows_Dienst()
        {
            BerichtsDatenSammler.VariantenStatus stamm = Stammzeile();

            // Der Stamm steht in der Gruppe, und sein Simulationsstand ist gelesen
            // (Zeitstempel ODER die benannte Lücke) - beides über DataRepository,
            // also ohne jeden Plattformdienst.
            Assert.Equal(PROJEKT_BHKW, stamm.IdProjekt);
            Assert.False(string.IsNullOrEmpty(stamm.SimStandText));
        }

        [Fact]
        public void Sammeln_und_Rechnen_laufen_im_Kern()
        {
            BerichtsDatenSammler.VariantenStatus stamm = Stammzeile();

            BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                PROJEKT_BHKW, stamm.Projektname, new List<int>(),
                false /* nicht neu simulieren */, false /* ohne Zeitreihen */,
                null, CancellationToken.None);

            Assert.NotNull(daten);
            Assert.NotEmpty(daten.Varianten);

            // Der EINE Rechenaufruf des Berichts - bis E3/4 nur aus der Schale
            // erreichbar (Befund P1), jetzt von beiden Plattformen.
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_BHKW);
            List<WirtschaftlichkeitErgebnis> ergebnisse = ctrl.Berechne(daten, p);

            Assert.NotNull(ergebnisse);
            Assert.NotEmpty(ergebnisse);
        }

        [Fact]
        public void Die_Brennstoffmengen_sind_die_ehemalige_Naht_und_plattformfrei()
        {
            // EnergieMengen.BaueBrennstoffmengen war die einzige Naht des Sammlers
            // (Befund #380). Sie führt keine Windows-Zeile und steht deshalb seit
            // E3/4 neben ihm im Kern.
            DataTable tab = EnergieMengen.BaueBrennstoffmengen(PROJEKT_BHKW);

            Assert.NotNull(tab);
            Assert.Equal(3, tab.Columns.Count);
        }
    }
}
