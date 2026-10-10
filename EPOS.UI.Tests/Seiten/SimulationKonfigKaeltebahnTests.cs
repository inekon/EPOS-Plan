using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Auftrag KS-2: der Doppelklick auf die KÄLTEBAHN des Anlagenschemas in der
/// Simulationskonfiguration. Die Kältemaschine und ihre Rückkühlung öffnen den Anlagendialog
/// der Kältemaschinen über <c>Dienste.Navigation</c> — denselben Weg wie die Kachel „Kühlung und
/// Kälteanlagen“, mit derselben benannten Ablehnung im Banner; die Wärmepumpe im Kühlbetrieb
/// öffnet den Senkendialog wie ihr Kasten in der Wärmebahn, ihre Quelle die Quellenwahl; der
/// Kältekreis hat wie der Heizkreis keinen Editor und trägt keinen Doppelklick.
/// Der Fall tauscht <c>Dienste.Navigation</c> und steht deshalb in der seriellen Sammlung.
/// </summary>
[Collection("KiDialogweg")]
public class SimulationKonfigKaeltebahnTests : EposBunitContext
{
    private const int ID_WP = 11203;
    private const int ID_KM = 18001;

    private readonly INavigation _vorher;

    public SimulationKonfigKaeltebahnTests()
    {
        _vorher = WindowsFormsApplication1.Dienste.Navigation;
        Navigation = new TestNavigation();
        WindowsFormsApplication1.Dienste.Navigation = Navigation;

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Mitschrift der Maskenaufrufe.</summary>
    private TestNavigation Navigation { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing) WindowsFormsApplication1.Dienste.Navigation = _vorher;
        base.Dispose(disposing);
    }

    private static readonly string[] Leer = new string[0];

    private static SchemaKnoten Kasten(string schluessel, SchemaKnotenart art, int y, string titel,
                                       string hinweis, bool kaelte, bool wp)
        => new SchemaKnoten(schluessel, art, art == SchemaKnotenart.Quelle ? 18 : art == SchemaKnotenart.Abnehmer ? 740 : 224,
                            y, 150, 35, "", titel, titel, Leer, Leer, hinweis, false, "", false, wp)
        { Kaelte = kaelte };

    /// <summary>Eine Wärmepumpe (oben und im Kühlbetrieb unten) und eine Kältemaschine mit Trockenkühler.</summary>
    private static SchemaBild Bild() => new SchemaBild(
        Knoten: new[]
        {
            Kasten("QUELLE_" + ID_WP, SchemaKnotenart.Quelle, 44, "Erdreich", "", false, true),
            Kasten("ERZEUGER_" + ID_WP, SchemaKnotenart.Erzeuger, 44, "Wärmepumpe 1", "", false, true),
            Kasten("ABNEHMER_HEIZKREIS", SchemaKnotenart.Abnehmer, 44, "Heizkreis", "", false, false),
            Kasten("KQUELLE_" + ID_WP, SchemaKnotenart.Quelle, 166, "Erdreich (Abwärme)", "Abwärme an: Erdreich", true, true),
            Kasten("KERZEUGER_" + ID_WP, SchemaKnotenart.Erzeuger, 166, "Wärmepumpe 1 (Kühlen)", "Kühlbetrieb", true, true),
            Kasten("KQUELLE_" + ID_KM, SchemaKnotenart.Quelle, 220, "Trockenkühler", "Rückkühlung: Trockenkühler", true, false),
            Kasten("KERZEUGER_" + ID_KM, SchemaKnotenart.Erzeuger, 220, "Kältemaschine 1", "Kältemaschine", true, false),
            Kasten("ABNEHMER_KAELTEKREIS", SchemaKnotenart.Abnehmer, 190, "Kältekreis", "", true, false)
        },
        Kanten: new SchemaKante[0], Band: new SchemaBandglied[0], Legende: new SchemaLegendeeintrag[0],
        Spaltenkoepfe: new[] { "Wärmequelle", "Erzeuger", "Speicher", "Abnehmer" },
        SpaltenX: new[] { 18, 224, 494, 740 },
        SpaltenBreite: new[] { 150, 214, 190, 132 },
        Breite: 890, Hoehe: 400, Rand: 18, KopfHoehe: 26,
        BandOben: 300, LegendeOben: 340,
        LinienBreite: 2, LinienBreiteHervor: 3, HatKaskade: false, IstLeer: false)
    {
        KaelteOben = 120,
        KaelteTitel = "Kälte"
    };

