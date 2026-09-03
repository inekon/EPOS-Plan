using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Kachelraster und Kennzahlkachel (iU9-W5.0) - die Kennzahlreihe der Kosten-
/// und der Wirtschaftlichkeitsseite. Im Bestand zwei TableLayoutPanels mit
/// gerechneten Prozentspalten (UcBkKosten.pnlKacheln,
/// UcWirtschaftlichkeit.KachelnBauen), hier ein CSS-Raster.
/// </summary>
public class KachelrasterTests : BunitContext
{
    [Fact]
    public void Das_Raster_traegt_die_Mindestbreite_als_Stilvorgabe()
    {
        var cut = Render<Kachelraster>(p => p.Add(x => x.Mindestbreite, 260));

        Assert.Contains("--epos-kachel-min: 260px",
            cut.Find(".epos-kachelraster").GetAttribute("style"));
    }

    [Fact]
    public void Ohne_Angabe_gilt_die_Vorgabe_220()
    {
        var cut = Render<Kachelraster>();

        Assert.Contains("220px", cut.Find(".epos-kachelraster").GetAttribute("style"));
    }

    [Fact]
    public void Die_Kacheln_stehen_im_Raster()
    {
        var cut = Render<Kachelraster>(p => p.Add(x => x.KindInhalt, (RenderFragment)(b =>
        {
            for (int i = 0; i < 3; i++)
            {
                b.OpenComponent<Kennzahlkachel>(i * 3);
                b.AddAttribute(i * 3 + 1, "Titel", "K" + i);
                b.AddAttribute(i * 3 + 2, "Wert", "1,00 €");
                b.CloseComponent();
            }
        })));

        Assert.Equal(3, cut.FindAll(".epos-kachelraster > .epos-kennzahlkachel").Count);
    }

    [Fact]
    public void Die_Kennzahlkachel_zeigt_Titel_Wert_und_Herkunft()
    {
        var cut = Render<Kennzahlkachel>(p => p
            .Add(x => x.Titel, "Investition")
            .Add(x => x.Wert, "12.001,00 €")
            .Add(x => x.Quelle, "abzüglich Zuschuss 1.000,00 €"));

        Assert.Equal("Investition", cut.Find(".epos-kennzahlkachel-titel").TextContent);
        Assert.Equal("12.001,00 €", cut.Find(".epos-kennzahlkachel-wert").TextContent);
        Assert.Equal("abzüglich Zuschuss 1.000,00 €", cut.Find(".epos-kennzahlkachel-quelle").TextContent);
    }

    [Fact]
    public void Ein_leerer_Wert_erscheint_als_Gedankenstrich()
    {
        // Hausregel der Kostenseite: 0,00 waere eine Aussage, die niemand
        // getroffen hat (Nutzerentscheidung 4 vom 18.08.2026).
        var cut = Render<Kennzahlkachel>(p => p.Add(x => x.Titel, "Betrieb"));

        Assert.Equal("—", cut.Find(".epos-kennzahlkachel-wert").TextContent);
    }

    [Fact]
    public void Ohne_Herkunft_bleibt_die_leise_Zeile_weg()
    {
        var cut = Render<Kennzahlkachel>(p => p.Add(x => x.Wert, "5"));

        Assert.Empty(cut.FindAll(".epos-kennzahlkachel-quelle"));
    }

    [Fact]
    public void Die_Kachel_ist_Anzeige_und_kein_Knopf()
    {
        var cut = Render<Kennzahlkachel>(p => p.Add(x => x.Titel, "Energie"));

        Assert.Empty(cut.FindAll("button"));
    }
}
