using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wärmepumpen Verwaltung (iU9-W7.5). Soll ist die Feldkarte von
/// <c>Form_WPAuswahl</c>: acht Zeilen und ein <c>ListView</c> mit fünf Spalten.
/// </summary>
public class WaermepumpenDialogTests : BunitContext
{
    public WaermepumpenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermepumpeAnlageDaten Zeile(string name, int nennleistung = 12) => new()
    {
        Bezeichner = name,
        IdWp = 1,
        Vorlauf = 35,
        Ruecklauf = 28,
        Nennleistung = nennleistung,
        Betriebsart = DbWerte.WP_BETRIEBSART_PARALLEL,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        Nutzungszeit = 24,
        HeizstabLeistung = 6
    };

    private static readonly WaermepumpenKatalogZeile[] Katalog =
    {
        new("Alpha", "WP Neu", "Split", "Außen", 60, 35, 20, 9, "Sole-Wasser", "einstufig", "Heizen")
    };

    /// <summary>Der Parametersatz der Detailansicht — hier ohne Datenbank.</summary>
    private static IReadOnlyDictionary<string, object> AnlageGaben(WaermepumpeAnlageDaten daten)
        => new Dictionary<string, object>
        {
            ["Daten"] = daten,
            ["Stammliste"] = new Func<IReadOnlyList<WaermepumpeStammZeile>>(
                () => new[] { new WaermepumpeStammZeile(1, daten.Bezeichner, false) }),
            ["TemperaturenPruefen"] = new Func<int?, int?, string?>((_, _) => null)
        };

