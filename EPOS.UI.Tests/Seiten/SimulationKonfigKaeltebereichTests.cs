using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Tests.Dialoge;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Der Bereich „Kälte“ der Simulationskonfiguration</b> (Welle KB-B; Entwurf Kältebereich 3, Entscheide E117):
/// der Schalter „Kühlung rechnen“ im Kopf, die Kälteerzeuger in Rechenfolge als Kacheln, die Kältespeicher nur
/// hier, eingeklappt bei „aus“, der Kühlbetrieb der Wärmepumpe an der Kachel und die Konfiguration der
/// Kältemaschine bzw. der Gruppe „Kühlbetrieb“ als Überlagerung.
/// </summary>
public class SimulationKonfigKaeltebereichTests : EposBunitContext
{
    private const int ID_WP = 11203;
    private const int ID_WP_GERAET = 5501;
    private const int ID_KM = 18001;
    private const int ID_KS = 1071273;

    public SimulationKonfigKaeltebereichTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ------------------------------------------------------------------ Proben

    private bool _projektKuehlung = true;
    private bool _projektAntwort = true;
    private readonly List<bool> _projektGeschrieben = new();
    private readonly List<(int Geraet, bool An)> _wpGeschrieben = new();
    private string? _wpAntwort;
    private readonly List<KaeltemaschineAnlageDaten> _kmGeschrieben = new();
    private string? _kmAntwort;
    private readonly List<(int Anlage, WaermepumpeAnlageDaten Daten)> _wpKonfigGeschrieben = new();
    private bool _wpKuehlbetrieb;
    private string? _wpSperrgrund;
    private bool _wpInKaskade = true;
    private int _anlagenJeKopie = 1;
    private int _ladungen;

    private KaeltebereichDaten Bereich() => new KaeltebereichDaten
    {
        Kuehlbetrieb = _projektKuehlung,
        Folge = new[] { KaelteStufe.FreieKuehlung, KaelteStufe.Kaeltespeicher, KaelteStufe.Waermepumpe, KaelteStufe.Kaeltemaschine },
        Erzeuger = new List<KaelteerzeugerZeile>
        {
            new KaelteerzeugerZeile
            {
                Kachel = new ErzeugerKachelDaten
                {
                    Schluessel = "kaelte-wp-" + ID_WP, Rang = "1", Titel = "Wärmepumpe 1",
                    Chips = new List<ChipDaten> { new ChipDaten("Wärmepumpe im Kühlbetrieb") },
                    Zustand = _wpKuehlbetrieb ? Kachelzustand.Aufgenommen : Kachelzustand.Verfuegbar,
                    Umschaltbar = true, Editierbar = true
                },
                Art = KaelteStufe.Waermepumpe, Nummer = 1, IdAnlage = ID_WP, IdGeraet = ID_WP_GERAET,
                Bezeichner = "Wärmepumpe 1", Kuehlbetrieb = _wpKuehlbetrieb, Sperrgrund = _wpSperrgrund,
                InWaermekaskade = _wpInKaskade
            },
            new KaelteerzeugerZeile
            {
                Kachel = new ErzeugerKachelDaten
                {
                    Schluessel = "kaelte-km-" + ID_KM, Rang = "2", Titel = "Kältemaschine 1",
                    Chips = new List<ChipDaten> { new ChipDaten("Kältemaschine"), new ChipDaten("2 × 150 kW") },
                    Zustand = Kachelzustand.Aufgenommen, Editierbar = true
                },
                Art = KaelteStufe.Kaeltemaschine, Nummer = 2, IdAnlage = ID_KM, IdGeraet = 77,
                Bezeichner = "Kältemaschine 1", Anzahl = 2, Kuehlbetrieb = true, FreieKuehlung = true,
                Rueckkuehlart = "Trockenkühler", AnlagenJeKopie = _anlagenJeKopie
            }
        },
        Kaeltespeicher = new List<SpeicherKachelDaten>
        {
            new SpeicherKachelDaten { IdPuffer = ID_KS, Bezeichner = "Kältespeicher 1", Volumen = "2 m³" }
        },
        FreieKuehlung = new List<FreieKuehlungZeile> { new(KaelteStufe.Kaeltemaschine, ID_KM, "Kältemaschine 1", true) }
    };

