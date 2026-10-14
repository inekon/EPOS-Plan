using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Abbildung des Solarkreises</b> zwischen Anlagenmodell und Dialogzeile
/// (<see cref="Kollektorfeldabbildung"/>, Welle M2): leer bleibt leer, die Arbeitstemperatur
/// ist ein Ja/Nein der Maske und in der Ablage „speicher", „fest" oder — nie gepflegt — NULL.
/// </summary>
public sealed class KollektorfeldabbildungTests
{
    [Fact]
    public void Hin_und_zurueck_bleibt_leer_leer_und_der_Weg_ein_Persistenzwert()
    {
        var m = new WErzeugerModel
        {
            Pumpenleistung_W = 60, Solarkreisverluste_Prozent = null, Uebertrager_Graedigkeit_K = 10,
            Kollektor_Spreizung_K = null, Arbeitstemperatur_Weg = "speicher"
        };
        var z = new ErzeugerZeile();
        Kollektorfeldabbildung.InZeile(m, z);

        Assert.Equal(60.0, z.SolarPumpenleistungW);
        Assert.Null(z.SolarkreisverlusteProzent);
        Assert.Equal(10.0, z.SolarGraedigkeitK);
        Assert.Null(z.SolarSpreizungK);
        Assert.True(z.SolarArbeitstemperaturAusSpeicher);

        var zurueck = new WErzeugerModel();
        Kollektorfeldabbildung.InModell(z, zurueck);
        Assert.Equal(60.0, zurueck.Pumpenleistung_W);
        Assert.Null(zurueck.Solarkreisverluste_Prozent);
        Assert.Equal("speicher", zurueck.Arbeitstemperatur_Weg);

        z.SolarArbeitstemperaturAusSpeicher = false;
        Kollektorfeldabbildung.InModell(z, zurueck);
        Assert.Equal("fest", zurueck.Arbeitstemperatur_Weg);       // war gepflegt

        var nie = new WErzeugerModel();
        Kollektorfeldabbildung.InModell(new ErzeugerZeile(), nie);
        Assert.Null(nie.Arbeitstemperatur_Weg);                     // nie gepflegt bleibt NULL
    }
}
