using System;
using System.Collections.Generic;
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
            Titel = WindowsFormsApplication1.MyResource.Resource.START_K_DETAILSIM_T
        }
    };

    private SimulationErgebnisDienste Ergebnisdienste() => new SimulationErgebnisDienste
    {
        Laden = _ => new SimulationErgebnisDaten
        {
            IdProjekt = 1030,
            ErgebnisGueltig = _ergebnisGueltig,
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

    /// <summary>Die eine Bildkachel des Reiters — „Simulation starten".</summary>
    private static IElement Kachel(IRenderedComponent<Startseite> cut)
        => cut.Find(".epos-simreiter-links .epos-kachel");

    private static IElement Speichern(IRenderedComponent<Startseite> cut)
        => cut.Find("button.epos-simreiter-speichern");

    // =====================================================================
    //  1 — Die Kachel rechnet an Ort und Stelle
    // =====================================================================

    /// <summary>
    /// <b>SIM‑E‑2, Punkt 1:</b> Der Klick auf die Kachel startet den Lauf IM REITER —
    /// der Laufdelegat wird gerufen, und es wird keine Ansicht gewechselt.
    /// </summary>
    [Fact]
    public void Die_Kachel_startet_den_Lauf_im_Reiter_ohne_Ansichtswechsel()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Kachel(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        Assert.Empty(Navigation.Masken);
        Assert.Empty(_gemeldet);
    }

    /// <summary>
    /// Der SPERRGRUND steht AN der Kachel und als Hinweis darunter (die weiche Hälfte
    /// der Regel W16b‑E‑6) — und die Kachel rechnet dann nicht.
    /// </summary>
    [Fact]
    public void Ein_Sperrgrund_steht_an_der_Kachel_und_als_Hinweis_darunter()
    {
        const string grund = "Die Datenbank ist blockiert.";
        var cut = Zeigen(Dienste(sperrgrund: grund));
        ReiterSimulation(cut);

        Assert.True(Kachel(cut).HasAttribute("disabled"));
        Assert.Contains(grund, Kachel(cut).TextContent);
        Assert.Contains(grund,
                        cut.Find(".epos-simreiter-links .epos-warnbanner").TextContent);

        Kachel(cut).Click();
        Assert.Equal(0, _laeufe);
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

        Kachel(cut).Click();
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
    /// </summary>
    [Fact]
    public void Ohne_Ergebnis_steht_rechts_der_Hinweis_und_keine_Ergebnisseite()
    {
        var cut = Zeigen(Dienste());
        ReiterSimulation(cut);

        Assert.Contains("Noch kein Ergebnis", cut.Find(".epos-simreiter-leer").TextContent);
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

        Kachel(cut).Click();
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

        Kachel(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));

        // Waehrend des Laufs bleibt er gesperrt.
        Assert.True(Speichern(cut).HasAttribute("disabled"));

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.False(Speichern(cut).HasAttribute("disabled")));

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
        Kachel(cut).Click();
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
        Kachel(reiter).Click();
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
    /// Und umgekehrt: Hält ein FREMDER Wirt die Sperre, ist die Kachel des Reiters
    /// gesperrt und nennt den Grund.
    /// </summary>
    [Fact]
    public void Ein_fremder_Lauf_sperrt_die_Kachel_des_Reiters()
    {
        var sperre = new SimulationLaufsperre();
        Assert.True(sperre.Anmelden(new object()));

        var cut = Zeigen(Dienste(sperre: sperre));
        ReiterSimulation(cut);

        Assert.True(Kachel(cut).HasAttribute("disabled"));
        Assert.Contains("an anderer Stelle",
                        cut.Find(".epos-simreiter-links .epos-warnbanner").TextContent);

        Kachel(cut).Click();
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
        cut.Find(".epos-simreiter-links .epos-kachel").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        Assert.Empty(Navigation.Masken);
    }

    // =====================================================================
    //  Der Rueckfall: OHNE Dienste bleibt der Reiter der von vor #220
    // =====================================================================

    /// <summary>
    /// Ohne Parametersatz gibt es keine rechte Spalte, und die Kachel meldet ihren
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

        Kachel(cut).Click();

        Assert.Equal(0, _laeufe);
        Assert.Equal(new[] { Masken.Simulation }, Navigation.Masken);
        Assert.Equal(SimulationMarke.SCHRITT_LAUF, Navigation.LetzteArgumente[0]);
    }
}
