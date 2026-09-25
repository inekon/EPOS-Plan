using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Karte „Herkunft" des Ergebnisbereichs (Umsetzungskonzept Zapfprofilgenerator N19): das
/// Herkunftsprotokoll des Kerns als Zeilen — eine je Eintrag, in der Reihenfolge des Rechenwegs.
///
/// <para>Drei Fälle: die Karte steht <b>ab Stufe Erweitert</b> und trägt je Eintrag eine Zeile; ohne
/// Eintrag sagt sie benannt „nichts zu vermerken"; in englischer Kultur stehen ihre Beschriftungen
/// englisch. Die Zeilen kommen fertig aus der Hülle — der Dialog übersetzt nichts.</para>
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    private static ZapfprofilHerkunftZeile[] Protokoll() => new[]
    {
        new ZapfprofilHerkunftZeile("Bezugsmenge", "Wohnen", "20 P", "Vorgabe", "Eingabe des Anwenders", ""),
        new ZapfprofilHerkunftZeile("Tagesbedarf", "Wohnen", "100 kWh/d", "umgerechnet",
                                    "Testquelle 2020 · Katalogfassung TEST-1 · erfundener Wert",
                                    "Der Katalogwert ist auf 50 °C gebracht."),
        new ZapfprofilHerkunftZeile("Zirkulation.Jahresverlust", "Projekt", "1200 kWh", "Vorgabe", "Eigenkonstruktion", "")
    };

    /// <summary>
    /// Die Karte steht nicht in der Stufe Einfach und ab Erweitert mit einer Zeile je Eintrag —
    /// Größe, Wert, Zone, Stand, Quelle und Vermerk in der Reihenfolge des Protokolls.
    /// </summary>
    [Fact]
    public void Die_Herkunftskarte_steht_ab_Erweitert_mit_einer_Zeile_je_Eintrag()
    {
        ZapfprofilDaten d = Daten();
        d.Vorschau!.Herkunft.AddRange(Protokoll());
        var cut = Aufbauen(d, vorschau: e =>
        {
            ZapfprofilVorschauDaten v = Vorschau(e);
            v.Herkunft.AddRange(Protokoll());
            return v;
        });

        // Stufe Einfach: keine Karte - das Protokoll traegt je Zone ein Dutzend Zeilen.
        Assert.Empty(cut.FindAll("details.epos-zapfprofil-herkunft"));

        Stufe(cut, "Erweitert");
        IElement karte = cut.Find("details.epos-zapfprofil-herkunft");
        Assert.Equal("Herkunft", karte.QuerySelector("summary")!.TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-zapfprofil-herkunft-leer"));

        IReadOnlyList<IElement> zeilen = cut.FindAll("tr.epos-zapfprofil-herkunftzeile");
        Assert.Equal(3, zeilen.Count);
        Assert.Equal(new[] { "Bezugsmenge", "Tagesbedarf", "Zirkulation.Jahresverlust" },
                     zeilen.Select(z => z.GetAttribute("data-groesse")).ToArray());

        IElement zweite = zeilen[1];
        Assert.Equal("Tagesbedarf", zweite.QuerySelector("th")!.TextContent.Trim());
        string[] zellen = zweite.QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal("100 kWh/d", zellen[0]);
        Assert.Equal("Wohnen", zellen[1]);
        Assert.Equal("umgerechnet", zellen[2]);
        Assert.Equal("Testquelle 2020 · Katalogfassung TEST-1 · erfundener Wert", zellen[3]);
        Assert.Equal("Der Katalogwert ist auf 50 °C gebracht.", zellen[4]);

        // Die Spaltenköpfe stehen einmal, in der Reihenfolge der Zeilen.
        Assert.Equal(new[] { "Größe", "Wert", "Zone", "Stand", "Quelle", "Vermerk" },
                     karte.QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Ein leeres Protokoll ist nicht still: Die Karte steht und sagt „nichts zu vermerken" — eine
    /// weggelassene Karte wäre von einer fehlenden nicht zu unterscheiden.
    /// </summary>
    [Fact]
    public void Ohne_Eintrag_nennt_die_Herkunftskarte_dass_nichts_zu_vermerken_ist()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert");

        Assert.Single(cut.FindAll("details.epos-zapfprofil-herkunft"));
        Assert.Empty(cut.FindAll("table.epos-zapfprofil-herkunftliste"));
        Assert.Equal("nichts zu vermerken", cut.Find(".epos-zapfprofil-herkunft-leer").TextContent.Trim());
    }

    /// <summary>
    /// In englischer Kultur stehen Titel, Untertitel und Spaltenköpfe der Karte englisch — aus den
    /// englischen Ressourcen, wie jede andere Beschriftung.
    /// </summary>
    [Fact]
    public void Die_Herkunftskarte_steht_englisch_in_englischer_Kultur()
    {
        using var _ = new Kulturvorrichtung("en-US");
        ZapfprofilDaten d = Daten();
        d.Vorschau!.Herkunft.AddRange(Protokoll());
        var texte = new ZapfprofilTexte
        {
            GruppeHerkunft = Resource.ZPG_GRP_HERKUNFT,
            HerkunftUnter = Resource.ZPG_HERKUNFT_UNTER,
            HerkunftLeer = Resource.ZPG_HERKUNFT_LEER,
            HerkunftSpalteGroesse = Resource.ZPG_HERKUNFT_SP_GROESSE,
            HerkunftSpalteWert = Resource.ZPG_HERKUNFT_SP_WERT,
            HerkunftSpalteZone = Resource.ZPG_HERKUNFT_SP_ZONE,
            HerkunftSpalteStand = Resource.ZPG_HERKUNFT_SP_STAND,
            HerkunftSpalteQuelle = Resource.ZPG_HERKUNFT_SP_QUELLE,
            HerkunftSpalteVermerk = Resource.ZPG_HERKUNFT_SP_VERMERK
        };
        var cut = Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Texte, texte)
            .Add(x => x.Vorschau, e =>
            {
                ZapfprofilVorschauDaten v = Vorschau(e);
                v.Herkunft.AddRange(Protokoll());
                return v;
            })
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0));

        cut.Find("fieldset[aria-label='Stufe']").QuerySelectorAll("input")[1].Change("1");

        IElement karte = cut.Find("details.epos-zapfprofil-herkunft");
        Assert.Equal("Origin", karte.QuerySelector("summary")!.TextContent.Trim());
        Assert.Equal(new[] { "Quantity", "Value", "Zone", "State", "Source", "Note" },
                     karte.QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()).ToArray());
        Assert.Contains("Where every value", karte.TextContent, StringComparison.Ordinal);
        Assert.Equal(3, cut.FindAll("tr.epos-zapfprofil-herkunftzeile").Count);
    }
}
