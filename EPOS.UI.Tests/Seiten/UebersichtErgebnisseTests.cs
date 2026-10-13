using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der Abschnitt „Ergebnisse — Energie“ der Übersicht (<see cref="UebersichtErgebnisse"/>) mit seiner Hülle
/// <see cref="UebersichtErgebnisGaben"/> und dem Energievergleich des Kerns — ohne Datenbank: Der Ladeweg liefert
/// synthetische Ergebnisse.
/// </summary>
public class UebersichtErgebnisseTests : EposBunitContext
{
    public UebersichtErgebnisseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    /// <summary>Wärmebedarf je Projekt [MWh/a]; der Strombedarf ist überall 50.</summary>
    private static readonly Dictionary<int, double> s_waerme = new() { [1030] = 100, [1031] = 110, [1032] = 100 };

    private int _geladen;

    private UebersichtErgebnisGaben Huelle() => new UebersichtErgebnisGaben((id, stamm) =>
    {
        _geladen++;
        return new VariantenDaten
        {
            IdProjekt = id, IstStamm = stamm, CO2Gesamt = s_waerme[id] / 5,
            Ergebnis = new ErgebnisModel
            {
                Energiebedarf = new ErgebnisEnergiebedarfModel
                {
                    Waermebedarf_Gesamt = s_waerme[id], Strombedarf_Gesamt = 50, Stromrestbedarf = 50
                }
            }
        };
    });

    private static VarianteZeile Zeile(int id, string bezeichner, bool simuliert, bool stamm = false) => new()
    {
        IdProjekt = id, IstStamm = stamm, Bezeichner = bezeichner, Projektname = "Musterhaus " + bezeichner,
        SimZeitpunkt = simuliert ? "02.09.26 10:00" : ""
    };

    private static UebersichtStand Stand(bool simuliert1031 = true, bool stammSimuliert = true, params int[] gewaehlt)
        => new()
        {
            StammId = 1030,
            Zeilen = new[]
            {
                Zeile(1030, "(Stammprojekt)", stammSimuliert, stamm: true),
                Zeile(1031, "WP klein", simuliert1031),
                Zeile(1032, "mit Puffer", stammSimuliert)
            },
            MarkierteId = 1030,
            GewaehlteVarianten = gewaehlt.Length == 0 ? new[] { 1030, 1031 } : gewaehlt
        };

