using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Kopplung der Zonen im Gebäudeeditor</b> (Gebäudesimulation G6b, Welle W2): der Zonendialog
/// bekommt die Vorgaben des Gebäudes, die übrigen Zonen als Nachbarn (auch vorläufige Ids) und die
/// Trennflächen, die eine andere Zone mit ihm führt; ohne Schemaschritt S-G bietet der Bauteildialog
/// keine Nachbarzone an.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und Zahlen.</para>
/// </summary>
public class GebaeudeZonenkopplungTests : EposBunitContext
{
    public GebaeudeZonenkopplungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz (wie in <see cref="GebaeudeZonenlisteTests"/>) mit 10 kW Heizleistungsgrenze.</summary>
    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5, HeizleistungMax = 10
    };

    private static ZoneDaten Zone(int id, string name, double? flaeche)
        => new()
        {
            Id = id, Bezeichner = name, Nutzflaeche = flaeche,
            Bauteile =
            {
                new BauteilDaten { Id = id * 10 + 1, Bezeichner = "Wand " + name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                   Flaeche = 100, UWert = 0.3, Azimut = 180 },
                new BauteilDaten { Id = id * 10 + 2, Bezeichner = "Dach " + name, Bauteilart = DbWerte.BAUTEILART_DACH,
                                   Flaeche = 50, UWert = 0.2 }
            }
        };

    /// <summary>
    /// Zwei gespeicherte Zonen, 60 + 90 = 150 m²; das Erdgeschoss führt die Decke zum Obergeschoss als
    /// Trennfläche.
    /// </summary>
    private static ZoneDaten[] ZweiGekoppelt()
    {
        ZoneDaten eg = Zone(1, "Erdgeschoss", 60);
        eg.Bauteile.Add(new BauteilDaten
        {
            Id = 13, Bezeichner = "Decke EG/OG", Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = 60, UWert = 0.9,
            Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, IdNachbarzone = 2
        });
        return new[] { eg, Zone(2, "Obergeschoss", 90) };
    }

    private sealed class Weg
    {
        internal string? KopplungSperre;

        internal GebaeudeZonenweg Zonenweg(IReadOnlyList<ZoneDaten>? zonen) => new()
        {
            Zonen = zonen ?? Array.Empty<ZoneDaten>(),
            Speichern = _ => "",
            MehrereZonenFreigegeben = true,
            KopplungSperre = KopplungSperre
        };
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(Weg weg, IReadOnlyList<ZoneDaten>? zonen = null)
    {
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, (d, neu, name) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Zonen, weg.Zonenweg(zonen)));
        cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == "Zonen").Click();
        return cut;
    }

    private static IReadOnlyList<IElement> Knoepfe(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").Where(b => b.TextContent.Trim() == text).ToList();

    private static IElement? Feld(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  ?.QuerySelector("input, select");

    // =================================================================================
    // Der Zonendialog im Gebäudeeditor
    // =================================================================================

    /// <summary>
    /// Die Vorgaben kommen aus dem Arbeitsstand des Gebäudes: Sollwert unverändert, die Leistungsgrenze
    /// ab zwei Zonen nach dem Flächenanteil (60 / 150 = 40 %).
    /// </summary>
    [Fact]
    public void Der_Zonendialog_zeigt_die_Vorgaben_des_Gebaeudes()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Öffnen…")[0].Click();
        IElement zone = cut.Find(".epos-zonendialog");

        Assert.Equal("Vorgabe: 20", Feld(zone, "Soll am Tag")!.GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 4 (40 % des Gebäudes)", Feld(zone, "Heizleistungsgrenze")!.GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 160 (40 % des Gebäudes)", Feld(zone, "Interne Wärmegewinne")!.GetAttribute("placeholder"));
    }

    /// <summary>
    /// Das Obergeschoss zeigt die Decke, die das Erdgeschoss führt, gespiegelt und nur zum Lesen; sein
    /// Bauteildialog bietet das Erdgeschoss als Nachbarzone — nicht sich selbst.
    /// </summary>
    [Fact]
    public void Der_Zonendialog_zeigt_die_Gegenseite_und_bietet_die_Nachbarzonen()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Öffnen…")[0].Click();
        IElement trenn = cut.FindAll(".epos-zonenbauteil").Last();
        Assert.Contains("Obergeschoss", trenn.TextContent);
        Assert.Empty(cut.FindAll(".epos-zonengegenseite"));
        cut.FindAll(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Single().Click();

        Knoepfe(cut, "Öffnen…")[1].Click();
        IElement gegen = cut.Find(".epos-zonengegenseite");
        Assert.Contains("Decke EG/OG", gegen.TextContent);
        Assert.Contains("geführt von Erdgeschoss", gegen.TextContent);

        Knoepfe(cut, "+ Neues Bauteil …").Single().Click();
        IElement bauteil = cut.Find(".epos-bauteildialog");
        Feld(bauteil, "Randbedingung")!.Change("4");
        IElement nachbar = Feld(cut.Find(".epos-bauteildialog"), "Nachbarzone")!;
        Assert.Contains("Erdgeschoss", nachbar.TextContent);
        Assert.DoesNotContain("Obergeschoss", nachbar.TextContent);
    }

    /// <summary>Eine neue, noch ungespeicherte Zone steht mit ihrer vorläufigen Id als Nachbar zur Wahl.</summary>
    [Fact]
    public void Eine_neue_Zone_steht_mit_vorlaeufiger_Id_als_Nachbar_zur_Wahl()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Duplizieren")[1].Click();
        ZoneDaten kopie = cut.Instance.ZonenImArbeitsstand[2];
        Assert.True(kopie.Id < 0);

        Knoepfe(cut, "Öffnen…")[0].Click();
        Knoepfe(cut, "+ Neues Bauteil …").Single().Click();
        Feld(cut.Find(".epos-bauteildialog"), "Randbedingung")!.Change("4");
        IElement nachbar = Feld(cut.Find(".epos-bauteildialog"), "Nachbarzone")!;
        Assert.Contains(nachbar.QuerySelectorAll("option"),
                        o => o.GetAttribute("value") == kopie.Id.ToString(System.Globalization.CultureInfo.CurrentCulture)
                             && o.TextContent.Contains(kopie.Bezeichner));
    }

    /// <summary>Ohne Schemaschritt S-G (etwa iOS) bietet der Bauteildialog keine Nachbarzone an.</summary>
    [Fact]
    public void Ohne_Schemaschritt_bietet_der_Bauteildialog_keine_Nachbarzone()
    {
        var cut = Aufbauen(new Weg { KopplungSperre = "Schemaschritt 147 fehlt." }, new[] { Zone(1, "Erdgeschoss", 60), Zone(2, "Obergeschoss", 90) });

        Knoepfe(cut, "Öffnen…")[0].Click();
        Knoepfe(cut, "+ Neues Bauteil …").Single().Click();

        Assert.DoesNotContain("Nachbarzone", Feld(cut.Find(".epos-bauteildialog"), "Randbedingung")!.TextContent);
        Assert.Contains("Schemaschritt 147 fehlt.", cut.Find(".epos-bauteildialog").TextContent);
    }
}
