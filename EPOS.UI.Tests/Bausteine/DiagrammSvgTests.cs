using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components.Web;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="DiagrammSvg"/> — das Diagramm als SVG im Baum
/// (Konzept Diagramme, Etappe E2, Option B).
///
/// <para><b>Was hier bewiesen wird.</b> Der Baustein bekommt ein
/// <c>Zeichenmodell</c> und zeichnet den Baum aus <c>SvgSchreiber.Baum</c> als
/// Razor-Elemente — mit vier Zutaten, die der Kern nicht liefern kann: die
/// Legende als Bedienfläche (DG-E2-1), die Zeigerzeile, die nachgezeichnete
/// Achsenteilung des Fensters (DG-E2-3) und den Farbwähler am Bild.</para>
///
/// <para><b>Was hier NICHT bewiesen wird:</b> der Zoom selbst. Er ist eine
/// Attributänderung an der <c>viewBox</c>, die das JS-Modul vornimmt — bunit hat
/// kein Layout und kein JavaScript. Geprüft wird stattdessen, was die Komponente
/// aus den gemeldeten Werten macht; die Geste misst der Prüfstand im Wirt
/// (<c>/diagrammsvg</c>) und die Abnahme am Gerät.</para>
///
/// <para>Die Klasse pinnt die Sprache (Regel seit W8): Sie prüft Zahlen mit
/// Dezimalkomma und Tausenderpunkt.</para>
/// </summary>
public class DiagrammSvgTests : EposBunitContext
{
    private const string MODUL = "./_content/EPOS.UI/epos-diagramm.js";

    /// <summary>Die Reihe MIT Farbrolle — ihr Legendeneintrag trägt einen Wähler.</summary>
    private const string MIT_ROLLE = "Temperatur";

    /// <summary>
    /// Die Reihe mit einer Farbe OHNE Rolle (<c>#123456</c> ist keine Hausfarbe,
    /// also <c>Farbton.Wert</c> mit der Rolle <c>UNBENANNT</c>) — sie ist seit
    /// DG-E5 der einzige Fall ohne Wähler.
    /// </summary>
    private const string OHNE_ROLLE = "Sonstiges";

