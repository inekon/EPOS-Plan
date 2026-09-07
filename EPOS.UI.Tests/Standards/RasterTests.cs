using System.Linq;
using Bunit;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>Raster&lt;TZeile&gt; - die Hausklasse ueber QuickGrid.</summary>
public class RasterTests : BunitContext
{
    private sealed record Zeile(int Id, string Bezeichner);

    [Fact]
    public void Raster_zeigt_Kopfzeile_und_Zeilen()
    {
        // QuickGrid laedt beim ersten Zeichnen ein JS-Modul; im lockeren Modus
        // beantwortet bunit das, ohne dass der Test ein Modul stellen muss.
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = new[]
        {
            new Zeile(3, "Erdgas"),
            new Zeile(7, "Fernwaerme")
        }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.KindInhalt, (RenderFragment)(bau =>
            {
                bau.OpenComponent<PropertyColumn<Zeile, string>>(0);
                bau.AddComponentParameter(1, nameof(PropertyColumn<Zeile, string>.Property),
                                          (System.Linq.Expressions.Expression<Func<Zeile, string>>)(z => z.Bezeichner));
                bau.AddComponentParameter(2, nameof(PropertyColumn<Zeile, string>.Title), "Bezeichnung");
                bau.CloseComponent();
            })));

        Assert.Contains("epos-raster", cut.Find("table").ClassName);
        Assert.Contains("Bezeichnung", cut.Find("thead").TextContent);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Contains("Fernwaerme", cut.Find("tbody").TextContent);
        Assert.DoesNotContain("epos-raster--bearbeitbar", cut.Find("table").ClassName);
    }

    // =====================================================================
    // Bearbeitbare Zellen (iU9-W3.0)
    // =====================================================================

    private sealed class Satz
    {
        internal bool Aktiv;
        internal double? Wert;
    }

    /// <summary>
    /// Ein Schalter und ein Zahlenfeld in TemplateColumns: Das Raster zeigt sie,
    /// und ihre Aenderung erreicht die Zeile - der Ersatz fuer die editierbaren
    /// Spalten des DataGridView.
    /// </summary>
    [Fact]
    public void Bearbeitbare_Zellen_melden_ihre_Aenderung_an_die_Zeile()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var satz = new Satz { Aktiv = false, Wert = 201.0 };
        var zeilen = new[] { satz }.AsQueryable();

        var cut = Render<Raster<Satz>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Bearbeitbar, true)
            .Add(x => x.KindInhalt, (RenderFragment)(bau =>
            {
                bau.OpenComponent<TemplateColumn<Satz>>(0);
                bau.AddComponentParameter(1, nameof(TemplateColumn<Satz>.Title), "aktiv");
                bau.AddComponentParameter(2, nameof(TemplateColumn<Satz>.ChildContent),
                    (RenderFragment<Satz>)(z => kind =>
                    {
                        kind.OpenComponent<Schalter>(0);
                        kind.AddComponentParameter(1, nameof(Schalter.Wert), z.Aktiv);
                        kind.AddComponentParameter(2, nameof(Schalter.WertChanged),
                            EventCallback.Factory.Create<bool>(this, b => z.Aktiv = b));
                        kind.CloseComponent();
                    }));
                bau.CloseComponent();

                bau.OpenComponent<TemplateColumn<Satz>>(3);
                bau.AddComponentParameter(4, nameof(TemplateColumn<Satz>.Title), "Wert");
                bau.AddComponentParameter(5, nameof(TemplateColumn<Satz>.ChildContent),
                    (RenderFragment<Satz>)(z => kind =>
                    {
                        kind.OpenComponent<Zahlenfeld>(0);
                        kind.AddComponentParameter(1, nameof(Zahlenfeld.Wert), z.Wert);
                        kind.AddComponentParameter(2, nameof(Zahlenfeld.WertChanged),
                            EventCallback.Factory.Create<double?>(this, w => z.Wert = w));
                        kind.CloseComponent();
                    }));
                bau.CloseComponent();
            })));

        Assert.Contains("epos-raster--bearbeitbar", cut.Find("table").ClassName);
        Assert.Equal(2, cut.FindAll("tbody td").Count);

        cut.Find("tbody td:first-child input").Change(true);
        Assert.True(satz.Aktiv);

        cut.Find("tbody td:last-child input").Input("55,5");
        Assert.Equal(55.5, satz.Wert);
    }

    // =====================================================================
    // Lange Listen (iU9-W13.0l)
    // =====================================================================

    /// <summary>
    /// Ohne <c>Virtualisiert</c> zeichnet QuickGrid JEDE Zeile - fuer die
    /// 20 746 Zeilen der CEC-Modulliste zu viel. Mit dem Schalter haelt es nur
    /// den sichtbaren Ausschnitt im Baum, und die Huelle bekommt die feste
    /// Hoehe, ohne die es nichts zu rollen gaebe.
    /// </summary>
    [Fact]
    public void Virtualisiert_zeichnet_nur_den_sichtbaren_Ausschnitt()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = Enumerable.Range(0, 2000)
                               .Select(i => new Zeile(i, "Modul " + i))
                               .AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Contains("epos-raster-huelle--hoch", cut.Find("div").ClassName);
        Assert.True(cut.FindAll("tbody tr").Count < 2000,
                    "Eine virtualisierte Liste darf nicht alle 2 000 Zeilen zeichnen.");
    }

    [Fact]
    public void Ohne_den_Schalter_bleibt_die_Huelle_die_gewohnte()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var zeilen = new[] { new Zeile(1, "Erdgas") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Equal("epos-raster-huelle", cut.Find("div").ClassName);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    // =====================================================================
    // Eine neue Zeilenmenge bekommt ein neues Raster (Befund W6-B-2)
    // =====================================================================

    /// <summary>
    /// <b>Der Waechter zum Befund W6‑B‑2</b> (Windows 07.09.2026, „der Filter bei der
    /// Herstellerauswahl funktioniert nicht"): QuickGrid traegt den virtualisierten und
    /// den flachen Weg in EINER Instanz, und der flache Zwischenspeicher
    /// (<c>_currentNonVirtualizedViewItems</c>) wird nur EINMAL gefuellt — beim ersten
    /// Datenabruf, als das <c>@ref</c> auf das Virtualize-Kind noch <c>null</c> war,
    /// und ohne Pagination mit der GANZEN Liste. Faellt der Schalter danach auf
    /// <c>false</c> (2 343 Zeilen → 109 nach einem Herstellerfilter), zeichnet
    /// QuickGrid genau diesen uralten Stand.
    ///
    /// <para>Der Fix ist ein <c>@key</c> an (Schalter, Zeilenzahl). Geprueft wird
    /// deshalb die IDENTITAET der Rasterkomponente: Nach dem Wechsel muss es eine
    /// ANDERE Instanz sein — eine frische faengt mit leerem Zustand an.</para>
    /// </summary>
    [Fact]
    public void Der_Wechsel_des_Virtualisierungsschalters_baut_das_Raster_neu_auf()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var viele = Enumerable.Range(0, 2343)
                              .Select(i => new Zeile(i, "ABB " + i))
                              .AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, viele)
            .Add(x => x.Virtualisiert, true)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> vorher = cut.FindComponent<QuickGrid<Zeile>>().Instance;
        Assert.Equal((true, 2343), cut.Instance.Rasterstand);

        // Der Herstellerfilter: 2 343 -> 109, und damit faellt der Schalter.
        var wenige = Enumerable.Range(0, 109)
                               .Select(i => new Zeile(i, "SMA America " + i))
                               .AsQueryable();

        cut.Render(p => p
            .Add(x => x.Zeilen, wenige)
            .Add(x => x.Virtualisiert, false)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> nachher = cut.FindComponent<QuickGrid<Zeile>>().Instance;

        Assert.Equal((false, 109), cut.Instance.Rasterstand);
        Assert.NotSame(vorher, nachher);

        // ... und in der Tabelle stehen die NEUEN Zeilen, keine einzige alte.
        string tabelle = cut.Find("tbody").TextContent;
        Assert.Contains("SMA America", tabelle);
        Assert.DoesNotContain("ABB", tabelle);
    }

    /// <summary>
    /// Auch ohne Wechsel des Schalters bekommt eine andere Zeilenzahl ein neues
    /// Raster — so steht die Liste nach einem Filterwechsel wieder am ANFANG statt
    /// im Nirgendwo der alten Rollposition.
    /// </summary>
    [Fact]
    public void Eine_andere_Zeilenzahl_baut_das_Raster_neu_auf()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var drei = new[] { new Zeile(1, "a"), new Zeile(2, "b"), new Zeile(3, "c") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, drei)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> vorher = cut.FindComponent<QuickGrid<Zeile>>().Instance;

        cut.Render(p => p
            .Add(x => x.Zeilen, new[] { new Zeile(2, "b") }.AsQueryable())
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.NotSame(vorher, cut.FindComponent<QuickGrid<Zeile>>().Instance);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    /// <summary>
    /// <b>Und die Gegenprobe:</b> Bleiben Schalter und Zeilenzahl gleich, BLEIBT das
    /// Raster dieselbe Instanz. Das ist keine Feinheit — ein Raster mit
    /// Bedienelementen in den Zellen (<c>Bearbeitbar</c>) wuerde dem Anwender sonst
    /// mitten in der Eingabe unter den Fingern neu aufgebaut, und der Eingabepunkt
    /// waere weg. Deshalb haengt der Schluessel an der ZAHL und nicht an der
    /// Zeilenmenge: Beim Tippen in einer Zelle aendert sie sich nicht.
    /// </summary>
    [Fact]
    public void Dieselbe_Zeilenzahl_behaelt_die_Rasterinstanz()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var erst = new[] { new Zeile(1, "Erdgas"), new Zeile(2, "Heizoel") }.AsQueryable();

        var cut = Render<Raster<Zeile>>(p => p
            .Add(x => x.Zeilen, erst)
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        QuickGrid<Zeile> vorher = cut.FindComponent<QuickGrid<Zeile>>().Instance;

        // Ein neuer AsQueryable-Aufruf ueber DIESELBEN Zeilen - genau das, was ein
        // Wirt bei jedem Render hereinreicht.
        cut.Render(p => p
            .Add(x => x.Zeilen, new[] { new Zeile(1, "Erdgas"), new Zeile(2, "Heizoel") }.AsQueryable())
            .Add(x => x.KindInhalt, Bezeichnerspalte()));

        Assert.Same(vorher, cut.FindComponent<QuickGrid<Zeile>>().Instance);
    }

    private static RenderFragment Bezeichnerspalte() => bau =>
    {
        bau.OpenComponent<PropertyColumn<Zeile, string>>(0);
        bau.AddComponentParameter(1, nameof(PropertyColumn<Zeile, string>.Property),
                                  (System.Linq.Expressions.Expression<Func<Zeile, string>>)(z => z.Bezeichner));
        bau.AddComponentParameter(2, nameof(PropertyColumn<Zeile, string>.Title), "Bezeichnung");
        bau.CloseComponent();
    };
}
