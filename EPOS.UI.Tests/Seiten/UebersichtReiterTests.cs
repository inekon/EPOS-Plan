using System;
using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
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

    private static readonly byte[] BILD = { 137, 80, 78, 71 };

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
            p.Add(x => x.RingWaerme, BILD);
            p.Add(x => x.RingStrom, BILD);
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
    }

    // =====================================================================
    //  Die Ringe (#222, Entscheid b und c)
    // =====================================================================

    /// <summary>Zwei Ringe, je einer in seiner Spalte — und beide rund bemessen.</summary>
    [Fact]
    public void Jede_Spalte_traegt_ihren_Ring()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(2, seite.FindAll("img").Count);
        Assert.Equal(2, seite.FindAll("div.epos-diagramm--rund img").Count);
        Assert.Equal(2, seite.FindAll("div.epos-simueb-ring").Count);
    }

    /// <summary>
    /// <b>Entscheid c (#222): KEINE ZOOMLEISTE AN RINGEN.</b> Die Leiste
    /// „×1 · 1:1" bleibt Zeitreihen und Balken vorbehalten; an einem Ring war sie
    /// Bedienfläche ohne Gegenwert, und auf ×1,2 schnitt der Rahmen den Kreis an.
    /// Das ist die eine Ausnahme zur Hausregel W8‑E‑2 — der RAHMEN bleibt.
    /// </summary>
    [Fact]
    public void Die_Ringe_tragen_keine_Zoomleiste()
    {
        var seite = Zeichnen(Daten());

        Assert.Empty(seite.FindAll("div.epos-diagramm-leiste"));
        Assert.Empty(seite.FindAll("button.epos-diagramm-knopf"));
        Assert.Equal(2, seite.FindAll("div.epos-diagramm").Count);   // der Rahmen bleibt
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

        Assert.Equal(2, seite.FindAll("img").Count);          // der Stromring bleibt stehen
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

        Assert.Empty(seite.FindAll("img"));
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
        Assert.Empty(seite.FindAll("img"));
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
            p.Add(x => x.RingWaerme, BILD);
            p.Add(x => x.RingStrom, BILD);
        });

        Assert.Empty(seite.FindAll("p.epos-simueb-leerkarte"));
        Assert.Equal(6, seite.FindAll(".epos-simueb-kennzahl").Count);
        Assert.Equal(2, seite.FindAll("ul.epos-simueb-legende").Count);
        Assert.Contains(Resource.SIMUEB_BADGE_OHNE_STROMERZEUGER, seite.Markup,
                        StringComparison.Ordinal);
    }
}
