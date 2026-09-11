using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenErgebnisBetriebTests : BunitContext
{
    [Fact]
    public void Ergebnis_erklaert_den_berechneten_Betrieb_und_nicht_erreichten_Peak()
    {
        var e = Ergebnis();
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, e));
        var text = string.Join(" ", cut.FindAll("p").Select(x => x.TextContent));
        Assert.Contains("Lastspitzenkappung", text);
        Assert.Contains("Kaskade", text);
        Assert.Contains("789,36".Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), text);
        Assert.Contains(cut.FindAll("*"), x => x.TextContent.Contains("Peak-Ziel wurde nicht erreicht"));
        Assert.Contains("Davon PV-Einspeisung", cut.Markup);
        Assert.Contains("Davon BHKW-Einspeisung", cut.Markup);
        Assert.Contains("Davon Batterie-Einspeisung", cut.Markup);
    }

    [Fact]
    public void Geaenderte_Eingaben_werden_auch_in_der_gemeinsamen_Ergebnisansicht_markiert()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p
            .Add(x => x.Ergebnis, Ergebnis()).Add(x => x.Veraltet, true));
        Assert.Contains(cut.FindAll("*"), x => x.TextContent.Contains("Diese Ergebnisse gehören zum vorherigen Stand"));
    }

    private static SpeicherFlottenErgebnis Ergebnis() => new()
    {
        Konfiguration = new FlottenStudieKonfiguration { Optionen = new()
            { Betriebsziel = FlottenBetriebsziel.PeakShaving, Verteilung = FlottenVerteilung.Kaskade,
              WirtschaftlicherPeakZielwertKw = 50 } },
        Studie = new FlottenStudienErgebnis { Variante = new() { Zulaessig = true, MaximalerNetzbezugKw = 789.36 } }
    };
}
