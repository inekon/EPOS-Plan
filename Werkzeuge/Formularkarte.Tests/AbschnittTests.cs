using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Abschnitte: Die Karte gliedert eine Maske nach ihren Behaeltern
/// (GroupBox, TabControl, TabPage, Panel) - genauso, wie der Anwender sie
/// sieht. Geprueft an zwei Masken: <c>Form_Kostenprofil</c>
/// (TabControl mit drei Reitern, darin ein Chart) und <c>Wizard_WPItem</c>
/// (GroupBox, darin ein TabControl mit zwei Reitern).
///
/// <para><b>Beide sind eingefrorene Pruefmuster.</b> Sie sind im Bestand geloescht -
/// Form_Kostenprofil mit iU9-W3.4, Wizard_WPItem mit iU9-W7.4 -, taugen aber
/// weiterhin als Analysegegenstand: Ein Behaelterbaum aus GroupBox, TabControl und
/// TabPage ist genau das, was diese Tests pruefen. Das Rezept steht in
/// <c>Werkzeuge/Formularkarte/LIESMICH.md</c>.</para>
/// </summary>
public sealed class AbschnittTests
{
    private static List<Abschnitt> Abschnitte(string relativ) =>
        Kartenbau.Abschnitte(Kartenbau.Vollstaendig(Repowurzel.Designer(relativ)));

    /// <summary>Dasselbe aus einem eingefrorenen Pruefmuster (iU9-W3.6).</summary>
    private static List<Abschnitt> Musterabschnitte(string relativ) =>
        Kartenbau.Abschnitte(Kartenbau.Vollstaendig(
            Repowurzel.Pruefmuster(relativ), null, Repowurzel.PruefmusterWurzel));

    // ---- Form_Kosten/Form_Kostenprofil: TabControl mit drei Reitern --------

    [Fact]
    public void Kostenprofil_HatFensterUndDreiReiter()
    {
        var abschnitte = Musterabschnitte("Kosten/Form_Kostenprofil.Designer.cs");

        Assert.Equal(new[] { "Fenster", "Monatswerte", "Wochenwerte", "Grafik" },
                     abschnitte.Select(a => a.Titel).ToArray());
        Assert.Null(abschnitte[0].Traeger);
        Assert.All(abschnitte.Skip(1), a => Assert.Equal("TabPage", a.Traeger!.Typ));
    }

    [Fact]
    public void Kostenprofil_ReiterHaengenAmTabControl()
    {
        var abschnitte = Musterabschnitte("Kosten/Form_Kostenprofil.Designer.cs");

        // Das TabControl selbst hat keine eigenen Zeilen und faellt deshalb
        // aus der Liste; seine Reiter stehen eine Stufe tiefer.
        Assert.All(abschnitte.Skip(1), a => Assert.Equal("tabs", a.Traeger!.Elter));
        Assert.All(abschnitte.Skip(1), a => Assert.Equal(2, a.Tiefe));
    }

    [Fact]
    public void Kostenprofil_ChartWirdZuChartBild()
    {
        var grafik = Musterabschnitte("Kosten/Form_Kostenprofil.Designer.cs").Single(a => a.Titel == "Grafik");

        var zeile = Assert.Single(grafik.Zeilen);
        Assert.Equal("chart", zeile.Element.Name);
        Assert.Equal("Chart", zeile.Element.Typ);
        Assert.Equal("ChartBild", zeile.Komponente);
    }

    [Fact]
    public void Kostenprofil_ListBoxWirdAuswahlfeldUndBekommtSeinLabel()
    {
        var woche = Musterabschnitte("Kosten/Form_Kostenprofil.Designer.cs").Single(a => a.Titel == "Wochenwerte");
        var zeile = woche.Zeilen.Single(z => z.Element.Name == "lbTag");

        Assert.Equal("Auswahlfeld", zeile.Komponente);
        Assert.Equal("Wochentag:", zeile.TextDe);
    }

    [Fact]
    public void Kostenprofil_BeschriftungWirktNurInnerhalbDesAbschnitts()
    {
        var abschnitte = Musterabschnitte("Kosten/Form_Kostenprofil.Designer.cs");
        var fenster = abschnitte[0];

        // lblName steht im Fenster und beschriftet tbBezeichner; die Label in
        // den Reitern bleiben dort und werden zu eigenen Textzeilen.
        Assert.Equal("Bezeichner:", fenster.Zeilen.Single(z => z.Element.Name == "tbBezeichner").TextDe);
        Assert.Contains(abschnitte.Single(a => a.Titel == "Monatswerte").Zeilen,
                        z => z.Element.Name == "lblKopfMonat" && z.Komponente == "Text");
    }

    // ---- Wizard_WPItem: GroupBox neben TabControl -------------------------

