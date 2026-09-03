using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>Herleitungszeile und Kohaerenzzeile.</summary>
public class ZeilenTests : BunitContext
{
    [Fact]
    public void Herleitungszeile_zeigt_Text_und_Formel()
    {
        var cut = Render<Herleitungszeile>(p => p
            .Add(x => x.Text, "Arbeitspreis mal Jahresverbrauch")
            .Add(x => x.Formel, "12,5 ct/kWh x 3.400 kWh"));

        Assert.Equal("Arbeitspreis mal Jahresverbrauch", cut.Find(".epos-herleitung-text").TextContent);
        Assert.Equal("12,5 ct/kWh x 3.400 kWh", cut.Find(".epos-herleitung-formel").TextContent);
    }

    [Fact]
    public void Herleitungszeile_ohne_Formel_zeigt_nur_den_Text()
    {
        var cut = Render<Herleitungszeile>(p => p.Add(x => x.Text, "Aus dem Katalog uebernommen"));

        Assert.Empty(cut.FindAll(".epos-herleitung-formel"));
        Assert.Contains("epos-herleitung", cut.Find("p").ClassName);
    }

    [Theory]
    [InlineData(KohaerenzZustand.Ok, "epos-kohaerenz--ok")]
    [InlineData(KohaerenzZustand.Abweichend, "epos-kohaerenz--abweichend")]
    public void Kohaerenzzeile_traegt_ihren_Zustand_als_Klasse(KohaerenzZustand zustand, string klasse)
    {
        var cut = Render<Kohaerenzzeile>(p => p
            .Add(x => x.Text, "Summe der Posten gleich Gesamtpreis")
            .Add(x => x.Zustand, zustand));

        Assert.Contains(klasse, cut.Find("p").ClassName);
        Assert.Equal("Summe der Posten gleich Gesamtpreis", cut.Find(".epos-kohaerenz-text").TextContent);
    }

    [Fact]
    public void Kohaerenzzeile_ist_ohne_Angabe_stimmig()
    {
        var cut = Render<Kohaerenzzeile>(p => p.Add(x => x.Text, "Passt"));

        Assert.Contains("epos-kohaerenz--ok", cut.Find("p").ClassName);
    }
}
