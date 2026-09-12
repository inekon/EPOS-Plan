using Bunit;
using EPOS.UI.Dialoge.Simulation;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// WertAbfrage (iU9-W10a.0f) - die kleine Abfrage EINER Zahl, Ersatz fuer
/// Views/Simulation/Eingabefrage.
///
/// <para>Der Vorlaeufer gab eine ZEICHENKETTE zurueck, die jeder Aufrufer selbst
/// durch ZahlParsen schickte - und bei Misserfolg unterschiedlich behandelte
/// (Befund W10-B18: der Monatsknopf meldete, der Werteknopf schwieg). Hier kommt
/// nur eine gueltige Zahl heraus oder null.</para>
/// </summary>
public class WertAbfrageTests : BunitContext
{
    /// <summary>
    /// Geschlossen ist NICHTS zu sehen — die Ueberlagerung zeichnet ihren Inhalt gar
    /// nicht erst (dieselbe Regel wie bei Reiterblatt und Rueckfrage).
    /// </summary>
    [Fact]
    public void Geschlossen_zeigt_die_Abfrage_nicht()
    {
        var cut = Render<WertAbfrage>(p => p
            .Add(x => x.Offen, false)
            .Add(x => x.TitelText, "Alle Werte setzen"));

        Assert.Empty(cut.FindAll(".epos-wertabfrage"));
    }

    [Fact]
    public void Offen_zeigt_Titel_Beschriftung_und_Einheit()
    {
        var cut = Render<WertAbfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.TitelText, "Alle Werte setzen")
            .Add(x => x.Beschriftung, "Wert für alle Stunden:")
            .Add(x => x.Einheit, "°C")
            .Add(x => x.Vorgabe, 10.0));

        Assert.Contains("Alle Werte setzen", cut.Markup);
        Assert.Contains("Wert für alle Stunden:", cut.Markup);
        Assert.Contains("°C", cut.Markup);
    }

    /// <summary>
    /// Die Vorgabe steht beim Oeffnen im Feld — der Vorlaeufer setzte sie ebenso
    /// (Eingabefrage.Fragen bekam sie als vierten Parameter).
    /// </summary>
    [Fact]
    public void Die_Vorgabe_steht_beim_Oeffnen_im_Feld()
    {
        var cut = Render<WertAbfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Vorgabe, 12.5));

        Assert.Equal(12.5, cut.Instance.Wert);
    }

    [Fact]
    public void OK_meldet_die_Zahl()
    {
        double? gemeldet = null;
        bool gerufen = false;

        var cut = Render<WertAbfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Vorgabe, 7.5)
            .Add(x => x.Beantwortet, (double? w) => { gemeldet = w; gerufen = true; }));

        cut.Find("button.epos-knopf--primaer").Click();

        Assert.True(gerufen);
        Assert.Equal(7.5, gemeldet);
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        double? gemeldet = 99;
        bool gerufen = false;

        var cut = Render<WertAbfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Vorgabe, 7.5)
            .Add(x => x.Beantwortet, (double? w) => { gemeldet = w; gerufen = true; }));

        cut.FindAll("button.epos-knopf")[1].Click();

        Assert.True(gerufen);
        Assert.Null(gemeldet);
    }

    /// <summary>
    /// Ohne gueltige Zahl bleibt OK gesperrt. Damit kann der Wirt gar keine ungueltige
    /// Eingabe bekommen — der Unterschied zwischen den beiden Zwillingsknoepfen des
    /// Quellprofils (Befund W10-B18) verschwindet an der Wurzel.
    /// </summary>
    [Fact]
    public void Ohne_Zahl_bleibt_OK_gesperrt()
    {
        var cut = Render<WertAbfrage>(p => p.Add(x => x.Offen, true));

        Assert.Null(cut.Instance.Wert);
        Assert.True(cut.Find("button.epos-knopf--primaer").HasAttribute("disabled"));

        cut.Find("input.epos-eingabe").Input("3,5");
        Assert.Equal(3.5, cut.Instance.Wert);
        Assert.False(cut.Find("button.epos-knopf--primaer").HasAttribute("disabled"));
    }

    /// <summary>Komma UND Punkt werden angenommen — Hausregel des Zahlenfelds.</summary>
    [Fact]
    public void Komma_und_Punkt_werden_angenommen()
    {
        var cut = Render<WertAbfrage>(p => p.Add(x => x.Offen, true));

        cut.Find("input.epos-eingabe").Input("8.25");
        Assert.Equal(8.25, cut.Instance.Wert);

        cut.Find("input.epos-eingabe").Input("8,75");
        Assert.Equal(8.75, cut.Instance.Wert);
    }
}
