using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>DER STARTSEITEN-REITER „SIMULATION" RECHNET</b> (Auftrag <b>#220</b>,
/// Anwenderentscheid <b>SIM‑E‑2</b> vom 11.09.2026, Option 1).
///
/// <para><b>Der Befund.</b> Der Anwender hat den Reiter an drei Bildschirmfotos
/// zurückgegeben: links Zusammenfassung, Konfigurationsknopf und die Kachel
/// „Simulation starten", rechts eine leere halbe Seite — und die Kachel wechselte
/// über die Marke <c>schritt=2</c> in die Ansicht SIMULATION (SIM‑E‑1, #216).</para>
///
/// <para><b>Das Soll.</b> (1) Die Kachel RECHNET an Ort und Stelle, mit
/// Fortschrittsbalken, Abbrechen und den Sperrgründen von Schritt ②; kein
/// Ansichtswechsel. (2) Rechts steht dieselbe Ergebniskomponente wie Schritt ③,
/// „Ergebnis speichern" im Kopf der Spalte, ohne Lauf ein Hinweis. (3) Die Dienste
/// kommen aus DERSELBEN Quelle wie die Ansicht (<c>SimulationGaben</c>) — damit auch
/// auf iOS. (4) Der Rückweg aus der Stromspeicher-Auslegung führt in den REITER
/// zurück (Marke mit Wirtkennung <c>wirt=START</c>). (5) Ein Lauf zur Zeit: Ansicht
/// und Reiter sperren sich über die gemeinsame <see cref="SimulationLaufsperre"/>.</para>
///
/// <para><b>SEIT AUFTRAG #233</b> (Anwenderrückmeldung 12.09.2026, Bildschirmfoto)
/// ist die linke Spalte ein BEDIENBLOCK fester Breite: Die Kachel „Simulation
/// starten" ist ein Hauptknopf in Blockbreite geworden
/// (<c>.epos-simreiter-hauptknopf</c>), ihre Erläuterung eine leise Zeile darunter,
/// der Sperrgrund eine zweite (<c>.epos-simreiter-sperrgrund</c>) statt eines
/// Warnbanners am Fuß der Spalte; rechts ist aus der einsamen Hinweiszeile eine
/// Karte geworden, und „Ergebnis speichern" nennt seinen Sperrgrund im
/// <c>title</c>. Der WEG bleibt in allem derselbe — geprüft wird hier die
/// Bedienung, nicht die Bauform.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (Hausregel seit iU9‑W8). Die Fälle laufen in
/// der SERIELLEN Sammlung <c>KiDialogweg</c>: Sie tauschen <c>Dienste.Navigation</c>
/// und zeichnen eine <c>AppWurzel</c>, und beides ist prozessweiter Zustand.</para>
/// </summary>
[Collection("KiDialogweg")]
public class StartreiterSimulationTests : EposBunitContext
{
    private readonly INavigation _vorherigeNavigation;

    public StartreiterSimulationTests()
    {
        _vorherigeNavigation = WindowsFormsApplication1.Dienste.Navigation;
        Navigation = new TestNavigation();
        WindowsFormsApplication1.Dienste.Navigation = Navigation;

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Mitschrift der Maskenaufrufe — sie muss leer bleiben.</summary>
    private TestNavigation Navigation { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WindowsFormsApplication1.Dienste.Navigation = _vorherigeNavigation;
            Navigationsziel.Aktuell = null;
        }

        base.Dispose(disposing);
    }

    // =====================================================================
    //  Probendaten
    // =====================================================================

    private int _laeufe;
    private int _abbrueche;
    private int _gespeichert;
    private bool _laufGerechnet;
    private bool _ergebnisGueltig;

    /// <summary>Ein ausdrücklich gesetzter Ergebniszustand (Auftrag #236); <c>null</c> = aus der Marke.</summary>
    private ErgebnisZustand? _zustand;
    private string _zustandsgrund = "";
    private TaskCompletionSource<Rueckmeldung>? _laufFertig;

    private readonly List<string> _gemeldet = new();

