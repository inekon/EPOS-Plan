using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Zeilenraster (iU9-W4.0) — Spaltenkopf, Zeilen, Abschlusszeile und
/// Summenfuss des Positionsrasters. Vorbild ist das Raster der
/// Kostenverwaltung (pnlRasterKopf + pnlZeilen + pnlFuss).
/// </summary>
public class ZeilenrasterTests : BunitContext
{
    private static RenderFragment Markup(string inhalt)
        => b => b.AddMarkupContent(0, inhalt);

    [Fact]
    public void Der_Spaltenkopf_traegt_die_uebergebenen_Ueberschriften()
    {
        var cut = Render<Zeilenraster>(p => p
            .Add(x => x.Spalten, new[] { "Aktionen", "Position", "Bemessung" }));

        var koepfe = cut.FindAll(".epos-zr-kopfzelle");
        Assert.Equal(3, koepfe.Count);
        Assert.Equal("Aktionen", koepfe[0].TextContent);
        Assert.Equal("Bemessung", koepfe[2].TextContent);
        Assert.All(koepfe, k => Assert.Equal("columnheader", k.GetAttribute("role")));
    }

    [Fact]
    public void Mit_Zeilenwahl_kommt_eine_erste_Spalte_dazu()
    {
        var cut = Render<Zeilenraster>(p => p
            .Add(x => x.Spalten, new[] { "Position" })
            .Add(x => x.MitZeilenwahl, true)
            .Add(x => x.WahlKopf, "Wahl"));

        var koepfe = cut.FindAll(".epos-zr-kopfzelle");
        Assert.Equal(2, koepfe.Count);
        Assert.Equal("Wahl", koepfe[0].TextContent);
        Assert.Equal("Position", koepfe[1].TextContent);
    }

    [Fact]
    public void Das_Spaltenmass_steht_als_Rasterspur_am_Wurzelelement()
    {
        var cut = Render<Zeilenraster>(p => p
            .Add(x => x.Spaltenmass, "104px 2fr 1fr"));

        Assert.Contains("grid-template-columns: 104px 2fr 1fr",
                        cut.Find(".epos-zeilenraster").GetAttribute("style"));
    }

    [Fact]
    public void Die_Zeilen_stehen_zwischen_Kopf_und_Fuss()
    {
        var cut = Render<Zeilenraster>(p => p
            .Add(x => x.Spalten, new[] { "Position" })
            .Add(x => x.KindInhalt, Markup("<div class=\"epos-zr-zeile\"><span id=\"z1\">A</span></div>")));

        Assert.Equal("A", cut.Find("#z1").TextContent);
    }

    [Fact]
    public void Die_Abschlusszeile_erscheint_nach_den_Zeilen()
    {
        var cut = Render<Zeilenraster>(p => p
            .Add(x => x.KindInhalt, Markup("<div id=\"z\">Zeile</div>"))
            .Add(x => x.AbschlussZeile, Markup("<div id=\"neu\" class=\"epos-zr-neuzeile\">+</div>")));

        Assert.Equal("+", cut.Find("#neu").TextContent);
        Assert.Contains("epos-zr-neuzeile", cut.Find("#neu").ClassName);
    }

    [Fact]
    public void Ohne_Abschlusszeile_steht_keine_da()
    {
        var cut = Render<Zeilenraster>();

        Assert.Empty(cut.FindAll(".epos-zr-neuzeile"));
    }

    [Fact]
    public void Der_Summenfuss_zeigt_jede_Zeile_und_hebt_die_starke_hervor()
    {
        var cut = Render<Zeilenraster>(p => p
            .Add(x => x.Summen, new[]
            {
                ("Summe Investitionskosten netto: 1.000,00 €", true),
                ("Summe brutto: 1.190,00 € (Umsatzsteuer 19 % aus dem Katalog)", false)
            }));

        var zellen = cut.FindAll(".epos-zr-summenzelle");
        Assert.Equal(2, zellen.Count);
        Assert.Contains("epos-zr-summenzelle--stark", zellen[0].ClassName);
        Assert.DoesNotContain("epos-zr-summenzelle--stark", zellen[1].ClassName);
        Assert.StartsWith("Summe brutto", zellen[1].TextContent);
    }

    [Fact]
    public void Ohne_Summen_gibt_es_keinen_Fuss()
    {
        var cut = Render<Zeilenraster>();

        Assert.Empty(cut.FindAll(".epos-zr-summe"));
    }

    [Fact]
    public void Das_Raster_meldet_sich_der_Sprachausgabe_als_Tabelle()
    {
        var cut = Render<Zeilenraster>(p => p.Add(x => x.Bezeichnung, "Positionen"));

        var wurzel = cut.Find(".epos-zeilenraster");
        Assert.Equal("table", wurzel.GetAttribute("role"));
        Assert.Equal("Positionen", wurzel.GetAttribute("aria-label"));
    }
}
