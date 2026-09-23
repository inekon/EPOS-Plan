using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der BEDARFS-Reiter (iU9-W11b.3), Vorbild <c>tabPage_Bedarf</c> mit
/// <c>chart1</c>/<c>chart2</c> und den drei Kanalzeilen.
///
/// <para>Soll: die vier Zahlen, die Kanalzeilen nur bei Praesenz, ein
/// Bildauftrag je Schalterstellung (Befund W11-B14: EINE Fuelllogik statt
/// zweier) und die drei Rueckrufe.</para>
/// <para>Der Selektor nennt seit der Windows-Abnahme 05.09.2026 die Klasse
/// <c>epos-simerg-knopf</c>: Jedes Diagramm steht seither im Baustein
/// <c>Diagramm</c> und bringt seine eigenen Knöpfe („1:1“, „Bereich“) mit.
/// <c>FindAll("button")</c> zählte die mit und prüfte damit nicht mehr, was
/// der Fall behauptet — nämlich die Knöpfe DIESES Reiters.</para>
/// </summary>
public class BedarfReiterTests : EposBunitContext
{
    private readonly List<Bildauftrag> _auftraege = new();

    /// <summary>
    /// Die zwei Zeichenmodelle des Reiters — EINE Instanz je Bild, EINMAL gebaut.
    /// Der Baustein <c>DiagrammSvg</c> vergleicht die Modellreferenz; ein Delegat,
    /// der bei jedem Aufruf ein neues Modell bauen würde, ließe ihn seinen Baum je
    /// Zeichenlauf neu setzen und nähme ihm Zoom und abgewählte Reihe.
    /// </summary>
    private static readonly Zeichenmodell WAERME =
        Ganglinie("Wärmelast Jahresganglinie", ChartRenderer.C_BEDARF);

    private static readonly Zeichenmodell STROM =
        Ganglinie("Strombedarf Jahresganglinie", ChartRenderer.C_NETZ);

    /// <summary>
    /// Eine kurze, echte Ganglinie (eine Woche) — kein Jahr: Der Fall prüft die
    /// Bedienung, nicht die Rechenzeit.
    /// </summary>
    private static Zeichenmodell Ganglinie(string titel, SkiaSharp.SKColor farbe)
    {
        var werte = new double[168];
        for (int i = 0; i < werte.Length; i++)
            werte[i] = 40.0 + 20.0 * Math.Sin(2 * Math.PI * i / 24.0);

        return ChartRenderer.GanglinieNormiertModell(
            titel, new[] { new ChartRenderer.Reihe("Gesamt", werte, farbe) },
            "kW", ChartRenderer.Achse.Jahresstunden, false);
    }

    public BedarfReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static BedarfDaten Daten(bool prozess = false) => new BedarfDaten
    {
        WaermelastMaxKw = 1234.5,
        WaermebedarfGesamtMwh = 480.25,
        StrombedarfMaxKw = 88.75,
        StrombedarfGesamtMwh = 120.5,
        KanalMwh = new[] { 400.0, 80.25, 0.0 },
        Kanalnamen = new[] { "Heizung", "Brauchwasser", "Prozesswärme" },
        KanalDa = new[] { true, true, prozess }
    };

