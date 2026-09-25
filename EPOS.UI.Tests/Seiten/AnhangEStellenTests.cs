using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// BV-E2 (Konzept Berichtsvorlagen 9.5, 11 Nr. 3) — <b>die Anhang-E-Überlagerung nennt die Stelle der
/// gewählten Vorlage</b>: Mit den Stellen der Hülle steht je Punkt die Überschrift des Kapitels der
/// Vorlage (oder „nicht im Bericht"), darunter leise die Stelle der Standardvorlage, und die leise Zeile
/// über der Tafel nennt die Vorlage; ohne Delegat, ohne Antwort oder bei einem Fehler gilt die
/// Standardvorlage mit dem Zusatz „bezogen auf die Standardvorlage". Die Stellen werden bei jedem Öffnen
/// geholt, die Wirtschaftlichkeitsseite reicht den Delegaten durch.
///
/// <para>Kultur de-DE (Hausvorrichtung); die Stellen der Hülle sind erfunden.</para>
/// </summary>
public class AnhangEStellenTests : EposBunitContext
{
    public AnhangEStellenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const string BEZUG = "Die Stellen im Bericht nennen die Kapitel der Vorlage „Kurzbericht“; die Zeile darunter nennt die Stelle in der Standardvorlage.";

    private static AnhangEStellen Kurzbericht() => new(BEZUG, new Dictionary<string, string>
    {
        ["0.1"] = "nicht im Bericht",
        ["1"] = "„5 Wirtschaftliche Bewertung“",
        ["11"] = "„5 Wirtschaftliche Bewertung“, „Anhang“ nicht im Bericht"
    });

    private IRenderedComponent<AnhangEChecklisteKnopf> Oeffne(Func<AnhangEStellen?>? laden)
    {
        var cut = Render<AnhangEChecklisteKnopf>(p =>
        {
            p.Add(x => x.Stand, new WirtschaftlichkeitStand());
            if (laden is not null) p.Add(x => x.StellenLaden, laden);
        });
        cut.Find("button.epos-wirt-checklistenknopf").Click();
        return cut;
    }

    /// <summary>Die Zelle „Stelle im Bericht" eines Punktes.</summary>
    private static IElement Stelle(IRenderedComponent<AnhangEChecklisteKnopf> cut, string nummer)
        => cut.FindAll("table.epos-wirt-checkliste tbody tr")
              .Where(z => !z.ClassList.Contains("epos-wirt-checkliste-gruppe"))
              .First(z => z.QuerySelector("td")!.TextContent == nummer)
              .QuerySelectorAll("td")[3];

    private static IReadOnlyList<string> LeiseZeilen(IRenderedComponent<AnhangEChecklisteKnopf> cut)
        => cut.FindAll(".epos-ueberlagerung .epos-herleitung-text")
              .Select(e => e.TextContent.Trim()).ToList();

    [Fact]
    public void Ohne_Delegat_bezieht_sich_die_Stelle_auf_die_Standardvorlage()
    {
        var cut = Oeffne(null);

        Assert.Contains(Resource.WIRT_AE_BEZUG_STANDARD, LeiseZeilen(cut));
        Assert.Equal("Die Stellen im Bericht sind bezogen auf die Standardvorlage.", Resource.WIRT_AE_BEZUG_STANDARD);
        Assert.Equal(Resource.WIRT_AE_1_STELLE, Stelle(cut, "1").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-wirt-checkliste-stelle"));
    }

    [Fact]
    public void Mit_eigener_Vorlage_nennt_die_Stelle_das_Kapitel_der_Vorlage_und_leise_die_Standardstelle()
    {
        var cut = Oeffne(Kurzbericht);

        Assert.Contains(BEZUG, LeiseZeilen(cut));
        Assert.DoesNotContain(Resource.WIRT_AE_BEZUG_STANDARD, LeiseZeilen(cut));

        IElement eins = Stelle(cut, "1");
        Assert.Equal("„5 Wirtschaftliche Bewertung“", eins.QuerySelector(".epos-wirt-checkliste-stelle")!.TextContent.Trim());
        Assert.Equal(Resource.WIRT_AE_1_STELLE, eins.QuerySelector(".epos-herleitung-text")!.TextContent.Trim());

        Assert.Equal("nicht im Bericht", Stelle(cut, "0.1").QuerySelector(".epos-wirt-checkliste-stelle")!.TextContent.Trim());
        Assert.StartsWith("„5 Wirtschaftliche Bewertung“, „Anhang“ nicht im Bericht",
                          Stelle(cut, "11").QuerySelector(".epos-wirt-checkliste-stelle")!.TextContent.Trim());

        // Ein Punkt ohne Stelle der Hülle zeigt die Standardstelle allein.
        IElement zweiA = Stelle(cut, "2a");
        Assert.Null(zweiA.QuerySelector(".epos-wirt-checkliste-stelle"));
        Assert.Equal(Resource.WIRT_AE_2A_STELLE, zweiA.TextContent.Trim());
    }

    [Fact]
    public void Die_Stellen_werden_bei_jedem_Oeffnen_geholt()
    {
        int geholt = 0;
        var cut = Oeffne(() => { geholt++; return Kurzbericht(); });
        Assert.Equal(1, geholt);

        cut.Find(".epos-ueberlagerung-zu").Click();
        cut.Find("button.epos-wirt-checklistenknopf").Click();
        Assert.Equal(2, geholt);
    }

    [Fact]
    public void Ohne_Antwort_oder_bei_einem_Fehler_gilt_die_Standardvorlage()
    {
        var ohne = Oeffne(() => null);
        Assert.Contains(Resource.WIRT_AE_BEZUG_STANDARD, LeiseZeilen(ohne));
        Assert.Empty(ohne.FindAll(".epos-wirt-checkliste-stelle"));

        var fehler = Oeffne(() => throw new InvalidOperationException("Kern nicht erreichbar"));
        Assert.Contains(Resource.WIRT_AE_BEZUG_STANDARD, LeiseZeilen(fehler));
        Assert.Equal(Resource.WIRT_AE_1_STELLE, Stelle(fehler, "1").TextContent.Trim());
    }

    [Fact]
    public void Die_Wirtschaftlichkeitsseite_reicht_den_Delegaten_an_die_Ueberlagerung()
    {
        var cut = Render<WirtschaftlichkeitSeite>(p => p.Add(x => x.AnhangEStellenLaden, Kurzbericht));

        cut.Find(".epos-wirt-abschnitt-fuss button.epos-wirt-checklistenknopf").Click();

        Assert.Contains(BEZUG, cut.FindAll(".epos-ueberlagerung .epos-herleitung-text").Select(e => e.TextContent.Trim()));
    }
}
