using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// KA-E-24 je Wirt: eine angekreuzte Projektzeile und ein angekreuzter Katalogsatz nebeneinander — die Projektzeile
/// mit den Werten ihrer Projektkopie aus derselben Parameterliste (Gabe <c>VergleichsparameterProjekt</c>), die
/// Herkunft im Spaltenkopf.
/// </summary>
internal static class VergleichWirtHilfe
{
    /// <summary>Eine Parameterliste mit einer Zeile „Leistung [kW]".</summary>
    public static IReadOnlyList<Parameterwert> Werte(string wert)
        => new[] { new Parameterwert(new ParameterEintrag("Leistung", "Leistung", "kW",
                                                          new[] { Verwendung.Simulation }, "Test"), wert) };

    /// <summary>Kreuzt die erste Projektzeile und den ersten Katalogsatz an, vergleicht und prüft Köpfe und Werte.</summary>
    public static void ProjektzeileGegenKatalogsatz<T>(IRenderedComponent<T> cut, List<int> projektIds, string projektName)
        where T : IComponent
    {
        cut.Find(".epos-zweispalten-bereich--projekt").QuerySelectorAll("td .epos-wahlkaestchen")[0].Click();
        cut.Find(".epos-zweispalten-bereich--katalog").QuerySelectorAll("td .epos-kaestchenzelle input")[0].Change(true);

        var knopf = cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--vergleichen");
        Assert.Null(knopf.GetAttribute("aria-disabled"));
        knopf.Click();

        var koepfe = cut.FindAll(".epos-vergleich thead th").Skip(1).ToList();
        Assert.Equal(2, koepfe.Count);
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_HERKUNFT_PROJEKT, koepfe[0].QuerySelector(".epos-vergleich-herkunft")!.TextContent);
        Assert.EndsWith(projektName, koepfe[0].TextContent);
        Assert.Equal(Resource.AUSWAHL_VERGLEICH_HERKUNFT_KATALOG, koepfe[1].QuerySelector(".epos-vergleich-herkunft")!.TextContent);

        Assert.Single(projektIds);
        var zeile = cut.FindAll(".epos-vergleich tbody tr").Single(tr => tr.TextContent.Contains("Leistung [kW]"));
        var werte = zeile.QuerySelectorAll("td").Select(td => td.TextContent).ToList();
        Assert.Equal("P:" + projektIds[0], werte[0]);
        Assert.StartsWith("K:", werte[1]);
    }
}