    public DiagrammSvgTests()
    {
        // Der Baustein laedt sein Modul dynamisch. In Loose-Mode beantwortet
        // bunit den import mit dem Standardwert, und der Baustein faellt auf
        // „kein Modul" zurueck - genau der Zustand einer WebView ohne Skript.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    //  Das Modell der Fälle
    // =====================================================================

    /// <summary>
    /// Zwei Reihen mit je 8 760 Stundenwerten — dasselbe Bild, das die
    /// Klimadaten-Hülle baut. Die Pfadregel lässt beide Reihen damit ROH in den
    /// Pfad gehen (bis 8 760 Stützstellen und drei Reihen).
    /// </summary>
    private static Zeichenmodell Modell() => Jahresgang(OHNE_ROLLE);

    /// <summary>
    /// Dasselbe Bild mit frei wählbarem Namen der zweiten Reihe. Ein anderer Name
    /// macht daraus ein ANDERES Bild (DG-E3-15) — sonst ist es dasselbe, auch als
    /// neue Instanz.
    /// </summary>
    private static Zeichenmodell Jahresgang(string zweite)
    {
        var temperatur = new double[8760];
        var winkel = new double[8760];
        for (int i = 0; i < 8760; i++)
        {
            temperatur[i] = 10.0 - 12.0 * Math.Cos(2 * Math.PI * i / 8760.0);
            winkel[i] = 30.0 + 20.0 * Math.Sin(2 * Math.PI * i / 8760.0);
        }

        return ChartRenderer.JahresgangModell(
            "Jahrestemperatur Verlauf",
            new[]
            {
                new ChartRenderer.Reihe(MIT_ROLLE, temperatur, Farbrolle.AUSSENTEMPERATUR),
                new ChartRenderer.Reihe(zweite, winkel, new SkiaSharp.SKColor(0x12, 0x34, 0x56))
            },
            "Stunde des Jahres", "Temperatur [°C]");
    }

    /// <summary>Dasselbe Bild OHNE Reihen — der Leerhinweis, ohne Zeichenfläche.</summary>
    private static Zeichenmodell LeeresModell()
        => ChartRenderer.JahresgangModell("Jahrestemperatur Verlauf",
                                          Array.Empty<ChartRenderer.Reihe>(),
                                          "Stunde des Jahres", "Temperatur [°C]");

    private IRenderedComponent<DiagrammSvg> Zeige(
        Zeichenmodell? modell = null,
        Farbpalette? palette = null,
        string kennung = "probe",
        bool farbwahl = false,
        Action<(Farbrolle Rolle, Farbe Farbe)>? farbeGewaehlt = null,
        Action<Farbrolle>? farbeZurueckgesetzt = null,
        Action<ChartRenderer.Achsenfenster?>? fensterGeaendert = null,
        string einheit = "")
    {
        return Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, modell ?? Modell());
            p.Add(x => x.Kennung, kennung);
            p.Add(x => x.Bezeichnung, "Jahresgang");
            p.Add(x => x.Einheit, einheit);
            if (palette is not null) p.Add(x => x.Palette, palette);
            if (farbwahl || farbeGewaehlt is not null) p.Add(x => x.FarbwahlErlaubt, true);
            p.Add(x => x.FarbeGewaehlt, farbeGewaehlt ?? (_ => { }));
            if (farbeZurueckgesetzt is not null)
                p.Add(x => x.FarbeZurueckgesetzt, farbeZurueckgesetzt);
            if (fensterGeaendert is not null)
                p.Add(x => x.FensterGeaendert, fensterGeaendert);
        });
    }

    // =====================================================================
    //  DS-1  Der Baum
    // =====================================================================

    /// <summary>
    /// Der Baum steht als ELEMENTE da, nicht als Markup-Klumpen: äußeres
    /// <c>&lt;svg&gt;</c>, darin die Datenfläche mit ihrer <c>viewBox</c> in
    /// Datenkoordinaten und je Reihe ein Pfad mit ihrem Namen.
    /// </summary>
    [Fact]
    public void DS1_Der_Baum_traegt_Flaeche_und_je_Reihe_einen_Pfad()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-diagramm-svg"));
        Assert.Single(cut.FindAll(".epos-diagramm-svg-flaeche"));

        var flaeche = cut.Find("svg.epos-flaeche");
        Assert.Equal("none", flaeche.GetAttribute("preserveAspectRatio"));
        Assert.Equal("0 0 8759 " + flaeche.GetAttribute("viewBox")!.Split(' ')[3],
                     flaeche.GetAttribute("viewBox"));

        // DIE VOLLEN GRENZEN als eigenes Attribut - daraus liest das JS-Modul,
        // ohne eine Kopie zu fuehren, die beim Modellwechsel veraltet.
        Assert.Equal(flaeche.GetAttribute("viewBox"), flaeche.GetAttribute("data-voll"));

        Assert.Equal(2, cut.FindAll("path.epos-reihe").Count);
        Assert.Single(cut.FindAll("path[data-reihe='" + MIT_ROLLE + "']"));
        Assert.Single(cut.FindAll("path[data-reihe='" + OHNE_ROLLE + "']"));
    }

    /// <summary>
    /// Text bleibt Text — Titel und Legendeneinträge stehen als
    /// <c>&lt;text&gt;</c> im Baum, nicht in einem Bild. Genau das ist der
    /// Gewinn gegenüber dem PNG.
    /// </summary>
    [Fact]
    public void DS1_Titel_und_Legende_stehen_als_Text_im_Baum()
    {
        var cut = Zeige();

        Assert.Contains(cut.FindAll("text[data-marke='titel']"),
                        t => t.TextContent.Contains("Jahrestemperatur"));
        Assert.Equal(MIT_ROLLE, cut.Find("text[data-legende='" + MIT_ROLLE + "']").TextContent);
        Assert.Equal(OHNE_ROLLE, cut.Find("text[data-legende='" + OHNE_ROLLE + "']").TextContent);
    }

    /// <summary>
    /// Die <c>clipPath</c>-Kennungen tragen die KENNUNG der Instanz. Stehen zwei
    /// Bilder auf einer Seite, schnitte sonst das eine am Rechteck des anderen.
    /// </summary>
    [Fact]
    public void DS1_Die_clipPath_Kennungen_tragen_die_Kennung()
    {
        // Der Jahresgang fuehrt keine zugeschnittene Gruppe; fuer diesen Fall
        // traegt das Modell eine - alles andere waere ein Beweis am falschen Bild.
        var modell = new Zeichenmodell(200, 100, Farbton.Aus(Farbrolle.HINTERGRUND));
        modell.Gruppe(new Rahmen(10, 10, 100, 50),
                      zg => zg.Linie(0, 0, 200, 100, new Stift(Farbton.Aus(Farbrolle.ACHSE), 1f)));

        var cut = Zeige(modell: modell, kennung: "klima-temperatur");

        Assert.Equal("klima-temperatur-c1", cut.Find("clipPath").GetAttribute("id"));
        Assert.Equal("url(#klima-temperatur-c1)", cut.Find("g").GetAttribute("clip-path"));
    }

    /// <summary>
    /// <b>Eine Kennung, die ein Wirt aus einem NAMEN bildet, wird gesäubert.</b>
    /// Der Ganglinienbaustein unterscheidet seine Bilder über
    /// <c>„K|4711|Werk Nord"</c>; daraus würde ohne Regel
    /// <c>clip-path="url(#ganglinie-K|4711|Werk Nord-c1)"</c> — ein Verweis mit
    /// Leerzeichen und Strich, den kein Browser auflöst, und das Bild verlöre
    /// seinen Zuschnitt.
    ///
    /// <para>Der PARAMETER bleibt dabei, was der Wirt gesetzt hat.</para>
    /// </summary>
    [Fact]
    public void DS1_Eine_Kennung_mit_Sonderzeichen_wird_gesaeubert()
    {
        var modell = new Zeichenmodell(200, 100, Farbton.Aus(Farbrolle.HINTERGRUND));
        modell.Gruppe(new Rahmen(10, 10, 100, 50),
                      zg => zg.Linie(0, 0, 200, 100, new Stift(Farbton.Aus(Farbrolle.ACHSE), 1f)));

        var cut = Zeige(modell: modell, kennung: "ganglinie-K|4711|Werk Nord");

        Assert.Equal("ganglinie-K-4711-Werk-Nord-c1", cut.Find("clipPath").GetAttribute("id"));
        Assert.Equal("url(#ganglinie-K-4711-Werk-Nord-c1)",
                     cut.Find("g").GetAttribute("clip-path"));

        // Der Name, den der Wirt vergeben hat, bleibt unangetastet.
        Assert.Equal("ganglinie-K|4711|Werk Nord", cut.Instance.Kennung);
    }

    /// <summary>
    /// <b>Die Palette wirkt beim Zeichnen.</b> Dasselbe Modell, eine andere
    /// Palette, ein anderer Strich — die Modelle sind palettenfrei, sie tragen
    /// Rollen.
    /// </summary>
    [Fact]
    public void DS1_Eine_getauschte_Palette_faerbt_die_Reihe_um()
    {
        var modell = Modell();

        string hausfarbe = Zeige(modell: modell)
            .Find("path[data-reihe='" + MIT_ROLLE + "']").GetAttribute("stroke")!;

        var getauscht = new Farbpalette(
            new Dictionary<Farbrolle, Farbe> { { Farbrolle.AUSSENTEMPERATUR, new Farbe(0xFF, 0, 0, 90) } },
            Farbpalette.Vorgabe);

        string neu = Zeige(modell: modell, palette: getauscht, kennung: "zweite")
            .Find("path[data-reihe='" + MIT_ROLLE + "']").GetAttribute("stroke")!;

        Assert.Equal("#FF0000", neu);
        Assert.NotEqual(hausfarbe, neu);
    }

    // =====================================================================
    //  DS-2  Die Legende schaltet die Reihe (DG-E2-1)
    // =====================================================================

    /// <summary>
    /// Ein Klick auf den TEXT blendet die Reihe aus — der Pfad bekommt
    /// <c>display="none"</c>, der Eintrag wird gedämpft. Ein zweiter Klick holt
    /// sie zurück.
    /// </summary>
    [Fact]
    public void DS2_Der_Legendentext_blendet_die_Reihe_aus_und_wieder_ein()
    {
        var cut = Zeige();

        Assert.False(cut.Find("path[data-reihe='" + MIT_ROLLE + "']").HasAttribute("display"));

        cut.Find("text[data-legende='" + MIT_ROLLE + "']").Click();

        Assert.Equal("none",
            cut.Find("path[data-reihe='" + MIT_ROLLE + "']").GetAttribute("display"));
        Assert.Contains("epos-legende--aus",
            cut.Find("text[data-legende='" + MIT_ROLLE + "']").ClassName);
        Assert.True(cut.Instance.IstAus(MIT_ROLLE));

        // Die ANDERE Reihe bleibt, wo sie war.
        Assert.False(cut.Find("path[data-reihe='" + OHNE_ROLLE + "']").HasAttribute("display"));

        cut.Find("text[data-legende='" + MIT_ROLLE + "']").Click();

        Assert.False(cut.Find("path[data-reihe='" + MIT_ROLLE + "']").HasAttribute("display"));
        Assert.False(cut.Instance.IstAus(MIT_ROLLE));
    }

    /// <summary>
    /// Der Eintrag ist ein Bedienelement, also auch mit der Tastatur erreichbar:
    /// <c>role="button"</c>, <c>tabindex="0"</c>, Eingabe und Leertaste schalten.
    /// </summary>
    [Fact]
    public void DS2_Der_Eintrag_ist_fokussierbar_und_schaltet_mit_der_Tastatur()
    {
        var cut = Zeige();
        var eintrag = cut.Find("text[data-legende='" + MIT_ROLLE + "']");

        Assert.Equal("button", eintrag.GetAttribute("role"));
        Assert.Equal("0", eintrag.GetAttribute("tabindex"));

        eintrag.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.True(cut.Instance.IstAus(MIT_ROLLE));

        cut.Find("text[data-legende='" + MIT_ROLLE + "']")
           .KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.False(cut.Instance.IstAus(MIT_ROLLE));
    }

    /// <summary>
    /// Ohne <c>LegendeSchaltbar</c> ist die Legende nur noch Beschriftung — kein
    /// <c>role</c>, keine Klasse, kein Klick.
    /// </summary>
    [Fact]
    public void DS2_Ohne_LegendeSchaltbar_bleibt_die_Legende_Beschriftung()
    {
        var cut = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Modell())
            .Add(x => x.Kennung, "starr")
            .Add(x => x.LegendeSchaltbar, false));

        Assert.Empty(cut.FindAll(".epos-legende-eintrag"));
        Assert.False(cut.Find("text[data-marke='legende:" + MIT_ROLLE + "']").HasAttribute("role"));
    }

    // =====================================================================
    //  DS-3  Der Farbwähler am Bild (DG-E2-1, Farbrollen Teil 2)
    // =====================================================================

    /// <summary>
    /// Ein Klick auf das FARBFELD öffnet den Wähler; die gewählte Farbe geht
    /// samt Rolle nach oben, und der Wähler schließt sich.
    /// </summary>
    [Fact]
    public void DS3_Das_Farbfeld_oeffnet_den_Waehler_und_meldet_Rolle_und_Farbe()
    {
        var wahl = new List<(Farbrolle Rolle, Farbe Farbe)>();
        var cut = Zeige(farbeGewaehlt: w => wahl.Add(w));

        Assert.Empty(cut.FindAll(".epos-farbwahl"));

        cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']")[0].Click();

        Assert.Single(cut.FindAll(".epos-farbwahl"));
        Assert.Single(cut.FindAll(".epos-farbwahl-schliessflaeche"));

        cut.Find(".epos-farbwahl input.epos-farbfeld-waehler").Change("#123456");

        Assert.Single(wahl);
        Assert.Equal(Farbrolle.AUSSENTEMPERATUR, wahl[0].Rolle);
        Assert.Equal(0x12, wahl[0].Farbe.R);
        Assert.Equal(0x34, wahl[0].Farbe.G);
        Assert.Equal(0x56, wahl[0].Farbe.B);

        // Die DECKUNG bleibt die der Hausfarbe - gewaehlt wird der Farbton.
        Assert.Equal(Farbpalette.Vorgabe[Farbrolle.AUSSENTEMPERATUR].A, wahl[0].Farbe.A);
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>„Hausfarbe" meldet die Rolle zurück und schließt den Wähler.</summary>
    [Fact]
    public void DS3_Hausfarbe_meldet_die_Rolle_zurueck()
    {
        var rollen = new List<Farbrolle>();
        var cut = Zeige(farbwahl: true, farbeZurueckgesetzt: r => rollen.Add(r));

        cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']")[0].Click();
        cut.Find(".epos-farbwahl button.epos-knopf").Click();

        Assert.Equal(new[] { Farbrolle.AUSSENTEMPERATUR }, rollen);
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// <b>Eine Reihe mit einer Farbe OHNE Rolle bekommt keinen Wähler.</b> Es gibt
    /// keine Rolle, auf die die Einstellung zeigen könnte — ein Wähler wäre dort
    /// ein Versprechen ohne Wirkung. <b>Eine GERECHNETE Farbe mit Herkunftsrolle
    /// fällt seit DG-E5 nicht mehr darunter</b> (Abschnitt DS-13).
    /// </summary>
    [Fact]
    public void DS3_Eine_Reihe_ohne_Rolle_bekommt_keinen_Farbwaehler()
    {
        var cut = Zeige(farbwahl: true);

        Assert.NotEmpty(cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']"));
        Assert.Empty(cut.FindAll("rect[data-legende='" + OHNE_ROLLE + "']"));

        // Umschalt+Eingabe auf diesem Eintrag oeffnet deshalb auch nichts.
        cut.Find("text[data-legende='" + OHNE_ROLLE + "']")
           .KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>Umschalt+Eingabe öffnet den Wähler — der Tastaturweg zum Farbfeld.</summary>
    [Fact]
    public void DS3_Umschalt_und_Eingabe_oeffnet_den_Waehler()
    {
        var cut = Zeige(farbwahl: true);

        cut.Find("text[data-legende='" + MIT_ROLLE + "']")
           .KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        Assert.Single(cut.FindAll(".epos-farbwahl"));
        Assert.False(cut.Instance.IstAus(MIT_ROLLE));   // geschaltet wird dabei NICHT
    }

    /// <summary>Esc und der Klick daneben schließen den Wähler.</summary>
    [Fact]
    public void DS3_Esc_und_der_Klick_daneben_schliessen_den_Waehler()
    {
        var cut = Zeige(farbwahl: true);

        cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']")[0].Click();
        cut.Find(".epos-farbwahl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".epos-farbwahl"));

        cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']")[0].Click();
        cut.Find(".epos-farbwahl-schliessflaeche").Click();
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// Ohne <c>FarbeGewaehlt</c> gibt es keinen Wähler, auch wenn ihn jemand
    /// erlaubt hat: Kein Delegat, kein Bedienelement.
    /// </summary>
    [Fact]
    public void DS3_Ohne_Ruecklauf_gibt_es_kein_Farbfeld()
    {
        var cut = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Modell())
            .Add(x => x.Kennung, "ohne")
            .Add(x => x.FarbwahlErlaubt, true));

        Assert.Empty(cut.FindAll(".epos-legende-farbfeld"));
    }

    // =====================================================================
    //  DS-4  Die Zeigerzeile
    // =====================================================================

    /// <summary>
    /// Das Modul meldet die STUNDE; die Werte liest die Komponente aus dem
    /// Modell — ein Interop je Bildaufbau, kein Kernaufruf.
    /// </summary>
    [Fact]
    public async Task DS4_Die_gemeldete_Stunde_fuellt_die_Zeigerzeile()
    {
        var modell = Modell();
        var cut = Zeige(modell: modell, einheit: "°C");

        Assert.Equal("", cut.Find(".epos-diagramm-zeigerzeile").TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(4000));

        string zeile = cut.Find(".epos-diagramm-zeigerzeile").TextContent;
        Assert.Contains("4.000 h", zeile);
        Assert.Contains(MIT_ROLLE + ": ", zeile);
        Assert.Contains(OHNE_ROLLE + ": ", zeile);
        Assert.Contains("°C", zeile);

        // Der Wert ist der der Stunde 4 000, mit Dezimalkomma.
        Assert.Contains(modell.Reihen[0].Werte[4000].ToString("0.###", CultureInfo.CurrentCulture),
                        zeile);

        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(null));
        Assert.Equal("", cut.Find(".epos-diagramm-zeigerzeile").TextContent.Trim());
    }

    /// <summary>
    /// Eine ausgeblendete Reihe steht nicht in der Zeile — sie ist ja auch nicht
    /// im Bild.
    /// </summary>
    [Fact]
    public async Task DS4_Eine_ausgeblendete_Reihe_steht_nicht_in_der_Zeigerzeile()
    {
        var cut = Zeige();
        cut.Find("text[data-legende='" + OHNE_ROLLE + "']").Click();

        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(4000));

        string zeile = cut.Find(".epos-diagramm-zeigerzeile").TextContent;
        Assert.Contains(MIT_ROLLE + ": ", zeile);
        Assert.DoesNotContain(OHNE_ROLLE + ": ", zeile);
    }

    /// <summary>Die gemeldete Zoomstufe steht in der Leiste — mit Dezimalkomma.</summary>
    [Fact]
    public async Task DS4_Die_gemeldete_Stufe_erscheint_mit_Dezimalkomma()
    {
        var cut = Zeige();

        Assert.Equal("×1", cut.Find(".epos-diagramm-stufe").TextContent.Trim());

        await cut.InvokeAsync(() => cut.Instance.ZoomGemeldet(2.5));

        Assert.Equal("×2,5", cut.Find(".epos-diagramm-stufe").TextContent.Trim());
    }

    // =====================================================================
    //  DS-5  Das Fenster und die nachgezeichnete Achsenteilung (DG-E2-3)
    // =====================================================================

    /// <summary>
    /// Im Fenster gilt die Monatsteilung des Bildes nicht mehr: Die Komponente
    /// blendet die Elemente mit der Marke <c>xachse</c> aus und zeichnet die
    /// Ticks aus <c>ChartRenderer.Jahresstundenteilung</c> nach — dieselbe
    /// Regel, mit der der Renderer sie ins PNG setzt.
    /// </summary>
    [Fact]
    public async Task DS5_Ein_Fenster_blendet_die_Monatsachse_aus_und_zeichnet_Ticks()
    {
        var fenster = new List<ChartRenderer.Achsenfenster?>();
        var cut = Zeige(fensterGeaendert: f => fenster.Add(f));

        Assert.NotEmpty(cut.FindAll("[data-marke='xachse']"));
        Assert.All(cut.FindAll("[data-marke='xachse']"),
                   e => Assert.False(e.HasAttribute("display")));
        Assert.Empty(cut.FindAll(".epos-diagramm-ticks"));

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(3000, 3400));

        Assert.All(cut.FindAll("[data-marke='xachse']"),
                   e => Assert.Equal("none", e.GetAttribute("display")));

        var ticks = ChartRenderer.Jahresstundenteilung(3000, 3400);
        Assert.NotEmpty(ticks);
        Assert.Equal(ticks.Count, cut.FindAll(".epos-diagramm-ticks line").Count);

        // Die Beschriftungen sind die der Teilung, dazu der Achsentitel.
        var texte = cut.FindAll(".epos-diagramm-ticks text").Select(t => t.TextContent).ToList();
        Assert.Equal(ticks.Count + 1, texte.Count);
        foreach ((int _, string text) in ticks) Assert.Contains(text, texte);
        Assert.Contains(Resource.CHART_ACHSE_JAHRESSTUNDEN, texte);

        Assert.Equal((3000, 3400), cut.Instance.Fenster);
        Assert.Single(fenster);
        Assert.Equal(3000, fenster[0]!.Von);
        Assert.Equal(3401, fenster[0]!.Bis);   // Achsenfenster.Bis ist ausschliesslich
    }

    /// <summary>
    /// Deckt das gemeldete Fenster das ganze Bild ab, ist das die Vollansicht:
    /// Die Monatsachse steht wieder, die Ticks fallen, und der Wirt bekommt
    /// <c>null</c>.
    /// </summary>
    [Fact]
    public async Task DS5_Der_Vollbereich_stellt_die_Monatsachse_her_und_meldet_null()
    {
        var fenster = new List<ChartRenderer.Achsenfenster?>();
        var cut = Zeige(fensterGeaendert: f => fenster.Add(f));

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(3000, 3400));
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(0, 8759));

        Assert.All(cut.FindAll("[data-marke='xachse']"),
                   e => Assert.False(e.HasAttribute("display")));
        Assert.Empty(cut.FindAll(".epos-diagramm-ticks"));
        Assert.Null(cut.Instance.Fenster);

        Assert.Equal(2, fenster.Count);
        Assert.Null(fenster[1]);
    }

    /// <summary>
    /// Der Knopf „1:1" stellt die volle Ansicht her und sagt es nach oben —
    /// EIN Knopf für Zoom und Ausschnitt.
    /// </summary>
    [Fact]
    public async Task DS5_Der_Knopf_stellt_die_volle_Ansicht_her()
    {
        var fenster = new List<ChartRenderer.Achsenfenster?>();
        var cut = Zeige(fensterGeaendert: f => fenster.Add(f));

        await cut.InvokeAsync(() => cut.Instance.ZoomGemeldet(4));
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(3000, 3400));

        cut.FindAll("button.epos-diagramm-knopf").First(k => k.TextContent.Trim() == "1:1").Click();

        Assert.Equal(1.0, cut.Instance.Stufe);
        Assert.Null(cut.Instance.Fenster);
        Assert.Null(fenster[^1]);
    }

    /// <summary>Der Umschalter „Bereich" bleibt gedrückt, solange er an ist.</summary>
    [Fact]
    public void DS5_Der_Bereichsknopf_schaltet_um()
    {
        var cut = Zeige();

        Assert.Equal("false", cut.Find("button[aria-pressed]").GetAttribute("aria-pressed"));

        cut.Find("button[aria-pressed]").Click();

        Assert.True(cut.Instance.Bereichsmodus);
        Assert.Equal("true", cut.Find("button[aria-pressed]").GetAttribute("aria-pressed"));
    }

    // =====================================================================
    //  DS-6  Die Grenzfälle
    // =====================================================================

    /// <summary>
    /// Ein Bild ohne Reihen trägt nur den Leerhinweis und keine Zeichenfläche:
    /// Der Baum steht unverändert da, aber es gibt weder Leiste noch
    /// Zeigerzeile — dort wäre nichts zu bedienen und nichts abzulesen.
    /// </summary>
    [Fact]
    public void DS6_Ohne_Reihen_steht_der_Leerhinweis_ohne_Leiste()
    {
        var cut = Zeige(modell: LeeresModell());

        Assert.Single(cut.FindAll("[data-marke='leerhinweis']"));
        Assert.Empty(cut.FindAll("svg.epos-flaeche"));
        Assert.Empty(cut.FindAll(".epos-diagramm-leiste"));
        Assert.Empty(cut.FindAll(".epos-diagramm-zeigerzeile"));
    }

    /// <summary>Ohne Modell steht der Platzhalter — und sonst nichts.</summary>
    [Fact]
    public void DS6_Ohne_Modell_steht_der_Platzhalter()
    {
        var cut = Render<DiagrammSvg>(p => p
            .Add(x => x.PlatzhalterText, "Kein Diagramm vorhanden"));

        Assert.Equal("Kein Diagramm vorhanden",
                     cut.Find(".epos-chartbild-platzhalter").TextContent.Trim());
        Assert.Empty(cut.FindAll("svg"));
        Assert.Empty(cut.FindAll(".epos-diagramm-leiste"));
    }

    /// <summary>
    /// Mit <c>OhneZoom</c> fällt die Leiste, die Legende bleibt: Ein Bild ohne
    /// Zeitachse hat nichts zu zoomen, seine Reihen bleiben trotzdem schaltbar.
    /// </summary>
    [Fact]
    public void DS6_OhneZoom_faellt_die_Leiste_und_die_Legende_bleibt()
    {
        var cut = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Modell())
            .Add(x => x.Kennung, "starr")
            .Add(x => x.OhneZoom, true));

        Assert.Empty(cut.FindAll(".epos-diagramm-leiste"));
        Assert.False(cut.Find(".epos-diagramm-svg-flaeche").HasAttribute("tabindex"));

        cut.Find("text[data-legende='" + MIT_ROLLE + "']").Click();
        Assert.True(cut.Instance.IstAus(MIT_ROLLE));
    }

    /// <summary>
    /// Die Fläche ist benannt und tastaturerreichbar. <c>role="group"</c> statt
    /// <c>role="img"</c>: Die Legendeneinträge SIND Bedienelemente, und in einem
    /// Bild erreichte sie keine Sprachausgabe mehr.
    /// </summary>
    [Fact]
    public void DS6_Die_Flaeche_ist_benannt_und_tastaturerreichbar()
    {
        var cut = Zeige();
        var flaeche = cut.Find(".epos-diagramm-svg-flaeche");

        Assert.Equal("group", flaeche.GetAttribute("role"));
        Assert.Equal("Jahresgang", flaeche.GetAttribute("aria-label"));
        Assert.Equal("0", flaeche.GetAttribute("tabindex"));
    }

    // =====================================================================
    //  DS-7  Der Weg zum Modul
    // =====================================================================

    /// <summary>
    /// Beim ersten Zeichnen wird das Modul geladen und an die Fläche gehängt.
    ///
    /// <para><b>ZWEI Gaben, nicht drei</b> (DG-E3-13): Der Modus ist entfallen, weil
    /// das Modul nur noch einen kennt — den der <c>viewBox</c>. Der CSS-Transform auf
    /// einem PNG ist mit dem Baustein <c>Diagramm</c> gefallen.</para>
    /// </summary>
    [Fact]
    public void DS7_Beim_ersten_Zeichnen_wird_gebunden()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var binden = modul.SetupVoid("binden", _ => true);

        Zeige();

        Assert.Single(binden.Invocations);
        Assert.Equal(2, binden.Invocations.Single().Arguments.Count);
    }

    /// <summary>
    /// Lädt das Modul nicht, steht das Bild da — vollständig, nur ohne Zoom.
    /// Legende und Farbwahl bleiben bedienbar: Sie sind Blazor, kein JavaScript.
    /// </summary>
    [Fact]
    public void DS7_Ohne_Modul_bleibt_das_Bild_stehen()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;

        var cut = Zeige();

        Assert.Equal(2, cut.FindAll("path.epos-reihe").Count);
        Assert.Equal("×1", cut.Find(".epos-diagramm-stufe").TextContent.Trim());

        cut.Find("text[data-legende='" + MIT_ROLLE + "']").Click();
        Assert.True(cut.Instance.IstAus(MIT_ROLLE));
    }

    /// <summary>Das Abräumen löst die Handler wieder.</summary>
    [Fact]
    public async Task DS7_Das_Abraeumen_loest_die_Handler()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.SetupVoid("binden", _ => true);
        var loesen = modul.SetupVoid("loesen", _ => true);

        var cut = Zeige();
        await cut.Instance.DisposeAsync();

        Assert.Single(loesen.Invocations);
    }

    /// <summary>
    /// <b>Befund 26.09.2026 (Gebäudedialog):</b> Steht zwischendurch der Platzhalter
    /// (Modell <c>null</c> — eine unbeheizte Zone), fällt die Fläche aus dem DOM. Die
    /// nächste Fläche ist ein NEUES Element; ohne erneutes <c>binden</c> trägt sie keinen
    /// Handler, Ziehen markiert Text und nichts zoomt. Dasselbe Modell kommt zurück —
    /// gebunden wird trotzdem, und der Zustand beginnt bei 1:1.
    /// </summary>
    [Fact]
    public void DS7_Nach_dem_Platzhalter_wird_die_neue_Flaeche_gebunden()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var binden = modul.SetupVoid("binden", _ => true);
        modul.SetupVoid("zuruecksetzen", _ => true);

        Zeichenmodell bild = Modell();
        var cut = Zeige(bild);
        Assert.Single(binden.Invocations);

        cut.Render(p => p.Add(x => x.Modell, (Zeichenmodell?)null));
        Assert.NotNull(cut.Find(".epos-chartbild-platzhalter"));

        cut.Render(p => p.Add(x => x.Modell, bild));

        Assert.Equal(2, binden.Invocations.Count);
        Assert.Equal("×1", cut.Find(".epos-diagramm-stufe").TextContent.Trim());
    }

    /// <summary>Ein Zeichenlauf auf DERSELBEN Fläche bindet nicht ein zweites Mal.</summary>
    [Fact]
    public void DS7_Derselbe_Zeichenlauf_bindet_nicht_doppelt()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var binden = modul.SetupVoid("binden", _ => true);

        Zeichenmodell bild = Modell();
        var cut = Zeige(bild);
        cut.Render(p => p.Add(x => x.Modell, bild));

        Assert.Single(binden.Invocations);
    }

    // =====================================================================
    //  DS-8  Was die Etappe E3 dazugelegt hat
    // =====================================================================
    //
    //  Die Gruppe (a) bringt Bilder mit, die der Jahresgang nicht kannte: ZWEI
    //  y-Achsen (jede Reihe mit eigenem Datenfenster, DG-E3-1), FLAECHEN
    //  (DG-E3-2), GEBUENDELTE Pfade samt Nachladen ab dem Vierfachen (DG-E3-3)
    //  und eine x-Achse, die keine Jahresstunde zaehlt.

    /// <summary>Wie viele Stützstellen die Stapelfälle führen.</summary>
    private const int STAPEL_N = 3000;

    /// <summary>Der Name der Reihe auf der ZWEITEN y-Achse.</summary>
    private const string RECHTS = "Speicherinhalt";

    /// <summary>Der Name der unteren Stapelfläche.</summary>
    private const string FLAECHE = "Heizwärme";

    /// <summary>
    /// Ein Erzeugerstapel: zwei gestapelte FLÄCHEN, eine Linie darüber und eine
    /// Reihe auf der ZWEITEN Achse — vier Reihen also, und damit GEBÜNDELT
    /// (<c>Pfadregel.Roh</c> gilt bis drei Reihen).
    ///
    /// <para>Die zweite Achse führt Werte um 800; die linke bleibt weit darunter.
    /// Nur so unterscheiden sich die beiden y-Fenster, und nur daran erkennt der
    /// Baustein, welche Reihe rechts steht.</para>
    /// </summary>
    /// <param name="sortiert">
    /// Dauerlinie statt Ganglinie — dann zählt x den RANG, und die Zeichenfläche sagt
    /// <c>Achsenart.Index</c> statt <c>Stunden</c> (DG-E3-11).
    /// </param>
    private static Zeichenmodell Stapelmodell(bool sortiert = false)
    {
        var heizung = new double[STAPEL_N];
        var wasser = new double[STAPEL_N];
        var bedarf = new double[STAPEL_N];
        var speicher = new double[STAPEL_N];
        for (int i = 0; i < STAPEL_N; i++)
        {
            heizung[i] = 20.0 + 15.0 * Math.Sin(2 * Math.PI * i / 96.0);
            wasser[i] = 5.0 + 3.0 * Math.Cos(2 * Math.PI * i / 96.0);
            bedarf[i] = heizung[i] + wasser[i] + 4.0;
            speicher[i] = 800.0 + 300.0 * Math.Sin(2 * Math.PI * i / 96.0 - 1.1);
        }

        return ChartRenderer.ErzeugerStapelModell(
            "Erzeugerstapel",
            new[]
            {
                new ChartRenderer.Reihe(FLAECHE, heizung, ChartRenderer.C_WP,
                                        ChartRenderer.Stapelart.Flaeche),
                new ChartRenderer.Reihe("Warmwasser", wasser, ChartRenderer.C_PV,
                                        ChartRenderer.Stapelart.Flaeche)
            },
            new[] { new ChartRenderer.Reihe("Wärmebedarf", bedarf, ChartRenderer.C_BEDARF) },
            null, "Leistung [kW]", ChartRenderer.Achse.Monate, sortiert,
            new[] { new ChartRenderer.Reihe(RECHTS, speicher, ChartRenderer.C_NETZ) },
            "Speicherinhalt [kWh]");
    }

    /// <summary>Ein Stundenprofil über 168 Wochenstunden — eine Fläche mit Randlinie.</summary>
    private static Zeichenmodell Profilmodell()
    {
        var werte = new double[168];
        for (int i = 0; i < 168; i++) werte[i] = 10.0 + i;
        return ChartRenderer.StundenprofilModell("Stundenprofil", werte, 24,
                                                 "Wochenstunde (1..168)", "Verteilung");
    }

    private IRenderedComponent<DiagrammSvg> ZeigeStapel()
        => Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, Stapelmodell());
            p.Add(x => x.Kennung, "stapel");
            p.Add(x => x.Einheit, "kW");
            p.Add(x => x.EinheitRechts, "kWh");
        });

    // ---- Die Zeigerzeile liest das eigene Fenster jeder Reihe ------------

    /// <summary>
    /// <b>DG-E3-1 in der Zeigerzeile.</b> Das Stundenprofil zählt 1 … n: Seine
    /// Zeichenfläche geht von 0 bis n, die REIHE aber von 1 bis n — Wert
    /// <c>i</c> steht am rechten Rand seines Fachs. Der Index ist deshalb
    /// x − 1 und nicht x − <c>Flaeche.Daten.XVon</c>; genau das war der Grund,
    /// aus dem der Kernteil diesen Punkt offen gelassen hat.
    /// </summary>
    [Fact]
    public async Task DS8_Die_Zeigerzeile_liest_das_eigene_Fenster_der_Reihe()
    {
        Zeichenmodell modell = Profilmodell();
        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, modell);
            p.Add(x => x.Kennung, "profil");
        });

        // Die Stelle 1 meint den ERSTEN Wert der Reihe.
        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(1));
        Assert.Contains(modell.Reihen[0].Werte[0].ToString("0.###", CultureInfo.CurrentCulture),
                        cut.Find(".epos-diagramm-zeigerzeile").TextContent);

        // Und die Stelle 168 den letzten.
        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(168));
        Assert.Contains(modell.Reihen[0].Werte[167].ToString("0.###", CultureInfo.CurrentCulture),
                        cut.Find(".epos-diagramm-zeigerzeile").TextContent);

        // Die Stelle 0 liegt VOR dem Fenster der Reihe: Dort zeichnet sie nichts,
        // und dann steht sie auch nicht in der Zeile.
        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(0));
        Assert.DoesNotContain(":", cut.Find(".epos-diagramm-zeigerzeile").TextContent);
    }

    /// <summary>
    /// Eine Reihe der ZWEITEN Achse trägt ihre eigene Einheit. Ohne das stünde
    /// in der Zeile der richtige Wert mit der falschen Einheit — kWh neben kW.
    /// </summary>
    [Fact]
    public async Task DS8_Die_zweite_Achse_traegt_EinheitRechts()
    {
        var cut = ZeigeStapel();

        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(500));

        string zeile = cut.Find(".epos-diagramm-zeigerzeile").TextContent;
        Assert.Contains(FLAECHE + ": ", zeile);
        Assert.Contains("kW", zeile);
        Assert.Contains("kWh", zeile);
        Assert.Contains(RECHTS + ": ", zeile);

        // DIE EINHEIT DER STELLE KOMMT AUS DEM MODELL (DG-E3-11): Der Erzeugerstapel
        // zaehlt auf x Stuetzstellen einer ZEITREIHE, seine Zeichenflaeche sagt
        // Achsenart.Stunden - und damit steht „h" hinter der Zahl. Bis zum Abschluss
        // der Etappe E3 sagte das ein Parameter der Aufrufstelle; zwei Stellen, die
        // dasselbe behaupten, gehen irgendwann auseinander.
        Assert.Contains("500 h", zeile);
    }

    /// <summary>
    /// <b>DG-E3-11 an der anderen Achsenart:</b> In der DAUERLINIE zählt x den RANG.
    /// Der Renderer sagt dort <c>Achsenart.Index</c>, und die Zeigerzeile schreibt
    /// keine Einheit hinter die Zahl — ein Rang ist keine Stunde.
    /// </summary>
    [Fact]
    public async Task DS8_In_der_Dauerlinie_traegt_die_Stelle_keine_Einheit()
    {
        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, Stapelmodell(sortiert: true));
            p.Add(x => x.Kennung, "dauerlinie");
            p.Add(x => x.Einheit, "kW");
        });

        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(500));

        string zeile = cut.Find(".epos-diagramm-zeigerzeile").TextContent;
        Assert.Contains("500", zeile);
        Assert.DoesNotContain("500 h", zeile);
    }

    // ---- Die zweite Achse fällt mit ihren Reihen -------------------------

    /// <summary>
    /// <b>Die Marke <c>yachse2</c> fällt mit ihrer Reihe.</b> Rasterlinie,
    /// Beschriftung und Titel der rechten Achse beschriften nichts mehr, sobald
    /// jede ihrer Reihen über die Legende abgewählt ist.
    /// </summary>
    [Fact]
    public void DS8_Die_zweite_Achse_faellt_mit_ihrer_Reihe()
    {
        var cut = ZeigeStapel();

        Assert.NotEmpty(cut.FindAll("[data-marke='yachse2']"));
        Assert.All(cut.FindAll("[data-marke='yachse2']"),
                   e => Assert.False(e.HasAttribute("display")));
        Assert.False(cut.Instance.ZweiteAchseAus);

        cut.Find("text[data-legende='" + RECHTS + "']").Click();

        Assert.True(cut.Instance.ZweiteAchseAus);
        Assert.All(cut.FindAll("[data-marke='yachse2']"),
                   e => Assert.Equal("none", e.GetAttribute("display")));

        // Und zurueck: Der Eintrag schaltet in beide Richtungen.
        cut.Find("text[data-legende='" + RECHTS + "']").Click();
        Assert.False(cut.Instance.ZweiteAchseAus);
        Assert.All(cut.FindAll("[data-marke='yachse2']"),
                   e => Assert.False(e.HasAttribute("display")));
    }

    /// <summary>
    /// Ein Bild OHNE zweite Achse hat nichts auszublenden — <c>ZweiteAchseAus</c>
    /// bleibt falsch, auch wenn jede Reihe abgewählt ist.
    /// </summary>
    [Fact]
    public void DS8_Ohne_zweite_Achse_bleibt_der_Schalter_aus()
    {
        var cut = Zeige();

        cut.Find("text[data-legende='" + MIT_ROLLE + "']").Click();
        cut.Find("text[data-legende='" + OHNE_ROLLE + "']").Click();

        Assert.False(cut.Instance.ZweiteAchseAus);
    }

    // ---- Der Legendenklick schaltet auch eine Fläche ---------------------

    /// <summary>
    /// <b>Eine FLÄCHE schaltet wie eine Linie.</b> Der Schreiber gibt ihr
    /// denselben Griff <c>data-reihe</c>, und <c>display="none"</c> wirkt auf
    /// beide — der Baustein braucht dafür keinen Sonderweg.
    /// </summary>
    [Fact]
    public void DS8_Der_Legendenklick_schaltet_auch_eine_Flaeche()
    {
        var cut = ZeigeStapel();

        var flaeche = cut.Find("path[data-reihe='" + FLAECHE + "']");
        Assert.Equal("none", flaeche.GetAttribute("stroke"));   // eine Flaeche ohne Rand
        Assert.False(flaeche.HasAttribute("display"));

        cut.Find("text[data-legende='" + FLAECHE + "']").Click();

        Assert.True(cut.Instance.IstAus(FLAECHE));
        Assert.Equal("none",
                     cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("display"));
    }

    // ---- Nachladen ab dem Vierfachen (DG-E3-3) ---------------------------

    /// <summary>
    /// <b>Ab dem Vierfachen rechnet der Baustein den Ausschnitt ROH nach.</b>
    /// Vier Reihen heißt gebündelt (<c>Pfadregel.Roh</c> gilt bis drei); zoomt
    /// der Anwender darüber hinaus, zeigt die Bündelung nicht mehr die echte
    /// Stützstelle. Der neue Pfad kommt aus <c>SvgSchreiber.Reihenpfad</c> mit
    /// Fenster — <b>kein Kernaufruf</b>, das Modell trägt die Werte ohnehin —
    /// und ersetzt das <c>d</c> des vorhandenen Pfades.
    /// </summary>
    [Fact]
    public async Task DS8_Ab_dem_Vierfachen_wird_der_Ausschnitt_roh_nachgerechnet()
    {
        Zeichenmodell modell = Stapelmodell();
        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, modell);
            p.Add(x => x.Kennung, "stapel");
        });

        Assert.Empty(cut.Instance.Ausschnittpfade);
        string vollpfad = cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("d")!;

        // 200 von 3 000 Stuetzstellen sind das Fuenfzehnfache.
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(1000, 1200));

        Assert.Equal(modell.Reihen.Count, cut.Instance.Ausschnittpfade.Count);

        Datenreihe reihe = modell.Reihen.Single(r => r.Name == FLAECHE);
        string erwartet = SvgSchreiber.Reihenpfad(reihe, modell.Flaeche, 1000, 1200, true);

        Assert.Equal(erwartet, cut.Instance.Ausschnittpfade[FLAECHE]);
        Assert.Equal(erwartet, cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("d"));
        Assert.NotEqual(vollpfad, erwartet);
    }

    /// <summary>
    /// Unter dem Vierfachen bleibt es beim Vollpfad des Schreibers: Die
    /// Bündelung ist dort vom rohen Bild nicht zu unterscheiden, und ein
    /// zweiter Pfad wäre Arbeit ohne Wirkung.
    /// </summary>
    [Fact]
    public async Task DS8_Unter_dem_Vierfachen_steht_wieder_der_Vollpfad()
    {
        Zeichenmodell modell = Stapelmodell();
        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, modell);
            p.Add(x => x.Kennung, "stapel");
        });

        string vollpfad = cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("d")!;

        // Rund das Doppelte - darunter bleibt der Vollpfad.
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(0, 1500));
        Assert.Empty(cut.Instance.Ausschnittpfade);
        Assert.Equal(vollpfad, cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("d"));

        // Erst nachladen, dann wieder herauszoomen: Der Vollpfad kommt zurueck.
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(1000, 1200));
        Assert.NotEmpty(cut.Instance.Ausschnittpfade);

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(0, 1500));
        Assert.Empty(cut.Instance.Ausschnittpfade);
        Assert.Equal(vollpfad, cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("d"));
    }

    /// <summary>
    /// Eine ROHE Reihe wird nie nachgeladen: Ihr Vollpfad trägt schon jede
    /// Stützstelle, und der Fensterpfad wäre genau sein Ausschnitt.
    /// </summary>
    [Fact]
    public async Task DS8_Eine_rohe_Reihe_wird_nicht_nachgeladen()
    {
        var cut = Zeige();   // zwei Reihen, 8 760 Stuetzstellen - also roh

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(3000, 3400));

        Assert.Empty(cut.Instance.Ausschnittpfade);
    }

    /// <summary>Die volle Ansicht räumt den nachgerechneten Ausschnitt ab.</summary>
    [Fact]
    public async Task DS8_Der_Knopf_raeumt_den_Ausschnitt_ab()
    {
        var cut = ZeigeStapel();

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(1000, 1200));
        Assert.NotEmpty(cut.Instance.Ausschnittpfade);

        cut.FindAll("button.epos-diagramm-knopf").First(k => k.TextContent.Trim() == "1:1").Click();

        Assert.Empty(cut.Instance.Ausschnittpfade);
    }

    // ---- Die Achsenart ---------------------------------------------------

    /// <summary>
    /// Ein eigener Titel steht auch dort, wo die Achsenart keinen mitbringt —
    /// der Wirt kennt den Ressourcentext seines Bildes.
    /// </summary>
    [Fact]
    public async Task DS8_Ein_eigener_Achsentitel_steht_im_Ausschnitt()
    {
        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, Profilmodell());
            p.Add(x => x.Kennung, "profil");
            p.Add(x => x.XTitelText, "Wochenstunde");
        });

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(24, 72));

        Assert.Contains("Wochenstunde",
                        cut.FindAll(".epos-diagramm-ticks text").Select(t => t.TextContent));
    }

    // ---- Der Leerraum im Text --------------------------------------------

    /// <summary>
    /// <b>Zwei Leerzeichen bleiben zwei Leerzeichen.</b> Der Bildtitel des
    /// Kostenprofils heißt „Kostenprofil  [ct/kWh]"; das PNG setzt beide, der
    /// Browser faltet XML-Leerraum zusammen und zeigt eines. Jeder
    /// <c>&lt;text&gt;</c> des Bausteins trägt deshalb <c>xml:space</c>.
    /// </summary>
    [Fact]
    public void DS8_Jeder_Text_traegt_xml_space_preserve()
    {
        var cut = Zeige();

        var texte = cut.FindAll("text");
        Assert.NotEmpty(texte);

        // Am MARKUP geprueft und nicht ueber GetAttribute: xml:space traegt einen
        // Namensraum, und wie ein Parser ihn zurueckgibt, ist seine Sache.
        Assert.All(texte, t => Assert.Contains("xml:space=\"preserve\"", t.OuterHtml));
    }

    // =====================================================================
    //  DS-9  Der Wert am Element (DG-E3-10)
    // =====================================================================

    /// <summary>
    /// Ein Monatsstapel — ein Bild OHNE Zeichenfläche. Seine Schichten sind
    /// gewöhnliche Rechtecke mit der Marke <c>reihe:&lt;Name&gt;</c> und ihrem
    /// fertig formatierten <c>data-wert</c>.
    /// </summary>
    private static Zeichenmodell Stapelbild()
    {
        var waerme = new double[12];
        var strom = new double[12];
        for (int m = 0; m < 12; m++)
        {
            waerme[m] = 40.0 - 3.0 * m;
            strom[m] = 10.0 + 0.5 * m;
        }

        return ChartRenderer.MonatsStapelModell("Monatsstapel", "MWh", new[]
        {
            new ChartRenderer.Reihe("Wärmepumpe", waerme, ChartRenderer.C_WP),
            new ChartRenderer.Reihe("Heizkessel", strom, ChartRenderer.C_KESSEL)
        });
    }

    private IRenderedComponent<DiagrammSvg> ZeigeStapelbild()
        => Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, Stapelbild());
            p.Add(x => x.Kennung, "monatsstapel");
        });

    /// <summary>Das erste Element, das einen <c>data-wert</c> trägt.</summary>
    private static IElement ErstesWertelement(IRenderedComponent<DiagrammSvg> cut)
        => cut.FindAll("[data-wert]").First(e => e.GetAttribute("data-wert")!.Length > 0);

    /// <summary>
    /// <b>Ohne Zeichenfläche gibt es keine Bedienung</b> (DG-E3-10): weder Leiste noch
    /// JS-Bindung — dort wäre nichts zu zoomen. Die ZEIGERZEILE steht trotzdem: Sie
    /// ist der einzige Ort, an dem eine Säule ihre Zahl nennt.
    /// </summary>
    [Fact]
    public void DS9_Ein_Bild_ohne_Flaeche_traegt_keine_Leiste_aber_eine_Zeigerzeile()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        var binden = modul.SetupVoid("binden", _ => true);

        var cut = ZeigeStapelbild();

        Assert.Empty(cut.FindAll(".epos-diagramm-leiste"));
        Assert.Empty(binden.Invocations);
        Assert.Single(cut.FindAll(".epos-diagramm-zeigerzeile"));
        Assert.True(cut.Instance.ZeigtWertAmElement);
    }

    /// <summary>
    /// <b>Der Zeiger auf einem Element zeigt dessen Wert; das Verlassen löscht ihn.</b>
    /// Der Text kommt fertig formatiert aus dem Kern — die Oberfläche formatiert nicht
    /// ein zweites Mal, sonst gingen Bild und Zeigetext auseinander.
    /// </summary>
    [Fact]
    public void DS9_Der_Zeiger_auf_einer_Saeule_zeigt_ihren_Wert()
    {
        var cut = ZeigeStapelbild();
        IElement element = ErstesWertelement(cut);
        string wert = element.GetAttribute("data-wert")!;

        element.PointerEnter();
        Assert.Equal(wert, cut.Instance.WertAmZeiger);
        Assert.Equal(wert, cut.Find(".epos-diagramm-zeigerzeile").TextContent.Trim());

        // Die MAUS loescht beim Verlassen.
        ErstesWertelement(cut).PointerLeave(new PointerEventArgs { PointerType = "mouse" });
        Assert.Equal("", cut.Instance.WertAmZeiger);
    }

    /// <summary>
    /// <b>Berührung: Antippen zeigt, Antippen DANEBEN löscht</b> (DG-E3-10). Das
    /// Verlassen eines Elements darf bei Berührung NICHT löschen — es folgte
    /// unmittelbar auf das Antippen, und der Wert wäre nie zu lesen.
    /// </summary>
    [Fact]
    public void DS9_Bei_Beruehrung_loescht_erst_das_Antippen_daneben()
    {
        var cut = ZeigeStapelbild();
        IElement element = ErstesWertelement(cut);
        string wert = element.GetAttribute("data-wert")!;

        element.PointerDown(new PointerEventArgs { PointerType = "touch" });
        Assert.Equal(wert, cut.Instance.WertAmZeiger);

        // Das Verlassen bei Beruehrung laesst ihn stehen …
        ErstesWertelement(cut).PointerLeave(new PointerEventArgs { PointerType = "touch" });
        Assert.Equal(wert, cut.Instance.WertAmZeiger);

        // … und erst ein Druck NEBEN jedes markierte Element raeumt die Zeile.
        cut.Find(".epos-diagramm-svg-flaeche")
           .PointerDown(new PointerEventArgs { PointerType = "touch" });
        Assert.Equal("", cut.Instance.WertAmZeiger);
    }

    /// <summary>
    /// <b>Der Legendenschalter greift auch bei einem Pixelbild.</b> Dort gibt es keinen
    /// Reihenpfad mit <c>data-reihe</c>; die Zugehörigkeit steht in der Marke
    /// <c>reihe:&lt;Name&gt;</c>. Ohne diese zweite Lesart färbte ein Klick nur den
    /// Legendeneintrag und blendete nichts aus.
    /// </summary>
    [Fact]
    public void DS9_Ein_Legendenklick_blendet_auch_die_Saeulen_aus()
    {
        var cut = ZeigeStapelbild();

        int vorher = cut.FindAll("[data-marke='reihe:Wärmepumpe'][display='none']").Count;
        Assert.Equal(0, vorher);

        cut.Find("[data-legende='Wärmepumpe'].epos-legende-eintrag").Click();

        Assert.True(cut.Instance.IstAus("Wärmepumpe"));
        Assert.NotEmpty(cut.FindAll("[data-marke='reihe:Wärmepumpe'][display='none']"));
        // Die NACHBARREIHE bleibt stehen.
        Assert.Empty(cut.FindAll("[data-marke='reihe:Heizkessel'][display='none']"));
    }

    // =====================================================================
    //  DS-10  Achsenseite und Punktreihen (DG-E3-11/12)
    // =====================================================================

    /// <summary>
    /// <b>Die Reihe sagt selbst, auf welcher Achse sie steht</b> (DG-E3-12). Der Fall
    /// gibt beiden Achsen DIESELBE y-Spanne — das alte Raten über die Spanne hätte hier
    /// die rechte Reihe für eine linke gehalten und ihr „kW" statt „kWh" gegeben.
    /// </summary>
    [Fact]
    public async Task DS10_Die_Achsenseite_kommt_aus_der_Reihe_nicht_aus_der_Spanne()
    {
        var links = new double[] { 10, 20, 30, 40 };
        var rechts = new double[] { 10, 20, 30, 40 };
        var flaeche = new Zeichenflaeche(new Rahmen(0, 0, 100, 100),
                                         new Datenfenster(0, 3, 0, 40));

        var modell = new Zeichenmodell(200, 120, Farbton.Aus(Farbrolle.HINTERGRUND))
        {
            Flaeche = flaeche
        };
        modell.FuegeReihe(new Datenreihe("Leistung", Farbton.Aus(Farbrolle.STAMM), 2f, null,
                                         links, flaeche.Daten));
        modell.FuegeReihe(new Datenreihe("Inhalt", Farbton.Aus(Farbrolle.STROM_NETZ), 2f, null,
                                         rechts, flaeche.Daten,
                                         Achsenseite: Achsenseite.Rechts));

        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x => x.Modell, modell);
            p.Add(x => x.Kennung, "zweiachsen");
            p.Add(x => x.Einheit, "kW");
            p.Add(x => x.EinheitRechts, "kWh");
        });

        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(1));

        string zeile = cut.Find(".epos-diagramm-zeigerzeile").TextContent;
        Assert.Contains("Leistung: 20 kW", zeile);
        Assert.Contains("Inhalt: 20 kWh", zeile);
    }

    /// <summary>
    /// <b>Eine Reihe mit eigenen x-Stellen wird GESUCHT, nicht gerechnet</b>
    /// (DG-E3-11): Die Zeigerzeile nimmt den NÄCHSTLIEGENDEN Punkt. Über die
    /// gleichmäßige Schrittweite gerechnet stünde bei ungleichmäßigen Stützstellen der
    /// falsche Wert da.
    /// </summary>
    [Fact]
    public async Task DS10_Die_Zeigerzeile_nimmt_den_naechstliegenden_Punkt()
    {
        // Vier Stuetzstellen, ungleichmaessig: 0, 1, 8, 10.
        var x = new double[] { 0, 1, 8, 10 };
        var y = new double[] { 100, 200, 300, 400 };
        var flaeche = new Zeichenflaeche(new Rahmen(0, 0, 100, 100),
                                         new Datenfenster(0, 10, 0, 400),
                                         Achsenart.Wert, "kWh");

        var modell = new Zeichenmodell(200, 120, Farbton.Aus(Farbrolle.HINTERGRUND))
        {
            Flaeche = flaeche
        };
        modell.FuegeReihe(new Datenreihe("Kapitalwert", Farbton.Aus(Farbrolle.STAMM), 2f, null,
                                         y, flaeche.Daten, Reihenart.Linie, null, null, x, "€"));

        var cut = Render<DiagrammSvg>(p =>
        {
            p.Add(x2 => x2.Modell, modell);
            p.Add(x2 => x2.Kennung, "ungleich");
        });

        // Die Stelle 7 liegt am naechsten bei der Stuetzstelle 8 - drittes Element.
        // Gleichmaessig gerechnet (Schrittweite 10/3) waere es das zweite.
        await cut.InvokeAsync(() => cut.Instance.ZeigerGemeldet(7));
        string zeile = cut.Find(".epos-diagramm-zeigerzeile").TextContent;

        Assert.Contains("Kapitalwert: 300 €", zeile);
        // Die EINHEIT der x-Stelle kommt aus dem Modell, nicht aus einem Parameter.
        Assert.Contains("7 kWh", zeile);
    }

    // =====================================================================
    //  DS-11  Der Wähler weicht keinem zweiten Klick aus (DG-E3-14)
    // =====================================================================
    //
    //  Die Schliessflaeche liegt ueber dem Bild. Ohne die Klasse
    //  epos-diagramm-svg-flaeche--waehler faengt sie jeden Klick auf ein anderes
    //  Farbfeld ab: Der erste Klick schloesse nur, erst der zweite oeffnete - und
    //  fuer den Anwender oeffnet "der Klick auf das Farbfeld nicht immer".

    /// <summary>Die ZWEITE Reihe mit Farbrolle — auf sie wechselt der Wähler.</summary>
    private const string ZWEITE_ROLLE = "Wärmepumpe";

    /// <summary>
    /// Sechs Reihen, fünf davon in einer Hausfarbe und damit mit Rolle. Die
    /// Legende läuft dadurch über die halbe Bildbreite: Nur so gibt es
    /// Legendeneinträge in BEIDEN Bildhälften, und die Lage des Wählers ist
    /// prüfbar.
    /// </summary>
    private static Zeichenmodell Rollenmodell()
    {
        static double[] Welle(double hub, double versatz)
        {
            var werte = new double[744];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = hub + hub * Math.Sin(2 * Math.PI * i / 744.0 + versatz);
            return werte;
        }

        return ChartRenderer.JahresgangModell(
            "Rollen im Bild",
            new[]
            {
                new ChartRenderer.Reihe(MIT_ROLLE, Welle(12, 0.0), ChartRenderer.C_AUSSENTEMPERATUR),
                new ChartRenderer.Reihe(ZWEITE_ROLLE, Welle(8, 0.4), ChartRenderer.C_WP),
                new ChartRenderer.Reihe(OHNE_ROLLE, Welle(20, 0.8), SkiaSharp.SKColors.Orange),
                new ChartRenderer.Reihe("Photovoltaik", Welle(6, 1.2), ChartRenderer.C_PV),
                new ChartRenderer.Reihe("Netzbezug", Welle(9, 1.6), ChartRenderer.C_NETZ),
                new ChartRenderer.Reihe("Wärmebedarf", Welle(15, 2.0), ChartRenderer.C_BEDARF)
            },
            "Stunde des Jahres", "Leistung [kW]");
    }

    private IRenderedComponent<DiagrammSvg> ZeigeRollen()
        => Zeige(modell: Rollenmodell(), kennung: "rollen", farbwahl: true);

    /// <summary>Das erste Farbfeld eines Eintrags — der Rahmen liegt auf der Füllung.</summary>
    private static IElement Farbfeld(IRenderedComponent<DiagrammSvg> cut, string reihe)
        => cut.FindAll("rect[data-legende='" + reihe + "']")[0];

    /// <summary>
    /// <b>Der Wechsel von einem Eintrag zum nächsten kostet EINEN Klick.</b> Fängt
    /// die Schließfläche den Klick auf das zweite Farbfeld ab, schließt er nur den
    /// Wähler, und erst ein zweiter öffnet ihn wieder.
    /// </summary>
    [Fact]
    public void DS11_Ein_Klick_wechselt_vom_einen_Farbfeld_zum_anderen()
    {
        var cut = ZeigeRollen();

        Farbfeld(cut, MIT_ROLLE).Click();
        Assert.Equal(Diagrammfarben.Anzeigename(ChartRenderer.C_AUSSENTEMPERATUR.Ton().Rolle),
                     cut.Find(".epos-farbwahl .epos-farbfeld-name").TextContent);

        Farbfeld(cut, ZWEITE_ROLLE).Click();

        Assert.Single(cut.FindAll(".epos-farbwahl"));
        Assert.Equal(Diagrammfarben.Anzeigename(ChartRenderer.C_WP.Ton().Rolle),
                     cut.Find(".epos-farbwahl .epos-farbfeld-name").TextContent);
    }

    /// <summary>
    /// Dasselbe Farbfeld ein zweites Mal schließt den Wähler — das Feld ist ein
    /// UMSCHALTER, nicht nur ein Öffner.
    /// </summary>
    [Fact]
    public void DS11_Dasselbe_Farbfeld_schliesst_den_Waehler()
    {
        var cut = ZeigeRollen();

        Farbfeld(cut, MIT_ROLLE).Click();
        Assert.Single(cut.FindAll(".epos-farbwahl"));

        Farbfeld(cut, MIT_ROLLE).Click();
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// Der Legendentext schaltet seine Reihe UND schließt den offenen Wähler in
    /// EINEM Klick: Sein Klick steigt zur Fläche auf, und dort schließt er.
    /// </summary>
    [Fact]
    public void DS11_Der_Legendentext_schaltet_und_schliesst_in_einem_Klick()
    {
        var cut = ZeigeRollen();

        Farbfeld(cut, MIT_ROLLE).Click();
        Assert.Single(cut.FindAll(".epos-farbwahl"));

        cut.Find("text[data-legende='" + OHNE_ROLLE + "']").Click();

        Assert.True(cut.Instance.IstAus(OHNE_ROLLE));
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>Ein Klick irgendwo auf das Bild schließt den Wähler.</summary>
    [Fact]
    public void DS11_Ein_Klick_auf_die_Flaeche_schliesst_den_Waehler()
    {
        var cut = ZeigeRollen();

        Farbfeld(cut, MIT_ROLLE).Click();
        cut.Find(".epos-diagramm-svg-flaeche").Click();

        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// Ein Klick IM Wähler lässt ihn stehen — sonst schlösse der Griff zum
    /// Systemwähler genau das Fenster, das er bedient.
    /// </summary>
    [Fact]
    public void DS11_Ein_Klick_im_Waehler_laesst_ihn_stehen()
    {
        var cut = ZeigeRollen();

        Farbfeld(cut, MIT_ROLLE).Click();
        cut.Find(".epos-farbwahl").Click();

        Assert.Single(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// Die Fläche trägt <c>epos-diagramm-svg-flaeche--waehler</c> nur, solange ein
    /// Wähler offen ist: Die Klasse hebt das Bild über die Schließfläche, und ohne
    /// Wähler gibt es nichts zu heben.
    /// </summary>
    [Fact]
    public void DS11_Die_Flaeche_traegt_die_Klasse_nur_bei_offenem_Waehler()
    {
        const string KLASSE = "epos-diagramm-svg-flaeche--waehler";
        var cut = ZeigeRollen();

        Assert.DoesNotContain(KLASSE, cut.Find(".epos-diagramm-svg-flaeche").ClassName);

        Farbfeld(cut, MIT_ROLLE).Click();
        Assert.Contains(KLASSE, cut.Find(".epos-diagramm-svg-flaeche").ClassName);

        cut.Find(".epos-diagramm-svg-flaeche").Click();
        Assert.DoesNotContain(KLASSE, cut.Find(".epos-diagramm-svg-flaeche").ClassName);
    }

    /// <summary>
    /// <b>Die Lage des Wählers folgt der Bildhälfte.</b> Links hängt er an der
    /// linken Kante seines Farbfeldes; in der rechten Hälfte hängt er an der
    /// RECHTEN — mit <c>left</c> stünde er dort über den Bildrand hinaus.
    /// </summary>
    [Fact]
    public void DS11_In_der_rechten_Bildhaelfte_haengt_der_Waehler_rechts()
    {
        Zeichenmodell modell = Rollenmodell();
        var cut = Zeige(modell: modell, kennung: "lage", farbwahl: true);

        // Erst die Stellen lesen, dann klicken: Jeder Zeichenlauf tauscht die
        // Knoten, und ein gemerkter Verweis zeigte danach ins Leere.
        var stellen = new List<(string Name, double X)>();
        foreach (Datenreihe reihe in modell.Reihen)
        {
            var felder = cut.FindAll("rect[data-legende='" + reihe.Name + "']");
            if (felder.Count == 0) continue;   // eine Reihe ohne Rolle traegt kein Feld
            stellen.Add((reihe.Name ?? "",
                         double.Parse(felder[0].GetAttribute("x")!,
                                      CultureInfo.InvariantCulture)));
        }

        int links = 0, rechts = 0;
        foreach ((string name, double x) in stellen)
        {
            Farbfeld(cut, name).Click();
            string stil = cut.Find(".epos-farbwahl").GetAttribute("style")!;

            if (x > modell.Breite / 2.0) { rechts++; Assert.StartsWith("right:", stil); }
            else { links++; Assert.StartsWith("left:", stil); }

            cut.Find(".epos-diagramm-svg-flaeche").Click();
        }

        // Bewiesen ist die Regel nur, wenn beide Haelften vorkommen.
        Assert.True(links > 0, "kein Legendeneintrag in der linken Bildhälfte");
        Assert.True(rechts > 0, "kein Legendeneintrag in der rechten Bildhälfte");
    }

    // =====================================================================
    //  DS-12  Dasselbe Bild in neuer Instanz (DG-E3-15)
    // =====================================================================
    //
    //  Die Reiter bauen ihr Zeichenmodell bei JEDEM Zeichenlauf neu. Haenge der
    //  Zustand an der Referenz, verloere der Anwender nach jeder Farbwahl Zoom
    //  und abgewaehlte Reihe - entgegen der Zusage des Farbwahlwirtes. Der
    //  Baustein setzt deshalb nur zurueck, wenn das neue Modell ein ANDERES BILD
    //  zeigt, nicht wenn es eine neue Instanz desselben ist.

    /// <summary>
    /// <b>Dasselbe Bild in neuer Instanz lässt jeden Zustand stehen:</b>
    /// abgewählte Reihe, Fenster samt nachgezeichneter Achsenteilung und den
    /// offenen Farbwähler.
    /// </summary>
    [Fact]
    public async Task DS12_Eine_neue_Instanz_desselben_Bildes_laesst_den_Zustand_stehen()
    {
        var cut = Zeige(farbwahl: true);

        cut.Find("text[data-legende='" + OHNE_ROLLE + "']").Click();
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(3000, 3400));
        cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']")[0].Click();

        Assert.True(cut.Instance.IstAus(OHNE_ROLLE));
        Assert.Equal((3000, 3400), cut.Instance.Fenster);
        Assert.Single(cut.FindAll(".epos-farbwahl"));
        int ticks = cut.FindAll(".epos-diagramm-ticks line").Count;
        Assert.NotEqual(0, ticks);

        // DIESELBEN Reihen, dieselbe Flaeche - nur eine andere Instanz.
        cut.Render(p => p.Add(x => x.Modell, Modell()));

        Assert.True(cut.Instance.IstAus(OHNE_ROLLE));
        Assert.Equal((3000, 3400), cut.Instance.Fenster);
        Assert.Equal(ticks, cut.FindAll(".epos-diagramm-ticks line").Count);
        Assert.All(cut.FindAll("[data-marke='xachse']"),
                   e => Assert.Equal("none", e.GetAttribute("display")));
        Assert.Single(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// <b>Ein anderer Reihenname ist ein anderes Bild:</b> Ausschnitt, Zeiger,
    /// abgewählte Reihe und der Wähler haben dort keine Bedeutung mehr.
    /// </summary>
    [Fact]
    public async Task DS12_Ein_anderes_Bild_setzt_alles_zurueck()
    {
        var cut = Zeige(farbwahl: true);

        cut.Find("text[data-legende='" + MIT_ROLLE + "']").Click();
        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(3000, 3400));
        cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']")[0].Click();

        cut.Render(p => p.Add(x => x.Modell, Jahresgang("Sonnenhöhe")));

        Assert.False(cut.Instance.IstAus(MIT_ROLLE));
        Assert.Null(cut.Instance.Fenster);
        Assert.Empty(cut.FindAll(".epos-diagramm-ticks"));
        Assert.Empty(cut.FindAll(".epos-farbwahl"));
    }

    /// <summary>
    /// Der nachgerechnete Ausschnitt überlebt die neue Instanz — <b>mit den Werten
    /// des NEUEN Modells</b>: Ein stehen gebliebener Pfad zeigte sonst den vorigen
    /// Rechenlauf.
    /// </summary>
    [Fact]
    public async Task DS12_Der_nachgerechnete_Ausschnitt_ueberlebt_die_neue_Instanz()
    {
        var cut = Render<DiagrammSvg>(p => p
            .Add(x => x.Modell, Stapelmodell())
            .Add(x => x.Kennung, "stapel"));

        await cut.InvokeAsync(() => cut.Instance.FensterGemeldet(1000, 1200));
        Assert.NotEmpty(cut.Instance.Ausschnittpfade);

        Zeichenmodell neu = Stapelmodell();
        cut.Render(p => p.Add(x => x.Modell, neu));

        Assert.Equal((1000, 1200), cut.Instance.Fenster);

        Datenreihe reihe = neu.Reihen.Single(r => r.Name == FLAECHE);
        string erwartet = SvgSchreiber.Reihenpfad(reihe, neu.Flaeche, 1000, 1200, true);

        Assert.Equal(erwartet, cut.Instance.Ausschnittpfade[FLAECHE]);
        Assert.Equal(erwartet, cut.Find("path[data-reihe='" + FLAECHE + "']").GetAttribute("d"));
    }

    // =====================================================================
    //  DS-13  Jede Reihe hat einen Wähler (DG-E5, Anwenderentscheid DG-Q8)
    // =====================================================================

    /// <summary>Die Reihe mit im Layout GERECHNETER Farbe samt Herkunftsrolle.</summary>
    private const string GERECHNET = "Abgestuft";

    /// <summary>
    /// Ein Bild mit drei Reihen: eine Hausfarbe, eine im Layout gerechnete Farbe
    /// MIT Herkunftsrolle und eine Farbe ganz ohne Rolle.
    /// </summary>
    private static Zeichenmodell Dreierbild()
    {
        var a = new double[8760];
        var b = new double[8760];
        var c = new double[8760];
        for (int i = 0; i < 8760; i++)
        {
            a[i] = 10.0 - 12.0 * Math.Cos(2 * Math.PI * i / 8760.0);
            b[i] = 20.0 + 5.0 * Math.Sin(2 * Math.PI * i / 8760.0);
            c[i] = 30.0 + 20.0 * Math.Sin(2 * Math.PI * i / 8760.0);
        }

        return ChartRenderer.JahresgangModell(
            "Dreierbild",
            new[]
            {
                new ChartRenderer.Reihe(MIT_ROLLE, a, Farbrolle.AUSSENTEMPERATUR),
                new ChartRenderer.Reihe(GERECHNET, b,
                                        Farbpalette.Gerechnet(Farbrolle.REST,
                                                              new Farbe(0x7B, 0x1F, 0xA2))),
                new ChartRenderer.Reihe(OHNE_ROLLE, c, new SkiaSharp.SKColor(0x12, 0x34, 0x56))
            },
            "Stunde des Jahres", "Temperatur [°C]");
    }

    /// <summary>
    /// <b>Eine gerechnete Farbe mit Herkunftsrolle bekommt ihr Farbfeld</b>
    /// (DG-Q8): Der Wähler zeigt auf die HERKUNFTSROLLE — die Abstufung entsteht
    /// weiterhin im Bild, aus der geänderten Hausfarbe. Nur eine Farbe ohne Rolle
    /// bleibt ohne.
    /// </summary>
    [Fact]
    public void DS13_Eine_gerechnete_Farbe_mit_Herkunftsrolle_bekommt_einen_Waehler()
    {
        var cut = Zeige(modell: Dreierbild(), farbwahl: true, kennung: "drei");

        Assert.NotEmpty(cut.FindAll("rect[data-legende='" + MIT_ROLLE + "']"));
        Assert.NotEmpty(cut.FindAll("rect[data-legende='" + GERECHNET + "']"));
        Assert.Empty(cut.FindAll("rect[data-legende='" + OHNE_ROLLE + "']"));

        // Kein Farbfeld gehoert zum Eintrag ohne Rolle - die zwei anderen tragen es.
        Assert.NotEmpty(cut.FindAll(".epos-legende-farbfeld"));
        Assert.All(cut.FindAll(".epos-legende-farbfeld"),
                   f => Assert.NotEqual(OHNE_ROLLE, f.GetAttribute("data-legende")));
        Assert.Contains(cut.FindAll(".epos-legende-farbfeld"),
                        f => f.GetAttribute("data-legende") == GERECHNET);
    }

    /// <summary>
    /// Der Klick auf das Farbfeld der gerechneten Reihe meldet ihre
    /// HERKUNFTSROLLE — nicht die gerechnete Farbe.
    /// </summary>
    [Fact]
    public void DS13_Der_Waehler_der_gerechneten_Reihe_meldet_die_Herkunftsrolle()
    {
        var wahl = new List<(Farbrolle Rolle, Farbe Farbe)>();
        var cut = Zeige(modell: Dreierbild(), kennung: "drei",
                        farbeGewaehlt: w => wahl.Add(w));

        cut.FindAll("rect[data-legende='" + GERECHNET + "']")[0].Click();
        Assert.Single(cut.FindAll(".epos-farbwahl"));

        cut.Find(".epos-farbwahl input.epos-farbfeld-waehler").Change("#00A000");

        Assert.Single(wahl);
        Assert.Equal(Farbrolle.REST, wahl[0].Rolle);
        Assert.Equal(0x00, wahl[0].Farbe.R);
        Assert.Equal(0xA0, wahl[0].Farbe.G);
        Assert.Equal(0x00, wahl[0].Farbe.B);
    }

    /// <summary>
    /// Umschalt+Eingabe öffnet den Wähler auch auf der gerechneten Reihe — der
    /// Tastaturweg gilt für jeden Eintrag, der ein Farbfeld hat.
    /// </summary>
    [Fact]
    public void DS13_Umschalt_und_Eingabe_oeffnet_den_Waehler_der_gerechneten_Reihe()
    {
        var cut = Zeige(modell: Dreierbild(), farbwahl: true, kennung: "drei");

        cut.Find("text[data-legende='" + GERECHNET + "']")
           .KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        Assert.Single(cut.FindAll(".epos-farbwahl"));
        Assert.False(cut.Instance.IstAus(GERECHNET));   // geschaltet wird dabei NICHT
    }
}
