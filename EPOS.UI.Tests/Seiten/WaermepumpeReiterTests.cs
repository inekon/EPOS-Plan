using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der WAERMEPUMPEN-Reiter (iU9-W11b.4), Vorbild <c>tabPage_Wärmepumpe</c> mit
/// zehn Feldern, zwei Rastern, der Erdreichzeile, <c>chart4</c> und drei
/// Unterblaettern.
///
/// <para>Soll: die Felder aus dem DTO, „-" ohne Bivalenzpunkt, die
/// Pufferkapazitaetszeile NUR ohne Speicherliste, das dritte Unterblatt nur mit
/// Temperaturreihen, der Erdreich-Warnbanner, der Doppelklick auf eine
/// Modulzeile und der Sortiertumschalter.</para>
/// </summary>
public class WaermepumpeReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;
    private readonly List<Bildauftrag> _auftraege = new();

    public WaermepumpeReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
    }

    private static SimulationErgebnisCtrl.WaermepumpeErgebnis Erg(bool bivalenz = true,
                                                                  bool puffer = true,
                                                                  bool erdreich = false)
    {
        var e = new SimulationErgebnisCtrl.WaermepumpeErgebnis
        {
            DeckungProzent = 62.5,
            BivalenzpunktVorhanden = bivalenz,
            Bivalenzpunkt = -3.5,
            StufeneingangMwh = 480.25,
            RestwaermeMwh = 180.0,
            StromverbrauchMwh = 75.0,
            HeizstabStromverbrauchMwh = 2.5,
            WaermeproduktionMwh = 300.0,
            Vollbenutzungsstunden = 1856.0,
            MinSpkLeistungKw = 20.22,
            PufferVolumenKwh = 1160.0
        };
        e.Module.Add(new SimulationErgebnisCtrl.WpModulZeile("WP 1", 30.0, 300.0, 75.0, 2.5, 1856.0));
        if (puffer)
        {
            e.Puffer.Add(new SimulationErgebnisCtrl.PufferZeile(
                "Puffer 1", "Senke", 13.9, 2947.0, 2946.0, 12.0, 6627.4, 55.0, true));
        }
        if (erdreich)
        {
            e.ErdreichHinweise.Add("Sonde 1: 98 W/m — VDI 4640 überschritten");
            e.ErdreichWarnung = true;
        }
        return e;
    }

    private IRenderedComponent<WaermepumpeReiter> Zeichnen(
        SimulationErgebnisCtrl.WaermepumpeErgebnis? erg,
        bool temperaturen = false, Action? modul = null, Action? csv = null)
        => Render<WaermepumpeReiter>(p =>
        {
            p.Add(x => x.Daten, erg);
            p.Add(x => x.Bild, a => { _auftraege.Add(a); return new byte[] { 1 }; });
            p.Add(x => x.Speichertemperaturen, temperaturen);
            if (modul is not null) p.Add(x => x.ModulOeffnen, EventCallback.Factory.Create(this, modul));
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
        });

    // =====================================================================

    [Fact]
    public void Die_Felder_kommen_aus_dem_DTO()
    {
        var seite = Zeichnen(Erg());
        string text = seite.Markup;

        Assert.Contains("62,50", text);       // Deckungsgrad
        Assert.Contains("-3,50", text);       // Bivalenzpunkt
        Assert.Contains("1856", text);        // Vollbenutzungsstunden, F0
        Assert.Contains("20,22", text);       // Mindest-Spitzenkesselleistung
    }

    /// <summary>„-" statt einer Zahl, wenn der Lauf keinen Bivalenzpunkt kennt.</summary>
    [Fact]
    public void Ohne_Bivalenzpunkt_steht_ein_Strich()
    {
        var seite = Zeichnen(Erg(bivalenz: false));
        Assert.Contains("<dd>-</dd>", seite.Markup);
    }

    /// <summary>
    /// Mit Speicherliste uebernimmt die Tabelle; die Altzeile „Kapazitaet des
    /// Pufferspeichers" bleibt dann weg (<c>PufferspeicherErgebnisAnzeigen</c>
    /// :2427-2440).
    /// </summary>
    [Fact]
    public void Mit_Speicherliste_entfaellt_die_Kapazitaetszeile()
    {
        var mit = Zeichnen(Erg(puffer: true));
        Assert.DoesNotContain("Kapazität des Pufferspeichers", mit.Markup);

        var ohne = Zeichnen(Erg(puffer: false));
        Assert.Contains("Kapazität des Pufferspeichers", ohne.Markup);
    }

    /// <summary>Die Modultabelle: sechs Spalten, je Modul eine Zeile.</summary>
    [Fact]
    public void Die_Modultabelle_hat_sechs_Spalten()
    {
        var seite = Zeichnen(Erg());
        var raster = seite.FindAll("table.epos-raster");

        Assert.Equal(2, raster.Count);                               // Module und Puffer
        Assert.Equal(6, raster[0].QuerySelectorAll("thead th").Length);
        Assert.Equal(8, raster[1].QuerySelectorAll("thead th").Length);
    }

    /// <summary>
    /// Der Kombispeicher bekommt „ *" und den erklaerenden Mouseover
    /// (Etappe D4, D5b-Restpunkt 4).
    /// </summary>
    [Fact]
    public void Die_Kombispeicherzeile_traegt_Stern_und_Mouseover()
    {
        var seite = Zeichnen(Erg());
        var zelle = seite.FindAll("table.epos-raster")[1].QuerySelectorAll("td")[6];

        Assert.Contains("*", zelle.TextContent);
        Assert.False(string.IsNullOrEmpty(zelle.GetAttribute("title")));
    }

    /// <summary>Die VDI-4640-Warnung erreicht den Anwender als Banner.</summary>
    [Fact]
    public void Die_Erdreichpruefung_erscheint_als_Warnbanner()
    {
        var ohne = Zeichnen(Erg());
        Assert.Empty(ohne.FindAll("[role='alert']"));

        var mit = Zeichnen(Erg(erdreich: true));
        Assert.Single(mit.FindAll("[role='alert']"));
    }

    /// <summary>
    /// Zwei Unterblaetter ohne Temperaturreihen, drei mit — die Seite haengt
    /// sich nur ein, wenn der Lauf eine Reihe traegt.
    /// </summary>
    [Fact]
    public void Das_dritte_Unterblatt_haengt_an_den_Temperaturreihen()
    {
        Assert.Equal(2, Zeichnen(Erg()).FindAll("button[role='tab']").Count);
        Assert.Equal(3, Zeichnen(Erg(), temperaturen: true).FindAll("button[role='tab']").Count);
    }

    /// <summary>
    /// Befund W11-B18: Der Umschalter wechselt NUR die Schalterstellung des
    /// Bildauftrags — die Reihen bleiben in beiden Zweigen dieselben.
    /// </summary>
    [Fact]
    public void Der_Sortiertschalter_wechselt_nur_den_Bildauftrag()
    {
        var seite = Zeichnen(Erg());
        _auftraege.Clear();

        seite.Find("input[type='checkbox']").Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.WpProduktion && a.Sortiert);
    }

    [Fact]
    public void Der_Doppelklick_auf_eine_Modulzeile_meldet_sich()
    {
        int gerufen = 0;
        var seite = Zeichnen(Erg(), modul: () => gerufen++);

        seite.FindAll("table.epos-raster")[0].QuerySelectorAll("tbody tr")[0].DoubleClick();
        Assert.Equal(1, gerufen);
    }

    /// <summary>Ohne Lauf mit Waermepumpe steht die Rubrik LEER da.</summary>
    [Fact]
    public void Ohne_Waermepumpe_bleibt_die_Rubrik_leer()
    {
        var seite = Zeichnen(null);

        Assert.Empty(seite.FindAll("table"));
        Assert.Empty(seite.FindAll("button[role='tab']"));
        Assert.Contains("Keine Simulationsdaten", seite.Markup);
    }
}
