using Bunit;
using EPOS.UI.Dialoge.Import;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// GD-N am Importdialog: die Zeile des vorgelegten Zonenplans eines früheren Imports (samt benannter Abweichung der
/// Prüfsumme) und der Prüfbefund der Projektdatei beim Weg „Gebäudedatei + Projektdatei“.
/// </summary>
public partial class GebaeudeImportZonenDialogTests
{
    [Fact]
    public void Der_vorgelegte_Zonenplan_steht_ueber_dem_Zonenbaum_und_die_Abweichung_benannt()
    {
        const string ZEILE = "Vorbelegt mit dem gespeicherten Zonenplan aus „haus.ifc“ (2026-10-12): 4 Räume in 2 Zonen.";
        const string ABWEICHUNG = "Die gewählte Datei weicht vom gespeicherten Zonenplan ab (Prüfsumme aaa statt bbb).";
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s.Zonierung is null ? s
            : s with { Zonierung = s.Zonierung with { Planvorlage = ZEILE, PlanvorlageAbweichung = ABWEICHUNG } });
        var vorlage = cut.Find(".epos-gebimport-planvorlage");
        Assert.Equal("ja", vorlage.GetAttribute("data-abweichend"));
        Assert.Contains(ZEILE, vorlage.TextContent);
        Assert.Contains(ABWEICHUNG, vorlage.TextContent);

        IRenderedComponent<GebaeudeImportDialog> gleich = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s.Zonierung is null ? s
            : s with { Zonierung = s.Zonierung with { Planvorlage = ZEILE } });
        var g = gleich.Find(".epos-gebimport-planvorlage");
        Assert.Equal("nein", g.GetAttribute("data-abweichend"));
        Assert.DoesNotContain("Prüfsumme", g.TextContent);

        IRenderedComponent<GebaeudeImportDialog> ohne = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s);
        Assert.Empty(ohne.FindAll(".epos-gebimport-planvorlage"));
    }

    [Fact]
    public void Der_Pruefbefund_der_Projektdatei_steht_im_Block_der_Projektdatei()
    {
        string[] befund = { "Projektdatei „haus.sqproj“: vorhanden.", "Lesbar (Fassung 3).", "Prüfsumme 0123456789ab…" };
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s with
        {
            Projektdatei = new GebaeudeProjektdateiDaten { Dateiname = "haus.sqproj", Fassung = "3", Pruefbefund = befund }
        }, projektdatei: true);
        var zeilen = cut.FindAll(".epos-gebimport-sqbefund li");
        Assert.Equal(befund, zeilen.Select(z => z.TextContent.Trim()));

        IRenderedComponent<GebaeudeImportDialog> ohne = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s with
        {
            Projektdatei = new GebaeudeProjektdateiDaten { Dateiname = "haus.sqproj", Fassung = "3" }
        }, projektdatei: true);
        Assert.Empty(ohne.FindAll(".epos-gebimport-sqbefund"));
    }
}
