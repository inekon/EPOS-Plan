using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

public sealed class SpeicherFlottenBedienvorgabenTests
{
    [Fact]
    public void Unvollstaendiger_Altstand_bekommt_sichtbare_Wiederholung_und_effektiven_Preis()
    {
        var a = new SpeicherAuslegungKonfiguration { Flotte = new() };
        a.Flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = 20;
        SpeicherFlottenStudieCtrl.BedienvorgabenErgaenzen(a, .31746);
        Assert.True(a.Flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(20, a.Flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);
        Assert.Equal(.31746, a.Flotte.Optionen.EnergieAusgleichEuroProKWh);
    }

    [Fact]
    public void Spaeter_gewaehlte_Einzeljahre_und_eigener_Ausgleich_bleiben_erhalten()
    {
        var a = new SpeicherAuslegungKonfiguration { Flotte = new() };
        SpeicherFlottenStudieCtrl.BedienvorgabenErgaenzen(a, .3);
        a.Flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen = false;
        a.Flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = 1;
        a.Flotte.Optionen.EnergieAusgleichEuroProKWh = .15;
        SpeicherFlottenStudieCtrl.BedienvorgabenErgaenzen(a, .5);
        Assert.False(a.Flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(1, a.Flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);
        Assert.Equal(.15, a.Flotte.Optionen.EnergieAusgleichEuroProKWh);
    }

    [Fact]
    public void Echte_Jahresdaten_werden_nicht_in_ein_Wiederholungsszenario_umgewandelt()
    {
        var a = new SpeicherAuslegungKonfiguration { Flotte = new(),
            FlottenProjektjahre = new() { new FlottenProjektjahr { Jahr = 2026 }, new FlottenProjektjahr { Jahr = 2027 } } };
        a.Flotte.Optionen.EnergieAusgleichEuroProKWh = .1;
        SpeicherFlottenStudieCtrl.BedienvorgabenErgaenzen(a, .3);
        Assert.False(a.Flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(2, a.Flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);
        Assert.Equal(.1, a.Flotte.Optionen.EnergieAusgleichEuroProKWh);
    }
}
