using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// EnergietraegerWahl (ET-5, 08.09.2026): Gruppe und Art in der Gliederung des Katalogs -
/// zwei Auswahlfelder, ein Wert (die Traeger-Id).
/// </summary>
public class EnergietraegerWahlTests : BunitContext
{
    private static IReadOnlyList<EnergietraegerWahl.Eintrag> Katalog() => new[]
    {
        new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
        new EnergietraegerWahl.Eintrag(12, "Gas", "Biogas"),
        new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
        new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2"),
        new EnergietraegerWahl.Eintrag(31, "", "Sonstiges")
    };

    [Fact]
    public void Die_Auswahl_bestimmt_Gruppe_und_Art()
    {
        var cut = Render<EnergietraegerWahl>(p => p
            .Add(x => x.Eintraege, Katalog())
            .Add(x => x.Auswahl, 58));

        var selects = cut.FindAll("select");
        Assert.Equal(2, selects.Count);

        // Die Gruppen: jede einmal, in Reihenfolge der Eintraege; ohne group_code "Sonstige".
        var gruppen = selects[0].QuerySelectorAll("option");
        Assert.Equal(new[] { "Gas", "Strom", "Sonstige" }, new[] { gruppen[0].TextContent, gruppen[1].TextContent, gruppen[2].TextContent });
        Assert.True(gruppen[1].HasAttribute("selected"));

        // Die Arten: nur die der Gruppe Strom, die gewaehlte markiert.
        var arten = selects[1].QuerySelectorAll("option");
        Assert.Equal(2, arten.Length);
        Assert.Contains("Elektrische Energie 2", selects[1].InnerHtml);
        Assert.DoesNotContain("Erdgas E", selects[1].InnerHtml);
        Assert.True(arten[1].HasAttribute("selected"));
    }

    [Fact]
    public void Ein_Gruppenwechsel_waehlt_den_ersten_Traeger_der_Gruppe()
    {
        int? gemeldet = null;
        var cut = Render<EnergietraegerWahl>(p => p
            .Add(x => x.Eintraege, Katalog())
            .Add(x => x.Auswahl, 58)
            .Add(x => x.AuswahlChanged, (int? id) => gemeldet = id));

        cut.FindAll("select")[0].Change("0");   // Gas

        Assert.Equal(11, gemeldet);
    }

    [Fact]
    public void Ein_Artwechsel_meldet_die_Traeger_Id()
    {
        int? gemeldet = null;
        var cut = Render<EnergietraegerWahl>(p => p
            .Add(x => x.Eintraege, Katalog())
            .Add(x => x.Auswahl, 58)
            .Add(x => x.AuswahlChanged, (int? id) => gemeldet = id));

        cut.FindAll("select")[1].Change("60");

        Assert.Equal(60, gemeldet);
    }

    [Fact]
    public void Aktiv_false_sperrt_beide_Felder()
    {
        var cut = Render<EnergietraegerWahl>(p => p
            .Add(x => x.Eintraege, Katalog())
            .Add(x => x.Auswahl, 11)
            .Add(x => x.Aktiv, false));

        Assert.All(cut.FindAll("select"), s => Assert.True(s.HasAttribute("disabled")));
    }
}
