using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Berichte;

/// <summary>
/// Der kleine Dialog „Variante anlegen" der Gruppe „Variante verwalten" (Anwenderauftrag
/// 13.10.2026): Feldbestand (Quelle, Bezeichner, Zielname), die Vorgabe der Quelle (der
/// Stamm), die Sperre ohne Bezeichner, OK mit Quelle und Bezeichner, Abbrechen und Kreuz
/// mit <c>null</c>, die hereingereichte Meldung — und der Fall ohne Gaben.
/// </summary>
public class VarianteAnlegenDialogTests : EposBunitContext
{
    private static readonly (int Id, string Text)[] QUELLEN =
    {
        (1030, "Stammprojekt: Musterhaus"),
        (1031, "Variante: WP klein"),
        (1032, "Variante: WP groß")
    };

    public VarianteAnlegenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VarianteAnlegenDialog> Aufbauen(
        Action<VarianteAnlegenWahl?>? beimSchliessen = null,
        Action<ComponentParameterCollectionBuilder<VarianteAnlegenDialog>>? mehr = null)
        => Render<VarianteAnlegenDialog>(p =>
        {
            p.Add(x => x.Quellen, QUELLEN);
            p.Add(x => x.Zielname, (string b) => "Musterhaus - " + b);
            p.Add(x => x.HinweisText, "Die neue Variante gehört zur Gruppe des Stammprojekts „Musterhaus“.");
            if (beimSchliessen is not null) p.Add(x => x.Geschlossen, beimSchliessen);
            mehr?.Invoke(p);
        });

    private static IElement Wahl(IRenderedComponent<VarianteAnlegenDialog> cut) => cut.Find("select");
    private static IElement Feld(IRenderedComponent<VarianteAnlegenDialog> cut) => cut.Find("input[type=text]");
    private static IElement Ok(IRenderedComponent<VarianteAnlegenDialog> cut)
        => cut.Find(".epos-dialog > .epos-leiste .epos-knopf--primaer");
    private static IElement Abbrechen(IRenderedComponent<VarianteAnlegenDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button").First(k => !k.ClassList.Contains("epos-knopf--primaer"));

    [Fact]
    public void Der_Dialog_zeigt_Hinweis_Quelle_Bezeichner_und_beide_Knoepfe()
    {
        var cut = Aufbauen();

        Assert.Equal("Variante anlegen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Musterhaus", cut.Find(".epos-herleitung").TextContent);
        Assert.Equal(3, Wahl(cut).QuerySelectorAll("option").Length);
        Assert.Equal("Quelle:", cut.FindAll(".epos-feld-text")[0].TextContent);
        Assert.Equal("Bezeichner:", cut.FindAll(".epos-feld-text")[1].TextContent);
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);
    }

    /// <summary>Die Vorgabe ist der erste Eintrag — das Stammprojekt —, sonst die hereingereichte.</summary>
    [Fact]
    public void Die_Quelle_ist_mit_dem_Stamm_vorbelegt_oder_mit_der_Vorgabe()
    {
        Assert.Equal(1030, Aufbauen().Instance.QuelleId);
        Assert.Equal("1030", Wahl(Aufbauen()).GetAttribute("value"));

        var mitVorgabe = Aufbauen(mehr: p => p.Add(x => x.VorgabeQuelle, 1032));
        Assert.Equal(1032, mitVorgabe.Instance.QuelleId);
    }

    [Fact]
    public void Ohne_Bezeichner_bleibt_OK_gesperrt_und_der_Zielname_kommt_aus_dem_Delegaten()
    {
        var cut = Aufbauen();

        Assert.False(cut.Instance.OkErlaubt);
        Assert.True(Ok(cut).HasAttribute("disabled"));
        Assert.Equal("", cut.Instance.Zielzeile);

        Feld(cut).Input("   ");
        Assert.False(cut.Instance.OkErlaubt);

        Feld(cut).Input("Sommer");
        Assert.True(cut.Instance.OkErlaubt);
        Assert.False(Ok(cut).HasAttribute("disabled"));
        Assert.Equal("Neuer Projektname: Musterhaus - Sommer", cut.Instance.Zielzeile);
    }

    [Fact]
    public void OK_liefert_Quelle_und_getrimmten_Bezeichner()
    {
        VarianteAnlegenWahl? ergebnis = null;
        var cut = Aufbauen(w => ergebnis = w);

        Wahl(cut).Change("1031");
        Feld(cut).Input("  Aus WP klein  ");
        Ok(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(1031, ergebnis!.Value.QuelleId);
        Assert.Equal("Aus WP klein", ergebnis.Value.Bezeichner);
    }

    [Fact]
    public void Abbrechen_und_Kreuz_melden_nichts()
    {
        int gemeldet = 0;
        VarianteAnlegenWahl? ergebnis = new VarianteAnlegenWahl(1, "x");
        var cut = Aufbauen(w => { gemeldet++; ergebnis = w; });

        Feld(cut).Input("Sommer");
        Abbrechen(cut).Click();
        Assert.Equal(1, gemeldet);
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(2, gemeldet);
    }

    /// <summary>Die Meldung des Wirts steht als Banner im Dialog; ohne Meldung kein Banner.</summary>
    [Fact]
    public void Eine_hereingereichte_Meldung_steht_als_Banner()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-warnbanner"));

        var cut = Aufbauen(mehr: p => p.Add(x => x.Meldung, "Der Name ist schon vergeben."));
        Assert.Equal("Der Name ist schon vergeben.", cut.Find(".epos-warnbanner-text").TextContent);
    }

    /// <summary>Ein leerer Titel: die Überlagerung trägt Titel und Kreuz, der Dialog keins von beiden.</summary>
    [Fact]
    public void Ein_leerer_Titel_laesst_Kopf_und_Kreuz_aus()
    {
        var cut = Aufbauen(mehr: p => p.Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-dialog-kopf--ohnetitel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    /// <summary>Ohne Gaben zeichnet der Dialog und bleibt gesperrt (keine Quelle, kein Bezeichner).</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_bleibt_gesperrt()
    {
        var cut = Render<VarianteAnlegenDialog>();

        Assert.Empty(Wahl(cut).QuerySelectorAll("option"));
        Assert.Equal(0, cut.Instance.QuelleId);
        Feld(cut).Input("Sommer");
        Assert.False(cut.Instance.OkErlaubt);
        Assert.Equal("", cut.Instance.Zielzeile);
    }
}
