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

        seite.Find("button").Click();
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

    [Fact]
    public void Solarthermie_zeigt_fuenf_Felder_und_die_Kollektortabelle()
    {
        var seite = Render<SolarthermieReiter>(p => p
            .Add(x => x.Daten, Solar())
            .Add(x => x.Bild, Bild));

        Assert.Contains("8,40", seite.Markup);
        Assert.Contains("40,50", seite.Markup);
        Assert.Equal(6, seite.FindAll("table.epos-raster thead th").Count);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Solarthermie);
    }

    /// <summary>Ohne bekannten Bezug bleibt das Deckungsfeld LEER (woertlich :4603).</summary>
    [Fact]
    public void Solarthermie_laesst_die_Deckung_ohne_Bezug_leer()
    {
        var seite = Render<SolarthermieReiter>(p => p
            .Add(x => x.Daten, Solar(deckung: false))
            .Add(x => x.Bild, Bild));

        Assert.DoesNotContain("8,40", seite.Markup);
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
        Assert.Contains("1505", seite.Markup);
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
            UeberschussMwh = 12.25,
            DeckungProzent = 0.0,
            StrombedarfMwh = 120.5,
            ReststrombedarfMwh = 90.0,
            MaxLeistungKw = 1500.0
        };
        e.Module.Add(new SimulationErgebnisCtrl.PvModulZeile("Modul A", 1.7, 120, 42.5));
        return e;
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

    /// <summary>
    /// Beide Haken stehen beim Aufbau AUS; das Bild traegt dann nur Strombedarf
    /// und Photovoltaik (woertlich :4676-4679).
    /// </summary>
    [Fact]
    public void Photovoltaik_startet_mit_zwei_abgeschalteten_Reihen()
    {
        var seite = PvZeichnen();

        Assert.Equal(2, seite.Instance.GewaehlteReihen.Count);
        Assert.DoesNotContain("SPEICHERFUELLSTAND", seite.Instance.GewaehlteReihen);
    }

    /// <summary>B3: Der Speicherfuellstand kommt ueber seinen Haken dazu.</summary>
    [Fact]
    public void Photovoltaik_nimmt_den_Speicherfuellstand_ueber_seinen_Haken_dazu()
    {
        var seite = PvZeichnen();
        seite.FindAll("input[type='checkbox']")[1].Change(true);

        Assert.Contains("SPEICHERFUELLSTAND", seite.Instance.GewaehlteReihen);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Photovoltaik
                                         && a.Reihen is not null
                                         && a.Reihen.Contains("SPEICHERFUELLSTAND"));
    }

    [Fact]
    public void Photovoltaik_zeigt_die_Modultabelle_mit_fuenf_Spalten()
    {
        var seite = PvZeichnen();
        Assert.Equal(5, seite.FindAll("table.epos-raster thead th").Count);
    }
}
