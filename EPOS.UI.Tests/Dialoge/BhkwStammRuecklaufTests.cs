using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Abschaltgrenze des Rücklaufs im BHKW-Stammblatt</b> (UB‑E3‑b, Fachkonzept U‑3): das Feld „Höchster Rücklauf
/// (Abschaltgrenze)" neben dem Auslegungsrücklauf, die Vorgabe 70 °C bei Neuanlage und der Hinweis (keine Sperre), wenn
/// der Auslegungsrücklauf die Grenze erreicht. Die Regel steht im Kern. Kultur de-DE über <see cref="EposBunitContext"/>.
/// </summary>
public class BhkwStammRuecklaufTests : EposBunitContext
{
    private static readonly (int Id, string Text)[] Brennstoffe = { (0, "Stadtgas"), (1, "Erdgas E") };

    public BhkwStammRuecklaufTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<BhkwKatalogDialog> Aufbauen(BhkwKatalogDaten d, KatalogModus modus = KatalogModus.Bearbeiten)
        => Render<BhkwKatalogDialog>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Modus, modus)
            .Add(x => x.Brennstoffe, Brennstoffe)
            .Add(x => x.Ueberschreiben, dd => new KatalogSpeicherErgebnis(true, "ok", dd.Bezeichner))
            .Add(x => x.Anlegen, (_, n) => new KatalogSpeicherErgebnis(true, "ok", n)));

    [Fact]
    public void Feld_steht_neben_dem_Auslegungsruecklauf()
    {
        var cut = Aufbauen(new BhkwKatalogDaten { Bezeichner = "Modul", Vorlauf = 80, Ruecklauf = 60, RuecklaufMax = 72 });
        string m = cut.Markup;
        int rl = m.IndexOf("Rücklauf:", System.StringComparison.Ordinal);
        int max = m.IndexOf("Höchster Rücklauf (Abschaltgrenze)", System.StringComparison.Ordinal);
        Assert.True(rl >= 0 && max > rl, "Das Feld der Abschaltgrenze steht hinter dem Auslegungsrücklauf.");
        Assert.Contains("value=\"72\"", m);
        Assert.False(cut.Instance.RuecklaufHinweisAktiv);
    }

    [Fact]
    public void Neuanlage_traegt_die_Vorgabe_70_Grad()
    {
        var neu = new BhkwKatalogDaten { Bezeichner = "Neu" };
        Assert.Equal(70.0, neu.RuecklaufMax);
        var cut = Aufbauen(neu, KatalogModus.Neu);
        Assert.Contains("value=\"70\"", cut.Markup);
    }

    [Theory]
    [InlineData(70, true)]
    [InlineData(75, true)]
    [InlineData(65, false)]
    public void Hinweis_wenn_der_Auslegungsruecklauf_die_Grenze_erreicht(int auslegung, bool hinweis)
    {
        var cut = Aufbauen(new BhkwKatalogDaten { Bezeichner = "Modul", Ruecklauf = auslegung, RuecklaufMax = 70 });
        Assert.Equal(hinweis, cut.Instance.RuecklaufHinweisAktiv);
        string text = string.Format(CultureInfo.CurrentCulture, cut.Instance.HinweisRuecklaufMax, auslegung, 70);
        Assert.Equal(hinweis, cut.Markup.Contains(text, System.StringComparison.Ordinal));
    }

    [Fact]
    public void Ohne_Grenze_kein_Hinweis()
    {
        var cut = Aufbauen(new BhkwKatalogDaten { Bezeichner = "Modul", Ruecklauf = 90, RuecklaufMax = null });
        Assert.False(cut.Instance.RuecklaufHinweisAktiv);
    }
}
