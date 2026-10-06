using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Nutzung je Zone im Zonenbaum des Imports</b> (Stufe NP2b; Konzept Nutzungsprofile 6.2, Zeile „Zonenbaum des
/// Imports“) — gefahren mit der echten Hülle und dem Zonenhaus; der Stand der Hülle bekommt die Gruppen bzw. die Herleitung
/// eines Katalogs, den die Testumgebung ohne Datenbank nicht hat: Die Klappliste je Zone führt „keine“, die Einträge ohne
/// Gruppe und je Kategorie eine <c>optgroup</c>.
/// </summary>
public partial class GebaeudeImportZonenDialogTests
{
    /// <summary>Der Zonenbaum mit einer Umformung jedes Stands der Hülle (der Katalog, den die Testumgebung nicht hat).</summary>
    private IRenderedComponent<GebaeudeImportDialog> ZonenbaumUmgeformt(Zonenbaumprobe p, Func<GebaeudeImportStand, GebaeudeImportStand> umformen)
    {
        string probe = Path.Combine(Wurzel(), "Referenzlaeufe", "Importproben", "ifc4_zonen.ifc");
        var huelle = new GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"];
        IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(probe, "ifc4_zonen.ifc", new FileInfo(probe).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return umformen(zuordnen(a)); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(e => { p.Uebernommen.Add(e); return Task.FromResult<string?>(null); }));
        });
        Einlesen(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum")));
        return cut;
    }

    /// <summary>Der Plan eines Stands, umgeformt; ohne Plan bleibt der Stand.</summary>
    private static GebaeudeImportStand MitPlan(GebaeudeImportStand s, Func<GebaeudeZonenplanDaten, GebaeudeZonenplanDaten> plan)
        => s.Zonierung?.Plan is GebaeudeZonenplanDaten p ? s with { Zonierung = s.Zonierung with { Plan = plan(p) } } : s;

    private static readonly IReadOnlyList<GebaeudeNutzungsgruppe> GRUPPEN = new[]
    {
        new GebaeudeNutzungsgruppe("", new[] { new GebaeudeZonenregelDaten("Großraum Nord", "Großraum Nord (nicht im Katalog)") }),
        new GebaeudeNutzungsgruppe("EPOS-Muster", new[]
        {
            new GebaeudeZonenregelDaten("#1", "Wohnen"), new GebaeudeZonenregelDaten("#2", "Büro"),
        }),
        new GebaeudeNutzungsgruppe("Eigene Profile", new[] { new GebaeudeZonenregelDaten("#12", "Mein Büro") }),
    };

    /// <summary>
    /// Die Klappliste je Zone führt „keine“, dann ohne Gruppe den Text „(nicht im Katalog)“, dann je Kategorie eine
    /// <c>optgroup</c> mit ihren Profilen; gewählt ist der Schlüssel der Zone, die Wahl geht als Schritt NUTZUNG hinaus.
    /// </summary>
    [Fact]
    public void Die_Nutzung_je_Zone_steht_nach_Kategorie_gruppiert()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(p, s => MitPlan(s, plan => plan with
        {
            Nutzungen = GRUPPEN.SelectMany(g => g.Eintraege).ToList(),
            Nutzungsgruppen = GRUPPEN,
            Zonen = plan.Zonen.Select((z, i) => i == 0 ? z with { Nutzung = "#12" } : z).ToList(),
        }));

        IElement wahl = cut.FindAll("select.epos-gebimport-nutzung")[0];
        IReadOnlyList<IElement> optionen = wahl.Children.ToList();
        Assert.Equal("option", optionen[0].LocalName);
        Assert.Equal("", optionen[0].GetAttribute("value"));
        Assert.Equal("option", optionen[1].LocalName);
        Assert.Equal("Großraum Nord (nicht im Katalog)", optionen[1].TextContent);
        Assert.Equal(new[] { "EPOS-Muster", "Eigene Profile" }, wahl.QuerySelectorAll("optgroup").Select(o => o.GetAttribute("label")));
        Assert.Equal(new[] { "#1", "#2" }, wahl.QuerySelectorAll("optgroup[label=\"EPOS-Muster\"] option").Select(o => o.GetAttribute("value")));
        Assert.True(wahl.QuerySelector("optgroup[label=\"Eigene Profile\"] option[value=\"#12\"]")!.HasAttribute("selected"));

        cut.FindAll("select.epos-gebimport-nutzung")[1].Change("#2");
        Assert.Equal(GebaeudePlanschrittArt.NUTZUNG, p.Letzter.Art);
        Assert.Equal("#2", p.Letzter.Nutzung);
    }

    /// <summary>
    /// Unter einer Zone mit Herleitung steht die Herleitungszeile (Profil · Kategorie — Quelle; Größen aus der Datei;
    /// Kennwerte) mit der Art der Quelle als <c>data-quelle</c>; eine Zone ohne Herleitung trägt keine Zeile.
    /// </summary>
    [Fact]
    public void Unter_der_Zone_steht_die_Herleitungszeile()
    {
        var p = new Zonenbaumprobe();
        var h = new GebaeudeProfilherleitung("Büro", "EPOS-Muster", "DIN_NUMMER", "aus DIN-Nr. 1 der Projektdatei",
                                             "Heizen und Personen aus der Datei", "Mo–Fr · 7–18 h · 21 °C");
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(p, s => MitPlan(s, plan => plan with
        {
            Zonen = plan.Zonen.Select((z, i) => i == 0 ? z with { Herleitung = h } : z).ToList(),
        }));

        string erste = cut.FindAll("tr.epos-gebimport-planzone")[0].GetAttribute("data-zone")!;
        IReadOnlyList<IElement> zeilen = cut.FindAll("tr.epos-gebimport-planherleitung");
        IElement zeile = Assert.Single(zeilen);
        Assert.Equal(erste, zeile.GetAttribute("data-zone"));
        Assert.Equal("DIN_NUMMER", zeile.GetAttribute("data-quelle"));
        Assert.Equal("Büro · EPOS-Muster — aus DIN-Nr. 1 der Projektdatei; Heizen und Personen aus der Datei; Mo–Fr · 7–18 h · 21 °C",
                     zeile.TextContent.Trim());
        Assert.Same(zeile, cut.FindAll("tr.epos-gebimport-planzone")[0].NextElementSibling);
    }
}
