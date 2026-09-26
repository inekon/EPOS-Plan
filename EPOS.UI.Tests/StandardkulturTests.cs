using System.Globalization;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Gegenprobe zur Standardkultur von <c>EPOS.UI.Tests</c> (Auftrag #529): Ohne Pinnung gilt
/// en-US, und die Ressourcen antworten englisch; eine <see cref="Kulturvorrichtung"/> schaltet
/// auf de-DE und stellt danach wieder auf en-US zurück. Fällt dieser Fall, fehlt oder wirkt
/// <c>StandardkulturEnUs</c> nicht — oder eine Klasse hat eine Kultur stehen lassen.
/// </summary>
public class StandardkulturTests
{
    private const string Englisch = "Hours of the year [h]";
    private const string Deutsch = "Jahresstunden [h]";

    [Fact]
    public void Ohne_Pinnung_gilt_en_US_und_die_Vorrichtung_stellt_darauf_zurueck()
    {
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
        Assert.Equal(Englisch, Resource.CHART_ACHSE_JAHRESSTUNDEN);

        using (new Kulturvorrichtung())
        {
            Assert.Equal("de-DE", CultureInfo.CurrentUICulture.Name);
            Assert.Equal(Deutsch, Resource.CHART_ACHSE_JAHRESSTUNDEN);
        }

        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        Assert.Equal(Englisch, Resource.CHART_ACHSE_JAHRESSTUNDEN);
    }
}
