using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// S2 — das HAUPTFENSTER (iU9-W16c.2), die Windows-Schale um
/// <see cref="AppWurzel"/>.
///
/// <para>Geprueft wird der Zuschnitt, der den Rueckbau von <c>MDIMainForm</c>
/// traegt: Menueband und Kopfband stehen ueber JEDER Ansicht, und
/// <c>Springe</c> ist der EINZIGE Handler — erst der Weg der Huelle, dann die
/// Wurzel. Faellt diese Reihenfolge, oeffnet ein Menuepunkt unter Windows eine
/// Ansicht, wo er bisher ein modales Fenster zeigte.</para>
///
/// <para>Die Sprache ist auf de-DE gepinnt (Regel seit iU9-W8): Die
/// Menuebeschriftungen kommen aus <c>MyResource</c>, und der Windows-Laeufer
/// laeuft englisch.</para>
/// </summary>
public class HauptfensterTests : BunitContext
{
    public HauptfensterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Services.AddSingleton<IProjektQuelle>(new KeineProjekte());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    protected override void Dispose(bool disposing)
    {
        Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    /// <summary>Der kleinste Parametersatz, mit dem die Startseite steht.</summary>
    private static IReadOnlyDictionary<string, object> Startgaben() =>
        new Dictionary<string, object>
        {
            ["ProjektId"] = new Func<int>(() => 1030),
        };

    private IRenderedComponent<Hauptfenster> Fenster(
        Func<string, string, Task<bool>>? weg = null,
        IReadOnlyDictionary<string, object>? startgaben = null,
        string startansicht = Seitenschluessel.Projektliste)
    {
        return Render<Hauptfenster>(p => p
            .Add(x => x.Weg, weg)
            .Add(x => x.Startansicht, startansicht)
            .Add(x => x.StartseiteGaben, startgaben)
            .Add(x => x.VersionText, "Version 1.0.0.0"));
    }

    // =====================================================================
    //  Aufbau
    // =====================================================================

    [Fact]
    public void Das_Fenster_traegt_Menueband_Kopfband_und_Inhalt()
    {
        var cut = Fenster();

        Assert.Single(cut.FindAll(".epos-menueband"));
        Assert.Single(cut.FindAll(".epos-hauptfenster-marke"));

        // Ohne Startseitengaben bleibt die Projektliste stehen - der Zustand
        // der iOS-Huelle und zugleich der Rueckfall unter Windows.
        Assert.Single(cut.FindAll(".epos-seite"));
    }

    [Fact]
    public void Das_Kopfband_zeigt_Name_Gattung_Claim_und_Version()
    {
        // InitMarke (MDIMainForm.cs:217-294) setzte genau diese vier Texte;
        // die ersten drei waren deutsche LITERALE im Code (Befund W16-B25).
        var cut = Fenster();

        Assert.Equal("EPOS-Plan", cut.Find(".epos-hauptfenster-name").TextContent.Trim());
        Assert.Contains("Energieplanungs-Software",
                        cut.Find(".epos-hauptfenster-untertitel").TextContent, StringComparison.Ordinal);
        Assert.Contains("Energie · Planung · Optimierung · Simulation",
                        cut.Find(".epos-hauptfenster-untertitel").TextContent, StringComparison.Ordinal);
        Assert.Equal("Version 1.0.0.0", cut.Find(".epos-hauptfenster-version").TextContent.Trim());
    }

    [Fact]
    public void Das_Kopfband_traegt_die_Fensterhilfe()
    {
        // Offener Punkt W16b-O-4: Form_Start.btn_Help war der Knopf des
        // FENSTERS (Befund W16b-B5) - er sass oberhalb des Reiterwerks und
        // meinte den Ablauf des Programms. Sein Ziel in help_mapping.txt ist
        // "Programmablauf"; hier traegt ihn das Kopfband.
        var cut = Fenster();

        Assert.Single(cut.FindAll(".epos-hauptfenster-marke .epos-infoknopf"));
        Assert.Equal("Hauptfenster.btn_Help", cut.Instance.HilfeSchluessel);
    }

    [Fact]
    public void Das_Menueband_steht_ueber_jeder_Ansicht()
    {
        // Unter Windows verschwindet das Menue nie - auch nicht, wenn die
        // Konfigurationsseite die Startseite abloest (E-5). Deshalb ist es die
        // Kopfleiste der Wurzel und nicht Teil einer Ansicht.
        var cut = Fenster(startgaben: Startgaben(), startansicht: Seitenschluessel.Startseite);

        Assert.Single(cut.FindAll(".epos-menueband"));
        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    // =====================================================================
    //  Der EINE Handler
    // =====================================================================

    [Fact]
    public async Task Ein_Menueklick_geht_zuerst_den_Weg_der_Huelle()
    {
        var gegangen = new List<(string Ziel, string Argument)>();

        var cut = Fenster(weg: (ziel, argument) =>
        {
            gegangen.Add((ziel, argument));
            return Task.FromResult(true);
        });

        await cut.Instance.Springe(
            new Menuepunkt("MenuItem_PV_Import_CEC", "MENU_PV_IMPORT_CEC",
                           Seitenschluessel.PvImport, argument: "CEC"));

        Assert.Single(gegangen);
        Assert.Equal(Seitenschluessel.PvImport, gegangen[0].Ziel);
        Assert.Equal("CEC", gegangen[0].Argument);
    }

    [Fact]
    public async Task Was_der_Weg_nicht_behandelt_wechselt_die_Ansicht()
    {
        // Der iOS-Zustand (kein Weg) und der Windows-Rueckfall sind derselbe
        // Programmtext: Was die Huelle nicht kennt, entscheidet die Wurzel.
        var cut = Fenster(weg: (_, _) => Task.FromResult(false),
                          startgaben: Startgaben());

        Assert.Empty(cut.FindAll(".epos-startseite"));

        await cut.Instance.Springe(
            new Menuepunkt("Startseite", "MENU_PROJEKTE", Seitenschluessel.Startseite));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    [Fact]
    public async Task Ein_Punkt_ohne_Ziel_tut_nichts()
    {
        // Die zwoelf aufklappenden Punkte und die acht Trenner tragen kein
        // Ziel; ein Klick auf sie darf keinen Weg anstossen.
        int rufe = 0;
        var cut = Fenster(weg: (_, _) => { rufe++; return Task.FromResult(true); });

        await cut.Instance.Springe(new Menuepunkt("Administration", "MENU_ADMINISTRATION", ""));
        await cut.Instance.Springe(Menuepunkt.Trennstrich("toolStripSeparator1"));

        Assert.Equal(0, rufe);
    }

    [Fact]
    public void Zeige_setzt_eine_Ansicht_von_aussen()
    {
        // Der Weg, den die Huelle nach einem Projektwechsel oder aus einem
        // Dialog heraus geht - im Bestand war das Program.startfrm bzw.
        // StartseiteHuelle.Aktuelle.
        var cut = Fenster(startgaben: Startgaben());

        Assert.True(cut.Instance.Zeige(Seitenschluessel.Startseite));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    // =====================================================================
    //  Das Menue selbst
    // =====================================================================

    [Fact]
    public async Task Ein_Klick_im_Band_landet_im_selben_Handler()
    {
        string? gemeldet = null;
        var cut = Fenster(weg: (ziel, _) => { gemeldet = ziel; return Task.FromResult(true); });

        cut.Find("#menue-Projekte").Click();
        cut.Find("#menue-MenuItem_ProjektNeu").Click();

        await Task.Yield();

        Assert.Equal(Seitenschluessel.ProjektNeu, gemeldet);
    }

    [Fact]
    public void Das_Band_fuehrt_die_Tabelle_des_Bestands()
    {
        var cut = Fenster();

        // VIER Punkte der obersten Ebene, deutsche Beschriftung. Bis zum
        // Anwenderentscheid W16c-E-2 (04.09.2026) standen "Deutsch" und
        // "Englisch" als eigene Koepfe daneben.
        string band = cut.Find(".epos-menueband").TextContent;
        Assert.Contains("Projekt", band, StringComparison.Ordinal);
        Assert.Contains("Administration", band, StringComparison.Ordinal);
        Assert.Contains("Hilfe", band, StringComparison.Ordinal);
        Assert.Contains("Sprache", band, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Der_Sprachwechsel_geht_ueber_das_Untermenue_Sprache()
    {
        // W16c-E-2: Der Weg der Huelle bekommt denselben Seitenschluessel wie
        // vorher (HauptfensterHuelle.SpracheSetzen + Application.Restart) - nur
        // haengt der Punkt jetzt unter dem Kopf "Sprache".
        string? gemeldet = null;
        var cut = Fenster(weg: (ziel, _) => { gemeldet = ziel; return Task.FromResult(true); });

        cut.Find("#menue-Sprache").Click();
        cut.Find("#menue-Englisch").Click();

        await Task.Yield();

        Assert.Equal(Seitenschluessel.SpracheEnglisch, gemeldet);
    }
}
