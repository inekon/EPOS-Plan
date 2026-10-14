using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// K6d (Katalog v23): die Deckungszahl der Übersicht trägt mit Split-Zeile die Vorlagenmarke der Hauptzahl
/// <c>kaelte.deckungsgrad_gesamt</c> — auch ohne Kälteerzeuger; ohne Split bleibt die Marke des Kühlkanals.
/// </summary>
public sealed class UebersichtReiterKaelteDezentralTests
{
    private static SimulationErgebnisDaten Daten(bool mitDezentral, bool mitErzeuger)
    {
        var k = new KaelteDaten { KaeltebedarfMwh = 100.0, MitDezentral = mitDezentral };
        if (mitErzeuger)
            k.Erzeuger = new[] { new KaelteerzeugerAnzeige("WP 1", 12, 60.0, 12.0, 5.0, 12.0, "") };
        var d = new SimulationErgebnisDaten();
        d.Bedarf.Kaelte = k;
        return d;
    }

    [Theory]
    [InlineData(true, "stamm.kennzahl.kaelte.deckungsgrad_gesamt")]
    [InlineData(false, "stand.kennzahl.kaelte.deckungsgrad_gesamt")]
    public void Mit_Split_traegt_die_Deckungszahl_die_Marke_der_Hauptzahl(bool stamm, string erwartet)
    {
        foreach (bool erzeuger in new[] { true, false })
        {
            SimulationErgebnisDaten d = Daten(true, erzeuger);
            SimulationErgebnisHuelle.Vorlagenfelder(d, stamm);
            Assert.Equal(erwartet, d.Bedarf.Kaelte!.DeckungFeld);
            Assert.Equal(Vorlagenfeldstufe.Aehnlich, d.Bedarf.Kaelte.DeckungStufe);
            Assert.NotEmpty(EPOS.UI.Dienste.Vorlagenfeldorte.Finde(erwartet));
        }
    }

    [Fact]
    public void Ohne_Split_bleibt_die_Marke_des_Kuehlkanals()
    {
        SimulationErgebnisDaten mit = Daten(false, true);
        SimulationErgebnisHuelle.Vorlagenfelder(mit, true);
        Assert.Equal("stamm.kennzahl.kaelte.deckungsgrad", mit.Bedarf.Kaelte!.DeckungFeld);

        SimulationErgebnisDaten ohne = Daten(false, false);
        SimulationErgebnisHuelle.Vorlagenfelder(ohne, true);
        Assert.Equal("", ohne.Bedarf.Kaelte!.DeckungFeld);
        Assert.Equal(Vorlagenfeldstufe.Entspricht, ohne.Bedarf.Kaelte.DeckungStufe);
    }
}
