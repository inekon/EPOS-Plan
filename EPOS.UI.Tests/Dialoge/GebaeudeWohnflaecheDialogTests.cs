using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wohn-/Nutzfläche eines Projektgebäudes (iU9-W9.3). Soll ist die Feldkarte von
/// <c>Form_GebWohnflaeche</c>: 15 Zeilen — vier gesperrte Kopffelder, die Bedarfsart mit
/// ihrem Einheitsauszug, Verbrauch, Jahresnutzungsgrad, der Schalter für die dezentrale
/// Warmwasserbereitung und drei Knöpfe.
/// </summary>
public class GebaeudeWohnflaecheDialogTests : BunitContext
{
    private static readonly string[] BEDARFSARTEN =
    {
        "Ölverbrauch [l/a]",
        "Gasverbrauch [m³/a]",
        "Gasverbrauch [MWh/a] (Ho)",
        "Brennstoffverbrauch [MWh/a]",
        "Verbrauch  [MWh/a]",
        "Wohnfläche [m²]"
    };

    public GebaeudeWohnflaecheDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<GebaeudeWohnflaecheDialog> Aufbauen(
        string einheit = "Wohnfläche [m²]",
        double wert = 120.5,
        double nutzungsgrad = 0.85,
        bool dezentral = false,
        Action<GebaeudeWohnflaecheErgebnis?>? geschlossen = null)
        => Render<GebaeudeWohnflaecheDialog>(p => p
            .Add(x => x.Gebaeudename, "Haus 1")
            .Add(x => x.Beschreibung, "Ein Mehrfamilienhaus")
            .Add(x => x.Gebaeudeart, "Mehrfamilienhaus")
            .Add(x => x.Baujahr, "1969 bis 1978")
            .Add(x => x.Wert, wert)
            .Add(x => x.Jahresnutzungsgrad, nutzungsgrad)
            .Add(x => x.Einheit, einheit)
            .Add(x => x.DezentralWarmwasser, dezentral)
            .Add(x => x.Bedarfsarten, BEDARFSARTEN)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    private static IElement Knopf(IRenderedComponent<GebaeudeWohnflaecheDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        // Zwei Zahlenfelder (Verbrauch, Jahresnutzungsgrad), eine Klappliste,
        // ein Schalter, ein mehrzeiliges und drei einzeilige Textfelder.
        Assert.Equal(2, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Single(cut.FindAll("select"));
        Assert.Single(cut.FindAll("input[type=checkbox]"));
        Assert.Single(cut.FindAll("textarea"));

        Assert.Contains("Eingabe der gesamten Wohn-/Nutzfläche", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Info ausgewähltes Gebäude", cut.Markup);
        Assert.Contains("Eingabe für das ausgewählte Gebäude", cut.Markup);
        Assert.Contains("Gebäudeart:", cut.Markup);
        Assert.Contains("Gebäudename:", cut.Markup);
        Assert.Contains("Beschreibung:", cut.Markup);
        Assert.Contains("Baujahr:", cut.Markup);
        Assert.Contains("Art der Angabe:", cut.Markup);
        Assert.Contains("Wärmebedarf/Wohnfläche:", cut.Markup);
        Assert.Contains("Jahresnutzungsgrad:", cut.Markup);
        Assert.Contains("Dezentrale Warmwasserbereitung", cut.Markup);
        Assert.Contains("z.B. 0.85 für 85%", cut.Markup);
    }

    [Fact]
    public void Die_vier_Kopffelder_sind_nur_lesbar()
    {
        var cut = Aufbauen();

        // Vier gesperrte Felder: drei einzeilige (Gebaeudeart, Gebaeudename, Baujahr)
        // und die mehrzeilige Beschreibung. Das gesperrte Feld "Art der Angabe" kommt
        // als viertes einzeiliges dazu.
        Assert.Equal(4, cut.FindAll("input[type=text][readonly]").Count);
        Assert.Single(cut.FindAll("textarea[readonly]"));
        Assert.Contains("Haus 1", cut.Markup);
        Assert.Contains("Mehrfamilienhaus", cut.Markup);
        Assert.Contains("1969 bis 1978", cut.Markup);
    }

    [Fact]
    public void Die_Klappliste_fuehrt_die_sechs_Bedarfsarten()
    {
        var cut = Aufbauen();

        Assert.Equal(6, cut.Find("select").QuerySelectorAll("option").Length);
        Assert.Contains("Ölverbrauch [l/a]", cut.Markup);
        Assert.Contains("Gasverbrauch [MWh/a] (Ho)", cut.Markup);
    }

    // =================================================================================
    // Einheitsauszug
    // =================================================================================

    [Fact]
    public void Die_Einheit_ist_der_Auszug_zwischen_den_eckigen_Klammern()
    {
        var cut = Aufbauen(einheit: "Wohnfläche [m²]");
        Assert.Equal("m²", cut.Instance.Einheitszeichen);
    }

    [Fact]
    public void Ein_Wechsel_der_Bedarfsart_schreibt_Art_und_Einheit_um()
    {
        var cut = Aufbauen(einheit: "Wohnfläche [m²]");

        cut.Find("select").Change("0");   // Ölverbrauch [l/a]

        Assert.Equal("Ölverbrauch [l/a]", cut.Instance.GewaehlteEinheit);
        Assert.Equal("l/a", cut.Instance.Einheitszeichen);
    }

    /// <summary>
    /// Ein Bestandswert, den die Liste nicht führt, wird ihr VORANGESTELLT — die
    /// <c>ComboBox</c> des Vorläufers war frei beschreibbar (A-16 aus Welle 7).
    /// </summary>
    [Fact]
    public void Ein_unbekannter_Bestandswert_wird_der_Liste_vorangestellt()
    {
        var cut = Aufbauen(einheit: "Fernwärme [MWh/a]");

        Assert.Equal(7, cut.Find("select").QuerySelectorAll("option").Length);
        Assert.Contains("Fernwärme [MWh/a]", cut.Markup);
        Assert.Equal("MWh/a", cut.Instance.Einheitszeichen);
    }

    // =================================================================================
    // OK, Abbrechen, Pflichtfelder
    // =================================================================================

    [Fact]
    public void OK_liefert_die_vier_Werte_zurueck()
    {
        GebaeudeWohnflaecheErgebnis? ergebnis = null;
        var cut = Aufbauen(wert: 120.5, nutzungsgrad: 0.85, dezentral: true,
                           geschlossen: e => ergebnis = e);

        Knopf(cut, "OK").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(120.5, ergebnis!.Wert);
        Assert.Equal(0.85, ergebnis.Jahresnutzungsgrad);
        Assert.Equal("Wohnfläche [m²]", ergebnis.Einheit);
    }

    /// <summary>
    /// <b>Befund W9‑B3.</b> Der Vorläufer schrieb den Schalter NICHT zurück; hier geht er
    /// mit (A-2).
    /// </summary>
    [Fact]
    public void OK_nimmt_die_dezentrale_Warmwasserbereitung_mit()
    {
        GebaeudeWohnflaecheErgebnis? ergebnis = null;
        var cut = Aufbauen(dezentral: false, geschlossen: e => ergebnis = e);

        cut.Find("input[type=checkbox]").Change(true);
        Knopf(cut, "OK").Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.DezentralWarmwasser);
    }

    /// <summary>
    /// <b>Befund W9‑B4.</b> <c>Double.Parse</c> ohne Prüfung warf im Vorläufer; hier
    /// bleibt der Dialog mit einem Warnbanner stehen.
    /// </summary>
    [Fact]
    public void Ein_leeres_Pflichtfeld_meldet_und_schliesst_nicht()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.FindAll("input[inputmode=decimal]")[0].Input("");
        Knopf(cut, "OK").Click();

        Assert.False(gerufen);
        Assert.Contains("Wärmebedarf/Wohnfläche", cut.Instance.Meldung);
        Assert.Single(cut.FindAll("[role=alert]"));
    }

    [Fact]
    public void Ein_leerer_Jahresnutzungsgrad_meldet_ebenfalls()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.FindAll("input[inputmode=decimal]")[1].Input("");
        Knopf(cut, "OK").Click();

        Assert.False(gerufen);
        Assert.Contains("Jahresnutzungsgrad", cut.Instance.Meldung);
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        GebaeudeWohnflaecheErgebnis? ergebnis = null;
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: e => { ergebnis = e; gerufen = true; });

        Knopf(cut, "Abbrechen").Click();

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_liefert_null()
    {
        bool gerufen = false;
        GebaeudeWohnflaecheErgebnis? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => { ergebnis = e; gerufen = true; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }
}
