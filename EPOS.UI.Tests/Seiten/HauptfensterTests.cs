using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
/// <para>Geprueft wird der Zuschnitt, der den Rueckbau von <c>Hauptfensterrahmen</c>
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

    /// <summary>
    /// Der kleinste Parametersatz, mit dem die Startseite steht.
    ///
    /// <para>Mit <paramref name="eigener"/> traegt er — wie
    /// <c>StartseiteHuelle.Gaben()</c> — einen EIGENEN <see cref="SeitenZustand"/>;
    /// ohne ihn ist es der iOS-Fall, in dem die Wurzel ihren beilegt
    /// (Befund W16c‑B12).</para>
    /// </summary>
    private static IReadOnlyDictionary<string, object> Startgaben(
        SeitenZustand? eigener = null,
        Func<IReadOnlyList<(int Id, string Name)>>? varianten = null)
    {
        var gaben = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["ProjektId"] = new Func<int>(() => 1030),
        };
        if (eigener is not null) gaben[SeitenZustand.PARAMETER] = eigener;
        if (varianten is not null) gaben["Varianten"] = varianten;
        return gaben;
    }

    /// <summary>
    /// Der kleinste Parametersatz von „Berichte &amp; Kosten". Ohne
    /// <c>SeitenGaben</c> steht statt einer Seite der Hinweis — der Rahmen
    /// (Navigation, Kopfzeile, Rückwegknopf) steht trotzdem, und der ist hier
    /// der Gegenstand.
    /// </summary>
    private static IReadOnlyDictionary<string, object> Berichtegaben() =>
        new Dictionary<string, object>
        {
            ["ZurueckText"] = "◀ Zurück",
        };

    private IRenderedComponent<Hauptfenster> Fenster(
        Func<string, string, Task<bool>>? weg = null,
        IReadOnlyDictionary<string, object>? startgaben = null,
        IReadOnlyDictionary<string, object>? berichtegaben = null,
        string startansicht = Seitenschluessel.Projektliste)
    {
        return Render<Hauptfenster>(p => p
            .Add(x => x.Weg, weg)
            .Add(x => x.Startansicht, startansicht)
            .Add(x => x.StartseiteGaben, startgaben)
            .Add(x => x.BerichteKostenGaben, berichtegaben)
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
        // Die zwoelf aufklappenden Punkte (seit W16c-E-2 mit "Sprache", seit
        // W16c-E-6 mit "Profile & Lastgaenge" und ohne die zwei aufgeloesten
        // Ein-Punkt-Untermenues) und die acht Trenner tragen kein Ziel; ein
        // Klick auf sie darf keinen Weg anstossen.
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
    public async Task Varianten_und_Bericht_wechselt_auf_die_Ansicht_BerichteKosten()
    {
        // ANWENDERENTSCHEID W16c-E-3 (04.09.2026): Der Menuepunkt holt NICHT
        // mehr den sechsten Reiter der Startseite nach vorn
        // (StartseiteHuelle.ZeigeBerichteKosten), sondern wechselt die ANSICHT
        // der AppWurzel - derselbe Weg wie auf iOS. Der Weg der Huelle meldet
        // dafuer false; "BERICHTE_KOSTEN" steht in Ansichten, nicht in Masken.
        var cut = Fenster(weg: (_, _) => Task.FromResult(false),
                          startgaben: Startgaben(),
                          berichtegaben: Berichtegaben(),
                          startansicht: Seitenschluessel.Startseite);

        Assert.Single(cut.FindAll(".epos-startseite"));

        Menuepunkt punkt = Menuetabelle.Alle.Single(p => p.Name == "MenuItem_VariantenBericht");
        Assert.Equal(Seitenschluessel.BerichteKosten, punkt.Ziel);

        await cut.Instance.Springe(punkt);
        cut.Render();

        // Die Startseite ist ABGELOEST, die Seite "Berichte & Kosten" steht -
        // und das Menueband darueber wie ueber jeder Ansicht.
        Assert.Empty(cut.FindAll(".epos-startseite"));
        Assert.Single(cut.FindAll(".epos-navigation"));
        Assert.Single(cut.FindAll(".epos-menueband"));
    }

    [Fact]
    public async Task Der_Rueckweg_aus_Berichte_und_Kosten_fuehrt_zur_Startansicht()
    {
        // Der Rueckwegknopf gibt es nur, weil AppWurzel einen "Geschlossen"-
        // Rueckruf setzt (ZurueckZurListe); im sechsten Reiterblatt der
        // Startseite fehlt er - dort fuehrt die Reiterleiste zurueck.
        var cut = Fenster(weg: (_, _) => Task.FromResult(false),
                          startgaben: Startgaben(),
                          berichtegaben: Berichtegaben(),
                          startansicht: Seitenschluessel.Startseite);

        await cut.Instance.Springe(
            Menuetabelle.Alle.Single(p => p.Name == "MenuItem_VariantenBericht"));
        cut.Render();

        cut.Find(".epos-navigation-zurueck").Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Empty(cut.FindAll(".epos-navigation"));
    }

    // =====================================================================
    //  Der Weg der WINDOWS-HUELLE (Befund W16c-B12)
    // =====================================================================

    /// <summary>
    /// Der Parametersatz, den <c>BlazorSeite&lt;Hauptfenster&gt;</c> unter
    /// Windows wirklich baut: die acht Gaben von
    /// <c>HauptfensterHuelle.Gaben()</c> PLUS den <see cref="SeitenZustand"/>,
    /// den die Huelle JEDEM Satz beilegt (<c>BlazorSeite.cs:93-96</c>).
    /// </summary>
    private static Dictionary<string, object> Huellengaben(
        SeitenZustand zustand,
        IReadOnlyDictionary<string, object>? startgaben = null) =>
        new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [SeitenZustand.PARAMETER] = zustand,
            ["Weg"] = new Func<string, string, Task<bool>>((_, _) => Task.FromResult(false)),
            ["Startansicht"] = Seitenschluessel.Startseite,
            ["StartseiteGaben"] = startgaben ?? Startgaben(),
            ["BerichteKostenGaben"] = Berichtegaben(),
            ["Produktname"] = "EPOS-Plan",
            ["Gattung"] = "Energieplanungs-Software",
            ["Claim"] = "Energie · Planung · Optimierung · Simulation",
            ["VersionText"] = "Version 1.0.0.0",
        };

    /// <summary>
    /// Rendert so, wie die Huelle es tut: EIN Woerterbuch auf die
    /// Wurzelkomponente — <c>RootComponents.Add&lt;T&gt;("#app", parameter)</c>
    /// entspricht <c>AddMultipleAttributes</c>. Der Unterschied zu
    /// <see cref="Fenster"/> (getippte Parameter) ist genau der, der den
    /// Startabsturz vom 04.09.2026 verdeckt hat: Ein Schluessel ohne
    /// <c>[Parameter]</c> faellt nur auf diesem Weg auf.
    /// </summary>
    private IRenderedComponent<Hauptfenster> AusHuelle(IDictionary<string, object> gaben)
    {
        return Render<Hauptfenster>(b =>
        {
            b.OpenComponent<Hauptfenster>(0);
            b.AddMultipleAttributes(1, gaben);
            b.CloseComponent();
        });
    }

    [Fact]
    public void Der_Parametersatz_der_Huelle_zeichnet_das_Fenster()
    {
        // BEFUND W16c-B12 (04.09.2026): BlazorSeite<T> traegt den Zustand IMMER
        // nach. Bis W16b war die Wurzel BlazorSeite<Startseite> und die
        // Startseite trug den Parameter; seit W16c ist es
        // BlazorSeite<Hauptfenster> — und Hauptfenster hatte ihn nicht. Blazor
        // warf beim ERSTEN Zeichnen "does not have a property matching the name
        // 'Zustand'", der Verteiler verpackte es, und der Anwender sah eine
        // TargetInvocationException an Application.Run.
        var zustand = new SeitenZustand();

        var cut = AusHuelle(Huellengaben(zustand));

        Assert.Single(cut.FindAll(".epos-menueband"));
        Assert.Single(cut.FindAll(".epos-hauptfenster-marke"));
        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Same(zustand, cut.Instance.Zustand);
    }

    [Fact]
    public void Der_Zustand_der_Startseitenhuelle_hat_Vorrang()
    {
        // Unter Windows fuehrt StartseiteHuelle ihren EIGENEN Zustand und meldet
        // darueber den Projektwechsel (ProjektKontextCtrl.Gewechselt). Der
        // Zustand der Seitenhuelle darf ihn nicht verdraengen - sonst gaebe es
        // zwei Zustaende fuer dieselbe Ansicht, und der Menueweg "Projekt
        // oeffnen" bliebe ohne Wirkung.
        var eigener = new SeitenZustand();
        var wurzel = new SeitenZustand();

        var cut = AusHuelle(Huellengaben(wurzel, Startgaben(eigener)));

        Assert.Same(eigener,
            cut.FindComponent<EPOS.UI.Seiten.Start.Startseite>().Instance.Zustand);
    }

    [Fact]
    public void Ohne_eigenen_Zustand_bekommt_die_Startseite_den_der_Huelle()
    {
        // Der iOS-Fall: IProjektQuelle.StartseiteGaben fuehrt keinen Zustand.
        // Dann legt AppWurzel den der Seitenhuelle bei, statt die Seite ohne
        // jeden Zustand zu lassen.
        var wurzel = new SeitenZustand();

        var cut = AusHuelle(Huellengaben(wurzel, Startgaben()));

        Assert.Same(wurzel,
            cut.FindComponent<EPOS.UI.Seiten.Start.Startseite>().Instance.Zustand);
    }

    [Fact]
    public void Ein_Projektwechsel_der_Huelle_erreicht_die_Startseite()
    {
        // Abnahmepunkt 9 der W16c-Liste: Nach einem Projektwechsel zeigt das
        // Kopfband der Startseite das neue Projekt. Der Weg ist
        // SeitenZustand.ProjektSetzen -> Startseite.BeiZustand -> Laden().
        var wurzel = new SeitenZustand();
        var namen = new List<(int Id, string Name)>();

        var cut = AusHuelle(Huellengaben(
            wurzel,
            Startgaben(varianten: () => namen)));

        Assert.DoesNotContain("Heizzentrale Nord",
            cut.Find(".epos-startseite-projekt").TextContent, StringComparison.Ordinal);

        namen.Add((1030, "Heizzentrale Nord"));
        wurzel.ProjektSetzen(1030, "Heizzentrale Nord");

        // BeiZustand zeichnet ueber InvokeAsync - also warten, nicht raten.
        cut.WaitForAssertion(() =>
            Assert.Contains("Heizzentrale Nord",
                cut.Find(".epos-startseite-projekt").TextContent, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(typeof(Hauptfenster))]
    [InlineData(typeof(EPOS.UI.Seiten.Start.Startseite))]
    [InlineData(typeof(EPOS.UI.Seiten.Berichte.BerichteKostenSeite))]
    public void Jede_Seite_einer_Seitenhuelle_traegt_den_Parameter_Zustand(Type seite)
    {
        // Dieselbe Bedingung, die BlazorSeite<T> beim Bauen prueft
        // (ZustandParameterPruefen) - hier auf der Linux-Seite festgehalten,
        // weil es fuer die Windows-Huelle kein Pruefprojekt gibt.
        System.Reflection.PropertyInfo? eigenschaft = seite.GetProperty(
            SeitenZustand.PARAMETER,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(eigenschaft);
        Assert.True(eigenschaft!.CanWrite);
        Assert.True(eigenschaft.IsDefined(
            typeof(Microsoft.AspNetCore.Components.ParameterAttribute), true));
        Assert.True(eigenschaft.PropertyType.IsAssignableFrom(typeof(SeitenZustand)));
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

    // =====================================================================
    //  BEFUND W16c-B13 (Windows-Abnahme 05.09.2026) — der Menueweg ueber
    //  drei Ebenen, gerendert wie die Huelle
    // =====================================================================

    [Fact]
    public void Der_Menueweg_ueber_drei_Ebenen_klappt_im_Fenster_der_Huelle_auf()
    {
        // Der Weg, den der Anwender ging: Administration ▸ "Waermebedarf &
        // Heizung ▸" - und dort blieb er stehen, weil die zweite Ebene sich
        // nicht aufklappen liess. Geprueft wird ueber AusHuelle, also mit dem
        // Parametersatz, den BlazorSeite<Hauptfenster> unter Windows uebergibt.
        var cut = AusHuelle(Huellengaben(new SeitenZustand()));

        cut.Find("#menue-Administration").Click();
        Assert.Equal("true", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("#menue-MenuItem_Brauchwasser"));

        cut.Find("#menue-MenuItem_WBundHeizung").Click();

        Assert.Equal("true", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll("#menue-MenuItem_Brauchwasser"));
        Assert.Single(cut.FindAll("#menue-MenuItem_WP"));

        // Das Kopfband und die Ansicht darunter stehen unveraendert - das
        // Menue legt sich darueber, es verschiebt nichts.
        Assert.Single(cut.FindAll(".epos-hauptfenster-marke"));
        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    [Fact]
    public async Task Ein_Punkt_der_dritten_Ebene_landet_im_selben_Handler()
    {
        // Der dreistufige Weg. Bis W16c-B13 war er unter Windows gar nicht
        // erreichbar; bis W16c-E-6 lautete er Administration ▸ Energiesysteme
        // ▸ Photovoltaik ▸ "Bearbeiten...". Genau dieses Untermenue mit dem
        // EINEN Punkt ist mit W16c-E-6 aufgeloest - die dritte Ebene fuehrt
        // jetzt Administration ▸ Waermebedarf & Heizung ▸ Profile &
        // Lastgaenge ▸ Waermebedarf Lastgang.
        string? gemeldet = null;
        var gaben = Huellengaben(new SeitenZustand());
        gaben["Weg"] = new Func<string, string, Task<bool>>(
            (ziel, _) => { gemeldet = ziel; return Task.FromResult(true); });

        var cut = AusHuelle(gaben);

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_WBundHeizung").Click();
        cut.Find("#menue-MenuItem_ProfileLastgaenge").Click();
        cut.Find("#menue-MenuItem_WaermebedarfExtern").Click();

        await Task.Yield();

        Assert.Equal(Seitenschluessel.WaermebedarfExternAdmin, gemeldet);

        // Nach der Wahl steht das Band zu - samt seiner Schliessflaeche.
        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));
    }

    [Fact]
    public void Der_Klick_neben_das_Menue_schliesst_es_im_Fenster()
    {
        // Der Ersatz fuer das gestrichene @onfocusout (W16c-B13): Solange ein
        // Menue offen ist, liegt ein durchsichtiger Deckel ueber der Ansicht.
        var cut = AusHuelle(Huellengaben(new SeitenZustand()));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_Energiesysteme").Click();
        Assert.Single(cut.FindAll(".epos-menueband-schliessflaeche"));

        cut.Find(".epos-menueband-schliessflaeche").Click();

        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));
        Assert.Single(cut.FindAll(".epos-menueband"));
    }
}
