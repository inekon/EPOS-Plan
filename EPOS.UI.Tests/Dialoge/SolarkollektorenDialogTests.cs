using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Eingabe der Solarkollektoren (iU9-W7.7). Soll ist die Feldkarte von
/// <c>Form_SolarKollektoren</c>: zwei Listen mit den beiden Pfeilen, der Modulblock
/// mit sechs Anzeigefeldern und die Gruppe „Kollektor" mit sechs Bedienelementen.
/// </summary>
public class SolarkollektorenDialogTests : BunitContext
{
    private static readonly KatalogZeile[] Katalog =
    {
        new(11, "Vitosol 200", "Viessmann\nKollektortyp: Flach\nModulfläche: 2,51 m²\nAperturfläche: 2,31 m²"),
        new(12, "Vitosol 300", "Viessmann\nKollektortyp: Röhre\nModulfläche: 3,2 m²\nAperturfläche: 3 m²")
    };

    public SolarkollektorenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId = 11) => new()
    {
        Schluessel = schluessel,
        Bezeichner = name,
        GeraetId = geraetId,
        AnzahlModule = 4,
        Neigung = 30,
        Azimut = 0,
        Vorlauf = 60,
        Ruecklauf = 40
    };

    private static ErzeugerDetail Detail(string name) => new(
        name, "",
        new[] { ("Kollektor:", "Flach"), ("Hersteller :", "Viessmann"),
                ("Beschreibung :", "Flachkollektor"), ("Aperturfläche:", "2,31") });

    private IRenderedComponent<SolarkollektorenDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string, bool, IReadOnlyDictionary<string, object>>? editorGaben = null,
        Func<string, bool>? katalogLoeschen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
        => Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Modulflaeche, _ => 2.5)
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Vitosol 300", 12))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.EditorGaben, editorGaben)
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<SolarkollektorenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-mitte button").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Auswahl in Projekt:", ueberschriften);
        Assert.Contains("Auswahl in DB:", ueberschriften);

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Modul", "Kollektor" }, gruppen);

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Kollektor in DB ändern...", knoepfe);
        Assert.Contains("Kollektor in DB neu...", knoepfe);
        Assert.Contains("Kollektor in DB löschen", knoepfe);
        Assert.Contains("Übernehmen", knoepfe);
    }

    [Fact]
    public void Der_Modulblock_ist_reine_Anzeige()
    {
        var cut = Aufbauen();
        var modul = cut.FindAll(".epos-gruppenkopf-koerper")[0];

        // Name plus die vier Detailfelder.
        Assert.Equal(5, modul.QuerySelectorAll("input[readonly]").Length);
        Assert.Empty(modul.QuerySelectorAll("input:not([readonly])"));
    }

    [Fact]
    public void Die_Kollektorgruppe_traegt_sechs_Bedienelemente()
    {
        var cut = Aufbauen();
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];

        // Anzahl, Neigung, Azimut, Vorlauf, Ruecklauf plus die gerechnete Flaeche.
        Assert.Equal(5, kollektor.QuerySelectorAll("input:not([readonly])").Length);
        Assert.Single(kollektor.QuerySelectorAll("input[readonly]"));
        Assert.Contains("Übernehmen", kollektor.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
    }

    /// <summary>Die Maske ist lokalisiert (22 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<SolarkollektorenDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Detail, Detail)
            .Add(x => x.TitelText, "Entering the solar panels")
            .Add(x => x.LabelAnzahl, "Modules:")
            .Add(x => x.BtnUebernehmenText, "Take over"));

        Assert.Equal("Entering the solar panels", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Modules:", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("Take over", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Die Kollektorgruppe erscheint nur bei einer Projektzeile
    // =================================================================================

    [Fact]
    public void Eine_Katalogzeile_zeigt_das_Detail_OHNE_Kollektorgruppe()
    {
        // dataGridView1_Click:289 blendete groupBox_Kollektor aus.
        var cut = Aufbauen();
        Assert.Equal(2, cut.FindAll(".epos-gruppenkopf-titel").Count);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Modul" }, gruppen);
    }

    [Fact]
    public void Eine_Projektzeile_fuellt_die_Kollektorgruppe()
    {
        var cut = Aufbauen();
        var werte = cut.FindAll(".epos-gruppenkopf-koerper")[1]
                       .QuerySelectorAll("input").Select(e => e.GetAttribute("value")).ToList();

        // Reihenfolge: Anzahl, Aperturflaeche (gerechnet), Neigung, Azimut, Vorlauf, Ruecklauf.
        Assert.Equal("4", werte[0]);
        Assert.Equal("10", werte[1]);            // 2,5 m² x 4
        Assert.Equal("30", werte[2]);
        Assert.Equal("0", werte[3]);
        Assert.Equal("60", werte[4]);
        Assert.Equal("40", werte[5]);
    }

    [Fact]
    public void Die_Aperturflaeche_folgt_der_Modulanzahl_live()
    {
        // textBox_Anzahl_TextChanged:367 rechnete bei jedem Tastendruck nach.
        var cut = Aufbauen();
        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];

        kollektor.QuerySelectorAll("input")[0].Input("6");

        Assert.Equal("15", cut.FindAll(".epos-gruppenkopf-koerper")[1]
                              .QuerySelectorAll("input")[1].GetAttribute("value"));
    }

    // =================================================================================
    // Aufnehmen, Entfernen, Uebernehmen
    // =================================================================================

    [Fact]
    public void Der_linke_Pfeil_ist_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();
        var pfeile = cut.FindAll(".epos-zweispalten-mitte button");

        Assert.True(pfeile[0].HasAttribute("disabled"));    // ◀ ohne Katalogwahl
        Assert.False(pfeile[1].HasAttribute("disabled"));   // ▶ mit Projektzeile
    }

    [Fact]
    public void Der_linke_Pfeil_legt_eine_Zeile_an_und_waehlt_sie()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        int gerufen = 0;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gerufen = id;
            return new AufnahmeErgebnis(Zeile(9, "Vitosol 300", 12));
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[1].Click();  // Katalogzeile 2
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        Assert.Equal(12, gerufen);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Vitosol 300", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Eine_Ablehnung_beim_Aufnehmen_meldet_und_legt_nichts_an()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Vitosol 200") };
        var cut = Aufbauen(zeilen, aufnehmen: _ =>
            new AufnahmeErgebnis(null, "Der Datensatz konnte nicht in das Projekt übernommen werden.", true));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        Assert.Single(zeilen);
        Assert.Contains("nicht in das Projekt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_rechte_Pfeil_entfernt_genau_die_gewaehlte_Zeile()
    {
        var a = Zeile(1, "Vitosol 200");
        var b = Zeile(2, "Vitosol 300", 12);
        var zeilen = new List<ErzeugerZeile> { a, b };

        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button")!.Click();
        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    [Fact]
    public void Uebernehmen_schreibt_die_fuenf_Ganzzahlen_und_meldet()
    {
        var zeile = Zeile(1, "Vitosol 200");
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile }, uebernehmen: z => uebernommen.Add(z));

        var kollektor = cut.FindAll(".epos-gruppenkopf-koerper")[1];
        kollektor.QuerySelectorAll("input")[0].Input("6");     // Anzahl
        kollektor.QuerySelectorAll("input")[2].Input("35");    // Neigung
        kollektor.QuerySelectorAll("input")[3].Input("15");    // Azimut
        kollektor.QuerySelectorAll("input")[4].Input("55");    // Vorlauf
        kollektor.QuerySelectorAll("input")[5].Input("35");    // Ruecklauf
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(6, zeile.AnzahlModule);
        Assert.Equal(35, zeile.Neigung);
        Assert.Equal(15, zeile.Azimut);
        Assert.Equal(55, zeile.Vorlauf);
        Assert.Equal(35, zeile.Ruecklauf);
        Assert.Same(zeile, uebernommen.Single());

        // A-24: Der 500-ms-Bildblitz mit Thread.Sleep wird ein Hinweis.
        Assert.Contains("übernommen", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_leeres_Ganzzahlfeld_gilt_als_Null()
    {
        // Program.GanzzahlPruefen(..., leerErlaubt: true) im Vorlaeufer.
        var zeile = Zeile(1, "Vitosol 200");
        var cut = Aufbauen(new List<ErzeugerZeile> { zeile });

        cut.FindAll(".epos-gruppenkopf-koerper")[1].QuerySelectorAll("input")[3].Input("");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(0, zeile.Azimut);
    }

    // =================================================================================
    // Katalogpflege
    // =================================================================================

    [Fact]
    public void Katalog_aendern_und_loeschen_sind_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();

        Assert.True(Knopf(cut, "Kollektor in DB ändern...").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Kollektor in DB löschen").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Kollektor in DB neu...").HasAttribute("disabled"));
    }

    [Fact]
    public void Kollektor_aendern_zeigt_den_Editor_in_der_Ueberlagerung()
    {
        string? name = null;
        bool? neu = null;
        var cut = Aufbauen(editorGaben: (n, b) =>
        {
            name = n; neu = b;
            return new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } };
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Kollektor in DB ändern...").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Vitosol 200", name);
        Assert.False(neu);
    }

    [Fact]
    public void Kollektor_neu_fragt_erst_den_Namen()
    {
        string? name = null;
        bool? neu = null;
        var cut = Aufbauen(editorGaben: (n, b) =>
        {
            name = n; neu = b;
            return new Dictionary<string, object> { ["Daten"] = new SolarkollektorKatalogDaten { Name = n } };
        });

        Knopf(cut, "Kollektor in DB neu...").Click();
        Assert.False(cut.Instance.EditorOffen);

        cut.Find(".epos-ueberlagerung input").Input("Neuer Kollektor");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Equal("Neuer Kollektor", name);
        Assert.True(neu);
    }

    [Fact]
    public void Kollektor_loeschen_fragt_nach()
    {
        var geloescht = new List<string>();
        var cut = Aufbauen(katalogLoeschen: n => { geloescht.Add(n); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Kollektor in DB löschen").Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal("Vitosol 200", geloescht.Single());
    }

    // =================================================================================
    // Abschluss und Tastatur
    // =================================================================================

    [Fact]
    public void OK_und_Abbrechen_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut2, "Abbrechen").Click();
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_nur_ohne_offene_Rueckfrage()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();
        Knopf(cut, "Kollektor in DB löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }
}
