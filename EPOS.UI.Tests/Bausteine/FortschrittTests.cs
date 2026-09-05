using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Fortschritt (iU9-W11a.7) - Balken, Text und Abbrechen einer laufenden Rechnung.
///
/// <para>Geprueft wird, was den Baustein ausmacht: der Unterschied zwischen
/// bestimmtem und unbestimmtem Balken, dass es OHNE Rueckruf keinen Knopf gibt, dass
/// ein Klick ihn ausloest, und die ARIA-Angaben, an denen ein Sprachausgabeprogramm
/// haengt.</para>
/// </summary>
public class FortschrittTests : BunitContext
{
    [Fact]
    public void Mit_Anteil_ist_der_Balken_bestimmt()
    {
        var cut = Render<Fortschritt>(p => p.Add(x => x.Anteil, 0.42));

        var balken = cut.Find("progress");
        Assert.Equal("42", balken.GetAttribute("value"));
        Assert.Equal("42", balken.GetAttribute("aria-valuenow"));
        Assert.Equal("progressbar", balken.GetAttribute("role"));
        Assert.DoesNotContain("epos-fortschritt-balken--unbestimmt", balken.ClassName);
    }

    /// <summary>
    /// Ohne Anteil laeuft der Balken. Ein <c>progress</c> OHNE <c>value</c> ist genau
    /// das - der Browser zeichnet den laufenden Balken selbst.
    /// </summary>
    [Fact]
    public void Ohne_Anteil_ist_der_Balken_unbestimmt()
    {
        var cut = Render<Fortschritt>(p => p.Add(x => x.Text, "Rechnet"));

        var balken = cut.Find("progress");
        Assert.Null(balken.GetAttribute("value"));
        Assert.Null(balken.GetAttribute("aria-valuenow"));
        Assert.Contains("epos-fortschritt-balken--unbestimmt", balken.ClassName);
    }

    /// <summary>Werte ausserhalb 0…1 werden geklemmt — „110 %" waere Unfug.</summary>
    [Theory]
    [InlineData(-0.5, "0")]
    [InlineData(0.0, "0")]
    [InlineData(1.0, "100")]
    [InlineData(1.7, "100")]
    public void Der_Anteil_wird_auf_null_bis_hundert_geklemmt(double anteil, string erwartet)
    {
        var cut = Render<Fortschritt>(p => p.Add(x => x.Anteil, anteil));

        Assert.Equal(erwartet, cut.Find("progress").GetAttribute("value"));
    }

    [Fact]
    public void Der_Text_steht_neben_dem_Balken()
    {
        var cut = Render<Fortschritt>(p => p.Add(x => x.Text, "Variante 3 von 7"));

        Assert.Equal("Variante 3 von 7", cut.Find(".epos-fortschritt-text").TextContent);
    }

    /// <summary>
    /// OHNE Rueckruf KEIN Knopf - dieselbe Regel wie bei Dateiwahl und Sprung. Ein
    /// Abbrechen, das nicht abbricht, waere eine Zusage, die niemand einloest.
    /// </summary>
    [Fact]
    public void Ohne_Rueckruf_gibt_es_keinen_Abbrechen_Knopf()
    {
        var cut = Render<Fortschritt>(p => p.Add(x => x.Anteil, 0.3));

        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Mit_Rueckruf_gibt_es_den_Knopf_und_der_Klick_loest_aus()
    {
        bool abgebrochen = false;
        var cut = Render<Fortschritt>(p => p
            .Add(x => x.Anteil, 0.3)
            .Add(x => x.Abbrechen, EventCallback.Factory.Create(this, () => { abgebrochen = true; })));

        var knopf = cut.Find("button");
        Assert.False(abgebrochen);

        knopf.Click();

        Assert.True(abgebrochen);
    }

    /// <summary>
    /// Nach dem Klick bleibt der Knopf stehen, aber gesperrt: Der Abbruch wirkt erst
    /// an der naechsten Phasengrenze, und ein zweiter Klick beschleunigt ihn nicht.
    /// </summary>
    [Fact]
    public void Nach_dem_Klick_ist_der_Knopf_gesperrt()
    {
        int rufe = 0;
        var cut = Render<Fortschritt>(p => p
            .Add(x => x.Abbrechen, EventCallback.Factory.Create(this, () => { rufe++; })));

        cut.Find("button").Click();
        Assert.True(cut.Find("button").HasAttribute("disabled"));

        cut.Find("button").Click();
        Assert.Equal(1, rufe);
    }

    [Fact]
    public void Unsichtbar_zeichnet_gar_nichts()
    {
        var cut = Render<Fortschritt>(p => p
            .Add(x => x.Sichtbar, false)
            .Add(x => x.Anteil, 0.5));

        Assert.Empty(cut.FindAll(".epos-fortschritt"));
    }

    /// <summary>Ohne Text steht der Vorgabetext des Katalogs.</summary>
    [Fact]
    public void Ohne_Text_steht_der_Vorgabetext()
    {
        var cut = Render<Fortschritt>(p => p.Add(x => x.Anteil, 0.1));

        string text = cut.Find(".epos-fortschritt-text").TextContent;
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    /// <summary>Die Beschriftung des Abbrechen-Knopfs laesst sich uebersteuern.</summary>
    [Fact]
    public void Die_Knopfbeschriftung_laesst_sich_setzen()
    {
        var cut = Render<Fortschritt>(p => p
            .Add(x => x.AbbrechenText, "Lauf stoppen")
            .Add(x => x.Abbrechen, EventCallback.Factory.Create(this, () => { })));

        Assert.Equal("Lauf stoppen", cut.Find("button").TextContent);
    }
}
