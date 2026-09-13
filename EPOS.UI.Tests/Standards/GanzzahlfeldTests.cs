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

    /// <summary>
    /// <b>Ein GELEERTES Feld bleibt leer</b> — Gegenstück zu
    /// <c>ZahlenfeldTests.Ein_geleertes_Feld_bleibt_leer_wenn_der_Wirt_seinen_Wert_behaelt</c>,
    /// dort steht der Befund.
    /// </summary>
    [Fact]
    public void Ein_geleertes_Feld_bleibt_leer_wenn_der_Wirt_seinen_Wert_behaelt()
    {
        int wirt = 12;
        var cut = Render<Ganzzahlfeld>(p => p
            .Add(x => x.Wert, wirt)
            .Add(x => x.WertChanged, (int? w) => { if (w.HasValue) wirt = w.Value; }));

        cut.Find("input").Input("1");
        cut.Render(p => p.Add(x => x.Wert, wirt));
        Assert.Equal(1, wirt);

        cut.Find("input").Input("");
        cut.Render(p => p.Add(x => x.Wert, wirt));

        Assert.True(cut.Instance.Geleert);
        Assert.Equal("", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Input("25");
        cut.Render(p => p.Add(x => x.Wert, wirt));
        Assert.False(cut.Instance.Geleert);
        Assert.Equal(25, wirt);
        Assert.Equal("25", cut.Find("input").GetAttribute("value"));
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
    [Fact]
    public void Ohne_Angabe_ist_das_Feld_bedienbar()
    {
        var cut = Render<Ganzzahlfeld>();

        Assert.False(cut.Find("input").HasAttribute("disabled"));
    }

    [Fact]
    public void Aktiv_false_sperrt_das_Feld_laesst_den_Wert_aber_lesbar()
    {
        // iU9-W2.3: HT/NT entfaellt im Rollenmodell (Leitentscheidung L10) -
        // die Stunden bleiben stehen, sind aber nicht mehr zu aendern.
        var cut = Render<Ganzzahlfeld>(p => p
            .Add(x => x.Wert, 6)
            .Add(x => x.Aktiv, false));

        Assert.True(cut.Find("input").HasAttribute("disabled"));
        Assert.Equal("6", cut.Find("input").GetAttribute("value"));
    }
}
