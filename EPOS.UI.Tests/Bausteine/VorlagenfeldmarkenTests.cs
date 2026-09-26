using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// BV-E6 (Konzept Berichtsvorlagen 9.4, 9.6): <b>der Parameter <c>Vorlagenfeld</c></b> an
/// <see cref="Kennzahlkachel"/>, <see cref="DiagrammSvg"/>, <see cref="Vergleichstabelle"/> und
/// <see cref="Textfeld"/>. Ohne ihn bleibt das Markup, wie es war — keine Marke, keine Kopfzeile;
/// mit ihm steht genau EINE Marke an der vorgesehenen Stelle, gelesen über das stabile Merkmal
/// <c>data-vorlagenfeld</c> der Marke (nie über die Kennung eines Diagramms).
/// </summary>
public class VorlagenfeldmarkenTests : EposBunitContext
{
    private const string MARKE = "[data-vorlagenfeld]";

    public VorlagenfeldmarkenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    //  Kennzahlkachel
    // =====================================================================

    [Fact]
    public void Kachel_ohne_Vorlagenfeld_bleibt_wie_sie_war()
    {
        IRenderedComponent<Kennzahlkachel> k = Render<Kennzahlkachel>(p => p
            .Add(x => x.Titel, "Kapitalwert").Add(x => x.Wert, "12.300 €"));

        Assert.Empty(k.FindAll(MARKE));
        Assert.Empty(k.FindAll(".epos-kennzahlkachel-titelzeile"));
        // Der Titel ist weiter unmittelbares Kind der Karte.
        Assert.Equal("Kapitalwert", k.Find(".epos-kennzahlkachel > .epos-kennzahlkachel-titel").TextContent);
    }

    [Fact]
    public void Kachel_mit_Vorlagenfeld_traegt_die_Marke_in_der_Titelzeile()
    {
        IRenderedComponent<Kennzahlkachel> k = Render<Kennzahlkachel>(p => p
            .Add(x => x.Titel, "Kapitalwert").Add(x => x.Wert, "12.300 €")
            .Add(x => x.Vorlagenfeld, "wirtschaft.beste.kapitalwert"));

        IElement marke = Assert.Single(k.FindAll(MARKE));
        Assert.Equal("wirtschaft.beste.kapitalwert", marke.GetAttribute("data-vorlagenfeld"));
        Assert.Equal("entspricht", marke.GetAttribute("data-vorlagenfeldstufe"));
        Assert.NotNull(k.Find(".epos-kennzahlkachel-titelzeile [data-vorlagenfeld]"));
        Assert.Equal("Kapitalwert", k.Find(".epos-kennzahlkachel-titelzeile .epos-kennzahlkachel-titel").TextContent);
        Assert.Equal("12.300 €", k.Find(".epos-kennzahlkachel-wert").TextContent);
    }

    [Fact]
    public void Kachel_reicht_Stufe_und_Hinweis_weiter()
    {
        IRenderedComponent<Kennzahlkachel> k = Render<Kennzahlkachel>(p => p
            .Add(x => x.Titel, "Investition").Add(x => x.Wert, "7.001,00 €")
            .Add(x => x.Vorlagenfeld, "stamm.wirtschaft.investition")
            .Add(x => x.VorlagenfeldStufe, Vorlagenfeldstufe.Aehnlich)
            .Add(x => x.VorlagenfeldHinweis, "im Bericht aus der Wirtschaftlichkeitsrechnung"));

        IRenderedComponent<Vorlagenfeldknopf> knopf = k.FindComponent<Vorlagenfeldknopf>();
        Assert.Equal(Vorlagenfeldstufe.Aehnlich, knopf.Instance.Stufe);
        Assert.Equal("im Bericht aus der Wirtschaftlichkeitsrechnung", knopf.Instance.StufenHinweis);
    }

    // =====================================================================
    //  DiagrammSvg
    // =====================================================================

    private static Zeichenmodell Modell()
    {
        var werte = new double[8760];
        for (int i = 0; i < werte.Length; i++) werte[i] = Math.Sin(i / 500.0);
        return ChartRenderer.JahresgangModell("Probe",
            new[] { new ChartRenderer.Reihe("Reihe", werte, Farbrolle.AUSSENTEMPERATUR) }, "Stunde", "Wert");
    }

