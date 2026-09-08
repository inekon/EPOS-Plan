using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der UEBERSICHTS-Reiter (iU9-W11b.2), Vorbild <c>tabPage_Uebersicht</c> (R2)
/// UND <c>NavigatorUebersicht</c> (428 Z. samt 148 Zeilen GDI).
///
/// <para>Soll: die 13 Zahlen aus dem Kern-DTO, die Praesenzregel je Zeile,
/// zwei Ringe und zwei Kacheln, das Eigenanteilsraster, ohne Bedarf KEIN Ring
/// (Befund W11-B36) und die beiden Rollen der Komponente.</para>
/// <para>Der Selektor nennt seit der Windows-Abnahme 05.09.2026 die Klasse
/// <c>epos-simerg-knopf</c>: Jedes Diagramm steht seither im Baustein
/// <c>Diagramm</c> und bringt seine eigenen Knöpfe („1:1“, „Bereich“) mit.
/// <c>FindAll("button")</c> zählte die mit und prüfte damit nicht mehr, was
/// der Fall behauptet — nämlich die Knöpfe DIESES Reiters.</para>
/// </summary>
public class UebersichtReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;

    public UebersichtReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        // Die BESCHRIFTUNGEN folgen der Oberflaechensprache, die ZAHLEN der
        // Zahlenkultur — der Vorlaeufer formatierte mit ToString("F2") und damit
        // ebenfalls kulturabhaengig. Beide werden festgelegt (Regel seit W8).
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
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

    private static UebersichtDaten Daten(bool waermebedarf = true, bool strombedarf = true) =>
        new UebersichtDaten
        {
            Waermepumpe = true,
            Heizstab = true,
            Heizkessel = true,
            WaermedeckungProzent = 98.7,
            StromdeckungProzent = 12.3,
            WaermebedarfVorhanden = waermebedarf,
            StrombedarfVorhanden = strombedarf,
            ReststromMwh = 30.0,
            RestwaermeMwh = 6.04,
            EigenanteilSpalten = new[] { "Energie-Erzeuger", "Ergebnis [MWh/a]", "Deckung Heizung [MWh/a]" },
            Eigenanteil = new[]
            {
                new Rasterzeile(new[] { "Wärmepumpe", "300,00", "280,00" }),
                new Rasterzeile(new[] { "Heizkessel", "174,21", "160,00" })
            }
        };

    private IRenderedComponent<UebersichtReiter> Zeichnen(UebersichtDaten daten,
                                                          bool nurNavigator = false,
                                                          Action? details = null)
        => Render<UebersichtReiter>(p =>
        {
            p.Add(x => x.Kennzahlen, Zahlen());
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Kuchen, BILD);
            p.Add(x => x.RingWaerme, BILD);
            p.Add(x => x.RingStrom, BILD);
            p.Add(x => x.NurNavigator, nurNavigator);
            if (details is not null) p.Add(x => x.BedarfDetails, EventCallback.Factory.Create(this, details));
        });

    // =====================================================================

    /// <summary>Die 13 Zahlen stehen mit dem Format „F2" des Vorlaeufers.</summary>
    [Fact]
    public void Die_Kennzahlen_stehen_mit_zwei_Nachkommastellen()
    {
        var seite = Zeichnen(Daten());
        string text = seite.Markup;

        Assert.Contains("480,25", text);
        Assert.Contains("120,50", text);
        Assert.Contains("300,00", text);
        Assert.Contains("6,04", text);
    }

    /// <summary>
    /// Praesenz: In einem Projekt ohne BHKW, Solarthermie und PV stehen deren
    /// Zeilen NICHT da — der Vorlaeufer zeigte „0,00".
    /// </summary>
    [Fact]
    public void Zeilen_ohne_Komponente_stehen_nicht_da()
    {
        var seite = Zeichnen(Daten());
        string text = seite.Markup;

        Assert.DoesNotContain("Wärmeproduktion BHKW", text);
        Assert.DoesNotContain("Solare Wärme", text);
        Assert.DoesNotContain("Stromproduktion PV", text);
        Assert.Contains("Wärmeproduktion WP", text);
        Assert.Contains("Stromverbrauch Heizstab", text);
    }

    /// <summary>Die beiden Restzeilen beschreiben das Projekt und bleiben immer stehen.</summary>
    [Fact]
    public void Die_beiden_Restzeilen_bleiben_immer_stehen()
    {
        var d = Daten();
        d.Waermepumpe = d.Heizstab = d.Heizkessel = false;

        var seite = Zeichnen(d);
        Assert.Contains("Restwärmebedarf", seite.Markup);
        Assert.Contains("Reststrombedarf", seite.Markup);
    }

    /// <summary>
    /// Zwei Ringe, nebeneinander — der Kuchen entfällt seit W11b‑B‑11 (08.09.2026): er
    /// zeigte dieselbe Wärmebedarfsdeckung wie der Ring.
    /// </summary>
    [Fact]
    public void Die_volle_Rolle_zeigt_zwei_Ringe_und_keinen_Kuchen()
    {
        var seite = Zeichnen(Daten());
        Assert.Equal(2, seite.FindAll("img").Count);
        Assert.Empty(seite.FindAll("section.epos-simerg-diagrammzeile"));
        // beide Ringe in EINER Spaltenzeile nebeneinander
        Assert.Contains(seite.FindAll("div.epos-simerg-spalten"), z => z.QuerySelectorAll("img").Length == 2);
    }

    /// <summary>
    /// Befund W11-B36: Ohne Bedarf steht kein Ring, sondern der Satz dazu — der
    /// Vorlaeufer setzte den Mittelwert hart auf 100 %.
    /// </summary>
    [Fact]
    public void Ohne_Bedarf_steht_kein_Ring()
    {
        var seite = Zeichnen(Daten(waermebedarf: false, strombedarf: false));

        Assert.Empty(seite.FindAll("img"));            // kein Ring - und keinen Kuchen gibt es mehr
        Assert.Equal(2, seite.FindAll("p.epos-simerg-hinweis").Count);
    }

    /// <summary>Die zwei KPI-Kacheln des Navigators.</summary>
    [Fact]
    public void Zwei_Kacheln_zeigen_Reststrom_und_Restwaerme()
    {
        var seite = Zeichnen(Daten());
        string text = seite.Markup;

        Assert.Contains("Reststrombedarf", text);
        Assert.Contains("Restwärmebedarf", text);
        Assert.Contains("30,00", text);
    }

    /// <summary>
    /// <b>Anwenderwunsch 08.09.2026 (W11b‑B‑12).</b> Die Kachelreihe folgt
    /// jetzt den Ringen darüber — links Restwärme (unter dem Wärmering),
    /// rechts Reststrom (unter dem Stromring); vorher stand Reststrom links.
    /// </summary>
    [Fact]
    public void Die_Kacheln_stehen_in_der_Reihenfolge_der_Ringe()
    {
        var seite = Zeichnen(Daten());
        var kacheln = seite.FindAll("div.epos-kennzahlkachel");

        Assert.Equal(2, kacheln.Count);
        Assert.Contains("Restwärmebedarf", kacheln[0].TextContent);
        Assert.Contains("Reststrombedarf", kacheln[1].TextContent);
    }

    /// <summary>Das Eigenanteilsraster: Kopfzeile und je Erzeuger eine Zeile.</summary>
    [Fact]
    public void Das_Eigenanteilsraster_zeigt_je_Erzeuger_eine_Zeile()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(3, seite.FindAll("table.epos-raster thead th").Count);
        Assert.Equal(2, seite.FindAll("table.epos-raster tbody tr").Count);
    }

    /// <summary>
    /// Die zweite Rolle: im Ergebnisreiter zeigt dieselbe Komponente NUR den
    /// Navigatorteil — keine Kennzahlengruppen, kein Kuchen.
    /// </summary>
    [Fact]
    public void Die_Navigatorrolle_zeigt_nur_Ringe_Kacheln_und_Raster()
    {
        var seite = Zeichnen(Daten(), nurNavigator: true);

        Assert.Equal(2, seite.FindAll("img").Count);
        Assert.Empty(seite.FindAll("dl.epos-simerg-werte"));
        Assert.Single(seite.FindAll("table.epos-raster"));
    }

    /// <summary>
    /// <b>Befund W11b‑B‑4</b> (Windows-Abnahme V2 vom 07.09.2026): „Simulation
    /// Übersicht: Die Charts sind optisch zu groß." Alle DREI Bilder dieses
    /// Reiters sind rund — der Kuchen 960 × 600, die zwei Ringe je 720 × 560 —
    /// und tragen deshalb die Maßmarke. Sie stehen weiter über beide Spalten
    /// (W11b‑B‑2 bleibt); begrenzt ist die ANZEIGEBREITE des Bildes, nicht die
    /// Zeile, in der es steht.
    /// </summary>
    [Fact]
    public void Beide_Ringe_des_Reiters_sind_rund_bemessen()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(2, seite.FindAll("div.epos-diagramm--rund").Count);
        Assert.Equal(2, seite.FindAll("div.epos-diagramm--rund img").Count);
    }

    /// <summary>In der Navigatorrolle bleiben es die zwei Ringe — beide rund.</summary>
    [Fact]
    public void Auch_die_Navigatorrolle_bemisst_ihre_Ringe()
    {
        var seite = Zeichnen(Daten(), nurNavigator: true);

        Assert.Equal(2, seite.FindAll("div.epos-diagramm--rund").Count);
    }

    // =====================================================================
    //  Anwenderrueckmeldung 08.09.2026 — W11b‑B‑15: nach Wärme und Strom
    // =====================================================================

    /// <summary>
    /// <b>Anwenderrückmeldung 08.09.2026 (W11b‑B‑15).</b> „Es sollte nach
    /// Kategorie Strom und Wärme gruppiert werden." Statt der zwei Balken
    /// „Energiebedarf" und „Ergebnisse" untereinander stehen zwei Gruppen
    /// NEBENEINANDER — in derselben Ordnung wie die zwei Ringe darunter.
    /// </summary>
    [Fact]
    public void Die_Kennzahlen_stehen_in_den_zwei_Gruppen_Waerme_und_Strom()
    {
        var seite = Zeichnen(Daten());
        var koepfe = seite.FindAll("h2.epos-gruppenkopf-titel");

        Assert.Equal(2, koepfe.Count);
        Assert.Equal("Wärme", koepfe[0].TextContent.Trim());
        Assert.Equal("Strom", koepfe[1].TextContent.Trim());

        // Die zwei alten Balken gibt es nicht mehr.
        Assert.DoesNotContain("Energiebedarf", seite.Markup);
        Assert.DoesNotContain("Ergebnisse", seite.Markup);
    }

    /// <summary>
    /// Die zwei Gruppen stehen in EINER Spaltenzeile — nebeneinander, wie die
    /// zwei Ringe; untereinander wären es wieder zwei Balken.
    /// </summary>
    [Fact]
    public void Die_zwei_Gruppen_stehen_nebeneinander()
    {
        var seite = Zeichnen(Daten());

        Assert.Contains(seite.FindAll("div.epos-simerg-spalten"),
                        z => z.QuerySelectorAll("dl.epos-simerg-werte").Length == 2);
    }

    /// <summary>
    /// Die Wärmegruppe: erst der BEDARF, dann die Erzeuger in der Reihenfolge
    /// des Entwurfs (WP, BHKW, Solar, SPK), zuletzt der REST. Das Projekt der
    /// Probe hat Wärmepumpe und Kessel — BHKW und Solarthermie fehlen nach der
    /// Präsenzregel.
    /// </summary>
    [Fact]
    public void Die_Waermegruppe_fuehrt_Bedarf_Erzeuger_und_Rest()
    {
        var seite = Zeichnen(Daten());
        var listen = seite.FindAll("dl.epos-simerg-werte");

        Assert.Equal(2, listen.Count);
        Assert.Equal(
            new[] { "Wärmebedarf des Nahwärmenetzes:", "Wärmeproduktion WP:",
                    "Wärmeproduktion der Spitzenkessel:", "Restwärmebedarf:" },
            listen[0].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Die Stromgruppe: BEDARF, die drei Verbraucher, die Erzeuger, REST. Der
    /// Stromverbrauch SPK stand vorher HINTER den Erzeugerzeilen; er gehört zu
    /// den Verbrauchern, aus denen sich die Restzahl ergibt.
    /// </summary>
    [Fact]
    public void Die_Stromgruppe_fuehrt_Bedarf_Verbraucher_Erzeuger_und_Rest()
    {
        var seite = Zeichnen(Daten());
        var listen = seite.FindAll("dl.epos-simerg-werte");

        Assert.Equal(
            new[] { "Strombedarf:", "Stromverbrauch WP:", "Stromverbrauch Heizstab:",
                    "Stromverbrauch SPK:", "Reststrombedarf:" },
            listen[1].QuerySelectorAll("dt").Select(z => z.TextContent.Trim()).ToArray());
    }

    /// <summary>
    /// Der Rest ist keine weitere Zeile der Aufstellung, sondern ihr Ergebnis:
    /// Er trägt in allen drei Zellen die Abschlusskennzeichnung (fett, Linie
    /// darüber). Keine andere Zeile trägt sie — und keine gewöhnliche Zeile
    /// bekommt ein leeres Klassenattribut (<c>Zeilenklasse</c> gibt <c>null</c>).
    /// </summary>
    [Fact]
    public void Die_Restzeile_schliesst_jede_Gruppe_betont_ab()
    {
        var seite = Zeichnen(Daten());
        var betont = seite.FindAll("dt.epos-simerg-abschluss");

        Assert.Equal(2, betont.Count);
        Assert.Equal("Restwärmebedarf:", betont[0].TextContent.Trim());
        Assert.Equal("Reststrombedarf:", betont[1].TextContent.Trim());

        Assert.Equal(4, seite.FindAll("dd.epos-simerg-abschluss").Count);   // Wert und Einheit je Zeile
        Assert.Equal(9, seite.FindAll("dl.epos-simerg-werte dt").Count);    // 4 Wärme + 5 Strom
        Assert.Equal(2, seite.FindAll("dl.epos-simerg-werte dt[class]").Count);
    }

    /// <summary>Kein Rueckruf = kein Knopf (Hausregel seit W2).</summary>
    [Fact]
    public void Ohne_Rueckruf_bleibt_der_Bedarfsknopf_weg()
    {
        var seite = Zeichnen(Daten());
        Assert.Empty(seite.FindAll("button.epos-simerg-knopf"));
    }

    [Fact]
    public void Der_Bedarfsknopf_meldet_seinen_Klick()
    {
        int gerufen = 0;
        var seite = Zeichnen(Daten(), details: () => gerufen++);

        seite.Find("button.epos-simerg-knopf").Click();
        Assert.Equal(1, gerufen);
    }
}
