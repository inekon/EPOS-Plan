using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenDialogTests : BunitContext
{
    [Fact]
    public void Echter_Einstieg_zeigt_Flotteneditor_statt_altem_Einzelspeicherraster()
    {
        var cut = Render<SpeicherOptimierungDialog>(p => p
            .Add(x => x.Vorgaben, () => new SpeicherOptimierungVorgaben())
            .Add(x => x.FlottenRechnen, (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())));
        Assert.Single(cut.FindComponents<SpeicherFlottenEditor>());
        Assert.Contains("Speicher &amp; Betriebsführung", cut.Markup);
        Assert.DoesNotContain("Vorbelegung: 500", cut.Markup);
    }

    [Fact]
    public void Berechnung_bekommt_alle_physischen_Einheiten_als_unabhaengigen_Stand()
    {
        SpeicherOptimierungEingaben? erhalten = null;
        var vorgaben = new SpeicherOptimierungVorgaben { Eingaben = new SpeicherOptimierungEingaben
        { Auslegung = new SpeicherAuslegungKonfiguration { Flotte = new FlottenStudieKonfiguration
        {
            Einheiten = new() { new() { Id = "a", Name = "A", KapazitaetKWh = 20 }, new() { Id = "b", Name = "B", KapazitaetKWh = 80 } },
            Optionen = new() { EnergieAusgleichEuroProKWh = 0.2 },
            Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
        } } } };
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, () => vorgaben)
            .Add(x => x.Rechnen, (e, _) => { erhalten = e; return Task.FromResult(new SpeicherFlottenErgebnis { Erfolg = true }); }));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Speichervergleich berechnen").Click();
        Assert.Equal(2, erhalten!.Auslegung.Flotte.Einheiten.Count);
        erhalten.Auslegung.Flotte.Einheiten[0].KapazitaetKWh = 999;
        Assert.Equal(20, vorgaben.Eingaben.Auslegung.Flotte.Einheiten[0].KapazitaetKWh);
        Assert.Single(cut.FindComponents<SpeicherFlottenErgebnisAnsicht>());
    }

    [Fact]
    public async Task Projektuebernahme_ist_explizit_und_nach_Eingabeaenderung_gesperrt()
    {
        int uebernahmen = 0;
        var config = new FlottenStudieKonfiguration
        {
            Einheiten = new() { new() { Id = "a", Name = "A", KapazitaetKWh = 20, LadeleistungKw = 5, EntladeleistungKw = 5,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5 } },
            Optionen = new() { EnergieAusgleichEuroProKWh = 0.2 },
            Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
        };
        var input = new FlottenEingang { DatenId = "Test", Istwerte = new()
        {
            new FlottenNetzintervall { Zeitstempel = DateTimeOffset.Parse("2026-01-01T00:00:00Z"), LastKw = 10 },
            new FlottenNetzintervall { Zeitstempel = DateTimeOffset.Parse("2026-01-01T00:15:00Z"), LastKw = 10 }
        } };
        var ergebnis = new SpeicherFlottenErgebnis { Erfolg = true, Konfiguration = config,
            Studie = FlottenSimulator.Simuliere(input, config) };
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, () => new SpeicherOptimierungVorgaben { Eingaben = new()
                { Auslegung = new() { Flotte = config } } })
            .Add(x => x.Rechnen, (_, _) => Task.FromResult(ergebnis))
            .Add(x => x.ProjektUebernehmen, e => { Assert.Same(ergebnis, e); uebernahmen++; return Task.FromResult(""); }));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Speichervergleich berechnen").Click();
        Assert.Equal(0, uebernahmen);
        cut.FindAll("button").Single(b => b.TextContent.Contains("für die Projektsimulation aktivieren")).Click();
        Assert.Equal(1, uebernahmen);
        Assert.Contains("Bitte die Projektsimulation neu berechnen", cut.Markup);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Speicher & Betriebsführung").Click();
        await cut.InvokeAsync(() => cut.FindComponent<SpeicherFlottenEditor>().Instance.WertChanged.InvokeAsync(config));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Vergleich & Ergebnisse").Click();
        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Contains("für die Projektsimulation aktivieren")).HasAttribute("disabled"));
    }
}
