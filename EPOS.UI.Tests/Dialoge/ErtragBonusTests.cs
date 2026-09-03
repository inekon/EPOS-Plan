using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Kosten;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Abschnitt „Ertrag/Bonus" (iU9-W4.1), Vorbild
/// <c>Views/Kosten/ucErtragBonus</c>.
///
/// <para>Soll ist die Feldkarte: vier Gruppen für das BHKW (KWKG, Förderdauer,
/// Steuern, Pflegeorte), eine Gruppe für die Photovoltaik (Erklärung,
/// Stammprojekt, Knopf) und der Leersatz für alle übrigen Komponenten.</para>
/// </summary>
public class ErtragBonusTests : BunitContext
{
    private static readonly (int Id, string Text)[] PROJEKTE =
    {
        (7, "Musterprojekt"), (9, "Zweitprojekt")
    };

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Beim_BHKW_stehen_vier_Gruppen_in_der_Reihenfolge_der_Feldkarte()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.TitelKwkg, "KWKG-Zuschlag")
            .Add(x => x.TitelDauer, "Förderdauer und Jahresdeckel")
            .Add(x => x.TitelSteuern, "Steuervergünstigungen")
            .Add(x => x.TitelVerweise, "Pflegeorte"));

        var titel = cut.FindAll(".epos-gruppenkopf-titel");
        Assert.Equal(4, titel.Count);
        Assert.Equal("KWKG-Zuschlag", titel[0].TextContent);
        Assert.Equal("Förderdauer und Jahresdeckel", titel[1].TextContent);
        Assert.Equal("Steuervergünstigungen", titel[2].TextContent);
        Assert.Equal("Pflegeorte", titel[3].TextContent);
    }

    [Fact]
    public void Die_KWKG_Saetze_kommen_fertig_herein_und_stehen_in_fester_Breite()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.EinspeisungText, "bis 50 kW:      8,0 ct/kWh")
            .Add(x => x.SonderregelText, "Sonderregel neue Anlagen ≤ 50 kWel")
            .Add(x => x.EigenText, "Selbst genutzter KWK-Strom")
            .Add(x => x.DauerText, "Neue Anlagen: 30.000 Vollbenutzungsstunden")
            .Add(x => x.SteuernText, "Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3")
            .Add(x => x.Fk7Text, "FK7: Der STROMPREIS-Teil"));

        Assert.Equal("bis 50 kW:      8,0 ct/kWh", cut.Find(".epos-ertrag-tabelle").TextContent);
        Assert.Contains("Sonderregel neue Anlagen", cut.Markup);
        Assert.Contains("Selbst genutzter KWK-Strom", cut.Markup);
        Assert.Contains("30.000 Vollbenutzungsstunden", cut.Markup);
        Assert.Contains("Stromsteuer-Befreiung", cut.Markup);
        Assert.Contains("FK7: Der STROMPREIS-Teil", cut.Markup);
    }

    [Fact]
    public void Bei_der_Photovoltaik_stehen_Erklaerung_Liste_und_Knopf()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.PvErklaerungText, "Die PV-Vergütung wird STAMMPROJEKTBEZOGEN gepflegt")
            .Add(x => x.LabelPvProjekt, "Stammprojekt:")
            .Add(x => x.PvOeffnenText, "PV-Vergütungsdialog öffnen…"));

        Assert.Single(cut.FindAll(".epos-gruppenkopf"));
        Assert.Contains("STAMMPROJEKTBEZOGEN", cut.Markup);
        Assert.Equal(2, cut.Find("select").QuerySelectorAll("option").Length);
        Assert.Equal("PV-Vergütungsdialog öffnen…", cut.Find("button").TextContent);
    }

    [Fact]
    public void Ohne_BHKW_und_ohne_PV_steht_nur_der_Leersatz()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.LeerText, "Diese Komponente führt keine laufenden Erträge"));

        Assert.Empty(cut.FindAll(".epos-gruppenkopf"));
        Assert.Contains("keine laufenden Erträge", cut.Markup);
    }

    // =====================================================================
    // Photovoltaik: Vorwahl und Sprung
    // =====================================================================

    [Fact]
    public void Ohne_Vorwahl_steht_das_erste_Projekt(){
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE));

        Assert.Equal(7, cut.Instance.GewaehltesProjekt);
    }

    [Fact]
    public void Eine_Vorwahl_wird_uebernommen()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.ProjektVorwahl, 9));

        Assert.Equal(9, cut.Instance.GewaehltesProjekt);
    }

    [Fact]
    public void Der_Knopf_meldet_das_gewaehlte_Stammprojekt()
    {
        int gemeldet = 0;
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE)
            .Add(x => x.PvOeffnen, (int id) => gemeldet = id));

        cut.Find("select").Change("9");
        cut.Find("button").Click();

        Assert.Equal(9, gemeldet);
    }

    [Fact]
    public void Ohne_Projekte_ist_der_Knopf_gesperrt()
    {
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, Array.Empty<(int, string)>()));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    // =====================================================================
    // Sprung in den Gesetzeskatalog
    // =====================================================================

    [Fact]
    public void Ohne_Sprungbruecke_fehlt_der_Katalogknopf()
    {
        var cut = Render<ErtragBonus>(p => p.Add(x => x.IstBhkw, true));

        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Der_Katalogknopf_springt_und_laesst_danach_neu_lesen()
    {
        string? ziel = null;
        int neuGelesen = 0;
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.Sprung, (Func<string, Task<bool>>)(z => { ziel = z; return Task.FromResult(true); }))
            .Add(x => x.KatalogGeaendert, () => neuGelesen++));

        cut.Find("button").Click();

        Assert.Equal(Sprungziel.Gesetzesparameter, ziel);
        Assert.Equal(1, neuGelesen);
    }

    [Fact]
    public void BHKW_und_PV_koennen_nicht_gleichzeitig_gelten_aber_beides_wird_gezeigt()
    {
        // Der Wirt entscheidet über HatInhalt; die Komponente zeigt, was sie
        // bekommt — dieselbe Arbeitsteilung wie im Vorläufer (Zeige).
        var cut = Render<ErtragBonus>(p => p
            .Add(x => x.IstBhkw, true)
            .Add(x => x.IstPv, true)
            .Add(x => x.Projekte, PROJEKTE));

        Assert.Equal(5, cut.FindAll(".epos-gruppenkopf").Count);
    }
}
