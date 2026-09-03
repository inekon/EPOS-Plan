using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// Ganzzahlfeld - Komma und Punkt sind hier bewusst KEINE gueltigen Zeichen
/// (Program.GanzzahlParsen, Program.cs:466-476).
/// </summary>
public class GanzzahlfeldTests : BunitContext
{
    [Fact]
    public void Ganze_Zahl_wird_uebernommen()
    {
        int? erhalten = null;
        var cut = Render<Ganzzahlfeld>(p => p.Add(x => x.WertChanged, (int? w) => erhalten = w));

        cut.Find("input").Input("12");

        Assert.Equal(12, erhalten);
        Assert.False(cut.Instance.Fehlerhaft);
    }

    [Fact]
    public void Komma_ist_im_Ganzzahlfeld_ungueltig()
    {
        int? erhalten = null;
        var cut = Render<Ganzzahlfeld>(p => p.Add(x => x.WertChanged, (int? w) => erhalten = w));

        cut.Find("input").Input("1,5");

        Assert.Null(erhalten);
        Assert.True(cut.Instance.Fehlerhaft);
        Assert.Contains("epos-fehleingabe", cut.Find("input").ClassName);
    }

    [Fact]
    public void Leeres_Feld_meldet_null()
    {
        int? erhalten = 3;
        bool gemeldet = false;
        var cut = Render<Ganzzahlfeld>(p => p
            .Add(x => x.Wert, 3)
            .Add(x => x.WertChanged, (int? w) => { erhalten = w; gemeldet = true; }));

        cut.Find("input").Input("");

        Assert.True(gemeldet);
        Assert.Null(erhalten);
        Assert.False(cut.Instance.Fehlerhaft);
    }

    [Fact]
    public void Wert_ausserhalb_des_Bereichs_faerbt()
    {
        int? erhalten = null;
        var cut = Render<Ganzzahlfeld>(p => p
            .Add(x => x.Min, 1)
            .Add(x => x.Max, 50)
            .Add(x => x.WertChanged, (int? w) => erhalten = w));

        cut.Find("input").Input("0");

        Assert.Null(erhalten);
        Assert.True(cut.Instance.Fehlerhaft);
    }
}
