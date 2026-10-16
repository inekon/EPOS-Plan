using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using KiKern;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Ansicht „Fluss“ des Abschnitts „Ergebnisse — Energie“ (<see cref="UebersichtErgebnisFluss"/>, Konzept
/// Energieflussbild Abschnitt 4) mit der echten Hülle <see cref="UebersichtErgebnisGaben"/> und dem Flussbau, der
/// Abbildung und dem Renderer des Kerns — ohne Datenbank: Der Ladeweg liefert synthetische Ergebnisse (Wärmepumpe,
/// Heizkessel, Photovoltaik, Netzbezug).
/// </summary>
public class UebersichtErgebnisFlussTests : EposBunitContext
{
    public UebersichtErgebnisFlussTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private int _geladen;

    /// <summary>Synthetische Ergebnisse: Stamm (1) und Variante 2 simuliert, Variante 3 nicht.</summary>
    private VariantenDaten Daten(int id, bool stamm)
    {
        _geladen++;
        bool puffer = id == 2;
        var bedarf = new ErgebnisEnergiebedarfModel
        {
            Waermebedarf_Gesamt = 182.4, Waermelast_Max = 96, Strombedarf_Gesamt = 64.2, Strombedarf_Max = 31,
            Stromrestbedarf = puffer ? 70.9 : 75.6, Waermerestbedarf = 0
        };
        bedarf.Waermebedarf_Kanal[0] = 151.0;
        bedarf.Waermebedarf_Kanal[1] = 31.4;
        return new VariantenDaten
        {
            IdProjekt = id, IstStamm = stamm,
            EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>
            {
                new() { CarrierId = 1, Traeger = "Erdgas", MengeMWh = puffer ? 24.6 : 37.2, WaermeMengeMWh = puffer ? 24.6 : 37.2 },
                new() { CarrierId = 2, Traeger = "Strom", Netzstrom = true, MengeMWh = puffer ? 70.9 : 75.6, Einheit = "kWh" }
            },
            Ergebnis = new ErgebnisModel
            {
                Energiebedarf = bedarf,
                Waermepumpe = new ErgebnisWaermepumpeModel
                {
                    Waermebedarf = 182.4, Waermeproduktion_WP = puffer ? 160.3 : 148.9,
                    Stromverbrauch_WP = puffer ? 44.1 : 42.5, Waermebedarfsdeckung = puffer ? 87.9 : 81.6
                },
                Heizkessel = new ErgebnisHeizkesselModel
                {
                    Waermebedarf = 182.4, Waermeproduktion = puffer ? 22.1 : 33.5, Gasverbrauch = puffer ? 24.6 : 37.2
                },
                Photovoltaik = new ErgebnisPhotovoltaikModel { Strombedarf = 64.2, Stromproduktion = 24.0, Ueberschuss = puffer ? 6.4 : 8.1 }
            }
        };
    }

    private static VarianteZeile Zeile(int id, string bezeichner, bool simuliert, bool stamm = false) => new()
    {
        IdProjekt = id, IstStamm = stamm, Bezeichner = bezeichner, Projektname = "Musterhaus " + bezeichner,
        SimZeitpunkt = simuliert ? "12.10.26 23:05" : ""
    };

    private static UebersichtStand Stand() => new()
    {
        StammId = 1,
        Zeilen = new[]
        {
            Zeile(1, "(Stammprojekt)", true, stamm: true),
            Zeile(2, "mit Puffer", true),
            Zeile(3, "Variante 3", false)
        },
        MarkierteId = 1,
        GewaehlteVarianten = new[] { 2, 3 }
    };

    /// <summary>Der Abschnitt mit Ablage der Ansicht und der Versionswahl in eigenen Wörterbüchern (wie die Hülle).</summary>
    private IRenderedComponent<UebersichtErgebnisse> Zeige(Dictionary<string, string> ablage, UebersichtErgebnisGaben? huelle = null,
                                                          bool mitFluss = true)
    {
        huelle ??= new UebersichtErgebnisGaben(Daten, new FluechtigeEinstellungen());
        UebersichtErgebnisGaben h = huelle;
        return Render<UebersichtErgebnisse>(p =>
        {
            p.Add(x => x.Stand, Stand())
             .Add(x => x.Laden, h.Laden)
             .Add(x => x.AnsichtLesen, () => ablage.TryGetValue("ansicht", out string? a) ? a : UebersichtErgebnisse.ANSICHT_TABELLE)
             .Add(x => x.AnsichtGewechselt, a => ablage["ansicht"] = a)
             .Add(x => x.FlussTexte, UebersichtErgebnisGaben.FlussTexte());
            if (mitFluss)
                p.Add(x => x.FlussLaden, h.FlussLaden)
                 .Add(x => x.FlussVersionLesen, () => ablage.TryGetValue("version", out string? v) ? int.Parse(v) : null)
                 .Add(x => x.FlussVersionGewechselt, id => ablage["version"] = id.ToString());
        });
    }

    private static Dictionary<string, string> Flussablage() => new() { ["ansicht"] = UebersichtErgebnisse.ANSICHT_FLUSS };

    private static void Waehle(IRenderedComponent<UebersichtErgebnisse> cut, int id)
        => cut.Find(".epos-ergebnis-fluss-kopf select").Change(id.ToString());

    // ---- Fälle -----------------------------------------------------------

    [Fact]
    public void Dritte_Stellung_Fluss_zeigt_das_Bild_und_merkt_die_Wahl()
    {
        var ablage = new Dictionary<string, string>();
        var cut = Zeige(ablage);

        var knoepfe = cut.FindAll(".epos-ergebnis-ansicht button[role=radio]");
        Assert.Equal(new[] { "Tabelle", "Grafik", "Fluss" }, knoepfe.Select(k => k.TextContent).ToArray());
        Assert.Equal(new[] { "tabelle", "grafik", "fluss" }, knoepfe.Select(k => k.GetAttribute("data-ansicht")).ToArray());
        Assert.Empty(cut.FindAll(".epos-ergebnis-fluss"));

        cut.Find(".epos-ergebnis-ansicht button[data-ansicht=fluss]").Click();
        Assert.Equal(UebersichtErgebnisse.ANSICHT_FLUSS, ablage["ansicht"]);
        Assert.Equal("true", cut.Find(".epos-ergebnis-ansicht button[data-ansicht=fluss]").GetAttribute("aria-checked"));
        Assert.Empty(cut.FindAll("table.epos-ergebnisvergleich"));
        Assert.Empty(cut.FindAll(".epos-ergebnis-grafik"));

        // Zwei Modelle (breit und schmal) im Baum, je mit eigener Kennung; das Stilblatt wählt.
        var bilder = cut.FindComponents<DiagrammSvg>();
        Assert.Equal(2, bilder.Count);
        Assert.Equal(new[] { "ergebnis-fluss-1-breit", "ergebnis-fluss-1-schmal" }, bilder.Select(b => b.Instance.Kennung).ToArray());
        Assert.All(bilder, b => Assert.True(b.Instance.OhneZoom));
        Assert.All(bilder, b => Assert.False(b.Instance.LegendeSchaltbar));
        Assert.NotNull(cut.Find(".epos-ergebnis-fluss-mass--breit svg"));
        Assert.NotNull(cut.Find(".epos-ergebnis-fluss-mass--schmal svg"));

        // Gemerkt: ein neuer Abschnitt mit derselben Ablage steht wieder auf „Fluss“.
        var neu = Zeige(ablage);
        Assert.Equal(UebersichtErgebnisse.ANSICHT_FLUSS, neu.Instance.Ansicht);
        Assert.NotEmpty(neu.FindAll(".epos-ergebnis-fluss figure"));
    }

    [Fact]
    public void Pfeiltasten_laufen_ueber_drei_Stellungen_Pos1_und_Ende_an_die_Raender()
    {
        var ablage = new Dictionary<string, string>();
        var cut = Zeige(ablage);
        void Taste(string k) => cut.Find(".epos-ergebnis-ansicht").KeyDown(new KeyboardEventArgs { Key = k });

        Taste("ArrowRight");
        Assert.Equal("grafik", cut.Instance.Ansicht);
        Taste("ArrowRight");
        Assert.Equal("fluss", cut.Instance.Ansicht);
        Taste("ArrowRight");
        Assert.Equal("tabelle", cut.Instance.Ansicht);
        Taste("ArrowLeft");
        Assert.Equal("fluss", cut.Instance.Ansicht);
        Taste("Home");
        Assert.Equal("tabelle", cut.Instance.Ansicht);
        Taste("End");
        Assert.Equal("fluss", cut.Instance.Ansicht);
        Assert.Equal("fluss", ablage["ansicht"]);
    }

    [Fact]
    public void Alte_Werte_bleiben_gueltig_unbekannte_und_Fluss_ohne_Ladeweg_fallen_auf_die_Tabelle()
    {
        Assert.Equal("grafik", Zeige(new() { ["ansicht"] = "grafik" }).Instance.Ansicht);
        Assert.Equal("tabelle", Zeige(new() { ["ansicht"] = "tabelle" }).Instance.Ansicht);
        Assert.Equal("tabelle", Zeige(new() { ["ansicht"] = "sankey" }).Instance.Ansicht);

        // Kein Delegat, kein Knopf: ohne FlussLaden zwei Stellungen, ein gespeichertes „fluss“ zeigt die Tabelle.
        var ohne = Zeige(Flussablage(), mitFluss: false);
        Assert.Equal(new[] { "Tabelle", "Grafik" },
                     ohne.FindAll(".epos-ergebnis-ansicht button[role=radio]").Select(k => k.TextContent).ToArray());
        Assert.Equal("tabelle", ohne.Instance.Ansicht);
        Assert.NotEmpty(ohne.FindAll("table.epos-ergebnisvergleich"));
    }

    [Fact]
    public void Versionswahl_Stamm_vorgegeben_nicht_simulierte_Version_benannt_ohne_Bild()
    {
        var ablage = Flussablage();
        var cut = Zeige(ablage);

        var optionen = cut.FindAll(".epos-ergebnis-fluss-kopf select option").Where(o => o.GetAttribute("value") != "").ToList();
        Assert.Equal(new[] { "Stamm", "mit Puffer", "Variante 3 (nicht simuliert)" }, optionen.Select(o => o.TextContent.Trim()).ToArray());
        Assert.Equal(1, cut.Instance.Fluss!.Gewaehlt);
        Assert.Equal("1", cut.Find(".epos-ergebnis-fluss").GetAttribute("data-version"));

        Waehle(cut, 3);
        Assert.Equal("3", ablage["version"]);
        Assert.Empty(cut.FindAll(".epos-ergebnis-fluss figure"));
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BK_ERG_FLUSS_NICHT_SIMULIERT,
                     cut.Find(".epos-ergebnis-fluss-leer").TextContent);

        Waehle(cut, 2);
        Assert.Equal("2", ablage["version"]);
        Assert.Equal(new[] { "ergebnis-fluss-2-breit", "ergebnis-fluss-2-schmal" },
                     cut.FindComponents<DiagrammSvg>().Select(b => b.Instance.Kennung).ToArray());

        // Die Wahl gilt beim nächsten Zeichnen (Ablage der Hülle je Sitzung).
        var neu = Zeige(ablage);
        Assert.Equal(2, neu.Instance.Fluss!.Gewaehlt);
    }

    [Fact]
    public void Das_Modell_bleibt_dieselbe_Referenz_solange_sich_nichts_aendert()
    {
        var huelle = new UebersichtErgebnisGaben(Daten, new FluechtigeEinstellungen());
        var cut = Zeige(Flussablage(), huelle);
        ErgebnisFlussStand erst = cut.Instance.Fluss!.Stand!;
        int geladen = _geladen;

        cut.Render();
        Assert.Same(erst, cut.Instance.Fluss!.Stand);
        Assert.Same(erst, huelle.FlussLaden(1));
        // Die Kennzahlen kommen aus demselben Puffer wie die Tabelle.
        Assert.Equal(geladen, _geladen);
        Assert.Same(erst.Breit, cut.FindComponents<DiagrammSvg>()[0].Instance.Modell);
    }

    [Fact]
    public void Keine_sichtbare_Wertetabelle_aber_eine_verborgene_Textalternative()
    {
        var cut = Zeige(Flussablage());
        IElement wurzel = cut.Find(".epos-ergebnis-fluss");

        // SK-Q12: keine Tabelle, keine Zahl im Bild.
        Assert.Empty(wurzel.QuerySelectorAll("table"));
        var zahl = new Regex(@"\d+[,.]\d");
        Assert.DoesNotContain(wurzel.QuerySelectorAll("svg text"), t => zahl.IsMatch(t.TextContent));

        // Die Textalternative: verborgen, über aria-describedby am Bild.
        IElement bild = cut.Find("figure.epos-ergebnis-fluss-bild");
        string id = bild.GetAttribute("aria-describedby")!;
        Assert.False(string.IsNullOrEmpty(id));
        Assert.StartsWith("Energieflussbild der Version Stamm", bild.GetAttribute("aria-label"));
        IElement text = cut.Find("#" + id);
        Assert.Contains("epos-komponenten-sprachausgabe", text.ClassList);
        ErgebnisFlussStand stand = cut.Instance.Fluss!.Stand!;
        var punkte = text.QuerySelectorAll("li").Select(l => l.TextContent).ToList();
        Assert.Equal(stand.Fluesse.Count + stand.Knoten.Count, punkte.Count);
        Assert.Contains(punkte, p => Regex.IsMatch(p, @"^.+ → .+: \d+(,\d+)? MWh/a \(\d+(,\d)? %\) · (direkt|abgeleitet|bilanziert)$"));
        Assert.Contains(punkte, p => p.StartsWith("Wärme: Zufluss ") && p.Contains(" · Rest ") && p.EndsWith(" MWh/a"));
        // Je Band eine Zeile, auch der bilanzierten.
        Assert.Contains(punkte, p => p.Contains("Wärmepumpe") && p.Contains("→"));
    }

    [Fact]
    public void Hinweise_stehen_leise_unter_dem_Bild()
    {
        var cut = Zeige(Flussablage());
        var leise = cut.FindAll(".epos-ergebnis-fluss .epos-leisezeile").Select(p => p.TextContent).ToList();
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BK_ERG_FLUSS_ZEIGER, leise[0]);
        // Die Wärmepumpe bringt die Konvention des Umweltanteils mit.
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.BK_ERG_FLUSS_HINWEIS_UMWELT, leise);
        Assert.All(cut.FindAll(".epos-ergebnis-fluss-hinweis"), h => Assert.Contains("epos-leisezeile", h.ClassList));
        // Die Hinweise des Kerns (nicht geschlossene Knoten) stehen ebenfalls als Zeile.
        ErgebnisFlussStand stand = cut.Instance.Fluss!.Stand!;
        Assert.All(stand.Hinweise, h => Assert.Contains(h, leise));
        // Die Quelle der Werte bleibt unter dem Abschnitt.
        Assert.NotNull(cut.Find("p.epos-ergebnis-quelle"));
    }

    [Fact]
    public void Der_Assistent_liest_die_Ansicht_Fluss_mit_Flussliste()
    {
        var cut = Zeige(Flussablage());
        string text = cut.Instance.Text();
        Assert.StartsWith("Ansicht: Fluss (Energieflussbild der Version Stamm)\n", text);
        // Danach die Gruppen der Tabelle wie in allen Ansichten, dann der Block des Bildes.
        Assert.Contains("[bedarf] Bedarf", text);
        int block = text.IndexOf("[fluss] Energiefluss — Stamm", StringComparison.Ordinal);
        Assert.True(block > text.IndexOf("[bedarf] Bedarf", StringComparison.Ordinal));
        Assert.Matches(@"\n.+ → .+: [\d,.]+ MWh/a \([\d,.]+ %\) · (direkt|abgeleitet|bilanziert)", text.Substring(block));
        Assert.DoesNotContain("\r", text);

        Waehle(cut, 3);
        string ohne = cut.Instance.Text();
        Assert.StartsWith("Ansicht: Fluss (Energieflussbild der Version Variante 3)\n", ohne);
        Assert.EndsWith(WindowsFormsApplication1.MyResource.Resource.BK_ERG_FLUSS_NICHT_SIMULIERT, ohne);
    }

    [Fact]
    public void Die_Huelle_reicht_den_Fluss_im_Parametersatz()
    {
        var huelle = new UebersichtErgebnisGaben(Daten, new FluechtigeEinstellungen());
        IReadOnlyDictionary<string, object> gaben = huelle.Gaben();
        Assert.IsType<Func<int, ErgebnisFlussStand>>(gaben["FlussLaden"]);
        Assert.IsType<ErgebnisFlussTexte>(gaben["FlussTexte"]);
        Assert.Equal("Fluss", ((ErgebnisFlussTexte)gaben["FlussTexte"]).Ansicht);

        // Die Sitzungsablage der Version.
        var setzen = (Action<int>)gaben["FlussVersionGewechselt"];
        var lesen = (Func<int?>)gaben["FlussVersionLesen"];
        setzen(4711);
        Assert.Equal(4711, lesen());

        // Eine Version außerhalb des geladenen Standes liefert einen benannten Stand ohne Bild.
        ErgebnisFlussStand fremd = huelle.FlussLaden(99);
        Assert.True(fremd.NichtSimuliert);
        Assert.False(fremd.HatBild);
        setzen(1);
    }

    [Fact]
    public void Ohne_Versionen_zeichnet_die_Komponente_nichts()
    {
        var cut = Render<UebersichtErgebnisFluss>();
        Assert.Empty(cut.FindAll(".epos-ergebnis-fluss"));
        Assert.Null(cut.Instance.Stand);
    }
}
