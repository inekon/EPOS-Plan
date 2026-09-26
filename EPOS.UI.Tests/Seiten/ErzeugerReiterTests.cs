using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die vier ERZEUGER-Reiter der Ergebnisseite — Heizkessel (R6, iU9-W11b.5),
/// Solarthermie (R7, W11b.6), BHKW (R8, W11b.7) und Photovoltaik (R9, W11b.8).
///
/// <para>Sie teilen denselben Aufbau — Feldblock, Modultabelle, Diagramm — und
/// liegen deshalb in EINER Probendatei; jede Ausprägung hat ihre eigenen
/// Fälle. Geprüft werden die Felder aus dem DTO, die Präsenzregeln, die
/// Brennstoffblöcke und die Bildaufträge.</para>
/// <para>Der Selektor nennt seit der Windows-Abnahme 05.09.2026 die Klasse
/// <c>epos-simerg-knopf</c>: Jedes Diagramm steht seither im Baustein
/// <c>Diagramm</c> und bringt seine eigenen Knöpfe („1:1“, „Bereich“) mit.
/// <c>FindAll("button")</c> zählte die mit und prüfte damit nicht mehr, was
/// der Fall behauptet — nämlich die Knöpfe DIESES Reiters.</para>
/// <para>Seit den Anwenderwünschen 09.09.2026 dazu die WAHL DER REIHEN
/// (<b>W11b‑B‑19</b>: je Bild eine Schalterzeile, die Wahl im
/// <c>Bildauftrag.Reihen</c>) und die GLIEDERUNG der zwei Kennzahlenlisten
/// (<b>W11b‑B‑20</b>: Unterabschnitte, betonte Restzeile, Beschriftungen ohne
/// doppelte Einheit).</para>
/// <para>Mit <b>W11b‑B‑21/22</b> trägt auch das Kesselbild seine Reihenwahl und
/// der Kesselreiter VIER Hauptgruppen mit dunklem Balken in ZWEI Rasterzeilen;
/// <b>W11b‑B‑23</b> zieht dieselbe Bauform durch alle vier Reiter — Balken statt
/// <c>h3</c> für jede Hauptgruppe, ein Leerhinweis, der die fehlende Komponente
/// nennt, und die Reihenwahl auch am BHKW-Bild.</para>
/// <para>Mit der Welle <b>GM‑1</b> tragen auch Solarthermie und Photovoltaik den
/// Schalter „sortiert": Die Schalterleiste aller vier Reiter hat damit dieselben
/// zwei Zeilen — oben die Darstellungsart, darunter die Reihen.</para>
/// <para>Seit der Etappe DG-E3, Gruppe (a), steht jedes der vier Bilder im
/// Baustein <c>DiagrammSvg</c>: Der Zeitausschnitt ist die viewBox seiner
/// Zeichenfläche, „Bereich" und „1:1" bedienen sie, und kein Zoom kostet mehr
/// einen Rundlauf in den Kern (DG-E3-9).</para>
/// </summary>
public class ErzeugerReiterTests : EposBunitContext
{
    private readonly List<Bildauftrag> _auftraege = new();

    public ErzeugerReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Zeichenmodelle, nach Bild und Schalterstellung getrennt und je EINMAL
    /// gebaut. Der Baustein <c>DiagrammSvg</c> vergleicht die Modellreferenz; ein
    /// Delegat, der bei jedem Aufruf ein neues Modell bauen würde, ließe ihn seinen
    /// Baum je Zeichenlauf neu setzen und nähme ihm Zoom und abgewählte Reihe.
    /// </summary>
    private readonly Dictionary<string, Zeichenmodell> _modelle = new();

    private Zeichenmodell? Modell(Bildauftrag a)
    {
        _auftraege.Add(a);

        string schluessel = a.Bild + (a.Sortiert ? "-s" : "");
        if (_modelle.TryGetValue(schluessel, out Zeichenmodell? vorhanden)) return vorhanden;

        Zeichenmodell neu = Erzeugerbild(a.Bild, a.Sortiert);
        _modelle[schluessel] = neu;
        return neu;
    }

    /// <summary>
    /// Ein echtes Erzeugerbild aus einer KURZEN Reihe (eine Woche) — die Fälle prüfen
    /// die Bedienung, nicht die Rechenzeit eines Jahres.
    /// </summary>
    private static Zeichenmodell Erzeugerbild(string name, bool sortiert)
    {
        var werte = new double[168];
        for (int i = 0; i < werte.Length; i++)
            werte[i] = 100.0 + 60.0 * Math.Sin(2 * Math.PI * i / 24.0);
        if (sortiert) Array.Sort(werte, (x, y) => y.CompareTo(x));

        return ChartRenderer.ErzeugerStapelModell(
            name,
            new[] { new ChartRenderer.Reihe("Produktion", werte, ChartRenderer.C_KESSEL) },
            Array.Empty<ChartRenderer.Reihe>(), null,
            "kW", ChartRenderer.Achse.Jahresstunden, sortiert);
    }

    /// <summary>Die Beschriftungen der Schalterzeile Nr. <paramref name="nr"/>.</summary>
    private static string[] Schalterzeile<T>(IRenderedComponent<T> seite, int nr = 0)
        where T : IComponent
        => seite.FindAll("div.epos-simerg-schalter")[nr]
                .QuerySelectorAll("label.epos-schalter")
                .Select(l => l.TextContent.Trim()).ToArray();

    /// <summary>Das Kästchen Nr. <paramref name="k"/> der Schalterzeile Nr. <paramref name="nr"/>.</summary>
    private static AngleSharp.Dom.IElement Kasten<T>(IRenderedComponent<T> seite, int k, int nr = 0)
        where T : IComponent
        => seite.FindAll("div.epos-simerg-schalter")[nr]
                .QuerySelectorAll("input[type=checkbox]")[k];

    /// <summary>Die Beschriftungen einer Kennzahlenliste, in ihrer Reihenfolge.</summary>
    private static string[] Zeilen<T>(IRenderedComponent<T> seite, int nr)
        where T : IComponent
        => seite.FindAll("dl.epos-simerg-werte")[nr]
                .QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray();

    // =====================================================================
    // R6 — Heizkessel
    // =====================================================================

    private static SimulationErgebnisCtrl.HeizkesselErgebnis Kessel()
    {
        var e = new SimulationErgebnisCtrl.HeizkesselErgebnis
        {
            DeckungProzent = 36.25,
            StufeneingangMwh = 180.0,
            RestwaermeMwh = 6.04,
            WaermeproduktionMwh = 174.21,
            StrombedarfMwh = 1.25,
            ReststrombedarfMwh = 0.5,
            GasMwh = 190.0,
            MaxKesselleistungKw = 250.0,
            GasspitzeKw = 210.5,
            QuellwaermeMwh = 0.0
        };
        e.Module.Add(new SimulationErgebnisCtrl.KesselModulZeile("Kessel 1", 190.0, 0.0, 91.7));
        return e;
    }

    private static Brennstoffzeile[] Kesselbrennstoffe() =>
    [
        new Brennstoffzeile("Gasverbrauch (Hu):", 190.0, true),
        new Brennstoffzeile("Ölverbrauch:", 0.0, true),      // Kessel führt Öl, Wert 0
        new Brennstoffzeile("Koks:", 0.0, false),
        new Brennstoffzeile("Pellets:", 0.0, false)
    ];

