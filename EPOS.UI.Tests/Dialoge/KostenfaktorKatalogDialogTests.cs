using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalog der Kostenfaktoren (iU9-W1.5). Soll ist die Feldkarte von
/// <c>Form_KostenAdmin</c>: Liste, "Neu", "Löschen", "OK" und der
/// Einleitungssatz — dazu das Textfeld, das frueher der Unterdialog
/// <c>Form_KostenItemNeu</c> war.
/// </summary>
public class KostenfaktorKatalogDialogTests : BunitContext
{
    private static KostenfaktorKatalogDialog.KostenfaktorZeile Z(int id, string name) => new(id, name);

    private static readonly KostenfaktorKatalogDialog.KostenfaktorZeile[] Bestand =
    {
        Z(3, "Montage"),
        Z(7, "Wartung")
    };

    public KostenfaktorKatalogDialogTests()
    {
        // QuickGrid (im Raster) laedt beim ersten Zeichnen ein JS-Modul.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<KostenfaktorKatalogDialog> Aufbauen(
        Action? beimSchliessen = null,
        Func<string, int>? neu = null,
        Func<int, bool>? loeschen = null,
        Func<string, bool>? rueckfrage = null,
        Func<IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>>? neuLaden = null)
    {
        return Render<KostenfaktorKatalogDialog>(p => p
            .Add(x => x.Zeilen, Bestand)
            .Add(x => x.Neu, neu ?? (_ => 11))
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Rueckfrage, rueckfrage)
            .Add(x => x.NeuLaden, neuLaden ?? (() => Bestand))
            .Add(x => x.Geschlossen, () => beimSchliessen?.Invoke()));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen();

        Assert.Equal("Administration Kostenfaktoren", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Verwalten Sie hier die Kostenfaktoren", cut.Find(".epos-herleitung-text").TextContent);
        Assert.Single(cut.FindAll("input[type=text]"));            // Bezeichner (frueher Unterdialog)
        Assert.Single(cut.FindAll(".epos-raster"));                // die Liste
        // Neu, zwei Wahlknoepfe der Zeilen, Löschen, OK.
        Assert.Equal(5, cut.FindAll("button.epos-knopf").Count);
    }

    [Fact]
    public void Die_Liste_zeigt_den_Bestand()
    {
        var cut = Aufbauen();

        var zellen = cut.FindAll(".epos-raster tbody td");
        Assert.Equal(4, zellen.Count);                             // 2 Zeilen x (Wahl + Bezeichnung)
        Assert.Equal("Montage", zellen[1].TextContent.Trim());
        Assert.Equal("Wartung", zellen[3].TextContent.Trim());
    }

    [Fact]
    public void Ohne_Markierung_ist_Loeschen_gesperrt()
    {
        // btnDeleteKostenfaktor_Click: SelectedItems.Count == 0 -> return.
        var cut = Aufbauen();

        Assert.Null(cut.Instance.Gewaehlt);
        Assert.True(cut.FindAll("button.epos-knopf")[3].HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Wahlspalte_markiert_eine_Zeile()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-anlagenwahl")[1].Click();

        Assert.Equal(7, cut.Instance.Gewaehlt);
        Assert.Equal("true", cut.FindAll(".epos-anlagenwahl")[1].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Ein_leerer_Name_legt_nichts_an()
    {
        // btnNeuKostenfaktor_Click: neueBezeichnung.Length == 0 -> return.
        bool gerufen = false;
        var cut = Aufbauen(neu: _ => { gerufen = true; return 1; });

        cut.Find("input[type=text]").Input("   ");
        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.False(gerufen);
    }

    [Fact]
    public void Neu_legt_an_leert_das_Feld_und_laedt_die_Liste_neu()
    {
        string? erhalten = null;
        var bestand = new List<KostenfaktorKatalogDialog.KostenfaktorZeile>(Bestand);
        var cut = Aufbauen(
            neu: name => { erhalten = name; bestand.Add(Z(11, name)); return 11; },
            neuLaden: () => bestand);

        cut.Find("input[type=text]").Input("  Gerüst  ");
        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.Equal("Gerüst", erhalten);
        Assert.Equal("", cut.Find("input[type=text]").GetAttribute("value"));
        Assert.Equal(3, cut.Instance.Angezeigt.Count);
        Assert.Equal(11, cut.Instance.Gewaehlt);   // der neue Satz ist markiert
    }

    [Fact]
    public void Ein_gescheitertes_Anlegen_meldet_sich_als_Warnbanner()
    {
        // Frueher eine MessageBox ("Der Kostenfaktor konnte nicht angelegt werden.").
        var cut = Aufbauen(neu: _ => 0);

        cut.Find("input[type=text]").Input("Gerüst");
        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.Equal("Der Kostenfaktor konnte nicht angelegt werden.",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Loeschen_fragt_vorher_und_nennt_den_Namen()
    {
        string? frage = null;
        bool geloescht = false;
        var cut = Aufbauen(
            rueckfrage: text => { frage = text; return true; },
            loeschen: _ => { geloescht = true; return true; });

        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll("button.epos-knopf")[3].Click();

        Assert.Equal("Kostenfaktor 'Montage' wirklich löschen?", frage);
        Assert.True(geloescht);
    }

    [Fact]
    public void Ein_Nein_in_der_Rueckfrage_loescht_nicht()
    {
        bool geloescht = false;
        var cut = Aufbauen(
            rueckfrage: _ => false,
            loeschen: _ => { geloescht = true; return true; });

        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll("button.epos-knopf")[3].Click();

        Assert.False(geloescht);
        Assert.Equal(3, cut.Instance.Gewaehlt);
    }

    [Fact]
    public void Loeschen_meldet_die_Id_der_markierten_Zeile()
    {
        // Abweichung zum Vorlaeufer: Er loeschte ueber die Bezeichnung.
        int erhalten = 0;
        var bestand = new List<KostenfaktorKatalogDialog.KostenfaktorZeile>(Bestand);
        var cut = Aufbauen(
            rueckfrage: _ => true,
            loeschen: id => { erhalten = id; bestand.RemoveAll(z => z.StammId == id); return true; },
            neuLaden: () => bestand);

        cut.FindAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll("button.epos-knopf")[3].Click();

        Assert.Equal(7, erhalten);
        Assert.Single(cut.Instance.Angezeigt);
        Assert.Null(cut.Instance.Gewaehlt);
    }

    [Fact]
    public void Ohne_Rueckfragedelegat_wird_sofort_geloescht()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return true; });

        cut.FindAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll("button.epos-knopf")[3].Click();

        Assert.True(geloescht);
    }

    [Fact]
    public void OK_und_Esc_schliessen_den_Dialog()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(1, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);

        // Enter bleibt unbelegt (A-7): "Neu" und "Löschen" schreiben sofort.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(2, gemeldet);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_KostenAdmin.btn_Help" }, hilfe.Geoeffnet);
    }
}
