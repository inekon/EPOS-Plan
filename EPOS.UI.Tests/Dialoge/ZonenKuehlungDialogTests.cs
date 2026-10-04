using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Kühlgruppe des Zonendialogs (KU3-3, E67/E68): der Schalter mit „wie Gebäude" als Leerwert, die
/// Leistungsgrenze mit dem Flächenanteil des Gebäudes als Platzhalter, die Kühlspalte der Zonenmatrix je nach
/// wirksamem Schalter, der Rundlauf in den Arbeitsstand und die Texte in beiden Sprachen.
/// </summary>
public class ZonenKuehlungDialogTests : EposBunitContext
{
    public ZonenKuehlungDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Das Gebäude: gekühlt (Kühlsollwert 26 °C, Grenze 8 kW) oder nicht, 200 m².</summary>
    private static GebaeudeArbeitsstand Gebaeude(bool gekuehlt = true)
    {
        var d = new GebaeudeKatalogDaten
        {
            Name = "Haus", WohnflaecheGesamt = 200, SollTag = 20, NachtAbsenkung = 16,
            KuehlungAktiv = gekuehlt, KuehlSollwert = 26, KuehlleistungMax = 8
        };
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(d, neu: false);
        return arbeit;
    }

    private IRenderedComponent<ZonenDialog> Aufbauen(ZoneDaten? zone = null, GebaeudeArbeitsstand? arbeit = null,
                                                      ZonenDialogTexte? texte = null, Action<ZoneDaten?>? geschlossen = null)
    {
        arbeit ??= Gebaeude();
        return Render<ZonenDialog>(p =>
        {
            p.Add(x => x.Zone, zone ?? new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 50 })
             .Add(x => x.NutzflaecheGebaeude, 200.0)
             .Add(x => x.Gebaeude, arbeit.Vorgaben)
             .Add(x => x.Gebaeudestand, arbeit)
             .Add(x => x.Zonenzahl, 2)
             .Add(x => x.Geschlossen, z => geschlossen?.Invoke(z));
            if (texte is not null) p.Add(x => x.Texte, texte);
        });
    }

    private static IElement? FeldOderNichts(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  ?.QuerySelector("input, select");

    private static IElement Kuehlung(IRenderedComponent<ZonenDialog> cut) => cut.Find(".epos-zonenkuehlung");

    [Fact]
    public void Leerwert_ist_wie_Gebaeude_und_nennt_den_Schalter_des_Gebaeudes()
    {
        var cut = Aufbauen();
        IElement schalter = FeldOderNichts(Kuehlung(cut), "Zone wird gekühlt")!;
        Assert.Equal("0", schalter.GetAttribute("value"));
        Assert.Equal("wie Gebäude (Ja)", schalter.QuerySelector("option[value=\"0\"]")!.TextContent.Trim());
        Assert.Null(cut.Instance.Arbeitsstand.KuehlungAktiv);
        Assert.True(cut.Instance.KuehlungAktivWirksam);
        // Die Grenze: leer = der Flächenanteil der Gebäudegrenze (8 kW × 50/200).
        IElement grenze = FeldOderNichts(Kuehlung(cut), "Kühlleistungsgrenze")!;
        Assert.Contains("2", grenze.GetAttribute("placeholder"));
        Assert.Contains("Spalte „Kühlen\"", cut.Markup);
    }

    [Fact]
    public void Nein_an_der_Zone_schaltet_aus_und_sperrt_die_Kuehlspalte()
    {
        var cut = Aufbauen();
        FeldOderNichts(Kuehlung(cut), "Zone wird gekühlt")!.Change("2");
        Assert.False(cut.Instance.Arbeitsstand.KuehlungAktiv);
        Assert.False(cut.Instance.KuehlungAktivWirksam);
        Assert.Null(FeldOderNichts(Kuehlung(cut), "Kühlleistungsgrenze"));

        FeldOderNichts(Kuehlung(cut), "Zone wird gekühlt")!.Change("0");
        Assert.Null(cut.Instance.Arbeitsstand.KuehlungAktiv);
        Assert.NotNull(FeldOderNichts(Kuehlung(cut), "Kühlleistungsgrenze"));
    }

    [Fact]
    public void Ja_an_der_Zone_kuehlt_auch_wenn_das_Gebaeude_nicht_kuehlt()
    {
        var cut = Aufbauen(arbeit: Gebaeude(gekuehlt: false));
        IElement schalter = FeldOderNichts(Kuehlung(cut), "Zone wird gekühlt")!;
        Assert.Equal("wie Gebäude (Nein)", schalter.QuerySelector("option[value=\"0\"]")!.TextContent.Trim());
        Assert.Null(FeldOderNichts(Kuehlung(cut), "Kühlleistungsgrenze"));
        schalter.Change("1");
        Assert.True(cut.Instance.Arbeitsstand.KuehlungAktiv);
        Assert.NotNull(FeldOderNichts(Kuehlung(cut), "Kühlleistungsgrenze"));
    }

    [Fact]
    public void Die_Grenze_der_Zone_geht_in_den_Arbeitsstand_und_zurueck()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(geschlossen: z => zurueck = z);
        FeldOderNichts(Kuehlung(cut), "Kühlleistungsgrenze")!.Input("1,5");
        Assert.Equal(1.5, cut.Instance.Arbeitsstand.KuehlleistungMaxKw);
        Zoneneingaben e = cut.Instance.Arbeitsstand.Eingaben();
        Assert.Equal(1.5, e.KuehlleistungMaxKw);
        Assert.Null(e.KuehlungAktiv);
        cut.Find(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Click();
        Assert.NotNull(zurueck);
        Assert.Equal(1.5, zurueck!.KuehlleistungMaxKw);
    }

    [Fact]
    public void Unbeheizte_Zone_zeigt_keine_Kuehlfelder()
    {
        var cut = Aufbauen(zone: new ZoneDaten { Id = 7, Bezeichner = "Keller", Nutzflaeche = 50, IstBeheizt = false });
        Assert.Empty(cut.FindAll(".epos-zonenkuehlung"));
    }

    [Fact]
    public void Die_Texte_der_Kuehlgruppe_in_beiden_Sprachen()
    {
        ZonenDialogTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new ZonenDialogTexte();
        var deutsch = new ZonenDialogTexte();
        Assert.Equal("Kühlung der Zone", deutsch.KuehlGruppe);
        Assert.Equal("Zone cooling", englisch.KuehlGruppe);
        Assert.Equal("Zone is cooled", englisch.KuehlLabelAktiv);
        Assert.Equal("Cooling power limit", englisch.KuehlLabelLeistungMax);
        Assert.NotEqual(deutsch.KuehlZeile, englisch.KuehlZeile);

        var cut = Aufbauen(texte: englisch);
        Assert.NotNull(FeldOderNichts(Kuehlung(cut), "Zone is cooled"));
        Assert.Contains("Zone cooling", cut.Markup);
    }
}
