using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Befund W9‑B‑2</b> der Windows-Abnahme vom 05.09.2026 — „Liste zu lang".
///
/// <para>Die Liste „Gebäude in DB" wuchs mit ihrem Bestand ins Endlose und schob
/// Filter, Detailblock und Schlussleiste meterweit nach unten; um an „OK" zu kommen,
/// musste der Anwender die ganze SEITE rollen. Der Anwender hat die Hausregel dazu
/// gegeben: <b>Listen stehen in einem festen Rahmen mit Rollbalken.</b></para>
///
/// <para>Umgesetzt ist sie an der EINEN Stelle, die alle Listen des Hauses tragen —
/// der Hüllenklasse <c>.epos-raster-huelle</c>. Sie steht um die handgeschriebenen
/// Tabellen der Projekt/DB-Dialoge, um das QuickGrid des Bausteins <c>Raster</c> und
/// um die <c>ProjektListe</c>.</para>
///
/// <para><b>Eine bunit-Probe sieht eine Stilregel nicht</b> (Lehre W6‑B‑1) — geprüft
/// wird deshalb zweierlei: die REGEL im Stilblatt und das MARKUP, das sie treffen
/// muss. Denselben Weg geht
/// <c>KostenSeiteTests.Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex</c>.</para>
/// </summary>
public class ListenrahmenTests : BunitContext
{
    public ListenrahmenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        DeutscheOberflaeche();
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    // =====================================================================
    // Die Regel im Stilblatt
    // =====================================================================

    /// <summary>
    /// Die Höhe steht als TOKEN in <c>:root</c>, nicht als Rückfall in der Regel
    /// (Hausregel „Eine Farbe steht als Token"). Sie ist eine HÖCHSTHÖHE — eine
    /// kurze Liste wird davon nicht künstlich hoch — und in <c>rem</c> angegeben,
    /// damit sie mit der Schriftgröße mitwächst.
    /// </summary>
    [Fact]
    public void Die_Listenhoehe_steht_als_Token_in_rem()
    {
        string wurzel = Stilblock(":root {");

        Assert.Contains("--epos-listenhoehe:", wurzel);
        Assert.Matches(@"--epos-listenhoehe:\s*[\d.]+rem;", wurzel);
    }

    /// <summary>
    /// Der Rahmen selbst: begrenzte Höhe, Rollbalken in BEIDE Richtungen (die
    /// waagerechte Rolle stand schon seit dem Befund vom 03.09.2026 da).
    /// </summary>
    [Fact]
    public void Die_Rasterhuelle_traegt_Hoechsthoehe_und_Rollbalken()
    {
        string huelle = Stilblock(".epos-raster-huelle {");

        Assert.Contains("max-height: var(--epos-listenhoehe)", huelle);
        Assert.Contains("overflow: auto", huelle);

        // Und der Rückweg ist benannt, damit niemand die Regel aufweicht.
        Assert.Contains("max-height: none", Stilblock(".epos-raster-huelle--frei {"));
    }

    /// <summary>
    /// Eine gerollte Liste ohne stehenden Spaltenkopf ist nicht mehr zuzuordnen —
    /// dieselbe Begründung wie bei der virtualisierten Liste aus iU9‑W13.
    /// </summary>
    [Fact]
    public void Der_Spaltenkopf_bleibt_beim_Rollen_stehen()
    {
        string kopf = Stilblock(".epos-raster-huelle .epos-raster thead th {");

        Assert.Contains("position: sticky", kopf);
        Assert.Contains("top: 0", kopf);
        Assert.Contains("background:", kopf);
    }

    // =====================================================================
    // Das Markup, das die Regel treffen muss
    // =====================================================================

    private sealed record Zeile(int Id, string Bezeichner);

    private IRenderedComponent<Raster<Zeile>> Rasterprobe(bool? begrenzt = null)
    {
        var zeilen = new[] { new Zeile(3, "Erdgas") }.AsQueryable();

        return Render<Raster<Zeile>>(p =>
        {
            p.Add(x => x.Zeilen, zeilen);
            if (begrenzt is not null) p.Add(x => x.Begrenzt, begrenzt.Value);
            p.Add(x => x.KindInhalt, (RenderFragment)(bau =>
            {
                bau.OpenComponent<PropertyColumn<Zeile, string>>(0);
                bau.AddComponentParameter(1, nameof(PropertyColumn<Zeile, string>.Property),
                    (System.Linq.Expressions.Expression<Func<Zeile, string>>)(z => z.Bezeichner));
                bau.AddComponentParameter(2, nameof(PropertyColumn<Zeile, string>.Title), "Bezeichnung");
                bau.CloseComponent();
            }));
        });
    }

    /// <summary>Der Rahmen ist die VORGABE — kein Wirt muss ihn bestellen.</summary>
    [Fact]
    public void Ein_Raster_steht_von_selbst_im_Rahmen()
    {
        string klasse = Rasterprobe().Find("div").ClassName ?? "";

        Assert.Contains("epos-raster-huelle", klasse);
        Assert.DoesNotContain("epos-raster-huelle--frei", klasse);
    }

    [Fact]
    public void Begrenzt_false_nimmt_dem_Raster_den_Rahmen()
    {
        Assert.Contains("epos-raster-huelle--frei",
                        Rasterprobe(begrenzt: false).Find("div").ClassName ?? "");
    }

    [Fact]
    public void Eine_Projektliste_steht_von_selbst_im_Rahmen()
    {
        var cut = Render<ProjektListe>(p => p
            .Add(x => x.Zeilen, new[] { new ProjektKopfZeile(7, "Projekt 7") }));

        string klasse = cut.Find(".epos-projektliste .epos-raster-huelle").ClassName ?? "";
        Assert.DoesNotContain("epos-raster-huelle--frei", klasse);
    }

    [Fact]
    public void Begrenzt_false_nimmt_der_Projektliste_den_Rahmen()
    {
        var cut = Render<ProjektListe>(p => p
            .Add(x => x.Zeilen, new[] { new ProjektKopfZeile(7, "Projekt 7") })
            .Add(x => x.Begrenzt, false));

        Assert.Contains("epos-raster-huelle--frei",
                        cut.Find(".epos-projektliste .epos-raster-huelle").ClassName ?? "");
    }

    /// <summary>
    /// Der Befund kam am Gebäudedialog auf: <b>beide</b> Listen — „ausgewählte
    /// Gebäude im Projekt" und „Gebäude in DB" — stehen in einem Rahmen.
    /// </summary>
    [Fact]
    public void Beide_Listen_des_Gebaeudedialogs_stehen_im_Rahmen()
    {
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Baualtersklassen, new[] { "vor 1919" }));

        Assert.Equal(2, cut.FindAll(".epos-zweispalten-spalte .epos-raster-huelle").Count);
    }

    // =====================================================================
    // Hilfen
    // =====================================================================

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
    private static string Stilblock(string selektor)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
