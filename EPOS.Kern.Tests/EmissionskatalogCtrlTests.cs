using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// EMK‑B‑1 (Windows-Abnahme 08.09.2026): Mit Trägerkontext liefert der Katalog NUR die Werte
    /// des Trägers — die trägerlosen Vorlagen nur als Rückfall und im Verwaltungsmodus.
    /// </summary>
    [Collection("Testdatenbank")]
    public class EmissionskatalogCtrlTests
    {
        private const int CO2 = 1;
        private const int STROM = 60;          // "Elektrische Energie" der Auslieferung

        [Fact]
        public void Mit_Traeger_kommen_nur_seine_Werte_der_geltende_zuerst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<EmissionswertModel> werte = EmissionskatalogCtrl.Werte(CO2, STROM);

            Assert.NotEmpty(werte);
            Assert.All(werte, w => Assert.Equal(STROM, w.CarrierId));
            // Der geltende Wert steht oben - hinter ihm kein weiterer geltender.
            bool nachGeltendem = false;
            foreach (EmissionswertModel w in werte)
            {
                if (!w.IstAktiv) nachGeltendem = true;
                else Assert.False(nachGeltendem, "geltender Wert hinter einem nicht geltenden");
            }
        }

        [Fact]
        public void Ohne_Traeger_kommen_nur_die_Vorlagen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<EmissionswertModel> vorlagen = EmissionskatalogCtrl.Werte(CO2, 0);

            Assert.NotEmpty(vorlagen);
            Assert.All(vorlagen, w => Assert.Null(w.CarrierId));
        }

        [Fact]
        public void Ein_Traeger_ohne_eigene_Werte_bekommt_die_Vorlagen_als_Rueckfall()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<EmissionswertModel> vorlagen = EmissionskatalogCtrl.Werte(CO2, 0);
            List<EmissionswertModel> rueckfall = EmissionskatalogCtrl.Werte(CO2, 999999);

            Assert.Equal(vorlagen.Count, rueckfall.Count);
            Assert.All(rueckfall, w => Assert.Null(w.CarrierId));
        }
    }
}
