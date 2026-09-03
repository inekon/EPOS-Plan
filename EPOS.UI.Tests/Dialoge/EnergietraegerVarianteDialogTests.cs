using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der erste als Komponente geschriebene Dialog (iZ5) - Verhalten wie
/// Views/Kosten/Form_Kosten_Auswahl, nur ohne Datenbank und ohne MessageBox.
/// </summary>
public class EnergietraegerVarianteDialogTests : BunitContext
{
    private static readonly (int Id, string Name)[] Traeger =
    {
        (3, "Erdgas"),
        (7, "Fernwaerme")
    };

    public EnergietraegerVarianteDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<EnergietraegerVarianteDialog> Aufbauen(
        Action<EnergietraegerVarianteErgebnis?> beimSchliessen, int? vorwahl = null)
    {
        return Render<EnergietraegerVarianteDialog>(p => p
            .Add(x => x.Energietraeger, Traeger)
            .Add(x => x.VorwahlId, vorwahl)
            .Add(x => x.Geschlossen, beimSchliessen));
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen(_ => { });

        Assert.Equal("Energieträger Variante", cut.Find(".epos-dialog-titel").TextContent);
        var beschriftungen = cut.FindAll(".epos-feld-text");
        Assert.Equal("Energieträger:", beschriftungen[0].TextContent);
        Assert.Equal("Energieträger Varianten Bezeichnung:", beschriftungen[1].TextContent);
        Assert.Equal("OK", cut.Find(".epos-knopf--primaer").TextContent);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen(_ => { });
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_Kosten_Auswahl.btn_Help" }, hilfe.Geoeffnet);
    }

    [Fact]
    public void Ohne_Vorwahl_ist_der_erste_Traeger_gewaehlt_und_das_Namensfeld_leer()
    {
        var cut = Aufbauen(_ => { });

        Assert.Equal(3, cut.Instance.Auswahl);
        Assert.Equal("", cut.FindAll("input")[0].GetAttribute("value"));
    }

    [Fact]
    public void Die_Vorwahl_wird_uebernommen()
    {
        var cut = Aufbauen(_ => { }, vorwahl: 7);

        Assert.Equal(7, cut.Instance.Auswahl);
    }

    [Fact]
    public void Mit_Vorwahl_ist_der_Variantenname_mit_dem_Traegernamen_vorbelegt()
    {
        // Wie der WinForms-Vorlaeufer (comboBox_Varianten.Text = Traegername):
        // der Anwender kann sofort OK druecken (Befund 03.09.2026, Heizkessel).
        var cut = Aufbauen(_ => { }, vorwahl: 7);

        string erwartet = System.Linq.Enumerable.First(Traeger, t => t.Id == 7).Name;
        Assert.Equal(erwartet, cut.FindAll("input")[0].GetAttribute("value"));
    }

    [Fact]
    public void Eine_Auswahl_belegt_den_Variantennamen_vor()
    {
        // wie cmbBrennstoffArt_SelectedIndexChanged:
        // TextBox_Variante.Text = cmbBrennstoffArt.Text
        var cut = Aufbauen(_ => { });

        cut.Find("select").Change("7");

        Assert.Equal("Fernwaerme", cut.FindAll("input")[0].GetAttribute("value"));
    }

    [Fact]
    public void OK_mit_Namen_liefert_das_Ergebnis()
    {
        EnergietraegerVarianteErgebnis? ergebnis = null;
        bool gemeldet = false;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet = true; });

        cut.Find("select").Change("7");
        cut.FindAll("input")[0].Input("Fernwaerme Grundtarif");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.True(gemeldet);
        Assert.NotNull(ergebnis);
        Assert.Equal(7, ergebnis!.BrennstoffId);
        Assert.Equal("Fernwaerme", ergebnis.BrennstoffName);
        Assert.Equal("Fernwaerme Grundtarif", ergebnis.VariantenName);
    }

    [Fact]
    public void OK_ohne_Namen_zeigt_das_Warnbanner_und_meldet_nichts()
    {
        bool gemeldet = false;
        var cut = Aufbauen(_ => gemeldet = true);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.False(gemeldet);
        Assert.Equal("Bitte einen Variantennamen (Code) eingeben.",
                     cut.Find(".epos-warnbanner-text").TextContent);
        Assert.Equal("alert", cut.Find(".epos-warnbanner").GetAttribute("role"));
    }

    [Fact]
    public void Nach_einer_Eingabe_verschwindet_die_Meldung_wieder()
    {
        var cut = Aufbauen(_ => { });

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Single(cut.FindAll(".epos-warnbanner"));

        cut.FindAll("input")[0].Input("Erdgas Grundversorgung");
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        EnergietraegerVarianteErgebnis? ergebnis = new(1, "x", "y");
        bool gemeldet = false;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet = true; });

        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Enter_wirkt_wie_OK()
    {
        EnergietraegerVarianteErgebnis? ergebnis = null;
        var cut = Aufbauen(e => ergebnis = e);

        cut.FindAll("input")[0].Input("Erdgas Grundversorgung");
        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.NotNull(ergebnis);
        Assert.Equal(3, ergebnis!.BrennstoffId);
        Assert.Equal("Erdgas Grundversorgung", ergebnis.VariantenName);
    }

    [Fact]
    public void Esc_wirkt_wie_Abbrechen()
    {
        EnergietraegerVarianteErgebnis? ergebnis = new(1, "x", "y");
        bool gemeldet = false;
        var cut = Aufbauen(e => { ergebnis = e; gemeldet = true; });

        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Das_Wurzelelement_nimmt_den_Fokus_auf()
    {
        var cut = Aufbauen(_ => { });

        Assert.Equal("-1", cut.Find("div.epos-dialog").GetAttribute("tabindex"));
    }
}
