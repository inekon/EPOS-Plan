using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// ETAPPE E7b — <b>die Leistungspreis-Staffel auf der Trägerkarte</b> (Entscheid Q11,
/// Anwender 22.09.2026; der Rest nach Empfehlung: die zweistufige Staffel zieht in die
/// Kostenverwaltung neben die Energiepreisstruktur).
///
/// <para>Geprüft wird der Baustein <see cref="EnergietraegerEinstellungen"/> allein:
/// Die Gruppe steht nur, wo die Hülle sie freigibt (<c>MitStaffel</c> — der
/// Stromträger im Projektkontext), mit drei Feldern und ihren Einheiten und der
/// Erklärzeile; eine Eingabe schreibt in den Stand und meldet „geändert", ein
/// geleertes Feld heißt „nicht gepflegt" (<c>null</c>) statt „Wert von vorhin".</para>
/// </summary>
public class EnergietraegerStaffelTests : EposBunitContext
{
    public EnergietraegerStaffelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static EnergietraegerStand Strom(bool mitStaffel) => new()
    {
        TraegerZeile = "Elektrische Energie  (VDI 3805 1)",
        GruppeZeile = "Gruppe: Strom",
        Arbeitspreis = 0.25,
        Grundpreis = 2400,
        MitLeistungspreis = true,
        EinheitArbeitspreis = "€/kWh",
        EinheitLeistungspreis = "€/(kW·a)",
        MitStaffel = mitStaffel,
        StaffelGrenze = mitStaffel ? 1500 : null,
        StaffelPreis1 = mitStaffel ? 60 : null,
        StaffelPreis2 = mitStaffel ? 90 : null
    };

    private int _geaendert;

    private IRenderedComponent<EnergietraegerEinstellungen> Zeige(EnergietraegerStand stand) =>
        Render<EnergietraegerEinstellungen>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.Geaendert, () => _geaendert++));

    /// <summary>Das Eingabefeld zur Beschriftung — über den Feldtext, nicht über die Reihenfolge.</summary>
    private static IElement Feld(IRenderedComponent<EnergietraegerEinstellungen> cut, string text)
    {
        foreach (IElement label in cut.FindAll("label.epos-feld"))
        {
            IElement? t = label.QuerySelector(".epos-feld-text");
            if (t != null && t.TextContent == text) return label;
        }
        throw new Xunit.Sdk.XunitException("Kein Feld „" + text + "“.");
    }

    [Fact]
    public void Beim_Stromtraeger_im_Projekt_steht_die_Staffel_mit_drei_Feldern()
    {
        var cut = Zeige(Strom(mitStaffel: true));

        Assert.Contains("Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)", cut.Markup);

        IElement grenze = Feld(cut, "Staffelgrenze");
        Assert.Equal("kW", grenze.QuerySelector(".epos-einheit")!.TextContent);
        Assert.Equal("1500", grenze.QuerySelector("input")!.GetAttribute("value"));

        IElement p1 = Feld(cut, "Preis bis zur Grenze");
        IElement p2 = Feld(cut, "Preis über der Grenze");
        Assert.Equal("€/(kW·a)", p1.QuerySelector(".epos-einheit")!.TextContent);
        Assert.Equal("€/(kW·a)", p2.QuerySelector(".epos-einheit")!.TextContent);
        Assert.Equal("60,00", p1.QuerySelector("input")!.GetAttribute("value"));   // zwei Stellen
        Assert.Equal("90,00", p2.QuerySelector("input")!.GetAttribute("value"));

        // Die Erklärzeile sagt, woran bemessen wird und was die Staffel ersetzt.
        Assert.Contains("Viertelstundenspitze", cut.Markup);
        Assert.Contains("ersetzt Leistungspreis und saisonale Sätze", cut.Markup);
    }

    [Fact]
    public void Ohne_Freigabe_der_Huelle_fehlt_die_Staffel()
    {
        var cut = Zeige(Strom(mitStaffel: false));

        Assert.DoesNotContain("Leistungspreis-Staffel", cut.Markup);
        Assert.DoesNotContain("Staffelgrenze", cut.Markup);
        Assert.DoesNotContain("Viertelstundenspitze", cut.Markup);
    }

    [Fact]
    public void Eine_Eingabe_schreibt_in_den_Stand_und_ein_leeres_Feld_heisst_nicht_gepflegt()
    {
        EnergietraegerStand stand = Strom(mitStaffel: true);
        var cut = Zeige(stand);

        Feld(cut, "Staffelgrenze").QuerySelector("input")!.Input("2000");
        Assert.Equal(2000.0, stand.StaffelGrenze);
        Assert.True(_geaendert > 0);

        Feld(cut, "Preis über der Grenze").QuerySelector("input")!.Input("");
        Assert.Null(stand.StaffelPreis2);
        Assert.Equal(60.0, stand.StaffelPreis1);   // die übrigen Felder bleiben
    }
}