    private static ErzeugerZeile Waermepumpe(bool bauartGebunden) => new ErzeugerZeile
    {
        DbWert = "Wärmepumpe",
        IdAnlage = ID_WP,
        IdType = 1,
        Bezeichner = "Wärmepumpe 1",
        IstWaermepumpe = true,
        QuellenwahlMoeglich = true,
        BauartGebunden = bauartGebunden,
        Prioritaet = 1,
        Kachel = new ErzeugerKachelDaten
        {
            Schluessel = "Wärmepumpe", Rang = "1", Titel = "Wärmepumpe 1", Editierbar = true
        }
    };

    private SimulationKonfigDienste Dienste(bool bauartGebunden = false) => new SimulationKonfigDienste
    {
        Laden = _ => new SimulationKonfigDaten
        {
            IdProjekt = 1017,
            Gruppen = new System.Collections.Generic.List<KachelGruppe>
            {
                new KachelGruppe
                {
                    Titel = "Wärmeerzeuger",
                    Zeilen = new System.Collections.Generic.List<ErzeugerZeile> { Waermepumpe(bauartGebunden) }
                }
            }
        },
        SchemaLaden = _ => Bild(),
        WaermesenkeGaben = _ => new System.Collections.Generic.Dictionary<string, object>
        {
            ["Daten"] = new EPOS.UI.Dialoge.Simulation.WaermesenkeDaten()
        },
        Quellentypen = _ => new System.Collections.Generic.List<Quellentyp>
        {
            new Quellentyp("", "Systemrücklauf"),
            new Quellentyp("Erdreich", "Erdreich")
        },
        QuelleTyp = _ => "Erdreich"
    };

