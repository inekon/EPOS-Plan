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

    /// <summary>Kuchen, zwei Ringe — drei Bilder in der vollen Rolle.</summary>
    [Fact]
    public void Die_volle_Rolle_zeigt_drei_Bilder()
    {
        var seite = Zeichnen(Daten());
        Assert.Equal(3, seite.FindAll("img").Count);
    }

    /// <summary>
    /// Befund W11-B36: Ohne Bedarf steht kein Ring, sondern der Satz dazu — der
    /// Vorlaeufer setzte den Mittelwert hart auf 100 %.
    /// </summary>
    [Fact]
    public void Ohne_Bedarf_steht_kein_Ring()
    {
        var seite = Zeichnen(Daten(waermebedarf: false, strombedarf: false));

        Assert.Single(seite.FindAll("img"));           // nur noch der Kuchen
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
    /// Navigatorteil — kein Energiebedarf, kein Ergebnisblock, kein Kuchen.
    /// </summary>
    [Fact]
    public void Die_Navigatorrolle_zeigt_nur_Ringe_Kacheln_und_Raster()
    {
        var seite = Zeichnen(Daten(), nurNavigator: true);

        Assert.Equal(2, seite.FindAll("img").Count);
        Assert.Empty(seite.FindAll("dl.epos-simerg-werte"));
        Assert.Single(seite.FindAll("table.epos-raster"));
    }

    /// <summary>Kein Rueckruf = kein Knopf (Hausregel seit W2).</summary>
    [Fact]
    public void Ohne_Rueckruf_bleibt_der_Bedarfsknopf_weg()
    {
        var seite = Zeichnen(Daten());
        Assert.Empty(seite.FindAll("button"));
    }

    [Fact]
    public void Der_Bedarfsknopf_meldet_seinen_Klick()
    {
        int gerufen = 0;
        var seite = Zeichnen(Daten(), details: () => gerufen++);

        seite.Find("button").Click();
        Assert.Equal(1, gerufen);
    }
}
