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
/// Wärmepumpen Verwaltung — seit W7‑B‑3 (08.09.2026) EIN Dialog: links die Wärmepumpen des
/// Projekts, rechts der Katalog, darunter die Detailansicht der markierten Anlage eingebettet.
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
        Firma = "Bosch",
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

    private static readonly Katalogfilterzeile[] Katalog =
    {
        new Katalogfilterzeile(1, "WP Neu")
            .MitText(Katalogfilterprofil.SpHersteller, "Alpha")
            .MitText(Katalogfilterprofil.SpBezeichner, "WP Neu")
            .MitText(Katalogfilterprofil.SpQuelle, "Sole-Wasser")
            .MitZahl(Katalogfilterprofil.SpNennleistung, 20, 1)
            .MitZahl(Katalogfilterprofil.SpVlMin, 35, 0)
            .MitZahl(Katalogfilterprofil.SpVlMax, 60, 0)
            .MitZahl(Katalogfilterprofil.SpZuheizung, 9, 1)
            .MitKennzeichen(Katalogfilterprofil.SpKuehlen, false)
            .MitZahl(Katalogfilterprofil.SpCop, 4.1, 2)
    };

    private static readonly Katalogfilterprofil Katalogprofil =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Waermepumpe);

    /// <summary>Der Parametersatz der eingebetteten Detailansicht; <paramref name="mangel"/> = Temperaturprüfung schlägt an.</summary>
    private static IReadOnlyDictionary<string, object> AnlageGaben(WaermepumpeAnlageDaten daten, string? mangel = null)
        => new Dictionary<string, object>
        {
            ["Daten"] = daten,
            ["Stammliste"] = new Func<IReadOnlyList<WaermepumpeStammZeile>>(
                () => new[] { new WaermepumpeStammZeile(1, daten.Bezeichner, false),
                              new WaermepumpeStammZeile(2, "WP Neu", false) }),
            ["TemperaturenPruefen"] = new Func<int?, int?, string?>((_, _) => mangel)
        };

    private IRenderedComponent<WaermepumpenDialog> Aufbauen(
        List<WaermepumpeAnlageDaten>? zeilen = null,
        Func<string, WaermepumpeAnlageDaten?>? anlegen = null,
        Action<WaermepumpeAnlageDaten>? uebernehmen = null,
        Action<WaermepumpeAnlageDaten>? entfernen = null,
        bool wizard = false,
        Action<bool>? geschlossen = null,
        string? mangel = null)
        => Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.Katalog, () => Katalog)
            .Add(x => x.Katalogprofil, Katalogprofil)
            .Add(x => x.AnlageGaben, d => AnlageGaben(d, mangel))
            .Add(x => x.Anlegen, anlegen ?? (n => Zeile(n, 20)))
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermepumpenDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>Die Zeilenwahl der PROJEKTliste (das erste Raster).</summary>
    private static IElement Projektwahl(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[index];

    /// <summary>Die Zeilenwahl der KATALOGliste (das zweite Raster).</summary>
    private static IElement Katalogwahl(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[index];

    private static IElement Pfeil(IRenderedComponent<WaermepumpenDialog> cut, int index)
        => cut.FindAll(".epos-zweispalten-uebernahme button")[index];

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_Spalten_der_Projektliste_stehen()
    {
        var cut = Aufbauen();
        var kopf = cut.FindAll(".epos-raster")[0].QuerySelectorAll("th")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "Wahl", "Hersteller", "Typ", "Leistung [kW]", "Vorlauf [°C]",
                             "Rücklauf [°C]", "Betriebsart" }, kopf);
    }

    [Fact]
    public void Zwei_Listen_zwei_Pfeile_und_die_OK_Leiste_stehen()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        var pfeile = cut.FindAll(".epos-zweispalten-uebernahme button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(2, pfeile.Count);
        Assert.Contains(pfeile, t => t.Contains("In das Projekt übernehmen"));
        Assert.Contains(pfeile, t => t.Contains("Aus dem Projekt entfernen"));

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("OK", knoepfe);
        Assert.Contains("❌Abbrechen", knoepfe);
        // Die Wege des Vorlaeufers sind weg: kein "Neu..", kein "Aendern..", keine Ueberlagerung.
        Assert.DoesNotContain("➕ Neu..", knoepfe);
        Assert.DoesNotContain("✏️ Ändern..", knoepfe);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Die_Zeile_zeigt_die_Werte_der_Anlage()
    {
        var cut = Aufbauen();
        var zellen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody td")
                        .Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains("Bosch", zellen);            // Hersteller (W7-B-1)
        Assert.Contains("WP Alpha", zellen);         // Typ = Modellbezeichnung
        Assert.Contains("12", zellen);
        Assert.Contains("35", zellen);
        Assert.Contains("28", zellen);
        Assert.Contains(DbWerte.WP_BETRIEBSART_PARALLEL, zellen);
    }

    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<WaermepumpenDialog>(p => p
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") })
            .Add(x => x.TitelText, "Heat pump management")
            .Add(x => x.KopfbandText, "Enter the heat pump data")
            .Add(x => x.LabelHinzu, "Add to project"));

        Assert.Equal("Heat pump management", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Enter the heat pump data", cut.Find(".epos-kontextzeile").TextContent.Trim());
        Assert.Contains(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t.Contains("Add to project"));
    }

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t == "OK");
    }

    // =================================================================================
    // Die eingebettete Detailansicht
    // =================================================================================

    [Fact]
    public void Die_erste_Zeile_ist_gewaehlt_und_ihre_Detailansicht_steht_eingebettet()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var cut = Aufbauen(zeilen);

        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);
        Assert.True(cut.Instance.DetailOffen);
        Assert.Single(cut.FindAll(".epos-wp-eingebettet"));
        // Die Detailansicht traegt kein eigenes OK - nur die Verwaltung hat eines.
        Assert.Single(cut.FindAll("button").Where(b => b.TextContent.Trim() == "OK"));
    }

    [Fact]
    public void Ohne_Zeile_steht_der_Leersatz()
    {
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten>());

        Assert.False(cut.Instance.DetailOffen);
        Assert.Contains("Links eine Wärmepumpe markieren", cut.Markup);
        Assert.True(Pfeil(cut, 1).HasAttribute("disabled"));     // nichts zu entfernen
    }

    [Fact]
    public void Uebernehmen_aus_der_Datenbank_haengt_die_Zeile_an_und_waehlt_sie()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d));

        Assert.True(Pfeil(cut, 0).HasAttribute("disabled"));     // ohne Katalogwahl gesperrt
        Katalogwahl(cut, 0).Click();
        Pfeil(cut, 0).Click();

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("WP Neu", zeilen[1].Bezeichner);
        Assert.Same(zeilen[1], cut.Instance.Gewaehlt);
        // Die bisherige Zeile ging beim Wechsel ins Modell, die neue sofort.
        Assert.Equal(2, uebernommen.Count);
        Assert.Same(zeilen[1], uebernommen[1]);
    }

    [Fact]
    public void Ein_Zeilenwechsel_uebernimmt_die_bisherige_Zeile()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta");
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { a, b }, uebernehmen: d => uebernommen.Add(d));

        Projektwahl(cut, 1).Click();

        Assert.Same(b, cut.Instance.Gewaehlt);
        Assert.Same(a, uebernommen.Single());
    }

    [Fact]
    public void Ein_Mangel_haelt_den_Zeilenwechsel_an_und_das_Band_nennt_ihn()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta");
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(new List<WaermepumpeAnlageDaten> { a, b },
                           uebernehmen: d => uebernommen.Add(d),
                           mangel: "Die Vorlauftemperatur muss über der Rücklauftemperatur liegen.");

        Projektwahl(cut, 1).Click();

        Assert.Same(a, cut.Instance.Gewaehlt);
        Assert.Empty(uebernommen);
        Assert.Contains("Vorlauftemperatur", cut.Instance.Meldung);
        Assert.Contains("WP Alpha", cut.Instance.Meldung);
    }

    // =================================================================================
    // Entfernen
    // =================================================================================

    [Fact]
    public void Entfernen_trifft_die_ZEILE_und_nicht_den_Index()
    {
        var a = Zeile("WP Alpha");
        var b = Zeile("WP Beta");
        var zeilen = new List<WaermepumpeAnlageDaten> { a, b };
        var entfernt = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, entfernen: d => entfernt.Add(d));

        Projektwahl(cut, 1).Click();
        Pfeil(cut, 1).Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    [Fact]
    public void Nach_dem_Entfernen_wandert_die_Wahl_auf_die_erste_Zeile()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha"), Zeile("WP Beta") };
        var cut = Aufbauen(zeilen);

        Pfeil(cut, 1).Click();
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);

        Pfeil(cut, 1).Click();
        Assert.Null(cut.Instance.Gewaehlt);
        Assert.False(cut.Instance.DetailOffen);
    }

    // =================================================================================
    // Abschluss und Tastatur
    // =================================================================================

    [Fact]
    public void OK_uebernimmt_die_markierte_Zeile_und_meldet_true()
    {
        bool? ergebnis = null;
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var uebernommen = new List<WaermepumpeAnlageDaten>();
        var cut = Aufbauen(zeilen, uebernehmen: d => uebernommen.Add(d), geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
        Assert.Same(zeilen[0], uebernommen.Single());
    }

    [Fact]
    public void OK_mit_Mangel_schliesst_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b, mangel: "Temperaturen prüfen.");

        Knopf(cut, "OK").Click();

        Assert.Null(ergebnis);
        Assert.Contains("Temperaturen prüfen.", cut.Instance.Meldung);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "❌Abbrechen").Click();
        Assert.False(ergebnis);

        bool? zweites = null;
        var cut2 = Aufbauen(geschlossen: b => zweites = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zweites);
    }

    // =====================================================================
    //  W7-B-3 Nachtrag (08.09.2026): Umstellen statt Innenliste
    // =====================================================================

    [Fact]
    public void Umstellen_macht_die_markierte_Zeile_zur_Katalogzeile_ohne_neue_Zeile()
    {
        var zeilen = new List<WaermepumpeAnlageDaten> { Zeile("WP Alpha") };
        var cut = Aufbauen(zeilen);

        Assert.True(Knopf(cut, "Markierte auf diese Wärmepumpe umstellen").HasAttribute("disabled"));

        Katalogwahl(cut, 0).Click();                                   // "WP Neu" markieren
        var umstellen = Knopf(cut, "Markierte auf diese Wärmepumpe umstellen");
        Assert.False(umstellen.HasAttribute("disabled"));
        umstellen.Click();

        Assert.Single(zeilen);                                         // keine neue Zeile
        Assert.Equal("WP Neu", zeilen[0].Bezeichner);
        Assert.Contains("WP Neu", cut.FindAll(".epos-raster")[0].TextContent);
        Assert.Equal(2, cut.FindAll(".epos-raster").Count);            // die Innenliste steht nicht
    }
}
