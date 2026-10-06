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
/// Die Kachel „Kühlung und Kälteanlagen“ im Reiter Energieerzeuger der Startseite: Sie steht an der Stelle
/// des früheren Knopfes zu den Kältemaschinen, trägt ein Sinnbild wie die übrigen Kacheln, öffnet den
/// Erzeugerdialog der Kältemaschinen und zeigt darunter nur Bedienelemente — die Kältemaschinen des Projekts und
/// die Schalter der Wärmepumpen mit Kühlfunktion, die über die Naht <c>KuehlbetriebSchreiben</c> schreiben —,
/// keine Erklärtexte (Anwenderauftrag 06.10.2026); der Grund einer Sperre steht als <c>title</c> am Schalter. Der Fall tauscht <c>Dienste.Navigation</c> und steht deshalb in der seriellen Sammlung.
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
    public void Titel_und_Beschreibung_sind_die_des_Auftrags()
    {
        Assert.Equal("Kühlung und Kälteanlagen", Resource.START_E_KUEHL_TITEL);
        Assert.Equal("Kälteerzeugung mit Kältemaschinen und Wärmepumpen", Resource.START_E_KUEHL_TEXT);
    }

    [Fact]
    public void Die_Kachel_traegt_ihr_Sinnbild_wie_die_uebrigen()
    {
        var cut = Zeichnen(Bestand());

        var bild = cut.Find(".epos-startkachel-kuehlung > button.epos-kachel img");
        Assert.Equal(Kachelbilder.KuehlungQuelle, bild.GetAttribute("src"));
        Assert.Contains(Kachelbilder.KLASSE_SYMBOL, bild.ClassName);
        Assert.Equal("", bild.GetAttribute("alt"));
        Assert.EndsWith("/" + Kachelbilder.KUEHLUNG_DATEI, Kachelbilder.KuehlungQuelle);

        // Ohne Gaben dasselbe Bild.
        Assert.Equal(Kachelbilder.KuehlungQuelle,
                     Render<ErzeugerReiter>().Find(".epos-startkachel-kuehlung img").GetAttribute("src"));
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
    public void Die_Auswahl_zeigt_je_kuehlfaehige_Waermepumpe_einen_Schalter_und_den_Sperrgrund_als_title()
    {
        var cut = Zeichnen(Bestand(), (_, _) => null);

        var schalter = cut.FindAll(".epos-startkachel-kuehlpumpen label.epos-schalter");
        Assert.Equal(new[] { "Wärmepumpe 1", "Wärmepumpe 2", "Wärmepumpe 3" },
                     schalter.Select(s => s.TextContent.Trim()).ToArray());

        var kaesten = cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]");
        Assert.True(kaesten[0].HasAttribute("checked"));
        Assert.False(kaesten[1].HasAttribute("checked"));
        Assert.False(kaesten[1].HasAttribute("disabled"));
        // Weiche Sperre mit Grund am Bedienelement: aria-disabled statt disabled, Grund als title.
        Assert.False(kaesten[2].HasAttribute("disabled"));
        Assert.Equal("true", kaesten[2].GetAttribute("aria-disabled"));
        Assert.Equal("Kennlinie fehlt", schalter[2].GetAttribute("title"));
        Assert.False(schalter[0].HasAttribute("title"));
        Assert.False(schalter[1].HasAttribute("title"));
        // Keine Grundzeile mehr unter dem Schalter.
        Assert.Empty(cut.FindAll(".epos-startkachel-kuehlpumpen .epos-herleitungszeile"));
        Assert.Equal("Wärmepumpe 3", cut.FindAll(".epos-startkachel-kuehlpumpe")[2].TextContent.Trim());
    }

    [Fact]
    public void Ein_weich_gesperrter_Schalter_schreibt_nicht()
    {
        var aufrufe = new List<(int Id, bool An)>();
        var cut = Zeichnen(Bestand(), (id, an) => { aufrufe.Add((id, an)); return null; });

        cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]")[2].Change(true);
        Assert.Empty(aufrufe);
    }

    [Fact]
    public void Ohne_kuehlfaehige_Waermepumpe_steht_keine_Auswahl_und_ohne_Kaeltemaschine_keine_Liste()
    {
        var daten = new KuehlungKachelDaten
        {
            KuehlungRechnen = true,
            Kaeltemaschinen = new[] { new KuehlKaeltemaschine("Kältemaschine 1", 1) }
        };
        var cut = Zeichnen(daten, (_, _) => null);

        Assert.Empty(cut.FindAll(".epos-startkachel-kuehlpumpen"));
        Assert.Single(cut.FindAll(".epos-startkachel-kaeltemaschinen li"));

        var nurPumpen = Zeichnen(new KuehlungKachelDaten
        {
            KuehlungRechnen = true,
            Waermepumpen = new[] { new KuehlWaermepumpe(11, "Wärmepumpe 1", false, null) }
        }, (_, _) => null);
        Assert.Empty(nurPumpen.FindAll(".epos-startkachel-kaeltemaschinen"));
        Assert.Single(nurPumpen.FindAll(".epos-startkachel-kuehlpumpen label.epos-schalter"));
        Assert.Empty(nurPumpen.FindAll(".epos-startkachel-kuehlwahl .epos-herleitungszeile"));
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
    public void Ist_Kuehlung_rechnen_aus_steht_kein_Infokasten_und_nichts_ist_gesperrt()
    {
        var cut = Zeichnen(Bestand(rechnen: false), (_, _) => null);

        Assert.Empty(cut.FindAll(".epos-startkachel-kuehlung .epos-warnbanner"));
        Assert.DoesNotContain("Kühlung rechnen", cut.Find(".epos-startkachel-kuehlung").TextContent);
        Assert.False(cut.FindAll(".epos-startkachel-kuehlpumpen input[type=checkbox]")[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Die_leere_Kachel_zeigt_keinen_Leertext_und_keine_Wahl_auch_ohne_Gaben()
    {
        var leer = Zeichnen(new KuehlungKachelDaten { KuehlungRechnen = false });
        var kachel = leer.Find(".epos-startkachel-kuehlung");
        Assert.Empty(kachel.QuerySelectorAll(".epos-startkachel-kuehlwahl"));
        Assert.Empty(kachel.QuerySelectorAll(".epos-herleitungszeile, .epos-warnbanner, input"));
        // Auf der Kachel stehen nur Titel und Beschreibung, sonst kein Text.
        static string OhneLeerraum(string s) => string.Concat(s.Where(c => !char.IsWhiteSpace(c)));
        Assert.Equal(OhneLeerraum(Resource.START_E_KUEHL_TITEL + Resource.START_E_KUEHL_TEXT), OhneLeerraum(kachel.TextContent));
        Assert.Contains("epos-kachel-statuspunkt--aus", leer.Find(".epos-startkachel-kuehlung .epos-kachel-statuspunkt").ClassName);

        var ohne = Render<ErzeugerReiter>();
        Assert.Empty(ohne.FindAll(".epos-startkachel-kuehlwahl"));
        Assert.Empty(ohne.FindAll(".epos-startkachel-kuehlung .epos-herleitungszeile"));
    }
}
