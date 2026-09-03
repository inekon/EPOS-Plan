using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wärmepumpen-Katalog (iU9-W7.1). Soll ist die Feldkarte von
/// <c>Form_WPFilterAuswahl</c>: 29 Zeilen — sieben Klapplisten, vier Zahlenfelder,
/// ein Suchfeld, ein Raster mit sieben Spalten und vier Knöpfe.
/// </summary>
public class WaermepumpenKatalogDialogTests : BunitContext
{
    private static readonly WaermepumpenKatalogZeile[] Katalog =
    {
        new("Alpha", "CS-070", "Monoblock", "Innen", 55, 35, 7.0, 3, "Luft-Wasser", "stetig", "Heizen"),
        new("Alpha", "CS-127", "Split", "Außen", 60, 35, 12.7, 6, "Luft-Wasser", "einstufig", "Heizen/Kühlen"),
        new("Beta", "BX-200", "Monoblock", "Innen", 45, 30, 20.0, 9, "Sole-Wasser", "zweistufig", "Heizen"),
        new("Gamma", "cs-990", "Monoblock", "Außen", 35, 25, 99.0, 0, "Sole-Wasser", "stetig", "Heizen")
    };

    public WaermepumpenKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<WaermepumpenKatalogDialog> Aufbauen(
        Action<string?>? geschlossen = null,
        IReadOnlyList<WaermepumpenKatalogZeile>? zeilen = null)
        => Render<WaermepumpenKatalogDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? Katalog)
            .Add(x => x.Geschlossen, n => geschlossen?.Invoke(n)));

    /// <summary>Die Datenzeilen des Rasters (ohne Kopfzeile).</summary>
    private static int Trefferzahl(IRenderedComponent<WaermepumpenKatalogDialog> cut)
        => cut.FindAll(".epos-raster tbody tr").Count;

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        // Sieben Klapplisten, vier Zahlenfelder, ein Suchfeld.
        Assert.Equal(7, cut.FindAll(".epos-zahlenraster select").Count);
        Assert.Equal(5, cut.FindAll(".epos-zahlenraster input").Count);

        // Vier Knoepfe: Filtern, Reset, Uebernehmen, Abbrechen.
        var knopftexte = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Daten filtern", "Filter Reset", "✔ Auswahl übernehmen", "Abbrechen" },
                     knopftexte);
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Designer()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        foreach (string soll in new[]
                 {
                     "Hersteller", "Auslegung", "Funktionsprinzip", "Regelung", "Bauart",
                     "Aufstellung", "Zuheizung", "VLT Min [°C]", "VLT Max [°C]",
                     "Leist. Min [kW]", "Leist. Max [kW]", "Modell filtern (z.B. CS*7*)"
                 })
            Assert.Contains(soll, texte);
    }

    [Fact]
    public void Die_sieben_Spalten_des_Rasters_stehen()
    {
        var cut = Aufbauen();
        var kopf = cut.FindAll(".epos-raster th").Select(e => e.TextContent.Trim()).ToList();

        // Die achte Spalte ist die Zeilenwahl - sie ersetzt die Zeilenmarkierung des
        // DataGridView, die ein Raster nicht kennt.
        Assert.Equal(new[] { "Wahl", "Hersteller", "Modell", "VLT max [°C]", "VLT min [°C]",
                             "Leistung [kW]", "Zuheizer [kW]", "Bauart" }, kopf);
    }

    /// <summary>Englische Beschriftungen: Die Maske war NICHT lokalisiert (W7.9).</summary>
    [Fact]
    public void Die_Texte_lassen_sich_von_aussen_setzen()
    {
        var cut = Render<WaermepumpenKatalogDialog>(p => p
            .Add(x => x.Zeilen, Katalog)
            .Add(x => x.TitelText, "Heat pump catalogue")
            .Add(x => x.LabelHersteller, "Manufacturer")
            .Add(x => x.TextAlle, "All")
            .Add(x => x.BtnResetText, "Reset filter"));

        Assert.Equal("Heat pump catalogue", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Manufacturer", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("All", cut.FindAll("option").Select(o => o.TextContent));
        Assert.Contains("Reset filter", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    // =================================================================================
    // Vorbelegung
    // =================================================================================

    [Fact]
    public void Die_Klapplisten_beginnen_mit_Alle_und_zeigen_alles()
    {
        var cut = Aufbauen();

        // Erste Klappliste = Hersteller: "Alle" + Alpha, Beta, Gamma (sortiert).
        var hersteller = cut.FindAll(".epos-zahlenraster select")[0]
                            .QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal(new[] { "Alle", "Alpha", "Beta", "Gamma" }, hersteller);

        Assert.Equal(Katalog.Length, Trefferzahl(cut));
    }

    [Fact]
    public void Die_Hoechstwerte_kommen_aus_den_Daten()
    {
        var cut = Aufbauen();
        var zahlen = cut.FindAll(".epos-zahlenraster input").Take(4)
                        .Select(e => e.GetAttribute("value")).ToList();

        // Reihenfolge: VLT Min, VLT Max, Leist. Min, Leist. Max.
        // Nachkommastelle wie im Vorlaeufer (NumericUpDown.DecimalPlaces = 1).
        Assert.Equal("0,0", zahlen[0]);
        Assert.Equal("60,0", zahlen[1]);
        Assert.Equal("0,0", zahlen[2]);
        Assert.Equal("99,0", zahlen[3]);
    }

    [Fact]
    public void Die_Herleitungszeile_nennt_die_Trefferzahl()
    {
        // Der Vorlaeufer schrieb sie in die Fensterueberschrift (A-7).
        var cut = Aufbauen();
        Assert.Equal("4 Wärmepumpen gefunden", cut.Find(".epos-herleitung").TextContent.Trim());
    }

    // =================================================================================
    // Filtern
    // =================================================================================

    [Fact]
    public void Eine_Klappliste_engt_die_Treffer_ein()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-zahlenraster select")[0].Change("1");   // Hersteller = Alpha
        Assert.Equal(2, Trefferzahl(cut));
        Assert.Equal("2 Wärmepumpen gefunden", cut.Find(".epos-herleitung").TextContent.Trim());
    }

    [Fact]
    public void Zwei_Klapplisten_wirken_zusammen()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-zahlenraster select")[0].Change("1");   // Hersteller = Alpha
        cut.FindAll(".epos-zahlenraster select")[4].Change("2");   // Bauart = Split
        Assert.Equal(1, Trefferzahl(cut));
        Assert.Contains("CS-127", cut.Find(".epos-raster tbody").TextContent);
    }

    [Fact]
    public void Das_Suchfeld_kennt_Platzhalter_und_Teilsuche()
    {
        var cut = Aufbauen();
        var suche = cut.FindAll(".epos-zahlenraster input")[4];

        // Das Beispiel aus der Beschriftung.
        suche.Input("CS*7*");
        Assert.Equal(2, Trefferzahl(cut));

        // Genau ein Zeichen.
        suche.Input("CS-0?0");
        Assert.Equal(1, Trefferzahl(cut));

        // Klartext = Teilsuche, ohne Ruecksicht auf Gross- und Kleinschreibung.
        suche.Input("cs");
        Assert.Equal(3, Trefferzahl(cut));
    }

    [Fact]
    public void Ein_Bereichsfilter_greift()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-zahlenraster input")[0].Input("50");   // VLT Min
        Assert.Equal(2, Trefferzahl(cut));
    }

    [Fact]
    public void Reset_stellt_alles_zurueck()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-zahlenraster select")[0].Change("1");
        cut.FindAll(".epos-zahlenraster input")[4].Input("BX");
        Assert.Equal(0, Trefferzahl(cut));

        cut.FindAll(".epos-leiste button")[1].Click();             // "Filter Reset"

        Assert.Equal(Katalog.Length, Trefferzahl(cut));
        Assert.Equal("0", cut.FindAll(".epos-zahlenraster select")[0].GetAttribute("value"));
        Assert.Equal("60,0", cut.FindAll(".epos-zahlenraster input")[1].GetAttribute("value"));
    }

    // =================================================================================
    // Uebernehmen, Abbrechen, Tastatur
    // =================================================================================

    [Fact]
    public void Uebernehmen_ist_ohne_Zeile_gesperrt()
    {
        string? ergebnis = "nicht gerufen";
        var cut = Aufbauen(n => ergebnis = n);

        var uebernehmen = cut.FindAll(".epos-leiste button")[2];
        Assert.True(uebernehmen.HasAttribute("disabled"));

        uebernehmen.Click();
        Assert.Equal("nicht gerufen", ergebnis);
    }

    [Fact]
    public void Uebernehmen_liefert_den_Bezeichner_der_gewaehlten_Zeile()
    {
        string? ergebnis = null;
        var cut = Aufbauen(n => ergebnis = n);

        cut.FindAll(".epos-raster tbody tr")[1].QuerySelector("button")!.Click();
        cut.FindAll(".epos-leiste button")[2].Click();

        Assert.Equal("CS-127", ergebnis);
    }

    [Fact]
    public void Die_Zeilenwahl_ist_ein_Beruehrungsziel_statt_eines_Doppelklicks()
    {
        // A-8: Der Vorlaeufer nahm die Zeile per CellDoubleClick an. Ein Doppelklick
        // ist kein Beruehrungsziel (M2/iL4) - wie in W5 A-3 wird daraus ein Knopf.
        var cut = Aufbauen();
        Assert.Equal(Katalog.Length, cut.FindAll(".epos-raster tbody tr button").Count);
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        string? ergebnis = "nicht gerufen";
        var cut = Aufbauen(n => ergebnis = n);

        cut.FindAll(".epos-leiste button")[3].Click();
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_mit_null()
    {
        string? ergebnis = "nicht gerufen";
        var cut = Aufbauen(n => ergebnis = n);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Ein_leerer_Katalog_bricht_die_Vorbelegung_nicht()
    {
        // Max() ueber eine leere Liste wirft; GroessterVorlauf faengt das ab.
        var cut = Aufbauen(zeilen: Array.Empty<WaermepumpenKatalogZeile>());

        Assert.Equal(0, Trefferzahl(cut));
        Assert.Equal("0 Wärmepumpen gefunden", cut.Find(".epos-herleitung").TextContent.Trim());
    }
}
