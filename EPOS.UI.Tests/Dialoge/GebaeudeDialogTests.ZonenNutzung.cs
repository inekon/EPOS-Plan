using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// MA-2: Einstieg in das Blatt „Nutzung und Kalender“ aus dem Ampelblatt der Modellprüfung — „Bearbeiten…“ an Gruppe 3 öffnet
/// das Blatt für das geprüfte Gebäude über dem Ampelblatt; ohne Delegat kein Knopf.
/// </summary>
public partial class GebaeudeDialogTests
{
    [Fact]
    public void MA2_Bearbeiten_an_Gruppe_3_oeffnet_Nutzung_und_Kalender()
    {
        var geoeffnet = new List<string>();
        var cut = MitModellpruefung(GespeicherteZeilen(), geoeffnet);
        Pruefknopf(cut)!.Click();
        Assert.Empty(cut.FindAll("button.epos-modellpruefung-bearbeiten"));

        var gefragt = new List<string>();
        cut.Render(p => p.Add(x => x.ZonenNutzungGaben, z => { gefragt.Add(z.Name); return new ZonenNutzungProbeweg().Weg(); }));
        var knopf = cut.Find(".epos-modellpruefung-gruppe[data-gruppe='3'] button.epos-modellpruefung-bearbeiten");
        Assert.Equal(R.MPB_BTN_BEARBEITEN, knopf.TextContent.Trim());
        Assert.Single(cut.FindAll("button.epos-modellpruefung-bearbeiten"));
        knopf.Click();
        Assert.Equal(new[] { "Haus A" }, gefragt);
        Assert.True(cut.Instance.ZonenNutzungOffen);
        Assert.Equal(3, cut.FindAll("table.epos-zonennutzung-tabelle tbody tr").Count);
        Assert.Equal(R.ZNB_TITEL, cut.FindAll(".epos-ueberlagerung-titel").Last().TextContent);
    }
}