    private IRenderedComponent<WaermepumpenDialog> Aufbauen(
        List<WaermepumpeAnlageDaten>? zeilen = null,
        Func<string, WaermepumpeAnlageDaten?>? anlegen = null,
        Action<WaermepumpeAnlageDaten>? uebernehmen = null,
        Action<WaermepumpeAnlageDaten>? entfernen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null)
        => Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.AnlageGaben, AnlageGaben)
            .Add(x => x.Anlegen, anlegen ?? (n => Zeile(n, 20)))
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermepumpenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_fuenf_Spalten_der_Karte_stehen()
    {
        var cut = Aufbauen();
        var kopf = cut.FindAll(".epos-raster th").Select(e => e.TextContent.Trim()).ToList();

        // Wahl und Aktion sind die beiden Zugaben: die Zeilenmarkierung, die ein
        // Raster nicht kennt, und der Knopf, der den Doppelklick ersetzt.
        Assert.Equal(new[] { "Wahl", "Name", "Leistung [kW]", "Vorlauf [°C]",
                             "Rücklauf [°C]", "Betriebsart", "Aktion" }, kopf);
    }

    [Fact]
    public void Die_drei_Knoepfe_der_Karte_stehen()
    {
        var cut = Aufbauen();
        var knoepfe = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();

        Assert.Contains("➕ Neu..", knoepfe);
        Assert.Contains("✏️ Ändern..", knoepfe);
        Assert.Contains("🗑️ Löschen", knoepfe);
        Assert.Contains("OK", knoepfe);
        Assert.Contains("❌Abbrechen", knoepfe);
    }

    [Fact]
    public void Die_Zeile_zeigt_die_Werte_der_Anlage()
    {
        var cut = Aufbauen();
        var zellen = cut.FindAll(".epos-raster tbody td").Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains("WP Alpha", zellen);
        Assert.Contains("12", zellen);
        Assert.Contains("35", zellen);
        Assert.Contains("28", zellen);
        Assert.Contains(DbWerte.WP_BETRIEBSART_PARALLEL, zellen);
    }

    /// <summary>Die Maske ist lokalisiert (10 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.TitelText, "Heat pump management")
            .Add(x => x.KopfbandText, "Enter the heat pump data")
            .Add(x => x.BtnAendernText, "Change..."));

        Assert.Equal("Heat pump management", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Enter the heat pump data", cut.Find(".epos-kontextzeile").TextContent.Trim());
        Assert.Contains("Change...", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t == "OK");
    }

    // =================================================================================
    // Neu: Katalog, dann Detailansicht
    // =================================================================================

    [Fact]
    public void Neu_zeigt_erst_den_Katalog()
    {
        var cut = Aufbauen();
        Knopf(cut, "➕ Neu..").Click();

        Assert.True(cut.Instance.KatalogOffen);
        Assert.False(cut.Instance.DetailOffen);
    }

    [Fact]
    public void Nach_der_Katalogwahl_erscheint_die_Detailansicht()
    {
        var cut = Aufbauen();
        Knopf(cut, "➕ Neu..").Click();

        var katalog = cut.Find(".epos-ueberlagerung");
        katalog.QuerySelector(".epos-raster tbody tr button")!.Click();
        katalog.QuerySelectorAll("button")
               .First(b => b.TextContent.Trim() == "✔ Auswahl übernehmen").Click();

        Assert.False(cut.Instance.KatalogOffen);
        Assert.True(cut.Instance.DetailOffen);
    }

    [Fact]
    public void Erst_das_OK_der_Detailansicht_haengt_die_Zeile_an()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d));

        Knopf(cut, "➕ Neu..").Click();
        var katalog = cut.Find(".epos-ueberlagerung");
        katalog.QuerySelector(".epos-raster tbody tr button")!.Click();
        katalog.QuerySelectorAll("button")
               .First(b => b.TextContent.Trim() == "✔ Auswahl übernehmen").Click();

        Assert.Single(zeilen);                                    // noch nicht angehaengt

        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("WP Neu", zeilen[1].Bezeichner);
        Assert.Single(uebernommen);
    }

    [Fact]
    public void Ein_Abbruch_in_der_Detailansicht_haengt_nichts_an()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var cut = Aufbauen(zeilen);

        Knopf(cut, "➕ Neu..").Click();
        var katalog = cut.Find(".epos-ueberlagerung");
        katalog.QuerySelector(".epos-raster tbody tr button")!.Click();
        katalog.QuerySelectorAll("button")
               .First(b => b.TextContent.Trim() == "✔ Auswahl übernehmen").Click();

        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.Single(zeilen);
        Assert.False(cut.Instance.DetailOffen);
    }

    [Fact]
    public void Ein_Abbruch_im_Katalog_oeffnet_die_Detailansicht_nicht()
    {
        var cut = Aufbauen();
        Knopf(cut, "➕ Neu..").Click();

        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.False(cut.Instance.KatalogOffen);
        Assert.False(cut.Instance.DetailOffen);
    }

    // =================================================================================
    // Aendern und Ansicht
    // =================================================================================

    [Fact]
    public void Aendern_ist_ohne_Zeile_gesperrt()
    {
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten>());

        Assert.True(Knopf(cut, "✏️ Ändern..").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "🗑️ Löschen").HasAttribute("disabled"));
    }

    [Fact]
    public void Aendern_oeffnet_die_Detailansicht_bedienbar()
    {
        var cut = Aufbauen();
        Knopf(cut, "✏️ Ändern..").Click();

        Assert.True(cut.Instance.DetailOffen);
        // Bedienbar: die OK-Leiste ist da.
        Assert.Contains(cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
                           .Select(b => b.TextContent.Trim()), t => t == "OK");
    }

    [Fact]
    public void Ansicht_zeigt_die_Zeile_NUR_LESEND()
    {
        // A-22: Der Vorlaeufer oeffnete denselben Dialog voll bedienbar und warf das
        // Ergebnis weg - ein Formular, dessen Eingaben still verfallen.
        var cut = Aufbauen();
        cut.Find(".epos-raster tbody tr td:last-child button").Click();

        Assert.True(cut.Instance.DetailOffen);

        var knoepfe = cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
                         .Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("OK", knoepfe);
        Assert.Contains("Schließen", knoepfe);
    }

    [Fact]
    public void Ansicht_nimmt_die_Zeile_und_nicht_die_Datenbank()
    {
        // Die Namenssuche des Vorlaeufers war projektuebergreifend mehrdeutig und
        // uebersah ungespeicherte Zeilen. Hier steht die zweite Zeile da, obwohl sie
        // dieselbe Bezeichnung traegt wie die erste.
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha", 12), Zeile("WP Alpha", 30) };
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-raster tbody tr td:last-child button")[1].Click();

        Assert.Same(zeilen[1], cut.Instance.Gewaehlt);
    }

    // =================================================================================
    // Loeschen
    // =================================================================================

    [Fact]
    public void Loeschen_trifft_die_ZEILE_und_nicht_den_Index()
    {
        // Der Vorlaeufer nahm list_werzmodel.RemoveAt(listView.SelectedIndex) - im
        // Assistenten fuehrt dieselbe Liste alle Erzeugertypen, und der Anzeigeindex
        // traf dort eine fremde Anlage.
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta");
        var zeilen = new List<WaermepumpeAnlageDaten> { a, b };

        var entfernt = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, entfernen: d => entfernt.Add(d));

        // Jede Zeile traegt ZWEI Knoepfe (Zeilenwahl und "Ansicht") - deshalb die
        // Wahl ueber die Zeile und nicht ueber einen fortlaufenden Knopfindex.
        cut.FindAll(".epos-raster tbody tr")[1].QuerySelector("button")!.Click();
        Knopf(cut, "🗑️ Löschen").Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    [Fact]
    public void Nach_dem_Loeschen_wandert_die_Wahl_auf_die_erste_Zeile()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha"), Zeile("WP Beta") };
        var cut = Aufbauen(zeilen);

        Knopf(cut, "🗑️ Löschen").Click();
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);

        Knopf(cut, "🗑️ Löschen").Click();
        Assert.Null(cut.Instance.Gewaehlt);
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
        Knopf(cut2, "❌Abbrechen").Click();
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_nur_wenn_keine_Ueberlagerung_offen_ist()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "➕ Neu..").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);

        bool? zweites = null;
        var cut2 = Aufbauen(geschlossen: b => zweites = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zweites);
    }
}
