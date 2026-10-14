using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Tests.Dialoge;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Pfeile der Kältefolge im Bereich „Kälte“</b> (Welle KB-D2; Entscheid E117 F1, Protokoll KB-D): ▲/▼ je
/// Erzeugerkachel auf <see cref="SimulationKonfigDienste.KaelteVerschieben"/>, der Knopf „Vorgabefolge“ auf
/// <see cref="SimulationKonfigDienste.KaelteVorgabefolge"/>, die Herleitungszeile aus
/// <see cref="KaeltebereichDaten.FolgeHinweis"/>; geschrieben wird sofort, danach lädt die Seite neu, eine Ablehnung
/// steht im Banner.
/// </summary>
public class SimulationKonfigKaeltefolgeTests : EposBunitContext
{
    private const int ID_WP = 11203;
    private const int ID_KM1 = 18001;
    private const int ID_KM2 = 18002;

    public SimulationKonfigKaeltefolgeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ------------------------------------------------------------------ Proben

    /// <summary>Die Folge, wie der Kern sie führt (Anlagen-IDs in Rechenfolge).</summary>
    private List<int> _folge = new() { ID_WP, ID_KM1, ID_KM2 };
    private bool _gepflegt;
    private bool _gesperrt;
    private string? _antwort;
    private readonly List<(int Anlage, int Richtung)> _verschoben = new();
    private int _vorgaben;
    private int _ladungen;

    private static string Name(int id) => id switch
    {
        ID_WP => "Wärmepumpe 1",
        ID_KM1 => "Kältemaschine Nord",
        _ => "Kältemaschine Süd"
    };

    private KaeltebereichDaten Bereich()
    {
        var erzeuger = new List<KaelteerzeugerZeile>();
        for (int i = 0; i < _folge.Count; i++)
        {
            int id = _folge[i];
            erzeuger.Add(new KaelteerzeugerZeile
            {
                Kachel = new ErzeugerKachelDaten
                {
                    Schluessel = "kaelte-" + id, Rang = (i + 1).ToString(), Titel = Name(id),
                    Zustand = Kachelzustand.Aufgenommen
                },
                Art = id == ID_WP ? KaelteStufe.Waermepumpe : KaelteStufe.Kaeltemaschine,
                Nummer = i + 1, IdAnlage = id, Bezeichner = Name(id), Kuehlbetrieb = true,
                KaelteRang = _gepflegt ? i + 1 : null,
                NachVornMoeglich = i > 0, NachHintenMoeglich = i < _folge.Count - 1
            });
        }
        return new KaeltebereichDaten
        {
            Kuehlbetrieb = true,
            Folge = new[] { KaelteStufe.FreieKuehlung, KaelteStufe.Kaeltespeicher, KaelteStufe.Waermepumpe, KaelteStufe.Kaeltemaschine },
            Erzeuger = erzeuger,
            FolgeGepflegt = _gepflegt,
            FolgeHinweis = _gepflegt ? Resource.KAELTEFOLGE_HINWEIS_GEPFLEGT : Resource.KAELTEFOLGE_HINWEIS_VORGABE
        };
    }

    private SimulationKonfigDienste Dienste(bool mitWeg = true) => new SimulationKonfigDienste
    {
        Laden = _ =>
        {
            _ladungen++;
            return new SimulationKonfigDaten
            {
                IdProjekt = 1017, Gesperrt = _gesperrt, Sperrgrund = _gesperrt ? "Lesemodus" : "",
                Kaeltebereich = Bereich()
            };
        },
        SchemaLaden = _ => SchemaBild.Leer,
        KaelteVerschieben = mitWeg
            ? (id, richtung) =>
            {
                _verschoben.Add((id, richtung));
                if (_antwort is not null) return _antwort;
                int i = _folge.IndexOf(id);
                (_folge[i], _folge[i + richtung]) = (_folge[i + richtung], _folge[i]);
                _gepflegt = true;
                return null;
            }
            : null,
        KaelteVorgabefolge = mitWeg
            ? () =>
            {
                _vorgaben++;
                if (_antwort is not null) return _antwort;
                _folge = new List<int> { ID_WP, ID_KM1, ID_KM2 };
                _gepflegt = false;
                return null;
            }
            : null
    };

