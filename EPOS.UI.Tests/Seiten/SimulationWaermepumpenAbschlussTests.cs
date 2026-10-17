using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Katalogauswahl Stufe 3, R2: Schließt der Wärmepumpendialog der Simulation, schließt die
/// Hülle die Projektkopien der Sitzung über die Naht <c>WaermepumpeAbschluss</c> ab — mit OK
/// NACH dem Schreiben der Anlagen und mit <c>true</c>, mit Abbrechen ohne Schreiben und mit
/// <c>false</c>. Ohne Naht (iOS) bleibt es beim Schreiben; ohne Liste geschieht nichts.
/// </summary>
public class SimulationWaermepumpenAbschlussTests
{
    [Fact]
    public void Ok_schreibt_zuerst_und_schliesst_dann_mit_true_ab()
    {
        var modelle = new List<WErzeugerModel> { new WErzeugerModel() };
        var folge = new List<string>();

        SimulationErgebnisHuelle.WaermepumpenAbschliessen(true, modelle,
            m => { Assert.Same(modelle, m); folge.Add("schreiben"); },
            (ok, m) => { Assert.Same(modelle, m); folge.Add("abschluss:" + ok); });

        Assert.Equal(new[] { "schreiben", "abschluss:True" }, folge);
    }

    [Fact]
    public void Abbrechen_schreibt_nicht_und_schliesst_mit_false_ab()
    {
        var modelle = new List<WErzeugerModel> { new WErzeugerModel() };
        var folge = new List<string>();

        SimulationErgebnisHuelle.WaermepumpenAbschliessen(false, modelle,
            m => folge.Add("schreiben"),
            (ok, m) => folge.Add("abschluss:" + ok));

        Assert.Equal(new[] { "abschluss:False" }, folge);
    }

    [Fact]
    public void Ohne_Naht_bleibt_es_beim_Schreiben_und_ohne_Liste_geschieht_nichts()
    {
        var folge = new List<string>();
        SimulationErgebnisHuelle.WaermepumpenAbschliessen(true, new List<WErzeugerModel>(),
            m => folge.Add("schreiben"), null);
        Assert.Equal(new[] { "schreiben" }, folge);

        folge.Clear();
        SimulationErgebnisHuelle.WaermepumpenAbschliessen(true, null,
            m => folge.Add("schreiben"), (ok, m) => folge.Add("abschluss"));
        Assert.Empty(folge);
    }
}
