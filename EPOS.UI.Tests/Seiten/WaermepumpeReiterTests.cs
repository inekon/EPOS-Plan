using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
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
/// <para>Seit der Etappe DG-E3 stehen ALLE VIER Bilder des Reiters im Baustein
/// <c>DiagrammSvg</c>. Die STREUWOLKE zählt auf x die Außentemperatur statt der
/// Stunde (Gruppe (b)); was x bedeutet, sagt seit DG-E3-11 ihr Modell, nicht der
/// Aufrufer.</para>
/// </summary>
public class WaermepumpeReiterTests : EposBunitContext
{
    private readonly List<Bildauftrag> _auftraege = new();

    public WaermepumpeReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
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
            p.Add(x => x.Modell, Modell);
            p.Add(x => x.Speichertemperaturen, temperaturen);
            if (modul is not null) p.Add(x => x.ModulOeffnen, EventCallback.Factory.Create(this, modul));
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
        });

    /// <summary>
    /// Die Zeichenmodelle ALLER VIER Bilder, nach Bild, Schalterstellung und
    /// Reihenwahl getrennt und je EINMAL gebaut. Der Baustein <c>DiagrammSvg</c>
    /// vergleicht die Modellreferenz; ein je Zeichenlauf neu gebautes Modell setzte
    /// seinen Baum jedes Mal neu und nähme ihm Zoom und abgewählte Reihe.
    /// </summary>
    private readonly Dictionary<string, Zeichenmodell> _modelle = new();

    private Zeichenmodell? Modell(Bildauftrag a)
    {
        _auftraege.Add(a);

        string schluessel = a.Schluessel;
        if (_modelle.TryGetValue(schluessel, out Zeichenmodell? vorhanden)) return vorhanden;

        Zeichenmodell neu = a.Bild switch
        {
            Bilder.Speichertemperaturen => Temperaturbild(),
            Bilder.WpLeistungTemperatur => Streuwolke(),
            _ => Gangbild(a.Bild, a.Sortiert)
        };
        _modelle[schluessel] = neu;
        return neu;
    }

    /// <summary>
    /// Die STREUWOLKE (Etappe DG-E3, Gruppe (b)): x ist die Außentemperatur, jede
    /// Reihe eine Punktwolke mit ihrer x-Stelle je Punkt.
    /// </summary>
    private static Zeichenmodell Streuwolke()
    {
        var punkte = new List<(double, double)>();
        for (int i = 0; i < 48; i++) punkte.Add((-15.0 + i * 0.75, 20.0 + i));

        return ChartRenderer.StreuwolkeModell(
            "Leistung über Außentemperatur", "°C", "kW",
            new[] { new ChartRenderer.Punktreihe("Wärmebedarf", punkte, ChartRenderer.C_WP) });
    }

    /// <summary>Eine kurze, echte Ganglinie (eine Woche) statt eines Jahres.</summary>
    private static Zeichenmodell Gangbild(string name, bool sortiert)
    {
        var werte = new double[168];
        for (int i = 0; i < werte.Length; i++)
            werte[i] = 60.0 + 40.0 * Math.Sin(2 * Math.PI * i / 24.0);
        if (sortiert) Array.Sort(werte, (x, y) => y.CompareTo(x));

        return ChartRenderer.ErzeugerStapelModell(
            name,
            new[] { new ChartRenderer.Reihe("Wärmeproduktion", werte, ChartRenderer.C_WP) },
            Array.Empty<ChartRenderer.Reihe>(), null,
            "kW", ChartRenderer.Achse.Jahresstunden, sortiert);
    }

    /// <summary>Die zwei Speicherschichten — dasselbe Bild wie im Kern, nur kurz.</summary>
    private static Zeichenmodell Temperaturbild()
    {
        var oben = new double[168];
        var unten = new double[168];
        for (int i = 0; i < oben.Length; i++)
        {
            oben[i] = 55.0 + 5.0 * Math.Sin(2 * Math.PI * i / 24.0);
            unten[i] = 38.0 + 4.0 * Math.Sin(2 * Math.PI * i / 24.0);
        }

        return ChartRenderer.TemperaturverlaufModell(
            "Speichertemperaturen",
            new[]
            {
                new ChartRenderer.Reihe("Puffer 1 oben", oben, ChartRenderer.C_SPEICHER[0]),
                new ChartRenderer.Reihe("Puffer 1 unten", unten, ChartRenderer.C_QUELLTEMPERATUR)
            },
            true);
    }

    // Die VIER Schalterzeilen in der Reihenfolge des Markups: über der
    // Modultabelle „Heizstab in die JAZ einrechnen", im Blatt „Wärmeproduktion"
    // erst „sortiert" (die Darstellungsart) und dann die vier Reihen
    // (W11b‑B‑17); die Streuwolke steht mit der Auslegung am Ende des Reiters,
    // unter den Unterblaettern.
    private const int JAZ = 0;
    private const int SORTIERT = 1;
    private const int GANGLINIE = 2;
    private const int STREUWOLKE = 3;

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
    public void Die_zwoelf_Felder_stehen_in_drei_Gruppen_mit_Balken()
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
            new[] { "Stromverbrauch WP:", "Stromverbrauch Heizstab:",
                    "Jahresarbeitszahl WP:", "Jahresarbeitszahl mit Heizstab:" },
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

    /// <summary>Die Modultabelle: sieben Spalten (mit der JAZ), je Modul eine Zeile.</summary>
    [Fact]
    public void Die_Modultabelle_hat_sieben_Spalten()
    {
        var seite = Zeichnen(Erg());
        var raster = seite.FindAll("table.epos-raster");

        Assert.Equal(2, raster.Count);                               // Module und Puffer
        Assert.Equal(7, raster[0].QuerySelectorAll("thead th").Length);
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

    // =====================================================================
    //  VIER BILDER, EIN WEG (Etappe DG-E3, Gruppen (a) und (b))
    //
    //  Alle vier stehen im Baustein DiagrammSvg: Der Ausschnitt ist die
    //  viewBox ihrer Zeichenflaeche. Bei der Streuwolke zaehlt x die
    //  AUSSENTEMPERATUR - was x bedeutet, sagt ihr Modell (DG-E3-11), und die
    //  Spanne laesst sich dort ebenso aufziehen wie eine Stundenspanne.
    // =====================================================================

    /// <summary>
    /// Das SVG-Diagramm des sichtbaren UNTERBLATTS — es steht neben der Streuwolke,
    /// die unter den Blaettern liegt, also wird die ausdruecklich uebergangen.
    /// </summary>
    private static DiagrammSvg Bild(IRenderedComponent<WaermepumpeReiter> seite)
        => seite.FindComponents<DiagrammSvg>()
                .Select(b => b.Instance)
                .First(b => b.Kennung != "simerg-wp-streuwolke");

    /// <summary>
    /// <b>Streuwolke und Produktionsbild stehen nebeneinander</b> — beide als
    /// <c>DiagrammSvg</c>, jedes unter seiner eigenen Kennung. Ein Pixelbild führt
    /// dieser Reiter nicht mehr.
    /// </summary>
    [Fact]
    public void Streuwolke_und_Produktionsbild_stehen_nebeneinander()
    {
        var seite = Zeichnen(Erg());

        Assert.Empty(seite.FindAll("img"));
        Assert.Contains(_auftraege, a => a.Bild == Bilder.WpLeistungTemperatur);

        Assert.Equal(new[] { "simerg-wp-produktion", "simerg-wp-streuwolke" },
                     seite.FindComponents<DiagrammSvg>()
                          .Select(b => b.Instance.Kennung).ToArray());
        Assert.Equal("kW", Bild(seite).Einheit);
    }

    /// <summary>
    /// Der Schalter „sortiert" stellt die Achsenart des Produktionsbildes auf den
    /// RANG um — dort zählt x nicht mehr die Zeit, sondern den Platz in der
    /// Rangfolge, und die Stundeneinheit fällt weg.
    /// </summary>
    [Fact]
    public void Der_Sortiertschalter_stellt_die_Achsenart_auf_Rang()
    {
        var seite = Zeichnen(Erg());

        Kasten(seite, SORTIERT, 0).Change(true);

        Assert.Contains(_auftraege, a => a.Bild == Bilder.WpProduktion && a.Sortiert);
    }

    /// <summary>
    /// Jedes der drei Blätter trägt seine EIGENE Kennung: Sie zeigen verschiedene
    /// Größen, und zwei Bilder dürfen nie dieselben <c>clipPath</c>-Kennungen
    /// bekommen.
    /// </summary>
    [Fact]
    public void Jedes_Unterblatt_traegt_seine_eigene_Kennung()
    {
        var seite = Zeichnen(Erg(), temperaturen: true);

        Assert.Equal("simerg-wp-produktion", Bild(seite).Kennung);

        seite.FindAll("button[role='tab']")[1].Click();
        Assert.Equal("simerg-wp-strom", Bild(seite).Kennung);

        seite.FindAll("button[role='tab']")[2].Click();
        Assert.Equal("simerg-wp-temperaturen", Bild(seite).Kennung);
        Assert.Equal("°C", Bild(seite).Einheit);
    }

    /// <summary>
    /// <b>Auch die Streuwolke trägt die Zoomleiste</b> (Etappe DG-E3, Gruppe (b)).
    /// Bis dahin blieb sie ein Pixelbild und hatte allein „1:1" — den reinen
    /// Bildzoom ihres Rahmens. Jetzt trägt sie eine Zeichenfläche wie jedes andere
    /// Bild; der aufgezogene Bereich ist dort eine TEMPERATURspanne statt einer
    /// Stundenspanne, und genau das ist der Gewinn.
    /// </summary>
    [Fact]
    public void Auch_die_Streuwolke_traegt_die_Zoomleiste()
    {
        var seite = Zeichnen(Erg());

        Assert.All(seite.FindComponents<DiagrammSvg>(),
                   b => Assert.Equal(new[] { "Bereich", "1:1" },
                                     b.FindAll("button.epos-diagramm-knopf")
                                      .Select(k => k.TextContent.Trim()).ToArray()));
    }

    /// <summary>
    /// <b>Die Auslegung steht am Ende des Reiters</b> (Anwenderwunsch 05.10.2026):
    /// erst die Kennzahlen Wärme und Strom, dann Modul- und Speichertabelle, dann
    /// die Unterblätter mit der Jahresganglinie und zuletzt der Block „Auslegung“
    /// mit dem Bild „Leistung über Außentemperatur“.
    /// </summary>
    [Fact]
    public void Die_Auslegung_steht_nach_der_Ganglinie_am_Ende()
    {
        var seite = Zeichnen(Erg(), temperaturen: true);

        var folge = seite.FindAll(
                "h2.epos-gruppenkopf-titel, table.epos-raster, button[role='tab'], div.epos-simerg-schalter")
            .Select(e => e.TagName.ToLowerInvariant() switch
            {
                "h2" => e.TextContent.Trim(),
                "table" => "Tabelle",
                "button" => "Blatt",
                _ => "Schalter",
            })
            .ToArray();

        Assert.Equal(new[]
        {
            "Wärme", "Strom", "Schalter", "Tabelle", "Tabelle",
            "Blatt", "Blatt", "Blatt", "Schalter", "Schalter",
            "Auslegung", "Schalter",
        }, folge);

        // Die Streuwolke ist das letzte Bild, die Ganglinie steht davor.
        Assert.Equal("simerg-wp-streuwolke",
                     seite.FindComponents<DiagrammSvg>().Last().Instance.Kennung);
    }

    // =====================================================================
    //  DIE JAHRESARBEITSZAHL (Übergabe „Dialoge und Korrekturen" 05.10.2026,
    //  Anwenderentscheide 06.10.2026): gerechnet im Kern (Jahresarbeitszahl),
    //  hier nur angezeigt — zwei Nachkommastellen, ohne Strom ein Strich.
    // =====================================================================

    /// <summary>Die Werte des Blocks „Strom" ohne die Einheitenspalte, in ihrer Reihenfolge.</summary>
    private static string[] Stromwerte(IRenderedComponent<WaermepumpeReiter> seite)
        => seite.FindAll("dl.epos-simerg-werte")[1]
                .QuerySelectorAll("dd:not(.epos-simerg-einheit)")
                .Select(d => d.TextContent.Trim()).ToArray();

    /// <summary>Die Zelle der Spalte JAZ in der ersten Modulzeile.</summary>
    private static string ModulJaz(IRenderedComponent<WaermepumpeReiter> seite)
        => seite.FindAll("table.epos-raster")[0].QuerySelectorAll("tbody tr")[0]
                .QuerySelectorAll("td")[5].TextContent.Trim();

    /// <summary>Der Kopf der Spalte JAZ.</summary>
    private static AngleSharp.Dom.IElement JazKopf(IRenderedComponent<WaermepumpeReiter> seite)
        => seite.FindAll("table.epos-raster")[0].QuerySelectorAll("thead th")[5];

    /// <summary>
    /// Der Block „Strom" zeigt nach den zwei Stromverbräuchen die JAZ der Wärmepumpe
    /// (300 ÷ 75 = 4,00) und die des Systems mit Heizstab ((300 + 2,5) ÷ (75 + 2,5) = 3,90),
    /// beide mit der Formel als Kurztext an Beschriftung und Wert.
    /// </summary>
    [Fact]
    public void Der_Block_Strom_zeigt_beide_Jahresarbeitszahlen()
    {
        var seite = Zeichnen(Erg());

        Assert.Equal(new[] { "75,00", "2,50", "4,00", "3,90" }, Stromwerte(seite));

        var zeilen = seite.FindAll("dl.epos-simerg-werte")[1].QuerySelectorAll("dt");
        Assert.Null(zeilen[0].GetAttribute("title"));
        Assert.Contains("÷", zeilen[2].GetAttribute("title"));
        Assert.Contains("VDI 4650", zeilen[3].GetAttribute("title"));
    }

    /// <summary>
    /// Ohne Strom keine Jahresarbeitszahl: ein Strich in beiden Zeilen und in der Spalte
    /// je Modul — nie eine Division durch null. Mit Heizstab, aber ohne Strom der
    /// Wärmepumpe gibt es nur die JAZ des Systems.
    /// </summary>
    [Fact]
    public void Ohne_Strom_steht_bei_der_Jahresarbeitszahl_ein_Strich()
    {
        var ohne = Erg();
        ohne.StromverbrauchMwh = 0;
        ohne.HeizstabStromverbrauchMwh = 0;
        ohne.Module.Clear();
        ohne.Module.Add(new SimulationErgebnisCtrl.WpModulZeile("WP 1", 30.0, 0.0, 0.0, 0.0, 0.0));

        var seite = Zeichnen(ohne);
        Assert.Equal(new[] { "0,00", "0,00", "—", "—" }, Stromwerte(seite));
        Assert.Equal("—", ModulJaz(seite));
        Kasten(seite, JAZ, 0).Change(true);
        Assert.Equal("—", ModulJaz(seite));

        var nurStab = Erg();
        nurStab.StromverbrauchMwh = 0;
        nurStab.WaermeproduktionMwh = 0;
        Assert.Equal(new[] { "0,00", "2,50", "—", "1,00" }, Stromwerte(Zeichnen(nurStab)));
    }

    /// <summary>
    /// Die Spalte JAZ rechnet ohne Heizstab (Vorgabe); der Schalter über der Modultabelle
    /// nimmt ihn dazu. Kopf und Kurztext nennen die Bilanzgrenze, die gerade gilt, und
    /// der Rückweg stellt die Vorgabe wieder her.
    /// </summary>
    [Fact]
    public void Die_Spalte_JAZ_folgt_dem_Heizstabschalter()
    {
        var seite = Zeichnen(Erg());

        Assert.Equal(new[] { "Heizstab in die JAZ einrechnen" }, Zeile(seite, JAZ));
        Assert.False(Kasten(seite, JAZ, 0).HasAttribute("checked"));
        Assert.Equal("JAZ", JazKopf(seite).TextContent.Trim());
        Assert.Contains("ohne Heizstab", JazKopf(seite).GetAttribute("title"));
        Assert.Equal("4,00", ModulJaz(seite));

        Kasten(seite, JAZ, 0).Change(true);

        Assert.Equal("JAZ mit Heizstab", JazKopf(seite).TextContent.Trim());
        Assert.Contains("mit seinem Heizstab", JazKopf(seite).GetAttribute("title"));
        Assert.Equal("3,90", ModulJaz(seite));
        // Der Block „Strom" bleibt, wie er ist: Der Schalter gilt nur der Spalte.
        Assert.Equal(new[] { "75,00", "2,50", "4,00", "3,90" }, Stromwerte(seite));

        Kasten(seite, JAZ, 0).Change(false);

        Assert.Equal("JAZ", JazKopf(seite).TextContent.Trim());
        Assert.Equal("4,00", ModulJaz(seite));
    }

    /// <summary>
    /// Der Hilfe-Assistent kennt den Schalter der Spalte JAZ als Anzeigeschalter und setzt
    /// ihn über denselben Weg wie der Anwender — nur, wo die Modultabelle steht.
    /// </summary>
    [Fact]
    public void Der_Heizstabschalter_steht_bei_den_Anzeigeschaltern()
    {
        const string NAME = "Heizstab in die JAZ einrechnen";

        var anzeige = new Ergebnisanzeige();
        var seite = Render<WaermepumpeReiter>(p => p
            .AddCascadingValue(anzeige)
            .Add(x => x.Daten, Erg())
            .Add(x => x.Modell, Modell));

        Anzeigeschalter schalter = Assert.Single(anzeige.Schalter, s => s.Name == NAME);
        Assert.False(schalter.An);
        schalter.An = true;
        seite.WaitForAssertion(() => Assert.Equal("3,90", ModulJaz(seite)));

        var ohneModule = Erg();
        ohneModule.Module.Clear();
        var leer = new Ergebnisanzeige();
        var ohne = Render<WaermepumpeReiter>(p => p
            .AddCascadingValue(leer)
            .Add(x => x.Daten, ohneModule)
            .Add(x => x.Modell, Modell));

        Assert.DoesNotContain(leer.Schalter, s => s.Name == NAME);
        Assert.DoesNotContain(NAME, ohne.Markup);
    }

    /// <summary>
    /// CSV am Diagramm: Das Produktionsbild führt den benannten Export der Wärmepumpe in seiner
    /// Zoomleiste (Vorrang vor der Naht); die Streuwolke (x = Temperatur) bekommt keinen Knopf, und
    /// am Seitenende steht keiner mehr.
    /// </summary>
    [Fact]
    public void Der_Waermepumpenexport_steht_am_Produktionsbild()
    {
        int gerufen = 0, naht = 0;
        var seite = Render<WaermepumpeReiter>(p => p
            .Add(x => x.Daten, Erg())
            .Add(x => x.Modell, Modell)
            .Add(x => x.Csv, EventCallback.Factory.Create(this, () => gerufen++))
            .AddCascadingValue(new Ganglinienexport((m, t) => { naht++; return Task.CompletedTask; })));

        var knoepfe = seite.FindAll("button.epos-diagramm-csv");
        Assert.Single(knoepfe);
        knoepfe[0].Click();

        Assert.Equal(1, gerufen);
        Assert.Equal(0, naht);
        Assert.Empty(seite.FindAll("button.epos-simerg-knopf[title]"));
    }
}