    private IRenderedComponent<SimulationKonfigSeite> Seite(bool mitWeg = true)
        => Render<SimulationKonfigSeite>(p => p.Add(x => x.Dienste, Dienste(mitWeg)).Add(x => x.StartProjekt, 1017));

    private static IElement Kachel(IRenderedComponent<SimulationKonfigSeite> cut, int idAnlage)
        => cut.Find($"div.epos-simkonfig-kaelte-element[data-anlage='{idAnlage}'] div.epos-erzeugerkachel");

    private static IElement? Pfeil(IRenderedComponent<SimulationKonfigSeite> cut, int idAnlage, string zeichen)
        => Kachel(cut, idAnlage).QuerySelectorAll("button.epos-erzeugerkachel-glyphe")
                                .FirstOrDefault(b => b.TextContent.Trim() == zeichen);

    private static string[] Titel(IRenderedComponent<SimulationKonfigSeite> cut)
        => cut.FindAll("div.epos-simkonfig-kaelte-erzeuger .epos-erzeugerkachel-titel")
              .Select(t => t.TextContent.Trim()).ToArray();

    private static IElement? Vorgabeknopf(IRenderedComponent<SimulationKonfigSeite> cut)
        => cut.FindAll("button.epos-simkonfig-kaelte-vorgabe").FirstOrDefault();

    // ------------------------------------------------------------------ Pfeile

    [Fact]
    public void Jede_Kachel_traegt_die_Pfeile_am_Rand_ausgegraut()
    {
        var cut = Seite();

        IElement vornErster = Pfeil(cut, ID_WP, "▲")!, hintenErster = Pfeil(cut, ID_WP, "▼")!;
        Assert.True(vornErster.HasAttribute("disabled"));
        Assert.False(hintenErster.HasAttribute("disabled"));
        Assert.Equal(Resource.SIMKONF_KAELTE_TIP_VOR, vornErster.GetAttribute("title"));
        Assert.Equal(Resource.SIMKONF_KAELTE_TIP_ZURUECK, hintenErster.GetAttribute("title"));

        Assert.False(Pfeil(cut, ID_KM1, "▲")!.HasAttribute("disabled"));
        Assert.False(Pfeil(cut, ID_KM1, "▼")!.HasAttribute("disabled"));
        Assert.False(Pfeil(cut, ID_KM2, "▲")!.HasAttribute("disabled"));
        Assert.True(Pfeil(cut, ID_KM2, "▼")!.HasAttribute("disabled"));
    }

    [Fact]
    public void Ohne_Delegat_stehen_weder_Pfeile_noch_Knopf()
    {
        var cut = Seite(mitWeg: false);

        Assert.Null(Pfeil(cut, ID_WP, "▲"));
        Assert.Null(Pfeil(cut, ID_KM1, "▼"));
        Assert.Null(Vorgabeknopf(cut));
        // Die Herleitungszeile steht trotzdem - sie liest nur.
        Assert.Contains(Resource.KAELTEFOLGE_HINWEIS_VORGABE, cut.Find("div.epos-simkonfig-kaelte-folge").TextContent);
    }

    [Fact]
    public void Ein_einzelner_Erzeuger_hat_keine_Pfeile_und_keinen_Knopf()
    {
        _folge = new List<int> { ID_KM1 };
        var cut = Seite();

        Assert.Null(Pfeil(cut, ID_KM1, "▲"));
        Assert.Null(Pfeil(cut, ID_KM1, "▼"));
        Assert.Null(Vorgabeknopf(cut));
    }

