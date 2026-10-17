using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// <b>Die Zahlenregel des Hauses</b> (<see cref="Zahlen.ZahlParsen"/>; Stufe KP2, Welle U1, Nebenbefund
/// aus U0b): Eine Zahl ist ENDLICH. „NaN", „Infinity" und ein Überlauf wie „1e400" sind keine Eingabe —
/// „aus" läuft allein über den <c>AusText</c> des <see cref="Zahlenfeld"/>s, sonst trüge ein getipptes
/// „NaN" still den Zustand „aus" in einen Satz, der ihn nicht kennt. Dieselbe Regel trifft jeden
/// Aufrufer (Zahlenfeld, Katalogfelder, Katalogbrowser, Modulkatalog und ihre Sichtklassen).
/// </summary>
public class ZahlenTests : BunitContext
{
    [Theory]
    [InlineData("NaN")]
    [InlineData("nan")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("∞")]
    [InlineData("1e400")]
    [InlineData("-1e400")]
    public void Nur_endliche_Zahlen_gelten(string text)
        => Assert.False(Zahlen.ZahlParsen(text, out _), text);

    [Theory]
    [InlineData("1,5", 1.5)]
    [InlineData("2.25", 2.25)]
    [InlineData("-3", -3.0)]
    [InlineData("1e3", 1000.0)]
    [InlineData(" 0,0001 ", 0.0001)]
    public void Endliche_Zahlen_bleiben_wie_sie_sind(string text, double wert)
    {
        Assert.True(Zahlen.ZahlParsen(text, out double d), text);
        Assert.Equal(wert, d);
    }

    /// <summary>Im Zahlenfeld ohne „aus" färbt „NaN" das Feld und meldet nichts.</summary>
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e400")]
    public void Das_Zahlenfeld_ohne_aus_faerbt_nicht_endliche_Texte(string text)
    {
        int meldungen = 0;
        var cut = Render<Zahlenfeld>(p => p.Add(x => x.WertChanged, (double? _) => meldungen++));

        cut.Find("input").Input(text);

        Assert.Equal(0, meldungen);
        Assert.True(cut.Instance.Fehlerhaft);
        Assert.Contains("epos-fehleingabe", cut.Find("input").ClassName);
    }

    /// <summary>Mit „aus" gilt allein der AusText; „NaN" ist dort ebenso eine Fehleingabe.</summary>
    [Fact]
    public void Aus_laeuft_allein_ueber_den_AusText()
    {
        double? erhalten = 0;
        var cut = Render<Zahlenfeld>(p => p
            .Add(x => x.AusText, "aus")
            .Add(x => x.WertChanged, (double? w) => erhalten = w));

        cut.Find("input").Input("NaN");
        Assert.True(cut.Instance.Fehlerhaft);
        Assert.Equal(0.0, erhalten);

        cut.Find("input").Input("aus");
        Assert.False(cut.Instance.Fehlerhaft);
        Assert.True(erhalten.HasValue && double.IsNaN(erhalten.Value));
    }
}
