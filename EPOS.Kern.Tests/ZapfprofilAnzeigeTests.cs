using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>θ_Anzeige und Schwelle der Stundenzählung im Eingang</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.0, 4.6, N9 (h); Stufe Z4): die Laufangabe des Dialogs geht vor, sonst
    /// die Einstellung (<c>Zapfprofil.Anzeigetemperatur</c>, <c>Zapfprofil.Stundenschwelle</c>),
    /// sonst keine — eine ungültige Einstellung benannt verworfen. Auf der Arbeitskopie der
    /// Testdatenbank (Projekt 1006); ohne sie schweigt der Fall. Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilAnzeigeTests
    {
        private const int PROJEKT = 1006;

        [Fact]
        public void Laufangabe_vor_Einstellung_vor_keiner_Anzeige()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                var einstellungen = new FluechtigeEinstellungen();
                Dienste.Einstellungen = einstellungen;
                ZapfprofilStand stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null);
                bool[] we = new bool[365];

                Zapfprofileingang ohne = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Null(ohne.AnzeigetemperaturC);
                Assert.Null(ohne.SchwelleKw);
                Assert.Empty(ohne.Vorhinweise);

                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_ANZEIGETEMPERATUR, "45.5");
                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_STUNDENSCHWELLE, "7");
                Zapfprofileingang mitEinstellung = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Equal(45.5, mitEinstellung.AnzeigetemperaturC);
                Assert.Equal(7.0, mitEinstellung.SchwelleKw);

                Zapfprofileingang mitLauf = ZapfprofilCtrl.Eingang(PROJEKT, stand with { Anzeige = new ZapfAnzeige(40.0, null) }, 0, we);
                Assert.Equal(40.0, mitLauf.AnzeigetemperaturC);
                Assert.Equal(7.0, mitLauf.SchwelleKw);

                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_STUNDENSCHWELLE, "viel");
                Zapfprofileingang ungueltig = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Null(ungueltig.SchwelleKw);
                ZapfHinweis h = Assert.Single(ungueltig.Vorhinweise);
                Assert.Equal(ZapfprofilCtrl.HINWEIS_EINSTELLUNG_UNGUELTIG, h.Code);
                Assert.Equal(new object[] { ZapfprofilCtrl.EINSTELLUNG_STUNDENSCHWELLE, "viel" }, h.Satz.Werte.ToArray());
            }
            finally { Dienste.Einstellungen = vorher; }
        }
    }
}
