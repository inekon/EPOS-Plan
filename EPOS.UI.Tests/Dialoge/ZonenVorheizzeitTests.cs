using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Vorheizzeit der Zone im Zonendialog (Entwurf Vorheizrampe 2.9, Welle AH-V5): Feld 1–47 h, leer = wie Gebäude,
/// die Zeile zur Wirkung, OK gibt den Wert zurück, ein gespeicherter Wert außerhalb hält das OK mit Meldung an, und
/// der Assistent kennt das Feld. Kultur de-DE (<see cref="EposBunitContext"/>).
/// </summary>
public class ZonenVorheizzeitTests : EposBunitContext
{
    public ZonenVorheizzeitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<ZonenDialog> Aufbauen(ZoneDaten zone, Action<ZoneDaten?>? geschlossen = null)
    {
        var d = new GebaeudeKatalogDaten { Name = "Haus", WohnflaecheGesamt = 200, SollTag = 20, NachtAbsenkung = 16 };
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(d, neu: false);
        return Render<ZonenDialog>(p => p
            .Add(x => x.Zone, zone)
            .Add(x => x.NutzflaecheGebaeude, 200.0)
            .Add(x => x.Gebaeude, arbeit.Vorgaben)
            .Add(x => x.Gebaeudestand, arbeit)
            .Add(x => x.Geschlossen, z => geschlossen?.Invoke(z)));
    }

    private static IElement? Feld(IRenderedComponent<ZonenDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-feld")
              .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              ?.QuerySelector("input");

    private static void Ok(IRenderedComponent<ZonenDialog> cut)
        => cut.Find(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Click();

    [Fact]
    public void Das_Feld_steht_leer_mit_wie_Gebaeude_und_OK_gibt_den_Wert_zurueck()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150 }, z => zurueck = z);

        IElement feld = Feld(cut, "Vorheizzeit (h)")!;
        Assert.Equal("wie Gebäude", feld.GetAttribute("placeholder"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.ZONDLG_HRL_VORHEIZZEIT, cut.Markup);

        feld.Input("5");
        Ok(cut);
        Assert.NotNull(zurueck);
        Assert.Equal(5, zurueck!.VorheizzeitH);
        Assert.Equal(5, zurueck.Eingaben().VorheizzeitH);
    }

    [Fact]
    public void Eine_getippte_Zahl_ausserhalb_faerbt_und_haelt_das_OK_an()
    {
        bool geschlossen = false;
        var cut = Aufbauen(new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150 }, _ => geschlossen = true);
        Feld(cut, "Vorheizzeit (h)")!.Input("48");
        Assert.Contains("epos-fehleingabe", Feld(cut, "Vorheizzeit (h)")!.ClassName);
        Ok(cut);
        Assert.False(geschlossen);
    }

    [Fact]
    public void Ein_gespeicherter_Wert_ausserhalb_haelt_das_OK_mit_Meldung_an()
    {
        bool geschlossen = false;
        var cut = Aufbauen(new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150, VorheizzeitH = 60 },
                           _ => geschlossen = true);
        Ok(cut);
        Assert.False(geschlossen);
        Assert.Contains("Vorheizzeit", cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_unbeheizte_Zone_zeigt_das_Feld_nicht()
    {
        var cut = Aufbauen(new ZoneDaten { Id = 7, Bezeichner = "Keller", Nutzflaeche = 50, IstBeheizt = false });
        Assert.Null(Feld(cut, "Vorheizzeit (h)"));
    }
}
