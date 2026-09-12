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

    // =====================================================================
    // iU9-W16a.2 - Zustand und Aktiv (Befund W16-B7)
    // =====================================================================

    /// <summary>
    /// Mit <c>Zustand</c> steht der Punkt IMMER — gruen „im Projekt", grau „nicht
    /// im Projekt". Woertlich <c>Wizard_Komponenten.KachelZeichnen</c>:
    /// <c>StatusSichtbar = true</c> in beiden Faellen,
    /// <c>StatusFarbe = an ? KARTE_STATUS : KARTE_RAHMEN</c>.
    /// </summary>
    [Theory]
    [InlineData(Kachelstand.An, false)]
    [InlineData(Kachelstand.Aus, true)]
    public void Der_Zustand_faerbt_den_Statuspunkt(Kachelstand zustand, bool grau)
    {
        var cut = Render<Kachel>(p => p
            .Add(x => x.Titel, "Wärmepumpe")
            .Add(x => x.Zustand, zustand));

        var punkt = cut.Find(".epos-kachel-statuspunkt");
        Assert.Equal(grau, punkt.ClassList.Contains("epos-kachel-statuspunkt--aus"));
    }

    /// <summary>
    /// Der Punkt steht mit <c>Zustand</c> auch OHNE Statustext — vorher hing er
    /// allein am Text.
    /// </summary>
    [Fact]
    public void Der_Zustand_zeigt_den_Punkt_auch_ohne_Text()
    {
        var mitZustand = Render<Kachel>(p => p
            .Add(x => x.Titel, "Wärmepumpe")
            .Add(x => x.Zustand, Kachelstand.Aus));
        Assert.Single(mitZustand.FindAll(".epos-kachel-statuspunkt"));

        var ohneZustand = Render<Kachel>(p => p.Add(x => x.Titel, "Wärmepumpe"));
        Assert.Empty(ohneZustand.FindAll(".epos-kachel-statuspunkt"));
    }

    /// <summary>
    /// Ohne <c>Zustand</c> bleibt alles beim Alten: Punkt nur mit Text, und immer
    /// gruen. Die Kacheln der Kostenseite und der Projektliste aendern sich nicht.
    /// </summary>
    [Fact]
    public void Ohne_Zustand_bleibt_der_Punkt_gruen()
    {
        var cut = Render<Kachel>(p => p
            .Add(x => x.Titel, "Kostenprofil")
            .Add(x => x.Status, "3 Profile gepflegt"));

        Assert.False(cut.Find(".epos-kachel-statuspunkt")
                        .ClassList.Contains("epos-kachel-statuspunkt--aus"));
    }

    /// <summary>
    /// <c>Aktiv = false</c> nimmt die Kachel aus der Bedienung, ohne sie zu
    /// verstecken — der Ersatz fuer <c>Cursors.Default</c> auf Karte und Kindern.
    /// Ein <c>disabled</c>-Knopf meldet sich zugleich einer Sprachausgabe als
    /// gesperrt und faellt aus der Tabreihenfolge.
    /// </summary>
    [Fact]
    public void Eine_nicht_aktive_Kachel_zeigt_ihren_Bestand_und_meldet_keinen_Klick()
    {
        int gerufen = 0;
        var cut = Render<Kachel>(p => p
            .Add(x => x.Titel, "Brauchwasser")
            .Add(x => x.Beschreibung, "2 im Projekt · nur Anzeige")
            .Add(x => x.Zustand, Kachelstand.An)
            .Add(x => x.Aktiv, false)
            .Add(x => x.Geklickt, () => gerufen++));

        var knopf = cut.Find("button");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains("nur Anzeige", cut.Find(".epos-kachel-beschreibung").TextContent);
        Assert.Single(cut.FindAll(".epos-kachel-statuspunkt"));
        Assert.False(cut.Find(".epos-kachel-statuspunkt")
                        .ClassList.Contains("epos-kachel-statuspunkt--aus"));
        Assert.Equal(0, gerufen);
    }

    /// <summary>Die Vorgabe ist bedienbar — jede vorhandene Kachel bleibt es.</summary>
    [Fact]
    public void Die_Vorgabe_ist_bedienbar()
    {
        var cut = Render<Kachel>(p => p.Add(x => x.Titel, "Kostenprofil"));

        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }
}
