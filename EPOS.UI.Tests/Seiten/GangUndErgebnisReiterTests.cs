using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die drei Reiter des Ergebnisblocks — Waermegang (<c>NavigatorWaerme</c>),
/// Stromgang (<c>NavigatorStrom</c>) und der Ergebnisreiter selbst
/// (R11 + <c>DashboardForm</c>), iU9-W11b.10 und .11.
///
/// <para>Soll: die drei Zustandsachsen des Waermegangs, das Ausblenden
/// fehlender Reihen, der ergaenzte Sortiertumschalter des Stromgangs
/// (Befund W11-B41), die vier Navigationsblaetter und die Autarkiekacheln samt
/// der NICHT gespeicherten Was-waere-wenn-Kapazitaet (Befund W11-B32).</para>
/// </summary>
public class GangUndErgebnisReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;
    private readonly List<Bildauftrag> _auftraege = new();

    public GangUndErgebnisReiterTests()
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
    // Waermegang
    // =====================================================================

    private static WaermegangDaten Waerme() => new WaermegangDaten
    {
        Erzeuger = new[]
        {
            new Ganglinienreihe("WAERMEPUMPE", "Wärmepumpe", true),
            new Ganglinienreihe("HEIZSTAB", "Heizstab", true),
            new Ganglinienreihe("HEIZKESSEL", "Heizkessel", true),
            new Ganglinienreihe("SOLARTHERMIE", "Solarthermie", false),
            new Ganglinienreihe("BHKW_WAERME", "BHKW", false)
        },
        Speicher = new[]
        {
            new Ganglinienreihe("PUFFER_1018023", "Puffer 1", true)
        },
        Bedarfsarten = new (int, string)[] { (-1, "Gesamt"), (0, "Heizung"), (1, "Brauchwasser") }
    };

    private IRenderedComponent<WaermegangReiter> WaermeZeichnen(Action<(int, IReadOnlyList<string>,
                                                                        IReadOnlyList<string>)>? csv = null)
        => Render<WaermegangReiter>(p =>
        {
            p.Add(x => x.Daten, Waerme());
            p.Add(x => x.Bild, Bild);
            if (csv is not null)
                p.Add(x => x.Csv, EventCallback.Factory.Create<(int, IReadOnlyList<string>,
                                                                IReadOnlyList<string>)>(this, csv));
        });

    /// <summary>Fehlende Reihen erscheinen gar nicht — und koennen nicht in den Export.</summary>
    [Fact]
    public void Waermegang_zeigt_nur_die_vorhandenen_Erzeuger()
    {
        var seite = WaermeZeichnen();
        string text = seite.Markup;

        Assert.Contains("Wärmepumpe", text);
        Assert.Contains("Heizkessel", text);
        Assert.DoesNotContain("Solarthermie", text);
    }

    /// <summary>
    /// Die Bedarfsart schaltet zwischen PRODUKTION (−1) und DECKUNG je Kanal —
    /// die Kernachse der E2-Umschaltung (<c>VektorenSetzen</c> :424-453).
    /// </summary>
    [Fact]
    public void Waermegang_meldet_die_Bedarfsart_im_Bildauftrag()
    {
        var seite = WaermeZeichnen();
        Assert.Equal(-1, seite.Instance.Bedarfsart);

        _auftraege.Clear();
        seite.Find("select").Change("1");

        Assert.Equal(1, seite.Instance.Bedarfsart);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Waermegang && a.Kanal == 1);
    }

    /// <summary>Die drei Schalter: sortiert, Bedarfslinie, dazu die zwei Listen.</summary>
    [Fact]
    public void Waermegang_traegt_Sortiert_und_Bedarfslinie()
    {
        var seite = WaermeZeichnen();
        var haken = seite.FindAll("input[type='checkbox']");

        // 2 Schalter + 3 Erzeuger + 1 Speicher (die Mehrfachauswahl hat je Eintrag einen)
        Assert.True(haken.Count >= 2);

        _auftraege.Clear();
        haken[0].Change(true);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Waermegang && a.Sortiert);
    }

    /// <summary>
    /// Der Export ist IMMER chronologisch und traegt Bedarfsart, Erzeuger und
    /// Speicher — woertlich (<c>btn_CsvExport_Click</c> :299-337).
    /// </summary>
    [Fact]
    public void Waermegang_meldet_Bedarfsart_Erzeuger_und_Speicher_an_den_Export()
    {
        (int Kanal, IReadOnlyList<string> Erz, IReadOnlyList<string> Sp) gemeldet = (0, [], []);
        var seite = WaermeZeichnen(w => gemeldet = w);

        seite.Find("button.epos-simerg-knopf").Click();

        Assert.Equal(-1, gemeldet.Kanal);
        Assert.NotNull(gemeldet.Erz);
        Assert.NotNull(gemeldet.Sp);
    }

    // =====================================================================
    // Stromgang
    // =====================================================================

    private static StromgangDaten Strom() => new StromgangDaten
    {
        Reihen = new[]
        {
            new Ganglinienreihe("GESAMT", "Gesamt", true),
            new Ganglinienreihe("PROFIL_LASTGANG", "Lastgangprofil", true),
            new Ganglinienreihe("WAERMEPUMPE", "Wärmepumpe", true),
            new Ganglinienreihe("HEIZSTAB", "Heizstab", false),
            new Ganglinienreihe("BHKW_STROM", "BHKW", true),
            new Ganglinienreihe("PV", "PV", false)
        }
    };

    private IRenderedComponent<StromgangReiter> StromZeichnen(Action<IReadOnlyList<string>>? csv = null)
        => Render<StromgangReiter>(p =>
        {
            p.Add(x => x.Daten, Strom());
            p.Add(x => x.Bild, Bild);
            if (csv is not null)
                p.Add(x => x.Csv, EventCallback.Factory.Create<IReadOnlyList<string>>(this, csv));
        });

    /// <summary>Ausgangszustand „nur Gesamt an" — woertlich <c>SetControl</c> :224-228.</summary>
    [Fact]
    public void Stromgang_startet_mit_nur_der_Gesamtlinie()
    {
        var seite = StromZeichnen();
        Assert.Equal(new[] { "GESAMT" }, seite.Instance.GewaehlteReihen);
    }

    /// <summary>Befund W11-B41: Der Sortiertumschalter ist ERGAENZT.</summary>
    [Fact]
    public void Stromgang_hat_jetzt_einen_Sortiertumschalter()
    {
        var seite = StromZeichnen();
        _auftraege.Clear();

        seite.FindAll("input[type='checkbox']")[0].Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Stromgang && a.Sortiert);
    }

    [Fact]
    public void Stromgang_zeigt_nur_die_vorhandenen_Reihen()
    {
        var seite = StromZeichnen();
        string text = seite.Markup;

        Assert.Contains("Lastgangprofil", text);
        Assert.DoesNotContain("Heizstab", text);
        Assert.DoesNotContain(">PV<", text);
    }

    [Fact]
    public void Stromgang_meldet_die_gewaehlten_Reihen_an_den_Export()
    {
        IReadOnlyList<string> gemeldet = Array.Empty<string>();
        var seite = StromZeichnen(r => gemeldet = r);

        seite.Find("button.epos-simerg-knopf").Click();
        Assert.Equal(new[] { "GESAMT" }, gemeldet);
    }

    // =====================================================================
    // Ergebnisreiter
    // =====================================================================

    private static AutarkieDaten Autarkie(bool pv = true, bool st = true, bool stBekannt = true)
        => new AutarkieDaten
        {
            HatPv = pv,
            HatSolarthermie = st,
            AutarkiePvProzent = 38.1,
            DeckungStProzent = 12.4,
            DeckungStBekannt = stBekannt,
            NutzungsgradStProzent = 62.0,
            Co2ErsparnisKg = 4820.0,
            SpeichernutzenKwh = 1250.0,
            SpeicherKwh = 5.0
        };

    private IRenderedComponent<ErgebnisReiter> ErgebnisZeichnen(AutarkieDaten a,
                                                                Action<double>? kapazitaet = null)
        => Render<ErgebnisReiter>(p =>
        {
            p.Add(x => x.Autarkie, a);
            p.Add(x => x.Bild, Bild);
            p.Add(x => x.UebersichtInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<i>ueb</i>")));
            p.Add(x => x.WaermegangInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<i>wg</i>")));
            p.Add(x => x.StromgangInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<i>sg</i>")));
            if (kapazitaet is not null)
                p.Add(x => x.KapazitaetGeaendert, EventCallback.Factory.Create<double>(this, kapazitaet));
        });

    /// <summary>Aus den vier Navigationsknoepfen wird ein innerer Reiter.</summary>
    [Fact]
    public void Der_Ergebnisreiter_traegt_vier_Blaetter()
    {
        var seite = ErgebnisZeichnen(Autarkie());
        Assert.Equal(4, seite.FindAll("button[role='tab']").Count);
        Assert.Equal("UEBERSICHT", seite.Instance.AktivesBlatt);
    }

    [Fact]
    public void Die_drei_Fremdinhalte_erscheinen_in_ihrem_Blatt()
    {
        var seite = ErgebnisZeichnen(Autarkie());
        Assert.Contains("<i>ueb</i>", seite.Markup);

        seite.Find("button[role='tab'][id='reiter-WAERMEGANG']").Click();
        Assert.Contains("<i>wg</i>", seite.Markup);
    }

    /// <summary>Kacheln und Balken der Autarkie-Analyse.</summary>
    [Fact]
    public void Die_Autarkie_zeigt_Kacheln_Balken_und_Monatsbild()
    {
        var seite = ErgebnisZeichnen(Autarkie());
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal(3, seite.FindAll(".epos-kennzahlkachel").Count);
        Assert.Equal(2, seite.FindAll("meter").Count);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.AutarkieMonate);
    }

    /// <summary>Ohne Waermebedarf steht „nicht benoetigt" statt einer Quote.</summary>
    [Fact]
    public void Ohne_Waermebedarf_steht_nicht_benoetigt()
    {
        var seite = ErgebnisZeichnen(Autarkie(stBekannt: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Contains("nicht benötigt", seite.Markup);
    }

    /// <summary>
    /// Befund W11-B32: Die Kapazitaet ist eine Was-waere-wenn-Groesse. Sie meldet
    /// sich an die Huelle (die rechnet neu) und traegt den Hinweis, dass sie
    /// nicht gespeichert wird.
    /// </summary>
    [Fact]
    public void Die_Kapazitaet_meldet_sich_und_nennt_sich_fluechtig()
    {
        double gemeldet = 0;
        var seite = ErgebnisZeichnen(Autarkie(), k => gemeldet = k);
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Contains("nicht gespeichert", seite.Markup);
        seite.Find("input[type='text']").Input("12");
        Assert.Equal(12.0, gemeldet);
    }

    /// <summary>Ohne PV bleiben Kachel, Balken und das Kapazitaetsfeld weg.</summary>
    [Fact]
    public void Ohne_Photovoltaik_bleibt_die_PV_Kachel_weg()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Single(seite.FindAll("meter"));
        Assert.Empty(seite.FindAll("input[type='text']"));
    }
}
