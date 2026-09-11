using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenErgaenzungenTests : EposBunitContext
{
    [Fact]
    public void Rainflow_Kennlinie_und_marginale_Verschleisskosten_werden_editierbar_gemeldet()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, Konfiguration())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Eingabe(cut, "Marginale Verschleißkosten").Input("0,035");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Kennlinienpunkt")).Click();
        Eingabe(cut, "Entladetiefe Punkt 1").Input("80");
        Eingabe(cut, "Zyklen bis EOL Punkt 1").Input("6000");

        Assert.Equal(.035, gemeldet!.Einheiten[0].GrenzverschleissEuroProKWhEntladung, 10);
        Assert.Equal(.8, gemeldet.Einheiten[0].RainflowKurve[0].Entladetiefe, 10);
        Assert.Equal(6000, gemeldet.Einheiten[0].RainflowKurve[0].ZyklenBisEol);
        Assert.Equal(.8, gemeldet.Auslegung.Achsen[0].Vorlage.RainflowKurve[0].Entladetiefe, 10);
        Assert.Equal(.8, gemeldet.Wirtschaftlichkeit.Einheiten[0].RainflowKurve[0].Entladetiefe, 10);
    }

    [Fact]
    public void Kopierte_Einheit_bekommt_eine_unabhaengige_Rainflow_Kennlinie()
    {
        var config = Konfiguration();
        config.Einheiten[0].RainflowKurve.Add(new FlottenRainflowPunkt { Entladetiefe = .5, ZyklenBisEol = 9000 });
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, config));

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Kopieren").Click();
        FlottenStudieKonfiguration snapshot = cut.Instance.AktuellerSnapshot;
        snapshot.Einheiten[0].RainflowKurve[0].ZyklenBisEol = 1;

        Assert.Equal(9000, cut.Instance.AktuellerSnapshot.Einheiten[1].RainflowKurve[0].ZyklenBisEol);
    }

    [Fact]
    public void Ergebnis_zeigt_unbekannten_Rainflow_und_Kapitalwert_als_Strich()
    {
        SpeicherFlottenErgebnis e = Ergebnis(new List<FlottenRainflowPunkt>());
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, e));

        Assert.Contains("Kapitalwert gegenüber „ohne Speicher“: —", cut.Markup);
        Assert.Contains("<td>—</td>", cut.Markup);
    }

    [Fact]
    public void Ergebnis_zeigt_Rainflow_Schaden_nur_mit_hinterlegter_Kennlinie()
    {
        SpeicherFlottenErgebnis e = Ergebnis(new List<FlottenRainflowPunkt>
            { new() { Entladetiefe = .8, ZyklenBisEol = 6000 } });
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, e));

        Assert.DoesNotContain("<td>—</td>", cut.Markup);
        Assert.Contains("0,25", cut.Markup);
    }

    [Fact]
    public void Ergebnis_behauptet_ohne_zulaessigen_Kandidaten_keine_beste_Flotte()
    {
        SpeicherFlottenErgebnis e = Ergebnis(new List<FlottenRainflowPunkt>());
        e.Auslegung = new FlottenAuslegungErgebnis
        {
            NullvarianteGewonnen = false,
            Kandidaten = new() { new FlottenKandidatZusammenfassung { KandidatId = "x", Zulaessig = false } }
        };

        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, e));

        Assert.Contains("Keine technisch zulässige Flotte", cut.Markup);
        Assert.DoesNotContain("Beste technisch zulässige Flotte", cut.Markup);
    }

    private static SpeicherFlottenErgebnis Ergebnis(List<FlottenRainflowPunkt> kurve) => new()
    {
        Konfiguration = new FlottenStudieKonfiguration
        {
            Einheiten = new() { new FlottenEinheit { Id = "s1", Name = "S1", RainflowKurve = kurve } }
        },
        Studie = new FlottenStudienErgebnis
        {
            Variante = new FlottenSimulationErgebnis
            {
                SpeicherKennzahlen = new() { new FlottenSpeicherKennzahlen { SpeicherId = "s1", RainflowSchaden = .25 } }
            }
        }
    };

    private static FlottenStudieKonfiguration Konfiguration()
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "S1", KapazitaetKWh = 100,
            LadeleistungKw = 50, EntladeleistungKw = 50,
            Ladewirkungsgrad = .95, Entladewirkungsgrad = .95,
            SocMin = .1, SocMax = .9, SocStart = .5
        };
        return new FlottenStudieKonfiguration
        {
            Einheiten = new() { einheit },
            Auslegung = new FlottenAuslegungEingang
            {
                Achsen = new() { new FlottenAuslegungsAchse { Aktiv = true, Vorlage = einheit } }
            }
        };
    }

    private static AngleSharp.Dom.IElement Eingabe(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;
}