    private SimulationKonfigDienste Dienste(bool mitKonfiguration = true) => new SimulationKonfigDienste
    {
        Laden = _ =>
        {
            _ladungen++;
            return new SimulationKonfigDaten
            {
                IdProjekt = 1017,
                Speicher = new List<SpeicherKachelDaten>
                {
                    new SpeicherKachelDaten { IdPuffer = 42, Bezeichner = "Puffer 1" }
                },
                Kaeltebereich = Bereich()
            };
        },
        SchemaLaden = _ => SchemaBild.Leer,
        PufferVerwaltungGaben = _ => new Dictionary<string, object> { ["IdProjekt"] = 1017 },
        KuehlbetriebWpSchreiben = (geraet, an) =>
        {
            _wpGeschrieben.Add((geraet, an));
            if (_wpAntwort is null) _wpKuehlbetrieb = an;
            return _wpAntwort;
        },
        KaeltemaschineKonfigurationLaden = mitKonfiguration
            ? id => id != ID_KM ? null : new KaeltemaschineKonfigurationGaben
            {
                Daten = new KaeltemaschineAnlageDaten
                {
                    AnlagenId = ID_KM, Bezeichner = "Kältemaschine 1", Anzahl = 2, KuehlCarrierId = 3,
                    Geraet = new KaeltemaschineGeraetwerte
                    {
                        Bezeichner = "KM-Typ", Nennkaelteleistung = 150, NennEer = 3.2, Rueckkuehlart = "Trockenkühler"
                    }
                },
                Stromtraeger = new List<(int, string)> { (3, "Strom"), (4, "Strom 2") },
                ProjektStromtraeger = 3
            }
            : null,
        KaeltemaschineKonfigurationSpeichern = mitKonfiguration
            ? d => { _kmGeschrieben.Add(d.Kopie()); return _kmAntwort; }
            : null
    };

    private SimulationParameterDienste Parameter(bool mitWpWeg = true)
    {
        var p = new SimulationParameterDienste
        {
            Laden = () => new ParameterDaten { Kuehlbetrieb = _projektKuehlung },
            KuehlbetriebSchreiben = an => { _projektGeschrieben.Add(an); return _projektAntwort; }
        };
        if (mitWpWeg)
        {
            p.WaermepumpeKonfigurationLaden = id => id == ID_WP
                ? new WaermepumpeAnlageDaten { IdWp = ID_WP_GERAET, Kuehlbetrieb = _wpKuehlbetrieb }
                : null;
            p.WaermepumpeKonfigurationSpeichern = (id, d) =>
            {
                _wpKonfigGeschrieben.Add((id, d));
                return new AnlagenkonfigErgebnis(true, "");
            };
            p.WaermepumpeKuehlGaben = () => new WaermepumpeKuehlGaben
            {
                Vorlaeufe = _ => new List<KuehlVorlaufEintrag> { new KuehlVorlaufEintrag(18, "") },
                Sperrgrund = _ => null,
                Stromtraeger = new List<(int, string)> { (3, "Strom") },
                ProjektStromtraeger = 3
            };
        }
        return p;
    }

    private IRenderedComponent<SimulationKonfigSeite> Seite(bool mitParameter = true, bool mitKonfiguration = true)
        => Render<SimulationKonfigSeite>(p =>
        {
            p.Add(x => x.Dienste, Dienste(mitKonfiguration)).Add(x => x.StartProjekt, 1017);
            if (mitParameter) p.Add(x => x.Parameter, Parameter());
        });

    private static IElement Bereichselement(IRenderedComponent<SimulationKonfigSeite> cut)
        => cut.Find("section.epos-simkonfig-kaelte");

