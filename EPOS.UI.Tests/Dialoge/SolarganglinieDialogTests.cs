using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Solarthermieganglinien (iU9-W7.8). Soll ist die Feldkarte von
/// <c>Form_Solarganglinie</c>: neun Zeilen — zwei Listen, die beiden Pfeile, Name
/// und Beschreibung als Anzeige, „Bearbeiten…", OK und Abbrechen.
/// </summary>
public class SolarganglinieDialogTests : BunitContext
{
    private static readonly KatalogZeile[] Katalog =
    {
        new(21, "Ganglinie Nord", "Messreihe 2024, Standort Nord"),
        new(22, "Ganglinie Süd", "Messreihe 2024, Standort Süd")
    };

    public SolarganglinieDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int ganglinieId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = ganglinieId };

    /// <summary>
    /// Ein leerer Parametersatz der Ganglinienverwaltung (iU9-W14b.2): Er genügt für
    /// den Knopf „Bearbeiten…"; die Verwaltung selbst prüft
    /// <c>SolarganglinieAdminDialogTests</c>.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, object> LEERER_SATZ =
        new Dictionary<string, object>();

    private IRenderedComponent<SolarganglinieDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, ErzeugerZeile?>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        IReadOnlyDictionary<string, object>? verwaltungGaben = null,
        Func<IReadOnlyList<KatalogZeile>>? katalog = null,
        Action<bool>? geschlossen = null)
        => Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 21) })
            .Add(x => x.Katalog, katalog ?? (() => Katalog))
            .Add(x => x.Aufnehmen, aufnehmen ?? (id => Zeile(100000, "Ganglinie Süd", id)))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.VerwaltungGaben, verwaltungGaben)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<SolarganglinieDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen(verwaltungGaben: LEERER_SATZ);

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-auswahlpfeile button").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Ausgewählt im Projekt", ueberschriften);
        Assert.Contains("Solarthermieganglinie aus DB", ueberschriften);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Name:", texte);
        Assert.Contains("Beschreibung:", texte);

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Bearbeiten...", knoepfe);
        Assert.Contains("OK", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);
    }

    [Fact]
    public void Name_und_Beschreibung_sind_nur_lesbar()
    {
        var cut = Aufbauen();
        Assert.Single(cut.FindAll("input[readonly]"));       // Name
        Assert.Single(cut.FindAll("textarea[readonly]"));    // Beschreibung
    }

    /// <summary>Die Maske ist lokalisiert (6 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.TitelText, "Solar thermal energy curves")
            .Add(x => x.LabelProjektliste, "Selected in the project")
            .Add(x => x.LabelBeschreibung, "Description:"));

        Assert.Equal("Solar thermal energy curves", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Selected in the project",
                        cut.FindAll(".epos-untergruppe").Select(e => e.TextContent));
        Assert.Contains("Description:", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    /// <summary>
    /// Ohne Parametersatz der Verwaltung bleibt „Bearbeiten…" weg — seit iU9-W14b.2
    /// hängt der Knopf an <c>VerwaltungGaben</c> statt am Sprung-Delegaten.
    /// </summary>
    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Verwaltung()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Bearbeiten...");

        var mit = Aufbauen(verwaltungGaben: LEERER_SATZ);
        Assert.Contains("Bearbeiten...", mit.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    // =================================================================================
    // Auswahl
    // =================================================================================

    [Fact]
    public void Die_Wahl_zeigt_Name_UND_Beschreibung()
    {
        // A-27: Der Vorlaeufer setzte nur den Namen; sein Beschreibungsfeld blieb in
        // JEDEM Zustand leer, obwohl der Katalogsatz sie fuehrt.
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();

        Assert.Equal("Ganglinie Nord", cut.Find("input[readonly]").GetAttribute("value"));
        Assert.Equal("Messreihe 2024, Standort Nord", cut.Find("textarea[readonly]").TextContent);
    }

    [Fact]
    public void Auch_eine_Projektzeile_zeigt_ihre_Beschreibung()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr button")[0].Click();

        Assert.Equal("Messreihe 2024, Standort Nord", cut.Find("textarea[readonly]").TextContent);
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_linke_Pfeil_ist_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();
        Assert.True(cut.FindAll(".epos-auswahlpfeile button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_linke_Pfeil_legt_eine_Zeile_an()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 21) };
        int gerufen = 0;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gerufen = id;
            return Zeile(100000, "Ganglinie Süd", id);
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[1].Click();
        cut.FindAll(".epos-auswahlpfeile button")[0].Click();

        Assert.Equal(22, gerufen);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Ganglinie Süd", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Der_rechte_Pfeil_entfernt_GENAU_die_gewaehlte_Zeile()
    {
        // Der Vorlaeufer suchte die erste Zeile mit demselben Namen - bei zwei
        // Zuordnungen derselben Ganglinie also nicht zwingend die markierte.
        var a = Zeile(1, "Ganglinie Nord", 21);
        var b = Zeile(2, "Ganglinie Nord", 21);
        var zeilen = new List<ErzeugerZeile> { a, b };

        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button")!.Click();
        cut.FindAll(".epos-auswahlpfeile button")[1].Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    // =================================================================================
    // Sprung, Abschluss, Tastatur
    // =================================================================================

    /// <summary>
    /// „Bearbeiten…" öffnet die Verwaltung als ÜBERLAGERUNG (iU9-W14b.2) — bis dahin
    /// war es ein Sprung in ein WinForms-Fenster über
    /// <c>Sprungziel.SolarganglinieAdmin</c>, den es nicht mehr gibt.
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_die_Verwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltungGaben: LEERER_SATZ);

        Assert.False(cut.Instance.VerwaltungOffen);
        Knopf(cut, "Bearbeiten...").Click();
        Assert.True(cut.Instance.VerwaltungOffen);
    }

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
    public void Esc_schliesst_mit_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }
}
