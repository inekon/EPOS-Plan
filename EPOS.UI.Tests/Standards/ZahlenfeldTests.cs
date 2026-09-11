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
    [Fact]
    public void Ohne_Angabe_ist_das_Feld_bedienbar()
    {
        var cut = Render<Zahlenfeld>();

        Assert.False(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Aktiv_false_sperrt_das_Feld_laesst_den_Wert_aber_lesbar()
    {
        // iU9-W2.3: Der Tarifdialog sperrt den Block des nicht gewaehlten
        // Rechenmodells, statt ihn auszublenden - die Werte des anderen Modells
        // bleiben lesbar und erhalten (Form_Tarifstruktur.ModusUebernehmen).
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Wert, 0.2345)
            .Add(x => x.Nachkommastellen, 4)
            .Add(x => x.Aktiv, false));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.Equal("0,2345", cut.Find("input").GetAttribute("value"));
    }

    // =====================================================================
    //  Hoechstens vier Nachkommastellen (Auftrag #224, Konzept 7.8)
    // =====================================================================

    /// <summary>
    /// AUFTRAG #224 (Anwenderwunsch 11.09.2026): Das Bildschirmfoto der
    /// Stromspeicher-Auslegung zeigte <c>94,86832980505137 %</c>,
    /// <c>89,99999999999999 %</c> und <c>0,31746000000002055</c> — keine dieser Zahlen
    /// hat jemand getippt; sie entstehen als Wurzel, als Quotient und als Mittelwert.
    /// Ohne eigene Vorgabe zeigt das Feld seither HÖCHSTENS vier Stellen. Die Regel
    /// gilt HAUSWEIT, nicht nur für diese Ansicht.
    /// </summary>
    [Theory]
    [InlineData(0.31746000000002055, "0,3175")]
    [InlineData(94.86832980505137, "94,8683")]
    [InlineData(89.99999999999999, "90")]
    [InlineData(1234.5, "1234,5")]
    [InlineData(-0.00005, "-0,0001")]
    [InlineData(42.0, "42")]
    public void Ohne_Vorgabe_zeigt_das_Feld_hoechstens_vier_Nachkommastellen(double wert, string text)
    {
        var cut = Render<Zahlenfeld>(p => p.Add(x => x.Wert, wert));

        Assert.Equal(text, cut.Find("input").GetAttribute("value"));
    }

    /// <summary>
    /// Der GESPEICHERTE Wert bleibt: Gerundet wird die Anzeige, nicht der Stand — erst
    /// eine Eingabe ändert ihn. Ohne Eingabe meldet das Feld deshalb GAR NICHTS.
    /// </summary>
    [Fact]
    public void Die_Rundung_aendert_den_gespeicherten_Wert_nicht()
    {
        double? gemeldet = null;
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Wert, 0.31746000000002055)
            .Add(x => x.WertChanged, (double? w) => gemeldet = w));

        Assert.Equal("0,3175", cut.Find("input").GetAttribute("value"));
        Assert.Null(gemeldet);
        Assert.Equal(0.31746000000002055, cut.Instance.Wert!.Value, 15);
    }

    /// <summary>
    /// Eine EINGABE mit mehr Stellen bleibt stehen, solange der Anwender tippt — sonst
    /// spränge ihm der Text unter den Fingern weg.
    /// </summary>
    [Fact]
    public void Eine_laengere_Eingabe_wird_nicht_beschnitten()
    {
        double? gemeldet = null;
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.WertChanged, (double? w) => gemeldet = w));

        cut.Find("input").Input("0,123456");

        Assert.Equal(0.123456, gemeldet);
        Assert.Equal("0,123456", cut.Find("input").GetAttribute("value"));
    }

    /// <summary>Die eigene Vorgabe schlägt die Höchstzahl — Prozente stehen auf zwei.</summary>
    [Fact]
    public void Eine_eigene_Stellenzahl_gilt_weiterhin()
    {
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.Wert, 94.86832980505137)
            .Add(x => x.Nachkommastellen, 2));

        Assert.Equal("94,87", cut.Find("input").GetAttribute("value"));
    }
}
