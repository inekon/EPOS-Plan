using System;
using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Das DASHBOARD der Simulationsübersicht (Auftrag #222, SIM‑E‑3), Vorbild
/// <c>tabPage_Uebersicht</c> (R2) UND <c>NavigatorUebersicht</c> (428 Z. samt
/// 148 Zeilen GDI).
///
/// <para><b>Soll:</b> ZWEI Spalten (Wärme links, Strom rechts), je Spalte ein
/// Kopfband mit Abzeichen, drei Kennzahlen, der Ring mit HTML-Legende, die
/// Erzeugertabelle mit rechtsbündigen Köpfen und der Einheit im Kopf, der
/// Schalter für die Zeilen ohne Beitrag und der Weg in den Bedarf. Dazu die
/// zwei Sonderfälle: ohne Bedarf kein Ring (Befund W11‑B36), bei 0 % Deckung
/// der Hinweis auf die fehlenden Stromerzeuger.</para>
///
/// <para>Der Selektor nennt seit der Windows-Abnahme 05.09.2026 die Klasse
/// <c>epos-simerg-knopf</c>: Jedes Diagramm steht seither im Baustein
/// <c>Diagramm</c> und bringt seine eigenen Knöpfe mit. <c>FindAll("button")</c>
/// zählte die mit und prüfte damit nicht mehr, was der Fall behauptet — seit
/// #222 tragen die zwei Ringe allerdings gar keine Knöpfe mehr.</para>
/// </summary>
public class UebersichtReiterTests : EposBunitContext
{
    public UebersichtReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        // Die BESCHRIFTUNGEN folgen der Oberflaechensprache, die ZAHLEN der
        // Zahlenkultur — beide werden festgelegt (Regel seit W8).
    }

    // =====================================================================
    // Probendaten — ein Projekt mit Wärmepumpe, Heizstab und Heizkessel
    // =====================================================================

    /// <summary>
    /// Ein ECHTER Ring als Zeichenmodell (Etappe DG-E3, Gruppe (c)) — ohne Legende
    /// im Bild, wie die Hülle ihn baut: Sie steht hier als HTML daneben.
    ///
    /// <para>EINMAL je Fall gebaut und dann liegen gelassen: Der Baustein baut seinen
    /// Knotenbaum nur neu, wenn die REFERENZ des Modells wechselt.</para>
    /// </summary>
    private readonly Zeichenmodell _ring = ChartRenderer.RingModell(
        "Wärmebedarfsdeckung",
        new[]
        {
            new ChartRenderer.Ringsegment("Wärmepumpe", 300.0, ChartRenderer.C_WP),
            new ChartRenderer.Ringsegment("Rest", 180.0, ChartRenderer.C_KESSEL)
        },
        62.5, "%", "gedeckt", false);

    private static SimulationErgebnisCtrl.UebersichtKennzahlen Zahlen() =>
        new SimulationErgebnisCtrl.UebersichtKennzahlen
        {
            StrombedarfGesamtMwh = 120.5,
            WaermebedarfGesamtMwh = 480.25,
            RestwaermeMwh = 6.04,
            ReststromMwh = 30.0,
            WpWaermeproduktionMwh = 300.0,
            WpStromverbrauchMwh = 75.0,
            KesselWaermeproduktionMwh = 174.21,
            HeizstabStromverbrauchMwh = 2.5,
            KesselStromverbrauchMwh = 1.25,
            BhkwWaermeproduktionMwh = 0.0,
            BhkwStromproduktionMwh = 0.0,
            SolarWaermeproduktionMwh = 0.0,
            PvStromproduktionMwh = 0.0
        };

    /// <summary>Die Wärmetabelle der Probe: WP mit Beitrag, Heizstab ohne.</summary>
    private static Erzeugertabelle Waermetabelle() => new Erzeugertabelle
    {
        Spalten = new[]
        {
            new Tabellenkopf("Erzeuger"),
            new Tabellenkopf("Erzeugung", "MWh/a"),
            new Tabellenkopf("Heizung", "MWh/a")
        },
        Zeilen = new[]
        {
            new Erzeugerzeile("Wärmepumpe", new[] { "300,00", "280,00" }),
            new Erzeugerzeile("Heizstab", new[] { "0,00", "0,00" }, OhneBeitrag: true),
            new Erzeugerzeile("Heizkessel", new[] { "174,21", "160,00" })
        },
        Summe = new Erzeugerzeile("Summe Erzeuger", new[] { "474,21", "440,00" }),
        Rest = new Erzeugerzeile("Restwärmebedarf", new[] { "6,04", "—" })
    };

    private static Erzeugertabelle Stromtabelle(bool mitErzeuger) => new Erzeugertabelle
    {
        Spalten = new[]
        {
            new Tabellenkopf("Erzeuger"),
            new Tabellenkopf("Erzeugung", "MWh/a"),
            new Tabellenkopf("Anteil", "%")
        },
        Zeilen = mitErzeuger
            ? new[] { new Erzeugerzeile("Photovoltaik", new[] { "14,80", "12,3" }) }
            : System.Array.Empty<Erzeugerzeile>(),
        Rest = new Erzeugerzeile("Reststrombedarf", new[] { "30,00", "87,7" }),
        LeerText = mitErzeuger ? "" : "Keine Stromerzeuger im Projekt — die Tabelle erscheint mit dem ersten Erzeuger."
    };

    private static UebersichtDaten Daten(bool waermebedarf = true, bool strombedarf = true,
                                         bool stromerzeuger = true)
        => new UebersichtDaten
        {
            Waermepumpe = true,
            Heizstab = true,
            Heizkessel = true,
            WaermedeckungProzent = 98.7,
            StromdeckungProzent = stromerzeuger ? 12.3 : 0.0,
            WaermebedarfVorhanden = waermebedarf,
            StrombedarfVorhanden = strombedarf,
            WaermebedarfMwh = 480.25,
            StrombedarfMwh = 199.25,
            ReststromMwh = 30.0,
            RestwaermeMwh = 6.04,
            Kaskade = "Wärmepumpe → Heizkessel",
            StromerzeugerVorhanden = stromerzeuger,
            WaermeLegende = new[]
            {
                new Ringanteil("Wärmepumpe", 300.0, 62.5, "#2ECC71"),
                new Ringanteil("Heizkessel", 174.21, 36.3, "#95A5A6"),
                new Ringanteil("Rest (ungedeckt)", 6.04, 1.3, "#D9DEE5", IstRest: true)
            },
            StromLegende = stromerzeuger
                ? new[]
                {
                    new Ringanteil("Photovoltaik", 14.8, 12.3, "#2ECC71"),
                    new Ringanteil("Netzbezug (ungedeckt)", 30.0, 87.7, "#D9DEE5", IstRest: true)
                }
                : new[] { new Ringanteil("Netzbezug (ungedeckt)", 199.25, 100.0, "#D9DEE5", IstRest: true) },
            WaermeTabelle = Waermetabelle(),
            StromTabelle = Stromtabelle(stromerzeuger)
        };

    private IRenderedComponent<UebersichtReiter> Zeichnen(UebersichtDaten daten,
                                                          Action? details = null,
                                                          Action? strom = null)
        => Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, daten);
            p.Add(x => x.RingWaerme, _ring);
            p.Add(x => x.RingStrom, _ring);
            if (details is not null) p.Add(x => x.BedarfDetails, EventCallback.Factory.Create(this, details));
            if (strom is not null) p.Add(x => x.StromDetails, EventCallback.Factory.Create(this, strom));
        });

    // =====================================================================
    //  Zwei Spalten, eine Übersicht (#222, Entscheid a)
    // =====================================================================

    /// <summary>
    /// Das Dashboard führt GENAU ZWEI Spalten — links Wärme, rechts Strom, in
    /// derselben Ordnung wie bisher (W11b‑B‑15, W11b‑B‑12).
    /// </summary>
    [Fact]
    public void Das_Dashboard_fuehrt_zwei_Spalten_Waerme_und_Strom()
    {
        var seite = Zeichnen(Daten());
        var spalten = seite.FindAll("section.epos-simueb-spalte");
        var koepfe = seite.FindAll("h2.epos-gruppenkopf-titel");

        Assert.Equal(2, spalten.Count);
        Assert.Equal(2, koepfe.Count);
        Assert.Equal("Wärme", koepfe[0].TextContent.Trim());
        Assert.Equal("Strom", koepfe[1].TextContent.Trim());

        // Sie stehen NEBENEINANDER in EINER Rasterzeile.
        Assert.Single(seite.FindAll("div.epos-simueb-spalten"));
    }

    /// <summary>
    /// Die zwei Kennzahlengruppen des Vorläufers (13 Zeilen als
    /// <c>dl.epos-simerg-werte</c>) und sein Eigenanteilsraster
    /// (<c>table.epos-raster</c>) sind mit #222 gefallen — jede Zahl steht
    /// genau einmal.
    /// </summary>
    [Fact]
    public void Die_alten_Kennzahlenlisten_und_das_Eigenanteilsraster_sind_weg()
    {
        var seite = Zeichnen(Daten());

        Assert.Empty(seite.FindAll("dl.epos-simerg-werte"));
        Assert.Empty(seite.FindAll("table.epos-raster"));
        Assert.Empty(seite.FindAll("div.epos-kennzahlkachel"));
    }

    /// <summary>
    /// Je Spalte DREI Kennzahlen: Bedarf, Deckung durch Erzeuger, Rest — die
    /// letzte betont, denn sie ist das Ergebnis und keine weitere Zeile.
    /// </summary>
    [Fact]
    public void Jede_Spalte_fuehrt_drei_Kennzahlen_mit_betontem_Rest()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(6, seite.FindAll("div.epos-simueb-kennzahl").Count);
        Assert.Equal(2, seite.FindAll("div.epos-simueb-kennzahl--betont").Count);

        string text = seite.Markup;
        Assert.Contains("480,25", text);     // Wärmebedarf
        Assert.Contains("199,25", text);     // Strombedarf mit Eigenverbrauch
        Assert.Contains("98,7", text);       // Wärmedeckung
        Assert.Contains("6,04", text);       // Restwärme
        Assert.Contains("30,00", text);      // Reststrom
    }

    /// <summary>
    /// Das Abzeichen im Kopfband: bei der Wärme die KASKADE des Laufs, beim Strom
    /// nur dann ein Satz, wenn kein Stromerzeuger im Projekt steht.
    /// </summary>
    [Fact]
    public void Das_Kopfband_traegt_die_Kaskade_und_den_Strombefund()
    {
        var abzeichen = Zeichnen(Daten(stromerzeuger: false))
            .FindAll("span.epos-gruppenkopf-summe");

        Assert.Equal(2, abzeichen.Count);
        Assert.Equal("Kaskade: Wärmepumpe → Heizkessel", abzeichen[0].TextContent.Trim());
        Assert.Equal("kein Stromerzeuger im Projekt", abzeichen[1].TextContent.Trim());
    }

    /// <summary>Mit Stromerzeugern bleibt das zweite Abzeichen leer.</summary>
    [Fact]
    public void Mit_Stromerzeuger_steht_kein_Strombefund_im_Kopfband()
    {
        var seite = Zeichnen(Daten());

        Assert.DoesNotContain("kein Stromerzeuger im Projekt", seite.Markup);
        Assert.Single(seite.FindAll("span.epos-gruppenkopf-summe"));
    }

    /// <summary>
    /// KEINE ZAHL GEHT VERLOREN: Die drei Eigenverbräuche der Wärmeerzeuger — der
    /// Nenner des Stromrings — stehen als leise Zeile unter den Kennzahlen der
    /// Stromspalte. Sie sind die einzigen der 13 Vorläuferzahlen, die in keinem
    /// Erzeugerreiter wieder vorkommen.
    /// </summary>
    [Fact]
    public void Die_Eigenverbraeuche_der_Waermeerzeuger_bleiben_stehen()
    {
        var zeile = Zeichnen(Daten()).Find("p.epos-simueb-eigenverbrauch").TextContent;

        Assert.Contains("75,00", zeile);     // Wärmepumpe
        Assert.Contains("2,50", zeile);      // Heizstab
        Assert.Contains("1,25", zeile);      // Spitzenkessel
        // E29 (#536): ohne Kälte der Bestandssatz, kein Kältestrom.
        Assert.StartsWith("davon Eigenverbrauch der Wärmeerzeuger:", zeile.Trim());
        Assert.DoesNotContain("Kältestrom", zeile);
    }

    /// <summary>
    /// E29 (#536, Befund N6, E29‑Q9 a): Mit Kälte der Stufenrechnung nennt die Zeile den
    /// Kältestrom als vierten Eigenverbrauch — in der Reihenfolge des Nenners, hinter dem
    /// Kessel — und der Satz spricht von Wärme- und Kälteerzeugern.
    /// </summary>
    [Fact]
    public void Mit_Kaelte_nennt_die_Eigenverbrauchszeile_den_Kaeltestrom()
    {
        var k = Zahlen();
        k.KaeltestromStufeMwh = 4.5;
        var zeile = Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, k);
            p.Add(x => x.Daten, Daten());
            p.Add(x => x.RingWaerme, _ring);
            p.Add(x => x.RingStrom, _ring);
        }).Find("p.epos-simueb-eigenverbrauch").TextContent.Trim();

        Assert.StartsWith("davon Eigenverbrauch der Wärme- und Kälteerzeuger:", zeile);
        Assert.Contains("Kältestrom 4,50", zeile);
        Assert.True(zeile.IndexOf("1,25", StringComparison.Ordinal) < zeile.IndexOf("Kältestrom", StringComparison.Ordinal),
                    "Der Kältestrom steht hinter dem Kessel: " + zeile);
    }

    // =====================================================================
    //  Die Ringe (#222, Entscheid b und c)
    // =====================================================================

    /// <summary>Zwei Ringe, je einer in seiner Spalte — und beide rund bemessen.</summary>
    [Fact]
    public void Jede_Spalte_traegt_ihren_Ring()
    {
        var seite = Zeichnen(Daten());

        Assert.Empty(seite.FindAll("img"));
        Assert.Equal(2, seite.FindAll("div.epos-simueb-ring").Count);

        // ZWEI RINGE ALS SVG (Etappe DG-E3, Gruppe (c)) - und ZWEI Kennungen: Sie
        // bilden die clipPath-Namen, und mit derselben schnitte das eine Bild am
        // Rechteck des anderen.
        var ringe = seite.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>();
        Assert.Equal(2, ringe.Count);
        Assert.Equal(new[] { "simerg-ring-waerme", "simerg-ring-strom" },
                     ringe.Select(r => r.Instance.Kennung).ToArray());
    }

    /// <summary>
    /// <b>Am Ring ist die Legende NICHT schaltbar</b> (Entscheid der Etappe DG-E3):
    /// Ein Kreis, dessen Segment fehlt, ist kein Kreis mehr — die Anteile ergänzen
    /// sich zu 100 %. Der WERT am Zeiger bleibt: Er ist das, was der Ring statt des
    /// Zooms anbietet (DG-E3-10).
    /// </summary>
    [Fact]
    public void Am_Ring_ist_die_Legende_nicht_schaltbar()
    {
        var ring = Zeichnen(Daten()).FindComponents<EPOS.UI.Bausteine.DiagrammSvg>()[0]
                                    .Instance;

        Assert.False(ring.LegendeSchaltbar);
        Assert.True(ring.ZeigtWertAmElement);
    }

    /// <summary>
    /// <b>Entscheid c (#222): KEINE ZOOMLEISTE AN RINGEN.</b> Die Leiste
    /// „×1 · 1:1" bleibt Zeitreihen und Balken vorbehalten; an einem Ring war sie
    /// Bedienfläche ohne Gegenwert, und auf ×1,2 schnitt der Rahmen den Kreis an.
    ///
    /// <para>Seit der Etappe DG-E3 steht das NICHT mehr am Aufrufer: Ein Modell ohne
    /// Zeichenfläche hat keine Datenkoordinaten, und der Baustein lässt die Leiste
    /// von selbst weg — <c>OhneZoom</c> setzt der Reiter nicht. Der RAHMEN bleibt.</para>
    /// </summary>
    [Fact]
    public void Die_Ringe_tragen_keine_Zoomleiste()
    {
        var seite = Zeichnen(Daten());

        Assert.Empty(seite.FindAll("div.epos-diagramm-leiste"));
        Assert.Empty(seite.FindAll("button.epos-diagramm-knopf"));
        Assert.All(seite.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>(),
                   r => Assert.False(r.Instance.OhneZoom));
        Assert.Equal(2, seite.FindAll("div.epos-diagramm-svg-flaeche").Count);
    }

    /// <summary>
    /// <b>Die Legende ist HTML.</b> Je Segment ein Farbkästchen, der Name, die
    /// Menge in MWh und der Anteil in Prozent — die zwei Zahlen aus W11b‑B‑11,
    /// jetzt kopierbar statt als Rastergrafik im Bild. Den Abschluss macht die
    /// Summenzeile mit dem Bedarf und 100 %.
    /// </summary>
    [Fact]
    public void Die_Legende_steht_als_HTML_mit_MWh_und_Prozent()
    {
        var seite = Zeichnen(Daten());
        var waerme = seite.FindAll("ul.epos-simueb-legende")[0];
        var zeilen = waerme.QuerySelectorAll("li");

        Assert.Equal(4, zeilen.Length);               // drei Segmente plus Summe
        Assert.Contains("Wärmepumpe", zeilen[0].TextContent);
        Assert.Contains("300,00 MWh", zeilen[0].TextContent);
        Assert.Contains("62,5 %", zeilen[0].TextContent);

        // Das Farbkaestchen traegt die Segmentfarbe des Bildes.
        Assert.Contains("#2ECC71",
                        zeilen[0].QuerySelector("span.epos-simueb-farbe")!.GetAttribute("style") ?? "");

        // Der Rest steht abgesetzt, die Summe schliesst ab.
        Assert.Single(waerme.QuerySelectorAll("li.epos-simueb-legende-rest"));
        Assert.Contains("480,25 MWh", zeilen[3].TextContent);
        Assert.Contains("100,0 %", zeilen[3].TextContent);
    }

    /// <summary>
    /// <b>Der 0‑%‑Fall (Entscheid b).</b> Ohne Stromerzeuger steht der Ring
    /// trotzdem da — als grauer Vollring, denn der ungedeckte Rest ist immer ein
    /// Segment —, die Legende nennt den Netzbezug mit 100 %, und darunter steht
    /// der Weg in die Konfiguration.
    /// </summary>
    [Fact]
    public void Bei_null_Prozent_steht_der_Hinweis_auf_die_Konfiguration()
    {
        var seite = Zeichnen(Daten(stromerzeuger: false));

        // Der Stromring bleibt stehen.
        Assert.Equal(2, seite.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>().Count);
        var hinweis = seite.Find("p.epos-simueb-leerhinweis").TextContent;
        Assert.Contains("Kein Stromerzeuger in der Kaskade", hinweis);
        Assert.Contains("① Konfiguration", hinweis);

        var strom = seite.FindAll("ul.epos-simueb-legende")[1];
        Assert.Contains("Netzbezug (ungedeckt)", strom.TextContent);
        Assert.Contains("100,0 %", strom.TextContent);
    }

    /// <summary>Die Gegenprobe: Mit Deckung steht der Hinweis nicht da.</summary>
    [Fact]
    public void Mit_Deckung_steht_kein_Leerhinweis()
    {
        Assert.Empty(Zeichnen(Daten()).FindAll("p.epos-simueb-leerhinweis"));
    }

    /// <summary>
    /// Befund W11‑B36: Ohne Bedarf steht kein Ring, sondern der Satz dazu — der
    /// Vorlaeufer setzte den Mittelwert hart auf 100 %.
    /// </summary>
    [Fact]
    public void Ohne_Bedarf_steht_kein_Ring()
    {
        var seite = Zeichnen(Daten(waermebedarf: false, strombedarf: false));

        Assert.Empty(seite.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>());
        Assert.Equal(2, seite.FindAll("p.epos-simerg-hinweis").Count);
        Assert.Empty(seite.FindAll("ul.epos-simueb-legende"));
    }

    // =====================================================================
    //  Die Erzeugertabelle (#222, Punkt 4)
    // =====================================================================

    /// <summary>
    /// <b>Anwenderbefund 11.09.2026:</b> „Zahlenspalte unter der Überschrift ist
    /// ungünstig." Die erste Spalte trägt den Namen und steht links; jede weitere
    /// ist eine Zahlenspalte, und ihr Kopf steht RECHTSbündig über ihr. Die
    /// Einheit steht einmal im Kopf, als zweite Zeile.
    /// </summary>
    [Fact]
    public void Die_Zahlenkoepfe_stehen_rechts_und_tragen_die_Einheit()
    {
        var seite = Zeichnen(Daten());
        var koepfe = seite.FindAll("table.epos-simueb-tabelle")[0].QuerySelectorAll("thead th");

        Assert.Equal(3, koepfe.Length);
        Assert.Contains("epos-simueb-name", koepfe[0].ClassName ?? "");
        Assert.Null(koepfe[1].ClassName);                     // eine Zahlenspalte, keine Namensspalte
        Assert.Equal("MWh/a", koepfe[1].QuerySelector("span.epos-simueb-kopfeinheit")!.TextContent);

        // Die Einheit steht NICHT in den Zellen.
        Assert.DoesNotContain("300,00 MWh/a", seite.Markup);
    }

    /// <summary>Summen- und Restzeile schließen die Tabelle betont ab.</summary>
    [Fact]
    public void Summe_und_Rest_schliessen_die_Tabelle_ab()
    {
        var seite = Zeichnen(Daten());
        var waerme = seite.FindAll("table.epos-simueb-tabelle")[0];

        Assert.Single(waerme.QuerySelectorAll("tr.epos-simueb-summe"));
        Assert.Single(waerme.QuerySelectorAll("tr.epos-simueb-rest"));
        Assert.Contains("Summe Erzeuger", waerme.QuerySelector("tr.epos-simueb-summe")!.TextContent);
        Assert.Contains("Restwärmebedarf", waerme.QuerySelector("tr.epos-simueb-rest")!.TextContent);
    }

    /// <summary>
    /// Eine Zeile OHNE BEITRAG steht gedimmt da — sie verschwindet nicht von
    /// selbst, denn eine angelegte Anlage mit 0,00 ist eine Aussage (#190).
    /// </summary>
    [Fact]
    public void Zeilen_ohne_Beitrag_stehen_gedimmt_und_bleiben_zunaechst_stehen()
    {
        var seite = Zeichnen(Daten());
        var waerme = seite.FindAll("table.epos-simueb-tabelle")[0];

        Assert.Equal(3, waerme.QuerySelectorAll("tbody tr:not(.epos-simueb-summe):not(.epos-simueb-rest)").Length);
        Assert.Single(waerme.QuerySelectorAll("tr.epos-simueb-null"));
        Assert.True(seite.Instance.NullzeilenSichtbar(true));
    }

    /// <summary>
    /// Der SCHALTER blendet sie aus und wieder ein; er merkt sich den Stand, und
    /// er steht nur da, wo es überhaupt eine Nullzeile gibt.
    /// </summary>
    [Fact]
    public void Der_Schalter_blendet_die_Nullzeilen_aus_und_wieder_ein()
    {
        var seite = Zeichnen(Daten());
        var schalter = seite.FindAll("button.epos-simueb-schalter");

        // Nur die Waermespalte fuehrt eine Zeile ohne Beitrag.
        Assert.Single(schalter);
        Assert.Equal("1 Zeilen ohne Beitrag ausblenden", schalter[0].TextContent.Trim());

        schalter[0].Click();
        Assert.False(seite.Instance.NullzeilenSichtbar(true));
        Assert.Empty(seite.FindAll("tr.epos-simueb-null"));
        Assert.Equal("1 Zeilen ohne Beitrag einblenden",
                     seite.Find("button.epos-simueb-schalter").TextContent.Trim());

        seite.Find("button.epos-simueb-schalter").Click();
        Assert.True(seite.Instance.NullzeilenSichtbar(true));
        Assert.Single(seite.FindAll("tr.epos-simueb-null"));
    }

    /// <summary>
    /// Ohne Stromerzeuger steht statt der Zeilen der Satz, dass die Tabelle mit
    /// dem ersten Erzeuger erscheint — die Restzeile bleibt.
    /// </summary>
    [Fact]
    public void Ohne_Stromerzeuger_traegt_die_Stromtabelle_ihren_Leersatz()
    {
        var strom = Zeichnen(Daten(stromerzeuger: false))
            .FindAll("table.epos-simueb-tabelle")[1];

        Assert.Single(strom.QuerySelectorAll("td.epos-simueb-leer"));
        Assert.Contains("Keine Stromerzeuger im Projekt", strom.TextContent);
        Assert.Single(strom.QuerySelectorAll("tr.epos-simueb-rest"));
    }

    /// <summary>
    /// #190: Steht ein Erzeuger des Projekts auf keinem Platz der Simulation,
    /// trägt seine Zeile den Zusatz „(nicht in der Kaskade)".
    /// </summary>
    [Fact]
    public void Ein_Erzeuger_ohne_Kaskadenplatz_traegt_den_Zusatz()
    {
        UebersichtDaten daten = Daten();
        daten.WaermeTabelle.Zeilen = new[]
        {
            new Erzeugerzeile("Heizkessel", new[] { "0,00", "0,00" }, OhneBeitrag: true,
                              Zusatz: " (nicht in der Kaskade)")
        };

        var seite = Zeichnen(daten);

        Assert.Contains("nicht in der Kaskade",
                        seite.Find("span.epos-simueb-zusatz").TextContent);
    }

    /// <summary>Die Gegenprobe: Ohne Lücke trägt keine Zeile den Zusatz.</summary>
    [Fact]
    public void Ohne_Luecke_traegt_keine_Zeile_den_Zusatz()
    {
        Assert.DoesNotContain("nicht in der Kaskade", Zeichnen(Daten()).Markup);
    }

    // =====================================================================
    //  Die Meldung wird handlungsfähig (Anwenderentscheid vom 16.09.2026)
    // =====================================================================

    private static UebersichtDaten MitAngebot(bool moeglich, string sperrgrund = "")
    {
        UebersichtDaten daten = Daten();
        daten.OhnePlatzAngebote = new[]
        {
            new Platzangebot(4711, "LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ",
                             "Wärmepumpe „WP 1“ ist im Projekt angelegt, aber nicht in der Kaskade.",
                             moeglich, sperrgrund)
        };
        return daten;
    }

    /// <summary>
    /// Ist ein Platz frei, trägt die Meldung einen Knopf — und er meldet die ANLAGE,
    /// nicht die Erzeugerart: Die Konfigurationsseite hebt damit genau eine Karte hervor.
    /// </summary>
    [Fact]
    public void Ein_Erzeuger_ohne_Platz_bekommt_einen_Knopf_mit_der_Anlagennummer()
    {
        int gemeldet = 0;
        var seite = Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, MitAngebot(true));
            p.Add(x => x.Aufnehmen, EventCallback.Factory.Create<int>(this, id => gemeldet = id));
        });

        var knopf = seite.Find("button.epos-warnbanner-aktion");
        Assert.Contains("Konfiguration", knopf.TextContent);

        knopf.Click();
        Assert.Equal(4711, gemeldet);
    }

    /// <summary>
    /// Ist KEIN Platz frei, steht kein Knopf da, sondern der Grund — ein Knopf, der
    /// nichts täte, wäre schlimmer als keiner.
    /// </summary>
    [Fact]
    public void Ohne_freien_Platz_steht_der_Grund_statt_eines_Knopfes()
    {
        var seite = Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, MitAngebot(false, "Die vier Plätze der Kaskade sind belegt."));
            p.Add(x => x.Aufnehmen, EventCallback.Factory.Create<int>(this, _ => { }));
        });

        Assert.Empty(seite.FindAll("button.epos-warnbanner-aktion"));
        Assert.Contains("Die vier Plätze der Kaskade sind belegt.",
                        seite.Find("span.epos-warnbanner-text").TextContent);
    }

    /// <summary>
    /// Ohne eingelegten Weg gibt es keinen Knopf (Hausregel „kein Delegat, kein Knopf")
    /// — die Meldung selbst steht trotzdem.
    /// </summary>
    [Fact]
    public void Ohne_Aufnahmeweg_bleibt_die_Meldung_ohne_Knopf()
    {
        var seite = Zeichnen(MitAngebot(true));

        Assert.Empty(seite.FindAll("button.epos-warnbanner-aktion"));
        Assert.Contains("nicht in der Kaskade", seite.Find("span.epos-warnbanner-text").TextContent);
    }

    /// <summary>Die Gegenprobe: Ohne Angebot steht kein Banner über dem Dashboard.</summary>
    [Fact]
    public void Ohne_Angebot_steht_kein_Banner()
    {
        Assert.Empty(Zeichnen(Daten()).FindAll("div.epos-warnbanner"));
    }

    /// <summary>
    /// BEDARFSKANAL OHNE VERSORGER: Jeder Satz der Hülle steht als WARNBANNER über dem
    /// Dashboard — derselbe Satz wie im Laufprotokoll. Einen Knopf trägt er nicht: Der
    /// Handgriff ist die Senkenzuordnung einer Anlage, und welche das sein soll,
    /// entscheidet der Anwender.
    /// </summary>
    [Fact]
    public void Ein_Kanal_ohne_Versorger_steht_als_Warnbanner_ueber_dem_Dashboard()
    {
        UebersichtDaten daten = Daten();
        daten.KanaeleOhneVersorger = new[]
        {
            "Kanal Prozesswärme mit 50,0 MWh/a Bedarf hat keinen Versorger: keine Anlage " +
            "trägt eine Senke für diesen Kanal. Senken im Anlagendialog zuordnen."
        };

        var seite = Zeichnen(daten);

        var banner = seite.Find("div.epos-warnbanner");
        Assert.Contains("epos-warnbanner--warnung", banner.ClassName);
        Assert.Contains("Kanal Prozesswärme mit 50,0 MWh/a", banner.TextContent);
        Assert.Empty(seite.FindAll("button.epos-warnbanner-aktion"));
    }

    // =====================================================================
    //  Der Fuß
    // =====================================================================

    /// <summary>Kein Rueckruf = kein Knopf (Hausregel seit W2).</summary>
    [Fact]
    public void Ohne_Rueckruf_bleiben_die_Fussknoepfe_weg()
    {
        var seite = Zeichnen(Daten());
        Assert.Empty(seite.FindAll("button.epos-simerg-knopf"));
    }

    /// <summary>
    /// Jede Spalte bekommt ihren eigenen Weg in den Bedarf — die Wärmespalte
    /// „Wärmebedarf Übersicht…", die Stromspalte ihr Gegenstück (#222).
    /// </summary>
    [Fact]
    public void Jede_Spalte_meldet_ihren_eigenen_Bedarfsknopf()
    {
        int waerme = 0, strom = 0;
        var seite = Zeichnen(Daten(), details: () => waerme++, strom: () => strom++);
        var knoepfe = seite.FindAll("button.epos-simerg-knopf");

        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Wärmebedarf Übersicht...", knoepfe[0].TextContent.Trim());
        Assert.Equal("Strombedarf Übersicht...", knoepfe[1].TextContent.Trim());

        knoepfe[0].Click();
        seite.FindAll("button.epos-simerg-knopf")[1].Click();

        Assert.Equal(1, waerme);
        Assert.Equal(1, strom);
    }

    /// <summary>Nur der Wärmerückruf: nur ein Knopf, und zwar seiner.</summary>
    [Fact]
    public void Ohne_Stromrueckruf_bleibt_nur_der_Waermeknopf()
    {
        var seite = Zeichnen(Daten(), details: () => { });

        Assert.Single(seite.FindAll("button.epos-simerg-knopf"));
        Assert.Equal("Wärmebedarf Übersicht...",
                     seite.Find("button.epos-simerg-knopf").TextContent.Trim());
    }
    // =====================================================================
    //  Der LEERZUSTAND (Auftrag #236, Anwenderrückmeldung 12.09.2026)
    // =====================================================================

    /// <summary>Die Bedarfsrechnung des gemeldeten Projekts (Ganglinie, kein Wärmebedarf).</summary>
    private static BedarfDaten Bedarfszahlen() => new BedarfDaten
    {
        WaermebedarfGesamtMwh = 0.0,
        StrombedarfGesamtMwh = 2850.2
    };

    private IRenderedComponent<UebersichtReiter> ZeichnenOhneErgebnis(
        ErgebnisZustand zustand, string text = "", UebersichtDaten? daten = null)
        => Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Zustand, zustand);
            p.Add(x => x.Zustandstext, text);
            p.Add(x => x.Bedarf, Bedarfszahlen());
        });

    private static string Zahl(double wert)
        => wert.ToString("N2", CultureInfo.CurrentCulture);

    /// <summary>
    /// <b>DER BEFUND #236.</b> Ohne gerechneten Lauf zeichnet der Reiter kein
    /// Ergebnis: kein Ring, keine Deckung, keine Erzeugertabelle — und vor allem
    /// nicht die Marke „kein Stromerzeuger im Projekt" über lauter Nullen. Der
    /// STROMBEDARF steht trotzdem da, und zwar mit der Zahl der Bedarfsrechnung.
    /// </summary>
    [Fact]
    public void Ohne_Ergebnis_steht_der_Bedarf_und_keine_Deckung()
    {
        var seite = ZeichnenOhneErgebnis(ErgebnisZustand.NichtGerechnet);

        // Die zwei Spalten stehen, aber jede trägt nur EINE Kennzahl.
        Assert.Equal(2, seite.FindAll("section.epos-simueb-spalte").Count);
        Assert.Equal(2, seite.FindAll(".epos-simueb-kennzahl").Count);

        // Die Zahl der Bedarfsrechnung — nicht 0,00.
        Assert.Contains(Zahl(2850.2), seite.Markup, StringComparison.Ordinal);

        // Nichts, was ein Ergebnis behaupten würde.
        Assert.Empty(seite.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>());
        Assert.Empty(seite.FindAll("ul.epos-simueb-legende"));
        Assert.Empty(seite.FindAll("table.epos-simueb-tabelle"));
        Assert.DoesNotContain(Resource.SIMUEB_BADGE_OHNE_STROMERZEUGER, seite.Markup,
                              StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.SIMERG_MSG_OHNE_BEDARF, seite.Markup,
                              StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.SIMUEB_LBL_DECKUNG, seite.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// An der Stelle des Ergebnisses steht EINE ruhige Karte mit dem Grund — kein
    /// Warnbanner (Regel W16b‑E‑6, dritte Stufe).
    /// </summary>
    [Fact]
    public void Der_Leerzustand_nennt_seinen_Grund_in_einer_ruhigen_Karte()
    {
        const string satz = "Das Ergebnis ist veraltet — die Speicher-Einstellungen wurden geändert.";
        var seite = ZeichnenOhneErgebnis(ErgebnisZustand.Veraltet, satz);

        var karte = seite.Find("p.epos-simueb-leerkarte");
        Assert.Equal(satz, karte.TextContent.Trim());
        Assert.Empty(seite.FindAll(".epos-warnbanner"));
    }

    /// <summary>Ohne eigenen Satz steht der allgemeine.</summary>
    [Fact]
    public void Ohne_Satz_steht_der_allgemeine_Leerzustandstext()
    {
        var seite = ZeichnenOhneErgebnis(ErgebnisZustand.NichtGerechnet);

        Assert.Equal(Resource.SIMERG_ZUSTAND_NICHT_GERECHNET,
                     seite.Find("p.epos-simueb-leerkarte").TextContent.Trim());
    }

    /// <summary>
    /// <b>DIE WACHE.</b> Auch ein VORBELEGTES <c>UebersichtDaten</c> — genau das, was
    /// die Hülle bis #236 stehen ließ — wird bei ungültigem Zustand nicht gezeichnet:
    /// kein „0,00 MWh/a" an der Stelle des Strombedarfs, keine Marke, keine 0,0 %.
    /// </summary>
    [Fact]
    public void Ein_vorbelegtes_Nullobjekt_wird_nicht_als_Ergebnis_gezeichnet()
    {
        var seite = ZeichnenOhneErgebnis(ErgebnisZustand.Veraltet,
                                         daten: new UebersichtDaten());

        // Die STROMSPALTE zeigt die Zahl der Bedarfsrechnung, nicht die 0,00 des
        // vorbelegten DTO — das war das Bildschirmfoto des Anwenders.
        var werte = seite.FindAll(".epos-simueb-kennzahl-wert");
        Assert.Equal(2, werte.Count);
        Assert.StartsWith(Zahl(2850.2), werte[1].TextContent.Trim(), StringComparison.Ordinal);

        Assert.DoesNotContain(Resource.SIMUEB_BADGE_OHNE_STROMERZEUGER, seite.Markup,
                              StringComparison.Ordinal);
        Assert.Single(seite.FindAll("p.epos-simueb-leerkarte"));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Mit gültigem Zustand steht das Dashboard unverändert — Ring,
    /// Legende, Tabelle und Marke; die Leerkarte gibt es dann nicht.
    /// </summary>
    [Fact]
    public void Mit_gueltigem_Zustand_steht_das_Dashboard_unveraendert()
    {
        var seite = Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, Daten(stromerzeuger: false));
            p.Add(x => x.Zustand, ErgebnisZustand.Gueltig);
            p.Add(x => x.Bedarf, Bedarfszahlen());
            p.Add(x => x.RingWaerme, _ring);
            p.Add(x => x.RingStrom, _ring);
        });

        Assert.Empty(seite.FindAll("p.epos-simueb-leerkarte"));
        Assert.Equal(6, seite.FindAll(".epos-simueb-kennzahl").Count);
        Assert.Equal(2, seite.FindAll("ul.epos-simueb-legende").Count);
        Assert.Contains(Resource.SIMUEB_BADGE_OHNE_STROMERZEUGER, seite.Markup,
                        StringComparison.Ordinal);
    }

    // =====================================================================
    //  Stufe KU1 — der Block „Kältedeckung" unter den zwei Spalten (Kühlkonzept 8.4)
    // =====================================================================

    private IRenderedComponent<UebersichtReiter> ZeichnenMitKaelte(KaelteDaten? kaelte)
        => Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, Daten());
            p.Add(x => x.RingWaerme, _ring);
            p.Add(x => x.RingStrom, _ring);
            p.Add(x => x.Bedarf, new BedarfDaten { Kaelte = kaelte });
        });

    private static KaelteDaten Kaelte(double mwh) => new KaelteDaten
    {
        KaeltebedarfMwh = mwh,
        KaeltelastMaxKw = 8.25,
        KaelterestbedarfMwh = mwh,
        GrenzeFeuchte = WindowsFormsApplication1.SimulationKaeltebedarf.GrenzeFeuchte,
        Deckungshinweis = "Kein Kälteerzeuger im Projekt."
    };

    /// <summary>
    /// Die Kälte ist kein drittes Spaltenpaar, sondern ein Block UNTER den beiden Spalten:
    /// Kältebedarf, Kältespitze und die ungedeckte Kälte als betonte Restzahl, darunter der
    /// Satz zur Deckung und die Grenze der Zahl (K5). Die zwei Spalten bleiben, wie sie sind.
    /// </summary>
    [Fact]
    public void Mit_Kaeltebedarf_steht_der_Block_Kaeltedeckung_unter_den_Spalten()
    {
        var seite = ZeichnenMitKaelte(Kaelte(12.5));

        var block = seite.Find("section.epos-simueb-kaelte");
        Assert.Contains(Resource.SIMUEB_GRP_KAELTE, block.TextContent, StringComparison.Ordinal);
        Assert.Equal(3, block.QuerySelectorAll(".epos-simueb-kennzahl").Length);
        Assert.Contains("12,50", block.QuerySelector(".epos-simueb-kennzahl--betont")!.TextContent);
        Assert.Contains("Kein Kälteerzeuger im Projekt.", block.TextContent);
        Assert.Contains(WindowsFormsApplication1.SimulationKaeltebedarf.GrenzeFeuchte, block.TextContent);

        Assert.Equal(2, seite.FindAll(".epos-simueb-spalten > section").Count);
        Assert.Null(seite.Find(".epos-simueb-spalten").QuerySelector("section.epos-simueb-kaelte"));
    }

    /// <summary>Ohne erhobene Kälte, und bei Kältebedarf 0, bleibt das Dashboard unverändert.</summary>
    [Fact]
    public void Ohne_Kaeltebedarf_bleibt_das_Dashboard_unveraendert()
    {
        Assert.Empty(ZeichnenMitKaelte(null).FindAll("section.epos-simueb-kaelte"));
        Assert.Empty(ZeichnenMitKaelte(Kaelte(0.0)).FindAll("section.epos-simueb-kaelte"));
    }

    // =====================================================================
    //  Stufe KU2 Welle 3 — die Deckung im Kälteblock (Kühlkonzept 8.4; E21, E34)
    // =====================================================================

    /// <summary>Phantasiewerte: 12,5 MWh/a Kältebedarf, davon 5 MWh/a gedeckt mit 1,25 MWh/a Kältestrom.</summary>
    private static KaelteDaten KaelteMitErzeuger()
    {
        KaelteDaten k = Kaelte(12.5);
        k.KaelterestbedarfMwh = 7.5;
        k.Deckungshinweis = "Die Wärmepumpen im Kühlbetrieb decken 5,00 MWh/a des Kältebedarfs (40,0 %).";
        k.DeckungsgradProzent = 40.0;
        k.KaeltestromMwh = 1.25;
        k.EerJahreswert = 4.0;
        k.KaeltestromNetzbezugMwh = 0.8;
        k.Erzeuger = new[]
        {
            new KaelteerzeugerAnzeige("WP Probe", 18, 5.0, 1.25, 4.0, 0.8, "„Kühlstrom“, anteilig am Netzbezug")
        };
        k.Legende = new[]
        {
            new Ringanteil("WP Probe", 5.0, 40.0, "#2ECC71"),
            new Ringanteil("Rest (ungedeckt)", 7.5, 60.0, "#A0A0A0", IstRest: true)
        };
        return k;
    }

    private IRenderedComponent<UebersichtReiter> ZeichnenMitKaelteerzeuger(Zeichenmodell? ring)
        => Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, Daten());
            p.Add(x => x.RingWaerme, _ring);
            p.Add(x => x.RingStrom, _ring);
            p.Add(x => x.RingKaelte, ring);
            p.Add(x => x.Bedarf, new BedarfDaten { Kaelte = KaelteMitErzeuger() });
        });

    /// <summary>
    /// Mit Kälteerzeuger trägt der Block die Deckung nach dem Muster der zwei Spalten: Deckungsgrad,
    /// Kältestrom und Jahresarbeitszahl Kälte als zweites Kennzahlenband, die leise Zeile mit dem
    /// Netzbezug des Kältestroms, den Ring mit HTML-Legende (eigene Kennung) und die
    /// Kälteerzeugertabelle mit dem Stromträger je Anlage (E34); der Satz zur Deckung und die
    /// Grenze der Zahl (K5) bleiben darunter.
    /// </summary>
    [Fact]
    public void Mit_Kaelteerzeuger_zeigt_der_Block_Deckung_Ring_und_Kaelteerzeugertabelle()
    {
        var seite = ZeichnenMitKaelteerzeuger(_ring);
        var block = seite.Find("section.epos-simueb-kaelte");

        Assert.Equal(6, block.QuerySelectorAll(".epos-simueb-kennzahl").Length);
        string text = block.TextContent;
        Assert.Contains(Resource.SIMUEB_LBL_DECKUNG, text, StringComparison.Ordinal);
        Assert.Contains(Resource.SIMUEB_LBL_KAELTESTROM, text, StringComparison.Ordinal);
        Assert.Contains(Resource.SIMUEB_LBL_JAZ_KAELTE, text, StringComparison.Ordinal);
        Assert.Contains("40,0", text);
        Assert.Contains(string.Format(Resource.SIMUEB_KAELTESTROM_NETZ, "0,80"), text, StringComparison.Ordinal);

        // Der Ring mit eigener Kennung und die HTML-Legende daneben.
        Assert.Single(block.QuerySelectorAll(".epos-simueb-ring"));
        var ringe = seite.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>();
        Assert.Equal(new[] { "simerg-ring-waerme", "simerg-ring-strom", "simerg-ring-kaelte" },
                     ringe.Select(r => r.Instance.Kennung).ToArray());
        Assert.False(ringe[2].Instance.LegendeSchaltbar);
        Assert.Equal(2, block.QuerySelectorAll("ul.epos-simueb-legende li").Length);
        Assert.Single(block.QuerySelectorAll("li.epos-simueb-legende-rest"));

        // Die Kälteerzeugertabelle: sieben Spalten, eine Zeile, Stromträger als Text hinten.
        Assert.Equal(7, block.QuerySelectorAll(".epos-simueb-kaeltetabelle thead th").Length);
        var zeile = Assert.Single(block.QuerySelectorAll(".epos-simueb-kaeltetabelle tbody tr"));
        string[] zellen = zeile.QuerySelectorAll("td").Select(z => z.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "WP Probe", "18", "5,00", "1,25", "4,00", "0,80", "„Kühlstrom“, anteilig am Netzbezug" }, zellen);

        Assert.Contains("Die Wärmepumpen im Kühlbetrieb decken", text, StringComparison.Ordinal);
        Assert.Contains(WindowsFormsApplication1.SimulationKaeltebedarf.GrenzeFeuchte, text, StringComparison.Ordinal);
        Assert.Equal(2, seite.FindAll(".epos-simueb-spalten > section").Count);
    }

    /// <summary>Ohne Ringmodell (kein gültiges Ergebnis) fällt nur der Ring weg — Zahlen und Tabelle bleiben.</summary>
    [Fact]
    public void Mit_Kaelteerzeuger_ohne_Ringmodell_bleiben_Zahlen_und_Tabelle()
    {
        var block = ZeichnenMitKaelteerzeuger(null).Find("section.epos-simueb-kaelte");
        Assert.Empty(block.QuerySelectorAll(".epos-simueb-ring"));
        Assert.Equal(6, block.QuerySelectorAll(".epos-simueb-kennzahl").Length);
        Assert.Single(block.QuerySelectorAll(".epos-simueb-kaeltetabelle tbody tr"));
    }

    /// <summary>Ohne Kälteerzeuger steht im Kälteblock weder Deckungsband noch Ring noch Tabelle.</summary>
    [Fact]
    public void Ohne_Kaelteerzeuger_steht_im_Kaelteblock_weder_Ring_noch_Tabelle()
    {
        var block = ZeichnenMitKaelte(Kaelte(12.5)).Find("section.epos-simueb-kaelte");
        Assert.Equal(3, block.QuerySelectorAll(".epos-simueb-kennzahl").Length);
        Assert.Empty(block.QuerySelectorAll(".epos-simueb-ring"));
        Assert.Empty(block.QuerySelectorAll(".epos-simueb-kaeltetabelle"));
        Assert.DoesNotContain(Resource.SIMUEB_LBL_JAZ_KAELTE, block.TextContent, StringComparison.Ordinal);
    }
}
