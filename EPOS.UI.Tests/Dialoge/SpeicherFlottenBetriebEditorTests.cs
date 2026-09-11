using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenBetriebEditorTests : EposBunitContext
{
    [Fact]
    public void Alle_gemeinsamen_Betriebsfelder_werden_als_Snapshot_gemeldet()
    {
        FlottenSimulationOptionen? gemeldet = null;
        var eingang = new FlottenSimulationOptionen();
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, eingang)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Auswahl(cut, "Betriebsziel:").Change(((int)FlottenBetriebsziel.MultiUse).ToString());
        Auswahl(cut, "Leistungsverteilung:").Change(((int)FlottenVerteilung.Grenzkosten).ToString());
        Auswahl(cut, "Erzeuger-Reihenfolge:").Change(((int)FlottenErzeugerPrioritaet.BhkwVorPv).ToString());
        Eingabe(cut, "Wirtschaftlicher Peak-Zielwert:").Input("82.5");
        Schalter(cut, "Laden aus dem Netz erlauben").Change(true);
        Schalter(cut, "Batterieexport ins Netz erlauben").Change(true);

        Assert.NotNull(gemeldet);
        Assert.Equal(FlottenBetriebsziel.MultiUse, gemeldet!.Betriebsziel);
        Assert.Equal(FlottenVerteilung.Grenzkosten, gemeldet.Verteilung);
        Assert.Equal(FlottenErzeugerPrioritaet.BhkwVorPv, gemeldet.ErzeugerPrioritaet);
        Assert.Equal(82.5, gemeldet.WirtschaftlicherPeakZielwertKw);
        Assert.True(gemeldet.NetzladungErlaubt);
        Assert.True(gemeldet.BatterieexportErlaubt);
        Assert.Equal(FlottenBetriebsziel.PvGreedy, eingang.Betriebsziel);
    }

    [Fact]
    public void Peakziel_erscheint_nur_fuer_PeakShaving_und_MultiUse()
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen()));

        Assert.DoesNotContain("Wirtschaftlicher Peak-Zielwert", cut.Markup);
        Auswahl(cut, "Betriebsziel:").Change(((int)FlottenBetriebsziel.PeakShaving).ToString());
        Assert.Contains("Wirtschaftlicher Peak-Zielwert", cut.Markup);
        Auswahl(cut, "Betriebsziel:").Change(((int)FlottenBetriebsziel.PvPlanung).ToString());
        Assert.DoesNotContain("Wirtschaftlicher Peak-Zielwert", cut.Markup);
    }

    [Fact]
    public void Gesperrter_Editor_deaktiviert_alle_Eingaben()
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.MultiUse
            })
            .Add(x => x.Aktiv, false));

        Assert.All(cut.FindAll("select, input"), x => Assert.True(x.HasAttribute("disabled")));
    }

    private static AngleSharp.Dom.IElement Auswahl(
        IRenderedComponent<SpeicherFlottenBetriebEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;

    private static AngleSharp.Dom.IElement Eingabe(
        IRenderedComponent<SpeicherFlottenBetriebEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;

    private static AngleSharp.Dom.IElement Schalter(
        IRenderedComponent<SpeicherFlottenBetriebEditor> cut, string label) => Eingabe(cut, label);
}
