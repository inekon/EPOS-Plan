using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der WÄRMEBEDARF EINES GEBÄUDES (iU9-W9.8, Anwenderwunsch <b>W9‑E‑2</b> vom
/// 05.09.2026) — die Überlagerung hinter dem Knopf „Simulation…" des Gebäudedialogs.
///
/// <para><b>Was geprüft wird:</b> die drei Kennzahlen, die Einheitenwahl (W8‑O‑5), der
/// Schalter „sortiert" samt seiner Wirkung auf den Bildauftrag, der Datenzoom — und
/// dass es weder einen Brauchwasser- noch einen Gesamt-Schalter gibt: der Anwender hat
/// beides ausdrücklich abbestellt.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Zahlen und Beschriftungen.</para>
/// </summary>
public class GebaeudeBedarfDialogTests : BunitContext
{
    public GebaeudeBedarfDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Bildauftrag, wie ihn die Komponente stellt.</summary>
    private sealed record Auftrag(bool Sortiert, Diagrammbereich? Bereich);

    private static GebaeudeBedarfDaten Daten(double mwh = 52.84, double kw = 31.5,
                                             double? vbh = 1677.0)
    {
        var monate = new double[12];
        for (int m = 0; m < 12; m++) monate[m] = mwh / 12.0;

        return new GebaeudeBedarfDaten
        {
            Name = "EFH-A-TS-212",
            HeizwaermeMwh = mwh,
            MaxLastKw = kw,
            VollbenutzungsstundenH = vbh,
            MonatswerteMwh = monate
        };
    }

    private IRenderedComponent<GebaeudeBedarfDialog> Aufbauen(
        GebaeudeBedarfDaten? daten = null,
        List<Auftrag>? auftraege = null,
        Energieeinheit? einheit = null,
        Action<Energieeinheit>? einheitGewaehlt = null,
        Action<bool>? geschlossen = null)
    {
        List<Auftrag> liste = auftraege ?? new List<Auftrag>();

        return Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Bildauftrag, (sortiert, bereich) =>
            {
                liste.Add(new Auftrag(sortiert, bereich));
                return new byte[] { 1, 2, 3 };
            })
            .Add(x => x.Einheit, einheit ?? Energieeinheit.MWh)
            .Add(x => x.EinheitGewaehlt, einheitGewaehlt)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    /// <summary>
    /// Die drei Kennzahlen, der Gebäudename, das Bild und die Monatsübersicht — mehr
    /// nicht.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_steht()
    {
        var cut = Aufbauen();

        Assert.Contains("Wärmebedarf Gebäude", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("EFH-A-TS-212", cut.Find(".epos-kontextzeile").TextContent);

        Assert.Contains("Wärmebedarf Heizung:", cut.Markup);
        Assert.Contains("max. Wärmelast", cut.Markup);
        Assert.Contains("Vollbenutzungsstunden:", cut.Markup);

        Assert.Contains("52,84", cut.Markup);      // MWh, deutsche Anzeige
        Assert.Contains("31,50", cut.Markup);      // kW
        Assert.Contains("1677,00", cut.Markup);    // h/a

        Assert.NotNull(cut.Find("img.epos-chartbild"));
        Assert.Contains("Januar:", cut.Markup);
        Assert.Contains("Dezember:", cut.Markup);
    }

    /// <summary>
    /// <b>Der Nachtrag des Anwenders</b> („ohne Brauchwasser und ohne gesamt"): Der
    /// Dialog zeigt GENAU EINE Reihe. Weder der Schalter „Gesamt" noch ein
    /// Bedarfsartschalter der Ergebnisseite steht hier.
    /// </summary>
    [Fact]
    public void Es_gibt_weder_Brauchwasser_noch_Gesamt()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Brauchwasser", cut.Markup);
        Assert.DoesNotContain("Gesamt", cut.Markup);
        Assert.DoesNotContain("Prozess", cut.Markup);

        // Genau EIN Schalter: „sortiert".
        Assert.Single(cut.FindAll("input[type=checkbox]"));
        Assert.Contains("sortiert", cut.Markup);
    }

    /// <summary>Ohne Höchstlast gibt es keine Vollbenutzungsstunden, sondern „—".</summary>
    [Fact]
    public void Ohne_Hoechstlast_steht_ein_Strich()
    {
        var cut = Aufbauen(Daten(mwh: 0, kw: 0, vbh: null));

        Assert.Contains("—", cut.Markup);
    }

    /// <summary>Ohne Monatswerte bleibt die Monatstabelle weg.</summary>
    [Fact]
    public void Ohne_Monatswerte_gibt_es_keine_Monatstabelle()
    {
        var cut = Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, new GebaeudeBedarfDaten { Name = "X", HeizwaermeMwh = 1 }));

        Assert.DoesNotContain("Januar:", cut.Markup);
    }

    /// <summary>Ohne Delegat kein Bild — der Platzhalter steht.</summary>
    [Fact]
    public void Ohne_Bildauftrag_steht_der_Platzhalter()
    {
        var cut = Render<GebaeudeBedarfDialog>(p => p.Add(x => x.Daten, Daten()));

        Assert.Empty(cut.FindAll("img.epos-chartbild"));
        Assert.Contains("Kein Diagramm vorhanden", cut.Markup);
    }

    // =================================================================================
    // Der Schalter „sortiert"
    // =================================================================================

    /// <summary>
    /// Der Schalter lässt NEU ZEICHNEN: Der zweite Auftrag trägt <c>Sortiert = true</c>.
    /// Das ist die Dauerlinie — dieselbe Umschaltung wie im Bedarfsreiter der
    /// Ergebnisseite.
    /// </summary>
    [Fact]
    public void Der_Schalter_sortiert_laesst_neu_zeichnen()
    {
        var auftraege = new List<Auftrag>();
        var cut = Aufbauen(auftraege: auftraege);

        Assert.Contains(auftraege, a => !a.Sortiert);
        Assert.False(cut.Instance.Sortiert);

        cut.Find("input[type=checkbox]").Change(true);

        Assert.True(cut.Instance.Sortiert);
        Assert.Contains(auftraege, a => a.Sortiert);
    }

    // =================================================================================
    // Der Datenzoom (Befund A-1 der Windows-Abnahme 05.09.2026)
    // =================================================================================

    /// <summary>
    /// Ein aufgezogenes Rechteck geht UNVERÄNDERT in den Bildauftrag — was an dieser
    /// Stelle des Bildes steht, weiß nur der Renderer, der es gezeichnet hat.
    /// </summary>
    [Fact]
    public async Task Ein_aufgezogener_Bereich_geht_in_den_Bildauftrag()
    {
        var auftraege = new List<Auftrag>();
        var cut = Aufbauen(auftraege: auftraege);

        Diagramm diagramm = cut.FindComponent<Diagramm>().Instance;
        await cut.InvokeAsync(() => diagramm.BereichGemeldet(0.25, 0.5, 0.1, 0.9));

        Assert.NotNull(cut.Instance.Bereich);
        Assert.Equal(0.25, cut.Instance.Bereich!.XVon);
        Assert.Contains(auftraege, a => a.Bereich is not null && a.Bereich.XBis == 0.5);
    }

    /// <summary>
    /// Ein Schalterwechsel VERWIRFT den Achsenausschnitt: Ganglinie und Dauerlinie
    /// tragen an derselben Bildstelle verschiedene Stunden, und ein mitgeschleppter
    /// Ausschnitt zeigte danach etwas anderes, als der Anwender aufgezogen hat.
    /// </summary>
    [Fact]
    public async Task Der_Schalterwechsel_verwirft_den_Ausschnitt()
    {
        var cut = Aufbauen();

        Diagramm diagramm = cut.FindComponent<Diagramm>().Instance;
        await cut.InvokeAsync(() => diagramm.BereichGemeldet(0.25, 0.5, 0.1, 0.9));
        Assert.NotNull(cut.Instance.Bereich);

        cut.Find("input[type=checkbox]").Change(true);

        Assert.Null(cut.Instance.Bereich);
    }

    // =================================================================================
    // Die Einheit (W8-O-5)
    // =================================================================================

    /// <summary>
    /// MWh ist die Vorgabe, kWh ist wählbar — und die Wahl geht an die Hülle zurück,
    /// damit sie gemerkt wird (dieselbe Ablage wie im Bedarfsergebnisdialog).
    /// </summary>
    [Fact]
    public void Die_Einheit_ist_waehlbar_und_wird_gemeldet()
    {
        Energieeinheit? gemeldet = null;
        var cut = Aufbauen(einheitGewaehlt: e => gemeldet = e);

        Assert.Same(Energieeinheit.MWh, cut.Instance.Anzeigeeinheit);
        Assert.Contains("52,84", cut.Markup);

        cut.Find("select").Change("1");   // kWh

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
        Assert.Same(Energieeinheit.KWh, gemeldet);

        // 52,84 MWh sind 52 840 kWh, in kWh-Formatierung ohne Nachkommastellen.
        Assert.Contains("52840", cut.Markup.Replace(".", "").Replace(" ", ""));
    }

    /// <summary>
    /// Die LEISTUNG folgt der Einheitenwahl nicht — sie steht in kW, egal was gewählt
    /// ist (Hausregel: eine Energiemenge wird umgerechnet, eine Leistung nicht).
    /// </summary>
    [Fact]
    public void Die_Hoechstlast_bleibt_in_kW()
    {
        var cut = Aufbauen();

        cut.Find("select").Change("1");   // kWh

        Assert.Contains("31,50", cut.Markup);
        Assert.Contains("kW", cut.Markup);
    }

    // =================================================================================
    // Tastatur und Schlussleiste
    // =================================================================================

    [Fact]
    public void OK_schliesst()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(ergebnis);
    }

    [Theory]
    [InlineData("Escape")]
    [InlineData("Enter")]
    public void Esc_und_Enter_schliessen(string taste)
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = taste });

        Assert.True(ergebnis);
    }
}
