using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Stromspeicher (iU9-W6.6). Soll ist die Feldkarte von
/// <c>Form_Stromspeicher</c>: zwei Listen (die rechte mit zwei Spalten), die
/// beiden Pfeile und der reine Anzeigeblock mit sieben Feldern — davon zwei mit
/// Beschriftungen aus dem Ressourcenkatalog statt aus dem Designer.
/// </summary>
public class StromspeicherDialogTests : BunitContext
{
    private static readonly KatalogZeile[] Katalog =
    {
        new(41, "Speicher 10", "10 kW\nLithium"),
        new(42, "Speicher 20", "20 kW\nLithium")
    };

    public StromspeicherDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Typ:", "Lithium"), ("Leistung [kW]:", "10"),
                ("Energie (Kapazität) [kWh]:", "20"), ("Degradation [%/a]:", "0,1"),
                ("Ladezustand [%]:", "50"), ("Modulkosten [€/kWh]:", "400") });

    private IRenderedComponent<StromspeicherDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Func<string, Task<bool>>? sprung = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
    {
        return Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Detail, n => Detail(n))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Speicher 20", 42))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Sprung, sprung)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-auswahlpfeile button").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("ausgewählte Stromspeicher:", ueberschriften);
        Assert.Contains("Stromspeicher aus Datenbank:", ueberschriften);

        // Sieben NUR LESBARE Anzeigefelder: Name, Typ, Leistung, Energie, Degradation,
        // Ladezustand, Modulkosten.
        Assert.Equal(7, cut.FindAll(".epos-gruppenkopf-koerper input[readonly]").Count);
    }

    [Fact]
    public void Die_zwei_berichtigten_Beschriftungen_stehen_da()
    {
        // EinheitenBeschriftungKorrigieren: Der Designer trug "Energie [kW]" und
        // "Modulkosten" - beides fachlich falsch (Abnahmebefund 1).
        var cut = Aufbauen();

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Energie (Kapazität) [kWh]:", texte);
        Assert.Contains("Modulkosten [€/kWh]:", texte);
        Assert.DoesNotContain("Energie [kW]:", texte);
    }

    [Fact]
    public void Die_Katalogliste_hat_die_zweite_Spalte()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster")[1].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Name", kopf);
        Assert.Contains("Eigenschaften", kopf);
        Assert.Contains("10 kW", cut.FindAll(".epos-mehrzeilig")[0].TextContent);
    }

    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Sprung()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");

        var mit = Aufbauen(sprung: _ => Task.FromResult(true));
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent), t => t == "Bearbeiten...");
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Je_Klick_entsteht_eine_eigene_Zeile()
    {
        // AP2b: Bis dahin landete immer dasselbe Feldobjekt in der Liste; zweimal
        // derselbe Speicher sind zwei Zeilen.
        var zeilen = new List<ErzeugerZeile>();
        int naechster = 10;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
            new AufnahmeErgebnis(Zeile(naechster++, "Speicher 10", id)));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();

        Assert.Equal(2, zeilen.Count);
        Assert.NotEqual(zeilen[0].Schluessel, zeilen[1].Schluessel);
    }

    [Fact]
    public void Der_Pfeil_zurueck_trifft_genau_die_gewaehlte_Zeile()
    {
        // A-17: Der Vorlaeufer nahm die ERSTE Zeile gleichen Namens.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41), Zeile(2, "Speicher 10", 41) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-auswahlpfeile button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    [Fact]
    public void Nach_der_letzten_Zeile_wandert_die_Auswahl_in_den_Katalog()
    {
        // btn_Entfernen_Click baute dafuer einen Mausklick auf die erste Rasterzeile nach.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-auswahlpfeile button")[1].Click();

        Assert.Empty(zeilen);
        Assert.Null(cut.Instance.Projektzeile);
        Assert.Equal(41, cut.Instance.Katalogzeile!.Id);
    }

    // =================================================================================
    // Detail und Tastatur
    // =================================================================================

    [Fact]
    public void Beide_Listen_holen_ihr_Detail_aus_demselben_Katalogsatz()
    {
        // listBox_SP_SelectedIndexChanged und dataGridView1_Click sind zeichengleich -
        // es gibt keine Projektkopie, die abweichen koennte.
        var gefragt = new List<string>();
        var cut = Render<StromspeicherDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Speicher 10", 41) })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Detail, n => { gefragt.Add(n); return Detail(n); }));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal(new[] { "Speicher 10", "Speicher 20" }, gefragt);
    }

    [Fact]
    public void Bearbeiten_springt_in_die_Speicherverwaltung()
    {
        string? ziel = null;
        var cut = Aufbauen(sprung: s => { ziel = s; return Task.FromResult(true); });

        cut.FindAll(".epos-auswahlspalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Equal(Sprungziel.StromspeicherAdmin, ziel);
    }

    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_nicht_belegt()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, rufe);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }
}
