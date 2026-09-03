using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Die Zeilenregel fuer Beschriftungen, an gebauten Faellen geprueft.
///
/// <para>
/// Das im Konzept vermutete Raster "Label bei x=28, Steuerelement bei x=270"
/// gibt es im Bestand nicht (Label-x liegen bei 12 bis 18, Feld-x je Maske
/// verschieden). Tragfaehig ist die Zeilenregel - und die hat Grenzen, die
/// hier ebenfalls festgehalten sind.
/// </para>
/// </summary>
public sealed class LabelRegelTests
{
    private static Maske Bauen(params (string Name, string Typ, int X, int Y, string Text)[] elemente)
    {
        var maske = new Maske { Bezeichner = "Probe", Klasse = "Probe" };
        var reihenfolge = 0;
        foreach (var (name, typ, x, y, text) in elemente)
        {
            var element = new Steuerelement
            {
                Name = name,
                Typ = typ,
                Art = Typtabelle.Einordnen(typ),
                Reihenfolge = reihenfolge++,
                Eingehaengt = true
            };
            element.Eigenschaften["Location"] = x + ", " + y;
            if (text.Length > 0) element.Eigenschaften["Text"] = text;
            maske.Steuerelemente.Add(element);
        }
        LabelRegel.Anwenden(maske);
        return maske;
    }

    [Fact]
    public void LabelLinksInDerselbenZeileGewinnt()
    {
        var maske = Bauen(("lbl", "Label", 13, 29, "Energieträger:"),
                          ("feld", "TextBox", 159, 26, ""));

        Assert.Equal("lbl", maske.Finden("feld")!.Beschriftung!.Name);
    }

    [Fact]
    public void DasNaechsteLabelLinksGewinnt()
    {
        var maske = Bauen(("weit", "Label", 10, 60, "weit weg"),
                          ("nah", "Label", 100, 60, "nah"),
                          ("feld", "TextBox", 160, 60, ""));

        Assert.Equal("nah", maske.Finden("feld")!.Beschriftung!.Name);
    }

    [Fact]
    public void AchtPixelVersatzGehenNochDurch()
    {
        Assert.NotNull(Bauen(("lbl", "Label", 10, 52, "x"), ("feld", "TextBox", 100, 60, ""))
                       .Finden("feld")!.Beschriftung);
        Assert.Null(Bauen(("lbl", "Label", 10, 51, "x"), ("feld", "TextBox", 100, 60, ""))
                    .Finden("feld")!.Beschriftung);
    }

    [Fact]
    public void OhneTrefferInDerZeileGiltDasLabelDarueber()
    {
        var maske = Bauen(("lbl", "Label", 100, 40, "darüber"),
                          ("feld", "TextBox", 104, 60, ""));

        Assert.Equal("lbl", maske.Finden("feld")!.Beschriftung!.Name);
    }

    [Fact]
    public void LabelDarueberDarfHoechstens24PixelEntferntStehen()
    {
        Assert.NotNull(Bauen(("lbl", "Label", 100, 36, "x"), ("feld", "TextBox", 100, 60, ""))
                       .Finden("feld")!.Beschriftung);
        Assert.Null(Bauen(("lbl", "Label", 100, 35, "x"), ("feld", "TextBox", 100, 60, ""))
                    .Finden("feld")!.Beschriftung);
    }

    [Fact]
    public void EinLabelBeschriftetNurEinFeld()
    {
        var maske = Bauen(("lbl", "Label", 10, 60, "einmal"),
                          ("feld1", "TextBox", 100, 60, ""),
                          ("feld2", "TextBox", 200, 60, ""));

        // feld1 steht links und greift zuerst zu; feld2 findet danach nur noch
        // ein vergebenes Label und bleibt ohne Beschriftung.
        Assert.Equal("lbl", maske.Finden("feld1")!.Beschriftung!.Name);
        Assert.Null(maske.Finden("feld2")!.Beschriftung);
    }

    [Fact]
    public void LabelAusEinemAnderenAbschnittZaehltNicht()
    {
        var maske = Bauen(("lbl", "Label", 10, 60, "aus der Gruppe"),
                          ("feld", "TextBox", 100, 60, ""));
        maske.Finden("lbl")!.Elter = "groupBox1";
        LabelRegel.Anwenden(maske);

        Assert.Null(maske.Finden("feld")!.Beschriftung);
    }

    [Fact]
    public void KnoepfeBekommenKeineBeschriftung()
    {
        var maske = Bauen(("lbl", "Label", 10, 60, "Text"),
                          ("btn", "Button", 100, 60, "OK"));

        Assert.Null(maske.Finden("btn")!.Beschriftung);
        Assert.False(maske.Finden("lbl")!.AlsBeschriftungVerbraucht);
    }

    [Fact]
    public void EinFreiesLabelBleibtEineEigeneZeile()
    {
        var maske = Bauen(("hinweis", "Label", 10, 200, "Hinweistext"));
        Assert.True(Kartenbau.Zeilenwuerdig(maske.Finden("hinweis")!));
    }
}
