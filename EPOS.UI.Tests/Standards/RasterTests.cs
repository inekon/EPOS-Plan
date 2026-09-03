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
    }
}