    private static IReadOnlyList<StartKachel> Kacheln() => new[]
    {
        new StartKachel
        {
            Schluessel = Kachelschluessel.SimulationKonfiguration,
            Reiter = Reiterschluessel.Simulation,
            Titel = "Simulation Konfiguration..."
        },
        new StartKachel
        {
            Schluessel = Kachelschluessel.SimulationErgebnis,
            Reiter = Reiterschluessel.Simulation,
            Titel = WindowsFormsApplication1.MyResource.Resource.START_K_DETAILSIM_T,
            Beschreibung = WindowsFormsApplication1.MyResource.Resource.START_K_DETAILSIM_B
        }
    };

    private SimulationErgebnisDienste Ergebnisdienste() => new SimulationErgebnisDienste
    {
        Laden = _ => new SimulationErgebnisDaten
        {
            IdProjekt = 1030,
            // Der ZUSTAND ist seit #236 die Wahrheit; ohne eigenen Wert folgt er der
            // alten Marke (gültig / noch nicht gerechnet).
            Zustand = _zustand ?? (_ergebnisGueltig
                                       ? ErgebnisZustand.Gueltig
                                       : ErgebnisZustand.NichtGerechnet),
            Zustandsgrund = _zustandsgrund,
            Bedarf = new BedarfDaten { StrombedarfGesamtMwh = 2850.2 },
            ReiterStromspeicher = true
        },
        Bild = _ => null,
        Laufen = melder =>
        {
            _laeufe++;
            // Der Lauf ist gerechnet, sobald er FERTIG ist - gelesen wird das erst
            // beim Neuladen nach dem await.
            _laufGerechnet = true;
            _ergebnisGueltig = true;
            _laufFertig = new TaskCompletionSource<Rueckmeldung>();
            return _laufFertig.Task;
        },
        Abbrechen = () => _abbrueche++,
        Speichern = () => { _gespeichert++; return new Rueckmeldung(true, "gespeichert"); }
    };

    private SimulationAnsichtDienste Dienste(string sperrgrund = "",
                                             SimulationLaufsperre? sperre = null)
        => new SimulationAnsichtDienste
        {
            Konfiguration = new Dictionary<string, object>
            {
                ["Dienste"] = new SimulationKonfigDienste { Laden = _ => new SimulationKonfigDaten() },
                ["StartProjekt"] = 1030
            },
            Ergebnis = new Dictionary<string, object>
            {
                ["Dienste"] = Ergebnisdienste(),
                ["StartProjekt"] = 1030
            },
            Sperrgrund = () => sperrgrund,
            ErgebnisVorhanden = () => _laufGerechnet,
            Laufsperre = sperre
        };

    private static IReadOnlyDictionary<string, object> Gaben(SimulationAnsichtDienste dienste)
        => new Dictionary<string, object>
        {
            ["Dienste"] = dienste,
            ["ProjektText"] = "Projekt „B3-Kaskade“"
        };

    /// <summary>Der Parametersatz der Startseite, wie ihn die Hülle liefert.</summary>
    private IReadOnlyDictionary<string, object> Startgaben(SimulationAnsichtDienste? dienste)
    {
        var satz = new Dictionary<string, object>
        {
            ["Kacheln"] = (Func<IReadOnlyList<StartKachel>>)(() => Kacheln()),
            ["ProjektId"] = (Func<int>)(() => 1030),
            ["Bericht"] = (Func<Zusammenfassung?>)(() =>
                new Zusammenfassung("B3-Kaskade", "480 MWh/a", "120 MWh/a", "WP")),
            ["Geklickt"] = (Action<string>)(s => _gemeldet.Add(s))
        };

        return satz;
    }

