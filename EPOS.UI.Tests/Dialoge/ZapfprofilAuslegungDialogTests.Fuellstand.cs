using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Speichergröße der Füllstandslinie am Wochenbild</b> (Umsetzungskonzept
/// Zapfprofilgenerator 4.7, N10 (k), N11 (d); Mockup „Maßgebende Woche der Stundenbilanz"): EINE
/// Wahl an EINER Stelle — unmittelbar über dem ersten Wochenbild, nicht mehr bei den Eingaben des
/// Verfahrensvergleichs; jeder Eintrag nennt sein Volumen in der Kultur der Oberfläche, die Vorgabe
/// den Bezug, den sie auflöst; ein Bezug ohne Volumen steht gesperrt mit dem Grund des Kerns, auch
/// für den Assistenten; ein Wechsel rechnet neu und zeichnet Bild, Kachel und Herleitung mit dem
/// Ergebnis; jedes weitere Wochenbild nennt den geltenden Bezug.
///
/// <para>Die Auslegung kommt aus einem Prüfdelegaten — der Dialog rechnet nicht. Kultur de-DE,
/// alle Zahlen erfunden.</para>
/// </summary>
public partial class ZapfprofilAuslegungDialogTests
{
    private const string LABEL_FUELLSTAND = "Speichergröße der Füllstandslinie";
    private const string GRUND_OHNE_PUNKT = "Die Summenlinie hat keinen empfohlenen Punkt.";

    /// <summary>Eine Speichergruppe mit Wochenbild beim Bezug <paramref name="bezugL"/> und der Wahl, wie die Hülle sie füllt.</summary>
    private static ZapfprofilAuslegungsgruppeDaten MitFuellstandslinie(ZapfprofilFuellstandbezug art, string name, double bezugL,
                                                                     double kapazitaetKwh)
    {
        ZapfprofilAuslegungsgruppeDaten g = MitLadevorschlag();
        g.Vergleich!.FuellstandBezugArt = art;
        g.Vergleich.FuellstandBezug = name;
        g.Vergleich.FuellstandBezugL = bezugL;
        g.Vergleich.KapazitaetKwh = kapazitaetKwh;
        g.Vergleich.MinFuellstandKwh = kapazitaetKwh / 2;
        g.Vergleich.ReserveAnteil = 0.5;
        g.Vergleich.WochenModell = Woche(bezugL);
        return g;
    }

    /// <summary>Ein Wochenbild aus dem Kern mit der Füllstandslinie beim Bezugsvolumen.</summary>
    private static Zeichenmodell Woche(double bezugL)
    {
        double[] z = Enumerable.Range(0, 168).Select(h => h % 24 == 7 ? 20.0 : 2.0).ToArray();
        double[] eins = Enumerable.Repeat(1.0, 168).ToArray();
        double[] fuellstand = Enumerable.Repeat(bezugL / 40.0, 168).ToArray();
        return ZapfprofilBilder.AuslegungswocheModell(z, eins, eins, new double[168], fuellstand, bezugL, 8, null);
    }

    /// <summary>Die Wahl samt Umgebung: der Kasten über dem Wochenbild.</summary>
    private static IElement Fuellstandwahl<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => Assert.Single(cut.FindAll(".epos-zapfausl-fuellstandwahl"));

    [Fact]
    public void Die_Speichergroesse_der_Fuellstandslinie_steht_ueber_dem_Wochenbild_und_nicht_bei_den_Eingaben()
    {
        ZapfprofilAuslegungStartDaten start = MitWertemengen(Ergebnis(MitFuellstandslinie(
            ZapfprofilFuellstandbezug.NenninhaltPunkt, "Nenninhalt des Punkts", 400, 20)));
        var cut = Aufbauen(start, rechnen: _ => start.Ergebnis!);

        // Genau EIN Feld für den Wert — nicht mehr in der Gruppe „Eingaben des Verfahrensvergleichs".
        Assert.Single(cut.FindAll("label .epos-feld-text"), t => t.TextContent.Trim() == LABEL_FUELLSTAND);
        IElement eingaben = cut.FindAll("section.epos-gruppenkopf")
            .Single(s => s.QuerySelector(".epos-gruppenkopf-titel")!.TextContent.Trim() == "Eingaben des Verfahrensvergleichs");
        Assert.DoesNotContain(LABEL_FUELLSTAND, eingaben.TextContent);
        Assert.DoesNotContain("Füllstandslinie", eingaben.TextContent);

        // Am Verfahrensvergleich, unmittelbar über dem Wochenbild.
        IElement wahl = Fuellstandwahl(cut);
        Assert.NotNull(wahl.Closest(".epos-zapfausl-vergleich"));
        Assert.Contains("epos-diagramm-svg", wahl.NextElementSibling!.ClassName);
        Assert.Contains("Auf dieses Volumen beziehen sich Füllstandslinie und Kachel „Füllstand“; angesetzt: Nenninhalt des Punkts 400 l",
                        wahl.TextContent);

        // Jeder Eintrag mit seinem Volumen in der Kultur der Oberfläche; die Vorgabe nennt, was sie auflöst.
        IElement feld = Feld(cut, LABEL_FUELLSTAND, "select");
        Assert.Same(wahl, feld.Closest(".epos-zapfausl-fuellstandwahl"));
        Assert.Equal(new[]
        {
            "Vorgabe: Nenninhalt des Punkts · 400 l", "Nenninhalt des Punkts · 400 l", "Punkt · 370 l",
            "Nenninhalt des Bands · 500 l", "Obergrenze des Bands · 1.540 l"
        }, feld.QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray());
        Assert.DoesNotContain(feld.QuerySelectorAll("option"), o => o.HasAttribute("disabled"));
        Assert.Equal("0", feld.QuerySelectorAll("option").Single(o => o.HasAttribute("selected")).GetAttribute("value"));

        // Die Kachel nennt denselben Bezug.
        Assert.Contains("(Nenninhalt des Punkts 400 l)", cut.Find(".epos-zapfausl-kacheln").TextContent);
    }

    [Fact]
    public void Ein_Bezug_ohne_Volumen_steht_benannt_gesperrt_auch_fuer_den_Assistenten()
    {
        KiMaskenbruecke.Leeren();
        ZapfprofilAuslegungsgruppeDaten g = MitFuellstandslinie(ZapfprofilFuellstandbezug.NenninhaltBand, "Nenninhalt des Bands", 500, 25);
        Fuellstandwahl(g.Vergleich!, ZapfprofilFuellstandbezug.NenninhaltBand,
                       (null, GRUND_OHNE_PUNKT), (null, GRUND_OHNE_PUNKT), (500, ""), (1540, ""));
        var gerechnet = new List<ZapfprofilAuslegungEingabeDaten>();
        ZapfprofilAuslegungStartDaten start = MitWertemengen(Ergebnis(g));
        var cut = Aufbauen(start, rechnen: e => { gerechnet.Add(e); return start.Ergebnis!; });

        IElement feld = Feld(cut, LABEL_FUELLSTAND, "select");
        IElement[] optionen = feld.QuerySelectorAll("option").ToArray();
        Assert.Equal("Vorgabe: Nenninhalt des Bands · 500 l", optionen[0].TextContent.Trim());
        Assert.Equal("Nenninhalt des Punkts · nicht bestimmbar", optionen[1].TextContent.Trim());
        Assert.Equal("Punkt · nicht bestimmbar", optionen[2].TextContent.Trim());
        foreach (IElement o in optionen[1..3])
        {
            Assert.True(o.HasAttribute("disabled"));
            Assert.Equal(GRUND_OHNE_PUNKT, o.GetAttribute("title"));
        }
        Assert.False(optionen[3].HasAttribute("disabled"));
        Assert.False(optionen[4].HasAttribute("disabled"));

        // Der Grund steht auch sichtbar unter dem Feld — ein title allein erreicht kein Touchgerät.
        string wahl = Fuellstandwahl(cut).TextContent;
        Assert.Contains("Nenninhalt des Punkts nicht bestimmbar: " + GRUND_OHNE_PUNKT, wahl);
        Assert.Contains("Punkt nicht bestimmbar: " + GRUND_OHNE_PUNKT, wahl);

        // Ein gesperrter Eintrag wird nicht übernommen und rechnet nicht neu.
        int vorher = gerechnet.Count;
        feld.Change("2");
        Assert.Equal(ZapfprofilFuellstandbezug.Vorgabe, cut.Instance.Eingabe.FuellstandBezug);
        Assert.Equal(vorher, gerechnet.Count);

        // Der Assistent sieht dieselben Einträge und bekommt denselben Grund.
        KiFeldzugang z = Zugang("fuellstand_bezug");
        Assert.Contains(z.Wahleintraege(), e => e.Text == "Punkt · nicht bestimmbar");
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => z.Setzen(2));
        Assert.Contains(GRUND_OHNE_PUNKT, ex.Message);
        Assert.Equal(ZapfprofilFuellstandbezug.Vorgabe, cut.Instance.Eingabe.FuellstandBezug);
        Setze("fuellstand_bezug", "Obergrenze des Bands");
        Assert.Equal(ZapfprofilFuellstandbezug.BandMax, cut.Instance.Eingabe.FuellstandBezug);
    }

    [Fact]
    public void Ein_Wechsel_der_Speichergroesse_zeichnet_Bild_Kachel_und_Herleitung_neu()
    {
        ZapfprofilAuslegungDaten Rechne(ZapfprofilAuslegungEingabeDaten e)
            => e.FuellstandBezug == ZapfprofilFuellstandbezug.BandMax
                ? Ergebnis(MitFuellstandslinie(ZapfprofilFuellstandbezug.BandMax, "Obergrenze des Bands", 1540, 77))
                : Ergebnis(MitFuellstandslinie(ZapfprofilFuellstandbezug.NenninhaltPunkt, "Nenninhalt des Punkts", 400, 20));
        var gerechnet = new List<ZapfprofilAuslegungEingabeDaten>();
        var cut = Aufbauen(MitWertemengen(Rechne(new ZapfprofilAuslegungEingabeDaten())),
                           rechnen: e => { gerechnet.Add(e.Kopie()); return Rechne(e); });

        string bildVorher = cut.Find(".epos-zapfausl-vergleich .epos-diagramm-svg").InnerHtml;
        Assert.Contains("(Nenninhalt des Punkts 400 l)", cut.Find(".epos-zapfausl-kacheln").TextContent);

        Feld(cut, LABEL_FUELLSTAND, "select").Change("4");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ZapfprofilFuellstandbezug.BandMax, cut.Instance.Eingabe.FuellstandBezug);
            Assert.Equal(ZapfprofilFuellstandbezug.BandMax, gerechnet.Last().FuellstandBezug);
            Assert.Contains("(Obergrenze des Bands 1.540 l)", cut.Find(".epos-zapfausl-kacheln").TextContent);
            Assert.Contains("angesetzt: Obergrenze des Bands 1.540 l", Fuellstandwahl(cut).TextContent);
            string bild = cut.Find(".epos-zapfausl-vergleich .epos-diagramm-svg").InnerHtml;
            Assert.NotEqual(bildVorher, bild);
            Assert.Contains("1.540", bild);
            Assert.Equal("4", Feld(cut, LABEL_FUELLSTAND, "select").QuerySelectorAll("option")
                              .Single(o => o.HasAttribute("selected")).GetAttribute("value"));
        }, Frist);
    }

    [Fact]
    public void Bei_mehreren_Speichergruppen_steht_die_Wahl_am_ersten_Bild_und_die_uebrigen_nennen_den_Bezug()
    {
        ZapfprofilAuslegungsgruppeDaten erste = MitFuellstandslinie(ZapfprofilFuellstandbezug.NenninhaltPunkt, "Nenninhalt des Punkts", 400, 20);
        ZapfprofilAuslegungsgruppeDaten zweite = MitFuellstandslinie(ZapfprofilFuellstandbezug.NenninhaltPunkt, "Nenninhalt des Punkts", 800, 40);
        zweite.Zonen = new List<string> { "Zone 3" };
        ZapfprofilAuslegungStartDaten start = MitWertemengen(Ergebnis(erste, zweite));
        var cut = Aufbauen(start, rechnen: _ => start.Ergebnis!);

        IReadOnlyList<IElement> vergleiche = cut.FindAll(".epos-zapfausl-vergleich");
        Assert.Equal(2, vergleiche.Count);
        Assert.Single(cut.FindAll("label .epos-feld-text"), t => t.TextContent.Trim() == LABEL_FUELLSTAND);
        Assert.NotNull(vergleiche[0].QuerySelector(".epos-zapfausl-fuellstandwahl select"));
        Assert.Null(vergleiche[1].QuerySelector("select"));
        Assert.Contains("Speichergröße der Füllstandslinie: Nenninhalt des Punkts 800 l — gewählt am ersten Wochenbild",
                        vergleiche[1].TextContent);
        // Die Liter der Wahl sind die des ersten Bilds.
        Assert.Equal("Vorgabe: Nenninhalt des Punkts · 400 l",
                     Feld(cut, LABEL_FUELLSTAND, "select").QuerySelectorAll("option")[0].TextContent.Trim());
    }

    [Fact]
    public void Ohne_Verfahrensvergleich_steht_keine_Wahl_der_Speichergroesse()
    {
        ZapfprofilAuslegungsgruppeDaten g = Speichergruppe();
        g.Vergleich = null;
        ZapfprofilAuslegungStartDaten start = MitWertemengen(Ergebnis(g));
        var cut = Aufbauen(start, rechnen: _ => start.Ergebnis!);

        Assert.Empty(cut.FindAll(".epos-zapfausl-fuellstandwahl"));
        Assert.DoesNotContain(cut.FindAll("label .epos-feld-text"), t => t.TextContent.Trim() == LABEL_FUELLSTAND);
    }
}
