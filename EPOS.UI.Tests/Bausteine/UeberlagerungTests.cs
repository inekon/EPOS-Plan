using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Ueberlagerung (iU9-W4.0) — der modale Bereich INNERHALB der Komponente.
/// Er loest den Ausweg der Wellen 1 bis 3 ab: Statt eines zweiten Fensters
/// (zweite WebView, Risiko R2) oder eines eingerueckten Blocks (A-13/A-10)
/// steht der Unterdialog jetzt ueber dem Wirt, im selben Fenster.
/// </summary>
public class UeberlagerungTests : BunitContext
{
    [Fact]
    public void Geschlossen_steht_nichts_im_Baum()
    {
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, false)
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Inhalt</p>"))));

        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-hintergrund"));
    }

    [Fact]
    public void Offen_traegt_der_Bereich_die_Rolle_und_die_Abdunkelung()
    {
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Titel, "Neue Art"));

        var bereich = cut.Find(".epos-ueberlagerung");
        Assert.Equal("dialog", bereich.GetAttribute("role"));
        Assert.Equal("true", bereich.GetAttribute("aria-modal"));
        Assert.Equal("Neue Art", bereich.GetAttribute("aria-label"));
        Assert.Single(cut.FindAll(".epos-ueberlagerung-hintergrund"));
    }

    [Fact]
    public void Der_Inhalt_erscheint_im_Bereich()
    {
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"x\">Inhalt</p>"))));

        Assert.Equal("Inhalt", cut.Find("#x").TextContent);
    }

    [Fact]
    public void Der_Titel_steht_in_der_Kopfzeile()
    {
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Titel, "Wert bearbeiten"));

        Assert.Equal("Wert bearbeiten", cut.Find(".epos-ueberlagerung-titel").TextContent);
    }

    [Fact]
    public void Ohne_Titel_und_ohne_Kreuz_gibt_es_keine_Kopfzeile()
    {
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Titel, "")
            .Add(x => x.Schliessbar, false));

        Assert.Empty(cut.FindAll(".epos-ueberlagerung-kopf"));
    }

    [Fact]
    public void Das_Kreuz_schliesst_und_meldet_beides()
    {
        bool? neuerZustand = null;
        int geschlossen = 0;
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.OffenChanged, (bool b) => neuerZustand = b)
            .Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(neuerZustand);
        Assert.Equal(1, geschlossen);
    }

    [Fact]
    public void Ohne_Schliessbar_fehlt_das_Kreuz()
    {
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Titel, "Titel")
            .Add(x => x.Schliessbar, false));

        Assert.Empty(cut.FindAll(".epos-ueberlagerung-zu"));
    }

    [Fact]
    public void Esc_schliesst_den_Bereich()
    {
        int geschlossen = 0;
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find(".epos-ueberlagerung").KeyDown(key: "Escape");

        Assert.Equal(1, geschlossen);
    }

    [Fact]
    public void Ein_Klick_auf_die_Abdunkelung_schliesst_NICHT()
    {
        int geschlossen = 0;
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find(".epos-ueberlagerung-hintergrund").Click();

        Assert.Equal(0, geschlossen);
    }

    [Fact]
    public void Mit_SchliessenBeiHintergrund_schliesst_er_doch()
    {
        int geschlossen = 0;
        var cut = Render<Ueberlagerung>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.SchliessenBeiHintergrund, true)
            .Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find(".epos-ueberlagerung-hintergrund").Click();

        Assert.Equal(1, geschlossen);
    }

    [Fact]
    public void Die_Fokusfalle_steht_vor_und_hinter_dem_Inhalt()
    {
        var cut = Render<Ueberlagerung>(p => p.Add(x => x.Offen, true));

        var fallen = cut.FindAll(".epos-fokusfalle");
        Assert.Equal(2, fallen.Count);
        Assert.All(fallen, f => Assert.Equal("0", f.GetAttribute("tabindex")));
    }

    [Fact]
    public void Der_Bereich_ist_selbst_fokussierbar()
    {
        var cut = Render<Ueberlagerung>(p => p.Add(x => x.Offen, true));

        Assert.Equal("-1", cut.Find(".epos-ueberlagerung").GetAttribute("tabindex"));
    }
}
