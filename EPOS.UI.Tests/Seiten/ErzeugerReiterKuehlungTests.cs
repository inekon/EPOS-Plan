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
/// Die Kachel „Kühlung“ im Reiter Energieerzeuger der Startseite: Sie steht an der Stelle des früheren
/// Knopfes zu den Kältemaschinen, öffnet deren Erzeugerdialog, zeigt die Kältemaschinen des Projekts und die
/// Auswahl der Wärmepumpen mit Kühlfunktion, deren Schalter über die Naht <c>KuehlbetriebSchreiben</c>
/// schreiben. Der Fall tauscht <c>Dienste.Navigation</c> und steht deshalb in der seriellen Sammlung.
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

    private static KuehlungKachelDaten Bestand(bool rechnen = true) => new()
    {
        KuehlungRechnen = rechnen,
        Kaeltemaschinen = new[] { new KuehlKaeltemaschine("Kältemaschine 1", 1), new KuehlKaeltemaschine("Kältemaschine 2", 2) },
        Waermepumpen = new[]
        {
            new KuehlWaermepumpe(11, "Wärmepumpe 1", true, null),
            new KuehlWaermepumpe(12, "Wärmepumpe 2", false, null),
            new KuehlWaermepumpe(13, "Wärmepumpe 3", false, "Kennlinie fehlt")
        }
    };

    private IRenderedComponent<ErzeugerReiter> Zeichnen(KuehlungKachelDaten? daten,
                                                        Func<int, bool, string?>? schreiben = null)
        => Render<ErzeugerReiter>(p => p
            .Add(x => x.Kuehlung, daten is null ? null : () => daten)
            .Add(x => x.KuehlbetriebSchreiben, schreiben));

    [Fact]
    public void Statt_des_Knopfes_steht_die_Kachel_Kuehlung_und_oeffnet_die_Kaeltemaschinen()
    {
        var cut = Zeichnen(Bestand());

        Assert.Empty(cut.FindAll("button.epos-startreiter-kaeltemaschine"));
        Assert.Empty(cut.FindAll(".epos-startreiter-zusatz"));

        var kachel = cut.Find(".epos-kachelraster .epos-startkachel-mit-wahl.epos-startkachel-kuehlung > button.epos-kachel");
        Assert.Equal(Resource.START_E_KUEHL_TITEL, kachel.QuerySelector(".epos-kachel-titel")!.TextContent.Trim());
        Assert.Equal(Resource.START_E_KUEHL_TEXT, kachel.QuerySelector(".epos-kachel-beschreibung")!.TextContent.Trim());

        kachel.Click();
        Assert.Equal(new[] { Seitenschluessel.KaeltemaschineAnlage }, Navigation.Masken);
    }

    [Fact]
    public void Die_Kaeltemaschinen_des_Projekts_stehen_auf_der_Kachel()
    {
        var cut = Zeichnen(Bestand());

        string[] zeilen = cut.FindAll(".epos-startkachel-kaeltemaschinen li").Select(l => l.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Kältemaschine 1", string.Format(Resource.START_E_KUEHL_KM_ANZAHL, "Kältemaschine 2", 2) }, zeilen);
        Assert.Contains(Resource.START_E_KUEHL_KM, cut.Find(".epos-startkachel-kaeltemaschinen").TextContent);
        var punkt = cut.Find(".epos-startkachel-kuehlung .epos-kachel .epos-kachel-statuspunkt");
        Assert.DoesNotContain("epos-kachel-statuspunkt--aus", punkt.ClassName);
    }

    [Fact]
    public void Die_Auswahl_zeigt_je_kuehlfaehige_Waermepumpe_einen_Schalter_samt_Sperrgrund()
    {
        var cut = Zeichnen(Bestand(), (_, _) => null);

        var schalter = cut.FindAll(".epos-startkachel-kuehlpumpen label.epos-schalter");
        Assert.Equal(new[] { "Wärmepumpe 1", "Wärmepumpe 2", "Wärmepumpe 3" },
                     schalter.Select(s => s.TextContent.Trim()).ToArray());

        var kaesten = cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]");
        Assert.True(kaesten[0].HasAttribute("checked"));
        Assert.False(kaesten[1].HasAttribute("checked"));
        Assert.False(kaesten[1].HasAttribute("disabled"));
        Assert.True(kaesten[2].HasAttribute("disabled"));
        Assert.Contains("Kennlinie fehlt", cut.Find(".epos-startkachel-kuehlpumpen").TextContent);
    }

    [Fact]
    public void Ohne_kuehlfaehige_Waermepumpe_steht_keine_Auswahl()
    {
        var daten = new KuehlungKachelDaten
        {
            KuehlungRechnen = true,
            Kaeltemaschinen = new[] { new KuehlKaeltemaschine("Kältemaschine 1", 1) }
        };
        var cut = Zeichnen(daten, (_, _) => null);

        Assert.Empty(cut.FindAll(".epos-startkachel-kuehlpumpen"));
        Assert.Single(cut.FindAll(".epos-startkachel-kaeltemaschinen li"));
    }

    [Fact]
    public void Der_Schalter_ruft_den_Schreibweg_und_meldet_dessen_Grund()
    {
        var aufrufe = new List<(int Id, bool An)>();
        string? antwort = null;
        var cut = Zeichnen(Bestand(), (id, an) => { aufrufe.Add((id, an)); return antwort; });

        cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]")[1].Change(true);
        Assert.Equal(new[] { (12, true) }, aufrufe);
        Assert.Empty(cut.FindAll(".epos-startkachel-kuehlwahl .epos-warnbanner--warnung"));

        antwort = "Der Kühlbetrieb bleibt gesperrt.";
        cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]")[0].Change(false);
        Assert.Equal((11, false), aufrufe[1]);
        Assert.Contains("Der Kühlbetrieb bleibt gesperrt.", cut.Find(".epos-startkachel-kuehlwahl").TextContent);
    }

    [Fact]
    public void Ohne_Schreibweg_sind_die_Schalter_gesperrt()
    {
        var cut = Zeichnen(Bestand());

        Assert.All(cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]"),
                   k => Assert.True(k.HasAttribute("disabled")));
    }

    [Fact]
    public void Ist_Kuehlung_rechnen_aus_nennt_die_Kachel_das_ohne_zu_sperren()
    {
        var cut = Zeichnen(Bestand(rechnen: false), (_, _) => null);

        Assert.Contains(Resource.START_E_KUEHL_PROJEKT_AUS, cut.Find(".epos-startkachel-kuehlwahl").TextContent);
        Assert.False(cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]")[1].HasAttribute("disabled"));

        var an = Zeichnen(Bestand(rechnen: true), (_, _) => null);
        Assert.DoesNotContain(Resource.START_E_KUEHL_PROJEKT_AUS, an.Find(".epos-startkachel-kuehlwahl").TextContent);
    }

    [Fact]
    public void Die_leere_Kachel_zeigt_ihren_Hinweis_auch_ohne_Gaben()
    {
        var leer = Zeichnen(new KuehlungKachelDaten { KuehlungRechnen = true });
        Assert.Contains(Resource.START_E_KUEHL_LEER, leer.Find(".epos-startkachel-kuehlwahl").TextContent);
        Assert.Empty(leer.FindAll(".epos-startkachel-kuehlwahl input"));
        Assert.Contains("epos-kachel-statuspunkt--aus", leer.Find(".epos-startkachel-kuehlung .epos-kachel-statuspunkt").ClassName);

        var ohne = Render<ErzeugerReiter>();
        Assert.Contains(Resource.START_E_KUEHL_LEER, ohne.Find(".epos-startkachel-kuehlwahl").TextContent);
    }
}