    /// <summary>
    /// <b>Der ZEUGE der Anzeigeschalter</b> (Welle #458, Stufe 2): Das Blatt meldet seine
    /// Schalter beim Register der Seite an — in der Reihenfolge der Schalterzeilen —,
    /// ein Setzen geht denselben Weg wie der Schalter, und mit dem Blatt fällt die
    /// Anmeldung.
    /// </summary>
    [Fact]
    public void Das_Blatt_meldet_seine_Anzeigeschalter_beim_Register_der_Seite_an()
    {
        var anzeige = new Ergebnisanzeige();
        var seite = Render<HeizkesselReiter>(p => p
            .AddCascadingValue(anzeige)
            .Add(x => x.Daten, Kessel())
            .Add(x => x.Brennstoffe, Kesselbrennstoffe())
            .Add(x => x.BedarfVorhanden, true)
            .Add(x => x.Modell, Modell));

        Assert.Equal(new[]
                     {
                         Resource.SIM_CHK_SORTIERT, Resource.CHART_LEGENDE_WAERMEPRODUKTION_HEIZKESSEL,
                         Resource.CHART_SEGMENT_RESTWAERME, Resource.CHART_LEGENDE_WAERMEBEDARF_GESAMT
                     },
                     anzeige.Schalter.Select(s => s.Name).ToArray());

        anzeige.Schalter[0].An = true;
        seite.WaitForAssertion(() => Assert.True(seite.Instance.Sortiert));

        seite.Instance.Dispose();
        Assert.Empty(anzeige.Schalter);
    }

    /// <summary>Ohne Ergebnis zeichnet das Blatt keine Schalter — und meldet auch keine an.</summary>
    [Fact]
    public void Ohne_Ergebnis_meldet_das_Blatt_keine_Anzeigeschalter_an()
    {
        var anzeige = new Ergebnisanzeige();
        Render<HeizkesselReiter>(p => p
            .AddCascadingValue(anzeige)
            .Add(x => x.Daten, (SimulationErgebnisCtrl.HeizkesselErgebnis?)null)
            .Add(x => x.Modell, Modell));

        Assert.Empty(anzeige.Schalter);
    }

    private IRenderedComponent<HeizkesselReiter> KesselZeichnen(
        SimulationErgebnisCtrl.HeizkesselErgebnis? erg, bool bedarf = true, Action? csv = null,
        IReadOnlyList<Brennstoffzeile>? brennstoffe = null,
        bool brennstoffDefiniert = false)
        => Render<HeizkesselReiter>(p =>
        {
            p.Add(x => x.Daten, erg);
            p.Add(x => x.Brennstoffe, brennstoffe ?? Kesselbrennstoffe());
            p.Add(x => x.BrennstoffDefiniert, brennstoffDefiniert);
            p.Add(x => x.BedarfVorhanden, bedarf);
            p.Add(x => x.Modell, Modell);
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
        });

    /// <summary>
    /// Auftrag BH-1, Heizkesselseite: derselbe Unterschied wie beim BHKW. Bleibt
    /// keine Brennstoffzeile uebrig, obwohl ein Kessel des Projekts einen
    /// Brennstoff fuehrt, dann ist der Block die Folge eines Laufs ohne
    /// Kesselbetrieb — nicht eines Pflegefehlers.
    /// </summary>
    [Fact]
    public void Kessel_ohne_Verbrauch_mit_gepflegtem_Brennstoff_meldet_nicht_gelaufen()
    {
        var seite = KesselZeichnen(Kessel(), brennstoffe: Array.Empty<Brennstoffzeile>(),
                                   brennstoffDefiniert: true);

        Assert.Contains("nicht gelaufen", seite.Markup);
        Assert.DoesNotContain("Kein Brennstoff für diese Heizkessel definiert", seite.Markup);
    }

    [Fact]
    public void Kessel_ohne_gepflegten_Brennstoff_behaelt_den_Bestandstext()
    {
        var seite = KesselZeichnen(Kessel(), brennstoffe: Array.Empty<Brennstoffzeile>(),
                                   brennstoffDefiniert: false);

        Assert.Contains("Kein Brennstoff für diese Heizkessel definiert", seite.Markup);
        Assert.DoesNotContain("nicht gelaufen", seite.Markup);
    }

    [Fact]
    public void Kessel_zeigt_die_Felder_und_die_Quellwaermezeile()
    {
        var seite = KesselZeichnen(Kessel());
        string text = seite.Markup;

        Assert.Contains("36,25", text);
        Assert.Contains("174,21", text);
        Assert.Contains("210,50", text);
        Assert.Contains("Quellwärme aus Kaskade", text);   // Etappe D4, auch bei 0,00
    }

    /// <summary>
    /// Die Praesenzregel des Brennstoffblocks: sichtbar bei Jahreswert &gt; 0 ODER
    /// wenn ein Kessel des Projekts den Brennstoff fuehrt. Der vorhandene
    /// Oelkessel mit 0-Ergebnis bleibt damit sichtbar.
    /// </summary>
    [Fact]
    public void Kessel_zeigt_nur_die_praesenten_Brennstoffzeilen()
    {
        var seite = KesselZeichnen(Kessel());

        Assert.Contains("Gasverbrauch", seite.Markup);
        Assert.Contains("Ölverbrauch", seite.Markup);
        Assert.DoesNotContain("Koks", seite.Markup);
        Assert.DoesNotContain("Pellets", seite.Markup);
    }

