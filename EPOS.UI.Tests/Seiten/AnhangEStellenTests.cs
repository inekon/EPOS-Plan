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
/// gewählten Vorlage</b>: Mit den Stellen der Hülle steht je Punkt deren Stelle — wie die Checkliste des
/// Kerns sie aus den Kapiteln der Vorlage baut — an der Stelle der eigenen, und die leise Zeile über der
/// Tafel nennt die Vorlage; ein Punkt ohne Stelle der Hülle behält die eigene. Ohne Delegat, ohne
/// Antwort oder bei einem Fehler gilt die Standardvorlage mit dem Zusatz „bezogen auf die
/// Standardvorlage". Die Stellen werden bei jedem Öffnen geholt, die Wirtschaftlichkeitsseite reicht den
/// Delegaten durch.
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

    private const string BEZUG = "Die Stellen im Bericht nennen die Kapitel der Vorlage „Kurzbericht“.";

    private const string STELLE_1 = "Wortbericht: „5 Wirtschaftliche Bewertung“ › „Kennzahlen im Szenario „Erwartet““ · "
                                  + "Tabellenbericht: Blatt „Wirtschaftlichkeit“, Block „Erwartet“";

    private const string STELLE_01 = "Wortbericht: nicht im Bericht · Tabellenbericht: Blatt „Übersicht“";

    private static AnhangEStellen Kurzbericht() => new(BEZUG, new Dictionary<string, string>
    {
        ["0.1"] = STELLE_01,
        ["1"] = STELLE_1
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

    /// <summary>
    /// Die eigene Stelle eines Punktes: die Spalte der Checkliste des Kerns ohne Kapitelstellen (die
    /// Ressourcen <c>WIRT_AE_*_STELLE</c> sind Muster mit <c>{0}</c>).
    /// </summary>
    private static string Standardstelle(string nummer)
        => WindowsFormsApplication1.AnhangECheckliste.Punkte(new WindowsFormsApplication1.ChecklistenLage())
              .Single(p => p.Nummer == nummer).Stelle;

    private static IReadOnlyList<string> LeiseZeilen(IRenderedComponent<AnhangEChecklisteKnopf> cut)
        => cut.FindAll(".epos-ueberlagerung .epos-herleitung-text")
              .Select(e => e.TextContent.Trim()).ToList();

    [Fact]
    public void Ohne_Delegat_bezieht_sich_die_Stelle_auf_die_Standardvorlage()
    {
        var cut = Oeffne(null);

        Assert.Contains(Resource.WIRT_AE_BEZUG_STANDARD, LeiseZeilen(cut));
        Assert.Equal("Die Stellen im Bericht sind bezogen auf die Standardvorlage.", Resource.WIRT_AE_BEZUG_STANDARD);
        Assert.Equal(Standardstelle("1"), Stelle(cut, "1").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-wirt-checkliste-stelle"));
    }

    /// <summary>
    /// Mit einer eigenen Vorlage ersetzt die Stelle der Hülle die eigene — eine Zeile, keine zweite
    /// leise darunter; die leise Zeile über der Tafel nennt die Vorlage.
    /// </summary>
    [Fact]
    public void Mit_eigener_Vorlage_ersetzt_die_Stelle_der_Vorlage_die_eigene()
    {
        var cut = Oeffne(Kurzbericht);

        Assert.Contains(BEZUG, LeiseZeilen(cut));
        Assert.DoesNotContain(Resource.WIRT_AE_BEZUG_STANDARD, LeiseZeilen(cut));

        IElement eins = Stelle(cut, "1");
        Assert.Equal(STELLE_1, eins.QuerySelector(".epos-wirt-checkliste-stelle")!.TextContent.Trim());
        Assert.Equal(STELLE_1, eins.TextContent.Trim());
        Assert.Null(eins.QuerySelector(".epos-herleitung-text"));
        Assert.NotEqual(Standardstelle("1"), eins.TextContent.Trim());

        Assert.Equal(STELLE_01, Stelle(cut, "0.1").QuerySelector(".epos-wirt-checkliste-stelle")!.TextContent.Trim());

        // Ein Punkt ohne Stelle der Hülle zeigt die eigene.
        IElement zweiA = Stelle(cut, "2a");
        Assert.Null(zweiA.QuerySelector(".epos-wirt-checkliste-stelle"));
        Assert.Equal(Standardstelle("2a"), zweiA.TextContent.Trim());

        // Leise stehen nur der Hinweis und die Bezugszeile über der Tafel - keine Zeile je Punkt.
        Assert.Equal(2, LeiseZeilen(cut).Count);
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
        Assert.Equal(Standardstelle("1"), Stelle(fehler, "1").TextContent.Trim());
    }

    [Fact]
    public void Die_Wirtschaftlichkeitsseite_reicht_den_Delegaten_an_die_Ueberlagerung()
    {
        var cut = Render<WirtschaftlichkeitSeite>(p => p.Add(x => x.AnhangEStellenLaden, Kurzbericht));

        cut.Find(".epos-wirt-abschnitt-fuss button.epos-wirt-checklistenknopf").Click();

        Assert.Contains(BEZUG, cut.FindAll(".epos-ueberlagerung .epos-herleitung-text").Select(e => e.TextContent.Trim()));
    }
}
