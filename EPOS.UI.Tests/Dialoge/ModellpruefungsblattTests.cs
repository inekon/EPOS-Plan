using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Ein Probeweg des Blatts „Modell prüfen“ (MA-1b) ohne Datenbank: fünf Gruppen, Gruppe 2 mit zwei Befunden und Vorschlägen;
/// „Alle übernehmen“ und „Übernehmen“ leeren die Gruppe und machen sie zurücknehmbar, „Zurücknehmen“ stellt sie wieder her.
/// </summary>
internal sealed class ModellpruefungProbeweg
{
    public int Geprueft, Alle, Einzeln, Zurueck, Nord;
    private bool _uebernommen;

    public ModellpruefungStand Stand()
    {
        ModellpruefungGruppenzeile Gruppe(Pruefgruppe g, Pruefampel a, Pruefsprungziel z, params ModellpruefungBefundzeile[] b)
            => new(g, "Gruppe " + (int)g, a, b.Length, b.Count(x => x.Vorschlag.Length > 0), z,
                   _uebernommen && g == Pruefgruppe.AufbautenStoffwerte, b);
        var befunde = _uebernommen ? Array.Empty<ModellpruefungBefundzeile>() : new[]
        {
            new ModellpruefungBefundzeile("Nur U-Wert", Pruefampel.Gelb, "Zone 1 — Außenwand Nord", "Aufbau Mauerwerk"),
            new ModellpruefungBefundzeile("Nur U-Wert", Pruefampel.Gelb, "Zone 1 — Dach", "Aufbau Dach"),
        };
        return new ModellpruefungStand(_uebernommen ? Rechenstatus.Vollstaendig : Rechenstatus.RechnetMitVorgaben,
                                       _uebernommen ? "vollständig" : "rechnet mit Vorgaben", new[]
        {
            Gruppe(Pruefgruppe.GeometrieBauteile, Pruefampel.Gruen, Pruefsprungziel.Randbedingung),
            Gruppe(Pruefgruppe.AufbautenStoffwerte, _uebernommen ? Pruefampel.Gruen : Pruefampel.Gelb, Pruefsprungziel.Aufbau, befunde),
            Gruppe(Pruefgruppe.NutzungKonditionierung, Pruefampel.Gruen, Pruefsprungziel.Zonen),
            Gruppe(Pruefgruppe.OrtLage, Pruefampel.Gelb, Pruefsprungziel.Befund,
                   new ModellpruefungBefundzeile("Nordrichtung angenommen", Pruefampel.Gelb, "", "")),
            Gruppe(Pruefgruppe.Rechenfaehigkeit, Pruefampel.Gruen, Pruefsprungziel.Befund),
        });
    }

    public ModellpruefungWeg Weg(bool mitNord = true) => new()
    {
        Gebaeudename = "Probehaus",
        Pruefen = () => { Geprueft++; return Stand(); },
        AlleUebernehmen = () => { Alle++; _uebernommen = true; return new ModellpruefungAusgang(true, "2 übernommen", Stand()); },
        Uebernehmen = _ => { Einzeln++; _uebernommen = true; return new ModellpruefungAusgang(true, "2 übernommen", Stand()); },
        Zuruecknehmen = _ => { Zurueck++; _uebernommen = false; return new ModellpruefungAusgang(true, "zurück", Stand()); },
        NordrichtungBestaetigen = mitNord ? () => { Nord++; return new ModellpruefungAusgang(true, R.MPB_MSG_NORD_BESTAETIGT, Stand()); } : null,
    };
}