    private static IElement Kachel(IRenderedComponent<SimulationKonfigSeite> cut, int idAnlage)
        => cut.Find($"div.epos-simkonfig-kaelte-element[data-anlage='{idAnlage}'] div.epos-erzeugerkachel");

    // ------------------------------------------------------------------ Aufbau

    [Fact]
    public void Der_Bereich_steht_unter_dem_Waermeteil_mit_Kopf_und_Schalter()
    {
        var cut = Seite();

        IElement bereich = Bereichselement(cut);
        Assert.Equal(Resource.SIMKONF_GRP_KAELTE, bereich.QuerySelector(".epos-gruppenkopf-titel")!.TextContent);
        Assert.NotNull(bereich.QuerySelector("section.epos-simkonfig-kuehlung input[type=checkbox]"));
        Assert.Contains(Resource.SIMKONF_HRL_KUEHLBETRIEB, bereich.TextContent);

        // Unter den Spalten im selben fieldset - und nicht mehr unter „Weitere Einstellungen“.
        Assert.NotNull(cut.Find("fieldset.epos-simkonfig-bereich").QuerySelector("section.epos-simkonfig-kaelte"));
        Assert.Null(cut.Find("fieldset.epos-simkonfig-einstellungen").QuerySelector(".epos-simkonfig-kuehlung"));
        List<IElement> folge = cut.FindAll("section.epos-simkonfig-erzeuger, section.epos-simkonfig-speicher, section.epos-simkonfig-kaelte").ToList();
        Assert.Equal(new[] { "epos-simkonfig-erzeuger", "epos-simkonfig-speicher", "epos-simkonfig-kaelte" },
                     folge.Select(e => e.ClassList.First()).ToArray());
    }

    [Fact]
    public void Die_Kaelteerzeuger_stehen_in_Rechenfolge_mit_Folgezeile_und_freier_Kuehlung()
    {
        var cut = Seite();

        List<IElement> kacheln = cut.FindAll("div.epos-simkonfig-kaelte-erzeuger div.epos-erzeugerkachel").ToList();
        Assert.Equal(new[] { "Wärmepumpe 1", "Kältemaschine 1" },
                     kacheln.Select(k => k.QuerySelector(".epos-erzeugerkachel-titel")!.TextContent.Trim()).ToArray());
        Assert.Equal(new[] { "1", "2" },
                     kacheln.Select(k => k.QuerySelector(".epos-erzeugerkachel-rang")!.TextContent.Trim()).ToArray());

        string text = cut.Find("div.epos-simkonfig-kaelte-erzeuger").TextContent;
        Assert.Contains(string.Format(Resource.SIMKONF_KAELTE_FOLGE, string.Join(", ", Resource.SIMKONF_KAELTE_STUFE_FREI,
            Resource.SIMKONF_KAELTE_STUFE_SPEICHER, Resource.SIMKONF_KAELTE_STUFE_WP, Resource.SIMKONF_KAELTE_STUFE_KM)), text);
        Assert.Contains(string.Format(Resource.SIMKONF_KAELTE_FREI, string.Format(Resource.SIMKONF_KAELTE_FREI_VORN, "Kältemaschine 1")),
                        cut.Find("p.epos-simkonfig-kaelte-frei").TextContent);
        // Ohne Schreibweg der Folge (KaelteVerschieben) keine Pfeile an den Kacheln.
        Assert.DoesNotContain("▲", cut.Find("div.epos-simkonfig-kaelte-erzeuger").TextContent);
    }

    [Fact]
    public void Kaeltespeicher_stehen_nur_im_Kaeltebereich()
    {
        var cut = Seite();

        Assert.Contains("Kältespeicher 1", cut.Find("div.epos-simkonfig-kaelte-speicher").TextContent);
        Assert.DoesNotContain("Kältespeicher 1", cut.Find("section.epos-simkonfig-speicher").TextContent);
        Assert.Contains("Puffer 1", cut.Find("section.epos-simkonfig-speicher").TextContent);
    }

