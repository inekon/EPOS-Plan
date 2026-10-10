using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>„CSV…“ am Wochenprofil</b> (CSV-3): Das Vorschaubild des Wochenrasters nimmt die Naht seines
/// Wirts aus der Kaskade (Gebäude, Gebäudekatalog, Bedarfsprofile) und schreibt die 168
/// Wochenstunden im Raster Wochenstunde.
/// </summary>
public class WochenrasterCsvTests : EposBunitContext
{
    public WochenrasterCsvTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    [Fact]
    public void Das_Wochenbild_schreibt_168_Wochenstunden()
    {
        var exporte = new List<(Zeichenmodell M, Zeitraster R)>();
        var naht = new Ganglinienexport((m, t, r) => { exporte.Add((m, r)); return Task.CompletedTask; });
        double[] woche = Enumerable.Range(0, 168).Select(i => i % 24 < 6 ? 17.0 : 21.0).ToArray();

        var cut = Render<CascadingValue<Ganglinienexport>>(p => p
            .Add(x => x.Value, naht)
            .AddChildContent<Wochenraster>(w => w
                .Add(x => x.Wert, woche)
                .Add(x => x.Einheit, "°C")
                .Add(x => x.Vorschau, werte => ChartRenderer.KostenprofilModell("Woche", werte, "°C", "Monat"))));

        cut.WaitForAssertion(() => cut.Find("div.epos-diagramm-leiste button.epos-diagramm-csv"));
        cut.Find("div.epos-diagramm-leiste button.epos-diagramm-csv").Click();

        var (m, r) = Assert.Single(exporte);
        Assert.Equal(Zeitraster.Wochenstunde, r);
        Assert.All(ZeitreihenCsv.AusModell(m), s => Assert.Equal(168, s.Werte.Length));
    }
}
