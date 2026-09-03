using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>Kachel - der anklickbare Einstieg (Vorbild EinstiegsKarte.cs).</summary>
public class KachelTests : BunitContext
{
    [Fact]
    public void Titel_Beschreibung_und_Status_werden_gezeigt()
    {
        var cut = Render<Kachel>(p => p
            .Add(x => x.Titel, "Kostenprofil")
            .Add(x => x.Beschreibung, "Preisverlaeufe je Energietraeger")
            .Add(x => x.Status, "3 Profile gepflegt"));

        Assert.Equal("Kostenprofil", cut.Find(".epos-kachel-titel").TextContent);
        Assert.Equal("Preisverlaeufe je Energietraeger", cut.Find(".epos-kachel-beschreibung").TextContent);
        Assert.Contains("3 Profile gepflegt", cut.Find(".epos-kachel-status").TextContent);
        Assert.Single(cut.FindAll(".epos-kachel-statuspunkt"));
    }

    [Fact]
    public void Ohne_Status_gibt_es_keinen_Statuspunkt()
    {
        var cut = Render<Kachel>(p => p.Add(x => x.Titel, "Kostenprofil"));

        Assert.Empty(cut.FindAll(".epos-kachel-status"));
        Assert.Empty(cut.FindAll(".epos-kachel-statuspunkt"));
    }

    [Fact]
    public void Der_Klick_wird_gemeldet()
    {
        int gerufen = 0;
        var cut = Render<Kachel>(p => p
            .Add(x => x.Titel, "Kostenprofil")
            .Add(x => x.Geklickt, () => gerufen++));

        cut.Find("button").Click();

        Assert.Equal(1, gerufen);
    }

    [Fact]
    public void Das_Bild_erscheint_nur_mit_Adresse()
    {
        var ohne = Render<Kachel>(p => p.Add(x => x.Titel, "Kostenprofil"));
        Assert.Empty(ohne.FindAll("img"));

        var mit = Render<Kachel>(p => p
            .Add(x => x.Titel, "Kostenprofil")
            .Add(x => x.Bild, "_content/EPOS.UI/help_icon.png"));
        Assert.Equal("_content/EPOS.UI/help_icon.png", mit.Find("img").GetAttribute("src"));
    }
}
