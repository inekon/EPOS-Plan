using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
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
/// </summary>
public class ErzeugerReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;
    private readonly List<Bildauftrag> _auftraege = new();

    public ErzeugerReiterTests()
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

    private byte[]? Bild(Bildauftrag a) { _auftraege.Add(a); return new byte[] { 1 }; }

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

    private IRenderedComponent<HeizkesselReiter> KesselZeichnen(
        SimulationErgebnisCtrl.HeizkesselErgebnis? erg, bool bedarf = true, Action? csv = null)
        => Render<HeizkesselReiter>(p =>
        {
            p.Add(x => x.Daten, erg);
            p.Add(x => x.Brennstoffe, Kesselbrennstoffe());
            p.Add(x => x.BedarfVorhanden, bedarf);
            p.Add(x => x.Bild, Bild);
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
        });

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
    /// <b>Anwenderrückmeldung 08.09.2026 (W11b‑B‑15).</b> Neun Zeilen in EINER
    /// Liste mischten Wärme, Strom und die zwei Leistungen der Auslegung; die
    /// Restwärme stand noch VOR der Produktion, aus der sie sich ergibt. Drei
    /// Unterabschnitte, und der Rest schliesst seine Gruppe betont ab. Der
    /// Brennstoffblock bleibt eine eigene Gruppe mit dunklem Balken.
    /// </summary>
    [Fact]
    public void Kessel_gliedert_seine_Felder_in_Waerme_Strom_und_Auslegung()
    {
        var seite = KesselZeichnen(Kessel());

        Assert.Equal(new[] { "Wärme", "Strom", "Auslegung" },
                     seite.FindAll("h3.epos-untergruppe").Select(k => k.TextContent.Trim()).ToArray());

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

    /// <summary>Ohne Projektbedarf steht „0" und nicht „NaN" (woertlich :4530).</summary>
    [Fact]
    public void Kessel_zeigt_ohne_Bedarf_eine_Null()
    {
        var seite = KesselZeichnen(Kessel(), bedarf: false);
        Assert.DoesNotContain("36,25", seite.Markup);
    }

    [Fact]
    public void Kessel_wechselt_den_Bildauftrag_mit_dem_Sortiertschalter()
    {
        var seite = KesselZeichnen(Kessel());
        _auftraege.Clear();

        seite.Find("input[type='checkbox']").Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Heizkessel && a.Sortiert);
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
            .Add(x => x.Bild, Bild));

    [Fact]
    public void Solarthermie_zeigt_fuenf_Felder_und_die_Kollektortabelle()
    {
        var seite = SolarZeichnen();

        Assert.Contains("8,40", seite.Markup);
        Assert.Contains("40,50", seite.Markup);
        Assert.Equal(6, seite.FindAll("table.epos-raster thead th").Count);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Solarthermie);
    }

    /// <summary>Ohne bekannten Bezug bleibt das Deckungsfeld LEER (woertlich :4603).</summary>
    [Fact]
    public void Solarthermie_laesst_die_Deckung_ohne_Bezug_leer()
    {
        var seite = SolarZeichnen(deckung: false);

        Assert.DoesNotContain("8,40", seite.Markup);
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

        Assert.Equal(new[] { "Wärmebedarf", "Wärmeproduktion" }, Schalterzeile(seite));
        Assert.All(seite.FindAll("div.epos-simerg-schalter")[0]
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

        Kasten(seite, 0).Change(false);                 // Wärmebedarf

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

        Kasten(seite, 0).Change(false);
        Kasten(seite, 1).Change(false);

        Assert.Empty(seite.Instance.GewaehlteReihen);
        Assert.Empty(_auftraege.Last(a => a.Bild == Bilder.Solarthermie).Reihen!);
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

        Assert.Equal(new[] { "Wärme" },
                     seite.FindAll("h3.epos-untergruppe").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Wärmebedarf:", "Gesamte Wärmeleistung der Module:", "Überschuß:",
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
        var seite = Render<SolarthermieReiter>(p => p.Add(x => x.Bild, Bild));

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
        IReadOnlyList<Brennstoffzeile>? brennstoffe = null)
        => Render<BhkwReiter>(p => p
            .Add(x => x.Daten, erg)
            .Add(x => x.Praesent, praesent)
            .Add(x => x.Brennstoffe, brennstoffe ?? new[]
            {
                new Brennstoffzeile("Gasverbrauch (Hu):", 62.0, true)
            })
            .Add(x => x.Bild, Bild));

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

        Assert.Equal(new[] { "Wärme", "Strom", "Betrieb" },
                     seite.FindAll("h3.epos-untergruppe").Select(k => k.TextContent.Trim()).ToArray());

        var listen = seite.FindAll("dl.epos-simerg-werte");
        Assert.Equal(4, listen.Count);          // drei Gruppen + der Brennstoffblock

        Assert.Equal(
            new[] { "Wärmebedarf:", "Wärmeproduktion:", "Wärmeüberschuß (*):",
                    "davon in den Speicher:", "aus dem Speicher gedeckt:",
                    "Wärmebedarfsdeckung:", "Restwärmebedarf:" },
            listen[0].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Strombedarf:", "Stromproduktion:", "Strombedarfsdeckung:", "Reststrombedarf:" },
            listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(
            new[] { "Vbh thermisch, Summe Module", "Vbh thermisch, Mittel Module",
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
        Assert.DoesNotContain(">kW<", seite.Markup);
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

        Assert.Equal(new[] { "Erzeugung", "Bedarf und Deckung", "Einstrahlung" },
                     seite.FindAll("h3.epos-untergruppe").Select(k => k.TextContent.Trim()).ToArray());

        Assert.Equal(3, seite.FindAll("dl.epos-simerg-werte").Count);
        Assert.Equal(
            new[] { "Gesamte Stromerzeugung der Module:", "davon direkt genutzt:", "Überschuß:" },
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
            .Add(x => x.Bild, Bild));

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

    // ---- W11b‑B‑19: EINE Schalterzeile mit ALLEN vier Reihen ---------------

    /// <summary>
    /// <b>Anwenderwunsch 09.09.2026.</b> Bis dahin trug die Zeile ZWEI Schalter
    /// („Überschuß anzeigen“, „Speicherfüllung anzeigen“), während Strombedarf und
    /// Photovoltaik fest an und nirgends abwählbar waren. Jetzt trägt EINE Zeile
    /// JEDE Reihe, in der Beschriftung ihrer LEGENDE — die zwei alten Schalter
    /// gehen darin auf. Die Vorbelegung bleibt: Grundreihen an, Zusatzreihen aus
    /// (wörtlich :4676-4679).
    /// </summary>
    [Fact]
    public void Photovoltaik_traegt_eine_Schalterzeile_mit_allen_vier_Reihen()
    {
        var seite = PvZeichnen();

        Assert.Single(seite.FindAll("div.epos-simerg-schalter"));
        Assert.Equal(new[] { "Strombedarf", "Photovoltaik", "Überschuss", "Speicherfüllstand" },
                     Schalterzeile(seite));

        Assert.True(Kasten(seite, 0).HasAttribute("checked"));
        Assert.True(Kasten(seite, 1).HasAttribute("checked"));
        Assert.False(Kasten(seite, 2).HasAttribute("checked"));
        Assert.False(Kasten(seite, 3).HasAttribute("checked"));
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
        Kasten(seite, 3).Change(true);

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

        Kasten(seite, 0).Change(false);                 // Strombedarf

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

        Kasten(seite, 0).Change(false);
        Kasten(seite, 1).Change(false);

        Assert.Empty(seite.Instance.GewaehlteReihen);
        Assert.Empty(_auftraege.Last(a => a.Bild == Bilder.Photovoltaik).Reihen!);
    }

    [Fact]
    public void Photovoltaik_zeigt_die_Modultabelle_mit_fuenf_Spalten()
    {
        var seite = PvZeichnen();
        Assert.Equal(5, seite.FindAll("table.epos-raster thead th").Count);
    }
}
