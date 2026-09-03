using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Projektliste - der Einstieg der iOS-Huelle (iU10-2).
///
/// Die Beschriftungen sind deutsche Standardwerte der Komponente; die UI-Kultur
/// wird trotzdem gepinnt, weil das Warnbanner und die Ressourcentexte der
/// Bausteine daran haengen (dieselbe Begruendung wie in SpeichernLeisteTests -
/// die CI-Laeufer auf macOS und Windows laufen englisch).
/// </summary>
public class ProjektlisteTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    private static readonly ProjektZeile[] DreiProjekte =
    {
        new ProjektZeile(1030, "B3-Kaskade", "Region 12", "WP+BHKW"),
        new ProjektZeile(1007, "Speichervariante A", "Region 12", "WP+Speicher"),
        new ProjektZeile(1017, "Speichervariante B", "Region 05", "BHKW+PV")
    };

    public ProjektlisteTests()
    {
        // QuickGrid (im Raster) laedt beim ersten Zeichnen ein JS-Modul.
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        base.Dispose(disposing);
    }

    [Fact]
    public void Die_Liste_zeichnet_eine_Zeile_je_Projekt()
    {
        var cut = Render<Projektliste>(p => p.Add(x => x.Zeilen, DreiProjekte));

        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Contains("B3-Kaskade", cut.Find("tbody").TextContent);
        Assert.Contains("Region 05", cut.Find("tbody").TextContent);
        Assert.Equal("3", cut.Find(".epos-seite-anzahl").TextContent);
    }

    [Fact]
    public void Die_Spaltenkoepfe_stehen_in_der_Reihenfolge_der_Startmaske()
    {
        var cut = Render<Projektliste>(p => p.Add(x => x.Zeilen, DreiProjekte));

        var koepfe = cut.FindAll("thead th");
        Assert.Equal(5, koepfe.Count);
        Assert.Contains("Nr.", koepfe[0].TextContent);
        Assert.Contains("Projekt", koepfe[1].TextContent);
        Assert.Contains("Klimaregion", koepfe[2].TextContent);
        Assert.Contains("Ausstattung", koepfe[3].TextContent);
    }

    [Fact]
    public void Ohne_Projekt_erscheint_der_Leertext_statt_einer_Tabelle()
    {
        var cut = Render<Projektliste>();

        Assert.Empty(cut.FindAll("table"));
        Assert.Contains("Es ist noch kein Projekt angelegt.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_Knopf_meldet_Projekt_und_Maskenschluessel()
    {
        (ProjektZeile Projekt, string Maske)? gemeldet = null;

        var cut = Render<Projektliste>(p => p
            .Add(x => x.Zeilen, DreiProjekte)
            .Add(x => x.Oeffnen, (( ProjektZeile Projekt, string Maske) w) => gemeldet = w));

        cut.FindAll(".epos-projekt-energie")[1].Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(1007, gemeldet!.Value.Projekt.Id);
        Assert.Equal(Seitenschluessel.Energietraeger, gemeldet.Value.Maske);
    }

    [Fact]
    public void Der_zweite_Knopf_traegt_den_BHKW_Schluessel()
    {
        (ProjektZeile Projekt, string Maske)? gemeldet = null;

        var cut = Render<Projektliste>(p => p
            .Add(x => x.Zeilen, DreiProjekte)
            .Add(x => x.Oeffnen, ((ProjektZeile Projekt, string Maske) w) => gemeldet = w));

        cut.FindAll(".epos-projekt-bhkw")[0].Click();

        Assert.Equal(Seitenschluessel.BhkwWirtschaftlichkeit, gemeldet!.Value.Maske);
        Assert.Equal(1030, gemeldet.Value.Projekt.Id);
    }
}
