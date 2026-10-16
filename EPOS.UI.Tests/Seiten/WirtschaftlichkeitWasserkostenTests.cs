using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using W = WindowsFormsApplication1;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Welle KB-7c1 — die Wasserkosten des Rückkühlwerks auf der Wirtschaftlichkeitsseite</b> (Entwurf
/// <c>Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md</c>, Abschnitt 5): Unter den Betriebskosten steht die
/// Unterzeile „davon Wasser Rückkühlwerk" mit dem Betrag und die Herleitung „Menge × Preis" — aus der Zeilendefinition des
/// Kerns über dieselbe Abbildung der Hülle, die die Seite bekommt; ohne Wasserkosten keine Zeile. Der Hinweis „ohne Preis"
/// steht als Hinweiszeile der Gliederung.
/// </summary>
public class WirtschaftlichkeitWasserkostenTests : EposBunitContext
{
    public WirtschaftlichkeitWasserkostenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const int STAMM = 1050;
    private const int KM = 1055;
    private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Die Nachweiszeile, die die Wirtschaftlichkeit des Kerns an die Betriebskosten hängt (Satzherkunft
    /// „WASSERTRAEGER", im Nachweisumschlag gespeichert).</summary>
    private static W.KostenPositionNachweis Wasserzeile(double menge, double preis) => new()
    {
        Bezeichnung = "Wasser Rückkühlwerk: " + menge.ToString("N1", DE) + " m³ × " + preis.ToString("N2", DE) + " €/m³",
        Kostenart = W.DbWerte.KOSTENART_BEDARFSGEBUNDEN,
        Menge = menge,
        Einheitpreis = preis,
        SatzHerkunft = "WASSERTRAEGER",
        BetragJahr = menge * preis
    };

    private static List<W.WirtschaftlichkeitErgebnis> Spalten(bool mitWasser)
    {
        var stamm = new W.WirtschaftlichkeitErgebnis { IdProjekt = STAMM, IstStamm = true, BetriebskostenJahr = 1000 };
        var km = new W.WirtschaftlichkeitErgebnis { IdProjekt = KM, BetriebskostenJahr = 3500 };
        if (mitWasser) km.Betriebskosten.Add(Wasserzeile(1000, 2.5));
        return new List<W.WirtschaftlichkeitErgebnis> { stamm, km };
    }

    private IRenderedComponent<WirtschaftlichkeitSeite> Zeige(List<W.WirtschaftlichkeitErgebnis> spalten, MatrixZeile? hinweis = null)
    {
        var zeilen = W.WirtschaftlichkeitZeilen.Kennzahlen(spalten, new W.TarifParameter(), 0, true)
            .Where(z => z.Schluessel.StartsWith("BETRIEBSKOSTEN", StringComparison.Ordinal))
            .Select(z => W.WirtschaftlichkeitSeiteGaben.Matrixzeile(z, spalten, DE, "B"))
            .ToList();
        if (hinweis is not null) zeilen.Add(hinweis);
        var stand = new WirtschaftlichkeitStand
        {
            Varianten = new[]
            {
                new VarianteZeile { IdProjekt = STAMM, Art = "Stamm", Bezeichner = "(Stammprojekt)", Projektname = "Kälte", IstStamm = true },
                new VarianteZeile { IdProjekt = KM, Art = "Variante", Bezeichner = "Rückkühlwerk", Projektname = "Kälte" }
            },
            GewaehlteVarianten = new[] { STAMM, KM },
            Szenarien = new[] { (0, "Erwartet") },
            SzenarioId = 0,
            HatErgebnisse = true,
            Ansicht = new ErgebnisAnsicht
            {
                Matrix = new ErgebnisMatrix { Spalten = new[] { "Position", "Stamm", "Rückkühlwerk" }, Zeilen = zeilen }
            }
        };
        return Render<WirtschaftlichkeitSeite>(p => p.Add(x => x.Laden, () => stand));
    }

    private static IReadOnlyList<IElement> Gliederungszeilen(IRenderedComponent<WirtschaftlichkeitSeite> cut) =>
        cut.FindAll(".epos-wirt-gliederung tbody tr");

    [Fact]
    public void Die_Gliederung_zeigt_die_Wasserkosten_unter_den_Betriebskosten()
    {
        var cut = Zeige(Spalten(true));

        List<string> titel = Gliederungszeilen(cut).Select(z => z.QuerySelector("th, td")!.TextContent.Trim()).ToList();
        int bk = titel.FindIndex(t => t.StartsWith(W.MyResource.Resource.WIRT_ZEILE_BETRIEBSKOSTEN, StringComparison.Ordinal));
        Assert.True(bk >= 0, string.Join(" | ", titel));
        Assert.Equal("davon Wasser Rückkühlwerk", titel[bk + 1]);
        Assert.Contains("2.500", Gliederungszeilen(cut)[bk + 1].TextContent);
        Assert.Contains("1.000,0 m³/a × 2,50 €/m³", cut.Markup);
    }

    [Fact]
    public void Ohne_Wasserkosten_steht_keine_Wasserzeile()
    {
        var cut = Zeige(Spalten(false));

        Assert.DoesNotContain("Wasser Rückkühlwerk", cut.Markup);
        Assert.Contains(Gliederungszeilen(cut), z => z.TextContent.Contains(W.MyResource.Resource.WIRT_ZEILE_BETRIEBSKOSTEN));
    }

    [Fact]
    public void Der_Hinweis_ohne_Preis_steht_auf_der_Seite()
    {
        string hinweis = string.Format(DE, W.MyResource.Resource.WIRT_HINWEIS_WASSER_OHNE_PREIS, 1000.0.ToString("N1", DE));
        var zeile = new MatrixZeile
        {
            Titel = W.MyResource.Resource.WIRT_ZEILE_HINWEIS,
            Abschnitt = MatrixZeile.ABSCHNITT_HINWEIS,
            Zellen = new[] { "", "⚠ " + hinweis }
        };
        var cut = Zeige(Spalten(false), zeile);

        Assert.StartsWith("Wasser Rückkühlwerk 1.000,0 m³/a ohne Preis", hinweis, StringComparison.Ordinal);
        Assert.Contains(hinweis, cut.Markup);
    }
}
