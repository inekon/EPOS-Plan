using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Strom;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// AUFTRAG #225 (Anwenderwunsch 11.09.2026): Das Bildschirmfoto der Ansicht
/// „STROMSPEICHER_AUSLEGUNG" zeigte links „1 2 3", eine breite Lücke, dann rechts
/// „4 5" — die Lücke kam von <c>margin-inline-start: auto</c> am Rechenknopf
/// (<c>.epos-ablaufleiste-aktion</c>), das die breite Fassung der Ablaufleiste seit
/// jeher trägt. Der neue Modifikator <c>Buendig</c> nimmt NUR diesen Abstand weg.
///
/// <para><b>Was hier geprüft wird:</b> Der Stromspeicher-Wirt setzt den Modifikator,
/// die fünf Stationen stehen ohne Lücke; der Simulations-Wirt (Windows-Abnahme #216,
/// die rechtsbündige Werkzeugleiste) setzt ihn NICHT — er bleibt bei
/// <c>Kompakt</c>, und daran ändert #225 nichts.</para>
/// </summary>
public sealed class AblaufleisteBuendigTests : EposBunitContext
{
    public AblaufleisteBuendigTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Der Stromspeicher-Wirt TRÄGT den Modifikator
    // =====================================================================

    [Fact]
    public void Die_Stromspeicher_Auslegung_traegt_die_Modifikatorklasse_Buendig()
    {
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben { Auslegung = new SpeicherAuslegungKonfiguration() },
            Leistungspreisquellen = Array.Empty<SpeicherOptimierungLeistungspreisQuelle>()
        };
        var dienste = new StromspeicherAuslegungDienste
        {
            Vorgaben = () => vorgaben,
            EinstellungenSpeichern = _ => Task.FromResult(""),
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis { Erfolg = true })
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

        Assert.Single(cut.FindAll("nav.epos-ablaufleiste--buendig"));

        // Fünf Stationen in EINER Reihe — kein zweiter Wirt schiebt die Kompakt-Klasse mit.
        Assert.Empty(cut.FindAll("nav.epos-ablaufleiste--kompakt"));

        // SEIT AUFTRAG #224 sind es FUENF BLAETTER und KEIN Aktionsplatz (SD-E-9,
        // Option A): Station 4 ist die Seite „Optimierung", ihr Rechenknopf steht in
        // ihr. Jede Station traegt ihren nummerierten Kreis (Konzept 7.8).
        Assert.Equal(5, cut.FindAll("nav.epos-ablaufleiste--buendig button.epos-ablaufleiste-knopf").Count);
        Assert.Empty(cut.FindAll("nav.epos-ablaufleiste--buendig button.epos-ablaufleiste-rechnen"));
        Assert.Equal(5, cut.FindAll("nav.epos-ablaufleiste--buendig span.epos-ablaufleiste-stufe").Count);
    }

    // =====================================================================
    //  Der Simulations-Wirt bleibt bei Kompakt — #225 aendert ihn NICHT
    // =====================================================================

    [Fact]
    public void Die_Simulationsseite_traegt_weiterhin_nur_Kompakt_und_nicht_Buendig()
    {
        var konfigdienste = new SimulationKonfigDienste
        {
            Laden = _ => new SimulationKonfigDaten { IdProjekt = 1030 },
            Verschieben = (_, _) => { },
            Speichern = () => true
        };
        var ergebnisdienste = new SimulationErgebnisDienste
        {
            Laden = _ => new SimulationErgebnisDaten { IdProjekt = 1030 },
            Bild = _ => null,
            Laufen = _ => Task.FromResult(new Rueckmeldung(true, "")),
            Speichern = () => new Rueckmeldung(true, "")
        };
        var dienste = new SimulationAnsichtDienste
        {
            Konfiguration = new Dictionary<string, object>
            {
                ["Dienste"] = konfigdienste,
                ["StartProjekt"] = 1030
            },
            Ergebnis = new Dictionary<string, object>
            {
                ["Dienste"] = ergebnisdienste,
                ["StartProjekt"] = 1030
            },
            Sperrgrund = () => "",
            ErgebnisVorhanden = () => false
        };

        var cut = Render<SimulationSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.ProjektText, "Projekt „B3-Kaskade“"));

        Assert.Single(cut.FindAll("nav.epos-ablaufleiste--kompakt"));
        Assert.Empty(cut.FindAll("nav.epos-ablaufleiste--buendig"));
    }
}
