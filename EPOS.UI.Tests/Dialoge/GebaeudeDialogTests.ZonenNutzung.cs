using AngleSharp.Dom;
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

    /// <summary>
    /// N1: Die Leiste im Reiter „Zonen“ des Editors öffnet das Ampelblatt auf der geklickten Gruppe (ihre Befunde offen); nicht
    /// im Assistenten.
    /// </summary>
    [Fact]
    public void N1_Ampelleiste_im_Editor_oeffnet_das_Ampelblatt_auf_der_Gruppe()
    {
        var geoeffnet = new List<string>();
        var cut = MitModellpruefung(GespeicherteZeilen(), geoeffnet);
        cut.Render(p => p.Add(x => x.ProjektGaben, _ => new Dictionary<string, object> { ["StartReiter"] = GebaeudeKatalogDialog.REITER_ZONEN }));
        cut.FindAll("td.epos-spalte-stift button").First().Click();
        Assert.Equal(5, cut.FindAll(".epos-pruefleiste-gruppe").Count);
        Assert.False(cut.Instance.ModellpruefungOffen);

        cut.Find(".epos-pruefleiste-gruppe[data-gruppe='2']").Click();
        Assert.True(cut.Instance.ModellpruefungOffen);
        IElement gruppe = cut.Find(".epos-modellpruefung-gruppe[data-gruppe='2']");
        Assert.Equal("true", gruppe.QuerySelector("button.epos-modellpruefung-aufklappen")!.GetAttribute("aria-expanded"));
        Assert.Equal(2, gruppe.QuerySelectorAll(".epos-modellpruefung-befund").Length);
        Assert.Equal("false", cut.Find(".epos-modellpruefung-gruppe[data-gruppe='4'] button.epos-modellpruefung-aufklappen").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void N1_Ampelleiste_nicht_im_Assistenten()
    {
        var geoeffnet = new List<string>();
        var cut = MitModellpruefung(GespeicherteZeilen(), geoeffnet, wizard: true);
        cut.Render(p => p.Add(x => x.ProjektGaben, _ => new Dictionary<string, object> { ["StartReiter"] = GebaeudeKatalogDialog.REITER_ZONEN }));
        cut.FindAll("td.epos-spalte-stift button").FirstOrDefault()?.Click();
        Assert.Empty(cut.FindAll(".epos-pruefleiste"));
    }

    /// <summary>
    /// N1 (MA-2): „Zone…“ im Blatt „Nutzung und Kalender“ über dem Ampelblatt — der Editor geht ÜBER dem Ampelblatt mit der Zone
    /// auf (Startzone); schließt er, steht das Ampelblatt wieder oben.
    /// </summary>
    [Fact]
    public void N1_Zone_aus_dem_Blatt_ueber_dem_Ampelblatt_oeffnet_den_Editor_darueber()
    {
        var geoeffnet = new List<string>();
        var gaben = new List<IReadOnlyDictionary<string, object>>();
        var cut = MitModellpruefung(GespeicherteZeilen(), geoeffnet);
        cut.Render(p => p
            .Add(x => x.ZonenNutzungGaben, _ => new ZonenNutzungProbeweg().Weg())
            .Add(x => x.ProjektGaben, _ => new Dictionary<string, object>()));
        Pruefknopf(cut)!.Click();
        cut.Find(".epos-modellpruefung-gruppe[data-gruppe='3'] button.epos-modellpruefung-bearbeiten").Click();
        Assert.True(cut.Instance.ZonenNutzungOffen);
        IElement zone = cut.FindAll("button.epos-zonennutzung-zone").First();
        zone.Click();

        Assert.False(cut.Instance.ZonenNutzungOffen);
        Assert.True(cut.Instance.ModellpruefungOffen);
        Assert.True(cut.Instance.EditorUeberAmpelblatt);
        // Der Editor steht im Stapel nach dem Ampelblatt (liegt darüber).
        var titel = cut.FindAll(".epos-ueberlagerung-titel").Select(t => t.TextContent).ToList();
        Assert.True(titel.IndexOf(R.MPB_TITEL) < titel.Count - 1);

        cut.FindAll(".epos-ueberlagerung").Last().QuerySelector("button.epos-ueberlagerung-zu")!.Click();
        Assert.False(cut.Instance.EditorUeberAmpelblatt);
        Assert.True(cut.Instance.ModellpruefungOffen);
    }
}
