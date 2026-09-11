using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der iOS-WEG in die Simulation (Auftrag <b>#208</b>, Stufe S2 des Konzepts
/// „Simulationsablauf ohne Dialog", Anwenderentscheid <b>SIM‑Q6</b>).
///
/// <para><b>Der Unterschied zu den Fällen in <c>AppWurzelTests</c>.</b> Dort kommt
/// der Parametersatz über die WINDOWS-Naht — den Delegaten
/// <c>AppWurzel.SimulationGaben</c>, den <c>HauptfensterHuelle</c> einlegt. Hier
/// kommt er über <c>IProjektQuelle.SimulationGaben</c>, und das ist der einzige Weg,
/// den iOS hat: Dort gibt es keine Seitenhülle, nur die Projektquelle
/// (<c>EPOS.iOS/Dienste/IosProjektQuelle</c>). Bis #208 lieferte sie <c>null</c>,
/// und die Ansicht ging auf dem iPad nicht auf.</para>
///
/// <para><b>Der Einstieg ist die PROJEKTLISTE</b> — auf iOS die Startansicht. Sie
/// führte bis #208 zwei Knöpfe je Zeile (Energieträger, BHKW-Wirtschaftlichkeit);
/// der dritte ist die Simulation.</para>
/// </summary>
public class SimulationAufIosTests : EposBunitContext
{
    private static readonly ProjektZeile[] ZweiProjekte =
    {
        new ProjektZeile(1030, "B3-Kaskade", "Region 12", "WP+BHKW"),
        new ProjektZeile(1007, "Speichervariante A", "Region 12", "WP+Speicher")
    };

    public SimulationAufIosTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Der Parametersatz, den <c>SimulationAnsichtQuelle.AnsichtGaben</c> liefert —
    /// hier in seiner schmalsten Form, denn geprüft wird der WEG und nicht der
    /// Inhalt (der steht in <c>EPOS.Kern.Tests/SimulationAnsichtQuelleTests</c>).
    /// </summary>
    private static IReadOnlyDictionary<string, object> Simulationsgaben()
        => new Dictionary<string, object>
        {
            ["Dienste"] = new EPOS.UI.Seiten.Simulation.SimulationAnsichtDienste
            {
                Konfiguration = new Dictionary<string, object>
                {
                    ["Dienste"] = new EPOS.UI.Seiten.Simulation.SimulationKonfigDienste
                    {
                        Laden = _ => new EPOS.UI.Seiten.Simulation.SimulationKonfigDaten()
                    },
                    ["StartProjekt"] = 1030
                },
                Ergebnis = new Dictionary<string, object>
                {
                    ["Dienste"] = new EPOS.UI.Seiten.Simulation.SimulationErgebnisDienste
                    {
                        Laden = _ => new EPOS.UI.Seiten.Simulation.SimulationErgebnisDaten
                        {
                            IdProjekt = 1030,
                            ErgebnisGueltig = true
                        },
                        Bild = _ => null
                    },
                    ["StartProjekt"] = 1030
                },
                ErgebnisVorhanden = () => true
            },
            ["ProjektText"] = "Projekt „B3-Kaskade“"
        };

    // =====================================================================

    /// <summary>
    /// <b>Die Kachel gibt es.</b> Jede Zeile der Projektliste führt seit #208 drei
    /// Knöpfe; der dritte heißt „Simulation…".
    /// </summary>
    [Fact]
    public void Jede_Zeile_der_Projektliste_fuehrt_einen_Knopf_Simulation()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.Equal(2, cut.FindAll("button.epos-projekt-simulation").Count);
        Assert.All(cut.FindAll("button.epos-projekt-simulation"),
                   k => Assert.Contains("Simulation", k.TextContent, StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>Der Weg, den #208 öffnet.</b> Ein Tipp auf „Simulation…" wechselt auf die
    /// Ansicht SIMULATION — allein über <c>IProjektQuelle.SimulationGaben</c>, ohne
    /// jede Windows-Naht.
    /// </summary>
    [Fact]
    public void Der_Knopf_Simulation_erreicht_die_Ansicht_ueber_die_Projektquelle()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { Simulation = Simulationsgaben() };
        var cut = Aufbauen(quelle);

        cut.FindAll("button.epos-projekt-simulation")[0].Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-simansicht"));
        Assert.Empty(cut.FindAll("button.epos-projekt-simulation"));
    }

    /// <summary>
    /// <b>Ohne Parametersatz geht nichts auf</b> — und die Liste bleibt stehen, statt
    /// leer zu werden. Genau der Zustand der iOS-Hülle VOR #208
    /// (<c>SimulationGaben</c> lieferte <c>null</c>); dieselbe Regel wie beim
    /// KI-Assistenten und bei „Berichte &amp; Kosten".
    /// </summary>
    [Fact]
    public void Ohne_Simulationsgaben_bleibt_die_Liste_stehen()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.FindAll("button.epos-projekt-simulation")[0].Click();
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-simansicht"));
        Assert.Equal(2, cut.FindAll("button.epos-projekt-simulation").Count);
    }

    /// <summary>
    /// <b>Der Rückweg über den Stapel</b> (#207, SIM‑Q4): „← zurück" führt dorthin,
    /// woher man kam — auf iOS also in die Projektliste.
    /// </summary>
    [Fact]
    public void Der_Rueckweg_aus_der_Simulation_landet_wieder_in_der_Projektliste()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { Simulation = Simulationsgaben() };
        var cut = Aufbauen(quelle);

        cut.FindAll("button.epos-projekt-simulation")[1].Click();
        cut.Render();
        Assert.Single(cut.FindAll(".epos-simansicht"));

        cut.FindAll("button").First(k => k.TextContent.Trim() == "← zurück").Click();
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-simansicht"));
        Assert.Equal(2, cut.FindAll("button.epos-projekt-simulation").Count);
    }

    // =====================================================================

    private IRenderedComponent<AppWurzel> Aufbauen(TestProjektquelle quelle)
    {
        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>();
    }
}