    [Fact]
    public void Diagramm_ohne_Vorlagenfeld_hat_keine_Kopfzeile()
    {
        IRenderedComponent<DiagrammSvg> d = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Modell()).Add(x => x.Kennung, "probe"));

        Assert.Empty(d.FindAll(MARKE));
        Assert.Empty(d.FindAll(".epos-diagramm-kopf"));
        // Die Zoomleiste steht weiter unmittelbar im Rahmen des Bildes.
        Assert.NotNull(d.Find(".epos-diagramm-svg > .epos-diagramm-leiste"));
    }

    [Fact]
    public void Diagramm_mit_Vorlagenfeld_stellt_die_Marke_neben_die_Zoomleiste()
    {
        IRenderedComponent<DiagrammSvg> d = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Modell()).Add(x => x.Kennung, "kapitalwert-spanne")
            .Add(x => x.Vorlagenfeld, "bild.wirtschaft.spanne"));

        IElement marke = Assert.Single(d.FindAll(MARKE));
        // Zugeordnet über den Schlüssel — nie über die Kennung des Bildes.
        Assert.Equal("bild.wirtschaft.spanne", marke.GetAttribute("data-vorlagenfeld"));
        Assert.NotNull(d.Find(".epos-diagramm-kopf > .epos-diagramm-leiste"));
        Assert.NotNull(d.Find(".epos-diagramm-kopf > [data-vorlagenfeld]"));
        // Die Leiste steht VOR der Marke: Sie bricht links um, die Marke hält ihre Spur rechts.
        IElement kopf = d.Find(".epos-diagramm-kopf");
        Assert.Equal("epos-diagramm-leiste", kopf.Children[0].ClassName);
    }

    [Fact]
    public void Diagramm_ohne_Zoom_traegt_die_Marke_allein_in_der_Kopfzeile()
    {
        IRenderedComponent<DiagrammSvg> d = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Modell()).Add(x => x.Kennung, "probe").Add(x => x.OhneZoom, true)
            .Add(x => x.Vorlagenfeld, "bild.wirtschaft.bruecke"));

        Assert.Empty(d.FindAll(".epos-diagramm-leiste"));
        Assert.Single(d.FindAll(".epos-diagramm-kopf > [data-vorlagenfeld]"));
    }

    [Fact]
    public void Diagramm_ohne_Modell_zeigt_keine_Marke()
    {
        IRenderedComponent<DiagrammSvg> d = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, (Zeichenmodell?)null).Add(x => x.Vorlagenfeld, "bild.wirtschaft.spanne"));

        Assert.Empty(d.FindAll(MARKE));
        Assert.NotNull(d.Find(".epos-chartbild-platzhalter"));
    }

    // =====================================================================
    //  Vergleichstabelle
    // =====================================================================

    private static readonly Vergleichszeile[] Zeilen =
    {
        new("Leistung", new[] { "10", "12" }, true),
        new("Hersteller", new[] { "A", "A" }, false),
    };

    [Fact]
    public void Vergleichstabelle_ohne_Vorlagenfeld_bleibt_wie_sie_war()
    {
        IRenderedComponent<Vergleichstabelle> t = Render<Vergleichstabelle>(p => p
            .Add(x => x.Koepfe, new[] { "Satz 1", "Satz 2" }).Add(x => x.Zeilen, Zeilen));

        Assert.Empty(t.FindAll(MARKE));
        Assert.Empty(t.FindAll(".epos-vergleichstabelle-kopf"));
        Assert.NotNull(t.Find(".epos-vergleichstabelle > .epos-vergleichstabelle-schalter"));
    }

    [Fact]
    public void Vergleichstabelle_traegt_eine_Marke_am_Kopf_und_keine_je_Zeile()
    {
        IRenderedComponent<Vergleichstabelle> t = Render<Vergleichstabelle>(p => p
            .Add(x => x.Koepfe, new[] { "Satz 1", "Satz 2" }).Add(x => x.Zeilen, Zeilen)
            .Add(x => x.Vorlagenfeld, "tabelle.wirtschaft.kennzahlen")
            .Add(x => x.VorlagenfeldZusatz, "stand.wirtschaft.<zeile>"));

        IElement marke = Assert.Single(t.FindAll(MARKE));
        Assert.Equal("tabelle.wirtschaft.kennzahlen", marke.GetAttribute("data-vorlagenfeld"));
        Assert.Empty(t.FindAll("table [data-vorlagenfeld]"));
        Assert.NotNull(t.Find(".epos-vergleichstabelle-kopf > .epos-vergleichstabelle-schalter"));
        Assert.Equal("stand.wirtschaft.<zeile>", t.FindComponent<Vorlagenfeldknopf>().Instance.Zusatz);
    }

    // =====================================================================
    //  Textfeld
    // =====================================================================

    [Fact]
    public void Textfeld_ohne_Vorlagenfeld_hat_keine_Marke()
    {
        IRenderedComponent<Textfeld> f = Render<Textfeld>(p => p
            .Add(x => x.Bezeichnung, "Kunde").Add(x => x.Wert, "Firma 1"));
        Assert.Empty(f.FindAll(MARKE));
    }

    [Theory]
    [InlineData(false, "input")]
    [InlineData(true, "textarea")]
    public void Textfeld_stellt_die_Marke_hinter_das_Eingabeelement(bool mehrzeilig, string element)
    {
        IRenderedComponent<Textfeld> f = Render<Textfeld>(p => p
            .Add(x => x.Bezeichnung, "Kunde").Add(x => x.Wert, "Firma 1").Add(x => x.Mehrzeilig, mehrzeilig)
            .Add(x => x.Vorlagenfeld, "projekt.kunde"));

        IElement marke = Assert.Single(f.FindAll(MARKE));
        Assert.Equal("projekt.kunde", marke.GetAttribute("data-vorlagenfeld"));
        // Im label ist das Eingabeelement das ERSTE beschriftbare Element: Die Marke steht dahinter,
        // sonst löste ein Klick auf die Beschriftung die Marke aus.
        IElement label = f.Find("label");
        string html = label.InnerHtml;
        Assert.True(html.IndexOf("<" + element, StringComparison.Ordinal)
                    < html.IndexOf("data-vorlagenfeld", StringComparison.Ordinal));
    }
}
