using System.IO;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Geraetesteckbrief - die lesende Liste aus Beschriftung und Wert (WP1a): Zeilen in Reihenfolge im dl-Muster,
/// „—" bei fehlendem Wert (ohne Einheit), Einheit hinter dem Wert, Herleitung leise darunter, Titel als Name des
/// Abschnitts und wahlweise als Überschrift, ohne Zeilen nur der Leertext.
/// </summary>
public class GeraetesteckbriefTests : EposBunitContext
{
    private IRenderedComponent<Geraetesteckbrief> Zeige(IReadOnlyList<Steckbriefzeile> zeilen, bool titelAnzeigen = true,
                                                        string leertext = "")
        => Render<Geraetesteckbrief>(p => p
            .Add(x => x.Titel, "Gewähltes Gerät")
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Leertext, leertext)
            .Add(x => x.Klasse, "probe"));

    [Fact]
    public void Die_Zeilen_stehen_in_Reihenfolge_mit_Einheit_und_Herleitung()
    {
        var c = Zeige(new[]
        {
            new Steckbriefzeile("Name", "WP 1", Kennung: "name"),
            new Steckbriefzeile("Nennleistung", "12", "kW", Kennung: "nennleistung"),
            new Steckbriefzeile("Heizleistung bei B0/W35", "10,5", "kW", "Projektkennlinie", "heizleistung"),
        });

        var wurzel = c.Find("section.epos-geraetesteckbrief.probe");
        Assert.Equal("Gewähltes Gerät", wurzel.GetAttribute("aria-label"));
        Assert.Equal("Gewähltes Gerät", c.Find("h4.epos-geraetesteckbrief-titel").TextContent);
        Assert.NotNull(c.Find("dl.epos-steckbrief-werte.epos-geraetesteckbrief-werte"));
        Assert.Equal(new[] { "Name", "Nennleistung", "Heizleistung bei B0/W35" },
                     c.FindAll("dt").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(new[] { "WP 1", "12 kW", "10,5 kW" },
                     c.FindAll(".epos-geraetesteckbrief-wert").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal("Projektkennlinie",
                     c.Find("dd[data-zeile=heizleistung] .epos-geraetesteckbrief-herleitung").TextContent.Trim());
        Assert.Single(c.FindAll(".epos-geraetesteckbrief-herleitung"));
    }

    [Fact]
    public void Ein_fehlender_Wert_steht_als_Strich_ohne_Einheit()
    {
        var c = Zeige(new[]
        {
            new Steckbriefzeile("Baujahr", null, Kennung: "baujahr"),
            new Steckbriefzeile("Heizstab", "  ", "kW", Kennung: "heizstab"),
        });

        Assert.Equal("—", c.Find("dd[data-zeile=baujahr] .epos-geraetesteckbrief-wert").TextContent.Trim());
        Assert.Equal("—", c.Find("dd[data-zeile=heizstab] .epos-geraetesteckbrief-wert").TextContent.Trim());
        Assert.Equal(2, c.FindAll("dd.epos-geraetesteckbrief-fehlt").Count);
    }

    [Fact]
    public void Ohne_Titelanzeige_traegt_nur_das_aria_label_den_Titel()
    {
        var c = Zeige(new[] { new Steckbriefzeile("Name", "WP 1") }, titelAnzeigen: false);
        Assert.Empty(c.FindAll("h4"));
        Assert.Equal("Gewähltes Gerät", c.Find("section").GetAttribute("aria-label"));
    }

    [Fact]
    public void Ohne_Gaben_und_ohne_Zeilen_zeichnet_er_nur_den_Leertext()
    {
        var leer = Render<Geraetesteckbrief>();
        Assert.Empty(leer.FindAll("dl"));
        Assert.Empty(leer.FindAll("p"));
        Assert.Null(leer.Find("section").GetAttribute("aria-label"));

        var mitText = Zeige(Array.Empty<Steckbriefzeile>(), leertext: "Kein Gerät gewählt");
        Assert.Empty(mitText.FindAll("dl"));
        Assert.Equal("Kein Gerät gewählt", mitText.Find("p.epos-geraetesteckbrief-leer").TextContent);
    }

    [Fact]
    public void Das_Stilblatt_fuehrt_die_Regeln_des_Steckbriefs_ohne_eigene_Farben()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))) d = d.Parent;
        Assert.NotNull(d);
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")).Replace("\r\n", "\n");
        int a = css.IndexOf(".epos-geraetesteckbrief {", StringComparison.Ordinal);
        Assert.True(a > 0);
        int e = css.IndexOf(".epos-geraetesteckbrief-herleitung {", a, StringComparison.Ordinal);
        Assert.True(e > a);
        string block = css[a..css.IndexOf('}', e)];
        Assert.DoesNotContain("#", block);
        Assert.DoesNotContain("rgb", block);
        // Er nimmt das Raster des Bauteilsteckbriefs mit, statt ein zweites zu bauen.
        Assert.Contains(".epos-steckbrief-werte {", css);
    }
}