    /// <summary>Die Seite mit sichtbarem Schema.</summary>
    private IRenderedComponent<SimulationKonfigSeite> SeiteMitSchema(bool bauartGebunden = false)
    {
        var cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste(bauartGebunden))
            .Add(x => x.StartProjekt, 1017));
        cut.FindAll("button.epos-simkonfig-ansichtknopf")[1].Click();
        Assert.Equal(SimulationKonfigSeite.ANSICHT_SCHEMA, cut.Instance.AktiveAnsicht);
        return cut;
    }

    private static IElement Element(IRenderedComponent<SimulationKonfigSeite> cut, string titel)
        => cut.FindAll("g.epos-schema-knoten").Single(g => g.GetAttribute("aria-label") == titel);

    [Fact]
    public void Der_Doppelklick_auf_die_Kaeltemaschine_oeffnet_ihren_Anlagendialog()
    {
        var cut = SeiteMitSchema();

        Element(cut, "Kältemaschine 1").DoubleClick();

        Assert.Equal(new[] { Seitenschluessel.KaeltemaschineAnlage }, Navigation.Masken);
        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Der_Doppelklick_auf_die_Rueckkuehlung_der_Kaeltemaschine_oeffnet_denselben_Dialog()
    {
        var cut = SeiteMitSchema();

        Element(cut, "Trockenkühler").DoubleClick();

        Assert.Equal(new[] { Seitenschluessel.KaeltemaschineAnlage }, Navigation.Masken);
    }

    [Fact]
    public void Der_Doppelklick_auf_die_Waermepumpe_im_Kuehlbetrieb_oeffnet_ihren_Senkendialog()
    {
        var cut = SeiteMitSchema();

        Element(cut, "Wärmepumpe 1 (Kühlen)").DoubleClick();
        Assert.Equal("Waermesenke", cut.Instance.OffenerEditor);
        Assert.Empty(Navigation.Masken);
    }

    [Fact]
    public void Die_Waermepumpe_oeffnet_in_beiden_Bahnen_denselben_Editor()
    {
        var oben = SeiteMitSchema();
        Element(oben, "Wärmepumpe 1").DoubleClick();

        var unten = SeiteMitSchema();
        Element(unten, "Wärmepumpe 1 (Kühlen)").DoubleClick();

        Assert.Equal(oben.Instance.OffenerEditor, unten.Instance.OffenerEditor);
    }

    [Fact]
    public void Der_Doppelklick_auf_die_Quelle_der_Waermepumpe_im_Kuehlbetrieb_oeffnet_die_Quellenwahl()
    {
        var cut = SeiteMitSchema();

        Element(cut, "Erdreich (Abwärme)").DoubleClick();

        Assert.Equal("Quellenwahl", cut.Instance.OffenerEditor);
        Assert.Empty(Navigation.Masken);
    }

    [Fact]
    public void Der_Kaeltekreis_traegt_wie_der_Heizkreis_keinen_Doppelklick()
    {
        var cut = SeiteMitSchema();

        Assert.False(Element(cut, "Kältekreis").HasAttribute("blazor:ondblclick"));
        Assert.False(Element(cut, "Heizkreis").HasAttribute("blazor:ondblclick"));
        foreach (string titel in new[] { "Kältemaschine 1", "Trockenkühler", "Wärmepumpe 1 (Kühlen)", "Erdreich (Abwärme)" })
            Assert.True(Element(cut, titel).HasAttribute("blazor:ondblclick"), titel);
    }

    [Fact]
    public void Der_Tooltipp_nennt_den_Weg_in_der_Ressourcensprache()
    {
        var cut = SeiteMitSchema();

        Assert.EndsWith(Resource.SIM_SCHEMA_DK_KAELTEMASCHINE,
                        Element(cut, "Kältemaschine 1").QuerySelector("title")!.TextContent);
        Assert.EndsWith(Resource.SIM_SCHEMA_DK_RUECKKUEHLUNG,
                        Element(cut, "Trockenkühler").QuerySelector("title")!.TextContent);
        Assert.EndsWith(Resource.SIM_SCHEMA_DK_SENKE,
                        Element(cut, "Wärmepumpe 1 (Kühlen)").QuerySelector("title")!.TextContent);
        Assert.EndsWith(Resource.SIM_SCHEMA_DK_QUELLE,
                        Element(cut, "Erdreich (Abwärme)").QuerySelector("title")!.TextContent);
        Assert.Equal("Doppelklick öffnet die Kältemaschinen des Projekts.", Resource.SIM_SCHEMA_DK_KAELTEMASCHINE);
    }

    /// <summary>
    /// Lehnt die Naht ab, verschluckt die Seite das nicht: Der Grund steht im Banner, benannt
    /// wie an der Kachel — ohne gezeichnete Wurzel „keine Oberfläche“.
    /// </summary>
    [Fact]
    public void Eine_abgelehnte_Navigation_steht_benannt_im_Banner()
    {
        Navigation.Antwort = false;
        INavigationsZiel? zielVorher = Navigationsziel.Aktuell;
        Navigationsziel.Aktuell = null;
        try
        {
            var cut = SeiteMitSchema();

            Element(cut, "Kältemaschine 1").DoubleClick();

            string erwartet = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                Resource.WURZEL_ANSICHT_FEHLER, Resource.START_E_KUEHL_TITEL, Resource.WURZEL_KEINE_OBERFLAECHE);
            Assert.Contains(erwartet, cut.Find(".epos-warnbanner").TextContent);
        }
        finally
        {
            Navigationsziel.Aktuell = zielVorher;
        }
    }
}