    private IRenderedComponent<BedarfReiter> Zeichnen(BedarfDaten daten,
                                                      Action? waerme = null,
                                                      Action? strom = null,
                                                      Action? csv = null)
        => Render<BedarfReiter>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Modell, a =>
            {
                _auftraege.Add(a);
                return a.Bild == Bilder.BedarfWaerme ? WAERME : STROM;
            });
            if (waerme is not null) p.Add(x => x.WaermeDetails, EventCallback.Factory.Create(this, waerme));
            if (strom is not null) p.Add(x => x.StromDetails, EventCallback.Factory.Create(this, strom));
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
        });

    // =====================================================================

    [Fact]
    public void Die_vier_Zahlen_stehen_mit_zwei_Nachkommastellen()
    {
        var seite = Zeichnen(Daten());
        string text = seite.Markup;

        Assert.Contains("1.234,50", text);    // N2 mit Tausendertrennung (W11b-B-13)
        Assert.Contains("480,25", text);
        Assert.Contains("88,75", text);
        Assert.Contains("120,50", text);
    }

    /// <summary>
    /// <b>Anwenderrückmeldung 08.09.2026 (W11b‑B‑15).</b> Die Ordnung „Wärme
    /// links, Strom rechts" stand hier nur als Kommentar im Quelltext — auf dem
    /// Schirm zwei namenlose Listen nebeneinander. Die zwei Balken sind
    /// dieselben wie in der Übersicht; „Wärmebedarf je Bedarfsart" wird damit
    /// zum Unterabschnitt der Wärme (ein Balken im Balken wäre eine Hierarchie,
    /// die es nicht gibt).
    /// </summary>
    [Fact]
    public void Die_zwei_Spalten_tragen_die_Koepfe_Waerme_und_Strom()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(new[] { "Wärme", "Strom" },
                     seite.FindAll("h2.epos-gruppenkopf-titel").Select(k => k.TextContent.Trim()).ToArray());
        Assert.Equal("Wärmebedarf je Bedarfsart",
                     seite.Find("h3.epos-untergruppe").TextContent.Trim());
    }

    /// <summary>
    /// <b>W11b‑B‑23 (09.09.2026).</b> Der Reiter war der einzige des Stapels ohne
    /// betonte Abschlusszeile — seine <c>Wertzeile</c> kannte den Schalter gar
    /// nicht. „Gesamter Wärmebedarf" und „Gesamter Strombedarf" sind die
    /// SUMMENzeile ihrer Gruppe und stehen jetzt so da, wie die Restzeile in
    /// jedem anderen Reiter steht.
    ///
    /// <para>Und sie tragen MWh/a wie die Zeile, aus der sie stammen: Beide
    /// nannten dieselbe DTO-Größe, die eine in „MWh", die andere in „MWh/a".</para>
    /// </summary>
    [Fact]
    public void Die_Summenzeile_schliesst_jede_Spalte_betont_ab()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(new[] { "Gesamter Wärmebedarf:", "Gesamter Strombedarf:" },
                     seite.FindAll("dt.epos-simerg-abschluss")
                          .Select(z => z.TextContent.Trim()).ToArray());
        Assert.Equal(4, seite.FindAll("dd.epos-simerg-abschluss").Count);

        // Die uebrigen Zeilen bleiben ohne Klasse - kein leeres class="".
        Assert.DoesNotContain("<dt class=\"\">", seite.Markup);
        Assert.DoesNotContain("<dd class=\"epos-simerg-einheit\">MWh</dd>", seite.Markup);
    }

    /// <summary>
    /// <b>W11b‑B‑23.</b> „max. Wärmelast", „Gesamter Wärmebedarf",
    /// „max. Strombedarf" und „Gesamter Strombedarf" standen als einzige
    /// Beschriftungen der Ergebnisreiter OHNE Doppelpunkt da. Sie tragen ihn
    /// jetzt — die zwei Gruppenlisten sind damit in JEDER Zeile gleich gesetzt.
    ///
    /// <para>Die Kanalliste dazwischen bleibt ausgenommen: Ihre Beschriftungen
    /// sind die NAMEN der Bedarfsarten, die der Lauf führt, keine
    /// Feldbeschriftungen.</para>
    /// </summary>
    [Fact]
    public void Jede_Beschriftung_der_zwei_Gruppen_endet_auf_einen_Doppelpunkt()
    {
        var seite = Zeichnen(Daten());
        var listen = seite.FindAll("dl.epos-simerg-werte");

        foreach (int nr in new[] { 0, 2 })          // [0] Wärme, [1] Kanäle, [2] Strom
        {
            Assert.All(listen[nr].QuerySelectorAll("dt"),
                       z => Assert.EndsWith(":", z.TextContent.Trim()));
        }

        Assert.Equal(new[] { "Heizung", "Brauchwasser" },
                     listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Ein Kanal, den der Lauf nicht fuehrt, hat weder Zeile noch Schalter
    /// (<c>_bedarfKanalDa</c> im Vorlaeufer).
    /// </summary>
    [Fact]
    public void Kanaele_ohne_Praesenz_stehen_nicht_da()
    {
        var seite = Zeichnen(Daten());

        Assert.Contains("Heizung", seite.Markup);
        Assert.Contains("Brauchwasser", seite.Markup);
        Assert.DoesNotContain("Prozesswärme", seite.Markup);
    }

    [Fact]
    public void Mit_Prozesskanal_steht_die_dritte_Zeile_da()
    {
        var seite = Zeichnen(Daten(prozess: true));
        Assert.Contains("Prozesswärme", seite.Markup);
    }

    /// <summary>Zwei Bilder — Waermelast und Strombedarf.</summary>
    [Fact]
    public void Zwei_Bilder_werden_angefordert()
    {
        Zeichnen(Daten());

        Assert.Contains(_auftraege, a => a.Bild == Bilder.BedarfWaerme);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.BedarfStrom);
    }

    /// <summary>
    /// <b>Beide Ganglinien stehen als SVG im Baum</b> (Etappe DG-E3, Gruppe (a)) —
    /// mit ihrer eigenen Kennung, damit zwei Bilder nie dieselben
    /// <c>clipPath</c>-Kennungen bekommen.
    /// </summary>
    [Fact]
    public void Beide_Ganglinien_stehen_als_DiagrammSvg_mit_eigener_Kennung()
    {
        var seite = Zeichnen(Daten());
        var bilder = seite.FindComponents<DiagrammSvg>();

        Assert.Equal(2, bilder.Count);
        Assert.Equal(new[] { "simerg-bedarf-waerme", "simerg-bedarf-strom" },
                     bilder.Select(b => b.Instance.Kennung).ToArray());
        Assert.Equal(2, seite.FindAll("svg.epos-flaeche").Count);
    }

    /// <summary>
    /// Befund W11-B14: EINE Fuelllogik. „Sortiert" wechselt nur den
    /// Bildauftrag; derselbe Schalterstand ergibt denselben Schluessel.
    ///
    /// <para><b>Anwenderwunsch 08.09.2026 (W11b‑B‑16):</b> und zwar EIN Schalter
    /// für BEIDE Ganglinien. Bis dahin trug jede Spalte einen eigenen; der der
    /// Stromspalte kam im Zwei-Spalten-Raster neben „Wärmebedarf je Bedarfsart"
    /// zu stehen, und der Anwender meldete ihn als „funktioniert nicht" — er
    /// hakte ihn an und sah die WÄRME-Ganglinie unverändert.</para>
    /// </summary>
    [Fact]
    public void Der_eine_Sortiertschalter_wechselt_beide_Bildauftraege()
    {
        var seite = Zeichnen(Daten());

        // Genau EINER — vor W11b‑B‑16 stand in jeder Spalte einer.
        Assert.Single(seite.FindAll("label.epos-schalter"),
                      s => s.TextContent.Trim() == "sortiert");

        _auftraege.Clear();
        seite.FindAll("input[type='checkbox']")[0].Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.BedarfWaerme && a.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.BedarfStrom && a.Sortiert);
    }

    /// <summary>
    /// Die Kanalschalter wirken je Serie — der Bildauftrag traegt die
    /// gewaehlten Schluessel, und der CSV-Export nimmt dieselben.
    /// </summary>
    [Fact]
    public void Die_Kanalschalter_stehen_im_Bildauftrag()
    {
        var seite = Zeichnen(Daten());

        // [0] sortiert (W11b‑B‑16: der EINE, ganz oben), [1] Gesamt,
        // [2] Heizung, [3] Brauchwasser — mehr Kästchen hat der Reiter nicht.
        var kaesten = seite.FindAll("input[type='checkbox']");
        Assert.Equal(4, kaesten.Count);
        kaesten[2].Change(true);

        Assert.Contains("KANAL_0", seite.Instance.GewaehlteReihen);
        Assert.Contains("GESAMT", seite.Instance.GewaehlteReihen);
    }

    [Fact]
    public void Ohne_Rueckruf_bleiben_die_drei_Knoepfe_weg()
    {
        var seite = Zeichnen(Daten());
        Assert.Empty(seite.FindAll("button.epos-simerg-knopf"));
    }

    [Fact]
    public void Die_drei_Knoepfe_melden_ihren_Klick()
    {
        int w = 0, s = 0, c = 0;
        var seite = Zeichnen(Daten(), () => w++, () => s++, () => c++);

        var knoepfe = seite.FindAll("button.epos-simerg-knopf");
        Assert.Equal(3, knoepfe.Count);
        knoepfe[0].Click();
        knoepfe[1].Click();
        knoepfe[2].Click();

        Assert.Equal(1, w);
        Assert.Equal(1, s);
        Assert.Equal(1, c);
    }

    // =====================================================================
    //  Stufe KU1 — die Kälteseite (Kühlkonzept 8.4; E21, K5, K6, K18)
    // =====================================================================

    private static readonly Zeichenmodell KAELTE =
        Ganglinie("Kältelast Jahresganglinie", ChartRenderer.C_BEDARF);

    private static readonly string FEUCHTE = SimulationKaeltebedarf.GrenzeFeuchte;

    private static BedarfDaten MitKaelte()
    {
        BedarfDaten d = Daten();
        d.Kaelte = new KaelteDaten
        {
            KaeltebedarfMwh = 12.5,
            KaeltelastMaxKw = 8.25,
            StundenMitKuehlbedarf = 640,
            VollbenutzungsstundenH = 1515.0,
            KaelterestbedarfMwh = 12.5,
            Kanalname = "Kühlung",
            Hinweise = new[] { "Kein Kälteerzeuger im Projekt." },
            GrenzeFeuchte = FEUCHTE,
            Deckungshinweis = "Kein Kälteerzeuger im Projekt."
        };
        return d;
    }

    private IRenderedComponent<BedarfReiter> ZeichnenMitKaelte(BedarfDaten daten, Action? csvKaelte = null)
        => Render<BedarfReiter>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Modell, a =>
            {
                _auftraege.Add(a);
                return a.Bild == Bilder.BedarfWaerme ? WAERME : a.Bild == Bilder.BedarfKaelte ? KAELTE : STROM;
            });
            if (csvKaelte is not null) p.Add(x => x.CsvKaelte, EventCallback.Factory.Create(this, csvKaelte));
        });

    /// <summary>
    /// K18: Ein Projekt, das keine Kälte ERHOBEN hat, zeigt keine Kältegruppe — keine Nullen,
    /// kein Bild, kein Bildauftrag.
    /// </summary>
    [Fact]
    public void Ohne_erhobene_Kaelte_steht_keine_Kaeltegruppe()
    {
        var seite = Zeichnen(Daten());

        Assert.Empty(seite.FindAll("section.epos-simerg-kaelte"));
        Assert.DoesNotContain(_auftraege, a => a.Bild == Bilder.BedarfKaelte);
        Assert.DoesNotContain("Kälte", seite.Markup);
    }

    /// <summary>
    /// Der Abschnitt „Kälte" (8.4): Werte mit betonter Summenzeile, die VIERTE Kanalzeile
    /// „Kühlung" in eigener Untergruppe, das EIGENE Bild der Kältelast und die Grenze der Zahl
    /// (K5) — und der Kühlkanal steht NICHT in den Kanalzeilen der Wärme (4.3 #32).
    /// </summary>
    [Fact]
    public void Mit_Kaelte_steht_der_Abschnitt_mit_Kanalzeile_eigenem_Bild_und_Grenze()
    {
        var seite = ZeichnenMitKaelte(MitKaelte());
        var block = seite.Find("section.epos-simerg-kaelte");

        Assert.Contains("12,50", block.TextContent);
        Assert.Contains("8,25", block.TextContent);
        Assert.Contains("640", block.TextContent);
        Assert.Contains(FEUCHTE, block.TextContent);
        Assert.Contains("Kein Kälteerzeuger im Projekt.", block.TextContent);

        var listen = block.QuerySelectorAll("dl.epos-simerg-werte");
        Assert.Equal(2, listen.Length);
        Assert.Equal(new[] { "Kühlung" },
                     listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
        Assert.DoesNotContain("Kühlung", seite.FindAll("dl.epos-simerg-werte")[1].TextContent);

        Assert.Contains(_auftraege, a => a.Bild == Bilder.BedarfKaelte);
        Assert.Equal(new[] { "simerg-bedarf-waerme", "simerg-bedarf-strom", "simerg-bedarf-kaelte" },
                     seite.FindComponents<DiagrammSvg>().Select(b => b.Instance.Kennung).ToArray());
    }

    /// <summary>
    /// Symmetrie (E21): Der Kälteabschnitt führt dieselben Bausteine wie die Wärme — Kopf,
    /// Werteliste mit betonter Summenzeile (die letzte), die Untergruppe „je Bedarfsart" mit
    /// den Kanalzeilen und ein eigenes Bild. Abweichungen sind benannt: die Kälte hat keinen
    /// Kanalschalter (ein Kanal, keine Auswahl) und dafür die Sätze zu Deckung, K6 und K5.
    /// </summary>
    [Fact]
    public void Der_Kaelteabschnitt_fuehrt_die_Bausteine_der_Waerme()
    {
        var seite = ZeichnenMitKaelte(MitKaelte());
        var waerme = seite.FindAll("section.epos-simerg-block")[0];
        var kaelte = seite.Find("section.epos-simerg-kaelte");

        foreach (var abschnitt in new[] { waerme, kaelte })
        {
            Assert.NotNull(abschnitt.QuerySelector(".epos-gruppenkopf"));
            var werte = abschnitt.QuerySelectorAll("dl.epos-simerg-werte");
            Assert.True(werte.Length >= 2);
            Assert.Contains("epos-simerg-abschluss", werte[0].QuerySelectorAll("dt").Last().ClassName ?? "");
            Assert.NotNull(abschnitt.QuerySelector("h3.epos-untergruppe"));
        }
        Assert.NotNull(kaelte.QuerySelector("svg.epos-flaeche"));
    }

    /// <summary>Der eine Schalter „sortiert" wechselt auch das Bild der Kältelast.</summary>
    [Fact]
    public void Der_Sortiertschalter_wechselt_auch_das_Kaeltebild()
    {
        var seite = ZeichnenMitKaelte(MitKaelte());

        _auftraege.Clear();
        seite.FindAll("input[type='checkbox']")[0].Change(true);

        Assert.Contains(_auftraege, a => a.Bild == Bilder.BedarfKaelte && a.Sortiert);
    }

    /// <summary>Der CSV-Knopf der Kälteseite erscheint nur mit Delegat und meldet seinen Klick.</summary>
    [Fact]
    public void Der_Kaelte_CSV_Knopf_meldet_seinen_Klick()
    {
        Assert.Empty(ZeichnenMitKaelte(MitKaelte()).FindAll("section.epos-simerg-kaelte button.epos-simerg-knopf"));

        int n = 0;
        var seite = ZeichnenMitKaelte(MitKaelte(), () => n++);
        seite.Find("section.epos-simerg-kaelte button.epos-simerg-knopf").Click();

        Assert.Equal(1, n);
    }
}
