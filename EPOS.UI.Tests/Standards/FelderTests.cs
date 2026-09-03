using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>Textfeld, Auswahlfeld, Datumsfeld und Schalter.</summary>
public class FelderTests : BunitContext
{
    [Fact]
    public void Textfeld_meldet_jede_Eingabe()
    {
        string? erhalten = null;
        var cut = Render<Textfeld>(p => p
            .Add(x => x.Bezeichnung, "Bezeichnung")
            .Add(x => x.WertChanged, (string w) => erhalten = w));

        cut.Find("input").Input("Erdgas Grundversorgung");

        Assert.Equal("Erdgas Grundversorgung", erhalten);
        Assert.Equal("Bezeichnung", cut.Find(".epos-feld-text").TextContent);
    }

    [Fact]
    public void Auswahlfeld_zeigt_alle_Eintraege()
    {
        var cut = Render<Auswahlfeld>(p => p
            .Add(x => x.Eintraege, new[] { (3, "Erdgas"), (7, "Fernwaerme") }));

        var optionen = cut.FindAll("option");
        Assert.Equal(2, optionen.Count);
        Assert.Equal("Erdgas", optionen[0].TextContent);
        Assert.Equal("7", optionen[1].GetAttribute("value"));
    }

    [Fact]
    public void Auswahlfeld_liefert_die_Id()
    {
        int? erhalten = null;
        var cut = Render<Auswahlfeld>(p => p
            .Add(x => x.Eintraege, new[] { (3, "Erdgas"), (7, "Fernwaerme") })
            .Add(x => x.AuswahlChanged, (int? id) => erhalten = id));

        cut.Find("select").Change("7");

        Assert.Equal(7, erhalten);
    }

    [Fact]
    public void Auswahlfeld_meldet_null_fuer_den_Platzhalter()
    {
        int? erhalten = 3;
        bool gemeldet = false;
        var cut = Render<Auswahlfeld>(p => p
            .Add(x => x.Eintraege, new[] { (3, "Erdgas") })
            .Add(x => x.Platzhalter, "(bitte waehlen)")
            .Add(x => x.Auswahl, 3)
            .Add(x => x.AuswahlChanged, (int? id) => { erhalten = id; gemeldet = true; }));

        cut.Find("select").Change("");

        Assert.True(gemeldet);
        Assert.Null(erhalten);
        Assert.Equal(2, cut.FindAll("option").Count);
    }

    [Fact]
    public void Auswahlfeld_ist_bedienbar_und_laesst_sich_sperren()
    {
        // Aktiv=false wird von Feldern gebraucht, die ANGEKUENDIGT, aber noch nicht
        // pflegbar sind - Luecke K3 des Dialogs "BHKW-Wirtschaftlichkeit".
        var offen = Render<Auswahlfeld>(p => p
            .Add(x => x.Eintraege, new[] { (3, "Erdgas") }));
        Assert.False(offen.Find("select").HasAttribute("disabled"));

        var gesperrt = Render<Auswahlfeld>(p => p
            .Add(x => x.Eintraege, new[] { (3, "Erdgas") })
            .Add(x => x.Aktiv, false));
        Assert.True(gesperrt.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Datumsfeld_liest_und_schreibt_im_ISO_Format()
    {
        DateOnly? erhalten = null;
        var cut = Render<Datumsfeld>(p => p
            .Add(x => x.Wert, new DateOnly(2026, 9, 3))
            .Add(x => x.WertChanged, (DateOnly? d) => erhalten = d));

        Assert.Equal("date", cut.Find("input").GetAttribute("type"));
        Assert.Equal("2026-09-03", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Change("2027-01-15");

        Assert.Equal(new DateOnly(2027, 1, 15), erhalten);
    }

    [Fact]
    public void Schalter_meldet_die_Umschaltung()
    {
        bool? erhalten = null;
        var cut = Render<Schalter>(p => p
            .Add(x => x.Bezeichnung, "Ohne Variante")
            .Add(x => x.WertChanged, (bool w) => erhalten = w));

        cut.Find("input").Change(true);

        Assert.True(erhalten);
        Assert.Equal("checkbox", cut.Find("input").GetAttribute("type"));
    }
}
