using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Berichte;

/// <summary>
/// Der kleine Dialog „Variante löschen" (Anwenderauftrag 13.10.2026): nur die Varianten zur
/// Wahl, die Vorgabe, der Hinweis auf die Rückfrage, OK mit der Id, Abbrechen und Kreuz mit
/// <c>null</c> — und der Fall ohne Gaben (gesperrt). Gelöscht wird hier nichts; das tut der
/// Wirt nach seiner Rückfrage.
/// </summary>
public class VarianteLoeschenDialogTests : EposBunitContext
{
    private static readonly (int Id, string Text)[] VARIANTEN =
    {
        (1031, "WP klein"),
        (1032, "WP groß")
    };

    public VarianteLoeschenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VarianteLoeschenDialog> Aufbauen(
        Action<int?>? beimSchliessen = null,
        Action<ComponentParameterCollectionBuilder<VarianteLoeschenDialog>>? mehr = null)
        => Render<VarianteLoeschenDialog>(p =>
        {
            p.Add(x => x.Varianten, VARIANTEN);
            p.Add(x => x.HinweisText, "Die gewählte Variante wird nach einer Rückfrage gelöscht.");
            if (beimSchliessen is not null) p.Add(x => x.Geschlossen, beimSchliessen);
            mehr?.Invoke(p);
        });

    private static IElement Wahl(IRenderedComponent<VarianteLoeschenDialog> cut) => cut.Find("select");
    private static IElement Ok(IRenderedComponent<VarianteLoeschenDialog> cut)
        => cut.Find(".epos-dialog > .epos-leiste .epos-knopf--primaer");
    private static IElement Abbrechen(IRenderedComponent<VarianteLoeschenDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button").First(k => !k.ClassList.Contains("epos-knopf--primaer"));

    [Fact]
    public void Der_Dialog_zeigt_Hinweis_Variantenwahl_und_beide_Knoepfe()
    {
        var cut = Aufbauen();

        Assert.Equal("Variante löschen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Rückfrage", cut.Find(".epos-herleitung").TextContent);
        Assert.Equal(2, Wahl(cut).QuerySelectorAll("option").Length);
        Assert.Empty(cut.FindAll("input[type=text]"));
        Assert.Equal("Variante löschen", Ok(cut).TextContent.Trim());
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);
    }

    [Fact]
    public void Die_erste_Variante_ist_vorbelegt_oder_die_Vorgabe()
    {
        Assert.Equal(1031, Aufbauen().Instance.VarianteId);
        Assert.Equal(1032, Aufbauen(mehr: p => p.Add(x => x.VorgabeVariante, 1032)).Instance.VarianteId);
        Assert.True(Aufbauen().Instance.OkErlaubt);
    }

    [Fact]
    public void OK_liefert_die_gewaehlte_Variante()
    {
        int? ergebnis = null;
        var cut = Aufbauen(id => ergebnis = id);

        Wahl(cut).Change("1032");
        Ok(cut).Click();

        Assert.Equal(1032, ergebnis);
    }

    [Fact]
    public void Abbrechen_und_Kreuz_melden_nichts()
    {
        int gemeldet = 0;
        int? ergebnis = 1;
        var cut = Aufbauen(id => { gemeldet++; ergebnis = id; });

        Abbrechen(cut).Click();
        Assert.Equal(1, gemeldet);
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(2, gemeldet);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_bleibt_gesperrt()
    {
        int gemeldet = 0;
        var cut = Render<VarianteLoeschenDialog>(p => p.Add(x => x.Geschlossen, (int? _) => gemeldet++));

        Assert.Empty(Wahl(cut).QuerySelectorAll("option"));
        Assert.False(cut.Instance.OkErlaubt);
        Assert.True(Ok(cut).HasAttribute("disabled"));
        Assert.Equal(0, gemeldet);
    }
}
