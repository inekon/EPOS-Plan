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
}