    /// <summary>
    /// <b>Anwenderwunsch 09.09.2026 (W11b‑B‑22).</b> „Wärme", „Strom" und
    /// „Auslegung" standen als h3-Unterabschnitte UNTEREINANDER in EINER Spalte,
    /// der Brennstoffblock als einzige Gruppe mit dunklem Balken daneben. Jetzt
    /// sind es VIER gleichrangige Hauptgruppen mit Balken in ZWEI Rasterzeilen:
    /// oben Wärme | Strom (wie im Bedarfs- und im Übersichtsreiter), darunter
    /// Auslegung | Brennstoffverbrauch. Kein <c>h3</c> bleibt übrig; die
    /// Zeilenfolge JEDER Gruppe ist die von W11b‑B‑15.
    /// </summary>
    [Fact]
    public void Kessel_gliedert_seine_Felder_in_vier_Gruppen_mit_Balken()
    {
        var seite = KesselZeichnen(Kessel());

        // Der fünfte Balken steht über der Kesseltabelle - sein Titel trägt seit
        // W11b‑B‑23 keinen Doppelpunkt mehr (SIMERG_GRP_MODULE_SPK).
        Assert.Equal(new[] { "Wärme", "Strom", "Auslegung", "Brennstoffverbrauch der Spitzenkessel",
                             "Wärmeproduktion der einzelnen Spitzenkessel" },
                     seite.FindAll("h2.epos-gruppenkopf-titel").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Empty(seite.FindAll("h3.epos-untergruppe"));

        // Zwei Rasterzeilen mit je zwei Listen - nicht vier Gruppen in einer.
        Assert.Equal(2, seite.FindAll("div.epos-simerg-spalten")
                             .Count(z => z.QuerySelectorAll("dl.epos-simerg-werte").Length == 2));

        var listen = seite.FindAll("dl.epos-simerg-werte");
        Assert.Equal(4, listen.Count);          // drei Gruppen + der Brennstoffblock

        Assert.Equal(
            new[] { "Wärmebedarfsdeckung:", "Wärmebedarf:", "Wärmeproduktion der Spitzenkessel:",
                    "Quellwärme aus Kaskade:", "Restwärmebedarf:" },
            listen[0].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Strombedarf:", "Reststrombedarf:" },
            listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Gesamte Wärmeleistung der Heizkessel:", "Maximaler Gasbezug:" },
            listen[2].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());

        Assert.Equal(new[] { "Restwärmebedarf:", "Reststrombedarf:" },
                     seite.FindAll("dt.epos-simerg-abschluss").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// <b>W11b‑B‑23.</b> Die Quellwärme der Kaskade stand als EINZIGE Wärmezeile
    /// der ganzen Ergebnisseite in „MWh" statt „MWh/a"
    /// (<c>SIM_KESSEL_QUELLWAERME_EINHEIT</c>, die Einheit des WinForms-Feldes) —
    /// dieselbe Größenart wie die Zeilen darüber, nur ohne Zeitbezug.
    /// </summary>
    [Fact]
    public void Kessel_nennt_die_Quellwaerme_in_MWh_je_Jahr()
    {
        var seite = KesselZeichnen(Kessel());

        Assert.DoesNotContain("<dd class=\"epos-simerg-einheit\">MWh</dd>", seite.Markup);
        Assert.Contains("<dd class=\"epos-simerg-einheit\">MWh/a</dd>", seite.Markup);
    }

    /// <summary>
    /// <b>W11b‑B‑23.</b> Bleibt nach der Präsenzregel KEINE Brennstoffzeile
    /// übrig, stand hier ein dunkler Balken ohne Inhalt. Jetzt sagt derselbe
    /// Leerhinweis wie im BHKW-Reiter, was fehlt.
    /// </summary>
    [Fact]
    public void Kessel_meldet_einen_leeren_Brennstoffblock()
    {
        var seite = KesselZeichnen(Kessel(), brennstoffe: Array.Empty<Brennstoffzeile>());

        Assert.Single(seite.FindAll("[role='alert']"));
        Assert.Equal(3, seite.FindAll("dl.epos-simerg-werte").Count);   // ohne Brennstoffliste
    }

    // ---- W11b‑B‑21: die drei Reihen des Kesselbildes sind wählbar ----------

    /// <summary>
    /// Zwei Schalterzeilen: OBEN „sortiert" (die Darstellungsart), DARUNTER je
    /// Reihe ein Schalter (die Reihen) — dieselbe Trennung wie im
    /// Wärmepumpenreiter (W11b‑B‑17). Alle drei Reihen stehen beim Aufbau AN,
    /// beschriftet mit der Ressource ihrer Legende, in der Reihenfolge des Bildes.
    /// </summary>
    [Fact]
    public void Kessel_traegt_je_Reihe_einen_Schalter()
    {
        var seite = KesselZeichnen(Kessel());

        Assert.Equal(new[] { "sortiert" }, Schalterzeile(seite, 0));
        Assert.Equal(new[] { "Wärmeproduktion Heizkessel", "Restwärme", "Wärmebedarf gesamt" },
                     Schalterzeile(seite, 1));
        Assert.All(seite.FindAll("div.epos-simerg-schalter")[1]
                        .QuerySelectorAll("input[type=checkbox]"),
                   k => Assert.True(k.HasAttribute("checked")));

        Assert.Equal(new[] { "WAERMEPRODUKTION", "RESTWAERME", "WAERMEBEDARF" },
                     seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "WAERMEPRODUKTION", "RESTWAERME", "WAERMEBEDARF" },
                     _auftraege.Last(a => a.Bild == Bilder.Heizkessel).Reihen!.ToArray());
    }

    /// <summary>Die Abwahl gibt den Bildauftrag OHNE diesen Schlüssel weiter.</summary>
    [Fact]
    public void Kessel_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag()
    {
        var seite = KesselZeichnen(Kessel());
        _auftraege.Clear();

        Kasten(seite, 1, 1).Change(false);              // Restwärme

        Assert.Equal(new[] { "WAERMEPRODUKTION", "WAERMEBEDARF" },
                     seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "WAERMEPRODUKTION", "WAERMEBEDARF" },
                     _auftraege.Last(a => a.Bild == Bilder.Heizkessel).Reihen!.ToArray());
    }

    /// <summary>
    /// ALLE abgewählt heisst KEINE Reihe und nicht „alle": Der Auftrag trägt eine
    /// LEERE Liste, aus der die Hülle den Leerhinweis des Renderers zeichnet
    /// (dieselbe Regel wie im Wärmepumpenreiter, W11b‑B‑17).
    /// </summary>
    [Fact]
    public void Kessel_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste()
    {
        var seite = KesselZeichnen(Kessel());
        _auftraege.Clear();

        Kasten(seite, 0, 1).Change(false);
        Kasten(seite, 1, 1).Change(false);
        Kasten(seite, 2, 1).Change(false);

        Assert.Empty(seite.Instance.GewaehlteReihen);
        Assert.Empty(_auftraege.Last(a => a.Bild == Bilder.Heizkessel).Reihen!);
    }

    /// <summary>Ohne Projektbedarf steht „0" und nicht „NaN" (woertlich :4530).</summary>
    [Fact]
    public void Kessel_zeigt_ohne_Bedarf_eine_Null()
    {
        var seite = KesselZeichnen(Kessel(), bedarf: false);
        Assert.DoesNotContain("36,25", seite.Markup);
    }

    /// <summary>
    /// „sortiert" steht seit W11b‑B‑21 in seiner EIGENEN Zeile über den Reihen
    /// und wechselt nur die Darstellungsart: Die drei Reihen bleiben dieselben.
    /// </summary>
    [Fact]
    public void Kessel_wechselt_den_Bildauftrag_mit_dem_Sortiertschalter()
    {
        var seite = KesselZeichnen(Kessel());
        _auftraege.Clear();

        Kasten(seite, 0, 0).Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Heizkessel && a.Sortiert);
        Assert.Equal(new[] { "WAERMEPRODUKTION", "RESTWAERME", "WAERMEBEDARF" },
                     _auftraege.Last(a => a.Bild == Bilder.Heizkessel).Reihen!.ToArray());
    }

    [Fact]
    public void Kessel_ohne_Lauf_bleibt_leer()
    {
        var seite = KesselZeichnen(null);

        Assert.Empty(seite.FindAll("table"));
        Assert.Empty(seite.FindAll("img"));
    }

    [Fact]
    public void Kessel_meldet_seinen_CSV_Klick()
    {
        int gerufen = 0;
        var seite = KesselZeichnen(Kessel(), csv: () => gerufen++);

        seite.Find("button.epos-simerg-knopf").Click();
        Assert.Equal(1, gerufen);
    }

    // =====================================================================
    // R7 — Solarthermie
    // =====================================================================

    private static SimulationErgebnisCtrl.SolarthermieErgebnis Solar(bool deckung = true)
    {
        var e = new SimulationErgebnisCtrl.SolarthermieErgebnis
        {
            DeckungBekannt = deckung,
            DeckungProzent = 8.4,
            StufeneingangMwh = 60.0,
            RestwaermeMwh = 55.0,
            WaermeproduktionMwh = 40.5,
            UeberschussMwh = 1.25
        };
        e.Module.Add(new SimulationErgebnisCtrl.SolarModulZeile("Kollektor A", 2.4, 20, 40.5, 1.25));
        return e;
    }

    private IRenderedComponent<SolarthermieReiter> SolarZeichnen(bool deckung = true)
        => Render<SolarthermieReiter>(p => p
            .Add(x => x.Daten, Solar(deckung))
            .Add(x => x.Modell, Modell));