    [Fact]
    public void Bei_Schalter_aus_ist_der_Bereich_eingeklappt_und_nennt_was_nicht_wirkt()
    {
        _projektKuehlung = false;
        var cut = Seite();

        IElement bereich = Bereichselement(cut);
        Assert.Empty(bereich.QuerySelectorAll("div.epos-erzeugerkachel"));
        Assert.Empty(bereich.QuerySelectorAll("div.epos-simkonfig-kaelte-speicher"));
        Assert.Equal(string.Format(Resource.SIMKONF_KAELTE_AUS, 1, 1, 1),
                     bereich.QuerySelector("p.epos-simkonfig-kaelte-aus")!.TextContent.Trim());
        Assert.False(cut.Instance.KuehlungAn);
    }

    [Fact]
    public void Der_Schalter_schreibt_den_Projektweg_und_klappt_den_Bereich_auf()
    {
        _projektKuehlung = false;
        var cut = Seite();

        cut.Find("section.epos-simkonfig-kuehlung input[type=checkbox]").Change(true);

        Assert.Equal(new[] { true }, _projektGeschrieben);
        Assert.True(cut.Instance.KuehlungAn);
        Assert.Empty(cut.FindAll("p.epos-simkonfig-kaelte-aus"));
        Assert.Equal(2, cut.FindAll("div.epos-simkonfig-kaelte-erzeuger div.epos-erzeugerkachel").Count);
        Assert.Empty(_wpGeschrieben);
    }

    [Fact]
    public void Ein_gescheitertes_Schreiben_des_Schalters_laesst_den_Bereich_eingeklappt()
    {
        _projektKuehlung = false;
        _projektAntwort = false;
        var cut = Seite();

        cut.Find("section.epos-simkonfig-kuehlung input[type=checkbox]").Change(true);

        Assert.Equal(new[] { true }, _projektGeschrieben);
        Assert.False(cut.Find("section.epos-simkonfig-kuehlung input[type=checkbox]").HasAttribute("checked"));
        Assert.NotEmpty(cut.FindAll("p.epos-simkonfig-kaelte-aus"));
        Assert.Contains(Resource.SIMKONF_MSG_KUEHLBETRIEB_FEHLER, cut.Markup);
    }

    [Fact]
    public void Ohne_Schalter_und_ohne_Kaelteanlagen_steht_kein_Bereich()
    {
        SimulationKonfigDienste dienste = Dienste();
        dienste.Laden = _ => new SimulationKonfigDaten { IdProjekt = 1017 };
        var cut = Render<SimulationKonfigSeite>(p => p.Add(x => x.Dienste, dienste).Add(x => x.StartProjekt, 1017));

        Assert.Empty(cut.FindAll("section.epos-simkonfig-kaelte"));
    }

    [Fact]
    public void Ohne_Schalterweg_zeigt_der_Bereich_den_gelesenen_Stand_ohne_Schalter()
    {
        var cut = Seite(mitParameter: false);

        Assert.Empty(cut.FindAll("section.epos-simkonfig-kuehlung"));
        Assert.Equal(2, cut.FindAll("div.epos-simkonfig-kaelte-erzeuger div.epos-erzeugerkachel").Count);
    }

    // ------------------------------------------------------------------ Wärmepumpe

    [Fact]
    public void Aufnehmen_schaltet_den_Kuehlbetrieb_der_Waermepumpe_an_und_Entfernen_aus()
    {
        var cut = Seite();
        int vorher = _ladungen;

        Kachel(cut, ID_WP).QuerySelector("button.epos-erzeugerkachel-aufnehmen")!.Click();

        Assert.Equal(new[] { (ID_WP_GERAET, true) }, _wpGeschrieben);
        Assert.True(_ladungen > vorher);
        IElement nachher = Kachel(cut, ID_WP);
        Assert.DoesNotContain("epos-erzeugerkachel--verfuegbar", nachher.ClassName ?? "");

        nachher.QuerySelector($"button[title='{Resource.SIMKONF_KAELTE_TIP_AUS}']")!.Click();
        Assert.Equal(new[] { (ID_WP_GERAET, true), (ID_WP_GERAET, false) }, _wpGeschrieben);
    }

