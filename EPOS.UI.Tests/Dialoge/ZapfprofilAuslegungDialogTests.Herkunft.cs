using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Karte „Herkunft" der Überlagerung „Auslegung" (Umsetzungskonzept Zapfprofilgenerator N19):
/// dieselbe Karte wie im Ergebnisbereich des Zapfprofils, mit dem Herkunftsprotokoll der Auslegung.
///
/// <para><b>Die Überlagerung führt keine eigene Stufe</b>, deshalb entscheidet die Hülle über
/// <see cref="ZapfprofilAuslegungDaten.HerkunftSichtbar"/>, ob die Karte steht — der Dialog fragt
/// nur das Kennzeichen. Leer und sichtbar heißt benannt „nichts zu vermerken".</para>
/// </summary>
public partial class ZapfprofilAuslegungDialogTests
{
    /// <summary>
    /// Ohne das Kennzeichen der Hülle (Stufe Einfach) steht keine Karte; mit ihm steht sie und trägt
    /// je Eintrag eine Zeile in der Reihenfolge des Protokolls.
    /// </summary>
    [Fact]
    public void Die_Herkunftskarte_der_Auslegung_steht_nur_mit_dem_Kennzeichen_der_Huelle()
    {
        ZapfprofilAuslegungDaten ohne = Ergebnis(Speichergruppe());
        Assert.False(ohne.HerkunftSichtbar);
        Assert.Empty(Aufbauen(Start(ohne)).FindAll("details.epos-zapfausl-herkunft"));

        ZapfprofilAuslegungDaten mit = Ergebnis(Speichergruppe());
        mit.HerkunftSichtbar = true;
        mit.Herkunft.Add(new ZapfprofilHerkunftZeile("Auslegung.SpeicherC", "Projekt", "60 °C", "überschrieben",
                                                     "Eingabe des Anwenders", ""));
        mit.Herkunft.Add(new ZapfprofilHerkunftZeile("Auslegung.ErzeugerKw", "Projekt", "25 kW", "Vorgabe",
                                                     "Testquelle 2020 · erfundener Wert", "Schätzformel Kessel"));

        IRenderedComponent<ZapfprofilAuslegungDialog> cut = Aufbauen(Start(mit));
        IElement karte = cut.Find("details.epos-zapfausl-herkunft");
        Assert.Equal("Herkunft", karte.QuerySelector("summary")!.TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-zapfprofil-herkunft-leer"));

        IReadOnlyList<IElement> zeilen = cut.FindAll("tr.epos-zapfprofil-herkunftzeile");
        Assert.Equal(new[] { "Auslegung.SpeicherC", "Auslegung.ErzeugerKw" },
                     zeilen.Select(z => z.GetAttribute("data-groesse")).ToArray());
        Assert.Contains("Schätzformel Kessel", zeilen[1].TextContent, StringComparison.Ordinal);
        Assert.Equal(new[] { "Größe", "Wert", "Zone", "Stand", "Quelle", "Vermerk" },
                     karte.QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()).ToArray());
    }

    /// <summary>Sichtbar und leer: Die Karte sagt „nichts zu vermerken" statt zu schweigen.</summary>
    [Fact]
    public void Ohne_Eintrag_nennt_die_Herkunftskarte_der_Auslegung_dass_nichts_zu_vermerken_ist()
    {
        ZapfprofilAuslegungDaten d = Ergebnis(Speichergruppe());
        d.HerkunftSichtbar = true;

        IRenderedComponent<ZapfprofilAuslegungDialog> cut = Aufbauen(Start(d));
        Assert.Single(cut.FindAll("details.epos-zapfausl-herkunft"));
        Assert.Empty(cut.FindAll("table.epos-zapfprofil-herkunftliste"));
        Assert.Equal("nichts zu vermerken", cut.Find(".epos-zapfprofil-herkunft-leer").TextContent.Trim());
    }
}
