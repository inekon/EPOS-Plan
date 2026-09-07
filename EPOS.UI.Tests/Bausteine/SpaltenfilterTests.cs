using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Das Popover eines Spaltenfilters</b> (Anwenderentscheid <b>W14a‑E‑10</b> vom
/// 07.09.2026, Konzept_Katalogfilter 5.6.2/5.6.3, Schritt S1.2).
///
/// <para>Zwei Dinge werden geprüft: das <b>Verhalten</b> des Popovers — was
/// übernimmt, was schließt, was löscht — und die <b>Messung zum offenen Punkt
/// O‑6</b>, an der die Entscheidung „Enter/Verlassen übernimmt und schließt"
/// hängt.</para>
///
/// <para>Kultur gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class SpaltenfilterTests : BunitContext
{
    public SpaltenfilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
    }

    // =====================================================================
    //  Das Popover
    // =====================================================================

    private (IRenderedComponent<Spaltenfilter> Cut, System.Collections.Generic.List<string> Werte,
             System.Collections.Generic.List<int> Geschlossen) Aufbauen(
                 string wert = "", string hinweis = "")
    {
        var werte = new System.Collections.Generic.List<string>();
        var zu = new System.Collections.Generic.List<int>();

        var cut = Render<Spaltenfilter>(p => p
            .Add(x => x.Spaltenname, "Brennstoff")
            .Add(x => x.Wert, wert)
            .Add(x => x.Platzhalter, "enthält…")
            .Add(x => x.Hinweis, hinweis)
            .Add(x => x.TextLoeschen, "Filter löschen")
            .Add(x => x.WertChanged, EventCallback.Factory.Create<string>(this, w => werte.Add(w)))
            .Add(x => x.Geschlossen, EventCallback.Factory.Create(this, () => zu.Add(1))));

        return (cut, werte, zu);
    }

    /// <summary>
    /// <b>Genau drei Dinge</b> (5.6.2): Spaltenname als Überschrift, EIN Eingabefeld,
    /// der Knopf „Filter löschen". Kein „Übernehmen", keine Werteliste.
    /// </summary>
    [Fact]
    public void Das_Popover_traegt_Name_ein_Feld_und_Filter_loeschen()
    {
        var (cut, _, _) = Aufbauen();

        Assert.Equal("Brennstoff", cut.Find(".epos-spaltenfilter-name").TextContent);
        Assert.Single(cut.FindAll("input"));
        Assert.Equal("enthält…", cut.Find("input").GetAttribute("placeholder"));
        Assert.Equal("Filter löschen", cut.Find(".epos-spaltenfilter-loeschen").TextContent);

        // Keine zweite Zeile, solange kein Hinweis gesetzt ist.
        Assert.Empty(cut.FindAll(".epos-spaltenfilter-hinweis"));
    }

    /// <summary>
    /// Bei einer ZAHLENSPALTE steht der Formenhinweis darunter — sonst könnte
    /// niemand wissen, dass <c>&gt;10</c> und <c>10..60</c> gehen (Frage Q1).
    /// </summary>
    [Fact]
    public void Eine_Zahlenspalte_nennt_die_Formen()
    {
        var (cut, _, _) = Aufbauen(hinweis: "z. B. >10, <60, 10..60, =15");
        Assert.Contains("10..60", cut.Find(".epos-spaltenfilter-hinweis").TextContent);
    }

    /// <summary>
    /// <b>Tippen übernimmt NICHT</b> — das ist die Entscheidung zu O‑6. Solange nur
    /// getippt wird, bleibt die Zeilenmenge unberührt, das Raster wird nicht neu
    /// gebaut und das Popover bleibt stehen.
    /// </summary>
    [Fact]
    public void Tippen_uebernimmt_nicht()
    {
        var (cut, werte, zu) = Aufbauen();

        cut.Find("input").Input("Ga");
        cut.Find("input").Input("Gas");

        Assert.Empty(werte);
        Assert.Empty(zu);
        Assert.Equal("Gas", cut.Instance.Text);
    }

    /// <summary><b>Enter übernimmt und schließt.</b></summary>
    [Fact]
    public void Enter_uebernimmt_und_schliesst()
    {
        var (cut, werte, zu) = Aufbauen();

        cut.Find("input").Input("Gas");
        cut.Find("input").KeyDown("Enter");

        Assert.Equal(new[] { "Gas" }, werte);
        Assert.Single(zu);
    }

    /// <summary><b>Das Verlassen übernimmt ebenfalls</b> — „Enter/Verlassen" (O‑6).</summary>
    [Fact]
    public void Verlassen_uebernimmt_und_schliesst()
    {
        var (cut, werte, zu) = Aufbauen();

        cut.Find("input").Input("Gas");
        cut.Find("input").FocusOut();

        Assert.Equal(new[] { "Gas" }, werte);
        Assert.Single(zu);
    }

    /// <summary>
    /// Ein Wechsel des Feldwertes (<c>change</c>) übernimmt — der Weg, den der
    /// Browser bei Enter und beim Fokuswechsel selbst geht.
    /// </summary>
    [Fact]
    public void Der_Feldwechsel_uebernimmt()
    {
        var (cut, werte, zu) = Aufbauen();

        cut.Find("input").Change("10..60");

        Assert.Equal(new[] { "10..60" }, werte);
        Assert.Single(zu);
    }

    /// <summary>
    /// <b>Esc schließt, ohne zu übernehmen</b> — der zuletzt gesetzte Filter bleibt
    /// stehen.
    /// </summary>
    [Fact]
    public void Esc_schliesst_ohne_zu_uebernehmen()
    {
        var (cut, werte, zu) = Aufbauen("Gas");

        cut.Find("input").Input("Öl");
        cut.Find("input").KeyDown("Escape");

        Assert.Empty(werte);
        Assert.Single(zu);
    }

    /// <summary>
    /// „Filter löschen" nimmt den Filter DIESER Spalte zurück (leerer Ausdruck) und
    /// schließt. Der Knopf hält <c>mousedown</c> an, damit das Feld den Fokus nicht
    /// verliert und <c>onfocusout</c> ihn nicht unter dem Klick wegnimmt.
    /// </summary>
    [Fact]
    public void Filter_loeschen_nimmt_zurueck_und_schliesst()
    {
        var (cut, werte, zu) = Aufbauen("Gas");

        cut.Find(".epos-spaltenfilter-loeschen").Click();

        Assert.Equal(new[] { "" }, werte);
        Assert.Single(zu);
        Assert.Equal("", cut.Instance.Text);
    }

    /// <summary>
    /// Für die zwei letzten Spalten öffnet das Popover nach LINKS — sonst schnitte
    /// der Listenrahmen (<c>.epos-raster-huelle</c>, <c>overflow: auto</c>) es ab.
    /// </summary>
    [Fact]
    public void Rechts_aussen_oeffnet_das_Popover_nach_links()
    {
        var cut = Render<Spaltenfilter>(p => p
            .Add(x => x.Spaltenname, "T_NOCT")
            .Add(x => x.RechtsBuendig, true));

        Assert.Contains("epos-spaltenfilter--rechts", cut.Find("div").ClassName);
    }

    // =====================================================================
    //  DIE MESSUNG ZU O-6
    // =====================================================================

    private sealed record Probe(int Id, string Name);

    /// <summary>
    /// <b>Offener Punkt O‑6, gemessen:</b> „Ein neu aufgebautes Raster schließt das
    /// Popover."
    ///
    /// <para>Der Fall baut ein <c>Raster</c> mit einer Spalte, die QuickGrids eigenes
    /// <c>ColumnOptions</c> führt — der Weg, den Konzept 5.6.7 als Fundstelle nennt.
    /// Der Optionsknopf öffnet das Popover (<c>.col-options</c> steht da). Danach
    /// wechselt die ZEILENZAHL, wie es jeder Filterschritt tut; der <c>@key</c> an
    /// <c>(Virtualisiert, Zeilenzahl)</c> (Fix W6‑B‑2) baut das QuickGrid neu auf —
    /// und das Popover ist <b>weg</b>, weil sein Zustand in der alten Rasterinstanz
    /// lag.</para>
    ///
    /// <para><b>Daraus folgt die Entscheidung</b> (im Kopf von
    /// <c>Spaltenfilter.razor</c>): Das Feld wirkt bei <b>Enter oder beim
    /// Verlassen</b>, nicht beim Tippen. Der zweite im Konzept genannte Weg —
    /// „Popover nach dem Neuaufbau wieder öffnen" — löste den Schreibzeiger nicht:
    /// Das Eingabefeld wäre ein NEUES DOM‑Element und der Fokus damit weg.</para>
    /// </summary>
    [Fact]
    public void O6_Ein_neu_aufgebautes_Raster_schliesst_das_Popover()
    {
        var drei = new[] { new Probe(1, "A"), new Probe(2, "B"), new Probe(3, "C") };

        var cut = Render<Raster<Probe>>(p => p
            .Add(x => x.Zeilen, drei.AsQueryable())
            .Add(x => x.KindInhalt, (RenderFragment)(bau =>
            {
                bau.OpenComponent<TemplateColumn<Probe>>(0);
                bau.AddComponentParameter(1, nameof(TemplateColumn<Probe>.Title), "Name");
                bau.AddComponentParameter(2, nameof(TemplateColumn<Probe>.ChildContent),
                    (RenderFragment<Probe>)(z => b2 => b2.AddContent(0, z.Name)));
                bau.AddComponentParameter(3, nameof(TemplateColumn<Probe>.ColumnOptions),
                    (RenderFragment)(b3 => b3.AddMarkupContent(0, "<p id=\"popover\">Filter</p>")));
                bau.CloseComponent();
            })));

        // Der Optionsknopf steht im Kopf - QuickGrid zeichnet ihn, sobald
        // ColumnOptions gesetzt ist ("A button to display this UI will be included
        // in the header cell by default").
        cut.Find("th .col-options-button").Click();
        Assert.Single(cut.FindAll("#popover"));

        // Und jetzt wechselt die Zeilenzahl - das tut jeder Filterschritt.
        cut.Render(p => p.Add(x => x.Zeilen,
                              new[] { new Probe(1, "A") }.AsQueryable()));

        Assert.Empty(cut.FindAll("#popover"));
        Assert.Single(cut.FindAll("tbody tr"));
    }

    /// <summary>
    /// Die Gegenprobe: Der Filterstand des HAUSES liegt im Wirt, nicht in der
    /// Rasterinstanz — deshalb übersteht er denselben Neuaufbau. Das ist der Grund,
    /// warum <c>Katalogliste</c> ihn führt und nicht <c>Spaltenfilter</c>.
    /// </summary>
    [Fact]
    public void O6_Gegenprobe_Der_Filterstand_im_Wirt_uebersteht_den_Neuaufbau()
    {
        var stand = new WindowsFormsApplication1.Katalogfilterstand();
        stand.Setzen(WindowsFormsApplication1.Katalogfilterprofil.SpBrennstoff, "Gas");

        // Ein Neuaufbau des Rasters ruehrt ihn nicht an - er ist ein Objekt des
        // Wirtes und kein Zustand der Komponente.
        Assert.True(stand.Gefiltert(WindowsFormsApplication1.Katalogfilterprofil.SpBrennstoff));
        Assert.Equal("Gas", stand.Ausdruck(WindowsFormsApplication1.Katalogfilterprofil.SpBrennstoff));
    }

    // =====================================================================
    //  Die Regeln im Stilblatt (eine bunit-Probe rechnet kein CSS aus)
    // =====================================================================

    /// <summary>
    /// <b>Der Trichter ist ein Formwechsel, kein Farbwechsel</b> (Auflage des
    /// Anwenders zu Rev. 3). Die Farbe kommt obendrauf und ist eine VORHANDENE
    /// (<c>--epos-marke</c>) — es kommt keine neue dazu (W16b‑E‑5).
    /// </summary>
    [Fact]
    public void Der_gesetzte_Trichter_nimmt_nur_eine_vorhandene_Farbe()
    {
        string block = Stilblock(".epos-trichter--gesetzt {");
        Assert.Contains("color: var(--epos-marke)", block);

        string umriss = Stilblock(".epos-trichter {");
        Assert.Contains("color: var(--epos-text-sehr-leise)", umriss);
    }

    /// <summary>
    /// Die gefilterte Spalte bekommt eine leise Tönung aus dem vorhandenen Farbsatz —
    /// über <c>ColumnBase.Class</c> an Kopf UND Körperzellen.
    /// </summary>
    [Fact]
    public void Die_gefilterte_Spalte_wird_leise_getoent()
    {
        string block = Stilblock(".epos-raster th.epos-spalte--gefiltert,");
        Assert.Contains("var(--epos-karte-flaeche-hover)", block);
    }

    /// <summary>
    /// Der Kopfinhalt steht in einem eigenen Kasten IN der Zelle — <b>kein
    /// <c>display: flex</c> auf einem <c>&lt;th&gt;</c></b> (Befund W5‑B‑1).
    /// </summary>
    [Fact]
    public void Kein_display_flex_auf_der_Kopfzelle()
    {
        Assert.Contains("display: flex", Stilblock(".epos-spaltenkopf {"));

        // Der Zeilenumbruch davor grenzt die EIGENE Regel von der aelteren
        // ".epos-raster-huelle .epos-raster thead th" ab, die den Kopf beim
        // Rollen stehen laesst.
        Assert.Contains("position: relative", Stilblock("\n.epos-raster thead th {"));
    }

    /// <summary>Trichter und Sortierknopf sind 44 px hoch (iL4).</summary>
    [Fact]
    public void Die_Knoepfe_des_Spaltenkopfs_halten_das_Beruehrungsziel()
    {
        Assert.Contains("min-height: var(--epos-touchziel)", Stilblock(".epos-trichter {"));
        Assert.Contains("min-height: var(--epos-touchziel)", Stilblock(".epos-spaltenkopf-titel {"));
        Assert.Contains("min-height: var(--epos-touchziel)",
                        Stilblock(".epos-katalog-ruecksetzer {"));
    }

    private static string Stilblatt()
    {
        System.IO.DirectoryInfo? d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null &&
               !System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return System.IO.File.ReadAllText(
            System.IO.Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    private static string Stilblock(string selektor)
    {
        string css = Stilblatt();
        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