    private IRenderedComponent<UebersichtErgebnisse> Zeige(UebersichtStand stand, UebersichtErgebnisGaben? huelle = null)
        => Render<UebersichtErgebnisse>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.Laden, (huelle ?? Huelle()).Laden));

    private static IElement KennzahlZeile(IRenderedComponent<UebersichtErgebnisse> cut, string beginn)
        => cut.FindAll(".epos-ergebnisvergleich tbody tr")
              .Single(tr => tr.QuerySelector("th")!.TextContent.StartsWith(beginn));

    // ---- Fälle -----------------------------------------------------------

    [Fact]
    public void Der_Abschnitt_erscheint_mit_Gruppen_und_Einheiten()
    {
        var cut = Render<UebersichtErgebnisse>(p => p
            .Add(x => x.Stand, Stand())
            .Add(x => x.Laden, Huelle().Laden)
            .Add(x => x.Titel, WindowsFormsApplication1.MyResource.Resource.BK_ERG_ABSCHNITT_ERGEBNISSE));

        Assert.Equal("Ergebnisse — Energie", cut.Find("h2.epos-uebersicht-abschnitt").TextContent);
        string[] gruppen = cut.FindAll("tr.epos-ergebnis-gruppe th").Select(t => t.TextContent).ToArray();
        Assert.Equal("Bedarf", gruppen.First());
        Assert.Contains("Endenergie", gruppen);
        Assert.DoesNotContain("Emissionen", gruppen);
        Assert.DoesNotContain(cut.FindAll("th.epos-ergebnis-kennzahl"), t => t.TextContent.EndsWith("[t/a]"));
        Assert.Contains(cut.FindAll("th.epos-ergebnis-kennzahl"), t => t.TextContent == "Wärmebedarf gesamt [MWh/a]");
        Assert.Contains(cut.FindAll("th.epos-ergebnis-kennzahl"), t => t.TextContent == "Netzbezug Strom [MWh/a]");

        // Spaltenköpfe: Name und Simulationsstand wie die Gegenüberstellung.
        var koepfe = cut.FindAll("thead th.epos-komponenten-versionkopf");
        Assert.Equal(2, koepfe.Count);
        Assert.Equal("Stamm", koepfe[0].QuerySelector(".epos-komponenten-version")!.TextContent);
        Assert.Equal("simuliert 02.09.26 10:00", koepfe[1].QuerySelector(".epos-komponenten-stand")!.TextContent);
        Assert.Contains("Kosten stehen unter Wirtschaftlichkeit", cut.Find(".epos-ergebnis-quelle").TextContent);
    }

    [Fact]
    public void Abweichung_ist_markiert_mit_Prozentangabe()
    {
        var cut = Zeige(Stand());

        var zellen = KennzahlZeile(cut, "Wärmebedarf gesamt").QuerySelectorAll("td");
        Assert.DoesNotContain("epos-komponenten-zelle--abweichend", zellen[0].ClassName);
        Assert.Contains("epos-komponenten-zelle--abweichend", zellen[1].ClassName);
        Assert.Equal("110", zellen[1].QuerySelector(".epos-ergebnis-wert")!.TextContent);
        Assert.Equal("+10,0 %", zellen[1].QuerySelector(".epos-ergebnis-prozent")!.TextContent);
        Assert.Equal("weicht vom Stammprojekt ab", zellen[1].QuerySelector(".epos-komponenten-sprachausgabe")!.TextContent);

        // Gleicher Strombedarf: keine Marke, keine Prozentangabe.
        var strom = KennzahlZeile(cut, "Strombedarf gesamt").QuerySelectorAll("td");
        Assert.DoesNotContain("epos-komponenten-zelle--abweichend", strom[1].ClassName);
        Assert.Null(strom[1].QuerySelector(".epos-ergebnis-prozent"));
    }

    [Fact]
    public void Nicht_simuliert_zeigt_Strich_mit_Kurztext()
    {
        var cut = Zeige(Stand(simuliert1031: false));

        var zelle = KennzahlZeile(cut, "Wärmebedarf gesamt").QuerySelectorAll("td")[1];
        Assert.Equal("—", zelle.QuerySelector(".epos-ergebnis-wert")!.TextContent);
        Assert.Equal("nicht simuliert", zelle.GetAttribute("title"));
        Assert.DoesNotContain("epos-komponenten-zelle--abweichend", zelle.ClassName);
        Assert.Equal(1, _geladen);   // die nicht simulierte Version wird gar nicht geladen
    }

    [Fact]
    public void Ohne_jede_Simulation_erscheint_der_Hinweis()
    {
        var cut = Zeige(Stand(simuliert1031: false, stammSimuliert: false));

        Assert.Empty(cut.FindAll("table"));
        Assert.StartsWith("Für keine Version im Vergleich liegt ein Simulationsergebnis vor",
                          cut.Find(".epos-ergebnis-leer").TextContent);
        Assert.Equal(0, _geladen);
    }

    [Fact]
    public void Die_Vergleichswahl_steuert_die_Spalten()
    {
        UebersichtErgebnisGaben huelle = Huelle();
        var cut = Zeige(Stand(gewaehlt: new[] { 1030 }), huelle);
        Assert.Single(cut.FindAll("thead th.epos-komponenten-versionkopf"));

        cut.Render(p => p.Add(x => x.Stand, Stand(gewaehlt: new[] { 1030, 1031, 1032 })));
        string[] namen = cut.FindAll("thead .epos-komponenten-version").Select(e => e.TextContent).ToArray();
        Assert.Equal(new[] { "Stamm", "WP klein", "mit Puffer" }, namen);
        Assert.Equal(3, KennzahlZeile(cut, "Wärmebedarf gesamt").QuerySelectorAll("td").Length);

        // Der Puffer der Hülle lädt jede Version nur einmal, solange ihr Simulationsstand bleibt.
        Assert.Equal(3, _geladen);
        cut.Render(p => p.Add(x => x.Stand, Stand(gewaehlt: new[] { 1030, 1032 })));
        Assert.Equal(3, _geladen);
        Assert.Equal(2, cut.FindAll("thead th.epos-komponenten-versionkopf").Count);
    }

    private IRenderedComponent<UebersichtErgebnisse> ZeigeMitAblage(UebersichtStand stand, Func<bool> lesen,
                                                                     Action<bool> melden, bool nurUnterschiede = false)
        => Render<UebersichtErgebnisse>(p => p
            .Add(x => x.Stand, stand)
            .Add(x => x.Laden, Huelle().Laden)
            .Add(x => x.EmissionenOffenLesen, lesen)
            .Add(x => x.EmissionenOffenGewechselt, melden)
            .Add(x => x.NurUnterschiede, nurUnterschiede));

    [Fact]
    public void Die_Emissionen_sind_eine_zugeklappte_Gruppe_mit_Zahl_und_Hinweis()
    {
        var cut = Zeige(Stand());

        IElement gruppe = cut.Find("section.epos-ergebnis-klappgruppe");
        Assert.Equal("Emissionen", gruppe.QuerySelector(".epos-klappgruppe-text")!.TextContent);
        Assert.Equal("false", gruppe.QuerySelector(".epos-klappgruppe-knopf")!.GetAttribute("aria-expanded"));
        Assert.Null(gruppe.QuerySelector("table"));
        // Nur CO₂ gesamt trägt einen Wert (spezifisch fehlt) - die Zahl steht im Kopf.
        Assert.Equal("1 Kennzahl", gruppe.QuerySelector(".epos-klappgruppe-zusatz")!.TextContent);
        // 20 gegen 22 t/a: Der Kopf zeigt die Abweichung auch zugeklappt.
        Assert.Contains("weicht vom Stammprojekt ab", gruppe.QuerySelector(".epos-ergebnis-klapphinweis")!.TextContent);
    }

    [Fact]
    public void Aufklappen_zeigt_die_Zeilen_und_die_Stellung_geht_in_die_Ablage()
    {
        bool offen = false;
        var cut = ZeigeMitAblage(Stand(), () => offen, an => offen = an);

        cut.Find("section.epos-ergebnis-klappgruppe .epos-klappgruppe-knopf").Click();

        Assert.True(offen);
        IElement gruppe = cut.Find("section.epos-ergebnis-klappgruppe");
        var zeilen = gruppe.QuerySelectorAll("th.epos-ergebnis-kennzahl");
        Assert.Contains(zeilen, z => z.TextContent.EndsWith("[t/a]"));
        var zellen = gruppe.QuerySelectorAll("tbody td");
        Assert.Equal("22,0", zellen[1].QuerySelector(".epos-ergebnis-wert")!.TextContent);
        Assert.Contains("epos-komponenten-zelle--abweichend", zellen[1].ClassName);

        // Ein neuer Abschnitt in derselben Sitzung liest die Stellung aus der Ablage.
        var zweiter = ZeigeMitAblage(Stand(), () => offen, an => offen = an);
        Assert.NotNull(zweiter.Find("section.epos-ergebnis-klappgruppe table"));
    }

    [Fact]
    public void Nur_Unterschiede_blendet_Gruppen_ohne_Abweichung_aus()
    {
        var cut = ZeigeMitAblage(Stand(gewaehlt: new[] { 1030, 1032 }), () => false, _ => { }, nurUnterschiede: true);

        // 1032 rechnet wie der Stamm: keine Gruppe weicht ab, der Hinweis steht.
        Assert.Empty(cut.FindAll("table"));
        Assert.Empty(cut.FindAll("section.epos-ergebnis-klappgruppe"));
        Assert.Equal("Keine Version im Vergleich weicht in den Energie-Ergebnissen vom Stammprojekt ab.",
                     cut.Find(".epos-ergebnis-gleich").TextContent);

        cut.Render(p => p.Add(x => x.Stand, Stand()));
        string[] gruppen = cut.FindAll("tr.epos-ergebnis-gruppe th").Select(t => t.TextContent).ToArray();
        Assert.Equal(new[] { "Bedarf" }, gruppen);   // nur der Wärmebedarf weicht ab
        Assert.Single(cut.FindAll("section.epos-ergebnis-klappgruppe"));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Abschnitt_nichts()
    {
        var cut = Render<UebersichtErgebnisse>();
        Assert.Empty(cut.Markup.Trim());
        Assert.Equal("", cut.Instance.Text());
    }

    [Fact]
    public void In_der_Uebersicht_zwei_Abschnitte_und_der_Assistent_liest_die_Ergebnisse()
    {
        UebersichtStand stand = Stand();
        UebersichtErgebnisGaben huelle = Huelle();
        var satz = new Dictionary<string, object>(huelle.Gaben())
        {
            ["Laden"] = new Func<UebersichtStand, UebersichtErgebnisStand>(huelle.Laden)
        };
        var cut = Render<UebersichtSeite>(p => p
            .Add(x => x.Laden, () => stand)
            .Add(x => x.ErgebnisGaben, satz)
            .Add(x => x.LabelAbschnittKomponenten, WindowsFormsApplication1.MyResource.Resource.BK_ERG_ABSCHNITT_KOMPONENTEN));

        string[] abschnitte = cut.FindAll("h2.epos-uebersicht-abschnitt").Select(h => h.TextContent).ToArray();
        Assert.Equal(new[] { "Komponenten und Einstellungen", "Ergebnisse — Energie" }, abschnitte);

        KiFeldzugang feld = KiMaskenbruecke.Feldzugang(KiMaskennamen.BERICHTE_UEBERSICHT, "ergebnisse");
        Assert.NotNull(feld);
        Assert.False(feld.Setzbar);
        string text = (string)feld.Lesen();
        Assert.Contains("Bedarf · Wärmebedarf gesamt [MWh/a]: Stamm = 100; WP klein = 110 (+10,0 %)"
                        + " — weicht vom Stammprojekt ab", text);
    }
}
