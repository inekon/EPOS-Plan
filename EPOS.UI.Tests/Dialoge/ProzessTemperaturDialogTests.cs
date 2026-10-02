using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Das Temperaturpaar der Prozesswärme in den beiden Dialogen</b> (Entscheidungsvorlage
/// Modellgrenzen, PW1 Stufe 1): im Projektdialog (<see cref="BedarfsProfileDialog"/>) das Paar
/// der gewählten Projektzeile — übernommen in den Arbeitsstand, geschrieben mit OK —, im
/// Stammkopf (<see cref="TypStammDialog"/>) das Paar des Katalogsatzes. Nur die Prozesswärme
/// zeigt es; die Prüfung kommt als Delegat aus der Hülle.
/// </summary>
public class ProzessTemperaturDialogTests : EposBunitContext
{
    public ProzessTemperaturDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Prüfung der Hülle, nachgebildet: beide oder keine, Vorlauf nicht unter dem Rücklauf.</summary>
    private static string? Pruefen(double? v, double? r)
    {
        if (v is null && r is null) return null;
        if (v is null || r is null) return "Paar unvollständig";
        return v < r ? "Vorlauf unter Rücklauf" : null;
    }

    private IRenderedComponent<BedarfsProfileDialog> Projektdialog(
        BedarfsArt art, List<BedarfsProfilZeile> zeilen, bool mitPruefung = true, Action? geaendert = null)
        => Render<BedarfsProfileDialog>(p =>
        {
            p.Add(x => x.Art, art)
             .Add(x => x.Zeilen, zeilen)
             .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung", "Typ 1", 70, 50))
             .Add(x => x.Geaendert, geaendert);
            if (mitPruefung) p.Add(x => x.TemperaturPruefen, Pruefen);
        });

    private static IElement Knopf(IRenderedComponent<BedarfsProfileDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Die_Projektzeile_zeigt_ihr_Paar_und_uebernimmt_ein_neues()
    {
        var zeile = new BedarfsProfilZeile { IdZ = 1, IdStamm = 3, Name = "Trocknung", Summe = 10, Vorlauf = 80, Ruecklauf = 60 };
        int geaendert = 0;
        var cut = Projektdialog(BedarfsArt.Prozesswaerme, new List<BedarfsProfilZeile> { zeile },
                                geaendert: () => geaendert++);

        Assert.Contains("Temperaturniveau des Prozesses", cut.Markup);
        Assert.Equal("80 / 60 °C", cut.Instance.InfoTemperatur);

        // Der neue Jahresverbrauch, dann Vorlauf und Rücklauf.
        var felder = cut.FindAll("input[inputmode=decimal]");
        Assert.Equal(3, felder.Count);
        felder[1].Input("120");
        cut.FindAll("input[inputmode=decimal]")[2].Input("90");
        Knopf(cut, "Temperaturen übernehmen").Click();

        Assert.Equal(120, zeile.Vorlauf);
        Assert.Equal(90, zeile.Ruecklauf);
        Assert.True(zeile.TemperaturGeaendert);
        Assert.Equal(1, geaendert);
        Assert.Equal("120 / 90 °C", cut.Instance.InfoTemperatur);
        Assert.Contains("gespeichert wird mit OK", cut.Instance.Meldung);
    }

    [Fact]
    public void Ein_unzulaessiges_Paar_meldet_und_aendert_nichts()
    {
        var zeile = new BedarfsProfilZeile { IdZ = 1, IdStamm = 3, Name = "Bad", Summe = 10 };
        var cut = Projektdialog(BedarfsArt.Prozesswaerme, new List<BedarfsProfilZeile> { zeile });

        Assert.Equal("ohne", cut.Instance.InfoTemperatur);
        cut.FindAll("input[inputmode=decimal]")[1].Input("40");
        cut.FindAll("input[inputmode=decimal]")[2].Input("60");
        Knopf(cut, "Temperaturen übernehmen").Click();

        Assert.Equal("Vorlauf unter Rücklauf", cut.Instance.Meldung);
        Assert.Null(zeile.Vorlauf);
        Assert.False(zeile.TemperaturGeaendert);
    }

    [Theory]
    [InlineData(BedarfsArt.Brauchwasser, true)]
    [InlineData(BedarfsArt.Stromverbraucher, true)]
    [InlineData(BedarfsArt.Prozesswaerme, false)]
    public void Ohne_Prozesswaerme_oder_ohne_Pruefung_kein_Temperaturniveau(BedarfsArt art, bool mitPruefung)
    {
        var cut = Projektdialog(art, new List<BedarfsProfilZeile> { new() { IdZ = 1, Name = "X", Summe = 1 } },
                                mitPruefung);
        Assert.DoesNotContain("Temperaturniveau des Prozesses", cut.Markup);
        Assert.Single(cut.FindAll("input[inputmode=decimal]"));
    }

    private IRenderedComponent<TypStammDialog> Stammkopf(BedarfsArt art, TypStammDaten daten,
                                                        Func<TypStammDaten, bool, string, EPOS.UI.Dialoge.Erzeuger.KatalogSpeicherErgebnis> speichern)
        => Render<TypStammDialog>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Modus, EPOS.UI.Dialoge.Erzeuger.KatalogModus.Bearbeiten)
            .Add(x => x.Typen, () => new[] { "Typ 1" })
            .Add(x => x.Speichern, speichern)
            .Add(x => x.TemperaturPruefen, Pruefen));

    [Fact]
    public void Der_Stammkopf_prueft_und_schreibt_das_Paar_der_Prozesswaerme()
    {
        var daten = new TypStammDaten { Art = BedarfsArt.Prozesswaerme, Name = "P", Typ = "Typ 1", Vorlauf = 70, Ruecklauf = 50 };
        for (int m = 0; m < 12; m++) daten.Monat[m] = 1;
        TypStammDaten? geschrieben = null;
        var cut = Stammkopf(BedarfsArt.Prozesswaerme, daten,
                            (d, neu, bez) => { geschrieben = d; return new(true, "", bez); });

        Assert.Contains("Temperaturniveau des Prozesses", cut.Markup);
        var felder = cut.FindAll("input[inputmode=decimal]");
        Assert.Equal(14, felder.Count);

        // Halbes Paar: gemeldet, nicht geschrieben.
        felder[13].Input("");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Überschreiben").Click();
        Assert.Null(geschrieben);
        Assert.Equal("Paar unvollständig", cut.Instance.Meldung);

        cut.FindAll("input[inputmode=decimal]")[13].Input("45");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Überschreiben").Click();
        Assert.NotNull(geschrieben);
        Assert.Equal(70, geschrieben!.Vorlauf);
        Assert.Equal(45, geschrieben.Ruecklauf);
    }

    [Fact]
    public void Der_Stammkopf_der_anderen_zeigt_kein_Paar()
    {
        var daten = new TypStammDaten { Art = BedarfsArt.Brauchwasser, Name = "B", Typ = "Typ 1" };
        var cut = Stammkopf(BedarfsArt.Brauchwasser, daten, (d, n, b) => new(true, "", b));
        Assert.DoesNotContain("Temperaturniveau des Prozesses", cut.Markup);
        Assert.Equal(12, cut.FindAll("input[inputmode=decimal]").Count);
    }
}
