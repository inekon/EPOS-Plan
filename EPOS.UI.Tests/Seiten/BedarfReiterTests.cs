using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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
public class BedarfReiterTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;
    private readonly List<Bildauftrag> _auftraege = new();

    public BedarfReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
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
            p.Add(x => x.Bild, a => { _auftraege.Add(a); return new byte[] { 1 }; });
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
        Assert.Single(seite.FindAll("label.epos-schalter")
                           .Where(s => s.TextContent.Trim() == "sortiert"));

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
}
