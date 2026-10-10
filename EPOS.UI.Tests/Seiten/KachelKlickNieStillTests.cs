using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// KT-2: Der Klick auf die Kachel „Kühlung und Kälteanlagen“ endet nie still. Vier Stellen konnten ihn
/// wortlos verschlucken — die Naht ohne Oberfläche, die Wurzel ohne Projekt, eine Ausnahme beim Aufbau der
/// Gaben und eine Ausnahme beim ersten Zeichnen des Dialogs. Jede nennt jetzt ihren Grund in einem Banner.
/// Dazu der Befund selbst: Unter Windows setzt die Wurzel ihr Projekt nie über die Projektliste, das offene
/// Projekt steht im Projektkontext des Kerns — die Ansicht geht damit auf.
/// Die Fälle tauschen <c>Dienste.Navigation</c>, <c>Dienste.Projekt</c> und <c>Navigationsziel.Aktuell</c>
/// und stehen deshalb in der seriellen Sammlung.
/// </summary>
[Collection("KiDialogweg")]
public class KachelKlickNieStillTests : EposBunitContext
{
    private readonly INavigation _navigationVorher;
    private readonly IProjektKontext _projektVorher;
    private readonly INavigationsZiel? _zielVorher;

    public KachelKlickNieStillTests()
    {
        _navigationVorher = WindowsFormsApplication1.Dienste.Navigation;
        _projektVorher = WindowsFormsApplication1.Dienste.Projekt;
        _zielVorher = Navigationsziel.Aktuell;

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WindowsFormsApplication1.Dienste.Navigation = _navigationVorher;
            WindowsFormsApplication1.Dienste.Projekt = _projektVorher;
            Navigationsziel.Aktuell = _zielVorher;
        }
        base.Dispose(disposing);
    }

    /// <summary>Ein offenes Projekt im Projektkontext des Kerns — der Windows-Weg.</summary>
    private sealed class OffenesProjekt : IProjektKontext
    {
        public OffenesProjekt(int id) { Id = id; }
        public bool Vorhanden => true;
        public int Id { get; }
        public string Name => "Projekt " + Id;
        public string Klimazone => "";
        public bool Uebernehmen(int id, string name) => false;
        public event Action? Gewechselt { add { } remove { } }
    }

    private static TestProjektquelle MitStartseite() => new(Array.Empty<ProjektZeile>())
    {
        Startseite = new Dictionary<string, object> { ["ProjektId"] = new Func<int>(() => 1030) }
    };

    private IRenderedComponent<AppWurzel> WurzelAufStartseite(
        TestProjektquelle quelle, Func<int, IReadOnlyDictionary<string, object>?>? gaben = null)
    {
        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>(p =>
        {
            p.Add(x => x.Startansicht, Seitenschluessel.Startseite);
            if (gaben is not null) p.Add(x => x.KaeltemaschineAnlageGaben, gaben);
        });
    }

    // ---- Stelle 1: die Naht ohne Oberfläche ---------------------------------

    [Fact]
    public void Lehnt_die_Navigation_ab_nennt_der_Reiter_den_Grund_im_Warnbanner()
    {
        var navigation = new TestNavigation { Antwort = false };
        WindowsFormsApplication1.Dienste.Navigation = navigation;
        Navigationsziel.Aktuell = null;

        var cut = Render<ErzeugerReiter>();
        Assert.Empty(cut.FindAll(".epos-warnbanner--warnung"));

        cut.Find("button.epos-startkachel-kuehlung").Click();

        Assert.Equal(new[] { Seitenschluessel.KaeltemaschineAnlage }, navigation.Masken);
        string text = cut.Find(".epos-warnbanner--warnung").TextContent;
        Assert.Contains(R.START_E_KUEHL_TITEL, text);
        Assert.Contains(R.WURZEL_KEINE_OBERFLAECHE, text);
        Assert.Equal(cut.Instance.OeffnenFehler, cut.Find(".epos-warnbanner--warnung .epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Geht_die_Ansicht_auf_bleibt_der_Reiter_ohne_Warnbanner()
    {
        WindowsFormsApplication1.Dienste.Navigation = new TestNavigation { Antwort = true };

        var cut = Render<ErzeugerReiter>();
        cut.Find("button.epos-startkachel-kuehlung").Click();

        Assert.Empty(cut.FindAll(".epos-warnbanner--warnung"));
        Assert.Equal("", cut.Instance.OeffnenFehler);
    }

    // ---- Stelle 2: die Wurzel ohne Projekt -------------------------------

    [Fact]
    public void Ohne_Projekt_steht_KMA_KEINE_ANSICHT_ueber_der_Startseite()
    {
        WindowsFormsApplication1.Dienste.Projekt = new LeererProjektKontext();
        var cut = WurzelAufStartseite(MitStartseite());
        Assert.Single(cut.FindAll(".epos-startseite"));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.KaeltemaschineAnlage, ""));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Equal(R.KMA_KEINE_ANSICHT, cut.Instance.AnsichtFehler);
        Assert.Contains(R.KMA_KEINE_ANSICHT, cut.Find(".epos-warnbanner--warnung").TextContent);
    }

    // ---- Der Befund: das offene Projekt aus dem Kern ---------------------

    [Fact]
    public void Mit_offenem_Projekt_im_Projektkontext_geht_die_Ansicht_auf()
    {
        WindowsFormsApplication1.Dienste.Projekt = new OffenesProjekt(1030);
        int gefragt = 0;
        var cut = WurzelAufStartseite(MitStartseite(), id =>
        {
            gefragt = id;
            return new Dictionary<string, object>();
        });

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.KaeltemaschineAnlage, ""));
        cut.Render();

        Assert.Equal(1030, gefragt);
        Assert.Single(cut.FindComponents<KaeltemaschineAnlageDialog>());
        Assert.Equal("", cut.Instance.AnsichtFehler);
    }

    // ---- Stelle 3: eine Ausnahme beim Aufbau der Gaben -------------------

    [Fact]
    public void Werfen_die_Gaben_steht_die_Meldung_ueber_der_Startseite()
    {
        WindowsFormsApplication1.Dienste.Projekt = new OffenesProjekt(1030);
        var cut = WurzelAufStartseite(MitStartseite(),
            _ => throw new InvalidOperationException("Kühlträger fehlt im Test"));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.KaeltemaschineAnlage, ""));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Empty(cut.FindComponents<KaeltemaschineAnlageDialog>());
        string text = cut.Find(".epos-warnbanner--warnung").TextContent;
        Assert.Contains(R.START_E_KUEHL_TITEL, text);
        Assert.Contains("Kühlträger fehlt im Test", text);
    }

    [Fact]
    public void Der_naechste_gelungene_Wechsel_leert_den_Banner()
    {
        WindowsFormsApplication1.Dienste.Projekt = new LeererProjektKontext();
        var cut = WurzelAufStartseite(MitStartseite(), _ => new Dictionary<string, object>());
        cut.Instance.OeffneMaske(Seitenschluessel.KaeltemaschineAnlage, "");
        cut.Render();
        Assert.NotEqual("", cut.Instance.AnsichtFehler);

        WindowsFormsApplication1.Dienste.Projekt = new OffenesProjekt(1030);
        cut.Instance.OeffneMaske(Seitenschluessel.KaeltemaschineAnlage, "");
        cut.Render();

        Assert.Equal("", cut.Instance.AnsichtFehler);
        Assert.Single(cut.FindComponents<KaeltemaschineAnlageDialog>());
    }

    // ---- Stelle 4: eine Ausnahme beim ersten Zeichnen des Dialogs --------

    [Fact]
    public void Werfen_die_Anlagen_zeichnet_der_Dialog_mit_Banner_statt_abzubrechen()
    {
        var cut = Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, () => throw new InvalidOperationException("Tabelle gesperrt"))
            .Add(x => x.Waermepumpen, () => throw new InvalidOperationException("WP gesperrt")));

        string text = cut.Find(".epos-warnbanner--warnung .epos-warnbanner-text").TextContent;
        Assert.Contains("Tabelle gesperrt", text);
        Assert.Contains("WP gesperrt", text);
        Assert.StartsWith(R.KMA_LADEN_FEHLER.Split('{')[0].Trim(), text.Trim());
        Assert.Empty(cut.Instance.Namensliste);
    }
}
