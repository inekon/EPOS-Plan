using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Berichte;

/// <summary>
/// Der kleine Dialog „Variante umbenennen" (Anwenderauftrag 13.10.2026): nur die Varianten
/// zur Wahl, die Vorgabe, der vorbelegte Bezeichner, der einem Wahlwechsel folgt — außer
/// der Anwender hat getippt (Von-Hand-Merker) —, der Zielname mit der Id der Variante, OK,
/// Abbrechen, Meldung und der Fall ohne Gaben.
/// </summary>
public class VarianteUmbenennenDialogTests : EposBunitContext
{
    private static readonly (int Id, string Text)[] VARIANTEN =
    {
        (1031, "WP klein"),
        (1032, "WP groß")
    };

    public VarianteUmbenennenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<VarianteUmbenennenDialog> Aufbauen(
        Action<VarianteUmbenennenWahl?>? beimSchliessen = null,
        Action<ComponentParameterCollectionBuilder<VarianteUmbenennenDialog>>? mehr = null)
        => Render<VarianteUmbenennenDialog>(p =>
        {
            p.Add(x => x.Varianten, VARIANTEN);
            p.Add(x => x.Zielname, (int id, string b) => "Musterhaus - " + b + " [" + id + "]");
            p.Add(x => x.HinweisFormat, "Neuer Bezeichner für die Variante „{0}“.");
            if (beimSchliessen is not null) p.Add(x => x.Geschlossen, beimSchliessen);
            mehr?.Invoke(p);
        });

    private static IElement Wahl(IRenderedComponent<VarianteUmbenennenDialog> cut) => cut.Find("select");
    private static IElement Feld(IRenderedComponent<VarianteUmbenennenDialog> cut) => cut.Find("input[type=text]");
    private static IElement Ok(IRenderedComponent<VarianteUmbenennenDialog> cut)
        => cut.Find(".epos-dialog > .epos-leiste .epos-knopf--primaer");
    private static IElement Abbrechen(IRenderedComponent<VarianteUmbenennenDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button").First(k => !k.ClassList.Contains("epos-knopf--primaer"));

    [Fact]
    public void Die_erste_Variante_ist_vorbelegt_samt_ihrem_Bezeichner_und_Hinweis()
    {
        var cut = Aufbauen();

        Assert.Equal("Variante umbenennen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(2, Wahl(cut).QuerySelectorAll("option").Length);
        Assert.Equal(1031, cut.Instance.VarianteId);
        Assert.Equal("WP klein", cut.Instance.Bezeichner);
        Assert.Equal("WP klein", Feld(cut).GetAttribute("value"));
        Assert.Equal("Neuer Bezeichner für die Variante „WP klein“.", cut.Instance.HinweisZeile);
        Assert.True(cut.Instance.OkErlaubt);
        Assert.Equal("Neuer Projektname: Musterhaus - WP klein [1031]", cut.Instance.Zielzeile);
    }

    [Fact]
    public void Die_Vorgabe_waehlt_die_genannte_Variante()
    {
        var cut = Aufbauen(mehr: p => p.Add(x => x.VorgabeVariante, 1032));

        Assert.Equal(1032, cut.Instance.VarianteId);
        Assert.Equal("WP groß", cut.Instance.Bezeichner);
    }

    /// <summary>Ein Wahlwechsel zieht den Bezeichner nach — bis der Anwender selbst tippt.</summary>
    [Fact]
    public void Ein_Wahlwechsel_zieht_den_Bezeichner_nach_ein_getippter_bleibt_stehen()
    {
        var cut = Aufbauen();

        Wahl(cut).Change("1032");
        Assert.Equal("WP groß", cut.Instance.Bezeichner);
        Assert.Equal("Neuer Bezeichner für die Variante „WP groß“.", cut.Instance.HinweisZeile);

        Feld(cut).Input("Meine Fassung");
        Wahl(cut).Change("1031");
        Assert.Equal("Meine Fassung", cut.Instance.Bezeichner);
        Assert.Equal(1031, cut.Instance.VarianteId);
    }

    [Fact]
    public void Ohne_Bezeichner_bleibt_OK_gesperrt()
    {
        var cut = Aufbauen();

        Feld(cut).Input("   ");
        Assert.False(cut.Instance.OkErlaubt);
        Assert.True(Ok(cut).HasAttribute("disabled"));
        Assert.Equal("", cut.Instance.Zielzeile);
    }

    [Fact]
    public void OK_liefert_Variante_und_getrimmten_Bezeichner()
    {
        VarianteUmbenennenWahl? ergebnis = null;
        var cut = Aufbauen(w => ergebnis = w);

        Wahl(cut).Change("1032");
        Feld(cut).Input("  Kessel klein  ");
        Ok(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(1032, ergebnis!.Value.VarianteId);
        Assert.Equal("Kessel klein", ergebnis.Value.Bezeichner);
    }

    [Fact]
    public void Abbrechen_und_Kreuz_melden_nichts()
    {
        int gemeldet = 0;
        VarianteUmbenennenWahl? ergebnis = new VarianteUmbenennenWahl(1, "x");
        var cut = Aufbauen(w => { gemeldet++; ergebnis = w; });

        Abbrechen(cut).Click();
        Assert.Equal(1, gemeldet);
        Assert.Null(ergebnis);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(2, gemeldet);
    }

    [Fact]
    public void Eine_hereingereichte_Meldung_steht_als_Banner()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-warnbanner"));

        var cut = Aufbauen(mehr: p => p.Add(x => x.Meldung, "Fehler beim Umbenennen."));
        Assert.Equal("Fehler beim Umbenennen.", cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_bleibt_gesperrt()
    {
        var cut = Render<VarianteUmbenennenDialog>();

        Assert.Empty(Wahl(cut).QuerySelectorAll("option"));
        Assert.Equal(0, cut.Instance.VarianteId);
        Assert.Equal("", cut.Instance.HinweisZeile);
        Feld(cut).Input("Neu");
        Assert.False(cut.Instance.OkErlaubt);
    }
}
