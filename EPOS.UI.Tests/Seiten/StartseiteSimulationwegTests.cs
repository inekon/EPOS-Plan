using System;
using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der WEG der Startseite in die Simulation (Auftrag <b>#207</b>, Stufe S1).
///
/// <para><b>Was sich geändert hat.</b> Bis #207 löste der Knopf „Simulation
/// Konfiguration…" die Startseite INNERHALB ihrer eigenen Komponente ab, und die
/// Kachel „Simulation" zog eine breite <c>Ueberlagerung</c> über sie (Entscheid
/// E‑5). Seither meldet die Seite nur noch den WEG: Beide gehen über
/// <c>Dienste.Navigation</c> auf die freie Ansicht <c>SIMULATION</c>. Das ist auf
/// BEIDEN Plattformen derselbe Weg (Konzept „Simulationsablauf" 2).</para>
///
/// <para><b>Seit dem Anwenderentscheid SIM‑E‑1</b> (Windows-Abnahme #216 vom
/// 11.09.2026, Punkt 1: „Belege den Button (Kachel ‚Simulation') mit ‚Simulation
/// starten'") trägt die KACHEL die Marke <c>schritt=2</c> und löst damit den Lauf
/// aus; der Knopf bleibt bei <c>schritt=1</c>. Das revidiert für diese Kachel den
/// #207-Entscheid „sie öffnet ③ und startet nicht von selbst".</para>
///
/// <para><b>Seit Auftrag #233</b> (Anwenderrückmeldung 12.09.2026) ist der Weg
/// „Simulation starten" keine KACHEL mehr, sondern der HAUPTKNOPF des
/// Bedienblocks (<c>.epos-simreiter-hauptknopf</c>): dieselbe Beschriftung,
/// derselbe Schlüssel, dieselbe Marke — eine andere Bauform.</para>
///
/// <para>Der Fall tauscht <c>Dienste.Navigation</c> und gehört deshalb in die
/// serielle Sammlung — dieselbe Regel wie die KI-Dialogwege aus #199.</para>
/// </summary>
[Collection("KiDialogweg")]
public class StartseiteSimulationwegTests : EposBunitContext
{
    private readonly INavigation _vorher;

    public StartseiteSimulationwegTests()
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

    // =====================================================================
    //  Probendaten — der Reiter „Simulation" mit seinem Knopf und seiner Kachel
    // =====================================================================

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

    private IRenderedComponent<Startseite> Zeigen(List<string> gemeldet)
        => Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln())
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Bericht, () => new Zusammenfassung("B3-Kaskade", "480 MWh/a", "120 MWh/a", "WP"))
            .Add(x => x.Geklickt, sch => gemeldet.Add(sch)));

    // =====================================================================
    //  Die zwei Wege
    // =====================================================================

    /// <summary>
    /// Der Knopf „Simulation Konfiguration…" öffnet die Ansicht bei Schritt ① —
    /// und meldet sich NICHT mehr über <c>Geklickt</c>.
    /// </summary>
    [Fact]
    public void Der_Konfigurationsknopf_oeffnet_die_Ansicht_bei_Schritt_eins()
    {
        List<string> gemeldet = new List<string>();
        var cut = Zeigen(gemeldet);

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-startreiter-leiste .epos-knopf").Click();

        Assert.Equal(new[] { Masken.Simulation }, Navigation.Masken);
        Assert.Equal(SimulationMarke.SCHRITT_KONFIGURATION, Navigation.LetzteArgumente[0]);
        Assert.Empty(gemeldet);
    }

    /// <summary>
    /// Der Hauptknopf „Simulation starten" öffnet dieselbe Ansicht und LÖST DEN LAUF
    /// AUS (SIM‑E‑1): Marke <c>schritt=2</c> — das ist derselbe Weg, den der
    /// Rechenknopf der Ablaufleiste geht, samt Sperrprüfung.
    /// </summary>
    [Fact]
    public void Der_Hauptknopf_Simulation_starten_loest_Schritt_zwei_aus()
    {
        List<string> gemeldet = new List<string>();
        var cut = Zeigen(gemeldet);

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-simreiter-hauptknopf").Click();

        Assert.Equal(new[] { Masken.Simulation }, Navigation.Masken);
        Assert.Equal(SimulationMarke.SCHRITT_LAUF, Navigation.LetzteArgumente[0]);
        Assert.Empty(gemeldet);
    }

    /// <summary>
    /// Und der Knopf HEISST so — der Titel kommt aus <c>START_K_DETAILSIM_T</c>,
    /// den die Hülle setzt; hier steht er als Probendatum und wird angezeigt.
    /// </summary>
    [Fact]
    public void Der_Hauptknopf_traegt_die_Beschriftung_Simulation_starten()
    {
        List<string> gemeldet = new List<string>();
        var cut = Zeigen(gemeldet);

        cut.FindAll("[role='tab']")[4].Click();

        Assert.Equal("Simulation starten",
                     WindowsFormsApplication1.MyResource.Resource.START_K_DETAILSIM_T);
        Assert.Contains("Simulation starten",
                        cut.Find(".epos-simreiter-hauptknopf").TextContent);
    }

    /// <summary>
    /// Meldet die Navigation <c>false</c> — es zeichnet gerade keine Wurzel, die den
    /// Schlüssel kennt —, geht der Klick den GEWÖHNLICHEN Weg über
    /// <c>Geklickt</c>. Dieselbe Regel wie vor #207 ohne Parametersatz.
    /// </summary>
    [Fact]
    public void Ohne_zustaendige_Navigation_meldet_der_Klick_seinen_Schluessel()
    {
        Navigation.Antwort = false;

        List<string> gemeldet = new List<string>();
        var cut = Zeigen(gemeldet);

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-startreiter-leiste .epos-knopf").Click();
        cut.Find(".epos-simreiter-hauptknopf").Click();

        Assert.Equal(new[] { Kachelschluessel.SimulationKonfiguration,
                             Kachelschluessel.SimulationErgebnis }, gemeldet);
    }
}
