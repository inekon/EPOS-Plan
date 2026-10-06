using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
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
    private IRenderedComponent<GebaeudeImportDialog> ZonenbaumUmgeformt(Zonenbaumprobe p, Func<GebaeudeImportStand, GebaeudeImportStand> umformen,
                                                                       RaumnutzungWeg? katalog = null, bool projektdatei = false,
                                                                       RaumnutzungKiZugang? ki = null)
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
            if (katalog is not null) c.Add(x => x.Raumnutzung, katalog);
            if (ki is not null) c.Add(x => x.RaumnutzungKi, ki);
            if (projektdatei)
                c.Add(x => x.ProjektdateiLesen, (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)((_, _, _) =>
                    Task.FromResult(new GebaeudeProjektdateiDaten())));
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

    /// <summary>
    /// <b>Der Einzonenweg der Projektdatei</b> (NP2b-4): Ergibt die Datei eine Zone, steht der Vorschlag aus der DIN-Nummer als
    /// Hinweiszeile im Block der Projektdatei — ohne Auswahlfeld; im Mehrzonenweg (als Zonen übernommen) steht er nicht.
    /// </summary>
    [Fact]
    public void Der_Einzonenweg_zeigt_den_Vorschlag_aus_der_DIN_Nummer_als_Hinweis()
    {
        const string VORSCHLAG = "Vorschlag aus DIN-Nr. 1: Büro · EPOS-Muster — zuweisbar im Gebäudeeditor über „Nutzungsprofil übernehmen…“";
        bool einzonig = true;
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s with
        {
            Projektdatei = new GebaeudeProjektdateiDaten { Dateiname = "haus.sqproj", Einzonenvorschlag = VORSCHLAG },
            Zonierung = einzonig ? s.Zonierung! with { Einzonig = true } : s.Zonierung,
        }, projektdatei: true);

        IElement hinweis = cut.Find(".epos-gebimport-sqvorschlag");
        Assert.Equal(VORSCHLAG, hinweis.TextContent.Trim());
        Assert.Empty(hinweis.QuerySelectorAll("select, input"));

        einzonig = false;
        cut.Find("[data-aktion=\"anlegen\"]").Click();
        cut.Find(".epos-gebimport-anlegen-ok").Click();
        Assert.Empty(cut.FindAll(".epos-gebimport-sqvorschlag"));
    }

    /// <summary>
    /// <b>Der Assistent erreicht das Blatt im Import</b> (NP2b-5a, wie im Gebäudeeditor): Der Importdialog reicht den Zugang
    /// des Wirts an das Blatt; die Sicht des Gebäudedialogs löst darüber die Felder <c>np_*</c> auf — die Maske des
    /// Gebäudedialogs führt sie. Nach dem Schließen ist das Blatt gelöst.
    /// </summary>
    [Fact]
    public void Der_Importdialog_reicht_den_KI_Zugang_an_das_Blatt()
    {
        var ki = new RaumnutzungKiZugang();
        var sicht = new GebaeudeKiSicht { Nutzungsprofile = ki };
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(new Zonenbaumprobe(), s => s, Nutzungskatalog(), ki: ki);
        Assert.False(ki.Angebunden);
        Assert.Throws<InvalidOperationException>(() => sicht.Setzen("np_name", "x"));

        cut.Find(".epos-gebimport-nutzungsprofile-oeffnen").Click();
        cut.Find("[data-kategorie=\"2\"] .epos-raumnutzung-kategorie-waehlen").Click();
        cut.Find("[data-profil=\"12\"] .epos-raumnutzung-profil-waehlen").Click();
        Assert.True(ki.Angebunden);
        Assert.Equal("Mein Büro", sicht.Lesen("np_name"));
        cut.InvokeAsync(() => sicht.Setzen("np_kuehl_soll", 25.0));
        Assert.Equal(25.0, cut.FindComponent<RaumnutzungBlatt>().Instance.Arbeitsstand!.KuehlSoll);

        KiKern.KiDialog maske = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE)!;
        Assert.Contains(maske.Felder, f => f.Name == "np_name" && f.Eigenschaftspfad == "GebaeudeKiSicht.np_name");

        cut.Find(".epos-gebimport-nutzungsblatt .epos-ueberlagerung-zu").Click();
        Assert.False(ki.Angebunden);
        Assert.Null(sicht.Lesen("np_name"));
    }

    /// <summary>
    /// <b>Neu lesen nach dem Blatt</b> (NP2b-3): Das Blatt hat den Katalog geändert (ein neues Profil steht vor den übrigen);
    /// nach dem Schließen liest der Plan neu, und die schon gewählte Nutzung der offenen Zeile „Zone hinzufügen“ bleibt
    /// dieselbe — sie hängt am Schlüssel, nicht an der Stelle in der Liste.
    /// </summary>
    [Fact]
    public void Nach_dem_Blatt_bleibt_die_gewaehlte_Nutzung_der_neuen_Zone()
    {
        var p = new Zonenbaumprobe();
        bool geaendert = false;
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumUmgeformt(p, s => !geaendert ? s : MitPlan(s, plan => plan with
        {
            Nutzungen = new[] { new GebaeudeZonenregelDaten("#99", "Neues Profil") }.Concat(plan.Nutzungen).ToList(),
        }), Nutzungskatalog());

        Aktion(cut, "anlegen").Click();
        Waehlen(Wahl(cut, "Nutzung"), t => t == "Büro");
        cut.Find(".epos-gebimport-nutzungsprofile-oeffnen").Click();
        geaendert = true;
        cut.Find(".epos-gebimport-nutzungsblatt .epos-ueberlagerung-zu").Click();
        Assert.False(cut.Instance.NutzungsprofileOffen);

        cut.Find(".epos-gebimport-anlegen-ok").Click();
        Assert.Equal(GebaeudePlanschrittArt.ANLEGEN, p.Letzter.Art);
        Assert.Equal("BUERO", p.Letzter.Nutzung);
    }
}
