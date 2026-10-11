using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Kachel „Kühlung und Kälteanlagen“ im Reiter Energieerzeuger der Startseite: eine Kachel wie die übrigen —
/// Titel, eine Beschreibungszeile, Sinnbild und Statuspunkt, kein Detailtext und keine Bedienelemente
/// (Anwenderwunsch 08.10.2026). Ein Klick öffnet den Erzeugerdialog der Kältemaschinen; die Schalter
/// „Wärmepumpen im Kühlbetrieb“ stehen dort (<see cref="EPOS.UI.Tests.Dialoge.KaeltemaschineAnlageWaermepumpenTests"/>).
/// Der Fall tauscht <c>Dienste.Navigation</c> und steht deshalb in der seriellen Sammlung.
/// </summary>
[Collection("KiDialogweg")]
public class ErzeugerReiterKuehlungTests : EposBunitContext
{
    private readonly INavigation _vorher;

    public ErzeugerReiterKuehlungTests()
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

    private static KuehlungKachelDaten Bestand(bool kaeltemaschine = true, bool wpImKuehlbetrieb = true) => new()
    {
        KuehlungRechnen = true,
        Kaeltemaschinen = kaeltemaschine
            ? new[] { new KuehlKaeltemaschine("Kältemaschine 1", 1), new KuehlKaeltemaschine("Kältemaschine 2", 2) }
            : Array.Empty<KuehlKaeltemaschine>(),
        Waermepumpen = new[]
        {
            new KuehlWaermepumpe(11, "Wärmepumpe 1", wpImKuehlbetrieb, null),
            new KuehlWaermepumpe(13, "Wärmepumpe 3", false, "Kennlinie fehlt")
        }
    };

    private IRenderedComponent<ErzeugerReiter> Zeichnen(KuehlungKachelDaten? daten)
        => Render<ErzeugerReiter>(p => p.Add(x => x.Kuehlung, daten is null ? null : () => daten));

    private static string OhneLeerraum(string s) => string.Concat(s.Where(c => !char.IsWhiteSpace(c)));

    [Fact]
    public void Die_Kachel_steht_im_Kachelraster_und_oeffnet_die_Kaeltemaschinen()
    {
        var cut = Zeichnen(Bestand());

        Assert.Empty(cut.FindAll("button.epos-startreiter-kaeltemaschine"));
        Assert.Empty(cut.FindAll(".epos-startreiter-zusatz"));

        var kachel = cut.Find(".epos-kachelraster button.epos-kachel.epos-startkachel-kuehlung");
        Assert.Null(kachel.Closest(".epos-startkachel-mit-wahl"));
        Assert.Equal(Resource.START_E_KUEHL_TITEL, kachel.QuerySelector(".epos-kachel-titel")!.TextContent.Trim());
        Assert.Equal(Resource.START_E_KUEHL_TEXT, kachel.QuerySelector(".epos-kachel-beschreibung")!.TextContent.Trim());

        kachel.Click();
        Assert.Equal(new[] { Seitenschluessel.KaeltemaschineAnlage }, Navigation.Masken);
    }

    [Fact]
    public void Titel_und_Beschreibung_sind_die_des_Auftrags()
    {
        Assert.Equal("Kühlung und Kälteanlagen", Resource.START_E_KUEHL_TITEL);
        Assert.Equal("Kälteerzeugung mit Kältemaschinen und Wärmepumpen", Resource.START_E_KUEHL_TEXT);
    }

    [Fact]
    public void Die_Kachel_traegt_ihr_Sinnbild_wie_die_uebrigen()
    {
        var cut = Zeichnen(Bestand());

        var bild = cut.Find("button.epos-startkachel-kuehlung img");
        Assert.Equal(Kachelbilder.KuehlungQuelle, bild.GetAttribute("src"));
        Assert.Contains(Kachelbilder.KLASSE_SYMBOL, bild.ClassName);
        Assert.Equal("", bild.GetAttribute("alt"));
        Assert.EndsWith("/" + Kachelbilder.KUEHLUNG_DATEI, Kachelbilder.KuehlungQuelle);

        // Ohne Gaben dasselbe Bild.
        Assert.Equal(Kachelbilder.KuehlungQuelle,
                     Render<ErzeugerReiter>().Find("button.epos-startkachel-kuehlung img").GetAttribute("src"));
    }

    [Fact]
    public void Auf_der_Kachel_stehen_nur_Titel_und_Beschreibung_keine_Liste_und_kein_Schalter()
    {
        var cut = Zeichnen(Bestand());

        var kachel = cut.Find("button.epos-startkachel-kuehlung");
        Assert.Equal(OhneLeerraum(Resource.START_E_KUEHL_TITEL + Resource.START_E_KUEHL_TEXT), OhneLeerraum(kachel.TextContent));
        Assert.Empty(cut.FindAll(".epos-startkachel-kuehlwahl, .epos-startkachel-kaeltemaschinen, .epos-startkachel-kuehlpumpen"));
        Assert.Empty(cut.FindAll(".epos-kachelraster input"));
        Assert.DoesNotContain("Kältemaschine 1", cut.Markup);
        Assert.DoesNotContain("Wärmepumpe 1", cut.Markup);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void Der_Statuspunkt_ist_gruen_mit_Kaeltemaschine_oder_Waermepumpe_im_Kuehlbetrieb(
        bool kaeltemaschine, bool wpImKuehlbetrieb, bool gruen)
    {
        var cut = Zeichnen(Bestand(kaeltemaschine, wpImKuehlbetrieb));

        var punkt = cut.Find("button.epos-startkachel-kuehlung .epos-kachel-statuspunkt");
        Assert.Equal(!gruen, punkt.ClassName!.Contains("epos-kachel-statuspunkt--aus"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_die_Kachel_mit_grauem_Punkt()
    {
        var ohne = Render<ErzeugerReiter>();
        var kachel = ohne.Find("button.epos-startkachel-kuehlung");
        Assert.Contains("epos-kachel-statuspunkt--aus", kachel.QuerySelector(".epos-kachel-statuspunkt")!.ClassName);
        Assert.Equal(OhneLeerraum(Resource.START_E_KUEHL_TITEL + Resource.START_E_KUEHL_TEXT), OhneLeerraum(kachel.TextContent));
    }
}
