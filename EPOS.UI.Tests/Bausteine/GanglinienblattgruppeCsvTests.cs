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
/// <b>„CSV…“ im Stammblatt der Ganglinienverwaltungen</b> (CSV-3): Die Gruppe nimmt die Naht ihrer
/// Verwaltung aus der Kaskade und hängt den Satznamen an den Bildtitel — die Datei nennt den
/// gezeigten Satz. Der Jahresverlauf geht mit 8 760 Stundenwerten im Raster Stunde hinaus.
/// </summary>
public class GanglinienblattgruppeCsvTests : EposBunitContext
{
    public GanglinienblattgruppeCsvTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static Zeichenmodell Jahr()
    {
        var werte = Enumerable.Range(0, 8760).Select(i => (double)(i % 24)).ToArray();
        return ChartRenderer.JahresgangModell("Ganglinie",
            new[] { new ChartRenderer.Reihe("Leistung", werte, ChartRenderer.C_QUELLTEMPERATUR) },
            "Monat", "Leistung [kW]");
    }

    [Fact]
    public void Die_Gruppe_schreibt_den_Satz_ueber_die_Naht_der_Verwaltung()
    {
        var exporte = new List<(Zeichenmodell M, string T, Zeitraster R)>();
        var naht = new Ganglinienexport((m, t, r) => { exporte.Add((m, t, r)); return Task.CompletedTask; });
        Zeichenmodell modell = Jahr();

        var cut = Render<CascadingValue<Ganglinienexport>>(p => p
            .Add(x => x.Value, naht)
            .AddChildContent<Ganglinienblattgruppe>(g => g
                .Add(x => x.Modell, modell)
                .Add(x => x.Name, "Werk Nord")
                .Add(x => x.Titel, "Ganglinie")));

        cut.Find("div.epos-diagramm-leiste button.epos-diagramm-csv").Click();

        var (m, t, r) = Assert.Single(exporte);
        Assert.Same(modell, m);
        Assert.Equal("Ganglinie Werk Nord", t);
        Assert.Equal(Zeitraster.Stunde, r);
        Assert.Equal(8760, ZeitreihenCsv.AusModell(m)[0].Werte.Length);
    }

    [Fact]
    public void Ohne_Naht_traegt_die_Gruppe_keinen_Knopf()
    {
        var cut = Render<Ganglinienblattgruppe>(g => g.Add(x => x.Modell, Jahr()).Add(x => x.Name, "Werk Nord"));
        Assert.Empty(cut.FindAll("button.epos-diagramm-csv"));
    }
}
