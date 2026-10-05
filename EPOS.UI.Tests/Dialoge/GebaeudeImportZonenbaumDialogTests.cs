using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Zonenbaum im Zuordnungsdialog</b> (Mehrzonenkonzept 6.4, Muster des Zonierungsdialogs) — gefahren mit der echten
/// Hülle und dem Zonenhaus (Z4: Keller-, Erd- und Obergeschoss): Baum Gebäude → Zonen → Räume mit Auswahlhaken, die Liste
/// „Nicht zugeordnete Räume", jede Knopfaktion als Schritt am Zonenplan des Kerns (Zone anlegen mit Nutzung, löschen,
/// aufheben mit Rückfrage, Räume zuordnen, Geschoss zur Zone, Rest nach Regel), die Sperre am OK mit Zahl und die Ablehnung
/// des Kerns als Banner mit dem Raumhaken als Ausweg.
/// </summary>
public partial class GebaeudeImportZonenDialogTests
{
    private sealed class Zonenbaumprobe
    {
        public List<GebaeudeZuordnungsanfrage> Anfragen { get; } = new();
        public List<GebaeudeImportErgebnis> Uebernommen { get; } = new();
        public GebaeudePlanschritt Letzter => Anfragen.Last(a => a.Planschritte is { Count: > 0 }).Planschritte!.Last();
    }

