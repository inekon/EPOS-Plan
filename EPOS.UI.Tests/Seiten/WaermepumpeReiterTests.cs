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
/// <para>Seit dem Anwenderwunsch 08.09.2026 (<b>W11b‑B‑17</b>) dazu die WAHL DER
/// REIHEN: je Bild eine Schalterzeile, alle vorbelegt an, die Wahl im
/// Bildauftrag.</para>
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

    // Die DREI Schalterzeilen in der Reihenfolge des Markups: die Streuwolke
    // steht ueber den Unterblaettern, im Blatt „Wärmeproduktion" folgen erst
    // „sortiert" (die Darstellungsart) und dann die vier Reihen (W11b‑B‑17).
    private const int STREUWOLKE = 0;
    private const int SORTIERT = 1;
    private const int GANGLINIE = 2;

    /// <summary>Die Beschriftungen einer Schalterzeile, in ihrer Reihenfolge.</summary>
    private static string[] Zeile(IRenderedComponent<WaermepumpeReiter> seite, int nr)
        => seite.FindAll("div.epos-simerg-schalter")[nr]
                .QuerySelectorAll("label.epos-schalter")
                .Select(l => l.TextContent.Trim()).ToArray();

    /// <summary>
    /// Das Kaestchen Nr. <paramref name="k"/> der Schalterzeile Nr.
    /// <paramref name="nr"/>. Ueber die ZEILE und nicht ueber die Beschriftung,
    /// weil „Wärmeproduktion" und „Heizstab" in BEIDEN Bildern vorkommen.
    /// </summary>
    private static AngleSharp.Dom.IElement Kasten(
        IRenderedComponent<WaermepumpeReiter> seite, int nr, int k)
        => seite.FindAll("div.epos-simerg-schalter")[nr]
                .QuerySelectorAll("input[type=checkbox]")[k];

    // =====================================================================

    [Fact]
    public void Die_Felder_kommen_aus_dem_DTO()
    {
        var seite = Zeichnen(Erg());
        string text = seite.Markup;

        Assert.Contains("62,50", text);       // Deckungsgrad
        Assert.Contains("-3,50", text);       // Bivalenzpunkt
        Assert.Contains("1.856", text);       // Vollbenutzungsstunden, N0 (W11b-B-13)
        Assert.Contains("20,22", text);       // Mindest-Spitzenkesselleistung
    }

    /// <summary>
    /// <b>W11b‑B‑23 (09.09.2026).</b> Die zehn Zeilen standen bis W11b‑B‑15 in
    /// EINER Liste, in der sich Wärmemengen, Stromverbräuche und
    /// Auslegungsgrößen abwechselten; seither tragen Gruppen die Ordnung. Jetzt
    /// sind es dunkle BALKEN statt <c>h3</c> — Wärme und Strom sind Hauptgruppen
    /// und stehen NEBENEINANDER in der ersten Rasterzeile, die Auslegung darunter
    /// (dieselbe Bauform wie im Bedarfs-, Übersichts-, Heizkessel- und
    /// BHKW-Reiter). Die Zeilenfolge je Gruppe bleibt die von W11b‑B‑15.
    /// </summary>
    [Fact]
    public void Die_zehn_Felder_stehen_in_drei_Gruppen_mit_Balken()
    {
        var seite = Zeichnen(Erg(puffer: false));

        Assert.Equal(new[] { "Wärme", "Strom", "Auslegung" },
                     seite.FindAll("h2.epos-gruppenkopf-titel").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Empty(seite.FindAll("h3.epos-untergruppe"));
        Assert.Contains(seite.FindAll("div.epos-simerg-spalten"),
                        z => z.QuerySelectorAll("dl.epos-simerg-werte").Length == 2);

        var listen = seite.FindAll("dl.epos-simerg-werte");
        Assert.Equal(3, listen.Count);

        Assert.Equal(
            new[] { "Wärmebedarfsdeckung:", "Wärmebedarf:", "Wärmeproduktion WP:", "Restwärmebedarf:" },
            listen[0].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Stromverbrauch WP:", "Stromverbrauch Heizstab:" },
            listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Bivalenzpunkt:", "durchschnittliche Vollbenutzungsstunden:",
                    "Minimale Spitzenkesselleistung:", "Kapazität des Pufferspeichers:" },
            listen[2].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>Die Restwärme schliesst die Wärmegruppe betont ab (W11b‑B‑15).</summary>
    [Fact]
    public void Die_Restwaerme_schliesst_die_Waermegruppe_betont_ab()
    {
        var seite = Zeichnen(Erg());
        var betont = seite.FindAll("dt.epos-simerg-abschluss");

        Assert.Single(betont);
        Assert.Equal("Restwärmebedarf:", betont[0].TextContent.Trim());
        Assert.Equal(2, seite.FindAll("dd.epos-simerg-abschluss").Count);
    }

    /// <summary>
    /// <b>W11b‑B‑20 (09.09.2026).</b> Die Kennzahlenliste steht ALLEIN in ihrer
    /// Rasterzeile — neben ihr steht kein zweiter Block, darunter spannt schon das
    /// Diagramm über die volle Breite. Ohne <c>epos-simerg-kennzahlenzeile</c> sass
    /// sie in EINER Spalte des auto-fit-Rasters, und lange Beschriftungen wie
    /// „durchschnittliche Vollbenutzungsstunden:“ liefen rechts heraus.
    /// </summary>
    [Fact]
    public void Die_Kennzahlenliste_nimmt_die_ganze_Rasterzeile()
    {
        var seite = Zeichnen(Erg());
        Assert.Single(seite.FindAll("section.epos-simerg-kennzahlenzeile"));
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

        Kasten(seite, SORTIERT, 0).Change(true);

        Assert.True(seite.Instance.Sortiert);

        Bildauftrag gang = _auftraege.Last(a => a.Bild == Bilder.WpProduktion);
        Assert.True(gang.Sortiert);
        Assert.Equal(new[] { "HEIZWAERMEBEDARF", "WARMWASSERBEDARF", "WAERMEPRODUKTION", "HEIZSTAB" },
                     gang.Reihen!.ToArray());
    }

    // =====================================================================
    //  W11b‑B‑17 — „Die Graphen der Diagramme sollten auswählbar sein (select)
    //  für beide Grafiken." (Anwenderwunsch 08.09.2026)
    // =====================================================================

    /// <summary>
    /// Je Reihe ein Schalter, die Beschriftung DIESELBE wie in der Legende des
    /// Bildes — der Anwender hakt ab, was er dort liest. Alle stehen beim Aufbau
    /// AN: Das Bild sieht aus wie bisher, bis er etwas abwählt.
    /// </summary>
    [Fact]
    public void Beide_Diagramme_tragen_je_Reihe_einen_Schalter()
    {
        var seite = Zeichnen(Erg());

        Assert.Equal(new[] { "Wärmebedarf", "Heizstab", "Wärmeproduktion" },
                     Zeile(seite, STREUWOLKE));
        Assert.Equal(new[] { "sortiert" }, Zeile(seite, SORTIERT));
        Assert.Equal(new[] { "Heizwärmebedarf", "Warmwasserbedarf", "Wärmeproduktion", "Heizstab" },
                     Zeile(seite, GANGLINIE));

        // Alle REIHEN stehen an — „sortiert" ist keine Reihe, sondern die
        // Darstellungsart, und bleibt aus wie bisher.
        foreach (int zeile in new[] { STREUWOLKE, GANGLINIE })
            Assert.All(seite.FindAll("div.epos-simerg-schalter")[zeile]
                            .QuerySelectorAll("input[type=checkbox]"),
                       k => Assert.True(k.HasAttribute("checked")));
        Assert.False(Kasten(seite, SORTIERT, 0).HasAttribute("checked"));

        Assert.Equal(new[] { "WAERMEBEDARF", "HEIZSTAB", "WAERMEPRODUKTION" },
                     seite.Instance.GewaehlteReihenStreuwolke.ToArray());
        Assert.Equal(new[] { "HEIZWAERMEBEDARF", "WARMWASSERBEDARF", "WAERMEPRODUKTION", "HEIZSTAB" },
                     seite.Instance.GewaehlteReihenProduktion.ToArray());
    }

    /// <summary>
    /// Die Abwahl gibt den Bildauftrag OHNE diesen Schluessel weiter — und nur
    /// diesen: Jedes der zwei Bilder führt seine eigene Wahl.
    /// </summary>
    [Fact]
    public void Die_Abwahl_nimmt_die_Reihe_aus_dem_Bildauftrag()
    {
        var seite = Zeichnen(Erg());
        _auftraege.Clear();

        Kasten(seite, STREUWOLKE, 1).Change(false);      // Heizstab der Streuwolke
        Kasten(seite, GANGLINIE, 0).Change(false);       // Heizwärmebedarf der Ganglinie

        Assert.Equal(new[] { "WAERMEBEDARF", "WAERMEPRODUKTION" },
                     _auftraege.Last(a => a.Bild == Bilder.WpLeistungTemperatur).Reihen!.ToArray());
        Assert.Equal(new[] { "WARMWASSERBEDARF", "WAERMEPRODUKTION", "HEIZSTAB" },
                     _auftraege.Last(a => a.Bild == Bilder.WpProduktion).Reihen!.ToArray());
    }

    /// <summary>
    /// ALLE abgewählt heisst KEINE Reihe und nicht „alle": Der Auftrag trägt eine
    /// LEERE Liste, aus der die Hülle den Leerhinweis des Renderers zeichnet.
    /// Keine Ausnahme, kein Sonderfall.
    /// </summary>
    [Fact]
    public void Alle_Reihen_abgewaehlt_geben_eine_leere_Liste()
    {
        var seite = Zeichnen(Erg());
        _auftraege.Clear();

        for (int k = 0; k < 3; k++) Kasten(seite, STREUWOLKE, k).Change(false);

        Assert.Empty(seite.Instance.GewaehlteReihenStreuwolke);
        Assert.Empty(_auftraege.Last(a => a.Bild == Bilder.WpLeistungTemperatur).Reihen!);
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
