using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der PARAMETER-Reiter der Ergebnisseite (iU9-W11b.1), Vorbild
/// <c>Form_Simulation_Detail.tabPage_Parameter</c> mit
/// <c>tabControl_Einstellungen</c>.
///
/// <para>Soll: die Unterblaetter nach <c>Tool_1..6</c> und jedes Feld schreibt
/// SOFORT.</para>
///
/// <para><b>Es sind VIER Unterblaetter</b> (W11b‑B‑28, Anwenderwunsch
/// 10.09.2026): Das Blatt „Stromspeicher" ist als <c>SpeicherParameterBlock</c>
/// in den ERGEBNISreiter gezogen. Seine Faelle stehen jetzt in
/// <c>SpeicherParameterBlockTests</c> — samt Optimierungsknopf, der hier
/// geprueft wurde.</para>
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
        BereitschaftSchreiben = w => _bereitschaft.Add(w)
    };

    private static ParameterDaten Alles() => new ParameterDaten
    {
        Unterblaetter = new[]
        {
            ParameterBlatt.Bedarf, ParameterBlatt.Bhkw,
            ParameterBlatt.Waermepumpe, ParameterBlatt.Heizkessel
        },
        Netzverluste = 10,
        Betriebsart = 1,
        UntersteLeistungsgrenze = 30,
        Heizstab = true,
        Bereitschaft = 8760
    };

    private IRenderedComponent<ParameterReiter> Zeichnen(ParameterDaten daten,
                                                         bool gesperrt = false)
        => Render<ParameterReiter>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.Gesperrt, gesperrt));

    // =====================================================================
    // Sichtbarkeit der Unterblaetter
    // =====================================================================

    /// <summary>
    /// VIER Unterblaetter in der Reihenfolge, in der der Vorlaeufer sie
    /// einhaengte — und OHNE „Stromspeicher".
    ///
    /// <para>Der Vorlaeufer fuehrte fuenf (Befund W11-B1), und dieser Reiter bis
    /// W11b‑B‑28 auch. Das Speicherblatt steht seit dem Anwenderwunsch vom
    /// 10.09.2026 im Ergebnisreiter „Stromspeicher"; ein Blatt gleichen Namens
    /// hier waere die zweite Pflegestelle desselben Satzes.</para>
    /// </summary>
    [Fact]
    public void Vier_Unterblaetter_stehen_in_der_Reihenfolge_der_Tools()
    {
        var seite = Zeichnen(Alles());
        var knoepfe = seite.FindAll("button[role='tab']");

        Assert.Equal(4, knoepfe.Count);
        Assert.Equal("Wärme-/Strombedarf", knoepfe[0].TextContent);
        Assert.Equal("BHKW", knoepfe[1].TextContent);
        Assert.Equal("Wärmepumpe", knoepfe[2].TextContent);
        Assert.Equal("Heizkessel", knoepfe[3].TextContent);

        Assert.DoesNotContain(knoepfe, k => k.TextContent == Resource.SIM_STROMSPEICHER);
    }

    /// <summary>
    /// Und mit dem Blatt sind die SPEICHERFELDER weg: keine Betriebsfuehrung,
    /// keine Preisquelle, kein Optimierungsknopf. Geprueft am Markup des ganzen
    /// Reiters, nicht an einem einzelnen Blatt — die Frage ist, ob irgendwo
    /// noch etwas davon steht.
    /// </summary>
    [Fact]
    public void Kein_Speicherfeld_steht_mehr_im_Reiter()
    {
        var seite = Zeichnen(Alles());

        Assert.DoesNotContain(Resource.SP_PARAM_LABEL_SOC_MIN, seite.Markup);
        Assert.DoesNotContain(Resource.SP_PARAM_GRUPPE_BETRIEBSFUEHRUNG, seite.Markup);
        Assert.DoesNotContain(Resource.PREIS_PARAM_GRUPPE_PREISQUELLE, seite.Markup);
        Assert.DoesNotContain(seite.FindAll("button"),
                              b => b.TextContent.Contains("optimieren"));
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

    /// <summary>
    /// <b>Die projektweite BHKW-Untergrenze steht hier — und nur hier</b>
    /// (Anwenderentscheid <b>W6‑E‑7</b> vom 07.09.2026). Sie zeigt den gepflegten Wert,
    /// trägt ihre Einheit am Feld und schreibt sofort.
    ///
    /// <para>Die Beschriftung nennt <b>keine feste Prozentzahl mehr</b>: Bis W6‑E‑7 hieß
    /// sie „… der Module [30%]" und behauptete damit einen Wert, den der Rechenweg als
    /// stillen Fallback trug. Der Fallback ist gefallen; im Feld steht, was gilt.</para>
    /// </summary>
    [Fact]
    public void Die_projektweite_Untergrenze_zeigt_ihren_Wert_und_schreibt_sofort()
    {
        var seite = Zeichnen(Alles());
        seite.Find("button[role='tab'][id='reiter-BHKW']").Click();

        Assert.DoesNotContain("[30%]", seite.Markup);
        Assert.Contains(Resource.SIMERG_LBL_UNTERE_LEISTUNGSGRENZE, seite.Markup);

        var feld = seite.Find("input[type='text']");
        Assert.Equal("30", feld.GetAttribute("value"));

        feld.Input("12");

        Assert.Equal(new[] { 12 }, _grenze);
    }

    /// <summary>
    /// Und darunter steht, was eine <b>0</b> bedeutet — seit W6‑E‑7 rechnet sie als 0
    /// und nicht mehr still als 30 %. Ohne diese Zeile wäre der Unterschied aus der
    /// Maske heraus nicht erkennbar.
    /// </summary>
    [Fact]
    public void Unter_der_Untergrenze_steht_ihre_Herleitung()
    {
        var seite = Zeichnen(Alles());
        seite.Find("button[role='tab'][id='reiter-BHKW']").Click();

        Assert.Contains(Resource.SIMERG_HRL_UNTERE_LEISTUNGSGRENZE,
                        seite.Find("p.epos-herleitung").TextContent);
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
    // Der Sperrzustand
    // =====================================================================

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