    [Fact]
    public void Solarthermie_zeigt_fuenf_Felder_und_die_Kollektortabelle()
    {
        var seite = SolarZeichnen();

        Assert.Contains("8,40", seite.Markup);
        Assert.Contains("40,50", seite.Markup);
        Assert.Equal(7, seite.FindAll("table.epos-raster thead th").Count);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Solarthermie);
    }

    /// <summary>Ohne bekannten Bezug bleibt das Deckungsfeld LEER (woertlich :4603).</summary>
    [Fact]
    public void Solarthermie_laesst_die_Deckung_ohne_Bezug_leer()
    {
        var seite = SolarZeichnen(deckung: false);

        Assert.DoesNotContain("8,40", seite.Markup);
    }

    /// <summary>
    /// ERTRAG OHNE ABNEHMER: Liefert der Kern den Satz, steht er als HINWEISZEILE
    /// (Stufe Hinweis, kein Fehler) unter den Kennzahlen — ohne ihn steht dort nichts.
    /// Der Satz selbst entsteht im Kern (<c>SimulationErgebnisCtrl.SolarHinweisOhneAbnehmer</c>);
    /// der Reiter zeigt ihn nur.
    /// </summary>
    [Fact]
    public void Solarthermie_zeigt_den_Ertrag_ohne_Abnehmer_als_Hinweiszeile()
    {
        var ohne = SolarZeichnen();
        Assert.Empty(ohne.FindAll(".epos-warnbanner"));

        var e = Solar(true);
        e.HinweisOhneAbnehmer = "Ertrag 28,9 MWh/a ohne Abnehmer: Die Senke „Heizkreis (Heizung + " +
                                "Warmwasser)\" bedient nicht den Kanal Prozesswärme, in dem der Bedarf liegt.";
        var mit = Render<SolarthermieReiter>(p => p.Add(x => x.Daten, e).Add(x => x.Modell, Modell));

        var banner = mit.Find(".epos-simerg-kennzahlenzeile .epos-warnbanner");
        Assert.Contains("epos-warnbanner--hinweis", banner.ClassName);
        Assert.Contains("ohne Abnehmer", banner.TextContent);
        Assert.Contains("Prozesswärme", banner.TextContent);
    }

    /// <summary>
    /// Die Erzeugung steht als KOLLEKTORERTRAG BRUTTO mit seinen zwei Teilen „davon
    /// genutzt" und „Überschuss" — die mehrdeutige Zeile „Wärmeproduktion der Module"
    /// (sie zeigte nur den genutzten Teil) ist fort, ebenso die Leistungsbeschriftung.
    /// </summary>
    [Fact]
    public void Solarthermie_nennt_Bruttoertrag_Nutzung_und_Ueberschuss()
    {
        var seite = SolarZeichnen();

        var zeilen = Zeilen(seite, 0);
        Assert.Contains("Kollektorertrag brutto:", zeilen);
        Assert.Contains("davon genutzt:", zeilen);
        Assert.Contains("Überschuss:", zeilen);
        Assert.DoesNotContain("Wärmeproduktion der Module:", seite.Markup);
        Assert.DoesNotContain("Gesamte Wärmeleistung der Module:", seite.Markup);
    }

    /// <summary>
    /// BRUTTO = GENUTZT + ÜBERSCHUSS geht auf dem Blatt auf: Der Bruttowert ist die
    /// Summe der zwei ANGEZEIGTEN Teile. Fall der Anwendermeldung (5,43 genutzt,
    /// 52,33 Überschuss → 57,76) und ein Rundungsfall, in dem die ungerundete Summe
    /// eine andere zweite Nachkommastelle zeigte (0,004 + 0,004 → 0,00 statt 0,01; 0,006 + 0,006 → 0,02 statt 0,01).
    /// </summary>
    [Theory]
    [InlineData(5.43, 52.33, "5,43", "52,33", "57,76")]
    [InlineData(0.004, 0.004, "0,00", "0,00", "0,00")]
    [InlineData(0.006, 0.006, "0,01", "0,01", "0,02")]
    public void Solarthermie_Bruttoertrag_ist_die_Summe_der_angezeigten_Teile(
        double genutzt, double ueberschuss, string tGenutzt, string tUeber, string tBrutto)
    {
        var e = Solar();
        e.WaermeproduktionMwh = genutzt;
        e.UeberschussMwh = ueberschuss;
        var seite = Render<SolarthermieReiter>(p => p.Add(x => x.Daten, e).Add(x => x.Modell, Modell));

        var liste = seite.FindAll("dl.epos-simerg-werte")[0];
        string[] titel = liste.QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray();
        string[] werte = liste.QuerySelectorAll("dd:not(.epos-simerg-einheit)")
                              .Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(titel.Length, werte.Length);

        Assert.Equal(tBrutto, werte[Array.IndexOf(titel, "Kollektorertrag brutto:")]);
        Assert.Equal(tGenutzt, werte[Array.IndexOf(titel, "davon genutzt:")]);
        Assert.Equal(tUeber, werte[Array.IndexOf(titel, "Überschuss:")]);
    }

    /// <summary>
    /// Die KOLLEKTORTABELLE gliedert je Feld wie die Tafel: „brutto“ vor „genutzt“ und
    /// „Überschuss“, brutto als Summe der zwei angezeigten Teile; die mehrdeutige Spalte
    /// „Wärmeprod.“ (sie zeigte nur den genutzten Teil) steht dort nicht mehr.
    /// </summary>
    [Fact]
    public void Solarthermie_Kollektortabelle_zeigt_je_Feld_brutto_genutzt_und_Ueberschuss()
    {
        var e = Solar();
        e.Module.Clear();
        e.Module.Add(new SimulationErgebnisCtrl.SolarModulZeile("Kollektor A", 2.4, 20, 5.43, 52.33));
        e.Module.Add(new SimulationErgebnisCtrl.SolarModulZeile("Kollektor B", 2.0, 10, 0.006, 0.006));
        var seite = Render<SolarthermieReiter>(p => p.Add(x => x.Daten, e).Add(x => x.Modell, Modell));

        string[] kopf = seite.FindAll("table.epos-raster thead th")
                             .Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "brutto [MWh/a]", "genutzt [MWh/a]", "Überschuss [MWh/a]" }, kopf[4..]);
        Assert.DoesNotContain("Wärmeprod. [MWh/a]", kopf);

        var zeilen = seite.FindAll("table.epos-raster tbody tr");
        Assert.Equal(2, zeilen.Count);
        string[] a = zeilen[0].QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "57,76", "5,43", "52,33" }, a[4..]);
        string[] b = zeilen[1].QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "0,02", "0,01", "0,01" }, b[4..]);
    }

    // ---- W11b‑B‑19: die zwei Linien des Solarbildes sind wählbar ----------

    /// <summary>
    /// Je Reihe ein Schalter, die Beschriftung DIESELBE wie in der Legende des
    /// Bildes. Beide stehen beim Aufbau AN: Das Bild sieht aus wie bisher, bis
    /// der Anwender etwas abwählt.
    /// </summary>
    [Fact]
    public void Solarthermie_traegt_je_Reihe_einen_Schalter()
    {
        var seite = SolarZeichnen();

        Assert.Equal(new[] { "Wärmebedarf", "Wärmeproduktion" }, Schalterzeile(seite, 1));
        Assert.All(seite.FindAll("div.epos-simerg-schalter")[1]
                        .QuerySelectorAll("input[type=checkbox]"),
                   k => Assert.True(k.HasAttribute("checked")));

        Assert.Equal(new[] { "WAERMEBEDARF", "WAERMEPRODUKTION" },
                     seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "WAERMEBEDARF", "WAERMEPRODUKTION" },
                     _auftraege.Last(a => a.Bild == Bilder.Solarthermie).Reihen!.ToArray());
    }

    /// <summary>Die Abwahl gibt den Bildauftrag OHNE diesen Schlüssel weiter.</summary>
    [Fact]
    public void Solarthermie_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag()
    {
        var seite = SolarZeichnen();
        _auftraege.Clear();

        Kasten(seite, 0, 1).Change(false);              // Wärmebedarf

        Assert.Equal(new[] { "WAERMEPRODUKTION" }, seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "WAERMEPRODUKTION" },
                     _auftraege.Last(a => a.Bild == Bilder.Solarthermie).Reihen!.ToArray());
    }

    /// <summary>
    /// ALLE abgewählt heisst KEINE Reihe und nicht „alle“: Der Auftrag trägt eine
    /// LEERE Liste, aus der die Hülle den Leerhinweis des Renderers zeichnet
    /// (dieselbe Regel wie im Wärmepumpenreiter, W11b‑B‑17).
    /// </summary>
    [Fact]
    public void Solarthermie_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste()
    {
        var seite = SolarZeichnen();
        _auftraege.Clear();

        Kasten(seite, 0, 1).Change(false);
        Kasten(seite, 1, 1).Change(false);

        Assert.Empty(seite.Instance.GewaehlteReihen);
        Assert.Empty(_auftraege.Last(a => a.Bild == Bilder.Solarthermie).Reihen!);
    }

    /// <summary>
    /// <b>Der Schalter „sortiert"</b> (Welle GM‑1) steht in seiner EIGENEN Zeile
    /// über den Reihen — dieselbe Trennung wie im Kesselreiter — und wechselt nur
    /// die Darstellungsart: Die zwei Reihen bleiben dieselben.
    /// </summary>
    [Fact]
    public void Solarthermie_wechselt_den_Bildauftrag_mit_dem_Sortiertschalter()
    {
        var seite = SolarZeichnen();

        Assert.Equal(new[] { "sortiert" }, Schalterzeile(seite, 0));
        _auftraege.Clear();

        Kasten(seite, 0, 0).Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Solarthermie && a.Sortiert);
        Assert.Equal(new[] { "WAERMEBEDARF", "WAERMEPRODUKTION" },
                     _auftraege.Last(a => a.Bild == Bilder.Solarthermie).Reihen!.ToArray());
    }

    // ---- W11b‑B‑20: die Kennzahlenliste ------------------------------------

    /// <summary>
    /// <b>Anwenderwunsch 09.09.2026.</b> Die fünf Zeilen standen in der Reihenfolge
    /// der WinForms-Maske — Deckungsgrad zuerst, der Rest VOR der Produktion, aus
    /// der er sich ergibt. Jetzt trägt EIN Unterabschnitt „Wärme“ die fachliche
    /// Ordnung Bedarf → Erzeugung → Überschuss → Rest → Deckung, und der Rest ist
    /// betont. Keine Zahl fällt weg.
    /// </summary>
    [Fact]
    public void Solarthermie_gliedert_ihre_Felder_und_betont_den_Rest()
    {
        var seite = SolarZeichnen();

        // W11b‑B‑23: der Gruppentitel ist ein dunkler Balken, kein h3 - Wärme ist
        // eine HAUPTgruppe, hier wie in jedem anderen Reiter des Stapels.
        Assert.Equal(new[] { "Wärme" },
                     seite.FindAll("h2.epos-gruppenkopf-titel").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Empty(seite.FindAll("h3.epos-untergruppe"));
        Assert.Equal(
            new[] { "Wärmebedarf:", "Kollektorertrag brutto:", "davon genutzt:", "Überschuss:",
                    "Restwärmebedarf:", "Wärmebedarfsdeckung:" },
            Zeilen(seite, 0));
        Assert.Equal(new[] { "Restwärmebedarf:" },
                     seite.FindAll("dt.epos-simerg-abschluss").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Die Liste steht ALLEIN in ihrer Rasterzeile — daneben steht kein zweiter
    /// Block. Ohne <c>epos-simerg-kennzahlenzeile</c> sass sie in EINER Spalte des
    /// auto-fit-Rasters, und lange Beschriftungen liefen rechts heraus.
    /// </summary>
    [Fact]
    public void Solarthermie_gibt_der_Kennzahlenliste_die_ganze_Rasterzeile()
    {
        var seite = SolarZeichnen();
        Assert.Single(seite.FindAll("section.epos-simerg-kennzahlenzeile"));
    }

    /// <summary>
    /// Befund W11-B20: Ein Folgelauf ohne Solarthermie liess die Zahlen des
    /// Vorlaufs stehen. Das DTO ist dann null - die Rubrik ist leer.
    /// </summary>
    [Fact]
    public void Solarthermie_ohne_Lauf_bleibt_leer()
    {
        var seite = Render<SolarthermieReiter>(p => p.Add(x => x.Modell, Modell));

        Assert.Empty(seite.FindAll("table"));
        Assert.Empty(seite.FindAll("img"));
    }

    // =====================================================================
    // R8 — BHKW
    // =====================================================================

    private static SimulationErgebnisCtrl.BhkwErgebnis Bhkw(bool vbh = true)
    {
        var e = new SimulationErgebnisCtrl.BhkwErgebnis
        {
            BetriebsstundenThermisch = 1505,
            BetriebsstundenDurchschnitt = 1505,
            VbhElektrischBekannt = vbh,
            VbhElektrisch = 1420,
            StufeneingangMwh = 200.0,
            StrombedarfMwh = 120.5,
            WaermeproduktionMwh = 25.61,
            StromproduktionMwh = 18.0,
            RestwaermeMwh = 174.0,
            ReststrombedarfMwh = 102.5,
            WaermeueberschussMwh = 0.5,
            SpeicherladungMwh = 14.32,
            SpeicherdeckungMwh = 14.11,
            WaermedeckungProzent = 5.3,
            StromdeckungProzent = 14.9
        };
        e.Module.Add(new SimulationErgebnisCtrl.BhkwModulZeile("", 25.61, 18.0));
        return e;
    }

    private IRenderedComponent<BhkwReiter> BhkwZeichnen(
        SimulationErgebnisCtrl.BhkwErgebnis? erg, bool praesent = true,
        IReadOnlyList<Brennstoffzeile>? brennstoffe = null,
        bool brennstoffDefiniert = false)
        => Render<BhkwReiter>(p => p
            .Add(x => x.Daten, erg)
            .Add(x => x.Praesent, praesent)
            .Add(x => x.Brennstoffe, brennstoffe ?? new[]
            {
                new Brennstoffzeile("Gasverbrauch (Hu):", 62.0, true)
            })
            .Add(x => x.BrennstoffDefiniert, brennstoffDefiniert)
            .Add(x => x.Modell, Modell));

    /// <summary>
    /// <b>Auftrag BH-1 (Anwenderbefund 19.09.2026).</b> Ein BHKW, das hinter
    /// Waermepumpe und Heizkessel in der Kaskade steht, bekommt keinen
    /// Waermebedarf mehr ab und laeuft 0 h/a. Der Brennstoffblock fuehrt nur
    /// Zeilen mit Verbrauch &gt; 0 und blieb damit leer — gemeldet wurde
    /// „Kein Brennstoff für dieses BHKW definiert". Das ist die falsche
    /// Auskunft: Gepflegt ist der Brennstoff (<c>Tab_BHKW.Brennstoff</c>), nur
    /// verbraucht wurde keiner.
    /// </summary>
    [Fact]
    public void Bhkw_ohne_Verbrauch_mit_gepflegtem_Brennstoff_meldet_nicht_gelaufen()
    {
        var seite = BhkwZeichnen(Bhkw(), brennstoffe: Array.Empty<Brennstoffzeile>(),
                                 brennstoffDefiniert: true);

        Assert.Contains("nicht gelaufen", seite.Markup);
        Assert.DoesNotContain("Kein Brennstoff für dieses BHKW definiert", seite.Markup);
    }

    /// <summary>
    /// Die Gegenprobe: Ohne gepflegten Brennstoff bleibt der Bestandstext stehen —
    /// dann ist er wahr (Auftrag BH-1).
    /// </summary>
    [Fact]
    public void Bhkw_ohne_gepflegten_Brennstoff_behaelt_den_Bestandstext()
    {
        var seite = BhkwZeichnen(Bhkw(), brennstoffe: Array.Empty<Brennstoffzeile>(),
                                 brennstoffDefiniert: false);

        Assert.Contains("Kein Brennstoff für dieses BHKW definiert", seite.Markup);
        Assert.DoesNotContain("nicht gelaufen", seite.Markup);
    }

    /// <summary>
    /// Etappe E2: Die beiden Bestandszeilen heissen „Vbh thermisch, …" — sie
    /// fuehrten nie Betriebsstunden.
    /// </summary>
    [Fact]
    public void Bhkw_zeigt_die_beiden_thermischen_Vbh_Zeilen()
    {
        var seite = BhkwZeichnen(Bhkw());

        Assert.Contains("Vbh thermisch, Summe Module", seite.Markup);
        Assert.Contains("Vbh thermisch, Mittel Module", seite.Markup);
        Assert.Contains("1.505", seite.Markup);   // N0 (W11b-B-13)
    }

    /// <summary>
    /// <b>Anwenderrückmeldung 08.09.2026 (W11b‑B‑15).</b> Fünfzehn Zeilen in
    /// EINER Liste: drei Stundenzeilen, dann Wärme und Strom im Wechsel, die
    /// Restzahlen in der Mitte, die zwei Deckungsgrade ganz unten. Jetzt tragen
    /// drei Unterabschnitte die Ordnung, jede Gruppe schliesst mit ihrem Rest.
    /// </summary>
    [Fact]
    public void Bhkw_gliedert_seine_Felder_in_Waerme_Strom_und_Betrieb()
    {
        var seite = BhkwZeichnen(Bhkw());

        // W11b‑B‑23: vier Hauptgruppen mit Balken in ZWEI Rasterzeilen, kein h3.
        Assert.Equal(new[] { "Wärme", "Strom", "Betrieb", "Brennstoffverbrauch" },
                     seite.FindAll("h2.epos-gruppenkopf-titel").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Empty(seite.FindAll("h3.epos-untergruppe"));
        Assert.Equal(2, seite.FindAll("div.epos-simerg-spalten")
                             .Count(z => z.QuerySelectorAll("dl.epos-simerg-werte").Length == 2));

        var listen = seite.FindAll("dl.epos-simerg-werte");
        Assert.Equal(4, listen.Count);          // drei Gruppen + der Brennstoffblock

        Assert.Equal(
            new[] { "Wärmebedarf:", "Wärmeproduktion:", "Wärmeüberschuß (*):",
                    "davon in den Speicher:", "aus dem Speicher gedeckt:",
                    "Wärmebedarfsdeckung:", "Restwärmebedarf:" },
            listen[0].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Strombedarf:", "Stromproduktion:", "Stromeinspeisung:",   // E29 (#536)
                    "Strombedarfsdeckung:", "Reststrombedarf:" },
            listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        // W11b‑B‑23: die zwei Vbh-Zeilen tragen jetzt denselben Doppelpunkt wie
        // jede andere Beschriftung der Kennzahlenlisten.
        Assert.Equal(
            new[] { "Vbh thermisch, Summe Module:", "Vbh thermisch, Mittel Module:",
                    "Vollbenutzungsstunden elektrisch:" },
            listen[2].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());

        Assert.Equal(new[] { "Restwärmebedarf:", "Reststrombedarf:" },
                     seite.FindAll("dt.epos-simerg-abschluss").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>Ohne elektrische Nennleistung steht „—" und keine erfundene Zahl.</summary>
    [Fact]
    public void Bhkw_zeigt_ohne_Nennleistung_einen_Gedankenstrich()
    {
        var seite = BhkwZeichnen(Bhkw(vbh: false));
        Assert.Contains("<dd>—</dd>", seite.Markup);
    }

    /// <summary>
    /// Befund W11-B21: Die zwei Speicherzeilen tragen jetzt Katalogschluessel
    /// statt eines zweiten ResourceManagers auf die Form-.resx.
    /// </summary>
    [Fact]
    public void Bhkw_zeigt_die_zwei_Speicherzeilen_aus_dem_Katalog()
    {
        var seite = BhkwZeichnen(Bhkw());

        Assert.Contains("davon in den Speicher", seite.Markup);
        Assert.Contains("aus dem Speicher gedeckt", seite.Markup);
        Assert.Contains("14,32", seite.Markup);
    }

    /// <summary>
    /// E29 (#536, E27‑Q3 b, E29‑Q4 a): die BHKW-Einspeisung als eigene Zeile direkt nach
    /// der Stromproduktion, mit der Formel des KWK-Splits im Tooltip; die übrigen
    /// Beschriftungen tragen kein title-Attribut.
    /// </summary>
    [Fact]
    public void Bhkw_zeigt_die_Stromeinspeisung_mit_Formel_im_Tooltip()
    {
        var erg = Bhkw();
        erg.EinspeisungMwh = 27.4575;
        var seite = BhkwZeichnen(erg);

        var zeile = seite.FindAll("dt").Single(d => d.TextContent.Trim() == "Stromeinspeisung:");
        string? tip = zeile.GetAttribute("title");
        Assert.NotNull(tip);
        Assert.Contains("KWK-Einspeisung der Wirtschaftlichkeit", tip);
        Assert.Contains("PV-Eigenverbrauch", tip);
        Assert.Equal("27,46", zeile.NextElementSibling!.TextContent.Trim());

        Assert.Single(seite.FindAll("dt[title]"));
    }

    // ---- W11b‑B‑23: die vier Reihen des BHKW-Bildes sind wählbar ----------

    /// <summary>
    /// Das BHKW-Bild war das letzte, das IMMER alles zeichnete. Es trägt jetzt
    /// dieselben zwei Schalterzeilen wie Kessel und Wärmepumpe: oben „sortiert",
    /// darunter je Reihe ein Schalter, alle AN, beschriftet mit der Ressource
    /// ihrer Legende und in der Reihenfolge des Bildes.
    /// </summary>
    [Fact]
    public void Bhkw_traegt_je_Reihe_einen_Schalter()
    {
        var seite = BhkwZeichnen(Bhkw());

        Assert.Equal(new[] { "sortiert" }, Schalterzeile(seite, 0));
        Assert.Equal(new[] { "Wärmeproduktion", "Speicherladung", "Restwärme", "Wärmebedarf" },
                     Schalterzeile(seite, 1));

        Assert.Equal(new[] { "WAERMEPRODUKTION", "SPEICHERLADUNG", "RESTWAERME", "WAERMEBEDARF" },
                     seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "WAERMEPRODUKTION", "SPEICHERLADUNG", "RESTWAERME", "WAERMEBEDARF" },
                     _auftraege.Last(a => a.Bild == Bilder.Bhkw).Reihen!.ToArray());
    }

    /// <summary>Die Abwahl gibt den Bildauftrag OHNE diesen Schlüssel weiter.</summary>
    [Fact]
    public void Bhkw_nimmt_die_abgewaehlte_Reihe_aus_dem_Bildauftrag()
    {
        var seite = BhkwZeichnen(Bhkw());
        _auftraege.Clear();

        Kasten(seite, 1, 1).Change(false);              // Speicherladung

        Assert.Equal(new[] { "WAERMEPRODUKTION", "RESTWAERME", "WAERMEBEDARF" },
                     seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "WAERMEPRODUKTION", "RESTWAERME", "WAERMEBEDARF" },
                     _auftraege.Last(a => a.Bild == Bilder.Bhkw).Reihen!.ToArray());
    }

    /// <summary>Ohne Praesenz: kein Diagramm, kein Umschalter, keine Speicherzeilen.</summary>
    [Fact]
    public void Bhkw_ohne_Praesenz_zeigt_kein_Diagramm()
    {
        var seite = BhkwZeichnen(Bhkw(), praesent: false);

        Assert.Empty(seite.FindAll("img"));
        Assert.Empty(seite.FindAll("input[type='checkbox']"));
        Assert.DoesNotContain("davon in den Speicher", seite.Markup);
    }

    /// <summary>„Falls GAR kein Brennstoff aktiv war" — woertlich :7614.</summary>
    [Fact]
    public void Bhkw_meldet_einen_leeren_Brennstoffblock()
    {
        var seite = BhkwZeichnen(Bhkw(), brennstoffe: Array.Empty<Brennstoffzeile>());
        Assert.Single(seite.FindAll("[role='alert']"));
    }

    /// <summary>Ein Modul ohne Namen bekommt den Ersatztext der Oberflaeche.</summary>
    [Fact]
    public void Bhkw_setzt_den_Ersatznamen_eines_namenlosen_Moduls()
    {
        var seite = BhkwZeichnen(Bhkw());
        Assert.Contains(Resource.SIM_BHKW_MODUL_STANDARD, seite.Markup);
    }

    // =====================================================================
    // R9 — Photovoltaik
    // =====================================================================

    private static SimulationErgebnisCtrl.PhotovoltaikErgebnis Pv()
    {
        var e = new SimulationErgebnisCtrl.PhotovoltaikErgebnis
        {
            StromproduktionMwh = 42.5,
            GenutztMwh = 30.0,
            UeberschussMwh = 12.25,
            DeckungProzent = 0.0,
            StrombedarfMwh = 120.5,
            ReststrombedarfMwh = 90.0,
            MaxEinstrahlungWm2 = 1058.93
        };
        e.Module.Add(new SimulationErgebnisCtrl.PvModulZeile("Modul A", 1.7, 120, 42.5));
        // W11b-B-8: ein CEC-Modul ohne Masse - die Flaeche ist geschaetzt.
        e.Module.Add(new SimulationErgebnisCtrl.PvModulZeile("Modul B", 51.2, 20, 13.26, true));
        return e;
    }

    // ---- Windows-Abnahme V3 07.09.2026: W11b-B-6 bis B-8 ------------------------

    [Fact]
    public void Photovoltaik_zeigt_Erzeugung_und_genutzten_Anteil_als_zwei_Zeilen()
    {
        var seite = PvZeichnen();
        Assert.Contains(Resource.SIMERG_LBL_PV_GESAMT, seite.Markup);
        Assert.Contains(Resource.SIMERG_LBL_PV_GENUTZT, seite.Markup);
        Assert.Contains("42,50", seite.Markup);
        Assert.Contains("30,00", seite.Markup);
    }

    [Fact]
    public void Photovoltaik_zeigt_die_Einstrahlung_in_Watt_je_Quadratmeter()
    {
        var seite = PvZeichnen();
        Assert.Contains("W/m²", seite.Markup);
        Assert.Contains("1.058,93", seite.Markup);   // N2 (W11b-B-13)

        // Nur die EINHEITENSPALTEN der Kennzahlenlisten; das Bild daneben nennt
        // seit der Etappe DG-E3 „kW" als y-Achsentitel in seinem SVG.
        Assert.All(seite.FindAll("dl.epos-simerg-werte dd.epos-simerg-einheit"),
                   z => Assert.NotEqual("kW", z.TextContent.Trim()));
    }

    // ---- W11b‑B‑20: die Kennzahlenliste ------------------------------------

    /// <summary>
    /// <b>Anwenderwunsch 09.09.2026.</b> Jede Beschriftung trug ihre Einheit
    /// ZWEIMAL — im Text („… der Module [MWh/a]:“) und in der Einheitenspalte
    /// daneben. Sie steht jetzt nur noch in ihrer Spalte; der Ressourcentext ist
    /// gekürzt (de + en).
    /// </summary>
    [Fact]
    public void Photovoltaik_nennt_die_Einheit_nur_in_ihrer_Spalte()
    {
        var seite = PvZeichnen();

        Assert.Equal("Gesamte Stromerzeugung der Module:", Resource.SIMERG_LBL_PV_GESAMT);
        Assert.All(seite.FindAll("dl.epos-simerg-werte dt"),
                   z => Assert.DoesNotContain("MWh", z.TextContent));
        Assert.DoesNotContain("[W/m²]", seite.Markup);
        Assert.Contains("<dd class=\"epos-simerg-einheit\">MWh/a</dd>", seite.Markup);
    }

    /// <summary>
    /// Sieben Zeilen in EINER Liste, in der sich Erzeugung, Bedarf und
    /// Einstrahlung abwechselten. Drei Unterabschnitte tragen die Ordnung; der
    /// Reststrombedarf ist die betonte Zeile seiner Gruppe.
    /// </summary>
    [Fact]
    public void Photovoltaik_gliedert_seine_Felder_in_Erzeugung_Bedarf_und_Einstrahlung()
    {
        var seite = PvZeichnen();

        // W11b‑B‑23: drei Hauptgruppen mit Balken, kein h3; die zwei Stromgruppen
        // stehen NEBENEINANDER in der ersten Rasterzeile.
        Assert.Equal(new[] { "Erzeugung", "Bedarf und Deckung", "Einstrahlung" },
                     seite.FindAll("h2.epos-gruppenkopf-titel").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Empty(seite.FindAll("h3.epos-untergruppe"));
        Assert.Contains(seite.FindAll("div.epos-simerg-spalten"),
                        z => z.QuerySelectorAll("dl.epos-simerg-werte").Length == 2);

        Assert.Equal(3, seite.FindAll("dl.epos-simerg-werte").Count);
        Assert.Equal(
            new[] { "Gesamte Stromerzeugung der Module:", "davon direkt genutzt:", "Überschuss:" },
            Zeilen(seite, 0));
        Assert.Equal(new[] { "Strombedarf:", "Reststrombedarf:", "Strombedarfsdeckung:" },
                     Zeilen(seite, 1));
        Assert.Equal(new[] { "Maximale solare Einstrahlung:" }, Zeilen(seite, 2));

        Assert.Equal(new[] { "Reststrombedarf:" },
                     seite.FindAll("dt.epos-simerg-abschluss").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>Auch hier steht die Liste ALLEIN in ihrer Rasterzeile.</summary>
    [Fact]
    public void Photovoltaik_gibt_der_Kennzahlenliste_die_ganze_Rasterzeile()
    {
        var seite = PvZeichnen();
        Assert.Single(seite.FindAll("section.epos-simerg-kennzahlenzeile"));
    }

    [Fact]
    public void Photovoltaik_kennzeichnet_eine_geschaetzte_Flaeche()
    {
        var seite = PvZeichnen();
        var zellen = seite.FindAll("td[title]");
        Assert.Single(zellen);
        Assert.StartsWith("≈", zellen[0].TextContent.Trim());
        Assert.Equal(Resource.SIMERG_TIP_FLAECHE_GESCHAETZT, zellen[0].GetAttribute("title"));
    }

    private IRenderedComponent<PhotovoltaikReiter> PvZeichnen()
        => Render<PhotovoltaikReiter>(p => p
            .Add(x => x.Daten, Pv())
            .Add(x => x.Modell, Modell));

    /// <summary>
    /// Befund W11-B22: Der Deckungsgrad stand in zwei von drei Referenzprojekten
    /// auf „NaN". Das DTO liefert 0,00.
    /// </summary>
    [Fact]
    public void Photovoltaik_zeigt_den_Deckungsgrad_als_Zahl()
    {
        var seite = PvZeichnen();

        Assert.Contains("0,00", seite.Markup);
        Assert.DoesNotContain("NaN", seite.Markup);
    }

    // ---- W11b‑B‑19: EINE Reihenzeile mit ALLEN vier Reihen -----------------

    /// <summary>
    /// <b>Anwenderwunsch 09.09.2026.</b> Die REIHENZEILE trägt JEDE Reihe des
    /// Bildes, in der Beschriftung ihrer LEGENDE — man hakt ab, was man dort
    /// liest. Die Vorbelegung ist die des Vorbilds: Grundreihen an, Zusatzreihen
    /// aus (wörtlich :4676-4679). Über ihr steht seit GM‑1 die Zeile mit der
    /// Darstellungsart.
    /// </summary>
    [Fact]
    public void Photovoltaik_traegt_eine_Schalterzeile_mit_allen_vier_Reihen()
    {
        var seite = PvZeichnen();

        Assert.Equal(2, seite.FindAll("div.epos-simerg-schalter").Count);
        Assert.Equal(new[] { "Strombedarf", "Photovoltaik", "Überschuss", "Speicherfüllstand" },
                     Schalterzeile(seite, 1));

        Assert.True(Kasten(seite, 0, 1).HasAttribute("checked"));
        Assert.True(Kasten(seite, 1, 1).HasAttribute("checked"));
        Assert.False(Kasten(seite, 2, 1).HasAttribute("checked"));
        Assert.False(Kasten(seite, 3, 1).HasAttribute("checked"));
    }

    /// <summary>
    /// <b>Der Schalter „sortiert"</b> (Welle GM‑1) steht in seiner EIGENEN Zeile
    /// über den Reihen — dieselbe Trennung wie im Kesselreiter — und wechselt nur
    /// die Darstellungsart: Die gewählten Reihen bleiben dieselben.
    /// </summary>
    [Fact]
    public void Photovoltaik_wechselt_den_Bildauftrag_mit_dem_Sortiertschalter()
    {
        var seite = PvZeichnen();

        Assert.Equal(new[] { "sortiert" }, Schalterzeile(seite, 0));
        _auftraege.Clear();

        Kasten(seite, 0, 0).Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Photovoltaik && a.Sortiert);
        Assert.Equal(new[] { "STROMBEDARF", "PHOTOVOLTAIK" },
                     _auftraege.Last(a => a.Bild == Bilder.Photovoltaik).Reihen!.ToArray());
    }

    /// <summary>
    /// Beide Zusatzreihen stehen beim Aufbau AUS; das Bild traegt dann nur
    /// Strombedarf und Photovoltaik (woertlich :4676-4679).
    /// </summary>
    [Fact]
    public void Photovoltaik_startet_mit_zwei_abgeschalteten_Reihen()
    {
        var seite = PvZeichnen();

        Assert.Equal(new[] { "STROMBEDARF", "PHOTOVOLTAIK" },
                     seite.Instance.GewaehlteReihen.ToArray());
        Assert.DoesNotContain("SPEICHERFUELLSTAND", seite.Instance.GewaehlteReihen);
    }

    /// <summary>B3: Der Speicherfuellstand kommt ueber seinen Haken dazu.</summary>
    [Fact]
    public void Photovoltaik_nimmt_den_Speicherfuellstand_ueber_seinen_Haken_dazu()
    {
        var seite = PvZeichnen();
        Kasten(seite, 3, 1).Change(true);

        Assert.Contains("SPEICHERFUELLSTAND", seite.Instance.GewaehlteReihen);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Photovoltaik
                                         && a.Reihen is not null
                                         && a.Reihen.Contains("SPEICHERFUELLSTAND"));
    }

    /// <summary>
    /// NEU an W11b‑B‑19: auch eine GRUNDREIHE ist abwählbar. Sie fällt aus dem
    /// Bildauftrag; die Reihenfolge der übrigen bleibt.
    /// </summary>
    [Fact]
    public void Photovoltaik_nimmt_die_abgewaehlte_Grundreihe_aus_dem_Bildauftrag()
    {
        var seite = PvZeichnen();
        _auftraege.Clear();

        Kasten(seite, 0, 1).Change(false);              // Strombedarf

        Assert.Equal(new[] { "PHOTOVOLTAIK" }, seite.Instance.GewaehlteReihen.ToArray());
        Assert.Equal(new[] { "PHOTOVOLTAIK" },
                     _auftraege.Last(a => a.Bild == Bilder.Photovoltaik).Reihen!.ToArray());
    }

    /// <summary>
    /// ALLE abgewählt heisst KEINE Reihe und nicht „alle“ — der Auftrag trägt eine
    /// LEERE Liste, aus der die Hülle den Leerhinweis des Renderers zeichnet.
    /// </summary>
    [Fact]
    public void Photovoltaik_ohne_gewaehlte_Reihe_gibt_eine_leere_Liste()
    {
        var seite = PvZeichnen();
        _auftraege.Clear();

        Kasten(seite, 0, 1).Change(false);
        Kasten(seite, 1, 1).Change(false);

        Assert.Empty(seite.Instance.GewaehlteReihen);
        Assert.Empty(_auftraege.Last(a => a.Bild == Bilder.Photovoltaik).Reihen!);
    }

    [Fact]
    public void Photovoltaik_zeigt_die_Modultabelle_mit_fuenf_Spalten()
    {
        var seite = PvZeichnen();
        Assert.Equal(5, seite.FindAll("table.epos-raster thead th").Count);
    }

    // =====================================================================
    //  W11b‑B‑23 — der Leerhinweis nennt die fehlende Komponente
    // =====================================================================

    /// <summary>
    /// Heizkessel und Wärmepumpe sagten seit je, WELCHE Komponente dem Lauf
    /// fehlt; BHKW, Solarthermie und Photovoltaik teilten sich den allgemeinen
    /// Satz „Bitte zuerst die Simulation durchführen." Jetzt nennt jeder Reiter
    /// seine eigene — dieselbe Satzform, derselbe erste Satz.
    /// </summary>
    [Fact]
    public void Die_Leerhinweise_nennen_die_fehlende_Komponente()
    {
        Assert.Contains("BHKW", Render<BhkwReiter>(p => p.Add(x => x.Modell, Modell))
                                    .Find("p.epos-simerg-hinweis").TextContent);
        Assert.Contains("Solarthermie", Render<SolarthermieReiter>(p => p.Add(x => x.Modell, Modell))
                                            .Find("p.epos-simerg-hinweis").TextContent);
        Assert.Contains("Photovoltaik", Render<PhotovoltaikReiter>(p => p.Add(x => x.Modell, Modell))
                                            .Find("p.epos-simerg-hinweis").TextContent);
    }

    // =====================================================================
    //  DER BAUSTEIN DiagrammSvg (Etappe DG-E3, Gruppe (a))
    //
    //  Jedes der vier Bilder traegt eine Zeitachse und steht deshalb als SVG
    //  im Baum: Der Zeitausschnitt ist die viewBox der Zeichenflaeche und
    //  kostet keinen Rundlauf in den Kern mehr (DG-E3-9).
    // =====================================================================

    /// <summary>Das (einzige) Diagramm des Reiters.</summary>
    private static DiagrammSvg Bildrahmen<T>(IRenderedComponent<T> seite)
        where T : class, IComponent
        => seite.FindComponent<DiagrammSvg>().Instance;

    /// <summary>Die Beschriftungen der Knoepfe am Bild, in ihrer Reihenfolge.</summary>
    private static string[] Knoepfe<T>(IRenderedComponent<T> seite) where T : class, IComponent
        => seite.FindComponent<DiagrammSvg>()
                .FindAll("button.epos-diagramm-knopf")
                .Select(k => k.TextContent.Trim()).ToArray();

    /// <summary>
    /// <b>Jeder der vier Reiter trägt GENAU EIN <c>DiagrammSvg</c></b> — mit seiner
    /// eigenen Kennung, damit zwei Bilder nie dieselben <c>clipPath</c>-Kennungen
    /// bekommen.
    /// </summary>
    [Fact]
    public void Der_Kesselreiter_traegt_ein_DiagrammSvg_mit_seiner_Kennung()
        => Bildpruefung(KesselZeichnen(Kessel()), "simerg-heizkessel");

    [Fact]
    public void Der_Solarreiter_traegt_ein_DiagrammSvg_mit_seiner_Kennung()
        => Bildpruefung(SolarZeichnen(), "simerg-solarthermie");

    [Fact]
    public void Der_Bhkwreiter_traegt_ein_DiagrammSvg_mit_seiner_Kennung()
        => Bildpruefung(BhkwZeichnen(Bhkw()), "simerg-bhkw");

    [Fact]
    public void Der_Pvreiter_traegt_ein_DiagrammSvg_mit_seiner_Kennung()
        => Bildpruefung(PvZeichnen(), "simerg-photovoltaik");

    private static void Bildpruefung<T>(IRenderedComponent<T> seite, string kennung)
        where T : class, IComponent
    {
        Assert.Single(seite.FindComponents<DiagrammSvg>());
        Assert.Equal(kennung, Bildrahmen(seite).Kennung);
        Assert.Equal("kW", Bildrahmen(seite).Einheit);
        Assert.Single(seite.FindAll("svg.epos-flaeche"));
    }

    /// <summary>
    /// Der Zoom ist Bedienung am Bild: Über jedem der vier Bilder stehen „Bereich"
    /// und „1:1".
    /// </summary>
    [Fact]
    public void Jedes_der_vier_Bilder_traegt_den_Bereichsknopf()
    {
        string[] soll = { "Bereich", "1:1" };

        Assert.Equal(soll, Knoepfe(KesselZeichnen(Kessel())));
        Assert.Equal(soll, Knoepfe(SolarZeichnen()));
        Assert.Equal(soll, Knoepfe(BhkwZeichnen(Bhkw())));
        Assert.Equal(soll, Knoepfe(PvZeichnen()));
    }
}
