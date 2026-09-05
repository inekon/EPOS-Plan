using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der PARAMETER-Reiter der Ergebnisseite (iU9-W11b.1), Vorbild
/// <c>Form_Simulation_Detail.tabPage_Parameter</c> mit
/// <c>tabControl_Einstellungen</c> und seinen fuenf Unterblaettern.
///
/// <para>Soll: die Unterblaetter nach <c>Tool_1..6</c>, jedes Feld schreibt
/// SOFORT, die Geraetedaten des Speichers sind sichtbar und gesperrt, ohne
/// aktive Variante sind die Speicherfelder Attrappen, der Optimierungsknopf
/// erscheint nur mit Sprungdelegat.</para>
///
/// <para>Die Sprache ist festgelegt (Regel seit W8): Die Beschriftungen kommen
/// aus <c>MyResource.Resource</c> und folgen der Oberflaechensprache des
/// Fadens.</para>
/// </summary>
public class ParameterReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    private readonly List<(double Wert, string Einheit)> _netzverluste = new();
    private readonly List<int> _betriebsart = new();
    private readonly List<int> _grenze = new();
    private readonly List<bool> _heizstab = new();
    private readonly List<double> _bereitschaft = new();
    private readonly List<(string Feld, string Wert)> _speicher = new();

    public ParameterReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        base.Dispose(disposing);
    }

    // =====================================================================
    // Probendaten
    // =====================================================================

    private SimulationErgebnisDienste Dienste() => new SimulationErgebnisDienste
    {
        NetzverlusteSchreiben = (w, e) => _netzverluste.Add((w, e)),
        BetriebsartSchreiben = w => _betriebsart.Add(w),
        LeistungsgrenzeSchreiben = w => _grenze.Add(w),
        HeizstabSchreiben = w => _heizstab.Add(w),
        BereitschaftSchreiben = w => _bereitschaft.Add(w),
        SpeicherfeldSchreiben = (f, w) => _speicher.Add((f, w))
    };

    private static ParameterDaten Alles() => new ParameterDaten
    {
        Unterblaetter = new[]
        {
            ParameterBlatt.Bedarf, ParameterBlatt.Bhkw, ParameterBlatt.Stromspeicher,
            ParameterBlatt.Waermepumpe, ParameterBlatt.Heizkessel
        },
        Netzverluste = 10,
        Betriebsart = 1,
        UntersteLeistungsgrenze = 30,
        Heizstab = true,
        Bereitschaft = 8760,
        Speicher = new SpeicherParameterDaten
        {
            VarianteVorhanden = true,
            Variantenstatus = "Variante: Speicher 1",
            SoCMinProzent = 10,
            SoCMaxProzent = 90,
            SoCMinKwh = "1,0 kWh",
            SoCMaxKwh = "9,0 kWh",
            Ladeschwellwert = 90,
            LadeleistungKw = 11.04,
            KapazitaetKwh = 10.0,
            Betriebsart = "Gruenstrom",
            Berechnungsart = "Dauernutzung",
            Betriebsarten = new[]
            {
                new Steuerwahl("Gruenstrom", "Grünstrom"),
                new Steuerwahl("Graustrom", "Graustrom")
            },
            Berechnungsarten = new[]
            {
                new Steuerwahl("Dauernutzung", "Dauernutzung"),
                new Steuerwahl("Arbitrage", "Preissteuerung / Arbitrage")
            },
            KompatibilitaetMoeglich = true,
            LadenAusPv = true,
            Kapitalzins = 3.5,
            Nutzungsdauer = 15,
            Preisquelle = "Fixpreis",
            Preisquellen = new[]
            {
                new Steuerwahl("Fixpreis", "Fixpreis"),
                new Steuerwahl("Spotmarkt", "Spotmarkt")
            },
            PreisreiheLabel = "Preisreihe",
            Preisreihen = new (int, string)[] { (7, "Spot 2024") },
            PreisreiheMoeglich = true
        }
    };

    private IRenderedComponent<ParameterReiter> Zeichnen(ParameterDaten daten,
                                                         bool gesperrt = false,
                                                         bool optimierung = true)
        => Render<ParameterReiter>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.Gesperrt, gesperrt)
            .Add(x => x.OptimierungMoeglich, optimierung));

    // =====================================================================
    // Sichtbarkeit der Unterblaetter
    // =====================================================================

    /// <summary>
    /// Fuenf Unterblaetter, nicht vier (Befund W11-B1) — und in der Reihenfolge,
    /// in der der Vorlaeufer sie einhaengte.
    /// </summary>
    [Fact]
    public void Fuenf_Unterblaetter_stehen_in_der_Reihenfolge_der_Tools()
    {
        var seite = Zeichnen(Alles());
        var knoepfe = seite.FindAll("button[role='tab']");

        Assert.Equal(5, knoepfe.Count);
        Assert.Equal("Wärme-/Strombedarf", knoepfe[0].TextContent);
        Assert.Equal("BHKW", knoepfe[1].TextContent);
        Assert.Equal("Stromspeicher", knoepfe[2].TextContent);
        Assert.Equal("Wärmepumpe", knoepfe[3].TextContent);
        Assert.Equal("Heizkessel", knoepfe[4].TextContent);
    }

    /// <summary>
    /// „Bedarf" ist immer dabei — auch in einem Projekt ohne jeden Erzeuger
    /// (<c>UpdateTabPages</c> :2846).
    /// </summary>
    [Fact]
    public void Ohne_Erzeuger_bleibt_nur_das_Bedarfsblatt()
    {
        var seite = Zeichnen(new ParameterDaten());

        Assert.Single(seite.FindAll("button[role='tab']"));
        Assert.Equal(ParameterBlatt.Bedarf, seite.Instance.AktivesBlatt);
    }

    // =====================================================================
    // Jedes Feld schreibt sofort
    // =====================================================================

    [Fact]
    public void Netzverluste_schreiben_sofort()
    {
        var seite = Zeichnen(Alles());
        seite.Find("input[type='text']").Input("12");

        Assert.Single(_netzverluste);
        Assert.Equal(12.0, _netzverluste[0].Wert);
        Assert.Equal("%", _netzverluste[0].Einheit);
    }

    /// <summary>
    /// Befund W11-B8 entfaellt: Ein Modellfeld loest kein Ereignis aus, das den
    /// gerade gelesenen Wert zurueckschriebe. Die Betriebsart schreibt genau
    /// EINMAL je Klick.
    /// </summary>
    [Fact]
    public void Betriebsart_schreibt_genau_einmal()
    {
        var seite = Zeichnen(Alles());
        seite.Find("button[role='tab'][id='reiter-BHKW']").Click();

        var wahl = seite.FindAll("input[type='radio']");
        Assert.Equal(3, wahl.Count);
        wahl[2].Change(true);

        Assert.Single(_betriebsart);
        Assert.Equal(2, _betriebsart[0]);
    }

    [Fact]
    public void Heizstab_und_Bereitschaft_schreiben_sofort()
    {
        var seite = Zeichnen(Alles());

        seite.Find("button[role='tab'][id='reiter-WAERMEPUMPE']").Click();
        seite.Find("input[type='checkbox']").Change(false);
        Assert.Equal(new[] { false }, _heizstab);

        seite.Find("button[role='tab'][id='reiter-HEIZKESSEL']").Click();
        seite.Find("input[type='text']").Input("4000");
        Assert.Equal(new[] { 4000.0 }, _bereitschaft);
    }

    // =====================================================================
    // Das Stromspeicherblatt (P3)
    // =====================================================================

    /// <summary>
    /// Die beiden Geraetedaten sind sichtbar und LESBAR, aber gesperrt — die
    /// Regel seit iU9-W2.3 ersetzt die vier leeren Ereignisbehandler des
    /// Vorlaeufers (Befund W11-B10).
    /// </summary>
    [Fact]
    public void Geraetedaten_stehen_gesperrt_aber_lesbar()
    {
        var seite = Zeichnen(Alles());
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();

        var gesperrt = seite.FindAll("input[disabled]");
        Assert.Contains(gesperrt, e => e.GetAttribute("value") == "11,04");
        Assert.Contains(gesperrt, e => e.GetAttribute("value") == "10");
    }

    /// <summary>
    /// Ohne aktive Variante gaebe es kein Ziel fuer das Zurueckschreiben — dann
    /// sind die Felder Attrappen (<c>LeseSpeicherVariante</c> :6455-6470).
    /// </summary>
    [Fact]
    public void Ohne_aktive_Variante_sind_die_Speicherfelder_gesperrt()
    {
        ParameterDaten d = Alles();
        d.Speicher.VarianteVorhanden = false;
        d.Speicher.Variantenstatus = "Das Projekt führt keine Speichervariante.";

        var seite = Zeichnen(d);
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();

        Assert.Empty(seite.FindAll("input:not([disabled])"));
        Assert.Contains("epos-simerg-warn", seite.Find("p.epos-simerg-status").ClassName);
    }

    [Fact]
    public void Speicherfelder_schreiben_ueber_ihren_Feldschluessel()
    {
        var seite = Zeichnen(Alles());
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();

        seite.FindAll("input[type='text']:not([disabled])")[0].Input("15");
        seite.FindAll("select")[0].Change("1");
        seite.FindAll("input[type='checkbox']:not([disabled])")[0].Change(true);

        Assert.Contains(_speicher, z => z.Feld == SpeicherFeld.SoCMin && z.Wert == "15");
        Assert.Contains(_speicher, z => z.Feld == SpeicherFeld.Betriebsart && z.Wert == "Graustrom");
        Assert.Contains(_speicher, z => z.Feld == SpeicherFeld.Kompatibilitaet);
    }

    /// <summary>
    /// Der Ausbaustufen-Schalter „BHKW stromgefuehrt" bleibt in JEDEM Fall
    /// gesperrt — der Anwender soll sehen, dass es ihn gibt, aber nicht auf
    /// eine Wirkung warten, die ausbleibt.
    /// </summary>
    [Fact]
    public void Der_Ausbaustufen_Schalter_bleibt_gesperrt()
    {
        var seite = Zeichnen(Alles());
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();

        var haken = seite.FindAll("input[type='checkbox']");
        Assert.Contains(haken, e => e.HasAttribute("disabled"));
    }

    // =====================================================================
    // Die Sprungbruecke und der Sperrzustand
    // =====================================================================

    /// <summary>Kein Sprungdelegat = kein Knopf (Regel seit W2.2).</summary>
    [Fact]
    public void Ohne_Sprungdelegat_bleibt_der_Optimierungsknopf_weg()
    {
        var seite = Zeichnen(Alles(), optimierung: false);
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();

        Assert.DoesNotContain(seite.FindAll("button"),
                              b => b.TextContent.Contains("optimieren"));
    }

    [Fact]
    public void Der_Sperrzustand_sperrt_alle_Felder()
    {
        var seite = Zeichnen(Alles(), gesperrt: true);

        Assert.Empty(seite.FindAll("input:not([disabled])"));
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Parameterblock stellt seine Felder in den Formularraster, einspaltig: Er ist die schmale Spalte der Ergebnisseite, und unter mancher Zahl steht ihre Entsprechung in kWh.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Der_Parameterblock_steht_im_einspaltigen_Formularraster()
    {
        var seite = Zeichnen(Alles());

        var raster = seite.FindAll(".epos-simerg-felder .epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Equal(raster.Count,
                     seite.FindAll(".epos-simerg-felder .epos-formularraster--einspaltig").Count);
        Assert.NotEmpty(seite.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));
    }
}