    [Fact]
    public void WpItem_HatDreiGruppenUndZweiReiter()
    {
        var abschnitte = Musterabschnitte("Wizard/Wizard_WPItem.Designer.cs");

        var kenndaten = abschnitte.Single(a => a.Traeger?.Name == "groupBox2");
        Assert.Equal("GroupBox", kenndaten.Traeger!.Typ);
        Assert.Equal("Wärmepumpen Kenndaten", kenndaten.Titel);
        Assert.Equal(1, kenndaten.Tiefe);

        var cop = abschnitte.Single(a => a.Traeger?.Name == "tabPage1");
        Assert.Equal("TabPage", cop.Traeger!.Typ);
        Assert.Equal("tabControl1", cop.Traeger.Elter);
        Assert.Equal(2, cop.Tiefe);

        Assert.Equal(3, abschnitte.Count(a => a.Traeger?.Typ == "GroupBox"));
        Assert.Equal(2, abschnitte.Count(a => a.Traeger?.Typ == "TabPage"));
    }

    [Fact]
    public void WpItem_ErkenntGanzzahlfelderAusDerFormCs()
    {
        var kenndaten = Musterabschnitte("Wizard/Wizard_WPItem.Designer.cs")
            .Single(a => a.Traeger?.Name == "groupBox2");

        // Program.GanzzahlPruefen(textBox_PHeizstab, ...) in Wizard_WPItem.cs
        var heizstab = kenndaten.Zeilen.Single(z => z.Element.Name == "textBox_PHeizstab");
        Assert.Equal("Ganzzahl", heizstab.Feldtyp);
        Assert.Equal("Ganzzahlfeld", heizstab.Komponente);

        // Ein reines Anzeigefeld bleibt Textfeld - und wird als "nur lesen" vermerkt.
        var nennleistung = kenndaten.Zeilen.Single(z => z.Element.Name == "textBox_Nennleistung");
        Assert.Equal("Textfeld", nennleistung.Komponente);
        Assert.Contains("nur lesen", nennleistung.Bereich);
    }

    // ---- Form_Klimadaten: Panel ueber TabControl ueber TabPage ------------
    //
    // iU9-W14c.9: Die Maske ist mit Welle 14c gefallen; ihre drei Dateien liegen
    // seither als PRUEFMUSTER unter Pruefmuster/Klimadaten/ (Muster W2/W4/W7/W13).
    // Sie ist die EINZIGE Maske des Bestands gewesen, deren btn_Help im DESIGNER
    // stand statt ueber InfoKnopf.Anbringen - und genau das pruefen die zwei Faelle
    // hier und der Skelettfall in RazorSchreiberTests. Ein Umhaengen auf eine andere
    // Maske haette den Fall inhaltlich veraendert.

    [Fact]
    public void Klimadaten_GehtDreiStufenTief()
    {
        var maske = Kartenbau.Vollstaendig(
            Repowurzel.Pruefmuster("Klimadaten/Form_Klimadaten.Designer.cs"),
            null, Repowurzel.PruefmusterWurzel);
        var abschnitte = Kartenbau.Abschnitte(maske);

        // Panel -> TabControl -> TabPage: die Kette steht in Elter, die Tiefe
        // im Abschnitt.
        var reiter = abschnitte.Single(a => a.Traeger?.Name == "tabPage1");
        Assert.Equal(3, reiter.Tiefe);
        Assert.Equal("tabControl1", reiter.Traeger!.Elter);
        Assert.Equal("panel_KlimaGraph", maske.Finden("tabControl1")!.Elter);
        Assert.Null(maske.Finden("panel_KlimaGraph")!.Elter);
    }

    [Fact]
    public void Klimadaten_HilfeknopfWirdInfoKnopf()
    {
        var zeile = Musterabschnitte("Klimadaten/Form_Klimadaten.Designer.cs")
            .SelectMany(a => a.Zeilen)
            .Single(z => z.Element.Name == "btn_Help");

        Assert.Equal("Hilfe", zeile.Feldtyp);
        Assert.Equal("InfoKnopf", zeile.Komponente);
    }

    [Fact]
    public void WpItem_GruppenkoepfeSindDasZielDerBehaelter()
    {
        var maske = Kartenbau.Vollstaendig(
            Repowurzel.Pruefmuster("Wizard/Wizard_WPItem.Designer.cs"), null, Repowurzel.PruefmusterWurzel);

        Assert.Equal("Gruppenkopf", Kartenbau.Ziel(maske.Finden("groupBox2")!).Komponente);
        Assert.Equal("Gruppenkopf", Kartenbau.Ziel(maske.Finden("tabPage1")!).Komponente);
        Assert.Equal("Aufteilung", Kartenbau.Ziel(maske.Finden("tabControl1")!).Komponente);
    }
}