    [Fact]
    public void Ein_Pfeil_schreibt_sofort_und_laedt_neu()
    {
        var cut = Seite();
        int vorher = _ladungen;

        Pfeil(cut, ID_KM2, "▲")!.Click();

        Assert.Equal(new[] { (ID_KM2, -1) }, _verschoben);
        Assert.True(_ladungen > vorher);
        Assert.Equal(new[] { "Wärmepumpe 1", "Kältemaschine Süd", "Kältemaschine Nord" }, Titel(cut));
        Assert.Contains(Resource.KAELTEFOLGE_HINWEIS_GEPFLEGT, cut.Find("div.epos-simkonfig-kaelte-folge").TextContent);

        Pfeil(cut, ID_WP, "▼")!.Click();
        Assert.Equal((ID_WP, +1), _verschoben[1]);
        Assert.Equal(new[] { "Kältemaschine Süd", "Wärmepumpe 1", "Kältemaschine Nord" }, Titel(cut));
    }

    [Fact]
    public void Eine_Ablehnung_steht_im_Banner_und_die_Folge_bleibt()
    {
        _antwort = "Der Kälteerzeuger steht bereits am Rand der Folge.";
        var cut = Seite();

        Pfeil(cut, ID_KM1, "▼")!.Click();

        Assert.Single(_verschoben);
        Assert.Contains(_antwort, cut.Markup);
        Assert.Equal(new[] { "Wärmepumpe 1", "Kältemaschine Nord", "Kältemaschine Süd" }, Titel(cut));
    }

    // ------------------------------------------------------------------ Vorgabefolge

    [Fact]
    public void Der_Knopf_Vorgabefolge_ist_bei_Vorgabe_weich_gesperrt_und_meldet_den_Grund()
    {
        var cut = Seite();

        IElement knopf = Vorgabeknopf(cut)!;
        Assert.Equal(Resource.SIMKONF_KAELTE_BTN_VORGABE, knopf.TextContent.Trim());
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal(Resource.SIMKONF_KAELTE_VORGABE_GILT, knopf.GetAttribute("title"));

        knopf.Click();

        Assert.Equal(0, _vorgaben);
        Assert.Contains(Resource.SIMKONF_KAELTE_VORGABE_GILT, cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_Knopf_Vorgabefolge_stellt_die_Vorgabe_her()
    {
        _folge = new List<int> { ID_KM2, ID_WP, ID_KM1 };
        _gepflegt = true;
        var cut = Seite();

        IElement knopf = Vorgabeknopf(cut)!;
        Assert.False(knopf.HasAttribute("aria-disabled"));
        Assert.Equal(Resource.SIMKONF_KAELTE_TIP_VORGABE, knopf.GetAttribute("title"));
        Assert.Contains(Resource.KAELTEFOLGE_HINWEIS_GEPFLEGT, cut.Find("div.epos-simkonfig-kaelte-folge").TextContent);

        knopf.Click();

        Assert.Equal(1, _vorgaben);
        Assert.Equal(new[] { "Wärmepumpe 1", "Kältemaschine Nord", "Kältemaschine Süd" }, Titel(cut));
        Assert.Contains(Resource.KAELTEFOLGE_HINWEIS_VORGABE, cut.Find("div.epos-simkonfig-kaelte-folge").TextContent);
        Assert.Equal("true", Vorgabeknopf(cut)!.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Eine_Ablehnung_der_Vorgabefolge_steht_im_Banner()
    {
        _gepflegt = true;
        _antwort = "Die Folge der Kälteerzeuger ließ sich nicht speichern: gesperrt";
        var cut = Seite();

        Vorgabeknopf(cut)!.Click();

        Assert.Equal(1, _vorgaben);
        Assert.Contains(_antwort, cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Im_Lesemodus_schreiben_weder_Pfeile_noch_Knopf()
    {
        _gepflegt = true;
        _gesperrt = true;
        var cut = Seite();

        Assert.True(Pfeil(cut, ID_KM1, "▲")!.HasAttribute("disabled"));
        Assert.True(Pfeil(cut, ID_KM1, "▼")!.HasAttribute("disabled"));
        Assert.True(Vorgabeknopf(cut)!.HasAttribute("disabled"));
        Assert.Empty(_verschoben);
    }
}