    private IRenderedComponent<Startseite> Zeigen(SimulationAnsichtDienste? dienste,
                                                  string marke = "")
        => Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln())
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Bericht, () => new Zusammenfassung("B3-Kaskade", "480 MWh/a", "120 MWh/a", "WP"))
            .Add(x => x.Geklickt, s => _gemeldet.Add(s))
            .Add(x => x.SimulationGaben, () => dienste is null ? null : Gaben(dienste))
            .Add(x => x.Marke, marke));

    // ---- Lesehilfen ------------------------------------------------------

    /// <summary>Auf den fünften Reiter („Simulation") wechseln.</summary>
    private static void ReiterSimulation(IRenderedComponent<Startseite> cut)
        => cut.FindAll(".epos-startseite > .epos-reiter > .epos-reiter-leiste [role='tab']")[4].Click();

    /// <summary>
    /// Der HAUPTKNOPF des Bedienblocks — „Simulation starten" (seit #233; bis dahin
    /// die einzige Bildkachel des Reiters).
    /// </summary>
    private static IElement Hauptknopf(IRenderedComponent<Startseite> cut)
        => cut.Find(".epos-simreiter-links .epos-simreiter-hauptknopf");

    private static IElement Speichern(IRenderedComponent<Startseite> cut)
        => cut.Find("button.epos-simreiter-speichern");

    // =====================================================================
    //  1 — Die Kachel rechnet an Ort und Stelle
    // =====================================================================

    /// <summary>
    /// <b>SIM‑E‑2, Punkt 1:</b> Der Klick auf den Hauptknopf startet den Lauf IM
    /// REITER — der Laufdelegat wird gerufen, und es wird keine Ansicht gewechselt.
    /// </summary>
    [Fact]
    public void Der_Hauptknopf_startet_den_Lauf_im_Reiter_ohne_Ansichtswechsel()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Hauptknopf(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        Assert.Empty(Navigation.Masken);
        Assert.Empty(_gemeldet);
    }

    /// <summary>
    /// Der SPERRGRUND sperrt den Hauptknopf, steht in seinem <c>title</c> und als
    /// ZEILE darunter (Auftrag #233; bis dahin an der Kachel und in einem Warnbanner
    /// am Fuß der Spalte — dasselbe zweimal). Die weiche Hälfte der Regel
    /// W16b‑E‑6 bleibt: Der Grund steht da, wo der gesperrte Knopf steht.
    /// </summary>
    [Fact]
    public void Ein_Sperrgrund_sperrt_den_Hauptknopf_und_steht_als_Zeile_darunter()
    {
        const string grund = "Die Datenbank ist blockiert.";
        var cut = Zeigen(Dienste(sperrgrund: grund));
        ReiterSimulation(cut);

        Assert.True(Hauptknopf(cut).HasAttribute("disabled"));
        Assert.Equal(grund, Hauptknopf(cut).GetAttribute("title"));
        Assert.Equal(grund, cut.Find(".epos-simreiter-sperrgrund").TextContent.Trim());

        // Das Banner am Fuss der Spalte ist damit fort.
        Assert.Empty(cut.FindAll(".epos-simreiter-links .epos-warnbanner"));

        Hauptknopf(cut).Click();
        Assert.Equal(0, _laeufe);
    }

    /// <summary>
    /// OHNE Sperrgrund steht weder die Zeile noch ein <c>title</c> am Knopf — ein
    /// leerer Sprechblasentext ist schlimmer als keiner.
    /// </summary>
    [Fact]
    public void Ohne_Sperrgrund_steht_weder_Zeile_noch_Sprechblase()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Assert.False(Hauptknopf(cut).HasAttribute("disabled"));
        Assert.False(Hauptknopf(cut).HasAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-simreiter-sperrgrund"));
    }

    /// <summary>
    /// Während des Laufs steht der <c>Fortschritt</c> in der LINKEN Spalte — dort, wo
    /// geklickt wurde —, und sein Abbrechen geht den Weg der Ergebnisseite. GENAU
    /// EINER: Der Balken der Ergebnisseite ist dafür abgeschaltet.
    /// </summary>
    [Fact]
    public void Waehrend_des_Laufs_steht_ein_Fortschritt_mit_Abbrechen_in_der_linken_Spalte()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Hauptknopf(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-fortschritt")));
        Assert.Single(cut.FindAll(".epos-simreiter-links .epos-fortschritt"));

        cut.Find("button.epos-fortschritt-abbruch").Click();
        Assert.Equal(1, _abbrueche);

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-fortschritt")));
    }

    // =====================================================================
    //  2 — Rechts das Ergebnis
    // =====================================================================

    /// <summary>
    /// <b>SIM‑E‑2, Punkt 2:</b> Ohne gerechneten Lauf steht rechts ein HINWEIS statt
    /// einer Leerfläche — und die Ergebniskomponente ist noch gar nicht aufgebaut.
    ///
    /// <para>Seit Auftrag <b>#233</b> ist der Hinweis eine KARTE mit kleinem Sinnbild
    /// und einem Satz, keine einsame Textzeile in einer 1 200 px breiten Fläche.</para>
    /// </summary>
    [Fact]
    public void Ohne_Ergebnis_steht_rechts_die_Leerkarte_und_keine_Ergebnisseite()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        IElement karte = cut.Find(".epos-simreiter-rechts .epos-simreiter-leer");
        Assert.Contains("Noch kein Ergebnis", karte.TextContent);
        Assert.Single(cut.FindAll(".epos-simreiter-leer .epos-simreiter-leertext"));

        // Das Sinnbild der gefallenen Kachel traegt jetzt die Karte.
        IElement bild = cut.Find(".epos-simreiter-leer img");
        Assert.Equal("_content/EPOS.UI/bilder/start/PDetailSim.jpg", bild.GetAttribute("src"));
        Assert.Contains("epos-simreiter-leerbild", bild.ClassName);

        Assert.Empty(cut.FindAll(".epos-simerg"));
    }

    /// <summary>
    /// Nach dem Lauf zeigt die rechte Spalte die ÜBERSICHT — das Startblatt der
    /// Ergebnisseite seit #216, seit #222 als Dashboard Wärme | Strom.
    /// </summary>
    [Fact]
    public void Nach_dem_Lauf_zeigt_die_rechte_Spalte_die_Uebersicht()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Hauptknopf(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));
        Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simueb"));
        Assert.Empty(cut.FindAll(".epos-simreiter-leer"));
    }

    /// <summary>
    /// „Ergebnis speichern" steht im KOPF der rechten Spalte und ist nur nach einem
    /// vollständigen Lauf frei — dieselbe Bedingung wie in der Werkzeugleiste der
    /// Ansicht (<c>SpeichernMoeglich</c>).
    /// </summary>
    [Fact]
    public void Ergebnis_speichern_ist_nur_nach_einem_vollstaendigen_Lauf_frei()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Assert.Equal("Ergebnis speichern", Speichern(cut).TextContent.Trim());
        Assert.True(Speichern(cut).HasAttribute("disabled"));

        // Und er sagt WARUM (Auftrag #233) - vorher stand er stumm ueber einer
        // leeren Flaeche und sah aus wie ein bedienbarer Knopf.
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.START_SIM_KEIN_ERGEBNIS,
                     Speichern(cut).GetAttribute("title"));

        Hauptknopf(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));

        // Waehrend des Laufs bleibt er gesperrt.
        Assert.True(Speichern(cut).HasAttribute("disabled"));

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.False(Speichern(cut).HasAttribute("disabled")));

        // Frei heisst auch: keine Sprechblase mehr.
        Assert.False(Speichern(cut).HasAttribute("title"));

        Speichern(cut).Click();
        Assert.Equal(1, _gespeichert);
    }

    // =====================================================================
    //  4 — Die Marke mit Wirtkennung (der Rueckweg aus der Auslegung)
    // =====================================================================

    /// <summary>
    /// Die MARKE des Reiters trägt die Wirtkennung <c>wirt=START</c> — daran
    /// unterscheidet der Rückwegstapel den Reiter von der Ansicht.
    /// </summary>
    [Fact]
    public void Der_stehende_Reiter_liefert_eine_Marke_mit_Wirtkennung()
    {
        var cut = Zeigen(Dienste());

        // Auf Reiter 1 fuehrt die Startseite keine Marke.
        Assert.Equal("", cut.Instance.AktuelleMarke);

        ReiterSimulation(cut);
        Hauptknopf(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));

        string marke = cut.Instance.AktuelleMarke;
        Assert.Equal(SimulationMarke.WIRT_START, SimulationMarke.WirtLesen(marke));
        Assert.Equal(3, SimulationMarke.Lesen(marke).Schritt);
        Assert.Equal(SimulationErgebnisSeite.Blatt.Uebersicht, SimulationMarke.Lesen(marke).Blatt);
    }

    /// <summary>
    /// Und dieselbe Marke stellt Reiter und Reiterblatt wieder her: Der Rückweg aus
    /// der Stromspeicher-Auslegung landet auf dem Reiter „Simulation" mit dem Blatt
    /// „Stromspeicher" vorn.
    /// </summary>
    [Fact]
    public void Die_Marke_stellt_den_Reiter_samt_Reiterblatt_wieder_her()
    {
        _laufGerechnet = true;
        _ergebnisGueltig = true;

        string marke = SimulationMarke.Schreiben(SimulationMarke.WIRT_START, 3,
                                                 SimulationErgebnisSeite.Blatt.Stromspeicher);

        var cut = Zeigen(Dienste(), marke);

        cut.WaitForAssertion(() => Assert.Equal(Reiterschluessel.Simulation, cut.Instance.AktiverReiter));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));

        SimulationReiter reiter = cut.FindComponent<SimulationReiter>().Instance;
        cut.WaitForAssertion(() =>
            Assert.Equal(SimulationErgebnisSeite.Blatt.Stromspeicher, reiter.AktivesBlatt));
    }

    /// <summary>
    /// Eine Marke OHNE Wirtkennung meint die ANSICHT und geht die Startseite nichts an
    /// — sie bleibt auf ihrem ersten Reiter.
    /// </summary>
    [Fact]
    public void Eine_Marke_ohne_Wirtkennung_laesst_die_Startseite_in_Ruhe()
    {
        _laufGerechnet = true;

        var cut = Zeigen(Dienste(), SimulationMarke.SCHRITT_ERGEBNIS);

        Assert.Equal(Reiterschluessel.Projekt, cut.Instance.AktiverReiter);
    }

    /// <summary>
    /// DER GANZE WEG (SIM‑E‑2, Punkt 4): Aus dem Reiter heraus wird die
    /// Stromspeicher-Auslegung geöffnet — und ihr „← zurück" führt in den REITER
    /// zurück, nicht auf den ersten Reiter der Startseite.
    /// </summary>
    [Fact]
    public void Der_Rueckweg_aus_der_Auslegung_kehrt_in_den_Reiter_zurueck()
    {
        _laufGerechnet = true;
        _ergebnisGueltig = true;

        SimulationAnsichtDienste dienste = Dienste();
        var quelle = new TestProjektquelle
        {
            Startseite = Startgaben(dienste),
            Simulation = Gaben(dienste),
            Auslegung = new Dictionary<string, object>
            {
                ["Dienste"] = new EPOS.UI.Seiten.Strom.StromspeicherAuslegungDienste()
            }
        };
        Services.AddSingleton<IProjektQuelle>(quelle);

        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Startansicht, Seitenschluessel.Startseite)
            .Add(x => x.SimulationGaben, () => Gaben(dienste)));

        cut.WaitForElement(".epos-startseite");
        cut.FindAll(".epos-startseite > .epos-reiter > .epos-reiter-leiste [role='tab']")[4].Click();
        cut.WaitForElement(".epos-simreiter-rechts .epos-simerg");

        // Der Weg in die Auslegung - genau der, den der Knopf des
        // Stromspeicher-Blatts geht (SimulationErgebnisHuelle.AuslegungOeffnen).
        Assert.True(Navigationsziel.Aktuell?.OeffneMaske(Seitenschluessel.StromspeicherAuslegung));
        cut.WaitForElement(".epos-spauslegung");

        // „← zurueck" der Auslegung.
        cut.FindAll(".epos-spauslegung-kopfaktionen button.epos-knopf").Last().Click();

        cut.WaitForElement(".epos-startseite");
        cut.WaitForAssertion(() =>
            Assert.Equal(Reiterschluessel.Simulation,
                         cut.FindComponent<Startseite>().Instance.AktiverReiter));
    }

    // =====================================================================
    //  5 — Ein Lauf zur Zeit
    // =====================================================================

    /// <summary>
    /// <b>SIM‑E‑2, Punkt 5:</b> Läuft der Lauf im REITER, ist Schritt ② der ANSICHT
    /// gesperrt — beide teilen sich die <see cref="SimulationLaufsperre"/> der Quelle.
    /// </summary>
    [Fact]
    public void Ein_Lauf_im_Reiter_sperrt_Schritt_zwei_der_Ansicht()
    {
        var sperre = new SimulationLaufsperre();

        var reiter = Zeigen(Dienste(sperre: sperre));
        ReiterSimulation(reiter);
        Hauptknopf(reiter).Click();
        reiter.WaitForAssertion(() => Assert.Equal(1, _laeufe));

        // DIESELBE Sperre in einem zweiten Parametersatz - genau das tut
        // SimulationAnsichtQuelle.AnsichtGaben bei jedem Aufruf.
        var ansicht = Render<SimulationSeite>(p => p
            .Add(x => x.Dienste, Dienste(sperre: sperre)));

        IElement rechnen = ansicht.Find("button.epos-ablaufleiste-rechnen");
        Assert.True(rechnen.HasAttribute("disabled"));
        Assert.Contains("an anderer Stelle", ansicht.Find(".epos-warnbanner").TextContent);

        // Ist der Lauf zu Ende, gibt er die Sperre frei.
        reiter.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        reiter.WaitForAssertion(() => Assert.Empty(reiter.FindAll(".epos-fortschritt")));

        ansicht.Render();
        Assert.False(ansicht.Find("button.epos-ablaufleiste-rechnen").HasAttribute("disabled"));
    }

    /// <summary>
    /// Und umgekehrt: Hält ein FREMDER Wirt die Sperre, ist der Hauptknopf des
    /// Reiters gesperrt und nennt den Grund.
    /// </summary>
    [Fact]
    public void Ein_fremder_Lauf_sperrt_den_Hauptknopf_des_Reiters()
    {
        var sperre = new SimulationLaufsperre();
        Assert.True(sperre.Anmelden(new object()));

        var cut = Zeigen(Dienste(sperre: sperre));
        ReiterSimulation(cut);

        Assert.True(Hauptknopf(cut).HasAttribute("disabled"));
        Assert.Contains("an anderer Stelle",
                        cut.Find(".epos-simreiter-sperrgrund").TextContent);

        Hauptknopf(cut).Click();
        Assert.Equal(0, _laeufe);
    }

    // =====================================================================
    //  3 — Dienste aus EINER Quelle (der iOS-Fall)
    // =====================================================================

    /// <summary>
    /// <b>SIM‑E‑2, Punkt 3:</b> Ohne Hüllen-Delegat — der iOS-Fall — holt die Wurzel
    /// denselben Parametersatz aus <c>IProjektQuelle.SimulationGaben</c>, und der
    /// Reiter rechnet damit auf dem iPad genauso.
    /// </summary>
    [Fact]
    public void Ohne_Huellen_Delegat_liefert_die_Projektquelle_die_Dienste()
    {
        SimulationAnsichtDienste dienste = Dienste();
        var quelle = new TestProjektquelle
        {
            Startseite = Startgaben(dienste),
            Simulation = Gaben(dienste)
        };
        Services.AddSingleton<IProjektQuelle>(quelle);

        // KEIN SimulationGaben-Parameter: genau die iOS-Lage.
        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Startansicht, Seitenschluessel.Startseite));

        cut.WaitForElement(".epos-startseite");
        cut.FindAll(".epos-startseite > .epos-reiter > .epos-reiter-leiste [role='tab']")[4].Click();

        // Die rechte Spalte steht - also sind die Dienste angekommen.
        cut.WaitForElement(".epos-simreiter-rechts");
        cut.Find(".epos-simreiter-links .epos-simreiter-hauptknopf").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        Assert.Empty(Navigation.Masken);
    }

    // =====================================================================
    //  Der Rueckfall: OHNE Dienste bleibt der Reiter der von vor #220
    // =====================================================================

    /// <summary>
    /// Ohne Parametersatz gibt es keine rechte Spalte, und der Hauptknopf meldet seinen
    /// Schlüssel wie vor #220 — der Fall „kein Projekt offen" und der Fall eines
    /// Prüfstands. Dieselbe Hausregel wie überall: kein Delegat, keine Bedienung.
    /// </summary>
    [Fact]
    public void Ohne_Dienste_bleibt_der_Reiter_der_von_vor_220()
    {
        var cut = Zeigen(null);
        ReiterSimulation(cut);

        Assert.Empty(cut.FindAll(".epos-simreiter-rechts"));
        Assert.Single(cut.FindAll(".epos-simreiter--allein"));

        Hauptknopf(cut).Click();

        Assert.Equal(0, _laeufe);
        Assert.Equal(new[] { Masken.Simulation }, Navigation.Masken);
        Assert.Equal(SimulationMarke.SCHRITT_LAUF, Navigation.LetzteArgumente[0]);
    }

    // =====================================================================
    //  Auftrag #233 - der Bedienblock statt des Kachelrasters
    // =====================================================================

    /// <summary>
    /// <b>Die Regel des Auftrags:</b> Ein Kachelraster gehört in einen Reiter mit DREI
    /// Spalten (W16b‑E‑7); dieser Reiter hat zwei und zeichnet deshalb einen
    /// BEDIENBLOCK. Weder Raster noch Kachel stehen noch darin.
    /// </summary>
    [Fact]
    public void Der_Reiter_zeichnet_kein_Kachelraster_mehr()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Assert.Empty(cut.FindAll(".epos-simreiter-links .epos-kachelraster"));
        Assert.Empty(cut.FindAll(".epos-simreiter-links .epos-kachel"));
        Assert.Single(cut.FindAll(".epos-simreiter-links .epos-simreiter-hauptknopf"));
    }

    /// <summary>
    /// DIE TEXTE BLEIBEN DIE DER KACHEL: Beschriftung und Erläuterung des Hauptknopfes
    /// kommen weiter aus dem Kachelregister der Hülle — nur die Bauform ist eine
    /// andere. Deshalb fällt auch kein Kachelschlüssel weg.
    /// </summary>
    [Fact]
    public void Der_Hauptknopf_traegt_Titel_und_Erlaeuterung_des_Kachelregisters()
    {
        var cut = Render<SimulationReiter>(p => p
            .Add(x => x.Kacheln, new[]
            {
                new StartKachel
                {
                    Schluessel = Kachelschluessel.SimulationErgebnis,
                    Reiter = Reiterschluessel.Simulation,
                    Titel = "Simulation starten",
                    Beschreibung = "Präzise Jahressimulation mit allen Details"
                }
            }));

        Assert.Contains("Simulation starten",
                        cut.Find(".epos-simreiter-hauptknopf").TextContent);
        Assert.Equal("Präzise Jahressimulation mit allen Details",
                     cut.Find(".epos-simreiter-erklaerung").TextContent.Trim());
    }

    /// <summary>
    /// Und ohne Kachelregister — ein Prüfstand, eine Plattform ohne Hülle — stehen die
    /// Texte aus den Ressourcen: Der Block ist die Bauform des Reiters und hängt nicht
    /// daran, ob jemand ihm Kacheln reicht.
    /// </summary>
    [Fact]
    public void Ohne_Kachelregister_stehen_die_Texte_aus_den_Ressourcen()
    {
        var cut = Render<SimulationReiter>(p => p
            .Add(x => x.Kacheln, Array.Empty<StartKachel>()));

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.START_K_DETAILSIM_T,
                        cut.Find(".epos-simreiter-hauptknopf").TextContent);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.START_K_DETAILSIM_B,
                     cut.Find(".epos-simreiter-erklaerung").TextContent.Trim());
    }

    /// <summary>
    /// Der ZWEITKNOPF geht unverändert seinen Weg: Er meldet den Schlüssel
    /// <c>SIMULATION_KONFIGURATION</c> nach oben (die Startseite wechselt damit in die
    /// Ansicht, Schritt ①). Geändert hat sich allein seine Breite.
    /// </summary>
    [Fact]
    public void Der_Zweitknopf_meldet_weiter_den_Konfigurationsschluessel()
    {
        List<string> gemeldet = new List<string>();

        var cut = Render<SimulationReiter>(p => p
            .Add(x => x.Kacheln, Kacheln())
            .Add(x => x.Geklickt, (string s) => gemeldet.Add(s)));

        cut.Find(".epos-startreiter-leiste .epos-simreiter-konfig").Click();

        Assert.Equal(new[] { Kachelschluessel.SimulationKonfiguration }, gemeldet);
    }
    // =====================================================================
    //  7 — Der VERALTETE Lauf (Auftrag #236, Anwenderrückmeldung 12.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>Der Befund #236.</b> Der Anwender kam aus der Stromspeicher-Auslegung
    /// desselben Projekts zurück. <c>LaufGerechnet</c> bleibt dabei wahr — die rechte
    /// Spalte steht also —, aber das Ergebnis ist VERALTET. Bis #236 zeigte die
    /// Übersicht dort ein Nullobjekt („Strombedarf 0,00 MWh/a", Marke „kein
    /// Stromerzeuger im Projekt"), während links 2 850,20 MWh/a standen. Jetzt steht
    /// der Zustand SICHTBAR im Kopf der Spalte, und die Übersicht zeigt die
    /// Bedarfszahl.
    /// </summary>
    [Fact]
    public void Ein_veraltetes_Ergebnis_steht_sichtbar_im_Kopf_der_rechten_Spalte()
    {
        _laufGerechnet = true;
        _zustand = ErgebnisZustand.Veraltet;
        _zustandsgrund = "die Speicher-Einstellungen wurden geändert";

        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        // Die Spalte steht (der Kommentar an ErgebnisVorhanden will das) …
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));

        // … und nennt den Zustand als leise Zeile, nicht als Banner.
        var zeile = cut.Find(".epos-simreiter-rechtskopf p.epos-simreiter-zustand");
        Assert.Contains(_zustandsgrund, zeile.TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".epos-simreiter-rechts .epos-warnbanner"));

        // Die Übersicht zeigt die Bedarfszahl — kein „0,00" aus einem Nullobjekt.
        Assert.Contains((2850.2).ToString("N2", CultureInfo.CurrentCulture),
                        cut.Find(".epos-simreiter-rechts .epos-simueb").TextContent,
                        StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".epos-simreiter-rechts ul.epos-simueb-legende"));
    }

    /// <summary>
    /// „Ergebnis speichern" bleibt bei einem veralteten Lauf GESPERRT — und sein
    /// <c>title</c> nennt seit #236 den Zustand statt nur „Noch kein Ergebnis".
    /// </summary>
    [Fact]
    public void Bei_veraltetem_Ergebnis_nennt_der_Speicherknopf_den_Grund()
    {
        _laufGerechnet = true;
        _zustand = ErgebnisZustand.Veraltet;
        _zustandsgrund = "die Speicherflotte wurde neu gerechnet";

        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));

        IElement knopf = Speichern(cut);
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains(_zustandsgrund, knopf.GetAttribute("title") ?? "", StringComparison.Ordinal);
        Assert.Equal(0, _gespeichert);
    }

    /// <summary>
    /// Ein ABGEBROCHENER Lauf ist derselbe Fall mit einem anderen Satz: Die Spalte
    /// steht, der Grund steht da, gespeichert wird nicht.
    /// </summary>
    [Fact]
    public void Ein_abgebrochener_Lauf_nennt_seinen_Abbruchgrund()
    {
        _laufGerechnet = true;
        _zustand = ErgebnisZustand.Abgebrochen;
        _zustandsgrund = "Die Klimaregion fehlt.";

        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));

        Assert.Contains(_zustandsgrund,
                        cut.Find(".epos-simreiter-rechtskopf p.epos-simreiter-zustand").TextContent,
                        StringComparison.Ordinal);
        Assert.True(Speichern(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Nach einem gültigen Lauf gibt es keine Zustandszeile, und
    /// „Ergebnis speichern" ist frei.
    /// </summary>
    [Fact]
    public void Ein_gueltiges_Ergebnis_traegt_keine_Zustandszeile()
    {
        _laufGerechnet = true;
        _zustand = ErgebnisZustand.Gueltig;

        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-simreiter-rechts .epos-simerg")));

        Assert.Empty(cut.FindAll(".epos-simreiter-rechtskopf p.epos-simreiter-zustand"));
        cut.WaitForAssertion(() => Assert.False(Speichern(cut).HasAttribute("disabled")));
    }
}