    private IRenderedComponent<GebaeudeImportDialog> Zonenbaum(Zonenbaumprobe p, GebaeudeImportTexte? texte = null)
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
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return zuordnen(a); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(e => { p.Uebernommen.Add(e); return Task.FromResult<string?>(null); }));
            if (texte is not null) c.Add(x => x.Texte, texte);
        });
        Einlesen(cut, texte?.DateiKnopf.TrimEnd('…', '.') ?? "Datei wählen");
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum")));
        return cut;
    }

    private static IElement Zonenzeile(IRenderedComponent<GebaeudeImportDialog> cut, string name)
        => cut.FindAll(".epos-gebimport-zonenbaum tr.epos-gebimport-planzone")
              .First(z => z.QuerySelector("input.epos-gebimport-zonenname")!.GetAttribute("value") == name);

    private static void ZoneWaehlen(IRenderedComponent<GebaeudeImportDialog> cut, string name, bool an = true)
        => Zonenzeile(cut, name).QuerySelector("input.epos-gebimport-wahl")!.Change(an);

    private static void Aufklappen(IRenderedComponent<GebaeudeImportDialog> cut, string name)
        => Zonenzeile(cut, name).QuerySelector("button.epos-gebimport-aufklappen")!.Click();

    private static void RaumWaehlen(IRenderedComponent<GebaeudeImportDialog> cut, string kennung)
        => cut.FindAll("tr[data-raum='" + kennung + "'] input.epos-gebimport-wahl").First().Change(true);

    private static IElement Aktion(IRenderedComponent<GebaeudeImportDialog> cut, string aktion)
        => cut.Find("button[data-aktion='" + aktion + "']");

    private static IReadOnlyList<string?> Offen(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.FindAll(".epos-gebimport-offen tbody tr").Select(z => z.GetAttribute("data-raum")).ToList();

    /// <summary>Die Räume einer Zone im Baum — aufgeklappt gelesen, der Klappzustand bleibt, wie er war.</summary>
    private static IReadOnlyList<string?> Raeume(IRenderedComponent<GebaeudeImportDialog> cut, string zone)
    {
        bool offen = Zonenzeile(cut, zone).QuerySelector("button.epos-gebimport-aufklappen")!.GetAttribute("aria-expanded") == "true";
        if (!offen) Aufklappen(cut, zone);
        var raeume = new List<string?>();
        for (IElement? z = Zonenzeile(cut, zone).NextElementSibling; z is not null && z.ClassList.Contains("epos-gebimport-planraum"); z = z.NextElementSibling)
            raeume.Add(z.GetAttribute("data-raum"));
        if (!offen) Aufklappen(cut, zone);
        return raeume;
    }

    private static IElement OkKnopf(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer");

    [Fact]
    public void Der_Zonenbaum_zeigt_Gebaeude_Zonen_und_Raeume_mit_Auswahlhaken_aus_dem_Plan()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);

        Assert.Contains("Gebäude", cut.Find(".epos-gebimport-baumwurzel").TextContent);
        Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, Zonenzeilen(cut));
        Assert.All(cut.FindAll("tr.epos-gebimport-planzone"), z =>
        {
            Assert.StartsWith("Z:", z.GetAttribute("data-zone"), StringComparison.Ordinal);
            Assert.NotNull(z.QuerySelector("input[type=checkbox].epos-gebimport-wahl[aria-label]"));
            Assert.NotNull(z.QuerySelector("select.epos-gebimport-nutzung"));
        });
        // Die Nutzung der Klappliste: keine, Wohnen, Büro, Schule; unter Z4 ohne Vorbelegung.
        IElement nutzung = Zonenzeile(cut, "Erdgeschoss").QuerySelector("select.epos-gebimport-nutzung")!;
        Assert.Equal(new[] { "", "WOHNEN", "BUERO", "SCHULE" }, nutzung.QuerySelectorAll("option").Select(o => o.GetAttribute("value")));

        // Aufgeklappt stehen die Räume mit Geschoss und eigenem Haken; zu Beginn ist rechts nichts offen.
        Assert.Empty(cut.FindAll("tr.epos-gebimport-planraum"));
        Aufklappen(cut, "Erdgeschoss");
        Assert.NotEmpty(cut.FindAll("tr.epos-gebimport-planraum[data-raum] input.epos-gebimport-wahl"));
        Assert.Contains("Alle Räume sind einer Zone zugeordnet.", cut.Find(".epos-gebimport-offen-leer").TextContent);
        Assert.Contains("(0)", cut.Find(".epos-gebimport-offentitel").TextContent);
        Assert.Equal(new[] { "anlegen", "loeschen", "aufheben", "zuordnen", "geschoss", "rest" },
                     cut.FindAll(".epos-gebimport-planleiste button[data-aktion]").Select(k => k.GetAttribute("data-aktion")));
        Assert.Empty(p.Anfragen.Where(a => a.Planschritte is not null));
        Assert.Null(OkKnopf(cut).GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Zone_hinzufuegen_legt_die_Zone_mit_Name_und_Nutzung_an_und_die_Mehrfachwahl_ordnet_Raeume_zu()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);

        Aktion(cut, "anlegen").Click();
        IElement zeile = cut.Find(".epos-gebimport-neuezone");
        Assert.Equal("Zone 4", zeile.QuerySelector("input")!.GetAttribute("value"));
        Waehlen(Wahl(cut, "Nutzung"), t => t == "Büro");
        cut.Find(".epos-gebimport-anlegen-ok").Click();

        Assert.Equal(new GebaeudePlanschritt(GebaeudePlanschrittArt.ANLEGEN, Name: "Zone 4", Nutzung: "BUERO"), p.Letzter);
        Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss", "Zone 4" }, Zonenzeilen(cut));
        Assert.Equal("BUERO", Zonenzeile(cut, "Zone 4").QuerySelector("select.epos-gebimport-nutzung option[selected]")!.GetAttribute("value"));
        Assert.Contains("ohne Raum", Zonenzeile(cut, "Zone 4").TextContent);
        Assert.Empty(cut.FindAll(".epos-gebimport-neuezone"));

        // Zwei Räume des Obergeschosses gewählt, die neue Zone als Ziel: Räume zur ausgewählten Zone.
        IReadOnlyList<string?> og = Raeume(cut, "Obergeschoss");
        Aufklappen(cut, "Obergeschoss");
        RaumWaehlen(cut, og[0]!);
        RaumWaehlen(cut, og[1]!);
        Assert.True(Aktion(cut, "zuordnen").HasAttribute("disabled"));
        ZoneWaehlen(cut, "Zone 4");
        Aktion(cut, "zuordnen").Click();
        Assert.Equal(GebaeudePlanschrittArt.ZUORDNEN, p.Letzter.Art);
        Assert.Equal(new[] { og[0], og[1] }, p.Letzter.Raeume);
        Assert.Equal(Zonenzeile(cut, "Zone 4").GetAttribute("data-zone"), p.Letzter.Zone);
        Assert.Equal(new[] { og[0], og[1] }, Raeume(cut, "Zone 4"));

        // Umbenennen und Nutzung im Baum.
        Zonenzeile(cut, "Zone 4").QuerySelector("input.epos-gebimport-zonenname")!.Change("Büro OG");
        Assert.Equal(GebaeudePlanschrittArt.UMBENENNEN, p.Letzter.Art);
        Assert.Equal("Büro OG", p.Letzter.Name);
        Zonenzeile(cut, "Büro OG").QuerySelector("select.epos-gebimport-nutzung")!.Change("SCHULE");
        Assert.Equal(new GebaeudePlanschritt(GebaeudePlanschrittArt.NUTZUNG, Zone: p.Letzter.Zone, Nutzung: "SCHULE"), p.Letzter);

        // Ein doppelter Name lehnt der Kern ab: Banner, Schritt fällt heraus.
        int schritte = cut.Instance.Planschritte.Count;
        Zonenzeile(cut, "Büro OG").QuerySelector("input.epos-gebimport-zonenname")!.Change("Erdgeschoss");
        Assert.Equal(schritte, cut.Instance.Planschritte.Count);
        Assert.Equal("IMP_IFC_PROT_PLAN_NAME_DOPPELT", cut.Find(".epos-gebimport-ausweg").GetAttribute("data-kennung"));
        Assert.Contains("Büro OG", Zonenzeilen(cut));

        // Das Ergebnis trägt die Schritte und die Haken des Plans.
        OkKnopf(cut).Click();
        GebaeudeImportErgebnis ergebnis = Assert.Single(p.Uebernommen);
        Assert.Equal(cut.Instance.Planschritte, ergebnis.Planschritte);
        Assert.NotNull(ergebnis.Plangrundhaken);
    }

    [Fact]
    public void Zonen_loeschen_schiebt_die_Raeume_nach_rechts_sperrt_OK_mit_Zahl_und_Rest_nach_Regel_gibt_es_frei()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);
        IReadOnlyList<string?> og = Raeume(cut, "Obergeschoss");
        IReadOnlyList<string?> eg = Raeume(cut, "Erdgeschoss");

        Assert.True(Aktion(cut, "loeschen").HasAttribute("disabled"));
        ZoneWaehlen(cut, "Obergeschoss");
        ZoneWaehlen(cut, "Erdgeschoss");
        Aktion(cut, "loeschen").Click();
        Assert.Equal(GebaeudePlanschrittArt.LOESCHEN, p.Letzter.Art);
        Assert.Equal(2, p.Letzter.Zonen!.Count);
        Assert.Equal(new[] { "Kellergeschoss" }, Zonenzeilen(cut));
        Assert.Equal(eg.Concat(og).OrderBy(k => k).ToList(), Offen(cut).OrderBy(k => k).ToList());

        // OK gesperrt: weich, mit Zahl; der Versuch meldet sich und übernimmt nicht.
        int n = eg.Count + og.Count;
        IElement ok = OkKnopf(cut);
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        Assert.Contains(n + " Räume sind keiner Zone zugeordnet", ok.GetAttribute("title"));
        Assert.Contains(n + " Räume", cut.Find(".epos-gebimport-okhinweis").TextContent);
        Assert.Equal(n, cut.Instance.NichtZugeordnet);
        ok.Click();
        Assert.Empty(p.Uebernommen);

        // „Rest nach Regel zuordnen" am OK: nur die nicht zugeordneten, nach der gewählten Regel (Z4).
        cut.Find(".epos-gebimport-okhinweis button[data-aktion='rest-ok']").Click();
        Assert.Equal(new GebaeudePlanschritt(GebaeudePlanschrittArt.REST, Regel: "Z4"), p.Letzter);
        Assert.Empty(Offen(cut));
        Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, Zonenzeilen(cut));
        Assert.Empty(cut.FindAll(".epos-gebimport-okhinweis"));
        Assert.Null(OkKnopf(cut).GetAttribute("aria-disabled"));
        OkKnopf(cut).Click();
        Assert.Single(p.Uebernommen);
    }

    [Fact]
    public void Zonierung_aufheben_fragt_zurueck_und_Geschoss_zur_Zone_ordnet_das_Geschoss_zu()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);
        IReadOnlyList<string?> og = Raeume(cut, "Obergeschoss");

        Aktion(cut, "aufheben").Click();
        Assert.True(cut.Instance.AufhebenFrageOffen);
        cut.FindAll("button").First(k => k.TextContent == "Nein").Click();
        Assert.False(cut.Instance.AufhebenFrageOffen);
        Assert.Empty(cut.Instance.Planschritte);

        Aktion(cut, "aufheben").Click();
        cut.FindAll("button").First(k => k.TextContent == "Ja").Click();
        Assert.Equal(GebaeudePlanschrittArt.AUFHEBEN, p.Letzter.Art);
        Assert.Empty(Zonenzeilen(cut));
        Assert.NotEmpty(Offen(cut));
        Assert.Contains("OK", cut.Find(".epos-gebimport-okhinweis").TextContent);

        // Eine neue Zone, das Obergeschoss hinein.
        Aktion(cut, "anlegen").Click();
        cut.Find(".epos-gebimport-anlegen-ok").Click();
        ZoneWaehlen(cut, "Zone 1");
        Waehlen(Wahl(cut, "Geschoss"), t => t == "Obergeschoss");
        Aktion(cut, "geschoss").Click();
        Assert.Equal(GebaeudePlanschrittArt.GESCHOSS, p.Letzter.Art);
        Assert.Equal(Zonenzeile(cut, "Zone 1").GetAttribute("data-zone"), p.Letzter.Zone);
        Assert.False(string.IsNullOrEmpty(p.Letzter.Geschoss));
        Assert.Equal(og.OrderBy(k => k), Raeume(cut, "Zone 1").OrderBy(k => k));
        Assert.DoesNotContain(Offen(cut), og.Contains);
    }

    [Fact]
    public void Eine_ungleich_beheizte_Zuordnung_steht_als_Banner_und_der_Raumhaken_ist_der_Ausweg()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);
        IReadOnlyList<string?> kg = Raeume(cut, "Kellergeschoss");
        string lager = Assert.Single(kg)!;   // das unbeheizte Lager

        Aufklappen(cut, "Kellergeschoss");
        RaumWaehlen(cut, lager);
        ZoneWaehlen(cut, "Erdgeschoss");
        Aktion(cut, "zuordnen").Click();
        Assert.Empty(cut.Instance.Planschritte);
        IElement banner = cut.Find(".epos-gebimport-ausweg");
        Assert.Equal("IMP_IFC_PROT_PLAN_BEHEIZUNG", banner.GetAttribute("data-kennung"));
        Assert.Contains("nicht gleich beheizt", banner.TextContent);
        Assert.Equal(kg, Raeume(cut, "Kellergeschoss"));

        banner.QuerySelector("label.epos-schalter input")!.Change(true);
        Assert.Equal(GebaeudePlanschrittArt.ZUORDNEN, p.Letzter.Art);
        Assert.Contains(lager, Raeume(cut, "Erdgeschoss"));
        Assert.Empty(cut.FindAll(".epos-gebimport-ausweg"));
        Assert.True(Assert.Single(cut.Instance.Planschritte).Raeume!.Contains(lager));
    }

    [Fact]
    public void Ein_Regelwechsel_mit_Schritten_fragt_zurueck_und_Nein_laesst_den_Plan_stehen()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);
        Aktion(cut, "anlegen").Click();
        cut.Find(".epos-gebimport-anlegen-ok").Click();
        Assert.Single(cut.Instance.Planschritte);

        IElement wahl = cut.FindAll("select").First(s => s.TextContent.Contains("Z4 –"));
        wahl.Change(wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Z1", StringComparison.Ordinal)).GetAttribute("value"));
        Assert.True(cut.Instance.RegelFrageOffen);
        cut.FindAll("button").First(k => k.TextContent == "Nein").Click();
        Assert.Single(cut.Instance.Planschritte);
        Assert.Equal("Z4", cut.Instance.Zonenregel);

        wahl = cut.FindAll("select").First(s => s.TextContent.Contains("Z4 –"));
        wahl.Change(wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Z1", StringComparison.Ordinal)).GetAttribute("value"));
        cut.FindAll("button").First(k => k.TextContent == "Ja").Click();
        Assert.Empty(cut.Instance.Planschritte);
        Assert.Equal("Z1", cut.Instance.Zonenregel);
    }

    [Fact]
    public void Eine_Zone_bleibt_der_Einzonenweg_der_Baum_zeigt_sie_und_der_Hinweis_sagt_es()
    {
        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(p);
        IElement wahl = cut.FindAll("select").First(s => s.TextContent.Contains("Z4 –"));
        wahl.Change(wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Z5", StringComparison.Ordinal)).GetAttribute("value"));

        Assert.Single(Zonenzeilen(cut));
        Assert.Contains("Als Zone mit Bauteilen übernehmen", cut.Find(".epos-gebimport-bauteile").TextContent);
        Assert.Contains("Name und Nutzung dieser Zone werden nicht übernommen", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-gebimport-flaechen"));
    }

    [Fact]
    public void Die_Texte_des_Zonenbaums_stehen_auch_englisch()
    {
        GebaeudeImportTexte deutsch, englisch;
        using (new Kulturvorrichtung("de-DE")) deutsch = new GebaeudeImportTexte();
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Zone hinzufügen", deutsch.PlanAnlegen);
        Assert.Equal("Add zone", englisch.PlanAnlegen);
        Assert.Equal("Remove zoning", englisch.PlanAufheben);
        Assert.Equal("Assign the rest by rule", englisch.PlanRest);
        Assert.Equal("Unassigned rooms ({0})", englisch.PlanOffenTitel);
        foreach (string eigenschaft in new[] { "SpalteNutzung", "PlanWurzel", "ZoneWaehlen", "ZoneName", "ZoneNutzung", "NutzungKeine",
                                               "ZoneLeer", "RaumWaehlen", "PlanName", "PlanAnlegenOk", "PlanAnlegenVerwerfen", "PlanLoeschen",
                                               "PlanZuordnen", "PlanZuordnenGrund", "PlanGeschoss", "PlanGeschossKnopf", "PlanRestRegel",
                                               "PlanHinweis", "PlanEinzonig", "AlsZonePlanHinweis", "PlanOffenLeer", "PlanAusserhalb",
                                               "PlanAufhebenTitel", "PlanAufhebenFrage", "PlanOkGesperrt" })
        {
            var e = typeof(GebaeudeImportTexte).GetProperty(eigenschaft)!;
            string de = (string)e.GetValue(deutsch)!, en = (string)e.GetValue(englisch)!;
            Assert.False(string.IsNullOrWhiteSpace(de), eigenschaft);
            Assert.False(string.IsNullOrWhiteSpace(en), eigenschaft);
            if (eigenschaft != "PlanAufhebenTitel") Assert.NotEqual(de, en);
        }

        var p = new Zonenbaumprobe();
        IRenderedComponent<GebaeudeImportDialog> cut;
        using (new Kulturvorrichtung("en-US")) cut = Zonenbaum(p, englisch);
        Assert.Contains("Add rooms to the selected zone", cut.Find(".epos-gebimport-planleiste").TextContent);
        Assert.Contains("Unassigned rooms (0)", cut.Find(".epos-gebimport-offentitel").TextContent);
    }
}
