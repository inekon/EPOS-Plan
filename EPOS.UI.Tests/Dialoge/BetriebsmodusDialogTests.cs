using Bunit;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// BetriebsmodusDialog (iU9-W10a.1) - der Ersatz fuer Form_Betriebsmodus.
///
/// <para>FELDBESTAND laut Feldkarte: neun Steuerelemente - Titel mit {0}, eine
/// Kopfzeile, drei Wahlknoepfe mit je einer Erlaeuterung, OK und Abbrechen. Dazu
/// NEU der Hilfeknopf (Befund W10-B2, Abweichung A-1).</para>
/// </summary>
public class BetriebsmodusDialogTests : BunitContext
{
    /// <summary>Die drei Steuerwerte, wie die Huelle sie aus WaermequelleClass reicht.</summary>
    private const string LAUFZEIT = "Laufzeit";
    private const string LEISTUNG = "Leistung";
    private const string PV = "PV";

    private IRenderedComponent<BetriebsmodusDialog> Zeige(string aktuellerModus,
                                                          Action<string?>? geschlossen = null)
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        return Render<BetriebsmodusDialog>(p =>
        {
            p.Add(x => x.Bezeichner, "WP Erdgeschoss");
            p.Add(x => x.AktuellerModus, aktuellerModus);
            p.Add(x => x.SteuerwertLaufzeit, LAUFZEIT);
            p.Add(x => x.SteuerwertLeistung, LEISTUNG);
            p.Add(x => x.SteuerwertPv, PV);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });
    }

    // ================================================================== Feldbestand

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Zeige(LAUFZEIT);

        // Titel mit eingesetztem Anlagennamen.
        Assert.Equal("Betriebsmodus - WP Erdgeschoss", cut.Find("h1.epos-dialog-titel").TextContent);

        // Kopfzeile (im Vorlaeufer fett) als legend der Optionsgruppe.
        Assert.Contains("Leistungssteuerung der Wärmepumpe:",
                        cut.Find("legend.epos-optionsgruppe-titel").TextContent);

        // Drei Wahlknoepfe.
        Assert.Equal(3, cut.FindAll("input[type=radio]").Count);

        // Drei Erlaeuterungen.
        Assert.Equal(3, cut.FindAll("p.epos-option-beschreibung").Count);

        // Zwei Fussknoepfe.
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);

        // NEU (A-1): der Hilfeknopf.
        Assert.NotNull(cut.Find("button.epos-infoknopf"));
    }

    /// <summary>
    /// Die drei Erlaeuterungen stehen WOERTLICH da, samt der Umbrueche, die der
    /// Vorlaeufer mit "\n" setzte (SIM_BM_TEXT_*).
    /// </summary>
    [Fact]
    public void Die_Erlaeuterungen_stehen_woertlich_unter_ihrer_Option()
    {
        var cut = Zeige(LAUFZEIT);
        var texte = cut.FindAll("p.epos-option-beschreibung");

        Assert.Contains("die über den Bedarf hinaus", texte[0].TextContent);
        Assert.Contains("Lange Laufzeiten, wenig Takten.", texte[0].TextContent);
        Assert.Contains("keinen Überschuss", texte[1].TextContent);
        Assert.Contains("begrenzt auf den PV-Überschuss", texte[2].TextContent);
    }

    // ================================================================== Vorauswahl

    [Theory]
    [InlineData(LEISTUNG, BetriebsmodusDialog.LEISTUNG)]
    [InlineData(PV, BetriebsmodusDialog.PV)]
    [InlineData(LAUFZEIT, BetriebsmodusDialog.LAUFZEIT)]
    public void Die_Vorauswahl_folgt_dem_gespeicherten_Modus(string modus, int platz)
    {
        Assert.Equal(platz, Zeige(modus).Instance.Auswahl);
    }

    /// <summary>
    /// Ein leerer oder unbekannter BM_Typ bedeutet LAUFZEITOPTIMIERT - der
    /// default-Zweig von ModusVorwaehlen:134-142. Ohne diese Regel staende ein
    /// Bestandsprojekt ohne gepflegten Modus ohne jede Vorauswahl da.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("gibt-es-nicht")]
    public void Ohne_gepflegten_Modus_steht_Laufzeit(string modus)
    {
        Assert.Equal(BetriebsmodusDialog.LAUFZEIT, Zeige(modus).Instance.Auswahl);
    }

    // ===================================================================== Ergebnis

    [Fact]
    public void OK_liefert_den_Steuerwert_der_gewaehlten_Option()
    {
        string? ergebnis = null;
        bool gerufen = false;

        var cut = Zeige(LAUFZEIT, m => { ergebnis = m; gerufen = true; });

        cut.FindAll("input[type=radio]")[2].Change("2");     // PV
        cut.Find("button.epos-knopf--primaer").Click();

        Assert.True(gerufen);
        Assert.Equal(PV, ergebnis);
    }

    /// <summary>
    /// Ohne Umschalten liefert OK den Modus, mit dem der Dialog aufging - der Aufrufer
    /// schreibt ihn dann unveraendert zurueck, genau wie im Vorlaeufer.
    /// </summary>
    [Fact]
    public void OK_ohne_Aenderung_liefert_den_Ausgangsmodus()
    {
        string? ergebnis = null;
        var cut = Zeige(LEISTUNG, m => ergebnis = m);

        cut.Find("button.epos-knopf--primaer").Click();

        Assert.Equal(LEISTUNG, ergebnis);
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        string? ergebnis = "unveraendert";
        bool gerufen = false;

        var cut = Zeige(PV, m => { ergebnis = m; gerufen = true; });

        cut.FindAll(".epos-leiste button")[0].Click();       // Abbrechen

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_liefert_null()
    {
        string? ergebnis = "unveraendert";
        bool gerufen = false;

        var cut = Zeige(PV, m => { ergebnis = m; gerufen = true; });

        cut.Find("div.epos-dialog").KeyDown("Escape");

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Enter IST belegt: Der Dialog entscheidet nur und schreibt nichts; im Vorlaeufer
    /// war AcceptButton gesetzt.
    /// </summary>
    [Fact]
    public void Enter_bestaetigt()
    {
        string? ergebnis = null;
        var cut = Zeige(LEISTUNG, m => ergebnis = m);

        cut.Find("div.epos-dialog").KeyDown("Enter");

        Assert.Equal(LEISTUNG, ergebnis);
    }
}
