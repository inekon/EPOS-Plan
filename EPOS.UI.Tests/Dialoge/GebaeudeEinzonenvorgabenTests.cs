using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Vorgaben der Baualtersklasse übernehmen…“ im Gebäudeeditor</b> (Welle MA-7, Entwurf Geführter Modellaufbau 2.1): der
/// Knopf im Reiter „Gebäude und Hülle“, die Sperre mit Grund (Zonen, keine Klasse), die Vorschau alt → neu je Feld, die
/// Übernahme nur auf Klick in den Arbeitsstand (das OK schreibt), die Rücknahme und das Profil der Gebäudeart über den Generator
/// der Konditionierung. Kultur de-DE (deutsche Texte und Zahlen).
/// </summary>
public class GebaeudeEinzonenvorgabenTests : EposBunitContext
{
    private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
    private const string KNOPF = "Vorgaben der Baualtersklasse übernehmen…";

    public GebaeudeEinzonenvorgabenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus V",
        Konditionierung = new KonditionierungDaten(),
        Typ = "Einfamilienhaus",
        Beschreibung = "Beschreibung",
        Waermegewinne = 400,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 15,
        SonstigeFlaechen = 5,
        WbvkFensterWand = 0.1,
        AnschlussFensterWand = 50,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        WwBedarf = 700,
        Gebaeudeart = "Einfamilienhaus",
        Verwendung = "Wohngebaeude",
        Baualtersklasse = 4,
        Bauart = Gebaeudebauweise.LEICHT,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Fensterdurchlassgrad = 0.4,
        Raumhoehe = 2.5,
        FlaecheAussenwand = 200,
        Dachflaeche = 120,
        Grundflaeche = 100,
        UWertAussenwand = 0.3,
        UWertFenster = 1.3,
        UWertDachflaeche = 0.2,
        UWertGrundflaeche = 0.35,
        UWertSonstiges = 0.5,
        SollTag = 20,
        NachtAbsenkung = 17,
        MaxTemperatur = 24,
        Luftwechselrate = 0.5,
    };

    private readonly List<GebaeudeKatalogDaten> _gespeichert = new();

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(GebaeudeKatalogDaten? daten = null, KonditionierungWeg? kond = null,
                                                               GebaeudeZonenweg? zonen = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Konditionierung, kond)
            .Add(x => x.Zonen, zonen)
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Einfamilienhaus", "Hotel" })
            .Add(x => x.Baualtersklassen, Enumerable.Range(0, 13).Select(i => ((char)('A' + i)).ToString()).ToArray())
            .Add(x => x.Speichern, (d, _, _) => { _gespeichert.Add(d.Kopie()); return new GebaeudeKatalogErgebnis(true, ""); }));

    private static IElement Knopf(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
    {
        cut.Find(".epos-leiste button.epos-knopf--primaer").Click();
        Assert.True(_gespeichert.Count == 1, "Nicht gespeichert: " + string.Join(" | ", cut.FindAll(".epos-warnbanner, [role=alert]").Select(e => e.TextContent.Trim())));
    }

    private static string U(double? w) => w!.Value.ToString("N2", DE) + " W/(m²K)";

    [Fact]
    public void Der_Knopf_steht_im_ersten_Reiter_und_oeffnet_die_Vorschau_alt_neu()
    {
        var cut = Aufbauen();
        Einzonenvorgabe v = Einzonenvorgaben.Fuer(4)!;
        IElement knopf = Knopf(cut, KNOPF);
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-gebk-vorgaben-grund"));

        knopf.Click();
        Assert.True(cut.Instance.VorgabenOffen);
        Assert.Contains("Vorgabe Klasse E", cut.Find(".epos-gebk-vorgaben-herkunft").TextContent);
        IElement wand = cut.Find(".epos-gebk-vorgaben-tabelle tr[data-feld=UAussenwand]");
        Assert.Equal(U(0.3), wand.QuerySelectorAll("td")[0].TextContent.Trim());
        Assert.Equal(U(v.Werte.UAussenwand), wand.QuerySelectorAll("td")[1].TextContent.Trim());
        IElement bauart = cut.Find(".epos-gebk-vorgaben-tabelle tr[data-feld=Bauart]");
        Assert.Contains("aendert", bauart.ClassName);
        // Die Vorschau ändert nichts: Abbrechen, OK - der Satz bleibt.
        Knopf(cut, "Abbrechen").Click();
        Assert.False(cut.Instance.VorgabenOffen);
        Ok(cut);
        Assert.Equal(0.3, _gespeichert.Single().UWertAussenwand);
        Assert.Equal(Gebaeudebauweise.LEICHT, _gespeichert.Single().Bauart);
    }

    [Fact]
    public void Uebernehmen_traegt_die_Vorgabe_ein_und_das_OK_schreibt_sie()
    {
        var cut = Aufbauen();
        Einzonenvorgabe v = Einzonenvorgaben.Fuer(4)!;
        Knopf(cut, KNOPF).Click();
        Knopf(cut, "Übernehmen").Click();
        Assert.False(cut.Instance.VorgabenOffen);
        Assert.Contains("Vorgaben E übernommen", cut.Markup);
        Ok(cut);
        GebaeudeKatalogDaten d = _gespeichert.Single();
        Assert.Equal(v.Werte.UAussenwand, d.UWertAussenwand);
        Assert.Equal(v.Werte.UFenster, d.UWertFenster);
        Assert.Equal(v.Werte.UDach, d.UWertDachflaeche);
        Assert.Equal(v.Werte.UGrund, d.UWertGrundflaeche);
        Assert.Equal(v.Werte.Fensterdurchlassgrad, d.Fensterdurchlassgrad);
        Assert.Equal(Gebaeudebauweise.SCHWER, d.Bauart);
        Assert.Equal(Gebaeudebauweise.BauweiseAusBauart(Gebaeudebauweise.SCHWER, 150), d.Bauweise);
        // Flächen bleiben.
        Assert.Equal(200, d.FlaecheAussenwand);
    }

    [Fact]
    public void Zuruecknehmen_stellt_den_Stand_davor_wieder_her()
    {
        var cut = Aufbauen();
        Knopf(cut, KNOPF).Click();
        Knopf(cut, "Übernehmen").Click();
        Knopf(cut, "Vorgaben zurücknehmen").Click();
        Assert.Empty(cut.FindAll(".epos-gebk-vorgaben-zurueck"));
        Ok(cut);
        GebaeudeKatalogDaten d = _gespeichert.Single();
        Assert.Equal(0.3, d.UWertAussenwand);
        Assert.Equal(1.3, d.UWertFenster);
        Assert.Equal(0.4, d.Fensterdurchlassgrad);
        Assert.Equal(Gebaeudebauweise.LEICHT, d.Bauart);
    }

    [Fact]
    public void Ohne_Baualtersklasse_ist_der_Knopf_gesperrt_mit_Grund()
    {
        GebaeudeKatalogDaten s = Satz();
        s.KlasseGesetzt = false;
        var cut = Aufbauen(s);
        IElement knopf = Knopf(cut, KNOPF);
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains("Keine Baualtersklasse gesetzt", cut.Find(".epos-gebk-vorgaben-grund").TextContent);
        Assert.Contains("Keine Baualtersklasse gesetzt", knopf.GetAttribute("title"));
    }

    [Fact]
    public void Mit_Zonen_ist_der_Knopf_gesperrt_mit_Grund()
    {
        var zonen = new GebaeudeZonenweg
        {
            Katalog = true,
            Zonen = new[] { new ZoneDaten { Id = 1, Bezeichner = "Erdgeschoss", Nutzflaeche = 100 } },
            Speichern = _ => ZonenSchreibergebnis.Gut,
        };
        var cut = Aufbauen(zonen: zonen);
        Assert.True(Knopf(cut, KNOPF).HasAttribute("disabled"));
        Assert.Contains("Zonenweg", cut.Find(".epos-gebk-vorgaben-grund").TextContent);
    }

    [Fact]
    public void Das_Profil_der_Gebaeudeart_geht_ueber_den_Generator_der_Konditionierung()
    {
        var profile = new (string, Raumnutzungsprofil)[] { ("Eigene", NutzungsprofilUebernahmeTests.Buero()) };
        string? gefragt = null;
        KonditionierungWeg kond = KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Katalogbau, projekt: false, profile: profile,
                                                                  profilDerArt: art => { gefragt = art; return 11; });
        var cut = Aufbauen(kond: kond);
        Knopf(cut, KNOPF).Click();
        Assert.Equal("Einfamilienhaus", gefragt);
        Assert.Contains("„Büro Probe“", cut.Find(".epos-gebk-vorgaben-profil").TextContent.Replace("1 · ", ""));
        Knopf(cut, "Profil übernehmen…").Click();
        // Die Rückfrage der Konditionierung steht im Reiter „Konditionierung“.
        Assert.False(cut.Instance.VorgabenOffen);
        Assert.Equal("KONDITIONIERUNG", cut.Instance.AktiverReiter);
        Assert.NotEmpty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Ohne_Profilregel_nennt_das_Blatt_den_Grund()
    {
        var profile = new (string, Raumnutzungsprofil)[] { ("Eigene", NutzungsprofilUebernahmeTests.Buero()) };
        KonditionierungWeg kond = KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Katalogbau, projekt: false, profile: profile,
                                                                  profilDerArt: _ => null);
        var cut = Aufbauen(kond: kond);
        Knopf(cut, KNOPF).Click();
        Assert.Contains("gibt kein Nutzungsprofil vor", cut.Find(".epos-gebk-vorgaben-profilgrund").TextContent);
        Assert.Empty(cut.FindAll(".epos-gebk-vorgaben-profilknopf"));
    }
}
