using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// EINE HILFE-PILLE JE BILDSCHIRM (Auftrag #221, Anwenderentscheid <b>KI‑D‑E‑1</b>
/// vom 11.09.2026).
///
/// <para><b>Der Befund.</b> „Die KI-Buttons haben keine unterschiedliche Funktion im
/// Kontext. Daher ist es nicht sinnvoll, auf einer Sicht zwei KI-Buttons zu sehen. Es
/// muss einen Kontext in der KI-Funktion der zweiten Sicht geben, der sich von dem
/// anderen KI-Button unterscheidet." Gemessen: Das Kopfband des
/// <c>Hauptfenster</c>s zeichnete seine Pille mit dem FESTEN Schlüssel
/// <c>Hauptfenster.btn_Help</c> über jeder Ansicht, und die Ansichten darunter
/// zeichneten eine zweite mit ihrem eigenen.</para>
///
/// <para><b>Das Soll.</b> Unter Windows genau EINE Pille je Bildschirm, und ihr
/// Schlüssel folgt der aktiven Ansicht; auf iOS — wo es kein Kopfband gibt — bleibt
/// es bei der Pille der Ansicht.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (Hausregel seit iU9‑W8). Die Fälle laufen in
/// der SERIELLEN Sammlung <c>KiDialogweg</c>: Sowohl <c>Navigationsziel.Aktuell</c> — die
/// zuletzt gezeichnete <c>AppWurzel</c> — als auch <c>KiChatKontext.Aufruf</c> sind
/// prozessweiter Zustand, und xunit fährt Testklassen nebeneinander (Lehre iU5‑O‑1).</para>
/// </summary>
[Collection("KiDialogweg")]
public class HilfePilleTests : EposBunitContext
{
    public HilfePilleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Services.AddSingleton<IProjektQuelle>(new KeineProjekte());
    }

    protected override void Dispose(bool disposing)
    {
        Navigationsziel.Aktuell = null;
        KiChatKontext.AufrufMelden(null);
        KiMaskenbruecke.Leeren();
        base.Dispose(disposing);
    }

    // =====================================================================
    //  Probendaten
    // =====================================================================

    /// <summary>Der Parametersatz der Ansicht SIMULATION in seiner schmalsten Form.</summary>
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
                            ErgebnisGueltig = true,
                            ReiterStromspeicher = true
                        },
                        Bild = _ => null
                    },
                    ["StartProjekt"] = 1030
                },
                ErgebnisVorhanden = () => true
            },
            ["ProjektText"] = "Projekt „B3-Kaskade“"
        };

    /// <summary>Der Parametersatz der Ansicht STROMSPEICHER_AUSLEGUNG.</summary>
    private static IReadOnlyDictionary<string, object> Auslegungsgaben()
        => new Dictionary<string, object>
        {
            ["Dienste"] = new EPOS.UI.Seiten.Strom.StromspeicherAuslegungDienste()
        };

    private IRenderedComponent<Hauptfenster> Fenster(
        Func<IReadOnlyDictionary<string, object>?>? simulation = null,
        Func<IReadOnlyDictionary<string, object>?>? auslegung = null)
        => Render<Hauptfenster>(p => p
            .Add(x => x.Startansicht, Seitenschluessel.Projektliste)
            .Add(x => x.SimulationGaben, simulation)
            .Add(x => x.StromspeicherAuslegungGaben, auslegung)
            .Add(x => x.VersionText, "Version 1.0.0.0"));

    /// <summary>
    /// KEIN Hilfeschlüssel steht zweimal auf demselben Bildschirm — die Regel, die der
    /// Befund KI‑D‑E‑1 verlangt.
    /// </summary>
    private static void Schluesselprobe(IRenderedComponent<Hauptfenster> cut)
    {
        List<string> schluessel = cut.FindComponents<EPOS.UI.Bausteine.InfoKnopf>()
                                     .Select(k => k.Instance.Schluessel)
                                     .ToList();

        Assert.Equal(schluessel.Distinct(StringComparer.Ordinal).Count(), schluessel.Count);
    }

    // =====================================================================
    //  1 — Genau EINE Pille, und ihr Schluessel folgt der Ansicht
    // =====================================================================

    [Fact]
    public void Ohne_Ansicht_traegt_die_Kopfband_Pille_den_Schluessel_des_Fensters()
    {
        var cut = Fenster();

        Assert.Single(cut.FindAll(".epos-hilfepille"));
        Assert.Equal("Hauptfenster.btn_Help", cut.Instance.KopfbandSchluessel);
    }

    [Fact]
    public void In_der_Simulation_steht_genau_EINE_Pille_und_sie_traegt_deren_Schluessel()
    {
        var cut = Fenster(simulation: Simulationsgaben);

        cut.Instance.Zeige(Seitenschluessel.Simulation);
        cut.Render();
        cut.WaitForElement(".epos-simansicht");

        // Die Ansicht zeichnet KEINE eigene mehr - das Kopfband fuehrt sie, und auch
        // der Kopf von Schritt ① traegt seinen Schluessel nicht ein zweites Mal.
        Assert.Single(cut.FindAll(".epos-hauptfenster-marke .epos-hilfepille"));
        Assert.Empty(cut.FindAll(".epos-simansicht-kopfaktionen .epos-hilfepille"));
        Assert.Empty(cut.FindAll(".epos-simkonfig-kopf .epos-hilfepille"));

        // Und die HAUSREGEL, die der Befund verlangt: Kein Schluessel steht zweimal
        // auf dem Bildschirm - genau das war „zwei KI-Buttons ohne unterschiedliche
        // Funktion im Kontext". Was bleibt, ist der Knopf „Berechnungsweg" im
        // Erzeugerabschnitt; er traegt einen anderen Schluessel und eine andere
        // Hilfeseite.
        Schluesselprobe(cut);

        // Und sie traegt den Schluessel von Schritt ① - daraus leitet der Kern den
        // Bereich ab (B_SIM_KONFIG statt B_HAUPTFENSTER).
        cut.WaitForAssertion(() =>
            Assert.Equal("Form_Simulation_Config.btn_Help", cut.Instance.KopfbandSchluessel));

        Assert.Equal(KiChatKontext.B_SIM_KONFIG,
                     KiChatKontext.BereichFuerHilfeschluessel(cut.Instance.KopfbandSchluessel));
    }

    [Fact]
    public void In_der_Stromspeicher_Auslegung_folgt_die_Pille_deren_Schluessel()
    {
        var cut = Fenster(auslegung: Auslegungsgaben);

        cut.Instance.Zeige(Seitenschluessel.StromspeicherAuslegung);
        cut.Render();
        cut.WaitForElement(".epos-spauslegung");

        Assert.Single(cut.FindAll(".epos-hilfepille"));
        cut.WaitForAssertion(() =>
            Assert.Equal("Form_SpeicherOptimierung.btn_Help", cut.Instance.KopfbandSchluessel));
    }

    [Fact]
    public void Zurueck_auf_die_Startansicht_faellt_der_Schluessel_des_Fensters_wieder_zu()
    {
        var cut = Fenster(simulation: Simulationsgaben);

        cut.Instance.Zeige(Seitenschluessel.Simulation);
        cut.Render();
        cut.WaitForAssertion(() =>
            Assert.Equal("Form_Simulation_Config.btn_Help", cut.Instance.KopfbandSchluessel));

        // Die Ansicht wird verworfen - und meldet dabei ab.
        cut.Instance.Zeige(Seitenschluessel.Projektliste);
        cut.Render();
        cut.WaitForAssertion(() =>
            Assert.Equal("Hauptfenster.btn_Help", cut.Instance.KopfbandSchluessel));

        Assert.Single(cut.FindAll(".epos-hilfepille"));
    }

    [Fact]
    public void Der_Schritt_der_Simulation_wechselt_den_Schluessel_der_Pille()
    {
        var cut = Fenster(simulation: Simulationsgaben);

        // SIMULATION_ERGEBNIS ist die Einstiegsmarke auf Schritt ③ (#207).
        cut.Instance.Zeige(Seitenschluessel.SimulationErgebnis);
        cut.Render();

        // Schritt ③ traegt den Schluessel der Ergebnisseite, und aus ihm folgt ein
        // ANDERER Bereich als aus Schritt ①.
        cut.WaitForAssertion(() =>
            Assert.Equal("Form_Simulation_Detail.btn_Help", cut.Instance.KopfbandSchluessel));

        Assert.Equal(KiChatKontext.B_SIM_DETAIL,
                     KiChatKontext.BereichFuerHilfeschluessel(cut.Instance.KopfbandSchluessel));
    }

    // =====================================================================
    //  2 — Ohne Kopfband zeichnet die Ansicht ihre Pille selbst (iOS)
    // =====================================================================

    [Fact]
    public void Ohne_Kopfband_zeichnet_die_Simulationsansicht_ihre_eigene_Pille()
    {
        // Der iOS-Fall: AppWurzel OHNE Hauptfenster - der CascadingValue fehlt.
        var quelle = new TestProjektquelle { Simulation = Simulationsgaben() };
        Services.AddSingleton<IProjektQuelle>(quelle);

        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Startansicht, Seitenschluessel.Simulation));

        cut.WaitForElement(".epos-simansicht");
        Assert.Single(cut.FindAll(".epos-simansicht-kopfaktionen .epos-hilfepille"));
    }

    [Fact]
    public void Mit_Kopfband_zeichnet_die_Simulationsansicht_keine()
    {
        // Die Gegenprobe zur Probe darueber: dieselbe Ansicht, nur unter dem
        // Hauptfenster.
        var cut = Fenster(simulation: Simulationsgaben);

        cut.Instance.Zeige(Seitenschluessel.Simulation);
        cut.Render();
        cut.WaitForElement(".epos-simansicht");

        Assert.Empty(cut.FindAll(".epos-simansicht-kopfaktionen .epos-hilfepille"));
    }

    // =====================================================================
    //  3 — Die STELLE: Ansicht · Schritt · Reiter
    // =====================================================================

    [Fact]
    public void Der_Kontext_nennt_Ansicht_Schritt_und_Reiter()
    {
        var kontext = new Hilfekontext("Form_Simulation_Detail.btn_Help",
                                       "Simulation", "3 Ergebnis", "Stromspeicher");

        Assert.Equal("Simulation · 3 Ergebnis · Stromspeicher", kontext.Dialogname);
    }

    [Fact]
    public void Leere_Teile_fallen_aus_der_Stelle_weg()
    {
        Assert.Equal("Simulation · 1 Konfiguration",
                     new Hilfekontext("x", "Simulation", "1 Konfiguration").Dialogname);

        Assert.Equal("Simulation", new Hilfekontext("x", "Simulation").Dialogname);
        Assert.Equal("", new Hilfekontext("x").Dialogname);
        Assert.False(Hilfekontext.Keiner.Da);
    }

    [Fact]
    public void Die_Kopfband_Pille_reicht_die_Stelle_als_Dialognamen_weiter()
    {
        var cut = Fenster(simulation: Simulationsgaben);

        cut.Instance.Zeige(Seitenschluessel.SimulationErgebnis);
        cut.Render();

        cut.WaitForAssertion(() =>
            Assert.Equal("Form_Simulation_Detail.btn_Help", cut.Instance.KopfbandSchluessel));

        // Der Dialogname des Chats: „Simulation · 3 Ergebnis · Übersicht" - der
        // Reiter, mit dem Schritt ③ aufmacht (Windows-Abnahme #216, Punkt 4).
        EPOS.UI.Bausteine.InfoKnopf pille =
            cut.FindComponents<EPOS.UI.Bausteine.InfoKnopf>()[0].Instance;

        // SEIT AUFTRAG #224 traegt der Schrittname keine Ziffer mehr — sie steht im
        // nummerierten Kreis der Stufenleiste (Konzept 7.8).
        Assert.StartsWith("Simulation · Ergebnis", pille.Dialogname, StringComparison.Ordinal);
    }

    // =====================================================================
    //  4 — Die Pille LEUCHTET, solange der Assistent fuer sie steht (#218)
    // =====================================================================

    [Fact]
    public void Die_Pille_leuchtet_nur_fuer_den_Schluessel_ihrer_eigenen_Ansicht()
    {
        var cut = Fenster();
        Assert.False(cut.Instance.AssistentSteht);

        // Der Assistent geht aus einer ANDEREN Maske auf - die Pille bleibt dunkel.
        KiChatKontext.AufrufMelden(
            KiAufrufkontext.AusHilfeschluessel("Form_PV.btn_Help"));
        cut.WaitForAssertion(() => Assert.False(cut.Instance.AssistentSteht));

        // Aus DIESER - sie leuchtet.
        KiChatKontext.AufrufMelden(
            KiAufrufkontext.AusHilfeschluessel("Hauptfenster.btn_Help"));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.AssistentSteht));

        // Und das Chatfenster geht zu: Die Wurzel meldet den Wechsel nach oben, ohne
        // dass jemand in der WebView geklickt haette.
        KiChatKontext.AufrufMelden(null);
        cut.WaitForAssertion(() => Assert.False(cut.Instance.AssistentSteht));
    }

    // =====================================================================
    //  5 — Die STARTFRAGE je Bereich (statt der leeren Eingabezeile)
    // =====================================================================

    [Theory]
    [InlineData("Form_Simulation_Config.btn_Help", "KI_FRAGE_SIMULATION_KONFIG")]
    [InlineData("Form_Simulation_Detail.btn_Help", "KI_FRAGE_SIMULATION_ERGEBNIS")]
    public void Eine_Ansicht_mit_Startfrage_belegt_die_Eingabezeile_vor(
        string hilfeschluessel, string ressource)
    {
        string erwartet =
            WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(ressource) ?? "";
        Assert.NotEqual("", erwartet);

        KiAufrufkontext aufruf = KiAufrufkontext.AusHilfeschluessel(hilfeschluessel);

        Assert.Equal(erwartet, aufruf.Frage);
    }

    [Fact]
    public void Eine_Ansicht_ohne_Startfrage_laesst_die_Eingabezeile_leer()
    {
        // Die Gegenprobe: Der Assistent stellt keine erfundene Frage.
        Assert.Equal("", KiAufrufkontext.AusHilfeschluessel("Form_PV.btn_Help").Frage);
        Assert.Equal("", KiAufrufkontext.AusHilfeschluessel("").Frage);
    }

    [Fact]
    public void Eine_mitgegebene_Frage_gewinnt_gegen_die_Startfrage()
    {
        // Der Weg aus einem BANNER (Weg 2, #199) weiss genauer, worum es geht.
        KiAufrufkontext aufruf = KiAufrufkontext.AusHilfeschluessel(
            "Form_Simulation_Config.btn_Help", null, "Warum steht das BHKW hinten?");

        Assert.Equal("Warum steht das BHKW hinten?", aufruf.Frage);
    }
}