/// <summary>
/// MA-1b: das Blatt „Modell prüfen“ — Status und fünf Gruppen mit Zählung, „Alle Vorschläge übernehmen“ über den Weg mit neuem
/// Status, Zurücknehmen nur wenn möglich, Befundliste erst auf Klick, Ansicht im Sprungziel oder gesperrt mit Grund, Gruppe 4.
/// </summary>
public sealed class ModellpruefungsblattTests : EposBunitContext
{
    public ModellpruefungsblattTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<Modellpruefungsblatt> Blatt(ModellpruefungProbeweg p, string? ansichtSperre = null,
                                                          List<Pruefsprungziel>? spruenge = null, Action? simulation = null,
                                                          bool mitNord = true)
        => Render<Modellpruefungsblatt>(c =>
        {
            c.Add(x => x.Weg, p.Weg(mitNord)).Add(x => x.AnsichtSperre, ansichtSperre)
             .Add(x => x.Ansicht, z => spruenge?.Add(z));
            if (simulation is not null) c.Add(x => x.Simulation, simulation);
        });

    private static string Zaehlung(int n, int m) => string.Format(System.Globalization.CultureInfo.CurrentCulture, R.MPB_ZAEHLUNG, n, m);

    [Fact]
    public void Zeigt_Status_und_fuenf_Gruppen_mit_Ampel_und_Zaehlung()
    {
        var p = new ModellpruefungProbeweg();
        var cut = Blatt(p);
        Assert.Equal(1, p.Geprueft);
        Assert.Equal("rechnet mit Vorgaben", cut.Find(".epos-modellpruefung-status").TextContent.Trim());
        Assert.Contains("epos-ampel-gelb", cut.Find(".epos-modellpruefung-status").ClassName);
        Assert.Equal("Probehaus", cut.Find(".epos-modellpruefung-gebaeude").TextContent);
        var gruppen = cut.FindAll(".epos-modellpruefung-gruppe");
        Assert.Equal(5, gruppen.Count);
        Assert.Contains("epos-ampel-gelb", gruppen[1].QuerySelector(".epos-ampel")!.ClassName);
        Assert.Contains("epos-ampel-gruen", gruppen[0].QuerySelector(".epos-ampel")!.ClassName);
        Assert.Equal(Zaehlung(2, 2), gruppen[1].QuerySelector(".epos-modellpruefung-zaehlung")!.TextContent);
        Assert.Equal(Zaehlung(0, 0), gruppen[0].QuerySelector(".epos-modellpruefung-zaehlung")!.TextContent);
        // „Vorschlag übernehmen“ nur mit Vorschlag; „Zurücknehmen“ erst nach einer Übernahme.
        Assert.True(gruppen[0].QuerySelector("button.epos-modellpruefung-uebernehmen")!.HasAttribute("disabled"));
        Assert.False(gruppen[1].QuerySelector("button.epos-modellpruefung-uebernehmen")!.HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("button.epos-modellpruefung-zuruecknehmen"));
    }

