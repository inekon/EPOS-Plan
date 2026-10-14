using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Spalte „Nutzung/Profil“ der Zonenliste</b> (GD-5, Entwurf Gebäudedialog Mehrzonen 2.5): je Zone das Profil mit den
/// Größen, die sie vom Gebäude erbt („geerbt“, auch ohne Kennwert des Profils), ohne Profil „geerbt vom Gebäude“ samt der
/// Vorgabe der Gebäudeart, eine unbeheizte Zone ohne Heiz- und Kühlkalender; die Aktion „Profil/Kalender zuweisen…“ öffnet die
/// Zone. Kulturpinnung über <see cref="EposBunitContext"/>.
/// </summary>
public class GebaeudeZonennutzungTests : EposBunitContext
{
    public GebaeudeZonennutzungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeKatalogDaten Satz(string art) => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = art,
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5
    };

    private static ZoneDaten Zone(int id, string name, double flaeche, bool beheizt = true) => new()
    {
        Id = id, Bezeichner = name, Nutzflaeche = flaeche, IstBeheizt = beheizt,
        Bauteile = { new BauteilDaten { Id = id * 10 + 1, Bezeichner = "Wand " + name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                        Flaeche = 100, UWert = 0.3, Azimut = 180 } }
    };

    /// <summary>Eine Konditionierung mit eigenem Kalender für alle Größen außer <paramref name="geerbt"/>.</summary>
    private static KonditionierungDaten Eigen(params KonditionierungGroesse[] geerbt)
    {
        var k = new KonditionierungDaten();
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            k.Spalte(g).Kalender.Zustand = geerbt.Contains(g) ? KonditionierungZustand.VomGebaeude : KonditionierungZustand.Angelegt;
        return k;
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(IReadOnlyList<ZoneDaten> zonen, string art, Func<string?, string?>? vorgabe)
    {
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz(art))
            .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { art })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, (d, neu, name) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Zonen, new GebaeudeZonenweg
            {
                Zonen = zonen,
                Vorgabeprofil = vorgabe,
                Speichern = _ => ZonenSchreibergebnis.Gut,
            }));
        cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == "Zonen").Click();
        return cut;
    }

    private static IReadOnlyList<IElement> Zellen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("table.epos-zonenliste tbody td.epos-zonennutzung");

    [Fact]
    public void Die_Spalte_nennt_Profil_geerbte_Groessen_die_Vorgabe_der_Gebaeudeart_und_unbeheizt()
    {
        ZoneDaten wohnen = Zone(1, "Wohnen EG", 60);
        wohnen.Nutzungsprofil = "Wohnen";
        wohnen.Konditionierung = Eigen(KonditionierungGroesse.Lueftung, KonditionierungGroesse.Geraete);
        ZoneDaten ohne = Zone(2, "Obergeschoss", 50);
        ohne.Konditionierung = Eigen(KonditionierungDaten.Alle.ToArray());
        ZoneDaten keller = Zone(3, "Keller", 40, beheizt: false);
        keller.Nutzungsprofil = "Lager";
        keller.Konditionierung = Eigen();
        var gefragt = new List<string?>();

        var cut = Aufbauen(new[] { wohnen, ohne, keller }, "Mehrfamilienhaus", a => { gefragt.Add(a); return "Wohnen"; });

        Assert.Contains(cut.FindAll("table.epos-zonenliste thead th"), th => th.TextContent.Trim() == "Nutzung/Profil");
        IReadOnlyList<IElement> zellen = Zellen(cut);
        Assert.Equal(3, zellen.Count);
        Assert.Equal("Wohnen · geerbt: Lüftung, Geräte", zellen[0].TextContent.Trim());
        Assert.Equal("teil", zellen[0].GetAttribute("data-geerbt"));
        Assert.Equal("geerbt vom Gebäude · Vorgabe (Profil der Gebäudeart): Wohnen", zellen[1].TextContent.Trim());
        Assert.Equal("ganz", zellen[1].GetAttribute("data-geerbt"));
        Assert.Equal("Lager · unbeheizt — ohne Heiz- und Kühlkalender", zellen[2].TextContent.Trim());
        Assert.Equal("nein", zellen[2].GetAttribute("data-geerbt"));
        Assert.Contains("Mehrfamilienhaus", gefragt);
    }

    [Fact]
    public void Ohne_Vorgabe_und_ohne_Konditionierung_erbt_die_Zone_alles_vom_Gebaeude()
    {
        ZoneDaten zone = Zone(1, "Halle", 100);
        zone.Nutzungsprofil = "Büro";
        var cut = Aufbauen(new[] { zone, Zone(2, "Nebenraum", 20) }, "Hotel", null);

        IReadOnlyList<IElement> zellen = Zellen(cut);
        Assert.Equal("Büro · geerbt vom Gebäude", zellen[0].TextContent.Trim());
        Assert.Equal("geerbt vom Gebäude", zellen[1].TextContent.Trim());
    }

    [Fact]
    public void Profil_Kalender_zuweisen_oeffnet_die_Zone_im_Zonendialog()
    {
        var cut = Aufbauen(new[] { Zone(1, "Erdgeschoss", 60), Zone(2, "Obergeschoss", 50) }, "Einfamilienhaus", _ => "Wohnen");
        IReadOnlyList<IElement> knoepfe = cut.FindAll("button.epos-zone-profil");
        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Profil/Kalender zuweisen…", knoepfe[1].TextContent.Trim());
        Assert.Equal("Obergeschoss – Profil/Kalender zuweisen…", knoepfe[1].GetAttribute("aria-label"));

        knoepfe[1].Click();

        Assert.True(cut.Instance.ZonendialogOffen);
        Assert.NotEmpty(cut.FindAll(".epos-zonendialog"));
    }
}
