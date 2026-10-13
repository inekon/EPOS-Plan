using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die grafische Darstellung des Abschnitts „Ergebnisse — Energie“ (<see cref="UebersichtErgebnisGrafik"/>) samt Umschalter
/// „Tabelle | Grafik“ in <see cref="UebersichtErgebnisse"/> und der Ablage der Wahl über die Einstellungsnaht der Hülle
/// <see cref="UebersichtErgebnisGaben"/> — ohne Datenbank, mit einer flüchtigen Ablage als Attrappe der Naht.
/// </summary>
public class UebersichtErgebnisGrafikTests : EposBunitContext
{
    public UebersichtErgebnisGrafikTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    /// <summary>Wärmebedarf je Projekt [MWh/a].</summary>
    private static readonly Dictionary<int, double> s_waerme = new() { [1030] = 100, [1031] = 110, [1032] = 100 };

    private static UebersichtErgebnisGaben Huelle() => new((id, stamm) => new VariantenDaten
    {
        IdProjekt = id, IstStamm = stamm, CO2Gesamt = s_waerme[id] / 5,
        EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>
        {
            new() { CarrierId = 1, Traeger = "Erdgas", MengeMWh = 30, WaermeMengeMWh = 30 },
            new() { CarrierId = 2, Traeger = "Strom", Netzstrom = true, MengeMWh = 50, Einheit = "kWh" }
        },
        Ergebnis = new ErgebnisModel
        {
            Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermebedarf_Gesamt = s_waerme[id], Strombedarf_Gesamt = 50, Stromrestbedarf = 50
            },
            Waermepumpe = new ErgebnisWaermepumpeModel { Stromverbrauch_Kuehlung = 3, Waermeproduktion_WP = 35, Stromverbrauch_WP = id == 1030 ? 10 : 9 }
        }
    });

    private static VarianteZeile Zeile(int id, string bezeichner, bool simuliert, bool stamm = false) => new()
    {
        IdProjekt = id, IstStamm = stamm, Bezeichner = bezeichner, Projektname = "Musterhaus " + bezeichner,
        SimZeitpunkt = simuliert ? "02.09.26 10:00" : ""
    };

    private static UebersichtStand Stand(bool simuliert1032 = true) => new()
    {
        StammId = 1030,
        Zeilen = new[]
        {
            Zeile(1030, "(Stammprojekt)", true, stamm: true),
            Zeile(1031, "WP klein", true),
            Zeile(1032, "mit Puffer", simuliert1032)
        },
        MarkierteId = 1030,
        GewaehlteVarianten = new[] { 1030, 1031, 1032 }
    };

    /// <summary>Der Abschnitt mit der Ansicht aus einer eigenen Ablage (Lesen und Schreiben wie die Hülle).</summary>
    private IRenderedComponent<UebersichtErgebnisse> Zeige(Dictionary<string, string> ablage, UebersichtStand? stand = null,
                                                          Func<UebersichtStand, UebersichtErgebnisStand>? laden = null)
        => Render<UebersichtErgebnisse>(p => p
            .Add(x => x.Stand, stand ?? Stand())
            .Add(x => x.Laden, laden ?? Huelle().Laden)
            .Add(x => x.AnsichtLesen, () => ablage.TryGetValue("ansicht", out string? a) ? a : UebersichtErgebnisse.ANSICHT_TABELLE)
            .Add(x => x.AnsichtGewechselt, a => ablage["ansicht"] = a));

    private static Dictionary<string, string> Grafikablage() => new() { ["ansicht"] = UebersichtErgebnisse.ANSICHT_GRAFIK };

    /// <summary>Setzt eine flüchtige Ablage für <c>Dienste.Einstellungen</c> und stellt sie zurück.</summary>
    private static void MitAblage(Action<IEinstellungen> lauf)
    {
        IEinstellungen alt = WindowsFormsApplication1.Dienste.Einstellungen;
        var ablage = new FluechtigeEinstellungen();
        WindowsFormsApplication1.Dienste.Einstellungen = ablage;
        try { lauf(ablage); }
        finally { WindowsFormsApplication1.Dienste.Einstellungen = alt; }
    }

    private static UebersichtErgebnisGrafik Grafik(IRenderedComponent<UebersichtErgebnisse> cut)
        => cut.FindComponent<UebersichtErgebnisGrafik>().Instance;

    /// <summary>Die Balken (Rechtecke mit Füllung) einer Version in einem Modell.</summary>
    private static List<Rechteck> Balken(Zeichenmodell m, string version)
        => m.Befehle.OfType<Rechteck>().Where(r => r.Marke == "reihe:" + version && r.Fuellung is not null).ToList();

    // ---- Fälle -----------------------------------------------------------

    [Fact]
    public void Umschalter_steht_auf_Tabelle_und_wechselt_samt_Ablage()
    {
        var ablage = new Dictionary<string, string>();
        var cut = Zeige(ablage);

        var knoepfe = cut.FindAll(".epos-ergebnis-ansicht button[role=radio]");
        Assert.Equal(new[] { "Tabelle", "Grafik" }, knoepfe.Select(k => k.TextContent).ToArray());
        Assert.Equal("true", knoepfe[0].GetAttribute("aria-checked"));
        Assert.Equal(UebersichtErgebnisse.ANSICHT_TABELLE, cut.Instance.Ansicht);
        Assert.NotEmpty(cut.FindAll("table.epos-ergebnisvergleich"));
        Assert.Empty(cut.FindAll(".epos-ergebnis-grafik"));

        cut.Find(".epos-ergebnis-ansicht button[data-ansicht=grafik]").Click();
        Assert.Equal(UebersichtErgebnisse.ANSICHT_GRAFIK, ablage["ansicht"]);
        Assert.Equal("true", cut.Find(".epos-ergebnis-ansicht button[data-ansicht=grafik]").GetAttribute("aria-checked"));
        Assert.Empty(cut.FindAll("table.epos-ergebnisvergleich"));
        Assert.NotEmpty(cut.FindAll(".epos-ergebnis-grafik figure.epos-ergebnis-diagramm"));

        // Pfeiltaste schaltet zurück.
        cut.Find(".epos-ergebnis-ansicht").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal(UebersichtErgebnisse.ANSICHT_TABELLE, ablage["ansicht"]);
        Assert.NotEmpty(cut.FindAll("table.epos-ergebnisvergleich"));
    }

    [Fact]
    public void Die_Huelle_legt_die_Ansicht_ueber_die_Einstellungsnaht_ab()
        => MitAblage(ablage =>
        {
            Assert.Equal(UebersichtErgebnisse.ANSICHT_TABELLE, UebersichtErgebnisGaben.AnsichtLesen());
            IReadOnlyDictionary<string, object> gaben = new UebersichtErgebnisGaben((id, s) => new VariantenDaten()).Gaben();
            ((Action<string>)gaben["AnsichtGewechselt"])(UebersichtErgebnisse.ANSICHT_GRAFIK);
            Assert.Equal("grafik", ablage.Lies("Uebersicht.Ergebnisse.Ansicht"));
            Assert.Equal(UebersichtErgebnisse.ANSICHT_GRAFIK, ((Func<string>)gaben["AnsichtLesen"])());

            // Ein neuer Abschnitt mit den Gaben der Hülle öffnet in der Grafik.
            var cut = Render<UebersichtErgebnisse>(p => p
                .Add(x => x.Stand, Stand())
                .Add(x => x.Laden, Huelle().Laden)
                .Add(x => x.AnsichtLesen, (Func<string>)gaben["AnsichtLesen"])
                .Add(x => x.AnsichtGewechselt, (Action<string>)gaben["AnsichtGewechselt"]));
            Assert.Equal(UebersichtErgebnisse.ANSICHT_GRAFIK, cut.Instance.Ansicht);
            Assert.NotEmpty(cut.FindAll(".epos-ergebnis-grafik"));
        });

    [Fact]
    public void Ein_Diagramm_je_Gruppe_und_Einheit()
    {
        var cut = Zeige(Grafikablage());
        UebersichtErgebnisGrafik grafik = Grafik(cut);
        UebersichtErgebnisStand stand = cut.Instance.Ergebnis!;

        foreach (ErgebnisVergleichGruppe g in stand.Gruppen.Where(g => g.Zustand == ErgebnisGruppenZustand.Offen))
        {
            IReadOnlyList<ErgebnisDiagramm> d = grafik.Diagramme(g);
            int erwartet = g.Zeilen.Where(z => z.Einheit.Length > 0).Select(z => z.Einheit).Distinct().Count()
                           + g.Zeilen.Count(z => z.Einheit.Length == 0);
            Assert.Equal(erwartet, d.Count);
            Assert.All(d, x => Assert.All(x.Zeilen, z => Assert.Equal(x.Einheit, z.Einheit)));
            Assert.Equal(d.Count, cut.FindAll("section.epos-ergebnis-grafik-gruppe--" + g.Schluessel + " figure").Count);
        }

        // Der Bedarf trennt MWh/a von kW; die Achse trägt die Einheit in eckigen Klammern.
        ErgebnisVergleichGruppe bedarf = stand.Gruppen.Single(g => g.Schluessel == Energievergleich.GRUPPE_BEDARF);
        ErgebnisDiagramm mwh = grafik.Diagramme(bedarf).First(x => x.Einheit == "MWh/a");
        Assert.Contains(mwh.Modell!.Befehle.OfType<WindowsFormsApplication1.Zeichnung.Text>(), t => t.Inhalt == "[MWh/a]");
        Assert.Equal("Bedarf [MWh/a]", mwh.Bezeichnung);
        Assert.NotNull(cut.Find("figure[data-einheit='MWh/a'] .epos-diagramm-svg-flaeche[aria-label='Bedarf [MWh/a]']"));

        // Die JAZ steht für sich.
        ErgebnisVergleichGruppe eff = stand.Gruppen.Single(g => g.Schluessel == Energievergleich.GRUPPE_EFFIZIENZ);
        Assert.Contains(grafik.Diagramme(eff), x => x.Einheit.Length == 0 && x.Zeilen.Count == 1);
    }

    [Fact]
    public void Jede_Version_hat_in_allen_Diagrammen_dieselbe_Farbe()
    {
        var cut = Zeige(Grafikablage());
        UebersichtErgebnisGrafik grafik = Grafik(cut);
        IReadOnlyList<ErgebnisGrafikVersion> versionen = grafik.Versionen;
        Assert.Equal(3, versionen.Count);
        Assert.Equal(3, versionen.Select(v => v.Rolle).Distinct().Count());

        var diagramme = cut.Instance.Ergebnis!.Gruppen.SelectMany(g => grafik.Diagramme(g)).ToList();
        Assert.True(diagramme.Count > 3);
        foreach (ErgebnisGrafikVersion v in versionen)
            foreach (ErgebnisDiagramm d in diagramme)
                Assert.All(Balken(d.Modell!, v.Name), r => Assert.Equal(v.Rolle, r.Fuellung!.Ton.Rolle));

        // Die Legende steht einmal, mit der Farbe der Rolle.
        Assert.Single(cut.FindAll(".epos-ergebnis-grafik-legende"));
        var eintraege = cut.FindAll(".epos-ergebnis-grafik-version");
        Assert.Equal(versionen.Select(v => v.Farbe), eintraege.Select(e => e.GetAttribute("data-farbe")));
        Assert.Equal(Diagrammfarben.Hex(Farbpalette.Aktuell[Farbrolle.STAMM]), versionen[0].Farbe);
    }

    [Fact]
    public void Abweichung_traegt_Randstrich_und_Zeigertext_wie_die_Tabelle()
    {
        var cut = Zeige(Grafikablage());
        UebersichtErgebnisGrafik grafik = Grafik(cut);
        ErgebnisVergleichGruppe bedarf = cut.Instance.Ergebnis!.Gruppen.Single(g => g.Schluessel == Energievergleich.GRUPPE_BEDARF);
        Zeichenmodell m = grafik.Diagramme(bedarf).First(x => x.Einheit == "MWh/a").Modell!;

        ErgebnisVergleichZelle zelle = bedarf.Zeilen.First(z => z.Kennzahl == "Wärmebedarf gesamt").Zellen[1];
        Assert.True(zelle.WeichtAb);
        string zeiger = "WP klein · Wärmebedarf gesamt: " + zelle.Text + " MWh/a (" + zelle.Abweichung + ")";
        Assert.Contains(m.Befehle, b => b.Wert == zeiger && b is Rechteck);
        Assert.Contains(m.Befehle.OfType<Linie>(), l => l.Wert == zeiger);
        // Der Stamm weicht nie ab: kein Randstrich in seiner Klammer.
        Assert.DoesNotContain(m.Befehle.OfType<Linie>(), l => l.Marke == "reihe:Stamm");
    }

    [Fact]
    public void Nicht_simulierte_Version_hat_keinen_Balken_und_steht_grau_in_der_Legende()
    {
        var cut = Zeige(Grafikablage(), Stand(simuliert1032: false));
        UebersichtErgebnisGrafik grafik = Grafik(cut);
        Assert.False(grafik.Versionen[2].Simuliert);

        foreach (ErgebnisDiagramm d in cut.Instance.Ergebnis!.Gruppen.SelectMany(g => grafik.Diagramme(g)))
        {
            Assert.Empty(Balken(d.Modell!, "mit Puffer"));
            Assert.NotEmpty(Balken(d.Modell!, "Stamm"));
        }
        IElement ohne = cut.Find(".epos-ergebnis-grafik-version--ohne");
        Assert.Contains("mit Puffer", ohne.TextContent);
        Assert.Contains("nicht simuliert", ohne.TextContent);
        Assert.Null(ohne.GetAttribute("data-farbe"));
    }

    [Fact]
    public void Ausgeblendete_Gruppe_fehlt_und_zugeklappte_bleibt_zu()
    {
        UebersichtErgebnisGaben huelle = Huelle();
        UebersichtErgebnisStand Laden(UebersichtStand s)
        {
            UebersichtErgebnisStand e = huelle.Laden(s);
            e.Gruppen.Single(g => g.Schluessel == Energievergleich.GRUPPE_STROM).Zustand = ErgebnisGruppenZustand.Ausgeblendet;
            return e;
        }
        var cut = Zeige(Grafikablage(), laden: Laden);

        Assert.Empty(cut.FindAll("[data-gruppe=strom]"));
        Assert.Empty(cut.FindAll(".epos-ergebnis-klappgruppe--strom"));
        Assert.NotEmpty(cut.FindAll("section.epos-ergebnis-grafik-gruppe--bedarf"));

        IElement em = cut.Find("section.epos-ergebnis-grafik-klappgruppe.epos-ergebnis-klappgruppe--emissionen");
        Assert.Equal("false", em.QuerySelector(".epos-klappgruppe-knopf")!.GetAttribute("aria-expanded"));
        Assert.Null(em.QuerySelector("figure"));
        Assert.Contains("weicht vom Stammprojekt ab", em.TextContent);

        // Aufgeklappt zeigt sie ihre Diagramme.
        em.QuerySelector(".epos-klappgruppe-knopf")!.Click();
        em = cut.Find("section.epos-ergebnis-klappgruppe--emissionen");
        Assert.NotNull(em.QuerySelector("figure.epos-ergebnis-diagramm"));
    }

    [Fact]
    public void Jedes_Diagramm_hat_eine_Textalternative()
    {
        var cut = Zeige(Grafikablage());
        var figuren = cut.FindAll(".epos-ergebnis-grafik figure.epos-ergebnis-diagramm");
        Assert.NotEmpty(figuren);
        foreach (IElement f in figuren)
        {
            string titel = f.QuerySelector(".epos-diagramm-svg-flaeche")!.GetAttribute("aria-label")!;
            Assert.False(string.IsNullOrEmpty(titel));
            IElement tabelle = f.QuerySelector("table.epos-ergebnis-wertetabelle.epos-komponenten-sprachausgabe")!;
            Assert.Equal(titel, tabelle.QuerySelector("caption")!.TextContent);
            Assert.Equal(4, tabelle.QuerySelectorAll("thead th").Length);
            Assert.NotEmpty(tabelle.QuerySelectorAll("tbody tr"));
        }
        IElement waerme = cut.FindAll("table.epos-ergebnis-wertetabelle tbody tr")
                             .First(tr => tr.QuerySelector("th")!.TextContent == "Wärmebedarf gesamt [MWh/a]");
        Assert.Contains("(+10,0 %)", waerme.TextContent);
    }
}