    [Fact]
    public void Ein_Sperrgrund_steht_am_Element_und_Aufnehmen_schreibt_nicht()
    {
        _wpSperrgrund = "Keine Kühlkennlinie im Projekt.";
        var cut = Seite();

        Assert.Contains(_wpSperrgrund, Kachel(cut, ID_WP).QuerySelector("p.epos-simkonfig-kaelte-sperrgrund")!.TextContent);
        Kachel(cut, ID_WP).QuerySelector("button.epos-erzeugerkachel-aufnehmen")!.Click();

        Assert.Empty(_wpGeschrieben);
        Assert.Contains(_wpSperrgrund, cut.Find("div.epos-simkonfig-fuss").TextContent);
    }

    [Fact]
    public void Eine_Ablehnung_des_Kernwegs_steht_im_Banner()
    {
        _wpAntwort = "Quellspeicher gesetzt.";
        var cut = Seite();

        Kachel(cut, ID_WP).QuerySelector("button.epos-erzeugerkachel-aufnehmen")!.Click();

        Assert.Single(_wpGeschrieben);
        Assert.Contains(string.Format(Resource.SIMKONF_KAELTE_WP_FEHLER, _wpAntwort), cut.Find("div.epos-simkonfig-fuss").TextContent);
    }

    [Fact]
    public void Eine_Waermepumpe_ausserhalb_der_Waermekaskade_sagt_es_am_Element()
    {
        _wpInKaskade = false;
        var cut = Seite();

        Assert.Contains(Resource.SIMKONF_KAELTE_NICHT_KASKADE, Kachel(cut, ID_WP).TextContent);
        Assert.DoesNotContain(Resource.SIMKONF_KAELTE_NICHT_KASKADE, Kachel(cut, ID_KM).TextContent);
    }

    [Fact]
    public void Konfiguration_der_Waermepumpe_oeffnet_die_Gruppe_Kuehlbetrieb_und_OK_schreibt()
    {
        var cut = Seite();

        Kachel(cut, ID_WP).QuerySelector("button.epos-simkonfig-kaelte-konfigknopf")!.Click();

        Assert.True(cut.Instance.WpKuehlKonfigurationOffen);
        IElement ueb = cut.Find("div.epos-simkonfig-kaelte-wp");
        Assert.Contains(new WaermepumpeKonfigurationTexte().GruppeKuehlbetrieb, ueb.TextContent);
        Assert.Single(cut.FindComponents<WaermepumpeKuehlbetriebGruppe>());

        ueb.QuerySelector("input[type=checkbox]")!.Change(true);
        cut.Find("div.epos-simkonfig-kaelte-wp button.epos-knopf--primaer").Click();

        (int anlage, WaermepumpeAnlageDaten daten) = Assert.Single(_wpKonfigGeschrieben);
        Assert.Equal(ID_WP, anlage);
        Assert.True(daten.Kuehlbetrieb);
        Assert.False(cut.Instance.WpKuehlKonfigurationOffen);
    }

    // ------------------------------------------------------------------ Kältemaschine

    [Fact]
    public void Konfiguration_der_Kaeltemaschine_oeffnet_die_Komponente_und_OK_schreibt()
    {
        var cut = Seite();

        Kachel(cut, ID_KM).QuerySelector("button.epos-simkonfig-kaelte-konfigknopf")!.Click();

        Assert.True(cut.Instance.KaeltemaschineKonfigurationOffen);
        Assert.Single(cut.FindComponents<KaeltemaschineKonfiguration>());
        IElement ueb = cut.Find("div.epos-simkonfig-kaelte-km");
        Assert.Contains("KM-Typ", ueb.TextContent);

        ueb.QuerySelector("input[type=text]")!.Input("Kältemaschine Nord");
        cut.Find("div.epos-simkonfig-kaelte-km button.epos-knopf--primaer").Click();

        KaeltemaschineAnlageDaten d = Assert.Single(_kmGeschrieben);
        Assert.Equal("Kältemaschine Nord", d.Bezeichner);
        Assert.Equal(ID_KM, d.AnlagenId);
        Assert.False(cut.Instance.KaeltemaschineKonfigurationOffen);
    }