    [Fact]
    public void Befunde_erst_auf_Klick()
    {
        var cut = Blatt(new ModellpruefungProbeweg());
        Assert.Empty(cut.FindAll(".epos-modellpruefung-befund"));
        cut.FindAll("button.epos-modellpruefung-aufklappen")[1].Click();
        var befunde = cut.FindAll(".epos-modellpruefung-befund");
        Assert.Equal(2, befunde.Count);
        Assert.Contains("Zone 1 — Außenwand Nord", befunde[0].TextContent);
        Assert.Contains("Aufbau Mauerwerk", befunde[0].TextContent);
        cut.FindAll("button.epos-modellpruefung-aufklappen")[1].Click();
        Assert.Empty(cut.FindAll(".epos-modellpruefung-befund"));
        // Ohne Befund ist nichts aufzuklappen.
        Assert.True(cut.FindAll("button.epos-modellpruefung-aufklappen")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Alle_uebernehmen_ruft_den_Weg_und_zeigt_den_neuen_Status_mit_Ruecknahme()
    {
        var p = new ModellpruefungProbeweg();
        var cut = Blatt(p);
        cut.Find("button.epos-modellpruefung-alle").Click();
        Assert.Equal(1, p.Alle);
        Assert.Equal("vollständig", cut.Find(".epos-modellpruefung-status").TextContent.Trim());
        Assert.Contains("epos-ampel-gruen", cut.Find(".epos-modellpruefung-status").ClassName);
        Assert.Equal("2 übernommen", cut.Find(".epos-modellpruefung-meldung").TextContent);
        Assert.True(cut.Find("button.epos-modellpruefung-alle").HasAttribute("disabled"));   // nichts mehr vorzuschlagen
        var zurueck = Assert.Single(cut.FindAll("button.epos-modellpruefung-zuruecknehmen"));
        zurueck.Click();
        Assert.Equal(1, p.Zurueck);
        Assert.Equal("rechnet mit Vorgaben", cut.Find(".epos-modellpruefung-status").TextContent.Trim());
        Assert.Empty(cut.FindAll("button.epos-modellpruefung-zuruecknehmen"));
    }

    [Fact]
    public void Vorschlag_einer_Gruppe_uebernehmen()
    {
        var p = new ModellpruefungProbeweg();
        var cut = Blatt(p);
        cut.FindAll("button.epos-modellpruefung-uebernehmen")[1].Click();
        Assert.Equal(1, p.Einzeln);
        Assert.Single(cut.FindAll("button.epos-modellpruefung-zuruecknehmen"));
    }

    [Fact]
    public void Ansicht_springt_im_Sprungziel_oder_nennt_den_Grund()
    {
        var spruenge = new List<Pruefsprungziel>();
        var cut = Blatt(new ModellpruefungProbeweg(), spruenge: spruenge);
        cut.FindAll("button.epos-modellpruefung-ansicht")[1].Click();
        cut.FindAll("button.epos-modellpruefung-ansicht")[0].Click();
        Assert.Equal(new[] { Pruefsprungziel.Aufbau, Pruefsprungziel.Randbedingung }, spruenge);

        var gesperrt = Blatt(new ModellpruefungProbeweg(), ansichtSperre: R.MPB_SPERRE_ANSICHT, spruenge: spruenge);
        var knopf = gesperrt.FindAll("button.epos-modellpruefung-ansicht")[1];
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.MPB_SPERRE_ANSICHT, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.Equal(2, spruenge.Count);
        Assert.Equal(R.MPB_SPERRE_ANSICHT, gesperrt.Find(".epos-modellpruefung-meldung").TextContent);
    }

    [Fact]
    public void Gruppe_vier_traegt_Klimaregion_und_Nordrichtung()
    {
        var p = new ModellpruefungProbeweg();
        var cut = Blatt(p);
        var gruppen = cut.FindAll(".epos-modellpruefung-gruppe");
        Assert.Single(gruppen[3].QuerySelectorAll("button.epos-modellpruefung-klima"));
        Assert.Single(cut.FindAll("button.epos-modellpruefung-klima"));
        cut.Find("button.epos-modellpruefung-klima").Click();
        Assert.Equal(R.MPB_HINWEIS_KLIMA, cut.Find(".epos-modellpruefung-meldung").TextContent);
        cut.Find("button.epos-modellpruefung-nord").Click();
        Assert.Equal(1, p.Nord);
        Assert.Equal(R.MPB_MSG_NORD_BESTAETIGT, cut.Find(".epos-modellpruefung-meldung").TextContent);

        var ohne = Blatt(new ModellpruefungProbeweg(), mitNord: false);
        ohne.Find("button.epos-modellpruefung-nord").Click();
        Assert.Equal(R.MPB_HINWEIS_NORD, ohne.Find(".epos-modellpruefung-meldung").TextContent);
    }

    [Fact]
    public void Simulation_nur_mit_Delegat()
    {
        Assert.Empty(Blatt(new ModellpruefungProbeweg()).FindAll("button.epos-modellpruefung-simulation"));
        int n = 0;
        var cut = Blatt(new ModellpruefungProbeweg(), simulation: () => n++);
        cut.Find("button.epos-modellpruefung-simulation").Click();
        Assert.Equal(1, n);
    }
}
