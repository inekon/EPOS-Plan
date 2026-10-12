using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Der Arbeitspunkt am eingestellten Vorlauf (WP1a, Gerätesteckbrief): Normpunkt je Wärmepumpentyp, Auswertung auf
/// einer Stützstelle und zwischen zwei Vorläufen mit denselben Funktionen wie der Lauf, kein Arbeitspunkt außerhalb
/// der Stützstellen oder der Quellachse.
/// </summary>
public class KennlinienarbeitspunktTests
{
    /// <summary>Zwei Kennlinien (35 und 55 °C) über −10 … 10 °C, absteigend wie aus Tab_Kenndaten.</summary>
    private static SimulationWaermepumpe.Kennlinienwahl Wahl()
    {
        SimulationWaermepumpe._Kenndaten Kurve(int vorlauf, double copBei0, double leistungBei0) => new()
        {
            ID_WP = 1,
            Vorlauf = vorlauf,
            anz = 3,
            dat = new[]
            {
                new SimulationWaermepumpe._DAT { Temperatur = 10, COP = copBei0 + 1, Leistung = leistungBei0 + 2 },
                new SimulationWaermepumpe._DAT { Temperatur = 0, COP = copBei0, Leistung = leistungBei0 },
                new SimulationWaermepumpe._DAT { Temperatur = -10, COP = copBei0 - 1, Leistung = leistungBei0 - 2 },
            }
        };
        return new SimulationWaermepumpe.Kennlinienwahl
        {
            Kurven = new[] { Kurve(35, 4.0, 10.0), Kurve(55, 3.0, 9.0) },
            Stunden = new int[2],
            Interpolieren = true,
        };
    }

    [Theory]
    [InlineData("Luft-Wasser", 'A', 2)]
    [InlineData("Sole-Wasser", 'B', 0)]
    [InlineData("Wasser-Wasser", 'W', 10)]
    public void Der_Normpunkt_folgt_dem_Typ(string typ, char kennung, int temperatur)
    {
        Assert.Equal((kennung, temperatur), Kennlinienarbeitspunkt.Normpunkt(typ));
    }

    [Fact]
    public void Ohne_Typ_gibt_es_keinen_Normpunkt_und_keinen_Arbeitspunkt()
    {
        Assert.Null(Kennlinienarbeitspunkt.Normpunkt(""));
        Assert.Null(Kennlinienarbeitspunkt.Normpunkt(null));
        Assert.Null(Kennlinienarbeitspunkt.Rechnen(1, 35, ""));
        Assert.Null(Kennlinienarbeitspunkt.Rechnen(0, 35, "Sole-Wasser"));
    }

    [Fact]
    public void Auf_der_Stuetzstelle_rechnet_ihre_Kennlinie()
    {
        KennlinienArbeitspunkt a = Kennlinienarbeitspunkt.Auswerten(Wahl(), 35, 'B', 0);
        Assert.NotNull(a);
        Assert.Equal(10.0, a.Heizleistung, 9);
        Assert.Equal(4.0, a.Cop, 9);
        Assert.Equal("B0/W35", a.Normpunkt);

        // A2: innerhalb der Kennlinie linear zwischen 0 und 10 °C.
        KennlinienArbeitspunkt l = Kennlinienarbeitspunkt.Auswerten(Wahl(), 55, 'A', 2);
        Assert.Equal(9.4, l.Heizleistung, 9);
        Assert.Equal(3.2, l.Cop, 9);
    }

    [Fact]
    public void Zwischen_zwei_Vorlaeufen_wird_wie_im_Lauf_interpoliert()
    {
        KennlinienArbeitspunkt a = Kennlinienarbeitspunkt.Auswerten(Wahl(), 45, 'B', 0);
        double[] lauf = SimulationWaermepumpe.VorlaufGewichten(new double[] { 1, 4.0, 10.0, 2.5 },
                                                               new double[] { 1, 3.0, 9.0, 3.0 }, 0.5);
        Assert.Equal(lauf[2], a.Heizleistung, 9);
        Assert.Equal(lauf[1], a.Cop, 9);
        Assert.Equal("B0/W45", a.Normpunkt);
    }

    [Fact]
    public void Ausserhalb_der_Stuetzstellen_oder_der_Quellachse_gibt_es_keinen_Arbeitspunkt()
    {
        Assert.Null(Kennlinienarbeitspunkt.Auswerten(Wahl(), 65, 'B', 0));
        Assert.Null(Kennlinienarbeitspunkt.Auswerten(Wahl(), 30, 'B', 0));
        Assert.Null(Kennlinienarbeitspunkt.Auswerten(Wahl(), 35, 'W', 12));
        Assert.Null(Kennlinienarbeitspunkt.Auswerten(null, 35, 'B', 0));
    }
}