    [Fact]
    public void Eine_Ablehnung_haelt_die_Konfiguration_offen_und_Abbrechen_schreibt_nicht()
    {
        _kmAntwort = "Anzahl muss mindestens 1 sein.";
        var cut = Seite();
        Kachel(cut, ID_KM).QuerySelector("button.epos-simkonfig-kaelte-konfigknopf")!.Click();

        cut.Find("div.epos-simkonfig-kaelte-km button.epos-knopf--primaer").Click();
        Assert.True(cut.Instance.KaeltemaschineKonfigurationOffen);
        Assert.Contains(_kmAntwort, cut.Find("div.epos-simkonfig-kaelte-km").TextContent);

        int geschrieben = _kmGeschrieben.Count;
        cut.FindAll("div.epos-simkonfig-kaelte-km .epos-leiste button").First(b => b.TextContent.Trim() == Resource.ALLG_BTN_ABBRECHEN).Click();
        Assert.False(cut.Instance.KaeltemaschineKonfigurationOffen);
        Assert.Equal(geschrieben, _kmGeschrieben.Count);
    }

    [Fact]
    public void Eine_geteilte_Projektkopie_nennt_wie_viele_Anlagen_sie_traegt()
    {
        _anlagenJeKopie = 3;
        var cut = Seite();

        string hinweis = string.Format(Resource.SIMKONF_KAELTE_GILT_FUER, 3);
        Assert.Contains(hinweis, Kachel(cut, ID_KM).TextContent);
        Kachel(cut, ID_KM).QuerySelector("button.epos-simkonfig-kaelte-konfigknopf")!.Click();
        Assert.Contains(hinweis, cut.Find("div.epos-simkonfig-kaelte-km").TextContent);
    }

    [Fact]
    public void Ohne_Konfigurationsweg_steht_an_der_Kaeltemaschine_kein_Knopf()
    {
        var cut = Seite(mitKonfiguration: false);

        Assert.Null(Kachel(cut, ID_KM).QuerySelector("button.epos-simkonfig-kaelte-konfigknopf"));
        Assert.NotNull(Kachel(cut, ID_WP).QuerySelector("button.epos-simkonfig-kaelte-konfigknopf"));
    }

    // ------------------------------------------------------------------ Hilfe-Assistent

    [Fact]
    public async Task Der_Assistent_liest_den_Bereich_und_schaltet_den_Kuehlbetrieb_ueber_denselben_Weg()
    {
        var cut = Seite();
        var sicht = new SimulationKiSicht(() => cut.Instance.Stand, () => null, () => null, () => null, () => "", () => "",
                                          konfigseite: () => cut.Instance);

        Assert.StartsWith("1. Wärmepumpe 1", sicht.Kaelteerzeuger);
        Assert.Contains("2. Kältemaschine 1 (Kältemaschine, 2 × 150 kW)", sicht.Kaelteerzeuger);
        Assert.Equal("Kältespeicher 1 · 2 m³", sicht.Kaeltespeicher);
        Assert.Equal("", sicht.KuehlbetriebWaermepumpen);

        await cut.InvokeAsync(() => sicht.KuehlbetriebWaermepumpen = "wärmepumpe 1");
        Assert.Equal(new[] { (ID_WP_GERAET, true) }, _wpGeschrieben);
        Assert.Equal("Wärmepumpe 1", sicht.KuehlbetriebWaermepumpen);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.InvokeAsync(() => sicht.KuehlbetriebWaermepumpen = "Kältemaschine 1"));
        Assert.Equal(string.Format(Resource.KI_DLG_SIM_KUEHL_WP_UNBEKANNT, "Kältemaschine 1"), ex.Message);
        Assert.Single(_wpGeschrieben);
    }
}
