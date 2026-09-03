using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// Zahlenfeld - die Hausregel aus Program.ZahlParsen: Komma ODER Punkt, kein
/// Tausendertrennzeichen, Fehleingabe faerbt statt zu melden.
/// </summary>
public class ZahlenfeldTests : BunitContext
{
    [Fact]
    public void Komma_wird_als_Dezimaltrenner_akzeptiert()
    {
        double? erhalten = null;
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Bezeichnung, "Preis")
            .Add(x => x.WertChanged, (double? w) => erhalten = w));

        cut.Find("input").Input("1,5");

        Assert.Equal(1.5, erhalten);
        Assert.False(cut.Instance.Fehlerhaft);
        Assert.DoesNotContain("epos-fehleingabe", cut.Find("input").ClassName);
    }

    [Fact]
    public void Punkt_wird_als_Dezimaltrenner_akzeptiert()
    {
        double? erhalten = null;
        var cut = Render<Zahlenfeld>(p => p.Add(x => x.WertChanged, (double? w) => erhalten = w));

        cut.Find("input").Input("2.25");

        Assert.Equal(2.25, erhalten);
        Assert.False(cut.Instance.Fehlerhaft);
    }

    [Fact]
    public void Tausendertrennzeichen_ist_ungueltig()
    {
        // "1.234,5" wuerde mit double.Parse(CurrentCulture) still zu 12345 -
        // genau das lehnt die Hausregel ab (Program.cs:449-462).
        double? erhalten = null;
        var cut = Render<Zahlenfeld>(p => p.Add(x => x.WertChanged, (double? w) => erhalten = w));

        cut.Find("input").Input("1.234,5");

        Assert.Null(erhalten);
        Assert.True(cut.Instance.Fehlerhaft);
        Assert.Contains("epos-fehleingabe", cut.Find("input").ClassName);
    }

    [Fact]
    public void Text_faerbt_das_Feld_statt_zu_melden()
    {
        var cut = Render<Zahlenfeld>();

        cut.Find("input").Input("abc");

        Assert.True(cut.Instance.Fehlerhaft);
        Assert.Contains("epos-fehleingabe", cut.Find("input").ClassName);
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Leeres_Feld_ist_neutral_und_meldet_null()
    {
        double? erhalten = 7.0;
        bool gemeldet = false;
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Wert, 7.0)
            .Add(x => x.WertChanged, (double? w) => { erhalten = w; gemeldet = true; }));

        cut.Find("input").Input("");

        Assert.True(gemeldet);
        Assert.Null(erhalten);
        Assert.False(cut.Instance.Fehlerhaft);
    }

    [Fact]
    public void Wert_ausserhalb_des_Bereichs_faerbt_und_meldet_nicht()
    {
        double? erhalten = null;
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Min, 0.0)
            .Add(x => x.Max, 100.0)
            .Add(x => x.WertChanged, (double? w) => erhalten = w));

        cut.Find("input").Input("150");

        Assert.Null(erhalten);
        Assert.True(cut.Instance.Fehlerhaft);
    }

    [Fact]
    public void Anzeige_nutzt_Komma_und_die_gewuenschten_Nachkommastellen()
    {
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Wert, 1.5)
            .Add(x => x.Nachkommastellen, 2));

        Assert.Equal("1,50", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Einheit_steht_hinter_dem_Feld()
    {
        var cut = Render<Zahlenfeld>(p => p.Add(x => x.Einheit, "kWh"));

        Assert.Equal("kWh", cut.Find(".epos-einheit").TextContent);
    }
}
